---
date: "2026-03-26"
title: "HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么"
description: "从 Settings、Installer、Generate/All 到 BuildProcessors，拆解 HybridCLR 的 build-time 工具链到底生成了什么、被谁消费、少了会怎样。"
weight: 32
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Toolchain"
series: "HybridCLR"
---
> HybridCLR 菜单不是一组“方便操作”的按钮，而是在为改造后的 IL2CPP runtime 逐项准备输入：本地 `libil2cpp`、热更 DLL、防裁剪信息、裁剪后的 AOT 快照、桥接代码，以及泛型风险清单。

这是 HybridCLR 系列第 3 篇，回到 build-time 侧解释 runtime 主链为什么能成立。

但只讲 runtime 还不够。  
因为真正落项目时，大家最容易困惑的反而是 Editor 侧那堆菜单。

很多人第一次看 HybridCLR 菜单，脑子里都会冒出两个问题：

- 这些按钮为什么这么多
- 它们到底在生成什么，少点一步会怎样

我觉得这篇文章就该把这件事讲透。

## 这篇要回答什么

这篇主要回答 5 个问题：

1. `Settings` 到底在配置什么，它为什么不只是“杂项设置”。
2. `Installer` 真正安装的是什么。
3. `Generate/All` 这条顺序为什么不能随便改。
4. `CompileDll / Il2CppDef / LinkXml / AOTDlls / MethodBridge / AOTGenericReference` 分别产出什么。
5. 这些产物最后分别被谁消费。

这篇文章的重点不是“菜单翻译”，而是把 build-time 这条因果链重新立起来。

上一篇已经把 AOT 泛型和补充 metadata 的运行时边界讲过了，所以本文只讲生成物、依赖关系和消费方，不再重复展开那部分 runtime 语义。

## 为什么这个问题值得单独拆

因为如果不把工具链这层讲清楚，读者会非常容易产生两种错觉。

第一种错觉是：

`HybridCLR 的核心都在 runtime，Editor 按钮只是辅助。`

第二种错觉是：

`这些 Generate 按钮互相独立，缺一个最多只是优化不到位。`

但从源码看，这两种理解都不对。

HybridCLR 的 build-time 工具链不是在做“预处理杂活”，而是在给 runtime 准备真正的输入。

缺一步，后果通常不是“效果差一点”，而是：

- runtime 根本找不到它需要的输入
- 构建时没用上你本地改造过的 `libil2cpp`
- 解释器跨边界调用签名对不上
- 热更引用的 AOT 类型在主包里被裁掉
- AOT 泛型问题没有被显式暴露出来

所以这篇文章的最好读法不是按菜单一个个记，而是先把依赖链记住。

## 先给源码地图

这篇还是基于：

`<ProjectRoot>`

如果你准备边看边读，我建议优先打开这些文件：

- `Packages/HybridCLR/Editor/Settings/HybridCLRSettings.cs`
- `Packages/HybridCLR/Editor/SettingsUtil.cs`
- `Packages/HybridCLR/Editor/Installer/InstallerController.cs`
- `Packages/HybridCLR/Editor/Commands/PrebuildCommand.cs`
- `Packages/HybridCLR/Editor/Commands/CompileDllCommand.cs`
- `Packages/HybridCLR/Editor/Commands/Il2CppDefGeneratorCommand.cs`
- `Packages/HybridCLR/Editor/Commands/LinkGeneratorCommand.cs`
- `Packages/HybridCLR/Editor/Commands/StripAOTDllCommand.cs`
- `Packages/HybridCLR/Editor/BuildProcessors/CopyStrippedAOTAssemblies.cs`
- `Packages/HybridCLR/Editor/Commands/MethodBridgeGeneratorCommand.cs`
- `Packages/HybridCLR/Editor/Commands/AOTReferenceGeneratorCommand.cs`
- `Packages/HybridCLR/Editor/BuildProcessors/CheckSettings.cs`

如果你的工程菜单里还混着 `Define Symbols`、`BuildAssets And CopyTo AssemblyTextAssetPath`、`AOT引用热更检查` 这类按钮，那通常是项目自己的封装，不是 HybridCLR package 主链。本文只按 package 自己的工具链讲。

## 先给一个总判断：这是一条产物流水线，不是一组平铺命令

如果直接看 `PrebuildCommand.cs`，这件事其实已经很直白了：

