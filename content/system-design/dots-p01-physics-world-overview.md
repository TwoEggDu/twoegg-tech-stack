---
title: "Unity DOTS P01｜DOTS 里的物理世界到底怎样运转"
slug: "dots-p01-physics-world-overview"
date: "2026-03-28"
draft: true
description: "DOTS 里的物理不是 Rigidbody 的平移版，而是一套挂在 ECS 调度链上的独立 Physics World。先把世界地图立住，后面的 Query、Events、Baking 和 Character Controller 才不会写成碎片。"
tags:
  - "Unity"
  - "DOTS"
  - "Physics"
  - "ECS"
  - "Havok"
  - "Burst"
  - "Jobs"
series: "Unity DOTS Physics"
primary_series: "unity-dots-physics"
series_role: "article"
series_order: 1
weight: 2101
---

很多人第一次看 DOTS Physics，脑子里会自动把它翻译成“Rigidbody 的 ECS 版”。这个翻译很方便，但也很危险，因为它会把物理理解成一堆组件字段，而不是一条独立的执行链。

DOTS 里的物理真正重要的不是“有没有 Collider”，而是**物理世界怎样接进 ECS 世界**。一旦这张地图没立住，后面你看到的 Query、Events、Baking、Character Controller 都会变成名字相似但边界不清的零件。

---

## 为什么 MonoBehaviour 物理直觉会失效

在传统 Unity 里，很多人习惯把物理理解成一个对象的附属能力：我有 `Rigidbody`、我有 `Collider`、我能挂 `OnTriggerEnter`，那物理逻辑就算成立了。

DOTS 不是这么组织的。它更像是在问：

- 物理状态该放在哪个世界里更新
- 哪些数据要连续存放，哪些数据只是表示层
- 结构变更什么时候能发生
- 结果怎样安全地回到后续系统

这就意味着，DOTS Physics 讨论的第一层不是“组件长什么样”，而是“世界怎么分、链路怎么走”。

---

## ECS World 与 Physics World 的关系

先把两个世界分开看。

`ECS World` 负责的是 Entity、System 和调度边界。它管的是“哪些系统在什么顺序里运行，哪些数据归谁处理”。

`Physics World` 负责的是物理构建、步进、导出和查询。它管的是“这一帧物理如何推进，碰撞结果如何生成，查询如何回应”。

这两个世界不是一回事，但也不是互不相干。DOTS 物理真正麻烦的地方，恰恰在于它们要同步。

可以把最小链路理解成这样：

```text
Authoring / Baking
    -> ECS 数据
    -> Physics Build
    -> Physics Step
    -> Physics Export / Events
    -> 后续 ECS 系统消费结果
```

这里最关键的不是某个 API 名字，而是职责分界：

- Baking 把编辑期信息变成运行时数据
- Build 把 ECS 数据整理成可步进的物理世界
- Step 让物理世界推进一个固定步长
- Export 和 Events 把结果暴露给后续系统

如果你把它想成“一个 System 直接改另一个 System 的数据”，就会很快把结构变更、同步点和事件消费揉成一团。

---

## Unity Physics 与 Havok Physics 各站哪一层

Unity Physics 和 Havok Physics 不是两套完全不同的问题空间。它们共享大体的 ECS 接入方式，但在目标、代价和工程边界上不等价。

简单说：

- Unity Physics 更像默认路线，强调和 ECS 的接入一致性
- Havok Physics 更像更重的一条路，强调更强的物理能力和工程取舍

这篇不需要在这里把它们讲成产品对比表，真正要留下的是判断方法：

| 维度 | 要问什么 | 本篇该怎么理解 |
|------|----------|----------------|
| 接入方式 | 它们是不是都要经过 ECS 这条主链 | 是，但不代表运行时代价一样 |
| 工程代价 | 项目愿不愿意承担更重的物理成本 | 要看场景，不是只看“更强” |
| 适用场景 | 这套物理是不是项目的主问题 | 不是主问题就别把它写太深 |

如果项目本身只是少量碰撞、少量触发、少量移动，很多时候你并不需要把物理系统写成专题主角。只有当物理本身就是仿真主线时，这套地图才值得展开。

---

## 固定步长与物理主链

DOTS Physics 最容易被写乱的地方，是把它当成“某个 System 的普通逻辑”。实际上，物理更像是固定步长里的一个独立主链。

一个比较稳的理解顺序是：

```text
先准备输入
-> 再构建 Physics World
-> 再推进一步
-> 再导出结果
-> 再让后续系统消费结果
```

这条链里有三个边界最重要。

第一，物理通常需要稳定的时间步。它不是“这一帧算一点、下一帧再算一点”的随缘逻辑，而是需要可控的推进节奏。  
第二，Build / Step / Export 不是同一件事。Build 是整理数据，Step 是推进仿真，Export 是把结果放回后续系统能读的位置。  
第三，结构变更不能随便插在中间。你一旦在错误的时间点改了 Archetype，就会把正在跑的物理链弄成不稳定状态。

这也是为什么 `EntityCommandBuffer` 在后面仍然会反复出现。它不是“为了优雅”，而是为了把结构变更放回正确的同步点。

---

## 常见误读为什么会反复出现

这部分只拆误区，不在这里展开修法。

### 误区 1：有物理组件就等于理解了 DOTS 物理

这其实只是把对象拆成了数据，并没有回答世界怎么推进、结果怎么导出、结构变化怎么避开同步点。

### 误区 2：Query 和 Physics Query 是一回事

它们只是名字像。`EntityQuery` 处理的是 ECS 数据选择，`Physics Query` 处理的是向物理世界发问。两者的代价模型和职责都不一样。

### 误区 3：只要能读到物理结果，就可以立刻改结构

不行。你读到的是结果，不代表你有权限在任何时刻改世界布局。结构变更必须放在正确的同步边界里。

这三类误读如果不先拆开，后面的 Query、Events 和 Baking 很容易被写成 API 清单，而不是工程判断。

---

## 这张地图决定后面几篇怎么读

这篇的任务到这里就应该结束了。它只负责把世界地图立住，不负责把每个零件讲完。

后面几篇的分工是：

- `P02`：Collider、PhysicsBody、PhysicsMass 的数据模型怎么拆
- `P03`：Physics Query 什么时候该用哪种
- `P04`：CollisionEvents / TriggerEvents 怎样安全回写
- `P06`：Collider Authoring 到运行时物理数据的 Baking 链
- `P05`：Character Controller 与 Kinematic 移动的边界
- `P07`：调试与性能分析怎么看

---

## 小结

DOTS 里的物理不是一堆长得像 `Rigidbody` 的组件，而是一条挂在 ECS 调度链上的独立物理主线。

先把 `ECS World`、`Physics World`、固定步长和 Build / Step / Export 这条链分清，后面的 Query、Events、Baking 和 Character Controller 才有稳定边界。

下一步应读：`DOTS-P02｜Collider、PhysicsBody、PhysicsMass：DOTS 物理数据模型怎么拆`

理由：先把组件和数据切面冻结，后面的 Query、Events、Baking 才不会反复换术语。
