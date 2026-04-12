---
title: "DOD 行业案例 02｜id Tech 7 / DOOM Eternal：不用 ECS 框架，用 Job Graph 手工管数据流"
slug: "dod-industry-02-idtech7-doom"
date: "2026-03-28"
description: "DOOM Eternal 的引擎没有 ECS 框架，但它的渲染和游戏逻辑都是数据导向的——通过手工设计的 Job Graph 管理数据流，让 CPU 核心充分并行工作。这个案例证明 DOD 不等于必须有 ECS 框架。"
tags:
  - "id Tech 7"
  - "DOOM Eternal"
  - "Job Graph"
  - "数据导向"
  - "引擎架构"
  - "渲染"
series: "数据导向行业横向对比"
primary_series: "dod-industry"
series_role: "article"
series_order: 2
weight: 2120
---

## 核心论点先行

DOD（数据导向设计）是一种思维方式，ECS 框架是它的一种实现路径，但不是唯一路径。

DOOM Eternal 的 id Tech 7 引擎没有 ECS，没有 Archetype，没有 Component Query。但它的渲染管线和游戏逻辑都是彻底数据导向的——靠的是手工设计的 Job Graph。这个案例值得深究，因为它回答了一个关键问题：当你不用框架时，DOD 到底长什么样？

---

## id Tech 7 的背景

id Software 是自研引擎的坚守者。从 DOOM 1993 到 Quake 系列，再到现代的 id Tech 4、5、6、7，他们始终保持对底层的完全控制。

**DOOM 2016（id Tech 6）** 是一个重要转折点。id Software 在这一代开始大规模引入多线程并行化，将渲染、动画、游戏逻辑拆分到独立线程，告别了之前"一根主线程串到底"的架构。

**DOOM Eternal（id Tech 7）** 在 2016 的基础上进一步深化。在 GDC 2020 的演讲"DOOM Eternal: Engine and Rendering"中，id Software 工程师详细介绍了他们如何用 Job Graph 替代传统的线程同步模型，以及如何让渲染线程和游戏线程真正流水线化。这是本文的主要技术来源。

---

## Job Graph：依赖显式，调度并行

id Tech 7 没有任何类 ECS 框架，但它有一套完整的并行机制，核心是 **Job Graph**。

### 基本结构

Job Graph 的概念并不复杂：

- **节点（Node）** 是一个计算任务，比如"更新所有可见实体的 Transform"、"剔除场景中不可见的物体"、"生成 DrawCall 批次"。
- **边（Edge）** 是依赖关系，表示"这个任务必须等那个任务完成后才能开始"。
- **引擎运行时** 按照图的拓扑顺序调度——没有依赖关系的节点可以同时在不同 CPU 核心上执行。

这是一种非常朴素的有向无环图（DAG）调度模式，在图形学领域并不新鲜（RenderGraph 就是类似思路），但 id Tech 7 把它扩展到了整个游戏逻辑层。

### 和 DOTS Job System 的关键区别

Unity DOTS 的 Job System 也是基于依赖调度的，但两者的依赖管理方式截然不同：

- **DOTS Job System**：依赖通过 Safety System 自动检测。当两个 Job 访问同一块 NativeArray 时，Safety System 会检查读写冲突，并在 Schedule 时自动插入依赖关系。开发者只需声明数据访问意图（ReadOnly / ReadWrite），框架保证正确性。

- **id Tech 7 Job Graph**：依赖是手工声明的。工程师写 Job 的时候，需要自己知道"这个 Job 依赖哪些其他 Job"，并显式注册依赖边。引擎不会自动检测冲突，如果你漏了一条依赖边，结果就是数据竞争和难以复现的 bug。

手工声明的代价是维护成本，优势是精确控制和零框架开销。对于 id Software 这样深度优化的团队，他们更在意消除一切框架层的间接开销，愿意用工程纪律换取极致性能。

---

## 数据流设计：渲染和游戏线程的流水线化

id Tech 7 在线程架构上的最大突破是：**渲染线程和游戏线程真正流水线化**。

### 传统引擎的帧同步瓶颈

在很多传统引擎架构中，渲染线程和游戏线程虽然"分离"，但实际上存在帧同步点：

```
Frame N:  [游戏逻辑] ------> [渲染提交] ------> [GPU执行]
Frame N+1:                   等待 Frame N 完成  [游戏逻辑] ...
```

游戏线程更新完场景状态后，渲染线程才能开始提交这一帧的 DrawCall。这意味着两个线程是串行的，CPU 利用率大打折扣。

### id Tech 7 的流水线方案

id Tech 7 的数据流设计打破了这个瓶颈：

```
Frame N:   [游戏逻辑] --> [Job Graph 更新数据] --> [渲染线程提交 DrawCall] --> GPU
Frame N+1:               [游戏逻辑运行] <-- 与 Frame N 渲染线程并行 -->
```

渲染 Frame N 的提交工作和游戏 Frame N+1 的逻辑更新是**同时在跑的**。这要求数据层面有清晰的双缓冲或时间戳隔离——渲染线程读的是上一帧游戏逻辑写完的数据快照，游戏线程已经在向下一帧的缓冲写入。

