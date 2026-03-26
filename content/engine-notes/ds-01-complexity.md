---
date: "2026-03-26"
title: "数据结构与算法 01｜算法复杂度实战：Big-O 不是理论，是选数据结构的判断依据"
description: "Big-O 不是考试题，是每次选数据结构时的底层判断逻辑。这篇用游戏里真实发生的性能问题，把时间复杂度和空间复杂度讲成能直接用的工具。"
slug: "ds-01-complexity"
weight: 741
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 性能优化
  - 游戏架构
series: "数据结构与算法"
---

> Big-O 是一种**估算工具**：当数据量翻倍时，这段代码会慢多少？
>
> 理解它的目的不是通过面试，而是在 1000 个单位和 100000 个单位的场景下，能在写代码之前就知道哪种实现会出问题。

---

## 为什么游戏开发者需要关心复杂度

游戏有一个非常特殊的约束：**每帧 16ms（60fps）或 33ms（30fps）**，超了就掉帧。

后台服务出现慢查询，加个索引、等一次上线就解决了。游戏里一段 O(n²) 的碰撞检测代码，在 100 个单位时毫无问题，在 1000 个单位时直接让游戏变成 PPT——而且这个问题在开发阶段可能根本测不出来（测试场景就 50 个单位）。

```
场景：1000 个单位的 RTS，每帧检测两两之间的碰撞

O(n²) 实现：1000 × 1000 = 1,000,000 次检查 / 帧
O(n log n) 实现（空间哈希）：约 10,000 次检查 / 帧
```

差了 100 倍。这就是复杂度分析的实际价值。

---

## Big-O 的含义：只关心增长趋势

Big-O 描述的是"当输入规模 n 增大时，操作次数**大约**怎么增长"。它忽略常数，只关心最主要的增长项：

```
f(n) = 3n² + 100n + 500
Big-O：O(n²)  ← 忽略系数 3，忽略低阶项
```

这意味着什么？

```
n = 100：  3×100² + 100×100 + 500 = 40,500
n = 1000： 3×1000² + 100×1000 + 500 = 3,100,500（约 3M）

翻了 10 倍 → 操作次数翻了约 76 倍
这就是 O(n²) 的特性：n 翻 k 倍，操作次数翻 k² 倍
```

---

## 常见复杂度从快到慢

```
O(1)        常数时间    数组按索引访问，哈希表查找
O(log n)    对数时间    二叉搜索，堆操作，A* 的优先队列
O(n)        线性时间    数组遍历，链表查找
O(n log n)  线性对数    快速排序，归并排序
O(n²)       平方时间    嵌套循环，暴力碰撞检测
O(2ⁿ)       指数时间    暴力回溯，通常不可用于游戏
```

直观感受（n = 10,000）：

```
O(1)        → 1 次操作
O(log n)    → 约 13 次操作
O(n)        → 10,000 次操作
O(n log n)  → 约 130,000 次操作
O(n²)       → 100,000,000 次操作（每帧！）
```

---

## 游戏里的真实案例

### 案例一：敌人查找最近目标

```csharp
// 方案 A：O(n)  线性扫描
// 每个单位每帧都扫描所有敌人
public Transform FindNearestEnemy(Vector3 pos, List<Enemy> enemies)
{
    Transform nearest = null;
    float minDist = float.MaxValue;

    foreach (var enemy in enemies)  // n 次遍历
    {
        float dist = Vector3.Distance(pos, enemy.transform.position);
        if (dist < minDist)
        {
            minDist = dist;
            nearest = enemy.transform;
        }
    }
    return nearest;
}

// 100 个友方单位都要找最近敌人：100 × 100 = 10,000 次距离计算 / 帧
// 1000 个友方，1000 个敌人：1,000,000 次 / 帧 → 已经很慢
```

```csharp
// 方案 B：O(1) 近似，用空间哈希 (DS-16)
// 把场景分成格子，每个单位只检查附近格子里的敌人
// 平均每个格子几个单位 → 近似 O(1)
public Transform FindNearestEnemy(Vector3 pos, SpatialHash grid)
{
    return grid.GetNearestInRadius(pos, searchRadius);
}
```

### 案例二：技能冷却查询

```csharp
// 方案 A：O(n) — 用 List 存所有技能，每次查询都遍历
List<Skill> cooldownSkills = new List<Skill>();

bool IsOnCooldown(SkillId id)
{
    foreach (var skill in cooldownSkills)  // 平均 n/2 次
        if (skill.Id == id) return true;
    return false;
}

// 方案 B：O(1) — 用 HashSet 存冷却中的技能 ID
HashSet<SkillId> cooldownSkills = new HashSet<SkillId>();

bool IsOnCooldown(SkillId id) => cooldownSkills.Contains(id);  // O(1)
```

