---
date: "2026-03-26"
title: "游戏引擎架构地图 07｜为什么 DOTS 和 Mass 不能只算“一个模块”"
description: "区分模块交付形式与架构职责，用创作数据、实体组织、批量更新和专业表示的链路解释 DOTS 与 Mass 怎样跨越引擎边界。"
slug: "game-engine-architecture-07-dots-and-mass"
weight: 480
featured: false
tags:
  - Unity
  - Unreal Engine
  - DOTS
  - MassEntity
  - ECS
  - Architecture
series: "游戏引擎架构地图"
---

场景里的人物越来越多时，问题可能出在对象数据分散、更新工作重复，也可能出在动画、绘制或系统之间的数据同步。即使引入一套批处理框架，这些边界也不会自动消失：人物在哪里编辑，运行时保存哪些状态，谁更新位置，画面又从哪里读取结果，仍要逐一安排。

DOTS 与 Mass 值得放进架构地图，正是因为它们会同时改变其中几段。**包、插件和模块描述能力怎样组织与交付；数据导向框架的职责，要沿创作数据、实体组织、执行和表示之间的连接来理解。** 本文标题中的“不能只算一个模块”，强调的是不能只记住安装入口，就忽略这些连接。

> 资料范围：Unity 部分采用 Entities 与 Entities Graphics **1.0** 官方文档，本次核对页面标注为 **1.0.16**；Unreal 部分采用 **UE 5.6** 官方文档。本文是公开文档支持的架构映射与设计分析，没有执行引擎测试或性能测量。

## 先分清代码容器与架构职责

一个框架完全可以通过插件交付，再拆成多个代码模块。模块也可以访问世界接口、维护世界内的状态；这些行为是否合理，取决于具体依赖与生命周期协议，不能只凭“模块”这个名字决定。

Unreal 的模块文档讨论的是代码封装、构建依赖、加载阶段与目标选择。MassGameplay 文档则明确说明，Mass 的框架由承担不同功能的插件组成。两者可以同时成立。[UE 模块](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-modules?application_version=5.6)、[MassGameplay 概览](https://dev.epicgames.com/documentation/en-us/unreal-engine/overview-of-mass-gameplay-in-unreal-engine?application_version=5.6)

把这一区分收回 [00A 的职责与依赖模型]({{< relref "system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md" >}})，就能得到更准确的位置：

| 要回答的问题 | 观察对象 | 数据导向组织可能改变什么 |
| --- | --- | --- |
| 对象怎样存在 | 身份、组件组合、世界容器 | 用实体与数据集合承担一部分世界组织职责 |
| 工作怎样推进 | 查询、系统、处理阶段 | 按一批满足条件的数据组织更新 |
| 内容怎样进入运行时 | 创作描述、转换产物、生成请求 | 将适合编辑的数据变成适合执行的数据 |
| 结果怎样被消费 | 渲染、动画、交互等专业表示 | 建立实体状态与外部系统之间的桥接 |
| 代码怎样进入项目 | 包、插件、模块、依赖声明 | 按工具、运行时与专业桥接拆分代码 |

因此，可以将 DOTS/Mass 标为一块**跨职责的数据导向扩展区**。这里的“扩展”描述既有引擎引入另一套组织方式，不表示它必然是严格依赖图上新增的一层。ECS 本身就可以承担世界组织职责，也不要求先给每个实体配一个 GameObject 或 Actor，再把 ECS 叠在上面。

与 [02 的世界模型]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}}) 相接的是身份、组件和容器；与 [03 的运行时机制]({{< relref "system-design/game-engine-architecture-03-runtime-foundation.md" >}}) 相接的是执行和同步；与 [04 的专业系统]({{< relref "system-design/game-engine-architecture-04-professional-subsystems.md" >}}) 相接的是表示与消费协议。它们需要分别解释，不能用“更高性能”概括全部变化。

## 用一群移动人物建立公共模型

先不代入引擎名。假设人物的移动模拟只需要位置、速度和目标点，而美术还需要编辑外观、材质和动画配置。可以采用下面的**设计示意**：

```text
创作描述：初始位置、移动参数、外观引用
    ↓ 转换或生成
实体数据：身份、位置、速度、目标点
    ↓ 查询并批量更新
模拟结果：本轮位置、朝向和行为状态
    ↓ 按约定时点发布
专业表示：渲染实例、动画实例或交互代理
```

