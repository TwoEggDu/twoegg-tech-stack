---
date: "2026-03-28"
title: "游戏图形系统系列索引｜先看一帧画面经过了谁，再回到 API、驱动和 GPU 硬件"
description: "给 游戏图形系统补一个稳定入口：先说明一帧游戏画面从引擎到 GPU 的主链，再列出当前全部文章。"
slug: "game-graphics-stack-series-index"
weight: 109
featured: false
tags:
  - "Graphics"
  - "Engine"
  - "GPU"
  - "Index"
series: "游戏图形系统"
series_id: "game-graphics-stack"
series_role: "index"
series_order: 0
series_nav_order: 110
series_title: "游戏图形系统"
series_entry: true
series_audience:
  - "图形程序"
  - "引擎开发 / 客户端"
series_level: "入门"
series_best_for: "当你想建立一帧游戏画面从引擎、图形 API、驱动到 GPU 的总地图"
series_summary: "把一帧画面从引擎、API、驱动、GPU 硬件到跨平台差异接成一条可导航的图形主线。"
series_intro: "这组文章处理的核心问题是：一帧画面到底经过了谁，图形系统为什么既像一条流水线，又像一组跨层协作的系统。它适合用来建立总图，再把 API、驱动、桌面 / 移动 GPU 和复杂渲染特性放回这张图里。"
series_reading_hint: "围绕引擎无关原理与 Piccolo 实现案例阅读；基础概念按需查阅，相机、GPU 资源、表面着色和后处理输出已有具体专篇。"
---
当前写作与学习主线围绕 **引擎无关的原理与设计，以及 Piccolo 的具体实现** 组织，按问题选择文章定位。下面的科普目录提供按需查阅的概念地图，不要求先全部读完。

具体入口包括 Piccolo 的 [从 GLSL 到 GPU：渲染引擎、API 与系统]({{< relref "rendering/piccolo-rendering-02-shader-api-driver-gpu.md" >}})，以及 [11 相机跟随与渲染视图]({{< relref "engine-toolchain/piccolo-engine-11-camera-follow-render-view.md" >}})、[15 网格与纹理的 GPU 资源路径]({{< relref "engine-toolchain/piccolo-engine-15-mesh-texture-gpu-resources.md" >}})、[16 材质、光照与阴影]({{< relref "engine-toolchain/piccolo-engine-16-material-light-shadow-surface-color.md" >}})、[17 后处理与最终画面]({{< relref "engine-toolchain/piccolo-engine-17-post-processing-final-frame.md" >}})。分别追程序、相机、共享资源、场景颜色与最终输出；各篇说明源码、独立编译/解码/数值与运行证据，完整引擎和 GPU 捕获仍待验证。[18 编辑器拾取、创建与删除]({{< relref "engine-toolchain/piccolo-engine-18-editor-pick-create-delete.md" >}}) 则用额外的整数 ID 图说明 GPU 结果怎样服务编辑器对象选择，并核对两端状态的一致性。通用篇解释模型、接口与取舍，引用案例时区分通用约束和 Piccolo 的选择。

如果还不熟悉 Mesh、Material、Pass 和 Draw，先读 [从场景对象到一帧画面]({{< relref "rendering/piccolo-rendering-01-scene-to-frame.md" >}})，再进入这条源码路线。

Piccolo 的 [12 动画、骨骼姿态与网格蒙皮]({{< relref "engine-toolchain/piccolo-engine-12-animation-skeleton-mesh-skinning.md" >}}) 可作为动态几何案例：从 CPU 片段采样追到每实例矩阵与顶点权重，明确默认实现的混合、发布与编号边界。原骨骼/数学和 Shader 已独立检查，实际 GPU 蒙皮仍待验证。

[13 粒子发射器与 GPU 模拟]({{< relref "engine-toolchain/piccolo-engine-13-particle-emitter-gpu-simulation.md" >}}) 提供 GPU 动态状态案例：沿空闲/存活索引、计算调度、紧凑绘制与计数回读，区分 GPU 模拟与 CPU 发起 Draw；独立检查和 Shader 编译通过，设备运行待验证。

{{< series-directory >}}

