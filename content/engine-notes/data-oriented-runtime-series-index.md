---
date: "2026-03-28"
title: "数据导向运行时系列索引｜先看为什么引擎都在建数据导向孤岛，再看结构与调度"
description: "给 数据导向运行时补一个稳定入口：先说明数据导向运行时到底在解决什么，再列出当前全部文章。"
slug: "data-oriented-runtime-series-index"
weight: 309
featured: false
tags:
  - "ECS"
  - "Data-Oriented"
  - "Runtime"
  - "Index"
series: "数据导向运行时"
series_id: "data-oriented-runtime"
series_role: "index"
series_order: 0
series_nav_order: 160
series_title: "数据导向运行时"
series_entry: true
series_audience:
  - "引擎开发"
  - "客户端主程"
series_level: "进阶"
series_best_for: "当你想把 DOTS、Mass 和自研 ECS 放回同一类运行时问题里比较"
series_summary: "把数据导向运行时从问题动机、存储结构、结构变化、调度和表示层边界一路接起来。"
series_intro: "这组文章处理的不是“ECS 语法怎么写”，而是现代引擎为什么都会在某些高密度场景里建数据导向孤岛。它先解释问题动机，再拆 Archetype / Chunk、结构变化、调度、构建期前移和表示层边界。"
series_reading_hint: "第一次读建议从总论、数据布局和结构变化开始，再看调度、Baking 和表示层边界。"
---
{{< series-directory >}}

