---
date: "2026-03-26"
title: "数据结构与算法 13｜空间哈希：密集动态场景的 O(1) 近邻查询"
description: "空间哈希把连续空间映射到哈希表，适合物体密集且频繁移动的场景（子弹地狱、大量 NPC、多人游戏）。插入、删除、近邻查询都接近 O(1)，且实现非常简单。"
slug: "ds-13-spatial-hashing"
weight: 765
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 空间分区
  - 哈希
  - 游戏架构
series: "数据结构与算法"
---

> 子弹地狱游戏里有 500 颗子弹和 200 个敌人，每帧都要检测哪些子弹打中了哪些敌人。四叉树建树代价高，BVH 动态维护复杂。空间哈希：把物体的格子坐标哈希一下，O(1) 插入删除，O(1) 近邻查询，每帧重建也很快。

---

## 基本思路

把 2D/3D 空间划分成固定大小的格子（Cell），每个格子用哈希表的一个桶存储落在该格子里的物体。

```
世界坐标 → 格子坐标 → 哈希值 → 哈希表桶

(x=34.5, y=67.2) → cellSize=10 → cell(3, 6) → hash(3,6) → bucket[hash]
```

**核心操作**：
- 格子坐标：`cellX = floor(x / cellSize)`
- 哈希函数：把 (cellX, cellY) 映射到整数索引

---

## 实现

```csharp
public class SpatialHash<T>
{
    private readonly float cellSize;
    private readonly Dictionary<long, List<T>> buckets = new();

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
    }

    // 世界坐标 → 格子坐标
    private (int cx, int cy) WorldToCell(Vector2 pos)
    {
        return (Mathf.FloorToInt(pos.x / cellSize),
                Mathf.FloorToInt(pos.y / cellSize));
    }

    // 格子坐标 → 哈希键（用大质数避免碰撞）
    private static long CellKey(int cx, int cy)
    {
        // 把两个 int 合并成一个 long 作为键
        return ((long)(cx + 32768)) << 32 | (uint)(cy + 32768);
        // 或者用质数哈希（避免整数溢出时也适用）：
        // unchecked { return (long)cx * 1_000_003L + cy; }
    }

    private List<T> GetOrCreateBucket(long key)
    {
        if (!buckets.TryGetValue(key, out var bucket))
        {
            bucket = new List<T>();
            buckets[key] = bucket;
        }
        return bucket;
    }

    // 插入一个点状物体
    public void Insert(Vector2 pos, T obj)
    {
        var (cx, cy) = WorldToCell(pos);
        GetOrCreateBucket(CellKey(cx, cy)).Add(obj);
    }

    // 插入一个有体积的物体（可能跨越多个格子）
    public void InsertAABB(Vector2 min, Vector2 max, T obj)
    {
        var (minCx, minCy) = WorldToCell(min);
        var (maxCx, maxCy) = WorldToCell(max);

        for (int cx = minCx; cx <= maxCx; cx++)
        for (int cy = minCy; cy <= maxCy; cy++)
            GetOrCreateBucket(CellKey(cx, cy)).Add(obj);
    }

    // 查询：找以 center 为中心，radius 为半径的圆内的所有物体
    public List<T> QueryRadius(Vector2 center, float radius)
    {
        var results = new List<T>();
        var seen    = new HashSet<T>();  // 避免跨格子物体重复出现

        var (minCx, minCy) = WorldToCell(center - new Vector2(radius, radius));
        var (maxCx, maxCy) = WorldToCell(center + new Vector2(radius, radius));

        float r2 = radius * radius;

        for (int cx = minCx; cx <= maxCx; cx++)
        for (int cy = minCy; cy <= maxCy; cy++)
        {
            if (!buckets.TryGetValue(CellKey(cx, cy), out var bucket)) continue;
            foreach (var obj in bucket)
            {
                if (seen.Add(obj))
                    results.Add(obj);  // 精确距离筛选可在此处添加
            }
        }
        return results;
    }

    public void Clear() => buckets.Clear();
}
```

---

## 每帧重建的高效实现

对于高度动态的场景（每帧所有物体都在移动），最简单的策略是**每帧清空后重建**。

```csharp
public class DynamicSpatialHash<T>
{
    private readonly float cellSize;
    // 用固定大小的数组代替 Dictionary，避免 GC
    private readonly List<T>[] table;
    private readonly int tableSize;

    public DynamicSpatialHash(float cellSize, int tableSize = 4096)
    {
        this.cellSize  = cellSize;
        this.tableSize = tableSize;
        table = new List<T>[tableSize];
        for (int i = 0; i < tableSize; i++)
            table[i] = new List<T>();
    }

    private int Hash(int cx, int cy)
    {
        // 大质数哈希，把网格坐标映射到 [0, tableSize)
        unchecked
        {
            int h = cx * 374761393 + cy * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return Math.Abs(h) % tableSize;
        }
    }

    public void Insert(Vector2 pos, T obj)
    {
        int cx = Mathf.FloorToInt(pos.x / cellSize);
        int cy = Mathf.FloorToInt(pos.y / cellSize);
        table[Hash(cx, cy)].Add(obj);
    }

    public void Query(Vector2 center, float radius, List<T> results)
    {
        int minCx = Mathf.FloorToInt((center.x - radius) / cellSize);
        int maxCx = Mathf.FloorToInt((center.x + radius) / cellSize);
        int minCy = Mathf.FloorToInt((center.y - radius) / cellSize);
        int maxCy = Mathf.FloorToInt((center.y + radius) / cellSize);

        for (int cx = minCx; cx <= maxCx; cx++)
        for (int cy = minCy; cy <= maxCy; cy++)
            foreach (var obj in table[Hash(cx, cy)])
                results.Add(obj);
        // 注意：固定大小哈希表可能有碰撞（不同格子映射到同一桶）
        // 需要在结果里二次过滤，或接受少量误报
    }

    // 每帧重建：只清空列表，不释放内存（避免 GC）
    public void Clear()
    {
        foreach (var bucket in table)
            bucket.Clear();
    }
}
```

