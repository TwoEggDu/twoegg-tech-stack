---
title: "Unreal 网络 05｜网络优化：带宽控制、Delta 序列化与优先级"
slug: "ue-net-05-optimization"
date: "2026-03-28"
description: "Unreal 网络优化围绕减少带宽消耗展开：条件复制、Delta 序列化、优先级调度、ReplicationGraph。理解这些机制才能在不牺牲体验的前提下支持更多并发玩家。"
tags:
  - "Unreal"
  - "网络"
  - "网络优化"
  - "带宽"
series: "Unreal Engine 架构与系统"
weight: 6210
---

网络带宽是联机游戏的核心瓶颈之一。Unreal 提供了多层优化机制，从属性级别的条件复制，到 Actor 级别的优先级调度，再到场景级别的 ReplicationGraph。理解这些工具的层次，才能找到正确的优化切入点。

---

## 复制系统的工作流程

每一帧，引擎遍历所有标记为 `bReplicates = true` 的 Actor，执行：

```
1. 相关性检查（IsNetRelevantFor）
   → 不相关的 Actor 不复制，本帧跳过

2. 优先级计算（GetNetPriority）
   → 优先级高的 Actor 更频繁地更新

3. 带宽预算检查
   → 超出带宽预算的 Actor 延到下一帧

4. 属性 Diff
   → 与上一次复制状态对比，只发送变化的属性

5. 序列化（FNetBitWriter）
   → 将变化的属性写入 BitStream 发送
```

---

## 条件复制（优化带宽分发）

```cpp
void AMyCharacter::GetLifetimeReplicatedProps(
    TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);

    // 所有人都需要知道的：生命值、名字
    DOREPLIFETIME(AMyCharacter, Health);

    // 只有拥有者需要知道：弹药、资源
    DOREPLIFETIME_CONDITION(AMyCharacter, Ammo, COND_OwnerOnly);

    // 其他人需要知道，拥有者不需要（自己本地已经有了）
    DOREPLIFETIME_CONDITION(AMyCharacter, SimulatedPosition, COND_SkipOwner);

    // 只初始化一次（静态信息）
    DOREPLIFETIME_CONDITION(AMyCharacter, CharacterClass, COND_InitialOnly);

    // 动态条件（运行时决定是否复制）
    DOREPLIFETIME_CONDITION_NOTIFY(AMyCharacter, TeamScore,
        COND_None, REPNOTIFY_Always);
}
```

---

## Push Model：主动推送而非轮询

默认情况下，引擎每帧都 Diff 所有已复制属性，找出变化的部分——这在大量 Actor 时开销很大。

UE4.25+ 引入了 **Push Model**：由代码主动标记"这个属性变了"，引擎只序列化被标记的属性：

```cpp
// 开启 Push Model（在 .Build.cs 中）
// PublicDefinitions.Add("UE_WITH_IRIS=0");  // 传统复制
// 在项目设置或 .ini 中开启 PushModel

// 声明时使用 UPROPERTY with PUSH
UPROPERTY(ReplicatedUsing = OnRep_Health)
int32 Health;

// 修改属性时主动标记
void AMyCharacter::SetHealth(int32 NewHealth)
{
    if (Health != NewHealth)
    {
        Health = NewHealth;
        MARK_PROPERTY_DIRTY_FROM_NAME(AMyCharacter, Health, this);  // 主动标记
    }
}
```

Push Model 可以显著降低大量 Actor 场景的 CPU 开销（减少不必要的内存比对）。

---

## 自定义序列化（节省带宽）

```cpp
// 自定义 NetSerialize（用于压缩特殊数据）
USTRUCT()
struct FMyCompressedVector
{
    GENERATED_BODY()

    float X, Y, Z;

    bool NetSerialize(FArchive& Ar, class UPackageMap* Map, bool& bOutSuccess)
    {
        // 将浮点压缩为 16 位整数（精度 0.1cm，范围 ±3276m）
        if (Ar.IsSaving())
        {
            int16 CompX = FMath::Clamp((int32)(X * 10.f), -32768, 32767);
            int16 CompY = FMath::Clamp((int32)(Y * 10.f), -32768, 32767);
            int16 CompZ = FMath::Clamp((int32)(Z * 10.f), -32768, 32767);
            Ar << CompX << CompY << CompZ;  // 6 bytes 而不是 12 bytes
        }
        else
        {
            int16 CompX, CompY, CompZ;
            Ar << CompX << CompY << CompZ;
            X = CompX / 10.f;
            Y = CompY / 10.f;
            Z = CompZ / 10.f;
        }
        bOutSuccess = true;
        return true;
    }
};

// 让 Unreal 知道使用自定义序列化
template<>
struct TStructOpsTypeTraits<FMyCompressedVector>
    : public TStructOpsTypeTraitsBase2<FMyCompressedVector>
{
    enum { WithNetSerializer = true };
};
```

---

## Actor 复制优先级

```cpp
// 影响复制频率的因素：
AMyActor::AMyActor()
{
    // 基础优先级（相对值，越高越优先）
    NetPriority = 3.0f;  // 重要物体（玩家角色默认 3.0）

    // 更新频率上限
    NetUpdateFrequency = 10.f;  // 每秒最多复制 10 次

    // 最小更新间隔
    MinNetUpdateFrequency = 2.f;  // 即使没变化，也至少每 0.5 秒复制一次

    // 不相关后的休眠时间
    // NetCullDistanceSquared = 250000.f * 250000.f;  // 250m 外不复制
}

// 动态调整优先级（离玩家越近优先级越高）
virtual float GetNetPriority(const FVector& ViewPos, const FVector& ViewDir,
    AActor* Viewer, AActor* ViewTarget, UActorChannel* InChannel,
    float Time, bool bLowBandwidth) override
{
    float Dist = FVector::Dist(GetActorLocation(), ViewPos);
    float DistFactor = FMath::Clamp(1.f - Dist / 5000.f, 0.1f, 1.f);
    return NetPriority * DistFactor;
}
```

---

## 带宽监控

```cpp
// 控制台命令
// net.Stats 1          → 显示网络统计（发送/接收字节数、Actor 数量）
// net.PacketLoss=0.1   → 模拟 10% 丢包（测试用）
// net.PktLag=100       → 模拟 100ms 延迟

// 代码获取网络统计
if (UNetDriver* NetDriver = GetWorld()->GetNetDriver())
{
    // 每个连接的统计
    for (UNetConnection* Conn : NetDriver->ClientConnections)
    {
        UE_LOG(LogNet, Log, TEXT("Client %s: InBytes=%d, OutBytes=%d, Ping=%dms"),
            *Conn->GetName(),
            Conn->InBytes,
            Conn->OutBytes,
            (int32)(Conn->AvgLag * 1000.f));
    }
}
```

---

## Iris：UE5 的新复制系统

UE5.1 引入了实验性的 **Iris Replication System**，目标是替代传统复制系统：

- **按对象分离的 Replication State**：每个属性独立管理，减少全量比对
- **Filter 系统**：替代 `IsNetRelevantFor` 的声明式 API
- **更好的可扩展性**：支持大规模多人场景

目前（UE5.3）Iris 仍在迭代中，传统系统依然是默认选项。
