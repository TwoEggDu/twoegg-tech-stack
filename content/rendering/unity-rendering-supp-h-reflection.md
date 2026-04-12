---
title: "Unity 渲染系统补H｜渲染算法对比：反射方案（Cubemap/Planar/SSR/RTXGI）"
slug: "unity-rendering-supp-h-reflection"
date: "2026-03-26"
description: "反射是 PBR 材质质感的重要组成。不同方案在动态性、精度、性能上差异巨大：Reflection Probe 是静态快照，Planar Reflection 精确但代价高，SSR 动态但有屏幕边界问题，实时光追反射精确但硬件要求高。"
weight: 1570
tags:
  - "Unity"
  - "Rendering"
  - "反射"
  - "Reflection Probe"
  - "SSR"
  - "光线追踪"
  - "PBR"
series: "Unity 渲染系统"
---
PBR 材质的质感，很大程度上来自反射。金属的质感、水面的倒影、光滑地板映出的灯光——这些视觉效果都依赖反射计算是否准确。但反射天然昂贵：精确的反射需要知道场景中每个方向的光照，而这正是渲染本身要解决的问题，形成循环。不同方案在精度、动态性、性能代价之间做出不同取舍，理解这些取舍是选择合适方案的前提。

---

## 反射的物理基础

### Fresnel 效应

反射强度不是固定的，它随视线角度变化。**Fresnel 效应**描述了这一现象：当视线与表面法线夹角越大（掠射角），反射越强；垂直看向表面时，反射最弱（对于非金属，垂直方向反射率 F0 通常在 2%~5%）。

在 PBR 的 Cook-Torrance BRDF 中，Fresnel 项通常用 Schlick 近似计算：

```
F(θ) = F0 + (1 - F0) * (1 - cos θ)^5
```

其中 F0 是材质的基础反射率（金属来自 Albedo，非金属来自折射率）。

### 粗糙度与反射模糊

光滑表面（Roughness → 0）产生清晰的镜面反射；粗糙表面（Roughness → 1）产生模糊的漫反射感。在 IBL（Image Based Lighting）框架里，这通过对 Environment Map 进行**预卷积（Pre-filtered Environment Map）**实现——对应不同粗糙度的反射存储在 Cubemap 的不同 Mip Level 中，采样时根据粗糙度选择对应 Mip 即可近似模糊反射。

这个预卷积过程是离线完成的，代价不算在运行时。

---

## Reflection Probe（反射探针）

### 工作原理

Reflection Probe 在场景中某个位置渲染一个 Cubemap（6 个面，通常 256×256 或 512×512），作为该位置的环境光快照。附近物体采样这个 Cubemap 作为反射来源。

Unity 支持两种模式：

**Baked（烘焙）**：
- 在编辑时渲染一次，结果存为资产
- 不反映动态物体
- 运行时代价为零（只是贴图采样）
- 适合静态场景

**Realtime（实时）**：
- 每帧（或按指定间隔）重渲染 Cubemap
- 反映动态物体
- **代价极高**：每次更新等于从该位置渲染完整场景的 6 个面；一个 Realtime Probe 每帧更新 = 额外渲染 6 次场景（可设为每帧只更新一个面以分摊）
- 除非必要，应避免高频更新

### Box Projection 修正视差

默认情况下，Cubemap 采样假设反射来自无限远处（球面 Cubemap 没有位置信息）。对于室内场景，这会产生明显的视差错误——站在房间不同角落看同一面镜子，反射的画面应该不同，但默认 Cubemap 不变。

Unity 的 **Box Projection** 将探针限定在一个包围盒内，采样时根据视线与包围盒的交点来修正 UV，消除室内场景的视差错误。代价是每次采样增加一次射线-AABB 求交运算，开销极小。

### Blend Distance

Unity 支持多个 Reflection Probe 之间的过渡混合（Blend Distance），避免两个探针影响范围边界处的突变。

---

## Planar Reflection（平面反射）

### 工作原理

Planar Reflection 的原理最直接：将虚拟相机放在反射平面的镜像位置，渲染一遍场景，将结果贴到反射平面上。

步骤：
1. 以反射平面为对称轴，将主相机镜像到平面另一侧
2. 用斜裁切平面（Oblique Projection Matrix）裁掉平面以下的内容
3. 渲染一次完整场景到 RenderTexture
4. 将 RenderTexture 采样后应用到反射平面材质

### 适用场景和代价

Planar Reflection 的精度是最高的——它是一次真实渲染，不存在任何近似。局限在于只适用于**平坦的反射平面**（水面、镜面、抛光地板），无法处理复杂形状的反射。

**代价 = 额外一次完整场景渲染**，与主渲染代价相当，Draw Call 翻倍。帧率开销在 30%~50%。

Unity 的 Water System（HDRP）使用 Planar Reflection 实现水面反射。自定义实现时需要注意：
- 反射相机的 Culling Mask 应排除不可见物体
- 应根据反射平面高度动态调整斜裁切平面

---

## SSR（Screen Space Reflection，屏幕空间反射）

### 原理回顾

SSR 在已渲染完成的帧缓冲中进行光线步进（Ray Marching）：从像素出发，沿反射方向在屏幕空间步进，与深度缓冲比对，找到交点，采样该位置的颜色作为反射结果。

