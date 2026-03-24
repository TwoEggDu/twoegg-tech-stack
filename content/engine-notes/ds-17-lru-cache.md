---
title: "数据结构与算法 17｜LRU Cache：资源流式加载与内存管理"
description: "LRU（Least Recently Used）缓存淘汰策略用于：有限内存里保留最近用到的资源，丢弃最久未使用的。游戏里的贴图流式加载、Chunk 缓存、UI 资源管理都是 LRU 的经典场景。实现上哈希表 + 双向链表 = O(1) 全操作。"
slug: "ds-17-lru-cache"
weight: 773
tags:
  - 软件工程
  - 数据结构
  - 算法
  - 缓存
  - 内存管理
  - 游戏架构
series: "数据结构与算法"
---

> 游戏地图 4GB，内存只有 512MB。不可能全部加载——只加载玩家附近的 Chunk，当玩家走远后卸载最久未访问的，为新 Chunk 腾出空间。这就是 LRU Cache 的核心场景。

---

## 什么是 LRU

**LRU（Least Recently Used）**：当缓存满了，淘汰"最久没有被使用过"的那个条目。

```
缓存容量：3
操作序列：访问 A, B, C, D

访问 A：[A]          缓存未满，直接加入
访问 B：[B, A]       最近访问的放最前
访问C：  [C, B, A]   满了
访问D：  [D, C, B]   A 是最久未使用的，淘汰 A，加入 D
访问B：  [B, D, C]   B 已在缓存，移到最前（更新访问时间）
访问A：  [A, B, D]   C 被淘汰，加载 A
```

---

## 实现：哈希表 + 双向链表

要让所有操作都是 O(1)：
- **哈希表**：O(1) 查找某个 key 是否在缓存里，并直接拿到节点引用
- **双向链表**：O(1) 把某个节点移到头部（最近访问），O(1) 删除尾部（最久未访问）

```csharp
public class LRUCache<TKey, TValue>
{
    private class Node
    {
        public TKey   Key;
        public TValue Value;
        public Node   Prev, Next;
    }

    private readonly int               capacity;
    private readonly Dictionary<TKey, Node> map = new();

    // 哨兵节点（虚拟头尾，简化边界处理）
    private readonly Node head = new();  // 最近使用端
    private readonly Node tail = new();  // 最久未使用端

    public LRUCache(int capacity)
    {
        this.capacity = capacity;
        head.Next = tail;
        tail.Prev = head;
    }

    // 访问 key，返回 value；不存在返回 default
    public TValue Get(TKey key)
    {
        if (!map.TryGetValue(key, out var node))
            return default;

        MoveToFront(node);   // 最近访问，移到头部
        return node.Value;
    }

    // 插入/更新
    public void Put(TKey key, TValue value)
    {
        if (map.TryGetValue(key, out var existing))
        {
            existing.Value = value;
            MoveToFront(existing);
            return;
        }

        var node = new Node { Key = key, Value = value };
        map[key] = node;
        AddToFront(node);

        if (map.Count > capacity)
            Evict();  // 超出容量，淘汰最久未使用的
    }

    public bool ContainsKey(TKey key) => map.ContainsKey(key);

    // -- 链表操作 --

    private void AddToFront(Node node)
    {
        node.Prev = head;
        node.Next = head.Next;
        head.Next.Prev = node;
        head.Next      = node;
    }

    private void Remove(Node node)
    {
        node.Prev.Next = node.Next;
        node.Next.Prev = node.Prev;
    }

    private void MoveToFront(Node node)
    {
        Remove(node);
        AddToFront(node);
    }

    private void Evict()
    {
        var lru = tail.Prev;   // 链表尾部 = 最久未访问
        Remove(lru);
        map.Remove(lru.Key);
        OnEvict(lru.Key, lru.Value);
    }

    // 子类可以重写这个方法，在淘汰时做清理（卸载资源等）
    protected virtual void OnEvict(TKey key, TValue value) { }
}
```

---

## 游戏场景一：Chunk 流式加载

开放世界游戏将地图切分为 Chunk，只保留玩家附近的 Chunk 在内存里：

