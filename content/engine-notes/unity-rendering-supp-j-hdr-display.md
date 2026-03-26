---
title: "Unity 渲染系统补J｜HDR 显示输出：色彩空间、HDR10、SDR vs HDR 渲染路径"
slug: "unity-rendering-supp-j-hdr-display"
date: "2026-03-26"
description: "游戏内的 HDR（高动态范围）有两层含义：渲染用 HDR 帧缓冲（防止过曝截断）和输出到 HDR 显示器（真实亮度超过 100 nit）。这篇讲清楚色彩空间（sRGB/Linear/P3/Rec2020）、Tonemapping、HDR10/Dolby Vision 输出格式，以及 Unity 的 HDR 显示支持。"
weight: 1590
tags:
  - "Unity"
  - "Rendering"
  - "HDR"
  - "色彩空间"
  - "Tonemapping"
  - "HDR10"
  - "显示技术"
series: "Unity 渲染系统"
---
"HDR"这个词在游戏渲染里出现在两个完全不同的语境中，经常被混淆。第一个是**渲染管线内部的 HDR 帧缓冲**——用浮点格式存储光照计算结果，防止过亮区域的颜色被截断到 1.0，让 Bloom 和 Tonemapping 能正确工作。第二个是**输出到 HDR 显示器**——把亮度信号真正发送到支持 1000~4000 nit 峰值亮度的显示器，让玩家看到超出 SDR 范围的亮度和色彩。这两件事相互关联但性质不同，都需要理解清楚。

---

## HDR 渲染（内部）vs HDR 显示（输出）

### HDR 渲染管线（内部精度）

渲染管线里的 HDR 是一个精度问题：光照计算会产生超过 [0,1] 范围的数值（太阳直射、镜面高光、自发光物体的 Intensity > 1），如果帧缓冲是 8-bit（LDR），这些超亮值会被截断（Clamp），导致高光区域一片死白、过渡失真。

HDR 帧缓冲（通常是 `RGBA16F`，16-bit 半精度浮点）保留这些超范围值，让 Bloom 等后处理能感知到哪些区域"真正很亮"，Tonemapping 也能基于完整的动态范围做压缩。

**HDR 渲染管线不需要 HDR 显示器**——结果最终会经过 Tonemapping 压缩到 [0,1] 后输出到普通 SDR 显示器。

### HDR 显示输出（硬件能力）

HDR 显示器具备超过 SDR 的亮度范围（SDR 标准约 100 nit，HDR10 显示器峰值可达 600~4000 nit）和更宽的色域。将游戏信号发送给 HDR 显示器时，不需要将高亮区域压缩到 100 nit 以内，可以直接输出 800 nit 的高光，让观众看到真实的亮度差异。

**HDR 显示输出需要 HDR 显示器 + 操作系统 HDR 模式开启 + 游戏的 HDR 输出支持**。

---

## 色彩空间基础

### Gamma 空间 vs Linear 空间

人眼对亮度的感知是非线性的——对暗部变化更敏感，对亮部变化不敏感。历史上，显示器的 Gamma 曲线（输出亮度 ∝ 输入电压^2.2）正好补偿了这一特性，形成了 **sRGB** 标准的 Gamma ≈ 2.2 编码。

**问题**：物理光照计算（加减乘除）必须在线性空间中进行。在 Gamma 空间里做光照混合，结果是错误的（暗部过暗、亮部不准确，阴影边缘偏黑）。

**Unity 的 Color Space 设置**（Project Settings → Player → Color Space）：
- `Gamma`：旧版默认，所有计算在 Gamma 空间进行，兼容性好但光照不准确
- `Linear`：推荐设置，Shader 在 Linear 空间计算，结果写入 Linear 帧缓冲，最终显示时做 Linear→sRGB 转换（硬件 sRGB 写入）

### sRGB 贴图的自动转换

在 Linear 渲染模式下，贴图有两种情况：
- **颜色贴图（Albedo、Emission 等）**：存储的是 sRGB 值（美术在显示器上绘制的颜色是 Gamma 编码的），采样时 GPU 自动做 sRGB→Linear 转换（勾选 `sRGB (Color Texture)`）
- **数据贴图（Normal Map、Roughness、Metallic 等）**：存储的是线性数据，不应做 Gamma 转换（不勾选 `sRGB`）

这个区分很重要——如果把 Normal Map 标记为 sRGB，采样时会做错误的 Gamma 转换，导致法线方向错误，表现为光照异常。

---

## 色域（Color Gamut）

色域决定了"能表示哪些颜色"，与动态范围（亮度范围）是独立维度：

