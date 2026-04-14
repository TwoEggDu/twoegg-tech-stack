---
title: "Mono 实现分析｜SGen GC：精确式分代 GC 与 nursery 设计"
slug: "mono-sgen-gc-precise-generational-nursery"
date: "2026-04-14"
description: "拆解 Mono SGen GC 的核心设计：nursery（copying collector，bump pointer 分配）与 major heap（mark-sweep / mark-compact）的分代模型、精确式 GC 与 BoehmGC 保守式的根本差异、write barrier 与 card table 的跨代引用追踪、并发标记策略，以及与 CoreCLR GC / IL2CPP BoehmGC / LeanCLR 的横向对比。"
weight: 63
featured: false
tags:
  - Mono
  - CLR
  - GC
  - SGen
  - Memory
series: "dotnet-runtime-ecosystem"
series_id: "mono"
---

> SGen 是 Mono 的精确式分代 GC，用 nursery + major heap 的两代模型替代了早期的 BoehmGC。nursery 用 copying collector 实现极快的分配和回收，major heap 用 mark-sweep 或可选的 mark-compact 处理长期存活对象。

这是 .NET Runtime 生态全景系列的 Mono 模块第 4 篇。

C3 拆了 Mini JIT 的编译管线，从 IL 到 SSA 到 native code 的完整路径。这篇进入 Mono 的另一个核心子系统：垃圾收集器。GC 与 JIT 的协作极为紧密——JIT 在编译时记录每个安全点的根集信息（GC Map），GC 在收集时依赖这些信息精确枚举存活对象。SGen 能做到精确式收集、对象搬移、分代管理，前提就是 Mini JIT 在编译阶段提供了充分的类型信息。

## SGen 在 Mono 中的位置

Mono 的 GC 经历了一次根本性的架构替换：

```
Mono 早期（< 2.8）
  └── BoehmGC（保守式，第三方库）

Mono 2.8+ → 当前
  └── SGen（精确式，自研分代 GC）
```

BoehmGC 是 Mono 早期使用的垃圾收集器。Boehm-Demers-Weiser GC 是一个通用的保守式 GC 库，不需要编译器或 runtime 的特殊配合——它通过扫描栈和全局数据区中所有看起来像堆地址的值来识别根。这种方式实现简单，但有根本性的限制：无法做对象搬移（不能区分真引用和碰巧等于堆地址的整数），因此无法做堆压缩，无法实现高效的分代收集。

SGen 的开发目标是解决 BoehmGC 的这些限制。它是 Mono 自研的精确式分代 GC，设计上与 CoreCLR 的 GC 同类——都是精确式、都做分代、都支持压缩。SGen 从 Mono 2.8 版本开始成为默认 GC，BoehmGC 仍然保留为备选项（通过编译开关切换），但在主流使用中已经被 SGen 完全替代。

SGen 的源码位于 Mono 代码树的 `mono/sgen/` 目录下，核心文件包括 `sgen-gc.c`（收集器主逻辑）、`sgen-nursery-allocator.c`（nursery 分配器）、`sgen-marksweep.c`（major heap 的 mark-sweep 收集器）、`sgen-copying.c`（nursery 的 copying collector）。

## 分代模型

SGen 采用两代分代模型：nursery（年轻代）和 major heap（老年代）。与 CoreCLR 的三代模型（Gen0 / Gen1 / Gen2）不同，SGen 没有中间的缓冲代。

```
SGen 堆结构：

┌─────────────────────────────────────────────┐
│ nursery（年轻代）                            │
│ 固定大小（默认 4MB）                          │
│ copying collector                           │
│ 新分配的对象在这里                             │
├─────────────────────────────────────────────┤
│ major heap（老年代）                          │
│ 动态增长                                     │
│ mark-sweep（默认）或 mark-compact（可选）      │
│ 从 nursery 存活下来的对象在这里                 │
└─────────────────────────────────────────────┘
```

两代 vs 三代的差异本质上是对"中等寿命对象"的处理策略。CoreCLR 的 Gen1 充当缓冲区，给"比一次 GC 活得长但不一定是长命"的对象一次额外的考察机会。SGen 没有这个缓冲——nursery 收集后存活的对象直接进入 major heap。这意味着 SGen 可能比 CoreCLR 更快地把短中期存活的对象提升到老年代，增加 major collection 的扫描负担。但两代模型的实现更简单，跨代引用追踪的逻辑也更直接——只需要追踪 major → nursery 方向的引用。

## Nursery

### 分配机制：Bump Pointer

