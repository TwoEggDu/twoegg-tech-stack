---
title: "游戏常用效果｜体积云与体积光：Ray Marching 原理与移动端近似方案"
slug: shader-weather-04-volumetric
date: "2026-03-28"
description: "深入讲解 Ray Marching 在体积云和体积光中的应用原理，对比完整方案与移动端近似方案的质量与性能权衡，附简化版体积云 Fragment Shader 代码。"
tags: ["Shader", "HLSL", "URP", "天气", "体积云", "体积光", "Ray Marching"]
series: "Shader 手写技法"
weight: 4600
---

体积云和体积光是那种看起来很贵、但如果知道原理就能找到对应近似方案的效果。完整的物理体积渲染确实开销高——每帧对三维体积做几十步采样，移动端根本跑不动。但大多数游戏里看到的体积云和丁达尔效应（God Ray），背后都是经过精心近似的廉价方案，和真正的体积渲染差距肉眼难以察觉。这篇文章把两条路都讲清楚：先理解完整原理，再讲移动端怎么近似。

---

## Ray Marching 的基本原理

Ray Marching（光线步进）是一种沿射线方向逐步采样的渲染技术。传统光栅化假设场景由不透明表面组成，无法处理体积效果。Ray Marching 绕开这个限制：从相机出发，沿视线方向每隔固定步长采样一次密度函数，累积透射率和散射光。

核心循环结构：

```hlsl
float3 RayMarchCloud(float3 rayOrigin, float3 rayDir, int steps, float stepSize)
{
    float  transmittance = 1.0; // 透明度（1=完全透明，0=完全遮挡）
    float3 scatteredLight = 0;

    float3 pos = rayOrigin;
    for (int i = 0; i < steps; i++)
    {
        pos += rayDir * stepSize;

        float density = SampleCloudDensity(pos); // 当前点密度
        if (density <= 0.0) continue;

        // Beer-Lambert 定律：透射率随密度衰减
        float extinction = density * stepSize;
        transmittance *= exp(-extinction);

        // 向太阳方向步进采样，计算到达该点的光照
        float sunShadow = SampleLightTransmittance(pos, _SunDirection);
        float3 luminance = _SunColor * sunShadow * HGPhase(dot(rayDir, _SunDirection), 0.3);

        // 累积散射光
        scatteredLight += luminance * density * stepSize * transmittance;

        if (transmittance < 0.01) break; // 足够不透明则提前退出
    }

    return scatteredLight;
}
```

三个核心计算：**密度函数**（当前位置是否有云）、**Beer-Lambert 透射率**（光在介质中衰减）、**相位函数**（光的散射方向分布）。

---

## 体积云的密度函数

云的形状用多层噪声叠加描述：低频噪声给出大体形状，高频噪声腐蚀边缘添加细节：

```hlsl
float SampleCloudDensity(float3 pos)
{
    // 云层高度范围限制
    float heightFraction = (pos.y - _CloudBottomAltitude) /
                           (_CloudTopAltitude - _CloudBottomAltitude);
    if (heightFraction < 0.0 || heightFraction > 1.0) return 0.0;

    // 高度渐变：底部和顶部密度低，中间密度高
    float heightGradient = heightFraction * (1.0 - heightFraction) * 4.0;

    // 低频形状噪声（带风速偏移）
    float2 shapeUV = pos.xz * _CloudShapeScale + _Time.y * _CloudWindSpeed;
    float  shape   = SAMPLE_TEXTURE2D_LOD(_CloudShapeNoise, sampler_CloudShapeNoise, shapeUV, 0).r;

    // 高频细节噪声（速度略快于大形状）
    float2 detailUV = pos.xz * _CloudDetailScale + _Time.y * _CloudWindSpeed * 1.5;
    float  detail   = SAMPLE_TEXTURE2D_LOD(_CloudDetailNoise, sampler_CloudDetailNoise, detailUV, 0).r;

    // 高频噪声腐蚀形状边缘
    float density = saturate(shape - (1.0 - detail) * _DetailWeight);
    return density * heightGradient * _CloudDensity;
}
```

实际项目中常用 3D Texture 存储噪声以表达云层立体感，但 3D 纹理采样成本高，移动端通常退化为 2D Texture 加视差偏移来模拟深度。

---

## 体积光（God Ray）：屏幕空间近似

真正的体积光需要在光线路径上逐步采样阴影贴图——同样是 Ray Marching，只是换成对光源方向步进。这个开销在移动端基本不可接受。

实际上 90% 的游戏里用的 God Ray 是屏幕空间的径向模糊方案，原理极其简单：

```hlsl
// 屏幕空间 God Ray（后处理 Pass）
half3 GodRayScreenSpace(float2 screenUV, float2 sunScreenPos, int numSamples)
{
    float2 delta = (screenUV - sunScreenPos) / float(numSamples) * _GodRayDecay;
    float2 uv    = screenUV;
    half3  color = 0;
    float  decay = 1.0;

    for (int i = 0; i < numSamples; i++)
    {
        uv -= delta;

        // 以太阳为中心向外径向采样场景颜色
        half3  sampleColor = SAMPLE_TEXTURE2D(_CameraColorTexture,
                                               sampler_CameraColorTexture, uv).rgb;
        // 深度遮罩：只有天空像素（深度极大值）参与 God Ray
        float  depth       = SAMPLE_TEXTURE2D(_CameraDepthTexture,
                                               sampler_CameraDepthTexture, uv).r;
        float  skyMask     = step(0.9999, depth); // 1=天空，0=有遮挡

        color += sampleColor * skyMask * decay * _GodRayWeight;
        decay *= _GodRayDensity; // 每步衰减
    }
    return color;
}
```

