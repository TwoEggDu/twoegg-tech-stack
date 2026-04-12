---
title: "Unreal GAS 05｜AbilityTask：技能内部的异步操作"
slug: "ue-gas-05-ability-task"
date: "2026-03-28"
description: "AbilityTask 是 GAS 中处理异步操作的机制，让技能可以等待动画结束、等待玩家输入、等待碰撞命中，而不阻塞游戏线程。理解 AT 的生命周期是写好复杂技能的关键。"
tags:
  - "Unreal"
  - "GAS"
  - "AbilityTask"
  - "异步"
series: "Unreal Engine 架构与系统"
weight: 6130
---

技能逻辑往往不是瞬间完成的：需要等动画播放到特定帧才触发伤害，需要等玩家再次按键确认目标，需要等投射物飞行命中目标。`AbilityTask` 是 GAS 处理这类**技能内部异步操作**的机制。

---

## AbilityTask 的本质

AbilityTask 是一个轻量级的 UObject，它在激活时启动一个异步操作，完成时通过动态多播委托通知技能。技能通过监听这些委托决定后续流程：

```
ActivateAbility()
    │
    ▼ 创建 AbilityTask
UAbilityTask_PlayMontageAndWait::CreatePlayMontageAndWaitProxy(...)
    │
    ▼ ReadyForActivation() 启动任务
    │
    [等待动画播放...]
    │
    ├─ OnCompleted.Broadcast()   → 动画正常结束
    ├─ OnCancelled.Broadcast()   → 动画被打断
    ├─ OnInterrupted.Broadcast() → 被其他动画打断
    └─ OnBlendOut.Broadcast()    → 动画开始淡出
```

---

## 常用内置 AbilityTask

### 播放蒙太奇并等待

```cpp
UAbilityTask_PlayMontageAndWait* Task =
    UAbilityTask_PlayMontageAndWait::CreatePlayMontageAndWaitProxy(
        this,           // OwningAbility
        NAME_None,      // TaskInstanceName（用于同时运行多个 AT 时区分）
        AttackMontage,
        1.0f,           // PlayRate
        NAME_None,      // StartSection
        false           // bStopWhenAbilityEnds
    );

Task->OnCompleted.AddDynamic(this, &ThisClass::OnAttackCompleted);
Task->OnCancelled.AddDynamic(this, &ThisClass::OnAttackCancelled);
Task->ReadyForActivation();
```

### 等待 GameplayEvent

技能等待一个 GameplayEvent（比如动画通知触发的伤害时机）：

```cpp
UAbilityTask_WaitGameplayEvent* EventTask =
    UAbilityTask_WaitGameplayEvent::WaitGameplayEvent(
        this,
        FGameplayTag::RequestGameplayTag("Event.Attack.HitMoment"),
        nullptr,  // OptionalExternalTarget
        true      // TriggerOnce
    );

EventTask->EventReceived.AddDynamic(this, &ThisClass::OnHitMoment);
EventTask->ReadyForActivation();

// 在 AnimNotify 中发送 GameplayEvent
void UAN_AttackHit::NotifyBegin(USkeletalMeshComponent* MeshComp, ...)
{
    if (AActor* Owner = MeshComp->GetOwner())
    {
        UAbilitySystemBlueprintLibrary::SendGameplayEventToActor(
            Owner,
            FGameplayTag::RequestGameplayTag("Event.Attack.HitMoment"),
            FGameplayEventData()
        );
    }
}
```

### 等待目标选择

```cpp
// 等待玩家选择目标位置（AOE 技能常用）
UAbilityTask_WaitTargetData* TargetTask =
    UAbilityTask_WaitTargetData::WaitTargetData(
        this,
        NAME_None,
        EGameplayTargetingConfirmation::UserConfirmed,  // 玩家点击确认
        AGameplayAbilityTargetActor_GroundTrace::StaticClass()
    );

TargetTask->ValidData.AddDynamic(this, &ThisClass::OnTargetSelected);
TargetTask->Cancelled.AddDynamic(this, &ThisClass::OnTargetCancelled);
TargetTask->ReadyForActivation();
```

