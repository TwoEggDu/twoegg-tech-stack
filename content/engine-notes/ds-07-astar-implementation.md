---
title: "数据结构与算法 07｜A* 实现细节：Open/Closed 列表、路径平滑、性能优化"
description: "A* 原理简单，但工程实现里有很多细节决定它的性能：Open 列表用什么数据结构、如何避免重复入队、路径平滑算法、大地图分层寻路。这篇把 A* 从原理变成能用的代码。"
slug: "ds-07-astar-implementation"
weight: 753
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 寻路
  - A*
  - 游戏架构
series: "数据结构与算法"
---

> A* 的理论只有十几行代码。但游戏里用的 A* 需要处理：大地图性能、对角移动、地形代价、路径平滑、多个单位并发寻路——这些才是实际工作量所在。

---

## Open 列表的数据结构选择

A* 的性能瓶颈在于 Open 列表的操作：每次需要取出 f 值最小的节点。

### 用 List + 线性扫描：O(n)

```csharp
// 最简单但最慢的实现
Vector2Int GetLowestF(List<AStarNode> open)
{
    var best = open[0];
    foreach (var node in open)
        if (node.f < best.f) best = node;
    return best.pos;
}
```

100 个 Open 节点就要扫描 100 次，n 个节点每次取最小是 O(n)。整体 A* 变成 O(n²)。

### 用二叉堆（优先队列）：O(log n)

```csharp
// .NET 6+ 内置 PriorityQueue
var open = new PriorityQueue<Vector2Int, float>();  // (节点, f值)

// 入队：O(log n)
open.Enqueue(neighbor, fValue);

// 取出最小 f 值节点：O(log n)
var curr = open.Dequeue();
```

整体 A* 复杂度：O((V + E) log V)。

### 注意：PriorityQueue 不支持更新优先级

C# 的 `PriorityQueue` 不提供"更新已有节点的 f 值"操作。当我们发现一条更优的路径到达某个节点时，只能重新插入一个新条目（旧条目留在队列里），通过 Closed 集合过滤掉过期条目：

```csharp
while (open.Count > 0)
{
    var curr = open.Dequeue();

    // 如果已经处理过（Closed），跳过这个过期条目
    if (closed.Contains(curr)) continue;
    closed.Add(curr);

    // ... 正常处理
}
```

这是一个常见实现技巧：允许重复条目存在，用 Closed 集合去重。

---

## 完整的格子地图 A* 实现

```csharp
public class AStarGrid
{
    private int width, height;
    private float[,] tileCost;   // 每格的移动代价（1=普通，3=沼泽，0=障碍）

    public AStarGrid(int w, int h)
    {
        width = w; height = h;
        tileCost = new float[w, h];
        // 初始化：默认代价 1
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            tileCost[x, y] = 1f;
    }

    public void SetObstacle(int x, int y) => tileCost[x, y] = 0f;
    public void SetCost(int x, int y, float cost) => tileCost[x, y] = cost;

    // 8方向移动（含斜向）
    private static readonly (int dx, int dy, float cost)[] Directions = {
        ( 0,  1, 1.0f), ( 0, -1, 1.0f), ( 1,  0, 1.0f), (-1,  0, 1.0f),
        ( 1,  1, 1.414f), ( 1, -1, 1.414f), (-1,  1, 1.414f), (-1, -1, 1.414f)
    };

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (tileCost[end.x, end.y] == 0f) return null;  // 终点是障碍

        var gCost  = new Dictionary<Vector2Int, float>();
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var open   = new PriorityQueue<Vector2Int, float>();
        var closed = new HashSet<Vector2Int>();

        gCost[start] = 0f;
        open.Enqueue(start, Heuristic(start, end));

        while (open.Count > 0)
        {
            var curr = open.Dequeue();
            if (closed.Contains(curr)) continue;

            if (curr == end)
                return ReconstructPath(parent, start, end);

            closed.Add(curr);

            foreach (var (dx, dy, dirCost) in Directions)
            {
                int nx = curr.x + dx, ny = curr.y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                var next = new Vector2Int(nx, ny);
                if (closed.Contains(next)) continue;
                if (tileCost[nx, ny] == 0f) continue;  // 障碍

                // 斜向移动：检查两侧是否可通（避免穿墙角）
                if (dx != 0 && dy != 0)
                {
                    if (tileCost[curr.x + dx, curr.y] == 0f) continue;
                    if (tileCost[curr.x, curr.y + dy] == 0f) continue;
                }

                float newG = gCost[curr] + dirCost * tileCost[nx, ny];

                if (!gCost.TryGetValue(next, out float oldG) || newG < oldG)
                {
                    gCost[next] = newG;
                    parent[next] = curr;
                    float f = newG + Heuristic(next, end);
                    open.Enqueue(next, f);
                }
            }
        }

        return null;
    }

    // 切比雪夫距离（8方向）
    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + (Mathf.Sqrt(2f) - 1f) * Mathf.Min(dx, dy);
    }

    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        var curr = end;
        while (curr != start) { path.Add(curr); curr = parent[curr]; }
        path.Add(start);
        path.Reverse();
        return path;
    }
}
```

---

## 路径平滑：让路径不那么"锯齿"

格子 A* 找到的路径是一串格子坐标，走起来呈 45 度折线，不自然。

### 方法一：字符串拉直（String Pulling）

遍历路径，把能用直线连接的中间点删掉：

