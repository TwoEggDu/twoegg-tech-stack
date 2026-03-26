---
title: "Shader 进阶技法 06｜布料 Shader：各向异性高光与天鹅绒效果"
slug: "shader-advanced-06-fabric"
date: "2026-03-26"
description: "布料的高光不是圆形的——丝绸的高光沿纤维方向拉伸，天鹅绒在逆光方向有模糊的毛边光。理解各向异性 BRDF（Kajiya-Kay 模型），以及天鹅绒的 Sheen 模型。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "布料"
  - "各向异性"
  - "天鹅绒"
  - "Sheen"
series: "Shader 手写技法"
weight: 4340
---
金属、皮肤之后，布料是另一类需要专用 BRDF 的材质。布料的高光不是各向同性的圆形光斑——丝绸沿纤维方向产生拉伸高光，天鹅绒在逆光方向呈现柔软的光边（Sheen）。

---

## 各向异性高光（Anisotropic Specular）

各向同性（Isotropic）材质：高光在任何方向看都一样（球形）——大多数材质如此。

各向异性（Anisotropic）材质：高光在某个方向被拉伸——金属拉丝、头发、丝绸、CD 光盘都是这种表现。

### Kajiya-Kay 模型（头发/纤维）

Kajiya-Kay 是头发和纤维材质的经典模型。它用**切线方向**（纤维方向）而不是法线来计算高光：

```hlsl
// T：切线方向（纤维走向）
// L：光方向
// V：视线方向

// sinTL = sin(T, L) 的近似：1 - dot²
float TdotL = dot(tangent, lightDir);
float sinTL = sqrt(max(0, 1.0 - TdotL * TdotL));

// sinTV = sin(T, V)
float TdotV = dot(tangent, viewDir);
float sinTV = sqrt(max(0, 1.0 - TdotV * TdotV));

// Kajiya-Kay 高光
float spec = pow(TdotL * TdotV + sinTL * sinTV, _Shininess);
spec = max(0, spec);
```

### 沿纤维方向偏移切线（丝绸/金属拉丝）

用高光偏移贴图（Anisotropy Map）让切线方向随表面细节变化：

```hlsl
// 采样各向异性贴图（R/G = 方向偏移，-1~1）
float2 anisoOffset = SAMPLE_TEXTURE2D(_AnisoMap, sampler_AnisoMap, input.uv).rg * 2.0 - 1.0;

// 偏移切线方向（在切线空间偏移后变换回世界空间）
float3 offsetTangent = normalize(tangentWS + normalWS * anisoOffset.x * _AnisoStrength
                                           + bitangentWS * anisoOffset.y * _AnisoStrength);

// 用偏移后的切线做 Kajiya-Kay 计算
```

---

## 天鹅绒效果（Sheen / Velvet）

天鹅绒（Velvet）的特征：
- **正面**：颜色较暗（纤维相互遮挡）
- **边缘 / 逆光**：柔软的亮边（Sheen），像一层光晕

Sheen 模型的核心是：**掠射角处（grazing angle）高光增强**，和 Fresnel 效果类似，但更柔和、更宽。

### Charlie Sheen 近似（URP Lit 内置）

```hlsl
// Sheen BRDF（Charlie 近似）
float D_Charlie(float roughness, float NdotH)
{
    float invR  = 1.0 / roughness;
    float cos2h = NdotH * NdotH;
    float sin2h = max(1.0 - cos2h, 0.0078125);  // 防止 0
    return (2.0 + invR) * pow(sin2h, invR * 0.5) / (2.0 * PI);
}

// 可见性项（Neubelt）
float V_Neubelt(float NdotV, float NdotL)
{
    return saturate(1.0 / (4.0 * (NdotL + NdotV - NdotL * NdotV)));
}

// 合并 Sheen
float  NdotH    = saturate(dot(normalWS, halfDir));
float  sheenD   = D_Charlie(_SheenRoughness, NdotH);
float  sheenV   = V_Neubelt(NdotV, NdotL);
half3  sheen    = sheenD * sheenV * _SheenColor.rgb * NdotL;
```

---

## 完整布料 Shader

