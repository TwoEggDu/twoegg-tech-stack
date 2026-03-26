---
date: "2026-03-26"
title: "游戏编程设计模式 03｜Observer / Event Bus：事件驱动解耦"
description: "Observer 模式让系统之间通过事件通信，而不是直接调用。这篇对比 C# 委托、UnityEvent、ScriptableObject Event Channel 三种实现方式，并讲清楚 Event Bus 的适用场景与陷阱。"
slug: "pattern-03-observer-event-bus"
weight: 731
tags:
  - 软件工程
  - 设计模式
  - Observer
  - Event Bus
  - 游戏架构
series: "游戏编程设计模式"
---

> **Observer 模式**：定义对象间的一对多依赖关系，当一个对象状态改变时，所有依赖它的对象都自动收到通知并更新。
>
> 这是游戏里使用频率最高的模式之一。成就系统、UI 刷新、音效触发、任务进度——几乎所有"A 发生了，B 需要响应"的场景，都可以用 Observer 来解耦。

---

## 没有 Observer 时的问题

玩家击杀了一个敌人。这件事需要通知很多系统：

```csharp
// 没有 Observer：战斗系统直接耦合所有相关系统
public class PlayerCombat : MonoBehaviour
{
    // 直接引用所有需要通知的系统
    private AchievementSystem achievements;
    private QuestSystem quests;
    private UIManager ui;
    private AudioManager audio;
    private AnalyticsTracker analytics;
    private ComboCounter combo;

    public void OnKillEnemy(Enemy enemy)
    {
        killCount++;

        achievements.CheckKillAchievements(killCount, enemy.type);
        quests.OnEnemyKilled(enemy.type, enemy.location);
        ui.ShowKillFeed(enemy.name);
        audio.PlayKillSound(enemy.type);
        analytics.TrackEvent("enemy_killed", enemy.type);
        combo.OnKill();
    }
}
```

这段代码的问题：
- `PlayerCombat` 知道了六个本来不需要知道的系统
- 加一个新系统（比如"连杀特效"），要改 `PlayerCombat`
- 没有某个系统（某些平台不需要 Analytics），要改 `PlayerCombat`
- 测试 `PlayerCombat` 的战斗逻辑，必须 Mock 六个系统

---

## Observer 模式：通过事件解耦

核心思想：`PlayerCombat` 不直接调用其他系统，而是**发布一个事件**。其他系统**订阅**这个事件，自己决定怎么响应。

```
发布方（Publisher）：知道"发生了什么"，但不知道"谁在关心"
订阅方（Subscriber）：知道"自己要响应什么"，但不知道"是谁发布的"
```

---

## 实现方式一：C# 委托（Action / event）

C# 原生的事件机制，性能最好，适合同一程序集内的解耦。

```csharp
// 定义事件数据
public class EnemyKilledEventArgs
{
    public EnemyType EnemyType { get; }
    public Vector3 Position { get; }
    public int TotalKillCount { get; }

    public EnemyKilledEventArgs(EnemyType type, Vector3 pos, int count)
    {
        EnemyType = type;
        Position = pos;
        TotalKillCount = count;
    }
}

// 发布方：只负责发布事件
public class PlayerCombat : MonoBehaviour
{
    public static event Action<EnemyKilledEventArgs> OnEnemyKilled;

    private int killCount;

    public void KillEnemy(Enemy enemy)
    {
        killCount++;
        OnEnemyKilled?.Invoke(new EnemyKilledEventArgs(
            enemy.Type,
            enemy.transform.position,
            killCount
        ));
    }
}

// 订阅方：各自独立处理
public class AchievementSystem : MonoBehaviour
{
    void OnEnable()  => PlayerCombat.OnEnemyKilled += HandleKill;
    void OnDisable() => PlayerCombat.OnEnemyKilled -= HandleKill;

    void HandleKill(EnemyKilledEventArgs args)
    {
        if (args.TotalKillCount == 100) Unlock("百人斩");
        if (args.EnemyType == EnemyType.Boss) Unlock("猎杀传说");
    }
}

public class QuestSystem : MonoBehaviour
{
    void OnEnable()  => PlayerCombat.OnEnemyKilled += HandleKill;
    void OnDisable() => PlayerCombat.OnEnemyKilled -= HandleKill;

    void HandleKill(EnemyKilledEventArgs args)
    {
        UpdateKillObjective(args.EnemyType);
    }
}
```

