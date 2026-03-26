+++
title = "项目实战 01｜卡通角色完整 Shader：NPR 渲染的全套组合"
slug = "shader-project-01-toon-character"
date = 2026-03-26
description = "一个完整的卡通角色 Shader 需要整合多个 NPR 技法：Cel 色阶漫反射、Ramp 贴图、面部阴影修正、各向异性头发高光、轮廓描边（顶点外扩）、Rim Light。这篇把这些模块拼成一套可直接用的角色方案。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "项目实战", "NPR", "卡通渲染", "角色"]
series = ["Shader 手写技法"]
[extra]
weight = 4400
+++

卡通角色渲染（NPR，Non-Photorealistic Rendering）是游戏开发中最常见的需求之一。单独用一篇讲 Cel Shading，另一篇讲描边，在实际项目里需要把它们整合成一套方案。

---

## 整体结构

```
卡通角色输出 =
    漫反射（Cel 色阶 / Ramp）
    + 高光（各向异性 / 普通卡通高光）
    + Rim Light（边缘光）
    + 描边（背面外扩，独立 Pass）
```

通常用**两个 Pass**：
1. `ForwardLit`：正面光照（漫反射 + 高光 + Rim）
2. `Outline`：背面外扩描边

---

## 模块一：Ramp 漫反射

比 step 色阶更灵活——用一张 1D 渐变贴图控制明暗过渡形状：

```hlsl
// NdotL → [0,1]，作为 Ramp 贴图的 U 坐标
float  NdotL = saturate(dot(normalWS, light.direction));

// 采样 Ramp：1×256 的渐变贴图，美术可自由设计色阶
half3  ramp  = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NdotL, 0.5)).rgb;

// 乘以固有色
half3  diffuse = albedo * ramp * light.color;
```

Ramp 贴图的几种常见设计：
- 硬色阶：两段颜色中间有硬边（日系卡通）
- 软渐变：过渡柔和（写实风格）
- 三段色（亮 / 中间调 / 暗）：在贴图里直接设计暗部的冷色偏移

---

## 模块二：卡通高光

卡通高光通常是一个硬边圆斑（而不是 Blinn-Phong 的渐变）：

```hlsl
float3 halfDir = normalize(light.direction + viewDir);
float  NdotH   = saturate(dot(normalWS, halfDir));

// step 产生硬边高光
half   specMask = step(_SpecThreshold, pow(NdotH, _SpecShininess));

// 可选：用 smoothstep 软化边缘
// half specMask = smoothstep(_SpecThreshold - _SpecSoftness,
//                            _SpecThreshold + _SpecSoftness, pow(NdotH, _SpecShininess));

half3  specular  = specMask * _SpecColor.rgb * light.color;
```

---

## 模块三：各向异性头发高光（Kajiya-Kay）

头发使用切线方向而非法线计算高光，并沿 UV 方向偏移切线模拟发丝：

```hlsl
// 采样高光偏移贴图（存储切线偏移量）
half  shift   = SAMPLE_TEXTURE2D(_HairShiftTex, sampler_HairShiftTex, input.uv).r - 0.5;

// 沿法线偏移切线（主高光 / 副高光各偏不同量）
float3 T1 = normalize(input.tangentWS + input.normalWS * (shift + _SpecShift1));
float3 T2 = normalize(input.tangentWS + input.normalWS * (shift + _SpecShift2));

// Kajiya-Kay 高光
float TdotH1 = dot(T1, halfDir);
half  hairSpec1 = sqrt(max(0, 1 - TdotH1 * TdotH1));
hairSpec1 = pow(hairSpec1, _HairShininess1) * step(0.0, TdotH1);

float TdotH2 = dot(T2, halfDir);
half  hairSpec2 = sqrt(max(0, 1 - TdotH2 * TdotH2));
hairSpec2 = pow(hairSpec2, _HairShininess2) * step(0.0, TdotH2);

half3 hairSpecular = (_HairSpecColor1.rgb * hairSpec1
                   + _HairSpecColor2.rgb * hairSpec2) * light.color * NdotL;
```

---

## 模块四：Rim Light（边缘光）

掠射角处叠加一层彩色边缘光，增强卡通角色的立体感：

