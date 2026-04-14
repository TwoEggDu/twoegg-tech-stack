---
date: "2026-03-27"
title: "技能系统深度系列索引｜先读哪篇，遇到什么问题该回看哪篇"
description: "给技能系统深度系列补一个总入口：推荐阅读顺序、按问题回看路径、公共前置文章，覆盖主线 00-14、业内框架深挖 15-21、网络同步 22-26、多角色协作 27-29、深度展开 30-32 和案例坑型录 33-36，共 38 篇。"
slug: "skill-system-series-index"
weight: 7999
featured: false
tags:
  - Gameplay
  - Skill System
  - Combat
  - Index
  - Architecture
series: "技能系统深度"
series_id: "skill-system"
series_role: "index"
series_order: 0
series_nav_order: 20
series_title: "技能系统深度"
series_audience:
  - "Gameplay / 战斗开发"
  - "客户端主程"
  - "服务端 / 战斗程序"
  - "技术策划"
  - "技术美术"
series_level: "进阶"
series_best_for: "当你想把技能系统从数据、执行、表现、多人同步、业内框架对标、团队协作到问题诊断按层拆清楚"
series_summary: "把技能系统从数据、执行、表现到多人同步拆成一条可落地的执行链，再用 5 个业内框架做横向对标，加上 C/S 联合、多角色协作、深度展开和案例坑型录"
series_intro: "这组文章覆盖的不是单个技能效果，而是从输入、施法请求、校验、命中、效果、Buff 到动画和多人同步的完整链路，再扩展到 GAS/Dota2/守望先锋/LoL/PoE 五大框架的运行时拆解、帧同步与状态同步的深度对比、策划-美术-程序的协作工作流、以及 7 类高频坑型录和诊断指南。"
series_reading_hint: "第一次阅读建议先按主线 00-14 走完，再根据兴趣进入业内框架（15-21）、网络同步（22-26）、协作（27-29）或案例诊断（33-36）。"
---

> 这组文章如果一篇篇单看，其实都能成立；但技能系统真正难的地方，不在某个单点知识，而在于你能不能先知道自己现在卡在执行链的哪一层——主线 15 篇把链拆清楚，深挖 22 篇把每一层的业内做法、网络真相、协作流程和坑型录补完。

这是技能系统深度系列的索引页。  
它不讲新的底层机制，只做一件事：

`给这 38 篇文章补一个稳定入口，让读者知道先读哪篇、遇到什么问题该回看哪篇。`

## 这篇要回答什么

这篇主要回答 4 个问题：

1. 38 篇文章覆盖了哪些主题。
2. 如果按系统阅读走，最稳的顺序是什么。
3. 如果不是系统读，而是带着问题来查，该先跳哪几篇。
4. 主线（00-14）和深挖（15-36）的关系是什么。

## 先给一句总判断

如果把整个系列压成一句话，我会这样描述：

`技能系统不是"放一个技能"的局部功能点，而是一条从输入意图、施法请求、校验约束、目标命中、效果执行、Buff 规则，一直延伸到表现对齐、AI 调用边界、多人同步真相、框架选型、团队协作和问题诊断的完整工程体系。`

所以这组文章故意不是按"今天做一个火球术"去写，也不是按某个引擎 API 平铺，而是按问题层拆：

- 技能系统到底在解决什么
- 概念边界怎样切
- 定义层和运行时层怎么分
- 生命周期怎么统一
- 输入、校验、目标、命中怎样接成执行主链
- Effect 和 Buff 为什么应该独立成层
- 动画与表现怎样对齐但不绑死
- AI 和技能系统到底谁负责什么
- 业内五大框架的运行时到底长什么样
- 帧同步和状态同步下技能系统的真相归属有什么不同
- 策划、美术、程序三个角色分别该交付什么
- 遇到技能相关的 bug 应该先查哪一层

## 先补几个公共前置

这组文章虽然已经尽量把技能系统本体讲完整，但它不是"从零补完所有程序基础"的系列。  
如果你对下面这些问题还没有稳定直觉，我建议先补几篇公共前置：

