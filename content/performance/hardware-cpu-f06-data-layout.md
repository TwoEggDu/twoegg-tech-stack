---
title: "底层硬件 F06｜数据布局实战：按访问模式选择 AoS、SoA 与 AoSoA"
slug: "hardware-cpu-f06-data-layout"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "对比 AoS、SoA、AoSoA 的访问足迹、向量化条件与维护成本，给出可复核的布局计算和实验方法，明确 Unity ECS Chunk 的适用边界。"
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

前面的硬件基础讨论了执行依赖、Cache、SIMD、带宽与 False Sharing。这些机制共同影响一个工程问题：**数据该怎样排列，才能服务实际的访问模式？**

这一篇用 AoS、SoA、AoSoA 三种布局建立判断框架。下文的字节数是明确假设后的推导，代码是布局示意；本文尚未提供可复现的性能基准，不能据此承诺加速倍数。

---

> **先补语言规则**：sizeof、alignof、成员偏移、填充和合法字节访问见 [CXX-06：从一段字节读到一个值]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})。公共课先确认访问成立的前提，再回到这里讨论排列与访问成本；布局观察和性能测量应分别记录。

## 三种布局的内存结构

先从结构定义入手，把三种布局的内存排列方式写清楚，后面的分析才有共同语言。

**AoS（Array of Structs，结构体数组）**

每个 Entity 是一个结构体，字段捆绑在一起，然后连续存进数组。AoS 不等同于面向对象，也不等同于对象指针数组。示例假设 `float` 大小和对齐均为 4 bytes、`Float3` 为 12 bytes，目标 ABI 上应检查 `sizeof` / `alignof`：

```cpp
struct Float3 { float x, y, z; };
struct Entity {
    Float3 position;   // 12 bytes
    Float3 velocity;   // 12 bytes
    float  health;     //  4 bytes
};
static_assert(sizeof(Entity) == 28, "check the example layout");

Entity entities[N];
```

内存排列如下（每个方块是 4 字节，`P`=position，`V`=velocity，`H`=health）：

```
地址低 ──────────────────────────────────────────────── 地址高

[Px Py Pz Vx Vy Vz H_][Px Py Pz Vx Vy Vz H_][Px Py Pz Vx Vy Vz H_] ...
 ◄────── Entity 0 ─────►◄────── Entity 1 ─────►◄────── Entity 2 ─────►

每个 Entity：28 bytes
以 64-byte cache line 为例：行边界可以落在 Entity 内部，不能按每行两个对象计数
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

[Px0 Px1 Px2 Px3 Px4 Px5 Px6 Px7 Px8 Px9 ...Px(N-1)]  ← position_x 数组
[Py0 Py1 Py2 Py3 Py4 Py5 Py6 Py7 Py8 Py9 ...Py(N-1)]  ← position_y 数组
[Pz0 Pz1 Pz2 Pz3 ...Pz(N-1)]
[Vx0 Vx1 Vx2 Vx3 ...Vx(N-1)]
...

假设数组按 64 bytes 对齐：一行可容纳 16 个 4-byte float；只扫描该字段时都被使用
```

---

**AoSoA（Array of Structs of Arrays，嵌套布局）**

把 N 个 Entity 分成若干"小组"，每个小组内部是 SoA，小组之间按顺序排列。下面是自定义的按标量字段分块示例，不是 Unity DOTS Chunk 的结构声明：

```c
#define CHUNK_SIZE 128  // 示例块容量，需按实际循环选择

struct Chunk {
    float position_x[CHUNK_SIZE];  // 128 个 Entity 的 position.x 连续
    float position_y[CHUNK_SIZE];
    float position_z[CHUNK_SIZE];
    float velocity_x[CHUNK_SIZE];
    float velocity_y[CHUNK_SIZE];
    float velocity_z[CHUNK_SIZE];
    float health    [CHUNK_SIZE];
};  // 在 float 为 4 bytes 的假设下，字段载荷为 7 × 128 × 4 = 3584 bytes

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
  ◄──── 分块限制每次处理的范围，不保证 L1 驻留 ────►
```

---

## Cache 行为分析：布局如何改变访问足迹

场景设定：N = 100000 个 Entity，循环执行 `position += velocity * dt`，不读 health。下文继续使用 4-byte float、28-byte Entity、64-byte 缓存行的假设；缓存行大小应按设备确认，见 [F02：Cache 体系]({{< relref "performance/hardware-cpu-f02-cache.md" >}})。

**AoS 的 cache 行为**

连续扫描这组 28-byte 结构体时，相邻对象共享缓存行，对象也会跨行。不能把一行简化成两个完整对象，再用 `24 × 2 / 64` 得到利用率。

