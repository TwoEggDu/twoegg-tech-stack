---
title: "Unity Dedicated Server：构建配置、启动参数与服务器专属逻辑"
slug: "game-backend-ded-srv-02-unity-ds"
date: "2026-04-04"
description: "Unity DS 构建和普通 Player 构建的本质区别，UNITY_SERVER 宏的正确用法，以及哪些初始化代码会在 DS 上静默崩溃。"
tags:
  - "游戏后端"
  - "Dedicated Server"
  - "Unity"
  - "构建配置"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 22
weight: 3022
---

# Unity Dedicated Server：构建配置、启动参数与服务器专属逻辑

## 问题空间：一套代码库，两种运行环境

你的 Unity 项目已经有一套完整的游戏逻辑。现在需要把它跑在服务器上——无窗口、无 GPU、无本地玩家。

最直接的做法是什么？很多人第一反应是"复制一份项目，删掉渲染相关的东西"。但这意味着两套代码库，任何逻辑修改都要同步两处，维护代价极高。

正确的做法是利用 Unity 的 **Server Build** 编译目标和 **条件编译宏**，让同一套代码在不同的构建目标下编译出不同的产物——客户端包含完整渲染，DS 包含纯逻辑。

但在进入具体操作之前，我们需要先理解 Unity DS 构建和普通 Player 构建在底层有什么本质区别。

## 抽象模型：Server Build 和 Player Build 的差异层次

两种构建的差异不只是"有没有渲染"这么简单，它们的差异存在于多个层次：

**运行时模块层**：Unity 的运行时是模块化的。Player Build 包含渲染模块（RenderPipeline）、音频模块（Audio）、输入模块（InputSystem）。Server Build 在构建阶段就剔除了这些模块，IL2CPP 或 Mono 运行时里根本就不包含这些代码。这不是靠 `#if` 条件编译实现的，而是构建系统在打包阶段直接排除了这些原生库。

**脚本层**：通过 `UNITY_SERVER` 宏，你的 C# 代码可以在编译期区分"服务器构建"和"客户端构建"，从而排除掉不应该在服务器上运行的代码路径。

**资产层**：Server Build 不打包贴图的高分辨率 mip、不打包音频 Clip、不打包某些 Mesh 的 LOD——这些资产对纯逻辑服务器没有任何价值，打包进去只会增大可执行文件体积。

**初始化顺序层**：Unity 在 Server Build 模式下不会初始化 AudioListener、不会创建渲染上下文、不会尝试连接图形驱动。这个差异直接影响 `Awake`/`Start` 阶段哪些 API 可以安全调用。

## 具体实现：配置 Server Build

### 构建目标设置

在 Unity 2021 LTS 之后，Server Build 是一个独立的构建目标，不再需要通过 `-batchmode -nographics` 启动参数模拟无头模式。

在 Build Settings 中：
1. 选择目标平台（Linux x86_64 是最常见的 DS 平台）
2. 勾选 **Dedicated Server** 选项（或在 Target 下拉里选择对应的 Server 构建目标）

Unity 2023+ 的 Build Profiles 功能允许你在项目中保存多个构建配置，这对于"同一项目需要输出客户端包 + 服务端包"的场景很方便：创建两个 Profile，一个 Client，一个 Server，分别配置。

从命令行触发 Server Build：

```bash
# Unity 命令行触发 Linux Server 构建
Unity \
  -batchmode \
  -quit \
  -projectPath /path/to/project \
  -buildTarget LinuxHeadlessSimulation \
  -executeMethod BuildScript.BuildServer \
  -logFile /tmp/build.log
```

注意 `-buildTarget LinuxHeadlessSimulation` 这个参数——这是 Unity 2021+ 的 Linux Server 目标名称。旧版本用 `-buildTarget Linux64` 配合 `-headless` 来模拟，行为不完全等价。

### UNITY_SERVER 宏的正确使用方式

`UNITY_SERVER` 是 Unity 在编译 Server Build 时自动定义的预处理宏。它的作用域是整个编译期，不是运行期。

**正确用法——排除不应在 DS 上编译的代码**：

```csharp
public class PlayerController : MonoBehaviour
{
    private Rigidbody _rb;

#if !UNITY_SERVER
    private Animator _animator;
    private AudioSource _footstepAudio;
#endif

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

#if !UNITY_SERVER
        _animator = GetComponent<Animator>();
        _footstepAudio = GetComponent<AudioSource>();
#endif
    }

    private void Update()
    {
#if !UNITY_SERVER
        // 客户端：从本地输入读取操作
        HandleLocalInput();
        UpdateAnimation();
#else
        // 服务器：从网络输入包处理操作
        ProcessNetworkInputs();
#endif
    }
}
```

