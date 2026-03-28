---
title: "Shader 核心技法 13｜噪声函数：Value Noise、Perlin Noise 与 FBM 在 Shader 里的实现"
slug: "shader-technique-13-noise-functions"
date: "2026-03-28"
description: "从零手写 Value Noise、Perlin Noise 和 FBM，理解噪声函数的数学原理与 HLSL 实现，并用噪声驱动溶解边缘、湍流水面和云朵形状。"
tags: ["Shader", "HLSL", "URP", "核心技法", "噪声", "Perlin", "FBM"]
series: "Shader 手写技法"
weight: 4290
---

程序化效果最核心的需求是"有控制的随机"——完全随机会得到白噪声，毫无美感；完全规则又显得机械。噪声函数解决的正是这个矛盾：输入连续坐标，输出看起来随机、但相邻值之间平滑过渡的浮点数。溶解效果的边缘、程序化云朵、湍流水面的扰动，背后几乎都是同一套数学。

---

## 为什么不直接用纹理采样做随机

最朴素的做法是美术烘焙一张噪声贴图，运行时采样。这没有问题，但有局限：贴图有固定分辨率、重复时会穿帮、无法在运行时动态改变频率或层数。程序化噪声可以任意缩放、任意叠加、占用极少显存，代价是 GPU 算力。现代 GPU 的 ALU 非常便宜，对于中等复杂度的噪声来说完全划算。

---

## Hash 函数：伪随机的起点

所有噪声函数都依赖一个基础组件：给定整数坐标，返回看似随机的浮点数。这个函数叫 hash，要求：相同输入永远相同输出，相邻输入的输出之间没有明显规律，计算要快。

```hlsl
// 1D hash：整数输入，[0,1] 输出
float hash1(float n)
{
    return frac(sin(n) * 43758.5453123);
}

// 2D hash：vec2 输入，[0,1] 输出
float hash21(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p.yx + 19.19);
    return frac((p.x + p.y) * p.x);
}

// 2D hash，返回 vec2（Perlin Noise 需要梯度）
float2 hash22(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)),
               dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453123) * 2.0 - 1.0; // [-1,1]
}
```

`frac(sin(dot(uv, vec)) * large_number)` 这个公式在 ShaderToy 和各种教程里随处可见，原理是 `sin` 函数在大参数下相邻值变化极快，乘以大常数再取小数部分，结果对输入极度敏感，实现了廉价的伪随机。它的缺点是在某些硬件上精度不足，大坐标范围会出现条带，生产项目里更稳健的做法是用整数位操作。

---

## Value Noise：双线性插值的格点随机值

Value Noise 的思路很直接：把空间划分成整数格，每个格点分配一个随机值，格内用平滑插值连接。

```hlsl
// smoothstep 版插值：比 lerp 连续性更好（导数在端点为 0）
float3 smootherstep(float3 t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float valueNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = smootherstep(float3(f, 0.0)).xy;

    float a = hash21(i + float2(0, 0));
    float b = hash21(i + float2(1, 0));
    float c = hash21(i + float2(0, 1));
    float d = hash21(i + float2(1, 1));

    return lerp(lerp(a, b, u.x),
                lerp(c, d, u.x), u.y);
}
```

Value Noise 的问题是在格点交汇处容易出现明显的"十字"或"方块"伪影，这是因为每个格点的值是独立随机的，缺乏方向信息。

---

## Perlin Noise：梯度噪声，更自然的有机感

Perlin Noise 的改进在于：格点上存的不是随机值，而是随机**梯度向量**。格内的值由位置到格点的距离向量与格点梯度做点积，再插值。这让噪声具有方向性，消除了 Value Noise 的方块感。

```hlsl
float perlinNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = smootherstep(float3(f, 0.0)).xy;

    // 四个角的梯度向量
    float2 g00 = hash22(i + float2(0, 0));
    float2 g10 = hash22(i + float2(1, 0));
    float2 g01 = hash22(i + float2(0, 1));
    float2 g11 = hash22(i + float2(1, 1));

    // 距离向量
    float2 d00 = f - float2(0, 0);
    float2 d10 = f - float2(1, 0);
    float2 d01 = f - float2(0, 1);
    float2 d11 = f - float2(1, 1);

    // 点积
    float v00 = dot(g00, d00);
    float v10 = dot(g10, d10);
    float v01 = dot(g01, d01);
    float v11 = dot(g11, d11);

    // 双线性插值，Perlin Noise 输出范围约 [-0.7, 0.7]
    float n = lerp(lerp(v00, v10, u.x),
                   lerp(v01, v11, u.x), u.y);
    return n * 0.5 + 0.5; // 映射到 [0, 1]
}
```

