---
title: "数据结构与算法 19｜内存分配器：线性分配、栈分配、池分配与 Free List"
description: "系统默认的内存分配器（malloc/new）是通用的，但太慢、太容易产生碎片。游戏引擎针对不同的生命周期模式，用专用分配器替换它：线性分配器、栈分配器、池分配器、自由链表分配器。这篇讲清楚它们的原理、适用场景和 Unity 里的实际应用。"
slug: "ds-19-memory-allocators"
weight: 777
tags:
  - 软件工程
  - 内存管理
  - 性能优化
  - 游戏架构
series: "数据结构与算法"
---

> Unity 默认的 `new` 通过 .NET GC 堆分配内存，每次分配可能触发 GC，每次 GC 可能造成帧卡顿。游戏引擎底层用专用分配器完全绕过 GC，精确控制内存的分配和释放时机。

---

## 为什么通用分配器不够用

```
通用分配器（malloc / new）：
  优点：灵活，任意大小，任意时机分配和释放
  缺点：
    1. 速度慢：需要查找合适的空闲块（O(n) 或更复杂）
    2. 内存碎片：多次分配释放后，内存里出现很多小空洞
    3. 缓存不友好：不同时间分配的对象分散在内存各处
    4. .NET GC 延迟：GC 暂停时间不可预测
```

游戏的内存分配模式通常很规律：
- 每帧分配的临时数据，帧结束后全部释放
- 子弹、粒子：大量相同大小的对象，频繁创建销毁
- 关卡数据：加载时全部分配，卸载时全部释放

针对这些规律，专用分配器可以做到比通用分配器快几个数量级。

---

## 线性分配器（Linear Allocator / Arena Allocator）

**思路**：维护一个大内存块和一个"当前偏移量"。分配时把偏移量往后移，释放时整体重置。

```csharp
public unsafe class LinearAllocator : IDisposable
{
    private byte* memory;
    private int   offset;
    private int   capacity;

    public LinearAllocator(int capacityBytes)
    {
        // 分配一大块连续内存（只分配一次）
        memory   = (byte*)Marshal.AllocHGlobal(capacityBytes);
        capacity = capacityBytes;
        offset   = 0;
    }

    // 分配：O(1)，只需移动指针
    public T* Alloc<T>() where T : unmanaged
    {
        int size    = sizeof(T);
        int aligned = (offset + 7) & ~7;  // 8 字节对齐
        if (aligned + size > capacity) throw new OutOfMemoryException();
        T* ptr = (T*)(memory + aligned);
        offset = aligned + size;
        return ptr;
    }

    // 释放：O(1)，整体重置
    public void Reset() => offset = 0;

    public void Dispose() => Marshal.FreeHGlobal((IntPtr)memory);
}
```

**优点**：
- 分配代价接近 0（只移动一个整数）
- 内存连续，缓存极友好
- 无碎片

**缺点**：
- 无法单独释放某个对象，只能整体 Reset
- 适合"一批对象一起分配，一起释放"的场景

**游戏应用**：
```csharp
// 每帧的临时计算数据
LinearAllocator frameAlloc = new(1 * 1024 * 1024);  // 1MB

void Update()
{
    // 帧开始，重置分配器（不释放内存，只移动指针）
    frameAlloc.Reset();

    // 帧内分配临时数据（完全不产生 GC）
    var pathNodes = frameAlloc.Alloc<PathNode>(); // 寻路临时数据
    var visibleSet = frameAlloc.Alloc<int>();     // 可见集合
    // ...
    // 帧结束，Reset，下一帧复用
}
```

---

## 栈分配器（Stack Allocator）

在线性分配器基础上支持"按 LIFO 顺序释放"：

```csharp
public class StackAllocator
{
    private byte[] memory;
    private int    top;      // 当前栈顶
    private Stack<int> markers = new();  // 记录各层的栈顶位置

    public StackAllocator(int capacity)
    {
        memory = new byte[capacity];
        top    = 0;
    }

    // 分配
    public int Alloc(int size)
    {
        int ptr = top;
        top += size;
        return ptr;
    }

    // 标记当前位置（用于批量释放到某个点）
    public void PushMarker() => markers.Push(top);

    // 释放到上一个标记点（释放该标记以来的所有分配）
    public void PopMarker()  => top = markers.Pop();
}
```

**游戏应用**：关卡加载分层——

```
Marker 0（应用启动）
  └── 加载关卡通用资源（字体、UI 框架）
      Marker 1（进入关卡一）
        └── 加载关卡一特有资源
            Marker 2（进入战斗场景）
              └── 加载战斗特效、粒子
            PopMarker 2 → 卸载战斗场景资源
        PopMarker 1 → 卸载关卡一资源
```

---

## 池分配器（Pool Allocator）

**思路**：预分配 N 个相同大小的对象槽，用空闲链表管理空槽。分配和释放都是 O(1)。

