---
title: "Unity DOTS E17｜MonoBehaviour ↔ ECS 边界：Managed 与 Unmanaged 世界的数据传递模式"
slug: "dots-e17-monobehaviour-ecs-boundary"
date: "2026-03-28"
description: "没有纯 ECS 项目，Managed 的 MonoBehaviour 世界和 Unmanaged 的 ECS 世界必须共存。本篇讲清楚跨边界数据传递的几种模式，以及哪些边界设计会让混合架构长期可维护，哪些会让它越来越难改。"
tags:
  - "Unity"
  - "DOTS"
  - "ECS"
  - "MonoBehaviour"
  - "混合架构"
  - "边界"
series: "Unity DOTS 工程实践"
primary_series: "unity-dots-engineering"
series_role: "article"
series_order: 17
weight: 1970
---

## 边界比迁移更重要

很多团队在引入 DOTS 时的第一个误区，是把它当成"把所有 MonoBehaviour 替换掉"的任务。这条路在 2026 年的实际项目里走不通：UI 系统（UGUI / UI Toolkit）、平台输入（Input System）、物理关节（ConfigurableJoint）、音频（AudioSource）、Animator 状态机——这些组件深度依赖 Unity 的 Managed 对象模型，短期内不会有 Unmanaged 的替代品。

目标因此要换一个表述：**让 ECS 的批量仿真层和 Managed 的表现/控制层有清晰的边界，而不是让其中一层消灭另一层。**

清晰边界带来三件事：ECS 侧的代码能充分利用 Burst 和 Job System；Managed 侧的代码保持熟悉的面向对象结构；两侧的开发者可以独立迭代，不会因为一次重构把另一侧弄坏。

---

## 从 MonoBehaviour 写入 ECS

### 模式 1：EntityManager 直接写

最直接的方式。MonoBehaviour 拿到 `World.DefaultGameObjectInjectionWorld` 的 `EntityManager`，在主线程里直接设值。

```csharp
public class PlayerInputMono : MonoBehaviour
{
    private EntityManager _em;
    private Entity _inputEntity;

    void Start()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        // 假设已有一个专用的 InputSingleton Entity
        _inputEntity = _em.CreateEntity(typeof(InputSingleton));
    }

    void Update()
    {
        var input = new InputSingleton
        {
            MoveDir = new float2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            FirePressed = Input.GetButtonDown("Fire1")
        };
        _em.SetComponentData(_inputEntity, input);
    }
}
```

缺点：只能在主线程上调用，且每次 `SetComponentData` 都会触发一次结构性访问检查。适合低频写入，不适合每帧大批量操作。

### 模式 2：EntityCommandBuffer 录制

在 MonoBehaviour 里创建 ECB，录制操作，交给 `ECBSystem` 在下一帧的合适时机播放。这在需要创建/销毁 Entity 时更安全。

```csharp
void SpawnEnemy(float3 pos)
{
    var ecbSystem = World.DefaultGameObjectInjectionWorld
        .GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
    var ecb = ecbSystem.CreateCommandBuffer();
    var e = ecb.CreateEntity();
    ecb.AddComponent(e, new LocalTransform { Position = pos, Scale = 1f });
    ecb.AddComponent(e, new EnemyTag());
}
```

ECB 录制是线程安全的（使用 `ecb.AsParallelWriter()` 还可以在 Job 里录制），但注意：播放发生在下一帧，如果当帧就需要读取结果，要用 `EntityManager` 直接写。

### 模式 3：Singleton Component 作为通道（推荐）

这是最推荐的日常模式。MonoBehaviour 把数据写入一个 Singleton Component，ECS System 在仿真时读取它。双方通过数据解耦，互不持有引用。

```csharp
// 共享数据定义（放在公共 Assembly）
public struct InputSingleton : IComponentData
{
    public float2 MoveDir;
    public bool FirePressed;
}

// MonoBehaviour 侧：写入
public class InputBridge : MonoBehaviour
{
    void Update()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var singleton = world.EntityManager
            .GetComponentData<InputSingleton>(
                SystemAPI.GetSingletonEntity<InputSingleton>());
        singleton.MoveDir = new float2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
        singleton.FirePressed = Input.GetButtonDown("Fire1");
        world.EntityManager.SetComponentData(
            SystemAPI.GetSingletonEntity<InputSingleton>(), singleton);
    }
}

// ECS System 侧：读取
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var input = SystemAPI.GetSingleton<InputSingleton>();
        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>())
        {
            transform.ValueRW.Position +=
                new float3(input.MoveDir.x, 0, input.MoveDir.y)
                * speed.ValueRO.Value
                * SystemAPI.Time.DeltaTime;
        }
    }
}
```

