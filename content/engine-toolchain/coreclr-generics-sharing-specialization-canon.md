---
title: "CoreCLR 实现分析｜泛型实现：代码共享、特化与 System.__Canon"
slug: "coreclr-generics-sharing-specialization-canon"
date: "2026-04-14"
description: "从 CoreCLR 源码出发，拆解泛型的完整实现：JIT 按需实例化的运行时模型、引用类型代码共享与 System.__Canon canonical 类型、值类型独立 JIT 的特化路径、Generic Dictionary 的运行时类型查找、RGCTX 的 hidden parameter 传递机制、Constrained Call 的 boxing 避免优化，以及与 IL2CPP / LeanCLR 泛型处理策略的对比。"
weight: 46
featured: false
tags:
  - CoreCLR
  - CLR
  - Generics
  - Sharing
  - JIT
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> 泛型是 CoreCLR 中类型系统和 JIT 协作最紧密的子系统——类型系统为每个泛型实例构建 MethodTable，JIT 决定哪些实例可以共享同一份 native code，Generic Dictionary 在运行时弥合共享代码与具体类型之间的信息差。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 7 篇。

B6 拆解了异常处理的两遍扫描模型和 SEH 集成机制。异常处理解决的是"控制流异常时 runtime 怎样正确传播"的问题。这篇进入另一个核心子系统：泛型。泛型解决的问题是"同一段算法逻辑怎样在不同类型参数下复用，且不牺牲类型安全和运行时性能"。

## 泛型在 CoreCLR 中的位置

ECMA-A4 在规范层面定义了泛型的类型系统规则——开放类型与封闭类型、泛型约束、类型参数的 variance。那篇讲的是"规范要求了什么"，这篇讲的是"CoreCLR 怎么实现的"。

CoreCLR 的泛型实现跨越三个模块：

**类型系统（`src/coreclr/vm/generics.cpp`）** — 负责泛型类型的加载和实例化。当代码首次使用 `List<int>` 时，ClassLoader 基于 `List<T>` 的开放类型定义和类型参数 `int`，构建封闭类型的 MethodTable。这个过程称为泛型实例化（generic instantiation）。

**JIT 编译器（`src/coreclr/jit/`）** — 决定每个泛型方法是否需要独立编译，还是可以复用已有的共享代码。JIT 在编译共享代码时，插入通过 Generic Dictionary 或 RGCTX 查找具体类型信息的逻辑。

**Generic Dictionary（`src/coreclr/vm/genericdict.cpp`）** — 运行时的泛型上下文信息存储。共享代码无法硬编码具体类型的 MethodTable 指针或方法地址，这些信息存放在 dictionary 中，由共享代码在运行时查找。

三个模块的协作关系：类型系统决定"构建几份 MethodTable"，JIT 决定"编译几份 native code"，Generic Dictionary 在共享代码和具体类型之间架起桥梁。

与 IL2CPP 的根本差异在于时机。IL2CPP 是 AOT 方案，必须在构建阶段预判项目使用了哪些泛型组合，没见过的组合在运行时就没有 native code 可执行。CoreCLR 是 JIT 方案——运行时遇到 `Dictionary<PlayerID, Inventory>` 就现场编译一份，不存在"构建时没见过"的问题。这个根本差异决定了两个 runtime 在泛型处理上面临的挑战完全不同：CoreCLR 的问题是"怎么减少重复编译"，IL2CPP 的问题是"怎么保证不遗漏"。

## 泛型代码共享策略

如果每个泛型实例化都独立编译一份 native code，内存消耗会不可接受。一个典型的 .NET 应用可能有 `List<string>`、`List<object>`、`List<Exception>`、`List<Task>` 等数十种引用类型实例化——它们的方法体在 native code 层面完全相同。

CoreCLR 的共享策略分为两条路径：

### 引用类型：全部共享

所有引用类型参数的泛型实例共享同一份 JIT 代码。`List<string>`、`List<object>`、`List<MyClass>` 在 native code 层面复用同一份编译产物。

