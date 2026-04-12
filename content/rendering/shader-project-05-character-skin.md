---
title: "项目实战 05｜角色皮肤完整方案：SSS + 毛孔细节 + 湿润"
slug: "shader-project-05-character-skin"
date: "2026-03-26"
description: "写实角色皮肤需要整合多个技术：Wrap Lighting（柔和明暗）、Transmission（薄处透光）、多层法线（基础 + 毛孔细节）、Specular 颜色（皮脂色调）、湿润状态（光滑度提升 + 法线压平）。这篇把这些组合成完整皮肤 Shader。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "项目实战"
  - "皮肤"
  - "SSS"
  - "写实角色"
series: "Shader 手写技法"
weight: 4440
---
皮肤是写实角色渲染里最难做的材质——不够通透显得像塑料，细节不足显得像蜡像。这篇整合进阶层 SSS 的理论，加入毛孔细节和湿润状态，给出一套可用于生产的皮肤 Shader。

---

## 整体结构

```
皮肤输出 =
    漫反射（Wrap Lighting + 散射颜色）
    + 透射（Transmission，耳朵/手指背光透光）
    + 高光（皮脂色调 Specular，宽而柔和）
    + 细节法线（毛孔/皱纹，高 Tiling）
    + 湿润叠加（光滑度提升，法线压平）
```

---

## 贴图布局

```
BaseMap (RGBA)：
    RGB = 固有色（皮肤颜色）
    A   = Alpha（通常 1）

NormalMap：基础法线（皱纹、面部结构）

DetailNormalMap：细节法线（毛孔、细纹，高 Tiling = 8~16）

ThicknessMap：
    R = 厚度（白=薄/透光，黑=厚/不透光）
    耳朵、鼻翼、嘴唇、手指 = 白色

MaskMap：
    R = 皮脂（Sebaceous）分布——T 区（额头/鼻子/下巴）比脸颊更亮
    G = 湿润遮罩（汗水、雨水区域）
    B = AO
    A = （可空）
```

---

## 模块一：皮肤漫反射（Wrap + 散射颜色）

```hlsl
// Wrap Lighting：背面也有漫射
float w        = _WrapFactor;   // 0.3~0.5
float rawNdotL = dot(normalWS, mainLight.direction);
float NdotL_w  = saturate((rawNdotL + w) / (1.0 + w));

// 在亮部颜色和皮肤散射颜色之间插值
// 暗部偏红（血液散射），亮部保持固有色
half3 scatter  = lerp(_ScatterColor.rgb, half3(1,1,1), NdotL_w);
half3 diffuse  = albedo * scatter * mainLight.color * mainLight.shadowAttenuation;
```

---

## 模块二：透射（Transmission）

耳朵/手指背光透光，用厚度贴图控制：

```hlsl
// 厚度贴图采样
half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r;

// 透射方向（法线向内扭曲光方向，模拟散射偏移）
float3 transmitDir = normalize(mainLight.direction + normalWS * _TransmitDistortion);
float  VdotT       = saturate(dot(viewDir, -transmitDir));

// 薄处（厚度小）× 视线对齐 = 透光强度
half   transmit    = pow(VdotT, _TransmitPower) * thickness * _TransmitStrength
                   * mainLight.shadowAttenuation;
half3  transmission = transmit * mainLight.color * _TransmitColor.rgb;
```

---

## 模块三：皮肤高光（宽而带色调）

真实皮肤的高光（来自皮脂）颜色略带暖色，且比金属高光更宽（Blinn-Phong 指数 16~64）：

```hlsl
// 皮脂分布遮罩：T 区更亮
half sebacious = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv).r;

float3 halfDir = normalize(mainLight.direction + viewDir);
half   NdotH   = saturate(dot(normalWS, halfDir));

// 高光指数（较低 = 宽而柔和的高光）
half   specPower = lerp(_SpecPowerMin, _SpecPowerMax, sebacious);
half   spec      = pow(NdotH, specPower);

// 高光颜色带皮脂暖色
half3  specular  = spec * _SkinSpecColor.rgb * mainLight.color * NdotL_w;
```

---

## 模块四：毛孔细节法线

细节法线使用高 Tiling，只在近处可见，远处用权重淡化（避免 Aliasing）：