Nursery 的对象分配使用 bump pointer（指针碰撞）算法：

```
nursery 内存布局：

[已分配对象][已分配对象][...][当前分配指针 →|空闲空间         ]
                                 ↑
                         bump pointer（分配指针）
```

分配一个新对象只需要三步：

1. 检查 bump pointer + 对象大小 是否超过 nursery 边界
2. 把 bump pointer 返回给调用者作为对象地址
3. 把 bump pointer 向前推进 对象大小 个字节

这是所有内存分配算法中最快的——只需要一次指针加法和一次边界检查。没有空闲链表搜索，没有 best-fit / first-fit 的匹配过程。分配的时间复杂度是 O(1)。

Bump pointer 分配之所以可行，是因为 nursery 使用 copying collector 做回收——回收后所有存活对象都被搬走，nursery 变成一块完整的空闲空间，bump pointer 复位到起始位置。不需要考虑碎片问题，因为每次 nursery GC 后碎片都被彻底消除。

### 多线程分配

多个线程同时分配对象时，如果共享一个 bump pointer 会产生竞争。SGen 的解决方案是线程局部分配缓冲区（TLAB，Thread-Local Allocation Buffer）：

每个线程从 nursery 中分配一块私有的 TLAB（通常几 KB 到几十 KB）。线程内的对象分配在自己的 TLAB 中进行，不需要加锁。TLAB 用完后再从 nursery 全局分配器中获取新的 TLAB——只有这一步需要同步。

CoreCLR 的 Gen0 分配使用了相同的 TLAB 策略（CoreCLR 中称为"allocation context"）。两种实现的设计理由完全相同：把高频的小对象分配变成无锁操作，只在低频的 TLAB 补充时做同步。

### Nursery GC：Copying Collection

当 nursery 空间耗尽（bump pointer 到达 nursery 边界）时，触发 nursery 收集。SGen 的 nursery 使用 copying collector（复制收集器）：

1. **扫描根。** 枚举栈、寄存器、静态字段、GC handle 中的引用
2. **复制存活对象。** 从根出发遍历对象图，把 nursery 中所有可达的对象复制到 major heap
3. **更新引用。** 所有指向已搬移对象的引用被更新为新地址
4. **重置 nursery。** 整个 nursery 变成空闲空间，bump pointer 复位

Copying collector 的核心特性是：只访问存活对象，不访问死亡对象。如果 nursery 中 90% 的对象在 GC 时已经死亡（典型比例），copying collector 只需要处理 10% 的存活对象——扫描范围和搬移代价都与存活对象的数量成正比，与 nursery 的总大小无关。

这与 mark-sweep 形成对比：mark-sweep 在标记阶段也只访问存活对象，但在清扫阶段需要遍历整个堆段来回收死亡对象的空间。对于死亡率极高的年轻代，copying collector 的效率明显更高。

### 与 CoreCLR Gen0 的对比

| 维度 | SGen nursery | CoreCLR Gen0 |
|------|-------------|-------------|
| **大小** | 固定 4MB（可配置） | 动态调整（256KB~几MB） |
| **分配** | bump pointer + TLAB | bump pointer + allocation context |
| **回收算法** | copying collector（搬移到 major） | compacting（搬移到 Gen1） |
| **存活对象去向** | 直接进入 major heap | 进入 Gen1（缓冲区） |
| **碎片** | 无（每次 GC 后 nursery 完全清空） | 无（压缩消除碎片） |

两者在分配端的设计几乎相同——bump pointer + 线程局部缓冲。差异在于回收后存活对象的去向：SGen 直接送入 major heap，CoreCLR 送入 Gen1 缓冲区。

## Major Heap

### Mark-Sweep（默认）

Major heap 的默认收集算法是 mark-sweep：

1. **标记阶段。** 从根出发遍历对象图，标记所有可达对象。标记信息存储在对象头或外部的 mark bitmap 中
2. **清扫阶段。** 遍历 major heap 的所有内存块，回收未标记对象占用的空间，把空间加入空闲链表

Mark-sweep 不搬移对象——存活对象留在原位，死亡对象的空间被标记为空闲。后续的分配从空闲链表中取空间。

SGen 的 major heap mark-sweep 实现把堆空间划分为固定大小的 block（内存块）。每个 block 内部用空闲链表管理可用空间。分配时先在当前 block 的空闲链表中查找合适大小的空间，block 满了再分配新的 block。

### Mark-Compact（可选）

