---
title: "横切对比｜GC 实现：分代精确 vs 保守式 vs 协作式 vs stub"
slug: "runtime-cross-gc-implementation-generational-conservative-cooperative"
date: "2026-04-14"
description: "同一份 ECMA-335 GC 契约，在 CoreCLR、Mono、IL2CPP、HybridCLR、LeanCLR 五个 runtime 里走出五种完全不同的垃圾回收实现：分代精确式、轻量精确式、保守式非分代、保守式 + 动态根适配、stub 接口。从 GC 类型、分代策略、精确性、write barrier、finalization、根扫描到暂停模型，逐维度横切对比五种 GC 实现的设计 trade-off。"
weight: 83
featured: false
tags:
  - "ECMA-335"
  - "CoreCLR"
  - "IL2CPP"
  - "LeanCLR"
  - "Mono"
  - "GC"
  - "Comparison"
series: "dotnet-runtime-ecosystem"
series_id: "runtime-cross"
---

> ECMA-335 对 GC 的规定极其克制：不可达对象可能被回收，Finalizer 可能被调用，不保证时机，不保证顺序。五个 runtime 在这份契约下给出了五种截然不同的实现——没有"最好的 GC"，只有在不同约束下各自最优的 GC。

这是 .NET Runtime 生态全景系列的横切对比篇第 4 篇。

CROSS-G3 对比了方法执行——五个 runtime 怎么让同一段 CIL 字节码跑起来。但方法执行过程中分配的对象怎么管理、什么时候回收、回收时怎么找到所有存活对象——这些问题的答案在五个 runtime 里差异巨大。这篇把五种 GC 实现并排摊开。

![5 种 GC 策略对比](../../images/runtime-ecosystem/cross-gc-strategies.svg)

*图：精确度从高到低：CoreCLR/SGen > LeanCLR(设计) > BoehmGC。*

## ECMA-335 的 GC 契约：只定义语义，不定义算法

ECMA-335 Partition I 8.4 对自动内存管理的规定可以压成三条：

**第一条：托管对象在托管堆上分配，生命周期由运行时管理。** 开发者调用 `new` 分配对象，但不需要（也不能）手动释放。运行时负责在"适当的时候"回收不可达的对象。

**第二条：Finalizer 可能被调用，但不保证时机和顺序。** 如果一个类型定义了 `Finalize()` 方法，运行时在回收该对象之前"可能"调用它。但规范明确说了——不保证什么时候调用，不保证多个 Finalizer 之间的执行顺序，甚至不保证一定会调用（进程退出时可能来不及）。

**第三条：规范不约束 GC 算法。** 分代还是非分代、mark-sweep 还是 copying collector、引用计数还是 tracing、精确式还是保守式——规范不关心。只要满足前两条语义，runtime 用什么算法都行。

这份极简契约给了每个 runtime 完全的实现自由。五个 runtime 各自走出了五条完全不同的路线。

## CoreCLR GC — 分代精确式

CoreCLR 的 GC 是五个 runtime 中最复杂、优化最激进的。它的核心设计可以用四个关键词概括：分代、精确、并发、可切换。

### 分代策略：3 代 + LOH + POH

CoreCLR GC 把托管堆分成五个区域：

**Gen 0（第 0 代）。** 新分配的对象默认进入 Gen 0。Gen 0 的大小通常在几百 KB 到几 MB 之间，由 GC 根据分配速率动态调整。Gen 0 回收最频繁，也最快——大部分短命对象在 Gen 0 就被回收了。

**Gen 1（第 1 代）。** 从 Gen 0 存活下来的对象被提升到 Gen 1。Gen 1 是 Gen 0 和 Gen 2 之间的缓冲区，回收频率低于 Gen 0。

**Gen 2（第 2 代）。** 从 Gen 1 存活下来的对象被提升到 Gen 2。Gen 2 的回收（full GC）代价最高——需要扫描整个堆。但 Gen 2 的回收频率也最低。

**LOH（Large Object Heap）。** 大于 85000 字节的对象直接进入 LOH，不走分代提升。LOH 使用 mark-sweep 而不是 copying——移动大对象的成本太高。LOH 在 Gen 2 回收时一起处理。

