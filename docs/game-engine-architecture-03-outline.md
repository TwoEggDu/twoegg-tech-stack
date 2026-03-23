# 游戏引擎架构地图 03｜详细提纲：脚本、反射、GC、任务系统，到底站在引擎的哪一层

## 本提纲用途

- 对应文章：`03`
- 本次增量类型：`详细提纲`
- 证据基础：`docs/game-engine-architecture-03-evidence-card.md`
- 证据等级：`官方文档`
- 约束说明：`docs/engine-source-roots.md` 中 Unity 与 Unreal 仍不是 `READY`，本提纲只安排“官方资料明确写了什么”与“基于这些事实的暂定判断”，不写源码级定论。

## 文章主问题与边界

- 这篇只回答：`什么叫引擎的运行时底座，为什么脚本后端、反射、GC、任务系统不该被当成功能模块平铺。`
- 这篇不展开：`00 总论里的六层总地图`
- 这篇不展开：`02 里 Scene / World、GameObject / Actor 的默认对象世界差异`
- 这篇不展开：`07 里 DOTS / Mass 对世界模型与执行模型的重构`
- 这篇不展开：`04/05/06 里渲染、资源发布链、平台抽象的内部实现`
- 这篇不做：`C# vs C++ 的语言优劣比较、Blueprint VM / IL2CPP / GC 的实现级性能结论`
- 本篇允许落下的判断强度：`先把 Unity 与 Unreal 里“代码怎么运行、对象怎么被识别、内存怎么被管理、任务怎么被调度”压回运行时底座层，再说明它为什么不是一排表面功能名词；不做源码级强结论。`

## 一句话中心判断

- `Unity 这层更接近 scripting backend + PlayerLoop + GC / reflection + Job System 共同组成默认执行骨架；Unreal 这层更接近 C++ + UObject / Reflection + Blueprint + Task Graph 共同组成对象与执行底座。`
- `因此，脚本后端、反射、GC、任务系统不是和渲染、物理、音频并列的业务模块，而是决定代码如何运行、对象如何被识别、内存如何被管理、任务如何被调度的运行时底座。`

## 行文顺序与字数预算

| 正文部分 | 目标字数 | 本段任务 |
| --- | --- | --- |
| 1. 这篇要回答什么 | 350 - 500 | 把“GC / Job System / Task Graph 到底算什么”重新压回运行时底座问题 |
| 2. 这一层负责什么 | 700 - 950 | 定义运行时底座层到底负责哪些边界，并给出总对照表 |
| 3. 这一层不负责什么 | 350 - 500 | 明确本篇不越界到世界模型、DOTS / Mass、专业子系统和实现级性能判断 |
| 4. Unity 怎么落地 | 850 - 1100 | 按 scripting backend / PlayerLoop / GC / reflection / Job System 铺开 Unity 的运行时底座 |
| 5. Unreal 怎么落地 | 850 - 1100 | 按 C++ / UObject / Reflection / Blueprint / Task Graph 铺开 Unreal 的运行时底座 |
| 6. 为什么不是表面 API 差异 | 500 - 700 | 把两边差异收回到执行方式、对象元信息、内存与调度组织方式 |
| 7. 常见误解 | 450 - 650 | 集中拆掉把这层写成功能列表或语言之争的误读 |
| 8. 我的结论 | 250 - 400 | 收束成一句架构判断，并挂回 07 等后续文章 |

## 详细结构

### 1. 这篇要回答什么

- 开篇切口：
  - 先写常见问法：`GC 到底算运行时、性能问题，还是引擎里的一个模块？`
  - 再写常见误读：`Job System / Task Graph 只是优化工具，Blueprint / IL2CPP 只是脚本实现细节。`
- 要抛出的核心问题：
  - `什么机制决定代码怎么运行、对象怎么被引擎识别、内存何时被回收、任务怎样被调度。`
- 这一节要完成的动作：
  - 把问题从“低层名词列表”改写成“运行时底座层”
  - 说明为什么脚本后端、反射、GC、任务系统必须放在同一篇里谈
  - 明确这篇不负责解释世界模型、渲染、资源链、平台抽象
- 可直接引用的证据锚点：
  - 证据卡 `1` 到 `5`
- 本节事实与判断分界：
  - `事实`：Unity 与 Unreal 官方都把这些能力写进正式编程 / 运行时文档体系
  - `判断`：所以这篇真正讨论的不是几个低层术语，而是引擎的运行时底座

### 2. 这一层负责什么

- 本节要先定义“运行时底座层”到底负责回答什么：
  - 代码以什么形态进入运行时
  - 引擎怎样识别对象、类型和元信息
  - 内存与对象生命周期靠什么机制被管理
  - 每帧执行顺序和任务调度靠什么骨架组织
