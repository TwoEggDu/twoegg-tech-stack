---
date: "2026-03-27"
title: "技能系统深度 04｜输入与施法请求：按键、输入缓冲、技能队列、取消窗口怎么接"
description: "技能系统和输入系统真正接上的地方，不是按钮映射，而是施法请求的生成与消费。按键、输入缓冲、技能队列、取消窗口这些机制如果边界不清，很快就会把技能系统和动画、角色控制一起搅乱。"
slug: "skill-system-04-input-and-cast-request"
weight: 8004
tags:
  - Gameplay
  - Skill System
  - Input
  - Combat
  - Architecture
series: "技能系统深度"
---

> 玩家按下一个键，不等于技能已经释放。更准确地说：`玩家按下的是输入，技能系统消费的是施法请求。`

很多项目里，技能系统和输入系统最早是直接连在一起的：

```csharp
if (Input.GetKeyDown(KeyCode.Q))
{
    skillSystem.Cast("Fireball");
}
```

这在原型期没有任何问题。

但只要项目开始引入下面这些需求，问题就会立刻冒出来：

- 技能需要蓄力，按下和松开不是一回事
- 技能有输入缓冲，硬直结束前提前按键也该吃进去
- 技能能排队，上一招结束后自动接下一招
- 某些技能能取消，某些不能取消
- UI、角色控制、AI、网络回放都可能发起“施法意图”

这时候你会发现：

`输入系统产生的不是“技能已经生效”，而是“想释放某个技能”的请求。`

所以这一篇真正要做的，就是把输入和技能之间那一层请求机制立出来。

---

## 为什么输入不能直接等于施法

输入是外部意图，施法是系统内部执行。

这两者中间至少隔着三件事：

- 这次输入是否合法
- 这次输入应该立刻执行、缓冲还是丢弃
- 这次输入最终会变成哪一种技能生命周期

举个最简单的例子。

玩家在角色硬直还剩 `0.12s` 的时候提前按下 `Q`。

如果你把 `KeyDown(Q)` 直接等价为 `Cast(Fireball)`，系统只会得到两种粗暴结果：

- 现在不能放，直接丢掉
- 现在硬塞进去，打断当前状态

但真正更合理的第三种结果往往是：

`接受这次输入，放进缓冲区，等当前窗口允许时再转成施法请求。`

所以这里第一层边界就出来了：

- 输入系统负责采集玩家意图
- 技能系统负责消费施法请求
- 中间需要一层 Request

---

## 我建议的最小输入链路

如果只想先把骨架搭稳，我建议把链路拆成这样：

```text
设备输入
-> Input Action
-> Skill Intent
-> Cast Request
-> Skill Queue / Buffer
-> Skill Lifecycle
```

这里每一层解决的问题不同。

### 设备输入

键盘、手柄、触屏、鼠标。

这层只回答：用户做了什么物理动作。

### Input Action

把物理输入映射成游戏语义。

例如：

- `Q` -> `AbilitySlot.Primary`
- 右摇杆按下 -> `AbilitySlot.Ultimate`
- 长按 -> `ChargeStart`
- 松手 -> `ChargeRelease`

### Skill Intent

这层表达的是：

`我想用哪个能力槽位，带着什么输入语义。`

例如：

- 想释放 `PrimarySkill`
- 想开始蓄力
- 想取消当前引导

### Cast Request

当意图被转译成技能系统可处理对象时，就进入 `Cast Request`。

它应该包含：

- 谁发起的
- 想放哪个技能
- 目标信息或方向信息
- 发起时间
- 这是不是来自缓冲或队列

### Skill Queue / Buffer

请求不一定立刻执行。

它可能：

- 立即消费
- 进入缓冲区等待时机
- 进入队列等待前一个技能结束
- 被丢弃

### Skill Lifecycle

只有请求真正被消费之后，才会进入技能生命周期。

---

## Cast Request 应该长什么样

我建议把它当成显式对象，而不是几个散参数。

```csharp
public class CastRequest
{
    public Entity requester;
    public SkillInstance skill;

    public Vector3 aimDirection;
    public Entity explicitTarget;
    public Vector3? targetPoint;

    public float requestTime;
    public RequestSource source; // Player / AI / Replay / Network
    public bool fromBufferedInput;
}
```

