+++
title = "Shader 核心技法 09｜视差贴图：用高度图模拟真实深度"
slug = "shader-technique-09-parallax-mapping"
date = 2026-03-26
description = "法线贴图只改变光照，不改变轮廓。视差贴图用高度图偏移 UV，让砖墙、石块的凹槽从斜角看有真实的深度感。理解 Parallax Offset、Steep Parallax 和性能取舍。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "技法", "视差贴图", "Parallax Mapping", "高度图"]
series = ["Shader 手写技法"]
[extra]
weight = 4250
+++

法线贴图改变了表面的光照方向，让平面"看起来"有凹凸——但从侧面看轮廓依然是直线，没有真实的深度感。视差贴图（Parallax Mapping）向前一步：根据高度图偏移 UV，让凹凸处的贴图位置发生视觉上的位移，产生真实的深度错觉。

---

## 原理：视线方向决定偏移量

真实的凹凸表面，从斜角看到的贴图会产生视差——凹陷处看到的位置比实际更靠近视线方向。视差贴图模拟这种现象：

```
偏移量 = 高度值 × 视线的切线空间方向
```

高度越高，偏移越多；视线越斜，偏移越大；正视（视线垂直表面）时偏移为零。

---

## 简单视差（Parallax Offset）

最简单的实现，一次采样：

```hlsl
// 需要把视线变换到切线空间（在 Vertex Shader 里计算）
// tangentViewDir：视线方向在切线空间的投影

float height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uv).r;

// UV 偏移：高度越高、视线越斜 → 偏移越大
float2 parallaxOffset = (tangentViewDir.xy / tangentViewDir.z) * height * _ParallaxScale;
float2 parallaxUV     = uv + parallaxOffset;
```

`_ParallaxScale` 控制凹凸幅度（0.01~0.1）。值过大时边缘会出现伪影。

---

## 切线空间视线方向

视差需要在切线空间里计算，因为高度图是相对于表面的：

```hlsl
// Vertex Shader 里计算切线空间视线
float3 viewDirWS = GetWorldSpaceViewDir(positionWS);

// TBN 矩阵（行向量形式）：把世界空间变换到切线空间
float3x3 worldToTangent = float3x3(tangentWS, bitangentWS, normalWS);
output.tangentViewDir   = mul(worldToTangent, viewDirWS);
```

---

## Steep Parallax Mapping（陡峭视差）

简单视差在高度差大时会出现明显错误。Steep Parallax 把高度分成多层，沿视线方向步进，找到视线与高度场交叉的层：

```hlsl
float2 ParallaxSteep(float2 uv, float3 viewDir, int layerCount)
{
    float layerDepth     = 1.0 / layerCount;
    float currentDepth   = 0.0;
    float2 deltaUV       = viewDir.xy / viewDir.z * _ParallaxScale / layerCount;
    float2 currentUV     = uv;
    float  heightSample  = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, currentUV).r;

    [loop]
    for (int i = 0; i < layerCount; i++)
    {
        if (currentDepth >= heightSample) break;
        currentUV    -= deltaUV;
        heightSample  = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, currentUV).r;
        currentDepth += layerDepth;
    }
    return currentUV;
}
```

层数越多越精确，但采样次数线性增加。通常 8~16 层在质量和性能间取得平衡。

---

## Parallax Occlusion Mapping（POM）

Steep Parallax 的升级版：在找到交叉层后，在上一层和当前层之间线性插值，得到更精确的交叉点：

```hlsl
float2 ParallaxOcclusion(float2 uv, float3 viewDir, int layerCount)
{
    // ... 同 Steep 的步进循环 ...

    // 找到交叉层后，在前后两层之间插值
    float2 prevUV    = currentUV + deltaUV;
    float  prevDepth = currentDepth - layerDepth;
    float  prevH     = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, prevUV).r;

    float  afterDelta  = heightSample - currentDepth;
    float  beforeDelta = prevH - prevDepth;
    float  weight      = afterDelta / (afterDelta - beforeDelta);

    return lerp(currentUV, prevUV, weight);
}
```

POM 用较少的层数就能达到 Steep Parallax 需要更多层才能达到的精度。

---

## 完整视差 Shader

