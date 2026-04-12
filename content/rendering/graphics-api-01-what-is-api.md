---
title: "图形 API 01｜图形 API 是什么：OpenGL / Vulkan / Metal / DirectX 解决的共同问题"
slug: "graphics-api-01-what-is-api"
date: "2026-03-26"
description: "OpenGL、Vulkan、Metal、DirectX——它们都叫图形 API，但解决的是同一个问题：让应用程序用统一的接口驱动不同厂商的 GPU。这篇讲清楚图形 API 的定位、它在软件栈里的层次、以及不同 API 的设计哲学差异。"
weight: 700
tags:
  - "图形API"
  - "OpenGL"
  - "Vulkan"
  - "Metal"
  - "DirectX"
  - "GPU"
series: "图形 API 基础"
---
## 问题的起点

GPU 是专用硬件。NVIDIA、AMD、Intel、Apple、Qualcomm 各自设计自己的 GPU，指令集互不兼容。如果应用程序要直接操作 GPU，就必须为每款 GPU 写一套定制代码——这显然不可行。

图形 API 解决的就是这个问题：在应用程序和 GPU 驱动之间建立一份契约（Contract）。应用程序按照 API 规范调用接口，GPU 驱动负责把这些调用翻译成具体 GPU 能执行的机器指令。

这层抽象让一个游戏可以不加修改地跑在 NVIDIA RTX 4090 和 AMD RX 7900 XTX 上，只要两者都提供了正确的驱动实现。

## 软件栈层次

从上到下，图形调用经过这些层次：

```
应用程序 / 游戏
       ↓
游戏引擎图形后端（Unity Graphics Backend）
       ↓
图形 API 运行时（OpenGL / Vulkan / Metal / Direct3D）
       ↓
用户态驱动（User-mode Driver，厂商实现）
       ↓
内核态驱动（Kernel-mode Driver）
       ↓
GPU 硬件
```

每一层的职责边界很清晰：

- **图形 API 运行时**：定义接口规范，做基本的参数校验和状态跟踪
- **用户态驱动**：做实际的翻译工作——把 API 调用转为 GPU 命令流、编译 Shader、管理 GPU 内存
- **内核态驱动**：处理硬件中断、DMA 传输、进程间 GPU 资源隔离

用户态驱动是性能差异的核心来源。同一套 OpenGL 代码，在 NVIDIA 驱动和 AMD 驱动上跑出不同的帧率，根本原因在驱动实现质量不同。

## 主流图形 API 一览

| API | 创建者 | 支持平台 | 首发年代 | 抽象层次 |
|-----|--------|----------|----------|---------|
| OpenGL | Khronos Group | 跨平台（Windows/Linux/macOS） | 1992 | 高（全局状态机，驱动隐式管理）|
| Vulkan | Khronos Group | 跨平台（Windows/Linux/Android） | 2016 | 低（显式控制，零隐式开销）|
| Metal | Apple | iOS / macOS / tvOS | 2014 | 中低（显式但有 Apple 封装）|
| Direct3D 11 | Microsoft | Windows / Xbox | 2009 | 高（类 OpenGL，驱动隐式管理）|
| Direct3D 12 | Microsoft | Windows / Xbox | 2015 | 低（类 Vulkan，显式控制）|
| OpenGL ES | Khronos Group | Android / iOS（已弃用）| 2003 | 高（OpenGL 的移动端子集）|
| WebGPU | W3C | 浏览器（跨平台）| 2023 | 中（Vulkan 风格，沙箱安全限制）|

值得注意：Metal 比 Vulkan 早两年，是低开销 API 的先行者。Vulkan 的设计深受 AMD Mantle（内部 API）启发。

## 高层 API vs 低层 API

这是理解现代图形 API 演进的核心分野。

**高层 API（OpenGL / Direct3D 11）的工作方式：**

