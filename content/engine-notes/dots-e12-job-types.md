---
title: "Unity DOTS E12｜IJobEntity vs IJobChunk vs IJob：三种 Job 的适用边界与性能差异"
slug: "dots-e12-job-types"
date: "2026-03-28"
description: "三种 ECS Job 不是替代关系，而是不同粒度的控制层级。IJobEntity 是最简洁的高层抽象，IJobChunk 给你 Chunk 级别的控制权，IJob 用于非 ECS 的纯数据并行任务。本篇讲清楚三者的内存访问模式和选择依据。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "IJobEntity"
  - "IJobChunk"
  - "Jobs"
  - "Burst"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 12
weight: 1920
---

DOTS 的 Job System 不是一个接口，而是三个层级。很多人在 IJobEntity 上遇到限制时直接跳到 IJobChunk，却不清楚为什么需要这一跳，也不清楚什么时候该退出 ECS 体系直接用 IJob。这篇文章把三者的内存访问模式和适用边界讲清楚。

## 三者的本质差异

先建立一个直觉模型。ECS 数据在内存里的组织结构是：**Archetype → Chunk → Entity**。三种 Job 正好对应了操作这个层级结构的三个入口：

- **IJobEntity**：你只关心每个 Entity，框架帮你迭代所有 Chunk
- **IJobChunk**：你拿到每个 Chunk，自己决定怎么处理里面的 Entity
- **IJob**：你拿到原始 NativeArray，ECS 结构对你透明

这不是性能从低到高的排序，而是控制粒度从粗到细的分层。

---

## IJobEntity：最高层抽象

### 语法结构

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float DeltaTime;

    void Execute(ref LocalTransform transform, in Velocity velocity)
    {
        transform.Position += velocity.Value * DeltaTime;
    }
}
```

`partial struct` 是强制要求——Source Generator 需要在同一 struct 上追加生成的代码。`ref` 表示读写，`in` 表示只读，这两个关键字直接决定了 ComponentTypeHandle 是否带写权限，不是装饰性的。

### Source Generator 做了什么

IJobEntity 在编译期会被展开为 IJobChunk 实现。你写的 `Execute` 方法会被包装进一个标准的 `Execute(in ArchetypeChunk chunk, ...)` 调用，框架生成 ComponentTypeHandle 的获取、NativeArray 的切片、以及 Enabled Component 的掩码处理。

这意味着 IJobEntity 的运行时开销和等价的 IJobChunk 写法是相同的，没有额外的虚调用或装箱。

### 特殊参数

`Execute` 的参数除了 Component 之外，还支持几个带 Attribute 标注的内置参数：

```csharp
void Execute(
    [EntityIndexInQuery] int sortKey,       // 当前 Entity 在 Query 结果中的索引
    [ChunkIndexInQuery] int chunkIndex,     // 当前 Chunk 的索引
    Entity entity,                          // Entity 本身（只读，不需要 Attribute）
    ref LocalTransform transform,
    in Velocity velocity
)
```

`[EntityIndexInQuery]` 常用于向 EntityCommandBuffer.ParallelWriter 写入命令时作为 sortKey，保证并行写入的确定性排序。

### 过滤属性

```csharp
[BurstCompile]
[WithAll(typeof(ActiveTag))]              // 必须同时拥有 ActiveTag
[WithNone(typeof(DisabledTag))]           // 不能拥有 DisabledTag
[WithChangeFilter(typeof(Velocity))]      // 只处理 Velocity 在上一帧后发生变化的 Chunk
public partial struct MoveJob : IJobEntity
{
    public float DeltaTime;
    void Execute(ref LocalTransform transform, in Velocity velocity) { ... }
}
```

`[WithChangeFilter]` 会在 Chunk 级别跳过未变化的数据——但它的粒度是整个 Chunk，只要 Chunk 里有任意一个 Entity 的 Velocity 被写过（ChangeVersion 更新），整个 Chunk 都会被处理。

### 调度

```csharp
protected override void OnUpdate()
{
    var job = new MoveJob { DeltaTime = SystemAPI.Time.DeltaTime };

    // 并行调度，按 Chunk 分批，不同 Chunk 在不同线程执行
    job.ScheduleParallel();

    // 单线程顺序执行
    // job.Schedule();

    // 主线程立即同步执行（跳过 Job 队列，用于调试或数据量极少时）
    // job.Run();
}
```

---

## IJobChunk：Chunk 级控制

### 何时需要 IJobChunk

IJobEntity 能覆盖大多数场景，但有两类需求它处理不了：

1. **需要 Chunk 级元数据**：SharedComponent 的值、Chunk 内实际 Entity 数量、Chunk 的 ChangeVersion
2. **需要精确的 Chunk 级跳过逻辑**：不是"某个 Component 变了就处理整个 Chunk"，而是你自己判断多个条件后决定是否处理这个 Chunk

### 语法结构

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct ChunkMoveJob : IJobChunk
{
    public float DeltaTime;
    public uint LastSystemVersion;

    public ComponentTypeHandle<LocalTransform> TransformHandle;
    [ReadOnly] public ComponentTypeHandle<Velocity> VelocityHandle;

    public void Execute(
        in ArchetypeChunk chunk,
        int unfilteredChunkIndex,
        bool useEnabledMask,
        in v128 chunkEnabledMask)
    {
        // 手动判断：Velocity 在这个 Chunk 上是否有变化
        if (!chunk.DidChange(VelocityHandle, LastSystemVersion))
            return; // 整个 Chunk 跳过，完全不分配任何迭代开销

        var transforms = chunk.GetNativeArray(ref TransformHandle);
        var velocities = chunk.GetNativeArray(ref VelocityHandle);
        int count = chunk.Count;

        for (int i = 0; i < count; i++)
        {
            transforms[i] = new LocalTransform
            {
                Position = transforms[i].Position + velocities[i].Value * DeltaTime,
                Rotation = transforms[i].Rotation,
                Scale = transforms[i].Scale
            };
        }
    }
}
```

