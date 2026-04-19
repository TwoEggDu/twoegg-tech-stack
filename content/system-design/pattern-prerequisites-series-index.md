---
date: "2026-04-18"
title: "设计模式前置知识索引｜先补地基，再读模式"
slug: "pattern-prerequisites-series-index"
description: "把设计模式专题的 5 篇前置知识、.NET 与 CLR 机制文章和模式正文放进同一张三层阅读地图，先补概念，再追实现，最后回到模式。"
tags:
  - "设计模式"
  - "前置知识"
  - "索引"
  - "阅读入口"
  - "C#"
series: "设计模式前置知识"
weight: 895
series_id: "pattern-prerequisites"
series_role: "index"
series_order: 0
series_entry: true
series_audience:
  - "第一次系统读设计模式的工程师"
  - "想把设计概念和 .NET 运行时机制连起来的读者"
  - "已经在读模式正文，但总觉得卡在基础概念上的读者"
series_level: "前置到进阶"
series_best_for: "适合先补地基，再进入模式正文和机制深挖的读者。"
series_summary: "这组文章不讲语言大全，只补设计模式反复会用到的概念地基，并把它们和 .NET 与 CLR 的实现机制桥接起来。"
series_intro: "如果你读模式文章时老是卡在 interface、abstract、virtual、delegate、readonly 这些词上，就先从这里走一遍。"
series_reading_hint: "先读概念层，再按兴趣进入实现机制层或模式层，不必一次吃完整套。"
---

> 这页不是正文，而是一张阅读地图：先补概念，再追机制，最后回到模式。

## 一、先看这张三层阅读地图

这组前置知识的目标很明确：不把你变成 C# 语法专家，只把设计模式里最容易反复绊脚的那批概念讲清楚。

整套阅读线可以拆成三层：

1. **概念层**：先弄清楚词义和设计角色，比如 `interface` 到底在表达什么，`virtual` 和 `abstract` 差在哪。
2. **实现机制层**：再去看 .NET / CLR 怎样把这些概念落地，比如类型系统、方法分派、IL、状态机和线程池。
3. **模式层**：最后回到 Template Method、Strategy、Command、Builder 这些文章，看它们为什么会写成现在这个结构。

如果直接跳到第三层，读者很容易出现一种错觉：代码好像看懂了，但概念没有抓牢。前置知识的任务，就是先把这层雾清掉。

## 二、先走概念层

建议按这个顺序阅读：

1. [类型、对象、实例、静态：代码到底挂在谁身上]({{< relref "system-design/pattern-prerequisites-01-type-instance-static.md" >}})
2. [interface、abstract class、abstract、virtual、override：契约、骨架与扩展点]({{< relref "system-design/pattern-prerequisites-02-interface-abstract-virtual.md" >}})
3. [继承、组合、依赖：什么时候该继承，什么时候该拼装]({{< relref "system-design/pattern-prerequisites-03-inheritance-composition-dependency.md" >}})
4. [委托、回调、事件：为什么很多模式今天看起来更轻了]({{< relref "system-design/pattern-prerequisites-04-delegate-callback-event.md" >}})
5. [const、readonly、static readonly、immutable：值什么时候该定下来]({{< relref "system-design/pattern-prerequisites-05-const-readonly-immutability.md" >}})

它们分别回答的是五类不同问题：

- 行为到底挂在对象上，还是挂在类型上
- 契约、骨架、必填步骤和可选扩展点怎么区分
- 什么时候应该让对象协作，而不是往继承树上继续堆
- 现代语言怎样把传统模式写轻
- 哪些值应该在创建后尽快收口成稳定边界

## 三、想追根到底，就进入实现机制层

概念层解决的是“设计上是什么意思”，实现机制层解决的是“运行时到底怎么做到”。

这几篇最值得和前置知识配套读：

- [程序集与 IL：编译后到底留下了什么]({{< relref "engine-toolchain/build-debug-02c-dotnet-assembly-and-il.md" >}})
- [CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})
- [跨运行时类型系统：MethodTable、Il2CppClass、RuntimeType]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})
- [ET 前置：async/await、状态机与 continuation]({{< relref "et-framework-prerequisites/et-pre-02-async-await-state-machine-and-continuation.md" >}})

