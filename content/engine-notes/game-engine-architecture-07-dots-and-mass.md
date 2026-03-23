---
title: "游戏引擎架构地图 07｜为什么 DOTS 和 Mass 不能只算“一个模块”"
description: "基于 Unity 与 Unreal 官方资料，把 DOTS 与 Mass 收回到数据导向扩展层，解释它们为什么不是普通功能包或插件。"
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

> 这篇只回答一个问题：`DOTS 和 Mass 在整张引擎架构地图里到底站哪一层，为什么它们不该被当成普通功能模块。`  
> 它不重讲 `00` 的六层总地图，不重做 `02` 的默认对象世界全量比较，也不把 `03` 的 GC、反射、任务系统内部机制重新展开，只先把数据导向扩展层压清楚。

先说明这篇的证据边界。

当前这版首稿只使用官方文档证据。`docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径都还不是 `READY`，所以下文里凡是“事实”，都只落在官方资料明确写出来的范围；凡是“判断”，都会明确写成工程判断，而不是伪装成源码结论。

## 这篇要回答什么

很多人第一次看到 `DOTS` 或 `Mass` 时，最自然的理解往往是：

- Unity 又多了一组高性能 package
- Unreal 又多了一个面向 AI 或 gameplay 的插件

这种理解不能说完全错，但它太容易把问题压扁。

因为只要把 `DOTS / Mass` 写成“又一个模块”，你就会自动漏掉几件更大的事：

- 它们到底有没有碰默认对象世界
- 它们到底有没有碰正式执行骨架
- 它们到底有没有改写作者数据进入运行时的方式
- 它们到底是不是只服务一个功能点，还是已经跨到表示、调度、同步这类更宽的边界

所以这篇真正要回答的问题不是：

`DOTS 和 Mass 分别提供了哪些 API。`

也不是：

`它们谁更先进，谁更适合大项目。`

而是：

`当引擎开始为高规模对象、批处理更新和数据导向表示专门改写默认对象世界与执行组织时，这种能力在整张架构图上应该放在哪里。`

这个问题值得单独拆出来，是因为如果站位一开始就错了，后面几种写法都会一起跑偏：

- 会把 `DOTS` 写成 ECS 教程
- 会把 `Mass` 写成 gameplay 插件导览
- 会把二者写成性能宣传文
- 会把 `02` 的世界模型、`03` 的运行时底座、`04/05/06` 的系统边界全混在一篇里

从官方资料能直接成立的事实是：

- Unity 官方明确把 `Entities` 写成 `DOTS` 的一部分，并把它定义成 data-oriented 的 `ECS architecture`
- Unreal 官方明确把 `MassEntity` 写成 `gameplay-focused framework for data-oriented calculations`

基于这些事实，我在这篇里先给出的判断是：

`这篇真正要解释的不是两个名词各自有多少功能，而是它们为什么都已经超出了“普通模块”的站位。`

## 这一层负责什么

如果要把这层先压成一句更工程化的话，它负责回答的是：

`当默认对象世界在规模、访问模式和执行成本上不再够用时，引擎怎样长出一套新的数据组织、批处理执行和系统桥接方式。`

也就是说，这一层首先关心的不是“多加一个功能”，而是下面四件事：

1. 默认对象世界怎样被重新表示。
2. 实体数据怎样被批处理查询、更新和调度。
3. 作者数据怎样进入这种新世界，或者这种新世界怎样挂回原有世界容器。
4. 这种扩展怎样跨到表示、生成、LOD、复制等外围系统，而不是停在单点 API。

如果先把两边压成一张最小对照表，会更容易看清问题：

| 对照维度 | Unity DOTS | Unreal Mass | 这层真正负责什么 |
| --- | --- | --- | --- |
| 默认对象世界的关系 | `SubScene + Baking` 说明它会改写 `GameObject` 作者数据进入运行时的路径 | `UWorld + UMassEntitySubsystem` 说明它直接挂在默认世界容器上 | 二者都不是脱离默认世界独立漂浮的功能点 |
| 世界承载 | `Entity Scene + World` | `UWorld + FMassEntityManager` | 二者都直接碰世界容器与对象承载方式 |
| 运行时核心组织 | `Entity / Component / System / Jobs` | `Entity / Fragment / Archetype / Query / Command Buffer` | 二者都在改写批处理执行结构 |
| 跨层桥接 | `Baking` 触及作者数据导入，`Entities Graphics` 连接既有渲染架构 | `MassGameplay` 扩到 `representation / spawning / LOD / replication / StateTree` | 二者都跨出“单一模块”边界 |
| 工程判断 | 数据导向重构/扩展层 | 数据导向框架扩展层 | 地图位置相近，但不做一一等价 |

从官方文档能直接成立的事实是：

- Unity 这边，`SubScene`、`baking`、`World`、`systems`、`Entities Graphics` 都被正式写进同一条 ECS / DOTS 语境
- Unreal 这边，`UWorldSubsystem`、`EntityManager`、`archetype`、`query`、`MassGameplay` 都被正式写进同一条 Mass 语境

基于这些事实，我在这里先给出的判断是：

`DOTS / Mass` 之所以要单独放到“数据导向扩展层”，不是因为它们更炫，而是因为它们都同时碰到了世界模型层和运行时底座层的连接方式。`

