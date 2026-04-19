---
date: "2026-04-18"
title: "设计模式前置 03｜继承、组合、依赖：什么时候该继承，什么时候该拼装"
description: "先把继承、组合、依赖这三件事分开，后面的设计模式才不会全挤成一团。它们分别回答“我是什么”“我手里有什么”“我使用谁”，也是理解模板方法、策略、装饰器和桥接的基础。"
slug: "pattern-prerequisites-03-inheritance-composition-dependency"
tags:
  - "设计模式"
  - "前置知识"
  - "C#"
  - "软件工程"
series: "设计模式前置知识"
weight: 898
---

> 一句话作用：把“类型关系”和“对象协作”分开看，后面的模式文章才不会把继承、组合、依赖混成一锅。

## 一、概念拆分

继承、组合、依赖看起来都在说“两个类怎么连在一起”，其实问的是三件不同的事。

- 继承回答“我是什么”
- 组合回答“我手里有什么”
- 依赖回答“我使用谁”

继承更像分类。`CsvExportJob` 是一种 `ExportJob`，所以它可以复用基类流程，也可以接管某些步骤。组合更像装配。一个对象把另一个对象放进自己内部，靠它完成某个子任务。依赖更像合作。当前对象不一定拥有对方，但它在某个时刻需要对方帮忙。

这三者不要混成同一个词。很多设计失败，都是因为开发者把“可以继承”误当成“应该继承”，最后把原本应该松耦合的协作关系，硬拧成了类层级。

## 二、最小可运行 C# 示例

下面这个例子把三者放在同一段代码里。

```csharp
using System;

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public abstract class ExportJob
{
    protected readonly IClock Clock;

    protected ExportJob(IClock clock)
    {
        Clock = clock;
    }

    public void Run()
    {
        Before();
        ExportCore();
        After();
    }

    protected virtual void Before()
    {
        Console.WriteLine($"开始导出：{Clock.UtcNow:O}");
    }

    protected abstract void ExportCore();

    protected virtual void After()
    {
        Console.WriteLine("导出结束");
    }
}

public sealed class CsvExportJob : ExportJob
{
    public CsvExportJob(IClock clock) : base(clock)
    {
    }

    protected override void ExportCore()
    {
        Console.WriteLine("生成 CSV 文件");
    }
}

public sealed class ExportFacade
{
    private readonly ExportJob _job;

    public ExportFacade(ExportJob job)
    {
        _job = job;
    }

    public void Export()
    {
        _job.Run();
    }
}

public static class Demo
{
    public static void Main()
    {
        var facade = new ExportFacade(new CsvExportJob(new SystemClock()));
        facade.Export();
    }
}
```

这段代码里：

- `CsvExportJob : ExportJob` 是继承
- `ExportFacade` 持有 `ExportJob`，这是组合
- `ExportJob` 通过构造函数拿到 `IClock`，这是依赖

同一段代码里，三种关系都在，但各自解决的事不同。

## 三、最常见误解

- 误解一：继承比组合高级。不是。继承只是更强的耦合方式，只有当“父子类型关系稳定”时才值得用。
- 误解二：组合只是“加几个字段”。不是。组合强调的是对象之间的协作边界，不是语法上的成员变量。
- 误解三：依赖就是成员字段。不是。参数、局部变量、返回值、构造注入都可能形成依赖。
- 误解四：能继承就先继承。也不是。只要你想替换行为、隔离变化，组合通常比继承更安全。

## 四、放进设计模式里怎么看

读设计模式时，可以拿这三条线去对号入座。

- 看到“基类控制流程，子类补细节”，先想 Template Method
- 看到“把可变行为塞进独立对象”，先想 Strategy、Decorator、Bridge
- 看到“对象靠注入的协作者完成工作”，先想 Facade、Factory、DI

更具体一点：

- 模式里如果出现 `abstract class` 和 `protected virtual`，通常是在用继承表达骨架
- 模式里如果出现“把一个对象放进另一个对象里”，通常是在用组合包住变化
- 模式里如果出现“传入接口作为参数”，通常是在隔离依赖

所以，理解这三者，不是为了背术语，而是为了看懂模式为什么这么选。

## 五、读完这篇接着看哪些模式

- [Template Method]({{< relref "system-design/patterns/patterns-02-template-method.md" >}})
- [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})
- [Decorator]({{< relref "system-design/patterns/patterns-10-decorator.md" >}})
- [Facade]({{< relref "system-design/patterns/patterns-05-facade.md" >}})
- [Factory Method 与 Abstract Factory]({{< relref "system-design/patterns/patterns-09-factory.md" >}})
- [Bridge]({{< relref "system-design/patterns/patterns-19-bridge.md" >}})

## 往下再走一步：它在 .NET / CLR 里怎么实现

继承、组合、依赖本质上是设计关系，但它们最后都会落到对象图、引用边界和生命周期上。如果你想往机制层再走一步，可以看：

- [CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})
- [内存模型、对象布局与 GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})
- [CoreCLR GC：分代、精确、工作站与服务器模式]({{< relref "engine-toolchain/coreclr-gc-generational-precise-workstation-server.md" >}})

第一篇让你看清类型层级和对象引用是怎么被运行时理解的，第二篇解释对象布局和引用边界，第三篇则让你看到对象图和生命周期为什么会影响真实性能。这样再回头看“组合优于继承”时，就不会只把它当成一句口号。

## 六、小结

- 继承回答“我是什么”，组合回答“我手里有什么”，依赖回答“我使用谁”
- 继承适合稳定的类型层级，组合适合可替换的协作部件，依赖适合解耦调用边界
- 后面看 Template Method、Strategy、Decorator、Bridge 时，先判断它们是在用哪一种关系

