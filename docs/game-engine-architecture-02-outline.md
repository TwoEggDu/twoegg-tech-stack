# 游戏引擎架构地图 02｜详细提纲：Unity 的 GameObject 和 Unreal 的 Actor，到底差在哪

## 本提纲用途

- 对应文章：`02`
- 本次增量类型：`详细提纲`
- 证据基础：`docs/game-engine-architecture-02-evidence-card.md`
- 证据等级：`官方文档`
- 约束说明：`docs/engine-source-roots.md` 中 Unity 与 Unreal 仍不是 `READY`，本提纲只安排“官方资料明确写了什么”与“基于这些事实的暂定判断”，不写源码级定论。

## 文章主问题与边界

- 这篇只回答：`两台引擎默认如何组织“世界里的对象”，以及这种差异为什么会影响后续架构。`
- 这篇不展开：`00 总论里的六层总地图`
- 这篇不展开：`03 运行时底座层里 GC、反射、脚本后端、任务系统的内部机制`
- 这篇不展开：`07 数据导向扩展层里 DOTS / Mass 对默认对象世界的重写`
- 这篇不展开：`05/06 里资源构建链、平台抽象、网络复制实现细节`
- 本篇允许落下的判断强度：`先把 Unity 与 Unreal 的默认对象世界压成两套稳定的组织方式，再说明为什么它们不是名词换皮；不做源码级强结论。`

## 一句话中心判断

- `Unity 的默认对象世界更接近 Scene -> GameObject -> Component / MonoBehaviour -> Transform hierarchy；Unreal 的默认对象世界更接近 World -> Level -> Actor -> Component + Gameplay Framework。`
- `因此，GameObject 和 Actor 不能只做名词对照，它们分别挂在两套不同的世界容器、生命周期入口和默认框架脚手架上。`

## 行文顺序与字数预算

| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 这篇要回答什么 | 350 - 500 | 把“Actor 不是 GameObject 换皮”这个问题重新压回世界模型层 |
| 2. 这一层负责什么 | 700 - 950 | 定义世界模型层到底在回答哪些问题，并给出对照表 |
| 3. 这一层不负责什么 | 350 - 500 | 明确本篇不越界到运行时机制、DOTS / Mass、复制细节 |
| 4. Unity 怎么落地 | 750 - 1000 | 按 Scene / Hierarchy / GameObject / Component / MonoBehaviour 铺开默认对象世界 |
| 5. Unreal 怎么落地 | 850 - 1150 | 按 World / Level / Actor / Component / Pawn / Controller / GameMode / GameState 铺开默认对象世界 |
| 6. 为什么不是表面 API 差异 | 500 - 700 | 把两边差异收回到世界组织方式和默认框架结构 |
| 7. 常见误解 | 450 - 650 | 集中拆掉几种最容易把两边压成一一映射的误读 |
| 8. 我的结论 | 250 - 400 | 收束成一句架构判断，并挂回 03 / 07 等后续文章 |

## 详细结构

### 1. 这篇要回答什么

- 开篇切口：
  - 先写常见问法：`Unity 的 GameObject 和 Unreal 的 Actor 到底是不是一个东西。`
  - 再指出这个问法的问题不在名词，而在默认对象世界的组织方式。
- 要抛出的核心问题：
  - `两台引擎默认如何组织“世界里的对象”，以及这种差异为什么会影响后续架构。`
- 这一节要完成的动作：
  - 把问题从“术语对照”改写成“世界模型对照”
  - 说明为什么 Scene / World、GameObject / Actor、Component、生命周期、gameplay framework 必须放在同一篇里谈
  - 说明本篇不负责解释 GC、反射、序列化、网络复制的底层实现
- 可直接引用的证据锚点：
  - 证据卡 `1` 到 `6`
- 本节事实与判断分界：
  - `事实`：Unity 与 Unreal 官方文档都把“世界中的对象如何存在与更新”写成正式概念体系
  - `判断`：所以这篇真正比较的不是两个类名，而是两套默认对象世界

### 2. 这一层负责什么

