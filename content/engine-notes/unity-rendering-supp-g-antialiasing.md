+++
title = "Unity 渲染系统补G｜渲染算法对比：抗锯齿（MSAA/TAA/FXAA/DLSS/FSR）"
slug = "unity-rendering-supp-g-antialiasing"
date = 2026-03-26
description = "锯齿来自离散采样，各种抗锯齿方案从不同角度解决这个问题：MSAA 在光栅化阶段多采样，FXAA 在后处理阶段检测边缘，TAA 累积多帧历史，DLSS/FSR 用低分辨率渲染再升频。这篇做横向对比，帮助选择合适方案。"
weight = 1560
[taxonomies]
tags = ["Unity", "Rendering", "抗锯齿", "MSAA", "TAA", "FXAA", "DLSS", "FSR"]
[extra]
series = "Unity 渲染系统"
+++

游戏画面里的锯齿是一个老问题，但解法从来不统一。MSAA 在光栅化阶段用更多样本；FXAA 在后处理阶段用图像处理模糊边缘；TAA 跨帧累积信息；DLSS/FSR 干脆以低分辨率渲染再用算法升频。这些方案各有取舍，没有"最好的方案"，只有"当前平台和质量目标下最合适的方案"。这篇从原理出发，做横向对比，帮助做出有依据的选择。

---

## 锯齿的根本原因：采样定理与离散化

### 奈奎斯特频率与走样

锯齿（Aliasing）的本质是**采样频率不足以还原信号**。根据奈奎斯特–香农采样定理，要准确还原一个信号，采样频率必须至少是信号最高频率的两倍。屏幕的像素网格就是一种采样，像素间距决定了采样频率。

几何边缘在频域里含有无限高频分量（阶跃函数）——像素网格无法精确表示亚像素边缘，只能做二元判断"覆盖或不覆盖"，结果就是阶梯状锯齿。

### 几何走样 vs 着色走样

走样有两种性质不同的来源：

- **几何走样（Geometric Aliasing）**：三角形边缘的阶梯状锯齿。由光栅化的覆盖判断引起。MSAA 直接针对这一类。
- **着色走样（Shading Aliasing）**：高频纹理、镜面高光（Specular）在运动时的闪烁。由 Shader 计算结果的高频变化引起，MSAA **不能**解决这一类。

区分这两种来源很重要——很多情况下 MSAA 开着但高光仍然闪烁，原因就在这里。

---

## MSAA（Multi-Sample Anti-Aliasing，多重采样抗锯齿）

### 工作原理

MSAA 在光栅化阶段为每个像素放置多个**子采样点（Sample）**。以 4x MSAA 为例，每个像素有 4 个采样点，分布在像素内不同位置。

覆盖判断对每个采样点独立进行——如果三角形覆盖了 3/4 的采样点，该像素的颜色就混合一个 3/4 的覆盖率权重。但 Shader（片元着色器）**只执行一次**，结果共享给所有覆盖的采样点。深度和 Stencil 值则是每个采样点独立存储。

这意味着：
- MSAA 解决几何边缘锯齿：有效
- MSAA 解决 Specular 闪烁：**无效**（Shader 只算一次）

### TBDR 架构上的 MSAA

移动端 GPU 普遍采用 TBDR（Tile-Based Deferred Rendering）架构。MSAA 的多采样缓冲保留在 On-Chip 的 Tile Buffer 中，Resolve（合并为最终颜色）发生在 Tile 写回主存之前。这意味着 MSAA 的额外显存带宽消耗极低，**移动端 4x MSAA 的性能代价远低于桌面端**，是移动端首选的抗锯齿方案。

### Unity URP 中的 MSAA 设置

在 URP Asset（`UniversalRenderPipelineAsset`）中，`Anti Aliasing (MSAA)` 可设置为 Disabled / 2x / 4x / 8x。移动端通常选 4x，8x 一般不必要。

延迟渲染路径（Deferred Rendering Path）下，MSAA 在 G-Buffer Pass 之前无法直接工作，Unity URP 的 Deferred 路径对 MSAA 有限制，需要留意。

---

## FXAA（Fast Approximate Anti-Aliasing，快速近似抗锯齿）

### 工作原理

FXAA 是后处理阶段的全屏图像处理，不改动光栅化流程。它的步骤：