```hlsl
// 基础法线
float3 normalBase = UnpackNormalScale(
    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);

// 细节法线（高 Tiling，近距离毛孔细节）
float2 detailUV   = input.uv * _PoreNormalTiling;
float3 poreNormal = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, detailUV));

// 根据摄像机距离淡化细节（避免远处 Moire 纹）
float camDist     = length(input.posWS - _WorldSpaceCameraPos.xyz);
float detailFade  = saturate(1.0 - (camDist - _DetailFadeStart) / _DetailFadeRange);

poreNormal = lerp(float3(0,0,1), poreNormal, detailFade * _PoreNormalScale);

// 叠加
float3 blendedNormal = normalize(float3(normalBase.xy + poreNormal.xy, normalBase.z));
float3 normalWS = normalize(TransformTangentToWorld(blendedNormal,
    half3x3(input.tangentWS, input.bitangentWS, input.normalWS)));
```

---

## 模块五：湿润状态

下雨/出汗时皮肤变湿：光滑度提升、法线被"压平"（水膜遮盖细节）、颜色略暗：

```hlsl
// 湿润遮罩（由美术绘制，或由 C# 控制 _WetLevel 参数）
half wetMask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv).g;
half wetness = wetMask * _WetLevel;   // _WetLevel: 0=干燥，1=完全湿润

// 湿润效果：
// 1. 固有色变暗（水吸收光）
albedo = lerp(albedo, albedo * 0.7, wetness);

// 2. 法线压平（水膜使表面更光滑）
//    在 TBN 之前把法线向 (0,0,1) 插值
blendedNormal = lerp(blendedNormal, float3(0,0,1), wetness * 0.6);

// 3. 光滑度提升（水面更光滑）
float smoothness = lerp(_BaseSmoothness, 0.95, wetness);
```

---

## 完整皮肤 Shader

