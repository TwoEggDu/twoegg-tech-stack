---
title: "数据结构与算法 09｜二叉堆与优先队列：A* 的底层结构，技能队列，伤害优先级"
description: "优先队列是『每次取出最高优先级元素』的数据结构，底层用二叉堆实现。A* 的 Open 列表、技能释放队列、伤害事件调度都依赖它。这篇讲清楚二叉堆的原理和实现，以及在游戏里的具体用法。"
slug: "ds-09-heap-priority-queue"
weight: 757
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 堆
  - 优先队列
  - 游戏架构
series: "数据结构与算法"
---

> "每次需要处理代价最小（或优先级最高）的那个"——这个需求在游戏里非常频繁。A* 每步取 f 值最小的节点，技能系统按优先级处理效果，伤害事件按时间戳调度。这些都是优先队列的场景。

---

## 为什么不用排序数组

最简单的优先队列实现：维护一个有序数组，每次取第一个。

```csharp
// 有序数组：插入 O(n)，取最小 O(1)
SortedList<float, AStarNode> open = new();
open.Add(fValue, node);   // 插入：要找到正确位置 O(log n)，但移动元素 O(n)
var best = open.Values[0]; // 取最小：O(1)
```

每次插入要移动元素，O(n)。在 A* 里每步都要插入多个节点——整体退化到 O(n²)。

**优先队列需要的操作**：
- 插入：O(log n)
- 取出最小/最大：O(log n)
- 查看最小/最大（不取出）：O(1)

二叉堆完美满足这三个要求。

---

## 二叉堆：原理

**最小堆（Min-Heap）**：每个节点的值 ≤ 它的子节点。根节点始终是最小值。

```
      1
    /   \
   3     2
  / \   / \
 7   4  5   6
```

**关键性质**：父节点 ≤ 子节点（最小堆）。不要求左右子节点有顺序关系。

**用数组存储**（不需要指针！）：

```
数组：[1, 3, 2, 7, 4, 5, 6]
索引：[0, 1, 2, 3, 4, 5, 6]

节点 i 的父节点：(i - 1) / 2
节点 i 的左子：  2 * i + 1
节点 i 的右子：  2 * i + 2
```

这是一个**完全二叉树**用数组的完美映射，内存连续，缓存友好。

---

## 二叉堆的核心操作

### 插入（Heapify Up）

把新元素加到数组末尾，然后不断与父节点比较，如果比父节点小就交换（上浮）：

```csharp
void Push(float value)
{
    heap.Add(value);
    HeapifyUp(heap.Count - 1);
}

void HeapifyUp(int index)
{
    while (index > 0)
    {
        int parent = (index - 1) / 2;
        if (heap[parent] <= heap[index]) break;  // 父节点 ≤ 当前节点，堆性质满足
        (heap[parent], heap[index]) = (heap[index], heap[parent]);
        index = parent;
    }
}
```

最多上浮 log(n) 层，O(log n)。

### 取出最小值（Heapify Down）

取出根节点（最小值），把数组末尾的元素移到根，然后不断与子节点比较，与较小的子节点交换（下沉）：

```csharp
float Pop()
{
    float min = heap[0];
    heap[0] = heap[heap.Count - 1];
    heap.RemoveAt(heap.Count - 1);
    if (heap.Count > 0) HeapifyDown(0);
    return min;
}

void HeapifyDown(int index)
{
    int count = heap.Count;
    while (true)
    {
        int left  = 2 * index + 1;
        int right = 2 * index + 2;
        int smallest = index;

        if (left  < count && heap[left]  < heap[smallest]) smallest = left;
        if (right < count && heap[right] < heap[smallest]) smallest = right;

        if (smallest == index) break;

        (heap[smallest], heap[index]) = (heap[index], heap[smallest]);
        index = smallest;
    }
}
```

最多下沉 log(n) 层，O(log n)。

---

## 完整的泛型最小堆

```csharp
public class MinHeap<T> where T : IComparable<T>
{
    private List<T> heap = new();

    public int  Count   => heap.Count;
    public T    Peek()  => heap.Count > 0 ? heap[0]
                           : throw new InvalidOperationException("堆为空");
    public bool IsEmpty => heap.Count == 0;

    public void Push(T item)
    {
        heap.Add(item);
        HeapifyUp(heap.Count - 1);
    }

    public T Pop()
    {
        if (heap.Count == 0) throw new InvalidOperationException("堆为空");
        T min = heap[0];
        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);
        if (heap.Count > 0) HeapifyDown(0);
        return min;
    }

    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (heap[p].CompareTo(heap[i]) <= 0) break;
            (heap[p], heap[i]) = (heap[i], heap[p]);
            i = p;
        }
    }

    private void HeapifyDown(int i)
    {
        int n = heap.Count;
        while (true)
        {
            int l = 2 * i + 1, r = 2 * i + 2, s = i;
            if (l < n && heap[l].CompareTo(heap[s]) < 0) s = l;
            if (r < n && heap[r].CompareTo(heap[s]) < 0) s = r;
            if (s == i) break;
            (heap[s], heap[i]) = (heap[i], heap[s]);
            i = s;
        }
    }
}
```

---

## .NET 内置 PriorityQueue

.NET 6+ 提供了 `PriorityQueue<TElement, TPriority>`：

```csharp
// 最小优先队列（priority 小的先出）
var pq = new PriorityQueue<string, int>();

pq.Enqueue("低优先级任务", 10);
pq.Enqueue("高优先级任务", 1);
pq.Enqueue("中优先级任务", 5);

while (pq.Count > 0)
{
    string task = pq.Dequeue();  // 输出：高优先级 → 中优先级 → 低优先级
    Debug.Log(task);
}

// 只查看不取出
pq.TryPeek(out string top, out int priority);
```

