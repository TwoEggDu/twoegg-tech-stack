---
title: "GPU 渲染优化 05｜后处理在移动端的取舍与降质策略"
slug: "gpu-opt-05-postprocess-mobile"
date: "2026-03-25"
description: "后处理在 PC 上几乎是标配，在移动端却是性能消耗的大户。本篇从移动端带宽和 ALU 代价出发，逐一评估常见后处理效果的代价，给出哪些值得保留、哪些应该关闭、哪些可以降质替代的具体建议。"
tags:
  - "移动端"
  - "GPU"
  - "后处理"
  - "性能优化"
  - "URP"
  - "Bloom"
  - "SSAO"
series: "移动端硬件与优化"
weight: 2250
---
后处理的代价在移动端被严重低估。每个全屏后处理效果至少需要一次额外的 RT 读写（Load + Store），加上 Fragment Shader 的 ALU 代价。对于 1080P 设备，一次全屏 Blit 的带宽代价约 8~16MB，多个效果叠加后很快成为瓶颈。

---

## 后处理的代价来源

后处理的带宽代价有两部分：

**① 读取相机颜色 RT**：每个后处理效果需要读取上一步的颜色结果，这是一次全屏纹理采样。

**② 写入输出 RT**：每个效果输出结果到一张新的 RT，这是一次全屏写入。

如果 5 个后处理效果串行执行（Bloom → Color Grading → Vignette → Chromatic Aberration → FXAA），就有 5 次全屏 Load + 5 次全屏 Store，外加每个效果的 Fragment ALU 代价。

URP 的 Post Processing 系统在 Unity 6 / URP 17 引入了 RenderGraph，可以自动合并相邻后处理 Pass，减少中间 RT 的数量。在 2022.3 LTS 上，URP 也尝试把多个后处理合并到一次 Blit 里（Uber Post Processing Pass）。但即便合并，带宽代价仍然存在。

---

## 常见后处理效果的移动端代价评估

### Bloom

**代价**：高

Bloom 的实现通常是：
1. 下采样（多次 Blit，逐步降低分辨率）
2. 模糊（高斯模糊或 Kawase 模糊，多次 Blit）
3. 上采样并与原图混合

URP 的 Bloom 默认有 6 次下采样 + 6 次上采样，即使每次是低分辨率 Blit，累计带宽和 ALU 代价仍然显著。

**移动端策略**：
- 低档：关闭 Bloom
- 中档：把 `Max Iterations` 从默认 6 降到 3~4，`Threshold` 调高减少参与 Bloom 的像素
- 高档：保留 Bloom，用 `Downscale` 设为 `Half`（从半分辨率开始计算）

---

### SSAO（Screen Space Ambient Occlusion）

**代价**：高

SSAO 需要：
1. 深度 RT 采样（读取 `_CameraDepthTexture`）
2. 多次随机方向采样（默认 8~16 次 tap）
3. 模糊 Pass（降噪）
4. 与光照结果混合

采样次数多、依赖深度 RT 是 SSAO 代价高的主要原因。

**移动端策略**：
- 低档、中档：关闭 SSAO
- 高档：开启 Low 质量（4 tap），仅对重要物体的接触区域有效
- 替代方案：烘焙 AO 贴图（静态物体用 Lightmap AO，动态物体用 Blob Shadow 或 Bent Normal AO 贴图近似）

---

### Color Grading / Tonemapping

**代价**：极低

Color Grading 在 URP 里通过 LUT（Look-Up Table）贴图实现：先把颜色变换关系预烘焙成一张 32×32×32 的 3D LUT，运行时只需要用颜色值在 LUT 里做一次 3D 纹理采样。

**整个 Color Grading 的 Fragment Shader 代价约等于 1 次纹理采样**，是代价最低的后处理效果之一。

**移动端策略**：所有档位都应该保留 Color Grading，它是视觉风格最重要的保障，代价可以忽略不计。

---

### Tonemapping

**代价**：极低

Tonemapping 通常和 Color Grading 合并在同一个 Pass 里（URP 的 Uber Post Pass），额外代价几乎为零。移动端保留。

---

### Vignette

**代价**：极低

Vignette 只是根据 UV 距离中心的距离乘一个衰减系数，几条 ALU 指令，和 Color Grading 合并在 Uber Pass 里。移动端保留。

