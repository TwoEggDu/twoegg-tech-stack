---
title: "Unity DOTS N01｜NetCode 世界观：Client World / Server World / Ghost 各自在解决什么"
slug: "dots-n01-netcode-world-overview"
date: "2026-03-28"
draft: true
description: "NetCode 不是网络版 ECS API，而是一套把 Client World、Server World、Ghost、Authority、Prediction 和排障路径重新划分的运行时地图。本篇先把这张地图画清，再进入后续的同步与预测细节。"
tags:
  - "Unity"
  - "DOTS"
  - "NetCode"
  - "Multiplayer"
  - "ECS"
  - "Networking"
series: "Unity DOTS NetCode"
primary_series: "unity-dots-netcode"
series_role: "article"
series_order: 1
weight: 2301
---
> 验证环境：Unity 6000.0.x · com.unity.netcode 1.4.x · com.unity.entities 1.3.x


多人同步最容易写歪的地方，不是某个 API 名字，而是把“谁在算、谁在改、谁在看”这三件事混成一件事。

在单机里，这个问题通常不会爆炸。玩家按下按钮，逻辑更新，结果立刻在本地显现，执行链看起来像一条直线。但一旦进到 DOTS NetCode，直线就会被拆成几层：客户端先采样输入，服务器负责权威裁决，Ghost 负责同步状态，Prediction 负责本地先行模拟，Interpolation 负责远端平滑显示。只要这几层没先分清，后面的 `CommandData`、`Snapshot`、`Rollback` 和排障文章都会变成一团。

这篇先不讲版本敏感 API，也不讲某个包具体按钮，而是先把 DOTS NetCode 的世界地图立住。

---

## 为什么 NetCode 很容易被看成 API 套皮

很多人第一次看 NetCode，直觉是“这就是 ECS 再加一层网络同步”。

这个直觉不完全错，但它会掩盖真正的边界问题。NetCode 里最重要的不是“多了哪些同步组件”，而是 `Client World`、`Server World`、`Ghost`、`Authority` 和 `Prediction` 这些角色分别站在哪一层，谁拥有最终裁决权，谁只是先行模拟，谁负责把状态送到远端。

如果不先画地图，后面每个词都像在讲同一件事：

- `Ghost` 像实体同步
- `Snapshot` 像快照
- `Prediction` 像客户端多算一遍
- `Rollback` 像服务器纠正客户端

这些词表面上相关，但它们解决的是不同层的问题。NetCode 真正的复杂度，不在于“有没有同步”，而在于“同步发生在哪个世界、哪条链路、哪一层权威上”。

---

## Client World 与 Server World 分别负责什么

先把最基础的世界划分说清楚。

`Server World` 负责权威状态推进。它决定某个实体当前到底处于什么状态，哪些输入被接受，哪些命中成立，哪些资源和冷却最终生效。它不是“更慢的客户端”，而是唯一能裁决最终真相的一侧。

`Client World` 不是单纯的显示层。它要做三件事：

- 采样本地输入
- 基于输入做有限度的本地预测
- 接收服务器结果并重建远端可见状态

这也是为什么 NetCode 不能被理解成“客户端只负责画画”。本地玩家的手感、输入响应和预测表现，都依赖 Client World 的主动参与。

可以把这条链简化成下面这张图：

```text
输入采样 -> Client World 先行模拟 -> 发给 Server World
            -> Server World 权威裁决 -> 回传结果
            -> Client World / 其他客户端重建可见状态
```

如果把这条链写反，很多问题都会变得难解释。比如：客户端已经看见技能起手了，但服务器还没接受；远端已经看见伤害结果了，但自己没看到施法过程；本地对象先播了动画，结果服务器后来判无效。这些都不是“网络延迟”一个词能解释完的，它们本质上是世界职责没切清。

---

## Ghost 在链路里站什么位置

`Ghost` 的作用，首先不是“一个网络版 Entity”，而是“同步链路里被选中的对象模型”。

