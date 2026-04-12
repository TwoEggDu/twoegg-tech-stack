---
title: "DOD 实战案例 01｜大规模单位调度（RTS）：ECS + Jobs 完整实现，从 1000 到 100000 单位的扩展路径"
slug: "dod-case-01-rts-units"
date: "2026-03-28"
description: "RTS 游戏的单位调度是 ECS 最经典的使用场景：大量同构对象，每帧更新移动、战斗、AI。本篇从 Archetype 设计出发，讲清楚 1000 到 100000 单位的扩展路径——Query 优化、LOD 触发、渲染端接入各自在哪个量级开始变关键。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "RTS"
  - "性能优化"
  - "实战"
  - "数据导向"
series: "数据导向实战案例"
primary_series: "dod-cases"
series_role: "article"
series_order: 1
weight: 2210
---

RTS 游戏里的单位是 ECS 最"教科书级"的使用对象——数量大、结构同构、每帧都要更新。但从 1000 到 100000 单位，优化路径完全不同：1000 时随便写都能跑，10000 开始需要并行化，50000 以上非 LOD 不可。本篇把这条扩展路径拆开，每个量级讲清楚瓶颈在哪、该做什么。

---

## Archetype 设计

一个 RTS 单位在不同阶段对应不同的 Archetype，这是 ECS 架构的核心决策点。

### 核心单位（待机 / 移动状态）

```csharp
// 每帧必须更新的移动相关数据
struct Position      : IComponentData { public float3 Value; }
struct Velocity      : IComponentData { public float3 Value; }
struct MoveTarget    : IComponentData { public float3 Value; }

// 生命值，Combat 阶段写入
struct Health        : IComponentData { public float Current; public float Max; }

// SharedComponent：同队伍单位聚集在同一批 Chunk
struct TeamID        : ISharedComponentData { public int Value; }
struct UnitType      : ISharedComponentData { public int MeshIndex; }
```

### 战斗中（叠加 Combat Fragment）

```csharp
struct AttackTarget  : IComponentData { public Entity Value; }
struct DamageOutput  : IComponentData { public float Value; public float Cooldown; }
```

进入攻击范围时用 `EntityManager.AddComponent` 追加这两个 Fragment，Archetype 自动切换。

### 死亡后（等待动画播完）

```csharp
struct DeathTimer    : IComponentData { public float Remaining; }
// 移除 AttackTarget、DamageOutput、MoveTarget、Velocity
// 保留 Position、Health（值已 <= 0）、RenderMesh（播死亡动画）
```

**为什么 TeamID 用 SharedComponent？**

SharedComponent 的值相同的 Entity 会被放进同一批 Chunk。同队伍单位聚合在一起后，AI 批量处理时（"找同队伍最近单位"）遍历的内存是连续的，Cache 命中率显著提高。代价是：不同 TeamID 值越多，Chunk 越碎——4 支队伍就会把单位分进 4 组 Chunk，可以接受；如果做 MMO 式几十个阵营就要权衡。

---

## 三个核心 System

### MovementSystem

```csharp
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        new MoveJob { DeltaTime = dt }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref Position pos, ref Velocity vel, in MoveTarget target)
        {
            float3 dir = target.Value - pos.Value;
            float dist = math.length(dir);
            if (dist > 0.1f)
            {
                vel.Value = math.normalize(dir) * 5f;
                pos.Value += vel.Value * DeltaTime;
            }
            else
            {
                vel.Value = float3.zero;
            }
        }
    }
}
```

`IJobEntity` 配合 `ScheduleParallel()` 是 ECS 并行移动更新的标准写法。每个线程处理一个 Chunk，天然无写冲突（不同 Entity 的 Position 互不重叠）。

### CombatSystem

```csharp
[BurstCompile]
public partial struct CombatSystem : ISystem
{
    private ComponentLookup<Health> _healthLookup;

    public void OnCreate(ref SystemState state)
    {
        _healthLookup = state.GetComponentLookup<Health>(isReadOnly: false);
    }

    public void OnUpdate(ref SystemState state)
    {
        _healthLookup.Update(ref state);
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        new CombatJob
        {
            HealthLookup = _healthLookup,
            DeltaTime    = SystemAPI.Time.DeltaTime
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct CombatJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Health> HealthLookup;
        public float DeltaTime;

        void Execute(in AttackTarget target, ref DamageOutput dmg)
        {
            dmg.Cooldown -= DeltaTime;
            if (dmg.Cooldown > 0f) return;
            dmg.Cooldown = 0.5f; // 攻击间隔

            if (!HealthLookup.HasComponent(target.Value)) return;
            var h = HealthLookup[target.Value];
            h.Current -= dmg.Value;
            HealthLookup[target.Value] = h;
        }
    }
}
```