**POH（Pinned Object Heap）。** .NET 5 引入。被 pin 的对象（通过 `fixed` 语句或 `GCHandle.Alloc(GCHandleType.Pinned)`）进入 POH。把 pinned 对象集中到专用堆，避免它们在普通堆中造成碎片化。

分代的核心假设是"代际假说"（generational hypothesis）：大部分对象生命周期很短，年轻代的回收效率远高于扫描整个堆。这个假设在服务端 .NET 应用中通常成立——每次 HTTP 请求产生大量临时对象，请求结束后这些对象就不可达了。

### 精确式栈扫描

CoreCLR GC 是精确式的——它知道内存中每个位置存的是值类型还是引用类型。

精确性来自两个层面：

**堆对象层面。** 每个 MethodTable 关联一个 GC descriptor，编码了实例字段中哪些偏移位置是引用类型。GC 扫描一个堆对象时，只检查 GC descriptor 标记的位置，不会把一个 `int` 字段误判为指针。

**栈帧层面。** RyuJIT 在编译每个方法时生成 GC info（也叫 stack map），精确记录了每个 GC 安全点上，每个栈槽和寄存器里存的是不是引用。GC 触发时，遍历所有线程的调用栈，在每个栈帧的 GC info 指导下精确地识别出所有引用。

精确式的好处是零误判——不会因为一个整数值恰好等于某个堆地址而误保留对象。代价是 JIT 编译器必须在每个 GC 安全点维护完整的引用位置信息，这增加了编译器的复杂度和生成代码的元数据体积。

### Card table write barrier

分代 GC 有一个核心问题：回收 Gen 0 时，不想扫描整个 Gen 2（太慢），但 Gen 2 对象可能持有 Gen 0 对象的引用。如果漏掉这些跨代引用，就会错误回收仍然存活的 Gen 0 对象。

CoreCLR 用 card table 解决这个问题。card table 把整个堆地址空间划分成固定大小的"卡片"（每卡通常 256 字节或 512 字节）。每当代码修改一个引用类型字段时，write barrier 被触发，把目标地址对应的卡片标记为"脏"。

```
// 伪代码：write barrier 的核心逻辑
void write_ref(Object** dest, Object* ref) {
    *dest = ref;                               // 实际写入
    card_table[((size_t)dest) >> card_shift] = 0xFF;  // 标记脏卡
}
```

Gen 0 回收时，GC 只需要扫描脏卡覆盖的区域，而不是整个老年代。这把跨代引用的追踪成本从 O(老年代大小) 降低到 O(脏卡数量)。

write barrier 的代价是每次引用赋值多了几条指令。RyuJIT 会在所有引用类型字段赋值点内联 write barrier 代码。这个开销在实际应用中通常占总执行时间的 1-3%，对于分代 GC 带来的回收效率提升来说是值得的。

### Workstation / Server 模式

CoreCLR GC 提供两种运行模式：

**Workstation GC。** 单线程 GC，适用于客户端应用。GC 线程和应用线程共享 CPU。暂停时间相对可控，但吞吐量不是最优。

**Server GC。** 每个逻辑 CPU 核心一个 GC 线程，每个 GC 线程管理一个独立的堆段。适用于服务端高吞吐场景。GC 并行执行，暂停时间不一定比 Workstation 短，但单位时间回收能力更强。

两种模式还可以分别启用并发/后台 GC——Gen 0 / Gen 1 回收时暂停应用线程（STW，Stop The World），Gen 2 回收在后台线程进行，只在标记阶段的特定点做短暂暂停。

## Mono SGen — 精确式分代

Mono 的 GC 叫 SGen（Simple Generational GC）。它和 CoreCLR GC 的核心设计思路相似——精确式分代——但实现上更轻量。

### 两代结构：nursery + major

SGen 只有两代，不像 CoreCLR 有三代加两个特殊堆：

**Nursery（幼儿园）。** 对应 CoreCLR 的 Gen 0。新对象在 nursery 分配。nursery 使用 copying collector——把存活对象复制到 major heap，然后整个 nursery 空间清零重用。copying 的好处是分配只需要移动一个指针（bump allocator），速度极快。

