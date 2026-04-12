---
date: "2026-03-26"
title: "数据结构与算法 08｜拓扑排序：技能依赖树、资源加载顺序、任务调度"
description: "拓扑排序解决的问题是：有一组任务，任务之间有依赖关系（A 必须在 B 之前完成），请给出一个合法的执行顺序。技能树解锁、资源加载、构建系统都是这个问题的不同形式。"
slug: "ds-08-topological-sort"
weight: 755
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 图论
  - 游戏架构
series: "数据结构与算法"
---

> 技能树里，学"火球术"需要先学"初级魔法"和"火系亲和"。资源加载里，加载角色模型需要先加载骨骼和材质。这类"有依赖关系的执行顺序"问题，用拓扑排序解决。

---

## 什么是拓扑排序

给定一个**有向无环图（DAG，Directed Acyclic Graph）**，拓扑排序给出一个节点的线性顺序，使得对于每条边 A→B，A 在 B 之前出现。

```
技能依赖图：
初级魔法 → 火系亲和 → 火球术 → 爆裂火球
初级魔法 → 能量护盾
                               ↑
                       只能有有向无环图
                       （依赖关系不能有环：A依赖B，B依赖A → 死锁）
```

拓扑排序的结果（可能有多个合法顺序）：
```
初级魔法 → 火系亲和 → 能量护盾 → 火球术 → 爆裂火球
初级魔法 → 能量护盾 → 火系亲和 → 火球术 → 爆裂火球
... 等多种合法顺序
```

---

## 实现方式一：Kahn 算法（BFS，入度法）

**思路**：入度为 0 的节点（没有依赖）可以立即处理，处理后把它的"贡献"去掉，看看哪些节点入度变为 0，再处理它们。

```csharp
// nodes: 所有节点
// edges: 依赖关系（from 必须在 to 之前）
List<T> TopologicalSort<T>(IEnumerable<T> nodes, IEnumerable<(T from, T to)> edges)
{
    // 构建邻接表和入度表
    var adjacency = new Dictionary<T, List<T>>();
    var inDegree  = new Dictionary<T, int>();

    foreach (var node in nodes)
    {
        adjacency[node] = new List<T>();
        inDegree[node]  = 0;
    }

    foreach (var (from, to) in edges)
    {
        adjacency[from].Add(to);
        inDegree[to]++;
    }

    // 把所有入度为 0 的节点加入队列
    var queue = new Queue<T>();
    foreach (var (node, degree) in inDegree)
        if (degree == 0) queue.Enqueue(node);

    var result = new List<T>();

    while (queue.Count > 0)
    {
        var curr = queue.Dequeue();
        result.Add(curr);

        foreach (var neighbor in adjacency[curr])
        {
            inDegree[neighbor]--;
            if (inDegree[neighbor] == 0)
                queue.Enqueue(neighbor);
        }
    }

    // 如果 result 的数量不等于节点总数，说明有环（循环依赖）
    if (result.Count != inDegree.Count)
        throw new InvalidOperationException("检测到循环依赖！");

    return result;
}
```

**复杂度**：O(V + E)

---

## 实现方式二：DFS 后序（递归）

**思路**：DFS 遍历，当一个节点的所有依赖都处理完，把它加入结果（后序），最后反转结果。

```csharp
List<T> TopologicalSortDFS<T>(Dictionary<T, List<T>> adjacency)
{
    var visited = new HashSet<T>();
    var result  = new List<T>();

    void DFS(T node)
    {
        if (visited.Contains(node)) return;
        visited.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
            foreach (var neighbor in neighbors)
                DFS(neighbor);

        result.Add(node);  // 后序：所有依赖处理完后才加入
    }

    foreach (var node in adjacency.Keys)
        DFS(node);

    result.Reverse();  // 后序反转 = 拓扑序
    return result;
}
```

---

## 游戏场景一：技能树解锁

```csharp
public class SkillTree
{
    // 技能依赖关系
    private Dictionary<string, List<string>> prerequisites = new()
    {
        ["初级魔法"]   = new(),                              // 无前置
        ["火系亲和"]   = new() { "初级魔法" },
        ["能量护盾"]   = new() { "初级魔法" },
        ["火球术"]     = new() { "火系亲和" },
        ["爆裂火球"]   = new() { "火球术" },
        ["魔法加强"]   = new() { "火系亲和", "能量护盾" },   // 多个前置
    };

    // 检查是否可以解锁某个技能
    public bool CanUnlock(string skill, HashSet<string> learned)
    {
        if (!prerequisites.ContainsKey(skill)) return false;
        return prerequisites[skill].All(pre => learned.Contains(pre));
    }

    // 获取合法的解锁顺序（用于新手引导、AI 决策）
    public List<string> GetUnlockOrder()
    {
        var edges = new List<(string, string)>();
        foreach (var (skill, prereqs) in prerequisites)
            foreach (var pre in prereqs)
                edges.Add((pre, skill));  // 前置 → 技能

        return TopologicalSort(prerequisites.Keys, edges);
        // 结果：["初级魔法", "火系亲和", "能量护盾", "火球术", "魔法加强", "爆裂火球"]
        // （其中一种合法顺序）
    }
}
```

---

## 游戏场景二：资源加载顺序

游戏资产之间有依赖关系：材质依赖贴图，Prefab 依赖材质和模型，场景依赖 Prefab。

