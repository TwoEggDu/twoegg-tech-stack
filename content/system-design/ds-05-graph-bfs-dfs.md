---
date: "2026-03-26"
title: "数据结构与算法 05｜图论基础 + BFS / DFS：地图遍历、连通性检测、迷宫生成"
description: "图是游戏里使用最广泛的数据结构之一——寻路地图、技能依赖树、关卡连通性、对话流程图都是图。这篇用游戏案例讲清楚图的表示方式、BFS 和 DFS 的原理与实现，以及它们各自适合的游戏场景。"
slug: "ds-05-graph-bfs-dfs"
weight: 749
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 图论
  - 寻路
  - 游戏架构
series: "数据结构与算法"
---

> 图（Graph）是节点和边的集合。地图是图，技能树是图，关卡流程是图，NPC 对话选项是图。理解图的遍历，是理解寻路算法的前提。

---

## 图的基本概念

```
节点（Vertex / Node）：地图上的格子、技能、关卡
边（Edge）：节点之间的连接关系

有向图：边有方向（A→B 不等于 B→A）
  例：技能依赖（先学技能 A 才能学技能 B）

无向图：边无方向（A-B 等于 B-A）
  例：导航网格（可以双向走）

带权图：边有权重（代价）
  例：寻路地图（不同地形移动代价不同）

无权图：边没有权重（或权重均为 1）
  例：迷宫（每步代价相同）
```

---

## 图的两种表示方式

### 邻接矩阵（Adjacency Matrix）

用二维数组 `matrix[i][j]` 表示节点 i 到节点 j 是否有边（或边的权重）。

```csharp
// n 个节点的邻接矩阵
int[,] matrix = new int[n, n];
matrix[0, 1] = 1;  // 节点 0 到节点 1 有边
matrix[1, 3] = 5;  // 节点 1 到节点 3，权重为 5

// 查询 i 到 j 是否有边：O(1)
bool hasEdge = matrix[i, j] != 0;

// 遍历节点 i 的所有邻居：O(n)（要扫描整行）
```

**适用**：节点数少（n < 500），边很密集（接近完全图）。

**不适用**：大型稀疏图（n = 10,000，大多数节点之间没有连接）——矩阵占 10,000 × 10,000 = 1 亿个格子，大部分是 0，浪费内存。

### 邻接表（Adjacency List）

每个节点维护一个列表，存储它的所有邻居。

```csharp
// 用 Dictionary + List 实现邻接表
Dictionary<int, List<(int neighbor, float weight)>> graph = new();

// 添加边
void AddEdge(int from, int to, float weight)
{
    if (!graph.ContainsKey(from))
        graph[from] = new List<(int, float)>();
    graph[from].Add((to, weight));
}

// 遍历节点 i 的所有邻居：O(度数)
foreach (var (neighbor, weight) in graph[i])
    // ...
```

**适用**：大多数游戏场景——稀疏图（每个节点只连接少数邻居）。导航网格、格子地图都是稀疏图。

**游戏里的格子地图**（最常见的图）：

```csharp
// 4 方向或 8 方向的格子地图，不需要显式邻接表
// 通过坐标计算邻居，隐式表示图
int[] dx = { 0, 0, 1, -1 };      // 4方向
int[] dy = { 1, -1, 0, 0 };

List<(int, int)> GetNeighbors(int x, int y, bool[,] walkable)
{
    var result = new List<(int, int)>();
    for (int d = 0; d < 4; d++)
    {
        int nx = x + dx[d], ny = y + dy[d];
        if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && walkable[nx, ny])
            result.Add((nx, ny));
    }
    return result;
}
```

---

## BFS（广度优先搜索）

**思路**：从起点出发，先访问所有距离为 1 的节点，再访问距离为 2 的节点，以此类推——一圈一圈向外扩展。用队列实现。

