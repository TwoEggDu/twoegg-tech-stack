---
title: "Unreal GAS 03｜GameplayEffect：属性修改、Duration 与 Stack"
slug: "ue-gas-03-gameplay-effect"
date: "2026-03-28"
description: "GameplayEffect 是 GAS 中属性修改的载体，分 Instant、Duration 和 Infinite 三类。理解 Modifier、Stack 和 Period 的语义，才能正确实现伤害、Buff 和 Debuff。"
tags:
  - "Unreal"
  - "GAS"
  - "GameplayEffect"
  - "Buff"
series: "Unreal Engine 架构与系统"
weight: 6110
---

GameplayEffect（GE）是 GAS 中"做什么"的描述——它不执行游戏逻辑，只是一份数据说明：对哪些属性做什么修改、持续多久、叠加几层。所有属性的改变（伤害、回血、Buff）都应该通过 GE 进行，而不是直接调用 `SetHealth()`。

---

## 三种 Duration 类型

| 类型 | 说明 | 典型用途 |
|------|------|---------|
| **Instant** | 立即应用，永久修改 BaseValue | 造成伤害、消耗资源、永久升级 |
| **Duration** | 持续一段时间后自动移除 | 中毒、眩晕、速度加成 |
| **Infinite** | 不自动移除，需手动移除 | 装备加成、被动技能、持久状态 |

```
GE_FireDamage       → Instant    → Health.BaseValue -= 50
GE_Burning          → Duration(5s) → Health.BaseValue -= 5（每秒）
GE_ArmorBonus       → Infinite   → Armor.BaseValue += 20（装备时）
```

---

## Modifier：属性修改规则

每个 GE 可以包含多个 Modifier，每个 Modifier 描述对一个属性的一种操作：

```
Modifier 结构：
  Attribute:    Health                 ← 修改哪个属性
  ModifierOp:   Add / Multiply / Override  ← 操作类型
  Magnitude:    -50                    ← 数值（支持 ScalableFloat、曲线等）
```

**操作类型说明**：
- `Add`：CurrentValue += Magnitude（最常用）
- `Multiply`：CurrentValue *= Magnitude（用于百分比加成，Magnitude = 1.1 表示 +10%）
- `Override`：CurrentValue = Magnitude（强制覆盖）

多个 GE 同时作用时，计算顺序：
```
FinalValue = (BaseValue + AllAddModifiers) * AllMultiplyModifiers
```

---

## 在蓝图中配置 GE

```
GE_Burning (UGameplayEffect)
  DurationPolicy: HasDuration
  DurationMagnitude: 5.0
  Period: 1.0          ← 每 1 秒触发一次 Modifier
  Modifiers:
    [0] Attribute: MyAttributeSet.Health
        ModifierOp: Add
        Magnitude: -10   ← 每秒扣 10 点血
  GameplayTags:
    GrantedTags: State.Burning   ← 应用期间持有此 Tag
```

---

## 在 C++ 中应用 GE

```cpp
// 方式一：从 ASC 直接应用
void AMyCharacter::ApplyBurningEffect(AActor* Instigator)
{
    if (!AbilitySystemComponent || !BurningEffect) return;

    FGameplayEffectContextHandle Context = AbilitySystemComponent->MakeEffectContext();
    Context.AddInstigator(Instigator, Instigator);

    FGameplayEffectSpecHandle Spec = AbilitySystemComponent->MakeOutgoingSpec(
        BurningEffect, 1.f, Context);

    if (Spec.IsValid())
    {
        FActiveGameplayEffectHandle Handle =
            AbilitySystemComponent->ApplyGameplayEffectSpecToSelf(*Spec.Data.Get());

        // Handle 可以用来手动移除该 GE（Infinite 类型常用）
        // AbilitySystemComponent->RemoveActiveGameplayEffect(Handle);
    }
}

// 方式二：在 GameplayAbility 内部应用（更常用）
void UGA_Attack::ActivateAbility(...)
{
    // 对目标应用 GE
    FGameplayEffectSpecHandle DamageSpec = MakeOutgoingGameplayEffectSpec(DamageEffect, GetAbilityLevel());

    // 通过 AbilitySystemBlueprintLibrary 也可以
    ApplyGameplayEffectSpecToTarget(CurrentSpecHandle, CurrentActorInfo, CurrentActivationInfo,
        DamageSpec, TargetData);
}
```

---

## Stack：叠加机制

Duration/Infinite 类型的 GE 可以配置叠加行为：

```
StackingType:
  AggregatedBySource  → 同一个来源（不同施加者各自独立叠加）
  AggregatedByTarget  → 同一个目标（不管来源，共用一个 Stack）

StackLimitCount:      3      → 最多叠 3 层
StackDurationRefreshPolicy: RefreshOnSuccessfulApplication → 每次叠加刷新持续时间
StackPeriodResetPolicy:     ResetOnSuccessfulApplication   → 每次叠加重置周期计时器
```

典型应用：中毒可以叠 5 层，每层每秒扣 5 血，叠加时刷新持续时间。

---

## 条件触发：Conditional GameplayEffect

GE 可以在条件满足时触发另一个 GE：

```cpp
// 示例：当 Health < 20% 时，自动触发"濒死"Buff
// 在 GE_LowHealthCheck 的 ConditionalGameplayEffects 中：
//   ConditionRequiredTags: (空，无需额外条件)
//   GameplayEffectClass: GE_NearDeathBuff
```

---

## SetByCaller：运行时设置数值

有时 GE 的数值不能在设计时确定（比如伤害值由攻击力计算），用 SetByCaller 传入：

```cpp
// 设置 Magnitude
FGameplayEffectSpecHandle DamageSpec = MakeOutgoingGameplayEffectSpec(DamageEffect, 1.f);
DamageSpec.Data->SetSetByCallerMagnitude(
    FGameplayTag::RequestGameplayTag("Data.Damage"),
    CalculatedDamage  // 运行时计算的值
);

// GE 的 Modifier 配置：
//   Magnitude Source: SetByCaller
//   Data Tag: Data.Damage
```

---

## 移除 GE

```cpp
// 通过 Handle 移除（Infinite GE 常用）
AbilitySystemComponent->RemoveActiveGameplayEffect(ArmorBonusHandle);

// 通过 Tag 移除（移除所有带此 Tag 的 GE）
FGameplayTagContainer TagsToRemove;
TagsToRemove.AddTag(FGameplayTag::RequestGameplayTag("State.Burning"));
AbilitySystemComponent->RemoveActiveGameplayEffectBySourceEffect(
    nullptr, // 任意来源
    TagsToRemove
);

// 更常用的方式：RemoveActiveEffectsWithGrantedTags
AbilitySystemComponent->RemoveActiveEffectsWithGrantedTags(TagsToRemove);
```
