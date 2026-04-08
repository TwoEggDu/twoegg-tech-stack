---
date: "2026-04-08"
title: "Unity 异步运行时 01｜UniTask 的最小内核：为什么只是一个 struct 壳，却非要带上 source 和 token"
description: "如果不先看清 UniTask 最小内核的数据模型，后面的 PlayerLoop、builder、CompletionSource 和帧时序语义都会悬空。UniTask 真正的起点不是某个 await API，而是一个很薄的 struct 壳、一个 source 协议、以及一个经常被误解的 token。"
slug: "unity-async-runtime-01-unitask-minimal-kernel-struct-source-token"
weight: 2365
featured: false
tags:
  - "Unity"
  - "Async"
  - "UniTask"
  - "IUniTaskSource"
  - "Runtime"
  - "Source"
series: "Unity 异步运行时"
primary_series: "unity-async-runtime"
series_role: "article"
series_order: 11
---

> 如果只用一句话概括这篇文章，我会这样说：`UniTask 最关键的设计，不是“把 Task 换成了 struct”这么简单，而是把“任务壳”和“完成协议”彻底拆开：外面只是一个很薄的值类型句柄，真正的状态、结果、异常、取消和 continuation 注册，全都下沉到 source 协议里；token 则负责把这两层重新安全地绑回同一次操作。`

上一篇 `00` 已经把问题空间立住了：

- `Task` 并不是坏抽象，只是它默认站在一般托管运行时的世界里
- Unity 里却多出了一整套强主线程、强帧时序、强生命周期、强原生异步对象的运行时语义
- UniTask 之所以会长出来，不只是为了少一点 GC，而是为了补回这些 Unity 特有的语义

但如果继续往下写 PlayerLoop、builder、CompletionSource、`Yield / NextFrame / Delay`，读者很快会遇到一个更底层的问题：

`UniTask 自己这个东西，到底是什么？`

很多人第一次打开源码时都会有一种反差感：

- 名字叫 `UniTask`
- 心里预期的是“一个 Unity 版 Task 对象”
- 打开源码却发现，它几乎什么都没有

这个反差不是错觉。

UniTask 的非泛型形态，在源码里真的非常薄：

- 一个 `IUniTaskSource source`
- 一个 `short token`
- 一个很薄的 `Awaiter`
- 少量适配逻辑

也就是说，它首先不是“状态很多的任务对象”，而是：

`一个很轻的异步结果句柄。`

而这个句柄真正有意义，依赖的是背后那套 source 协议。理解这一层，后面几篇才会真正接起来。

## 第一层：UniTask 真正做的第一刀，不是“优化”，而是“拆壳”

如果先不看任何细节，只看设计方向，UniTask 和很多人脑子里默认的 `Task` 心智模型有一个根本差异：

- `Task` 更像“状态和行为都装在对象里”
- `UniTask` 更像“外部句柄 + 内部协议对象”

这意味着 UniTask 首先做的事情不是“把类改成 struct”，而是：

`把“我要 await 的那个句柄”和“真正保存状态、结果、异常、取消、continuation 的那层实现”拆开。`

这件事的意义非常大，因为它直接决定了后面所有机制的形状：

- PlayerLoop runner 可以成为某类 source
- `Delay` / `NextFrame` / `WhenAll` 可以各自提供自己的 source
- builder 可以把状态机完成信号挂到某个 source 上
- CompletionSource 可以作为可复用的 source 被池化

也就是说，UniTask 世界里真正“多样化”的，不是外层壳，而是里面的 source。

外层壳被故意做得几乎没个性，反而是为了让内部 source 体系能非常自由地长开。

## 第二层：非泛型 UniTask 为什么薄到只剩 `source + token`

先看最小形态。

非泛型 `UniTask` 的核心字段就是：

- `IUniTaskSource source`
- `short token`

这两个字段已经足够回答它最核心的几个问题：

- 这次等待是不是已经完成
- 如果没完成，完成后 continuation 去哪里注册
- 如果完成了，结果、异常、取消从哪里取
- 当前这个句柄到底是不是还对得上它声称代表的那一次操作

