---
title: "Harness Engineering 08｜Harness 复盘与指标"
slug: "harness-engineering-08-retrospective-and-metrics"
date: "2026-05-13"
description: "跑过 N 次 Harness 任务之后，怎么度量 Harness 真的让系统变好了。这篇给出可操作的复盘节奏和五项最小指标的实操方法，不讲虚的\"AI Coding 率\"。"
tags:
  - "Harness Engineering"
  - "Metrics"
  - "Retrospective"
  - "AI Engineering"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 80
weight: 2180
---

> **读这篇之前**：本篇是系列收尾，最依赖真实执行记录。建议先把 02-07 都过一遍——它们提到的"等真实跑过 N 次任务"在这篇汇总。

## 这篇解决什么问题

跑了一段时间 Harness 之后，每个人都会问同一个问题：

**它到底有没有让事情变好？**

外部讲 Harness 的文章基本用一个指标回答这个问题——"AI Coding 率 90%"。但这个指标的问题很大：

- AI 生成的代码行数不等于它承担的工程判断
- 算"AI 写的"还是"人改了之后还算 AI 写的"——口径模糊
- 把所有任务都算进去，会把"AI 写 boilerplate"和"AI 完成复杂业务"等价

这是 [ai-empowerment-08]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}}) 里已经讲过的观点。这篇不重复，而是回答下一步：**不用 AI Coding 率，那用什么？**

这篇要回答的是：

- 五项最小指标的实操方法
- 复盘的节奏和模板
- 怎样判断"Harness 真的变好"vs"只是这周运气好"
- 反馈沉淀的归类法则

## 五项最小指标的实操方法

[ai-empowerment-08]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}}) 列了五项最小指标，这篇展开实操。

### 指标一：一次交付可用率

- 定义：AI 第一次输出能直接进入下一阶段的比例
- 怎么算：每次任务记录"AI 输出 → 人/工具下一步操作"是否需要返工
- 阈值参考：Bootstrap 期 30%-50%；Growth 期 60%-80%；超过 80% 要警觉是不是只在做 boilerplate

<!-- DATA-TODO: 补一份真实任务的一次交付可用率统计——至少 10 个任务样本，分类记录。 -->

### 指标二：人工返工点

- 定义：返工时人主要改哪几类东西
- 怎么算：返工时打标签——结构 / 事实 / 风格 / 边界 / 链接
- 阈值参考：单一标签占比超过 50% 说明 Harness 在那个维度有缺口

| 返工类型 | 说明 | 应对 |
|---------|------|------|
| 结构 | 章节/代码结构错 | 补 Workflow |
| 事实 | 引用的事实/数据错 | 补 Context |
| 风格 | 表达风格不符 | 软规则，先观察 |
| 边界 | 越过停止点 | 补 Rules |
| 链接 | relref / 引用错 | 补 Skill 检查项 |

<!-- DATA-TODO: 补一份真实返工标签分布，最好带 2 个时间点对照。 -->

### 指标三：规则违例数

- 定义：AI 违反 CLAUDE.md / Skill 描述里写的规则的次数
- 怎么算：每次任务结束扫一遍输出
- 阈值参考：单条规则月违例超过 3 次 → 要么规则没传达到、要么规则该重写

### 指标四：验证通过率

- 定义：AI 输出 + 第一次跑验证（构建 / 测试 / lint）通过的比例
- 怎么算：CI 结果或本地构建结果
- 阈值参考：通过率长期低于 60% 说明 Verify 设计或 Execute 边界有问题

### 指标五：反馈沉淀率

- 定义：失败案例中转化为规则 / Skill / 文档的比例
- 怎么算：每个失败事件后跟踪是否回写
- 阈值参考：至少 50%（剩下 50% 是一次性偏好或边界外）

<!-- DATA-TODO: 补一份"过去 N 次失败 → 沉淀去向"的真实表格。 -->

## 复盘的节奏和模板

不需要每周都跑完整复盘。下面是一个分层节奏：

### 单次任务复盘（每次任务结束后）

- 5 分钟以内
- 模板：
  ```
  任务名：
  Agent：
  一次交付：是 / 否（如否，返工类型）
  规则违例：有 / 无（如有，哪条）
  验证：通过 / 失败（如失败，原因）
  沉淀动作：无 / 写规则 / 改 Skill / 补文档
  ```

### 周复盘（每周一次）

- 30 分钟
- 看本周五项指标的快照
- 看是否有同类错重复（Repetition rate）
- 决定下周要不要瘦身或 audit

### 月复盘（每月一次）

