---
title: "CPU 性能优化 03｜Update 调用链优化：减少 Update 数量与手动调度管理器"
slug: "cpu-opt-03-update-scheduling"
date: "2026-03-28"
description: "Unity 的 Update 机制在大量 MonoBehaviour 时有显著的 C#-to-Native 调用开销。本篇分析 Update 的内部实现，给出减少 Update 数量的策略，以及用中央调度管理器替代分散 Update 的设计模式。"
tags: ["Unity", "Update", "性能优化", "CPU", "架构"]
series: "移动端硬件与优化"
weight: 2160
---

## Unity Update 的内部开销

`Update` 看起来只是一个普通的 C# 方法，但 Unity 调用它的方式决定了它的性能特征。

### C# 到 Native 的调用代价

Unity 引擎的核心是 C++ 代码，游戏逻辑是 C# 代码。每次 Unity 调用一个 MonoBehaviour 的 `Update` 方法，都是一次**跨越 Native-Managed 边界**的调用：

```
Unity Native Loop (C++)
    → PlayerLoopCallbacks (C++)
        → MonoBehaviour.Update 注册列表 (Native)
            → 每个 MonoBehaviour_Update 的 Native-to-Managed 桥接
                → C# Update() 方法执行
```

这个桥接（Bridge call）的代价包含：
- 参数封送（Marshaling）：把 Native 的调用上下文转换为托管环境
- 线程状态切换：GC 需要感知当前是否在 Native 代码中
- 返回时的清理工作

**实测数据（Unity 2022，Android 中端 ARM）**：

| MonoBehaviour 数量（含空 Update） | 每帧 Update 总开销 |
|----------------------------------|--------------------|
| 100 个                            | ~0.05 ms           |
| 500 个                            | ~0.25 ms           |
| 1000 个                           | ~0.5 ms            |
| 5000 个                           | ~2.5 ms            |
| 10000 个                          | ~5 ms              |

注意：**即使 Update 函数体是空的**，1000 个 MonoBehaviour 也会消耗约 0.5 ms，这完全是调用开销，不是逻辑开销。对于 60fps 游戏（帧时间 16.6 ms），这是相当大的固定成本。

### Unity 内部的 Update 注册机制

Unity 在 MonoBehaviour 的 `Awake` 或首次激活时，检查该类是否定义了 `Update` 方法，如果有，就把它加入一个内部的 C++ 列表（Native Update List）。每帧按列表顺序调用。

几个重要细节：
- **只有定义了 `Update` 方法的 MonoBehaviour 才会被注册**。如果一个 MonoBehaviour 根本没有声明 `Update`，Unity 不会尝试调用它，零开销。
- **`SetActive(false)` 会把 MonoBehaviour 从注册列表中移除**，下帧不再调用 Update，直到重新激活。
- **`enabled = false`** 同样将 MonoBehaviour 的 Update 反注册。
- 这个列表的遍历在 Native 侧完成，C# 端不直接参与遍历。

### 空 Update 的陷阱

一个常见的代码反模式：

```csharp
// 坏：声明了空 Update 但实际上什么都不做
// Unity 仍然会注册并每帧调用这个方法（一次 Native-to-Managed 桥接）
public class MyComponent : MonoBehaviour
{
    // 这个空方法每帧产生固定调用开销！
    void Update() { }
}

// 好：如果暂时不需要 Update，直接删除这个方法
public class MyComponent : MonoBehaviour
{
    // 没有 Update 方法，Unity 不注册，零调用开销
}
```

**检查项目中空 Update 的脚本**（在编辑器扩展中）：