- [游戏编程设计模式 02｜Command 模式：操作对象化]({{< relref "system-design/pattern-02-command.md" >}})
- [游戏编程设计模式 06｜State 模式 vs FSM vs 行为树：三种状态管理方式的适用边界]({{< relref "system-design/pattern-06-state-fsm-behavior-tree.md" >}})
- [软件工程基础 04｜开闭原则（OCP）：对扩展开放，对修改封闭]({{< relref "system-design/solid-sw-04-ocp.md" >}})
- [数据结构与算法 08｜拓扑排序：技能依赖树、资源加载顺序、任务调度]({{< relref "system-design/ds-08-topological-sort.md" >}})
- [从玩家输入到屏幕画面：游戏内容和运行时是怎么汇合的]({{< relref "rendering/player-input-and-game-content-to-screen.md" >}})

这些文章不是技能系统系列编号本身，但会把：

- 输入如何进入运行时
- 状态机和行为树怎么分
- 为什么要把变化收回接口
- 为什么技能树、任务链和依赖图本质上是同类问题

这几层底座先立住。

## 推荐阅读顺序

### 主线：00-14（先走完这条）

如果你第一次系统读，我建议按下面这条顺序走。主线 15 篇把技能系统从问题空间到工程落地到框架对照走完一个闭环。

0. [技能系统深度系列索引｜先读哪篇，遇到什么问题该回看哪篇]({{< relref "system-design/skill-system-series-index.md" >}}) — 先建地图，不然很容易把后面的话题混成同一个问题。
1. [技能系统深度 00｜总论：技能系统到底在解决什么]({{< relref "system-design/skill-system-00-overview.md" >}}) — 先把问题空间立住，避免后面自动滑回"写一个技能脚本"。
2. [技能系统深度 01｜边界：技能、效果、Buff、属性、标签、资源、冷却分别是什么]({{< relref "system-design/skill-system-01-boundaries.md" >}}) — 整个系列最重要的基础篇之一，先把概念边界拆开。
3. [技能系统深度 02｜数据模型：SkillDef、SkillInstance、CastContext、TargetSpec、EffectSpec 怎么设计]({{< relref "system-design/skill-system-02-data-model.md" >}}) — 把定义层和运行时层的骨架立起来。
4. [技能系统深度 03｜生命周期：Instant / Cast / Channel / Charge / Toggle / Passive / Combo 怎么统一]({{< relref "system-design/skill-system-03-lifecycle.md" >}}) — 先把时序骨架稳定下来，后面的输入、命中、表现才有地方落。
5. [技能系统深度 04｜输入与施法请求：按键、输入缓冲、技能队列、取消窗口怎么接]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}}) — 开始把玩家/AI 的外部意图接进技能系统主链。
6. [技能系统深度 05｜校验与约束：冷却、消耗、距离、状态、标签阻塞应该放在哪]({{< relref "system-design/skill-system-05-validation-and-constraints.md" >}}) — 把"为什么现在不能放"从零散 if 收回统一约束层。
7. [技能系统深度 06｜目标选择与命中：单体、范围、投射物、锁定、碰撞、命中时机怎么拆]({{< relref "system-design/skill-system-06-targeting-and-hit-resolution.md" >}}) — 把"想打谁"和"最后真的打到谁"分开。
8. [技能系统深度 07｜效果系统：伤害、治疗、位移、驱散、召唤为什么应该统一落到 Effect System]({{< relref "system-design/skill-system-07-effect-system.md" >}}) — 真正的执行引擎在这里。
9. [技能系统深度 08｜Buff / Modifier：叠层、刷新、覆盖、快照、实时重算应该怎么建模]({{< relref "system-design/skill-system-08-buff-modifier.md" >}}) — 长期维护最容易失控的一层，一定要单独抽出来。
10. [技能系统深度 09｜动画与表现解耦：前摇点、生效点、受击点、VFX/SFX、Timeline/Event 怎么对齐]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}}) — 到这里再谈"手感"和"表现"，不会把表现误当成技能本体。
11. [技能系统深度 10｜AI 与技能系统的边界：AI 负责选技能，不该接管技能执行链]({{< relref "system-design/skill-system-10-ai-boundary.md" >}}) — 回答"AI 算不算技能系统"这个最容易混的问题。
12. [技能系统深度 11｜多人同步：服务器权威、预测、回滚、命中确认、冷却同步应该怎么拆]({{< relref "system-design/skill-system-11-multiplayer-sync.md" >}}) — 把技能执行链放进多端同步场景，重新拆清请求、确认、命中和冷却的真相归属。
13. [技能系统深度 12｜编辑器与配置工具：技能编辑器、依赖图、校验器、调试视图怎样服务执行链]({{< relref "system-design/skill-system-12-editor-and-tooling.md" >}}) — 把工程期真正需要的工具层接回执行链。
14. [技能系统深度 13｜测试与回归：公式回归、边界条件、战斗日志、技能时间线怎样落地]({{< relref "system-design/skill-system-13-testing-and-regression.md" >}}) — 把工具层继续往下接成回归体系。
15. [技能系统深度 14｜Unity 自研技能系统 vs Unreal GAS：思想映射，而不是 API 对照]({{< relref "system-design/skill-system-14-unity-self-built-vs-unreal-gas.md" >}}) — 把整条自研主线和 GAS 做一次思想映射收束。