```csharp
// BFS：找从 start 到 target 的最短路径（无权图）
List<Vector2Int> BFS(Vector2Int start, Vector2Int target, bool[,] walkable)
{
    int width = walkable.GetLength(0), height = walkable.GetLength(1);
    var visited = new bool[width, height];
    var parent  = new Dictionary<Vector2Int, Vector2Int>();  // 记录路径
    var queue   = new Queue<Vector2Int>();

    queue.Enqueue(start);
    visited[start.x, start.y] = true;

    int[] dx = { 0, 0, 1, -1 };
    int[] dy = { 1, -1, 0, 0 };

    while (queue.Count > 0)
    {
        var curr = queue.Dequeue();

        if (curr == target)
            return ReconstructPath(parent, start, target);  // 找到终点

        for (int d = 0; d < 4; d++)
        {
            var next = new Vector2Int(curr.x + dx[d], curr.y + dy[d]);

            if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                continue;
            if (!walkable[next.x, next.y] || visited[next.x, next.y])
                continue;

            visited[next.x, next.y] = true;
            parent[next] = curr;
            queue.Enqueue(next);
        }
    }

    return null;  // 无路可走
}

// 从 parent 字典反向重建路径
List<Vector2Int> ReconstructPath(
    Dictionary<Vector2Int, Vector2Int> parent,
    Vector2Int start, Vector2Int target)
{
    var path = new List<Vector2Int>();
    var curr = target;
    while (curr != start)
    {
        path.Add(curr);
        curr = parent[curr];
    }
    path.Add(start);
    path.Reverse();
    return path;
}
```

**BFS 的关键性质**：

在无权图（每步代价相同）中，BFS 找到的路径**保证是最短路径**（经过的节点数最少）。

**BFS 的复杂度**：O(V + E)，V 是节点数，E 是边数。对于 m×n 的格子地图，V = m×n，E ≈ 4×m×n，即 O(m×n)。

---

## DFS（深度优先搜索）

**思路**：从起点出发，沿着一条路径一直走到头（或走到目标），走不通再回退换方向。用栈（或递归）实现。

```csharp
// DFS：递归版本
bool DFS(Vector2Int curr, Vector2Int target, bool[,] walkable,
         bool[,] visited, List<Vector2Int> path)
{
    if (curr == target)
    {
        path.Add(curr);
        return true;
    }

    visited[curr.x, curr.y] = true;
    path.Add(curr);

    int[] dx = { 0, 0, 1, -1 };
    int[] dy = { 1, -1, 0, 0 };

    for (int d = 0; d < 4; d++)
    {
        var next = new Vector2Int(curr.x + dx[d], curr.y + dy[d]);

        if (next.x < 0 || next.x >= walkable.GetLength(0)) continue;
        if (next.y < 0 || next.y >= walkable.GetLength(1)) continue;
        if (!walkable[next.x, next.y] || visited[next.x, next.y]) continue;

        if (DFS(next, target, walkable, visited, path))
            return true;  // 找到了，沿路返回
    }

    path.RemoveAt(path.Count - 1);  // 回退：这条路不通
    return false;
}
```

**DFS 不保证最短路径**——它找到的是"第一条找到的路径"，可能绕很远。

---

## BFS vs DFS：怎么选

| | BFS | DFS |
|---|---|---|
| 实现结构 | 队列（FIFO） | 栈 / 递归（LIFO） |
| 路径质量 | 最短路径（无权图） | 不保证最短 |
| 内存占用 | 高（要存整个"波前"） | 低（只存一条路径） |
| 适合的问题 | 找最短路、层级遍历 | 连通性检测、枚举所有路径、迷宫生成 |

### BFS 适合的游戏场景

**寻路（无权格子地图）**：

```
BFS 在所有格子代价相同时（平坦地形）找到的一定是最短路
如果地形有不同代价（沼泽慢、道路快），需要 Dijkstra 或 A*（DS-06/DS-07）
```

**关卡连通性检测**：

```csharp
// 检测玩家能否从出生点到达所有关键区域
// BFS 从出生点出发，检查所有重要节点是否被访问到
bool IsLevelSolvable(Vector2Int spawn, Vector2Int[] keyAreas, bool[,] walkable)
{
    var visited = BFSVisited(spawn, walkable);
    foreach (var area in keyAreas)
        if (!visited[area.x, area.y]) return false;
    return true;
}
```

**技能/词条影响范围**：