- 建议放一张总对照表：

| 对照维度 | Unity 运行时底座 | Unreal 运行时底座 | 本节要压出的意思 |
| --- | --- | --- | --- |
| 代码执行入口 | scripting backend（Mono / IL2CPP） | C++ + Blueprint scripting | 先分清代码怎样进入正式运行时 |
| 对象与元信息 | C# reflection 与 Unity 运行时缓存约束 | UObject + Reflection System | 引擎如何识别对象，不只是语言语法问题 |
| 内存管理 | Mono / IL2CPP 共享 GC 约束 | UObject 提供 GC 等基础能力 | GC 属于运行时机制，不是业务模块 |
| 执行骨架 | PlayerLoop + MonoBehaviour event order | 对象运行时 + Blueprint scripting + Task Graph | 每帧执行与任务调度有正式地基 |
| 并行调度 | Job System 共享 worker threads | FTaskGraphInterface 管理线程与任务 | 任务系统先是执行基础设施，再谈优化 |

- 本节必须压出的判断：
  - `运行时底座层不是“性能优化专区”，而是回答“整台引擎靠什么运行起来”的一整层。`
  - `如果不先分清这一层，后面会把脚本后端、对象元信息、GC、任务调度和专业子系统全部混写。`
- 证据锚点：
  - 证据卡 `1`、`2`、`3`、`4`、`5`
- 本节事实与判断分界：
  - `事实`：官方文档能直接支持代码入口、对象元信息、GC、执行骨架、任务调度这五个维度
  - `判断`：这五个维度足以把“运行时底座层”从功能模块列表里单独拎出来

### 3. 这一层不负责什么

- 必须明确写出的边界：
  - 不把 `Scene / World`、`GameObject / Actor` 的默认对象组织再讲一遍
  - 不把 `DOTS / Mass` 当作本篇主角
  - 不解释渲染、物理、动画、音频、UI 的内部执行细节
  - 不做 `C# vs C++` 谁更好、`Blueprint` 是否“慢”、`GC` 是否“落后”的产品或性能裁判
  - 不把 `IL2CPP`、`Blueprint VM`、`Task Graph` 写成实现级源码深挖
  - 不写任何编辑器或脚本入门教程
- 建议用一段“为什么必须克制”收尾：
  - 如果把世界模型、运行时底座、数据导向扩展和专业子系统全揉进来，这篇会从架构文章塌成术语百科

### 4. Unity 怎么落地

- 本节只沿着 Unity 官方给出的运行时链条往下写，不做功能导览

#### 4.1 scripting backend 决定脚本如何真正进入运行时

- 可用材料：
  - Overview of .NET in Unity
  - IL2CPP Overview
- 可落下的事实：
  - Unity 官方明确不同平台会使用不同 scripting backends
  - Unity 官方明确区分 `JIT` 与 `AOT`
  - Unity 官方明确 `IL2CPP` 会在构建时把 IL 转成 C++，再生成平台原生二进制
- 可落下的暂定判断：
  - `Unity 的脚本后端不是外围构建工具，而是决定代码最终以什么形态进入运行时的底座能力。`
- 证据锚点：
  - 证据卡 `1`

#### 4.2 PlayerLoop 是 Unity 默认执行骨架，脚本生命周期挂在这套骨架上

- 可用材料：
  - LowLevel.PlayerLoop
  - Order of Execution for Event Functions
- 可落下的事实：
  - PlayerLoop 文档明确它代表 Unity player loop，并暴露原生系统的 update order
  - `Awake` / `OnEnable` / `Start` / `Update` / `LateUpdate` / `FixedUpdate` 被官方作为统一执行顺序说明
- 可落下的暂定判断：
  - `PlayerLoop + 生命周期` 更像 Unity 默认执行骨架，而不是挂在渲染、物理、UI 旁边的一项功能点
- 证据锚点：
  - 证据卡 `2`

#### 4.3 GC 与 reflection 不是附件，而是 Unity 运行时对象与内存模型的约束

- 可用材料：
  - Overview of .NET in Unity
  - C# reflection overhead
- 可落下的事实：
  - Unity 官方说明 `Mono` 与 `IL2CPP` 都使用 `Boehm garbage collector`
  - Unity 官方说明默认启用 incremental GC
  - Unity 官方说明 reflection 对象会被缓存，Unity 不会自动释放这些缓存，因此 GC 会持续扫描它们
- 可落下的暂定判断：
  - `GC / reflection` 在 Unity 里不是独立业务模块，而是直接影响对象元信息与内存扫描方式的运行时机制
