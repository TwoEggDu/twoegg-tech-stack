---
title: "CoreCLR 实现分析｜线程与同步：Thread、Monitor、ThreadPool"
slug: "coreclr-threading-synchronization-thread-monitor-threadpool"
date: "2026-04-14"
description: "从 CoreCLR 源码出发，拆解线程与同步的完整实现：托管线程与 OS 线程的包装关系、Thread 结构中的 TLS / GC alloc context / exception state、线程状态机与 GC suspension、Monitor 的 thin lock 到 AwareLock 的锁升级机制、SyncBlock 数组的全局管理、ThreadPool 的 hill-climbing 算法与 I/O 完成端口、.NET 内存模型的 store-release 语义、Thread.MemoryBarrier 的使用场景，以及与 IL2CPP / LeanCLR 的线程模型对比。"
weight: 47
featured: false
tags:
  - CoreCLR
  - CLR
  - Threading
  - Synchronization
  - ThreadPool
series: "dotnet-runtime-ecosystem"
series_id: "coreclr"
---

> 线程是 runtime 中唯一同时跨越用户代码和 OS 内核的抽象——CoreCLR 在 OS 线程之上包装出托管线程，在对象头里内嵌轻量锁，在进程级维护一个自适应线程池，目标是让 C# 开发者用 `lock` 和 `async/await` 就能写出正确的并发代码。

这是 .NET Runtime 生态全景系列的 CoreCLR 模块第 8 篇。

B7 讲了 CoreCLR 的泛型实现——引用类型代码共享、值类型特化。泛型解决的是"同一份算法适用于不同类型"的问题。这篇进入另一个维度：当多个线程同时执行这些代码时，runtime 怎样管理线程本身、怎样保证共享状态的正确访问、怎样调度大量并发任务。

## 线程在 CoreCLR 中的位置

线程子系统涉及 CoreCLR 的多个层次：

**VM 层（`src/coreclr/vm/`）** — 线程管理的核心逻辑。`threads.cpp` / `threads.h` 定义了 `Thread` 结构和线程生命周期管理，`syncblk.cpp` 实现 SyncBlock 和 Monitor 的锁逻辑，`threadpoolrequest.cpp` 管理 ThreadPool 的工作项调度。

**BCL 层（`System.Threading`）** — 面向用户的 API。`System.Threading.Thread` 包装底层的 VM Thread 结构，`Monitor` 类提供 `Enter` / `Exit` / `Wait` / `Pulse` 方法，`ThreadPool` 类提供 `QueueUserWorkItem` 和 Task 调度入口。

**OS 层** — 真正的线程创建和调度由操作系统完成。CoreCLR 在 Windows 上调用 `CreateThread`，在 Linux/macOS 上调用 `pthread_create`。

三层的关系：BCL 提供面向 C# 开发者的 API，VM 层把这些 API 映射到内部的 Thread 结构和同步原语上，OS 层负责真正的线程创建、调度和上下文切换。CoreCLR 的设计选择是 **1:1 映射**——每个托管线程对应一个 OS 线程，不做用户态调度（区别于 Go 的 goroutine 或 Erlang 的 lightweight process）。

## 托管线程 vs OS 线程

CoreCLR 中的"线程"实际上是两层结构的叠加。

### OS 线程

OS 线程是操作系统调度的基本单位。它拥有自己的内核栈、用户栈、寄存器上下文，由 OS 的调度器分配 CPU 时间片。OS 不知道也不关心这个线程上运行的是 C#、C++ 还是其他语言的代码。

### Thread 结构

CoreCLR 为每个托管线程维护一个 `Thread` 对象（C++ 结构，定义在 `src/coreclr/vm/threads.h`）。这个结构不是 `System.Threading.Thread`——后者是 managed 对象，前者是 VM 内部的 native 数据结构。两者通过互相持有引用关联。

Thread 结构包含几类关键信息：

**GC 分配上下文（alloc context）。** 每个线程有自己的分配缓冲区指针（`m_alloc_context`）。`new` 操作在大多数情况下只是把线程私有的分配指针向前推进——不需要加锁，因为每个线程操作自己的缓冲区。分配缓冲区耗尽时才需要向 GC 的全局分配器申请新的缓冲区。B5 讲过的 Gen0 分配机制，在线程层面就是靠这个 per-thread alloc context 实现的无锁快速分配。