**优点**：性能好，类型安全，IDE 支持友好。
**缺点**：`static event` 是全局的，订阅方必须知道发布方的类型（有一定耦合）；忘记取消订阅会导致内存泄漏（被 `static event` 持有引用的对象不会被 GC）。

**必须配对**：每个 `OnEnable` 的 `+=` 都要在 `OnDisable` 里有对应的 `-=`。

---

## 实现方式二：UnityEvent

Unity 提供的事件系统，可以在 Inspector 里直接连接订阅方，不需要写代码。

```csharp
using UnityEngine.Events;

[System.Serializable]
public class EnemyKilledEvent : UnityEvent<EnemyKilledEventArgs> { }

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] public EnemyKilledEvent onEnemyKilled;

    public void KillEnemy(Enemy enemy)
    {
        killCount++;
        onEnemyKilled?.Invoke(new EnemyKilledEventArgs(enemy.Type, enemy.transform.position, killCount));
    }
}
```

在 Inspector 里，可以直接把 `AchievementSystem` 拖进去，选择方法，无需代码。

**优点**：零代码连接，策划/TA 可以在 Inspector 里配置；序列化到场景文件，打开场景就能看到所有连接关系。
**缺点**：运行时动态订阅比 C# event 慢约 10 倍；Inspector 连接有时候在场景重构时会断（修改了组件或 GameObject 结构）；不能直接传递复杂参数类型。

---

## 实现方式三：ScriptableObject Event Channel

Unity 官方推荐的架构模式（来自 Open Projects），特别适合**跨场景、跨 Prefab 的通信**。

```csharp
// 事件通道：一个 ScriptableObject 资产
[CreateAssetMenu(menuName = "Events/EnemyKilledEvent")]
public class EnemyKilledEventChannel : ScriptableObject
{
    public event Action<EnemyKilledEventArgs> OnEventRaised;

    // 发布方调用这个方法
    public void Raise(EnemyKilledEventArgs args)
    {
        OnEventRaised?.Invoke(args);
    }
}

// 发布方：引用 ScriptableObject 资产
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private EnemyKilledEventChannel enemyKilledChannel;

    public void KillEnemy(Enemy enemy)
    {
        killCount++;
        enemyKilledChannel.Raise(new EnemyKilledEventArgs(enemy.Type, enemy.transform.position, killCount));
    }
}

// 订阅方：同样引用同一个 ScriptableObject 资产
public class AchievementSystem : MonoBehaviour
{
    [SerializeField] private EnemyKilledEventChannel enemyKilledChannel;

    void OnEnable()  => enemyKilledChannel.OnEventRaised += HandleKill;
    void OnDisable() => enemyKilledChannel.OnEventRaised -= HandleKill;

    void HandleKill(EnemyKilledEventArgs args) { /* ... */ }
}
```

发布方和订阅方之间**没有任何直接引用**，它们只是都引用了同一个 `.asset` 文件。

**优点**：
- 完全解耦（发布方和订阅方互不知道对方）
- 跨场景通信（ScriptableObject 不依附于场景）
- 在 Inspector 里可以直接看到连接关系（哪些对象引用了这个 Channel）
- 可以在编辑器里手动 `Raise`，方便调试

**缺点**：需要为每种事件类型创建对应的 ScriptableObject 类；资产文件数量可能增多。

---

## Event Bus：全局消息总线

当系统数量多、层次深，不想手动管理每对发布/订阅关系时，可以引入 Event Bus（全局消息总线）。

