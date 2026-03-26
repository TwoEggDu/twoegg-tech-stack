+++
title = "Shader 进阶技法 08｜地形多层混合：Splat Map 方案"
slug = "shader-advanced-08-splat-map"
date = 2026-03-26
description = "地形需要在一张网格上显示多种地表材质（草、泥、石、雪）。Splat Map 用一张 RGBA 纹理存储每种材质的混合权重，Shader 采样各层并按权重混合。这篇实现一个 4 层地形混合 Shader。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "进阶", "地形", "Splat Map", "多层混合"]
series = ["Shader 手写技法"]
[extra]
weight = 4360
+++

Unity 内置 Terrain 有自己的 Terrain Shader，但当你需要自定义地表（如 Mesh 地形、程序化地形、特效地形）时，需要自己实现 Splat Map 混合。

---

## 核心思路

```
Splat Map（RGBA）= 4 种地表材质的混合权重
    R → 草地（Grass）
    G → 泥土（Dirt）
    B → 岩石（Rock）
    A → 雪地（Snow）

最终颜色 = 草地颜色 * R + 泥土颜色 * G + 岩石颜色 * B + 雪地颜色 * A
```

Splat Map 的四个通道之和通常为 1（归一化），由美术工具（如 Unity Terrain Paint、Substance、Houdini）烘焙生成。

---

## 基础 4 层混合

```hlsl
// 采样 Splat Map，获取各层权重
half4 splatControl = SAMPLE_TEXTURE2D(_SplatMap, sampler_SplatMap, input.uv);

// 各层使用独立 UV（各自 Tiling 不同）
float2 uvGrass = input.uv * _GrassTiling;
float2 uvDirt  = input.uv * _DirtTiling;
float2 uvRock  = input.uv * _RockTiling;
float2 uvSnow  = input.uv * _SnowTiling;

// 采样各层 Albedo
half3 colGrass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uvGrass).rgb;
half3 colDirt  = SAMPLE_TEXTURE2D(_DirtTex,  sampler_DirtTex,  uvDirt ).rgb;
half3 colRock  = SAMPLE_TEXTURE2D(_RockTex,  sampler_RockTex,  uvRock ).rgb;
half3 colSnow  = SAMPLE_TEXTURE2D(_SnowTex,  sampler_SnowTex,  uvSnow ).rgb;

// 线性混合
half3 albedo = colGrass * splatControl.r
             + colDirt  * splatControl.g
             + colRock  * splatControl.b
             + colSnow  * splatControl.a;
```

---

## 法线贴图混合

各层也可以有自己的法线贴图，同样按权重混合：

```hlsl
float3 nGrass = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, uvGrass));
float3 nDirt  = UnpackNormal(SAMPLE_TEXTURE2D(_DirtNormal,  sampler_DirtNormal,  uvDirt ));
float3 nRock  = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal,  sampler_RockNormal,  uvRock ));
float3 nSnow  = UnpackNormal(SAMPLE_TEXTURE2D(_SnowNormal,  sampler_SnowNormal,  uvSnow ));

// 法线在切线空间按权重叠加（叠加前不做归一化，叠加后统一归一化）
float3 blendedNormal = nGrass * splatControl.r
                     + nDirt  * splatControl.g
                     + nRock  * splatControl.b
                     + nSnow  * splatControl.a;
blendedNormal = normalize(blendedNormal);

// 切线空间 → 世界空间
float3 normalWS = TransformTangentToWorld(blendedNormal,
    half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
```

---

## 基于高度的混合（Height-Based Blending）

纯线性混合在边界处过渡比较生硬。基于高度图的混合可以让边缘更自然，比如草和泥交界处会凹凸参差：

```hlsl
// 各层高度贴图（R 通道）
half hGrass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uvGrass).a;  // 用 alpha 存高度
half hDirt  = SAMPLE_TEXTURE2D(_DirtTex,  sampler_DirtTex,  uvDirt ).a;
half hRock  = SAMPLE_TEXTURE2D(_RockTex,  sampler_RockTex,  uvRock ).a;
half hSnow  = SAMPLE_TEXTURE2D(_SnowTex,  sampler_SnowTex,  uvSnow ).a;

// 将 Splat 权重加上高度，再做 softmax 归一化
float4 blendHeight;
blendHeight.r = splatControl.r + hGrass;
blendHeight.g = splatControl.g + hDirt;
blendHeight.b = splatControl.b + hRock;
blendHeight.a = splatControl.a + hSnow;

// 只保留高度最大的几层（软化裁剪）
float maxH  = max(max(blendHeight.r, blendHeight.g), max(blendHeight.b, blendHeight.a));
float threshold = maxH - _BlendSharpness;   // _BlendSharpness 越大，边界越硬（0.1~0.5）

blendHeight = max(blendHeight - threshold, 0);
float totalWeight = dot(blendHeight, 1.0);
float4 weights = blendHeight / (totalWeight + 0.0001);  // 重新归一化

// 用新权重混合
half3 albedo = colGrass * weights.r
             + colDirt  * weights.g
             + colRock  * weights.b
             + colSnow  * weights.a;
```

效果：草和泥的交界处，高处的草（高度图亮处）会保留，低处（凹陷）变为泥土，更接近真实感。

---

## 完整地形 Shader

