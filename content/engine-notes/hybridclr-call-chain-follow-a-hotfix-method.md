+++
date = 2026-03-20
title = "HybridCLR 调用链实战｜跟着一个热更方法一路走到 Interpreter::Execute"
description = "从 Assembly.Load(byte[])、MethodInfo.Invoke、MetadataCache、InterpreterInvoke 到 HiTransform 与 Interpreter::Execute，沿着一条真实调用链把 HybridCLR 再跑一遍。"
weight = 34
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "Runtime", "SourceCode"]
series = "HybridCLR"
+++

> 如果说前几篇是在拆 HybridCLR 的零件，那这一篇要做的，就是沿着一条真实调用链，把这些零件重新装回一台会跑的机器。

这是 HybridCLR 系列第 5 篇，用一条真实调用链把前面几篇重新串起来。

这里默认前几篇的术语已经建立，所以不再重讲补充 metadata、菜单生成和资源挂载的细节，而只追方法调用主链。

但只拆零件还不够。  
真正到读源码时，很多人还是会卡在一个更实际的问题上：

`如果业务代码里真的调用了一个热更方法，它到底是怎么一路走进解释器的？`

这篇就专门回答这个问题。

它不是再讲一遍“HybridCLR 原理综述”，而是把一条完整调用链重新走一遍。  
目标也很明确：

`让读者可以跟着这篇文章进源码，知道每个关键转折点为什么会发生。`

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 业务代码里的 `Assembly.Load(byte[])`，最后是怎么接进 HybridCLR runtime 的。
2. 一个热更程序集里的方法，什么时候会被标记成“由 interpreter 实现”。
3. `MethodInfo.Invoke` 最终为什么会掉到 `InterpreterInvoke`，而不是普通 AOT invoker。
4. `Interpreter::Execute` 在第一次执行前，为什么还要先经过一次 transform。
5. 这一整条链里，哪些动作发生在装载时，哪些动作是第一次调用时才懒执行的。

## 先固定一个足够真实的入口

为了避免整篇文章飘在抽象层，我们先固定一个真实入口。

在你这个项目里，热更程序集的装载和主逻辑入口调用在 `Assets/GameScripts/AOT/Procedure/ProcedureLoadAssembly.cs` 里。关键点只有两句：

```csharp
var assembly = Assembly.Load(textAsset.bytes);
entryMethod.Invoke(appType, objects);
```

第一句把热更 DLL 从字节数组加载进当前 `AppDomain`。  
第二句通过反射调用热更程序集里的入口方法。

如果把这两句再压缩一下，这篇文章真正追的就是这条链：

`Assembly.Load(byte[]) -> 获取 MethodInfo -> Invoke -> invoker_method -> InterpreterInvoke -> HiTransform::Transform -> Interpreter::Execute`

后文虽然会经常提 `GameApp.Entrance` 这个项目里的实际入口，但为了讲清主线，你也可以把它脑补成一个最简单的热更方法：

```csharp
public static int Add(int a, int b)
{
    return a + b;
}
```

因为对于 HybridCLR 来说，复杂业务逻辑不是重点。  
重点是：这个方法属于热更程序集，因此它不会走普通 AOT 方法那条路。

## 先给源码地图

这篇主要看这些文件：

- `Assets/GameScripts/AOT/Procedure/ProcedureLoadAssembly.cs`
- `libil2cpp/icalls/mscorlib/System/AppDomain.cpp`
- `libil2cpp/vm/MetadataCache.cpp`
- `libil2cpp/hybridclr/metadata/Assembly.cpp`
- `libil2cpp/vm/Class.cpp`
- `libil2cpp/hybridclr/metadata/MetadataModule.h`
- `libil2cpp/hybridclr/metadata/InterpreterImage.cpp`
- `libil2cpp/vm/Runtime.cpp`
- `libil2cpp/hybridclr/interpreter/InterpreterModule.cpp`
- `libil2cpp/hybridclr/transform/Transform.cpp`
- `libil2cpp/hybridclr/metadata/MethodBodyCache.cpp`
- `libil2cpp/hybridclr/interpreter/Interpreter_Execute.cpp`

如果只想跟这一篇断点，这些文件已经足够。

## 第一跳：`Assembly.Load(byte[])` 怎么接进 HybridCLR