也就是说，外层 UniTask 本体并不保存：

- 结果缓存
- 异常缓存
- continuation 列表
- 调度队列
- 生命周期绑定逻辑

这些都不在壳上，而是在 source 上。

### 1. `source == null` 在 UniTask 里不是“没值”，而是“同步成功完成”

这是第一眼最容易忽略的点。

在非泛型 `UniTask` 里，如果 `source == null`，它的 `Status` 会直接返回 `Succeeded`。`Awaiter.GetResult()` 也会直接返回，不再向下取任何状态。

这意味着：

`UniTask 把“已经成功完成”的最轻路径，直接编码成了“没有 source”。`

这和很多面向对象的直觉不同，因为我们会本能地以为：

- 没对象 = 没状态 = 空

但 UniTask 这里表达的是另一层含义：

- 既然已经成功完成，而且没有额外载荷，那就没有必要再持有一个真正的状态对象

这会让它在大量“本来就已经完成”的路径上非常轻。

例如：

- 某些同步快速返回
- 某些桥接 API 判断后直接完成
- 某些 builder 直接落在完成路径

这些路径不需要额外堆对象来表示“我已经完成了”。

### 2. 泛型 `UniTask<T>` 又多带了一份内联 `result`

如果继续看 `UniTask<T>`，它比非泛型版本多了一个字段：

- `T result`

这对应的是另一个关键优化方向：

- 对于已经同步拿到的结果，不必再额外挂一个 source
- 结果可以直接内联在值类型句柄里

于是 `UniTask<T>` 会自然分成两种形态：

- `source == null`：说明结果已经直接在 `result` 里
- `source != null`：说明真正的完成协议仍由 source 提供

这件事表面上像“少一次分配”，但更深的意义是：

`UniTask 从数据模型一开始就在区分：这次 await 到底只是一个已知结果，还是一个真正仍在运行中的异步协议。`

这个区分对 Unity 很重要，因为很多 API 并不是每次都真的需要启动一次完整的异步状态机。

## 第三层：source 协议才是 UniTask 真正的运行时中心

如果只看外层壳，很容易误以为 UniTask “没什么内容”。

真正的内容其实都在 `IUniTaskSource` 和 `IUniTaskSource<T>` 里。

这个接口非常克制，只定义了四件事：

- `GetStatus(short token)`
- `OnCompleted(Action<object> continuation, object state, short token)`
- `GetResult(short token)`
- `UnsafeGetStatus()`

这四个入口，已经把一个 awaitable 协议压缩到了最小闭环。

### 1. `GetStatus` 负责回答：现在到底到了哪一步

UniTask 的状态枚举很简单：

- `Pending`
- `Succeeded`
- `Faulted`
- `Canceled`

这和大家熟悉的 Task 大方向一致，但它的重点不在“状态丰富”，而在：

- 是否已完成
- 若已完成，属于成功、错误还是取消

对 await 协议来说，这已经够了。

### 2. `OnCompleted` 负责回答：还没完成时，continuation 挂到哪里

这是 UniTask 世界真正的接线口。

如果当前 source 还没完成，编译器生成的 continuation 最终就要注册到这里。

后面你看到的所有高层行为：

- PlayerLoop 某个 timing 继续
- 某个 Promise 完成后继续
- 某个组合任务全部结束后继续

本质上都要落成：

`把 continuation 挂到某个 source 的 OnCompleted 上。`

这也是为什么我们后面写 builder、runner、delay promise 时，最终都会回到这一个问题：

- continuation 到底注册到了哪个 source

### 3. `GetResult` 负责把成功、异常、取消重新兑现回来

这一步经常被低估。

很多人会以为“状态知道了不就完了”，其实不够。

因为 await 协议最后一定要有一个统一出口，把：

- 成功结果
- 异常抛出
- 取消抛出

在恢复时刻重新兑现回来。

也就是说，source 不只是“知道完成没完成”，它还必须知道：

