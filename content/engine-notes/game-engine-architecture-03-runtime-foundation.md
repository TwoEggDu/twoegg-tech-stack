---
date: "2026-03-26"
title: "游戏引擎架构地图 03｜脚本、反射、GC、任务系统，到底站在引擎的哪一层"
description: "基于 Unity 与 Unreal 官方资料，把脚本后端、对象元信息、GC 和任务调度收回同一层运行时底座，而不是把它们写成一排平铺功能。"
slug: "game-engine-architecture-03-runtime-foundation"
weight: 440
featured: false
tags:
  - Unity
  - Unreal Engine
  - Runtime
  - GC
  - Reflection
  - Task System
series: "游戏引擎架构地图"
---

> 这篇只回答一个问题：`什么叫引擎的运行时底座，为什么脚本后端、反射、GC、任务系统不该被当成功能模块平铺。`  
> 它不展开 00 总论里的六层总地图，不重讲 02 的默认对象世界，也不提前展开 07 的 DOTS / Mass，只先把运行时底座层压清楚。

先说明这篇的证据边界。

当前这版首稿只使用官方文档证据。`docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径都还不是 `READY`，所以下文里凡是“事实”，都只落在官方资料明确写出来的范围；凡是“判断”，都会明确写成工程判断，而不是伪装成源码结论。

## 这篇要回答什么

很多人在聊游戏引擎底层时，最容易把几个词直接平铺在一起：

- `GC`
- `Reflection`
- `IL2CPP`
- `Blueprint`
- `Job System`
- `Task Graph`

然后文章会迅速滑向两种写法。

第一种写法，是把它们写成一份低层术语清单。  
第二种写法，是把它们写成“谁更快、谁更现代、谁更先进”的语言或产品争论。

这两种写法都会漏掉真正的问题。

这篇真正要回答的，不是：

`GC 到底算不算一个功能模块？`

也不是：

`Unity 是不是等于 C#，Unreal 是不是等于 C++？`

而是：

`到底是什么机制，决定代码怎么进入运行时、对象怎么被引擎识别、内存怎么被管理、任务怎么被调度。`

一旦问题这样重写，脚本后端、反射、GC、任务系统就不该再被看成一排平铺名词，而应该被收回同一个层次里讨论。

从官方文档能直接成立的事实是：

- Unity 把 scripting backend、PlayerLoop、GC、reflection、Job System 都写在正式运行时和脚本执行语境里。
- Unreal 把 C++ 编程入口、UObject、Reflection System、Blueprint、Task Graph 都放在正式编程与 API 文档体系里。

基于这些事实，我在这篇里先给出的判断是：

`这篇讨论的不是几项低层功能，而是引擎的运行时底座。`

## 这一层负责什么

如果要把“运行时底座层”压成一句更工程化的话，它主要回答四件事：

1. 代码以什么形态进入正式运行时。
2. 引擎怎么识别对象、类型和元信息。
3. 对象与内存靠什么机制被管理。
4. 每帧执行顺序和任务调度靠什么骨架组织。

先把两边压成一张最小对照表，会更容易看清问题。

| 对照维度 | Unity 运行时底座 | Unreal 运行时底座 | 这层真正负责什么 |
| --- | --- | --- | --- |
| 代码进入运行时 | `Mono / IL2CPP` scripting backend | `C++ + Blueprint` scripting | 代码最终以什么形态进入正式执行环境 |
| 对象与元信息 | `reflection` 与运行时缓存约束 | `UObject + Reflection System` | 引擎怎样识别对象、类型和元数据 |
| 内存管理 | `Mono / IL2CPP` 共享 GC 约束 | `UObject` 提供 GC 等基础能力 | 对象和内存怎样被持续管理 |
| 执行骨架 | `PlayerLoop + MonoBehaviour` 生命周期 | `C++ / UObject / Blueprint` 对象运行时 | 每帧和生命周期怎样组织起来 |
| 任务调度 | `Job System` 与 worker threads | `Task Graph` 正式接口 | 任务怎样进入正式调度系统 |

从官方文档能直接成立的事实是：

- Unity 官方明确 `IL2CPP` 是 scripting backend，并明确区分 `JIT` 与 `AOT`。
- Unity 官方把 `PlayerLoop` 描述成 Unity player loop 的正式入口，并把 `Awake / Start / Update / LateUpdate / FixedUpdate` 放在统一执行顺序里说明。
- Unity 官方把 GC、reflection overhead、Job System 都放在 .NET / runtime / multithreading 的语境里解释。
- Unreal 官方明确 `UObject` 是对象系统基类，Reflection System 通过 `UCLASS`、`USTRUCT` 等宏把类型接入引擎能力。
- Unreal 官方把 Blueprint 写成完整的 gameplay scripting system，把 `FTaskGraphInterface` 写成正式运行时接口。

