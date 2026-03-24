+++
title = "Unity 渲染系统 05｜后处理：Volume 系统与全屏 Pass 的工作机制"
description = "讲清楚 URP 后处理的 Volume 覆盖机制、全屏 Pass 对帧缓冲区的操作原理，以及 Bloom、Tonemapping、SSAO、Depth of Field 各自在像素级别做了什么。"
slug = "unity-rendering-05-postprocessing"
weight = 800
featured = false
tags = ["Unity", "Rendering", "PostProcessing", "Volume", "Bloom", "Tonemapping", "SSAO", "URP"]
series = "Unity 渲染系统"
+++

> 如果只用一句话概括这篇，我会这样说：后处理不是"给画面加滤镜"的魔法，而是在场景渲染完成后，对整张帧缓冲区做一系列全屏的 Fragment Shader 计算——每个效果是一个独立的 Pass，把上一个 Pass 的输出作为输入，串联处理最终输出到屏幕。

前几篇讲的所有内容——Mesh、Material、光照、动画、粒子——产生的都是场景里的几何渲染。这篇讲在这一切完成之后，对整张画面做的最后一层处理。

---

## 后处理是什么：全屏 Pass

场景渲染完成后，Color Buffer 里存的是 HDR（High Dynamic Range）的原始渲染结果——亮度范围可能从 0 到几百，颜色没有经过任何校正，看起来通常是偏灰、偏暗、缺乏层次的。

后处理的工作是把这张 Color Buffer 变成最终的显示画面。

每个后处理效果是一个**全屏 Pass**：

```
输入：上一个 Pass 的 Color Buffer（或 Depth Buffer）
执行：对每个像素运行一段 Fragment Shader
输出：处理后的 Color Buffer
```

多个效果串联：

```
原始 Color Buffer
  → Bloom Pass     → 中间 RT
  → Tonemapping    → 中间 RT
  → Color Grading  → 中间 RT
  → FXAA（抗锯齿） → 最终 Color Buffer → 屏幕
```

---

## Volume 系统：后处理的配置容器

URP 用 **Volume** 来管理后处理参数。Volume 不是"执行"后处理，而是配置在空间中的"参数包"——相机根据自己的位置决定使用哪些 Volume 的参数，以及各自占多大权重。

### Global Volume 和 Local Volume

**Global Volume**（`Is Global = true`）：全局生效，不受位置影响。通常放场景里一个，作为基础配置。

**Local Volume**：有一个 Collider 作为影响范围。相机进入这个 Collider 时，该 Volume 的参数开始以指定的 `Blend Distance` 渐入，离开时渐出。

用途示例：进入洞穴时，自动叠加一个"低曝光 + 强 SSAO + 色调偏冷"的 Volume，离开后渐出。无需任何代码，相机位置自动驱动混合。

### Volume 参数混合

多个 Volume 同时生效时，按优先级和混合权重混合参数：

```
最终 Bloom 阈值 = GlobalVolume.BloomThreshold × (1 - localWeight)
               + LocalVolume.BloomThreshold × localWeight
```

每个参数可以单独设置是否**覆盖（Override）**——打开 Override 才会参与混合，关闭则沿用下层的值。这允许 Local Volume 只覆盖它关心的参数，不影响其他效果。

---

## 主要后处理效果的像素级原理

### Bloom（泛光）

Bloom 让高亮区域产生向外扩散的光晕，模拟真实相机镜头在强光下的过曝效果。

**执行流程**：

```
1. 亮度提取：对每个像素，取 RGB 亮度值，低于阈值的像素置黑（只保留高亮部分）
2. 下采样（Downsample）：把高亮图多次缩小（1/2 → 1/4 → 1/8 → 1/16...）
3. 模糊（Blur）：在缩小的图上做高斯模糊（小图上模糊 = 大范围光晕扩散）
4. 上采样（Upsample）：把模糊后的图逐级放大回原始分辨率
5. 叠加：把 Bloom 结果加法混合到原始 Color Buffer 上
```

下采样再上采样的原因：直接在原始分辨率上做大范围模糊开销极高（高斯模糊的计算量和模糊半径的平方成正比）。降低分辨率后，同样的模糊 Pass 能产生更大范围的扩散效果，同时大幅降低计算量。

### Bloom 各 Pass 的 Fragment Shader 伪代码

**亮度提取 Pass**（第一步，把暗部像素置黑）：

```hlsl
float3 BrightExtractFrag(float2 uv) {
    float3 color = tex2D(_MainTex, uv).rgb;

    // 计算感知亮度（人眼对绿色最敏感）
    float brightness = dot(color, float3(0.2126, 0.7152, 0.0722));

    // 低于阈值的像素置黑（Knee 曲线让截断更柔和，这里用硬截断示意）
    float weight = max(0, brightness - _Threshold) / max(brightness, 0.0001);
    return color * weight;
}
```

