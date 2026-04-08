# Unity 异步运行时：从 Task 到 UniTask 系列规划

## 定位

这组文章不是 `UniTask 入门教程`，也不是 `async/await API 速查表`。

它真正想解决的问题是：

`把 Unity 里的异步执行从问题空间、运行时模型、源码实现、工程风险四层接起来，讲清楚 Task 为什么会在 Unity 里错位，UniTask 为什么会长出来，以及引入这套系统后到底多了哪些能力和麻烦。`

一句话说，这个系列的重点不是“会不会用 UniTask”，而是：

`看不看得懂 Unity 异步运行时为什么会这样长出来。`

## 为什么值得单独做成系列

如果只写一篇 `UniTask：async/await 在 Unity 中的正确用法`，后面很快会遇到四个问题：

- `Task`、`线程`、`async`、`PlayerLoop`、`主线程切回` 会被混成一团
- 读者会把 UniTask 误解成“更轻量一点的 Task”，看不到它其实改了调度语义
- 真实工程里的坑不会先出在语法，而是出在生命周期、异常、取消、AOT、调度边界
- 只讲 API 用法，会把真正有价值的源码层内容全部压扁

所以更稳的做法不是写一篇插件文，而是固定一条上位主线：

`线程与任务基础 -> Unity 执行模型 -> Task 在 Unity 里的错位 -> UniTask 的最小内核 -> PlayerLoop 调度 -> builder / source 协议 -> 帧时序原语 -> 生命周期与异常 -> 外延场景与风险`

## 系列边界

### 属于这个系列的内容

- `Task` 在 Unity 里为什么不天然顺手
- CPU 硬件线程、操作系统线程、语言运行时任务这三层概念怎样区分
- Unity 里的主线程、渲染线程、Job Worker、ThreadPool、PlayerLoop 分别是什么
- `async/await`、`SynchronizationContext`、continuation 到底在调度什么
- UniTask 的 `struct + source + token` 数据模型
- UniTask 的 `PlayerLoop` 注入、runner、builder、completion source、帧时序原语
- 引入 UniTask 后在生命周期、取消、异常、未观察错误上的新增复杂度
- 与 `AsyncOperation`、`Addressables`、加载链、IL2CPP / HybridCLR 的交点

### 不属于这个系列的内容

- 完整的 C# 语言入门
- 完整的操作系统线程调度教程
- 完整的 Unity Job System / DOTS 教程
- 完整的 Addressables / YooAsset 教程
- 完整的 HybridCLR 系列

这组文章只覆盖这些主题与 Unity 异步运行时的交点，不吞掉它们各自的完整体系。

## 源码与证据范围

### 1. UniTask 源码

当前已确认本地可用：

- `E:\NHT\workspace\UniTask-master`

这一层主要负责回答：

- UniTask 的最小数据模型是什么
- 调度层到底建在 `PlayerLoop` 的哪一层
- builder、source、completion source 是怎样接起来的
- `Yield / NextFrame / Delay / WhenAll / Cancellation` 这些常用能力在实现层分别落在哪里

当前已经确认的关键源码锚点包括：

- `src/UniTask/Assets/Plugins/UniTask/Runtime/UniTask.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/IUniTaskSource.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/PlayerLoopHelper.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/Internal/PlayerLoopRunner.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/AsyncUniTaskMethodBuilder.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/UniTaskCompletionSource.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/UniTask.Delay.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/UniTask.WhenAll.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/UniTaskScheduler.cs`
- `src/UniTask/Assets/Plugins/UniTask/Runtime/CancellationTokenExtensions.cs`

### 2. 现有专题规划与文章证据

当前主仓库里已经有几个相关落点：

- 根规划里的 `插件-05`：`UniTask：async/await 在 Unity 中的正确用法`
- 根规划里的 `加载-01`：`异步加载管线：AsyncOperation / UniTask / 协程在加载中的正确用法`
- HybridCLR 系列里已经存在 async / UniTask 在 AOT 与热更环境下的故障诊断文章

这意味着：

- UniTask 不适合继续留成一篇孤立插件文
- 加载链适合做 UniTask 的应用外篇，不并回源码主线
- HybridCLR 风险适合做延伸阅读，不在 UniTask 主线里重写一遍