`ComponentLookup` 是随机访问的关键路径，后文会专门讨论它的 Cache 问题。

### DeathSystem

```csharp
[BurstCompile]
public partial struct DeathSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (health, entity) in
                 SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.Current <= 0f)
            {
                ecb.RemoveComponent<AttackTarget>(entity);
                ecb.RemoveComponent<DamageOutput>(entity);
                ecb.RemoveComponent<MoveTarget>(entity);
                ecb.RemoveComponent<Velocity>(entity);
                ecb.AddComponent(entity, new DeathTimer { Remaining = 1.5f });
            }
        }
    }
}
```

ECB（EntityCommandBuffer）在帧末统一执行结构变更，避免在 Job 执行期间修改 Archetype 引发数据竞争。

---

## 1000 单位：基础实现

1000 单位时三个 System 顺序执行完全够用，单帧总耗时通常在 0.5ms 以内。

**主要隐患：CombatSystem 的 ComponentLookup 随机访问。**

每个攻击者要读取被攻击者的 Health。被攻击目标分布在不同 Chunk，访问顺序随机，Cache 命中率低。1000 单位时这个问题不明显，但要提前埋下优化点：

```csharp
// 按攻击目标的 Entity Index 排序，让相邻攻击者尽量读同一个 Chunk
// 实现方式：用 NativeArray 收集 (attacker, target) 对，按 target.Index 排序后再处理
```

实际上在 1000 单位时不必真的实现这个排序，但要知道 10000 时需要它。

---

## 10000 单位：并行化

单线程已经撑不住。10000 单位时，纯单线程的移动更新约 3~5ms，Combat 约 2~4ms，合计容易超过 8ms 帧预算。

**MovementSystem** 直接改 `ScheduleParallel()` 即可，代码已在上面展示。实测从 ~4ms 降到 ~0.8ms（8 核机器，量级参考）。

**CombatSystem** 用 ECB ParallelWriter 处理 Health 变化：

```csharp
// 不直接写 Health，而是用 ECB 记录"扣血事件"，帧末统一合并
// 优点：并行安全；缺点：同一帧对同一目标的多次攻击需要合并逻辑
struct DamageEvent : IBufferElementData
{
    public float Amount;
}
// 各攻击者向目标的 DynamicBuffer<DamageEvent> 追加数据（ECB.AppendToBuffer）
// DeathSystem 阶段再遍历 Buffer 求和，更新 Health
```

**新问题：JobHandle.Complete 的位置。**

`ScheduleParallel()` 返回 `JobHandle`，如果在同帧稍后的代码里调用 `.Complete()`，主线程会阻塞等待。错误示例：

```csharp
// 错误：在 OnUpdate 末尾立刻 Complete，等于白并行
var handle = new MoveJob().ScheduleParallel();
handle.Complete(); // 主线程在这里卡住
```

正确做法是把 `JobHandle` 传给 `state.Dependency`，让 ECS 框架统一在合适时机 Complete（通常是下一帧开始前，或下游 System 真正需要数据时）。

**Profiler 读图技巧：** 在 Unity Profiler 的 Timeline 视图里，找 `Worker Thread` 行，如果 Worker 大量时间是空闲的（灰色），说明并行度不足；如果主线程有长时间的 `WaitForJobGroupID`，说明 Complete 点太早。

---

## 100000 单位：LOD 分级

10 万单位每帧全量更新 Combat + Movement 在任何硬件上都是不现实的。解决方案：**距离 LOD**。

### Enableable Component 控制遍历范围

```csharp
// 可启用/禁用的标记组件（IEnableableComponent）
struct IsInCombatRange : IComponentData, IEnableableComponent { }
```

`IEnableableComponent` 是 Entities 1.x 的特性：禁用时不移出 Archetype（不触发 Chunk 重排），只在 Query 层面被过滤掉，开销极低。

### LOD System

