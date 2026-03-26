---
date: "2026-03-26"
title: "数据结构与算法 02｜连续内存与缓存局部性：为什么数组比链表快"
description: "同样是存一组数据，数组和链表的性能差距在游戏场景下可以达到数十倍。这篇从 CPU 缓存的工作原理出发，讲清楚内存布局对性能的实际影响，以及游戏开发中如何用数据布局换性能。"
slug: "ds-02-cache-locality"
weight: 743
tags:
  - 软件工程
  - 数据结构
  - 性能优化
  - 缓存局部性
  - 游戏架构
series: "数据结构与算法"
---

> DOD（数据导向设计，pattern-07）讲的是"设计数据的布局"。这篇是它的底层原理——CPU 缓存是如何工作的，以及为什么内存连续性对游戏性能如此关键。

---

## CPU 缓存：被忽视的硬件约束

现代 CPU 的运算速度远超内存访问速度。为了弥补这个差距，CPU 有三级缓存（L1/L2/L3）：

```
访问延迟（近似值）：
寄存器：         < 1 周期
L1 缓存（32KB）：  4 周期
L2 缓存（256KB）： 12 周期
L3 缓存（8MB）：   40 周期
主内存（RAM）：    200 周期

从 RAM 读数据比从 L1 读慢 50 倍。
```

关键机制：**缓存行（Cache Line）**

CPU 读取内存时，不是只读你要的那几个字节，而是一次读取 **64 字节**（一个 Cache Line）。如果接下来要用的数据恰好在这 64 字节里，就是"缓存命中"——直接从 L1 读，4 个周期；如果不在，就是"缓存缺失"——要从 RAM 重新加载，200 个周期。

**缓存命中率是现代 CPU 性能的核心指标之一。**

---

## 数组 vs 链表：一个决定性的差距

### 数组的内存布局

```
int[] arr = { 10, 20, 30, 40, 50, 60, 70, 80 };

内存（连续）：
地址 0x1000: [10][20][30][40][50][60][70][80]
              ↑ 一个 Cache Line（32字节，8个int）一次全装进来
```

当你遍历数组时，第一次读取触发一次 Cache Miss，把 64 字节都加载进 L1。接下来的 15 个 `int`（或 10 个 `float3`）都在这 64 字节里，缓存命中。

### 链表的内存布局

```csharp
class Node
{
    public int value;
    public Node next;  // 指针指向另一个堆对象
}
```

```
内存（分散）：
地址 0x1000: [value=10][next→0x4A80]    ← Node 1
地址 0x4A80: [value=20][next→0x2B30]    ← Node 2（4页内存之外）
地址 0x2B30: [value=30][next→0x8C10]    ← Node 3（又一个新位置）
```

每个 `Node` 是单独 `new` 出来的，散落在堆的各处。遍历时，每访问一个节点就可能触发一次 Cache Miss——每次都要从 RAM 加载新的 64 字节。

### 实测差距

```
遍历 100,000 个元素求和：

数组（int[]）：          ~0.1 ms
链表（LinkedList<int>）：~3.0 ms

差距：约 30 倍
```

操作次数完全一样（都是 O(n)），差距完全来自**缓存效率**。

---

## List<T> vs LinkedList<T>：Unity 里的选择

C# 的 `List<T>` 底层是数组（`T[]`），`LinkedList<T>` 是双向链表。

```csharp
// 绝大多数情况下，用 List<T>
List<Enemy> enemies = new List<Enemy>();

// LinkedList<T> 的唯一优势：O(1) 的中间插入/删除（当你有节点引用时）
// 但这个优势在游戏里很少值得为缓存性能付出的代价
LinkedList<Enemy> enemies = new LinkedList<Enemy>();
```

**实际结论**：

| 操作 | List | LinkedList |
|---|---|---|
| 按索引访问 | O(1) | O(n) |
| 尾部追加 | 均摊 O(1) | O(1) |
| 中间插入 | O(n)（移动元素） | O(1)（有节点引用时） |
| 遍历性能 | 极快（缓存友好） | 慢（指针追踪） |
| 内存占用 | 紧凑 | 每个节点额外 2 个指针 |

在游戏里，"频繁在中间插入/删除"这个需求大多数时候可以用其他方式解决（Swap-and-Pop、标记删除），所以 `List<T>` 几乎是默认选择。

---

## Swap-and-Pop：缓存友好的删除

从 `List` 中间删除一个元素，标准做法是 `RemoveAt(i)`，它会把后面所有元素前移 — O(n)。

但如果你不关心顺序，有一个 O(1) 的技巧：