**常见错误用法——把宏当运行期开关用**：

```csharp
// 错误示范：UNITY_SERVER 是编译期宏，不是运行期变量
// 以下代码在语法上合法，但概念上是错的
if (isServer)  // 这是运行期判断，不是编译期宏
{
    // ...
}
```

编译期宏 `UNITY_SERVER` 和运行期判断 `NetworkManager.Singleton.IsServer` 是两个不同层次的东西：

- `UNITY_SERVER`：决定这段代码是否被编译进 IL。Server Build 产物里根本不存在 `#if !UNITY_SERVER` 包裹的代码，哪怕你去反编译也找不到。
- `NetworkManager.Singleton.IsServer`：运行期判断，当前进程是否在充当服务器角色。即使在非 Server Build 的 Listen Server 模式下也可以为 `true`。

两者结合使用才是最健壮的模式：
- 用 `UNITY_SERVER` 宏在编译期排除绝对不需要的代码（渲染、音频）
- 用运行期判断处理"同一套代码在服务端和客户端有不同行为"的逻辑

### Player Settings 中的资产剥离

在 Project Settings > Player 中，Server Build 模式下可以配置：

- **Texture Compression**：服务器可以使用最低压缩（甚至不打包某些贴图组）
- **Strip Engine Code**：开启后会移除未使用的引擎代码，Server Build 默认可以移除大量渲染相关代码
- **Managed Stripping Level**：设置为 High 可以进一步精简服务器二进制体积

一个典型的优化结果：一个 500MB 的 Unity 客户端包，对应的 Linux Server Build 可能只有 80-120MB。

## DS 上不可用的 Unity 系统

这部分是最容易踩坑的地方，完整记录。

### AudioListener 和 AudioSource

在 Server Build 中，AudioListener 组件不会被初始化，尝试调用 `AudioSource.Play()` 会静默失败（不报错，不崩溃，只是什么都不发生）。但如果你的代码在 `Awake` 里依赖 `AudioListener.volume` 来判断音频状态，可能会得到意外的默认值。

解决方案：用 `UNITY_SERVER` 宏直接排除所有音频相关代码。

### Screen 和 Display

`Screen.width`、`Screen.height`、`Screen.SetResolution` 在 Server Build 上返回值未定义（通常是 0 或一个无意义的值）。如果有代码用 `Screen.width / 2` 做屏幕中心计算，而这段代码又意外地在服务端运行，你会得到除以零的异常。

### 某些 Physics 事件

`OnCollisionEnter` 和 `OnTriggerEnter` 在 DS 上是可用的——物理系统在 DS 上是完整运行的。**但 `OnParticleCollision` 不可用**，因为粒子系统在 Server Build 中被剥离了。

这个细节在游戏逻辑里经常被忽略：如果你的伤害判定依赖 `OnParticleCollision`（比如火焰喷射器用粒子碰撞判断命中），这套逻辑在 DS 上根本不会触发。需要换成基于物理射线或重叠检测的服务端友好判断方式。

### Cursor 和 Application.targetFrameRate

`Cursor.lockState`、`Cursor.visible` 在 DS 上没有意义，调用不报错但也不起任何作用。

`Application.targetFrameRate`：**这个 API 在 DS 上是可用的，而且很重要**——你需要主动设置服务器的 Tick 率。如果不设置，Unity 的 DS 构建默认会尽可能快地运行（可能跑到数百 FPS），导致无意义的 CPU 消耗。

## NetworkManager 在 DS 模式下的初始化顺序

以 Unity Netcode for GameObjects（NGO）为例，DS 模式下的 NetworkManager 初始化有几个关键注意点。

### 启动时序

DS 进程启动后，NetworkManager 不会自动启动——它等待你的代码调用 `NetworkManager.Singleton.StartServer()`。这意味着你需要有一段"服务器引导代码"，在场景加载完成后执行：

```csharp
public class ServerBootstrapper : MonoBehaviour
{
    private void Start()
    {
#if UNITY_SERVER
        ParseCommandLineArgs();
        StartDedicatedServer();
#endif
    }

    private void StartDedicatedServer()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", (ushort)_port);

        NetworkManager.Singleton.StartServer();
        Debug.Log($"[Server] Started on port {_port}");
    }
}
```

注意 `"0.0.0.0"` 而不是 `"127.0.0.1"`——服务器需要监听所有网络接口，否则外部客户端无法连接。这个小错误会让你在本地测试时正常（因为本地客户端走 loopback），但在部署到云服务器后彻底失效。

### 场景管理和 NetworkManager 的冲突

