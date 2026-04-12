---
title: "Unreal Mass 07｜Mass 实战案例拆解：City Sample 人群与 Mass Traffic 的架构决策"
slug: "mass-07-city-sample-case"
date: "2026-03-28"
description: "Epic 在 City Sample 中用 Mass Framework 驱动数万个行人和车辆。从这个官方案例反推 Mass 的设计动机，比读文档更有价值——它揭示了哪些设计决策是框架级别的约束，哪些是项目级别的选择。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "City Sample"
  - "Mass Traffic"
  - "ECS"
  - "实战"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 7
weight: 2070
---

City Sample 是 Epic 为配合 UE5 发布而制作的官方演示项目，也是 Mass Framework 最权威的参考实现。它不是一个"示例教程"，而是一个真实的生产级项目——在运行时同时驱动数千个行人和数百辆车，全部由 Mass 仿真，全部实时渲染在城市场景中。

从这个案例反推 Mass 的设计动机，比通读文档更有价值。文档告诉你"怎么用"，案例告诉你"为什么这样设计"。

## 规模数字：City Sample 到底跑多少 Entity

City Sample 的人群规模因平台而异，但在高端 PC（RTX 3080 级别）上，行人数量峰值在数千个同时存在，车辆由 Mass Traffic 系统驱动，数量在数百辆量级。这些数字听起来没有某些宣传材料里的"10万 NPC"那么震撼，但关键不在于原始数量——关键在于每个 Entity 都有完整的行为逻辑、路径跟随和避碰，而不是纯粹的视觉粒子。

CPU 预算方面，Mass 仿真层（不含渲染）在高端 PC 上的帧时间开销约 1~2ms。这个数字能够成立，依赖三个核心机制：LOD 分级更新、ZoneGraph 替代 NavMesh、以及 ISM 渲染替代 Actor 渲染。去掉其中任何一个，这个预算都会超出。

渲染策略是远近分离的：近处的 Entity 切换为完整 Actor（带 SkeletalMesh 和动画），视觉质量最高；中距离使用 Instanced Static Mesh（ISM）渲染，只有位移和朝向；远处继续用 ISM 但降低更新频率。这个分层渲染决策在 Representation Fragment 中体现，运行时可以动态切换。

## 人群系统的 Fragment 设计

City Sample 的行人 Entity 使用了一组精心筛选的 Fragment 组合。理解这组合，就能理解 Mass 的数据设计哲学。

**核心运动 Fragment：**

- `FTransformFragment`：位置、旋转，所有可见 Entity 必须有
- `FMassVelocityFragment`：当前速度向量，用于移动处理器积分位置
- `FAgentRadiusFragment`：避碰半径，Mass Avoidance Processor 读取此值计算排斥力
- `FZoneGraphPathFollowFragment`：当前的 ZoneGraph 路径跟随状态，包括路径 ID、当前路段进度、目标点

**精度控制 Fragment：**

- LOD Fragment（`FMassLODFragment` 系列）：存储当前 LOD 等级（LOD0/LOD1/LOD2/Off），由 LOD Processor 每帧根据距离和可见性更新
- `FMassRepresentationFragment`：存储当前使用的表现方式（Actor、ISM 高精度、ISM 低精度），驱动 Representation Processor 决定是否需要 Spawn/Despawn Actor

**状态 Fragment：**

- `FMassStateTreeFragment`：关联 StateTree 资产，存储当前状态机执行上下文（仅 LOD0 运行）
- `FMassCrowdObstacleFragment`：标记此 Entity 是否作为动态障碍物参与避碰计算

Fragment 的数量和类型直接决定了 Archetype 的内存布局。City Sample 刻意控制了 Fragment 数量——不是每个行人都带完整 AI 数据，LOD 越低的 Entity 参与的 Processor 越少，内存访问模式也更紧凑。

## ZoneGraph：为什么不用 NavMesh

NavMesh 是 Unreal 传统 AI 的路径系统，对单个角色效果很好。但对大量 Agent，它有一个根本性的问题：路径查询是 O(N) 的，每个 Agent 独立查询、独立分配路径节点、独立跟踪进度。数千个 Entity 同时查询 NavMesh，CPU 开销是线性增长的，而且 NavMesh 的数据结构不是 Cache-Friendly 的——不同 Agent 的路径数据散落在堆内存各处。