这种设计是 DOD 的直接体现：数据流向是单向的、明确的，没有"渲染线程随时可能读到游戏线程正在修改的数据"这类问题，因为数据访问模式被显式设计进了架构。

### 渲染数据流的具体形态

从高层看，id Tech 7 的渲染数据流大致是：

1. **Visibility Job**：场景中哪些实体在视锥体内，哪些被遮挡？输出一个可见列表。
2. **Transform 更新 Job**：依赖 Visibility 结果，只更新可见实体的世界矩阵。
3. **DrawCall 批次生成 Job**：依赖 Transform 结果，生成 GPU 需要的渲染命令。
4. **渲染提交**：把批次送到 GPU，这一步主要是 GPU 驱动交互，开销相对固定。

每一步的输出都是下一步的输入，数据只向前流动，没有反向读取。这是 Job Graph 强制带来的架构纪律。

---

## Megatexture 和流式加载：同一个 DOD 思想

id Tech 系列还有一个著名技术：**Megatexture**（最早出现在 DOOM 3：ROE，后在 id Tech 5 中成熟）。

Megatexture 的核心是：把场景所有纹理合并成一张超大的虚拟纹理，运行时按摄像机视角和距离，**按需**从磁盘流式加载需要的 mip 级别数据，而不是预先把所有纹理全部加载到显存。

这本质上也是 DOD 思想的体现：

- **传统方案**：按"对象"组织数据。一个敌人有它的纹理，一块地形有它的纹理，加载一个对象就把它所有的数据一起加载。
- **Megatexture 方案**：按"使用模式"组织数据。当前帧需要哪块纹理区域，就加载哪块，数据布局完全由访问模式驱动。

到 DOOM Eternal，这套流式思想延伸到了更多资产类型。引擎在运行时持续预测"接下来最可能需要什么数据"，并提前发起异步 IO，保证计算单元（CPU/GPU）不因等待数据而空转。

---

## 和 ECS 方案的对比

| 维度 | ECS 框架（DOTS / Mass） | id Tech Job Graph |
|------|------------------------|------------------|
| 依赖管理 | 框架自动（Safety System）| 手工声明 |
| 框架开销 | 有（Archetype 管理、Query 调度）| 几乎零 |
| 维护成本 | 低（框架保证安全）| 高（手工管理正确性）|
| 适合场景 | 大量同构对象 | 复杂异构计算流程 |
| 工具链 | 丰富（Entities Hierarchy 等）| 自研 |
| 数据局部性 | 由 Archetype 保证 | 由工程师手工保证 |

这张表没有绝对的好坏，只有适合不适合。

DOOM Eternal 的计算流程相对固定：每帧要做的事情（可见性、Transform、DrawCall）是确定的，变化的只是数据。这种"流程固定、数据变化"的场景，Job Graph 是合适的。

ECS 的真正优势在于"大量同构对象"——当你有 10000 个敌人，每个都有 Health、Position、Velocity 组件，Archetype 能把这些数据打包成连续内存，让批量更新的 cache miss 降到最低。但当对象类型多样、行为差异大时，ECS 的 Archetype 碎片化问题会变严重：大量只有少数 Entity 的 Archetype，反而破坏了局部性。

---

## 工程启示

从 id Tech 7 这个案例，可以提炼几个对实际工程有用的判断：

**1. 渲染和物理引擎更适合 Job Graph**

渲染管线的计算步骤是确定的，依赖关系是静态的，适合用手工 DAG 来表达。Unity 自己的 SRP（Scriptable Render Pipeline）底层也在往 RenderGraph（一种特化的 Job Graph）方向走，和 DOTS ECS 是两套并行的机制。

**2. ECS 的边界在于对象同构性**

当你的游戏实体类型高度多样（战斗单位、地形、UI 元素、特效粒子都混在同一个 World 里），Archetype 碎片化会让 ECS 的局部性优势大打折扣，这时候手工管理内存布局反而可能更高效。

**3. DOD 的本质不变**

不管是 ECS 还是 Job Graph，DOD 的核心原则始终是同一个：**设计数据布局让 CPU 高效访问，不让逻辑耦合影响内存局部性**。框架是帮你达成这个目标的工具，不是目标本身。

id Tech 7 的选择在说：我们不需要框架帮我们管，我们自己管，管得更好。这是一种需要极强工程纪律支撑的立场，但它确实有效。

---

## 下一篇

前两篇分别看了两个极端：Unity DOTS 用框架自动化一切，id Tech 7 手工控制一切。下一篇换一个角度——看看**开源社区**是怎么做 ECS 的。

[DOD 行业案例 03｜Flecs 与 EnTT：C++ 开源 ECS 库的设计取舍](../dod-industry-03-flecs-entt/) 会分析两个在游戏社区广泛使用的轻量级 ECS 库，以及它们在性能、接口设计和实际使用场景上各自的取舍逻辑。
