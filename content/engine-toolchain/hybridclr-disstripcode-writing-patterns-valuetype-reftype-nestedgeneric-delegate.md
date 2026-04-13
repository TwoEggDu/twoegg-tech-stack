---
title: "HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写"
date: "2026-03-31"
description: "承接共享类型判断之后，专门把 DisStripCode 这一步写成一套可执行的写法规约：什么该写、怎么写、哪些写法看起来像对了其实没保住。"
weight: 50
tags:
  - "HybridCLR"
  - "IL2CPP"
  - "AOT"
  - "DisStripCode"
  - "Generics"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> 真正让 DisStripCode 难写的，从来不是“这段代码有多长”，而是你写进去的那一行，到底有没有让 IL2CPP 看见你真正缺的那个 AOT 泛型实例。

这是 HybridCLR 系列第 21 篇。

上一篇 [HCLR-20｜HybridCLR 共享类型判断｜为什么报错是 ValueTuple<int,string>，你写的却可能是 ValueTuple<int,object>]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) 已经把“该写什么类型”这一步拆开了。

这一篇继续往下走，只回答更工程化的一步：

`当你已经知道该走 DisStripCode 时，代码到底该怎么写，才能真正把那个实例保进 AOT。`

## 这篇要回答什么

这篇主要回答 4 个问题：

1. DisStripCode 在整条修法链里到底负责什么。
2. 值类型、引用类型、嵌套泛型、委托分别怎么写。
3. 什么叫“看起来像写了，其实没保住”。
4. 项目里怎么把这类代码长期维护下去。

## 收束

DisStripCode 不是”随手写一行看起来相关的代码”。它是你显式地在 AOT 世界里造出一个足够接近目标共享实例的真实调用点，让 IL2CPP 在构建时愿意为它生成或保留那份 native 实现。

所以判断对了共享类型，只是开始。  
真正落地时，还要回答第二个问题：

`你到底该引用“类型”，还是“方法”，还是“委托签名”，还是某个 builder 路径。`

## 先把 DisStripCode 的职责钉死

在开始写模板之前，先把它的职责边界压清楚。

DisStripCode 做的是：

- 在 AOT 程序集里显式写出某组泛型实例的真实引用
- 让 IL2CPP 在构建时看见这组实例
- 从而为它生成或保留对应的 AOT native 实现

它**不**负责：

- 补充 metadata
- 解决裁剪导致的类型 / 成员不可见
- 自动推断热更里所有新出现的泛型用法

这也是为什么：

- 补 metadata 的问题要回 [HCLR-22｜AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}})
- 裁剪问题要看 `link.xml` / `[Preserve]`
- 共享类型判断要先回 [HCLR-20]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}})

## 第一类写法：泛型类型实例化

这是最常见、也最容易上手的一类。

比如你要保的是：

```csharp
List<object>.ctor
Dictionary<ValueTuple<int, object>, object>.ctor
```

那最直接的写法就是在 AOT 程序集里真实构造一次：

```csharp
[Preserve]
static void ForceAOTRefs()
{
    _ = new List<object>();
    _ = new Dictionary<(int, object), object>();
}
```

这类写法适合：

- 缺的是构造函数
- 缺的是”有一个真实对象被造出来”这条路径
- 该类型本身的共享实例已经足够覆盖目标函数

**为什么 `new List<object>()` 能覆盖 `List<HotfixType>`：** IL2CPP 的引用类型共享规则会把所有引用类型泛型参数归并到 `object`。`List<string>`、`List<MyClass>`、`List<HotfixType>` 在 native 层共享同一份 `List<object>` 实现。因此只要保住 `List<object>` 的构造路径，所有引用类型参数的构造都被覆盖。

这类写法的优点是直观。  
缺点是它只保证”你造出来的这条路”被看见，不自动保证同类所有方法都已经覆盖。

## 第二类写法：泛型方法实例化

有些时候，光把类型 new 出来不够。

比如你缺的是：

```csharp
Enumerable.ToList<object>(...)
MyGenericUtility.Show<ValueTuple<int, object>>(...)
```

这时真正要保的是**方法实例**，而不是“这个类型存在”。

更稳的写法是直接写出目标方法调用：

```csharp
[Preserve]
static void ForceAOTGenericMethods()
{
    IEnumerable<object> xs = null;
    _ = xs?.ToList();

    MyGenericUtility.Show<ValueTuple<int, object>>(default);
}
```

这类写法最值得记住的一点是：

`要保方法，就优先让 IL2CPP 真看见那次方法调用。`

**为什么 `Show<ValueTuple<int, object>>` 需要精确匹配：** 值类型泛型参数在 IL2CPP 里每个不同组合都是独立实现。`Show<ValueTuple<int, object>>` 和 `Show<ValueTuple<int, int>>` 是两份完全分开的 native 代码，不能互相覆盖。引用类型参数的方法则可以通过 `object` 共享。

不要只写一个”看起来长得像”的变量声明，然后假设构建器会替你脑补。

## 第三类写法：值类型和嵌套泛型

真正容易错的，是这一类。

比如你目标缺的是：

```csharp
Dictionary<ValueTuple<int, string>, List<HotfixType>>.TryGetValue
```