---

## 游戏应用

### 子弹与敌人碰撞检测

```csharp
public class BulletCollisionSystem : MonoBehaviour
{
    private DynamicSpatialHash<Enemy> enemyHash;
    private float cellSize = 2f;  // 格子大小 ≈ 敌人碰撞体直径

    void Update()
    {
        // 每帧重建敌人空间哈希（O(n)）
        enemyHash.Clear();
        foreach (var enemy in EnemyManager.All)
            enemyHash.Insert(enemy.position, enemy);

        // 检测所有子弹（O(m × 查询候选数)，候选数接近常数）
        var candidates = new List<Enemy>();
        foreach (var bullet in BulletManager.All)
        {
            candidates.Clear();
            enemyHash.Query(bullet.position, bullet.radius + maxEnemyRadius, candidates);

            foreach (var enemy in candidates)
                if (Vector2.Distance(bullet.position, enemy.position)
                    < bullet.radius + enemy.radius)
                    HandleHit(bullet, enemy);
        }
    }
}
```

### 多人游戏的玩家近邻查询

```csharp
// 服务器端：找某个玩家周围的所有其他玩家（用于兴趣管理）
spatialHash.Clear();
foreach (var player in allPlayers)
    spatialHash.Insert(player.position, player.id);

// 找每个玩家视野范围内的其他玩家（需要同步状态给他们）
foreach (var player in allPlayers)
{
    var visible = new List<int>();
    spatialHash.Query(player.position, player.viewRadius, visible);
    SyncStateToPlayer(player, visible);
}
```

### NPC 群体行为（Flocking / 鸟群模拟）

```csharp
// Boids 算法：每个 Boid 需要找邻近的其他 Boid
// 空间哈希让每帧查询从 O(n²) 降到 O(n × 近邻数)
void UpdateFlocking(List<Boid> boids)
{
    spatialHash.Clear();
    foreach (var boid in boids)
        spatialHash.Insert(boid.position, boid);

    foreach (var boid in boids)
    {
        var neighbors = spatialHash.Query(boid.position, boid.neighborRadius);
        boid.ApplyFlockingRules(neighbors);  // 分离、对齐、聚合
    }
}
```

---

## 格子大小的选择

**格子大小** 是空间哈希最重要的参数：

```
格子太小：
  每个格子里物体很少，查询准确（几乎无无关结果）
  但物体跨越很多格子（插入时要写入大量格子）
  内存占用高

格子太大：
  每个格子里物体很多，查询结果里很多无关物体（需要二次过滤）
  物体大多数只在一个格子里（插入快）
  查询检查的格子数少

经验法则：格子大小 ≈ 典型物体的碰撞体直径 × 2~3
```

```csharp
// 一般做法：cellSize = 查询半径 × 2
// 这样每次查询最多检查 3×3 = 9 个格子（2D），27 个格子（3D）
float queryRadius = 5f;
float cellSize    = queryRadius * 2f;  // = 10f
```

---

## 与其他空间结构的对比

| | 空间哈希 | 四叉树 | BVH |
|---|---|---|---|
| 插入 | O(1) | O(log n) | O(log n) |
| 删除 | O(1) | O(log n) | O(log n) |
| 近邻查询 | O(1) 平均 | O(log n + k) | O(log n + k) |
| 每帧重建 | O(n)，快 | O(n log n)，慢 | O(n log²n)，很慢 |
| 不均匀分布 | 可能退化（一个格子物体太多） | 四叉树退化 | 不退化 |
| 内存 | 固定（数组大小预设） | 动态 | 动态 |
| 实现复杂度 | 极简 | 中等 | 复杂 |

**选空间哈希的场景**：
- 物体密度均匀（子弹、鸟群、粒子）
- 高度动态（每帧所有物体都移动）
- 需要极简实现（Jam 游戏、原型）

---

## 小结

- **空间哈希**：世界坐标 → 格子坐标 → 哈希键，O(1) 插入删除和近邻查询
- **每帧重建**：对完全动态的场景，每帧 Clear + 重新插入比维护动态树更简单高效
- **固定大小数组**：用整数数组代替 Dictionary，消除 GC，适合高频更新
- **格子大小**：约等于查询半径的 2 倍，每次查询检查约 9 个格子（2D）
- **哈希冲突**：不同格子可能映射到同一桶，查询结果需要二次过滤（精确距离检测）
- **最适合**：密集动态场景；物体分布不均匀时改用 BVH
