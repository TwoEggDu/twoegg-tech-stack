---
title: "数据结构与算法 16｜哈希表深度：碰撞解决、负载因子与游戏里的隐患"
description: "哈希表是游戏代码里最常用的数据结构之一（Dictionary、HashSet 无处不在），但它的性能陷阱很容易被忽视：哈希函数质量、碰撞解决策略、扩容触发的 GC、糟糕的自定义哈希。这篇把哈希表从原理到工程实践讲透。"
slug: "ds-16-hashtable"
weight: 771
tags:
  - 软件工程
  - 数据结构
  - 哈希表
  - 性能优化
  - 游戏架构
series: "数据结构与算法"
---

> Dictionary<K,V> 是 C# 里用得最多的容器之一，但很少人能说清楚它的 Add 为什么偶尔会比平时慢 100 倍，或者为什么自定义了 GetHashCode 后 Dictionary 查询速度骤降。

---

## 哈希表的基本原理

```
哈希表 = 数组 + 哈希函数

存储 key-value 对：
1. 用哈希函数计算 key 的哈希值：hash = key.GetHashCode()
2. 映射到数组索引：index = hash % arraySize
3. 在 array[index] 存储 value

查询：
1. 计算 hash，得到 index
2. 检查 array[index]

理想情况：O(1) 插入和查询
```

**问题**：两个不同的 key 可能得到相同的 index（**哈希碰撞**）。

---

## 碰撞解决策略

### 策略一：链式法（Chaining）

每个数组槽存一个链表，碰撞的元素都加进同一个链表。

```
array[3] → [("apple", 1)] → [("cherry", 2)] → null
            ↑ hash("apple")%8=3   ↑ hash("cherry")%8 也是 3
```

```csharp
// 概念实现
class ChainedHashTable<K, V>
{
    private LinkedList<(K key, V value)>[] buckets;

    V Get(K key)
    {
        int idx = Math.Abs(key.GetHashCode()) % buckets.Length;
        foreach (var (k, v) in buckets[idx])
            if (k.Equals(key)) return v;
        throw new KeyNotFoundException();
    }
}
```

**优点**：实现简单，负载因子可以超过 1。
**缺点**：每个链表节点是堆分配，缓存不友好（指针追踪）。

### 策略二：开放地址法（Open Addressing）

所有元素都存在数组里（无链表）。碰撞时按某种规则找下一个空槽（**探测序列**）。

```
线性探测：碰撞时尝试 index+1, index+2, index+3, ...
二次探测：尝试 index+1², index+2², index+3², ...
双重哈希：用第二个哈希函数决定步长
```

```csharp
// 线性探测
int FindSlot(int[] keys, int key)
{
    int idx = key % keys.Length;
    while (keys[idx] != EMPTY && keys[idx] != key)
        idx = (idx + 1) % keys.Length;
    return idx;
}
```

**优点**：数据连续存储，缓存友好。
**缺点**：负载因子不能太高（通常 < 0.7），否则探测链变长，性能急剧下降。

**.NET 的 Dictionary\<K,V\>** 使用的是链式法的变体（每个桶是数组中的一个条目链，没有真正的链表，用数组索引代替指针）。

---

## 负载因子与扩容

**负载因子（Load Factor）**= 已有元素数 / 数组容量。

.NET Dictionary 默认负载因子阈值 = 1（链式法允许超过 1），但实际上 .NET 7+ 使用了更激进的扩容策略。

**扩容过程**：
1. 分配新的（更大的）数组（通常是当前容量的 2 倍或下一个质数）
2. 把所有已有元素 **重新哈希**（rehash）到新数组
3. 丢弃旧数组

**扩容是 O(n) 操作**，会在某次 Add 调用时突然发生，导致帧时间尖刺。

```csharp
// 避免扩容触发 GC 的做法：提前指定容量
var dict = new Dictionary<int, Enemy>(expectedCount);
// 如果知道大约会有多少元素，提前设置容量，避免中途扩容

// List 同理
var list = new List<GameObject>(100);
```

---

## 哈希函数的质量

哈希函数质量直接决定碰撞频率，进而决定性能。

### int 和 long 的哈希

```csharp
// int 的 GetHashCode 就是它本身
int x = 42;
x.GetHashCode();  // 返回 42

// 问题：如果 key 全是 8 的倍数，且数组大小是 8 的倍数
// 所有 key 都映射到同一个桶！→ 哈希表退化为链表
```