在 System 中调度：

```csharp
[BurstCompile]
public partial struct ChunkMoveSystem : ISystem
{
    private EntityQuery _query;
    private ComponentTypeHandle<LocalTransform> _transformHandle;
    private ComponentTypeHandle<Velocity> _velocityHandle;

    public void OnCreate(ref SystemState state)
    {
        _query = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<LocalTransform>()
            .WithAll<Velocity>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _transformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(false);
        _velocityHandle  = SystemAPI.GetComponentTypeHandle<Velocity>(true);

        var job = new ChunkMoveJob
        {
            DeltaTime         = SystemAPI.Time.DeltaTime,
            LastSystemVersion = state.LastSystemVersion,
            TransformHandle   = _transformHandle,
            VelocityHandle    = _velocityHandle,
        };

        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }
}
```

### IJobChunk 的真正优势

`chunk.DidChange(handle, lastVersion)` 比 `[WithChangeFilter]` 更灵活：你可以组合多个 Component 的变更状态，或者加入 SharedComponent 的值判断，然后决定这个 Chunk 的处理策略——这是 IJobEntity 的属性做不到的。

对于状态变化稀疏的场景（比如大量静止物体、只有少数激活），精确跳过 Chunk 能带来显著的性能差距。

---

## IJob：纯数据并行

IJob 和 ECS 没有直接关系，它只是 Unity Job System 的基础接口。

```csharp
[BurstCompile]
public struct PathSmoothJob : IJob
{
    public NativeArray<float3> Waypoints;
    public float SmoothFactor;

    public void Execute()
    {
        for (int i = 1; i < Waypoints.Length - 1; i++)
        {
            Waypoints[i] = math.lerp(
                Waypoints[i],
                (Waypoints[i - 1] + Waypoints[i + 1]) * 0.5f,
                SmoothFactor
            );
        }
    }
}
```

### 与 ECS 配合使用

典型模式是从 Query 提取数据、用 IJob 处理、写回结果：

