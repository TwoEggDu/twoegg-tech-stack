---
title: "Shader 核心技法 07｜高度雾与自定义雾效"
slug: "shader-technique-07-height-fog"
date: "2026-03-26"
description: "Unity 内置雾只有线性/指数两种，无法做高度雾（低处浓厚、高处稀薄）。在 Shader 里用世界 Y 坐标手写高度雾，配合距离雾，实现层叠式大气效果。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "技法"
  - "雾效"
  - "高度雾"
  - "大气"
series: "Shader 手写技法"
weight: 4230
---
Unity 内置的 `MixFog` 函数只做基于距离的指数/线性雾，不支持高度雾（低处浓厚、高处稀薄）。游戏里常见的山谷晨雾、海面薄雾，需要在 Shader 里手写高度雾。

---

## Unity 内置雾回顾

URP Shader 里开启内置雾的完整流程：

```hlsl
// 1. 声明关键字
#pragma multi_compile_fog

// 2. Vertex Shader：计算雾因子
output.fogFactor = ComputeFogFactor(posCS.z);

// 3. Fragment Shader：混合雾色
finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
```

`ComputeFogFactor` 根据深度计算 0~1 的雾浓度，`MixFog` 混入 `unity_FogColor`。

内置雾可以在 `Lighting → Environment → Other Settings` 里设置模式（Linear/Exponential）和参数。

---

## 高度雾原理

高度雾的雾浓度由**世界空间 Y 坐标**决定：Y 越低，雾越浓。

最简单的形式：

```hlsl
// 世界 Y 越低 → 雾浓度越高
float heightFog = 1.0 - saturate((positionWS.y - _FogMinHeight) /
                                  (_FogMaxHeight - _FogMinHeight));
// positionWS.y < _FogMinHeight → heightFog = 1（完全是雾）
// positionWS.y > _FogMaxHeight → heightFog = 0（无雾）
// 中间：线性过渡
```

---

## 距离衰减叠加

单纯高度雾在远处效果不好——远处山顶也该有一些雾气。叠加距离雾：

```hlsl
// 距离雾（指数）
float dist        = length(positionWS - _WorldSpaceCameraPos.xyz);
float distFog     = 1.0 - exp(-dist * _DistFogDensity);

// 高度雾（线性）
float heightFog   = 1.0 - saturate((positionWS.y - _FogMin) / (_FogMax - _FogMin));

// 叠加：取较大值（远处或低处都有雾）
float fogAmount   = max(distFog, heightFog * _HeightFogStrength);
fogAmount         = saturate(fogAmount);
```

---

## 完整高度雾函数

封装成可复用的函数：

```hlsl
// 高度雾 + 距离雾叠加
float ComputeHeightFog(float3 posWS, float3 camPos,
                        float fogMin, float fogMax, float fogStrength,
                        float distDensity)
{
    // 高度分量
    float height    = saturate((posWS.y - fogMin) / max(fogMax - fogMin, 0.001));
    float heightFog = (1.0 - height) * fogStrength;

    // 距离分量（指数）
    float dist    = length(posWS - camPos);
    float distFog = 1.0 - exp(-dist * distDensity);

    return saturate(max(heightFog, distFog));
}
```

在 Fragment Shader 里使用：

```hlsl
float fogAmount = ComputeHeightFog(
    input.positionWS, _WorldSpaceCameraPos.xyz,
    _FogMin, _FogMax, _HeightFogStrength, _DistFogDensity
);
half3 fogColor   = lerp(_FogColorBottom.rgb, _FogColorTop.rgb,
                        saturate((input.positionWS.y - _FogMin) / (_FogMax - _FogMin)));
finalColor.rgb   = lerp(finalColor.rgb, fogColor, fogAmount);
```

两个雾色（`_FogColorBottom` / `_FogColorTop`）按高度渐变，低处偏冷色（蓝灰），高处偏暖色（橙黄），产生更自然的大气感。

---

## 完整 Shader 集成

```hlsl
Properties
{
    // ... 已有属性 ...
    _FogColorBottom ("Fog Color Bottom", Color) = (0.5, 0.6, 0.7, 1)
    _FogColorTop    ("Fog Color Top",    Color) = (0.8, 0.7, 0.6, 1)
    _FogMin         ("Fog Min Height",   Float) = 0.0
    _FogMax         ("Fog Max Height",   Float) = 10.0
    _HeightFogStrength ("Height Fog Strength", Range(0,1)) = 0.8
    _DistFogDensity ("Distance Fog Density", Float) = 0.005
}

// CBUFFER 里加：
float4 _FogColorBottom;
float4 _FogColorTop;
float  _FogMin;
float  _FogMax;
float  _HeightFogStrength;
float  _DistFogDensity;

// Fragment Shader 末尾，在返回之前：
float fogT      = saturate((input.positionWS.y - _FogMin) / max(_FogMax - _FogMin, 0.001));
half3  fogColor = lerp(_FogColorBottom.rgb, _FogColorTop.rgb, fogT);

float height    = 1.0 - fogT;
float distFog   = 1.0 - exp(-length(input.positionWS - _WorldSpaceCameraPos.xyz) * _DistFogDensity);
float fogAmount = saturate(max(height * _HeightFogStrength, distFog));

finalColor.rgb = lerp(finalColor.rgb, fogColor, fogAmount);
```

---

## 与 URP 内置雾共存

如果场景同时使用 URP 内置雾（`MixFog`）和自定义高度雾，可以选择：

**方案 A：替代内置雾**
去掉 `#pragma multi_compile_fog` 和 `MixFog`，完全用自定义雾。

**方案 B：叠加**
先用 `MixFog` 混入距离雾，再叠加高度雾。适合只需要在现有雾基础上加高度效果的场景。

```hlsl
// 先内置雾
finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

// 再叠加高度雾
float heightFog = (1.0 - saturate((posY - _FogMin) / (_FogMax - _FogMin))) * _HeightFogStrength;
finalColor.rgb  = lerp(finalColor.rgb, _FogColorBottom.rgb, saturate(heightFog));
```

---

## 体积雾（更进阶）

真正的体积雾需要光线步进（Ray Marching），在 URP 里用 Renderer Feature 实现。步骤：

1. 从摄像机出发沿视线方向步进 N 步
2. 每步采样雾密度场（3D Noise 贴图）
3. 累积透过率和散射光

这属于进阶技法范畴，本篇只覆盖不需要额外 Pass 的轻量高度雾。

---

## 小结

| 概念 | 要点 |
|------|------|
| 高度雾 | `(1 - saturate((y - fogMin) / (fogMax - fogMin)))` |
| 距离雾 | `1 - exp(-dist * density)` 指数衰减 |
| 两色渐变 | 按 Y 高度 lerp 底部色和顶部色 |
| 叠加 | `max(heightFog, distFog)`，取最浓的值 |
| 内置雾 | 可以与 `MixFog` 叠加，也可以完全替代 |

下一篇：Decal 投影——把贴图投影到任意表面（弹孔、泥土、符文印记），不修改原模型 UV。
