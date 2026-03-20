+++
title = "HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute"
description = "沿着 RuntimeApi -> metadata -> transform -> execute 的真实调用链，拆解 HybridCLR 在 IL2CPP 上如何把热更新代码跑起来。"
weight = 30
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "Runtime"]
+++

> HybridCLR 不是“把 DLL load 进来就能跑”，而是在一个原本偏静态、偏 AOT 的 IL2CPP runtime 上，补了一整条从 metadata 装载到解释执行的运行时链路。

这是 HybridCLR 系列第 1 篇，先把总地图和 runtime 主链立住。
本文只把工具链、AOT 泛型、MethodBridge 压到够建立地图的程度；更细的边界留给后面的专题篇。

如果只看功能描述，HybridCLR 很容易被理解成“IL2CPP 上的 C# 热更新方案”。

这句话当然没错，但它太粗了。粗到会把最关键的边界一起抹平。

因为只要你真的打开源码，就会发现 HybridCLR 做的事情根本不是单点魔法。它至少同时覆盖了三层问题：

- 构建前，先生成运行时真正需要的那些输入
- 运行时，把热更程序集和补充 metadata 正式接进 IL2CPP
- 执行时，把方法体取出来、转成内部指令、再交给解释器跑

如果这三层不分开，读源码的时候就会很容易把 `metadata`、`transform`、`interpreter`、`MethodBridge`、`AOTGenericReference` 看成几摊散点。

这篇文章的目标，就是把它们重新串回一条完整链路。

## 这篇要回答什么

如果把问题压成几句最核心的话，这篇文章要回答的是：

1. `IL2CPP` 明明是 AOT，为什么还能跑热更代码。
2. `HybridCLR` 到底往 `libil2cpp` 里补了哪些能力。
3. Editor 菜单里那几个生成步骤，到底在为 runtime 准备什么。
4. 一个热更方法，最终是怎么一路走到 `Interpreter::Execute` 的。

这篇不是接入教程，也不是源码目录索引。

它更像一篇“源码导读版总论”。目标不是把所有文件列出来，而是建立一张够用的因果地图，让读者可以顺着文章自己进源码。

## 先给一段最小 IL2CPP 背景

如果不先把 IL2CPP 的坐标立住，后面很容易把 HybridCLR 理解成“另起炉灶”。

但它其实不是。

Unity 托管代码的大致路径可以先压成一句话：

`C# -> IL -> il2cpp 转换 -> C++ -> native binary`

这句话的重点不在“会转成 C++”，而在于它背后的运行时假设：

- 运行时里要执行的方法，理想状态下已经提前 AOT 成 native 代码
- 类型、方法、metadata 的主体结构，理想状态下在构建时就已经基本确定
- 默认运行时并不是一个可以继续接收新 IL、再现场 JIT 执行的 CLR

所以 IL2CPP 的问题从来不是“看不懂 DLL 文件”，而是它原本并不打算在运行时天然支持这样一条链路：

`新程序集进来 -> 建立运行时 metadata 映射 -> 找到 method body -> 现场把它跑起来`

HybridCLR 干的，恰恰就是把这条链补出来。

因此本文里的 IL2CPP 背景只服务于一个目的：

`让读者先知道，HybridCLR 不是绕开 IL2CPP，而是补进 IL2CPP。`

## 为什么这个问题值得先搞清楚

因为如果没有这张地图，HybridCLR 的源码会非常容易看乱。

你会看到：

- `Packages/HybridCLR/Editor` 在生成一堆文件
- `RuntimeApi.cs` 提供了几个 C# API
- `metadata` 目录里全是 image、assembly、token、method body
- `transform` 目录看起来像在改写 IL
- `interpreter` 目录里又是一套执行器和桥接表

但如果不先建立因果关系，这些东西很容易看起来互相平行。

我对这套源码的一个基本判断是：

`HybridCLR 不是一个解释器，而是一套 build-time 和 runtime 共同完成的系统链路。`

所以这篇文章不会按目录平铺，而只追一条主线：

`C# RuntimeApi -> native RuntimeApi -> metadata::Assembly -> MetadataModule / MethodBodyCache -> HiTransform -> Interpreter::Execute`

