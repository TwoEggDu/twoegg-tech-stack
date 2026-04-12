---
title: "Shader 语法基础 09｜Fragment Shader 完整写法：UV 采样、法线与输出颜色"
slug: "shader-basic-09-fragment-shader"
date: "2026-03-28"
description: "详解 URP Fragment Shader 的完整写法，包括 SV_Target 输出语义、URP 纹理采样宏、插值法线的 normalize 处理、alpha clip 的工作原理，以及一个支持 albedo 采样、tint 颜色和 alpha cutout 的完整片元着色器。"
tags: ["Shader", "Fragment Shader", "URP", "Unity", "HLSL"]
series: "Shader 手写技法"
weight: 4130
---

Fragment Shader（片元着色器）在光栅化之后运行，每个屏幕像素（准确说是"片元"）执行一次。它的输入来自 Vertex Shader 经过插值的 Varyings，输出则是写入 Render Target 的颜色值。Fragment Shader 的代码量通常比 Vertex Shader 多，因为大多数视觉效果——采样、光照、alpha 处理——都发生在这里。

---

## Fragment Shader 的输入：插值后的 Varyings

Fragment Shader 接收的参数类型就是 Vertex Shader 输出的 `Varyings` 结构体，但经过了光栅化器的插值处理。插值是线性的（在屏幕空间做透视校正插值），这意味着：

- UV 是插值的，可以直接拿去采样
- 法线是插值的，但方向会偏离单位长度，**必须先 normalize**
- 世界坐标是插值的，精度足够用于光照计算

一个容易踩的坑：多边形面积很小或法线变化剧烈时，插值后的法线长度可能偏离 1 很多，不 normalize 直接用于光照会产生明显的视觉错误。

---

## SV_Target：输出语义

Fragment Shader 的返回值需要标注 `SV_Target` 语义：

```hlsl
half4 frag(Varyings IN) : SV_Target
{
    return half4(1, 0, 0, 1); // 纯红色，不透明
}
```

`SV_Target` 表示写入默认的第 0 号 Render Target。如果使用 MRT（Multiple Render Targets），可以用 `SV_Target0`、`SV_Target1` 等分别指定。

关于精度选择：
- `half4`（16 位浮点）：颜色、UV、法线方向——移动端推荐，省电省带宽
- `float4`（32 位浮点）：世界坐标、深度相关计算——需要精度时用

Fragment Shader 里混用是合法的，编译器会自动做精度提升（half 参与 float 运算时提升为 float）。

---

## 纹理采样：URP 的三件套宏

URP 使用宏来声明和采样纹理，这套宏在不同平台后端（DirectX、Metal、Vulkan）下展开为不同实现，保证跨平台一致性。

**声明部分（在 CBUFFER 之外）：**

```hlsl
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
```

`TEXTURE2D` 声明纹理对象本身，`SAMPLER` 声明与之配套的采样器状态（Sampler State，控制过滤模式和 Wrap 模式）。采样器名称约定为 `sampler_` 加纹理名。

**采样部分（在函数体内）：**

```hlsl
half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
```

等价于传统写法的 `tex2D(_BaseMap, uv)`，但宏版本在 Vulkan/Metal 下能正确分离纹理和采样器对象。

如果需要指定 mip 级别：

```hlsl
// 手动指定 mip level
half4 texColor = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, IN.uv, 0);

// 采样法线贴图（自动解码 BC5/DXT5nm 格式）
half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
```

---

## 法线在 Fragment Shader 里的用途

从 Vertex Shader 传来的世界空间法线需要 normalize，然后可以用于简单的漫反射计算：

```hlsl
float3 normalWS = normalize(IN.normalWS);

// 简单的半兰伯特漫反射（不依赖 URP 光照系统）
float3 lightDir = normalize(_MainLightPosition.xyz);
float NdotL = dot(normalWS, lightDir);
float diffuse = NdotL * 0.5 + 0.5; // 映射到 [0, 1]，避免背面全黑
```

如果使用法线贴图，需要把切线空间法线变换到世界空间。这需要在 Vertex Shader 里传递完整的 TBN 矩阵（Tangent、Bitangent、Normal），Fragment Shader 里重建变换：

```hlsl
// Fragment Shader 里的法线贴图处理
half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
float3x3 TBN   = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
float3 normalWS = normalize(mul(normalTS, TBN));
```

本篇示例不使用法线贴图，仅展示插值法线的基本用法。

---

## Alpha Clip：clip() 的工作原理

Alpha Test（镂空效果）通过 `clip()` 函数实现。`clip(x)` 的语义是：如果 x 小于 0，则丢弃当前片元（不写入颜色和深度）：

```hlsl
half alpha = texColor.a * _BaseColor.a;
clip(alpha - _Cutoff); // alpha < _Cutoff 时丢弃
```

等价于：

```hlsl
if (alpha < _Cutoff) discard;
```

使用 Alpha Clip 时需要：
1. 在 Properties 里声明 `_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5`
2. 在 SubShader Tags 里设置 `"RenderType" = "TransparentCutout"` 和 `"Queue" = "AlphaTest"`
3. Pass 里保持 `ZWrite On`（这是 AlphaTest 和 Transparent 的核心区别）

Alpha Clip 不需要排序，GPU 可以做 Early-Z 优化（但实际上有 clip 的 Shader 会禁用 Early-Z，取决于驱动实现）。

---

## 完整示例：albedo 采样 + tint 颜色 + alpha cutout

```hlsl
Shader "Custom/FragmentShaderDemo"
{
    Properties
    {
        _BaseMap   ("Base Texture", 2D)           = "white" {}
        _BaseColor ("Base Color", Color)           = (1, 1, 1, 1)
        _Cutoff    ("Alpha Cutoff", Range(0, 1))   = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off      // 双面渲染，镂空材质常用
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv0, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 采样 albedo 纹理
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // 叠加 tint 颜色
                half4 finalColor = texColor * _BaseColor;

                // Alpha Clip：低于阈值的片元直接丢弃
                clip(finalColor.a - _Cutoff);

                // 用法线做简单的朝向提示（可选，仅做演示）
                float3 normalWS = normalize(IN.normalWS);
                half   facing   = saturate(dot(normalWS, float3(0, 1, 0))) * 0.3 + 0.7;

                return half4(finalColor.rgb * facing, 1.0);
            }
            ENDHLSL
        }
    }
}
```

这个示例演示了 Fragment Shader 的全部核心要素：纹理声明与采样宏、tint 颜色叠加、alpha clip、插值法线 normalize。`facing` 这个简单的漫反射近似让材质在顶面更亮、侧面更暗，不依赖 URP 光照系统，适合做风格化效果。下一篇会进入 URP 光照系统的核心——Lit Shader 的文件结构和 PBR 光照函数。