**降采样 Pass**（执行 3–5 次，每次分辨率减半）：

```hlsl
// Dual Kawase 降采样：以当前像素为中心，取 4 个偏移 0.5 texel 的样本
// 比简单 4 倍降采样有更好的频率特性（减少锯齿感）
float3 DownsampleFrag(float2 uv) {
    float2 texelSize = _MainTex_TexelSize.xy; // 1 / (width, height)
    float offset = 0.5;

    float3 sum = 0;
    sum += tex2D(_MainTex, uv + float2(-offset, -offset) * texelSize).rgb;
    sum += tex2D(_MainTex, uv + float2( offset, -offset) * texelSize).rgb;
    sum += tex2D(_MainTex, uv + float2(-offset,  offset) * texelSize).rgb;
    sum += tex2D(_MainTex, uv + float2( offset,  offset) * texelSize).rgb;
    return sum * 0.25; // 取平均
}
```

**上采样 Pass**（执行相同次数，每次分辨率翻倍，同时叠加上一级结果）：

```hlsl
// Tent 滤波器：3×3 加权采样（权重呈帐篷形状，边缘权重低于中心）
float3 UpsampleFrag(float2 uv) {
    float2 texelSize = _MainTex_TexelSize.xy;
    float r = _ScatterRadius; // 扩散半径，通常 0.5–1.0

    // 3×3 Tent 权重：角落 1/16，边缘 2/16，中心 4/16
    float3 sum = 0;
    sum += tex2D(_MainTex, uv + float2(-r, -r) * texelSize).rgb * (1.0/16.0);
    sum += tex2D(_MainTex, uv + float2( 0, -r) * texelSize).rgb * (2.0/16.0);
    sum += tex2D(_MainTex, uv + float2( r, -r) * texelSize).rgb * (1.0/16.0);
    sum += tex2D(_MainTex, uv + float2(-r,  0) * texelSize).rgb * (2.0/16.0);
    sum += tex2D(_MainTex, uv                              ).rgb * (4.0/16.0);
    sum += tex2D(_MainTex, uv + float2( r,  0) * texelSize).rgb * (2.0/16.0);
    sum += tex2D(_MainTex, uv + float2(-r,  r) * texelSize).rgb * (1.0/16.0);
    sum += tex2D(_MainTex, uv + float2( 0,  r) * texelSize).rgb * (2.0/16.0);
    sum += tex2D(_MainTex, uv + float2( r,  r) * texelSize).rgb * (1.0/16.0);

    // 上采样结果 + 上一级（更高分辨率）的结果叠加
    float3 prevLevel = tex2D(_PrevLevelTex, uv).rgb;
    return sum + prevLevel;
}
```

**最终叠加**（在 UberPost Pass 里完成）：

```hlsl
float3 UberPostFrag(float2 uv) {
    float3 sceneColor = tex2D(_CameraColorTexture, uv).rgb;
    float3 bloomColor = tex2D(_BloomTexture, uv).rgb;

    // Bloom 加法叠加到场景颜色
    float3 result = sceneColor + bloomColor * _BloomIntensity;

    // 紧接着做 Tonemapping 和 Color Grading（合并在同一个 Pass）
    result = ACESFilm(result);               // Tonemapping
    result = tex3D(_ColorGradingLUT, result).rgb; // 3D LUT Color Grading

    return result;
}
```

整个 Bloom 链路的 RT 流向（以 3 次降采样为例）：

```
原始 Color Buffer（1920×1080）
  → BrightExtract → RT_Full（1920×1080，暗部置黑）
  → Downsample    → RT_Half（960×540）
  → Downsample    → RT_Quarter（480×270）
  → Downsample    → RT_Eighth（240×135）
  → Upsample      → RT_Quarter（480×270，叠加 RT_Quarter）
  → Upsample      → RT_Half（960×540，叠加 RT_Half）
  → Upsample      → RT_Full（1920×1080，叠加 RT_Full）
  → UberPost      → 最终 Color Buffer（Bloom + Tonemapping + LUT 合并执行）
```
- `Threshold`：亮度提取的阈值，值越高，越只有很亮的区域才有 Bloom
- `Intensity`：叠加的强度倍数
- `Scatter`：光晕的扩散范围

### Tonemapping（色调映射）

HDR 渲染的 Color Buffer 里，亮度值可能从 0 到数百，但屏幕只能显示 0～1 的范围。Tonemapping 把 HDR 范围压缩到 0～1，同时尽量保留亮部细节（避免纯白过曝）和暗部细节（避免纯黑死黑）。

常用的映射曲线：
- **ACES**（Academy Color Encoding System）：工业标准，高亮处有轻微压缩，整体对比度高，暗部略深。URP 默认
- **Neutral**：较为线性，颜色失真少，适合需要精确颜色还原的场景