---

## 从 ECS 读取到 MonoBehaviour

### 模式 1：主线程 System 完成后，MonoBehaviour 轮询 Singleton

ECS System 把结果写入 Singleton，MonoBehaviour 在自己的 `Update` 里轮询。简单可靠，适合每帧都需要同步的数据（如相机跟随目标位置）。

关键约束：MonoBehaviour 的读取必须发生在对应 System 的 Update **完成之后**。Unity 的 PlayerLoop 会先执行 ECS World 的 Update（在 `SimulationSystemGroup` 里），再执行 MonoBehaviour 的 Update，因此默认顺序天然满足这个要求。

```csharp
public class CameraFollowMono : MonoBehaviour
{
    void Update()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var target = em.GetComponentData<CameraTargetSingleton>(
            SystemAPI.GetSingletonEntity<CameraTargetSingleton>());
        transform.position = Vector3.Lerp(
            transform.position, (Vector3)target.Position, Time.deltaTime * 5f);
    }
}
```

### 模式 2：System 触发 C# 事件

适合低频、事件驱动的场景（角色死亡、关卡结束）。System 在主线程里 raise 一个静态 C# event，MonoBehaviour 订阅它。

```csharp
// 战斗系统触发事件
public partial class CombatSystem : SystemBase
{
    public static event Action<Entity, int> OnHpChanged;

    protected override void OnUpdate()
    {
        // 注意：只能在主线程里触发 C# event，不能在 Burst Job 里
        Entities.WithoutBurst().ForEach((Entity e, ref Health hp, ref DamageEvent dmg) =>
        {
            hp.Value -= dmg.Amount;
            OnHpChanged?.Invoke(e, hp.Value);
        }).Run();
    }
}

// UI MonoBehaviour 订阅
public class HpBarUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    private Entity _targetEntity;

    void OnEnable()  => CombatSystem.OnHpChanged += HandleHpChanged;
    void OnDisable() => CombatSystem.OnHpChanged -= HandleHpChanged;

    void HandleHpChanged(Entity e, int hp)
    {
        if (e != _targetEntity) return;
        _slider.value = hp / 100f;
    }
}
```

注意：`WithoutBurst()` 是必须的，Burst 编译的代码不能调用托管委托。

### 模式 3：System 写 NativeArray，MonoBehaviour 读取

适合批量数据（小地图上所有单位的位置）。System 持有一个共享 `NativeArray`，MonoBehaviour 在 Job 完成后读取。

```csharp
public partial class MinimapDataSystem : SystemBase
{
    public NativeArray<float2> UnitPositions;

    protected override void OnCreate()
    {
        UnitPositions = new NativeArray<float2>(1024, Allocator.Persistent);
    }

    protected override void OnDestroy() => UnitPositions.Dispose();

    protected override void OnUpdate()
    {
        var positions = UnitPositions;
        Entities.ForEach((int entityInQueryIndex, in LocalTransform t, in UnitTag _) =>
        {
            if (entityInQueryIndex < positions.Length)
                positions[entityInQueryIndex] = t.Position.xz;
        }).ScheduleParallel();
        // MonoBehaviour 在下一帧读取，Job 已完成
    }
}
```

---

## Managed Component：class 作为 IComponentData

DOTS 允许把 `class` 标记为 `IComponentData`，用来持有 Unity Object 引用（`Animator`、`AudioSource` 等）。这是在完全迁移不可行时的过渡方案。

```csharp
// Managed Component：注意是 class，不是 struct
public class AnimatorComponent : IComponentData
{
    public Animator Animator;
}

// 创建时注入引用
var e = em.CreateEntity();
em.AddComponentObject(e, new AnimatorComponent { Animator = go.GetComponent<Animator>() });

// System 读取（必须 WithoutBurst）
public partial class AnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach((AnimatorComponent anim, in AnimState state) =>
        {
            anim.Animator.SetFloat("Speed", state.Speed);
        }).Run();
    }
}
```