支线只在必要时插入：

- Editor 工具链
- AOT 泛型
- MethodBridge / ReversePInvokeWrapper

## 先给源码地图

本文基于这个工程里的源码：

`E:\HT\Projects\DP\TopHeroUnity`

如果你准备边看边跟源码，建议先把这三层边界立住。

### 第一层：Editor 工具链

目录在：

`E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Editor`

这一层负责生成运行时要消费的输入，比如 `link.xml`、裁剪后的 AOT DLL、`MethodBridge.cpp`、`AOTGenericReferences.cs`。

### 第二层：C# 入口

目录在：

`E:\HT\Projects\DP\TopHeroUnity\Packages\HybridCLR\Runtime`

这里最重要的文件是：

- `RuntimeApi.cs`
- `HomologousImageMode.cs`

这一层负责给业务代码暴露 C# API，但真正干活的不是 C#，而是下面的 native runtime。

### 第三层：真正的 runtime

目录在：

`E:\HT\Projects\DP\TopHeroUnity\HybridCLRData\LocalIl2CppData-WindowsEditor\il2cpp\libil2cpp\hybridclr`

这里才是本文真正的主战场，后文会主要追这几处：

- `Runtime.cpp`
- `RuntimeApi.cpp`
- `metadata/Assembly.cpp`
- `metadata/MetadataModule.cpp`
- `metadata/MethodBodyCache.cpp`
- `transform/Transform.cpp`
- `interpreter/InterpreterModule.cpp`
- `interpreter/Interpreter_Execute.cpp`

如果你准备跟着断点读，我建议顺序就是上面这 8 个文件。

## 先看 Editor 工具链：运行时到底提前需要哪些输入

很多人第一次看到 HybridCLR 菜单，会把它理解成“编辑器上的一组便利按钮”。

但如果你直接看 `PrebuildCommand.cs`，你会发现它本质上是在按固定依赖顺序准备 runtime 的输入。

```csharp
[MenuItem("HybridCLR/Generate/All", priority = 200)]
public static void GenerateAll()
{
    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);
    Il2CppDefGeneratorCommand.GenerateIl2CppDef();
    LinkGeneratorCommand.GenerateLinkXml(target);
    StripAOTDllCommand.GenerateStripedAOTDlls(target);
    MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
    AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
}
```

这段代码对应的不是“功能列表”，而是一条依赖链。

在总论里，这些按钮不需要逐个展开到源码细节。先记住它们分别在准备 4 类输入就够了：

- `Installer` 和 `Il2CppDef` 负责把当前工程和本地 `libil2cpp` 对齐
- `CompileDll`、`LinkXml` 负责把热更程序集和裁剪可见性准备好
- `AOTDlls` 负责拿到“最终裁剪后的 AOT 世界是什么样”
- `MethodBridge`、`AOTGenericReference` 分别补 ABI 边界和泛型风险显式化

这一层在系列里的任务，只是先把 build-time 因果关系立住。  
每个按钮到底生成什么、被谁消费、少了会怎样，后面的工具链篇再单独展开。

### 这一层到底解决了什么问题

如果把这一节压成一句话，就是：

`HybridCLR 菜单不是在做杂活，而是在提前生成 runtime 真正要吃的输入。`

少了它们，后面的 runtime 不是“性能差一点”，而是整个链条根本搭不起来。

顺便说一句，如果你自己的项目菜单里还有 `Define Symbols`、`BuildAssets And CopyTo AssemblyTextAssetPath`、`AOT引用热更检查` 这类入口，那通常是项目自己的封装，不是 HybridCLR package runtime 原理本身。本文后面只按 package 主线讲。

## HybridCLR 是怎么挂进 libil2cpp 的

真正进入 runtime 主线以后，第一站应该看 `Runtime.cpp`。

```cpp
void Runtime::Initialize()
{
    RuntimeApi::RegisterInternalCalls();
    metadata::MetadataModule::Initialize();
    interpreter::InterpreterModule::Initialize();
    transform::TransformModule::Initialize();
}
```

