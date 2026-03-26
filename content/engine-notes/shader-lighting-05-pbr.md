+++
title = "Shader 核心光照 05｜PBR 基础：金属度、粗糙度与 Cook-Torrance"
slug = "shader-lighting-05-pbr"
date = 2026-03-26
description = "理解 PBR 的金属度/粗糙度工作流，Cook-Torrance BRDF 的三个分项（D/F/G），以及如何使用 URP 的 UniversalFragmentPBR 接口，而不是从零手写 BRDF。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "光照", "PBR", "金属度", "粗糙度", "Cook-Torrance"]
series = ["Shader 手写技法"]
[extra]
weight = 4150
+++

Blinn-Phong 是经验模型，不保证物理正确。PBR（Physically Based Rendering）从物理原理出发，同一套参数在任何光照环境下都能得到一致的视觉结果。这篇讲清楚 PBR 的参数含义和 URP 的接入方式。

---

## 金属度 / 粗糙度工作流

URP（和绝大多数现代引擎）采用 **Metallic-Roughness** 工作流：

| 参数 | 范围 | 物理含义 |
|------|------|---------|
| **Metallic**（金属度） | 0~1 | 0=非金属（木头、皮肤），1=纯金属（铁、金） |
| **Roughness**（粗糙度） | 0~1 | 0=镜面，1=完全漫散射 |
| **Albedo**（基础色） | RGB 0~1 | 非金属的漫反射颜色；金属的高光颜色 |

**金属度的作用**：

- 非金属（metallic=0）：有漫反射颜色，高光是白色（中性）
- 金属（metallic=1）：**没有漫反射**（所有光都变成高光），高光颜色等于 albedo

这是金属和非金属最根本的光学区别——金属不透射光到内部，全部反射。

**粗糙度的作用**：

- roughness=0（光滑）：高光集中成一个点，镜面反射
- roughness=1（粗糙）：高光扩散成大面积，接近漫反射

---

## Cook-Torrance BRDF

PBR 的高光用 **Cook-Torrance 微面元模型**。公式：

```
f_cook-torrance = (D * F * G) / (4 * NdotL * NdotV)
```

三个分项：

| 项 | 名称 | 作用 |
|----|------|------|
| **D** | Normal Distribution Function（法线分布函数） | 控制高光形状，粗糙度决定微面元法线分布 |
| **F** | Fresnel 项 | 掠射角时高光增强（菲涅耳效应） |
| **G** | Geometry 遮蔽函数 | 微面元相互遮挡，消除物理上不可能的高光 |

URP 使用的具体实现：D = GGX，F = Schlick 近似，G = Smith GGX。

**不需要手写这些公式**——URP 把完整 PBR 计算封装成了 `UniversalFragmentPBR`。

---

## 使用 `UniversalFragmentPBR`

URP 提供了一个高层接口，只需要填入 `SurfaceData` 和 `InputData`，剩下的全自动：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Fragment Shader 里：
SurfaceData surfaceData = (SurfaceData)0;
surfaceData.albedo       = albedoSample.rgb;
surfaceData.metallic     = metallic;
surfaceData.smoothness   = 1.0 - roughness;   // URP 用 smoothness = 1-roughness
surfaceData.normalTS     = normalTS;           // 切线空间法线
surfaceData.occlusion    = ao;                 // 环境光遮蔽
surfaceData.emission     = emission;
surfaceData.alpha        = albedoSample.a;

InputData inputData = (InputData)0;
inputData.positionWS     = input.positionWS;
inputData.normalWS       = normalWS;
inputData.viewDirectionWS = viewDir;
inputData.shadowCoord    = input.shadowCoord;
inputData.fogCoord       = input.fogFactor;
inputData.vertexLighting = input.vertexLighting;   // 逐顶点附加光
inputData.bakedGI        = SAMPLE_GI(...);          // 烘焙 GI

