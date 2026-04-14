---
title: "Unity DOTS P03｜Physics Query：Raycast、ColliderCast、DistanceQuery 什么该用哪种"
slug: "dots-p03-physics-query-selection"
date: "2026-03-28"
draft: true
description: 'Physics Query 最容易被写成"先 Raycast 再说"，但不同查询回答的是不同问题。先把 Raycast、ColliderCast 和 DistanceQuery 各自在问什么、代价差在哪、什么时候根本不该查说清楚，后面的 Events 和 Character Controller 才不会写散。'
tags:
  - "Unity"
  - "DOTS"
  - "Physics"
  - "Query"
  - "Raycast"
  - "ECS"
series: "Unity DOTS Physics"
primary_series: "unity-dots-physics"
series_role: "article"
series_order: 3
weight: 2103
---

P01 已经把 `Physics World` 的地图立住了，P02 又把 `Collider / PhysicsBody / PhysicsMass` 的数据切面压住了。到 P03，最容易出错的地方终于浮上来了：**向物理世界发问时，到底该问什么，为什么不能一上来先 Raycast 再说**。

很多 DOTS Physics 代码之所以越写越散，不是因为查询 API 太多，而是因为"问题模型"没有先被冻结。想问体积扫掠，却用了射线；想问距离和重叠，却先做命中；明明事件链能回答，却每帧自己扫一遍。P03 的任务，就是先把 Query 这层从"API 选择题"拉回"问题建模题"。

---

## 为什么 Physics Query 总会被写成"先 Raycast 再说"

Raycast 是最容易上手的查询，因为它直观、便宜、名字也最像"万能检测"。很多团队一旦需要地面检测、视线判断、前方碰撞、命中探测，第一反应都是先打一根射线。

问题在于，Raycast 只回答一类特定问题：**从一点沿一个方向发出一条线，先撞到了什么**。它并不自动等价于：

- 一个体积从 A 扫到 B 会不会撞到东西。
- 当前这个实体和周围障碍的最近距离是多少。
- 一个胖胶囊体、一个球体、一个盒体沿路径推进时会不会先被卡住。

一旦把这些不同问题都压成"打一根射线"，短期可能能跑，长期一定会积累误判、边界抖动和补丁逻辑。

## 先分清 EntityQuery 和 Physics Query

这一层必须先说清，因为它是最常见的术语混淆点。

`EntityQuery` 回答的是：**ECS 世界里哪些实体符合某些组件条件**。  
`Physics Query` 回答的是：**物理世界里几何体、距离、扫掠和命中关系是什么**。

它们经常一起出现，但职责完全不同。一个简单例子：

- 你可以先用 `EntityQuery` 选出"所有需要做地面检测的角色"。
- 再对这些角色逐个向 `Physics World` 发 `Raycast` 或 `ColliderCast`。

如果把这两层混掉，很容易在性能分析时得出错误结论：你以为"查询慢"，其实慢的是实体筛选；或者你以为"ECS 没收益"，其实问题出在物理查询类型选错了。

## Raycast、ColliderCast、DistanceQuery 分别在问什么

最稳的方式，是先把三类问题分开。

| 查询类型 | 它真正问的问题 | 典型场景 | 最常见误用 |
|---------|----------------|----------|-----------|
| `Raycast` | 一条线先打到谁 | 视线、瞄准、细长探测、地面采样 | 用它替代体积扫掠 |
| `ColliderCast` | 一个体积沿路径推进会先撞到谁 | 角色前探、胖体积移动、子弹体积扫掠 | 用它做纯距离判断 |
| `DistanceQuery` | 当前和目标之间离多近、是否重叠、最近点在哪 | 贴墙、近距离吸附、柔性避障、重叠判定 | 用它替代明确命中路径 |

这张表最重要的不是背 API 名字，而是记住：**每种 Query 在问的根本不是同一个问题**。问题问错了，后面再怎么调参数，也只是把补丁越叠越厚。

## 什么时候该选哪种

如果把选择规则再压缩一点，可以记成三句话：

- 你在问"线先打到谁"，用 `Raycast`。
- 你在问"一个体积扫过去会先碰到谁"，用 `ColliderCast`。
- 你在问"现在离多近、是否已经贴上或重叠"，用 `DistanceQuery`。

