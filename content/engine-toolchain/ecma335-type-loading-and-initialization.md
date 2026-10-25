---
title: "ECMA-335 基础｜类型加载与初始化：.cctor、beforefieldinit 与泛型静态字段"
slug: "ecma335-type-loading-and-initialization"
date: "2026-10-23"
lastmod: "2026-10-25"
description: "区分程序集加载、类型准备、CLI 类型初始化与业务初始化，通过静态构造、泛型静态字段和失败实验理解 IL2CPP、HybridCLR 的首次访问边界。"
weight: 23
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "Initialization", "Generics"]
primary_series: "ecma335"
series_order: 17
---

> **读这篇之前**：先读 [程序集身份、引用与加载]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}})。本文讨论 CLI 类型初始化，Unity 的 Awake、OnEnable 等组件回调属于另一套生命周期。

项目把热更 DLL 加载完，再通过反射枚举所有类型，接着发现“自动注册”的处理器并没有进入注册表。注册代码写在静态构造函数中，作者以为类型被发现就会执行它。

另一个常见现象恰好相反：第一次调用很慢，后面却正常。第一次访问可能同时触发类型信息准备、静态字段初始化、泛型准备、解释器转换和业务缓存构建。把它们都叫“类初始化”，会让排查失去方向。

这篇要分开四件事：**程序集加载、类型解析／准备、CLI 类型初始化，以及项目主动安排的业务初始化。**

## 同一个“初始化”，其实有四种状态

| 层次 | 解决的问题 | 不能直接推出什么 |
|---|---|---|
| 程序集加载 | 输入程序集进入运行时的可解析范围 | 不代表其中每个类型都已经使用过 |
| 类型解析与准备 | 确认类型身份，准备执行需要的布局、成员等信息 | 不代表静态构造函数已经执行 |
| CLI 类型初始化 | 按触发规则执行 .cctor，建立静态状态 | 不代表网络、资源、配置等业务依赖就绪 |
| 业务初始化 | 按项目顺序加载依赖、注册服务、预热 | 需要团队定义完成、失败和重试机制 |

这是一张职责图，不是所有 runtime 都采用的固定四步函数调用链。类型准备可以惰性进行，也可以在构建期完成一部分。

尤其不能只因为某个内部函数叫 `Class::Init`，就推断它等同于运行用户的 `.cctor`。要查当前版本的实际调用路径或观察行为。后续 [IL2CPP runtime 篇]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md" >}}) 中的类型信息准备，应与这里的语言语义分开理解。

## .ctor 与 .cctor 各自初始化什么

`.ctor` 是实例构造函数，为某次对象构造服务。`.cctor` 是类型初始化器，通常承担静态字段的初始化；它无参数、无返回值，由运行时按规则触发。

```csharp
public sealed class Registry
{
    public static readonly object Gate = new object();

    static Registry()
    {
        System.Console.WriteLine("Registry .cctor");
    }

    public Registry()
    {
        System.Console.WriteLine("Registry .ctor");
    }
}
```

两次 `new Registry()` 可以执行两次实例构造，但不会因此正常执行两次该类型的静态构造。也不要反过来推断“只有 new 对象才初始化类型”：访问静态字段、调用静态方法也可能触发。

**源代码里没写 static 构造函数，不代表 IL 中没有 .cctor。** 静态字段初始化表达式可能被编译器放入生成的 .cctor。判断触发规则，还要看类型上的 `beforefieldinit` 标志。

## beforefieldinit 放宽的是时机

