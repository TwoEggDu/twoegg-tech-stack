---
title: "游戏常用效果｜地形阴影：Height Shadow、SDF Shadow 与性能权衡"
slug: "shader-terrain-03-shadow"
date: "2026-03-28"
description: "从 CSM 的分辨率瓶颈出发，对比 Height Shadow、SDF Shadow 和烘焙阴影三种方案，帮你在画质与性能之间找到平衡点。"
tags: ["Shader", "HLSL", "URP", "地形", "阴影", "Height Shadow", "SDF"]
series: "Shader 手写技法"
weight: 4550
---

地形阴影是大世界游戏里最难处理的性能痛点之一。玩家能看到几百米外的山体投影，Shadow Map 的分辨率却只有 2048 或 4096——把这张图拉伸覆盖几百米，每个像素代表几十厘米，近处阴影的锯齿几乎无法接受。Unity URP 用 Cascaded Shadow Map 缓解了这个问题，但对超大地形，仍然需要额外的技术手段。

---

## Cascaded Shadow Map（CSM）

URP 默认支持最多 4 级 Shadow Cascade。每一级覆盖距相机不同半径的范围，使用同一张 Shadow Atlas 上的不同区域：

- **Cascade 0**：最近，范围最小（比如 10m），分辨率最高
- **Cascade 1**：稍远，范围扩大（50m）
- **Cascade 2**：更远（150m）
- **Cascade 3**：最远（500m），分辨率最低

在 Shader 里，URP 通过 `_MainLightWorldToShadow` 矩阵数组和 `_CascadeShadowSplitSpheres` 确定当前片段应使用哪一级 Cascade，这部分逻辑封装在 `Shadows.hlsl` 的 `ComputeCascadeIndex` 里，不需要手写。

地形面临的问题是 Cascade 3 覆盖范围极大，单个 texel 代表的物理距离可能超过 1m，平坦地表上的阴影边缘会出现明显阶梯。调整 `cascadeBorder` 增大 Cascade 间的混合区域可以软化边界，但会引入双重阴影的 artifact。

---

## Shadow Acne 与 Bias 调优

地形的几何特性（大面积水平面）使它特别容易产生 Shadow Acne——表面自我遮挡产生的条纹噪声。原因是采样深度与存储深度之间的精度误差。

URP 提供两个 bias 参数：

- **Depth Bias**：沿光源方向偏移阴影接收位置，减小自遮挡
- **Normal Bias**：沿法线方向偏移，对水平面地形更有效

```hlsl
// URP 在 ShadowCaster Pass 里通过 ApplyShadowBias 施加偏移
// 对地形通常需要把 Normal Bias 调到 0.4~0.8
// 在 URP Asset → Shadows → Normal Bias 设置，或单独给 Terrain 的 Light 加 Additional Shadow Data
```

地形坡面法线朝向多样，Normal Bias 过大会导致陡坡处阴影"飘离"几何体。实践中建议对地形 Renderer 单独设置 `AdditionalShadowData` 组件，而不是全局调整 URP Asset 的 bias 值。

---

## Height Shadow：用高度图模拟自遮挡

对于相对平缓的地形，有一种开销极低的方案：直接用 Heightmap 计算光线与地表的高度关系，近似模拟太阳直射角下的自遮挡阴影。

基本思路：从当前片段的世界坐标，沿太阳方向步进若干次，每步采样 Heightmap；若某步的地形高度超过光线高度，则认为该点处于阴影中。

```hlsl
float HeightShadow(float2 worldXZ, float worldY, float3 lightDir,
                   Texture2D heightmap, SamplerState samp,
                   float2 terrainSizeXZ, float terrainHeight)
{
    float2 stepXZ = normalize(lightDir.xz) * 2.0;  // 步长 2m（根据地形尺度调整）
    float  stepY  = lightDir.y * 2.0;
    int    steps  = 8;

    for (int i = 1; i <= steps; i++)
    {
        float2 sampleXZ = worldXZ + stepXZ * i;
        float2 sampleUV = sampleXZ / terrainSizeXZ;

        // 越界时截断，避免采样地形外
        if (any(sampleUV < 0.0) || any(sampleUV > 1.0))
            break;

        float terrainH = SAMPLE_TEXTURE2D_LOD(heightmap, samp, sampleUV, 0).r * terrainHeight;
        float rayH     = worldY + stepY * i;

        if (terrainH > rayH)
            return 0.0; // 被遮挡
    }
    return 1.0;
}
```

