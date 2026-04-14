---
date: "2026-04-13"
title: "技能系统深度 30｜Combo 系统架构：连招判定、输入窗口、动画衔接与取消树"
description: "Combo 的本质不是'按 A 再按 B'，而是一棵以输入序列为边、以技能状态为节点的有向图。这篇拆清输入窗口的三种类型、动画衔接的两种驱动模式、取消树的优先级设计，以及格斗/ARPG/MOBA 三种 Combo 模型的差异。"
slug: "skill-system-30-combo-architecture"
weight: 8030
tags:
  - Gameplay
  - Skill System
  - Combo
  - Input
  - Animation
series: "技能系统深度"
series_order: 30
---

> Combo 不是 if-else 链，是一棵以输入序列为边、技能状态为节点的有向图。

很多项目的连招系统最早都长成这样：

```csharp
if (currentSkill == "LightAttack1" && input == Attack)
    PlaySkill("LightAttack2");
else if (currentSkill == "LightAttack2" && input == Attack)
    PlaySkill("LightAttack3");
else if (currentSkill == "LightAttack1" && input == Heavy)
    PlaySkill("HeavyFinisher");
```

在原型期这没什么问题。普攻三连，外加一个重击分支，四个 if 搞定。

但只要项目开始加下面这些需求，这套写法就会迅速崩掉：

- 连段路线从 1 条变成 8 条
- 不同武器有不同的连段树
- 某些连段只在特定 Buff 状态下可用
- 连段之间需要输入缓冲
- 策划要能在编辑器里配连招表，不想改代码

这时候你会发现：

`连招判定的本质不是"当前技能 + 下一个输入 = 下一个技能"，而是在一棵有向图上的遍历。`

这篇就是把这棵图的结构立出来。

---

## 这篇要回答什么

1. Combo 系统的图模型长什么样。
2. 输入窗口的三种类型各自解决什么问题。
3. 动画衔接的两种驱动模式各有什么 tradeoff。
4. 取消树的优先级设计怎样做到可配置。
5. 格斗、ARPG、MOBA 三种品类的 Combo 模型有什么本质差异。

---

## Combo 的本质：一棵有向图

把连招系统画成图，节点是技能状态，边是输入条件。

```
               ┌─ [Heavy] ──> HeavyFinisher
               │
Idle ─[Light]─> Light1 ─[Light]─> Light2 ─[Light]─> Light3
                  │                   │
                  └─[Dodge]─> Roll    └─[Skill]─> UpperSlash ─[Light]─> AirCombo
```

每条边上不只是"按了什么键"，还可能带条件：

- 输入类型：Light / Heavy / Skill / Dodge / Direction + Attack
- 时间窗口：必须在当前技能的第 12 帧到第 24 帧之间输入
- 前置状态：需要持有某个 Tag 或 Buff
- 方向修饰：前 + 攻击 vs 后 + 攻击触发不同分支

用数据结构表达：

```csharp
public class ComboEdge
{
    public string fromSkillId;
    public string toSkillId;
    public InputType requiredInput;
    public int windowStartFrame;   // Cancel Window 起始帧
    public int windowEndFrame;     // Cancel Window 结束帧
    public TagRequirement[] tags;  // 需要持有的 Tag
    public int priority;           // 同一节点多条边时的优先级
}

public class ComboGraph
{
    public string graphId;
    public ComboEdge[] edges;

    public string Evaluate(string currentSkillId, int currentFrame, InputType input, TagContainer tags)
    {
        // 找到所有从 currentSkillId 出发、满足条件的边
        // 按 priority 排序，返回最高优先级的 toSkillId
        // 没有匹配则返回 null
    }
}
```

这个模型的好处是：

- 策划可以在编辑器里拖节点、连线
- 不同武器各自一棵图，切武器就是切图
- 运行时查询是确定性的：当前节点 + 当前帧 + 输入 + 状态 → 下一个节点
- 新增连招路线不需要改代码

---

## 输入窗口的三种类型

Combo 系统里最容易混的概念就是"窗口"。至少有三种窗口，它们各自解决不同的问题。

### Cancel Window：当前技能的可取消帧

Cancel Window 回答的问题是：`当前技能在动画的哪些帧可以被打断。`

一个普攻动画 30 帧：

- 第 0-11 帧：前摇，不可取消
- 第 12-20 帧：Active，命中已结算，可以被下一招取消
- 第 21-30 帧：后摇，可以被取消也可以自然播完

Cancel Window 就是 `[12, 30]` 这段。

不同的后续技能可以有不同的 Cancel Window。普攻接普攻可能从第 12 帧开始，但普攻接闪避可能从第 8 帧就可以——因为闪避的取消优先级更高。

