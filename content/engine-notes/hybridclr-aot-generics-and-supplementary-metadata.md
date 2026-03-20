+++
title = "HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑"
description = "从 AOT 泛型缺口、补充 metadata、Consistent/SuperSet 映射，到 AOTGenericReference 和 MethodBridge，拆解 HybridCLR 在泛型问题上到底补了什么、没补什么。"
weight = 31
featured = false
tags = ["Unity", "IL2CPP", "HybridCLR", "Generics"]
+++

> HybridCLR 的补充 metadata 解决的是“运行时能不能看懂这段 AOT metadata”，不是“凭空生成一份原本没有被 AOT 出来的泛型 native 实现”。

这是 HybridCLR 系列第 2 篇。第一篇先把 `RuntimeApi -> metadata -> transform -> execute` 这条主链立住；这一篇只收一件事：AOT 泛型和补充 metadata 到底各自补哪一层。

这篇不再重讲 `MethodInfo -> Execute` 的执行主链，也不展开菜单生成顺序，只讨论泛型与同源 metadata 的边界。

主链一旦立住，紧接着就会遇到一个更具体、也更容易把人绕晕的问题：

`为什么有些代码明明能编译，到了 IL2CPP 运行时还是会因为泛型出问题？`

这篇文章就专门拆这个问题。

我想先给一个非常压缩的判断：

- 有一类问题，本质上是 metadata 不足，runtime 看不懂
- 另一类问题，本质上是 AOT 实现根本不存在，runtime 没东西可调

HybridCLR 的补充 metadata 主要解决第一类。  
而 `AOTGenericReference`、手工保泛型实例、`MethodBridge`，则是在帮你处理第二类和跨边界类问题。

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 为什么泛型在 IL2CPP 上天然更容易出问题。
2. `LoadMetadataForAOTAssembly` 到底补了什么能力。
3. `HomologousImageMode.Consistent` 和 `SuperSet` 的差别到底是什么。
4. `AOTGenericReference`、MethodBridge、补充 metadata 分别解决哪一层问题。

这篇不是“教你怎么点菜单”，而是把这些菜单和 runtime 行为重新对上。

## 先给一个最小泛型背景

如果对 ECMA-335 有印象，这里最该先想起的是三组东西：

- `TypeDef` / `MethodDef`
- `TypeSpec`
- `MethodSpec`

前两者更偏“定义”，后两者更偏“具体实例化”。

比如：

- `List<T>` 是泛型类型定义
- `List<int>` 才是一个具体实例
- `Foo<T>.Bar<U>(T a, U b)` 是泛型方法定义
- `Foo<int>.Bar<string>(...)` 才是一个具体方法实例

在普通 CLR 世界里，这个问题经常没有那么刺眼，因为运行时可以继续做更多动态工作。

但在 IL2CPP 世界里，问题会更尖锐。  
因为 IL2CPP 的基本前提是 AOT：

- 你最终要调的很多东西，本来就希望在构建时已经准备好
- 某个具体泛型实例如果构建时根本没被看到，对应 native 实现就可能根本不存在

所以泛型问题在 IL2CPP 上很容易裂成两层：

1. metadata 层  
   runtime 能不能正确读到泛型定义、实例化关系、方法体、签名。
2. native 实现层  
   真正落到调用边界时，有没有对应的 AOT generic method/native pointer。

这两层一定要分开，不然后面看 HybridCLR 的代码就会误判。

## 为什么这个问题值得单独拿出来讲

因为 HybridCLR 最容易被误解的地方，就在这里。

很多人会把下面几件事混成一件事：

- 补充 metadata
- AOT 泛型实例保留
- `AOTGenericReference`
- `MethodBridge`

但从源码看，它们根本不是一回事。

我觉得最稳的理解方式，是先把问题拆成两种失败：

### 失败一：runtime 看不懂

比如：

- 某些 method body、signature、泛型上下文没法在当前 runtime 里被恢复
- transform 拿不到足够 metadata

这种问题，补充 metadata 通常是有效的。

