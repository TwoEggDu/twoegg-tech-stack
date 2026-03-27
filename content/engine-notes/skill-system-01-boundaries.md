---
date: "2026-03-27"
title: "技能系统深度 01｜边界：技能、效果、Buff、属性、标签、资源、冷却分别是什么"
description: "技能系统最容易出事的地方不是代码不会写，而是边界从一开始就没分清。技能、效果、Buff、属性、标签、资源、冷却这些概念到底谁负责什么，这篇先拆干净。"
slug: "skill-system-01-boundaries"
weight: 8001
tags:
  - Gameplay
  - Skill System
  - Combat
  - Buff
  - Architecture
series: "技能系统深度"
---

> 技能系统真正的第一步，不是写 `UseSkill()`，而是先回答：`到底什么是 Skill，什么不是 Skill。`

很多项目一开始技能系统长得很乱，不是因为工程师不会抽象，而是因为一上来就把这些东西揉进了同一个对象里：

- 技能定义
- 伤害公式
- Buff 配置
- 资源消耗
- 冷却时间
- 状态限制
- 特效名
- 命中判定
- UI 文案

一开始它们看上去都“跟技能有关”，于是最常见的结果就是一个巨大的 `SkillData`：

```csharp
public class SkillData
{
    public string id;
    public float manaCost;
    public float cooldown;
    public int damage;
    public float buffDuration;
    public string buffId;
    public bool requireTarget;
    public bool ignoreSilence;
    public string animationName;
    public string vfxName;
    public SkillType type;
}
```

这类结构在前期很顺手，但只要项目开始加规则，就会立刻出现两个症状：

- 你分不清“这是 Skill 的职责，还是 Buff 的职责”
- 每新增一个机制，都只能继续往这个类上加字段

所以技能系统真正的第一篇实现文章，不应该先讲生命周期，也不应该先讲网络，而应该先把概念边界切开。

---

## 一次技能释放里，到底有哪些东西同时在场

先看一个简单例子：`Fireball`。

玩家按下技能键之后，系统里其实同时存在好几种完全不同的概念。

### Skill

`Fireball` 作为一个技能，是“能力入口”。

它回答的是：

- 这是什么技能
- 什么时候能放
- 这次释放会走什么生命周期
- 最后会生成哪些效果

Skill 是“能力定义 + 执行入口”。

### Effect

火球命中之后，真正改变世界的不是 “Fireball 这个名字”，而是具体效果：

- 造成 120 点火焰伤害
- 施加一个 3 秒 Burn
- 击退目标 1 米

这些都是 Effect。

Effect 的职责不是“决定能不能施法”，而是“真正改世界状态”。

### Buff

Burn 不是一个 Skill，也不是一次瞬时 Effect。

Buff 更像是“挂在目标身上一段时间的持续规则包”。

它通常带着这些东西：

- 持续时间
- 层数
- 周期 Tick
- 属性修饰
- 标签
- 触发器

也就是说，Buff 是“持续存在的运行时状态”。

### Attribute

生命、攻击、防御、暴击率、施法速度，这些是 Attribute。

属性系统回答的是：

- 基础值是多少
- 各类 Modifier 怎么叠
- 当前最终值是多少

属性系统不应该知道“火球术”是什么。

### Tag

沉默、眩晕、无敌、燃烧、飞行、霸体、无法施法，这些往往更适合表达成 Tag。

Tag 本质上是：

`一个可查询、可组合、可传播的状态标签。`

Tag 不一定有持续时间，也不一定直接改属性。

### Resource

法力、能量、怒气、子弹、充能次数，这些是 Resource。

资源系统回答的是：

- 当前有多少
- 消耗多少
- 回得多快
- 能不能透支

它和 Skill 强相关，但它不是 Skill 本体。

### Cooldown

冷却也是独立概念。

它回答的是：

- 这个技能什么时候能再次使用
- 是否有公共冷却
- 是否和别的技能共享冷却组
- 是否多层充能

冷却不属于 Effect，也不属于 Buff。

---

## 先给每个概念一个最短定义

如果想先把边界立住，我建议用下面这组最短定义。

### Skill：能力入口

Skill 是玩家或 AI 可以主动发起的一次能力调用入口。

它主要负责：

- 接收请求
- 校验条件
- 驱动生命周期
- 生成 Effect

### Effect：世界变化单元

Effect 是一次具体的状态改变。

它主要负责：

- 扣血
- 治疗
- 位移
- 施加 Buff
- 驱散、打断、控制

### Buff：持续中的规则包

Buff 是附着在实体上的一段持续运行的状态。

它主要负责：

- 持续时间
- 层数
- Tick
- 属性修饰
- 标签附着
- 事件触发

### Attribute：可计算的数值状态

Attribute 是角色当前数值状态的容器和计算结果。

它主要负责：

- 基础值
- 各类修饰项
- 最终值求解

### Tag：可查询的语义状态

Tag 是系统间沟通状态约束的轻量语义标签。

它主要负责：

- 表达“当前是什么状态”
- 让系统快速判断能不能做某事
- 作为 Buff、Skill、AI、动画之间的桥接语言

### Resource：消耗与回补系统

Resource 负责“能不能支付这次能力成本”。

### Cooldown：时间约束系统

Cooldown 负责“这个能力多久之后才能再来一次”。

---

## 一张表看懂谁负责什么

最实用的办法，是强迫自己回答下面这些问题。

