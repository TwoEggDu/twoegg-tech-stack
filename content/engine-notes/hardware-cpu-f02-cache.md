---
title: "底层硬件 F02｜Cache 体系全景：为什么数据布局决定了性能上限"
slug: "hardware-cpu-f02-cache"
date: "2026-03-28"
description: "用真实延迟数字（L1 4 cycle / L3 40 cycle / RAM 200+ cycle）解释 CPU cache 层次结构，讲清楚 cache line 的工作原理、空间局部性和时间局部性，以及为什么 AoS 在批量遍历时必然触发 cache miss。"
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
CPU 的运算速度已经快到每个 cycle 只需要约 0.3 ns，但主内存的访问延迟高达 60 ns 以上。这两者之间存在约 200 倍的差距，而这个差距正是现代程序性能优化的核心战场。Cache 的存在就是为了弥合这道鸿沟——但它能不能真正发挥作用，完全取决于你怎么组织数据。

---

## 延迟数字：你必须记住的几个数字

讨论 cache 优化之前，先把真实数字摆出来。这些数字来自 "Latency Numbers Every Programmer Should Know" 以及 Anandtech 等基准测试，不同 CPU 世代略有差异，但数量级是稳定的：

| 存储层级 | 容量（典型值） | 延迟（cycles） | 延迟（时间） |
|---------|-------------|--------------|-----------|
| L1 Cache | 32 ~ 64 KB | ~4 cycle | ~1 ns |
| L2 Cache | 256 KB ~ 1 MB | ~12 cycle | ~3 ns |
| L3 Cache | 8 ~ 64 MB | ~40 cycle | ~10 ns |
| 主内存（RAM） | GB 级 | ~200 cycle | ~60 ns |

这张表的含义很直接：如果你的数据在 L1，CPU 4 个 cycle 就能拿到；如果在主内存，要等 200 个 cycle。在这 200 个 cycle 里，CPU 的执行单元要么等待，要么依赖乱序执行机制找别的事做——但如果代码有数据依赖链，就只能干等。

**一次 cache miss 打到主内存，代价等于 L1 命中的 50 倍。** 这不是理论上的损失，是每次循环迭代里实实在在的停顿。

---

## Cache Line：数据传输的最小单位

Cache 不是按字节工作的，而是按 **cache line** 工作。在 x86-64 和现代 ARM 上，cache line 大小固定为 **64 bytes**。

规则非常简单：每当 CPU 需要一个字节，如果它不在 cache 里，CPU 会把包含这个字节的整个 64 bytes 的 cache line 从下一级存储（或主内存）加载进来。

```
主内存中连续的字节排列：
地址:  0x00  0x08  0x10  0x18  0x20  0x28  0x30  0x38
       |--------------------------------------------------| 64 bytes = 1 cache line

访问 0x04 → 整个 0x00 ~ 0x3F 的 cache line 都被加载进 L1
```

这个机制带来两个重要推论：

**空间局部性（Spatial Locality）**：访问 `arr[0]` 之后再访问 `arr[1]` 到 `arr[15]`，这 16 个 `float`（每个 4 bytes，共 64 bytes）已经全部在同一个 cache line 里了，后续 15 次访问几乎免费。

**时间局部性（Temporal Locality）**：最近访问过的数据还留在 cache 里，短时间内再次访问不需要重新加载。L1 虽然只有 32~64 KB，但对于频繁重用的热点数据，这已经足够。

反过来，如果你的访问模式是随机跳转——比如沿着指针链表遍历，或者随机访问一个大数组——每次访问都可能加载一个新的 cache line，而加载进来的大部分数据又不会被用到。**你用了整个 cache line 64 bytes 的带宽成本，却只用上了其中 4 bytes 的数据。**

---

## Cache Miss 的三种类型

不是所有 cache miss 都一样，理解它们的来源有助于针对性优化。

**Cold Miss（冷缺失）**：数据第一次被访问，cache 里什么都没有，必须从内存加载。这类 miss 几乎无法避免，但可以通过预取（Prefetch）来隐藏延迟。游戏中每帧遍历新激活的对象时大量触发这类 miss。