### 失败二：runtime 看懂了，但就是没有实现

比如：

- 需要直接调一个 AOT generic method
- 需要拿一个具体实例化后的方法指针
- 需要跨 interpreter / AOT / native 边界走桥接

这种问题，补充 metadata 本身不够。  
因为“看得懂”和“调得到”是两回事。

## 先给源码地图

这一篇还是基于同一个工程：

`E:\HT\Projects\DP\TopHeroUnity`

如果你准备跟着源码看，我建议先盯这几处：

- `Packages/HybridCLR/Runtime/HomologousImageMode.cs`
- `Packages/HybridCLR/Runtime/RuntimeApi.cs`
- `Packages/HybridCLR/Editor/Commands/AOTReferenceGeneratorCommand.cs`
- `Packages/HybridCLR/Editor/AOT/GenericReferenceWriter.cs`
- `HybridCLRData/.../hybridclr/metadata/Assembly.cpp`
- `HybridCLRData/.../hybridclr/metadata/AOTHomologousImage.cpp`
- `HybridCLRData/.../hybridclr/metadata/ConsistentAOTHomologousImage.cpp`
- `HybridCLRData/.../hybridclr/metadata/SuperSetAOTHomologousImage.cpp`
- `HybridCLRData/.../hybridclr/CommonDef.h`
- `HybridCLRData/.../hybridclr/interpreter/InterpreterModule.cpp`

这篇的主线不会直接从 `Execute` 开始，而是先从“补 metadata 的语义到底是什么”开始。

## `LoadMetadataForAOTAssembly` 真正补的不是执行能力，而是 metadata 能力

先看 C# 层暴露出来的枚举：

```csharp
public enum HomologousImageMode
{
    Consistent,
    SuperSet,
}
```

再看真正的入口：

```cpp
int32_t RuntimeApi::LoadMetadataForAOTAssembly(Il2CppArray* dllBytes, int32_t mode)
{
    return (int32_t)hybridclr::metadata::Assembly::LoadMetadataForAOTAssembly(
        il2cpp::vm::Array::GetFirstElementAddress(dllBytes),
        il2cpp::vm::Array::GetByteLength(dllBytes),
        (hybridclr::metadata::HomologousImageMode)mode);
}
```

这句和上一篇讲的一样，首先已经把语义钉死了：

`这个 API 不是在加载热更程序集执行，而是在给 AOT 程序集补一份同源 image。`

继续看 `Assembly.cpp`：

```cpp
LoadImageErrorCode Assembly::LoadMetadataForAOTAssembly(const void* dllBytes, uint32_t dllSize, HomologousImageMode mode)
{
    AOTHomologousImage* image = nullptr;
    switch (mode)
    {
    case HomologousImageMode::CONSISTENT: image = new ConsistentAOTHomologousImage(); break;
    case HomologousImageMode::SUPERSET: image = new SuperSetAOTHomologousImage(); break;
    }

    LoadImageErrorCode err = image->Load((byte*)CopyBytes(dllBytes, dllSize), dllSize);
    ...
    const Il2CppAssembly* aotAss = il2cpp::vm::Assembly::GetLoadedAssembly(assName);
    ...
    image->SetTargetAssembly(aotAss);
    image->InitRuntimeMetadatas();
    AOTHomologousImage::RegisterLocked(image, lock);
    return LoadImageErrorCode::OK;
}
```

这段代码说明补充 metadata 的完整动作是：

1. 先创建一种 `AOTHomologousImage`
2. 解析传进来的 DLL bytes
3. 根据程序集名找到已经加载的 AOT assembly
4. 初始化这份 image 的 runtime metadata
5. 把这份 image 注册到目标 AOT assembly 上

也就是说，HybridCLR 在这里补的不是“代码生成”，而是：

`给一个已经存在的 AOT assembly 补一份运行时可查询、可解析、可拿 method body 的 metadata image。`

## `AOTHomologousImage` 到底在干什么

`AOTHomologousImage.cpp` 里有一个很关键的全局表：

