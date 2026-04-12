---
title: "Unity DOTS 工程实践｜系列索引"
slug: "dots-engineering-index"
date: "2026-03-28"
description: "Unity Entities 1.x 的完整工程实践，从 ECS 核心到 Baking、Jobs、Burst、边界管理。不是 API 手册，每篇聚焦一个真实的工程决策点，18 篇覆盖从第一行 ECS 代码到能处理边界问题的工程能力。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Burst"
  - "Jobs"
  - "Entities"
series: "Unity DOTS 工程实践"
series_id: "unity-dots-engineering"
series_role: "index"
series_order: 0
series_nav_order: 52
series_title: "Unity DOTS 工程实践"
series_audience:
  - "Unity 客户端"
  - "引擎 / 底层开发"
  - "ECS / 数据导向"
series_level: "进阶"
series_best_for: "当你想把 Unity DOTS 从概念变成可落地的工程实践，理解每个 API 背后的设计取舍"
series_summary: "Unity Entities 1.x 的完整工程实践：从 ECS 核心架构到 Baking、Jobs、Burst 和 OOP 边界管理"
series_intro: "这 18 篇文章处理的不是「DOTS 入门」，而是从第一行 ECS 代码到能处理真实工程边界问题的完整路径。它们假设读者已经了解 ECS 的架构哲学（DOD-00~06 层），现在需要的是可运行的代码示例和具体的工程决策依据：SystemBase 和 ISystem 该怎么选、EntityQuery 的变更检测有什么代价、IBufferElementData 什么时候比 List<T> 好、ECB 的常见踩坑在哪里。每篇文章聚焦一个工程决策点，不是 API 速查，而是让你理解为什么要这样设计。"
series_reading_hint: "建议按编号顺序读完 E01~E06（ECS 核心）再按需选读后续。如果你已经写过 DOTS 代码，可以直接跳到 E12~E14（Jobs/Burst/NativeCollection）。E16~E17（渲染接入和 OOP 边界）是最常被实际项目卡住的地方，值得单独重点阅读。"
weight: 1800
---

{{< series-directory >}}