### 深挖：15-36（按兴趣选读）

主线走完之后，下面 22 篇按兴趣选读。它们不是"新的主线"，而是在主线每一层上往深处挖：业内框架怎么做、网络同步的具体技术、团队协作的具体流程、以及真实项目里的坑。

## 按主题分组去读

如果你不想严格按顺序读，而是想按主题看，这 38 篇目前可以分成下面十二块。

### 一、问题空间与概念边界（00-01）

- [技能系统深度 00｜总论：技能系统到底在解决什么]({{< relref "system-design/skill-system-00-overview.md" >}})
- [技能系统深度 01｜边界：技能、效果、Buff、属性、标签、资源、冷却分别是什么]({{< relref "system-design/skill-system-01-boundaries.md" >}})

这一组回答的是：

`技能系统到底是什么、为什么它不是一个按钮函数、以及 Skill / Effect / Buff / Attribute / Tag 到底谁负责什么。`

### 二、骨架与运行时模型（02-03）

- [技能系统深度 02｜数据模型：SkillDef、SkillInstance、CastContext、TargetSpec、EffectSpec 怎么设计]({{< relref "system-design/skill-system-02-data-model.md" >}})
- [技能系统深度 03｜生命周期：Instant / Cast / Channel / Charge / Toggle / Passive / Combo 怎么统一]({{< relref "system-design/skill-system-03-lifecycle.md" >}})

这一组回答的是：

`技能系统的定义层、运行时层、上下文层和时序层应该怎样建骨架。`

### 三、执行主链（04-06）

- [技能系统深度 04｜输入与施法请求：按键、输入缓冲、技能队列、取消窗口怎么接]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}})
- [技能系统深度 05｜校验与约束：冷却、消耗、距离、状态、标签阻塞应该放在哪]({{< relref "system-design/skill-system-05-validation-and-constraints.md" >}})
- [技能系统深度 06｜目标选择与命中：单体、范围、投射物、锁定、碰撞、命中时机怎么拆]({{< relref "system-design/skill-system-06-targeting-and-hit-resolution.md" >}})

这一组回答的是：

`玩家或 AI 的意图怎样进入系统、怎样被校验、怎样真正送达到目标。`

### 四、世界变化与持续规则（07-08）

- [技能系统深度 07｜效果系统：伤害、治疗、位移、驱散、召唤为什么应该统一落到 Effect System]({{< relref "system-design/skill-system-07-effect-system.md" >}})
- [技能系统深度 08｜Buff / Modifier：叠层、刷新、覆盖、快照、实时重算应该怎么建模]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})

这一组回答的是：

`技能怎样真正改变世界，以及这些变化如何稳定地长期存在。`

### 五、跨系统边界（09-11）

- [技能系统深度 09｜动画与表现解耦：前摇点、生效点、受击点、VFX/SFX、Timeline/Event 怎么对齐]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}})
- [技能系统深度 10｜AI 与技能系统的边界：AI 负责选技能，不该接管技能执行链]({{< relref "system-design/skill-system-10-ai-boundary.md" >}})
- [技能系统深度 11｜多人同步：服务器权威、预测、回滚、命中确认、冷却同步应该怎么拆]({{< relref "system-design/skill-system-11-multiplayer-sync.md" >}})

这一组回答的是：

`技能系统怎样和表现系统、AI 系统、多人同步语境对齐，但又不被它们吞掉。`

### 六、工程化落地（12-13）

- [技能系统深度 12｜编辑器与配置工具：技能编辑器、依赖图、校验器、调试视图怎样服务执行链]({{< relref "system-design/skill-system-12-editor-and-tooling.md" >}})
- [技能系统深度 13｜测试与回归：公式回归、边界条件、战斗日志、技能时间线怎样落地]({{< relref "system-design/skill-system-13-testing-and-regression.md" >}})

