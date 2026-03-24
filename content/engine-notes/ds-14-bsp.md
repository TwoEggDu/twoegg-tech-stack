---
title: "数据结构与算法 14｜BSP 树：静态室内场景的可见性与碰撞"
description: "BSP（Binary Space Partitioning）用平面递归地把空间分成两半，是 Quake / Doom 时代的核心技术，至今仍用于静态场景的预计算可见性、编译器 CSG 运算和精确碰撞检测。"
slug: "ds-14-bsp"
weight: 767
tags:
  - 软件工程
  - 数据结构
  - 算法
  - BSP
  - 空间分区
  - 游戏架构
series: "数据结构与算法"
---

> Doom（1993）用 BSP 树在 486 CPU 上实现了流畅的 3D 渲染——每帧用 BSP 确定绘制顺序，完全不需要深度缓冲。今天的 BSP 仍活跃在静态碰撞网格、编辑器的 CSG 笔刷运算和可见性预计算中。

---

## BSP 树的基本思想

选一个分割平面，把空间里的所有多边形分成两组：在平面"正面"的和在"背面"的。递归地对每组继续分割，形成二叉树。

```
选分割平面 P：
  前向子树（Front）：P 正面的所有多边形
  后向子树（Back）：P 背面的所有多边形

被 P 切割的多边形：沿 P 分成两个多边形，分别放入前/后子树
```

---

## 实现

```csharp
public class BSPNode
{
    public Plane   splitter;    // 分割平面
    public BSPNode front;       // 正面子树
    public BSPNode back;        // 背面子树
    public List<Triangle> triangles = new();  // 叶节点存储的三角面
    public bool IsLeaf => front == null && back == null;
}

// 判断三角形与分割平面的关系
enum PlaneRelation { Front, Back, Spanning, Coplanar }

PlaneRelation ClassifyTriangle(Triangle tri, Plane plane, float epsilon = 1e-5f)
{
    int front = 0, back = 0;
    foreach (var v in tri.vertices)
    {
        float d = plane.GetDistanceToPoint(v);
        if      (d >  epsilon) front++;
        else if (d < -epsilon) back++;
    }
    if (front > 0 && back == 0) return PlaneRelation.Front;
    if (back  > 0 && front == 0) return PlaneRelation.Back;
    if (front == 0 && back == 0) return PlaneRelation.Coplanar;
    return PlaneRelation.Spanning;
}

BSPNode Build(List<Triangle> triangles)
{
    if (triangles.Count == 0) return null;

    var node = new BSPNode();

    // 选分割平面（简单策略：用第一个三角形的平面）
    node.splitter = new Plane(triangles[0].normal, triangles[0].centroid);

    var frontList = new List<Triangle>();
    var backList  = new List<Triangle>();

    foreach (var tri in triangles)
    {
        switch (ClassifyTriangle(tri, node.splitter))
        {
            case PlaneRelation.Front:    frontList.Add(tri); break;
            case PlaneRelation.Back:     backList.Add(tri);  break;
            case PlaneRelation.Coplanar: node.triangles.Add(tri); break;
            case PlaneRelation.Spanning:
                // 切割三角形为前后两部分
                SplitTriangle(tri, node.splitter, frontList, backList);
                break;
        }
    }

    node.front = Build(frontList);
    node.back  = Build(backList);
    return node;
}
```

---

## BSP 的经典应用：画家算法渲染

Doom/Quake 时代的软件渲染没有 Z-buffer，用 BSP 确定从后往前的绘制顺序：

```csharp
// 相对于摄像机位置，BSP 树给出从后往前的多边形顺序
void RenderBSP(BSPNode node, Vector3 cameraPos, List<Triangle> sortedOutput)
{
    if (node == null) return;

    // 判断摄像机在分割平面的哪一侧
    float d = node.splitter.GetDistanceToPoint(cameraPos);

    if (d >= 0)
    {
        // 摄像机在正面：先渲染背面（离摄像机远），再渲染正面
        RenderBSP(node.back,  cameraPos, sortedOutput);
        sortedOutput.AddRange(node.triangles);
        RenderBSP(node.front, cameraPos, sortedOutput);
    }
    else
    {
        // 摄像机在背面：先渲染正面（离摄像机远），再渲染背面
        RenderBSP(node.front, cameraPos, sortedOutput);
        sortedOutput.AddRange(node.triangles);
        RenderBSP(node.back,  cameraPos, sortedOutput);
    }
}
// 结果：sortedOutput 是从后往前的多边形序列，直接按顺序画即可正确遮挡
```

