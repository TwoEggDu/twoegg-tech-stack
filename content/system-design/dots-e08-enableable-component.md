---
title: "Unity DOTS E08｜Enableable Component：不改 Archetype 的开关方案及其代价"
slug: "dots-e08-enableable-component"
date: "2026-03-28"
description: "Enableable Component 让你不触发 Structural Change 就能开关 Entity 的某个 Component，代价是 Query 迭代时的 bit mask 检测。本篇讲清楚 EnabledMask 的工作原理、和 Add/Remove Component 的对比，以及什么场景该用哪种方式。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "Enableable Component"
  - "EnabledMask"
  - "Structural Change"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 8
weight: 1880
---

## 问题背景：热路径里的 Structural Change

E01 里讲过，Add/Remove Component 是典型的 Structural Change。每次操作都会把 Entity 从一个 Chunk 搬到另一个 Chunk：旧 Chunk 的数据要被挖走，新 Chunk 要分配空间并写入。单次操作代价不高，但在热路径里——比如每帧根据 AI 状态给上千个 Entity 加减 `FrozenTag`——持续的数据搬移会让 Chunk 碎片化，也会让 Chunk 缓存频繁失效。

这类"频繁开关"的场景真正需要的不是改变数据布局，而是一个轻量的"这个 Component 此刻是否有效"的标记。Enableable Component 就是为此而生。

---

## Enableable Component 的机制

### 接口定义

要让一个 Component 具备 enable/disable 能力，只需同时实现 `IComponentData` 和 `IEnableableComponent`：

```csharp
using Unity.Entities;

public struct IsActive : IComponentData, IEnableableComponent { }

public struct IsFrozen : IComponentData, IEnableableComponent
{
    public float FreezeTimer;
}
```

`IEnableableComponent` 是一个空接口，仅作为标记。Component 本身的数据字段照常定义，enabled 状态完全独立存储。

### 每个 Chunk 的 EnabledMask

Entities 包为每种 Enableable Component 类型在 Chunk 中维护一个 **64-bit mask**（`v128` 实际是 128-bit，支持最多 128 个 Entity/Chunk，按需扩展）。每个 bit 对应 Chunk 内的一个 Entity slot。

```
Chunk (容量 = 64 个 Entity)
┌─────────────────────────────────────────────────────────────────┐
│  Entity[0]  Entity[1]  ...  Entity[62]  Entity[63]             │
│  Pos  Vel   Pos  Vel        Pos  Vel    Pos  Vel   ...  (数据) │
├─────────────────────────────────────────────────────────────────┤
│  IsActive EnabledMask:                                          │
│  bit63 ... bit1  bit0                                           │
│  1       ... 0    1    ← bit=1 表示 enabled，bit=0 表示 disabled│
├─────────────────────────────────────────────────────────────────┤
│  IsFrozen EnabledMask:                                          │
│  bit63 ... bit1  bit0                                           │
│  0       ... 1    0                                             │
└─────────────────────────────────────────────────────────────────┘
```

Enable/Disable 操作只做一次位写入，Entity 的位置和数据完全不动。这正是"不触发 Structural Change"的含义——Archetype 没有变，Chunk 归属没有变，只有 mask 里的一个 bit 翻转了。

---

## Query 中的 EnabledMask 行为

Enableable Component 对 Query 语义有明确的影响，需要区分三种写法：

| Query 写法 | 匹配条件 |
|---|---|
| `WithAll<T>()` | Archetype 含 T，且 T 当前为 **enabled** |
| `WithDisabled<T>()` | Archetype 含 T，且 T 当前为 **disabled** |
| `WithPresent<T>()` | Archetype 含 T，**不论** enabled 状态 |

```csharp
// 只处理 IsActive 为 enabled 的 Entity（最常见写法）
EntityQuery activeQuery = SystemAPI.QueryBuilder()
    .WithAll<IsActive, LocalTransform>()
    .Build();

// 处理 IsActive 为 disabled 的 Entity（例如唤醒逻辑）
EntityQuery dormantQuery = SystemAPI.QueryBuilder()
    .WithDisabled<IsActive>()
    .WithAll<LocalTransform>()
    .Build();

// 不论状态，统一遍历（例如统计或重置）
EntityQuery allQuery = SystemAPI.QueryBuilder()
    .WithPresent<IsActive>()
    .WithAll<LocalTransform>()
    .Build();
```

**迭代代价**：Query 在遍历 Chunk 时，若 Chunk 内所有 Entity 都是 enabled，ECS 运行时会检测到"全 1"mask 并跳过 bit 检测，按完整 Chunk 处理；只有存在混合状态时才进行逐 bit 筛选。因此实际开销取决于状态分布，但始终略高于纯 Archetype 过滤（后者在 Query 构建阶段就完成了 Chunk 筛选，迭代时无需额外检测）。

---

## 代码示例

### 在主线程上 Enable / Disable

```csharp
using Unity.Entities;

public partial class FreezeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // 直接操作：SetComponentEnabled 不触发 Structural Change
        foreach (var (frozen, entity) in
            SystemAPI.Query<RefRO<IsFrozen>>()
                     .WithEntityAccess()
                     .WithDisabled<IsFrozen>())
        {
            // 这里只是演示访问 disabled 状态的写法
            _ = frozen;
            _ = entity;
        }

        // 在 Job 外直接操作
        // EntityManager.SetComponentEnabled<IsFrozen>(entity, true);

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
```