这一组回答的是：

`技能系统进入团队协作和长期维护阶段后，定义层、依赖关系、运行时过程以及回归证据应该怎样被稳定地看见和验证。`

### 七、框架映射与收束（14）

- [技能系统深度 14｜Unity 自研技能系统 vs Unreal GAS：思想映射，而不是 API 对照]({{< relref "system-design/skill-system-14-unity-self-built-vs-unreal-gas.md" >}})

这一组回答的是：

`当自研技能系统主线已经写完整之后，怎样把它和 Unreal GAS 做一次思想映射，看清哪些概念同构、哪些边界不同，以及哪些经验值得借。`

### 八、业内框架深挖（15-21）

- [技能系统深度 15｜GAS 运行时链路深挖：AbilitySystemComponent → GameplayEffect → AttributeSet 的执行主链]({{< relref "system-design/skill-system-15-gas-runtime-chain.md" >}}) — 从 ActivateAbility 追到属性变更落地，拆清 GE Modifier 求值顺序和 Tag 查询代价。
- [技能系统深度 16｜GAS 网络预测层深挖：PredictionKey、AbilityTask 与多端状态同步]({{< relref "system-design/skill-system-16-gas-prediction-layer.md" >}}) — PredictionKey 分配/确认/过期机制、AbilityTask 异步模型、GE 复制策略。
- [技能系统深度 17｜Dota 2 数据驱动技能系统：ability_datadriven、Modifier 与事件驱动 Action]({{< relref "system-design/skill-system-17-dota2-data-driven.md" >}}) — 120+ 英雄不碰 C++ 的数据驱动边界，以及哪些技能最终还是要回到代码。
- [技能系统深度 18｜守望先锋 ECS 技能方案：组件是数据、系统是逻辑、技能是组合]({{< relref "system-design/skill-system-18-overwatch-ecs-ability.md" >}}) — ECS 下技能不是对象而是 Component 组合，与 OOP 技能系统的核心差异。
- [技能系统深度 19｜英雄联盟服务端技能架构：Live Service 下的技能系统演化]({{< relref "system-design/skill-system-19-lol-server-architecture.md" >}}) — 竞技公平性、双周 patch、160+ 英雄的工程约束。
- [技能系统深度 20｜Path of Exile 技能图与 Gem 组合系统：大规模技能组合的架构挑战]({{< relref "system-design/skill-system-20-poe-gem-combination.md" >}}) — Skill Gem + Support Gem + Passive Tree 的组合爆炸和求值性能。
- [技能系统深度 21｜业内技能框架横向矩阵：从 8 个维度比较 GAS / Dota 2 / 守望先锋 / LoL / PoE / 自研]({{< relref "system-design/skill-system-21-industry-comparison-matrix.md" >}}) — 8 个架构维度横向对比 6 种方案，附选型决策树。

这一组回答的是：

`行业里最有代表性的 5 个技能框架的运行时到底长什么样，以及该怎样选型。`

### 九、客户端-服务端联合（22-26）

- [技能系统深度 22｜技能同步的 5 个"触发"含义：从本地输入到全端确认的完整网络链]({{< relref "system-design/skill-system-22-sync-five-meanings-of-trigger.md" >}}) — 一个技能"触发了"在不同语境下有 5 个含义，这是不同步的根本原因。
- [技能系统深度 23｜帧同步下的技能系统：确定性、输入收集与回滚重播]({{< relref "system-design/skill-system-23-lockstep-skill-system.md" >}}) — 确定性约束传导到浮点数、随机数、帧率、Buff 求值。
- [技能系统深度 24｜状态同步下的客户端预测与冲突消解：预测窗口、快照与回滚]({{< relref "system-design/skill-system-24-state-sync-prediction-rollback.md" >}}) — 预测窗口该多大、哪些状态该快照、服务端否决后怎么回滚。
- [技能系统深度 25｜命中确认与延迟补偿：Lag Compensation 在技能命中里怎么做]({{< relref "system-design/skill-system-25-hit-confirmation-lag-compensation.md" >}}) — "我打中了但没算"和"我躲开了但被打中"的根因。
- [技能系统深度 26｜服务端技能校验与反作弊：频率检测、资源校验与异常链的分层设计]({{< relref "system-design/skill-system-26-server-validation-anticheat.md" >}}) — 服务端校验三层架构、常见作弊检测、帧同步 vs 状态同步下反作弊差异。