- 完成之后该怎么把这次操作的最终语义还原给调用方

### 4. `UnsafeGetStatus` 不是主协议，而是调试与内部辅助口

这个方法的存在很值得注意。

它说明 UniTask 很明确地区分了两件事：

- 正式消费路径：必须带 token 校验
- 内部观察 / 调试路径：可以绕过部分校验去看当前状态

换句话说，UniTask 从接口层就已经在防御“随便拿一个句柄乱看乱用”。

## 第四层：token 到底是什么，它为什么不是取消 token

对第一次读 UniTask 源码的人来说，`token` 往往是最容易误解的字段。

因为项目里我们太习惯把 token 看成：

- `CancellationToken`
- 某种取消信号
- 某种上下文标识

但 UniTask 这里的 `short token` 根本不是这个意思。

更准确地说，它是：

`这次句柄和这次 source 实例操作版本是否匹配的校验值。`

### 1. token 的工作不是“告诉你要不要停”，而是“告诉你你现在碰的是不是同一次操作”

这点一定要和取消语义分开。

`CancellationToken` 回答的是：

- 调用上下文是否要求这段逻辑终止

而 UniTask 这里的 `token` 回答的是：

- 你这个 awaiter / 句柄，现在访问的 source，还是不是它原本代表的那次异步操作

也就是说，一个是**业务 / 生命周期终止语义**，另一个是**协议一致性校验语义**。

### 2. token 最常见的来源，其实是 source 内部的 version

如果继续往下看 `UniTaskCompletionSourceCore<TResult>`，会发现里面有一个 `short version`。在 `Reset()` 时，这个版本会递增；`ValidateToken(token)` 会检查传进来的 token 是否仍和当前 version 一致。

这揭示了 token 的核心用途：

- source 可能被复用
- 同一个 source 对象可能先后服务多次异步操作
- 外层句柄必须知道自己是不是还绑在那次原始操作上

否则就会出现一种很危险的错配：

- 你手里的句柄以为自己代表“上一次操作”
- source 本体却已经被重置、复用，开始服务“下一次操作”

一旦没有 token / version 这道闸门，协议很容易被用错到完全不可推理。

### 3. token 也是 UniTask 默认“单次消费”约束的一部分基础设施

后面写 CompletionSource 那篇会系统展开这件事，但现在先给一个最短判断：

`UniTask 不是默认站在“可以随便多次 await 同一个结果”的模型上。`

原因不是作者任性，而是这套 source + pooling + version 体系，本来就更接近：

- 一个 source 完成一次操作
- 结果被消费
- source 可以复位、回池、再服务下一次操作

在这个世界里，如果不引入 version/token 校验，错误复用和二次 await 会非常难防。

所以 token 不只是一个小字段，它实际上在给这整个设计兜协议安全。

## 第五层：为什么 Awaiter 自己也这么薄

再看外层 `Awaiter`，你会发现它几乎没有“业务”。

它最重要的几件事无非是：

- `IsCompleted` 去看状态
- `GetResult` 转交给 source
- `OnCompleted / UnsafeOnCompleted` 转交给 source

这说明 UniTask 的 awaiter 不打算成为一个很聪明的中间层。

### 1. Awaiter 不是另一个状态机，它只是协议转接头

很多人第一次读 awaiter 源码会觉得“怎么就这么点”。

其实这恰恰说明设计是收束的。

UniTask 很清楚各层职责：

- builder 负责和 C# async 状态机对接
- awaiter 负责满足 await 协议
- source 负责保存并兑现异步状态
- runner / promise / CompletionSource 负责提供具体完成机制

于是 awaiter 自己只需要成为一个足够薄的转接头。

### 2. static `AwaiterActions.InvokeContinuationDelegate` 说明它在尽量避免每次 await 再包一层委托

这个细节也很有代表性。

awaiter 在向 source 注册 continuation 时，不是每次动态造一个包装器，而是复用一个静态 `Action<object>`，把真正的 `Action continuation` 作为 state 传进去。

