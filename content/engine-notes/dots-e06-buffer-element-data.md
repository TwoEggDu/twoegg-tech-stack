---
title: "Unity DOTS E06｜IBufferElementData：动态缓冲区替代 List<T> 的时机与写法"
slug: "dots-e06-buffer-element-data"
date: "2026-03-28"
description: "IBufferElementData 是 ECS 里每个 Entity 持有可变长度数据的机制，存储方式和 List<T> 完全不同。本篇讲清楚 Buffer 的内存布局、容量超限时的行为、在 Job 中的安全访问，以及什么场景适合用 Buffer 而不是引用类型。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "IBufferElementData"
  - "DynamicBuffer"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 6
weight: 1860
---

## 为什么不能用 List\<T\>

ECS 的核心要求是 `IComponentData` 必须是 **unmanaged struct**——不能包含任何托管类型（class、interface、数组、`List<T>`）。原因很直接：Chunk 内存是一块连续的非托管堆，GC 无法追踪其中的托管引用。如果你尝试在 Component 里放一个 `List<Waypoint>`，编译器会直接报错。

传统 MonoBehaviour 思路里，"一个对象持有一组数据"是用 `List<T>` 实现的。ECS 的替代方案是 `IBufferElementData`——它让每个 Entity 持有一段**可变长度**的 unmanaged 数据序列，并由 Entities 包负责内存管理。

---

## 内存布局：内联 vs 外联

`DynamicBuffer<T>` 的存储策略取决于当前元素数量是否超过 **InternalCapacity**。

```
InternalCapacity = 8（默认，元素大小 <= 8 字节时约为 8 个）

[ 内联模式：元素数量 <= InternalCapacity ]

  Chunk 内存
  ┌───────────────────────────────────────────┐
  │  Entity[0]  │  Entity[1]  │  Entity[2]   │
  │  Component  │  Component  │  Component   │
  │  Buffer[0]  │  Buffer[0]  │  Buffer[0]   │
  │  Buffer[1]  │  Buffer[1]  │  ...         │
  │  ...        │  ...        │              │
  └───────────────────────────────────────────┘
        Buffer 数据直接内联在 Chunk 里，无堆分配

[ 外联模式：元素数量 > InternalCapacity ]

  Chunk 内存
  ┌───────────────────────────────────────┐
  │  Entity[0]  │  ...                   │
  │  Component  │                        │
  │  ptr ──────────────────────┐         │
  │  len=12     │              │         │
  └───────────────────────────────────────┘
                               ↓
                    Heap 独立分配块
                    ┌─────────────────────┐
                    │ elem[0] ~ elem[11]  │
                    └─────────────────────┘
```

一旦元素数量超过 InternalCapacity，Entities 会在非托管堆上申请一块新内存，把所有元素搬过去，Chunk 里只保留一个指针和长度。这个过程是**自动的**，你不需要手动触发，但它意味着一次额外的内存分配和数据拷贝。

### 调整 InternalCapacity

用 `[InternalBufferCapacity(N)]` 可以覆盖默认值：

```csharp
// 预期路径点不超过 16 个，避免外联
[InternalBufferCapacity(16)]
public struct WaypointElement : IBufferElementData
{
    public float3 Position;
}
```

把 InternalCapacity 设得太大会让每个 Chunk 能装下的 Entity 数量减少（因为每个 Entity 占用的字节变多），需要根据实际数据量权衡。

---

## 基本用法

### 定义 Buffer 元素类型

```csharp
using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(8)]
public struct WaypointElement : IBufferElementData
{
    public float3 Position;
}
```

### 在 Authoring 中添加 Buffer

```csharp
using Unity.Entities;
using UnityEngine;

public class WaypointAuthoring : MonoBehaviour
{
    public Vector3[] Waypoints;

    class Baker : Baker<WaypointAuthoring>
    {
        public override void Bake(WaypointAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<WaypointElement>(entity);
            foreach (var wp in authoring.Waypoints)
                buffer.Add(new WaypointElement { Position = wp });
        }
    }
}
```

### 在 System 中读写 Buffer

```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[BurstCompile]
public partial struct WaypointSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (buffer, transform) in
            SystemAPI.Query<DynamicBuffer<WaypointElement>, RefRW<LocalTransform>>())
        {
            if (buffer.Length == 0) continue;

            // 读取第一个路径点
            float3 target = buffer[0].Position;

            // 追加一个新路径点
            buffer.Add(new WaypointElement { Position = target + new float3(0, 1, 0) });

            // 删除已经抵达的路径点
            buffer.RemoveAt(0);

            // 查询长度与容量
            int len = buffer.Length;
            int cap = buffer.Capacity;
        }
    }
}
```