这段代码非常短，但信息密度很高。

第一，它说明 HybridCLR 并不是一个“外部解释器”，而是直接挂进 `libil2cpp` 的一组 runtime 模块。

第二，它说明这套 runtime 至少有四个启动点：

- `RuntimeApi`：把 C# 对外 API 接进 internal call
- `MetadataModule`：初始化 metadata 相关基础设施
- `InterpreterModule`：初始化解释器和桥接表
- `TransformModule`：初始化 transform 侧的运行环境

这也是为什么我一直觉得，读 HybridCLR 不能从 `Interpreter_Execute.cpp` 直接开始。

因为如果你不先看到 `Runtime::Initialize()`，就会下意识把它理解成“解释器实现细节”。

但实际上，它首先是一套被接进 IL2CPP runtime 的系统模块。

## C# 的 RuntimeApi 调到 native 后，语义到底是什么

这条主线的第二站应该看 `RuntimeApi.cs` 和 `RuntimeApi.cpp`。

先看 C# 侧：

```csharp
#if UNITY_EDITOR
public static LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode)
{
    return LoadImageErrorCode.OK;
}
#else
[MethodImpl(MethodImplOptions.InternalCall)]
public static extern LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode);
#endif
```

`PreJitMethod` 和 `PreJitClass` 也是同样的模式。

这件事的含义非常明确：

- 在 Editor 里，这些 API 只是占位
- 真正的 runtime 语义只存在于 player 环境里的 internal call

再看 native 侧：

```cpp
void RuntimeApi::RegisterInternalCalls()
{
    il2cpp::vm::InternalCalls::Add("HybridCLR.RuntimeApi::LoadMetadataForAOTAssembly(...)", (Il2CppMethodPointer)LoadMetadataForAOTAssembly);
    il2cpp::vm::InternalCalls::Add("HybridCLR.RuntimeApi::PreJitClass(System.Type)", (Il2CppMethodPointer)PreJitClass);
    il2cpp::vm::InternalCalls::Add("HybridCLR.RuntimeApi::PreJitMethod(System.Reflection.MethodInfo)", (Il2CppMethodPointer)PreJitMethod);
}
```

这一步只是在做绑定。

真正重要的是下面这句：

```cpp
int32_t RuntimeApi::LoadMetadataForAOTAssembly(Il2CppArray* dllBytes, int32_t mode)
{
    return (int32_t)hybridclr::metadata::Assembly::LoadMetadataForAOTAssembly(
        il2cpp::vm::Array::GetFirstElementAddress(dllBytes),
        il2cpp::vm::Array::GetByteLength(dllBytes),
        (hybridclr::metadata::HomologousImageMode)mode);
}
```

这句代码非常值得停一下。

因为它说明 `LoadMetadataForAOTAssembly` 的语义不是“执行热更 DLL”，而是把字节数组交给 `metadata::Assembly::LoadMetadataForAOTAssembly`。

也就是说，它首先在解决的是 metadata 问题，不是代码执行问题。

### `PreJitMethod` 真正在做什么

很多人第一次看到 `PreJitMethod`，会下意识把它理解成“提前 JIT”。

但 HybridCLR 这里并不是把方法 JIT 成 native 代码，而是在提前做 transform 和缓存。

从 `RuntimeApi.cpp` 可以直接看出来：

```cpp
int32_t PreJitMethod0(const MethodInfo* methodInfo)
{
    if (!methodInfo->isInterpterImpl)
    {
        return false;
    }
    ...
    return interpreter::InterpreterModule::GetInterpMethodInfo(methodInfo) != nullptr;
}
```

这里真正干活的是 `InterpreterModule::GetInterpMethodInfo`。

它的意思更接近：

`提前把解释执行需要的 InterpMethodInfo 准备好，避免首帧第一次跑时现场做 transform。`

所以这套 API 的正确理解应该是：

- `LoadMetadataForAOTAssembly`：补 AOT 同源 metadata
- `PreJitMethod / PreJitClass`：预热解释器方法的 transform 结果

## 热更程序集和补充 metadata，在 runtime 里是怎么落位的

