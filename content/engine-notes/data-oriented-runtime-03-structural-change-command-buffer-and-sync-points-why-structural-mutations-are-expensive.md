---
date: "2026-03-26"
title: "数据导向运行时 03｜Structural Change、Command Buffer 与同步点：为什么改结构总是贵"
description: "把 Unity DOTS、Unreal Mass 和自研 ECS 放回同一条结构变化主链里，说明 structural change 真正贵在哪里，command buffer 在解决什么，以及为什么同步点只是代价的一种表现。"
slug: "data-oriented-runtime-03-structural-change-command-buffer-and-sync-points-why-structural-mutations-are-expensive"
weight: 340
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

> 这篇只回答一个问题：`为什么一旦你真的按 archetype + chunk 把世界存起来，结构变化就几乎一定会变贵。`
>
> 真正难的不是“给实体加个组件”这句话，而是这句话在数据导向运行时里，已经不再等价于“给一个对象多写一个字段”。

上一篇
[《数据导向运行时 02｜Archetype、Chunk、Fragment：性能到底建在什么地方》]({{< relref "engine-notes/data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from.md" >}})
已经把 `archetype + chunk + query cache` 这条存储主链立住了。

那接下来的问题就很自然：

`既然这套布局能让查询和批处理变便宜，为什么给实体加减组件、切状态、创建销毁实体，又会突然变得很贵？`

这篇就是专门回答这个问题。

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 什么才算 `structural change`，它和普通“写组件值”到底差在哪里。
2. 为什么结构变化一发生，系统付出的不只是几次内存写入，而是整条数据组织链一起受影响。
3. Unity 为什么会把它和 `sync point` 紧紧绑在一起。
4. Unreal 为什么大量实体操作要走 `command buffer / Defer`。
5. 如果自己手搓第一版，为什么必须把 deferred structural change 当作内核能力，而不是后加优化。

## 先给一句总判断

如果必须先把这篇压成一句话，我会这样写：

`结构变化之所以贵，不是因为“改组件”这个动作本身有多复杂，而是因为它会改写实体所属的结构分区，进而触发 archetype 迁移、chunk 重排、location 更新、query 命中变化，以及执行链上的同步或延迟提交。`

这句话里最重要的一个区分是：

- `写值`
  主要是在既有布局里改数据。

- `改结构`
  主要是在改“这个实体属于哪一种布局”。

两者不是一回事。

## 证据地图

先说明这篇的证据边界。

当前这版首稿，能直接压实的事实主要来自下面几类官方资料：

- Unity `Entity command buffer overview`
- Unity `Enableable components overview`
- Unity `SetEnabled`
- Unity 旧版 `Sync points` 说明页
- Unreal `FMassEntityManager`
- Unreal `FMassCommandBuffer`
- Unreal `FMassEntityManager::Defer`
- Unreal `FMassCommandBuffer::Flush`
- Unreal `FMassProcessingContext::bFlushCommandBuffer`

这里有一个需要明确写出来的限制：

