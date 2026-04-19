---
title: "CCLR-01｜值类型、引用类型、对象：先把 3 个最容易混的词讲清楚"
slug: "cclr-01-value-types-reference-types-objects"
date: "2026-04-19"
description: "先把值类型、引用类型和对象分开，再把复制语义、对象身份和 boxing 放回 runtime 里看，后面读 ECMA-335 和 CoreCLR 才不会从第一步就踩空。"
tags:
  - "C#"
  - "CLR"
  - "CCLR"
  - "ValueType"
  - "Runtime"
series: "从 C# 到 CLR"
series_id: "csharp-to-clr"
weight: 1801
---

> 值类型讲的是复制什么，引用类型讲的是指向什么，对象讲的是运行时真的活着的那一个实体。

这是 `从 C# 到 CLR` 系列的第 1 篇。它不负责把 runtime 讲透，只负责把最容易混的 3 个词拆开：`值类型`、`引用类型`、`对象`。

如果你刚进来，先看 [CCLR-00｜这条系列到底在讲什么]({{< relref "engine-toolchain/cclr-00-what-this-series-is-about.md" >}})，先把路线图放到脑子里，再读这一篇。

> **本文明确不展开的内容：**
> - `int / bool / enum / char / decimal` 的逐个特殊语义（在 [CCLR-02]({{< relref "engine-toolchain/cclr-02-primitive-types-runtime-contract.md" >}}) 展开）
> - `string` 和 `object` 的专门运行时行为（在 [CCLR-03]({{< relref "engine-toolchain/cclr-03-string-and-object.md" >}}) 展开）
> - `MethodTable`、对象头、GC 的实现细节（在 [ECMA-335 类型系统]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) 和 [内存模型]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) 里继续追深）

## 一、为什么这篇单独存在

多数 C# 工程师不是不会写代码，而是会把几个层次的词混在一起。

你会听到这样的说法：

- `class` 是对象
- `struct` 都在栈上
- 引用类型就是指针
- 对象就是变量里的那个值

这些话里有些能帮你过第一关，但它们都不够精确。

真正麻烦的地方在于：这些词一旦没拆开，后面的 runtime 阅读就会一路打结。你看到“复制”，会不知道复制的是数据还是引用；你看到“对象”，会不知道说的是类型、实例还是对象身份；你看到“boxing”，会把一次真实分配误看成普通转换。

这篇要做的事情很小，但很关键：先把语义边界收紧。

`值类型` 先回答“复制什么”；`引用类型` 先回答“变量里放的是什么”；`对象` 先回答“运行时到底有哪一个实体在内存里活动”。

只要这三件事分清楚，后面你再看 `ECMA-335`、`CoreCLR`、`Mono`、`IL2CPP`、`HybridCLR`、`LeanCLR`，脑子里就不再是名词堆，而是边界图。

## 二、最小可运行示例

下面这段代码故意把值语义、引用语义和对象身份放在一起看。

```csharp
using System;

public struct Point
{
    public int X;
    public int Y;
}

public sealed class Node
{
    public int Value;
}

public static class Demo
{
    public static Point Move(Point point)
    {
        point.X += 10;
        point.Y += 10;
        return point;
    }

    public static void Bump(Node node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        node.Value += 10;
    }
}

public static class Program
{
    public static void Main()
    {
        Point a = new Point { X = 1, Y = 2 };
        Point b = a;
        b = Demo.Move(b);

        Node x = new Node { Value = 1 };
        Node y = x;
        Demo.Bump(y);

        Console.WriteLine($"a = ({a.X}, {a.Y})");
        Console.WriteLine($"b = ({b.X}, {b.Y})");
        Console.WriteLine($"x = {x.Value}");
        Console.WriteLine($"y = {y.Value}");
        Console.WriteLine(ReferenceEquals(x, y));
    }
}
```

这段代码的结果会很直接：

- `a` 没变，因为 `Point` 是值类型，`b = a` 复制的是内容
- `x` 变了，因为 `Node` 是引用类型，`y = x` 复制的是引用
- `ReferenceEquals(x, y)` 为 `True`，说明 `x` 和 `y` 指向同一个对象

这就是本篇最核心的第一层结论：

`值类型` 的关键是值复制，`引用类型` 的关键是引用共享，`对象` 的关键是同一个运行时实体。

## 三、把三个词分清

先把三个词拆成最小定义。

### 值类型

值类型的变量保存的是数据本身。

你把一个值类型赋给另一个变量，发生的是内容复制，不是身份共享。两个变量之后可以各自变化，互不影响。

从阅读 runtime 的角度看，值类型最重要的不是“轻不轻”，而是“它的语义允许按值搬运”。

### 引用类型

引用类型的变量保存的是对象引用。

复制引用类型变量，复制的不是对象内容，而是“指向对象的那根线”。所以两个变量可以指向同一个运行时对象，改一个地方，另一个地方就能看到。

这也是为什么引用类型更适合承担协作、共享、生命周期控制这些职责。

### 对象

对象不是“某个语法关键字”，而是 runtime 里真的存在的实体。

