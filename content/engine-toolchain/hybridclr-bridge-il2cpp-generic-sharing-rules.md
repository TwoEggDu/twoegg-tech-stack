---
title: "HybridCLR 桥接篇｜IL2CPP 泛型共享规则：引用类型共享 object，值类型为什么不能"
date: "2026-04-13"
description: "在 AOT 泛型问题和 Full Generic Sharing 之间，补上一层最容易被跳过的背景：IL2CPP 原本就有一套 generic sharing 规则，引用类型共享 object，值类型必须独立实例化。这套规则直接决定了 DisStripCode 该怎么写、AOTGenericReferences 的清单为什么长成那样，以及 FGS 到底在解决什么。"
weight: 39
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Generics"
  - "AOT"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> IL2CPP 的泛型共享规则不是 HybridCLR 发明的，但如果不先把它拆清楚，后面关于 AOT 泛型缺口、DisStripCode 写法和 Full Generic Sharing 的所有判断都会歪。

这是 HybridCLR 系列的一篇桥接文章，位置在 HCLR-9（性能与预热策略）和 HCLR-10（Full Generic Sharing）之间。

HCLR-2 已经把 AOT 泛型问题拆成了两层失败：metadata 不足和 AOT 实例不存在。但到 HCLR-10 时，文章突然出现了一个前提——"IL2CPP 原本就做了部分 generic sharing"。这个前提从未被系统展开过。

这篇只做一件事：

`把 IL2CPP 的泛型代码生成和共享规则单独讲清楚，让后面的文章不必每次都从头补这段背景。`

## 这篇要回答什么

这篇主要回答 5 个问题：

1. IL2CPP 面对泛型时，到底怎么决定"生成几份 native 代码"。
2. 为什么所有引用类型泛型实例能共享一份实现。
3. 为什么值类型泛型实例必须各自独立。
4. 这套规则怎么影响 DisStripCode 的写法和 AOTGenericReferences 的清单。
5. Full Generic Sharing 到底在这套规则上推进了什么。

## 为什么要单独讲泛型共享

HCLR-2 花了大量篇幅把 AOT 泛型的两层失败立住：

- 失败一：metadata 不足，runtime 看不懂
- 失败二：AOT 实例不存在，runtime 没东西可调

但这两层失败背后，还藏着一个更基础的问题：

`IL2CPP 面对一个泛型定义和它的 N 种实例化，到底生成了几份 native 代码？`

这个问题如果不先回答，后面的判断全都悬着：

- 为什么有些 AOT 泛型缺口"自动"就不存在？
- 为什么 DisStripCode 里引用类型可以统一写 `object`？
- 为什么值类型泛型总是 AOT 泛型问题的重灾区？
- 为什么 HCLR-10 里说 Full Generic Sharing 要把共享规则"推广到值类型"？

所有这些，根源都在同一套规则里。

## IL2CPP 的泛型代码生成策略

先把 IL2CPP 的基本行为压清楚。

在 Mono / CoreCLR 这类 JIT runtime 里，泛型实例化可以在运行时完成——runtime 看到一个新的泛型参数组合时，现场生成对应的机器码。

但 IL2CPP 是 AOT runtime。它必须在构建时就把所有需要的 native 代码准备好。

于是一个非常实际的问题就来了：

`如果项目里用了 List<int>、List<string>、List<MyClass>、List<GameObject>、List<Vector3>……IL2CPP 是不是要为每种参数都生成一份完整的 native 实现？`

如果真的每种都生成，代码膨胀会非常快。一个泛型容器配上几十种参数类型，native 代码量就成倍增长。

所以 IL2CPP 有一套共享规则：能共享的泛型实例，复用同一份 native 代码；不能共享的，才单独生成。

这套规则的分界线非常清晰：**引用类型和值类型**。

## 引用类型共享 object

所有引用类型泛型实例，在 native 层共享同一份实现。

具体来说：

- `List<string>` 和 `List<MyClass>` 共享同一份 native 代码
- `Dictionary<string, GameObject>` 和 `Dictionary<string, MyComponent>` 共享同一份
- `Action<SomeClass>` 和 `Action<AnotherClass>` 共享同一份

在 IL2CPP 的共享规则下，它们都退化到引用类型参数用 `object` 替代的那个版本。也就是说，`List<string>` 和 `List<MyClass>` 在 native 层走的都是 `List<object>` 的代码路径。

为什么能这样做？因为所有引用类型在 native 层有三个关键的共同点：

### 1. 内存大小相同

不管是 `string`、`GameObject`、`MyClass` 还是任何自定义引用类型，它们作为变量存储时占用的空间都是一个指针的大小——32 位平台上是 4 字节，64 位平台上是 8 字节。

变量本身不存储对象数据，只存储一个指向堆上对象的指针。

### 2. 内存布局相同

