---
title: "Shader 核心技法 03｜卡通渲染：Cel Shading 色阶与边缘光"
slug: "shader-technique-03-cel-shading"
date: "2026-03-26"
description: "Cel Shading（赛璐珞）风格的核心是阶梯状漫反射和 Rim Light。用 step/smoothstep 把连续光照变成色阶，加边缘光（Rim），实现日式动漫风格渲染。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "技法"
  - "卡通渲染"
  - "Cel Shading"
  - "Rim Light"
series: "Shader 手写技法"
weight: 4190
---
卡通渲染（Cel Shading / Toon Shading）的特征是**阶梯式明暗**——光照不连续过渡，而是突变成几个离散的色阶。配合外描边（下一篇），构成完整的日式动漫风格。

---

## 一、阶梯漫反射

把连续的 NdotL 值量化为几个离散的色阶：

```hlsl
float NdotL = saturate(dot(normalWS, mainLight.direction));

// 两色阶（最常见）：明暗各一色
float cel = step(0.5, NdotL);              // 0 或 1
half3 color = lerp(_ShadowColor.rgb, _LitColor.rgb, cel);
```

**带过渡的软色阶（更自然）：**

```hlsl
float cel = smoothstep(0.45, 0.55, NdotL);   // 在 0.45~0.55 之间平滑过渡
```

**三色阶：**

```hlsl
float shadow = step(0.3, NdotL);             // 暗部 / 中间调
float mid    = step(0.6, NdotL);             // 中间调 / 亮部

half3 color;
color = lerp(_ShadowColor.rgb,  _MidColor.rgb, shadow);
color = lerp(color,             _LitColor.rgb, mid);
```

**用渐变贴图（Ramp Texture）：**

用 NdotL 作为 U 坐标采样一维渐变贴图，美术直接控制色阶分布：

```hlsl
float2 rampUV  = float2(NdotL, 0.5);   // V 固定在中间，U = NdotL
half3  rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;
half3  color   = albedo * rampCol;
```

Ramp 贴图由美术绘制，可以做出任意形状的色阶，是卡通渲染最灵活的方式。

---

## 二、卡通高光（Specular Cel）

把连续高光也变成色阶：

```hlsl
float3 halfDir = normalize(mainLight.direction + viewDir);
float  NdotH   = saturate(dot(normalWS, halfDir));
float  specCel = step(1.0 - _SpecSize, pow(NdotH, _Shininess));  // 超过阈值才显示高光
half3  specular = specCel * _SpecColor.rgb * mainLight.color;
```

`_SpecSize` 控制高光斑大小（0.05~0.3）。`step` 把高光变成硬边圆斑，典型的卡通高光形态。

---

## 三、边缘光（Rim Light）

边缘光（Rim / Fresnel）在物体边缘产生发光效果，增强体积感和视觉冲击：

```hlsl
float NdotV  = saturate(dot(normalWS, viewDir));
float rim    = 1.0 - NdotV;                          // 边缘处 NdotV 接近 0，rim 接近 1
float rimCel = step(1.0 - _RimThreshold, rim);        // 色阶化
half3 rimColor = rimCel * _RimColor.rgb * _RimIntensity;
```

`_RimThreshold` 控制边缘宽度（0.5~0.9），值越大边缘越窄。

**朝向光源的 Rim（更自然）：**

纯 Fresnel Rim 在背光面也会出现，有时不自然。加上光照方向约束，只让受光侧产生 Rim：

```hlsl
float NdotL_unclamped = dot(normalWS, mainLight.direction);  // 不 saturate
float rimMask = NdotL_unclamped * 0.5 + 0.5;                 // 受光侧 > 0.5
float rim     = (1.0 - NdotV) * rimMask;
float rimCel  = step(1.0 - _RimThreshold, rim);
```

---

## 四、完整卡通 Shader

```hlsl
Shader "Custom/ToonLit"
{
    Properties
    {
        _BaseMap       ("Base Map",        2D)     = "white" {}
        _BaseColor     ("Base Color",      Color)  = (1, 1, 1, 1)
        _ShadowColor   ("Shadow Color",    Color)  = (0.4, 0.4, 0.6, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSmooth  ("Shadow Smooth",   Range(0,0.2)) = 0.05
        _SpecColor     ("Specular Color",  Color)  = (1, 1, 1, 1)
        _SpecSize      ("Specular Size",   Range(0,1)) = 0.1
        _Shininess     ("Shininess",       Float)  = 64
        _RimColor      ("Rim Color",       Color)  = (0.5, 0.8, 1, 1)
        _RimThreshold  ("Rim Threshold",   Range(0,1)) = 0.7
        _RimIntensity  ("Rim Intensity",   Float)  = 1.5
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _ShadowColor;
                float  _ShadowThreshold;
                float  _ShadowSmooth;
                float4 _SpecColor;
                float  _SpecSize;
                float  _Shininess;
                float4 _RimColor;
                float  _RimThreshold;
                float  _RimIntensity;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0;
                                float3 positionWS:TEXCOORD1; float4 shadowCoord:TEXCOORD2; float2 uv:TEXCOORD3; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionHCS = pi.positionCS;
                o.positionWS  = pi.positionWS;
                o.shadowCoord = GetShadowCoord(pi);
                o.normalWS    = TransformObjectToWorldNormal(i.normalOS);
                o.uv          = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir  = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light  light    = GetMainLight(input.shadowCoord);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // ── 色阶漫反射 ────────────────────────────────────
                float NdotL = dot(normalWS, light.direction);
                float cel   = smoothstep(_ShadowThreshold - _ShadowSmooth,
                                         _ShadowThreshold + _ShadowSmooth, NdotL);
                cel *= light.shadowAttenuation;
                half3 diffuse = albedo.rgb * lerp(_ShadowColor.rgb, light.color, cel);

                // ── 卡通高光 ──────────────────────────────────────
                float3 halfDir = normalize(light.direction + viewDir);
                float  NdotH   = saturate(dot(normalWS, halfDir));
                float  specCel = step(1.0 - _SpecSize, pow(NdotH, _Shininess));
                specCel *= saturate(NdotL);   // 背面不显示高光
                half3 specular = specCel * _SpecColor.rgb * light.color;

                // ── 边缘光 ────────────────────────────────────────
                float NdotV   = saturate(dot(normalWS, viewDir));
                float rimMask = dot(normalWS, light.direction) * 0.5 + 0.5;
                float rim     = (1.0 - NdotV) * rimMask;
                float rimCel  = step(1.0 - _RimThreshold, rim);
                half3 rimColor = rimCel * _RimColor.rgb * _RimIntensity;

                half3 finalColor = diffuse + specular + rimColor;
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}
```

---

## 小结

| 概念 | 要点 |
|------|------|
| 色阶漫反射 | `smoothstep(threshold-s, threshold+s, NdotL)` |
| Ramp 贴图 | 用 NdotL 做 U 采样一维渐变贴图，美术友好 |
| 卡通高光 | `step(1-specSize, pow(NdotH, shininess))`，硬边圆斑 |
| Rim Light | `1 - NdotV`，step 色阶化，可乘光照方向遮罩 |

下一篇：描边——顶点外扩法（背面扩展）与后处理描边（深度/法线边缘检测）。