NGO 的 NetworkSceneManager 在 DS 模式下默认控制场景加载。但 DS 通常需要在启动参数里指定要加载的地图，而不是硬编码在代码里。正确流程：

1. DS 进程启动，加载一个轻量的"Bootstrap Scene"（只包含 NetworkManager）
2. Bootstrap 代码解析命令行参数，获取地图名
3. 调用 `NetworkManager.Singleton.SceneManager.LoadScene(mapName, LoadSceneMode.Single)`
4. 场景加载完成后，开始接受客户端连接

如果反过来——先连接、后加载场景——会导致客户端收到场景切换事件时状态不一致。

## 启动参数解析

DS 进程通常通过命令行参数接收运行配置。Unity 没有内置的参数解析工具，需要自己实现。

一个稳定可用的最小实现：

```csharp
public class ServerConfig
{
    public ushort Port { get; private set; } = 7777;
    public string MapName { get; private set; } = "MainMap";
    public string RoomId { get; private set; } = "";
    public string ConfigFilePath { get; private set; } = "";

    public static ServerConfig ParseFromCommandLine()
    {
        var config = new ServerConfig();
        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "-port":
                    if (ushort.TryParse(args[i + 1], out var port))
                        config.Port = port;
                    break;
                case "-map":
                    config.MapName = args[i + 1];
                    break;
                case "-roomId":
                    config.RoomId = args[i + 1];
                    break;
                case "-config":
                    config.ConfigFilePath = args[i + 1];
                    break;
            }
        }

        return config;
    }
}
```

启动命令示例：

```bash
./GameServer \
  -batchmode \
  -nographics \
  -port 7778 \
  -map Arena01 \
  -roomId room_abc123 \
  -config /etc/gameserver/config.json \
  -logFile /var/log/gameserver/server.log
```

`-batchmode` 和 `-nographics` 是 Unity 的内置参数。即使是 Server Build（已经不含渲染），加上这两个参数也是好习惯——它们会抑制一些旧版本 Unity 可能触发的 GUI 初始化尝试。

## 服务器专属 MonoBehaviour 的生命周期管理

在 DS 上，MonoBehaviour 的生命周期（`Awake` → `Start` → `Update`）本身是正常工作的，但有几个需要注意的点。

### 避免在 Awake 里访问可能未初始化的网络状态

`Awake` 在 NetworkManager 启动之前就会被调用。如果在 `Awake` 里访问 `NetworkManager.Singleton.IsServer`，会在 NetworkManager 还没有执行 `StartServer()` 的情况下得到 `false`，导致逻辑错误。

正确做法：把依赖网络状态的初始化逻辑放在 `NetworkManager.OnServerStarted` 回调里，或者使用 `NetworkBehaviour` 的 `OnNetworkSpawn` 代替。

### DS 上的 Update 频率控制

由于 `Application.targetFrameRate` 控制着 DS 的 Tick 率，你需要在 `ServerBootstrapper.Start()` 里显式设置：

```csharp
// 设置服务器 Tick 率为 30Hz（射击游戏通常用 30-64Hz）
Application.targetFrameRate = 30;
QualitySettings.vSyncCount = 0; // DS 上必须关闭 VSync
```

如果不关闭 VSync，`QualitySettings.vSyncCount` 默认值可能导致 DS 以屏幕刷新率（60Hz）为上限，无法突破到你期望的服务端逻辑帧率。更关键的是，某些 Linux 无头环境根本不存在"屏幕"，VSync 行为会变得不可预测。

## 工程边界

**Unity DS 构建目前在 Unity 2021.2+ 才稳定可用**。更早的版本需要用 `-headless` 标志的特殊构建，行为差异较大，不建议在新项目里使用。

**Server Build 的 IL Stripping 可能误删代码**：如果你通过反射（`Type.GetType()`）动态加载某些类，而这些类没有被显式引用，IL Stripping 可能把它们从服务器二进制里移除，导致运行时找不到类型。解决方案是添加 `link.xml` 文件来保留需要的程序集。

**NGO 不是唯一选择**：FishNet、Mirror 等网络中间件对 DS 模式的支持程度和 API 设计有差异，选型时需要验证它们的 Server-only 构建流程。

## 最短结论

Unity Server Build 和 Player Build 的本质区别在于：**运行时模块层面就已经剔除了渲染、音频、输入的原生库**，而不是靠运行时判断绕过。

`UNITY_SERVER` 宏是你的编译期防火墙——把所有"属于客户端表现层"的代码用它包裹起来，让 Server Build 的二进制彻底不包含这些代码。

NetworkManager 在 DS 模式下不自动启动，启动时序和命令行参数解析是你的第一道工程关卡。搞清楚这两件事，后面的问题都是可调试的。