配置方式：

```csharp
public class CancelWindowDef
{
    public string skillId;
    public int startFrame;
    public int endFrame;
    public CancelCategory[] allowedCategories; // NormalAttack, Skill, Dodge, Ultimate
}
```

### Chain Window：连招输入的接受窗口

Chain Window 回答的问题是：`按下一招的有效时间段是什么。`

Cancel Window 是"当前技能允许被打断的帧"，Chain Window 是"系统接受下一个输入的时间段"。它们经常重叠，但不一定相同。

一种常见设计：

- Cancel Window：`[12, 30]`（技能逻辑层面可以被打断）
- Chain Window：`[10, 28]`（输入系统层面接受连招输入）

Chain Window 比 Cancel Window 稍早开始，是为了配合输入缓冲。玩家在第 10 帧按下攻击，此时 Cancel Window 还没开放（要第 12 帧才行），但 Chain Window 已经在接受输入了。这个输入会被缓存，等到第 12 帧 Cancel Window 开放时自动消费。

这就引出第三种窗口。

### Buffer Window：输入缓冲窗口

Buffer Window 回答的问题是：`玩家提前按了下一招，系统记住多久。`

[第 04 篇]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}}) 里讲过输入缓冲的基本模型。在 Combo 场景下，Buffer Window 的设计需要更精细。

```csharp
public class BufferConfig
{
    public float bufferDuration;  // 缓冲时长，通常 0.1s - 0.2s
    public int maxBufferedInputs; // 最多缓冲几个输入，通常 1-2 个
    public bool consumeOnWindow;  // 窗口开放时是否自动消费最早的缓冲输入
}
```

三种窗口之间的时序关系：

```
帧:    0    5    10   12   20   28   30
       |----|----|----|----|----|----|
前摇   ████████████
Cancel Window          ████████████████
Chain Window        █████████████████
Buffer Window    ██████(提前输入被记住)
```

Buffer Window 的存在让玩家不需要精确到帧地按键。在格斗游戏里 Buffer Window 通常是 3-5 帧（50-83ms at 60fps），在 ARPG 里通常是 6-12 帧（100-200ms）。这个数值直接影响连招手感：太短则"吃不进去"，太长则"操作模糊"。

---

## 动画衔接的两种驱动模式

确定了"从技能 A 切到技能 B"之后，下一个问题是：动画怎么衔接。

### 模式一：动画驱动

动画系统自己控制切换时机和过渡方式。

在 Unreal 中的典型做法：

- 每个连段阶段是一个 Montage Section
- Section 之间用 `MontageSection -> NextSection` 配置转接
- AnimNotify 驱动逻辑事件（命中判定、特效触发）
- 过渡用 BlendIn/BlendOut 控制混合时长

在 Unity 中的典型做法：

- 用 Animator StateMachine 或 Playable Graph
- 技能 A 的动画 State 有 Transition 指向技能 B
- Transition 配 Blend Duration、Exit Time
- AnimationEvent 驱动逻辑回调

动画驱动的好处：

- 策划和美术在动画编辑器里就能看到完整的连段流
- 动画混合质量高，手感自然
- 不需要代码参与过渡逻辑

动画驱动的问题：

- 动画状态机变成了技能逻辑的宿主，逻辑和表现耦合
- 修改连段路线需要改 Animator Controller，不是改数据表
- 网络同步困难：服务器没有动画系统，无法复现 Transition 时序
- 不同角色如果动画时长不同，同一套逻辑的 Cancel Window 帧数就不一样

### 模式二：逻辑驱动

技能状态机决定切换，动画系统跟随播放。

```csharp
// 技能系统决定切换
void OnComboTransition(string fromSkill, string toSkill)
{
    skillStateMachine.ExitSkill(fromSkill);
    skillStateMachine.EnterSkill(toSkill);

    // 通知表现层
    presentationBridge.PlaySkillAnimation(toSkill, blendDuration: 0.08f);
}
```

逻辑驱动的好处：

- 技能执行链完全由技能系统控制，不依赖动画状态
- 服务器和客户端走同一套状态机
- Cancel Window 用逻辑帧定义，和动画时长解耦
- 修改连段路线只需要改 ComboGraph 数据表

逻辑驱动的问题：

- 需要额外做动画对齐：逻辑帧和动画帧不完全同步时出现滑步
- 手感调试更依赖参数，不像动画编辑器那样所见即所得
- 动画混合需要代码控制，美术没法直接在编辑器里预览

### 我的建议

大多数项目的实际做法是混合模式：

`逻辑驱动决定"切到哪个技能"，动画驱动控制"怎么混合过去"。`