```csharp
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;

public static class FindEmptyUpdates
{
    [MenuItem("Tools/Find Empty Update Methods")]
    static void Find()
    {
        var scripts = AssetDatabase.FindAssets("t:MonoScript");
        int count = 0;
        foreach (var guid in scripts)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            var type = script.GetClass();
            if (type == null) continue;

            var method = type.GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, System.Type.EmptyTypes, null);

            if (method != null && method.DeclaringType == type)
            {
                var body = method.GetMethodBody();
                // 方法体只有 return（2字节IL：ldnull + ret 或直接 ret）
                if (body != null && body.GetILAsByteArray().Length <= 2)
                {
                    Debug.LogWarning($"Empty Update: {type.FullName} in {path}", script);
                    count++;
                }
            }
        }
        Debug.Log($"Found {count} empty Update methods.");
    }
}
#endif
```

---

## 减少 Update 的策略

### 策略 1：禁用不可见和不活跃的对象

```csharp
// 当对象超出相机视野或逻辑上不活跃时，禁用 Update
public class Enemy : MonoBehaviour
{
    private bool _isVisible = true;

    void OnBecameInvisible()
    {
        // 相机看不到时，关闭 AI Update（但保留 Transform，不销毁对象）
        enabled = false; // 只禁用这个 MonoBehaviour 的 Update
    }

    void OnBecameVisible()
    {
        enabled = true; // 重新进入视野时恢复
    }
}

// 更彻底：整个 GameObject 不活跃时用对象池而不是 SetActive(false)
// SetActive(false) 会禁用所有子组件，但对象池可以复用已初始化的对象
```

**注意**：`OnBecameInvisible` 基于渲染器包围盒，编辑器 Scene 视图也算"相机"。在 SceneView 中看不到但 GameView 看得到的情况下，对象不会触发 `OnBecameInvisible`。需要用更可靠的距离/视锥体检测。

### 策略 2：Tick 间隔（不需要每帧更新的逻辑）

```csharp
// 坏：每帧检查 AI 决策（AI 决策不需要 60fps 精度）
public class EnemyAI : MonoBehaviour
{
    void Update()
    {
        // 这个决策每帧跑没意义，浪费 CPU
        DecideNextAction();
        UpdatePathfinding();
    }
}

// 好：按固定间隔更新 AI
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float _aiTickInterval = 0.2f; // 5次/秒
    private float _nextTickTime;

    void Update()
    {
        if (Time.time < _nextTickTime) return; // 未到时间，跳过
        _nextTickTime = Time.time + _aiTickInterval;
        DecideNextAction();
        UpdatePathfinding();
    }
}

// 更好：把不频繁的更新移出 Update，用 InvokeRepeating
public class EnemyAI : MonoBehaviour
{
    void Start()
    {
        // 从 0.1s 后开始，每 0.2s 调用一次
        // 注意：InvokeRepeating 内部用字符串反射，有微小开销
        InvokeRepeating(nameof(AITick), 0.1f, 0.2f);
    }

    void AITick()
    {
        DecideNextAction();
        UpdatePathfinding();
    }

    // Update 里只处理需要每帧精度的逻辑（如移动插值）
    void Update()
    {
        MoveTowardsTarget(Time.deltaTime);
    }
}
```

**交错 Tick**（Staggered Tick）：当场景中有大量 AI 时，避免所有 AI 在同一帧 Tick：

```csharp
public class EnemyAI : MonoBehaviour
{
    private static int _globalTickOffset = 0;
    private int _myTickOffset;
    private const int TICK_EVERY_N_FRAMES = 10; // 每 10 帧 Tick 一次

    void Awake()
    {
        // 每个 AI 分配不同的帧偏移，均匀分散到各帧
        _myTickOffset = _globalTickOffset % TICK_EVERY_N_FRAMES;
        _globalTickOffset++;
    }

    void Update()
    {
        if (Time.frameCount % TICK_EVERY_N_FRAMES != _myTickOffset) return;
        AITick(Time.deltaTime * TICK_EVERY_N_FRAMES); // 补偿累积的 deltaTime
    }
}
```

### 策略 3：事件驱动替代轮询

