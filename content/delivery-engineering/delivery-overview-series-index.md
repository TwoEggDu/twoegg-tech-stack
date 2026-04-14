---
date: "2026-04-14"
title: "交付总论系列索引｜先对齐什么是交付，再看完整链路和方法论模型"
description: "给交付工程专栏补一个稳定入口：先定义交付和发布的区别，再把交付链路、质量维度、飞轮模型和成熟度评估一次讲清。"
slug: "delivery-overview-series-index"
weight: 1
featured: true
tags:
  - "Delivery Engineering"
  - "Methodology"
  - "Index"
series: "交付总论"
series_id: "delivery-overview"
series_role: "index"
series_order: 0
series_nav_order: 1
series_title: "交付总论"
series_entry: true
series_audience:
  - "技术负责人 / 主程"
  - "版本 / 发布负责人"
  - "客户端 / 引擎开发"
  - "QA / 测试负责人"
series_level: "入门到进阶"
series_best_for: "当你想建立交付的完整认知框架，或者需要向团队解释'为什么发布不等于交付'"
series_summary: "定义交付的边界，建立交付飞轮方法论模型，给出成熟度自评方法和整套专栏的阅读导航。"
series_intro: "这组文章不讲任何具体的打包技巧或平台配置，而是先把'交付'这个概念从'发布'里拆出来，建立一套引擎无关的认知框架和方法论模型。后续 18 卷的所有术语、质量标准和阅读路径都定义在这里。"
delivery_layer: "principle"
delivery_volume: "V01"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L3"
  - "L4"
  - "L5"
---

## 这组文章要解决什么

很多团队"会开发"但"不会交付"——功能能写出来，但不能稳定地、可重复地、可验证地到达用户设备上。

这组文章先把交付的定义、边界和方法论立住，让后续所有卷的讨论有锚点。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [发布 vs 交付]({{< relref "delivery-engineering/delivery-overview-01-release-vs-delivery.md" >}}) | 发布和交付的本质区别是什么？为什么"包能打出来"不等于"产品能上线"？ |
| 02 | [交付链路全景]({{< relref "delivery-engineering/delivery-overview-02-delivery-landscape.md" >}}) | 从内容冻结到用户设备，完整链路长什么样？客户端和服务端各自走哪条路？ |
| 03 | [交付质量的四个维度]({{< relref "delivery-engineering/delivery-overview-03-four-quality-dimensions.md" >}}) | 功能、性能、稳定性、兼容性——四个维度分别在回答什么问题？ |
| 04 | [交付飞轮模型]({{< relref "delivery-engineering/delivery-overview-04-delivery-flywheel.md" >}}) | 八个环节、三道门、两个环、一个轴心——交付如何变成可持续转动的系统？ |
| 05 | [交付成熟度]({{< relref "delivery-engineering/delivery-overview-05-maturity-model.md" >}}) | 你的团队处于哪一级？从手动发布到持续交付，改进路线怎么排？ |
| 06 | [阅读地图]({{< relref "delivery-engineering/delivery-overview-06-reading-guide.md" >}}) | 19 卷怎么读？五条阅读线怎么选？不同角色从哪里切入？ |

## 推荐阅读顺序

如果你是第一次读这套专栏，按 01 → 06 的顺序走完即可。

如果你只是想快速理解方法论框架，读 01（定义）+ 04（飞轮模型）就够了。

如果你是要向团队推介这套体系，01 + 04 + 05 是最适合分享的组合。