更直白一点说：

- 世界模型层回答“对象默认怎样存在”
- 运行时底座层回答“执行骨架怎样站住”
- 数据导向扩展层回答“当默认对象组织和默认执行方式不再够用时，怎样再开出一条不同的数据导向通道”

所以这层真正负责的是：

`把高规模对象和批处理执行从默认对象世界里重新拎出来，用另一套更数据导向的组织方式再建一次。`

## 这一层不负责什么

边界先压清，不然后面一定会串题。

这篇明确不做下面几件事：

- 不把 `DOTS / Mass` 写成完整入门教程
- 不把 `Entity / System / Fragment / Query` 写成术语百科
- 不重做 `02` 里 `Scene / World`、`GameObject / Actor` 的全量比较
- 不重做 `03` 里 `GC / reflection / Job System / Task Graph` 的内部机制分析
- 不把 `Entities Graphics`、`Mass Representation` 展开成渲染或表现系统文章
- 不把 `baking`、`spawning`、`replication` 展开成资产发布链或网络同步教程
- 不做 `DOTS` 和 `Mass` 的性能优劣判断
- 不宣称 `DOTS` 已完全取代 `GameObject / MonoBehaviour`
- 不宣称 `Mass` 已完全取代 `Actor / Gameplay Framework`
- 不主张二者可以做严格的一一语义映射

为什么必须克制？

因为只要把默认对象世界、执行底座、渲染桥接、网络同步、平台抽象和性能裁判一起拖进来，这篇就会从“架构站位文”直接塌成“教程 + 百科 + 对比文”。

而这篇真正要站住的，只是一个更上游的问题：

`DOTS / Mass 在地图里到底站哪。`

从证据边界上说，本篇也必须继续保持一个限制：

- 当前没有本地 `READY` 的 Unity / Unreal 源码根路径
- 因此本篇只能写“官方资料明确写了什么”
- 然后在这个基础上给出“最稳的工程判断”

基于这些边界，我在这里先做的判断是：

`如果一篇文章开始顺手解释 ECS 语法、Mass API、性能结论和所有外围系统，那它就已经偏离了这篇真正的问题。`

## Unity 怎么做

先看 Unity 官方文档把 `DOTS` 这条链是怎么串起来的。

### `Entities` 不是单点功能，而是 DOTS 的正式入口之一

从 Unity 的 `Entities overview` 和 ECS workflow 相关文档里，能直接落下几件事实：

- Unity 官方明确 `Entities package` 是 `DOTS` 的一部分
- Unity 官方明确 `Entities` 提供的是 data-oriented implementation of the `Entity Component System (ECS) architecture`
- Unity 官方把 ECS workflow 说明成一组协同工作的技术与包，而不是一个孤立功能

这组事实很关键，因为它说明 `DOTS` 的起点就不是“又装一个 package，多一组 API”。

更接近事实的说法是：

`Unity 官方从一开始就把 DOTS 写成一套新的数据组织与执行路线。`

基于这些事实，我在这里的判断是：

`如果官方自己已经把 Entities 写成 DOTS 架构入口，而不是单点功能开关，那么把 DOTS 压成“普通模块”就已经太窄了。`

### `SubScene + Baking` 说明 DOTS 直接改写作者数据进入运行时的路径

再看 Unity 官方关于 `SubScene` 和 `baking` 的说明。

能直接落下的事实包括：

- Unity 官方明确写出 `ECS uses subscenes instead of scenes`
- Unity 官方明确写出 `Unity's core scene system is incompatible with ECS`
- `GameObject` 和 `MonoBehaviour` 的 authoring data 可以放进 `SubScene`
- baking 会把这些作者数据转成写入 `Entity Scene` 的 runtime data
- baking 发生在 Editor 中，而不是游戏运行时，并被官方类比为 asset importing

这几条事实的意义，不只是“DOTS 有一个转换流程”。