**Capacity Miss（容量缺失）**：工作集数据量超过 cache 容量，频繁地将旧数据踢出再重新加载。如果你的 L3 是 16 MB，但每帧要处理 50 MB 的粒子数据，L3 根本装不下，每次遍历都要大量访问主内存。

**Conflict Miss（冲突缺失）**：与 cache 的组相联（Set-Associative）结构有关——即使 cache 总容量够用，多个数据恰好映射到同一个 cache 组，导致互相驱逐。这类 miss 相对较少见，但在特定内存对齐方式下会出现。

对游戏开发来说，**Cold Miss 和 Capacity Miss 是日常性能问题的主要来源**。大量 Entity 每帧只访问一次，它们的数据在两帧之间已经被其他数据踢出 cache；数据总量超过 L3 大小时，带宽直接打到主内存。

---

## AoS vs SoA：最重要的布局决策

现在进入本篇最核心的部分。理解了 cache line 的工作原理，AoS 和 SoA 的性能差异就完全可以从第一原理推导出来。

### AoS（Array of Structs）：典型 OOP 对象数组

```cpp
struct Entity {
    float pos_x, pos_y, pos_z;   // 12 bytes
    float vel_x, vel_y, vel_z;   // 12 bytes
    float health;                 // 4 bytes
    float armor;                  // 4 bytes
    int   flags;                  // 4 bytes
    // ... 更多字段，假设总计 64 bytes（恰好一个 cache line）
};

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

每次循环迭代，访问 `entities[i]` 时触发一次 cache line 加载（假设数据不在 cache）。这次加载把整个 64 bytes 拉进来，包括 pos、vel、health、armor、flags……

但你只用到了 pos 和 vel，共 24 bytes。其他 40 bytes 的 health、armor、flags 被加载进 L1，占用宝贵的 cache 空间，却一次都没有被使用。

**10000 个对象 → 10000 次 cache line 加载 → 10000 × 64 bytes = 640 KB 的内存带宽消耗，但实际有效数据只有 10000 × 24 bytes = 240 KB。带宽利用率不到 38%。**

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

现在每次加载 `pos_x` 的一个 cache line，得到连续的 16 个 float，接下来 15 次迭代对 `pos_x` 的访问全部命中 cache。`vel_x` 同理。

**10000 个对象的位置更新，只需要访问 pos_x / pos_y / pos_z / vel_x / vel_y / vel_z 六个数组，每个数组 625 个 cache line（10000 floats ÷ 16 floats/line），共约 3750 次 cache line 加载。AoS 是 10000 次，SoA 约是 AoS 的 37.5%。**

health、armor、flags 这次完全没有被加载进 cache，不占用任何带宽，不污染任何 cache 容量。

### 对比总结

```
操作：遍历 10000 个 Entity，只更新 position（用 velocity）

AoS 方案：
  每次迭代  → 1 cache line 加载（64 bytes）
  有效数据  → 24 bytes（pos + vel）
  总加载次数 → 10000 次 cache line
  带宽利用率 → 37.5%

SoA 方案：
  每 16 次迭代 → 1 cache line 加载（pos_x 或 vel_x 等）
  有效数据  → 64 bytes（全部有效）
  总加载次数 → ~3750 次 cache line（只计 pos + vel 字段）
  带宽利用率 → ~100%

性能差距（实测）：3x ~ 10x，取决于对象大小和字段使用率
```

---

## 硬件 Prefetcher：连续访问的隐藏加速器

CPU 内置了 **硬件 Prefetcher**，它持续监测内存访问模式。如果检测到连续的、步长规律的访问模式，就会提前把下一批 cache line 从内存加载进来，在数据被实际访问之前就准备好。

SoA 的访问模式对硬件 Prefetcher 非常友好：`pos_x[0], pos_x[1], pos_x[2] ...` 是完全线性的连续访问，Prefetcher 几乎可以做到零延迟（数据在被访问之前就已经到达 L1）。

AoS 的指针跳转场景则完全不同。考虑一个常见的游戏对象设计：

```cpp
// 用指针存储组件，对象散落在堆的各个角落
class GameObject {
    Transform* transform;    // 指针，指向堆上某处
    Renderer*  renderer;     // 另一个指针，另一个地方
    Collider*  collider;     // 又一个指针
};
```

遍历时，每次解引用指针都跳到内存中一个随机位置，**硬件 Prefetcher 完全无法预测**下一个访问地址，每次都是 cold miss，直接打到主内存，等满 200 cycle。

除了硬件 Prefetcher，还可以使用软件预取提示：

```cpp
// GCC / Clang
__builtin_prefetch(&pool.pos_x[i + 16], 0, 1);