这一组回答的是：

`技能系统进入联网场景后，"触发""命中""冷却"的真相到底归谁、客户端预测和服务端权威怎样调解、以及怎样防作弊。`

### 十、多角色协作（27-29）

- [技能系统深度 27｜策划配技能的完整数据管道：从 Excel / 编辑器到运行时的链路]({{< relref "system-design/skill-system-27-designer-data-pipeline.md" >}}) — SkillDef 哪些字段暴露给策划、公式系统复杂度边界、"改了一个数线上崩了"的防护链。
- [技能系统深度 28｜美术出技能表现的交付标准：VFX / 动画 / 音效的挂接点与验收规范]({{< relref "system-design/skill-system-28-artist-vfx-delivery-standard.md" >}}) — 四条时间线的对齐约定、挂接点命名规范、美术自检清单。
- [技能系统深度 29｜程序为策划和美术搭的基建：运行时、工具、管道各该提供什么]({{< relref "system-design/skill-system-29-programmer-infrastructure.md" >}}) — 三层基建（运行时/工具/管道）的接口边界和常见断裂点。

这一组回答的是：

`技能系统不是程序一个人的事——策划、美术、程序三个角色分别该交付什么、在哪对齐、常见的断裂点在哪。`

### 十一、深度展开（30-32）

- [技能系统深度 30｜Combo 系统架构：连招判定、输入窗口、动画衔接与取消树]({{< relref "system-design/skill-system-30-combo-architecture.md" >}}) — 输入窗口三种类型、动画衔接两种驱动模式、格斗/ARPG/MOBA 三种 Combo 模型。
- [技能系统深度 31｜数值平衡框架：伤害公式设计、数值曲线与平衡验证方法论]({{< relref "system-design/skill-system-31-balance-framework.md" >}}) — 伤害公式设计空间、TTK 作为平衡指标、自动化平衡验证。
- [技能系统深度 32｜技能系统性能剖析：GC 热点、实体瓶颈与帧预算分配]({{< relref "system-design/skill-system-32-performance-profiling.md" >}}) — 大规模场景下的 GC 压力、Tag 查询开销、Modifier 聚合成本和碰撞检测瓶颈。

这一组回答的是：

`技能系统里三个最容易被"以后再说"然后真出事的专题：Combo、数值平衡、性能。`

### 十二、案例与坑型录（33-36）

- [技能系统深度 33｜技能系统高频坑型录：7 类问题的信号、根因与最稳排法]({{< relref "system-design/skill-system-33-pitfall-patterns.md" >}}) — 7 类最常见问题按信号、根因、排查步骤和防护建议分类整理。
- [技能系统深度 34｜案例：技能多人不同步的 5 种根因 — 从"表现不对"到分层定位]({{< relref "system-design/skill-system-34-case-multiplayer-desync.md" >}}) — 5 种不同步根因，每种从现象到定位到修复走完一条链。
- [技能系统深度 35｜案例：Buff 系统失控 — 叠加、快照、实时重算混用的生产事故]({{< relref "system-design/skill-system-35-case-buff-system-meltdown.md" >}}) — Buff 叠到 99 层变无敌的事故复盘，从现场到根因到修法。
- [技能系统深度 36｜技能系统验证与问题定位指南：从"技能不对"到定位具体断层]({{< relref "system-design/skill-system-36-verification-and-diagnosis-guide.md" >}}) — 分层诊断流程图，每层有检查方法、日志关键字、常见误判和修复方向。

这一组回答的是：

`技能系统出问题时应该先查哪一层、最常见的 7 类坑长什么样、以及两个典型生产事故的完整复盘。`

## 如果你不是系统读，而是带着问题来查

如果你已经在项目里遇到具体问题，那比起从头读，更稳的是按问题跳。

### 1. 你现在的代码已经长成"大 SkillData / 大 UseSkill()"

先看：

- [技能系统深度 01｜边界]({{< relref "system-design/skill-system-01-boundaries.md" >}})
- [技能系统深度 02｜数据模型]({{< relref "system-design/skill-system-02-data-model.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})

### 2. 你现在最大的痛点是手感不好，按键、缓冲、后摇、取消总是互相打架

先看：

- [技能系统深度 03｜生命周期]({{< relref "system-design/skill-system-03-lifecycle.md" >}})
- [技能系统深度 04｜输入与施法请求]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}})
- [技能系统深度 09｜动画与表现解耦]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}})