```csharp
[MenuItem("HybridCLR/Generate/All", priority = 200)]
public static void GenerateAll()
{
    var installer = new Installer.InstallerController();
    if (!installer.HasInstalledHybridCLR())
    {
        throw new BuildFailedException("please install it via menu 'HybridCLR/Installer'");
    }
    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);
    Il2CppDefGeneratorCommand.GenerateIl2CppDef();
    LinkGeneratorCommand.GenerateLinkXml(target);
    StripAOTDllCommand.GenerateStripedAOTDlls(target);
    MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
    AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
}
```

这段代码不是“菜单目录”，而是一条明确的依赖顺序：

1. 先要有热更 DLL
2. 再把本地 `libil2cpp` 的 generated 部分同步到当前工程
3. 再根据热更 DLL 生成 `link.xml`
4. 再拿裁剪后的 AOT DLL 快照
5. 再根据 AOT DLL 和热更 DLL 生成 bridge
6. 最后再分析泛型风险清单

这条顺序本身就说明：

`HybridCLR 的 build-time 工具链是一条依赖流水线。`

![HybridCLR 工具链图](../../images/hybridclr/toolchain-pipeline.svg)

*图：这一组按钮不是平铺命令，而是一条先产出、再被 runtime 或构建链消费的依赖流水线。*

## `Settings` 不是杂项，而是整条流水线的产物路由表

如果要理解这些生成物最后去哪，第一步反而不是看命令，而是先看 `HybridCLRSettings.cs`。

```csharp
public string hotUpdateDllCompileOutputRootDir = "HybridCLRData/HotUpdateDlls";
public string strippedAOTDllOutputRootDir = "HybridCLRData/AssembliesPostIl2CppStrip";
public string outputLinkFile = "HybridCLRGenerate/link.xml";
public string outputAOTGenericReferenceFile = "HybridCLRGenerate/AOTGenericReferences.cs";
public string[] patchAOTAssemblies;
```

这几个字段其实已经把工具链最重要的产物图画出来了：

- 热更 DLL 输出到哪
- 裁剪后的 AOT DLL 输出到哪
- `link.xml` 写到哪
- `AOTGenericReferences.cs` 写到哪
- 哪些 AOT assembly 需要被当成补充 metadata 输入

再看 `SettingsUtil.cs`：

```csharp
public static string HybridCLRDataDir => $"{ProjectDir}/HybridCLRData";
public static string LocalIl2CppDir => $"{LocalUnityDataDir}/il2cpp";
public static string GeneratedCppDir => $"{LocalIl2CppDir}/libil2cpp/hybridclr/generated";

public static string GetHotUpdateDllsOutputDirByTarget(BuildTarget target)
{
    return $"{HotUpdateDllsRootOutputDir}/{target}";
}

public static string GetAssembliesPostIl2CppStripDir(BuildTarget target)
{
    return $"{AssembliesPostIl2CppStripDir}/{target}";
}
```

这就更清楚了。

工具链的关键产物至少会落到三块地方：

- `HybridCLRData/HotUpdateDlls/{target}`
- `HybridCLRData/AssembliesPostIl2CppStrip/{target}`
- `HybridCLRData/LocalIl2CppData-*/il2cpp/libil2cpp/hybridclr/generated`

所以 `Settings` 的真实语义不是“个性化配置”，而是：

`定义整条工具链的产物拓扑。`

## `Installer` 真正安装的不是插件，而是本地 `libil2cpp`

这一点在 `InstallerController.cs` 里非常明确。

先看最核心的一段：

```csharp
string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
CloneBranch(workDir, hybridclrRepoURL, _curDefaultVersion.hybridclr.branch, hybridclrRepoDir);

string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
CloneBranch(workDir, il2cppPlusRepoURL, _curDefaultVersion.il2cpp_plus.branch, il2cppPlusRepoDir);

Directory.Move($"{hybridclrRepoDir}/hybridclr", $"{il2cppPlusRepoDir}/libil2cpp/hybridclr");
return $"{il2cppPlusRepoDir}/libil2cpp";
```

再看真正落地到本地的部分：

```csharp
string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
BashUtil.RecreateDir(localUnityDataDir);
BashUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);

string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
BashUtil.CopyDir($"{libil2cppWithHybridclrSourceDir}", dstLibil2cppDir, true);

BashUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);
```