驱动替你做了大量工作。你调用 `glDrawElements`，驱动在内部悄悄完成：
- 检查当前管线状态是否有变化，如果有则重新编译或切换 Shader 变体
- 追踪所有纹理和 Buffer 的读写依赖，插入必要的同步点
- 管理 GPU 内存分配和回收
- 决定命令什么时候真正提交给 GPU

代码简单，但开销不可控。驱动在运行时发现一个新的状态组合（比如某个 Shader + 某种混合模式第一次组合），会在 `glDraw` 调用时偷偷重新编译 Shader，直接导致帧率突刺。这种现象叫 **Driver Shader Compilation Stutter**，是高层 API 的顽固问题。

**低层 API（Vulkan / Direct3D 12 / Metal）的工作方式：**

你明确告诉 GPU 所有信息：
- 这个 Render Target 加载时要 CLEAR 还是 LOAD，渲染完要 STORE 还是 DONT_CARE
- 这张纹理从 Compute Shader 写入切换到 Fragment Shader 读取，需要一个 Image Memory Barrier
- 这帧的所有 Draw Call 先录制到 Command Buffer，最后一次性提交

开销可预测，但代码量大了 5~10 倍。用 Vulkan 画一个三角形需要约 800 行 C++ 代码，OpenGL 只需要 50 行。

## 为什么游戏引擎要封装这一层

引擎不直接暴露底层 API，原因有两个：

**跨平台**：同一个游戏要跑在 PC（Direct3D 12）、主机（PS5 用 GNM/GNMX、Xbox 用 Direct3D 12）、iOS（Metal）、Android（Vulkan），引擎图形后端负责把上层统一的渲染指令翻译成各平台对应的 API 调用。

**隔离复杂度**：Vulkan 的 200+ 个接口函数、手动同步、Memory Heap 管理不是游戏逻辑应该关心的。引擎提供 `CommandBuffer.DrawMesh`、`RenderTexture.GetTemporary` 这类高层接口。

Unity 的具体实现：`Graphics.RenderMesh` 不直接调用 Vulkan。它把渲染指令写入引擎内部的渲染队列，在渲染线程由 Graphics Backend 翻译为对应 API 调用。在 Editor 或运行时可以通过 `SystemInfo.graphicsDeviceType` 查询当前后端：

```csharp
// 查询当前图形 API 后端
GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
// 可能的值：
// GraphicsDeviceType.Direct3D12
// GraphicsDeviceType.Vulkan
// GraphicsDeviceType.Metal
// GraphicsDeviceType.OpenGLES3
// GraphicsDeviceType.WebGPU（实验性）
Debug.Log($"当前图形后端：{deviceType}");
```

Player Settings 里可以手动排序各平台的 Graphics API 优先级，或关闭 Auto Graphics API 强制指定。

## WebGPU 的特殊性

WebGPU 不是 WebGL 的升级版，而是一个全新的 API，设计风格更接近 Vulkan（显式 RenderPass、CommandEncoder）。它运行在浏览器沙箱内，有额外限制：不能直接访问底层 GPU 扩展功能，Shader 必须用 WGSL（一种新的着色语言）或经过验证的 SPIR-V。

浏览器在后台把 WebGPU 调用翻译为平台原生 API（Windows 上翻译为 Direct3D 12，macOS 翻译为 Metal，Linux 翻译为 Vulkan）。

Unity WebGL 平台正在从 WebGL 2（基于 OpenGL ES 3.0）迁移到 WebGPU，主要目标是解除 WebGL 的多线程渲染限制。

## 小结

- 图形 API 是应用程序和 GPU 驱动之间的接口契约，屏蔽了硬件差异
- 软件栈：应用 → 引擎后端 → 图形 API 运行时 → 用户态驱动 → GPU
- 高层 API 驱动做更多工作，代码简单但开销不可控（Shader 重编译 Stutter）
- 低层 API 应用显式控制一切，开销可预测但代码量大
- 引擎图形后端负责把统一的渲染指令翻译到各平台 API，上层不直接接触底层调用
