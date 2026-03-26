---
title: "URP 深度配置 01｜Pipeline Asset 解读：每个参数背后的渲染行为"
slug: "urp-config-01-pipeline-asset"
date: "2026-03-25"
description: "逐节拆解 UniversalRenderPipelineAsset 的每个配置项：Rendering、Quality、Lighting、Shadows、Post-processing 各区块的参数含义、对渲染行为的实际影响，以及移动端和 PC 的典型取值策略。"
tags:
  - "Unity"
  - "URP"
  - "Pipeline Asset"
  - "渲染配置"
  - "性能优化"
series: "URP 深度"
weight: 1530
---
`UniversalRenderPipelineAsset`（以下简称 Pipeline Asset）是 URP 的全局配置入口，挂在 `Project Settings → Graphics` 或 `Quality Settings` 里。这篇逐块拆解每个参数的含义，以及为什么这样设置。

系列九（unity-rendering-09）里已经介绍了它的整体定位，这篇不重复"它是什么"，直接讲每个参数背后的渲染行为。

---

## Rendering 区块

```
Rendering
  ├─ Depth Texture
  ├─ Opaque Texture
  ├─ HDR
  ├─ Anti Aliasing (MSAA)
  └─ Upscaling Filter
```

### Depth Texture

**开启后**：URP 在 Opaque 渲染完成后，把深度缓冲区复制到一张 `_CameraDepthTexture`（Texture2D），供后续 Pass 和 Shader 采样。

**不开启时**：深度仍然存在于 Depth Buffer 里（GPU 用于深度测试），但不能作为 Texture 被 Shader 采样。

**代价**：一次额外的 CopyDepth Pass（Blit），移动端有带宽开销。Forward 路径下，如果没有 Depth Prepass，还需要额外渲染一遍场景深度。

**什么时候必须开启**：
- 粒子系统软粒子（Soft Particles）— 需要采样深度做淡出
- 后处理景深（Depth of Field）— 需要深度信息
- 屏幕空间折射 / 扭曲效果
- 任何自定义 Shader 里用到 `_CameraDepthTexture` 的效果

**Deferred 路径**：不受此开关影响，深度天然存在（G-Buffer Pass 就写深度）。

### Opaque Texture

**开启后**：在 Opaque 渲染完成、Skybox 渲染后，把颜色缓冲区复制到 `_CameraOpaqueTexture`，供后续 Shader（通常是半透明物体）采样不透明结果。

**典型用途**：水面折射（水面 Shader 采样 `_CameraOpaqueTexture` + 偏移 UV）、玻璃折射。

**代价**：一次全屏 CopyColor Blit，移动端开销不小。如果没有需要它的效果，关闭。

### HDR

**开启后**：Camera 的颜色缓冲使用 16-bit 浮点格式（`R16G16B16A16_SFloat`）而不是 8-bit（`R8G8B8A8`），允许亮度值超过 1.0，避免高光过曝截断。后处理 Tonemapping 在 HDR 空间下更准确。

**关闭后**：颜色缓冲用 8-bit，直接截断超过 1.0 的值，Bloom 等后处理在 LDR 空间计算，效果打折。

**代价**：颜色 RT 体积翻倍（每帧带宽和 Tile Memory 占用加倍）。移动端高端机上开，低端机上考虑关闭。

### Anti Aliasing (MSAA)

可选 Disabled / 2x / 4x / 8x。

MSAA 的工作原理是对每个像素采多个子采样点，只在几何边缘（覆盖率 < 1 的像素）才实际执行多次采样，内部全覆盖的像素仍然只计算一次。所以 MSAA 解决的是**几何锯齿**，对 Shader 内部的高频噪声（纹理走样）没有帮助。

