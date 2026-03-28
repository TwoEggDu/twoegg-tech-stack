---
title: "游戏常用效果｜扭曲特效：屏幕空间 UV 偏移实现热浪与传送门"
slug: shader-fx-01-distortion
date: "2026-03-28"
description: "讲解屏幕空间扭曲特效的完整实现流程，包括 URP 中获取 OpaqueTexture、法线图驱动 UV 偏移、热浪和传送门效果的具体参数设置，附可配置的通用扭曲 Shader。"
tags: ["Shader", "HLSL", "URP", "特效", "扭曲", "屏幕空间", "热浪"]
series: "Shader 手写技法"
weight: 4610
---

扭曲特效的原理比它的视觉效果简单得多：在采样屏幕颜色时，把原本对准当前像素的 UV 偏移一个小量，周围的颜色就被"拉"过来了，产生折射扭曲的视觉。热浪、热气、传送门、魔法护盾边缘、水面折射——这类效果全都基于这一个核心操作，区别只在于用什么驱动 UV 偏移量的变化，以及偏移区域怎么遮罩。

---

## 扭曲特效的原理

Shader 通常只能控制自己覆盖区域的颜色。要读取屏幕上其他位置的颜色，需要一张屏幕颜色的快照——在 URP 里叫 `_CameraOpaqueTexture`。

使用前提是在 **URP Renderer Asset** 里勾选 **Opaque Texture**，否则这张贴图不存在。勾选后，URP 在 Opaque 物体渲染完毕后，将场景颜色复制到 `_CameraOpaqueTexture`，透明物体的 Shader 可以采样它。

扭曲 Shader 属于 Transparent 物体，渲染在 Opaque 之后，可读到完整的背景颜色：

```hlsl
// 在 Shader 中声明
TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

// Fragment 中计算屏幕 UV
float4 screenPos = ComputeScreenPos(positionCS);
float2 screenUV  = screenPos.xy / screenPos.w;

// 加上偏移量采样背景
float3 bgColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,
                                   sampler_CameraOpaqueTexture,
                                   screenUV + uvOffset).rgb;
```

---

## 扰动法线贴图驱动 UV 偏移

UV 偏移量来自法线贴图。法线贴图的 XY 分量解包后值域是 [-1, 1]，直接作为偏移量：

```hlsl
float3 distortNormal = UnpackNormal(
    SAMPLE_TEXTURE2D(_DistortMap, sampler_DistortMap, distortUV));

// 只取 XY 分量作为屏幕空间偏移
float2 uvOffset = distortNormal.xy * _DistortStrength;
```

`_DistortStrength` 控制偏移幅度，典型值 0.02~0.1。过大时采样到的颜色离本来位置太远，失去折射感，变成明显的位移错位。

---

## 热浪效果

热浪的特征是持续振动、慢速流动、随时间振荡强弱。用 `_Time.y` 驱动 UV 流动，叠加 sin 波形制造振荡感：

```hlsl
// 主流动方向：沿 Y 轴向上（热气上升）
float2 distortUV = IN.uv * _DistortTiling + float2(0.0, _Time.y * _FlowSpeed);

// sin 波振荡控制扭曲强度（热浪有强弱交替的节奏感）
float osc      = sin(_Time.y * _OscillateFreq) * 0.5 + 0.5;
float strength = _DistortStrength * osc;

float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_DistortMap, sampler_DistortMap, distortUV));

// 叠加第二层不同方向的扰动，打破规律性
float2 distortUV2 = IN.uv * _DistortTiling * 0.7
                  + float2(_Time.y * _FlowSpeed * 0.4, 0.0);
float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_DistortMap, sampler_DistortMap, distortUV2));

float2 offset = (n1.xy + n2.xy * 0.5) * strength;
```

热浪通常没有自身可见的颜色——只影响背景颜色，最终输出就是偏移采样到的背景色，Alpha 完全透明（不透明度为 0 的对象在场景中不可见，但后处理效果正常工作）。实际实现时用 `Blend SrcAlpha OneMinusSrcAlpha` + Alpha 为 0 即可。

---

## 传送门效果：圆形 Mask + 边缘发光

传送门在热浪基础上增加圆形遮罩和边缘发光：

```hlsl
// 圆形遮罩：从中心到边缘的距离决定可见范围
float2 centered  = IN.uv - 0.5;           // uv 中心化
float  dist      = length(centered);
// smoothstep 做软边：圆形内部 alpha=1，外部 alpha=0
float  circleMask = 1.0 - smoothstep(0.45 - _RimWidth, 0.5, dist);

// 扭曲强度也受遮罩控制：边缘扭曲强，中心扭曲弱
float2 offset = (n1.xy + n2.xy * 0.5) * _DistortStrength * circleMask;

// 边缘发光：在圆形边界处叠加一圈颜色
float rimMask  = smoothstep(0.45 - _RimWidth * 2.0, 0.45, dist)
               * (1.0 - smoothstep(0.45, 0.5, dist));
float3 rimColor = _RimColor.rgb * rimMask * _RimIntensity;
```

