---
title: "Unity DOTS NetCode｜系列索引"
slug: "dots-netcode-index"
date: "2026-03-28"
draft: true
description: "把 Client / Server World、Ghost、Snapshot、Prediction、Rollback、Relevancy 和排障路径放回一条可判断的 DOTS NetCode 主线里。"
tags:
  - "Unity"
  - "DOTS"
  - "NetCode"
  - "Multiplayer"
  - "ECS"
  - "Index"
series: "Unity DOTS NetCode"
series_id: "unity-dots-netcode"
series_role: "index"
series_order: 0
series_nav_order: 55
series_title: "Unity DOTS NetCode"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "多人同步"
  - "引擎 / 网络开发"
series_level: "深水区"
series_best_for: "当你想把 NetCode 从 Ghost API 教程拉回到世界划分、同步预算、预测与回滚边界"
series_summary: "把 Client / Server World、Ghost、Snapshot、Prediction、Rollback 和排障证据链接成一条可落地的 NetCode 主线。"
series_intro: '这组文章处理的不是"再多一个网络组件怎么配"，而是多人同步在 DOTS 里为什么会变成一套独立的世界划分、复制链、预测链和排障链。它先立住 World、Authority、Ghost、Snapshot 的地图，再讲输入链、Prediction / Rollback、同步预算、典型对象拆法和调试收束。'
series_reading_hint: "第一次读建议按 N01 → N03 → N02 建立世界与同步地图，再进入 N04（Prediction / Rollback）。N05 处理同步预算，N06 处理角色 / 投射物 / 技能三类高频对象，N07 最后统一回到故障树和排障顺序。"
weight: 2300
---

这组文章默认你已经读过 [`Unity DOTS 工程实践｜系列索引`]({{< relref "system-design/dots-engineering-index.md" >}}) 里的主线文章，知道 ECS 世界、Job 和系统组这些基础概念。

如果没有先立住 `Client World / Server World / Ghost / Snapshot / Prediction` 这几个概念，NetCode 很容易被误读成"网络版 ECS API"；这组文章的目标，就是先把地图画清，再进入真正高风险的同步与排障问题。

建议按下面这条路径进入：

1. `N01` 先立世界地图。
2. `N03` 和 `N02` 再把状态复制链和输入链分开讲清。
3. `N04` 之后进入 Prediction / Rollback，再往下收束到同步预算、对象拆法和排障。

{{< series-directory >}}