### 等待属性变化

```cpp
// 等待 Health 降到某个阈值
UAbilityTask_WaitAttributeChange* AttrTask =
    UAbilityTask_WaitAttributeChange::WaitForAttributeChangeRatioThreshold(
        this,
        UMyAttributeSet::GetHealthAttribute(),
        EWaitAttributeChangeComparison::LessThan,
        0.3f,   // 30%
        true    // bTriggerOnce
    );

AttrTask->OnChange.AddDynamic(this, &ThisClass::OnLowHealth);
AttrTask->ReadyForActivation();
```

---

## 自定义 AbilityTask

```cpp
UCLASS()
class UAbilityTask_WaitProjectileHit : public UAbilityTask
{
    GENERATED_BODY()
public:
    // 创建函数（蓝图暴露的工厂方法）
    UFUNCTION(BlueprintCallable, Category = "Ability|Tasks",
        meta = (HidePin = "OwningAbility", DefaultToSelf = "OwningAbility",
                BlueprintInternalUseOnly = "true"))
    static UAbilityTask_WaitProjectileHit* WaitProjectileHit(
        UGameplayAbility* OwningAbility,
        AMyProjectile* Projectile);

    // 委托：命中时触发
    UPROPERTY(BlueprintAssignable)
    FProjectileHitDelegate OnHit;

    // AbilityTask 生命周期
    virtual void Activate() override;
    virtual void OnDestroy(bool AbilityEnded) override;

private:
    UPROPERTY()
    TObjectPtr<AMyProjectile> TrackedProjectile;

    UFUNCTION()
    void HandleProjectileHit(AActor* HitActor, FVector HitLocation);
};

void UAbilityTask_WaitProjectileHit::Activate()
{
    if (TrackedProjectile)
    {
        TrackedProjectile->OnHit.AddDynamic(this, &ThisClass::HandleProjectileHit);
    }
}

void UAbilityTask_WaitProjectileHit::HandleProjectileHit(AActor* HitActor, FVector HitLocation)
{
    if (ShouldBroadcastAbilityTaskDelegates())
    {
        OnHit.Broadcast(HitActor, HitLocation);
    }
    EndTask();  // 任务完成，自动销毁
}

void UAbilityTask_WaitProjectileHit::OnDestroy(bool AbilityEnded)
{
    if (TrackedProjectile)
    {
        TrackedProjectile->OnHit.RemoveDynamic(this, &ThisClass::HandleProjectileHit);
    }
    Super::OnDestroy(AbilityEnded);
}
```

---

## AbilityTask 的生命周期规则

1. **必须调用 `ReadyForActivation()`**，否则任务不会启动
2. **任务完成后调用 `EndTask()`**，它会自动清理任务
3. **技能结束时，所有未完成的 AT 自动销毁**（OnDestroy 被调用）
4. **检查 `ShouldBroadcastAbilityTaskDelegates()`** 再触发委托，防止技能已结束时还广播

---

## 多个 AbilityTask 并行

```cpp
// 同时等待蒙太奇结束和按键确认
void UGA_ChargeAttack::ActivateAbility(...)
{
    CommitAbility(...);

    // Task 1: 播放蓄力动画
    UAbilityTask_PlayMontageAndWait* MontageTask = ...;
    MontageTask->OnCompleted.AddDynamic(this, &ThisClass::OnChargeComplete);

    // Task 2: 等待松键
    UAbilityTask_WaitInputRelease* ReleaseTask =
        UAbilityTask_WaitInputRelease::WaitInputRelease(this, true);
    ReleaseTask->OnRelease.AddDynamic(this, &ThisClass::OnInputReleased);

    MontageTask->ReadyForActivation();
    ReleaseTask->ReadyForActivation();
    // 两个任务同时运行，哪个先触发就处理哪个
}
```
