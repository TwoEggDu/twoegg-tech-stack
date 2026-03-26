---
date: "2026-03-26"
title: "软件工程基础 07｜依赖倒置原则（DIP）：依赖抽象而非具体"
description: "DIP 是 SOLID 中最重要也最难理解的一条，是所有软件架构的根基。这篇从游戏系统的实际案例出发，解释什么是依赖倒置、为什么它能让架构变得灵活，以及依赖注入的多种实现方式。"
slug: "solid-sw-07-dip"
weight: 713
tags:
  - 软件工程
  - SOLID
  - DIP
  - 依赖倒置
  - 依赖注入
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> **DIP（Dependency Inversion Principle）有两条规则：**
> 1. **高层模块不应该依赖低层模块，两者都应该依赖抽象。**
> 2. **抽象不应该依赖细节，细节应该依赖抽象。**
>
> 简单说：**依赖接口（抽象），不要依赖具体实现。**

DIP 是 SOLID 五条原则里影响最深远的一条。前几条原则（SRP、OCP、LSP、ISP）告诉你怎么设计单个类和接口，DIP 告诉你**模块之间怎么连接**——这决定了整个系统的架构弹性。

---

## 理解"依赖"

在代码里，"A 依赖 B"意味着：A 的代码里出现了对 B 的直接引用。

```csharp
// PlayerController 依赖 MySQLDatabase（具体实现）
public class PlayerController
{
    private MySQLDatabase database = new MySQLDatabase(); // 直接依赖具体类

    public void SaveProgress()
    {
        database.Save(GetCurrentData()); // 依赖 MySQLDatabase 的具体 API
    }
}
```

如果有一天你想换成 SQLite 或者云存档，你必须：
1. 修改 `PlayerController`（违反 OCP）
2. 同时测试 `PlayerController` 时，必须有真实的 MySQL 环境

---

## "高层"和"低层"是什么

**低层模块（Low-level）**：具体的技术实现细节——文件读写、数据库操作、网络请求、音频播放。这些是"怎么做"的细节。

**高层模块（High-level）**：业务逻辑、游戏规则——"玩家死亡后存档"、"达成成就时发通知"。这些是"做什么"的策略。

传统的依赖方向（违反 DIP）：

```
PlayerController（高层）→ MySQLDatabase（低层）
                        → FileSystem（低层）
                        → NetworkManager（低层）
```

高层依赖低层意味着：低层的任何变化，都会向上传播影响到高层。低层是高层的"控制者"。

DIP 要求的依赖方向：

```
PlayerController（高层）→ IDataStorage（抽象）
MySQLDatabase（低层）   → IDataStorage（抽象）
FileStorage（低层）     → IDataStorage（抽象）
CloudStorage（低层）    → IDataStorage（抽象）
```

高层和低层都依赖同一个抽象。高层定义它需要什么（接口），低层去实现这个接口。**控制权反转了**——现在是高层在决定低层的规格，而不是低层在决定高层能用什么。

---

## 游戏中的 DIP 案例：成就系统

### 违反 DIP 的版本

```csharp
// 各种系统直接互相依赖具体类——高度耦合
public class PlayerCombat : MonoBehaviour
{
    private AchievementManager achievementManager; // 直接依赖具体类
    private NotificationUI notificationUI;          // 直接依赖具体类
    private AnalyticsTracker analyticsTracker;      // 直接依赖具体类

    void Start()
    {
        achievementManager = FindObjectOfType<AchievementManager>();
        notificationUI = FindObjectOfType<NotificationUI>();
        analyticsTracker = FindObjectOfType<AnalyticsTracker>();
    }

    public void OnKillEnemy(Enemy enemy)
    {
        killCount++;

        // 战斗系统直接知道成就系统的存在
        achievementManager.CheckKillAchievements(killCount, enemy.type);

        // 战斗系统直接知道通知 UI 的存在
        if (killCount == 100)
            notificationUI.ShowAchievement("百人斩");

        // 战斗系统直接知道数据分析的存在
        analyticsTracker.TrackEvent("enemy_killed", enemy.type.ToString());
    }
}
```

这个设计的问题：