Mark-sweep 的问题是碎片化。长期运行后，大量不同大小的空洞散布在 major heap 中，总空闲空间可能很大但没有单个足够大的连续空间来满足大对象的分配请求。

SGen 提供可选的 mark-compact 收集器作为替代：

1. **标记阶段。** 与 mark-sweep 相同
2. **压缩阶段。** 把存活对象向堆的一端搬移，消除所有碎片
3. **引用更新。** 更新所有指向已搬移对象的引用

Mark-compact 消除碎片的代价是搬移对象的开销——需要拷贝存活对象并更新所有引用。在 major heap 中存活对象较多的场景下（与 nursery 的高死亡率相反，major heap 的存活率通常很高），搬移的代价可能显著。

默认使用 mark-sweep 而非 mark-compact 是一个延迟 vs 碎片的 trade-off：mark-sweep 的 GC 暂停时间更短（不搬移），但长期运行会积累碎片；mark-compact 的暂停时间更长（搬移），但碎片为零。对于 Mono 面向的移动端和嵌入式场景，GC 暂停时间通常是更敏感的指标。

### 碎片处理

在 mark-sweep 模式下，SGen 通过几种机制缓解碎片：

**Block 级管理。** 不同大小的对象分配在不同的 block 中，同一 block 内的对象大小相近。这减少了因大小不匹配导致的空洞浪费。

**空闲链表合并。** 清扫阶段合并相邻的空闲区域，尽可能创建更大的连续空闲块。

**按需切换。** 如果碎片积累到严重影响分配效率的程度，可以通过配置切换到 mark-compact 模式，做一次全量压缩。

## 精确式 GC

### SGen 是精确式 GC

SGen 是精确式（precise / exact）GC——它精确知道每个栈位置、寄存器、对象字段是否包含托管对象引用。这个能力来自两个信息源：

**JIT 生成的 GC Map。** Mini JIT 在编译每个方法时，生成一份 GC Map（与 CoreCLR 的 GC Info 功能等价）。GC Map 记录了方法中每个安全点的根集信息——哪些栈帧偏移包含引用、哪些寄存器持有活跃引用。SGen 在收集时读取 GC Map 来枚举栈根。

**类型系统提供的对象布局。** 每个托管类型的 metadata 描述了它的字段布局——哪些字段是引用类型、哪些是值类型。SGen 在遍历对象图时，根据对象的类型信息知道应该追踪哪些字段。

### 与 BoehmGC 保守式的核心差异

BoehmGC 是保守式（conservative）GC。保守式意味着它不依赖编译器或 runtime 提供的类型信息——它把栈上、寄存器中所有看起来像堆地址的值都当作潜在的引用。

这个差异导致一系列连锁后果：

| 维度 | SGen（精确式） | BoehmGC（保守式） |
|------|---------------|-------------------|
| **根枚举** | 通过 GC Map 精确知道每个位置是否是引用 | 扫描栈/寄存器，把所有疑似地址当引用 |
| **误保留** | 不会发生 | 可能发生——碰巧等于堆地址的整数值导致死对象无法回收 |
| **对象搬移** | 可以（精确知道所有引用的位置，可以安全更新） | 不能（无法区分真引用和碰巧的值） |
| **堆压缩** | 支持（nursery copying + major 可选 compact） | 不支持（只能 mark-sweep） |
| **分代收集** | 高效分代（精确追踪跨代引用） | 不适合分代（无法精确维护跨代引用信息） |
| **编译器协作** | 需要——JIT 必须生成 GC Map | 不需要——GC 独立工作 |

### 为什么 Mono 从 Boehm 换成 SGen

从 BoehmGC 切换到 SGen 的核心动机是性能，具体体现在三个方面：

**分代收集。** BoehmGC 每次收集都扫描整个堆，无法利用分代假设跳过长期存活对象。SGen 的 nursery 收集只扫描 4MB 的年轻代——对于分配密集的应用，GC 暂停时间从毫秒级降到亚毫秒级。

**堆压缩。** BoehmGC 不能搬移对象，长期运行后碎片化不可避免。SGen 的 nursery 使用 copying collector（零碎片），major heap 可选 mark-compact（按需消除碎片）。在 Unity 等长时间运行的应用中，碎片化导致的内存浪费是一个实际问题。

**精确回收。** BoehmGC 的保守扫描可能把碰巧等于堆地址的整数值误判为引用，导致已经死亡的对象无法被回收（false retention）。在 32 位平台上，堆地址空间占整数空间的比例较高，误保留的概率更显著。SGen 的精确扫描彻底消除了这个问题。

