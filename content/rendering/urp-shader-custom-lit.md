---
title: "URP Shader 手写｜从骨架到完整光照：接入主光、附加光与阴影"
slug: "urp-shader-custom-lit"
date: "2026-03-26"
description: "在 URP 里手写一个完整的 Lit Shader：讲清楚 URP Shader 的骨架结构、include 体系、与 Built-in 的关键差异，以及如何正确接入主光（GetMainLight）、附加光循环、Shadow 采样和 ShadowCaster Pass。最终得到一个可以直接用于项目的自定义光照 Shader 模板。"
tags:
  - "Unity"
  - "URP"
  - "Shader"
  - "HLSL"
  - "光照"
  - "阴影"
  - "渲染管线"
series: "URP 深度"
weight: 1680
---
URP 自带的 Lit Shader 功能完整，但高度封装——想加一个效果、改一个细节，要在几千行代码里找位置。手写 Shader 的价值在于：你完全清楚每一行代码在做什么，改起来没有障碍。

这篇从一个空文件开始，一步步写出一个接入 URP 完整光照体系的 Shader：主光漫反射 + 高光、附加光循环、主光阴影接收、ShadowCaster Pass。

---

## 为什么 Built-in 的 Shader 在 URP 里不工作

新建一个 Built-in 的 Shader，放进 URP 项目，材质会显示洋红色。原因有三个：

**① RenderPipeline Tag 不匹配**

```hlsl
// Built-in：没有 RenderPipeline tag
Tags { "RenderType"="Opaque" }

// URP：必须声明目标管线
Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
```

URP 在渲染时会跳过没有 `RenderPipeline = "UniversalPipeline"` 标记的 SubShader，直接走 Fallback，Fallback 通常是洋红色的 Error Shader。

**② LightMode Tag 不同**

```hlsl
// Built-in
Tags { "LightMode"="ForwardBase" }   // 旧写法

// URP
Tags { "LightMode"="UniversalForward" }  // 正向渲染主 Pass
```

URP 的渲染循环只会执行 `LightMode = "UniversalForward"`（以及 `"ShadowCaster"`、`"DepthOnly"` 等）的 Pass，Built-in 的 `ForwardBase` 不会被执行。

**③ Include 体系完全不同**

```hlsl
// Built-in
#include "UnityCG.cginc"
#include "Lighting.cginc"

// URP（路径在 Package 目录下）
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
```

变量名、宏、内置函数全部不同。`UnityObjectToClipPos` → `TransformObjectToHClip`，`_WorldSpaceLightPos0` → `GetMainLight().direction`，等等。

---

## URP Shader 的骨架

最简单的 URP Unlit Shader：

```hlsl
Shader "Custom/URPUnlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        // ① 声明目标管线和渲染类型
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            // ② 声明 Pass 的 LightMode
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // ③ URP 的核心 include
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ④ 属性必须放在 CBUFFER 里（SRP Batcher 兼容要求）
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                // ⑤ URP 的坐标变换宏
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                return baseColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}
```

**几个关键点：**

- `CBUFFER_START(UnityPerMaterial)` / `CBUFFER_END`：SRP Batcher 要求材质属性放在名为 `UnityPerMaterial` 的 Constant Buffer 里，否则 SRP Batcher 不会对这个 Shader 生效
- `TEXTURE2D` / `SAMPLER`：URP 的纹理声明宏，在不同平台展开为对应的纹理类型
- `TransformObjectToHClip`：等价于 Built-in 的 `UnityObjectToClipPos`，来自 `Core.hlsl`
- `TRANSFORM_TEX`：UV 缩放偏移宏，等价于 Built-in 的同名宏，但依赖 CBUFFER 里有 `_BaseMap_ST`

---

## URP 的 Include 体系

不需要记所有文件，常用的只有四个：

| 文件 | 内容 |
|------|------|
| `Core.hlsl` | 坐标变换（TransformObjectToHClip 等）、矩阵宏、CBUFFER 宏、TEXTURE2D 宏 |
| `Lighting.hlsl` | `GetMainLight()`、`GetAdditionalLight()`、BRDF 函数 |
| `Shadows.hlsl` | `TransformWorldToShadowCoord()`、Shadow 采样函数 |
| `ShaderVariablesFunctions.hlsl` | `GetWorldSpaceViewDir()`、`GetWorldSpaceNormalizeViewDir()` 等工具函数 |