不是所有实体都值得同步，也不是所有同步都应该同步完整状态。Ghost 的存在，本质上是在回答一个更工程化的问题：哪些实体需要从 Server World 进入客户端可见世界，哪些字段值得跨端传播，哪些只是本地表现层要自己重建。

这里最容易犯的错误，是把 Ghost 理解成“只要同步，就把实体都 Ghost 化”。

这不对。同步对象本身就是预算问题。你同步的不是“一个 Entity 是否存在”，而是：

- 这个实体是否值得让远端知道
- 它的哪些状态是权威状态
- 哪些状态只是表现层的派生结果
- 哪些字段值得进入快照

也就是说，Ghost 不是终点，它只是 NetCode 世界里“这类实体需要进入复制链”的标签。

这也是为什么后面的 `Snapshot` 文章会比这里更细：这篇只负责告诉你 Ghost 站在哪，下一篇才会回答哪些字段应该被送出去、哪些不该。

---

## Authority、Prediction、Interpolation 三种角色

这三者经常被一起提，但它们不是同一层面的东西。

`Authority` 解决的是“谁说了算”。

`Prediction` 解决的是“本地为什么要先算”。

`Interpolation` 解决的是“远端为什么不能也直接预测”。

把它们放在一起看，最清晰的结论其实很简单：

- Server World 持有 Authority
- Client World 对本地输入做 Prediction
- 其他客户端对远端对象做 Interpolation

这三者之间的关系，不是“谁比谁快”，而是“谁负责最终真相，谁负责即时手感，谁负责视觉平滑”。

如果再往前一步，就会进入 `Rollback`。但这篇不展开回滚机制，只先把角色划开。因为回滚只有在你先认清权威、预测和显示三层之后才有意义；否则你只会把“重放逻辑”理解成“客户端多跑一遍而已”。

---

## 常见误读为什么会不断复发

DOTS NetCode 里最常见的误读，几乎都来自于把不同层的职责揉在一起。

第一种误读是把同步状态等同于同步所有 ECS 数据。实际项目里，真正值得进入同步链的通常只是少量关键状态，剩下的大量数据只是本地派生或表示结果。

第二种误读是把 Prediction 理解成“客户端复制一份服务器逻辑就行”。实际上，预测不是简单多跑一次，而是先行模拟、后续对齐、必要时修正的完整链路。

第三种误读是把远端显示当成本地预测的同类对象。远端对象大多数时候只需要平滑重建，它不应该承担和本地玩家一样的即时输入责任。

这三种误读如果不先拆掉，后面你会发现：

- `CommandData` 看起来像输入包，但你会把它写成状态包
- `Snapshot` 看起来像快照，但你会拿它当完整世界复制
- `Prediction` 看起来像本地手感，但你会拿它直接替代权威裁决

也就是说，术语看起来都懂了，实际边界还是错的。

---

## 这张地图决定后面几篇怎么读

这篇的作用，不是把 NetCode 一次讲完，而是给后续几篇先定住分工：

- `N03` 讲 `Snapshot` 与 `Ghost` 同步
- `N02` 讲 `CommandData` 与输入链
- `N04` 讲 `Prediction / Rollback`
- `N05` 讲 `Relevancy / Prioritization / Interpolation`
- `N06` 讲角色、投射物、技能系统怎么拆
- `N07` 讲调试与排障

如果前面这张世界地图没立稳，后面每一篇都会重新解释一遍谁负责什么，最后文章会变得又长又散。

---

## 小结

DOTS NetCode 不是把 ECS 再套一层网络壳，而是把多个世界、多个权威层和多个同步角色重新分开。

先把 `Client World`、`Server World`、`Ghost`、`Authority` 和 `Prediction` 的地图画清，后面的同步、预测和排障才有可讨论的边界。下一篇开始，我们再把 `Snapshot` 和 `Ghost` 的复制链拆细。

下一步应读：`DOTS-N03｜Snapshot 与 Ghost 同步：什么应该同步，什么根本不该发`

扩展阅读：[技能系统深度 11｜多人同步：服务器权威、预测、回滚、命中确认、冷却同步应该怎么拆]({{< relref "engine-notes/skill-system-11-multiplayer-sync.md" >}})