核心限制：**只能反射屏幕内可见的内容**。屏幕边缘以外、被遮挡的物体、相机背后的内容，SSR 无法反射。

### Unity URP/HDRP 中的 SSR

**HDRP** 内置 SSR，通过 Volume 的 `Screen Space Reflection` Override 控制：
- `Minimum Smoothness`：只对光滑程度超过阈值的表面应用 SSR
- `Ray Max Iterations`：步进次数，直接影响性能代价
- `Thickness`：深度比较容差

**URP** 官方 SSR 在 Unity 6 中加入（通过 Render Graph）。Unity 2022.x 的 URP 需要自定义 Renderer Feature 实现 SSR。

### 性能代价

步进次数是主要开销。通常配置：
- 移动端：不建议使用（步进计算量高，带宽读取代价大）
- PC 中端：32~64 步进，Half Resolution
- PC 高端：64~128 步进，Full Resolution

### Fallback 策略：三层反射链

SSR 命中失败时需要 Fallback，标准实践是三层：

```
SSR 命中 → Reflection Probe（Cubemap） → IBL（天空盒 / SkyLight）
```

权重混合：SSR confidence 用于控制 SSR 结果与 Fallback 的混合权重，在边缘区域平滑过渡，避免反射消失处出现硬边。

---

## RTXGI / 实时光追反射

### 原理

基于 DXR（DirectX Raytracing）的实时光追反射：从像素出发，向 GPU 光追 BVH（Bounding Volume Hierarchy）发射射线，直接求交后返回交点的辐射亮度。结果精确，不受屏幕边界限制，能反射屏幕外、被遮挡的内容。

### Unity HDRP 的实现

HDRP 通过 Volume Override 的 `Ray Traced Reflections` 启用：
- 替代 SSR，对符合平滑度条件的表面发射光追射线
- 可配置每像素样本数（Sample Count）、Bounces 次数
- 去噪（Denoiser）是必须开启的，原始光追结果噪声极大

**硬件要求**：需要 NVIDIA RTX 或 AMD RX 6000+ 以上支持 DXR 的 GPU。

**代价**：极高。即使 1 Sample/Pixel，代价约为 2~5ms（1080p），是目前最昂贵的反射方案。实际应用中通常以较低分辨率 + 激进降噪配合使用。

---

## 粗糙反射（Glossy Reflection）

清晰反射只是特殊情况（Roughness = 0）。真实材质大多有一定粗糙度，需要模糊反射。

**IBL 方案（离线预计算）**：
- Prefiltered Environment Map 在每个 Mip Level 存储对应粗糙度的卷积结果
- BRDF LUT（2D 贴图，存储 NdotV 和 Roughness 两轴的预积分 BRDF 值）
- 运行时：`reflection = PrefilterMap.SampleLevel(R, roughness * maxMip) * (BRDF_LUT.r * F0 + BRDF_LUT.g)`

**SSR 的模糊反射**：通过 Cone Tracing（多条射线平均）或在 SSR 结果上做 Blur Pass，根据粗糙度控制模糊半径。

**光追的模糊反射**：多 Sample 在反射方向周围采样 GGX Lobe，代价更高但结果正确。

---

## 横向对比表

| 方案 | 动态性 | 精度 | 性能代价 | 平台适用性 | 限制 |
|------|-------|------|---------|-----------|------|
| Baked Probe | 静态 | 低（无视差，除非 BoxProj） | 极低 | 所有平台 | 不反映动态物体 |
| Realtime Probe | 动态 | 中 | 高（每帧重渲 6 面） | PC/主机 | 代价随探针数线性增长 |
| Planar Reflection | 动态 | 极高 | 极高（等于一次完整渲染） | PC/主机 | 仅平坦平面 |
| SSR | 动态 | 高（屏幕内） | 中 | PC/主机 | 屏幕边界外无效 |
| 实时光追反射 | 动态 | 极高 | 极高 | 高端 PC/主机 | 需 DXR 硬件 |

---

## 移动端建议

移动端的反射方案应极度保守：

- **只用 Baked Reflection Probe**：代价为零，配合 BoxProjection 可以修正室内视差
- **Fresnel 控制可见性**：通过 Fresnel 使反射在掠射角更强，在正视角很弱，减少反射与现实的偏差被玩家注意到
- **SSR 不建议**：移动端的带宽代价和步进计算量使 SSR 通常不合算
- **Planar Reflection 极少使用**：仅在有明确预算支持时用于水面等视觉核心场景，且应降低反射 RenderTexture 分辨率（如 1/2 或 1/4）

---

## 小结

反射方案的选择是一个精度与代价的权衡矩阵。Baked Probe 在静态场景下几乎是免费的，应该作为基础层永远存在；SSR 在中高端 PC 和主机上提供动态细节，但需要完善的 Fallback 策略；Planar Reflection 用于视觉核心的水面或镜面；光追反射是目前精度上限，但代价限制了它的适用范围。无论选择哪种方案，三层 Fallback（SSR → Probe → IBL）都应该作为标准实践，确保任何情况下反射不会突然消失。