```csharp
public class AssetLoadScheduler
{
    // 资产依赖图（asset → 它依赖的资产列表）
    private Dictionary<string, List<string>> dependencies = new();

    public void AddDependency(string asset, string dependsOn)
    {
        if (!dependencies.ContainsKey(asset))
            dependencies[asset] = new List<string>();
        dependencies[asset].Add(dependsOn);
    }

    // 返回正确的加载顺序（被依赖的资产先加载）
    public List<string> GetLoadOrder(string rootAsset)
    {
        // 收集 rootAsset 的所有传递依赖
        var allAssets = new HashSet<string>();
        var edges     = new List<(string, string)>();
        CollectDependencies(rootAsset, allAssets, edges);

        // 拓扑排序：被依赖的先加载
        return TopologicalSort(allAssets, edges);
    }

    private void CollectDependencies(string asset,
        HashSet<string> visited, List<(string, string)> edges)
    {
        if (!visited.Add(asset)) return;
        if (!dependencies.TryGetValue(asset, out var deps)) return;

        foreach (var dep in deps)
        {
            edges.Add((dep, asset));   // 依赖项 → 被依赖项（先加载依赖项）
            CollectDependencies(dep, visited, edges);
        }
    }
}

// 使用
var scheduler = new AssetLoadScheduler();
scheduler.AddDependency("PlayerPrefab",  "PlayerMesh");
scheduler.AddDependency("PlayerPrefab",  "PlayerMaterial");
scheduler.AddDependency("PlayerMaterial","PlayerTexture");
scheduler.AddDependency("MainScene",     "PlayerPrefab");
scheduler.AddDependency("MainScene",     "LevelTileset");

var order = scheduler.GetLoadOrder("MainScene");
// 输出：["PlayerTexture", "PlayerMesh", "PlayerMaterial",
//        "LevelTileset", "PlayerPrefab", "MainScene"]
```

---

## 游戏场景三：构建系统 / CI 任务调度

自动化构建中，不同的构建步骤有依赖关系（先编译，再打包，再上传）：

```csharp
var buildTasks = new List<string>
    { "编译代码", "生成资产Bundle", "打包APK", "运行单元测试", "上传到CDN", "推送到测试服" };

var taskDependencies = new List<(string, string)>
{
    ("编译代码",      "生成资产Bundle"),
    ("编译代码",      "运行单元测试"),
    ("生成资产Bundle","打包APK"),
    ("运行单元测试",  "打包APK"),
    ("打包APK",       "上传到CDN"),
    ("上传到CDN",     "推送到测试服"),
};

var order = TopologicalSort(buildTasks, taskDependencies);
// 输出：["编译代码", "运行单元测试", "生成资产Bundle", "打包APK", "上传到CDN", "推送到测试服"]
// （运行单元测试和生成资产Bundle 的相对顺序由 Kahn 算法决定，两种都合法）
```

---

## 循环依赖检测

拓扑排序的一个重要副产品：**检测依赖关系是否有环**。

```csharp
// Kahn 算法结束后，如果处理的节点数 < 总节点数，说明有环
if (result.Count < totalNodes)
{
    // 找出哪些节点在环里（入度仍然 > 0 的节点）
    var inCycle = inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key);
    throw new Exception($"循环依赖：{string.Join(", ", inCycle)}");
}
```

实际应用：
- **Addressables / AssetBundle 打包**：检查 Bundle 之间是否有循环依赖（循环依赖会导致 Bundle 体积膨胀或加载失败）
- **脚本依赖**：C# 程序集之间不能循环引用，编译器会报错，根本上是拓扑排序的约束
- **技能树**：玩家可能通过 MOD 创造出循环依赖的技能，需要在加载时检测

---

## 并行化：哪些任务可以同时执行

拓扑排序还可以告诉我们哪些任务**可以并行**——没有依赖关系的任务可以同时执行：

```csharp
// 按"层级"分组：同一层级的节点没有相互依赖，可以并行
List<List<T>> GetParallelLayers<T>(IEnumerable<T> nodes,
                                   IEnumerable<(T from, T to)> edges)
{
    var inDegree   = /* ... 计算入度 ... */;
    var adjacency  = /* ... 构建邻接表 ... */;
    var layers     = new List<List<T>>();

    while (inDegree.Any(kv => kv.Value >= 0))
    {
        var layer = inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        if (layer.Count == 0) break;

        layers.Add(layer);

        foreach (var node in layer)
        {
            inDegree[node] = -1;  // 标记已处理
            foreach (var neighbor in adjacency[node])
                inDegree[neighbor]--;
        }
    }
    return layers;
}

// 结果示例（资源加载）：
// 第 0 层（可并行）：["PlayerTexture", "PlayerMesh", "LevelTileset"]
// 第 1 层（可并行）：["PlayerMaterial"]
// 第 2 层：["PlayerPrefab"]
// 第 3 层：["MainScene"]
```

---

## 小结

- **拓扑排序的前提**：有向无环图（DAG）。有环则无法排序（循环依赖）
- **Kahn 算法**（BFS 入度法）：直观，容易实现循环检测和并行层级计算
- **DFS 后序法**：代码简洁，但循环检测需要额外处理
- **游戏中的典型应用**：技能树解锁顺序、资源加载顺序、构建任务调度
- **循环依赖检测**：拓扑排序的副产品，在打包系统和 Mod 加载中尤其重要
- **并行层级**：同一层内的任务没有依赖关系，可以并行加载/执行，用于优化加载时间
