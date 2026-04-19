---
date: "2026-04-18"
title: "设计模式前置 04｜委托、回调、事件：让行为变成可以传递的值"
description: "委托、回调、事件解决的是同一类问题：把一段会变化的行为从类里拿出来，交给调用方、订阅方或者运行时去决定。它们是理解 Strategy、Command、Observer 和 Pipeline 的现代语言入口。"
slug: "pattern-prerequisites-04-delegate-callback-event"
tags:
  - "设计模式"
  - "前置知识"
  - "C#"
  - "软件工程"
series: "设计模式前置知识"
weight: 899
---

> 一句话作用：把“要做什么”从类层次里拆出来，改成可以传递、可以订阅、可以在运行时替换的行为值。

## 一、概念拆分

这几个词常被混在一起，其实分别站在不同角度。

- 委托：一种“函数签名类型”，表示某个位置可以放一个方法
- 回调：把方法交给别人，等对方在合适时机调用
- 事件：带订阅限制的回调集合，发布者负责触发，外部只能订阅或取消订阅

更直白地说，委托像插座，回调像你把充电器留给服务台，事件像广播频道。三者都在表达“行为可以被外部注入”，但控制权不一样。

现代 C# 里的 `Func<>` 和 `Action<>` 让这件事更轻了。很多过去必须写成一堆策略类、观察者类、命令类的地方，现在一个委托就能把核心变化点提出来。

## 二、最小可运行 C# 示例

下面这个例子同时演示委托、回调和事件。

```csharp
using System;

public delegate decimal PricingRule(decimal amount);

public sealed class JobRunner
{
    public event EventHandler<JobCompletedEventArgs>? Completed;

    public decimal Run(decimal amount, PricingRule rule, Action<string>? onStep = null)
    {
        onStep?.Invoke("开始计算");

        decimal result = rule(amount);

        onStep?.Invoke("计算完成");
        OnCompleted(result);
        return result;
    }

    private void OnCompleted(decimal result)
    {
        Completed?.Invoke(this, new JobCompletedEventArgs(result));
    }
}

public sealed class JobCompletedEventArgs : EventArgs
{
    public decimal Result { get; }

    public JobCompletedEventArgs(decimal result)
    {
        Result = result;
    }
}

public static class Demo
{
    public static void Main()
    {
        var runner = new JobRunner();

        runner.Completed += (_, e) =>
        {
            Console.WriteLine($"事件通知：结果是 {e.Result}");
        };

        decimal total = runner.Run(
            100m,
            amount => amount * 0.9m,
            step => Console.WriteLine($"回调：{step}")
        );

        Console.WriteLine($"最终金额：{total}");
    }
}
```

这里的分工很清楚：

- `PricingRule` 是委托类型
- `amount => amount * 0.9m` 是一个传入的可替换算法
- `onStep` 是回调
- `Completed` 是事件

同一段逻辑里，委托负责“算法可替换”，回调负责“过程可通知”，事件负责“完成后广播”。

## 三、最常见误解

- 误解一：委托就是回调。不是。委托是类型，回调是用法。
- 误解二：事件就是委托。也不是。事件底层常用委托，但它限制了外部能做什么。
- 误解三：lambda 就是委托。lambda 只是写法，最终还是要落到委托类型上。
- 误解四：事件一定异步。不是。事件本身只是同步触发的通知机制，异步是另外一层选择。

## 四、放进设计模式里怎么看

当你学完这些，就会发现很多模式在现代语言里会变轻。

- Strategy 常常退化成一个委托参数
- Command 常常退化成一个动作委托，尤其是没有撤销需求时
- Observer 常常退化成事件或事件聚合器
- Pipeline 和 async/await 常常把回调链写得更短

所以，看到“类很多”时先别急着上模式。先问一句：这里是不是只需要一个函数值、一个回调，或者一个事件订阅？

如果答案是“是”，那很多传统模式就不用上到类层级。你不是在削弱设计，而是在把设计收紧到刚好够用。

## 五、读完这篇接着看哪些模式

- [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})
- [Command]({{< relref "system-design/patterns/patterns-06-command.md" >}})
- [Observer]({{< relref "system-design/patterns/patterns-07-observer.md" >}})
- [Pipeline]({{< relref "system-design/patterns/patterns-24-pipeline.md" >}})
- [async-await]({{< relref "system-design/patterns/patterns-21-async-await.md" >}})

## 往下再走一步：它在 .NET / CLR 里怎么实现

委托、回调和事件在概念层看起来很轻，但它们背后其实连着线程池、状态机和运行时调用机制。继续往下读，最合适的是这几篇：

- [ET 前置：async/await、状态机与 continuation]({{< relref "et-framework-prerequisites/et-pre-02-async-await-state-machine-and-continuation.md" >}})
- [CoreCLR 线程、同步、Monitor 与线程池]({{< relref "engine-toolchain/coreclr-threading-synchronization-thread-monitor-threadpool.md" >}})
- [CoreCLR 架构总览：从 dotnet run 到 JIT]({{< relref "engine-toolchain/coreclr-architecture-overview-dotnet-run-to-jit.md" >}})

第一篇会把 continuation 和状态机怎么把回调链收成顺序代码讲清楚，第二篇会把线程池和同步边界补上，第三篇则让你看到一次委托调用最终怎样落回运行时管线。读完之后再看 Command、Observer、Pipeline、async/await，会更容易判断“这里到底是在传递一个对象，还是在传递一个行为”。

## 六、小结

- 委托是“可传递的函数类型”，回调是“把函数交给别人以后由别人调用”，事件是“受限的订阅式通知”
- 现代 C# 把很多经典模式压缩成了更轻的委托、回调和事件
- 先判断问题是否只需要行为值，再决定要不要上到完整的类层次设计

