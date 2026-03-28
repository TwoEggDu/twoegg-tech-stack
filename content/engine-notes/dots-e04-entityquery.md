---
title: "Unity DOTS E04｜EntityQuery 完整语法：过滤器、变更检测、EnabledMask、缓存"
slug: "dots-e04-entityquery"
date: "2026-03-28"
description: "EntityQuery 不只是过滤 Component 类型的工具，它的缓存机制、变更版本号、EnabledMask 和 ChunkFilter 都有性能含义。本篇把这些机制讲清楚，让你写出高效的 Query 代码。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "EntityQuery"
  - "变更检测"
  - "EnabledMask"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 4
weight: 1840
---

EntityQuery 是 ECS 数据访问的入口。它描述"我想操作哪些 Entity"，让 World 把符合条件的 Archetype 和 Chunk 交给你。但如果只把它当成一个简单的类型过滤器，就会错过很多性能工具——变更检测、EnabledMask、SharedComponent 过滤，每一个都和性能直接挂钩。

## 1. 基础构建语法

在 `ISystem` 中，推荐通过 `SystemAPI.QueryBuilder()` 构建 Query：

```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MoveSystem : ISystem
{
    private EntityQuery _movableQuery;

    public void OnCreate(ref SystemState state)
    {
        // 构建 Query：同时拥有 LocalTransform 和 MoveSpeed，且没有 Frozen tag
        _movableQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
            .WithAll<LocalTransform, MoveSpeed>()
            .WithNone<Frozen>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        // 直接使用缓存好的 Query
        var job = new MoveJob { DeltaTime = SystemAPI.Time.DeltaTime };
        state.Dependency = job.ScheduleParallel(_movableQuery, state.Dependency);
    }
}
```

三个核心过滤条件：

| 方法 | 含义 |
|------|------|
| `.WithAll<T>()` | Entity 必须拥有 T（且 T 处于 enabled 状态，若 T 是 Enableable Component） |
| `.WithAny<T, U>()` | Entity 至少拥有 T 或 U 其中之一 |
| `.WithNone<T>()` | Entity 不能拥有 T（或 T 处于 disabled 状态） |
| `.WithPresent<T>()` | Entity 拥有 T 即可，不关心 enabled/disabled 状态 |

`.WithPresent<T>()` 是专门为 Enableable Component 设计的——它让 Query 能"看到"所有拥有 T 的 Entity，无论 T 当前是否被启用。后面 EnabledMask 一节会详细说明它和 `.WithAll<T>()` 的区别。

## 2. Query 缓存机制

**永远在 `OnCreate` 里构建 Query，不要在 `OnUpdate` 里构建。**

原因在于 Query 的构建过程并不廉价：它需要遍历 World 中所有现有的 Archetype，找出匹配的 Chunk 列表，并注册到 EntityManager 的内部索引。每帧重复这个过程是纯粹的浪费。

缓存的 Query 只在以下情况更新内部 Chunk 列表：
- 有新的 Archetype 被创建（新的 Component 组合出现）
- 有 Archetype 被销毁

这个增量更新由 `EntityManager` 自动维护，System 侧无需感知。

**性能数量级对比（参考值，10,000 个 Entity 规模）：**

| 操作 | 耗时（主线程） |
|------|----------------|
| 每帧重新构建 Query | ~0.3–0.8 ms |
| 使用缓存 Query 迭代 | ~0.01 ms（仅迭代开销） |

差距在大型项目中会放大，因为 Archetype 数量越多，构建开销越高。

一个额外的好处：在 `OnCreate` 里构建 Query 时可以调用 `state.RequireForUpdate(_movableQuery)`，当 Query 匹配零个 Entity 时整个 System 的 `OnUpdate` 会被自动跳过，连调度开销都省掉了。

## 3. 变更检测

ECS 的变更检测基于**版本号（ChangeVersion）**，而非 dirty flag。每个 Chunk 对每个 ComponentType 都维护一个 `uint` 版本号，每次有写访问发生时，版本号递增。System 同样记录上次运行时的 `LastSystemVersion`。

