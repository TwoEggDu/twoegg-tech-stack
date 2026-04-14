---
date: "2026-03-27"
title: "技能系统深度 06｜目标选择与命中：单体、范围、投射物、锁定、碰撞、命中时机怎么拆"
description: "技能系统从“想打谁”到“真的命中了谁”中间隔着完整的一条链路：目标选择、空间查询、投射物、碰撞、命中时机和效果触发。把这些都塞进一个 target 参数里，很快就会失控。这篇把目标和命中层拆开。"
slug: "skill-system-06-targeting-and-hit-resolution"
weight: 8006
tags:
  - Gameplay
  - Skill System
  - Combat
  - Targeting
  - Hit Resolution
series: "技能系统深度"
series_order: 6
---

> 技能系统里，`“我想打谁”` 和 `“最后真的打到了谁”` 不是同一件事。

很多技能系统最早都会把目标理解成一个参数：

```csharp
Cast(skill, target);
```

这对最简单的单体锁定技能没问题。

但只要项目里出现下面这些技能，你就会发现这个抽象太粗了：

- 指向地点的范围技能
- 扇形、圆形、矩形区域技能
- 投射物技能
- 穿透技能
- 锁定目标但会被位移躲开的技能
- 需要碰撞检测和命中窗口的近战技能

也就是说，技能系统里的“目标”至少分成两步：

- `Targeting`：这次技能打算怎么选目标
- `Hit Resolution`：在真实运行时，最后到底命中了谁

这两层如果不拆开，系统很快就会变成：

`既没有稳定的目标选择模型，也没有稳定的命中结算模型。`

---

## 先拆开三件事

我建议先把这一层拆成三件完全不同的事。

### 1. 目标选择（Targeting）

回答的是：

- 这次技能理论上想覆盖谁
- 选目标的规则是什么

比如：

- 单体锁定
- 地点释放
- 朝向扇形
- 角色周围圆形范围

### 2. 空间执行（Delivery）

回答的是：

- 技能是立即生效，还是通过投射物/区域/近战判定送达

例如：

- 立即结算
- 发射飞行物
- 在地面生成持续区域
- 在前方一段时间内持续判定

### 3. 命中结算（Hit Resolution）

回答的是：

- 最终命中了谁
- 在什么时候命中
- 每个目标命中几次
- 命中后如何触发效果

这三层一拆开，很多技能类型都能被统一表达。

---

## Targeting 不该只是一个 target 参数

真正稍微复杂一点的技能，目标规则本身就是定义的一部分。

我更建议把目标选择写成显式定义：

```csharp
public class TargetingDef
{
    public TargetMode mode; // Single / Point / Cone / Circle / Line / Self
    public float range;
    public float radius;
    public float angle;
    public TargetFactionRule factionRule;
    public bool requireLineOfSight;
}
```

这样技能定义回答的是：

- 需要单体还是地点
- 覆盖范围几何形状是什么
- 对敌、对友还是对自己
- 是否需要 LOS

而不是简单地把一切都压成一个 `Entity target`。

---

## Targeting 和 Delivery 必须分开

这是最容易被混掉的一层。

举个例子。

一个火球术：

- Targeting：锁定单体敌人
- Delivery：生成投射物飞过去
- Hit Resolution：投射物真正碰到目标时才命中

一个地面火雨：

- Targeting：选择一个地面点
- Delivery：在地面生成区域
- Hit Resolution：区域内单位每秒结算一次

一个近战横扫：

- Targeting：角色前方扇形
- Delivery：在某个动画命中窗口做一次扇形查询
- Hit Resolution：查询结果中的目标被命中

所以：

- Targeting 说的是“技能想打哪类对象”
- Delivery 说的是“技能如何把这次打击送达”
- Hit Resolution 说的是“最终实际命中的结果”

---

## 常见的 Delivery 模型

如果要先搭稳定骨架，我建议先把送达方式归成几类。

### 1. Immediate

校验通过后立即结算。

适合：

- 瞬发治疗
- 瞬发增益
- 某些闪现或净化

### 2. Projectile

通过投射物送达。

适合：

- 火球
- 箭矢
- 飞刀

这里的关键问题变成：

- 速度
- 飞行时间
- 是否追踪
- 是否穿透
- 是否可被阻挡

### 3. Area

在空间中生成一个区域。

适合：

- 地面火圈
- 治疗图腾
- 毒云

关键问题是：

- 区域持续多久
- Tick 间隔多久
- 同一目标是否可重复命中

### 4. Melee Window

在一个短暂命中窗口里做碰撞/范围检测。

适合：

- 近战挥砍
- 突刺
- 旋风斩

