---
date: "2026-03-26"
title: "数据导向运行时 02｜Archetype、Chunk、Fragment：性能到底建在什么地方"
description: "把 Unity DOTS、Unreal Mass 和自研 ECS 放回同一条存储主链里，说明 archetype、chunk、fragment、query cache 分别在解决什么问题，以及性能为什么不是凭空来的。"
slug: "data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from"
weight: 330
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

> 这篇只回答一个问题：`为什么这类系统的性能，最后几乎都会建在 archetype、chunk 和 query cache 这套结构上。`
>
> 真正值得看的，不是 `Archetype`、`Fragment` 这些词本身有多新，而是当你试图让几千到几十万个同构对象稳定更新时，系统为什么会一次次收敛到这套存储形状。

上一篇
[《数据导向运行时 01｜Unity DOTS、Unreal Mass 与自研 ECS：问题空间怎么对齐》]({{< relref "engine-toolchain/data-oriented-runtime-01-aligning-unity-dots-unreal-mass-and-self-built-ecs.md" >}})
先把三条线的比较标尺立住了：

- 世界怎样承载
- 高规模对象怎样存
- 处理逻辑怎样找到对象
- 结构变化怎样提交
- 执行怎样调度
- 仿真层和表示层怎样切
- 构建期和运行期怎样分工

这篇只往前走一步，专门盯第二件事：

`高规模对象到底怎样存，系统的"快"又到底是从哪里买来的。`

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 为什么 `archetype` 不是术语装饰，而是这类系统的分类主轴。
2. 为什么 `chunk` 几乎总会出现，而且它解决的不是"好看"，而是访问模式和批处理粒度。
3. `component`、`fragment`、`tag` 这些名词，真正应该怎样对齐。
4. 为什么 query 几乎都不会每帧从零扫世界，而是会缓存 archetype 集合。
5. 如果自己手搓第一版，应该从这里继承什么，而不是照抄哪家的 API。

## 先给一句总判断

如果必须先把这篇压成一句话，我会这样写：

`这类系统的性能，不是来自"少用继承"，而是来自先按组件组合把对象分桶，再把同桶对象压进连续块里，让查询命中的不是零散对象，而是一批已经预分组的数据。`

这句话里其实压着四层结构：

- `EntityHandle` 解决身份
- `Archetype` 解决分类
- `Chunk` 解决局部性和批处理粒度
- `Query cache` 解决"别每次都重找一遍"

后面如果 `03` 要讲结构变化为什么贵，也得建立在这四层已经成立的前提上。

## 证据地图

先说明这篇的证据边界。

当前这版首稿，能够直接压实的事实主要来自下面几类官方材料：

- Unity `Entities` 的 `Archetypes concepts`、`EntityQuery overview`、`World concepts`、`Component concepts`
- Unity `ArchetypeChunk` API 文档
- Unreal 的 `FMassEntityManager`、`FMassEntityQuery`、`FMassFragmentRequirements`、`FMassEntityHandle`、`FMassArchetypeEntityCollection`

这里仍然有两个限制：

- 本地还没有稳定可引用的 `com.unity.entities@1.x` 包源码路径
- `docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径都还是 `TODO`

所以这篇里所有"事实"都只落在当前官方资料能直接支持的范围。

也就是说：

- `事实` 负责回答"官方资料明确写了什么"
- `工程判断` 负责回答"这些事实合在一起，说明系统为什么长成这样"

因此这篇只先讲存储与查询的主链，不提前把 `04` 的调度问题和 `03` 的结构变化成本混进来。

## 先把最小结构图画出来

如果把这类系统的存储主链先画成一张最小图，我会这样记：

```text
EntityHandle / EntityId
        |
        v
Archetype(signature of component/fragment/tag set)
        |
        v
Chunk 0: [EntityId[] | Position[] | Velocity[] | Health[]]
Chunk 1: [EntityId[] | Position[] | Velocity[] | Health[]]
Chunk 2: [EntityId[] | Position[] | Velocity[] | Health[]]
        |
        v