```csharp
// 坏：每帧轮询状态变化
public class HealthBar : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;
    private float _lastHp = -1;

    void Update()
    {
        // 每帧检查 HP 是否变化，然后更新 UI
        if (_stats.Hp != _lastHp)
        {
            _lastHp = _stats.Hp;
            UpdateHealthBarUI(_stats.Hp);
        }
    }
}

// 好：事件驱动，HP 变化时通知 UI
public class PlayerStats : MonoBehaviour
{
    public event Action<float> OnHpChanged;
    private float _hp;

    public float Hp
    {
        get => _hp;
        set
        {
            if (Mathf.Approximately(_hp, value)) return;
            _hp = value;
            OnHpChanged?.Invoke(_hp); // 只在 HP 真正变化时触发
        }
    }
}

public class HealthBar : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;

    void OnEnable()  { _stats.OnHpChanged += UpdateHealthBarUI; }
    void OnDisable() { _stats.OnHpChanged -= UpdateHealthBarUI; }

    private void UpdateHealthBarUI(float hp)
    {
        // 直接更新，无轮询，无 Update
        // 每秒可能只调用 2-3 次，而不是 60 次
    }
}
```

---

## 中央调度管理器（GameLoop Manager）

这是从根本上解决 Update 碎片化的架构方案：**用一个 Manager 的单次 Update 调用，驱动所有子系统的 Tick**。

### 核心思路

```
传统方式：
  Native Loop → MonoBehaviour_A.Update (Native-Managed 桥接)
  Native Loop → MonoBehaviour_B.Update (Native-Managed 桥接)
  Native Loop → MonoBehaviour_C.Update (Native-Managed 桥接)
  ... × N 个桥接调用

Manager 方式：
  Native Loop → GameLoopManager.Update (1次桥接)
                    → IUpdatable_A.Tick  (C# 虚调用，无桥接)
                    → IUpdatable_B.Tick  (C# 虚调用)
                    → IUpdatable_C.Tick  (C# 虚调用)
```

N 个 Native-Managed 桥接调用 → 1 次桥接 + (N-1) 次 C# 虚调用。C# 虚调用约 3-5 ns，远小于 Native-Managed 桥接的 ~500 ns。

### 完整实现

