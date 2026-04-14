---
title: "IL2CPP 实现分析｜泛型代码生成：共享、特化与 Full Generic Sharing"
date: "2026-04-14"
description: "拆解 IL2CPP 泛型代码生成的三种策略：引用类型共享实例化、值类型独立实例化、Full Generic Sharing 统一路径。覆盖 ABI 层面的共享条件、代码膨胀的量化分析、Il2CppFullySharedGenericAny 的实现机制、supplementary metadata 与 AOTGenericReference 的关系、DisStripCode 的泛型保留策略，以及与 CoreCLR/LeanCLR 泛型处理的对比。"
weight: 54
featured: false
tags:
  - IL2CPP
  - Unity
  - Generics
  - AOT
  - CodeGeneration
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

> 泛型是 IL2CPP 最复杂的问题域。代码膨胀和运行时正确性之间的张力，贯穿了 IL2CPP 泛型处理的全部设计决策。

这是 .NET Runtime 生态全景系列 IL2CPP 模块第 5 篇，承接 D2 中泛型处理一节的展开。

D2 介绍了 il2cpp.exe 的泛型处理策略——引用类型共享代码、值类型独立生成代码。那篇只用了一节篇幅，点出了结论但没有展开原因。D4 在讨论 global-metadata.dat 时，也提到了泛型容器和泛型参数的 section 定义，但没有解释这些 metadata 如何驱动代码生成决策。

这篇把泛型代码生成作为独立主题展开。核心问题只有一个：il2cpp.exe 面对一个泛型定义和 N 种类型参数时，到底生成多少份 C++ 代码、为什么这样选择、Unity 2021+ 的 Full Generic Sharing 又改变了什么。

## 泛型是 IL2CPP 最复杂的问题域

泛型在标准 .NET 运行时（CoreCLR、Mono）中相对透明——JIT 编译器在运行时遇到 `List<int>` 就现场编译一份，遇到 `List<float>` 就再编译一份，不需要预判项目中会使用哪些泛型组合。

IL2CPP 是 AOT 方案，所有代码必须在构建时生成。这意味着 il2cpp.exe 必须在构建阶段就决定：为哪些泛型实例生成代码、哪些实例可以共享、哪些必须独立。任何遗漏都会导致运行时找不到可执行的 native 代码。

这个决策涉及两个相互矛盾的目标：

**减少代码膨胀。** 每个泛型实例化都生成一份完整的 C++ 代码意味着 N 种类型参数产生 N 份几乎相同的代码。包体大小和编译时间都会线性增长。

**保证运行时正确性。** 不同类型参数的泛型实例在内存布局、ABI 调用约定上可能完全不同。如果强行让布局不同的类型共享同一份代码，运行时会产生内存越界、参数传递错误等严重问题。

IL2CPP 的泛型代码生成策略就是在这两个目标之间找到平衡点。

## 引用类型共享实例化

IL2CPP 的第一条规则：所有引用类型参数的泛型实例共享同一份 C++ 代码。

```csharp
// 三个不同的泛型实例
var a = new List<string>();
var b = new List<MyClass>();
var c = new List<object>();
```

il2cpp.exe 只为这三个实例生成一份 C++ 代码——以 `List<object>` 为 canonical 形式。`List<string>` 和 `List<MyClass>` 在 native 层面直接复用 `List<object>` 的实现。

### 为什么引用类型可以共享

在 native 层面，所有引用类型的表示方式完全相同——一个指针。无论是 `string`、`MyClass`、`object` 还是任何自定义的引用类型，它们作为变量、字段或参数时都是同样大小的指针值。

这意味着三件事在 ABI 层面完全一致：

**内存布局相同。** `List<string>` 内部存储元素的数组是 `string[]`，在 native 层面是一个指针数组。`List<MyClass>` 的元素数组在 native 层面同样是指针数组。指针大小相同（64 位平台上都是 8 字节），数组的内存布局完全一致。

