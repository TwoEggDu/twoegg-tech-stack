---
date: "2026-03-27"
title: "技能系统深度 07｜效果系统：伤害、治疗、位移、驱散、召唤为什么应该统一落到 Effect System"
description: "技能系统真正的发动机不是 Skill 本身，而是 Effect System。Skill 负责组织一次释放，真正改变世界的是伤害、治疗、位移、驱散、召唤这些效果单元。这篇把 Effect System 的职责和落地方式拆开。"
slug: "skill-system-07-effect-system"
weight: 8007
tags:
  - Gameplay
  - Skill System
  - Combat
  - Effect System
  - Architecture
series: "技能系统深度"
---

> 如果前几篇在做“拆边界”和“立骨架”，那这一篇要做的就是把发动机装上：`真正改世界状态的，不是 Skill 的名字，而是 Effect。`

很多项目一开始写技能，最顺手的做法是：

```csharp
public class FireballSkill : SkillBehaviour
{
    public override void Execute(Entity caster, Entity target)
    {
        int damage = (int)(caster.MagicPower * 1.2f);
        target.TakeDamage(damage);
        target.AddBuff("Burn", 3f);
    }
}
```

这在只有三五个技能的时候没有任何问题。

但只要技能类型开始增加，就会立刻遇到这些现实：

- 治疗和伤害逻辑重复度很高
- 上 Buff、驱散、净化、护盾都各写一套
- 位移、召唤、投射物、传送越来越难复用
- 每个技能都自己直接改世界，结算顺序开始混乱

于是项目会慢慢变成：

`有很多技能，但没有技能系统。`

因为每个技能都变成了一个小型战斗脚本。

所以这一篇的核心观点很明确：

`Skill 负责组织一次能力调用，Effect System 负责真正执行世界变化。`

---

## 为什么 Skill 不应该自己直接改世界

这里不是说 Skill 不能“发起”改变，而是说：

`Skill 不应该把“如何改世界”都写死在自己体内。`

原因有四个。

### 1. 技能和世界变化不是一一对应关系

同一个技能，往往会产生多个效果：

- 直接伤害
- 上 Debuff
- 击退
- 生成投射物
- 触发额外被动

如果这些都写在 Skill 里，Skill 很快会变成巨型脚本。

### 2. 世界变化本身应该是可复用的

“造成伤害”不是只属于火球术。

它也可能出现在：

- 平 A
- 爆炸桶
- 陷阱
- Buff Tick
- 被动反击

也就是说，“伤害”本身应该是一个独立效果单元，而不是某个技能私有的代码片段。

### 3. 结算顺序需要可控

一旦系统稍微复杂，你就会遇到：

- 先伤害还是先驱散
- 先破盾还是先扣血
- 先施加 Tag 还是先做控制
- 先命中还是先触发被动

这些不是“具体技能的小细节”，而是整套战斗系统都需要统一的结算规则。

### 4. 调试、回放、网络同步都需要显式效果

如果世界变化是一个个显式 Effect，那么你就更容易：

- 记日志
- 做回放
- 发网络消息
- 跟踪一次技能到底改了什么

---

## Effect System 到底在做什么

我建议把它理解成：

`把一次技能释放转译成一组可执行世界变化，并按规则把它们应用出去。`

这里面至少包含三步。

### 第一步：从 Skill 生成效果描述

Skill 不直接改世界，而是生成一组 `EffectSpec`。

例如：

```text
Fireball
  -> DamageEffectSpec
  -> ApplyBuffEffectSpec(Burn)
```

### 第二步：解析上下文

每个 EffectSpec 都会绑定：

- 施法者
- 目标集合
- 这次施法的快照属性
- 当前标签和状态

### 第三步：按顺序执行

系统统一决定：

- 这些效果按什么顺序执行
- 什么时候视为命中
- 是否允许被拦截、免疫、重定向

这就是 Effect System 的本体。

---

## 我建议的最小效果分类

如果一上来就想做“完美效果树”，大概率会把系统写炸。

更实用的做法，是先把最常见效果归成几类。

### 1. Direct Effect

直接一次性改状态。

例如：

- 伤害
- 治疗
- 增减资源
- 立即位移

### 2. Apply-State Effect

给目标施加一个持续状态。

例如：

- 上 Buff
- 上 Debuff
- 加 Tag
- 加护盾

### 3. Control Effect

改变目标的行动能力。

例如：

- 眩晕
- 沉默
- 打断
- 击退

### 4. Spawn Effect

创建新的运行时实体。

例如：

- 生成投射物
- 召唤图腾
- 放陷阱
- 生成地面区域

### 5. Cleanup / Utility Effect

处理驱散、净化、移除状态、刷新冷却、重置层数等。

这套分类不是为了“学术好看”，而是为了让工程上先有稳定落点。

---

## 一个建议的执行骨架

如果用最少对象先把执行层立住，我建议这样组织：

