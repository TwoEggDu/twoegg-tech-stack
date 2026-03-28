---
title: "Unreal GAS 07｜GameplayCue：视觉音效解耦与本地执行"
slug: "ue-gas-07-gameplay-cue"
date: "2026-03-28"
description: "GameplayCue 是 GAS 中视觉/音效反馈与游戏逻辑解耦的机制。Cue 在客户端本地执行，不参与游戏逻辑，服务器只发送 Cue 事件，客户端自行决定如何表现。"
tags:
  - "Unreal"
  - "GAS"
  - "GameplayCue"
  - "特效"
series: "Unreal Engine 架构与系统"
weight: 6150
---

GAS 中视觉/音效反馈（粒子、音效、屏幕抖动）与游戏逻辑完全分离，由 GameplayCue 系统负责。这种分离的好处是：Cue 可以在客户端本地执行，不需要等待服务器往返，视觉反馈更即时，也减少了网络带宽。

---

## Cue 的触发方式

GameplayCue 通过 Tag 触发（Tag 必须以 `GameplayCue.` 开头）：

```
GameplayCue.Character.FootStep    ← 脚步声
GameplayCue.Character.Hit.Slash   ← 斩击命中特效
GameplayCue.Skill.Fire.Ignite     ← 点燃特效
GameplayCue.Environment.Explosion ← 爆炸
```

触发方式有两种：
1. **通过 GE**：在 GameplayEffect 的 `GameplayCues` 列表中配置，GE 应用/移除时自动触发
2. **手动触发**：从 GA 或 C++ 直接发送 Cue

```cpp
// 方式一：在 GA 中手动触发 Cue
void UGA_FireStrike::ActivateAbility(...)
{
    // ...执行伤害逻辑...

    // 触发一次性 Cue（OnExecuted，通常用于特效、音效）
    FGameplayCueParameters CueParams;
    CueParams.Location = HitLocation;
    CueParams.Normal = HitNormal;
    CueParams.EffectCauser = GetAvatarActorFromActorInfo();

    AbilitySystemComponent->ExecuteGameplayCue(
        FGameplayTag::RequestGameplayTag("GameplayCue.Skill.Fire.Ignite"),
        CueParams
    );
}

// 方式二：通过 GE 自动触发（持续型，GE 激活时 OnActive，GE 移除时 OnRemove）
// 在 GE_Burning 的 GameplayCues 中添加：
//   GameplayCueTag: GameplayCue.Character.Burning
//   MinLevel/MaxLevel: 默认
```

---

## GameplayCue Notify 类型

| 类型 | 说明 | 适用场景 |
|------|------|---------|
| `UGameplayCueNotify_Static` | 无实例，静态处理 | 一次性特效、音效（命中、爆炸） |
| `UGameplayCueNotify_Actor` | 会生成 Actor，有生命周期 | 持续特效（燃烧粒子、持续音效） |

---

## UGameplayCueNotify_Static（一次性 Cue）

```cpp
UCLASS()
class UGC_SlashHit : public UGameplayCueNotify_Static
{
    GENERATED_BODY()
public:
    // Cue 被执行时调用（对应 ExecuteGameplayCue）
    virtual bool OnExecute_Implementation(
        AActor* MyTarget,
        const FGameplayCueParameters& Parameters) const override
    {
        if (MyTarget)
        {
            // 在命中位置播放粒子
            UGameplayStatics::SpawnEmitterAtLocation(
                MyTarget->GetWorld(),
                SlashParticle,
                Parameters.Location,
                Parameters.Normal.Rotation()
            );

            // 播放音效
            UGameplayStatics::PlaySoundAtLocation(
                MyTarget->GetWorld(),
                SlashSound,
                Parameters.Location
            );
        }
        return true;  // true = 默认处理，false = 阻止默认处理
    }

protected:
    UPROPERTY(EditDefaultsOnly)
    UParticleSystem* SlashParticle;

    UPROPERTY(EditDefaultsOnly)
    USoundBase* SlashSound;
};
```

---

## UGameplayCueNotify_Actor（持续型 Cue）

```cpp
UCLASS()
class AGC_BurningEffect : public AGameplayCueNotify_Actor
{
    GENERATED_BODY()
public:
    // GE 激活（持续 Cue 开始）
    virtual bool OnActive_Implementation(
        AActor* MyTarget,
        const FGameplayCueParameters& Parameters) override
    {
        // 播放燃烧粒子（持续）
        if (BurningParticleComponent)
        {
            BurningParticleComponent->Activate();
        }
        return true;
    }

    // GE 移除（持续 Cue 结束）
    virtual bool OnRemove_Implementation(
        AActor* MyTarget,
        const FGameplayCueParameters& Parameters) override
    {
        // 停止燃烧粒子
        if (BurningParticleComponent)
        {
            BurningParticleComponent->Deactivate();
        }
        return true;
    }

protected:
    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UParticleSystemComponent> BurningParticleComponent;
};
```

---

## Cue 的注册与发现

Cue Notify 类通过 Tag 自动关联：类名必须匹配 Tag 结构，或在 `GameplayCueNotifyPaths` 中指定搜索路径。

引擎启动时，`UGameplayCueManager` 会扫描指定路径下的所有 `UGameplayCueNotify` 资产，建立 Tag 到 Cue 的映射表。

```ini
; DefaultGame.ini
[/Script/GameplayAbilities.AbilitySystemGlobals]
; 指定 Cue 资产搜索路径
+GameplayCueNotifyPaths="/Game/Abilities/GameplayCues"
```

---

## Cue 与网络

Cue 的设计原则：
- **服务器只发送 Cue 事件（Tag + Parameters）**，不执行视觉逻辑
- **客户端本地执行**，服务器不参与表现层
- GE 触发的 Cue 通过 GE 的网络同步传播，GA 手动触发的 Cue 通过 RPC 传播

```cpp
// 在非预测上下文中，可以用 LocalGameplayCues 跳过服务器
// （纯客户端特效，不需要服务器验证）
AbilitySystemComponent->ExecuteGameplayCueLocal(
    FGameplayTag::RequestGameplayTag("GameplayCue.Character.FootStep"),
    CueParams
);
```

---

## CueParameters 传递数据

```cpp
FGameplayCueParameters CueParams;
CueParams.Location         = ImpactPoint;        // 特效位置
CueParams.Normal           = ImpactNormal;       // 法线（用于对齐）
CueParams.Instigator       = Attacker;           // 攻击者
CueParams.EffectCauser     = AttackWeapon;       // 造成效果的对象
CueParams.SourceObject     = DamageAbility;      // 来源
CueParams.RawMagnitude     = DamageAmount;       // 数值（可用于调整特效强度）
CueParams.NormalizedMagnitude = DamageAmount / MaxDamage;  // 归一化值
```