正因为都是指针，引用类型泛型参数在数组、字段、方法参数里的布局都是一样的。

`List<string>` 的内部数组 `T[]` 和 `List<MyClass>` 的内部数组 `T[]`，在 native 层都是一个指针数组。每个元素占一个指针大小，排列方式完全一致。

### 3. ABI 传参方式相同

在函数调用时，不管是传 `string` 还是传 `MyClass`，走的都是"传一个指针"。调用约定（calling convention）对它们的处理完全一样：同样大小的参数，放在同样的寄存器或栈位置。

这三个条件加在一起，意味着 IL2CPP 完全可以只生成一份 `List<object>` 的 native 代码，然后让所有引用类型实例复用这份代码。在执行层面，区别只在 metadata 层——runtime 知道当前操作的"逻辑类型"是什么，但 native 代码本身不需要因此而不同。

## 值类型为什么不能共享

值类型的情况完全不同。

先看几个最基础的例子：

- `int` 是 4 字节
- `float` 是 4 字节
- `double` 是 8 字节
- `long` 是 8 字节
- `Vector3` 是 12 字节（三个 float）
- `Vector4` 是 16 字节（四个 float）
- `ValueTuple<int, int>` 是 8 字节
- `ValueTuple<int, double>` 是 16 字节（对齐后）
- `Matrix4x4` 是 64 字节

这些类型的大小完全不同。而大小不同，直接意味着三件事全部不成立：

### 1. 内存大小不同

`List<int>` 的内部数组，每个元素占 4 字节。`List<double>` 的内部数组，每个元素占 8 字节。`List<Vector3>` 的内部数组，每个元素占 12 字节。

同一份 native 代码不可能用固定的偏移量去访问不同大小的元素。

### 2. 内存布局不同

值类型直接内联存储数据，不经过指针间接寻址。这意味着数组的元素紧密排列，但排列的间距取决于元素大小。

`int[]` 里第 3 个元素的偏移是 `3 * 4 = 12`，`Vector3[]` 里第 3 个元素的偏移是 `3 * 12 = 36`。如果用同一份代码去访问，偏移量计算直接就是错的。

### 3. ABI 传参方式不同

在函数调用层面，不同大小的值类型走的调用约定路径也不同。4 字节的 `int` 可能直接放在寄存器里传递，12 字节的 `Vector3` 可能需要拆成多个寄存器或走栈传递，64 字节的 `Matrix4x4` 几乎一定要走栈。

这意味着 `void Process<T>(T value)` 这个方法，当 `T = int` 和 `T = Vector3` 时，生成的 native 代码在参数接收方式上就已经不同了。

所以 IL2CPP 必须为每种值类型泛型实例生成独立的 native 代码。`List<int>`、`List<float>`、`List<Vector3>` 在 native 层是三份完全不同的实现。

即使 `int` 和 `float` 都是 4 字节，IL2CPP 仍然不会共享它们。因为除了大小之外，值类型的拷贝语义、可能的对齐要求、以及部分平台上浮点和整数走不同寄存器等因素，都会导致共享不安全。

## 这件事对 AOT 泛型问题的影响

把上面的规则和 HCLR-2 里的两层失败放在一起看，一个非常关键的推论就出来了：

### 引用类型：天然更容易"够用"

只要 IL2CPP 在构建时见过**任何一个**引用类型实例，比如 `List<object>`，那么所有其他引用类型实例——`List<string>`、`List<MyClass>`、`List<GameObject>`——就自动有 native 实现可用。

因为它们共享同一份代码。

这也意味着，对于引用类型泛型参数：

- AOT 泛型缺口出现的概率天然更低
- 即使热更代码里出现了全新的引用类型（比如热更定义的 `HotfixPlayerData`），只要 AOT 世界里已经有 `List<object>` 的实现，`List<HotfixPlayerData>` 在 native 层就能直接复用

### 值类型：必须每种都提前见过

值类型就没有这个便利。

`List<int>` 的 native 实现只能给 `List<int>` 用。`List<float>` 需要自己的，`List<Vector3>` 也需要自己的，`List<ValueTuple<int, string>>` 又需要自己的。

如果构建时没有见过某个具体的值类型泛型实例，运行时就没有对应的 native 代码可调。

这就是为什么值类型泛型是 AOT 泛型问题的重灾区——每多一种值类型参数组合，就多一个潜在的缺口。

而热更场景让这个问题更尖锐：如果热更代码里新定义了一个 `struct MyData { int id; float score; }`，然后用了 `Dictionary<int, MyData>`，那 AOT 主包在构建时根本不可能预见这个组合。对应的 native 实现自然不存在。

## 对 DisStripCode 的直接影响

理解了共享规则之后，DisStripCode 的写法逻辑就变得非常清晰。

### 引用类型位置：写 `object` 就够了

在 DisStripCode 里写：

