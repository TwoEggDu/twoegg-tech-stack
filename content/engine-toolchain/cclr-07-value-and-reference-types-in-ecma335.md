---
date: "2026-04-19"
title: "CCLR-07｜ECMA-335 里的值类型和引用类型：先把类型分类对上号"
description: "先把 CLI 里的 value type、reference type、boxed object、managed pointer 这几类词放稳，再去看 runtime、装箱和对象布局时，就不会把概念混成一团。"
slug: "cclr-07-value-and-reference-types-in-ecma335"
weight: 1807
tags:
  - "C#"
  - "CLR"
  - "CCLR"
  - "ECMA-335"
  - "TypeSystem"
series: "从 C# 到 CLR"
series_id: "csharp-to-clr"
---

> `value type` 和 `reference type` 在 ECMA-335 里不是“栈和堆”的别名，而是复制、身份、装箱和托管引用规则的入口标签。

这是 `从 C# 到 CLR` 系列里一个很重要的入口。前面的文章已经把 C#、CLI、CTS 这些层次连起来了；这一篇要做的事更小，也更具体：**把类型分类这件事讲清楚**。

如果你把 `value type` 和 `reference type` 只理解成“一个放栈、一个放堆”，后面看装箱、方法调用、泛型、字段布局时都会很容易串层。ECMA-335 里的分类不是为了考试，它是 runtime 选择存储、传递和调用规则时最先要看的入口。

## 这篇只解决什么

- CLI 里到底有哪些类型分类
- `value type` 和 `reference type` 的边界在哪里
- 为什么装箱不是“转个格式”这么简单
- 为什么 `managed pointer` 既重要又不能随便拿来想象成普通指针

**本文不展开的内容：**

- `MethodTable`、对象头、字段偏移和 GC 细节
- `box` / `unbox.any` 的完整 IL 语义
- `ref struct`、`Span<T>`、`stackonly` 这些更靠后的边界

这些内容会在后面的对象布局、装箱、引用传递文章里展开。这里先把座标系立起来。

## 一、先看问题

很多人第一次接触 CLI 类型系统时，最容易犯两个错误。

第一，把“值类型”和“引用类型”理解成纯内存位置问题。实际上，这两个词描述的是**语义和传递方式**，不是单纯的栈和堆。

第二，把 C# 语法层面的 `struct`、`class`、`record struct`、`record class` 直接等同于 CLI 的全部类型分类。C# 只是前端，CLI 才是运行时真正按规则处理的地方。

先看一个很小的例子：

```csharp
public readonly struct Meter
{
    public Meter(double value) => Value = value;
    public double Value { get; }
}

public sealed class OrderId
{
    public OrderId(string value) => Value = value;
    public string Value { get; }
}
```

这两个类型都很简单，但运行时对它们的处理并不一样。`Meter` 是值类型，`OrderId` 是引用类型。前者在赋值和参数传递时倾向于复制值，后者传递的是对象引用。这个差别一旦进入泛型、集合、装箱、字段布局，行为就会放大。

如果你把它们都当“对象”，就会忽略复制成本；如果你把它们都当“地址”，就会忽略值语义。

## 二、CLI 的解法

ECMA-335 里的最小分法，可以先记成三组：

1. `reference type`
2. `value type`
3. `managed pointer`

`reference type` 这边，变量里放的是对象引用。对象本体通常由 runtime 管，生命周期也由 runtime 追踪。

`value type` 这边，变量里保存的是值本身。它可能作为字段内嵌在别的对象里，也可能作为局部变量或数组元素存在。值类型不是“次等类型”，它只是遵守另一套传递和复制规则。

`managed pointer` 则是更靠近 runtime 的概念。它不是普通的原生指针，而是带托管语义的引用形式。后面看 `ref` 参数、返回值和受限类型时，它会非常重要。

一个最小的认知图是这样的：

- `class` 往往对应 `reference type`
- `struct` 往往对应 `value type`
- `ref` / `out` / `in` 牵涉托管引用和别名语义

这不是说 C# 和 CLI 一一等价，而是说 C# 前端会把你写下来的概念，映射到 CLI 允许的那几类语义上。

## 三、你该先记住的边界

### 值类型

值类型最重要的关键词是“按值、可内联、可复制”。它的优势是布局直接，局部性好，和一些批量数据结构很合拍。它的代价是复制行为更明显，字段一多时也可能变得昂贵。

### 引用类型

引用类型最重要的关键词是“按引用、共享身份、独立生命周期”。它的优势是身份稳定，别名清晰，适合表示对象和服务。它的代价是多了一层间接访问，也把对象管理交给 runtime。

### 管理指针