如果你关心的是下面这些问题，这层就很有价值：

- `virtual` 为什么真的能被重写
- `interface` 调用为什么和普通实例方法分派不同
- 委托、事件、回调到底怎么跑起来
- `async/await` 为什么看起来像同步，底层却不是同步
- `const`、`readonly`、对象布局和生命周期到底落在什么边界上

## 四、最后回到模式层

当概念层和机制层都打通之后，再读模式会顺很多。与这 5 篇前置文最相关的模式文章，可以按问题簇来看。

### 1. 类型与静态边界

- [Facade]({{< relref "system-design/patterns/patterns-05-facade.md" >}})
- [Factory Method 与 Abstract Factory]({{< relref "system-design/patterns/patterns-09-factory.md" >}})
- [Adapter]({{< relref "system-design/patterns/patterns-11-adapter.md" >}})

### 2. 契约与骨架

- [Template Method]({{< relref "system-design/patterns/patterns-02-template-method.md" >}})
- [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})
- [Decorator]({{< relref "system-design/patterns/patterns-10-decorator.md" >}})
- [Bridge]({{< relref "system-design/patterns/patterns-19-bridge.md" >}})
- [State]({{< relref "system-design/patterns/patterns-48-state.md" >}})

### 3. 组合与依赖

- [Template Method]({{< relref "system-design/patterns/patterns-02-template-method.md" >}})
- [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})
- [Decorator]({{< relref "system-design/patterns/patterns-10-decorator.md" >}})
- [Bridge]({{< relref "system-design/patterns/patterns-19-bridge.md" >}})
- [依赖注入与 Service Locator]({{< relref "system-design/patterns/patterns-27-di-vs-service-locator.md" >}})

### 4. 行为传递与通知

- [Command]({{< relref "system-design/patterns/patterns-06-command.md" >}})
- [Observer]({{< relref "system-design/patterns/patterns-07-observer.md" >}})
- [Promise / Future / async-await]({{< relref "system-design/patterns/patterns-21-async-await.md" >}})
- [Pipeline / Pipes and Filters]({{< relref "system-design/patterns/patterns-24-pipeline.md" >}})

### 5. 值边界与快照

- [Builder]({{< relref "system-design/patterns/patterns-04-builder.md" >}})
- [Flyweight]({{< relref "system-design/patterns/patterns-17-flyweight.md" >}})
- [Prototype]({{< relref "system-design/patterns/patterns-20-prototype.md" >}})
- [Object Pool]({{< relref "system-design/patterns/patterns-47-object-pool.md" >}})
- [数据导向设计]({{< relref "system-design/patterns/patterns-49-data-oriented-design.md" >}})

## 五、如果你是不同类型的读者，该怎么选

### 第一次系统读这个专题

按顺序读完 5 篇前置知识，再去看 Template Method、Strategy、Factory、Command、Observer 这些正文，理解成本最低。

### 你更关心 .NET / CLR 底层

从前置 02 和前置 04 开始，然后跳到类型系统、IL、状态机、线程池这些机制文，会更有收益。

### 你已经在读模式正文，但总觉得卡

不要从头重读整套专栏，直接回跳到对应的前置知识：

- 卡在 Template Method / State / Decorator：先回前置 02
- 卡在 Strategy / Bridge / DI：先回前置 03
- 卡在 Command / Observer / async：先回前置 04
- 卡在 Builder / Flyweight / Prototype：先回前置 05

## 六、这组前置知识怎么互相跳

它们不是 5 篇互不相干的小文，而是一条逐步加深的阅读线。

- 前置 01 先把类型、对象、静态边界拉直
- 前置 02 在这个基础上讲契约、骨架、扩展点
- 前置 03 再讲对象关系怎么组织
- 前置 04 说明现代语言怎样让行为本身可传递
- 前置 05 最后把值边界、只读约束和不可变收口

如果你只打算先读两篇，我建议先读 `01 + 02`。这两篇会直接决定你后面能不能真正读懂 Template Method、Strategy、Factory、Decorator 这些文章。

## 小结

- 这组前置知识负责补设计模式的地基，不负责替代模式正文
- 实现机制层负责解释运行时是怎么做到的，适合想继续追底层的读者
- 把概念层、机制层、模式层串起来以后，整套专题会变得好读很多
