# 游戏引擎架构地图 07｜详细提纲：为什么 DOTS 和 Mass 不能只算“一个模块”？

## 本提纲用途
- 对应文章：`07`
- 本次增量类型：`详细提纲`
- 证据基础：`docs/game-engine-architecture-07-evidence-card.md`
- 证据等级：`官方文档`
- 约束说明：`docs/engine-source-roots.md` 中 Unity / Unreal 仍不是 `READY`，本提纲只安排“官方资料明确写了什么”和“基于这些事实的暂定判断”，不写源码级定论。

## 文章主问题与边界

- 这篇只回答：`DOTS 和 Mass 在整张引擎架构地图里到底站哪一层，为什么它们不该被当成普通功能模块？`
- 这篇不展开：`00` 里的六层总地图细节
- 这篇不展开：`02` 里的 `Scene / World`、`GameObject / Actor` 默认对象世界全量比较
- 这篇不展开：`03` 里的 `GC / reflection / Job System / Task Graph` 运行时机制细节
- 这篇不展开：`04 / 05 / 06` 里的渲染、资产发布链、平台抽象完整内部实现
- 这篇不做：`DOTS` 与 `Mass` 的性能优劣判断、教程式 API 罗列、严格一一语义映射
- 本篇允许落下的判断强度：`只把 DOTS / Mass 收回到“数据导向扩展层”的地图站位，并说明它们分别如何改写默认对象世界和执行组织；不做源码级强结论。`

## 一句话中心判断

- `Unity 的 DOTS 更接近“以 Entities / SubScene / Baking / World / Systems 为核心，对默认 GameObject 世界和执行骨架做数据导向重构”；Unreal 的 Mass 更接近“以 UWorld 挂接的 MassEntity / MassGameplay 为核心，对默认 Actor 世界和批处理执行方式做数据导向扩展”。`
- `因此，DOTS / Mass 最稳的地图站位不是普通功能模块，也不宜直接压成单纯运行时底座，而是建立在世界模型层与运行时底座层之上的“数据导向扩展层”。`

## 行文顺序与字数预算
| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 这篇要回答什么 | 350 - 500 | 把“DOTS / Mass 到底算什么”从包名争论改写成层级站位问题 |
| 2. 这一层负责什么 | 700 - 950 | 定义“数据导向扩展层”负责改写哪些对象组织与执行边界，并给出总对照表 |
| 3. 这一层不负责什么 | 350 - 500 | 明确本篇不越界到默认世界模型全解、运行时底座细节、专业子系统或性能裁判 |
| 4. Unity 怎么落地 | 850 - 1100 | 沿 `Entities / SubScene / Baking / World / Systems / Entities Graphics` 铺开 DOTS |
| 5. Unreal 怎么落地 | 850 - 1100 | 沿 `MassEntity / UMassEntitySubsystem / FMassEntityManager / Query / MassGameplay` 铺开 Mass |
| 6. 为什么不是表面 API 差异 | 500 - 700 | 把差异收回到对象表示、批处理执行、世界承载与系统桥接方式 |
| 7. 常见误解 | 450 - 650 | 集中拆掉“又一个模块”“已经完全替代默认对象模型”“可以一一对照”等误读 |
| 8. 我的结论 | 250 - 400 | 收束成一条地图判断，并把后续系列重新挂回总地图 |

## 详细结构

### 1. 这篇要回答什么
- 开篇切口：
  - 先写常见问法：`DOTS 是不是 Unity 里又一个高性能包？Mass 是不是 Unreal 里又一个 AI / gameplay 插件？`
  - 再写常见误读：`如果只看包名、插件名和 API 名词，很容易把它们写成“又多了一套功能”。`
- 要抛出的核心问题：
  - `当引擎开始为大规模实体、批处理更新、数据导向表示专门改写默认对象世界与执行路径时，这种能力该放在整张架构图的哪一层？`
- 这一节要完成的动作：
  - 把问题从“功能模块命名”改写成“层级站位判断”
  - 说明本文只回答 `DOTS / Mass 在地图里站哪里`
  - 明确本文不负责解释所有 ECS / Mass 术语，不做教程
- 可直接引用的证据锚点：
  - 证据卡 `1` 到 `6`
- 本节事实与判断分界：
  - `事实`：Unity / Unreal 官方都把这些能力写成正式框架，而不是零散示例 API
  - `判断`：所以本文真正要解释的是它们在架构图里的位置，而不是名词表

### 2. 这一层负责什么
- 本节要先定义“数据导向扩展层”到底负责回答什么：
  - 默认对象世界在大规模实体场景下如何被重新表示
  - 实体数据如何被批处理查询、更新、调度与同步
  - 作者数据如何进入这种新世界，或这种新世界如何挂回原有世界容器
  - 这种扩展如何跨到表示、生成、LOD、复制等多项系统，而不止停留在单一模块
