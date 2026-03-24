+++
title = "Shadow Map 机制：生成、级联与阴影质量问题"
slug = "unity-rendering-02b-shadow-map"
date = 2025-01-26
description = "Shadow Map 如何从光源视角生成深度图并在渲染时进行比较，Cascade Shadow Map 为什么能兼顾近处精度和远处覆盖，Shadow Acne / Peter Panning 的成因和 Bias 的两面性，PCF / PCSS 软化阴影的采样原理。"
[taxonomies]
tags = ["Unity", "Shadow Map", "Cascade", "PCF", "阴影", "光照"]
series = ["Unity 渲染系统"]
[extra]
weight = 550
+++

如果只用一句话概括这篇：Shadow Map 是一种"先从光源视角拍一张深度照片，渲染时用来判断当前像素是否在阴影里"的技术，它的所有问题——精度不足、自遮挡、软硬边——都源于这张深度照片的分辨率和精度限制。

---

## 从上一篇出发

02（光照资产）提到了实时光的阴影由 Shadow Map 提供，并在 Pass 顺序里说明 Shadow Map 在 Opaque 渲染之前生成。但 Shadow Map 具体怎么生成、为什么要分 Cascade、Bias 调错了会出什么问题，这些都没有展开。

---

## Shadow Map 的基本原理

Shadow Map 分两个阶段：

**阶段一：Shadow Caster Pass（生成深度图）**

把摄像机放在光源位置，用光源的视角对场景做一次渲染，但只写入深度值，不计算颜色：

```
光源位置 → 构造光源的 View 矩阵和 Projection 矩阵
  ↓
对场景所有 Shadow Caster 物体做一次 Draw（只输出深度）
  ↓
得到一张 Shadow Map（深度纹理）
  每个 texel 存储的值：从光源到该方向最近的遮挡物深度
```

对于平行光（Directional Light），用正交投影；对于点光源，用六面 Cubemap 深度图；对于聚光灯，用透视投影。

**阶段二：Shadow Receiver（阴影采样）**

在不透明物体的 Fragment Shader 里，判断当前像素是否在阴影里：

```hlsl
// 伪代码，展示原理
float4 fragPos_WorldSpace = ...;

// 把当前像素坐标变换到光源的裁剪空间
float4 fragPos_LightClip = lightViewProjectionMatrix * fragPos_WorldSpace;

// 转换为 Shadow Map 的 UV 坐标和深度值
float2 shadowUV = fragPos_LightClip.xy / fragPos_LightClip.w * 0.5 + 0.5;
float currentDepth = fragPos_LightClip.z / fragPos_LightClip.w;

// 采样 Shadow Map，得到光源视角下该方向最近遮挡物的深度
float closestDepth = tex2D(shadowMap, shadowUV).r;

// 比较：当前像素深度 > 存储的最近深度 → 当前像素在遮挡物后面 → 在阴影里
float shadow = (currentDepth > closestDepth) ? 0.0 : 1.0;

// 最终颜色乘以阴影系数
finalColor *= shadow;
```

---

## Shadow Acne：自遮挡问题

Shadow Acne 是 Shadow Map 最常见的视觉问题：在应该被直接照亮的表面上出现条纹状的错误阴影（自遮挡）。

**成因：**

Shadow Map 的分辨率是有限的（比如 2048×2048）。一个 Shadow Map texel 覆盖场景中一块有限面积。当物体表面和光线的夹角比较倾斜时，同一个 texel 对应的场景表面面积很大，深度值只记录了这块面积内某一点的深度。

渲染时，表面上的像素去采样 Shadow Map，得到的深度值可能比自身的深度稍小（因为 Shadow Map 存的是比自身更近的点），导致判断为"被自己遮挡"：

```
（侧视图，地面倾斜时）

地面实际形状：  ╱╱╱╱╱╱
Shadow Map：   ▓▓▓▓▓▓  ← 每个 texel 代表一段水平深度

采样时，地面上的点 P 去取 Shadow Map：
  Shadow Map texel 存的是 P 附近的平均深度
  P 自身的深度比 texel 存的值略大（P 在 texel 对应区域的"低处"）
  → 判断 P 被自己遮挡 → 错误阴影
```