```csharp
// 全局事件总线：任何地方都可以发布/订阅
public static class EventBus
{
    private static Dictionary<Type, Delegate> handlers = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out Delegate existing))
            handlers[type] = Delegate.Combine(existing, handler);
        else
            handlers[type] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out Delegate existing))
        {
            var updated = Delegate.Remove(existing, handler);
            if (updated == null) handlers.Remove(type);
            else handlers[type] = updated;
        }
    }

    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out Delegate handler))
            ((Action<T>)handler)?.Invoke(eventData);
    }
}

// 使用示例
public class PlayerCombat : MonoBehaviour
{
    public void KillEnemy(Enemy enemy)
    {
        EventBus.Publish(new EnemyKilledEventArgs(enemy.Type, enemy.transform.position, ++killCount));
    }
}

public class AchievementSystem : MonoBehaviour
{
    void OnEnable()  => EventBus.Subscribe<EnemyKilledEventArgs>(HandleKill);
    void OnDisable() => EventBus.Unsubscribe<EnemyKilledEventArgs>(HandleKill);
    void HandleKill(EnemyKilledEventArgs args) { /* ... */ }
}
```

**Event Bus 的优缺点**：

优点：完全解耦，发布方和订阅方互不知道对方；简单统一的 API。

缺点：
- **调试困难**：当一个事件触发了你不预期的响应，很难追踪是哪里订阅了（因为订阅关系是隐式的）
- **事件命名冲突**：用 string 做 key 时容易拼错；用 Type 做 key 时不同模块的同名类型会冲突
- **顺序不可控**：多个订阅方的执行顺序不确定
- **内存泄漏风险**：更高——因为全局总线会持有对订阅方的引用，更容易忘记取消订阅

推荐原则：**优先用有明确范围的 C# event 或 ScriptableObject Channel，只有在真正需要跨系统、跨层级通信时才用 Event Bus**。

---

## 常见陷阱

### 陷阱一：忘记取消订阅导致内存泄漏

```csharp
// 这是最常见的内存泄漏来源之一
public class UIPanel : MonoBehaviour
{
    void Start()
    {
        // 订阅了，但没有在 OnDisable 里取消
        PlayerCombat.OnEnemyKilled += UpdateKillCounter;
    }

    // 当这个 UIPanel 被销毁，PlayerCombat.OnEnemyKilled 仍然持有它的引用
    // UIPanel 不会被 GC，每次触发事件还会试图调用已销毁对象的方法
}
```

解决：永远在 `OnDisable` 里取消订阅，永远成对出现。

### 陷阱二：在事件处理过程中修改订阅列表

```csharp
// 在处理事件时取消订阅（或添加新订阅）可能导致迭代器失效
void HandleKill(EnemyKilledEventArgs args)
{
    // 假设某个条件下要取消订阅自己
    if (isCompleted)
        PlayerCombat.OnEnemyKilled -= HandleKill; // 在迭代中修改委托列表
    // C# 委托是值类型快照，这里其实是安全的，但要理解其行为
}
```

C# 的委托调用是快照式的（Delegate.Combine 返回新委托），所以在迭代中修改是安全的，但行为可能不直觉——本次调用列表不变，下次才生效。

### 陷阱三：事件参数被意外修改

```csharp
// 如果事件参数是引用类型且可变，订阅方可能修改它
public class EnemyKilledArgs
{
    public int damage; // 可变字段
}

// 第一个订阅方改了 damage
void Handler1(EnemyKilledArgs args) { args.damage *= 2; }

// 第二个订阅方看到的 damage 已经被改过了
void Handler2(EnemyKilledArgs args) { Debug.Log(args.damage); }
```

解决：事件参数用 `readonly struct` 或者字段全部只读。

---

## 小结

| 实现方式 | 适用场景 | 主要优势 | 主要劣势 |
|---|---|---|---|
| C# event（委托） | 同程序集内，性能敏感 | 快、类型安全 | 需要引用发布方类型 |
| UnityEvent | 策划/TA 配置，Inspector 可见 | 无代码连接 | 运行时慢，连接容易断 |
| ScriptableObject Channel | 跨场景/跨 Prefab | 完全解耦，可调试 | 需要创建资产文件 |
| Event Bus | 跨系统全局通信 | 极度解耦 | 调试困难，泄漏风险高 |

Observer 模式是游戏系统解耦的核心工具，几乎每个系统之间的通信都可以（应该）用它来处理。记住：**订阅要配对取消，参数要只读**，其他问题都好解决。