**调用约定相同。** 一个接收 `T` 参数的泛型方法，当 `T` 是任意引用类型时，native 层面传递的都是一个指针值。寄存器分配、栈帧布局、返回值传递方式完全一致。

**对象头结构相同。** D3 分析过，每个引用类型对象以 `Il2CppObject` 开头——`klass*` + `monitor*`，16 字节对象头。后面的实例字段布局因类型而异，但泛型代码操作的是 `T` 类型的引用，不直接访问对象内部字段，所以字段布局差异不影响泛型代码的正确性。

看一个具体的转换结果：

```csharp
public class Holder<T>
{
    private T _item;
    public T Get() => _item;
    public void Set(T value) { _item = value; }
}
```

当 `T` 是引用类型时，il2cpp.exe 生成的共享版本：

```cpp
// Holder<object> — canonical 共享实现
struct Holder_1_tSHARED : public RuntimeObject
{
    RuntimeObject* ____item;  // 所有引用类型 T 都是指针
};

RuntimeObject* Holder_1_Get_mSHARED(
    Holder_1_tSHARED* __this,
    const RuntimeMethod* method)
{
    return __this->____item;
}

void Holder_1_Set_mSHARED(
    Holder_1_tSHARED* __this,
    RuntimeObject* ___value,
    const RuntimeMethod* method)
{
    il2cpp_gc_wbarrier_set_field(
        __this, (void**)&__this->____item, ___value);
    __this->____item = ___value;
}
```

`Holder<string>.Get()` 和 `Holder<MyClass>.Get()` 在运行时调用的是同一个 `Holder_1_Get_mSHARED` 函数。类型参数的具体信息通过 `method` 参数中的 `RuntimeMethod*` 携带——如果运行时需要知道 `T` 的实际类型（比如做类型转换检查），就从 `RuntimeMethod` 中取出泛型参数信息。

## 值类型独立实例化

IL2CPP 的第二条规则：每种值类型参数的泛型实例必须独立生成 C++ 代码。

```csharp
var intHolder = new Holder<int>();
var floatHolder = new Holder<float>();
var vec3Holder = new Holder<Vector3>();
```

il2cpp.exe 为这三个实例各生成一份完整的 C++ 代码：

```cpp
// Holder<int>
struct Holder_1_tINT : public RuntimeObject
{
    int32_t ____item;  // 4 字节
};

int32_t Holder_1_Get_mINT(
    Holder_1_tINT* __this,
    const RuntimeMethod* method)
{
    return __this->____item;
}

// Holder<float>
struct Holder_1_tFLOAT : public RuntimeObject
{
    float ____item;  // 4 字节，但类型不同
};

float Holder_1_Get_mFLOAT(
    Holder_1_tFLOAT* __this,
    const RuntimeMethod* method)
{
    return __this->____item;
}

// Holder<Vector3>
struct Holder_1_tVEC3 : public RuntimeObject
{
    Vector3_t ____item;  // 12 字节 (x, y, z)
};

Vector3_t Holder_1_Get_mVEC3(
    Holder_1_tVEC3* __this,
    const RuntimeMethod* method)
{
    return __this->____item;
}
```

### 为什么值类型不能共享

值类型之间的差异体现在两个层面：

**内存布局不同。** `int` 占 4 字节，`double` 占 8 字节，`Vector3` 占 12 字节，一个自定义的大 struct 可能占数百字节。这直接影响了包含 `T` 字段的结构体大小——`Holder<int>` 的实例是 `16（对象头）+ 4 = 20` 字节，`Holder<Vector3>` 的实例是 `16 + 12 = 28` 字节。GC 分配时需要的大小不同，字段的偏移量不同。

