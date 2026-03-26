---
title: "Shader 核心光照 01｜Blinn-Phong 高光：镜面反射与半角向量"
slug: "shader-lighting-01-blinn-phong"
date: "2026-03-26"
description: "在 Lambert 漫反射基础上加上镜面高光。理解 Phong 模型的反射向量和 Blinn-Phong 的半角向量优化，写出可控高光大小和强度的完整光照 Shader。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "光照"
  - "Blinn-Phong"
  - "高光"
series: "Shader 手写技法"
weight: 4110
---
Lambert 漫反射让物体有了立体感，但表面是完全哑光的——金属、塑料、湿润表面都有高光（Specular）。这篇在漫反射基础上加上镜面高光，完成 Blinn-Phong 光照模型。

---

## Phong 高光：反射向量

经典 Phong 模型的高光公式：

```
specular = pow(max(0, dot(R, V)), shininess)
```

- **R**：光线在表面的反射方向（`reflect(-L, N)`）
- **V**：从表面指向摄像机的方向（View Direction）
- **shininess**：光泽度，值越大高光越集中

当视线方向 V 和反射方向 R 重合时，`dot(R, V) = 1`，高光最强；偏离时快速衰减。`pow` 控制衰减速度——shininess 越大，高光斑越小越亮。

---

## Blinn-Phong：用半角向量优化

Blinn-Phong 是 Phong 的改进版，用**半角向量 H**替代反射向量 R：

```
H = normalize(L + V)   // L 和 V 的中间方向
specular = pow(max(0, dot(N, H)), shininess)
```

为什么用 H 而不是 R？

1. **计算更便宜**：`reflect` 需要 dot + 两次乘法；`H = normalize(L+V)` 一次加法 + 归一化
2. **效果更自然**：Blinn-Phong 在掠射角（视线几乎平行表面）时不会出现 Phong 的高光截断问题
3. **主流标准**：OpenGL/DirectX 规范光照模型、游戏引擎 Legacy 管线都用 Blinn-Phong

---

## 完整 Shader：漫反射 + Blinn-Phong 高光

```hlsl
Shader "Custom/BlinnPhong"
{
    Properties
    {
        _BaseColor    ("Base Color",     Color)  = (1, 1, 1, 1)
        _BaseMap      ("Base Map",       2D)     = "white" {}
        _SpecColor    ("Specular Color", Color)  = (1, 1, 1, 1)
        _Shininess    ("Shininess",      Float)  = 32.0
        _SpecStrength ("Spec Strength",  Float)  = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

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
                float4 _SpecColor;
                float  _Shininess;
                float  _SpecStrength;
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
                float3 positionWS  : TEXCOORD1;   // ← 新增：世界空间位置（算 V 用）
                float2 uv          : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS  = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS    = TransformObjectToWorldNormal(input.normalOS);
                output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 法线归一化
                float3 normalWS = normalize(input.normalWS);

                // 视线方向（从表面指向摄像机）
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // 主光源
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                // ── 漫反射（Lambert）─────────────────────────────────
                half NdotL   = saturate(dot(normalWS, lightDir));
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 diffuse = albedo.rgb * mainLight.color * NdotL;

                // ── 镜面高光（Blinn-Phong）───────────────────────────
                float3 halfDir = normalize(lightDir + viewDirWS);   // 半角向量 H
                half   NdotH   = saturate(dot(normalWS, halfDir));
                half   spec    = pow(NdotH, _Shininess) * _SpecStrength;
                half3  specular = _SpecColor.rgb * mainLight.color * spec;

                // ── 合并输出 ─────────────────────────────────────────
                half3 color = diffuse + specular;
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }
}
```

---

## 参数调节指南

| 参数 | 范围 | 效果 |
|------|------|------|
| `_Shininess` | 1~512 | 小→大光斑，大→小而亮的光斑 |
| `_SpecStrength` | 0~2 | 高光整体强度 |
| `_SpecColor` | 颜色 | 白色→中性高光，金色→黄铜感，彩色→卡通风 |

金属：`_Shininess = 128~512`，`_SpecColor` 接近 `_BaseColor`
塑料：`_Shininess = 32~64`，`_SpecColor = (1,1,1)`
皮肤：`_Shininess = 8~16`，`_SpecStrength` 很低

---

## 能量守恒问题

Blinn-Phong 不是物理正确的——漫反射 + 高光加起来可能超过 1，相当于表面"产生了能量"。

简单处理方法：用高光强度压低漫反射：

```hlsl
half3 diffuse  = albedo.rgb * mainLight.color * NdotL * (1.0 - spec * _SpecStrength);
half3 specular = _SpecColor.rgb * mainLight.color * spec;
```

这是近似处理，不精确，但视觉上更合理。PBR 材质（下下篇）从根本上解决了能量守恒问题。

---

## GetWorldSpaceViewDir

URP 内置函数，从世界空间位置计算视线方向（已归一化）：

```hlsl
float3 viewDir = GetWorldSpaceViewDir(positionWS);
// 等价于：normalize(_WorldSpaceCameraPos.xyz - positionWS)
```

不需要手动传 `_WorldSpaceCameraPos`，URP 已经通过 `Core.hlsl` 提供。

---

## 小结

| 概念 | 要点 |
|------|------|
| Phong 高光 | `pow(dot(R, V), shininess)`，R = reflect(-L, N) |
| Blinn-Phong | `pow(dot(N, H), shininess)`，H = normalize(L+V)，更高效更自然 |
| Shininess | 值越大，高光斑越小越亮 |
| ViewDir | `GetWorldSpaceViewDir(positionWS)` |
| 能量守恒 | Blinn-Phong 不守恒，PBR 才真正解决 |

下一篇：法线贴图——用贴图存储表面细节，TBN 矩阵把切线空间法线变换到世界空间。
