---
date: "2026-03-28"
title: "构建与调试前置系列索引｜先把 Debug/Release、语言差异和 Unity 开关分层"
description: "给构建与调试补一个稳定入口：先说明 Debug/Release 到底在改什么，再区分 C++ 与 C#/.NET 的编译链，补上断点、符号与源码映射这层调试原理，最后落到 Unity 的 Development Build、Script Debugging 和 Deep Profile。"
slug: "build-debug-prereqs-series-index"
weight: 57
featured: false
tags:
  - "Build"
  - "Debugging"
  - "Unity"
  - "C++"
  - "C#"
  - "Index"
series: "构建与调试前置"
series_id: "build-debug-prereqs"
series_role: "index"
series_order: 0
series_nav_order: 150
series_title: "构建与调试前置"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "构建 / 发布 / 排障"
series_level: "基础"
series_best_for: "当你想先把 Debug/Release、语言编译链差异和 Unity 构建开关放回同一张地图里"
series_summary: "把“调试模式”“发布模式”“Development Build”“Deep Profile”这些经常混成一团的词拆回不同层次。"
series_intro: "这组文章处理的不是某个 IDE 按钮怎么点，而是先把几条最容易被混在一起的链拆开：Debug/Release 作为工程取舍到底改了什么，C++ 和 C#/.NET 为什么不能直接套同一套直觉，调试器为什么需要断点、符号、源码映射和运行时协作，以及 Unity 里为什么并没有一个单独的“Debug 模式”。只有这张前置地图先立住，后面再去看 Player Settings、IL2CPP、Profiler、崩溃分析和性能回归，很多讨论才不会从第一句就跑偏。"
series_reading_hint: "第一次读建议按 01 → 02 → 02b → 03 顺序读；如果你已经在 Unity 项目里卡在 Development Build、Script Debugging 或 Deep Profile 的区别上，也可以直接跳到第 03 篇。"
---
{{< series-directory >}}
