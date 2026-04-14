---
date: "2026-04-14"
title: "包体管理与分发系列索引｜首包控制、分包策略和三端分发的工程设计"
description: "给交付工程专栏 V06 补一个稳定入口：首包放什么、按需下载怎么做、三端各自的包体限制和分发策略怎么处理。"
slug: "delivery-package-distribution-series-index"
weight: 500
featured: false
tags:
  - "Delivery Engineering"
  - "Package Management"
  - "Distribution"
  - "CDN"
  - "Index"
series: "包体管理与分发"
series_id: "delivery-package-distribution"
series_role: "index"
series_order: 0
series_nav_order: 6
series_title: "包体管理与分发"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "工具链工程师"
  - "版本 / 发布负责人"
series_level: "进阶"
series_best_for: "当你想搞清楚首包该放什么、三端的包体限制差在哪、CDN 部署和热更新资源管线怎么设计"
series_summary: "从首包架构决策到三端分发策略到 CDN 部署和热更新资源管线，把包体管理从'打完包传上去'升级到工程化设计。"
series_intro: "V05 讲了资源怎么打包和加载。V06 接上来讲一个更贴近用户的问题：用户要下载多大的包才能开始玩？后续更新要下多少？三个平台各自有什么限制？CDN 怎么部署才不出事？这些问题直接影响用户留存和发布节奏。"
delivery_layer: "principle"
delivery_volume: "V06"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L5"
---

## 这组文章要解决什么

V05 解决了资源管线的内部工程（打包、加载、版本、生命周期）。V06 面向外部：包体怎么分发到用户设备上。

首包大小直接影响下载转化率。每增加 10MB，下载放弃率上升约 1-2%。三端的包体限制完全不同——iOS 有蜂窝网络下载限制、Android 有 AAB 基础模块上限、微信小游戏有 4MB 主包限制。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [包体管理的本质]({{< relref "delivery-engineering/delivery-package-distribution-01-fundamentals.md" >}}) | 首包 vs 追加 vs 热更——架构决策怎么做？ |
| 02 | [包体大小控制]({{< relref "delivery-engineering/delivery-package-distribution-02-size-control.md" >}}) | 什么进首包、什么按需下载、什么热更推送？压缩和裁剪怎么做？ |
| 03 | [Android 分发：AAB / PAD / 多渠道]({{< relref "delivery-engineering/delivery-package-distribution-03-android.md" >}}) | Google Play 的 AAB 模型、Play Asset Delivery、国内多渠道包怎么管？ |
| 04 | [iOS 分发：App Thinning / ODR]({{< relref "delivery-engineering/delivery-package-distribution-04-ios.md" >}}) | App Slicing、On-Demand Resources、蜂窝网络下载限制怎么处理？ |
| 05 | [微信小游戏分发]({{< relref "delivery-engineering/delivery-package-distribution-05-wechat.md" >}}) | 4MB 主包限制、分包策略、CDN 资源加载的结构性约束？ |
| 06 | [CDN 部署与版本管理]({{< relref "delivery-engineering/delivery-package-distribution-06-cdn.md" >}}) | CDN 发布、回滚、缓存策略、多区域同步怎么做？ |
| 07 | [热更新资源管线]({{< relref "delivery-engineering/delivery-package-distribution-07-hotupdate-resources.md" >}}) | 增量下载、一致性校验、回滚机制的完整工程设计？ |

## 推荐阅读顺序

01-02 是原理层（任何平台适用）。03-05 是三端平台专项。06-07 是运营期的部署和热更新。

如果你只做某一端，可以只读 01 + 02 + 对应平台（03/04/05）+ 06 + 07。
