---
title: "Unreal GAS 02｜AttributeSet：属性定义、初始化与网络同步"
slug: "ue-gas-02-attribute-set"
date: "2026-03-28"
description: "AttributeSet 是 GAS 中属性（生命值、攻击力、速度）的存储容器。理解 FGameplayAttributeData、PreAttributeChange 和 RepNotify 是正确使用属性系统的基础。"
tags:
  - "Unreal"
  - "GAS"
  - "AttributeSet"
  - "属性系统"
series: "Unreal Engine 架构与系统"
weight: 6100
---

GAS 的属性系统用 `UAttributeSet` 存储角色的数值属性（生命值、魔法值、攻击力等）。与直接在 Character 上定义 float 变量不同，AttributeSet 中的属性是第一类公民：它们自动与 GE（GameplayEffect）联动，支持网络同步，并且有完善的修改钩子。

---

## FGameplayAttributeData

每个属性的类型是 `FGameplayAttributeData`，它存储两个值：

- **BaseValue**：基础值，长期效果（装备加成、永久增益）修改的是 BaseValue
- **CurrentValue**：当前值，临时效果（Buff/Debuff）在 BaseValue 基础上计算

```cpp
UCLASS()
class UMyAttributeSet : public UAttributeSet
{
    GENERATED_BODY()
public:
    // ATTRIBUTE_ACCESSORS 宏生成 Getter/Setter/InitXxx 方法
    ATTRIBUTE_ACCESSORS(UMyAttributeSet, Health)
    ATTRIBUTE_ACCESSORS(UMyAttributeSet, MaxHealth)
    ATTRIBUTE_ACCESSORS(UMyAttributeSet, Damage)
    ATTRIBUTE_ACCESSORS(UMyAttributeSet, MoveSpeed)

    UPROPERTY(BlueprintReadOnly, ReplicatedUsing = OnRep_Health)
    FGameplayAttributeData Health;

    UPROPERTY(BlueprintReadOnly, ReplicatedUsing = OnRep_MaxHealth)
    FGameplayAttributeData MaxHealth;

    UPROPERTY(BlueprintReadOnly, ReplicatedUsing = OnRep_Damage)
    FGameplayAttributeData Damage;

    UPROPERTY(BlueprintReadOnly)
    FGameplayAttributeData MoveSpeed;  // 不需要 RepNotify 的属性
};
```

---

## ATTRIBUTE_ACCESSORS 宏

这个宏是 GAS 提供的工具宏，展开后生成四个方法：

```cpp
// 宏定义（GAS 源码中）
#define ATTRIBUTE_ACCESSORS(ClassName, PropertyName) \
    GAMEPLAYATTRIBUTE_PROPERTY_GETTER(ClassName, PropertyName) \
    GAMEPLAYATTRIBUTE_VALUE_GETTER(PropertyName) \
    GAMEPLAYATTRIBUTE_VALUE_SETTER(PropertyName) \
    GAMEPLAYATTRIBUTE_VALUE_INITTER(PropertyName)

// 展开后等价于：
static FGameplayAttribute GetHealthAttribute();      // 获取属性描述符
float GetHealth() const;                            // 读取 CurrentValue
void SetHealth(float NewVal);                       // 设置值（不通过 GE）
void InitHealth(float NewVal);                      // 初始化 BaseValue 和 CurrentValue
```

---

## 网络同步

属性需要在 `GetLifetimeReplicatedProps` 中注册，并实现 RepNotify：

```cpp
void UMyAttributeSet::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);

    DOREPLIFETIME_CONDITION_NOTIFY(UMyAttributeSet, Health, COND_None, REPNOTIFY_Always);
    DOREPLIFETIME_CONDITION_NOTIFY(UMyAttributeSet, MaxHealth, COND_None, REPNOTIFY_Always);
    DOREPLIFETIME_CONDITION_NOTIFY(UMyAttributeSet, Damage, COND_None, REPNOTIFY_Always);
}

// RepNotify：客户端收到更新时调用
void UMyAttributeSet::OnRep_Health(const FGameplayAttributeData& OldHealth)
{
    GAMEPLAYATTRIBUTE_REPNOTIFY(UMyAttributeSet, Health, OldHealth);
    // GAMEPLAYATTRIBUTE_REPNOTIFY 通知 ASC 预测系统，不要在这里自己更新 UI
    // UI 更新应该绑定 ASC 的 OnAttributeChanged 委托
}
```