**异常状态（exception state）。** 线程当前正在处理的异常信息、嵌套的 exception tracker、从 Pass 1 到 Pass 2 的追踪状态。B6 分析的两遍扫描模型，运行时的状态就存储在每个线程的 Thread 结构中。

**线程本地存储（TLS）。** 托管代码中标记 `[ThreadStatic]` 的静态字段存储在线程私有的区域。Thread 结构维护一个 TLS 表，每个带 `[ThreadStatic]` 的类型占一个槽位，槽位指向该线程私有的字段存储块。

**帧链（frame chain）。** 线程的调用栈上交替出现 managed 帧和 native 帧。Thread 结构维护一个帧链表，记录 managed/native 转换边界的位置，GC 在扫描根时通过这个链表遍历托管栈帧。

**GC 模式标志。** 线程在两种模式之间切换：cooperative 模式（线程运行 managed 代码，GC 不能随意中断）和 preemptive 模式（线程运行 native 代码或处于等待状态，GC 可以随时进行）。这个标志决定了 GC 暂停时是否需要等待该线程到达安全点。

```
Thread 结构（简化）
┌────────────────────────────────┐
│  m_ThreadId          (OS线程ID) │
│  m_alloc_context     (GC分配)   │
│  m_pFrame            (帧链头)   │
│  m_ExceptionState    (异常追踪) │
│  m_ThreadLocalBlock  (TLS)     │
│  m_fPreemptiveGCDisabled (GC模式)│
│  m_State             (线程状态) │
│  m_pManagedObject    (Thread对象)│
└────────────────────────────────┘
```

### 1:1 映射的代价与收益

1:1 映射意味着创建一个 `new Thread()` 就会创建一个 OS 线程，包括分配内核栈（通常 1MB）和用户栈。这使得线程创建成本较高——不适合为每个短任务创建一个线程，这正是 ThreadPool 存在的原因（后面展开）。

收益是简单性和兼容性。OS 调度器已经针对线程调度做了大量优化（优先级、CPU 亲和性、抢占式调度），CoreCLR 不需要在用户态重新实现这些能力。P/Invoke 调用 native 代码时，native 代码看到的就是一个正常的 OS 线程，不存在绿色线程带来的栈切换问题。

## Thread 生命周期

托管线程经历一个明确的状态机。

### 创建

`Thread.Start()` 调用 VM 层的 `Thread::CreateNewThread`，后者调用 OS 的线程创建 API。新线程创建后不会立即执行用户代码——它先执行 runtime 的初始化序列：

1. 创建 Thread 结构并初始化各字段
2. 设置 alloc context（向 GC 申请一块 Gen0 分配缓冲区）
3. 注册到全局的 ThreadStore 链表——runtime 维护所有活跃线程的链表，GC 暂停时需要遍历这个链表
4. 切换到 cooperative 模式
5. 调用用户提供的 `ThreadStart` 委托

### 运行

线程在 cooperative 和 preemptive 模式之间频繁切换。执行 managed 代码时处于 cooperative 模式，调用 P/Invoke 进入 native 代码时切换到 preemptive 模式，从 P/Invoke 返回时切回 cooperative 模式。模式切换的代码在 JIT 为 P/Invoke 调用生成的 stub 中——每次跨越 managed/native 边界都会触发。

### GC 挂起

当 GC 需要收集时，它必须把所有执行 managed 代码的线程暂停到安全点。B5 已经分析过暂停机制（hijacking return address 和 trap page）。从线程的视角看：

- **cooperative 模式的线程** — GC 设置暂停标志后，线程在下一个安全点（方法调用、循环回边）检查到标志，主动暂停
- **preemptive 模式的线程** — 不需要等待。这些线程正在执行 native 代码或等待 I/O，不会修改托管堆上的引用，GC 可以安全地并行进行

这就是为什么 GC 只需要等待 cooperative 模式的线程。长时间运行 native 代码（如阻塞式 I/O）的线程不会延迟 GC 暂停。

### 终止

线程正常结束（`ThreadStart` 委托返回）或异常终止时，runtime 执行清理序列：

1. 执行线程的 finalizer 注册（如果有）
2. 从 ThreadStore 链表中移除
3. 释放 alloc context 中未使用的空间给 GC
4. 释放 TLS 存储
5. 释放 Thread 结构本身

`Thread.Abort()` 在 .NET Core 中已被移除（抛出 `PlatformNotSupportedException`）。在 .NET Framework 中它通过在目标线程上注入一个 `ThreadAbortException` 来中断执行——这个机制被证明是不可靠的（目标线程可能在 native 代码中、可能持有锁、可能处于不一致状态），.NET Core 的设计选择是用 `CancellationToken` 替代线程中止。

