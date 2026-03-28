---
title: "Unreal 网络 01｜Unreal 网络架构：Actor 复制与 NetDriver"
slug: "ue-net-01-architecture"
date: "2026-03-28"
description: "Unreal 的网络架构以 Actor 复制为核心，由 NetDriver 管理连接，ReplicationGraph 决定哪些 Actor 复制给哪些客户端。理解这套架构是处理联机 Bug 的基础。"
tags:
  - "Unreal"
  - "网络"
  - "Actor复制"
  - "NetDriver"
series: "Unreal Engine 架构与系统"
weight: 6170
---

Unreal 的网络系统是 Server-Authoritative（服务器权威）模型：服务器持有游戏的真实状态，客户端通过 Actor 复制接收状态同步。理解这套架构的层次结构，才能正确处理联机游戏中的各种同步问题。

---

## 架构总览

```
Server
  UNetDriver
    ├─ UNetConnection (Client 1)
    │    └─ UChannel[]
    │         ├─ UControlChannel  ← 连接控制（握手、登录）
    │         ├─ UVoiceChannel    ← 语音
    │         └─ UActorChannel    ← 每个被复制的 Actor 一个 Channel
    ├─ UNetConnection (Client 2)
    └─ ...

  UReplicationGraph / UNetReplicationGraphConnection
    └─ 决定：哪些 Actor 复制给哪个连接
```

每个 Actor 复制都走自己的 `UActorChannel`，Channel 内按属性变化增量同步。

---

## Actor 复制开关

```cpp
// Actor 需要明确开启复制
UCLASS()
class AMyActor : public AActor
{
    GENERATED_BODY()
public:
    AMyActor()
    {
        // 开启 Actor 复制（默认 false）
        bReplicates = true;

        // 开启移动组件复制（角色位置/旋转同步）
        SetReplicateMovement(true);
    }
};
```

---

## 网络角色（Network Role）

每个 Actor 在不同机器上有不同的 `ENetRole`：

| Role | 描述 | 典型对象 |
|------|------|---------|
| `ROLE_Authority` | 权威端（服务器） | 所有 Actor 在服务器上都是 Authority |
| `ROLE_AutonomousProxy` | 自治代理（本地玩家） | 玩家自己控制的 Character |
| `ROLE_SimulatedProxy` | 模拟代理（其他玩家/AI） | 其他玩家的 Character |
| `ROLE_None` | 不复制 | 纯本地 Actor |

```cpp
void AMyActor::BeginPlay()
{
    Super::BeginPlay();

    if (HasAuthority())
    {
        // 在服务器执行的逻辑
    }

    if (IsLocallyControlled())
    {
        // 本地玩家控制的逻辑
    }

    switch (GetLocalRole())
    {
    case ROLE_Authority:         // 服务器
    case ROLE_AutonomousProxy:   // 本地玩家
    case ROLE_SimulatedProxy:    // 其他玩家/AI
    }
}
```

---

## 属性复制（UPROPERTY Replication）

```cpp
UCLASS()
class AMyCharacter : public ACharacter
{
    GENERATED_BODY()
public:
    // 声明可复制属性
    UPROPERTY(ReplicatedUsing = OnRep_Health)
    float Health;

    // 注册复制
    virtual void GetLifetimeReplicatedProps(
        TArray<FLifetimeProperty>& OutLifetimeProps) const override
    {
        Super::GetLifetimeReplicatedProps(OutLifetimeProps);

        // 复制给所有连接
        DOREPLIFETIME(AMyCharacter, Health);

        // 条件复制（只复制给拥有者）
        DOREPLIFETIME_CONDITION(AMyCharacter, PrivateInfo, COND_OwnerOnly);

        // 条件复制（跳过本地控制的 Actor）
        DOREPLIFETIME_CONDITION(AMyCharacter, SimPosition, COND_SkipOwner);
    }

    // 客户端收到属性更新时的回调
    UFUNCTION()
    void OnRep_Health()
    {
        // 更新 UI、播放受伤效果等
        UpdateHealthUI();
    }
};
```

---

## 复制条件（ELifetimeCondition）

| 条件 | 说明 |
|------|------|
| `COND_None` | 复制给所有连接 |
| `COND_OwnerOnly` | 只复制给拥有者 |
| `COND_SkipOwner` | 跳过拥有者（复制给其他人） |
| `COND_SimulatedOnly` | 只复制给 SimulatedProxy |
| `COND_AutonomousOnly` | 只复制给 AutonomousProxy |
| `COND_InitialOnly` | 只在 Actor 第一次复制时发送 |
| `COND_ReplayOnly` | 只在回放时复制 |

---

## 复制频率控制

```cpp
// Actor 的最大复制频率（默认 1/帧，可以降低以节省带宽）
AMyActor::AMyActor()
{
    // 每秒最多复制 10 次（而不是每帧）
    NetUpdateFrequency = 10.f;

    // 优先级（相对值，高优先级的 Actor 更频繁地被检查）
    NetPriority = 1.0f;

    // 静态 Actor 可以设置为只同步一次
    // bNetLoadOnClient = true;  // 随关卡加载，不需要运行时复制
}

// 手动标记需要立即复制（紧急状态变化）
void AMyCharacter::TakeDamage_Internal(float Damage)
{
    Health -= Damage;
    ForceNetUpdate();  // 触发立即复制，不等下次更新频率
}
```

---

## NetRelevancy：相关性判断

不是所有 Actor 都需要复制给所有客户端，引擎通过 `IsNetRelevantFor()` 判断：

```cpp
// 自定义相关性（距离 + 可见性）
bool AMyActor::IsNetRelevantFor(
    const AActor* RealViewer,
    const AActor* ViewTarget,
    const FVector& SrcLocation) const
{
    // 超过 1000 单位不相关
    float DistSq = FVector::DistSquared(GetActorLocation(), SrcLocation);
    if (DistSq > 1000.f * 1000.f)
    {
        return false;
    }

    return Super::IsNetRelevantFor(RealViewer, ViewTarget, SrcLocation);
}

// 或者简单地：永远相关（Boss、关键道具）
AMyBossActor::AMyBossActor()
{
    bAlwaysRelevant = true;
}
```

---

## ReplicationGraph（UE4.20+）

对于大型多人游戏（100+ 玩家），传统复制系统因为要遍历所有 Actor 判断相关性而性能较差。ReplicationGraph 通过空间索引（格子划分、节点树）加速这个过程：

```cpp
// 继承 UReplicationGraph 创建自定义复制图
UCLASS()
class UMyReplicationGraph : public UReplicationGraph
{
    GENERATED_BODY()
public:
    virtual void InitGlobalActorClassSettings() override;
    virtual void InitGlobalGraphNodes() override;
    virtual void InitConnectionGraphNodes(UNetReplicationGraphConnection* RepGraphConnection) override;
    // 将 Actor 分配到适当的节点（GridSpatialization、AlwaysRelevant 等）
};
```