这一节是整篇文章最重要的基础层。

因为解释器最终执行什么，不是由 `Execute` 决定的，而是由 metadata 层先把“它到底是什么方法、方法体在哪、该用哪份 image”这些事情定下来。

### 第一条线：热更程序集本身怎么进来

如果顺着 `Assembly.Load(byte[])` 往下找，会在 `AppDomain.cpp` 里看到：

```cpp
const Il2CppAssembly* assembly = il2cpp::vm::MetadataCache::LoadAssemblyFromBytes(
    il2cpp::vm::Array::GetFirstElementAddress(rawAssembly),
    il2cpp::vm::Array::GetByteLength(rawAssembly),
    rawSymbolStoreBytes,
    rawSymbolStoreLength);
```

继续往下看 `MetadataCache.cpp`：

```cpp
const Il2CppAssembly* il2cpp::vm::MetadataCache::LoadAssemblyFromBytes(...)
{
    Il2CppAssembly* newAssembly =
        hybridclr::metadata::Assembly::LoadFromBytes(assemblyBytes, length, rawSymbolStoreBytes, rawSymbolStoreLength);
    return newAssembly;
}
```

这条线把热更程序集正式引到了 `hybridclr/metadata/Assembly.cpp`。

再看真正干活的 `LoadFromBytes` 和 `Create`：

```cpp
Il2CppAssembly* Assembly::LoadFromBytes(const void* assemblyData, uint64_t length, ...)
{
    Il2CppAssembly* ass = Create((const byte*)assemblyData, length, ...);
    RunModuleInitializer(ass->image);
    return ass;
}

Il2CppAssembly* Assembly::Create(const byte* assemblyData, uint64_t length, ...)
{
    uint32_t imageId = InterpreterImage::AllocImageIndex((uint32_t)length);
    InterpreterImage* image = new InterpreterImage(imageId);
    ...
    image->Load(assemblyData, (size_t)length);
    ...
    image->InitRuntimeMetadatas();
    il2cpp::vm::MetadataCache::RegisterInterpreterAssembly(ass);
    return ass;
}
```

这一段代码的语义非常清楚：

1. 先给这份程序集分配一个 `InterpreterImage`
2. 把 DLL 字节和可选 PDB 真的载入这份 image
3. 基于 raw image 构建 `Il2CppAssembly` 和 `Il2CppImage`
4. 初始化 runtime metadata
5. 把这份 assembly 正式注册进 `MetadataCache`

这里最重要的点不是“加载文件”，而是：

`热更程序集在 runtime 里被包装成了一个可以参与后续 metadata 解析与执行的 InterpreterImage。`

### 第二条线：补充 metadata 怎么进来

和热更程序集不同，`LoadMetadataForAOTAssembly` 不会创建新的热更 assembly。  
它的语义更接近：

`给已有 AOT 程序集挂一份可查询的同源 metadata image。`

这篇只需要先把这个判断立住。  
至于 `Consistent / SuperSet`、`AOTHomologousImage` 注册表，以及为什么“看得懂 metadata”不等于“native 实现一定存在”，后面的 AOT 泛型篇再展开。

## MetadataModule 到底解决了什么问题

如果说 `Assembly.cpp` 在做“装载”，那 `MetadataModule.cpp` 做的就是“统一入口”。

先看初始化：

```cpp
void MetadataModule::Initialize()
{
    MetadataPool::Initialize();
    InterpreterImage::Initialize();
    Assembly::InitializePlaceHolderAssemblies();
}
```

这里除了初始化池和 `InterpreterImage` 之外，还有一个很容易被忽略的点：`InitializePlaceHolderAssemblies()`。

它会提前注册一批 placeholder assembly：

```cpp
void Assembly::InitializePlaceHolderAssemblies()
{
    for (const char** ptrPlaceHolderName = g_placeHolderAssemblies; *ptrPlaceHolderName; ++ptrPlaceHolderName)
    {
        Il2CppAssembly* placeHolderAss = CreatePlaceHolderAssembly(nameWithExtension);
        il2cpp::vm::MetadataCache::RegisterInterpreterAssembly(placeHolderAss);
    }
}
```

