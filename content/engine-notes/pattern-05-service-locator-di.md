---
title: "游戏编程设计模式 05｜Service Locator 与依赖注入：全局服务访问的两种策略"
description: "游戏里有些服务需要全局访问（音效、存档、日志）。Service Locator 和依赖注入是两种解决这个问题的方式，各有取舍。这篇对比它们的实现、优缺点，以及在 Unity 项目里的选择策略。"
slug: "pattern-05-service-locator-di"
weight: 735
tags:
  - 软件工程
  - 设计模式
  - Service Locator
  - 依赖注入
  - 游戏架构
series: "游戏编程设计模式"
---

> 游戏里有一类服务是"几乎所有地方都需要"的：音效系统、日志系统、存档系统、时间系统。
>
> 怎么让代码方便地访问这些全局服务，同时不把整个项目变成一团全局变量的耦合网，是这篇要解决的问题。

---

## 问题的起点：全局服务的访问困境

一个最直接的解法是单例（Singleton）：

```csharp
// 单例模式：全局唯一实例，到处都能访问
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySound(string soundName) { /* ... */ }
}

// 使用：任何地方都能直接调用
AudioManager.Instance.PlaySound("attack");
```

单例的问题在 DIP 那篇提过：代码直接依赖了 `AudioManager` 这个具体类。换音频中间件（从 Unity Audio 换到 FMOD）需要修改所有调用处；写单元测试时无法替换成 Mock。

但单例的便利性是真实的——它简单、直接、不需要手动传递引用。

Service Locator 和依赖注入是两种"保留便利性，同时解决耦合问题"的方案。

---

## 方案一：Service Locator

Service Locator 是一个全局注册表：你向它注册"我提供某个服务的具体实现"，其他地方向它查询"我需要某个服务"。

关键：注册和查询都**通过接口**，而不是通过具体类型。

```csharp
// 服务定位器（注册表）
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new();

    // 注册：提供一个接口的具体实现
    public static void Register<TInterface>(TInterface service)
    {
        services[typeof(TInterface)] = service;
    }

    // 查询：获取一个接口的当前实现
    public static TInterface Get<TInterface>()
    {
        if (services.TryGetValue(typeof(TInterface), out object service))
            return (TInterface)service;

        throw new InvalidOperationException(
            $"Service '{typeof(TInterface).Name}' not registered. " +
            $"Did you forget to call ServiceLocator.Register?");
    }

    // 安全查询：返回 null 而不是抛异常
    public static bool TryGet<TInterface>(out TInterface service)
    {
        if (services.TryGetValue(typeof(TInterface), out object obj))
        {
            service = (TInterface)obj;
            return true;
        }
        service = default;
        return false;
    }

    // 测试时替换实现（Mock）
    public static void Override<TInterface>(TInterface replacement)
    {
        services[typeof(TInterface)] = replacement;
    }

    public static void Clear() => services.Clear();
}
```

定义服务接口和具体实现：

```csharp
// 接口（稳定，不会改变）
public interface IAudioService
{
    void PlaySfx(string soundId, Vector3 position);
    void PlayMusic(string trackId);
    void StopMusic();
    void SetMasterVolume(float volume);
}

public interface ISaveService
{
    void Save<T>(string key, T data);
    T Load<T>(string key, T defaultValue = default);
}

// 具体实现一：Unity 原生音频
public class UnityAudioService : MonoBehaviour, IAudioService
{
    public void PlaySfx(string soundId, Vector3 position) { /* 用 AudioSource.PlayClipAtPoint */ }
    public void PlayMusic(string trackId) { /* ... */ }
    public void StopMusic() { /* ... */ }
    public void SetMasterVolume(float volume) { AudioListener.volume = volume; }
}

// 具体实现二：FMOD（日后切换时，游戏代码不需要改）
public class FMODAudioService : MonoBehaviour, IAudioService
{
    public void PlaySfx(string soundId, Vector3 position) { /* 用 FMOD API */ }
    public void PlayMusic(string trackId) { /* ... */ }
    public void StopMusic() { /* ... */ }
    public void SetMasterVolume(float volume) { /* FMOD 的音量控制 */ }
}

// 测试用 Mock
public class NullAudioService : IAudioService
{
    public void PlaySfx(string soundId, Vector3 position) { } // 什么都不做
    public void PlayMusic(string trackId) { }
    public void StopMusic() { }
    public void SetMasterVolume(float volume) { }
}
```

在游戏启动时注册具体实现：

```csharp
// 启动器（Bootstrapper）：负责把所有服务注册进 ServiceLocator
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private UnityAudioService audioService;
    [SerializeField] private LocalSaveService saveService;

    void Awake()
    {
        ServiceLocator.Register<IAudioService>(audioService);
        ServiceLocator.Register<ISaveService>(saveService);
    }
}
```

使用时：

```csharp
public class PlayerCombat : MonoBehaviour
{
    public void OnKillEnemy(Enemy enemy)
    {
        // 通过接口访问服务，不依赖具体类型
        ServiceLocator.Get<IAudioService>().PlaySfx("enemy_death", enemy.transform.position);
    }
}

// 测试时：替换为 Mock，不需要真实的音频系统
[SetUp]
public void Setup()
{
    ServiceLocator.Register<IAudioService>(new NullAudioService());
}
```

---

## Service Locator 的优缺点

