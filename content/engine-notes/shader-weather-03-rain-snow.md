---
title: "游戏常用效果｜雨雪 Shader：雨滴法线扰动、积雪顶面权重与粒子配合"
slug: shader-weather-03-rain-snow
date: "2026-03-28"
description: "讲解雨天地面波纹动画、湿润度计算、雨滴法线扰动，以及积雪覆盖的顶面权重算法和法线修正，附完整雨雪双模式地面材质 Shader 代码。"
tags: ["Shader", "HLSL", "URP", "天气", "雨雪效果", "法线扰动", "积雪"]
series: "Shader 手写技法"
weight: 4590
---

雨雪天气效果分两个层次：粒子系统负责视觉上的雨丝和雪片飘落，Shader 负责地表对降水的响应。粒子可以做得很华丽，但如果地面材质没有任何变化——干燥的路面上下着倾盆大雨——整个效果就会显得假。这篇文章专注于地表响应这一层：雨天的涟漪波纹和湿润感，雪天的顶面积雪覆盖和法线修正。

---

## 雨天地面效果：波纹与湿润度

雨天地面有两个关键视觉特征：涟漪动画和湿润反光。

**湿润度**的表现方式是降低材质的粗糙度（Roughness），提升反射率。水面的 Roughness 接近 0，能清晰反射环境。在 Shader 里引入 `_WetLevel`（0~1）参数，用它插值 Roughness：

```hlsl
// 湿润表面：粗糙度趋近 0，积水区域可略提 Metallic 增强反射感
float wetRoughness = lerp(roughness, 0.05, _WetLevel);
float wetMetallic  = lerp(metallic,  0.02, _WetLevel);
```

**涟漪动画**用多层流动的法线贴图叠加。单层法线会显得太规律，两到三层不同速度、不同缩放的法线相加，效果自然很多。法线图使用标准涟漪贴图（ripple normal map），UV 沿 Y 轴随时间流动，模拟水面向外扩散的雨滴波纹。

---

## 雨滴法线扰动：多层叠加

涟漪本质上是法线扰动，让多层法线图以不同速度在世界空间 UV 上流动：

```hlsl
// 用世界空间 XZ 坐标作为 UV，确保不同网格拼接处涟漪连续
float2 worldUV = positionWS.xz;

// 两层涟漪：速度和缩放不同，避免周期感
float2 rippleUV1 = worldUV * _RippleTiling
                 + float2(0.0, _Time.y * _RippleSpeed);
float2 rippleUV2 = worldUV * _RippleTiling * 1.7
                 + float2(0.31, _Time.y * _RippleSpeed * 0.6);

float3 r1 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleMap, sampler_RippleMap, rippleUV1));
float3 r2 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleMap, sampler_RippleMap, rippleUV2));

// 两层叠加后归一化
float3 rippleNormal = normalize(r1 + r2);

// 用湿润度 * 顶面权重，让涟漪只出现在水平面
float surfaceUp = saturate(normalWS.y);
float3 finalNormalTS = normalize(lerp(originalNormalTS, rippleNormal,
                                      _WetLevel * _RippleStrength * surfaceUp));
```

`surfaceUp` 这一项很关键：竖直的墙面和朝下的表面不会出现涟漪，符合真实物理。

---

## 积雪顶面权重

积雪只会堆积在朝上的表面，用世界空间法线的 Y 分量来判断：

```hlsl
// 法线转到世界空间（顶点阶段处理或 Fragment 还原）
float3 normalWS = normalize(mul(float3x3(tangentWS, bitangentWS, geometricNormalWS),
                                normalTS));

// smoothstep 控制积雪边缘的软硬
// _SnowThreshold：临界角（0.7 ≈ 45°），超过此值开始积雪
// _SnowEdgeSoftness：过渡区宽度
float snowWeight = smoothstep(_SnowThreshold - _SnowEdgeSoftness,
                               _SnowThreshold + _SnowEdgeSoftness,
                               normalWS.y);
```

通过调整 `_SnowThreshold` 可以模拟不同积雪量：刚下雪时阈值高（只有接近水平的面有雪），大雪后阈值低（连较陡的坡面也有积雪）。

---

## 积雪混合与法线修正

有了顶面权重之后，用它混合原始材质和雪的外观：

```hlsl
// Albedo：略带蓝调的雪白色
float3 snowAlbedo = float3(0.90, 0.95, 1.00);
float3 finalAlbedo = lerp(albedo, snowAlbedo, snowWeight);

// 粗糙度：新雪粗糙（0.85），老雪/冰光滑（0.35）
float snowRoughness = lerp(0.85, 0.35, _SnowAge);
float finalRoughness = lerp(roughness, snowRoughness, snowWeight);

// 法线修正：积雪覆盖后原始细节被压平，趋向切线空间的竖直方向 (0,0,1)
float3 snowNormalTS = float3(0.0, 0.0, 1.0);
float3 finalNormalTS = normalize(lerp(sampledNormalTS, snowNormalTS, snowWeight));
```

---

## 完整雨雪双模式地面材质

