---
title: "游戏常用效果｜Terrain 渲染原理：Heightmap、SplatMap 与多层纹理混合"
slug: "shader-terrain-01-heightmap"
date: "2026-03-28"
description: "从 Grid Mesh 到 SplatMap，拆解 Unity Terrain 的完整渲染管线，以及如何用高度混合让层间过渡自然。"
tags: ["Shader", "HLSL", "URP", "地形", "Terrain", "Heightmap", "SplatMap"]
series: "Shader 手写技法"
weight: 4530
---

做开放世界或大场景项目，地形渲染几乎是绕不过去的基础课题。Unity 的 Terrain 系统看起来"开箱即用"，但只要涉及定制材质、特殊效果或性能优化，就必须搞清楚它的渲染原理。这篇文章从底层开始拆解：地形网格是怎么生成的、高度图如何驱动顶点、SplatMap 怎么控制多层纹理，以及高度混合为什么能让层间过渡更自然。

---

## Grid Mesh 与 Heightmap 的关系

Unity Terrain 在 CPU 侧生成一张规则的矩形网格（Grid Mesh）。网格的 XZ 坐标均匀分布，Y 坐标默认为 0。真正的地形起伏来自 **Heightmap**——一张 R16 格式的灰度贴图，存储归一化高度值 `[0, 1]`。

渲染时，顶点着色器从 Heightmap 中采样对应 UV 处的高度值，乘以地形最大高度（`_TerrainHeightmapScale.y`），叠加到顶点 Y 坐标上。这一步在 Unity 内置地形 Shader 里由引擎内部处理，但了解它之后，就能明白为什么修改地形高度会触发网格重建：碰撞体需要更新，而顶点数据本身是在 Shader 里实时算出来的。

```hlsl
// 从 Heightmap 重建顶点高度（自定义地形 Shader 中手动实现）
float height = SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, sampler_TerrainHeightmapTexture,
                                    input.uv, 0).r;
float3 worldPos = float3(
    input.positionOS.x * _TerrainSize.x,
    height * _TerrainHeightmapScale.y,
    input.positionOS.z * _TerrainSize.z
);
```

法线同样从 Heightmap 派生——相邻像素的高度差决定坡度。Terrain 系统在每次编辑后烘焙一张 Normal Map 并作为 `_TerrainNormalmapTexture` 提供给 Shader，运行时不需要再做有限差分。如果要在自定义 Shader 里实时重建法线（用于特殊效果），可以采样相邻高度：

```hlsl
// 有限差分重建世界空间法线（适合特效需求，性能有代价）
float2 ts = _TerrainHeightmapTexture_TexelSize.xy;
float hL = SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, sampler_TerrainHeightmapTexture, uv + float2(-ts.x, 0), 0).r;
float hR = SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, sampler_TerrainHeightmapTexture, uv + float2( ts.x, 0), 0).r;
float hD = SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, sampler_TerrainHeightmapTexture, uv + float2(0, -ts.y), 0).r;
float hU = SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, sampler_TerrainHeightmapTexture, uv + float2(0,  ts.y), 0).r;

float scale = _TerrainHeightmapScale.y;
float3 normal = normalize(float3((hL - hR) * scale, 2.0, (hD - hU) * scale));
```

---

## SplatMap：四通道权重图

地形表面要混合多种材质（草地、泥土、岩石、雪地……），Unity 的方案是 **SplatMap**（控制贴图）。这是一张 RGBA 贴图，每个通道存储一层纹理的混合权重：

- R 通道 → 第 0 层（`_Splat0`）的权重
- G 通道 → 第 1 层（`_Splat1`）的权重
- B 通道 → 第 2 层（`_Splat2`）的权重
- A 通道 → 第 3 层（`_Splat3`）的权重

RGBA 四个分量之和归一化为 1.0，超过 4 层时需要第二张 SplatMap（`_Control1`）。自定义 Shader 必须沿用 Unity 的变量命名约定，否则地形系统无法自动赋值：

```hlsl
// SplatMap 变量命名约定（Unity Terrain 强制要求）
TEXTURE2D(_Control);    SAMPLER(sampler_Control);   // 权重贴图（第 1 张，4 层）
TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);    // 第 0 层 Albedo
TEXTURE2D(_Splat1);     SAMPLER(sampler_Splat1);
TEXTURE2D(_Splat2);     SAMPLER(sampler_Splat2);
TEXTURE2D(_Splat3);     SAMPLER(sampler_Splat3);
TEXTURE2D(_Normal0);    SAMPLER(sampler_Normal0);   // 第 0 层 Normal（可选）
TEXTURE2D(_Normal1);    SAMPLER(sampler_Normal1);
TEXTURE2D(_Normal2);    SAMPLER(sampler_Normal2);
TEXTURE2D(_Normal3);    SAMPLER(sampler_Normal3);

float4 _Splat0_ST;  // 各层 tiling & offset
float4 _Splat1_ST;
float4 _Splat2_ST;
float4 _Splat3_ST;
```

---

## 多层纹理混合：加权叠加与归一化

基础的混合公式是加权平均。从 `_Control` 读出四个权重，分别乘以各层采样颜色，求和：

