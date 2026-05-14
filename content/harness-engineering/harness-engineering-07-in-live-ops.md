---
title: "Harness Engineering 07｜Harness 在长线运营里的位置"
slug: "harness-engineering-07-in-live-ops"
date: "2026-05-13"
description: "在长线运营场景里，Harness 怎样把 AI 接进来。运营期高频低风险任务多，Harness 化 ROI 高；但活动配置、热更、玩家反馈这些场景有它们独有的约束。"
tags:
  - "Harness Engineering"
  - "Live Ops"
  - "AI Engineering"
series: "Harness Engineering"
primary_series: "harness-engineering"
series_role: "article"
series_order: 70
weight: 2170
---

> **读这篇之前**：本篇假设你已经读过 [01]({{< relref "harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md" >}}) 和 [06｜Harness 在交付工程里的位置]({{< relref "harness-engineering/harness-engineering-06-in-delivery-engineering.md" >}})。如果你想了解长线运营主线，先看 [长线运营工程专栏]({{< relref "live-ops-engineering" >}})——本篇只讲 Harness 跟运营场景的接口。

## 这篇解决什么问题

游戏上线之后真正的工作量在长线运营。这阶段的任务跟开发期不一样：

- 频率更高（每周活动配置、每月版本、随时热更）
- 单次风险更低（不会一个改动炸掉整个项目，但可能炸掉某个活动）
- 反馈更快（玩家几小时内会发现问题）
- 跟数据系统紧密耦合（每个改动要看数据反馈）

这正好是 Harness 化 ROI 最高的场景——高频低风险、规则稳定、失败模式明确。但运营也有它独有的约束，外部讲 Harness 的文章基本没碰。

这篇要回答的是：

- 长线运营的任务谱跟开发期有什么不同
- 哪些运营任务最适合 Harness 化
- 活动配置 / 热更 / A/B / 玩家反馈四个场景各自的 Harness 设计要点
- 怎样让 Harness 跟数据系统接

## 长线运营的任务谱

跟交付工程的任务谱相似，但有自己的特点：

### 类型一：周期性配置任务

- 例：每周开新活动、改活动参数、上线新签到
- 特点：模板化强、变量少、失败影响有限
- 适合 Harness 化吗：非常适合

### 类型二：热更代码

- 例：修一个线上 bug、加一个紧急 feature flag
- 特点：风险中等，必须快但又不能急
- 适合 Harness 化吗：部分——AI 起草，人 review

### 类型三：A/B 实验

- 例：开一个新功能给 5% 玩家、统计 7 天数据
- 特点：跨多个系统、依赖数据回流
- 适合 Harness 化吗：实验配置可以 Harness 化，决策不能

### 类型四：玩家反馈处理

- 例：玩家报告某活动 bug、客服汇总常见问题
- 特点：非结构化输入、要 case by case
- 适合 Harness 化吗：分类 / 路由可以；判断必须人

<!-- EXPERIENCE-TODO: 每个类型补一两个真实例子。 -->

## 活动配置场景的 Harness 设计

活动配置是运营最高频任务。Harness 设计要点：

### 要点一：把活动配置 schema 当成一级 Context

- 不是把 schema 文档藏在 Wiki 里让 AI 自己找
- 应该在 Harness Context 层显式声明：本项目当前的活动配置 schema 在哪、有几种活动类型、每种类型的必填字段是什么

### 要点二：用真实历史活动作为 few-shot

- AI 第一次开活动时给它看 3-5 个已上线活动的真实配置
- 比 schema 描述更有用——AI 能看到约定不在 schema 里的部分

### 要点三：把"上线前必跑的校验"做成机械化门禁

- 活动时间不冲突 / 奖励配置合法 / 文案过审等等
- 这些校验不应该靠 CLAUDE.md 描述，应该是 lint 工具或 CI check

### 要点四：失败回流到模板

- 每次活动上线后出问题，回头把那个失败点写进活动模板的"注意事项"
- 不是写规则——是改模板本身

<!-- DATA-TODO: 补一份真实活动配置 Harness 的最小例子——schema 节选 + few-shot 配置 + 机械化门禁列表。 -->

## 热更代码场景的 Harness 设计

热更对游戏项目特别——它有自己的代码加载机制（HybridCLR / Lua / 自研脚本系统）、有自己的限制（什么能热更什么不能）。

### 要点一：把"热更能力边界"写进 Rules