```csharp
// "爆炸范围内的所有单位"：从爆炸中心 BFS，找所有在 N 格以内的单位
List<Unit> GetUnitsInRange(Vector2Int center, int range, bool[,] walkable)
{
    // BFS 控制层数（距离），达到 range 层时停止扩展
}
```

### DFS 适合的游戏场景

**迷宫生成**（Recursive Backtracking 算法）：

```csharp
// 用 DFS 生成迷宫：从任意格子出发，随机访问未访问的邻居，打通墙壁
void GenerateMaze(int x, int y, bool[,] visited, bool[,] walls)
{
    visited[x, y] = true;

    // 随机打乱方向
    var dirs = new int[] { 0, 1, 2, 3 };
    Shuffle(dirs);

    foreach (int d in dirs)
    {
        int nx = x + dx[d] * 2;  // 跳 2 格，中间是墙
        int ny = y + dy[d] * 2;

        if (InBounds(nx, ny) && !visited[nx, ny])
        {
            // 打通 (x,y) 到 (nx,ny) 之间的墙
            walls[x + dx[d], y + dy[d]] = false;
            GenerateMaze(nx, ny, visited, walls);
        }
    }
}
```

**技能依赖树解锁检测**：

```csharp
// 检查解锁某个技能所需的所有前置技能是否已学
// DFS 遍历依赖图，检查所有依赖节点
bool CanUnlock(SkillId skill, HashSet<SkillId> learned,
               Dictionary<SkillId, List<SkillId>> dependencies)
{
    if (learned.Contains(skill)) return true;
    if (!dependencies.ContainsKey(skill)) return false;

    foreach (var prereq in dependencies[skill])
        if (!learned.Contains(prereq)) return false;

    return true;
}
```

**连通分量（找孤岛）**：

```csharp
// 程序化地图生成后，找出所有独立的陆地区域
// DFS 标记连通分量，每个分量是一个"孤岛"
int[] LabelConnectedComponents(bool[,] isLand)
{
    int width = isLand.GetLength(0), height = isLand.GetLength(1);
    int[,] label = new int[width, height];  // 0 = 未访问
    int currentLabel = 0;

    for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
    {
        if (isLand[x, y] && label[x, y] == 0)
        {
            currentLabel++;
            DFSLabel(x, y, currentLabel, isLand, label);
        }
    }
    return currentLabel;  // 返回连通分量数量
}
```

---

## 洪水填充（Flood Fill）

BFS 的一个特化应用，在游戏里非常常见：

```csharp
// 经典用途：油漆桶工具（编辑器地形刷）
// 把与起点颜色相同的连通区域全部填充为新颜色
void FloodFill(int[,] grid, int x, int y, int oldColor, int newColor)
{
    if (grid[x, y] != oldColor) return;

    var queue = new Queue<(int, int)>();
    queue.Enqueue((x, y));
    grid[x, y] = newColor;

    int[] dx = { 0, 0, 1, -1 };
    int[] dy = { 1, -1, 0, 0 };

    while (queue.Count > 0)
    {
        var (cx, cy) = queue.Dequeue();
        for (int d = 0; d < 4; d++)
        {
            int nx = cx + dx[d], ny = cy + dy[d];
            if (InBounds(nx, ny) && grid[nx, ny] == oldColor)
            {
                grid[nx, ny] = newColor;
                queue.Enqueue((nx, ny));
            }
        }
    }
}

// 游戏里的其他用途：
// - 编辑器：地形类型填充、区域标记
// - 程序生成：房间填充、海洋/陆地分离
// - 游戏逻辑：消消乐的消除判定、围棋的气计算
```

---

## 小结

- **图的表示**：稀疏图用邻接表（游戏里的大多数情况），密集小图用邻接矩阵；格子地图通过坐标隐式表示图
- **BFS**：队列驱动，逐层扩展，保证无权图最短路——适合寻路、范围检测、连通性验证
- **DFS**：栈/递归驱动，一路到底再回退——适合迷宫生成、连通分量标记、依赖关系检测
- **Flood Fill**：BFS 的特化，编辑器工具和程序生成的常用工具
- **寻路中的 BFS 局限**：只适合无权图；带权图（不同地形代价不同）需要 Dijkstra 或 A*（见 DS-06/DS-07）