代价很明确：不能 Burst 编译，不能并行 Job，`class` 实例存在 GC 压力。把它视为临时桥梁，而不是长期设计目标。

---

## CompanionObject：了解原理即可

`Entities.Graphics`（原 Hybrid Renderer）内部使用 CompanionObject 机制：对于需要 Managed 组件的 Entity（如带 `Animator` 的角色），系统会自动维护一个隐藏的 GameObject，将 Managed 组件挂在其上，位置每帧跟随 Entity 同步。

这解释了为什么在使用 Entities.Graphics 时，Hierarchy 窗口里会出现一些命名奇怪的隐藏 GameObject。不建议项目直接依赖这个机制，了解它的存在有助于排查"为什么这个 Entity 有一个对应的 GameObject"之类的困惑。

---

## 边界设计原则

经过上面几种模式，可以总结出几条原则：

**单向依赖**：ECS 仿真层（System、Component）不应该持有对 MonoBehaviour 的引用。依赖方向只能是 Managed → ECS（写入），或 ECS → Singleton（MonoBehaviour 读取），不能反过来让 System 持有 `MonoBehaviour` 引用并调用其方法。

**数据权威唯一**：同一份数据（HP、世界坐标）只在一侧是"权威"，另一侧只读或只显示。HP 的权威在 ECS，UI 只显示；玩家输入的权威在 MonoBehaviour，MovementSystem 只读。两侧都能写同一份数据是最常见的 bug 来源。

**同步点明确**：MonoBehaviour 读 ECS 数据必须在对应 System 完成之后。默认 PlayerLoop 顺序（ECS SimulationSystemGroup 先于 MonoBehaviour Update）天然满足，不要把 MonoBehaviour 的读取放进 `FixedUpdate` 或协程里随意打乱顺序。

**反模式：每帧在 MonoBehaviour 里调用 `EntityManager.GetComponent`**

```csharp
// 反模式：不要这样做
void Update()
{
    // 每帧遍历查询，造成结构性访问开销 + 强耦合
    var entities = _em.GetAllEntities();
    foreach (var e in entities)
    {
        if (_em.HasComponent<Health>(e))
        {
            var hp = _em.GetComponentData<Health>(e);
            // ... 处理逻辑
        }
    }
    entities.Dispose();
}
```

正确做法是把这类查询逻辑移到 System 里完成，结果写入 Singleton 或通过事件推送。

---

## 典型分层结构

实际项目推荐把代码分成三层：

```
┌──────────────────────────────┐
│  Managed 层                   │  UI、输入、音频、相机
│  MonoBehaviour / ScriptableObject │  熟悉的面向对象写法
└──────────────┬───────────────┘
               │  读写 Singleton / 事件通道
┌──────────────▼───────────────┐
│  桥接层                       │  InputSingleton, EventChannel
│  Singleton Component / NativeArray │  数据契约，双方共识
└──────────────┬───────────────┘
               │  SystemAPI.GetSingleton<>
┌──────────────▼───────────────┐
│  ECS 层                       │  仿真：位置、速度、AI、战斗
│  ISystem / IJobEntity         │  Burst 编译，Job 并行
└──────────────────────────────┘
```

桥接层是关键。它定义了双方的数据契约，改动需要双方协商，但改动范围可以控制得很小。ECS 层完全不知道 MonoBehaviour 的存在，Managed 层只需要了解 Singleton 的数据结构，不需要理解 System 内部逻辑。

---

## 小结

混合架构的可维护性，90% 取决于边界是否清晰，10% 取决于哪侧用了多少 DOTS。把 Singleton Component 作为主要通道，单向依赖，数据权威唯一——这三条原则能让一个混合项目在两年后仍然可以安全迭代。

下一篇 **E18「DOTS 调试工具全景」** 将介绍如何用 Entities Hierarchy、Systems 窗口、Memory Profiler 和 Burst Inspector 实际观察这套架构的运行状态，包括如何定位 Singleton 数据异常、System 执行顺序错乱等常见问题。
