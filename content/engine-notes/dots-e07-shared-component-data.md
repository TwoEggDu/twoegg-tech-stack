---
title: "Unity DOTS E07｜ISharedComponentData：分组的代价与用途，Chunk 碎片化的风险"
slug: "dots-e07-shared-component-data"
date: "2026-03-28"
description: "SharedComponent 的值决定 Entity 所在的 Chunk，这让它天然适合「按 LOD 级别、材质、团队分组」——但每个不同的值都会分裂出新的 Chunk，容易造成内存碎片化。本篇讲清楚 SharedComponent 的内存影响和适用边界。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "ISharedComponentData"
  - "Chunk"
  - "碎片化"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 7
weight: 1870
---

普通 `IComponentData` 的值只影响数据本身，不影响 Entity 的位置。而 `ISharedComponentData` 完全不同——它的值直接决定 Entity 住在哪个 Chunk 里。这一设计带来了强大的分组能力，同时也埋下了碎片化的隐患。

## SharedComponent 的内存语义

ECS 中，Archetype 由 Component 类型集合决定。通常情况下，拥有相同 Archetype 的 Entity 都住在同一批 Chunk 中（按顺序填充）。

加入 `ISharedComponentData` 之后，规则变了：**相同 Archetype + 相同 SharedComponent 值的 Entity，才住在同一 Chunk。** 不同的值会把 Entity 物理上隔离到不同的 Chunk，即便其他所有 Component 类型完全一致。

以 LOD 级别为例，假设场景中有 300 个单位，全部拥有相同 Archetype，但 SharedComponent `LodLevel` 的值分别为 0、1、2：

```
Archetype: [Position, Velocity, Health, LodLevel(shared)]

LodLevel = 0            LodLevel = 1            LodLevel = 2
┌──────────────┐        ┌──────────────┐        ┌──────────────┐
│ Chunk A      │        │ Chunk C      │        │ Chunk E      │
│ 128 entities │        │ 128 entities │        │  44 entities │
├──────────────┤        ├──────────────┤        └──────────────┘
│ Chunk B      │        │ Chunk D      │
│  72 entities │        │  28 entities │
└──────────────┘        └──────────────┘

  200 entities            156 entities            44 entities
  (Chunk A + B)           (Chunk C + D)           (Chunk E)
```

三个不同的 `LodLevel` 值，形成三个独立的 Chunk 组。Query 过滤 `LodLevel == 0` 时，系统只需要遍历 Chunk A 和 Chunk B，完全跳过其他 Chunk——这是 SharedComponent 最大的优势。

## 为什么改变 SharedComponent 值有代价

修改 `ISharedComponentData` 的值，本质上是一次 **Structural Change**。具体发生的事：

1. Entity 从旧值对应的 Chunk 中移除（数据被复制走）
2. Entity 被插入新值对应的 Chunk（数据写入新位置）
3. 旧 Chunk 中该 Entity 后面的所有 Entity 向前移动填补空缺

这和 `AddComponent` / `RemoveComponent` 是同等级别的操作，代价不低。如果在热路径（如每帧对大量 Entity 执行）里频繁改变 SharedComponent 值，会产生大量 Chunk 迁移，性能会急剧下降。

**错误示范：** 把"当前速度等级"做成 SharedComponent，每帧根据速度大小更新它。这等同于每帧触发大量 Structural Change，比直接用普通 Component 慢几个数量级。

## Managed vs Unmanaged SharedComponent

`ISharedComponentData` 有两种形态，选择哪种取决于是否需要在 Burst Job 中访问。

### Managed SharedComponent

结构体内可以包含 managed 引用（如 `Material`、`Mesh`）。Entities.Graphics 大量使用这种形式来按材质和网格分组，确保渲染调用可以合批。

```csharp
// Managed SharedComponent，可以持有 class 引用
public struct RenderMeshShared : ISharedComponentData, IEquatable<RenderMeshShared>
{
    public Material Material;
    public Mesh Mesh;

    public bool Equals(RenderMeshShared other)
        => Material == other.Material && Mesh == other.Mesh;

    public override int GetHashCode()
        => HashCode.Combine(Material, Mesh);
}
```

值存在 Managed heap，Unity 会对所有出现过的不同值做引用计数管理。`Equals` 和 `GetHashCode` 必须正确实现，否则会被误判为不同值，产生额外的 Chunk 分裂。

### Unmanaged SharedComponent

Entities 1.x 支持不含 managed 字段的结构体作为 SharedComponent，称为 unmanaged shared component。**只有 unmanaged 版本可以在 Burst Job 中访问。**

```csharp
// Unmanaged SharedComponent，可在 Burst Job 中访问
public struct LodLevel : ISharedComponentData
{
    public int Value; // 0 / 1 / 2
}
```

结构体不含任何 class 引用，编译器自动满足 unmanaged 约束。在 `IJobChunk` 中，可以通过 `chunk.GetSharedComponent<LodLevel>(handle)` 读取当前 Chunk 所属的值。

## Chunk 碎片化问题

SharedComponent 的核心风险在于：**每个不同的值都会产生独立的 Chunk 组**。

设想一个极端情况：游戏内有 5000 个单位，给每个单位一个 `TeamId` SharedComponent，共有 200 支队伍。平均每队 25 人。

