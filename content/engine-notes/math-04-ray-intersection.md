---
title: "图形数学 04｜射线求交：Ray-AABB、Ray-OBB、Ray-Triangle"
slug: "math-04-ray-intersection"
date: "2026-03-26"
description: "射线求交是拾取（Picking）、物理射线检测、BVH 加速结构的核心操作。这篇讲三种最常用的求交算法：Ray-AABB（slab 方法）、Ray-OBB（转换到 OBB 本地空间）、Ray-Triangle（Möller–Trumbore 算法），并给出可直接使用的代码。"
weight: 630
tags:
  - "数学"
  - "图形学"
  - "射线求交"
  - "AABB"
  - "OBB"
  - "Ray Casting"
  - "BVH"
series: "图形数学"
---
## 射线的表示

射线（Ray）是从一个点出发、沿某个方向延伸的半直线：

```
P(t) = Origin + t * Direction,   t >= 0
```

`t` 是参数，`t = 0` 是射线起点，`t > 0` 是射线前方的点。求交的目标是找到满足某个几何体表面方程的最小正 `t` 值，从而得到最近的交点坐标。

方向向量通常要求归一化（`length = 1`），这样 `t` 的值就等于射线行进的实际距离（单位米）。不归一化也可以运行，但 `t` 的含义变成方向向量长度的倍数，需要换算。

---

## Ray-AABB：Slab 方法

AABB（Axis-Aligned Bounding Box）由每个轴的 `[min, max]` 区间定义。把 AABB 看作三对平行平面（slab）的交集：x-slab、y-slab、z-slab。

射线穿过每个 slab 时，分别计算进入（t_min）和退出（t_max）的 `t` 值：

```
t_min_x = (aabb.min.x - ray.origin.x) / ray.dir.x
t_max_x = (aabb.max.x - ray.origin.x) / ray.dir.x
```

如果 `ray.dir.x < 0`，两个值需要交换（进入在右侧，退出在左侧）。

射线**同时在所有 slab 内部**的区间是 `[max(t_min), min(t_max)]`。如果这个区间非空（`t_enter <= t_exit`）且 `t_exit >= 0`，则相交：

```cpp
bool RayAABB(Ray ray, AABB aabb, float& tHit)
{
    float t_min = 0.0f;
    float t_max = FLT_MAX;

    for (int i = 0; i < 3; i++)
    {
        float invD = 1.0f / ray.dir[i];
        float t0 = (aabb.min[i] - ray.origin[i]) * invD;
        float t1 = (aabb.max[i] - ray.origin[i]) * invD;
        if (invD < 0) std::swap(t0, t1);

        t_min = std::max(t_min, t0);
        t_max = std::min(t_max, t1);

        if (t_max < t_min) return false;  // 区间为空，不相交
    }

    tHit = t_min;
    return true;
}
```

**处理 dir = 0 的情况**：当射线方向的某个分量为 0 时，`1.0f / 0.0f` 在 IEEE 754 里等于 `±inf`，`(aabb.min - origin) * inf` 的结果是 `±inf` 或 `NaN`（当 `origin` 恰好在 slab 边界上时）。实践中通常允许这种 inf 传播（多数 CPU/GPU 的 min/max 对 NaN 有确定行为），或者提前 clamp dir 的分量到一个极小值 `1e-7f`。

---

## Ray-OBB：转换到本地空间

OBB（Oriented Bounding Box）有旋转，但本地空间里它就是一个 AABB（以自身中心为原点，半尺寸为 `halfExtents`）。

把射线从世界空间转换到 OBB 本地空间，然后做 Ray-AABB 测试：

```cpp
bool RayOBB(Ray worldRay, Matrix4x4 obbWorldMatrix, Vector3 halfExtents, float& tHit)
{
    // 构造 OBB 的逆矩阵（世界→本地）
    Matrix4x4 invOBB = obbWorldMatrix.inverse;

    // 变换射线到 OBB 本地空间
    Ray localRay;
    localRay.origin = invOBB.MultiplyPoint(worldRay.origin);
    // 方向只做旋转/缩放，不做平移（用 MultiplyVector 而非 MultiplyPoint）
    localRay.dir    = invOBB.MultiplyVector(worldRay.dir);

    // 在本地空间里对 [-halfExtents, +halfExtents] 做 AABB 测试
    AABB localAABB;
    localAABB.min = -halfExtents;
    localAABB.max =  halfExtents;

    return RayAABB(localRay, localAABB, tHit);
    // 注意：tHit 此时是本地空间里的 t，如果本地射线方向有缩放则 t 值需要换算
    // 如果 obbWorldMatrix 含非均匀缩放，tHit 不直接等于世界空间距离
}
```

如果 OBB 的变换矩阵含非均匀缩放，本地空间里的 `t` 值不等于世界空间里的距离，需要乘以 `length(localRay.dir)` 换算。通常 OBB 只含旋转+平移+均匀缩放，这个问题不存在。

---

## Ray-Triangle：Möller–Trumbore 算法

这是现代游戏引擎和光线追踪里最标准的三角形求交算法，直接用重心坐标 `(u, v)` 和 `t` 联立方程，不需要先求平面。