| 色域标准 | 覆盖范围 | 主要用途 |
|---------|---------|---------|
| sRGB / Rec.709 | 35.9% of CIE 1931 | SDR 显示器、Web、SDR 游戏 |
| Display P3 (DCI-P3) | ~45.5% | 苹果设备（iPhone/iPad/MacBook），电影放映 |
| Rec.2020 / BT.2020 | ~75.8% | HDR 电视、HDR10 标准 |
| Rec.2100 | 同 Rec.2020 | HDR10/Dolby Vision 的色域容器 |

游戏渲染的色彩计算通常在 sRGB 或更宽的内部色域进行，输出时映射到目标显示器的色域。sRGB 范围外的颜色在 SDR 显示器上无法正确显示，会被裁剪（Gamut Clipping）。

---

## HDR 渲染内部：RGBA16F 帧缓冲

### 浮点帧缓冲的工作方式

开启 HDR 渲染后，Unity 的 Camera 使用 `RGBA16F`（每通道 16-bit 半精度浮点）作为帧缓冲格式。半精度浮点的表示范围约为 -65504 ~ 65504，远超 [0,1]。

代价：
- 每像素 8 bytes（vs 8-bit RGBA 的 4 bytes），带宽和内存占用翻倍
- 移动端 MSAA + HDR 的组合代价更高

### Bloom 在 HDR 帧缓冲上的工作原理

在 SDR（8-bit）帧缓冲上，所有颜色都在 [0,1]，Bloom 只能通过亮度阈值（threshold）提取"较亮"区域，但实际上它们并不比普通颜色"亮"，只是接近 1.0。结果是 Bloom 范围和强度很难控制。

在 HDR 帧缓冲上：
- 自发光材质的 Emission Intensity = 3.0，其颜色值就是 3.0（真实高于白色）
- Bloom 的 Threshold 可以设为 1.0，只对真正超亮的区域触发 Bloom
- Bloom 的强度和形状自然地反映材质的自发光强度

这是 HDR 渲染带来的正确工作流：**Emission Intensity 控制发光强度，直接映射到 Bloom 效果**，而不是靠强行拉高 Bloom Threshold 来近似。

---

## Tonemapping

### 为什么需要 Tonemapping

渲染完成后，HDR 帧缓冲里的颜色值可能从 0.0 到数百（太阳、自发光等）。SDR 显示器只能接受 [0,1] 的颜色。Tonemapping（色调映射）是将 HDR 范围非线性地压缩到可显示范围的过程。

Tonemapping 的目标：
- 保持暗部和中间调的细节
- 高光区域优雅地"卷曲"（Rolloff），不是硬截断
- 整体色彩倾向和风格可控

### 常见 Tonemapping 曲线

**Reinhard**：`output = x / (1 + x)`，计算简单，高光被无限压缩但永远不会到 1.0，整体偏暗淡。

**ACES（Academy Color Encoding System）**：电影工业标准，S 型曲线，暗部有轻微 Lift，中间调对比度强，高光有明显 Rolloff，整体色调偏暖偏对比度高。是 Unity URP/HDRP 的推荐选项，画面有电影感。

**Neutral**：Unity 自研曲线，对颜色色调影响最小，接近线性压缩，适合需要精确色彩还原的场景。

**GT Tonemap（Gran Turismo）**：可参数化的 S 曲线，支持调节暗部、中间调、高光的斜率和截距，灵活性高。

### Unity 中的设置

通过 Post Processing Volume 的 `Tonemapping` Override 控制。URP 和 HDRP 支持的模式有所不同：
- URP：None / Neutral / ACES
- HDRP：None / External / Custom Curve / Neutral / ACES / AgX（Unity 2023.1+）

**AgX**（Unity 2023.1+ 的新选项）：来自 Blender 的 Tonemapping 算法，对高饱和度颜色处理更自然，避免 ACES 在某些颜色上的过饱和偏移问题。

---

## HDR 显示输出（HDR10）

### HDR10 格式标准

HDR10 是目前最主流的 HDR 显示标准：
- **10-bit 色深**：每通道 10-bit，可表示 1024 个级别（vs SDR 的 8-bit/256 级），减少暗部带状渐变（Banding）
- **PQ（Perceptual Quantizer）EOTF**：ST.2084 标准，将 [0,1] 的信号值非线性映射到 0~10000 nit 的绝对亮度，专为人眼感知优化
- **色域**：Rec.2020（BT.2020）
- **元数据**：MaxCLL（Maximum Content Light Level，内容最大亮度）和 MaxFALL（Maximum Frame Average Light Level，帧平均最大亮度），用于通知显示器进行色调映射

