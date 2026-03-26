+++
title = "项目实战 02｜写实武器 Shader：PBR + 磨损细节 + 自发光能量槽"
slug = "shader-project-02-realistic-weapon"
date = 2026-03-26
description = "写实武器需要传达材质质感——金属光泽、边缘磨损、科幻能量槽的自发光脉冲。在标准 PBR 工作流上叠加磨损遮罩、细节法线和自发光动画，组合出有说服力的武器表现。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "项目实战", "PBR", "武器", "写实", "自发光"]
series = ["Shader 手写技法"]
[extra]
weight = 4410
+++

写实武器 Shader 的核心是在 PBR 基础上叠加几个额外的表现层：**磨损（Wear）细节**让武器显得经过使用，**自发光能量槽（Energy Channel）**在科幻武器上制造科技感，**细节法线**增加近距离的表面质感。

---

## 整体结构

```
写实武器输出 =
    PBR 基础（UniversalFragmentPBR）
    + 磨损遮罩（边缘磨损 = 暴露底层金属）
    + 细节法线（叠加微观表面细节）
    + 自发光脉冲（能量槽按曲线闪烁）
```

---

## 模块一：PBR 基础贴图布局

写实武器通常使用 **Mask Map** 打包贴图（减少采样次数）：

```
BaseMap (RGBA)：
    RGB = 固有色（Albedo）
    A   = Alpha（不透明武器通常不用，可存 AO 或磨损权重）

MaskMap (RGBA)：
    R = Metallic（金属度）
    G = Occlusion（环境遮蔽）
    B = Detail Mask（细节法线强度遮罩）
    A = Smoothness（光滑度）

NormalMap：切线空间法线
DetailNormalMap：细节法线（近距离表面纹理）
EmissionMap：自发光区域遮罩
```

---

## 模块二：磨损细节

磨损通常发生在**凸起的边缘和棱角**——法线朝外的地方更容易被磨损。

**曲率近似**：用法线与模型中心方向的夹角近似曲率（简单但有效）。更精确的做法是在 DCC 工具里烘焙 Curvature Map：

```hlsl
// 采样曲率贴图（白色=凸出边缘，黑色=凹面/平面）
half curvature = SAMPLE_TEXTURE2D(_CurvatureMap, sampler_CurvatureMap, input.uv).r;

// 磨损遮罩：曲率高的地方磨损更多
// _WearAmount 控制整体磨损程度（0=全新，1=严重磨损）
half wearMask = step(1.0 - _WearAmount, curvature);

// 磨损处暴露底层金属（Metallic 升高，Smoothness 降低，Albedo 变为金属色）
half3  baseAlbedo   = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
half4  mask         = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);

half   metallic     = lerp(mask.r,   1.0,           wearMask);  // 磨损处变纯金属
half   smoothness   = lerp(mask.a,   _WearSmoothness, wearMask);  // 磨损处可能更粗糙
half3  albedo       = lerp(baseAlbedo, _WearColor.rgb, wearMask);  // 磨损处底层金属色
```

---

## 模块三：细节法线叠加

细节法线覆盖表面微观纹理（划痕、拉丝、凹坑），使用更小的 Tiling：

```hlsl
// 主法线
float3 normalTS = UnpackNormalScale(
    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);

// 细节法线（更高 Tiling，用于近距离效果）
float2 detailUV  = input.uv * _DetailTiling;
float3 detailN   = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, detailUV));

// 细节遮罩控制细节强度（平面区域用全强度细节，凹槽区域细节弱）
half   detailMask = mask.b;  // MaskMap B 通道
detailN = lerp(float3(0,0,1), detailN, detailMask * _DetailNormalScale);

// 叠加：Reoriented Normal Mapping（比直接相加更正确）
float3 blendedN;
blendedN.xy = normalTS.xy + detailN.xy;
blendedN.z  = normalTS.z;
blendedN    = normalize(blendedN);

float3 normalWS = TransformTangentToWorld(blendedN,
    half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
```

---

## 模块四：自发光能量槽脉冲

科幻武器的能量槽：采样自发光遮罩 → 乘以颜色 → 乘以脉冲强度（sin 波或 Curve）：

```hlsl
// 采样自发光遮罩（白色区域发光）
half emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).r;

// 脉冲动画（sin 波形，0~1 之间振荡）
float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;    // 0~1

// 可选：充能效果（从 0 到 1 线性增长，然后保持亮）
// float charge = saturate(_ChargeLevel);  // 由 C# 控制 Material Property

// HDR 自发光颜色（Intensity > 1 触发 Bloom）
half3 emission = emissionMask * _EmissionColor.rgb * _EmissionIntensity * pulse;
```

---

## 完整武器 Shader