```cpp
std::vector<AOTHomologousImage*> s_images;

AOTHomologousImage* AOTHomologousImage::FindImageByAssembly(const Il2CppAssembly* ass)
{
    il2cpp::os::FastAutoLock lock(&il2cpp::vm::g_MetadataLock);
    return FindImageByAssemblyLocked(ass, lock);
}
```

这件事非常重要。

因为它说明“补 metadata”不是一次性过程，而是把一份同源 image 长期挂到目标 AOT assembly 上。

后面 runtime 再需要：

- 读 type
- 读 method
- 读 field
- 读 method body
- 读 generic metadata

都可以沿着 assembly 反查回这份 homologous image。

所以如果要压一句话：

`AOTHomologousImage 是 HybridCLR 给 AOT assembly 加的那层“第二套可运行时查询的 metadata 视图”。`

## `Consistent` 和 `SuperSet` 到底差在哪

这两个模式只看名字很抽象，但只要一看实现，差别其实非常明显。

### `Consistent`：更像“按 token/row 对齐的同构映射”

先看 `ConsistentAOTHomologousImage.cpp`：

```cpp
void ConsistentAOTHomologousImage::InitTypes()
{
    ...
    Il2CppTypeDefinition* typeDef = (Il2CppTypeDefinition*)il2cpp::vm::MetadataCache::GetAssemblyTypeHandle(image, index);
    uint32_t rowIndex = DecodeTokenRowIndex(typeDef->token);
    ...
    TbTypeDef data = _rawImage->ReadTypeDef(rowIndex);
    ...
    if (std::strcmp(name1, name2))
    {
        RaiseExecutionEngineException("metadata type not match");
    }
}
```

方法和字段初始化也是同样的路子：  
先按 token/row 去对齐，再验证名字是否一致。

这说明 `Consistent` 的前提很强：

- 补充 metadata 和目标 AOT metadata 的结构要高度一致
- 它假设 token/row 的对应关系基本可靠
- 如果类型名、方法名、字段名对不上，就直接认为 metadata 不匹配

所以 `Consistent` 更像是一种“严格同构”的模式。

### `SuperSet`：更像“按名字和签名做宽松匹配”

再看 `SuperSetAOTHomologousImage.cpp`，路线就变了。

它先按类型名找，再按方法名和签名找：

```cpp
const Il2CppMethodDefinition* FindMatchMethod(const Il2CppTypeDefinition* aotTypeDef, ..., const char* methodName, const MethodRefSig& methodSignature)
{
    for (uint16_t i = 0; i < aotTypeDef->method_count; i++)
    {
        const Il2CppMethodDefinition* aotMethodDef = ...;
        const char* aotMethodName = ...;
        if (std::strcmp(aotMethodName, methodName))
        {
            continue;
        }
        if (IsMatchMethodSig(aotMethodDef, methodSignature, klassGenContainer))
        {
            return aotMethodDef;
        }
    }
    return nullptr;
}
```

类型找不到时，它甚至会回落到一个默认 missing type：

```cpp
labelInitDefault:
type.aotIl2CppType = _defaultIl2CppType;
```

这说明 `SuperSet` 的核心思路不是“严格按 row 对齐”，而是：

- 先尽量按名字和签名把能对上的东西对上
- 对不上的部分，不立即整体失败
- 尽量在一个更宽松的 superset metadata 上继续工作

所以这两个模式的直观理解可以压成这样：

- `Consistent`：你给我的补充 metadata，最好和真实 AOT 世界几乎同构
- `SuperSet`：你给我的补充 metadata 可以更大，我来尽量把它映射回当前 AOT 世界

## 补充 metadata 到底能解决什么

如果只看运行时这一层，我觉得它最核心的价值是两件事。

### 1. 让 runtime 能重新拿到 method body 和泛型相关 metadata

补充 metadata 之后，后面的 transform/解释器链路就不必只依赖已经被裁剪后的 AOT metadata。

它可以通过 `AOTHomologousImage` 重新拿到：

