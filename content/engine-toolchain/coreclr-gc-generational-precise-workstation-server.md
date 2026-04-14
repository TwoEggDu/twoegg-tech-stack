---
title: "CoreCLR 实现分析｜GC：分代精确 GC、Workstation vs Server、Pinned Object Heap"
slug: "coreclr-gc-generational-precise-workstation-server"
date: "2026-04-14"
description: "从 CoreCLR GC 源码出发，拆解分代模型（Gen0/1/2）与晋升策略、Large Object Heap 的碎片问题与压缩选项、Pinned Object Heap 对固定对象的集中管理、精确式 GC Info 与保守式 BoehmGC 的根本差异、Card Table 驱动的 Write Barrier、GC 暂停与安全点的实现机制、Workstation vs Server GC 的线程模型、Background GC 的并发收集策略，以及与 IL2CPP BoehmGC / Mono SGen / LeanCLR 的横向对比。"
weight: 44
featured: false
tags:
  - CoreCLR
  - CLR
  - GC
  - Memory
  - Performance
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> CoreCLR 的 GC 是一个分代式、精确式、支持并发收集的托管堆回收器。它在延迟和吞吐之间提供两种模式——Workstation 和 Server——并通过 Pinned Object Heap 解决固定对象的碎片化问题。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 5 篇。

B3 拆解了类型系统的 MethodTable / EEClass 分离设计，B4 走通了 RyuJIT 从 IL 到 native code 的编译管线。这篇进入 CoreCLR 的另一个核心子系统：垃圾收集器（GC）。GC 与类型系统和 JIT 的协作极为紧密——类型系统提供 GC 描述信息，JIT 在编译时插入 write barrier 和记录 GC Info，GC 在收集时依赖这些信息精确枚举所有存活对象。

{{< figure src="/images/runtime-ecosystem/coreclr-gc-heap-layout.svg" caption="CoreCLR GC 堆布局：Gen0 最小最快，Gen2 最大最慢。LOH 和 POH 独立于分代体系。" >}}

## GC 在 CoreCLR 中的位置

CoreCLR 的 GC 源码集中在 `src/coreclr/gc/` 目录下，与 VM（`src/coreclr/vm/`）和 JIT（`src/coreclr/jit/`）并列的独立模块。这个目录结构反映了一个设计决策：GC 被设计为可替换组件，通过 `IGCHeap` 接口与 VM 交互。

核心源码文件：

| 文件 | 职责 |
|------|------|
| `src/coreclr/gc/gc.cpp` | GC 核心逻辑——标记、清扫、压缩、分代管理 |
| `src/coreclr/gc/gcinterface.h` | `IGCHeap` 接口定义——VM 与 GC 的契约 |
| `src/coreclr/gc/gcee.cpp` | Execution Engine 端的 GC 集成 |
| `src/coreclr/gc/handletable.cpp` | GC Handle 表——strong、weak、pinned handle |
| `src/coreclr/gc/objecthandle.cpp` | 对象 handle 管理 |

GC 与 VM 的交互通过两个方向的接口完成：VM 通过 `IGCHeap` 调用 GC（分配对象、触发收集），GC 通过 `IGCToCLR` 回调 VM（暂停线程、枚举根）。这个双向接口的设计让 GC 有可能被替换为第三方实现，虽然实践中几乎没有项目这样做。

> **本文明确不展开的内容：**
> - GC 算法的数学证明（标记-清扫的正确性证明、分代假说的统计基础不在本文范围）
> - 具体 GC 调优参数（`GCHeapCount`、`GCConserveMemory` 等配置项的调优指南不在本文展开）
> - .NET MAUI / Blazor 的 GC 配置（框架层面的 GC 行为配置不在本文范围）
> - 自定义 GC 实现（通过 `IGCHeap` 接口替换 GC 的工程实践不在本文展开）

回顾 B1 建立的模块关系，GC 在执行链路中的位置：

```
对象分配请求（new）
  → JIT 生成的分配代码
  → GC Allocator（从 Gen0 分配区取内存）
  → 分配区耗尽 → 触发 GC
  → GC 扫描根（栈、静态字段、GC handle）
  → 标记存活对象 → 清扫/压缩 → 回收内存
```

## 分代模型

