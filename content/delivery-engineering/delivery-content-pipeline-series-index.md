---
date: "2026-04-14"
title: "内容生产与配置管线系列索引｜上游不规范，下游不可能稳定"
description: "给交付工程专栏 V02 补一个稳定入口：先把内容、资源、配置三条生产线的边界划清，再讲规范怎么定、导表怎么跑、多端差异怎么管。"
slug: "delivery-content-pipeline-series-index"
weight: 100
featured: false
tags:
  - "Delivery Engineering"
  - "Content Pipeline"
  - "Configuration"
  - "Index"
series: "内容生产与配置管线"
series_id: "delivery-content-pipeline"
series_role: "index"
series_order: 0
series_nav_order: 2
series_title: "内容生产与配置管线"
series_entry: true
series_audience:
  - "技术美术"
  - "工具链工程师"
  - "客户端 / 引擎开发"
  - "技术负责人 / 主程"
series_level: "入门到进阶"
series_best_for: "当你想搞清楚为什么'资源没问题、代码没问题、但构建出来还是有问题'——问题往往在生产线的边界和规范上"
series_summary: "把内容、资源、配置三条生产线的边界、规范、验证和多端差异管理讲清楚，为后续的资源管线和构建卷提供干净的上游。"
series_intro: "这组文章不讲 AssetBundle 怎么打包或 Addressables 怎么配置——那些是资源管线（V05）的事。这里讲的是更上游的问题：美术交付的资源格式对不对、策划填的配置表有没有错、三端的资源差异该谁管。如果这一层不干净，后面的管线再精密也会出问题。"
delivery_layer: "principle"
delivery_volume: "V02"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 这组文章要解决什么

交付链路的第一段是生产。游戏项目的生产不只有"写代码"，而是内容、资源、配置、代码四条并行的生产线。

很多交付事故的根因不在构建或发布，而在这四条生产线的某一条出了问题——资源格式不对、配置引用断了、多端资源差异没管住。

这组文章把生产段的工程问题拆开讲清楚。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [内容与资源的边界]({{< relref "delivery-engineering/delivery-content-pipeline-01-boundaries.md" >}}) | 什么是内容、什么是资源、什么是配置？三者的边界在哪？ |
| 02 | [美术资源生产管线]({{< relref "delivery-engineering/delivery-content-pipeline-02-art-production.md" >}}) | 从 DCC 工具到引擎，美术资源经过哪些环节？每个环节的质量门是什么？ |
| 03 | [资源规范与准入]({{< relref "delivery-engineering/delivery-content-pipeline-03-asset-standards.md" >}}) | 格式、命名、尺寸、面数、压缩——规范怎么定、怎么检查、怎么落地？ |
| 04 | [配置与导表管线]({{< relref "delivery-engineering/delivery-content-pipeline-04-config-pipeline.md" >}}) | Excel → 序列化 → 运行时，导表的完整链路和常见问题是什么？ |
| 05 | [配置验证]({{< relref "delivery-engineering/delivery-content-pipeline-05-config-validation.md" >}}) | 类型检查、范围检查、引用完整性——怎样在提交时就拦住配置错误？ |
| 06 | [多端资源差异管理]({{< relref "delivery-engineering/delivery-content-pipeline-06-multiplatform-assets.md" >}}) | iOS / Android / 微信小游戏在纹理格式、模型精度、音频策略上的差异怎么管？ |

## 推荐阅读顺序

按 01 → 06 顺序读。01 定义边界，02-03 覆盖资源线，04-05 覆盖配置线，06 覆盖多端差异。

如果你只关心配置管线（策划导表相关），直接读 04 + 05。

如果你只关心多端资源差异（TA 相关），直接读 01 + 06。
