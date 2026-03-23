# 游戏引擎架构地图 02｜证据卡：Unity 的 GameObject 和 Unreal 的 Actor，到底差在哪

## 本卡用途

- 对应文章：`02`
- 本次增量类型：`证据卡`
- 证据等级：`官方文档`
- 约束原因：`docs/engine-source-roots.md` 中 Unity 与 Unreal 的状态都不是 `READY`，本轮不得声称源码级验证。

## 文章主问题与边界

- 这篇只回答：`两台引擎默认如何组织“世界里的对象”，以及这种差异为什么会影响后续架构。`
- 这篇不展开：`00 总论里的六层总地图`
- 这篇不展开：`03 运行时底座层里 GC、反射、任务系统的内部机制`
- 这篇不展开：`07 数据导向扩展层里 DOTS / Mass 对默认对象世界的改写`
- 这篇不展开：`05/06 里资源构建链与平台抽象的内部实现`
- 本篇允许做的事：`只锁定 Scene / World、GameObject / Actor、Component、MonoBehaviour / Actor lifecycle、Pawn / Controller / GameMode / GameState 这些官方证据边界。`

## 源码可用性

| 引擎 | 当前状态 | 本轮结论边界 |
| --- | --- | --- |
| Unity | `TODO` | 只能引用官方手册与 API，不写“源码显示” |
| Unreal | `TODO` | 只能引用官方文档与 API，不写“源码显示” |

## 官方文档入口与可直接证明的事实

### 1. Unity 先把 Scene 当作世界组织入口

