---
date: "2026-04-08"
title: "Unity 异步运行时 07｜Forget、UniTaskVoid、未观察异常：UniTask 为什么没有沿用 TaskScheduler 那套世界"
description: "UniTask 不是把 Task 的异常模型原封不动搬进 Unity。Forget、UniTaskVoid、UniTaskScheduler、主线程派发和未观察异常发布，共同定义了另一套更贴 Unity 运行时的异常可观测性路径。读懂这套路径，才能看清 fire-and-forget 在 Unity 项目里到底是便利工具、危险捷径，还是一套必须显式约束的运行时协议。"
slug: "unity-async-runtime-07-forget-unitaskvoid-unobserved-exception"
weight: 2371
featured: false
tags:
  - "Unity"
  - "Async"
  - "UniTask"
  - "Exception"
  - "Forget"
  - "Runtime"
series: "Unity 异步运行时"
primary_series: "unity-async-runtime"
series_role: "article"
series_order: 17
---

> 如果只用一句话概括这篇文章，我会这样说：`UniTask 没有把自己塞进完整的 TaskScheduler / AggregateException 世界里；它对 fire-and-forget、UniTaskVoid 和未观察异常的处理，更像是在 Unity 运行时里明确划出一条“异常最终必须被发布、被转交、或被显式消费”的可观测性路径。`

到这一步，主线里真正难的部分其实已经差不多都出现过了：

- `Task` 和 Unity 的默认世界观并不完全重合
- UniTask 用 `struct + source + token` 重写了最小结果载体
- 调度层不只是“回主线程”，而是明确接到了 `PlayerLoop` timing
- builder 自己很薄，真正承重的是状态机 runner 和 source 协议
- completion source、version、token 校验共同把协议收成了一种更接近单次消费的模型
- 生命周期取消不是风格问题，而是 continuation 和对象世界之间的边界契约

但到项目里，团队真正最容易爆炸的地方通常还不是这些。更常见的爆炸点是：

- 这段异步逻辑根本没人 `await`
- 某个事件回调、按钮点击、动画触发、绑定更新只能 fire-and-forget
- 异常冒出来时，没有任何调用方在等待它
- 日志里只剩下一条“不知道从哪来的 UnobservedTaskException”
- 或者更糟：你以为自己“已经忽略结果了”，其实只是把异常悄悄推迟到了另一个出口

也就是说，这篇文章真正要回答的问题不是：

- `Forget` 怎么用
- `UniTaskVoid` 能不能用

而是：

- UniTask 到底把“没人等待的异步错误”放到了哪里
- 为什么它没有沿用一整套 `TaskScheduler` 式的异常世界
- `Forget`、`UniTaskVoid`、`UniTaskScheduler` 和主线程派发之间到底是什么关系
- 为什么 Unity 项目的 fire-and-forget 如果不建立约束，很快就会从便利手段退化成排障噩梦

## 第一层：为什么“没人 await 的异步”在 Unity 里会特别常见

先把问题空间说清楚。

在一般托管应用里，团队虽然也会写 fire-and-forget，但很多主路径仍然偏向：

- 上层发起
- 下层返回 `Task`
- 调用链一路 `await`
- 异常最终回到某个清晰的调用方

Unity 项目里却不是这样。

### 1. 大量入口本来就不是“可等待调用链”

Unity 里最常见的异步入口往往是：

- 按钮点击
- 事件回调
- 生命周期函数
- 数据绑定更新
- UI 动画触发
- 某个系统消息监听

这些入口很多都天然要求签名是：

- `void`
- `Action`
- `UnityAction`
- 某种事件委托

也就是说，调用点天生就不鼓励你把异步结果层层往上传。

### 2. 业务上经常真的“不关心结果”，但绝不等于“不关心异常”

很多逻辑确实是 fire-and-forget 的：

- 上报埋点
- 后台预热
- 动画伴随加载
- 非关键 UI 刷新
- 某个状态变化后的附带异步动作

这些场景里，你可能真的不需要上层拿到返回值。