```hlsl
float NdotV  = saturate(dot(normalWS, viewDir));
float rim    = pow(1.0 - NdotV, _RimPower);

// 只在受光面显示 Rim（用 NdotL 遮罩）
rim = rim * saturate(NdotL * 2.0);

half3 rimLight = rim * _RimColor.rgb * _RimIntensity;
```

---

## 模块五：描边 Pass（背面外扩）

描边用独立 Pass，在裁剪空间沿法线方向外扩顶点，只渲染背面：

```hlsl
Pass
{
    Name "Outline"
    Cull Front   // 只渲染背面

    HLSLPROGRAM
    #pragma vertex vertOutline
    #pragma fragment fragOutline

    Varyings vertOutline(Attributes i)
    {
        Varyings o;
        // 裁剪空间等宽外扩
        float4 clipPos  = TransformObjectToHClip(i.pos.xyz);
        float3 clipNorm = mul((float3x3)UNITY_MATRIX_VP,
                              TransformObjectToWorldNormal(i.n));

        // 按屏幕宽高比修正，保证描边在任何分辨率下等宽
        float2 extend   = normalize(clipNorm.xy) * (_OutlineWidth / _ScreenParams.y);
        clipPos.xy      += extend * clipPos.w;   // 乘以 w 保证透视不变形

        o.hcs = clipPos;
        return o;
    }

    half4 fragOutline(Varyings i) : SV_Target
    {
        return _OutlineColor;
    }
    ENDHLSL
}
```

---

## 面部阴影修正

卡通角色面部的灯光阴影在旋转时容易产生难看的边缘抖动。常见方案：**SDF 面部阴影贴图**。

美术在静态 A 面/B 面两张图里预烘焙面部光照的 SDF 分布，运行时根据光源方向插值采样，避免实时法线产生的噪点阴影：

```hlsl
// 计算光源在面部平面上的投影方向（XZ 平面）
float3 lightDirFlat = normalize(float3(light.direction.x, 0, light.direction.z));
float  lightAngle   = dot(headForward, lightDirFlat);   // -1~1
float  lightSide    = dot(headRight,   lightDirFlat);   // 正=右，负=左

// UV 翻转：光在左边时水平翻转采样坐标
float2 sdfUV = input.uv;
if (lightSide < 0) sdfUV.x = 1.0 - sdfUV.x;

// 采样 SDF 贴图（值表示"这个像素在该角度下是否受光"的阈值）
half sdfValue = SAMPLE_TEXTURE2D(_FaceShadowTex, sampler_FaceShadowTex, sdfUV).r;

// lightAngle 映射到 [0,1] 作为比较阈值
float threshold = lightAngle * 0.5 + 0.5;
half  faceShadow = step(1.0 - threshold, sdfValue);    // 0=阴影，1=受光
```

---

## 完整角色 Shader 框架