忽略首尾行的边界开销，整个数组为 `28 × N` bytes，循环使用的字段为 `24 × N` bytes，因此有效字段占比约为 **24/28 ≈ 85.7%**。这个比例描述本例扫描覆盖的字节，不是 cache miss 率。若结构体变大，还要看热字段偏移：冷字段位于未触及的缓存行时，并不会仅因属于同一结构体就全部读入。

**SoA 的 cache 行为**

六个位置、速度数组的有效载荷为 `24 × N` bytes，health 数组无需直接访问。按 64 bytes 对齐时，一行容纳 16 个 float，可在相邻迭代中复用。

连续标量字段也便于向量加载。例如 AVX2 的 256-bit 向量容纳 8 个 32-bit float，16 个 float 需要两个这样的向量宽度；这不等于整个更新只需两条指令，还要加载速度、运算并写回。能否自动向量化还取决于依赖、别名、编译目标与优化选项，不能只看布局名称。

**AoSoA 的平衡点**

AoSoA 把一次处理的数据限制在块内，有机会改善跨字段复用，也便于分配任务。但块容量需要兼顾向量宽度、尾部浪费、索引成本和工作集。小于 L1 标称容量不等于保证命中：缓存组竞争、其他线程和代码访问的外部数据仍可能驱逐它。

布局优化的预期是减少不必要的访问或改善执行方式。最终耗时是否下降，还取决于原来受限于什么，以及新布局增加了哪些成本。

---

## 怎样比较布局收益：先拆开变量

本文未保留与原始代码、构建配置、设备和计数器定义对应的 benchmark 记录，因此不把整理过的吞吐量、miss 率或演讲中的数量级作为实测证据。

可以按下面的对照组补测。表中是实验设计，**不是已经得到的结果**：

| 对照 | 尽量保持一致的条件 | 要回答的问题 |
|---|---|---|
| 完整 AoS → 紧凑热字段 AoS | 算法、线程数、向量化策略 | 拆冷热数据是否已经足够？ |
| 紧凑 AoS → SoA | 字段、工作量、编译选项 | 字段分离带来多少净收益？ |
| SoA → AoSoA | 总元素数、计算逻辑 | 不同块容量是否改善复用？ |
| 单线程 → 多线程 | 同一布局与正确性校验 | 并行收益何时受带宽或调度限制？ |

若要分离 SIMD 的贡献，需要显式控制向量化选项并检查汇编；相同编译开关不保证两种布局生成相同执行方式。面向产品的最终比较，则应使用实际发布优化配置。

不能把布局、SIMD、核心数的估计收益相乘：它们可能在解决同一个瓶颈，优化后瓶颈也会转移。记录各阶段耗时，才知道收益来自哪里。

<!-- DATA-TODO: 补充 AoS/SoA/AoSoA 基准源码、处理器与编译器版本、原始多轮耗时、PMU 事件定义和正确性结果；在复现之前不恢复加速倍数或 miss 率表。 -->

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

在这个七个标量字段的例子中，SoA 需要访问七个数组的对应位置，AoS 的字段则相邻。AoS 因此可能减少不同缓存行的访问，但 28-byte 对象也可能跨行；实际 miss 次数还取决于缓存是否已经持有这些行，不能固定写成一次对七次。

**场景二：数据量小于 L1 Cache**

工作集较小、频繁复用时，减少缓存传输的收益可能缩小，但容量只是条件之一，向量化与指令成本仍可能不同。不能用固定 Entity 数量作为“布局无影响”的门槛；低频路径优先保持清晰，再由测量决定是否重构。

**场景三：Entity 有动态 Component 组合**

在稀疏数据场景中，要同时考虑查找、增删与遍历。可以用稠密组件数组配合稀疏索引，也可以按 Archetype 分组；SoA 不必给每个 Entity 的每种可选组件都预留空洞。选择取决于访问和结构变更的比例。

**实际工程原则**

> 高频扫描少数字段时，把 SoA、AoSoA 和冷热拆分列为候选；随机访问完整对象时，保留 AoS 作为基线。布局选择同时包含性能收益、迁移成本和维护成本。

---

## Struct 对齐与 Padding：布局之外的另一个浪费源

另一个基础问题是 struct 的 padding。下面假设 char / int / double 的大小为 1 / 4 / 8 bytes，对齐为 1 / 4 / 8 bytes；其他 ABI 或 pack 设置下必须重新检查。

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

在这个 ABI 假设下，从 24 bytes 降到 16 bytes，连续数组的总占用减少三分之一。字段重排不是零成本：它可能影响 Native 互操作、二进制序列化与既有文件格式，也可能改变热点字段的位置。先确认契约，再测收益。

C# 的 `StructLayout` 来自 `System.Runtime.InteropServices`。`LayoutKind.Sequential` 约束字段顺序，不意味着没有填充；字段类型、Pack 和运行环境仍影响偏移。使用 Burst 或 Native 互操作时，按目标配置核对实际布局。