```csharp
// 射线检测：两点之间是否有障碍
bool HasClearLine(Vector2Int a, Vector2Int b, bool[,] walkable)
{
    // Bresenham 直线算法，检测沿线的所有格子是否可走
    // 简化版：用 Physics2D.Linecast 或自实现 Bresenham
    return !Physics2D.Linecast(GridToWorld(a), GridToWorld(b), obstacleLayer);
}

List<Vector2Int> SmoothPath(List<Vector2Int> path, bool[,] walkable)
{
    if (path.Count <= 2) return path;
    var smooth = new List<Vector2Int> { path[0] };
    int anchor = 0;

    for (int i = 2; i < path.Count; i++)
    {
        if (!HasClearLine(path[anchor], path[i], walkable))
        {
            smooth.Add(path[i - 1]);  // i-1 是最后一个能直线到达的点
            anchor = i - 1;
        }
    }
    smooth.Add(path[path.Count - 1]);
    return smooth;
}
```

### 方法二：Catmull-Rom 样条平滑

字符串拉直后，再用样条曲线让路径更流畅：

```csharp
// 在路径点之间插入 Catmull-Rom 样条插值点
Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
{
    return 0.5f * (
        (2f * p1) +
        (-p0 + p2) * t +
        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
        (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
    );
}
```

---

## Unity NavMesh 的 A*

Unity 的 `NavMeshAgent` 用的不是格子 A*，而是在**导航网格（Navigation Mesh）**上运行的 A*。

**导航网格**是一组凸多边形（Polygon），覆盖可行走的区域：

```
格子地图：精度高，内存大，适合塔防/策略类
导航网格：精度由网格精度决定，内存小，适合 3D 动作/RPG
```

导航网格上的 A* 节点不是格子，而是多边形。移动代价是多边形之间的距离。Unity 在底层处理了：
- 路径平滑（NavMeshAgent 的 `steeringTarget` 始终朝向平滑后的路径点）
- 多层导航（楼梯、跳跃链接：`OffMeshLink`）
- 局部避障（RVO，Reciprocal Velocity Obstacles）

---

## 大地图性能优化

### 分层 A*（Hierarchical Pathfinding A*，HPA*）

把大地图分成若干小区域（Chunk）：
1. 预计算各 Chunk 之间的"入口点"连通性（离线完成）
2. 寻路时先在 Chunk 级别找高层路径（哪些 Chunk 要经过）
3. 再在每个 Chunk 内部找详细路径

```
100×100 地图（10,000 个节点）：
普通 A*：最坏探索 10,000 节点
HPA*（10×10 的 Chunk，100 个 Chunk）：
  高层：在 100 个 Chunk 节点上搜索（极快）
  低层：在每个 Chunk 的 100 个节点内搜索
  总计：约几百个节点
```

### 帧分摊（Time-Sliced Pathfinding）

把寻路计算分散到多帧，避免单帧卡顿：

```csharp
// 每帧只处理 N 步 A*，没找到就下一帧继续
public class AStarJob
{
    private PriorityQueue<Vector2Int, float> open;
    private HashSet<Vector2Int> closed;
    private Dictionary<Vector2Int, Vector2Int> parent;
    private Dictionary<Vector2Int, float> gCost;
    public bool IsComplete { get; private set; }
    public List<Vector2Int> Result { get; private set; }

    public void Step(int maxSteps)
    {
        int steps = 0;
        while (open.Count > 0 && steps < maxSteps)
        {
            // ... 执行 maxSteps 步 A* 逻辑
            steps++;
        }
    }
}

// 管理器每帧分配预算
void Update()
{
    foreach (var job in activeJobs)
        job.Step(50);  // 每帧每个任务最多处理 50 步
}
```

### 流场（Flow Field）

当需要大量单位（几百到几千）都往同一个目标移动时（RTS 兵团移动），给每个单位都跑一次 A* 浪费极大。

Flow Field 的思路：从目标出发，对整张地图跑一次 Dijkstra，预计算每个格子"朝哪个方向走能最快到目标"，然后所有单位都查这张方向图：

```csharp
// 预计算流场：O(V + E)，只跑一次
Dictionary<Vector2Int, Vector2Int> BuildFlowField(Vector2Int target, bool[,] walkable)
{
    // 从目标反向 BFS/Dijkstra
    // 每个格子记录"下一步走哪个方向"
}

// 每个单位查询：O(1)
Vector2Int GetMoveDirection(Vector2Int pos, FlowField field)
{
    return field[pos];  // 直接查表
}
```

1000 个单位寻路，Flow Field 只需要 1 次 Dijkstra + 1000 次 O(1) 查表，比 1000 次 A* 快几个数量级。

---

## 常见问题与解决方案

**问题：路径穿越墙角**

```
斜向移动时，从 A 到 B 穿过了两个障碍格的夹角
解决：斜向移动前，检查水平方向和垂直方向的格子是否都可走
```

**问题：单位重叠（多个单位挤在同一格）**

```
解决：局部避障（ORCA/RVO），不在 A* 层面处理，而是在速度层面处理
Unity NavMeshAgent 内置 RVO
```

**问题：动态障碍（障碍物移动）**

```
解决一：D* Lite（支持动态更新的 A*）
解决二：定期重新寻路（每 0.5 秒跑一次 A*）
解决三：局部重规划（只更新障碍物附近的路径段）
```

---

## 小结

- **Open 列表用优先队列**（二叉堆），整体复杂度 O((V+E) log V)；避免用 List 线性扫描
- **斜向移动**：代价乘以 √2；斜向前检查两侧格，避免穿墙角
- **路径平滑**：字符串拉直 + 样条曲线，让路径自然流畅
- **大地图**：HPA*（分层寻路）或帧分摊避免单帧卡顿
- **大量单位**：Flow Field 预计算全局方向，所有单位 O(1) 查表
- **Unity NavMesh**：导航网格上的 A*，内置平滑和避障，3D 项目首选
