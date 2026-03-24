---
title: "数据结构与算法 12｜BVH：层次包围体树，光线追踪与碰撞检测的基础"
description: "BVH（Bounding Volume Hierarchy）是一种自底向上构建的树形空间结构，能适应物体分布不均匀的场景。是现代光线追踪、物理引擎碰撞宽相和 GPU 加速渲染的核心数据结构。"
slug: "ds-12-bvh"
weight: 763
tags:
  - 软件工程
  - 数据结构
  - 算法
  - BVH
  - 光线追踪
  - 碰撞检测
  - 游戏架构
series: "数据结构与算法"
---

> 四叉树/八叉树按固定的空间网格分割，当物体分布不均匀时会严重退化。BVH 反过来，按物体本身的分布来构建树——聚集的物体放进同一子树，分散的物体自然分开，始终保持平衡。

---

## BVH 的基本思想

**Bounding Volume Hierarchy**：用一棵树层次地组织包围体。

- **叶节点**：存储单个物体的包围体
- **内部节点**：存储其子节点的合并包围体（"这个子树内所有物体的 AABB"）

```
根节点（整个场景的 AABB）
├── 内部节点 A（左半部分物体的 AABB）
│   ├── 叶 1（物体 1 的 AABB）
│   ├── 叶 2（物体 2 的 AABB）
│   └── 叶 3（物体 3 的 AABB）
└── 内部节点 B（右半部分物体的 AABB）
    ├── 叶 4（物体 4 的 AABB）
    └── 叶 5（物体 5 的 AABB）
```

**与四叉树的区别**：
- 四叉树：固定的空间分割，不管物体在哪
- BVH：根据物体位置来构建，物体密集的地方树节点也密集

---

## BVH 的构建

### 自顶向下构建（Top-Down）

递归地把物体集合分成两组，为每组建子树：

```csharp
public class BVHNode
{
    public AABB aabb;
    public BVHNode left, right;
    public int objectIndex = -1;  // -1 = 内部节点；≥ 0 = 叶节点
}

BVHNode BuildBVH(int[] objectIndices, AABB[] aabbs)
{
    var node = new BVHNode();

    // 计算这组物体的合并 AABB
    node.aabb = aabbs[objectIndices[0]];
    for (int i = 1; i < objectIndices.Length; i++)
        node.aabb = AABB.Union(node.aabb, aabbs[objectIndices[i]]);

    // 叶节点：只有一个物体
    if (objectIndices.Length == 1)
    {
        node.objectIndex = objectIndices[0];
        return node;
    }

    // 分割：选最长轴，在中点处分割
    Vector3 size = node.aabb.max - node.aabb.min;
    int splitAxis = size.x > size.y
                    ? (size.x > size.z ? 0 : 2)
                    : (size.y > size.z ? 1 : 2);

    // 按选定轴的中心排序，分成左右两组
    float midValue = (node.aabb.min[splitAxis] + node.aabb.max[splitAxis]) * 0.5f;

    var left  = objectIndices.Where(i => GetCenter(aabbs[i])[splitAxis] < midValue).ToArray();
    var right = objectIndices.Where(i => GetCenter(aabbs[i])[splitAxis] >= midValue).ToArray();

    // 处理边界情况（所有物体都在同一侧）
    if (left.Length == 0 || right.Length == 0)
    {
        int half = objectIndices.Length / 2;
        left  = objectIndices[..half];
        right = objectIndices[half..];
    }

    node.left  = BuildBVH(left,  aabbs);
    node.right = BuildBVH(right, aabbs);
    return node;
}
```

### 更优的分割策略：SAH（Surface Area Heuristic）

中点分割简单但不优化查询代价。SAH 根据"切割后的遍历代价"选择最优分割面：

```
代价函数：
Cost(split) = C_traverse                    // 遍历内部节点的代价（常数）
            + P(left|parent)  × N_left  × C_intersect  // 左子树的期望代价
            + P(right|parent) × N_right × C_intersect  // 右子树的期望代价

P(left|parent) = SA(left) / SA(parent)     // 表面积比 = 光线击中的概率比
```

SAH 是光线追踪 BVH 的标准构建方法（Embree、OptiX 都用 SAH）。实现较复杂，游戏引擎的离线 BVH（光照烘焙、静态碰撞）通常用 SAH，实时动态 BVH 用更简单的分割策略。

---

## BVH 的查询

### 光线与 BVH 求交（Raycast）