CoreCLR GC 采用经典的三代分代策略，基于一个被大量实践验证的假设：大多数对象的生命周期很短（generational hypothesis）。

### 三代结构

**Gen0（第 0 代）——短命对象。** 新分配的对象进入 Gen0。Gen0 的空间最小（通常 256KB~4MB，根据缓存大小和分配速率动态调整），收集频率最高。绝大多数对象在 Gen0 阶段就会死亡——局部变量、临时字符串、LINQ 中间结果、短命的委托对象。Gen0 收集是最快的，因为扫描范围小，且大部分对象已死亡，存活对象的拷贝量很少。

**Gen1（第 1 代）——缓冲区。** 从 Gen0 收集中存活下来的对象被提升（promote）到 Gen1。Gen1 的功能是充当 Gen0 和 Gen2 之间的缓冲——它给那些"比一次 GC 活得长，但也不一定是长命的"对象一次额外的考察机会。Gen1 的大小通常在几 MB 量级，收集频率低于 Gen0。

**Gen2（第 2 代）——长命对象。** 从 Gen1 收集中存活的对象被提升到 Gen2。Gen2 是最大的堆段，容纳所有长期存活的对象——静态数据、缓存、全局集合、长生命周期的服务对象。Gen2 收集代价最高（需要扫描整个托管堆），频率最低。

### 晋升条件

对象在每次 GC 中存活后，被提升到上一代：

```
Gen0 对象存活 → 提升到 Gen1
Gen1 对象存活 → 提升到 Gen2
Gen2 对象存活 → 留在 Gen2（最高代，不再提升）
```

提升不是简单的标记改变。GC 在压缩阶段将存活对象从当前代的内存区域物理搬移到上一代的内存区域。这意味着对象的内存地址会改变，所有引用它的指针都需要更新——这就是精确式 GC 能做而保守式 GC 做不了的事。

### 触发条件

GC 在以下条件下被触发：

- **Gen0 分配区耗尽。** 最常见的触发方式。每次 `new` 分配对象时，分配指针向前推进；当分配指针到达 Gen0 的边界时，触发 Gen0 收集
- **Gen1 空间压力。** 当 Gen0 收集后提升到 Gen1 的对象太多，Gen1 空间不足时，触发 Gen1 收集（同时收集 Gen0）
- **Gen2 空间压力。** 当 Gen1 收集后提升到 Gen2 的对象太多，或总堆大小接近预算时，触发 Full GC（Gen0 + Gen1 + Gen2）
- **显式调用 `GC.Collect()`。** 应用代码主动触发，可指定收集代数
- **系统内存压力。** 操作系统报告物理内存不足时，runtime 触发 Full GC 尝试释放内存

在 `src/coreclr/gc/gc.cpp` 中，`gc_heap::generation_allocator` 管理每代的分配区，`gc_heap::garbage_collect` 是收集的入口，根据代数决定扫描范围。

## Large Object Heap（LOH）

大于 85,000 字节（85KB）的对象不进入 Gen0，而是直接分配在 Large Object Heap 上。LOH 在逻辑上属于 Gen2——只有 Full GC 才会收集 LOH 上的对象。

### 为什么单独设 LOH

分代 GC 的压缩阶段需要搬移存活对象。搬移小对象的成本可接受，但搬移一个几 MB 的 byte 数组代价高昂——内存拷贝的时间和 TLB 失效的开销都与对象大小成正比。LOH 的设计选择是：**默认不做压缩，只做标记-清扫（mark-sweep）**。

### 碎片风险

不压缩意味着 LOH 上会产生碎片。当大对象被回收后，留下的空洞可能无法被后续的分配利用（尺寸不匹配）。在长时间运行的服务端应用中，LOH 碎片化可能导致明显的内存浪费——总堆占用远高于实际存活对象的大小。

### LOH 压缩选项

从 .NET 4.5.1 开始，CoreCLR 提供了 LOH 压缩的选项：

```csharp
GCSettings.LargeObjectHeapCompactionMode =
    GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect();
```

设置 `CompactOnce` 后，下一次 Full GC 会对 LOH 做一次压缩，然后自动恢复为不压缩模式。这是一个按需触发的一次性操作——runtime 不会自动决定何时压缩 LOH，这个决策交给应用代码。

