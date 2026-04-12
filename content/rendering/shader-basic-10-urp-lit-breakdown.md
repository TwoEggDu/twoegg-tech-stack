---
title: "Shader 语法基础 10｜URP Lit Shader 拆解：从 LitInput.hlsl 到 Lighting.hlsl"
slug: "shader-basic-10-urp-lit-breakdown"
date: "2026-03-28"
description: "拆解 URP Lit Shader 的完整文件结构，梳理 InputData 和 SurfaceData 两个关键结构体的字段来源，讲解 UniversalFragmentPBR() 的调用方式，并示范如何基于 URP Lit 添加顶点色叠加效果，而不需要从零实现 BRDF。"
tags: ["Shader", "URP", "Lit", "PBR", "Unity", "HLSL"]
series: "Shader 手写技法"
weight: 4140
---

前几篇从零搭建了最小 Unlit Shader。但实际项目里，大多数材质需要响应场景灯光——方向光、点光源、阴影、环境光遮蔽……从零实现这些是几千行代码的工作量。URP 已经把这套 PBR 光照系统写好了，暴露给我们的接口非常简洁：填好两个结构体，调用一个函数，剩下的引擎来做。

---

## URP Lit Shader 的文件结构

Unity 包里的 Lit Shader 由多个文件协作完成，打开 `Packages/com.unity.render-pipelines.universal/Shaders/` 可以看到：

```
Lit.shader
  └─ LitInput.hlsl          // 材质属性声明、贴图采样辅助函数
  └─ LitForwardPass.hlsl    // vert / frag 主体
       └─ Lighting.hlsl     // UniversalFragmentPBR() 等光照函数
            └─ BRDF.hlsl    // Cook-Torrance BRDF 实现
            └─ GlobalIllumination.hlsl  // 环境光、反射探针
```

`Lit.shader` 本身很短，主要是 Properties 声明和 SubShader/Pass 框架。真正的逻辑在 `.hlsl` 文件里，通过 `#include` 链接。

理解这个结构的意义在于：当我们要写自定义 Lit Shader 时，可以选择 include `LitForwardPass.hlsl`（直接复用所有逻辑），或者只 include `Lighting.hlsl`（只用光照函数，自己写 vert/frag），灵活度很高。

---

## InputData 结构体：光照计算的上下文

`InputData` 是传给光照函数的"渲染上下文"，描述当前片元所在的空间信息：

```hlsl
struct InputData
{
    float3 positionWS;              // 世界空间坐标
    float4 positionCS;              // 裁剪空间坐标（用于 shadowCoord 计算）
    float3 normalWS;                // 世界空间法线（已 normalize）
    half3  viewDirectionWS;         // 从片元指向摄像机的方向（已 normalize）
    float4 shadowCoord;             // 阴影贴图采样坐标
    half   fogCoord;                // 雾效系数
    half3  vertexLighting;          // 每顶点额外光源（非主光）
    half3  bakedGI;                 // 烘焙 GI / 光照探针
    float2 normalizedScreenSpaceUV; // 归一化屏幕 UV
    half4  shadowMask;              // 烘焙阴影遮罩
};
```

在 `LitForwardPass.hlsl` 里有一个辅助函数 `InitializeInputData()` 负责填充这个结构体，我们自己写 Shader 时可以参考它的实现，按需填写必要字段。最关键的几个：

- `normalWS`：必须是已 normalize 的世界空间法线
- `viewDirectionWS`：`GetWorldSpaceNormalizeViewDir(positionWS)` 可以一步得到
- `shadowCoord`：`TransformWorldToShadowCoord(positionWS)` 计算，或从 Vertex Shader 传入
- `bakedGI`：`SampleSH(normalWS)` 采样球谐光照

---

## SurfaceData 结构体：材质的 PBR 参数

`SurfaceData` 描述材质本身的物理属性，是 PBR 方程的输入材料：

```hlsl
struct SurfaceData
{
    half3 albedo;           // 基础颜色（漫反射颜色），[0,1]
    half  metallic;         // 金属度，0=非金属，1=纯金属
    half3 specular;         // 高光颜色（与 metallic 工作流互斥）
    half  smoothness;       // 光滑度，0=粗糙，1=光滑镜面
    half  occlusion;        // 环境光遮蔽，0=完全遮蔽，1=无遮蔽
    half3 emission;         // 自发光颜色
    half  alpha;            // 透明度（AlphaBlend/AlphaTest 时使用）
    half3 normalTS;         // 切线空间法线（来自法线贴图解码）
    half3 clearCoatMask;    // 清漆层（高级，非必填）
    half  clearCoatSmoothness;
};
```

填充这个结构体是 Fragment Shader 里最主要的工作。典型的 PBR 材质：

