---
date: "2026-03-26"
title: "数据结构与算法 06｜Dijkstra → A*：最短路径原理与启发函数"
description: "BFS 在无权图里找最短路，Dijkstra 把它推广到带权图，A* 在 Dijkstra 基础上加入启发函数让搜索方向更聪明。这篇讲清楚三者的演进关系，以及为什么 A* 是游戏寻路的标准算法。"
slug: "ds-06-dijkstra-astar"
weight: 751
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 寻路
  - A*
  - 游戏架构
series: "数据结构与算法"
---

> BFS 解决"步数最少"，Dijkstra 解决"代价最小"，A* 解决"代价最小且搜索要快"。三个算法解决的是同一个问题的不同版本。

---

## 问题升级：带权图的最短路径

BFS 假设每步代价相同（都是 1）。但真实的游戏地图里，代价通常不同：

```
地形代价：
  草地：1
  泥地：3
  山地：5
  道路：0.5
```

在这种情况下，"步数最少的路径"不等于"代价最小的路径"：

```
A → 草地 → 草地 → 草地 → B   代价 = 3 步
A → 道路 → 道路 → 道路 → 道路 → B   代价 = 4 步 × 0.5 = 2（更快！）
```

BFS 会选步数最少的（前者），但游戏里玩家/NPC 应该走代价最小的（后者）。

---

## Dijkstra：带权图的最短路径

Dijkstra 的思路：维护一个"到达每个节点的最小已知代价"，每次从中取出代价最小的节点扩展。

```csharp
Dictionary<Vector2Int, float> Dijkstra(
    Vector2Int start, Vector2Int target,
    Func<Vector2Int, List<(Vector2Int, float)>> getNeighbors)
{
    // dist[node] = 从 start 到 node 的最小已知代价
    var dist   = new Dictionary<Vector2Int, float>();
    var parent = new Dictionary<Vector2Int, Vector2Int>();
    // 优先队列：(代价, 节点)，代价小的先出
    var pq = new PriorityQueue<Vector2Int, float>();

    dist[start] = 0f;
    pq.Enqueue(start, 0f);

    while (pq.Count > 0)
    {
        var curr = pq.Dequeue();

        if (curr == target) break;  // 找到目标，提前终止

        foreach (var (neighbor, cost) in getNeighbors(curr))
        {
            float newDist = dist[curr] + cost;

            if (!dist.TryGetValue(neighbor, out float oldDist) || newDist < oldDist)
            {
                dist[neighbor] = newDist;
                parent[neighbor] = curr;
                pq.Enqueue(neighbor, newDist);
            }
        }
    }

    return dist;  // 返回从 start 到所有节点的最短代价
}
```

**Dijkstra 的正确性**：每次从优先队列里取出的节点，其代价已经是最小值（贪心性质）。只要边的代价非负，Dijkstra 保证找到最短路径。

**复杂度**：O((V + E) log V)，使用二叉堆优先队列时。

---

## Dijkstra 的问题：盲目搜索

Dijkstra 会向所有方向均匀扩展，直到找到目标：

```
起点在左下角，目标在右上角

Dijkstra 的搜索范围（近似）：
■■■■■■■■■
■■■■■■■■■
■■■■■■■■■
■■■■■■■■■    ← 探索了大量"错误方向"的节点
■S□□□□□□T
```

在一个 100×100 的地图里，Dijkstra 可能要探索接近 10,000 个节点才能找到目标——即使目标就在"直线方向"上。

**根本问题**：Dijkstra 不知道目标在哪个方向，只能盲目扩展。

---

## A*：加入启发函数，引导搜索方向

A* 在 Dijkstra 的基础上，给每个节点增加一个**启发值 h(n)**——"从这个节点到目标的预估代价"。

```
f(n) = g(n) + h(n)

g(n)：从起点到节点 n 的实际已知代价（Dijkstra 里的 dist）
h(n)：从节点 n 到目标的预估代价（启发函数）
f(n)：总预估代价（用这个值决定优先扩展哪个节点）
```

```csharp
List<Vector2Int> AStar(
    Vector2Int start, Vector2Int target,
    Func<Vector2Int, List<(Vector2Int, float)>> getNeighbors,
    Func<Vector2Int, float> heuristic)  // 启发函数
{
    var gCost  = new Dictionary<Vector2Int, float>();
    var parent = new Dictionary<Vector2Int, Vector2Int>();
    // 优先队列按 f(n) = g(n) + h(n) 排序
    var open   = new PriorityQueue<Vector2Int, float>();
    var closed = new HashSet<Vector2Int>();

    gCost[start] = 0f;
    open.Enqueue(start, heuristic(start));

    while (open.Count > 0)
    {
        var curr = open.Dequeue();

        if (curr == target)
            return ReconstructPath(parent, start, target);

        if (closed.Contains(curr)) continue;  // 已处理过，跳过
        closed.Add(curr);

        foreach (var (neighbor, moveCost) in getNeighbors(curr))
        {
            if (closed.Contains(neighbor)) continue;

            float newG = gCost[curr] + moveCost;

            if (!gCost.TryGetValue(neighbor, out float oldG) || newG < oldG)
            {
                gCost[neighbor] = newG;
                parent[neighbor] = curr;
                float f = newG + heuristic(neighbor);
                open.Enqueue(neighbor, f);
            }
        }
    }

    return null;  // 无路可走
}
```

