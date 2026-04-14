---
title: "HybridCLR 共享类型判断｜为什么报错是 ValueTuple<int,string>，你写的却可能是 ValueTuple<int,object>"
date: "2026-03-31"
description: "把 AOT 泛型修法里最容易误判的一层单独拆开：日志里的具体闭包类型和 DisStripCode 里该写的共享类型为什么经常不是同一个，以及该按什么规则判断。"
weight: 49
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "Generics"
  - "DisStripCode"
  - "SharingType"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> AOT 泛型修法里最容易把人带偏的一步，不是"到底该不该补 metadata"，而是"日志里看到的类型名，到底是不是你该写进 DisStripCode 的类型"。

这是 HybridCLR 系列第 20 篇。

前一篇 [HCLR-19｜HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) 已经把入口判断收成了一张图，但那张图只解决了"该走哪条路"。

真正落到 `DisStripCode` 时，读者马上会遇到第二个更细的坑：

`日志里报的是具体闭包类型，我在 AOT 代码里是不是就该照着这个类型原样写？`

这一篇只回答这一个问题。

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 为什么日志里的具体类型和你最终该写进 `DisStripCode` 的类型，经常不是同一个。
2. 什么叫"具体闭包类型"，什么叫"共享类型"。
3. 值类型、引用类型、嵌套泛型在共享判断上为什么规则不同。
4. 遇到一个 AOT 泛型报错时，怎么把它稳定地翻译成"该写什么"。

## 收束

日志告诉你的，是当前缺口长什么样。DisStripCode 真正要你写进去的，往往是这组泛型在共享规则下该落到什么类型。

所以这一步最容易错的，不是"不知道该写"，而是：

`以为日志里写什么，你就照抄什么。`

## 先拆开两个概念：具体闭包类型 vs 共享类型

先看一个最小例子。

假设运行时出现的是：

```
MissingMethodException: AOT generic method not instantiated in aot module
    void System.ValueTuple<System.Int32,System.String>.ctor()
```

如果你只看这条日志，最自然的直觉是：

`那我去 AOT 代码里写一个 ValueTuple<int, string> 的引用不就好了？`

这条直觉不是总错，但它不稳定。

原因在于：AOT 泛型修法里，至少有两层"类型"在同时出现：

- **具体闭包类型**：也就是你在日志、调用栈、泛型签名里直接看到的那个封闭后的类型
- **共享类型**：也就是 IL2CPP / HybridCLR 在共享泛型实现时，真正拿来判断"这类实例能不能复用同一份实现"的那组类型

日志里出现的是前者，AOT 修法很多时候需要你判断的是后者。

如果这两层不先拆开，后面 `ValueTuple<int,string>`、`List<HotfixType>`、`Dictionary<K,V>`、`async builder` 这些问题一定会混。

## 为什么会有"共享类型"这一步

如果只从项目层面理解，你可以把它先压成一个足够工程化的判断：

`IL2CPP 不关心你写过多少种表面不同的泛型实例，它更关心这些实例在共享规则下到底会不会落到同一类实现。`

这背后的底层机制是：IL2CPP 的共享规则对值类型和引用类型有本质区别。所有引用类型泛型实例共享同一份实现，例如 `List<string>` 和 `List<MyClass>` 共享 `List<object>` 的 native 代码。但每个不同的值类型都会得到独立实现，例如 `List<int>` 和 `List<float>` 是两份完全分开的 native 代码。这也是为什么 DisStripCode 必须精确保留值类型实例，而引用类型位置可以用 `object` 覆盖。

于是就会出现几种看起来反直觉、但实际上非常关键的情况：

- `string` 这种引用类型，很多时候共享后会落到 `object`
- `List<string>` 这种 class 泛型类型，它的共享类型可以直接落到 `object`
- `ValueTuple<int,string>` 这种值类型泛型类型，因为外层是 struct，它不会整体变成 `object`，而是变成 `ValueTuple<int,object>`

也就是说：

`是不是值类型，不只决定"能不能共享"，还决定"共享之后到底落成什么"。`

## 第一条规则：class 引用类型经常直接落到 object

这一条是最容易理解的。

对于普通 class 引用类型：

- `string`
- `MyClass`
- `HotfixType`

它们在共享判断里，最常落到的就是 `object`。

