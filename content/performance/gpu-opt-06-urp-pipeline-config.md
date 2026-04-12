---
title: "GPU 优化 06｜URP 移动端 Pipeline 配置：Renderer Feature、Pass 裁剪与带宽优化"
slug: "gpu-opt-06-urp-pipeline-config"
date: "2026-03-28"
description: "URP 默认配置面向 PC，移动端需要针对性裁剪。本篇覆盖 URP Asset 的关键参数含义、Renderer Feature 的代价评估、Depth Priming 与 Deferred vs Forward 的选择，以及 Framebuffer Fetch 对带宽的影响。"
tags:
  - "Mobile"
  - "GPU"
  - "URP"
  - "性能优化"
  - "Unity"
series: "移动端硬件与优化"
weight: 2130
---

Universal Render Pipeline（URP）是 Unity 移动端项目的标准渲染管线。但 URP 的默认配置是 PC 向的，移动端需要系统性地裁剪才能发挥移动端 GPU 的 TBDR 优势。

---

## URP Asset 核心参数

### Rendering 部分

```
Rendering Path: Forward
  → 移动端通常选 Forward（Deferred 在移动端的代价后文详述）

Depth Priming Mode:
  Auto（推荐）→ 引擎自动判断是否用 Depth Prepass
  Forced      → 强制 Depth Prepass（DrawCall 翻倍，通常不推荐）
  Disabled    → 不用 Depth Prepass

Depth Texture: 按需开启
  → 关闭可以节省 1 次 Shadow Map 以外的深度 RT
  → 如果 Soft Particles / Post-process 需要深度，必须开启

Opaque Texture: 按需开启
  → 关闭可以节省 1 次 Blit（场景颜色复制到 _CameraOpaqueTexture）
  → Refraction / Distortion 效果需要此项

Terrain Holes: 不需要时关闭
  → 影响所有地形的 Shader，会禁止 GPU Instancing 优化
```

### Quality 部分

```
HDR: 移动端慎用
  → HDR 需要 R16G16B16A16 Framebuffer（2× RGBA8888 的带宽）
  → 如果不做 Bloom/Tonemapping，关闭 HDR 节省 50% Framebuffer 带宽

  带宽对比（1080p @ 60fps）：
  LDR (RGBA8888): 写入 1080p × 4 bytes × 60 = 480 MB/s
  HDR (RGBA16F):  写入 1080p × 8 bytes × 60 = 960 MB/s

Render Scale: 0.75-0.9（移动端常用）
  → 降低内部渲染分辨率，最后 Upscale 到屏幕分辨率
  → 0.85 缩放：节省约 28% GPU 工作量，视觉质量影响轻微

MSAA:
  Off（性能最佳）
  2x（轻微锗抗锯齿，代价低）
  4x（移动端 TBDR 友好，代价可接受）
  8x（不推荐，代价较高）

  TBDR 上 MSAA 比 TAA 更高效：
  → MSAA 可以在 Tile Memory 内完成 resolve，不需要额外 DRAM 读写
  → TAA 需要历史帧，每帧需要 DRAM 读写历史缓冲区
```

---

## Forward vs Deferred 的移动端选择

### Forward Rendering（推荐移动端）

```
Forward 的每个物体渲染：
  对每个影响该物体的光源，执行 1 次 Fragment Shader

优点：
  简单，移动端 TBDR 可以发挥 On-Chip 优势
  没有额外 GBuffer 带宽
  配合 MSAA 效果好

缺点：
  多光源场景：每个光源叠加 1 个 DrawCall（或 Shader 变体）
  大量动态光的场景性能下降快

适用：
  移动端的大多数场景（< 8 个动态光）
```

### Deferred Rendering（谨慎使用）