### 3. 统一写法约束

整组系列统一遵守下面这些约束：

- 先回答“为什么会这样”，再讲“接口怎么用”
- 明确区分线程、任务、异步等待、分帧调度，不混用术语
- 不按文件目录平铺源码，只追一条主链
- 明确区分“源码直接证明的事实”和“从事实反推的工程判断”
- 每篇只回答一个主问题，不把调度、取消、异常、加载链全塞到一篇里
- 每篇都专门写“常见误解”
- 每篇结尾必须收回到工程动作，而不是停在概念解释

## 与现有大规划的关系

这组专题建议这样挂接到当前总规划：

- `插件-05` 不再定位成单篇教程，改成专题入口或索引页
- `加载-01` 保留在“打包、加载与流式系统”里，作为 UniTask 的应用外篇
- HybridCLR 已有 async / UniTask 风险文章继续留在原系列里，只在 UniTask 专题中做导读链接

也就是说，这组文章是：

`一个新的小专题主线 + 一个加载应用外篇 + 一个现有风险系列的桥接入口`

## 前置番外

这些文章不属于 UniTask 主线编号，但决定读者进入主线时是否会把概念混掉。

| 编号 | 标题 | 角色 | 状态 |
|------|------|------|------|
| 番外-01 | “线程”这个词其实指三层东西：CPU 硬件线程、操作系统线程、语言运行时任务 | 概念拆分 | 待写 |
| 番外-02 | Unity 的执行模型不是只有主线程：主线程、渲染线程、Job Worker、ThreadPool、PlayerLoop 分别负责什么 | Unity 现场地图 | 待写 |
| 番外-03 | async/await 不是多线程：Continuation、SynchronizationContext、TaskScheduler 到底在调度什么 | Task 语义前置 | 待写 |
| 番外-04 | 什么时候该用线程，什么时候只是异步等待，什么时候该分帧 | 工程判断补篇 | 可选 |

### 番外职责说明

#### 番外-01

唯一职责：

`把“线程”这个词拆成 CPU、操作系统、语言运行时三个层次，并讲清为什么会有多线程，以及它带来的收益和代价分别是什么。`

这一篇要明确回避的内容：

- 不讲锁、原子操作、内存序的全部细节
- 不展开成完整 OS 并发教程
- 不提前吞掉 Unity 执行模型

#### 番外-02

唯一职责：

`把 Unity 项目里的真实执行现场立住，让读者知道“代码到底在谁的线程、谁的循环、谁的阶段里跑”。`

#### 番外-03

唯一职责：

`把 async/await 从“自动开线程”的误解里拉出来，解释 continuation 被谁保存、被谁恢复、被谁调度。`

#### 番外-04

唯一职责：

`把 CPU 密集、I/O 等待、Unity API、分帧逻辑分别归到正确执行模型上，给出判断框架。`

## 主线结构

这组主线不按“API 分类”拆，也不按“扩展方法分组”拆。

更稳的主线是：

`Task 错位 -> UniTask 最小内核 -> PlayerLoop 调度 -> builder / 状态机 -> source 协议 -> 帧时序原语 -> 生命周期与异常`

### 主线目录

| 编号 | 标题 | 角色 | 状态 |
|------|------|------|------|
| 00 | Task 在 Unity 里到底错位在哪，为什么会长出 UniTask | 问题空间 | 待写 |
| 01 | UniTask 的最小内核：struct、source、token 为什么要这样设计 | 数据模型 | 待写 |
| 02 | PlayerLoop 注入：UniTask 真正的调度器到底在哪里 | 调度层 | 待写 |
| 03 | async UniTask 方法是怎么跑起来的：builder、状态机、runnerPromise | builder 接线层 | 待写 |
| 04 | 单次消费不是限制，而是协议：CompletionSource、version、token 校验 | source 协议层 | 待写 |
| 05 | 帧时序语义：Yield、NextFrame、Delay、DelayFrame 到底差在哪 | 时间原语层 | 待写 |
| 06 | 引入这套系统后最容易出事的地方：取消、销毁、PlayMode 退出、生命周期收口 | 生命周期边界 | 待写 |
| 07 | 异常去哪了：Forget、UniTaskVoid、未观察异常和主线程派发 | 异常与排障 | 待写 |

