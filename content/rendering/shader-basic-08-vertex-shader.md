---
title: "Shader 语法基础 08｜Vertex Shader 完整写法：输入结构、变换链与输出插值"
slug: "shader-basic-08-vertex-shader"
date: "2026-03-28"
description: "详解 URP Vertex Shader 的完整写法，包括 Attributes/Varyings 结构体语义、Object→World→Clip 变换链、URP include 体系、TRANSFORM_TEX 宏的工作原理，以及含法线的完整顶点着色器代码。"
tags: ["Shader", "Vertex Shader", "URP", "Unity", "HLSL"]
series: "Shader 手写技法"
weight: 4120
---

Vertex Shader 是 GPU 管线的第一个可编程阶段，每个顶点执行一次。它的核心职责只有一件事：把顶点从模型空间变换到裁剪空间，输出 `SV_POSITION`。其余输出字段（UV、法线、世界坐标等）会被光栅化阶段插值后传给 Fragment Shader。理解这个分工，就理解了 Vertex Shader 的边界。

---

## Attributes：从 Mesh 读取的输入

`Attributes` 结构体描述 GPU 从 Mesh 的顶点缓冲区里读取哪些数据，每个字段后面的语义（Semantic）告诉编译器这个字段对应的是哪种顶点属性。

```hlsl
struct Attributes
{
    float4 positionOS : POSITION;   // 模型空间顶点坐标
    float3 normalOS   : NORMAL;     // 模型空间法线
    float4 tangentOS  : TANGENT;    // 切线（w 分量是手性标志，±1）
    float2 uv0        : TEXCOORD0;  // 第一套 UV
    float2 uv1        : TEXCOORD1;  // 第二套 UV（Lightmap 常用）
    float4 color      : COLOR;      // 顶点色
};
```

语义的含义：
- `POSITION`：顶点在模型空间的局部坐标，xyz 是坐标，w 通常是 1
- `NORMAL`：单位法线向量，仅 xyz，方向信息
- `TANGENT`：切线向量，w 分量存储切线空间的手性（bitangent = cross(normal, tangent) * tangent.w）
- `TEXCOORD0~7`：UV 通道，最多 8 套，类型可以是 float2/float3/float4
- `COLOR`：顶点颜色，RGBA，范围 [0, 1]

后缀 `OS` 是 Object Space 的约定命名，URP 的 ShaderLibrary 全面采用这套命名约定（OS / WS / VS / CS 分别对应 Object / World / View / Clip Space），建议跟进。

---

## Varyings：传递给 Fragment Shader 的插值数据

`Varyings` 结构体的字段会被光栅化阶段在三角形内部插值。`SV_POSITION` 是唯一的强制字段，其他字段可以按需定义：

```hlsl
struct Varyings
{
    float4 positionCS  : SV_POSITION; // 裁剪空间坐标，必须有
    float2 uv          : TEXCOORD0;   // 插值 UV
    float3 normalWS    : TEXCOORD1;   // 世界空间法线（插值后需 normalize）
    float3 positionWS  : TEXCOORD2;   // 世界空间坐标（用于光照计算）
};
```

`SV_POSITION` 必须是裁剪空间（Clip Space）坐标，因为光栅化器需要用它做透视除法和视口变换。你不能往里面写世界坐标然后期望画面正确。

其余语义名（TEXCOORD0~N）只是编译器标识插值寄存器的方式，对 GPU 没有语义含义——叫 `TEXCOORD1` 的字段放法线完全合法。

---

## 变换链：从模型空间到裁剪空间

顶点变换经历四个坐标空间：

```
Object Space → World Space → View Space → Clip Space
```

URP 的 `Core.hlsl` 提供了封装好的变换函数，不需要手动乘矩阵：

```hlsl
// Object Space → World Space
float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

// World Space → Clip Space（合并了 View 变换）
float4 positionCS = TransformWorldToHClip(positionWS);

// 也可以一步到位（Object Space → Clip Space）
float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);

// 法线变换（Object Space → World Space，处理了非均匀缩放）
float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
```

`TransformObjectToWorldNormal` 内部使用的是法线矩阵（世界矩阵的逆转置），而不是直接乘 Model 矩阵。这一点很重要：如果物体有非均匀缩放，直接乘 Model 矩阵会导致法线方向错误。法线必须走专用函数。

---

## URP 的 Include 体系

使用这些变换函数的前提是 include 正确的头文件。URP 的入口头文件是：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
```

这一个 include 会链式引入：
- `SpaceTransforms.hlsl`：所有 Transform* 函数
- `Common.hlsl`：基础数学工具
- `Macros.hlsl`：TEXTURE2D、SAMPLER、CBUFFER_START 等宏
- Unity 内置矩阵 uniform（`unity_ObjectToWorld`、`unity_MatrixVP` 等）

如果还需要光照，则加：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
```

不要 include BRP 的头文件（`UnityCG.cginc` 等），混用会导致编译错误或宏冲突。

---

## TRANSFORM_TEX：处理 Tiling 和 Offset

材质面板上的 Tiling 和 Offset 参数存储在 `_TextureName_ST` 变量里（Unity 自动派生），格式是 `float4`，xy 是 tiling，zw 是 offset。

`TRANSFORM_TEX` 宏把这个变换一步应用到 UV 上：

```hlsl
// 宏展开后等价于：uv * _BaseMap_ST.xy + _BaseMap_ST.zw
OUT.uv = TRANSFORM_TEX(IN.uv0, _BaseMap);
```

注意：`_BaseMap_ST` 必须在 `CBUFFER_START(UnityPerMaterial)` 块里声明，否则 GPU Instancing 和 SRP Batcher 会失效：

```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
CBUFFER_END
```

`_BaseMap` 本身（贴图句柄）不放进 CBUFFER，只有它的 `_ST` 配套变量放进去。

---

## 完整的 Vertex Shader 代码

把以上所有部分组合成一个包含 position、uv、法线的完整顶点着色器：

```hlsl
Shader "Custom/VertexShaderDemo"
{
    Properties
    {
        _BaseMap   ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 坐标变换
                float3 positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS     = TransformWorldToHClip(positionWS);
                OUT.positionWS     = positionWS;

                // 法线变换（正确处理非均匀缩放）
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // UV tiling/offset
                OUT.uv = TRANSFORM_TEX(IN.uv0, _BaseMap);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // normalize 插值后的法线（插值会让长度偏离 1）
                float3 normalWS = normalize(IN.normalWS);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}
```

这个 Shader 已经把 world-space 法线和坐标传给了 Fragment Shader，下一篇会展开如何在 Fragment 阶段用这些数据做采样、alpha clip 和简单的光照计算。
