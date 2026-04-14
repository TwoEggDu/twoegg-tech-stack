---
title: "横切对比｜泛型实现：共享 vs 特化 vs Full Generic Sharing"
date: "2026-04-14"
description: "同一个 ECMA-335 泛型模型（TypeSpec / MethodSpec），在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里走出五种完全不同的泛型实例化策略：JIT 按需实例化、AOT 静态实例化、引用类型共享、Full Generic Sharing、运行时泛型膨胀。从实例化时机、共享策略、缺失处理到代码膨胀，逐维度横切对比五种泛型实现的设计 trade-off。"
weight: 84
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "HybridCLR"
  - "LeanCLR"
  - "Generics"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> JIT runtime 遇到一个没见过的泛型实例化，现场编译一份就行了。AOT runtime 遇到同样的情况——崩溃。泛型不是一个独立的语言特性问题，它是 JIT 和 AOT 执行策略在运行时能力上最尖锐的分野。

这是 .NET Runtime 生态全景系列的横切对比篇第 5 篇，也是 Phase 2 横切对比的收尾篇。

CROSS-G4 对比了 GC 实现——五个 runtime 怎么回收不可达对象。但 GC 解决的是对象的"死亡"问题。泛型面对的是对象的"诞生"问题——一个泛型类型定义只有一份，但运行时可能需要数十甚至数百种不同的封闭类型实例。五个 runtime 怎么从一份泛型定义产生这些实例，路线差异巨大。

## ECMA-335 的泛型模型

ECMA-335 Partition II 定义了 .NET 泛型的两个核心概念：开放类型和封闭类型。

### TypeSpec 与 MethodSpec

泛型类型定义（如 `List<T>`）在 metadata 中是一条 TypeDef 记录，带有泛型参数列表（GenericParam 表）。泛型方法定义（如 `Array.Sort<T>`）在 metadata 中是一条 MethodDef 记录，同样带有泛型参数。

当代码使用一个具体的泛型实例化（如 `List<int>`、`Dictionary<string, Player>`）时，metadata 用 TypeSpec 来描述这个封闭类型——TypeSpec 引用原始的泛型定义，加上具体的类型参数。方法级的泛型实例化（如 `Sort<int>`）用 MethodSpec 描述。

### 开放类型 vs 封闭类型

**开放类型**（open type）：至少有一个类型参数未被绑定。比如 `List<T>` 本身就是开放类型——`T` 还没有确定。开放类型不能被实例化，不能创建对象。

**封闭类型**（closed type）：所有类型参数都已绑定为具体类型。`List<int>` 是封闭类型——`T` 被绑定为 `int`。只有封闭类型才能被实例化。

规范只定义了这些语义，不规定 runtime 怎么从开放类型产生封闭类型的运行时表示。每个 runtime 自行决定：什么时候实例化、为每种封闭类型生成独立实现还是共享实现、运行时遇到未预见的封闭类型怎么处理。

这些决策的差异，构成了五个 runtime 泛型实现最核心的分化点。

## CoreCLR — JIT 按需实例化 + 引用类型共享

CoreCLR 的泛型策略可以用一句话概括：**首次遇到任何封闭类型时，JIT 现场处理，不存在"没见过"的问题。**

### 按需实例化

当代码第一次使用 `List<PlayerData>`（假设 `PlayerData` 是值类型）时，CoreCLR 的流程是：

1. 类型加载器检测到 `List<PlayerData>` 的 TypeSpec
2. 查找 `List<T>` 的泛型定义（TypeDef）
3. 用 `PlayerData` 替换 `T`，构建一个新的 MethodTable——字段偏移、实例大小、vtable 都按 `PlayerData` 的实际大小重新计算
4. 当 `List<PlayerData>` 的方法首次被调用时，RyuJIT 为该方法生成专门针对 `PlayerData` 的 native code

整个过程在运行时完成。不需要在构建时预见所有可能的泛型组合——用到的时候 JIT 编译就行了。

这意味着 CoreCLR 永远不会遇到"泛型缺失"问题。任何合法的泛型组合，只要代码执行路径到达，JIT 都能处理。

### 引用类型共享

但如果每个封闭类型都生成独立的 native code，代码膨胀会很严重。`List<string>`、`List<object>`、`List<MyClass>` 的方法逻辑完全相同——区别只在元素存储的大小和 GC 追踪需求。而所有引用类型的大小都一样（一个指针大小），GC 追踪方式也一样（都是引用类型字段）。

CoreCLR 利用这个特性做引用类型共享（reference type sharing）：所有引用类型泛型实例化共享同一份 JIT 生成的 native code。

