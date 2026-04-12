---
title: "Unreal 网络 02｜RPC：远程过程调用的三种类型与使用规范"
slug: "ue-net-02-rpc"
date: "2026-03-28"
description: "Unreal 的 RPC 分 Server、Client、NetMulticast 三类，分别用于客户端调用服务器、服务器通知特定客户端、服务器广播所有客户端。错用 RPC 是联机 Bug 的常见来源。"
tags:
  - "Unreal"
  - "网络"
  - "RPC"
  - "联机"
series: "Unreal Engine 架构与系统"
weight: 6180
---

RPC（Remote Procedure Call，远程过程调用）是 Unreal 联机系统中除了属性复制之外的另一个核心机制。属性复制是"状态同步"（服务器推送最新状态给客户端），RPC 是"事件通知"（一端主动触发另一端执行特定函数）。

---

## 三种 RPC 类型

| 类型 | 声明宏 | 调用方 | 执行方 | 典型用途 |
|------|--------|--------|--------|---------|
| **Server** | `UFUNCTION(Server)` | 客户端 | 服务器 | 客户端请求操作（开火、购买、交互） |
| **Client** | `UFUNCTION(Client)` | 服务器 | 特定客户端 | 服务器通知特定客户端（UI、音效） |
| **NetMulticast** | `UFUNCTION(NetMulticast)` | 服务器 | 所有客户端 + 服务器 | 广播事件（爆炸、技能特效） |

---

## 声明与实现

```cpp
UCLASS()
class AMyCharacter : public ACharacter
{
    GENERATED_BODY()

    // ────── Server RPC ──────
    // Reliable: 保证送达（重要逻辑）
    // WithValidation: 附带验证函数（防作弊）
    UFUNCTION(Server, Reliable, WithValidation)
    void ServerFire(FVector Origin, FVector Direction);
    bool ServerFire_Validate(FVector Origin, FVector Direction);   // 返回 false = 断开连接
    void ServerFire_Implementation(FVector Origin, FVector Direction);

    // ────── Client RPC ──────
    UFUNCTION(Client, Reliable)
    void ClientShowKillFeed(const FString& KillerName, const FString& VictimName);
    void ClientShowKillFeed_Implementation(const FString& KillerName, const FString& VictimName);

    // ────── NetMulticast RPC ──────
    // Unreliable: 可以丢失（不重要的视觉效果）
    UFUNCTION(NetMulticast, Unreliable)
    void MulticastPlayHitEffect(FVector HitLocation);
    void MulticastPlayHitEffect_Implementation(FVector HitLocation);
};
```

---

## 实现示例

```cpp
// ─── Server RPC ───────────────────────────────────────────
bool AMyCharacter::ServerFire_Validate(FVector Origin, FVector Direction)
{
    // 验证：起点不能离角色太远（防止传送作弊）
    float DistSq = FVector::DistSquared(Origin, GetActorLocation());
    return DistSq < 500.f * 500.f;
}

void AMyCharacter::ServerFire_Implementation(FVector Origin, FVector Direction)
{
    // 服务器执行：生成子弹、处理命中
    check(HasAuthority());

    FActorSpawnParameters Params;
    AMyProjectile* Projectile = GetWorld()->SpawnActor<AMyProjectile>(
        ProjectileClass, Origin, Direction.Rotation(), Params);

    // 广播特效给所有客户端
    MulticastPlayFireEffect(Origin, Direction);
}

// ─── Client RPC ───────────────────────────────────────────
void AMyCharacter::ClientShowKillFeed_Implementation(
    const FString& KillerName, const FString& VictimName)
{
    // 只在本地客户端执行：更新 UI
    check(!HasAuthority() || IsNetMode(NM_ListenServer));

    if (UMyHUD* HUD = Cast<UMyHUD>(GetHUD()))
    {
        HUD->ShowKillFeed(KillerName, VictimName);
    }
}

// ─── NetMulticast RPC ─────────────────────────────────────
void AMyCharacter::MulticastPlayHitEffect_Implementation(FVector HitLocation)
{
    // 在所有客户端 + 服务器执行
    // 通常用于播放特效，不包含游戏逻辑
    UGameplayStatics::SpawnEmitterAtLocation(this, HitParticle, HitLocation);
    UGameplayStatics::PlaySoundAtLocation(this, HitSound, HitLocation);
}
```

---

## Reliable vs Unreliable

| 类型 | 送达保证 | 开销 | 适用场景 |
|------|---------|------|---------|
| **Reliable** | 保证送达，按序执行 | 高（重传机制） | 游戏逻辑、UI 更新、状态改变 |
| **Unreliable** | 可能丢失，不保序 | 低 | 视觉特效、音效、非关键更新 |

**注意**：Reliable RPC 在高丢包网络下可能因重传堆积导致延迟增加，不要滥用。

---

## RPC 的执行条件

RPC 只能在**有网络连接的 Actor** 上调用，且调用方必须正确：

```cpp
// Server RPC：必须由客户端调用
// 如果在服务器上调用 Server RPC，它直接在本地执行（不通过网络）
void AMyCharacter::Fire()
{
    if (IsLocallyControlled())
    {
        // 本地玩家按键 → 发 Server RPC
        ServerFire(GetActorLocation(), GetControlRotation().Vector());
    }
}

// Client RPC：必须在服务器上调用，只发给该 Actor 的 OwningConnection
void AGameMode::NotifyPlayerKill(APlayerController* Killer, APlayerController* Victim)
{
    // 通知击杀者
    if (AMyCharacter* KillerChar = Cast<AMyCharacter>(Killer->GetPawn()))
    {
        KillerChar->ClientShowKillFeed(Killer->GetName(), Victim->GetName());
    }
}
```

---

## 常见错误

**错误 1：在服务器上调用 Server RPC（想当然地认为会发给所有客户端）**
```cpp
// ❌ 错误理解
// Server RPC 在服务器上调用 = 直接本地执行，没有广播
void AMyActor::ServerDoSomething_Implementation()
{
    ServerDoSomething();  // 在 Server Implementation 里再调 Server RPC = 死循环
}

// ✅ 如果想广播给所有客户端，用 NetMulticast
```

**错误 2：NetMulticast 在客户端调用**
```cpp
// ❌ 错误：NetMulticast 必须从服务器调用
void AMyActor::OnClientClick()
{
    MulticastPlayEffect();  // 客户端调用 Multicast，只在本地执行，不会广播
}

// ✅ 正确：先 Server RPC，服务器再调 Multicast
void AMyActor::OnClientClick()
{
    ServerRequestEffect();
}
void AMyActor::ServerRequestEffect_Implementation()
{
    MulticastPlayEffect();  // 服务器调用，广播给所有客户端
}
```

---

## RPC 与属性复制的选择

```
用属性复制（UPROPERTY Replicated）当：
  - 状态数据（生命值、位置、弹药数）
  - 晚加入的客户端需要获得当前状态

用 RPC 当：
  - 一次性事件（开枪、爆炸）
  - 不需要持久化的操作
  - 需要传递复杂参数（事件数据）
  - 只需要特定客户端知道（Client RPC）
```