half4 color = UniversalFragmentPBR(inputData, surfaceData);
return color;
```

`UniversalFragmentPBR` 内部处理了：主光 + 附加光 + IBL（环境光）+ 阴影 + 雾效。

---

## 完整 PBR Shader

```hlsl
Shader "Custom/PBRLit"
{
    Properties
    {
        _BaseColor  ("Base Color",  Color)  = (1, 1, 1, 1)
        _BaseMap    ("Albedo Map",  2D)     = "white" {}
        _Metallic   ("Metallic",    Range(0,1)) = 0.0
        _Roughness  ("Roughness",   Range(0,1)) = 0.5
        _NormalMap  ("Normal Map",  2D)     = "bump" {}
        _NormalScale("Normal Scale",Float)  = 1.0
        [NoScaleOffset] _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1.0
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #pragma shader_feature_local _NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _Metallic;
                float  _Roughness;
                float  _NormalScale;
                float  _OcclusionStrength;
                float4 _EmissionColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);    SAMPLER(sampler_NormalMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);

            struct Attributes
            {
                float4 positionOS  : POSITION;
                float3 normalOS    : NORMAL;
                float4 tangentOS   : TANGENT;
                float2 uv          : TEXCOORD0;
                float2 lightmapUV  : TEXCOORD1;   // 光照贴图 UV
            };

            struct Varyings
            {
                float4 positionHCS   : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float3 positionWS    : TEXCOORD1;
                float3 normalWS      : TEXCOORD2;
                float4 tangentWS     : TEXCOORD3;   // w 存 bitangent 符号
                float4 shadowCoord   : TEXCOORD4;
                half3  vertexLighting : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
                half   fogFactor     : TEXCOORD7;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   norInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS  = posInputs.positionCS;
                output.positionWS   = posInputs.positionWS;
                output.shadowCoord  = GetShadowCoord(posInputs);
                output.uv           = TRANSFORM_TEX(input.uv, _BaseMap);

                output.normalWS     = norInputs.normalWS;
                // tangentWS.w 存 bitangent 方向符号（由 GetVertexNormalInputs 提供）
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS    = half4(norInputs.tangentWS, sign);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                output.vertexLighting = VertexLighting(posInputs.positionWS, norInputs.normalWS);
                output.fogFactor      = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ── 基础采样 ──────────────────────────────────────
                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedo       = albedoSample * _BaseColor;
                half  ao           = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
                ao = LerpWhiteTo(ao, _OcclusionStrength);

                // ── 法线 ──────────────────────────────────────────
                float3 normalWS;
                #ifdef _NORMALMAP
                    float3 normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
                    float3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                    normalWS = TransformTangentToWorld(normalTS,
                        half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS));
                    normalWS = NormalizeNormalPerPixel(normalWS);
                #else
                    normalWS = NormalizeNormalPerPixel(input.normalWS);
                #endif

                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // ── SurfaceData ───────────────────────────────────
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = albedo.rgb;
                surfaceData.metallic    = _Metallic;
                surfaceData.smoothness  = 1.0 - _Roughness;
                surfaceData.occlusion   = ao;
                surfaceData.emission    = _EmissionColor.rgb;
                surfaceData.alpha       = albedo.a;
                surfaceData.normalTS    = float3(0, 0, 1);   // 已在 normalWS 里处理

                // ── InputData ─────────────────────────────────────
                InputData inputData = (InputData)0;
                inputData.positionWS      = input.positionWS;
                inputData.normalWS        = normalWS;
                inputData.viewDirectionWS = viewDir;
                inputData.shadowCoord     = input.shadowCoord;
                inputData.fogCoord        = input.fogFactor;
                inputData.vertexLighting  = input.vertexLighting;
                inputData.bakedGI         = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                inputData.shadowMask      = SAMPLE_SHADOWMASK(input.lightmapUV);

                // ── PBR 计算（主光+附加光+IBL+雾）─────────────────
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
```

---

## Smoothness vs Roughness

URP 的 `SurfaceData.smoothness` 是 **Smoothness = 1 - Roughness**。

Unity 的 Lit Shader 在 Inspector 里显示的是 Smoothness（越大越光滑）。如果你的贴图存的是 Roughness（Unreal 约定），记得转换：

```hlsl
// 贴图存 Roughness，转成 smoothness
surfaceData.smoothness = 1.0 - roughnessSample.r;
```

---

## 常见问题

**Q：金属材质漫反射有颜色残留**

纯金属（metallic=1）理论上漫反射为零。如果还有颜色，检查 metallic 贴图是否正确，或者 metallic 值是否真的到了 1。

**Q：物体在阴暗处太暗，没有任何光**

需要环境光（GI/SH）。确认场景里烘焙了光照，`SAMPLE_GI` 能采样到正确数据。

**Q：PBR 效果在 PC 上正确，移动端变暗**

可能是 HDR 或 Tone Mapping 配置问题。检查 URP Asset 的 Color Grading 设置，或 Camera 的 Post Processing 配置。

---

## 小结

| 概念 | 要点 |
|------|------|
| 金属度 | 0=非金属（有漫反射），1=纯金属（只有高光） |
| 粗糙度 | 0=镜面，1=完全散射；URP 用 smoothness=1-roughness |
| Cook-Torrance | D（法线分布）× F（菲涅耳）× G（遮蔽），URP 已封装 |
| `UniversalFragmentPBR` | 填 SurfaceData + InputData，自动算主光+附加光+IBL |
| 能量守恒 | PBR 保证能量守恒，Blinn-Phong 不保证 |

下一篇：环境光与 IBL——球谐光照、反射探针、IndirectDiffuse / IndirectSpecular 的采样方式。