```csharp
// 把要删除的元素和最后一个元素交换，然后删除最后一个
void SwapAndPop<T>(List<T> list, int index)
{
    int last = list.Count - 1;
    list[index] = list[last];  // 覆盖要删除的位置
    list.RemoveAt(last);       // 删除最后一个（O(1)）
}

// 使用
void RemoveEnemy(int index)
{
    SwapAndPop(enemies, index);
    // enemies 的顺序改变了，但如果你不需要保序，这没问题
}
```

这个技巧在粒子系统、子弹系统、敌人列表里非常常用——顺序无关紧要，但删除频率极高。

---

## 结构体数组（SoA）vs 对象数组（AoS）

这是 DOD（pattern-07）的核心内容，从缓存角度再看一遍：

```csharp
// AoS（Array of Structures）：每个对象包含所有数据
public class Enemy
{
    public Vector3 position;   // 12 bytes
    public Vector3 velocity;   // 12 bytes
    public float health;       //  4 bytes
    public Sprite sprite;      //  8 bytes（引用）
    // 每个 Enemy 对象约 40+ bytes，还有对象头开销
}
Enemy[] enemies = new Enemy[1000];  // 1000 个分散的堆对象

// 更新所有位置时，每个 Enemy 的 Cache Line 还装着 health/sprite——全是噪音
foreach (var e in enemies)
    e.position += e.velocity * dt;
```

```csharp
// SoA（Structure of Arrays）：同类数据连续存储
public struct EnemyDatabase
{
    public Vector3[] positions;   // [pos0][pos1][pos2]... 连续
    public Vector3[] velocities;  // [vel0][vel1][vel2]... 连续
    public float[]   healths;     // [hp0][hp1][hp2]...   连续
}

// 更新所有位置时，Cache Line 里装的全是 position 数据
for (int i = 0; i < count; i++)
    db.positions[i] += db.velocities[i] * dt;
// 每次 Cache Miss 加载 64 字节 ≈ 5 个 Vector3，全是有用数据
```

在 1000~10000 个单位规模下，SoA 版本的性能提升通常是 3~10 倍。

---

## struct vs class：栈分配与堆分配

```csharp
// class：引用类型，分配在堆上，GC 管理，访问需要解引用
class HealthComponent { public float current, max; }

// struct：值类型，小的 struct 分配在栈上，或内嵌在数组里
struct HealthData { public float current, max; }

HealthData[] healths = new HealthData[1000];
// 内存：[c0,m0][c1,m1][c2,m2]... 紧密相邻，连续存储
// 遍历时缓存命中率极高

HealthComponent[] healths = new HealthComponent[1000];
// 内存：[ref0][ref1][ref2]... 数组本身是连续的，但每个 ref 指向堆上的独立对象
// 遍历时每个 ref 都要解引用 → 随机内存访问 → Cache Miss
```

**原则**：纯数据的小型结构（< 32 bytes 经验值）优先用 `struct`，放进数组里，缓存利用率最高。

---

## 字符串：游戏里的性能陷阱

C# 的 `string` 是引用类型，每次拼接都分配新对象：

```csharp
// 每次调用都分配一个新字符串对象 → 持续产生 GC 垃圾
void Update()
{
    string log = "Enemy count: " + enemies.Count;  // 每帧分配
    Debug.Log(log);
}

// 用 StringBuilder 重用缓冲区
private StringBuilder sb = new StringBuilder(64);

void Update()
{
    sb.Clear();
    sb.Append("Enemy count: ");
    sb.Append(enemies.Count);
    Debug.Log(sb);  // 不分配新字符串
}
```

更好的方式：在生产代码里用整数 ID 代替字符串做查找，字符串只在最终展示时使用。

---

## 内存访问模式总结

```
顺序访问（数组遍历）：     缓存命中率 ~100%，最快
步进访问（每 N 个元素）：  命中率下降，步长越大越慢
随机访问（链表、指针追踪）：命中率最低，最慢

实际测试（遍历 1MB 数据）：
顺序访问：  ~1ms
步长 64：   ~4ms（每次都跨 Cache Line）
随机访问：  ~30ms（每次都 Cache Miss）
```

---

## 小结

- **Cache Line（64 字节）**：CPU 每次从内存加载的最小单位，连续的数据共用 Cache Line，离散的数据各自触发 Cache Miss
- **数组比链表快的原因**：不是操作次数少，而是缓存命中率高
- **Swap-and-Pop**：无序删除的 O(1) 技巧，保持数组紧凑
- **SoA vs AoS**：处理特定字段时，SoA 的缓存利用率远高于 AoS
- **struct 数组**：值类型内嵌在数组里，内存连续，比引用类型数组缓存友好得多
- **字符串**：引用类型，频繁拼接产生 GC 压力；热路径用 `StringBuilder` 或整数 ID