这个对象的价值非常高：

- 它把输入和技能生命周期之间接起来
- 它能进入日志和回放
- 它让 AI 和玩家走同一套入口
- 它让网络重放时不必伪造按键

只要你把输入直接写死成 `Cast(skillId)`，这些能力后面都会补得很痛苦。

---

## 输入缓冲为什么值得单独讲

输入缓冲不是“手感小优化”，而是技能系统时序的一部分。

它解决的问题是：

`玩家按得对，但按键发生的时机比技能可接受窗口稍早。`

最常见的场景包括：

- 动作游戏里，上一招后摇快结束时提前按下一招
- 连段游戏里，在可接段窗口前提前给输入
- 蓄力技能结束时，玩家提前给了取消或翻滚输入

如果没有输入缓冲，系统会给玩家一种很差的感觉：

`我明明按了，但角色不认。`

一个最小缓冲模型可以只是：

```csharp
public class BufferedInput
{
    public CastRequest request;
    public float expireTime;
}
```

然后在每次状态更新时检查：

- 当前是否进入可消费窗口
- 缓冲请求是否还没过期

---

## 技能队列和输入缓冲不是一回事

这两个概念特别容易混。

### 输入缓冲

强调的是：

- 输入已经发生
- 当前暂时不能执行
- 允许在短时间内记住它

### 技能队列

强调的是：

- 有意识地安排多个技能顺序执行
- 上一个技能结束后，下一个自动接上

换句话说：

- 输入缓冲更像“短时记忆”
- 技能队列更像“执行计划”

动作游戏和 MOBA 往往更重输入缓冲；自动战斗、AI、某些 MMO 更常用技能队列。

---

## 取消窗口必须是系统规则，不该只靠动画蒙

技能系统接输入的另一个关键点，是“取消窗口”。

玩家什么时候可以：

- 打断当前技能
- 翻滚取消
- 接普攻
- 接下一个技能

这件事如果没有系统规则，最后就会只剩两种实现：

- 全靠动画状态机硬切
- 全靠代码里到处埋 if

更稳的做法是把取消窗口也显式化。

例如：

```csharp
public class CancelPolicy
{
    public bool canMoveCancel;
    public bool canSkillCancel;
    public bool canDodgeCancel;
    public float cancelOpenTime;
}
```

然后生命周期层负责回答：

`当前阶段是否允许消费某类新请求。`

这比“动画播到某帧才让切”更稳定，因为动画可以参与窗口判定，但不独占规则。

---

## 玩家、AI、网络回放最好共享同一个请求入口

这是架构上特别关键的一点。

很多系统的问题在于：

- 玩家技能走一套入口
- AI 技能走另一套入口
- 网络同步又走第三套入口

最后同一个技能要维护三种行为。

更稳的做法是：

- 玩家输入生成 `CastRequest`
- AI 决策生成 `CastRequest`
- 网络重放恢复 `CastRequest`

然后统一交给技能系统消费。

也就是说，输入系统只是请求生产者之一，不是技能系统的唯一上游。

---

## 一个最小的请求消费器应该做什么

我建议技能系统里至少有一层明确的请求消费器：

```csharp
public class CastRequestConsumer
{
    public CastConsumeResult TryConsume(CastRequest request)
    {
        if (!CanAcceptRequestNow(request))
            return CastConsumeResult.Buffered;

        return StartCast(request);
    }
}
```

这个消费器至少要能分出几种结果：

- `Accepted`
- `Buffered`
- `Queued`
- `Rejected`

只要结果是显式的，系统就更容易：

- 给 UI 反馈
- 打调试日志
- 做手感调优

---

## 这一篇真正想留下来的结论

技能系统和输入系统真正接上的地方，不是按键映射，而是施法请求。

只要你不把这层单独立出来，后面很快就会把：

- 输入缓冲
- 技能队列
- 取消窗口
- AI 施法
- 网络回放

这些能力全都搅进技能执行本体里。

所以这一篇最短的结论可以直接记成：

`输入系统负责产生意图，技能系统负责消费施法请求。中间的 Cast Request、输入缓冲、技能队列和取消窗口，才是技能手感和工程稳定性的真正连接层。`