1. 将颜色转换为**亮度（Luma）**空间
2. 计算当前像素与邻居的亮度梯度，检测高梯度边缘
3. 沿边缘方向进行**次像素混合**，对锯齿边缘做平滑

FXAA 本质是一种图像空间的模糊滤波器，聪明之处在于只对识别出的边缘区域做模糊，而不是全图模糊。

### 优缺点

**优点**：
- 代价极低，一次全屏 Pass，带宽消耗很小
- 不需要深度、Motion Vector 等额外数据
- 移动端友好

**缺点**：
- 画面有轻微模糊感，影响文字、细线等高频细节的清晰度
- 运动中效果不稳定，可能有闪烁
- 不能处理着色走样（Specular 闪烁）

FXAA 适合对性能极度敏感、对画质要求不高的场景，例如移动端超低画质档。

---

## TAA（Temporal Anti-Aliasing，时间抗锯齿）

### 工作原理

TAA 的核心思路：每帧渲染时用轻微抖动的相机矩阵（Jitter）偏移投影矩阵，使不同帧的采样位置覆盖像素内的不同子位置。然后将当前帧与历史帧混合（History Blending），相当于跨帧累积了更多采样点。

关键组件：
- **Jitter**：每帧偏移投影矩阵半个像素以内的随机量，通常用 Halton 序列保证分布均匀
- **Motion Vector**：记录每个像素从上一帧到当前帧的屏幕空间位移，用于将历史帧"对齐"到当前帧
- **History Blend**：`output = lerp(current, history_reprojected, blend_factor)`，blend_factor 通常在 0.05~0.1 之间

### 优缺点

**优点**：
- 静止画面收敛后质量极高，接近 8x MSAA
- 能处理着色走样（Specular 闪烁被时间平滑）
- 是现代 PC/主机游戏的主流方案

**缺点**：
- **鬼影（Ghosting）**：快速运动物体的历史帧对齐不准确，出现残影。需要 Clamp/Clip 历史帧颜色、提高 blend_factor 来抑制，但会损失收敛质量
- 需要 Motion Vector Pass，有额外代价
- 画面有轻微模糊（低频），需要额外的锐化 Pass（如 TAA Sharpening）
- 与后处理的兼容问题：Bloom、DOF 等后处理效果应在 TAA 之前还是之后应用，需要仔细处理

### Unity URP 的 TAA

Unity URP 14（对应 Unity 2022.2+）正式加入 TAA。在 Camera 的 Anti-aliasing 下拉菜单中选择 TAA，可配置 Base Blend Factor 和 Variance Clamp Scale。

与 Depth of Field（DOF）配合时，DOF 应在 TAA 之前应用（Pre-TAA DOF），否则 DOF 的散焦圆盘会被 TAA 的历史混合拉出拖影。

---

## SMAA（Subpixel Morphological Anti-Aliasing，子像素形态学抗锯齿）

SMAA 是对 MLAA（形态学抗锯齿）的改进，工作在后处理阶段，但比 FXAA 更精细。

流程分三步：
1. **边缘检测**：用亮度或颜色梯度检测边缘像素，比 FXAA 更准确
2. **权重计算**：识别边缘模式（L型、Z型等形状），计算混合权重
3. **邻域混合**：根据权重混合颜色

SMAA 比 FXAA 画质更好（尤其是文字和细线），但代价也稍高（3 个 Pass）。

Unity URP 官方不内置 SMAA，但可通过自定义 Renderer Feature 添加。开源实现（如 `Amplify Anti-Aliasing`）在 Asset Store 可获取。

---

## DLSS（Deep Learning Super Sampling，深度学习超采样）

### 原理

DLSS 以**低于目标分辨率**渲染场景（例如目标 4K 则渲染 2K），然后用运行在 Tensor Core 上的 AI 神经网络将低分辨率图像升频到目标分辨率。

DLSS 2/3 使用时间信息（Motion Vector + 历史帧）辅助升频，不仅做空间插值，还能恢复历史帧中的高频细节。DLSS 3 额外加入了 Frame Generation（帧生成），可在渲染帧之间插入 AI 生成帧，进一步提升帧率数字（但增加输入延迟）。

渲染分辨率倍率（Performance Mode）：

| 模式 | 渲染分辨率 | 升频比 |
|------|-----------|--------|
| Quality | 目标 67% | 1.5x |
| Balanced | 目标 58% | 1.7x |
| Performance | 目标 50% | 2x |
| Ultra Performance | 目标 33% | 3x |