这一跳如果不钉死，后面所有 runtime 讨论都会悬空。

在托管侧，我们看到的是：

```csharp
var assembly = Assembly.Load(textAsset.bytes);
```

但在 `libil2cpp` 里，真正接这条链的是 `AppDomain::LoadAssemblyRaw`：

```cpp
const Il2CppAssembly* assembly = il2cpp::vm::MetadataCache::LoadAssemblyFromBytes(
    il2cpp::vm::Array::GetFirstElementAddress(rawAssembly),
    il2cpp::vm::Array::GetByteLength(rawAssembly),
    rawSymbolStoreBytes,
    rawSymbolStoreLength);
```

这段代码在 `libil2cpp/icalls/mscorlib/System/AppDomain.cpp`。

关键点在下一跳。  
`MetadataCache::LoadAssemblyFromBytes` 并没有走原生 AOT assembly 注册逻辑，而是明确把它交给了 HybridCLR：

```cpp
Il2CppAssembly* newAssembly = hybridclr::metadata::Assembly::LoadFromBytes(
    assemblyBytes, length, rawSymbolStoreBytes, rawSymbolStoreLength);
```

也就是说，从“字节数组装载程序集”这个入口开始，HybridCLR 就已经接管了这件事。

## 第二跳：`Assembly::LoadFromBytes` 真正做了什么

`hybridclr/metadata/Assembly.cpp` 这一段非常关键，因为它决定了热更程序集进入 runtime 后到底长什么样。

它的外层入口很短：

```cpp
Il2CppAssembly* Assembly::LoadFromBytes(...)
{
    Il2CppAssembly* ass = Create(...);
    RunModuleInitializer(ass->image);
    return ass;
}
```

真正的主体在 `Create(...)`：

```cpp
uint32_t imageId = InterpreterImage::AllocImageIndex((uint32_t)length);
InterpreterImage* image = new InterpreterImage(imageId);
err = image->Load(assemblyData, (size_t)length);
...
image->InitBasic(image2);
image->BuildIl2CppAssembly(ass);
image->BuildIl2CppImage(image2);
image->InitRuntimeMetadatas();
il2cpp::vm::MetadataCache::RegisterInterpreterAssembly(ass);
```

这段代码至少做了 4 件事：

1. 给热更程序集分配一个 `InterpreterImage`。
2. 读取 DLL 里的 metadata 和 method body。
3. 把它包装成 `Il2CppAssembly` / `Il2CppImage`，让外部世界仍然通过标准 IL2CPP 结构访问它。
4. 把这个 assembly 注册回 `MetadataCache`。

这一步最容易被误解成“只是把 DLL 读进内存”。  
但它真实的语义更接近：

`把一个新的解释型程序集正式接进 IL2CPP 的 metadata 世界。`

也正因为这一步已经把热更程序集包装成 `InterpreterImage`，后面 token 查找、method invoker 分派、transform 取 method body 才有了基础。

## 第三跳：方法为什么会被标记成 interpreter 实现

程序集被接进来，还不等于方法已经会跑。  
接下来真正关键的是：`MethodInfo` 在初始化时，runtime 怎么知道这个方法不是普通 AOT 方法。

这个动作发生在 `libil2cpp/vm/Class.cpp` 的 `Class::SetupMethods` 里。

你会看到这样一段：

```cpp
newMethod->invoker_method = MetadataCache::GetMethodInvoker(klass->image, methodInfo.token);
newMethod->isInterpterImpl = hybridclr::interpreter::InterpreterModule::IsImplementsByInterpreter(newMethod);
```

这两句要连在一起看。

第一句先根据 `image + token` 去拿 `invoker_method`。  
第二句再根据这个 `invoker_method` 判断，这个方法是不是 interpreter 实现。

真正的分流发生在 `MetadataCache::GetMethodInvoker`：

```cpp
if (hybridclr::metadata::IsInterpreterImage(image))
{
    return hybridclr::metadata::MetadataModule::GetMethodInvoker(image, token);
}
```

也就是说：

- 如果这是普通 AOT image，就去 `codeGenModule->invokerIndices` 里找原生 invoker
- 如果这是 `InterpreterImage`，就改走 HybridCLR 自己的 invoker 分发

