---
title: "Unity DOTS E03｜SystemBase vs ISystem：Managed 与 Unmanaged 边界对系统设计的影响"
slug: "dots-e03-systembase-vs-isystem"
date: "2026-03-28"
description: "SystemBase 和 ISystem 不是风格差异，而是 Managed 和 Unmanaged 两种执行上下文的本质区别。本篇讲清楚两者在内存分配、Burst 兼容性、调度开销上的差异，以及工程中的选择依据。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "ISystem"
  - "SystemBase"
  - "Burst"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 3
weight: 1830
---

很多人第一次接触 DOTS 时，会把 `SystemBase` 和 `ISystem` 当成写法偏好来对待——"一个用 class，一个用 struct，随便选"。这个理解会在后期造成麻烦。

**两者的核心差异不是语法，而是执行上下文的边界**：一个活在 Managed heap，一个活在 Unmanaged 内存。这条边界决定了 Burst 编译能力、调度开销、以及持有状态的方式。

---

## 内存分配位置：从对象布局开始理解

**SystemBase** 继承自 `ComponentSystemBase`，是一个 C# class。它的实例由 GC 管理，分配在 Managed heap 上，可以持有任意 C# 对象引用。

**ISystem** 是一个 unmanaged struct。World 在内部维护一块 Native 内存区域，System 实例直接分配在其中，不经过 GC，不能包含引用类型字段。

```
Managed Heap                         Native / Unmanaged Memory
┌─────────────────────────────┐      ┌──────────────────────────────┐
│  SystemBase 实例 (class)    │      │  World 内部 SystemData 区     │
│  ┌───────────────────────┐  │      │  ┌──────────────────────────┐ │
│  │ m_StatePtr (IntPtr)   │──┼──┐   │  │ ISystem struct 实例       │ │
│  │ SomeClassRef          │  │  │   │  │ nativeField: int          │ │
│  │ Dictionary<K,V>       │  │  └──►│  │ localCache: NativeArray   │ │
│  └───────────────────────┘  │      │  └──────────────────────────┘ │
└─────────────────────────────┘      └──────────────────────────────┘
         GC 可见，可能触发 GC                  GC 不可见，无 GC 压力
```

SystemBase 内部有一个 `m_StatePtr` 指针，指向 World 侧的 `SystemState`（Unmanaged），但 System 对象本身始终在 Managed heap。这个双重身份是很多开销的来源。

---

## Burst 兼容性

这是选择两者时最关键的维度。

**ISystem** 的三个生命周期方法（`OnCreate`、`OnUpdate`、`OnDestroy`）全部可以标记 `[BurstCompile]`。标记后，整个方法体（包括其中调用的辅助方法）都在 Burst 编译器下运行，JIT 开销消除，SIMD 优化生效。

```csharp
[BurstCompile]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}
```

**SystemBase** 的 `OnUpdate` 本身**不能** Burst 编译——方法运行在 Managed 上下文中，Burst 编译器无法处理。旧的 `Entities.ForEach` 写法中，传入的 lambda 可以标记 `[WithBurst]`，Burst 只编译那个 lambda，外层调度逻辑仍是 Managed。

每次 `OnUpdate` 调用，SystemBase 都会经历一次 Managed → Native 的边界切换（调用 `SystemState` 内部方法），加上 Burst 覆盖范围受限，这是两者性能差异的根本原因。

---

## 代码对比：同一逻辑的两种写法

假设需求：每帧按 `Speed` 组件的值沿 X 轴移动所有 Entity。

### ISystem 写法（推荐，Unity 6 / Entities 1.x）

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MoveSystemISystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Speed>>())
        {
            transform.ValueRW.Position.x += speed.ValueRO.Value * dt;
        }
    }
}
```

`SystemAPI.Query` 在编译期生成类型化的 EntityQuery，`RefRW` / `RefRO` 明确表达读写意图，配合 Burst 后整个循环会被编译为 SIMD 指令。

### SystemBase 写法（旧式 Entities.ForEach，已不推荐）

```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MoveSystemBase_Legacy : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        Entities
            .ForEach((ref LocalTransform transform, in Speed speed) =>
            {
                transform.Position.x += speed.Value * dt;
            })
            .ScheduleParallel();
    }
}
```

### SystemBase 新写法（Unity 6，foreach + SystemAPI.Query）

```csharp
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MoveSystemBase_New : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Speed>>())
        {
            transform.ValueRW.Position.x += speed.ValueRO.Value * dt;
        }
    }
}
```

新写法语法和 ISystem 几乎一致，但 `OnUpdate` 本身依然在 Managed 上下文，无法获得完整 Burst 加速。

---

## 持有状态的场景

### SystemBase：直接持有 class 字段

SystemBase 是 class，可以像普通 MonoBehaviour 一样持有任意 Managed 对象：

```csharp
public partial class PoolManagerSystem : SystemBase
{
    // 直接持有 Managed 对象，无任何限制
    private GameObjectPool _pool;
    private Dictionary<int, AudioClip> _audioMap;

    protected override void OnCreate()
    {
        _pool = new GameObjectPool(prefab: null, capacity: 128);
        _audioMap = new Dictionary<int, AudioClip>();
    }

