---
title: "Unity DOTS E14｜NativeCollection 选型：Array / List / HashMap / Queue / MultiHashMap / Stream"
slug: "dots-e14-native-collection"
date: "2026-03-28"
description: "NativeCollection 是 ECS Job 里唯一能安全传递和修改的数据容器。不同容器有不同的内存布局、并发模型和适用场景。本篇讲清楚每种容器的特性、分配器选择和 Dispose 时机。"
tags:
  - "Unity"
  - "DOTS"
  - "NativeArray"
  - "NativeList"
  - "NativeHashMap"
  - "Jobs"
  - "内存管理"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 14
weight: 1940
---

## 为什么 Job 只能用 NativeCollection

C# 托管对象（`List<T>`、`Dictionary<TKey, TValue>`、普通数组等）分配在 GC heap 上。Job System 要求 Job struct 只包含 blittable 数据，不能持有托管引用——原因很简单：Job 可能在任意线程上执行，而 GC 在回收时会移动堆上对象的地址，这在多线程环境下会造成悬空引用。

NativeCollection 解决了这个问题：

- 内存分配在 **Native Memory**（Unmanaged heap），不在 GC heap，GC 不会移动也不会自动回收它。
- Job struct 按值复制传入线程，但底层 native 指针指向同一块内存，所以数据共享是安全的。
- 在 Editor 下，Unity 的 **SafetyHandle** 机制会检测越界访问、双重释放、非法并发写，帮助尽早暴露问题。
- 离开作用域后不会自动释放——你必须手动调用 `Dispose()`，或者用 `using` 语句管理生命周期。

---

## 分配器（Allocator）选择

分配器是创建 NativeCollection 时最先要决定的事，选错了要么运行时报错，要么造成内存泄漏。

| Allocator | 生命周期 | 典型用途 |
|-----------|---------|---------|
| `Allocator.Temp` | 当前帧内（不能跨帧，不能传入 Job） | 方法内部临时计算 |
| `Allocator.TempJob` | 最长 4 帧（超时 Unity 会报警告） | 分配后传入 Job，完成后 Dispose |
| `Allocator.Persistent` | 无限期，直到手动 Dispose | System 的成员变量 |

```csharp
// Temp：只在当前方法内用，离开前必须 Dispose
var tempArr = new NativeArray<int>(64, Allocator.Temp);
// ... 使用 ...
tempArr.Dispose();

// TempJob：分配后调度 Job，Job 完成后 Dispose
var jobArr = new NativeArray<float>(count, Allocator.TempJob);
var handle = new MyJob { Data = jobArr }.Schedule();
handle.Complete();
jobArr.Dispose();

// Persistent：System 成员变量，OnCreate 分配，OnDestroy 释放
public partial class MySystem : SystemBase
{
    NativeList<Entity> _cache;

    protected override void OnCreate()
    {
        _cache = new NativeList<Entity>(128, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        _cache.Dispose(); // 忘记这一行就是内存泄漏
    }
}
```

---

## NativeArray\<T\>

**特性：** 固定大小，分配时确定长度，不能 resize。内存布局与 `T[]` 完全等价，但在 native memory 上，因此可以被 Burst 编译器当作连续内存块做向量化优化。

**并发模型：** 多个 Job 可以同时以 `[ReadOnly]` 方式读，但任意一个 Job 写入时其余 Job 不能访问。

**适合场景：** 数量已知的数据传递、ComponentLookup 的批量缓存、Query 结果的固定大小复制。

```csharp
[BurstCompile]
public struct ScaleJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> Input;
    [WriteOnly] public NativeArray<float> Output;

    public void Execute(int index)
    {
        Output[index] = Input[index] * 2f;
    }
}

// 调度示例
var input  = new NativeArray<float>(1024, Allocator.TempJob);
var output = new NativeArray<float>(1024, Allocator.TempJob);
// 填充 input ...
var handle = new ScaleJob { Input = input, Output = output }
    .Schedule(1024, 64);
handle.Complete();
input.Dispose();
output.Dispose();
```

---

## NativeList\<T\>

**特性：** 动态大小，支持 `Add`、`RemoveAtSwapBack`，内部是一块可扩容的 NativeArray 加上 length 计数器。

**并发模型：** 默认不能在并行 Job 中直接调用 `Add`（会触发 SafetyHandle 报错）。需要通过 `AsParallelWriter()` 获取并行写句柄，该句柄允许多线程同时 Enqueue 但不保证顺序。

**适合场景：** 数量未知的结果收集（串行 Job 或 IJob）、Entity 过滤后的输出列表。