这一步的意义可以粗略理解成：先把未来会被真正热更 DLL 填充的 assembly 槽位注册进 metadata 世界。

但这一层更关键的函数其实是 `GetUnderlyingInterpreterImage`：

```cpp
Image* MetadataModule::GetUnderlyingInterpreterImage(const MethodInfo* methodInfo)
{
    return metadata::IsInterpreterMethod(methodInfo)
        ? hybridclr::metadata::MetadataModule::GetImage(methodInfo->klass)
        : (metadata::Image*)hybridclr::metadata::AOTHomologousImage::FindImageByAssembly(
            methodInfo->klass->rank ? il2cpp_defaults.corlib->assembly : methodInfo->klass->image->assembly);
}
```

这段代码是全链路里一个非常关键的“分流点”。

它回答的是：

`当后面有人拿着一个 MethodInfo 来问“对应的 metadata image 在哪”时，到底该去热更 InterpreterImage 里找，还是去 AOT 同源 image 里找？`

也就是说，从这一层开始，后面的 `MethodBodyCache`、`Transform`、`Interpreter` 就不用再关心“这份 metadata 到底来自热更程序集还是补充 AOT image”。

它们只需要认 `Image*` 这一层统一抽象。

这正是 `MetadataModule` 的真正价值。

## 一个 MethodInfo 怎么找到 MethodBody

到这里为止，runtime 已经知道了：

- 方法对应哪份 image
- 这份 image 是热更 image 还是 AOT 同源 image

下一步的问题就变成了：

`怎么从 MethodInfo 找到真正的方法体？`

这一层主要看 `MethodBodyCache.cpp`。

```cpp
static MethodBodyCacheInfo* GetOrInitMethodBodyCache(hybridclr::metadata::Image* image, uint32_t token)
{
    ImageTokenPair key = { image, token };
    auto it = s_methodBodyCache.find(key);
    if (it != s_methodBodyCache.end())
    {
        return it->second;
    }
    MethodBody* methodBody = image->GetMethodBody(token);
    ...
    s_methodBodyCache[key] = ci;
    return ci;
}

MethodBody* MethodBodyCache::GetMethodBody(hybridclr::metadata::Image* image, uint32_t token)
{
    MethodBodyCacheInfo* ci = GetOrInitMethodBodyCache(image, token);
    ci->accessVersion = s_methodBodyCacheVersion;
    ++ci->accessCount;
    return ci->methodBody;
}
```

这层设计我觉得非常合理，因为它刚好卡在 CLI metadata 的天然边界上。

如果对照 ECMA-335，一个方法定义最终会对应到 `MethodDef` 及其 method body 信息。method body 的物理布局则在 `II.25.4 Common Intermediate Language physical layout` 里定义，包括：

- header
- IL code
- locals signature
- EH table

HybridCLR 在 runtime 里要拿 method body，最自然的键就是：

`image + token`

因为：

- `token` 决定“这是谁”
- `image` 决定“这份 token 应该在哪个 metadata 空间里解释”

如果没有 `image`，同一个 token 在不同程序集里当然可能根本不是同一个方法。

这也是为什么 `MethodBodyCache` 不是只按 token 缓存，而是明确按 `(image, token)` 缓存。

### 这一层到底解决了什么问题

它解决的是：

`把“逻辑上的方法”稳定地映射成“可以被 transform 消费的 MethodBody”。`

没有这一步，后面的解释器根本还没有输入。

## 为什么 HybridCLR 不直接解释 IL，而要先 Transform

很多人第一次看 HybridCLR，会先入为主地以为它是“直接解释 IL”。

但只要看 `Transform.cpp`，这个理解就会立刻被纠正。

```cpp
InterpMethodInfo* HiTransform::Transform(const MethodInfo* methodInfo)
{
    metadata::Image* image = metadata::MetadataModule::GetUnderlyingInterpreterImage(methodInfo);
    metadata::MethodBodyCache::EnableShrinkMethodBodyCache(false);
    metadata::MethodBody* methodBody = metadata::MethodBodyCache::GetMethodBody(image, methodInfo->token);
    ...
    TransformContext ctx(image, methodInfo, *methodBody, pool, resolveDatas);
    ctx.TransformBody(0, 0, *result);
    metadata::MethodBodyCache::EnableShrinkMethodBodyCache(true);
    return result;
}
```

