---
title: "Unreal Mass 03｜Mass Structural Change：FMassCommandBuffer、Deferred Add/Remove 与 DOTS ECB 对比"
slug: "mass-03-structural-change"
date: "2026-03-28"
description: "结构变更（Add/Remove Fragment、销毁 Entity）在 Mass 和 DOTS 里都是贵操作，两者用相似的延迟提交机制解决。本篇对比 Mass 的 FMassCommandBuffer 和 DOTS 的 EntityCommandBuffer，讲清楚各自的 Flush 时机和常见踩坑。"
tags:
  - "Unreal Engine"
  - "Mass"
  - "FMassCommandBuffer"
  - "Structural Change"
  - "ECS"
  - "数据导向"
series: "Unreal Mass 深度"
primary_series: "unreal-mass"
series_role: "article"
series_order: 3
weight: 2030
---

## 为什么结构变更需要延迟

在 ECS 里，"结构变更"是一类特殊操作：给 Entity 添加或移除 Fragment、销毁 Entity。这类操作之所以"贵"，根本原因在于 Archetype/Chunk 布局的连续性。

Mass 和 DOTS 的内存组织方式高度相似：相同 Fragment 组合的 Entity 被紧密排列在同一 Chunk 里。Processor 执行期间，遍历逻辑直接对这段连续内存进行读写——如果此时有人在另一个线程（或同一线程的深层调用）把某个 Entity 从一个 Archetype 搬移到另一个 Archetype，正在遍历的指针就会悬空，轻则数据错乱，重则直接崩溃。

DOTS 的解法是 **EntityCommandBuffer（ECB）**：Processor 只把"我想做什么"写入一个命令队列，等到安全窗口再统一回放。Mass 采用完全相同的思路，对应类型是 **FMassCommandBuffer**。两者解决的是完全相同的问题，理解一个对理解另一个帮助极大。

---

## FMassCommandBuffer 基本用法

### 获取 CommandBuffer

在 Processor 的 `Execute` 里，通过 `Context.Defer()` 拿到当前帧可用的 CommandBuffer 引用：

```cpp
void UMyDestroyProcessor::Execute(FMassEntityManager& EntityManager,
                                   FMassExecutionContext& Context)
{
    FMassCommandBuffer& CmdBuf = Context.Defer();

    Context.ForEachEntityChunk(EntityQuery, [&](FMassExecutionContext& ChunkContext)
    {
        const TArrayView<FMyDeadFragment> DeadFragments =
            ChunkContext.GetMutableFragmentView<FMyDeadFragment>();

        const TConstArrayView<FMassEntityHandle> Entities =
            ChunkContext.GetEntities();

        for (int32 i = 0; i < ChunkContext.GetNumEntities(); ++i)
        {
            if (DeadFragments[i].bShouldDie)
            {
                // 不能在这里直接销毁，压入命令队列
                CmdBuf.PushCommand<FMassCommandDestroyEntity>(Entities[i]);
            }
        }
    });
}
```

### 添加 / 移除 Fragment

```cpp
// 给 Entity 添加一个新 Fragment（Entity 会被移到新 Archetype）
CmdBuf.PushCommand<FMassCommandAddFragment<FMyStatusFragment>>(EntityHandle);

// 移除某个 Fragment
CmdBuf.PushCommand<FMassCommandRemoveFragment<FMyStatusFragment>>(EntityHandle);

// 同时添加多个 Fragment（减少 Flush 时的 Archetype 迁移次数）
CmdBuf.PushCommand<FMassCommandAddFragmentsList>(
    EntityHandle,
    TArray<const UScriptStruct*>{ FFragmentA::StaticStruct(), FFragmentB::StaticStruct() }
);
```

**注意**：`PushCommand` 是线程安全的。Mass 的 CommandBuffer 内部使用锁或无锁结构保护并发写入，你不需要自己加锁，这一点比 DOTS 的 `ParallelWriter` 更省心。

---

## Flush 时机

### Mass 的自动 Flush

Mass 的 CommandBuffer 与 **Phase** 绑定。每个 Phase（PrePhysics、Physics、PostPhysics、FrameEnd 等）执行完毕后，引擎会自动对该 Phase 积累的所有命令做一次 Flush，把结构变更统一落地。

```
Phase N 开始
  → Processor A Execute（压命令）
  → Processor B Execute（压命令）
  → Processor C Execute（压命令）
Phase N 结束 → 自动 Flush CommandBuffer
Phase N+1 开始（结构变更已生效）
```

这套机制对大多数使用场景非常友好：你只管压命令，框架负责在合适的时机回放，不需要手动管理生命周期。

### DOTS ECB 的手动 Flush

DOTS 的 ECB 由 `EntityCommandBufferSystem` 持有，System 会在特定时机（通常是某个 Group 的末尾）调用 `Playback` 并 `Dispose`：

```csharp
// DOTS C# 示例：从 ECBSystem 获取 ECB
var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                   .CreateCommandBuffer(state.WorldUnmanaged);

ecb.AddComponent<MyComponent>(entity);
// 不需要手动 Playback，EndSimulationEntityCommandBufferSystem 会处理
```

DOTS 也支持完全手动控制播放时机：

```csharp
var ecb = new EntityCommandBuffer(Allocator.TempJob);
// ... 填充命令 ...
ecb.Playback(EntityManager);
ecb.Dispose(); // 必须手动 Dispose，否则内存泄漏
```

