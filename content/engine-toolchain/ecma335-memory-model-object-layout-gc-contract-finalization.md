---
title: "ECMA-335 基础｜对象布局、GC 契约与终结语义"
slug: "ecma335-memory-model-object-layout-gc-contract-finalization"
date: "2026-04-14"
lastmod: "2026-10-23"
description: "从值存储、对象引用和可达性出发，理解 IL2CPP 与 HybridCLR 必须共同维护的布局、GC 根、写屏障与资源释放边界。"
weight: 15
featured: false
tags: ["ECMA-335", "IL2CPP", "HybridCLR", "GC"]
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
primary_series: "ecma335"
series_order: 18
---

一段热更代码把对象放进容器，稍后 AOT 代码继续使用它。无论其中有多少次解释器与 native 调用切换，这个对象都必须保持正确的类型、引用关系和生命周期。

这篇关心两套执行路径共同维护的契约：**对象怎样存储，引用怎样保持可见，资源什么时候应当释放。** 分代算法、CoreCLR 对象头和 Mono SGen 的完整实现留给各自专题。

## 布局是存储规则，不是统一的 C++ 结构

CLI 区分 auto、sequential、explicit 等布局控制方式：

| 方式 | 关注点 |
|---|---|
| AutoLayout | 运行时安排布局，不应依赖固定字段偏移 |
| SequentialLayout | 按布局规则处理字段顺序、对齐和 packing |
| ExplicitLayout | 通过显式偏移表达布局，仍须满足有效性与引用安全约束 |

这些规则不意味着所有平台上 sizeof 都相同，也不意味着可以直接把托管对象指针交给 native。字段的托管表示、marshaling 后的原生表示和平台 ABI 需要分别检查。

规范没有要求每个对象必须拥有某个固定大小的对象头。IL2CPP 的类型指针、同步信息和数组长度等实现细节，应绑定具体平台与版本；不要把 64 位平台上的某个数值写成 CLI 常量。

## 内嵌的值与装箱对象

```csharp
public struct Point { public int X, Y; }
public sealed class Unit { public Point Position; }
```

未装箱的 Point 可以内嵌在 Unit 中，也可作为 Point[] 的元素。它不必拥有单独的对象身份。把 Point 赋给 object 则会发生装箱，形成可通过对象引用访问的表示。

若 Point 含有引用字段，GC 仍需正确处理这些字段。**值类型并不等于“不含引用”或“可以无条件 memcpy”。** 普通数值结构和含引用结构跨边界传递的要求不能混同。

## GC 的核心问题是可达性

GC 要保留仍可从根集合到达的对象。常见根来源包括静态状态、执行中仍活跃的引用，以及显式句柄等；具体如何发现和表示它们由运行时决定。

```text
活跃根 → Unit → Inventory → Item
```

这条链上的 Item 即使没有单独的局部变量引用，也可能仍然存活。反过来，一个局部变量的源码作用域还没有结束，并不能单独保证它一直作为 GC 根存活；优化后的活跃区间需要结合运行时语义理解。

对象变得不可达，也不等于立刻回收。GC 的调度、保守扫描造成的额外保留、终结处理等都可能影响实际时机。因此“清空引用后内存没有马上下降”不能直接作为泄漏证据。

### 动手验证：静态容器怎样留下一个对象