这几句已经把 `Installer` 的真正语义说透了：

- 先准备一份带 `hybridclr` 的 `libil2cpp`
- 再复制 Unity Editor 自己那份 `il2cpp` 到项目本地
- 然后把本地 `libil2cpp` 替换成带 HybridCLR 的版本
- 最后清理 `Il2cppBuildCache`

所以 `Installer` 安装的不是“HybridCLR 功能开关”，而是：

`一份项目本地、可被构建真正使用的 HybridCLR 版 il2cpp/libil2cpp。`

## 真正打包时，Unity 怎么知道该用这份本地 `libil2cpp`

这件事在 `CheckSettings.cs` 里有非常关键的一段：

```csharp
string curIl2cppPath = Environment.GetEnvironmentVariable("UNITY_IL2CPP_PATH");
if (curIl2cppPath != SettingsUtil.LocalIl2CppDir)
{
    Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", SettingsUtil.LocalIl2CppDir);
}
```

也就是说，真正打包前，HybridCLR 会通过 `UNITY_IL2CPP_PATH` 把 Unity 的构建链路指向项目本地这份 `il2cpp`。

这就是为什么我一直不喜欢把 `Installer` 理解成“装一个包”。

因为它真正改的是：

`构建时到底用哪一份 il2cpp。`

少了这一步，后面你生成再多文件，runtime 本体还是 Unity 自带那份没被改造的 `libil2cpp`。

而且 `CheckSettings.cs` 做的还不止这一件事。  
它还会在正式构建前强制确认几件前提：

- 当前脚本后端是不是 `IL2CPP`
- `Installer` 有没有真正跑过
- package 版本和本地安装的 `libil2cpp` 版本是否一致

也就是说，HybridCLR 的 build-time 链路不是“尽量帮你做对”，而是：

`在真正进入打包前，尽量把关键前置条件都钉死。`

## `CompileDll`：先把热更 DLL 产出来

`CompileDllCommand.cs` 的核心逻辑其实很短：

```csharp
ScriptCompilationSettings scriptCompilationSettings = new ScriptCompilationSettings();
scriptCompilationSettings.group = group;
scriptCompilationSettings.target = target;
scriptCompilationSettings.options = developmentBuild ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None;
ScriptCompilationResult scriptCompilationResult = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, buildDir);
```

这一步的作用非常直接：

- 按当前 `BuildTarget` 编译热更程序集
- 输出到 `HybridCLRData/HotUpdateDlls/{target}`
- `development` 标志也一并进入编译语义

为什么 `development` 值得单独提？

因为后面的 `MethodBridge.cpp` 也会把当前 `development` 状态写进去，而 `CheckSettings.cs` 会在正式打包前专门校验这个标志是否一致：

```csharp
var match = Regex.Match(File.ReadAllText(methodBridgeFile), @"// DEVELOPMENT=(\d)");
...
if (developmentFlagInMethodBridge != developmentFlagInEditorSettings)
{
    Debug.LogError("Please run 'HybridCLR/Generate/All' before building.");
}
```

这说明 `CompileDll` 和后面的 bridge 生成并不是松耦合的。

### 这一层到底解决了什么问题

它解决的是：

`给整条流水线提供统一的热更程序集输入。`

没有这一步，`LinkXml`、`MethodBridge`、`AOTGenericReference` 根本没有分析对象。

## `Il2CppDef`：把当前工程信息同步进本地 `libil2cpp/generated`

`Il2CppDefGeneratorCommand.cs` 很短，但它的位置非常关键：

```csharp
var options = new Il2CppDef.Il2CppDefGenerator.Options()
{
    UnityVersion = Application.unityVersion,
    HotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved,
    UnityVersionOutputFile = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/generated/UnityVersion.h",
    AssemblyManifestOutputFile = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/generated/AssemblyManifest.cpp",
};
```

这一步至少在同步两类信息：

- 当前 Unity 版本
- 当前热更程序集清单

输出文件则直接写进本地 `libil2cpp/hybridclr/generated`：

- `UnityVersion.h`
- `AssemblyManifest.cpp`

这一步的真实语义不是“生成宏定义”，而是：

`把当前工程的版本和热更程序集事实，同步进本地改造过的 libil2cpp。`