```csharp
[BurstCompile]
public struct CollectEntitiesJob : IJobChunk
{
    [ReadOnly] public ComponentTypeHandle<HealthComponent> HealthHandle;
    [ReadOnly] public EntityTypeHandle EntityHandle;
    public NativeList<Entity>.ParallelWriter Result;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex,
                        bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var healths  = chunk.GetNativeArray(ref HealthHandle);
        var entities = chunk.GetNativeArray(EntityHandle);
        for (int i = 0; i < chunk.Count; i++)
        {
            if (healths[i].Value <= 0f)
                Result.AddNoResize(entities[i]); // 并行写
        }
    }
}

// 调度
var deadList = new NativeList<Entity>(Allocator.TempJob);
var handle = new CollectEntitiesJob
{
    HealthHandle = GetComponentTypeHandle<HealthComponent>(true),
    EntityHandle = GetEntityTypeHandle(),
    Result       = deadList.AsParallelWriter()
}.ScheduleParallel(_query);
handle.Complete();
// 处理 deadList ...
deadList.Dispose();
```

---

## NativeHashMap\<TKey, TValue\> 与 NativeParallelHashMap\<TKey, TValue\>

**NativeHashMap：** 标准哈希表，O(1) 查找。并行 Job 中只能标记 `[ReadOnly]` 读取，写入必须在串行 Job（`IJob`）里完成。

**NativeParallelHashMap：** 并发写安全版本，允许多个线程同时写入，但同一个 key 不能被两个线程同时写（会发生 last-write-wins 覆盖，不报错但结果不确定）。

**适合场景：** Entity → 数值的映射（如血量快照）、去重逻辑、跨 Job 传递 lookup 表。

```csharp
// 串行 Job 写入 HashMap
[BurstCompile]
public struct BuildLookupJob : IJob
{
    [ReadOnly] public NativeArray<Entity>         Entities;
    [ReadOnly] public NativeArray<HealthComponent> Healths;
    public NativeHashMap<Entity, float>           LookupOut;

    public void Execute()
    {
        for (int i = 0; i < Entities.Length; i++)
            LookupOut.TryAdd(Entities[i], Healths[i].Value);
    }
}

// 并行 Job 读取 HashMap
[BurstCompile]
public struct ApplyDamageJob : IJobParallelFor
{
    [ReadOnly] public NativeHashMap<Entity, float> BaseLookup;
    public NativeArray<HealthComponent>            Healths;
    [ReadOnly] public NativeArray<Entity>          Entities;

    public void Execute(int index)
    {
        if (BaseLookup.TryGetValue(Entities[index], out float baseVal))
            Healths[index] = new HealthComponent { Value = baseVal - 10f };
    }
}
```

---

## NativeQueue\<T\>

**特性：** FIFO 队列，支持 `Enqueue`/`Dequeue`。通过 `AsParallelWriter()` 可在并行 Job 中多线程入队，出队只能在串行 Job 或主线程中进行。

**适合场景：** 命令队列（生产者 Job → 消费者 Job）、事件队列（碰撞事件、动画触发通知）。

```csharp
NativeQueue<DamageCommand> _commandQueue;

// 并行 Job 中入队
[BurstCompile]
public struct EnqueueDamageJob : IJobParallelFor
{
    public NativeQueue<DamageCommand>.ParallelWriter Queue;
    [ReadOnly] public NativeArray<Entity> Targets;

    public void Execute(int index)
    {
        Queue.Enqueue(new DamageCommand { Target = Targets[index], Amount = 5f });
    }
}

// 串行 Job 中出队消费
[BurstCompile]
public struct ProcessDamageJob : IJob
{
    public NativeQueue<DamageCommand> Queue;
    public ComponentLookup<HealthComponent> HealthLookup;

    public void Execute()
    {
        while (Queue.TryDequeue(out var cmd))
        {
            if (HealthLookup.TryGetComponent(cmd.Target, out var hp))
                HealthLookup[cmd.Target] = new HealthComponent { Value = hp.Value - cmd.Amount };
        }
    }
}
```

---

## NativeMultiHashMap\<TKey, TValue\>

**特性：** 同一个 key 可以存储多个 value，底层依然是哈希表，但 bucket 里是链表结构。用 `GetValuesForKey` 或迭代器遍历同一 key 的所有值。

**适合场景：** Entity → 多个 Buff 效果、区域格子 → 多个 Entity（空间分区）、骨骼 → 多个蒙皮权重。

```csharp
[BurstCompile]
public struct BuildCellMapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Entity>     Entities;
    [ReadOnly] public NativeArray<Translation> Positions;
    public NativeParallelMultiHashMap<int, Entity>.ParallelWriter CellMap;
    public float CellSize;

    public void Execute(int index)
    {
        int cellKey = HashCell(Positions[index].Value, CellSize);
        CellMap.Add(cellKey, Entities[index]);
    }

    static int HashCell(float3 pos, float size)
    {
        int x = (int)math.floor(pos.x / size);
        int z = (int)math.floor(pos.z / size);
        return x * 73856093 ^ z * 19349663;
    }
}

// 读取某格子内所有 Entity
if (cellMap.TryGetFirstValue(key, out Entity e, out var it))
{
    do { ProcessEntity(e); }
    while (cellMap.TryGetNextValue(out e, ref it));
}
```