```csharp
[BurstCompile]
public partial struct PathSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 从 Query 拿到 NativeArray（分配在 TempJob 生命周期）
        var query = SystemAPI.QueryBuilder().WithAll<Waypoint>().Build();
        var waypoints = query.ToComponentDataArray<Waypoint>(Allocator.TempJob);

        // 转换为 float3 数组传给纯计算 Job
        var positions = new NativeArray<float3>(waypoints.Length, Allocator.TempJob);
        for (int i = 0; i < waypoints.Length; i++)
            positions[i] = waypoints[i].Value;

        var smoothJob = new PathSmoothJob
        {
            Waypoints    = positions,
            SmoothFactor = 0.5f,
        };

        state.Dependency = smoothJob.Schedule(state.Dependency);
        // 结果在 Dependency 完成后写回，此处省略
        waypoints.Dispose(state.Dependency);
        positions.Dispose(state.Dependency);
    }
}
```

IJob 适合那些本质上不是"遍历 Entity"的任务：路径平滑、空间分区构建、物理碰撞预处理、统计聚合。这类任务的输入输出是结构化数组，硬套 IJobEntity 只会让代码更难读。

---

## 三种 Job 横向对比

| 维度 | IJobEntity | IJobChunk | IJob |
|------|-----------|-----------|------|
| 抽象层级 | Entity 级 | Chunk 级 | 无 ECS |
| 代码量 | 最少 | 中等 | 少 |
| Chunk 级跳过 | 仅通过属性 | 完全可控 | N/A |
| Chunk 元数据访问 | 不支持 | 支持 | N/A |
| Burst 兼容 | 是 | 是 | 是 |
| ScheduleParallel | 支持 | 支持 | 不支持（单 Job） |
| 适合场景 | 通用 ECS 逻辑 | 需要 Chunk 元数据或精确跳过 | 非 ECS 纯数据计算 |
| 学习成本 | 低 | 中 | 低 |

**选择建议：**

- 默认从 IJobEntity 开始，它覆盖了 80% 的 ECS 场景
- 需要判断 Chunk ChangeVersion、SharedComponent 值、或者精确控制 Chunk 级跳过逻辑时，升级到 IJobChunk
- 输入输出是纯 NativeArray、与 ECS 结构无关的计算任务，用 IJob

不要为了"更底层"而用 IJobChunk——它的代码量更多，出错概率更高，只在真正需要 Chunk 级控制时才有价值。

---

## 关于 ScheduleParallel 的一点说明

`ScheduleParallel` 的并行单位是 **Chunk**，不是 Entity。调度器会把 Query 匹配到的所有 Chunk 分发给 Worker Thread，每个 Worker 处理一个或多个 Chunk。

这意味着：

- 同一个 Chunk 内的 Entity 总是在同一个线程上顺序执行
- 不同 Chunk 之间没有数据共享（Component 数据分布在不同 Chunk），天然无竞争
- Chunk 越多、每个 Chunk 越满（256 bytes 上限），并行效率越高
- 如果 Query 结果只有 1 个 Chunk，`ScheduleParallel` 和 `Schedule` 效果相同

`.Run()` 跳过整个 Job 调度机制，在主线程同步执行。适合数据量极少（< 100 个 Entity）的调试场景，或需要在同一帧内立刻读回结果的情况。生产代码里大量使用 `.Run()` 会抵消 DOTS 的并行收益。

---

## 小结

三种 Job 的关系不是替代，而是互补的控制层级。IJobEntity 的 Source Generator 展开本质是 IJobChunk，IJobChunk 的内部迭代本质是对 NativeArray 的 for 循环。理解这个展开链，就能在正确的层级做正确的事，而不是在错误的层级硬撑。

下一篇 **E13「Burst 编译规则全景」** 会讲清楚为什么这三种 Job 都能标注 `[BurstCompile]`，Burst 对托管代码的限制边界在哪里，以及如何定位 Burst 编译失败的根本原因。