Tonemapping 之后颜色进入 LDR（0～1），后续的 Color Grading 在 LDR 空间操作。

### Color Grading（色彩分级）

Color Grading 对颜色进行创作性调整，影响整体色调风格。在像素级别，是对每个像素的 RGB 值做一次颜色空间变换：

- **White Balance**：调整色温（冷色/暖色）和色调（偏绿/偏品红）
- **Lift / Gamma / Gain**：分别控制暗部/中间调/高光的颜色偏移
- **Hue Shift**：全局色相偏转
- **Saturation**：饱和度
- **Contrast**：对比度

Unity URP 里，Color Grading 通常会预先把调整曲线烘焙进一张 **3D LUT（Look Up Table）**：一个 32×32×32 的三维颜色查找表，存储了所有输入颜色到输出颜色的映射。Fragment Shader 执行时，只需要用当前像素的 RGB 值做一次 3D 纹理采样，就能得到调整后的颜色，开销极低。

### SSAO（屏幕空间环境遮蔽）

SSAO 让物体的角落、缝隙、相互遮挡处偏暗，增强立体感和真实感。

**执行流程**：

```
1. 在深度缓冲重建像素的世界位置
2. 对每个像素，在其周围半球方向随机采样若干点
3. 对每个采样点，检查它在深度缓冲里是否被其他几何体遮挡
4. 被遮挡的采样点比例越高，这个像素越"被周围几何体包围"，赋予更深的 AO 值
5. 对 AO 图做模糊（消除随机采样的噪点）
6. 把 AO 值乘到间接光漫反射项上（减少被遮蔽区域的环境光）
```

SSAO 是"屏幕空间"的近似，只能感知到深度缓冲里存在的几何体。被其他物体完全遮住的区域、视角外的几何体，都无法参与 AO 计算——这是 SSAO 在某些角度会失效的根本原因。

### Depth of Field（景深）

模拟相机焦外模糊效果——焦点范围内清晰，焦外模糊（Bokeh）。

**执行流程**：

```
1. 从深度缓冲计算每个像素的 CoC（Circle of Confusion，弥散圆半径）
   CoC 值由像素深度与焦点距离的差值决定
2. 按 CoC 大小，对 Color Buffer 做变化半径的模糊：
   CoC 接近 0（在焦点内）→ 不模糊
   CoC 大（远离焦点）→ 大范围模糊（产生 Bokeh 效果）
```

景深的计算量和模糊半径成正比。高品质的 Bokeh 模拟开销较高，移动端通常用低精度的近似版本或完全关闭。

---

## 后处理在 Frame Debugger 里的样子

打开 Frame Debugger，展开 PostProcessing 节点：

```
▼ PostProcessing
  ▶ Blit（UberPost）   ← 包含 Bloom 叠加 + Tonemapping + Color Grading 的合并 Pass
  ▶ Blit（FXAA）       ← 抗锯齿
```

URP 会尽量把多个后处理效果合并进一个 `UberPost` Pass，减少全屏 Pass 的数量。这意味着 Bloom、Tonemapping、Color Grading 通常在同一个 Fragment Shader 里连续执行，而不是多个独立 Pass。

点击 `UberPost` 的 Blit 事件，右侧 Properties 里能看到所有后处理参数的实际值，以及输入 RT 的内容预览。

---

## 后处理的性能开销

**每个全屏 Pass 的开销正比于屏幕分辨率**：2560×1440 的屏幕，每个像素都要执行一次 Fragment Shader。分辨率翻倍，开销翻四倍。移动端后处理效果的精简是重要的性能优化方向。

**Bloom 的下采样链**：屏幕越大，Bloom 需要的下采样级别越多，开销越高。可以通过限制最大迭代次数（`Max Iterations`）或在移动端降低采样质量来控制。

**SSAO 的采样数**：采样数越多，噪点越少，质量越高，但开销也越高。通常在高端平台用 16 或 32 采样，移动端用 4～8 采样配合更强的模糊。

**合理取舍**：Tonemapping 和基础 Color Grading（LUT 采样）几乎是必须的且开销低；Bloom 开销中等但视觉提升显著；SSAO 和 DOF 在移动端通常是最先被砍的。

---

## 资产篇总结

至此，"Unity 渲染系统"的资产篇六篇文章全部完成：

```
00 综述       → 建立所有渲染资产的全局地图
01 几何与表面  → Mesh + Material + Texture 的数据路径
01b～01f      → Draw Call、Render Target、Frame Debugger、RenderDoc
02 光照资产   → 四条光照路径的产生机制与合并
03 动画变形   → 骨骼蒙皮与 Blend Shape 的顶点变形
04 粒子特效   → 动态几何生成与半透明渲染
05 后处理     → Volume 系统与全屏 Pass 串联
```

下一组文章进入管线篇：Unity 的固定渲染管线是什么、SRP 解决了什么问题、URP 的完整架构。