```hlsl
Shader "Custom/ToonCharacter"
{
    Properties
    {
        _BaseMap        ("Albedo",           2D)          = "white" {}
        _BaseColor      ("Base Color",       Color)       = (1,1,1,1)
        _RampTex        ("Ramp Texture",     2D)          = "white" {}

        [Header(Specular)]
        _SpecColor      ("Specular Color",   Color)       = (1,1,1,1)
        _SpecThreshold  ("Spec Threshold",   Range(0,1))  = 0.7
        _SpecShininess  ("Spec Shininess",   Float)       = 128
        _SpecSoftness   ("Spec Softness",    Range(0,0.1))= 0.02

        [Header(Rim Light)]
        _RimColor       ("Rim Color",        Color)       = (0.8,0.9,1,1)
        _RimPower       ("Rim Power",        Range(1,8))  = 3.0
        _RimIntensity   ("Rim Intensity",    Range(0,2))  = 0.5

        [Header(Outline)]
        _OutlineColor   ("Outline Color",    Color)       = (0.1,0.1,0.1,1)
        _OutlineWidth   ("Outline Width",    Range(0,5))  = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // ── Pass 1：正面光照 ───────────────────────────────────
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float4 _SpecColor; float _SpecThreshold; float _SpecShininess; float _SpecSoftness;
                float4 _RimColor; float _RimPower; float _RimIntensity;
                float4 _OutlineColor; float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 hcs:SV_POSITION; float3 normalWS:TEXCOORD0;
                                float3 posWS:TEXCOORD1; float4 shadowCoord:TEXCOORD2;
                                float2 uv:TEXCOORD3; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.pos.xyz);
                o.hcs = pi.positionCS; o.posWS = pi.positionWS;
                o.shadowCoord = GetShadowCoord(pi);
                o.normalWS    = TransformObjectToWorldNormal(i.n);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(input.posWS));
                Light  light = GetMainLight(input.shadowCoord);
                float3 L = light.direction;
                float3 H = normalize(L + V);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // ── 漫反射（Ramp）─────────────────────────────
                float NdotL  = saturate(dot(N, L));
                // 阴影衰减融入 Ramp UV：有阴影时把 NdotL 压暗
                float rampU  = NdotL * light.shadowAttenuation;
                half3 ramp   = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampU, 0.5)).rgb;
                half3 diffuse = albedo.rgb * ramp * light.color;

                // ── 卡通高光 ──────────────────────────────────
                float NdotH  = saturate(dot(N, H));
                half  spec   = smoothstep(_SpecThreshold - _SpecSoftness,
                                          _SpecThreshold + _SpecSoftness,
                                          pow(NdotH, _SpecShininess));
                half3 specular = spec * _SpecColor.rgb * light.color * NdotL;

                // ── Rim Light ──────────────────────────────────
                float NdotV  = saturate(dot(N, V));
                float rim    = pow(1.0 - NdotV, _RimPower) * saturate(NdotL * 2.0);
                half3 rimLight = rim * _RimColor.rgb * _RimIntensity;

                // ── 环境光 ────────────────────────────────────
                half3 ambient = albedo.rgb * SampleSH(N) * 0.5;

                return half4(diffuse + specular + rimLight + ambient, albedo.a);
            }
            ENDHLSL
        }

        // ── Pass 2：描边 ───────────────────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex   vertOutline
            #pragma fragment fragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor; float _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; };
            struct Varyings   { float4 hcs:SV_POSITION; };

            Varyings vertOutline(Attributes i) {
                Varyings o;
                float4 clipPos  = TransformObjectToHClip(i.pos.xyz);
                float3 clipNorm = mul((float3x3)UNITY_MATRIX_VP,
                                     TransformObjectToWorldNormal(i.n));
                clipPos.xy += normalize(clipNorm.xy) * (_OutlineWidth * 0.001) * clipPos.w;
                o.hcs = clipPos;
                return o;
            }

            half4 fragOutline(Varyings i) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        // ── Shadow Caster ─────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; };
            struct Varyings   { float4 hcs:SV_POSITION; };
            Varyings vertShadow(Attributes i) {
                Varyings o;
                float3 posWS  = TransformObjectToWorld(i.pos.xyz);
                float3 normWS = TransformObjectToWorldNormal(i.n);
                o.hcs = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                return o;
            }
            half4 fragShadow(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
```

---

## 移动端策略

| 模块 | 高档 | 中档 | 低档 |
|------|------|------|------|
| Ramp 漫反射 | ✅ Ramp 贴图 | ✅ | 固定色阶 step |
| 卡通高光 | ✅ smoothstep 软边 | ✅ | step 硬边 |
| Rim Light | ✅ | ✅ | 关闭 |
| 描边 Pass | ✅ | ✅ | 关闭 |
| 各向异性头发 | ✅ | 普通卡通高光 | 普通卡通高光 |
| SDF 面部阴影 | ✅ | ✅ | 关闭 |

---

## 小结

卡通角色 Shader = Ramp 漫反射（Ramp 贴图控制色阶）+ 硬边卡通高光（smoothstep 软化）+ Rim Light（边缘光增强立体感）+ 背面外扩描边（独立 Pass）。头发区域可替换为 Kajiya-Kay 各向异性高光，面部区域可接入 SDF 阴影贴图消除噪点阴影。

下一篇：写实武器 Shader——PBR 基础 + 磨损细节遮罩 + 自发光能量槽，组合出有说服力的写实质感。