```csharp
public class PoolAllocator<T> where T : class, new()
{
    private T[]        pool;
    private Stack<int> freeIndices = new();
    private int[]      generation;   // 版本号，检测 double-free

    public PoolAllocator(int capacity)
    {
        pool       = new T[capacity];
        generation = new int[capacity];

        // 初始化对象（预热）
        for (int i = 0; i < capacity; i++)
        {
            pool[i] = new T();
            freeIndices.Push(i);
        }
    }

    public (T obj, int index, int gen) Alloc()
    {
        if (freeIndices.Count == 0)
            throw new OutOfMemoryException("池已满");

        int idx = freeIndices.Pop();
        return (pool[idx], idx, generation[idx]);
    }

    public void Free(int index, int gen)
    {
        if (generation[index] != gen)
        {
            Debug.LogError($"double-free 或悬空引用！index={index}");
            return;
        }
        generation[index]++;  // 使所有旧引用失效
        freeIndices.Push(index);
    }
}
```

**游戏应用**：子弹、特效、敌人——与对象池（DS 系列设计模式篇 ObjectPool）原理相同，区别在于这里是非托管内存层面的池。

---

## 自由链表分配器（Free List Allocator）

最通用的专用分配器，支持任意大小的分配和单个释放，但要管理空闲块链表：

```csharp
// 每个内存块的头部存储：大小 + 空闲/占用标记 + 下一个空闲块的链接
struct BlockHeader
{
    public int  size;
    public bool isFree;
    public int  nextFreeOffset;  // 指向下一个空闲块（-1 = 无）
}

public class FreeListAllocator
{
    private byte[] memory;
    private int    freeListHead;  // 空闲链表的第一个块的偏移量

    // First Fit 策略：找到第一个够大的空闲块
    public int Alloc(int size)
    {
        int curr = freeListHead;
        while (curr != -1)
        {
            var header = ReadHeader(curr);
            if (header.isFree && header.size >= size)
            {
                // 可选：分割（如果剩余空间足够大，分成两个块）
                MarkUsed(curr, size);
                return curr + sizeof(BlockHeader);  // 返回数据区指针
            }
            curr = header.nextFreeOffset;
        }
        throw new OutOfMemoryException();
    }

    public void Free(int ptr)
    {
        int headerOffset = ptr - sizeof(BlockHeader);
        MarkFree(headerOffset);
        // 可选：合并相邻空闲块（Coalescing），减少碎片
        TryCoalesce(headerOffset);
    }

    // ... ReadHeader, MarkUsed, MarkFree, TryCoalesce 的实现
}
```

**游戏应用**：关卡编辑器的撤销/重做系统，需要任意大小的命令对象，但命令有明确的生命周期。

---

## Unity 中的内存管理实践

### NativeArray 和 NativeContainer（零 GC）

Unity DOTS 提供了零 GC 的容器，内部用非托管内存：

```csharp
// NativeArray：连续非托管内存，无 GC
var positions = new NativeArray<float3>(1000, Allocator.TempJob);
// Allocator.TempJob = 线性分配器，Job 结束后自动释放

// 在 Job 里安全访问（多线程）
var job = new UpdatePositionsJob { positions = positions };
var handle = job.Schedule(positions.Length, 64);
handle.Complete();
positions.Dispose();  // 显式释放

// 常用 Allocator：
// Allocator.Temp     = 当前帧内释放（最快）
// Allocator.TempJob  = Job 内释放（4帧内必须释放）
// Allocator.Persistent = 长期持有（较慢，类似 malloc）
```

### ArrayPool（.NET 标准库）

对于托管对象，用 `ArrayPool<T>` 复用数组避免 GC：

```csharp
// 不要每次 new int[]
int[] temp = ArrayPool<int>.Shared.Rent(256);
try
{
    // 使用 temp 做临时计算
    SortNeighbors(temp);
}
finally
{
    ArrayPool<int>.Shared.Return(temp);  // 归还给池，不触发 GC
}
```

---

## 小结

| 分配器 | 分配代价 | 释放代价 | 碎片 | 适用场景 |
|---|---|---|---|---|
| 线性分配器 | O(1)，极快 | 只能整体 Reset | 无 | 每帧临时数据、批量处理 |
| 栈分配器 | O(1) | O(1)，LIFO | 无 | 分层的生命周期（关卡加载） |
| 池分配器 | O(1) | O(1) | 无（固定大小） | 大量同大小对象（子弹、粒子） |
| 自由链表 | O(n)，最坏 | O(1) | 有（需要 coalesce）| 变长对象，明确生命周期 |
| GC 堆（new）| 快（摊销） | GC 暂停 | GC 处理 | 一般用途 |

- **线性分配器是最快的**：分配只是一次加法，适合一切"批量分配，一起释放"的场景
- **池分配器**：游戏里最常用，子弹/粒子/敌人的标准方案
- **NativeArray + Allocator.Temp**：Unity DOTS 的标准临时数据方案，完全绕过 GC
- **ArrayPool**：托管环境下的 GC 友好数组复用，适合中等规模临时数组
