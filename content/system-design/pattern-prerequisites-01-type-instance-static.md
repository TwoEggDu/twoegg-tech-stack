---
date: "2026-04-18"
title: "设计模式前置 01｜class、object、instance、static：行为和状态到底挂在哪"
description: "先把 class、object、instance、static 这几个最常混的词分开，后面的工厂、外观、单例、策略和依赖注入才不会在调用边界上打结。"
slug: "pattern-prerequisites-01-type-instance-static"
tags:
  - "设计模式"
  - "前置知识"
  - "C#"
  - "软件工程"
series: "设计模式前置知识"
weight: 896
---

> 一句话作用：先把“行为挂在类型上，还是挂在对象上”这件事讲清楚，后面的模式文章才不会从第一步就跑偏。

## 一、先把词拆开

`class` 不是对象。`class` 只是类型定义，是一张蓝图。`object` 或 `instance` 才是运行时真的活在内存里的实体。

这几个词经常被混着用，是因为它们都出现在同一段代码里。但它们回答的不是同一个问题。

- `class` 回答“这类东西长什么样”
- `object` 回答“运行时现在手里有哪一个实体”
- `instance method` 回答“这个行为要不要依赖某个对象当前的状态”
- `static method` 回答“这个行为是不是只做纯计算，不需要对象身份”

`static class` 又比 `static method` 更进一步。它不是“一个全局对象”，而是“根本不产生对象，只把一组静态成员收在一起”。

所以，静态不是“更高级的实例”，而是“根本没有实例”。

这层边界不清，后面的模式就很容易塌掉。你本来需要一个可替换的协作者，最后却写成了全局工具类；你本来需要对象状态驱动行为，最后却写成一堆只靠参数传来传去的静态函数。

## 二、最小可运行 C# 示例

下面这段代码故意把“对象级行为”和“静态工具函数”放在一起，让边界一眼可见。

```csharp
using System;

public sealed class ShippingOrder
{
    public decimal WeightKg { get; }
    public decimal DistanceKm { get; }

    public ShippingOrder(decimal weightKg, decimal distanceKm)
    {
        if (weightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg));
        }

        if (distanceKm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceKm));
        }

        WeightKg = weightKg;
        DistanceKm = distanceKm;
    }
}

public sealed class ShippingQuoteService
{
    private readonly decimal _baseFee;

    public ShippingQuoteService(decimal baseFee)
    {
        if (baseFee < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseFee));
        }

        _baseFee = baseFee;
    }

    public decimal Quote(ShippingOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        decimal raw = _baseFee + order.WeightKg * 2m + order.DistanceKm * 1.5m;
        return Money.RoundUpToCents(raw);
    }
}

public static class Money
{
    public static decimal RoundUpToCents(decimal value)
    {
        return Math.Ceiling(value * 100m) / 100m;
    }
}

public static class Program
{
    public static void Main()
    {
        var service = new ShippingQuoteService(6m);
        var order = new ShippingOrder(3.2m, 14m);

        Console.WriteLine(service.Quote(order));
    }
}
```

这段代码里，`ShippingQuoteService` 是对象，因为它带着 `_baseFee` 这类状态。你如果换一个基础费用，就需要一个新的服务实例。

`Money` 则适合作为静态工具。它只做纯计算，不保存任何对象身份，也不需要跨调用保留状态。

这不是语法风格问题，而是边界问题。对象级行为适合进入协作关系，静态工具适合做稳定、无状态、可重用的转换。

## 三、最常见误解

### 1. `class` 就是对象

不是。`class` 是类型，`object` 才是类型在运行时的某一个实例。

### 2. `static class` 就是单例

也不是。单例是“全局只保留一个实例”，静态类是“根本没有实例”。这两者的初始化方式、测试方式和扩展方式都不同。

### 3. 只要能写成 `static`，就应该写成 `static`

错。`static` 适合纯工具和纯转换；一旦你需要状态、替换、注入、模拟、生命周期控制，静态边界就会很快变硬。

### 4. 实例方法只是“写起来更面向对象”

不对。实例方法的关键在于：它天然能访问对象状态，也天然能参与对象之间的协作边界。

### 5. `property` 和字段是一回事

也不对。字段是存储，属性是访问边界。属性可以做校验、惰性计算、只读暴露；字段不承担这些职责。

## 四、放进设计模式里怎么看

读模式文章时，先问一个非常实际的问题：这段行为到底属于“某个对象”，还是属于“这个类型的公共工具能力”？

- 工厂模式围绕“谁负责创建对象”工作，所以天然和 `class`、构造过程、实例边界有关
- 外观模式通常暴露一个实例级入口，把多个对象协作收成一条稳定路径
- 策略、装饰器、桥接更强调“可替换的对象协作者”，所以它们通常不适合写成静态工具
- 单例经常被误写成静态类，但两者不是一个问题：前者是“只有一个对象”，后者是“没有对象”

一句话记住：

`static` 适合工具，`instance` 适合协作。设计模式大多数时候讨论的都是后者。

## 五、读完这篇接着看哪些模式

- [Factory Method 与 Abstract Factory]({{< relref "system-design/patterns/patterns-09-factory.md" >}})
- [Facade]({{< relref "system-design/patterns/patterns-05-facade.md" >}})
- [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})
- [Decorator]({{< relref "system-design/patterns/patterns-10-decorator.md" >}})
- [依赖注入与 Service Locator]({{< relref "system-design/patterns/patterns-27-di-vs-service-locator.md" >}})

## 往下再走一步：它在 .NET / CLR 里怎么实现

如果你想把“类型、对象、实例、静态”从概念层继续往下追，可以看这几篇机制文：

- [程序集与 IL：编译后到底留下了什么]({{< relref "engine-toolchain/build-debug-02c-dotnet-assembly-and-il.md" >}})
- [CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})
- [跨运行时类型系统：MethodTable、Il2CppClass、RuntimeType]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})

第一篇让你看到 C# 编译后留下的 IL 和 metadata；第二篇解释 `class` 在 CoreCLR 里怎样挂到 `MethodTable` 和 `EEClass` 上；第三篇则把同一组概念放到 `CoreCLR / IL2CPP / runtime-cross` 里做横向对照。

## 六、小结

- `class` 是类型定义，`object` 才是运行时实体
- `static` 解决的是“不要对象也能调用”，不是“更高级的实例”
- 设计模式大多数时候讨论的是对象协作边界，不是静态工具收纳技巧