`.WithChangeFilter<T>()` 让 Query 在迭代时跳过那些"自上次 System 运行以来 T 没有发生写访问"的 Chunk：

```csharp
public partial struct DirtyPositionSystem : ISystem
{
    private EntityQuery _changedQuery;

    public void OnCreate(ref SystemState state)
    {
        _changedQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
            .WithAll<LocalTransform, BoundingBox>()
            .WithChangeFilter<LocalTransform>()   // 只处理 Position 有变化的 Chunk
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        // 只有上一帧（或更早）有 LocalTransform 写访问的 Chunk 会进入 Job
        var job = new RebuildBoundsJob();
        state.Dependency = job.ScheduleParallel(_changedQuery, state.Dependency);
    }
}
```

**关键细节：变更检测是 Chunk 粒度，不是 Entity 粒度。**

同一 Chunk 内只要有任意一个 Entity 的 `LocalTransform` 被写访问，整个 Chunk 就会通过 filter。这意味着：

- 如果你的 System 对某个 ComponentType 做了读写（`ref` 访问），整个 Chunk 的版本号都会被更新，哪怕实际数值没变
- 要避免"假写"：如果你只是读取数据，确保使用 `in` 而不是 `ref`

```csharp
// 错误：使用 ref 会标记整个 Chunk 为已变更，破坏下游的变更检测
foreach (var (transform, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>())
{
    // 只是读取 transform，但 RefRW 已经触发了写标记
    var pos = transform.ValueRO.Position;
}

// 正确：只读就用 RefRO
foreach (var (transform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<MoveSpeed>>())
{
    var pos = transform.ValueRO.Position;
}
```

## 4. EnabledMask

Enableable Component（实现 `IEnableableComponent` 的 Component）不改变 Archetype，只在 Chunk 内用一个 64-bit mask 记录每个 Entity 的启用状态。

这对 Query 行为的影响：

```csharp
// 情况 A：WithAll<Shield>
// 只迭代 Shield 处于 enabled 状态的 Entity
// Query 内部会按 EnabledMask 跳过 disabled 的 Entity

// 情况 B：WithNone<Shield>
// 只迭代 Shield 处于 disabled 状态的 Entity（或根本没有 Shield 的 Entity）

// 情况 C：WithPresent<Shield>
// 迭代所有拥有 Shield 组件的 Entity，不管 enabled/disabled
// 适合在 Job 里手动读取 IsEnabled 状态并统一处理
```

实际代码示例：

```csharp
[BurstCompile]
public partial struct ShieldRechargeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 只处理 Shield 当前 disabled（耗尽）的 Entity，等待充能
        foreach (var (recharge, entity) in
            SystemAPI.Query<RefRW<ShieldRecharge>>()
                     .WithDisabled<Shield>()
                     .WithEntityAccess())
        {
            recharge.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if (recharge.ValueRO.Timer <= 0f)
            {
                // 重新 enable Shield
                SystemAPI.SetComponentEnabled<Shield>(entity, true);
            }
        }
    }
}
```

**EnabledMask 的代价**：相比纯 Archetype 过滤（完全不进入 Chunk），EnabledMask 迭代仍然需要加载 Chunk 数据，只是在 Entity 级别跳过。当 disabled 比例很高时，这个 overhead 可以接受；当需要完全排除大批 Entity 时，考虑用真正的 Archetype 分离（添加/移除 Tag Component）。

## 5. SharedComponent 过滤

`SharedComponent` 的值存储在 Chunk 级别——同一个 Chunk 内所有 Entity 共享同一个 SharedComponent 值。这使得按 SharedComponent 值过滤 Chunk 的开销极低。

```csharp
public partial struct TeamRenderSystem : ISystem
{
    private EntityQuery _unitQuery;

    public void OnCreate(ref SystemState state)
    {
        _unitQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
            .WithAll<LocalTransform, UnitRenderer>()
            .WithSharedComponentFilter(new TeamID())  // 声明会用 SharedComponent 过滤
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        // 运行时设置具体的过滤值，只处理 Team 0 的单位
        _unitQuery.SetSharedComponentFilter(new TeamID { Value = 0 });

        var chunks = _unitQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator);
        // ... 处理 chunks

        // 重置过滤，避免影响其他使用同一 Query 的地方
        _unitQuery.ResetFilter();
    }
}
```