这里的控制流已经很清楚了：

1. 先拿到底层 `Image`
2. 再按 `token` 取 `MethodBody`
3. 然后构造 `TransformContext`
4. 最后把 `MethodBody` 改写成 `InterpMethodInfo`

这说明 HybridCLR 不是“直接拿原始 CIL 一条条解释”，而是先做了一层中间转换。

为什么一定要这样做？

因为原始 CIL 的核心模型是栈机。

栈机的优点是表达紧凑，缺点是如果执行器每一步都严格按栈语义实时还原，运行期开销会比较重，而且很多局部分析也更难做。

所以 HybridCLR 的路线不是“直接解释原始 IL”，而是：

`先把 MethodBody 变成更适合自己执行器消费的内部表示，再进入 execute。`

如果你继续往下看 `TransformContext.h`，会发现它不是一个“轻薄上下文”，而是整个转换过程的状态容器。它同时拿着：

- 当前 `Image`
- 当前 `MethodInfo`
- 当前 `MethodBody`
- 中间解析状态
- resolve 结果

这就意味着 transform 在 HybridCLR 里不是“预处理小功能”，而是 runtime 正式链路里的一层。

### 这一层到底解决了什么问题

它解决的是：

`把 CLI 栈机方法体，改写成 HybridCLR 自己的可执行方法表示。`

没有这一步，`Interpreter::Execute` 拿到的将只是原始 IL，而不是它自己能高效 dispatch 的内部指令。

## `Interpreter::Execute` 到底怎么跑起来

真正进入执行阶段以后，最值得先看的不是 `Execute` 本身，而是 `InterpreterModule::GetInterpMethodInfo`。

```cpp
InterpMethodInfo* InterpreterModule::GetInterpMethodInfo(const MethodInfo* methodInfo)
{
    il2cpp::os::FastAutoLock lock(&il2cpp::vm::g_MetadataLock);

    if (methodInfo->interpData)
    {
        return (InterpMethodInfo*)methodInfo->interpData;
    }
    ...
    InterpMethodInfo* imi = transform::HiTransform::Transform(methodInfo);
    il2cpp::os::Atomic::FullMemoryBarrier();
    const_cast<MethodInfo*>(methodInfo)->interpData = imi;
    return imi;
}
```

这段代码等于把前面几节的结论压实了。

它的含义是：

- 解释器方法第一次真正需要执行时，先检查 `MethodInfo` 上有没有已经缓存好的 `interpData`
- 如果没有，就现场跑 `HiTransform::Transform`
- 然后把产物挂回 `methodInfo->interpData`

这也是为什么前面说 `PreJitMethod` 的语义其实是“提前触发 transform 并缓存结果”。

### 真正的执行循环

接着看 `Interpreter_Execute.cpp`：

```cpp
void Interpreter::Execute(const MethodInfo* methodInfo, StackObject* args, void* ret)
{
    MachineState& machine = InterpreterModule::GetCurrentThreadMachineState();
    InterpFrameGroup interpFrameGroup(machine);
    ...
    PREPARE_NEW_FRAME_FROM_NATIVE(methodInfo, args, ret);

LoopStart:
    try
    {
        for (;;)
        {
            switch (*(HiOpcodeEnum*)ip)
            {
            case HiOpcodeEnum::None:
            {
                continue;
            }
            ...
```

有两点非常关键。

第一，`Execute` 不是直接在跑原始 CIL opcode，而是在跑 `HiOpcodeEnum`。

这和上一节完全对上了：transform 的产物已经不是原始 IL，而是 HybridCLR 自己的高层解释指令。

第二，`Execute` 先做的是 frame 和 machine state 的准备，而不是“先解析 IL”。

也就是说，到进入 `switch` dispatch loop 的这一刻，解释器需要的结构已经都准备完了：

