---
date: "2026-04-08"
title: "Unity 异步运行时 04｜CompletionSource、Version、Token：为什么 UniTask 天然会长出单次消费协议"
description: "UniTask 的 source/completion 协议不是 TaskCompletionSource 的轻量翻版，而是一套围绕复用、竞态收口、单次消费和未观察错误发布建立起来的协议面。读懂 Version、token、OnCompleted、GetResult 与 TrySet* 之间的关系，才能理解 UniTask 为什么会在多次 await、状态探测、取消与异常表面上表现得和 Task 很不一样。"
slug: "unity-async-runtime-04-completion-source-version-token"
weight: 2368
featured: false
tags:
  - "Unity"
  - "Async"
  - "UniTask"
  - "CompletionSource"
  - "ValueTaskSource"
  - "Runtime"
series: "Unity 异步运行时"
primary_series: "unity-async-runtime"
series_role: "article"
series_order: 14
---

> 如果只用一句话概括这篇文章，我会这样说：`UniTask 的单次消费、token/version 校验、取消与异常表面的“别扭感”，不是附加限制，而是 source/completion 协议在“可复用、低分配、竞态可收口”这三个目标下的自然结果。`

在 Task 世界里，很多开发者的直觉是这样的：

- 一个异步结果对象创建出来后，可以被多处观察
- 完成、异常、取消只是同一个“最终状态”的不同取值
- `await` 只是消费这个状态，不会对对象本身施加太多协议约束
- 如果拿到了句柄，多次 `await`、轮询 `Status`、晚一点再 `Result`，通常都在语义上成立

这套直觉放到 `Task` 上大体没问题，因为 `Task` 的对象身份、生命周期和状态存储方式，本来就更偏向“堆上长期存在、可以被多个观察者读取的完成体”。

但 UniTask 不是沿着这条路长出来的。

它在前文已经确立了两个前提：

- UniTask 不是一个肥大的引用类型任务对象，而是更薄的结果壳
- 真正承载状态的，是 source 一侧的 completion 协议，而不是 `UniTask` 这个表面值本身

只要把中心从“我拿到一个任务对象”切换到“我拿到一个带 token 的 source 视图”，后面的很多现象就都变得合理了：

- 为什么要有 `Version`
- 为什么外部拿到的是 `token`
- 为什么 `OnCompleted` 会对多次注册非常敏感
- 为什么会出现 “can not await twice” 这类错误
- 为什么取消和异常并不只是一个普通布尔位，而会牵扯“是否已被观察”

这一篇要讲的，不是 API 使用清单，而是：

`UniTaskCompletionSource 这层 completion 协议，到底在保护什么。`

## 第一层：问题不在“怎么完成”，而在“谁来拥有完成的真相”

很多人第一次看 completion source，会把它理解成一个简化版的 `TaskCompletionSource`：

- 外部创建一个 source
- 未来某个时刻 `TrySetResult`
- 消费方 `await` 这个结果

如果停在这个抽象层，UniTask 看起来并没有多特别。

真正的分水岭在于：

`UniTask 的 source 不只是“把结果塞进去”的地方，它还是“并发竞态如何收口、异常是否已被观察、这个槽位是否已经被复用”的唯一真相源。`

这时 completion 协议需要同时回答五个问题：

1. 当前这次操作到底有没有完成
2. 完成的是成功、取消还是失败
3. continuation 是先注册还是先完成，谁来接住竞态
4. 这个 source 槽位是不是已经被下一次操作复用了
5. 异常或取消如果没有被消费，应该如何暴露出去

注意，这五个问题不是并列功能点，而是一套联动约束。

如果 source 可以被池化复用，那么“对象身份”和“这一次异步操作身份”就已经不是同一回事了。  
如果 continuation 的注册和完成信号可能并发发生，那么 `OnCompleted` 和 `TrySet*` 就必须共享一套竞态收口协议。  
如果异常可能没被读到，那么 “faulted” 不只是一个状态，还意味着“未观察错误是否要被发布”。