```hlsl
SurfaceData surfaceData;
surfaceData.albedo     = texColor.rgb * _BaseColor.rgb;
surfaceData.metallic   = SAMPLE_TEXTURE2D(_MetallicGlossMap, ...).r * _Metallic;
surfaceData.smoothness = SAMPLE_TEXTURE2D(_MetallicGlossMap, ...).a * _Smoothness;
surfaceData.occlusion  = SAMPLE_TEXTURE2D(_OcclusionMap, ...).g;
surfaceData.emission   = _EmissionColor.rgb;
surfaceData.alpha      = texColor.a * _BaseColor.a;
surfaceData.normalTS   = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, ...));
```

---

## UniversalFragmentPBR()：把光照计算交给引擎

填好 `InputData` 和 `SurfaceData` 后，调用一个函数就能得到完整的 PBR 光照结果：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half4 color = UniversalFragmentPBR(inputData, surfaceData);
```

这个函数内部做了：
1. 主方向光的 Cook-Torrance BRDF（漫反射 + 镜面反射）
2. 最多 4 个额外点光源的光照（Vertex Lighting 或 Forward+ Tile Light）
3. 环境反射（反射探针 + 天空盒）
4. 烘焙 GI 和实时阴影合并
5. 自发光叠加

我们不需要实现任何一项，只负责提供正确的输入。

---

## 实战：基于 URP Lit 添加顶点色叠加

在不修改 BRDF 的前提下，往 URP Lit Shader 里加效果，最典型的做法是在填充 `SurfaceData` 时叠加额外数据。下面这个示例把顶点色乘进 albedo，实现一个简单的植被着色风格（根部压暗）：

```hlsl
Shader "Custom/LitWithVertexColor"
{
    Properties
    {
        _BaseMap        ("Base Texture", 2D)          = "white" {}
        _BaseColor      ("Base Color", Color)          = (1, 1, 1, 1)
        _Metallic       ("Metallic",  Range(0, 1))     = 0.0
        _Smoothness     ("Smoothness", Range(0, 1))    = 0.5
        _VertexColorMix ("Vertex Color Mix", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Metallic;
                half   _Smoothness;
                half   _VertexColorMix;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv0        : TEXCOORD0;
                float4 vertColor  : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                half4  vertColor   : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS    = TransformWorldToHClip(positionWS);
                OUT.positionWS    = positionWS;
                OUT.normalWS      = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv            = TRANSFORM_TEX(IN.uv0, _BaseMap);
                OUT.vertColor     = IN.vertColor;
                OUT.fogFactor     = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- 采样基础贴图 ---
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // --- 顶点色叠加（lerp 控制混合强度）---
                half3 albedo = texColor.rgb * _BaseColor.rgb;
                albedo = lerp(albedo, albedo * IN.vertColor.rgb, _VertexColorMix);

                // --- 填充 SurfaceData ---
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo;
                surfaceData.metallic   = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion  = 1.0;
                surfaceData.alpha      = texColor.a * _BaseColor.a;
                surfaceData.normalTS   = half3(0, 0, 1); // 无法线贴图时用默认法线

                // --- 填充 InputData ---
                InputData inputData = (InputData)0;
                inputData.positionWS        = IN.positionWS;
                inputData.normalWS          = normalize(IN.normalWS);
                inputData.viewDirectionWS   = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord       = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord          = IN.fogFactor;
                inputData.bakedGI           = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                // --- URP PBR 光照计算 ---
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // --- 雾效 ---
                color.rgb = MixFog(color.rgb, IN.fogFactor);

                return color;
            }
            ENDHLSL
        }
    }
}
```

这个 Shader 的关键设计思路：所有 BRDF 运算都在 `UniversalFragmentPBR` 里面，我们只改了"喂进去的食材"——把顶点色 lerp 进 albedo。这是改造 URP Lit 的标准套路：定制 `SurfaceData` 的填充逻辑，不动光照核心。

---

## 几个值得注意的细节

`#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE` 这行不能省，否则物体不会接收阴影。URP 用 shader keyword 来开关阴影级联，不声明就默认关闭。

`(SurfaceData)0` 和 `(InputData)0` 是 HLSL 的零初始化语法，确保没有显式赋值的字段是 0，而不是垃圾值。这是防止未初始化字段导致奇怪视觉 bug 的好习惯。

`SampleSH(normalWS)` 采样球谐光照，是 URP 里环境光的来源之一。漏掉这行会导致物体在没有直接光照的区域完全黑掉。

到这里，从 ShaderLab 结构（第 07 篇）到 Vertex Shader（第 08 篇）、Fragment Shader（第 09 篇），再到 URP 光照系统对接（本篇），构成了完整的 URP Shader 开发知识链路。后续可以在这个基础上展开法线贴图、自定义光照模型、后处理等进阶话题。
