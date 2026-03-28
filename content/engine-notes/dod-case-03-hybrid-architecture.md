---
title: "DOD 实战案例 03｜混合架构设计：ECS 仿真层 + GameObject 表现层的稳定边界策略"
slug: "dod-case-03-hybrid-architecture"
date: "2026-03-28"
description: "没有纯 ECS 项目，真实项目永远是混合的。本篇讲清楚 ECS 仿真层和 GameObject 表现层的稳定边界怎样设计——哪些数据是权威，哪些是镜像，同步时机如何保证，以及哪些边界设计会让混合架构越来越难改。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "混合架构"
  - "GameObject"
  - "架构设计"
  - "数据导向"
series: "数据导向实战案例"
primary_series: "dod-cases"
series_role: "article"
series_order: 3
weight: 2230
---

网上的 ECS 教程几乎都是"纯 ECS"示例：所有逻辑全写在 System 里，所有数据全放在 Component 里，世界干净整洁。真实项目不是这样的。UI 跑在 Canvas 上，音效挂在 AudioSource 里，相机是 Managed 对象，输入是事件驱动的——这些东西迁不进 ECS，也没有必要迁。

**混合架构不是妥协，是理性选择**。问题只有一个：边界要稳，数据流向要清晰。

---

## 为什么永远是混合架构

Unity ECS（Entities 1.x）擅长的只有一件事：管理**大量同构的仿真对象**。成千上万个单位的位置更新、AI 状态机推进、战斗伤害结算——这是 ECS 的主场。

以下这些东西，ECS 管不了，也不应该管：

| 系统 | 为什么不适合 ECS |
|------|-----------------|
| UI（Canvas / TextMeshPro） | 依赖 Managed 对象图，每个控件是独一份 |
| 音频（AudioSource） | Managed 组件，必须挂在 GameObject 上 |
| 相机（Camera） | 单例性质，Managed，带 Unity 渲染管线钩子 |
| 输入（New Input System） | 事件驱动回调，不是每帧轮询 |
| Animator / 粒子系统 | 有内部状态机，设计上是 Managed |

结论很简单：**ECS 管仿真，MonoBehaviour 管表现和单例系统**，中间留一条单向的数据通道。

---

## 三层架构：仿真 → 桥接 → 表现

稳定边界的核心是**数据流单向**。仿真层产生数据，表现层消费数据，两者不互相引用。

```
┌─────────────────────────────────────────────┐
│  仿真层（ECS World）                          │
│  - LocalTransform, Velocity, HP              │
│  - MovementSystem, CombatSystem, AISystem    │
│  - 权威数据，Job 并行，高吞吐                  │
└──────────────────┬──────────────────────────┘
                   │ 单向写出
┌──────────────────▼──────────────────────────┐
│  桥接层（Singleton Component + NativeQueue） │
│  - InputSingleton（输入 → ECS）              │
│  - HitEventQueue（ECS → 表现）               │
│  - FollowTargetBuffer（位置镜像）             │
└──────────────────┬──────────────────────────┘
                   │ 单向读取
┌──────────────────▼──────────────────────────┐
│  表现层（MonoBehaviour / UI）                 │
│  - HealthBarUI, DamageNumberUI               │
│  - CameraController, AudioManager           │
│  - 角色 Animator, VFX                        │
└─────────────────────────────────────────────┘
```

**黄金规则**：仿真层里的任何 System，不允许持有 MonoBehaviour 引用，不允许访问 GameObject。表现层里的 MonoBehaviour，可以读桥接层数据，但不写入仿真层的核心状态。

---

## 输入到 ECS 的正确接法

New Input System 用回调通知输入事件，ECS 用轮询读取数据。两者的驱动模型不同，需要一个 Singleton 做缓冲。

**桥接组件（只存数据，无逻辑）：**

```csharp
// Bridge/InputSingleton.cs
using Unity.Entities;
using Unity.Mathematics;

public struct InputSingleton : IComponentData
{
    public float2 MoveDirection;  // 归一化方向
    public bool   JumpPressed;    // 单帧触发
    public bool   FireHeld;       // 持续按住
}
```

**MonoBehaviour 写入侧（主线程，每帧 OnUpdate）：**

```csharp
// Presentation/PlayerInputBridge.cs
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputBridge : MonoBehaviour
{
    private EntityManager _em;
    private Entity        _inputEntity;

    void Start()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _inputEntity = _em.CreateEntity(typeof(InputSingleton));
    }

    void Update()
    {
        var input = new InputSingleton
        {
            MoveDirection = Gamepad.current != null
                ? Gamepad.current.leftStick.ReadValue()
                : (Vector2)new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")),
            JumpPressed = Input.GetKeyDown(KeyCode.Space),
            FireHeld    = Input.GetMouseButton(0)
        };
        _em.SetComponentData(_inputEntity, input);
    }
}
```

