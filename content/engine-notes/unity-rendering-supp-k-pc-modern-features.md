---
title: "Unity 渲染系统补K｜PC 平台现代特性：DLSS/FSR/XeSS、VRS、DirectStorage"
slug: "unity-rendering-supp-k-pc-modern-features"
date: "2026-03-26"
description: "PC 平台近年出现了一批改变渲染架构的新技术：DLSS/FSR/XeSS 让低分辨率渲染达到高分辨率画质，VRS 降低非关键区域的 Shader 开销，DirectStorage 绕过 CPU 直接把贴图从 SSD 加载到显存。这篇讲清楚这三类技术的原理和 Unity 的支持情况。"
weight: 1600
tags:
  - "Unity"
  - "Rendering"
  - "PC"
  - "DLSS"
  - "FSR"
  - "VRS"
  - "DirectStorage"
  - "现代渲染"
series: "Unity 渲染系统"
---
PC 平台的渲染技术在过去几年经历了一轮架构级的变化。这批新技术不是对传统管线的小修小补，而是从根本上改变了"渲染分辨率与显示分辨率的关系"、"Shader 执行密度"以及"贴图数据如何从存储到达显存"这三个核心环节。

对于 Unity 开发者而言，理解这些技术的原理，是正确评估项目性能上限、选择技术方案的前提。

---

## 一、升采样技术（Upscaling）

### 核心思想

升采样的出发点很简单：在低于目标显示分辨率的内部分辨率下渲染，再通过算法把图像"放大"到目标分辨率。这比直接全分辨率渲染快，因为像素数量决定了 Fragment Shader 的执行次数——1080p 渲染 + 升到 4K，比直接 4K 渲染节省约 75% 的像素着色开销。

难点在于放大质量。朴素的双线性插值会模糊细节。现代升采样技术的核心工作，就是用不同手段"推断"出缺失的像素细节。

### DLSS（Deep Learning Super Sampling）

DLSS 是 NVIDIA 推出的神经网络驱动升采样方案，分为 DLSS 2 和 DLSS 3 两个阶段。

**DLSS 2 原理：**

- 利用 NVIDIA GPU 上的 Tensor Core 执行推断
- 输入：当前帧低分辨率图像 + 运动向量（Motion Vector）+ 历史帧数据
- 输出：高分辨率重建图像
- 神经网络在 NVIDIA 内部用大量高/低分辨率图像对离线训练，模型以驱动形式分发，游戏无需内嵌权重

**DLSS 3 新增 Frame Generation（帧生成）：**

DLSS 3 在 DLSS 2 的基础上引入了光流加速器（Optical Flow Accelerator）。它分析连续两帧之间的像素运动，生成一帧"插值帧"插入到真实渲染帧之间，从而在不增加 GPU 渲染负担的前提下提升显示帧率。Frame Generation 对延迟敏感型游戏（如 FPS）需谨慎使用，因为插值帧会引入轻微的输入延迟。

**质量模式对照：**

| 模式 | 内部渲染分辨率比例 | 适用场景 |
|------|--------------------|----------|
| Quality | ~67%（约 2/3） | 画质优先，常用于 RPG/冒险 |
| Balanced | ~58% | 画质与性能折中 |
| Performance | 50% | 性能优先，竞技游戏 |
| Ultra Performance | 33% | 极限性能，适合 8K 显示 |

**Unity 集成：** 通过官方包 `com.unity.modules.nvidia`（即 Unity DLSS 插件）集成，URP 和 HDRP 均支持。需要项目开启运动向量 Pass，并确保 Anti-Aliasing 设置为 DLSS 模式。

### FSR（FidelityFX Super Resolution）

FSR 是 AMD 的开源升采样方案，分为 FSR 1.x 和 FSR 2.x 两代，原理差异显著。

**FSR 1.0（空间升采样）：**

- 仅基于当前帧的空间信息，不使用历史帧
- 算法核心是 EASU（Edge Adaptive Spatial Upsampling）边缘自适应升采样 + RCAS（Robust Contrast Adaptive Sharpening）锐化
- 优点：无需运动向量，集成简单，支持包括 AMD/NVIDIA/Intel 在内的所有 GPU，理论上也可跑在主机和移动端
- 缺点：不积累时间信息，细节恢复能力弱于 DLSS 2，运动中的细线、发丝容易出现锯齿

**FSR 2.x（时间升采样）：**