```csharp
// 可更新接口
public interface IUpdatable
{
    int UpdatePriority { get; } // 决定 Tick 顺序（小值先执行）
    void Tick(float deltaTime);
}

// 可选：支持固定帧率更新
public interface IFixedUpdatable
{
    void FixedTick(float fixedDeltaTime);
}

// 可选：支持 LateUpdate
public interface ILateUpdatable
{
    void LateTick(float deltaTime);
}
```

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央游戏循环管理器
/// 用单个 MonoBehaviour 驱动所有 IUpdatable，减少 Native-Managed 桥接次数
/// </summary>
public class GameLoopManager : MonoBehaviour
{
    // 单例
    private static GameLoopManager _instance;
    public static GameLoopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[GameLoopManager]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<GameLoopManager>();
            }
            return _instance;
        }
    }

    // 更新列表（需要排序时用 List，频繁注销时考虑 HashSet + 脏标志）
    private readonly List<IUpdatable> _updatables = new(128);
    private readonly List<IFixedUpdatable> _fixedUpdatables = new(32);
    private readonly List<ILateUpdatable> _lateUpdatables = new(32);

    // 脏标志：注册/注销后需要重新排序
    private bool _isDirty = false;

    // 迭代期间暂存的注册/注销请求（避免在 Tick 中修改列表）
    private readonly List<IUpdatable> _pendingAdd = new(16);
    private readonly List<IUpdatable> _pendingRemove = new(16);
    private bool _isTicking = false;

    #region 注册 / 注销

    public void Register(IUpdatable updatable)
    {
        if (updatable == null) return;

        if (_isTicking)
        {
            // Tick 期间不直接修改列表，加入待处理队列
            _pendingAdd.Add(updatable);
            return;
        }

        if (!_updatables.Contains(updatable))
        {
            _updatables.Add(updatable);
            _isDirty = true;
        }
    }

    public void Unregister(IUpdatable updatable)
    {
        if (updatable == null) return;

        if (_isTicking)
        {
            _pendingRemove.Add(updatable);
            return;
        }

        _updatables.Remove(updatable);
    }

    public void Register(IFixedUpdatable fixedUpdatable)
    {
        if (fixedUpdatable != null && !_fixedUpdatables.Contains(fixedUpdatable))
            _fixedUpdatables.Add(fixedUpdatable);
    }

    public void Unregister(IFixedUpdatable fixedUpdatable)
        => _fixedUpdatables.Remove(fixedUpdatable);

    public void Register(ILateUpdatable lateUpdatable)
    {
        if (lateUpdatable != null && !_lateUpdatables.Contains(lateUpdatable))
            _lateUpdatables.Add(lateUpdatable);
    }

    public void Unregister(ILateUpdatable lateUpdatable)
        => _lateUpdatables.Remove(lateUpdatable);

    #endregion

    #region Unity 生命周期（只有 3 次 Native-Managed 桥接）

    void Update()
    {
        // 应用待处理的排序
        if (_isDirty)
        {
            _updatables.Sort(static (a, b) =>
                a.UpdatePriority.CompareTo(b.UpdatePriority));
            _isDirty = false;
        }

        float dt = Time.deltaTime;
        _isTicking = true;

        // 批量 Tick：1 次 Native 桥接，N 次 C# 调用
        for (int i = 0; i < _updatables.Count; i++)
            _updatables[i].Tick(dt);

        _isTicking = false;
        FlushPendingChanges();
    }

    void FixedUpdate()
    {
        float fdt = Time.fixedDeltaTime;
        for (int i = 0; i < _fixedUpdatables.Count; i++)
            _fixedUpdatables[i].FixedTick(fdt);
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _lateUpdatables.Count; i++)
            _lateUpdatables[i].LateTick(dt);
    }

    #endregion

    private void FlushPendingChanges()
    {
        // 处理 Tick 期间积累的注册/注销请求
        if (_pendingRemove.Count > 0)
        {
            foreach (var item in _pendingRemove)
                _updatables.Remove(item);
            _pendingRemove.Clear();
        }

        if (_pendingAdd.Count > 0)
        {
            foreach (var item in _pendingAdd)
            {
                if (!_updatables.Contains(item))
                {
                    _updatables.Add(item);
                    _isDirty = true;
                }
            }
            _pendingAdd.Clear();
        }
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
```

**业务代码的使用方式**：

```csharp
// MonoBehaviour 注册到 Manager，自己不声明 Update
public class Enemy : MonoBehaviour, IUpdatable
{
    // 优先级常量：数值越小越先执行
    public const int PRIORITY_AI = 100;
    public const int PRIORITY_MOVEMENT = 200;
    public const int PRIORITY_ANIMATION = 300;

    public int UpdatePriority => PRIORITY_AI;

    void OnEnable()
    {
        GameLoopManager.Instance.Register(this);
    }

    void OnDisable()
    {
        GameLoopManager.Instance.Unregister(this);
    }

    // 这里是原来 Update 的逻辑
    public void Tick(float deltaTime)
    {
        UpdateAI(deltaTime);
        MoveToTarget(deltaTime);
    }
}

// 纯 C# 类（非 MonoBehaviour）也可以注册
public class WeaponSystem : IUpdatable
{
    public int UpdatePriority => Enemy.PRIORITY_MOVEMENT + 50;

    public void Initialize()
    {
        GameLoopManager.Instance.Register(this);
    }

    public void Dispose()
    {
        GameLoopManager.Instance.Unregister(this);
    }

    public void Tick(float deltaTime)
    {
        UpdateCooldowns(deltaTime);
        CheckAmmo();
    }
}
```

### 性能对比

以 1000 个 Enemy 为例：

| 方式                           | 每帧 Update 开销 |
|-------------------------------|----------------|
| 1000 个 MonoBehaviour.Update  | ~0.5 ms（主要是桥接）|
| 1 个 Manager + 1000 个 IUpdatable.Tick | ~0.05 ms（1次桥接 + 1000次C#虚调用）|
| 提升                           | ~10x             |

**注意**：虚调用的开销（~3-5 ns）在 1000 次时约 3-5 μs（0.003-0.005 ms），非常小。桥接的主要开销被消除。

---

## Unity 2022+ 的现代方案

### BurstCompatible Update（Jobs 化 Update 逻辑）

对于大量同质化对象（如 NPC、粒子、子弹），可以把 Update 逻辑搬到 Burst 编译的 Job 中：

```csharp
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

// 把 Enemy 数据分离为 NativeArray（纯数据，无引用）
public class EnemySystem : MonoBehaviour
{
    private NativeArray<float3> _positions;
    private NativeArray<float3> _velocities;
    private NativeArray<float> _healths;
    private int _enemyCount;

    // Burst 编译的 Job：完全在 Worker Thread 上执行，无 Native-Managed 桥接
    [BurstCompile]
    struct MoveEnemiesJob : IJobParallelFor
    {
        public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Velocities;
        public float DeltaTime;

        public void Execute(int index)
        {
            Positions[index] += Velocities[index] * DeltaTime;
        }
    }

    void Update()
    {
        // 调度 Job，后续帧完成（或在需要结果时等待）
        var job = new MoveEnemiesJob
        {
            Positions = _positions,
            Velocities = _velocities,
            DeltaTime = Time.deltaTime
        };

        // IJobParallelFor 自动并行化，使用 Worker Thread，不占主线程
        JobHandle handle = job.Schedule(_enemyCount, 64); // 批大小 64
        handle.Complete(); // 或者延迟到 LateUpdate 再 Complete，与渲染并行
    }
}
```

这个方案的额外收益：**并行执行**（多核）+ **SIMD 向量化**（Burst）。1000 个 Enemy 的移动更新，在 Burst + Jobs 下可以达到 0.01 ms 级别。

### ECS / DOTS 的 System 调度

Unity ECS（Entities 包）的 System 完全绕过 MonoBehaviour，使用 PlayerLoop 直接注册 C++ 系统调度：

```csharp
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

// ECS System：没有 MonoBehaviour，没有 Native-Managed 桥接
[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // 处理所有拥有 LocalTransform 和 EnemyVelocity 组件的 Entity
        foreach (var (transform, velocity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyVelocity>>())
        {
            transform.ValueRW.Position += velocity.ValueRO.Value * dt;
        }
    }
}
```

ECS 的调度完全在 Native 侧完成，托管代码（C#）只是描述逻辑，执行时由 Burst JIT 编译为机器码，性能与手写 C++ 相当。

---

## FixedUpdate 和 LateUpdate 的注意事项

### FixedUpdate 的 Catchup 行为

`FixedUpdate` 以固定时间步长（默认 0.02s = 50Hz）运行，与渲染帧率解耦。当渲染帧率低于物理帧率时，Unity 会在一帧内多次调用 `FixedUpdate` 来"追赶"时间：

```csharp
// 假设目标物理帧率 50Hz（0.02s/帧），渲染帧率降到 20fps（0.05s/帧）
// Unity 会在这 0.05s 的渲染帧中执行 2-3 次 FixedUpdate

// 危险：如果 FixedUpdate 执行时间超过 fixedDeltaTime，就会触发死亡螺旋
// FixedUpdate 越慢 → 需要更多次 Catchup → 帧越慢 → 更多 Catchup ...

// 防护：设置最大允许时间步长
// Time.maximumDeltaTime（默认 0.333s）限制单帧内 Catchup 的上限
// Time.maximumParticleDeltaTime（粒子系统的上限）

// 配置：Project Settings → Time → Maximum Allowed Timestep
```

**FixedUpdate 的使用原则**：
- 物理相关逻辑（Rigidbody 操作）放 FixedUpdate
- 逻辑上需要固定步长的游戏玩法（如战斗伤害计算、网络同步）放 FixedUpdate
- 不把重型逻辑放 FixedUpdate（它可能一帧跑多次）

### LateUpdate 与相机跟随

```csharp
// 问题：角色 Update 移动，相机 Update 跟随，可能出现执行顺序问题
// （相机 Update 比角色 Update 先执行，这一帧相机跟随的是上一帧位置）

// 解决方案 1：相机跟随逻辑放在 LateUpdate
// LateUpdate 在所有 Update 完成后执行，角色位置已是当帧最终位置
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    void LateUpdate() // 不是 Update
    {
        // 此时 _target 的位置已经是当帧 Update 之后的最终位置
        transform.position = Vector3.Lerp(
            transform.position,
            _target.position + _offset,
            _smoothSpeed * Time.deltaTime);
    }
}

