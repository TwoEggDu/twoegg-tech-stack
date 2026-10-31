---
title: "底层硬件 · CPU 与内存体系｜系列索引"
slug: "hardware-cpu-index"
date: "2026-03-28"
lastmod: "2026-10-31"
description: "从 CPU 流水线到 SIMD、Cache、内存带宽和多核并行，把数据导向设计的硬件基础讲透。这 6 篇文章是 DOTS 工程、移动端优化、服务端高性能开发共同的底座。"
tags:
  - "CPU"
  - "Cache"
  - "SIMD"
  - "内存"
  - "性能基础"
  - "底层基础"
series: "底层硬件 · CPU 与内存体系"
series_id: "hardware-cpu-memory"
series_role: "index"
series_order: 0
series_nav_order: 15
series_title: "底层硬件 · CPU 与内存体系"
series_audience:
  - "引擎 / 底层开发"
  - "性能优化"
  - "Unity DOTS / Unreal Mass"
series_level: "进阶"
series_best_for: "当你想判断数据布局能否减少访存、SIMD 是否被有效利用，以及并行瓶颈来自依赖、共享写入还是带宽"
series_summary: "把 CPU 流水线、Cache 体系、SIMD、内存带宽和多核并行讲到能做工程判断"
series_intro: "这 6 篇文章从流水线、Cache、SIMD、内存带宽和多核一致性出发，解释硬件机制怎样影响数据布局与任务划分：什么访问模式适合 SoA，向量宽度为什么不等于加速比，什么时候需要隔离热点写入，以及哪些正确性约束必须由语言或 Job 依赖保证。阅读时区分架构机制、编译器支持、模型推导与设备实测，不把固定倍数、缓存行大小或某种布局当成跨平台结论。这些分析方法可用于 Unity DOTS、Unreal Mass、移动端优化和服务端性能问题。"
series_reading_hint: "理解数据导向性能时，建议按 F02（Cache 与访问局部性）→ F03（向量化条件）→ F04（延迟、带宽与流量口径）→ F05（并发正确性与共享写入）→ F06（布局取舍）阅读。F01 补充流水线与分支背景；移动端读者可重点看 F04 的共享内存预算，再到目标设备验证。"
weight: 1300
---

{{< series-directory >}}
