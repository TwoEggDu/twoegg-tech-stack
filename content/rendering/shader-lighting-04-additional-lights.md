---
title: "Shader 核心光照 04｜附加光源：点光与聚光的多光源循环"
slug: "shader-lighting-04-additional-lights"
date: "2026-03-26"
description: "场景里有多个点光和聚光时，URP 的 ForwardLit 需要遍历附加光源。理解 GetAdditionalLightsCount / GetAdditionalLight 的用法，以及移动端限制附加光数量的策略。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "光照"
  - "点光"
  - "附加光源"
  - "多光源"
series: "Shader 手写技法"
weight: 4140
---
前几篇只处理了主方向光（`GetMainLight`）。场景里还有点光（Point Light）、聚光（Spot Light），它们是附加光源（Additional Lights）。这篇讲如何在 Shader 里遍历处理它们。

---

## URP 的多光源模型

URP 采用 **Forward Rendering**，每个物体的每个像素都要遍历影响它的所有光源。这和 Deferred Rendering 不同——Forward 的光照计算都在物体的 Shader 里完成。

URP 把光源分成两类：

| 类型 | 获取方式 | 说明 |
|------|---------|------|
| 主光（Main Light） | `GetMainLight()` | 通常是方向光，最多 1 个 |
| 附加光（Additional Lights） | `GetAdditionalLight(i, posWS)` | 点光、聚光，有上限 |

附加光的上限在 URP Asset 里配置（`Additional Lights → Per Object Limit`），移动端默认 4 个，PC 端默认 8 个。

---

## 必须声明的关键字

附加光有两种处理模式，通过关键字区分：

```hlsl
// 逐顶点附加光（Per-Vertex，轻量，精度较低）
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX

// 逐像素附加光（Per-Pixel，精确，开销更高）
#pragma multi_compile _ _ADDITIONAL_LIGHTS

// 附加光软阴影
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
```

URP Asset 里的 `Additional Lights → Per Object → Per Pixel / Per Vertex` 控制哪个关键字被启用。

---

## Fragment Shader 里的附加光循环

```hlsl
// 在 ForwardLit Fragment Shader 里，主光之后加：

half3 additionalLightColor = 0;

uint lightCount = GetAdditionalLightsCount();
for (uint i = 0; i < lightCount; i++)
{
    // positionWS：当前像素的世界空间位置
    Light light = GetAdditionalLight(i, input.positionWS);

    half NdotL = saturate(dot(normalWS, light.direction));
    half atten = light.distanceAttenuation * light.shadowAttenuation;

    additionalLightColor += albedo.rgb * light.color * NdotL * atten;
}

half3 finalColor = diffuse + additionalLightColor;
```

`GetAdditionalLight(i, positionWS)` 自动处理了：
- 点光的距离衰减（inverse square falloff）
- 聚光的锥形角度衰减
- 阴影衰减（如果开启了附加光阴影）

---

## 附加光阴影

附加光阴影开销更高，通常移动端不开。如果要支持：

```hlsl
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

// 循环里：
Light light = GetAdditionalLight(i, input.positionWS, shadowMask);
```

其中 `shadowMask` 是烘焙阴影遮罩，需要额外的 Varyings 传递。实际项目中附加光阴影通常只在 PC/主机上启用。

---

## 完整 ForwardLit Pass（主光 + 附加光）

