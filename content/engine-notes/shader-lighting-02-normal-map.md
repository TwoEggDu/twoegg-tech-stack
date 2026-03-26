+++
title = "Shader 核心光照 02｜法线贴图：TBN 矩阵与切线空间"
slug = "shader-lighting-02-normal-map"
date = 2026-03-26
description = "法线贴图让低面数模型呈现高面数的光照细节。理解切线空间（TBN）的几何含义，在 URP Shader 里正确采样法线贴图并变换到世界空间参与光照计算。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "光照", "法线贴图", "TBN", "切线空间"]
series = ["Shader 手写技法"]
[extra]
weight = 4120
+++

一个低面数的平面，贴上法线贴图后，光照反应和有凹凸的高面数模型几乎一样。法线贴图是游戏里提升视觉细节最高效的手段之一，理解它的原理才能用对。

---

## 法线贴图存的是什么

普通贴图（Albedo）存颜色——每个像素的 RGB 对应物体表面该点的颜色。

法线贴图存的是**每个像素的法线方向**——不是真实的几何法线，而是期望的光照法线。通过改变法线方向，让平坦表面的光照表现得像有凹凸一样。

法线贴图呈现蓝紫色的原因：大多数像素的法线接近"垂直于表面"方向，在切线空间里是 (0, 0, 1)，映射成颜色是 (0.5, 0.5, 1.0)，即蓝紫色。

---

## 切线空间（Tangent Space）

法线贴图里的方向是在**切线空间**里描述的——以表面自身为参考系：

```
T（Tangent）   → 沿 UV 的 U 方向（贴图横向）
B（Bitangent） → 沿 UV 的 V 方向（贴图纵向）
N（Normal）    → 垂直于表面（贴图深度方向）
```

这三个互相垂直的向量构成 **TBN 矩阵**。用它把切线空间的法线方向变换到世界空间，就能参与正常的光照计算。

**为什么要用切线空间而不是直接存世界空间法线？**

- 世界空间法线贴图在物体旋转后就失效了（法线方向绝对固定）
- 切线空间法线贴图跟随物体旋转，并且可以跨不同物体复用同一张贴图

---

## 顶点阶段：构建 TBN

顶点需要额外输入**切线（tangent）**，Unity 在导入 Mesh 时自动计算：

```hlsl
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;   // ← 新增，w 分量存 bitangent 方向符号
    float2 uv         : TEXCOORD0;
};
```

`tangentOS.w` 是 +1 或 -1，用来修正 bitangent 的朝向（镜像 UV 的处理）。

在 Vertex Shader 里用 `GetVertexNormalInputs` 一次性构建 TBN：

```hlsl
VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

output.tangentWS   = normalInputs.tangentWS;
output.bitangentWS = normalInputs.bitangentWS;
output.normalWS    = normalInputs.normalWS;
```

---

## 片元阶段：采样并变换法线

```hlsl
// 1. 采样法线贴图，解码到切线空间
float4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
float3 normalTS = UnpackNormal(normalSample);   // 解码：(0~1) → (-1~1)

// 2. 构建 TBN 矩阵（列向量形式）
float3x3 TBN = float3x3(
    normalize(input.tangentWS),
    normalize(input.bitangentWS),
    normalize(input.normalWS)
);

// 3. 切线空间 → 世界空间
float3 normalWS = normalize(mul(normalTS, TBN));
```

或者用 URP 封装的函数：

```hlsl
float3 normalWS = TransformTangentToWorld(normalTS,
    half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
normalWS = NormalizeNormalPerPixel(normalWS);
```

`NormalizeNormalPerPixel` 是 URP 提供的条件归一化，性能略优于无条件 `normalize`。

---

## 法线贴图强度控制

`UnpackNormal` 解码后的 XY 分量控制法线偏移强度，Z 分量是深度。缩放 XY 可以控制凹凸强度：

```hlsl
float3 normalTS = UnpackNormal(normalSample);
normalTS.xy *= _NormalScale;   // _NormalScale = 0 时完全平滑，1 时原始强度，>1 时增强
normalTS.z = sqrt(1.0 - saturate(dot(normalTS.xy, normalTS.xy)));  // 重新计算 z 保证单位长度
```