适用场景：LOD 级别（按距离分组）、团队 ID、材质批次。注意 `SetSharedComponentFilter` 是运行时状态，不影响 Archetype 结构，也不线程安全——只能在主线程调用。

## 6. EntityQueryMask：快速归属判断

`EntityQueryMask` 不用于迭代，而是用于**快速判断某个 Entity 是否匹配某 Query**，时间复杂度 O(1)：

```csharp
public partial struct AOISystem : ISystem
{
    private EntityQueryMask _interactableMask;

    public void OnCreate(ref SystemState state)
    {
        var interactableQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
            .WithAll<Interactable, LocalTransform>()
            .WithNone<Disabled>()
            .Build(ref state);

        // 从 Query 生成 Mask，后续不需要保留 Query 本身
        _interactableMask = state.EntityManager.GetEntityQueryMask(interactableQuery);
    }

    // 在 AOI 检测时，判断某 Entity 是否可交互，无需遍历
    public bool IsInteractable(Entity entity) =>
        _interactableMask.MatchesIgnoreFilter(entity);
}
```

`EntityQueryMask` 实际上是三个 `ulong` 的位掩码，比较时只做位运算，适合高频调用（每帧对大量 Entity 做归属判断的 AOI、兴趣管理系统）。

## 7. 常见误用

**1. 在 Job 内构建 Query**

`EntityQuery` 的构建依赖 `EntityManager`，后者不能在 Job 线程访问。所有 Query 构建必须在主线程完成（`OnCreate` 或 `OnUpdate` 的主线程部分）。

**2. 手动 Dispose 缓存的 Query**

在 `OnCreate` 中通过 `state.RequireForUpdate` 或直接 `Build` 注册的 Query，其生命周期由 `SystemState` 管理，World 销毁时自动清理。手动调用 `Dispose` 会导致 double-free。只有通过 `EntityManager.CreateEntityQuery` 直接创建的 Query 才需要手动 Dispose。

**3. WithNone 和 WithDisabled 的区别**

```csharp
// WithNone<Shield>：Entity 根本没有 Shield 组件（不在此 Archetype）
//                  OR Entity 有 Shield 但它是 Enableable 且当前 disabled
.WithNone<Shield>()

// WithDisabled<Shield>：专指 Entity 拥有 Shield 且当前处于 disabled 状态
//                       Shield 必须实现 IEnableableComponent
.WithDisabled<Shield>()
```

混用这两个概念会导致逻辑 bug，尤其在同时有"没有 Shield 的 Entity"和"有 Shield 但禁用的 Entity"时，两者行为截然不同。

**4. 忘记 ResetFilter**

调用 `SetSharedComponentFilter` 之后如果不 `ResetFilter`，Query 会保留过滤状态，影响下次使用（包括其他 System 如果持有同一 Query 引用的情况）。养成在同一帧内成对调用 `Set` 和 `Reset` 的习惯。

## 小结

| 机制 | 粒度 | 适用场景 |
|------|------|----------|
| Archetype 过滤（WithAll/None/Any） | Archetype | 静态结构分组，零开销 |
| 变更检测（WithChangeFilter） | Chunk | 响应式处理，避免无效计算 |
| EnabledMask（WithAll on Enableable） | Entity | 动态开关，不改结构 |
| SharedComponent 过滤 | Chunk | 运行时分组，LOD/Team |
| EntityQueryMask | Entity | O(1) 归属判断 |

EntityQuery 的设计哲学是把"我要谁"的描述与"怎么处理"的逻辑分离。描述越精确，引擎能跳过的无关数据就越多，这正是 ECS 数据局部性优势的来源。

---

下一篇 **DOTS-E05「ComponentLookup 与随机访问」** 将讨论当你需要在 Job 里访问"不在当前迭代 Entity 上的 Component"时该怎么做——`ComponentLookup` 的缓存、线程安全约束，以及如何避免它成为性能瓶颈。
