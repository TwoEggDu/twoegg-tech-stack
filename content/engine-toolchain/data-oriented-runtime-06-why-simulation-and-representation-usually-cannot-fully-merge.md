---
date: "2026-03-26"
title: "数据导向运行时 06｜表示层边界怎么切：GameObject、Actor、ISM 与 ECS 世界"
description: "把 Unity DOTS、Unreal Mass 和自研 ECS 放回同一条 simulation-to-representation 主链里，说明为什么仿真层和表示层通常不能彻底合并，以及表示桥真正应该站在哪里。"
slug: "data-oriented-runtime-06-why-simulation-and-representation-usually-cannot-fully-merge"
weight: 370
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

> 这篇只回答一个问题：`为什么仿真层和表示层通常不能彻底合并。`
>
> 真正需要被解释的，不是"ECS 能不能渲染"，而是当 runtime 数据已经被收敛出来之后，它到底怎样再接回 GameObject、Actor、ISM、LOD、相机和渲染表现，而不把仿真世界重新拖回对象泥潭里。

前面几篇已经把数据导向运行时的主骨架立起来了：

- [《数据导向运行时 02｜Archetype、Chunk、Fragment：性能到底建在什么地方》]({{< relref "engine-toolchain/data-oriented-runtime-02-archetype-chunk-fragment-where-performance-comes-from.md" >}})
  讲了为什么 runtime 世界会收敛到 `archetype + chunk + query cache`。
- [《数据导向运行时 03｜Structural Change、Command Buffer 与同步点：为什么改结构总是贵》]({{< relref "engine-toolchain/data-oriented-runtime-03-structural-change-command-buffer-and-sync-points-why-structural-mutations-are-expensive.md" >}})
  讲了为什么结构变化必须被收束。
- [《数据导向运行时 04｜调度怎么做：Burst/Jobs、Mass Processor、自己手搓执行图》]({{< relref "engine-toolchain/data-oriented-runtime-04-how-scheduling-works-burst-jobs-mass-processor-and-self-built-execution-graph.md" >}})
  讲了为什么系统还需要显式执行骨架。
- [《数据导向运行时 05｜构建期前移怎么做：Baking、Traits / Templates / Spawn、离线转换》]({{< relref "engine-toolchain/data-oriented-runtime-05-why-build-time-keeps-moving-forward-baking-traits-templates-spawn-and-offline-conversion.md" >}})
  讲了 authoring 世界怎样被收敛成 runtime 真正想消费的结构和模板。

那接下来的问题就非常自然：

`当这套仿真世界已经被收成更纯的数据形态之后，它怎样再长回"能被玩家看见和交互"的表现世界？`

这就是这篇要回答的事。

## 这篇要回答什么

这篇主要回答 5 个问题：

1. 为什么"一个实体对应一个表现对象"不是稳定前提。
2. Unity 里 `Entities Graphics`、`Companion Components` 和 `GameObject / MonoBehaviour` 的边界到底怎么切。
3. Unreal 里 `Actor`、`Mass Representation`、`LOD`、`ISM` 的关系到底说明了什么。
4. 为什么表示层和仿真层在更新频率、身份粒度、线程约束上天然不同。
5. 如果自己手搓，为什么第一版最好一开始就做 `Representation Bridge`，而不是让 ECS 直接托管全部表现。

## 先给一句总判断

如果必须先把这篇压成一句话，我会这样写：

`仿真层和表示层之所以通常不能彻底合并，不是因为引擎作者保守，而是因为它们服务的目标不同：仿真层追求结构稳定、批处理和可调度，表示层追求可见性、层级、LOD、材质、相机和引擎对象协作；真正稳的做法不是强行统一，而是明确做一层表示桥。`

这句话里最重要的一个提醒是：

`"实体"` 和 `"看得见的东西"`，并不总是同一层概念。

有时候是：

- 一实体对一对象

但很多时候其实是：

- 多实体对一表现批次
- 一实体对零表现
- 一实体在不同 LOD 下切换不同表现类型

只要这一点成立，"仿真 == 表现"的直觉就已经不稳了。

## 证据地图

先说明这篇的证据边界。

当前这版首稿，能直接压实的事实主要来自下面几类官方资料：

- Unity `Entities Graphics overview`
- Unity `Companion components`
- Unity `Entities Graphics feature matrix`
- Unreal `Overview of Mass Gameplay`
- Unreal `Mass Representation` / `Mass LOD` / `Mass Spawner` 相关说明

这里仍然有一个需要明确写出来的限制：

- 当前本地还没有稳定可引用的 `com.unity.entities@1.x` 包源码路径
- `docs/engine-source-roots.md` 里 Unity 和 Unreal 的源码根路径也都还是 `TODO`

