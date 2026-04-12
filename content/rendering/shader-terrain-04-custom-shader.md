---
title: "游戏常用效果｜自定义地形 Shader：替换 Unity 内置地形材质的完整流程"
slug: "shader-terrain-04-custom-shader"
date: "2026-03-28"
description: "从替换 materialTemplate 到实现雪覆盖与季节变化，拆解自定义地形 Shader 的必要 Pass 和变量约定。"
tags: ["Shader", "HLSL", "URP", "地形", "自定义Shader", "Terrain"]
series: "Shader 手写技法"
weight: 4560
---

Unity 内置的地形 Shader 足够通用，但一旦需要雪覆盖、湿润积水、季节颜色变化等特效，就只能替换成自定义材质。替换地形 Shader 并不复杂，但有几个约定俗成的规则必须遵守：变量命名、Pass 结构、以及与地形系统的数据对接方式。

---

## 为什么要替换默认地形 Shader

Unity 的 `TerrainLit.shader` 实现了标准的 PBR 光照 + SplatMap 混合，已经很完善。但它不支持：

- **雪覆盖**：根据法线朝向和高度动态叠加雪贴图
- **湿润效果**：雨后地表积水，调整 Roughness 和 Albedo
- **季节颜色**：秋天草地变黄，春天恢复绿色
- **自定义光照模型**：卡通风格或带 Rim Light 的写实渲染
- **区域特效**：地下洞穴渐隐、区域颜色叠加

这些需求都指向同一个解决方案：替换 `Terrain.materialTemplate`。

---

## 替换入口：materialTemplate

在 C# 侧，一行代码即可完成替换：

```csharp
[RequireComponent(typeof(Terrain))]
public class TerrainMaterialOverride : MonoBehaviour
{
    public Material customMaterial;

    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.materialTemplate = customMaterial;
    }
}
```

`materialTemplate` 赋值后，地形立刻切换到新材质渲染。但如果 Shader 缺少 URP 要求的 Pass，会出现阴影缺失或深度预通道错误，所以 Pass 结构必须完整。

---

## 必须实现的三个 Pass

```hlsl
// Pass 1：主光照
Pass
{
    Name "UniversalForward"
    Tags { "LightMode" = "UniversalForward" }
    // ... 主渲染逻辑
}

// Pass 2：阴影投射
Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }
    ZWrite On
    ZTest LEqual
    ColorMask 0
    Cull Back

    HLSLPROGRAM
    #pragma vertex ShadowPassVertex
    #pragma fragment ShadowPassFragment
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
    ENDHLSL
}

// Pass 3：深度预通道
Pass
{
    Name "DepthOnly"
    Tags { "LightMode" = "DepthOnly" }
    ZWrite On
    ColorMask R
    Cull Back

    HLSLPROGRAM
    #pragma vertex DepthOnlyVertex
    #pragma fragment DepthOnlyFragment
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
    ENDHLSL
}
```

ShadowCaster 和 DepthOnly 可以直接 include URP 包里现成的 Pass 文件，不需要从零写。

---

## 变量命名约定

Unity 地形系统在材质上自动设置以下属性，Shader 里的声明名称必须一致：

```hlsl
// 权重贴图（地形系统自动赋值，不需要手动传）
TEXTURE2D(_Control);   SAMPLER(sampler_Control);   // SplatMap 第一张（层 0~3）
TEXTURE2D(_Control1);  SAMPLER(sampler_Control1);  // SplatMap 第二张（层 4~7）

// 各层 Albedo
TEXTURE2D(_Splat0);    SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);    SAMPLER(sampler_Splat1);
TEXTURE2D(_Splat2);    SAMPLER(sampler_Splat2);
TEXTURE2D(_Splat3);    SAMPLER(sampler_Splat3);

// 各层 Normal（可选，但命名必须对应）
TEXTURE2D(_Normal0);   SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);   SAMPLER(sampler_Normal1);
TEXTURE2D(_Normal2);   SAMPLER(sampler_Normal2);
TEXTURE2D(_Normal3);   SAMPLER(sampler_Normal3);

// 各层 Tiling & Offset
float4 _Splat0_ST;
float4 _Splat1_ST;
float4 _Splat2_ST;
float4 _Splat3_ST;
```

只要 Shader 中声明了这些变量，地形笔刷的所有编辑操作都会自动反映到渲染结果中，无需额外的 C# 桥接代码。

---

## 雪覆盖效果

雪的分布由两个因素决定：面朝上（法线 Y 分量大）且高度超过阈值。

