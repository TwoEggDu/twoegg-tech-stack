---
title: "底层硬件 F05｜多核与并行陷阱：False Sharing、内存序和原子操作的真实代价"
slug: "hardware-cpu-f05-multicore-false-sharing"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "从缓存一致性与伪共享出发，区分并发正确性和性能问题，说明原子内存序、Job 依赖、安全检查与任务粒度各自解决什么。"
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

> **C++ 公共前置**：数据竞争、happens-before、mutex/条件变量、原子发布与任务收尾，先读 [CXX-11：数据交给另一个线程后，谁保证它有效且可见]({{< relref "engine-toolchain/cpp-foundations-11-concurrency-handoff.md" >}})。本页保留硬件问题入口；缓存协议、缓存行大小、指令成本和性能倍数不能替代语言正确性证明，具体平台结论仍需相应测量。

把计数工作拆到多个线程后，性能可能没有提高，甚至下降。应先排除数据竞争和错误依赖，再检查共享写入、带宽、调度与负载分配，不能只凭“并行后更慢”就认定是硬件惩罚。

这篇文章从缓存一致性出发，解释 **False Sharing** 如何影响并行性能，再区分内存序、原子操作和 Unity Job 安全检查的责任边界。

> 版本与证据：Unity 部分以 2022.3 的 Job System 文档为基线；C++ 示例使用 C++11 起的原子语义。缓存行大小、拓扑和指令成本依平台而变。本文没有提供设备性能实测，Unity 示例也尚未完成项目内运行验证。

---

## 多核 CPU 的真实拓扑

先用一个简化图建立层次关系，而不是把某款 CPU 的容量和延迟当成通用规格：

```text
Core 0 / L1 ─┐
Core 1 / L1 ─┼─ 缓存层次与一致性互连 ── 内存控制器 ── DRAM
Core 2 / L1 ─┤   （L2、末级缓存可能按核心或集群组织）
Core 3 / L1 ─┘
```

常见设计中 L1 靠近各核心，但 L2 是否私有、末级缓存由哪些核心共享、是否存在跨芯粒或跨 NUMA 节点链路，都要按目标芯片核对。不能统一指定为“L1 32 KB / 4 cycle，L2 256 KB / 12 cycle，L3 所有核心共享”。

核心之间通过互连与缓存一致性机制协作，某些请求可以由另一核心的缓存供数。**缓存一致性协议正是通过通信工作的**，不能说核心之间没有通信通道，更不能据此推断所有数据都必须绕回内存。缓存层次与访问边界可接着看 [F02：Cache]({{< relref "performance/hardware-cpu-f02-cache.md" >}})。

---

## Cache Coherence：多核如何维持数据一致

假设两个核心都缓存了地址 `x`，其中一个核心修改它，硬件必须协调该地址的副本和写权限。这是 **Cache Coherence（缓存一致性）**要处理的问题，但它不自动为程序中不同地址建立完整的同步协议。

用经典 **MESI** 作为教学模型，某一级一致性缓存中的缓存行可处于以下状态：

| 状态 | 含义（简化模型） |
|------|----------------|
| **M**odified | 此缓存持有独占的已修改副本，尚未与内存一致 |
| **E**xclusive | 此缓存持有独占的干净副本 |
| **S**hared | 持有可共享的干净副本，其他缓存可能也有；不保证一定有多个副本 |
| **I**nvalid | 当前副本无效 |

实际处理器可能使用 MESIF、MOESI、目录协议及更多瞬态状态，不能把下列示意当作所有硬件的逐步实现：

```text
Core 0 需要写行 X，但尚未取得写权限
  → 请求该行的写权限
  → 协议使相关共享副本失效并协调所有权
  → 获得权限后执行写入

Core 1 之后请求 X
  → 如果本地副本已失效，重新请求有效数据
  → 数据可能由持有者缓存、下级缓存或内存提供
```

