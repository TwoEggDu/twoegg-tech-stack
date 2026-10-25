# 游戏引擎架构地图 07｜证据卡：为什么 DOTS 和 Mass 不能只算“一个模块”

## 本轮修订与来源范围

- 核对日期：2026-10-25。
- 对应正文：`content/system-design/game-engine-architecture-07-dots-and-mass.md`。
- 文章类型：映射篇，先给创作数据、实体组织、批量执行、专业表示的公共问题，再落 Unity / Unreal。
- Unity：统一使用 `docs.unity3d.com/Packages/` 下 Entities 与 Entities Graphics 的 `@1.0` 文档；本次读取页面元数据均标注 `1.0.16`。不再将 1.0—1.3 的不同材料混成未标版本的统一事实。
- Unreal：本卡所列官方资料均带 `application_version=5.6`；以模块、MassEntity 与 MassGameplay 概览作为可直接核对的依据。
- 验证等级：公开文档读取与编辑审查；没有启动 Unity / Unreal、执行示例、采集性能数据或完成源码级依赖审计。站点构建由本轮集成验证记录另行确认。

## 核心边界与修订提纲

正文按“问题 → 代码容器与职责 → 人群设计示意 → Unity → Unreal → 公共对照 → 交接协议”组织。

1. 模块、包与插件是代码组织或分发形式；框架可以通过这些形式交付。是否访问 World 应检查接口与寿命协议，不能凭“普通模块”这个分类否定。
2. DOTS/Mass 的影响覆盖内容生产、实体组织、执行与专业桥接，因此在职责地图上标作跨职责扩展区；这不是严格依赖图上必须叠在传统对象模型之上的一层。
3. 具体链路分别核对，不能做 `System = Query`、`Baking = Spawner` 的等价映射。
4. 人群的位置、速度、目标点及交接设计是讲解示意；官方资料支持其采用的机制，不构成运行结果。

## 事实与官方来源

| 来源与版本 | 本轮直接支持的事实 | 在正文中的使用边界 |
| --- | --- | --- |
| [UE Modules 5.6](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-modules?application_version=5.6) | 模块承担代码封装、构建依赖与加载管理等职责；项目和插件可组织模块 | 说明代码容器与架构职责是不同维度，不推导任意依赖都合理 |
| [Entities overview 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html) | Entities 是 DOTS 的一部分，提供数据导向 ECS 实现 | 区分包与较宽的技术组合，不声称取代所有 GameObject 用途 |
| [Subscenes 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/conversion-subscenes.html) | authoring 内容可置于 SubScene，连接 baking 与实体内容管理 | 解释制作入口，不把 SubScene 等同于 ECS World |
| [Baking 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/baking-overview.html) | 在 Editor 内把创作数据转换为实体运行数据 | 区分制作期转换与运行期协作，不宣称任意脚本逻辑自动转换 |
| [World concepts 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-worlds.html) | World 包含实体、EntityManager 与 systems | 说明 ECS 自身的世界组织，不称其必然依赖逐个 GameObject 对象 |
| [ECS workflow 1.0](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/ecs-workflow-intro.html) | authoring/baker 连接组件数据；实体可运行时创建；系统查询与更新数据；jobs 需考虑并行收益与开销 | 不把所有 system 写成自动多线程；人物移动逻辑是项目设计示意 |
| [Entities Graphics 1.0](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/overview.html) | 在 ECS 与既有渲染架构间提供桥接及相关 baking 支持 | 不扩展为所有动画、上传或渲染流程均由 EntityManager 承担 |
| [MassEntity overview 5.6](https://dev.epicgames.com/documentation/unreal-engine/overview-of-mass-entity-in-unreal-engine?application_version=5.6) | Fragment 组成实体，按 Archetype/Chunk 组织；Processor 通过查询处理批次；结构组合变更可经命令缓冲延迟；概览介绍 World 子系统、管理器与配置/模板 | 支持实体组织与 World 上下文的关系；不声称每个实体都是 Actor，不推断未核验的内部 API 调用链 |
| [MassGameplay overview 5.6](https://dev.epicgames.com/documentation/en-us/unreal-engine/overview-of-mass-gameplay-in-unreal-engine?application_version=5.6) | 框架由多个插件组成；包含表示、生成、LOD 等能力；可选择不同精度 Actor、ISM 或无表示，并分别讨论模拟与表示 LOD | 支持模拟/表示分工，不保证任意角色动画、交互、复制都自动兼容 |

## 本轮撤回的旧表述

- 删除“普通模块不应直接挂世界容器”的推论。模块可承担世界内职责，必须检查的是依赖和生命周期。
- 将“建立在世界模型与运行时底座之上的独立扩展层”改成跨职责组织方式，与 00A 的职责/依赖区分对齐。
- 不再用“不是普通 package/plugin”否定真实交付形式；改为“只知道交付形式不足以理解影响范围”。
- 删除重复的源码根路径状态、首稿状态及“下一次先建提纲”指令；正文当前范围由这份修订记录说明。
- 不再借未固定版本的 Mass API 入口作本轮依据：读取 `API/Runtime/MassEntity` 和 `UMassEntitySubsystem` 的 5.6 入口未取得正文，故使用已读取的官方概览并收窄结论，不编造 API 级验证。

## 公共模型与具体例子的区分

“创作描述 → 实体数据 → 批量更新 → 专业表示”是本文的设计讲解框架；箭头表示数据交接，不能当作模块依赖、线程安排或严格逐帧流水线。

- Unity 的制作期转换、World 与渲染桥接分别有上表来源；人物移动参数及处理顺序是应用示意。
- Mass 的可选 Actor/ISM/无表示以及表示转换来自官方材料；“外观变化不应隐式删除模拟身份”是本文据此提出的设计检查原则。
- 状态归属、身份有效性与更新时间是工程审查问题，不是已验证两台引擎都提供相同协议。
- 本篇不作性能优劣判断，也不声称旧对象体系已被替代。

## 相关入口

- 公共模型：`content/system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md`。
- 相邻职责：本系列 02 世界模型、03 运行时机制、04 专业系统。
- 机制细节：`content/engine-toolchain/data-oriented-runtime-series-index.md`。
- 表示交接：`content/engine-toolchain/data-oriented-runtime-06-why-simulation-and-representation-usually-cannot-fully-merge.md`。

本卡服务于当前正文证据与版本维护；系列状态以 canonical `docs/game-engine-architecture-series-plan.md` 为准。
