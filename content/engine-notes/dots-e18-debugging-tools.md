---
title: "Unity DOTS E18｜DOTS 调试工具全景：Entities Hierarchy、Chunk Utilization、Job Debugger、Burst Inspector"
slug: "dots-e18-debugging-tools"
date: "2026-03-28"
description: "光会写 DOTS 代码不够，能读懂 Profiler 里的 ECS 数据才算真正掌握。本篇讲清楚 Entity Hierarchy、Archetype Window、Chunk Utilization、Job Debugger 和 Burst Inspector 这五个工具的使用场景和读图方法。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "调试"
  - "Profiler"
  - "Burst Inspector"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 18
weight: 1980
---

DOTS 系列走到第 18 篇，代码能跑起来只是起点。真正的工程能力体现在：当性能不对劲、内存莫名变高、Job 报了安全错，你能在 5 分钟内定位到根因——而不是靠猜。

本篇把 DOTS 调试工具链梳理一遍，每个工具讲清楚打开路径、能看到什么、以及具体能解决哪类实际问题。

---

## 一、Entities Hierarchy 窗口

**打开路径：Window → Entities → Hierarchy**

这是 DOTS 调试的起点。进入 Play Mode 后，Hierarchy 窗口会列出当前所有 World 里的 Entity，按 Archetype 分组显示。点开任意 Entity，Inspector 面板会展示它挂载的所有 Component 及其当前数值——和 GameObject 的 Inspector 逻辑一致，但对象是 ECS 里的数据。

**实际用途一：验证 Baker 输出**

SubScene 烘焙完成后，最常见的问题是"Component 挂了但值不对"或者"Baker 根本没有生成这个 Component"。在 Entities Hierarchy 里选中对应 Entity，直接对比 Inspector 里的数据和你在 Baker 里 `AddComponent` 的逻辑。如果 Component 不见了，说明 Baker 逻辑有条件判断没走到，或者 `GetEntity` 用了错误的 TransformUsageFlags。

**实际用途二：验证 Structural Change 后的归属**

系统执行 `EntityManager.AddComponent` 或 `EntityCommandBuffer` 回放之后，Entity 会被迁移到新 Archetype。在 Hierarchy 里刷新，可以直接看到 Entity 是否出现在新 Archetype 分组下，而不是去日志里猜。

---

## 二、Archetype Window

**打开路径：Window → Entities → Archetypes**

Archetype Window 是内存问题的第一诊断台。它列出当前所有 Archetype 的：

- Component 类型组合（展开可见完整列表）
- 拥有该 Archetype 的 Entity 数量
- 分配的 Chunk 数量
- 已使用内存和总分配内存

**读图方法：找碎片化 Archetype**

理想状态是：Entity 数量多、Chunk 数量少、每 Chunk 利用率接近 100%。危险信号是：Chunk 数量多但 Entity 数量少，这意味着大量 Chunk 只填了几个 Entity，剩余空间全部浪费。

最典型的成因是 `ISharedComponentData` 值过多。每一种不同的 SharedComponent 值都会强制独占一个 Chunk，哪怕这个值只对应 1 个 Entity。如果你用 SharedComponent 来区分"渲染层级"或"阵营 ID"，而这些值有几十种，Chunk 碎片化就是必然的。

在 Archetype Window 里，这个问题一眼可见：一个 Archetype 显示 60 个 Chunk，但 Entity 数只有 70，就说明平均每个 Chunk 只装了约 1 个 Entity，利用率极低。

---

## 三、Chunk Utilization（内存利用率）

Chunk 利用率不是一个独立窗口，而是 Archetype Window 里的关键指标：已使用内存 / 总分配内存的百分比。

- **100%**：每个 Chunk 都填满了，缓存命中率最高，遍历效率最好。
- **50%~80%**：合理范围，正常的 Entity 增删会造成一定碎片。
- **低于 50%**：需要关注。遍历时 CPU 需要加载的缓存行数是满载情况的两倍以上，System 耗时会虚高。

**改善方法：**

1. 减少 SharedComponent 的枚举值数量。能用整数字段替代 SharedComponent 的，优先用字段。
2. 用 `IEnableableComponent` 替代"添加/移除 Tag Component"的模式。Enableable Component 不改变 Archetype，Entity 保持在同一 Chunk 里，利用率不受影响。
3. 批量销毁不活跃 Entity 后，可以调用 `EntityManager.UniversalQuery` 配合 Chunk 迭代做一次手动整理，但通常改掉 SharedComponent 滥用才是根治方案。

---

## 四、Job Debugger

**打开路径：Jobs → Debugger**（也可以在 Profiler 窗口的 Jobs 选项卡里查看）

Job Debugger 展示 Job 的调度时间线和依赖关系图。每个 Job 显示：提交时间、开始执行时间、完成时间，以及它依赖哪些上游 Job、被哪些下游 Job 等待。

**实际用途一：定位主线程卡顿原因**

Profiler 里看到主线程有一段明显的 `JobHandle.Complete` 等待，说明某个 Job 还没跑完，主线程就来取结果了。切到 Job Debugger，找到那个 Complete 时刻对应的 Job，看它的等待链：是因为依赖了一个跑得慢的上游 Job？还是因为它本身提交太晚，Worker 线程刚刚开始执行？

两种情况的解法不同。前者要优化上游 Job 的性能；后者要把 `Schedule` 调用移到更早的 System 里，留足 Worker 线程时间。