ZoneGraph 是 Epic 专为 Mass 设计的路径图系统，它的核心思路不同：路径是预定义的"车道"网络，Agent 不需要动态寻路，只需要选择一条合适的 Lane 并沿着走。这把路径逻辑从 O(N 次寻路) 变成了 O(N 次位置积分)——后者是可以用 SIMD 并行化的向量运算，前者不行。

`FZoneGraphPathFollowFragment` 存储的是 Lane ID 和进度，而不是一组路点。Processor 每帧只需要根据进度和速度推进位置，遇到 Lane 分叉时根据简单规则选择，不需要 A* 搜索。这个设计让路径跟随的计算代价接近常数，而不是随 Agent 数量线性增长。

和 Unity DOTS 对比：DOTS 没有内置的 ZoneGraph 等价物。如果用 DOTS 实现相同规模的人群，需要自己实现路径图系统，或者接受 NavMesh 的性能限制。这是 Unreal Mass 生态在大规模 NPC 场景下的明显优势之一。

## Mass Traffic：车辆系统的架构

Mass Traffic 是 City Sample 中驱动车辆的子系统，它和人群系统共享 Mass Framework 的基础设施，但 Fragment 设计完全不同，因为车辆的运动约束和人群完全不同。

**车辆核心 Fragment：**

- `FMassTrafficVehicleLaneFragment`：当前所在的交通车道 ID、车道内位置、车道方向
- `FMassTrafficVehicleSpeedFragment`：当前速度、目标速度、加减速参数
- `FMassTrafficNextVehicleFragment`：前车的 Entity Handle，用于计算跟车距离（这是一个 Entity 引用，不是空间查询）
- `FMassTrafficLightStateFragment`：当前关注的交通灯状态（红/绿/黄）

车辆不用 NavMesh，也不用 ZoneGraph——它们跑在预定义的交通流网络（Traffic Lanes）上，这个网络在关卡设计阶段由美术/设计师手工摆放，运行时是只读数据。车辆 Entity 只需要在 Lane 上积分前进，在 Lane 末端选择下一条 Lane，逻辑比行人更简单。

**交通灯信号的处理方式**是这套系统里一个值得注意的设计决策：交通灯状态改变时，通过 `Mass Signals` 系统广播信号，而不是让每辆车每帧轮询交通灯状态。这意味着交通灯变化只触发一次处理，响应逻辑在信号被处理时执行，不在状态改变帧执行。对于数百辆车，每帧轮询交通灯的开销看起来不大，但 Mass Signals 的解耦设计让"仿真事件"和"响应逻辑"在时间上分离——这个模式在更复杂的事件（死亡、碰撞）上尤其重要，避免了在同一帧内出现级联副作用。

## LOD 在 City Sample 里的实际分级

City Sample 的 LOD 分级不是简单的"更新频率减半"，每个 LOD 等级对应不同的行为精度：

**LOD0（近处，约30米以内）：**
完整 StateTree AI 运行，包括感知、决策、动画状态机。Entity 对应一个真实的 Actor（带 SkeletalMesh 和全精度动画），避碰使用完整的速度障碍算法（RVO），物理代理也是激活的。这个级别的 Entity 数量通常很少（玩家视野内的近距离角色），因此全力运行是可以接受的。

**LOD1（中距离，约30~80米）：**
StateTree 降频运行（不是每帧），避碰简化为基于半径的推斥而不是完整 RVO，动画切换为 ISM 动画（顶点动画贴图，VAT），不再是 SkeletalMesh。位置更新仍然每帧执行，但行为决策频率降低。

**LOD2（远处，约80~200米）：**
只更新 Transform，沿 ZoneGraph Lane 匀速前进，不做避碰，不做 AI 决策。视觉上是 ISM，更新频率进一步降低（每 N 帧更新一次）。

**Off（200米以外或视野外）：**
Entity 存在于内存中（Archetype 不变），但所有 Processor 都跳过它。当 LOD Processor 检测到它重新进入范围时，恢复更新并插值到当前应有的位置。

这个分级是 City Sample 能够维持 1~2ms CPU 预算的核心。LOD0 的 Entity 数量被严格控制在个位数到数十个，占总预算的大部分；LOD2 和 Off 的 Entity 占数量的多数，但几乎不消耗 CPU。

## 从案例反推的设计教训

拆解 City Sample 之后，可以总结出几条 Mass 项目级设计决策：

**LOD 不是优化手段，是可行性前提。** 没有分级 LOD，10000 个 NPC 全力运行在任何平台上都不可能达到实时帧率。Mass LOD 系统不是"性能调优时再考虑"的东西，它需要在 Fragment 设计阶段就规划好哪些数据是 LOD0 专用、哪些是所有级别共享的。

