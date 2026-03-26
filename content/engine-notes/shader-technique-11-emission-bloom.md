+++
title = "Shader 核心技法 11｜自发光与 Bloom 联动：HDR 颜色驱动光晕"
slug = "shader-technique-11-emission-bloom"
date = 2026-03-26
description = "自发光（Emission）的颜色值超过 1 时，Post Processing 的 Bloom 会拾取它产生光晕效果。理解 HDR 颜色、Bloom 的工作原理，以及如何用动画驱动自发光强度做出脉冲发光效果。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "技法", "自发光", "Emission", "Bloom", "HDR"]
series = ["Shader 手写技法"]
[extra]
weight = 4270
+++

游戏里发光的符文、能量球、霓虹灯——它们的光晕效果（Bloom）不是 Shader 直接画出来的，而是 Shader 输出超过 1 的 HDR 颜色，由后处理的 Bloom pass 自动产生。理解这条流程，才能精确控制发光效果。

---

## HDR 与 Bloom 的工作原理

**HDR（High Dynamic Range）**：允许颜色值超过 1.0。现实中的光源亮度远超普通表面，HDR 颜色值模拟这种差异。

**Bloom 的流程**：
1. 场景渲染到 HDR RT（颜色值可以超过 1）
2. Bloom Pass 提取亮度 > 1 的像素（Threshold 阈值可调）
3. 对这些亮像素做高斯模糊，产生光晕
4. 把模糊结果叠加回原图

所以自发光颜色超过 1 才会触发 Bloom。颜色值 = 1 是"正常白色"，不产生光晕。

---

## 开启 HDR 渲染

确保以下设置开启：
- URP Asset → `HDR` 勾选
- Camera → `Rendering → Post Processing` 勾选（或 URP Asset 里全局开启）
- `Volume → Bloom` 组件添加并开启，设置 Threshold（通常 0.9~1.1）

---

## Shader 里的自发光

在 Properties 里用 `[HDR]` 标签声明发光颜色，允许 Inspector 里的颜色拾取器超出 0-1：

```hlsl
Properties
{
    [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
}
```

`[HDR]` 标签让颜色拾取器进入 HDR 模式，可以设置 Intensity（强度倍数）：
- Intensity = 0：颜色 RGB 正常（0~1）
- Intensity = 1：颜色乘以 2（1~2），触发 Bloom
- Intensity = 2：颜色乘以 4（2~4），强烈光晕

在 Fragment Shader 里直接加到最终颜色：

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _EmissionColor;   // 可以是 HDR 值
CBUFFER_END

// Fragment Shader 末尾：
half3 emission   = _EmissionColor.rgb;
// 可选：乘以贴图遮罩，让发光只出现在特定区域
emission        *= SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, input.uv).r;

finalColor.rgb  += emission;
return half4(finalColor, 1.0);
```

---

## 脉冲发光动画

用 sin 驱动发光强度，产生呼吸/脉冲效果：

```hlsl
// 方案 1：直接对 EmissionColor 强度做动画
float pulse   = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;   // 0~1 循环
half3 emission = _EmissionColor.rgb * (pulse * _MaxIntensity + _MinIntensity);

// 方案 2：更有节奏感的脉冲（快闪 + 缓慢淡出）
float t       = frac(_Time.y * _PulseSpeed);
float pulse   = pow(1.0 - t, 3.0);   // 快速下降曲线
half3 emission = _EmissionColor.rgb * pulse * _MaxIntensity;
```

代码控制（C# 侧）：

```csharp
// 在 Update 里修改材质属性
float intensity = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * maxIntensity;
material.SetColor("_EmissionColor", baseEmissionColor * intensity);
// 注意：这里 baseEmissionColor 应该是 HDR 颜色（归一化后的方向）
```

---

## Emission Mask 贴图

只让贴图的部分区域发光：

```hlsl
// _EmissionMask：R 通道 = 发光区域（白色发光，黑色不发光）
half mask     = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, input.uv).r;
half3 emission = _EmissionColor.rgb * mask;
finalColor.rgb += emission;
```

常见用途：角色眼睛发光（mask 只覆盖眼部），武器刻纹发光（mask 按纹路绘制）。

---

## 完整自发光 Shader 片段

```hlsl
Properties
{
    _BaseColor      ("Base Color",      Color)  = (1,1,1,1)
    _BaseMap        ("Base Map",        2D)     = "white" {}
    [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
    _EmissionMap    ("Emission Map",    2D)     = "white" {}
    _PulseSpeed     ("Pulse Speed",     Float)  = 1.0
    _PulseMinMax    ("Pulse Min Max",   Vector) = (0.5, 2.0, 0, 0)
}

// CBUFFER：
float4 _EmissionColor;
float  _PulseSpeed;
float4 _PulseMinMax;   // x=min, y=max

// Fragment Shader：
half4 albedo     = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
half  emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).r;

// 脉冲动画
float pulse      = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
float intensity  = lerp(_PulseMinMax.x, _PulseMinMax.y, pulse);
half3 emission   = _EmissionColor.rgb * emissionMask * intensity;

// ... 光照计算 ...
finalColor.rgb += emission;
return half4(finalColor, albedo.a);
```

---

## Bloom 参数调节

| 参数 | 效果 |
|------|------|
| Threshold | 从多少亮度开始提取，通常 0.9~1.0 |
| Intensity | Bloom 光晕的整体强度 |
| Scatter | 光晕扩散范围（越大越弥漫） |
| Clamp | 最大亮度上限，防止过曝 |

Shader 里发光颜色 × intensity 越高，Bloom 光晕越明显。通常：
- 轻微发光（UI 图标）：intensity = 1~2
- 能量球、魔法：intensity = 3~8
- 爆炸闪光：intensity = 10+（短暂帧）

---

## 自发光与烘焙

勾选 `Contribute Global Illumination` 后，自发光会参与烘焙——高亮的发光物体会给周围物体投下烘焙的自发光颜色。这适合固定的发光装置（路灯、壁灯），不适合动画发光（烘焙是静态的）。

---

## 小结

| 概念 | 要点 |
|------|------|
| HDR 颜色 | 值 > 1 触发 Bloom，`[HDR]` 标签让 Inspector 可调 |
| Bloom 流程 | 提取亮像素 → 高斯模糊 → 叠加 |
| Emission Mask | R 通道控制发光区域 |
| 脉冲动画 | `sin(_Time.y * speed)` 驱动强度 |
| 开启条件 | URP Asset HDR + Camera PostProcess + Volume Bloom |

下一篇：顶点色驱动——让美术在模型上刷数据，驱动遮罩、混合、弯曲权重等效果。
