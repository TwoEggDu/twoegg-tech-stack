---
date: "2026-04-13"
title: "技能系统深度 18｜守望先锋 ECS 技能方案：组件是数据、系统是逻辑、技能是组合"
description: "Overwatch 用 ECS 做技能系统，技能不是对象而是 Component 组合，执行不是方法调用而是 System 驱动。这篇从 GDC 2017 的公开资料出发，拆清 ECS 下技能的表达方式、网络同步的天然适配、以及与 OOP 技能系统的核心差异。"
slug: "skill-system-18-overwatch-ecs-ability"
weight: 8018
tags:
  - Gameplay
  - Skill System
  - Overwatch
  - ECS
  - Networking
series: "技能系统深度"
series_order: 18
---

> Overwatch 没有用继承链来建技能系统。它证明了另一条路：`技能是 Component 的组合，执行是 System 的驱动，网络同步是 Component 快照的回滚。`

17 篇我们拆了 Dota 2 的数据驱动方案，它的核心是"声明式模型 + 事件驱动 Action"。

但 Dota 2 的底层仍然运行在一个传统的面向对象引擎上——技能是对象、英雄是对象、Modifier 是对象。
数据驱动改变的是策划和代码的交互方式，但没有改变运行时的对象模型。

Overwatch 做了更激进的事：它从引擎架构层面就选择了 ECS（Entity Component System），技能系统直接生长在 ECS 之上。

这不是"ECS 替代 OOP"的布道文。
这是一篇从 GDC 2017 公开资料出发，拆解 Overwatch 技能系统在 ECS 架构下的具体表达方式、网络同步的天然适配、以及与 OOP 技能系统的核心差异。

---

## 这篇要回答什么

这篇主要回答 7 个问题：

1. ECS 架构下，技能到底用什么方式表达——不是 class，不是配置表，而是 Component 组合。
2. 一个具体技能（比如闪光的闪现）的 Component 组成到底长什么样。
3. StatScript / ModScript 在 ECS 之上提供了怎样的脚本化配置层。
4. 确定性模拟为什么在 ECS 下天然可行，以及它对网络同步意味着什么。
5. 客户端预测 → 服务端权威 → 差异修正的网络模型如何依赖 ECS 的数据布局。
6. Timothy Ford 在 GDC 2017 "Overwatch Gameplay Architecture and Netcode" 中公开了哪些关键架构决策。
7. ECS 技能系统和 OOP 技能系统在增加技能、修改技能、调试方式上的 tradeoff。

---

## ECS 的前提：为什么要先讲架构再讲技能

在 Dota 2 和 GAS 的分析里，我们可以直接进技能系统，因为它们的技能系统和引擎架构之间有相对清晰的分层。

但 Overwatch 不同。
它的技能系统不是"基于 ECS 的技能系统"，而是"ECS 本身就是技能系统的运行时"。

如果不先理解 ECS 的三个基本角色，后面的技能表达方式就没有根基：

- **Entity**：一个纯 ID，没有任何数据和行为。一个英雄是 Entity，一个子弹也是 Entity。
- **Component**：挂在 Entity 上的纯数据容器。没有方法，只有字段。
- **System**：遍历特定 Component 组合的逻辑单元。没有自己的状态，只读写 Component。

在传统 OOP 里，"闪光的闪现"可能是一个 `BlinkAbility` 类，继承自 `BaseAbility`，里面有 `Execute()` 方法。

在 ECS 里，不存在 `BlinkAbility` 这个类。

---

## 技能不是 class，是 Component 组合

这是 Overwatch ECS 技能方案的核心转变。

在 OOP 技能系统里，一个技能的身份由它的类型决定——`BlinkAbility` 是一个闪现，`RocketBarrageAbility` 是一组火箭弹。

在 ECS 里，一个技能的身份由它携带的 Component 集合决定。

以闪光（Tracer）的闪现为例，它的 Entity 上可能挂着这样一组 Component：