基于这些事实，我在这里的判断是：

`运行时底座层不是“性能优化专区”，而是回答整台引擎靠什么正式运行起来的一整层。`

所以，这一层最不该被写成：

- 渲染、物理、音频旁边再挂几个“低层功能点”
- C# 和 C++ 的语言优劣比较
- 一串彼此没有边界的底层术语

## 这一层不负责什么

边界必须先压清，不然后面一定会串题。

这篇明确不做下面几件事：

- 不重讲 `Scene / World`、`GameObject / Actor` 的默认对象世界差异
- 不展开 `DOTS / Mass` 怎样改写世界模型和执行模型
- 不解释渲染、物理、动画、音频、UI 的内部实现
- 不做 `C# vs C++` 谁更先进、`Blueprint` 是否“更慢”、`GC` 是否“更落后”的产品裁判
- 不把 `IL2CPP`、`Blueprint VM`、`Task Graph` 写成源码级调用链分析
- 不写任何编辑器按钮、脚本创建或项目配置教程

为什么必须克制？

因为只要把世界模型、运行时底座、数据导向扩展、专业子系统和平台细节一起揉进来，这篇就会从架构文章直接塌成术语百科。

而这篇真正要站住的，只是一个更基础的问题：

`什么机制在支撑整台引擎的执行方式。`

## Unity 怎么落地

先看 Unity 官方文档给出的运行时链条。

### scripting backend 决定脚本怎样真正进入运行时

从 Unity 关于 `.NET in Unity` 和 `IL2CPP` 的说明里，能直接落下几件事实：

- Unity 官方明确不同平台可能使用不同的 scripting backends。
- Unity 官方明确区分 `JIT` 与 `AOT`：前者允许运行时动态生成 IL，后者不支持这件事。
- Unity 官方明确 `IL2CPP` 会在构建时把 IL 转成 C++，再生成平台原生二进制。

这些事实很重要，因为它们说明 `脚本后端` 在 Unity 里并不是一个外围打包步骤，也不是一个“脚本语言插件”。

它直接决定了：

- 代码最后以什么形态进入运行时
- 运行时能不能接受动态生成代码
- 同一套脚本逻辑最终靠什么执行路径落到平台二进制上

基于这些事实，我的判断是：

`scripting backend` 在 Unity 里首先是运行时底座的一部分，而不是附属工具链。

### PlayerLoop 和生命周期共同组成默认执行骨架

从 `LowLevel.PlayerLoop` 与 `Order of Execution for Event Functions` 文档里，能直接看到：

- `PlayerLoop` 被官方描述为代表 Unity player loop 的类，并且会暴露原生系统的 update order。
- `Awake`、`OnEnable`、`Start`、`Update`、`LateUpdate`、`FixedUpdate` 等生命周期都被统一放进官方执行顺序文档里说明。
- 在场景初始对象上，`Awake / OnEnable` 会先于 `Start / Update` 这类后续入口。

这意味着，Unity 默认并不是先给你一份“脚本 API 列表”，再让你自己猜它们如何运行。

更接近事实的说法是：

`PlayerLoop` 提供执行骨架，`MonoBehaviour` 生命周期把脚本入口挂到这套骨架上。

所以这层真正回答的不是“Update 属于哪一个模块”，而是：

`Unity 默认怎样组织每帧执行和对象脚本进入运行状态的顺序。`

基于这些事实，我的判断是：

`PlayerLoop + 生命周期` 更像 Unity 的默认执行骨架，而不是渲染、物理、UI 旁边的又一个功能点。

### GC 和 reflection 不是附件，而是对象与内存模型的约束

从 Unity 关于 `.NET`、`C# reflection overhead` 的文档里，还能直接落下几件事实：

- Unity 官方说明 `Mono` 与 `IL2CPP` 都使用 `Boehm garbage collector`。
- Unity 官方说明默认启用 incremental GC。
- Unity 官方说明 `System.Reflection` 相关对象会被缓存，而这些缓存不会被 Unity 自动回收，因此 GC 会持续扫描它们。

这组事实的意义，不在于告诉你“GC 是个底层知识点”，而在于它说明：

- 对象和元信息怎样被运行时长期持有
- 垃圾回收不是业务模块，而是整个运行时内存模型的一部分
- reflection 也不是单纯的语法便利，而会变成对象识别和 GC 扫描边界的一部分

基于这些事实，我的判断是：

`GC / reflection` 在 Unity 里更接近对象与内存模型的正式约束，而不是一项可以和渲染、音频并排列出来的业务功能。

### Job System 扩展的是执行地基，而不是新增一个功能分区