所以它是 build-time 到 runtime 本体之间的一层桥。

## `LinkXml`：把热更引用暴露给主包裁剪器

`LinkGeneratorCommand.cs` 的主线也很短：

```csharp
List<string> hotfixAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
var analyzer = new Analyzer(MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotfixAssemblies));
var refTypes = analyzer.CollectRefs(hotfixAssemblies);
linkXmlWriter.Write($"{Application.dataPath}/{ls.outputLinkFile}", refTypes);
```

这一层解决的是静态裁剪可见性问题。

主包裁剪器只看构建时能看到的可达性。  
但热更 DLL 对很多 AOT 类型的引用，是“构建后未来才会出现的引用”。

所以 `LinkXml` 的真实语义不是“生成一个配置文件”，而是：

`把热更代码里会用到的 AOT 类型，提前暴露给 UnityLinker。`

少了这一步，后果通常不是运行得慢，而是运行时根本找不到目标类型或成员。

## `AOTDlls`：先做一次 `buildScriptsOnly`，再把裁剪后的 AOT DLL 快照拷出来

这一层我觉得是菜单里最容易被误解的一步。

很多人会把 `Generate/AOTDlls` 误解成“生成 AOT 版本的热更 DLL”。

但看 `StripAOTDllCommand.cs` 就会发现，不是这么回事：

```csharp
CheckSettings.DisableMethodBridgeDevelopmentFlagChecking = true;
EditorUserBuildSettings.buildScriptsOnly = true;

BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
{
    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
    locationPathName = location,
    options = buildOptions,
    target = target,
    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
};

var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
```

这里真正干的，是一次 `buildScriptsOnly` 的 Player 构建。

为什么要这么重？

因为它不是为了“编出 DLL”，而是为了拿到 Unity 真正裁剪后的 AOT 世界。

接着看 `CopyStrippedAOTAssemblies.cs`：

```csharp
var dstPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

foreach (var fileFullPath in Directory.GetFiles(srcStripDllPath, "*.dll"))
{
    var file = Path.GetFileName(fileFullPath);
    File.Copy($"{fileFullPath}", $"{dstPath}/{file}", true);
}
```

也就是说，这一步最终把 Unity 构建过程中产出的 `ManagedStripped` DLL 复制到：

`HybridCLRData/AssembliesPostIl2CppStrip/{target}`

这就是后面很多分析步骤真正依赖的输入基线。

### 这一层到底解决了什么问题

它解决的是：

`给后续分析器提供“最终裁剪后的 AOT 世界快照”，而不是源码世界里的理想程序集。`

没有这一步，后面的 MethodBridge 和泛型分析都容易建立在错误前提上。

## `MethodBridge`：基于热更 DLL 和裁剪后 AOT DLL，生成真正会被 runtime 消费的桥接代码

`MethodBridgeGeneratorCommand.cs` 把依赖关系写得非常清楚：

```csharp
string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
List<string> aotAssemblyNames = Directory.Exists(aotDllDir)
    ? Directory.GetFiles(aotDllDir, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToList()
    : new List<string>();
if (aotAssemblyNames.Count == 0)
{
    throw new Exception("please run `HybridCLR/Generate/All` or `HybridCLR/Generate/AotDlls` ...");
}
```

这一步明说了：  
如果没有裁剪后的 AOT DLL 快照，桥接代码根本没法生成。

继续往下看：

```csharp
methodBridgeAnalyzer.Run();
reversePInvokeAnalyzer.Run();
calliAnalyzer.Run();
pinvokeAnalyzer.Run();

string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge.cpp";
GenerateMethodBridgeCppFile(..., outputFile);

CleanIl2CppBuildCache();
```

这说明它至少在做三件事：

- 分析 AOT 泛型桥接需求
- 分析 reverse P/Invoke、`calli`、`pinvoke`
- 生成 `MethodBridge.cpp` 并清理 `Il2cppBuildCache`

为什么生成完还要清缓存？

因为这份文件不是“报告”，而是会被后续 il2cpp 构建真正编进去的 generated C++。

如果不清 `Il2cppBuildCache`，Unity 后续可能继续复用旧的 build cache。

再对照 runtime 侧，`InterpreterModule::Initialize()` 会显式调用 `InitMethodBridge()` 去消费这些生成出来的 stub 表。也就是说：

