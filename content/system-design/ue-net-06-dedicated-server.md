---
title: "Unreal 网络 06｜Dedicated Server：服务器端构建与部署实践"
slug: "ue-net-06-dedicated-server"
date: "2026-03-28"
description: "Dedicated Server 是不渲染画面的纯服务器构建版本，是正式联机游戏的标准部署方式。理解 DS 的构建、启动参数和服务器专属代码路径，是上线联机功能的必修课。"
tags:
  - "Unreal"
  - "网络"
  - "DedicatedServer"
  - "服务器部署"
series: "Unreal Engine 架构与系统"
weight: 6220
---

Dedicated Server（DS，专用服务器）是没有渲染系统、音频系统、本地玩家的纯逻辑服务器版本。正式联机游戏通常部署 DS，而不是让某个玩家作为 Listen Server——后者会给 Host 玩家带来不公平的延迟优势。

---

## 三种运行模式

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| **Standalone** | 单机，无网络 | 单人游戏、本地测试 |
| **Listen Server** | 某个玩家既是 Host 也参与游戏 | 小型多人（局域网对战、合作模式） |
| **Dedicated Server** | 独立服务器进程，无本地玩家 | 正式联机游戏 |

---

## 构建 Dedicated Server

```csharp
// MyGameServer.Target.cs（专用服务器构建目标）
using UnrealBuildTool;

public class MyGameServerTarget : TargetRules
{
    public MyGameServerTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Server;  // 关键：标记为 Server 目标
        DefaultBuildSettings = BuildSettingsVersion.V2;
        ExtraModuleNames.Add("MyGame");

        // Server 不需要渲染/音频相关模块
        bWithServerCode = true;
    }
}
```

```bash
# 构建命令
UnrealBuildTool.exe MyGameServer Linux Development \
    "E:/Projects/MyGame/MyGame.uproject"

# 或通过 UAT 完整打包
RunUAT.bat BuildCookRun \
    -project="E:/Projects/MyGame/MyGame.uproject" \
    -noP4 -platform=Linux -serverconfig=Development \
    -server -serverplatform=Linux \
    -cook -build -stage -archive \
    -archivedirectory="E:/Build/Server"
```

---

## 启动服务器

```bash
# 基本启动
./MyGameServer /Game/Maps/Lobby -log -port=7777

# 常用启动参数
# -log              → 输出日志到控制台
# -port=7777        → 监听端口
# -MaxPlayers=20    → 最大玩家数
# -NoSound          → 禁用声音（Dedicated 默认已禁用）
# ?listen           → 作为 Listen Server 启动（非 DS 时）
# -NullRHI          → 使用空 RHI（DS 自动使用，无需指定）

# 带选项（通过 URL Options 传递给 GameMode）
./MyGameServer /Game/Maps/GameMap?MaxPlayers=16?GameTime=300 -log
```

---

## 服务器专属代码路径

```cpp
// 判断当前运行模式
bool bIsServer     = HasAuthority();                          // 服务器（DS 或 Listen Host）
bool bIsDedicated  = IsRunningDedicatedServer();             // 仅 DS
bool bIsClient     = IsNetMode(NM_Client);                   // 纯客户端
bool bIsListenServer = IsNetMode(NM_ListenServer);           // Listen Server

// 常见模式：
// NM_Standalone, NM_DedicatedServer, NM_ListenServer, NM_Client

// 在构建时排除客户端代码（减小 Server 包体）
#if WITH_SERVER_CODE
    // 仅 Server 构建包含的代码
    void ServerOnlyFunction() { ... }
#endif

// 在构建时排除编辑器代码
#if WITH_EDITOR
    void EditorOnlyHelper() { ... }
#endif
```

---

## GAS 在 DS 上的特殊注意事项

```cpp
// GameplayCue 在 DS 上不执行（纯视觉，DS 没有渲染系统）
// GAS 内部已处理，Cue 在 Server 端自动跳过

// 但 GE 的逻辑效果（属性修改）在 DS 上正常执行并复制

// 确认 ASC 初始化在正确时机
void AMyCharacter::PossessedBy(AController* NewController)
{
    Super::PossessedBy(NewController);

    if (HasAuthority())
    {
        // DS 上，Possess 时初始化 ASC
        if (AMyPlayerState* PS = GetPlayerState<AMyPlayerState>())
        {
            AbilitySystemComponent = PS->GetAbilitySystemComponent();
            AbilitySystemComponent->InitAbilityActorInfo(PS, this);
        }
    }
}
```

---

## 服务器日志与监控

```cpp
// 自定义日志类别
DEFINE_LOG_CATEGORY(LogMyGame);

// 在关键路径记录日志
void AMyGameMode::PostLogin(APlayerController* NewPlayer)
{
    Super::PostLogin(NewPlayer);

    UE_LOG(LogMyGame, Log, TEXT("[Server] Player '%s' logged in. Total players: %d"),
        *NewPlayer->GetName(),
        GetNumPlayers());
}

// DS 崩溃恢复（通常由外部守护进程处理）
// 在启动脚本中监控并自动重启：
// while true; do ./MyGameServer ... ; sleep 5; done
```

---

## 常见 DS 问题排查

**问题 1：DS 上某些 Actor 行为异常**
```
检查项：
- Actor 是否正确设置了 bReplicates = true
- 代码是否误用了 IsLocallyControlled()（DS 上永远 false）
- BeginPlay 中的逻辑是否区分了服务器/客户端
```

**问题 2：客户端连不上 DS**
```bash
# 检查端口监听
netstat -an | grep 7777

# 防火墙是否开放 UDP 7777
# Unreal 默认用 UDP，不是 TCP

# 检查 DS 日志是否有 "Pending Connection" 错误
```

**问题 3：DS 内存占用过高**
```cpp
// 减少 DS 上不必要的资源加载
// 在 .ini 中排除不需要的模块
// [/Script/Engine.Engine]
// +ModulesToAddInDedicatedServer=
// -ModulesToIgnoreInDedicatedServer=SlateCore

// 使用 UGameplayStatics::IsServer() 跳过客户端资源加载
if (!IsRunningDedicatedServer())
{
    LoadClientOnlyAssets();
}
```

---

## 快速测试联机（编辑器内）

```
# PIE 多玩家测试设置（Editor Preferences → Play in Editor）
# Number of Players: 2
# Net Mode: Play As Listen Server（或 Play As Client）
# Run Under One Process: false（更接近真实网络）

# 也可以命令行启动两个实例：
MyGame.exe /Game/Maps/Level -log &
MyGame.exe 127.0.0.1 -log &
```