这不要求每次都向所有核心广播，也不要求修改后的数据必须先写回 L3 才能交给另一个核心。具体 x86 协议和状态描述可查 [Intel 架构手册](https://www.intel.com/content/www/us/en/developer/articles/technical/intel-sdm.html)。

当两个核心反复请求同一行的写权限时，所有权交接可能限制吞吐。但某核心持有写权限期间可以连续写入多次，不能把每条 store 都机械地算成一次跨核往返。

---

## False Sharing：代价昂贵的“巧合邻居”

**两个线程访问不同的逻辑变量，却因变量位于同一缓存行而相互影响**，这是伪共享的典型形态。至少一方写入就可能产生这种问题，不限于双方都写；参见 [Linux 的 False Sharing 文档](https://docs.kernel.org/kernel-hacking/false-sharing.html)。

下面仅以**一致性粒度为 64 bytes 的目标**为例。64 bytes 不是所有 CPU 的通用常量。为避免普通计数循环被编译器合并成少量写入，示意使用原子 RMW 保留重复更新：

```cpp
#include <atomic>

struct alignas(64) Counters {
    std::atomic<int> counter0{0};
    std::atomic<int> counter1{0};
};
static_assert(sizeof(Counters) == 64, "example requires one 64-byte block");
Counters counters;

// Thread 0 的循环
// for (int i = 0; i < 100000000; ++i)
//     counters.counter0.fetch_add(1, std::memory_order_relaxed);

// Thread 1 的循环
// for (int i = 0; i < 100000000; ++i)
//     counters.counter1.fetch_add(1, std::memory_order_relaxed);
```

两个线程分别更新自己的对象，不存在对同一计数值的争抢，但两个字段位于同一示例缓存行，可能触发所有权反复交接。这里选原子操作是为了便于观察反复写入，并不表示“线程只写自己的独立字段”在语言层面必需原子操作。做实验还应确认 `std::atomic<int>` 在目标上是否 lock-free、线程实际落在哪些核心，并校验最终计数。

### 修复方案：隔离热点写入，或减少发布次数

在同样的 64-byte 假设下，可以对照下面的布局：

```cpp
struct alignas(64) PaddedCounter {
    std::atomic<int> value{0};
};
static_assert(sizeof(PaddedCounter) == 64, "check array stride");
PaddedCounter isolated[2];
// 两个线程分别更新 isolated[0].value 和 isolated[1].value
```

`alignas` 与对象大小共同保证本例的起点和步长。目标一致性粒度变为其他值时，要重新检查；padding 还会增加空间、缓存和 TLB 压力，不能到处无条件添加。通常应优先考虑局部累积、批量发布，从源头减少跨核可见的写入。

C# 的下面声明只指定布局大小，**不是 C++ `alignas(64)` 的等价物**。`Size` 与字段打包、对齐是不同属性，见 [Microsoft StructLayout 说明](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute?view=netframework-4.8.1)：

```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct PaddedCounter {
    public int value;
}
```

要证明每个对象独占缓存行，还需检查目标运行时的实际布局、分配起点、数组步长和缓存行粒度。仅看到 `Size = 64` 不能完成这个证明；布局调整也不保证恢复线性扩展，其他瓶颈可能仍然存在。

### False Sharing 的常见出现场景

- 线程局部统计量紧密放在同一数组里，且被频繁发布。
- 不同工作线程写相邻分块的边界元素。
- 同一缓存行上的不同 ECS 实体或组件字段由多个线程并发写入。
- 一个线程写热点字段，其他线程频繁读取邻近的只读字段。

诊断时要把这些作为候选：结合地址布局、访问频率和缓存一致性相关计数器，再比较隔离或减少写入后的变化，不能只因“字段相邻”就宣称它已是瓶颈。

---

## 内存序：乱序执行让并发更难

False Sharing 主要是性能问题，**内存序（Memory Ordering）**关乎并发正确性。编译器优化、CPU 执行与其他核心看到操作的顺序，需要通过语言内存模型约束。

经典错误写法如下，假设 `data` 和 `ready` 都是普通 C++ 变量：

```cpp
// Thread 0
// data = compute();
// ready = true;

// Thread 1
// while (!ready) {}
// process(data);
```

这里首先是**缺少同步的数据竞争，导致 C++ 未定义行为**，不能只解释成“CPU 偶尔把两条指令交换了”。编译器也可能优化等待循环。缓存一致性和 `volatile` 都不能替代正确的 C++ 同步，见 [C++ 数据竞争与 happens-before 规则](https://eel.is/c++draft/intro.races)。

### 内存序选项

以下名称按 C++ 原子 API 描述；C# 常用 `Volatile`、`Interlocked` 等 API 表达同步，不能说三种语言都提供同一套参数。

| 内存序 | 保证范围 |
|--------|---------|
| `relaxed` | 保证本次原子访问及该原子对象的修改顺序；不靠这次操作发布其他普通数据 |
| `acquire` | 与读到的相应 release 发布建立同步时，保证发布前的数据可用于后续访问 |
| `release` | 发布此前的操作；需要合适的接收操作配合形成同步 |
| `seq_cst` | 在相应 acquire / release 语义外，顺序一致操作与栅栏还参与规定的单一总序；不是所有普通内存访问都进入一个全局顺序 |

细节见 [C++ 原子排序规则](https://eel.is/c++draft/atomics.order)；C# 例如 [Volatile.Read](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile.read?view=netframework-4.8.1) 有自己的可见性和排序契约。

**Release / Acquire 配对**可以用于一次性发布：

```cpp
#include <atomic>

// 在线程启动前初始化；整个交接期间对象保持存活
std::atomic<bool> ready{false};
int data = 0;

void producer() { // 一个生产者，只发布一次
    data = 42;
    ready.store(true, std::memory_order_release);
}

int consumer() { // 一个消费者
    while (!ready.load(std::memory_order_acquire)) {}
    return data;
}
```

消费者的 acquire 读到生产者 release 写入的 `true` 后，这次发布之前的 `data` 写入 happens-before 随后的读取。这里要求发布后不再并发修改 `data`，也不重置 `ready` 复用协议；实际任务还需负责线程启动、退出和对象生命周期。示例说明可见性，不是推荐用忙等处理长时间任务。

`seq_cst` 如何实现取决于目标、操作类型和编译器，并非每次都生成 `MFENCE` 或 `DMB`。也不能给各内存序排一个跨平台固定周期表。先证明协议正确，再检查目标汇编与竞争负载。

游戏业务通常应优先使用已有任务依赖或同步 API；如果自行实现并发容器，需要独立证明数据访问、发布和回收协议，而不是只把变量改成 atomic。

---

## 原子操作的真实代价

`Interlocked.Increment`、`atomic<int>::fetch_add` 是原子读改写（RMW），不能与寄存器中的普通加法按一张固定延迟表比较。也要区别原子 load、store 和 RMW：它们的实现与所有权需求不相同。

| 对照条件 | 可能影响成本的因素 |
|---------|------------------|
| 单线程更新一个缓存中的原子对象 | 指令实现、内存序与本地依赖链 |
| 多线程更新同一对象 | 真实共享、所有权竞争、硬件重试或序列化 |
| 多线程更新同一行的不同对象 | 伪共享带来的行级竞争 |
| 多线程更新隔离的对象 | 竞争减少，但仍有各自的原子与访存成本 |

原子 RMW 需要实现不可分割的更新；即使使用 `relaxed`，也不会消除同一地址或同一缓存行的竞争。而原子读取并不一概要求独占缓存行。

**Lock-free 不等于无代价，也不等于每个线程都能及时完成。** 它描述进展保证，不以“绝不进入内核态”定义；某个原子类型是否 lock-free 还依赖实现。反过来，mutex 的无竞争路径也不必然发生内核切换。相关定义可查 [C++ 原子 lock-free 规则](https://eel.is/c++draft/atomics.lockfree)。

如果每处理一个实体都更新同一共享计数器，应考虑把高频共享写入改为归约：

```text
每个任务处理自己的一块输入
  → 在局部变量中累计
  → 每块完成时写入该块独占的结果槽
  → 依赖所有块完成后，再汇总结果槽
```

这减少了发布次数，不是只把同样多的原子加法挪到旁边。分块槽与任务编号要明确对应；`IJobParallelFor` 的元素下标不是 Worker 线程编号。具体容器或并行写接口需按项目的 Collections 版本选择，并确认它表达的是归约、计数还是分段输出。

---

## Job Safety System：访问约定与依赖检查

Unity 的读写声明和 Job 依赖首先服务于**访问正确性与生命周期管理**，并不直接控制 MESI，也不保证消除 False Sharing。

先看作用于**同一或重叠数据**的普通访问冲突：

| Job A / Job B | B 读 | B 写 |
|--------------|------|------|
| A 读 | 数据访问本身可并行 | 冲突访问需要按算法要求排序或采用受支持的并发协议 |
| A 写 | 冲突访问需要排序或同步 | 冲突写入需要排序或受支持的并发协议 |

RAW 是读依赖前面的写，WAR 是反依赖，WAW 是输出依赖，不能都称为真依赖。读读之间没有这种数据冲突，但仍可能竞争缓存容量和带宽，并不意味着无性能成本。

对于普通 `NativeContainer` Job，**调用方要传入正确的 `JobHandle` 依赖**。开启安全检查时，冲突的调度可以报错；不能依赖调度器发现两个写者后自动替你串行。`[ReadOnly]` 使容器按只读用途参与检查，并允许多个读者，但它不是“禁止缓存失效”的硬件指令。参见 [NativeContainer 安全检查](https://docs.unity3d.com/2022.3/Documentation/Manual/JobSystemNativeContainer.html) 与 [Job 依赖](https://docs.unity3d.com/2022.3/Documentation/Manual/JobSystemJobDependencies.html)。

下面是一个 Job 定义，要求两个数组覆盖相同范围、互不重叠；示例系数只用于说明读写关系：

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ProcessJob : IJobParallelFor {
    [ReadOnly] public NativeArray<float3> positions;
    public NativeArray<float3> velocities;
    public float coefficient;

    public void Execute(int index) {
        velocities[index] += positions[index] * coefficient;
    }
}
```

若先后两次更新同一份速度数据，调度关系应明确写出。以下是调用方片段，假设容器已创建并初始化，`count` 合法、`batchSize` 为经测试选定的正整数，且没有其他未纳入依赖的访问：

```csharp
var job = new ProcessJob {
    positions = positions,
    velocities = velocities,
    coefficient = coefficient
};
JobHandle first = job.Schedule(count, batchSize);
JobHandle second = job.Schedule(count, batchSize, first);
second.Complete();
// 此后调用方才能读取结果，或在不再使用时释放这些容器
```

同一 Job 内的不同 `Execute(index)` 可以处理不同元素，程序必须保证迭代独立。两个独立 Job 即使逻辑上处理不相交区间，容器级安全检查也可能保守地拒绝其并行写；应使用受支持的范围约定、并发写接口或拆分设计，不能把关闭检查当作正确性证明。

依赖传递与检查也不保证相邻输出不会发生伪共享。Entities 系统对组件依赖的管理还有额外规则，不能把它和这里的普通 `NativeArray.Schedule` 行为混为一谈。

---

## Job 粒度与调度开销

正确处理了数据访问和同步，仍可能因为**任务粒度太细**而得不偿失。线程池减少了逐任务创建线程的需要，但任务入队、分配、依赖处理与结果收尾都不是零成本。

`JobHandle.Complete()` 表示需要等待相关工作完成并恢复相应访问权限；调用线程也可能参与执行任务，不能统一描述成“只做 spinning wait”。相关行为见 [Unity 创建与运行 Job 的说明](https://docs.unity3d.com/cn/2022.3/Manual/JobSystemCreatingJobs.html)。这里不提供脱离 Unity 版本、平台和负载的微秒 / 纳秒常数。

实际需要权衡两个方向：

- 批次太小，调度和领取工作的成本可能占比较高。
- 批次太大，独立批次不够或各批耗时不均，可能造成部分 Worker 空闲、尾部拖长。

因此 `IJobParallelFor` 的 batch size 应根据每个元素的计算量、总元素数和负载差异扫描测试；不是固定设为 64～128，也没有通用的“超过 1000 个元素才划算”门槛。接口对批次与工作窃取的说明见 [IJobParallelFor 文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html)。

测量应同时看任务本身耗时、主线程等待及整帧关键路径。尽可能在结果真正被使用时再完成依赖，但不能在 `Complete` 前越过容器访问或生命周期约束。

<!-- DATA-TODO: 补固定设备和编译配置下的相邻/隔离原子计数器对照、lock-free 状态、线程落点、最终计数和一致性流量；另补 Unity Job 的批次扫描、Profiler 关键路径。当前没有伪共享减速倍数或调度成本实测。 -->

---

## 总结：把正确性与性能证据分开

回到开头的问题：为什么加了线程反而更慢？伪共享是一条可能的原因链：

```text
不同线程频繁访问同一缓存行中的不同数据，且至少一方写
    → 一致性协议协调副本与权限
    → 反复交接或重新取数增加通信与等待
    → 可能限制并行吞吐
```

但识别它需要布局、访问和测量证据，不能由线程数或耗时变化单独推出。修复方向包括：

1. **数据隔离**：按目标缓存行粒度、实际地址与步长隔离热点，评估 padding 的空间代价。
2. **减少共享写入**：局部累计后合并，降低高频发布与竞争。
3. **明确访问依赖**：以语言同步规则或 Job API 契约保证正确性，不能让缓存一致性替代同步。
4. **选择任务粒度**：在调度成本、可并行工作量和负载均衡之间测量取舍。

`[ReadOnly]` 和 `JobHandle` 表达访问与依赖约定，不是向硬件承诺“这里没有 False Sharing”。先证明程序正确，再用目标设备上的生成代码、计数器与关键路径判断性能，才能把并行问题定位到具体原因。