下面用普通托管对象观察引用链。把代码保存到 .NET 控制台项目的 Program.cs，以 Release 配置独立运行，不附加调试器：

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class Program
{
    private static readonly List<object> Roots = new List<object>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndRetain()
    {
        object item = new object();
        Roots.Add(item);
        return new WeakReference(item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CollectAndRead(WeakReference weak)
    {
        GC.Collect();
        return weak.IsAlive;
    }

    public static void Main()
    {
        WeakReference weak = CreateAndRetain();
        Console.WriteLine($"retained: {CollectAndRead(weak)}");
        Roots.Clear();
        Console.WriteLine($"after clear: {CollectAndRead(weak)}");
    }
}
```

创建对象的 helper 返回后，局部变量 item 已离开该调用；对象仍可沿“静态字段 Roots → List 的元素 → item”到达。WeakReference 自身不把目标变成强引用，第一次收集后仍存活的原因是容器。

Clear 移除容器元素中的强引用后，目标才有机会被回收；List 对象本身和它的容量可以继续保留。预期在这份桌面示例中看到 `retained: True`、`after clear: False`，但不能把第二行推广成所有运行时在任何环境中调用一次 GC.Collect 就必定得到的结果。

本机验证：2026-10-23，Windows、.NET SDK 10.0.402、Release、独立进程无调试器，直接从本文提取代码，输出依次为 `retained: True`、`after clear: False`。这是桌面 .NET 的观察，未代替 IL2CPP／HybridCLR Player 验证。

NoInlining 用于减少桌面实验中内联和局部变量活跃期对观察的干扰，不是跨运行时的生命周期保证。本例无终结器，也不测进程内存是否归还给操作系统。改成 Unity／HybridCLR 实验时，应分别记录后端、架构和调试器状态；保守扫描或额外根可能继续保留目标，需要检查实际引用链。

这个模型对应一个常见热更问题：AOT 静态注册表留下旧热更对象或委托。先解除注册、移除容器引用，再检查对象和 metadata 的各自生命周期；“业务已经不用它”并不等于 GC 已经看不到它。强制 GC 仅用于此处实验，不是线上清理策略。

## 解释执行必须加入同一套引用管理

AOT 代码、解释器帧和 native 互操作可能同时持有托管引用。解释器不能只保存一个看起来像地址的值，就假定 GC 一定能看到它。

一个运行时可以使用栈扫描、根区注册、句柄或其他集成方式。具体 HybridCLR 版本如何组织帧和根，应看实现专题；ECMA 前置层需要记住的是：**引用从哪条执行路径产生，不影响它必须满足 GC 可达性契约。**

继续阅读 [IL2CPP／HybridCLR 的 BoehmGC、根与写屏障]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})。不要把某种 GC 根注册方案写成所有解释器必须采用的数据结构。

## 写屏障与线程屏障不是一回事

当对象引用发生修改，某些 GC 方案需要记录这次变化，避免漏掉新的可达关系。分代与增量收集是常见需求来源，但记录方式和触发条件属于实现。

解释器执行引用字段或数组元素写入时，也必须遵守宿主 GC 的写入协议，不能仅完成内存赋值。

这里的 **GC write barrier** 不等同于并发内存模型的 **memory barrier**。前者服务于收集器的引用追踪，后者约束跨线程内存操作的可见性与排序。遇到 volatile、Interlocked 等问题，应转到 [线程内存模型分支]({{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}})。

## 托管引用跨到 native 之后

native 保存托管对象地址时，需要分别回答：

- 什么保证对象仍然存活？
- 什么保证本次传递期间地址满足要求？
- native 保存的是临时指针，还是需要跨调用持有的句柄？
- 谁负责释放句柄或原生资源？

“当前 GC 不移动对象”不能替代生命周期管理，也不保证换版本或换运行时后仍成立。普通 GCHandle 用于保持引用，固定地址的要求还要区分 pinned 等具体机制；可用类型和 marshaling 规则继续看 [原生互操作分支]({{< relref "engine-toolchain/ecma335-pinvoke-native-interop-marshaling-spec.md" >}})。

## Finalizer 与 Dispose 解决不同问题

C# 的终结器最终关联到运行时的终结处理。它可以作为某些非托管资源的兜底释放机制，但不应承担“这个业务动作必须及时发生”的职责。

需要保留的边界是：

1. 终结执行的及时性和相互顺序不应作为业务依赖。
2. 不要假定进程退出时所有终结器都会执行；例如现代 .NET 不把退出时执行终结器作为承诺。
3. 终结器可能影响对象回收过程，但“所有 runtime 固定经过两次 GC”不是统一契约。
4. 对象复活会进一步复杂化生命周期，不适合用来实现普通缓存或业务重试。

需要明确释放时点的资源使用 IDisposable／using 等机制；包装系统句柄时优先采用适当的 SafeHandle 模式。Dispose 是程序主动调用的协议，不是调用它就让 GC 立刻回收托管对象。

Microsoft 的 [终结器说明](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/finalizers)区分了确定性释放与终结行为。UnityEngine.Object 的 Destroy 又是引擎对象生命周期的一层，不能当作普通托管 GC 或 Dispose 的同义词。

## 工程观察：把三个证据分开

| 想确认什么 | 应观察什么 |
|---|---|
| 托管对象是否仍被引用 | 根与引用链、容器或静态缓存 |
| GC 是否完成相关回收 | 目标 runtime 的 GC 事件与内存快照 |
| native 资源是否释放 | 资源自身的句柄、计数或释放日志 |

热更重载后内存增长，可能来自旧引用仍存活、解释器或 metadata 缓存、native 资源未释放等不同来源。只观察进程内存总量，不足以区分这些原因。

规范入口：[ECMA-335](https://ecma-international.org/publications-and-standards/standards/ecma-335/) Partition I 的存储与内存管理语义；算法与布局实现继续看 [IL2CPP GC 专题]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})。

**AOT 与解释执行共享对象世界，也必须共享正确的引用管理；及时释放业务资源仍然需要显式生命周期。**

## 系列位置

- 上一篇：[类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})
- 下一篇：[反射与特性]({{< relref "engine-toolchain/ecma335-custom-attributes-reflection-encoding.md" >}})
- 返回：[ECMA-335 主线索引]({{< relref "engine-toolchain/ecma335-series-index.md" >}})