---

## BSP 的现代应用

### 一、编辑器 CSG 笔刷运算

虚幻引擎（早期）、Hammer（Source 引擎编辑器）、TrenchBroom 都用 BSP 实现 CSG（构造实体几何）操作：

```
两个立方体 A 和 B：
  A Union B（并集）：两个立方体合并成一个形状
  A Subtract B（差集）：从 A 里挖掉 B 的形状（做房间的门洞）
  A Intersect B（交集）：只保留两者重叠的部分

CSG 流程：
1. 把 A 和 B 各自构建为 BSP 树
2. 用 A 的 BSP 树对 B 的几何体进行分类（哪些面在 A 内部/外部）
3. 根据运算类型（并/差/交）选择保留哪些面
4. 输出结果几何体
```

Unity Editor 的 ProBuilder 复杂布尔运算底层也是类似的 BSP 思路。

### 二、静态场景碰撞（Solid BSP / Leaf BSP）

**Solid BSP**：叶节点分为"实体（Solid）"和"空（Empty）"，一次点查询就能判断一个点是在墙内还是墙外：

```csharp
bool IsPointInSolid(BSPSolidNode node, Vector3 point)
{
    if (node.IsLeaf) return node.IsSolid;  // 叶节点：直接返回是否实体

    float d = node.splitter.GetDistanceToPoint(point);
    return d >= 0
        ? IsPointInSolid(node.front, point)
        : IsPointInSolid(node.back,  point);
}
// O(log n) 的点查询，比遍历所有三角面快很多
```

### 三、Quake 的 PVS（Potentially Visible Set）

Quake 把地图的叶节点预计算了 PVS（潜在可见集合）：

```
离线预计算：
  对每个叶节点（玩家可能所在的区域），计算"从这个位置可能看到哪些其他叶节点"
  结果压缩存储（行程长度编码）

运行时：
  找到玩家当前所在的叶节点 → O(log n)
  查表找到该叶节点的 PVS
  只绘制 PVS 内的几何体 → 大幅减少渲染量
```

这是 90 年代"室内场景渲染"的核心技术。现代室内场景（现代 FPS）用 Portal Rendering 或 Clustered Visibility 代替，但原理相通。

---

## BSP 分割平面的选择策略

好的分割平面让树尽量平衡，减少多边形被切割的次数：

```
评分函数：
score = w_balance × |frontCount - backCount|   // 惩罚不平衡（越小越好）
      + w_split   × splitCount                 // 惩罚切割数量（越小越好）

常用策略：
1. 随机采样一部分多边形，选评分最低的作为分割面
2. AABB 中轴分割（简单但切割多）
3. 选能使前后子树面积近似相等的平面（SAH 思路）
```

---

## BSP 的局限与替代

**BSP 的问题**：
- **只适合静态场景**：构建代价高（O(n²) 甚至更高），不适合动态物体
- **切割增加多边形数量**：一个多边形可能被切成很多片，内存占用大
- **构建时间长**：大型关卡需要分钟级别的预计算

**现代替代方案**：
- **静态场景碰撞**：预烘焙的凸包网格（Havok / PhysX 的 Mesh Collider）
- **可见性剔除**：Portal/Zone 系统（更适合走廊/房间结构），Umbra/Enlighten（自动化遮挡剔除）
- **CSG 编辑**：仍然用 BSP（没有更简单的替代）

---

## 小结

- **BSP**：用平面递归二分空间，静态场景的经典解法
- **画家算法**：BSP 的历史应用，从后往前的排序遍历，无需深度缓冲
- **Solid BSP**：叶节点标记"实体/空"，O(log n) 判断点是否在几何体内
- **PVS 预计算**：基于 BSP 的可见性预计算，Quake 时代室内渲染的核心
- **CSG 笔刷**：BSP 至今仍是编辑器布尔运算的最优方案
- **局限**：仅适合静态场景，动态物体改用 BVH 或空间哈希
