---
title: "HybridCLR 桥接篇｜IL2CPP GC 模型：BoehmGC、GC root、write barrier 与解释器的关系"
date: "2026-04-13"
description: "从 BoehmGC 的保守式扫描讲起，经过 GC root 注册、解释器栈的动态根、write barrier 的作用与缺失后果，到 HybridCLR v4.0.0 引入 Incremental GC 支持，最后把 GC 和程序集卸载绑在一起，给后续 HCLR-28 和 HCLR-30 建立必要的 GC 知识基础。"
weight: 62
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "GC"
  - "Memory"
series: "HybridCLR"
hybridclr_version: "v6.x (main branch, 2024-2025)"
---
> 大多数 Unity 工程师对 GC 的认知停在"GC.Collect() 会卡一帧"。但后面两篇文章要讨论的问题——程序集卸载为什么危险、Incremental GC 为什么需要 write barrier——都要求你理解 GC 在 IL2CPP 里到底是怎么工作的。

这是 HybridCLR 系列的桥接篇 F，位于解释器指令优化（HCLR-27）之后、程序集热重载（HCLR-28）和 Incremental GC 适配（HCLR-30）之前。

它不讲 HybridCLR 的新功能，只做一件事：

`把 IL2CPP 的 GC 模型讲到足够支撑后面两篇文章的程度。`

> 本文源码分析基于 HybridCLR 社区版 v6.x（main 分支，2024-2025）。如果你使用的版本差异较大，部分文件路径或函数签名可能有变化。

## Unity/IL2CPP 用的是 BoehmGC

Unity 的 IL2CPP runtime 没有自己实现垃圾回收器。它用的是一个叫 BoehmGC 的开源库（全称 Boehm-Demers-Weiser Garbage Collector），已经有三十多年历史。

BoehmGC 是一个 **保守式**（conservative）GC。这个"保守"不是形容词修饰，而是它最核心的技术特征。

保守式 GC 在扫描内存时，不知道某个位置上的值到底是一个整数还是一个指针。它只能做一件事：看这个值的数值是否落在已分配的堆对象的地址范围内。如果是，就假设它是一个指针，把它指向的对象标记为存活。

这意味着一个值为 `0x7FFE3C00` 的 `int` 变量，如果恰好和某个堆对象的地址重合，BoehmGC 就会把那个对象当作存活的，不回收它。这就是"保守"的含义——宁可多保留一个不该保留的对象，也不冒险回收一个可能还活着的对象。

与之对应的是 **精确式**（precise / exact）GC。.NET CoreCLR、Java HotSpot 用的都是精确式 GC。精确式 GC 在编译期或运行时生成类型布局信息（通常叫 GC map 或 stack map），明确知道每个内存位置上存的是值类型还是引用。扫描时只看引用位置，不会误判。

Unity 选择 BoehmGC 有历史原因：IL2CPP 把 C# 编译成 C++，而 C++ 编译器生成的机器码不会附带精确的栈布局信息。要做精确式 GC，需要在代码生成阶段为每个 GC 安全点插入 stack map，这对 IL2CPP 的架构来说改动太大。

保守式 GC 的代价是可能出现"假阳性"——一个已经没有真实引用的对象因为数值巧合而不被回收。在实际项目中，这种情况发生的概率很低，但它确实会在极端场景下导致内存占用偏高。

## GC root 是什么

GC 回收内存的基本算法是标记-清扫（mark-sweep）。它的工作方式可以压成三步：

1. 从一组"根"（root）出发
2. 沿着每个根的引用链，递归标记所有可达的对象
3. 扫描整个堆，回收所有没有被标记的对象

"根"就是 GC 的起点。一个对象只要能从任何一个根出发、沿引用链走到，它就是存活的。反过来，如果从所有根出发都走不到某个对象，这个对象就是垃圾。

在 IL2CPP + BoehmGC 的环境下，根有四个来源：

**静态变量。** 所有类的静态字段都是根。一个 `static List<GameObject>` 引用了一个 List 对象，那个 List 引用了里面所有 GameObject——整条链都是存活的。静态变量的生命周期和 AppDomain 一样长，所以它们永远是根。

**线程栈。** 每个线程的调用栈上的局部变量也是根。BoehmGC 在扫描时会遍历每个注册线程的栈帧，把栈上所有看起来像指针的值都当作根。这里又用到了保守扫描——它不区分 `int` 和 `object`，只看数值。

**GCHandle。** 通过 `GCHandle.Alloc()` 显式注册的对象引用。这是 native 代码持有 managed 对象引用的标准方式。IL2CPP 用 GCHandle 把 C++ 侧需要长期持有的 managed 对象钉住，防止被 GC 回收。

