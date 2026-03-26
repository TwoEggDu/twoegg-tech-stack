+++
title = "Shader 核心技法 02｜溶解效果：噪声 clip 与发光边缘"
slug = "shader-technique-02-dissolve"
date = 2026-03-26
description = "溶解（Dissolve）是游戏里最常见的特效之一。用噪声贴图 + clip 实现基础溶解，再加上 smoothstep 做出带发光边缘的高质量溶解效果。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "技法", "溶解", "特效", "clip"]
series = ["Shader 手写技法"]
[extra]
weight = 4180
+++

角色死亡消散、物体被火焰燃烧、传送效果——溶解（Dissolve）是游戏里出现频率极高的特效。核心原理极简：用噪声贴图的值和一个阈值比较，低于阈值的像素丢弃。

---

## 基础溶解

```hlsl
// 采样噪声贴图（0~1 的随机值）
half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * _NoiseScale).r;

// noise < threshold 时 clip（丢弃像素）
// clip(x)：x < 0 时 discard
clip(noise - _Threshold);

// 正常渲染剩余像素
return _BaseColor;
```

`_Threshold` 从 0 推进到 1：
- `0`：全部显示（噪声值都 > 0，clip 不触发）
- `0.5`：约一半像素消失
- `1`：全部消失（所有噪声值都 < 1，全部被 clip）

通过动画控制 `_Threshold` 就能驱动溶解过程。代码控制：

```csharp
material.SetFloat("_Threshold", dissolveProgress);  // 0→1
```

---

## 带发光边缘

溶解边缘加上发光，质量立刻提升一个档次。原理：在 clip 边界的附近一段距离内，输出发光颜色。

```hlsl
half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * _NoiseScale).r;

// 溶解边缘：noise 在 [_Threshold, _Threshold + _EdgeWidth] 范围内
float edge = smoothstep(_Threshold, _Threshold + _EdgeWidth, noise);
// edge = 0：在发光区域（接近 clip 边界）
// edge = 1：正常显示区域

// 丢弃已溶解的像素
clip(noise - _Threshold);

// 发光颜色与基础颜色混合
half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
half3 edgeColor = _EdgeColor.rgb * _EdgeIntensity;  // HDR 值（>1 触发 Bloom）

half3 finalColor = lerp(edgeColor, baseColor, edge);
return half4(finalColor, 1.0);
```

`_EdgeWidth` 控制发光边缘宽度（0.02~0.1 之间合适），`_EdgeIntensity` 控制亮度（设为 2~5 配合 Bloom 效果更佳）。

---

## 完整 Shader

```hlsl
Shader "Custom/Dissolve"
{
    Properties
    {
        _BaseColor     ("Base Color",     Color)  = (1, 1, 1, 1)
        _BaseMap       ("Base Map",       2D)     = "white" {}
        _NoiseTex      ("Noise Texture",  2D)     = "gray"  {}
        _NoiseScale    ("Noise Scale",    Float)  = 1.0
        _Threshold     ("Threshold",      Range(0, 1)) = 0.0
        [HDR] _EdgeColor ("Edge Color",  Color)  = (1, 0.5, 0, 1)
        _EdgeWidth     ("Edge Width",     Float)  = 0.05
        _EdgeIntensity ("Edge Intensity", Float)  = 3.0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull Off   // 溶解物体通常双面

            HLSLPROGRAM
            #pragma vertex   vert_shadow
            #pragma fragment frag_shadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTex_ST;
                float  _NoiseScale;
                float  _Threshold;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attr { float4 pos : POSITION; float2 uv : TEXCOORD0;
                          float3 n : NORMAL; float4 t : TANGENT; };
            struct Vary { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Vary vert_shadow(Attr i) {
                Vary o;
                float3 posWS = TransformObjectToWorld(i.pos.xyz);
                float3 nWS   = TransformObjectToWorldNormal(i.n);
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, _MainLightPosition.xyz));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                o.pos = posCS;
                o.uv  = i.uv;
                return o;
            }
            half4 frag_shadow(Vary i) : SV_Target {
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv * _NoiseScale).r;
                clip(noise - _Threshold);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

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
                float4 _NoiseTex_ST;
                float  _NoiseScale;
                float  _Threshold;
                float4 _EdgeColor;
                float  _EdgeWidth;
                float  _EdgeIntensity;
            CBUFFER_END

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0;
                                float3 positionWS:TEXCOORD1; float4 shadowCoord:TEXCOORD2; float2 uv:TEXCOORD3; };

            Varyings vert(Attributes input) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionHCS = pi.positionCS;
                o.positionWS  = pi.positionWS;
                o.shadowCoord = GetShadowCoord(pi);
                o.normalWS    = TransformObjectToWorldNormal(input.normalOS);
                o.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 噪声采样
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * _NoiseScale).r;

                // 丢弃溶解区域
                clip(noise - _Threshold);

                // 边缘遮罩：0 = 发光区，1 = 正常区
                float edge = smoothstep(_Threshold, _Threshold + _EdgeWidth, noise);

                // 光照
                float3 normalWS = normalize(input.normalWS);
                Light  mainLight = GetMainLight(input.shadowCoord);
                half   NdotL     = saturate(dot(normalWS, mainLight.direction));
                half   atten     = mainLight.shadowAttenuation;

                half3 albedo    = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half3 diffuse   = albedo * mainLight.color * NdotL * atten;
                half3 edgeColor = _EdgeColor.rgb * _EdgeIntensity;

                // 边缘区域用发光色替换
                half3 finalColor = lerp(edgeColor, diffuse, edge);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
```

---

## 变体：方向性溶解

不用噪声，而是用世界空间 Y 坐标做溶解（从下往上消散）：

```hlsl
// 用世界坐标 Y 做溶解轴
float dissolveValue = (input.positionWS.y - _DissolveMin) / (_DissolveMax - _DissolveMin);
// 加噪声扰动边缘，避免直线
half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv).r * _NoisePower;
clip(dissolveValue + noise - _Threshold);
```

---

## 性能注意

- `clip` 在 TBDR GPU（移动端）上会破坏 HSR——溶解的 Alpha Test 物体**每个像素都必须执行完 Fragment Shader** 才能丢弃，丢失了 Early-Z 优势
- 溶解物体尽量放在 `AlphaTest` 队列，不要放 `Transparent`（透明队列更昂贵）
- 大面积溶解特效考虑粒子系统代替，而不是大网格 clip

---

## 小结

| 概念 | 要点 |
|------|------|
| 基础溶解 | `clip(noise - threshold)`，noise 来自噪声贴图 |
| 发光边缘 | `smoothstep(threshold, threshold+width, noise)` 做边缘遮罩 |
| HDR 发光色 | `_EdgeColor` 用 HDR 属性（`[HDR]`），配合 Bloom 后处理 |
| ShadowCaster | 必须同步 clip，否则阴影穿帮 |
| 移动端代价 | clip 破坏 HSR，大面积慎用 |

下一篇：卡通渲染——色阶漫反射、Rim Light、外描边，实现 Cel Shading 风格。
