---
title: "Shader 进阶技法 16｜实时光线追踪基础：DXR、Vulkan RT 与混合渲染思路"
slug: shader-advanced-16-raytracing
date: "2026-03-28"
description: "光线追踪在 PC 和主机端已进入实用阶段。本文讲清 DXR 管线的五种 Shader 类型、加速结构 BLAS/TLAS 的作用，以及为什么现代游戏都用「混合渲染」而非纯光追，最后说明 Unity 的支持现状与移动端限制。"
tags: ["Shader", "HLSL", "进阶", "光线追踪", "DXR", "混合渲染"]
series: "Shader 手写技法"
weight: 4440
---

光栅化渲染从发明至今最大的局限始终是同一件事：它不知道场景里别的东西在哪儿。一个片元做 shading 时，它只有自己的几何信息和传入的贴图，根本无法感知其他物体是否遮挡了它、周围有什么颜色的光从哪里反射来。反射、全局照明、准确的接触阴影——这些都要靠各种近似技巧来"假装"。

光线追踪从根本上解决了这个问题：从每个像素出发射出光线，查询它真正命中了什么。命中点可以再次射出光线，这样就能得到真实的镜面反射、折射、全局照明。代价是巨大的计算量——这也是为什么它直到 2018 年 NVIDIA Turing 架构引入专用 RT Core 之后才变得实时可用。

---

## DXR 管线：五种 Shader 类型

DXR（DirectX Raytracing）是微软在 D3D12 上引入的光线追踪 API。它定义了一套新的 Shader 类型，和传统的 VS/PS 完全不同：

**RayGen Shader（光线生成）**
整个 RT 管线的入口点。通常每个像素执行一次，负责构造初始光线并调用 `TraceRay()`。可以类比为 Compute Shader 的 `CSMain`，只是结果写入 RT 输出而不是 RWTexture2D。

**ClosestHit Shader（最近命中）**
光线命中加速结构中最近的三角形时执行。这里做真正的 shading 计算，或者发射二次光线（反射光、折射光）。

**AnyHit Shader（任意命中）**
光线穿越半透明物体时执行，可以在这里决定是否忽略该命中（实现 alpha test）。性能敏感，不必要时不要实现。

**Miss Shader（未命中）**
光线没有命中任何几何体时执行，通常用来采样天空盒或返回背景色。

**Intersection Shader（相交测试）**
只用于非三角形几何（如程序化球体、SDF）。大多数情况下用不到，GPU 硬件会自动处理三角形相交。

```hlsl
// RayGen Shader 示意
[shader("raygeneration")]
void RayGenMain()
{
    uint2 launchIndex = DispatchRaysIndex().xy;
    uint2 launchDim   = DispatchRaysDimensions().xy;

    RayDesc ray;
    ray.Origin    = _CameraPos;
    ray.Direction = computeRayDirection(launchIndex, launchDim);
    ray.TMin      = 0.001;
    ray.TMax      = 1000.0;

    RayPayload payload = (RayPayload)0;
    TraceRay(_AccelStructure, RAY_FLAG_NONE, 0xFF,
             0, 1, 0, ray, payload);

    _OutputTex[launchIndex] = float4(payload.color, 1.0);
}

// ClosestHit Shader 示意
[shader("closesthit")]
void ClosestHitMain(inout RayPayload payload, BuiltInTriangleIntersectionAttributes attr)
{
    // attr.barycentrics 是重心坐标，可以插值顶点属性
    float3 hitNormal = interpolateNormal(attr.barycentrics);
    payload.color = saturate(dot(hitNormal, _LightDir)) * _Albedo;
}
```

---

## 加速结构：BLAS 和 TLAS

光线追踪不能对场景里所有三角形逐一测试，那样复杂度是 O(n)，完全无法实时。GPU 用 BVH（Bounding Volume Hierarchy，包围体层次结构）加速相交测试，把复杂度降到 O(log n)。

DXR 把加速结构分两层：