### Unity 的 HDR Output 设置

Unity 2022.2+ 正式支持 HDR Display Output（通过 `HDROutputSettings` API）：

```csharp
// 检测 HDR 显示器是否可用
if (HDROutputSettings.main.available)
{
    HDROutputSettings.main.RequestHDRModeChange(true);
}
```

Project Settings → Player → `Use HDR Display Output`（勾选后 Unity 会尝试开启 HDR 输出）。

在 HDR 输出模式下，Tonemapping 之后不再做 sRGB Gamma 编码，而是做 PQ 编码，并将色域从 sRGB 映射到 Rec.2020，然后输出 10-bit 信号给显示器。

**Paper White Nits**（SDR 白点亮度）：在 HDR 模式下，需要定义 UI 和 SDR 内容对应的亮度基准（通常 200~300 nit），用于混合 HDR 渲染内容和 UI。

---

## Dolby Vision vs HDR10 vs HLG

| 格式 | 色深 | EOTF | 元数据 | 使用场景 |
|------|------|------|--------|---------|
| HDR10 | 10-bit | PQ (ST.2084) | 静态（片头设置一次） | 游戏、蓝光、流媒体 |
| HDR10+ | 10-bit | PQ | 动态（每帧更新） | 流媒体（Amazon、三星）|
| Dolby Vision | 12-bit | PQ | 动态 + 杜比私有算法 | 高端流媒体、部分游戏机 |
| HLG（Hybrid Log-Gamma） | 10-bit | HLG | 无 | 广播电视（SDR/HDR 兼容） |

游戏领域主要使用 **HDR10**，部分主机（Xbox Series X）支持 **Dolby Vision Gaming**。HLG 主要用于广播，游戏很少涉及。

---

## SDR on HDR 显示（HDR→SDR Fallback）

### 过亮的 SDR 内容问题

当 HDR 显示器在 HDR 模式下显示 SDR 内容时，显示器会将 SDR 的"白色"（100 nit 基准）映射到很高亮度（可能 400~600 nit），导致画面刺眼过亮。

### Paper White 设置

Unity 的 HDR Output 框架提供 **Paper White Nits** 参数，定义 SDR 内容（尤其是 UI）对应的绝对亮度。设置合理的 Paper White（200~300 nit）确保 UI 在 HDR 模式下不会过亮。

玩家设置菜单中提供 Paper White 滑块是推荐的做法，因为不同用户的室内亮度不同。

---

## 移动端 HDR

### iOS：广色域与 P3

iPhone 7 起搭载 Display P3 色域的屏幕（覆盖约 25% 更多颜色范围）。iOS 的 Metal 渲染管线支持 P3 输出：设置 `CAMetalLayer.pixelFormat = .rgba16Float` 并开启广色域模式，Shader 输出的颜色可直接表示 P3 范围内的颜色。

Unity 在 iOS 上需要在 Player Settings 中勾选 `Require Wide Color Display` 以解锁广色域渲染。

### Android：HDR Vivid 和碎片化

Android 的 HDR 支持碎片化严重。Android 13+ 提供 `ColorSpace.Named.BT2020_HLG` 和 `BT2020_PQ` 等标准色彩空间 API，但设备支持情况各异。华为等厂商有自己的 HDR 标准（如 HDR Vivid）。

Unity 对 Android HDR 的显式支持（通过 `HDROutputSettings`）在 Unity 2022.3+ 逐步完善，但测试覆盖面有限，生产环境需要谨慎评估设备兼容性。

### Mobile HDR 的实际取舍

移动端 HDR 渲染管线（RGBA16F）本身是推荐的——确保光照计算精度、Bloom 正确工作。但 HDR 显示输出（发送 10-bit PQ 信号给屏幕）在移动端大多数情况下利大于弊的前提是目标设备确实支持，否则只是增加功耗和带宽。

建议策略：默认使用 HDR 渲染管线（内部精度），HDR 显示输出根据运行时检测结果动态开启。

---

## 小结

HDR 是渲染管线中跨越多个层次的话题。在管线内部，RGBA16F 帧缓冲保留光照计算的完整动态范围，让 Bloom 和 Tonemapping 能做正确的工作；Tonemapping 是 HDR 管线必不可少的终点，曲线的选择直接影响画面风格。在显示输出层，HDR10 的 PQ 编码和 Rec.2020 色域让真实高亮度信号送达显示器；Paper White 设置确保 UI 和 SDR 内容在 HDR 显示器上不过亮。这两层都做对，才是完整的 HDR 工作流。