1. `PlayerCombat` 依赖了三个低层系统，任何一个变化都会影响它
2. 测试 `PlayerCombat` 的战斗逻辑，必须初始化成就、UI、数据分析三套系统
3. 想换掉通知 UI 的实现（比如从 Unity UI 换成 TextMeshPro），需要改 `PlayerCombat`
4. 想关掉数据分析（有些市场不允许），需要改 `PlayerCombat` 而不仅仅是配置

### DIP 的正确设计

**第一步：定义抽象**

```csharp
// 高层定义它需要的接口规格（不关心谁实现）
public interface IGameEventListener
{
    void OnEnemyKilled(EnemyKilledEvent evt);
}

[System.Serializable]
public class EnemyKilledEvent
{
    public EnemyType enemyType;
    public int totalKillCount;
    public Vector3 position;
}
```

**第二步：高层依赖抽象，不依赖具体**

```csharp
// PlayerCombat 只依赖抽象接口
public class PlayerCombat : MonoBehaviour
{
    // 通过注入而不是查找获得依赖
    private List<IGameEventListener> eventListeners = new();

    public void AddListener(IGameEventListener listener)
    {
        eventListeners.Add(listener);
    }

    public void OnKillEnemy(Enemy enemy)
    {
        killCount++;

        // 战斗系统只知道"有一些监听者"，不知道监听者是什么
        var evt = new EnemyKilledEvent
        {
            enemyType = enemy.Type,
            totalKillCount = killCount,
            position = enemy.transform.position
        };

        foreach (var listener in eventListeners)
            listener.OnEnemyKilled(evt);
    }

    private int killCount = 0;
}
```

**第三步：低层实现抽象**

```csharp
// 成就系统：实现接口，响应事件
public class AchievementManager : MonoBehaviour, IGameEventListener
{
    public void OnEnemyKilled(EnemyKilledEvent evt)
    {
        if (evt.totalKillCount == 100)
            UnlockAchievement("百人斩");

        if (evt.enemyType == EnemyType.Boss)
            UnlockAchievement("猎杀传说");
    }

    void UnlockAchievement(string name) { /* 解锁成就逻辑 */ }
}

// 数据分析：实现接口
public class AnalyticsTracker : MonoBehaviour, IGameEventListener
{
    public void OnEnemyKilled(EnemyKilledEvent evt)
    {
        TrackEvent("enemy_killed", new Dictionary<string, object>
        {
            {"type", evt.enemyType.ToString()},
            {"count", evt.totalKillCount}
        });
    }

    void TrackEvent(string name, Dictionary<string, object> properties) { /* ... */ }
}

// 通知 UI：实现接口
public class NotificationUI : MonoBehaviour, IGameEventListener
{
    public void OnEnemyKilled(EnemyKilledEvent evt)
    {
        if (evt.totalKillCount % 10 == 0)
            ShowToast($"已击杀 {evt.totalKillCount} 个敌人！");
    }

    void ShowToast(string message) { /* 显示通知 UI */ }
}
```

**第四步：在外部装配（组装依赖）**

```csharp
// 由一个专门的"装配者"把依赖关系连接起来
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private AchievementManager achievementManager;
    [SerializeField] private AnalyticsTracker analyticsTracker;
    [SerializeField] private NotificationUI notificationUI;

    void Start()
    {
        // 把所有监听者注入给战斗系统
        playerCombat.AddListener(achievementManager);
        playerCombat.AddListener(analyticsTracker);
        playerCombat.AddListener(notificationUI);
    }
}
```

现在：
- `PlayerCombat` 完全不知道成就、分析、UI 的存在，只知道"有一些 `IGameEventListener`"
- 增加新的响应（比如新增音效系统），只需要新写一个 `IGameEventListener` 的实现，注册进来即可
- 去掉数据分析（合规需求），只需要在 `GameBootstrap` 里不注册 `AnalyticsTracker`，`PlayerCombat` 代码不需要改
- 测试 `PlayerCombat` 战斗逻辑，只需要 Mock 一个 `IGameEventListener`，不需要真实的成就和 UI 系统

---

## 依赖注入（Dependency Injection）

DIP 规定了"依赖抽象"，但没有规定**谁来负责把具体实现注入进来**。这就是依赖注入（DI）解决的问题。

依赖注入有三种方式：

### 方式一：构造函数注入（最推荐，明确依赖关系）