```text
Entity: Tracer_Blink
├── InputBindingComponent     → 绑定到哪个按键
├── CooldownComponent         → 冷却时间、当前剩余冷却、充能层数
├── ChargeComponent           → 最大 3 层充能、当前可用层数
├── MovementComponent         → 位移方向、位移距离、位移速度
├── InvulnerableComponent     → 位移期间无敌标记
├── PredictedComponent        → 客户端可预测标记
├── EffectComponent           → 闪现的视觉/音效触发引用
└── OwnerComponent            → 所属英雄 Entity ID
```

这里没有一个叫 `BlinkAbility` 的对象。
"闪现"这个技能的语义，是由这些 Component 的组合隐式定义的。

改变这个组合，就改变了技能的行为：
- 去掉 `InvulnerableComponent`，闪现期间就不再无敌
- 把 `ChargeComponent` 的 max 从 3 改成 1，就变成了单次闪现
- 把 `MovementComponent` 换成 `TeleportComponent`，闪现就变成了瞬移

`技能不是被定义出来的，是被组合出来的。`

---

## System 驱动：逻辑不在技能里，在系统里

Component 只有数据，没有行为。
那"闪现"这个动作是谁执行的？

答案是 System。

ECS 里的 System 是按 Component 组合来查询和处理 Entity 的。
和技能相关的 System 可能包括：

```text
CooldownSystem
  → 每帧遍历所有有 CooldownComponent 的 Entity
  → 减少 remaining 值，处理充能恢复

MovementAbilitySystem
  → 遍历同时有 MovementComponent + InputBindingComponent 的 Entity
  → 当输入触发时，执行位移逻辑

InvulnerabilitySystem
  → 遍历有 InvulnerableComponent 的 Entity
  → 在位移期间标记该 Entity 跳过伤害计算

EffectTriggerSystem
  → 遍历有 EffectComponent + 状态变更标记的 Entity
  → 触发对应的视觉和音效表现
```

注意几个关键特征：

1. **System 不知道"闪现"**。`CooldownSystem` 不关心它处理的是闪现的冷却还是火箭弹的冷却，它只认 `CooldownComponent`。
2. **System 之间通过 Component 状态间接通信**。`MovementAbilitySystem` 执行完位移后不会调用 `InvulnerabilitySystem`，而是 `InvulnerableComponent` 的状态变更会让后者在下一次遍历时自然生效。
3. **技能的执行顺序由 System 的调度顺序决定**，不是由技能内部的方法调用链决定。

这和我们在 `03｜生命周期` 里讨论的"阶段边界"有本质的不同：

- 自研系统的阶段边界是显式定义的——Request → Validate → Cast → Execute → End
- ECS 的"阶段"是隐式的——由 System 的执行顺序和 Component 状态的传播自然形成

---

## StatScript / ModScript：ECS 之上的脚本层

纯 ECS 有一个实际工程问题：策划不能直接操作 Component 和 System。

Overwatch 的解法是在 ECS 之上加了一层脚本化配置：

- **StatScript**：定义英雄和技能的数值参数。类似于我们在 `02｜数据模型` 里讨论的 SkillDef——技能的静态数值定义。
- **ModScript**：定义技能执行时的行为修改。比如"闪现期间移速提升 200%"这类修改器逻辑。

根据 GDC 2017 演讲中公开的信息，StatScript 和 ModScript 的定位是：

1. 策划可以通过脚本调整数值和行为规则，不需要修改 C++ 的 System 代码。
2. 脚本最终会被编译为对 Component 数据的读写操作，运行时仍然走 ECS 的标准路径。
3. 脚本层不引入新的运行时状态——所有状态都存在 Component 里。

这个设计和 Dota 2 的 KV 配置有相似之处，但底层差异很大：

- Dota 2 的 KV 配置是声明式的，运行时由引擎的事件系统解释执行
- Overwatch 的 StatScript / ModScript 最终转化为 Component 数据变更，运行时由 ECS System 统一驱动