从 Unity 的 `Job system overview` 里，能直接看到：

- Unity 官方把 Job System 描述为允许用户代码与 Unity 共享 worker threads 的多线程系统。
- worker thread 数量会与可用 CPU core 匹配。
- Job System 会把 blittable 类型数据复制到 native memory，并在 managed / native 之间用 `memcpy` 传输。

这些事实说明，`Job System` 在回答的首先不是“多线程优化技巧”，而是：

- 任务怎样被正式派发到共享执行资源上
- 数据怎样在托管世界和原生执行边界之间移动
- 引擎怎样保证多线程调度有一套可控的默认约束

基于这些事实，我的判断是：

`Job System` 首先是 Unity 运行时执行地基的一部分，而不是和动画、音频、渲染并列的新功能分区。

把这一节收成一句话，就是：

`Unity 的运行时底座更像“脚本后端 + 默认执行骨架 + GC / reflection 约束 + 任务调度设施”的组合。`

这还不是 `DOTS / Burst`，也不是在讲世界模型或资源链。

## Unreal 怎么落地

再看 Unreal 官方文档给出的运行时链条。

### Unreal 的正式编程入口不是“只有 C++”

从 `Programming with C++ in Unreal Engine` 这组文档里，能直接看到：

- Unreal 官方把 `Programming with C++` 写成正式编程入口。
- 同一组基础编程文档里并列出现 `Objects`、`Reflection System` 等内容。

这意味着，Unreal 从官方入口开始，就没有把“写 C++”和“接入引擎对象运行时”分成两件完全无关的事。

基于这些事实，我的判断是：

`Unreal 的运行时底座不是“纯 C++ 自己跑”，而是从正式编程入口起就接入对象系统。`

### UObject 和 Reflection System 定义了对象运行时基础

从 `Objects in Unreal Engine` 与 `Reflection System` 文档里，能直接落下几件事实：

- `UObject` 是 Unreal 对象的基类。
- 反射系统通过 `UCLASS`、`USTRUCT` 等宏把类型接入引擎与编辑器能力。
- 官方文档明确列出 `UObject` 提供的基础能力，包括 `garbage collection`、`reflection`、`serialization`、`automatic editor integration`、`runtime type information`，以及网络复制相关能力。

这组事实非常关键，因为它说明 Unreal 里“对象”不是一个只有语法意义的 C++ 类实例。

它从一开始就站在一套更强的引擎对象运行时里：

- 类型会被引擎识别
- 元信息会被反射系统接管
- 对象管理、序列化和 GC 不是临时外挂

基于这些事实，我的判断是：

`UObject + Reflection System` 不只是元数据工具，而是 Unreal 如何识别对象、管理对象并把对象接入引擎能力的运行时基础。

### Blueprint 是正式脚本系统，不只是编辑器便利功能

从 Unreal 官方术语文档和编程入口文档里，还能直接看到：

- Blueprint 被官方定义为完整的 `gameplay scripting system`。
- Blueprint 不是脱离对象系统存在的一块可视化外壳，而是站在正式编程体系里的脚本入口之一。

这意味着，Unreal 的运行时底座并不能被压成一句：

`它就是 C++。`

更接近事实的说法是：

`它是 C++ 入口、对象运行时和 Blueprint 脚本系统共同组成的执行基础。`

基于这些事实，我的判断是：

`Blueprint` 在本篇里应该先被看作正式脚本执行体系的一部分，而不是只看成编辑器层的便利功能。

### Task Graph 是正式调度接口，而不是外挂优化模块

从 `FTaskGraphInterface` 和 `AttachToThread` 的 API 文档里，能直接看到：

- Task Graph 被写成正式运行时接口，而不是零散技巧文章。
- API 暴露 `GetNumWorkerThreads`、`IsMultithread`、`IsThreadProcessingTasks` 这类线程与调度能力。
- 外部线程需要被显式接入 task graph system，才能被该系统识别和管理。

这些事实说明，Task Graph 真正回答的是：

- 线程怎样进入正式调度体系
- 任务怎样被运行时识别和分派
- 引擎如何维持一套统一的并行执行边界

基于这些事实，我的判断是：

`Task Graph` 首先是 Unreal 的调度地基，而不是一个外挂优化插件。

把这一节收成一句话，就是：

`Unreal 的运行时底座更像“C++ 执行入口 + UObject / Reflection 对象运行时 + Blueprint 脚本系统 + Task Graph 调度地基”的组合。`

这还不是在讲世界模型，也不是在讲渲染、物理、资源链或平台抽象。

## 为什么不是表面 API 差异

把前面两节再压一次，至少能先看出四层差异。

### 第一层差异：代码进入运行时的组织方式不同

Unity 官方更强调：