但“不关心结果”并不等于：

- 允许异常悄悄蒸发
- 允许取消和失败混成一团
- 允许错误在另一个线程、另一个阶段、甚至对象已经销毁后才被看见

这正是 UniTask 异常模型必须单独设计的原因。

### 3. Unity 项目里异常必须仍然回到“可观测世界”

Unity 里的调试和排障本来就更依赖：

- 主线程日志
- 编辑器 Console
- 某个统一事件出口
- 与当前运行时上下文可对齐的报错位置

所以 fire-and-forget 的真正难题从来不是“怎么忽略结果”，而是：

`结果没人要的时候，错误到底交给谁。`

## 第二层：UniTask 明确说了，它没有 TaskScheduler 那套完整世界

这不是推断，而是源码里写得很直接。`UniTaskScheduler.cs` 顶部就有一句非常关键的注释：

- `UniTask has no scheduler like TaskScheduler.`
- `Only handle unobserved exception.`

这两句话基本已经把整篇文章的核心结论说完了一半。

### 1. UniTask 的重点不是建设一个通用调度器对象，而是补 Unity 的时间与异常语义

前面几篇已经看到，UniTask 真正接管调度的地方是：

- `PlayerLoopHelper`
- runner / queue
- 各类 source / promise

也就是说，UniTask 从一开始就没有打算把“任务调度”重新抽象成另一个 `TaskScheduler`。它做的事更直接：

- continuation 该在哪个 `PlayerLoopTiming` 恢复
- source 协议怎样完成与回收
- 没人观察的异常最终怎样发布

这说明它在运行时设计上的取舍很明确：

`调度不是抽象成一个通用 scheduler 对象，而是分散在 PlayerLoop + source 协议体系里；真正留出一个统一静态出口的，是“未观察异常”。`

### 2. `UniTaskScheduler` 真正负责的是“最后的异常出口”

`UniTaskScheduler` 公开的核心能力非常少：

- `UnobservedTaskException` 事件
- `PropagateOperationCanceledException`
- 在 Unity 下的 `UnobservedExceptionWriteLogType`
- 在 Unity 下的 `DispatchUnityMainThread`
- 内部 `PublishUnobservedTaskException(Exception ex)`

这个接口形态本身已经说明很多事情：

- 它不是用来安排 continuation 的
- 也不是用来控制任务队列优先级的
- 更不是一个“拿来调度任务”的公共对象

它真正要回答的问题只有一个：

`当一条 UniTask 链的异常没有被正常 await 消费时，这个错误最后如何重新进入可观测世界。`

### 3. OperationCanceledException 默认不往“未观察异常”里传播

这是另一个很重要的选择。

`PropagateOperationCanceledException` 默认是 `false`。这意味着：

- 取消不是默认要大张旗鼓上报的错误
- UniTask 默认把取消看作一种经常发生、并且往往属于正常收口的终局

这和前面 `06` 的生命周期文章完全接得上：

- Unity 里很多取消本来就不该被看作异常事故
- 如果每次页面关闭、对象销毁、切场景都把取消当成未观察异常往外打，日志会立刻失真

所以 UniTask 的异常模型从这里就已经体现出 Unity 偏好：

`默认强调可观测性，但不把“上下文正常终止”误当作错误洪水。`

## 第三层：Forget 到底做了什么，它不是“吞掉错误”，而是“替你接管观察义务”

很多团队把 `Forget()` 直觉理解成：

- 不要结果了
- 静默执行
- 等价于“别管它”

这正好是最危险的误解。

如果看 `UniTaskExtensions.cs` 里的 `Forget(this UniTask task)`，它做的事情其实非常具体：

- 先取 awaiter
- 如果已经完成，立刻 `GetResult()`
- 如果还没完成，就通过 `SourceOnCompleted` 注册一个回调
- 在回调里再次 `GetResult()`
- 一旦 `GetResult()` 抛异常，就调用 `UniTaskScheduler.PublishUnobservedTaskException(ex)`