```hlsl
Shader "Custom/ParallaxLit"
{
    Properties
    {
        _BaseMap      ("Albedo",        2D)            = "white" {}
        _NormalMap    ("Normal Map",    2D)            = "bump" {}
        _HeightMap    ("Height Map",    2D)            = "gray" {}
        _ParallaxScale("Parallax Scale",Range(0,0.1)) = 0.05
        _ParallaxLayers("Parallax Layers", Range(4, 32)) = 16
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
                float4 _BaseMap_ST;
                float  _ParallaxScale;
                float  _ParallaxLayers;
            CBUFFER_END

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float4 t:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 posHCS:SV_POSITION; float2 uv:TEXCOORD0;
                                float3 T:TEXCOORD1; float3 B:TEXCOORD2; float3 N:TEXCOORD3;
                                float3 posWS:TEXCOORD4; float4 shadowCoord:TEXCOORD5;
                                float3 tangentViewDir:TEXCOORD6; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.pos.xyz);
                VertexNormalInputs   ni = GetVertexNormalInputs(i.n, i.t);
                o.posHCS      = pi.positionCS;
                o.posWS       = pi.positionWS;
                o.shadowCoord = GetShadowCoord(pi);
                o.uv          = TRANSFORM_TEX(i.uv, _BaseMap);
                o.T = ni.tangentWS; o.B = ni.bitangentWS; o.N = ni.normalWS;

                float3 viewDir = GetWorldSpaceViewDir(pi.positionWS);
                float3x3 w2t   = float3x3(ni.tangentWS, ni.bitangentWS, ni.normalWS);
                o.tangentViewDir = mul(w2t, viewDir);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 vd = normalize(input.tangentViewDir);

                // ── POM UV 偏移 ───────────────────────────────────
                int    layers    = max(4, (int)_ParallaxLayers);
                float  layerH    = 1.0 / layers;
                float  curDepth  = 0.0;
                float2 dUV       = vd.xy / max(vd.z, 0.01) * _ParallaxScale / layers;
                float2 curUV     = input.uv;
                float  curH      = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, curUV).r;

                [loop]
                for (int i = 0; i < layers; i++) {
                    if (curDepth >= curH) break;
                    curUV   -= dUV;
                    curH     = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, curUV).r;
                    curDepth += layerH;
                }
                // 插值（POM 精化）
                float2 prevUV = curUV + dUV;
                float  prevH  = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, prevUV).r;
                float  w      = (curH - curDepth) / max((curH - curDepth) - (prevH - (curDepth - layerH)), 0.001);
                float2 finalUV = lerp(curUV, prevUV, w);

                // ── 正常光照（使用偏移后的 UV）───────────────────
                half4  albedo  = SAMPLE_TEXTURE2D(_BaseMap,   sampler_BaseMap,   finalUV);
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, finalUV));
                float3 normalWS = normalize(input.T * normalTS.x + input.B * normalTS.y + input.N * normalTS.z);

                Light  light = GetMainLight(input.shadowCoord);
                half   NdotL = saturate(dot(normalWS, light.direction));
                half3  color = albedo.rgb * light.color * NdotL * light.shadowAttenuation;
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }
}
```

---

## 性能与适用场景

| 方案 | 采样次数 | 质量 | 适用 |
|------|---------|------|------|
| 简单视差 | 2（高度+贴图） | 低，边缘错误明显 | 轻度起伏，如布料 |
| Steep Parallax | 8~16+ | 中，无接缝伪影 | 砖墙、石块 |
| POM | 8~16 层 + 插值 | 高 | PC/主机，质量要求高 |

**移动端**：视差贴图在移动端开销较高（多次纹理采样）。中低档设备关闭，高档设备限制层数为 8。通过 `shader_feature_local _PARALLAX` 做成可开关变体。

---

## 小结

| 概念 | 要点 |
|------|------|
| 视差原理 | 高度图偏移 UV，斜视时产生深度错觉 |
| 切线空间视线 | 顶点阶段计算，世界视线 × TBN 矩阵 |
| 简单视差 | 一次高度采样，快但不精确 |
| Steep Parallax | 分层步进，找交叉层 |
| POM | 步进后插值，精度更高，层数可减少 |

下一篇：屏幕空间 UV——用屏幕坐标做扫描线、噪声覆盖、X-Ray 透视等效果。
