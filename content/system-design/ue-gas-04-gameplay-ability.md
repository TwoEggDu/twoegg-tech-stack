---
title: "Unreal GAS 04｜GameplayAbility：技能生命周期、Cost 与 Cooldown"
slug: "ue-gas-04-gameplay-ability"
date: "2026-03-28"
description: "GameplayAbility 定义技能的激活、执行和结束逻辑。Cost GE 和 Cooldown GE 是 GAS 内置的资源消耗与冷却机制，正确使用它们才能与预测系统协同工作。"
tags:
  - "Unreal"
  - "GAS"
  - "GameplayAbility"
  - "技能"
series: "Unreal Engine 架构与系统"
weight: 6120
---

GameplayAbility（GA）是 GAS 中技能逻辑的容器。一个技能从激活到结束，经历一套完整的生命周期回调。正确管理这个生命周期，是技能系统稳定运行的关键。

---

## 技能生命周期

```
TryActivateAbility()
    │
    ├─ CanActivateAbility() 检查
    │    ├─ 是否已激活（取决于 InstancingPolicy）
    │    ├─ Tag 条件满足（RequiredTags、BlockedTags）
    │    ├─ Cost 是否足够（调用 CheckCost）
    │    └─ Cooldown 是否结束（调用 CheckCooldown）
    │
    ▼
ActivateAbility()         ← 技能主逻辑入口（C++ 或蓝图重写）
    │
    ├─ CommitAbility()    ← 扣除 Cost + 开始 Cooldown（通常在激活前期调用）
    │
    ├─ [技能执行中]        ← AbilityTask 等异步操作
    │
    └─ EndAbility()       ← 技能结束（成功完成或取消都必须调用）
```

---

## 最小化 GA 实现

```cpp
UCLASS()
class UGA_Attack : public UGameplayAbility
{
    GENERATED_BODY()
public:
    UGA_Attack()
    {
        // 网络执行策略
        NetExecutionPolicy = EGameplayAbilityNetExecutionPolicy::LocalPredicted;

        // 实例化策略：每次激活创建新实例
        InstancingPolicy = EGameplayAbilityInstancingPolicy::InstancedPerExecution;
    }

    virtual void ActivateAbility(
        const FGameplayAbilitySpecHandle Handle,
        const FGameplayAbilityActorInfo* ActorInfo,
        const FGameplayAbilityActivationInfo ActivationInfo,
        const FGameplayEventData* TriggerEventData) override
    {
        // 1. 提交技能（扣除 Cost，开始 Cooldown）
        if (!CommitAbility(Handle, ActorInfo, ActivationInfo))
        {
            EndAbility(Handle, ActorInfo, ActivationInfo, true, true);
            return;
        }

        // 2. 播放攻击动画（通过 AbilityTask）
        UAbilityTask_PlayMontageAndWait* MontageTask =
            UAbilityTask_PlayMontageAndWait::CreatePlayMontageAndWaitProxy(
                this, NAME_None, AttackMontage, 1.f);

        MontageTask->OnCompleted.AddDynamic(this, &UGA_Attack::OnMontageCompleted);
        MontageTask->OnCancelled.AddDynamic(this, &UGA_Attack::OnMontageCancelled);
        MontageTask->ReadyForActivation();

        // 不在这里调用 EndAbility，等动画播完再结束
    }

    UFUNCTION()
    void OnMontageCompleted()
    {
        EndAbility(CurrentSpecHandle, CurrentActorInfo, CurrentActivationInfo, true, false);
    }

    UFUNCTION()
    void OnMontageCancelled()
    {
        EndAbility(CurrentSpecHandle, CurrentActorInfo, CurrentActivationInfo, true, true);
    }

protected:
    UPROPERTY(EditDefaultsOnly)
    UAnimMontage* AttackMontage;
};
```

---

## InstancingPolicy

