---
title: "Unity 游戏更新与发布：从首次整包到资源热更新"
slug: "unity-game-update-release-series-index"
date: "2026-10-23"
lastmod: "2026-10-23"
description: "从首次交付、线上修复和新内容上线出发，串联版本绑定、构建范围、客户端强更、资源热更与发布回退；第 1 篇已有正文，其余六篇为提纲。"
weight: 65
featured: false
draft: false
tags: ["Unity", "YooAsset", "交付工程", "热更新", "Index"]
series: "Unity 游戏更新与发布"
series_id: "unity-game-update-release"
series_role: "index"
series_order: 0
series_nav_order: 20
series_title: "Unity 游戏更新与发布"
series_entry: true
series_audience: ["Unity 客户端", "构建与发布维护者", "技术负责人"]
series_level: "入门到进阶"
series_best_for: "已经能打出游戏包，希望知道一次改动应该更新什么、如何构建，以及怎样让玩家采用正确版本。"
series_summary: "用客户端 100 到 101 的连续示例，连接业务变化、兼容策略、构建、启动更新与发布生效。第 1 篇已有正文，其余六篇已建立提纲。"
series_intro: "先从第一次上线、线上 Bug 与活动和英雄更新理解发布的原因，再设计版本映射、选择构建范围、追踪客户端更新，并核对版本真正生效的条件。"
series_reading_hint: "首次阅读按 01 到 07；已有具体任务时按问题跳转。第 1 篇先讲中文概念，其余章节明确标注提纲，正文将逐篇补齐。"
type: "reading-series"
reading_title: "Unity 游戏更新与发布"
reading_lead: "从首次整包，到每一次内容更新。"
reading_intro: "线上 Bug、新英雄与活动，都会带来新的交付决定。沿同一个游戏的持续迭代，理解该更新什么、怎样构建，以及如何让玩家用上正确版本。"
reading_status: "第 1 篇已有正文，其余 6 篇为提纲。"
reading_groups:
  - title: "先从发布场景开始"
    intro: "理解一次更新为什么发生，以及它会改变哪些内容。"
    first: 1
    last: 3
  - title: "再把工程流程接起来"
    intro: "从兼容关系走到构建、启动检查与真正上线。"
    first: 4
    last: 7
---

## 系列目录 {#chapters}

先用三种场景建立判断，再沿版本、构建、玩家更新与发布继续深入。已有具体任务时，也可以从对应章节进入。

{{< reading-series-chapters >}}

## 沿相关专题继续深入 {#related}

- [YooAsset 资源管理与交付]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}})：深入资源收集、清单、文件系统、加载、缓存和生命周期。本系列承接它的项目层问题，解释版本如何选择、整包何时升级以及资源怎样上线。
- [HybridCLR 系列]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：深入 C# 热更执行与兼容机制。本系列关心热更代码与已发客户端如何配套，以及失败时如何处理。
- [包体管理与分发]({{< relref "delivery-engineering/delivery-package-distribution-series-index.md" >}})、[脚本热更新]({{< relref "delivery-engineering/delivery-hot-update-series-index.md" >}})、[灰度上线与线上运营]({{< relref "delivery-engineering/delivery-release-operations-series-index.md" >}})：按具体机制继续深入。