这不是真正的体积——它把太阳方向的颜色沿径向累积，产生视觉上的光柱感。开销极低，移动端 16~32 个采样就效果不错。

---

## 简化版体积云 Fragment Shader

以下是固定步长、适合中端 PC 的简化体积云实现：

```hlsl
Shader "Custom/VolumetricCloud"
{
    Properties
    {
        _CloudShapeNoise     ("Shape Noise (2D)",   2D)    = "white" {}
        _CloudDetailNoise    ("Detail Noise (2D)",  2D)    = "white" {}
        _CloudBottomAltitude ("Cloud Bottom",       Float) = 800.0
        _CloudTopAltitude    ("Cloud Top",          Float) = 1200.0
        _CloudDensity        ("Cloud Density",      Float) = 0.5
        _CloudShapeScale     ("Shape Scale",        Float) = 0.0001
        _CloudDetailScale    ("Detail Scale",       Float) = 0.001
        _DetailWeight        ("Detail Weight",      Range(0,1)) = 0.3
        _CloudWindSpeed      ("Wind Speed",         Float) = 0.02
        _MarchSteps          ("March Steps",        Integer) = 32
        _StepSize            ("Step Size",          Float) = 20.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CloudShapeNoise);  SAMPLER(sampler_CloudShapeNoise);
            TEXTURE2D(_CloudDetailNoise); SAMPLER(sampler_CloudDetailNoise);

            CBUFFER_START(UnityPerMaterial)
                float _CloudBottomAltitude;
                float _CloudTopAltitude;
                float _CloudDensity;
                float _CloudShapeScale;
                float _CloudDetailScale;
                float _DetailWeight;
                float _CloudWindSpeed;
                int   _MarchSteps;
                float _StepSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS  : TEXCOORD0;
            };

            float SampleDensity(float3 pos)
            {
                float hf = (pos.y - _CloudBottomAltitude) /
                           (_CloudTopAltitude - _CloudBottomAltitude);
                if (hf < 0.0 || hf > 1.0) return 0.0;
                float hg = hf * (1.0 - hf) * 4.0;

                float2 sUV = pos.xz * _CloudShapeScale  + _Time.y * _CloudWindSpeed;
                float2 dUV = pos.xz * _CloudDetailScale + _Time.y * _CloudWindSpeed * 1.5;
                float  s   = SAMPLE_TEXTURE2D_LOD(_CloudShapeNoise,  sampler_CloudShapeNoise,  sUV, 0).r;
                float  d   = SAMPLE_TEXTURE2D_LOD(_CloudDetailNoise, sampler_CloudDetailNoise, dUV, 0).r;

                return saturate(s - (1.0 - d) * _DetailWeight) * hg * _CloudDensity;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 从 Object 空间顶点方向近似视线方向（天空盒用法）
                OUT.viewDirWS  = TransformObjectToWorld(IN.positionOS.xyz) - _WorldSpaceCameraPos;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 rayDir  = normalize(IN.viewDirWS);
                float3 rayPos  = _WorldSpaceCameraPos;

                float  transmittance = 1.0;
                float3 cloudColor    = 0;

                for (int i = 0; i < _MarchSteps; i++)
                {
                    rayPos += rayDir * _StepSize;

                    float density = SampleDensity(rayPos);
                    if (density <= 0.0) continue;

                    // 透射率衰减（系数 0.01 缩放密度到合理范围）
                    float extinction = density * _StepSize * 0.01;
                    transmittance *= exp(-extinction);

                    // 简单光照：来自上方的白色光
                    cloudColor += float3(1.0, 0.97, 0.93) * density * _StepSize * 0.01
                                  * transmittance;

                    if (transmittance < 0.01) break;
                }

                float alpha = 1.0 - transmittance;
                // 避免除以零
                float3 finalColor = alpha > 0.001 ? cloudColor / alpha : 0;
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
```

---

## 移动端近似：2D 贴图 + 视差

对移动端来说，完整的 Ray Marching 体积云不现实。常用方案是多层 2D 云朵贴图叠加，通过视差偏移（Parallax）制造深度感：

```hlsl
// 视差偏移：根据视线方向在水平面上偏移云层 UV
float2 viewOffset = viewDirWS.xz / max(viewDirWS.y, 0.1) * _ParallaxStrength;

float2 cloud1UV = worldUV * 0.0003 + _Time.y * 0.010 + viewOffset * 1.0;
float2 cloud2UV = worldUV * 0.0005 + _Time.y * 0.007 + viewOffset * 0.5;

float cloud1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloud1UV).r;
float cloud2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloud2UV).r;

// 两层相乘：只有两层都有云的区域才显示（形状更自然）
float cloudMask = saturate(cloud1 * cloud2 * 2.0);
```

两层贴图各不同速度流动，乘法叠加后形状变化比单层加法更自然，类似 FBM 噪声的效果。

---

## 性能对比

| 方案 | 单帧采样次数 | 适用平台 | 视觉质量 |
|------|------------|----------|----------|
| 完整 Ray Marching（64 步） | 64+ 次 3D 采样 | 高端 PC / 主机 | 最高 |
| 简化步长（16~32 步） | 16~32 次 | 中端 PC | 中高 |
| 屏幕空间 God Ray | 16~32 次 2D 采样 | PC / 主机 | 中（非真体积） |
| 2D 贴图 + 视差近似 | 2~4 次 2D 采样 | 移动端 | 较低但可接受 |

移动端实际项目中，天空区域的体积云通常用 2D 近似，体积光用屏幕空间径向模糊，两者加在一起控制在 4ms 以内。完整 Ray Marching 留给 PC/主机平台，或作为高画质选项通过图形设置开关控制。关键不在于哪种方案"正确"，而在于哪种方案在目标硬件上能稳定 60fps 同时让玩家觉得好看。