- 证据锚点：
  - 证据卡 `3`

#### 4.4 Job System 扩展的是执行地基，而不是新增一个功能分区

- 可用材料：
  - Job system overview
- 可落下的事实：
  - Unity 官方把 Job System 描述为允许用户代码与 Unity 共享 worker threads 的多线程系统
  - Unity 官方说明 worker threads 数量与可用 CPU core 匹配
  - Unity 官方说明 Job System 会把 blittable 数据复制到 native memory，并在 managed / native 之间用 `memcpy`
- 可落下的暂定判断：
  - `Job System` 首先在回答“任务如何被安全地调度到执行地基上”，而不是和渲染、动画、音频并列的业务模块
- 证据锚点：
  - 证据卡 `3`

#### 4.5 本节收口

- 必须收成一句话：
  - `Unity 的运行时底座更像“脚本后端 + 默认执行骨架 + 反射与 GC 约束 + 任务调度设施”的组合。`
- 必须明确的边界提醒：
  - 这还不是 `DOTS / Burst`
  - 这也不是在讲渲染或资源链

### 5. Unreal 怎么落地

- 本节只沿着 Unreal 官方给出的运行时链条往下写，不做产品导览

#### 5.1 Unreal 的正式编程入口不是“只有 C++”，而是 C++ 接入一整套对象运行时

- 可用材料：
  - Programming with C++ in Unreal Engine
- 可落下的事实：
  - Unreal 官方把 `Programming with C++` 作为正式编程入口
  - 同一组基础编程文档里并列出现 `Objects`、`Reflection System` 等内容
- 可落下的暂定判断：
  - `Unreal 的运行时底座不是“纯 C++ 自己跑”，而是从正式编程入口起就接入引擎对象系统。`
- 证据锚点：
  - 证据卡 `4`

#### 5.2 UObject + Reflection System 定义了 Unreal 的对象运行时基础

- 可用材料：
  - Objects in Unreal Engine
  - Reflection System in Unreal Engine
- 可落下的事实：
  - Unreal 官方明确 `UObject` 是 Unreal 对象的基类
  - Unreal 官方明确反射系统通过 `UCLASS`、`USTRUCT` 等宏把类接入引擎与编辑器功能
  - Unreal 官方 `Objects` 文档明确列出 `UObject` 提供 `garbage collection`、`reflection`、`serialization`、`automatic editor integration`、`runtime type information`
- 可落下的暂定判断：
  - `UObject + Reflection System` 不只是元数据工具，而是 Unreal 如何识别对象、管理对象并让对象接入引擎能力的运行时基础
- 证据锚点：
  - 证据卡 `4`

#### 5.3 Blueprint 是正式 gameplay scripting system，而不是仅仅一个可视化编辑器功能

- 可用材料：
  - Unreal Engine Terminology
  - Programming with C++ in Unreal Engine
- 可落下的事实：
  - Unreal 官方把 Blueprint 定义为完整的 gameplay scripting system
  - Blueprint 不是脱离对象系统存在的脚本玩具，而是站在 Unreal 正式编程体系内
- 可落下的暂定判断：
  - `Blueprint` 在本篇里应被看作脚本执行入口的一部分，而不是只看成编辑器便利功能
- 证据锚点：
  - 证据卡 `5`

#### 5.4 Task Graph 是正式运行时调度接口，而不是“优化插件”

- 可用材料：
  - FTaskGraphInterface
  - FTaskGraphInterface::AttachToThread
- 可落下的事实：
  - Unreal 官方 API 直接把 task graph 写成正式接口
  - API 暴露 `GetNumWorkerThreads`、`IsMultithread`、`IsThreadProcessingTasks` 等线程与调度能力
  - `AttachToThread` 文档明确外部线程需要被显式接入 task graph system 才能被其识别和管理
- 可落下的暂定判断：
  - `Task Graph` 首先在回答 Unreal 里的任务与线程怎样进入正式调度系统，而不是一个外挂优化模块
- 证据锚点：
  - 证据卡 `5`

#### 5.5 本节收口

- 必须收成一句话：
  - `Unreal 的运行时底座更像“C++ 执行入口 + UObject / Reflection 对象运行时 + Blueprint 脚本系统 + Task Graph 调度地基”的组合。`
- 必须明确的边界提醒：
  - 这还不是在讲世界模型
  - 这也不是在讲渲染、物理、资源链或平台抽象

### 6. 为什么不是表面 API 差异

