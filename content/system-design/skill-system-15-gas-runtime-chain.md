---
date: "2026-04-13"
title: "技能系统深度 15｜GAS 运行时链路深挖：AbilitySystemComponent → GameplayEffect → AttributeSet 的执行主链"
description: "大多数 GAS 教程停在"怎么用"，没有讲清 ASC 内部的 Ability 激活链、GE 的 Modifier 求值顺序、AttributeSet 的钩子时机、以及 Tag 查询的运行时代价。这篇从 ActivateAbility 开始，一路追到属性变更落地。"
slug: "skill-system-15-gas-runtime-chain"
weight: 8015
tags:
  - Gameplay
  - Skill System
  - Unreal
  - GAS
  - Runtime
series: "技能系统深度"
series_order: 15
---

> GAS 的真正价值不在 API 表面，而在它把 Ability 激活、Effect 聚合、Attribute 变更和 Tag 查询这四件事的运行时边界拆得足够清楚。

14 篇做完了自研技能系统和 GAS 的思想映射，但那一篇刻意没有展开 GAS 内部的运行时流转。

原因很简单：如果不先让读者看清自研系统的边界意识，直接进 GAS 源码层，文章很容易退化成"背 API 名字"。

但现在主线已经立住了。
所以这一篇要做的事，就是从运行时视角把 GAS 的执行主链拆开来看。

这不是教程。
这是一篇对 GAS 运行时结构的源码级拆解，和 HybridCLR 系列里追调用链的写法是同一类。

---

## 这篇要回答什么

这篇主要回答 6 个问题：

1. `UAbilitySystemComponent` 在运行时到底管什么，为什么不只是"技能容器"。
2. 一次 Ability 从请求激活到结束清理，经过了哪些步骤。
3. `GameplayEffect` 的 Modifier 聚合链到底按什么顺序求值。
4. `UAttributeSet` 的 Pre/Post 钩子到底该做什么、不该做什么。
5. Tag 的 Grant/Block/Required 查询在运行时怎么执行。
6. `GameplayCue` 的可靠和不可靠通知到底有什么区别。

---

## 先把 14 篇的概念映射收回来

14 篇已经做了一件事：把自研系列 00-13 的概念和 GAS 的核心对象做了一次结构映射。

那一篇得出的关键结论是：

`GAS 和自研技能系统在回答同一类执行链问题，只是收束方式和工程重量不同。`

但 14 篇有一个刻意的留白：
它只讲了"GAS 的概念对应什么"，没有追"GAS 运行时到底怎么跑"。

这篇就是来补这条链的。

从 `TryActivateAbility` 开始，一路追到属性变更落地、Tag 状态刷新、GameplayCue 通知发出。

---

## ASC 的角色：不是技能列表，是运行时调度器

很多人第一次接触 GAS，会把 `UAbilitySystemComponent` 理解成：

- 一个挂在 Actor 上的技能容器
- 管着"这个角色有哪些技能"

这是对的，但远远不够。

ASC 在运行时至少管三件事。

### 第一件：Ability 的生命周期

ASC 通过 `GiveAbility` 把一个 `FGameplayAbilitySpec` 注册到自己的 `ActivatableAbilities` 列表里。
这一步只代表"这个角色拥有了这个能力"，不代表能力在运行。

真正的激活是 `TryActivateAbility`。
取消是 `CancelAbility` 或 `CancelAbilityHandle`。
结束是 Ability 自身调用 `EndAbility`。

每一个 Ability 实例都有自己的激活状态，ASC 负责维护这些状态的生命周期。

### 第二件：Effect 的聚合

ASC 内部有一个 `FActiveGameplayEffectsContainer`，它管着当前所有 Active GameplayEffect。

每当一个 `GameplayEffect` 被 Apply 到这个 ASC 上时，它会：

- 把 GE 的 Modifier 注册进属性聚合链
- 把 GE 的 GrantedTags 加进当前 Tag 集合
- 如果是 Duration 或 Infinite 类型，把它保留在容器里直到过期或被移除

也就是说，ASC 不只是持有 Ability，还持有所有正在生效的 Effect 的运行时状态。

### 第三件：Tag 的状态

