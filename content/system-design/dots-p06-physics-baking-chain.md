---
title: "Unity DOTS P06｜Physics Baking：Collider Authoring 到运行时物理数据的转换链"
slug: "dots-p06-physics-baking-chain"
date: "2026-03-28"
draft: true
description: "DOTS 物理的 Baking 不是把 Authoring 对象简单搬进运行时，而是把碰撞几何、身体语义和质量模型冻结成 ECS 可消费的数据。先把转换链和边界讲清，后面的 Query、Events 和角色控制才不会把编辑期与运行期混在一起。"
tags:
  - "Unity"
  - "DOTS"
  - "Physics"
  - "Baking"
  - "ECS"
  - "Authoring"
series: "Unity DOTS Physics"
primary_series: "unity-dots-physics"
series_role: "article"
series_order: 6
weight: 2106
---

P01 已经把物理世界地图立住了，P02 把 `Collider`、`PhysicsBody`、`PhysicsMass` 的切面压住了，P03 也把 Query 的问题模型拆开了。到 P06，真正该回答的是：**Collider Authoring 这类编辑期对象，怎样在 Baking 阶段变成运行时可用的物理数据，边界又该画在哪里**。

这不是一个“把组件导入到 ECS”的格式转换问题。Baking 的价值在于把编辑器里容易变、运行时里不该变的东西提前冻结掉，让物理世界只拿到它真正需要的那一层数据。做对了，运行时链路会更稳定；做错了，编辑期脚本、资源状态和物理仿真会互相污染。

---

## 验证环境与口径

本文按 `Unity 6 / Entities 1.x / Unity Physics / Havok Physics` 的常见 Baking 口径来写。具体包版本和 API 名可能随项目不同而变化，但职责边界不变：**Authoring 负责描述意图，Baking 负责冻结数据，Runtime 负责消费结果**。

这篇不展开：

- `Physics Query` 的选型细节，由 `DOTS-P03` 继续讲。
- 命中事件回写链，由 `DOTS-P04` 继续讲。
- 角色控制和 Kinematic 边界，由 `DOTS-P05` 继续讲。

## 为什么 Baking 不是“顺手把数据塞进组件”

很多项目一开始会把 Baking 理解成“编辑器里多写一层脚本”。这会把两个完全不同的职责混在一起。

Authoring 的职责是表达意图：这个碰撞体想长什么样、这个身体是静态还是动态、这个资源在编辑器里如何被人修改。  
Baking 的职责是把这些意图转成运行时友好的、可稳定读取的数据：Blob 数据、Physics 组件、以及后续系统能直接消费的结构。

如果把这两层混掉，最典型的后果有三个。

- 编辑器状态被运行时逻辑反复读取，导致“改了场景就变逻辑”的脆弱耦合。
- 运行时状态被 Authoring 重新覆盖，导致你以为自己在改物理，实际上只是改了源数据。
- 物理链路里出现双权威，Transform、Physics 和编辑器脚本轮流抢写。

所以 Baking 的第一原则不是“尽量多搬一些字段”，而是“只把运行时真正需要的东西搬过去”。

## Authoring、Baker 和 Runtime Data 各自负责什么

可以把这条链压成三层。

| 层 | 负责什么 | 不负责什么 |
|---|---|---|
| Authoring | 人可读、可编辑的物理意图 | 运行时步进和权威裁决 |
| Baker | 把意图冻结成 ECS 运行时数据 | 每帧动态状态更新 |
| Runtime Data | 物理世界真正消费的数据 | 编辑器依赖和临时 UI 状态 |

这三层的边界一旦立住，很多工程判断就简单了。

- 形状、过滤、是否参与碰撞，属于 Authoring 输入。
- 几何体、质量模型、静态或动态语义，属于 Baking 输出。
- 速度、碰撞结果、触发事件，属于 Runtime 消费结果。

也就是说，Baking 不是“把东西做完”，而是“把运行时该知道的东西提前做完”。它最重要的价值，是把数据从可变的编辑态切换成稳定的运行态。

## 最小写法：把物理资源冻结成运行时数据

下面这段伪代码只想表达转换顺序，不在乎 API 名字是否和某个包版本完全一致。

```csharp
public class EnemyColliderAuthoring : MonoBehaviour
{
    public Vector3 Size;
    public bool IsDynamic;
}

public class EnemyColliderBaker : Baker<EnemyColliderAuthoring>
{
    public override void Bake(EnemyColliderAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // 1. 从 Authoring 读到的是编辑期意图
        // 2. 在这里把几何与身体语义冻结成运行时数据
        // 3. 运行时不再回读这个 MonoBehaviour
        AddComponent(entity, new PhysicsBodyTag
        {
            IsDynamic = authoring.IsDynamic
        });

        AddComponent(entity, new PhysicsMassTag
        {
            // 质量参数应来自可预期的配置，不应依赖帧状态
            Mass = authoring.IsDynamic ? 1.0f : 0.0f
        });

        AddComponent(entity, new PhysicsColliderTag
        {
            Size = authoring.Size
        });
    }
}
```

这段代码背后的关键不是“怎么写 Baker”，而是三个转换动作。

1. 从 Authoring 读意图。
2. 把意图转成运行时结构。
3. 让运行时只读结果，不再回头看源对象。

如果运行时还要继续回读 Authoring，Baking 就失去意义了。

## 边界与代价

Baking 这条链并不免费，它把一部分复杂度前移了。

首先，Baker 需要尽量确定性。它不应该依赖当前帧输入、随机临时状态、网络权威或者某个只有运行时才存在的单例。否则同一份场景在不同构建、不同平台、不同导入时会产出不同结果。

其次，Baking 适合处理稳定信息，不适合承载高频动态状态。你不应该把每帧都会变的速度、方向、临时受力，做成 Baking 的输入。那不是构建期该做的事。

最后，Baking 还会放大版本敏感性。包升级之后，API 名、组件名、调试入口可能变，但职责边界不应该变。写这类文章时，最好始终先说“这层负责什么”，再说“这版具体 API 长什么样”。

## 常见误区

第一个误区，是把 Baking 当成“编辑器自动帮我写好运行时逻辑”。不是。它只负责数据转换，不负责业务决策。

第二个误区，是把 Authoring 当成运行时状态来源。Authoring 是输入，不是权威。运行时应该消费 Baking 结果，而不是每帧再回看输入源。

第三个误区，是把所有和物理有关的东西都往 Baking 里塞。静态几何、身体语义和质量模型适合前移，动态响应、事件回流和查询结果不适合前移。

## 小结

P06 要你记住的不是某个 Baker 写法，而是 `Authoring -> Baking -> Runtime Data` 这条链的职责切分。

Baking 的目标，是把稳定的物理意图提前冻结成运行时可消费的数据，把动态状态留给仿真链自己处理。

下一步应读：`DOTS-P07｜DOTS Physics 调试与性能分析：Broadphase、接触对、固定步长抖动怎么看`

理由：先把转换链立住，再去看调试和性能，才能区分是烘焙阶段的问题，还是运行时仿真链的问题。
