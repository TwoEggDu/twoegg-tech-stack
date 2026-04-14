---
date: "2026-04-14"
title: "多端构建系列索引｜同一个项目怎样稳定产出三端构建产物"
description: "给交付工程专栏 V07 补一个稳定入口：先讲构建系统的通用模型，再覆盖 Unity 构建管线和 iOS / Android / 微信小游戏三端构建专项。"
slug: "delivery-multiplatform-build-series-index"
weight: 600
featured: false
tags:
  - "Delivery Engineering"
  - "Build System"
  - "Multi-platform"
  - "Index"
series: "多端构建"
series_id: "delivery-multiplatform-build"
series_role: "index"
series_order: 0
series_nav_order: 7
series_title: "多端构建"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "工具链工程师"
  - "技术负责人 / 主程"
series_level: "进阶"
series_best_for: "当你想搞清楚构建系统怎么设计、三端构建各自的坑在哪、构建时间怎么从 2 小时压到 30 分钟"
series_summary: "把构建从'点一下 Build 按钮'升级到工程化设计：输入/变换/产出/验证的四段模型，三端平台专项，构建时间优化。"
series_intro: "这组文章的核心观点是：构建系统是交付链路的中枢——上游所有生产线在这里汇聚，下游所有验证和发布从这里开始。构建不可靠，后面全部白做。"
delivery_layer: "principle"
delivery_volume: "V07"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L5"
---

## 这组文章要解决什么

V05-V06 覆盖了资源管线和包体分发。V07 聚焦构建系统本身：怎样从代码库 + 资源 + 配置生成三端可执行产物。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [构建系统的本质]({{< relref "delivery-engineering/delivery-multiplatform-build-01-fundamentals.md" >}}) | 输入/变换/产出/验证——构建的四段模型是什么？ |
| 02 | [构建配置管理]({{< relref "delivery-engineering/delivery-multiplatform-build-02-build-config.md" >}}) | Debug / Development / Release 差异怎么管？构建参数怎么版本化？ |
| 03 | [Unity 构建管线]({{< relref "delivery-engineering/delivery-multiplatform-build-03-unity-pipeline.md" >}}) | BuildPipeline / SBP / 构建后处理在 Unity 中怎么用？ |
| 04 | [iOS 构建专项]({{< relref "delivery-engineering/delivery-multiplatform-build-04-ios.md" >}}) | Xcode 工程生成、签名、Provisioning Profile、Framework 怎么处理？ |
| 05 | [Android 构建专项]({{< relref "delivery-engineering/delivery-multiplatform-build-05-android.md" >}}) | Gradle 配置、签名、minSdk / targetSdk、64 位要求怎么处理？ |
| 06 | [微信小游戏构建专项]({{< relref "delivery-engineering/delivery-multiplatform-build-06-wechat.md" >}}) | Unity WebGL 到微信小游戏的转换链路、内存限制、JS 桥接怎么处理？ |
| 07 | [构建时间优化]({{< relref "delivery-engineering/delivery-multiplatform-build-07-build-optimization.md" >}}) | 增量构建、缓存、并行、产物归档怎么做？CI 构建时间怎么从 2 小时压到 30 分钟？ |

## 推荐阅读顺序

01-02 是原理层。03 是 Unity 实践层。04-06 是三端平台专项。07 是优化。

如果你只关心某一端，读 01 + 02 + 03 + 对应平台（04/05/06）+ 07。