共享能成立的前提是 ABI 一致性：所有引用类型在内存中都是指针大小（x64 上 8 字节），赋值语义相同（复制指针），GC 扫描方式相同（都标记为引用）。从 native code 的角度看，操作一个 `string` 引用和操作一个 `object` 引用没有任何区别——都是对一个指针大小的 slot 做读写。

这意味着 `List<string>.Add(string item)` 和 `List<object>.Add(object item)` 编译出的 native code 完全一样：接收一个指针参数，写入内部数组的一个指针 slot。JIT 只需要编译一次，所有引用类型实例化都可以复用。

### 值类型：独立编译

值类型参数的泛型实例必须独立编译。`List<int>` 和 `List<double>` 各有一份 native code。

原因是值类型的 ABI 不一致：`int` 是 4 字节，`double` 是 8 字节，`Vector3` 是 12 字节。字段偏移不同，内部数组的元素大小不同，方法参数的传递方式不同（小值类型可能通过寄存器传递，大值类型通过栈传递），GC 描述不同（值类型内部是否包含引用字段）。强行让不同大小的值类型共享同一份代码，会导致内存越界和参数传递错误。

```
引用类型共享：
List<string>   ─┐
List<object>   ─┤── 共享 → List<System.__Canon> 的 JIT code
List<MyClass>  ─┘

值类型独立：
List<int>      ── 独立 JIT code（元素 4 字节）
List<double>   ── 独立 JIT code（元素 8 字节）
List<Vector3>  ── 独立 JIT code（元素 12 字节）
```

值类型独立编译的代价是代码膨胀——每种值类型组合都需要一份 native code。但收益是性能最优：JIT 知道元素的确切大小和对齐方式，可以生成精确的内存操作指令，无需额外的间接层。对于数值计算密集型场景（`List<int>` 的排序、`Span<float>` 的向量运算），这种特化带来的性能优势远大于代码膨胀的成本。

## System.__Canon

`System.__Canon` 是 CoreCLR 内部的 canonical 类型，定义在 `src/coreclr/vm/generics.cpp` 中。它不是一个用户可见的类型——C# 代码无法引用它，BCL 文档中也找不到它。它只存在于 runtime 内部，作为引用类型泛型代码共享的基准标记。

### 工作原理

当 ClassLoader 加载 `List<string>` 时，它首先检查是否已存在 `List<__Canon>` 的 MethodTable。如果不存在，就创建一个以 `__Canon` 作为类型参数的 canonical MethodTable。后续所有引用类型的 `List<T>` 实例化——`List<object>`、`List<Exception>`、`List<Task>`——都指向这同一个 canonical MethodTable 的 JIT 代码。

```
类型加载流程：
1. 遇到 List<string>
2. 将类型参数 string 规范化 → __Canon（因为 string 是引用类型）
3. 查找 List<__Canon> 的 MethodTable：
   - 不存在 → 创建 canonical MethodTable，触发 JIT 编译
   - 已存在 → 复用已有的 JIT 代码
4. 为 List<string> 创建独立的 MethodTable（记录具体的类型参数），
   但 JIT 代码指向 List<__Canon> 的编译产物
```

每个引用类型实例化仍然有自己的 MethodTable——`List<string>` 和 `List<object>` 的 MethodTable 是不同的指针，因为它们需要记录各自的类型参数信息（反射 `typeof(List<string>)` 和 `typeof(List<object>)` 必须返回不同的 `Type` 对象）。但它们的 vtable slots 中的方法入口地址指向同一份 native code。

### __Canon 的 MethodTable 和 EEClass

`__Canon` 本身有一个 MethodTable，但这个 MethodTable 极其精简——它没有实例字段、没有虚方法、不实现任何接口。它的 `m_BaseSize` 等于一个引用类型对象的最小大小（对象头 + MethodTable 指针）。

`__Canon` 的存在意义不在于描述一个真实的类型，而在于作为类型参数的占位符。JIT 在编译 `List<__Canon>` 的方法时，看到的类型参数是 `__Canon`，它知道这意味着"任意引用类型"，因此生成的代码只依赖引用类型的共性——指针大小、引用语义、GC 标记为引用——而不依赖任何具体类型的特征。