// 解决方案 2：用 Script Execution Order 强制相机 Update 在角色之后
// Edit → Project Settings → Script Execution Order
// 或者在 MonoBehaviour 上加特性：
[DefaultExecutionOrder(1000)] // 数字大的后执行
public class CameraFollow : MonoBehaviour { ... }
```

### FixedUpdate 中操作 Transform 的性能陷阱

```csharp
// 当 GameObject 既有 Rigidbody 又手动设置 Transform 时，物理系统需要同步
// 频繁在 FixedUpdate 中 transform.position = ... 会导致物理系统重置刚体状态

// 坏：手动设置 Transform（绕过物理）
void FixedUpdate()
{
    transform.position += velocity * Time.fixedDeltaTime; // 直接移动，物理系统不感知
}

// 好：通过 Rigidbody API 移动
private Rigidbody _rb;
void FixedUpdate()
{
    _rb.MovePosition(_rb.position + velocity * Time.fixedDeltaTime); // 物理系统感知
    // 或者
    _rb.velocity = velocity; // 让物理引擎负责移动
}
```

---

## 综合优化案例

将以上策略组合应用到一个 100 个 Enemy 的场景：

```csharp
// 优化前：100 个 MonoBehaviour，每个都有 Update，含 AI + 物理查询
// 开销：100 * Native桥接(~500ns) + 100 * OverlapSphere(有分配) = ~50μs + GC压力

