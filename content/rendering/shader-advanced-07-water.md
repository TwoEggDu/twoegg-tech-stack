---
title: "Shader 进阶技法 07｜水体完整实现：折射、反射、深度与泡沫"
slug: "shader-advanced-07-water"
date: "2026-03-26"
description: "一个完整的水面 Shader 需要整合多个技术：双层法线波动、折射（Opaque Texture）、反射（探针+Fresnel）、深度颜色过渡、岸边泡沫。这篇把这些模块组合成可直接使用的水面方案。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "水面"
  - "折射"
  - "反射"
  - "深度"
series: "Shader 手写技法"
weight: 4350
---
水面是游戏里最常见的复杂 Shader 之一，需要把多个技术模块整合：双层法线（波动）、折射（水下可见）、反射（水面镜面）、深度颜色（浅水浅色、深水深色）、泡沫（岸边）。

---

## 整体结构

```
水面 Shader 输出 =
    lerp(折射颜色, (深度颜色 + 水面颜色 + 镜面高光 + 反射), Fresnel)
    + 泡沫遮罩
```

Fresnel 决定折射和反射的比例——正视（NdotV 大）时折射更多，掠射（NdotV 小）时反射更多。

---

## 模块一：双层法线波动

两张法线贴图以不同方向和速度滚动叠加，产生自然的波纹：

```hlsl
float2 uv1 = input.uv * _WaveScale + float2( 0.04, 0.03) * _Time.y;
float2 uv2 = input.uv * _WaveScale + float2(-0.02, 0.05) * _Time.y + 0.5;

float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv1));
float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv2));
float3 normalTS = normalize(n1 + n2);   // 叠加两层法线

// 切线空间 → 世界空间
float3 normalWS = normalize(input.T * normalTS.x + input.B * normalTS.y + input.N * normalTS.z);
```

---

## 模块二：折射

用法线偏移屏幕 UV，采样 Camera Opaque Texture：

```hlsl
float2 screenUV   = input.screenPos.xy / input.screenPos.w;
float2 offset     = normalTS.xy * _RefractionStrength;
float2 refractUV  = saturate(screenUV + offset);

half3 refraction  = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,
                        sampler_CameraOpaqueTexture, refractUV).rgb;
```

---

## 模块三：深度颜色

比较水面深度和水底深度，浅处偏浅色，深处偏深色：

```hlsl
// 场景（水底）线性深度
float rawDepth    = SampleSceneDepth(screenUV);
float sceneDepth  = LinearEyeDepth(rawDepth, _ZBufferParams);

// 水面自身的线性深度
float waterDepth  = input.screenPos.w;

// 深度差 = 水的实际深度
float depthDiff   = sceneDepth - waterDepth;
float depthFade   = saturate(depthDiff / _DepthMaxDistance);  // 0=浅，1=深

// 浅水颜色 → 深水颜色
half3 waterColor  = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFade);

// 浅水更透明（alpha 随深度增加）
half  waterAlpha  = lerp(_ShallowAlpha, 1.0, depthFade);
```

---

## 模块四：Fresnel 反射

掠射角时更多反射，正视时更多折射：

```hlsl
float NdotV   = saturate(dot(normalWS, viewDir));
float fresnel = pow(1.0 - NdotV, 4.0);  // 掠射角时 fresnel 接近 1

// 采样反射探针
float3 reflectDir = reflect(-viewDir, normalWS);
half   mip        = 0.0;  // 水面光滑，用最清晰的 Mip
half4  envSample  = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDir, mip);
half3  envColor   = DecodeHDREnvironment(envSample, unity_SpecCube0_HDR);
```

---

## 模块五：岸边泡沫

浅水区（深度差很小）叠加泡沫噪声：

```hlsl
// 泡沫出现在浅水区（depthDiff 小的地方）
float foamDepth = saturate(depthDiff / _FoamDistance);  // 0=岸边，1=远离岸边
float foamMask  = 1.0 - foamDepth;   // 岸边=1，水中=0

// 采样泡沫噪声贴图（滚动）
float2 foamUV   = input.uv * _FoamScale + float2(_Time.y * 0.02, 0);
half   foam     = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).r;

// 噪声超过阈值才显示泡沫
foam = step(_FoamThreshold, foam) * foamMask;
```

---

## 完整水面 Shader

