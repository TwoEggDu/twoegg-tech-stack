---
title: "一次 AssetBundle 构建后 Shader Variant 丢失问题的定位与修复"
description: "记录一次 AB 构建后 shader variant 未生成的问题排查，聚焦 URP ShaderPrefilteringData 链路，以及 URP 14.0.11 之前项目的变通方案为什么有效、为什么不合理。"
series: "工程诊断案例"
weight: 20
featured: true
tags:
  - "Unity"
  - "URP"
  - "Shader"
  - "AssetBundle"
  - "Build"
---
## 问题现象

AssetBundle 构建完成后，运行时发现部分 shader 效果异常，表现为光照缺失、阴影消失或后处理失效。

检查 shader 本身没有问题，换一台机器构建也偶发复现。进一步排查后发现，是 shader variant 在 AB 里根本没有被打进去——不是 variant 被过滤掉了，而是从来就没有生成过。

Shader variant 丢失当然不止这一种原因，本文只记录这次排查里遇到的具体情况：variant 在 AB 构建阶段根本没有生成。

---

## 这不是 OnProcessShader 的问题

遇到 shader variant 丢失，很多人的第一反应是去查 `IPreprocessShaders.OnProcessShader`，看看是不是哪里把 variant 过滤掉了。

但这次排查遇到的问题，边界在更底层的地方。

Unity URP 有一套叫做 **Shader Keyword Prefiltering** 的机制，控制的是哪些 keyword 组合在构建时**根本不参与编译**。它的作用层级比 `OnProcessShader` 更早——不是"编译了再过滤"，而是"根本不生成"。

这套机制的配置存在每个 URP Pipeline Asset 的 `.asset` 文件里，就是这些字段：

```yaml
m_PrefilteringModeMainLightShadows: 3
m_PrefilteringModeAdditionalLight: 3
m_PrefilteringModeAdditionalLightShadows: 0
m_PrefilteringModeForwardPlus: 0
m_PrefilteringModeDeferredRendering: 0
m_PrefilteringModeScreenSpaceOcclusion: 0
m_PrefilterXRKeywords: 1
m_PrefilterDebugKeywords: 1
m_PrefilterHDROutput: 1
m_PrefilterSoftShadows: 0
# ...以及更多 SSAO、DBuffer、SoftShadow 细分字段
```

这些字段统称 **`ShaderPrefilteringData`**，在 `UniversalRenderPipelineAssetPrefiltering.cs` 里定义，每个字段都带有 `[ShaderKeywordFilter.RemoveIf]` 或 `[ShaderKeywordFilter.SelectIf]` attribute。Unity 构建系统在处理 shader 时直接读取这些 attribute 加字段运行时值，决定哪些 keyword 组合生成、哪些不生成。

这意味着：如果这些字段的值是陈旧的，你看代码、看 `OnProcessShader` 回调全都是对的，但 variant 就是不在 AB 里。

---

## 字段怎么写进去的

`ShaderPrefilteringData` 不是手动维护的，它应该在每次构建前由 Unity 根据项目实际配置自动计算并写入。

调用链是这样的：

- `GatherShaderFeatures(isDevelopmentBuild)`
  - `GetGlobalAndPlatformSettings()`
    - 读取 `URP Global Settings` 里的 `stripUnusedVariants`
    - 读取 `stripDebugVariants` 等开关，以及平台信息（XR/Mobile）
  - `GetSupportedFeaturesFromVolumes()`
    - 扫描项目里所有 `VolumeProfile`，确定哪些后处理 keyword 被用到
  - `HandleEnabledShaderStripping()`
    - `TryGetRenderPipelineAssets()`
      - 获取当前平台所有 `URP Pipeline Asset`
    - `GetSupportedShaderFeaturesFromAssets()`
      - 逐个扫描每个 `URP Asset` 及其 `Renderer / RendererFeature`
      - `CreatePrefilteringSettings()`：计算出 `ShaderPrefilteringData`
      - `urpAsset.UpdateShaderKeywordPrefiltering(ref spd)`：写入字段
      - `AssetDatabase.SaveAssetIfDirty(urpAsset)`：落盘

正常的 Player 构建会在 `IPreprocessBuildWithReport.OnPreprocessBuild` 阶段触发这个链路，字段值保持最新。

**但 AssetBundle 构建不经过 `OnPreprocessBuild`。**

