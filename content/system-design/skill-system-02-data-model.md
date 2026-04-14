---
date: "2026-03-27"
title: "技能系统深度 02｜数据模型：SkillDef、SkillInstance、CastContext、TargetSpec、EffectSpec 怎么设计"
description: "技能系统能不能长期扩展，关键不在生命周期写得多花，而在定义层和运行时层有没有分开。这篇从 SkillDef、SkillInstance、CastContext、TargetSpec、EffectSpec 五个核心对象开始，把技能系统的数据骨架立起来。"
slug: "skill-system-02-data-model"
weight: 8002
tags:
  - Gameplay
  - Skill System
  - Combat
  - Data Model
  - Architecture
series: "技能系统深度"
series_order: 2
---

> 技能系统的数据模型真正要回答的，不是“字段要不要全”，而是：`什么东西是定义，什么东西是这一次施法的运行时状态。`

很多技能系统会越写越难扩展，一个非常核心的原因是：

`定义层和运行时层从第一天开始就没分开。`

最典型的症状有两个。

第一个症状，是把一切都塞进配置对象。

```csharp
public class SkillData
{
    public string id;
    public float cooldown;
    public float manaCost;
    public int currentCharges;
    public float remainCooldown;
    public Character currentTarget;
    public bool isCasting;
}
```

这个对象里既有“技能本来是什么”，也有“这一次释放现在进行到哪了”。

第二个症状，是运行时里没有中间上下文。

于是系统只能在各种函数之间传：

```csharp
UseSkill(skillId, caster, target, direction, isCritical, fromAI, skillLevel, ...)
```

参数越来越多，最后谁也分不清某个字段到底属于技能定义、角色状态、还是本次施法环境。

所以这一篇要做的事很简单：

把技能系统里最关键的几类数据对象切开。

---

## 先分成两大层：定义层和运行时层

如果只记一个总原则，我建议记这个：

`定义层描述“它本来是什么”，运行时层描述“这次到底发生了什么”。`

### 定义层

定义层通常是：

- 技能配置
- 效果配置
- Buff 配置
- 目标选择配置
- 消耗和冷却配置

这些东西的特点是：

- 可复用
- 可被策划编辑
- 不应该保存“这一局、这一帧”的即时状态

### 运行时层

运行时层通常是：

- 当前技能槽里绑定了哪个技能
- 这次施法是否在前摇
- 当前锁定了哪个目标
- 投射物是不是已经发出去
- 当前冷却还剩多久
- 当前 Buff 堆到了几层

这些东西的特点是：

- 只对当前角色、当前战斗、当前这次施法有效
- 会随时间变化
- 通常不应该回写到定义对象里

---

## 我建议的五个核心对象

如果是一个想长期扩展的技能系统，我建议至少把下面五类对象立出来：

- `SkillDef`
- `SkillInstance`
- `CastContext`
- `TargetSpec`
- `EffectSpec`

这五个名字不是唯一标准，但它们分别解决的问题很稳定。

---

## SkillDef：技能“本来是什么”

`SkillDef` 负责描述一个技能的静态定义。

它应该回答：

- 技能 ID 是什么
- 消耗规则是什么
- 冷却规则是什么
- 生命周期模板是什么
- 目标选择方式是什么
- 生效时会生成哪些效果

示意结构可以像这样：

```csharp
public class SkillDef
{
    public string id;
    public string displayName;

    public CostDef cost;
    public CooldownDef cooldown;
    public CastPolicyDef castPolicy;
    public TargetingDef targeting;

    public List<EffectDef> effects;
    public List<TagId> requiredTags;
    public List<TagId> blockedTags;
}
```

这里最重要的是：

`SkillDef 不应该保存这一次施法的当前目标、剩余前摇、当前冷却剩余。`

这些都不是“定义”，而是运行时状态。

---

## SkillInstance：角色身上的技能运行时实例

如果 `SkillDef` 是“技能模板”，那 `SkillInstance` 就是：

`某个角色此刻真正拥有并使用的这份技能状态。`

它通常负责：

- 当前等级
- 当前槽位
- 当前充能数
- 当前冷却剩余
- 当前是否处于施法中
- 当前激活出来的 Cast 是否存在

示意：

```csharp
public class SkillInstance
{
    public SkillDef def;
    public Entity owner;

    public int level;
    public int currentCharges;
    public float cooldownRemaining;

    public ActiveCast activeCast;
}
```

注意这里的 `SkillInstance` 也不等于“这一次施法”。

因为同一个技能实例，在一局战斗里可以释放很多次。

所以“这一次施法”的细节，还需要单独一层上下文。

---

## CastContext：这一次施法到底发生在什么环境里

很多项目中间层不够，就是因为少了 `CastContext`。

结果是所有数据都只能散在参数列表里。

我建议把一次技能释放的上下文显式做成对象。

它至少应该包含：

- 施法者是谁
- 技能实例是什么
- 当前目标是谁
- 发射方向是什么
- 这次施法发生在什么时候
- 当前快照到的关键属性是什么

示意：

```csharp
public class CastContext
{
    public SkillInstance skill;
    public Entity caster;

    public Entity primaryTarget;
    public Vector3 origin;
    public Vector3 direction;

    public int castFrame;
    public float castTime;

    public StatSnapshot snapshot;
}
```

`CastContext` 的价值在于：

- 它让“这一次施法”的数据有归属
- 它把定义层和效果层之间接起来
- 它天然适合进入日志、回放、网络同步和调试工具

---

## TargetSpec：目标是怎么被选出来的

很多技能系统会把“目标选择”简单理解成一个 `Entity target`。

但真正稍微复杂一点的技能，目标根本不是一个单体对象。

它可能是：