## 每篇核心问题

### 00｜Task 在 Unity 里到底错位在哪，为什么会长出 UniTask

核心问题：

`Task 的默认世界观为什么和 Unity 的主线程、PlayerLoop、帧时序、对象生命周期不完全兼容。`

这一篇必须覆盖：

- `Task` 不是错，错位来自默认调度语义
- `Task.Delay` 与“下一帧 / 固定帧 / 渲染尾部”不是同一类时间概念
- Unity API 访问边界为什么让“线程池思维”不够用
- UniTask 解决的是“运行时错位”，不只是“减点 GC”

### 01｜UniTask 的最小内核：struct、source、token 为什么要这样设计

核心问题：

`为什么 UniTask 本体这么薄，而真正的语义要藏在 source 和 token 里。`

源码主锚点：

- `UniTask.cs`
- `IUniTaskSource.cs`

这一篇必须覆盖：

- `UniTask` 只是一个 `struct` 壳
- `IUniTaskSource` 怎样承载真正的完成、异常、取消语义
- `token` 为什么存在
- 为什么这套设计天然会带来“单次消费”约束

### 02｜PlayerLoop 注入：UniTask 真正的调度器到底在哪里

核心问题：

`UniTask 为什么不靠 TaskScheduler，而要把自己的 runner 和 queue 注入 Unity PlayerLoop。`

源码主锚点：

- `PlayerLoopHelper.cs`
- `Internal/PlayerLoopRunner.cs`

这一篇必须覆盖：

- `PlayerLoopTiming` 的层次和含义
- runner / queue 在注入层分别承担什么角色
- continuation 是怎样被插进 Unity 主循环的
- 引入这层基础设施后，会给项目带来哪些新的初始化与兼容风险

### 03｜async UniTask 方法是怎么跑起来的：builder、状态机、runnerPromise

核心问题：

`async UniTask` 并没有绕开 C# 状态机，它只是换了 builder 和承接协议。`

源码主锚点：

- `CompilerServices/AsyncUniTaskMethodBuilder.cs`

这一篇必须覆盖：

- builder 为什么反而写得很薄
- `Start`、`AwaitOnCompleted`、`AwaitUnsafeOnCompleted` 分别在接什么
- `runnerPromise` 为什么是关键点
- 为什么真正“重”的不在 builder 文件里

### 04｜单次消费不是限制，而是协议：CompletionSource、version、token 校验

核心问题：

`为什么很多 UniTask 结果不能随便多次 await，这不是偶然限制，而是协议设计。`

源码主锚点：

- `UniTaskCompletionSource.cs`

这一篇必须覆盖：

- `UniTaskCompletionSourceCore<TResult>` 的状态布局
- `TrySetResult / TrySetException / TrySetCanceled`
- `OnCompleted` 怎样处理竞态
- `version` 和 `token` 怎样阻止二次消费
- 未观察异常为什么会在这里开始积累

### 05｜帧时序语义：Yield、NextFrame、Delay、DelayFrame 到底差在哪

核心问题：

`UniTask 的等待原语为什么不是“更方便的 Delay”，而是在显式表达帧阶段与时间语义。`

源码主锚点：

- `UniTask.Delay.cs`

这一篇必须覆盖：

- `Yield` 与 `NextFrame` 的语义差异
- `DelayFrame` 与 `Delay(TimeSpan)` 的边界
- `DeltaTime / UnscaledDeltaTime / Realtime` 三种时间模型
- 为什么这部分才是 UniTask 最有 Unity 味的地方

### 06｜引入这套系统后最容易出事的地方：取消、销毁、PlayMode 退出、生命周期收口

核心问题：

`真正难的不是写出 await，而是对象、页面、场景、PlayMode 已经结束时，continuation 到底该不该继续跑。`

源码主锚点：

- `CancellationTokenExtensions.cs`
- `PlayerLoopHelper.cs`