- 例：HybridCLR 下哪些 .NET 特性是热更补丁限制的
- 例：哪些字段类型变化会破坏热更兼容性
- 这些边界写在 CLAUDE.md 里，AI 写代码时主动避开

### 要点二：热更修改必须走"双跑"

- 同一个修改要在主工程和热更补丁两边都跑
- Harness 状态机里加一个"双跑验证"阶段

### 要点三：回滚预案是 Workflow 的强制项

- 任何热更任务，Plan 阶段必须包含回滚方案
- 没有回滚方案的热更任务，Harness 直接拒绝进入 Execute

<!-- EXPERIENCE-TODO: 用 HybridCLR 真实场景举例。配合 .NET Runtime 生态系列的相关篇章交叉引用。 -->

## A/B 实验场景的 Harness 设计

A/B 实验跨系统多（功能开关、灰度系统、数据系统、活动系统），是 Harness 容易踩坑的场景。

### 要点一：实验配置和实验决策严格分开

- AI 可以帮起草实验配置（参数、灰度比例、统计周期）
- 但实验"做不做、什么时候做"是产品决策

### 要点二：实验过程不让 AI 干涉

- 实验跑起来后中途参数不能改、不能提前结束、不能加新分支
- Harness 在这个阶段进入"只读"模式

### 要点三：实验结果分析可以借 AI

- 拉数据、生成报告、提炼初步发现 → AI 适合
- 决定"是否全量上线" → 人

<!-- DATA-TODO: 补一份"AI 起草 A/B 实验配置"的真实例子——配置长什么样、哪些字段 AI 起草、哪些必须人填。 -->

## 玩家反馈处理场景的 Harness 设计

玩家反馈是非结构化的，最容易把 AI 用得过度。

### 要点一：分类 / 路由可以 Harness 化

- 玩家反馈进来 → AI 分类（bug / 建议 / 投诉 / 客服问询） → 路由到对应人员
- 这是有边界的、可验证的任务

### 要点二：自动回复要严格控制

- AI 起草回复 → 人 review → 发送
- 不要让 AI 直接对玩家说话

### 要点三：长期模式识别留给人

- "这周玩家反馈集中在某个点" → AI 可以汇总
- "这意味着我们的某个设计有问题" → 人来判断

<!-- EXPERIENCE-TODO: 如果有真实运营反馈系统的接入经验可以补。没有的话用泛化的"客服系统 + 反馈分类"作例子。 -->

## Harness 跟数据系统的接口

长线运营高度依赖数据。Harness 跟数据系统接口的关键：

### 接口一：让 AI 能查数据但不能改数据

- 数据查询接口（读取活动指标、玩家行为统计）→ 可以暴露给 AI
- 数据写入接口（修改埋点、清洗数据） → 必须人

### 接口二：数据回流到 Memory

- 上次活动的数据表现 → 沉淀进 Harness Memory
- 下次类似活动开始前 AI 自动参考

### 接口三：异常数据自动报警

- 数据异常时不依赖 AI 主动发现——靠监控系统
- AI 只负责在被通知后帮分析

<!-- DATA-TODO: 补一份"数据查询接口暴露给 AI 的最小白名单"——具体哪些 query 类型可以、哪些不行。 -->

## 跟长线运营其他文章的联动

本篇是 Harness 视角看长线运营。如果你想从运营视角理解全貌：

- [长线运营 · AI 集成]({{< relref "live-ops-engineering" >}})——后续根据具体篇目稳定后补完整 relref

<!-- DATA-TODO: 等 live-ops-engineering 系列的具体篇目稳定后，补 2-3 处具体 relref。 -->

## 收束

长线运营是 Harness 落地 ROI 最高的场景，但也是最容易把 AI 用得过度的场景——因为运营任务看起来都"很模板"，容易让人想"全交给 AI"。

最短结论是：

**运营场景的 Harness，关键不是让 AI 接多少活，而是让 AI 知道哪些事它绝对不该碰——尤其是直接对玩家说话和直接改数据。**

下一篇 [08｜Harness 复盘与指标]({{< relref "harness-engineering/harness-engineering-08-retrospective-and-metrics.md" >}}) 是这个系列的收尾——跑了这么多 Harness 任务之后，怎么度量它真的让系统变好了。

<!-- DATA-TODO: 等真实跑过 2-3 次运营任务 Harness 化后，把这篇所有 TODO 都填实。 -->
