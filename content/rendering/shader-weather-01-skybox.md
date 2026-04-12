---
title: "游戏常用效果｜天空盒与大气散射：Rayleigh/Mie 散射与程序化天空实现"
slug: shader-weather-01-skybox
date: "2026-03-28"
description: "从物理原理出发，用 HLSL 实现程序化天空盒——Rayleigh 散射模拟蓝天与红霞，Mie 散射模拟太阳日晕，配合 URP 环境光联动。"
tags: ["Shader", "HLSL", "URP", "天气", "天空盒", "大气散射", "Rayleigh", "Mie"]
series: "Shader 手写技法"
weight: 4580
---

真实的天空颜色不是贴图贴出来的，是大气对光线散射的结果。正午的天空是蓝色，傍晚变成橙红，日出前的天边是深紫——这些都来自同一套物理过程。理解散射原理之后，用几十行 HLSL 就能在游戏里还原这些现象，而且是实时的、随太阳方向动态变化的。

---

## 静态 Cubemap vs 程序化天空

**静态 Cubemap** 是最省事的做法：美术渲染或拍摄一张 HDR 全景图，展开成六面体贴图，引擎采样视线方向对应的像素。缺点明显——贴图是死的，无法随昼夜变化，换时段就得换贴图，内存压力也大（一张 4K HDR Cubemap 约 200MB）。

**程序化天空** 在 Fragment Shader 里实时计算每个像素的颜色。输入只有视线方向和太阳方向，输出天空颜色。性能开销比贴图采样大，但完全动态，一套 Shader 能处理从黎明到夜晚的全部过渡，是开放世界项目的标配。

Unity 内置的 Procedural Skybox 就是程序化方案，但可配置项有限，手写版可以更精确地控制散射参数。

---

## 大气散射的物理基础

太阳光射入大气层时，会被空气分子和微粒散射。散射行为分两类：

**Rayleigh 散射**：光与比波长小得多的分子（氮气、氧气）碰撞，散射强度与波长四次方成反比（`I ∝ 1/λ⁴`）。波长短的蓝光（450nm）散射强度是红光（700nm）的约 5.5 倍，所以天空是蓝的。当视线接近地平线、光程变长，蓝光被散射殆尽，剩下红橙光——这是日出日落的成因。

**Mie 散射**：光与和波长尺寸相近的微粒（尘埃、水汽）碰撞，散射强度对波长不敏感，结果是白色。集中在太阳周围形成白色光晕（日晕），雾天和阴天的天空偏白也是 Mie 散射主导。

---

## 程序化天空的参数体系

```hlsl
// Shader Properties
float3 _SunDirection;       // 太阳方向（世界空间，归一化）
float  _AtmosphereDensity;  // 大气密度，控制整体散射强度
float3 _RayleighCoeff;      // Rayleigh 散射系数，通常 (5.8, 13.5, 33.1) * 1e-6
float  _MieCoeff;           // Mie 散射系数，控制日晕强度
float  _MieG;               // Mie 不对称因子，[-1,1]，正值前向散射（更亮的日晕）
float  _SunIntensity;       // 太阳光强
float3 _NightColor;         // 夜晚天空底色
```

---

## 简化版 Rayleigh 散射实现

完整的大气散射需要 Ray Marching 沿视线积分，移动端开销太高。下面是经典的单次散射近似，精度够用、性能友好：

