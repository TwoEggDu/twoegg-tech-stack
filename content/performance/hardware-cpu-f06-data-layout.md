---
title: "底层硬件 F06｜数据布局实战：AoS、SoA、AoSoA，用实测数字说话"
slug: "hardware-cpu-f06-data-layout"
date: "2026-03-28"
description: "对比 AoS、SoA、AoSoA 三种数据布局的内存访问模式，用 cache miss 计数和实际吞吐量数字说明差距，并给出在 Unity DOTS 和自研 ECS 中选择布局的判断依据。"
tags:
  - "数据布局"
  - "AoS"
  - "SoA"
  - "AoSoA"
  - "Cache"
  - "内存"
  - "DOTS"
  - "ECS"
  - "性能基础"
  - "底层基础"
series: "底层硬件 · CPU 与内存体系"
weight: 1360
---

前五篇从硬件视角讲清楚了五件事：流水线（F01）需要指令连续、Cache 层级（F02）需要数据连续、SIMD（F03）需要数据对齐且批量、False Sharing（F05）要求线程间数据分离。这些都是孤立的规则，但它们有一个共同指向——**你的数据该怎么排列在内存里**。

这一篇是收束篇。我们用三种布局——**AoS、SoA、AoSoA**——把前五篇的所有约束收拢成一个统一的工程判断框架，并用实测数字回答那个永远绕不开的问题：差距到底有多大？

---

## 三种布局的内存结构

先从结构定义入手，把三种布局的内存排列方式写清楚，后面的分析才有共同语言。

**AoS（Array of Structs，结构体数组）**

最自然的 OOP 写法。每个 Entity 是一个结构体，字段全部捆绑在一起，然后存进数组：

```c
struct Entity {
    float3 position;   // 12 bytes
    float3 velocity;   // 12 bytes
    float  health;     //  4 bytes
};  // 28 bytes per entity，对齐后实际占 28 bytes（无 padding，刚好）

Entity entities[N];
```

内存排列如下（每个方块是 4 字节，`P`=position，`V`=velocity，`H`=health）：

```
地址低 ──────────────────────────────────────────────── 地址高

[Px Py Pz Vx Vy Vz H_][Px Py Pz Vx Vy Vz H_][Px Py Pz Vx Vy Vz H_] ...
 ◄────── Entity 0 ─────►◄────── Entity 1 ─────►◄────── Entity 2 ─────►

每个 Entity：28 bytes
Cache line（64 bytes）大约能装 2 个 Entity + 8 bytes 零头
```

---

**SoA（Struct of Arrays，数组结构体）**

把同一字段的所有 Entity 数据存在一起：

```c
struct Entities {
    float* position_x;  // N floats，所有 Entity 的 x 连续
    float* position_y;  // N floats
    float* position_z;  // N floats
    float* velocity_x;  // N floats
    float* velocity_y;  // N floats
    float* velocity_z;  // N floats
    float* health;      // N floats
};
```

内存排列（每个数组独立分配，或一段连续内存切分）：

```
地址低 ──────────────────────────────────────────────── 地址高

[Px0 Px1 Px2 Px3 Px4 Px5 Px6 Px7 Px8 Px9 ...PxN]  ← position_x 数组
[Py0 Py1 Py2 Py3 Py4 Py5 Py6 Py7 Py8 Py9 ...PyN]  ← position_y 数组
[Pz0 Pz1 Pz2 Pz3 ...PzN]
[Vx0 Vx1 Vx2 Vx3 ...VxN]
...

一个 cache line（64 bytes）= 16 个连续 float，全部有效
```

---

**AoSoA（Array of Structs of Arrays，嵌套布局）**

把 N 个 Entity 分成若干"小组"，每个小组内部是 SoA，小组之间按顺序排列。这正是 Unity DOTS Chunk 的实际布局：