圆形内部采样扭曲后的背景颜色，外部完全透明，边缘加一圈颜色。改变 `_RimColor` 就能在魔法蓝、传送橙红、时间裂缝紫之间切换风格。

---

## 完整通用扭曲 Shader

```hlsl
Shader "Custom/ScreenDistortion"
{
    Properties
    {
        _DistortMap      ("Distort Normal Map", 2D)         = "bump"  {}
        _DistortStrength ("Distort Strength",   Range(0,0.1)) = 0.02
        _DistortTiling   ("Distort Tiling",     Float)      = 2.0
        _FlowSpeed       ("Flow Speed",         Float)      = 0.5
        _FlowDirection   ("Flow Direction (XY)", Vector)    = (0, 1, 0, 0)
        _OscillateFreq   ("Oscillate Frequency", Float)     = 2.0

        [Header(Mask)]
        _MaskTex         ("Mask (Alpha channel)", 2D)       = "white" {}
        _UseMask         ("Use Mask",             Float)    = 0

        [Header(Rim)]
        _RimColor        ("Rim Color",    Color)            = (0.5, 0.8, 1.0, 1)
        _RimIntensity    ("Rim Intensity", Float)           = 2.0
        _RimWidth        ("Rim Width",    Range(0, 0.2))    = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "Distortion"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_DistortMap);          SAMPLER(sampler_DistortMap);
            TEXTURE2D(_MaskTex);             SAMPLER(sampler_MaskTex);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _DistortMap_ST;
                float  _DistortStrength;
                float  _DistortTiling;
                float  _FlowSpeed;
                float4 _FlowDirection;
                float  _OscillateFreq;
                float  _UseMask;
                float4 _RimColor;
                float  _RimIntensity;
                float  _RimWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _DistortMap);
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 流动方向归一化（避免零向量）
                float2 flowDir = normalize(_FlowDirection.xy + float2(0.0001, 0));

                // 两层流动 UV（不同速度打破规律感）
                float2 uv1 = IN.uv * _DistortTiling + flowDir       * _Time.y * _FlowSpeed;
                float2 uv2 = IN.uv * _DistortTiling * 0.7
                           + flowDir.yx * _Time.y * _FlowSpeed * 0.5;

                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_DistortMap, sampler_DistortMap, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_DistortMap, sampler_DistortMap, uv2));

                // sin 振荡强度
                float osc = sin(_Time.y * _OscillateFreq) * 0.5 + 0.5;

                // 遮罩权重（可选）
                float maskW = 1.0;
                if (_UseMask > 0.5)
                    maskW = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).a;

                // UV 偏移量
                float2 offset = (n1.xy + n2.xy * 0.5) * _DistortStrength * osc * maskW;

                // 采样扭曲后的背景
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float3 bgColor  = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,
                                                    sampler_CameraOpaqueTexture,
                                                    screenUV + offset).rgb;

                // 圆形遮罩 + 边缘发光
                float2 centered = IN.uv - 0.5;
                float  dist     = length(centered);
                float  alpha    = 1.0 - smoothstep(0.45 - _RimWidth, 0.5, dist);

                float  rimMask  = smoothstep(0.45 - _RimWidth * 2.0, 0.45, dist)
                                * (1.0 - smoothstep(0.45, 0.5, dist));
                float3 rim      = _RimColor.rgb * rimMask * _RimIntensity;

                float3 finalColor = bgColor * alpha + rim;
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
```

---

## 使用注意事项

**Opaque Texture 必须开启**。在 UniversalRenderer 的 Inspector 中找到 Opaque Texture 选项并打勾。如果场景里看不到扭曲效果，首先检查这个选项。

**RenderQueue 顺序**。扭曲物体必须在 Opaque 队列之后渲染，通常设为 3000（Transparent）或更高。如果 Queue 设错，`_CameraOpaqueTexture` 里可能还没有正确的背景颜色，扭曲区域会显示为黑色。

**顶点色控制区域**。可以把 Mesh 的顶点色 Alpha 通道作为扭曲强度权重，这样不规则形状的扭曲区域不需要额外遮罩贴图。粒子系统的热浪特效常用这个技巧，每个粒子的顶点色 Alpha 控制局部扭曲强度，实现粒子生命周期内强度的渐入渐出。