所以这篇里的 `事实` 只落在当前官方资料能直接支持的范围。

尤其 Unreal 这一侧，这篇当前不追 `Mass Representation` 内部源码实现，只使用官方 `MassGameplay` 概览里已经明确写出的表示、LOD、ISM、pooling 和 spawner 边界。

## 先把最关键的误解打掉：一个实体，不一定对应一个表现对象

这篇如果不先把这句话讲清，后面很容易一直写偏。

很多人会默认觉得：

`既然 runtime 世界里有 entity，那屏幕上就应该有一个和它一一对应的对象。`

这在小规模原型里常常还能成立。
但一旦系统开始认真处理：

- 大规模群体
- LOD
- culling
- instancing
- pooling
- 纯逻辑实体

这个一一对应就会很快失效。

最直接的几种情况就是：

1. `一实体对零表现`
   逻辑存在，但当前不可见、被关停、或只参与仿真。

2. `一实体对不同表现类型`
   近处是 Actor / GameObject，远处是 ISM，极远处直接 Off。

3. `多实体对一表现批次`
   instancing、批渲染、群体表示都会把多个实体压进一组渲染表示。

一旦这些情况成立，你就很难再把表示层简单写成"仿真数据附带的一个字段"。

## Unity 这边，官方直接把 ECS 渲染写成"桥"，而不是新渲染管线

先看 Unity。

### 从资料里能直接看见什么

`Entities Graphics overview` 里，最关键的事实有几条：

- `Entities Graphics` 不是 render pipeline
- 它是一个 system，会收集渲染 ECS entities 所需的数据，再把这些数据送给 Unity 现有渲染架构
- URP 和 HDRP 仍然负责 authoring 内容并定义 rendering passes
- runtime 时，`Entities Graphics` 处理带有 `LocalToWorld`、`RenderMesh`、`RenderBounds` 等组件的 entities
- 被处理的 entities 会被加到 batches 里，再由 `SRP Batcher` 渲染
- GameObject baking system 会把 `MeshRenderer`、`MeshFilter`、`LODGroup`、`Transform` 这类 authoring 组件压成实体侧运行时组件

这组事实非常关键。

因为它直接说明：

`Unity 并没有把"ECS 渲染"写成一套完全脱离现有渲染体系的新世界。`

更准确地说法是：

`它在 ECS 仿真世界和现有渲染架构之间，加了一层专门的桥。`

### Companion Components 又把另一条边界暴露得很清楚

再看 `Companion components` 文档，官方又给了另一组非常硬的事实：

- 你可以把 `MonoBehaviour` 组件作为 companion component 附着到 entity 上，而不是把它们转成 `IComponentData`
- 这也意味着 managed companion components 不享受 ECS 组件那种高性能
- companion component entity 的 transform 会随着 `LocalToWorld` 更新
- companion component 可以放进 subscene，并且 managed component 会序列化在 subscene 里
- 你可以写同时包含 `IComponentData` 和 managed companion component 的 ECS query
- 但这类 query 不能 Burst compile，而且必须在主线程执行，因为 managed components 不是线程安全的
- 某些图形相关组件会被转成 companion components
- Camera 转换默认还是禁用的，因为 scene main camera 不能作为 companion component entity

这几条事实比"Unity 支持混合"更有信息量。

它们直接把边界写出来了：

- 你可以桥接
- 但桥接后的 managed 对象不是免费午餐
- 一旦你跨进 managed / MonoBehaviour 世界，Burst 和线程约束就会回来
- 某些关键表现对象，比如主相机，本来就不适合被当成普通 companion entity

### 工程判断：Unity 的表示层边界是"桥接优先"，不是"彻底同化"

基于上面这些事实，对 Unity 更稳的判断是：

`Unity 在表示层问题上的真实答案，不是"GameObject 退场"，而是"尽量让仿真层留在 ECS 世界，再用 Entities Graphics 和 companion components 有选择地接回现有表现体系"。`

它暴露出来的边界至少有三层：

1. `纯 ECS 表现路径`
   像 `RenderMesh`、`RenderBounds`、批渲染这种，更适合直接走实体到渲染批次的路径。

2. `混合表现路径`
   像 Light、VFX、Volume、部分图形组件，更适合通过 companion component 桥接。

3. `传统对象路径`
   某些对象天然还是相机、主表现对象、复杂 MonoBehaviour 驱动世界的一部分，不会因为你有了 ECS 就自然消失。

所以 Unity 这里最关键的结论不是"都能兼容"。

更关键的是：

`只要你跨进 GameObject / MonoBehaviour 世界，线程、安全性和性能模型就会明显变。`