- 引入类 TAA（Temporal Anti-Aliasing）的时间积累机制
- 需要运动向量 + 深度缓冲 + Jitter（抖动偏移）
- 质量接近 DLSS 2，在某些场景下甚至优于 DLSS 2（尤其是精细植被）
- 完全开源（MIT 许可），通过 AMD FidelityFX SDK 发布
- Unity 通过 FidelityFX SDK 的 Unity 包集成 FSR 2.x

### XeSS（Xe Super Sampling）

XeSS 是 Intel 推出的升采样方案，于 2022 年随 Arc GPU 发布。

- 在 Intel Arc GPU（搭载 XMX 矩阵加速单元）上运行神经网络推断，质量接近 DLSS 2
- 在非 Arc GPU 上回退到基于 DP4a（4路点积）指令的通用路径，质量略低但仍优于 FSR 1.0
- Unity 2022+ 通过 `com.unity.render-pipelines.core` 中的 XeSS 集成支持

**三种升采样技术横向对比：**

| 特性 | DLSS 2/3 | FSR 2.x | XeSS |
|------|----------|---------|------|
| 算法类型 | 神经网络（时间） | 时间积累 | 神经网络（时间） |
| 需要运动向量 | 是 | 是 | 是 |
| 开源 | 否 | 是 | 部分开源 |
| GPU 限制 | NVIDIA RTX | 全平台 | Intel Arc 最优，全平台可用 |
| Unity 支持 | 官方插件 | FidelityFX SDK | Unity 2022+ |
| 整体画质排名 | 顶级 | 接近 DLSS | 中等偏上 |

### 渲染分辨率选择建议

经验上，**70% 渲染分辨率**（对应 DLSS/FSR 的 Quality 模式附近）是画质与性能的常用平衡点：像素数量约为全分辨率的 49%，帧率提升明显，而重建质量在静止场景下与原生渲染差异极小。在高速运动场景（FPS 游戏、赛车）中，建议配合足够精确的运动向量以避免重影（Ghosting）。

---

## 二、Variable Rate Shading（VRS，可变速率着色）

### 原理

传统光栅化管线默认每个像素都执行一次 Fragment Shader。但屏幕上并非所有区域都值得这种精度：

- 屏幕中心（注视点）需要精细着色
- 屏幕边缘、被高频运动模糊的区域，用较低密度着色对观感影响极小
- VR 的菲涅尔视场边缘，用户根本感知不到精细细节

VRS 的思路是：允许渲染管线以"着色频率低于像素频率"的方式执行 Shader。例如 2×2 的 Shading Rate 表示一组 4 个像素只执行一次 Fragment Shader，计算量降为原来的 25%。

### Tier 1 vs Tier 2

DirectX 12 将 VRS 分为两个 Tier：

**Tier 1 VRS：**
- 以 DrawCall 为粒度设置 Shading Rate
- 整个 Draw Call 内所有像素使用同一 Shading Rate
- 典型应用：UI 渲染不需要降采样（1×1），远景 Mesh 可设为 2×2 或 4×4

**Tier 2 VRS：**
- 以屏幕 Tile（通常 8×8 或 16×16 像素）为粒度设置 Shading Rate
- 支持 VRS Image（Shading Rate Image）：一张低分辨率贴图，每个 Texel 对应一个屏幕 Tile 的着色频率
- 典型配置：中心区域 1×1，过渡区域 2×1 或 1×2，边缘 2×2 或 4×4
- 着色率可结合 Eye Tracking 数据动态更新，实现注视点渲染（Foveated Rendering）

**各着色频率的性能收益（理论值）：**

| Shading Rate | 像素/着色执行 | 相对开销 |
|--------------|---------------|----------|
| 1×1 | 1 | 100% |
| 2×1 / 1×2 | 2 | ~50% |
| 2×2 | 4 | ~25% |
| 4×4 | 16 | ~6% |

实际收益因 Shader 复杂度和内存带宽瓶颈而异，通常整帧节省 10%~30%。

### 硬件与 API 要求

- **NVIDIA**：Turing 架构（RTX 20 系）及以后支持 Tier 2 VRS
- **AMD**：RDNA 2 架构（RX 6000 系）及以后支持 Tier 2 VRS
- **主机**：PlayStation 5 和 Xbox Series X|S 原生支持等效 VRS 功能
- **API**：DX12（Windows）或 Vulkan；DX11 不支持 VRS

### Unity 支持情况

Unity URP 和 HDRP 通过 `ShadingRateImage` API 支持 Tier 2 VRS：

