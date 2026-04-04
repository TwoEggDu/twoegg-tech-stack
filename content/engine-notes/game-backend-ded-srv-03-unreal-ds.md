---
title: "Unreal Dedicated Server：Cook、打包、启动流程与常见陷阱"
slug: "game-backend-ded-srv-03-unreal-ds"
date: "2026-04-04"
description: "Unreal DS 构建为什么比 Unity 复杂：Server Target 体系、Cook 流程、WITH_SERVER_CODE 宏，以及实际项目中最常踩的几个坑。"
tags:
  - "游戏后端"
  - "Dedicated Server"
  - "Unreal Engine"
  - "构建配置"
series: "游戏后端基础"
primary_series: "game-backend"
series_role: "article"
series_order: 23
weight: 3023
---

# Unreal Dedicated Server：Cook、打包、启动流程与常见陷阱

## 问题空间：Unreal DS 构建为什么更复杂

如果你刚从 Unity 转到 Unreal，第一次尝试构建 Dedicated Server 时，很可能会在以下某个地方停下来：

- 找不到"Server Build"按钮——Unreal 没有一个简单的复选框
- 编译成功了，但服务器进程启动后立刻崩溃，没有任何有意义的错误信息
- 服务器进程跑起来了，但地图加载失败，日志显示资产找不到
- Cook 了三次，每次都漏打了一些服务器需要的蓝图

这些问题有一个共同的根源：**Unreal 的 DS 构建不是一个选项，而是一套独立的构建体系**，它有自己的 Target 类型、自己的 Cook 流程、自己的配置文件。理解这套体系的设计逻辑，才能避免走弯路。

## 抽象模型：Unreal 的 Target 类型体系

Unreal 引擎用 **Build Target** 来区分不同的构建产物。每个项目在 `Source/` 目录下有若干个 `.Target.cs` 文件，每个文件对应一种构建目标。

标准的四种 Target 类型：

| Target 类型 | 用途 | 包含渲染 | 包含编辑器 |
|------------|------|---------|-----------|
| `TargetType.Game` | 玩家客户端 | 是 | 否 |
| `TargetType.Client` | 纯客户端（配合 DS 使用） | 是 | 否 |
| `TargetType.Server` | Dedicated Server | 否 | 否 |
| `TargetType.Editor` | 编辑器运行 | 是 | 是 |

`Game` 和 `Client` 的区别：`Game` Target 包含了既能作为客户端又能作为 Host 服务器运行的代码；`Client` Target 被明确裁剪为"只作为客户端"，不包含任何服务器逻辑——它的体积更小，但不能运行 Listen Server。

**DS 场景下推荐的配对方式**：`Client` Target（客户端）+ `Server` Target（专用服务器）。

一个典型项目的 Target 文件结构：

```
Source/
  MyGame.Target.cs          # Game Target（含服务器逻辑的完整客户端）
  MyGameClient.Target.cs    # Client Target（纯客户端，最小体积）
  MyGameServer.Target.cs    # Server Target（Dedicated Server）
  MyGameEditor.Target.cs    # Editor Target
```

## Server Target 的 Build.cs 配置

`MyGameServer.Target.cs` 的基本结构：

```csharp
public class MyGameServerTarget : TargetRules
{
    public MyGameServerTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Server;
        DefaultBuildSettings = BuildSettingsVersion.V2;

        // 关闭不需要的功能
        bUseLoggingInShipping = true;  // 生产环境保留日志
        bWithServerCode = true;        // 显式声明包含服务器代码

        ExtraModuleNames.AddRange(new string[] {
            "MyGame",
            "MyGameServer"  // 可选：服务器专属模块
        });
    }
}
```

对应的模块 `Build.cs` 里，可以通过判断 Target 类型来决定依赖哪些模块：

