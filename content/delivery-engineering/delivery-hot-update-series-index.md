---
date: "2026-04-14"
title: "脚本热更新系列索引｜代码怎么在不重新安装的情况下到达设备"
description: "给交付工程专栏 V08 补一个稳定入口：先讲热更新的本质和架构选型，再落到 HybridCLR 和 DHE 的工程化，最后覆盖验证和事故模式。"
slug: "delivery-hot-update-series-index"
weight: 700
featured: false
tags:
  - "Delivery Engineering"
  - "Hot Update"
  - "HybridCLR"
  - "Index"
series: "脚本热更新"
series_id: "delivery-hot-update"
series_role: "index"
series_order: 0
series_nav_order: 8
series_title: "脚本热更新"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "工具链工程师"
  - "技术负责人 / 主程"
series_level: "进阶到高级"
series_best_for: "当你想搞清楚脚本热更新的架构选型、HybridCLR 怎么接进 CI、热更新发布前要验什么、出了什么事故该怎么排查"
series_summary: "从热更新的原理层（为什么需要、边界在哪、风险是什么）到实践层（HybridCLR / DHE 的工程化）到验证和事故模式。"
series_intro: "这组文章不是 HybridCLR 的 API 文档——那些在本站引擎工具链栏的 HybridCLR 系列（48 篇）中有完整覆盖。这里从交付工程视角讲：热更新在交付链路中的位置、架构选型的决策框架、CI 集成的工程化方法、发布前的验证策略、以及真实项目中的事故模式。"
delivery_layer: "principle"
delivery_volume: "V08"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这组文章要解决什么

V06 讲了资源热更新（不重新安装就更新资源）。V08 讲的是更进一步的脚本热更新——不重新安装就更新代码逻辑。

脚本热更新的工程复杂度远高于资源热更新：它涉及编译管线、运行时兼容性、元数据一致性和平台限制。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [脚本热更新的本质]({{< relref "delivery-engineering/delivery-hot-update-01-fundamentals.md" >}}) | 为什么需要脚本热更新？边界在哪？风险是什么？ |
| 02 | [热更新架构选型]({{< relref "delivery-engineering/delivery-hot-update-02-architecture.md" >}}) | 全解释/混合执行/差分热更/Lua vs C#——怎么选？ |
| 03 | [HybridCLR 工程化]({{< relref "delivery-engineering/delivery-hot-update-03-hybridclr.md" >}}) | GenerateAll / AOT 泛型 / 元数据 / CI 集成怎么做？ |
| 04 | [DHE 进阶]({{< relref "delivery-engineering/delivery-hot-update-04-dhe.md" >}}) | Differential Hybrid Execution 的原理、适用场景和工程约束？ |
| 05 | [热更新验证]({{< relref "delivery-engineering/delivery-hot-update-05-verification.md" >}}) | 冒烟/兼容性回归/旧包兼容新热更——发布前验什么？ |
| 06 | [热更新事故模式]({{< relref "delivery-engineering/delivery-hot-update-06-incident-patterns.md" >}}) | 依赖断裂/元数据缺失/版本不匹配——典型事故怎么排查？ |

## 与 HybridCLR 系列的关系

本站引擎工具链栏有完整的 HybridCLR 系列（48 篇），覆盖解释器原理、AOT 泛型、元数据桥接、MonoBehaviour 挂载、性能优化、加密和排障。

V08 从交付视角串联这些内容——讲"在交付链路中怎么用"，技术深挖请到 HybridCLR 系列阅读。
