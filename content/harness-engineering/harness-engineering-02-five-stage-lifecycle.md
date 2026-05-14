---
title: "Harness Engineering 02｜v0 之后——Harness 的五阶段生命周期"
slug: "harness-engineering-02-five-stage-lifecycle"
date: "2026-05-13"
description: "Harness 不是一搭起来就稳态运行的工程系统。它有生命周期：Bootstrap → Growth → Bloat → Drift → Sunset。这篇给出四个可操作的诊断指标，帮你判断自己的 Harness 现在所处的阶段。"
tags:
  - "Harness Engineering"
  - "AI Engineering"
  - "Developer Productivity"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 20
weight: 2120
---

> **读这篇之前**：建议先读 [01｜为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness]({{< relref "harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md" >}})，理解领域差异化在哪。这一篇假设你的 v0 已经跑起来一段时间，开始感觉"不太对劲但说不出来"。

## 这篇解决什么问题

很多人搭起来 v0 Harness 之后会卡在一个奇怪的位置：

- 一开始 AI 接活很顺，CLAUDE.md 短小、Skill 清爽、状态机不绕
- 跑了三个月之后，CLAUDE.md 一千多行、Skill 二十几个、每次开 session 光读上下文就要两分钟
- AI 错的地方反而比一开始多了——不是不会做，是被太多规则裹住，反复反过来确认

这不是"Harness 设计错了"，是 Harness 在演化。任何活的工程系统都有生命周期。问题在于，外部讲 Harness 的文章基本只讲"怎么从 0 到 v0"——讲完搭起来的瞬间就结束了。**v0 之后这条更长的路，没人讲。**

这篇要回答的是：

1. Harness 的生命周期有哪几个阶段
2. 怎么判断自己现在在哪个阶段
3. 阶段切换时该做什么、不该做什么

## 五阶段生命周期

```text
Bootstrap → Growth → Bloat → Drift → Sunset
```

### Bootstrap：第一份 Harness 跑起来

- 特征：CLAUDE.md 很短、Skill 很少、Workflow 还在试错
- 主要工作：把"AI 必须知道的项目事实"显式化
- 健康信号：AI 第一次接任务能完成，但需要多次澄清
- 风险：还没立稳就开始往里堆规则

### Growth：规则与 Skill 在快速沉淀

- 特征：每次 AI 犯一种新错，就回写一条规则、补一个 Skill
- 主要工作：把重复犯的错变成机械化门禁
- 健康信号：同类错连续出现概率下降、AI 一次交付可用率上升
- 风险：把不该规则化的偏好也写成规则

### Bloat：规则过多开始干扰判断

- 特征：CLAUDE.md / Skill 描述累计超过一定体量，AI 开始"为遵守 A 规则而违反 B 规则"
- 主要工作：识别哪些规则该删、该合、该降级为"建议"
- 健康信号：AI 在没有人为干预时也会主动询问而不是闷头做
- 风险：用更多规则修补 Bloat 引起的错乱

### Drift：规则跟代码脱节

- 特征：CLAUDE.md 里写的规则在代码里已经不成立——可能是版本升级、可能是约定变更、可能是约定本来就没人维护
- 主要工作：把"规则"和"实际代码状态"做一次重新对齐
- 健康信号：每次 AI 犯错时，能快速判断是 AI 错还是规则错
- 风险：把 Drift 当成 Bloat 处理，删错规则

### Sunset：整个 Harness 退役或重构

- 特征：项目本身大变（换引擎、换语言、合并到别的项目），原 Harness 不再适用
- 主要工作：决定保留哪部分作为 v2 的种子
- 健康信号：能清楚区分"这个 Harness 教会我的东西"和"这个 Harness 本身的实现"
- 风险：因为不舍得而强行延寿

<!-- EXPERIENCE-TODO: 这五个阶段最好配一张时间线图——可以是手绘也可以是 mermaid，标注每个阶段大致的时间长度、主要事件和过渡信号。 -->

## 四个可操作的诊断指标

阶段不是凭感觉判断的。下面四个指标是 wiki 工作笔记里沉淀的、可以每周或每两周看一眼的诊断面板。

### 指标一：Context bloat 比

- 定义：每次 AI session 开场加载的 token 数 / 该任务实际产出的 token 数
- 健康区间：Bootstrap 期可以很高（学习成本），Growth 期应该下降，Bloat 期反向上升
- 怎么算：粗算，CLAUDE.md + 主要 Skill 描述的字符数 / 一次任务的平均输出字符数
- <!-- DATA-TODO: 补一份自己项目的真实计算示例 -->