- 2 小时
- 看月度趋势
- 决定是否进入下一个生命周期阶段
- 决定是否大规模瘦身 / audit / 升级

### 季度复盘（每季度一次）

- 半天
- 战略级判断：Harness 是不是还在解决正确的问题
- 是否需要重构整个 Harness 模型

<!-- EXPERIENCE-TODO: 补一份"我自己最近一次月复盘的真实记录"——可以匿名化数字。重点是月度趋势看到了什么、做了什么决定。 -->

## 怎样判断"Harness 真的变好"vs"运气好"

单次任务好坏波动很大。要看趋势：

### 判断一：连续 N 周指标向好

- 一周的"指标变好"不算数
- 至少连续 4 周指标向好才能确认趋势
- 反过来：单周变差不代表退步

### 判断二：同类任务的指标差距收敛

- 不同 Agent / 不同任务类型的指标之间差距应该缩小
- 差距长期不收敛说明 Harness 在某些场景下没起作用

### 判断三：人接的"判断类工作"比例上升

- AI 接走机械化任务后，人理应有更多时间做架构 / 业务 / 边界判断
- 如果人还在反复纠正 AI 的低级错误，Harness 没真的让事情变好

### 判断四：新人 onboarding 时间下降

- Harness 是给新人和 AI 都看的
- 新人入项目能跑通第一个完整流程的时间，是 Harness 实际效用的一个慢指标

<!-- DATA-TODO: 等真实跑半年以上后，补一份 6 个月的指标演化曲线和趋势判断。 -->

## 反馈沉淀的归类法则

不是每个失败都该写成规则。归类法则：

| 失败原因 | 沉淀位置 |
|---------|---------|
| 项目级规则遗漏 | CLAUDE.md / AGENTS.md |
| 某类任务流程遗漏 | `.agents/harness/*.md` |
| 某个 Skill 触发不准 | Skill description |
| 需要真实数据 | 正文 `DATA-TODO` |
| 需要项目经验 | 正文 `EXPERIENCE-TODO` |
| 一次性偏好 | 不沉淀，仅在对话内说明 |
| 边界外的事故 | 不沉淀；如果重复发生才考虑 |
| Drift 引起的错 | 修规则本身，不加新规则 |

最后两条特别重要——很多人沉淀过度，把所有失败都写成规则，最终堆出 Bloat。

<!-- EXPERIENCE-TODO: 补一段"我曾经沉淀过度的真实例子"——写了一条规则后悔了，最终回头删掉。这种例子比"沉淀对了"更有教学价值。 -->

## 一个常见错觉：指标变好等于系统变好

要警惕的几个错觉：

### 错觉一：一次交付可用率高 = 真的好

- 可能是因为只接了简单任务
- 必须配合"任务复杂度分布"看

### 错觉二：规则违例数低 = 规则有效

- 可能是规则太宽以至于没法违反
- 必须配合"规则被触发引用次数"看

### 错觉三：沉淀率高 = 学习快

- 可能是沉淀过度
- 必须配合"沉淀后 N 周是否被再次引用"看

<!-- DATA-TODO: 等积累了真实指标后，验证这三个错觉是否在自己的数据里出现过。 -->

## 收束

Harness Engineering 不是一项可以"完成"的工程。它是一条长期演化的轨道——需要诊断、瘦身、对齐、复盘。

最短结论是：

**别问 Harness 让 AI 写了多少。问 Harness 让人节省了多少重复判断、让同类错少了多少、让新人接手快了多少。这些是慢指标，但是真指标。**

回到系列开头——[01｜为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness]({{< relref "harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md" >}}) 讲了领域差异化；[02]({{< relref "harness-engineering/harness-engineering-02-five-stage-lifecycle.md" >}})-[04]({{< relref "harness-engineering/harness-engineering-04-drift-and-rot.md" >}}) 讲演化与诊断；[05]({{< relref "harness-engineering/harness-engineering-05-cross-repo-sdk-vendor.md" >}}) 讲跨仓库；[06]({{< relref "harness-engineering/harness-engineering-06-in-delivery-engineering.md" >}})-[07]({{< relref "harness-engineering/harness-engineering-07-in-live-ops.md" >}}) 讲场景联动。本篇收尾。

一个 Harness 的真正价值，要等你走完一个完整生命周期才能看清。

<!-- DATA-TODO: 这篇是全系列最依赖真实执行数据的一篇。发布前必须有至少 3 次完整 Harness 任务记录（含指标 + 复盘 + 沉淀），否则会变成纯方法论。建议本篇先标"草稿"，等其他 7 篇发布、并跑过若干轮真实任务后再发。 -->