这一篇必须覆盖：

- `CancellationToken` 在 UniTask 世界里的角色
- `RegisterWithoutCaptureExecutionContext` 为何存在
- 生命周期收口为什么会成为工程硬要求
- 引入 UniTask 后最常见的资源泄漏、悬挂 continuation 和退出期错误

### 07｜异常去哪了：Forget、UniTaskVoid、未观察异常和主线程派发

核心问题：

`UniTask 没有 TaskScheduler 那套世界，它自己的异常出口、未观察异常策略和主线程派发是怎样工作的。`

源码主锚点：

- `UniTaskScheduler.cs`
- `UniTaskCompletionSource.cs`

这一篇必须覆盖：

- 未观察异常的发布路径
- `OperationCanceledException` 为什么默认不向外传播
- 主线程派发为什么可能影响日志和排障体验
- `Forget` 和 `UniTaskVoid` 为什么是工程上最危险的几个入口

## 外延篇

这些文章不进入主线编号，但和主线有天然连接关系。

| 编号 | 标题 | 角色 | 状态 |
|------|------|------|------|
| 外篇-A | WhenAll / WhenAny / WhenEach：UniTask 的并发组合怎么收拢结果和异常 | 并发组合 | 待写 |
| 外篇-B | Unity bridge：AsyncOperation、UnityWebRequest、Addressables 为什么需要专门桥接层 | Unity 应用桥接 | 待写 |
| 外篇-C | UniTask 在 IL2CPP / HybridCLR 下为什么会放大风险 | AOT / 热更风险桥接 | 待写 |

### 外延职责说明

#### 外篇-A

唯一职责：

`说明 UniTask 在组合多个异步结果时，怎样收拢完成、异常与返回值，以及这种组合方式和 Task 世界有什么相同与不同。`

#### 外篇-B

唯一职责：

`说明 Unity 原生异步对象不是 Task 世界里的原生居民，所以 UniTask 为什么必须提供 bridge，而这些 bridge 如何与加载链接起来。`

这一篇建议与总规划里的 `加载-01` 互相导读。

#### 外篇-C

唯一职责：

`说明 UniTask 一旦进入 IL2CPP、AOT 泛型、HybridCLR 热更环境，为什么很多“平时能跑”的写法会升级成真机风险。`

这一篇建议只做桥接，不重写已有 HybridCLR 系列。

## 推荐写作顺序

### 第一阶段：先把问题空间和前置地基立住

建议顺序：

1. 番外-01
2. 番外-02
3. 番外-03
4. 主线-00

这一阶段的目标不是开始拆源码，而是先消灭四个最高频误解：

- `async` 就是多线程
- `await` 就是后台跑
- UniTask 只是更轻量一点的 Task
- Unity 异步问题只和语法有关

### 第二阶段：先拿最有辨识度的主干

建议顺序：

1. 主线-02
2. 主线-06
3. 主线-07

这一阶段优先写调度、生命周期、异常，是因为：

- 这些内容最能体现 UniTask 与普通 Task 文章的差异
- 这些内容最能直接回应“引入这套系统后会多出哪些麻烦”
- 这些内容也最能反向支撑前面的 Task 痛点分析

### 第三阶段：再回头拆最底层协议

建议顺序：

1. 主线-01
2. 主线-03
3. 主线-04
4. 主线-05

这一阶段适合在读者已经接受问题空间之后，回头拆最底层实现。

### 第四阶段：补应用和风险外延

建议顺序：

1. 外篇-A
2. 外篇-B
3. 外篇-C

## 暂不展开的内容

下面这些方向可能以后会写，但不应抢走当前主线：

- 完整 C# 并发基础教程
- 完整 Unity Job System / DOTS 调度专题
- 完整 Addressables / YooAsset 系列
- 完整网络 I/O / Socket async 系列
- 完整 HybridCLR 深度系列

这些内容和 UniTask 会发生交点，但不应吞掉这组文章的主线。

## 当前最短结论

`UniTask 不该继续留在“项目常用插件”里做一篇用法文，而应该升级成一个“前置番外 + 源码主线 + 应用外延”的 Unity 异步运行时专题。`
