---
title: "软件工程基础 04｜开闭原则（OCP）：对扩展开放，对修改封闭"
description: "OCP 说的是：好的代码设计应该让你在增加新功能时，不需要修改已经存在并运行良好的代码。这篇通过技能系统和道具系统的完整案例，解释 OCP 在游戏开发中的实际应用。"
slug: "solid-sw-04-ocp"
weight: 707
tags:
  - 软件工程
  - SOLID
  - OCP
  - 开闭原则
  - 代码质量
series: "软件工程基础与 SOLID 原则"
---

> **OCP（Open/Closed Principle）：软件实体（类、模块、函数）应该对扩展开放，对修改封闭。**
>
> 换句话说：增加新功能，应该通过**增加新代码**实现，而不是**修改旧代码**。

这条原则听起来像矛盾：既然需要增加功能，怎么可能不修改代码？

答案是：修改**行为**，不修改**逻辑判断的框架**。当你的扩展点设计正确时，增加一个新功能就像"插入一块新拼图"，而不是"重新设计整个拼图框架"。

---

## 违反 OCP 的典型场景：每次加新技能都要改核心代码

假设你在做一个角色扮演游戏，战斗系统里有一个技能释放函数：

```csharp
// v1：一开始只有火球
public class SkillSystem : MonoBehaviour
{
    public void UseSkill(string skillName, Character caster, Character target)
    {
        if (skillName == "Fireball")
        {
            int damage = caster.MagicPower * 3;
            target.TakeDamage(damage, DamageType.Fire);
            SpawnEffect("FireballEffect", target.transform.position);
        }
    }
}
```

策划加了冰锥技能：

```csharp
// v2：加了冰锥，修改了原来的代码
public void UseSkill(string skillName, Character caster, Character target)
{
    if (skillName == "Fireball")
    {
        int damage = caster.MagicPower * 3;
        target.TakeDamage(damage, DamageType.Fire);
        SpawnEffect("FireballEffect", target.transform.position);
    }
    else if (skillName == "IceLance")
    {
        int damage = caster.MagicPower * 2;
        target.TakeDamage(damage, DamageType.Ice);
        target.ApplyStatus(StatusEffect.Slow, duration: 3f);
        SpawnEffect("IceLanceEffect", target.transform.position);
    }
}
```

又加了治疗、召唤、范围技能……

```csharp
// v10：已经是灾难
public void UseSkill(string skillName, Character caster, Character target)
{
    if (skillName == "Fireball") { /* 15 行 */ }
    else if (skillName == "IceLance") { /* 12 行 */ }
    else if (skillName == "Heal") { /* 10 行 */ }
    else if (skillName == "Summon") { /* 25 行，还有嵌套 if */ }
    else if (skillName == "AoEStorm") { /* 20 行，还需要找所有目标 */ }
    else if (skillName == "Teleport") { /* 8 行 */ }
    else if (skillName == "Shield") { /* 15 行 */ }
    // 一共 200+ 行，每次加技能都要改这个文件
}
```

**这里的问题**：

1. 每次新增技能，必须修改 `UseSkill` 函数——这是一个高风险操作，可能意外影响已有技能的逻辑
2. 这个函数越来越长，越来越难理解和测试
3. 在多人协作时，两个人同时加技能会产生代码冲突
4. 无法在不改变框架代码的情况下，让策划通过配置表驱动技能

---

## OCP 的解决方案：把变化封装在实现里，把稳定留在接口上

**核心思路**：把"技能是什么"和"技能系统如何调用技能"分离开。

技能系统框架是**稳定的**——它不关心技能具体做什么，它只知道"我有一个技能，调用它"。

每个具体技能的实现是**可变的**——每次加新技能，只需要加一个新的实现，不碰框架。