**ABI 参数传递不同。** C/C++ 的调用约定对不同大小的值类型有不同的处理方式。4 字节的 `int` 通常通过寄存器传递；12 字节的 `Vector3` 在某些平台上通过寄存器传递，在某些平台上通过栈传递；更大的 struct 几乎总是通过指针传递。如果 `Holder<int>.Get()` 和 `Holder<Vector3>.Get()` 共享同一个函数，返回值的传递方式就会不匹配——调用方按 4 字节读，被调用方按 12 字节写，产生内存越界。

这不是理论上的风险，而是 ABI 层面的硬约束。两个值类型哪怕只差一个字节的对齐方式，都可能导致参数传递错误。所以 IL2CPP 对值类型采取了最保守的策略：每种值类型参数组合都独立生成完整的 C++ 代码。

## 泛型代码膨胀的量化

独立实例化意味着代码量和值类型参数的组合数成正比。

考虑一个泛型方法集合 M 和一组值类型 V。如果每个泛型方法都被每种值类型实例化，总代码量是 |M| x |V|。

一个典型例子是 `Dictionary<TKey, TValue>`。这个类型内部包含多个嵌套类型和方法：

```csharp
public class Dictionary<TKey, TValue>
{
    private struct Entry  // 内部结构体
    {
        public int hashCode;
        public int next;
        public TKey key;
        public TValue value;
    }

    public struct Enumerator  // 枚举器
    {
        // ...
    }

    // 数十个方法：Add, Remove, TryGetValue,
    // ContainsKey, GetEnumerator, Resize...
}
```

假设项目中使用了以下四种 `Dictionary` 实例：

| 实例 | TKey | TValue | 共享情况 |
|------|------|--------|---------|
| `Dictionary<int, string>` | 值类型 | 引用类型 | TKey 独立，TValue 共享 |
| `Dictionary<int, float>` | 值类型 | 值类型 | 都独立 |
| `Dictionary<string, int>` | 引用类型 | 值类型 | TKey 共享，TValue 独立 |
| `Dictionary<long, object>` | 值类型 | 引用类型 | TKey 独立，TValue 共享 |

只要类型参数组合中包含不同的值类型，就需要独立生成。上面四种组合中，`Dictionary<int, string>` 和 `Dictionary<long, object>` 虽然 TValue 方向可以共享，但 TKey 不同（`int` vs `long`），仍然需要各自独立生成 `Entry`、`Enumerator` 和所有方法的完整 C++ 代码。

一个 `Dictionary` 实例化的完整 C++ 代码量在千行量级。四种实例化可能产出数千行代码，而这只是一个容器类型。真实项目中，`List<T>`、`HashSet<T>`、`Queue<T>`、`Action<T>`、`Func<T,TResult>` 等泛型类型被不同值类型参数实例化的组合数可能达到数百种。

GenericMethods.cpp 文件——il2cpp.exe 输出目录中存放所有泛型方法实例化的文件——在大型项目中可能超过十万行。这是 IL2CPP 构建时间长和包体大的主要原因之一。

## Full Generic Sharing（Unity 2021+）

Unity 2021.3 LTS 引入了 Full Generic Sharing（全泛型共享），根本性地改变了 IL2CPP 的泛型代码生成策略。

### 核心思路

Full Generic Sharing 的目标是打破"值类型必须独立实例化"的限制。它引入了一个 canonical 类型 `Il2CppFullySharedGenericAny`，让所有泛型参数——无论引用类型还是值类型——都统一到同一份 C++ 代码上。

传统策略下，`Holder<int>`、`Holder<float>`、`Holder<Vector3>` 各自需要独立的代码。Full Generic Sharing 下，这三者加上所有引用类型实例共享同一份实现。

### 实现机制

Full Generic Sharing 的核心策略是把所有泛型参数统一为指针大小的值来传递。对于本身就是指针的引用类型，这没有任何变化。对于值类型，需要额外的间接层：