ASC 维护一个运行时 Tag 容器。
这个容器由当前所有 Active GE 的 `GrantedTags`、以及 Ability 自身的 `ActivationOwnedTags` 等来源合成。

当外部系统需要查"这个角色当前是不是处于某个状态"时，查的就是这个容器。

### 挂载选择：为什么 Lyra 把 ASC 放在 PlayerState 上

在 Unreal 的典型架构里，ASC 可以挂在 Pawn 上，也可以挂在 PlayerState 上。

Lyra 项目选择了后者。

原因是：如果 ASC 挂在 Pawn 上，角色死亡或 Pawn 被销毁重生时，ASC 和上面所有 Active Effect 都会跟着丢失。
把 ASC 挂在 PlayerState 上，能让角色在 Pawn 切换、死亡重生等场景下保持 Ability 和 Effect 状态的连续性。

但代价是挂载关系更复杂，`GetAbilitySystemComponent()` 的查找路径也需要额外适配。

---

## 一次 Ability 激活的完整调用链

这一节是整篇最长的一段，因为 Ability 激活不是一步到位的事。

### ActivateAbility 之前：CanActivateAbility 校验

当外部调用 `TryActivateAbility` 时，ASC 不会直接激活 Ability。
它会先走一轮校验，对应 `UGameplayAbility::CanActivateAbility`。

这个函数里至少检查三件事。

**Cost Check。**
对应 `CheckCost`。
如果 Ability 定义了 `CostGameplayEffect`，系统会检查当前属性是否够支付这个 GE 的消耗。
注意这里只是检查，不是真正扣除。

**Cooldown Check。**
对应 `CheckCooldown`。
系统检查 ASC 上是否存在一个和 `CooldownGameplayEffect` 对应的 Active GE。
如果存在，说明当前还在冷却中，激活被拒绝。

**Tag Check。**
对应 `DoesAbilitySatisfyTagRequirements`。
Ability 定义了 `ActivationRequiredTags` 和 `ActivationBlockedTags`。
ASC 会用自己当前的 Tag 容器去做 container match：

- 所有 RequiredTags 必须在 ASC 上存在
- 所有 BlockedTags 必须不在 ASC 上存在

如果有任何一个 BlockedTag 命中，激活立刻被拒绝。

这三步校验，对应自研系列 05 篇的校验层。
区别在于 GAS 把 Cost 和 Cooldown 都表达成了 `GameplayEffect`，而不是独立数值字段。

### ActivateAbility 到 CommitAbility

校验通过后，`ActivateAbility` 被调用。
但这一步并不会立刻扣资源或启动冷却。

真正执行消耗和冷却的，是 `CommitAbility`。

这就是 GAS 里最重要的一个两步设计：

`ActivateAbility 是"我开始执行了"，CommitAbility 是"我确认要花掉资源和开始冷却了"。`

为什么要分成两步？

因为 GAS 内建了客户端预测。
在预测场景下，客户端可以先 ActivateAbility 开始播动画和执行前置逻辑，但资源扣除和冷却启动需要等到 Commit 才真正生效。
如果服务端拒绝了这次激活，系统需要一个干净的回滚点。

`CommitAbility` 内部拆成两个子步骤：

- `CommitAbilityCost`：Apply CostGameplayEffect，真正扣资源
- `CommitAbilityCooldown`：Apply CooldownGameplayEffect，启动冷却

在 `UGameplayAbility::CommitAbility` 的实现里，这两个步骤是顺序调用的。

### Ability 执行中：AbilityTask

Ability 从 Activate 到 End 之间，通常不是一帧就完成的。

GAS 用 `UAbilityTask` 作为异步执行单元，让 Ability 可以在执行中间等待：

- `UAbilityTask_WaitDelay`：等一段时间
- `UAbilityTask_PlayMontageAndWait`：播动画并等待完成或中断
- `UAbilityTask_WaitTargetData`：等待目标选择
- `UAbilityTask_WaitGameplayEvent`：等一个 GameplayEvent

AbilityTask 是 UObject，不是协程。
它可以被 Ability 取消，可以被 ASC 清理，可以参与网络预测。

但 AbilityTask 的细节留给 16 篇。
这里只需要记住：AbilityTask 是 Ability 在激活态和结束态之间的"中间执行步骤"。