`脚本层改变了谁写数据，但没有改变数据的运行时形态——Component 仍然是唯一的状态载体。`

---

## 确定性模拟：相同输入 → 相同输出

Overwatch 的网络架构依赖一个关键前提：确定性模拟（Deterministic Simulation）。

意思是：给定相同的初始状态和相同的输入序列，任何一台机器运行出来的结果必须完全一致。

ECS 让确定性模拟变得天然可行，原因有三个：

### 1. 所有状态都在 Component 里

没有散落在各个对象方法里的局部变量，没有隐式的单例状态。
一个 Entity 在某一帧的完整状态，就是它身上所有 Component 的字段值集合。

### 2. System 的执行顺序是确定的

System 不是事件驱动的，而是按固定顺序每帧调度。
只要 System 列表和调度顺序一致，逻辑执行路径就一致。

### 3. 没有对象级的副作用

OOP 里一个方法调用可能触发回调、事件、观察者通知，这些副作用的执行顺序往往难以保证一致。
ECS 的 System 直接读写 Component，没有中间的间接调用层。

这三个特征加在一起，意味着：

`保存一帧的所有 Component 数据 = 保存了完整的游戏状态快照。`
`回滚到某一帧 = 恢复那一帧的 Component 快照 + 从该帧重新执行 System 序列。`

这对技能系统的影响是直接的：
- 闪光的闪现在客户端预测执行后，如果服务端判定结果不同，只需要回滚相关 Component 到服务端的状态，然后重新跑 System
- 不需要像 OOP 系统那样"撤销"一个方法的执行——因为根本没有方法执行，只有数据替换

---

## 网络模型：预测、权威、修正

Timothy Ford 在 GDC 2017 的 "Overwatch Gameplay Architecture and Netcode" 演讲中，详细描述了 Overwatch 的网络同步架构。

核心模型是三步：

### 第一步：客户端预测

客户端收到玩家输入后，不等待服务端确认，直接在本地执行 System 序列，更新 Component 状态。

对于闪现这类技能，玩家按下按键后立即看到角色位移，不会感知到网络延迟。

能做到这一点的前提是确定性模拟——客户端和服务端跑同一套 System，在输入相同的情况下，结果应该一致。

### 第二步：服务端权威

服务端收到客户端的输入后，在自己的 ECS 环境里执行同一套 System 序列。
服务端的结果是权威的。

如果客户端的预测和服务端的结果完全一致，什么都不需要做。

### 第三步：差异修正

如果客户端的预测和服务端的结果不一致（比如因为另一个玩家的行为在服务端产生了客户端未知的影响），服务端会把权威的 Component 状态发给客户端。

客户端收到后：
1. 将本地 Component 回滚到服务端告知的权威状态
2. 从该帧开始，用之后缓存的输入序列重新执行 System 序列
3. 追赶到当前帧

这就是网络游戏中常说的"回滚-重模拟"（Rollback and Resimulate）。

ECS 在这个过程中的优势非常明显：

```text
OOP 回滚：
  → 需要知道"哪些对象在这段时间内被修改了"
  → 需要每个对象支持序列化 / 反序列化自己的状态
  → 对象之间的引用关系在回滚后可能指向过期状态
  → 回调和事件监听器在回滚后可能产生副作用

ECS 回滚：
  → Component 是纯数据，直接做内存拷贝就完成了快照
  → Entity 只是 ID，不存在引用失效问题
  → System 无状态，重新执行不会产生累积副作用
  → 回滚粒度可以精确到单个 Component
```

`ECS 的数据布局让网络同步从"状态管理问题"简化为"数组替换问题"。`

---

## GDC 2017 核心提炼

Timothy Ford 的演讲是 Overwatch 技术架构公开信息的主要来源。以下是与技能系统直接相关的关键要点：

### Entity-Component 不新鲜，但 Overwatch 的执行严格性值得注意

Ford 在演讲中强调了几个 Overwatch 特有的约束：

