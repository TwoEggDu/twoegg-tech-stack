---
title: "Unity DOTS N05｜Relevancy / Prioritization / Interpolation：不同实体为什么不该被同等对待"
slug: "dots-n05-relevancy-prioritization-and-interpolation"
date: "2026-03-28"
draft: true
description: "网络同步不是把所有实体一股脑发出去，而是先做预算分配，再决定哪些对象值得同步、哪些对象值得优先、哪些对象只需要平滑显示。"
tags:
  - "Unity"
  - "DOTS"
  - "NetCode"
  - "Relevancy"
  - "Prioritization"
  - "Interpolation"
series: "Unity DOTS NetCode"
primary_series: "unity-dots-netcode"
series_role: "article"
series_order: 5
weight: 2305
---
> 验证环境：Unity 6000.0.x · com.unity.netcode 1.4.x · com.unity.entities 1.3.x


N01 已经把世界地图立住了，N03 讲清了 Snapshot 和 Ghost 应该同步什么，N04 则说明了 Prediction / Rollback 为什么不是简单重跑。N05 要继续往下收：**网络同步本质上是预算问题，不同实体不该被同等对待，Relevancy、Prioritization 和 Interpolation 就是把预算分到该花的地方**。

如果把所有 Ghost 都当成同一级别对象处理，你会很快撞上三个问题：带宽不够、修正频率太高、远端显示太抖。NetCode 最后不是输在“没同步”，而是输在“同步分配方式不对”。

---

## 为什么同步首先是预算问题

同步链路里最稀缺的资源通常不是代码行数，而是每帧能稳定承受的网络预算和修正预算。你不能把所有实体都看成同样重要，因为它们对玩家体验的贡献本来就不同。

例如：

- 本地玩家角色通常比远处路人更重要。
- 投射物通常比静态装饰物更重要。
- 高风险交互对象通常比纯背景对象更重要。

这不是“主观偏心”，而是同步策略必须显式承认的工程事实。预算不够时，先保留最影响玩法和判定的对象，才是正确方向。

## Relevancy 负责“发不发”

`Relevancy` 解决的是“这个实体值不值得让当前客户端知道”。

它不是简单的距离判断，而是把空间位置、可见性、交互关系、玩法相关性一起纳入筛选。一个实体离得近，不代表一定该同步；一个实体离得远，也不代表一定不该同步。

所以 Relevancy 更像一层“同步门槛”，负责先把明显不重要的对象挡在外面，避免把预算浪费在无关实体上。

## Prioritization 负责“先发谁”

当可同步对象已经筛过一遍之后，`Prioritization` 决定的是顺序。

这一步的意义在于：即使都需要同步，预算也未必够一次发完。那就必须把更关键的实体排前面，例如：

- 正在和本地玩家交互的对象。
- 会立即影响命中的对象。
- 会影响本地预测修正的对象。

优先级不是装饰字段，而是丢包、拥塞和预算紧张时真正决定体验的阀门。没有优先级，所有对象都会在同一层抢带宽，最后谁都没发好。

## Interpolation 负责“怎么显示”

`Interpolation` 的任务不是帮远端对象做预测，而是让远端显示更平滑。

远端对象通常没有本地输入权威，因此不应该像本地玩家那样走预测式前行模拟。更稳的做法，是基于历史状态做时间上的平滑过渡，让位置、旋转和动画过渡不要一跳一跳地闪。

也就是说：

- 本地玩家更需要 Prediction。
- 远端对象更需要 Interpolation。

把这两者混成一层，远端对象会被迫承担不该承担的重算成本。

## 最小写法：先筛选，再排序，再平滑显示

```csharp
public sealed class NetSyncBudget
{
    public IEnumerable<EntityId> SelectRelevant(IEnumerable<EntityId> allEntities)
    {
        // 先按交互关系、视距、玩法重要性做 relevancy 过滤
        return allEntities;
    }

    public IEnumerable<EntityId> Prioritize(IEnumerable<EntityId> relevantEntities)
    {
        // 再按本地玩家相关性、命中风险、当前可见性排优先级
        return relevantEntities;
    }

    public void InterpolateRemote(EntityState previous, EntityState current, float alpha)
    {
        // 对远端对象做平滑过渡，而不是走本地预测
    }
}
```

这段伪代码想表达的是完整链路：先决定发不发，再决定先发谁，最后决定远端怎么显示。只做最后一步而不做前两步，预算问题不会消失，只会被拖到更晚的时候爆。

## 不要把所有实体当成同一类

高频实体最怕被平均对待。角色、投射物、技能、环境对象，它们对网络的需求完全不同。

角色通常需要更高的同步优先级，因为它直接影响玩家操作反馈和命中修正。投射物通常需要更紧的时序控制，因为它和命中判定高度相关。环境对象通常可以更激进地做降频、裁剪和插值。

如果你把这些对象放在同一套同步规则里，就会出现两种极端：要么角色不够准，要么环境浪费太多预算。

## 常见误区

第一个误区，是把 Relevancy 只理解成“距离过滤”。距离只是输入之一，不是全部。

第二个误区，是把 Prioritization 误解成“谁更重要就永远更高优先级”。实际工程里优先级经常是动态变化的。

第三个误区，是把 Interpolation 和 Prediction 混成一件事。前者是平滑显示，后者是本地先行模拟，职责完全不同。

## 小结

N05 的核心不是“怎么同步更多”，而是“怎么把有限预算花在最该花的实体上”。
`Relevancy` 决定发不发，`Prioritization` 决定先发谁，`Interpolation` 决定远端怎么平滑看。
只要你还把所有实体当成同等对象，NetCode 就很难稳定。

下一篇 / 后续阅读：
`DOTS-N06｜Character、Projectile、技能系统：三类高频对象在 NetCode 下怎么拆`
