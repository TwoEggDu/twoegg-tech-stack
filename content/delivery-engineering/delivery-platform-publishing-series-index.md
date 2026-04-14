---
date: "2026-04-14"
title: "平台发布系列索引｜从构建产物到用户手上的最后一段路"
description: "给交付工程专栏 V09 补一个稳定入口：发布不是上传，而是从构建产物经过审核、灰度、回滚预案到达用户的完整流程。三端各自的规则和坑。"
slug: "delivery-platform-publishing-series-index"
weight: 800
featured: false
tags:
  - "Delivery Engineering"
  - "Platform Publishing"
  - "App Store"
  - "Google Play"
  - "Index"
series: "平台发布"
series_id: "delivery-platform-publishing"
series_role: "index"
series_order: 0
series_nav_order: 9
series_title: "平台发布"
series_entry: true
series_audience:
  - "版本 / 发布负责人"
  - "客户端 / 引擎开发"
  - "工具链工程师"
series_level: "进阶"
series_best_for: "当你想搞清楚三端各自的审核规则、发布流程、紧急发布通道和常见拒审原因"
series_summary: "覆盖发布流程设计、发布节奏、iOS/Android/微信三端审核专项、主机平台认证和紧急发布处理。"
series_intro: "这组文章不讲怎么点'上传'按钮，而是讲发布在交付链路中的工程角色：发布前要确认什么、三端审核各自的规则和周期、紧急发布时的快速通道、版本回撤的条件和风险。"
delivery_layer: "principle"
delivery_volume: "V09"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L5"
---

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [发布的本质]({{< relref "delivery-engineering/delivery-platform-publishing-01-fundamentals.md" >}}) | 发布在交付链路中的位置？发布前要确认什么？ |
| 02 | [发布节奏设计]({{< relref "delivery-engineering/delivery-platform-publishing-02-release-cadence.md" >}}) | 周发布/双周/按需？三端审核周期怎么协调？ |
| 03 | [iOS 发布专项]({{< relref "delivery-engineering/delivery-platform-publishing-03-ios.md" >}}) | App Store Connect、审核要点、TestFlight、加急和回撤？ |
| 04 | [Android 发布专项]({{< relref "delivery-engineering/delivery-platform-publishing-04-android.md" >}}) | Google Play、测试轨道、Staged Rollout、国内多渠道？ |
| 05 | [微信小游戏发布专项]({{< relref "delivery-engineering/delivery-platform-publishing-05-wechat.md" >}}) | 微信审核规则、版本管理、灰度和运营约束？ |
| 06 | [主机平台发布]({{< relref "delivery-engineering/delivery-platform-publishing-06-console.md" >}}) | TRC/XR 认证要求概览和常见不通过原因？ |
| 07 | [紧急发布与版本回撤]({{< relref "delivery-engineering/delivery-platform-publishing-07-emergency.md" >}}) | 三端各自的紧急发布通道和版本回撤机制？ |