也就是说，在 UniTask 里，completion 协议的职责远比 `SetResult` 大：

`它在定义一次异步操作从挂接、完成到被消费的完整交易边界。`

## 第二层：为什么 Version 和 token 不是装饰，而是“操作身份”

看 `UniTaskCompletionSourceCore<TResult>` 这一层时，最容易低估的是两个字段：

- `short version`
- 所有 `GetStatus/GetResult/OnCompleted` 都要求传入的 `token`

这套设计的意思不是“多传一个参数更安全”，而是：

`source 对象本身不等于某一次异步操作；真正代表“这一次操作”的，是 source 引用加上当前 version。`

换句话说：

- source 是“可复用槽位”
- version 是“当前槽位上的操作代号”
- token 是“把这个代号带到消费者侧去”

这就是为什么 `Reset()` 会递增 `version`。一旦槽位进入下一轮生命周期，旧消费者手里的 token 就必须失效，否则旧 continuation、旧状态探测、旧 `GetResult` 都可能读到下一轮操作的状态。

这个问题在 `Task` 世界里没有这么尖锐，因为：

- `Task` 更倾向于“一次分配对应一次任务身份”
- 对象地址本身就已经足够表达“我正在等待哪一个任务”

但 UniTask 明确在压缩分配和支持复用。复用一旦成立，就必须把“对象身份”和“操作身份”拆开。  
于是 version/token 不是锦上添花，而是最低成本的隔离机制。

### 为什么 ValidateToken 会报“can not await twice”

源码里的 `ValidateToken` 很直接：传入 token 不等于当前 version，就抛错。

很多人第一次看到这类异常，会以为这是 UniTask 人为设置的高压线：

- 不让你二次 await
- 不让你随便查状态
- 限制过严

但更准确的理解是：

`它不是在阻止“多写几次 await”，而是在阻止“把属于旧操作的消费行为，错误地施加到新操作上”。`

你可以把它想成租车场景：

- source 是同一辆车位
- version 是这次租赁单号
- token 是你手上的取车码

一旦车位复用了，旧取车码继续有效就会导致身份错乱。  
对 source 来说，这种错乱比“报错严格”更危险，因为它会静默串台。

所以 token/version 校验的本质不是限制消费者自由，而是：

`在允许 source 复用的前提下，强制把一次异步消费绑定到唯一的一次操作身份。`

## 第三层：OnCompleted 真正解决的不是回调注册，而是完成竞态

如果只看函数名，`OnCompleted` 很容易被误解成：

“把 continuation 存起来，等将来完成时调一下。”

这只是表面现象。它真正困难的地方在于：

`continuation 注册和完成信号到达，本身就是一组并发竞态。`

源码注释其实已经把问题说得很清楚，大致对应三种模式：

- 先看到 pending，再注册 continuation，之后完成
- 先完成，再看到非 pending，之后直接取结果
- 先看到 pending，但 `TrySet*` 和 `OnCompleted` 在边界上撞车

真正的难点都在第三种。

### 为什么必须有 sentinel

在 `UniTaskCompletionSourceCore<TResult>` 里，`continuation` 并不只是一个普通字段，它还承担“这个注册阶段现在处于什么状态”的信号作用。

源码里用一个共享 sentinel 作为占位，本质上是在表达：

- 还没有人注册 continuation
- 已经有人注册了 continuation
- 完成路径已经抢先一步接管，后续注册必须立即执行或报错

这不是为了写法花哨，而是为了保证：

- `TrySet*` 如果先赢，后来的 `OnCompleted` 不能把 continuation 永远丢在字段里没人调
- `OnCompleted` 如果先赢，后来的 `TrySet*` 必须准确调用这个已保存 continuation
- 双方不能都以为“轮到对方来调”