```hlsl
Shader "Custom/FabricLit"
{
    Properties
    {
        _BaseMap        ("Albedo",          2D)           = "white" {}
        _BaseColor      ("Base Color",      Color)        = (1,1,1,1)
        [Toggle] _ANISO ("Anisotropic",     Float)        = 0
        _AnisoShift     ("Aniso Shift",     Range(-1,1))  = 0.1
        _Shininess1     ("Shininess 1",     Float)        = 64
        _Shininess2     ("Shininess 2",     Float)        = 16
        _SpecColor1     ("Specular Color 1", Color)       = (1,1,1,1)
        _SpecColor2     ("Specular Color 2", Color)       = (0.8,0.6,0.4,1)
        _SheenColor     ("Sheen Color",     Color)        = (0.9,0.9,0.9,1)
        _SheenRoughness ("Sheen Roughness", Range(0.1,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float  _AnisoShift; float _Shininess1; float _Shininess2;
                float4 _SpecColor1; float4 _SpecColor2;
                float4 _SheenColor; float _SheenRoughness;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float4 t:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 hcs:SV_POSITION; float3 normalWS:TEXCOORD0; float3 tangentWS:TEXCOORD1;
                                float3 bitangentWS:TEXCOORD2; float3 posWS:TEXCOORD3;
                                float4 shadowCoord:TEXCOORD4; float2 uv:TEXCOORD5; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.pos.xyz);
                VertexNormalInputs   ni = GetVertexNormalInputs(i.n, i.t);
                o.hcs = pi.positionCS; o.posWS = pi.positionWS;
                o.shadowCoord = GetShadowCoord(pi);
                o.normalWS    = ni.normalWS;
                o.tangentWS   = ni.tangentWS;
                o.bitangentWS = ni.bitangentWS;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            // Charlie Sheen D 项
            float D_Charlie(float roughness, float NdotH) {
                float invR  = 1.0 / max(roughness, 0.001);
                float cos2h = NdotH * NdotH;
                float sin2h = max(1.0 - cos2h, 0.0078125);
                return (2.0 + invR) * pow(sin2h, invR * 0.5) / (2.0 * 3.14159265);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 T = normalize(input.tangentWS);
                float3 B = normalize(input.bitangentWS);
                float3 V = normalize(GetWorldSpaceViewDir(input.posWS));

                Light  light = GetMainLight(input.shadowCoord);
                float3 L = light.direction;
                float3 H = normalize(L + V);

                float NdotL = saturate(dot(N, L));
                float NdotV = saturate(dot(N, V));
                float NdotH = saturate(dot(N, H));
                half  atten = light.shadowAttenuation;

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // ── 漫反射 ─────────────────────────────────────
                half3 diffuse = albedo.rgb * light.color * NdotL * atten;

                // ── 各向异性双高光（丝绸双层反射）──────────────
                // 偏移切线（主/副高光各偏不同量）
                float3 T1 = normalize(T + N * _AnisoShift);
                float3 T2 = normalize(T - N * _AnisoShift * 0.5);

                float TdotH1 = dot(T1, H); float sinTH1 = sqrt(max(0, 1-TdotH1*TdotH1));
                float TdotL1 = dot(T1, L); float sinTL1 = sqrt(max(0, 1-TdotL1*TdotL1));
                float TdotV1 = dot(T1, V); float sinTV1 = sqrt(max(0, 1-TdotV1*TdotV1));

                float TdotH2 = dot(T2, H); float sinTH2 = sqrt(max(0, 1-TdotH2*TdotH2));
                float TdotL2 = dot(T2, L); float sinTL2 = sqrt(max(0, 1-TdotL2*TdotL2));
                float TdotV2 = dot(T2, V); float sinTV2 = sqrt(max(0, 1-TdotV2*TdotV2));

                half spec1 = pow(max(0, TdotL1*TdotV1 + sinTL1*sinTV1), _Shininess1);
                half spec2 = pow(max(0, TdotL2*TdotV2 + sinTL2*sinTV2), _Shininess2);

                half3 specular = (_SpecColor1.rgb * spec1 + _SpecColor2.rgb * spec2)
                                 * light.color * NdotL * atten;

                // ── Sheen（天鹅绒边缘光）─────────────────────
                float sheenD  = D_Charlie(_SheenRoughness, NdotH);
                float sheenV  = saturate(1.0 / (4.0 * (NdotL + NdotV - NdotL * NdotV) + 0.0001));
                half3 sheen   = sheenD * sheenV * _SheenColor.rgb * NdotL * light.color * atten;

                return half4(diffuse + specular + sheen, albedo.a);
            }
            ENDHLSL
        }
    }
}
```

---

## 小结

| 效果 | 技术 | 关键参数 |
|------|------|---------|
| 丝绸各向异性 | Kajiya-Kay，切线偏移 | `_AnisoShift`：切线偏移量 |
| 双层高光 | 主/副切线各算一次 | `_Shininess1/2`，`_SpecColor1/2` |
| 天鹅绒 Sheen | Charlie D × Neubelt V | `_SheenRoughness`，`_SheenColor` |

下一篇：水体完整实现——把折射、反射、深度颜色、法线波动、泡沫全部整合成一个可用的水面 Shader。
