---
title: "数据结构与算法 11｜四叉树与八叉树：空间分区的层次结构"
description: "四叉树（2D）和八叉树（3D）把空间递归地划分为子区域，让『找某个区域内的所有物体』从 O(n) 降到 O(log n + k)。是宽相碰撞检测、可见性剔除、区域查询的常用数据结构。"
slug: "ds-11-quadtree-octree"
weight: 761
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 空间分区
  - 碰撞检测
  - 游戏架构
series: "数据结构与算法"
---

> 策略游戏地图上有 5000 个单位，找"以玩家为圆心半径 10 格内的所有敌人"——暴力遍历 5000 个单位太慢。四叉树把地图递归分成小格，让查询只扫描相关区域，跳过绝大多数无关单位。

---

## 四叉树（Quadtree）

四叉树把 2D 空间递归地分成四个象限（NW、NE、SW、SE）。每个节点最多存 N 个对象，超过阈值就分裂为四个子节点。

```
根节点（整张地图）
├── NW（左上四分之一）
│   ├── NW NW（进一步分裂）
│   └── ...
├── NE（右上）
├── SW（左下）
└── SE（右下）
```

### 实现

```csharp
public class Quadtree<T>
{
    private const int MAX_OBJECTS = 8;   // 超过 8 个对象就分裂
    private const int MAX_DEPTH   = 8;   // 最多分 8 层

    private Rect      bounds;
    private int       depth;
    private List<(Rect aabb, T obj)> objects = new();
    private Quadtree<T>[] children;      // null = 叶节点

    public Quadtree(Rect bounds, int depth = 0)
    {
        this.bounds = bounds;
        this.depth  = depth;
    }

    public void Insert(Rect aabb, T obj)
    {
        // 如果已分裂，插入到覆盖该 AABB 的子节点
        if (children != null)
        {
            foreach (var child in children)
                if (child.bounds.Overlaps(aabb))
                    child.Insert(aabb, obj);
            return;
        }

        objects.Add((aabb, obj));

        // 超过容量且未达最大深度：分裂
        if (objects.Count > MAX_OBJECTS && depth < MAX_DEPTH)
            Split();
    }

    private void Split()
    {
        float hw = bounds.width  * 0.5f;
        float hh = bounds.height * 0.5f;
        float x  = bounds.x, y = bounds.y;

        children = new Quadtree<T>[4]
        {
            new(new Rect(x,      y + hh, hw, hh), depth + 1),  // NW
            new(new Rect(x + hw, y + hh, hw, hh), depth + 1),  // NE
            new(new Rect(x,      y,      hw, hh), depth + 1),  // SW
            new(new Rect(x + hw, y,      hw, hh), depth + 1),  // SE
        };

        // 把当前节点的对象重新分配到子节点
        foreach (var (aabb, obj) in objects)
            foreach (var child in children)
                if (child.bounds.Overlaps(aabb))
                    child.Insert(aabb, obj);

        objects.Clear();
    }

    // 查询：找出与 queryRect 重叠的所有对象
    public void Query(Rect queryRect, List<T> results)
    {
        if (!bounds.Overlaps(queryRect)) return;  // 当前节点与查询区域无交叉，剪枝

        foreach (var (aabb, obj) in objects)
            if (aabb.Overlaps(queryRect))
                results.Add(obj);

        if (children != null)
            foreach (var child in children)
                child.Query(queryRect, results);
    }
}
```

### 使用示例

```csharp
// 建树
var qt = new Quadtree<Unit>(new Rect(0, 0, 1024, 1024));
foreach (var unit in allUnits)
    qt.Insert(unit.GetAABB(), unit);

// 查询：找玩家附近 50 单位内的所有敌人
var queryRect = new Rect(player.pos.x - 50, player.pos.y - 50, 100, 100);
var nearbyUnits = new List<Unit>();
qt.Query(queryRect, nearbyUnits);
// 只遍历了少量相关节点，而不是全部 5000 个单位
```

---

## 四叉树的更新策略

四叉树的弱点：**动态场景下更新代价高**。每帧大量物体移动时，需要频繁删除和重新插入。

### 策略一：每帧重建

```csharp
// 最简单粗暴，但也出人意料地高效（因为重建是自上而下的连续内存操作）
// 适合物体数量中等（< 1000）的场景
void Update()
{
    quadtree.Clear();
    foreach (var unit in allUnits)
        quadtree.Insert(unit.GetAABB(), unit);
}
```

### 策略二：松散四叉树（Loose Quadtree）

每个节点的边界扩大 2 倍（overlap），物体只插入"中心点所在"的那一个节点，避免跨越边界时的重复存储和频繁迁移：