```hlsl
Shader "Custom/Terrain4Layer"
{
    Properties
    {
        _SplatMap     ("Splat Map",   2D) = "red" {}

        _GrassTex     ("Grass Albedo+H", 2D) = "white" {}
        _DirtTex      ("Dirt Albedo+H",  2D) = "white" {}
        _RockTex      ("Rock Albedo+H",  2D) = "white" {}
        _SnowTex      ("Snow Albedo+H",  2D) = "white" {}

        _GrassNormal  ("Grass Normal", 2D) = "bump" {}
        _DirtNormal   ("Dirt Normal",  2D) = "bump" {}
        _RockNormal   ("Rock Normal",  2D) = "bump" {}
        _SnowNormal   ("Snow Normal",  2D) = "bump" {}

        _GrassTiling  ("Grass Tiling", Float) = 8
        _DirtTiling   ("Dirt Tiling",  Float) = 6
        _RockTiling   ("Rock Tiling",  Float) = 4
        _SnowTiling   ("Snow Tiling",  Float) = 5

        _BlendSharpness ("Blend Sharpness", Range(0.01,1)) = 0.2
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
                float4 _SplatMap_ST;
                float _GrassTiling; float _DirtTiling; float _RockTiling; float _SnowTiling;
                float _BlendSharpness;
            CBUFFER_END

            TEXTURE2D(_SplatMap);  SAMPLER(sampler_SplatMap);
            TEXTURE2D(_GrassTex);  SAMPLER(sampler_GrassTex);
            TEXTURE2D(_DirtTex);   SAMPLER(sampler_DirtTex);
            TEXTURE2D(_RockTex);   SAMPLER(sampler_RockTex);
            TEXTURE2D(_SnowTex);   SAMPLER(sampler_SnowTex);
            TEXTURE2D(_GrassNormal); SAMPLER(sampler_GrassNormal);
            TEXTURE2D(_DirtNormal);  SAMPLER(sampler_DirtNormal);
            TEXTURE2D(_RockNormal);  SAMPLER(sampler_RockNormal);
            TEXTURE2D(_SnowNormal);  SAMPLER(sampler_SnowNormal);

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
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ── Splat 权重 ─────────────────────────────────
                half4 splat = SAMPLE_TEXTURE2D(_SplatMap, sampler_SplatMap, input.uv);

                float2 uvG = input.uv * _GrassTiling;
                float2 uvD = input.uv * _DirtTiling;
                float2 uvR = input.uv * _RockTiling;
                float2 uvS = input.uv * _SnowTiling;

                // ── 各层采样（Albedo α 通道存高度）─────────────
                half4 sG = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uvG);
                half4 sD = SAMPLE_TEXTURE2D(_DirtTex,  sampler_DirtTex,  uvD);
                half4 sR = SAMPLE_TEXTURE2D(_RockTex,  sampler_RockTex,  uvR);
                half4 sS = SAMPLE_TEXTURE2D(_SnowTex,  sampler_SnowTex,  uvS);

                // ── 高度混合权重 ───────────────────────────────
                float4 bh = float4(splat.r + sG.a, splat.g + sD.a, splat.b + sR.a, splat.a + sS.a);
                float maxH = max(max(bh.r, bh.g), max(bh.b, bh.a));
                bh = max(bh - (maxH - _BlendSharpness), 0);
                float4 w = bh / (dot(bh, 1.0) + 0.0001);

                // ── 混合 Albedo ────────────────────────────────
                half3 albedo = sG.rgb * w.r + sD.rgb * w.g + sR.rgb * w.b + sS.rgb * w.a;

                // ── 混合法线 ───────────────────────────────────
                float3 nG = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, uvG));
                float3 nD = UnpackNormal(SAMPLE_TEXTURE2D(_DirtNormal,  sampler_DirtNormal,  uvD));
                float3 nR = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal,  sampler_RockNormal,  uvR));
                float3 nS = UnpackNormal(SAMPLE_TEXTURE2D(_SnowNormal,  sampler_SnowNormal,  uvS));
                float3 nt = normalize(nG * w.r + nD * w.g + nR * w.b + nS * w.a);

                float3 normalWS = normalize(TransformTangentToWorld(nt,
                    half3x3(input.tangentWS, input.bitangentWS, input.normalWS)));

                // ── 光照 ──────────────────────────────────────
                Light  light  = GetMainLight(input.shadowCoord);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.posWS));
                float  NdotL  = saturate(dot(normalWS, light.direction));

                half3 diffuse  = albedo * light.color * NdotL * light.shadowAttenuation;
                half3 ambient  = albedo * SampleSH(normalWS);

                return half4(diffuse + ambient, 1.0);
            }
            ENDHLSL
        }
    }
}
```

---

## 性能分析

4 层地形需要大量纹理采样：

| 采样 | 数量 | 说明 |
|------|------|------|
| Splat Map | 1 | 混合权重 |
| Albedo × 4 | 4 | 各层颜色+高度 |
| Normal × 4 | 4 | 各层法线 |
| 总计 | **9 次** | —— |

移动端分层策略：

| 层级 | 方案 | 采样数 |
|------|------|--------|
| 高档 | 4 层 + 高度混合 + 法线 | 9 |
| 中档 | 4 层 + 法线（无高度混合） | 9（但省去混合计算）|
| 低档 | 2 层 + 无法线 | 3 |
| 极低档 | 1 层固定纹理 | 1 |

可用 `shader_feature_local` 做变体控制，按目标机型选择层数。

---

## 小结

地形 Splat Map 方案 = Splat Map 存权重 + 各层独立采样 + 权重混合 Albedo 和法线。基于高度的软化混合让边界更自然，但采样次数较多，移动端需分层取舍。

下一篇：GPU 粒子 Shader——用 StructuredBuffer 传粒子数据，Shader 里读 SV_InstanceID 实现大量粒子的 GPU Instancing 渲染。
