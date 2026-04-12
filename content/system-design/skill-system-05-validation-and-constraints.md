---
date: "2026-03-27"
title: "技能系统深度 05｜校验与约束：冷却、消耗、距离、状态、标签阻塞应该放在哪"
description: "技能系统里“能不能放”往往比“怎么放”更容易失控。冷却、消耗、距离、视线、状态限制、标签阻塞这些约束如果没有统一校验层，后面一定会散到技能脚本、动画状态机和角色控制里。"
slug: "skill-system-05-validation-and-constraints"
weight: 8005
tags:
  - Gameplay
  - Skill System
  - Combat
  - Validation
  - Tags
series: "技能系统深度"
---

> 技能系统里最容易被写成到处散落 if 判断的一层，就是“这次到底能不能放”。更准确地说：`校验不是技能的小前置，而是技能系统的一整层约束系统。`

玩家按下技能键之后，系统最先遇到的问题不是伤害公式，而是：

- 技能在不在冷却
- 资源够不够
- 当前能不能施法
- 目标是否合法
- 距离够不够
- 当前状态是否被沉默、缴械、禁锢之类标签阻塞

很多项目前期会把这些逻辑写成：

```csharp
if (skill.cooldownRemaining > 0) return;
if (owner.Mana < skill.cost) return;
if (owner.IsSilenced) return;
if (target == null) return;
if (Vector3.Distance(owner.pos, target.pos) > skill.range) return;
```

问题不在于它不能用，而在于随着规则增长，这些判断会散到：

- 技能脚本
- 角色状态机
- AI 决策代码
- UI 灰置逻辑
- 网络校验逻辑

最后没有任何一层知道“完整规则到底是什么”。

所以这一篇真正要做的，是把“能不能放”从零散判断，收束成统一约束系统。

---

## 校验层到底在解决什么

它至少解决三件事。

### 1. 决定一次请求是否可进入生命周期

不是所有请求都能走到 `Windup`。

很多请求应该在此之前就被挡下。

### 2. 给系统一个统一解释

为什么不能放，不应该只是 `false`。

系统最好知道是：

- 冷却中
- 资源不足
- 没有目标
- 目标超距
- 当前被沉默

只有这样 UI、AI、日志、网络才有机会统一理解。

### 3. 让约束规则集中，而不是散在技能里

校验层越集中，系统越稳。

因为“不能施法”的规则，本来就是公共规则，不应该藏在每个技能自己的脚本里。

---

## 我建议把约束拆成五类

如果想先把骨架立住，我建议技能校验先按下面五类组织。

### 1. 冷却约束

回答：

- 这个技能冷却好没好
- 是否有公共冷却
- 是否和别的技能共享冷却组
- 当前有没有可用充能

### 2. 资源约束

回答：

- 法力/能量/怒气是否足够
- 是否允许透支
- 消耗是在起手时扣、命中时扣，还是结束时扣

### 3. 目标与空间约束

回答：

- 是否需要目标
- 距离够不够
- 目标是否在扇形或范围内
- 是否要求 LOS

### 4. 状态与标签约束

回答：

- 当前是否沉默
- 当前是否被眩晕
- 当前是否在某类互斥技能状态下
- 当前身上是否有阻塞标签

### 5. 生命周期约束

回答：

- 当前是否正处于不可接技阶段
- 当前是否允许取消
- 当前是否允许插入更高优先级技能

这五类一分开，系统就不容易把“状态限制”和“空间限制”混成一坨。

---

## 最好不要只返回 bool

这一点非常重要。

如果校验函数只返回：

```csharp
bool CanCast(...)
```

那后面所有系统都会开始各自重写一遍判断，只为了知道“为什么不行”。

我更建议至少返回结构化结果：

```csharp
public enum CastBlockReason
{
    None,
    Cooldown,
    NotEnoughResource,
    NoTarget,
    OutOfRange,
    LineOfSightBlocked,
    BlockedByTag,
    InterruptedByState
}

public struct CastValidationResult
{
    public bool canCast;
    public CastBlockReason reason;
}
```

这会带来三个直接收益：

- UI 知道该怎么提示
- AI 知道是换技能还是换目标
- 调试日志能说清为什么失败

---

## Tag 是最稳的阻塞语言

前面讲过，Tag 是系统之间共享语义状态的好工具。

在校验层，它尤其适合做约束表达。

例如一个技能定义里，可以直接描述：

```text
RequiredTags:
  - State.CanCast

BlockedTags:
  - State.Silenced
  - State.Stunned
  - Weapon.Disarmed
```

然后校验层统一判断：

- 施法者是否满足 RequiredTags
- 是否命中 BlockedTags

这比写：

