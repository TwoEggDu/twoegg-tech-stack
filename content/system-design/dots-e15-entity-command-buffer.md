---
title: "Unity DOTS E15｜EntityCommandBuffer：延迟结构变更的正确用法、并发 ECB 与常见踩坑"
slug: "dots-e15-entity-command-buffer"
date: "2026-03-28"
description: "ECB 不是异步机制，而是「结构变更要在同步点外批量提交」的设计约束的直接体现。本篇讲清楚为什么不能在 Job 里直接做结构变更、ECB 的正确用法、并发 ECB 的特殊要求，以及常见踩坑。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "EntityCommandBuffer"
  - "Structural Change"
  - "Jobs"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 15
weight: 1950
---

## 从一个崩溃说起

你在一个 `IJobChunk` 里写了这样的代码：

```csharp
entityManager.DestroyEntity(entity); // 编译报错
```

编译器直接拒绝了。再换一种方式，把 `EntityManager` 注入进去试试？同样不行——`EntityManager` 在 Job 内部根本不允许做结构变更操作。

这不是 API 设计疏漏，而是内存模型的硬约束。理解这个约束，才能真正用好 EntityCommandBuffer（ECB）。

---

## 为什么 Job 里不能直接做结构变更

ECS 的结构变更（Structural Change）指以下操作：

- 为 Entity 添加 / 移除 Component
- 创建 / 销毁 Entity
- 向 DynamicBuffer 追加数据超出容量

这类操作会**重组 Chunk 内存**：Entity 从一个 Archetype 的 Chunk 搬到另一个 Chunk，或者整个 Chunk 被释放。

现在考虑并行场景：两个 Job 同时在跑，Job A 持有 Chunk X 的 `NativeArray<LocalTransform>`，Job B 在 Chunk X 上执行 `DestroyEntity`。Job B 执行后，Chunk X 被释放，Job A 手里的 `NativeArray` 指向的是已释放的内存——轻则读到脏数据，重则直接崩溃。

Safety System 在调度时会做依赖检查，但它无法检查"运行时动态发生的内存重组"，所以 Unity 的解决方案是：**结构变更只能在主线程，且必须在所有相关 Job 完成之后执行**。

这个执行时机就是**同步点（Sync Point）**。

---

## ECB 的本质：命令录制器

ECB 的设计思路很直接：既然 Job 里不能做结构变更，那就让 Job 把"我想做什么"记录下来，等到主线程的同步点统一执行。

几个常见误解需要澄清：

| 误解 | 实际情况 |
|------|----------|
| ECB 是异步执行 | 播放（Playback）是同步的，在主线程单线程执行 |
| ECB 像消息队列，随时消费 | 播放是批量原子执行，时机由 ECB System 决定 |
| ECB 越多越好 | 每次 Playback 都是一次同步点，过多会破坏并行效率 |

ECB 有两种触发播放的方式：

1. **托管给 ECB System**：把 ECB 的生命周期交给某个 `EntityCommandBufferSystem`，它会在自身 Update 时自动 Playback 并 Dispose。
2. **手动播放**：自己调用 `ecb.Playback(entityManager)`，然后 `ecb.Dispose()`。

---

## 基本用法

### 从 ECB System 获取 ECB

```csharp
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct DestroyOnHealthZeroSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 从 EndSimulationEntityCommandBufferSystem 获取 ECB
        // 播放时机：当帧 Simulation 结束后
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (health, entity) in
            SystemAPI.Query<RefRO<HealthComponent>>().WithEntityAccess())
        {
            if (health.ValueRO.Value <= 0f)
            {
                // 录制命令，不是立即执行
                ecb.DestroyEntity(entity);
            }
        }
        // 无需手动 Dispose，ECB System 会负责
    }
}
```

注意：这里是单线程 `foreach`，不是 Job，所以直接使用 `ecb` 即可。

### 录制常用命令

```csharp
// 销毁 Entity
ecb.DestroyEntity(entity);

// 添加 Component（带初始值）
ecb.AddComponent(entity, new Velocity { Value = float3.zero });

// 添加 Component（仅类型，值为默认）
ecb.AddComponent<Dead>(entity);

// 移除 Component
ecb.RemoveComponent<Active>(entity);

// 创建新 Entity（返回一个"临时 Entity"，播放后变为真实 Entity）
Entity newEntity = ecb.CreateEntity();
ecb.AddComponent(newEntity, new SpawnTag());
```

---

## 并发 ECB（ParallelWriter）

当 Job 通过 `ScheduleParallel` 并行调度时，多个线程会同时向同一个 ECB 写入命令。这时需要用 `ecb.AsParallelWriter()` 获取并发写入器。

### 为什么需要 chunkIndex

并发写入时，多个线程的写入顺序是不确定的。ECB 需要保证**播放结果的确定性**（相同逻辑跑两次结果相同），解决方案是：

- 每条命令附带一个 `sortKey`（排序键）
- 播放前按 `sortKey` 排序，相同 `sortKey` 的命令保持录制时的相对顺序

`IJobChunk` 提供的 `unfilteredChunkIndex` 天然满足这个需求：同一个 Chunk 只会被一个线程处理，不同 Chunk 的 `chunkIndex` 不同，因此用它作为 `sortKey` 既保证了顺序确定性，又无需额外计算。

### 完整并行 Job 示例