[ECMA-335 第六版](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition I §8.9.5 区分了两种触发语义：

| 类型标志 | 最重要的观察边界 |
|---|---|
| 有 beforefieldinit | 初始化必须在首次访问该类型所定义的静态字段时或更早发生；单独调用静态方法不必触发 |
| 无 beforefieldinit | 首次访问其静态字段、调用其静态方法或实例构造函数会触发；值类型的实例／虚方法调用还有对应规则 |

精确规则需要结合类型类别阅读，不能概括成“所有第一次碰到类型的动作都触发”。`typeof(T)` 或发现一个 Type 对象本身，不是强制执行 T 的静态构造的契约。

C# 显式声明静态构造函数时，编译器不会给该类型添加 beforefieldinit。仅有静态字段初始化时，编译器通常采用宽松语义。参见 Microsoft 的 [CA1810 说明](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1810)。

这里“允许更早”很重要。如果某次测试在调用一个空静态方法前就打印了初始化日志，它只能证明该次运行选择了这个时机，不能证明所有运行时都必须如此。

### 初始化职责与触发窗口 {#initialization-window}

图中先把不同职责分开，再用下文的两个静态类说明 CLI 初始化时机。职责卡片之间没有调用箭头；下半部分仅画首次使用、初始化正常完成的示例，不代替上表对其他类型类别的规则。

```mermaid
%%{init: {"flowchart": {"nodeSpacing": 20, "rankSpacing": 28, "padding": 8, "subGraphTitleMargin": {"top": 8, "bottom": 20}}}}%%
flowchart TB
    subgraph Responsibilities["分别确认的职责：不表示固定加载顺序"]
        Assembly["程序集加载：输入进入可解析范围"]
        Type["类型准备：身份、布局与成员信息"]
        Business["业务初始化：配置、服务与预热完成"]
        Assembly ~~~ Type
        Type ~~~ Business
    end
    subgraph Initialization["CLI 类型初始化：建立静态状态"]
        Flag{"类型带 beforefieldinit？"}
        Flag -->|是：Relaxed| Relaxed["初始化允许更早发生<br/>单独调用 Ping 不必触发"]
        Relaxed --> Field["首次读取 Value 时<br/>必须已完成初始化"]
        Flag -->|否：Precise| Precise["本例首次调用 Ping<br/>触发类型初始化"]
        Precise --> Body["完成字段初始化与 .cctor<br/>再进入 Ping 方法体"]
    end
    Business ~~~ Flag
```

`Relaxed` 这条路径表达允许的时机范围，不规定初始化日志必须出现在 `Ping` 前还是后；CLI 静态状态已经建立，也不能推出业务初始化已经完成。

## 动手验证：比较标志和触发点

将下面代码作为普通 .NET 控制台项目的 Program.cs；放入 Unity 时，把 Console 输出换成日志，并由明确入口调用同样的步骤。

```csharp
using System;
using System.Reflection;

public static class Relaxed
{
    public static readonly int Value = Init();
    private static int Init() { Console.WriteLine("Relaxed init"); return 1; }
    public static void Ping() { Console.WriteLine("Relaxed Ping"); }
}

public static class Precise
{
    public static readonly int Value = Init();
    static Precise() { Console.WriteLine("Precise .cctor"); }
    private static int Init() { Console.WriteLine("Precise field"); return 2; }
    public static void Ping() { Console.WriteLine("Precise Ping"); }
}

public static class Program
{
    public static void Main()
    {
        Console.WriteLine((typeof(Relaxed).Attributes & TypeAttributes.BeforeFieldInit) != 0);
        Console.WriteLine((typeof(Precise).Attributes & TypeAttributes.BeforeFieldInit) != 0);
        Console.WriteLine("before calls");
        Relaxed.Ping();
        Precise.Ping();
        Console.WriteLine(Relaxed.Value);
        Console.WriteLine(Precise.Value);
    }
}
```

观察三个点：

1. C# 编译产物中，Relaxed 带 BeforeFieldInit，Precise 不带。
2. Precise 的字段初始化和静态构造体在其 Ping 方法体执行前完成。
3. Relaxed 的初始化必须赶在读取 Value 之前完成；不要断言它一定发生在 Ping 之前或之后。

本机验证：2026-10-23，.NET SDK 10.0.402，Release。标志输出为 True、False；Precise 的初始化早于其 Ping 方法体，Relaxed 在本次运行中晚于 Ping、早于 Value 读取。后面两个实验得到独立的 7／9，以及一次 Broken attempt、两次初始化异常。这是本次桌面 .NET 的观察；编辑器、IL2CPP Player 和 HybridCLR 热更代码仍需分别验证，不能把 Relaxed 的这次日志顺序当成规范要求。

## 泛型共享代码，不等于共享静态字段

下面的泛型类型为每个封闭类型保留自己的静态字段：

```csharp
public static class Cache<T>
{
    public static int Count;
    static Cache() { System.Console.WriteLine(typeof(T).Name); }
}
```

在 Main 中执行：

```csharp
Cache<int>.Count = 7;
Cache<string>.Count = 9;
System.Console.WriteLine(Cache<int>.Count);     // 7
System.Console.WriteLine(Cache<string>.Count);  // 9
```

`Cache<int>` 和 `Cache<string>` 是不同封闭类型；各自的 Count 和类型初始化状态需要区分。IL2CPP 是否让某些方法共享 native 代码，是代码生成策略，不会把它们的静态字段合成一个变量。

这也解释了为什么预热一个泛型组合，不能自动证明其他组合的静态状态已经建立。若不同字段被显式赋成同一个引用对象，当然仍可共享那个对象；“字段独立”不是“对象图完全隔离”。

## 静态构造失败不是业务重试入口

再加入：

```csharp
public static class Broken
{
    static Broken()
    {
        System.Console.WriteLine("Broken attempt");
        throw new InvalidOperationException("missing configuration");
    }
    public static void Ping() { }
}
```

在 Main 中调用两次：

```csharp
for (int i = 0; i < 2; i++)
{
    try { Broken.Ping(); }
    catch (TypeInitializationException e)
    {
        System.Console.WriteLine(e.InnerException?.GetType().Name);
    }
}
```

预期看到一次构造尝试、两次初始化失败；第二次调用并不是重新执行一遍初始化逻辑。Microsoft 的 [静态构造说明](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors)也明确指出，失败的静态构造不会被运行时重新调用。

因此网络请求、可重试下载、等待异步任务等业务动作，不适合依赖 .cctor 来调度。更稳的做法是显式的初始化入口，自己表达未开始、进行中、完成和失败状态。静态构造适合建立局部且确定的类型不变量。

还有一个边界：循环初始化、初始化中的递归访问，以及多线程互相等待，可能暴露尚未完成的静态状态或形成死锁风险。“运行时通常保证一次初始化”不是任意初始化代码都安全的证明。

## 回到热更新启动流程

把业务启动安排成显式依赖，而不是依赖扫描顺序：

```text
确认主包、补充元数据与热更 DLL 的版本匹配
  → 在需要的 AOT 泛型执行前准备相应支持
  → 加载热更程序集及其依赖
  → 主动调用热更入口
  → 按业务顺序加载配置、注册服务、预热
```

这是一种工程组织方式，不是 CLI 强制的唯一加载次序。HybridCLR 的 [补充元数据说明](https://www.hybridclr.cn/docs/basic/aotgeneric)要求在使用相关 AOT 泛型之前完成必要准备；它不意味着每次补充都会重跑已有类型的 .cctor。

加载一个同名 DLL、重启编辑器 Play Mode、关闭 Domain Reload、采用某种程序集卸载功能，也不是同一件事。是否存在新的类型身份、旧引用是否仍被持有、静态状态是否重建，取决于具体机制与版本。需要重置业务状态时，应有明确的 Reset／Shutdown 契约，不能靠“再加载一次”猜测。

**程序集可见、类型可解析、静态状态已建立、业务已经就绪，是四个需要分别确认的事实。**

## 系列位置

- 上一篇：[程序集身份、引用与加载]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}})
- 下一篇：[对象布局与 GC 契约]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