```
Deferred 的渲染流程：
  Pass 1（GBuffer Pass）：把所有物体的 Albedo/Normal/等写入多个 RT
  Pass 2（Lighting Pass）：基于 GBuffer 计算所有光照

问题：GBuffer = 3-4 张全屏 RT，每张都需要从 Tile Memory 写到 DRAM：

GBuffer 带宽（1080p）：
  Albedo (RGBA8):   1080p × 4 bytes = 8 MB
  Normal (RG16):    1080p × 4 bytes = 8 MB
  Specular (RGBA8): 1080p × 4 bytes = 8 MB
  Depth:            1080p × 4 bytes = 8 MB
  总计：32MB 写入 + 32MB 读取 = 64MB / 帧
  60fps：64MB × 60 = 3.84 GB/s（只是 GBuffer）

对比 Forward：无额外 GBuffer，带宽节省 3.84 GB/s

例外情况（Deferred 适合的场景）：
  100+ 动态光的场景（Deferred 的 Lighting Pass 更高效）
  但移动端几乎不会有这种场景

  如果需要大量光：用 Forward+ / Clustered Forward（URP 14+）
```

### Framebuffer Fetch（Apple Silicon 的带宽节省）

```
在支持 Framebuffer Fetch 的设备上（Apple GPU / Adreno 部分型号），
Deferred 的 GBuffer 可以完全在 Tile Memory 内传递，不走 DRAM。

Unity URP 在 Metal/Vulkan 路径上已自动使用 Framebuffer Fetch：
  iOS（Metal）：自动使用
  Android Vulkan（Adreno）：部分支持

在不支持 Framebuffer Fetch 的设备上（OpenGL ES / 旧 Mali）：
  Deferred 的 GBuffer 带宽开销完全无法避免
  → 移动端 Deferred 只适合已知目标设备支持 Framebuffer Fetch
```

---

## Renderer Feature 代价评估

Renderer Feature 是 URP 的自定义渲染扩展点，每个 Feature 都可能增加额外的 Pass：

### 常见 Renderer Feature 的代价

```
Screen Space Ambient Occlusion (SSAO)：
  代价：约 0.4-0.8ms（全屏后处理 Pass）
  移动端建议：关闭，或使用 Baked AO 替代

Decal Renderer：
  代价：约 0.2-0.5ms（视 Decal 数量）
  移动端建议：限制 Decal 数量（< 20 个活跃 Decal）

Full Screen Pass（自定义后处理）：
  代价：每个 Blit Pass 约 0.2-0.5ms（取决于 Shader 复杂度）
  移动端建议：合并多个 Pass 到单个 Multi-Effect Shader

Render Objects（自定义绘制层）：
  代价：等于重新绘制一批对象
  移动端建议：只在必要时使用，控制每层 DrawCall 数量
```

### 评估 Renderer Feature 代价

```csharp
// 代码中动态禁用 Renderer Feature
using UnityEngine.Rendering.Universal;

var rendererData = urpAsset.GetRendererList()[0] as UniversalRendererData;
foreach (var feature in rendererData.rendererFeatures)
{
    if (feature.name == "SSAO")
    {
        feature.SetActive(false); // 低端设备禁用
    }
}
```

---

## 后处理 Pass 的带宽成本

后处理是 Blit 链：每个 Pass 从上一个 RT 读取 → 写入新 RT。

```
Unity URP 后处理链的带宽（1080p，每帧）：

Bloom（双重模糊）：
  DownSample × 4（从 1080p 到 135p）：约 1.5 MB read/write
  UpSample × 4：约 1.5 MB read/write
  合计：约 3 MB / 帧，60fps = 180 MB/s

Color Grading（LUT）：
  1 次全屏 Blit + 3D LUT 采样
  约 8 MB / 帧 = 480 MB/s（1080p 全屏 Blit）

SSAO：
  1 次 SSAO Pass + 1 次 Blur = 约 16-24 MB / 帧 = 1-1.5 GB/s
  （SSAO 计算每个像素需要多次深度采样）

总计（Bloom + Color Grading + SSAO）：
  约 2.0-2.5 GB/s 额外带宽
  → 移动端带宽预算 30-40 GB/s，后处理占 5-8%
```

**合并 Pass 的方案**：

```hlsl
// ❌ 两个独立的 Full Screen Pass
// Pass 1: Color Grading
// Pass 2: Vignette

// ✅ 合并到一个 Shader
// 在单次 Blit 中同时完成 Color Grading + Vignette + Grain
Shader "Custom/PostProcess_Combined"
{
    HLSLPROGRAM
    half4 frag(v2f i) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D(_MainTex, ...);

        // Color Grading
        color.rgb = ApplyLUT(color.rgb, _LUT);

        // Vignette
        float2 uv = i.uv - 0.5;
        float vignette = 1.0 - dot(uv, uv) * _VignetteStrength;
        color.rgb *= saturate(vignette);

        // Film Grain
        color.rgb += (Random(i.uv) - 0.5) * _GrainStrength;

        return color;
    }
    ENDHLSL
}
```