箭头表示数据产生与交接，不表示代码模块依赖，也不承诺每个环节都串行或跨线程执行。

这个拆法保留了四个不同问题。作者需要可编辑的描述；模拟需要参与计算的数据；更新过程需要明确的读写顺序；专业系统需要适合自己消费的表示。一个人物可以有这些不同表示，却不必在每种表示中复制整套玩法对象。

例如，设计上可以让远处人物只保留低成本外观，靠近时才连接可交互的代理。切换外观不应自动变成删除模拟身份；反过来，删除模拟实体，也需要通知仍持有其表示的系统。这里的“可以”是设计选项，下面再看具体引擎提供了哪些接点。

## Unity：创作数据与实体世界之间有明确的转换

Unity 将 Entities 定义为 DOTS 的一部分，提供 ECS 的数据导向实现。这说明 Entities 是具体包，而 DOTS 是更宽的技术组合；阅读架构时应继续追包提供的组织方式。[Entities 1.0 概览](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)

### 编辑对象怎样变成运行时数据

在 SubScene 工作流中，内容制作者使用 GameObject 与 authoring 组件描述内容，再通过 baking 生成实体与 ECS 组件。Baking 文档明确区分创作数据和运行时数据，并说明转换发生在 Editor 中。[SubScene](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/conversion-subscenes.html)、[Baking](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/baking-overview.html)

代入移动人物例子，设计者可以在 authoring 组件中编辑移动速度，由项目自己的 baker 将需要的值写入 ECS 组件。这个“人物速度”示例是应用设计；文档支持的是通过 baker 读取 authoring 组件并生成 ECS 数据的机制。它不会承诺任意 MonoBehaviour 的行为逻辑都能自动转换。[ECS 工作流](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/ecs-workflow-intro.html)

这里要分开两种连接：**制作期的 GameObject 描述转换为实体数据**，与**运行时实体和其他对象协作**。前者不意味着每次更新实体，都要先调用同名 GameObject 的脚本。运行时也可以创建实体，SubScene 是内容工作流中的一种入口。

### 实体在哪儿，谁处理它

`World` 包含实体，由 `EntityManager` 提供创建、修改和销毁等操作，并持有一组 systems。这里的 World 属于 ECS 自身的世界组织，不能直接等同于传统 Scene。[World concepts](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-worlds.html)

