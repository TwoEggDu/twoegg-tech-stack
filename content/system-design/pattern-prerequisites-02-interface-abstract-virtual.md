---
date: "2026-04-18"
title: "设计模式前置 02｜interface、abstract class、abstract、virtual、override：契约、骨架与扩展点"
description: "把 interface、abstract class、abstract、virtual、override 各自解决的问题拆开，Template Method、Strategy、State、Decorator、Bridge 才不会在同一层上混成一团。"
slug: "pattern-prerequisites-02-interface-abstract-virtual"
tags:
  - "设计模式"
  - "前置知识"
  - "C#"
  - "软件工程"
series: "设计模式前置知识"
weight: 897
---

> 一句话作用：先把“能力契约、流程骨架、必填步骤、可选钩子”这四件事分开，后面的设计模式文章才不会从术语层就开始打架。

## 一、先把概念放回各自的位置

这组词最容易被混在一起，因为它们都和“扩展”有关。但它们解决的其实不是同一个问题。

- `interface` 回答“你必须会做什么”
- `abstract class` 回答“你属于哪一类东西，公共骨架是什么”
- `abstract method` 回答“这一步必须由子类自己填”
- `virtual method` 回答“这里先给默认实现，必要时你再改”
- `override` 回答“子类现在正式接管这一步”

换句话说，`interface` 偏能力契约，`abstract class` 偏流程骨架。前者更像标准插头，后者更像已经搭好的脚手架。

如果把这几个词都当成“可继承、可重写”的不同写法，后面的模式就会很快变形。你会分不清某一步到底是必须实现，还是只是留了一个钩子；也会分不清某个类型到底是在表达“我必须有这个能力”，还是在表达“我接受这套骨架”。

## 二、最小可运行 C# 示例

下面这段代码把几种角色都放到了同一个导出流程里。

```csharp
using System;

public interface IExporter
{
    void Export(string content);
}

public abstract class ExporterBase : IExporter
{
    public void Export(string content)
    {
        BeforeExport(content);
        string payload = BuildPayload(content);
        Write(payload);
        AfterExport(payload);
    }

    protected virtual void BeforeExport(string content)
    {
        Console.WriteLine($"准备导出：{content.Length} 字符");
    }

    protected abstract string BuildPayload(string content);

    protected abstract void Write(string payload);

    protected virtual void AfterExport(string payload)
    {
        Console.WriteLine($"导出完成：{payload.Length} 字符");
    }
}

public sealed class CsvExporter : ExporterBase
{
    protected override string BuildPayload(string content)
    {
        return content.Replace("|", ",");
    }

    protected override void Write(string payload)
    {
        Console.WriteLine($"CSV: {payload}");
    }

    protected override void AfterExport(string payload)
    {
        Console.WriteLine("CSV 导出后收尾");
    }
}

public static class Program
{
    public static void Main()
    {
        IExporter exporter = new CsvExporter();
        exporter.Export("name|price|count");
    }
}
```

这里的角色非常典型：

- `IExporter` 是契约：只要求“你会导出”
- `ExporterBase` 是骨架：流程顺序在这里被定死
- `BuildPayload()` 和 `Write()` 是必填步骤：不实现，流程就不成立
- `BeforeExport()` 和 `AfterExport()` 是钩子：大多数子类可直接复用，少数场景再覆盖

真正的模板方法是 `Export()`。它不是“普通公共方法”，而是把顺序写死的总流程方法。

## 三、最常见误解

### 1. `interface` 和 `abstract class` 是替代关系

不是。`interface` 更像“能力契约”，`abstract class` 更像“共享骨架”。你可以同时拥有两者，就像上面的 `ExporterBase : IExporter`。

### 2. `abstract` 和 `virtual` 都是“可重写”

这句话太粗糙了。`abstract` 的重点是“必须重写”，`virtual` 的重点是“可以不改，先用默认实现”。

### 3. `abstract class` 里面就应该全是 `abstract`

不对。抽象类完全可以包含字段、构造函数、普通方法和默认实现。它的价值恰恰在于：一部分东西统一收进基类，一部分东西留给子类变化。

### 4. `override` 只是语法修饰词

不是。`override` 表示子类正式接管了父类给出的扩展点。它不是风格标记，而是行为替换的边界。

### 5. `sealed` 可有可无

也不对。`sealed` 的意义是“到此为止，不再允许向下扩展”。它能阻止本来只想开放一层的设计继续失控。

## 四、放进设计模式里怎么看

理解这组概念之后，设计模式里的很多结构会立刻变得顺眼。

- `Template Method` 依赖 `abstract class + abstract method + virtual hook`
- `Strategy` 更偏 `interface`，因为它关心“可替换算法”，不关心共享骨架
- `State` 常常两边都用：状态契约用 `interface`，状态基类用 `abstract class`
- `Decorator` 倾向于先要一个稳定契约，再决定是否要抽象基类帮忙收口默认行为
- `Bridge` 本质上也在拆：一边是抽象，一边是实现契约

你可以把这组词简化成一句非常好记的话：

`interface` 定能力，`abstract class` 定骨架，`abstract` 是必填步骤，`virtual` 是可选钩子。

## 五、读完这篇接着看哪些模式

- [Template Method]({{< relref "system-design/patterns/patterns-02-template-method.md" >}})
- [Strategy]({{< relref "system-design/patterns/patterns-03-strategy.md" >}})
- [Decorator]({{< relref "system-design/patterns/patterns-10-decorator.md" >}})
- [Adapter]({{< relref "system-design/patterns/patterns-11-adapter.md" >}})
- [State]({{< relref "system-design/patterns/patterns-48-state.md" >}})
- [Bridge]({{< relref "system-design/patterns/patterns-19-bridge.md" >}})

## 往下再走一步：它在 .NET / CLR 里怎么实现

如果你想继续往机制层走，这组概念最值得接这几篇：

- [CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})
- [RyuJIT：从 IL 到 IR 再到 Native Code]({{< relref "engine-toolchain/coreclr-ryujit-il-to-ir-to-native-code.md" >}})
- [跨运行时类型系统：MethodTable、Il2CppClass、RuntimeType]({{< relref "engine-toolchain/runtime-cross-type-system-methodtable-il2cppclass-rtclass.md" >}})

第一篇会把 `virtual / interface / override` 背后的类型和槽位边界补齐，第二篇解释 JIT 如何看待虚调用与去虚拟化，第三篇则把 `CoreCLR / IL2CPP / runtime-cross` 放到同一张图里看。

## 六、小结

- `interface` 管能力，`abstract class` 管骨架
- `abstract` 是必填步骤，`virtual` 是可选钩子
- 看懂这层，Template Method、Strategy、State、Decorator、Bridge 的代码形状就会顺很多
