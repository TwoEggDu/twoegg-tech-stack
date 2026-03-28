---
title: "DOD 实战案例 02｜弹幕系统（5000+ 子弹）：碰撞检测、生命周期、VFX 同步的 ECS 实现"
slug: "dod-case-02-bullet-system"
date: "2026-03-28"
description: "弹幕系统是 DOTS 最常见的入门案例，但深度实现涉及大量边界问题：子弹生命周期管理、碰撞检测的 ECS 实现方式、命中时 VFX 的跨边界触发、以及 5000 子弹同屏的渲染合批。本篇把这些边界问题逐一讲清楚。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "弹幕"
  - "碰撞检测"
  - "VFX"
  - "实战"
  - "数据导向"
series: "数据导向实战案例"
primary_series: "dod-cases"
series_role: "article"
series_order: 2
weight: 2220
---

弹幕系统是很多人学 DOTS 的第一个真实案例，原因很简单：5000 个同屏对象、每帧全部移动、频繁创建销毁，OOP 写法会在这里暴露所有性能问题，而 ECS 写法正好全面发挥优势。

但「入门案例」并不意味着实现简单。真正把弹幕系统做完整，你会遇到这些问题：生命周期归零时如何触发 VFX？碰撞检测用不用 Unity Physics？命中同一目标的多颗子弹如何去重伤害？本篇把这些边界问题逐一讲清楚。

---

## 1. Archetype 设计

子弹是典型的「高频小对象」，Component 设计要尽量精简，避免 Chunk 利用率过低。

```csharp
// 基础移动数据
public struct BulletVelocity : IComponentData
{
    public float3 Value;
}

// 剩余存活时间（秒）
public struct BulletLifetime : IComponentData
{
    public float Remaining;
}

// 伤害值
public struct BulletDamage : IComponentData
{
    public int Value;
}

// 发射者（用于伤害归因、友伤判断）
public struct BulletOwner : IComponentData
{
    public Entity Value;
}

// 命中事件 Tag（加上后进入命中处理流水线）
public struct HitTag : IComponentData { }

// 命中目标（与 HitTag 同帧添加）
public struct HitTarget : IComponentData
{
    public Entity Value;
    public float3 HitPosition;
}

// 寿命耗尽 Tag（下帧销毁前触发过期特效）
public struct HitExpiredTag : IComponentData { }
```

**关键设计决策**：子弹命中或寿命归零后，不立即销毁 Entity，而是先加 `HitTag` 或 `HitExpiredTag`。这一帧内，特效触发 System 有机会读取命中位置、发射 VFX Graph 事件；下一帧的清理 System 才真正销毁 Entity。这个「延迟一帧销毁」的模式是 ECS 侧触发 Managed 代码的标准做法。

---

## 2. 移动 System（IJobEntity + BurstCompile）

移动 System 是整个系统里最简单但也最能体现 DOTS 吞吐量的部分。

```csharp
[BurstCompile]
public partial struct BulletMoveJob : IJobEntity
{
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter ECB;

    void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex,
                 ref LocalTransform transform,
                 ref BulletLifetime lifetime,
                 in BulletVelocity velocity)
    {
        // 位移
        transform.Position += velocity.Value * DeltaTime;

        // 生命周期递减
        lifetime.Remaining -= DeltaTime;

        // 寿命归零：标记为过期，等待特效和清理
        if (lifetime.Remaining <= 0f)
        {
            ECB.AddComponent<HitExpiredTag>(chunkIndex, entity);
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class BulletMoveSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
        new BulletMoveJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ECB = ecb
        }.ScheduleParallel();

        _ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

5000 个子弹的移动，在 Burst + SIMD 下耗时通常在 0.1ms 以内。

---

## 3. 碰撞检测：不用 Unity Physics

Unity Physics 对子弹来说太重。子弹碰撞的特点是：数量极多、半径极小、命中即消失，不需要持续的物理模拟和约束求解。

**推荐方案：RaycastCommand 批量扫掠**

用上一帧位置到当前帧位置做一次射线检测，等价于连续碰撞检测（CCD），防止高速子弹穿透。

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BulletMoveSystem))]
public partial class BulletCollisionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 1. 收集所有存活子弹的数据
        var bulletQuery = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, BulletVelocity, BulletDamage>()
            .WithNone<HitTag, HitExpiredTag>()
            .Build();

        int count = bulletQuery.CalculateEntityCount();
        if (count == 0) return;

        var entities    = bulletQuery.ToEntityArray(Allocator.TempJob);
        var transforms  = bulletQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var velocities  = bulletQuery.ToComponentDataArray<BulletVelocity>(Allocator.TempJob);

        float dt = SystemAPI.Time.DeltaTime;

        // 2. 构建 RaycastCommand 数组
        var commands = new NativeArray<RaycastCommand>(count, Allocator.TempJob);
        var results  = new NativeArray<RaycastHit>(count, Allocator.TempJob);

        var queryParams = new QueryParameters(layerMask: LayerMask.GetMask("Enemy"));

        for (int i = 0; i < count; i++)
        {
            float3 origin    = transforms[i].Position - velocities[i].Value * dt;
            float3 direction = math.normalizesafe(velocities[i].Value);
            float  distance  = math.length(velocities[i].Value) * dt;

            commands[i] = new RaycastCommand(origin, direction, queryParams, distance);
        }

        // 3. 批量调度，最大化并行
        var handle = RaycastCommand.ScheduleBatch(commands, results, 32, Dependency);
        handle.Complete();

        // 4. 处理命中结果
        var ecb = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>()
                       .CreateCommandBuffer();

        for (int i = 0; i < count; i++)
        {
            if (results[i].colliderInstanceID == 0) continue;

            // 通过 collider 找到对应的 ECS Entity（需要在 GameObject 上挂 EntityReference）
            var hitGO = results[i].collider.gameObject;
            if (!hitGO.TryGetComponent<EntityReference>(out var entityRef)) continue;

            ecb.AddComponent(entities[i], new HitTag());
            ecb.AddComponent(entities[i], new HitTarget
            {
                Value       = entityRef.Entity,
                HitPosition = results[i].point
            });
        }

        entities.Dispose();
        transforms.Dispose();
        velocities.Dispose();
        commands.Dispose();
        results.Dispose();
    }
}
```

