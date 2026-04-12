---
date: "2026-04-08"
title: "Unity 异步运行时 03｜async UniTask 方法是怎么跑起来的：builder、状态机与 runnerPromise"
description: "async UniTask 并没有绕开 C# 的 async 状态机。真正发生的事情是：编译器仍然生成状态机，只是把默认 builder 换成了 UniTask 的 builder，再把 continuation 接到 runnerPromise 这条执行链上。builder 本身很薄，重活在状态机 runner、source 协议和完成路径里。"
slug: "unity-async-runtime-03-builder-state-machine-runnerpromise"
weight: 2367
featured: false
tags:
  - "Unity"
  - "Async"
  - "UniTask"
  - "StateMachine"
  - "Builder"
  - "Runtime"
series: "Unity 异步运行时"
primary_series: "unity-async-runtime"
series_role: "article"
series_order: 13
---

> 如果只用一句话概括这篇文章，我会这样说：`async UniTask 并没有发明一套绕开 C# async 的新执行器，它仍然靠编译器生成的状态机运行；UniTask 真正替换掉的，是 builder 以及 continuation 的接线方式，而 builder 自己其实薄得惊人。`

前面几篇已经把外围问题立住了：

- `Task` 在 Unity 里不是不能用，而是默认语义和引擎运行时之间有错位
- `PlayerLoop` 是 Unity 里真正的“什么时候继续跑”语义中心
- UniTask 的最小结果载体不是 `Task`，而是 `struct + source + token`

现在可以进入一个很容易被误解的位置：

`一个 async UniTask 方法，到底是怎么跑起来的？`

很多人的直觉会在这里分成两种极端：

- 一种以为 UniTask 只是把 `Task` 换了个壳，底下执行链其实没什么不同
- 另一种以为 UniTask 非常“黑科技”，好像直接绕开了 C# async/await 机制，自己在 Unity 里搞了一套全新的解释器

这两种理解都不对。

更准确的说法是：

`UniTask 没有绕开 async 状态机，它换掉的是 builder，并把 continuation 从默认 Task 世界接到了 UniTask 自己的 runnerPromise / source 协议上。`

这篇文章不做编译器 lowering 教程，也不准备按源码文件顺序从头读到尾。我们只做一件事：

`把 async UniTask 的最小执行链摆清楚。`

只要这条链清楚了，后面很多看似零散的问题都会自然连上：

- 为什么 builder 本身代码很短，却能牵动整套执行系统
- 为什么第一次真正遇到异步挂起时，才需要创建 runnerPromise
- 为什么 UniTask 的返回值看起来像立即可得，但背后仍然是状态机推进
- 为什么后面讲 CompletionSource、单次消费、token 校验时，会不断回到这一层
- 为什么 AOT / IL2CPP / 热更环境里，很多 async 问题最后会落到状态机 runner 和泛型实例化上

## 第一层：先把最大误解拿掉，async UniTask 不是“绕开状态机”

如果只看用法，`async UniTask` 很容易给人一种错觉：

- 返回值不是 `Task`
- Await 的对象也经常不是标准 Task awaiter
- continuation 恢复位置还可以和 `PlayerLoop` 深度绑定

于是很多人会顺手脑补出一句话：

`UniTask 应该没有走 C# 那套标准 async 状态机。`

这个判断正好相反。

### 1. 语言层没有被绕开

`async UniTask` 仍然是标准的 C# async 方法。

只要你写的是：

```csharp
public async UniTask FooAsync()
{
    await UniTask.Yield();
    await BarAsync();
}
```

编译器做的核心工作并没有消失：

- 仍然会把方法拆成状态机
- 仍然会生成 `MoveNext`
- 仍然会在每个 `await` 点保存当前状态
- 仍然会用 builder 去承接 `Start`、`AwaitOnCompleted`、`SetResult`、`SetException`

变化不在“有没有状态机”，而在：

`这个状态机最后挂到了哪个 builder 上。`

对 `async Task` 来说，编译器会配标准 Task builder。

对 `async UniTask` 来说，编译器会配 UniTask 自己的 builder，也就是 [AsyncUniTaskMethodBuilder.cs](/E:/NHT/workspace/UniTask-master/src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/AsyncUniTaskMethodBuilder.cs) 里的实现。

这也是为什么这篇文章必须先讲 builder，再讲 runnerPromise。

### 2. UniTask 不是替换语言机制，而是替换“返回协议”和“恢复接线”

把 C# async 想成两层会更容易理解：

- 上层是语言层的状态机展开
- 下层是“这个状态机如何对外暴露结果，以及 continuation 挂到哪里”的运行时协议

