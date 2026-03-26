+++
title = "Shader 手写入门 02｜采样一张贴图：UV 坐标与纹理采样"
slug = "shader-intro-02-texture-uv"
date = 2026-03-26
description = "在 Shader 里采样一张贴图，理解 UV 坐标是什么、怎么从 Mesh 里读、TRANSFORM_TEX 在做什么，以及 Tiling 和 Offset 为什么能控制贴图的显示方式。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "入门", "贴图", "UV"]
series = ["Shader 手写技法"]
[extra]
weight = 4020
+++

颜色固定的 Shader 用处有限。大多数情况下你需要采样一张贴图——让贴图决定每个像素的颜色。这篇讲清楚贴图采样的完整流程，核心是理解 UV 坐标。

---

## UV 是什么

UV 是贴图坐标，U 对应横轴，V 对应纵轴。范围通常是 0 到 1——(0,0) 是贴图左下角，(1,1) 是右上角。

**UV 存在 Mesh 上**。3D 建模时，美术把每个顶点映射到贴图的某个位置，这个映射就是 UV 展开（UV Unwrap）。Vertex Shader 把这些 UV 坐标传给 Fragment Shader，Fragment Shader 用 UV 在贴图上查找对应位置的颜色。

```
顶点 UV (0.5, 0.5) → 贴图中心的颜色
顶点 UV (0.0, 0.0) → 贴图左下角的颜色
顶点 UV (1.0, 1.0) → 贴图右上角的颜色
```

---

## 完整代码

```hlsl
Shader "Custom/TextureSample"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Tint    ("Tint Color",  Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;   // Tiling 和 Offset（_贴图名_ST 是固定命名规则）
                float4 _Tint;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;  // 从 Mesh 读取的原始 UV
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;  // 传给 Fragment Shader 的 UV
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                // TRANSFORM_TEX：应用 Tiling 和 Offset
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 用 UV 坐标采样贴图
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return texColor * _Tint;
            }

            ENDHLSL
        }
    }
}
```

---

## 逐步解释

### 声明贴图

```hlsl
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
```

贴图的声明分两部分：
- `TEXTURE2D`：贴图本身（存储像素数据）
- `SAMPLER`：采样器（控制过滤方式：双线性/三线性，以及超出 0~1 范围时怎么处理：Repeat/Clamp）

命名规则：采样器名字固定是 `sampler_` + 贴图名。

**为什么不放在 CBUFFER 里**：贴图不是 Constant Buffer（常量缓冲），它是独立的 GPU 资源，声明在 CBUFFER 外面。只有标量和向量（float、float4 等）放在 CBUFFER 里。

---

### _MainTex_ST

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _Tint;
CBUFFER_END
```

每个 2D 贴图 Property，Unity 会自动生成一个对应的 `_贴图名_ST` 变量，存储 Inspector 里的 Tiling 和 Offset 参数：

```
_MainTex_ST.xy = Tiling（x 方向和 y 方向的重复次数）
_MainTex_ST.zw = Offset（x 方向和 y 方向的偏移量）
```

**必须在 CBUFFER 里声明 `_MainTex_ST`**，哪怕你不需要 Tiling/Offset 功能——因为 TRANSFORM_TEX 宏用到了它。

---

### TRANSFORM_TEX

```hlsl
o.uv = TRANSFORM_TEX(v.uv, _MainTex);
```

展开后等价于：

```hlsl
o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
```

就是把 Mesh 的原始 UV 乘以 Tiling、加上 Offset。如果 Inspector 里 Tiling = (2, 2)，UV 会乘以 2，贴图在物体上重复 2 次。

---

### SAMPLE_TEXTURE2D

```hlsl
half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
```

三个参数：贴图、采样器、UV 坐标。返回 UV 对应位置的颜色（RGBA）。

---

## UV 可以直接当颜色看

调试 UV 的常用技巧：把 UV 直接输出为颜色：

```hlsl
half4 frag(Varyings i) : SV_Target
{
    // U 显示为红色，V 显示为绿色
    return half4(i.uv.x, i.uv.y, 0, 1);
}
```

物体会变成左下黑、右下红、左上绿、右上黄的渐变。如果 UV 展开有问题（拉伸、重叠），在这个模式下一目了然。

---

## UV 动画：让贴图滚动

既然 UV 只是坐标，随时间移动 UV 就能让贴图滚动：

```hlsl
Properties
{
    _MainTex   ("Main Texture", 2D)     = "white" {}
    _ScrollX   ("Scroll Speed X", Float) = 0.5
    _ScrollY   ("Scroll Speed Y", Float) = 0.0
}

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float  _ScrollX;
    float  _ScrollY;
CBUFFER_END

half4 frag(Varyings i) : SV_Target
{
    // frac：取小数部分，让 UV 在 0~1 之间循环（防止 Clamp 模式下越界）
    float2 scrollUV = i.uv + float2(_ScrollX, _ScrollY) * _Time.y;
    scrollUV = frac(scrollUV);
    half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrollUV);
    return texColor;
}
```

`frac(x)`：返回 x 的小数部分。`frac(1.3) = 0.3`，`frac(2.7) = 0.7`。用来让 UV 在 0~1 范围内循环，配合贴图的 Repeat 模式实现无缝滚动。

---

## 多张贴图混合

一个 Shader 可以采样多张贴图，结果可以相乘、相加或 lerp 混合：

```hlsl
Properties
{
    _MainTex  ("Main Texture",   2D) = "white" {}
    _MaskTex  ("Mask Texture",   2D) = "white" {}
}

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _MaskTex_ST;
CBUFFER_END

TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
TEXTURE2D(_MaskTex);  SAMPLER(sampler_MaskTex);

half4 frag(Varyings i) : SV_Target
{
    half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half  mask      = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv).r;

    // 用 Mask 贴图的红色通道控制主贴图的透明度
    return half4(mainColor.rgb, mask);
}
```

---

## 小结

- UV 是贴图坐标，范围 0~1，存在 Mesh 顶点上，由 Vertex Shader 传给 Fragment Shader
- `TEXTURE2D` + `SAMPLER`：贴图声明，采样器命名规则是 `sampler_贴图名`
- `_MainTex_ST`：每个 2D 贴图自动生成的 Tiling/Offset 变量，必须放在 CBUFFER 里
- `TRANSFORM_TEX(uv, tex)`：应用 Tiling 和 Offset，等价于 `uv * _ST.xy + _ST.zw`
- `SAMPLE_TEXTURE2D(tex, sampler, uv)`：用 UV 坐标采样贴图，返回颜色
- UV 直接输出为颜色：调试展开质量的常用技巧
- UV 加 `_Time.y * 速度`：让贴图滚动，用 `frac()` 保持在 0~1 范围内循环

下一篇：给 Shader 加上最简单的光照——Lambert 漫反射，让物体有明暗。