- 当前锁定目标
- 鼠标点击位置
- 角色正前方扇形区域
- 一条射线
- 一个投射物命中点
- 某个半径范围内的所有敌人

所以我建议把目标选择单独建模。

```csharp
public class TargetSpec
{
    public TargetMode mode;
    public float range;
    public float radius;
    public LayerMask mask;
    public bool requireLineOfSight;
    public TargetFilter filter;
}
```

然后在运行时从 `TargetSpec` 解析出真正的命中集合。

这件事最大的好处是：

`技能定义里描述的是“如何选目标”，而不是“现在已经选到了谁”。`

---

## EffectSpec：效果真正落地前的可执行描述

前一篇讲过：Skill 不应该自己直接改世界，真正改世界的是 Effect。

但 Effect 在执行前，最好还有一层“已绑定上下文的效果描述”。

这就是 `EffectSpec`。

为什么要有它？

因为定义层里的 `DamageEffectDef` 只是说：

- 系数是多少
- 伤害类型是什么
- 是否可暴击

而真正执行时，还需要把这次施法的上下文补进去。

```csharp
public class EffectSpec
{
    public EffectDef def;
    public CastContext context;
    public IReadOnlyList<Entity> resolvedTargets;
}
```

一旦有了 `EffectSpec`，系统就更容易做到：

- 先解析目标
- 再排序效果
- 再按顺序应用
- 并把整个结算过程记入日志

---

## 再补三类非常实用的辅助对象

前面五个是核心骨架，但在真正项目里，通常还会补三类对象。

### 1. CostDef

描述技能如何支付成本。

```csharp
public class CostDef
{
    public ResourceType type;
    public float amount;
    public bool consumeOnCastStart;
    public bool refundIfInterrupted;
}
```

### 2. CooldownDef

描述冷却模型。

```csharp
public class CooldownDef
{
    public float duration;
    public string cooldownGroup;
    public int maxCharges;
}
```

### 3. CastPolicyDef

描述生命周期模板。

```csharp
public class CastPolicyDef
{
    public SkillCastType castType; // Instant / Cast / Channel / Charge / Toggle
    public float windup;
    public float activeWindow;
    public float recovery;
}
```

这三层单独拆出来之后，你就不会再把“消耗”“冷却”“生命周期”都硬塞回 `SkillDef` 的一堆字段里。

---

## 用一个 Fireball 走一遍完整数据流

假设现在有一个技能 `Fireball`。

它的数据流更像这样：

### 第一步：定义层存在一份 SkillDef

```text
SkillDef(Fireball)
  - Cost: 20 Mana
  - Cooldown: 8s
  - Targeting: SingleTarget, 20m
  - CastPolicy: Cast(0.4s windup)
  - Effects:
      1. Damage(1.2 * MagicPower)
      2. ApplyBuff(Burn, 3s)
```

### 第二步：角色身上挂着一份 SkillInstance

```text
Mage.SkillSlots[Q]
  -> SkillInstance(Fireball, level=3, cooldownRemaining=0)
```

### 第三步：按下按键，构造 CastContext

```text
CastContext
  - caster = Mage
  - primaryTarget = Goblin_01
  - origin = Mage.HandSocket
  - direction = Forward
  - snapshot = { MagicPower = 120, Crit = 18% }
```

### 第四步：从 SkillDef 生成 EffectSpec

```text
EffectSpec #1
  - Damage(1.2 * MagicPower)
  - targets = [Goblin_01]

EffectSpec #2
  - ApplyBuff(Burn, 3s)
  - targets = [Goblin_01]
```

### 第五步：效果执行，改变世界

```text
Goblin_01.HP -= 144
Goblin_01.AddBuff(Burn)
Fireball enters cooldown
```

这样一条链走下来，每类数据对象站位都很清楚。

---

## Unity 里怎么落地会比较稳

如果放到 Unity 的常见工程实践里，我更建议这样落：

- `SkillDef / BuffDef / EffectDef` 用 `ScriptableObject` 作为编辑入口
- `SkillInstance / ActiveCast / BuffRuntime` 用纯运行时对象保存
- `CastContext / EffectSpec` 作为一次执行过程中的中间对象

这里最关键的不是“是不是 ScriptableObject”，而是：

`不要把运行时状态写回定义对象。`

比如最危险的写法之一就是：

```csharp
// 错误示意：把剩余冷却写回 SkillDef
skillDef.cooldownRemaining = 3.2f;
```

只要这么做，定义层就不再是定义层了。

---

## 一个坏模型会怎么坏

如果定义层和运行时层不分，通常会逐步出现这些问题。

### 策划改一份配置，会影响所有角色

因为项目把“模板”和“实例状态”混在一起了。

### 冷却、层数、目标这些运行时状态变得无法追踪

你会发现：

- 当前到底是谁在施法，不清楚
- 当前这次施法的目标是谁，不清楚
- 为什么这次结算用了旧属性，不清楚

### 日志和回放很难做

因为系统根本没有“这一次施法”的中间对象。

### 网络同步变得非常痛苦

因为你既没有清晰的 SkillInstance，也没有清晰的 CastContext，更没有清晰的 EffectSpec。

---

## 这篇真正想留下来的骨架

如果想让技能系统后面还能继续长，我建议先把数据骨架固定成下面这句：

`SkillDef 描述技能本来是什么；SkillInstance 描述某个角色当前拥有的这份技能状态；CastContext 描述这一次施法的上下文；TargetSpec 描述目标选择规则；EffectSpec 描述绑定了上下文后真正要执行的效果。`

这五层一旦站住，后面的生命周期、Effect System、Buff System 才有地方落。

否则你写的不会是一套系统，而只是一组越来越大的技能脚本。
