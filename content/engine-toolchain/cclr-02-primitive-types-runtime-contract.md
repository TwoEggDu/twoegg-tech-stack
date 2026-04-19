---
title: "CCLR-02｜int、bool、enum、char、decimal：内建类型不是特殊语法，而是运行时约定"
slug: "cclr-02-primitive-types-runtime-contract"
date: "2026-04-19"
description: "把 int、bool、enum、char、decimal 放到 runtime 里看，就会发现它们不是几种语法糖，而是编译器、metadata 和执行模型共同遵守的约定。"
tags:
  - "C#"
  - "CLR"
  - "CCLR"
  - "Primitive"
  - "Metadata"
  - "Runtime"
series: "从 C# 到 CLR"
series_id: "csharp-to-clr"
weight: 1802
---

> 内建类型不是“更短的写法”，而是编译器和 runtime 共同承认的一组基础契约。

这是 `从 C# 到 CLR` 系列的第 2 篇。上一篇我们把“值类型、引用类型、对象”拆开了；这一篇继续往下走，把最常见的几个内建类型放回 runtime 里看。

如果你还没看过上一节，先回到 [CCLR-01]({{< relref "engine-toolchain/cclr-01-value-types-reference-types-objects.md" >}})。这篇建立在那条语义边界之上。

> **本文明确不展开的内容：**
> - `string` 和 `object` 的专门运行时行为（在 [CCLR-03]({{< relref "engine-toolchain/cclr-03-string-and-object.md" >}}) 展开）
> - 对象布局和 GC 的完整细节（在 [ECMA-335 内存模型]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) 继续追深）
> - 这几个类型在数值运算、溢出、序列化、反射、互操作里的所有边角（后续会在更细的机制文里展开）

## 一、为什么这篇单独存在

`int`、`bool`、`enum`、`char`、`decimal` 这些词看起来太熟，越熟越容易写错。

很多人对它们的直觉是“基础类型嘛，知道怎么用就行”。runtime 不是这样看。runtime 关心的是：

- 这个类型是不是可按值搬运
- 这个类型有没有固定的底层表示
- 这个类型会不会触发特殊的装箱、比较、默认值或代码生成规则
- 这个类型在不同 runtime 里是不是都被当成同一个契约来处理

所以这篇不是讲“怎么写数字”，而是讲“这些类型为什么能稳定地被编译器、metadata 和 runtime 一起理解”。

其中最容易写歪的点，是把 `decimal` 和其他几个词混在一起。

`int / bool / enum / char` 可以直接看成 CLI 里的基础值语义；`decimal` 虽然在 C# 里也是关键字，但它本质上是 `System.Decimal`，需要库和 runtime 一起配合，不是硬件层面的原生数值指令。

这条边界不分清，后面看性能、序列化和跨 runtime 行为时，很容易判断失误。

## 二、最小可运行示例

下面这段代码把几个内建类型放在同一个小场景里。

```csharp
using System;

enum BuildStatus : byte
{
    Unknown = 0,
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}

public static class RuntimeContracts
{
    public static decimal ApplyDiscount(decimal amount, decimal rate)
    {
        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (rate < 0m || rate > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        return decimal.Round(amount * (1m - rate), 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsHexDigit(char ch)
    {
        return ch is >= '0' and <= '9'
            or >= 'A' and <= 'F'
            or >= 'a' and <= 'f';
    }
}

public static class Program
{
    public static void Main()
    {
        BuildStatus status = (BuildStatus)2;

        Console.WriteLine(status);
        Console.WriteLine(RuntimeContracts.ApplyDiscount(199.99m, 0.15m));
        Console.WriteLine(RuntimeContracts.IsHexDigit('B'));
    }
}
```

这段代码里最值得注意的不是“能跑”，而是“每个类型的角色都很稳定”：

- `BuildStatus` 不是字符串枚举表，而是带底层整数表示的值类型
- `char` 不是“一个抽象字符概念”，而是一个可被 runtime 直接处理的 UTF-16 代码单元
- `decimal` 不是硬件指令集里的原生数值类型，而是需要库和运行时语义共同参与的固定小数精度类型

## 三、把五个内建类型拆开

### `int`

`int` 是最典型的整数值类型。

它的意义不只是“整数”，而是：编译器、JIT / AOT、反射和通用库都默认它是一种基础算术单位。你把它放进数组、泛型、字段和参数列表里，runtime 都知道该怎么处理。

### `bool`

`bool` 不是“任何非零都算真”。

在 C# 语义里，它只有两个合法状态：`true` 和 `false`。这件事看起来简单，但对分支消除、条件判断和序列化都很重要。

到了互操作或底层编码边界，`bool` 的表示会变复杂；入口文先守住 C# 语义，跨边界细节交给 runtime-cross 和互操作深水文。

### `enum`

`enum` 是“带名字的整数值类型”。

它不是一串字符串，也不是一组魔法常量。它的底层仍然是整数，只是把名字和语义附着在整数上，方便你读代码、做状态机和做协议映射。

### `char`

`char` 最容易被误解。