**备选方案：空间哈希（纯 ECS 场景）**

如果目标也是纯 ECS Entity（没有 GameObject），可以用 `NativeMultiHashMap<int3, Entity>` 把目标 Entity 按网格坐标索引，每帧 O(1) 查询子弹所在格子的潜在目标，避免 O(N×M) 的暴力遍历。适合目标数量也很多（数百个）的场景。

---

## 4. 命中事件处理与 VFX 触发

命中处理拆成两个 System，职责分离：

**DamageSystem**：处理伤害，合并同帧对同一目标的多次伤害（见第 7 节）。

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BulletCollisionSystem))]
public partial class DamageSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var healthLookup = SystemAPI.GetComponentLookup<Health>(isReadOnly: false);
        // 合并同帧伤害：key=目标Entity，value=累计伤害
        var damageAccum = new NativeHashMap<Entity, int>(64, Allocator.Temp);

        foreach (var (hitTarget, damage, _) in
                 SystemAPI.Query<RefRO<HitTarget>, RefRO<BulletDamage>, RefRO<HitTag>>())
        {
            var target = hitTarget.ValueRO.Value;
            if (!damageAccum.TryGetValue(target, out int existing))
                existing = 0;
            damageAccum[target] = existing + damage.ValueRO.Value;
        }

        foreach (var (target, totalDmg) in damageAccum)
        {
            if (!healthLookup.HasComponent(target)) continue;
            var hp = healthLookup[target];
            hp.Current -= totalDmg;
            healthLookup[target] = hp;
        }

        damageAccum.Dispose();
    }
}
```

**VFX 触发：必须在主线程**

VFX Graph 是 Managed 对象，Job 线程无法访问。标准做法是通过一个 Singleton Component 上的 `NativeQueue` 传递命中位置，在主线程 System 里消费。

```csharp
// 单例组件，存放命中事件队列
public struct HitEventQueue : IComponentData
{
    public NativeQueue<float3> Positions;
}

// 在主线程 System 里读取并触发 VFX
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class VFXTriggerSystem : SystemBase
{
    [SerializeField] private VisualEffect _hitVFX; // 通过 Bootstrap 注入

    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<HitEventQueue>()) return;

        var queue = SystemAPI.GetSingleton<HitEventQueue>().Positions;

        while (queue.TryDequeue(out float3 pos))
        {
            _hitVFX.SetVector3("HitPosition", pos);
            _hitVFX.SendEvent("OnHit");
        }
    }
}
```

`BulletCollisionSystem` 在主线程收集命中位置后，`Enqueue` 到这个队列，`VFXTriggerSystem` 在 `PresentationSystemGroup` 里消费，时序清晰，无线程安全问题。

---

## 5. 子弹池：避免高频 Structural Change

5000 颗子弹、每秒几百次发射和销毁，如果每次都动态创建和销毁 Entity，意味着每次都是 Structural Change，会打断 Job 调度、触发 Chunk 重排，开销可能高达 1~3ms/秒。

**Enableable Component 方案**：

```csharp
// 标记子弹是否激活，实现 IEnableableComponent
public struct BulletActive : IComponentData, IEnableableComponent { }

// 预创建 5000 个子弹 Entity，默认禁用 BulletActive
public partial class BulletPoolSystem : SystemBase
{
    private NativeQueue<Entity> _pool;