`MethodBridge.cpp` 是 build-time 生成、runtime 真消费的产物。`

### MethodBridge 在 runtime 里怎么被消费——以及缺失时发生了什么

理解"MethodBridge 是真消费的产物"之后，有一个更具体的问题值得追：runtime 是在哪个时刻发现桥接缺失的，发现之后又做了什么。

先从生成物入手。`MethodBridge.cpp` 里有大量类似这样的函数：

```cpp
// 一个典型的 bridge stub（示意，实际函数名由签名 hash 决定）
static void __M2NMethod_i4(const MethodInfo* method, uint16_t* argVarIndexs, StackObject* localVarBase, void** outRetVal)
{
    // 从解释器栈帧里取参数，按 ABI 重排，然后调用 AOT 函数
    typedef int32_t (*Fn)(int32_t, const MethodInfo*);
    int32_t arg0 = *(int32_t*)(localVarBase + argVarIndexs[0]);
    *((int32_t*)outRetVal) = ((Fn)(method->methodPointer))(arg0, method);
}
```

每个 stub 函数都对应一种方法签名——参数类型组合和返回类型组合各不同，就是一个不同的 stub。

这些 stub 函数在 `InitMethodBridge()` 里被批量注册进一张哈希表。哈希表的 key 是方法签名（经过规范化处理），value 是对应的 stub 函数指针。

**调用发生时的查表逻辑：**

HybridCLR 解释器在执行热更代码里的某个 delegate 调用或接口方法调用时，需要跨越"解释器 → AOT"边界。这时它会：

1. 拿到目标方法的签名
2. 用签名在桥接表里查对应的 stub
3. 找到 → 调用 stub，stub 负责参数重排和 ABI 适配
4. 找不到 → 调用 `MethodBridge_NotSupport`

`MethodBridge_NotSupport` 不是一个断言或 abort，而是一个会被直接"执行"的占位 stub：

```cpp
// 概念示意
static void MethodBridge_NotSupport(...)
{
    // 通常会抛出一个可被托管层捕获的异常
    // 而不是直接 abort()
    il2cpp::vm::Exception::Raise(
        il2cpp::vm::Exception::GetNotSupportedException(
            "method call bridge missing"));
}
```

所以缺少 MethodBridge 时，看到的通常是：

```
E Unity: NotSupportedException: method call bridge missing: ...SignatureString...
  at SomeDelegate (...)
```

而不是 SIGSEGV。这一点和 AOT 泛型缺失的崩溃表现（SIGSEGV 栈溢出）截然不同。

但在某些签名匹配逻辑有缺陷或桥接表本身损坏的情况下，也可能表现为 SIGABRT 或 SIGILL，取决于 HybridCLR 版本和具体调用路径。

**实际项目里 MethodBridge 缺失的常见触发点：**

- 带值类型参数的 delegate（值类型泛型参数组合爆炸，生成器可能漏掉）
- 通过 Func / Action 持有的热更方法，参数签名是 AOT 侧没见过的组合
- 反射调用带特定签名的方法（`MethodInfo.Invoke` 内部也走桥接逻辑）

每次热更代码新增了新的参数签名组合，就需要重新执行 `Generate/All` 重新生成桥接表，否则桥接表只包含上一次分析时见过的签名。

## `AOTGenericReference`：把泛型风险显式列出来，而不是自动修好

`AOTReferenceGeneratorCommand.cs` 的主线是：

```csharp
AssemblyReferenceDeepCollector collector =
    new AssemblyReferenceDeepCollector(
        MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames),
        hotUpdateDllNames);