**ZoneGraph 而不是 NavMesh。** 在 Entity 数量超过数百之后，NavMesh 的 O(N) 查询成本和非 Cache-Friendly 的数据访问模式会成为瓶颈。如果你的项目场景允许预定义路径网络（城市道路、人行道、走廊），ZoneGraph 是正确的选择。NavMesh 保留给需要真正动态寻路的场景（玩家控制角色、需要绕过动态障碍物的重要 NPC）。

**Actor Pool 是必须的。** Spawn 和 Despawn 一个完整 Actor（包含组件、初始化逻辑）的代价在单帧内处理数十次时会产生明显的帧率抖动。City Sample 的 Representation Processor 维护一个 Actor Pool，LOD 切换时从 Pool 取出/归还而不是真正的销毁/重建。这是 Representation 层面最重要的实现细节。

**Mass Signals 解耦仿真事件和响应逻辑。** 死亡、碰撞、交通灯变化这类事件，不应该在事件发生的同一帧内级联触发所有响应。Mass Signals 提供了一个帧间的事件队列，让响应逻辑在下一帧（或指定帧）集中处理。这避免了同一帧内的数据竞争，也让响应逻辑可以批量处理同类事件。

## 如果用 DOTS 做同样的事

Unity DOTS 和 Unreal Mass 解决的是同一类问题，但生态配套的完整度不同。如果用 DOTS 复现 City Sample 同等规模的场景：

**渲染：** DOTS 的 Entities Graphics 提供 GPU Instancing，功能上类似于 ISM，但集成度更高——Entity 的 Transform 直接映射到 GPU Instance，不需要手动同步。这一点 DOTS 并不逊色。

**LOD：** DOTS 没有内置的 Mass 风格 LOD 系统。需要自己实现距离检测 Processor，使用 `IEnableableComponent` 在不同 LOD 等级之间切换 Component 的激活状态。可以实现，但需要额外工作量，且不像 Mass LOD 那样有清晰的层级定义。

**路径：** DOTS 没有 ZoneGraph 等价物。可以使用 NavMesh 查询的 Burst-compiled 版本（`NavMeshQuery`），但本质上仍然是 O(N) 的动态寻路。对于固定路径网络，需要自己实现 Lane 数据结构和跟随逻辑。

**事件：** DOTS 没有 Mass Signals 等价物。常见的替代方案是使用 `NativeQueue` 写入事件、在下一帧的 System 中消费，或者使用 Enableable Tag Component 标记状态变化。功能可以实现，但没有统一的框架约定。

**结论：** 在 Unreal 生态中，Mass 的配套工具（ZoneGraph、StateTree、Niagara 粒子群集成）让大规模 NPC 比在 Unity DOTS 中更快落地，主要优势在于路径系统和 LOD 框架的完整度。DOTS 在渲染和纯计算性能上并不弱，但围绕大规模 NPC 的"工具链完整性"是 Unreal Mass 当前的实际优势。

---

## Mass 系列总结：框架定位的本质差异

Mass-01 到 Mass-07 覆盖了从 ECS 基础概念到 City Sample 实战的完整路径。回头看这七篇，Mass 和 Unity DOTS 的本质定位差异可以用一句话概括：

**DOTS 是一套"高性能 ECS 运行时"，Mass 是一套"大规模 NPC 仿真解决方案"。**

DOTS 给你一个高效的 ECS 基础设施，然后让你在上面构建自己的游戏逻辑，它的假设是"你知道你要构建什么，我给你工具"。Mass 给你一套已经为 NPC 仿真预设好结构的框架，ZoneGraph、StateTree、LOD、Representation、Signals 都是框架的一部分，它的假设是"你要做大规模 NPC，我给你一整套约定和配套系统"。

这意味着 Mass 的上手成本更高（需要理解更多预设的约定），但在大规模 NPC 场景下的落地速度更快——因为 Epic 已经在 City Sample 里验证了这套框架的工程可行性，你不需要重新发明 ZoneGraph 或 LOD 分级策略。

如果你的项目需要大规模 NPC 并且使用 Unreal，Mass 是值得认真学习的框架。如果你需要的是高性能的游戏逻辑计算（物理、程序生成、大规模战斗数值）而不是 NPC 仿真，DOTS 的通用性可能更适合。两者没有优劣之分，只有场景适配度的差异。