这听起来简单，但工程里最常见的错误恰恰是把"移动中的体积"偷换成"一根中心射线"。这样做在走廊、台阶、窄门和斜坡边界上最容易出错，因为真正参与碰撞的是体积，不是中心线。

反过来，把本来只是要判断"离墙还有多远"这种问题写成连续扫掠，也会白白把查询成本抬高。很多 Character Controller 的边界抖动，不是控制器本身太难，而是问错了物理问题。

## 最小写法：先确定问题模型，再决定查询批量

下面这段伪代码只想说明一个原则：**先写"我在问什么"，再决定 Query 类型**。

```csharp
public enum MovementProbeMode
{
    GroundRay,
    CapsuleSweep,
    DistanceCheck
}

public partial struct MovementProbeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 先根据角色状态选问题模型：
        // 站地检测 -> GroundRay
        // 胖体积前探 -> CapsuleSweep
        // 贴墙距离 / 重叠修正 -> DistanceCheck
        //
        // 然后再向 Physics World 发不同类型的查询，
        // 不要先固定一种 Query 再硬套所有场景。
    }
}
```

这段结构里真正重要的不是枚举，而是"问题模型先于 API 选择"。一旦先冻住问题模型，后面的查询批量化、缓存和调试都容易得多。

## Query 的真正代价不只在单次调用

很多人评估 Query 代价时，只盯着"这次 Raycast 快不快"。这只看到了最表面的一层。真正影响成本的，经常是下面三件事：

- 每帧要对多少实体做查询。
- 查询是在热循环里逐个发，还是能按明确批次组织。
- 查询结果出来以后，是否立刻触发了大量结构变更或桥接逻辑。

也就是说，单次 Query 便宜，不代表整条 Query 链便宜。最常见的坏写法，是在大量实体的每帧循环里临时决定查什么，再把返回结果立刻变成结构变更。这种代码看起来"只查了一下"，实际上把物理查询、状态判断和结构变化揉成了一锅。

## 什么时候根本不该查

P03 还要补一个很重要的判断：不是所有碰撞相关问题都该主动发 Query。

下面这些情况，常常更适合别的路径：

- 如果你要的是命中后回流逻辑，可能 `CollisionEvents / TriggerEvents` 更合适。
- 如果你已经有稳定的状态缓存，可能直接读缓存比每帧再查一遍更便宜。
- 如果你要的是离线可预处理的空间信息，可能 Baking 阶段先整理更划算。

换句话说，Physics Query 是"主动问物理世界"，但不是唯一入口。后面的 P04 和 P06 会继续解释，什么时候该靠事件链，什么时候该靠 Baking 链，而不是把一切都塞回运行时查询。

## 最容易踩的几个坑

第一个坑，是用 `Raycast` 近似所有体积移动问题。短期省事，长期最容易在边缘和斜面条件上爆雷。

第二个坑，是把 `DistanceQuery` 当成"轻量命中检测"，结果后来又不得不补一层方向、法线和扫掠逻辑。

第三个坑，是在 ECS 热循环里边筛实体边查物理边改结构。这样你最后很难知道问题出在查询本身，还是出在后续写回路径。

第四个坑，是把 `Physics Query` 和 `EntityQuery` 说成一回事。前者问的是几何关系，后者问的是 ECS 数据筛选；只要术语一混，代码和 profiling 都会跟着混。

## 小结

Physics Query 不是"从三种 API 里背答案"，而是先把你到底在问线命中、体积扫掠，还是距离与重叠说清楚。
`Raycast`、`ColliderCast` 和 `DistanceQuery` 回答的是三类不同问题，问题模型一旦问错，后面再怎么补参数都只是补丁。
只要先把 Query 和 `EntityQuery`、Events、Baking 这些邻接层分开，后面的 Character Controller、命中回流和调试链才会真正稳定。

下一步应该：`DOTS-P04｜CollisionEvents / TriggerEvents：命中事件怎样安全地回写 ECS 世界`

理由：Query 解决的是"我主动去问到了什么"，下一步就该接上"物理世界主动告诉我发生了什么"这条事件回流链。
扩展阅读：[Unity DOTS E04｜EntityQuery：为什么 ECS 的筛选方式和 MonoBehaviour 遍历完全不是一回事]({{< relref "system-design/dots-e04-entityquery.md" >}})