## Monitor

`lock(obj) { ... }` 是 C# 中最常用的同步原语。编译器把它展开为 `Monitor.Enter(obj)` 和 `Monitor.Exit(obj)` 调用，实际的锁逻辑在 CoreCLR VM 层实现。

### Thin Lock：对象头内联

最快路径下，锁信息不需要任何额外的数据结构——它直接存储在对象头的 ObjHeader 中。

ECMA-A3 中分析过，CoreCLR 的对象在 MethodTable 指针之前有一个 ObjHeader（位于对象引用的负偏移处）。ObjHeader 是一个 32 位的字段，它的位被划分为多种用途：

```
ObjHeader 位布局（32 位）：
┌─────────────────────────────────────────────┐
│ [31..26] 状态标志                            │
│ [25..16] 持有锁的线程 ID（10 位）             │
│ [15..0]  重入计数 / SyncBlock index          │
└─────────────────────────────────────────────┘
```

当 `Monitor.Enter(obj)` 被调用时，VM 检查 obj 的 ObjHeader：

1. 如果 ObjHeader 为空（没有锁信息、没有哈希码），直接用 CAS（Compare-And-Swap）操作把当前线程 ID 写入 ObjHeader——这就是 thin lock。一条原子指令完成加锁，没有内存分配，没有系统调用
2. 如果 ObjHeader 中的线程 ID 就是当前线程——重入。递增重入计数
3. 如果 ObjHeader 中的线程 ID 是另一个线程——竞争发生，需要升级

Thin lock 的性能极高：在无竞争的情况下，`lock` / `unlock` 只需要两条原子操作（进入时 CAS，退出时 CAS 或直接清零）。大多数 `lock` 的使用场景实际上很少真正发生竞争——同一时刻只有一个线程访问被保护的临界区，thin lock 就是为这种常见场景优化的。

### AwareLock：竞争升级

当 thin lock 检测到竞争（CAS 失败，另一个线程持有锁），需要升级为更重的锁——AwareLock。

升级过程：

1. 为对象分配一个 SyncBlock（从全局 SyncBlock 表中获取一个槽位）
2. 把 ObjHeader 从内联的 thin lock 模式切换为 SyncBlock index 模式
3. SyncBlock 中包含一个 AwareLock 实例
4. AwareLock 内部持有一个 OS 事件对象（Windows 上是 Event，Linux 上是 futex 或 pthread mutex）

```
锁升级路径：
无竞争 → Thin Lock（ObjHeader 内联，一条 CAS 指令）
竞争   → 自旋（短时间自旋等待，避免立即进入内核）
持续竞争 → AwareLock（分配 SyncBlock，创建 OS 同步对象）
         → 等待线程进入内核态睡眠
```

### 自旋优化

在升级到 OS 等待之前，CoreCLR 先做一轮自旋（spin-wait）。自旋的逻辑在 `AwareLock::EnterEpilog` 中：

等待线程在用户态循环检查锁的状态，每次循环执行 `PAUSE` 指令（x86）或 `yield` 指令（ARM），让 CPU 的超线程管线把执行资源让给另一个硬件线程。自旋的次数有上限——如果自旋若干次后锁仍未释放，线程放弃自旋，通过 OS 同步对象进入内核态等待。

自旋优化的假设是：如果锁的持有时间很短（几十到几百纳秒），等待线程在用户态自旋几微秒就能拿到锁，比进入内核态（syscall 开销通常几微秒）更快。如果锁的持有时间较长，自旋浪费 CPU 资源，应该尽快进入内核态睡眠。

## 对象头中的 SyncBlock

SyncBlock 不仅仅用于 Monitor 锁。它是 CoreCLR 中一个通用的"对象附加信息"容器。

### SyncBlock 表

CoreCLR 维护一个全局的 SyncBlock 数组（`SyncBlockCache`，定义在 `src/coreclr/vm/syncblk.h`）。每个需要 SyncBlock 的对象在这个数组中占一个槽位，对象的 ObjHeader 存储该槽位的索引。

```
对象                    SyncBlock 表
┌──────────┐           ┌──────────────────────────┐
│ObjHeader │──index──→ │ [0] SyncBlock (空闲)      │
│=5        │           │ [1] SyncBlock (对象 A)     │
│MethodTbl*│           │ ...                       │
│ 字段...   │           │ [5] SyncBlock (当前对象)   │
└──────────┘           │     - AwareLock            │
                       │     - hash code            │
                       │     - COM interop info      │
                       │     - appDomain index       │
                       └──────────────────────────┘
```