```
200 个不同的 TeamId 值
每队平均 25 Entity
每个 Chunk 上限 128 Entity

实际分布：200 个 Chunk，每个 Chunk 只有 ~25 个 Entity
Chunk 利用率：25 / 128 ≈ 19.5%
```

Query 遍历所有 200 个 Chunk 时，每个 Chunk 只处理约 25 条数据，但每次都要经历 Chunk 头部解析、调度开销等固定成本。相比填满的 Chunk（128 Entity），碎片化 Chunk 的单位处理成本高出数倍。

碎片化的具体代价：

- Query 需要遍历更多 Chunk 头部，调度 IJobChunk 的批次数成倍增加
- Cache 利用率降低：每个 Chunk 的有效数据量少，CPU 预取效益下降
- 内存占用增加：每个 Chunk 都有固定的元数据开销，空间利用率低

## 适合用 SharedComponent 的场景

SharedComponent 最适合**基数低（不同值的数量少）且变化不频繁**的分组维度：

| 场景 | 不同值数量 | 适合度 |
|------|-----------|--------|
| LOD 级别 (0/1/2/3) | 4 | 极佳 |
| 渲染材质/Mesh 组合 | 数十到数百 | 良好（Entities.Graphics 标准用法） |
| 阵营/Team ID | < 50 | 良好 |
| 地图区域 Zone | < 20 | 良好 |
| 敌人类型枚举 | < 30 | 良好 |

**黄金原则：** 不同值的数量越少，每组 Entity 数量越均匀，SharedComponent 的收益越高。

## 不适合的场景与替代方案

以下情况应当避免使用 SharedComponent：

**连续变化的数值**（速度等级、HP 区间）每帧触发 Structural Change，热路径中会产生严重性能问题。替代方案：用普通 `IComponentData` 存储，在 System 中用条件判断处理不同情况。

**大量唯一值**（Entity 运行时 ID、精确坐标）极端情况下每个 Entity 独占一个 Chunk，碎片化达到最坏情况。替代方案：用普通 Component 存储唯一标识。

**需要频繁 Enable/Disable 的状态标记**（是否存活、是否可见）每次状态变化都是 Structural Change。替代方案：用 Enableable Component（`IEnableableComponent`），可以在不触发 Structural Change 的情况下切换，详见下一篇。

## 代码示例

### 定义 LOD Level SharedComponent

```csharp
using Unity.Entities;

// Unmanaged SharedComponent，支持 Burst Job 访问
public struct LodLevel : ISharedComponentData
{
    public int Value; // 0 = 高精度, 1 = 中等, 2 = 低精度
}
```

### 在 System 中按 LOD Level 过滤 Query

```csharp
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public partial struct LodUpdateSystem : ISystem
{
    private EntityQuery _lodQuery;

    public void OnCreate(ref SystemState state)
    {
        // 构建 Query，按 SharedComponent 值过滤
        _lodQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
            .WithAll<LodLevel>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 只处理 LodLevel.Value == 0 的高精度 Entity
        _lodQuery.SetSharedComponentFilter(new LodLevel { Value = 0 });
        // ... 调度高精度更新 Job

        // 切换过滤器，处理 LodLevel.Value == 1 的中等精度 Entity
        _lodQuery.SetSharedComponentFilter(new LodLevel { Value = 1 });
        // ... 调度中等精度更新 Job

        // 重置过滤器，避免影响下次查询
        _lodQuery.ResetFilter();
    }
}
```

### 批量设置 SharedComponent 值

```csharp
using Unity.Entities;
using Unity.Collections;

public partial class LodAssignSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 获取所有需要重新分配 LOD 的 Entity
        var entities = _reassignQuery.ToEntityArray(Allocator.Temp);

        // EntityManager 操作需要在主线程执行（Structural Change）
        for (int i = 0; i < entities.Length; i++)
        {
            int newLod = ComputeLodLevel(entities[i]);
            EntityManager.SetSharedComponent(entities[i], new LodLevel { Value = newLod });
        }

        entities.Dispose();
    }

    private int ComputeLodLevel(Entity e)
    {
        // 根据距离摄像机的距离计算 LOD 级别
        // 实际项目中通常每隔几帧更新一次，而非每帧
        return 0; // placeholder
    }
}
```

注意 `EntityManager.SetSharedComponent` 必须在主线程调用，且每次调用都是一次 Structural Change。批量操作时应尽量合并到同一帧的同一时间点，避免分散调用导致 Sync Point 碎片化。

## 小结

`ISharedComponentData` 是 ECS 中一把双刃剑：

- 它通过物理隔离 Chunk 实现了零成本的分组过滤，LOD 分级、材质合批、阵营区分都是经典用例
- 但每个不同值都会分裂 Chunk，基数过高时碎片化会严重拖累 Query 性能
- 修改值等同于 Structural Change，绝不能放在每帧的热路径中

判断是否该用 SharedComponent，只需回答两个问题：不同值的数量是否足够少？这个值是否很少变化？两个答案都是"是"，才是 SharedComponent 的适用场景。

---

下一篇 **DOTS-E08「Enableable Component」** 将介绍另一种分组与过滤机制——`IEnableableComponent`。它不触发 Structural Change，Enable/Disable 只需翻转 Chunk 内的一个 bit，是高频状态切换场景的理想替代方案。