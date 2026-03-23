# 游戏引擎架构地图 08｜证据卡：Unity 和 Unreal，到底是什么气质的引擎

## 本卡用途

- 对应文章：`08`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 收束基础：`沿用 00-07 已建立的官方文档证据边界，只做总收束，不重开新专题`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`把前面几层收回来之后，Unity 和 Unreal 各自最稳定的架构气质是什么。`
- 这篇不展开：`01 到 07 每一层的完整内部机制`
- 这篇不展开：`性能高低、商业定位、团队规模适配或“谁更强”的产品比较`
- 这篇不展开：`源码内部实现、模块依赖图、线程模型或发布策略的实现级对照`
- 本篇允许做的事：`只把 00-07 已经建立的官方文档证据边界收束成“复杂度被放在什么组织方式里”的工程判断。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册与既有证据卡，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与既有证据卡，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 官方反复把内容生产、对象装配和能力进入项目的入口写成 `Editor + Prefab + Package + GameObject`

- Unity 入口：
  - [Scene view navigation](https://docs.unity3d.com/Manual/SceneViewNavigation.html)
  - [Prefabs](https://docs.unity3d.com/Manual/Prefabs.html)
  - [Get started with packages](https://docs.unity3d.com/Manual/Packages.html)
  - [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html)
- 可直接证明的事实：
  - Unity 官方明确 `Scene View` 是交互式创作世界的核心视图，`Inspector` 与 `Hierarchy` 一起组成日常编辑界面。
  - Unity 官方明确 `Prefab` 是 `reusable asset`，并允许以资产模板方式集中编辑和传播改动。
  - Unity 官方明确 `package` 可以同时承载 `Editor tools`、`Runtime tools`、`Asset collections` 与 `Project templates`，并由项目级 manifest 管理。
  - Unity 官方明确 `GameObject` 是基础对象，而行为与能力主要通过 `Component` 拼装。
- 暂定判断：
  - Unity 官方对外暴露的第一层组织语言，明显偏向 `可编辑工作流 + 可复用资产模板 + package 化能力进入 + component 组合对象`。

### 2. Unity 官方在运行时、交付与扩展层继续沿用 `backend + pipeline + profile + optional data-oriented route` 的组织方式

- Unity 入口：
  - [IL2CPP Overview](https://docs.unity3d.com/cn/2023.2/Manual/IL2CPP.html)
  - [BuildPipeline](https://docs.unity3d.com/ScriptReference/BuildPipeline.html)
  - [Introduction to build profiles](https://docs.unity3d.com/Manual/build-profiles.html)
  - [Entities overview](https://docs.unity.cn/Packages/com.unity.entities%401.3/manual/index.html)
- 可直接证明的事实：
  - Unity 官方明确 `IL2CPP` 是 scripting backend，会把 IL 转成 C++ 再生成平台原生二进制。
  - Unity 官方明确 `BuildPipeline` 同时负责构建 `players or AssetBundles`。
  - Unity 官方明确 `build profile` 是“用于在特定平台上构建应用的一组 configuration settings”。
  - Unity 官方明确 `Entities package` 是 `DOTS` 的一部分，并提供 data-oriented 的 `ECS architecture`。
- 暂定判断：
  - Unity 不是只在对象层强调组合式；连运行时后端、构建交付和高规模扩展，也更常被写成 `可切换后端 + pipeline + profile + package/route` 的工作流组织。

### 3. Unreal 官方反复把内容生产、对象世界和默认玩法脚手架写成 `Editor + World/Actor + Gameplay Framework`

- Unreal 入口：
  - [Unreal Editor Interface](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-editor-interface?application_version=5.6)
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
  - [GameFramework API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/GameFramework)
  - [Objects in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/objects-in-unreal-engine?application_version=5.6)
- 可直接证明的事实：
  - Unreal 官方明确 `Level Editor` 是开发内容时花费最多时间的主界面，`Content Browser` 是创建、导入、组织和管理资产的 primary area。
  - Unreal 官方明确 `World` 容纳所有 `Levels`，`Actor` 是可以放入 level 的对象。
  - Unreal 官方正式列出 `Pawn`、`Controller`、`GameMode`、`GameState` 等 `GameFramework` 类型。
  - Unreal 官方明确 `UObject` 提供 `garbage collection`、`reflection`、`serialization`、`runtime type information` 与 editor integration 等基础能力。
- 暂定判断：
  - Unreal 官方对外暴露的默认骨架，更明显是 `编辑器工作区 + world container + actor world + gameplay framework + UObject object system` 这一整套框架化组织。

### 4. Unreal 官方在交付、平台与数据导向扩展层继续沿用 `module + target + world-attached subsystem/framework` 的组织方式

- Unreal 入口：
  - [Packaging Your Project](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project)
  - [Build Configurations Reference for Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/build-configurations-reference-for-unreal-engine)
  - [ITargetPlatform](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Developer/TargetPlatform/ITargetPlatform)
  - [Mass Entity in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/mass-entity-in-unreal-engine?application_version=5.6)
- 可直接证明的事实：
  - Unreal 官方明确 packaging 是正式 `build operation`，并显式区分 `build / cook / stage / package`。
  - Unreal 官方明确 build configuration 由 `state + target` 组成。
  - Unreal 官方明确 `ITargetPlatform` 是目标平台接口，并区分 `settings` 与 `controls`。
  - Unreal 官方明确 `MassEntity` 是 `gameplay-focused framework for data-oriented calculations`，并通过 `UWorld` 级别的子系统组织进入默认世界。
- 暂定判断：
  - Unreal 连交付、平台抽象和高规模扩展，也更常被写成 `模块 / target / 平台对象 / world-attached framework` 的明确骨架，而不是单纯若干 workflow 选项。

### 5. 两边都覆盖完整引擎分层，但官方文档并没有把它们写成同一种复杂度分配方式

- 交叉入口：
  - [Introduction to render pipelines](https://docs.unity3d.com/Manual/render-pipelines-overview.html)
  - [UI Toolkit](https://docs.unity3d.com/Manual/UIElements.html)
  - [Lumen Technical Details in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-technical-details-in-unreal-engine)
  - [Slate Overview for Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/slate-overview-for-unreal-engine)
- 可直接证明的事实：
  - Unity 官方把渲染、UI 等专业能力持续写成 `render pipeline`、`UI Toolkit` 这类独立工作流和资源格式体系。
  - Unreal 官方把渲染、UI 等专业能力持续写成 `Lumen`、`Slate` 这类带自己框架、工具和调试入口的系统。
  - 结合 `01-07` 已建立的证据卡，可以确认两台引擎都覆盖内容生产、世界模型、运行时底座、专业子系统、资产与发布、平台抽象、数据导向扩展这些层。
- 暂定判断：
  - `08` 最稳的收束方式不是说“两边功能都很全”，而是说明它们虽然覆盖相近层级，却把复杂度放进了不同的默认组织方式里。

## 本轮可以安全落下的事实

- `事实`：`docs/engine-source-roots.md` 当前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。
- `事实`：基于 `01-07` 已建立的官方文档证据边界，Unity 官方反复把 `Scene View / Prefab / Package / GameObject / IL2CPP / BuildPipeline / build profiles / Entities` 暴露为正式入口。
- `事实`：基于 `01-07` 已建立的官方文档证据边界，Unreal 官方反复把 `Unreal Editor / World / Actor / Gameplay Framework / UObject / Packaging / target platform / Mass` 暴露为正式入口。
- `事实`：两台引擎都不是“只有运行时”的薄引擎；官方材料都同时覆盖内容生产、对象组织、运行时机制、专业子系统、交付与高规模扩展。
- `事实`：官方材料没有直接支持“哪台引擎更先进、更完整或更适合所有项目”这种产品结论。

## 基于这些事实的暂定判断

- `判断`：文章 `08` 可以把 Unity 的最稳定架构气质先收束为 `更偏组件化对象模型、package 化能力分发与通用工作流组织的引擎`。
- `判断`：文章 `08` 可以把 Unreal 的最稳定架构气质先收束为 `更偏编辑器工作区、World/Actor/Gameplay Framework 骨架与模块化系统组织的引擎`。
- `判断`：这里的“气质”不是市场定位或优劣标签，而是 `同样要处理内容、对象、运行时、子系统、交付与扩展时，复杂度默认被放在哪种组织方式里`。
- `判断`：本篇最安全的比较方式不是做一一语义映射，而是说明 Unity 更常把复杂度收进 `component / package / profile / pipeline` 这类通用工作流容器，Unreal 更常把复杂度收进 `world / framework / module / target / subsystem` 这类显式骨架。
- `判断`：文章 `08` 的结论必须继续明确写成 `基于官方文档结构与前面各篇证据边界得到的工程判断`，而不是伪装成源码级真相。

## 本卡暂不支持的强结论

- 不支持：`Unity` 或 `Unreal` 的整体架构已经完成源码级全量对照
- 不支持：`Prefab / Blueprint`、`Package / Plugin`、`BuildPipeline / UBT`、`DOTS / Mass` 都可以严格一一等价映射
- 不支持：`component/package/workflow-centered` 与 `editor/world/framework-centered` 已经足够推出性能、团队效率或商业成功上的强结论
- 不支持：`Unity` 一定更灵活，或 `Unreal` 一定更重型，这类脱离上下文的宣传式判断
- 不支持：把本篇写成 `Unity vs Unreal` 功能比较、选型建议、产品排名或入门教程
- 不支持：把 `01-07` 的细节重新在本篇重讲一遍，导致总收束失焦

## 下一次最合适的增量

- 基于本卡给 `08` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这两种气质各自把复杂度放在哪里
  3. 这篇不回答什么
  4. Unity 怎么收束
  5. Unreal 怎么收束
  6. 为什么这不是产品优劣比较
  7. 常见误解
  8. 我的结论
