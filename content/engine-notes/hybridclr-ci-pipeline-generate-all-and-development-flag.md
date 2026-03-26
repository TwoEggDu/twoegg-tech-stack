---
title: "HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景"
date: "2026-03-26"
series:
  - "HybridCLR"
tags:
  - "HybridCLR"
  - "CI/CD"
  - "Android"
  - "IL2CPP"
  - "Build Pipeline"
weight: 45
---
在 Unity Editor 里手动点 `HybridCLR/Generate/All` 是开发期的操作，**CI 打包流程里必须显式调用它，而且必须确保 Development 标志与最终构建完全一致。** 这两件事做不到，补充元数据就会失效，热更代码里的 async/await 就可能在真机上崩溃——即使在编辑器里跑完全正常。

这篇讲清楚三件事：为什么 GenerateAll 要进流程、Development 标志为什么会错、Launcher-only 场景优化是什么。

---

## 一、为什么 GenerateAll 必须进打包流程

`HybridCLR/Generate/All` 背后是一条严格的依赖链：

```
CompileDll → Il2CppDef → LinkXml → StripAOTDlls → MethodBridge → AOTGenericReference
```

其中最关键的一步是 **StripAOTDlls**，它会在内部启动一次完整的 `BuildScriptsOnly` Player 构建，拿到 IL2CPP 裁剪后的 AOT DLL。这批 DLL 有两个用途：

1. 运行时通过 `LoadMetadataForAOTAssembly` 加载，为解释器补充 AOT 泛型方法体（补充元数据）
2. 作为 MethodBridge 和 AOTGenericReference 生成的分析基础

**如果 StripAOTDlls 跑的那次内部构建与最终 APK 构建的环境不一致，这批 DLL 就是错的。** 拿错误的 DLL 生成的 MethodBridge 和 AOTGenericReference 也会是错的，运行时补充元数据失效，热更代码里的泛型调用找不到方法体，退化到 FullySharedGenericAny 路径，进而引发死循环崩溃。

如果打包流程里没有 GenerateAll，用的是上次手动生成的产物，这个问题就完全取决于"上次手动跑 Generate 的时候环境是否恰好一致"——在 CI 里这是不可接受的。

---

## 二、Development 标志不一致的具体机制

`StripAOTDllCommand.GenerateStripedAOTDlls` 在启动内部 BuildScriptsOnly 时，读的是 `EditorUserBuildSettings.development`：

```csharp
// HybridCLR 源码：StripAOTDllCommand.cs
static BuildOptions GetBuildPlayerOptions(BuildTarget buildTarget)
{
    BuildOptions options = BuildOptions.None;
    bool development = EditorUserBuildSettings.development;  // 读面板状态
    if (development)
        options |= BuildOptions.Development;
    ...
    return options;
}
```

`PrebuildCommand.GenerateAll`（菜单按钮背后的实现）同样读面板：

```csharp
// HybridCLR 源码：PrebuildCommand.cs
public static void GenerateAll()
{
    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);  // 读面板
    ...
    StripAOTDllCommand.GenerateStripedAOTDlls(target);  // 内部也读面板
    ...
}
```

**面板状态就是 Build Settings 窗口（Ctrl+Shift+B）里的 Development Build 勾选框。**

在 CI 环境里，这个勾选框的状态取决于上次有人在这台机器上手动操作时留下的值，完全不可控。如果 CI 打 release 包（不勾 Development），但 Generate 时面板上是勾着的，StripAOTDlls 内部就会以 Development 模式跑一次构建，拿到带调试符号的 AOT DLL；而最终 release APK 里用的是非 development 模式的 IL2CPP 编译产物——两者不匹配，补充元数据失效。

---

## 三、正确的修法：在流程里强制写入 Development 标志

在调用任何 Generate 步骤之前，**先把 `EditorUserBuildSettings.development` 写成和最终构建一致的值**，不依赖面板当前状态。

