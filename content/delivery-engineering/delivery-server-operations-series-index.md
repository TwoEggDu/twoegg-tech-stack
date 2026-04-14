---
date: "2026-04-14"
title: "服务端部署与运维系列索引｜从 Docker 镜像到线上稳定运行"
description: "给交付工程专栏 V11 补一个稳定入口：部署策略、容器编排、监控体系、扩缩容、数据库运维和云成本管理。"
slug: "delivery-server-operations-series-index"
weight: 1000
featured: false
tags:
  - "Delivery Engineering"
  - "Server"
  - "Operations"
  - "DevOps"
  - "Index"
series: "服务端部署与运维"
series_id: "delivery-server-operations"
series_role: "index"
series_order: 0
series_nav_order: 11
series_title: "服务端部署与运维"
series_entry: true
series_audience:
  - "服务端开发"
  - "运维 / DevOps"
  - "技术负责人 / 主程"
series_level: "进阶"
series_best_for: "当你想搞清楚服务端怎么部署、怎么监控、怎么扩缩容、数据库怎么运维、云成本怎么管"
series_summary: "把服务端从'代码写完了部署上去'升级到工程化运维：部署策略选型、K8s 编排、四支柱监控、有状态扩缩容、数据库运维和云成本管理。"
series_intro: "V10 讲了服务端怎么构建。V11 讲构建产物怎么到线上、线上怎么保持健康。服务端运维的复杂度远高于客户端——因为服务端 7×24 小时运行，任何停机都直接影响所有在线用户。"
delivery_layer: "principle"
delivery_volume: "V11"
delivery_reading_lines:
  - "L1"
  - "L3"
---

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [部署策略]({{< relref "delivery-engineering/delivery-server-operations-01-deployment.md" >}}) | 滚动更新/蓝绿/金丝雀——怎么选？零停机怎么做？ |
| 02 | [容器编排]({{< relref "delivery-engineering/delivery-server-operations-02-orchestration.md" >}}) | Docker Compose / K8s / 自建——游戏服务端的编排选型？ |
| 03 | [监控体系]({{< relref "delivery-engineering/delivery-server-operations-03-monitoring.md" >}}) | 日志/指标/追踪/告警——四支柱监控怎么建？ |
| 04 | [扩缩容]({{< relref "delivery-engineering/delivery-server-operations-04-scaling.md" >}}) | 水平扩展怎么做？有状态服务的扩展难点在哪？ |
| 05 | [数据库运维]({{< relref "delivery-engineering/delivery-server-operations-05-database.md" >}}) | 备份/迁移/分库分表/数据一致性怎么管？ |
| 06 | [云成本管理]({{< relref "delivery-engineering/delivery-server-operations-06-cost.md" >}}) | 资源规划/Spot Instance/弹性伸缩——怎么省钱？ |