**解决方案：Shadow Bias（深度偏移）**

在深度比较时，给 Shadow Map 存的深度值加一个偏移量，让比较不那么敏感：

```hlsl
float bias = 0.005; // 固定偏移
float shadow = (currentDepth - bias > closestDepth) ? 0.0 : 1.0;
//              ^^^^^^^^^^^^^^^^ 当前深度减去偏移，等于放宽了"在阴影里"的判定门槛
```

Unity 里 Depth Bias 和 Normal Bias 都在 Light 组件的 Shadow 设置里：

- **Depth Bias**：沿光线方向的偏移量，减少 Shadow Acne
- **Normal Bias**：沿法线方向向光源侧缩进，更精确地处理斜面

---

## Peter Panning：Bias 过大的问题

Bias 不能无限加大。当 Bias 太大时，阴影接收体"逃离"了真实阴影范围，导致物体看起来浮在地面上，与投射阴影的脚部分离——这个现象叫 **Peter Panning**（像彼得·潘一样飘在空中）。

```
Bias 太小：Shadow Acne（表面自遮挡条纹）
Bias 适中：正常阴影
Bias 太大：Peter Panning（阴影与物体脚部分离）
```

调 Bias 是在两个问题之间找平衡点，对于特别薄的物体（叶片、栅栏）这个平衡点很难找，这也是 Shadow Map 的固有局限。

---

## Cascade Shadow Map：兼顾精度和覆盖范围

**问题：** 平行光需要覆盖整个场景（几百米范围），但玩家身边 5 米内的阴影需要高精度细节。一张固定分辨率的 Shadow Map 无法同时满足这两个需求：覆盖大范围时，texel 代表的世界空间面积很大，近处阴影锯齿严重。

**解法：** 把阴影距离分成多个层级（Cascade），每个层级单独生成一张 Shadow Map，近处的 Cascade 覆盖范围小但 texel 密度高，远处的 Cascade 覆盖范围大但 texel 密度低。

```
摄像机到场景的距离分层（4 级 Cascade 示意）：

  摄像机 ──[0m─────10m]───────────────────────────────── 50m ──── 150m ──────────────
                ↑               ↑                    ↑                        ↑
            Cascade 0       Cascade 1            Cascade 2               Cascade 3
           10m 范围         50m 范围             150m 范围               无限远
           高精度           中精度               低精度                  最低精度
           (大量 texel)     (中量 texel)         (少量 texel)            (极少 texel)
```

每张 Cascade Shadow Map 的分辨率相同（比如 2048×2048），但覆盖的世界空间范围不同，所以单位面积内的 texel 密度不同。

渲染时，Fragment Shader 根据当前像素到摄像机的距离，选择合适的 Cascade 层级采样：

```hlsl
// 伪代码
float depth = distance(fragPos, cameraPos);

int cascadeIndex;
if      (depth < cascade0Far) cascadeIndex = 0;
else if (depth < cascade1Far) cascadeIndex = 1;
else if (depth < cascade2Far) cascadeIndex = 2;
else                          cascadeIndex = 3;

float shadow = SampleShadowMap(shadowMaps[cascadeIndex], fragPos, ...);
```

**在 Frame Debugger 里的体现：**

URP 的 Shadow Caster Pass 会按 Cascade 数量执行多次：

```
▼ MainLightShadowCasterPass
    Draw (shadow caster, cascade 0)  ← 近处高精度
    Draw (shadow caster, cascade 0)
    ...
    Draw (shadow caster, cascade 1)  ← 稍远
    ...
    Draw (shadow caster, cascade 2)
    ...
    Draw (shadow caster, cascade 3)  ← 远处低精度
```

4 级 Cascade 的 Shadow Map 生成成本是 1 级的约 4 倍（4 次完整的 Shadow Caster Pass）。

**Unity 中的配置位置：**

```
URP Pipeline Asset → Shadows
  ├─ Max Distance：超出这个距离的物体不接收实时阴影（改为烘焙或无阴影）
  ├─ Cascade Count：1 / 2 / 3 / 4 级
  └─ Cascade Splits：每级的距离分界点（百分比）

也可以在 Quality Settings 里按平台分别配置
```

