---
title: "底层硬件 F05｜多核与并行陷阱：False Sharing、内存序和原子操作的真实代价"
slug: "hardware-cpu-f05-multicore-false-sharing"
date: "2026-03-28"
description: "从 Cache Coherence 协议出发，解释 False Sharing 如何让两个线程互相拖累，Memory Ordering 为什么让并发编程困难，以及 DOTS Job Safety System 的设计为何是这些硬件约束的直接映射。"
tags:
  - "CPU"
  - "多核"
  - "False Sharing"
  - "内存序"
  - "原子操作"
  - "DOTS"
  - "Jobs"
  - "性能基础"
  - "底层基础"
series: "底层硬件 · CPU 与内存体系"
weight: 1350
---

你启动了 8 个线程同时累加计数器，满心期待 8 倍性能，结果却比单线程版本慢了 6 倍。

这不是 bug，也不是你用错了 API，而是多核 CPU 的物理结构在悄悄惩罚你。

这篇文章从硬件出发，解释 **False Sharing** 为什么是多线程性能的头号隐形杀手，为什么内存序让并发编程如此困难，以及 DOTS Job Safety System 那些看似繁琐的读写声明究竟在保护什么。

---

## 多核 CPU 的真实拓扑

首先建立一张正确的图：

```
┌─────────────────────────────────────────────────────┐
│  Core 0          Core 1          Core 2          Core 3  │
│  ┌────────┐      ┌────────┐      ┌────────┐      ┌────────┐  │
│  │ L1 32K │      │ L1 32K │      │ L1 32K │      │ L1 32K │  │
│  │ L2 256K│      │ L2 256K│      │ L2 256K│      │ L2 256K│  │
│  └────┬───┘      └────┬───┘      └────┬───┘      └────┬───┘  │
│       └───────┬────────┘              └───────┬────────┘       │
│               │                               │                │
│        ┌──────┴───────────────────────────────┴──────┐        │
│        │                  L3 Cache (共享, 8~32 MB)    │        │
│        └──────────────────────┬───────────────────────┘        │
│                               │                                │
│                         ┌─────┴─────┐                          │
│                         │    RAM     │                          │
│                         └───────────┘                          │
└─────────────────────────────────────────────────────┘
```

关键事实：

- 每个核心有**私有**的 L1（~32KB）和 L2（~256KB），延迟分别是 4 cycle 和 12 cycle
- L3 是所有核心**共享**的，延迟约 40 cycle
- 核心之间**没有直接通信通道**，协调完全依赖 Cache Coherence 协议

这最后一条是理解后续所有问题的根基。

---

## Cache Coherence：多核如何维持数据一致

假设 Core0 和 Core1 都缓存了同一块内存地址（比如 `x = 5`）。现在 Core0 把 `x` 改成 `7`，Core1 的 L1 里还是旧值 `5`——这就是 **Cache Coherence 问题**，不解决会直接导致数据错误。

现代 CPU 用 **MESI 协议**解决这个问题，每条 cache line 有四种状态：

| 状态 | 含义 |
|------|------|
| **M**odified | 只有我有这个 cache line，且我修改过，与内存不同步 |
| **E**xclusive | 只有我有这个 cache line，与内存一致 |
| **S**hared | 多个核心都有这个 cache line 的副本，与内存一致 |
| **I**nvalid | 这个 cache line 无效，需要重新加载 |

发生写操作时，协议是这样的：

```
Core0 想写 cache line X：
  → 广播 "Invalidate" 信号给所有核心
  → 其他核心将自己的 X 标为 Invalid
  → Core0 将自己的 X 标为 Modified
  → Core0 独占写权限

Core1 之后读 cache line X：
  → 发现自己的 X 是 Invalid
  → 从 L3 或内存重新加载（~40+ cycle）
  → 如果 Core0 的 X 还是 Modified 状态，Core0 必须先 flush 到 L3
```

这个流程在两个核心交替写同一地址时会反复触发，每次写操作都强迫对方重新加载，**性能就在这个来回里被耗尽**。

---

## False Sharing：代价昂贵的"巧合邻居"

Cache Coherence 本身是必要的正确性保证。但 False Sharing 是它的副作用：**两个线程根本没有共享数据，却因为数据在同一条 cache line 上而互相拖累**。

Cache line 是 **64 bytes**，这是 CPU 加载和失效的最小单位，不是单个字节，也不是单个 int。

经典例子：

```c
struct Counters {
    int counter0;  // Core0 更新这个
    int counter1;  // Core1 更新这个
};
// sizeof(int) = 4，两个 int 共 8 bytes，远小于 64 bytes
// 它们必然在同一条 cache line 里
```

运行两个线程：

```c
// Thread 0
for (int i = 0; i < 100000000; i++) {
    counters.counter0++;  // 每次写操作：使 Core1 的 cache line 失效
}

// Thread 1
for (int i = 0; i < 100000000; i++) {
    counters.counter1++;  // 每次写操作：使 Core0 的 cache line 失效
}
```

