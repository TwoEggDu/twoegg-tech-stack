---
title: "数据结构与算法 03｜排序算法选择：快排 / 归并 / 插入排序，游戏里如何选"
description: "游戏里的排序不是竞赛题，而是每帧都要跑的渲染排序、伤害优先级、技能队列。这篇讲清楚三种主力排序算法的原理和性能特征，以及在不同游戏场景下如何做选择。"
slug: "ds-03-sorting"
weight: 745
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 排序
  - 性能优化
series: "数据结构与算法"
---

> 排序不是只在面试里才用的东西。渲染管线每帧对 DrawCall 排序，伤害系统对优先级排序，UI 列表刷新时对条目排序——选错了排序算法，帧时间就白白浪费在这里。

---

## 三种主力排序算法

### 插入排序（Insertion Sort）

**思路**：像打牌时整理手牌，每次把新来的元素插入已排好的部分。

```csharp
void InsertionSort(int[] arr)
{
    for (int i = 1; i < arr.Length; i++)
    {
        int key = arr[i];
        int j = i - 1;
        while (j >= 0 && arr[j] > key)
        {
            arr[j + 1] = arr[j];
            j--;
        }
        arr[j + 1] = key;
    }
}
```

**复杂度**：
- 最好（已排好）：O(n)
- 最坏（完全逆序）：O(n²)
- 平均：O(n²)
- 空间：O(1)（原地）

**关键特性**：
- **稳定排序**：相等元素保持原有顺序
- **自适应**：数据越接近有序，越快
- **小数组极快**：n < 16 时，因为没有递归开销、缓存友好，实际速度超过快排

### 快速排序（Quick Sort）

**思路**：选一个基准（pivot），把小于它的放左边，大于它的放右边，然后递归。

```csharp
void QuickSort(int[] arr, int left, int right)
{
    if (left >= right) return;

    int pivot = Partition(arr, left, right);
    QuickSort(arr, left, pivot - 1);
    QuickSort(arr, pivot + 1, right);
}

int Partition(int[] arr, int left, int right)
{
    int pivot = arr[right];  // 选最右边为基准
    int i = left - 1;

    for (int j = left; j < right; j++)
    {
        if (arr[j] <= pivot)
        {
            i++;
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
    (arr[i + 1], arr[right]) = (arr[right], arr[i + 1]);
    return i + 1;
}
```

**复杂度**：
- 最好/平均：O(n log n)
- 最坏（已排序 + 选最边缘 pivot）：O(n²)
- 空间：O(log n)（递归栈）

**关键特性**：
- **不稳定排序**：相等元素可能改变相对顺序
- **缓存友好**：原地操作，内存访问顺序较连续
- **实践最快**：常数因子小，平均性能最好
- **弱点**：特定输入退化到 O(n²)；可用随机 pivot 或三数取中缓解

### 归并排序（Merge Sort）

**思路**：把数组对半分，分别排序，再合并两个有序数组。

```csharp
void MergeSort(int[] arr, int[] temp, int left, int right)
{
    if (left >= right) return;

    int mid = (left + right) / 2;
    MergeSort(arr, temp, left, mid);
    MergeSort(arr, temp, mid + 1, right);
    Merge(arr, temp, left, mid, right);
}

void Merge(int[] arr, int[] temp, int left, int mid, int right)
{
    // 复制到临时数组
    for (int k = left; k <= right; k++) temp[k] = arr[k];

    int i = left, j = mid + 1;
    for (int k = left; k <= right; k++)
    {
        if      (i > mid)              arr[k] = temp[j++];
        else if (j > right)            arr[k] = temp[i++];
        else if (temp[i] <= temp[j])   arr[k] = temp[i++];
        else                           arr[k] = temp[j++];
    }
}
```

**复杂度**：
- 最好/最坏/平均：O(n log n)（无退化）
- 空间：O(n)（需要额外的临时数组）

**关键特性**：
- **稳定排序**：保证相等元素的相对顺序
- **保证 O(n log n)**：不会退化，适合最坏情况要求严格的场景
- **外部排序友好**：合并操作天然适合分块处理
- **缺点**：额外 O(n) 内存；常数因子比快排大

---

## C# 的 Array.Sort 和 List.Sort

.NET 的内置排序用的是 **Timsort**（归并排序 + 插入排序的混合）：

- 把数组切成小块（每块约 32~64 个元素），用插入排序处理每块（小数组快）
- 然后用归并排序合并各块
- 检测已有序的"run"，自适应利用输入的部分有序性

结论：`Array.Sort` / `List.Sort` 已经是工程实践中的最优选择，**不要自己重写**。但要理解它什么时候快、什么时候慢：