---

## DOTS ECB vs Mass FMassCommandBuffer 对比表

| 维度 | DOTS ECB | Mass FMassCommandBuffer |
|------|----------|------------------------|
| 获取方式 | 从 ECBSystem 获取，或手动 `new` | `Context.Defer()` |
| Flush / Playback 时机 | 手动指定 ECBSystem，或显式 `Playback` | 每个 Phase 结束自动 Flush |
| 并发写入 | 需要 `CreateParallelWriter()` + chunkIndex | 内置线程安全，直接使用 |
| 手动 Dispose | 手动 `new` 时必须手动 `Dispose` | 不需要，框架管理生命周期 |
| 控制粒度 | 可精细控制播放顺序和时机 | 固定在 Phase 边界，灵活性较低 |
| 语言 | C# | C++ |

总结：Mass 的方案更"傻瓜"，适合快速迭代；DOTS 的方案更灵活，适合需要精细控制回放顺序的复杂场景。

---

## 直接结构变更（主线程）

如果你的逻辑发生在 Processor 外部（GameThread 上，没有并发遍历），可以绕过 CommandBuffer，直接调用 `FMassEntityManager` 的结构变更方法：

```cpp
// 适合：关卡初始化、低频事件响应（例如玩家技能触发）

// 获取 EntityManager（通常从 Subsystem 拿）
FMassEntityManager& EntityManager =
    UWorld::GetSubsystem<UMassEntitySubsystem>(World)->GetMutableEntityManager();

// 直接添加 Fragment（同步完成，立即生效）
EntityManager.AddFragmentToEntity(EntityHandle, FMyStatusFragment::StaticStruct());

// 直接移除 Fragment
EntityManager.RemoveFragmentFromEntity(EntityHandle, FMyStatusFragment::StaticStruct());

// 直接销毁 Entity
EntityManager.DestroyEntity(EntityHandle);
```

**何时用直接调用，何时用 CommandBuffer：**

- **初始化阶段**（`BeginPlay`、关卡加载）：直接调用，简单直接。
- **低频事件**（UI 按钮、网络消息回调）：如果能保证在 GameThread 且没有 Processor 正在运行，直接调用也可以。
- **Processor Execute 内部**：永远用 CommandBuffer，没有例外。

---

## 常见踩坑

### 踩坑一：在 Processor 内直接调用 EntityManager 结构变更

```cpp
// 错误示例 —— 会崩溃或产生未定义行为
void UBadProcessor::Execute(FMassEntityManager& EntityManager,
                             FMassExecutionContext& Context)
{
    Context.ForEachEntityChunk(EntityQuery, [&](FMassExecutionContext& ChunkContext)
    {
        for (FMassEntityHandle Entity : ChunkContext.GetEntities())
        {
            // 直接修改结构 —— 正在遍历的 Chunk 会被破坏！
            EntityManager.RemoveFragmentFromEntity(Entity, FMyFragment::StaticStruct());
        }
    });
}
```

Mass 在 Development 构建下会有断言检测这类错误，但 Shipping 构建不保证安全。始终使用 `Context.Defer()`。

### 踩坑二：以为 Flush 在 Phase 内立即生效

```cpp
void UMyProcessor::Execute(FMassEntityManager& EntityManager,
                            FMassExecutionContext& Context)
{
    FMassCommandBuffer& CmdBuf = Context.Defer();

    Context.ForEachEntityChunk(EntityQuery, [&](FMassExecutionContext& ChunkContext)
    {
        for (FMassEntityHandle Entity : ChunkContext.GetEntities())
        {
            CmdBuf.PushCommand<FMassCommandAddFragment<FNewFragment>>(Entity);

            // 错误：以为上面的命令已经生效，试图立即读取新 Fragment
            // 实际上 Flush 要等到 Phase 结束，这里拿到的是空指针或崩溃
            FNewFragment* NewFrag = EntityManager.GetFragmentDataPtr<FNewFragment>(Entity);
        }
    });
}
```

**正确做法**：如果下一个 Processor 需要读取新 Fragment，确保它运行在同一 Phase 内 CommandBuffer Flush 之后，或者放到下一个 Phase。可以通过 `ExecutionOrder` 和 `ExecutionFlags` 控制 Processor 的依赖顺序。

### 踩坑三：DOTS 用户忘记 Dispose 手动创建的 ECB

这是 DOTS 特有的坑，Mass 用户不会遇到。但如果你的项目同时使用两套框架，记住：手动 `new EntityCommandBuffer` 必须配对 `Dispose`，否则 Native 内存泄漏，不会被 GC 回收。

---

## 小结

结构变更是 ECS 里成本最高的操作之一，Mass 和 DOTS 用几乎相同的延迟提交思路解决这个问题。核心规则只有一条：**Processor Execute 内部永远用 CommandBuffer，主线程低频操作才用直接调用。**

Mass 的 `FMassCommandBuffer` 比 DOTS ECB 更易用（内置线程安全、自动 Flush、无需 Dispose），代价是失去了手动控制播放时机的灵活性。对于绝大多数游戏逻辑，这个折中是合理的。

下一篇 [Mass-04｜Mass LOD：用 FMassLODFragment 驱动大规模 AI 降频](../mass-04-lod) 将介绍 Mass 内置的 LOD 系统——如何用 Fragment 驱动数千个 AI Agent 的更新频率，在不感知的情况下把 CPU 开销降低一个数量级。