```text
“火球术能不能放？”          -> Skill / Resource / Cooldown / Tag
“火球命中后扣多少血？”      -> Effect / Attribute
“目标被点燃 3 秒”            -> Buff
“点燃期间每秒掉血”           -> Buff 内的周期 Effect
“燃烧状态下不能隐身”         -> Tag + 规则判断
“沉默时不能施法”             -> Tag / State Constraint
“法力值不够不能释放”         -> Resource
“8 秒后才能再放”             -> Cooldown
```

这张表的意义不在于“绝对正确”，而在于避免一种灾难：

`让所有问题最后都只能回到 Skill 类本身去处理。`

---

## 最容易混掉的四组边界

### 1. Skill 和 Effect 混掉

很多项目会把“技能”直接写成“造成什么效果”的容器。

于是一个 Skill 类里同时负责：

- 校验
- 施法状态
- 动画触发
- 命中判定
- 伤害计算
- 上 Buff

结果就是：每个技能都像一个小战斗系统。

更稳的做法是：

- Skill 负责“组织一次释放”
- Effect 负责“真正改世界”

### 2. Buff 和 Tag 混掉

Buff 和 Tag 经常一起出现，但它们不是一回事。

比如：

- `Burning` 作为 Tag，可以表示“目标当前处于燃烧状态”
- `BurnBuff` 作为 Buff，负责“持续 3 秒，每秒掉血，并附带 Burning Tag”

也就是说：

- Tag 是语义状态
- Buff 是持续规则容器

如果把 Tag 当 Buff 用，很快就会发现：

- 某些状态其实不需要持续时间
- 某些限制只想表达查询语义，不想挂一个完整 Buff 对象

### 3. Cooldown 和 Resource 混掉

“法力不足”和“技能在冷却”看上去都属于“不能施放”，但它们不是同一类问题。

- Resource 是支付能力
- Cooldown 是时间约束

把它们混在一个 `CanCastReason` 之外没有问题，但底层实现最好还是分开。

### 4. Attribute 和 Buff 混掉

如果你的 Buff 直接去改 `Attack += 20`，结束时再 `Attack -= 20`，很快就会遇到：

- Buff 叠层顺序错乱
- 覆盖关系难算
- 中途换装备或吃别的加成时数值不对

更稳的做法通常是：

- Attribute 负责最终值计算
- Buff 只提供 Modifier

---

## 一个更稳的最小建模方式

如果只想先立住系统边界，我建议把数据关系想成下面这样：

```csharp
public class SkillDef
{
    public string id;
    public CostDef cost;
    public CooldownDef cooldown;
    public TargetingDef targeting;
    public List<EffectDef> effects;
}

public abstract class EffectDef { }

public class DamageEffectDef : EffectDef
{
    public float ratio;
    public DamageType damageType;
}

public class ApplyBuffEffectDef : EffectDef
{
    public BuffDef buff;
}

public class BuffDef
{
    public string id;
    public float duration;
    public int maxStacks;
    public List<TagId> grantedTags;
    public List<ModifierDef> modifiers;
}
```

这个模型最重要的价值不是“足够完整”，而是它天然逼你承认：

- Skill 不是 Buff
- Buff 不是 Attribute
- Cooldown 不是 Effect
- Tag 不是 SkillType

---

## 以“火球术”再拆一遍

我们再回到 `Fireball`。

如果边界清楚，它更像下面这样被描述：

```text
Skill: Fireball
  - Cost: 20 Mana
  - Cooldown: 8s
  - Targeting: Single Target / 20m
  - Lifecycle: Cast 0.4s -> Fire -> Recover 0.2s
  - Effects:
      1. Damage(Fire, 120% MagicPower)
      2. ApplyBuff(Burn, 3s)

Buff: Burn
  - Duration: 3s
  - Tick: 1s
  - Effects:
      1. PeriodicDamage(Fire, 10% MagicPower)
  - Tags:
      - Status.Burning
```

这时候系统里的责任分布就很清楚了：

- 能不能放，看 Cost / Cooldown / Tag
- 放出来之后怎么走，看 Skill Lifecycle
- 命中后改什么，看 Effect
- 后续持续掉血，看 Buff
- “目标是否处于燃烧状态”，查 Tag

这就是边界清晰带来的力量。

---

## 边界切对之后，你会得到什么

### 新增技能时，不必碰核心结构

你只是加一个新的 SkillDef，组合不同 Effect。

### 新增规则时，更容易找到归属层

比如：

- “沉默”应该落到 Tag / 状态约束层
- “护盾吸收伤害”应该落到 Buff / Effect 处理链
- “共享冷却”应该落到 Cooldown 组

### 系统可测试性会明显变高

因为每层都变成了可单独验证的对象：

- Skill 是否能进入生命周期
- Effect 是否正确改世界
- Buff 是否正确叠层和结算
- Attribute 是否正确求值

---

## 这篇真正想留下来的结论

技能系统的难点，往往不是“不会做火球术”，而是：

`当火球术、治疗术、护盾、位移、召唤、沉默、霸体、共享冷却、被动技能、Buff 叠层一起出现时，你还能不能说清楚每个概念到底站在哪一层。`

如果不能，系统会很快塌成一个大对象。

如果能，后面的很多问题反而会简单下来。

所以我建议把这一篇的结论直接记成一句最短的话：

`Skill 组织一次能力调用；Effect 真正改变世界；Buff 挂住持续规则；Attribute 负责数值求解；Tag 负责状态语义；Resource 和 Cooldown 负责释放约束。`