它不是“一个 Unicode 字符”的全部概念，而是一个 UTF-16 代码单元。对很多基本文本处理足够了；一旦进入表情符号、组合字符和更复杂的文本处理，你就不能再把它当成“一个字符”的全部解释。

### `decimal`

`decimal` 是另一个层次的东西。

它偏向固定精度和十进制计算，适合财务、计费、比例这些场景。它看起来像 primitive，但它不是 CPU 原生数值指令的直接产物，而是由 `System.Decimal` 这套运行时约定承接的。

### 一张最小对照表

| 类型 | 语义重点 | 你最该记住的事 |
|---|---|---|
| `int` | 基础整数值类型 | 最接近 runtime 原生算术语义 |
| `bool` | 二值逻辑 | C# 语义只允许真 / 假，互操作边界另看编码规则 |
| `enum` | 命名的整数值类型 | 名字在上，底层整数在下 |
| `char` | UTF-16 代码单元 | 不是完整 Unicode 文本单位 |
| `decimal` | 十进制固定精度数值 | 需要库和 runtime 协作 |

## 四、直觉 vs 真相

### 直觉一：这些类型只是“语法糖”

真相不是。

它们之所以被称为内建类型，不是因为 C# 语法写起来短，而是因为语言、metadata 和 runtime 对它们有一整套共同约定。

`int` 进入数组、泛型、字段和参数列表时，runtime 都知道它是值类型；`enum` 进入反射、装箱和比较时，runtime 也知道它的底层还是整数。

### 直觉二：`enum` 就是名字列表

不对。

`enum` 的本体仍然是一个值类型。名字只是给底层整数加了一层可读性壳子。你如果忘了这一点，就会在序列化、协议映射和默认值处理里踩坑。

### 直觉三：`char` 就等于一个字符

也不对。

对很多英语场景，它确实够用；但从 runtime 语义上，它更接近一个编码单元。你把它和“人类感知的字符”混成一回事，就会在文本拆分和长度统计上出错。

### 直觉四：`decimal` 和 `int` 差不多

差很多。

`int` 更接近 runtime 原生算术；`decimal` 更接近一个专门为十进制语义设计的库类型。两者的精度、运算方式和性能代价都不是一回事。

## 五、在 Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR 里分别怎么落地

这几种 runtime 对内建类型的共同要求是一致的：都必须把它们识别成稳定的值类型契约。

区别在于，它们把这份契约交给了谁去实现。

### CoreCLR

CoreCLR 会把这些类型直接纳入类型系统和 JIT 优化路径。

`int`、`bool`、`enum`、`char` 这类类型，编译器和 JIT 都能很自然地识别；`decimal` 则更依赖库层实现和 helper 调用。这也是为什么你在 CoreCLR 里看数值类型时，总会碰到“原生值语义”和“库辅助语义”两条线。

### Mono

Mono 也遵守同样的契约，但它更强调跨平台和嵌入式可维护性。

这些内建类型在 Mono 里依旧是值类型，只是优化、AOT 行为和 runtime 辅助路径可能跟 CoreCLR 不一样。

### IL2CPP

IL2CPP 把这些类型转成 C++ 层面的实现，但语义并没有变。

`int / bool / enum / char` 仍然是按值搬运的基础类型；`decimal` 仍然是需要库语义支撑的数值类型。你只是把“谁来执行”从 CLR 换成了 C++ 编译链。

### HybridCLR

HybridCLR 的关键不是改写内建类型，而是在 AOT 约束上补热更新能力。

因此它面对这些类型时，依旧要保持同一套值语义。AOT 部分、解释器部分、补充 metadata 部分都要对齐这条底线，否则热更新和宿主 runtime 会出现语义裂缝。

### LeanCLR

LeanCLR 的目标更轻。

它可以把实现做得更小，但不能把语义做乱。`int / bool / enum / char / decimal` 仍然必须在 runtime 层表现为稳定的值类型约定；差别只是它把这套约定装进了更精简的对象模型和执行模型里。

## 六、小结

你可以先把这篇记成一句话：

- `int / bool / enum / char / decimal` 不是几种“写法不同的字面量”，而是一组 runtime 认可的基础类型契约
- `enum` 是带名字的整数值类型，`char` 是编码单元，不是“人类语言中的字符”本身，`decimal` 是库和 runtime 协作的十进制类型
- 只要把这些边界立住，后面读 `boxing`、`string`、泛型和多 runtime 对照时，心里就不会乱

## 系列位置

- 上一篇：[CCLR-01｜值类型、引用类型、对象：先把 3 个最容易混的词讲清楚]({{< relref "engine-toolchain/cclr-01-value-types-reference-types-objects.md" >}})
- 下一篇：[CCLR-03｜string 和 object：一个最特殊，一个最基础]({{< relref "engine-toolchain/cclr-03-string-and-object.md" >}})
- 向下追深：[ECMA-335 Type System]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
- 向旁对照：[Runtime Cross Type System]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})

> 本文是入口页。继续写正文前，请本地运行一次 `hugo`，确认 `ERROR` 为零。