### EndAbility

Ability 完成后，调用 `EndAbility`。

这一步会做几件事：

- 移除 Ability 在激活期间 Grant 给 ASC 的 Tag（`ActivationOwnedTags`）
- 取消所有正在运行的 AbilityTask
- 标记这个 Ability 实例为非激活状态

`CancelAbility` 和 `EndAbility` 的区别：
Cancel 会走 `OnGameplayAbilityCancelled` 回调，通常用于外部打断。
End 是正常结束流程。
但两者最终都会完成 Tag 清理和 Task 取消。

---

## GameplayEffect 的 Modifier 聚合与求值链

Ability 激活链解决的是"什么时候做"，GE 解决的是"对属性做什么"。

### 三类 GE：Instant、Duration、Infinite

`UGameplayEffect` 定义了 `DurationPolicy`，它决定这个 GE 是哪种存续模式。

**Instant。**
立即修改属性的 BaseValue，然后 GE 实例不保留在 ASC 上。
典型用途：直接伤害、直接治疗。
在 `FActiveGameplayEffectsContainer` 里不会留下持久条目。

**Duration。**
有明确的持续时间。
修改的是属性的 CurrentValue，而不是 BaseValue。
持续期间内 GE 保留在 Active 列表里，到期后自动移除。
典型用途：限时 Buff，如 5 秒增加 20% 攻速。

**Infinite。**
和 Duration 一样修改 CurrentValue，但没有自动过期时间。
必须手动移除。
典型用途：装备赋予的永久加成、被动技能的持续效果。

这个分类和自研系列 07 篇里 Effect 的 Instant/Apply-State 分类是同构的。
区别在于 GAS 没有把 Buff 和 Effect 分成两个独立概念，而是用 Duration/Infinite GE 统一承载持续效果。

### Modifier 聚合顺序

一个属性可能同时被多个 GE 的 Modifier 影响。

在 `FActiveGameplayEffectsContainer` 聚合时，同一属性上的 Modifier 按 `EGameplayModOp` 排序：

1. `Additive`：先加
2. `Multiplicitive`：再乘
3. `Division`：再除
4. `Override`：最后覆盖

这个顺序是固定的，不按 GE 的 Apply 时间排列。

也就是说，无论你先 Apply 一个 +50% 的乘法 Modifier，还是后 Apply 一个 +100 的加法 Modifier，最终求值时加法都先于乘法。

这对应自研系列 08 篇里 Modifier 聚合顺序的讨论：

`顺序必须是系统规则，不能每个 Buff 自己决定。`

GAS 在这件事上用了固定枚举顺序来保证确定性，这和我们建议的 `FlatBonus -> AdditivePercent -> Multiplicative -> FinalOffset` 是同一种思路。

### Execution Calculation

对于不能用简单 Modifier 表达的复杂公式，GAS 提供了 `UGameplayEffectExecutionCalculation`。

它是一个独立的计算类，可以：

- 读取 Source（施法者）的多个属性
- 读取 Target（目标）的多个属性
- 执行自定义公式
- 输出一组 Modifier 结果

`ExecutionCalculation` 在 `FGameplayEffectSpec` 的 `Execute` 路径中被调用，发生在标准 Modifier 聚合之外。

这对应自研系列 07 篇的 Effect System 执行器。
区别在于 GAS 把简单修改（Modifier）和复杂计算（ExecutionCalculation）拆成了两条路径，而不是统一走同一个 Executor。

---

## AttributeSet 的钩子链

`UAttributeSet` 是 GAS 里属性的宿主。
它通常挂在 ASC 的 Owner Actor 上，声明一组 `FGameplayAttributeData` 字段。

当 GE 修改属性时，AttributeSet 会收到一系列钩子调用。

### PreGameplayEffectExecute

在 GE 的 Modifier 真正修改属性之前调用。

签名是 `PreGameplayEffectExecute(FGameplayEffectModCallbackData& Data)`。

这个钩子通常用于：

- Clamp 输入值（比如把负数伤害修正成 0）
- 实现护盾吸收（在 HP 被减少之前，先让护盾承担一部分）
- 记录伤害来源（把 Source 信息缓存下来，供后续逻辑使用）