- 建议放一张总对照表：

| 对照维度 | Unity DOTS | Unreal Mass | 本节要压出的意思 |
| --- | --- | --- | --- |
| 作用对象 | 改写 `GameObject / Scene` 默认对象组织的承载方式 | 改写 `Actor / UWorld` 默认对象组织上的批处理框架 | 二者都不是单纯加功能点 |
| 世界承载 | `SubScene + Entity Scene + World` | `UWorld + UMassEntitySubsystem` | 都直接碰世界容器 |
| 数据组织 | `Entity / Component / System / Query` | `Entity / Fragment / Archetype / Query` | 都引入另一套数据组织方式 |
| 执行组织 | `systems + jobs + Burst-compatible workflow` | `queries + command buffer + Mass processors / subsystems` | 都重写批处理执行路径 |
| 外围桥接 | `baking` 触及作者数据与导入流程，`Entities Graphics` 连接渲染 | `MassGameplay` 扩到 `representation / spawning / LOD / replication / StateTree` | 都跨出单一功能模块边界 |
| 工程判断 | 数据导向重构/扩展层 | 数据导向重构/扩展层 | 地图位置相近，但实现不做一一等同 |

- 本节必须压出的判断：
  - `数据导向扩展层` 关心的不是“再给对象加多少功能”，而是“对象如何表示、如何批处理、如何接入世界与执行系统”
  - 这层之所以单列，不是因为它“更高级”，而是因为它跨过世界模型层和运行时底座层，改写两者的连接方式
- 证据锚点：
  - 证据卡 `1` 到 `6`
- 本节事实与判断分界：
  - `事实`：官方资料能直接支持 `World / UWorld`、`SubScene / UWorldSubsystem`、`query / archetype / systems`、`representation / graphics bridge` 这些维度
  - `判断`：这些维度足以把 `DOTS / Mass` 从“普通模块”里单独拎出来

### 3. 这一层不负责什么
- 必须明确写出的边界：
  - 不把 `DOTS / Mass` 写成默认世界模型的全量替代史
  - 不把 `Job System / Task Graph / GC / reflection` 重新展开成运行时底座文章
  - 不把 `Entities Graphics`、`Mass Representation` 写成完整渲染或表现系统文章
  - 不把 `baking`、`spawning`、`replication` 扩写成资产发布链或网络架构教程
  - 不做 `DOTS` 与 `Mass` 的性能优劣判断
  - 不宣称 `DOTS` 已完全取代 `GameObject / MonoBehaviour`，也不宣称 `Mass` 已完全取代 `Actor / Gameplay Framework`
- 建议用一段“为什么必须克制”收尾：
  - 如果把默认对象世界、运行时底座、专业子系统、平台与性能问题全拖进来，这篇会从“架构站位文”滑成“术语百科 + 产品对比”
- 证据锚点：
  - 证据卡“本卡暂不支持的强结论”部分

### 4. Unity 怎么落地
- 本节只沿着 Unity 官方给出的数据导向链条往下写，不做 DOTS 入门教程

#### 4.1 `Entities` 明确把 DOTS 写成数据导向 ECS 架构
- 可用材料：
  - `Entities overview`
  - `ECS workflow tutorial`
- 可落下的事实：
  - Unity 明确把 `Entities package` 写成 `DOTS` 的一部分
  - Unity 明确把 `Entities` 定义成 data-oriented 的 ECS 架构实现
  - `ECS workflow` 被写成一组协同工作的技术与包，而不是单点功能
- 可落下的暂定判断：
  - `DOTS` 的起点不是“又一个功能包”，而是换了一套对象组织与执行方式
- 证据锚点：
  - 证据卡 `1`

#### 4.2 `SubScene + Baking` 说明 DOTS 直接改写作者数据进入运行时的路径
- 可用材料：
  - `Subscenes overview`
  - `Baking overview`
- 可落下的事实：
  - Unity 明确写出 `ECS uses subscenes instead of scenes`
  - Unity 明确写出 `Unity's core scene system is incompatible with ECS`
  - `GameObject / MonoBehaviour` 作者数据会经由 baking 转成 `Entity Scene` 运行时数据
  - baking 发生在 Editor 中，并被类比为 asset importing
- 可落下的暂定判断：
  - `DOTS` 不只是运行时 API 扩展，它还直接跨进内容生产与数据导入边界
- 证据锚点：
  - 证据卡 `2`

