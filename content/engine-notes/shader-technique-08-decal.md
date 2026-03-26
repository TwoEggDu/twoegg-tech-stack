+++
title = "Shader 核心技法 08｜Decal 贴花：把贴图投影到任意表面"
slug = "shader-technique-08-decal"
date = 2026-03-26
description = "弹孔、血迹、泥土污迹、符文印记——Decal 把贴图投影到任意表面，不需要修改底层模型的 UV。理解 URP 的 Decal Renderer Feature，以及用深度重建世界坐标的手写方案。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "技法", "Decal", "贴花", "投影"]
series = ["Shader 手写技法"]
[extra]
weight = 4240
+++

弹孔、血迹、地面印记——这类效果不适合烘焙进底层贴图（动态生成），也不适合每次都修改模型 UV。Decal（贴花）把贴图从外部投影到任意表面，是这类动态细节的标准方案。

---

## URP 内置 Decal 系统

URP 14（Unity 2022.3）内置了 Decal Renderer Feature，这是首选方案。

**开启方式**：
1. URP Renderer Asset → Add Renderer Feature → `Decal`
2. 使用内置 `Shader Graphs/Decal` Shader 或 `Universal Render Pipeline/Decal` 材质
3. 在场景里放 `Decal Projector` 组件，设置投影范围和贴图

内置系统自动处理了：深度重建、法线混合、Normal Map 支持、性能剔除。

---

## 手写 Decal 原理（理解用）

理解内置系统如何工作，才能在需要时做定制。核心步骤：

**1. 用 Decal 体积（通常是 Box）覆盖目标区域**

Decal 渲染的是一个 Box Mesh，Box 的 UV 空间（0~1）就是投影空间。

**2. 从深度图重建世界坐标**

Decal 的 Fragment Shader 里需要知道当前像素对应的世界坐标，才能知道在哪个位置投影：

```hlsl
// 深度图采样
float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;

// 从屏幕 UV + 深度重建世界坐标
float4 clipPos = float4(screenUV * 2.0 - 1.0, depth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
    clipPos.y = -clipPos.y;
#endif
float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
worldPos.xyz /= worldPos.w;
```

**3. 把世界坐标变换到 Decal 的本地空间**

```hlsl
// 把世界坐标变换到 Decal 物体的本地空间（0~1 的 Box 范围）
float3 localPos = mul(unity_WorldToObject, float4(worldPos.xyz, 1.0)).xyz + 0.5;
// localPos 在 [0,1] 内：在 Decal 投影范围内
// localPos 超出 [0,1]：在范围外，clip 掉
clip(localPos - 0.0001);
clip(0.9999 - localPos);
```

**4. 用本地坐标做 UV 采样**

```hlsl
float2 decalUV = localPos.xz;   // 从上往下投影用 XZ 平面
half4  decal   = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, decalUV);

// Alpha Blend 叠加到底层颜色
```

---

## 简单手写 Decal Shader

```hlsl
Shader "Custom/SimpleDecal"
{
    Properties
    {
        _DecalTex   ("Decal Texture", 2D)    = "white" {}
        _DecalColor ("Decal Color",   Color) = (1,1,1,1)
        _FadeEdge   ("Edge Fade",     Range(0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
               "Queue" = "Transparent+10" }   // 比普通透明晚一点

        Pass
        {
            Name "DecalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1   // 防止 Z-fighting（深度偏移，稍微靠近摄像机）

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DecalTex_ST;
                float4 _DecalColor;
                float  _FadeEdge;
            CBUFFER_END
            TEXTURE2D(_DecalTex); SAMPLER(sampler_DecalTex);

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION;
                                float3 localPos    : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                // 物体空间位置（Box 的 -0.5~0.5，加 0.5 映射到 0~1）
                output.localPos    = input.positionOS.xyz + 0.5;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 lp = input.localPos;

                // 裁剪 Box 范围之外的像素
                clip(lp.x); clip(1.0 - lp.x);
                clip(lp.y); clip(1.0 - lp.y);
                clip(lp.z); clip(1.0 - lp.z);

                // 边缘渐变遮罩
                float2 edgeFade = min(lp.xz, 1.0 - lp.xz) / max(_FadeEdge, 0.001);
                float  alpha    = saturate(min(edgeFade.x, edgeFade.y));

                // 采样贴花贴图（用 XZ 平面 UV）
                float2 decalUV = lp.xz;
                half4  decal   = SAMPLE_TEXTURE2D(_DecalTex, sampler_DecalTex, decalUV) * _DecalColor;

                return half4(decal.rgb, decal.a * alpha);
            }
            ENDHLSL
        }
    }
}
```

---

## Z-fighting 处理

Decal 贴在表面上，深度值与底面几乎完全相同，容易产生 Z-fighting（闪烁）。解决方案：

```hlsl
// 深度偏移（ShaderLab 级别）
Offset -1, -1
// 第一个参数：乘以斜率偏移（斜面更多偏移）
// 第二个参数：固定偏移量（单位：深度精度的倍数）
```

或者在 Pass 里设置 `ZTest Always`（直接覆盖，但会覆盖在其他物体前面）。

---

## URP Decal Renderer Feature vs 手写

| | URP 内置 | 手写 |
|--|---------|------|
| 法线混合 | ✅ 支持 | 需自行实现 |
| 性能剔除 | ✅ 自动 | 需自行 clip |
| 深度重建 | ✅ 自动 | 需手写 |
| 定制灵活性 | 有限 | 完全自由 |
| 移动端支持 | 需 URP 14+ | 通用 |

实际项目：优先用内置 Decal 系统；需要特殊效果（如 Decal 带高光、带法线动画）再手写。

---

## 小结

| 概念 | 要点 |
|------|------|
| Decal 原理 | Box 投影，本地坐标做 UV，clip 范围外的像素 |
| Z-fighting | `Offset -1, -1` 或 `ZTest Always` |
| 边缘渐变 | `min(lp.xz, 1-lp.xz) / fadeWidth`，边缘 alpha 渐变到 0 |
| URP 内置 | Decal Renderer Feature + Decal Projector 组件，推荐优先用 |

下一篇：视差贴图——用高度图偏移 UV，让平面表现出砖墙、石块的凹凸深度感。