### 3. 你现在最大的痛点是"为什么这个技能不能放"

先看：

- [技能系统深度 05｜校验与约束]({{< relref "system-design/skill-system-05-validation-and-constraints.md" >}})
- [技能系统深度 01｜边界]({{< relref "system-design/skill-system-01-boundaries.md" >}})

### 4. 你现在最大的痛点是命中、投射物、范围技能和近战判定混成一坨

先看：

- [技能系统深度 06｜目标选择与命中]({{< relref "system-design/skill-system-06-targeting-and-hit-resolution.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})

### 5. 你现在最大的痛点是 Buff 叠层、覆盖、刷新和属性计算越来越乱

先看：

- [技能系统深度 08｜Buff / Modifier]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})
- [技能系统深度 01｜边界]({{< relref "system-design/skill-system-01-boundaries.md" >}})

### 6. 你发现 AI 怪物放技能总和玩家不是一套逻辑

先看：

- [技能系统深度 10｜AI 与技能系统的边界]({{< relref "system-design/skill-system-10-ai-boundary.md" >}})
- [技能系统深度 04｜输入与施法请求]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}})
- [技能系统深度 05｜校验与约束]({{< relref "system-design/skill-system-05-validation-and-constraints.md" >}})

### 7. 你现在最大的痛点是一进联网或对战场景，命中、冷却、表现和真相就开始错位

先看：

- [技能系统深度 11｜多人同步]({{< relref "system-design/skill-system-11-multiplayer-sync.md" >}})
- [技能系统深度 04｜输入与施法请求]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}})
- [技能系统深度 06｜目标选择与命中]({{< relref "system-design/skill-system-06-targeting-and-hit-resolution.md" >}})
- [技能系统深度 09｜动画与表现解耦]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}})

### 8. 你现在最大的痛点是配置越来越多，但没人说得清一个技能到底依赖了什么、为什么配错

先看：