- 本节要把前两节的材料收回成 4 个判断：
  - `差异一`：Unity 先把脚本代码接进 `Mono / IL2CPP` 与 `PlayerLoop`；Unreal 先把正式编程接进 `C++ + UObject / Reflection + Blueprint`
  - `差异二`：Unity 的 `GC / reflection` 与 Unreal 的 `UObject / reflection / GC` 都是在定义对象与内存怎样被运行时识别和管理
  - `差异三`：`Job System` 与 `Task Graph` 的共性不是“名字都像并行工具”，而是它们都在回答任务如何被正式调度
  - `差异四`：这层差异不是表面语法差异，而是执行入口、对象元信息、内存模型与调度地基的组织方式不同
- 建议放一段收束性的对照：
  - `最容易误写的，是把 Unity 压成 C#、把 Unreal 压成 C++。更稳的写法，是把两边分别压成一组运行时底座结构。`
- 本节事实与判断分界：
  - `事实`：官方文档能直接支持代码入口、对象运行时、反射、GC、任务调度这些差异
  - `判断`：所以两边真正不同的不是“API 风格”，而是整套运行时底座怎么组织

### 7. 常见误解

- 误解 `1`：
  - `GC 就是一个低层功能点，可以和渲染、物理、音频平铺`
  - 纠正方式：指出 GC 回答的是对象与内存如何被运行时管理
- 误解 `2`：
  - `Job System / Task Graph 只是优化工具，和引擎架构无关`
  - 纠正方式：指出两边官方都把它们放在正式执行与调度语境里
- 误解 `3`：
  - `Unity 和 Unreal 在这一层的差异，说到底就是 C# vs C++`
  - 纠正方式：强调脚本后端、对象系统、反射接入、GC 约束与任务调度接口才是这层真正差异
- 误解 `4`：
  - `Blueprint 可以直接等价成 MonoBehaviour，Task Graph 可以直接等价成 Job System`
  - 纠正方式：强调本文只比较它们在运行时底座层里的“站位”，不做一一对应映射
- 误解 `5`：
  - `这一篇应该顺手把 DOTS、Mass、Burst、复制和平台线程模型一起讲完`
  - 纠正方式：回到系列边界，说明这些分别属于后续文章或后续细化

### 8. 我的结论

- 收束顺序建议：
  - 先重申主问题不是低层名词列表，而是运行时底座怎么被组织
  - 再重申能直接成立的事实
  - 最后给出工程判断
- 本段必须写出的事实：
  - Unity 官方文档支持把 scripting backend、PlayerLoop、GC、reflection、Job System 放进正式运行时语境
  - Unreal 官方文档支持把 C++、UObject、Reflection System、Blueprint、Task Graph 放进正式编程与运行时语境
  - 当前没有本地 `READY` 的源码根路径
- 本段必须写出的判断：
  - `GC、反射、脚本后端、任务系统都应先被看作运行时底座，而不是一排业务模块。`
  - `Unity 与 Unreal 在这一层的差异，不是简化成 C# vs C++ 就能说清，而是要看执行入口、对象运行时、内存机制和任务调度怎样被组织。`
- 结尾过渡：
  - `07` 会继续回答“DOTS / Mass 为什么不是普通模块，而是对世界模型与执行模型的重构”

## 起草时必须保留的一张对照表

| 对照维度 | Unity | Unreal | 本文要落下的判断 |
| --- | --- | --- | --- |
| 代码进入运行时 | Mono / IL2CPP scripting backend | C++ + Blueprint scripting | 代码执行入口组织方式不同 |
| 对象与元信息 | reflection 与运行时缓存约束 | UObject + Reflection System | 对象如何被引擎识别不同 |
| 内存管理 | GC 约束写在 .NET / runtime 语境里 | UObject 自带 GC 等基础能力 | GC 属于运行时机制 |
| 执行骨架 | PlayerLoop + 生命周期 | 对象运行时 + Task Graph | 执行顺序与任务调度有正式地基 |
| 工程判断 | 更像脚本后端驱动的执行骨架 | 更像对象系统驱动的运行时基础 | 差异不只是语言或 API 风格 |

## 可直接拆出的两条短观点

- `GC、反射、任务系统不该和渲染、物理、音频平铺，因为它们先决定的是引擎怎么运行。`
- `Unity 和 Unreal 在这一层的差异，不只是谁写 C#、谁写 C++，而是谁怎样组织自己的运行时底座。`

## 起草时必须反复自检的三件事

- `我有没有把这篇写成低层名词列表，而不是运行时底座文章`
- `我有没有把世界模型、DOTS / Mass、专业子系统和平台细节抢写进来`
- `我有没有把事实和判断明确分开，并持续提醒当前没有源码级验证`