避免连续整数哈希退化的技巧：

```csharp
// 用质数大小的数组（.NET Dictionary 内部就用质数）
// 或者用 Fibonacci 哈希（乘以黄金比例的整数近似值）
static int FibonacciHash(int key)
{
    return (int)((uint)key * 2654435769u);
}
```

### Vector2Int 的自定义哈希

游戏里经常用 `Vector2Int` 或 `(int x, int y)` 作为字典的键（格子地图坐标）：

```csharp
// 糟糕的实现：大量碰撞
struct TileKey_Bad
{
    public int x, y;
    public override int GetHashCode() => x + y;  // (1,3) 和 (2,2) 哈希相同！
}

// 好的实现：用质数混合
struct TileKey_Good
{
    public int x, y;
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            return hash;
        }
    }
    public override bool Equals(object obj)
    {
        if (obj is TileKey_Good other)
            return x == other.x && y == other.y;
        return false;
    }
}

// 或者用内置的 HashCode 结合工具（.NET Core+）
public override int GetHashCode() => HashCode.Combine(x, y);
```

**忘记重写 Equals 的陷阱**：

```csharp
// 如果只重写了 GetHashCode 但没重写 Equals
// Dictionary 查找时：哈希相同 → 进入同一桶 → 用 Equals 比较
// 默认 Equals 是引用比较 → 明明是"相同"的键，却查不到！

struct MyKey
{
    public int x, y;
    public override int GetHashCode() => HashCode.Combine(x, y);
    // ↓ 必须同时重写！
    public override bool Equals(object obj)
        => obj is MyKey other && x == other.x && y == other.y;
}
```

---

## 游戏中常见的哈希表陷阱

### 陷阱一：在 Update 里频繁分配字典

```csharp
// 错误：每帧 new 一个 Dictionary，触发 GC
void Update()
{
    var visited = new Dictionary<int, bool>();  // 每帧分配！
    DoPathfinding(visited);
}

// 正确：缓存并每帧 Clear（Clear 不释放内存）
private Dictionary<int, bool> visited = new();
void Update()
{
    visited.Clear();
    DoPathfinding(visited);
}
```

### 陷阱二：enum 作为字典键的 boxing

```csharp
// 在旧版 .NET 中，enum 的 GetHashCode 会 boxing
enum SkillType { Fire, Ice, Thunder }

// 会产生 boxing（旧 .NET）
var dict = new Dictionary<SkillType, SkillData>();

// 解决：实现 IEqualityComparer 避免 boxing，或改用 int 键
// .NET Core 3+ / .NET 5+ 已修复，Unity 2021+ 不再有此问题
```

### 陷阱三：string 键的 GC

```csharp
// 每次字符串拼接都会产生新的 string 对象
string key = "enemy_" + enemyId;  // 每帧 new string
dict[key] = damage;

// 更好：用 int ID 替代字符串键
dict[enemyId] = damage;
```

### 陷阱四：HashSet 判重时漏掉 Equals

```csharp
// 自定义类型不重写 Equals/GetHashCode
// HashSet 以为每个实例都不同（因为引用不同）
var visited = new HashSet<GridNode>();
visited.Add(node1);
visited.Contains(new GridNode(1, 1));  // 返回 false！即使坐标相同

// 必须：重写 GetHashCode + Equals，或实现 IEquatable<T>
```

---

## 内存布局与性能

.NET Dictionary 的内部结构（概念）：

```
entries[] = 紧凑的数组，每个条目包含：
  int    hashCode   // 缓存的哈希值
  int    next       // 链中下一个条目的索引（-1 表示链尾）
  TKey   key
  TValue value

buckets[] = 每个槽指向 entries 数组中链头的索引
```

这种结构的关键：entries 是连续数组，查找时的指针追踪都在同一块内存里，比真正的链表缓存友好得多。

---

## 小结

- **碰撞解决**：链式法（.NET Dictionary）vs 开放地址法（更缓存友好）
- **负载因子**：超过阈值触发扩容（O(n) rehash），提前指定容量避免帧时间尖刺
- **哈希函数质量**：质量差 → 大量碰撞 → O(n) 退化；`HashCode.Combine` 是通用安全选择
- **自定义键必须同时重写 GetHashCode 和 Equals**：只重写其中一个是 bug
- **Update 里不要 new Dictionary**：缓存 + Clear，避免 GC
- **string 键性能差**：优先用 int ID，避免每帧字符串拼接