```csharp
[BurstCompile]
public partial struct CombatLODSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 假设有一个"摄像机焦点"位置
        float3 focusPos = GetCameraFocus();

        foreach (var (pos, entity) in
                 SystemAPI.Query<RefRO<Position>>().WithEntityAccess())
        {
            float dist = math.distance(pos.ValueRO.Value, focusPos);
            // 超过 200m 禁用 Combat 更新
            SystemAPI.SetComponentEnabled<IsInCombatRange>(entity, dist < 200f);
        }
    }
}
```

CombatSystem 的 Query 加上过滤条件：

```csharp
// 只处理 IsInCombatRange 启用的单位
SystemAPI.Query<...>().WithAll<IsInCombatRange>()
```

MovementSystem 做分帧更新：

```csharp
// 距离远的单位，只在 frameCount % 4 == 0 时更新
bool shouldUpdate = dist < 200f || (Time.frameCount % 4 == 0);
```

### 实际性能数字（量级参考，i7-12700H 估算）

| 场景 | 帧耗时（ECS 部分） |
|------|-----------------|
| 10 万单位，无 LOD | ~8ms |
| 10 万单位，LOD 开启（~20% 在 Combat 范围内） | ~2ms |

这是估算量级，实际数字因 Archetype 复杂度、攻击密度、硬件差异而变化，自测时以 Profiler 数据为准。

---

## 渲染端接入（Entities.Graphics）

### SharedComponent 组合的 Chunk 碎片化问题

Entities.Graphics 用 `RenderMeshArray`（SharedComponent）+ `MaterialMeshInfo` 驱动 GPU Instancing。当同一个 Archetype 上有多个 SharedComponent 时，Chunk 按所有 SharedComponent 的值组合分组：

```
TeamID=0, UnitType=Warrior  → Chunk A
TeamID=0, UnitType=Archer   → Chunk B
TeamID=1, UnitType=Warrior  → Chunk C
TeamID=1, UnitType=Archer   → Chunk D
```

4 支队伍 × 4 种单位类型 = 16 组 Chunk。每组 Chunk 容纳 128 个 Entity（典型值），16 组 × 128 = 只有 2048 个 Entity 才能填满一批。10 万单位时 Chunk 总数约 800，碎片不严重；但如果 TeamID 有几十个值，就会出现大量半空 Chunk，内存浪费且遍历效率下降。

### 推荐做法：用 MaterialProperty 做颜色差异

```csharp
// 不用不同的 RenderMesh 区分队伍颜色
// 改用 IComponentData + MaterialProperty 传颜色到 GPU
[MaterialProperty("_TeamColor")]
struct TeamColor : IComponentData { public float4 Value; }
```

这样所有队伍共享同一个 Mesh，RenderMeshArray 的 SharedComponent 只有一个值，Chunk 碎片化问题消失。TeamID 的 SharedComponent 仍然保留，用于 AI 批量查询同队单位，但不影响渲染分组。

---

## 扩展路径总结

| 单位数量 | 主要瓶颈 | 关键优化 |
|---------|---------|---------|
| < 1000 | 无明显瓶颈 | 基础 ECS 足够，不必过早优化 |
| 1000~10000 | 单线程执行时间 | `ScheduleParallel()`，正确传递 JobHandle |
| 10000~50000 | ComponentLookup 随机访问 Cache Miss | 攻击目标按 Entity Index 排序，ECB 批量合并伤害 |
| > 50000 | 总计算量超出帧预算 | Enableable Component LOD 分级，远距单位降频更新 |

几个容易忽视的细节：

- **Structural Change 频率**：单位死亡时的 Archetype 切换有同步开销，大量单位同帧死亡时 ECB 回放会有明显的 `ECBPlayback` 峰值。可以分帧限制每帧处理的死亡数量。
- **World 分离**：10 万单位的 ECS World 和渲染 World 建议分开（Entities.Graphics 默认已做），避免 Query 遍历到不相关 Entity。
- **Profiler Marker**：给每个 System 加 `ProfilerMarker`，方便在 Timeline 里精确定位耗时，而不是依赖猜测。

---

## 下一篇

[DOD 实战案例 02：弹幕系统](/engine-notes/dod-case-02-bullet-system) 会把同样的思路用在另一个极端——对象数量更大（几十万颗子弹）但生命周期极短、结构更简单。和 RTS 单位相比，弹幕的核心问题不是 LOD，而是**生成和销毁的吞吐量**：每帧可能新增数千颗子弹，ECB 的写入策略和 Chunk 填充率的平衡是关键。