从协议角度看，`OnCompleted` 不是一个“订阅接口”，而更像一次原子交接：

`要么把 continuation 正式交给 source；要么发现 source 已经完成，那就立刻在当前路径上把这次恢复做掉。`

### 为什么会出现 “Already continuation registered”

很多开发者把这个异常理解成“UniTask 不支持多订阅”，这句话不算错，但还是太表面。

更深一层的原因是：

`这一层 core 默认只承诺一条 continuation 链。`

为什么只能承诺一条？因为它解决的问题是“一个 await 状态机怎样恢复”，不是“发布一个可供任意订阅者观察的完成事件流”。

一旦允许多个 continuation 同时挂到同一个 core 上，协议复杂度会立刻暴涨：

- continuation 存储从单槽位变成列表
- 完成路径要遍历多订阅者
- 异常传播和已观察语义会变得更复杂
- 更重要的是，source 池化复用时，旧订阅泄漏的风险更高

所以 `UniTaskCompletionSourceCore<TResult>` 的默认形态，是为“单次 await 恢复”优化的，而不是为“多观察者广播”优化的。

这就解释了为什么它对重复 `OnCompleted` 会非常敏感。  
在它的协议模型里，第二次注册 continuation 通常不是合理需求，而更可能意味着：

- 同一个 UniTask 被二次 await
- await 之后又去探测状态
- 旧操作引用泄漏到了下一轮复用

也就是说，这个异常更多是在报告“协议被误用”，不是在说“功能缺失”。

## 第四层：GetResult 不只是拿结果，它还在做“消费确认”

很多人在阅读 UniTask 的 completion 协议时，会自然把注意力放在 `TrySetResult/TrySetException/TrySetCanceled` 上，认为完成路径决定一切。  
其实真正定义“这次异步是否被消费”的，是 `GetResult`。

原因很简单：

- 完成侧只是在写入最终状态
- 消费侧的 `GetResult` 才是在确认这次状态已经被观察

这点在异常和取消路径上尤其明显。

### 成功、异常、取消不是三个对称的状态位

在 `TrySetResult` 路径上，source 只需要写入结果，然后在适当时机唤醒 continuation。

但在 `TrySetException` 和 `TrySetCanceled` 路径上，事情多了一层：

- 这次 fault/cancel 有没有被读取
- 如果没有被读取，是否要作为未观察错误上报

这就是为什么 core 内部不只是存 `error`，还会维护 `hasUnhandledError`。  
`GetResult` 在读到异常或取消时，不是单纯把异常抛出去，而是先把“这次错误已被处理”的语义落下去，然后再继续抛出。

从协议上说：

