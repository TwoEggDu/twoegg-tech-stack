---
date: "2026-04-18"
title: "设计模式前置 05｜const、readonly、static readonly：值什么时候该定下来"
description: "把 const、readonly、static readonly 和不可变对象的边界分开，避免把编译期常量、构造后只读、类型级共享值和快照式不可变混成一个词。"
slug: "pattern-prerequisites-05-const-readonly-immutability"
weight: 900
tags:
  - "设计模式"
  - "前置知识"
  - "C#"
  - "const"
  - "readonly"
  - "immutable"
series: "设计模式前置知识"
---

> 一句话作用：先把“值什么时候定下来、之后还能不能改”讲清楚，后面的 Builder、Flyweight、Prototype、Object Pool 和 DOD 才知道该把边界收在哪。

## 一、先把四层边界拆开

这几个词看起来都在说“不要改”，但它们锁住的不是同一件事。

| 概念 | 什么时候确定 | 之后还能不能重绑 | 适合放什么 |
|---|---|---|---|
| `const` | 编译期 | 不能 | 真正常量、固定标识 |
| `readonly` | 构造期 | 只能在构造期间赋值一次 | 身份、依赖、创建后不该重绑的字段 |
| `static readonly` | 类型初始化期 | 只能在静态构造阶段赋值一次 | 运行时算出来但全局共享的默认值 |
| immutable | 对象创建后 | 不在原地修改，而是生成新值 | 快照、共享配置、值对象 |

这里最容易被忽略的一点是：`readonly` 锁住的是“字段引用不再换”，不等于“这个字段指向的对象内部绝对不变”。

所以，不要把 `readonly` 和“不可变对象”当成同义词。前者是字段约束，后者是对象语义。

## 二、最小可运行 C# 示例

下面这段代码把四种边界放在同一处。

```csharp
using System;

public static class ExportDefaults
{
    public const string CsvFormat = "csv";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
}

public sealed record ExportPlan(string Name, string Format, int BatchSize, TimeSpan Timeout)
{
    public static ExportPlan CreateDefault(string name)
        => new(name, ExportDefaults.CsvFormat, 500, ExportDefaults.DefaultTimeout);
}

public sealed class ConsoleLogger
{
    public void Write(string message) => Console.WriteLine(message);
}

public sealed class ExportRunner
{
    private readonly ConsoleLogger _logger;
    private readonly Guid _runnerId = Guid.NewGuid();

    public ExportRunner(ConsoleLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Run(ExportPlan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        _logger.Write($"[{_runnerId}] export {plan.Name}");
        _logger.Write($"format={plan.Format}, batch={plan.BatchSize}, timeout={plan.Timeout.TotalSeconds}s");
    }
}

public static class Program
{
    public static void Main()
    {
        var baseline = ExportPlan.CreateDefault("DailyInvoices");
        var tuned = baseline with { BatchSize = 1000 };

        var runner = new ExportRunner(new ConsoleLogger());
        runner.Run(tuned);
    }
}
```

这段代码里：

- `CsvFormat` 是编译期就定下来的常量
- `DefaultTimeout` 不是编译期常量，但类型级共享，所以用 `static readonly`
- `_logger` 是对象构造后就不该换掉的依赖，所以用 `readonly`
- `ExportPlan` 用 `record` 表示“修改时不在原地改，而是生成新快照”

`baseline with { BatchSize = 1000 }` 这一步非常关键。它展示的不是“语法糖很方便”，而是“变化应该以新值的形式出现，而不是在原对象上打补丁”。

## 三、最常见误解

### 1. `const` 和 `readonly` 是一回事

不是。`const` 是编译期常量，`readonly` 是构造后只读字段。前者适合永远不变的字面量，后者适合对象创建后不再重绑的成员。

### 2. `readonly` 就等于对象不可变

不对。`readonly` 只保证字段本身不换引用，不保证引用指向的对象内部不变。

```csharp
private readonly System.Collections.Generic.List<string> _tags = new();

public void AddTag(string tag)
{
    _tags.Add(tag);
}
```

这段代码合法，因为字段没换，变的是列表内部状态。

### 3. `static readonly` 只是“更高级的 `const`”

也不对。`static readonly` 解决的是“运行时才能算出来，但又想全局共享”的值。它和 `const` 的时机完全不同。

### 4. 不可变对象就是“没有 setter”

不准确。没有 setter 只是一个常见外观，不是本质。本质是：修改时返回新值，而不是在原对象上改。

### 5. 公共 `const` 随便放在库里没问题

这很危险。公共 `const` 容易被调用方在编译期内联，后面即使你改了库里的值，调用方不重新编译也可能继续拿到旧值。

## 四、放进设计模式里怎么看

这几个概念本身不是模式，但它们决定了模式边界怎么收。

- `Builder` 的构造过程可以是可变的，但最终产物最好尽快收成稳定对象
- `Flyweight` 最怕共享可变状态，所以共享部分通常应尽量不可变
- `Prototype` 复制的是一份已经调好的快照，不是随时可被别人继续乱改的草稿
- `Factory` 常常负责发放默认值和预设值，这时 `const / static readonly` 就会自然出现
- `Object Pool` 和 `DOD` 更要求你分清“什么是对象身份，什么是可重置状态，什么是应该作为快照传递的配置”

一句话记住：

先分清“常量、只读字段、共享默认值、不可变快照”，后面很多模式的边界自然会稳。

## 五、读完这篇接着看哪些模式

- [Builder]({{< relref "system-design/patterns/patterns-04-builder.md" >}})
- [Factory Method 与 Abstract Factory]({{< relref "system-design/patterns/patterns-09-factory.md" >}})
- [Flyweight]({{< relref "system-design/patterns/patterns-17-flyweight.md" >}})
- [Prototype]({{< relref "system-design/patterns/patterns-20-prototype.md" >}})
- [Object Pool]({{< relref "system-design/patterns/patterns-47-object-pool.md" >}})
- [数据导向设计]({{< relref "system-design/patterns/patterns-49-data-oriented-design.md" >}})

## 往下再走一步：它在 .NET / CLR 里怎么实现

如果你想把“值什么时候定下来”继续往下追，最适合接这几篇：

- [程序集与 IL：编译后到底留下了什么]({{< relref "engine-toolchain/build-debug-02c-dotnet-assembly-and-il.md" >}})
- [内存模型、对象布局与 GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})
- [CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})

第一篇会把 `const` 和字段初始化留下的 IL 区别拉出来，第二篇帮助你理解对象布局、字段生命周期和共享边界，第三篇则把类型、字段和运行时表示连接起来。

## 六、小结

- `const` 是编译期常量，`readonly` 是构造后只读，`static readonly` 是类型级共享只读值
- 不可变对象不等于“没有 setter”，而是“修改时产生新值”
- Builder、Flyweight、Prototype、Object Pool、DOD 都会直接受这层边界影响