```csharp
// 第一步：定义技能的抽象接口（稳定，不会再改）
public abstract class SkillBase : ScriptableObject
{
    public string skillName;
    public Sprite icon;
    public int manaCost;
    public float cooldown;

    // 子类实现具体逻辑
    public abstract void Execute(Character caster, Character target);
    public abstract bool CanExecute(Character caster, Character target);
}

// 第二步：技能系统框架只依赖抽象（对修改封闭）
public class SkillSystem : MonoBehaviour
{
    private Dictionary<string, SkillBase> skills = new();

    public void RegisterSkill(SkillBase skill)
    {
        skills[skill.skillName] = skill;
    }

    public void UseSkill(string skillName, Character caster, Character target)
    {
        if (!skills.TryGetValue(skillName, out SkillBase skill))
        {
            Debug.LogWarning($"Skill '{skillName}' not found.");
            return;
        }

        if (!skill.CanExecute(caster, target))
            return;

        if (caster.CurrentMana < skill.manaCost)
            return;

        caster.ConsumeMana(skill.manaCost);
        skill.Execute(caster, target);
    }
}
```

```csharp
// 第三步：每个技能是一个独立的 ScriptableObject 子类（对扩展开放）
// 加火球技能：只需要新建一个文件，不碰框架代码
[CreateAssetMenu(menuName = "Skills/Fireball")]
public class FireballSkill : SkillBase
{
    [SerializeField] private int damageMultiplier = 3;
    [SerializeField] private GameObject effectPrefab;

    public override bool CanExecute(Character caster, Character target)
    {
        return target != null && Vector3.Distance(caster.transform.position, target.transform.position) < 15f;
    }

    public override void Execute(Character caster, Character target)
    {
        int damage = caster.MagicPower * damageMultiplier;
        target.TakeDamage(damage, DamageType.Fire);
        GameObject.Instantiate(effectPrefab, target.transform.position, Quaternion.identity);
    }
}

// 加冰锥技能：同样，新建一个文件，不碰框架代码
[CreateAssetMenu(menuName = "Skills/IceLance")]
public class IceLanceSkill : SkillBase
{
    [SerializeField] private int damageMultiplier = 2;
    [SerializeField] private float slowDuration = 3f;

    public override bool CanExecute(Character caster, Character target)
    {
        return target != null;
    }

    public override void Execute(Character caster, Character target)
    {
        int damage = caster.MagicPower * damageMultiplier;
        target.TakeDamage(damage, DamageType.Ice);
        target.ApplyStatus(StatusEffect.Slow, slowDuration);
    }
}

// 加治疗技能：一样，新建文件
[CreateAssetMenu(menuName = "Skills/Heal")]
public class HealSkill : SkillBase
{
    [SerializeField] private int healMultiplier = 2;

    public override bool CanExecute(Character caster, Character target)
    {
        // 治疗目标是友方，不能治疗敌方
        return target != null && target.Faction == caster.Faction;
    }

    public override void Execute(Character caster, Character target)
    {
        int healAmount = caster.MagicPower * healMultiplier;
        target.RestoreHP(healAmount);
    }
}
```

现在的结构：

- `SkillSystem.UseSkill()` **从此不需要改变**，不管加多少新技能
- 每次新增技能 = 新建一个 `SkillBase` 子类文件
- 策划可以在 Inspector 里直接配置技能参数（因为是 ScriptableObject）
- 每个技能可以单独测试，不会互相影响
- 多人协作时，每人开发一个技能文件，没有代码冲突

---

## 另一个案例：道具/装备效果系统

OCP 的另一个典型应用是道具效果。如果不用 OCP：

```csharp
// 违反 OCP：每次加道具效果都要改这个函数
public void UseItem(string itemId, Character user)
{
    if (itemId == "HealingPotion") user.RestoreHP(50);
    else if (itemId == "ManaPotion") user.RestoreMana(30);
    else if (itemId == "SpeedBoost") user.ApplyBuff(BuffType.Speed, 10f);
    else if (itemId == "StrengthPotion") { user.AttackPower += 20; /* 还需要在某个时候移除... */ }
    else if (itemId == "Antidote") user.RemoveStatus(StatusEffect.Poison);
    else if (itemId == "ExpScroll") user.GainExperience(100);
    // 每个版本都在加更多 if/else
}
```