它们真正说明的是：

- `DOTS` 没有满足于在默认 `Scene -> GameObject` 世界旁边多加几个运行时组件
- 它直接改写了作者数据进入运行时数据的入口
- 它还跨进了内容生产与数据导入的边界

所以这里更稳的判断不是：

`DOTS 给 Unity 加了一套高性能对象 API。`

而是：

`DOTS 在 Unity 里更接近一条重新组织作者数据、运行时数据和世界承载方式的路线。`

### `World + systems + jobs + Entities Graphics` 说明 DOTS 同时碰世界组织、执行骨架和外围桥接

再往下看 Unity 官方关于 `World concepts`、ECS workflow 和 `Entities Graphics` 的说明。

能直接成立的事实包括：

- `World` 持有 `EntityManager` 和一组 `systems`
- 进入 Play mode 时，默认会创建 `default world`
- systems 会查询、变换 ECS 数据，也可以创建和销毁实体
- 在合适场景下，官方建议使用 `Burst-compatible jobs` 做并行工作
- `Entities Graphics` 被官方写成 `ECS for Unity` 与既有渲染架构之间的桥
- baking systems 本身也是 systems，并可以使用 jobs 与 Burst 处理重型加工

这些事实说明，`DOTS` 在 Unity 里碰到的不只是“对象如何存”。

它还同时碰了三层边界：

- 碰了世界组织：`World + EntityManager + systems`
- 碰了执行骨架：`systems + jobs`
- 碰了外围桥接：`Entities Graphics`、baking 与 authoring pipeline

基于这些事实，我在这里的判断是：

`Unity 的 DOTS 更像对默认 GameObject 世界、作者数据流和批处理执行骨架做的数据导向重构/特区化扩展，而不是一组普通 package。`

这里必须继续压住一个边界。

这还不是在断言：

- `DOTS` 已经完全取代 `GameObject / MonoBehaviour`
- `DOTS` 已经给 Unity 建立了唯一主路线
- `Burst`、`Jobs`、`Entities Graphics` 的内部边界已经被源码级压实

本篇当前只足够支持一个更稳的判断：

`DOTS` 已经明显超出了“普通功能模块”的站位，因为它同时碰了默认世界、执行组织和外围桥接。

## Unreal 怎么做

再看 Unreal 官方文档是怎么把 `Mass` 这条链串起来的。

### `MassEntity` 先被写成数据导向框架，并直接挂到 `UWorld`

从 Unreal 关于 `Mass Entity`、`MassEntity API module` 和 `UMassSubsystemBase` 的文档里，能直接落下几件事实：

- Unreal 官方明确 `MassEntity is a gameplay-focused framework for data-oriented calculations`
- `UMassEntitySubsystem` 的职责是为某个 `UWorld` 承载默认 `FMassEntityManager`
- `UMassSubsystemBase` 是所有 `Mass-related UWorldSubsystem` 的公共基类

这组事实很重要，因为它说明 `Mass` 在 Unreal 里不是脱离默认世界容器独立漂浮的插件。

更接近事实的说法是：

`Mass 从一开始就挂在 UWorld 级别的世界组织里。`

基于这些事实，我在这里的判断是：

`如果一个系统直接以 UWorldSubsystem 方式挂进世界容器，并且默认承载自己的实体管理器，它就已经不该再被压成“普通 gameplay 插件”。`

### `EntityManager + archetype / fragment / query / command buffer` 说明 Mass 改写的是数据布局与批处理执行方式

再看 `FMassEntityManager` 和 `FMassEntityQuery` 的 API 文档。

能直接落下的事实包括：

- `FMassEntityManager` 负责承载 entities 并管理 archetypes
- 实体会按 fragment 组合归入对应的 archetype
- 文档明确说明实体以 `chunked array` 形式存储
- 多数实体操作经由 `command buffer` 执行
- `FMassEntityQuery` 会在满足要求的 archetype 集合上触发计算
- subsystem requirements 也是查询约束的一部分

这几条事实的意义，不在于证明 `Mass` 有多少名词。

它们真正说明的是：

- `Mass` 在回答的不是“单个对象怎么挂更多功能”
- 它在回答“实体数据如何按 archetype 组织”
- 它在回答“批处理计算怎样通过 query 和 command buffer 正式进入执行链”

所以更稳的判断不是：

`Mass 只是给 Actor 世界旁边加了一套工具箱。`

而是：

`Mass 在 Unreal 里引入的是另一套实体数据布局、查询方式和执行组织。`

### `MassGameplay` 继续把这条链扩到表示、生成、LOD、复制与 StateTree