```
List<string>  ─┐
List<object>  ─┼→ 共享同一份 native code（按 object 编译）
List<MyClass> ─┘

List<int>     → 独立的 native code（int 是值类型，大小不同）
List<double>  → 独立的 native code（double 是值类型，大小不同）
```

共享代码中需要知道实际类型参数的地方（比如类型检查、创建新实例），通过运行时泛型上下文（Runtime Generic Context, RGCTX）传递。RGCTX 是一个隐式参数，携带实际类型参数的信息。

### 值类型必须特化

值类型不能共享，因为不同值类型的大小不同。`List<int>` 的元素是 4 字节内联存储，`List<Vector3>` 的元素是 12 字节内联存储——如果共享同一份代码，数组索引、内存复制、字段访问的偏移全部算错。

所以 CoreCLR 为每种值类型参数生成独立的 native code。`List<int>` 和 `List<long>` 有各自独立的 JIT 编译结果。

这种共享策略的效果是：引用类型泛型实例化的代码膨胀为零（全共享），值类型泛型实例化按需生成（代码膨胀 = 实际使用的值类型参数种数）。

## Mono — JIT 实例化 + Full AOT 的困境

Mono 在 JIT 模式下的泛型策略和 CoreCLR 高度相似：按需实例化 + 引用类型共享。Mini JIT 遇到新的泛型组合时现场编译。

### JIT 模式：和 CoreCLR 一致

在允许 JIT 的平台上（桌面 Linux、Android 等），Mono 的泛型处理和 CoreCLR 没有本质差异。首次遇到 `List<PlayerData>` 时，Mini JIT 编译一份专用代码。引用类型共享一份代码，值类型各自特化。不存在泛型缺失问题。

### Full AOT 模式：必须预见所有实例

在禁止 JIT 的平台上（iOS、游戏主机），Mono 进入 Full AOT 模式。所有方法必须在构建时预编译成 native code，运行时不能再 JIT 新代码。

这时泛型实例化变成了构建时的任务。`mono --aot` 在编译时遍历所有 DLL，找出代码中显式使用的泛型实例化，为每种组合生成 native code。

问题在于：构建时能找到的泛型实例化是有限的。如果代码通过反射或动态方式在运行时构造了一个构建时没见过的泛型组合——这个组合没有对应的 native code，无法执行。

Mono 在 Full AOT 模式下的 fallback 是解释器。如果解释器可用，未预编译的泛型实例化回退到解释执行。如果解释器也不可用（纯 Full AOT），就是运行时错误。

### generic sharing 在 AOT 中的扩展

Mono AOT 也做了引用类型共享——构建时为 `List<object>` 生成一份 AOT native code，运行时所有 `List<SomeRefType>` 共享这份代码。这大幅减少了引用类型泛型组合的覆盖压力。

Mono 后续版本（Unity 使用的 Mono 分支）还实现了一定程度的 Full Generic Sharing——把共享范围从"引用类型参数之间共享"扩展到"部分值类型参数也能共享"。但这方面 IL2CPP 的 FGS 走得更远。

## IL2CPP — 构建时静态实例化 + 引用类型共享 + FGS

IL2CPP 是纯 AOT runtime，没有 JIT，没有解释器。泛型实例化必须在构建时全部完成——这让泛型成为了 IL2CPP 生态最持久的痛点。

### 构建时实例化

il2cpp.exe 在构建期间遍历所有 DLL 的 metadata，收集所有在代码中显式出现的泛型实例化。对每一种封闭类型和封闭方法，生成独立的 C++ 代码：

```cpp
// 为 List<int> 生成的 C++ 方法
void List_1_Add_m54321(List_1_int_o* __this, int32_t item,
                        const MethodInfo* method) { ... }

// 为 List<string> 生成的 C++ 方法（和 List<object> 共享）
void List_1_Add_m12345(List_1_object_o* __this, Il2CppObject* item,
                        const MethodInfo* method) { ... }
```

引用类型共享同一份实现（和 CoreCLR 的策略一致），值类型各自独立。

### 构建时没见过 = 缺失

IL2CPP 的核心限制是：**构建时没有出现在代码中的泛型组合，运行时不存在对应的 native code。**

```csharp
// 构建时代码中有 List<int>，生成了对应的 C++ 代码
var list1 = new List<int>();  // OK

// 构建时代码中从未出现 List<MyHotfixType>
// 运行时尝试使用 → 崩溃
var list2 = new List<MyHotfixType>();  // AOT generic method not instantiated
```