`Lighting.hlsl` 内部已经 include 了 `Core.hlsl` 和 `Shadows.hlsl`，所以大多数情况只需要 include `Lighting.hlsl` 一个文件。

---

## 接入主光：GetMainLight

在 `Lighting.hlsl` include 之后，用 `GetMainLight()` 获取主方向光的数据：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Light 结构体包含：
// light.direction         — 光方向（世界空间，归一化，从表面指向光源）
// light.color             — 光颜色 × 光强度
// light.distanceAttenuation — 距离衰减（方向光始终为 1）
// light.shadowAttenuation — 阴影遮蔽（0=完全遮挡，1=完全照亮）

half4 frag(Varyings i) : SV_Target
{
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

    // 获取主光（不带阴影）
    Light mainLight = GetMainLight();

    // Lambert 漫反射
    half3 normalWS = normalize(i.normalWS);
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half3 diffuse = mainLight.color * NdotL;

    return half4(baseColor.rgb * diffuse, baseColor.a);
}
```

要让法线从 Object Space 变换到 World Space，Vertex Shader 需要传递法线：

```hlsl
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float3 normalWS    : TEXCOORD1;
    float3 positionWS  : TEXCOORD2;
    float2 uv          : TEXCOORD0;
};

Varyings vert(Attributes v)
{
    Varyings o;
    VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
    VertexNormalInputs normInputs  = GetVertexNormalInputs(v.normalOS);

    o.positionHCS = posInputs.positionCS;
    o.positionWS  = posInputs.positionWS;
    o.normalWS    = normInputs.normalWS;
    o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
    return o;
}
```

`GetVertexPositionInputs` 和 `GetVertexNormalInputs` 是 `Core.hlsl` 提供的工具函数，一次性计算出所有坐标空间下的位置和法线，比手动 `mul(UNITY_MATRIX_M, ...)` 更简洁且不容易出错。

---

## 加入高光：Blinn-Phong

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseMap_ST;
    half   _Smoothness;   // 光滑度，0~1
CBUFFER_END

half4 frag(Varyings i) : SV_Target
{
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
    Light mainLight = GetMainLight();

    half3 normalWS   = normalize(i.normalWS);
    half3 viewDirWS  = GetWorldSpaceNormalizeViewDir(i.positionWS);
    half3 halfDir    = normalize(mainLight.direction + viewDirWS);

    // 漫反射
    half NdotL = saturate(dot(normalWS, mainLight.direction));
    half3 diffuse = mainLight.color * NdotL;

    // 高光（Blinn-Phong）
    half NdotH    = saturate(dot(normalWS, halfDir));
    half specPow  = exp2(_Smoothness * 10.0 + 1.0);  // Smoothness → 高光锐度
    half3 specular = mainLight.color * pow(NdotH, specPow) * NdotL;

    half3 finalColor = baseColor.rgb * (diffuse + specular);
    return half4(finalColor, baseColor.a);
}
```

---

## 接入阴影

接入阴影需要两步：**传递阴影坐标** 和 **在 Fragment 里采样阴影**。

### 步骤一：Vertex Shader 计算阴影坐标

```hlsl
// 添加必要的 Shader Feature 关键字（控制阴影变体编译）
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile _ _SHADOWS_SOFT

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float3 normalWS     : TEXCOORD1;
    float3 positionWS   : TEXCOORD2;
    float2 uv           : TEXCOORD0;
    float4 shadowCoord  : TEXCOORD3;  // 阴影坐标
};

Varyings vert(Attributes v)
{
    Varyings o;
    VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
    VertexNormalInputs normInputs  = GetVertexNormalInputs(v.normalOS);

    o.positionHCS = posInputs.positionCS;
    o.positionWS  = posInputs.positionWS;
    o.normalWS    = normInputs.normalWS;
    o.uv          = TRANSFORM_TEX(v.uv, _BaseMap);

    // 计算阴影坐标（Cascade Shadow Map 的 UV）
    o.shadowCoord = GetShadowCoord(posInputs);
    return o;
}
```

### 步骤二：Fragment 用阴影坐标获取带阴影的主光