| 策略 | 说明 | 适用场景 |
|------|------|---------|
| `NonInstanced` | 所有激活共用一个 CDO 实例 | 简单的即时技能，不保存运行时状态 |
| `InstancedPerActor` | 每个 Actor 一个实例，重用 | 需要保存状态的技能（充能、蓄力） |
| `InstancedPerExecution` | 每次激活创建新实例 | 最安全，适合大多数技能 |

---

## NetExecutionPolicy

| 策略 | 执行位置 | 说明 |
|------|---------|------|
| `LocalOnly` | 仅本地客户端 | 纯客户端效果（UI、音效） |
| `LocalPredicted` | 本地预测 + 服务器验证 | 大多数玩家控制的技能 |
| `ServerOnly` | 仅服务器 | AI 技能、服务器逻辑 |
| `ServerInitiated` | 服务器发起，本地执行 | 服务器触发、客户端播放 |

---

## Cost GE：技能费用

在 GA 的 `CostGameplayEffectClass` 中配置一个 Instant GE：

```
GE_AttackCost (UGameplayEffect)
  DurationPolicy: Instant
  Modifiers:
    [0] Attribute: MyAttributeSet.Stamina
        ModifierOp: Add
        Magnitude: -20   ← 消耗 20 点体力
```

`CommitAbility()` 内部会：
1. 调用 `CheckCost()` 判断 Stamina >= 20
2. 如果满足，应用这个 GE，扣除 Stamina

```cpp
// 手动检查 Cost（不消耗）
if (!CheckCost(Handle, ActorInfo))
{
    // 体力不足，给玩家提示
    return;
}
```

---

## Cooldown GE：技能冷却

在 GA 的 `CooldownGameplayEffectClass` 中配置一个 Duration GE：

```
GE_AttackCooldown (UGameplayEffect)
  DurationPolicy: HasDuration
  DurationMagnitude: 1.5    ← 1.5 秒冷却
  GrantedTags:
    Cooldown.Attack          ← 冷却期间持有此 Tag
```

GA 的 `CooldownTags` 配置为 `Cooldown.Attack`，`CommitAbility()` 会检查这个 Tag 是否存在来判断是否在冷却中。

```cpp
// 查询剩余冷却时间
float TimeRemaining = 0.f;
float CooldownDuration = 0.f;
const FGameplayTagContainer* CooldownTags = GetCooldownTags();
if (CooldownTags && CooldownTags->Num() > 0)
{
    AbilitySystemComponent->GetActiveEffectsTimeRemainingAndDuration(
        FGameplayEffectQuery::MakeQuery_MatchAnyOwningTags(*CooldownTags),
        TimeRemaining,
        CooldownDuration
    );
}
```

---

## 技能的 Tag 条件

```
GA_Attack
  AbilityTags:         Ability.Attack        ← 这个技能自身的 Tag
  ActivationRequiredTags: State.Grounded     ← 必须有此 Tag 才能激活（在地面上）
  ActivationBlockedTags:  State.Stunned      ← 有此 Tag 时无法激活（被眩晕）
  BlockAbilitiesWithTag:  Ability.Attack     ← 激活时阻止同 Tag 的技能
  CancelAbilitiesWithTag: Ability.Dash       ← 激活时取消 Dash 技能
```

---

## 技能的输入绑定

```cpp
// 将技能与输入 ID 绑定
AbilitySystemComponent->GiveAbility(
    FGameplayAbilitySpec(GA_Attack::StaticClass(), 1,
        static_cast<int32>(EMyAbilityInputID::Attack),  // InputID
        this)
);

// 在 AMyCharacter 的输入处理中
void AMyCharacter::OnAttackPressed()
{
    AbilitySystemComponent->AbilityLocalInputPressed(
        static_cast<int32>(EMyAbilityInputID::Attack));
}

void AMyCharacter::OnAttackReleased()
{
    AbilitySystemComponent->AbilityLocalInputReleased(
        static_cast<int32>(EMyAbilityInputID::Attack));
}
```
