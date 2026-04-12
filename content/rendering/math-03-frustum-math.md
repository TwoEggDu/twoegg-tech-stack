---
title: "图形数学 03｜视锥体数学：裁剪平面提取、物体可见性判断"
slug: "math-03-frustum-math"
date: "2026-03-26"
description: "Frustum Culling 的核心是用 6 个平面方程判断物体是否在视锥体内。这篇讲如何从 VP 矩阵提取这 6 个平面、AABB 与平面的快速测试算法、以及 Unity 的 GeometryUtility.CalculateFrustumPlanes 背后做了什么。"
weight: 620
tags:
  - "数学"
  - "图形学"
  - "视锥体"
  - "Frustum Culling"
  - "AABB"
  - "剔除"
series: "图形数学"
---
## 视锥体的几何结构

透视相机的视锥体（Frustum）是一个四棱台，由 6 个平面围成：近平面（Near）、远平面（Far）、左平面（Left）、右平面（Right）、上平面（Top）、下平面（Bottom）。平面的法线统一定义为**指向视锥体内部**。

平面方程的一般形式：

```
Ax + By + Cz + D = 0
```

等价写法：`dot(n, p) + d = 0`，其中 `n = (A, B, C)` 是平面法线，`d = D`，`p` 是平面上任意一点。

给定一个点 `q`，计算它到平面的**有符号距离**：

```hlsl
float signedDist = dot(plane.xyz, q) + plane.w;
// 正值：q 在法线指向一侧（视锥体内部）
// 负值：q 在法线背侧（视锥体外部）
```

如果法线是归一化的，`signedDist` 就是实际距离（单位米）。如果不归一化，距离需要除以 `length(plane.xyz)`，但剔除测试只需要判断正负号，可以不归一化，节省一次 `sqrt`。

---

## 从 VP 矩阵提取 6 个裁剪平面

Gribb-Hartmann 方法是最常用的做法，直接从 View-Projection 矩阵的行（行主序）提取 6 个平面，不需要知道 FOV、aspect ratio 等相机参数。

设 VP 矩阵的 4 行分别为 `r0, r1, r2, r3`（每行是 `float4`）：

```
Near   = r3 + r2
Far    = r3 - r2
Left   = r3 + r0
Right  = r3 - r0
Bottom = r3 + r1
Top    = r3 - r1
```

**注意行列主序**：Unity 的矩阵（`Matrix4x4`）在 C# 侧是列主序存储，但 HLSL 侧是行主序。如果在 C# 里手动提取平面，需要先转置：

```csharp
// C# 侧提取视锥体平面
Matrix4x4 vp = projMatrix * viewMatrix;  // Unity 列主序

// 转为行主序（取各行）
Vector4 r0 = new Vector4(vp.m00, vp.m01, vp.m02, vp.m03);
Vector4 r1 = new Vector4(vp.m10, vp.m11, vp.m12, vp.m13);
Vector4 r2 = new Vector4(vp.m20, vp.m21, vp.m22, vp.m23);
Vector4 r3 = new Vector4(vp.m30, vp.m31, vp.m32, vp.m33);

Vector4 nearPlane   = r3 + r2;
Vector4 farPlane    = r3 - r2;
Vector4 leftPlane   = r3 + r0;
Vector4 rightPlane  = r3 - r0;
Vector4 bottomPlane = r3 + r1;
Vector4 topPlane    = r3 - r1;
```

提取后对平面进行归一化（如果需要精确距离）：

```csharp
Vector4 NormalizePlane(Vector4 p)
{
    float len = new Vector3(p.x, p.y, p.z).magnitude;
    return p / len;
}
```

---

## 点与视锥体的测试

判断一个点是否在视锥体内，对 6 个平面各做一次距离测试：

```csharp
bool IsPointInFrustum(Vector3 point, Vector4[] planes)
{
    foreach (var plane in planes)
    {
        float dist = plane.x * point.x + plane.y * point.y
                   + plane.z * point.z + plane.w;
        if (dist < 0) return false;  // 在某个平面的外侧
    }
    return true;
}
```

---

## AABB 与视锥体的快速测试

对每个平面，需要找 AABB 8 个角里"最靠外"的那个点——如果这个点都在平面内侧，AABB 整体一定在内侧；如果这个点在外侧，整个 AABB 都在该平面外，可以剔除。

这个"最靠外"的点叫 **p-vertex（正极点）**，不需要枚举 8 个角，可以根据平面法线符号直接选 AABB 的 min/max 分量：

