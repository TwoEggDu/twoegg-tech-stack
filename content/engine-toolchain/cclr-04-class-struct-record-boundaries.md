---
title: "CCLR-04｜class、struct、record：三种边界，不是三种写法"
slug: "cclr-04-class-struct-record-boundaries"
date: "2026-04-19"
description: "class、struct、record 不是三种语法风格，而是三种不同的边界选择：你到底更强调身份、复制语义，还是快照语义。"
tags:
  - "C#"
  - "CLR"
  - "CCLR"
  - "Class"
  - "Struct"
  - "Record"
series: "从 C# 到 CLR"
series_id: "csharp-to-clr"
weight: 1804
---

> `class`、`struct`、`record` 的差别，不在写法，而在你到底把这份数据当“一个活着的对象”、"一块会被复制的值"，还是“一份应该整体比较的快照”。

这是 `从 C# 到 CLR` 系列的第 4 篇。前面我们已经把“值类型 / 引用类型 / 对象”和“内建类型契约”拆开了；这一篇继续收紧，把 `class`、`struct`、`record` 三种最容易混的声明方式放回到运行时语义里看。

> **本文明确不展开的内容：**
> - 对象头、字段偏移、GC 扫描和完整对象布局（在 [CCLR-10]({{< relref "engine-toolchain/cclr-10-object-layout-in-coreclr.md" >}}) 和 [ECMA-335 内存模型]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) 继续追）
> - `record` 的完整编译产物拆解（这里只讲建模边界，不吞编译器 lowering 细节）
> - `ref struct`、`Span<T>`、栈限定类型的深水规则（不在这条入口线里展开）

## 一、为什么这篇单独存在

很多人第一次接触这三个词时，会把它们理解成三种“声明风格”。

这会让你在设计阶段做出很危险的错误：把本来该按值语义建模的东西写成 `class`，把本来有明确身份的对象压成 `record`，或者把本来会频繁复制的大块数据写成 `struct` 之后才发现成本异常。

`class`、`struct`、`record` 真正回答的不是“怎么写”，而是：

- 你关心身份，还是关心值
- 你关心共享修改，还是关心复制隔离
- 你关心对象关系，还是关心状态快照

所以它们不是语法分支，而是边界分支。

## 二、最小可运行示例

```csharp
using System;

public sealed class Order
{
    public Guid Id { get; }
    public string Customer { get; private set; }

    public Order(Guid id, string customer)
    {
        Id = id;
        Customer = customer;
    }

    public void Rename(string customer) => Customer = customer;
}

public readonly struct Money
{
    public decimal Amount { get; }

    public Money(decimal amount) => Amount = amount;
}

public sealed record ExportSnapshot(string OrderId, string Customer, Money Amount);

public static class Demo
{
    public static void Main()
    {
        var order = new Order(Guid.NewGuid(), "Ada");
        var amount = new Money(128.5m);
        var snapshot = new ExportSnapshot(order.Id.ToString(), order.Customer, amount);
        var next = snapshot with { Amount = new Money(199m) };

        order.Rename("Grace");

        Console.WriteLine(order.Customer);
        Console.WriteLine(snapshot.Customer);
        Console.WriteLine(snapshot == next);
    }
}
```

这段代码里，三个类型其实在回答三个不同问题。

`Order` 更像“一个有持续身份的对象”。你在意它是不是同一单订单，允许它在生命周期里被修改。

`Money` 更像“一个可复制的值”。你关心的是数值，不关心它是不是同一个实例。

`ExportSnapshot` 更像“一个可以整体比较和复制的状态快照”。它代表某个时刻的数据切片，而不是一个会继续演化的实体。

## 三、把核心概念分清

### 1. `class` 先表达身份

TL;DR：如果你关心“这是不是同一个实体”，通常先从 `class` 开始想。

`class` 最自然的语义是对象身份。它适合描述那些在系统里会持续存在、会被多个地方共同引用、会经历状态变化的东西。

你可以修改同一个对象，可以把它传给不同协作者，可以围绕它建立生命周期和关系网络。

所以 `class` 的关键词不是“堆上对象”，而是“身份、共享、持续性”。

### 2. `struct` 先表达值

TL;DR：如果你关心“这份数据是什么”，而不是“它是不是那一个实例”，`struct` 更自然。

`struct` 的核心语义不是“小”，而是“按值处理”。

当你复制一个值类型变量时，默认是在复制那份值本身，而不是共享一个带身份的对象。

所以 `struct` 适合那些边界清晰、值语义强、整体复制比共享身份更自然的数据块。大小当然会影响成本，但那是第二层问题，不是第一层语义。

### 3. `record` 先表达快照

TL;DR：`record` 的核心不是“更高级的 class”，而是“我希望这份数据天然适合被当成整体状态来比较和复制”。

`record` 仍然可能是引用类型，也可能是值类型（`record struct`）。它真正提供的是一种建模意图：这份数据更像快照，而不是围绕身份演化的实体。

这也是为什么 `record` 很适合配置、消息、DTO、事件、不可变状态。