```csharp
// MyGame.Build.cs
public class MyGame : ModuleRules
{
    public MyGame(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(new string[] {
            "Core", "CoreUObject", "Engine", "InputCore",
            "OnlineSubsystem", "GameplayAbilities"
        });

        // 客户端专属依赖（渲染相关）
        if (Target.Type != TargetType.Server)
        {
            PublicDependencyModuleNames.AddRange(new string[] {
                "Renderer", "RenderCore", "Slate", "SlateCore"
            });
        }

        // 服务器专属依赖
        if (Target.Type == TargetType.Server || Target.Type == TargetType.Game)
        {
            PublicDependencyModuleNames.Add("OnlineSubsystemNull");
        }
    }
}
```

这里有个微妙点：**如果你的 Game Target 要支持 Listen Server，它需要同时包含客户端和服务器的依赖**。所以服务器相关模块通常加在 `Game` 和 `Server` 两种 Target 里，而渲染模块只加在非 `Server` Target 里。

## WITH_SERVER_CODE 宏的使用

`WITH_SERVER_CODE` 是 Unreal 的编译期宏，在 Server Target 和 Game Target 下为 `1`，在 Client Target 下为 `0`。

它和 `UE_SERVER` 宏的区别：

- `WITH_SERVER_CODE`：表示"这次编译包含服务器代码"，在 Server 和 Game Target 都为 true
- `UE_SERVER`：表示"这次编译是 Server Target"，只在 Server Target 为 true

实际使用中的惯用法：

```cpp
// 服务器权威逻辑，只在包含服务器代码的构建里编译
#if WITH_SERVER_CODE
void AMyGameMode::ValidatePlayerPosition(APlayerController* PC)
{
    // 位置校验逻辑
    // 这段代码在 Client Target 里根本不存在
}
#endif

// 只属于 Dedicated Server 的代码（不需要在 Game Target 的监听服务器模式下运行）
#if UE_SERVER
void AMyGameMode::InitializeHeadlessMode()
{
    // 初始化无头模式专属系统
    // 这段代码只在 Server Target 构建里存在
}
#endif
```

运行期判断和编译期宏同样需要区分：

```cpp
// 运行期判断：当前 NetMode 是否为专用服务器
if (GetNetMode() == NM_DedicatedServer)
{
    // 适用于 Game Target 在 DS 模式运行时的逻辑
}

// 编译期宏：比运行期判断效率更高，代码完全不存在于 Client 二进制
#if WITH_SERVER_CODE
    // ...
#endif
```

## Cook 流程：服务器需要哪些资产

这是 Unreal DS 里最容易出问题的环节。

### 什么是 Cook

Unreal 的资产（蓝图、关卡、材质等）在编辑器里是源格式，无法直接被运行时读取。Cook 的过程是把这些源格式资产转换成目标平台的运行时格式（.uasset / .umap 的 cooked 版本）。

**服务器 Cook 和客户端 Cook 的关键差异**：

| 资产类型 | 客户端需要 | 服务器需要 | 原因 |
|---------|-----------|-----------|------|
| 关卡（Map）| 是 | 是 | 服务器需要加载关卡初始化碰撞和 Actor |
| 蓝图逻辑 | 是 | 是（仅服务端部分）| 服务器运行 GameMode、GameState |
| 材质/Shader | 是 | 否 | 服务器不渲染 |
| 贴图（Texture）| 是 | 否（通常）| 服务器不渲染 |
| 骨骼网格体（SkeletalMesh）| 是 | 是（碰撞外形）| 服务器需要碰撞体，但不需要渲染 Mesh |
| 音频（Sound）| 是 | 否 | 服务器不播放音频 |
| 粒子系统（Niagara）| 是 | 否 | 纯表现效果 |

关键：**服务器仍然需要 Collision 数据**。这意味着 SkeletalMesh 和 StaticMesh 的碰撞形状需要 cook 进服务器包里，但它们的顶点数据和材质引用可以被剥离。

### 触发服务器 Cook

