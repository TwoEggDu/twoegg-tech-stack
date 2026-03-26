---
title: "Shader 进阶技法 04｜屏幕空间反射：SSR 的原理与简化实现"
slug: "shader-advanced-04-ssr"
date: "2026-03-26"
description: "反射探针是静态快照，无法反射动态物体。SSR（屏幕空间反射）在屏幕内追踪反射光线，实现动态精确反射。理解线性步进（Linear March）原理，以及 SSR 的边界问题和适用场景。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "进阶"
  - "SSR"
  - "屏幕空间反射"
  - "光线步进"
series: "Shader 手写技法"
weight: 4320
---
反射探针（Reflection Probe）烘焙的是静态场景，无法反射动态移动的物体。SSR（Screen Space Reflection）直接在屏幕空间里追踪反射光线，反射结果来自当前帧的渲染结果——动态、精确，但有固有的边界限制。

---

## SSR 的核心思路

```
1. 当前像素的世界法线 + 视线方向 → 计算反射方向 R
2. 从当前像素位置，沿 R 方向在屏幕空间步进
3. 每步检查步进点的深度 vs 场景深度缓冲
4. 当步进深度 < 场景深度时：找到了被反射的表面
5. 采样该屏幕位置的颜色，作为反射颜色
```

所有工作都在**屏幕空间**完成——不需要额外的几何信息，只用已有的深度图和颜色图。

---

## 需要的数据

SSR 需要三张纹理：

| 纹理 | 来源 | 用途 |
|------|------|------|
| `_CameraDepthTexture` | URP 深度 Pass | 场景深度比较 |
| `_CameraOpaqueTexture` | URP Opaque Texture | 反射颜色来源 |
| `_CameraGBufferTexture2` / 法线图 | DepthNormals PrePass | 每像素法线 |

开启方式：
- Depth Texture：URP Asset → Depth Texture
- Opaque Texture：URP Asset → Opaque Texture
- 法线图：URP Asset → Depth Normal Prepass（或自定义 Feature）

---

## 核心算法：线性步进

```hlsl
float3 SSR(float3 posWS, float3 normalWS, float3 viewDir,
           int stepCount, float stepSize, float thickness)
{
    // 计算反射方向
    float3 reflectDir = reflect(-viewDir, normalWS);

    // 当前像素的屏幕坐标（步进起点）
    float4 startCS    = TransformWorldToHClip(posWS);
    float2 startSS    = startCS.xy / startCS.w * 0.5 + 0.5;

    float2 currentSS  = startSS;
    float3 currentWS  = posWS;

    for (int i = 1; i <= stepCount; i++)
    {
        // 沿反射方向步进
        currentWS += reflectDir * stepSize;

        // 把步进后的世界坐标投影到屏幕
        float4 stepCS = TransformWorldToHClip(currentWS);
        float2 stepSS = stepCS.xy / stepCS.w * 0.5 + 0.5;

        // 检查是否超出屏幕范围
        if (any(stepSS < 0) || any(stepSS > 1)) break;

        // 采样场景深度，转为线性深度
        float rawDepth   = SAMPLE_TEXTURE2D_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, stepSS, 0).r;
        float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

        // 步进点的线性深度
        float stepDepth  = -mul(UNITY_MATRIX_V, float4(currentWS, 1.0)).z;

        // 深度差比较：步进深度 > 场景深度（步进点在场景表面后面）
        float depthDiff = stepDepth - sceneDepth;
        if (depthDiff > 0 && depthDiff < thickness)
        {
            // 找到交叉点，返回该屏幕位置的颜色
            return SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, stepSS, 0).rgb;
        }
    }

    // 没有找到交叉，返回回退颜色（天空盒/反射探针）
    return 0;
}
```

---

## 完整 SSR 材质 Shader（简化版）