**BLAS（Bottom-Level Acceleration Structure，底层加速结构）**
对应单个网格或几何体，存储这个几何体的所有三角形 BVH。BLAS 一旦构建好可以复用，对应"这个模型长什么样"。

**TLAS（Top-Level Acceleration Structure，顶层加速结构）**
存储场景中所有 BLAS 实例及其变换矩阵。每帧如果物体移动了，只需更新 TLAS，不需要重建 BLAS。TLAS 对应"这个模型放在场景哪里"。

```
TLAS
├── Instance 0: BLAS_Tree × Transform_A
├── Instance 1: BLAS_Tree × Transform_B
├── Instance 2: BLAS_Character × Transform_C
└── Instance 3: BLAS_Building × Transform_D
```

Unity C# 端（URP RT 支持）：

```csharp
// 构建加速结构（简化示意）
RayTracingAccelerationStructure accelStructure =
    new RayTracingAccelerationStructure();
accelStructure.AddInstance(meshRenderer);
accelStructure.Build();

rayTracingShader.SetAccelerationStructure("_AccelStructure", accelStructure);
```

---

## TraceRay() 参数详解

```hlsl
TraceRay(
    AccelerationStructure,  // TLAS
    RayFlags,               // RAY_FLAG_NONE / RAY_FLAG_CULL_BACK_FACING_TRIANGLES 等
    InstanceMask,           // 用于过滤实例，0xFF 表示命中所有
    HitGroupIndex,          // 使用哪个 ClosestHit/AnyHit 组合
    GeometryMultiplier,     // 通常为 1
    MissShaderIndex,        // 使用哪个 Miss Shader
    Ray,                    // RayDesc：origin, direction, tmin, tmax
    Payload                 // 用户自定义结构，在各 Shader 间传递数据
);
```

`Payload` 结构体由开发者定义，贯穿整个光线的生命周期：

```hlsl
struct RayPayload
{
    float3 color;
    float  distance;
    int    recursionDepth; // 防止无限递归
};
```

---

## 混合渲染：为什么不用纯光追

即使在高端 PC 上，纯光追渲染性能也无法支撑主流分辨率下 60fps。实际游戏采用的是混合渲染（Hybrid Rendering）：

- 主要几何渲染：光栅化（速度快、成熟稳定）
- 反射：RT 反射，只对高光材质开启
- 环境遮蔽（AO）：RT AO，替代 SSAO
- 阴影：RT 阴影，替代 Shadow Map，特别是面光源软阴影
- 全局照明：可选，代价最高

```
帧渲染流程（混合模式）：
G-Buffer Pass（光栅化）
    ↓
RT Shadow Pass（光线追踪，1 spp）
    ↓
RT Reflection Pass（光线追踪，1 spp）
    ↓
Denoise（时域降噪，SVGF / TAA 类算法）
    ↓
Lighting Combine（合并所有结果）
    ↓
Post Processing
```

关键点是每条光线只采 1 个样本（1 spp），然后靠时域降噪（Temporal Denoising）补足质量。这是现代 RT 游戏的标准做法。

---

## Unity 中的 RT 支持

Unity 在 HDRP 中提供了 Ray Tracing 支持（需要 D3D12 + DXR 兼容显卡）。RT Shader 文件扩展名为 `.raytrace`，配合 `RayTracingShader` Asset 使用。URP 截至 Unity 6 还没有完整的 RT 管线支持，RT 能力主要集中在 HDRP。

---

## 移动端现状

移动端主流 GPU（Adreno、Mali、Apple GPU）中 Apple A14+ 芯片已有硬件光追能力，Metal 3 提供了对应 API。高通 Adreno 740 也加入了 RT 硬件。但实用门槛仍然很高：移动端带宽极其宝贵，RT 需要大量随机内存访问，功耗和热量都是瓶颈。当前阶段移动端 RT 更多是技术演示，在商业游戏中几乎不用——光线追踪本质上还是高端 PC 和主机的专属技术。
