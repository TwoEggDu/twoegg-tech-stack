---
title: "Unreal 网络 04｜PlayerController 与 GameMode：玩家登录与权限分层"
slug: "ue-net-04-player-controller"
date: "2026-03-28"
description: "PlayerController 是服务器和客户端之间最重要的桥梁，是唯一一个同时存在于两端的 Actor。GameMode 只在服务器存在，负责规则和玩家登录流程。"
tags:
  - "Unreal"
  - "网络"
  - "PlayerController"
  - "GameMode"
series: "Unreal Engine 架构与系统"
weight: 6200
---

在 Unreal 联机架构中，哪个类在哪个机器上存在，是理解网络流程的基础。PlayerController 是连接服务器逻辑和客户端的桥梁，也是写网络代码最常用的入口点。

---

## 对象存在关系

```
                    Server    Client(Own)  Client(Other)
GameMode             ✅          ❌            ❌
GameState            ✅          ✅            ✅
PlayerController     ✅          ✅            ❌   ← 每人的 PC 只在自己的客户端存在
PlayerState          ✅          ✅            ✅   ← 所有玩家的 PS 都复制到所有客户端
Pawn/Character       ✅          ✅            ✅
HUD                  ❌          ✅            ❌   ← 只在本地客户端
```

这个表格解释了为什么：
- UI 逻辑写在 `HUD` 或 `PlayerController` 的 `Client RPC` 里
- 游戏规则写在 `GameMode` 里（只在服务器）
- 其他玩家的信息从 `PlayerState` 读取（所有端都有）

---

## PlayerController 的网络职责

PlayerController 是**唯一一个在客户端和服务器上都有同一个对象的 Actor**（服务器有所有玩家的 PC，客户端只有自己的 PC）。

```cpp
UCLASS()
class AMyPlayerController : public APlayerController
{
    GENERATED_BODY()
public:
    // ─── 输入处理（本地客户端）───────────────────────────
    virtual void SetupInputComponent() override
    {
        Super::SetupInputComponent();
        InputComponent->BindAction("Attack", IE_Pressed, this,
            &AMyPlayerController::OnAttackPressed);
    }

    void OnAttackPressed()
    {
        // 客户端输入 → 告诉 Pawn 执行
        if (AMyCharacter* Char = Cast<AMyCharacter>(GetPawn()))
        {
            Char->StartAttack();  // Char 里会有 Server RPC
        }
    }

    // ─── 服务器 → 客户端通知（Client RPC）───────────────
    UFUNCTION(Client, Reliable)
    void ClientReceiveMessage(const FString& Message);
    void ClientReceiveMessage_Implementation(const FString& Message)
    {
        // 显示系统消息（在本地客户端执行）
        if (UMyHUD* HUD = Cast<UMyHUD>(GetHUD()))
        {
            HUD->ShowSystemMessage(Message);
        }
    }

    // ─── 客户端 → 服务器请求（Server RPC）───────────────
    UFUNCTION(Server, Reliable, WithValidation)
    void ServerRequestRespawn();
    bool ServerRequestRespawn_Validate() { return true; }
    void ServerRequestRespawn_Implementation()
    {
        // 在服务器处理复活请求
        if (AMyGameMode* GM = GetWorld()->GetAuthGameMode<AMyGameMode>())
        {
            GM->RespawnPlayer(this);
        }
    }
};
```

---

## 玩家登录流程

```
1. 客户端连接 → UNetDriver 建立 UNetConnection
2. 握手完成 → 服务器调用 GameMode::PreLogin()
   → 返回空字符串 = 允许，返回错误字符串 = 拒绝
3. Login() → 创建 PlayerController
4. PostLogin() → 创建 PlayerState，关联 PC
5. GameMode::RestartPlayer() → 找到 PlayerStart，Spawn Pawn，Possess
6. 客户端 PlayerController 的 AcknowledgePossession() → 客户端确认 Possess
```

```cpp
UCLASS()
class AMyGameMode : public AGameModeBase
{
    GENERATED_BODY()
public:
    // 登录前验证（检查 Token、封号状态等）
    virtual void PreLogin(const FString& Options, const FString& Address,
        const FUniqueNetIdRepl& UniqueId, FString& ErrorMessage) override
    {
        Super::PreLogin(Options, Address, UniqueId, ErrorMessage);
        if (ErrorMessage.IsEmpty())
        {
            // 自定义验证逻辑
            // ErrorMessage = TEXT("Server is full");  // 设置此值以拒绝连接
        }
    }

    // 玩家完全登录后
    virtual void PostLogin(APlayerController* NewPlayer) override
    {
        Super::PostLogin(NewPlayer);

        // 通知所有玩家有新玩家加入
        for (FConstPlayerControllerIterator It = GetWorld()->GetPlayerControllerIterator(); It; ++It)
        {
            if (AMyPlayerController* PC = Cast<AMyPlayerController>(It->Get()))
            {
                PC->ClientReceiveMessage(
                    FString::Printf(TEXT("%s joined the game"), *NewPlayer->GetName()));
            }
        }
    }

    // 玩家断线时
    virtual void Logout(AController* Exiting) override
    {
        Super::Logout(Exiting);
        // 清理该玩家的游戏状态
    }
};
```

---

## GameState vs PlayerState

```cpp
// GameState：所有玩家共享的游戏状态（复制给所有客户端）
UCLASS()
class AMyGameState : public AGameStateBase
{
    GENERATED_BODY()
public:
    UPROPERTY(Replicated)
    int32 BlueTeamScore;

    UPROPERTY(Replicated)
    int32 RedTeamScore;

    UPROPERTY(Replicated)
    float RemainingTime;
};

// PlayerState：每个玩家自己的状态（复制给所有客户端）
UCLASS()
class AMyPlayerState : public APlayerState
{
    GENERATED_BODY()
public:
    UPROPERTY(Replicated)
    int32 Kills;

    UPROPERTY(Replicated)
    int32 Deaths;

    UPROPERTY(Replicated)
    int32 TeamIndex;
};
```

---

## SeamlessTravel：无缝关卡切换

```cpp
// 服务器发起关卡切换（带玩家数据）
void AMyGameMode::SwitchToNextMap()
{
    // 非无缝切换（所有客户端断线重连）
    // GetWorld()->ServerTravel("/Game/Maps/Level2?listen");

    // 无缝切换（保持连接，PlayerController/PlayerState 可以被保留）
    bUseSeamlessTravel = true;
    GetWorld()->ServerTravel("/Game/Maps/Level2");
}

// 控制哪些 Actor 在无缝切换时保留
virtual void GetSeamlessTravelActorList(bool bToTransition,
    TArray<AActor*>& ActorList) override
{
    Super::GetSeamlessTravelActorList(bToTransition, ActorList);
    // ActorList 中的 Actor 会在关卡切换时保留
}
```
