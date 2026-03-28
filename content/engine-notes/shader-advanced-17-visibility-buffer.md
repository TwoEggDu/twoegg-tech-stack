---
title: "Shader 进阶技法 17｜Visibility Buffer：Nanite 背后的思路与传统 G-Buffer 的区别"
slug: shader-advanced-17-visibility-buffer
date: "2026-03-28"
description: "Visibility Buffer 只存 triangle ID 和 instance ID，把 shading 推迟到只有最终可见像素才计算。本文讲清它和传统 G-Buffer 的本质区别、两阶段渲染流程，以及 Nanite 如何在这个基础上叠加 Cluster LOD 和软件光栅化。"
tags: ["Shader", "HLSL", "进阶", "Visibility Buffer", "Nanite", "延迟渲染"]
series: "Shader 手写技法"
weight: 4450
---

传统延迟渲染用 G-Buffer 把几何信息缓存下来，再在 Lighting Pass 里统一计算光照。这套方案解决了多光源的效率问题，但带来了另一个根本性瓶颈：G-Buffer 本身的带宽。法线、albedo、roughness、metallic……每个 Pass 都要写入和读取大量数据，在分辨率越来越高、材质越来越复杂的今天，G-Buffer 带宽成了渲染性能的天花板之一。

更深的问题是过度绘制（Overdraw）。就算 Early-Z 剔除了大部分被遮挡的几何，仍然有相当多的片元被写入 G-Buffer 之后又被后面的几何覆盖掉，白白完成了大量顶点和片元计算。

Visibility Buffer 从根本上换了一种思路。

---

## Visibility Buffer 的核心思想

Visibility Buffer 只存两件事：**这个像素对应的是哪个三角形（triangle ID）、属于哪个实例（instance ID）**。

```
G-Buffer 存储（每像素）：
├── RT0: Albedo（RGB） + Occlusion（A）    → 32 bit
├── RT1: Normal（RGB） + Smoothness（A）   → 32 bit
├── RT2: Metallic + Roughness + ...       → 32 bit
└── Depth Buffer                          → 32 bit
合计：约 128 bit/像素

Visibility Buffer 存储（每像素）：
└── triangleID(24 bit) + instanceID(8 bit) → 32 bit 打包
合计：32 bit/像素（不含 Depth）
```

带宽立刻降到 G-Buffer 的 1/4 左右（仅 Visibility Pass 阶段）。真正的 shading 被推迟到 Material Pass，而且 Material Pass 只处理最终可见像素——每个屏幕像素只做一次 shading，完全消灭了 G-Buffer 里可能存在的重复 shading。

---

## 两阶段渲染流程

Visibility Buffer 渲染分两个阶段：

**第一阶段：Visibility Pass**

绑定一张 32-bit RenderTexture 作为 Visibility Buffer，光栅化所有几何，每个片元只做一件事：把 triangle ID 和 instance ID 打包写进去。不采样贴图，不做光照，不输出颜色。

```hlsl
// Visibility Pass - Fragment Shader
// 极简，只写 ID
uint frag(Varyings input) : SV_Target
{
    // 假设 _InstanceID 和 _TriangleID 由顶点/几何阶段传入
    uint vis = (_InstanceID & 0xFF) | ((_TriangleID & 0xFFFFFF) << 8);
    return vis;
}
```

**第二阶段：Material Pass（Shading Pass）**

用一个全屏 Quad（或 Compute Shader）逐像素处理。读取 Visibility Buffer，解码出 triangle ID 和 instance ID，从 GPU 缓冲区里取回对应三角形的顶点数据，手动插值出当前像素的世界坐标、法线、UV，然后执行材质计算。

```hlsl
// Material Pass - 读取并重建 shading 数据
uint visibilityData = _VisibilityBuffer.Load(int3(pixelCoord, 0));
uint instanceID  = visibilityData & 0xFF;
uint triangleID  = (visibilityData >> 8) & 0xFFFFFF;

// 从 StructuredBuffer 取回几何数据
InstanceData inst = _InstanceBuffer[instanceID];
TriangleData  tri  = _TriangleBuffer[inst.triangleOffset + triangleID];

// 手动重建重心坐标并插值
float3 barycentrics = computeBarycentrics(pixelCoord, tri, inst.transform);
float2 uv     = interpolate(tri.uv0, tri.uv1, tri.uv2, barycentrics);
float3 normal = interpolate(tri.n0, tri.n1, tri.n2, barycentrics);

// 正常执行材质计算
half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, uv);
// ... lighting ...
```

Material Pass 里的材质计算本质上变成了 Compute Shader 风格，而不是传统的 Fragment Shader。不同材质需要根据 instanceID 判断走哪套 shading 路径（相当于动态分支或 Indirect Dispatch）。

---

## Nanite 的实现思路

Nanite 是 Unreal Engine 5 的虚拟几何体系统，Visibility Buffer 是它的核心组成部分之一。在 Visibility Buffer 的基础上，Nanite 叠加了两项关键技术：

**Cluster LOD**

每个 Mesh 被预处理成多个 Cluster（约 128 个三角形一组），并在不同细节级别间建立层次关系。运行时在 GPU 上根据屏幕覆盖面积选择最合适的 Cluster 粒度，而不是整个 Mesh 切换 LOD。这使得超高多边形资产可以在保持画质的同时保持合理的 GPU 负载。

**软件光栅化（Software Rasterization）**

对于在屏幕上覆盖面积极小（接近亚像素）的 Cluster，硬件光栅化的 setup 开销反而比实际绘制还高。Nanite 对这些 Cluster 用 Compute Shader 做软件光栅化，完全绕开硬件光栅管线，效率更高。

```
Nanite 帧渲染流程（简化）：
GPU Instance Culling（Compute）
    ↓
Cluster LOD 选择（Compute）
    ↓
软件/硬件光栅化 → Visibility Buffer
    ↓
Material Classify（按材质分类像素）
    ↓
Per-Material Shading（Compute）
    ↓
合并输出
```

---

## 对 Shader 编写的影响

使用 Visibility Buffer 架构后，材质不再是一个绑定到网格的 Fragment Shader，而是变成一段在 Compute 里执行的代码。这对 Shader 编写有几点实际影响：

- 不能用标准 `ddx/ddy` 偏导数（Compute Shader 里没有），必须手动计算或用 `ddx_fine` 的变通方法
- 贴图采样需要显式指定 mip 级别（`SampleLevel`），不能依赖硬件自动 mip 选择
- 材质参数必须打包进 StructuredBuffer，通过 instanceID 索引，而不是传统的 per-draw cbuffer
- 不同材质类型需要在 Shader 内部做动态分支，或拆分成多个 Indirect Dispatch 批次

---

## Unity / URP 的支持情况

截至 Unity 6，URP 没有官方的 Visibility Buffer 实现。HDRP 内部有部分类似 Visibility Buffer 的优化路径，但不是完整的 Visibility Buffer 架构。Nanite 是 UE5 的专属功能，Unity 侧最接近的方案是 GPU Resident Drawer（Unity 6 新功能）+ Indirect Draw，但底层仍是 G-Buffer 延迟渲染，不是真正的 Visibility Buffer。

从研究角度理解 Visibility Buffer 的价值在于：它代表了高精度场景渲染的发展方向，理解它的两阶段流程和 ID 重建思路，对理解 Nanite、GPU Driven 渲染以及未来引擎架构的演进都非常有帮助。
