---
title: "Unreal GAS 06｜GameplayTag：技能条件、状态管理与查询"
slug: "ue-gas-06-gameplay-tag"
date: "2026-03-28"
description: "GameplayTag 是 GAS 的神经系统，用层级字符串描述游戏状态。技能的激活条件、互斥关系、Buff 判断都依赖 Tag。正确使用 Tag 能大幅降低技能系统的耦合度。"
tags:
  - "Unreal"
  - "GAS"
  - "GameplayTag"
  - "状态管理"
series: "Unreal Engine 架构与系统"
weight: 6140
---

GameplayTag 是 GAS 中描述游戏状态的标准方式。它的本质是一个层级字符串（如 `State.Burning`、`Ability.Attack`），引擎内部用哈希快速比较，支持父标签匹配。几乎所有 GAS 组件的条件判断都基于 Tag。

---

## Tag 的层级结构

```
State
  State.Burning        ← 燃烧
  State.Stunned        ← 眩晕
  State.Grounded       ← 在地面
  State.Airborne       ← 在空中

Ability
  Ability.Attack       ← 普通攻击
  Ability.Dash         ← 冲刺
  Ability.Skill
    Ability.Skill.Fire ← 火系技能

Cooldown
  Cooldown.Attack
  Cooldown.Skill.Fire

Data
  Data.Damage          ← SetByCaller 数据 Tag
  Data.HealAmount
```

父标签可以匹配所有子标签：查询 `State` 时，`State.Burning` 和 `State.Stunned` 都匹配。

---

## 注册 Tag

Tag 在 `DefaultGameplayTags.ini` 中集中注册：

```ini
; Config/DefaultGameplayTags.ini
[/Script/GameplayTags.GameplayTagsSettings]
+GameplayTagList=(Tag="State.Burning",DevComment="燃烧状态")
+GameplayTagList=(Tag="State.Stunned",DevComment="眩晕状态")
+GameplayTagList=(Tag="State.Grounded",DevComment="在地面")
+GameplayTagList=(Tag="Ability.Attack",DevComment="普通攻击能力Tag")
+GameplayTagList=(Tag="Cooldown.Attack",DevComment="普通攻击冷却Tag")
+GameplayTagList=(Tag="Data.Damage",DevComment="SetByCaller伤害数据")
```

也可以在 C++ 中用宏声明 Native Tag（推荐，避免字符串拼写错误）：

```cpp
// MyGameplayTags.h
namespace MyGameplayTags
{
    UE_DECLARE_GAMEPLAY_TAG_EXTERN(State_Burning)
    UE_DECLARE_GAMEPLAY_TAG_EXTERN(State_Stunned)
    UE_DECLARE_GAMEPLAY_TAG_EXTERN(Ability_Attack)
}

// MyGameplayTags.cpp
namespace MyGameplayTags
{
    UE_DEFINE_GAMEPLAY_TAG(State_Burning, "State.Burning")
    UE_DEFINE_GAMEPLAY_TAG(State_Stunned, "State.Stunned")
    UE_DEFINE_GAMEPLAY_TAG(Ability_Attack, "Ability.Attack")
}

// 使用
AbilitySystemComponent->HasMatchingGameplayTag(MyGameplayTags::State_Burning);
```

---

## ASC 上的 Tag 来源

ASC 持有的 Tag 来自三个来源：

1. **GE 的 GrantedTags**：GE 激活时添加，GE 移除时自动清除
2. **手动添加**：`AddLooseGameplayTag()` / `RemoveLooseGameplayTag()`
3. **GA 的 ActivationOwnedTags**：技能激活期间持有，技能结束自动清除

```cpp
// 手动添加/移除 Tag（不受 GE 管理，需手动清理）
AbilitySystemComponent->AddLooseGameplayTag(MyGameplayTags::State_Grounded);
AbilitySystemComponent->RemoveLooseGameplayTag(MyGameplayTags::State_Grounded);

// 通过 GE 添加（推荐，自动管理生命周期）
// GE_Burning 的 GrantedTags = State.Burning
// 应用 GE 时自动加 Tag，GE 过期/移除时自动去 Tag
```

