---
title: "CCLR-03｜string 和 object：一个最特殊，一个最基础"
slug: "cclr-03-string-and-object"
date: "2026-04-19"
description: "object 是整棵类型树的共同根，string 则是最像值、但仍然按引用类型活着的那个特例。把这两个词放回 runtime，你后面看相等性、装箱和对象布局时才不会串层。"
tags:
  - "C#"
  - "CLR"
  - "CCLR"
  - "String"
  - "Object"
  - "Runtime"
series: "从 C# 到 CLR"
series_id: "csharp-to-clr"
weight: 1803
---

> `object` 统一了“所有对象至少都得会什么”，`string` 则提醒你：运行时里最难懂的类型，往往不是最复杂的那个，而是最常见的那个。

这是 `从 C# 到 CLR` 系列的第 3 篇。前两篇先把“值类型 / 引用类型 / 对象”和“内建类型契约”拆开；这一篇继续往下走，把 `string` 和 `object` 这两个最常见、也最容易被误读的词重新摆到 runtime 坐标系里。

> **本文明确不展开的内容：**
> - `string` 对象布局、驻留池实现、GC 细节（在 [ECMA-335 内存模型]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) 和后续对象布局文继续追）
> - `boxing / unboxing` 的完整成本模型（在 [CCLR-05]({{< relref "engine-toolchain/cclr-05-boxing-and-unboxing.md" >}}) 展开）
> - `Span<T>`、`ReadOnlySpan<T>`、UTF 编码和文本处理 API 的完整边界（不在这条入口线里展开）

## 一、为什么这篇单独存在

很多工程师会把 `string` 和 `object` 当成两个太基础、所以没必要单独讲的词。

这恰好相反。越基础的词，越容易在脑子里糊成一句错误直觉。

`object` 常见的误解是“万能盒子”。这句话勉强够你过第一关，却完全解释不了：为什么所有引用类型最终都挂在它下面；为什么值类型一旦进入 `object` 语义边界，就会变成另一套运行时问题；为什么 `GetType()`、`ToString()`、`Equals()` 这些最基础的方法能统一存在。

`string` 常见的误解则是“就是字符数组”。这句话更危险。它会让你误以为 `string` 只是一个方便的文本容器，而忽略掉它在语言层、metadata 层、对象语义层都是一个极特殊的存在：

- 它是引用类型
- 它有对象身份
- 但它默认按值语义去比较内容
- 它又经常被编译器和 runtime 额外照顾

如果这两个词不拆开，后面你看对象布局、相等性、装箱、方法分派时都会不断串层。

## 二、最小可运行示例

```csharp
using System;

public sealed class Customer
{
    public int Id { get; }
    public string Name { get; }

    public Customer(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => $"{Id}:{Name}";
}

public static class Demo
{
    public static void Main()
    {
        string literal = "hello";
        string rebuilt = new string(new[] { 'h', 'e', 'l', 'l', 'o' });
        string interned = string.Intern(rebuilt);

        object any = new Customer(7, "Ada");

        Console.WriteLine(literal == rebuilt);
        Console.WriteLine(object.ReferenceEquals(literal, rebuilt));
        Console.WriteLine(object.ReferenceEquals(literal, interned));
        Console.WriteLine(any.GetType().Name);
        Console.WriteLine(any.ToString());
    }
}
```

这段代码里，最关键的不是“打印了什么”，而是它把两件事硬拆开了。

第一，`string` 的默认比较更接近“内容是否相同”，而不是“是不是同一个对象”。

第二，`object` 作为变量类型时，只负责提供一个统一入口；真正的方法分派、真实布局、真实类型信息仍然落在运行时里的那个具体对象上。

也就是说，`string` 是对象，但不像多数对象那样先强调身份；`object` 是根类型，但并不抹平真实类型。

## 三、把核心概念分清

### 1. `object` 是共同根，不是万能袋

TL;DR：`object` 的价值不在“什么都能装”，而在“什么都能以这套最低共同接口被看见”。

从 C# 表层看，所有引用类型最终都派生自 `object`，值类型也能通过装箱进入 `object` 语义边界。

从 runtime 看，这意味着对象世界至少共享一组最小公约数：类型身份、相等性入口、字符串表示、哈希入口。

这就是为什么后面你看 `MethodTable`、对象头、类型句柄时，会发现运行时总需要一个共同的根。没有这个根，很多跨类型的统一行为根本没法成立。

所以 `object` 的第一层意义不是“能装很多类型”，而是“给所有对象行为提供一个统一最低面”。

### 2. `string` 是引用类型，但它故意长得像值

TL;DR：`string` 仍然是对象，但语言和库故意让你先把它当“内容快照”来用。

`string` 有对象身份。你可以对它做 `ReferenceEquals`，可以看到 intern 之后多个变量指向同一份对象。

但 `string` 又不鼓励你用对象身份来思考它。日常代码里，你更多在意的是：

- 文本内容是不是相同
- 这份值是不是可变
- 这个字面量是不是能共享

这就是它特殊的地方：它是一个被设计成“更像值”的引用类型。

这个定位非常重要。因为如果你只记“`string` 是引用类型”，你会误以为它应该优先按身份思考；如果你只记“`string` 像值”，你又会忽略它仍然活在对象世界里，会被 GC 管理，会参与对象共享、intern 和运行时布局。