你可以把它理解成：类型定义告诉你“这类东西长什么样”，对象告诉你“现在内存里真的有一份活体实例”。

这也是 `object` 这个词最容易被误解的地方。它既可以指 `System.Object` 这个根类型，也可以指“运行时对象”这个更宽的概念。本文里我们尽量用后者来讲语义。

### 这三者的关系

| 词 | 本篇里的意思 | 你最该盯住的点 |
|---|---|---|
| 值类型 | 复制数据本身的类型 | 赋值后内容是否独立 |
| 引用类型 | 复制引用的类型 | 变量是否指向同一个对象 |
| 对象 | 运行时真实存在的实体 | 身份是否被共享 |

只要这张表在脑子里，你后面再看 boxing、数组、参数传递、GC，就不会把所有东西都揉成一团。

## 四、直觉 vs 真相

### 直觉一：值类型就是“在栈上”

真相不是这样。

值类型的核心语义是按值复制，不是“栈”这个位置标签。局部变量里、对象字段里、数组元素里、临时寄存器里，都可能出现值类型。

所以“值类型都在栈上”这句话只是一种过度简化，适合入门，不适合做设计判断。

### 直觉二：引用类型就是“堆上的指针”

也不完整。

引用类型的变量里确实保存的是引用，但那个引用不是裸指针语义本身，而是 runtime 承认的对象引用。它要配合 GC、类型信息和对象头一起工作。

你不能把它简单理解成“我手里拿着一根地址”。

### 直觉三：对象就是 class

更不对。

`class` 是类型定义；对象是运行时实例。一个 `class` 还没被实例化之前，只是蓝图，不是对象。

反过来，值类型也可以构成对象语义的一部分。只要 runtime 把它当成一个可追踪、可复制、可装箱的实体，它就已经进入对象模型的讨论范围。

### 直觉四：boxing 只是转换

不是。

boxing 会产生一个真实对象。值类型的数据被复制到这个对象里，随后它才有了对象身份、对象头和 GC 可见性。

所以 boxing 不是“换个看法”，而是“换成了另一种运行时存在方式”。

## 五、在 Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR 里分别怎么落地

这 5 个 runtime 都接受同一条语义底线：值类型按值复制，引用类型按引用共享，对象是运行时实体。

它们真正不同的地方，是把这条语义底线装进了怎样的对象模型里。

### CoreCLR

CoreCLR 把引用类型对象放在堆上，对象头里挂类型信息和同步信息；值类型则按“可嵌入、可按值搬运”的原则参与布局。

这也是为什么 CoreCLR 的类型系统文章会一直围着 `MethodTable`、对象布局、字段偏移打转。它要回答的就是：同一条语义怎么在对象模型里变成可执行的数据结构。

### Mono

Mono 也承认同样的语义边界，但它的实现更偏向嵌入式和跨平台可维护性。

对你来说，Mono 最重要的不是“它和 CoreCLR 哪个更强”，而是它提醒你：同一条值/引用/对象语义，可以有不同的类型元数据和对象表组织方式。

### IL2CPP

IL2CPP 把 C# 先转成 C++，再交给本地编译器。

但不管它中间怎么换皮，值类型还是按值存在，引用类型还是按对象引用存在，对象还是要有自己的运行时壳子。差别在于：这层语义最后被映射成了 C++ 对象、指针和生成代码。

### HybridCLR

HybridCLR 不是改写这条语义，而是在 IL2CPP 的约束上补热更新能力。

所以它依旧必须遵守值类型、引用类型、对象身份这些基础约定。差别只是：一部分代码来自 AOT，一部分代码来自解释执行或补充 metadata，但它们面对的还是同一套对象模型。

### LeanCLR

LeanCLR 走的是更轻的路线。

它的对象模型和类型模型更精简，但它仍然不能把值类型和引用类型混成一团。因为一旦这条边界塌了，后面的分派、GC、泛型和装箱就都会跟着失真。

所以 LeanCLR 的简化，是缩小实现面，而不是改写语义面。

## 六、小结

这篇只想让你记住三件事。

- 值类型讲的是按值复制，引用类型讲的是按引用共享，对象讲的是 runtime 里的真实实体
- `class`、`struct`、`object` 这些词在 C# 表层很近，但在 runtime 里承担的职责完全不同
- 只要这条边界先立住，后面读 `boxing`、`string`、`object`、GC 和多 runtime 对照，脑子里就不会再散

## 系列位置

- 上一篇：[CCLR-00｜从 C# 到 CLR：这条线到底在讲什么]({{< relref "engine-toolchain/cclr-00-what-this-series-is-about.md" >}})
- 下一篇：[CCLR-02｜int、bool、enum、char、decimal：内建类型不是特殊语法，而是运行时约定]({{< relref "engine-toolchain/cclr-02-primitive-types-runtime-contract.md" >}})
- 向下追深：[ECMA-335 Type System]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
- 向旁对照：[Runtime Cross Type System]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})

> 本文是入口页。继续写正文前，请本地运行一次 `hugo`，确认 `ERROR` 为零。