```csharp
/// <summary>
/// 打包前完整 Generate/All。
/// 必须在 BuildAndroidAPK / BuildWindowsEXE 之前调用，且 development 参数与最终 buildOptions 一致。
/// </summary>
public static void GenerateAllWithDevelopment(BuildTarget target, bool development, bool deepProfile = false)
{
#if ENABLE_HYBRIDCLR
    if (!HybridCLR.Editor.SettingsUtil.Enable)
        return;

    // 先强制写入面板状态，StripAOTDlls 内部会读这几个值
    EditorUserBuildSettings.development = development;
    EditorUserBuildSettings.allowDebugging = development;
    EditorUserBuildSettings.buildWithDeepProfilingSupport = development && deepProfile;

    CompileDllCommand.CompileDll(target, development);
    Il2CppDefGeneratorCommand.GenerateIl2CppDef();
    LinkGeneratorCommand.GenerateLinkXml(target);

    // StripAOTDlls 前临时切换为 Launcher-only 场景（见下一节）
    var savedScenes = EditorBuildSettings.scenes;
    try
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Launcher.unity", true)
        };
        StripAOTDllCommand.GenerateStripedAOTDlls(target);
    }
    finally
    {
        EditorBuildSettings.scenes = savedScenes;
    }

    MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
    AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
#endif
}
```

在打包流程里，紧接在最终 Player Build 之前调用它：

```csharp
// 打包流程入口（CI 构建器）
if (req.HybridClr)
{
    Debug.Log($"[Build] GenerateAll — development:{req.Development} deepProfile:{req.DeepProfile}");
    BuildDLLCommand.GenerateAllWithDevelopment(
        req.IsWindows ? BuildTarget.StandaloneWindows64 : BuildTarget.Android,
        req.Development,
        req.DeepProfile);
}
// 然后才是 BuildAndroidAPK / BuildWindowsEXE
```

`req.Development` 来自 CI 参数（命令行 `--development true/false`），与 `BuildOptions` 里的 `Development` 标志完全同源，不再依赖面板。

---

## 四、Launcher-only 场景：为什么要做、怎么做

`StripAOTDllCommand.GenerateStripedAOTDlls` 内部跑 BuildScriptsOnly 时，读的是 `EditorBuildSettings.scenes`——就是 Build Settings 面板上勾了的场景列表。

**如果面板上配了十几个场景，这次内部构建就要处理十几个场景的脚本依赖，耗时显著增加。** 更重要的是，最终 Player Build 往往只包含 Launcher 场景（热更内容通过资源系统动态加载），两者的场景集合不一致会引入额外的依赖分析偏差。

临时把场景列表切换为只包含 Launcher 场景，跑完 StripAOTDlls 再还原：

```csharp
var savedScenes = EditorBuildSettings.scenes;
try
{
    EditorBuildSettings.scenes = new[]
    {
        new EditorBuildSettingsScene("Assets/Scenes/Launcher.unity", true)
        // 路径与 BuildAndroidAPK 传入的 BUILD_SCENES 保持一致
    };
    StripAOTDllCommand.GenerateStripedAOTDlls(target);
}
finally
{
    EditorBuildSettings.scenes = savedScenes;  // 异常时也保证还原
}
```

两个收益：
- **速度**：只编译 Launcher 场景的脚本依赖，StripAOTDlls 内部构建耗时大幅下降
- **一致性**：Strip 结果与最终 APK 构建所用的场景集合完全相同，裁剪结果可复现

---

## 五、热更 DLL 编译也要传 development 参数

热更 DLL 打进 MPQ（或 AssetBundle）的那次编译，同样不能用无参版本的 `CompileDll`：

```csharp
// 错误：读面板，不可控
CompileDllCommand.CompileDll(buildTarget);

// 正确：显式传入与最终构建一致的 development 参数
CompileDllCommand.CompileDll(buildTarget, development);
```

虽然 development 标志对热更 DLL 的 IL 内容影响不大（主要影响调试符号），但让整条流程的 flag 来源统一在一处，是维护性的基本要求。

---

## 把这件事压成一句话

> CI 打包流程里，`GenerateAll` 之前必须先把 `EditorUserBuildSettings.development` 写成与最终 Player Build 一致的值；`StripAOTDlls` 之前把场景列表临时切为 Launcher-only。这两件事不做，补充元数据在真机上就是随机失效的。