**Major heap。** 对应 CoreCLR 的 Gen 1 + Gen 2。从 nursery 存活下来的对象被复制到 major heap。major heap 使用 mark-sweep 或 mark-compact 策略（可配置）。

大对象直接进入 major heap 的 LOH 区域，类似 CoreCLR 的 LOH 设计。

### 精确性

SGen 也是精确式 GC。Mono 在类型加载时为每个类型生成 GC descriptor，标记哪些字段偏移是引用。栈扫描时，Mono 的 JIT（Mini）同样生成 GC map 来标记每个安全点的引用位置。

在 AOT + Interpreter 模式下，解释器栈的根注册方式和 JIT 模式不同——解释器需要显式向 SGen 注册栈区域。这点和 HybridCLR 在 BoehmGC 上做的动态根注册类似。

### 与 CoreCLR GC 的差异

SGen 比 CoreCLR GC 更简单、更小：

- 没有 Server GC 模式——SGen 总是单线程 GC
- 没有 POH——pinned 对象在原地标记，由碎片整理阶段处理
- 两代而非三代——减少了提升逻辑的复杂度
- nursery 用 copying collector——比 CoreCLR Gen 0 的 mark-compact 分配更快，但存活对象多时复制成本高

SGen 的设计目标是"足够好"的 GC 配合可嵌入的体积。Mono runtime 需要嵌入到 Unity、Xamarin 等宿主中，GC 的复杂度和体积直接影响集成成本。

## IL2CPP BoehmGC — 保守式非分代

IL2CPP 使用的 GC 完全不同于前两者。它用的是 BoehmGC（Boehm-Demers-Weiser Garbage Collector），一个有三十多年历史的开源保守式 GC。

### 保守式扫描

BoehmGC 是保守式（conservative）GC——扫描内存时，它不知道某个位置存的到底是整数还是指针。它只能看这个值是否落在已分配堆对象的地址范围内，如果是就假设它是指针，把目标对象标记为存活。

```
假设栈上有一个 int 变量值为 0x7FFE3C00
如果这个值恰好等于某个堆对象的起始地址
→ BoehmGC 会把那个对象标记为存活（false positive）
→ 该对象不会被回收，即使没有真实引用指向它
```

这就是"false positive"问题——整数值碰巧等于堆地址时，对象被错误保留。实际项目中这种情况发生的概率很低（地址空间大、堆对象起始地址分布稀疏），但它确实会在极端场景下导致内存占用偏高。

### 为什么 IL2CPP 选了 BoehmGC

选择保守式 GC 不是技术偏好，而是架构约束的直接推论。

IL2CPP 把 C# 编译成 C++，再由平台 C++ 编译器（clang / MSVC / GCC）编译成 native code。C++ 编译器生成的机器码不会附带精确的栈布局信息——编译器可以自由地把变量放在寄存器、栈上、甚至优化掉，不会为 GC 生成 stack map。

要做精确式 GC，需要两个前提：第一，代码生成器（JIT 或 AOT）在每个 GC 安全点记录引用的位置；第二，GC 能读取这些记录。CoreCLR 的 RyuJIT 满足第一个条件，Mono 的 Mini JIT 也满足。但 IL2CPP 的代码生成器是 il2cpp.exe → C++ 源代码 → C++ 编译器，最后一步完全交给了第三方编译器，无法控制栈布局。

在这个架构下，保守式 GC 是唯一实际可行的选择。BoehmGC 成熟稳定、跨平台支持好、集成简单——它只需要知道堆的地址范围和线程栈的位置，不需要任何代码生成器的配合。

### 非分代 mark-sweep

BoehmGC 默认是非分代的 mark-sweep GC。每次回收都扫描整个堆：

1. **Mark 阶段**：从所有根（静态变量、线程栈、GCHandle）出发，保守地扫描内存，标记所有可达对象
2. **Sweep 阶段**：遍历整个堆，回收未标记的对象

没有分代意味着每次 GC 的成本和堆大小成正比。堆越大，暂停时间越长。这就是 Unity 项目中 GC.Collect() 会造成明显帧率卡顿的根源——BoehmGC 在标记阶段必须 STW（Stop The World），暂停所有托管线程。