A* 与 Dijkstra 的唯一区别：**优先队列里用 f(n) 而不是 g(n) 排序**。

---

## 启发函数：A* 的核心

启发函数 h(n) 决定了 A* 的行为：

### 曼哈顿距离（4方向格子地图）

```csharp
float ManhattanHeuristic(Vector2Int n, Vector2Int target)
{
    return Mathf.Abs(n.x - target.x) + Mathf.Abs(n.y - target.y);
}
```

适合只能上下左右移动的格子地图（无障碍物时，曼哈顿距离恰好等于实际最短路）。

### 切比雪夫距离（8方向格子地图）

```csharp
float ChebyshevHeuristic(Vector2Int n, Vector2Int target)
{
    int dx = Mathf.Abs(n.x - target.x);
    int dy = Mathf.Abs(n.y - target.y);
    return Mathf.Max(dx, dy);  // 8方向移动，斜着走和直走代价相同
}
```

### 欧几里得距离（自由移动/导航网格）

```csharp
float EuclideanHeuristic(Vector2Int n, Vector2Int target)
{
    float dx = n.x - target.x;
    float dy = n.y - target.y;
    return Mathf.Sqrt(dx * dx + dy * dy);
}
// 导航网格用 Vector3.Distance
float NavMeshHeuristic(Vector3 n, Vector3 target)
{
    return Vector3.Distance(n, target);
}
```

---

## 启发函数的"可接受性"（Admissibility）

**可接受的启发函数（Admissible）**：h(n) 永远不高估实际代价。即 h(n) ≤ 真实最短路代价。

```
如果 h(n) 满足可接受性：A* 保证找到最短路径
如果 h(n) 不满足：A* 可能找到次优路径，但搜索更快（Weighted A*）
```

曼哈顿距离在无障碍物的 4 方向格子地图上是精确的，有障碍物时是低估（实际绕路更远），满足可接受性。

**权衡**：h(n) 越"激进"（越接近真实值但不超过），A* 越快；如果刻意允许 h(n) 稍微高估（乘以一个系数 > 1），搜索更快但路径不再保证最优：

```csharp
// Weighted A*：牺牲最优性换速度
float f = gCost + 1.2f * heuristic(neighbor);  // 系数 1.2 让搜索偏向目标方向
```

---

## A* 的搜索范围对比

```
起点在左下角，目标在右上角，无障碍：

Dijkstra 探索范围（圆形扩展）：
■■■■■■■
■■■■■■■
■■■■■■■
■S□□□T    ← 探索了一大片

A*（曼哈顿启发）探索范围：
□□□□□□□
□□□□□T
□□□□T□
□□□T□□
□S□□□□    ← 主要探索目标方向
```

在开阔地形上，A* 的探索节点数可以是 Dijkstra 的 1/10 到 1/100。

---

## 三者的关系总结

| 算法 | 优先队列排序键 | 能找最短路 | 适用场景 |
|---|---|---|---|
| BFS | 步数（FIFO） | 是（无权图） | 无权格子地图 |
| Dijkstra | g(n)（实际代价） | 是（带权图） | 带权图，全图最短路 |
| A* | f(n) = g(n) + h(n) | 是（h 可接受时） | 单源最短路，游戏寻路标准方案 |

从 BFS 到 Dijkstra 到 A*，是逐步加入"已知信息"的过程：
- BFS：不知道代价，不知道方向
- Dijkstra：知道代价，不知道方向
- A*：知道代价，用启发函数估算方向

---

## 小结

- **Dijkstra**：带权图最短路径，所有边代价非负时保证最优，盲目向四周扩展
- **A***：Dijkstra + 启发函数 h(n)，优先探索"看起来更接近目标"的节点，大幅减少探索范围
- **启发函数选择**：4方向用曼哈顿，8方向用切比雪夫，导航网格用欧几里得
- **可接受性**：h(n) 不高估真实代价时，A* 保证最优；允许高估可以换取更快的搜索速度
- **下一篇（DS-07）**：A* 的工程实现细节——Open/Closed 列表的数据结构选择、路径平滑、NavMesh 上的 A* 变体
