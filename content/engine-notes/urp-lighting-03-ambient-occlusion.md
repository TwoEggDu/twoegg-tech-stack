---
title: "URP 深度光照 03｜Ambient Occlusion：SSAO 实现原理、参数调参与移动端策略"
slug: "urp-lighting-03-ambient-occlusion"
date: "2026-03-25"
description: "URP 里 Ambient Occlusion 的完整实现分析：SSAO 的屏幕空间采样原理、GTAO 的改进之处、URP SSAO Renderer Feature 的每个参数含义、与 Deferred 路径的关系，以及移动端降质到可接受代价的策略。"
tags:
  - "Unity"
  - "URP"
  - "SSAO"
  - "Ambient Occlusion"
  - "后处理"
  - "渲染管线"
series: "URP 深度"
weight: 1580
---
Ambient Occlusion（环境光遮蔽）模拟的是：在角落、缝隙、物体相交处，光线更难照进来，所以这些地方应该更暗。它在视觉上显著增加场景的立体感和接触感——尤其是去掉后你才会明显感觉"画面变平了"。

URP 里 AO 通过 **SSAO Renderer Feature** 实现，是屏幕空间算法，不需要预计算，可以处理动态物体。

---

## AO 的两种实现路线

游戏里 AO 有两大类实现：

**烘焙 AO（Baked AO）**：在 Lightmap 烘焙时计算，结果存在 Lightmap 贴图里。质量高，不消耗实时 GPU，但只适用于静态物体，动态物体和动态光照无效。

**屏幕空间 AO（SSAO）**：在渲染完场景后，基于屏幕上已有的深度和法线信息，实时计算每个像素的遮挡程度。支持动态物体，但只能处理屏幕范围内可见的几何体（屏幕外的遮挡看不到）。

URP 内置的是 SSAO（及其变体 GTAO）。

---

## SSAO 的基本原理

### 核心思路

对屏幕上每个像素，在其周围的半球范围内，随机采样若干个点：

1. 用当前像素的深度和法线，在世界空间重建位置
2. 在该位置的法线半球里，随机生成若干采样点
3. 把采样点投影到屏幕，查对应的深度值
4. 如果采样点的深度 > 屏幕深度（采样点在某个表面"后面"），说明该方向被遮挡
5. 被遮挡的采样点越多，该像素的 AO 值越大（越暗）

```
像素 P 的位置 = 从深度图重建
在 P 的法线半球内随机采样 N 个点 S₁, S₂, ... Sₙ
对每个 Sᵢ：
  投影到屏幕坐标 (u, v)
  从深度图读出 d = depth(u, v)
  重建对应世界坐标 Qᵢ
  如果 Sᵢ 比 Qᵢ 更深（Sᵢ 在 Qᵢ 后面）→ 被遮挡
AO(P) = 被遮挡的采样数 / N
```

### SSAO 的局限性

- **屏幕边缘伪影**：屏幕边缘外的遮挡物看不到，边缘处 AO 会有错误的亮区
- **法线采样方向偏差**：随机采样分布不均匀时，结果有噪声，需要模糊降噪
- **厚度无感知**：SSAO 无法区分"厚墙"和"薄片"，薄片可能产生过强的 AO
- **深度精度依赖**：近摄像机处精度高，远处精度低

---

## GTAO（Ground Truth Ambient Occlusion）

URP 14（Unity 2022.2）引入了 GTAO 作为 SSAO 的升级选项。

### GTAO 的改进

**SSAO 的问题**：在法线半球内随机采样，效率低且有噪声，需要大量采样点才能收敛。

**GTAO 的做法**：把半球 AO 的积分转化为在 2D 截面（Slice）上的弧长积分。对每个像素，沿若干方向做水平线搜索（Horizon Search），找到该方向上被遮挡的"地平线角"：

```
水平线搜索：
  选 N 个方向（均匀分布在 360° 里）
  沿每个方向，在屏幕空间步进，找最高深度角（Horizon Angle）
  AO 贡献 = 1 - cos(HorizonAngle)（超过地平线的部分被遮挡）
```

优势：
- 相同采样次数下，GTAO 的结果比 SSAO 更准确（噪声更少）
- 对薄面的处理更正确（通过双面搜索）
- 原生支持 Bent Normal（弯曲法线，用于 GI 的方向性遮挡，不只是强度遮挡）

---

## URP SSAO Renderer Feature 参数详解

在 Universal Renderer 里，Add Renderer Feature → Screen Space Ambient Occlusion，参数如下：

```
Screen Space Ambient Occlusion
  ├─ Downsample            ← 是否以半分辨率计算
  ├─ After Opaque          ← AO 应用时机
  ├─ Source
  │   ├─ Depth Normals     ← 用 Depth + DepthNormals RT 重建法线
  │   └─ Depth             ← 只用 Depth（不需要 DepthNormals Pass，性能更好）
  ├─ Normal Quality        ← 法线重建精度
  ├─ Intensity             ← AO 强度（0–4）
  ├─ Radius                ← 采样半径（世界单位）
  ├─ Falloff Distance      ← 超过此距离的像素不计算 AO
  └─ Blur Quality          ← 降噪模糊质量
```

