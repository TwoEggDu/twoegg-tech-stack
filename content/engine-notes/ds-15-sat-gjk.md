---
title: "数据结构与算法 15｜SAT / GJK：窄相碰撞检测的精确算法"
description: "宽相筛出候选对后，窄相要精确判断两个形状是否真的相交，并计算穿透深度和法线（用于物理响应）。SAT 适合凸多边形，GJK 适合任意凸体。这篇讲清楚两者的原理和游戏里的实际应用。"
slug: "ds-15-sat-gjk"
weight: 769
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 碰撞检测
  - 物理
  - 游戏架构
series: "数据结构与算法"
---

> 宽相只说"这两个物体的 AABB 重叠了，可能碰撞"。窄相要回答：它们真的碰了吗？碰了多深？法线朝哪里？这些信息用于计算碰撞响应、推开物体、触发音效和特效。

---

## SAT（分离轴定理，Separating Axis Theorem）

**定理**：两个凸形状不相交，当且仅当存在一条轴，使得它们在这条轴上的投影不重叠。

换句话说：如果能找到一条"分离轴"，两个形状就没有碰撞；如果所有候选轴上投影都重叠，就一定碰撞了。

```
两个矩形（OBB）的分离轴候选：
  矩形 A 的 2 条边法线
  矩形 B 的 2 条边法线
  = 4 条候选轴

只要有一条轴能分离它们，就没有碰撞
所有 4 条轴上都重叠，才是碰撞
```

---

## SAT 实现：2D 凸多边形

```csharp
public static class SAT2D
{
    // 把多边形投影到轴上，返回 [min, max] 区间
    static (float min, float max) Project(Vector2[] polygon, Vector2 axis)
    {
        float min = Vector2.Dot(axis, polygon[0]);
        float max = min;
        for (int i = 1; i < polygon.Length; i++)
        {
            float d = Vector2.Dot(axis, polygon[i]);
            if (d < min) min = d;
            if (d > max) max = d;
        }
        return (min, max);
    }

    // 两个区间是否重叠
    static bool Overlaps(float minA, float maxA, float minB, float maxB)
        => minA <= maxB && minB <= maxA;

    // 重叠量（用于穿透深度计算）
    static float OverlapAmount(float minA, float maxA, float minB, float maxB)
        => Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);

    // 检测两个凸多边形是否相交，并返回最小穿透向量（MTV）
    public static bool Intersect(Vector2[] polyA, Vector2[] polyB,
                                  out Vector2 mtv)
    {
        mtv = Vector2.zero;
        float minOverlap = float.PositiveInfinity;
        Vector2 mtvAxis  = Vector2.zero;

        // 收集所有候选轴（两个多边形各条边的法线）
        var axes = GetAxes(polyA).Concat(GetAxes(polyB));

        foreach (var axis in axes)
        {
            var (minA, maxA) = Project(polyA, axis);
            var (minB, maxB) = Project(polyB, axis);

            if (!Overlaps(minA, maxA, minB, maxB))
                return false;  // 找到分离轴，不碰撞

            float overlap = OverlapAmount(minA, maxA, minB, maxB);
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                mtvAxis    = axis;
            }
        }

        // 确保 MTV 方向从 B 指向 A（推开 A）
        Vector2 centerA = GetCentroid(polyA), centerB = GetCentroid(polyB);
        if (Vector2.Dot(mtvAxis, centerA - centerB) < 0)
            mtvAxis = -mtvAxis;

        mtv = mtvAxis * minOverlap;  // 最小穿透向量
        return true;
    }

    static IEnumerable<Vector2> GetAxes(Vector2[] polygon)
    {
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 edge = polygon[(i + 1) % polygon.Length] - polygon[i];
            yield return new Vector2(-edge.y, edge.x).normalized;  // 边的法线
        }
    }

    static Vector2 GetCentroid(Vector2[] polygon)
    {
        Vector2 sum = Vector2.zero;
        foreach (var v in polygon) sum += v;
        return sum / polygon.Length;
    }
}
```

### 使用示例

```csharp
// 两个旋转矩形的碰撞检测
Vector2[] rectA = GetRotatedRect(centerA, sizeA, angleA);
Vector2[] rectB = GetRotatedRect(centerB, sizeB, angleB);

if (SAT2D.Intersect(rectA, rectB, out Vector2 mtv))
{
    // 发生碰撞，mtv 是最小穿透向量
    // 把 A 沿 mtv 方向推开 mtv.magnitude 的距离
    transformA.position += (Vector3)mtv;
    // 触发碰撞响应（伤害、音效等）
    OnCollision(objectA, objectB, mtv);
}
```

---

## SAT 的限制

- **只适合凸形状**：凹形状需要分解为多个凸形状
- **3D 中轴数量爆炸**：两个多面体除了面法线外，还需要检测"边叉积"轴，OBB vs OBB 需要测 15 条轴
- **性能**：轴的数量与多边形边数成正比，边数很多时慢

---

## GJK（Gilbert–Johnson–Keerthi 算法）