### 3. `object` 讲共同面，`string` 讲特殊契约

TL;DR：`object` 的关键词是统一，`string` 的关键词是特例。

你可以把 `object` 理解成“整个对象世界最低共同接口的门牌号”。

你可以把 `string` 理解成“这个世界里被特别照顾的一位住户”。它没有脱离对象系统，但它的默认使用方式和普通引用类型已经不一样了。

这两层区分很关键，因为后面很多误解都来自于把“统一根”和“特殊引用类型”混成一件事。

## 四、直觉 vs 真相

- 你以为：`object` 就是一个装什么都行的大盒子。  
  实际上：`object` 的真正价值是提供对象系统的共同最低面，而不是当容器。  
  原因是：runtime 需要一个统一根，来承接类型身份、相等性入口和最小公共行为。  
  要看细节，去：[CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})

- 你以为：`string` 只是一个字符数组。  
  实际上：`string` 是一个被特意设计成“像值一样使用”的引用类型。  
  原因是：它既有对象身份，又默认按内容语义工作，还会受到编译器和 runtime 的额外照顾。  
  要看细节，去：[ECMA-335 内存模型、对象布局与 GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})

- 你以为：只要变量类型写成 `object`，运行时就看不见原始类型了。  
  实际上：`object` 变量只是入口统一，真实对象类型仍然存在，`GetType()` 和虚调用都会继续回到真实类型。  
  原因是：引用被抬高，不等于对象被抹平。  
  要看细节，去：[CCLR-12｜virtual、interface、override：多态分派到底怎么跑]({{< relref "engine-toolchain/cclr-12-virtual-interface-override-dispatch.md" >}})

## 五、在 Mono / CoreCLR / IL2CPP / HybridCLR / LeanCLR 里分别怎么落地

### 1. CoreCLR

CoreCLR 会把 `object` 当作整个对象模型的共同根来处理，类型身份、虚调用、对象头和 `MethodTable` 都围绕这条主线组织。

`string` 则是标准引用类型里的强特例：你在语言里看到的字面量、相等性和 intern 行为，最终都会落回对象系统和运行时约定。

想继续看深一点，去：[CoreCLR 类型系统：MethodTable、EEClass、TypeHandle]({{< relref "engine-toolchain/coreclr-type-system-methodtable-eeclass-typehandle.md" >}})

### 2. Mono

Mono 同样保留了以对象系统为中心的处理方式，但它更强调可嵌入性和多执行路径。

对你理解这篇来说，重点不是 Mono 的内部细节，而是：即便执行模型不同，`object` 作为根、`string` 作为特殊引用类型这层语义并不会变。

继续追：[Mono 架构总览：嵌入式 runtime 与 Unity 历史位置]({{< relref "engine-toolchain/mono-architecture-overview-embedded-runtime-unity.md" >}})

### 3. IL2CPP

IL2CPP 不会改变 `object` 和 `string` 的语言语义，但它会把很多运行时工作前移到构建链路里。

所以你看到的不是“另一种 string”，而是“同一套语义经过另一条执行管线”。

继续追：[IL2CPP 总管线：C# -> IL -> C++ -> Native]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})

### 4. HybridCLR

HybridCLR 建立在 IL2CPP 之上，它并不重新定义 `object` 和 `string` 的语义，而是在 AOT 边界里补出 metadata、解释执行和跨边界调用能力。

所以这篇里的重点仍然成立：语言语义没变，变化的是它们怎么被宿主 runtime 接住。

继续追：[CCLR-15｜从 AOT 到热更新：为什么 HybridCLR 要补 metadata、解释器和 bridge]({{< relref "engine-toolchain/cclr-15-why-hybridclr-needs-metadata-interpreter-and-bridge.md" >}})

### 5. LeanCLR

LeanCLR 的意义在于：如果你重新拿回 runtime 主权，还是得重新回答“共同根怎么定义、字符串这种特殊引用类型怎么安放”。

这说明 `object` 和 `string` 不是某个实现偶然长成的结果，而是整个语言—运行时桥接里必须回答的两个基础问题。

继续追：[CCLR-16｜从零到 CLR：LeanCLR 为什么选择另一条路]({{< relref "engine-toolchain/cclr-16-why-leanclr-takes-another-route.md" >}})

## 六、小结

- `object` 的关键词是共同根，不是万能容器。
- `string` 的关键词是特殊引用类型：它仍是对象，但默认按内容语义被使用。
- 先把这两个词放回正确位置，后面你看相等性、装箱、对象布局、多态分派时才不会混层。

## 系列位置

- 上一篇：[CCLR-02｜int、bool、enum、char、decimal：内建类型不是特殊语法，而是运行时约定]({{< relref "engine-toolchain/cclr-02-primitive-types-runtime-contract.md" >}})
- 下一篇：[CCLR-04｜class、struct、record：三种边界，不是三种写法]({{< relref "engine-toolchain/cclr-04-class-struct-record-boundaries.md" >}})
- 向下追深：[ECMA-335 内存模型、对象布局与 GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})
- 向旁对照：[IL2CPP 总管线：C# -> IL -> C++ -> Native]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}})

> 本文是入口页。继续提交前，请本地运行一次 `hugo`，确认 `ERROR` 为零。