```csharp
// 光线与 AABB 的快速求交（Slab Method）
bool RayAABB(Ray ray, AABB aabb, out float tMin, out float tMax)
{
    tMin = float.NegativeInfinity;
    tMax = float.PositiveInfinity;

    for (int i = 0; i < 3; i++)
    {
        float invD = 1f / ray.direction[i];
        float t0 = (aabb.min[i] - ray.origin[i]) * invD;
        float t1 = (aabb.max[i] - ray.origin[i]) * invD;
        if (invD < 0) (t0, t1) = (t1, t0);
        tMin = Mathf.Max(tMin, t0);
        tMax = Mathf.Min(tMax, t1);
        if (tMax < tMin) return false;
    }
    return tMax >= 0;
}

// BVH 光线求交：深度优先，发现不相交则剪枝整棵子树
bool RayCastBVH(BVHNode node, Ray ray, ref float tNearest, out int hitObject)
{
    hitObject = -1;

    if (!RayAABB(ray, node.aabb, out float tMin, out float tMax))
        return false;  // 光线不穿过这个节点的 AABB，整棵子树剪枝

    if (tMin > tNearest) return false;  // 已找到更近的交点，不用继续

    if (node.objectIndex >= 0)
    {
        // 叶节点：精确求交
        if (PreciseRayIntersect(ray, node.objectIndex, out float t) && t < tNearest)
        {
            tNearest  = t;
            hitObject = node.objectIndex;
            return true;
        }
        return false;
    }

    // 内部节点：先测试更近的子节点，提高剪枝效率
    bool hitLeft  = RayCastBVH(node.left,  ray, ref tNearest, out int leftHit);
    bool hitRight = RayCastBVH(node.right, ray, ref tNearest, out int rightHit);

    hitObject = hitRight >= 0 ? rightHit : leftHit;
    return hitLeft || hitRight;
}
```

### AABB 与 BVH 重叠查询（宽相碰撞检测）

```csharp
void QueryOverlap(BVHNode node, AABB queryAABB, List<int> results)
{
    if (!node.aabb.Overlaps(queryAABB)) return;  // 剪枝

    if (node.objectIndex >= 0)
    {
        results.Add(node.objectIndex);
        return;
    }

    QueryOverlap(node.left,  queryAABB, results);
    QueryOverlap(node.right, queryAABB, results);
}
```

---

## 动态 BVH

静态 BVH 建树代价高，不适合每帧大量物体移动的场景。动态 BVH 支持增量更新：

### 策略一：AABB 膨胀（Fat AABB）

给每个叶节点的 AABB 加上一个"缓冲距离"（Skin），物体在缓冲范围内移动不需要更新树：

```csharp
// Box2D / Bullet Physics 的策略
float skinWidth = 0.1f;  // 缓冲宽度
AABB fatAABB = new AABB
{
    min = obj.aabb.min - new Vector3(skinWidth, skinWidth, skinWidth),
    max = obj.aabb.max + new Vector3(skinWidth, skinWidth, skinWidth)
};
// 只有当物体移出 fatAABB 时，才重新插入树
```

### 策略二：旋转修复（Tree Rotation）

当叶节点更新时，检查并旋转树来保持平衡（类似 AVL 树的旋转）。Box2D 的 b2DynamicTree 使用此策略。

---

## 游戏引擎中的 BVH

**Unity 物理（PhysX）**：
- 静态物体（`isKinematic = true` 且不移动）：离线构建的 BVH，查询极快
- 动态物体：动态 AABB 树（Fat AABB 策略）
- `Physics.Raycast` 内部就是在 BVH 上做光线求交

**光照烘焙（Baked Lighting）**：
- Lightmap 烘焙时，光线追踪用 BVH 加速
- Unity 的 Progressive Lightmapper 使用 GPU 加速的 BVH 光线追踪

**Unity DOTS / Physics**：
- Unity Physics 包（ECS 物理）用 AABB 树作为宽相
- 比传统 PhysX 更适合大量动态物体（Data-Oriented，缓存友好）

---

## 小结

| | 四叉树/八叉树 | BVH |
|---|---|---|
| 构建方式 | 空间均匀分割 | 按物体聚类分割 |
| 物体分布不均匀 | 退化，树不平衡 | 始终适应物体分布 |
| 构建代价 | O(n log n) | O(n log²n)（SAH 更高） |
| 查询代价 | O(log n + k) | O(log n + k) |
| 动态更新 | 较易（重建节点） | 需要特殊维护（Fat AABB / 旋转） |
| 典型用途 | 2D 游戏、实时查询 | 光线追踪、物理引擎、不均匀场景 |

- **BVH 的核心优势**：适应物体分布，不会因为物体聚集而退化
- **SAH 构建**：代价最优的离线 BVH，光线追踪的标准选择
- **动态 BVH**：Fat AABB 减少更新频率，是实时物理引擎的主流方案
- **Unity 里的 BVH**：`Physics.Raycast`、光照烘焙、DOTS Physics 都在用