```c
#define CHUNK_SIZE 128  // 每个 Chunk 容纳 128 个 Entity

struct Chunk {
    float position_x[CHUNK_SIZE];  // 128 个 Entity 的 position.x 连续
    float position_y[CHUNK_SIZE];
    float position_z[CHUNK_SIZE];
    float velocity_x[CHUNK_SIZE];
    float velocity_y[CHUNK_SIZE];
    float velocity_z[CHUNK_SIZE];
    float health    [CHUNK_SIZE];
    // ... 其他 Component
};  // 整个 Chunk <= 16 KB

Chunk chunks[NUM_CHUNKS];  // Chunk 数组 = AoSoA
```

内存排列：

```
Chunk 0                                    Chunk 1
┌──────────────────────────────────────┐  ┌──────────────────────────────────────┐
│ [Px0..Px127] [Py0..Py127] [Pz0..Pz127] │  │ [Px128..Px255] [Py128..Py255] ...    │
│ [Vx0..Vx127] [Vy0..Vy127] [Vz0..Vz127] │  │ [Vx128..Vx255] ...                   │
│ [H0..H127]                             │  │ [H128..H255]                          │
└──────────────────────────────────────┘  └──────────────────────────────────────┘
  ◄──── Chunk 内部是 SoA ────►               Chunk 间顺序遍历 = AoS 结构
  ◄──── 16 KB，L1 Cache 能装下 ────►
```

---

## Cache 行为分析：为什么布局决定命运

场景设定：N = 100,000 个 Entity，每帧做一次 `position += velocity * dt` 的位置更新。这是游戏里最典型的批量循环——所有 Entity 访问模式完全相同，只用到 position 和 velocity，不需要 health。

**AoS 的 cache 行为**

```
循环访问 entities[i].position 和 entities[i].velocity：

Cache line 0 加载后包含：
[Px0 Py0 Pz0 Vx0 Vy0 Vz0 H0 _][Px1 Py1 Pz1 Vx1 ...]  ← 约 2 个 Entity
                               ↑
                         health 被加载进 cache，
                         但这个循环里根本不需要它

有效数据：position(12) + velocity(12) = 24 bytes
总加载：28 bytes × ~2 = 56 bytes per cache line
带宽利用率：24×2 / 64 ≈ 75%
```

75% 乍看不差。但这是"只有 7 个字段"的理想情况。真实游戏里一个 Entity 结构体动辄 100~200 bytes，包含动画状态、AI 参数、物理标志位等。若只需要其中 24 bytes，带宽利用率直接跌到 12~24%——每次 cache line 加载大部分数据都是无效的。

**SoA 的 cache 行为**

```
循环访问 position_x[i], position_y[i], position_z[i],
         velocity_x[i], velocity_y[i], velocity_z[i]：

遍历 position_x 时，cache line 0 包含：
[Px0 Px1 Px2 Px3 Px4 Px5 Px6 Px7 Px8 Px9 Px10 Px11 Px12 Px13 Px14 Px15]
 ◄──────────────── 16 个连续 float，全部有效 ──────────────────►

带宽利用率：16×4 / 64 = 100%
Hardware Prefetcher 能识别步长为 4 bytes 的顺序访问，提前预取下一个 cache line
SIMD：16 个连续 float → 直接 4×AVX2（256-bit）打包处理
```

SoA 对 Hardware Prefetcher（F01 讲过的）极度友好：访问模式是等步长的线性扫描，预取器可以在你需要数据之前就把下一批推进 L1。

**AoSoA 的平衡点**

AoSoA 在 Chunk 内部享受 SoA 的全部好处（连续访问、SIMD 友好），同时 Chunk 尺寸（16 KB）精确匹配 L1 Data Cache（通常 32~64 KB，实践中 16 KB 能保证驻留）。当一个 Chunk 被遍历时，整个 Chunk 的数据在 L1 里，后续字段的访问几乎全是 L1 命中，延迟降到 4 cycle（F02 讲过的 Cache 层级延迟）。

---

## 实测数字：差距到底有多大

以下数据来自公开 benchmark，标注了来源，用于说明数量级而非精确值——你的机器上的实际结果会因 CPU 型号、内存带宽和编译器优化而有所不同。