## Generic Dictionary

共享代码面临一个问题：代码本身是通用的（操作 `__Canon` 类型的指针），但某些操作需要知道实际的类型参数。比如 `new T()`——共享代码不知道 T 是 `string` 还是 `MyClass`，无法硬编码构造函数的地址。再比如 `typeof(T)`——共享代码需要返回具体类型的 `Type` 对象，而不是 `__Canon` 的。

Generic Dictionary 解决这个问题。它是一个与泛型实例绑定的查找表，存储该实例的具体类型信息。核心实现在 `src/coreclr/vm/genericdict.cpp` 中。

### 存储内容

Generic Dictionary 中的条目包括：

- **Type Handles** — 具体类型参数的 TypeHandle。`List<string>` 的 dictionary 包含 `string` 的 TypeHandle，`List<MyClass>` 的 dictionary 包含 `MyClass` 的 TypeHandle
- **Method Handles** — 泛型方法的具体实例化入口。如果共享代码需要调用另一个泛型方法的特定实例化，目标方法的地址存在 dictionary 中
- **Field Addresses** — 泛型类型的静态字段地址。不同实例化的静态字段位于不同的存储位置
- **Dispatch Stubs** — 接口分派的 VSD stub 地址

### 查找流程

JIT 在编译共享代码时，遇到需要具体类型信息的操作，生成一段从 Generic Dictionary 中查找的代码：

```
共享代码（List<__Canon>.GetType() 的 JIT 编译产物）：

1. 从 this 指针获取 MethodTable
2. 从 MethodTable 获取 Generic Dictionary 指针
3. 从 dictionary[slot_index] 读取 T 的 TypeHandle
4. 使用 TypeHandle 执行 GetType() 操作
```

`slot_index` 是 JIT 在编译时确定的常量。JIT 分析共享方法体中所有需要具体类型信息的位置，为每个位置分配一个 dictionary slot，在代码生成阶段插入从该 slot 读取的指令。

Generic Dictionary 的 slot 初始值可能为空。当共享代码首次访问某个 slot 时，如果该 slot 尚未填充，runtime 触发一次慢路径查找（dictionary lookup slow path），计算出正确的值并填入 slot。后续访问直接读取已填充的 slot，是一次内存读取操作。

## RGCTX（Runtime Generic Context）

Generic Dictionary 解决了泛型类型的上下文问题——每个 `List<T>` 实例的 dictionary 跟着 MethodTable 走。但泛型方法还需要一个额外的机制。

考虑这种情况：

```csharp
class Utils
{
    public static T Clone<T>(T source) where T : ICloneable
    {
        return (T)source.Clone();
    }
}
```

`Clone<T>` 是一个泛型方法，不属于任何泛型类型。它没有 `this` 指针可以追溯到 MethodTable，也就没有 Generic Dictionary 可以查询。RGCTX（Runtime Generic Context）解决这个问题。

### Hidden Parameter

当 JIT 编译一个共享的泛型方法时，它在方法签名中插入一个额外的隐藏参数（hidden parameter）。调用方在调用共享泛型方法时，除了传递显式参数之外，还传递一个 RGCTX 指针。

```
源码层面：
  Utils.Clone<string>(myString)

JIT 生成的调用：
  Utils.Clone__Canon(myString, rgctx_for_string)
                                └── hidden parameter
```

RGCTX 的内容与 Generic Dictionary 类似——存储具体类型的 TypeHandle、方法入口地址等。区别在于 Generic Dictionary 挂在 MethodTable 上（泛型类型的上下文），RGCTX 通过 hidden parameter 传递（泛型方法的上下文）。

### JIT 中的 RGCTX 查询

JIT 在共享代码中插入的 RGCTX 查询遵循固定的模式：

1. 从 hidden parameter 获取 RGCTX 指针
2. 用编译期确定的 slot index 索引 RGCTX 表
3. 如果 slot 已填充，直接使用
4. 如果 slot 为空，调用 runtime helper 填充（`JIT_GenericHandleMethod`）