- 本节要先定义“世界模型层”到底负责回答什么：
  - 世界入口是什么
  - 世界里允许存在哪些对象
  - 对象靠什么组合能力
  - 对象怎样进入运行状态
  - 默认 gameplay 框架是否已经嵌进对象世界
- 建议放一张总对照表：

| 对照维度 | Unity 默认入口 | Unreal 默认入口 | 本节要压出的意思 |
| --- | --- | --- | --- |
| 世界容器 | Scene | World / Level | 两边都先给世界入口，但容器层级不一样 |
| 基础对象 | GameObject | Actor | 都是世界中的对象节点，但站位不同 |
| 组合方式 | Component / MonoBehaviour | Component | 都支持组合，但默认框架依附程度不同 |
| 生命周期入口 | MonoBehaviour event functions | Actor lifecycle / BeginPlay | 运行入口暴露方式不同 |
| 默认 gameplay 脚手架 | 不先显式给出一组框架角色 | Pawn / Controller / GameMode / GameState | Unreal 更早把玩法角色框架写进默认世界 |

- 本节必须压出的判断：
  - `世界模型层不是“对象系统”这个抽象词，而是回答“世界靠什么被组织起来”的一整层。`
  - `如果不先分清这一层，后面会把对象容器、运行入口、gameplay 角色和运行时机制全部混线。`
- 证据锚点：
  - 证据卡 `1`、`2`、`3`、`4`、`5`、`6`
- 本节事实与判断分界：
  - `事实`：官方文档能直接支持上述五个对照维度
  - `判断`：这五个维度足以把 Unity / Unreal 的默认对象世界区分开

### 3. 这一层不负责什么

- 必须明确写出的边界：
  - 不把 `Scene / World` 写成一一等价术语表
  - 不解释 `GC`、`Reflection`、`IL2CPP`、`Blueprint VM`、`Task Graph` 的内部机制
  - 不把 `DOTS / Mass` 当作本篇主角
  - 不把 `网络复制`、`所有权`、`GameState 同步` 的实现细节在本篇写透
  - 不做 “Unity 更简单” 或 “Unreal 更高级” 的产品判断
  - 不写任何编辑器入门教程
- 建议用一段“为什么必须克制”收尾：
  - 如果把 gameplay framework、运行时底座、网络和数据导向扩展全部揉进来，这篇会从世界模型文章塌成百科

### 4. Unity 怎么落地

- 本节只沿着 Unity 官方默认对象链条往下写，不做功能导览

#### 4.1 Scene 是 Unity 默认对象世界的入口

- 可用材料：
  - Scenes
  - Hierarchy window reference
- 可落下的事实：
  - Scene 是承载游戏环境与菜单的单位，也是独立 level 文件
  - Hierarchy 显示并管理场景中的每个 GameObject
- 可落下的暂定判断：
  - `Unity 默认先把“世界”理解成一个场景中的对象层级，而不是先声明一组玩法角色类型。`
- 证据锚点：
  - 证据卡 `1`

#### 4.2 GameObject 是基础对象节点，但它本身主要是容器

- 可用材料：
  - GameObject
  - Component
- 可落下的事实：
  - GameObject 是角色、道具、场景物件的基础对象
  - GameObject 本身是 Components 的容器
  - 每个 GameObject 必带 Transform
- 可落下的暂定判断：
  - `Unity 的默认对象节点天然绑定了“空间存在 + 组件挂接”这件事。`
- 证据锚点：
  - 证据卡 `2`

#### 4.3 Component 与 MonoBehaviour 共同构成默认行为挂接方式

- 可用材料：
  - Component
  - MonoBehaviour
- 可落下的事实：
  - Component 永远附着在 GameObject 上
  - MonoBehaviour 始终作为 GameObject 的一个 Component 存在
- 可落下的暂定判断：
  - `Unity 默认不是先给玩法角色分工，而是先给对象节点，再靠组件和脚本组件把行为拼出来。`
- 证据锚点：
  - 证据卡 `2`