```bash
# 命令行 Cook（用于 CI/CD）
UnrealEditor-Cmd.exe \
  /path/to/project/MyGame.uproject \
  -run=Cook \
  -TargetPlatform=LinuxServer \
  -Unversioned \
  -CookAll \
  -iterate \
  -NoLogTimes
```

`-TargetPlatform=LinuxServer` 指定了这是服务器平台的 Cook，Unreal 会根据这个参数决定哪些资产需要包含、哪些可以跳过。

`-iterate` 参数允许增量 Cook——只重新 Cook 修改过的资产，大幅缩短迭代时间。第一次完整 Cook 之后，后续的增量 Cook 通常只需要几十秒。

### 关卡资产的 Server-Only 设置

在编辑器里，可以为 Actor 打上"Server-only"标签，告诉 Cook 系统这些 Actor 只需要在服务器版本的关卡里存在：

在 Actor 的 Details 面板里，`Replication > Net Load on Client` 设为 `false` 可以让这个 Actor 不在客户端加载；类似地，某些只用于客户端表现的 Actor（如天气特效、背景装饰物）可以设为不需要网络复制，这样服务器可以选择性地不加载它们。

## 打包流程

Cook 之后，还需要打包（Staging）——把 cook 好的资产和可执行文件组合成可以部署的目录结构。

```bash
# 完整的构建 + Cook + 打包流程
RunUAT.bat BuildCookRun \
  -project=/path/to/MyGame.uproject \
  -noP4 \
  -platform=Linux \
  -serverconfig=Development \
  -server \
  -serverplatform=Linux \
  -noclient \
  -build \
  -cook \
  -stage \
  -pak \
  -archive \
  -archivedirectory=/output/path
```

关键参数解释：
- `-server`：构建 Server Target
- `-noclient`：不构建客户端（在纯 CI 场景里可以单独构建服务端）
- `-serverconfig=Development`：服务器配置（`Development` 保留调试符号和日志，`Shipping` 最小化但难以调试）
- `-pak`：把资产打包成 .pak 文件（减少文件数量，提高 I/O 性能）

## 启动参数解析

Unreal DS 进程的启动方式：

```bash
./MyGameServer \
  /Game/Maps/Arena01 \       # 第一个非 - 参数是要加载的地图
  -log \                     # 输出日志到控制台（无头模式必加）
  -port=7777 \               # 游戏监听端口
  -QueryPort=27015 \         # Steam Query 端口（如果接入 Steam）
  -beaconport=15000 \        # Matchmaking beacon 端口
  -nosteam \                 # 不初始化 Steam（纯后端 DS 常用）
  -NOSTEAM \                 # 部分版本需要大写
  -ini:Game:[/Script/Engine.GameSession]:MaxPlayers=16
```

地图参数（`/Game/Maps/Arena01`）是位置参数，不是 `-map=` 这样的键值对。这和 Unity 的惯例不同，初次接触 Unreal DS 很容易在这里卡住。

在代码里解析自定义参数：

```cpp
// 在 GameMode 或专属的 ServerStartup Actor 里解析
FString RoomId;
if (FParse::Value(FCommandLine::Get(), TEXT("RoomId="), RoomId))
{
    UE_LOG(LogGameMode, Log, TEXT("Room ID: %s"), *RoomId);
}

int32 MaxPlayers = 16;
FParse::Value(FCommandLine::Get(), TEXT("MaxPlayers="), MaxPlayers);
```

## 常见陷阱

### 陷阱一：忘记关闭渲染导致 DS 崩溃

Unreal 的 Server Target 理论上不包含渲染模块，但如果你的蓝图或 C++ 代码里有对渲染相关对象的直接引用，而这些引用没有被 `#if !UE_SERVER` 保护，Server 构建在初始化阶段会出现空指针访问——因为渲染子系统根本没有初始化。

最常见的表现：服务器启动，加载第一张地图，然后在 `UGameViewportClient::Init` 或类似位置崩溃。