具体来说：

- ComboGraph 的 Evaluate 结果决定下一个技能是什么（逻辑驱动）
- 每个技能节点配一个 AnimationProfile，包含动画资源、BlendIn 时长、过渡类型（动画驱动）
- 服务器只跑逻辑驱动那一层，不关心 BlendIn 是 0.05s 还是 0.1s
- 客户端在逻辑驱动的基础上，用 AnimationProfile 做表现层过渡

```csharp
public class ComboAnimProfile
{
    public string animationClip;
    public float blendInDuration;       // 通常 0.05s - 0.15s
    public float blendOutDuration;
    public TransitionType transitionType; // CrossFade / Snap / CustomCurve
    public float playbackSpeed;          // 1.0 = 原速，某些连段会加速播放
}
```

这样既保住了逻辑层的确定性，又保住了表现层的手感。

---

## 取消树：哪些技能可以取消哪些

取消树回答的是：`当玩家在技能 A 执行中按了技能 B，B 能不能打断 A。`

最简单的实现是一个二维矩阵：

```
被取消方 →     LightAtk  HeavyAtk  Skill  Dodge  Ultimate
取消方 ↓
LightAtk          ✓         ✗       ✗      ✗       ✗
HeavyAtk          ✓         ✗       ✗      ✗       ✗
Skill             ✓         ✓       ✗      ✗       ✗
Dodge             ✓         ✓       ✓      ✗       ✗
Ultimate          ✓         ✓       ✓      ✓       ✗
```

但真实项目里，矩阵维护不住。当技能超过 50 个，矩阵变成 2500 个格子，策划不可能逐个配。

### 三种配置方式

**方式一：优先级数值。**

每个技能有一个 `cancelPriority` 值。优先级高的可以取消优先级低的。

```csharp
public class SkillCancelDef
{
    public int cancelPriority;       // LightAtk=10, Skill=30, Dodge=50, Ultimate=80
    public bool canBeSelfCancelled;  // 能不能被同类型取消
}
```

判定逻辑：

```csharp
bool CanCancel(SkillInstance current, SkillInstance incoming)
{
    if (!current.IsInCancelWindow())
        return false;
    return incoming.cancelPriority > current.cancelPriority
        || (incoming.cancelPriority == current.cancelPriority && current.canBeSelfCancelled);
}
```

简单直观，但表达力有限。无法表达"闪避能取消技能 A 但不能取消技能 B"这类特殊规则。

**方式二：Tag 规则。**

每个技能声明两组 Tag：

- `cancelledByTags`：能被哪些 Tag 的技能取消
- `cancelTags`：自己拥有哪些取消 Tag

```csharp
public class SkillCancelTagDef
{
    public GameplayTag[] cancelTags;       // 例: [Melee, Interrupt]
    public GameplayTag[] cancelledByTags;  // 例: [Dodge, Ultimate, Interrupt]
}
```

判定逻辑：

```csharp
bool CanCancel(SkillInstance current, SkillInstance incoming)
{
    if (!current.IsInCancelWindow())
        return false;
    return current.cancelledByTags.OverlapsWith(incoming.cancelTags);
}
```

表达力强，可以支持任意复杂的取消规则。但 Tag 一多容易失控，需要策划严格维护 Tag 命名规范。

**方式三：混合模式。**

优先级做大面控制（闪避一定能取消普攻），Tag 做特殊规则（某些 Boss 技能不可取消）。

```csharp
bool CanCancel(SkillInstance current, SkillInstance incoming)
{
    if (!current.IsInCancelWindow())
        return false;
    // 先检查硬性标记
    if (current.HasTag("Uncancellable"))
        return false;
    // 再查 Tag 规则
    if (current.cancelledByTags.OverlapsWith(incoming.cancelTags))
        return true;
    // 最后兜底优先级
    return incoming.cancelPriority > current.cancelPriority;
}
```

大部分项目最终都会走到这个混合模式上。

---

## 三种游戏类型的 Combo 差异

同样是"连招"，格斗、ARPG、MOBA 三种品类对 Combo 系统的要求差异极大。

### 格斗游戏

代表：Street Fighter 6、Guilty Gear Strive、Tekken 8。

核心特征：

- **帧精确**：Cancel Window 精度到 1 帧（16.67ms at 60fps）
- **窗口极窄**：链接技（Link）的 Cancel Window 通常 1-3 帧
- **连招路线固定**：2B > 2H > 236S 是确定的，不是随机选择
- **输入方向参与判定**：236 (下前) + P 和 623 (前下前) + P 是不同的技能
- **Buffer Window 短**：通常 3-5 帧，过长会导致"乱出招"
- **帧数据公开**：启动帧、Active 帧、Recovery 帧是策划核心参数