```csharp
public class DamageCalculator
{
    private readonly ICriticalHitSystem critSystem;
    private readonly IElementalSystem elementSystem;

    // 依赖在构造时就必须提供，不能存在"半初始化"状态
    public DamageCalculator(ICriticalHitSystem critSystem, IElementalSystem elementSystem)
    {
        this.critSystem = critSystem;
        this.elementSystem = elementSystem;
    }

    public int Calculate(int baseDamage, AttackContext context)
    {
        float critMultiplier = critSystem.GetCritMultiplier(context);
        float elementMultiplier = elementSystem.GetElementMultiplier(context);
        return (int)(baseDamage * critMultiplier * elementMultiplier);
    }
}
```

### 方式二：属性/字段注入（Unity 里最常用，通过 Inspector 赋值）

```csharp
public class PlayerController : MonoBehaviour
{
    // 通过 Inspector 赋值——Unity 负责"注入"
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;

    // stats、movement、combat 都可以是接口或基类类型
    // 但 Unity Inspector 需要 MonoBehaviour，所以通常是具体类型
    // 可以在代码里通过接口使用它们
}
```

### 方式三：方法注入（按需注入，适合临时依赖）

```csharp
// 不是持久的依赖，而是每次调用时传入
public class PathFinder
{
    // 不存储 INavigationGrid，每次查找路径时由调用者提供
    public List<Vector3> FindPath(Vector3 start, Vector3 end, INavigationGrid grid)
    {
        return grid.AStar(start, end);
    }
}
```

---

## Service Locator 与 DIP

在 Unity 中，另一种常见的 DIP 实现是**Service Locator 模式**：

```csharp
// 服务注册表——知道"有什么服务"，但不耦合具体实现
public static class ServiceLocator
{
    private static Dictionary<Type, object> services = new();

    public static void Register<T>(T service)
    {
        services[typeof(T)] = service;
    }

    public static T Get<T>()
    {
        if (services.TryGetValue(typeof(T), out object service))
            return (T)service;
        throw new Exception($"Service {typeof(T).Name} not registered.");
    }
}

// 启动时注册具体实现
void Start()
{
    ServiceLocator.Register<IAudioSystem>(new FMODAudioSystem());
    ServiceLocator.Register<ISaveSystem>(new CloudSaveSystem());
    ServiceLocator.Register<IAnalytics>(new FirebaseAnalytics());
}

// 使用时获取抽象接口，不关心具体实现
public class PlayerController
{
    void OnDeath()
    {
        // 通过 ServiceLocator 获取——只知道接口，不知道具体实现
        ServiceLocator.Get<ISaveSystem>().Save(GetCurrentData());
        ServiceLocator.Get<IAnalytics>().Track("player_death");
    }
}
```

Service Locator 的优缺点在系列七·B（设计模式篇）会详细讨论，这里只需要知道：它是 DIP 的一种实现手段。

---

## DIP 是架构设计的核心思想

DIP 不只是一条代码规范，它是**分层架构**的理论基础：

```
游戏逻辑层（高层）
    ↓ 依赖
抽象接口层（稳定）
    ↑ 依赖（实现）
具体技术层（低层）
  - Unity 引擎 API
  - 第三方 SDK
  - 数据库/网络
```

游戏逻辑层不应该直接调用 Unity 的 API 或者第三方 SDK。当你想换掉 Unity 换成另一个引擎，或者换掉某个 SDK，只需要替换最低层的实现，中间的抽象层和高层的游戏逻辑完全不需要改变。

这听起来很理想化，现实中很难完全做到，但这个方向是正确的。即使只做到 80%，可维护性也会显著提升。

---

## 小结

- **DIP 的核心**：高层模块和低层模块都依赖抽象（接口），不互相直接依赖
- **为什么重要**：它让系统的各层之间可以独立变化，真正实现低耦合
- **实现方式**：定义接口（高层写规格）+ 依赖注入（把具体实现传进来）
- **依赖注入三种方式**：构造函数注入、属性注入（Unity Inspector）、方法注入
- **架构意义**：DIP 是分层架构的理论基础，让"换底层技术不影响上层业务逻辑"成为可能

DIP 是 SOLID 最难的一条，也是回报最大的一条。掌握了它，你才真正理解了"低耦合"究竟是如何在代码层面实现的。