用 OCP 重设计：

```csharp
// 道具效果接口
public interface IItemEffect
{
    void Apply(Character user);
    bool CanUse(Character user);
}

// 道具数据
[CreateAssetMenu(menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public IItemEffect effect; // 注入具体效果
}

// 道具系统框架——永不修改
public class ItemSystem
{
    public void UseItem(ItemData item, Character user)
    {
        if (!item.effect.CanUse(user))
        {
            Debug.Log("Can't use this item right now.");
            return;
        }
        item.effect.Apply(user);
    }
}

// 各种道具效果：每次加新效果，只加新类
public class HealingPotionEffect : IItemEffect
{
    public int healAmount = 50;
    public bool CanUse(Character user) => user.CurrentHP < user.MaxHP;
    public void Apply(Character user) => user.RestoreHP(healAmount);
}

public class BuffEffect : IItemEffect
{
    public BuffType buffType;
    public float duration;
    public bool CanUse(Character user) => !user.HasBuff(buffType);
    public void Apply(Character user) => user.ApplyBuff(buffType, duration);
}

public class ExpScrollEffect : IItemEffect
{
    public int experienceAmount = 100;
    public bool CanUse(Character user) => true;
    public void Apply(Character user) => user.GainExperience(experienceAmount);
}
```

---

## OCP 的核心手段：多态与策略模式

OCP 的实现通常依赖两件事：

**抽象（接口/抽象类）**：把"做什么"从"怎么做"里分离出来。`SkillBase` 定义了"一个技能可以被执行"，但不定义具体怎么执行。

**多态（继承/实现）**：具体的"怎么做"在子类里实现，系统框架通过抽象类型来调用，不关心实际是哪个子类。

这两件事合在一起，就是**策略模式（Strategy Pattern）**的本质：把一族可替换的算法封装在各自的类里，通过接口来使用它们，使得算法可以独立于使用它的客户端变化。

---

## 什么时候应用 OCP

OCP 不是"一开始就把所有东西都抽象化"的借口。过度抽象会使代码难以理解。

应用 OCP 的时机：

**明确的变化点**：当你知道某个部分会经常增加新的类型时。技能类型会持续增加，道具效果会持续增加——这是明确的变化点。

**多人协作的模块**：多个程序员同时开发同一个功能（比如不同的技能），如果没有 OCP 设计，会频繁产生代码冲突。

**不应该随意动的核心逻辑**：框架代码、核心系统——一旦稳定，应该被"锁死"，只通过扩展来增加功能。

**不需要应用 OCP 的情况**：一次性代码、确定不会有多个类型的功能、内部实现工具函数。

---

## OCP 和 SRP 的关系

OCP 和 SRP 通常是相辅相成的：

- SRP 保证每个类只有一个变化理由
- OCP 保证这个变化不需要修改已有代码

如果 `SkillSystem` 同时管理"技能调用"和"每个技能的具体逻辑"（违反 SRP），那么每次加技能都需要改 `SkillSystem`（违反 OCP）。

把技能逻辑拆到各自的类里（SRP），技能系统就自然地对扩展开放了（OCP）。

---

## 小结

- **OCP 的本质**：通过增加新代码（新的类/实现）来增加新功能，而不是修改运行良好的旧代码
- **实现手段**：定义稳定的抽象（接口/抽象类），把变化封装在子类实现里
- **游戏中的典型场景**：技能系统、道具系统、伤害类型、AI 行为——任何会持续增加"新类型"的系统
- **不要过早应用**：只在明确的变化点上应用，不需要把所有代码都预先抽象化

OCP 是 SRP 的自然延伸：一旦你按照"变化理由"拆分了职责，扩展新功能的路径也就自然清晰了。