**动态根（dynamic root）。** BoehmGC 提供了 `GC_add_roots` / `GC_register_dynamic_root` 接口，允许运行时在任意时刻注册新的根区域。这个机制是 HybridCLR 解释器和 GC 打通的关键入口。

## 解释器的 GC root 问题

AOT 代码的 GC root 注册是在编译期静态完成的——IL2CPP 在代码生成阶段就知道哪些静态变量需要注册为根。但 HybridCLR 的解释器是在运行时动态执行的，它的栈不是 C++ 编译器生成的原生栈帧，BoehmGC 的自动栈扫描覆盖不到。

这意味着：如果解释器栈上有一个指向 managed 对象的引用，而 GC 不知道这个引用的存在，就可能把那个对象当作垃圾回收掉。解释器下一次访问这个引用时——崩溃。

HybridCLR 的解决方案是利用 BoehmGC 的动态根注册机制。

解释器的核心状态结构是 `MachineState`（在 `interpreter/InterpreterModule.h` 中定义）。每个解释器线程有一个 `MachineState` 实例，它持有一块用于模拟调用栈的内存区域 `_stackBase`。

在 `MachineState` 初始化时，它调用 `il2cpp::gc::GarbageCollector::RegisterDynamicRoot`，把 `_stackBase` 到栈顶之间的整片内存区域注册为一个 GC 动态根。这告诉 BoehmGC："这片内存区域里可能存在指向堆对象的引用，扫描时请包含它。"

但这里有一个微妙之处。解释器栈上的每一个槽位是一个 `StackObject`——它是一个 union 类型，可能存放 `int32_t`、`int64_t`、`float`、`double`，也可能存放 `Il2CppObject*`。GC 扫描这片内存时，根本不知道某个 8 字节到底是一个整数值还是一个对象指针。

好在 BoehmGC 本来就是保守式的——它处理这种情况的方式和处理原生栈一样：如果某个值看起来像一个合法的堆地址，就把它当指针。这在绝大多数情况下是安全的，代价是偶尔的假阳性（一个整数碰巧等于某个堆地址，导致那个对象多存活一轮）。

## 什么是 write barrier

前面讲的是 GC 怎么知道哪些对象活着。接下来要讲一个看起来很简单、但后果很严重的问题：GC 怎么知道引用关系发生了变化。

当代码执行一条引用赋值：

```csharp
obj.field = otherObj;
```

这条赋值改变了对象图的结构——`obj` 现在引用了 `otherObj`。如果 GC 正在进行标记，这个变化需要被 GC 感知到。

**Write barrier** 就是这个通知机制。它的实际表现形式是一段在每次引用写入时自动执行的代码，作用是告诉 GC："这个位置的引用刚刚被修改了。"

在 non-incremental GC 模式下，write barrier 不是必须的。因为 GC 是 stop-the-world 的——它暂停所有线程、一次性完成标记、然后恢复执行。在标记过程中没有任何代码在运行，所以不可能有引用被修改。

但 Incremental GC 改变了这个前提。

## 为什么 Incremental GC 需要 write barrier

Unity 从 2019.1 开始支持 Incremental GC。它的核心思路是把标记阶段拆成多个小步骤，分散到多帧执行，避免一次标记导致的长时间卡顿。

拆帧带来了一个严重的正确性问题。考虑以下时序：

```
帧 1：GC 开始标记。扫描到对象 A，标记 A 为存活。A.field 指向对象 B，标记 B 为存活。
       GC 时间片用完，暂停标记。

帧 1（业务代码继续执行）：
       业务代码执行 A.field = C;  （A 不再指向 B，改为指向 C）
       业务代码执行 B.field = null;（原来持有 C 的引用也断了）

帧 2：GC 恢复标记。继续扫描后续对象。
       C 从来没有被扫描过——因为帧 1 扫描 A 的时候，A.field 还指向 B。
       标记完成。C 没有被标记为存活。

清扫阶段：C 被当作垃圾回收。

帧 3：业务代码通过 A.field 访问 C——访问已释放内存——崩溃。
```

这就是经典的"丢失更新"（lost update）问题：GC 已经扫描过 A，认为 A 的引用关系已经处理完毕，但实际上 A 的 field 在标记完之后又被修改了。新引用的目标 C 从未被标记过，于是被错误回收。

Write barrier 的作用就是防止这种情况。每次引用写入时，write barrier 代码会把这个写入操作通知给 GC（通常是把被修改的对象或新目标对象放入一个"灰色队列"），确保 GC 在后续步骤中重新检查它。

没有 write barrier，Incremental GC 就会漏标活对象。漏标就会导致活对象被回收。活对象被回收就是悬空引用。悬空引用的结局只有一个——native crash。

## HybridCLR 解释器里的 write barrier