**实际用途二：读 Safety Check 原始错误**

Job Safety Check 报错时，Console 里的信息经常被截断，看不全依赖链。Job Debugger 里可以看到完整的冲突信息：哪两个 Job 在同一个 NativeContainer 上产生了读写竞争，调度顺序是什么。根据这些信息，才能准确判断是要加 `[ReadOnly]`、调整 `dependency` 传递，还是拆分 Container。

---

## 五、Burst Inspector

**打开路径：Jobs → Burst → Open Inspector**

Burst Inspector 让你直接看编译后的汇编代码，是验证 SIMD 向量化是否成功的唯一可靠手段。左侧列出所有已编译的 Burst Job，选中后右侧展示对应的汇编输出，可以对照 C# 源码逐段查看。

**读汇编的核心方法：看指令前缀**

| 指令前缀 | 架构 | 含义 |
|---------|------|------|
| `vmovaps`、`vaddps`、`vmulps` | x86 AVX/AVX2 | 256-bit SIMD，一次处理 8 个 float，向量化成功 |
| `addps`、`mulps`、`movaps` | x86 SSE2/SSE4 | 128-bit SIMD，一次处理 4 个 float，部分向量化 |
| `addss`、`movss`、`mulss` | x86 标量 | 每次处理 1 个 float，向量化失败 |
| `vadd.f32`、`vld1.32` | ARM NEON | SIMD 向量化成功 |

如果一个本应受益于 SIMD 的数值循环输出了大量 `addss`/`movss`，常见原因有：循环体内有条件分支、访问了非对齐内存、或者使用了 Burst 不支持的 API（如 `Mathf` 部分方法）。

**对比优化效果：切换编译目标**

Inspector 顶部可以选择编译目标（SSE2、SSE4、AVX2、NEON 等）。优化前后对比：在同一个目标下，看修改后汇编里向量指令的密度是否提升、标量指令是否减少。这比在真机上反复跑 Profiler 要快得多。

---

## 六、Unity Profiler 里的 ECS 数据

Profiler 的 CPU 选项卡对 ECS 有专门的层级展示：

**识别三种常见模式：**

**模式一：主线程大量等待 Job**
表现为主线程出现长段 `JobHandle.Complete`，Worker 线程还在跑。根因是 Job 提交太晚——通常是某个 System 在帧末才 Schedule，而下一帧开头另一个 System 就来 Complete。解法：把 Schedule 提前到更早的 SystemGroup，让 Job 在帧内有足够的并行时间。

**模式二：System 时间突然变长**
ECS System 的执行时间在某帧出现峰值，而 Job 本身的逻辑没变。这通常是 Structural Change 触发了大规模 Chunk 重组：大量 Entity 同时 Add/Remove Component，导致数据在 Chunk 间搬迁。Profiler 里会看到 `EntityManager` 相关的调用时间激增。解法是改用 Enableable Component，或者把 ECB 回放分散到多帧。

**模式三：GC Alloc 出现在 ECS System 里**
ECS System 里出现 GC Alloc，基本说明有 Managed 代码混入：`string` 拼接、LINQ 查询、装箱（把 struct 赋给 `object`）、或者调用了返回 `List<T>` 的 API。Burst Job 内部不允许 Managed 代码，但 `OnUpdate` 里如果有调度外的逻辑，就容易带入这类问题。

---

## 七、常见调试场景速查

| 问题 | 用哪个工具 | 看什么 |
|------|-----------|--------|
| Baker 没有生成正确 Component | Entities Hierarchy | Entity 的 Component 列表和数值 |
| 内存占用异常高 | Archetype Window | Chunk 数量和利用率百分比 |
| SharedComponent 导致碎片化 | Archetype Window | 同一 Archetype 下 Chunk 多但 Entity 少 |
| Job 安全报错信息不完整 | Job Debugger | 依赖图和冲突原始信息 |
| SIMD 向量化是否成功 | Burst Inspector | 汇编指令前缀（v 开头为向量化） |
| ECS System 卡顿峰值 | Unity Profiler | JobHandle.Complete 等待时间 |
| GC Alloc 混入 ECS | Unity Profiler | GC Alloc 列，定位到具体 System |

---

## 结语：DOTS-E01 到 E18，一条完整的路径

至此，Unity DOTS 工程实践系列的 18 篇文章全部完成。

这 18 篇构成了一条从零到可交付的完整学习路径：

- **E01~E03**：ECS 三要素——Entity、Component、System 的基本概念和第一行代码
- **E04~E06**：Query、Aspect、SystemGroup 的组织模式
- **E07~E09**：Job System 与 Burst 编译，理解并行调度的底层逻辑
- **E10~E12**：SubScene、Baker、Blob Asset，处理数据的序列化和烘焙边界
- **E13~E15**：Structural Change、ECB、Enableable Component，管理 Entity 生命周期
- **E16~E17**：Chunk 内存布局与 NativeContainer 进阶，工程性能的上限在这里
- **E18**：调试工具全景，能读懂 Profiler 才算真正掌握

从第一个 `IComponentData` 到能够在 Profiler 里定位 Chunk 碎片、看懂 Burst 汇编，这条路走下来，DOTS 的工程边界就清晰了：它不是 GameObject 的替代品，而是在特定场景下——大量同构 Entity、高频数值运算、需要精确控制内存布局——能够带来质变的架构选择。知道它能做什么、不能做什么、以及出了问题去哪里找，才是工程师真正需要的判断力。