var analyzer = new Analyzer(...);
analyzer.Run();
writer.Write(analyzer.AotGenericTypes.ToList(), analyzer.AotGenericMethods.ToList(), ...);
```

这一步不是在改 runtime，也不是在改 `libil2cpp`。

它是在分析：

`热更代码到底触发到了哪些 AOT 泛型类型和泛型方法实例。`

更关键的是生成物本身。  
从 `GenericReferenceWriter.cs` 看，输出文件默认长这样：

```csharp
codes.Add("public class AOTGenericReferences : UnityEngine.MonoBehaviour");
...
codes.Add("\t// {{ AOT generic types");
foreach(var typeName in typeNames)
{
    codes.Add($"\t// {typeName}");
}
...
foreach(var method in methodTypeAndNames)
{
    codes.Add($"\t\t// {PrettifyMethodSig(method.Item3)}");
}
```

注意这个生成物的主内容其实是注释清单。

所以我一直觉得，这一步的正确理解不是“自动修复泛型”，而是：

`把真正的泛型风险点整理成一份工程上可见的清单。`

它告诉你哪里可能要显式保实例、显式做引用、显式做 AOT 侧锚定。

## 为什么这条顺序不能乱

到这里，整条链其实已经很清楚了：

1. `Installer`  
   准备本地 HybridCLR 版 `libil2cpp`
2. `CompileDll`  
   准备热更 DLL 输入
3. `Il2CppDef`  
   把当前工程信息同步到本地 `libil2cpp/generated`
4. `LinkXml`  
   用热更 DLL 暴露裁剪依赖
5. `AOTDlls`  
   拿裁剪后 AOT 快照
6. `MethodBridge`  
   基于快照和热更 DLL 生成 runtime 会真正消费的 bridge
7. `AOTGenericReference`  
   输出泛型风险清单

这条顺序里有几处依赖是硬依赖：

- `LinkXml` 依赖热更 DLL
- `MethodBridge` 依赖裁剪后的 AOT DLL
- `AOTGenericReference` 依赖热更 DLL，有时也会拿 AOT DLL 做差量分析
- `CheckSettings` 会在真正打包前校验本地 `libil2cpp` 和 `MethodBridge.cpp` 状态

所以 `Generate/All` 不是“偷懒入口”，而是：

`把这几段硬依赖按正确顺序串起来执行。`

## 把这条工具链压成一句话

如果把这篇文章压成一句话，我会这样描述 HybridCLR 的 build-time 工具链：

`它先把改造后的 libil2cpp 安装到项目本地，再围绕当前 BuildTarget 产出热更 DLL、generated 版本适配文件、防裁剪信息、裁剪后的 AOT 快照、桥接 C++ 代码和泛型风险清单，最后在正式构建前通过 UNITY_IL2CPP_PATH 把 Unity 的打包链路接到这套本地产物上。`

我觉得这句话比“点一下 Generate/All”更接近源码里的真实结构。

## 常见误解

### 误解一：HybridCLR 菜单只是编辑器辅助，不属于原理

不对。

这些按钮的意义不是“方便”，而是 build-time 在为 runtime 准备真正输入。

### 误解二：`Installer` 只是下载仓库

不对。

它真正做的是本地 `libil2cpp` 的准备与替换，并通过 `UNITY_IL2CPP_PATH` 接进正式构建。

### 误解三：`AOTDlls` 是在生成热更 DLL 的 AOT 版本

不对。

它是在拿裁剪后的 AOT 世界快照，供桥接和泛型分析使用。

### 误解四：`AOTGenericReference` 是自动修复器

也不对。

从生成器实现看，它默认更像风险清单，而不是自动补救。

### 误解五：MethodBridge 只是性能优化，缺了最多是慢一点

不对。

它是 build-time 生成、runtime 真消费的桥接代码，缺它不是慢一点，而是很多跨边界调用根本接不上。

更具体地说，MethodBridge 缺失时，常见表现不是 AOT 泛型缺失那种 `SIGSEGV` / 栈溢出，而是运行时直接报：

`NotSupportedException: method call bridge missing: ...SignatureString...`

也就是说：

- 看到 `NotSupportedException`，优先查 MethodBridge 是否没生成或没带进包
- 看到 `IlCppFullySharedGenericAny`、重复栈帧、`SIGSEGV`，再优先查 AOT 泛型实例化或补充 metadata 链路

两者都会表现成“HybridCLR 跑不起来”，但它们坏的层完全不同，修法也完全不同。

## 最后一句

如果前两篇在讲：

- runtime 主链怎么跑
- AOT 泛型问题为什么会出现

那么这一篇其实是在讲：

`为什么 HybridCLR 必须先有一条足够完整的 build-time 流水线，runtime 那条链才有可能成立。`

三篇合起来，才比较接近 HybridCLR 在工程里的完整样子。

## 系列位置

- 上一篇：<a href="{{< relref "engine-notes/hybridclr-aot-generics-and-supplementary-metadata.md" >}}">HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑</a>
- 下一篇：<a href="{{< relref "engine-notes/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}}">HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化</a>
