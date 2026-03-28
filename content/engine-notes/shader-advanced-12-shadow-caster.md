---
title: "Shader 进阶技法 12｜自定义 Shadow Caster Pass：半透明物体的阴影投射"
slug: "shader-advanced-12-shadow-caster"
date: "2026-03-28"
description: "深入 URP 阴影管线，手写带 Alpha Clip 的 ShadowCaster Pass，让镂空/半透明物体正确投射阴影，理解 ApplyShadowBias 的作用与 shadow acne 的成因。"
tags: ["Shader", "HLSL", "URP", "进阶", "阴影", "ShadowCaster"]
series: "Shader 手写技法"
weight: 4400
---

默认情况下，把一个材质的 Rendering Mode 改成 Transparent，阴影就消失了。这不是 Unity 的 bug，而是 URP 阴影管线的设计取舍——ShadowCaster Pass 只写深度，不涉及 alpha，所以带镂空图案的树叶、铁栅栏、带透明边缘的贴花，默认都会以完整几何形状投射矩形阴影，或者完全不投射。要让这些物体投射出符合外轮廓形状的阴影，就需要手写 ShadowCaster Pass。

---

## URP 阴影的工作原理

URP 在渲染阴影贴图（Shadow Map）时，对场景中所有开启了阴影投射的 Renderer 执行 `LightMode = ShadowCaster` 的 Pass。这个 Pass 的任务极其单一：把顶点变换到光源视角的 Clip Space，输出深度。

标准的 ShadowCaster Pass 不采样任何贴图，也不关心颜色。这对不透明物体完全没问题——实心的几何体只需要深度。但一旦物体有镂空（alpha 小于阈值的区域），这些区域在几何上依然存在，ShadowCaster 还是会把它们写入深度图，阴影轮廓就错了。

解决方案：在自定义 ShadowCaster Pass 里，采样贴图获取 alpha 值，对低于阈值的像素调用 `clip()` 丢弃，让这些像素不写入深度图。

---

## ShadowCaster Pass 的必要构成

一个合法的 URP ShadowCaster Pass 需要：

1. `Tags { "LightMode" = "ShadowCaster" }` — URP 只执行这个 LightMode 的 Pass 来生成阴影。
2. `ZWrite On` — 必须写深度，这是 Shadow Map 的核心。
3. `ColorMask 0` — 阴影 Pass 不需要写颜色缓冲，关掉节省带宽。
4. `#pragma multi_compile_shadowcaster` 或 `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS` — 让编译器生成正确的阴影变体。
5. 调用 `ApplyShadowBias()` 修正 shadow acne。

---

## Shadow Acne 与 ApplyShadowBias

Shadow Acne（阴影粉刺）是阴影技术里最常见的视觉问题：物体表面出现条纹状的自阴影噪声。原因是：光源视角的深度图分辨率有限，同一个深度图像素可能覆盖多个世界空间像素；采样时浮点精度误差导致物体表面自己遮挡自己。

解决方法是在写入深度图之前，把顶点沿光源方向稍微偏移一点（depth bias）或沿法线方向偏移（normal bias），让表面离光源更"近"一些，避免自遮挡。

URP 提供了 `ApplyShadowBias(positionWS, normalWS, lightDirection)` 函数来完成这个偏移，其中 bias 的量由 Light 组件上的 Bias 和 Normal Bias 参数控制。

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// 在 Vertex Shader 里调用
float3 positionWS = TransformObjectToWorld(positionOS);
Light mainLight = GetMainLight();
positionWS = ApplyShadowBias(positionWS, normalWS, mainLight.direction);
float4 positionCS = TransformWorldToHClip(positionWS);

// 处理 reversed Z（部分平台深度范围是 [1,0] 而非 [0,1]）
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
```

---

## 完整代码：带 Alpha Clip 的 ShadowCaster Pass

下面是一个完整的 URP Shader，包含主体渲染 Pass 和支持 alpha clip 的 ShadowCaster Pass：

```hlsl
Shader "Custom/AlphaClipWithShadow"
{
    Properties
    {
        _MainTex  ("Texture", 2D)           = "white" {}
        _BaseColor("Base Color", Color)     = (1, 1, 1, 1)
        _Cutoff   ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        // AlphaTest Queue：在不透明物体之后、透明物体之前渲染
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest"
               "RenderPipeline" = "UniversalPipeline" }

        // ── Pass 1：主体渲染 ──────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off  // 双面渲染，树叶/栅栏通常需要

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
                float4 shadowCoord  : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS   = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS     = TransformWorldToHClip(positionWS);
                OUT.normalWS        = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv              = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.shadowCoord     = TransformWorldToShadowCoord(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col      = texColor * _BaseColor;

                // Alpha Clip：低于阈值的像素直接丢弃
                clip(col.a - _Cutoff);

                Light mainLight = GetMainLight(IN.shadowCoord);
                float ndotl = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                float shadow = mainLight.shadowAttenuation;

                col.rgb *= mainLight.color * (ndotl * shadow) + 0.15;
                return col;
            }
            ENDHLSL
        }

        // ── Pass 2：Shadow Caster（核心） ────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0  // 不写颜色，只写深度
            Cull Off      // 双面，与主体 Pass 保持一致

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float  _Cutoff;
            CBUFFER_END

            // URP 提供的阴影 bias 辅助变量
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 GetShadowPositionHClip(Attributes IN)
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                // 应用 Shadow Bias，消除 shadow acne
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                positionWS = ApplyShadowBias(positionWS, normalWS, lightDir);

                float4 posCS = TransformWorldToHClip(positionWS);

                // Reversed Z 平台修正
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return posCS;
            }

            Varyings shadowVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = GetShadowPositionHClip(IN);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 shadowFrag(Varyings IN) : SV_Target
            {
                // 采样贴图获取 alpha，低于阈值则丢弃该像素
                // 丢弃后该像素不写入 Shadow Map，阴影轮廓就正确了
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(texColor.a * _BaseColor.a - _Cutoff);

                return 0; // ColorMask 0，这个返回值不会被写入
            }
            ENDHLSL
        }
    }
}
```

---

## AlphaTest vs Transparent 的正确工作流

| 属性 | AlphaTest（推荐用于投影） | Transparent |
|---|---|---|
| Queue | AlphaTest (2450) | Transparent (3000) |
| RenderType | TransparentCutout | Transparent |
| ZWrite | On | Off |
| 阴影支持 | 可以（本文方案） | 需要额外处理 |
| 性能 | 高（Early-Z 友好） | 低（须排序，不能 Early-Z） |

对于树叶、栅栏、头发等镂空物体，应当始终使用 AlphaTest 而非 Transparent。AlphaTest 有确定的深度，可以正确写入 Shadow Map，也支持 Early-Z 剔除。Transparent 物体由于关闭了 ZWrite，真正做到半透明的同时失去了深度，阴影只能通过特殊技巧（如自定义 Renderer Feature）来实现，代价远高于 AlphaTest。

---

## 常见问题排查

如果自定义 ShadowCaster Pass 后阴影仍然不对，检查以下几点：

- Mesh Renderer 组件上的 **Cast Shadows** 是否设置为 On（不是 Off 也不是 Shadows Only）。
- Light 组件的 **Shadow Type** 是否启用，以及 Shadow Resolution 是否足够高。
- URP Asset 的 **Main Light Shadow** 是否已在 Pipeline Asset 中开启。
- `_Cutoff` 的值与主体 Pass 里 `clip()` 的阈值是否一致，不一致会导致阴影轮廓和视觉轮廓不匹配。
