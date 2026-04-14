---
date: "2026-04-14"
title: "服务端架构与构建系列索引｜服务端交付和客户端交付的本质差异"
description: "给交付工程专栏 V10 补一个稳定入口：服务端的技术选型、架构模式、编译管线和容器化——从交付视角看服务端怎么构建。"
slug: "delivery-server-architecture-series-index"
weight: 900
featured: false
tags:
  - "Delivery Engineering"
  - "Server"
  - "Architecture"
  - "Index"
series: "服务端架构与构建"
series_id: "delivery-server-architecture"
series_role: "index"
series_order: 0
series_nav_order: 10
series_title: "服务端架构与构建"
series_entry: true
series_audience:
  - "服务端开发"
  - "技术负责人 / 主程"
  - "全栈工程师"
series_level: "进阶"
series_best_for: "当你想从交付视角理解服务端的技术选型、构建管线和容器化，而不是从功能开发视角"
series_summary: "把服务端交付和客户端交付的差异讲清楚，覆盖架构选型（单体/微服务/Actor/ECS）、C# 服务端实践（ET Framework）、Server ECS 和容器化构建。"
series_intro: "客户端交付的核心挑战是'多端差异'和'平台审核'。服务端交付的核心挑战完全不同——它是'零停机更新'和'有状态服务的扩展'。这组文章从交付工程视角看服务端，不重复游戏后端的功能设计。"
delivery_layer: "principle"
delivery_volume: "V10"
delivery_reading_lines:
  - "L1"
  - "L3"
---

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [服务端交付与客户端交付的本质区别]({{< relref "delivery-engineering/delivery-server-architecture-01-differences.md" >}}) | 两条交付链路的差异在哪？为什么服务端交付需要单独讲？ |
| 02 | [游戏服务端架构选型]({{< relref "delivery-engineering/delivery-server-architecture-02-patterns.md" >}}) | 单体/微服务/Actor/ECS——从交付视角怎么选？ |
| 03 | [C# 服务端实践：ET Framework]({{< relref "delivery-engineering/delivery-server-architecture-03-et-framework.md" >}}) | ET 的架构、包化设计和运行时骨架从交付角度怎么理解？ |
| 04 | [Server ECS 选型]({{< relref "delivery-engineering/delivery-server-architecture-04-server-ecs.md" >}}) | 服务端为什么需要 ECS？约束和实现方案怎么选？ |
| 05 | [服务端编译与容器化]({{< relref "delivery-engineering/delivery-server-architecture-05-build-containerize.md" >}}) | .NET 构建、Docker 镜像、CI 集成怎么做？ |

## 与其他栏目的关系

本站 system-design 栏有完整的游戏后端系列（40+ 篇）、Server ECS 系列（13 篇）和 ET Framework 系列（16 篇）。V10 从交付视角串联这些内容——讲"怎样构建和交付"，功能设计请到 system-design 栏阅读。