- type 定义
- method 定义
- field 定义
- method body
- generic container / generic context

这也是为什么 HybridCLR 能在 IL2CPP 上解释执行一些原本没有完整 runtime metadata 支撑的调用路径。

### 2. 让 AOT assembly 在 runtime 里拥有一份“可解释”的 metadata 视图

这一点也很关键。

IL2CPP 的 AOT 世界本来更偏“最终结果”。  
而 HybridCLR 的 transform / interpreter 更需要一份“还能被 runtime 继续解释和解析”的 metadata 视图。

补充 metadata 其实就是把这层视图重新加回去。

## 补充 metadata 明显解决不了什么

这才是这篇最关键的结论。

它解决不了：

`原本就没有被 AOT 出来的具体泛型 native 实现。`

这不是我在做概念区分，而是源码里直接写了异常路径。

先看 `CommonDef.h`：

```cpp
inline void RaiseAOTGenericMethodNotInstantiatedException(const MethodInfo* method)
{
    TEMP_FORMAT(errMsg, "AOT generic method not instantiated in aot. assembly:%s, method:%s", ...);
    il2cpp::vm::Exception::Raise(il2cpp::vm::Exception::GetMissingMethodException(errMsg));
}
```

这个异常名已经把问题说死了：  
不是“metadata 没补上”，而是“这个 AOT generic method 根本没被 instantiate 到 aot 世界里”。

再看 `InterpreterModule.cpp` 的调用路径：

```cpp
if (!InitAndGetInterpreterDirectlyCallMethodPointer(method))
{
    RaiseAOTGenericMethodNotInstantiatedException(method);
}
```

`TransformContext.cpp` 里也有同样的判断：

```cpp
if (!InitAndGetInterpreterDirectlyCallMethodPointer(shareMethod))
{
    RaiseAOTGenericMethodNotInstantiatedException(shareMethod);
}
```

这两处的含义非常直接：

- runtime 已经走到“需要直接拿这个方法的可调用指针”这一步了
- 如果对应 AOT generic method 从来没被实例化过
- 那就不是补 metadata 能解决的问题

也就是说：

`补 metadata 解决的是“解释和解析能力”，不是“替 AOT 世界补一份本来没有的 native generic implementation”。`

如果你准备亲自跟一次这条失败链，我建议断点顺序就按下面三处走：

- `Assembly::LoadMetadataForAOTAssembly`：先确认补 metadata 只是注册 `AOTHomologousImage`
- `AOTHomologousImage::RegisterLocked`：再确认这份 image 是怎么长期挂回目标 AOT assembly 的
- `RaiseAOTGenericMethodNotInstantiatedException`：最后确认真正报错时，问题已经从 metadata 层推进到了“我要一个可调用指针，但 AOT 世界里没有”

这三处连起来，读者就能在源码里亲眼看到“看得懂”和“调得到”为什么是两回事。

## `AOTGenericReference` 到底在做什么

到这一步就能理解，为什么 HybridCLR 还需要单独做 `AOTGenericReference`。

先看命令本身：

```csharp
AssemblyReferenceDeepCollector collector =
    new AssemblyReferenceDeepCollector(
        MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames),
        hotUpdateDllNames);

var analyzer = new Analyzer(...);
analyzer.Run();
writer.Write(analyzer.AotGenericTypes.ToList(), analyzer.AotGenericMethods.ToList(), ...);
```

这一步不是在改 runtime，而是在分析：

`热更代码到底触发到了哪些 AOT 泛型类型和泛型方法实例。`

更有意思的是生成器本身。  
如果你看 `GenericReferenceWriter.cs`，会发现它生成出来的 `AOTGenericReferences.cs` 本质上更像一个报告文件，而不是魔法修复文件。

```csharp
codes.Add("\t// {{ AOT generic types");
foreach(var typeName in typeNames)
{
    codes.Add($"\t// {typeName}");
}

codes.Add("\tpublic void RefMethods()");
codes.Add("\t{");
foreach(var method in methodTypeAndNames)
{
    codes.Add($"\t\t// {PrettifyMethodSig(method.Item3)}");
}
```