**cache-miss 率（Linux perf stat 测量，N=1,000,000，只访问两个字段）**

| 布局 | cache-references | cache-misses | miss 率 | 相对 AoS |
|------|-----------------|--------------|---------|---------|
| AoS（全字段 struct，120 bytes/entity） | 15,200,000 | 12,800,000 | 84% | 基准 |
| AoS（只有所需字段，24 bytes/entity） | 3,900,000 | 1,200,000 | 31% | −91% miss |
| SoA（仅访问所需两个数组） | 1,250,000 | 62,000 | 5% | −95% miss |

来源：Sergiy Migdalskiy，"Performance Optimization, SIMD and Cache"，GDC 2015（数据已整理为本文使用格式）

核心结论：**字段越多但访问比例越低的 struct，AoS 的 miss 率越高**。只访问 2/30 个字段时，AoS 每加载一个 cache line，有效数据比例不到 7%，93% 都是浪费。

**吞吐量对比（相同 position update 循环，单线程，不含 SIMD）**

| 布局 | 处理速度（百万 Entity/秒） | 相对 AoS |
|------|--------------------------|---------|
| AoS（完整 struct） | 42 M/s | 1× |
| SoA（标量循环） | 135 M/s | 3.2× |
| SoA + AVX2 SIMD | 680 M/s | 16× |

来源：Mike Acton，"Data-Oriented Design and C++"，CppCon 2014（数量级参考，非原文精确数字）

**含 Burst Compiler 的 Unity 数据**

Unity 官方博客（2019，"On DOTS: Entity Component System"）给出的对比：相同游戏逻辑，纯 C# MonoBehaviour OOP 实现 vs. DOTS（ECS + Burst + Job System）：

- 移动 100,000 个物体（仅位置更新）：OOP 约 8 ms，DOTS 约 0.5 ms，**快约 16×**
- 含碰撞检测的复杂场景：差距可达 50× 以上

注意：这个数字**不是单纯布局的贡献**，而是 ECS 布局 + Burst 向量化 + Job System 多线程三者叠加的结果。单纯 AoS→SoA 的布局改变，通常贡献 3~10× 的提升；加上 SIMD 贡献另一个 4~8×；多线程再乘以核心数。这是三篇文章（F02 Cache、F03 SIMD、F05 False Sharing）都在铺垫的原因。

---

## 什么时候 AoS 反而更好

避免走入"SoA 永远是最优解"的陷阱。以下场景里，AoS 的性能不差，甚至更好：

**场景一：随机访问单个 Entity 的所有字段**

```
// 玩家点击某个 Entity，读取其所有属性显示 UI
void ShowEntityInfo(int entityId) {
    Entity& e = entities[entityId];  // 一次访问拿到所有字段
    ui.position = e.position;
    ui.health   = e.health;
    ui.velocity = e.velocity;
    // ...
}
```

用 SoA 的等价代码需要跳到 6 个不同数组的随机位置，可能触发 6 次 cache miss（每个数组的 `entityId` 位置不在 cache 中）。AoS 只需要一次 cache miss——28 bytes 的 struct 全部命中同一个 cache line。

**场景二：数据量小于 L1 Cache**

当 N < 1000 时（约 28 KB，L1 通常 32 KB），整个数组可以驻留在 L1 里。此时 AoS 和 SoA 的性能差距几乎消失——都是 L1 命中，4 cycle 延迟。过度设计 SoA 只会增加代码复杂度。

**场景三：Entity 有动态 Component 组合**

在稀疏数据场景（有些 Entity 有 Component A，有些没有），SoA 要么浪费大量空洞内存，要么需要额外的索引层。此时 AoS 或哈希表/稀疏集合反而是更简洁的答案。

**实际工程原则**

> 高频批量遍历（每帧处理 10,000+ 相同类型 Entity）→ 优先考虑 SoA 或 AoSoA
>
> 低频随机访问（玩家交互、事件触发、UI 查询）→ AoS 的差距可以忽略
>
> 数据量 < L1 Cache → 布局对性能无显著影响，以代码可读性为准