---

## NativeStream

**特性：** 专为并行 Job 大量写入而设计。内部按 foreachCount（通常等于 chunk 数）分配独立的写缓冲区，每个线程只写自己的 stream，写完后通过 `NativeStream.Reader` 顺序合并读取。不需要事先知道每个线程会写多少条数据。

**适合场景：** 并行 Job 产生数量未知的输出，例如碰撞事件列表、动画触发事件、LOD 变更通知。

```csharp
[BurstCompile]
public struct DetectCollisionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<AABB> Bounds;
    public NativeStream.Writer EventWriter;

    public void Execute(int index)
    {
        EventWriter.BeginForEachIndex(index);
        for (int j = index + 1; j < Bounds.Length; j++)
        {
            if (Bounds[index].Overlaps(Bounds[j]))
                EventWriter.Write(new CollisionEvent { A = index, B = j });
        }
        EventWriter.EndForEachIndex();
    }
}

// 调度与读取
int count = bounds.Length;
var stream = new NativeStream(count, Allocator.TempJob);

new DetectCollisionJob
{
    Bounds      = bounds,
    EventWriter = stream.AsWriter()
}.Schedule(count, 1).Complete();

var reader = stream.AsReader();
for (int i = 0; i < count; i++)
{
    int itemCount = reader.BeginForEachIndex(i);
    for (int k = 0; k < itemCount; k++)
    {
        var ev = reader.Read<CollisionEvent>();
        HandleCollision(ev);
    }
    reader.EndForEachIndex();
}

stream.Dispose();
```

---

## 常见内存泄漏模式

**1. OnDestroy 忘记 Dispose**

```csharp
// 错误写法
protected override void OnCreate()
{
    _lookup = new NativeHashMap<Entity, float>(256, Allocator.Persistent);
}
// 没有 OnDestroy，_lookup 永远不会被释放
```

Unity 在 Play Mode 结束时会输出 `A Native Collection has not been disposed` 警告，并附上分配时的堆栈，利用这个信息可以快速定位。

**2. Temp 容器在异常路径上没有释放**

```csharp
// 有风险的写法
var arr = new NativeArray<int>(64, Allocator.Temp);
DoSomethingThatMightThrow(); // 如果抛异常，arr 就泄漏了
arr.Dispose();

// 正确写法
var arr = new NativeArray<int>(64, Allocator.Temp);
try
{
    DoSomethingThatMightThrow();
}
finally
{
    arr.Dispose();
}
// 或者等价的：using var arr = new NativeArray<int>(64, Allocator.Temp);
```

**3. TempJob 超时未释放**

`TempJob` 分配的容器超过 4 帧没有 Dispose，Unity 会输出 `TempJob Allocator has allocations that are more than 4 frames old` 警告。常见原因是 Job 的 handle 被忘记 Complete，导致 Dispose 从未被调用。

---

## 选型决策表

| 场景 | 推荐容器 |
|------|---------|
| 固定大小数据传递 | `NativeArray<T>` |
| 数量未知结果收集（串行 Job） | `NativeList<T>` |
| 并行 Job 结果收集 | `NativeList<T>.AsParallelWriter()` 或 `NativeStream` |
| Key-Value 映射（串行写，并行读） | `NativeHashMap<TKey, TValue>` |
| Key-Value 映射（并行写） | `NativeParallelHashMap<TKey, TValue>` |
| 命令 / 事件队列 | `NativeQueue<T>` |
| 一对多映射 | `NativeMultiHashMap<TKey, TValue>` |
| 并行 Job 产生数量未知的大量输出 | `NativeStream` |

---

## 小结

NativeCollection 是 DOTS Job 体系的数据基础设施。选型的核心逻辑只有三个问题：

1. **大小确定吗？** 确定用 `NativeArray`，不确定用 `NativeList` 或 `NativeStream`。
2. **并行写吗？** 需要并行写就用带 `Parallel` 前缀的变体，或 `AsParallelWriter()`，或 `NativeStream`。
3. **生命周期多长？** 单帧用 `Temp`，跨 Job 用 `TempJob`，跨帧用 `Persistent`，并在对应的生命周期结束时 Dispose。

---

**Jobs / Burst 三篇（E12～E14）到此完结。** E12 讲了 IJob 与 IJobParallelFor 的调度模型，E13 讲了 Burst 编译器的原理与 `[BurstCompile]` 的使用约束，E14（本篇）覆盖了 NativeCollection 的全部核心容器。三篇合在一起构成了 DOTS 数据并行计算的完整工具链。

**下一组是进阶篇（E15～E17）**，将深入 ECS 的进阶特性：EntityCommandBuffer 的延迟写入机制、SystemGroup 调度顺序与 World 多实例，以及 Baking 工作流与 SubScene 的资产管理模型。
