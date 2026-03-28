---
date: "2026-03-26"
title: "数据导向运行时 04｜调度怎么做：Burst/Jobs、Mass Processor、自己手搓执行图"
description: "把 Unity DOTS、Unreal Mass 和自研 ECS 放回同一条执行主链里，说明 system、processor、query、job、compiler 分别在解决什么问题，以及为什么数据连续还不够，最后还是会走到显式调度。"
slug: "data-oriented-runtime-04-how-scheduling-works-burst-jobs-mass-processor-and-self-built-execution-graph"
weight: 350
featured: false
tags:
  - Unity
  - Unreal Engine
  - DOTS
  - MassEntity
  - ECS
  - Architecture
series: "数据导向运行时"
---

> 这篇只回答一个问题：`为什么数据连续还不够，最后还是要走到显式调度。`
>
> 真正需要被解释的，不是“怎么开多线程”，而是当世界已经按 archetype + chunk 排好之后，处理逻辑到底应该按什么顺序、什么依赖、什么粒度去执行。

前两篇已经把两块底子立住了：

- [《数据导向运行时 02｜Archetype、Chunk、Fragment：性能到底建在什么地方》]({{< relref "engine-notes/data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from.md" >}})
  先把 `archetype + chunk + query cache` 这条存储主链立住了。
- [《数据导向运行时 03｜Structural Change、Command Buffer 与同步点：为什么改结构总是贵》]({{< relref "engine-notes/data-oriented-runtime-03-structural-change-command-buffer-and-sync-points-why-structural-mutations-are-expensive.md" >}})
  又把 structural change、command buffer 和受控提交时机讲清了。

到这里，接下来的问题就非常自然：

`即使布局对了、结构变化也被收束了，系统之间到底怎样协作，才能让这套运行时真的稳定运转？`

这就是这篇要回答的事。

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 为什么“有 chunk”不等于“有执行骨架”。
2. Unity 里的 `System`、`Job`、`Burst` 到底分别站哪层，为什么它们不是一回事。
3. Unreal 里的 `Processor`、`Query`、`ExecutionContext`、`ProcessingPhase` 到底怎样组成执行链。
4. 自研第一版为什么应该先做 `phase scheduler`，而不是一上来做复杂任务图。
5. 为什么显式调度不是优化附属品，而是数据导向运行时真正的执行骨架。

## 先给一句总判断

如果必须先把这篇压成一句话，我会这样写：

`数据导向系统的执行性能，不只建在“数据排得整齐”，还建在“谁在什么时候、以什么依赖、对哪些 chunk 做什么事”这套显式调度骨架上；Burst 和编译器只是在优化执行单元，不是在替你决定执行图。`

这句话里最重要的区分是：

- `布局`
  回答“数据怎样放”。

- `调度`
  回答“逻辑怎样跑”。

没有前者，后者会很难看。
但只有前者，没有后者，系统一样会乱。

## 证据地图

先说明这篇的证据边界。

当前这版首稿，能直接压实的事实主要来自下面几类官方资料：

- Unity `System groups`
- Unity `ISystem overview`
- Unity `IJobEntity`
- Unity `IJobChunk`
- Unity `Job system overview`
- Unity `Burst compilation`
- Unreal `UMassProcessor`
- Unreal `EMassProcessingPhase`
- Unreal `FMassProcessorExecutionOrder`
- Unreal `FMassEntityQuery::ForEachEntityChunk`
- Unreal `FMassExecutionContext`
- Unreal `UMassProcessor::SetExecutionPriority`

这里仍然有一个要明确写出来的限制：

