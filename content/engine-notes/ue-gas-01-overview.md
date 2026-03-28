---
title: "Unreal GAS 01｜Gameplay Ability System 总览：核心组件与设计思路"
slug: "ue-gas-01-overview"
date: "2026-03-28"
description: "GAS 是 Unreal 官方的技能框架，核心是五个组件：ASC、GA、GE、AT 和 GC。理解它们各自的职责和交互方式，是用好 GAS 的第一步。"
tags:
  - "Unreal"
  - "GAS"
  - "GameplayAbilitySystem"
  - "技能系统"
series: "Unreal Engine 架构与系统"
weight: 6090
---

GAS（Gameplay Ability System）是 Unreal 官方提供的技能和属性框架，最早用于《堡垒之夜》，后来开源。它解决了动作/RPG 游戏里最复杂的一批问题：技能的激活与取消、属性计算与修改、状态效果的叠加与过期、网络同步。直接上手 GAS 会感到陡峭——但这份复杂度对应的是它能处理的问题的复杂度。

---

## 五个核心组件

| 组件 | 全称 | 职责 |
|------|------|------|
| **ASC** | AbilitySystemComponent | 技能系统的入口，挂载在 Actor 上，持有其他所有组件 |
| **GA** | GameplayAbility | 一个技能的逻辑定义（激活、执行、取消） |
| **GE** | GameplayEffect | 属性修改和状态效果的描述（不执行逻辑，只描述数据） |
| **AT** | AbilityTask | 技能内部的异步操作（等待动画、等待输入、等待碰撞） |
| **GC** | GameplayCue | 视觉/音效反馈，与逻辑解耦，可在客户端独立执行 |

---

## 系统架构

```
Actor (Character / PlayerController)
  └─ AbilitySystemComponent (ASC)
       ├─ GrantedAbilities: [GA_Attack, GA_Dash, GA_Heal]
       ├─ ActiveGameplayEffects: [GE_Burning(3s), GE_Slow(1s)]
       ├─ AttributeSet: [Health=80, MaxHealth=100, Damage=25]
       └─ GameplayTags: [State.Burning, State.Grounded]
```

每个能够使用技能的 Actor 都需要挂载 `UAbilitySystemComponent`，并拥有至少一个 `UAttributeSet` 来存储属性。

---

## 最小化接入

```cpp
// 1. 让 Character 实现 IAbilitySystemInterface
UCLASS()
class AMyCharacter : public ACharacter, public IAbilitySystemInterface
{
    GENERATED_BODY()
public:
    virtual UAbilitySystemComponent* GetAbilitySystemComponent() const override
    {
        return AbilitySystemComponent;
    }

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UAbilitySystemComponent> AbilitySystemComponent;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UMyAttributeSet> AttributeSet;
};

// 2. 初始化 ASC
void AMyCharacter::PossessedBy(AController* NewController)
{
    Super::PossessedBy(NewController);

    // 服务器侧初始化
    AbilitySystemComponent->InitAbilityActorInfo(this, this);
}

void AMyCharacter::OnRep_PlayerState()
{
    Super::OnRep_PlayerState();

    // 客户端侧初始化（PlayerState 拥有 ASC 时）
    AbilitySystemComponent->InitAbilityActorInfo(this, this);
}
```

---

## OwnerActor vs AvatarActor

ASC 的 `InitAbilityActorInfo` 需要两个参数：

- **OwnerActor**：逻辑拥有者（通常是 PlayerState，方便持久化）
- **AvatarActor**：物理表现者（通常是 Character 本体，动画、碰撞都在这里）

```
玩家角色典型设置：
  OwnerActor  = AMyPlayerState  （持有 ASC，跨死亡重生保留）
  AvatarActor = AMyCharacter    （当前控制的角色模型）

AI 角色简化设置：
  OwnerActor  = AMyAICharacter
  AvatarActor = AMyAICharacter  （两者相同）
```

将 ASC 挂在 PlayerState 上的好处：玩家死亡重生时，技能冷却、Buff 状态可以保留；挂在 Character 上则随死亡重置。

---

## 技能授予与激活

```cpp
// 授予技能（通常在服务器的 PossessedBy 中）
void AMyCharacter::GiveDefaultAbilities()
{
    if (!HasAuthority()) return;

    for (TSubclassOf<UGameplayAbility>& Ability : DefaultAbilities)
    {
        AbilitySystemComponent->GiveAbility(
            FGameplayAbilitySpec(Ability, 1, INDEX_NONE, this)
        );
    }
}

// 通过 Tag 激活技能
AbilitySystemComponent->TryActivateAbilitiesByTag(
    FGameplayTagContainer(FGameplayTag::RequestGameplayTag("Ability.Attack"))
);

// 通过 InputID 激活（绑定输入时常用）
AbilitySystemComponent->TryActivateAbilityByClass(UGA_Attack::StaticClass());
```

---

## 为什么用 GAS 而不是自己写

| 问题 | 自己写 | GAS |
|------|--------|-----|
| 技能打断 | 需要手写状态机 | Tag 系统自动处理 |
| 属性修改叠加 | 容易出 bug | GE 栈管理 |
| 网络同步 | 大量手写 RPC | ASC 自带同步 |
| 预测回滚 | 极难实现 | 内置预测机制 |
| 冷却/费用 | 自己管理 | GA 内置 Cost/Cooldown GE |

GAS 的主要成本是学习曲线和初始接入复杂度。对于需要丰富技能系统的项目，长期来看是值得的。

---

## 系列概览

本系列将依次覆盖：

1. **总览**（本篇）—— 架构和五个核心组件
2. **AttributeSet**—— 属性定义、初始化、网络同步
3. **GameplayEffect**—— 属性修改、Duration、Stack
4. **GameplayAbility**—— 技能生命周期、Cost、Cooldown
5. **AbilityTask**—— 异步操作、等待动画蒙太奇
6. **GameplayTag**—— Tag 系统、技能条件判断
7. **GameplayCue**—— 视觉音效解耦、本地执行
8. **网络预测**—— 客户端预测、服务器验证
