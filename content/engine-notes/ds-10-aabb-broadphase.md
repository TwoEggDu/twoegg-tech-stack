---
date: "2026-03-26"
title: "数据结构与算法 10｜AABB 与碰撞宽相：空间查询的第一道过滤"
description: "碰撞检测分两阶段：宽相（快速排除大多数不可能碰撞的对）和窄相（精确检测剩余的候选对）。AABB 是宽相的基础数据结构，Sort and Sweep 是宽相的经典算法。这篇讲清楚为什么需要宽相、AABB 怎么工作，以及游戏引擎如何利用它们。"
slug: "ds-10-aabb-broadphase"
weight: 759
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 碰撞检测
  - 物理
  - 游戏架构
series: "数据结构与算法"
---

> 场景里有 1000 个物体，暴力检测所有两两碰撞对需要 C(1000, 2) ≈ 50 万次检测——每帧。宽相的目标是把候选对数量从 O(n²) 降到接近 O(n)，再交给精确（但昂贵）的窄相处理。

---

## 为什么需要两阶段碰撞检测

```
暴力检测：
for i in 0..n:
  for j in i+1..n:
    if PreciseCollide(objects[i], objects[j]):  // 昂贵
      HandleCollision(i, j)
// 复杂度：O(n²) × 每次精确检测的代价
```

n=1000 时，每帧 50 万次精确检测（凸包相交、GJK 等），完全不可接受。

**两阶段策略**：
- **宽相（Broad Phase）**：用简单的包围体快速筛选出"可能碰撞"的候选对。快但不精确，可能有误报，不会漏报。
- **窄相（Narrow Phase）**：对候选对做精确的几何相交检测。精确但昂贵，只处理宽相筛出的少量候选对。

---

## AABB（轴对齐包围盒）

**Axis-Aligned Bounding Box**：与坐标轴平行的矩形（2D）或长方体（3D）。

```
2D AABB：用两个点定义
  min = (left, bottom)
  max = (right, top)

3D AABB：
  min = (minX, minY, minZ)
  max = (maxX, maxY, maxZ)
```

```csharp
public struct AABB
{
    public Vector3 min;
    public Vector3 max;

    public Vector3 Center => (min + max) * 0.5f;
    public Vector3 Size   => max - min;

    // 两个 AABB 是否重叠：三轴都重叠才算重叠
    public bool Overlaps(AABB other)
    {
        return min.x <= other.max.x && max.x >= other.min.x
            && min.y <= other.max.y && max.y >= other.min.y
            && min.z <= other.max.z && max.z >= other.min.z;
    }

    // 点是否在 AABB 内
    public bool Contains(Vector3 point)
    {
        return point.x >= min.x && point.x <= max.x
            && point.y >= min.y && point.y <= max.y
            && point.z >= min.z && point.z <= max.z;
    }

    // 从碰撞体的 Renderer 或 Collider 构建
    public static AABB FromCollider(Collider col)
    {
        var bounds = col.bounds;
        return new AABB { min = bounds.min, max = bounds.max };
    }

    // 合并两个 AABB（用于 BVH 构建，见 DS-12）
    public static AABB Union(AABB a, AABB b)
    {
        return new AABB
        {
            min = Vector3.Min(a.min, b.min),
            max = Vector3.Max(a.max, b.max)
        };
    }
}
```

**为什么选 AABB 而不是球体或 OBB？**

```
包围体对比（精度 vs 计算速度）：

球体：最快（球-球相交 = 距离² vs 半径和²），但"包围"不精确（旋转细长物体时包围球很大）
AABB：快，对大多数形状包围较紧，相交测试简单（逐轴比较）
OBB（有向包围盒）：包围更紧，但相交测试需要 SAT（15 轴），代价高很多
凸包：最精确，代价最高

宽相：用球体或 AABB——速度优先
窄相：用 OBB 或凸包——精度优先
```

---

## 暴力 AABB 宽相

最简单的宽相：对所有 AABB 两两检测重叠。

```csharp
// O(n²)，只适合 n < 50 的小场景
List<(int, int)> BruteForceAABB(AABB[] aabbs)
{
    var pairs = new List<(int, int)>();
    for (int i = 0; i < aabbs.Length; i++)
    for (int j = i + 1; j < aabbs.Length; j++)
        if (aabbs[i].Overlaps(aabbs[j]))
            pairs.Add((i, j));
    return pairs;
}
```

---

## Sort and Sweep（扫描线算法）

**核心洞察**：如果两个 AABB 在 X 轴上不重叠，它们一定不碰撞。把所有 AABB 的 X 轴区间排好序，用一次扫描就能快速找到所有在 X 轴上重叠的对，然后再检查 Y/Z 轴。