如果在 AB 构建之前没有跑过这条链路，Pipeline Asset 文件里的 `m_Prefiltering*` 字段就是上一次 Player 构建、或版本控制 checkout 时的旧值。构建系统读到旧值，按旧的策略决定不生成某些 variant——variant 丢失就这么发生了。

---

## 旧项目的变通方案

在官方修复之前，一些项目的做法是：AB 构建前先跑一次 `buildScriptsOnly=true` 的 Player 构建。

逻辑是对的：Player 构建会触发 `OnPreprocessBuild` → `GatherShaderFeatures` → `UpdateShaderKeywordPrefiltering` → `SaveAssetIfDirty`，Pipeline Asset 落盘后，再做 AB 构建时读到的就是正确的 prefiltering 数据。

但这个方案本身有几个问题：

**1. 代价极高**
`buildScriptsOnly` 的 Player 构建仍然会触发 IL2CPP 脚本编译和 `BuildOptions.CleanBuildCache`，清掉 `Library/Bee` 里的构建中间产物，整个过程非常耗时。真正需要的只是"更新几个 `.asset` 字段"，却搭了一辆重型列车。

**2. 关注点混搭**
某些项目把这步骑在 HybridCLR 的 `GenHybridCLRAll` 上实现——HybridCLR 编译里有一步跑 Player 构建，正好产生了更新 prefiltering 数据的副作用。但这两件事本质没有关系，用 HybridCLR 编译作为 shader prefiltering 的修复手段，出了问题很难定位原因。

**3. 附带 bug**
部分项目还有"构建前清 Pipeline Asset 引用、构建后回滚"的操作，用 SVN revert 来恢复 `QualitySettings.asset`，但忘记同步回滚 `GraphicsSettings.asset`，导致 `GraphicsSettings.renderPipelineAsset` 永久变成 null。

---

## 官方修复

URP **14.0.11**（2025-02-13，对应 Unity 2022.3.59f1）加入了一个新类：

```csharp
/// <summary>
/// This class is used solely to make sure Shader Prefiltering data inside the
/// URP Assets get updated before anything (Like Asset Bundles) are built.
/// </summary>
class UpdateShaderPrefilteringDataBeforeBuild : IPreprocessShaders
{
    public int callbackOrder => -100;

    public UpdateShaderPrefilteringDataBeforeBuild()
    {
        ShaderBuildPreprocessor.GatherShaderFeatures(Debug.isDebugBuild);
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippetData,
        IList<ShaderCompilerData> compilerDataList) {}
}
```

`IPreprocessShaders` 的构造函数在任何构建（包括 AB 构建）开始收集 shader 时都会被实例化，`callbackOrder = -100` 保证它在所有其他 shader 处理器之前运行。

这样，只要触发 AB 构建，`GatherShaderFeatures` 就会自动执行，Pipeline Asset 的 prefiltering 字段在当次构建里永远是最新的。升级到这个版本之后，不再需要任何前置的 Player 构建或手动干预。

---

## 还剩一个独立问题：ShaderCache 污染

`UpdateShaderPrefilteringDataBeforeBuild` 解决的是"prefiltering 配置陈旧"的问题，但还有另一个独立问题需要注意。

`Library/ShaderCache` 存的是已编译的 shader program 缓存，跨构建类型共用。当 shader 源文件或 include 文件发生变化时，Unity 有时不会正确失效这个缓存，AB 构建会复用旧的编译结果，导致新增的 variant 不出现在包里。

这个问题靠 prefiltering 机制解决不了。需要在感知到 shader 源码有实质变更时，手动清一次 `Library/ShaderCache`。

---

## 结论

| 问题 | 这次排查中的原因 | 修复方式 |
|---|---|---|
| AB 构建 shader variant 丢失 | Pipeline Asset 的 `m_Prefiltering*` 字段陈旧 | URP 14.0.11 `UpdateShaderPrefilteringDataBeforeBuild` 自动修复；旧版本需在 AB 构建前触发一次 Player 构建 |
| Shader 改动后 AB 不更新 | `Library/ShaderCache` 缓存污染 | 手动删除 `Library/ShaderCache` |

如果你的项目 URP 版本低于 14.0.11，而你遇到的正好也是 prefiltering 数据陈旧问题，它随时可能以各种面目重现。升级包或者补一个自定义的 `IPreprocessShaders`（在构造函数里调用 `GatherShaderFeatures`），是更可靠的长期方案。
