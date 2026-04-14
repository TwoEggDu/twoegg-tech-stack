---
date: "2026-03-28"
title: "Unity 裁剪系列索引｜先判断哪层在裁，再看代码模式、保留手段和实战"
description: "给 Unity 裁剪补一个稳定入口：先说明裁剪到底发生在哪几层，再列出当前全部文章。"
slug: "unity-stripping-series-index"
weight: 48
featured: false
tags:
  - "Unity"
  - "Stripping"
  - "IL2CPP"
  - "Index"
series: "Unity 裁剪"
series_id: "unity-stripping"
series_role: "index"
series_order: 0
series_nav_order: 80
series_title: "Unity 裁剪"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "构建 / 发布"
series_level: "进阶"
series_best_for: "当你想把 Unity 裁剪从 managed code、engine code、反射缺口到 link.xml / Preserve 的边界看清"
series_summary: "把 Unity 裁剪拆回 managed、engine、反射和保留手段几条边界，不再靠经验硬试。"
series_intro: "这组文章关心的是 Unity 的裁剪到底分几层、各自根据什么证据工作，以及为什么反射、泛型、字符串路径和资源挂载总会把问题密度抬高。它不是保命清单，而是先把裁剪判断拉回结构化问题。"
series_reading_hint: '第一次读建议先看"到底分几层"和 managed stripping 级别，再去看反射缺口、友好代码模式和 link.xml / Preserve 实战。'
---
{{< series-directory >}}