// 优化后：
public class EnemyManager : MonoBehaviour // 只有 1 个 MonoBehaviour 有 Update
{
    private List<EnemyData> _enemies = new(100);
    private Collider[] _queryBuffer = new Collider[10]; // 预分配，复用

    void Update()
    {
        float dt = Time.deltaTime;
        int frame = Time.frameCount;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];

            // AI 决策每 10 帧 Tick 一次，交错执行（i % 10 分散到各帧）
            if ((frame + i) % 10 == 0)
            {
                // NonAlloc 物理查询，无 GC
                int nearbyCount = Physics.OverlapSphereNonAlloc(
                    e.Position, e.DetectionRadius, _queryBuffer);
                e.UpdateAI(_queryBuffer, nearbyCount);
            }

            // 移动每帧更新（需要平滑）
            e.Move(dt);
        }
    }
}
// 优化后开销：1 * Native桥接 + 100 * C#方法调用 + 10 * NonAlloc查询/帧
// = ~0.5μs + ~1μs + ~5μs ≈ 6.5μs，远小于优化前的 50μs+
```

---

## 总结：Update 优化的优先级

1. **删除空 Update 方法**：零成本，立即见效
2. **禁用不可见对象的 Update**：`OnBecameInvisible` + `enabled = false`
3. **高频对象（>50 个）用中央调度 Manager**：消除桥接开销
4. **AI/逻辑降频 + 交错 Tick**：非实时逻辑 5-10 次/秒已够
5. **事件驱动替代状态轮询**：UI 更新、状态检测
6. **大量同质对象用 Jobs + Burst**：终极性能，完全并行
