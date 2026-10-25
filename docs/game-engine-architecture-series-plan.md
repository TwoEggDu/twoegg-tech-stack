# 游戏引擎架构地图系列规划

> 本文件是本系列唯一 canonical 篇级目录。执行计划、历史进度表与单篇提纲服务于执行，不反向覆盖这里的当前正文状态。

## 当前目录与本轮增量

2026-10-25 核对：已有一篇导读与00—07八篇正文，本轮增加00A原理补篇，共10篇正文；08仍只有提纲和证据卡。正文成稿、官方资料核对、站点验证与引擎运行分别记录。

| 编号 | 正文 / 计划 | 当前状态 |
| --- | --- | --- |
| 导读 | [从玩家输入到屏幕画面：游戏内容和运行时是怎么汇合的](../content/rendering/player-input-and-game-content-to-screen.md) | 已有正文，保留原URL、系列与导读顺序 |
| 00 | [游戏引擎架构地图 00｜总论：现代游戏引擎到底该怎么分层](../content/system-design/game-engine-architecture-00-overview.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 00A | [游戏引擎架构地图 00A｜引擎怎样分层：职责、依赖与资源服务的边界](../content/system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md) | 正文完成；官方资料、Hugo与生成页检查，模型为设计建议 |
| 01 | [游戏引擎架构地图 01｜为什么游戏引擎首先是一套内容生产工具](../content/system-design/game-engine-architecture-01-content-production.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 02 | [游戏引擎架构地图 02｜Unity 的 GameObject 和 Unreal 的 Actor，到底差在哪](../content/system-design/game-engine-architecture-02-gameobject-vs-actor.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 03 | [游戏引擎架构地图 03｜脚本、反射、GC、任务系统，到底站在引擎的哪一层](../content/system-design/game-engine-architecture-03-runtime-foundation.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 04 | [游戏引擎架构地图 04｜渲染、物理、动画、音频、UI，为什么都像半台小引擎](../content/system-design/game-engine-architecture-04-professional-subsystems.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 05 | [游戏引擎架构地图 05｜资源导入、Cook、Build、Package，为什么也是引擎本体](../content/system-design/game-engine-architecture-05-asset-and-delivery.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 06 | [游戏引擎架构地图 06｜跨平台引擎到底在抽象什么？](../content/system-design/game-engine-architecture-06-platform-abstraction.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 07 | [游戏引擎架构地图 07｜为什么 DOTS 和 Mass 不能只算“一个模块”](../content/system-design/game-engine-architecture-07-dots-and-mass.md) | 已有正文；原文证据边界保留，本轮未逐篇事实审计 |
| 08 | Unity 和 Unreal，到底是什么气质的引擎 | 仅有提纲与证据卡，暂无正文 |

00A 采用“问题 → 职责模型 → 接口依赖 → 资源边界 → 工程例子与取舍”的原理结构，不要求套用原主线的Unity/Unreal逐项对照。执行与验证见[00A提纲](game-engine-architecture-00a-layers-dependencies-outline.md)。

00的六组内容用于覆盖引擎问题，00A画接口依赖与数据交接，不将两者当作同一种严格分层。新模型是作者建议，公开引擎仅用于核对相近边界，不称为统一行业标准。原主线及08拟题保留，后续正文范围以下文为准。

## 系列定位

这组文章不写成 `Unity 教程`，也不写成 `Unreal 入门导览`。

它真正要解决的问题是：

`把现代游戏引擎拆成几层稳定的架构地图，再用 Unity 和 Unreal 去说明每一层各自是怎么落地的。`

这组文章最适合放在你现有两条主线之间：

- 比 `游戏图形系统` 更上层，不只讲渲染
- 比 `数据导向运行时` 更总览，不只讲 DOTS / Mass

它承担的角色是：

`给读者一张“现代游戏引擎到底由哪些层组成”的总地图。`

后面无论读者继续看渲染、DOTS、资源管线、平台适配还是性能文章，都能知道这些内容在整台引擎里站在哪。

## 目标读者

- 已经在用 Unity 或 Unreal，但对“引擎到底分哪几层”还没有稳定地图的人
- 想从“会用引擎”升级到“理解引擎架构”的客户端程序
- 会遇到引擎边界、性能判断、平台差异，但脑子里缺少总框架的人

## 系列核心主线

这组文章不按 `Unity 篇`、`Unreal 篇` 两条线平铺。

更稳的主线是按问题拆：

`内容生产 -> 世界模型与游戏框架 -> 运行时底座 -> 专业子系统 -> 资产与发布 -> 数据导向扩展`

其中最重要的是反复讲清楚三件事：

1. 每一层负责什么
2. 每一层不负责什么
3. Unity 和 Unreal 在这一层为什么会长成不同样子

## 系列结构

规划保留 `1 篇总论 + 8 篇主线 + 若干补篇`；当前正文状态以上表为准，00A是本轮新增补篇。

### 00｜总论：现代游戏引擎到底该怎么分层

核心问题：
现代游戏引擎如果不按零散功能记忆，而按架构理解，最稳定的划分方式是什么。

这一篇要完成的事：

- 先给出整张地图
- 说明为什么“渲染 / 物理 / 音频 / UI”这种教材式分类不够用
- 引入后面整组文章的统一分层

核心结论建议压成一句话：

`现代游戏引擎可以先看成一台由内容生产层、世界与游戏框架层、运行时底座层、专业子系统层、资产与发布层组成的系统机器。`

### 01｜内容生产层：为什么游戏引擎首先是一套生产工具

核心问题：
为什么游戏引擎不是单纯运行库，而首先是一套服务内容生产的工具体系。

Unity 例子：

- Editor
- Inspector
- Prefab
- Package Manager

Unreal 例子：

- Unreal Editor
- Content Browser
- Blueprint Editor
- Plugin

重点不是介绍按钮，而是解释：

`为什么编辑器工作流会反过来塑造引擎架构。`

### 02｜世界模型层：Unity 的 GameObject 和 Unreal 的 Actor 到底差在哪

核心问题：
游戏引擎如何组织“世界里有哪些对象，它们怎样更新、怎样协作”。

Unity 例子：

- Scene
- GameObject
- Component
- MonoBehaviour

Unreal 例子：

- World
- Level
- Actor
- ActorComponent
- Pawn / Controller / GameMode / GameState

这一篇要压清楚：

- Unity 默认是 `Scene -> GameObject -> Component`
- Unreal 默认是 `World -> Level -> Actor -> Component + Gameplay Framework`
- 两者不只是 API 风格不同，而是世界组织方式不同

### 03｜运行时底座层：脚本、反射、GC、任务系统到底站在哪

核心问题：
什么叫“引擎的运行时底座”，为什么 GC、脚本 VM、任务系统都应该放在这一层。

Unity 例子：

- C#
- Mono / IL2CPP
- PlayerLoop
- GC
- Job System
- Burst

Unreal 例子：

- C++ Runtime
- UObject
- Reflection
- GC
- Blueprint VM
- Task Graph

这一篇要专门回答你前面提过的问题：

- `GC` 是运行时机制，不是独立功能模块
- `Job / Task` 是执行底座，不是业务系统

### 04｜专业子系统层：渲染、物理、动画、音频、UI 为什么都长成“引擎里的国中国”

核心问题：
为什么这些系统都属于引擎大模块，但又各自像半台小引擎。

Unity 例子：

- URP / HDRP
- PhysX / 2D Physics
- Animator / Timeline
- Audio
- UGUI / UI Toolkit

Unreal 例子：

- Renderer / Lumen / Nanite
- Chaos Physics
- Animation Blueprint / Sequencer
- Audio Engine / MetaSounds
- UMG / Slate

重点要说明：

`这些系统不是平铺功能点，而是带自己数据结构、调度规则和工具链的“专业子系统”。`

### 05｜资产与发布层：资源导入、构建、打包为什么也是引擎的一部分

核心问题：
为什么资源导入、序列化、Cook / Build / Package 不该被看成外围流程。

Unity 例子：

- Asset Import Pipeline
- Serialization
- Addressables / AssetBundle
- Build Pipeline

Unreal 例子：

- Asset Registry
- Asset Manager
- Cook
- Package
- UnrealBuildTool

这一篇要收回到工程判断：

`很多所谓引擎能力，真正落地时都要经过资产与发布层的重新组织。`

### 06｜平台抽象层：跨平台引擎到底在抽象什么

核心问题：
Unity 和 Unreal 所谓“跨平台”，抽象掉了什么，又保留了什么。

Unity 例子：

- Graphics API backend
- Platform-dependent compilation
- Player settings
- 各平台 Build Target

Unreal 例子：

- RHI
- Platform abstraction
- Target platform
- Build config

重点：

- 跨平台不是“所有平台完全一样”
- 而是在统一抽象上管理不可避免的平台差异

### 07｜数据导向扩展层：DOTS 和 Mass 应该放在整张地图的哪里

核心问题：
为什么 DOTS 和 Mass 不能被粗暴地算成“又一个功能模块”。

Unity 例子：

- Entities
- Burst
- Collections
- Baking

Unreal 例子：

- MassEntity
- MassGameplay
- Representation

这一篇要压成一句话：

`DOTS 和 Mass 不是传统专业子系统，而是对世界模型层和运行时底座层的一次数据导向重构或特区化扩展。`

### 08｜总收束：Unity 和 Unreal 到底是两种什么气质的引擎

核心问题：
把前面所有层收回之后，Unity 和 Unreal 各自最稳定的架构气质是什么。

建议收束成下面两句：

- `Unity 更像一台以组件化、包化和通用工作流为中心的引擎。`
- `Unreal 更像一台以 World/Actor 框架、重编辑器和模块化系统为中心的引擎。`

这篇不要做产品优劣对比。
只做架构气质总结。

## 第一批最值得先写的 4 篇

如果不想一口气拉太长，建议先发这 4 篇：

1. `00｜现代游戏引擎到底该怎么分层`
2. `02｜Unity 的 GameObject 和 Unreal 的 Actor 到底差在哪`
3. `03｜脚本、GC、任务系统到底站在哪`
4. `07｜DOTS 和 Mass 应该放在整张地图的哪里`

这 4 篇先发出去，读者会先拿到：

- 宏观地图
- Unity / Unreal 的默认世界模型
- GC / 任务系统 / 运行时底座的位置
- DOTS / Mass 在整张图里的站位

这正好承接你当前已经在聊的话题。

## 每篇固定结构

为了和你现有系列保持一致，建议每篇尽量复用这个骨架：

1. 这篇要回答什么
2. 这一层负责什么
3. 这一层不负责什么
4. Unity 怎么做
5. Unreal 怎么做
6. 两者差异为什么不是表面 API 差异
7. 常见误解
8. 我的结论

## 题目风格建议

标题不要写成教程口吻。

更适合你的写法是：

- 现代游戏引擎到底该怎么分层
- Unity 的 GameObject 和 Unreal 的 Actor，到底差在哪
- 脚本、GC、任务系统，到底站在引擎的哪一层
- 为什么 DOTS 和 Mass 不能只算“一个模块”

避免写法：

- 今天带你认识 Unity 和 Unreal 的模块
- Unity 与 Unreal 引擎入门
- 一文看懂 DOTS

## 和现有系列的关系

这组文章最适合成为你现有内容的“上位地图”：

- 往下接 `游戏图形系统`
- 横向接 `数据导向运行时`
- 再往工程侧接资源、发布、性能和平台化文章

也就是说，它不是替代你现有系列，而是把现有系列挂到同一张总图上。

## 最后压成一句话

如果这组文章最后只能让读者记住一句话，那应该是：

`Unity 和 Unreal 的差异，不只是功能列表不同，而是它们在内容生产、世界模型、运行时底座、专业子系统和资产发布这几层上，给出了不同的工程组织方式。`