---

## Depth Priming 深度解析

```
Depth Priming = 先做一次 Depth Prepass（只写深度），
                然后正式渲染时所有片段都通过 Early-Z，
                消除所有 Overdraw。

代价 vs 收益：

代价：
  增加 1 次 DrawCall 轮（所有不透明物体的深度写入）
  DrawCall 数量翻倍（但 Depth-only Pass 的 Fragment Shader 极简）

收益：
  正式渲染时零 Overdraw（所有遮挡的片段在 Early-Z 阶段剔除）

何时值得：
  场景 Overdraw > 3x：Depth Priming 有收益
  场景 Overdraw < 2x：Depth Priming 代价 > 收益

  复杂城市/洞穴场景：值得
  开阔场景/简单场景：不值得

Unity URP 的 Auto 模式：
  引擎根据场景复杂度自动判断是否启用
  如果不确定，保持 Auto
```

---

## 移动端 URP 配置模板

### 低端设备（2-3GB RAM，骁龙 7xx / Mali-G52）

```
URP Asset:
  Rendering Path: Forward
  HDR: Off
  Render Scale: 0.75
  MSAA: Off
  Depth Texture: Off（如无需后处理深度）
  Opaque Texture: Off

Lighting:
  Main Light: Per Pixel，1 Cascade，512 Shadow Map，Distance: 20m
  Additional Lights: Off
  Reflection Probes: Off（改用 Sky Cube）

Shadows:
  Soft Shadows: Off
  Shadow Cascade Blend: Off

Post-processing:
  Bloom: Off
  SSAO: Off
  DOF: Off
  Color Grading: On（LUT 代价极低）
  Vignette: On
```

### 中端设备（4-6GB RAM，骁龙 8xx / Mali-G77）

```
URP Asset:
  Rendering Path: Forward
  HDR: Off（或 On，视 Bloom 是否需要）
  Render Scale: 0.85
  MSAA: 2x
  Depth Texture: On
  Opaque Texture: Off

Lighting:
  Main Light: Per Pixel，2 Cascades，1024 Shadow Map，Distance: 40m
  Additional Lights: Per Vertex（不用 Per Pixel）
  Reflection Probes: Simple（1 个 Realtime Probe）

Shadows:
  Soft Shadows: Low（4 次 PCF）

Post-processing:
  Bloom: On，Quality: Low
  SSAO: Off
  Color Grading: On
  Vignette: On
```

### 高端设备（8GB RAM，骁龙 8 Gen 2+）

```
URP Asset:
  Rendering Path: Forward
  HDR: On
  Render Scale: 1.0（或 0.9）
  MSAA: 4x
  Depth Texture: On
  Opaque Texture: On（支持 Distortion/Refraction）

Lighting:
  Main Light: Per Pixel，2 Cascades，2048 Shadow Map，Distance: 60m
  Additional Lights: Per Pixel（最多 4 个）
  Screen Space Shadows: On

Post-processing:
  Bloom: On，Quality: Medium
  SSAO: On，Intensity: 0.5，Sample Count: 4
  Color Grading: On（32-bit LUT）
  Depth of Field: On，Sample Count: 6
```

---

## 性能验证流程

```
修改 URP 配置后，用以下步骤验证效果：

1. Unity Profiler（真机）：
   确认 GPU 时间变化
   查看 Gfx.WaitForPresent 是否下降

2. Frame Debugger（Editor）：
   Window → Analysis → Frame Debugger
   检查 Pass 数量是否减少
   检查每个 Pass 是否必要

3. Snapdragon Profiler / Xcode（真机）：
   验证 DRAM 带宽是否下降
   确认 Early-Z 命中率是否提升

4. 对比记录：
   修改前帧时间、修改后帧时间、DRAM 带宽变化
   形成可追溯的优化记录
```