再看 Unreal 官方关于 `Mass Gameplay` 的说明。

能直接成立的事实包括：

- 官方明确 `Mass Gameplay plugin directly derives from the Mass Entity plugin`
- `MassGameplay` 覆盖 `world representation`、`spawning`、`LOD`、`replication`、`StateTree`
- 这些能力都围绕批量实体表示、更新与同步展开，而不是单对象脚本工作流

这组事实说明，`Mass` 不只是“再加一个 gameplay 模块”。

因为如果它只是普通模块，它的自然边界应该更像：

- 只解决一个具体功能点
- 不直接挂到世界容器
- 不连带带出表示、生成、LOD、复制这些跨系统问题

而现在官方文档给出的结构恰好反过来。

它更接近：

`以 MassEntity 为数据组织底座，再通过 MassGameplay 把批量实体世界向表示、生成、同步和状态管理继续扩展。`

基于这些事实，我在这里的判断是：

`Unreal 的 Mass 更像挂在 UWorld 之上的 data-oriented framework extension，用来在默认 Actor 世界旁边再开出一条批量实体组织与执行通道。`

同样，这里也必须继续压住边界。

这还不是在断言：

- `Mass` 已经完全取代 `Actor / Gameplay Framework`
- `MassGameplay` 已经可以覆盖 Unreal 默认对象世界的全部职责
- `processor`、调度器、复制内部机制已经在本篇完成源码级压实

本篇当前只足够支持一个更稳的判断：