但这个钩子不应该做游戏逻辑判断。
死亡判定不该放在这里，因为此时属性值还没有真正变更。

### PostGameplayEffectExecute

在属性修改之后调用。

这是真正适合做游戏响应逻辑的地方：

- 死亡判定（检查 HP 是否 <= 0，触发死亡流程）
- 属性 Clamp（确保 HP 不超过 MaxHP）
- 触发后续 Effect（比如 HP 过低时自动触发某个 Ability）

这对应自研系列 08 篇的 Modifier 聚合后处理。
GAS 用 Pre/Post 钩子把"修改前拦截"和"修改后响应"拆成了两个显式时机。

### PreAttributeChange 和 PostAttributeChange

除了 GE 执行路径上的 Pre/Post，AttributeSet 还有一组更通用的钩子。

`PreAttributeChange(const FGameplayAttribute& Attribute, float& NewValue)` 在 `CurrentValue` 被改变之前调用。
它的主要用途是做值域 Clamp，比如确保 MoveSpeed 不低于某个最小值。

注意一个容易被忽略的细节：`BaseValue` 和 `CurrentValue` 是两个不同的值。

- `BaseValue` 是属性的基础值，只有 Instant GE 会直接修改它
- `CurrentValue` 是 BaseValue 加上所有 Duration/Infinite GE 的 Modifier 聚合后的结果

PreAttributeChange 作用于 CurrentValue 的变更。
如果你在 Pre 里 Clamp 了 NewValue，改的是 CurrentValue，不是 BaseValue。

这个区分非常重要。
很多 GAS 初学者把 BaseValue 和 CurrentValue 混成一回事，导致 Buff 移除后属性值不能正确恢复。

---

## Tag 查询与 GameplayCue 的运行时代价

### Tag 系统的运行时实现

`FGameplayTag` 是层级化标签，比如 `State.Dead`、`Ability.Skill.Fireball`。

ASC 在运行时维护一个 `FGameplayTagCountContainer`，它记录每个 Tag 被多少个来源 Grant。
当所有来源都移除后，Tag 才真正从容器里消失。

Tag 查询有三种常见模式：

**GrantedTags。**
当前 ASC 上所有来源合成的 Tag 集合。

**BlockedAbilitiesWithTag。**
如果 ASC 的 GrantedTags 包含某些 Tag，对应的 Ability 就不能激活。
这是 `ActivationBlockedTags` 在运行时的实际执行路径。

**RequiredTags。**
Ability 定义的 `ActivationRequiredTags`，ASC 上必须存在这些 Tag 才能激活。

运行时查询并不是字符串比较。
`FGameplayTagContainer` 内部把 Tag 映射成数值 ID，查询是 container match 操作。
但当 Tag 容器很大、查询频率很高时，仍然会有性能开销。

尤其是在 Ability 激活校验路径上，每次 `TryActivateAbility` 都会执行一次 Tag match。
如果一个 Actor 上同时有大量 Active GE 各自 Grant 了 Tag，这个容器的体积会直接影响校验效率。

### GameplayCue

`GameplayCue` 是 GAS 把"表现触发"从"逻辑执行"里拆出来的方式。

它有两种通知模式。

**Replicated（可靠通知）。**
通过 `FActiveGameplayCue` 在 ASC 上复制。
所有相关客户端都能收到这个 Cue 的添加和移除事件。
典型用途：伤害数字、持续状态特效。

**Local Only（不可靠通知）。**
只在执行端本地触发，不通过网络复制。
典型用途：击中火花、脚步声、非关键视觉反馈。

选择标准很直接：

- 如果这个表现丢失会影响玩家对战斗状态的判断，用 Replicated
- 如果这个表现只是锦上添花，丢了也不影响理解，用 Local Only

这对应自研系列 09 篇的表现解耦。
GAS 的 GameplayCue 本质上就是在把"表现层消费逻辑事件"这件事系统化。

---

## 这条链和自研系统 00-08 的映射锚点

如果把这一篇追的运行时链路，和前面自研系列对应起来，可以得到一张对照表。