**注意**：`PriorityQueue` 不支持更新优先级（无 `DecreaseKey` 操作）。在 A* 中用重复入队 + 跳过过期条目的方式处理（见 DS-07）。

---

## 游戏中的应用

### A* Open 列表

```csharp
// A* 里用优先队列按 f(n) = g(n) + h(n) 取出最优节点
var open = new PriorityQueue<Vector2Int, float>();
open.Enqueue(startNode, 0f);

while (open.Count > 0)
{
    var curr = open.Dequeue();   // 取出 f 值最小的节点
    // ...
    open.Enqueue(neighbor, fValue);  // 插入新候选
}
```

### 技能效果优先级队列

很多 RPG 里，同一帧触发了多个效果（增益、减益、伤害），需要按优先级处理（高优先级效果先触发，可能影响后续效果）：

```csharp
public struct SkillEffect : IComparable<SkillEffect>
{
    public int priority;
    public Action execute;

    public int CompareTo(SkillEffect other)
        => other.priority.CompareTo(priority);  // 优先级高的先出（最大堆语义）
}

public class EffectQueue
{
    private MinHeap<SkillEffect> heap = new();

    public void Add(int priority, Action effect)
        => heap.Push(new SkillEffect { priority = priority, execute = effect });

    public void ProcessAll()
    {
        while (!heap.IsEmpty)
            heap.Pop().execute();
    }
}

// 使用
effectQueue.Add(100, () => ApplyShield());    // 护盾先于伤害
effectQueue.Add(50,  () => ApplyDamage());
effectQueue.Add(80,  () => ApplyBuff());
effectQueue.ProcessAll();
// 处理顺序：护盾(100) → Buff(80) → 伤害(50)
```

### 定时事件调度器

游戏里的"延迟触发"系统——N 秒后执行某个事件：

```csharp
public struct TimedEvent : IComparable<TimedEvent>
{
    public float triggerTime;
    public Action action;

    public int CompareTo(TimedEvent other)
        => triggerTime.CompareTo(other.triggerTime);  // 触发时间早的先出
}

public class EventScheduler
{
    private MinHeap<TimedEvent> events = new();

    public void Schedule(float delay, Action action)
    {
        float triggerTime = Time.time + delay;
        events.Push(new TimedEvent { triggerTime = triggerTime, action = action });
    }

    public void Update()
    {
        while (!events.IsEmpty && events.Peek().triggerTime <= Time.time)
            events.Pop().action.Invoke();
    }
}

// 使用
scheduler.Schedule(0.5f, () => SpawnBullet());   // 0.5 秒后生成子弹
scheduler.Schedule(2.0f, () => TriggerTrap());   // 2 秒后触发陷阱
scheduler.Schedule(0.1f, () => PlaySound());     // 0.1 秒后播放音效
// Update 里自动按时间顺序触发
```

### Top-K 问题：找最近的 K 个敌人

```csharp
// 从 1000 个敌人里找最近的 5 个，不需要完整排序
// 维护一个大小为 K 的最大堆（堆顶是当前 K 个里最远的）
// 遍历所有敌人：如果比堆顶更近，替换堆顶
List<Enemy> FindKNearest(Vector3 pos, List<Enemy> enemies, int k)
{
    // 最大堆（堆顶是最远的，方便快速判断是否要替换）
    var heap = new PriorityQueue<Enemy, float>();  // priority = -distance（取反变最大堆）

    foreach (var enemy in enemies)
    {
        float dist = Vector3.Distance(pos, enemy.transform.position);

        if (heap.Count < k)
        {
            heap.Enqueue(enemy, -dist);  // 取反：distance 大的 priority 小，先出
        }
        else if (dist < -heap.UnorderedItems.Max(x => x.Priority))
        {
            // 比堆里最远的更近，替换
            // 注意：.NET PriorityQueue 不支持直接替换堆顶，需要变通
            // 实际实现建议用自定义最大堆
        }
    }

    return heap.UnorderedItems.Select(x => x.Element).ToList();
}
```

---

## 堆化（Heapify）：从数组构建堆

从一个无序数组直接构建堆，比逐个插入更快：O(n) 而不是 O(n log n)。

```csharp
// Floyd 建堆算法：从最后一个非叶子节点开始，逐个下沉
void BuildHeap(List<float> arr)
{
    int n = arr.Count;
    for (int i = n / 2 - 1; i >= 0; i--)
        HeapifyDown(arr, i, n);
}

// 用途：把一组已有的数据快速变成堆
// 比如游戏开始时批量加载一批定时事件
```

---

## 小结

| 操作 | 二叉堆 | 有序数组 | 无序数组 |
|---|---|---|---|
| 插入 | O(log n) | O(n) | O(1) |
| 取最小 | O(log n) | O(1) | O(n) |
| 查看最小 | O(1) | O(1) | O(n) |
| 建堆 | O(n) | O(n log n) | O(1) |

- **优先队列 = 二叉堆**：每次取最小（或最大），O(log n) 插入和取出
- **用数组存储**：完全二叉树的数组映射，内存连续，无需指针
- **.NET 内置 `PriorityQueue`**：.NET 6+ 可直接用，游戏项目首选
- **A* Open 列表**：优先队列的最典型游戏应用
- **定时事件调度**：按触发时间排序，Update 里自动按时触发
- **Top-K 问题**：维护大小为 K 的堆，O(n log k) 找最近/最强的 K 个目标