**值类型 boxing。** 当一个值类型的值需要通过共享的泛型方法传递时，运行时将其装箱（boxing）为堆上的引用类型对象，或者通过指针间接传递。这样无论 `T` 是 `int`（4 字节）还是 `Vector3`（12 字节），调用约定都统一为传递一个指针。

**ConstrainedCall。** 对泛型参数 `T` 调用方法时，编译器不能直接知道目标方法的地址。Full Generic Sharing 使用 constrained call 机制——在运行时通过 `RuntimeMethod` 中携带的类型参数信息，查找实际的方法实现并调用。

概念上的代码路径变化：

```cpp
// 传统策略：Holder<int>.Get() — 直接返回值
int32_t Holder_1_Get_mINT(Holder_1_tINT* __this, ...) {
    return __this->____item;
}

// Full Generic Sharing：统一路径
void Holder_1_Get_mFGS(
    Holder_1_tFGS* __this,
    Il2CppFullySharedGenericAny* il2cppRetVal,
    const RuntimeMethod* method)
{
    // 从 method 获取 T 的实际类型信息
    const Il2CppType* T_type = method->genericTypeArguments[0];
    uint32_t T_size = il2cpp_type_get_size(T_type);

    // 按实际大小从 __this 中拷贝字段值到返回缓冲区
    memcpy(il2cppRetVal,
           (uint8_t*)__this + field_offset,
           T_size);
}
```

关键变化在于：返回值不再通过函数返回值传递，而是通过输出参数（指针）传递。字段访问不再是编译期固定偏移的直接读取，而是运行时根据类型参数计算偏移量后的内存拷贝。

### 代价

Full Generic Sharing 不是免费的优化，它用运行时开销换取了代码体积缩减：

**间接调用开销。** 传统策略下，`Holder<int>.Get()` 是一个直接返回 `int32_t` 的函数，C++ 编译器可以内联。Full Generic Sharing 下，同样的操作变成了一次 `memcpy`、一次类型信息查询，无法被编译器内联优化。

**值类型 boxing。** 某些场景下值类型需要被装箱为堆对象再传递。Boxing 意味着一次 GC 分配和一次内存拷贝，对 GC 敏感的游戏场景来说这是需要关注的开销。

**运行时分支。** 共享代码中需要根据 `T` 的实际类型做不同处理（值类型 vs 引用类型的赋值语义不同），增加了运行时的分支判断。

这是一个典型的 space-time trade-off：缩减了 native 代码体积和编译时间，增加了运行时的 CPU 开销。对于包体大小敏感的移动平台项目，这个 trade-off 通常是值得的。对于帧率极度敏感的性能热点路径，可能需要评估。

### 与传统策略的共存

Full Generic Sharing 不是一个非此即彼的开关。Unity 2021+ 的 IL2CPP 在实际构建中采用混合策略：

- 对于在 AOT 编译期间可见的泛型实例，仍然可以生成专用的（specialized）代码
- Full Generic Sharing 主要服务于那些在编译期未见但运行时需要的泛型实例——提供一条保底的执行路径

这意味着一个已知的 `List<int>` 实例仍然可能走传统的独立实例化路径以获得最优性能，而一个通过反射或热更代码在运行时构造的 `List<SomeNewStruct>` 则走 Full Generic Sharing 路径保证能执行。

## 补充 metadata 与 AOTGenericReference

泛型代码生成面临的另一个结构性问题是：il2cpp.exe 只能为它在静态分析中发现的泛型实例生成代码。

### 构建时不可见的泛型实例

考虑以下场景：AOT 代码中使用了 `List<int>` 和 `List<string>`，il2cpp.exe 为它们生成了 C++ 代码。但热更新代码中使用了 `List<MyNewStruct>`——这个类型在构建时不存在。

原生 IL2CPP 在这种情况下会在运行时抛出异常，因为没有对应的 native 实现可执行。

### HybridCLR 的解决路径

HybridCLR 通过两条路径解决这个问题：