UniTask 没去重写第一层。

它主要改的是第二层：

- 返回的不是 `Task` / `Task<T>`，而是 `UniTask` / `UniTask<T>`
- builder 不再生成标准 Task 风格的结果对象
- continuation 也不再天然站在标准 Task 世界里
- 真正承载状态机推进和结果完成的对象，变成了 `runnerPromise`

所以理解 UniTask builder 的正确角度不是：

`它有多复杂。`

而是：

`它到底负责把状态机接到哪条执行链上。`

## 第二层：builder 真正负责什么，为什么它自己反而很薄

如果你第一次打开 [AsyncUniTaskMethodBuilder.cs](/E:/NHT/workspace/UniTask-master/src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/AsyncUniTaskMethodBuilder.cs)，很容易产生一个反应：

`怎么这么短？`

这个反应是对的。builder 确实很薄。

以非泛型版本为例，它真正持有的状态很少：

- 一个 `runnerPromise`
- 一个异常字段 `ex`

泛型版本多一个结果字段 `result`。

这立刻说明一件事：

`builder 不是执行引擎，它更像编译器和 UniTask 运行时之间的转接头。`

### 1. `Create` 近乎什么都不做

builder 的 `Create()` 直接返回 `default`。

这意味着：

- 创建 `async UniTask` 方法本身，不会立刻构造一个重量级任务对象
- 只有当方法真的进入异步挂起路径时，才需要进一步把状态机挂到 runnerPromise 上

这个设计非常关键，因为大量 async 方法其实会走同步完成路径。

例如：

- 参数检查失败，直接抛异常
- 缓存命中，直接返回
- 某个分支没有真正等待任何异步点

如果每次调用 `async UniTask` 都先分配一套完整 promise/runner，再决定是不是用到它，这套模型就失去意义了。

### 2. `Task` 属性只是“把当前 builder 状态翻译成 UniTask”

builder 的 `Task` 属性逻辑非常直接：

- 如果已经有 `runnerPromise`，就返回 `runnerPromise.Task`
- 如果还没有 `runnerPromise`，但已经有异常，就返回 `UniTask.FromException(...)`
- 如果什么都没有，说明同步完成，就返回 `CompletedTask` 或 `FromResult(result)`

这段逻辑的意义很大。

它说明 `async UniTask` 的外部返回值，不一定总是背后挂着一个活跃中的状态机 runner。

更准确地说，存在三种不同路径：

- **同步成功完成**：直接把结果折叠成已完成的 `UniTask`
- **同步失败完成**：直接折叠成异常 `UniTask`
- **真正异步挂起**：才需要 runnerPromise 继续托管

这也是 UniTask 能把“快路径”和“慢路径”分开的核心原因之一。

### 3. `Start` 只是推进第一次 `MoveNext`

builder 的 `Start<TStateMachine>` 也非常薄：

```csharp
stateMachine.MoveNext();
```

这件事非常值得单独强调，因为它会修正很多人对 builder 的想象。

builder 并没有一个“主调度循环”。

它没有：

- 自己解释状态机
- 自己维护多段 continuation 列表
- 自己决定每一步落在哪个 Unity loop

它做的第一件事实质上只是：

`让编译器生成的状态机先跑第一步。`

也就是进入第一次 `MoveNext()`。

至于之后是不是会挂起、挂起后怎么恢复、恢复时由谁继续调用下一次 `MoveNext()`，都在后面。

### 4. `SetResult` / `SetException` 是完成信号，不是执行器

builder 的 `SetResult`、`SetException` 也很薄：

- 如果还没有 runnerPromise，就把结果/异常留在 builder 自己这里
- 如果 runnerPromise 已存在，就把完成信号转发给 runnerPromise

这进一步说明 builder 的定位：

`它只负责在“同步完成”和“已经进入异步链”之间做分流。`

真正的完成协议、await 消费、continuation 唤醒，都不在 builder 里。

这些东西都被推到了 runnerPromise 后面的 source 层去做。

## 第三层：状态机第一次遇到 await 时，真正的接线才开始发生

前面说 builder 很薄，容易让人误以为执行链也很简单。

其实不是。

真正关键的地方在 `AwaitOnCompleted` / `AwaitUnsafeOnCompleted`。

因为这两个方法才是：

`编译器在 await 点把“当前状态机的下一步”挂出去的地方。`

### 1. 第一次遇到 await 时，builder 才会创建 runnerPromise

在 [AsyncUniTaskMethodBuilder.cs](/E:/NHT/workspace/UniTask-master/src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/AsyncUniTaskMethodBuilder.cs) 里，无论泛型还是非泛型版本，逻辑都很清楚：