```csharp
// 已经基本有序的数组：Timsort 接近 O(n)
List<Enemy> enemies = ...; // 每帧只有少数单位移动
enemies.Sort((a, b) => a.distanceToPlayer.CompareTo(b.distanceToPlayer));
// 大多数元素位置变化不大 → Timsort 检测到有序 run → 极快

// 完全随机的数组：Timsort 是 O(n log n)
// 性能与标准快排相当，不会退化
```

---

## 游戏里的排序场景

### 场景一：渲染 DrawCall 排序（每帧，高频）

不透明物体按材质/Shader 排序（减少状态切换），透明物体按深度从后往前排序（保证混合正确）。

```csharp
// 不透明：按 material ID 排序，减少 SetPass Call
renderQueue.Sort((a, b) => a.materialId.CompareTo(b.materialId));

// 透明：按深度从后往前
transparentQueue.Sort((a, b) => b.depth.CompareTo(a.depth));
```

**选择**：用 `List.Sort`（Timsort）。渲染队列每帧变化不大（大多数物体相对位置稳定），Timsort 对近似有序数据特别快。需要稳定排序（相同深度的物体保持原顺序）→ Timsort 是稳定的。

### 场景二：伤害/事件优先级队列（高频）

技能系统、受击响应，需要按优先级处理事件。

```csharp
// 如果事件量很小（< 20），插入排序最合适
// 实际上更好的选择是用堆（DS-09），O(log n) 插入和取出
```

优先队列场景不应该用排序，应该用二叉堆（见 DS-09）——每次只需要取出最高优先级的那一个，不需要整体有序。

### 场景三：近邻查询结果排序（中频）

寻路系统找到多条路径后按代价排序，或者找最近的几个目标后排序。

```csharp
List<PathResult> candidates = FindCandidatePaths(...);  // 通常 < 50 条

// 结果不多，插入排序最合适（小数组 + 可能部分有序）
InsertionSort(candidates, ...);

// 实际项目里直接用 List.Sort 就行，编译器做了类似优化
candidates.Sort((a, b) => a.cost.CompareTo(b.cost));
```

### 场景四：排行榜 / 成就解锁（低频）

玩家查看排行榜，每次请求时对数千条记录排序。

```csharp
// 数量中等（几千），随机性强，用 List.Sort（快排为主）
leaderboard.Sort((a, b) => b.score.CompareTo(a.score));

// 如果需要实时更新 + 查询 Top N：用堆（DS-09）维护，避免全量重排
```

---

## 实用决策树

```
需要排序：
│
├── 数据量 < 20？
│   └── 用插入排序（或直接 List.Sort，Timsort 会自动用插入排序处理小块）
│
├── 需要稳定排序（相等元素保持原顺序）？
│   └── 用归并排序 / List.Sort（Timsort 是稳定的）
│
├── 只需要最大/最小的 Top K，不需要完全排序？
│   └── 用堆（DS-09），O(n log k) 而不是 O(n log n)
│
├── 数据接近有序（每次只有少量变化）？
│   └── List.Sort（Timsort 对此最优）
│
└── 一般情况？
    └── List.Sort（不要自己实现）
```

---

## 稳定排序 vs 不稳定排序

稳定排序：相等元素在排序后保持原有的相对顺序。

```
原始：[(A,3), (B,3), (C,1), (D,2)]

稳定排序按数字升序：[(C,1), (D,2), (A,3), (B,3)]
                                         ↑A在B前面，保持原顺序

不稳定排序可能结果：[(C,1), (D,2), (B,3), (A,3)]
                                         ↑B跑到A前面了
```

游戏里什么时候需要稳定排序？

- 渲染透明物体：相同深度的物体，保持提交顺序可以避免 Z-fighting 闪烁
- UI 列表：相同优先级的条目，刷新时不要乱跳
- 回放系统：相同帧的事件，处理顺序必须可复现

---

## 小结

| 算法 | 时间（平均） | 时间（最坏） | 空间 | 稳定 | 适用场景 |
|---|---|---|---|---|---|
| 插入排序 | O(n²) | O(n²) | O(1) | 是 | n < 20，近似有序 |
| 快速排序 | O(n log n) | O(n²) | O(log n) | 否 | 通用，数据随机 |
| 归并排序 | O(n log n) | O(n log n) | O(n) | 是 | 稳定性要求高，最坏情况确定 |
| Timsort（内置）| O(n log n) | O(n log n) | O(n) | 是 | 默认选择，自适应部分有序 |

- **默认用 `List.Sort`**：Timsort 已经是工程最优，不要重造轮子
- **小数组（< 20）**：插入排序，或者 `List.Sort` 自动处理
- **只要 Top K**：用堆（DS-09），不要对整个数组排序
- **不稳定排序的问题**：渲染、UI、回放场景需要稳定排序，注意 `Array.Sort` 的稳定性文档
