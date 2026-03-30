---
title: "Unity DOTS N04｜Prediction / Rollback：为什么“多跑一遍逻辑”远远不够"
slug: "dots-n04-prediction-and-rollback"
date: "2026-03-28"
draft: true
description: "Prediction 不是把逻辑再执行一次，Rollback 也不是把世界粗暴回退。先把确定性边界、输入重放和状态修正链路冻住，才能理解为什么这条链的成本会这么高。"
tags:
  - "Unity"
  - "DOTS"
  - "NetCode"
  - "Prediction"
  - "Rollback"
  - "Determinism"
series: "Unity DOTS NetCode"
primary_series: "unity-dots-netcode"
series_role: "article"
series_order: 4
weight: 2304
---

N01 已经把 `Client World`、`Server World`、`Ghost` 和 `Authority` 的地图立住了，N02 也把 `CommandData` 这条输入链冻住了。到了 N04，真正要处理的是最容易被低估的一段：**Prediction / Rollback 不是“客户端先算一遍，错了再重来”这么简单，它要求你先划出确定性边界，再接受一次回滚可能会影响整条链的成本**。

很多团队一开始会把 Prediction 理解成“本地再跑一遍服务器逻辑”，听起来像是一个性能问题。实际上它首先是一个边界问题，其次才是性能问题。你必须知道哪些状态可重放、哪些输入可复用、哪些分支会破坏确定性，否则回滚只会变成一个越来越贵的黑箱。

---

## 为什么 Prediction 不是“多跑一遍逻辑”

Prediction 的目标不是替代服务器裁决，而是让客户端在等待权威结果时先获得反馈。它之所以难，不在于“再执行一次”本身，而在于这次执行必须满足几个前提：

- 读到的输入必须和服务端后面会消费的输入同源。
- 逻辑分支必须尽量稳定，不能依赖本地偶然状态。
- 推进结果必须能被后续权威状态对齐。

如果只是把一段逻辑原样跑两次，但输入来源不一致、分支条件不稳定、状态回写顺序不固定，那你得到的不是 Prediction，而是两份互相打架的结果。

## 先把确定性边界冻结住

Prediction 是否可用，核心不在“逻辑写得够不够短”，而在“哪些部分是可重放的”。

适合进入预测链的内容通常有：

- 基于输入的移动、转向、技能起手。
- 只依赖固定 Tick 和同步状态的推进逻辑。
- 可被权威结果覆盖的临时状态。

不适合进入预测链的内容通常有：

- 依赖浮动帧时间、随机数但未固定种子的分支。
- 本地表现层派生值。
- 会在不同平台、不同包版本上出现细微差异的非稳定逻辑。

这就是为什么 Prediction 不是先问“能不能跑”，而是先问“这段逻辑能不能在回放时得到同样的结构结果”。如果答案不稳，Rollback 只会把不稳放大。

## Rollback 的成本到底花在哪

Rollback 的成本不是单一的“回退一次”，而是三层叠加：

1. 你要恢复到一个历史状态。
2. 你要把历史之后的输入重新重放。
3. 你要重新对齐重放期间被污染的派生状态。

这意味着 Rollback 真正贵的地方，不只是 CPU 时间，还包括：

- 状态快照的保存成本。
- 输入缓存的保存成本。
- 重放后修正视觉和逻辑分歧的成本。

如果你的预测窗口越长、输入越频繁、状态分支越多，Rollback 就越容易从“局部修正”变成“整段重算”。所以这件事必须一开始就按成本设计，而不是等到抖了再补。

## 最小写法：按 Tick 保存输入和可回放状态

```csharp
public struct PredictedFrame
{
    public int Tick;
    public PlayerCommand Input;
    public float3 Position;
    public quaternion Rotation;
}

public sealed class PredictionBuffer
{
    private readonly Queue<PredictedFrame> frames = new();

    public void Save(PredictedFrame frame) => frames.Enqueue(frame);

    public void RollbackTo(int tick)
    {
        // 1. 找到 tick 对应的历史状态
        // 2. 恢复该状态
        // 3. 用后续输入重新重放
    }
}
```

这段伪代码真正想表达的是：Rollback 不是“把当前值减回去”，而是“恢复历史切片，再用可重放输入把后续链重新生成一遍”。如果没有历史输入和历史状态，Rollback 根本无从谈起。

## 常见误区

最常见的误区，是把 Prediction 当成“客户端作弊式预知”。实际上它只是本地先行模拟，最后仍然要服从服务端权威。

第二个误区，是把 Rollback 当成一个纯逻辑工具。真实项目里它会牵连状态缓存、表现层重建、动画修正和调试链。

第三个误区，是以为只要服务器逻辑和客户端逻辑代码一样，确定性就自然成立。代码一样不等于执行结果一样，平台差异、数据顺序和隐藏依赖都可能破坏回放。

## 小结

Prediction 的难点不在“多跑一遍”，而在“这遍必须可重放、可对齐、可修正”。
Rollback 的成本也不只在计算，还在快照、输入缓存和状态重建链路上。
只要确定性边界没有先冻结住，预测越多，后面的修正就越贵。

下一篇 / 后续阅读：
`DOTS-N05｜Relevancy / Prioritization / Interpolation：不同实体为什么不该被同等对待`