- 如果 `runnerPromise == null`
- 就调用 `AsyncUniTask<TStateMachine>.SetStateMachine(...)` 或泛型对应版本
- 然后把 awaiter 的完成回调接到 `runnerPromise.MoveNext`

这段逻辑的分量，比文件长度看起来大得多。

它实际表达的是：

`编译器生成的状态机，第一次真的要跨 await 挂出去时，UniTask 才把这个状态机交给一个专门的 runner/promise 容器继续托管。`

这里有两个重点。

第一，**不是方法一开始就有 runnerPromise**。

第二，**runnerPromise 不是纯 promise，它同时知道怎么继续推进状态机**。

这就是“runnerPromise”这个名字比一般 `TaskCompletionSource` 更准确的原因。

### 2. continuation 挂的不是“下一段 lambda”，而是 `MoveNext`

builder 调用的是：

- `awaiter.OnCompleted(runnerPromise.MoveNext)`
- 或 `awaiter.UnsafeOnCompleted(runnerPromise.MoveNext)`

也就是说，await 完成后，被重新调起的不是一段匿名 continuation 逻辑，而是：

`状态机的下一次 MoveNext。`

这件事把 async 的本质暴露得很清楚：

- await 不是“切到另一个函数”
- await 也不是“把后半段代码复制出来”
- await 更像是“等条件满足后，再次驱动同一个状态机往前走一步”

而 UniTask 在这里做的，就是把“谁来调用下一次 `MoveNext`”这件事，交给自己的 runnerPromise 链条。

### 3. `AwaitUnsafeOnCompleted` 很重要，因为恢复语义不只是“能不能继续”

在实际运行中，更多路径会走 `AwaitUnsafeOnCompleted`。

这不是细枝末节。

它说明 UniTask builder 也仍然在遵守 .NET async 的 awaiter 协议：

- awaiter 决定完成后如何注册 continuation
- builder 负责把 continuation 挂进去
- 状态机恢复仍然沿着 `MoveNext` 继续推进

所以 UniTask 并没有跳出 async 生态。

它做的是：

`在兼容 C# async 协议的前提下，把 continuation 的承载对象换成了自己的 runnerPromise。`

## 第四层：runnerPromise 到底是什么，为什么它同时像 runner 又像 promise

只看 builder 还不够，因为 builder 只是把状态机和 awaiter 接起来。

真正把“继续推进状态机”和“对外暴露 UniTask 结果”绑在一起的，是 runnerPromise。

这部分主要落在 [StateMachineRunner.cs](/E:/NHT/workspace/UniTask-master/src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/StateMachineRunner.cs)。

### 1. 从接口命名就能看出它的双重职责

`IStateMachineRunnerPromise` 同时具备几类能力：

- 有 `MoveNext`
- 能暴露 `Task`
- 能 `SetResult`
- 能 `SetException`
- 同时自己还是一个 `IUniTaskSource`

这说明它不是单纯的“下一步执行器”，也不是单纯的“完成结果盒子”。

它是把两件事合在了一起：

- **runner**：保存状态机实例，并在恢复时调用 `stateMachine.MoveNext()`
- **promise/source**：把完成状态、异常、await continuation 协议统一对外暴露

这也是 UniTask 这一层比标准 “builder + TaskCompletionSource” 心智模型更紧凑的原因。

### 2. `SetStateMachine` 做的不是绑定引用，而是接管状态机副本

在 `AsyncUniTask<TStateMachine>.SetStateMachine(...)` 里，可以看到一个很关键的顺序：

- 先从对象池取一个 runner 实例
- 再把这个 runner 放进 builder 的 `runnerPromise` 字段
- 最后把当前状态机拷贝到 runner 内部

源码注释里甚至直接强调了顺序问题：先设置 runner，再拷贝状态机。

这背后是在处理 async 状态机通常是 `struct` 的现实。

这不是小技巧，而是执行正确性的核心部分：

- 编译器生成的状态机常常是值类型
- 一旦要跨 await 挂起，就必须有一个稳定的宿主把这份状态保存下来
- 后续每次恢复都必须打到同一份状态机实例上，而不是打到某个短命副本上

所以 `runnerPromise` 的第一项工作其实是：

`成为状态机的稳定宿主。`

### 3. `MoveNext` 只是把推进权重新交还给状态机

在 `AsyncUniTask<TStateMachine>` 里，`MoveNext` 最终指向的是 `Run()`，而 `Run()` 做的事情也非常直接：

```csharp
stateMachine.MoveNext();
```

这再次印证了本文的主线：

`真正执行业务逻辑的仍然是编译器生成的状态机。`

