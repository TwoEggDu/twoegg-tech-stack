---
title: "Shader 核心技法 01｜UV 动画：滚动、旋转与扭曲"
slug: "shader-technique-01-uv-animation"
date: "2026-03-26"
description: "用时间驱动 UV 坐标，实现贴图滚动、旋转、扭曲三种动画效果。掌握 UV 操作的基本模式，理解 frac/sin/矩阵旋转在 UV 空间的含义。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "技法"
  - "UV动画"
  - "滚动"
  - "扭曲"
series: "Shader 手写技法"
weight: 4170
---
UV 动画是 Shader 里最高效的动画手段之一——无需骨骼、无需 Animator，只需在 Fragment Shader 里修改采样 UV，贴图就能动起来。传送带、水流、火焰、魔法阵，都可以用这种方式实现。

---

## 一、UV 滚动

最基础的 UV 动画：让 UV 随时间线性偏移。

```hlsl
// 在 Fragment Shader 里，采样之前修改 UV
float2 scrolledUV = input.uv + float2(_ScrollX, _ScrollY) * _Time.y;
half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, scrolledUV);
```

`_Time.y` 是游戏时间（秒），乘以速度参数得到每帧的偏移量。

**frac 控制：**

`scrolledUV` 超出 0-1 范围时，贴图的 Wrap Mode 决定行为：
- `Repeat`（默认）：自动平铺，超出 1 从 0 重新开始
- `Clamp`：夹到边界颜色

如果贴图 Wrap Mode 是 Repeat，UV 超出 0-1 完全没问题。手动用 `frac` 效果等价：

```hlsl
float2 scrolledUV = frac(input.uv + float2(_ScrollX, _ScrollY) * _Time.y);
```

**双层叠加（水面常用）：**

两层贴图以不同速度和方向滚动，叠加后产生自然的水波感：

```hlsl
float2 uv1 = input.uv + float2( _Speed, 0) * _Time.y;
float2 uv2 = input.uv + float2(-_Speed * 0.7, _Speed * 0.3) * _Time.y;

half4 layer1 = SAMPLE_TEXTURE2D(_WaterTex, sampler_WaterTex, uv1);
half4 layer2 = SAMPLE_TEXTURE2D(_WaterTex, sampler_WaterTex, uv2);
half4 color  = (layer1 + layer2) * 0.5;
```

---

## 二、UV 旋转

以 UV 中心（0.5, 0.5）为圆心旋转：

```hlsl
float angle = _RotateSpeed * _Time.y;   // 随时间增加的角度（弧度）
float s = sin(angle);
float c = cos(angle);

// 以 (0.5, 0.5) 为中心旋转
float2 centered = input.uv - 0.5;
float2 rotated  = float2(
    centered.x * c - centered.y * s,
    centered.x * s + centered.y * c
);
float2 finalUV = rotated + 0.5;

half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, finalUV);
```

旋转矩阵是标准的 2D 旋转：`[cos, -sin; sin, cos]`。

**魔法阵效果：** 多层贴图以不同速度正反旋转叠加：

```hlsl
float2 uv1 = Rotate(input.uv,  _Speed * _Time.y);
float2 uv2 = Rotate(input.uv, -_Speed * 0.6 * _Time.y);
half4  color = SAMPLE_TEXTURE2D(_Rune1, sampler_Rune1, uv1)
             + SAMPLE_TEXTURE2D(_Rune2, sampler_Rune2, uv2);
```

---

## 三、UV 扭曲

用噪声贴图的值作为 UV 偏移，产生扭曲效果（热浪、水面折射、传送门）：

```hlsl
// 采样噪声贴图，把值从 [0,1] 映射到 [-1,1]
float2 noiseUV   = input.uv * _NoiseScale + float2(0, _Time.y * _FlowSpeed);
float2 distortion = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).rg - 0.5) * 2.0;

// 把噪声偏移加到原始 UV
float2 distortedUV = input.uv + distortion * _DistortStrength;
half4  color       = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, distortedUV);
```

`_DistortStrength` 控制扭曲幅度，通常在 0.01~0.1 之间。

**流动扭曲（熔岩/火焰）：** 噪声 UV 向上滚动，产生向上涌动的扭曲感：

```hlsl
float2 flowUV    = float2(input.uv.x, input.uv.y - _Time.y * _FlowSpeed);
float2 noise     = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV * _NoiseScale).rg;
float2 finalUV   = input.uv + (noise - 0.5) * _DistortStrength;
```

---

## 四、极坐标 UV

把 UV 从直角坐标转成极坐标，产生径向对称效果（光环、旋涡）：

```hlsl
float2 centered = input.uv - 0.5;
float  radius   = length(centered);                    // 0 到 ~0.7
float  angle    = atan2(centered.y, centered.x);       // -π 到 π

// 映射到 [0,1] 的极坐标 UV
float2 polarUV = float2(
    angle / (2.0 * PI) + 0.5,   // 角度 → 0~1
    radius * 2.0                 // 半径 → 0~1（圆内）
);

// 加旋转动画
polarUV.x = frac(polarUV.x + _Time.y * _RotSpeed);

half4 color = SAMPLE_TEXTURE2D(_RingTex, sampler_RingTex, polarUV);
```

---

## 五、Sprite Sheet 逐帧动画

把多帧动画排列在一张贴图里（Sprite Sheet），通过 UV 偏移逐帧播放：

```hlsl
float totalFrames = _Cols * _Rows;
float frame       = floor(frac(_Time.y * _FPS) * totalFrames);  // 当前帧索引

float col = fmod(frame, _Cols);           // 列索引
float row = floor(frame / _Cols);         // 行索引

float2 frameSize = float2(1.0 / _Cols, 1.0 / _Rows);
float2 frameUV   = (input.uv + float2(col, _Rows - 1.0 - row)) * frameSize;

half4 color = SAMPLE_TEXTURE2D(_Sheet, sampler_Sheet, frameUV);
```

---

## Properties 模板

```hlsl
Properties
{
    _BaseMap      ("Base Map",       2D)    = "white" {}
    _ScrollX      ("Scroll X",       Float) = 0.1
    _ScrollY      ("Scroll Y",       Float) = 0.0
    _RotateSpeed  ("Rotate Speed",   Float) = 0.5
    _NoiseTex     ("Noise Texture",  2D)    = "gray" {}
    _DistortStrength ("Distort Strength", Float) = 0.05
    _FlowSpeed    ("Flow Speed",     Float) = 0.3
    _NoiseScale   ("Noise Scale",    Float) = 1.0
}
```

---

## 小结

| 效果 | 核心操作 | 典型用途 |
|------|---------|---------|
| 滚动 | `uv + speed * _Time.y` | 传送带、水流、天空云 |
| 旋转 | 2D 旋转矩阵，以 0.5 为中心 | 魔法阵、旋涡 |
| 扭曲 | 噪声偏移 UV | 热浪、折射、传送门 |
| 极坐标 | `atan2 / length` 转换 | 光环、径向效果 |
| 逐帧 | `floor(frac(t*fps) * frames)` | 爆炸、烟雾序列帧 |

下一篇：溶解效果——噪声贴图 + clip，带发光边缘的进阶实现。