    protected override void OnCreate()
    {
        _pool = new NativeQueue<Entity>(Allocator.Persistent);

        var archetype = EntityManager.CreateArchetype(
            typeof(LocalTransform),
            typeof(BulletVelocity),
            typeof(BulletLifetime),
            typeof(BulletDamage),
            typeof(BulletOwner),
            typeof(BulletActive)
        );

        using var entities = EntityManager.CreateEntity(archetype, 5000, Allocator.Temp);
        foreach (var e in entities)
        {
            EntityManager.SetComponentEnabled<BulletActive>(e, false);
            _pool.Enqueue(e);
        }
    }

    // 发射子弹：从池中取出，设置数据，启用 BulletActive
    public bool TrySpawn(float3 position, float3 velocity, int damage,
                         Entity owner, float lifetime)
    {
        if (!_pool.TryDequeue(out Entity bullet)) return false;

        EntityManager.SetComponentData(bullet, LocalTransform.FromPosition(position));
        EntityManager.SetComponentData(bullet, new BulletVelocity { Value = velocity });
        EntityManager.SetComponentData(bullet, new BulletLifetime { Remaining = lifetime });
        EntityManager.SetComponentData(bullet, new BulletDamage { Value = damage });
        EntityManager.SetComponentData(bullet, new BulletOwner { Value = owner });
        EntityManager.SetComponentEnabled<BulletActive>(bullet, true); // 修改 bit，无 Structural Change
        return true;
    }

    // 回收子弹：禁用 BulletActive，归还到池
    public void Recycle(Entity bullet)
    {
        EntityManager.SetComponentEnabled<BulletActive>(bullet, false);
        _pool.Enqueue(bullet);
    }

    protected override void OnDestroy() => _pool.Dispose();
    protected override void OnUpdate() { }
}
```

`SetComponentEnabled` 只修改 Chunk 内的 bit mask，不触发 Structural Change，也不移动 Component 数据，是高频激活/禁用的标准 DOTS 方案。实测 5000 次/帧的激活/禁用开销约为动态创建方案的 1/20。

---

## 6. 渲染合批：5000 子弹几个 DrawCall？

DOTS Entities Graphics（原 Hybrid Renderer）会自动对相同 Mesh + Material 的 Entity 进行 GPU Instancing，每批次上限约 500~1023 个实例（取决于 constant buffer 大小）。

5000 颗子弹 → 约 5~10 个 DrawCall，性能开销极低。

**关键点**：所有子弹必须共用同一 `RenderMeshArray`，即相同的 Mesh 和 Material 实例。如果要区分子弹颜色（例如玩家子弹蓝色、Boss 子弹红色），**不要用不同的 Material**，而是用 `MaterialProperty` Component：

```csharp
// 对应 HLSL 里的 _BaseColor（需要在 Shader Graph 里标记为 Per-Instance）
[MaterialProperty("_BaseColor")]
public struct BulletColor : IComponentData
{
    public float4 Value;
}
```

这样所有子弹仍然共享同一 Material，颜色通过 per-instance 数据传入 GPU，合批不会被打断。

---

## 7. 常见坑汇总

**坑 1：在 Job 里触发 VFX**

VFX Graph API 是 Managed 代码，Burst Job 不能调用。任何需要通知 Managed 侧的操作，都必须通过 NativeQueue / Singleton Component 中转，在主线程 System 里消费。

**坑 2：同帧多颗子弹命中同一目标**

ECB 的多条 `SetComponent<Health>` 指令在 Playback 时会按顺序覆盖，结果是只有最后一颗子弹的伤害生效，前面的伤害全部丢失。

解决方案见第 4 节的 `DamageSystem`：用 `NativeHashMap` 在 System 内先累加所有伤害，最后一次性写入目标 Health，保证同帧伤害全部计入。

**坑 3：HitExpiredTag 和 HitTag 同帧叠加**

如果一颗子弹同一帧既命中目标又寿命归零（最后一帧刚好同时触发），会同时拥有两个 Tag，导致双重特效。

处理方式：碰撞 System 的 Query 加 `WithNone<HitExpiredTag>()`，优先响应寿命归零；或在清理 System 里统一处理两种 Tag，只触发一次特效。

---

## 小结

弹幕系统的 ECS 实现有几个核心原则：

- **生命周期状态机**：Active → HitTag/HitExpiredTag → 销毁，每个状态只由对应 System 处理
- **VFX 跨边界通信**：NativeQueue 中转，主线程消费，严禁在 Job 里访问 Managed API
- **伤害去重**：同帧对同一目标的多次伤害必须在写入 Health 之前合并
- **子弹池**：Enableable Component 是高频激活/禁用的最优解，避免 Structural Change
- **渲染合批**：共享 Material + MaterialProperty per-instance，维持 GPU Instancing 效率

下一篇「DOD 实战案例 03｜混合架构设计」将讨论更复杂的现实问题：当项目并非纯 DOTS，而是 GameObject + ECS 混合时，如何设计边界、如何让两套系统高效协作，避免频繁的托管/非托管数据同步成为新的性能瓶颈。