两个线程完全没有共享任何逻辑变量，但硬件层面每次写操作都会触发跨核 Invalidate。**实测结果：这段代码可以比单线程版本慢 5~10 倍**。线程数越多，如果数据布局不对，越慢。

### 修复方案：强制对齐到独立 cache line

```c
struct alignas(64) PaddedCounter {
    int value;
    char padding[60];  // 填充到 64 bytes，独占一条 cache line
};

PaddedCounter counters[NUM_THREADS];
// counters[0] 和 counters[1] 现在在不同的 cache line 上
// Core0 写 counters[0]，Core1 写 counters[1]：互不干扰
```

C# 在 Unity 中等价写法：

```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct PaddedCounter {
    public int value;
    // 编译器自动填充到 64 bytes
}
```

修复后，两个线程真正并行，性能恢复到接近线性扩展。

### False Sharing 的常见出现场景

- **线程局部统计量**放在结构体数组里（最常见）
- **NativeArray 分块处理**时，两个 Job 的边界数据在同一 cache line
- **ECS 组件数据**中频繁写入的小字段紧密排列
- **Job 的返回值**（如 IJobParallelFor 里的输出）与其他线程的输出共享 cache line

---

## 内存序：乱序执行让并发更难

False Sharing 是性能问题，**内存序**（Memory Ordering）是正确性问题。

现代 CPU 为了提高吞吐量，会**乱序执行指令**（Out-of-Order Execution）。编译器也会在优化时**重排指令**。这在单线程下完全安全——CPU 保证从本线程的视角看结果正确。但在多核场景下，其他核心观察到的操作顺序可能和你代码里的顺序不同。

经典 bug 模式：

```c
// Thread 0
data = compute();    // (1)
ready = true;        // (2) 想让 Thread 1 知道数据准备好了

// Thread 1
while (!ready) {}    // (3) 等待
process(data);       // (4) 使用数据
```

直觉上 (1) 一定发生在 (2) 之前，(4) 等 (3) 通过后才执行。但没有内存序保证时，CPU 可能把 (2) 重排到 (1) 之前，Thread 1 看到 `ready = true` 时 `data` 还没写入。

### 内存序选项

C++11 / Rust / C# 都提供了 atomic 操作的内存序参数：

| 内存序 | 含义 | 代价 |
|--------|------|------|
| `Relaxed` | 无顺序保证，只保证原子性 | 最低 |
| `Acquire` | 读操作，保证后面的读写不会被重排到这次读之前 | 低 |
| `Release` | 写操作，保证前面的读写不会被重排到这次写之后 | 低 |
| `SeqCst` | 所有核心看到相同的全局顺序 | 最高（需要 fence 指令） |

游戏开发中最常用的模式是 **Release/Acquire 配对**：

```cpp
// Producer（Job 写入数据）
std::atomic<bool> ready{false};
int data = 0;

// 写数据，然后用 release 写 ready
data = compute();
ready.store(true, std::memory_order_release);  // 保证 data 的写先于 ready 的写

// Consumer（下一个 Job 读取数据）
while (!ready.load(std::memory_order_acquire)) {}  // acquire 保证之后的读能看到 release 之前的写
process(data);  // 安全
```

`SeqCst` 提供最强保证但最昂贵，在 x86 上需要 `MFENCE` 指令，在 ARM 上需要 `DMB` 指令，这两条指令会**强制冲刷 store buffer**，阻塞流水线，代价是几十个 cycle。

**游戏开发建议**：除非你在写无锁队列或消息总线，否则不要手动管理内存序。使用 Job System 和 NativeContainer，让引擎替你处理这些细节。

---

## 原子操作的真实代价

`Interlocked.Increment`、`atomic<int>::fetch_add`——这些看起来只是"线程安全的加法"，但它们的代价远高于普通加法。

| 操作 | 大致延迟 |
|------|----------|
| 普通整数加法 | ~1 cycle |
| 同 core 的 atomic add（cache hit） | ~5 cycle |
| 跨 core 的 atomic add（需要 cache coherence） | ~40~200 cycle |
| 有竞争的 atomic add（多核同时操作同一变量） | ~100~500 cycle（视竞争程度） |

代价来源有两个：

1. **Cache Coherence 参与**：atomic 操作需要独占 cache line 所有权，触发 Invalidate 流程
2. **内存序语义**：默认的 SeqCst atomic 需要 memory fence

**Lock-free 不等于无代价**。Lock-free 的意义是避免内核态切换（`mutex.lock()` 在竞争时会调用操作系统的 futex，代价是微秒级），但原子操作本身在硬件层面仍然昂贵。

实际案例：游戏帧内如果有**百万次**跨线程原子操作（例如每个 Entity 的处理都更新一个共享计数器），这个共享计数器会成为性能瓶颈，即使没有任何锁。

**正确做法**：让每个线程维护自己的局部计数器，最后合并：