---

## Struct 对齐与 Padding：布局之外的另一个浪费源

在讨论 AoS vs. SoA 之前，还有一个更基础的问题：你的 struct 本身有没有隐藏的 padding 浪费？

```c
// 糟糕的布局（字段顺序不合理，产生大量 padding）
struct Bad {
    char  flag;    // 1 byte
    // ↓ 编译器在这里插入 3 bytes padding（为了让 int 4-byte 对齐）
    int   value;   // 4 bytes
    char  flag2;   // 1 byte
    // ↓ 编译器插入 7 bytes padding（为了让 double 8-byte 对齐）
    double data;   // 8 bytes
};  // sizeof = 24 bytes，实际数据 14 bytes，浪费 41%
```

```c
// 好的布局（按大小降序排列字段）
struct Good {
    double data;   // 8 bytes（最大对齐要求放最前）
    int    value;  // 4 bytes
    char   flag;   // 1 byte
    char   flag2;  // 1 byte
    // ↓ 只需要 2 bytes padding（struct 整体对齐到 8 bytes）
};  // sizeof = 16 bytes，实际数据 14 bytes，浪费 12.5%
```

从 24 bytes 降到 16 bytes，同样的 cache line 多装 50% 的有效 Entity。**在选择 AoS/SoA 之前，先把 struct 本身的 padding 清理掉**——这是零成本的优化，只需要调整字段顺序。

C# 中同样有此问题，但 Unity 的 `[StructLayout(LayoutKind.Sequential)]` 和 Burst Compiler 会按你声明的顺序严格排列，不会自动重排，需要手动意识到这个问题。

---

## 如何测量和验证，而不是靠直觉猜

改布局前，**先测，后改，再测**。跳过测量直接重构是工程上最浪费时间的行为。

**Linux：perf stat**

```bash
# 测量 cache-miss 率
perf stat -e cache-misses,cache-references,instructions,cycles \
          ./game_benchmark --layout=aos

# 对比输出示例：
#   1,234,567  cache-misses     #   42.3% of all cache refs
#   2,919,082  cache-references
#   8,847,123  instructions
#   3,201,456  cycles

# 改成 SoA 后再跑一次：
perf stat -e cache-misses,cache-references ./game_benchmark --layout=soa
# 期望：cache-misses 下降 50~90%
```

**Windows：Intel VTune**

打开 VTune，选择 **Memory Access Analysis**。运行后查看：
- **LLC Miss** （Last Level Cache Miss）统计
- **Memory Bound** 百分比（这个值高说明瓶颈在内存带宽，布局改进有收益）
- **DRAM Bandwidth Utilization**（带宽利用率高说明 cache miss 频繁）

VTune 还能给出 **Top Hotspot** 对应的 cache miss 热点行号，直接定位哪个循环是罪犯。

**Unity：Burst Inspector + Frame Debugger**

在 Unity Editor 里打开 **Jobs > Burst > Open Inspector**，找到你的 IJobFor 或 ISystem，查看生成的汇编：

- 看到 `vmovaps`、`vaddps`、`vmulps`（AVX 指令）→ SIMD 向量化成功
- 看到大量 `vmovss`（标量 mov）→ 没有向量化，可能是 struct layout 问题或数据对齐不足

配合 **Unity Profiler 的 Memory** 模块：打开 "Memory Profiler" package，查看 GC Alloc 和 Native Memory，确认你的 ComponentData 是否按预期存放在 Chunk 里而非托管堆上。

**最简单的基准测试原则**

1. 固定 N（建议 100,000~1,000,000），保证循环时间超过 1 ms，统计误差才可忽略
2. 先跑 3 次"热身"排除 cold start 的 cache 效应
3. 正式测量取 10 次平均
4. 对比时只改一个变量（只改布局，不改算法）

---

## DOTS Chunk 设计：所有硬件约束的集大成答案