**ECS System 读取侧（可跑 Burst）：**

```csharp
// ECS/Systems/MovementSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 读取 InputSingleton
        var input = SystemAPI.GetSingleton<InputSingleton>();
        float speed = 5f;
        float3 delta = new float3(input.MoveDirection.x, 0f, input.MoveDirection.y)
                       * speed * SystemAPI.Time.DeltaTime;

        foreach (var (transform, tag) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerTag>>())
        {
            transform.ValueRW.Position += delta;
        }
    }
}
```

`InputSingleton` 是桥接层的"邮箱"——MonoBehaviour 每帧覆盖写入，ECS 每帧读取，两侧完全解耦。

---

## ECS 事件到表现层（血条更新、死亡特效）

仿真层产生的事件（受击、死亡、升级）需要传递给表现层。按事件频率选方案：

### 方案 1：Singleton 状态镜像（低频，简单）

System 把结果写进 Singleton，MonoBehaviour 在 `LateUpdate` 里读。适合全局状态（玩家 HP、得分）。

```csharp
// Bridge/PlayerStateMirror.cs
public struct PlayerStateMirror : IComponentData
{
    public int   CurrentHP;
    public int   MaxHP;
    public bool  IsDead;
}
```

```csharp
// Presentation/HealthBarUI.cs
void LateUpdate()
{
    var mirror = _em.GetSingleton<PlayerStateMirror>();
    _slider.value = (float)mirror.CurrentHP / mirror.MaxHP;
    if (mirror.IsDead) ShowDeathScreen();
}
```

### 方案 2：NativeQueue 事件流（高频，批量）

战斗中每帧可能有数十次受击，用队列避免"跨帧覆盖"。

```csharp
// Bridge/HitEventChannel.cs
using Unity.Collections;
using Unity.Entities;

public struct HitEventChannel : IComponentData
{
    public NativeQueue<HitEvent> Queue;
}

public struct HitEvent
{
    public int   Damage;
    public float3 WorldPosition; // 飘字位置
}
```

```csharp
// ECS/Systems/CombatSystem.cs（节选）
var channel = SystemAPI.GetSingletonRW<HitEventChannel>();
channel.ValueRW.Queue.Enqueue(new HitEvent
{
    Damage        = damage,
    WorldPosition = transform.Position
});
```

```csharp
// Presentation/DamageNumberUI.cs
void LateUpdate()
{
    var channel = _em.GetSingleton<HitEventChannel>();
    while (channel.Queue.TryDequeue(out var evt))
    {
        SpawnDamageNumber(evt.Damage, evt.WorldPosition);
    }
}
```

**注意**：`NativeQueue` 在 `Dispose` 前需要手动管理生命周期。建议在一个专用的 `BridgeBootstrap` MonoBehaviour 里统一创建和释放。

### 方案 3：C# event（主线程 System，极低频）

只有不跑 Job、不跑 Burst 的主线程 System 才能触发 C# event。适合"玩家死亡"这类一局一次的事件。

```csharp
// 主线程 System（不加 [BurstCompile]）
public partial class PlayerDeathSystem : SystemBase
{
    public static event System.Action OnPlayerDied;

    protected override void OnUpdate()
    {
        Entities.WithAll<PlayerTag, DeadTag>().ForEach((Entity e) =>
        {
            OnPlayerDied?.Invoke();
            EntityManager.DestroyEntity(e);
        }).WithoutBurst().Run();
    }
}
```

---

## GameObject 表现对象跟随 ECS Entity

角色的视觉表现（Animator、粒子、阴影）仍然在 GameObject 上，需要跟随 ECS Entity 的位置移动。

**方案 A：MonoBehaviour 每帧读 Entity Position（简单，中低密度可用）**

```csharp
// Presentation/EntityFollower.cs
public class EntityFollower : MonoBehaviour
{
    public Entity Target;
    private EntityManager _em;

    void Start()  => _em = World.DefaultGameObjectInjectionWorld.EntityManager;

    void LateUpdate()
    {
        if (!_em.Exists(Target)) return;
        var pos = _em.GetComponentData<LocalTransform>(Target).Position;
        transform.position = pos;
    }
}
```

单个 `EntityManager.GetComponentData` 的主线程开销约 0.1–0.5 µs。100 个以内没有问题，1000 个以上建议改方案 B。

**方案 B：System 批量写出位置缓冲（中高密度）**

```csharp
// Bridge/FollowTargetBuffer.cs
public struct FollowTargetBuffer : IComponentData
{
    public NativeArray<float3> Positions; // 按 index 对应 GameObject 列表
}
```

System 在主线程写满缓冲，MonoBehaviour 统一读取，减少跨层调用次数。

**方案 C：Entities.Graphics（纯静态网格，零 MonoBehaviour）**

