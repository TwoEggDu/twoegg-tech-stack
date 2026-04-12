---
date: "2026-04-05"
title: "零·B 深度补充系列索引｜GPU 架构原理：Mali Valhall、Apple GPU 与执行模型深度"
description: "移动端优化技巧很多，但技巧背后的「为什么」才是让判断不出错的根基。这个系列专门补那些优化指南里被省略的架构原理：Mali 的 Warp 执行模型、带宽瓶颈的结构性成因、Apple GPU 的 Tile Memory 为什么能省带宽，以及这些特性如何落地到 Shader 编写和渲染管线设计上。"
slug: "zero-b-deep-series-index"
weight: 2295
featured: false
tags:
  - "GPU"
  - "架构"
  - "Mali"
  - "Apple GPU"
  - "移动端"
series: "零·B 深度补充"
series_id: "zero-b-deep"
series_role: "index"
series_order: 0
series_nav_order: 200
series_title: "零·B 深度补充"
series_entry: true
series_audience:
  - "图形程序"
  - "移动端优化"
series_level: "深度"
series_best_for: "当你想真正理解移动端 GPU 的执行模型，而不只是记住「用 mediump」「开 ASTC」这类结论"
series_summary: "补 GPU 架构原理的空缺：Mali Valhall / 5th Gen 的执行引擎与带宽模型，Apple GPU 的 Tile Memory 与 Metal 深度绑定。"
series_intro: "这个系列存在的原因很简单：大多数移动端优化文章给的是结论，不给推导过程。知道「Mali 上应该用 mediump」但不知道为什么，下次碰到边界情况还是会出错。知道「iOS 带宽效率高」但不知道 Tile Memory 和 Memoryless RT 是怎么运作的，就无法判断自己的渲染管线有没有用上这个优势。这个系列专门补这层原理。"
series_reading_hint: "每篇相对独立，按当前需要选读。如果你想系统建立 GPU 架构直觉，建议先看 Mali 篇（更通用，Warp 模型和带宽概念适用于大多数移动 GPU），再看 Apple GPU 篇（最激进的片上内存设计，反差感强）。"
---

这个系列不讲"怎么做"，讲"为什么"。

移动端优化有大量实操建议：用 mediump、开 ASTC、合并 Render Pass、减少 Overdraw……这些建议本身没有问题。但如果只记结论，碰到边界情况就会出错——比如不知道哪些变量不能用 mediump，或者不清楚合并 Render Pass 在 Mali 上和在 Apple GPU 上的原理有什么不同。

这个系列的目标是补那层原理。每篇聚焦一个 GPU 架构，从执行模型讲起，推导出优化建议背后的逻辑，而不是直接给清单。

---

## 这个系列目前覆盖什么

### [Mali 现代架构深度｜Valhall / 5th Gen 的 Execution Engine、带宽模型与移动优化含义]({{< relref "rendering/zero-b-deep-01-mali-modern-architecture.md" >}})

Mali 是移动端覆盖最广的 GPU 架构（联发科天玑全系、部分三星 Exynos 使用 Mali），但也是最容易被误判的：同一份 Shader 在 Adreno 上正常，在 Mali 上花屏或性能差异明显，根源经常不在代码逻辑，而在 Mali 的精度模型和带宽特性。

本篇覆盖：
- Midgard → Bifrost → Valhall → 5th Gen 的架构演进，标注每代的关键变化
- Valhall 的 Warp-based 超标量执行：为什么从 4 线程变成 16 线程，对 Shader 编写意味着什么
- fp16 在 Mali 上的真实含义：为什么 Adreno 上的 mediump 和 Mali 上的 mediump 行为完全不同
- Mali 的带宽瓶颈：为什么相同场景下 Mali 的 DRAM 带宽需求通常高于 Adreno
- Shader 编译时间问题：为什么低端 Mali 设备会在首次进入场景时卡顿 2-5 秒
- malioc 工具的使用方法：在不需要真机的情况下静态分析 Shader 的 fp16 利用率和带宽估算

### [Apple GPU 深度｜Tile Memory、Memoryless Render Target、Rasterization Order 与 Metal 绑定]({{< relref "rendering/zero-b-deep-02-apple-gpu.md" >}})

Apple GPU 是移动端性能天花板，但它的架构决策是最反直觉的：Tile Memory 可以被 Shader 直接读写、Depth Buffer 可以完全不分配 DRAM、透明混合可以不需要 CPU 排序。这些特性在 Metal 里有专用 API，通过 MoltenVK 跑 Vulkan 的游戏完全无法利用。

本篇覆盖：
- Apple GPU 的架构演进和与 Mali / Adreno 的根本差异
- Tile Memory（imageblock）：为什么 Deferred Shading 的 GBuffer 在 Apple GPU 上可以零 DRAM 带宽
- Memoryless Render Target：量化 Depth Buffer 和 MSAA Buffer 的带宽节省
- Rasterization Order Groups：透明混合如何不再依赖 CPU 排序
- SIMD-group 模型：Apple GPU 的 Warp size 是 32（与 NVIDIA 一致），Compute Shader 线程组对齐的意义
- Metal vs MoltenVK 的性能差距：哪些 Apple 专有优化路径在 Vulkan 路径下完全消失
- Xcode Instruments 的关键指标：如何确认 Memoryless 是否生效、Tile Bandwidth vs DRAM Bandwidth

---

## 按问题选读

- **Mali 设备花屏，但 Adreno 正常**：先看 Mali 篇的「mediump 精度行为」和「SPIR-V 驱动编译 Bug」部分，再配合 [Android Vulkan 驱动架构]({{< relref "performance/android-vulkan-driver.md" >}})
- **相同 Shader 在 Mali 上比 Adreno 慢很多**：看 Mali 篇的「带宽模型」和「fp16 ALU 吞吐」两节，用 malioc 验证 fp16 利用率
- **iOS 上帧率明显好于同规格 Android 旗舰，想知道为什么**：看 Apple GPU 篇，重点看 Tile Memory 和 Memoryless RT 的带宽节省量级
- **想在 iOS 上做 Deferred Shading 但担心带宽**：看 Apple GPU 篇的 Tile Memory / imageblock 部分和 Unity URP 的 Framebuffer Fetch 配置
- **Unity 项目切换到 Metal API，想知道实际收益**：看 Apple GPU 篇的「Metal vs MoltenVK」对比和 Xcode 验证方法

---

## 后续计划

后续可能补充的方向：

- **Adreno 架构深度**：GMEM 的工作机制、Flex Render 模式、Binning Pass 的 Vertex 两次处理开销
- **PowerVR 架构**：HSR（Hidden Surface Removal）的工作原理、为什么 PowerVR 的 Overdraw 代价和 Mali/Adreno 不同
- **移动端 GPU 通用优化原理**：把各架构共同的带宽、占用率、延迟隐藏概念统一梳理

{{< series-directory >}}