排查方法：
1. 用 `-log` 参数启动，找到崩溃前最后一行日志
2. 搜索 Crash Report 里的调用栈，找到第一个属于你的游戏代码的帧
3. 检查该函数里是否有对 `UGameViewportClient`、`USceneComponent`（渲染部分）、`UMaterialInterface` 的非保护访问

### 陷阱二：Cook 资产不足导致服务器地图加载失败

症状：服务器启动，开始加载地图，日志里出现大量 `Warning: Asset not found` 或 `Failed to load package`，最终地图加载超时或崩溃。

根本原因：服务器的 Cook 没有包含某些地图依赖的蓝图或数据资产。

排查步骤：
1. 在日志里找所有 `[AssetRegistry] Could not find` 或类似的警告
2. 确认这些资产是否被加入了服务器 Cook 的资产列表
3. 检查 `AssetManager.ini` 里的 `PrimaryAssetTypesToScan` 是否覆盖了服务器需要的资产类型

预防措施：为 CI 构建写一个 Cook 验证步骤——服务器启动后，自动化脚本检查所有必要地图是否能成功加载，而不是等到第一次正式部署才发现问题。

### 陷阱三：Blueprint NetMulticast 在纯 Server Cook 里失去客户端实现

如果你用 Blueprint 实现了一个 `NetMulticast` 函数，并且这个函数在蓝图里有"客户端专属"的节点（比如播放音效、播放粒子），这些节点不会在服务器 Cook 里被剥离——蓝图的服务器版本仍然包含这些节点，但在服务器上调用到它们时会因为相关系统未初始化而产生错误。

解决方案：把所有客户端表现逻辑从 `NetMulticast` 实现里移到 `OnRep_` 函数里，并用 `HasAuthority()` 判断区分逻辑路径。

### 陷阱四：ServerDefault 模式 vs Editor 模式的行为差异

`ServerDefault` 模式：在编辑器里用 Play In Editor (PIE) 启动一个模拟的专用服务器，方便快速迭代测试。这个模式有完整的渲染上下文（因为编辑器本身就有），行为和真实的 Server Build 不完全等价。

一个典型的"编辑器里正常，打包后崩溃"的例子：某个 Actor 在 `BeginPlay` 里调用了 `GetWorld()->GetGameViewport()`。在 PIE 的 ServerDefault 模式里，GameViewport 存在（编辑器提供了它）；在真实的 Server Build 里，GameViewport 是 null。这类问题只有在真正跑 Server Build 的时候才会暴露。

**工程建议**：每次重要功能迭代后，用真实的 Server Build 做一次冒烟测试，不要只依赖 PIE。

## 工程边界

**Unreal DS 的构建时间远超 Unity**：完整的构建 + Cook + 打包流程，中等规模项目通常需要 30-90 分钟。增量 Cook 可以缩短到 5-15 分钟，但仍需要规划好 CI 资源。

**调试 Server Build 需要 Development 配置，不要用 Shipping**：Shipping 构建关闭了日志、关闭了调试符号，DS 崩溃后几乎无从排查。生产环境可以用 Shipping，但至少要保留一个 Development 构建用于问题排查。

**`-log` 参数是 DS 调试的生命线**：没有渲染窗口的情况下，日志是唯一的调试输出。确保你的日志输出到可被运维系统收集的位置（比如写到文件或标准输出，再被 log aggregator 收集）。

## 最短结论

Unreal DS 构建比 Unity 更复杂的根本原因是：**Unreal 把构建目标、Cook 流程、运行时配置三件事都独立出来了**，你需要分别配置正确，任何一环出问题都会导致服务器启动失败。

Server Target 定义了"什么代码被编译"；Cook 流程决定了"什么资产被打包"；启动参数决定了"运行时加载什么地图用什么配置"。三者缺一不可，而且必须互相对齐。

两个最高频的坑：**没有加 `-log` 参数导致看不到崩溃原因**，以及 **Cook 资产不完整导致地图加载失败**。把这两个坑防住，Unreal DS 的构建流程本身其实相当稳定。
