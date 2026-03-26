---
title: "Shader 核心光照 03｜阴影接收与投射：ShadowCaster Pass 完整实现"
slug: "shader-lighting-03-shadows"
date: "2026-03-26"
description: "自定义 URP Shader 默认没有阴影。理解阴影贴图原理，写 ShadowCaster Pass 让物体投射阴影，在 ForwardLit Pass 里接收阴影，处理 Alpha Test 物体的阴影裁剪。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "光照"
  - "阴影"
  - "ShadowCaster"
  - "ShadowMap"
series: "Shader 手写技法"
weight: 4130
---
自己写的 Shader 默认没有阴影——不投射，也不接收。这篇补齐阴影的两个方向：写 ShadowCaster Pass 让物体投射阴影，在 ForwardLit Pass 里接收阴影。

---

## 阴影贴图原理（Shadow Map）

URP 的阴影是 Shadow Map 方案：

1. **深度 Pass**：从主光源的角度渲染一遍场景，只写深度，生成 Shadow Map
2. **光照 Pass**：渲染正常画面时，把当前像素投影到 Shadow Map 坐标，对比深度——如果当前像素比 Shadow Map 里记录的深度远，说明有物体挡住了光，该像素在阴影里

ShadowCaster Pass 就是第一步里被执行的 Pass——物体要参与深度渲染才能投射阴影。
接收阴影在 ForwardLit Pass 里，通过 `GetMainLight(shadowCoord)` 完成。

---

## ShadowCaster Pass

最简单的做法：直接 include URP 内置的 ShadowCaster Pass 文件：

```hlsl
Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }

    ZWrite On
    ZTest LEqual
    ColorMask 0   // 只写深度，不写颜色
    Cull Back

    HLSLPROGRAM
    #pragma vertex   ShadowPassVertex
    #pragma fragment ShadowPassFragment

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
    ENDHLSL
}
```

这个内置 Pass 已经处理了深度偏移（Shadow Bias）——防止 Shadow Acne（自阴影锯齿）。直接用即可，不需要自己写顶点/片元。

---

## ForwardLit Pass 里接收阴影

**第一步：声明关键字**

```hlsl
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _ _SHADOWS_SOFT
```

- `_MAIN_LIGHT_SHADOWS`：启用主光阴影
- `_MAIN_LIGHT_SHADOWS_CASCADE`：启用级联阴影（CSM，多层级，远近都清晰）
- `_SHADOWS_SOFT`：软阴影（PCF 采样）

**第二步：顶点阶段计算 Shadow Coord**

```hlsl
// 需要 include
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// Varyings 里加：
float4 shadowCoord : TEXCOORD_N;

// Vertex Shader 里：
VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
output.positionHCS = posInputs.positionCS;
output.positionWS  = posInputs.positionWS;

// 计算阴影坐标（投影到 Shadow Map 的 UV）
output.shadowCoord = GetShadowCoord(posInputs);
```

**第三步：Fragment Shader 里传入阴影坐标**

```hlsl
Light mainLight = GetMainLight(input.shadowCoord);
// mainLight.shadowAttenuation 就是阴影衰减系数：1=不在阴影里，0=完全在阴影里
```

把阴影衰减乘进漫反射：

```hlsl
half NdotL   = saturate(dot(normalWS, mainLight.direction));
half atten   = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
half3 diffuse = albedo.rgb * mainLight.color * NdotL * atten;
```

---

## 完整双 Pass Shader（含阴影）

```hlsl
Shader "Custom/LitWithShadows"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap   ("Base Map",   2D)    = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // ── Pass 1：阴影投射 ──────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ── Pass 2：正向光照（接收阴影）─────────────────────────
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
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float2 uv          : TEXCOORD3;
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
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // 获取主光（传入 shadowCoord，自动计算阴影衰减）
                Light mainLight = GetMainLight(input.shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                // 阴影衰减 × 距离衰减（点光有距离衰减，方向光为 1）
                half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                half4 albedo  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 diffuse = albedo.rgb * mainLight.color * NdotL * atten;

                return half4(diffuse, albedo.a);
            }
            ENDHLSL
        }
    }
}
```