```csharp
// 松散四叉树：节点边界扩展，物体按中心点分配
// 优点：物体不会因为轻微移动而跨节点迁移
// 缺点：查询时需要考虑扩展后的边界
```

### 策略三：分层更新

快速移动的物体（子弹、粒子）每帧更新，缓慢移动的物体（建筑、静态障碍）只在移动时更新：

```csharp
// 静态四叉树：只存静态物体，每次场景变化时重建一次
var staticQt = new Quadtree<StaticObstacle>(...);

// 动态四叉树：存动态物体，每帧重建
var dynamicQt = new Quadtree<Unit>(...);

// 查询时合并两者的结果
```

---

## 八叉树（Octree）

八叉树是四叉树的 3D 版本，把空间分成 8 个子立方体。实现与四叉树几乎相同，只是方向从 4 个变成 8 个：

```csharp
public class Octree<T>
{
    private Bounds   bounds;
    private Octree<T>[] children;   // 8 个子节点
    private List<(Bounds aabb, T obj)> objects = new();

    private void Split()
    {
        Vector3 c  = bounds.center;
        Vector3 hs = bounds.extents * 0.5f;  // 半尺寸

        children = new Octree<T>[8]
        {
            new(new Bounds(c + new Vector3(-hs.x,  hs.y, -hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3( hs.x,  hs.y, -hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3(-hs.x, -hs.y, -hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3( hs.x, -hs.y, -hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3(-hs.x,  hs.y,  hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3( hs.x,  hs.y,  hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3(-hs.x, -hs.y,  hs.z), bounds.size * 0.5f)),
            new(new Bounds(c + new Vector3( hs.x, -hs.y,  hs.z), bounds.size * 0.5f)),
        };
        // 重新分配对象...
    }
}
```

---

## 视锥剔除（Frustum Culling）与八叉树

八叉树最重要的游戏应用之一：**只渲染摄像机视锥内的物体**。

```csharp
// 用八叉树快速筛选出在视锥内的 Renderer
void CollectVisibleRenderers(Octree<Renderer> tree,
                             Plane[] frustumPlanes,
                             List<Renderer> visible)
{
    // 如果整个节点的 AABB 在视锥外，整棵子树都跳过
    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, tree.bounds))
        return;

    // 如果整个节点的 AABB 完全在视锥内，整棵子树都可见，无需递归检测
    // （可进一步优化，这里略去）

    // 检测当前节点的物体
    foreach (var (aabb, renderer) in tree.objects)
        if (GeometryUtility.TestPlanesAABB(frustumPlanes, aabb))
            visible.Add(renderer);

    // 递归检测子节点
    if (tree.children != null)
        foreach (var child in tree.children)
            CollectVisibleRenderers(child, frustumPlanes, visible);
}

// 实际使用（Unity 已内置，这里用于理解原理）
Camera.main.CalculateFrustumPlanes(out var planes);
var visible = new List<Renderer>();
octree.CollectVisibleRenderers(planes, visible);
// 只渲染 visible 里的物体
```

---

## 四叉树 vs 八叉树 vs 其他空间结构

| 结构 | 维度 | 分裂方式 | 适用场景 |
|---|---|---|---|
| 四叉树 | 2D | 4等分 | 2D 游戏、俯视角地图、RTS 单位查询 |
| 八叉树 | 3D | 8等分 | 3D 场景、视锥剔除、3D 碰撞宽相 |
| BVH（DS-12） | 任意 | 按物体聚类 | 光线追踪、物体分布不均匀的场景 |
| 空间哈希（DS-13） | 任意 | 均匀网格 | 密度均匀、动态更新频繁的场景 |
| BSP 树（DS-14） | 任意 | 任意平面切割 | 静态室内场景、可见性计算 |

**四叉树/八叉树的问题**：当物体分布不均匀时（比如大量物体聚集在地图一角），树会严重不平衡，查询退化。BVH（DS-12）解决了这个问题。

---

## 小结

- **四叉树**：2D 空间递归四分，区域查询从 O(n) 降到 O(log n + k)，k 是结果数
- **八叉树**：3D 版本，实现相同，多了 4 个子节点
- **分裂时机**：超过容量阈值时分裂；阈值越小树越深，查询越快但内存和建树代价越高
- **动态场景**：每帧重建适合中小场景；松散四叉树减少对象迁移；分层（静/动分离）用于大场景
- **视锥剔除**：八叉树的经典用途，整个子树 AABB 在视锥外时可跳过整棵子树
- **物体分布不均匀**：改用 BVH（DS-12）
