---
date: "2026-04-14"
title: "服务端版本与热更新系列索引｜前后端版本怎么协调、服务端怎么零停机更新"
description: "给交付工程专栏 V12 补一个稳定入口：前后端版本协调、零停机滚动更新、数据兼容回滚、协议版本管理与服务端热更新。"
slug: "delivery-server-versioning-series-index"
weight: 1100
featured: false
tags:
  - "Delivery Engineering"
  - "Server"
  - "Versioning"
  - "Hot Update"
  - "Index"
series: "服务端版本与热更新"
series_id: "delivery-server-versioning"
series_role: "index"
series_order: 0
series_nav_order: 12
series_title: "服务端版本与热更新"
series_entry: true
series_audience:
  - "服务端开发"
  - "客户端开发（需要理解服务端版本约束）"
  - "技术负责人 / 主程"
series_level: "进阶"
series_best_for: "当你想搞清楚服务端版本怎么管理、前后端版本怎么协调、服务端怎么不停机更新"
series_summary: "把服务端版本管理从'改了就部署'升级到工程化：客户端兼容策略、零停机滚动更新、回滚与数据兼容、协议灰度协调和三种级别的服务端热更新。"
series_intro: "V11 讲了服务端怎么部署和运维。V12 解决部署之上的版本问题——服务端不像客户端只跑一个版本，它必须同时兼容多个客户端版本。版本协调做不好，更新就是事故。"
delivery_layer: "principle"
delivery_volume: "V12"
delivery_reading_lines:
  - "L1"
  - "L3"
---

## 系列导读

客户端版本由玩家控制——不是所有人都会第一时间更新。服务端版本由团队控制——部署后立即生效。这个不对称性是所有版本协调问题的根源。

V12 回答五个核心问题：服务端怎么同时支持多个客户端版本、怎么做到零停机更新、回滚时数据兼容怎么处理、前后端协议版本怎么灰度协调、服务端能不能不重启就更新逻辑。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [服务端版本管理]({{< relref "delivery-engineering/delivery-server-versioning-01-coordination.md" >}}) | 服务端怎么同时兼容多个客户端版本？版本号怎么定义？ |
| 02 | [滚动更新]({{< relref "delivery-engineering/delivery-server-versioning-02-zero-downtime.md" >}}) | 玩家正在战斗，服务端怎么零停机更新？ |
| 03 | [服务端回滚]({{< relref "delivery-engineering/delivery-server-versioning-03-rollback.md" >}}) | 服务端回滚比客户端快，但数据兼容性怎么处理？ |
| 04 | [协议版本管理]({{< relref "delivery-engineering/delivery-server-versioning-04-protocol.md" >}}) | 前后端协议变更怎么灰度？怎么保证新旧版本同时跑？ |
| 05 | [服务端热更新]({{< relref "delivery-engineering/delivery-server-versioning-05-server-hotupdate.md" >}}) | 配置热推、代码热重载、函数级替换——三种热更新分别适合什么场景？ |

## 与其他系列的关系

V10 讲了服务端怎么构建，V11 讲了怎么部署和运维。V12 聚焦版本维度——客户端热更新在 V08 已经覆盖，本系列讲服务端这一侧的版本管理和热更新。V13 将进入 CI/CD 全链路自动化。