### 指标二：Skill 复用率

- 定义：某个 Skill 在过去 N 次任务中被实际触发的次数
- 健康区间：每个 Skill 至少有一次/周的实际触发
- 长期为 0 的 Skill：要么删，要么 description 写错了根本触发不到

### 指标三：Memory 沉淀率

- 定义：过去 N 次"AI 犯错"中，有多少回流成了规则 / Skill / 文档
- 健康区间：至少 50%——剩下的 50% 是一次性偏好或边界外的事故
- 沉淀率长期低于 30%：要么 Harness 本身不接受沉淀（流程问题），要么沉淀位置错乱

### 指标四：Repetition rate

- 定义：同一类错在过去 N 周内重复发生的次数
- 健康区间：每类错沉淀后，Repetition rate 应该在 2 周内降到 0
- 长期不为 0：要么沉淀进了 AI 读不到的位置，要么规则被其他规则盖住了

<!-- DATA-TODO: 补一份"我自己项目当前四项指标的快照"，最好带 2-3 个时间点的对照，看演化趋势。 -->

## 阶段过渡的实操手册

### Bootstrap → Growth

- 触发条件：连续 3 次任务没出现"AI 完全不知道这个项目在做什么"的卡点
- 该做：开始把单次澄清沉淀成规则
- 不该做：还没立稳就拆 Skill

### Growth → Bloat（应该尽量避免，但通常会发生）

- 触发条件：Context bloat 比开始反弹、AI 在"该执行"和"该确认"之间反复横跳
- 该做：先暂停加规则，回头审计现有规则的冲突
- 不该做：再加一条规则去修这个冲突

### Bloat → Growth（瘦身回到健康）

- 这是 03 篇 [Bloat 反模式与瘦身]({{< relref "harness-engineering/harness-engineering-03-bloat-and-slimming.md" >}}) 的主题
- 简单说：删 / 合 / 降级，主动放弃

### Growth → Drift（无意识发生）

- 触发条件：代码大改但 Harness 不动、项目升大版本后 CLAUDE.md 没审
- 该做：定期 audit；这是 04 篇 [Drift 与文档腐烂]({{< relref "harness-engineering/harness-engineering-04-drift-and-rot.md" >}}) 的主题
- 不该做：忽视

### 任意阶段 → Sunset

- 触发条件：项目级变更让原 Harness 不再适用
- 该做：留下教训文档，明确说 v1 死在哪里、v2 应该避免什么
- 不该做：把 v1 的所有规则直接搬到 v2

## 一个常见误判

很多人会把自己的 Harness 状态判错。

最常见的误判是：**把 Drift 当成 Bloat**。看到 Harness 越长越厚、AI 错的越来越多，第一反应是"规则太多了，要瘦身"——然后开始删规则。结果删了之后 AI 错得更多，因为问题根源不是规则多，是规则没跟上代码。

第二常见的误判是：**把 Bootstrap 缺位当成 Growth**。Harness 还没立稳（很多基础上下文都没写清楚），就开始往里加 Skill 和工作流——结果 Skill 触发条件踩不准、工作流走半道断掉，反而更乱。

判断的方法是回到四项指标：

- Repetition rate 长期不为 0 → Drift 嫌疑大
- Context bloat 比反向上升 → Bloat 嫌疑大
- Skill 复用率长期为 0 → Bootstrap 缺位嫌疑大
- Memory 沉淀率持续低 → 整个流程问题

<!-- EXPERIENCE-TODO: 补一段"我自己曾经误判过的一次"的叙述——具体是把哪个阶段误判成另一个、走了多远才回头、怎么修。 -->

## 收束

Harness 不是一搭起来就稳态运行的工程系统。它会长、会胖、会脱节、会过时。这条生命周期不是设计问题，是工程现实。

最短结论是：

**别问"我的 Harness 设计得对不对"。问"我的 Harness 现在处于哪个阶段、下一步该怎样过渡"。**

下一篇 [03｜Bloat 反模式与瘦身]({{< relref "harness-engineering/harness-engineering-03-bloat-and-slimming.md" >}}) 展开"Bloat 怎么识别、怎么瘦身"。再下一篇 [04｜Drift 与文档腐烂]({{< relref "harness-engineering/harness-engineering-04-drift-and-rot.md" >}}) 展开 Drift。

<!-- DATA-TODO: 等真实跑过 2-3 次完整生命周期（Bootstrap → Growth → Bloat → 瘦身回 Growth）后，补完整阶段时间线和指标演化曲线。当前阶段建议只发布到 03 / 04 篇骨架，等真实记录回填后再发 02。 -->