AOT 代码的 write barrier 是 IL2CPP 在代码生成阶段自动插入的。每一条 `stfld`（store field）指令如果目标是引用类型字段，IL2CPP 生成的 C++ 代码会在赋值后调用 `il2cpp_gc_wbarrier_set_field` 或等价的宏。

但解释器执行的代码不经过 IL2CPP 的代码生成管线。解释器需要自己在执行引用写入相关的指令时，显式调用 write barrier。

在 HybridCLR v4.0.0 之前，解释器没有 write barrier 调用。这意味着社区版在开启 Incremental GC 时存在正确性风险——解释器里的引用赋值不会通知 GC，可能导致前面描述的漏标崩溃。

从 v4.0.0 开始，HybridCLR 在 `interpreter/Instruction.h` 和相关执行路径中，为以下操作添加了 `HYBRIDCLR_SET_WRITE_BARRIER` 宏调用：

- **stfld**：对象实例字段赋值
- **stsfld**：静态字段赋值
- **stelem.ref**：数组元素赋值（引用类型）
- **cpobj**：值类型拷贝中可能包含的引用字段
- **initobj**：值类型初始化中对引用字段的清零

这个宏的内部实现最终调用到 BoehmGC 的 write barrier 接口。它的作用是：在每次通过解释器执行引用写入时，通知 GC 这个位置的引用发生了变化。

这使得社区版从 v4.0.0 起正式支持 Unity 的 Incremental GC。在此之前，使用 HybridCLR 的项目如果开启了 Incremental GC，可能会在运行一段时间后遇到随机崩溃——而且这种崩溃极难复现和定位，因为它取决于 GC 标记的时机和业务代码修改引用的时序。

## 为什么卸载程序集和 GC 有关

最后要把 GC 和另一个看似无关的问题连起来：程序集卸载。

卸载一个程序集意味着从内存中释放它的所有 metadata：`Il2CppClass`、`Il2CppMethodInfo`、类型定义表、方法定义表、虚表——全部释放。这在下一篇（HCLR-28）会详细展开。

但释放 metadata 的前提是：没有任何活对象还在引用这些 metadata。

每一个 managed 对象的内存布局里，头部就是一个指向 `Il2CppClass` 的指针。`Il2CppClass` 里面存着这个对象的类型信息、虚表、字段偏移——GC 在扫描对象时需要读取这些信息来判断对象内部哪些字段是引用。

如果在 GC 正在扫描的过程中，某个对象的 `Il2CppClass` 被释放了：

1. GC 尝试读取对象头部的类型指针——读到的是一个已释放的内存地址
2. GC 尝试通过这个地址访问类型信息——访问已释放内存
3. 结果：未定义行为，通常表现为 native crash

即使不在 GC 扫描过程中，只要还有一个活着的对象属于被卸载的程序集，它的类型指针就指向已释放的 metadata——后续任何访问这个对象的操作（包括 GC 扫描、方法调用、字段访问）都会崩溃。

所以程序集卸载必须和 GC 协调：

1. 先确保属于该程序集的所有对象都已经不可达（没有任何根能走到它们）
2. 触发一次完整的 GC，回收这些对象
3. 确认回收完成后，再释放 metadata

如果 GC 是 Incremental 的，问题更复杂：你不能在 GC 标记进行到一半的时候释放 metadata，因为 GC 可能正在用这些 metadata 来扫描对象内部的引用。必须等一个完整的 GC cycle 结束之后，才能安全释放。

这就是为什么这篇桥接文要放在 HCLR-28（热重载）和 HCLR-30（Incremental GC 适配）之前：不理解 GC root、保守扫描和 write barrier，后面两篇的核心论证就缺少根基。

## 收束

把这篇文章的几个要点收在一起：

**BoehmGC 是保守式的。** 它不区分整数和指针，靠数值范围判断。这决定了它对 HybridCLR 解释器栈的兼容方式——union 类型的 StackObject 可以直接被保守扫描覆盖。

**GC root 有四类。** 静态变量、线程栈、GCHandle、动态根。HybridCLR 通过动态根注册把解释器栈纳入 GC 扫描范围。

**Write barrier 在 non-incremental 模式下可以省略，但 Incremental GC 模式下是正确性前提。** 缺少 write barrier 会导致漏标——活对象被回收——崩溃。

**HybridCLR v4.0.0 补上了 write barrier。** 这使得社区版正式支持 Incremental GC。

**程序集卸载必须等 GC 完成。** 因为活对象的类型指针指向 metadata，释放 metadata 等于制造悬空指针。

下一篇（HCLR-28）从这里接续，展开"IL2CPP 为什么原本不能卸载程序集，热重载版到底要改什么"。

## 系列导航

上一篇（桥接篇 E）：解释器指令优化

下一篇：[HybridCLR DHE 内部机制｜dhao 文件格式、差分算法与函数级分流实现]({{< relref "engine-toolchain/hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md" >}})