错误信息是 `AOT generic method not instantiated in aot`——这不是 metadata 看不见的问题，而是 native code 不存在的问题。

对于纯 IL2CPP 项目（不用 HybridCLR），这个限制的影响有限——所有代码都在构建时确定，所有泛型组合都可以被静态发现。问题出现在引入热更新之后：热更新代码可能使用构建时不存在的类型作为泛型参数。

### Full Generic Sharing（Unity 2021+）

Unity 在 IL2CPP 中引入了 Full Generic Sharing（FGS），把泛型代码共享的范围从"仅引用类型"扩展到"所有类型参数"。

传统共享只针对引用类型——`List<string>` 和 `List<MyClass>` 共享一份代码，但 `List<int>` 和 `List<float>` 必须各自独立。原因是值类型的大小不同。

FGS 的核心思路是：为泛型方法生成一份通用代码，通过运行时泛型上下文（RGCTX）传递类型参数的大小和操作信息。值类型参数不再按大小内联，而是通过间接方式访问。

```
传统共享：
  List<string>  → 共享 object 实现
  List<MyClass> → 共享 object 实现
  List<int>     → 独立 int 实现
  List<float>   → 独立 float 实现

FGS：
  List<string>  ─┐
  List<MyClass> ─┤
  List<int>     ─┼→ 共享一份 FGS 通用实现
  List<float>   ─┘
```

FGS 的收益：即使构建时没见过 `List<SomeNewType>`，只要有 FGS 通用实现，运行时就能通过 RGCTX 提供 `SomeNewType` 的信息来执行。这大幅减少了 AOT 泛型缺失问题的发生概率。

FGS 的代价：通用代码路径比特化代码慢——值类型访问多了一层间接（通过 RGCTX 查大小和操作），编译器无法做针对特定类型的优化。对于热点泛型方法，FGS 的性能开销可以观测。

## HybridCLR — 补充 metadata + AOTGenericReference + FGS fallback

HybridCLR 面对的泛型问题是 IL2CPP 的直接遗产——AOT 类型的泛型实例在构建时确定，热更类型引入了构建时不可预见的泛型组合。HybridCLR 的解决方案是一套多层防线。

### 第一层：补充 metadata 解决可见性

热更 DLL 加载后，HybridCLR 通过 `InterpreterImage` 把热更类型的 metadata 注入到 IL2CPP 的类型系统中。这解决的是"runtime 能不能看见这个类型"的问题——metadata 不足时，runtime 无法解析 TypeSpec / MethodSpec，根本不知道 `List<HotfixType>` 是什么。

补充 metadata 让 runtime 能看懂所有类型引用。但"看懂"不等于"能执行"——还需要对应的 native code 或解释器路径。

### 第二层：AOTGenericReference 显式化缺口

HybridCLR 提供了 `AOTGenericReferences`工具——在构建前生成一份清单，列出热更代码中用到的所有泛型实例化。开发者检查这份清单，确认哪些泛型组合在 AOT 侧已有实现、哪些缺失。

缺失的组合有两种处理方式：

**方式一：在 AOT 侧手动实例化。** 在 AOT 程序集中显式写一个永远不会执行的方法，引用目标泛型组合，让 IL2CPP 构建时为它生成 native code。

```csharp
// DisStripCode.cs（放在 AOT 程序集中）
void UsedOnlyForStripping() {
    // 强制 IL2CPP 生成 List<HotfixType> 的 native 代码
    new List<HotfixType>();
    // 引用类型可以统一写 object（因为共享）
    new Dictionary<string, object>();
}
```

**方式二：依赖 FGS fallback。** 如果 IL2CPP 开启了 FGS，缺失的泛型组合可以走 FGS 通用实现，不需要手动实例化。

### 第三层：FGS fallback

在 Unity 2021+ 且开启了 FGS 的项目中，HybridCLR 的泛型缺失问题大幅缓解。大部分构建时未见过的泛型组合可以走 FGS 通用代码路径执行。

但 FGS 不是万能的。某些复杂的泛型嵌套（泛型方法中调用另一个泛型方法，且类型参数来自外层泛型）可能超出 FGS 的共享范围。这些情况仍然需要手动实例化或在热更代码中通过解释器直接执行。

### 解释器直接执行

对于热更代码中的泛型方法，HybridCLR 的解释器可以直接执行——不需要 native code，解释器 transform 时根据实际类型参数展开指令。这是解释器在泛型问题上的天然优势：解释器不需要预编译的 native code，CIL 字节码本身就是泛型的完整描述。

## LeanCLR — 运行时泛型膨胀