```hlsl
Shader "Custom/SSRSurface"
{
    Properties
    {
        _BaseColor    ("Base Color",    Color)      = (0.8, 0.8, 0.9, 1)
        _Smoothness   ("Smoothness",    Range(0,1)) = 0.9
        _SSRStrength  ("SSR Strength",  Range(0,1)) = 1.0
        _SSRStepCount ("SSR Steps",     Float)      = 16
        _SSRStepSize  ("SSR Step Size", Float)      = 0.3
        _SSRThickness ("SSR Thickness", Float)      = 0.5
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Smoothness;
                float  _SSRStrength;
                float  _SSRStepCount;
                float  _SSRStepSize;
                float  _SSRThickness;
            CBUFFER_END

            struct Attributes { float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 hcs:SV_POSITION; float3 normalWS:TEXCOORD0;
                                float3 posWS:TEXCOORD1; float4 shadowCoord:TEXCOORD2; };

            Varyings vert(Attributes i) {
                Varyings o;
                VertexPositionInputs pi = GetVertexPositionInputs(i.pos.xyz);
                o.hcs = pi.positionCS; o.posWS = pi.positionWS;
                o.shadowCoord = GetShadowCoord(pi);
                o.normalWS    = TransformObjectToWorldNormal(i.n);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir  = normalize(GetWorldSpaceViewDir(input.posWS));
                float3 reflDir  = reflect(-viewDir, normalWS);

                // ── 直接光照 ──────────────────────────────────────
                Light  light  = GetMainLight(input.shadowCoord);
                half   NdotL  = saturate(dot(normalWS, light.direction));
                half3  direct = _BaseColor.rgb * light.color * NdotL * light.shadowAttenuation;

                // ── SSR 步进 ──────────────────────────────────────
                half3 ssrColor = 0;
                float3 curWS   = input.posWS + normalWS * 0.05;  // 稍微偏移，避免自交

                int steps = (int)_SSRStepCount;
                [loop]
                for (int i = 1; i <= steps; i++)
                {
                    curWS += reflDir * _SSRStepSize;

                    float4 stepCS = TransformWorldToHClip(curWS);
                    if (stepCS.w <= 0) break;
                    float2 stepSS = stepCS.xy / stepCS.w * 0.5 + 0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                        stepSS.y = 1.0 - stepSS.y;
                    #endif

                    if (any(stepSS < 0.001) || any(stepSS > 0.999)) break;

                    float rawD    = SampleSceneDepth(stepSS);
                    float sceneD  = LinearEyeDepth(rawD, _ZBufferParams);
                    float stepD   = -mul(UNITY_MATRIX_V, float4(curWS, 1.0)).z;

                    float diff = stepD - sceneD;
                    if (diff > 0 && diff < _SSRThickness)
                    {
                        ssrColor = SampleSceneColor(stepSS);
                        break;
                    }
                }

                // ── Fresnel 混合 SSR ──────────────────────────────
                float NdotV   = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, 5.0) * _Smoothness;
                half3 reflect = lerp(direct, direct + ssrColor * _SSRStrength, fresnel);

                return half4(reflect, 1.0);
            }
            ENDHLSL
        }
    }
}
```

---

## SSR 的固有局限

| 问题 | 原因 | 缓解方案 |
|------|------|---------|
| 屏幕边缘消失 | 反射方向超出屏幕范围 | 边缘 fade（用 UV 到边界距离做 lerp，回退到反射探针） |
| 背向摄像机的表面无法反射 | 屏幕空间只有正面数据 | 接受这个限制，或 Fallback 到探针 |
| 步进步长决定精度 | 步长大：跳过薄物体；步长小：性能差 | 层次步进（先大步粗搜，再小步精确） |
| 移动物体反射延迟一帧 | Opaque Texture 在当前帧前已确定 | 时间性 AA（TAA）平滑历史帧 |

---

## 性能

SSR 是昂贵的效果，每个反射像素需要 N 次深度图采样（N = 步进次数）。

| 平台 | 建议步进次数 | 备注 |
|------|------------|------|
| PC/主机 | 32~64 | 高质量，可开启 TAA |
| 移动高档 | 8~16 | 仅用于水面、地板等关键位置 |
| 移动中低档 | 关闭，Fallback 反射探针 | — |

---

## 小结

| 概念 | 要点 |
|------|------|
| SSR 原理 | 屏幕空间光线步进，深度比较找反射点 |
| 需要 | Depth Texture + Opaque Texture |
| 步进方向 | `reflect(-viewDir, normalWS)` 世界空间反射向量 |
| 边界处理 | 超出屏幕时 break + Fallback 到反射探针 |
| 性能 | 步进次数 × 像素数，移动端慎用 |

下一篇：皮肤次表面散射（SSS）——光线穿透皮肤薄层散射的模拟，让角色皮肤有透光感。
