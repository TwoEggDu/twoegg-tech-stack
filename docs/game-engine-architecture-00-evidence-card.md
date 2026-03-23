# 游戏引擎架构地图 00｜证据卡：现代游戏引擎到底该怎么分层

## 本卡用途

- 对应文章：`00`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`如果不按零散功能记忆，而按架构理解，现代游戏引擎最稳定的分层方式是什么。`
- 这篇不展开：`02 世界模型层里 Unity GameObject 与 Unreal Actor 的细部差异`
- 这篇不展开：`03 运行时底座层里 GC、脚本 VM、任务系统的内部机制`
- 这篇不展开：`07 数据导向扩展层里 DOTS / Mass 的详细站位`
- 这篇不展开：`05/06 中 Build/Cook/Package 与平台抽象的内部实现`
- 本篇允许做的事：`先给出一张总地图，并解释为什么编辑器、世界模型、运行时底座、专业子系统、资产与发布要分层看。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档和 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. 内容生产层不是外围工具

- Unity 入口：
  - [Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html)
  - [Unity's Package Manager](https://docs.unity3d.com/es/2019.4/Manual/Packages.html)
- Unreal 入口：
  - [Unreal Engine Interface and Navigation](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-interface-and-navigation)
  - [Unreal Engine for Unity Developers](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-for-unity-developers)
- 可直接证明的事实：
  - Unity 官方把 package 说明为可交付 `Editor tools and libraries`、`Runtime tools and libraries`、`Asset collections`、`Project templates`。
  - Unity 官方把 Package Manager 放进 Editor 主菜单，并用于安装、更新、禁用包和 feature sets。
  - Unreal 官方单独给出 Editor 界面与导航文档。
  - Unreal 官方在 Unity 开发者迁移目录里把 Editor UI、systems/workflows、rendering、game objects、writing code 分成并列入口。
- 暂定判断：
  - `内容生产层` 可以在总地图里独立成立，因为两边官方文档都把编辑器与工作流当成引擎本体的一部分，而不是外围附录。

### 2. 世界模型层需要单独站出来

- Unity 入口：
  - [GameObjects](https://docs.unity3d.com/ru/2019.4/Manual/GameObjects.html)
- Unreal 入口：
  - [GameFramework API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/GameFramework)
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
- 可直接证明的事实：
  - Unity 官方把 GameObject 描述为最重要的概念，并明确 GameObject 需要靠 Components 获得行为。
  - Unreal 官方术语文档把 Object / UObject、Class、Game State 等作为基础术语。
  - Unreal 官方 GameFramework API 把 Actor 作为能放置或生成到 level 的基类，并继续展开 Pawn、Character 等 gameplay 类型。
- 暂定判断：
  - `世界模型层` 必须在总论里单列，否则会把“对象如何存在、更新、协作”的问题和渲染、物理、音频等专业子系统混成一层。

### 3. 运行时底座层不是“又一个功能模块”

- Unity 入口：
  - [IL2CPP Overview](https://docs.unity3d.com/cn/2023.2/Manual/IL2CPP.html)
  - [Introduction to IL2CPP](https://docs.unity3d.com/jp/current/Manual/il2cpp-introduction.html)
- Unreal 入口：
  - [Unreal Engine Reflection System](https://dev.epicgames.com/documentation/en-us/unreal-engine/reflection-system-in-unreal-engine)
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
- 可直接证明的事实：
  - Unity 官方把 IL2CPP 定义为 scripting backend，并明确它负责把 IL 转成 C++ 再生成平台原生二进制。
  - Unreal 官方明确 `UObject` 是对象基类，并实现 garbage collection、metadata、serialization。
  - Unreal 官方 Reflection System 文档把反射宏与 editor/runtime 功能连在一起说明。
- 暂定判断：
  - `运行时底座层` 可以在总论中独立成立，因为两边官方资料都把脚本、对象、反射、GC 视为底层机制，而不是把它们当成和渲染、音频同级的平铺功能项。

### 4. 专业子系统层与渲染能力不是“整台引擎的全部”

- Unity 入口：
  - [Universal RP](https://docs.unity3d.com/cn/2019.3/Manual/com.unity.render-pipelines.universal.html)
- Unreal 入口：
  - [Unreal Engine for Unity Developers](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-for-unity-developers)
- 可直接证明的事实：
  - Unity 官方把 URP 写成单独的 render pipeline 包，并明确它是 prebuilt Scriptable Render Pipeline。
  - Unreal 官方在 Unity 开发者迁移目录中把 rendering 单列为一组文档入口，同时并列于 game objects、systems/workflows、writing code。
- 暂定判断：
  - 在总论中把渲染、物理、动画、音频、UI 收拢成 `专业子系统层` 更稳，比直接拿教材式功能列表当总分层更贴近官方文档结构。

### 5. 资产与发布层是引擎体系内的交付能力

- Unity 入口：
  - [Build Settings](https://docs.unity3d.com/es/2019.4/Manual/BuildSettings.html)
  - [IL2CPP Overview](https://docs.unity3d.com/cn/2023.2/Manual/IL2CPP.html)
- Unreal 入口：
  - [Packaging Your Project](https://dev.epicgames.com/documentation/en-us/unreal-engine/packaging-your-project)
  - [Building Multi-Platform Projects in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/building-multi-platform-games-in-unreal-engine)
- 可直接证明的事实：
  - Unity 官方把 target platform、Scenes in Build、build settings、IL2CPP native binary 都放进正式手册。
  - Unreal 官方把 packaging 定义为把项目转换成 standalone executable 或 application 的过程。
  - Unreal 官方把 Build、Cook、Stage、Package 解释成构建操作的一部分，并把 Cook、Package、Deploy、Run 作为关键 build operations。
- 暂定判断：
  - `资产与发布层` 不能被当成外围脚本或最后一步按钮，它应在总图里与内容生产、世界模型、运行时底座并列。

### 6. 数据导向扩展层值得单独预留

- Unity 入口：
  - [Entities](https://docs.unity3d.com/cn/2023.2/Manual/com.unity.entities.html)
- Unreal 入口：
  - [MassGameplay Overview](https://dev.epicgames.com/documentation/en-us/unreal-engine/overview-of-mass-gameplay-in-unreal-engine)
  - [MassEntity API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/MassEntity)
- 可直接证明的事实：
  - Unity 官方把 Entities 包定义为现代 Entity Component System implementation，并配套 systems 和 components。
  - Unreal 官方把 Mass Entity 定义为 data-oriented calculations framework。
  - Unreal 官方说明 MassGameplay 直接建立在 Mass Entity 之上，并继续覆盖 world representation、spawning、LOD、replication、StateTree 等子系统。
- 暂定判断：
  - `数据导向扩展层` 可以在总图里作为单独层位预留，因为两边官方都把它写成独立框架或插件族，而不是某个单点功能开关。
  - 但 `DOTS / Mass` 的精确站位仍应留到 `07` 的独立证据卡里再做强判断。

## 本轮可以安全落下的事实

- `事实`：Unity 官方 docs 同时覆盖 package/editor、GameObject/component、IL2CPP、URP、Build Settings、Entities 这些不同方向，并把它们都视为 Unity 正式能力面的一部分。
- `事实`：Unreal 官方 docs 同时覆盖 editor interface、UObject/reflection、Actor/GameFramework、rendering guide、packaging/build operations、Mass 这些不同方向，并把它们分成并列的正式入口。
- `事实`：`docs/engine-source-roots.md` 目前没有任何 `READY` 的 Unity 或 Unreal 源码根路径。
- `事实`：因此本轮证据只能停在官方文档层，不能写成源码级定论。

## 基于这些事实的暂定判断

- `判断`：文章 `00` 可以先把现代游戏引擎压成一张 `内容生产层 -> 世界模型层 -> 运行时底座层 -> 专业子系统层 -> 资产与发布层 -> 数据导向扩展层` 的总地图。
- `判断`：这张图当前最稳的作用不是逐层写尽，而是先回答“为什么不能只按渲染 / 物理 / 音频 / UI 记引擎”。
- `判断`：在源码路径未就绪前，总论里可以提出“编辑器和构建链属于引擎本体”“GC 与 DOTS 不应被压成同一类问题”这类高层判断，但必须明确写成 `基于官方文档结构与工程组织方式得到的暂定判断`。

## 本卡暂不支持的强结论

- 不支持：`Unity 与 Unreal 的内部层边界已经被源码逐项验证`
- 不支持：`GC、Job System、Task Graph、DOTS、Mass 的精确站位已经落实到源码调用链`
- 不支持：`RHI、Build/Cook/Package、Package Manager 的内部耦合强度已经完成源码级比较`

## 下一次最合适的增量

- 基于本卡给 `00` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么不是表面 API 差异
  7. 常见误解
  8. 我的结论