SyncBlock 中可以存储：

- **AwareLock** — Monitor 的重量级锁
- **哈希码** — `Object.GetHashCode()` 的值（如果对象已经用了 thin lock 模式占据了 ObjHeader，哈希码需要存到 SyncBlock 中）
- **COM interop 信息** — COM 可调用包装器（CCW）的指针
- **弱引用支持** — WeakReference 的追踪信息

### SyncBlock 的延迟分配

并非每个对象都需要 SyncBlock。大多数对象在整个生命周期中都不会被 `lock`、不会调用 `GetHashCode()`（或者在 thin lock 未被占用时哈希码直接存在 ObjHeader 中）、不会参与 COM interop。SyncBlock 只在需要时才分配——第一次对某个对象执行 `Monitor.Enter` 且发生竞争时，或者 ObjHeader 的位域不够用时。

SyncBlock 表会随着需求增长。当表满时，runtime 分配一个更大的表并迁移已有的 SyncBlock。GC 在收集时会回收已死亡对象的 SyncBlock 槽位——如果一个对象被回收了，它占用的 SyncBlock 槽位标记为空闲，供后续对象复用。

## ThreadPool

`new Thread()` 创建 OS 线程的代价太高（内核栈分配、内核数据结构创建），不适合处理大量短生命周期的并发任务。ThreadPool 提供了一组预创建的工作线程，任务提交到队列中，空闲的线程从队列取任务执行。

### 工作线程池

ThreadPool 的核心是一个 work-stealing 队列。每个线程有一个本地队列（线程从自己的本地队列取任务，无竞争），还有一个全局队列（线程本地队列为空时从全局队列偷取任务）。`ThreadPool.QueueUserWorkItem` 把 work item 放入全局队列，`Task.Run` 也最终走同样的路径。

```
ThreadPool 架构：
                    全局队列
                 ┌──────────────┐
 QueueUserWork → │ [item] [item] │
 Task.Run    →   │ [item]       │
                 └──────────────┘
                       ↓ steal
    ┌──────────────┬──────────────┬──────────────┐
    │  Worker #1   │  Worker #2   │  Worker #3   │
    │  本地队列     │  本地队列     │  本地队列     │
    │  [item]      │  [empty]     │  [item]      │
    └──────────────┴──────────────┴──────────────┘
```

### Hill-Climbing 算法

ThreadPool 的一个核心问题是：该维持多少个工作线程？线程太少会导致 CPU 空闲（任务排队等待），线程太多会导致过度的上下文切换开销和内存浪费。

CoreCLR 使用 hill-climbing 算法动态调整线程数。算法的基本思路：

1. 以当前线程数为起点，周期性地（约每 500ms）测量吞吐量（单位时间内完成的 work item 数）
2. 小幅增加或减少线程数
3. 测量新的吞吐量
4. 如果吞吐量提高了，继续向这个方向调整；如果降低了，反向调整

这是一个简化的梯度上升——在"线程数 vs 吞吐量"的曲线上爬坡，寻找局部最优。算法在 `src/coreclr/vm/hillclimbing.cpp` 中实现。

Hill-climbing 比固定线程数更适应变化的负载。CPU 密集型任务的最优线程数接近 CPU 核心数，I/O 密集型任务的最优线程数可能是核心数的数倍（因为线程大部分时间在等待 I/O）。Hill-climbing 不需要预先知道负载类型，它通过运行时测量自动收敛。

### I/O 完成端口

ThreadPool 还维护一组独立的 I/O 完成端口（IOCP）线程（Windows 平台）。这些线程专门处理异步 I/O 操作的完成回调。

当 `FileStream.ReadAsync` 或 `Socket.ReceiveAsync` 发起异步 I/O 时，OS 在 I/O 完成后把完成通知投递到 IOCP。ThreadPool 的 IOCP 线程从 IOCP 取出完成通知，执行对应的回调（通常是恢复一个 `async` 方法的执行）。

IOCP 线程和普通工作线程分开管理，因为它们的调度特征不同——IOCP 线程大部分时间在 `GetQueuedCompletionStatus` 上阻塞等待，只在有 I/O 完成时才短暂执行回调。

### Task / async 与 ThreadPool 的关系

