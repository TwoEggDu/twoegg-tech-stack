---
title: "Unity DOTS Physics｜系列索引"
slug: "dots-physics-index"
date: "2026-03-28"
draft: true
description: "从 Unity Physics / Havok Physics 的世界模型、查询、事件、Baking，到 Character Controller 和调试路径，把物理系统放回 ECS 世界里重新理解。"
tags:
  - "Unity"
  - "DOTS"
  - "Physics"
  - "ECS"
  - "Index"
series: "Unity DOTS Physics"
series_id: "unity-dots-physics"
series_role: "index"
series_order: 0
series_nav_order: 53
series_title: "Unity DOTS Physics"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "引擎 / 底层开发"
  - "物理 / 仿真"
series_level: "进阶"
series_best_for: "当你想把 Unity Physics / Havok Physics 放回 ECS 世界里理解，而不是把它当成 Rigidbody 的平移版"
series_summary: "从物理世界地图、数据模型、查询、事件和 Baking，一路接到角色控制与调试排障。"
series_intro: "这组文章处理的不是“DOTS 版 Rigidbody 教程”，而是物理世界怎样接进 ECS 世界。它先把 Unity Physics / Havok Physics 在运行时里各站哪一层讲清，再拆 Collider、PhysicsBody、Query、Events、Baking 和 Character Controller 的边界，最后把这些结论收回调试与性能判断。"
series_reading_hint: "第一次读建议按 P01 → P04 建立物理世界地图，再按需进入 P06（Baking）、P05（Character Controller）和 P07（调试排障）。如果你是带着具体问题来查，可直接从 Query、Events 或 Character Controller 那几篇切入。"
weight: 2100
---

这组文章承接的是 [`Unity DOTS 工程实践｜系列索引`]({{< relref "system-design/dots-engineering-index.md" >}}) 之后最自然的一条支线。

它不再重复 ECS 基础，而是专门处理一个更容易让人混淆的主题：在 DOTS 里，物理不是 MonoBehaviour 物理系统的换皮，而是一套独立的世界模型、查询链和回写边界。

第一次系统读，建议按下面这条主线进入：

1. 先读 `P01` 和 `P02`，冻结物理世界地图和数据模型。
2. 再读 `P03`、`P04`、`P06`，建立 Query / 事件 / Baking 三条中段主线。
3. 最后读 `P05` 和 `P07`，把复杂边界和调试排障一起收束。

{{< series-directory >}}