```csharp
_ = new List<object>();
```

这一行能覆盖所有 `List<引用类型>` 的使用——`List<string>`、`List<MyClass>`、`List<HotfixType>` 全部包含在内。

因为 IL2CPP 看到 `List<object>` 时会为它生成 native 实现，而所有引用类型实例共享这份实现。

同理：

```csharp
_ = new Dictionary<string, object>();
```

能覆盖 `Dictionary<string, MyClass>`、`Dictionary<string, AnotherClass>` 等所有第二个参数为引用类型的情况。

### 值类型位置：必须精确

但如果值类型位置写错了，就覆盖不到。

```csharp
_ = new Dictionary<int, object>();
```

这一行只能保证 `Dictionary<int, 引用类型>` 有 native 实现。它**不能**覆盖 `Dictionary<float, object>()`，因为 `int` 和 `float` 是两种不同的值类型，不共享。

同样：

```csharp
_ = new Dictionary<int, object>();
```

也不能覆盖 `Dictionary<long, object>()`。`int` 是 4 字节，`long` 是 8 字节，更不可能共享。

所以 DisStripCode 的核心判断规则可以压成一句：

`引用类型参数位置用 object 兜底，值类型参数位置必须和实际使用的值类型严格一致。`

这也是为什么 HCLR-20（共享类型判断）和 HCLR-21（DisStripCode 写法手册）要花两篇来讲"该写什么类型"——不是因为写法复杂，而是因为共享规则在引用类型和值类型之间完全不同。

## 对 AOTGenericReferences 的影响

理解了共享规则之后，再回头看 `AOTGenericReferences.cs` 生成的清单，很多"看起来奇怪"的现象就不再奇怪了。

### 引用类型参数被替换成 object

如果清单里出现的是：

```
// System.Collections.Generic.List<System.Object>
```

而不是：

```
// System.Collections.Generic.List<MyProject.SomeClass>
```

这不是 bug，也不是生成器在偷懒。这是 IL2CPP 的共享规则在起作用——所有引用类型实例归并到了 `object` 版本，清单里自然只会出现归并后的形态。

### 值类型参数保持原样

如果清单里出现的是：

```
// System.Collections.Generic.Dictionary<System.Int32, System.Object>
// System.Collections.Generic.Dictionary<System.Single, System.Object>
```

`int` 和 `float` 被分开列出来，也不是生成器在冗余输出。这正是因为值类型不共享——`Dictionary<int, object>` 和 `Dictionary<float, object>` 需要各自独立的 native 实现，所以清单必须把它们分开列。

理解了这一点，读 `AOTGenericReferences.cs` 就不再是"看一堆莫名其妙的类型列表"，而是"直接看到 IL2CPP 共享规则的投影"。

## 引出 Full Generic Sharing

到这里，IL2CPP 原有共享规则的边界就非常清楚了：

- 引用类型：全部共享，一份 native 代码兜底
- 值类型：每种独立，必须提前见过

这套规则在大多数 AOT 场景里是够用的。但它的上限也很明显：

`值类型泛型实例必须穷举。穷举不到的，native 侧就没有实现。`

对于纯 AOT 项目，这个上限通常不太刺眼——因为构建时能看到的代码就是全部代码，IL2CPP 可以把所有出现过的值类型泛型实例都提前编译好。

但在热更新场景下，热更代码里可能出现任意新的值类型和泛型参数组合。穷举这件事从"有点麻烦"变成了"工程上不可能"。

HCLR-10 要讲的 Full Generic Sharing，核心创新就在这里：

`它把共享规则从"只有引用类型能共享"推广到"所有类型参数都能共享"。`

具体做法是引入一个规范的占位类型 `Il2CppFullySharedGenericAny`，用它代替所有泛型参数——不管是引用类型还是值类型。IL2CPP 在 AOT 编译时为每个泛型方法生成一份完全共享的版本，所有实例都走这份共享代码。

代价是什么？值类型参数不再能享受"精确大小、精确布局"的直接操作。共享代码需要通过 `ConstrainedCall` 做间接分发，值类型操作可能产生本不必要的装箱，泛型函数整体会有性能折价。

这不是免费午餐，而是一次在覆盖率和性能之间的重新取舍。但这个取舍的前提，正是这篇讲的这套共享规则——不先理解"旧规则为什么在值类型上有上限"，就无法理解"新规则到底在推进什么"。

## 收束

IL2CPP 的泛型共享规则是理解 AOT 泛型问题的底座：引用类型共享 `object`，值类型必须独立实例化。这套规则决定了 DisStripCode 的写法边界，解释了 AOTGenericReferences 清单的形态，也划出了 Full Generic Sharing 想要突破的上限。

把这一层先立住，后面关于泛型缺口、修法判断和 FGS 的讨论才有地基。

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}}">HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}}">HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版</a>