- 需要显式切换到 DX12 或 Vulkan 后端（Edit → Project Settings → Player → Graphics API）
- HDRP 内置 VRS 通道，可通过 Volume 系统配置 Shading Rate 分布
- 推荐搭配 Eye Tracking 或 Motion Vector 数据动态生成 VRS Image

**典型适用场景：**
- VR 应用：边缘区域大幅降采样，配合 Foveated Rendering
- 竞技游戏：快速运动区域降采样，静止精细区域保持 1×1
- 影视级渲染：UI 和字幕区域保持高精度，背景降采样

---

## 三、DirectStorage

### 传统 IO 路径的瓶颈

在 DirectStorage 出现之前，贴图从磁盘到显存的路径如下：

```
SSD → 系统内存（DMA 读取）→ CPU 解压缩（BCn/zstd/LZ4）→ 系统内存（解压后）→ DMA 拷贝 → 显存
```

这条路径有两个主要瓶颈：

1. **CPU 解压缩**：即便现代 CPU 的解压缩速度很快，大量贴图并发加载时仍会占满 CPU 核心，抢占游戏逻辑线程
2. **多次内存拷贝**：数据在系统内存与显存之间来回搬运，消耗内存带宽

### DirectStorage 的新路径

```
NVMe SSD → GPU 解压缩引擎 → 显存（直接写入）
```

核心变化：

- 利用 DX12 的 `ID3D12GraphicsCommandList6` 接口，发起 GPU 驱动的异步 IO 请求
- 支持 **GDeflate** 格式：专为 GPU 并行解压设计的压缩格式，压缩比与 Deflate 相当，但 GPU 可以高度并行解压
- 支持 **BCn** 硬件格式直接写入，减少格式转换开销
- CPU 仅提交命令，实际数据搬运和解压由 GPU 异步完成

### 性能对比

| 场景 | 传统 IO（CPU 解压） | DirectStorage（GPU 解压） |
|------|---------------------|---------------------------|
| 单张 4K DXT1 贴图加载 | ~20-50 ms | ~2-5 ms |
| 大型场景（100 张 4K 贴图） | 2-5 秒（连续） | 200-500 ms |
| CPU 占用 | 高（解压线程满载） | 极低（GPU 异步） |
| 开放世界场景切换 | 明显卡顿或加载屏 | 接近无缝流式加载 |

以上数据为参考量级，实际效果取决于 SSD 速度、贴图格式和 GPU 型号。

### 硬件与系统要求

- **操作系统**：Windows 10（基础 DirectStorage）或 Windows 11（最优性能）
- **API**：DX12，需要 NVMe SSD（SATA SSD 也支持但收益有限）
- **GPU 硬件解压**：NVIDIA RTX 40 系（Ada Lovelace）、AMD RDNA 3 支持 GDeflate 硬件解压；早期 GPU 回退到 GPU Shader 解压（仍快于 CPU 解压）

### Unity 与 Unreal 的支持对比

| 引擎 | 支持状态 | 备注 |
|------|----------|------|
| Unity（D3D12 Backend） | 实验性支持 | 通过 D3D12 后端部分集成，需手动配置 |
| Unreal Engine 5 | 深度集成 | Nanite Streaming 与 Lumen GI 均依赖 DirectStorage 提升流式效率 |

Unity 目前的实验性支持主要依托 D3D12 后端的异步加载通道，尚未形成面向开发者的高层封装 API。Addressables 系统目前不自动利用 DirectStorage，开发者需通过底层 C++ 插件桥接。

---

## 小结

PC 平台这三类技术分别解决了渲染管线中三个不同阶段的效率问题：

| 技术 | 解决的问题 | Unity 成熟度 |
|------|-----------|--------------|
| DLSS / FSR / XeSS | 像素着色开销过高（升采样补偿画质） | 较成熟（官方包或 SDK） |
| VRS | Fragment Shader 全屏均匀执行浪费 | 可用（需 DX12/Vulkan + 新硬件） |
| DirectStorage | 贴图加载 CPU 瓶颈 + 流式加载延迟 | 实验性（需关注官方路线图） |

对于当前（2026 年初）的 Unity 项目，升采样技术（尤其是 FSR 2.x，跨平台且开源）已经是值得优先集成的生产就绪方案；VRS 在 VR 项目中投资回报率高；DirectStorage 则处于"可以评估但尚不宜大规模依赖"的阶段。