UniTask runnerPromise 不是解释器。

它做的是：

- 保存状态机
- 在 awaiter 完成时触发恢复
- 把完成信号写进 source core
- 在结果被消费完后回收自己

所以当我们说“UniTask 有自己的运行时”时，准确含义不是它重写了 async 语言本身，而是：

`它重写了状态机被托管、被恢复、被完成、被回收的那一层。`

## 第五层：为什么说 builder 很薄，重活其实在 runnerPromise 后面的 source core

只看 `runnerPromise` 仍然还不够。

因为 runnerPromise 内部继续把真正的完成协议委托给了 `UniTaskCompletionSourceCore<T>`。

这就是为什么前一篇在讲 UniTask 最小内核时，必须一直强调：

`UniTask 不是一个胖对象，它更像 source 协议的轻量句柄。`

### 1. `runnerPromise.Task` 返回的是 `UniTask(this, core.Version)`

在 `AsyncUniTask<TStateMachine>` 里，`Task` 属性最终返回的是：

- 一个以当前 runnerPromise 为 source 的 `UniTask`
- 再带上当前 `core.Version`

这件事很关键，因为它说明对外暴露出去的 `UniTask` 只是一个轻量入口。

真正的状态在：

- runnerPromise 持有的状态机
- `UniTaskCompletionSourceCore<T>` 持有的完成状态、continuation 和版本信息

所以从外面看似乎只是“拿到一个 UniTask 返回值”，但在内部其实已经建立起这条关系：

`调用者手里的 UniTask -> runnerPromise(source) -> core(完成协议) -> 状态机恢复路径`

### 2. 结果完成并不等于对象生命周期结束

在 `SetResult` / `SetException` 里，runnerPromise 最终是把完成状态写进 `core`。

但 runnerPromise 自己不会在“刚完成”那一刻立刻消失。

原因很简单：

- 调用方可能还没 `await` 到结果
- `GetResult` 还没发生
- source 还需要保留状态给消费方读取

这跟很多人脑子里的“完成即销毁”不同。

在 UniTask 这里，更准确的顺序是：

1. 状态机跑完，runnerPromise 标记完成
2. 外部 awaiter 观察到完成
3. 调用 `GetResult`
4. 最后才进入回收/归还对象池逻辑

这一点会直接影响后面理解单次消费、version 校验和错误重入问题。

### 3. 回收时机被精确放在 `GetResult` 之后

从 [StateMachineRunner.cs](/E:/NHT/workspace/UniTask-master/src/UniTask/Assets/Plugins/UniTask/Runtime/CompilerServices/StateMachineRunner.cs) 可以看到，`GetResult` 里在 `core.GetResult(token)` 之后，会进入 `TryReturn()` 或 IL2CPP 特殊路径的延迟归还。

这件事说明：

`runnerPromise 是可复用对象池节点，而不是一次性垃圾对象。`

这也是 builder 本身能保持轻量的重要前提之一。

但它也带来一个后果：

`状态机 runner、completion source、token/version 校验，这几层必须形成严格协议，否则重用立刻会出错。`

这正是后面 04 那篇要详细展开的边界，所以这里先点到为止。

## 第六层：把整条执行链串起来，看一个 async UniTask 真正经历了什么

到这里可以把整个执行过程压成一条最小链路。

假设我们有这样的方法：

```csharp
public async UniTask<int> LoadAsync()
{
    await UniTask.Yield();
    return 42;
}
```

它在运行时的大致链路是这样的：

1. 调用方法时，编译器生成的状态机被创建，builder 也被初始化为默认值。
2. builder 的 `Start` 被调用，状态机第一次执行 `MoveNext()`。
3. 方法跑到 `await UniTask.Yield()`，发现当前 awaiter 还不能立刻给结果，于是需要挂起。
4. builder 的 `AwaitUnsafeOnCompleted` 被调用。
5. 如果这是第一次挂起，builder 创建并绑定 `runnerPromise`，把当前状态机副本交给它托管。
6. builder 把 awaiter 完成后的 continuation 注册成 `runnerPromise.MoveNext`。
7. 当前方法返回给调用者一个 `UniTask<int>`；如果此时还没完成，这个返回值背后就指向 `runnerPromise`。
8. 等到 `Yield` 对应的完成时机到了，awaiter 调用 `runnerPromise.MoveNext`。
9. runnerPromise 再次调用状态机的 `MoveNext()`，方法从 await 之后继续执行。
10. 方法 `return 42`，builder 或 runnerPromise 把结果写入 completion source core。
11. 外部 await 调用 `GetResult()` 取到结果。
12. runnerPromise 在结果被消费后归还对象池，等待下次复用。