这里最关键的是“命中窗口”，不是投射物飞行。

---

## 锁定、瞄准和实际命中不是一回事

这在设计和实现里都很重要。

### 锁定（Lock-On）

系统确定一个优先目标，供技能使用。

### 瞄准（Aim）

系统决定施法方向、地点或朝向。

### 命中（Hit）

真实运行时里，碰撞、查询或区域更新后得到的最终命中结果。

例如锁定火球：

- 起手时锁定了 A
- 投射物发出后，A 闪现离开
- 最终可能：
  - 仍然追踪命中 A
  - 因 LOS 丢失失效
  - 在途中被阻挡

也就是说：

`锁定不是命中承诺，瞄准也不是命中承诺。`

---

## 命中时机是技能系统的一部分，不是表现细节

很多团队容易把“什么时候算命中”交给动画或特效自己决定。

但实际上命中时机直接决定：

- 伤害什么时候结算
- Buff 什么时候挂上
- 受击反馈什么时候触发
- 冷却什么时候开始后续逻辑

所以命中时机必须是系统可表达的。

典型有三种：

### 1. Start Hit

技能进入 Active 时立刻命中。

### 2. Event Hit

等待某个明确事件点命中。

例如：

- 动画事件
- 投射物碰撞事件
- 区域 Tick 事件

### 3. End Hit

少数技能会在结束时统一结算。

比如某些蓄力收招技。

---

## 近战和投射物不要混成一套粗暴判定

看上去它们都叫“攻击目标”，但送达方式完全不同。

### 近战技能

更关心：

- 命中窗口
- 判定体积
- 同一目标去重
- 前后摇和攻击方向

### 投射物技能

更关心：

- 出生点
- 速度与轨迹
- 追踪与否
- 碰撞对象
- 生命周期

如果把它们都硬写成：

```csharp
FindTargetsInRange();
ApplyDamage();
```

那系统很快就表达不了：

- 穿透箭
- 弹射火球
- 命中第一个目标后爆炸
- 挥刀只在 0.18s 到 0.28s 内有判定

---

## 一个最小命中结果对象值得单独存在

和前面几篇一样，我更建议不要让命中结果只是一个临时列表。

```csharp
public class HitResult
{
    public Entity attacker;
    public Entity target;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public float hitTime;
    public DeliveryType deliveryType;
}
```

它的价值在于：

- 效果执行时知道命中发生在哪里
- 特效系统知道该把 VFX/SFX 挂在哪
- 日志和回放能记录真正命中信息
- 网络同步能更明确地表达结果

---

## 一个稳定的近战命中流程

以“横扫”为例。

它的流程更合理地应该是：

```text
CastRequest
-> Skill enters Windup
-> Animation / Timeline opens hit window
-> Delivery = MeleeWindow
-> Perform cone query
-> Filter by faction / distance / occlusion
-> Generate HitResult list
-> Resolve effects on each hit target
-> Close hit window
```

这里最重要的是：

- 目标选择规则是扇形
- 送达方式是近战命中窗口
- 实际命中结果要等查询结束后才知道

---

## 一个稳定的投射物命中流程

以“火球”为例。

```text
CastRequest
-> Skill enters Windup
-> Spawn projectile at event point
-> Projectile moves in world
-> On collision / expiration:
    - build HitResult
    - resolve effects
    - destroy projectile
```

此时技能系统和投射物系统的边界也会更清楚：

- 技能系统负责定义规则和创建送达对象
- 投射物负责空间中的运行
- 命中后仍然回到统一的 Effect Resolution

---

## 一个坏目标系统通常怎么坏

### 1. 一切都压成 `target`

最后点目标、扇形、投射物、范围技能全部无法统一表达。

### 2. 锁定、瞄准、命中混成一件事

于是系统无法处理“起手锁定了，但实际没命中”的情况。

### 3. Delivery 和 Effect 没有边界

投射物一边飞一边自己算伤害、上 Buff、播特效，最后完全脱离技能主框架。

### 4. 命中时机只靠动画或特效隐式触发

这样逻辑就很难被统一调试和回放。

---

## 这一篇真正想留下来的结论

技能系统从“想打谁”到“真的打到了谁”，中间隔着完整的一条链：

- Targeting
- Delivery
- Hit Resolution

把这三层切开之后，单体技能、范围技能、投射物技能、近战技能才有机会被统一落进同一套技能框架。

所以这一篇最短的结论就是：

`不要把目标理解成一个参数。技能系统真正需要的是：先定义怎么选目标，再定义怎么把攻击送达，最后再统一结算真正命中的结果。`
