---
title: "底层硬件 F02｜Cache 体系全景：为什么数据布局决定了性能上限"
slug: "hardware-cpu-f02-cache"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "解释 Cache 层级、缓存行与局部性，用明确假设下的字节模型比较 AoS 与 SoA，并区分工作集估算、硬件计数和真实耗时。"
tags:
  - "CPU"
  - "Cache"
  - "内存"
  - "数据布局"
  - "AoS"
  - "SoA"
  - "性能基础"
  - "底层基础"
series: "底层硬件 · CPU 与内存体系"
weight: 1320
---
CPU 执行计算时，数据是否及时到达会影响吞吐量。Cache 利用局部性减少访问更远存储层级的次数；数据布局会影响它的效果，但预取、访问依赖、工作集大小和其他线程的竞争也在起作用。

> **本文的证据边界**：下面的字节数是给定布局假设后的计算，不是性能实测。缓存行、容量和访问延迟要按目标处理器确认；Unity ECS 的说明以 Entities 1.0 文档为基准。

---

## 延迟数字：先区分层级，再看目标机器

L1、L2、末级缓存和主内存构成常见的数据访问层次，但并非所有 CPU 都有相同的层数、容量与共享方式。不能用一张固定的周期表代表桌面 CPU、手机大核和小核。

| 层级 | 在分析中关心什么 | 需要确认的条件 |
|---|---|---|
| L1 数据缓存 | 热点数据能否快速复用 | 每核容量、相联度、缓存行大小 |
| L2 / 末级缓存 | L1 未命中后从哪里得到数据 | 私有或共享、竞争、命中层级 |
| 主内存 | 数据是否需要离开缓存体系 | 访问延迟、可持续带宽、并发请求数 |

**一次 L1 miss 不等于一次 DRAM 访问，更不等于固定时长的 CPU 停顿。** 请求可能在下一层命中，也可能由其他核心的缓存提供数据；乱序执行和多个并发请求还可能掩盖部分等待。沿指针依赖链逐个取数，和同时发出多个独立读取，即使 miss 数相近，也可能表现不同。