Query cache -> matching archetypes -> matching chunks -> system iteration
```

这张图里最关键的一点是：

`系统真正遍历的，通常不是"一个一个对象"，而是"满足条件的一批 chunk"。`

所以性能的来源也不神秘。

它主要来自下面几件事一起成立：

- 满足同一类规则的对象已经先被分到同一种结构里
- 这些对象在内存里不是散着的，而是按列或近似按列排好
- 查询先找到 archetype，再拿到一批 chunk，而不是全世界暴力筛
- 一次批处理过程中，访问模式和内存布局比较一致

## EntityId / EntityHandle 先解决的不是速度，而是稳定身份

很多人一看到 `Archetype`、`Chunk`，会立刻把注意力放到"连续内存更快"。

但这条链最前面先要站住的，反而不是性能，而是身份。

### 从资料里能直接看见什么

Unity 文档明确说明：

- 一个 `World` 是实体的集合
- 实体 ID 只在自己的 `World` 内唯一
- `EntityId` 包含 `index` 和 `version`
- 因为索引会复用，所以实体被销毁时版本号会递增，用来避免旧句柄误认成新实体

Unreal 这边，`FMassEntityHandle` 的 API 也直接暴露了：

- `Index`
- `SerialNumber`

而且文档还明确提醒，单看 `Index` 和 `SerialNumber` 是否被设置，并不等于这个实体在当前子系统里仍然有效。

### 工程判断

这层真正说明的是：

`高规模数据导向系统首先需要一个便宜、可复用、可失效检测的身份句柄。`

也就是说，第一步不是"对象里装更多功能"，而是：

- 给实体一个可以被复用的槽位索引
- 再配一个版本号或序列号防悬挂引用

所以如果你自己手搓第一版，`index + generation` 基本不是"可选优化"，而是默认答案。

原因不是因为大家都爱抄同一套。

而是因为只要实体会删、会复用、会迁移，身份问题迟早会把你逼到这里。

## Archetype 先解决的不是速度，而是"按结构分桶"

现在才轮到 archetype。

### 从资料里能直接看见什么

Unity `Archetypes concepts` 里，最关键的事实有三条：

- archetype 是"同一世界里，拥有同一组组件类型的实体"的唯一标识
- 给实体加减组件类型时，`EntityManager` 会把实体移动到对应 archetype
- archetype 组织方式让按组件类型查询更高效，因为系统可以直接找满足条件的 archetype，而不是扫描每个实体

Unreal `FMassEntityManager` 和 `FMassFragmentRequirements` 也给出了一样硬的事实：

- `FMassEntityManager` 负责承载实体并管理 archetypes
- 每个有效实体会被分配到"当前 fragment 组合对应的 archetype"
- `FMassFragmentRequirements` 直接把"某个 archetype 需要满足什么 fragment 条件"写成了查询前提

换句话说，不管叫 `component` 还是 `fragment`，最稳定的那层都不是字段名，而是：

`一组对象当前拥有哪一组数据类型。`

### 工程判断

archetype 真正解决的问题，不是"帮你归类得更优雅"。

它解决的是：

`先把世界按结构切成一批桶，让后续查询和布局都不必再面向单个对象做决定。`

如果没有这层"先按结构分桶"，后面很多事情都会变丑：

- 你很难知道哪些对象能一起跑同一段逻辑
- 你很难把数据压到同一种布局里
- 你每次查询都会接近全世界重新筛

所以 archetype 的核心意义，不是名词，而是：

`把"对象的组成"从运行时到处临时判断，变成一个稳定的结构分区。`

这也是为什么 `03` 讲 structural change 时，一加一减组件会这么贵。
因为它不是在改一个字段，而是在把对象从一个结构分区搬到另一个结构分区。

## Chunk 真正解决的是局部性和批处理粒度

如果 archetype 解决的是"先怎么分桶"，chunk 解决的就是"桶里的东西具体怎样排"。

### 从资料里能直接看见什么

Unity 这边，官方资料给出的事实已经很直接：

- 同一 archetype 的实体和组件，会被放进统一的内存块，也就是 chunks
- 一个 chunk 是 `16KiB`
- chunk 里会为每种组件维护自己的数组，同时还有实体 ID 数组
- 同一个 chunk 内，各组件数组按相同索引对齐
- 删除或迁移实体时，chunk 会用末尾实体补洞
- 当 archetype 现有 chunk 装满时，`EntityManager` 会创建新 chunk

Unreal 这边，官方 API 文档也给了足够硬的信号：

- `FMassEntityManager` 明确写出 entities 以 `chunked array` 形式存储
- `FMassArchetypeEntityCollection` 的职责，是把某 archetype 的实体集合转换成一串连续 entity chunks
- `FMassEntityQuery` 本身就是对"缓存后的有效 archetypes 集合"触发计算

虽然 Unity 和 Unreal 文档展开程度不完全一样，但对你真正有用的事实是同一个：

`查询和执行面对的，不是一个抽象的"对象列表"，而是一批已经按结构排好的连续块。`

### 工程判断

chunk 之所以总会出现，不是因为大家都爱 16KiB，也不是因为"块状存储听起来更高级"。

它真正解决的是三个现实问题：

1. `局部性`
   同一批实体的同类数据靠得更近，系统扫过它们时更容易形成稳定访问模式。

2. `批处理粒度`
   处理逻辑可以按 chunk 为单位推进，而不是频繁跳回对象层。

3. `元数据边界`
   很多"这一批实体共享的信息"更适合附着在 chunk 级，而不是每个实体都重复带一份。

这里最容易被误解的一点是：

`chunk 不是单纯为了"连续"，而是为了让系统能以一种可预测的粒度工作。`

连续只是现象。
更本质的是你终于能说清：

- 这一批对象数据长什么样
- 一次迭代拿多少对象
- 哪些附加状态属于整批数据，而不是单个实体

所以如果自己手搓第一版，我反而建议先别纠结"是不是一定 16KiB"。

第一版更重要的是：

- 每个 archetype 下有固定大小的数据页
- 每页里按列排组件数组
- 每页都能快速知道当前实体数量和容量
- 删除时能补洞，迁移时能搬页

只要这些成立，你就已经拿到了 chunk 思想的主体。

## Component、Fragment、Tag 真正应该怎样对齐

这类文章很容易在这里被术语带歪。

### 从资料里能直接看见什么

Unity 文档把组件写成：

- 实体上的数据
- `IComponentData` 这种纯数据类型
- 按 archetype 存进 chunk

而 Unreal 文档把 fragment 写成：

- 描述某类实体数据的轻量数据单元
- 由 `FMassFragmentRequirements` 和 `FMassEntityQuery` 作为查询约束
- 决定实体会落到哪个 archetype

另外，Unreal API 和周边插件文档还直接暴露了几类与"单实体数据"不同的东西：

- `ChunkFragmentRequirements`
- `SharedFragment`
- `Tag`

Unity 这边虽然这篇不展开 shared component 和 chunk component，但 `Archetypes window` 也已经把它们单列成外部组件类别。

### 工程判断

如果把这些名词压回结构问题，我会这样对齐：

- `Component / Fragment`
  主要是"每个实体自己带的数据列"。

- `Tag`
  主要是"改变实体属于哪一类结构，但自己几乎不占每实体 payload"的存在标记。

- `Shared / Chunk-level data`
  主要是"这批实体共享或按 chunk 共享的数据"，不该在每个实体上重复拷贝。

这三类东西分不清，后面就很容易把性能判断做错。

因为它们影响系统的方式并不一样：

- 有些东西改变每实体负载大小
- 有些东西改变 archetype 分区
- 有些东西改变 chunk 级元数据

所以真正该盯的不是"这个名词在两边是不是同名"，而是：

`它到底影响每实体数据、结构分桶，还是整批数据的元信息。`

## Query 为什么几乎都要缓存 archetype 集合

到这里，query 的角色就顺了。

### 从资料里能直接看见什么

Unity `Archetypes concepts` 和 `EntityQuery overview` 这两条资料，足够直接支持下面几点：

- 按 archetype 组织后，按组件类型查询就不需要扫每个实体
- 一个 `EntityQuery` 先找到满足条件的 archetypes
- 然后再收集这些 archetype 的 chunks 供系统处理
- 世界里存在的 archetypes 往往会较早稳定下来，所以缓存查询会更快

Unreal `FMassEntityQuery` 的文档则说得更直白：

- 它用于在"缓存后的有效 archetypes 集合"上触发计算
- fragment requirements 和 subsystem requirements 会决定哪些 archetypes 有效

这两边一对照，几乎已经把 query cache 的意义讲透了。

### 工程判断

query 真正解决的不是"帮你把筛选条件写漂亮"。

它解决的是：

`既然世界已经按结构分桶，那系统就不该每帧再回到"逐对象重新判断"这条慢路上。`

所以 query cache 的价值，不是额外技巧，而是 archetype 模型的自然结果：

- archetype 负责先把世界切成有限种结构
- query 负责声明想看哪些结构
- cache 负责别每次都从零重建这个映射

这也是为什么我会说：

`archetype + chunk + query cache` 其实是一套连在一起的结构，不应该拆开理解。

只有 archetype，没有 chunk，你很难把局部性和批处理粒度站稳。
只有 chunk，没有 query cache，你又会每帧重复找路。

## 如果自己手搓，第一版应该怎么从这里抄

这里的"抄"，不是抄 API，而是抄结构。

如果让我给第一版最小系统定边界，我会这样做：

### 1. 先把句柄做成 `index + generation`

别用裸指针，也别假装对象引用可以自然承受删除和复用。

这一步不是优化，是世界稳定运行的基础。

### 2. archetype 用"组件类型集合"做 key

可以是排序后的 type list，也可以是 bitset 或 hash。

重点不是形式，而是：

- 相同结构的实体一定命中同一个 archetype
- 结构变化时一定能找到目标 archetype

### 3. 每个 archetype 下放固定大小 chunk

第一版不必执着于 Unity 的 `16KiB`。

但要明确下面几件事：

- chunk 是固定容量
- chunk 内按列排数据
- 每列索引一致
- 实体删除能补洞
- archetype 满了能继续挂新 chunk

### 4. query 先做 `All / Any / None`

这已经足够证明整条结构能不能站住。

别一开始就做复杂 DSL。

### 5. query 结果缓存到 archetype 集合

第一版不需要极致聪明，但至少要做到：

- 新 archetype 出现时，能增量更新已有查询
- 系统执行时，不是每次从世界所有实体重扫

### 6. 把 tag、shared data、chunk data 的边界先留出来

第一版可以先只实现最常用的一种。

但结构上不要把所有东西都塞成"每个实体都有一份普通组件"。

否则等你后面做 LOD、群组状态、chunk 级统计时，系统会非常难看。

## 常见误解

### 误解一：Archetype 的意义就是"组件列表"

不够准确。

它当然可以被描述成组件类型集合。
但对运行时来说，它真正重要的是：

`它把一批对象的结构身份固定下来，让存储、查询和迁移都能围绕这个结构身份工作。`

### 误解二：只要用了连续内存，系统就会自动变快

不是。

连续内存只是前提之一。
真正让它变得有意义的，是：

- 访问模式和布局相匹配
- 查询命中的确是一批同结构对象
- 结构变化没有把系统频繁打碎

### 误解三：Tag 没有 payload，所以几乎没有成本

也不对。

即使 tag 不怎么占每实体字节，它依然会影响 archetype。
而一旦 archetype 变了，查询命中、chunk 分布和结构迁移成本都会跟着变。

### 误解四：Query 本质上就是"每帧筛一遍对象"

这恰恰是对象世界的旧思路。

在 archetype 模型里，更合理的思路是：

`先找到满足条件的结构，再处理这些结构下面已经排好的 chunks。`

## 我的结论

如果一定要把这篇压成一句话，我会这样写：

> Archetype 负责先把世界按结构分桶，chunk 负责把同桶数据压成可批处理的连续块，query cache 负责让系统反复命中的不是零散对象，而是已经按结构排好的数据集合；这才是 Unity DOTS、Unreal Mass 和自研 ECS 性能真正开始成立的地方。

下一篇如果继续顺着这条线往下走，就不该先去讲更多 API，而应该去讲：

`为什么一旦这套布局成立，structural change 就会天然变贵。`