对比 Value Noise，Perlin Noise 在视觉上明显更"流动"，适合云、烟、水波等有机形状。

---

## FBM：分形布朗运动，叠加多层噪声

单层噪声频率单一，缺乏细节层次。FBM（Fractional Brownian Motion）的做法是把多个不同频率的噪声叠加，频率越高振幅越小。

三个关键参数：
- **octaves**：叠加层数，越多细节越丰富，计算也越贵
- **lacunarity**：每层频率的倍增系数，通常取 2.0
- **gain**（persistence）：每层振幅的衰减系数，通常取 0.5

```hlsl
float fbm(float2 uv, int octaves, float lacunarity, float gain)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * perlinNoise(uv * frequency);
        frequency *= lacunarity;
        amplitude *= gain;
    }
    return value;
}
```

注意：HLSL 中 `for` 循环的迭代次数应当是编译期常量或保持较小值，否则会显著增加 ALU 开销。实际项目中 4~6 层通常够用，8 层以上在移动端需要谨慎。

---

## 完整可运行 Shader：噪声驱动溶解效果

```hlsl
Shader "Custom/NoiseDissolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DissolveProgress ("Dissolve Progress", Range(0, 1)) = 0.5
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
        _EdgeColor ("Edge Color", Color) = (1, 0.4, 0.1, 1)
        _NoiseScale ("Noise Scale", Float) = 3.0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _DissolveProgress;
                float  _EdgeWidth;
                float4 _EdgeColor;
                float  _NoiseScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // --- noise 函数内联 ---
            float2 _hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453) * 2.0 - 1.0;
            }

            float _perlin(float2 uv)
            {
                float2 i = floor(uv); float2 f = frac(uv);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float n = lerp(
                    lerp(dot(_hash22(i),           f),
                         dot(_hash22(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(_hash22(i + float2(0,1)), f - float2(0,1)),
                         dot(_hash22(i + float2(1,1)), f - float2(1,1)), u.x), u.y);
                return n * 0.5 + 0.5;
            }

            float _fbm(float2 uv)
            {
                float v = 0.0, a = 0.5;
                v += a * _perlin(uv);       a *= 0.5;
                v += a * _perlin(uv * 2.0); a *= 0.5;
                v += a * _perlin(uv * 4.0); a *= 0.5;
                v += a * _perlin(uv * 8.0);
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float noise = _fbm(IN.uv * _NoiseScale);
                clip(noise - _DissolveProgress);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 边缘发光
                float edge = saturate(1.0 - (noise - _DissolveProgress) / _EdgeWidth);
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge);

                return col;
            }
            ENDHLSL
        }
    }
}
```

C# 侧只需要在材质上调节 `_DissolveProgress` 从 0 到 1，物体就会从噪声形状的边缘开始溶解消失，边缘自动显示橙色发光。

---

## 噪声扰动 UV：湍流水面

噪声不仅可以控制 alpha，还可以用来扰动 UV 坐标，让采样位置产生流动感。

```hlsl
// 在 frag 里，用噪声偏移 UV 再采样
float2 noiseUV = IN.uv * 4.0 + float2(_Time.y * 0.3, _Time.y * 0.15);
float offsetX = _fbm(noiseUV)           * 2.0 - 1.0;
float offsetY = _fbm(noiseUV + 5.7)    * 2.0 - 1.0;
float2 distortedUV = IN.uv + float2(offsetX, offsetY) * 0.04;
half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
```

两个不同相位的 FBM 分别驱动 U 和 V 方向的偏移，配合 `_Time.y` 让偏移随时间移动，就能得到持续流动的湍流感。云朵形状的做法类似：用 FBM 的值直接作为 alpha，配合阈值剔除即可。