这说明 UniTask 的“轻”不是单点优化，而是贯彻到了协议细部：

- 句柄要轻
- awaiter 要薄
- continuation 适配也要尽量少额外包装

### 3. 非泛型和泛型 awaiter 的分工也很清楚

- 非泛型 `UniTask`：完成即可，没有结果载荷
- 泛型 `UniTask<T>`：若 `source == null`，直接返回内联 `result`；否则再向 source 取值

也就是说，外层 awaiter 仍然坚持同一个设计原则：

`先走最轻的同步快路径，不够再把责任下沉给 source。`

## 第六层：为什么 `Preserve()` 的存在反而证明默认模型不是“随便多次 await”

源码里有一个特别值得写清的 API：`Preserve()`。

它做的事情不是“增强性能”，而是：

`把原本依赖底层 source 的单次消费结果，包成一个可 memoize 的 source，让结果可以安全复用。`

这件事的重要性在于，它反向说明了默认模型的意图。

如果 UniTask 默认就站在“天然多次 await 安全”的世界里，`Preserve()` 就不会显得这么必要。

但现在它必须显式提供一个 `MemoizeSource`，并在第一次消费后缓存：

- 成功结果
- 取消状态
- 异常信息

这说明什么？

说明默认 UniTask 的真实心智模型更像：

- 外层只是指向一次异步协议的句柄
- 这次协议的底层 source 可能是一次性消费的
- 如果你要把它变成一个稳定可复读的结果，就必须显式 memoize

这点对后面理解 CompletionSource 和池化非常关键。

## 第七层：为什么这种最小内核特别适合 Unity 运行时

现在回到系列主线。为什么 UniTask 要设计成这样，而不是“做一个功能更多的 Task 克隆”？

因为 Unity 运行时真正需要的，不是另一个“大而全的任务对象”，而是一个：

- 足够轻的 await 句柄
- 能挂接很多不同 Unity 异步源的统一协议
- 能和 PlayerLoop、Delay、原生 `AsyncOperation`、组合 Promise、生命周期收口一起工作
- 能适配大量高频、短生命周期、主线程敏感的等待场景

也就是说，UniTask 这里追求的不是“功能向外堆”，而是：

`把最外层压到极薄，让内部各种 Unity 特化 source 能接进来。`

这和 Unity 的运行时现实非常对口，因为 Unity 项目里的异步源本来就高度异构：

- 有的来自原生对象
- 有的来自 PlayerLoop timing
- 有的来自状态机 builder
- 有的来自组合任务
- 有的来自手工 CompletionSource

如果外层壳太重，内部协议就不容易统一；反过来，外层越像“一个薄句柄”，source 体系越容易扩展。

## 常见误解

### 误解一：UniTask 是 struct，所以它的核心价值主要就是少分配

不对。

少分配当然重要，但更深的设计在于：

- 它把任务壳和完成协议拆开了
- 它允许非常多的 Unity 特化 source 接进同一个 await 协议

### 误解二：token 就是取消 token，或者某种调度 token

不对。

token 在这里首先是版本一致性校验值，用来保证外层句柄和当前 source 操作实例仍然匹配。

### 误解三：UniTask 外层这么薄，说明复杂度不高

也不对。

复杂度没有消失，只是被下沉到了：

- source 协议层
- CompletionSource 与池化层
- PlayerLoop runner 层
- builder 和状态机接线层

### 误解四：既然有 `Preserve()`，那默认 UniTask 也应该能随便多次 await

不对。

`Preserve()` 恰恰说明默认模型不是这个预设；它是在你确实需要复用结果时，显式给你补一层 memoization。

## 最后把这件事压成一句话

`UniTask 的最小内核不是“一个更轻的 Task 对象”，而是“一个很薄的值类型句柄 + 一套真正承载状态与完成语义的 source 协议 + 一个保证这次句柄确实指向这次操作的 token”；只要这三个点没看清，后面无论写 PlayerLoop、builder、CompletionSource 还是帧时序，都会看见很多代码，却看不见它们为什么能接成同一套系统。`