`Mass` 已经明显超出了“普通插件”的站位，因为它直接挂进世界容器，并用另一套实体组织与批处理执行方式继续向外围系统扩展。`

## 为什么不是表面 API 差异

把前面两节再压一次，至少能先看出四层更重要的差异。

### 第一层差异：两边都不是在“加功能”，而是在改对象表示与执行组织

Unity 这边，官方资料直接支持：

- `SubScene + baking` 会改写 `GameObject` 作者数据进入运行时的路径
- `World + systems + jobs` 会改写实体执行组织

Unreal 这边，官方资料直接支持：

- `UWorld + UMassEntitySubsystem` 会把 Mass 直接挂进世界容器
- `EntityManager + archetype + query + command buffer` 会改写实体数据组织与批处理计算方式

基于这些事实，我的判断是：

`真正值得比较的，不是 DOTS 和 Mass 各自有哪些 API，而是它们各自对默认对象世界和执行骨架动了什么刀。`

### 第二层差异：地图位置相近，但不等于语义一一对应

这也是最容易写偏的地方。

因为一旦看到两边都有：

- entity
- query
- 批处理
- 数据导向

就很容易直接写成一组术语对译表。

但当前证据真正足够支持的，只是：

- Unity 的 `DOTS` 更接近一条以 `Entities / SubScene / Baking / World / Systems` 为核心的路线
- Unreal 的 `Mass` 更接近一条以 `UWorld / EntityManager / Query / MassGameplay` 为核心的路线

基于这些事实，我的判断是：

`它们在地图里的位置相近，但不能因此跳成“语义完全等价”。`

更稳的写法是：

`两边都在做数据导向扩展，但各自扩展的入口、默认世界关系和外围桥接方式并不相同。`

### 第三层差异：这层同时碰到了世界模型层和运行时底座层

如果把这篇收回整套系列地图，会更容易看出为什么它必须单列。

因为 `DOTS / Mass` 既不像：

- `04` 那样主要是某个专业子系统
- 也不像 `03` 那样只是执行底层机制

它们更接近一块“跨层扩展区”：

- 向下，会碰默认世界承载方式和执行骨架
- 向上，会继续碰表示、生成、渲染桥接、LOD、同步等外围系统

基于这些事实，我在这里的判断是：

`“数据导向扩展层”之所以要单列，不是因为它更高级，而是因为它同时改写了世界模型层与运行时底座层的连接方式。`

### 第四层差异：最稳的比较方式不是“谁更像谁”，而是“各自改写了哪一段成本模型”

这一点尤其重要。

如果只写名词对照，你得到的会是：

- `Entity` 对 `Entity`
- `System` 对 `Query`
- `Baking` 对 `Spawner`

这种写法的信息量很低，而且容易错。

更稳的比较方式是：

- Unity 这边，重点看 `GameObject authoring data` 怎么进 `Entity Scene`，以及 `World + systems + jobs` 怎么托住批处理执行
- Unreal 这边，重点看 `UWorld` 怎样挂 `MassEntitySubsystem`，以及 `EntityManager + archetype + query + MassGameplay` 怎样托住批量实体组织与外围扩展

基于这些事实，我的判断是：

`这两条路线真正相似的地方，不是表面 API，而是它们都在重写“高规模对象该如何表示、如何批处理、如何桥接外围系统”的成本模型。`

## 常见误解

### 误解一：`DOTS / Mass` 就是又一个高性能模块

这句话会直接把架构问题压扁成“性能标签”。

更接近事实的是：

- `DOTS` 会改写作者数据进入运行时的路径，也会改写世界与执行组织
- `Mass` 会直接挂进 `UWorld`，并用实体管理器、查询和外围子系统去组织批量实体

所以它们首先不是“更快”，而是“换了一套对象表示与执行组织方式”。

### 误解二：`DOTS` 已经完全等于 Unity 的未来默认对象模型，`Mass` 已经完全等于 Unreal 的未来默认世界

当前证据还不支持这种强结论。

本篇现在能安全落下的，只是：

- `DOTS` 在 Unity 里已经明显超出了普通 package 的站位
- `Mass` 在 Unreal 里已经明显超出了普通插件的站位

但这不等于：

- 默认对象世界已经被完全取代
- 两条旧路径已经失效
- 工程上只剩这一条路线

### 误解三：`DOTS` 和 `Mass` 可以直接做严格一一映射

这篇不支持这种写法。

原因不是两边没有相似点，而是当前证据更适合支持“架构站位”而不是“术语级对译”。

更稳的做法是：

- 先比较它们各自和默认世界的关系
- 再比较它们各自怎样组织批处理执行
- 最后比较它们怎样桥接外围系统

而不是先把每个名词排成一张“中文翻译表”。

### 误解四：既然已经碰到表示、复制和渲染桥接，就应该把渲染、网络和表现系统一起讲完

这也是最容易越界的地方。

这些材料在本篇里的作用，只是证明：

`DOTS / Mass` 已经跨出了单一模块边界。

它们在这里不是要求本篇顺手写完：

- `04` 的专业子系统层
- `05` 的资产与发布层
- 网络同步或表现系统的完整实现

### 误解五：这篇应该顺手裁判谁更先进、谁更适合大规模项目

这不是这篇的任务。

本文当前只做一件事：

`把 DOTS / Mass 放回正确层级。`

如果层级先没压清，性能、规模、工作流这些讨论都会立刻混线。

## 我的结论

先重申这篇能直接成立的事实。

- Unity 官方明确把 `Entities` 写成 `DOTS` 的一部分，并把它定义成 data-oriented 的 ECS 架构实现
- Unity 官方明确 `ECS uses subscenes instead of scenes`，并明确 `Unity's core scene system is incompatible with ECS`
- Unity 官方明确 `World`、`systems`、`Burst-compatible jobs`、`Entities Graphics` 都属于这条数据导向路线的一部分
- Unreal 官方明确 `MassEntity` 是 `gameplay-focused framework for data-oriented calculations`
- Unreal 官方明确 `UMassEntitySubsystem` 为某个 `UWorld` 承载默认 `FMassEntityManager`
- Unreal 官方明确 `FMassEntityManager`、`FMassEntityQuery`、`MassGameplay` 围绕 archetype、query、representation、spawning、LOD、replication、StateTree 组织这条路线
- 当前本地源码路径还没有任何 `READY` 标记，因此这篇不能声称自己做了源码级验证

基于这些事实，我在这篇里愿意先给出的工程判断是：

`DOTS / Mass` 最稳的地图位置，不是普通功能模块，也不宜压成单纯的运行时细节，而是建立在世界模型层与运行时底座层之上的“数据导向扩展层”。`

进一步说：

- Unity 的 `DOTS` 更接近对默认 `GameObject` 世界、作者数据流和批处理执行骨架做的数据导向重构/特区化扩展
- Unreal 的 `Mass` 更接近挂在 `UWorld` 之上的数据导向框架扩展，用来在默认 `Actor` 世界旁边建立另一条批量实体组织与执行通道

因此，这篇最值得记住的一句话不是：

`DOTS 和 Mass 都很快。`

而是：

`它们之所以不能只算“一个模块”，是因为它们都在重写默认对象世界和执行组织之间的连接方式。`

到这里，前四篇核心地图文章也就能闭合起来了：

- `00` 先给出总地图
- `02` 先压默认对象世界
- `03` 先压运行时底座
- `07` 再说明当默认对象世界和默认执行方式不再够用时，引擎怎样长出一层数据导向扩展区

后面再回到 `01 / 04 / 05 / 06`，就不必继续把 `DOTS / Mass` 混写进每一层了。