| GAS 运行时概念 | 自研系列对应 | 对应要点 |
|------|------|------|
| ASC 校验链（Cost/Cooldown/Tag） | 05 篇校验与约束 | 都在回答"为什么现在能放/不能放" |
| Ability 生命周期（Activate/Commit/End） | 03 篇生命周期 | 都承认技能不是瞬间函数 |
| GE Modifier 聚合（Add/Mul/Div/Override） | 07+08 篇 Effect+Buff | 都在处理多个效果同时修改属性时的确定性 |
| AttributeSet Pre/Post 钩子 | 08 篇 Modifier 后处理 | 都在属性变更前后提供系统级响应时机 |
| Tag 查询（Required/Blocked/Granted） | 01 篇标签边界 | 都在用标签做系统间的语义桥接 |
| GameplayCue | 09 篇表现解耦 | 都在强调表现不该反向驱动逻辑 |

这张表的意义不在于"GAS 和自研系统长得像"。
它的意义在于：如果你已经理解了自研系列每一层的职责，那 GAS 的运行时链路不是全新知识，而是同一类问题的一个更重型、更系统化的工程落地。

---

## 把整条链压成 7 步

如果你现在回头看这一篇，GAS 的一次完整 Ability 执行主链可以压成下面 7 步：

1. 外部调用 `TryActivateAbility`，ASC 查找对应的 `FGameplayAbilitySpec`。
2. `CanActivateAbility` 校验 Cost、Cooldown 和 Tag，任何一步失败就拒绝激活。
3. `ActivateAbility` 被调用，Ability 进入激活态，开始执行逻辑。
4. `CommitAbility` 真正 Apply Cost GE 和 Cooldown GE，扣除资源并启动冷却。
5. Ability 通过 AbilityTask 执行异步步骤，Apply GameplayEffect 到目标 ASC。
6. 目标 ASC 的 `FActiveGameplayEffectsContainer` 聚合 Modifier，触发 AttributeSet 的 Pre/Post 钩子，属性值落地。
7. `EndAbility` 清理 GrantedTags、取消 AbilityTask、标记 Ability 为非激活。

这 7 步里最值得注意的边界是：

- 第 1-2 步是请求与校验
- 第 3-4 步是激活与提交（两步设计是为了预测系统的回滚点）
- 第 5-6 步是效果执行与属性落地
- 第 7 步是清理

---

## 这条链里最容易看错的 4 个地方

### 误解一：ASC 只是一个技能列表

不对。
ASC 同时管着 Ability 生命周期、Active GE 容器和运行时 Tag 状态。
把它理解成"技能容器"会漏掉它对 Effect 和 Tag 的管理职责。

### 误解二：ActivateAbility 就是全部

不对。
真正的资源消耗和冷却启动发生在 CommitAbility。
Activate 只是"开始执行"，Commit 才是"确认花费"。
这个两步设计是 GAS 网络预测的基础，不是冗余。

### 误解三：Modifier 按 Apply 时间排序

不对。
Modifier 聚合按操作类型（Add/Mul/Div/Override）排序，不按 GE 的 Apply 先后。
这是确定性的来源。

### 误解四：PreGameplayEffectExecute 适合做死亡判定

不推荐。
Pre 钩子在属性值变更之前调用，此时 HP 还没有真正减少。
死亡判定应该放在 PostGameplayEffectExecute 里，在属性值已经落地之后再判断。

---

## 这一篇真正想留下来的结论

GAS 的运行时主链不复杂，但它每一层的边界非常清楚：

- ASC 是调度器，不只是容器
- Ability 有显式的 Activate / Commit / End 过程
- GE 的 Modifier 聚合有固定求值顺序
- AttributeSet 的钩子有明确的前后时机
- Tag 是运行时的状态查询语言
- GameplayCue 把表现从逻辑里隔离出去

如果把整篇压成最短一句话：

`GAS 运行时真正在跑的，不是一个 UseSkill() 函数，而是一条从 TryActivateAbility 出发、经过 Cost/Cooldown/Tag 校验、CommitAbility 扣资源、GameplayEffect 修改属性、AttributeSet 钩子响应、一直到 EndAbility 清理状态的完整执行链。这条链的每一层边界，和前面自研系列按层拆出来的执行链是同构的。`
