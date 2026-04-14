---
date: "2026-04-14"
title: "性能与稳定性工程系列索引｜预算、度量、治理与验证的工程循环"
description: "给交付工程专栏 V14 补一个系统入口：从性能工程的本质出发，覆盖预算体系、设备分档、CPU/GPU/内存治理和 Crash 治理七篇文章。"
slug: "delivery-performance-stability-series-index"
weight: 1300
featured: false
tags:
  - "Delivery Engineering"
  - "Performance"
  - "Stability"
  - "Index"
series: "性能与稳定性工程"
series_id: "delivery-performance-stability"
series_role: "index"
series_order: 0
series_nav_order: 14
series_title: "性能与稳定性工程"
series_entry: true
series_audience:
  - "客户端/引擎开发"
  - "技术负责人/主程"
  - "技术美术"
series_level: "进阶到高级"
series_best_for: "当你想搞清楚性能工程和性能优化的区别、预算体系怎么建、设备分档怎么做、CPU/GPU/内存怎么治理、Crash 怎么收敛"
series_summary: "从性能工程的本质（预算→度量→治理→验证）到具体实践：预算体系设计、设备分档策略、CPU/GPU/内存三个维度的工程化治理、Crash 上报与收敛。"
series_intro: "这组文章不是性能优化教程——本站性能工程专栏已有 110 篇文章覆盖 CPU、GPU、内存、渲染、物理等各方向的深度技术内容。V14 从交付工程视角讲：性能预算怎么定、设备分档怎么管、性能退化怎么在 CI 中拦截、Crash 怎么系统化收敛。"
delivery_layer: "principle"
delivery_volume: "V14"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 系列导读

性能优化是被动的——出了卡顿才去查、出了 OOM 才去修。性能工程是主动的——先定预算、持续度量、自动拦截退化、发布前验证达标。

V13 讲了验证与测试体系，其中性能测试是四层验证之一。V14 把性能测试背后的完整工程体系展开：预算怎么定、设备分档怎么管理用户体验的下限、CPU/GPU/内存三个方向怎么从交付角度治理、Crash 怎么系统化上报和收敛。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [性能工程的本质]({{< relref "delivery-engineering/delivery-performance-stability-01-engineering-cycle.md" >}}) | 性能工程和性能优化的区别是什么？四阶段循环怎么转？ |
| 02 | [性能预算体系]({{< relref "delivery-engineering/delivery-performance-stability-02-budget-system.md" >}}) | 预算按什么维度拆？帧时间/内存/包体/加载各怎么分配？ |
| 03 | [设备分档]({{< relref "delivery-engineering/delivery-performance-stability-03-device-tiering.md" >}}) | 能力指纹怎么算？质量配置怎么管？动态降级怎么做？ |
| 04 | [CPU 性能工程]({{< relref "delivery-engineering/delivery-performance-stability-04-cpu.md" >}}) | GC 压力怎么管？调度怎么优化？IL2CPP 有什么特殊考量？ |
| 05 | [GPU 性能工程]({{< relref "delivery-engineering/delivery-performance-stability-05-gpu.md" >}}) | Draw Call 怎么控？带宽怎么省？移动端 TBDR 要注意什么？ |
| 06 | [内存与包体治理]({{< relref "delivery-engineering/delivery-performance-stability-06-memory.md" >}}) | 内存预算怎么分配？OOM 怎么防？资源瘦身怎么系统化？ |
| 07 | [Crash 治理]({{< relref "delivery-engineering/delivery-performance-stability-07-crash.md" >}}) | 崩溃怎么上报？符号化怎么做？IL2CPP 崩溃怎么查？ |

## 与性能工程专栏的关系

本站性能工程专栏已有 110 篇文章，覆盖游戏预算（21 篇）、设备分档（10 篇）、CPU 优化（6 篇）、GPU 优化（7 篇）等方向的完整技术深挖。

V14 不重复这些内容。V14 从交付工程视角串联——讲"预算怎么变成 CI 约束、设备分档怎么驱动质量配置、性能退化怎么在发布前拦住"。技术深挖请到性能工程专栏阅读。