```csharp
public class ChunkCache : LRUCache<Vector2Int, Chunk>
{
    private const int MaxCachedChunks = 64;  // 同时保留 64 个 Chunk

    public ChunkCache() : base(MaxCachedChunks) { }

    // 淘汰时自动卸载
    protected override void OnEvict(Vector2Int coord, Chunk chunk)
    {
        chunk.Unload();
        Debug.Log($"卸载 Chunk {coord}（LRU 淘汰）");
    }

    // 获取 Chunk（缓存命中直接返回，未命中则加载）
    public Chunk GetOrLoad(Vector2Int coord)
    {
        var chunk = Get(coord);
        if (chunk == null)
        {
            chunk = LoadChunkFromDisk(coord);  // 磁盘加载
            Put(coord, chunk);
        }
        return chunk;
    }

    private Chunk LoadChunkFromDisk(Vector2Int coord) { /* ... */ return new Chunk(); }
}

// 使用
public class WorldManager : MonoBehaviour
{
    private ChunkCache chunkCache = new();

    void Update()
    {
        var playerChunk = WorldToChunkCoord(player.position);

        // 预加载周围 3x3 的 Chunk
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
            chunkCache.GetOrLoad(playerChunk + new Vector2Int(dx, dy));
    }
}
```

---

## 游戏场景二：贴图/材质缓存

动态 UI 系统（如无限滚动列表）需要异步加载图标，LRU 缓存避免重复加载：

```csharp
public class IconCache : LRUCache<string, Sprite>
{
    private const int MaxIcons = 100;

    public IconCache() : base(MaxIcons) { }

    protected override void OnEvict(string path, Sprite sprite)
    {
        // 减少引用计数，让 UnityEngine.Object 被 GC
        Resources.UnloadAsset(sprite.texture);
    }

    public async UniTask<Sprite> GetOrLoadAsync(string path)
    {
        var sprite = Get(path);
        if (sprite != null) return sprite;

        sprite = await LoadSpriteAsync(path);
        Put(path, sprite);
        return sprite;
    }

    private async UniTask<Sprite> LoadSpriteAsync(string path) { /* Addressables加载 */ return null; }
}
```

---

## 游戏场景三：AI 决策缓存

复杂的 AI 行为树评估代价高，用 LRU 缓存最近计算的结果（对相似状态的单位复用决策）：

```csharp
// 对 AI 状态哈希，缓存最近 N 个状态的决策结果
// 减少每帧重复计算（当状态没有变化时）
public class AIDecisionCache : LRUCache<int, AIAction>
{
    public AIDecisionCache() : base(200) { }

    public AIAction GetDecision(AIState state)
    {
        int hash = state.GetHashCode();
        var action = Get(hash);
        if (action == null)
        {
            action = EvaluateBehaviorTree(state);
            Put(hash, action);
        }
        return action;
    }

    private AIAction EvaluateBehaviorTree(AIState state) { /* 复杂计算 */ return null; }
}
```

---

## 线程安全 LRU

多线程场景（资源加载线程 + 游戏主线程）需要线程安全的 LRU：

```csharp
public class ThreadSafeLRUCache<TKey, TValue> : LRUCache<TKey, TValue>
{
    private readonly ReaderWriterLockSlim rwLock = new();

    public new TValue Get(TKey key)
    {
        rwLock.EnterWriteLock();    // Get 需要写锁（因为会修改链表顺序）
        try { return base.Get(key); }
        finally { rwLock.ExitWriteLock(); }
    }

    public new void Put(TKey key, TValue value)
    {
        rwLock.EnterWriteLock();
        try { base.Put(key, value); }
        finally { rwLock.ExitWriteLock(); }
    }
}
```

---

## 变体：LFU（Least Frequently Used）

LRU 淘汰"最久未用的"，LFU 淘汰"使用次数最少的"：

```
LRU 问题（缓存污染）：
  某个资源被大量一次性访问（比如过场动画的贴图）
  之后很长时间不用，但挤占了真正常用资源的位置

LFU 更适合"热点资源明确、冷热分布稳定"的场景
  比如：游戏主界面的 UI 素材一定比某张剧情贴图用得多
```

实际游戏引擎通常用 LRU 或 LRU 变体（CLOCK 算法、2Q 策略）而不是纯 LFU，因为 LFU 对新资源不友好（新加载的资源频率为 1，很容易被淘汰）。

---

## 小结

- **LRU = 哈希表 + 双向链表**：O(1) 访问、O(1) 插入、O(1) 淘汰，没有理由用 O(n) 的实现
- **哨兵节点**：虚拟头尾节点消除链表边界条件，代码更简洁
- **OnEvict 钩子**：淘汰时触发资源卸载，是 LRU 缓存与资源管理结合的关键
- **Chunk 流式加载**：LRU 最典型的游戏应用，维护"玩家附近 N 个块"的滑动窗口
- **容量设置**：容量太小 → 频繁缓存未命中；太大 → 内存压力；通常按"预期工作集大小 × 1.5"设置