## Write Barrier

### 跨代引用追踪

分代 GC 的基本约束：收集 nursery 时不扫描 major heap，但 major heap 中的对象可能持有指向 nursery 对象的引用。如果漏掉这些跨代引用，nursery 中存活的对象会被错误回收。

SGen 使用 card table 机制解决这个问题——与 CoreCLR 的方案相同。

### Card Table 机制

Card table 把整个堆按固定大小（通常 512 字节）划分为 card。每个 card 对应 card table 中的一个字节（或一个 bit）。当一个引用赋值操作可能创建从 major heap 到 nursery 的引用时，对应的 card 被标记为 dirty。

```
Major heap 内存：
|-- card 0 --|-- card 1 --|-- card 2 --|-- card 3 --|-- ...

Card table：
[clean] [dirty] [clean] [dirty] [...]
```

Nursery 收集时，SGen 扫描 card table，只检查 dirty card 对应的内存区域来找到跨代引用。不需要扫描整个 major heap。

### JIT 插入 Write Barrier

Card table 的维护由 JIT 在每个引用赋值处插入的 write barrier 代码完成。当 Mini JIT 编译一条引用类型字段赋值（`obj.field = otherObj`）时，生成的 native code 包含：

1. 实际的 store 指令——把 `otherObj` 的地址写入 `obj.field`
2. Write barrier 序列——计算赋值目标地址所在的 card index，标记对应的 card 为 dirty

Write barrier 的开销是每次引用赋值多执行几条指令。这个开销是分代 GC 的税——没有 write barrier 就无法追踪跨代引用，没有跨代引用追踪就无法安全地做局部收集。值类型赋值不需要 write barrier（值类型不是引用），纯算术操作也不需要。

### 与 CoreCLR 的 Write Barrier 对比

SGen 和 CoreCLR 的 write barrier 机制在原理上完全相同——都是 card table + JIT 插入。差异在于实现细节：

| 维度 | SGen | CoreCLR |
|------|------|---------|
| **Card 大小** | 512 字节 | 实现相关（类似量级） |
| **标记粒度** | 字节级（card table 每 card 一字节） | 字节级 |
| **barrier 类型** | store barrier（赋值时标记） | store barrier |
| **优化** | 基本的条件跳过 | 更精细的条件检查 |

两者的设计哲学一致：用轻量的 write barrier 开销换取年轻代收集的局部性。

## 并发与增量

### SGen 的并发标记

SGen 支持并发标记（concurrent marking）模式——major collection 的标记阶段可以在应用线程继续运行的同时进行。

并发标记的流程：

```
1. 初始标记（STW）
   短暂暂停，标记根直接可达的对象

2. 并发标记（应用线程继续运行）
   GC 线程在后台遍历对象图
   Write barrier 记录并发阶段中引用的变化

3. 最终标记（STW）
   短暂暂停，处理并发阶段中新产生的引用变化
   确保标记结果完整

4. 清扫
   回收未标记对象
```

这个流程与 CoreCLR 的 Background GC 非常接近——同样是三段式（初始标记 STW → 并发标记 → 最终标记 STW），同样依赖 write barrier 追踪并发阶段中的引用变化。

### 与 CoreCLR Background GC 对比

| 维度 | SGen 并发标记 | CoreCLR Background GC |
|------|-------------|----------------------|
| **适用代** | major collection | Gen2 collection |
| **并发阶段** | 标记 | 标记 + 清扫 |
| **年轻代穿插** | nursery GC 可在并发标记期间触发 | foreground GC 可在 BGC 期间触发 |
| **Write barrier 复用** | 复用分代 write barrier | 复用分代 write barrier |
| **多堆并行** | 无（单堆） | Server GC 模式下多堆并行 |

核心差异在 Server GC 的多堆并行能力——CoreCLR 的 Server GC 为每个逻辑 CPU 创建独立堆段，GC 线程并行收集各自的堆段。SGen 只有单一堆结构，没有多堆并行模式。这是 SGen 面向嵌入式和移动端（通常 CPU 核心数有限）的定位决定的——多堆并行在 4~8 核的移动设备上收益有限，在 64 核的服务器上才有显著优势。

## 与 CoreCLR GC / IL2CPP BoehmGC / LeanCLR 的对比