而 `MetadataModule::GetMethodInvoker` 再往下，会落到 `InterpreterImage::GetMethodInvoker(token)`：

```cpp
const Il2CppMethodDefinition* methodDef = &_methodDefines[methodIndex];
return hybridclr::interpreter::InterpreterModule::GetMethodInvoker(methodDef);
```

最后，`InterpreterModule::GetMethodInvoker` 会返回 `InterpreterInvoke` 或 `InterpreterDelegateInvoke`。

到这里，这个热更方法的关键身份其实已经确定了：

`它的 invoker 不再指向 AOT invoker，而是指向 HybridCLR 的解释器入口。`

## 第四跳：`MethodInfo.Invoke` 真正调的其实是 `invoker_method`

很多人会在这里误以为“反射调用是另外一套逻辑”。  
其实在 `libil2cpp` 里，反射最终还是会回到方法自己的 `invoker_method`。

看 `libil2cpp/vm/Runtime.cpp`：

```cpp
if (method->return_type->type == IL2CPP_TYPE_VOID)
{
    method->invoker_method(method->methodPointer, method, obj, params, NULL);
    return NULL;
}
```

也就是说，`MethodInfo.Invoke` 最终并不会神奇地绕过 HybridCLR。  
它仍然只是：

`准备参数 -> 调 method->invoker_method`

如果这个 `method` 来自 AOT image，`invoker_method` 就是普通 native invoker。  
如果这个 `method` 来自 `InterpreterImage`，`invoker_method` 就已经是 `InterpreterInvoke` 了。

这也是为什么上一节那条“method 初始化时如何选择 invoker”这么关键。

对这篇文章来说，可以把中间这段压成一句话：

`反射没有发明第二条执行链，它只是走进了方法对象已经绑定好的 invoker。`

如果你跟到这一跳，断点里最值得看的其实不是 `Invoke` 自己，而是 `method->invoker_method` 这个字段最终指向了谁。

- 如果它指向普通 native invoker，这还是 AOT 路径
- 如果它已经指向 `InterpreterInvoke`，那后面的控制流就已经彻底进入 HybridCLR 主链了

也就是说，这一跳真正的分叉点不在反射层，而在方法初始化阶段写进 `MethodInfo` 的那个函数指针。

## 第五跳：`InterpreterInvoke` 做的第一件事不是执行，而是整理调用现场

真正掉进 HybridCLR 的第一个函数，在 `hybridclr/interpreter/InterpreterModule.cpp`：

```cpp
static void InterpreterInvoke(Il2CppMethodPointer methodPointer, const MethodInfo* method, void* __this, void** __args, void* __ret)
{
    InterpMethodInfo* imi = method->interpData ? (InterpMethodInfo*)method->interpData : InterpreterModule::GetInterpMethodInfo(method);
    StackObject* args = (StackObject*)alloca(sizeof(StackObject) * imi->argStackObjectSize);
    ...
    ConvertInvokeArgs(args + isInstanceMethod, method, argDescs, __args);
    Interpreter::Execute(method, args, __ret);
}
```

这段代码可以直接看出 3 个事实：

1. 第一次执行前，不一定已经有 `InterpMethodInfo`，所以会先走 `GetInterpMethodInfo(method)`。
2. 反射或普通调用传进来的 `void** __args`，不会直接交给解释器，而是先转换成 HybridCLR 自己的 `StackObject` 参数布局。
3. 真正的执行入口依然是 `Interpreter::Execute(method, args, __ret)`。

也就是说，`InterpreterInvoke` 的职责并不是“解释执行方法体”。  
它更像一个桥：

`把 IL2CPP 世界里的调用约定，整理成解释器能接的参数布局，然后把控制权交给 Execute。`

如果你自己跟这一段，我建议顺手观察 3 个量：

- `method->interpData`：确认这次是不是第一次执行
- `imi->argStackObjectSize`：确认解释器到底准备了多大的参数栈布局
- `__args -> args`：确认托管/反射侧参数是在哪一步被翻译成 `StackObject[]` 的

这三个量一旦看明白，`InterpreterInvoke` 在整条链里的职责就会非常清楚。

## 第六跳：第一次执行时，为什么还要先 `GetInterpMethodInfo`

这一跳决定了这篇文章是不是能把“transform 为什么存在”说实。

