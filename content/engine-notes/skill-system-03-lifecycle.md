---
date: "2026-03-27"
title: "技能系统深度 03｜生命周期：Instant / Cast / Channel / Charge / Toggle / Passive / Combo 怎么统一"
description: "一个技能系统真正难的地方，不是支持某一种技能类型，而是用同一套生命周期模型把 Instant、吟唱、引导、蓄力、开关、被动和连段都接住。这篇把技能生命周期拆成稳定的状态骨架。"
slug: "skill-system-03-lifecycle"
weight: 8003
tags:
  - Gameplay
  - Skill System
  - Combat
  - State Machine
  - Architecture
series: "技能系统深度"
---

> 技能系统里最容易被低估的一件事是：`技能不是“执行一次函数”，而是“走完一段生命周期”。`

很多技能系统最早都会从一种最简单的技能开始：

- 按键
- 扣蓝
- 造成伤害
- 进冷却

于是代码很自然长成：

```csharp
public void Cast(SkillInstance skill, Entity caster, Entity target)
{
    if (!CanCast(skill, caster, target))
        return;

    ConsumeCost(skill, caster);
    ApplyEffects(skill, caster, target);
    StartCooldown(skill);
}
```

这段代码对“瞬发单体伤害技能”没有问题。

但只要项目开始加下面这些技能类型，它就会迅速崩掉：

- 有前摇的技能
- 会被打断的吟唱
- 需要持续维持的引导
- 松手释放的蓄力
- 开关型技能
- 常驻被动
- 连段技能

这些技能的共同点不是“规则更多”，而是：

`它们都要求系统显式表达“这次技能现在走到哪一步了”。`

这就是生命周期问题。

---

## 技能生命周期到底在解决什么

技能生命周期本质上在解决三件事。

### 1. 技能不是同一个时刻完成所有事

一次释放往往会跨越多个阶段：

- 请求
- 校验
- 前摇
- 生效
- 后摇
- 冷却

如果系统里没有显式状态，这些东西最后就只能藏在：

- 动画状态机里
- 协程里
- 一堆定时器里

然后整个技能系统的控制权就散掉了。

### 2. 技能会在中途被改变

技能不是发起之后就一定走到底。

它可能：

- 被打断
- 被沉默阻止
- 因目标死亡而取消
- 因资源不足而失败
- 因移动而提前结束

所以生命周期不仅要描述“正常路径”，还要描述“中途分叉”。

### 3. 不同技能类型需要共享一套框架

如果每种技能类型都各写一套逻辑，你很快会得到：

- `InstantSkill`
- `CastSkill`
- `ChannelSkill`
- `ChargeSkill`
- `ToggleSkill`

然后它们之间越来越难复用。

更稳的思路是：

`先建立一套统一状态骨架，再让不同技能类型选择经过哪些状态。`

---

## 我建议的最小生命周期骨架

如果只想先把框架立住，我建议先做下面这组状态。

```text
Idle
-> Requesting
-> Validating
-> Windup
-> Active
-> Recovery
-> Cooldown
-> Idle
```

以及两条中途分支：

```text
任意阶段
-> Interrupted
-> Cleanup
-> Cooldown 或 Idle
```

这里每个状态都应该有明确职责。

### Idle

技能当前处于可等待输入的空闲态。

### Requesting

系统刚收到一次施法请求，但还没真正进入施法。

这一步很适合处理：

- 输入缓冲
- 排队
- 请求合法性基础检查

### Validating

显式做前置校验。

例如：

- 资源够不够
- 冷却好没好
- 当前状态是否允许施法
- 目标是否合法

### Windup

前摇、施法动作、吟唱准备都可以先落在这一步。

重点是：技能已启动，但效果还没有真正生效。

### Active

技能真正生效的窗口。

这里不一定是“瞬时”。

它可能是：

- 瞬发一次
- 持续引导
- 蓄力完成后释放
- 开关技能处于激活中

### Recovery

后摇、硬直、收招、技能结束后的不可立即再执行区间。

### Cooldown

时间约束层。

并不是所有技能都必须经过 Cooldown，但大部分主动技能最后都要落到这里。

### Interrupted / Cleanup

打断不是简单的 `return false`。

它通常需要回答：

- 消耗退不退
- 投射物还在不在
- 引导是否中止
- Buff 要不要撤销
- 动画怎么收尾

所以最好显式有一段清理路径。

---

## 不同技能类型，如何落到同一套骨架里

这一篇真正想做的，不是把状态列完，而是证明：

`不同技能类型可以共用一套生命周期骨架。`

### 1. Instant

Instant 最简单。

它通常会这样走：

```text
Idle
-> Requesting
-> Validating
-> Active
-> Cooldown
-> Idle
```

比如瞬发治疗、立刻生效的闪现。

### 2. Cast

有前摇、有吟唱条的技能。

```text
Idle
-> Requesting
-> Validating
-> Windup
-> Active
-> Recovery
-> Cooldown
-> Idle
```

比如火球、读条治疗、大招施法。

### 3. Channel

引导型技能的关键是 Active 会持续一段时间，而且会不断检查中断条件。

```text
Idle
-> Requesting
-> Validating
-> Windup
-> Active(channel ticking...)
-> Recovery
-> Cooldown
-> Idle
```