**注意**：`REPNOTIFY_Always` 确保即使值未变化（预测回滚后）也触发 RepNotify，这对 GAS 的预测机制很重要。

---

## PreAttributeChange：修改前拦截

`PreAttributeChange` 在属性的 CurrentValue 被修改**前**调用，适合做值的范围限制：

```cpp
void UMyAttributeSet::PreAttributeChange(const FGameplayAttribute& Attribute, float& NewValue)
{
    Super::PreAttributeChange(Attribute, NewValue);

    // 限制 Health 不超过 MaxHealth
    if (Attribute == GetHealthAttribute())
    {
        NewValue = FMath::Clamp(NewValue, 0.f, GetMaxHealth());
    }

    // 限制 MoveSpeed 不低于最小值
    if (Attribute == GetMoveSpeedAttribute())
    {
        NewValue = FMath::Max(NewValue, 150.f);
    }
}
```

**注意**：`PreAttributeChange` 拦截的是 CurrentValue 的修改（临时效果），BaseValue 的变化需要在 `PostGameplayEffectExecute` 中处理。

---

## PostGameplayEffectExecute：GE 执行后处理

这是处理属性改变后置逻辑的正确位置（比如死亡判断、UI 通知）：

```cpp
void UMyAttributeSet::PostGameplayEffectExecute(const FGameplayEffectModCallbackData& Data)
{
    Super::PostGameplayEffectExecute(Data);

    FGameplayEffectContextHandle Context = Data.EffectSpec.GetContext();
    AActor* Instigator = Context.GetOriginalInstigator();
    AActor* EffectCauser = Context.GetEffectCauser();

    if (Data.EvaluatedData.Attribute == GetHealthAttribute())
    {
        // 限制 Health 范围
        SetHealth(FMath::Clamp(GetHealth(), 0.f, GetMaxHealth()));

        // 死亡判断（只在服务器执行）
        if (GetHealth() <= 0.f)
        {
            // 通知 Character 处理死亡逻辑
            if (AMyCharacter* OwnerCharacter = Cast<AMyCharacter>(GetOwningActor()))
            {
                OwnerCharacter->Die(Instigator);
            }
        }
    }
}
```

---

## 属性初始化

通常用 `DataTable` 初始化属性的初始值：

```cpp
// 在 GiveDefaultAbilities 附近，初始化属性
void AMyCharacter::InitializeAttributes()
{
    if (!DefaultAttributeEffect) return;

    FGameplayEffectContextHandle EffectContext = AbilitySystemComponent->MakeEffectContext();
    EffectContext.AddSourceObject(this);

    FGameplayEffectSpecHandle SpecHandle = AbilitySystemComponent->MakeOutgoingSpec(
        DefaultAttributeEffect, 1, EffectContext);

    if (SpecHandle.IsValid())
    {
        AbilitySystemComponent->ApplyGameplayEffectSpecToSelf(*SpecHandle.Data.Get());
    }
}
```

`DefaultAttributeEffect` 是一个 `GE_InitAttributes`，用 `Modifier` 将 DataTable 里的初始值赋给对应属性。

---

## Meta 属性

有些属性不需要被持久化，只用于传递计算中间值，称为 Meta 属性：

```cpp
// Damage 是一个 Meta 属性：GE 写入伤害值，PostExecute 里用它扣 Health
UPROPERTY()
FGameplayAttributeData Damage;  // 不复制，不持久化

void UMyAttributeSet::PostGameplayEffectExecute(...)
{
    if (Data.EvaluatedData.Attribute == GetDamageAttribute())
    {
        float LocalDamage = GetDamage();
        SetDamage(0.f);  // 立即清零，只用于这次计算

        float NewHealth = GetHealth() - LocalDamage;
        SetHealth(FMath::Clamp(NewHealth, 0.f, GetMaxHealth()));
    }
}
```