```hlsl
Shader "Custom/CharacterSkin"
{
    Properties
    {
        _BaseMap          ("Albedo",           2D)         = "white" {}
        _NormalMap        ("Normal Map",       2D)         = "bump" {}
        _NormalScale      ("Normal Scale",     Range(0,2)) = 1.0
        _DetailNormal     ("Pore Normal",      2D)         = "bump" {}
        _PoreNormalTiling ("Pore Tiling",      Float)      = 12.0
        _PoreNormalScale  ("Pore Scale",       Range(0,1)) = 0.3
        _DetailFadeStart  ("Detail Fade Start",Float)      = 3.0
        _DetailFadeRange  ("Detail Fade Range",Float)      = 5.0
        _ThicknessMap     ("Thickness Map",    2D)         = "white" {}
        _MaskMap          ("Mask(Seb/Wet/AO)", 2D)         = "white" {}

        [Header(SSS)]
        _WrapFactor       ("Wrap Factor",      Range(0,1)) = 0.4
        _ScatterColor     ("Scatter Color",    Color)      = (0.8,0.2,0.1,1)
        _TransmitStrength ("Transmit Strength",Range(0,2)) = 1.0
        _TransmitPower    ("Transmit Power",   Float)      = 3.0
        _TransmitDistortion("Transmit Distortion",Range(0,1))= 0.2
        [HDR]_TransmitColor("Transmit Color", Color)      = (0.8,0.2,0.1,1)

        [Header(Specular)]
        _SkinSpecColor    ("Spec Color",       Color)      = (1.0,0.95,0.9,1)
        _SpecPowerMin     ("Spec Power Min",   Float)      = 16.0
        _SpecPowerMax     ("Spec Power Max",   Float)      = 64.0
        _BaseSmoothness   ("Base Smoothness",  Range(0,1)) = 0.4

        [Header(Wet)]
        _WetLevel         ("Wet Level",        Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "SkinForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float  _NormalScale; float _PoreNormalTiling; float _PoreNormalScale;
                float  _DetailFadeStart; float _DetailFadeRange;
                float  _WrapFactor; float4 _ScatterColor;
                float  _TransmitStrength; float _TransmitPower; float _TransmitDistortion;
                float4 _TransmitColor;
                float4 _SkinSpecColor; float _SpecPowerMin; float _SpecPowerMax; float _BaseSmoothness;
                float  _WetLevel;
            CBUFFER_END

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);    SAMPLER(sampler_NormalMap);
            TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);
            TEXTURE2D(_ThicknessMap); SAMPLER(sampler_ThicknessMap);
            TEXTURE2D(_MaskMap);      SAMPLER(sampler_MaskMap);

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float4 t:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 hcs:SV_POSITION; float2 uv:TEXCOORD0;
                                float3 tangentWS:TEXCOORD1; float3 bitangentWS:TEXCOORD2;
                                float3 normalWS:TEXCOORD3; float3 posWS:TEXCOORD4;
                                float4 shadowCoord:TEXCOORD5; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.pos.xyz);
                VertexNormalInputs   ni = GetVertexNormalInputs(i.n, i.t);
                o.hcs = pi.positionCS; o.posWS = pi.positionWS;
                o.shadowCoord  = GetShadowCoord(pi);
                o.tangentWS    = ni.tangentWS;
                o.bitangentWS  = ni.bitangentWS;
                o.normalWS     = ni.normalWS;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 V = normalize(GetWorldSpaceViewDir(input.posWS));
                Light  light = GetMainLight(input.shadowCoord);

                // ── 贴图采样 ──────────────────────────────────
                half3  albedo    = SAMPLE_TEXTURE2D(_BaseMap,      sampler_BaseMap,      input.uv).rgb;
                half   thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r;
                half4  mask      = SAMPLE_TEXTURE2D(_MaskMap,      sampler_MaskMap,      input.uv);

                // ── 法线（基础 + 毛孔细节）────────────────────
                float3 nBase = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
                float2 detailUV = input.uv * _PoreNormalTiling;
                float3 poreN    = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, detailUV));

                float camDist   = length(input.posWS - _WorldSpaceCameraPos.xyz);
                float detailFade = saturate(1.0 - (camDist - _DetailFadeStart) / max(_DetailFadeRange, 0.001));
                poreN = lerp(float3(0,0,1), poreN, detailFade * _PoreNormalScale);

                // ── 湿润效果 ──────────────────────────────────
                half wetness = mask.g * _WetLevel;
                albedo = lerp(albedo, albedo * 0.75, wetness);
                float3 blendedN = normalize(float3(nBase.xy + poreN.xy, nBase.z));
                blendedN = lerp(blendedN, float3(0,0,1), wetness * 0.6);
                float smoothness = lerp(_BaseSmoothness + mask.r * 0.2, 0.95, wetness);

                float3 normalWS = normalize(TransformTangentToWorld(blendedN,
                    half3x3(input.tangentWS, input.bitangentWS, input.normalWS)));

                // ── Wrap Lighting 漫反射 ───────────────────────
                float w        = _WrapFactor;
                float rawNdotL = dot(normalWS, light.direction);
                float NdotL_w  = saturate((rawNdotL + w) / (1.0 + w));
                half3 scatter  = lerp(_ScatterColor.rgb, half3(1,1,1), NdotL_w);
                half3 diffuse  = albedo * scatter * light.color * light.shadowAttenuation;

                // ── 透射 ──────────────────────────────────────
                float3 tDir    = normalize(light.direction + normalWS * _TransmitDistortion);
                float  VdotT   = saturate(dot(V, -tDir));
                half   transmit = pow(VdotT, _TransmitPower) * thickness * _TransmitStrength * light.shadowAttenuation;
                half3  transColor = transmit * light.color * _TransmitColor.rgb;

                // ── 高光 ──────────────────────────────────────
                float3 H       = normalize(light.direction + V);
                half   NdotH   = saturate(dot(normalWS, H));
                half   specPow = lerp(_SpecPowerMin, _SpecPowerMax, mask.r + wetness * 2.0);
                specPow        = clamp(specPow, _SpecPowerMin, 256.0);
                half3  specular = pow(NdotH, specPow) * _SkinSpecColor.rgb * light.color * NdotL_w;

                // ── 环境光 ────────────────────────────────────
                half3 ambient = albedo * SampleSH(normalWS) * (1.0 - mask.b * 0.5);  // AO

                return half4(diffuse + transColor + specular + ambient, 1.0);
            }
            ENDHLSL
        }
    }
}
```

---

## 小结

| 模块 | 技术 | 关键参数 |
|------|------|---------|
| 柔和明暗 | Wrap Lighting | `_WrapFactor`：0.3~0.5 |
| 血色感 | 暗部散射颜色 | `_ScatterColor`：暖红色 |
| 耳朵透光 | Transmission | `_ThicknessMap`，`_TransmitStrength` |
| 毛孔细节 | 高 Tiling 法线 + 距离淡出 | `_PoreNormalTiling`：8~16 |
| 皮脂高光 | 低指数宽高光 + T区遮罩 | `_SpecPowerMin/Max` |
| 湿润 | 光滑度提升 + 法线压平 | `_WetLevel`：C# 动态控制 |

下一篇：UI 特效 Shader——扫光、溶解边框、全息干扰，给 UI 元素增加动态视觉效果。
