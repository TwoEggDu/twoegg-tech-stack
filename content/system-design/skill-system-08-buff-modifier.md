---
date: "2026-03-27"
title: "技能系统深度 08｜Buff / Modifier：叠层、刷新、覆盖、快照、实时重算应该怎么建模"
description: "技能系统真正容易失控的地方，通常不是主动技能，而是 Buff、Modifier 和属性联动。叠层、刷新、覆盖、快照、实时重算这些规则如果一开始没立住，后面几乎一定会出错。这篇专门拆 Buff 系统。"
slug: "skill-system-08-buff-modifier"
weight: 8008
tags:
  - Gameplay
  - Skill System
  - Combat
  - Buff
  - Modifier
  - Architecture
series: "技能系统深度"
series_order: 8
---

> 很多战斗系统不是死在“放技能”上，而是死在“Buff 到底怎么叠、怎么算、什么时候失效”上。

主动技能通常还比较容易讨论。

因为它们大多是：

- 发起
- 执行
- 结束

问题相对集中。

Buff 不一样。

Buff 的麻烦在于它会持续留在系统里，并不断和别的系统交叉：

- 影响属性
- 影响技能可释放条件
- 带标签
- 周期触发
- 互相覆盖
- 被驱散
- 和装备、被动、地形效果一起叠加

所以很多项目真正开始失控，往往不是“多了一个技能”，而是：

`多了 20 个会互相作用的 Buff。`

这一篇就专门处理这件事。

---

## Buff 和 Modifier 不是一回事

先把最容易混掉的一层拆开。

### Buff 是持续状态容器

Buff 回答的是：

- 这是什么状态
- 持续多久
- 当前多少层
- 每隔多久触发一次
- 附带哪些标签
- 结束时要不要清理什么

### Modifier 是数值影响项

Modifier 回答的是：

- 这个状态对某个属性产生什么影响
- 是加法、乘法、覆盖还是最终修正
- 影响多少

也就是说：

- Buff 是“挂在身上的东西”
- Modifier 是“这个东西对数值的具体影响”

一个 Buff 可以包含多个 Modifier。

例如：

```text
Buff: Berserk
  - Duration: 6s
  - Tags: State.Berserk
  - Modifiers:
      +20% Attack
      -10% Defense
```

如果把 Buff 和 Modifier 混成一回事，后面很快就会出现：

- 数值逻辑和持续逻辑缠在一起
- 覆盖、叠层和刷新规则难以表达

---

## Buff 系统至少要回答哪几件事

我建议你一开始就强迫 Buff 系统回答下面这些问题。

### 1. Buff 怎么进入目标

它是：

- 命中后挂上
- 区域内持续存在
- 装备/被动永久附着
- 自己给自己施加

### 2. Buff 怎么离开目标

它可能：

- 时间到自然结束
- 被驱散
- 被覆盖替换
- 目标死亡时清空
- 某个条件触发后主动移除

### 3. Buff 怎么影响世界

它可能通过：

- Modifier 改属性
- Tag 改状态语义
- Tick 触发周期 Effect
- 监听事件后触发反应

### 4. Buff 之间怎么相互作用

它必须回答：

- 能不能叠层
- 叠层时刷新不刷新持续时间
- 同类 Buff 是独立存在还是只保留一个
- 新 Buff 来时是覆盖旧的还是拒绝生效

如果这四件事没有系统规则，后面所有边界都只能靠特判。

---

## 最常见的五种叠层模型

很多项目说“支持 Buff 叠层”，但其实没有把“叠层”说完整。

因为叠层至少有五种常见语义。

### 1. 只刷新持续时间，不加层

典型场景：

- 流血状态
- 标记状态

规则是：

- 身上已有同类 Buff
- 再来一次时，不增加效果强度
- 只把时间刷新回满

### 2. 加层，同时刷新持续时间

典型场景：

- 中毒
- 灼烧

规则是：

- 新增一层
- 整个 Buff 的剩余时间也一起刷新

### 3. 加层，但每层独立计时

典型场景：

- 连续命中的短时 Debuff

规则是：

- 每次施加生成一层独立实例
- 各层分别倒计时

### 4. 只保留最强一层

典型场景：

- 同类减速
- 同类减伤

规则是：

- 如果新来的更强，就替换
- 如果更弱，就忽略或只刷新时间

### 5. 互斥组覆盖

典型场景：

- 姿态类状态
- 武器附魔类状态

规则是：

- 同组只能有一个
- 新来时先移除旧的，再挂新的

Buff 系统如果不把这些模型显式建出来，后面一定会出现：

`策划嘴上说“就是普通叠层”，程序心里完全不知道是哪一种。`

---

## 刷新、覆盖、替换，最好分成三件事

很多系统一开始只有一个模糊操作：

`ApplyBuff()`

但真正项目里，这个动作至少分成三种语义。

### 刷新（Refresh）

同一个 Buff 还在，只把持续时间拉满。

### 叠加（Stack）

同一个 Buff 还在，但层数增加。

### 替换（Replace）

移除旧的，挂上新的。

把这三件事区分开之后，很多规则会清楚得多。

---

## Modifier 应该怎么组织

Modifier 系统最重要的，不是字段多不多，而是：

`修饰顺序是否稳定。`

如果项目里既有：

- 基础攻击力
- 装备加成
- Buff 百分比加成
- 战斗中临时乘区
- 最终伤害修正

那系统就必须先回答：