`TrySetException/TrySetCanceled` 定义的是“完成状态”；GetResult 定义的是“观察责任已经被某个消费者承接”。`

如果没人来 `GetResult`，那这次 fault/cancel 在协议上仍然处于“悬空”状态。

### 为什么异常和取消会长出未观察表面

`Task` 用户很容易把取消看成“正常业务分支”，把异常看成“出了问题”。  
但 UniTask 的 core 更关注的是：

`这个 fault/cancel 最终有没有被某个消费路径明确接住。`

你会看到源码里：

- 普通异常被包装进 `ExceptionHolder`
- `OperationCanceledException` 单独保存
- 如果没有在消费阶段被处理，后续会走未观察异常发布路径

这说明 UniTask 把“取消”也放进了一套需要明确消费的完成协议里，而不是简单当成一个不带后果的 `false`。

这件事对工程层面的影响很大：

- `Forget()` 如果没有明确错误处理，就不只是“少等一下结果”
- 取消如果没人接，也可能以未观察错误的方式冒出来
- “已经完成” 不等于 “已经被妥善消费”

从这里开始，UniTask 的异常/取消表面就已经明显不同于很多人对 `Task` 的直觉了。

## 第五层：为什么多次 await 在 UniTask 里更容易暴露为误用

很多人第一次踩 UniTask 的坑，通常不是因为不会写 `await`，而是因为他们带着 `Task` 的消费直觉：

- 我把这个返回值缓存起来，后面再 await 一次
- 我先看 `Status`，晚点再等结果
- 我在两个地方都等这同一个值，应该问题不大

在 Task 语义下，这些写法往往不优雅，但常常还能工作。  
在 UniTask 的 source/core 语义下，它们会更快暴露为错误。

### 误用一：把“一次 await 句柄”当成“可反复观察对象”

UniTask 表面长得像任务值，但对很多由 source/core 支撑的结果来说，它其实更像：

`一次消费权限。`

这个权限里包括：

- 你能否挂 continuation
- 你能否读取结果
- 你手里的 token 是否仍然指向当前这轮操作

一旦把它当成 Task 那样的“可重复观察对象”，就很容易落入二次 await 或过期 token 读状态的问题。

### 误用二：把“状态探测”当成无害读取

在 `Task` 世界里，很多人习惯：

- 先看 `IsCompleted`
- 再决定是否 `await`

UniTask 的 core 对这件事更敏感，因为状态探测本身也走 token 校验，而且和 `OnCompleted` 的注册时序存在耦合。  
源码里直接把一类误用报成：

`Already continuation registered, can not await twice or get Status after await.`

这句报错非常值得注意。它不是只在说“不能 await 两次”，而是在说：

`await 之后再去碰状态探测，同样可能破坏这次单 continuation 协议。`

也就是说，在 UniTask 语义里，“查状态”不是零成本旁路，它也是协议参与者。

### 误用三：把 source 复用场景下的旧引用继续往后传

这类问题最隐蔽，也最危险。

如果某些 awaitable 背后来自池化 source，那么你把旧 UniTask、旧 awaiter、旧 token 继续保留到更晚时刻，再去 `GetResult` 或 `GetStatus`，就不再只是“读错一个值”。

更严重的风险是：

- 误读下一轮操作状态
- continuation 注册到错误的一轮
- 未观察错误的责任被错误转移

正因为这类错太难排查，所以 UniTask 选择在 token/version 层面尽早炸掉，而不是让错误静默渗透。

## 第六层：TrySetResult、TrySetException、TrySetCanceled 为什么长得像“竞争终点”

从名字看，`TrySetResult/TrySetException/TrySetCanceled` 像是三种普通 setter。  
但如果你从 completion 协议角度看，它们更像三种“争夺唯一终点”的提交动作。

共同点有三个：

1. 它们都在争夺“谁第一个把这次操作从 pending 推到 terminal”
2. 只有第一个成功者能决定最终状态
3. 成功推进后，它必须负责 continuation 的唤醒收口

这就是为什么实现里会先判断是否还是 pending，或者通过计数/原子操作确保只有第一个完成者获胜。

### 为什么取消和异常会被显式区分

对很多业务代码来说，取消和异常都只是 `catch` 分支里的不同处理方式。  
但从协议层看，它们被区分开来是有必要的：

- 取消需要保留 `CancellationToken` 语义
- 异常需要保留原始异常堆栈
- 两者在未观察路径上的处理策略虽然都可能上报，但工程含义不同

尤其值得注意的是，源码里会把 `OperationCanceledException` 视为取消通道的一部分，而不是简单混进普通 fault。  
这意味着：

`在 UniTask 的 completion 协议里，取消不是“带特殊类型的失败”，而是独立的终止语义。`

这会直接影响后面的上层 API 设计：

- 哪些地方用 `SuppressCancellationThrow`
- 哪些地方把取消当作业务中断
- 哪些地方必须显式区分 fault 和 canceled

所以别把 `TrySetCanceled` 看成礼貌性补充接口。它是协议中一个一等公民的终点。

## 第七层：为什么 UniTask 的这些限制看起来苛刻，实际却更诚实

写到这里，可以回到文章开头那个最容易引发抵触的问题：

`为什么 UniTask 这么容易报“不能二次 await”“token 不匹配”“已经注册 continuation”之类的错？`

我的判断是：

`因为它把 source/core 这层真实约束暴露得更早、更直接。`

在很多高层抽象里，框架会尽量把误用吞掉，或者转成更宽容的行为：

- 多次观察就多包一层
- 旧引用还活着就尽量兜底
- 取消和异常能不报就不报

这样用户表面上更舒服，但代价通常是：

- 分配更多
- 状态语义更模糊
- 静默串台更难排查
- 运行时真实约束被掩盖

UniTask 走的是另一条路线：

- 允许 source 复用
- 压缩对象开销
- 不默认给你广播式多订阅
- 强制把一次消费绑定到一次操作身份

那它就必须在协议边界上更硬。

这种“硬”不是因为作者主观上讨厌宽容，而是因为：

`当你把抽象做薄、把 allocation 压低、把 PlayerLoop 与 source 协议直接暴露到运行时层面时，很多原本能被堆对象和额外包装掩盖的问题，就必须在边界上被明确表达出来。`

## 第八层：工程上应该怎么消化这套 completion 协议

理解这篇之后，工程上最重要的不是背住报错字符串，而是换掉几个习惯。

### 1. 把很多 UniTask 结果当成“一次消费权”，不要当成“通用共享句柄”

如果你需要多个地方共享完成结果，优先考虑：

- 共享上层业务状态
- 共享已经 materialize 的结果
- 使用明确支持多观察者的抽象

而不是把同一个 UniTask 值到处传、到处 await。

### 2. 不要把 `Status` 探测当成零副作用操作

在 source/core 语义下，状态探测不是完全独立的观察旁路。  
尤其在已经进入 await 语义后，再去做额外状态探测，很容易踩到 continuation 协议边界。

### 3. 区分“完成了”和“被消费了”

只调用 `TrySetException/TrySetCanceled` 不代表事情已经结束。  
如果最终没有消费方进入 `GetResult` 路径，这个 fault/cancel 仍然可能以未观察异常的形式回到你脸上。

### 4. 对池化和复用保持敬畏

一旦一个 awaitable 背后站着复用的 source，对象引用就不再天然等于“这一次操作身份”。  
这时 token/version 的严格性是在帮你防串台，不是在故意刁难你。

### 5. 真要复用结果，先确认复用的是“值”还是“协议对象”

很多缓存设计的问题，不是出在“为什么 UniTask 不能多等几次”，而是出在：

`你真正想缓存的，到底是最终业务值，还是一次 still-pending 的协议对象？`

这两者在工程上完全不是一回事。

## 结语：UniTask 的 completion 协议，本质上是一套“低分配但高纪律”的交易规则

到这里，这篇文章其实可以压成一个更短的结论：

`UniTaskCompletionSource 不是“更轻一点的 TaskCompletionSource”，而是一套围绕 source 复用、竞态收口、单 continuation、观察责任和未观察错误发布建立起来的协议面。`

所以：

- `Version/token` 是操作身份隔离机制
- `OnCompleted` 是 continuation 注册与完成竞态的交接协议
- `GetResult` 不只是取值，还承担“错误已被观察”的确认
- `TrySetResult/TrySetException/TrySetCanceled` 不是普通 setter，而是对唯一终态的竞争提交

只要你接受这个前提，UniTask 那些看起来“苛刻”的限制就会变得很自然：

它不是在模仿 Task 时故意少给你一点自由。  
它是在一套更薄、更接近运行时真相的协议里，把本来就存在的约束提前说清楚。

顺着这条线往下读，最自然的主题不是再看一个 API，而是进入时间语义本身：

`Yield、NextFrame、Delay、DelayFrame 为什么不是几种写法，而是四种不同的运行时承诺。`