这也是为什么 companion components 官方文档会直接强调：

- 不享受 ECS 组件的快
- 不能 Burst
- 需要主线程

## Unreal 这边，官方直接把 Representation、LOD、ISM 写成 MassGameplay 子系统

再看 Unreal。

### 从资料里能直接看见什么

`Overview of Mass Gameplay` 已经把表示层边界写得非常清楚：

- `Mass Gameplay` 直接建立在 `Mass Entity` 之上
- 它包含 world representation、spawning、LOD、replication、StateTree 等功能
- `Mass Representation` 子系统专门负责 Mass Entities 的不同 visual aspects
- 对每个 representation LOD，系统都可以在四种表示类型里选择：
  - `high resolution Actor`
  - `low resolution Actor`
  - `Instanced Static Mesh (ISM)`
  - `No representation`
- 文档明确说 ISM 是最便宜的表示方式
- Representation 子系统负责不同表示类型之间的 transition
- 它直接和 `MassActorSpawner` 以及 `MassLOD` 子系统协作
- 它还能自动 recycle 和 pool spawned Actors

再看 `Mass LOD`，官方文档又直接补上了另一组事实：

- `Mass LOD` 负责为每个 entity 计算 LOD
- `Mass (Representation/Visualization) LOD` 不只算距离，还会算可见性和 frustum 相关条件
- 这个系统会把 entities 按"距离裁掉、视锥裁掉、可见"这些状态分进不同 chunks
- `Mass Simulation LOD` 则是为了负载均衡实体计算，会把同 LOD 的 entities 分成 chunks，甚至支持 variable frequency update

这几组事实放在一起，其实已经把 Unreal 的边界讲得很透了。

### 工程判断：Unreal 直接承认"仿真 LOD"和"表现 LOD"是两回事

对 Unreal `Mass` 最稳的判断，我会压成这样一句：

`Unreal 没有假装"表示层只是仿真层的附属字段"，它直接把 representation、simulation LOD、replication LOD 都拆成了独立子系统。`

这背后其实有三个很强的信号。

第一，`表示类型不是固定的`。

同一个 entity 在不同 LOD 下，可以是：

- 高精度 Actor
- 低精度 Actor
- ISM
- 或直接不显示

这已经直接推翻了"一实体永久对应一个对象"的直觉。

第二，`表现层和仿真层的更新目标不同`。

- `Representation/Visualization LOD` 在乎的是看不看得见、用什么表现最合适
- `Simulation LOD` 在乎的是计算负载和更新频率

这说明即使它们都围绕同一个 entity 世界，也不应该被写成同一个系统。

第三，`表现对象的生命周期和实体生命周期也不是一回事`。

Representation 子系统会：

- spawn actors
- recycle actors
- pool actors

这说明表现对象本身就有独立的资源和生命周期管理问题。

所以 Unreal 在这件事上的工程立场非常鲜明：

`Actor 世界继续存在，但它不再是高规模仿真数据的唯一真身；它更像一层按 LOD 和可见性动态接上的表示外壳。`

## Unity 和 Unreal 在这件事上的共同收敛

虽然两边的语言完全不同，但它们最后其实收敛到了同一个结构结论。

如果先压成一张最小对照表，我会这样记：

| 对照维度 | Unity DOTS | Unreal Mass | 自研第一版 |
| --- | --- | --- | --- |
| 仿真层主世界 | `Entities / archetype / chunk / query` | `MassEntity / archetype / query / processor` | `World / archetype / chunk / query` |
| 表示层接入方式 | `Entities Graphics` 桥到现有渲染；部分组件走 companion | `Mass Representation` 在 Actor / ISM / Off 之间切换 | `Representation Bridge` 桥到外部 render/object world |
| 表示粒度 | entity -> batch / companion object / none | entity -> hi-res actor / low-res actor / ISM / none | entity -> visual object / instance / none |
| 关键限制 | managed companion 不能 Burst，需主线程 | representation、simulation、replication 各有自己的 LOD / lifecycle | 第一版不要把表现对象塞回仿真内核 |
| 暴露的边界 | ECS 渲染不是新管线，而是桥 | 表示层、仿真层、复制层直接分开 | 一开始就显式做桥，而不是偷着耦合 |

这张表最重要的地方，不是"它们完全等价"。

而是你会更容易看到一个更稳的结论：

`表示层不是仿真层的皮肤，而是另一套受可见性、LOD、对象生命周期、线程约束和引擎对象体系支配的世界。`

## 为什么仿真层和表示层很难彻底合并

到这里，才能正面回答这篇的主问题。

它们之所以很难彻底合并，至少有四个更稳定的原因。