### Unity 支持

NVIDIA 提供 `DLSS Unity Plugin`（通过 Package Manager 安装：`com.unity.modules.unitywebrequestwww` 等），Unity 2021.2+ 支持，HDRP 和 URP 均可使用。

**限制**：仅支持 NVIDIA GeForce RTX 系列 GPU（需要 Tensor Core）。

---

## FSR（FidelityFX Super Resolution，AMD 保真度超分辨率）

### FSR 1：空间升频

FSR 1 是纯空间滤波器（RCAS + EASU），不使用时间信息，不需要 Motion Vector，不需要特定硬件。质量在中低档设置下明显低于 DLSS，但跨平台兼容性极好。

### FSR 2/3：时间升频

FSR 2 引入时间累积，质量大幅提升，与 DLSS 2 质量接近，且**不依赖特定 GPU 型号**，AMD/NVIDIA/Intel 均可运行。FSR 3 在 FSR 2 基础上加入了类似 DLSS 3 的 Frame Generation。

### Unity 支持

AMD 提供 `FidelityFX SDK` 的 Unity 集成，可通过 GitHub 获取 Unity 插件。Unity 6 的 `Upscaling` 框架将 FSR 作为内置选项之一。

---

## XeSS（Intel Xe Super Sampling）

XeSS 是 Intel 的超采样方案，设计上接近 DLSS（使用时间信息 + AI 升频），但在没有 Intel XMX（AI 加速引擎）的 GPU 上会退回到 DP4a 通用指令执行，因此在非 Intel GPU 上也可运行，质量介于 FSR 2 和 DLSS 之间。

Unity 的官方 XeSS 支持处于发展中，Intel 提供独立插件，集成方式与 DLSS/FSR 类似。

---

## 横向对比表

| 方案 | 画质 | 性能代价 | 鬼影风险 | 移动端适用 | Unity 支持方式 |
|------|------|---------|---------|-----------|--------------|
| MSAA 4x | 中（几何边缘） | 低（TBDR 几乎免费） | 无 | 首选 | 内置，URP Asset |
| FXAA | 低-中 | 极低 | 无 | 适用 | 内置，Camera 设置 |
| TAA | 高 | 中（需 Motion Vector） | 中 | 不推荐 | URP 14+ 内置 |
| SMAA | 中-高 | 低-中 | 无 | 可用 | 第三方插件 |
| DLSS 2/3 | 极高 | 负（升频后更快） | 低 | 不支持 | NVIDIA 插件 |
| FSR 1 | 中 | 极低 | 无 | 可用 | AMD 插件 / Unity 6 |
| FSR 2/3 | 高 | 低（升频后更快） | 低 | 部分支持 | AMD 插件 / Unity 6 |
| XeSS | 高 | 低（升频后更快） | 低 | 不推荐 | Intel 插件 |

---

## 选择建议

**移动端**：
- 首选 **MSAA 4x**，TBDR 架构下代价极低，效果稳定
- 性能极限时用 **FXAA** 兜底
- 不建议 TAA（移动端 Motion Vector 代价相对高，Ghosting 难调）

**PC 中低端 / 无 RTX**：
- 质量优先：**TAA**（需仔细调参抑制 Ghosting）
- 性能优先且有 AMD/Intel GPU：**FSR 2**
- 最保守：**FXAA + MSAA 组合**（MSAA 处理几何，FXAA 处理高频细节）

**PC 高端 / RTX**：
- **DLSS 2/3**，Quality 模式下画质超过原生 TAA，同时性能更好
- 无 RTX 则用 **FSR 2** 或 **XeSS**

**主机（Console）**：
- PS5/Xbox Series 均支持 FSR 2（AMD GPU 架构）
- TAA 仍是主机游戏的基础选项

---

## 小结

锯齿问题没有银弹，各方案解决的问题层面不同。MSAA 解决几何走样但不解决着色走样；FXAA 图像空间平滑但牺牲清晰度；TAA 质量高但需要管理 Ghosting；DLSS/FSR 以升频为核心，既抗锯齿又换取性能空间。实践中常见的做法是将多种方案分层叠加：主体用 TAA 或 DLSS/FSR，再加一个 Sharpening Pass 找回清晰度。理解每种方案的原理，才能在 Profile 时看懂画质问题的来源，做出有依据的取舍。