---

## Tag 查询

```cpp
// 单 Tag 查询
bool bIsBurning = AbilitySystemComponent->HasMatchingGameplayTag(
    MyGameplayTags::State_Burning);

// 多 Tag 查询（所有 Tag 都必须匹配）
FGameplayTagContainer RequiredTags;
RequiredTags.AddTag(MyGameplayTags::State_Grounded);
RequiredTags.AddTag(MyGameplayTags::State_Burning);
bool bGroundedAndBurning = AbilitySystemComponent->HasAllMatchingGameplayTags(RequiredTags);

// 任意 Tag 匹配
bool bAnyMatch = AbilitySystemComponent->HasAnyMatchingGameplayTags(RequiredTags);

// 父标签匹配：查询 "Ability" 会匹配 "Ability.Attack"、"Ability.Dash" 等
FGameplayTag AbilityParent = FGameplayTag::RequestGameplayTag("Ability");
bool bHasAnyAbility = AbilitySystemComponent->HasMatchingGameplayTag(AbilityParent);
```

---

## GA 的 Tag 条件配置

```cpp
// 在 GA 的构造函数或蓝图默认值中配置
UGA_Attack::UGA_Attack()
{
    // 这个技能自身的 Tag
    AbilityTags.AddTag(MyGameplayTags::Ability_Attack);

    // 激活时，ASC 上必须有这些 Tag
    ActivationRequiredTags.AddTag(MyGameplayTags::State_Grounded);

    // 激活时，ASC 上不能有这些 Tag（有任何一个就无法激活）
    ActivationBlockedTags.AddTag(MyGameplayTags::State_Stunned);
    ActivationBlockedTags.AddTag(MyGameplayTags::State_Airborne);

    // 技能激活时，具有这些 Tag 的其他技能会被取消
    CancelAbilitiesWithTag.AddTag(FGameplayTag::RequestGameplayTag("Ability.Dash"));

    // 技能激活期间，自动添加到 ASC 的 Tag（技能结束自动移除）
    ActivationOwnedTags.AddTag(FGameplayTag::RequestGameplayTag("State.Attacking"));
}
```

---

## 监听 Tag 变化

```cpp
// 监听特定 Tag 的变化（用于 UI 显示、状态同步）
void AMyCharacter::SetupTagListeners()
{
    AbilitySystemComponent->RegisterGameplayTagEvent(
        MyGameplayTags::State_Burning,
        EGameplayTagEventType::NewOrRemoved  // 添加或移除时触发
    ).AddUObject(this, &AMyCharacter::OnBurningTagChanged);
}

void AMyCharacter::OnBurningTagChanged(const FGameplayTag Tag, int32 NewCount)
{
    // NewCount > 0 表示 Tag 被持有
    bool bIsBurning = NewCount > 0;
    UpdateBurningVFX(bIsBurning);
}
```

---

## GameplayTagQuery：复杂查询

对于更复杂的条件（AND/OR/NOT 组合），使用 `FGameplayTagQuery`：

```cpp
// 构造：必须在地面 AND (没有眩晕 OR 有超级护甲)
FGameplayTagQuery Query = FGameplayTagQuery::BuildQuery(
    FGameplayTagQueryExpression()
    .AllTagsMatch()
    .AddTag(MyGameplayTags::State_Grounded)
    .AddExpr(FGameplayTagQueryExpression()
        .AnyTagsMatch()
        .AddTag(MyGameplayTags::State_Invulnerable)
        .AddExpr(FGameplayTagQueryExpression()
            .NoTagsMatch()
            .AddTag(MyGameplayTags::State_Stunned)
        )
    )
);

bool bCanAct = Query.Matches(*AbilitySystemComponent->GetOwnedGameplayTags());
```
