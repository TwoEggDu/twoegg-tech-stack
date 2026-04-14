---
date: "2026-04-14"
title: "工程基建系列索引｜看不见的基础设施决定了交付链路能不能跑起来"
description: "给交付工程专栏 V03 补一个稳定入口：先讲项目结构和编译域设计的通用原则，再落到 Unity 的 asmdef、Package 和脚本编译管线，最后覆盖依赖管理和多端条件编译。"
slug: "delivery-engineering-foundation-series-index"
weight: 200
featured: false
tags:
  - "Delivery Engineering"
  - "Engineering Foundation"
  - "Compilation"
  - "Index"
series: "工程基建"
series_id: "delivery-engineering-foundation"
series_role: "index"
series_order: 0
series_nav_order: 3
series_title: "工程基建"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "工具链工程师"
  - "技术负责人 / 主程"
series_level: "进阶"
series_best_for: "当你想搞清楚为什么构建时间越来越长、为什么编译域总是互相穿透、为什么第三方 SDK 接入总出问题"
series_summary: "把项目结构、编译管线和依赖管理这些'看不见的基础设施'讲清楚，为后续的资源管线、多端构建和 CI/CD 提供稳定的地基。"
series_intro: "这组文章不讲怎么写游戏逻辑，也不讲怎么优化性能。它讲的是更底层的东西：项目结构怎么设计才能支撑多端交付，编译管线为什么会慢、怎么加速，第三方依赖怎么管才不会在发版时出事。这些基础设施平时看不见，出问题时才发现已经来不及改。"
delivery_layer: "principle"
delivery_volume: "V03"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这组文章要解决什么

交付链路的生产段（V02）产出了内容、资源和配置。在它们进入构建段之前，需要一层工程基础设施把它们组织起来：项目结构、编译域、依赖管理、条件编译。

这层基础设施平时不会出问题。但当项目规模增长、团队人数增加、多端构建需求出现时，它会成为瓶颈或风险源。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [项目结构设计原则]({{< relref "delivery-engineering/delivery-engineering-foundation-01-project-structure.md" >}}) | 目录怎么划分、边界怎么定，才能支撑多人协作和多端构建？ |
| 02 | [编译域设计]({{< relref "delivery-engineering/delivery-engineering-foundation-02-compilation-domains.md" >}}) | 为什么要把代码分成多个编译域？域间依赖怎么管？ |
| 03 | [Unity 实践：asmdef / Package / 编译优化]({{< relref "delivery-engineering/delivery-engineering-foundation-03-unity-asmdef.md" >}}) | Unity 的 Assembly Definition 和 Package 化怎么用？编译时间怎么压？ |
| 04 | [脚本编译管线]({{< relref "delivery-engineering/delivery-engineering-foundation-04-script-compilation.md" >}}) | 从 C# 源码到运行时可执行代码，中间经过哪些步骤？IL2CPP 和 Mono 的区别在哪？ |
| 05 | [依赖管理与第三方集成]({{< relref "delivery-engineering/delivery-engineering-foundation-05-dependency-management.md" >}}) | SDK、插件、平台专属库怎么管？版本冲突怎么解？ |
| 06 | [条件编译与多端共用]({{< relref "delivery-engineering/delivery-engineering-foundation-06-conditional-compilation.md" >}}) | 一套代码怎么产出三端？平台宏、Feature Flag、编译开关怎么组织？ |

## 推荐阅读顺序

01-02 是原理层，讲任何引擎都适用的项目结构和编译域设计原则。

03-04 是 Unity 实践层，讲 Unity 的 asmdef、Package 化和脚本编译管线。

05-06 覆盖依赖管理和多端条件编译，跨原理和实践。

如果你不用 Unity，读 01 + 02 + 05 + 06 即可跳过 Unity 专属内容。