```csharp
// Bad：每个 Entity 都原子递增共享计数器
// IJobParallelFor.Execute(int index) { Interlocked.Increment(ref sharedCount); }

// Good：用 NativeArray 每个线程写自己的槽，最后汇总
// 或者使用 Unity 的 NativeCounter / NativeStream
```

---

## DOTS Job Safety System：硬件约束的直接映射

现在可以回答最初的问题：DOTS Job Safety System 为什么要求每个 Job 必须明确声明读或写？

Job System 的依赖图基于这张简单的规则表：

| Job A \ Job B | B 读同一数据 | B 写同一数据 |
|---------------|-------------|-------------|
| **A 读** | 并行 OK | A 必须等 B 写完（或 B 等 A） |
| **A 写** | B 必须等 A 写完 | 串行执行 |

这不是 API 设计的任意规定，而是 **Read-Write Hazard** 的完整枚举：

- **RAR（Read After Read）**：没有依赖，安全并行。多个核心同时读同一 cache line，MESI 协议让它们都进入 Shared 状态，无 Invalidate，无性能损耗。
- **WAR（Write After Read）** 和 **RAW（Read After Write）**：存在真依赖，必须串行化，否则后者读到脏数据。
- **WAW（Write After Write）**：两个 Job 都写同一块内存，结果取决于执行顺序，必须串行。

如果放开限制，允许两个 Job 同时写同一 `NativeArray`：

1. **数据竞争**：最终值取决于 CPU 的执行调度，结果不可预测
2. **False Sharing**：如果两个 Job 写的是相邻元素，触发跨核 cache line invalidation
3. **内存序问题**：没有 fence 保护，写操作对其他核心的可见顺序不确定

这三个问题叠加，轻则产生脏数据，重则崩溃。JobScheduler 的 `[ReadOnly]` 标注实际上是在告诉调度器：**这个 Job 不会触发 Invalidate，可以与其他读者并行**。

```csharp
// Unity DOTS 示例
[BurstCompile]
public struct ProcessJob : IJobParallelFor {
    [ReadOnly] public NativeArray<float3> positions;  // 并行读，无 Invalidation
    public NativeArray<float3> velocities;            // 独占写，需要调度保证串行

    public void Execute(int index) {
        velocities[index] += ComputeForce(positions[index]);
    }
}
```

调度器保证：
- 两个 `ProcessJob` 实例不会同时运行（写 `velocities`，WAW 依赖）
- `ProcessJob` 和只读 `positions` 的其他 Job 可以并行（RAR，安全）
- `ProcessJob` 和写 `positions` 的 Job 必须有序（RAW 或 WAR 依赖）

---

## Job 粒度与调度开销

正确处理了 False Sharing 和内存序，还有最后一个陷阱：**Job 粒度太细**。

线程（Worker Thread）的开销：

- 线程创建/销毁：~1~10 μs（操作系统调度）
- Job 调度（入队/出队）：~100~500 ns
- JobHandle.Complete() 的 spinning wait：~几 μs 到几十 μs

Unity Job System 用 **Worker Thread Pool + Spinning Wait** 设计避免了线程创建开销，但调度本身仍有成本。

`JobHandle.Complete()` 不用 `Thread.Sleep()` 而用 spin wait 的原因：如果 Job 很快完成（< 1ms），Sleep 会让线程进入内核态调度，唤醒延迟不可预测（可能几毫秒）。Spinning 在用户态忙等，延迟更低，代价是短暂占用 CPU——在游戏帧循环里这通常是可接受的。

实际建议：

- **粒度太细**（每个 Entity 一个独立 Job）：调度开销 > 计算收益，比单线程更慢
- **DOTS 的建议**：`IJobParallelFor` 的 batch size 通常设 64~128，Job 内部处理 **1000+ 个元素**效果最好
- **大型 Job + 内部并行** 比 **大量微型 Job** 效率更高

---

## 总结：硬件约束决定软件设计

回到开头的问题：为什么加了线程反而更慢？

原因链是：

```
数据布局不当
    → 多线程操作同一 cache line 的不同字节
    → 每次写触发 MESI Invalidate
    → 对方 cache line 失效，重新从 L3/RAM 加载（40+ cycle）
    → 线程越多，Invalidate 越频繁
    → 并行代码的实际吞吐量低于单线程
```

修复方向：

1. **数据隔离**：每个线程独占自己的 cache line（`alignas(64)` 或按线程分块）
2. **减少跨线程写**：用局部变量积累，最后汇总，避免高频原子操作
3. **显式声明读写**：让调度器（或你的代码）保证正确的依赖顺序
4. **合理粒度**：Job 足够大才能摊薄调度开销

**DOTS Job Safety System 的严格性不是过度设计**，它是这四条修复方向在 API 层面的强制实施。每一条 `[ReadOnly]` 标注，每一个 `JobHandle` 依赖声明，都是在告诉硬件：这里不会发生 False Sharing，这里的内存序是安全的。

游戏引擎把硬件约束封装成了规则，规则背后是物理现实。理解这层关系，你就能在遇到并行性能问题时直接定位根因——而不是靠猜测调整线程数。