**优点**：
- 调用方便，和单例一样只需一行代码
- 通过接口解耦，具体实现可以替换
- 测试时可以注入 Mock
- 支持多个平台使用不同实现（PC 用 FMOD，移动端用 Unity Audio）

**缺点**：

**隐式依赖**：调用方的依赖关系不在代码里显式写出来，只有运行到那行代码时才能发现。

```csharp
// 读 PlayerController 的代码，看不出它依赖了 IAudioService 和 ISaveService
// 只有实际运行时才知道
public class PlayerController : MonoBehaviour
{
    void Update()
    {
        ServiceLocator.Get<IAudioService>(); // 依赖隐藏在函数调用里
    }
}
```

**注册顺序依赖**：如果在注册之前调用 `Get`，会抛异常。需要保证 Bootstrapper 最先运行（通过 Unity 的 Script Execution Order 或者把 Bootstrapper 放在第一个 Awake 里执行）。

**全局状态**：Service Locator 本质上还是全局状态，多线程环境下需要额外的线程安全保护。

---

## 方案二：依赖注入（DI）

DIP 那篇已经详细讲过。这里从 Service Locator 的角度来对比：

依赖注入不是用"查询"来获取依赖，而是在**对象创建时把依赖传进来**。

```csharp
// Service Locator 方式：内部查询
public class PlayerController : MonoBehaviour
{
    void OnDeath()
    {
        ServiceLocator.Get<ISaveService>().Save("checkpoint", currentData);
        // 依赖是隐式的，从外面看不出来
    }
}

// 依赖注入方式：外部传入
public class PlayerController : MonoBehaviour
{
    private ISaveService saveService; // 依赖是显式的，可以从代码里看出来

    public void Inject(ISaveService saveService) // 或通过构造函数
    {
        this.saveService = saveService;
    }

    void OnDeath()
    {
        saveService.Save("checkpoint", currentData);
    }
}

// 装配器负责连接
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private LocalSaveService saveService;

    void Awake()
    {
        player.Inject(saveService);
    }
}
```

**依赖注入在 Unity 里的实践方式**（不用 DI 框架）：

```csharp
// 方式 1：Inspector 直接赋值（最简单）
public class PlayerController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource; // Unity Inspector 注入

    // 缺点：绑定到具体类型，无法通过接口访问
}

// 方式 2：[SerializeField] 配合接口（Unity 的限制：Inspector 不支持接口直接赋值）
// 需要用 [SerializeReference] 或者中间类来解决
public class PlayerController : MonoBehaviour
{
    [SerializeReference] private IAudioService audioService; // Unity 2019.3+ 支持
}

// 方式 3：Inject 方法（最灵活，适合程序化装配）
public class PlayerController : MonoBehaviour
{
    private IAudioService audio;
    private ISaveService save;

    public void Initialize(IAudioService audio, ISaveService save)
    {
        this.audio = audio;
        this.save = save;
    }
}
```

---

## 用 DI 框架（VContainer / Zenject）

大型项目通常会引入专门的 DI 框架来管理依赖关系，避免手动写大量装配代码。

**VContainer**（轻量、现代，推荐 Unity 项目使用）：

```csharp
// 注册容器
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册服务（接口→实现）
        builder.Register<IAudioService, UnityAudioService>(Lifetime.Singleton);
        builder.Register<ISaveService, LocalSaveService>(Lifetime.Singleton);

        // 注册 MonoBehaviour（从场景中找）
        builder.RegisterComponentInHierarchy<PlayerController>();
    }
}

// 使用时，VContainer 自动注入
public class PlayerController : MonoBehaviour
{
    private readonly IAudioService audio;
    private readonly ISaveService save;

    // VContainer 会自动找到 IAudioService 和 ISaveService 的实现并注入
    [Inject]
    public void Construct(IAudioService audio, ISaveService save)
    {
        this.audio = audio;
        this.save = save;
    }
}
```

---

## Service Locator vs 依赖注入：怎么选

| | Service Locator | 依赖注入 |
|---|---|---|
| 代码复杂度 | 低，调用简单 | 中，需要装配器或 DI 框架 |
| 依赖可见性 | 隐式（看代码不知道依赖什么） | 显式（构造函数/Inject 方法里能看到） |
| 可测试性 | 可以，但需要手动注册 Mock | 好，直接传 Mock 对象 |
| 适用规模 | 小到中型项目，快速开发 | 中到大型项目，长期维护 |
| Unity 集成 | 容易 | 需要了解 DI 框架或手写装配器 |

**实用建议**：

- 小项目（个人/2~3人）：Service Locator 就够了，别过度工程化
- 中型项目（5~15人）：Service Locator + 严格的接口规范，或者引入 VContainer
- 大型项目（15+人，多年开发）：DI 框架几乎是必须的，Zenject/VContainer 都是成熟选择

无论用哪种，最重要的是：**通过接口访问，而不是通过具体类型**。这是 DIP 的核心，Service Locator 和依赖注入只是实现这一点的两种手段。

---

## 小结

- **单例**：简单直接，但耦合具体实现，不可测试，不推荐用于有测试要求的系统
- **Service Locator**：解耦了接口和实现，全局访问便捷，但依赖是隐式的
- **依赖注入**：依赖显式，最可测试，需要装配代码（或 DI 框架）
- **选择原则**：按项目规模和团队情况选，小项目别过度工程化；无论哪种，都要通过接口而非具体类型来访问服务