也就是说，`Forget()` 的真实语义不是：

- “把错误吞掉”

而是：

- “我放弃显式 await，但我仍然承担把最终异常送进未观察异常出口的责任”

这和很多人心里的“忽略返回值”完全不是一回事。

### 1. Forget 不是忽略协议，而是改写异常回收路径

如果你正常 `await`：

- 错误会在 `await` 点重新抛出
- 调用链可以继续 try/catch 或往上冒泡

如果你 `Forget()`：

- 错误不再回到某个显式调用方
- 但它也不会无声消失
- 它会转移到 `UniTaskScheduler.PublishUnobservedTaskException`

所以 `Forget()` 的实质是：

`你放弃了结构化等待，但没有放弃异常发布。`

### 2. Forget 的危险不在于“会丢异常”，而在于“异常失去了原本的调用链语境”

这是工程上更麻烦的地方。

因为一旦走 `Forget()`：

- 你当然还能看到错误
- 但错误已经不再自然挂在原业务调用栈的控制流上
- 它会作为某个未观察异常，在另一个统一出口出现

这会直接带来排障代价：

- 谁发起了这条异步链
- 它本来属于哪个 UI 页面或系统模块
- 当时为什么没有等待它
- 它失败时对象和上下文还在不在

这些信息都不再像正常 `await` 那样天然保留在“谁 catch 了它”这条路径上。

所以 `Forget()` 不是不能用，而是：

`一旦用了，你就把异常的消费方式从结构化控制流改成了全局发布。`

### 3. `Forget(task, exceptionHandler, handleExceptionOnMainThread)` 其实暴露了 UniTask 对 Unity 现场的判断

UniTask 还提供了带 `exceptionHandler` 的 `Forget` 变体，而且默认 `handleExceptionOnMainThread = true`。

内部逻辑是：

- `await task`
- 失败就 catch
- 如有需要，先 `await UniTask.SwitchToMainThread()`
- 再调用传入的 `exceptionHandler`
- 如果 handler 自己再炸，再继续发布到 `UnobservedTaskException`

这个设计很说明问题。

它说明 UniTask 非常明确地预判到：

- 你的异常处理器常常不是纯后台逻辑
- 它很可能要写 Unity 日志、改 UI、上报引擎对象相关信息
- 所以“先回主线程再处理异常”在 Unity 里通常比纯线程安全更重要

也就是说，UniTask 对 fire-and-forget 的态度不是“反正没人等了，在哪处理都行”，而是：

`就算没人 await，错误也尽量要回到 Unity 主线程的可观测现场。`

## 第四层：UniTaskVoid 为什么危险，它危险在“没有结果壳”，不是因为语法像 async void

再看 `UniTaskVoid`。

源码里的 `UniTaskVoid` 本体几乎是空的：

- 它只是一个带 `AsyncUniTaskVoidMethodBuilder` 的只读 struct
- 公开方法只有一个空的 `Forget()`

这意味着：

- 你拿不到一个可 await 的结果壳
- 你也不会再通过返回值去结构化接住错误

真正关键的是它的 builder。

### 1. `AsyncUniTaskVoidMethodBuilder.SetException` 直接发布未观察异常

`AsyncUniTaskVoidMethodBuilder` 的 `SetException(Exception exception)` 非常直接：

- 如果有 runner，就先归还 runner
- 然后立刻 `UniTaskScheduler.PublishUnobservedTaskException(exception)`

这和 `async Task` / `async UniTask` 的思路完全不同。

对于可 await 的异步结果，异常通常会先被封装进那次操作的完成语义里，等调用方 `await/GetResult` 时再重新抛出。 

但 `UniTaskVoid` 没有这个“结果壳”。于是它的异常只能立刻被送进未观察异常发布路径。

也就是说：

`UniTaskVoid 不是“以后再观察”，而是“根本不存在一个可供以后观察的结果对象”。`

这就是它真正危险的地方。

### 2. UniTaskVoid 适合的不是“普通异步业务”，而是“签名上只能是 void 的桥接入口”

