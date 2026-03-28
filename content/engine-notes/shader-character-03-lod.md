---
title: "游戏常用效果｜角色 LOD 策略：骨骼数量、材质合并与 Imposter"
slug: "shader-character-03-lod"
date: "2026-03-28"
description: "角色 LOD 不只是减面，骨骼和蒙皮的消耗才是主要瓶颈。本篇梳理骨骼缩减、材质合并、Imposter 布告板的完整策略，附 Octahedral Imposter Shader 核心代码。"
tags: ["Shader", "HLSL", "URP", "角色渲染", "LOD", "Imposter", "性能优化"]
series: "Shader 手写技法"
weight: 4520
---

谈到角色 LOD，很多人第一反应是减少多边形面数。这个思路没错，但对于角色来说，**骨骼数量和蒙皮计算**往往比面数更影响性能，特别是在移动端。一个 5000 面、60 根骨骼的角色，和一个 1500 面、12 根骨骼的角色，GPU 渲染消耗差距不大，但 CPU 端的骨骼变换和蒙皮矩阵上传的消耗差异是数倍级别的。

---

## 骨骼消耗的来源

角色动画的开销分两个部分：

**骨骼动画求值（CPU）**：每帧按动画曲线计算每根骨骼的 TRS，沿骨骼层级向下传播变换。骨骼越多，计算量越大，且无法轻易并行（父子依赖关系串行）。

**蒙皮矩阵上传（CPU→GPU）**：将所有骨骼矩阵打包上传到 GPU 的常量缓冲区或 StructuredBuffer，每根骨骼 4x3 矩阵（48 字节），60 根骨骼就是 2880 字节。这看起来不大，但乘以场景中的角色数量就可观了。

因此角色 LOD 的骨骼策略不是"减少渲染时的矩阵数"，而是**真实减少参与动画求值的骨骼节点**。

## LOD 0→1→2 骨骼缩减策略

```
LOD 0（近景，8m 以内）：完整骨骼（50~80 根）
  - 全套手指骨（每只手 14 根）
  - 多段脊柱骨（5~8 根）
  - 面部骨骼、IK 辅助骨、布料骨

LOD 1（中景，8~25m）：精简骨骼（20~30 根）
  - 合并手指为单一"握拳"骨
  - 脊柱合并为 2~3 根
  - 移除 IK 骨，直接用 FK 近似

LOD 2（远景，25~60m）：极简骨骼（8~12 根）
  - 每条手臂 1~2 根
  - 每条腿 2 根
  - 脊柱 1 根，颈部 + 头部各 1 根
```

在 Unity 中实现骨骼 LOD 通常需要维护多套 Animator Controller 或使用 Animator Culling Mode，但更彻底的方案是**不同 LOD 使用不同的 SkinnedMeshRenderer**，每个 SMR 绑定到各自精简骨骼层级上，通过 LOD Group 切换激活状态。

## 材质 LOD：从多材质到 Atlas

近景角色往往分多个材质：皮肤、头发、服装、金属配件各一张。远景时这些材质的细节差异几乎不可见，但 DrawCall 数量不变，每个 Sub-Mesh 依然需要一次独立的渲染提交。

**材质合并（Atlas 合并）**：将近景的多张贴图烘焙到一张大 Atlas 中，把所有 Sub-Mesh 合并为一个网格，整个角色只有一次 DrawCall。

材质合并在制作时通过工具链完成（Shader 中的 UV 坐标指向 Atlas 中的不同区域），运行时无需额外计算。合并后的材质 Shader 只需要在采样时做 UV 变换：

```hlsl
// Atlas UV 变换：每个区域由 (offset.xy, scale.xy) 定义
float2 AtlasUV(float2 uv, float4 atlasRect)
{
    // atlasRect.xy = offset, atlasRect.zw = scale
    return uv * atlasRect.zw + atlasRect.xy;
}

// 采样时：
float4 color = tex2D(_AtlasMap, AtlasUV(input.uv, _MaterialAtlasRect));
```

## Imposter：最远处的布告板假体

当角色距离超过 60~80m 时，即使极简骨骼也是浪费。**Imposter** 方案在最远处用一个永远朝向相机的四边形（Billboard）替代角色，四边形上贴的是角色的预渲染纹理。

普通 Billboard 只存储一个角度的图像，旋转相机时画面不对。**Octahedral Imposter** 解决了这个问题：把角色从多个方向预渲染，存储角度信息到一张图集中，运行时根据相机方向查找最近的预渲染视角，做双线性插值混合。

### Octahedral Mapping

用正八面体（Octahedron）展开来均匀分布球面上的视角：

