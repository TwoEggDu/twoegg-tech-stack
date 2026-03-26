---
title: "Shader 进阶技法 01｜软粒子：深度交叉渐变消除硬边"
slug: "shader-advanced-01-soft-particles"
date: "2026-03-26"
description: "普通粒子和场景相交时会出现硬边切割。软粒子通过比较粒子深度和场景深度，在交叉处渐变 alpha，消除硬边。理解 Camera Depth Texture 的采样和深度差计算。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "软粒子"
  - "粒子"
  - "深度"
series: "Shader 手写技法"
weight: 4290
---
烟雾、水花、火焰粒子与场景几何体相交时，会出现明显的硬边切割——粒子的矩形 Quad 和地面的交叉线暴露无遗。软粒子（Soft Particles）通过深度比较，让交叉区域的 alpha 渐变到 0，消除硬边。

---

## 原理

粒子是一张 Quad（两个三角形），渲染时有自己的深度值。场景里（地面、墙壁）已经渲染完毕，深度缓冲里存了场景的深度。

软粒子的逻辑：

```
场景深度 - 粒子深度 = 深度差
深度差很小 → 粒子刚好贴着场景 → 渐变到透明
深度差很大 → 粒子漂浮在空中 → 正常显示
```

---

## 开启 Depth Texture

需要在 URP 里先开启深度图：

**URP Asset → Depth Texture** 勾选（或 Camera 的 Depth Texture 单独开启）。

```hlsl
// 深度图自动可用：
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
```

---

## 深度值读取与线性化

深度缓冲存的是**非线性深度**（近处精度高，远处精度低）。做深度差比较前需要线性化：

```hlsl
// 采样场景深度
float2 screenUV   = input.screenPos.xy / input.screenPos.w;
float  rawDepth   = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;

// 非线性深度 → 线性深度（观察空间，单位：世界单位）
float  sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

// 粒子自身的线性深度
float  particleLinearDepth = -TransformWorldToView(input.positionWS).z;
// 或者直接用 screenPos.w（已经是线性眼空间深度）
float  particleLinearDepth2 = input.screenPos.w;
```

---

## 完整软粒子 Shader

```hlsl
Shader "Custom/SoftParticle"
{
    Properties
    {
        _BaseMap      ("Texture",        2D)           = "white" {}
        _BaseColor    ("Color",          Color)         = (1,1,1,1)
        _SoftDistance ("Soft Distance",  Float)         = 1.0   // 渐变过渡距离（世界单位）
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
               "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _SoftDistance;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float4 color       : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos   = ComputeScreenPos(output.positionHCS);
                output.color       = input.color;
                output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ── 屏幕 UV ──────────────────────────────────────
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // ── 场景线性深度 ──────────────────────────────────
                float rawDepth       = SampleSceneDepth(screenUV);
                float sceneDepth     = LinearEyeDepth(rawDepth, _ZBufferParams);

                // ── 粒子线性深度（screenPos.w = 眼空间深度）────────
                float particleDepth  = input.screenPos.w;

                // ── 深度差 → 软化 alpha ───────────────────────────
                float depthDiff  = sceneDepth - particleDepth;
                float softFactor = saturate(depthDiff / max(_SoftDistance, 0.001));

                // ── 采样贴图 ──────────────────────────────────────
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color    = texColor * _BaseColor * input.color;

                // 交叉区域渐变透明
                color.a *= softFactor;

                return color;
            }
            ENDHLSL
        }
    }
}
```

---

## `SampleSceneDepth`

URP 提供了 `DeclareDepthTexture.hlsl`，封装了深度图采样：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float rawDepth = SampleSceneDepth(screenUV);
// 等价于：SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r
```

---

## Particle System 集成

Unity Particle System 的 Renderer 组件有内置软粒子支持：

`Particle System → Renderer → Rendering Mode → Billboard → Max Particle Size`，同时勾选 Soft Particles 需要在 Project Settings → Graphics 里开启（仅限内置管线）。

URP 下通常直接用自定义 Shader，如上所示。Particle System 的 `Custom Vertex Streams` 可以把粒子颜色（alpha）自动传入 `COLOR` 语义，与 Shader 的 `input.color.a` 对接。

---

## 性能注意

- 每个粒子片元多一次深度图采样
- 移动端深度图本身有额外 RT 开销（需开启 Depth Texture）
- 大量粒子全用软粒子代价较高；只对与场景明显相交的效果（烟雾、水花）开启

---

## 小结

| 概念 | 要点 |
|------|------|
| 硬边原因 | 粒子 Quad 与场景几何相交，深度突变 |
| 软化原理 | 深度差小 → alpha 渐变到 0 |
| 深度线性化 | `LinearEyeDepth(rawDepth, _ZBufferParams)` |
| 粒子深度 | `screenPos.w` = 眼空间线性深度 |
| 开启条件 | URP Asset 开启 Depth Texture |

下一篇：Stencil 高级用法——传送门、遮罩区域、卡通描边，用模板缓冲实现复杂的渲染分层。
