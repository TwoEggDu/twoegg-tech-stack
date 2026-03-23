# 游戏引擎架构地图 07｜证据卡：为什么 DOTS 和 Mass 不能只算“一个模块”

## 本卡用途

- 对应文章：`07`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`DOTS 和 Mass 在整张引擎架构图上到底站哪，为什么它们不是普通功能模块。`
- 这篇不展开：`00 总论里的六层总地图细节`
- 这篇不展开：`02 里 GameObject / Actor 默认对象世界的完整对照`
- 这篇不展开：`03 里 GC、反射、任务系统的内部机制`
- 这篇不展开：`04/05/06 里渲染、资源发布链、平台抽象的完整内部实现`
- 本篇允许做的事：`只锁定 Unity 的 DOTS / Entities / baking / worlds 与 Unreal 的 MassEntity / MassGameplay / entity manager / query / world subsystem 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册与包文档，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方把 Entities 写成 DOTS 的数据导向 ECS 架构

- Unity 入口：
  - [Entities overview](https://docs.unity.cn/Packages/com.unity.entities%401.3/manual/index.html)
  - [ECS workflow tutorial](https://docs.unity.cn/Packages/com.unity.entities%401.2/manual/ecs-workflow-tutorial.html)
- 可直接证明的事实：
  - Unity 官方明确 `Entities package` 是 `DOTS` 的一部分。
  - Unity 官方明确 `Entities` 提供的是 `data-oriented implementation of the Entity Component System (ECS) architecture`。
  - Unity 官方把 ECS workflow 说明为一组协同工作的技术与包，用来交付数据导向的开发方式，而不是某个单点功能。
- 暂定判断：
  - `DOTS` 的起点不是“再加一个功能包”，而是把对象组织和执行方式切到 `ECS + data-oriented` 这一套新模型上。

### 2. Unity ECS 要求用 SubScene 与 Baking，把 GameObject 作者数据改写成实体运行时数据

- Unity 入口：
  - [Subscenes overview](https://docs.unity.cn/Packages/com.unity.entities%401.2/manual/conversion-subscenes.html)
  - [Baking overview](https://docs.unity.cn/Packages/com.unity.entities%401.1/manual/baking-overview.html)
- 可直接证明的事实：
  - Unity 官方明确 `ECS uses subscenes instead of scenes`，原因是 `Unity's core scene system is incompatible with ECS`。
  - Unity 官方明确可以把 `GameObjects` 与 `MonoBehaviour` 放进 `SubScene`，再由 baking 转成 `entities` 与 `ECS components`。
  - Unity 官方明确 baking 会把 Unity Editor 中的 `GameObject authoring data` 转成写入 `Entity Scenes` 的 `runtime data`。
  - Unity 官方明确 baking 只发生在 Editor，不在游戏运行时执行，并把它类比为 asset importing。
- 暂定判断：
  - `DOTS` 不只是运行时 API 扩展，它直接改写了默认对象世界的承载方式，并且跨进了内容生产与数据导入链路。

### 3. Unity DOTS 还把 World、System、Jobs、Burst 与渲染桥接一起带进来