你不是在说“这东西一定没有对象身份”，你是在说“当我使用它时，优先按整体状态来理解它”。

### 4. 这三者不是排位赛

TL;DR：`class` 不是“更完整”，`struct` 不是“更快”，`record` 也不是“更新潮”。

它们只是优先回答不同问题。

如果你拿“快不快”来决定三者，就会忽略真正重要的建模语义；如果你拿“语法新不新”来决定三者，又会在运行时边界上不断踩坑。

## 四、直觉 vs 真相

- 你以为：`struct` 就是更轻量的 `class`。  
  实际上：`struct` 的一阶语义是值，不是轻量。  
  原因是：它默认复制值本身，而不是共享对象身份。  
  要看细节，去：[CCLR-11｜值类型到底在哪里]({{< relref "engine-toolchain/cclr-11-where-value-types-live.md" >}})

- 你以为：`record` 只是少写点样板代码。  
  实际上：`record` 是在用语法声明“我想把这份数据当整体状态来比较和复制”。  
  原因是：它强化的是快照语义，而不是写法 convenience。  
  要看细节，去：[CCLR-09｜泛型约束和签名：不是更难写，而是更早把边界说清楚]({{< relref "engine-toolchain/cclr-09-generic-constraints-and-signatures.md" >}})

- 你以为：`class` 就等于“会放到堆上所以慢”。  
  实际上：`class` 先表达的是身份和共享关系，内存位置只是后续实现层的结果。  
  原因是：运行时会先看语义边界，再决定对象怎么布局和追踪。  
  要看细节，去：[CCLR-10｜对象在 CoreCLR 里怎么存在]({{< relref "engine-toolchain/cclr-10-object-layout-in-coreclr.md" >}})

## 五、在 Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR 里分别怎么落地

### 1. CoreCLR

CoreCLR 会把这三种声明方式继续展开成对象布局、值类型布局、调用约定和 GC 追踪边界。

但在你到达那一步之前，先记住：runtime 不会替你决定“这个模型是身份还是快照”，这是你在语言层必须先做出的建模判断。

继续追：[CCLR-10｜对象在 CoreCLR 里怎么存在]({{< relref "engine-toolchain/cclr-10-object-layout-in-coreclr.md" >}})

### 2. Mono

Mono 同样尊重这套语义边界，只是执行路径和宿主场景更灵活。

所以 `class` / `struct` / `record` 的第一层差异不会因为 Mono 而改变；变化的是它们在 JIT、AOT、解释执行路径里的具体代价。

继续追：[Mono 架构总览：嵌入式 runtime 与 Unity 历史位置]({{< relref "engine-toolchain/mono-architecture-overview-embedded-runtime-unity.md" >}})

### 3. IL2CPP

IL2CPP 会把这些边界带进构建链路，而不是在运行时全部现算。

所以当你在 Unity 里选择 `struct` 还是 `class` 时，影响的并不只是“语义”，还会进一步影响 AOT 生成、桥接和调试边界。

继续追：[IL2CPP 总管线：C# -> IL -> C++ -> Native]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})

### 4. HybridCLR

HybridCLR 不会重新定义这三种语言语义，但它会让某些跨边界调用、泛型共享和热更新路径的代价变得更敏感。

这也是为什么建模边界如果一开始没定清，到了热更新阶段会更难收。

继续追：[CCLR-15｜从 AOT 到热更新：为什么 HybridCLR 要补 metadata、解释器和 bridge]({{< relref "engine-toolchain/cclr-15-why-hybridclr-needs-metadata-interpreter-and-bridge.md" >}})

### 5. LeanCLR

LeanCLR 的价值之一，是它把“这些语义最后怎样落成对象模型”重新抓回到自己手里。

但它同样不能跳过你在语言层的建模选择。先有边界判断，后有 runtime 设计。

继续追：[CCLR-16｜从零到 CLR：LeanCLR 为什么选择另一条路]({{< relref "engine-toolchain/cclr-16-why-leanclr-takes-another-route.md" >}})

## 六、小结

- `class` 的关键词是身份，`struct` 的关键词是值，`record` 的关键词是快照。
- 这三者不是三种写法风格，而是三种建模边界。
- 先把边界定清楚，后面你看对象布局、装箱、泛型和跨 runtime 差异时才不会把“语义问题”误当成“性能问题”。

## 系列位置

- 上一篇：[CCLR-03｜string 和 object：一个最特殊，一个最基础]({{< relref "engine-toolchain/cclr-03-string-and-object.md" >}})
- 下一篇：[CCLR-05｜装箱与拆箱：什么时候只是转换，什么时候真的产生对象]({{< relref "engine-toolchain/cclr-05-boxing-and-unboxing.md" >}})
- 向下追深：[CCLR-10｜对象在 CoreCLR 里怎么存在]({{< relref "engine-toolchain/cclr-10-object-layout-in-coreclr.md" >}})
- 向旁对照：[IL2CPP 总管线：C# -> IL -> C++ -> Native]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})

> 本文是入口页。继续提交前，请本地运行一次 `hugo`，确认 `ERROR` 为零。