#### 4.3 `World + Systems + Jobs + Entities Graphics` 说明 DOTS 同时碰世界组织、执行骨架和外围系统桥接
- 可用材料：
  - `World concepts`
  - `Understand the ECS workflow`
  - `Entities Graphics overview`
  - `Baking systems overview`
- 可落下的事实：
  - `World` 持有 `EntityManager` 与一组 systems
  - systems 负责查询、变换 ECS 数据，也能创建和销毁实体
  - 合适场景下官方建议使用 `Burst-compatible jobs`
  - `Entities Graphics` 被写成 ECS 与既有渲染架构之间的桥
  - baking systems 本身也是 systems，并能使用 jobs / Burst 处理重型加工
- 可落下的暂定判断：
  - `DOTS` 同时改写世界表示、执行组织和外围桥接，因此它最稳的位置不是“某个专业子系统”
- 证据锚点：
  - 证据卡 `3`

#### 4.4 本节收口
- 必须收成一句话：
  - `Unity 的 DOTS 更像对默认 GameObject 世界、作者数据流与批处理执行骨架的 data-oriented 重构/特区化扩展，而不是一个普通 package。`
- 必须明确的边界提醒：
  - 这还不是在断言 `DOTS` 已成为 Unity 唯一主路线
  - 这也不是在展开 `Burst` 或渲染系统的完整内部细节

### 5. Unreal 怎么落地
- 本节只沿着 Unreal 官方给出的 Mass 链条往下写，不做 gameplay 教程

#### 5.1 `MassEntity` 先被定义成数据导向框架，并挂到 `UWorld`
- 可用材料：
  - `Mass Entity in Unreal Engine`
  - `MassEntity API module`
  - `UMassSubsystemBase`
- 可落下的事实：
  - `MassEntity` 被官方定义为 gameplay-focused data-oriented framework
  - `UMassEntitySubsystem` 的职责是为某个 `UWorld` 承载默认 `FMassEntityManager`
  - `UMassSubsystemBase` 是一组 Mass 相关 `UWorldSubsystem` 的公共基类
- 可落下的暂定判断：
  - `Mass` 不是脱离默认世界容器独立漂浮的插件，而是直接挂到 `UWorld` 级别的世界组织里
- 证据锚点：
  - 证据卡 `4`

#### 5.2 `EntityManager + Archetype / Fragment / Query / Command Buffer` 说明 Mass 改写的是数据布局与批处理执行方式
- 可用材料：
  - `FMassEntityManager`
  - `FMassEntityQuery`
- 可落下的事实：
  - `FMassEntityManager` 管理 entities 与 archetypes
  - 实体按 chunked array 方式存储，并按 fragment 组合归入 archetype
  - 多数实体操作经由 command buffer 执行
  - `FMassEntityQuery` 以 archetype 集合和 subsystem requirements 组织计算
- 可落下的暂定判断：
  - `Mass` 引入的不是多几个 gameplay 名词，而是另一套数据布局、查询与执行组织方式
- 证据锚点：
  - 证据卡 `5`

#### 5.3 `MassGameplay` 继续扩到表示、生成、LOD、复制与 StateTree
- 可用材料：
  - `Overview of Mass Gameplay`
  - `MassEntity API module`
- 可落下的事实：
  - `Mass Gameplay plugin directly derives from the Mass Entity plugin`
  - `MassGameplay` 覆盖 `representation / spawning / LOD / replication / StateTree`
  - 这些能力围绕批量实体表示、更新与同步展开，而不是单对象脚本
- 可落下的暂定判断：
  - `MassGameplay` 证明 `Mass` 不是“再多一个模块”，而是围绕数据导向实体批量模拟展开的一层扩展区
- 证据锚点：
  - 证据卡 `6`

#### 5.4 本节收口
- 必须收成一句话：
  - `Unreal 的 Mass 更像挂在 UWorld 之上的 data-oriented framework extension，用来把默认 Actor 世界旁边再开出一条批量实体组织与执行通道。`
- 必须明确的边界提醒：
  - 这还不是在断言 `Mass` 已完全取代 `Actor / Gameplay Framework`
  - 这也不是在写 AI、人群、网络同步或表现系统教程

### 6. 为什么不是表面 API 差异
- 本节要把前两节材料收回成 4 个判断：
  - `差异一`：Unity 通过 `SubScene + Baking + World / Systems` 改写作者数据到运行时数据的入口；Unreal 通过 `UWorld + EntityManager + Query + MassGameplay` 在默认世界旁建立批处理实体框架
  - `差异二`：二者都在改写对象表示与执行组织，不是只多了一套命名不同的 API
  - `差异三`：二者地图位置相近，但不等于语义一一对应
  - `差异四`：最稳的比较方式不是“谁更像谁”，而是“它们各自对默认对象世界和执行骨架动了什么刀”