`这些修饰是按什么顺序叠的。`

一个很常见的稳定写法是：

```text
FinalValue =
(
    BaseValue
    + FlatBonus
)
* (1 + AdditivePercent)
* MultiplicativeBonus
+ FinalOffset
```

不同游戏公式会不一样，但关键不是具体公式，而是：

`顺序必须是系统规则，不能每个 Buff 自己决定。`

---

## 别让 Buff 直接改最终属性

最危险的写法之一是：

```csharp
void OnBuffApply()
{
    owner.Attack += 20;
}

void OnBuffRemove()
{
    owner.Attack -= 20;
}
```

它的问题不是“写法不优雅”，而是会在复杂场景下直接错：

- Buff 叠层时很难追
- 覆盖替换时可能多减或少减
- 中途换装备后，最终属性不对
- 被驱散、死亡、重生时清理路径很难补全

更稳的思路是：

- Buff 只注册 Modifier
- 属性系统统一重新计算最终值

例如：

```csharp
public class Modifier
{
    public StatType stat;
    public ModifierOp op; // Add / Mul / Override
    public float value;
    public object source;
}
```

然后属性系统从所有生效中的 Modifier 汇总求值。

---

## 快照和实时重算一定要先讲清楚

这是 Buff 系统里另一个必炸点。

一个 Buff 的数值，到底应该：

- 挂上时拍快照
- 还是每次 Tick / 查询时实时读取当前属性

两者都合理，但语义完全不同。

### 快照（Snapshot）

Buff 挂上时就把关键数值固定下来。

典型场景：

- “按施法瞬间攻击力计算的持续伤害”
- “按施法瞬间护盾强度生成的护盾值”

### 实时重算（Live Recompute）

Buff 生效期间，施法者或目标属性变化会影响后续结果。

典型场景：

- 持续回血受治疗加成影响
- 减速强度跟当前某属性实时关联

如果系统没有显式支持这两种模式，最后只会出现一种情况：

`设计想要快照，代码却在实时读；或者设计想要实时变，代码却把值拍死了。`

---

## Tag 最好由 Buff 来附着，而不是替代 Buff

前一篇说过：

- Tag 是状态语义
- Buff 是持续规则容器

在 Buff 系统里，我更建议这么用：

```text
Buff: Burn
  -> grants Tag: Status.Burning
  -> periodic damage every 1s

Buff: Silence
  -> grants Tag: State.CannotCast

Buff: Invulnerable
  -> grants Tag: State.Invulnerable
```

这样做的好处是：

- 施法校验只查 Tag
- Buff 负责持续时间和清理
- 语义层和运行时容器层分工清楚

---

## 一个稳定的 BuffRuntime 应该长什么样

如果只做最小骨架，我建议运行时对象里至少有：

```csharp
public class BuffRuntime
{
    public BuffDef def;
    public Entity owner;
    public Entity source;

    public int stacks;
    public float startTime;
    public float expireTime;
    public float nextTickTime;

    public StatSnapshot snapshot;
}
```

这里几个字段分别回答：

- `stacks`：当前层数
- `expireTime`：何时结束
- `nextTickTime`：周期性触发的下一次时间点
- `snapshot`：是否采用快照

只要这些状态显式存在，很多规则就不必靠隐式推断。

---

## 用一个 Burn Buff 走一遍

假设火球命中后挂一个 `Burn`：

```text
BuffDef: Burn
  - Duration: 3s
  - TickInterval: 1s
  - MaxStacks: 3
  - StackRule: AddStackAndRefreshDuration
  - GrantedTags:
      - Status.Burning
  - Modifiers:
      - Resistance.FireTaken +10%
  - TickEffects:
      - PeriodicDamage(10% MagicPower)
```

这个 Buff 系统需要能处理：

- 第一次挂上：创建 BuffRuntime
- 第二次挂上：层数 +1，刷新时间
- 每 1 秒：执行一次 Tick Effect
- 结束时：移除 GrantedTags 和 Modifier

只要这些都通过统一 BuffRuntime 和 Modifier 流程来做，系统就会稳很多。

---

## 一个坏 Buff 系统通常怎么坏

### 1. Buff 直接改最终属性

后面一定会在覆盖、驱散、死亡清理时出错。

### 2. 没有明确叠层语义

结果每个 Buff 都要单独问：

- 到底是刷新时间
- 还是加层
- 还是替换

### 3. 没有快照语义

结果设计和程序对同一个 Buff 的理解不一致。

### 4. Buff、Tag、Modifier 三层混掉

最后系统里只有一大团“状态”，谁负责持续时间、谁负责查询、谁负责改数值全混了。

---

## 这一篇真正想留下来的结论

主动技能决定的是“什么时候发起一次能力请求”，但战斗系统真正长期难维护的部分，往往是 Buff 和 Modifier。

所以我更建议把它们理解成：

- Buff：持续中的规则容器
- Modifier：对属性求值的影响项
- Tag：系统间查询状态语义的桥接语言

只要这三层切清楚，再把：

- 叠层
- 刷新
- 覆盖
- 快照
- 实时重算

这些规则显式建出来，Buff 系统才不会在项目中后期变成一团无法解释的例外集合。

如果把整篇压成最短一句话，那就是：

`Buff 负责“状态持续存在”，Modifier 负责“如何影响数值”，Tag 负责“让系统知道当前是什么状态”。三者切清楚，技能系统才有机会稳。`