    protected override void OnUpdate()
    {
        // 可以直接调用 Managed API
    }
}
```

### ISystem：持有 Unmanaged 状态

ISystem struct 不能包含引用类型字段。持有 Unmanaged 状态有两种方式：

**方式一：在 struct 中直接声明 NativeContainer 字段**

```csharp
[BurstCompile]
public partial struct CacheSystem : ISystem
{
    // NativeArray 是 unmanaged，可以直接作为字段
    private NativeArray<float> _localCache;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _localCache = new NativeArray<float>(256, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_localCache.IsCreated) _localCache.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) { /* 使用 _localCache */ }
}
```

**方式二：通过 SystemAPI.ManagedAPI 访问 Managed 对象（不推荐频繁调用）**

```csharp
public partial struct HybridSystem : ISystem
{
    // 注意：此方法不能标记 [BurstCompile]，因为访问了 Managed 对象
    public void OnUpdate(ref SystemState state)
    {
        // 通过 ManagedAPI 访问 Managed 组件或单例
        var config = SystemAPI.ManagedAPI.GetSingleton<GameConfigManaged>();
        // config 是 class 实例，可以读取其中的 Managed 字段
    }
}
```

一旦使用 `ManagedAPI`，该方法就不能标记 `[BurstCompile]`，相当于局部退回到 Managed 上下文。

---

## 调度开销实测

Unity 官方在 Entities 1.x 的性能分析报告中给出的参考数字：

| 指标 | SystemBase | ISystem |
|------|-----------|---------|
| 单次 OnUpdate 空调用开销 | ~2.5 µs | ~0.5 µs |
| 100 个 System 每帧总开销 | ~250 µs | ~50 µs |
| Burst 覆盖范围 | 部分（lambda） | 全量 |
| GC Alloc 风险 | 存在（box, delegate） | 无 |

**ISystem 的 OnUpdate 调用开销约为 SystemBase 的 1/3 ~ 1/5**。在 System 数量少于 20 个时，这个差距不会显现在 profiler 里。但当项目规模扩大，System 数量超过 100 个（这在中型 DOTS 项目中很常见），每帧仅系统调度开销就可以相差 200µs 以上——这已经足够引起帧率波动。

---

## 选择依据

**默认选 ISystem**，以下情况直接使用：

- 新项目的所有 System 起点
- 热路径 System（高频 OnUpdate，Entity 数量多）
- 纯数据处理逻辑，无需持有 Managed 对象
- 希望 Burst 覆盖完整生命周期的场景

**选 SystemBase**，当且仅当：

- 需要持有 Managed class 引用（`GameObjectPool`、`AudioManager`、`Addressables Handle`）
- System 内部需要 Coroutine / async 逻辑（ISystem struct 无法 yield）
- 需要大量调用返回 Managed 结果的 Unity 内置 API（如物理查询返回 `RaycastHit[]`）
- 对接第三方 SDK，SDK 接口要求传入 class 实例

---

## 迁移路径：从 SystemBase 到 ISystem

已有 SystemBase 代码迁移时，按以下步骤操作：

**第一步：确认字段是否包含 Managed 类型**

```csharp
// 迁移前：SystemBase 持有 Managed 字段
public partial class OldSystem : SystemBase
{
    private List<Entity> _spawnQueue = new();   // Managed，需处理
    private NativeArray<float> _weights;         // Unmanaged，可直接迁移
}
```

`List<Entity>` 是 Managed 类型，迁移前需先替换为 `NativeList<Entity>`（Unmanaged）。

**第二步：改写 class 为 struct，实现 ISystem 接口**

```csharp
[BurstCompile]
public partial struct NewSystem : ISystem
{
    private NativeList<Entity> _spawnQueue;  // 替换为 NativeList
    private NativeArray<float> _weights;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _spawnQueue = new NativeList<Entity>(64, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_spawnQueue.IsCreated) _spawnQueue.Dispose();
        if (_weights.IsCreated) _weights.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) { /* 逻辑不变 */ }
}
```

**第三步：将 `Entities.ForEach` 替换为 `SystemAPI.Query`**

旧式 `Entities.ForEach` 不能在 ISystem 中使用，统一改为 `foreach` + `SystemAPI.Query`。

**哪些场景不值得迁移：**

- System 已经和大量 Managed 对象深度耦合（改动成本超过收益）
- System 调用频率极低（每秒一次以下），开销差异可忽略
- System 内部使用了 Coroutine 流程控制，重写代价过高

---

## 小结

| 维度 | SystemBase | ISystem |
|------|-----------|---------|
| 分配位置 | Managed heap | Unmanaged（World 内部） |
| Burst 编译 | 部分（lambda 内） | 完整（OnCreate/Update/Destroy） |
| 持有 Managed 对象 | 直接持有 | 需通过 ManagedAPI，且失去 Burst |
| 调度开销 | 较高 | 约为前者 1/5 |
| 适用场景 | 与 Managed 生态对接 | 纯 ECS 逻辑、热路径 System |

选择不是非此即彼——一个项目里两者可以共存。原则是：**能用 ISystem 的地方就用 ISystem，只在真正需要 Managed 能力的地方保留 SystemBase**。

---

下一篇 **DOTS-E04「EntityQuery 完整语法」** 将深入 `EntityQuery` 的构建方式——`SystemAPI.Query` 只是入口，背后的 QueryDesc、WithAll / WithAny / WithNone 过滤器、以及 ChangeFilter 才是控制 System 处理范围的核心工具。