- 建议收束段：
  - `最容易写偏的，是把 DOTS 压成 ECS 包、把 Mass 压成 gameplay 插件。更稳的写法，是把它们都看成对默认世界模型与运行时执行方式做数据导向改写的扩展层。`
- 本节事实与判断分界：
  - `事实`：官方资料直接支持 `SubScene / Baking / World / Systems` 与 `UWorldSubsystem / EntityManager / Query / MassGameplay` 这些结构
  - `判断`：所以差异不在表面 API，而在架构位置与改写范围

### 7. 常见误解
- 误解 `1`：
  - `DOTS / Mass 就是又一个高性能模块`
  - 纠正方式：指出它们关心的是对象表示、批处理执行与世界挂接方式，不只是“更快”
- 误解 `2`：
  - `DOTS 已经完全等于 Unity 的未来默认对象模型，Mass 已经完全等于 Unreal 的未来默认世界`
  - 纠正方式：强调当前证据只足够支持“它们是数据导向扩展层”，不支持“已经完全取代默认对象体系”
- 误解 `3`：
  - `DOTS 的 Entity / System / Baking 可以和 Mass 的 Entity / Query / Processor 做严格一一映射`
  - 纠正方式：强调本文只比较“架构站位”，不做术语级对译
- 误解 `4`：
  - `既然涉及 Graphics / Representation / Replication，就应该把渲染、网络、表现系统全部一起展开`
  - 纠正方式：回到本篇边界，说明这些只是证明它们跨层，不是要求本篇把所有系统写完
- 误解 `5`：
  - `这篇应该顺手裁判谁更先进、谁更适合大项目`
  - 纠正方式：重申本文不做产品优劣判断，只做架构归位

### 8. 我的结论
- 收束顺序建议：
  - 先重申主问题不是“DOTS / Mass 有哪些功能”，而是“它们在地图里站哪层”
  - 再重申可以直接成立的事实
  - 最后给出工程判断
- 本段必须写出的事实：
  - Unity 官方支持把 `Entities` 放进 `DOTS`，并通过 `SubScene + Baking + World / Systems + Entities Graphics` 形成一整套数据导向路径
  - Unreal 官方支持把 `MassEntity` 写成 data-oriented framework，并通过 `UWorldSubsystem + EntityManager + Query + MassGameplay` 形成一整套批量实体扩展
  - 当前没有本地 `READY` 的 Unity / Unreal 源码根路径
- 本段必须写出的判断：
  - `DOTS / Mass` 最稳的地图位置是 `数据导向扩展层`
  - 这层建立在 `世界模型层 + 运行时底座层` 之上，但又会向内容生产与专业子系统桥接
  - 因而它们不适合被压成普通功能模块，也不宜被误写成单纯低层实现细节
- 结尾过渡：
  - 这篇写完之后，前四篇核心地图文章就都能闭合；后续再回到 `01 / 04 / 05 / 06`，就不必继续把 `DOTS / Mass` 混写进每一层

## 起草时必须保留的一张对照表

| 对照维度 | Unity DOTS | Unreal Mass | 本文要落下的判断 |
| --- | --- | --- | --- |
| 默认对象世界的关系 | `SubScene + Baking` 说明它直接改写 `GameObject` 作者数据进入运行时的路径 | `UWorld + UMassEntitySubsystem` 说明它直接挂在默认世界容器上 | 二者都直连默认世界组织 |
| 运行时核心组织 | `World + EntityManager + Systems + Jobs` | `FMassEntityManager + Archetype / Query + Command Buffer` | 二者都在改写批处理执行结构 |
| 跨层桥接 | `Entities Graphics` 连接既有渲染架构 | `MassGameplay` 扩到 `representation / spawning / LOD / replication / StateTree` | 二者都跨出“单一模块”边界 |
| 工程判断 | 数据导向重构/扩展层 | 数据导向框架扩展层 | 地图位置相近，不做一一等价 |

## 可直接拆出的两条短观点
- `判断 DOTS / Mass，不该先看它们像不像一个 package / plugin，而该先看它们改写了默认对象世界和执行组织的哪一段。`
- `“数据导向扩展层”之所以要单列，不是因为它更炫，而是因为它同时碰到了世界模型、运行时执行和外围系统桥接。`

## 起草时必须反复自检的三件事
- `我有没有把这篇写成 ECS / Mass 教程，或者性能宣传文，而不是架构站位文`
- `我有没有把 02 的默认世界模型、03 的运行时底座、04/05/06 的系统细节重新抢写进来`
- `我有没有把事实和判断明确分开，并持续提醒当前没有源码级验证`