**补充 metadata（supplementary metadata）。** 通过 `LoadMetadataForAOTAssembly` 加载补充 metadata，让运行时能够"看见"热更程序集中定义的新类型。但补充 metadata 解决的是"运行时能不能识别这个类型"，不是"有没有 AOT 编译出的 native 代码"。

**AOTGenericReference。** 在 AOT 代码中显式引用需要保留的泛型实例。il2cpp.exe 看见了引用就会生成代码。这解决了"native 实现存不存在"的问题。HybridCLR 的 `Generate/AOTGenericReference` 工具分析热更代码，找出所有被使用的泛型实例，自动生成引用代码。

两条路径各自解决不同层的问题：

```
层 1：metadata 可见性 → 补充 metadata
层 2：native 代码存在性 → AOTGenericReference / DisStripCode
```

Full Generic Sharing 的引入改变了这个格局——它提供了一条不需要预先 AOT 编译的保底路径。在 Full Generic Sharing 可用的情况下，即使 AOTGenericReference 中没有显式保留某个值类型泛型实例，运行时也能通过共享路径执行。代价是性能不如专用实例化。

## DisStripCode 的泛型保留策略

DisStripCode 是在 AOT 构建中显式写出泛型引用的代码文件，确保特定泛型实例在构建时被 il2cpp.exe 看见并生成代码。它的保留策略遵循 IL2CPP 的共享规则：

**引用类型参数使用 `object` 替代。** 由于所有引用类型共享同一份代码（canonical 形式是 `object`），DisStripCode 中不需要为每种引用类型分别写条目。写一个 `new List<object>()` 就能保住 `List<string>`、`List<MyClass>` 等所有引用类型实例。

**值类型参数必须精确保留。** `List<int>` 和 `List<float>` 的代码完全独立，保住 `List<int>` 不意味着 `List<float>` 也被保住了。DisStripCode 中需要为每种值类型组合分别写条目。

```csharp
// DisStripCode 示例
public class AOTGenericReferences : MonoBehaviour
{
    void Preserve()
    {
        // 引用类型：一个 object 实例保住所有引用类型
        new List<object>();
        new Dictionary<object, object>();

        // 值类型：每种组合都要写
        new List<int>();
        new List<float>();
        new List<long>();
        new Dictionary<int, object>();
        new Dictionary<int, float>();
        new Dictionary<long, object>();
    }
}
```

一个容易犯的错误是只保留了类型实例化，没有保留方法实例化。泛型方法（如 `Enumerable.Select<TSource, TResult>`）的保留需要显式调用该方法，而不只是引用包含它的类型。HybridCLR 系列 HCLR-21 对 DisStripCode 的写法模式有详细讨论。

## IL2CPP vs CoreCLR vs LeanCLR 的泛型处理对比

三个 runtime 对泛型的处理策略反映了各自的核心设计约束。

### CoreCLR：JIT 驱动的按需生成

CoreCLR 的泛型处理由 JIT 编译器在运行时完成：

- 首次遇到 `List<int>` 时，JIT 编译器现场编译一份专用的机器码
- 引用类型实例共享代码（和 IL2CPP 策略相同），canonical 类型是 `System.__Canon`
- 值类型实例独立编译（和 IL2CPP 策略相同）
- 永远不会遇到"泛型实例缺失"问题——任何泛型组合都可以在运行时现场生成

CoreCLR 的泛型共享策略和 IL2CPP 在概念上一致——引用类型共享、值类型特化。核心差异在时机：CoreCLR 在运行时按需生成，永远不会遗漏；IL2CPP 在构建时批量生成，存在遗漏的可能。

### LeanCLR：解释执行的天然灵活性

LeanCLR 作为纯解释器，不需要生成 native 代码。泛型膨胀（generic inflation）发生在 metadata 层面：