```csharp
public interface IEffectExecutor
{
    void Execute(EffectSpec spec, CombatContext context);
}

public class DamageEffectExecutor : IEffectExecutor
{
    public void Execute(EffectSpec spec, CombatContext context)
    {
        var caster = spec.context.caster;

        foreach (var target in spec.resolvedTargets)
        {
            int amount = DamageFormula.Calculate(spec, caster, target);
            context.ApplyDamage(caster, target, amount, spec);
        }
    }
}
```

然后 Skill 只做组合：

```csharp
public class SkillCastRunner
{
    public void Resolve(CastContext cast)
    {
        List<EffectSpec> effects = BuildEffectSpecs(cast);

        foreach (var effect in effects)
            effect.executor.Execute(effect, combatContext);
    }
}
```

这套结构的关键不是接口形式，而是职责切法：

- Skill 组织
- EffectSpec 描述这次要执行什么
- EffectExecutor 真正应用变化

---

## 伤害、治疗、驱散，为什么都值得变成独立 Effect

### 伤害

伤害不是一个简单数字。

它经常涉及：

- 伤害类型
- 暴击
- 护甲/抗性
- 护盾拦截
- 吸血
- 受伤加成

这类逻辑如果散落在每个技能里，很快就无法统一维护。

### 治疗

治疗表面和伤害相反，但其实也会涉及：

- 治疗加成
- 治疗暴击
- 过量治疗
- 治疗吸收

它同样应该是独立 Effect。

### 驱散 / 净化

驱散看上去像个“小功能”，但它实际上需要系统统一访问：

- 当前目标有哪些 Buff
- 哪些标签允许移除
- 驱散优先级如何定义

这类操作如果不走 Effect 层，最后只能写成目标对象上的一堆特殊函数。

---

## 效果顺序必须是系统规则，不是技能脚本随意写

举个例子。

一个技能同时具备下面两个效果：

1. 驱散目标身上的护盾
2. 对目标造成 300 点伤害

如果顺序不同，结果就不同：

- 先驱散再伤害：护盾不吸收这次伤害
- 先伤害再驱散：护盾可能先吃掉伤害

这说明什么？

说明“效果顺序”不是某个技能自己关起门来定义的小事，而是战斗系统的公共规则。

因此我建议至少明确下面这些排序维度：

- 技能内部效果顺序
- 命中后即时效果 vs 持续效果
- 护盾、减伤、受伤触发器的介入时机
- 失败效果是否中断后续效果

---

## 要不要快照，也是 Effect System 的职责边界

很多项目后面一定会遇到一个问题：

`伤害到底按释放时属性算，还是按命中时属性算？`

比如：

- 火球发出去后，施法者攻击力变了
- 引导过程中，目标获得了减伤 Buff
- 投射物飞行期间，施法者死了

这些问题都不是“公式细节”，而是 Effect System 必须明确的规则。

通常你至少要区分：

- `Snapshot`：释放时拍快照，后面沿用
- `Live Resolve`：执行时再读取当前状态

如果这件事没有系统层统一定义，最后每个技能都会用自己的土办法处理。

---

## 一个 Fireball 在 Effect System 里应该怎么长

还是用最经典的火球术。

如果把它做成效果驱动，它更像这样：

```text
Skill: Fireball
  - Lifecycle:
      Cast 0.4s
      Spawn Projectile

Projectile OnHit:
  1. DamageEffect(Fire, 1.2 * MagicPower)
  2. ApplyBuffEffect(Burn, 3s)
```

这里真正好用的地方是：

- `DamageEffect` 可以被别的技能复用
- `ApplyBuffEffect` 也可以被别的技能复用
- 投射物命中只是“什么时候触发这些效果”的条件

于是技能系统的表达能力会明显上升：

`技能负责组织时序，效果负责改变世界。`

---

## 一个坏 Effect System 通常怎么坏

### 1. 只有 Skill，没有 Effect

每个技能自己改世界，最后代码完全无法复用。

### 2. Effect 名义上拆了，执行还在 Skill 里

这种做法只是“数据上看起来有 Effect”，但世界变化仍然散在技能脚本里，意义不大。

### 3. 没有统一 CombatContext

结果伤害、护盾、驱散、Buff、日志各自改世界，最终顺序和副作用很难追。

### 4. 效果顺序没有系统规则

最后只能靠“这个技能碰巧这么写”来决定结果。

---

## 这一篇真正想留下来的结论

技能系统真正的发动机不是 Skill 本身，而是 Effect System。

Skill 负责：

- 接受请求
- 驱动生命周期
- 组织目标和时序

Effect System 负责：

- 统一描述世界变化
- 统一排序
- 统一执行
- 统一记录

所以如果要把这句话压成最短：

`Skill 说“什么时候、对谁、做哪些变化”；Effect System 才真正负责“把这些变化执行出去”。`