技能查询一帧可能发生成百上千次（AI 决策、UI 刷新）。O(1) vs O(n) 在高频场景下差距极大。

### 案例三：Unity 的 GameObject.Find

```csharp
// Unity 内部实现是 O(n)，遍历场景里所有 GameObject
// 在 Update 里调用 = 每帧 O(n)
void Update()
{
    var player = GameObject.Find("Player");  // 永远不要在 Update 里这样写
}

// 正确做法：缓存引用，只查一次
void Awake()
{
    player = GameObject.Find("Player");  // Awake 里查一次
}
```

---

## 均摊复杂度：List 的 Add 操作

`List<T>` 的 `Add` 单次操作是 O(1)，但偶尔会触发扩容（把旧数组复制到新的 2 倍大的数组），那次操作是 O(n)。

但**均摊**来看，n 次 Add 操作中只有 log₂(n) 次扩容，总工作量是 O(n log n) 或 O(n)（取决于分析方法），平均每次 Add 仍然是 O(1)。

```
这就是为什么 List.Add 的文档说"均摊 O(1)"
——大多数时候是 O(1)，偶尔是 O(n)，长期平均下来是 O(1)
```

实际影响：如果在游戏逻辑里频繁 Add 并且恰好触发扩容，会产生一个小的卡顿。预分配容量可以消除这个问题：

```csharp
// 已知大约会有 500 个子弹，提前分配
List<Bullet> bullets = new List<Bullet>(500);  // 不会触发中途扩容
```

---

## 空间复杂度：内存也要估算

除了时间，内存也有复杂度：

```csharp
// O(1) 空间：原地算法，只用固定几个变量
void BubbleSort(int[] arr) { int temp; ... }

// O(n) 空间：需要额外的 n 个元素的空间
int[] merged = new int[a.Length + b.Length];  // 归并排序的合并步骤

// O(n²) 空间：邻接矩阵存 n 个节点的图
int[,] adjacencyMatrix = new int[n, n];  // 1000 个节点 = 1,000,000 个整数 ≈ 4MB
```

游戏里的空间复杂度约束：
- 移动端内存严格（1~3GB 硬上限），O(n²) 的数据结构对大 n 基本不可用
- CPU 缓存只有几 MB，数据结构的内存布局直接影响缓存命中率（见 DS-02）

---

## 如何在日常开发中用好复杂度分析

**Step 1：识别 n 是什么**

每次写循环前，问自己：这里的 n 是什么？最坏情况下 n 是多少？

```
n = 同屏单位数：100? 10,000?
n = 场景里的可交互物体数：200? 50,000?
n = 技能数：10? 1000?
```

**Step 2：判断是否在热路径上**

- 每帧都执行 → 必须严格控制复杂度
- 关卡加载时执行一次 → O(n²) 在 n 不大时可以接受
- 玩家点击时执行 → 取决于玩家操作频率

**Step 3：估算实际操作次数**

```
n = 1000，O(n²) → 1,000,000 次 / 帧
60fps 下每秒 60,000,000 次操作
每次操作 1ns → 每秒 60ms 就在这里 → 帧时间直接超
```

**Step 4：不要过度优化**

```
n = 10，O(n²) → 100 次 / 帧 → 完全可以接受
不需要为 10 个元素引入复杂的数据结构
```

---

## 小结

| 复杂度 | 对应操作 | 游戏中适用规模 |
|---|---|---|
| O(1) | 哈希查找、数组索引 | 任意规模 |
| O(log n) | 二叉堆、BST | 任意规模 |
| O(n) | 线性遍历 | n < 10,000（热路径），n < 1,000,000（冷路径） |
| O(n log n) | 快排、归并排序 | n < 100,000（热路径） |
| O(n²) | 嵌套遍历 | n < 1,000（热路径），n < 10,000（冷路径） |
| O(2ⁿ) | 回溯算法 | n < 20，且不在热路径 |

- **复杂度分析的价值**：在写代码之前，预判"这段代码在目标规模下是否能跑得起来"
- **热路径优先**：每帧执行的代码必须严格控制复杂度，非热路径可以放宽
- **n 的估算**：写循环前先想清楚 n 是什么、最坏情况多大
- **不要过度优化**：n 小的场景用简单实现，不要为 10 个元素写红黑树