```hlsl
half4 frag(Varyings i) : SV_Target
{
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

    // 带阴影的主光：传入阴影坐标
    Light mainLight = GetMainLight(i.shadowCoord);
    // mainLight.shadowAttenuation 现在包含阴影遮蔽信息（0~1）

    half3 normalWS  = normalize(i.normalWS);
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
    half3 halfDir   = normalize(mainLight.direction + viewDirWS);

    half NdotL    = saturate(dot(normalWS, mainLight.direction));
    half NdotH    = saturate(dot(normalWS, halfDir));
    half specPow  = exp2(_Smoothness * 10.0 + 1.0);

    // shadowAttenuation 同时衰减漫反射和高光
    half shadow   = mainLight.shadowAttenuation;
    half3 diffuse = mainLight.color * NdotL * shadow;
    half3 specular = mainLight.color * pow(NdotH, specPow) * NdotL * shadow;

    half3 finalColor = baseColor.rgb * (diffuse + specular);
    return half4(finalColor, baseColor.a);
}
```

---

## 接入附加光

URP 支持多盏附加光（Point Light、Spot Light）。在 Forward 路径下，附加光通过循环逐一计算。

```hlsl
// 附加光的关键字
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

half4 frag(Varyings i) : SV_Target
{
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
    half3 normalWS  = normalize(i.normalWS);
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

    // 主光
    Light mainLight  = GetMainLight(i.shadowCoord);
    half  NdotL_main = saturate(dot(normalWS, mainLight.direction));
    half3 lighting   = mainLight.color * NdotL_main * mainLight.shadowAttenuation;

    // 附加光循环
    #ifdef _ADDITIONAL_LIGHTS
    uint additionalLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0; lightIndex < additionalLightCount; lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, i.positionWS);

        half NdotL = saturate(dot(normalWS, light.direction));
        // distanceAttenuation：点光/聚光的距离衰减
        // shadowAttenuation：附加光阴影（需要 _ADDITIONAL_LIGHT_SHADOWS）
        half attenuation = light.distanceAttenuation * light.shadowAttenuation;

        lighting += light.color * NdotL * attenuation;
    }
    #endif

    return half4(baseColor.rgb * lighting, baseColor.a);
}
```

**关键字说明**：
- `_ADDITIONAL_LIGHTS`：逐像素附加光，在 URP Pipeline Asset 里设置 Additional Lights 为 Per Pixel 时启用
- `_ADDITIONAL_LIGHTS_VERTEX`：逐顶点附加光，代价更低，效果差一些
- 如果两个都没有启用，附加光不参与计算

---

## ShadowCaster Pass：让物体能投射阴影

只有 UniversalForward Pass 的物体只能**接收**阴影，不能**投射**阴影。要投射阴影，需要一个 `LightMode = "ShadowCaster"` 的 Pass：

```hlsl
// 在同一个 SubShader 里添加第二个 Pass
Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }

    ZWrite On
    ZTest LEqual
    ColorMask 0          // 不写颜色，只写深度
    Cull Back

    HLSLPROGRAM
    #pragma vertex ShadowPassVertex
    #pragma fragment ShadowPassFragment

    // 关键字：支持不同的 Shadow 编译变体
    #pragma multi_compile_shadowcaster
    #pragma multi_compile _ DOTS_INSTANCING_ON

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShadowCasterPass.hlsl"
    ENDHLSL
}
```

**最简实现直接 include `ShadowCasterPass.hlsl`**，它已经包含了 `ShadowPassVertex` 和 `ShadowPassFragment` 的完整实现（处理 Shadow Bias、Normal Bias、深度输出）。自定义需求（如透明物体 Alpha Test 裁剪阴影边缘）才需要手写。

带 Alpha Test 的 Shadow Caster：

```hlsl
Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }
    ZWrite On ZTest LEqual ColorMask 0 Cull Off  // Cull Off：双面阴影

    HLSLPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile_shadowcaster
    #pragma shader_feature_local _ALPHATEST_ON

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half   _Cutoff;
    CBUFFER_END
    TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

    struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
    struct Varyings   { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; };

    Varyings vert(Attributes v)
    {
        Varyings o;
        // ApplyShadowBias：自动处理 Shadow Normal Bias
        float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
        float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
        posWS = ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz);
        o.positionHCS = TransformWorldToHClip(posWS);
        o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
        return o;
    }

    half4 frag(Varyings i) : SV_Target
    {
        #ifdef _ALPHATEST_ON
            half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a;
            clip(alpha - _Cutoff);
        #endif
        return 0;
    }
    ENDHLSL
}
```

---

## 完整 Shader 模板

把上面所有内容组合成可直接使用的模板：

