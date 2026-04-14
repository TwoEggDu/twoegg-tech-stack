---
date: "2026-03-26"
title: "数据导向运行时 05｜构建期前移怎么做：Baking、Traits / Templates / Spawn、离线转换"
description: "把 Unity DOTS、Unreal Mass 和自研 ECS 放回同一条 authoring-to-runtime 主链里，说明为什么这些系统越来越喜欢把运行时代价前移到构建期，以及这种前移到底在吃掉什么成本。"
slug: "data-oriented-runtime-05-why-build-time-keeps-moving-forward-baking-traits-templates-spawn-and-offline-conversion"
weight: 360
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

> 这篇只回答一个问题：`为什么这些系统越来越喜欢把运行时代价前移到构建期。`
>
> 真正值得看的，不是 `Baking`、`Trait`、`Template` 这些词，而是当系统既想保留 authoring 世界的灵活性，又想要 runtime 世界的数据导向性能时，它会被逼着在哪个阶段把"人类友好的描述"压成"机器友好的数据"。

前面几篇已经把三件事讲出来了：

- [《数据导向运行时 02｜Archetype、Chunk、Fragment：性能到底建在什么地方》]({{< relref "engine-toolchain/data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from.md" >}})
  讲了 runtime 世界为什么会收敛到 `archetype + chunk + query cache`。
- [《数据导向运行时 03｜Structural Change、Command Buffer 与同步点：为什么改结构总是贵》]({{< relref "engine-toolchain/data-oriented-runtime-03-structural-change-command-buffer-and-sync-points-why-structural-mutations-are-expensive.md" >}})
  讲了为什么这套布局一旦成立，结构变化就必须被收束。
- [《数据导向运行时 04｜调度怎么做：Burst/Jobs、Mass Processor、自己手搓执行图》]({{< relref "engine-toolchain/data-oriented-runtime-04-how-scheduling-works-burst-jobs-mass-processor-and-self-built-execution-graph.md" >}})
  讲了为什么数据连续还不够，最后还得有显式执行骨架。

到这里，一个更上游的问题就一定会冒出来：

`如果 runtime 世界已经这么在意布局、结构稳定性和执行骨架，那 authoring 世界那套灵活、冗余、编辑器友好的数据，到底什么时候被压成 runtime 真正需要的形状？`

这就是这篇要回答的事。

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 为什么 authoring world 和 runtime world 越来越不适合用同一份数据直接承载。
2. Unity 的 `Authoring -> Baking -> Entity Scene / Runtime Data` 到底在前移什么成本。
3. Unreal 的 `Traits / EntityConfig / Template / Spawn` 到底在前移什么决定。
4. 为什么这件事不只是"导出一步数据"，而是运行时契约本身的一部分。
5. 如果自己手搓，为什么也最好尽早做一层离线转换，而不是运行时把对象拼出来。

## 先给一句总判断

如果必须先把这篇压成一句话，我会这样写：

`构建期前移的核心，不是"把工作提前做掉"这么简单，而是把 authoring 世界里灵活、冗余、可编辑的描述，尽可能收敛成 runtime 世界真正需要的结构、初值、模板和依赖边界，好让运行时不再同时承担内容编辑和高规模仿真两套成本。`

这句话里最重要的区分是：

- `authoring`
  更像给人编辑和组织内容的数据形态。

- `runtime`
  更像给机器高频处理和批量执行的数据形态。

如果这两层不分，运行时很容易同时背上：

- 冗余数据
- 高转换成本
- 不稳定结构
- 难以预测的初始化路径

## 证据地图

先说明这篇的证据边界。

当前这版首稿，能直接压实的事实主要来自下面几类官方资料：

- Unity `Subscenes overview`
- Unity `Scenes overview`
- Unity `Baking overview`
- Unity `Baking systems overview`
- Unity `Entities Graphics overview`
- Unreal `MassGameplay Overview`
- Unreal `FMassEntityConfig`
- Unreal `FMassEntityConfig::GetOrCreateEntityTemplate`
- Unreal `FMassEntityTemplateRegistry`
- Unreal `FMassEntityTemplateData`
- Unreal `FMassEntityTemplateBuildContext::BuildFromTraits`
- Unreal `FMassSpawnedEntityType`

这里仍然有一个需要明确写出来的限制：