在 Fragment Shader 里将结果乘入光照衰减：

```hlsl
float heightShadow = HeightShadow(input.positionWS.xz, input.positionWS.y,
                                   _MainLightPosition.xyz,
                                   _TerrainHeightmapTexture, sampler_TerrainHeightmapTexture,
                                   _TerrainSize.xz, _TerrainHeightmapScale.y);

Light mainLight = GetMainLight(shadowCoord);
mainLight.shadowAttenuation *= heightShadow;
```

这种方法的限制很明确：步进次数固定，无法处理悬崖下方；太阳角度接近水平时步进距离极长，artifact 增多。适合步调和缓的山地关卡，不适合城堡或悬崖场景。

---

## SDF Shadow：离线烘焙，运行时查表

对于需要高质量地形阴影且不能依赖 Shadow Map 的项目（典型场景：移动端大世界，Shadow Map 分辨率预算极低），可以离线计算一张 SDF（有向距离场）贴图，记录地形上每个点到最近"阴影边界"的距离。

烘焙流程（通常在编辑器工具里完成）：

1. 对每个太阳角度预设（清晨/正午/黄昏），光线追踪地形几何体，生成阴影掩码（0 = 阴影，1 = 光照）
2. 对阴影掩码做 JFA（Jump Flood Algorithm）或 brute-force 求 2D SDF
3. 将结果存入 R16 贴图，正值表示光照区，负值表示阴影区

运行时 Shader 查表：

```hlsl
// 根据太阳方向选取对应的 SDF 贴图（或在多张之间插值）
float2 sdfUV     = input.positionWS.xz / _TerrainSize.xz;
float  shadowSDF = SAMPLE_TEXTURE2D(_TerrainShadowSDF, sampler_TerrainShadowSDF, sdfUV).r;

// SDF > 0 为光照区，SDF < 0 为阴影区，0 附近做软化
float shadowMask = smoothstep(-_ShadowSoftness, _ShadowSoftness, shadowSDF);

// 与 CSM 阴影混合：近处用 CSM，远处用 SDF
float blendFactor = saturate((camDist - _SdfBlendStart) / (_SdfBlendEnd - _SdfBlendStart));
float finalShadow = lerp(csmShadow, shadowMask, blendFactor);
```

SDF Shadow 的优点是采样成本极低（一次纹理采样），阴影边界软硬可控，没有 Shadow Acne。缺点是静态——太阳实时移动时需要在多张 SDF 贴图之间插值，且动态物体（树木倒下、建筑爆炸）无法反映到 SDF 中。

---

## Distance Shadow Fade

无论使用哪种阴影方案，都需要在 `ShadowDistance`（URP Asset 里设置）之外平滑淡出，避免阴影突然截止：

```hlsl
// URP 内置的阴影淡出
float shadowFade = GetMainLightShadowFade(input.positionWS);
// shadowFade 从 0（完整阴影）到 1（无阴影）
float finalShadow = lerp(shadowAttenuation, 1.0, shadowFade);
```

在自定义地形 Shader 里可以用这个函数直接接入 URP 的 Shadow Distance 设置，不需要重新实现淡出逻辑。

---

## 三种方案对比

| 方案 | 动态支持 | GPU 开销 | 质量 | 适合规模 |
|------|----------|----------|------|----------|
| CSM（内置） | 完整 | 中高 | 近处好，远处差 | 所有项目 |
| Height Shadow | 部分（太阳角度） | 极低 | 平缓地形尚可 | 移动端简单场景 |
| SDF Shadow | 有限（多张插值） | 极低 | 一致，可软化 | 移动端大世界 |
| 烘焙 Lightmap | 无 | 零 | 最高 | 单机固定光照场景 |

实际项目中常见的组合是：CSM 处理 Cascade 0～1 的近距离动态阴影，SDF 或烘焙 Lightmap 负责远距离地形，两者在过渡带线性混合，兼顾性能与画质。