对于子弹、碎片、树木等**没有 Animator、没有复杂表现逻辑**的对象，使用 Entities.Graphics（前 Hybrid Renderer）直接渲染，完全不需要 GameObject 跟随。

```csharp
// 给 Entity 添加渲染所需组件
_em.AddComponentData(entity, new RenderMeshArray(...));
_em.AddComponentData(entity, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
```

**选择依据**：

| 情况 | 推荐方案 |
|------|---------|
| 有 Animator、复杂特效，数量 < 200 | 方案 A |
| 有 Animator、复杂特效，数量 200–2000 | 方案 B |
| 纯静态网格，数量不限 | 方案 C |

---

## 反模式：让混合架构越来越烂的写法

**反模式 1：MonoBehaviour 每帧逐个查询 Entity**

```csharp
// 错误写法 — 每帧为每个 Entity 调用一次，开销随对象数线性增长
void Update()
{
    foreach (var unit in _units)
        unit.HP = _em.GetComponentData<Health>(unit.Entity).Value;
}
```

正确做法：System 批量写出镜像，MonoBehaviour 读镜像。

**反模式 2：System 持有 MonoBehaviour 引用**

```csharp
// 错误写法 — 仿真层依赖表现层，方向反转
public partial class CombatSystem : SystemBase
{
    public HealthBarUI UIRef; // 不允许
    protected override void OnUpdate()
    {
        UIRef.SetHP(hp); // 仿真层直接操控 UI
    }
}
```

单向依赖被反转后，System 无法独立测试，UI 重构会牵连仿真逻辑。

**反模式 3：在 Job 内访问 MonoBehaviour**

Job 在工作线程执行，Unity Managed 对象只能在主线程访问。这不是"不推荐"，是**直接报错**。任何需要访问 MonoBehaviour 的逻辑，必须放在主线程 System 里，不能加 `[BurstCompile]`。

**反模式 4：ECS 和 MonoBehaviour 互相引用**

```csharp
// EntityFollower.cs 持有 Entity（可以）
// CombatSystem.cs 持有 EntityFollower 引用（不允许）
// 两者互相持有 → 循环依赖 → 无法单独部署和测试
```

---

## 渐进式迁移策略

不要试图一次把整个项目迁到 ECS。正确路径：

1. **第一步**：把最热的仿真逻辑迁进去（大量单位的移动、碰撞检测）。保留所有 MonoBehaviour 表现逻辑不动。
2. **第二步**：建立桥接层（InputSingleton、事件队列），验证数据流单向。
3. **第三步**：逐步扩大 ECS 范围（战斗、AI）。每次扩展后，表现层通过桥接层读数据，不直接访问 ECS。
4. **长期**：表现层永远留在 MonoBehaviour。不要为了"纯 ECS"强行把 UI、音效、Animator 迁进去。

---

## 项目目录结构

```
Assets/
├── ECS/
│   ├── Components/          # IComponentData 定义
│   │   ├── LocalMovement.cs
│   │   ├── Health.cs
│   │   └── AIState.cs
│   └── Systems/             # ISystem / SystemBase
│       ├── MovementSystem.cs
│       ├── CombatSystem.cs
│       └── AISystem.cs
├── Bridge/                  # 桥接层：Singleton + 事件队列
│   ├── InputSingleton.cs
│   ├── HitEventChannel.cs
│   ├── PlayerStateMirror.cs
│   └── BridgeBootstrap.cs   # 负责 NativeQueue 生命周期
└── Presentation/            # 表现层：纯 MonoBehaviour
    ├── PlayerInputBridge.cs
    ├── HealthBarUI.cs
    ├── DamageNumberUI.cs
    ├── EntityFollower.cs
    └── CameraController.cs
```

每层的职责边界：

- **ECS/**：只知道数据和规则，不知道有 UI 和 GameObject 存在
- **Bridge/**：只存数据结构，不包含业务逻辑
- **Presentation/**：只读桥接层，不直接写 ECS 核心状态（InputSingleton 除外）

---

## 小结

混合架构的稳定性来自**边界的纪律性**，而不是技术选型。

ECS 仿真层产生权威数据，桥接层做单向缓冲，表现层消费镜像数据——这三条规则如果每个人都遵守，混合架构可以长期保持可维护。一旦某个 System 开始持有 MonoBehaviour 引用，或者某个 UI 开始直接写 ECS 状态，边界就开始腐烂，后续每次改动的代价都会更高。

---

> 至此，**数据导向实战案例**三篇（DOD 案例 01：Burst 性能优化、DOD 案例 02：内存布局与缓存命中、DOD 案例 03：混合架构设计）全部完结。整个数据导向专题——从硬件内存层级、DOTS 核心系统、Mass Entity 实践、行业架构对比，到三篇实战案例——已经完整覆盖。后续如需深入某一方向（Netcode for Entities、Physics DOTS、大世界流式加载），可作为独立专题展开。