```hlsl
// 将球面方向映射到正八面体 UV [0,1]^2
float2 OctahedralMap(float3 dir)
{
    // 投影到 L1 球
    float3 d = dir / (abs(dir.x) + abs(dir.y) + abs(dir.z));
    // 折叠下半球
    float2 uv = d.y >= 0.0 ? d.xz : (1.0 - abs(d.zx)) * sign(d.xz);
    return uv * 0.5 + 0.5;
}

// 从 UV 还原球面方向（逆映射）
float3 OctahedralUnmap(float2 uv)
{
    uv = uv * 2.0 - 1.0;
    float3 d = float3(uv.x, 1.0 - abs(uv.x) - abs(uv.y), uv.y);
    if (d.y < 0.0) d.xz = (1.0 - abs(d.zx)) * sign(d.xz);
    return normalize(d);
}
```

### Imposter Shader 核心

```hlsl
// Imposter Atlas 采样：gridSize x gridSize 个视角预渲染图
// atlasGridSize: 例如 8，表示 8x8 = 64 个视角
half4 ImposterFragment(Varyings input) : SV_Target
{
    // 获取相机相对于 Imposter 中心的方向
    float3 camDir = normalize(input.positionWS - _ImposterCenter);

    // 映射到 Octahedral UV，再映射到 atlas grid 坐标
    float2 octUV = OctahedralMap(camDir);
    float2 gridCoord = octUV * (_AtlasGridSize - 1.0);

    // 取最近的两个格子做插值（简单双线性）
    float2 gridFloor = floor(gridCoord);
    float2 gridFrac  = frac(gridCoord);

    // 计算四个相邻格子的 atlas UV
    float invGrid = 1.0 / _AtlasGridSize;
    float2 uv00 = (gridFloor + input.uv) * invGrid;
    float2 uv10 = (gridFloor + float2(1, 0) + input.uv) * invGrid;
    float2 uv01 = (gridFloor + float2(0, 1) + input.uv) * invGrid;
    float2 uv11 = (gridFloor + float2(1, 1) + input.uv) * invGrid;

    half4 c00 = tex2D(_ImposterAtlas, uv00);
    half4 c10 = tex2D(_ImposterAtlas, uv10);
    half4 c01 = tex2D(_ImposterAtlas, uv01);
    half4 c11 = tex2D(_ImposterAtlas, uv11);

    // 双线性插值
    half4 color = lerp(lerp(c00, c10, gridFrac.x),
                       lerp(c01, c11, gridFrac.x),
                       gridFrac.y);

    // Alpha cutoff，Imposter 通常不做半透明
    clip(color.a - 0.5);

    return half4(color.rgb, 1.0);
}
```

Imposter 的预渲染通常在编辑器工具中完成，输出一张包含 Color、Normal（可选）的 Atlas 贴图。Normal Atlas 可以让 Imposter 在动态光照下有正确的法线响应，避免看起来"贴片"感太强。

## Unity LOD Group 与 Shader LOD

Unity 的 `LOD Group` 组件在 Inspector 中配置各个 LOD 级别的屏幕覆盖率阈值。角色按上述结构组织为：

```
CharacterRoot
  ├── LOD0_Mesh (SkinnedMeshRenderer, 完整骨骼)
  ├── LOD1_Mesh (SkinnedMeshRenderer, 精简骨骼)
  ├── LOD2_Mesh (SkinnedMeshRenderer, 极简骨骼)
  └── LOD3_Imposter (MeshRenderer, Billboard + Imposter Shader)
```

LOD Group 的 `Fade Mode` 设为 `Cross Fade` 时，相邻 LOD 之间有短暂的 dithered 淡入淡出过渡，避免突然跳变。

**Shader LOD 关键字**是另一个维度的 LOD：

```hlsl
// 在 SubShader 中声明 LOD 值
SubShader
{
    LOD 300
    // 完整效果 Pass（高端设备）
}

SubShader
{
    LOD 150
    // 简化 Pass（低端设备）
}
```

通过 `Shader.globalMaximumLOD` 或 `Material.maximumLOD` 在运行时控制全局 Shader 质量档位，与几何 LOD 配合使用，可以在低端设备上同时降低面数和着色复杂度。

## 实际项目的 LOD 分配参考

| LOD 级别 | 距离阈值   | 面数     | 骨骼数 | 材质数 | DrawCall |
|----------|-----------|---------|--------|--------|---------|
| LOD 0    | 0~8m      | 15000+  | 60~80  | 4~6    | 4~6     |
| LOD 1    | 8~25m     | 5000    | 20~30  | 2~3    | 2~3     |
| LOD 2    | 25~60m    | 1200    | 8~12   | 1      | 1       |
| LOD 3    | 60m+      | 2 tri   | 0      | 1      | 1       |

LOD 3 是 Imposter，面数只有 2 个三角形（一个 Quad），全部开销转移到贴图采样。Atlas 分辨率通常 2048x2048（8x8 grid，每格 256x256），可接受。

角色 LOD 是工程和美术的协作工作，Shader 只是最终执行端。骨骼拓扑规划、材质 Atlas 烘焙、Imposter 预渲染工具链需要在项目早期就纳入 Pipeline，否则后期补救代价极高。