BoehmGC 支持增量模式（incremental mode），把标记阶段拆分成多个小步骤穿插在应用执行中。Unity 2019+ 引入了 Incremental GC 选项，利用的就是 BoehmGC 的增量标记能力。但增量模式依赖 write barrier 来追踪标记阶段中引用关系的变化——这和 HybridCLR 的 write barrier 适配直接相关。

### Finalization

BoehmGC 支持 finalization。当一个带有 Finalizer 的对象变得不可达时，BoehmGC 不会立即回收它，而是把它放入 finalization 队列。IL2CPP 有一个专用的 finalizer 线程，从队列中取出对象并调用其 `Finalize()` 方法。对象在 Finalizer 执行完成后的下一次 GC 中才会被真正回收——这意味着 finalizable 对象至少存活两轮 GC。

## HybridCLR — 复用 BoehmGC + write barrier 适配

HybridCLR 运行在 IL2CPP 内部，自然复用 IL2CPP 的 BoehmGC。但解释器引入了两个 GC 层面的新问题。

### 问题一：解释器栈的根注册

AOT 代码的局部变量在 C++ 编译器生成的栈帧上，BoehmGC 的自动栈扫描能覆盖到。但 HybridCLR 的解释器有自己的模拟栈——`MachineState` 的 `_stackBase` 区域。这片内存不在 C++ 原生栈上，BoehmGC 看不到。

解决方案是动态根注册。`MachineState` 初始化时调用 `GarbageCollector::RegisterDynamicRoot`，把解释器栈区域注册为 GC 根。GC 扫描时会遍历这片区域，保守地把看起来像堆地址的值当作引用。

由于 BoehmGC 本身就是保守式的，解释器栈上的 `StackObject` union（可能是 `int32_t`、`float`、也可能是 `Il2CppObject*`）不需要额外的精确标注——保守扫描天然兼容这种类型不确定的场景。

### 问题二：write barrier 适配（v4.0.0+）

HybridCLR v4.0.0 开始支持 Incremental GC。增量 GC 在标记阶段分步进行，应用线程在标记间隙继续执行。如果这期间发生了引用赋值（老对象的字段指向了一个新分配的对象），标记阶段可能漏掉这个新引用，导致存活对象被错误回收。

write barrier 就是为了解决这个问题——每次引用赋值时通知 GC"这里有引用变化"。AOT 代码的 write barrier 由 IL2CPP 在代码生成时自动插入。但解释器执行的引用赋值不经过 IL2CPP 的代码生成路径。

HybridCLR 的做法是在解释器的引用赋值指令（`stfld`、`stelem.ref`、`stind.ref` 等对应的 HiOpcode）中手动调用 write barrier：

```
执行引用赋值 HiOpcode
  → 完成实际写入
    → 调用 il2cpp::gc::WriteBarrier::GenericStore
      → BoehmGC 记录引用变化
```

这保证了解释器执行的引用赋值对 Incremental GC 可见，不会导致存活对象被误回收。

## LeanCLR — stub 接口，设计目标精确协作式

LeanCLR 当前的 GC 实现是一个 stub——`malloc` 分配，不回收。但它的架构已经定义了完整的 GC 接口和设计意图。

### 当前实现：malloc-only

LeanCLR Universal 版的 `GarbageCollector` 类只做一件事：通过 `GeneralAllocation::alloc` 分配内存（底层是系统 `malloc`），不做回收。对象一旦分配，除非整个进程退出，否则不会被释放。

```
newobj 指令
  → Interpreter::execute
    → GarbageCollector::alloc(instance_size)
      → malloc(instance_size)
        → 返回对象指针
```

这种设计在 H5 小游戏和短生命周期的嵌入场景下是可行的——场景切换时整个 runtime 实例销毁，所有内存由操作系统回收。600KB 的 runtime 体积中不包含任何 GC 算法代码。

### write_barrier 接口已定义

尽管 GC 是 stub，LeanCLR 已经在代码中定义了 `write_barrier` 相关的接口预留。这表明设计目标不是永远停留在 malloc-only，而是为未来的精确协作式 GC 做架构准备。