这个设计折中的理由是：LOH 压缩的代价很高（搬移大对象 + 更新引用），自动触发可能在不恰当的时机引入长暂停。让应用在已知的维护窗口（如请求低谷期）主动触发，是更可控的方案。

## Pinned Object Heap（POH）

.NET 5 引入了一个新的堆段：Pinned Object Heap（POH），通过 `GC.AllocateArray<T>(length, pinned: true)` 分配。

### 固定对象的问题

`fixed` 语句或 `GCHandle.Alloc(obj, GCHandleType.Pinned)` 会固定一个对象，告诉 GC 不要搬移它——通常是为了把托管内存地址传给非托管代码（P/Invoke、I/O 缓冲区）。

被固定的对象对 GC 压缩阶段是一个障碍。压缩需要把存活对象向一端搬移以消除碎片，但被固定的对象无法移动。如果固定对象散布在普通堆段的各个位置，它们会把堆切割成多个小碎片，严重降低压缩效率。在高频 I/O 的服务端场景中（如网络编程中大量固定的 byte[] 缓冲区），这个问题尤其明显。

### POH 的解决方案

POH 把所有需要固定的对象集中到一个专用的堆段中。POH 的收集策略与 LOH 类似——只做标记-清扫，不做压缩。但关键区别是：固定对象被隔离在 POH 中后，普通堆（Gen0/1/2）不再受固定对象的干扰，压缩可以高效进行。

```
普通堆（Gen0/1/2）：
  [存活] [死亡] [存活] [死亡] → 压缩后 → [存活][存活][空闲]
  没有 pinned 对象阻碍压缩

POH：
  [pinned] [空洞] [pinned] [空洞] → 不压缩，标记-清扫
  碎片只影响 POH 本身
```

POH 的价值不是消除碎片——它和 LOH 一样不压缩。它的价值是**把碎片限制在一个隔离的区域**，不让固定对象对普通堆的压缩效率造成影响。

## 精确式 GC vs 保守式

CoreCLR 的 GC 是精确式（precise / exact）的。这个特性是 CoreCLR GC 能做压缩、做分代、做对象搬移的前提。

### GC Info——精确根枚举

JIT 在编译每个方法时，同时生成一份 GC Info 数据（存储在 native code 旁边）。GC Info 记录了该方法在每个安全点（safe point）的根集信息：

- **哪些栈位置包含托管对象引用。** 精确到具体的栈帧偏移（rbp-8 是引用，rbp-16 不是）
- **哪些寄存器包含活跃的引用。** 在 call site 处，哪些 callee-saved 寄存器持有对象引用
- **引用的活跃范围。** 一个引用从哪条指令开始活跃、在哪条指令之后不再被使用

**源码位置：** `src/coreclr/jit/gcencode.cpp` 负责 GC Info 的编码，`src/coreclr/gc/gcinterface.h` 中的 `IGCHeap::GarbageCollect` 在收集时调用 `IGCToCLR::GcEnumAllocationsOfMethodRoots` 来枚举根。

GC Info 的存在让 GC 能做到：

1. **精确标记。** 只有真正是引用的位置才被视为根。不会把一个碰巧值等于堆地址的整数误判为引用
2. **安全搬移。** 压缩阶段搬移对象后，所有指向它的引用都能被精确更新——因为 GC 知道这些引用的确切位置
3. **精确释放。** 不会因为误判的"伪引用"导致死对象被错误保留

### 与 BoehmGC 的根本差异

IL2CPP 使用的 BoehmGC 是保守式（conservative）的。保守式 GC 不依赖 GC Info，它把栈上、寄存器中所有看起来像堆地址的值都当作潜在的引用来处理。

这个差异导致了一系列连锁后果：

| 维度 | CoreCLR（精确式） | BoehmGC（保守式） |
|------|-------------------|-------------------|
| **根枚举** | 通过 GC Info 精确知道每个位置是否是引用 | 扫描栈/寄存器，把所有疑似地址当引用 |
| **误保留** | 不会发生 | 可能发生——伪引用导致死对象无法回收 |
| **对象搬移** | 可以搬移（因为能精确更新引用） | 不能搬移（无法区分真引用和碰巧的整数值） |
| **堆压缩** | 支持（分代 GC 的压缩阶段） | 不支持（只能标记-清扫） |
| **碎片化** | 压缩后几乎无碎片 | 长期运行后碎片化不可避免 |
| **JIT 协作** | 需要——JIT 必须生成 GC Info | 不需要——GC 独立工作 |