- 当前方法对应的 `InterpMethodInfo`
- 当前执行帧
- 参数布局
- 返回值位置
- 指令指针 `ip`

这也是为什么我认为 HybridCLR 的主链必须写成：

`metadata -> method body -> transform -> execute`

而不是简单写成“它有个解释器”。

### 这一层到底解决了什么问题

它解决的是：

`把前面已经准备好的 InterpMethodInfo 真正按执行帧、参数和内部指令调度跑起来。`

这一步才是热更代码“真的开始执行”的那一刻。

## AOT 泛型和 MethodBridge，为什么不是附加优化，而是必要能力

到这里，主链已经走完了。  
但如果只讲到 `Interpreter::Execute` 就停，读者很容易误以为“剩下的都只是附加优化”。

地图层面其实还要记住两件事：

- `AOTGenericReference` 关注的是“热更代码到底触发到了哪些 AOT 泛型实例”
- `MethodBridge` 关注的是“这些调用跨 interpreter / AOT / native 边界时，ABI 怎么接起来”

它们都很重要，但都不是这篇的主线。  
总论里只需要知道：HybridCLR 不是只补解释器，它还补了泛型风险显式化和 ABI 边界。更细的边界分别放到 AOT 泛型篇和工具链篇里讲。

## 把整条链压成一句话

如果把全文压成一条尽量精确的链路，我会这样描述 HybridCLR：

`构建期先生成热更 DLL、防裁剪信息、裁剪后的 AOT 快照和 bridge；运行时再把热更程序集装成 InterpreterImage，把 AOT 补充 metadata 装成 AOTHomologousImage，随后按 image + token 取 MethodBody，经 HiTransform 产出 InterpMethodInfo，最后由 Interpreter::Execute 解释执行。`

我觉得这句话比“HybridCLR 是 IL2CPP 热更新方案”更接近它在源码里的真实位置。

## 常见误解

### 误解一：HybridCLR 本质上只是一个解释器

不对。

解释器只是最后一层。

在它之前，至少还有：

- runtime 装载与注册
- metadata image 管理
- method body 获取
- transform
- bridge 初始化

如果把它简化成“一个解释器”，很多关键问题都会被误判。

### 误解二：`LoadMetadataForAOTAssembly` 就是在加载热更 DLL

不对。

从 `RuntimeApi.cpp -> Assembly.cpp` 的调用链可以很清楚地看到，它的语义是给已有 AOT 程序集补一份同源 metadata image，而不是把那份 DLL 当热更程序集执行。

热更程序集自身的装载主线，是 `AppDomain.cpp -> MetadataCache::LoadAssemblyFromBytes -> Assembly::LoadFromBytes`。

### 误解三：`PreJitMethod` 真的是“提前 JIT 成 native 代码”

也不对。

至少从当前源码看，它最终是通过 `InterpreterModule::GetInterpMethodInfo` 触发 transform 并缓存 `interpData`。

它预热的是解释器方法的准备结果，不是把方法再编译成一份新的 native 代码。

### 误解四：MethodBridge 只是优化，没有它也能正常工作

不对。

MethodBridge 的位置不是“锦上添花”，而是 interpreter / AOT / native 三个世界之间的 ABI 桥。

没有它，很多跨边界调用不是慢一点，而是语义根本接不上。

## 最后一句

如果你准备继续往下深挖，我建议下一步不要立刻去啃 `TransformContext.cpp`。

更好的顺序是：

1. 再把 `Assembly.cpp` 和 `MetadataModule.cpp` 重读一遍
2. 然后带着一个具体方法，追 `MethodBodyCache -> Transform -> GetInterpMethodInfo -> Execute`
3. 最后再去看 `AOTGenericReference` 和 `MethodBridge` 这两条支线

因为对 HybridCLR 来说，真正的难点从来不是“有没有解释器”，而是：

`它是怎么把一个原本偏静态、偏 AOT 的 IL2CPP runtime，扩成一套真的能接收新 metadata、解析方法体并执行的系统。`

## 系列位置

- 上一篇：无。这是系列起点。
- 下一篇：[HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑](hybridclr-aot-generics-and-supplementary-metadata.md)