### Downsample

以半分辨率（1/4 面积）计算 AO，然后双线性上采样到全分辨率。

性能：省约 75% 的 AO 计算代价。
质量：边缘处有轻微锯齿（上采样不完美），但大多数场景下肉眼很难分辨。

**移动端强烈建议开启。**

### After Opaque

- **关闭（Before Opaque）**：AO 在不透明渲染之前计算，作为 GI（间接光）的遮挡，应用到环境光上。结果更准确，但不影响直接光。
- **开启（After Opaque）**：AO 在不透明渲染之后计算，把 AO 图乘到整个颜色输出上（包括直接光）。"更黑"，但在视觉上有时反而更明显。

技术上，"Before Opaque"才是正确的物理模型（AO 只应该影响间接光）；"After Opaque"是美术上的近似，但常见于追求强对比度的游戏。

### Source：Depth vs Depth Normals

**Depth**：只使用深度图，从深度重建法线（通过相邻像素差分）。不需要额外的 DepthNormals Pass，性能更好，但法线精度较低（尤其是水平面或接近水平的面，重建法线误差大）。

**Depth Normals**：需要开启 DepthNormals Prepass（URP 会额外渲染一个 Pass，同时写深度和世界法线到 `_CameraDepthNormalsTexture`）。法线精度高，AO 质量更好，但多一个完整场景渲染 Pass。

**选择建议**：PC 用 Depth Normals，移动端用 Depth（省一个 Pass）。

### Radius

采样半径，单位是世界空间单位。控制 AO 影响的范围：

- 太小：只有很近的角落有 AO，视觉效果微弱
- 太大：大范围的面都变暗，整个场景发灰，失真

典型值：0.1–0.5 米。根据场景尺度调整：室内紧凑场景用小 Radius，室外宽阔场景可以大一些。

### Intensity

AO 的强度倍增。不改变遮挡的计算，只是把 AO 值在输出前乘以这个系数。

1.0 = 完全按计算结果。> 1.0 = 强化 AO（角落更黑）。

### Falloff Distance

超过这个距离（摄像机到像素的距离）的像素不计算 AO，直接设为 1.0（无遮挡）。

用途：远处的 AO 精度很低（深度图精度不够），而且视觉上远处的 AO 不明显。设置 Falloff Distance 可以跳过远处像素，节省计算量。

典型值：20–50 米。

---

## SSAO 在 Deferred 路径下的优化

在 Deferred 路径下，G-Buffer 里已经有精确的世界法线（`GBuffer2`），不需要重建法线，也不需要额外的 DepthNormals Pass。URP 在 Deferred 路径下自动使用 G-Buffer 法线做 SSAO，比 Forward 路径质量更高。

这是 Deferred 路径的一个优势：G-Buffer 的法线"免费"提供给 SSAO 使用。

---

## 移动端降质策略

SSAO 完整开启在中低端手机上通常消耗 3–8ms（1080P），超出预算。按影响从大到小的降质方案：

| 降质项 | 节省代价 | 质量损失 |
|---|---|---|
| 开启 Downsample | ~75% | 边缘轻微锯齿 |
| Source = Depth（不开 Depth Normals）| 省一个完整 Pass | 平面法线重建误差 |
| Falloff Distance 调小（10–20m）| 按比例节省 | 远处无 AO |
| Blur Quality = Low | ~30% | 轻微噪声 |
| Intensity 调低（0.5）| 无代价节省 | 效果减弱 |
| 完全关闭，用烘焙 AO | 0 代价 | 动态物体无 AO |

移动端推荐配置：

```
Downsample: 开启
After Opaque: 开启（省 DepthNormals Pass 的协同）
Source: Depth
Radius: 0.15
Intensity: 0.8
Falloff Distance: 20
Blur Quality: Low
```

如果以上配置仍然超过 2ms，关闭 SSAO，改用在 Lightmap 里烘焙 AO + 角色使用 AO Map。

---

## 小结

| 参数 | 核心作用 | 移动端建议 |
|---|---|---|
| Downsample | 半分辨率计算，省 75% 代价 | 开启 |
| Source | Depth Normals 更准，Depth 更省 | Depth |
| Radius | AO 影响范围（世界单位）| 0.1–0.3 |
| Intensity | AO 强度倍增 | 0.5–1.0 |
| Falloff Distance | 超过距离不计算 | 15–25m |
| Blur Quality | 降噪模糊精度 | Low |

- SSAO 是移动端"看效果 vs 看性能"最典型的权衡点——降质方案明确，按预算逐级调整
- Deferred 路径下 SSAO 质量更好（G-Buffer 法线免费），但 Deferred 本身的带宽代价不适合移动端
- 静态场景的 AO 用烘焙，动态物体的 AO 才需要 SSAO

光照与阴影层到这里结束。下一步是扩展开发层（URP扩展-01：Renderer Feature 完整开发），从零写一个完整的 RendererFeature + RenderPass，把前面的配置知识落到代码层面。