```hlsl
Shader "Custom/WeatherGround"
{
    Properties
    {
        _BaseMap       ("Base Map",       2D)           = "white" {}
        _NormalMap     ("Normal Map",     2D)           = "bump"  {}
        _RoughnessMap  ("Roughness Map",  2D)           = "white" {}

        [Header(Weather)]
        _WeatherMode   ("Weather Mode (0=Dry 1=Rain 2=Snow)", Float) = 0
        _WetLevel      ("Wet Level",      Range(0,1))   = 0

        [Header(Rain)]
        _RippleMap     ("Ripple Normal Map", 2D)        = "bump"  {}
        _RippleTiling  ("Ripple Tiling",  Float)        = 5.0
        _RippleSpeed   ("Ripple Speed",   Float)        = 0.5
        _RippleStrength("Ripple Strength",Range(0,1))   = 0.5

        [Header(Snow)]
        _SnowThreshold    ("Snow Threshold",    Range(0,1))   = 0.7
        _SnowEdgeSoftness ("Snow Edge Softness",Range(0,0.3)) = 0.1
        _SnowAge          ("Snow Age (0=fresh 1=ice)", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);    SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_RippleMap);    SAMPLER(sampler_RippleMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float  _WeatherMode;
                float  _WetLevel;
                float  _RippleTiling;
                float  _RippleSpeed;
                float  _RippleStrength;
                float  _SnowThreshold;
                float  _SnowEdgeSoftness;
                float  _SnowAge;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   norInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS    = norInputs.normalWS;
                OUT.tangentWS   = norInputs.tangentWS;
                OUT.bitangentWS = norInputs.bitangentWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 基础采样
                float3 albedo    = SAMPLE_TEXTURE2D(_BaseMap,      sampler_BaseMap,      IN.uv).rgb;
                float3 normalTS  = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                float  roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, IN.uv).r;

                // TBN 矩阵，将切线空间法线转到世界空间
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);

                // 世界空间法线（用原始法线计算积雪权重）
                float3 normalWS = normalize(mul(normalTS, TBN));

                // === 雨天模式 ===
                if (_WeatherMode > 0.5 && _WeatherMode < 1.5)
                {
                    float surfaceUp = saturate(normalWS.y);

                    float2 worldUV = IN.positionWS.xz;
                    float2 uv1 = worldUV * _RippleTiling + float2(0.0, _Time.y * _RippleSpeed);
                    float2 uv2 = worldUV * _RippleTiling * 1.7
                               + float2(0.31, _Time.y * _RippleSpeed * 0.6);

                    float3 r1 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleMap, sampler_RippleMap, uv1));
                    float3 r2 = UnpackNormal(SAMPLE_TEXTURE2D(_RippleMap, sampler_RippleMap, uv2));
                    float3 rippleNTS = normalize(r1 + r2);

                    // 混合涟漪法线
                    normalTS  = normalize(lerp(normalTS, rippleNTS,
                                               _WetLevel * _RippleStrength * surfaceUp));
                    // 湿润：粗糙度趋近 0
                    roughness = lerp(roughness, 0.05, _WetLevel * surfaceUp);
                }

                // === 积雪模式 ===
                if (_WeatherMode > 1.5)
                {
                    float snowWeight = smoothstep(_SnowThreshold - _SnowEdgeSoftness,
                                                  _SnowThreshold + _SnowEdgeSoftness,
                                                  normalWS.y);

                    float3 snowAlbedo = float3(0.90, 0.95, 1.00);
                    albedo = lerp(albedo, snowAlbedo, snowWeight);

                    // 法线趋向竖直（切线空间 Z 轴）
                    normalTS  = normalize(lerp(normalTS, float3(0, 0, 1), snowWeight));

                    float snowRough = lerp(0.85, 0.35, _SnowAge);
                    roughness = lerp(roughness, snowRough, snowWeight);
                }

                // 重新计算世界空间法线（应用修改后的切线空间法线）
                normalWS = normalize(mul(normalTS, TBN));

                // 简单 PBR 光照（主光源漫反射）
                Light mainLight = GetMainLight();
                float NdotL     = saturate(dot(normalWS, mainLight.direction));
                float3 color    = albedo * mainLight.color * NdotL;
                color          += albedo * 0.1; // 环境光近似

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
```

---

## 粒子与 Shader 的分工

粒子系统（Particle System）负责视觉上的雨丝和雪花：雨用拉伸的 Billboard 粒子模拟细丝，雪用慢速漂浮的球形粒子。地面 Shader 通过全局 Shader Property 接收当前天气强度，根据参数实时切换外观：

```csharp
public class WeatherController : MonoBehaviour
{
    [SerializeField] ParticleSystem rainParticles;
    [SerializeField] ParticleSystem snowParticles;

    public void SetWeather(float rainLevel, float snowLevel)
    {
        // 控制粒子发射率
        var rainEmission = rainParticles.emission;
        rainEmission.rateOverTime = rainLevel * 200;

        var snowEmission = snowParticles.emission;
        snowEmission.rateOverTime = snowLevel * 50;

        // 驱动地面 Shader 全局参数
        float mode = snowLevel > 0.1f ? 2f : (rainLevel > 0.1f ? 1f : 0f);
        Shader.SetGlobalFloat("_WeatherMode", mode);
        Shader.SetGlobalFloat("_WetLevel", rainLevel);
    }
}
```

两者的协作关键是时机同步：降雨结束后，地面的干燥比粒子消散慢一些——符合真实雨后地面仍然潮湿的体验。`_WetLevel` 建议用 `Mathf.Lerp` 以较慢的速度（约 30~60 秒）归零，而粒子发射率可以立即停止。
