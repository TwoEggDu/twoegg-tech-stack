---
title: "URP Pipeline Asset 参数速查卡｜所有参数含义、推荐值与关联文章"
slug: "urp-pipeline-asset-cheat-sheet"
date: "2026-04-14"
description: "URP Pipeline Asset 全参数速查卡：逐参数列出默认值、含义、移动端与 PC 推荐值，配合 config-01 / config-02 深度阅读"
tags: ["Unity", "URP", "Pipeline Asset", "速查", "性能优化"]
series: "URP 深度"
weight: 1494
---

这篇是参数速查卡，不讲原理。每个参数的详细解释见 [config-01]({{< relref "urp-config-01-pipeline-asset" >}}) / [config-02]({{< relref "urp-config-02-renderer-settings" >}})。

---

## Rendering 区块

| 参数 | 默认值 | 含义 | 移动端推荐 | PC 推荐 |
|------|--------|------|------------|---------|
| Depth Texture | Off | 生成 `_CameraDepthTexture` 供 Shader 采样 | 按需开（软粒子、DOF 需要） | 按需开 |
| Opaque Texture | Off | 生成 `_CameraOpaqueTexture` | 有水面折射才开 | 有折射才开 |
| HDR | On | 颜色 RT 用 16-bit 浮点 | 有 Bloom/Tonemapping 才开 | 开 |
| Anti Aliasing (MSAA) | Disabled | 硬件多重采样 | 2x 或关（用 FXAA） | 按需 |
| Upscaling Filter | Auto | 低分辨率渲染后的上采样算法 | FSR | FSR |

---

## Quality 区块

| 参数 | 默认值 | 含义 | 移动端推荐 | PC 推荐 |
|------|--------|------|------------|---------|
| Render Scale | 1.0 | 渲染分辨率倍数 | 0.75–0.85 | 1.0 |
| HDR Precision | 16-bit | HDR RT 精度 | 16-bit | 16-bit |

---

## Lighting 区块

| 参数 | 默认值 | 含义 | 移动端推荐 | PC 推荐 |
|------|--------|------|------------|---------|
| Main Light | Per Pixel | 主光计算精度 | Per Pixel | Per Pixel |
| Cast Shadows (Main) | On | 主光是否投射阴影 | On | On |
| Additional Lights | Per Pixel | 附加光计算方式 | Per Object（限制数量） | Per Pixel |
| Per Object Limit | 4 | 每物体最大附加光数 | 2–4 | 4–8 |
| Cast Shadows (Additional) | Off | 附加光是否投射阴影 | Off（极耗性能） | 按需 |
| Reflection Probes | Off | 反射探针混合 | 开（如果用了 Probe） | 开 |
| Probe Blending | Off | 多 Probe 混合 | Off（省开销） | On |

---

## Shadows 区块

| 参数 | 默认值 | 含义 | 移动端推荐 | PC 推荐 |
|------|--------|------|------------|---------|
| Max Shadow Distance | 50 | 阴影最大距离 | 30–50 | 50–100 |
| Cascade Count | 4 | 级联数量 | 2 | 4 |
| Shadow Resolution | 2048 | Shadow Atlas 分辨率 | 1024 | 2048 |
| Depth Bias | 1 | 消除 Shadow Acne | 1–2 | 1 |
| Normal Bias | 1 | 消除 Shadow Acne | 0.5–1 | 1 |
| Soft Shadows | On | 软阴影（PCF） | Low 或 Off | Medium–High |

---

## Post-processing 区块

| 参数 | 默认值 | 含义 | 移动端推荐 | PC 推荐 |
|------|--------|------|------------|---------|
| Grading Mode | LDR | 颜色分级精度 | LDR | HDR |
| LUT Size | 32 | 查找表精度 | 16–32 | 32 |

---

## Quality Level 与 Pipeline Asset 配对速查

Unity 的 **Project Settings → Quality** 中每个 Quality Level 都可以绑定一个独立的 Pipeline Asset。移动端项目通常需要准备 3 套 Asset（Low / Medium / High），每套按上表中不同推荐值配置：

- **Low**：Render Scale 0.75、MSAA Off、Cascade 1、Shadow Resolution 512、Additional Lights Per Vertex
- **Medium**：Render Scale 0.85、MSAA 2x、Cascade 2、Shadow Resolution 1024、Additional Lights Per Object
- **High**：Render Scale 1.0、MSAA 4x、Cascade 4、Shadow Resolution 2048、Additional Lights Per Pixel

运行时通过 `QualitySettings.SetQualityLevel()` 切换即可自动切换对应的 Pipeline Asset。

---

## 深度阅读

- [URP Pipeline Asset 全解读]({{< relref "urp-config-01-pipeline-asset" >}}) — 逐参数讲原理与内部实现
- [URP Renderer Settings 全解读]({{< relref "urp-config-02-renderer-settings" >}}) — Renderer 侧参数详解
- [URP 移动端性能适配]({{< relref "urp-platform-01-mobile" >}}) — 分档策略与真机调优实践