URP 提供了封装函数：

```hlsl
float3 normalTS = UnpackNormalScale(normalSample, _NormalScale);
```

---

## 完整 Shader 片段（增量修改）

在 Blinn-Phong Shader 基础上加入法线贴图：

```hlsl
Properties
{
    // ... 原有属性 ...
    _NormalMap   ("Normal Map",    2D)    = "bump" {}   // "bump" 是 Unity 的平法线默认值
    _NormalScale ("Normal Scale",  Float) = 1.0
}

// CBUFFER 里加：
float  _NormalScale;
float4 _NormalMap_ST;

// 贴图声明：
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

// Attributes 加切线：
float4 tangentOS : TANGENT;

// Varyings 加 TBN 向量（传给片元）：
float3 tangentWS   : TEXCOORD3;
float3 bitangentWS : TEXCOORD4;
// normalWS 已有，TEXCOORD0

// Vertex Shader 里：
VertexNormalInputs ni = GetVertexNormalInputs(input.normalOS, input.tangentOS);
output.tangentWS   = ni.tangentWS;
output.bitangentWS = ni.bitangentWS;
output.normalWS    = ni.normalWS;

// Fragment Shader 里，替换法线读取：
float3 normalTS = UnpackNormalScale(
    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
float3 normalWS = TransformTangentToWorld(normalTS,
    half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
normalWS = NormalizeNormalPerPixel(normalWS);
// 之后 normalWS 参与 NdotL、NdotH 计算，和之前一样
```

---

## 常见问题

**Q：法线贴图方向反了（凹变成凸）**

两种可能：
1. 贴图导入设置里 Y 方向没有勾选 `Flip Green Channel`（OpenGL/DirectX 约定不同）
2. UV 展开时有镜像，`tangentOS.w` 的符号不对

在 Inspector 里勾选/取消 `Flip Green Channel` 对比效果。

**Q：法线贴图无效（和不加一样）**

检查 `tangentOS` 是否在 `Attributes` 里声明，以及 Mesh 是否有切线数据（`Import Settings → Tangents → Calculate`）。

**Q：用了法线贴图后接缝处有硬边**

切线空间在 UV 接缝处不连续，属于正常现象。减少 UV 接缝数量，或确保建模时 UV 岛展开合理。

**Q：法线强度调到 0 后还是有轻微凹凸**

`UnpackNormalScale(tex, 0)` 会返回 (0, 0, 1)——即平法线，不应有凹凸。如果还有，检查是否有其他法线相关的计算路径。

---

## 性能注意

- 法线贴图需要顶点额外传 `tangent`（每顶点多 16 字节）
- Fragment Shader 多一次纹理采样 + TBN 矩阵乘法
- 移动端中低档可以考虑通过 `shader_feature_local _NORMALMAP` 做成可开关的变体

```hlsl
#pragma shader_feature_local _NORMALMAP

// Fragment Shader 里：
#ifdef _NORMALMAP
    float3 normalTS = UnpackNormalScale(..., _NormalScale);
    normalWS = TransformTangentToWorld(normalTS, half3x3(T, B, N));
    normalWS = NormalizeNormalPerPixel(normalWS);
#else
    normalWS = normalize(input.normalWS);
#endif
```

---

## 小结

| 概念 | 要点 |
|------|------|
| 法线贴图 | 存切线空间法线方向，改变光照细节而非几何 |
| 切线空间 | T（U方向）、B（V方向）、N（表面法线）三轴坐标系 |
| TBN 矩阵 | 把切线空间方向变换到世界空间 |
| `UnpackNormal` | 解码贴图 (0~1) → 法线 (-1~1) |
| `_NormalScale` | 控制凹凸强度，用 `UnpackNormalScale` |
| 切线输入 | `float4 tangentOS : TANGENT`，w 存符号 |

下一篇：阴影接收——ShadowCaster Pass、shadowCoord、软阴影关键字。