### 设计目标：精确协作式

根据 LeanCLR 的架构设计，计划中的 GC 是精确协作式（precise cooperative）：

**精确。** 因为 LeanCLR 是纯解释器执行，运行时对每个栈帧的内容有完全的控制——知道哪个 `StackObject` 存的是引用、哪个是值类型。这和 CoreCLR 的精确性来源不同（CoreCLR 靠 JIT 生成 stack map），但效果一样：GC 能精确识别引用位置，不需要保守扫描。

**协作式（cooperative）。** GC 不强制暂停线程，而是在安全点（safe point）等待线程主动"协作"暂停。解释器的 dispatch loop 天然提供了细粒度的安全点——每执行 N 条指令就可以检查一次 GC 请求。这比 JIT 代码的安全点（只在方法调用、循环回边等位置）更密集，GC 响应延迟更低。

这个设计意图和 BoehmGC 的保守式形成鲜明对比：LeanCLR 的解释器架构天然支持精确 GC（因为 runtime 完全控制栈布局），而 IL2CPP 的 C++ codegen 架构天然只能用保守式（因为栈布局由 C++ 编译器控制）。

## 五方对比表

| 维度 | CoreCLR | Mono SGen | IL2CPP BoehmGC | HybridCLR | LeanCLR |
|------|---------|-----------|----------------|-----------|---------|
| GC 类型 | Tracing, mark-compact / mark-sweep | Tracing, copying + mark-sweep | Tracing, mark-sweep | 复用 BoehmGC | Stub (malloc-only) |
| 分代数 | 3 代 + LOH + POH | 2 代 (nursery + major) + LOH | 非分代 | 非分代（同 BoehmGC） | 无 |
| 精确性 | 精确式（JIT 生成 stack map） | 精确式（JIT 生成 GC map） | 保守式 | 保守式（同 BoehmGC） | 设计目标精确式 |
| Write barrier | Card table, JIT 内联 | 有, nursery → major 追踪 | 增量模式下有 | v4.0.0+ 解释器手动调用 | 接口已定义, 未实现 |
| Finalization | 专用 finalizer 线程 | 有 | 有, 专用线程 | 同 IL2CPP | 未实现 |
| 根扫描 | 精确栈扫描 + 静态变量 + GCHandle | 精确栈扫描 | 保守栈扫描 + 静态变量 + GCHandle | 同 BoehmGC + MachineState 动态根 | 无 GC 无需扫描 |
| 暂停模型 | STW (Gen 0/1) + 后台并发 (Gen 2) | STW | STW (非增量) / 增量分步 | 同 BoehmGC | 设计目标协作式 |
| 并行回收 | Server GC 多线程并行 | 单线程 | 单线程 | 单线程 | 无 |
| 大对象处理 | LOH (>85KB) + POH | LOH | 无特殊处理 | 同 BoehmGC | 无 |
| **源码锚点** | `src/coreclr/gc/gc.cpp` | `mono/sgen/sgen-gc.c` | `il2cpp/gc/GarbageCollector.cpp` | `hybridclr/interpreter/Engine.h` (MachineState) | `src/runtime/gc/garbage_collector.h` |

## 为什么没有"最好的 GC"

五种实现的差异根源不在技术偏好，而在各自面对的约束集合。

### 精确性的约束来源

CoreCLR 和 Mono 能做精确式 GC，因为它们控制代码生成过程——JIT 编译器在生成 native code 的同时生成 stack map。IL2CPP 做不到精确式，因为最后一步 native code 生成交给了 C++ 编译器，无法在每个 GC 安全点插入精确的引用位置信息。LeanCLR 计划做精确式，因为纯解释器天然拥有对栈布局的完全控制。

精确性不是一个独立的技术选项，而是执行策略（JIT / AOT / Interpreter）的直接推论。

### 分代策略的约束来源

分代 GC 依赖两个前提：第一，代际假说成立（大部分对象短命）；第二，有 write barrier 来追踪跨代引用。

CoreCLR 面向服务端应用，每个请求产生大量临时对象，代际假说高度成立。投入 write barrier 的开销换取年轻代高频快速回收，收益巨大。