```hlsl
Shader "Custom/RealisticWeapon"
{
    Properties
    {
        _BaseMap        ("Albedo",            2D)          = "white" {}
        _BaseColor      ("Base Color",        Color)       = (1,1,1,1)
        _NormalMap      ("Normal Map",        2D)          = "bump" {}
        _NormalScale    ("Normal Scale",      Range(0,2))  = 1.0
        _MaskMap        ("Mask (M/AO/D/S)",   2D)          = "white" {}
        _DetailNormal   ("Detail Normal",     2D)          = "bump" {}
        _DetailTiling   ("Detail Tiling",     Float)       = 8.0
        _DetailNormalScale ("Detail Intensity", Range(0,2))= 0.5

        [Header(Wear)]
        _CurvatureMap   ("Curvature Map",     2D)          = "black" {}
        _WearAmount     ("Wear Amount",       Range(0,1))  = 0.2
        _WearColor      ("Wear Metal Color",  Color)       = (0.7,0.65,0.6,1)
        _WearSmoothness ("Wear Smoothness",   Range(0,1))  = 0.3

        [Header(Emission)]
        _EmissionMap    ("Emission Mask",     2D)          = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color)     = (0,0.5,2,1)
        _EmissionIntensity ("Emission Intensity", Range(0,5)) = 2.0
        _PulseSpeed     ("Pulse Speed",       Range(0,10)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "WeaponForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float  _NormalScale;
                float  _DetailTiling; float _DetailNormalScale;
                float  _WearAmount; float4 _WearColor; float _WearSmoothness;
                float4 _EmissionColor; float _EmissionIntensity; float _PulseSpeed;
            CBUFFER_END

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);    SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MaskMap);      SAMPLER(sampler_MaskMap);
            TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);
            TEXTURE2D(_CurvatureMap); SAMPLER(sampler_CurvatureMap);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);

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
                // ── 采样贴图 ───────────────────────────────────
                half4  baseColor    = SAMPLE_TEXTURE2D(_BaseMap,    sampler_BaseMap,    input.uv) * _BaseColor;
                half4  mask         = SAMPLE_TEXTURE2D(_MaskMap,    sampler_MaskMap,    input.uv);
                half   curvature    = SAMPLE_TEXTURE2D(_CurvatureMap, sampler_CurvatureMap, input.uv).r;
                half   emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).r;

                // ── 磨损 ──────────────────────────────────────
                half wearMask   = step(1.0 - _WearAmount, curvature);
                half3  albedo   = lerp(baseColor.rgb, _WearColor.rgb, wearMask);
                half   metallic = lerp(mask.r, 1.0,              wearMask);
                half   smooth   = lerp(mask.a, _WearSmoothness,  wearMask);
                half   occlusion = mask.g;

                // ── 法线（主 + 细节）──────────────────────────
                float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
                float3 dN  = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, input.uv * _DetailTiling));
                dN = lerp(float3(0,0,1), dN, mask.b * _DetailNormalScale);
                float3 blendN = normalize(float3(nTS.xy + dN.xy, nTS.z));
                float3 normalWS = normalize(TransformTangentToWorld(blendN,
                    half3x3(input.tangentWS, input.bitangentWS, input.normalWS)));

                // ── 自发光脉冲 ────────────────────────────────
                float pulse    = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                half3 emission = emissionMask * _EmissionColor.rgb * _EmissionIntensity * pulse;

                // ── PBR 光照（UniversalFragmentPBR）──────────
                InputData inputData = (InputData)0;
                inputData.positionWS        = input.posWS;
                inputData.normalWS          = normalWS;
                inputData.viewDirectionWS   = normalize(GetWorldSpaceViewDir(input.posWS));
                inputData.shadowCoord       = input.shadowCoord;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.hcs);
                inputData.bakedGI           = SampleSH(normalWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo        = albedo;
                surface.metallic      = metallic;
                surface.smoothness    = smooth;
                surface.occlusion     = occlusion;
                surface.emission      = emission;
                surface.alpha         = 1.0;
                surface.normalTS      = blendN;

                return UniversalFragmentPBR(inputData, surface);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertShadow
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

## 充能效果变体

能量槽从空到满的充能动画，通过 C# 控制 `_ChargeLevel`（0→1）：

```csharp
// C# 控制充能进度
material.SetFloat("_ChargeLevel", Mathf.Lerp(0, 1, chargeProgress));
```

```hlsl
// Shader 里替换 pulse 逻辑
float charge = saturate(_ChargeLevel);
// 充能完成后的待机脉冲：充能时线性增长，满后缓慢脉冲
float idlePulse = sin(_Time.y * _PulseSpeed) * 0.15 + 0.85;  // 0.7~1.0 微弱跳动
float finalIntensity = lerp(charge, idlePulse, step(0.99, charge));
half3 emission = emissionMask * _EmissionColor.rgb * _EmissionIntensity * finalIntensity;
```

---

## 小结

写实武器 Shader = PBR 基础（Metallic/Smoothness/Occlusion Mask Map）+ 曲率磨损遮罩（边缘暴露底层金属）+ 细节法线叠加（微观划痕/拉丝）+ 自发光脉冲（HDR + Bloom）。这套结构可以直接扩展到盔甲、道具等其他写实物件。

下一篇：写实水面完整实现——在进阶水体的基础上加入 Gerstner 顶点波浪和动态泡沫轨迹。