LeanCLR 的泛型策略是五个 runtime 中最直接的：运行时遇到泛型实例化时，当场膨胀（inflate）出具体实例。

### RtGenericClass 与 Method::inflate

当 LeanCLR 首次遇到 `List<int>` 时：

1. 找到 `List<T>` 的 RtClass 定义
2. 创建 RtGenericClass，绑定 `T = int`
3. 重新计算字段偏移、实例大小（用 `int` 替换 `T` 的占位大小）
4. vtable 中的每个方法保持为 RtMethodInfo 引用

当 `List<int>.Add` 首次被调用时：

1. 找到 `Add` 方法的泛型定义
2. `Method::inflate` 用 `int` 替换类型参数 `T`
3. 生成 inflate 后的 RtMethodInfo
4. 首次执行时做 HL-IL / LL-IL transform，transform 过程中类型参数已经是具体的 `int`

### 不做共享

LeanCLR 当前不做引用类型共享。`List<string>` 和 `List<MyClass>` 各自独立 inflate。每次泛型调用都走完整的 inflate 路径，不缓存共享实例。

这个策略在性能上不是最优的——每次 inflate 都有计算开销。但对于解释器来说，inflate 的开销相对于解释执行本身不是瓶颈。一次 inflate 可能花几十微秒，而解释执行一个方法可能花数百微秒到毫秒级。

不做共享的收益是实现简单。引用类型共享需要 RGCTX 机制来在运行时传递实际类型参数，这会增加类型系统和解释器的复杂度。LeanCLR 选择在早期阶段保持实现的简单性，把共享优化留到性能成为瓶颈时再引入。

### 和 CoreCLR 的相似性

LeanCLR 的泛型膨胀在概念上和 CoreCLR 最接近——都是运行时按需实例化，都不存在"构建时没见过"的问题。差异在于最后一步：CoreCLR 用 JIT 为膨胀后的方法生成 native code，LeanCLR 用双层 transform 生成 LL-IL 指令序列供解释器执行。

这意味着 LeanCLR 和 CoreCLR 一样，永远不会遇到 AOT 泛型缺失问题。任何合法的泛型组合，运行时 inflate 就能得到完整的可执行表示。

## 五方对比表

| 维度 | CoreCLR | Mono | IL2CPP | HybridCLR | LeanCLR |
|------|---------|------|--------|-----------|---------|
| 实例化时机 | 运行时 JIT 按需 | JIT 按需 / AOT 构建时 | 构建时静态 | AOT 构建时 + 热更运行时 | 运行时 inflate |
| 共享策略 | 引用类型共享，值类型特化 | 同 CoreCLR (JIT) / 扩展共享 (AOT) | 引用类型共享 + FGS (2021+) | AOT 侧同 IL2CPP，热更侧解释器直接执行 | 不共享，逐次 inflate |
| 缺失处理 | 不存在缺失 | JIT 无缺失 / AOT 解释器 fallback | 运行时错误 (无 FGS) / FGS fallback | metadata 补充 + AOTGenericRef + FGS + 解释器 | 不存在缺失 |
| FGS 支持 | 不需要（JIT 按需） | 有限支持 | Unity 2021+ | 依赖 IL2CPP FGS | 不需要（运行时 inflate） |
| 代码膨胀 | 值类型参数按种类膨胀 | 同 CoreCLR (JIT) / AOT 全量 | 值类型按种类 + FGS 通用 | AOT 侧同 IL2CPP | 无 native code 膨胀 |
| 运行时开销 | JIT 编译 + RGCTX 查找 | JIT 编译 / AOT 零开销 | AOT 零开销 / FGS 间接开销 | AOT 零开销 / 解释器 transform | inflate + transform |
| RGCTX | 有，传递类型参数信息 | 有 | 有（FGS 模式下） | AOT 侧有 / 热更侧不需要 | 无 |

## 为什么泛型是 AOT runtime 的天然难题

泛型在五个 runtime 中的实现差异，本质上是 JIT 和 AOT 在"运行时能力"上的分野。

### JIT 的天然优势

JIT runtime（CoreCLR、Mono JIT 模式）面对泛型没有结构性困难。泛型的本质是"一份定义，多种实例化"——JIT 编译器在运行时按需为每种实例化生成代码。泛型参数是什么类型，JIT 就编译什么类型。不需要预见，不需要覆盖，不需要共享优化（虽然共享优化仍然值得做，为了减少代码膨胀）。

### AOT 必须预见所有实例

