---
title: "Shader 核心技法 04｜描边：顶点外扩法与后处理法"
slug: "shader-technique-04-outline"
date: "2026-03-26"
description: "描边有两条主路线：顶点外扩（背面法线扩展）和后处理（深度/法线边缘检测）。理解两种方法的原理、适用场景和各自的局限，在 URP 里实现可用的描边效果。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "技法"
  - "描边"
  - "Outline"
  - "后处理"
series: "Shader 手写技法"
weight: 4200
---
描边配合卡通渲染是标配。主流方案有两种：在物体层面做（顶点外扩），或在屏幕层面做（后处理边缘检测）。两种方案各有优劣，实际项目里经常组合使用。

---

## 方案一：顶点外扩法（Back-Face Outline）

**原理**：多一个 Pass，只渲染背面（`Cull Front`），在 Vertex Shader 里沿法线方向把顶点往外推一段距离，填充纯色——正面看去，外扩的背面露出来就是描边。

```hlsl
Pass
{
    Name "Outline"
    Tags { "LightMode" = "SRPDefaultUnlit" }
    Cull Front   // 只渲染背面

    HLSLPROGRAM
    #pragma vertex   vert_outline
    #pragma fragment frag_outline
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float  _OutlineWidth;
        float4 _OutlineColor;
    CBUFFER_END

    struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
    struct Varyings   { float4 positionHCS : SV_POSITION; };

    Varyings vert_outline(Attributes input)
    {
        Varyings output;
        // 沿法线方向外扩（物体空间）
        float3 expandedPos = input.positionOS.xyz + input.normalOS * _OutlineWidth * 0.01;
        output.positionHCS = TransformObjectToHClip(expandedPos);
        return output;
    }

    half4 frag_outline(Varyings input) : SV_Target
    {
        return _OutlineColor;
    }
    ENDHLSL
}
```

---

## 外扩在裁剪空间做：屏幕等宽描边

物体空间外扩有个问题：远处的物体描边会变细、近处变粗。改用**裁剪空间**外扩，描边宽度在屏幕上恒定：

```hlsl
Varyings vert_outline(Attributes input)
{
    Varyings output;

    float4 posCS     = TransformObjectToHClip(input.positionOS.xyz);
    float3 normalCS  = mul((float3x3)UNITY_MATRIX_VP,
                           mul((float3x3)UNITY_MATRIX_M, input.normalOS));

    // 裁剪空间里沿法线方向外扩（投影后的法线 XY 分量）
    float2 extendDir = normalize(normalCS.xy);
    posCS.xy += extendDir * _OutlineWidth * 0.01 * posCS.w;  // 乘 w 补偿透视

    output.positionHCS = posCS;
    return output;
}
```

乘以 `posCS.w` 是为了抵消透视除法（透视除法会把近处放大、远处缩小），让描边在屏幕上保持固定宽度。

---

## 顶点外扩法的问题

| 问题 | 原因 | 缓解方案 |
|------|------|---------|
| 硬边接缝（Hard Edge） | 法线不连续时外扩方向突变 | 平滑法线（把邻近顶点法线平均，存入顶点色或 UV1） |
| 凹面穿插 | 外扩后背面穿入正面 | 减小描边宽度 |
| 不适合开放 Mesh | 平面、开放边无法用背面 | 后处理法 |
| 无法处理远景 LOD | 低 LOD 法线变化大 | LOD 切换时关闭描边 |

**平滑法线方案**：在导入时或运行时，把顶点的法线替换为周围顶点法线的平均值，存在 `TEXCOORD1` 或顶点色里，外扩时用平滑后的法线。

---

## 方案二：后处理描边（Renderer Feature）

**原理**：渲染完正常场景后，用深度图和法线图做边缘检测（Sobel/Roberts），有差异的地方就是边缘，叠加描边颜色。

**优点**：适用于所有物体，对 Mesh 拓扑无要求，效果统一。
**缺点**：屏幕空间，无法区分某个物体的描边颜色；有深度边缘误检（远处细节噪点）。

URP 里实现后处理描边需要自定义 Renderer Feature，这里给出核心算法：

```hlsl
// 在后处理 Shader 里：
// _CameraDepthTexture：深度图
// _CameraNormalsTexture：法线图（需开启 DepthNormals PrePass）

float2 texelSize = _MainTex_TexelSize.xy;

// Roberts 边缘检测（对角差分）
float d1 = SampleDepth(uv + float2( 1,  1) * texelSize);
float d2 = SampleDepth(uv + float2(-1, -1) * texelSize);
float d3 = SampleDepth(uv + float2( 1, -1) * texelSize);
float d4 = SampleDepth(uv + float2(-1,  1) * texelSize);

float edgeDepth = sqrt(pow(d1 - d2, 2) + pow(d3 - d4, 2));

// 法线边缘检测
float3 n1 = SampleNormal(uv + float2( 1,  1) * texelSize);
float3 n2 = SampleNormal(uv + float2(-1, -1) * texelSize);
float  edgeNormal = 1.0 - dot(n1, n2);   // 法线差异大 → 边缘

float edge = saturate(edgeDepth * _DepthThreshold + edgeNormal * _NormalThreshold);
return lerp(sceneColor, _OutlineColor, edge);
```

---

## 实际项目常用组合

| 场景 | 推荐方案 |
|------|---------|
| 角色描边（固定颜色，精确轮廓） | 顶点外扩 + 平滑法线 |
| 场景物件全局描边 | 后处理边缘检测 |
| 选中高亮（Stencil） | Stencil 遮罩 + 外扩 |
| 移动端（低性能开销） | 顶点外扩（无额外 RT） |

---

## 基于 Stencil 的选中描边

只对特定物体描边（选中/高亮）：

```hlsl
// 正常渲染 Pass：写 Stencil
Stencil
{
    Ref 1
    Comp Always
    Pass Replace
}

// 描边 Pass：只在 Stencil != 1 的区域绘制（即轮廓外围）
Stencil
{
    Ref 1
    Comp NotEqual
}
Cull Off
// 用外扩后的位置渲染纯色
```

---

## 小结

| 方案 | 优点 | 缺点 |
|------|------|------|
| 顶点外扩 | 简单，性能好，支持单独颜色 | 硬边问题，对拓扑有要求 |
| 后处理 | 全局统一，对 Mesh 无要求 | 无法单独控制颜色，有误检 |
| 平滑法线外扩 | 解决硬边问题 | 需要预处理法线数据 |
| Stencil 描边 | 精确选中高亮 | 需要额外 Pass |

下一篇：透明与半透明——Alpha Blend 的渲染队列、深度写入、Premultiplied Alpha，以及移动端透明物体的性能代价。