对于泛型类型中的泛型方法（既有类型级别的 T 又有方法级别的 U），JIT 需要同时查询两层上下文：MethodTable 的 Generic Dictionary 提供 T 的信息，RGCTX 提供 U 的信息。

## 值类型泛型的独立 JIT

前面提到值类型泛型实例必须独立编译。这一节展开具体的编译行为。

当 JIT 遇到 `List<int>.Add(int item)` 时，它为这个方法生成完全独立的 native code。这份代码中：

- 元素大小硬编码为 4 字节（`int` 的大小）
- 数组索引计算直接使用 `index * 4` 而不是 `index * sizeof(T)` 的间接查找
- 方法参数通过寄存器传递（x64 上 `int` 通过整数寄存器传递）
- GC 描述标记该位置不是引用（不需要 GC 追踪）

同一个 `List<double>.Add(double item)` 生成的代码中，元素大小是 8 字节，参数通过浮点寄存器传递，数组偏移计算使用 `index * 8`。两份代码的逻辑结构相同但指令序列完全不同。

独立编译意味着值类型泛型方法不需要 Generic Dictionary——所有类型信息在编译期就已经确定，JIT 可以内联所有类型相关的常量，生成的代码没有任何运行时类型查找开销。这是值类型泛型在数值计算场景中性能优于引用类型泛型的根本原因。

## Constrained Call

值类型上调用接口方法是泛型实现中的一个特殊问题。

```csharp
void PrintHash<T>(T value) where T : struct
{
    Console.WriteLine(value.GetHashCode());
}
```

`T` 被约束为值类型，`GetHashCode()` 是 `object` 上的虚方法。如果按照常规的虚方法调用路径——把值类型 box 成引用类型对象，然后通过 vtable 分派——每次调用都会产生一次堆分配（boxing），这对性能敏感的路径是不可接受的。

ECMA-335 定义了 `constrained.` 前缀指令来解决这个问题。JIT 遇到 `constrained. callvirt` 时，执行以下判断：

1. 如果 T 的具体类型已知（值类型特化路径），直接生成对该值类型 `GetHashCode()` 实现的直接调用，不 boxing
2. 如果在共享代码中（T = `__Canon`），通过 RGCTX 查找具体类型，再判断是否需要 boxing

对于值类型独立编译的路径，JIT 在编译 `PrintHash<int>` 时知道 T = `int`，知道 `int.GetHashCode()` 的具体实现地址，直接生成一个 `call` 指令调用 `Int32.GetHashCode()`——没有 vtable 查找，没有 boxing，没有堆分配。

这个优化在泛型集合的内部实现中非常重要。`Dictionary<TKey, TValue>` 大量使用 `EqualityComparer<TKey>.Default.Equals(key1, key2)`。对于 `Dictionary<int, string>`，JIT 能够将整个调用链内联为一个直接的整数比较指令，完全消除虚方法分派和 boxing 的开销。

## 与 IL2CPP / LeanCLR 的泛型对比

三个 runtime 面对同一个规范（ECMA-335 泛型语义），选择了三条截然不同的实现路径。差异的根源是执行模型不同——JIT 可以按需编译，AOT 必须预判，解释器不生成 native code。

| 维度 | CoreCLR | IL2CPP | LeanCLR |
|------|---------|--------|---------|
| **实例化时机** | 运行时按需，JIT 遇到就编译 | 构建时静态，il2cpp.exe 预判 | 运行时按需，解释器 inflate |
| **"没见过的组合"** | 不存在此问题，现场 JIT | 致命问题，无 native code 可执行 | 不存在此问题，解释执行 |
| **引用类型共享** | `System.__Canon` 作为 canonical 类型 | `__Il2CppFullySharedGenericType`（FGS 模式） | 无代码共享（解释执行，无 native code 可共享） |
| **共享代码的类型查找** | Generic Dictionary + RGCTX | `Il2CppRGCTXData` + runtime metadata | 解释器直接访问 `RtGenericClass` 的类型参数 |
| **值类型处理** | 独立 JIT，硬编码大小和偏移 | 独立 C++ 代码生成 | 解释器根据类型参数动态计算大小 |
| **代码膨胀控制** | 引用类型共享消除膨胀；值类型独立但按需 | 引用类型共享；FGS 进一步扩大共享范围 | 无膨胀（不生成 native code） |
| **性能特征** | 值类型特化性能最优；引用类型有 dictionary 查找开销 | 值类型特化性能接近 CoreCLR；FGS 有额外间接层 | 解释执行，泛型与非泛型无性能差异 |