```csharp
public class SortAndSweep
{
    // 将每个 AABB 的 min.x 和 max.x 作为"事件点"
    struct Endpoint
    {
        public float value;
        public int   objectIndex;
        public bool  isStart;   // true = min.x（开始），false = max.x（结束）
    }

    public List<(int, int)> FindOverlappingPairs(AABB[] aabbs)
    {
        // 收集所有端点
        var endpoints = new Endpoint[aabbs.Length * 2];
        for (int i = 0; i < aabbs.Length; i++)
        {
            endpoints[2 * i]     = new Endpoint { value = aabbs[i].min.x, objectIndex = i, isStart = true  };
            endpoints[2 * i + 1] = new Endpoint { value = aabbs[i].max.x, objectIndex = i, isStart = false };
        }

        // 按 X 坐标排序
        Array.Sort(endpoints, (a, b) => a.value.CompareTo(b.value));

        var active = new HashSet<int>();  // 当前"活跃"的 AABB（已经开始但未结束）
        var pairs  = new List<(int, int)>();

        foreach (var ep in endpoints)
        {
            if (ep.isStart)
            {
                // 新 AABB 进入：与所有已活跃的 AABB 在 X 轴上重叠
                // 再检查 Y/Z 轴是否也重叠
                foreach (int activeIdx in active)
                {
                    var a = aabbs[ep.objectIndex];
                    var b = aabbs[activeIdx];
                    // 只需检查 Y 和 Z（X 轴已知重叠）
                    if (a.min.y <= b.max.y && a.max.y >= b.min.y
                     && a.min.z <= b.max.z && a.max.z >= b.min.z)
                    {
                        pairs.Add((Mathf.Min(ep.objectIndex, activeIdx),
                                   Mathf.Max(ep.objectIndex, activeIdx)));
                    }
                }
                active.Add(ep.objectIndex);
            }
            else
            {
                // AABB 退出
                active.Remove(ep.objectIndex);
            }
        }

        return pairs;
    }
}
```

**复杂度**：
- 最好（稀疏场景，很少重叠）：O(n log n)（排序主导）
- 最坏（所有物体挤在一起）：O(n²)（仍然退化）
- 实际游戏场景：接近 O(n log n)，因为同时重叠的物体不多

**帧间优化**：每帧物体移动不大，端点数组基本有序，插入排序的 O(n) 最好情况远好于完整快排的 O(n log n)。Unity Physics 和 Box2D 都用了这个技巧（Incremental Sort and Sweep）。

---

## 增量更新（Incremental Sort and Sweep）

物体每帧只移动一小段距离，端点数组的顺序变化很小。用插入排序维护有序性比每帧重新排序快得多：

```csharp
// 每帧只更新移动过的物体的端点位置
// 用插入排序局部修复顺序（近似有序时接近 O(n)）
void UpdateEndpoint(List<Endpoint> sorted, int idx, float newValue)
{
    sorted[idx] = sorted[idx] with { value = newValue };

    // 向左冒泡（如果变小了）
    int i = idx;
    while (i > 0 && sorted[i].value < sorted[i-1].value)
    {
        // 交换时检测配对状态变化（start 和 end 相对顺序改变意味着重叠状态变化）
        (sorted[i], sorted[i-1]) = (sorted[i-1], sorted[i]);
        i--;
    }
    // 向右同理
}
```

---

## Unity 的宽相实现

Unity Physics（Box2D / PhysX）宽相使用：

- **2D（Box2D）**：Dynamic AABB Tree（每个物体的 AABB 存在 BVH 树里，DS-12）
- **3D（PhysX）**：SAP（Sort And Prune，Sort and Sweep 的变体）+ MBP（Multi Box Pruning，把世界分成网格）

```csharp
// Unity 里直接用 Physics.OverlapBox / Physics.BoxCast 做空间查询
// 内部就是宽相查询
Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, layerMask);

// Physics2D 类似
Collider2D[] hits2D = Physics2D.OverlapBoxAll(center, size, angle, layerMask);
```

---

## 小结

- **两阶段碰撞检测**：宽相（AABB 快速筛选）+ 窄相（精确检测），把 O(n²) 降到接近 O(n)
- **AABB**：逐轴比较的快速相交测试，缺点是旋转物体时包围不精确
- **Sort and Sweep**：排序 X 轴端点，扫描一遍找出所有 X 轴重叠的对，再检查 Y/Z
- **增量更新**：利用帧间连续性，插入排序维护有序性，比每帧重排快
- **更复杂的宽相**：四叉树/八叉树（DS-11）、BVH（DS-12）、空间哈希（DS-13）——各有适用场景