---

### Chromatic Aberration（色差）

**代价**：低（但有额外采样）

色差需要对同一张 RT 用略微不同的 UV 采样 3 次（RGB 三通道各偏移不同），增加了纹理采样次数。

**移动端策略**：中低档关闭，只在高档且设计上需要时开启。色差是视觉风格效果，不是核心画质，去掉对多数场景影响不大。

---

### Motion Blur

**代价**：高

Motion Blur 需要 Velocity Buffer（每个像素的运动向量），加上按运动向量方向采样的模糊 Pass，总采样次数多。

**移动端策略**：
- 移动端屏幕帧率通常是 30~60fps，Motion Blur 的视觉收益本身就有限（帧率高时 Blur 量少）
- 低中高档通常全部关闭
- 替代方案：用摄像机 FOV 变化模拟快速移动感，代价为零

---

### Depth of Field（景深）

**代价**：高

标准景深实现需要 CoC（Circle of Confusion）计算 + 模糊 Pass，采样次数多。

**移动端策略**：
- 大多数移动端游戏不开景深
- 如果设计上必须，用 Bokeh Quality 最低档（单次采样），或用假景深（对背景做简单 Gaussian Blur，前景不模糊）

---

### Anti-Aliasing：FXAA vs TAA vs MSAA

| 方案 | 代价 | 移动端适用场景 |
|------|------|-------------|
| FXAA | 极低（单次全屏 Blit，几条 ALU）| 低中高档通用，首选 |
| SMAA | 低~中（多 Pass，质量优于 FXAA）| 高档可选 |
| TAA | 中（需要历史帧缓存，额外内存）| 移动端通常不推荐 |
| MSAA | 低（TBDR 上代价低，见带宽优化）| 中高档，与 FXAA 叠加 |

**移动端推荐**：低档 FXAA，中高档 2x MSAA + FXAA。TAA 需要历史帧缓存（额外一张全屏 RT 常驻内存），移动端内存紧张时代价不合算。

---

## 移动端后处理配置建议

| 效果 | 低档 | 中档 | 高档 |
|------|------|------|------|
| Color Grading / LUT | ✅ | ✅ | ✅ |
| Tonemapping | ✅ | ✅ | ✅ |
| Vignette | ✅ | ✅ | ✅ |
| FXAA | ✅ | ✅ | ✅ |
| MSAA | ❌ | 2x | 2x |
| Bloom | ❌ | 低质量 | 中质量 |
| SSAO | ❌ | ❌ | Low 质量 |
| Chromatic Aberration | ❌ | ❌ | 可选 |
| Motion Blur | ❌ | ❌ | ❌ |
| Depth of Field | ❌ | ❌ | 极低质量 |
| TAA | ❌ | ❌ | ❌ |

---

## 用 Volume Override 做后处理档位控制

URP 的后处理由 Volume 系统控制，可以在运行时通过代码切换 Volume Profile，或直接修改 VolumeComponent 的参数：

```csharp
// 在 QualityManager 切换档位时同步调整后处理参数
public static void ApplyPostProcessingForTier(QualityTier tier)
{
    var bloom = VolumeManager.instance.stack.GetComponent<Bloom>();
    var ssao  = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusion>();

    if (bloom != null)
    {
        bloom.active = tier >= QualityTier.Mid;
        if (tier == QualityTier.Mid)
        {
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.5f);
        }
    }

    if (ssao != null)
    {
        ssao.active = tier == QualityTier.High;
    }
}
```

**注意**：直接修改 VolumeComponent 的参数是运行时修改，切换后立刻生效。如果在游戏运行中途调整，应该通过 DOTween 或 Lerp 做平滑过渡，避免后处理效果突然变化。

---

## 小结

- 后处理的代价来自全屏 RT 读写（带宽）+ Fragment ALU，每个效果至少一次全屏 Load + Store
- **零代价保留**：Color Grading / LUT、Tonemapping、Vignette（合并在 Uber Pass 里）
- **按档位取舍**：Bloom（低档关闭，中档低质量）、SSAO（中低档关闭）、MSAA 替代 TAA
- **通常全档关闭**：Motion Blur、Depth of Field（移动端视觉收益低，代价高）
- 运行时通过 VolumeManager 控制后处理开关和参数，与质量分级系统联动