- 当前本地还没有稳定可引用的 `com.unity.entities@1.x` 包源码路径
- `docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径也都还是 `TODO`

所以这篇里的 `事实` 只落在当前官方资料能直接支持的范围。

尤其 Unreal 这边，当前更适合把证据压在：

- trait 如何构建 template
- config 如何描述要 spawn 的实体类型
- spawner / template registry 如何把这些定义收成可复用运行时模板

这已经足够回答"构建期前移在结构上怎么成立"，不需要现在就硬追内部源码实现细节。

## 先把问题压清：为什么 authoring 和 runtime 越来越不适合同一份数据

如果先不看任何引擎，只看问题本身，这件事其实很好理解。

authoring 世界更在乎的是：

- 数据好不好编辑
- 组件是不是好理解
- 内容团队能不能直接操作
- 引用关系是不是直观
- 资源和脚本能不能自由组合

runtime 世界更在乎的是：

- 数据是不是已经按执行模式排好
- 初始化路径是不是可预测
- 是否能低成本装载和流送
- 是否能避免运行时再做大规模转换
- 是否能把结构和初值稳定地交给 query / scheduler / representation

这两套需求不是完全对立，但它们经常明显不一致。

最典型的矛盾就是：

- authoring 世界喜欢"方便改"
- runtime 世界喜欢"尽量别再改结构"

一旦你真的接受了这一点，构建期前移就不再像"附加工具链"。
它更像是：

`在 authoring world 和 runtime world 之间，专门加一层翻译与收敛。`

## Unity 这边，Baking 是把 authoring scene 压成 Entity Scene

先看 Unity。

### 从资料里能直接看见什么

Unity 官方文档已经把这条链讲得非常直接：

- `ECS uses subscenes instead of scenes`，因为 Unity 的 core scene system 与 ECS 不兼容
- 你可以把 `GameObject` 和 `MonoBehaviour` 放进 `SubScene`
- baking 会把这些 authoring GameObjects 和 authoring components 转成 entities 与 ECS components
- baking 的输出会写进 `Entity Scenes`，也就是 runtime data
- baking 只发生在 Editor，从不在游戏里执行，官方还明确把它类比成 asset importing
- baking 是不可逆的过程，会把性能开销高但灵活的 GameObject 表示，转成对性能和存储更友好的实体数据
- open subscene 会触发 live baking，closed subscene 会触发异步或后台 baking
- full baking 的输出是一组落盘文件，后续由 Editor 或应用加载

`Baking systems overview` 还继续给了几条很关键的事实：

- baking systems 本身就是 systems
- 它们通过 queries 批处理 entities 和 components
- 因为 baking system 也是 system，所以可以使用 jobs 和 Burst
- baking systems 不会自动追踪 dependencies 和 structural changes，你需要显式声明和维护

再看 `Entities Graphics` 文档，Unity 又把另一层边界补上了：

- `Entities Graphics` 是 ECS for Unity 与现有渲染架构之间的桥
- 它也会在 baking 阶段把 `MeshRenderer`、`MeshFilter` 这类 authoring 组件压成实体侧运行时数据
- baking 在 Editor 中离线执行并把结果存到磁盘

把这些事实合在一起，Unity 其实已经把"前移"写得很透了。

### 工程判断：Unity 前移的不是单个步骤，而是整条 authoring-to-runtime 入口

基于这些事实，对 Unity 更稳的判断是：

`Unity 不是在运行时"顺手转一下数据"，而是在正式 authoring 入口和正式 runtime 入口之间，单独建立了一条 baking 生产线。`

它前移的成本，至少包括下面几类：

1. `表示收敛`
   把 GameObject / MonoBehaviour 的 authoring 形态压成 ECS 组件形态。

2. `结构稳定`
   把运行时需要的 archetype-compatible 数据提前组织出来，而不是进 Play 之后再大批量转换。

3. `初始化成本`
   把很多原本可能在加载或进入世界时才做的加工，提前成实体场景文件。

4. `桥接成本`
   把像渲染桥这种"旧世界与新世界之间的转换"尽量收在 baking 阶段，而不是每帧实时拼。

所以 Unity 的 baking 不该被理解成"导出一步"。
它更像：

`让 runtime 世界不必再同时背着 authoring 世界一起跑。`

### 为什么 Unity 会接受 live baking / full baking 这种复杂度

这个问题很值得单独提一下。

Unity 既然知道 baking 很复杂，为什么不干脆让运行时自己处理？

因为官方文档已经把答案写出来了：

- baking 很花时间和处理能力
- 如果在游戏里做，会直接拉低运行时性能
- Editor 和 closed subscene 的不同模式，正是在平衡 authoring 反馈速度和离线稳定产出

也就是说，Unity 宁愿把系统做复杂一些，也不愿意让 runtime 世界继续同时承担：

- 内容编辑灵活性
- 大量结构化转换
- 高规模实体初始化

这正是"前移"的本质。

## Unreal 这边，Traits / Config / Template / Spawn 是另一种前移

再看 Unreal `Mass`。

### 从资料里能直接看见什么

`MassGameplay Overview`、`FMassEntityConfig`、`TemplateRegistry` 和 `BuildFromTraits` 这条链，已经足够落下几条关键事实：

- `Mass Spawner` 子系统负责根据 `MassSpawner` 对象和 procedural calls 生成与管理 entities
- `Mass Spawner` 子系统拥有 `Mass Entity Template Registry`，用来存放可用 entity templates 信息
- `FMassEntityConfig` 明确描述一个 Mass agent to spawn
- `FMassEntityConfig` 不是直接描述"马上生成一个 entity 实例"，而是描述一组 features / traits，用来创建 entity template
- `GetOrCreateEntityTemplate(World)` 会根据 config 中包含的 features 创建 entity template
- `FMassEntityTemplateRegistry` 是一个 repository，存储在 `FMassEntityConfig` 处理过程中或自定义代码里创建并注册的 templates
- `FMassEntityTemplateData` 的职责，是定义并构建 finalized template；一旦 finalize 成 `FMassEntityTemplate`，它就会变成 immutable
- `BuildFromTraits` 直接说明：template build context 是从一组 traits 构建出来的
- `MassEntityTraitBase` 的文档也明确说，trait 是一组 fragments 的逻辑封装，template building method 可以基于属性或缓存值来配置 fragments
- `FMassSpawnedEntityType` 则明确把 `EntityConfig` 作为"描述要 spawn 的实体"的资产输入

这组事实很重要，因为它们说明 Unreal `Mass` 的前移不是走 `SubScene + Baking` 这条路。

它更像是在说：

`先用 traits 和 config 把"这一类实体该长什么样"收成 template，再在 spawn 时按 template 批量实例化。`

### 工程判断：Unreal 前移的重点是"组合与模板化"

基于上面这些事实，对 Unreal `Mass` 更稳的判断是：

`Mass 的前移重点，不是把整个关卡世界离线烘焙成另一份 scene，而是把"这类实体由哪些 fragments、哪些特性、哪些初始值构成"提前收敛成模板。`

它前移的成本，主要在下面几类：

1. `组合决策`
   哪些 traits 组合在一起，最后会生成哪种 fragments / tags / shared setup。

2. `模板构建`
   entity template 的 composition 和初值，不需要等到每次 spawn 时临时推导。

3. `运行时复用`
   已经构建好的 templates 可以被 registry 和 spawner 反复使用，而不是每次从原始配置重新拼。

4. `authoring-to-runtime 边界`
   content 侧看到的是 config / trait / spawner 这些更可描述的输入，而 runtime 侧更接近 template / spawned entities 这种可直接消费的形态。

所以 Unreal 这条线虽然没有 Unity 那么强烈的"离线烘焙整份 scene"气质，但它同样在做前移。

只是它前移的重点更偏：

`把实体构成和初始化规则先模板化。`

## Unity 和 Unreal 前移的共同本质

两边外观差异很大。

Unity 更像：

- `SubScene`
- `Baking`
- `Entity Scene`
- `runtime data files`

Unreal 更像：

- `Trait`
- `EntityConfig`
- `EntityTemplate`
- `Spawner / Registry`

但如果把表面名词去掉，你会发现它们在回答同一个问题：

`authoring 世界里对人友好的描述，什么时候被压成 runtime 真正想消费的形状。`

如果先压成一张最小对照表，我会这样记：

| 对照维度 | Unity DOTS | Unreal Mass | 自研第一版 |
| --- | --- | --- | --- |
| 前移入口 | `SubScene + Baking` | `Traits + EntityConfig + Template build` | `offline conversion / template build` |
| authoring 侧输入 | `GameObject / MonoBehaviour / authoring scene` | `Trait / Config / Spawner setup` | `prefab-like description / config data` |
| runtime 侧输出 | `Entity Scene / runtime data / ECS components` | `EntityTemplate / spawned entities` | `archetype-ready data / template` |
| 前移的核心 | 把 authoring scene 压成 runtime ECS 数据 | 把实体组成和初值规则压成可复用模板 | 把灵活描述压成 runtime 可直接装载形态 |
| 运行时收益 | 少做转换，少背 authoring 灵活性 | 少做临时拼装，稳定 spawn 输入 | 少在加载期和首帧做大规模结构化工作 |

这张表最重要的不是"它们一一等价"。

而是它会逼你看到一个更稳的结论：

`构建期前移的共同本质，是把 runtime 世界不想再承担的自由度、转换成本和组合决策，尽量提前收敛掉。`

## 为什么这不是"多一个工具链步骤"那么简单

这里必须再强调一次。

如果你把 baking、template build、spawn config 都理解成"多一个工具步骤"，你会低估它们的重要性。

它们真正决定的是：

- runtime 入口接到的到底是什么数据
- query / scheduler / representation 看到的数据边界是否稳定
- runtime 是否还要临时拼结构
- 同一类实体是不是有可重复的初始化契约

也就是说，这件事碰到的不是"导出格式"。
它碰到的是 runtime 契约本身。

这也是为什么：

- Unity 的 baking 直接挂在 scene / subscene / entity scene 这条主链上
- Unreal 的 template / spawner 直接挂在 entity creation 这条主链上

因为它们都知道：

`如果 authoring 到 runtime 的翻译不收敛，后面的 archetype、query、scheduler、representation 全都会跟着变脏。`

## 如果自己手搓，为什么也最好做一层离线转换

到这里，自研第一版其实也很难再继续假装：

`我运行时现拼就好了。`

### 1. 不做离线转换，运行时会被迫同时做两套工作

如果你直接让 runtime 世界自己从灵活对象描述里现拼：

- 你要在加载期临时决定组件集合
- 你要在进入世界时临时分配和构建 archetype-ready 数据
- 你要在首帧或流送时临时做大量结构化转换

这意味着 runtime 既要做仿真，又要做 authoring-to-runtime 翻译。

这通常不是好主意。

### 2. 第一版哪怕不做复杂 baking，也该先做 template build

自研第一版当然不需要复刻 Unity 那么完整的 baking world。

但至少应该尽早做一层最小转换：

- 输入：prefab-like 配置、组件初值、标签、共享数据描述
- 输出：稳定的 archetype signature、初值模板、可批量 spawn 的 runtime payload

也就是说，最小版本至少可以先做到：

- `config -> template`
- `template -> batch spawn`

先别急着追 live baking、增量 baking、编辑器回灌这些高级能力。

第一版更重要的是：

`别让 runtime 每次创建实体时都重新理解一遍"这一类东西应该长什么样"。`

### 3. 离线转换不只是为了快，也是为了让结构更稳定

这点很容易被忽略。

离线转换的价值，不只是少做几次运行时计算。

它还会直接提高下面这些东西的稳定性：

- archetype 更早确定
- query 命中边界更稳定
- representation bridge 更容易提前准备
- 调试时更容易知道"输入定义"和"runtime 形态"分别是什么

所以对自研第一版来说，最稳的方向不是：

`先把所有对象原样带进 runtime，再慢慢转换。`

更稳的方向是：

`先在离线或加载前阶段，把 runtime 真正需要的结构和初值收成模板。`

## 常见误解

### 误解一：构建期前移只是为了更快加载

不够准确。

更快加载当然是结果之一。
但它真正解决的还包括：

- 减少 runtime 结构化转换
- 稳定 archetype / template 边界
- 把 authoring 灵活性和 runtime 性能要求拆开

### 误解二：Unity 有 baking，Unreal 没有同类东西

也不对。

它们外观很不同。
但 Unreal 的 `trait -> config -> template -> spawn` 一样在做"提前收敛 runtime 真正想消费的实体形态"。

### 误解三：自研第一版先不做离线转换也没关系，反正以后再补

可以这么做，但代价通常很快就会回来。

因为你会发现：

- 创建路径越来越重
- 首帧和流送初始化越来越脏
- archetype 决定散落在 runtime 各处

这会让后面所有系统一起失稳。

### 误解四：前移越多越好

也不是。

前移太多，会把工具链和 authoring 反馈复杂度抬得很高。
所以更稳的做法不是"能前移的全前移"，而是：

`把 runtime 不想再承担、而且输入又足够稳定的那部分，优先前移。`

## 我的结论

如果一定要把这篇压成一句话，我会这样写：

> 构建期前移的真正意义，不是单纯"提前算一遍"，而是把 authoring 世界里灵活、冗余、可编辑的描述，提前收敛成 runtime 世界真正想消费的结构、模板和初值；Unity 用 `SubScene + Baking + Entity Scene` 做这件事，Unreal 用 `Trait + Config + Template + Spawn` 做这件事，自研第一版也最好至少先做一层 `config -> template -> spawn` 的最小离线转换。

顺着这条线继续往下走，下一篇最自然就该讲：

`当 runtime 数据已经被收敛出来之后，它怎样再接回 Actor、GameObject、ISM 和渲染表现。`
