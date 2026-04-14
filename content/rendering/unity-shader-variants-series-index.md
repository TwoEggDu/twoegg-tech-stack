---
date: "2026-03-28"
title: "Unity Shader Variant 治理系列索引｜先拆问题边界，再看收集、剔除和交付"
description: "给 Unity Shader Variant 治理补一个稳定入口：先立住 GPU 编译模型和全流程总览，再进入来源、保留、剔除、交付和排查。"
slug: "unity-shader-variants-series-index"
weight: 9
featured: false
tags:
  - "Unity"
  - "Shader Variant"
  - "Rendering"
  - "Index"
series: "Unity Shader Variant 治理"
series_id: "unity-shader-variants"
series_role: "index"
series_order: 0
series_nav_order: 40
series_title: "Unity Shader Variant 治理"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "图形 / 构建工具链"
series_level: "进阶"
series_best_for: "当你想把 Shader Variant 从存在理由、收集方式、剔除策略到 AssetBundle 交付问题一起看清"
series_summary: "把 Shader Variant 的来源、保留依据、剔除层级、交付边界和运行时命中放回一张连续链路里。"
series_intro: "这组文章处理的不是几个孤立按钮，而是一条从 GPU 编译模型、来源地图、构建保留、剔除层级、交付边界到运行时命中的完整链路。它的重点是治理链路，不是单条经验。"
series_reading_hint: '第一次读建议先看"为什么会存在"与"GPU 编译模型"，再看"全流程总览""来源地图"，然后接着看"保留机制：四方角色与六关链路""Player / AB 构建账单对比"，最后进入剔除层级、运行时命中和缺失排查。事故案例"ScriptableObject.OnEnable() 污染 pipeline asset"可在看完排查流程后阅读。'
---
{{< series-directory >}}