| 维度 | SGen (Mono) | CoreCLR GC | IL2CPP BoehmGC | LeanCLR |
|------|------------|-----------|----------------|---------|
| **精确性** | 精确式（GC Map） | 精确式（GC Info） | 保守式（栈扫描） | 无 GC（宿主管理） |
| **分代** | 2 代（nursery + major） | 3 代（Gen0/1/2） | 无分代 | N/A |
| **年轻代算法** | copying collector | compacting | N/A（无分代） | N/A |
| **年轻代大小** | 固定 4MB | 动态调整 256KB~几MB | N/A | N/A |
| **年轻代分配** | bump pointer + TLAB | bump pointer + allocation context | malloc | N/A |
| **老年代算法** | mark-sweep（默认）/ mark-compact（可选） | mark-sweep-compact | mark-sweep（全堆） | N/A |
| **堆压缩** | nursery 总是压缩，major 可选 | Gen0/1/2 压缩，LOH/POH 不压缩 | 不支持 | N/A |
| **并发收集** | 并发 major 标记 | Background GC（Gen2 并发标记+清扫） | 增量式收集 | N/A |
| **多堆并行** | 不支持 | Server GC（每 CPU 独立堆） | 不支持 | N/A |
| **Write Barrier** | card table + JIT 插入 | card table + JIT 插入 | 不需要（无分代） | N/A |
| **固定对象** | pinning nursery 对象阻止搬移 | POH 集中管理 | 无影响（不搬移） | N/A |
| **碎片化** | 低（nursery copying + major 可选压缩） | 低（压缩消除碎片） | 高（长期运行后不可避免） | N/A |
| **JIT 协作** | 深度协作（GC Map + write barrier） | 深度协作（GC Info + write barrier） | 不需要 | N/A |

几个差异的深层原因：

**SGen vs CoreCLR GC 的定位差异。** 两者在精确性、分代、压缩等核心维度上高度一致——都是精确式、分代、支持压缩、依赖 JIT 协作。差异集中在规模化能力：CoreCLR 的 Server GC 多堆并行、三代缓冲区、POH 集中管理固定对象——这些是面向服务端大堆场景的设计。SGen 面向嵌入式和移动端，单堆模型更简单，两代模型足够应对移动端的分配模式。

**BoehmGC 在 IL2CPP 中仍然可行的原因。** IL2CPP 使用 BoehmGC 而非精确式 GC，不是因为 BoehmGC 更好，而是因为 IL2CPP 的 AOT 转换链路（IL → C++ → native）不生成 CLR 格式的 GC Info。il2cpp.exe 输出的 C++ 代码通过 Clang 编译，Clang 不知道什么是托管引用。要在 IL2CPP 中使用精确式 GC，需要在 C++ 代码层面维护引用信息——这需要额外的工程投入，且与 BoehmGC 的"零侵入"特性相悖。BoehmGC 的保守扫描虽然有误保留和碎片化问题，但在 Unity 游戏的典型内存使用模式下（GC.Collect 手动调用 + 对象池复用），实际影响可控。

**LeanCLR 不自带 GC 的策略。** LeanCLR 作为约 600KB 的纯 C++17 runtime，把内存管理委托给宿主环境。这避免了在轻量 runtime 中实现精确式 GC 的巨大工程复杂度（GC Map 生成、write barrier、安全点机制），代价是无法独立控制收集策略。

## 收束

SGen 的设计可以从三个层次理解：

**分代模型是核心架构。** nursery 承接新分配，copying collector 保证零碎片和快速回收；major heap 容纳长期存活对象，mark-sweep 保证低暂停，mark-compact 可选消除碎片。两代模型比 CoreCLR 的三代更简单，但覆盖了分代假设的核心收益——绝大多数对象在 nursery 阶段死亡，只有少量存活对象需要提升到 major heap。

**精确式是能力基础。** SGen 相比 BoehmGC 的所有优势——分代收集、对象搬移、堆压缩、精确回收——都建立在"知道每个位置是否是引用"这一前提上。这个前提由 Mini JIT 的 GC Map 和类型系统的布局信息共同满足。从 BoehmGC 到 SGen 的切换，本质上是 Mono 在 GC 精度上从"保守猜测"升级到"精确知道"。

**并发标记是延迟优化。** major collection 的标记阶段可以在应用线程继续运行时并发进行，把 STW 暂停限制在初始标记和最终标记的短窗口内。这个策略与 CoreCLR 的 Background GC 同源，区别在于 SGen 没有 CoreCLR Server GC 的多堆并行能力——这是两者目标场景不同的直接结果。

## 系列位置

- 上一篇：MONO-C3 Mini JIT：IL → SSA → native 的编译管线
- 下一篇：MONO-C5 Mono AOT：Full AOT 与 LLVM 后端