**代价**：
- Tile Memory 占用 = 单采样 × MSAA 倍数（4x MSAA = 4 倍 Tile Memory 压力）
- 仅在 Forward 路径下生效。Deferred 路径不支持 MSAA（选 Deferred 时此选项灰掉）
- 移动端 2x 是性价比最高的选择；PC 项目通常用 TAA 或 FXAA 替代硬件 MSAA

### Upscaling Filter

Dynamic Resolution 时，以较低分辨率渲染后上采样到屏幕分辨率。选项：Auto / Point（最近邻）/ Linear / FSR（AMD FidelityFX Super Resolution）。

FSR 是 AMD 开源的空间上采样算法，质量接近原生分辨率，建议在支持的平台上选 FSR。URP 14（Unity 2022.2）起内置 FSR 支持。

---

## Quality 区块

```
Quality
  ├─ HDR Precision
  ├─ Render Scale
  └─ LOD Cross Fade
```

### Render Scale

范围 0.1–2.0，直接乘以屏幕分辨率作为实际渲染分辨率。1.0 = 原生分辨率，0.75 = 75% 渲染分辨率（面积 56%），2.0 = 超采样。

移动端常用 0.75–0.9 配合 FSR 上采样，在中端机上换取帧率。

运行时修改：`UniversalRenderPipeline.asset.renderScale = 0.75f`。

### HDR Precision

Unity 6（URP 17）新增，选 32-bit 可以在 HDR 下获得更高精度，但 RT 体积再翻倍。绝大多数项目用默认 16-bit 即可。

---

## Lighting 区块

```
Lighting
  ├─ Main Light
  │   └─ Cast Shadows
  ├─ Additional Lights
  │   ├─ Per Object Limit
  │   └─ Cast Shadows
  └─ Reflection Probes
      └─ Probe Blending
```

### Main Light

URP 里，"Main Light"是场景里 `Light.type = Directional` 且被标记为 Sun Source 的那盏灯（或者最亮的 Directional Light）。它走专属的高质量路径（包括 Cascade Shadow Map）。

`Cast Shadows`：关闭则主光源不产生阴影，Shadow Map 不渲染，节省大量 GPU 时间。没有阴影需求的纯 2D 游戏或 UI 重游戏应关闭。

### Additional Lights

所有非主光源（Point、Spot 等）统称 Additional Lights。

**Per Object Limit**：每个 Renderer 最多被多少个 Additional Light 影响。默认 4，最高 8（Forward 路径）。超出的光源按距离远近截断。

Forward+ 路径下此限制被放宽（改为 per-cluster 限制，默认 32 个 per cluster）。

**Cast Shadows**：Additional Light 产生阴影代价很高（每个有阴影的 Additional Light 都需要额外 Shadow Map Pass），移动端建议关闭，或只允许最多 1 个。

### Reflection Probes

**Probe Blending**：开启后，场景物体在两个 Probe 的重叠区域会平滑过渡（采样两个 Cubemap 做混合）。代价是 Shader 里的额外一次 Cubemap 采样。高端 PC 开，移动端按需。

---

## Shadows 区块

```
Shadows
  ├─ Max Distance
  ├─ Working Unit
  ├─ Cascade Count (1–4)
  ├─ [每个 Cascade 的分割比例]
  ├─ Depth Bias
  ├─ Normal Bias
  └─ Soft Shadows
```

### Max Distance

阴影渲染的最大距离（相机到物体的距离）。超过这个值的物体不渲染到 Shadow Map 里，对应的像素不接收阴影。

**性能核心参数**：Shadow Map 的覆盖范围越大，每个像素对应的世界空间越大，Shadow Map 精度越低（阴影越模糊 / 锯齿越明显）。减小 Max Distance = 同等 Shadow Map 分辨率下精度更高。

移动端典型值：30–60 米。PC：100–200 米。

### Cascade Count

Cascade Shadow Map：把摄像机视锥按距离分成 N 段，每段用一张独立的 Shadow Map（近处用小 Frustum = 高精度，远处用大 Frustum = 低精度）。

