---
title: "Shader 进阶技法 15｜SDF Height Shadow：角色自定义地面阴影系统"
slug: shader-advanced-15-sdf-shadow
date: "2026-03-28"
description: "卡通游戏里的地面阴影往往不能用实时 Shadow Map，而是用一张 SDF 贴图表达阴影形状，在地面 Shader 里采样并随光照方向偏移。本文从原理到完整 Fragment Shader 代码，讲清这套方案的实现思路。"
tags: ["Shader", "HLSL", "URP", "进阶", "SDF", "阴影", "卡通渲染"]
series: "Shader 手写技法"
weight: 4430
---

卡通渲染对阴影有特殊要求：形状要干净、边缘可控、颜色可调。实时 Shadow Map 生成的阴影天生带锯齿，采样精度受限于 ShadowMap 分辨率，在近处往往一片像素化，和精细的美术风格完全格格不入。更大的问题是它不可控——不同光照角度、不同距离下阴影形状会发生变化，美术无法对它做精确调整。

卡通项目里常见的解法是 Height Shadow + SDF 贴图：美术为每个角色（或角色类型）烘焙一张 SDF 贴图，在地面 Shader 里根据角色位置和光照方向实时采样，得到一个形状固定、边缘可控的柔软阴影。

---

## SDF 是什么

SDF（Signed Distance Field，有向距离场）是一种编码方式：纹理里每个像素存储的不是颜色，而是"当前位置到最近边界的距离"。边界内侧是负值，外侧是正值（或者反过来，取决于约定）。

对于阴影用途，通常简化为无符号版本：像素值越小，越靠近阴影"轮廓"；超过某个阈值就完全在阴影外面。用 SDF 的好处在于可以用一个简单的 `smoothstep` 就得到带软边的轮廓，而且可以任意缩放——缩放 SDF 对应的是把阴影轮廓向内外收缩。

```
shadow = 1 - smoothstep(threshold - softness, threshold + softness, sdf_value)
```

---

## 从世界坐标到 SDF UV

地面 Shader 要知道"当前片元相对于角色的位置"，才能正确采样 SDF。核心步骤：

1. 从 C# 端传入角色的世界坐标 `_CharacterPos`（只用 xz，忽略 y 高度）
2. 计算片元 xz 与角色 xz 的偏移
3. 除以阴影半径，归一化到 [-1, 1]
4. 映射到 [0, 1] 作为 UV

```hlsl
float2 worldToShadowUV(float3 fragWorldPos, float3 charPos, float radius)
{
    float2 offset = fragWorldPos.xz - charPos.xz;
    float2 uv = offset / radius * 0.5 + 0.5;
    return uv;
}
```

---

## 光照方向偏移

真实的阴影会随着太阳角度偏移方向。在 Height Shadow 里，只要把采样 UV 沿主光方向做一个偏移就能实现这个效果。

主光方向 `_MainLightDirection` 是三维向量，阴影在地面上偏移只需要它的 xz 分量（水平投影）：

```hlsl
float2 lightOffset(float3 lightDir, float shadowLength)
{
    // lightDir 通常是从片元指向光源的方向，取反得到阴影延伸方向
    float2 proj = -lightDir.xz;
    // 归一化后乘以偏移强度
    proj = normalize(proj) * shadowLength;
    return proj;
}
```

在采样前把这个偏移加到 UV 上：

```hlsl
float2 uv = worldToShadowUV(fragWorldPos, charPos, _ShadowRadius);
uv += lightOffset(_MainLightDirection, _ShadowLength) / _ShadowRadius * 0.5;
```

---

## 边缘软化

`smoothstep` 接受两个端点，让阴影边缘在这个范围内渐变，而不是硬切。`_ShadowSoftness` 越大，边缘过渡带越宽：

```hlsl
float sdf = SAMPLE_TEXTURE2D(_ShadowSDF, sampler_ShadowSDF, uv).r;
float shadow = 1.0 - smoothstep(
    _ShadowThreshold - _ShadowSoftness,
    _ShadowThreshold + _ShadowSoftness,
    sdf
);
shadow = saturate(shadow);
```

`sdf` 越小表示越靠近角色中心，大于 `_ShadowThreshold` 的部分不在阴影里。

---

## 完整 Fragment Shader（地面侧）

下面是地面材质的完整 Fragment Shader，基于 URP 的 HLSL 结构：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
TEXTURE2D(_ShadowSDF);     SAMPLER(sampler_ShadowSDF);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _BaseColor;
    float4 _ShadowColor;
    float3 _CharacterWorldPos;  // 由 C# 每帧更新
    float  _ShadowRadius;       // SDF 覆盖的世界空间半径
    float  _ShadowLength;       // 光照偏移强度
    float  _ShadowThreshold;    // SDF 阈值（0~1）
    float  _ShadowSoftness;     // 边缘软化范围
    float  _ShadowOpacity;      // 阴影最大不透明度
CBUFFER_END

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
};

Varyings vert(Attributes input)
{
    Varyings output;
    VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    return output;
}

half4 frag(Varyings input) : SV_Target
{
    // 地面基础颜色
    half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;

    // 计算 SDF UV（片元相对于角色的偏移，归一化到 [0,1]）
    float2 offset = input.positionWS.xz - _CharacterWorldPos.xz;
    float2 shadowUV = offset / _ShadowRadius * 0.5 + 0.5;

    // 光照偏移：使用主光方向的 xz 投影
    Light mainLight = GetMainLight();
    float2 lightProj = -mainLight.direction.xz;
    if (length(lightProj) > 0.001)
        lightProj = normalize(lightProj);
    shadowUV += lightProj * (_ShadowLength / _ShadowRadius) * 0.5;

    // 边界检查：UV 超出 [0,1] 则无阴影
    float inBounds = step(0.0, shadowUV.x) * step(shadowUV.x, 1.0)
                   * step(0.0, shadowUV.y) * step(shadowUV.y, 1.0);

    // 采样 SDF 并软化边缘
    float sdf = SAMPLE_TEXTURE2D(_ShadowSDF, sampler_ShadowSDF, shadowUV).r;
    float shadow = 1.0 - smoothstep(
        _ShadowThreshold - _ShadowSoftness,
        _ShadowThreshold + _ShadowSoftness,
        sdf
    );
    shadow = saturate(shadow) * inBounds * _ShadowOpacity;

    // 叠加阴影颜色
    half4 finalColor = lerp(baseColor, baseColor * _ShadowColor, shadow);
    return finalColor;
}
```

---

## C# 端更新角色位置

SDF UV 计算依赖 `_CharacterWorldPos`，需要每帧从 C# 更新到材质：

```csharp
using UnityEngine;

public class GroundShadowUpdater : MonoBehaviour
{
    public Material groundMaterial;
    public Transform character;

    void Update()
    {
        if (groundMaterial != null && character != null)
        {
            groundMaterial.SetVector("_CharacterWorldPos", character.position);
        }
    }
}
```

---

## 多角色与性能考虑

这套方案一个角色对应一个 `_CharacterWorldPos`。多角色场景下有几种扩展方式：

一种是地面材质支持多个 SDF 通道，每个角色对应一组参数，最后把多个 shadow 值取 max 或叠加。另一种是用 Decal 系统：为每个角色创建一个跟随的 Projector/Decal，把 SDF 阴影动态投影到地面，不需要修改地面 Shader。

卡通渲染中 SDF 阴影还有一个进化版本：离线烘焙全角色动画的多帧 SDF 序列，运行时根据动画状态切换，得到和动作精确匹配的阴影形状——代价是内存，适合主角等重要角色。