```hlsl
Pass
{
    Name "ForwardLit"
    Tags { "LightMode" = "UniversalForward" }

    HLSLPROGRAM
    #pragma vertex   vert
    #pragma fragment frag

    // 主光阴影
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
    #pragma multi_compile_fragment _ _SHADOWS_SOFT

    // 附加光
    #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
    #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _BaseMap_ST;
    CBUFFER_END

    TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS   : NORMAL;
        float2 uv         : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionHCS   : SV_POSITION;
        float3 normalWS      : TEXCOORD0;
        float3 positionWS    : TEXCOORD1;
        float4 shadowCoord   : TEXCOORD2;
        float2 uv            : TEXCOORD3;
        #ifdef _ADDITIONAL_LIGHTS_VERTEX
        half3  vertexLighting : TEXCOORD4;   // 逐顶点附加光预计算结果
        #endif
    };

    Varyings vert(Attributes input)
    {
        Varyings output;
        VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
        output.positionHCS = posInputs.positionCS;
        output.positionWS  = posInputs.positionWS;
        output.shadowCoord = GetShadowCoord(posInputs);
        output.normalWS    = TransformObjectToWorldNormal(input.normalOS);
        output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);

        // 逐顶点附加光（_ADDITIONAL_LIGHTS_VERTEX 启用时在这里计算）
        #ifdef _ADDITIONAL_LIGHTS_VERTEX
        output.vertexLighting = VertexLighting(posInputs.positionWS, output.normalWS);
        #endif

        return output;
    }

    half4 frag(Varyings input) : SV_Target
    {
        float3 normalWS = normalize(input.normalWS);
        half4  albedo   = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

        // ── 主光 ─────────────────────────────────────────────────
        Light mainLight = GetMainLight(input.shadowCoord);
        half  NdotL     = saturate(dot(normalWS, mainLight.direction));
        half  mainAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
        half3 color     = albedo.rgb * mainLight.color * NdotL * mainAtten;

        // ── 附加光（逐像素）────────────────────────────────────
        #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0; i < lightCount; i++)
        {
            Light light   = GetAdditionalLight(i, input.positionWS);
            half  addNdotL = saturate(dot(normalWS, light.direction));
            half  addAtten = light.distanceAttenuation * light.shadowAttenuation;
            color += albedo.rgb * light.color * addNdotL * addAtten;
        }
        #endif

        // ── 附加光（逐顶点，精度较低但更快）────────────────────
        #ifdef _ADDITIONAL_LIGHTS_VERTEX
        color += albedo.rgb * input.vertexLighting;
        #endif

        return half4(color, albedo.a);
    }
    ENDHLSL
}
```

---

## 移动端附加光策略

| 设置 | 适合场景 |
|------|---------|
| Additional Lights = Off | 最省，没有附加光（全烘焙） |
| Per Object Limit = 1~2 | 移动端低中档，保留关键点光 |
| Per Vertex | 中档，牺牲精度换性能 |
| Per Pixel, Limit = 4 | 移动端高档 |
| Per Pixel, Limit = 8+ | PC / 主机 |

移动端典型方案：主光用 Per Pixel，附加光用 Per Vertex 或限制到 2 个。大量静态光照用烘焙（Lightmap + Light Probe），不走实时附加光路径。

---

## `VertexLighting` 函数

逐顶点附加光的内置封装，在 Vertex Shader 里调用：

```hlsl
// 在 Vertex Shader 里预计算所有附加光的漫反射贡献
half3 vertexLight = VertexLighting(positionWS, normalWS);

// 在 Fragment Shader 里直接乘入 albedo
color += albedo.rgb * input.vertexLight;
```

`VertexLighting` 内部循环所有附加光，计算 Lambert 漫反射，不含高光。

---

## 常见问题

**Q：附加光没效果**

检查 URP Asset 里 `Additional Lights` 是否开启，以及 Shader 里对应的 `#pragma multi_compile` 是否有 `_ADDITIONAL_LIGHTS`。

**Q：点光范围之外还有光**

点光的衰减曲线由 URP Asset 的 `Light Falloff` 控制。`GetAdditionalLight` 已经处理了距离衰减，`light.distanceAttenuation` 超出范围后为 0，不应该有光——检查是否有环境光没有单独处理。

**Q：想限制某个物体只受主光影响**

在 Material 的 `Additional Lights Limit` 属性设为 0，或者在 Shader 里条件编译掉附加光循环。

---

## 小结

| 概念 | 要点 |
|------|------|
| 附加光 API | `GetAdditionalLightsCount()` + `GetAdditionalLight(i, posWS)` |
| 距离衰减 | `light.distanceAttenuation`，已自动计算，直接乘入 |
| 关键字 | `_ADDITIONAL_LIGHTS`（逐像素）/ `_ADDITIONAL_LIGHTS_VERTEX`（逐顶点） |
| 移动端策略 | 限制上限，低档用逐顶点或关闭，大量静态光用烘焙 |
| `VertexLighting` | 顶点阶段预计算附加光漫反射，Fragment 直接用 |

下一篇：PBR 基础——金属度/粗糙度工作流，Cook-Torrance BRDF，URP 的 `UniversalFragmentPBR` 接口。