格斗游戏的 ComboGraph 更像一张严格的表：

```
2B (7F startup, 3F active, 12F recovery)
  Cancel Window: [10, 15]  // Active 结束后 5 帧内可取消
  → 2H: Chain Window [8, 14], 输入 Down+Heavy
  → 5H: Chain Window [10, 15], 输入 Heavy
  → 236S: Chain Window [10, 22], 输入 QCF+Special (特殊技有更宽的窗口)
```

### ARPG

代表：鬼泣 5、Monster Hunter、原神。

核心特征：

- **手感优先**：Buffer Window 宽（100-200ms），保证"按了就有反应"
- **连段分支多**：从普攻第二段可以分支到 4-5 条不同路线
- **方向输入参与**：前+攻击、后+攻击触发不同招式
- **Cancel Window 宽松**：格斗 1-3 帧，ARPG 通常 6-15 帧
- **Hit Confirm**：某些分支只在命中目标后才开放（空挥不出连段）
- **武器切换切图**：不同武器对应不同的 ComboGraph

ARPG 的 ComboGraph 更像一棵分支丰富的树：

```
LightAtk1
  ├─ [Light] → LightAtk2
  │    ├─ [Light] → LightAtk3 (终结)
  │    ├─ [Heavy] → LauncherAtk (Hit Confirm: 必须命中才出)
  │    └─ [Dodge+Direction] → DodgeCounter
  ├─ [Heavy] → ChargeSlash (蓄力)
  └─ [Skill] → WeaponSkill
```

### MOBA

代表：英雄联盟、Dota 2、王者荣耀。

核心特征：

- **技能独立 CD**：每个技能自己有冷却，不存在"连段路线"
- **Combo 是组合而非连招**：QER 不是动画连接，是技能组合释放
- **输入缓冲简单**：通常只缓冲 1 个操作，不需要多级缓冲
- **Cancel Window 概念弱**：大部分技能要么可以被移动取消，要么完全不可取消
- **Order Queue 取代 Combo Graph**：指令队列（移动 → 放 Q → 移动 → 放 W）比连招图更重要

MOBA 里说的"Combo"更准确的名字是"技能连携"——策划设计的是技能之间的协同效果，而不是一套需要帧精确输入的连招路线。

它对 Combo 子系统的需求远比格斗和 ARPG 轻。大部分 MOBA 项目不需要 ComboGraph，只需要：

- 施法队列（CastQueue）
- 基础的输入缓冲
- 技能之间的取消规则（通常是"移动取消一切"）

---

## 与前序篇目的衔接

这篇涉及的几个核心机制，在系列前序篇目中已经分别讲过基础模型。

[第 03 篇]({{< relref "system-design/skill-system-03-lifecycle.md" >}}) 讲了技能生命周期的状态骨架。Combo 系统中每个节点对应的仍然是一次完整的生命周期遍历（Windup → Active → Recovery）。ComboGraph 做的事情是在 Recovery 阶段检查是否有合法的输入匹配到某条边，如果有就中断当前生命周期、启动下一个节点的生命周期。

[第 04 篇]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}}) 讲了输入缓冲和施法请求。Combo 场景下的输入缓冲在结构上和 04 篇完全一致（BufferedInput + expireTime），区别在于 Combo 的缓冲消费条件是 Cancel Window 和 Chain Window 的交集，而不是简单的"当前技能结束"。

[第 09 篇]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}}) 讲了动画和逻辑的解耦。Combo 的动画衔接就是这个问题的具体实例——逻辑层只关心"切到哪个技能"，表现层负责 BlendIn 和 CrossFade 的具体参数。

---

## 这篇的结论

Combo 系统的核心是三件事：

1. **图模型**。节点是技能状态，边是输入条件 + 时间窗口 + 前置要求。用数据驱动，不要硬编码 if-else。

2. **三种窗口分清**。Cancel Window 管逻辑可打断帧、Chain Window 管输入接受时段、Buffer Window 管提前输入记忆。混在一起是 Combo 手感问题的第一大来源。

3. **取消规则可配置**。优先级 + Tag 的混合模式覆盖大部分需求。纯矩阵在技能数量超过 30 时就不可维护了。

不同品类对 Combo 的要求差一个数量级。格斗要帧精确，ARPG 要分支丰富，MOBA 更多是技能队列而非连招图。在项目早期就应该明确你的 Combo 属于哪一类，然后选择对应复杂度的方案，而不是把格斗的精度和 ARPG 的分支数同时做进去。