推导出发点：交点 `P` 同时满足射线方程和三角形重心坐标参数化：

```
Origin + t * Dir = V0 + u * (V1 - V0) + v * (V2 - V0)
```

重排后得到线性方程组，用克拉默法则（Cramer's Rule）求解：

```cpp
bool RayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2,
                 float& t, float& u, float& v)
{
    const float EPSILON = 1e-7f;

    Vector3 e1 = v1 - v0;  // 边向量
    Vector3 e2 = v2 - v0;

    Vector3 h  = cross(ray.dir, e2);
    float   a  = dot(e1, h);

    // a ≈ 0 表示射线与三角形平行（或接近平行），无交点
    if (a > -EPSILON && a < EPSILON) return false;

    float   f  = 1.0f / a;
    Vector3 s  = ray.origin - v0;
    u = f * dot(s, h);

    // u 在 [0, 1] 之外则交点在三角形外
    if (u < 0.0f || u > 1.0f) return false;

    Vector3 q  = cross(s, e1);
    v = f * dot(ray.dir, q);

    // v < 0 或 u + v > 1 则交点在三角形外
    if (v < 0.0f || u + v > 1.0f) return false;

    t = f * dot(e2, q);

    // t > EPSILON 确保交点在射线前方（不是背后）
    return t > EPSILON;
}
```

结果解释：
- `t` 是交点到射线起点的距离
- `u, v` 是重心坐标，第三个坐标 `w = 1 - u - v`
- 交点坐标：`P = (1-u-v)*v0 + u*v1 + v*v2`
- 可以用重心坐标插值法线、UV、颜色等顶点属性

**双面 vs 单面**：上面的代码对双面三角形有效（`a < -EPSILON` 是背面，`a > EPSILON` 是正面，两者都返回 `true`）。如果只需要单面（背面剔除），只保留 `a > EPSILON` 的分支，把 `a > -EPSILON && a < EPSILON` 改为 `a < EPSILON`（即 `a <= 0` 时返回 false）。

---

## BVH 加速结构

直接对场景所有三角形做射线求交，时间复杂度是 O(n)。`Physics.Raycast` 如果每帧测试几百万个三角形是不可接受的。

BVH（Bounding Volume Hierarchy）把三角形组织成树形结构，每个内部节点存一个 AABB（包住子节点所有三角形）。射线求交时：

1. 先做 Ray-AABB 测试（O(1)）
2. 如果不相交，剪掉整棵子树
3. 如果相交，递归测试子节点
4. 到叶节点时做 Ray-Triangle 测试

对随机分布的场景，BVH 把射线求交从 O(n) 降到 O(log n)。Unity Physics（PhysX 底层）和 DOTS Physics（Unity.Physics）都使用 BVH 加速。

```
// 伪代码
float BVHRaycast(Node node, Ray ray)
{
    if (!RayAABB(ray, node.bounds)) return INF;   // 快速剔除

    if (node.isLeaf)
    {
        float tMin = INF;
        foreach (triangle in node.triangles)
            tMin = min(tMin, RayTriangle(ray, triangle));
        return tMin;
    }

    float tLeft  = BVHRaycast(node.left,  ray);
    float tRight = BVHRaycast(node.right, ray);
    return min(tLeft, tRight);
}
```

优化：先访问 `t` 值更小的子节点，可以更早确定最近交点并剪枝。

---

## Unity 的 API

```csharp
Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
RaycastHit hit;

if (Physics.Raycast(ray, out hit, maxDistance))
{
    // hit.point          — 世界空间交点坐标
    // hit.distance       — t 值（射线参数）
    // hit.normal         — 交点处表面法线（世界空间）
    // hit.triangleIndex  — 命中的三角形索引（需要 MeshCollider）
    // hit.barycentricCoordinate — 重心坐标 (u, v, w)

    // 用重心坐标取得顶点 UV
    Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
    int[] tri = mesh.triangles;
    Vector2[] uvs = mesh.uv;
    int idx = hit.triangleIndex * 3;
    Vector2 uv = uvs[tri[idx]]   * hit.barycentricCoordinate.x
               + uvs[tri[idx+1]] * hit.barycentricCoordinate.y
               + uvs[tri[idx+2]] * hit.barycentricCoordinate.z;
}
```

`Physics.Raycast` 内部走 PhysX 的 BVH，不会逐三角形遍历。`hit.barycentricCoordinate` 就是 Möller–Trumbore 算法里的 `(1-u-v, u, v)` 三元组。

---

## 小结

- Ray 用 `Origin + t * Direction` 表示，t ≥ 0 为有效交点。
- Ray-AABB Slab 方法：计算各轴进出 t 值，取 `[max(t_min), min(t_max)]`，区间非空则相交，O(1)。
- Ray-OBB：把射线变换到 OBB 本地空间后做 Ray-AABB。
- Ray-Triangle Möller–Trumbore：直接求解 t/u/v，20 行以内，双面/单面灵活控制。
- BVH 把场景射线求交从 O(n) 降到 O(log n)，是 `Physics.Raycast` 的底层加速结构。