GJK 是一种更通用的凸体相交检测算法，利用了一个关键概念：**明可夫斯基差（Minkowski Difference）**。

### 明可夫斯基差

两个形状 A 和 B 的明可夫斯基差 A⊖B = {a - b | a ∈ A, b ∈ B}。

**关键性质**：A 和 B 相交，当且仅当 A⊖B 包含原点（零向量）。

```
A 和 B 不相交：
  A 的所有点 - B 的所有点，没有一个差值等于 (0,0)
  → A⊖B 不包含原点

A 和 B 相交：
  存在点 a∈A, b∈B 使得 a = b
  → a - b = (0,0) ∈ A⊖B
  → A⊖B 包含原点
```

GJK 不显式构造 A⊖B（可能有很多顶点），而是用迭代的方式判断原点是否在 A⊖B 内。

### Support 函数

GJK 的核心操作是**支撑函数（Support Function）**：给定一个方向 d，返回形状在该方向上最远的点。

```csharp
// 凸多边形的支撑函数
Vector2 Support(Vector2[] shape, Vector2 direction)
{
    float maxDot = float.NegativeInfinity;
    Vector2 farthest = Vector2.zero;
    foreach (var v in shape)
    {
        float d = Vector2.Dot(v, direction);
        if (d > maxDot) { maxDot = d; farthest = v; }
    }
    return farthest;
}

// 明可夫斯基差的支撑点：A 在 d 方向最远点 - B 在 -d 方向最远点
Vector2 MinkowskiSupport(Vector2[] shapeA, Vector2[] shapeB, Vector2 direction)
{
    return Support(shapeA, direction) - Support(shapeB, -direction);
}
```

### GJK 主循环（简化版）

```csharp
public static bool GJK(Vector2[] shapeA, Vector2[] shapeB)
{
    // 初始方向（任意，比如 A 中心指向 B 中心）
    Vector2 d = GetCenter(shapeB) - GetCenter(shapeA);

    // 单纯形（Simplex）：最多 3 个点（2D）或 4 个点（3D）
    var simplex = new List<Vector2>();
    simplex.Add(MinkowskiSupport(shapeA, shapeB, d));

    d = -simplex[0];  // 新方向指向原点

    for (int iter = 0; iter < 64; iter++)  // 最大迭代次数防止死循环
    {
        Vector2 a = MinkowskiSupport(shapeA, shapeB, d);

        if (Vector2.Dot(a, d) < 0)
            return false;  // 新点没有超过原点，原点不在 A⊖B 内 → 不相交

        simplex.Add(a);

        if (HandleSimplex(simplex, ref d))
            return true;  // 单纯形包含原点 → 相交
    }
    return false;
}
```

GJK 的完整实现较复杂（HandleSimplex 需要处理线段、三角形等多种情况），但对任意凸体都有效，而且速度极快（实际测试通常 2-4 次迭代就收敛）。

---

## EPA（Expanding Polytope Algorithm）：GJK 之后求穿透深度

GJK 只能判断是否相交，不能给出穿透深度。EPA 在 GJK 找到的单纯形基础上扩展，求出最小穿透向量：

```
GJK 确认相交后 → EPA 继续扩展单纯形
不断向外扩展直到找到最近边
最近边的法线方向 + 到原点距离 = 最小穿透向量（MTV）
```

---

## 游戏引擎中的使用

**Unity PhysX**：
- 内部用 GJK + EPA 做凸体窄相
- `Physics.ComputePenetration` 直接暴露了这个功能

```csharp
// Unity 的碰撞穿透向量计算
if (Physics.ComputePenetration(
    colliderA, posA, rotA,
    colliderB, posB, rotB,
    out Vector3 direction, out float distance))
{
    // direction: 推开方向
    // distance: 穿透深度
    transform.position += direction * distance;
}
```

**2D 游戏的简单碰撞**：SAT 就够用，实现简单，适合矩形和简单多边形。

**Rigidbody 物理响应**：GJK + EPA 给出法线和穿透深度 → 计算冲量 → 更新刚体速度。

---

## 小结

| | SAT | GJK + EPA |
|---|---|---|
| 适用形状 | 凸多边形 | 任意凸体（含曲面凸体） |
| 输出 | 是否相交 + MTV | 是否相交 + MTV（EPA）|
| 实现复杂度 | 简单 | 中等 |
| 性能 | O(n + m)，n/m 是边数 | 接近 O(1)，迭代少 |
| 3D 扩展 | 需要处理边叉积轴，较繁琐 | 天然支持任意维度 |

- **SAT**：2D 简单凸多边形的首选，实现直观，可直接给出最小穿透向量
- **GJK**：任意凸体，3D 首选，PhysX/Bullet 的标准窄相算法
- **EPA**：GJK 之后求穿透深度，完整的"碰撞检测 + 响应信息"流程
- **Unity 里**：`Physics.ComputePenetration` 直接用，不用自己实现 GJK；2D 碰撞用 Box2D（SAT based）