`InterpreterInvoke` 里最容易被忽略的一句是：

```cpp
InterpMethodInfo* imi = method->interpData ? ... : InterpreterModule::GetInterpMethodInfo(method);
```

它说明 `InterpMethodInfo` 不是在程序集装载时就全部预生成好的。  
至少在默认路径下，它是按方法第一次被执行时懒生成的。

再看 `GetInterpMethodInfo`：

```cpp
InterpMethodInfo* imi = transform::HiTransform::Transform(methodInfo);
const_cast<MethodInfo*>(methodInfo)->interpData = imi;
return imi;
```

这一步的意义非常大。  
它说明 HybridCLR 的执行链不是：

`拿到原始 IL -> 直接逐条解释`

而是：

`拿到 MethodInfo -> 先 transform 成 InterpMethodInfo -> 再执行`

这也是为什么前几篇一直强调 `transform` 不是一个可有可无的中间层。

## 第七跳：`Transform` 先去取 method body，再把它改写成内部指令

`hybridclr/transform/Transform.cpp` 的入口很短，但信息密度很高：

```cpp
metadata::Image* image = metadata::MetadataModule::GetUnderlyingInterpreterImage(methodInfo);
metadata::MethodBody* methodBody = metadata::MethodBodyCache::GetMethodBody(image, methodInfo->token);
TransformContext ctx(image, methodInfo, *methodBody, pool, resolveDatas);
ctx.TransformBody(0, 0, *result);
```

这里最关键的是顺序。

### 1. 先决定这次取 method body 应该从哪个 image 上拿

`GetUnderlyingInterpreterImage(methodInfo)` 不是一个装饰性函数。  
它在决定：

- 如果这是热更方法，就直接从 `InterpreterImage` 上取
- 如果这是 AOT 方法但走了解释兜底，就从对应的 `AOTHomologousImage` 上取

### 2. 再通过 `MethodBodyCache` 取 `token -> MethodBody`

`MethodBodyCache::GetMethodBody(image, token)` 本质上是一个 `(image, token)` 维度的缓存。

它第一次会调用：

```cpp
MethodBody* methodBody = image->GetMethodBody(token);
```

后面再访问同一个方法体，就尽量复用缓存。

这一层的好处非常直接：

- `Transform` 不需要自己反复解析 method body
- 后续 inlineability 判断也可以复用同一份 method body

### 3. 再进入 `TransformContext`

到这里，HybridCLR 才真正开始把原始 CIL method body 改写成自己的内部表示。

所以这一跳最值得记住的一句话是：

`transform 的输入不是 MethodInfo 本身，而是“MethodInfo 所指向的 method body + runtime 解析结果”。`

跟这一跳时，最好顺便看一下 `image` 的实际类型。

- 如果它是 `InterpreterImage`，你看到的是热更程序集自己的 method body
- 如果它是 `AOTHomologousImage`，你看到的是 AOT 程序集补回来的那份同源 metadata 视图

这个观察点很值钱，因为它正好把“热更程序集执行”和“AOT 解释兜底”这两条路径在 transform 入口处收束到了一起。

## 第八跳：`Interpreter::Execute` 跑的已经不是原始 IL，而是 HiOpcode

等你真的走到 `hybridclr/interpreter/Interpreter_Execute.cpp`，会看到 `Execute` 的主体是一个巨大的 `switch`。

入口大概长这样：

```cpp
void Interpreter::Execute(const MethodInfo* methodInfo, StackObject* args, void* ret)
{
    ...
    PREPARE_NEW_FRAME_FROM_NATIVE(methodInfo, args, ret);
LoopStart:
    for (;;)
    {
        switch (*(HiOpcodeEnum*)ip)
        {
            case HiOpcodeEnum::InitLocals_n_2:
            ...
```

最重要的不是 `switch` 很大，而是这两个事实：

1. 它先通过 `PREPARE_NEW_FRAME_FROM_NATIVE` 建立 `InterpFrame` 和当前方法的执行现场。
2. `switch` 分派的已经不是原始 CIL opcode，而是 `HiOpcodeEnum`。

第一点说明解释器并不是“拿着一个 `MethodInfo` 就直接跑”。  
第二点说明 transform 的产物是真正会被执行的内部指令，而不是原始 IL 字节流。