注意这里大部分内容都是注释。

这件事非常值得讲清楚，因为它和很多人的直觉正好相反：

`AOTGenericReference 默认不是“自动修复器”，而是“把真实风险点显式列出来的清单”。`

它告诉你的不是“已经好了”，而是：

- 热更代码现在实际依赖了哪些 AOT 泛型实例
- 你最好显式把哪些实例保进 AOT 世界

## MethodBridge 和泛型问题是什么关系

很多人会把 MethodBridge 只理解成“跨 native 的桥接代码”。

但从泛型问题看，它还有另一层意义：

当调用跨 interpreter / AOT / native 边界时，光有 metadata 还不够。

你还得回答：

- 调的是哪一个具体实例化后的签名
- 这个签名有没有对应桥接桩
- 值类型泛型参数怎么过 ABI

`MethodBridgeGeneratorCommand.cs` 里同时跑了多种分析器：

```csharp
methodBridgeAnalyzer.Run();
reversePInvokeAnalyzer.Run();
calliAnalyzer.Run();
pinvokeAnalyzer.Run();
GenerateMethodBridgeCppFile(..., outputFile);
```

这说明 MethodBridge 的位置不是“小优化”，而是：

`当某个具体泛型实例真的要跨边界被调起来时，runtime 需要的另一套签名桥。`

所以这一层和 `AOTGenericReference` 的关系是：

- `AOTGenericReference` 更偏“哪些实例需要存在”
- `MethodBridge` 更偏“这些实例跨边界时怎么调用”

它们都不是补充 metadata 的替代品，但也都不是补充 metadata 能自动覆盖掉的部分。

如果你跟到这里还想继续顺着源码看，我建议只再补一处断点就够了：`MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper`。

原因很简单。到这一步读者最容易误判成“MethodBridge 只是工具层产物”。但真正跟一次生成流程就会发现，它分析的是具体签名，不是在做抽象概念补丁。

## 把这件事压成一句话

如果把这篇压成一句话，我会这样描述 HybridCLR 在 AOT 泛型问题上的位置：

`补充 metadata 让 runtime 重新拥有解释和解析 AOT 泛型 metadata 的能力；AOTGenericReference 告诉你哪些具体泛型实例不能只靠“看得懂”就算完，而必须在 AOT 世界里真的存在；MethodBridge 则负责这些具体实例跨 interpreter / AOT / native 边界时的调用问题。`

这三者各自站在不同层上，不能混。

## 常见误解

### 误解一：补充 metadata 以后，泛型问题就都解决了

不对。

补充 metadata 解决的是 metadata 能力，不是自动生成缺失的 AOT generic implementation。

### 误解二：`AOTGenericReference` 一生成，运行时就自动安全了

也不对。

从 `GenericReferenceWriter.cs` 看，它默认更像清单和提示，不是自动修复器。

### 误解三：`Consistent` 和 `SuperSet` 只是两个命名不同的模式

不对。

它们背后的映射策略完全不同：

- `Consistent` 更严格，偏 token/row 对齐
- `SuperSet` 更宽松，偏名字和签名匹配

### 误解四：泛型问题和 MethodBridge 没关系

不对。

一旦某个具体实例需要跨 interpreter / AOT / native 边界被调用，MethodBridge 就会进入问题现场。

## 最后一句

如果上一篇回答的是：

`HybridCLR 怎么把热更方法一路带到 Interpreter::Execute`

那么这一篇回答的其实是：

`为什么“能看到 metadata”不等于“这个泛型实例真的能在 IL2CPP 世界里被调用起来”。`

我觉得这是理解 HybridCLR 最容易掉坑、也最值得单独拆开的一层。

## 系列位置

- 上一篇：[HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-notes/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}})
- 下一篇：[HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么]({{< relref "engine-notes/hybridclr-toolchain-what-generate-buttons-do.md" >}})