这也是为什么前一篇 [HCLR-18｜Dictionary<ValueTuple, 热更类型> 的 MissingMethodException 与 object 替代法]({{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}) 里，值位置上的热更引用类型可以用 `object` 兜住。

这个判断成立的关键，不是"`object` 很万能"，而是：

`对于引用类型参数，共享规则本来就经常把它们归并到 object 这一类。`

所以当你看到的是：

```csharp
List<HotfixType>
Action<HotfixType>
Dictionary<int, HotfixType>
```

不要先问"这里能不能强行写 HotfixType"，而要先问：

`这个位置在共享判断里，是不是本来就该落到 object。`

## 第二条规则：值类型不能整体拿 object 覆盖

真正容易把人带偏的，是值类型。

比如：

- `int`
- `enum`
- `ValueTuple<int, int>`
- `ValueTuple<int, string>`

这些都不是 class 引用类型。

于是你不能像处理普通引用类型那样，直接说：

`我把整组东西都改成 object 就完了。`

原因很简单：

`值类型本身的布局和共享路径，不是"整个类型直接掉成 object"这么粗暴。`

最小例子就是：

- `ValueTuple<int,int>` 的共享类型仍然是 `ValueTuple<int,int>`
- `ValueTuple<int,string>` 的共享类型则是 `ValueTuple<int,object>`

这里最值得单独记住的，不是哪一条例外，而是这个判断方式：

`外层是不是值类型，决定它会不会保留外壳；内部每个泛型参数再继续按自己的共享规则往下算。`

## 第三条规则：嵌套泛型最容易误判

真正让人现场排查时崩掉的，通常不是简单的 `List<string>`。

而是这种：

```csharp
Dictionary<ValueTuple<int, string>, List<HotfixType>>
```

你如果只盯着表面类型名，很容易误判成两种极端：

- 要么觉得"全都照抄"
- 要么觉得"全都换 object"

这两种都不稳。

更稳的做法是拆层判断：

1. 先看最外层泛型函数到底是谁缺实例
2. 再看这个函数所属的泛型类型 / 泛型方法参数各自是什么
3. 再把每个参数按共享规则翻译一遍

上面这个例子里：

- `Dictionary<K,V>` 自己是 class，所以它的共享判断不能只盯着表面 `Dictionary<...>` 四个字
- `K` 是 `ValueTuple<int,string>`，因为外层是 struct，所以它要继续保留成 `ValueTuple<int,object>` 这类共享结果
- `V` 是 `List<HotfixType>`，它本身是 class，所以共享后很容易继续落到 `object`

所以你真正该做的，不是"照着报错抄一遍"，而是：

`把这组签名拆开，再逐层翻译成共享后的目标形态。`

## 给一个稳定的判断顺序

如果你已经拿到一条 AOT 泛型相关的日志，我建议用下面这个顺序判断：

### 第一步：先确认缺的是哪个具体函数

不要一看到某个泛型类型名，就急着写 AOT 引用。

先看清：

- 缺的是构造函数
- 还是实例方法
- 还是泛型方法
- 还是某个委托 / async builder 路径

因为共享判断最终落到的是**函数实例**，不是"这个类型看起来像谁"。

### 第二步：把函数所属的类型参数和方法参数分开

很多误判都出在这里。

你如果把：

```csharp
YourGenericClass<T1, T2>.Show<M1>(A1, A2)
```

里面的四层参数全部混成一句"这是个泛型"，后面就很难判断。

更稳的方式是分别问：

- class 自己的泛型参数是什么
- method 自己的泛型参数是什么
- 参数列表里又出现了哪些泛型实例

### 第三步：逐个位置做共享翻译

这一步只做一件事：

- class 引用类型，看它是不是该落到 `object`
- 值类型，看它是不是需要保留外壳、再继续翻里面的参数
- 嵌套泛型，看外层是 class 还是 struct

到这一步，你拿到的才是"该写什么"的真正输入。

## 三组最小例子

### 例子一：`List<string>.ctor`

日志如果缺的是：

```csharp
new List<string>()
```

共享判断里，`List<T>` 是 class，`string` 是引用类型。

这种情况下，真正要优先想到的不是"必须把 `List<string>` 原样写出来"，而是：

`它有没有可能直接被 object 这条共享路径覆盖。`

### 例子二：`ValueTuple<int,string>.ctor`

这就是最容易误判的例子。

`ValueTuple<T1,T2>` 外层是 struct，所以它不会整体退化成 `object`。  
但 `T2 = string` 又是引用类型，于是共享判断会继续往里算。

所以这里更稳的落点，是：

```csharp
ValueTuple<int, object>
```

而不是单纯照抄日志里的 `ValueTuple<int,string>`。

### 例子三：`Dictionary<ValueTuple<int,string>, List<HotfixType>>`

这组例子最值得学的，不是最后那行代码怎么写，而是判断方式：

- 先拆 `K`
- 再拆 `V`
- 再看最外层函数缺的是哪一个实例

只要这套拆法稳定，后面换成 `Action<ValueTuple<int,string>>`、`Func<List<HotfixType>>`、`YourGenericContainer<ValueTuple<int,string>>`，你用的还是同一套判断框架。

## 最后收成一张判断表

你可以先把这一篇压成下面 4 句话：

- 日志里看到的是具体闭包类型，不一定是最终该写进 AOT 代码的共享类型
- class 引用类型经常落到 `object`
- 值类型不能整体拿 `object` 覆盖，要看它自己是不是还保留外壳
- 嵌套泛型不要一把抹平，必须逐层翻译

如果这 4 句话没先立住，后面谈 `DisStripCode` 写法，很容易一开始就写错输入。

## 收束

> AOT 泛型修法里，最危险的不是你不会写 `DisStripCode`，而是你以为日志报出来的具体类型，就是你最终该写进去的类型。
> 真正要先判断的，是这组泛型在共享规则下该落到什么共享类型。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}">HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}">HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}">HybridCLR 案例｜Dictionary&lt;ValueTuple, 热更类型&gt; 的 MissingMethodException 与 object 替代法</a>
- 基础回链：<a href="{{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}">HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑</a>