若引用延迟数字，应同时给出处理器、频率状态、测量方法和访问模式。串行依赖加载测到的延迟，不能直接作为流式扫描的每元素耗时。[Intel 优化手册入口](https://www.intel.com/content/www/us/en/developer/articles/technical/intel-sdm.html)提供了按微架构分析的资料；实际项目仍需在目标设备上测量。

---

## Cache Line：分析局部性的基本单位

缓存通常以 **cache line** 为单位管理数据。64 bytes 是常见值，**不是 x86-64 与所有现代 ARM 的统一规格**。例如 LLVM 针对 Apple arm64 的[变更说明](https://github.com/llvm/llvm-project/pull/208716)明确讨论了 128-byte cache line。编译器的 interference-size 常量也不能当作所有缓存层级的完整规格表。

下面仅以 **64-byte 缓存行、普通可缓存内存**为例：

```text
地址: 0x00                                      0x3F
      |------------- 一条 64-byte line -------------|
访问 0x04：若需要填充该缓存行，会连同周围字节一起取得
```

**空间局部性**：假设 `float` 占 4 bytes，数组起始地址按 64 bytes 对齐，`arr[0..15]` 落在同一行。该行被取入且尚未驱逐时，后续访问可利用它；不对齐时，这 16 个数可能跨两行。命中仍有加载指令、执行端口和依赖成本，不能称为免费。

**时间局部性**：近期访问的数据如果仍驻留在缓存，再次使用可以避免重复填充。能否留住取决于工作集、组冲突和期间的其他访问，不是“小于容量”就保证命中。

如果一次只用到一行中的少量字节，其余数据又没有机会复用，那么传输和缓存空间的利用率就可能较低。指针遍历是否属于这种情况，要看指向的数据实际怎样分配、是否已在缓存中。

---

## Cache Miss 的三种类型

常用的 3C 分类帮助解释缺失原因，讨论时先明确是哪一级缓存：

- **Cold / Compulsory Miss（首次缺失）**：该级缓存尚未拥有所需数据。下一级可能已经有副本，不一定访问主内存；预取可以提前搬运数据，但不会消除首次传输本身。
- **Capacity Miss（容量缺失）**：复用前访问的工作集过大，数据无法全部保留。数组大于缓存是风险信号，不代表每次访问都缺失；同一行内仍有空间复用。
- **Conflict Miss（冲突缺失）**：多个地址竞争同一组中的有限位置，即使总容量有余也可能相互驱逐。

多核还要考虑一致性引起的失效，例如其他核心写入共享行。不能只凭“每帧遍历很多 Entity”，就断定主要是冷缺失或容量缺失。先看访问路径、复用距离与硬件计数，再决定是否拆分数据。

---

## AoS vs SoA：最重要的布局决策

现在进入本篇最核心的部分。先用一个可核算的例子分析访问足迹，再讨论它能否转化为性能收益。

### AoS（Array of Structs）：结构体数组

AoS 指结构体连续排列，不等同于 OOP，也不等同于对象引用数组。下面是布局示意，假设 `float` 和 `int` 都占 4 bytes：

```cpp
struct alignas(64) Entity {
    float pos_x, pos_y, pos_z;   // 12 bytes
    float vel_x, vel_y, vel_z;   // 12 bytes
    float health;                 // 4 bytes
    float armor;                  // 4 bytes
    int   flags;                  // 4 bytes
    unsigned char other[28];     // 其余字段的占位，便于计算
};
static_assert(sizeof(Entity) == 64, "example requires 64-byte Entity");

Entity entities[10000];
```

这些对象在内存里的布局是这样的：

```
内存地址（每格 = 1个 Entity = 64 bytes = 1 cache line）：

[  Entity[0]  ][  Entity[1]  ][  Entity[2]  ][  Entity[3]  ] ...
 pos vel hp ar  pos vel hp ar  pos vel hp ar  pos vel hp ar
```

现在假设你要执行一个位置更新系统，只需要 pos 和 vel：

```cpp
// 物理更新：position += velocity * dt
for (int i = 0; i < 10000; i++) {
    entities[i].pos_x += entities[i].vel_x * dt;
    entities[i].pos_y += entities[i].vel_y * dt;
    entities[i].pos_z += entities[i].vel_z * dt;
}
```

在本例的 64-byte 缓存行和对齐假设下，每个 Entity 的 pos、vel 位于它自己的那一行。访问 10000 个对象会触及 10000 条不同的缓存行，覆盖 640000 bytes；本循环实际使用的字段共 240000 bytes，比例为 37.5%。

**这是访问足迹的估算，不是 DRAM 带宽实测。** 只有继续约定初始缓存状态、预取和写入策略等，才能讨论传输量。位置更新还包含写入与最终写回，不能只数读入行就声称得到总内存流量。

### SoA（Struct of Arrays）：将字段分开存储

```cpp
struct EntityPool {
    float pos_x[10000];    // 40 KB
    float pos_y[10000];    // 40 KB
    float pos_z[10000];    // 40 KB
    float vel_x[10000];    // 40 KB
    float vel_y[10000];    // 40 KB
    float vel_z[10000];    // 40 KB
    float health[10000];   // 40 KB
    float armor[10000];    // 40 KB
    int   flags[10000];    // 40 KB
};
```

内存布局变成这样：

```
pos_x 数组（连续 40 KB）：
[ 0.f | 1.f | 2.f | 3.f | 4.f | 5.f | 6.f | 7.f | 8.f | 9.f | 10.f | 11.f | 12.f | 13.f | 14.f | 15.f | ... ]
|<---------  1 cache line = 64 bytes = 16 floats  -------->|

vel_x 数组（连续 40 KB）：
[ 0.f | 1.f | 2.f | 3.f | 4.f | 5.f | 6.f | 7.f | 8.f | 9.f | 10.f | 11.f | 12.f | 13.f | 14.f | 15.f | ... ]
|<---------  1 cache line = 64 bytes = 16 floats  -------->|
```

同样的位置更新循环：

```cpp
for (int i = 0; i < 10000; i++) {
    pool.pos_x[i] += pool.vel_x[i] * dt;
    pool.pos_y[i] += pool.vel_y[i] * dt;
    pool.pos_z[i] += pool.vel_z[i] * dt;
}
```

若六个数组各自按 64 bytes 对齐，每个数组的 10000 个 float 覆盖 625 条行，六个数组合计 3750 条行，覆盖 240000 bytes。后续迭代可复用同一行中的相邻元素；其他字段不在本循环的直接访问集合中。

### 对比总结

| 本例的计算项 | AoS | SoA |
|---|---:|---:|
| 需要的位置与速度字段 | 240000 bytes | 240000 bytes |
| 触及的不同缓存行 | 10000 | 3750 |
| 这些行覆盖的字节数 | 640000 | 240000 |
| 有效字段 / 覆盖字节 | 37.5% | 100% |

这些数字依赖本例的字段大小、对齐与访问范围，不能换算成固定加速倍数，也不是实际 cache miss 次数。AoS 若已经拆出紧凑的热数据，差距会变；SoA 也可能增加地址计算或维护多数组的成本。

---

## 硬件 Prefetcher：连续访问的隐藏加速器

硬件预取器可以识别部分连续或固定步长的访问，在程序需要数据前发起读取。SoA 的字段扫描往往便于识别，连续 AoS 同样可能形成规律步长；不能把 AoS 与随机指针跳转混为一谈。

如果下一个地址依赖当前加载的指针值，提前发出请求会更困难。不过对象可能来自连续内存池，也可能已经缓存，不能把每次解引用都算成冷缺失或主内存访问。

软件预取只是一种提示，需要验证它是否比硬件预取更有效。下面是 GCC / Clang 的语法示意，假设 `i`、`count` 非负且 `i < count`，数组有 `count` 个元素；距离 16 仅为示例：

```cpp
if (count - i > 16) {
    __builtin_prefetch(&pool.pos_x[i + 16], 0, 1);
}
```

提示过早可能挤掉有用数据，过晚则来不及，额外指令也有成本。先确认瓶颈，再对比有无预取的真实耗时。

---

## ECS Chunk 设计：按组件组织数据

以 [Unity Entities 1.0 的 Archetype 文档](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-archetypes.html)为准，相同组件组合的 Entity 属于同一个 Archetype，数据存放在 16 KiB 的 Chunk 中。Chunk 内为组件类型分别安排数组，并有 Entity ID 与管理开销；容量取决于具体组件组合。

```text
Chunk（16 KiB）
├── Entity ID 数组
├── Component A：[A0][A1][A2]…
├── Component B：[B0][B1][B2]…
└── 其他组件数据与管理开销
```

这里的 SoA 是**组件之间**的分离。若 Position 组件内部是 `float3`，连续排列的是各 Entity 的 `float3`，并不自动拆成 x、y、z 三个数组。托管组件和超出内联容量的动态缓冲还涉及 Chunk 外的数据，不能套用全部数据都在块内的模型。

只读 Position、Velocity 的循环可以避免直接访问其他组件。但 16 KiB 小于某颗 CPU 的 L1 容量，并不保证整个 Chunk 驻留：还有别的工作集、组冲突及外部访问。Job 并行度也受依赖、任务规模和调度限制。

因此，Chunk 提供了有利于局部性与批处理的组织方式，**没有消灭 cache miss，也不保证所有核心满载**。

---

## 如何验证：实测工具

在目标设备上先确认热点循环，再测布局变化。Linux 上可用下面的命令收集计数，但 `your_program` 需要替换成自己的基准程序：

```bash
perf stat -e cache-misses,cache-references,instructions,cycles ./your_program
```

通用事件的含义与支持情况依 CPU、内核及权限而异，不能把 `cache-misses / cache-references` 一律叫作 L1 miss 率。用 `perf list` 和对应 PMU 文档确认事件；区分采样估计、事件计数以及复用硬件计数器后的缩放值。

Intel VTune / AMD uProf 可以帮助定位内存访问热点，但源代码行归因也受采样和编译优化影响。Unity Profiler 适合先看 Job 耗时与主线程等待；[Burst Inspector](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/editor-burst-inspector.html)用于检查目标平台汇编，不能代替缓存计数或内存带宽测量。

至少记录目标 CPU、编译配置、数据规模、访问字段、线程数、缓存冷暖条件、耗时分布和结果校验。更完整的对照方法见 [F06：数据布局实战]({{< relref "performance/hardware-cpu-f06-data-layout.md" >}})。

<!-- DATA-TODO: 补充同一目标设备上 AoS/SoA 的基准源码、编译命令、原始耗时与 PMU 事件定义；在记录齐全前不添加实测倍数。 -->

---

## 小结

Cache 优化首先要回答：循环需要哪些字节、这些字节怎样分布、何时再次使用。布局模型可以帮助提出优化假设，但访问足迹、cache miss、DRAM 流量和总耗时是不同指标，需要分别验证。