比如激光、持续回复、旋风斩。

### 4. Charge

蓄力技能的重点是：

- 按下进入蓄力
- 松手或到达上限后释放

它本质上依然可以落在：

```text
Windup(holding)
-> Active(release)
```

只是 Windup 不再只是固定时长，而是一个可变时长窗口。

### 5. Toggle

开关型技能更像：

```text
Idle
-> Requesting
-> Validating
-> Active(enabled)
-> Requesting(关闭请求)
-> Recovery
-> Idle
```

它不一定进入传统意义上的 Cooldown，但会长期占住一个激活状态。

### 6. Passive

被动技能最特殊，因为它很多时候不由玩家主动触发。

但如果从系统角度看，它依然可以用同一套框架理解：

- 安装时进入 Active
- 条件触发时生成 Effect
- 卸载时 Cleanup

所以被动不一定需要“施法请求”，但依然需要“生命周期存在感”。

### 7. Combo

连段技能最难点不是“第几段伤害不同”，而是：

- 输入窗口
- 状态继承
- 超时重置

本质上更像“多个小生命周期串起来，并共享连段上下文”。

---

## 一个常见误区：把生命周期全部交给动画

很多项目最早会把生命周期这样理解：

- 动画播完就算技能结束
- 动画事件触发就算技能生效
- 动画被切掉就算技能取消

这类做法一开始很省事，但很快就会遇到问题：

- 同一个技能需要 AI 使用时怎么办
- 没有角色动画的召唤物技能怎么办
- 网络预测时本地和远端动画不同步怎么办
- 逻辑上打断了，但动画还没切掉时谁说了算

更稳的关系应该是：

`动画参与生命周期，但不独占生命周期。`

也就是说：

- 生命周期由技能系统管理
- 动画事件可以通知某个状态点
- 但技能状态不应该完全依赖 Animator 是否播到哪一帧

---

## 用一个“蓄力箭”走一遍

假设有一个技能 `ChargedArrow`。

它的行为是：

- 按下开始拉弓
- 最多蓄力 1.5 秒
- 提前松手按当前蓄力等级发射
- 被打断则失败

它的生命周期可以这样描述：

```text
Idle
-> Requesting
-> Validating
-> Windup(enter charge state)
    - startTime = now
    - maintain aiming state
    - if released: goto Active
    - if interrupted: goto Interrupted
    - if elapsed >= maxCharge: auto goto Active
-> Active
    - spawn projectile
    - damage scales by charge ratio
-> Recovery
-> Cooldown
-> Idle
```

这里最重要的是：

`“蓄力等级”不是 SkillDef 的字段变化，而是这一次 ActiveCast 在生命周期中的临时状态。`

这就是为什么生命周期一定要有显式运行时对象。

---

## 用状态机写法会更稳

实现层不一定非要搞成一整套状态模式，但我建议至少让状态转移显式存在。

示意：

```csharp
public enum SkillPhase
{
    Idle,
    Requesting,
    Validating,
    Windup,
    Active,
    Recovery,
    Cooldown,
    Interrupted,
    Cleanup
}

public class ActiveCast
{
    public SkillPhase phase;
    public float phaseStartTime;
    public CastContext context;
}
```

然后每帧或每次事件驱动时，更新当前阶段：

```csharp
public void Tick(ActiveCast cast, float now)
{
    switch (cast.phase)
    {
        case SkillPhase.Windup:
            if (ShouldInterrupt(cast))
                EnterInterrupted(cast);
            else if (now - cast.phaseStartTime >= cast.context.skill.def.castPolicy.windup)
                EnterActive(cast);
            break;

        case SkillPhase.Active:
            UpdateActivePhase(cast, now);
            break;
    }
}
```

只要状态和转移显式存在，很多复杂技能类型都能慢慢往里装，而不是不断长出旁路逻辑。

---

## 生命周期系统最常见的三种坏法

### 1. 完全没有显式阶段

所有逻辑全藏在：

- 协程
- 动画事件
- 定时器回调

最后没人说得清“技能现在到底处于哪一步”。

### 2. 每种技能类型各自写一套

结果是 Instant、Channel、Charge 之间无法共享：

- 打断逻辑
- 冷却逻辑
- 清理逻辑
- 日志与回放

### 3. 打断没有 Cleanup 路径

系统经常只处理“正常结束”，但没处理：

- 中途取消
- 被沉默阻止
- 目标消失
- 引导期间死亡

这种系统一上复杂技能，就会出现残留状态。

---

## 这一篇真正想留下来的结论

技能系统如果没有显式生命周期，它最后一定会把时序控制权散到动画、协程、投射物、Buff 和 UI 里。

更稳的做法是先承认：

`技能不是一个函数调用，而是一段带状态、带分支、可中断的生命周期。`

只要这层骨架站住了，你后面再去接：

- Effect System
- Buff System
- 动画事件
- 网络同步

就都有统一的落点。

所以这一篇最短的结论可以记成：

`不要为每种技能类型各写一套逻辑。先做一套统一生命周期骨架，再让 Instant、Cast、Channel、Charge、Toggle、Passive、Combo 选择经过哪些状态。`