- `Mono / IL2CPP` scripting backend
- `PlayerLoop`
- 挂在对象上的脚本生命周期

Unreal 官方更强调：

- `C++` 正式编程入口
- `UObject + Reflection System`
- `Blueprint` 作为 gameplay scripting system

所以这里真正不同的，不是“一个用 C#、一个用 C++”这么简单，而是代码怎样接进正式运行时的组织方式不同。

### 第二层差异：对象和元信息怎样被运行时识别不同

Unity 这边，reflection 与缓存约束、GC 扫描边界、脚本生命周期一起构成了脚本运行时的一部分。  
Unreal 这边，`UObject` 和 Reflection System 则把对象类型、元数据、序列化和 GC 基础能力更明确地收进对象运行时。

所以更稳的说法不是：

`两边都有反射，所以差不多。`

而是：

`两边都把对象识别和元信息接入当成运行时地基，但组织方式不同。`

### 第三层差异：GC 和任务系统都在回答“引擎怎么运行”

Unity 的 GC 与 Job System，和 Unreal 的 UObject GC 能力与 Task Graph，有一个共性：

它们都不该先被理解成“附加功能”。

更准确的说法是，它们都在回答：

- 对象和内存怎样被持续管理
- 任务和线程怎样进入正式调度

这也是为什么我不愿意把 `GC` 写成和渲染、物理、音频平铺的一项“低层功能”。

### 第四层差异：真正的区别不是 API 风格，而是运行时底座怎么被组织

最容易误写的，是把 Unity 压成 `C#`，把 Unreal 压成 `C++`。

更稳的写法是：

- Unity 更像 `脚本后端驱动的默认执行骨架`
- Unreal 更像 `对象运行时驱动的执行基础`

这不是最终定论，更不是实现级结论，但它至少能比“语言不同”更稳定地解释：

- 为什么两边会长出不同的脚本工作方式
- 为什么 GC、反射、调度系统会站在不同的组织重心上
- 为什么后面连 `DOTS / Mass` 这种问题也必须重新放回整张架构地图里看

## 常见误解

### 误解一：GC 就是一个底层功能点，可以和渲染、物理、音频平铺

这句话会把“对象和内存怎样被运行时管理”写没掉。  
GC 真正回答的是运行时对象生命周期和内存扫描边界，而不是某个业务模块的附属能力。

### 误解二：Job System 和 Task Graph 只是优化工具

如果只把它们写成优化工具，就会忽略官方文档其实都把它们放在正式执行与调度语境里。  
它们首先是执行基础设施，然后才谈优化效果。

### 误解三：这一层的差异，说到底就是 C# vs C++

这句话最省事，但信息量也最低。  
更接近事实的是：两边差异至少同时涉及脚本入口、对象运行时、反射接入、GC 约束和任务调度接口。

### 误解四：Blueprint 可以直接等价成 MonoBehaviour，Task Graph 可以直接等价成 Job System

这篇不做这种一一映射。  
本文只比较它们在运行时底座层里的“站位”，不主张它们在实现、能力或使用方式上完全对等。

### 误解五：这一篇应该顺手把 DOTS、Mass、Burst、UE::Tasks 一起讲完

不应该。  
这篇只先把运行时底座层压清，`07` 再去回答 DOTS / Mass 为什么不是普通模块。  
如果现在把它们全抢写进来，这篇就会失去边界。

## 我的结论

先重申这篇能直接成立的事实。

- Unity 官方文档支持把 `scripting backend`、`PlayerLoop`、`GC`、`reflection`、`Job System` 放进正式运行时语境。
- Unreal 官方文档支持把 `C++`、`UObject`、`Reflection System`、`Blueprint`、`Task Graph` 放进正式编程与运行时语境。
- 当前本地源码路径还没有任何 `READY` 标记，因此这篇不能声称自己做了源码级验证。

基于这些事实，我在这篇里愿意先给出的工程判断是：

`脚本后端、反射、GC、任务系统，都应该先被看作运行时底座，而不是一排业务模块。`

进一步说：

- Unity 这层更接近 `scripting backend + PlayerLoop + GC / reflection + Job System` 共同组成的默认执行骨架。
- Unreal 这层更接近 `C++ + UObject / Reflection + Blueprint + Task Graph` 共同组成的对象与执行底座。

因此，这一层最值得记住的一句话不是：

`Unity 用 C#，Unreal 用 C++。`

而是：

`两台引擎在这一层真正不同的，是它们怎样组织代码入口、对象运行时、内存机制和任务调度。`

下一篇 `07` 会继续回答：`DOTS / Mass` 为什么不是普通模块，而是对世界模型和执行模型的重构。  
等那一篇再回头看这篇，你会更容易看出，为什么运行时底座层必须先被单独压清。