### 1. 更新目标不同

仿真层首先在乎：

- 规则是否正确
- 结构是否稳定
- query 是否命中
- scheduler 是否可控

表示层首先在乎：

- 看不看得见
- 用哪一种表现类型最划算
- 当前材质、网格、灯光、VFX、Volume、相机约束是什么

这两层关注点天然不同。

### 2. 身份粒度不同

仿真层往往按 entity 身份思考。

表示层却可能按：

- actor
- renderer
- batch
- ISM instance group
- pooled object

来思考。

所以它们天生就不保证一一对应。

### 3. 线程与执行约束不同

这一点 Unity 文档已经把边界写得非常直接了：

- 只要你跨进 managed companion component
- 就不能 Burst
- 也必须回到主线程

Unreal 那边虽然表达方式不同，但 high-res actor、low-res actor、ISM、pooling、visual LOD 这些词本身也说明：

`表示层天然要和更重的引擎对象体系协作。`

这意味着它不太可能完全沿用仿真层那种"纯 chunk 批处理 + 线程友好"的成本模型。

### 4. 生命周期不同

仿真层的 entity 生命周期，通常跟规则和状态有关。

表示层的对象生命周期，还会受：

- 可见性
- LOD
- pooling
- streaming
- camera / level / renderer state

影响。

所以它们很难完全绑死成同一条生命周期。

## 如果自己手搓，为什么第一版就该做 Representation Bridge

到这里，自研第一版的方向其实已经很清楚了。

### 1. 不要让 ECS 直接托管全部表现对象

第一版最容易犯的错误，就是偷懒写成：

- entity 里直接挂表现对象句柄
- system 到处直接改外部对象
- 仿真和表现混在同一个 phase 里随手处理

这样短期看很方便。
但很快你就会遇到：

- 读写边界脏掉
- query 和外部对象状态互相污染
- LOD / pooling / culling 很难单独演化

### 2. 第一版桥接最好先做成单向数据流

我更推荐的第一版做法是：

- 仿真层产出稳定状态
- 表示桥读取这批状态
- 再统一写给外部 render / object world

也就是说，先把最小桥接做成：

`simulation -> representation`

而不是一开始就双向乱接。

### 3. 第一版就要接受"一实体可以没有表现"

这点要尽早写进设计里。

否则你后面一做：

- culling
- LOD
- pooling
- 延迟生成

就会发现系统默认假设错了。

所以第一版 bridge 的契约最好直接允许：

- entity 对应 visual object
- entity 对应 lightweight instance
- entity 当前没有 visual representation

### 4. 表示桥最好站在 Presentation phase

这个点和前面 `04` 的执行骨架也正好接上。

更稳的做法通常是：

- simulation phase 只更新仿真数据
- structural change 在受控时机 flush
- presentation phase 再统一桥接外部表现

这样系统会更容易保持：

- 仿真内核干净
- 表示层职责清楚
- 问题更容易定位

## 常见误解

### 误解一：既然 ECS 能渲染，那表示层问题就解决了

不是。

能渲染，只说明有一条表示桥能成立。
不等于仿真层和表示层已经合成一个世界。

### 误解二：Companion Components 说明 Unity 已经完全打通 GameObject 和 ECS

也不对。

官方文档恰恰明确写了：

- 它们不享受 ECS 组件那种高性能路径
- 不能 Burst
- 要回到主线程

这更像是在提醒你边界在哪里，而不是告诉你边界消失了。

### 误解三：Unreal 的 Actor、ISM、LOD 只是视觉优化细节

不够准确。

`Mass Representation` 官方就是把这些东西作为独立子系统写出来的。
这说明它们不是"后处理小技巧"，而是实体世界如何对接默认表现世界的正式结构。

### 误解四：第一版自研先把仿真做出来，表示以后再说

可以这么做，但风险很大。

因为你会很快把很多本该在表示桥上的决定，偷偷塞回仿真内核里。
等后面再拆，代价会更大。

## 我的结论

如果一定要把这篇压成一句话，我会这样写：

> 仿真层和表示层通常不能彻底合并，不是因为"技术还不够强"，而是因为它们在关注目标、身份粒度、线程约束和生命周期上天然不同；Unity 用 `Entities Graphics + companion components` 承认这层桥，Unreal 用 `Mass Representation + Actor / ISM / LOD` 承认这层桥，自研第一版也最好一开始就做显式的 `Representation Bridge`，而不是让 ECS 内核偷偷长回对象世界。

顺着这条线继续往下走，下一篇最自然就该讲：

`如果不抄整套 DOTS / Mass，第一版最小系统到底应该做到哪里。`