#### 4.4 MonoBehaviour 生命周期是默认对象世界进入运行状态的主要暴露面

- 可用材料：
  - MonoBehaviour
  - Order of Execution for Event Functions
  - MonoBehaviour.Start
- 可落下的事实：
  - Unity 官方明确提供 `Awake`、`OnEnable`、`Start`、`Update` 等生命周期顺序
  - 场景中对象的生命周期入口主要作为脚本事件顺序暴露
- 可落下的暂定判断：
  - `Unity 默认对象世界的运行入口，主要从挂在对象上的脚本生命周期进入，而不是先通过一组玩法框架角色来分工。`
- 证据锚点：
  - 证据卡 `3`

#### 4.5 本节收口

- 必须收成一句话：
  - `Unity 默认对象世界更像“场景里的组件化对象层级”，其核心问题是对象节点怎样靠组件和脚本组件被组织与驱动。`
- 必须明确的边界提醒：
  - 这还不是 DOTS
  - 这也不是在讲 GC 或脚本后端

### 5. Unreal 怎么落地

- 本节沿着 Unreal 官方默认对象链条往下写，不做产品介绍

#### 5.1 World / Level 是 Unreal 显式写出来的世界容器

- 可用材料：
  - Unreal Engine Terminology
  - Actor Lifecycle
- 可落下的事实：
  - Level 是 gameplay area，保存为 `.umap`
  - World 容纳所有 Levels，并负责 level streaming 与动态 Actor spawning
- 可落下的暂定判断：
  - `Unreal 对“世界容器”讲得比 Unity 更显式，先有 World / Level，再谈能放进去的对象。`
- 证据锚点：
  - 证据卡 `4`

#### 5.2 Actor 是世界中的对象，但它从一开始就嵌进了更强的世界语义

- 可用材料：
  - Unreal Engine Terminology
  - Actor Lifecycle
- 可落下的事实：
  - Actor 是可放进 level 的对象，可在 gameplay 中创建和销毁
  - Actor lifecycle 被单独展开成完整文档
- 可落下的暂定判断：
  - `Actor 不是一个纯容器名词，它默认处在更完整的 world / level / lifecycle 管线里。`
- 证据锚点：
  - 证据卡 `4`

#### 5.3 Component 依附 Actor，但 Unreal 的对象世界不止于“Actor + Component”

- 可用材料：
  - Basic Components in Unreal Engine
  - Actor Lifecycle
- 可落下的事实：
  - Component 必须附着在 Actor 上
  - `PreInitializeComponents`、`InitializeComponent`、`PostInitializeComponents`、`BeginPlay` 是关键阶段
- 可落下的暂定判断：
  - `Unreal 也有组合式对象，但官方把组合关系放进了更强的世界接入流程，而不只是一个通用组件容器模型。`
- 证据锚点：
  - 证据卡 `5`

#### 5.4 Pawn / Controller / GameMode / GameState 把默认 gameplay framework 提前写进对象世界

- 可用材料：
  - Unreal Engine Terminology
  - GameFramework API
  - APlayerController
- 可落下的事实：
  - Pawn 是可被 possessed 的 Actor 子类
  - PlayerController 负责输入并常常 possess Pawn 或 Character
  - GameMode 负责规则，GameState 负责同步到客户端的游戏状态
- 可落下的暂定判断：
  - `Unreal 默认对象世界从一开始就不只是“世界里有一堆对象”，而是“世界里已经预留了一组玩法角色分工”。`
- 证据锚点：
  - 证据卡 `6`

#### 5.5 本节收口

- 必须收成一句话：
  - `Unreal 默认对象世界更像“世界容器 + Actor 对象 + 组件组合 + gameplay framework”的框架化组织。`
- 必须明确的边界提醒：
  - 这还不是在讲网络复制实现
  - 这也不是在讲反射、GC、Blueprint VM 的底层机制

### 6. 为什么不是表面 API 差异