```hlsl
Shader "Custom/ProceduralSkybox"
{
    Properties
    {
        _SunDirection     ("Sun Direction",      Vector) = (0.3, 0.8, 0.5, 0)
        _SunIntensity     ("Sun Intensity",      Float)  = 10.0
        _AtmosphereDensity("Atmosphere Density", Float)  = 1.0
        _MieG             ("Mie G",              Float)  = 0.76
        _MieCoeff         ("Mie Coeff",          Float)  = 0.05
        _NightColor       ("Night Color",        Color)  = (0.01, 0.01, 0.05, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir   : TEXCOORD0;
            };

            float4 _SunDirection;
            float  _SunIntensity;
            float  _AtmosphereDensity;
            float  _MieG;
            float  _MieCoeff;
            float4 _NightColor;

            // Rayleigh 散射系数（对应 RGB 三个波长）
            static const float3 kRayleigh = float3(5.8e-3, 13.5e-3, 33.1e-3);

            // Henyey-Greenstein Mie 相位函数
            float HGPhase(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5) * 4.0 * PI);
            }

            // Rayleigh 相位函数
            float RayleighPhase(float cosTheta)
            {
                return (3.0 / (16.0 * PI)) * (1.0 + cosTheta * cosTheta);
            }

            float3 ComputeSkyColor(float3 viewDir, float3 sunDir)
            {
                float cosTheta = dot(viewDir, sunDir);

                // 大气路径长度近似：视线越平，光程越长
                float altitude  = max(viewDir.y, 0.0001);
                float pathLen   = _AtmosphereDensity / altitude;

                // Rayleigh 散射
                float3 rayleighExt   = kRayleigh * pathLen;
                float3 transmittance = exp(-rayleighExt);
                float3 rayleighColor = kRayleigh * RayleighPhase(cosTheta) * (1.0 - transmittance);

                // Mie 散射（日晕白光）
                float  mieExt   = _MieCoeff * pathLen;
                float3 mieColor = float3(1, 1, 1) * _MieCoeff * HGPhase(cosTheta, _MieG)
                                  * (1.0 - exp(-mieExt));

                return (rayleighColor + mieColor) * _SunIntensity;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldDir   = normalize(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(IN.worldDir);
                float3 sunDir  = normalize(_SunDirection.xyz);

                // 基础天空散射颜色
                float3 color = ComputeSkyColor(viewDir, sunDir);

                // 地平线渐变：y 分量指数衰减，混入偏暖的地平线色
                float  horizonBlend = exp(-max(viewDir.y, 0.0) * 6.0);
                float3 horizonColor = float3(0.8, 0.7, 0.6) * _SunIntensity * 0.2;
                color = lerp(color, horizonColor, horizonBlend * 0.5);

                // 太阳盘：dot 高次幂
                float cosToSun = dot(viewDir, sunDir);
                float sunDisk  = pow(max(cosToSun, 0.0), 1500.0);
                float sunGlow  = pow(max(cosToSun, 0.0), 200.0) * 0.3;
                color += float3(1.0, 0.95, 0.8) * _SunIntensity * (sunDisk + sunGlow);

                // 夜晚底色
                float nightBlend = saturate(-sunDir.y * 2.0);
                color = lerp(color, _NightColor.rgb, nightBlend);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
```

---

## 地平线渐变原理

`exp(-viewDir.y * k)` 这一行是地平线渐变的核心。当 `viewDir.y` 趋近于 0（地平线方向），指数值趋近于 1，地平线颜色全量混入；当 `viewDir.y` 接近 1（正上方），指数值趋近于 0，天顶颜色不受影响。参数 `k` 越大，过渡区间越窄，地平线颜色只影响极低仰角的区域；`k` 越小，效果越扩散，适合模拟高湿度大气。

日落效果可以将 `horizonColor` 替换为动态颜色，根据太阳仰角（`sunDir.y`）在蓝白色和橙红色之间插值：

```hlsl
float sunElevation = saturate(sunDir.y);
float3 horizonColor = lerp(float3(1.0, 0.3, 0.05), float3(0.8, 0.9, 1.0), sunElevation);
```

---

## 与 URP 集成：环境光联动

程序化天空盒的最大价值是让环境光随太阳方向实时变化。在 URP 中：

1. 创建 Material，Shader 指向自定义的程序化天空 Shader
2. 在 **Lighting Settings** 的 Environment 面板，把 Skybox Material 指向这个 Material
3. 把 **Ambient Mode** 设为 Realtime，或者每帧调用 `DynamicGI.UpdateEnvironment()`
4. 用 C# 脚本根据游戏时间更新 `_SunDirection`：

```csharp
public class SkyController : MonoBehaviour
{
    [SerializeField] Material skyMaterial;
    [SerializeField] Light    sunLight;

    void Update()
    {
        Vector3 sunDir = -sunLight.transform.forward;
        skyMaterial.SetVector("_SunDirection", sunDir);

        // 太阳颜色随仰角变化：低仰角偏红
        float elevation = Vector3.Dot(sunDir, Vector3.up);
        Color sunColor  = Color.Lerp(new Color(1f, 0.4f, 0.1f), Color.white, Mathf.Clamp01(elevation));
        sunLight.color  = sunColor;

        // 触发环境光更新（有性能开销，可改为每秒一次）
        DynamicGI.UpdateEnvironment();
    }
}
```

这样一来，从黎明到黄昏，天空颜色、太阳颜色、环境光方向和颜色全部联动，不需要任何额外的贴图资产。`_AtmosphereDensity` 调高模拟阴霾天气，`_MieCoeff` 调高模拟大雾，`_MieG` 调高让日晕更集中——参数和现实大气现象一一对应，调起来也比 Cubemap 替换直观得多。