- [技能系统深度 12｜编辑器与配置工具]({{< relref "system-design/skill-system-12-editor-and-tooling.md" >}})
- [技能系统深度 02｜数据模型]({{< relref "system-design/skill-system-02-data-model.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})
- [技能系统深度 08｜Buff / Modifier]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})

### 9. 你现在最大的痛点是每次改完公式、Buff 或时间线，都不知道有没有悄悄打坏旧技能

先看：

- [技能系统深度 13｜测试与回归]({{< relref "system-design/skill-system-13-testing-and-regression.md" >}})
- [技能系统深度 03｜生命周期]({{< relref "system-design/skill-system-03-lifecycle.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})
- [技能系统深度 08｜Buff / Modifier]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})
- [技能系统深度 12｜编辑器与配置工具]({{< relref "system-design/skill-system-12-editor-and-tooling.md" >}})

### 10. 你开始研究 GAS，但总想把 Unity 技能系统和 GAS API 一一对上

先看：

- [技能系统深度 14｜Unity 自研技能系统 vs Unreal GAS]({{< relref "system-design/skill-system-14-unity-self-built-vs-unreal-gas.md" >}})
- [技能系统深度 01｜边界]({{< relref "system-design/skill-system-01-boundaries.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})
- [技能系统深度 09｜动画与表现解耦]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}})
- [技能系统深度 11｜多人同步]({{< relref "system-design/skill-system-11-multiplayer-sync.md" >}})

### 11. 你想深入理解 GAS 的运行时内部，不只是会用

先看：

- [技能系统深度 15｜GAS 运行时链路深挖]({{< relref "system-design/skill-system-15-gas-runtime-chain.md" >}})
- [技能系统深度 16｜GAS 网络预测层深挖]({{< relref "system-design/skill-system-16-gas-prediction-layer.md" >}})

### 12. 你想看 Dota 2 怎么做数据驱动，尤其是策划不碰代码能到什么程度

先看：

- [技能系统深度 17｜Dota 2 数据驱动技能系统]({{< relref "system-design/skill-system-17-dota2-data-driven.md" >}})
- [技能系统深度 27｜策划配技能的完整数据管道]({{< relref "system-design/skill-system-27-designer-data-pipeline.md" >}})

### 13. 你想了解 ECS 架构做技能系统和传统 OOP 有什么本质差异

先看：

- [技能系统深度 18｜守望先锋 ECS 技能方案]({{< relref "system-design/skill-system-18-overwatch-ecs-ability.md" >}})
- [技能系统深度 02｜数据模型]({{< relref "system-design/skill-system-02-data-model.md" >}})

### 14. 你想知道 MOBA 类的服务端技能架构怎么做，尤其是 Live Service 下的约束

先看：

- [技能系统深度 19｜英雄联盟服务端技能架构]({{< relref "system-design/skill-system-19-lol-server-architecture.md" >}})
- [技能系统深度 26｜服务端技能校验与反作弊]({{< relref "system-design/skill-system-26-server-validation-anticheat.md" >}})

### 15. 你想做大规模技能组合系统，担心组合爆炸

先看：

- [技能系统深度 20｜Path of Exile 技能图与 Gem 组合系统]({{< relref "system-design/skill-system-20-poe-gem-combination.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})

### 16. 你在做框架选型，自研 vs GAS vs 其他，需要一个横向对比

先看：

- [技能系统深度 21｜业内技能框架横向矩阵]({{< relref "system-design/skill-system-21-industry-comparison-matrix.md" >}})
- [技能系统深度 14｜Unity 自研技能系统 vs Unreal GAS]({{< relref "system-design/skill-system-14-unity-self-built-vs-unreal-gas.md" >}})

### 17. 你在做帧同步项目，想知道技能系统需要特别注意什么

先看：

- [技能系统深度 22｜技能同步的 5 个"触发"含义]({{< relref "system-design/skill-system-22-sync-five-meanings-of-trigger.md" >}})
- [技能系统深度 23｜帧同步下的技能系统]({{< relref "system-design/skill-system-23-lockstep-skill-system.md" >}})

### 18. 你在做状态同步项目，客户端预测和回滚总是出问题

先看：

- [技能系统深度 22｜技能同步的 5 个"触发"含义]({{< relref "system-design/skill-system-22-sync-five-meanings-of-trigger.md" >}})
- [技能系统深度 24｜状态同步下的客户端预测与冲突消解]({{< relref "system-design/skill-system-24-state-sync-prediction-rollback.md" >}})
- [技能系统深度 25｜命中确认与延迟补偿]({{< relref "system-design/skill-system-25-hit-confirmation-lag-compensation.md" >}})

### 19. 你想知道服务端怎么做技能防作弊

先看：

- [技能系统深度 26｜服务端技能校验与反作弊]({{< relref "system-design/skill-system-26-server-validation-anticheat.md" >}})
- [技能系统深度 11｜多人同步]({{< relref "system-design/skill-system-11-multiplayer-sync.md" >}})

### 20. 你是策划，想知道怎么配技能效率最高、出错最少

先看：

- [技能系统深度 27｜策划配技能的完整数据管道]({{< relref "system-design/skill-system-27-designer-data-pipeline.md" >}})
- [技能系统深度 12｜编辑器与配置工具]({{< relref "system-design/skill-system-12-editor-and-tooling.md" >}})

### 21. 你是美术或技术美术，想知道 VFX / 动画 / 音效怎么和技能对齐

先看：

- [技能系统深度 28｜美术出技能表现的交付标准]({{< relref "system-design/skill-system-28-artist-vfx-delivery-standard.md" >}})
- [技能系统深度 09｜动画与表现解耦]({{< relref "system-design/skill-system-09-animation-and-presentation-decoupling.md" >}})

### 22. 你是程序，想知道该给策划和美术提供什么工具和基建

先看：

- [技能系统深度 29｜程序为策划和美术搭的基建]({{< relref "system-design/skill-system-29-programmer-infrastructure.md" >}})
- [技能系统深度 12｜编辑器与配置工具]({{< relref "system-design/skill-system-12-editor-and-tooling.md" >}})

### 23. 你想做连招 / Combo 系统，但不确定输入窗口和取消树怎么设计

先看：

- [技能系统深度 30｜Combo 系统架构]({{< relref "system-design/skill-system-30-combo-architecture.md" >}})
- [技能系统深度 03｜生命周期]({{< relref "system-design/skill-system-03-lifecycle.md" >}})
- [技能系统深度 04｜输入与施法请求]({{< relref "system-design/skill-system-04-input-and-cast-request.md" >}})

### 24. 你想验证数值是否平衡，但不知道用什么方法论

先看：

- [技能系统深度 31｜数值平衡框架]({{< relref "system-design/skill-system-31-balance-framework.md" >}})
- [技能系统深度 08｜Buff / Modifier]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})

### 25. 你的技能系统在大规模场景下性能不行，不知道瓶颈在哪

先看：

- [技能系统深度 32｜技能系统性能剖析]({{< relref "system-design/skill-system-32-performance-profiling.md" >}})
- [技能系统深度 07｜效果系统]({{< relref "system-design/skill-system-07-effect-system.md" >}})

### 26. 你遇到了 Buff 相关的 bug：叠层异常、属性算错、Buff 清不掉

先看：

- [技能系统深度 33｜技能系统高频坑型录]({{< relref "system-design/skill-system-33-pitfall-patterns.md" >}})（重点看第一、二类）
- [技能系统深度 35｜案例：Buff 系统失控]({{< relref "system-design/skill-system-35-case-buff-system-meltdown.md" >}})
- [技能系统深度 08｜Buff / Modifier]({{< relref "system-design/skill-system-08-buff-modifier.md" >}})

### 27. 你遇到了技能不同步的 bug：客户端和服务端状态不一致

先看：

- [技能系统深度 33｜技能系统高频坑型录]({{< relref "system-design/skill-system-33-pitfall-patterns.md" >}})（重点看第五类）
- [技能系统深度 34｜案例：技能多人不同步的 5 种根因]({{< relref "system-design/skill-system-34-case-multiplayer-desync.md" >}})
- [技能系统深度 22｜技能同步的 5 个"触发"含义]({{< relref "system-design/skill-system-22-sync-five-meanings-of-trigger.md" >}})

### 28. 技能出了问题但你不知道该查哪一层，需要一个诊断流程

先看：

- [技能系统深度 36｜技能系统验证与问题定位指南]({{< relref "system-design/skill-system-36-verification-and-diagnosis-guide.md" >}})
- [技能系统深度 33｜技能系统高频坑型录]({{< relref "system-design/skill-system-33-pitfall-patterns.md" >}})

## 这组文章现在已经写到哪

到目前为止，这组文章已经覆盖了 38 篇（索引 + 00-36），分成十二个主题组：

| 主题组 | 篇数 | 覆盖 |
|---|---|---|
| 一、问题空间与概念边界 | 2 | 00-01 |
| 二、骨架与运行时模型 | 2 | 02-03 |
| 三、执行主链 | 3 | 04-06 |
| 四、世界变化与持续规则 | 2 | 07-08 |
| 五、跨系统边界 | 3 | 09-11 |
| 六、工程化落地 | 2 | 12-13 |
| 七、框架映射与收束 | 1 | 14 |
| 八、业内框架深挖 | 7 | 15-21 |
| 九、客户端-服务端联合 | 5 | 22-26 |
| 十、多角色协作 | 3 | 27-29 |
| 十一、深度展开 | 3 | 30-32 |
| 十二、案例与坑型录 | 4 | 33-36 |

也就是说，这组文章现在已经能回答：

`技能系统在单端和多人场景里怎样搭出稳定执行链、业内五大框架各自的运行时长什么样、帧同步和状态同步下技能系统的关键差异在哪、策划-美术-程序各自该交付什么、Combo / 数值 / 性能三个专题怎么展开、以及遇到问题时该从哪层开始诊断。`

## 这篇真正想留下来的结论

技能系统最怕的，不是知识点不够，而是整条链在脑子里没有地图。

只要没有这张地图，团队讨论就会自动混成：

- 输入
- 技能
- 命中
- Buff
- 动画
- AI
- 同步
- 作弊
- 性能
- 工具

全都像"技能逻辑"的一部分。

而这组系列真正想做的，就是把这些层一层层拆开，再重新接回同一条执行链——先用 15 篇主线建骨架，再用 22 篇深挖把每一层在业内框架、网络模型、协作流程和真实事故里的具体样子补完。

如果把整篇压成最短一句话，我会这样总结：

`先把技能系统看成一条完整执行链，再决定自己当前卡在输入、约束、命中、效果、Buff、表现、AI、同步、框架选型、协作还是诊断哪一层。这个索引页的作用，就是先给你这张地图。`