- 本节要把前两节的材料收回成 3 个判断：
  - `差异一`：Unity 默认从 `Scene / Hierarchy / GameObject` 进入世界，Unreal 默认从 `World / Level / Actor` 进入世界
  - `差异二`：Unity 的默认行为入口更像脚本组件生命周期；Unreal 的默认行为入口更像 Actor 生命周期与 gameplay framework 角色分工
  - `差异三`：Unreal 早早就把 `Pawn / Controller / GameMode / GameState` 写进默认对象世界，因此它不是只在名词上比 Unity 多几个类
- 建议放一段对照性的收束：
  - `GameObject` 与 `Actor` 都能承载组件，但两边真正不同的是对象如何被放进世界、怎样进入运行、是否已经预留默认玩法框架。`
- 本节事实与判断分界：
  - `事实`：官方文档能直接支持世界容器、对象节点、生命周期入口、框架角色这四类差异
  - `判断`：所以两边差异属于世界组织方式，不是表面 API 风格差异

### 7. 常见误解

- 误解 `1`：
  - `Actor 就是 GameObject 的换皮`
  - 纠正方式：指出 Unreal 还同时显式给出 World / Level / Actor lifecycle / gameplay framework
- 误解 `2`：
  - `Unity 没有 gameplay framework，所以它只是对象和组件的堆叠`
  - 纠正方式：强调本篇只谈“默认显式框架入口”，不等于否认 Unity 上层可以组织出自己的玩法框架
- 误解 `3`：
  - `Scene 就等于 World，GameObject 就等于 Actor，可以直接两两对应`
  - 纠正方式：指出这些名词站在不同组织层级，不能简单做字典映射
- 误解 `4`：
  - `Component 模型看起来类似，所以两边的默认对象世界本质一样`
  - 纠正方式：强调组件相似不等于世界容器、生命周期和默认玩法角色结构相同
- 误解 `5`：
  - `这一篇应该顺手把复制、GC、DOTS、Mass 一起讲完`
  - 纠正方式：回到系列边界，说明这些分别属于后续文章

### 8. 我的结论

- 收束顺序建议：
  - 先重申主问题不是类名对照，而是默认对象世界如何组织
  - 再重申能直接成立的事实
  - 最后给出工程判断
- 本段必须写出的事实：
  - Unity 官方文档支持 `Scene -> GameObject -> Component / MonoBehaviour -> Transform hierarchy`
  - Unreal 官方文档支持 `World -> Level -> Actor -> Component + Gameplay Framework`
  - 当前没有本地 `READY` 的源码根路径
- 本段必须写出的判断：
  - `GameObject` 和 `Actor` 不能只做名词对照；两边真正差异在默认对象世界的组织方式
- 结尾过渡：
  - `03` 会继续回答“这些对象世界背后的运行时底座站在哪”
  - `07` 会继续回答“DOTS / Mass 怎样改写默认对象世界”

## 起草时必须保留的一张对照表

| 对照维度 | Unity | Unreal | 本文要落下的判断 |
| --- | --- | --- | --- |
| 世界入口 | Scene / Hierarchy | World / Level | 世界容器组织方式不同 |
| 基础对象 | GameObject | Actor | 不能只看名字相似 |
| 组合方式 | Component / MonoBehaviour | Component | 组合关系相似，但框架上下文不同 |
| 运行入口 | MonoBehaviour 生命周期 | Actor lifecycle / BeginPlay | 进入运行状态的主暴露面不同 |
| 默认框架 | 不先显式给出 Pawn / Controller / GameMode / GameState 这组角色 | 明确给出 gameplay framework 类型 | Unreal 默认更框架化 |

## 可直接拆出的两条短观点

- `GameObject 和 Actor 看起来都像“对象”，但它们各自挂在两套不同的世界组织方式上。`
- `Unity 和 Unreal 的差异不只在组件写法，而在默认对象世界是否已经预埋玩法框架。`

## 起草时必须反复自检的三件事

- `我有没有把这篇写成术语对照表，而不是世界模型文章`
- `我有没有把 GC、复制、DOTS / Mass、平台与构建链的细节抢写进来`
- `我有没有把事实和判断明确分开，并持续提醒当前没有源码级验证`