```csharp
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public partial struct ParallelDestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        // AsParallelWriter 在 Job 外调用一次
        var ecbParallel = ecbSingleton
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();

        new DestroyExpiredJob { ECB = ecbParallel }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct DestroyExpiredJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        // unfilteredChunkIndex 由框架自动注入，作为 sortKey
        public void Execute(
            [ChunkIndexAsQuery] int unfilteredChunkIndex,
            Entity entity,
            in LifetimeComponent lifetime)
        {
            if (lifetime.Remaining <= 0f)
            {
                // 并发写入必须传 sortKey（unfilteredChunkIndex）
                ECB.DestroyEntity(unfilteredChunkIndex, entity);
            }
        }
    }
}
```

如果使用 `IJobChunk`，则在 `Execute` 方法签名里直接有 `int unfilteredChunkIndex` 参数，传入方式相同。

---

## ECB 播放时机选择

Unity Entities 内置了以下 ECB System，按帧内执行顺序排列：

| ECB System | 适用场景 |
|---|---|
| `BeginInitializationEntityCommandBufferSystem` | 初始化最开始，常用于场景加载后的初始化 |
| `EndInitializationEntityCommandBufferSystem` | 初始化结束后 |
| `BeginSimulationEntityCommandBufferSystem` | Simulation 开始前，用于当帧生效的生成逻辑 |
| `EndSimulationEntityCommandBufferSystem` | **最常用**，Simulation 结束后处理销毁、状态变更 |
| `BeginPresentationEntityCommandBufferSystem` | 渲染前，用于视觉相关的最终调整 |

选择原则：**让变更在下一个合理的处理点之前生效**。销毁逻辑通常选 `EndSimulation`，这样当帧的所有逻辑都处理完后再清理，避免其他 System 在同一帧访问已被标记的 Entity 时出现歧义。

### 手动播放

不依赖 ECB System 时，完整流程如下：

```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // 分配一个临时 ECB
    var ecb = new EntityCommandBuffer(Allocator.TempJob);

    // ... 录制命令 ...

    // 完成所有 Job 后，在主线程播放
    state.Dependency.Complete();
    ecb.Playback(state.EntityManager);

    // 必须手动 Dispose，否则内存泄漏
    ecb.Dispose();
}
```

---

## 常见踩坑

### 坑 1：忘记 Dispose 导致内存泄漏

手动创建的 ECB 如果没有 `Dispose`，会在 Editor 的 Leak Detection 中产生警告，真机上则是静默泄漏。规则很简单：**谁创建，谁 Dispose**。托管给 ECB System 的不需要手动处理。

### 坑 2：同帧 Create 又 Destroy

```csharp
Entity e = ecb.CreateEntity();
ecb.AddComponent<Tag>(e);
ecb.DestroyEntity(e);  // 同一个 ECB 里
```

播放时按录制顺序执行：先创建，再加 Component，再销毁。最终这个 Entity 在播放帧内被销毁。这不是 bug，但通常意味着逻辑设计有问题，需要检查录制路径。

### 坑 3：并发 ECB 忘记传 chunkIndex

```csharp
// 错误写法
ecbParallel.DestroyEntity(entity); // 编译错误：缺少 sortKey 参数

// 正确写法
ecbParallel.DestroyEntity(unfilteredChunkIndex, entity);
```

`ParallelWriter` 的所有录制方法都要求第一个参数是 `int sortKey`，编译器会强制检查。

### 坑 4：在 OnDestroy 里使用 ECB System

```csharp
public void OnDestroy(ref SystemState state)
{
    // 危险：World 销毁时，ECB System 可能已不存在
    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    // 可能抛出 InvalidOperationException
}
```

`OnDestroy` 是 World 销毁流程的一部分，此时 ECB System 的存在性无法保证。如果必须在 OnDestroy 里做清理，使用 `state.EntityManager` 直接操作（此时已在主线程，结构变更是安全的），或检查 World 有效性后再获取 Singleton。

### 坑 5：ECB 在 Job 调度后被提前 Dispose

```csharp
var ecb = new EntityCommandBuffer(Allocator.TempJob);
var job = new MyJob { ECB = ecb.AsParallelWriter() };
var handle = job.ScheduleParallel();
ecb.Dispose(); // 错误：Job 还没完成，ECB 就被释放了
handle.Complete();
```

正确做法是先 `handle.Complete()` 再 `ecb.Dispose()`，或者使用 `ecb.Dispose(handle)` 传入依赖句柄，让 Job System 在 Job 完成后自动释放。

---

## 小结

ECB 的核心逻辑一句话：**Job 里录制，主线程同步点播放**。

记住几个关键点：

- 结构变更破坏内存布局，必须在同步点执行，Job 内不能直接操作
- ECB 不是异步，Playback 是同步的单线程批量执行
- 并行 Job 必须用 `AsParallelWriter()`，且每条命令需要传 `sortKey`（`unfilteredChunkIndex`）
- 手动创建的 ECB 必须手动 Dispose，托管给 ECB System 的不需要

下一篇 **E16「Entities.Graphics 渲染接入」** 将讨论如何让 DOTS 的 Entity 真正显示在屏幕上：`RenderMeshArray`、`MaterialMeshInfo` 的配置方式，以及 Hybrid Renderer 和 GPU Instancing 的工作机制。