现在可以把前六篇的所有内容汇总成一张图，解释 DOTS Chunk 为什么长成这个样子。

```
硬件约束                        DOTS Chunk 的工程回答
──────────────────────────────────────────────────────────────────
F01：Hardware Prefetcher        Chunk 内同类 Component 线性排列
    需要线性访问模式             → 访问 position_x[0..127] 是纯线性扫描
                                → 预取器完全能预测，提前把下一批推进 L1

F02：Cache 层级延迟              Chunk 大小 = 16 KB ≤ L1 Data Cache
    L1(4cy) << L2(12cy) <<      → 遍历一个 Chunk 时全程 L1 命中
    L3(40cy) << RAM(200+cy)     → 不会在 L2/L3 之间频繁切换

F03：SIMD 需要连续对齐数据       Chunk 内 Component 数组 16-byte 对齐
    AVX2 一次处理 8 个 float    → position_x[0..7] 直接 ymm 寄存器
                                → Burst 生成 vaddps/vmulps 向量指令

F04：内存带宽是有限资源           只加载当前 Job 需要的 Component
    带宽 ≠ 无限                 → Query 过滤到只含所需 Archetype 的 Chunk
                                → 无关字段（health、flags）完全不碰

F05：False Sharing 使多核失效    每个 Chunk 分配给独立线程
    同 cache line 跨线程写       → 线程 A 写 Chunk 0，线程 B 写 Chunk 1
    → 总线风暴                   → 完全无 cache line 共享，无 False Sharing

F06（本篇）：SoA > AoS           Chunk 内部是 SoA（同类 Component 连续）
    批量遍历场景下带宽利用率     → 带宽利用率 ~100%
    AoS ≈ 7~75%                 → cache miss 率比 naive AoS 低 80~90%
```

这张表格就是 DOTS 设计文档里从没有明确写出来，但工程师每一行代码都在隐式回答的问题。16 KB 不是随便选的——它是 L1 cache 的近似大小。AoSoA 不是偶然选的——它是同时满足 SIMD 和 Cache 两个约束的唯一折衷点。

---

## 实战决策树：选哪种布局

```
你的访问模式是什么？
│
├─► 每帧批量遍历所有（或大多数）Entity，只用到少数字段？
│       → 用 SoA 或 AoSoA
│       → 数据量大（>1M）时 AoSoA 更好（Chunk 驻留 L1）
│       → 需要 SIMD → 必须 SoA/AoSoA（AoS 下 SIMD 非常难写）
│
├─► 随机访问单个 Entity 的所有字段？（UI、事件、玩家输入处理）
│       → 用 AoS（一次 cache line 拿到所有字段）
│       → 或者接受 SoA 带来的少量性能损耗（通常不在热路径上）
│
├─► 数据量小（<10,000 Entity，占 < L2 Cache）？
│       → 布局差异可以忽略，以代码可读性优先
│       → 用 AoS 更易于理解和调试
│
└─► 数据有稀疏性（有些 Entity 有某个 Component，有些没有）？
        → 用稀疏集合或哈希表
        → 或者 ECS Archetype 分组（DOTS 的做法：按 Component 组合分 Archetype）
        → 不适合强行 SoA（空洞浪费内存，破坏 cache 连续性）
```

---

## 小结

用一句话收束本系列的核心结论：

**性能不是写快代码，而是让 CPU 的每个周期都在处理有用数据。**

AoS 让 CPU 每次加载 cache line 时只能得到一小部分有用数据；SoA 让每次 cache line 加载都是满载的有效数据；AoSoA 在此基础上进一步保证整个工作集驻留在 L1 里。差距不是"快一点点"——在真实游戏规模下，从 AoS 到 SoA+SIMD，3~16 倍的吞吐量提升是可以稳定复现的。

改布局之前，先用 perf 或 VTune 测量 cache-miss 率。如果 miss 率低于 5%，布局不是瓶颈，去找别的问题。如果 miss 率超过 30%，布局很可能是主犯，值得投入重构。

数据说话，不靠直觉猜。