这时别急着抄原始闭包类型。  
先回到上一篇的判断：

- `K = ValueTuple<int,string>` 要先翻成共享类型
- `V = List<HotfixType>` 也要先翻成共享类型

于是写法更像：

```csharp
[Preserve]
static void ForceDictionaryAOT()
{
    var d = new Dictionary<(int, object), object>();
    d.TryGetValue((0, null), out _);
}
```

**为什么 K 保留为 `(int, object)` 而 V 直接用 `object`：** `ValueTuple` 是 struct（值类型），IL2CPP 不会把它整体归并到 `object`，必须保留外壳并逐层翻译内部参数。而 `List<HotfixType>` 是 class（引用类型），所有引用类型参数在共享规则下落到 `object`。两条规则叠加，才得到 `Dictionary<(int, object), object>` 这个最终写法。

这个例子里最重要的不是最后那行代码长什么样，而是你已经先完成了两个动作：

1. 把具体闭包类型翻成共享类型
2. 再选择一个真实调用点去触发实例化

如果这两步顺序反了，后面越写越像，最后仍然可能不是你真正缺的那条实例。

## 第四类写法：委托、接口回调、async builder

这类坑最容易出现在“表面看不见泛型”的地方。

例如：

- `Action<T>`
- `Func<T1, T2>`
- `IUniTaskSource<T>`
- `AsyncUniTaskMethodBuilder<T>`

这些东西表面上不像“容器”，但背后照样会形成 AOT 泛型实例。

委托类型如 `Action<T>` 和 `Func<T, TResult>` 遵循相同的引用类型共享规则：`Action<object>` 可以覆盖所有 `Action<HotUpdateType>` 的使用。但 `Action<int>` 必须单独保留，因为 `int` 是值类型，不会被归并到 `object`。

这类写法更稳的做法是：

- 直接写出那个 delegate / builder / interface 的具体共享实例
- 再做一个最小调用或最小赋值，确保它不是只停留在注释和声明层

例如：

```csharp
[Preserve]
static void ForceDelegateAOT()
{
    Action<object> a = _ => { };
    a(null);

    Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object> b = default;
    _ = b;
}
```

这类写法的意义不在于“运行时真的会这么执行”，而在于：

`你给 IL2CPP 造了一个足够明确、足够真实的 AOT 可见点。`

## 最容易写错的 4 种情况

### 错法一：只写注释，不写真实引用

这就是 `AOTGenericReferences.cs` 最常见的误用。

注释是需求清单，不是代码。  
IL2CPP 不会因为你在注释里写了类型名，就为它生成实现。

### 错法二：在 AOT 代码里直接引用热更类型

这在 `HCLR-18` 已经踩过一次。

AOT 程序集在编译期看不见热更程序集，因此热更引用类型通常要先落到 `object` 这类共享类型，而不是直接写热更类名。

### 错法三：只保类型，不保真正缺的那个方法

比如你缺的是：

```csharp
SomeGenericType<...>.Show<...>(...)
```

你却只写了：

```csharp
_ = new SomeGenericType<...>();
```

这不一定能保住你真正缺的那次方法实例。

### 错法四：写了共享类型，但没有保护这段辅助代码

你在 AOT 程序集里写了一段 helper，不代表它一定会被留下。

这也是为什么这类方法一般要：

- 放在 AOT 程序集中
- 标上 `[Preserve]`
- 尽量集中放在固定 helper 文件里

否则你以为“已经写了”，最后只是又给 Stripper 新增了一次删除机会。

## 项目里怎么维护这类代码

如果把 DisStripCode 当作长期资产，而不是一次性补丁，我建议至少守住 4 条规则：

1. **按功能域分 helper 文件**
   不要把所有泛型实例都堆在一个越来越长的 `DisStripCode.cs` 里。
2. **每条引用标注来源**
   写清它是因为哪个报错、哪个 `AOTGenericReferences` 条目、哪个案例补进去的。
3. **热更 DLL 变了就重新跑一轮清单**
   不要把历史 DisStripCode 当成永远完整。
4. **热路径和非热路径分开维护**
   真正热路径要优先恢复 native；非热路径未必要第一时间把实例补全。

这样后面你再看这一坨代码时，它更像“项目级 AOT 泛型配置层”，而不是一堆没人敢动的遗留补丁。

## 收束

> DisStripCode 的本质，不是”写一段看起来相关的 C#”。
> 它是有意识地在 AOT 世界里造出一个足够准确的共享实例引用，让 IL2CPP 愿意为你真正缺的那个泛型函数生成或保留 native 实现。

---

## 系列位置

- 上一篇：<a href="{{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}">HybridCLR 共享类型判断｜为什么报错是 ValueTuple&lt;int,string&gt;，你写的却可能是 ValueTuple&lt;int,object&gt;</a>
- 下一篇：<a href="{{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}">HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合</a>
- 相关前文：<a href="{{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}">HybridCLR 案例｜Dictionary&lt;ValueTuple, 热更类型&gt; 的 MissingMethodException 与 object 替代法</a>
- 基础回链：<a href="{{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}">HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么</a>