```hlsl
// 在 Fragment Shader 中，获取世界空间法线后计算雪覆盖权重
float3 worldNormal = // ... 从 SplatMap 混合得到的世界法线

// 法线朝上权重（_SnowSlope 控制坡度容忍度，建议 4.0~8.0）
float upFacing  = saturate(worldNormal.y);
float snowFactor = pow(upFacing, _SnowSlope);

// 高度权重（可选，让低谷不积雪）
float heightFactor = saturate((input.positionWS.y - _SnowHeightMin) / _SnowHeightRange);
snowFactor *= heightFactor;

// 采样雪贴图（用世界空间 XZ 做 UV，避免地形拉伸）
float4 snowAlbedo = SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex,
                                      input.positionWS.xz * _SnowTiling);

// 混合到地形 Albedo
surfaceData.albedo     = lerp(surfaceData.albedo, snowAlbedo.rgb, snowFactor * _SnowAmount);
surfaceData.smoothness = lerp(surfaceData.smoothness, _SnowSmoothness, snowFactor * _SnowAmount);
surfaceData.metallic   = lerp(surfaceData.metallic, 0.0, snowFactor * _SnowAmount);
```

`_SnowAmount` 由 C# 侧的天气系统控制，可以在运行时平滑插值实现下雪/融雪过渡效果。

---

## 季节颜色变化

季节效果最简单的实现是用全局 Shader 变量 `_SeasonBlend` 驱动颜色插值：

```csharp
// C# 侧，在季节管理器中更新
// 0 = 夏季，1 = 秋季，可扩展到 4 季循环
Shader.SetGlobalFloat("_SeasonBlend", seasonController.currentBlend);
```

```hlsl
// Shader 里（Fragment，在 albedo 计算完成后）
float season = _SeasonBlend;

// 夏天不变色，秋天偏黄橙
float3 summerTint = float3(1.0, 1.0, 1.0);
float3 autumnTint = float3(1.5, 0.9, 0.3);

float3 seasonColor = lerp(summerTint, autumnTint, saturate(season));
surfaceData.albedo *= seasonColor;
```

4 季循环版本：

```hlsl
// _SeasonBlend: 0=冬, 1=春, 2=夏, 3=秋, 4=冬（循环）
float3 tints[4] = {
    float3(0.8, 0.9, 1.0),   // 冬：冷白
    float3(0.9, 1.1, 0.8),   // 春：嫩绿
    float3(1.0, 1.0, 1.0),   // 夏：正常
    float3(1.5, 0.9, 0.3)    // 秋：黄橙
};
int   s0 = (int)_SeasonBlend % 4;
int   s1 = (s0 + 1) % 4;
float t  = frac(_SeasonBlend);
surfaceData.albedo *= lerp(tints[s0], tints[s1], t);
```

---

## 完整的自定义地形 Shader 框架

```hlsl
Shader "Custom/TerrainCustom"
{
    Properties
    {
        // 地形系统自动填充，Inspector 里隐藏
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat0  ("Layer 0 (R)",    2D) = "white" {}
        [HideInInspector] _Splat1  ("Layer 1 (G)",    2D) = "white" {}
        [HideInInspector] _Splat2  ("Layer 2 (B)",    2D) = "white" {}
        [HideInInspector] _Splat3  ("Layer 3 (A)",    2D) = "white" {}
        [HideInInspector] _Normal0 ("Normal 0",        2D) = "bump" {}
        [HideInInspector] _Normal1 ("Normal 1",        2D) = "bump" {}
        [HideInInspector] _Normal2 ("Normal 2",        2D) = "bump" {}
        [HideInInspector] _Normal3 ("Normal 3",        2D) = "bump" {}

        // 自定义特效参数
        _SnowTex        ("Snow Albedo",     2D)         = "white" {}
        _SnowTiling     ("Snow Tiling",     Float)      = 0.1
        _SnowSlope      ("Snow Slope",      Float)      = 6.0
        _SnowAmount     ("Snow Amount",     Range(0,1)) = 0.0
        _SnowHeightMin  ("Snow Height Min", Float)      = 50.0
        _SnowHeightRange("Snow Height Range", Float)    = 20.0
        _SnowSmoothness ("Snow Smoothness", Range(0,1)) = 0.8
        _BlendSharpness ("Height Blend Sharpness", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry-100"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   TerrainVert
            #pragma fragment TerrainFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // --- 变量声明（见上文约定） ---
            // --- TerrainVert / TerrainFrag 实现（参考前几篇的混合与雪覆盖逻辑） ---
            ENDHLSL
        }

        // ShadowCaster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // DepthOnly Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}
```

这个框架是后续所有地形特效的起点。雪覆盖、湿润、季节变化都是在 `TerrainFrag` 里追加逻辑，Pass 结构和变量命名保持不变。