```csharp
Vector3 GetPVertex(Vector3 aabbMin, Vector3 aabbMax, Vector4 plane)
{
    // 对每个轴，法线分量为正则取 max，为负则取 min
    return new Vector3(
        plane.x >= 0 ? aabbMax.x : aabbMin.x,
        plane.y >= 0 ? aabbMax.y : aabbMin.y,
        plane.z >= 0 ? aabbMax.z : aabbMin.z
    );
}
```

完整 AABB-Frustum 测试：

```csharp
bool IsAABBVisible(Bounds bounds, Vector4[] frustumPlanes)
{
    Vector3 min = bounds.min;
    Vector3 max = bounds.max;

    foreach (var plane in frustumPlanes)
    {
        Vector3 pVertex = GetPVertex(min, max, plane);
        float dist = plane.x * pVertex.x + plane.y * pVertex.y
                   + plane.z * pVertex.z + plane.w;
        if (dist < 0) return false;  // p-vertex 在平面外侧 → 整个 AABB 在外
    }
    return true;  // 通过所有平面 → 可见（可能是部分可见）
}
```

时间复杂度：6 次平面测试，每次 3 次比较 + 1 次点积，总共 O(1)。这比 OBB 测试快得多，是场景剔除的标准方法。

**注意**：上述测试存在保守性误差——可能把实际不可见的 AABB 判断为可见（false positive），但不会把可见的判断为不可见（no false negative）。对渲染正确性没有影响，只是多画了极少数本可剔除的物体。如果需要更精确，可以加 n-vertex 测试（用 p-vertex 判断"完全在外"，用 n-vertex 判断"完全在内"）。

---

## Unity 的 GeometryUtility API

Unity 内置的 `GeometryUtility.CalculateFrustumPlanes` 直接返回摄像机的 6 个平面，省去手动提取的麻烦：

```csharp
using UnityEngine;

Camera cam = Camera.main;
Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

// 或者从自定义 VP 矩阵提取
Matrix4x4 customVP = projMatrix * viewMatrix;
Plane[] planes2 = GeometryUtility.CalculateFrustumPlanes(customVP);

// 测试 AABB
Renderer renderer = someObject.GetComponent<Renderer>();
bool visible = GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
```

`Plane` 结构体有 `normal`（归一化法线，`Vector3`）和 `distance`（原点到平面的有符号距离，`float`）字段。Unity 的 `Plane` 定义里，`distance` 是正数表示原点在平面法线指向那侧（即视锥体内侧），与 Gribb-Hartmann 提取出的 `w` 分量符号约定一致。

在 Compute Shader 里做 GPU Culling 时，通常把这 6 个平面打包进一个 `float4[6]` 的 constant buffer 传入：

```hlsl
// Compute Shader 侧
cbuffer FrustumPlanes : register(b1)
{
    float4 _Planes[6];  // xyz=normal, w=distance
};

bool IsVisible(float3 aabbMin, float3 aabbMax)
{
    for (int i = 0; i < 6; i++)
    {
        float3 pv;
        pv.x = _Planes[i].x >= 0 ? aabbMax.x : aabbMin.x;
        pv.y = _Planes[i].y >= 0 ? aabbMax.y : aabbMin.y;
        pv.z = _Planes[i].z >= 0 ? aabbMax.z : aabbMin.z;
        if (dot(_Planes[i].xyz, pv) + _Planes[i].w < 0)
            return false;
    }
    return true;
}
```

---

## 球体与视锥体测试

如果包围体是球体（Bounding Sphere），测试比 AABB 更简单：

```csharp
bool IsSphereVisible(Vector3 center, float radius, Vector4[] planes)
{
    foreach (var plane in planes)
    {
        float dist = plane.x * center.x + plane.y * center.y
                   + plane.z * center.z + plane.w;
        if (dist < -radius) return false;
    }
    return true;
}
```

`dist < -radius` 表示球体整体在平面外侧，可以剔除。球体测试比 AABB 快（不需要选 p-vertex），但精度更差（球体通常比 AABB 包得更松），适合粒子系统、远景地形块等形状比较规则的物体。

---

## 小结

- 视锥体 6 个平面用 Gribb-Hartmann 方法从 VP 矩阵直接提取，注意行列主序。
- AABB-Frustum 测试的核心是 p-vertex：根据平面法线符号直接取 AABB 的 min/max 分量，无需枚举 8 个顶点。
- 测试结果有保守性（false positive），不影响渲染正确性。
- Unity 里用 `GeometryUtility.CalculateFrustumPlanes` + `TestPlanesAABB` 直接完成，GPU Culling 在 Compute Shader 里实现相同逻辑。
- 球体测试速度更快，适合形状规则的物体；AABB 测试更精确，适合场景常规剔除。
