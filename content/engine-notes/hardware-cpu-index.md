---
title: "底层硬件 · CPU 与内存体系｜系列索引"
slug: "hardware-cpu-index"
date: "2026-03-28"
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
series_best_for: "当你想理解 cache-friendly 代码为什么快、SIMD 为什么能 4~8 倍加速、False Sharing 为什么拖垮多线程"
series_summary: "把 CPU 流水线、Cache 体系、SIMD、内存带宽和多核并行讲到能做工程判断"
series_intro: "这 6 篇文章处理的是一个被反复引用但少有人真正讲清楚的问题：数据布局怎样从硬件层面决定性能上限。它们不是 API 手册，而是从流水线分支预测、Cache Line 预取机制、SIMD 寄存器宽度、UMA 内存带宽竞争、False Sharing 这些物理约束出发，解释为什么 SoA 比 AoS 快、为什么 Burst 有那些限制、为什么多线程代码需要 alignas(64)。这些问题的答案不只服务于 Unity DOTS，同样是 Unreal Mass、移动端渲染优化和服务端高性能开发的共同底座。"
series_reading_hint: "如果你的目标是理解 Unity DOTS 的性能来源，建议从 F02（Cache）开始，再读 F03（SIMD），其余可按需选读。如果你在做移动端优化，F04（内存带宽）是最直接相关的一篇。"
weight: 1300
---

{{< series-directory >}}