`async/await` 是 C# 中最常用的异步编程模型。从 runtime 层面看，async 方法的执行最终依赖 ThreadPool：

1. `async` 方法在遇到 `await` 时，如果被等待的操作尚未完成，当前方法注册一个 continuation（后续执行体）
2. 操作完成时（如 I/O 完成、Task 完成），continuation 被投递到 ThreadPool 的队列
3. ThreadPool 的某个工作线程拾取这个 continuation 并执行——这就是 `await` 之后的代码运行的位置

这意味着 `await` 前后的代码可能在不同的线程上执行。`ExecutionContext`（包含 `AsyncLocal<T>` 值和安全上下文）在 continuation 之间被正确传播，但线程 ID 可能改变。

## Volatile 与 Memory Model

多线程编程中，编译器优化和 CPU 乱序执行可能导致线程之间看到不一致的内存状态。.NET 定义了自己的内存模型来规范这个行为。

### .NET 的强内存模型

.NET 的内存模型比 C++ 的默认内存模型更强——所有的 store（写操作）都隐含 release 语义，所有的 load（读操作）都隐含 acquire 语义。用 C++ 的术语说，.NET 的普通读写近似于 `std::memory_order_acquire` 和 `std::memory_order_release`。

这意味着：
- 一个线程中 store 之前的所有读写操作，在另一个线程通过 load 观察到这个 store 的值后，都是可见的
- store-store 不会被重排序到另一个 store 之后
- load-load 不会被重排序到另一个 load 之前

在 x86/x64 平台上，硬件本身就提供了接近这个强度的内存序——x86 的 Total Store Order（TSO）模型保证 store 不会被重排序。这意味着在 x86 上，.NET 的内存模型几乎是免费的。

在 ARM 平台上情况不同。ARM 的内存模型更弱，允许更激进的重排序。CoreCLR 在 ARM 上为每个 volatile 读写生成相应的 barrier 指令（`dmb` / `dsb`），保证 .NET 内存模型的语义。

### volatile 关键字

C# 的 `volatile` 关键字标记一个字段为易变的——编译器不会对该字段的读写做优化（如缓存到寄存器、消除冗余读取），JIT 生成的代码包含必要的 memory barrier 指令。

```csharp
private volatile bool _shouldStop;

// 线程 A
_shouldStop = true;  // volatile write: release barrier

// 线程 B（循环检查）
while (!_shouldStop)  // volatile read: acquire barrier
{
    DoWork();
}
```

没有 `volatile`，JIT 可能把 `_shouldStop` 缓存到寄存器中，线程 B 永远看不到线程 A 的更新。`volatile` 确保每次读取都从内存获取最新值。

### Thread.MemoryBarrier

`Thread.MemoryBarrier()` 是一个全屏障（full fence）——它阻止 barrier 前后的任何读写操作被重排序。在 x86 上编译为 `mfence` 或 `lock or` 指令，在 ARM 上编译为 `dmb ish`。

在实际代码中，`Thread.MemoryBarrier()` 的使用场景比 `volatile` 少得多。大多数同步需求通过 `lock`、`Interlocked` 操作或 `volatile` 字段就能满足。`MemoryBarrier` 主要出现在无锁数据结构的实现中——当开发者需要对内存操作的顺序有精确控制时。

需要注意的是，.NET 的强内存模型是一把双刃剑。它让多线程编程更容易推理（不需要像 C++ 那样频繁考虑 memory order），但也意味着在弱内存序硬件（ARM）上有额外的 barrier 开销。CoreCLR 的实现会在 ARM 平台上为更多操作插入 barrier 指令，这是正确性的代价。

## 与 IL2CPP / LeanCLR 的线程对比

三种 runtime 面对同一套 ECMA-335 线程模型规范，因目标场景不同做出了截然不同的实现选择。