这条链里最该记住的不是每一步，而是三个转折点：

- **第一次 `MoveNext`**：builder 只是点火
- **第一次真实挂起**：状态机被交给 runnerPromise 托管
- **最终 `GetResult`**：结果消费完成后，runnerPromise 才回收

一旦把这三个点抓住，builder/state machine/runnerPromise 的分工就不会再混。

## 第七层：这套设计到底意味着什么，为什么它会影响后面的 AOT / 崩溃讨论

如果只把这篇看成“源码阅读”，意义会偏小。

更重要的是看清这套设计对工程后果意味着什么。

### 1. `async UniTask` 的问题，很多都不是“await 语法问题”

当项目里出现：

- 某些 async 方法在线上真机才崩
- 某些 await 链在 IL2CPP 下行为异常
- 某些结果对象重复消费后出奇怪错误
- 某些 continuation 看起来恢复了，但状态不对

第一反应不应该只盯着：

- 这行 `await` 写得对不对
- 这个 API 名字用没用错

更应该意识到：

`很多问题发生在状态机托管层、runnerPromise 池化层、source 协议层，而不是语法表层。`

### 2. 泛型状态机类型本身就是运行时参与者

注意 `AsyncUniTask<TStateMachine>`、`AsyncUniTask<TStateMachine, T>` 这种类型形态。

这意味着：

- 状态机类型参数直接进入了运行时对象的泛型实例化
- builder、runner、source 协议不是纯抽象文字，它们会落成具体泛型代码

这也是为什么后面一旦谈到：

- IL2CPP 泛型实例化
- AOT 缺元数据
- HybridCLR 补元数据
- async 链只在某些具体泛型形态下出错

讨论就不可能只停留在“await 本身是什么”。

因为 async UniTask 的执行链，从一开始就是：

`编译器状态机 + 泛型 runnerPromise + source core`

这条链天然会和 AOT 世界发生相互作用。

### 3. builder 越薄，越说明不要把责任放错地方

很多文章在讲 async/await 时，容易把 builder 写成“核心大脑”。

但对 UniTask 而言，builder 恰恰不是大脑。

它更像一个非常薄的门面：

- 把编译器状态机点火
- 在第一次挂起时绑定 runnerPromise
- 把完成信号分流到快路径或慢路径

真正的大脑分散在更后面的层：

- 状态机本身决定业务控制流
- awaiter 决定什么时候恢复
- runnerPromise 决定由谁来恢复
- completion source core 决定结果如何完成、如何消费、如何校验

这会直接影响后面阅读源码的姿势：

`看到 builder 很短，不要以为 UniTask 的 async 机制很简单；看到 builder 很短，应该意识到重活被刻意后移到了更适合池化与协议复用的层里。`

## 第八层：这一篇的边界到哪里为止

为了避免和后面文章打架，最后把边界收一下。

这篇文章只解决一个问题：

`async UniTask 方法是如何从“编译器状态机”接到“UniTask 运行时链路”上的。`

所以我们刻意没有深挖下面几块：

- 没有系统展开 `PlayerLoop` 如何决定 continuation 的恢复时机
- 没有展开 `UniTaskCompletionSourceCore<T>` 的 version / token / 单次消费协议
- 没有细讲 `Yield`、`NextFrame`、`Delay` 这些 awaiter 各自怎样安排恢复
- 没有展开 `Forget`、`UniTaskVoid`、未观察异常的派发出口

这些内容后面各有自己的篇章。

但到这里，至少有一条主线已经必须成立：

`async UniTask 不是“逃离 C# async”，而是“把 C# async 生成的状态机，接入 UniTask 自己的返回协议与恢复协议”。`

只要这条主线立住，后面再看 04、05、07，阅读成本会低很多。

## 结语

理解 `async UniTask` 的关键，不是背 builder API，也不是死记 `AwaitUnsafeOnCompleted` 的调用顺序。

真正关键的是建立一个准确的心智模型：

- C# 编译器仍然生成状态机
- builder 负责把这个状态机接到 UniTask 世界
- 第一次真实挂起时，runnerPromise 接管状态机生命周期
- await 完成后，恢复的本质仍然是再次调用 `MoveNext`
- 对外暴露出去的 `UniTask` 只是轻量句柄，真正的完成协议在 source core

往下读，就会进入这条链上最容易被低估的一层：

`为什么 UniTask 的结果对象带着 version / token，为什么很多值默认只允许单次消费。`

因为只要 runnerPromise 会池化、source 会复用，这个协议就不是“细节优化”，而是正确性边界。