```csharp
if (owner.isSilenced || owner.isStunned || owner.isDisarmed)
```

要稳得多，因为：

- 规则变更时不必改核心代码
- Buff、装备、场地、剧情状态都能通过同一语言参与阻塞

---

## 冷却和资源最好单独是子系统，不要埋在 Skill 里

“技能能不能放”里，最容易被写死在 Skill 类里的两件事就是冷却和消耗。

但它们本质上都是公共约束。

### 冷却系统应该回答

- 当前剩余时间
- 当前剩余充能
- 是否触发公共冷却
- 是否触发共享组冷却

### 资源系统应该回答

- 当前余额
- 是否可支付
- 是否允许部分支付
- 中断时要不要返还

如果这些规则都埋在 Skill 内部，后面一旦出现：

- 多充能
- 公共冷却
- 替代消耗
- Buff 改变施法消耗

技能类就会迅速膨胀。

---

## 距离和视线最好先统一成 Target Validation

很多项目里，“有没有目标”“距离够不够”“视线通不通”会分散在：

- 输入层
- 技能层
- 碰撞层

更稳的做法是先承认：

这三件事都属于 Target Validation。

示意：

```csharp
public class TargetValidationRule
{
    public bool requireTarget;
    public float maxRange;
    public bool requireLineOfSight;
    public TargetFactionRule factionRule;
}
```

然后由统一校验器去回答：

- 目标缺失
- 超出范围
- 不满足阵营条件
- LOS 阻塞

这样技能层只关心“规则是什么”，而不是自己去实现每种空间判断。

---

## 生命周期约束是最容易漏掉的一类

很多系统前期能想到冷却和蓝量，却经常漏掉一类更关键的约束：

`当前角色正处于什么技能生命周期阶段。`

例如：

- 正在前摇，能不能接翻滚
- 正在引导，能不能插一个瞬发技能
- 正在后摇，能不能吃输入缓冲
- 正在被击飞，能不能施法

这类问题不应该交给输入层猜，也不应该交给 Animator 猜。

它应该由技能系统根据当前生命周期显式回答。

---

## 一个更稳的校验器长什么样

如果只做最小骨架，我建议是“规则列表 + 执行器”的结构。

```csharp
public interface ICastValidationRule
{
    CastValidationResult Validate(CastRequest request, CombatContext context);
}
```

例如：

- `CooldownRule`
- `ResourceRule`
- `RequiredTargetRule`
- `RangeRule`
- `BlockedTagRule`
- `LifecycleRule`

然后统一顺序执行：

```csharp
public CastValidationResult Validate(CastRequest request)
{
    foreach (var rule in rules)
    {
        var result = rule.Validate(request, context);
        if (!result.canCast)
            return result;
    }

    return CastValidationResult.Success;
}
```

这套结构的好处是：

- 新规则可以加新 Rule
- 公共规则不必写进每个技能
- UI/AI/网络都能复用同一套结果

---

## 用一个 Blink 技能走一遍

假设有个位移技能 `Blink`。

它的校验可能包括：

- 不在冷却
- 至少有 30 点法力
- 当前不处于 `State.Rooted`
- 目标点在 8 米内
- 目标点不在禁止区域

如果写成统一规则，它更像：

```text
CastRequest(Blink, targetPoint=P)
  -> CooldownRule
  -> ResourceRule
  -> BlockedTagRule
  -> RangeRule
  -> PositionValidityRule
```

这时候系统能明确给出失败原因：

- `Cooldown`
- `NotEnoughResource`
- `BlockedByTag`
- `OutOfRange`
- `InvalidTargetPosition`

这比简单返回 `false` 强太多。

---

## 一个坏校验系统通常怎么坏

### 1. 校验逻辑散在所有地方

UI 一套、AI 一套、技能脚本一套、服务器一套。

最后同一个技能会在不同入口表现不一致。

### 2. 没有显式失败原因

系统只能知道“不能放”，但不知道为什么。

### 3. 状态约束全写死成字段判断

这样一来，任何新状态都得改核心代码。

### 4. 生命周期约束被动画系统吞掉

最终技能什么时候能接、什么时候不能接，全靠动画状态机隐式决定。

---

## 这一篇真正想留下来的结论

“能不能放”不是技能执行前的一小段前置，而是技能系统的一整层约束系统。

更稳的做法是把约束拆成：

- 冷却
- 资源
- 目标与空间
- 状态与标签
- 生命周期

然后通过统一校验层给出结构化结果，而不是把规则散在各种 if 里。

所以这一篇最短的结论就是：

`技能系统不应该只回答“怎么放”，还必须统一回答“为什么现在不能放”。冷却、资源、距离、标签阻塞和生命周期约束，最好都收束到同一层校验系统里。`