AOT runtime（IL2CPP、Mono Full AOT）面对泛型有结构性困难。构建时必须确定所有需要的 native code，但泛型的组合空间是开放的——一个 `Dictionary<TKey, TValue>` 配上 N 种 key 类型和 M 种 value 类型，就有 N*M 种组合。

静态分析能覆盖代码中显式出现的组合。但通过反射构造的泛型实例、通过泛型约束间接产生的组合、通过热更新引入的新类型——这些在构建时不可见。

### 解释器的第三条路

解释器（HybridCLR、LeanCLR）提供了 JIT 和 AOT 之外的第三条路线。解释器的输入是 CIL 字节码，CIL 本身携带了泛型的完整描述（TypeSpec / MethodSpec）。解释器在 transform 或执行时根据实际类型参数做类型特化，不需要预编译的 native code。

这意味着解释器和 JIT 一样不存在泛型缺失问题，但代价是解释执行的性能远低于 native code。

### FGS：AOT 的自救

Full Generic Sharing 是 AOT runtime 在不引入 JIT 或解释器的前提下缓解泛型缺失的策略。它的思路是：与其为每种封闭类型生成独立代码，不如生成一份通用代码，通过运行时参数传递类型信息。

这本质上是在 native code 层面模拟解释器的行为——native code 不再假设它知道类型参数的具体类型，而是通过 RGCTX 动态查询。代价是通用代码路径比特化代码路径慢，因为多了间接访问和类型判断。

FGS 不能完全消除泛型缺失问题——某些极端情况下（深层嵌套泛型、泛型约束组合）仍可能超出 FGS 的覆盖范围。但它把"需要手动处理的泛型缺失"从"常见"降低到了"罕见"。

### 代码膨胀的三角约束

泛型实现面临一个三角约束：代码膨胀、运行时性能、运行时灵活性。

**全特化（CoreCLR 值类型 / IL2CPP 无 FGS）：** 每种封闭类型一份代码。性能最好（特化代码无间接开销），灵活性最低（AOT 下必须预见），膨胀最大。

**共享（引用类型共享 / FGS）：** 多种封闭类型共享一份代码。膨胀小，灵活性中（FGS 能覆盖大部分未预见组合），性能有间接开销。

**解释器：** 零 native code 膨胀。灵活性最高（任何合法组合都能执行），性能最低（解释执行）。

没有一种策略能同时在三个维度上最优。CoreCLR 通过 JIT 按需特化 + 引用类型共享在性能和膨胀之间取得了平衡，但依赖 JIT 能力。IL2CPP + FGS 在不依赖 JIT 的前提下尽可能减少膨胀和缺失，但 FGS 路径有性能代价。HybridCLR 用多层防线（AOT 特化 + FGS + 解释器 fallback）覆盖所有情况，但工程复杂度最高。

## 收束

同一个 ECMA-335 泛型模型，五个 runtime 给出了五种实例化策略。差异的根源是各自的执行策略决定了运行时能力的边界：

- CoreCLR 有 JIT，运行时按需实例化，泛型对它来说不是难题
- Mono 在 JIT 模式下和 CoreCLR 一致，在 Full AOT 模式下面临同样的预见性压力
- IL2CPP 是纯 AOT，构建时必须覆盖所有泛型组合，FGS 是在 AOT 框架内的最大化缓解
- HybridCLR 在 IL2CPP 上叠加了多层防线，最终由解释器兜底
- LeanCLR 用运行时 inflate 绕过了所有预见性问题，代价是没有 native code 的性能

泛型不是一个独立的语言特性实现问题。它是 JIT 和 AOT 执行策略在运行时能力上最直接的试金石——JIT 天然能处理开放的组合空间，AOT 天然需要在有限的构建时信息中覆盖无限的运行时可能。理解了这个约束结构，就理解了为什么 HybridCLR 需要那么多层防线，也理解了为什么 FGS 不能完全替代 JIT。

## 系列位置

这是横切对比篇第 5 篇（CROSS-G5），也是 Phase 2 横切对比的收尾篇。

Phase 1 的三篇横切对比（CROSS-G1 metadata 解析、G2 类型系统、G3 方法执行）覆盖了 runtime 的基础层三大环节。Phase 2 的两篇（CROSS-G4 GC 实现、G5 泛型实现）覆盖了两个跨 runtime 差异最显著的横切维度——GC 和泛型也是实际工程中最容易遇到跨 runtime 行为差异的两个领域。

五篇横切对比从 metadata 到类型系统、方法执行、GC、泛型，五个维度完整覆盖了"同一份 ECMA-335 规范在五个 runtime 中的实现分化"这条主线。后续系列将进入各 runtime 的纵深分析。