- 当前本地还没有稳定可引用的 `com.unity.entities@1.x` 包源码路径
- `docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径也都还是 `TODO`

所以这篇里的 `事实` 只落在当前官方资料明确写出来的范围。

其中 Unity 关于 `sync point` 和“结构变化会使直接引用失效”的最直接说明，我目前能稳定定位到的是较早版本文档。
我这里的使用方式是：

- 把它当作 Unity 官方对这一机制的明确解释
- 再结合 `Entities 1.x` 里的 `ECB`、`enableable component`、`SetEnabled` 文档，确认这条成本模型仍然成立

这部分属于带版本边界的引用，我会在正文里保持这个边界，不把它伪装成“当前源码已验证”。

## 先把最关键的区分立住：写值，不等于改结构

这篇最容易混掉的地方，就是把下面两件事写成同一件事：

- `SetComponent<T>(e, value)`
- `AddComponent<T>(e)` / `RemoveComponent<T>(e)`

在对象世界里，它们看起来都像“修改对象状态”。

但在数据导向世界里，它们落到的层根本不同。

### 写值，通常只是在已有布局里改一列数据

如果实体已经在某个 archetype 和 chunk 里站好了位置，那么：

- 改 `Position`
- 改 `Velocity`
- 改 `Health`

这类动作通常只是：

- 找到当前 chunk
- 找到对应列
- 在当前行写一个新值

它当然也可能有并发依赖、缓存行为和访问模式问题。
但它通常不要求这个实体换桶。

### 改结构，通常是在改“这个实体属于哪一种布局”

一旦你做的是：

- 创建实体
- 销毁实体
- 添加组件
- 移除组件
- 改变会影响分桶的 shared data
- 切换会通过加减组件实现的状态

事情就不再是“改这一行某一列的值”。

它变成了：

`这个实体当前所属的结构签名变了，它可能已经不属于原 archetype 了。`

而这件事一旦成立，后面整条链都会被牵动。

## Unity 这边，官方直接把 structural change 和 sync point 绑在一起

先看 Unity。

### 从资料里能直接看见什么

Unity 官方旧版 `Sync points` 说明里，直接给了几条非常硬的事实：

- `sync point` 会等待此前已调度的 jobs 完成
- structural changes 是 sync point 的主要来源
- 结构变化包括创建实体、删除实体、给实体添加组件、移除组件、改变 shared component 值
- 更广义地说，任何会改变实体 archetype，或导致 chunk 内实体顺序变化的操作，都属于 structural change
- 这类变化只能在主线程执行
- 结构变化不只会带来 sync point，还会使直接组件引用失效

这几条事实其实已经把成本模型讲得很明白了。

再往 `Entities 1.x` 看，当前官方文档又给了两组互相呼应的事实：

- `EntityCommandBuffer` 用来把 structural changes 记录下来，等 jobs 完成后再在主线程回放
- `IEnableableComponent` 和 `SetComponentEnabled` 明确强调：启用/禁用单个组件不需要 structural change
- 与之相对，`EntityManager.SetEnabled(Entity,bool)` 明确写着：这是通过添加或移除 `Disabled` 组件完成的，因此它需要 structural change

换句话说，Unity 现在给你的不是一个“多了个工具”的说法。

它给的是一整套更完整的区分：

- 哪些变化只是值变化
- 哪些变化会改 archetype
- 哪些高频状态更适合走 enable bit，而不是 add/remove component

### 工程判断

基于这些事实，对 Unity 更稳的判断是：

`sync point 不是结构变化“额外附赠的不幸”，而是这套数据布局在并发执行模型下的自然代价表现。`

为什么？

因为只要系统里已经有 jobs 正在读写：

- 你就不能随便把实体从一个 archetype 搬到另一个 archetype
- 也不能随便改变 chunk 内实体顺序
- 更不能假装之前借出去的直接引用还会继续有效

所以 Unity 才会把这些事压成一个硬边界：

`需要改结构的事，不能像普通值写入那样随时发生。`

这也是为什么 `EntityCommandBuffer` 在 Unity 里不是“方便工具”，而是数据导向运行时和 jobs 共存时的核心缓冲层。

## Unreal 这边，没有叫 sync point，但同样把多数实体操作压进 command buffer

再看 Unreal `Mass`。

### 从资料里能直接看见什么

`FMassEntityManager` 的 API 描述已经把几个关键事实直接写出来了：

- 它负责承载 entities 并管理 archetypes
- 实体以 `chunked array` 形式存储
- 每个有效实体会被分配到当前 fragments 组合对应的 archetype
- 虽然存在同步操作接口，但多数实体操作在大多数情况下通过 command buffer 完成
- 默认 command buffer 可以通过 `Defer()` 取得

`FMassCommandBuffer` 和 `Flush` 相关 API 又补上了另一组事实：

- command buffer 本身就是一组待执行命令的容器
- 它直接支持 `AddFragment`、`RemoveFragment`、`AddTag`、`RemoveTag`、`DestroyEntity` 这类明显会改结构的操作
- `Flush(EntityManager)` 的语义就是执行所有累积命令
- `MassProcessingContext` 还明确有 `bFlushCommandBuffer` 开关，用来控制执行函数结束时是否 flush commands

把这些事实合在一起，你已经不太需要再猜 Unreal 的立场了。

它其实说得很直白：

`在 Mass 这套 archetype/chunked storage 运行时里，实体结构操作的常规路径就是 defer，再 flush。`

### 工程判断

这里最重要的判断是：

`Unreal 没有用 Unity 那套“sync point”术语，并不代表它没有同一类结构成本；它只是把代价更多表达成“默认走 deferred command path，而不是随时原地改世界”。`

也就是说，两边真正相同的不是名词，而是下面这个约束：

`当实体已经被分配到 archetype 和 chunk 后，改结构就不该再被当成普通字段写入。`

Unity 更强调：

- jobs 会被同步点卡住
- 直接引用会失效

Unreal 更强调：

- 默认拿 command buffer
- 大多数实体操作延迟执行
- flush 才是结构变化真正落地的时机
- 这个时机本身就是执行上下文里显式控制的一部分

表达方式不同，但它们在结构上其实指向同一件事：

`结构变化必须被收束到受控时机。`

## Structural change 真正贵在哪里

到这里，才能回答这篇真正的核心问题。

结构变化真正贵，并不是因为“引擎故意把接口设计得麻烦”。

它贵，是因为它会同时牵动下面几层东西。

### 1. archetype 可能要变

只要组件集合变了，实体当前的结构签名就可能变。

这意味着：

- 原 archetype 不再正确
- 目标 archetype 需要被定位，甚至可能新建

这一步已经不是“改值”，而是“改结构归属”。

### 2. chunk 里的位置可能要变

一旦 archetype 变了，实体就可能要：

- 从原 chunk 挪出去
- 插入目标 archetype 的某个 chunk
- 触发原 chunk 补洞
- 触发目标 chunk 增加新行

即使 archetype 不变，只要你的操作会影响 chunk 内实体顺序或 chunk 级数据组织，它也已经不是普通写入。

### 3. location 表要更新

只要实体换了 archetype / chunk / row：

- `EntityId -> EntityLocation` 的映射就要改
- 被补洞搬动的那个实体 location 也要跟着修

这一层如果没有维护好，后面任何 `GetComponent`、`DestroyEntity`、`AddComponent` 都会慢慢坏掉。

### 4. query 命中可能要变

一个实体结构变了，受影响的不只是它自己。

还包括：

- 它原来命中的 query 可能不再命中
- 它新的 archetype 可能开始命中另一批 query
- 某些按 archetype 缓存的结果需要更新

所以 structural change 的成本，不是一个实体孤立承担的。
它会外溢到系统查询层。

### 5. 执行时机必须受控

如果这时还有 jobs、processors 或 systems 正在处理这些 chunks：

- 你就不能当作世界是静止的
- 也不能假设借出去的 view、lookup、buffer 还都安全

所以它最后一定会逼出某种“受控提交时机”：

- Unity 这边叫 `sync point` + `ECB playback`
- Unreal 这边更多体现为 `Defer` + `Flush`
- 自研这边则应该明确成 `deferred structural change`

这才是结构变化真正贵的地方：

`它改的不是某个值，而是世界当前这张结构地图本身。`

## Command buffer 解决的不是“让变化免费”，而是“把代价收束”

这里最容易出现的误解是：

`有了 command buffer，结构变化就不贵了。`

不是。

### 从资料里能直接看见什么

Unity `ECB` 文档明确说，它可以：

- 记录 structural changes
- 在 jobs 完成后再到主线程执行
- 把原本散在一帧中的多次结构变化，尽量收束到清晰的回放时机

Unreal 这边也一样：

- `FMassEntityManager` 说默认 command buffer 通过 `Defer()` 获得
- `FMassCommandBuffer::Flush` 的语义是执行累计命令

这两边都没有说“结构变化消失了”。

它们真正做的是：

- 先记录
- 再集中执行
- 避免在系统运行过程中任意打断世界结构

### 工程判断

所以 command buffer 真正解决的是：

`把高成本、会改世界拓扑的操作，从任意时刻发生，收束成少数几个可控提交点。`

这有三个直接好处：

1. 更容易维持执行期的一致性
2. 更容易把很多小打断合并成少数几次大提交
3. 更容易让 jobs / processors / systems 明确知道什么时候世界是稳定的

但它并没有改变 structural change 的本质。

回放发生时，该搬的数据还是要搬。
该改的 archetype、chunk、location、query 命中还是要改。

所以 command buffer 不是“免费通道”。
它更像是“成本管理层”。

## 为什么 Unity 会专门发展出 enableable component 这条路

这件事很能说明问题。

如果 add/remove component 真的像改一个 bool 一样便宜，Unity 就没必要特地给 `IEnableableComponent` 单开一条路。

但现在官方文档明确告诉你：

- 高频且不可预测的状态变化，适合用 enableable component
- 它不会造成 structural change
- 它还能减少 unique archetypes 数量，改善 chunk 使用

这说明什么？

说明 Unity 对这套成本模型的官方答案已经非常明确了：

`低频、持久状态变化，可以接受 structural change；高频状态切换，最好别老去改 archetype。`

这个判断对自研也非常值钱。

因为它提醒你：

- 不是所有“状态变化”都该实现成 add/remove tag
- 有些状态应该只是“同 archetype 内的启用位”
- 否则你会把高频状态切换硬生生变成高频结构迁移

## 如果自己手搓，第一版必须怎么做

到这里，自研第一版的约束其实已经很清楚了。

### 1. 先把“写值”和“改结构”分成两条 API 路

绝对不要把下面两类动作揉成同一种内部路径：

- `SetComponent`
- `AddComponent / RemoveComponent / DestroyEntity / CreateEntity`

前者应尽量留在当前布局里。
后者默认就该进入 deferred structural change。

### 2. 一定要有 command list / command buffer

名字可以不是 `ECB`，也可以不是 `FMassCommandBuffer`。

但结构必须有：

- `Create`
- `Destroy`
- `AddComponent`
- `RemoveComponent`

这些操作先记录。
到安全时机再统一 flush。

### 3. flush 时必须做完整迁移，不要半吊子

flush 至少要负责：

- 找目标 archetype
- 申请目标 chunk 行位
- 搬组件数据
- 修原 chunk 补洞
- 更新 location
- 刷新 query 相关缓存

如果 flush 只做“把命令执行了”，却不把这些不变量都守住，系统迟早会烂。

### 4. 第一版就该预留“高频状态不要总改 archetype”的手段

不一定第一版就完整实现 Unity 那种 enableable component。

但至少要在设计上承认：

`有些状态切换不该被表达成高频 add/remove component。`

否则你做出第一版之后，很快就会在行为状态、LOD 状态、激活状态这些地方反复踩坑。

## 常见误解

### 误解一：structural change 贵，主要是因为要分配内存

不够准确。

内存分配当然可能是成本的一部分。
但真正更核心的是：

- archetype 归属变了
- chunk 布局动了
- location 变了
- query 命中可能变了
- 执行时机也要被收束

### 误解二：有了 command buffer，这个问题就解决了

没有。

command buffer 解决的是“什么时候做”和“如何集中做”。
它不是在消灭结构变化本身。

### 误解三：tag 没有 payload，所以加减它很便宜

也不对。

只要 tag 参与 archetype 签名，它的加减就是结构变化。
即使它不怎么占每实体字节，也一样会影响分桶、chunk 分布和 query 命中。

### 误解四：状态切换就应该用 add/remove component 表达

不总是。

如果状态变化高频、不可预测，而且本质上更像启用位，那么更合理的方向通常是：

- Unity 那边用 `IEnableableComponent`
- 自研里用不改 archetype 的 enable bit 或类似机制

## 我的结论

如果一定要把这篇压成一句话，我会这样写：

> structural change 之所以贵，不是因为它“比写值多几步”，而是因为它会改写实体所属的结构分区，连带触发 archetype 迁移、chunk 重排、location 修正、query 命中变化，以及执行链上的同步或延迟提交；command buffer 不是在消灭这笔成本，而是在把它收束到可控时机。

顺着这条线继续往下走，下一篇最自然就该讲：

`为什么数据连续还不够，最后还是会走到显式调度。`