1. **Component 之间不互相引用**。一个 Component 不能持有对另一个 Component 的指针。Component 之间的关联只通过 Entity ID 间接表达。
2. **System 不缓存状态**。System 在两帧之间不保留任何信息，所有持久状态必须存在 Component 里。
3. **所有游戏逻辑跑在固定频率的 Tick 上**，不依赖帧率。这是确定性模拟的硬前提。

### 网络同步不是附加层，是架构的核心约束

Ford 明确表示，Overwatch 的 ECS 架构不是先设计好再"加上网络"，而是网络同步的需求从一开始就塑造了架构选择。

选择 ECS 的一个核心原因就是：它让确定性模拟和状态快照变得自然。

### StatScript 的设计意图

Ford 提到 StatScript 是策划调整英雄数值的主要工具。
它的设计目标是让策划能快速迭代而不需要编译 C++。

但 StatScript 不是通用脚本语言——它的能力边界被刻意限制在"对 Component 数值的读写"范围内。
需要新的行为模式时，仍然要由程序员添加新的 Component 类型和对应的 System。

### 预测和修正的实际成本

Ford 坦承，虽然 ECS 让回滚变得简单，但预测和修正在实际体验上仍然需要大量的调优。

比如：
- 哪些 Component 应该参与客户端预测，哪些只等服务端权威结果
- 修正发生时，视觉表现如何平滑过渡而不是突然跳变
- 声音和粒子效果在回滚时是否需要撤销

这些问题不是 ECS 能自动解决的，需要在 System 层面做针对性处理。

---

## 与 OOP 技能系统的 tradeoff

到这里，ECS 技能方案的优势已经很明确了。
但如果只讲优势，这篇就变成了布道文。

所以这一节要认真讲 tradeoff。

### 增加一个新技能

**OOP 方式**：
创建一个新的 `AbilityClass`，继承 `BaseAbility`，实现 `Execute()` 等虚方法。
新技能的所有行为都封装在一个地方，阅读一个文件就能理解完整逻辑。

**ECS 方式**：
确定新技能需要哪些 Component 的组合。
如果现有 Component 能表达，直接组合即可——甚至不需要写代码。
如果需要新行为，添加新的 Component 类型和对应的 System。

OOP 的优势：单点可读性好——一个技能的所有行为在一个文件里。
ECS 的优势：高复用性——新技能可能只是现有 Component 的重新组合。

### 修改一个已有技能

**OOP 方式**：
找到对应的 Ability 类，修改其方法。
修改范围通常局限在一个类里，但如果有继承链，修改基类会影响所有子类。

**ECS 方式**：
如果是数值变更，修改 Component 的数据（或通过 StatScript）。
如果是行为变更，可能需要修改 System 的逻辑——但 System 的修改会影响所有使用该 Component 组合的 Entity。

OOP 的风险：继承链的脆弱性——修改基类可能引发意料之外的连锁反应。
ECS 的风险：System 的广播性——修改一个 System 会影响所有匹配的 Entity，范围可能比预期大。

### 调试一个技能的运行时行为

**OOP 方式**：
在 Ability 类的方法上打断点，跟着调用链走。
调试体验接近普通的面向对象程序。

**ECS 方式**：
技能的逻辑分散在多个 System 里，没有一个单一的调用链可以跟踪。
调试时需要同时观察多个 System 对相关 Component 的读写。
这要求专门的调试工具——比如能按 Entity 聚合所有 Component 状态变更历史的查看器。

`ECS 的调试不是更难，而是需要不同的工具和思维方式。OOP 的调试是"跟着调用链走"，ECS 的调试是"看数据在哪些 System 之间怎么流动"。`

### 团队协作

OOP 按 Ability 类分工，文件粒度清晰，但共享基类时需要协调。
ECS 把 Component 作者和 System 作者分开，多个 System 可能同时读写同一个 Component 类型，需要明确的读写规则和 Component 新增的团队级约定。

---

## 与自研系统的映射

回到我们 00-13 篇建立的概念体系，做一次直接映射：