管理指针最重要的关键词是“别名、受限、受 runtime 约束”。它能让一些场景避免复制，也能让某些 API 把位置和值的关系讲清楚。但它不是你日常随手拿来当普通指针的东西。

## 四、最小可运行示例

下面这个例子只想说明一件事：值语义和引用语义，改变的是传递和修改的方式，不只是“放哪儿”。

```csharp
using System;

public struct Counter
{
    public int Value;
}

public sealed class Box
{
    public int Value;
}

public static class Program
{
    public static void Main()
    {
        var c1 = new Counter { Value = 1 };
        var c2 = c1;
        c2.Value = 2;

        var b1 = new Box { Value = 1 };
        var b2 = b1;
        b2.Value = 2;

        Console.WriteLine($"Counter: {c1.Value}, {c2.Value}");
        Console.WriteLine($"Box: {b1.Value}, {b2.Value}");
    }
}
```

`Counter` 的复制让两个变量彼此独立；`Box` 的复制只是复制了引用，两个变量指向同一个对象。这个差别很基础，但后面所有“为什么会装箱”“为什么会拆箱”“为什么泛型里表现不同”的问题，最后都会回到这里。

## 五、直觉 vs 真相

| 直觉 | 真相 |
|---|---|
| 值类型就是栈上数据 | 值类型首先是按值语义，它可以在栈帧、对象字段、数组元素、寄存器和装箱对象里出现 |
| 引用类型就是指针 | 引用类型变量保存托管对象引用，它背后还有类型信息、GC 约束和对象身份 |
| 装箱只是把值类型“看成 object” | 装箱会产生一个真实对象，把值复制进这个对象，让它进入引用类型世界 |
| `ref` 就是 C 指针 | `ref` / `out` / `in` 牵涉 managed pointer，它受 runtime 和 verifier 约束，不是裸地址 |

这组对比的中心结论是：**ECMA-335 的类型分类先定义语义边界，再由 runtime 决定具体存储和调用路径**。

如果你只记“值类型在栈、引用类型在堆”，后面看对象布局、泛型共享、装箱和 IL2CPP 生成代码时一定会误判。

## 六、它和 runtime 的关系

到了 runtime 层，这些分类不再只是语义词，而是直接影响存储和调用。

- 值类型常常可以内嵌在对象、数组或栈帧里
- 引用类型会有独立对象身份
- `box` 会把值类型包装成对象
- `unbox` / `unbox.any` 会按 CLI 规则从装箱对象里取回值

也就是说，CLI 对类型的分类，最后会变成 runtime 决定如何分配、如何复制、如何调用的依据。你越早把这个层次分清，后面看对象布局和装箱就越不容易串。

## 七、在 Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR 里分别怎么落地

这一篇停在 ECMA-335 层，不重写任何 runtime 的对象模型。你只需要先把下面这张对照表当成路牌。

| Runtime | 这一层怎么接下去 |
|---|---|
| CoreCLR | 把值类型、引用类型、装箱对象落到对象布局、`MethodTable`、字段布局和 JIT 调用规则上 |
| Mono | 遵守同一组 CLI 分类，但在嵌入式、JIT / AOT / 解释路径之间做不同工程取舍 |
| IL2CPP | 保留 CLI 语义，把类型分类翻译成 C++ 生成代码和原生对象模型需要的结构 |
| HybridCLR | 在 IL2CPP 的 AOT 约束上补 metadata 与解释执行，因此更依赖类型分类的可见性 |
| LeanCLR | 从零定义更轻的对象和类型结构，但仍不能改写值类型、引用类型、装箱对象这些语义边界 |

如果你要继续追深，类型分类的规范定义看 ECMA-335 类型系统；对象如何真正长在 runtime 里，看 CoreCLR 对象布局和 runtime-cross 类型系统对照。

## 小结

- 值类型、引用类型、管理指针不是语法标签，而是 CLI 运行时语义。
- 不要把 value/reference 简化成栈和堆，它们首先是传递和别名规则。
- 先把分类边界放稳，再去看装箱、对象布局和泛型，理解会顺很多。

## 系列位置

- 上一篇：[CCLR-06｜从 C# 到 CLI：语言前端、CTS、CLS 到底怎么对应]({{< relref "engine-toolchain/cclr-06-from-csharp-to-cli.md" >}})
- 下一篇：[CCLR-08｜成员的元数据长什么样：方法、字段、属性和事件怎么被描述]({{< relref "engine-toolchain/cclr-08-metadata-shape-of-members.md" >}})
- 向下追深：[ECMA-335 Type System]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}})
- 向旁对照：[Runtime Cross Type System]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})