---

## Alpha Test 物体的阴影

植被、镂空遮罩等使用 `clip` 的物体，ShadowCaster Pass 里也必须做同样的裁剪——否则 Shadow Map 里有完整轮廓，实际渲染时镂空，阴影会"穿帮"。

这时不能直接用内置 `ShadowCasterPass.hlsl`，需要手动写：

```hlsl
Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }
    ZWrite On
    ZTest LEqual
    ColorMask 0

    HLSLPROGRAM
    #pragma vertex   vert_shadow
    #pragma fragment frag_shadow

    #pragma shader_feature_local_fragment _ALPHATEST_ON

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float  _Cutoff;
    CBUFFER_END

    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);

    struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0;
                        float3 normalOS : NORMAL; float4 tangentOS : TANGENT; };
    struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

    Varyings vert_shadow(Attributes input)
    {
        Varyings output;
        // ApplyShadowBias 处理深度偏移（防止自阴影锯齿）
        float3 posWS    = TransformObjectToWorld(input.positionOS.xyz);
        float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
        float4 posCS    = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));

        // Clamp Depth 防止超出近裁面
        #if UNITY_REVERSED_Z
            posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
        #else
            posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
        #endif

        output.positionHCS = posCS;
        output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
        return output;
    }

    half4 frag_shadow(Varyings input) : SV_Target
    {
        #ifdef _ALPHATEST_ON
            half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
            clip(alpha - _Cutoff);   // 和 ForwardLit Pass 保持相同裁剪
        #endif
        return 0;
    }
    ENDHLSL
}
```

---

## DepthOnly Pass

部分渲染效果（深度预过、SSAO、景深）需要 DepthOnly Pass。和 ShadowCaster 类似，可以直接 include 内置实现：

```hlsl
Pass
{
    Name "DepthOnly"
    Tags { "LightMode" = "DepthOnly" }

    ZWrite On
    ColorMask R   // 只写 R 通道（深度）
    Cull Back

    HLSLPROGRAM
    #pragma vertex   DepthOnlyVertex
    #pragma fragment DepthOnlyFragment

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
    ENDHLSL
}
```

---

## 常见问题

**Q：物体不投射阴影**

没有 ShadowCaster Pass，或者 `LightMode` Tag 写错了（必须是 `"ShadowCaster"` 完整字符串）。

**Q：物体不接收阴影（全亮，没有阴影区域）**

`#pragma multi_compile _ _MAIN_LIGHT_SHADOWS` 没加，或者 `GetMainLight()` 没有传入 `shadowCoord`。

**Q：阴影有锯齿（Shadow Acne）**

Shadow Bias 不够。URP Asset 里调整 `Depth Bias` 和 `Normal Bias`，或者检查自定义 ShadowCaster Pass 里有没有调用 `ApplyShadowBias`。

**Q：级联阴影不工作**

URP Asset 里需要开启 Cascade，并且 `#pragma multi_compile` 里要包含 `_MAIN_LIGHT_SHADOWS_CASCADE`。

---

## 小结

| 概念 | 要点 |
|------|------|
| ShadowCaster Pass | `LightMode = ShadowCaster`，简单物体直接 include 内置实现 |
| 接收阴影 | `GetShadowCoord(posInputs)` 在顶点算，`GetMainLight(shadowCoord)` 在片元拿衰减 |
| `shadowAttenuation` | 0=阴影里，1=光照里，需乘入最终光照 |
| Alpha Test 阴影 | ShadowCaster Pass 里需要和 ForwardLit 做同样的 `clip` |
| 软阴影关键字 | `_SHADOWS_SOFT`，需在 `#pragma multi_compile_fragment` 里声明 |

下一篇：附加光源——场景里有多个点光和聚光时，如何在 Shader 里遍历处理。