**数据模型层（02 篇）**：自研系统的 SkillDef 在 ECS 里没有直接对应物。取而代之的是 Component 组合本身——一个技能的"定义"就是它有哪些 Component。StatScript 承担了 SkillDef 中"数值参数"的那部分职责。

**生命周期（03 篇）**：显式生命周期阶段（Request → Validate → Cast → Execute → End）在 ECS 里变成了 System 的执行顺序。一个关键差异是：自研系统的每个阶段有明确的进入和退出条件，ECS 的"阶段"是由 Component 的状态隐式标记的——比如 `CastStateComponent` 的值从 `Casting` 变到 `Executing` 就代表了阶段转换。

**验证与约束（05 篇）**：自研系统的 Validator 在 ECS 里对应 ValidationSystem，它遍历所有带有 `AbilityRequestComponent` 的 Entity，检查冷却、资源、状态约束。通过的被添加 `AbilityApprovedComponent`，不通过的被移除请求 Component。

**Buff/Modifier（08 篇）**：这可能是映射关系最直接的部分。自研系统的 Modifier 在 ECS 里就是额外的 Component——一个减速 Debuff 就是一个 `SlowModifierComponent` 挂在目标 Entity 上，`MovementSystem` 在计算移速时据此修正数值。

**网络同步（11 篇）**：11 篇讨论了权威服务器、客户端预测、延迟补偿的基本策略。Overwatch 的 ECS 架构让这三件事的实现都更加自然——服务端跑完整 System 序列、客户端跑同一套预测、差异修正只需回滚 Component 快照 + 重模拟。

`11 篇讨论的是"要做什么"，Overwatch 的 ECS 方案展示了"怎么让它做起来更简单"。`

---

## 几个常见误解

**"ECS 就是性能优化"**——Component 内存布局确实缓存友好，但 Overwatch 选 ECS 的核心原因不是性能，而是确定性模拟和网络同步的架构适配。6v6 的 FPS，Entity 数量远不到需要靠 ECS 解决性能瓶颈的量级。

**"ECS 适合所有类型的游戏"**——在不需要确定性模拟的单机 RPG 或回合制策略里，ECS 的组合式表达带来的复杂度可能得不偿失。架构选择取决于核心约束，不取决于技术风潮。

**"Overwatch 的所有代码都是 ECS"**——Ford 在演讲中明确提到，UI、菜单、匹配逻辑等不需要确定性模拟的部分仍然使用传统 OOP。ECS 覆盖的是"需要参与网络同步的游戏逻辑"这个范围。

---

## 这一篇真正想留下来的结论

Overwatch 的 ECS 技能方案，最有价值的不是"用 Component 代替 Class"这个表面形式，而是它回答了一个根本问题：

**当网络同步是硬约束时，技能系统的架构应该怎么选？**

Overwatch 的回答是：
1. 把所有可变状态收进 Component，让快照和回滚变成数组拷贝
2. 把所有逻辑收进无状态的 System，让重模拟变成重新遍历
3. 用确定性模拟保证客户端预测的可靠性
4. 用 StatScript 让策划在 Component 数据层面高效迭代

这套方案的代价也很明确：
- 单个技能的可读性不如 OOP——行为分散在多个 System 里
- 调试需要专门的工具支持
- Component 设计需要团队级的约定和纪律
- System 的修改影响范围比 OOP 方法的修改范围更广

如果把这个判断压到最短：

`ECS 技能系统用"组合"替代"继承"，用"数据流"替代"调用链"，换来了确定性模拟和网络回滚的天然适配。这个 tradeoff 是否值得，取决于你的项目是否把网络同步当作核心架构约束。`

17 篇的 Dota 2 用数据驱动解决了"策划效率"问题。
这一篇的 Overwatch 用 ECS 解决了"网络确定性"问题。
两个方案出发点不同，边界不同，tradeoff 不同。

理解它们各自在解决什么问题，比判断谁更好更有用。