```hlsl
Shader "Custom/URPLit"
{
    Properties
    {
        _BaseMap      ("Albedo", 2D)               = "white" {}
        _BaseColor    ("Base Color", Color)         = (1,1,1,1)
        _Smoothness   ("Smoothness", Range(0,1))    = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaTestToggle ("Alpha Test", Float) = 0
        _Cutoff       ("Alpha Cutoff", Range(0,1))  = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ── Pass 1：主渲染 ──────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                half   _Smoothness;
                half   _Cutoff;
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
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float2 uv          : TEXCOORD0;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(v.normalOS);

                o.positionHCS = posInputs.positionCS;
                o.positionWS  = posInputs.positionWS;
                o.normalWS    = normInputs.normalWS;
                o.uv          = TRANSFORM_TEX(v.uv, _BaseMap);
                o.shadowCoord = GetShadowCoord(posInputs);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                #ifdef _ALPHATEST_ON
                    clip(baseColor.a - _Cutoff);
                #endif

                half3 normalWS  = normalize(i.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // 主光 + 阴影
                Light mainLight = GetMainLight(i.shadowCoord);
                half  NdotL     = saturate(dot(normalWS, mainLight.direction));
                half3 halfDir   = normalize(mainLight.direction + viewDirWS);
                half  NdotH     = saturate(dot(normalWS, halfDir));
                half  specPow   = exp2(_Smoothness * 10.0h + 1.0h);
                half  shadow    = mainLight.shadowAttenuation;

                half3 lighting  = mainLight.color * NdotL * shadow;
                lighting       += mainLight.color * pow(NdotH, specPow) * NdotL * shadow;

                // 附加光
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint idx = 0u; idx < lightCount; idx++)
                {
                    Light light = GetAdditionalLight(idx, i.positionWS);
                    half  ndl   = saturate(dot(normalWS, light.direction));
                    half  atten = light.distanceAttenuation * light.shadowAttenuation;
                    lighting   += light.color * ndl * atten;
                }
                #endif

                // 环境光（来自 Ambient / Skybox）
                half3 ambient = SampleSH(normalWS);

                return half4(baseColor.rgb * (lighting + ambient), baseColor.a);
            }
            ENDHLSL
        }

        // ── Pass 2：阴影投射 ─────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half   _Cutoff;
                half   _Smoothness;
                float4 _BaseColor;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS    = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                posWS           = ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz);
                o.positionHCS   = TransformWorldToHClip(posWS);
                o.uv            = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ── Pass 3：深度预渲染（DepthOnly，供 SSAO / 软粒子使用）──────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
```

---

## 常见问题

**Q：材质球上的属性改了，Shader 没有反应**

检查 `CBUFFER_START(UnityPerMaterial)` 里是否包含了这个属性。SRP Batcher 要求所有材质属性都在这个 CBUFFER 里，漏掉会导致属性不生效。

**Q：阴影不显示**

1. 确认 Pass 里有 `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE`
2. 确认 Varyings 里有 `shadowCoord`，并在 Vertex Shader 里用 `GetShadowCoord` 赋值
3. 确认 `GetMainLight(i.shadowCoord)` 传入了 shadowCoord（不是 `GetMainLight()`）
4. 确认场景里有 ShadowCaster Pass（第二个 Pass）

**Q：附加光（点光/聚光）不起作用**

URP Pipeline Asset 里的 `Additional Lights` 要设为 `Per Pixel`，同时 Shader 里要有 `#pragma multi_compile _ _ADDITIONAL_LIGHTS` 关键字。

**Q：SRP Batcher 显示不兼容**

Frame Debugger 里点击 Draw Call，看右侧的 SRP Batcher 状态说明。最常见的原因是某个属性没有放在 `CBUFFER_START(UnityPerMaterial)` 里，或者 CBUFFER 里声明了 Properties 里没有的属性。

---

## 小结

- Built-in Shader 在 URP 失效的三个原因：缺 `RenderPipeline` Tag、LightMode 不匹配、include 体系不同
- SRP Batcher 兼容要求材质属性放在 `CBUFFER_START(UnityPerMaterial)` 里
- 主光用 `GetMainLight(shadowCoord)`，附加光用 `GetAdditionalLight(idx, posWS)` 循环
- 阴影接收：Vertex 里 `GetShadowCoord`，Fragment 里传入 `GetMainLight`，加 `multi_compile` 关键字
- 阴影投射：独立 `ShadowCaster` Pass，简单情况 include `ShadowCasterPass.hlsl` 即可
- `DepthOnly` Pass：供 SSAO、软粒子、Depth of Field 采样深度用，推荐加上