### 在 IJobEntity 中通过 EnabledRefRW 读写

`EnabledRefRW<T>` 是在 Job 内读写 enabled 状态的专用句柄，类似 `RefRW<T>` 对数据的作用：

```csharp
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct TickFreezeJob : IJobEntity
{
    public float DeltaTime;

    // 使用 EnabledRefRW 访问 enabled 状态
    // 使用 RefRW 访问 Component 数据
    void Execute(
        EnabledRefRW<IsFrozen> frozenEnabled,
        RefRW<IsFrozen> frozen)
    {
        frozen.ValueRW.FreezeTimer -= DeltaTime;

        if (frozen.ValueRO.FreezeTimer <= 0f)
        {
            // 计时结束：禁用 IsFrozen，不产生 Structural Change
            frozenEnabled.ValueRW = false;
        }
    }
}

[BurstCompile]
public partial struct TickFreezeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        new TickFreezeJob { DeltaTime = SystemAPI.Time.DeltaTime }.Schedule();
    }
}
```

### 批量开关与 EntityCommandBuffer

ECB 同样支持 `SetComponentEnabled`，适合跨系统的延迟操作：

```csharp
var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                   .CreateCommandBuffer(state.WorldUnmanaged);

foreach (var (hp, entity) in
    SystemAPI.Query<RefRO<HealthPoint>>().WithEntityAccess())
{
    if (hp.ValueRO.Value <= 0)
    {
        // 禁用 IsActive，无需 Add/Remove，无 Structural Change
        ecb.SetComponentEnabled<IsActive>(entity, false);
    }
}
```

---

## Enable/Disable vs Add/Remove 对比

| 维度 | Enable / Disable | Add / Remove |
|---|---|---|
| 是否触发 Structural Change | 否 | 是 |
| 内存操作 | 翻转 1 个 bit | 搬移 Entity 全部 Component 数据 |
| Archetype 变化 | 无 | 有（影响 Chunk 归属） |
| Query 过滤代价 | 迭代时 bit mask 检测 | 构建阶段完成，迭代无额外代价 |
| 线程安全（Job 内） | 通过 `EnabledRefRW` 直接写 | 必须通过 ECB 延迟执行 |
| 适合频率 | 高频开关（每帧 N 次） | 低频或一次性变化 |
| Component 数据保留 | 保留（disable 后数据仍在） | 不保留（Remove 后数据丢失） |

---

## Enableable Component vs Tag Component

Tag Component 是 zero-size 的 `IComponentData`，常见用法是通过 Add/Remove 来标记状态：

```csharp
public struct ActiveTag : IComponentData { }   // Tag，无数据
public struct IsActive : IComponentData, IEnableableComponent { }  // Enableable，无数据
```

两者在"标记状态"这件事上功能相同，但机制相反：

| 维度 | Tag + Add/Remove | Enableable Component |
|---|---|---|
| 切换代价 | Structural Change（数据搬移） | 1 个 bit 写入 |
| Query 过滤代价 | 零（纯 Archetype 过滤） | bit mask 检测 |
| 最佳场景 | 切换极少、查询极多 | 切换频繁、查询可接受轻微开销 |

**决策原则**：如果状态切换比查询更频繁，选 Enableable Component；如果状态几乎不变但被大量 Query 命中，选 Tag + Add/Remove。

典型案例对比：
- **冻结/解冻**（战斗中每帧可能发生）：Enableable Component 更合适，避免持续 Structural Change。
- **死亡标记**（一次性，Entity 死后不再复活）：Tag + Add/Remove 更合适，死亡后 Query 直接走 Archetype 过滤，零运行时开销。

---

## 适合使用 Enableable Component 的场景

**适合：**
- **冻结 / 眩晕状态**：战斗系统每帧对大量 Entity 施加或解除，切换极其频繁。
- **无敌帧**：受击后短暂无敌，以帧为单位开关，频率高。
- **AI 激活 / 休眠**：视野范围外的 AI 暂停更新，玩家靠近时重新激活，每帧可能批量切换。
- **技能 Buff 叠加层**：多个 Buff Component 独立 enable/disable，不改变 Archetype。

**不适合（改用 Tag + Add/Remove）：**
- 状态只在关卡加载时设置一次，之后不再变化。
- 状态切换极少（每分钟个位数次），但 Query 每帧运行在数万 Entity 上。
- 需要 Chunk 按状态完全分离，以便 SIMD 批量处理无分支执行。

---

## 小结

Enableable Component 填补了 ECS 里"快速开关"的空白。它不是 Add/Remove 的替代品，而是专为高频状态切换设计的补充方案：用 bit mask 的轻微查询开销换掉了 Structural Change 的数据搬移代价。理解这个权衡，就能在设计 Component 时做出正确的选择。

至此，ECS 核心六篇（E03 Archetype 与 Chunk、E04 SystemBase 与 ISystem、E05 IJobEntity 与 ScheduleParallel、E06 EntityCommandBuffer、E07 EntityQuery 进阶、E08 Enableable Component）已全部覆盖。下一篇 **E09** 将进入 **Baking 系统**：讲清楚 GameObject 是如何在编辑器 / 构建时被转换为 Entity 数据的，以及 Baker、BlobAsset 和 Baking World 各自扮演的角色。