几个值得展开的差异：

**CoreCLR vs IL2CPP 面对的核心问题不同。** CoreCLR 的泛型难题是"怎么减少重复编译和代码膨胀"——JIT 可以按需编译任何组合，但每份 native code 都占 CodeHeap 内存。`__Canon` 共享机制把引用类型的编译次数从 O(n) 降到 O(1)。IL2CPP 的泛型难题是"怎么保证构建时不遗漏"——如果运行时遇到了 `Dictionary<CustomKey, CustomValue>` 但构建时没有为这个组合生成 C++ 代码，就会抛 `AOT generic method not instantiated`。Full Generic Sharing 通过把更多组合归入共享路径来缓解遗漏风险，但代价是引入了额外的间接层。

**LeanCLR 的泛型处理最简单。** LeanCLR 是纯解释器，不生成 native code，泛型共享在代码层面没有意义。它的泛型实例化只需要在 metadata 层面完成——创建 `RtGenericClass`，绑定具体的类型参数，解释器在执行时直接从 `RtGenericClass` 读取类型参数信息来决定字段大小、方法分派目标。不需要 Generic Dictionary 的间接查找，也不需要 RGCTX 的 hidden parameter——解释器的执行上下文本身就包含所有类型信息。代价是执行性能远低于 JIT 和 AOT，但对于 H5/小游戏的场景，这个代价在可接受范围内。

**Constrained Call 的处理差异。** CoreCLR 在值类型特化路径上可以完全消除 boxing——JIT 知道具体类型，直接生成 `call` 指令。IL2CPP 在 AOT 阶段做类似的优化——il2cpp.exe 为已知的值类型实例化生成直接调用的 C++ 代码。LeanCLR 的解释器在执行 `constrained. callvirt` 时需要运行时检查目标类型是否重写了该方法：如果重写了，直接调用不 boxing；如果没有重写（回退到 `object` 的默认实现），必须 boxing。三者的语义相同，但实现时机不同——编译时优化 vs 运行时判断。

## 收束

CoreCLR 的泛型实现围绕一个核心权衡展开：共享与特化。

**共享路径。** 所有引用类型泛型参数被规范化为 `System.__Canon`，共享同一份 JIT 代码。Generic Dictionary 和 RGCTX 在运行时提供具体类型信息，让共享代码能够执行需要知道实际类型的操作。代价是 dictionary 查找带来的间接层——每次需要类型信息时多一次内存读取。

**特化路径。** 每种值类型组合独立编译，JIT 硬编码所有类型相关的常量。没有 dictionary 查找，没有间接层，性能与手写非泛型代码相当。代价是 CodeHeap 中的代码膨胀。

**桥接机制。** Generic Dictionary 是共享代码与具体类型之间的桥梁，RGCTX 通过 hidden parameter 把泛型方法的类型上下文传入共享代码，Constrained Call 通过编译期类型判断避免值类型上的不必要 boxing。

这套设计在 JIT 按需编译的前提下是最优解——引用类型通过共享大幅减少编译次数和内存占用，值类型通过特化获得最优执行性能，dictionary 和 RGCTX 在两条路径之间提供了统一的类型信息访问机制。IL2CPP 和 LeanCLR 面对不同的执行模型约束，在同样的权衡中做了不同的选择。

## 系列位置

- 上一篇：CLR-B6 异常处理：两遍扫描模型与 SEH 集成
- 下一篇：CLR-B8 线程与同步：Thread、ThreadPool 与同步原语