---

## 如何测量和验证，而不是靠直觉猜

改布局前，**先测，后改，再测**。先确认这个循环在实际场景中占多少时间，再决定基准实验规模。

**Linux：perf stat**

```bash
# game_benchmark 与参数是接口示意，需自行实现基准程序
perf stat -e cache-misses,cache-references,instructions,cycles ./game_benchmark --layout=aos
perf stat -e cache-misses,cache-references,instructions,cycles ./game_benchmark --layout=soa
```

用 `perf list` 和目标 CPU 的 PMU 文档确认事件含义。通用 cache 事件不一定描述 L1，可能不可用或需要权限；不要预设改成 SoA 就应下降某个百分比，也不要直接比较不同处理器的同名事件比例。

**Windows：Intel VTune / AMD uProf**

将内存访问热点与耗时关联起来。Memory Bound 可能涉及延迟、带宽或缓存层级等限制，不等同于 DRAM 带宽饱和。高带宽也可能说明流式访问已经有效利用硬件，仍需结合瓶颈分类判断。具体指标含义以工具和处理器版本为准，可从 [VTune 的 Memory Access Analysis 说明](https://www.intel.com/content/www/us/en/docs/vtune-profiler/user-guide/2024-0/memory-access-analysis.html)开始。

**Unity：Profiler + Burst Inspector**

Profiler 用于定位耗时、调度等待及分配；[Burst Inspector 1.8](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/editor-burst-inspector.html)用于检查目标 CPU 的循环汇编。x86 的 AVX 指令不能作为 ARM 目标的检查标准，单条向量 load 也不能证明整个循环已经向量化。

Memory Profiler 可帮助看内存占用与对象关系，但不能单凭 GC Alloc 就证明组件的物理排布符合预期，更不能据此得到 cache miss 率。Frame Debugger 主要分析渲染事件，不用于证明 CPU 数据布局的收益。

**基准测试至少需要这些条件**

1. 固定算法和输入，校验输出一致；使用结果校验等方式避免循环被编译器消除。
2. 记录优化选项、目标指令集、线程数与设备状态；把分配初始化移出计时区，或明确它们属于被测成本。
3. 分别设计冷缓存与稳态复用场景，说明预热改变了什么。仅跑几次预热不能证明所有数据驻留。
4. 进行多轮测量，报告中位数、波动和计时精度；不能以“超过 1 ms”断言误差可忽略。
5. 回到真实帧中验证总耗时。搬运、索引和布局转换成本也应计入产品收益。

---

## DOTS Chunk 设计：机制与边界

[Entities 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-archetypes.html)的 Chunk 是 16 KiB 的存储块，按组件类型存放数组。它与上面的 AoSoA 思路相近，但并非七个 float 数组固定容纳 128 个对象：实际容量与组件大小、管理开销有关，组件内部的 `float3` 也不会自动拆成三个字段数组。

| 机制 | 可能带来的好处 | 不能由此推出什么 |
|---|---|---|
| 同类组件连续存储 | 减少无关组件的直接访问 | 所有字段利用率都是 100% |
| 分块处理 | 限制一次处理的范围，便于调度 | 整块永久驻留 L1 |
| Burst 编译循环 | 在满足条件时优化或向量化 | 一定生成某种 SIMD 指令 |
| 并行处理不同块 | 在依赖允许时分摊工作 | 每块固定一个线程、必然无共享争用 |

查找外部对象、动态缓冲溢出、结构变更和共享数据写入仍会产生额外成本。16 KiB 的存储事实不能证明其唯一设计动机是匹配 L1；性能也不能仅归因于布局。

---

## 实战决策树：选哪种布局

| 主要访问模式 | 首先比较的候选 | 保留判断所需的证据 |
|---|---|---|
| 批量扫描少数字段 | AoS 冷热拆分、SoA | 字段覆盖范围、编译结果、循环耗时 |
| 多个阶段复用同一小批数据 | SoA、不同块容量的 AoSoA | 工作集、跨阶段复用、分块开销 |
| 随机访问完整对象 | 紧凑 AoS | 实际触及行数、查询频度 |
| 可选组件多且经常增删 | 稀疏集合、Archetype 分组 | 查找、搬移、遍历的综合成本 |
| 当前路径占比很低 | 保持现有布局 | 重构收益是否值得维护代价 |

没有按“一百万 Entity”或某个 miss 率切换布局的通用阈值。miss 发生在哪一级、能否重叠、是否位于关键依赖链，都比一个孤立百分比更重要。

---

## 小结

AoS、SoA、AoSoA 分别改变字段相邻关系与处理粒度，没有一种布局保证缓存命中或稳定的加速倍数。先用字节与访问模型提出假设，再用正确性校验、耗时和硬件证据检验，最后把收益放回整个帧预算中判断。