```hlsl
Shader "Custom/Water"
{
    Properties
    {
        _WaveNormal    ("Wave Normal",    2D)           = "bump" {}
        _WaveScale     ("Wave Scale",     Float)        = 1.0
        _RefractionStrength ("Refraction", Range(0,0.1)) = 0.03
        _ShallowColor  ("Shallow Color",  Color)        = (0.3,0.7,0.8,0.4)
        _DeepColor     ("Deep Color",     Color)        = (0.05,0.2,0.4,1)
        _ShallowAlpha  ("Shallow Alpha",  Range(0,1))   = 0.3
        _DepthMaxDistance("Depth Max",   Float)         = 5.0
        _Specular      ("Specular",       Float)        = 0.8
        _FoamTex       ("Foam Texture",   2D)           = "white" {}
        _FoamScale     ("Foam Scale",     Float)        = 3.0
        _FoamDistance  ("Foam Distance",  Float)        = 0.5
        _FoamThreshold ("Foam Threshold", Range(0,1))   = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WaveNormal_ST; float _WaveScale; float _RefractionStrength;
                float4 _ShallowColor; float4 _DeepColor; float _ShallowAlpha;
                float  _DepthMaxDistance; float _Specular;
                float4 _FoamTex_ST; float _FoamScale; float _FoamDistance; float _FoamThreshold;
            CBUFFER_END

            TEXTURE2D(_WaveNormal); SAMPLER(sampler_WaveNormal);
            TEXTURE2D(_FoamTex);    SAMPLER(sampler_FoamTex);

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float4 t:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 hcs:SV_POSITION; float4 screenPos:TEXCOORD0;
                                float3 T:TEXCOORD1; float3 B:TEXCOORD2; float3 N:TEXCOORD3;
                                float3 posWS:TEXCOORD4; float2 uv:TEXCOORD5; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.pos.xyz);
                VertexNormalInputs   ni = GetVertexNormalInputs(i.n, i.t);
                o.hcs = pi.positionCS; o.screenPos = ComputeScreenPos(pi.positionCS);
                o.posWS = pi.positionWS;
                o.T = ni.tangentWS; o.B = ni.bitangentWS; o.N = ni.normalWS;
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float3 viewDir  = normalize(GetWorldSpaceViewDir(input.posWS));
                Light  light    = GetMainLight();

                // ── 双层法线 ───────────────────────────────────
                float2 uv1 = input.uv * _WaveScale + float2( 0.04, 0.03) * _Time.y;
                float2 uv2 = input.uv * _WaveScale + float2(-0.02, 0.05) * _Time.y + 0.5;
                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv2));
                float3 normalTS = normalize(n1 + n2);
                float3 normalWS = normalize(input.T * normalTS.x + input.B * normalTS.y + input.N * normalTS.z);

                // ── 深度 ──────────────────────────────────────
                float rawDepth   = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterDepth = input.screenPos.w;
                float depthDiff  = max(0, sceneDepth - waterDepth);
                float depthFade  = saturate(depthDiff / _DepthMaxDistance);

                // ── 折射 ──────────────────────────────────────
                float2 refractUV = saturate(screenUV + normalTS.xy * _RefractionStrength);
                half3  refraction = SampleSceneColor(refractUV);

                // ── 水体颜色（深度过渡）───────────────────────
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFade);
                half  waterAlpha = lerp(_ShallowAlpha, 1.0, depthFade);

                // ── Fresnel ────────────────────────────────────
                float NdotV   = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, 4.0);

                // ── 反射（探针）──────────────────────────────
                float3 reflDir = reflect(-viewDir, normalWS);
                half4  envS    = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflDir, 0);
                half3  envColor = DecodeHDREnvironment(envS, unity_SpecCube0_HDR);

                // ── 镜面高光 ──────────────────────────────────
                float3 halfDir = normalize(light.direction + viewDir);
                half   NdotH   = saturate(dot(normalWS, halfDir));
                half3  specular = pow(NdotH, 256.0) * _Specular * light.color;

                // ── 合并：折射 ← Fresnel → 反射 ──────────────
                half3 underwater = lerp(refraction, waterColor, depthFade * 0.5);
                half3 surface    = lerp(underwater, envColor, fresnel) + specular;

                // ── 泡沫 ──────────────────────────────────────
                float foamMask   = saturate(1.0 - depthDiff / _FoamDistance);
                float2 foamUV    = input.uv * _FoamScale + float2(_Time.y * 0.02, 0);
                half   foam      = step(_FoamThreshold,
                    SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).r) * foamMask;
                surface = lerp(surface, half3(1,1,1), foam);

                return half4(surface, waterAlpha);
            }
            ENDHLSL
        }
    }
}
```

---

## 移动端简化策略

| 模块 | PC/主机 | 移动端高档 | 移动端中低档 |
|------|---------|----------|------------|
| 双层法线 | ✅ | ✅ | 单层法线 |
| 折射 | ✅ | ✅ | 固定底色 |
| 深度颜色 | ✅ | ✅ | 固定颜色 |
| 探针反射 | ✅ | ✅ | ✅（几乎免费） |
| 镜面高光 | ✅ | ✅ | 简化 |
| 泡沫 | ✅ | 可选 | 关闭 |

---

## 小结

水面 Shader = 双层法线（波动）+ 折射（Opaque Texture）+ 深度颜色（Depth Texture）+ Fresnel 反射（探针）+ 镜面高光 + 泡沫（深度遮罩）。各模块独立，可按平台性能分档开关。

下一篇：地形多层混合——用 Splat Map 在一张网格上混合多种地面材质（草、泥、石、雪）。