- 当前本地还没有稳定可引用的 `com.unity.entities@1.x` 包源码路径
- `docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径也都还是 `TODO`

所以这篇里的 `事实` 只落在当前官方资料能直接支持的范围。

尤其 Unreal 这一侧，这篇当前先不展开 `Task Graph / UE::Tasks` 内部实现。
目前能直接压实的，是：

- `Processor` 如何归入 processing phase
- `Query` 如何命中 matching chunks
- `ExecutionOrder / ExecutionPriority / game-thread requirement` 如何构成显式执行约束

也就是说，这篇更关注“执行骨架怎么长”，而不是内部线程池怎么实现。

## 先把“调度”这个词拆开，不然一定会写乱

很多人一说调度，脑子里立刻就只剩一个词：`并行`。

这其实不够。

对数据导向运行时来说，调度至少包含 4 层：

1. `Phase / Group`
   回答“这批逻辑站在一帧里的什么位置”。

2. `System / Processor`
   回答“哪一个执行单元负责发起这段工作”。

3. `Query / Chunk iteration`
   回答“这段工作到底打到哪些数据块上”。

4. `Job / Task / Compiler`
   回答“这段工作以什么形式被执行，以及机器码长什么样”。

如果这 4 层不拆开，后面就会出现一堆常见误解：

- 把 `Burst` 写成 scheduler
- 把 `Processor` 写成 thread
- 把 `System` 写成“逻辑函数的别名”
- 把 query 命中 chunk 和 job 并行混成一件事

所以这篇最重要的一步，不是先讲多线程，而是先把这 4 层站位压开。

## 为什么数据连续还不够

先把最根的问题说透。

就算你已经有了：

- `archetype`
- `chunk`
- `query cache`

系统也不会自动变成“会自己跑的机器”。

因为布局只解决了下面这些问题：

- 哪些对象属于同一结构
- 数据怎样排才更适合批处理
- query 怎样命中 matching chunks

但它没回答下面这些问题：

- movement 和 collision-prepare 谁先谁后
- spawn 和 destroy 的 command buffer 何时 flush
- simulation 和 presentation 是否应该同 phase
- 哪些逻辑可以并行，哪些必须串行
- 哪些代码只能在主线程或 game thread 上跑
- 谁负责把 query 命中的 chunks 送进真正的执行函数

所以只要系统规模稍微一上来，显式调度就不是“锦上添花”。
它会变成运行时本体。

## Unity 这边，`System + Job + Burst` 是三层，不是一层

先看 Unity。

### 从资料里能直接看见什么

Unity `System groups` 和 `ISystem overview` 已经把几条关键事实写得很清楚：

- system group 会在主线程上按排序顺序更新自己的 children
- 默认 world 里有三层根系统组：`Initialization`、`Simulation`、`Presentation`
- `ISystem` 的系统事件都跑在主线程
- 最佳实践是在 `OnUpdate` 里调度 jobs，把主要工作丢给 job 执行

`IJobEntity` 和 `IJobChunk` 文档又把另一层关系补全了：

- `IJobEntity` 适合按组件数据迭代实体
- `IJobEntity` 实际会生成一个 `IJobChunk` job
- `IJobChunk` 的写法本身就要求你显式声明要访问哪些 component type handle，再在 chunk 上做迭代

再往 Unity `Job system` 和 `Burst` 的官方文档看，还能直接落下几条事实：

- job 是一个小的工作单元
- 只有主线程能 schedule 和 complete jobs
- job system 会通过 dependencies 保证 jobs 在正确顺序下执行
- job system 使用 worker threads，并把线程数量控制在和 CPU 核心能力匹配的范围
- job system 为了安全，会把 job 所需的 blittable 数据复制到 native memory
- Burst 是基于 LLVM 的 compiler，工作在 High-Performance C# 子集上
- Burst 最初就是为 Unity job system 设计的
- Burst 是编译链的补充，不会替代 Mono 或 IL2CPP

> Burst 通过 ILPP（IL Post Processing）在编译阶段注入字节码、标记编译入口，这是它和普通 C# 代码共存的机制。如果你想搞清楚这个注入过程，可以看：[Unity 脚本编译管线 02｜ILPP：Unity 为什么要偷偷改你的字节码]({{< relref "engine-notes/unity-script-compilation-pipeline-02-ilpp.md" >}})

把这些事实合起来，其实已经足够把 Unity 这条执行链拆清了。

### 工程判断：Unity 的执行骨架至少分成三层

基于上面这些事实，我认为对 Unity 最稳的判断是：

`System`、`Job`、`Burst` 在 Unity 里回答的是三件不同的事。

更具体一点：

- `System group / System`
  负责回答“这段工作什么时候发起、放在一帧里的哪一层、和别的系统谁先谁后”。

- `Job`
  负责回答“这段工作以什么工作单元形式被调度到 worker threads，以及依赖怎样接起来”。

- `Burst`
  负责回答“这段 Burst-compatible 代码怎样被编成更好的 native code”。

所以如果把三者压成一句更工程化的话，就是：

`System 管时序，Job 管执行单元和依赖，Burst 管代码生成。`

这也是为什么我会说：

`Burst 不是“自动优化器”，它更不是 scheduler；它只是在已有执行骨架上，让某些执行单元跑得更好。`

### 为什么 Unity 会让 `OnUpdate` 还留在主线程

这点其实很有代表性。

如果 Unity 的目标是“把所有逻辑都自动扔到并行里”，那官方文档就不会明确说：

- system 事件在主线程
- 最佳实践是在 `OnUpdate` 里 schedule jobs

这件事本身已经说明 Unity 的执行立场：

`调度本身仍然需要一个显式的主控层，而不是让每段逻辑都直接变成线程。`

所以 `OnUpdate` 的角色，首先不是“干活的人”。
它更像：

- 声明本 system 想处理哪类数据
- 决定这帧要 schedule 哪些 jobs
- 串接 dependencies
- 决定 structural change 是否通过 ECB 延迟提交

如果把这层也硬塞进 Burst job 里，系统只会变得更难控制。

## Unreal 这边，`Processor + Query + ExecutionContext + Phase` 才是执行骨架

再看 Unreal `Mass`。

### 从资料里能直接看见什么

Unreal 官方关于 `UMassProcessor`、`ProcessingPhase` 和执行顺序的 API，已经直接给出了一组很硬的事实：

- `UMassProcessor` 有 `ProcessingPhase`，表示这个 processor 会在所属 processing phase 中自动运行
- `EMassProcessingPhase` 直接把 phase 列成 `PrePhysics`、`StartPhysics`、`DuringPhysics`、`EndPhysics`、`PostPhysics`、`FrameEnd`
- `FMassProcessorExecutionOrder` 直接暴露了 `ExecuteAfter`、`ExecuteBefore`、`ExecuteInGroup`
- `SetExecutionPriority` 的说明里还直接提到：这个变更会在下一次 processing graph build 时生效
- `DoesRequireGameThreadExecution` 说明 processor 还可以声明自己是否必须在 game thread 执行

这组事实非常关键。

因为它们说明 Unreal `Mass` 的执行链不是“有个 processor，剩下就自动发生”。

它至少有下面几层显式约束：

- phase
- before/after / group
- priority
- game thread requirement

再看 query 和 execution context 这一侧，官方文档又给了另一组关键事实：

- `FMassEntityQuery::ForEachEntityChunk` 会在所有满足 requirements 的实体上运行 execute function
- `FMassEntityQuery::ForEachEntityChunk` 的另一种形式会先验证给定 archetype collection 是否满足 query requirements
- `FMassExecutionContext` 提供当前 chunk 的 fragment views、entity iterator、entity collection、delta time
- `FMassExecutionContext` 本身也暴露了 `Defer()` 和 `FlushDeferred()`

把这些事实合在一起，你会发现 Unreal 这条执行链也非常分层。

### 工程判断：Processor 不是工作线程，Processor 是执行图节点

基于上面这些事实，我认为对 Unreal `Mass` 最稳的判断是：

`Processor` 在 Mass 里最接近“执行图上的节点”，而不是“具体干活的线程”。

它回答的首先是：

- 这个逻辑属于哪一个 processing phase
- 它和别的 processors 谁前谁后
- 它是不是要求 game thread
- 它通过哪些 queries 表达数据要求

而真正落到数据层时，又不是 processor 自己“凭空处理世界”。

它还是要通过：

- query 命中 matching archetypes / chunks
- execution context 提供当前 chunk 视图
- chunk 级迭代把逻辑真正跑起来

所以如果把 Unreal 这条链压成一句话，我会这样写：

`Processor 管执行图位置，Query 管命中哪些 chunk，ExecutionContext 管当前 chunk 的运行视图。`

这也是为什么当前这篇即使不深挖 Task Graph 内部实现，也已经足够把执行骨架写清。

因为真正关键的约束，官方 API 已经直接暴露了。

## Unity 和 Unreal 在调度上的共同收敛

虽然名词完全不同，但两边在执行骨架上其实收敛得很明显。

如果先压成一张最小对照表，我会这样记：

| 对照维度 | Unity DOTS | Unreal Mass | 自研第一版 |
| --- | --- | --- | --- |
| 帧内站位 | `Initialization / Simulation / Presentation` system groups | `EMassProcessingPhase` | `PreSim / Sim / PostSim / Presentation` |
| 调度发起单元 | `System` / `OnUpdate` | `UMassProcessor` | `System` |
| 数据命中方式 | `EntityQuery` + `IJobEntity / IJobChunk` | `FMassEntityQuery::ForEachEntityChunk` | `Query` + chunk iteration |
| 顺序约束 | `UpdateInGroup / UpdateBefore / UpdateAfter` | `ProcessingPhase / ExecuteAfter / ExecuteBefore / ExecuteInGroup / priority` | `phase + explicit order` |
| 执行载体 | `Job` + `JobHandle` dependencies | `ExecuteFunction` + `ExecutionContext` + processing graph | 串行 phase，后续再加依赖图 |
| 代码生成 | `Burst` | C++ 编译链 | 第一版先不做 |

这张表最重要的地方，不是“它们完全等价”。

而是你会更容易看到一个稳定结论：

`数据导向运行时最后几乎都会长出一层显式执行骨架，用来把“什么时候跑”“跑谁”“怎么跑”拆开。`

## 为什么显式调度不是优化附属品

这点值得单独强调。

很多人会把显式调度理解成一种“规模大了才补上的优化器”。

我反而觉得，更准确的说法是：

`当你已经不再按对象消息分发和脚本回调来组织世界时，显式调度会自然变成默认执行模型。`

原因很简单。

对象世界里，很多执行顺序是隐式长出来的：

- Update 回调顺序
- 对象引用链
- 生命周期回调
- 场景对象天然持有关系

但到了数据导向运行时里，这些隐式结构被削弱了。

你剩下的是：

- 一批 query
- 一批 chunk
- 一批 systems / processors
- 一批 read/write 关系
- 一批 structural change flush points

这时候如果没有显式调度，你就很难回答：

- 哪些逻辑先跑
- 哪些逻辑可并行
- 哪些逻辑必须等 command buffer flush
- 哪些逻辑属于 simulation，哪些属于 presentation

所以显式调度不是“后加插件”。
它是从对象回调世界切到批处理世界后，自然长出来的主骨架。

## 如果自己手搓，第一版应该怎么做执行图

到这里，自研第一版的策略其实已经很清楚了。

### 1. 第一版先做 `phase scheduler`，不要先做任务图

这件事在 `docs/self-built-ecs-implementation-plan.md` 里也已经定了：

- `PreSim`
- `Sim`
- `PostSim`
- `Presentation`

为什么先 phase，而不是直接搞依赖图？

因为第一版最需要证明的，不是“我能不能画出漂亮 DAG”。
而是：

`这套系统有没有一条可理解、可验证、可调试的默认执行顺序。`

没有这个，后面一加并行和优先级，系统只会更快失控。

### 2. system 元信息至少要声明 4 件事

我建议第一版的 system 至少显式声明：

- `phase`
- `query`
- `read set`
- `write set`

如果能再多一项，会很值钱：

- `does_structural_change`

这不是为了“更像专业框架”。
而是为了让你后面能回答下面这些问题：

- 这个 system 读谁写谁
- 它为什么必须在另一个 system 之后
- 它为什么不能和某个 system 并行
- 它是不是需要 command buffer flush

### 3. 第一版 phase 内先串行，别急着并行

这是整个实现计划里最重要的克制之一。

第一版 phase 内串行的价值很大：

- 更容易证明 query 和 chunk 迭代是对的
- 更容易看清 structural change 的 flush 时机
- 更容易暴露真正的读写冲突
- 更容易先把调试工具和统计做起来

如果这一层都还没站住，就上任务图和并行调度，问题只会更难定位。

### 4. 第一版别试图自己做一个 Burst

这点也要写死。

Burst 很重要，但它解决的是 codegen。

对自研第一版来说，真正的难点不是：

- 指令能不能更 SIMD
- LLVM 能不能帮你做更多优化

而是：

- 系统有没有正确 phase
- query 是否正确命中 chunk
- structural change flush 是否安全
- read/write 边界是否能被显式描述

这些没站住之前，编译器级优化都不是主问题。

## 常见误解

### 误解一：Burst 就是 Unity 的 scheduler

不是。

Burst 是 compiler。
它解决的是代码生成，不是执行顺序。

### 误解二：有了 query 和 chunk，系统就会自然跑得很顺

不会。

query 和 chunk 只保证你“更容易批处理正确的数据”。
它们不会自动回答：

- 谁先谁后
- 谁可并行
- 谁必须等 flush

### 误解三：Processor / System 就等于一个线程任务

不对。

Processor 和 System 更接近执行图节点或调度发起单元。
真正落到具体 chunk 工作时，执行粒度和线程粒度未必和它们一一对应。

### 误解四：第一版应该先做并行，不然就不像现代系统

也不对。

第一版更重要的是把 phase、query、flush 时机、read/write 边界站住。
这些没站住，并行只会把问题藏起来。

## 我的结论

如果一定要把这篇压成一句话，我会这样写：

> 数据导向运行时的执行骨架，真正要拆开的是 `什么时候跑`、`谁发起工作`、`命中哪些 chunk`、`以什么执行单元和什么机器码跑` 这几层；Unity 用 `System group + System + Job + Burst` 把它们拆开，Unreal 用 `ProcessingPhase + Processor + Query + ExecutionContext` 把它们拆开，自研第一版则应该先用 `phase scheduler + query + 串行 chunk iteration` 把这条骨架站住。

顺着这条线继续往下走，下一篇最自然就该讲：

`为什么这些系统越来越喜欢把运行时代价前移到构建期。`
