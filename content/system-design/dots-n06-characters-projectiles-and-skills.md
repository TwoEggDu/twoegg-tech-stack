---
title: "Unity DOTS N06｜Character、Projectile、技能系统：三类高频对象在 NetCode 下怎么拆"
slug: "dots-n06-characters-projectiles-and-skills"
date: "2026-03-28"
draft: true
description: "多人同步最容易出问题的往往不是大框架，而是 Character、Projectile 和技能系统这三类高频对象。先把仿真、表现和同步责任拆开，NetCode 才不会被对象类型拖乱。"
tags:
  - "Unity"
  - "DOTS"
  - "NetCode"
  - "Multiplayer"
  - "ECS"
  - "Prediction"
series: "Unity DOTS NetCode"
primary_series: "unity-dots-netcode"
series_role: "article"
series_order: 6
weight: 2306
---
> 验证环境：Unity 6000.0.x · com.unity.netcode 1.4.x · com.unity.entities 1.3.x


前面几篇已经把 NetCode 的世界地图、输入链和同步边界冻住了。到了 N06，问题会从“概念怎么分”变成“项目里最容易出事的对象到底该怎么拆”：Character、Projectile 和技能系统。

这三类对象之所以反复出问题，不是因为它们 API 特别难，而是因为它们都同时踩着三条线：一条是仿真，一条是表现，一条是同步。只要这三条线没先分开，项目最后就会把一个对象写成三份职责，谁都能碰，谁都说不清边界。

---

## 为什么痛点总集中在这三类对象

Character、Projectile 和技能系统有一个共同点：它们都高频、短反馈、强交互。

Character 直接承载玩家输入和体感，任何抖动、回滚、错位都会立刻被玩家察觉。Projectile 数量通常更多，生命周期更短，既要省同步预算，又要保证命中逻辑可追踪。技能系统最麻烦，因为它往往同时包含输入、前摇、判定、命中、冷却、表现和回放，天然跨越多层。

也就是说，这三类对象不是“复杂度高”这么简单，而是它们最容易把三种职责糊成一锅：

- 仿真状态到底是谁算。
- 表现效果到底谁负责播。
- 哪些字段值得进入同步链。

如果不先拆，这三类对象会把 NetCode 的所有边界问题一次性放大。

---

## Character 不是一个对象，而是一组职责

Character 最容易被写成“一个能动的实体”，但在 NetCode 里它更像一组责任集合。

本地玩家的 Character 需要负责输入采样、预测推进和本地反馈。远端玩家的 Character 需要负责权威状态重建和视觉平滑。服务端的 Character 负责最终裁决、碰撞、命中和规则结果。

如果把这三种角色混在一个实现里，就会出现典型问题：

- 本地 Character 直接拿远端状态改表现。
- 远端 Character 也跑完整输入链。
- 服务端 Character 同时承担显示和裁决。

更稳的切法是把 Character 拆成三层：

- `Simulation Layer`：移动、碰撞、状态推进、规则结果。
- `Representation Layer`：动画、骨骼、镜头、特效、UI。
- `Sync Contract`：哪些字段进入 Snapshot，哪些字段只在本地存在。

这不是抽象口号，而是能直接减少回滚噪音的分层方式。

---

## Projectile 为什么最容易把预算打爆

Projectile 看起来只是“一个飞出去的东西”，但在多人场景里，它通常会同时消耗同步预算、命中预算和表现预算。

如果每个投射物都完整同步位置、旋转、速度、生命周期和视觉状态，网络开销会迅速膨胀。如果只同步外观，不同步权威轨迹，命中判断又会失真。真正稳的做法，是把投射物按用途分流：

- 需要权威命中的，保留最小仿真状态和必要 Tick。
- 只负责视觉展示的，留在本地表现层。
- 一次性事件型的，尽量转成事件语义，不做全量实体复制。

Projectile 最重要的工程判断不是“能不能同步”，而是“要不要同步成完整实体”。很多项目的正确答案其实是：只同步权威轨迹，不同步完整视觉对象。

---

## 技能系统为什么最容易把层级揉碎

技能系统通常是三类对象里最难拆的，因为它天然就是多阶段链路。

一次技能可能包含：

- 输入触发。
- 前摇或蓄力。
- 权威判定。
- 命中反馈。
- 冷却推进。
- 表现层播放。

如果把这些阶段全塞进一个系统，代码会看起来“很完整”，但实际上每一层都在抢同一个对象的控制权。更稳的做法是把技能拆成三段：

- 仿真段负责状态机、冷却、资源和权威结果。
- 表现段负责动画、音效、VFX 和 UI。
- 同步段只负责把必要的状态和事件送到正确的一侧。

在 DOTS 里，技能最常见的错误不是逻辑不全，而是逻辑太全，导致每个阶段都以为自己有最终解释权。

---

## 最小拆分法长什么样

下面这段伪代码不是给某个包写死的实现，而是说明职责应该怎么切：

```csharp
public struct SkillRequest : IComponentData
{
    public int Tick;
    public int SkillId;
    public float2 Aim;
}

public struct SkillState : IComponentData
{
    public int CooldownTick;
    public byte Phase;
}

public partial struct SkillSimulationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 读取输入链中的 SkillRequest
        // 在 Server World 做权威判定
        // 只产出可以同步的结果和事件
    }
}

public partial struct SkillPresentationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 只消费仿真结果
        // 播放动画、特效、镜头与 UI
        // 不改权威状态
    }
}
```

这段代码的重点不是字段，而是边界：

- `SkillRequest` 是输入，不是结果。
- `SkillState` 是仿真状态，不是表现状态。
- `PresentationSystem` 只能消费结果，不能反向改权威。

只要这三层站稳，Character、Projectile 和技能系统就不会在同一层里互相打架。

---

## 常见错误拆法

第一种错误，是把 Character 当成单一实体，把输入、动画、命中和同步都塞进去。

第二种错误，是把 Projectile 当成完整网络对象，所有视觉字段都一起同步。

第三种错误，是把技能写成“一个大状态机”，然后在状态机里同时处理权威裁决和表现播放。

这些写法短期都能跑，但它们都会把同步成本、回滚成本和排障成本一起抬高。N06 要做的不是教你更多 API，而是让你在对象级别先做正确的职责分流。

---

## 小结

Character、Projectile 和技能系统之所以最容易翻车，是因为它们都同时跨了仿真、表现和同步三层。

最稳的切法不是把对象写得更大，而是把责任切得更清：仿真只管权威结果，表现只管视觉反馈，同步只管最小必要状态。

下一步应读：`DOTS-N07｜NetCode 调试与排障：延迟、抖动、错位、回滚尖峰怎么定位`

扩展阅读：[DOTS-N04｜Prediction / Rollback：为什么“多跑一遍逻辑”远远不够]({{< relref "system-design/dots-n04-prediction-and-rollback.md" >}})