---

## PCF：软化阴影边缘

硬阴影（Hard Shadow）的边缘是像素级的锯齿，因为深度比较是二值的（在阴影里或不在）。

**PCF（Percentage Closer Filtering）** 的思路：不是采样一次做一次比较，而是在 Shadow Map 上采样多个相邻点，对比较结果做平均：

```hlsl
// 采样 Shadow Map 周围 3×3 = 9 个点，对每个点做深度比较，取平均
float shadow = 0.0;
float texelSize = 1.0 / shadowMapResolution;

for (int x = -1; x <= 1; x++)
{
    for (int y = -1; y <= 1; y++)
    {
        float2 offset = float2(x, y) * texelSize;
        float closestDepth = tex2D(shadowMap, shadowUV + offset).r;
        shadow += (currentDepth - bias > closestDepth) ? 0.0 : 1.0;
    }
}
shadow /= 9.0; // 0.0 ~ 1.0 之间的过渡值，而不是纯 0 或 1
```

PCF 的采样范围越大，阴影边缘越柔和，但采样次数越多（性能开销越高）。URP 的 Soft Shadow 选项就是 PCF，可以在 Light 组件的 Shadow Type 里设置 Hard / Soft。

**PCSS（Percentage Closer Soft Shadows）** 进一步根据遮挡物和接收面的距离动态调整 PCF 的采样半径：遮挡物越远，阴影越软（接近真实的半影效果）。HDRP 支持 PCSS，URP 的标准 Soft Shadow 是固定半径的 PCF。

---

## 常见阴影问题诊断

| 现象 | 可能原因 | 解决方向 |
|---|---|---|
| 表面出现条纹 / 噪点 | Shadow Acne，Bias 太小 | 增大 Depth Bias 或 Normal Bias |
| 物体和阴影分离（脚不踩地）| Peter Panning，Bias 太大 | 减小 Bias，或增大 Shadow Map 分辨率 |
| 近处阴影清晰，远处模糊 | Cascade 正常工作，远 Cascade 分辨率低 | 增加 Cascade 数量，调整 Split 比例 |
| 某距离之外突然没有阴影 | 超出 Max Distance | 增大 Max Distance（注意性能开销）|
| 阴影边缘硬锯齿 | Hard Shadow 模式 | 改为 Soft Shadow（PCF）|
| 半透明物体没有阴影 | 半透明物体默认不参与 Shadow Caster Pass | 开启 Material 的 Cast Shadows + Receive Shadows，或使用 Cutout 代替 Transparent |
| 动态物体阴影抖动 | Cascade 切换边界处精度跳变 | 使用 Shadow Cascade Blend（过渡区域混合两级 Cascade 结果）|

---

## RenderDoc 验证 Shadow Map

用 RenderDoc 可以直接查看生成的 Shadow Map 内容：

1. 在 Event Browser 里找到 `MainLightShadowCasterPass` 的 Draw Call 序列
2. 点击最后一个 Draw Call，切换到 Texture Viewer
3. 在输出 RT 列表里找到 Shadow Map（通常是一张深度纹理，格式 D16/D24/D32）
4. 在 Texture Viewer 里把 Range 调整到 0–1，可以看到从光源视角看到的场景深度图
5. 在 Cascade 分级的情况下，Shadow Map 通常是一张 Atlas（多个 Cascade 拼在一张大图里）

---

## 小结

Shadow Map 是一种"先拍照，后比较"的阴影技术：

```
Shadow Caster Pass：从光源视角渲染深度 → 得到 Shadow Map
Shadow Receiver：Fragment Shader 里把像素变换到光源空间 → 与 Shadow Map 深度比较 → 得到阴影系数

Cascade：把阴影距离分层，近处高精度，远处低精度，兼顾质量和性能
Bias：解决 Shadow Acne，但不能过大（Peter Panning）
PCF：多次采样取平均，软化阴影边缘
```

Shadow Map 是实时阴影的主流方案，但它本质上是一种近似，所有的调参（Cascade 数量、Split、Bias、PCF 半径）都是在精度、性能、视觉质量之间做权衡。