// MSVC / Intel
_mm_prefetch((char*)&pool.pos_x[i + 16], _MM_HINT_T0);
```

这条指令告诉 CPU 提前开始加载后续数据，但前提是你能预测访问地址——SoA 天然满足，随机指针链则做不到。

---

## ECS Chunk 设计：SoA 的工程实现

理解了以上内容，Unity DOTS 的 **Archetype / Chunk** 设计就不再神秘了。

Unity DOTS 将具有相同 Component 组合的 Entity 归为同一个 **Archetype**，每个 Archetype 的数据存储在若干个 **Chunk** 里。Chunk 的固定大小是 **16 KB**——这个数字不是随意选的，16 KB 恰好可以装进大多数 CPU 的 L1 Cache（L1 通常是 32~64 KB）。

Chunk 内部的布局是严格的 SoA：

```
Chunk（16 KB）
├── Component A（所有 Entity 的 A）: [A0][A1][A2][A3]...[An]
├── Component B（所有 Entity 的 B）: [B0][B1][B2][B3]...[Bn]
└── Component C（所有 Entity 的 C）: [C0][C1][C2][C3]...[Cn]
```

当一个 Job 查询 "所有有 Position 和 Velocity 的 Entity" 时：

1. 只访问 Position 数组和 Velocity 数组，其他 Component（比如 RenderMesh、AudioSource）完全不碰
2. 每个 Chunk 的工作集是 16 KB，可以完整放进 L1
3. 数组是连续的，硬件 Prefetcher 全程有效
4. 多个 Job 可以并行处理不同 Chunk，CPU 多核同时满载

**这正是 DOTS 在大量 Entity 场景下性能碾压传统 MonoBehaviour 的根本原因——不是因为它"用了 Jobs"，而是因为它从数据布局层面就消灭了 cache miss。**

---

## 如何验证：实测工具

如果你想亲自验证 AoS vs SoA 的差距：

**Linux（perf）**：
```bash
perf stat -e cache-misses,cache-references,instructions ./your_program
```

**Intel VTune / AMD uProf**：提供详细的 cache miss 热点分析，可以看到具体哪行代码触发了 LLC（Last Level Cache）miss。

**Unity Profiler + Burst Inspector**：在 DOTS 项目里可以直接看到 Job 的 memory throughput 指标；Burst Inspector 可以查看生成的汇编，确认向量化和 prefetch 是否生效。

AoS vs SoA 遍历 100 万个 float 的性能差距通常在 **3x ~ 10x**，具体倍数取决于结构体大小（越大 AoS 越吃亏）和字段使用率（使用率越低 AoS 越浪费带宽）。

---

## 小结

- **延迟鸿沟是真实的**：L1 命中 4 cycle，主内存 200 cycle，50 倍差距不是纸面数字
- **Cache line 是传输单位**：每次 miss 加载 64 bytes，不管你用没用到那 64 bytes
- **空间局部性决定命中率**：连续访问同一数组 → 每个 cache line 物尽其用；随机指针跳转 → 大部分带宽浪费
- **AoS 的问题不是"慢"，而是带宽利用率低**：你花了加载整个对象的代价，却只用了其中一小部分字段
- **SoA 解决的是有效带宽**：只加载需要的字段，cache 空间全部用于有效数据，Prefetcher 全程可用
- **DOTS Archetype/Chunk 是 SoA 的工程实现**：16 KB Chunk 装进 L1，SoA 布局保证带宽利用率，这是 DOTS 性能优势的硬件级根源

下一篇：底层硬件 F03，SIMD 与向量化——当数据布局对齐之后，CPU 如何用一条指令同时处理 4 个或 8 个 float，以及 Burst Compiler 为什么能自动完成这件事。
