---
title: "Shader 核心技法 05｜透明与半透明：Alpha Blend 的正确姿势"
slug: "shader-technique-05-transparency"
date: "2026-03-26"
description: "透明物体在渲染管线里有特殊处理：不写深度、必须排序、Blend 方程决定混合方式。理解 Alpha Blend / Premultiplied / Additive 三种模式，以及移动端透明物体的性能代价。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "技法"
  - "透明"
  - "Alpha Blend"
  - "半透明"
series: "Shader 手写技法"
weight: 4210
---
透明物体是渲染里最容易出错的部分——排序错误、深度穿插、移动端带宽翻倍。这篇把透明的完整机制讲清楚。

---

## 透明物体为什么特殊

不透明物体的渲染流程可以随意排序——GPU 的深度测试会自动丢弃被遮挡的片元，最终只保留最近的像素。

透明物体不一样：**半透明混合需要知道背后的颜色**，必须先渲染不透明物体，再从远到近渲染透明物体，把透明色叠加到已有颜色上。这就是为什么透明物体在 `Transparent` 队列（3000）而不是 `Geometry` 队列（2000）。

---

## Blend 方程

```hlsl
// ShaderLab 语法
Blend SrcFactor DstFactor
// 等价于：output = src * SrcFactor + dst * DstFactor
// src = 当前像素颜色，dst = 帧缓冲里已有的颜色
```

常用模式：

| 模式 | 设置 | 效果 |
|------|------|------|
| Alpha Blend（普通半透明） | `Blend SrcAlpha OneMinusSrcAlpha` | 标准透明，玻璃、UI |
| Premultiplied Alpha | `Blend One OneMinusSrcAlpha` | 粒子推荐，无黑边 |
| Additive（叠加） | `Blend One One` | 发光、火焰、激光 |
| Multiply（正片叠底） | `Blend DstColor Zero` | 阴影贴花、污迹 |
| Soft Additive | `Blend OneMinusDstColor One` | 柔和发光 |

---

## Alpha Blend Shader

```hlsl
Shader "Custom/AlphaBlend"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 0.5)
        _BaseMap   ("Base Map",   2D)    = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha   // Alpha Blend
            ZWrite Off                         // ← 透明物体不写深度
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0;
                                float3 positionWS:TEXCOORD1; float2 uv:TEXCOORD2; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionHCS = pi.positionCS;
                o.positionWS  = pi.positionWS;
                o.normalWS    = TransformObjectToWorldNormal(i.normalOS);
                o.uv          = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                Light  light    = GetMainLight();
                half   NdotL    = saturate(dot(normalWS, light.direction));

                half4 albedo  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 diffuse = albedo.rgb * light.color * NdotL;
                return half4(diffuse, albedo.a);   // ← 返回 alpha
            }
            ENDHLSL
        }
    }
}
```

关键点：`ZWrite Off` 必须加——透明物体不能写深度，否则会遮挡后面的透明物体。

---

## Premultiplied Alpha

标准 Alpha Blend（`SrcAlpha OneMinusSrcAlpha`）有个问题：贴图边缘在白色背景上看起来有黑边。这是因为贴图存储的颜色值没有预乘 Alpha——在黑色（0,0,0）与实际颜色之间有错误过渡。

Premultiplied Alpha 要求贴图在 PS 里导出时 RGB 已乘以 Alpha：

```
RGB_premul = RGB * Alpha
```

Shader 里改用 `Blend One OneMinusSrcAlpha`：

```hlsl
Blend One OneMinusSrcAlpha
// 等价于：output = src.rgb + dst * (1 - src.a)
// src.rgb 已经是 color * alpha，不需要再乘
```

Unity Particle System 默认用 Premultiplied Alpha，粒子特效贴图推荐用这个。

---

## Additive（叠加混合）

火焰、魔法特效、激光：

```hlsl
Blend One One        // output = src + dst，颜色累加
ZWrite Off
Cull Off             // 特效通常不需要背面剔除
```

叠加混合天然实现"越多越亮"的效果——多个粒子叠加颜色加深，模拟发光。

注意：叠加混合的物体在黑色背景上效果好，白色背景上会消失（颜色加到 1 之后没有区别）。

---

## 透明度排序问题

透明物体必须从远到近渲染，Unity 以物体的 `Pivot` 位置为中心点做排序——当物体相互穿插时，以 Pivot 排序会出现穿帮：

```
A 的 Pivot 比 B 更近 → A 后渲染（叠在 B 上面）
但 A 的一部分几何体在 B 后面 → 该部分本应被 B 遮挡，却渲染在 B 上面
```

**解决方案**：
1. 拆分 Mesh，每个透明部分单独成为物体
2. OIT（Order-Independent Transparency）——移动端不支持
3. Dithered Alpha（用噪声 clip 模拟半透明，走不透明队列）

**Dithered Alpha（移动端透明替代方案）：**

```hlsl
// 不用 Alpha Blend，而是用噪声 clip 模拟半透明
// 渲染队列：Geometry，ZWrite On
float2 screenPos = input.positionHCS.xy / _ScreenParams.xy;
float  dither    = frac(dot(screenPos, float2(0.25, 0.25) * _ScreenParams.xy) * 0.0625);
clip(albedo.a - dither);   // 通过噪声 clip 模拟半透明
```

牺牲了精确的 Alpha，换来不透明物体的渲染代价（ZWrite On，无排序问题，TBDR 友好）。

---

## 移动端透明物体性能代价

透明物体对移动端的性能代价极高：

1. **不写深度** → 无法 Early-Z，每个像素都执行完整 Fragment Shader
2. **Overdraw** → TBDR 的 HSR 对透明物体完全失效，透明叠加层越多越贵
3. **Blend 读写** → 每个透明像素需要读 dst + 写 output，带宽加倍

实际测量表明，移动端全屏透明物体的代价是不透明物体的 2~4 倍。

**移动端透明物体原则**：
- 粒子系统控制屏幕覆盖率（`maxParticles`、粒子大小）
- UI 半透明层尽量合并，减少叠加层数
- 远处透明物体用不透明 LOD 替代
- 能用 Alpha Test（clip）的就不用 Alpha Blend

---

## 小结

| 混合模式 | Blend 设置 | 用途 |
|---------|-----------|------|
| Alpha Blend | `SrcAlpha OneMinusSrcAlpha` | 玻璃、UI、标准半透明 |
| Premultiplied | `One OneMinusSrcAlpha` | 粒子，无黑边 |
| Additive | `One One` | 发光、火焰 |
| Multiply | `DstColor Zero` | 阴影贴花 |

| 规则 | 说明 |
|------|------|
| `ZWrite Off` | 透明物体必须关闭深度写入 |
| 渲染队列 | `Transparent`（3000），晚于不透明物体 |
| 排序 | Unity 以 Pivot 排序，穿插时需拆分 Mesh |
| 移动端 | 透明代价高，能用 clip 就不用 Blend |

下一篇：折射——用 Camera Opaque Texture 实现水下/玻璃折射扭曲效果。
