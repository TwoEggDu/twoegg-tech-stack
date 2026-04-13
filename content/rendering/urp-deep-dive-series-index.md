---
date: "2026-03-28"
title: "URP 深度系列索引｜先立渲染路径与扩展边界，再进配置和实战"
description: "给 URP 深度补一个稳定入口：先说明 URP 的前置、配置、光照和扩展应该怎么读，再列出当前全部文章。"
slug: "urp-deep-dive-series-index"
weight: 1490
featured: false
tags:
  - "Unity"
  - "URP"
  - "Rendering"
  - "Index"
series: "URP 深度"
series_id: "urp-deep-dive"
series_role: "index"
series_order: 0
series_nav_order: 60
series_title: "URP 深度"
series_entry: true
series_audience:
  - "Unity 图形程序"
  - "客户端主程"
series_level: "进阶"
series_best_for: "当你想把 URP 的前置能力、配置项、光照和扩展边界按链路看清"
series_summary: "把 URP 从前置概念、配置、光照、Renderer Feature / RenderGraph 扩展，一路接到移动端配置、平台分档和线上治理。"
series_intro: "这组文章关心的不是“某个选项怎么点”，而是 URP 的渲染路径、资源组织、配置项和扩展点各自站在哪一层。只有先知道 CommandBuffer、RTHandle、渲染路径和 Renderer 的关系，后面的配置与自定义 Pass 才不容易写散。"
series_reading_hint: "第一次读 URP，建议先按前置三篇和配置主线往下看，再进入光照、扩展，最后再读移动端配置、机型分档和平台治理。"
---
这个系列从 URP 的基础概念到平台优化，按五个子组逐步深入。建议按顺序阅读。

如果你现在最关心的是设备识别、质量分级、热机降档、内容分层和线上治理这条线，可以直接先看 [机型分档专题入口｜先定分档依据，再接配置、内容、线上治理与验证]({{< relref “performance/device-tiering-series-index.md” >}})。

如果你已经知道设备会怎么分档，现在更想补的是”渲染系统本身该怎么设计、怎么量化、怎么长期保持健康”，可以继续看这条新接出来的内层主线：[渲染系统分档设计 01｜先定体验合同和预算合同：低档保什么，高档加什么]({{< relref “performance/rendering-tier-design-01-contracts.md” >}})。

## 阅读顺序

### 前置基础

1. [URP 深度前置 01｜CommandBuffer：Blit、SetRenderTarget、DrawRenderers]({{< relref “rendering/urp-pre-01-commandbuffer.md” >}})
2. [URP 深度前置 02｜RenderTexture 与 RTHandle：临时 RT、RTHandle 体系]({{< relref “rendering/urp-pre-02-rthandle.md” >}})
3. [URP 深度前置 03｜Forward、Deferred、Forward+：三条渲染路径对比]({{< relref “rendering/urp-pre-03-rendering-paths.md” >}})

### 管线配置

4. [URP 深度配置 01｜Pipeline Asset 解读：每个参数背后的渲染行为]({{< relref “rendering/urp-config-01-pipeline-asset.md” >}})
5. [URP 深度配置 02｜Universal Renderer Settings：渲染路径、Depth Priming、Native RenderPass]({{< relref “rendering/urp-config-02-renderer-settings.md” >}})
6. [URP 深度配置 03｜Camera Stack：Base Camera、Overlay Camera 与多摄像机组织]({{< relref “rendering/urp-config-03-camera-stack.md” >}})

### 光照与阴影

7. [URP 深度光照 01｜URP 光照系统：主光、附加光、Light Layer、Light Cookie]({{< relref “rendering/urp-lighting-01-lighting-system.md” >}})
8. [URP 深度光照 02｜URP Shadow 深度：Cascade 机制、Shadow Atlas、Bias 调参]({{< relref “rendering/urp-lighting-02-shadow.md” >}})
9. [URP 深度光照 03｜Ambient Occlusion：SSAO 实现原理、参数调参与移动端策略]({{< relref “rendering/urp-lighting-03-ambient-occlusion.md” >}})

### 扩展开发

10. [URP 深度扩展 01｜Renderer Feature 完整开发：从零写一个 ScriptableRendererFeature]({{< relref “rendering/urp-ext-01-renderer-feature.md” >}})
11. [URP 深度扩展 02｜RenderGraph 实战：Unity 6 的新写法]({{< relref “rendering/urp-ext-02-rendergraph.md” >}})
12. [URP 深度扩展 03｜URP 后处理扩展：Volume Framework 与自定义效果]({{< relref “rendering/urp-ext-03-postprocessing.md” >}})
13. [URP 深度扩展 04｜DrawRenderers 与 FilteringSettings：有选择地重绘物体]({{< relref “rendering/urp-ext-04-draw-renderers.md” >}})
14. [URP 深度扩展 05｜RenderDoc 调试 URP 自定义 Pass]({{< relref “rendering/urp-ext-05-renderdoc.md” >}})
15. [URP 深度扩展 06｜2022.3 → Unity 6 迁移指南：Breaking Change 与迁移策略]({{< relref “rendering/urp-ext-06-migration.md” >}})
16. [URP Shader 手写｜从骨架到完整光照：接入主光、附加光与阴影]({{< relref “rendering/urp-shader-custom-lit.md” >}})

### 平台与优化

17. [URP 深度平台 01｜移动端专项配置：为什么这么设、怎么验证]({{< relref “rendering/urp-platform-01-mobile.md” >}})
18. [URP 深度平台 02｜多平台质量分级：三档配置的工程实现]({{< relref “rendering/urp-platform-02-quality.md” >}})
19. [URP 深度平台 03｜机型分档怎样接线上：遥测回写、Remote Config、灰度与回滚]({{< relref “rendering/urp-platform-03-online-governance.md” >}})
20. [URP 深度平台 04｜热机后的质量分档：冷机、热机、长时运行与动态降档策略]({{< relref “rendering/urp-platform-04-thermal-and-dynamic-tiering.md” >}})
21. [URP 深度平台 05｜质量分档不只改 URP：资源、LOD、特效与包体怎么一起分层]({{< relref “rendering/urp-platform-05-content-tiering.md” >}})

{{< series-directory >}}