保守式 GC 的优势是实现简单、对编译器无要求，这就是为什么 IL2CPP 选择了它——il2cpp.exe 生成的 C++ 代码通过 Clang 编译，Clang 不会生成 CLR 格式的 GC Info。精确式 GC 需要 JIT 或 AOT 编译器的深度配合，这是一个工程投入更大但运行时表现更好的选择。

## Write Barrier

分代 GC 面临一个基本问题：收集 Gen0 时不扫描 Gen2，但 Gen2 的对象可能持有指向 Gen0 对象的引用。如果漏掉这些跨代引用，存活的 Gen0 对象会被错误回收。

### Card Table 机制

CoreCLR 使用 Card Table 来追踪跨代引用。Card Table 把整个堆按固定大小（每个 card 覆盖一段内存区域，在实现中每 card 对应若干字节的堆空间）划分为 card。当一个引用赋值可能创建跨代引用时，对应的 card 被标记为"dirty"。

GC 收集年轻代时，只需要扫描 dirty card 对应的内存区域来找到跨代引用，而不需要扫描整个 Gen2。

**源码位置：** Card Table 的操作在 `src/coreclr/gc/gc.cpp` 中的 `gc_heap::mark_through_cards_for_segments` 等函数中。

### JIT 插入 Write Barrier

Card Table 的维护不靠 GC 自己完成——它靠 JIT 在每个引用赋值处插入的 write barrier 代码。

当 JIT 编译一条类似 `obj.field = otherObj` 的引用赋值时，生成的 native code 不只是一条 store 指令。它还包含一段 write barrier 序列：

```
// 伪代码：引用赋值 + write barrier
mov [obj + field_offset], otherObj     // 实际赋值
shr temp, obj + field_offset, card_shift  // 计算 card index
mov byte [card_table + temp], 0xFF     // 标记 card 为 dirty
```

Write barrier 的代码在 `src/coreclr/vm/writebarriermanager.cpp` 和平台相关的汇编文件中（如 `src/coreclr/vm/amd64/JitHelpers_Slow.asm`）。JIT 在遇到引用类型字段赋值时，调用这些辅助函数而不是直接生成 store 指令。

Write barrier 的开销是分代 GC 的税——每次引用赋值多执行几条指令。但这个税让年轻代收集可以只扫描一小部分堆，收益远大于开销。值类型赋值不需要 write barrier（值类型不是引用），纯算术操作也不需要——只有引用类型字段的赋值才触发 write barrier。

## GC 暂停与安全点

GC 在标记阶段需要看到一个一致的对象图。如果应用线程在 GC 扫描的同时修改引用关系，标记结果可能不正确。因此，GC 需要在标记阶段暂停所有应用线程——这就是 Stop-The-World（STW）暂停。

### 暂停机制

CoreCLR 使用两种机制来暂停应用线程：

**Hijacking Return Address。** GC 修改目标线程栈帧上的返回地址，把它替换为一个 GC 暂停 stub 的地址。当线程从当前函数返回时，它不会回到原来的调用者，而是跳转到暂停 stub，在那里等待 GC 完成。GC 完成后恢复原始的返回地址，线程继续执行。

**Trap Page。** runtime 维护一个特殊的内存页。在正常执行期间，这个页是可读的。当 GC 需要暂停线程时，把这个页的权限改为不可访问。应用线程在安全点处会读取这个页——如果读取触发了访问违规异常，线程知道 GC 正在等待，主动暂停自己。

这两种机制解决的是同一个问题：如何在不等待线程主动检查暂停标志的前提下，尽快让所有线程到达安全点。

### 安全点（Safe Points）

安全点是代码中 GC 可以安全介入的位置——在这些位置，GC Info 能精确描述当前栈帧中所有引用的状态。

CoreCLR 中的安全点出现在两类位置：

**方法调用处（call sites）。** 每个方法调用都是一个安全点。在调用时，调用者的栈帧已经建立完毕，GC Info 能准确描述哪些栈位置和寄存器持有引用。