BoehmGC 是非分代的。要在保守式 GC 上做分代，write barrier 的实现更复杂——保守扫描本身就不知道哪些赋值是引用类型赋值，无法精确触发 barrier。BoehmGC 选择简单的全堆 mark-sweep，代价是每次 GC 成本和堆大小成正比。

Mono SGen 的两代设计是一个中间路线——比 CoreCLR 简单，比 BoehmGC 高效。nursery 用 copying collector 实现高速分配，major heap 用 mark-sweep 处理长命对象。对于嵌入式 CLR 的典型负载来说，这个平衡点是合适的。

### 暂停模型的约束来源

GC 暂停对不同类型的应用影响不同。

服务端应用（CoreCLR 的主要场景）对 P99 延迟敏感——一次 200ms 的 GC 暂停可能导致请求超时。所以 CoreCLR 投入大量复杂度来做后台/并发 GC，把 Gen 2 回收的暂停时间降到最低。

游戏应用（IL2CPP / HybridCLR 的主要场景）对帧率一致性敏感——一次 50ms 的 GC 暂停就意味着 3 帧卡顿。Unity 引入 Incremental GC 把标记阶段打散到多帧中，每帧只做一小部分标记工作，代价是 write barrier 开销和更长的整体回收周期。

LeanCLR 的协作式设计目标对暂停最友好——解释器的 dispatch loop 提供了极细粒度的安全点，GC 可以在任意两条指令之间请求协作暂停，理论上暂停延迟可以做到微秒级。但这个优势只有在解释器执行的前提下才成立——native code 的安全点密度远不如解释器。

### False positive 的实际影响

保守式 GC 的 false positive 在理论上听起来很危险，但在实际项目中影响有限。堆地址空间通常是 64 位的，一个 32 位或 64 位整数值恰好等于某个堆对象起始地址的概率非常低。实际项目中因为 false positive 导致可观测的内存增长，几乎只出现在特定的压力测试场景中。

更实际的影响是：保守式 GC 无法移动对象（compacting），因为它不确定某个"看起来像指针"的值到底是不是指针——如果移动了对象并更新了真正的指针，那些碰巧等于旧地址的整数值就会变成野指针。这意味着保守式 GC 不能做 copying collector 或 compacting collector，堆碎片化只能通过 free list 管理，长期运行后碎片化程度可能比精确式 GC 更严重。

## 收束

同一份 ECMA-335 GC 契约，五个 runtime 给出了五种实现。差异的根源是各自的执行策略和目标场景不同：

- CoreCLR 有 JIT，能生成精确的 stack map，所以做了精确式分代 GC，配合并发回收优化服务端延迟
- Mono SGen 同样有 JIT 的精确性基础，但用两代 + 更简单的设计匹配嵌入式 CLR 的体积约束
- IL2CPP 的 C++ codegen 无法提供精确栈布局，只能用保守式 BoehmGC，非分代 mark-sweep 是最简单可靠的选择
- HybridCLR 在 BoehmGC 上补了动态根注册和 write barrier 适配，让解释器栈和 Incremental GC 正确协作
- LeanCLR 当前用 stub GC 把体积压到最小，但解释器架构为未来的精确协作式 GC 提供了天然基础

GC 的选择不是一个独立的技术决策。执行策略决定了精确性的可行性，目标场景决定了分代和暂停模型的取舍，体积预算决定了 GC 算法的复杂度上限。理解了这些约束链条，就不会问"IL2CPP 为什么不用 CoreCLR 的 GC"这种脱离上下文的问题。

## 系列位置

这是横切对比篇第 4 篇（CROSS-G4），Phase 2 横切对比的第一篇。

上一篇 CROSS-G3 对比了方法执行策略——五个 runtime 怎么让 CIL 字节码跑起来。这篇在执行策略之上走了一步：代码跑起来之后分配的对象怎么管理和回收。

下一篇 CROSS-G5 将对比泛型实现——共享 vs 特化 vs Full Generic Sharing，这是 AOT runtime 面临的另一个核心难题。CROSS-G5 也是 Phase 2 横切对比的收尾篇。
