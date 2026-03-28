---
title: "游戏常用效果｜地形细节层：Detail Mesh 与草地的 GPU Instancing 渲染"
slug: "shader-terrain-02-detail-layer"
date: "2026-03-28"
description: "Detail Layer 的渲染机制、GPU Instancing 接入方式，以及如何用顶点动画实现风吹草动与交互弯曲。"
tags: ["Shader", "HLSL", "URP", "地形", "细节层", "GPU Instancing", "草地"]
series: "Shader 手写技法"
weight: 4540
---

Terrain 本体负责大地形的材质混合，但靠近玩家时地表会显得空旷——缺少贴地的小草、苔藓、碎石。这些密集分布的小物件由 Detail Layer 系统负责渲染。Detail Layer 与地形 Mesh 完全解耦，拥有独立的密度图、LOD 衰减和专属 Shader 需求。它的渲染量往往比地形本体还大，所以性能设计是第一优先级。

---

## Detail Layer 是什么

在 Terrain Inspector 的"Paint Details"模式下，每刷一笔就是向密度贴图（Detail Density Map）写入数据。这张贴图以 8 位灰度记录每个地表格子里某种细节的密度——0 表示没有，255 表示最大密度。

运行时，Unity 读取密度图，在每个格子里按密度随机撒点，然后用 `DrawMeshInstanced` 把同一种 Detail Mesh 批量绘制出来。每个实例的位置、旋转、缩放会有随机偏差以打破规律感，这些随机值通过 per-instance 矩阵传递给 Shader。

Terrain 的 `detailObjectDistance` 属性控制细节层的最大可见距离，超过距离后实例数量线性衰减至零，这一衰减在 CPU 侧完成，Shader 不需要处理。

---

## GPU Instancing 的接入

要让 Detail Shader 支持 GPU Instancing，需要做三件事：

1. 在 Pass 顶部加 `#pragma multi_compile_instancing`
2. 在结构体里加 `UNITY_VERTEX_INPUT_INSTANCE_ID`
3. 在顶点函数体里调用 `UNITY_SETUP_INSTANCE_ID`

```hlsl
#pragma multi_compile_instancing

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 color      : COLOR;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 worldPos   : TEXCOORD1;
    float  camDist    : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings DetailVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
    // ... 后续风力动画
    output.positionCS = TransformWorldToHClip(worldPos);
    output.worldPos   = worldPos;
    output.camDist    = length(_WorldSpaceCameraPos.xyz - worldPos);
    output.uv         = input.uv;
    return output;
}
```

---

## 风力顶点动画

草的摆动是 Detail Shader 最核心的视觉特征。原理是用正弦波扰动顶点的 X/Z 坐标，同时用顶点高度（本地空间 Y 值）作为权重——根部固定不动，叶尖摆动幅度最大。

```hlsl
// 高度权重 + 相位差 + 风向控制的草地摆动
float3 ApplyGrassWind(float3 positionOS, float3 worldPos, float time)
{
    // 高度权重：草根处为 0，叶尖处约为 1
    float heightWeight = saturate(positionOS.y / 0.5);

    // 用世界空间坐标引入相位差，避免所有草同步摆动
    float phase = dot(worldPos.xz, float2(0.9, 0.6)) * 1.5;

    // 主摆动方向（沿风向）
    float swingX = sin(time * _WindSpeed + phase) * _WindStrength * heightWeight;
    float swingZ = sin(time * _WindSpeed * 0.7 + phase + 1.2) * _WindStrength * 0.5 * heightWeight;

    worldPos.x += swingX * _WindDirection.x;
    worldPos.z += swingZ * _WindDirection.z;
    return worldPos;
}
```

在顶点着色器里替换 worldPos：

```hlsl
float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
worldPos = ApplyGrassWind(input.positionOS.xyz, worldPos, _Time.y);
output.positionCS = TransformWorldToHClip(worldPos);
```

加入 `_WindDirection` 参数让风向可在 C# 侧统一控制，所有草的摆动方向保持一致。

---

## 交互弯曲

玩家走过草地时，草应该向两侧躲开。在 C# 侧将玩家世界坐标写入全局 Shader 变量，Shader 里计算每根草与玩家的距离，在一定半径内施加额外的顶点偏移：

```csharp
// C# 侧每帧更新
Shader.SetGlobalVector("_InteractionCenter",   player.position);
Shader.SetGlobalFloat("_InteractionRadius",    1.2f);
Shader.SetGlobalFloat("_InteractionStrength",  0.4f);
```

```hlsl
// Shader 顶点函数中（在 worldPos 计算之后）
float3 toGrass   = worldPos - _InteractionCenter.xyz;
float  dist      = length(toGrass.xz);
float  falloff   = 1.0 - saturate(dist / _InteractionRadius);
float  heightW   = saturate(input.positionOS.y / 0.5);

worldPos.xz += normalize(toGrass.xz + 0.001) * falloff * _InteractionStrength * heightW;
```

---

## Alpha Clip 与双面渲染

草叶通常是带透明边缘的贴图，需要 alpha clip 而非半透明混合——半透明排序在大量实例下代价极高，而 clip 只需一条 discard 指令。

```hlsl
// Fragment Shader
half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
clip(col.a - _Cutoff);  // _Cutoff 通常设为 0.5
```

草叶需要双面可见，在 Pass 级别加 `Cull Off` 即可。URP 会通过 `FRONT_FACE_SEMANTIC` 处理背面法线方向，不需要在 Shader 内部手动翻转。

---

## LOD 渐隐

当相机接近 `detailObjectDistance` 上限时，实例密度骤降会产生"草突然消失"感。平滑做法是在距离衰减区间内降低 alpha，让草淡出而不是瞬间消失：

```hlsl
// Fragment Shader 中叠加距离 alpha
float distFade = 1.0 - saturate((input.camDist - _FadeStart) / (_FadeEnd - _FadeStart));
col.a *= distFade;
clip(col.a - 0.01);
```

`_FadeStart` 和 `_FadeEnd` 对应 `Terrain.detailObjectDistance` 的 80%～100% 区间，在 C# 里动态写入：

```csharp
Terrain terrain = GetComponent<Terrain>();
float maxDist = terrain.detailObjectDistance;
Shader.SetGlobalFloat("_FadeStart", maxDist * 0.8f);
Shader.SetGlobalFloat("_FadeEnd",   maxDist);
```

这样在视觉上草会先变稀疏再消失，而不是整块突然弹出。

---

## 完整的细节草 Shader Pass 结构

```hlsl
Pass
{
    Name "UniversalForward"
    Tags { "LightMode" = "UniversalForward" }
    Cull Off
    ZWrite On

    HLSLPROGRAM
    #pragma vertex DetailVert
    #pragma fragment DetailFrag
    #pragma multi_compile_instancing
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
    #pragma multi_compile_fog

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half   _Cutoff;
        float  _WindSpeed;
        float  _WindStrength;
        float4 _WindDirection;
        float  _FadeStart;
        float  _FadeEnd;
    CBUFFER_END

    // 全局交互变量（不放 CBUFFER，由 C# SetGlobal 写入）
    float4 _InteractionCenter;
    float  _InteractionRadius;
    float  _InteractionStrength;

    // ... Vert/Frag 实现
    ENDHLSL
}
```

ShadowCaster Pass 对 Detail Mesh 可选——大量草的阴影在移动端通常会关闭，直接在 Terrain Settings 里禁用细节层阴影即可。