| 维度 | CoreCLR | IL2CPP | LeanCLR |
|------|---------|--------|---------|
| **线程模型** | 1:1（托管线程 = OS 线程） | 1:1（C++ 线程 = OS 线程） | Universal 单线程 / Standard 1:1 |
| **Thread 结构** | VM Thread（alloc context, TLS, frame chain, GC mode） | `Il2CppThread`（native handle, sync, static fields） | MachineState（eval stack pool, frame stack） |
| **线程池** | 完整 ThreadPool + hill-climbing + IOCP | 简化 ThreadPool（复用 OS 线程池） | Universal 无 / Standard 基础队列 |
| **async/await** | 完整支持（Task, SynchronizationContext, ExecutionContext） | 完整支持（IL2CPP 翻译 async 状态机为 C++ 类） | 不支持 |
| **Monitor 实现** | thin lock → AwareLock 升级 | 对象头 monitor 指针 → OS mutex | Universal 空操作 / Standard OS mutex |
| **SyncBlock** | 全局 SyncBlock 表，延迟分配 | 对象头内联 monitor 指针 | Universal 可省略 sync_block 字段 |
| **GC 暂停协作** | cooperative/preemptive 双模式 + hijacking | GC 暂停由 BoehmGC 管理 | Universal 无需（单线程） |
| **内存模型** | .NET 强模型（store-release, load-acquire） | C++ 内存模型（由平台编译器决定） | N/A（单线程无重排序问题） |

几个差异展开说明：

**CoreCLR vs IL2CPP 的锁实现。** CoreCLR 的 thin lock 是一个精心设计的优化——在无竞争路径上只需要一条 CAS 指令。IL2CPP 走不同的路线：`Il2CppObject` 的对象头中有一个 `monitor` 指针字段（8 字节），`lock` 语句翻译为 C++ 的 `il2cpp::os::Monitor::Enter` 调用，底层映射到 OS 的 mutex。IL2CPP 没有 thin lock 优化，每次加锁都涉及 OS mutex 操作。但在 Unity 的游戏场景中，`lock` 的使用频率远低于服务端应用，这个性能差距在实际表现中并不显著。

**LeanCLR 的单线程策略。** LEAN-F1 和 LEAN-F3 分析过，LeanCLR Universal 版是单线程设计。这个选择的连锁效应在线程模块最为彻底：`lock` 语句退化为空操作（不需要同步），`Monitor.Enter` / `Monitor.Exit` 是 stub，对象头中的 `__sync_block` 字段可以省略（从 16 字节缩减到 8 字节），不需要 TLS（只有一个执行线程），不需要 GC 暂停协作（没有其他线程在修改对象图）。Standard 版恢复了基础多线程能力——每个线程一个 MachineState 实例，`lock` 映射到 OS mutex——但没有 ThreadPool 和 async/await 支持。

**async/await 的实现差异。** CoreCLR 的 async 机制是编译器（Roslyn 生成 async 状态机）和 runtime（ThreadPool 调度 continuation、ExecutionContext 流动）的深度协作。IL2CPP 保留了完整的 async 支持，因为 il2cpp.exe 会把 Roslyn 生成的 async 状态机 IL 翻译成等价的 C++ 状态机类。LeanCLR 不支持 async/await——这不仅因为 Universal 版是单线程，更因为 async 状态机依赖 ThreadPool 和 SynchronizationContext 等基础设施，这些在 LeanCLR 中都不存在。

## 收束

CoreCLR 的线程与同步子系统可以压缩为三个设计层次：

**线程抽象层。** 1:1 映射 OS 线程，每个托管线程附带一个 Thread 结构——GC alloc context 实现无锁分配，TLS 实现线程私有存储，cooperative/preemptive 模式标志让 GC 暂停只等待必要的线程。这些设计让线程不仅是执行单元，也是 GC、异常处理等子系统的协作节点。

**同步机制层。** Monitor 的 thin lock → AwareLock 升级路径体现了一个通用的优化策略：为最常见的情况（无竞争）提供极快路径，只在真正需要时才付出更高的代价。SyncBlock 表的延迟分配遵循同样的原则——大多数对象永远不需要 SyncBlock，不预先分配就是节省。

**调度层。** ThreadPool 通过 hill-climbing 算法自适应调整线程数，work-stealing 队列减少跨线程竞争，IOCP 线程专门处理异步 I/O 完成。`async/await` 在语言层面隐藏了 ThreadPool 的调度细节，但 runtime 层面所有的 continuation 最终都由 ThreadPool 的工作线程执行。

三个层次的关系是：线程抽象层提供执行容器，同步机制层解决共享状态的正确性，调度层解决大量并发任务的效率。CoreCLR 在每一层都选择了工程复杂度较高但运行时性能更好的方案——thin lock 比直接用 OS mutex 复杂得多，hill-climbing 比固定线程数复杂得多，但在服务端高并发场景下这些复杂度换来了可测量的性能收益。

## 系列位置

- 上一篇：CLR-B7 泛型实现：代码共享（reference types）vs 特化（value types）
- 下一篇：CLR-B9 Reflection 与 Emit：运行时代码生成