- Unity 入口：
  - [World concepts](https://docs.unity.cn/Packages/com.unity.entities%401.1/manual/concepts-worlds.html)
  - [Understand the ECS workflow](https://docs.unity.cn/Packages/com.unity.entities%401.0/manual/ecs-workflow-intro.html)
  - [Entities Graphics overview](https://docs.unity.cn/Packages/com.unity.entities.graphics%401.2/manual/overview.html)
  - [Baking systems overview](https://docs.unity.cn/Packages/com.unity.entities%401.3/manual/baking-baking-systems-overview.html)
- 可直接证明的事实：
  - Unity 官方明确 `World` 拥有 `EntityManager` 与一组 `systems`，进入 Play mode 时默认会创建 `default world` 并把系统加进去。
  - Unity 官方明确系统会查询、变换 ECS 数据，也可以创建和销毁实体；在合适场景下，最佳实践是创建 `Burst-compatible jobs` 并并行调度。
  - Unity 官方明确 `Entities Graphics` 是 `ECS for Unity` 与现有渲染架构之间的桥梁，允许用 `ECS instead of GameObjects`，并把渲染相关数据在 baking 时转成实体组件。
  - Unity 官方明确 baking systems 本身就是 systems，因此可以使用 `jobs` 与 `Burst compilation` 处理重型加工。
- 暂定判断：
  - `DOTS` 不只碰世界模型，也碰运行时执行骨架，并且需要和现有渲染子系统做桥接，所以更像一层数据导向扩展区，而不是渲染模块或普通 package。

### 4. Unreal 官方把 MassEntity 写成 data-oriented framework，并把默认管理器挂到 UWorld 上

- Unreal 入口：
  - [Mass Entity in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/mass-entity-in-unreal-engine?application_version=5.6)
  - [MassEntity API module](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/MassEntity)
  - [UMassSubsystemBase](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/MassEntity/UMassSubsystemBase)
- 可直接证明的事实：
  - Unreal 官方明确 `MassEntity is a gameplay-focused framework for data-oriented calculations`。
  - Unreal 官方 `MassEntity` API 模块明确 `UMassEntitySubsystem` 的职责是为某个 `UWorld` 承载默认的 `FMassEntityManager`。
  - Unreal 官方明确 `UMassSubsystemBase` 是所有 `Mass-related UWorldSubsystem` 的公共基类，并列出 `Representation`、`LOD`、`Replication`、`Simulation`、`Spawner`、`StateTree` 等派生子系统。
- 暂定判断：
  - `Mass` 不是离开世界容器独立运行的小插件，而是直接挂接在 `UWorld` 级别的世界与子系统组织里。

### 5. Unreal MassEntity 用 archetype、fragment、query、command buffer 组织数据与执行

- Unreal 入口：
  - [FMassEntityManager](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/MassEntity/FMassEntityManager)
  - [FMassEntityQuery](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/MassEntity/FMassEntityQuery)
- 可直接证明的事实：
  - Unreal 官方明确 `FMassEntityManager` 负责承载 entities 并管理 archetypes，实体以 `chunked array` 形式存储，每个实体会被分配到当前片段组合对应的 archetype。
  - Unreal 官方明确 `FMassEntityManager` 提供实体创建与操作 API，而多数实体操作通过 `command buffer` 执行。
  - Unreal 官方明确 `FMassEntityQuery` 用来在满足要求的有效 archetypes 集合上触发计算，并把 fragment 与 subsystem requirement 作为查询约束的一部分。
- 暂定判断：
  - `Mass` 在 Unreal 里不只是多一套 gameplay 名词，而是在数据布局、批处理查询和执行时机上引入另一套运行时组织方式。

### 6. Unreal 官方把 MassGameplay 写成对 MassEntity 的直接扩展，覆盖表示、生成、LOD、复制与 StateTree

- Unreal 入口：
  - [Overview of Mass Gameplay in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/overview-of-mass-gameplay-in-unreal-engine?application_version=5.6)
  - [MassEntity API module](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/MassEntity)
- 可直接证明的事实：
  - Unreal 官方明确 `Mass Gameplay plugin directly derives from the Mass Entity plugin`。
  - Unreal 官方明确 `MassGameplay` 包含 `world representation`、`spawning`、`LOD`、`replication`、`StateTree` 等功能。
  - Unreal 官方明确 `Mass Representation`、`Mass Spawner`、`Mass LOD`、`Mass Replication`、`Mass StateTree` 都是围绕实体批量表示、更新与同步展开的子系统，而不是单个对象脚本功能。
- 暂定判断：
  - `MassGameplay` 的官方拆法已经说明它不是“再加一个模块”，而是围绕数据导向实体批量模拟，对世界表示、执行调度与网络同步一起动刀的扩展层。

## 本轮可以安全落下的事实

- `事实`：Unity 官方把 `Entities` 明确归入 `DOTS`，并把它定义成数据导向的 `ECS architecture`。
- `事实`：Unity 官方明确 `core scene system` 与 `ECS` 不兼容，因此要通过 `SubScene + baking` 把 `GameObject / MonoBehaviour` 作者数据转成 `Entity Scene` 运行时数据。
- `事实`：Unity 官方明确 `World + systems + Burst-compatible jobs` 是 ECS workflow 的一部分，`Entities Graphics` 负责把 ECS 接到现有渲染架构上。
- `事实`：Unreal 官方明确 `MassEntity` 是 `gameplay-focused framework for data-oriented calculations`。
- `事实`：Unreal 官方明确 `UMassEntitySubsystem` 为给定 `UWorld` 承载默认 `FMassEntityManager`，`Mass` 相关能力以 `UWorldSubsystem` 方式组织。
- `事实`：Unreal 官方明确 `FMassEntityManager` 与 `FMassEntityQuery` 围绕 `archetype`、`fragment`、`command buffer`、`query requirements` 组织实体计算。
- `事实`：Unreal 官方明确 `MassGameplay` 直接建立在 `MassEntity` 之上，并扩到 `representation / spawning / LOD / replication / StateTree`。
- `事实`：`docs/engine-source-roots.md` 当前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `07` 可以把 `DOTS` 定位为 `对 Unity 默认 GameObject 世界与执行骨架的 data-oriented 重构/特区化扩展`，而不是普通功能包。
- `判断`：文章 `07` 可以把 `Mass` 定位为 `对 Unreal 默认 Actor 世界与执行组织的 data-oriented framework extension`，而不是普通 gameplay 模块。
- `判断`：`DOTS / Mass` 最稳的地图站位不是专业子系统层，也不是单纯运行时底座层，而是 `建立在世界模型层与运行时底座层之上的数据导向扩展层`。
- `判断`：这一层之所以单列，不是因为它们“更高级”，而是因为它们都在回答 `高规模对象如何表示、批处理、调度、同步` 这一类跨层问题。
- `判断`：当前最安全的写法不是把 `DOTS` 与 `Mass` 当成一一等价物，而是把它们收回为两台引擎各自对默认对象世界和执行方式做的数据导向改写。

## 本卡暂不支持的强结论

- 不支持：`DOTS` 已经完全取代 `GameObject / MonoBehaviour` 成为 Unity 的唯一主路径
- 不支持：`Mass` 已经完全取代 `Actor / Gameplay Framework` 成为 Unreal 的唯一主路径
- 不支持：`DOTS` 与 `Mass` 的内部调度、存储、同步细节已经完成源码级一一对照
- 不支持：`DOTS` 与 `Mass` 可以直接做严格的一一语义映射
- 不支持：`DOTS / Mass` 的性能收益已经可以脱离场景规模、内容结构和工程边界做强判断
- 不支持：`DOTS / Mass` 的层边界已经精确压实到不需要后续提纲和首稿收束

## 下一次最合适的增量

- 基于本卡给 `07` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么不是表面 API 差异
  7. 常见误解
  8. 我的结论