| Cascade | 阴影质量 | GPU 代价 |
|---|---|---|
| 1 | 均匀精度（近处可能模糊）| 1 次 Shadow Pass |
| 2 | 近处精细 + 远处粗糙 | 2 次 Shadow Pass |
| 4 | 近处高精度 + 平滑过渡 | 4 次 Shadow Pass |

移动端一般用 1–2 个 Cascade，PC 用 3–4 个。

### Depth Bias 与 Normal Bias

这两个参数解决 **Shadow Acne**（自阴影锯齿，表面对自身产生错误阴影）：

- **Depth Bias**：渲染 Shadow Map 时，在深度值上加一个偏移。值太小 → Acne；值太大 → Peter Pan（物体与阴影分离）
- **Normal Bias**：沿顶点法线偏移采样位置，比 Depth Bias 更准确，不容易产生 Peter Pan

调参顺序：先把 Depth Bias 调到没有 Acne，再用 Normal Bias 消除 Peter Pan。两个参数一起用。

### Soft Shadows

URP 的 Soft Shadows 是 PCF（Percentage Closer Filtering）— 对 Shadow Map 周边多个像素采样求平均，得到软边缘。

Quality 选项：
- **Off**：硬阴影，一次采样，最快
- **Low**：3×3 PCF
- **Medium**：5×5 PCF（URP 推荐移动端上限）
- **High**：自适应 PCF（桌面端品质）

---

## Post-processing 区块

```
Post-processing
  ├─ Grading Mode (HDR / LDR)
  ├─ LUT Size
  └─ Fast sRGB/Linear Conversions
```

### Grading Mode

- **HDR**：Color Grading 在 HDR 空间进行，Tonemapping 作为最后一步。需要 Camera 开启 HDR。效果最好
- **LDR**：Color Grading 在 0–1 的 LDR 空间进行。不需要 HDR RT，移动端低端机选项

### LUT Size

Color Grading 用一张 3D LUT 贴图实现，LUT Size 决定 LUT 的分辨率（默认 32，范围 16–65）。

Size 越大，Color Grading 曲线还原越精确，但 LUT 贴图体积也越大（32³ = 32×1024 像素的 Texture）。移动端 16 或 24 已经足够。

---

## 多 Quality Level 配置策略

Pipeline Asset 可以在 Quality Settings 里为每个质量等级独立指定：

```
Low     → URPAsset_Low.asset
Medium  → URPAsset_Medium.asset
High    → URPAsset_High.asset
```

实践建议：

| 参数 | Low（移动低端）| Medium（移动高端）| High（PC/主机）|
|---|---|---|---|
| HDR | 关 | 开 | 开 |
| MSAA | 关 | 2x | 4x / TAA |
| Cascade | 1 | 2 | 4 |
| Soft Shadows | 关 | Low | Medium |
| Additional Lights | 1 (per-vertex) | 4 | 8 |
| Reflection Blending | 关 | 开 | 开 |
| Depth Texture | 按需 | 开 | 开 |

运行时切换质量等级：

```csharp
QualitySettings.SetQualityLevel(2, applyExpensiveChanges: true);
// 如果不同 Quality Level 指定了不同 Pipeline Asset，切换会自动生效
```

---

## 小结

Pipeline Asset 参数的优先级，按移动端优化影响排序：

1. **Cascade Count + Shadow Max Distance** — 阴影是移动端最大的 GPU 杀手
2. **HDR** — RT 带宽翻倍，低端机直接关
3. **MSAA** — Tile Memory 压力，移动端 2x 或关
4. **Depth Texture / Opaque Texture** — 按实际需求开，不用就关
5. **Additional Lights Cast Shadows** — 默认关闭，除非场景真的需要

下一篇（URP配置-02）深入 Universal Renderer Settings，讲 Rendering Path 选择、Depth Priming、Native RenderPass 这几个对渲染流程影响更底层的参数。