- `Method::inflate` 根据泛型上下文膨胀出具体的方法签名
- 膨胀后的方法签名指向同一份 IL 字节码，解释器在执行时根据类型参数做相应处理
- 不存在代码膨胀问题——没有 native 代码生成，只有 metadata 层的类型签名膨胀
- 代价是解释执行的性能远低于 AOT 或 JIT 编译的 native 代码

LeanCLR 的泛型处理和 IL2CPP 形成了两个极端：IL2CPP 用代码膨胀换取 native 性能，LeanCLR 用解释执行的性能损失换取零膨胀和完全灵活性。

### 对比总结

| 维度 | IL2CPP（传统） | IL2CPP（FGS） | CoreCLR | LeanCLR |
|------|---------------|---------------|---------|---------|
| 代码生成时机 | 构建时 | 构建时 | 运行时（JIT） | 无 |
| 引用类型共享 | 共享（object） | 共享（object） | 共享（__Canon） | 共享（同一份 IL） |
| 值类型处理 | 独立实例化 | 统一共享路径 | 独立 JIT 编译 | 解释执行 |
| 代码膨胀 | N 种值类型 = N 份代码 | 1 份共享代码 | N 份（在内存中） | 无 |
| 运行时灵活性 | 仅限构建时可见 | 保底路径可用 | 任意组合 | 任意组合 |
| 值类型性能 | 最优（直接访问） | 有间接开销 | 最优（JIT 优化） | 最低（解释执行） |

## 收束

IL2CPP 的泛型代码生成经历了三个阶段的演化：

**阶段一：引用类型共享 + 值类型独立。** 这是 IL2CPP 从诞生之初就确定的基本策略。引用类型在 native 层面都是指针，内存布局和 ABI 调用约定一致，可以安全共享。值类型布局各不相同，必须独立生成。这个策略在正确性上没有问题，但代码膨胀是固有代价。

**阶段二：AOTGenericReference 和 supplementary metadata。** HybridCLR 引入热更新后，"构建时不可见的泛型实例"成为新问题。通过 AOTGenericReference 在 AOT 侧显式保留、通过补充 metadata 在运行时补充类型可见性，两条路径分别解决 native 代码存在性和 metadata 可见性。

**阶段三：Full Generic Sharing。** Unity 2021+ 引入 `Il2CppFullySharedGenericAny`，把所有泛型参数统一到指针大小的传递路径上。值类型通过间接传递和运行时类型查询来处理布局差异。这从根本上缓解了代码膨胀问题，也为 HybridCLR 场景提供了保底执行路径——不再强依赖每个值类型泛型实例都预先 AOT 生成。

这三个阶段的核心张力始终未变：代码膨胀与运行时性能的 trade-off。传统策略倾向于牺牲体积保性能，Full Generic Sharing 倾向于牺牲性能保体积和灵活性。具体项目中选择哪种策略，取决于包体预算和性能热点分布。

IL2CPP 模块到此五篇完成，覆盖了从管线架构（D1）到转换器内部（D2）、运行时基础设施（D3）、metadata 格式（D4）、泛型代码生成（D5）的完整链路。

---

**系列导航**

- 系列：.NET Runtime 生态全景系列 — IL2CPP 模块
- 位置：IL2CPP-D5（模块完结篇）
- 上一篇：[IL2CPP-D4 global-metadata.dat：格式、加载与 runtime 的绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}})

**相关阅读**

- [IL2CPP-D1 架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})
- [IL2CPP-D2 il2cpp.exe 转换器：IL → C++ 代码生成策略]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}})
- [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})
- [HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版]({{< relref "engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}})
- [HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}})
- [LeanCLR 源码分析｜类型系统：泛型膨胀、接口分派与值类型判断]({{< relref "engine-toolchain/leanclr-type-system-generic-inflation-interface-dispatch.md" >}})
- [ECMA-335 基础｜CLI Type System：值类型 vs 引用类型、泛型、接口、约束]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
- [横切对比｜类型系统实现：MethodTable vs Il2CppClass vs RtClass]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})