由于 `UniTaskVoid` 没有结构化结果壳，它更适合的场景其实非常窄：

- Unity 事件 / 回调签名必须是 `void`
- 你需要一个异步实现，但调用点没有返回值通道
- 你清楚地知道异常会走未观察异常路径，或者你显式在内部 catch 掉了

它并不适合被当成：

- “普通业务异步默认返回类型”
- “省得写 await 的快捷方式”
- “反正最后 Forget 一下”的日常替代品

否则你等于主动放弃了：

- 结果回传
- 上层组合
- 正常 await 异常传播
- 更清晰的控制流

### 3. `UniTask.Void(...)` 工厂和 `UnityAction(...)` 包装器，本质上都在承认同一件事

`UniTask.Factory.cs` 里有一系列辅助方法，会把 `Func<UniTaskVoid>`、`Func<T, UniTaskVoid>` 之类包装成：

- `Action`
- `UnityAction`
- 各种带参回调

而这些包装内部几乎都是：

- 调用 asyncAction
- 然后 `.Forget()`

这再次说明：

`UniTaskVoid` 存在的主要价值，不是建设一个更强的返回类型，而是帮助你在“签名只能是 void”的 Unity 世界里，把异步逻辑接进来，同时把异常导向统一发布路径。`

## 第五层：为什么 UniTask 要把未观察异常尽量派发回主线程

`UniTaskScheduler` 在 Unity 下还有两个特别值得注意的默认值：

- `DispatchUnityMainThread = true`
- 默认日志写入类型是 `Exception`

它在 `PublishUnobservedTaskException` 里会判断：

- 如果当前已经在主线程，直接触发事件
- 否则通过 `PlayerLoopHelper.UnitySynchronizationContext.Post(...)` 投回 Unity 主线程

这说明 UniTask 对“异常可观测性”的定义，不只是“ somewhere 被记录下来 ”，而是：

`尽量让未观察异常重新回到 Unity 主线程这个排障中心。`

### 1. 因为 Unity 的错误上下文通常以主线程为中心

很多异常后续要做的事都依赖主线程：

- 打 Console 日志
- 把错误和当前对象 / 场景 / UI 状态对齐
- 做一些引擎相关的清理或兜底
- 让开发者看到“这个错误是在哪个 Unity 现场被发现的”

如果未观察异常在任意后台线程直接乱飞，Unity 项目的排障体验会更差。

### 2. 这也意味着 fire-and-forget 错误会更像“全局运行时事件”，而不是局部控制流失败

一旦异常被投到 `UnobservedTaskException` 事件或主线程日志，它的语义就变了：

- 它不再是“某个 await 调用失败”
- 更像“运行时里出现了一条没人正常消费的异步错误”

这是一种更接近事件流的可观测性，而不是结构化异常传播。

所以项目里只要 fire-and-forget 过多，异常流就会越来越像：

- 统一总线
- 全局异常事件
- 失去精确归属感的运行时报警

这也是为什么团队不能把 `Forget()` 当成随手写法。

## 第六层：真正的工程风险不在“会不会报错”，而在“异常模型被分叉了”

现在把这些机制压回工程现场。

一旦项目里同时存在：

- 正常 `await` 的结构化异步
- `Forget()` 的未观察异常路径
- `UniTaskVoid` 的立即发布路径
- 生命周期取消导致的正常终止路径

你的异常模型其实已经分叉成了至少三类出口：

1. 正常 await 的异常
2. fire-and-forget 后转入 `UnobservedTaskException` 的异常
3. 默认不传播到未观察异常的取消

如果团队没有统一约束，很快就会出现下面这些症状。

### 1. 同类问题在不同代码里走不同出口

有的地方：

- `await` 然后在调用方 catch

有的地方：

- `.Forget()` 然后走全局未观察异常

有的地方：

- `UniTaskVoid` 直接发布

有的地方：

- 自己 catch 然后吞掉

这会让“异常到底应该去哪处理”失去一致答案。

### 2. 日志能看见错误，但看不见责任边界

未观察异常最大的工程代价不是“没有日志”，而是：

- 有日志，但上下文已经被拉平了
- 你看得到某个 ex，却不容易看见原始业务调用点是否本该等待它
- 你也不容易区分这条错误到底属于“可忽略后台动作”还是“本该阻塞主流程的关键链路”

### 3. 团队会误把 `Forget` 当作“语法消音器”

这是最危险的使用退化。

当某条异步链不好接返回值时，最容易出现的偷懒写法就是：

- “先 `.Forget()` 吧”

但一旦这样写，真实后果不是“事情结束了”，而是：

- 你把异常语义改道了
- 你把控制流问题变成了运行时事件问题
- 你把局部可推理性换成了全局排障成本

## 第七层：更稳的使用原则是什么

这篇不写成规则大全，但必须把工程判断压成几条硬原则。

### 原则一：能返回 `UniTask/UniTask<T>` 的业务函数，不要默认写成 `UniTaskVoid`

`UniTaskVoid` 不是“更方便的 UniTask”，而是“没有结果壳，只能走未观察异常路径的桥接类型”。

### 原则二：`Forget()` 只能用在“结果确实不需要上层组合，但异常仍然必须可观测”的地方

也就是说，先回答两个问题：

- 上层是否真的不需要等待结果
- 这条链如果失败，走全局未观察异常出口是否仍然可接受

只要第二个问题答案含糊，`Forget()` 就不该轻易写。

### 原则三：对需要自定义错误处理的 fire-and-forget，优先显式提供 handler，而不是假设全局日志足够

`Forget(task, exceptionHandler, handleExceptionOnMainThread)` 的存在，本来就是为了说明：

- 有些 fire-and-forget 的错误你其实知道该去哪处理
- 那就别等它漂到全局异常总线再说

### 原则四：取消默认不是异常洪水的一部分，要和真正错误分开看

否则只要生命周期收口稍微复杂一点，日志立刻会被取消刷爆，真正的未观察错误反而看不见。

### 原则五：把未观察异常视为“系统设计压力报警”，而不是普通业务分支

一条错误如果总是以 `UnobservedTaskException` 的形式出现，通常意味着至少有一个设计问题：

- 这条链本来就不该 fire-and-forget
- 或者它虽然应该 fire-and-forget，但没有局部 handler
- 或者它所处的入口需要更明确的桥接层，而不是直接把 `Forget()` 暴露给业务层

## 常见误解

### 误解一：`Forget()` 就是把异常吞掉

不对。

`Forget()` 没有吞异常，它只是把异常从结构化 await 路径改道到 `UniTaskScheduler.PublishUnobservedTaskException`。

### 误解二：`UniTaskVoid` 只是 UniTask 的无返回值版本

不对。

`UniTaskVoid` 最大的区别不是“没有返回值”，而是“没有可供以后 await/观察的结果壳”；异常会直接走未观察异常发布路径。

### 误解三：既然 UniTask 有 `UniTaskScheduler`，那它本质上和 TaskScheduler 也差不多

不对。

UniTask 源码自己就说了，它没有像 `TaskScheduler` 那样的 scheduler 世界；这个类型主要负责未观察异常出口，不负责一般 continuation 调度。

### 误解四：只要全局订了 `UnobservedTaskException`，项目里随便 `Forget()` 也问题不大

也不对。

全局订阅只能保证错误最终可见，不能自动替你恢复原本的责任边界、业务归属和控制流可推理性。

## 最后把这件事压成一句话

`UniTask 对 fire-and-forget 的处理，不是“允许你放心忽略结果”，而是“在你放弃结构化等待之后，仍然强制把异常导向一个统一的可观测出口”；Forget 会把错误改道到未观察异常发布，UniTaskVoid 更是从一开始就没有可供以后观察的结果壳。也正因为如此，Unity 项目里真正危险的从来不是有没有报错，而是你是否在不知不觉间，把本该局部消费的异步失败，改写成了全局运行时事件。`