**循环回边（back edges）。** 循环体中跳回循环头部的指令处是安全点。这保证了一个不包含任何方法调用的紧密循环（如纯计算循环）也不会无限期地推迟 GC 暂停。

JIT 在这些安全点处记录 GC Info，确保 GC 在暂停线程后能精确枚举根。一个不包含调用和循环的直线代码段中间没有安全点——但这类代码段的执行时间通常极短，不会显著推迟 GC 暂停。

## Workstation vs Server GC

CoreCLR 提供两种 GC 模式，针对完全不同的应用场景。

### Workstation GC

**默认模式，适用于桌面应用和交互式场景。**

- 使用单个 GC 线程执行收集
- GC 在触发收集的应用线程上执行（该线程变成 GC 线程）
- Gen0/Gen1 收集暂停时间短（通常亚毫秒到几毫秒）
- 堆预算较小，更频繁地触发收集以保持低内存占用
- 设计目标：**低延迟优先**——尽量缩短每次 GC 暂停的时间，避免界面卡顿

Workstation GC 的堆结构是一个统一的托管堆，所有应用线程共享同一组 Gen0/1/2 段。

### Server GC

**需要显式开启，适用于服务端高吞吐场景。**

通过配置开启：

```xml
<GarbageCollectionServer enabled="true" />
```

或环境变量：

```
DOTNET_gcServer=1
```

- 为每个逻辑 CPU 创建一个独立的 GC 线程和独立的堆段
- 每个堆段有自己的 Gen0/1/2 区域
- GC 收集时，所有 GC 线程并行工作，各自处理自己的堆段
- 堆预算更大，减少 GC 触发频率
- 设计目标：**吞吐优先**——利用多核并行缩短总的 GC 时间，最大化应用的有效工作时间

| 维度 | Workstation GC | Server GC |
|------|---------------|-----------|
| **GC 线程数** | 1（复用应用线程） | 每逻辑 CPU 一个专用线程 |
| **堆段** | 单一共享堆 | 每 CPU 独立堆段 |
| **Gen0 大小** | 较小（256KB~几MB） | 较大（数十MB） |
| **暂停频率** | 较高（小堆更快填满） | 较低（大堆填满更慢） |
| **单次暂停时间** | 短（扫描范围小） | 可能更长（堆更大），但并行弥补 |
| **吞吐** | 较低（串行收集） | 高（并行收集） |
| **适用场景** | GUI、CLI、低核数环境 | Web 服务、微服务、高核数服务器 |

Server GC 的开销是更高的内存占用——每个 CPU 的独立堆段意味着总堆大小是 Workstation 模式的数倍。在只有 1~2 个 CPU 核心的环境下，Server GC 没有并行优势，反而浪费内存。

## Background GC

无论 Workstation 还是 Server 模式，Gen2 的收集都可以在后台进行——这就是 Background GC（BGC）。

### 问题：Full GC 暂停太长

Gen2 收集需要标记整个堆的存活对象。在堆较大的应用中（几 GB 甚至更大），Full GC 的标记阶段可能需要几十到几百毫秒。如果整个过程都在 STW 状态下执行，应用会经历一次明显的暂停。

### Background GC 的工作方式

Background GC 把 Gen2 收集拆成可以并发执行的阶段：

```
1. 初始标记（STW）
   短暂暂停所有线程，标记根直接可达的对象

2. 并发标记（应用线程继续运行）
   GC 线程在后台遍历对象图，标记存活对象
   应用线程继续分配和执行
   Write barrier 记录并发阶段中引用的变化

3. 最终标记（STW）
   短暂暂停，处理并发阶段中新产生的引用变化
   确保标记结果完整

4. 并发清扫（应用线程继续运行）
   GC 线程回收死对象的内存
```

Background GC 的两次 STW 暂停（初始标记和最终标记）都很短，因为它们只处理增量变化。主要的标记和清扫工作在应用线程继续运行的状态下完成。

需要注意的是，Background GC 只适用于 Gen2 收集。Gen0 和 Gen1 的收集仍然是完全 STW 的——但它们本身就很快（小堆段 + 高死亡率），所以 STW 的暂停时间通常可以接受。