Systems 查询和修改 ECS 组件数据。需要并行且工作量合适时，可以调度 jobs；采用 ECS 并不自动意味着每个 system 都在工作线程运行。官方工作流也要求比较调度开销与收益。[Systems 与 jobs](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/ecs-workflow-intro.html#optimize-systems)

例如，项目可以让移动系统更新人物位置，再让后续表示消费该结果。但哪些系统先后执行、何时等待并行工作，仍属于执行协议，不能从“数据放在组件里”推断出来。

### 实体怎样接到画面

Entities Graphics 负责 ECS 与 Unity 既有渲染架构之间的桥接，并提供将编辑器中的渲染相关创作数据转成实体数据的能力。[Entities Graphics 1.0](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/overview.html)

因此，人物可以在制作阶段沿用熟悉的编辑入口，在运行时采用实体数据，并通过专门桥接进入渲染系统。这条链说明了哪些表示被连接，却不表示渲染器、资源上传或所有动画工作都由 ECS 的实体管理器接管。

## Unreal：实体模拟与世界内的专业表示协作

MassEntity 采用 Fragment 组合描述实体数据，按组合组织 Archetype，再通过 Processor 与查询处理批量数据。官方概览还描述了与 World 生命周期绑定的子系统组织，以及实体管理器对 Archetype 和实体操作的管理。[MassEntity 5.6 概览](https://dev.epicgames.com/documentation/unreal-engine/overview-of-mass-entity-in-unreal-engine?application_version=5.6)

可以由此确认：Mass 在 Unreal 的世界上下文中组织一套实体计算方式。不能进一步推成“每个实体都是 Actor”，也不能把插件能访问 World 当成它不再是插件的证据。

### 从内容配置走到批量更新

MassEntityConfig 资产可以声明 Traits，并生成实体模板；Processor 通过查询选择需要的 Fragment 数据批次。对移动人物来说，可以据此设计位置与速度的组合以及批量移动处理。前半段是文档描述的机制，具体人物规则由项目定义。[模板与处理流程](https://dev.epicgames.com/documentation/unreal-engine/overview-of-mass-entity-in-unreal-engine?application_version=5.6)

组件组合发生变化时，还会影响实体所属的 Archetype。官方描述了通过 MassCommandBuffer 请求组合变更、在当前处理批次结束时集中执行的方式。这与直接改某个位置数值不同，也说明批量访问必须配合结构变更协议。

### 表示变化可以独立于模拟组织

MassGameplay 将表示、生成、LOD 等能力接在 MassEntity 之上。其 Representation 子系统可以按配置采用不同精度的 Actor、ISM（实例化静态网格）或无表示，并协调表示之间的转换。[MassGameplay 的表示与 LOD](https://dev.epicgames.com/documentation/en-us/unreal-engine/overview-of-mass-gameplay-in-unreal-engine?application_version=5.6)

这给前面的人群设计提供了一个具体接点：模拟身份与屏幕上的 Actor/网格表示不是同一个概念。某个人物的显示形式变化时，需要管理的是表示关系及其状态交接，而不是据此认定实体身份必然改变。

同一份官方概览还分别讨论 Simulation LOD 与 Representation LOD。是否降低计算频率、是否切换显示形式，是不同决策；项目需要检查所采用的配置和处理路径。本文不把该能力扩展成任意角色动画、交互与复制都自动兼容的保证。

## 两边可以比较什么

把材料收回到公共问题，能得到下面这组对照。每一行都比较职责入口，不将左右两侧的类型视为等价物。

| 公共问题 | Unity 本文覆盖的路径 | Unreal 本文覆盖的路径 |
| --- | --- | --- |
| 作者怎样描述内容 | GameObject/authoring 组件与 SubScene | MassEntityConfig 与 Traits |
| 怎样建立运行数据 | Baking 生成实体数据，也可在运行时创建实体 | 模板与生成机制建立实体组合 |
| 谁组织实体状态 | ECS World、EntityManager、组件 | World 上下文中的 Mass 管理与 Fragment 组合 |
| 谁更新批量数据 | Systems 查询数据，按需要安排 jobs | Processors 通过查询处理数据批次 |
| 怎样进入专业表示 | Entities Graphics 桥接渲染 | MassGameplay 的表示、生成和 LOD 等系统 |

尤其不应把 `System` 与 `Query`、`Baking` 与 `Spawner` 排成一一对应关系：前一组把执行逻辑与数据选择混在一起，后一组把制作期转换与运行期生成混在一起。比较同一个问题，才知道两种实现究竟在哪一处相似、在哪一处需要不同的协议。

## 真正需要补齐的是交接协议

引入数据导向组织后，常见风险会集中到表示之间的连接。沿前面的人物例子，至少需要写清以下三项设计约定：

- **状态归属**：位置由模拟维护时，Actor、渲染实例或动画消费者怎样读取它；如果交互或物理会写回位置，应规定写回阶段，避免双方同时拥有最终决定权。
- **身份与寿命**：实体和专业表示怎样关联；回收、重建或延迟回调发生时，如何确认关联仍然有效，避免把旧结果应用到新对象。
- **更新时间**：消费者读取本轮还是上一轮结果；异步工作何时完成；结构变更与查询遍历怎样隔开。

这些是检查设计的公共问题，不是两台引擎自动提供相同答案的保证。数据布局、查询、调度和表示转换都有成本，最终收益要结合工作负载测量；本文的文档对照只能解释组织方式。

继续阅读时，可以先回到 [00A]({{< relref "system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md" >}}) 核对图中每根箭头的含义，再进入 [数据导向运行时系列]({{< relref "engine-toolchain/data-oriented-runtime-series-index.md" >}}) 深入布局、结构变化和调度；涉及实体与表现对象交接时，接着看 [表示层边界]({{< relref "engine-toolchain/data-oriented-runtime-06-why-simulation-and-representation-usually-cannot-fully-merge.md" >}})。

**理解 DOTS 与 Mass，要追它们怎样连接创作数据、实体状态、批量执行和专业表示；包、插件与模块是实现这些连接的组织形式。**