```hlsl
// Fragment Shader - 4 层 SplatMap 基础混合
half4 TerrainFrag(Varyings input) : SV_Target
{
    // 读取混合权重
    half4 splat = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.splatUV);

    // 归一化（确保权重之和为 1，防止亮度异常）
    half weightSum = splat.r + splat.g + splat.b + splat.a;
    splat /= (weightSum + 1e-5);

    // 各层使用独立的 Tiling UV
    float2 uv0 = input.uv * _Splat0_ST.xy + _Splat0_ST.zw;
    float2 uv1 = input.uv * _Splat1_ST.xy + _Splat1_ST.zw;
    float2 uv2 = input.uv * _Splat2_ST.xy + _Splat2_ST.zw;
    float2 uv3 = input.uv * _Splat3_ST.xy + _Splat3_ST.zw;

    // 采样各层 Albedo 并加权混合
    half4 col0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uv0);
    half4 col1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat1, uv1);
    half4 col2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat2, uv2);
    half4 col3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat3, uv3);

    half3 albedo = col0.rgb * splat.r
                 + col1.rgb * splat.g
                 + col2.rgb * splat.b
                 + col3.rgb * splat.a;

    // 法线混合后必须归一化，否则两层各占 50% 时法线模长变短导致高光变暗
    half3 n0 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uv0));
    half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal1, sampler_Normal1, uv1));
    half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal2, sampler_Normal2, uv2));
    half3 n3 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal3, sampler_Normal3, uv3));

    half3 blendedNormal = normalize(
        n0 * splat.r + n1 * splat.g + n2 * splat.b + n3 * splat.a
    );

    // 接入 URP PBR 光照
    InputData lightingInput  = (InputData)0;
    lightingInput.normalWS   = TransformTangentToWorld(blendedNormal, input.tangentToWorld);
    lightingInput.positionWS = input.positionWS;
    lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    SurfaceData surface  = (SurfaceData)0;
    surface.albedo       = albedo;
    surface.smoothness   = col0.a * splat.r + col1.a * splat.g
                         + col2.a * splat.b + col3.a * splat.a; // Alpha 复用为 smoothness
    surface.occlusion    = 1.0;

    return UniversalFragmentPBR(lightingInput, surface);
}
```

---

## 高度混合：让层间过渡更自然

线性加权混合在过渡区域会产生"泥糊"效果，因为两层纹理机械地各出 50% 透明度叠在一起。**高度混合（Height-based Blending）** 用各层自带的高度图来影响权重，使地表"凸起"部分的层压过"低洼"部分的层。

```hlsl
// 高度混合权重计算
// heightValues: 各层高度图采样值（通常存在 Splat 贴图 A 通道）
// splatWeights: 原始 SplatMap 线性权重
half4 HeightBlend(half4 heightValues, half4 splatWeights, half blendSharpness)
{
    // 高度值叠加原始权重，让高度高的层优先
    half4 combined = heightValues * splatWeights;

    // 只保留接近最大值的层，抑制次要层
    half maxVal   = max(max(combined.r, combined.g), max(combined.b, combined.a));
    half threshold = maxVal - (1.0 - blendSharpness);
    half4 result  = max(combined - threshold, 0.0);

    // 归一化
    half sum = result.r + result.g + result.b + result.a;
    return result / (sum + 1e-5);
}
```

在 Fragment Shader 中替换线性权重：

```hlsl
// 读取各层高度值（Splat 贴图 Alpha 通道，或单独的高度图）
half4 heights = half4(col0.a, col1.a, col2.a, col3.a);

// 用高度混合替换线性权重
half4 blendWeights = HeightBlend(heights, splat, _BlendSharpness);

// 后续用 blendWeights 代替 splat 做加权叠加
half3 albedo = col0.rgb * blendWeights.r
             + col1.rgb * blendWeights.g
             + col2.rgb * blendWeights.b
             + col3.rgb * blendWeights.a;
```

`_BlendSharpness` 建议范围 `[0, 1]`：0 等价于纯线性混合，接近 1 时过渡极窄，草地和泥土之间会有明显的犬牙交错边界，适合模拟岩石嵌入草地的效果。实际项目中通常取 `0.5～0.8`，在自然感和视觉层次之间取得平衡。

高度混合的代价是每层多一次 Alpha 通道读取（或额外高度贴图采样），片元计算量小幅上升，但在地形类场景中完全值得，视觉提升非常显著。

---

## 自定义 Terrain Shader 的入口

Unity 不允许直接在 Terrain Inspector 里拖入任意材质球。替换地形材质的正确方式是通过脚本给 `Terrain.materialTemplate` 赋值：

```csharp
Terrain terrain = GetComponent<Terrain>();
terrain.materialTemplate = myCustomMaterial;
```

自定义 Shader 必须声明正确的变量名（`_Control`、`_Splat0`～`_Splat3`、`_Normal0`～`_Normal3` 等），否则 Unity 无法自动传递地形参数。最安全的做法是以 URP 内置的 `TerrainLitInput.hlsl` 为蓝本，保留其变量声明，在 Fragment 阶段替换混合逻辑。内置地形 Shader 位于 URP 包的 `Shaders/TerrainLit.shader`，是理解地形 Shader 的最佳参考材料。