- Unity 入口：
  - [Scenes](https://docs.unity3d.com/Manual/CreatingScenes.html)
  - [Hierarchy window reference](https://docs.unity3d.com/Manual/hierarchy-reference.html)
  - [Transforms](https://docs.unity3d.com/Manual/class-Transform.html)
- 可直接证明的事实：
  - Unity 官方把 Scene 描述为承载游戏环境与菜单的单位，并把每个 Scene 文件视为一个独立 level。
  - Unity 官方说明 Hierarchy 会显示场景中的每个 GameObject，并用它来管理和分组这些对象。
  - Unity 官方说明 Transform 保存位置、旋转、缩放和 parent-child 状态；多层 parent-child 关系会形成 Transform hierarchy。
- 暂定判断：
  - Unity 官方先给出的默认世界入口更接近 `Scene -> Hierarchy -> GameObject hierarchy`，而不是一组单独命名的 gameplay 角色类。

### 2. Unity 的基础对象是 GameObject，功能靠 Component 与 MonoBehaviour 拼装

- Unity 入口：
  - [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html)
  - [Component](https://docs.unity3d.com/ScriptReference/Component.html)
  - [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html)
- 可直接证明的事实：
  - Unity 官方把 GameObject 说明为表示角色、道具、场景物件的基础对象，并明确它本身只是 Components 的容器。
  - Unity 官方说明每个 GameObject 都一定带有 Transform，不能移除，也不能创建没有 Transform 的 GameObject。
  - Unity 官方说明 Component 是一切附着在 GameObject 上的基类，Component 永远附着于某个 GameObject。
  - Unity 官方说明 MonoBehaviour 是大量 Unity 脚本的基类，并且 MonoBehaviour 始终作为 GameObject 的一个 Component 存在。
- 暂定判断：
  - Unity 官方默认对象模型更接近 `对象节点 + 组件组合 + 脚本组件`，而不是先引入一组预制好的 gameplay 角色分工。

### 3. Unity 默认更新/激活链主要暴露在 MonoBehaviour 生命周期里

- Unity 入口：
  - [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html)
  - [Order of Execution for Event Functions](https://docs.unity3d.com/Manual/ExecutionOrder.html)
  - [MonoBehaviour.Start](https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html)
- 可直接证明的事实：
  - Unity 官方明确 MonoBehaviour 提供 lifecycle functions。
  - Unity 官方说明 `Awake` 总是在 `Start` 之前，`OnEnable` 会在对象启用或 MonoBehaviour 实例创建后调用。
  - Unity 官方说明对于场景中的对象，所有脚本的 `Awake` / `OnEnable` 会先于任何 `Start` / `Update` 调用；`Start` 则发生在第一次 `Update` 前。
- 暂定判断：
  - Unity 官方把默认对象世界“怎么进入运行”的主入口，主要暴露成挂在 GameObject 上的脚本生命周期，而不是通过 Pawn / Controller / GameMode 这类框架角色来组织。

### 4. Unreal 显式区分 World、Level 与 Actor

- Unreal 入口：
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
  - [Actor Lifecycle](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-actor-lifecycle)
- 可直接证明的事实：
  - Unreal 官方把 Level 定义为 gameplay area，里面包含 geometry、Pawns 和 Actors，并说明每个 level 会保存成单独的 `.umap`。
  - Unreal 官方把 World 定义为容纳所有 Levels 的容器，并负责 level streaming 与动态 Actor 的 spawning。
  - Unreal 官方把 Actor 定义为可以放进 level 的对象，并说明 Actor 可以通过 C++ 或 Blueprint 在 gameplay 中被创建和销毁。
  - Unreal 官方的 Actor Lifecycle 文档把 Actor 的 load / spawn / initialize / BeginPlay 路径单独展开说明。
- 暂定判断：
  - Unreal 官方把“世界容器”和“对象实例”明确拆成 `World / Level / Actor` 几层来讲，世界组织入口比 Unity 更显式。

### 5. Unreal 的 Component 依附 Actor，但 Actor 自带更强的世界接入语义

- Unreal 入口：
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
  - [Basic Components in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/basic-components-in-unreal-engine)
  - [Actor Lifecycle](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-actor-lifecycle)
- 可直接证明的事实：
  - Unreal 官方说明 Component 是可添加到 Actor 上的一块功能，Component 不能独立存在，必须附着在 Actor 上。
  - Unreal 官方说明给 Actor 添加 Components，本质上是在拼装这个 Actor 的组成部分；即使没有 Blueprint 或 C++ 行为脚本，Actor 也可以作为 level 里的对象存在。
  - Unreal 官方的 Actor Lifecycle 文档把 `PreInitializeComponents`、`InitializeComponent`、`PostInitializeComponents`、`BeginPlay` 作为 Actor 接入世界的关键阶段。
- 暂定判断：
  - Unreal 里的 `Actor + Component` 虽然也是组合关系，但官方文档把它放进了更强的 world / level / lifecycle 管线里，而不只是一个通用容器模型。

### 6. Unreal 官方把 Pawn / Controller / GameMode / GameState 写成默认世界框架的一部分

- Unreal 入口：
  - [Unreal Engine Terminology](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-terminology)
  - [GameFramework API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/GameFramework)
  - [APlayerController](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/GameFramework/APlayerController)
- 可直接证明的事实：
  - Unreal 官方把 Pawn 定义为 Actor 的子类，用作玩家或 AI 在游戏中的 avatar / persona，并明确它可以被 possessed。
  - Unreal 官方说明 PlayerController 负责接收玩家输入并把它转换成游戏交互，且经常会 possess 一个 Pawn 或 Character。
  - Unreal 官方说明 GameMode 负责游戏规则，并且每个 Level 只有一个 GameMode；多人游戏里 GameMode 只存在于服务器。
  - Unreal 官方说明 GameState 是同步给所有客户端的游戏状态容器。
  - Unreal 官方 GameFramework API 直接把 `AController`、`AGameModeBase`、`AGameMode` 等列为正式框架类型。
- 暂定判断：
  - Unreal 默认对象世界不只是一组 `Actor` 实例，而是 `Actor + possession + rules + replicated state` 的框架化世界组织。

## 本轮可以安全落下的事实

- `事实`：Unity 官方文档把 Scene、Hierarchy、GameObject、Transform、Component、MonoBehaviour 连成一条默认对象组织链。
- `事实`：Unity 官方把脚本生命周期主要暴露为挂在 GameObject 上的 MonoBehaviour 事件顺序。
- `事实`：Unreal 官方文档显式区分 World、Level、Actor，并把 Actor 的生命周期单独写成完整文档。
- `事实`：Unreal 官方把 Pawn、PlayerController、GameMode、GameState 作为默认 gameplay framework 的正式组成部分。
- `事实`：`docs/engine-source-roots.md` 目前没有任何 `READY` 的 Unity 或 Unreal 源码根路径，因此本轮不能声称源码级验证。

## 基于这些事实的暂定判断

- `判断`：文章 `02` 可以先把 Unity 的默认世界模型压成 `Scene -> GameObject -> Component / MonoBehaviour -> Transform hierarchy`。
- `判断`：文章 `02` 可以先把 Unreal 的默认世界模型压成 `World -> Level -> Actor -> Component + Gameplay Framework`。
- `判断`：`GameObject` 和 `Actor` 不能只在名词层面一一对照；真正的差异还包括它们各自挂接的世界容器、生命周期暴露方式和 gameplay 框架脚手架。
- `判断`：当前最稳的写法不是下“谁更先进”或“谁更灵活”的产品判断，而是先把两边默认对象世界的组织方式区分清楚。

## 本卡暂不支持的强结论

- 不支持：`Actor 只是 GameObject 的换皮`
- 不支持：`Unity 完全没有 gameplay framework，因此天然更简单`
- 不支持：`Scene / World`、`GameObject / Actor`、`MonoBehaviour / Actor lifecycle` 可以直接做一一等价映射
- 不支持：`对象加载、序列化、所有权、网络复制边界已经完成源码级对照`
- 不支持：`Unity / Unreal 的默认世界模型优劣已经可以下定论`

## 下一次最合适的增量

- 基于本卡给 `02` 建详细提纲。
- 提纲必须沿用固定骨架：
  1. 这篇要回答什么
  2. 这一层负责什么
  3. 这一层不负责什么
  4. Unity 怎么落地
  5. Unreal 怎么落地
  6. 为什么不是表面 API 差异
  7. 常见误解
  8. 我的结论