在 Background GC 进行期间，如果 Gen0 分配区耗尽需要触发 Gen0 收集，这个 Gen0 收集会在 Background GC 的并发阶段之间插入执行——称为 foreground GC。这保证了年轻代收集的及时性不受后台 Gen2 收集的影响。

## 与 IL2CPP BoehmGC / Mono SGen / LeanCLR stub 的对比

| 维度 | CoreCLR GC | IL2CPP BoehmGC | Mono SGen | LeanCLR |
|------|-----------|----------------|-----------|---------|
| **精确性** | 精确式（GC Info） | 保守式（栈扫描） | 精确式（GC Map） | 无 GC（宿主管理） |
| **分代** | 3 代（Gen0/1/2） | 无分代 | 2 代（nursery + major） | N/A |
| **堆压缩** | 支持（Gen0/1/2 压缩，LOH/POH 不压缩） | 不支持 | 支持（major heap 可选压缩） | N/A |
| **并发收集** | Background GC（Gen2 并发） | 增量式收集 | 并发 major 收集 | N/A |
| **固定对象** | POH 集中管理 | 固定语义由 runtime 保证（不搬移所以无影响） | pinning nursery 对象阻止搬移 | N/A |
| **Write Barrier** | Card Table + JIT 插入 | 不需要（无分代） | Card Table + JIT/解释器插入 | N/A |
| **模式选择** | Workstation / Server | 单一模式 | 单一模式 | N/A |
| **碎片化** | 低（压缩消除碎片） | 高（长期运行后碎片化） | 中（取决于是否启用压缩） | N/A |
| **JIT 协作** | 深度协作（GC Info + write barrier） | 不需要 | 深度协作 | N/A |

几个差异的深层原因：

**BoehmGC 不需要分代的理由。** 分代 GC 的前提是精确知道跨代引用。BoehmGC 是保守式的，无法精确追踪引用变化，也就无法正确维护 card table。不做分代意味着每次收集都要扫描整个堆，但 BoehmGC 的增量式收集（把一次完整收集拆成多个小步骤）在一定程度上缓解了暂停时间的问题。

**Mono SGen 的定位。** SGen 是 Mono 的精确式分代 GC，设计上与 CoreCLR GC 最接近——都是精确式、都有分代、都支持压缩。区别在于 SGen 只有两代（nursery 和 major），没有 Server 模式的多堆并行能力。SGen 面向嵌入式和移动端场景，CoreCLR GC 面向服务端和桌面场景，并行收集能力是后者的差异化特性。

**LeanCLR 不自带 GC。** LeanCLR 作为一个面向 H5/小游戏的纯 C++17 runtime（约 600KB），不实现自己的 GC。托管堆的内存管理委托给宿主环境（如浏览器的 JavaScript GC 或游戏引擎的内存管理器）。这个设计选择避免了在一个轻量 runtime 中实现完整 GC 的复杂度，代价是无法独立控制收集策略和暂停时机。

## 收束

CoreCLR 的 GC 可以用三层来理解：

**分代模型是基础架构。** Gen0 承接新分配、Gen1 做缓冲、Gen2 容纳长命对象。LOH 隔离大对象避免高代价搬移。POH 集中管理固定对象避免碎片扩散到普通堆。每一层的存在都是为了把收集的范围缩小到"需要处理的那部分"。

**精确式 GC Info 是能力基础。** 没有 JIT 生成的 GC Info，GC 无法精确枚举根，无法安全搬移对象，无法做压缩，分代也就无从实现。这是 CoreCLR GC 与 BoehmGC 之间最本质的差异——不是策略不同，而是信息量不同。JIT 编译和 GC 之间的深度协作（GC Info + write barrier + safe points）是整个系统能工作的前提。

**Workstation / Server / Background 是策略层。** 在分代和精确式的基础之上，根据应用场景选择不同的线程模型和暂停策略。Workstation 偏低延迟，Server 偏高吞吐，Background GC 让 Gen2 的长时间标记不阻塞应用。策略可以切换，但底层的分代模型和精确式扫描是固定的。

## 系列位置

- 上一篇：CLR-B4 JIT 编译器：RyuJIT 的 IL → IR → native code 编译管线
- 下一篇：CLR-B6 异常处理：两遍扫描模型与 SEH 集成