`DynamicBuffer<T>` 的 API 和 `List<T>` 高度相似：`Add`、`RemoveAt`、`Insert`、`Clear`、索引器——但它是值语义，不涉及 GC。

---

## 在 Job 中访问 Buffer

Query 里直接使用 `DynamicBuffer<T>` 参数即可在主线程和 `IJobEntity` 中访问。如果需要在 **IJobParallelFor** 等并行 Job 里通过 Entity 查找 Buffer，要用 `BufferLookup<T>`。

```csharp
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public partial struct ReadWaypointJob : IJobEntity
{
    // 只读 Lookup，多个 Job 可以并发读取
    [ReadOnly] public BufferLookup<WaypointElement> WaypointLookup;

    void Execute(Entity entity, ref LocalTransform transform)
    {
        if (!WaypointLookup.HasBuffer(entity)) return;

        var buffer = WaypointLookup[entity];
        if (buffer.Length == 0) return;

        // 仅读取，不修改
        float3 next = buffer[0].Position;
        transform.Position = math.lerp(transform.Position, next, 0.1f);
    }
}

[BurstCompile]
public partial struct WaypointLookupSystem : ISystem
{
    BufferLookup<WaypointElement> _waypointLookup;

    public void OnCreate(ref SystemState state)
    {
        _waypointLookup = state.GetBufferLookup<WaypointElement>(isReadOnly: true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _waypointLookup.Update(ref state);

        new ReadWaypointJob
        {
            WaypointLookup = _waypointLookup
        }.ScheduleParallel();
    }
}
```

**并行安全规则：**
- `[ReadOnly] BufferLookup<T>`：多个线程可以同时读取不同 Entity 的 Buffer，安全。
- 可写 `BufferLookup<T>`：同一帧内不能有其他 Job 同时访问相同 Entity 的 Buffer，Safety System 会在运行时检测并报错。
- 在同一个 `IJobEntity` 里通过 Query 直接传入 `DynamicBuffer<T>`：并行安全，每个线程只处理自己分配到的 Entity。

---

## 性能特性

| 情形 | 访问开销 |
|------|---------|
| 内联 Buffer（元素数 <= InternalCapacity） | 等同于普通 Component 字段访问，无额外分配 |
| 外联 Buffer（超出 InternalCapacity） | 一次额外指针解引用 + 可能 cache miss |
| 频繁增删导致反复扩容/缩容 | 每次外联分配都有非托管堆开销 |

如果你的 Buffer 元素数量在编译期已知且固定（比如永远是 4 个），直接在 Component 里放 4 个字段或使用 `FixedList32Bytes<T>` 更高效——后者是一个内嵌在 struct 里的小型固定数组，不需要 Buffer 机制。

---

## 适合用 Buffer 的场景

- **路径点列表（Waypoints）**：每个 NPC 有自己的巡逻路径，长度各不相同。
- **技能效果列表（EffectBuffer）**：一个 Entity 当前生效的 Buff/Debuff，数量动态变化。
- **子 Entity 引用（ChildrenBuffer）**：场景树中父节点记录子节点的 Entity 引用列表（Entities 自带的 `LinkedEntityGroup` 就是这种模式）。
- **聊天消息历史**：需要追加、截断，但不跨 Entity 共享。

---

## 不适合用 Buffer 的场景

- **元素数量固定**：直接用 Component 字段或 `FixedList`，省去 Buffer 的管理开销。
- **需要跨 Entity 共享同一份数据**：Buffer 是每个 Entity 独有的。跨 Entity 共享应使用 `ISharedComponentData`（引用类型版本）或挂在单例 Entity 上的全局 `NativeList`。
- **元素是托管对象（class）**：Buffer 只支持 unmanaged 数据，不可能存放 `GameObject`、`string` 等托管引用。

---

## 小结

`IBufferElementData` 填补了 ECS 里"可变长度数据"的空缺，让每个 Entity 可以持有一段独立的、GC-free 的元素序列。关键决策点只有两个：**InternalCapacity 设多大**（太小会外联，太大浪费 Chunk 空间），以及**是否真的需要动态长度**（固定长度就别用 Buffer）。

下一篇 **DOTS-E07「ISharedComponentData」** 将讨论另一个方向：当多个 Entity 需要共享同一份数据时，如何用 SharedComponent 避免冗余存储，以及它对 Chunk 分组的深远影响。
