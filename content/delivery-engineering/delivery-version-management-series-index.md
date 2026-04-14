---
date: "2026-04-14"
title: "版本与分支管理系列索引｜多端交付的节奏谁来控制"
description: "给交付工程专栏 V04 补一个稳定入口：版本号怎么设计、分支策略怎么选、内容冻结怎么定、Feature Flag 怎么管、变更影响怎么追。"
slug: "delivery-version-management-series-index"
weight: 300
featured: false
tags:
  - "Delivery Engineering"
  - "Version Management"
  - "Branch Strategy"
  - "Index"
series: "版本与分支管理"
series_id: "delivery-version-management"
series_role: "index"
series_order: 0
series_nav_order: 4
series_title: "版本与分支管理"
series_entry: true
series_audience:
  - "技术负责人 / 主程"
  - "版本 / 发布负责人"
  - "客户端 / 引擎开发"
series_level: "进阶"
series_best_for: "当你想搞清楚多端版本号该不该统一、分支策略怎么选、内容冻结窗口怎么设、变更影响怎么追踪"
series_summary: "把版本管理从'起个版本号、建个分支'升级到多端交付的节奏控制系统：版本号设计、分支策略、冻结窗口、环境配置和变更追踪。"
series_intro: "这组文章不讲 Git 命令怎么用，而是讲版本管理在交付链路中的工程角色。版本号不只是一个数字——它决定了客户端和服务端的兼容范围。分支策略不只是一个流程——它决定了多团队协作的冲突频率和发布节奏。"
delivery_layer: "principle"
delivery_volume: "V04"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L3"
  - "L4"
---

## 这组文章要解决什么

V02 和 V03 解决了"生产什么"和"怎么组织"。V04 解决的是"怎么控制节奏"——什么时候冻结、什么时候合入、什么时候发布、出了问题怎么追踪。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [版本号设计]({{< relref "delivery-engineering/delivery-version-management-01-versioning.md" >}}) | Major.Minor.Patch.Build 各段的含义？三端版本号统一还是独立？ |
| 02 | [分支策略]({{< relref "delivery-engineering/delivery-version-management-02-branching.md" >}}) | Main/Develop/Release/Hotfix 各自的角色？多团队怎么避免分支冲突？ |
| 03 | [内容冻结与发布列车]({{< relref "delivery-engineering/delivery-version-management-03-content-freeze.md" >}}) | 什么时候停止接受新功能？时间驱动还是内容驱动？冻结期间的例外怎么处理？ |
| 04 | [环境配置与 Feature Flag]({{< relref "delivery-engineering/delivery-version-management-04-environments-and-flags.md" >}}) | 开发/测试/预发布/生产环境怎么切？Feature Flag 怎么管理生命周期？ |
| 05 | [变更追踪与影响分析]({{< relref "delivery-engineering/delivery-version-management-05-change-tracking.md" >}}) | 一次提交影响哪些端、哪些包？怎样在合入前就知道变更的影响范围？ |

## 推荐阅读顺序

按 01 → 05 顺序读。01-02 定义版本和分支的基本框架，03-04 讲节奏控制，05 讲变更追踪。

如果你只关心分支策略（刚接手多团队项目），直接读 02 + 03。