如果把这一层再压缩一下，可以得到一个很重要的判断：

`HybridCLR 的 interpreter 是在执行 transform 之后的内部 IR，而不是直接解释 ECMA-335 意义上的原始 CIL。`

这也是它和“纯 IL 解释器”非常不一样的地方。

## 把整条链压成 8 步

如果你现在回头看这一篇，其实整条链可以压成下面 8 步：

1. 业务代码通过 `Assembly.Load(byte[])` 把热更程序集送进 `AppDomain::LoadAssemblyRaw`。
2. `MetadataCache::LoadAssemblyFromBytes` 把装载转交给 `hybridclr::metadata::Assembly::LoadFromBytes`。
3. `Assembly::Create` 为这个 DLL 构造 `InterpreterImage`，并注册成解释型程序集。
4. `Class::SetupMethods` 为这个 image 里的方法绑定 `invoker_method`，最终指向 `InterpreterInvoke`。
5. 业务代码调用 `MethodInfo.Invoke`，`vm::Runtime::InvokeWithThrow` 最终执行 `method->invoker_method(...)`。
6. `InterpreterInvoke` 把参数转换成 `StackObject[]`，并在第一次调用时通过 `GetInterpMethodInfo` 触发 transform。
7. `HiTransform::Transform` 从 `MethodBodyCache` 取 method body，再产出 `InterpMethodInfo`。
8. `Interpreter::Execute` 进入 `HiOpcode` 分派循环，真正把这个热更方法跑起来。

这 8 步里，最值得注意的边界是：

- 第 1 到 3 步是装载期
- 第 4 步是方法初始化期
- 第 5 到 8 步是真正调用期
- transform 通常发生在第一次调用时，而不是程序集加载时

## 这一条链里，最容易看错的 4 个地方

### 误解一：`Assembly.Load(byte[])` 之后，方法就已经“编译好了”

不对。  
装载阶段解决的是“程序集如何进入 runtime metadata 世界”，不是“每个方法都已经变成可执行的 `InterpMethodInfo`”。

### 误解二：反射调用会绕开 HybridCLR 的解释链

也不对。  
`MethodInfo.Invoke` 最终还是走 `method->invoker_method`，而这个 invoker 在热更方法上已经被绑定成 `InterpreterInvoke`。

### 误解三：`Interpreter::Execute` 直接解释原始 IL

不对。  
它执行的是 transform 后的 `HiOpcode`，不是原始 CIL 字节流。

### 误解四：transform 是 build-time 发生的

也不对。  
至少从这条默认执行链来看，transform 是典型的 lazy path：方法第一次真正被执行时才触发。

## 断点建议

如果你准备自己进工程跟这一条链，最值得下断点的位置是：

- `ProcedureLoadAssembly.LoadAssetSuccess`
- `AppDomain::LoadAssemblyRaw`
- `MetadataCache::LoadAssemblyFromBytes`
- `hybridclr::metadata::Assembly::Create`
- `Class::SetupMethods`
- `Runtime::InvokeWithThrow`
- `InterpreterInvoke`
- `InterpreterModule::GetInterpMethodInfo`
- `HiTransform::Transform`
- `Interpreter::Execute`

只跟这一条线，不去扩展 `TransformContext.cpp` 的所有细节，基本就已经能把 HybridCLR 的“方法调用主链”立住。

## 最后压一句话

如果只用一句话概括这篇文章，我会写成：

`HybridCLR 让热更方法跑起来，不是靠 Assembly.Load 之后直接解释 DLL，而是先把程序集注册成 InterpreterImage，再把方法绑定到解释器 invoker，第一次调用时懒生成 InterpMethodInfo，最后由 Execute 去跑 transform 之后的内部指令。`

这条链如果你能在源码里自己走通，前面几篇关于 runtime、AOT 泛型、工具链、MonoBehaviour 的讨论，基本就都能落回具体代码了。

## 系列位置

- 上一篇：<a href="{{< relref "engine-notes/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}}">HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化</a>
- 下一篇：<a href="{{< relref "engine-notes/hybridclr-boundaries-and-tradeoffs.md" >}}">HybridCLR 的边界与 trade-off｜不要把补充 metadata、AOT 泛型、MethodBridge、MonoBehaviour、DHE 混成一件事</a>
