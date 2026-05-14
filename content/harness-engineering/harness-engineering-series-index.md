---
date: "2026-05-13"
title: 'Harness Engineering 系列索引｜v0 之后，Harness 该怎么长'
description: "不讲怎么搭 v0——那是 ai-empowerment 08 的事。讲游戏引擎客户端场景下，Harness 的演化、诊断、瘦身、跨仓库分层，以及跟交付与长线运营的接口。"
slug: "harness-engineering-series-index"
weight: 1
featured: true
tags:
  - "Harness Engineering"
  - "AI Engineering"
  - "Developer Productivity"
  - "Game Engine"
  - "Index"
series: "Harness Engineering"
series_id: "harness-engineering"
series_role: "index"
series_order: 0
series_nav_order: 60
series_title: "Harness Engineering"
series_entry: true
series_audience:
  - "已搭过 v0 Harness 的游戏引擎 / 客户端工程师"
  - "同一份 SDK vendor 到多项目的 Package 作者"
  - "想把 AI Coding 实践写进作品集的资深工程师"
series_level: "进阶"
series_best_for: "当你已经搭起来 v0 Harness、但不确定它现在该加、该删、还是该拆"
series_summary: "围绕一个独家主轴——Harness 的五阶段生命周期（Bootstrap → Growth → Bloat → Drift → Sunset）和四个诊断指标——把游戏引擎客户端的领域约束、跨仓库 SDK 作用域、交付与运营场景的接口都接上。"
series_intro: "外部很多文章讲怎么搭一个 Harness 把 AI Coding 率提到 90%，讲完就结束了。但 Harness 真正难的不是搭起来，是搭起来之后维护、瘦身、退役。这个系列假设你已经读过 ai-empowerment 08，从 v0 之后接续，主战场是游戏引擎客户端。"
series_reading_hint: "先读 01 确认你是不是这个系列的目标读者，再按演化主轴（02-04）→ 场景联动（05-07）→ 复盘（08）顺序读。"
---

## 这组文章要解决什么

外面关于 AI Coding Harness 的文章基本两类：

- **企业级团队提效故事**：搭起来一个 Harness，AI Coding 率从 25% 到 90%，七天五人干完二十人数周的活
- **方法论与规范**：四根支柱、Skill 设计模式、Spec-Driven Development

这些都不解决一个工程师真实会碰到的问题：

- 我已经搭起来一个 v0 Harness，它现在该加、该删、还是该拆？
- 我的 Harness 越长越厚，已经开始干扰判断了，怎么瘦身？
- 我的 Harness 规则跟代码慢慢脱节了，怎么发现、怎么修？
- 我有一个 SDK 被 vendor 到三个游戏项目里，Harness 的规则放 SDK 内还是宿主里？
- 我做游戏引擎客户端，C++ 引擎源码、Shader Variant、AssetBundle 这些东西的 Harness 跟做 Java 后端不一样，差异在哪？

这个系列从 [AI 赋能 08｜我如何搭建自己的 AI Coding Harness Engineering]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}}) 之后接续——假设你已经懂五层模型（Context / Rules / Workflow / Checks / Memory）、状态机（Intake → Handoff → Learn）和五项最小指标。

## 先给一句总判断

**Harness 不是 prompt 的进化版，也不是流程的复杂化。它是给 AI Agent 工作搭一条可控轨道——轨道会因为工程变化而扭曲、生锈、长歪。维护这条轨道，才是 Harness Engineering 的真正主题。**

## 文章列表

### 领域差异化线（01）

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [为什么游戏引擎客户端的 AI Coding 需要重新设计 Harness]({{< relref "harness-engineering/harness-engineering-01-why-game-engine-needs-rethink.md" >}}) | 通用 v0 Harness 在游戏引擎客户端场景会遇到什么独有约束 |

### 演化与诊断主轴（02-04）

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 02 | [v0 之后——Harness 的五阶段生命周期]({{< relref "harness-engineering/harness-engineering-02-five-stage-lifecycle.md" >}}) | Bootstrap / Growth / Bloat / Drift / Sunset 怎么判断、怎么过渡 |
| 03 | [Bloat 反模式与瘦身]({{< relref "harness-engineering/harness-engineering-03-bloat-and-slimming.md" >}}) | 什么时候 Harness 该停止扩展、规则与上下文该怎么裁 |
| 04 | [Drift 与文档腐烂]({{< relref "harness-engineering/harness-engineering-04-drift-and-rot.md" >}}) | 怎样在游戏项目快节奏迭代中防止 Harness 跟代码脱节 |

### 场景联动与复盘（05-08）

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 05 | [跨仓库 Harness：SDK vendor 视角]({{< relref "harness-engineering/harness-engineering-05-cross-repo-sdk-vendor.md" >}}) | 同一份 SDK vendor 到多个宿主项目时，Harness 信息归谁 |
| 06 | [Harness 在交付工程里的位置]({{< relref "harness-engineering/harness-engineering-06-in-delivery-engineering.md" >}}) | 多端交付闭环里，Harness 跟构建脚本、产物校验、发布门禁怎样衔接 |
| 07 | [Harness 在长线运营里的位置]({{< relref "harness-engineering/harness-engineering-07-in-live-ops.md" >}}) | 运营期高频低风险任务，AI 通过 Harness 接进来的判断 |
| 08 | [Harness 复盘与指标]({{< relref "harness-engineering/harness-engineering-08-retrospective-and-metrics.md" >}}) | 跑过 N 次 Harness 任务后，怎样度量 Harness 真的让系统变好了 |

## 推荐阅读顺序

**如果你已经搭过 v0**，按 01 → 02 → 03/04 → 05/06/07（按需）→ 08。

**如果你没搭过 v0**，先回去读 [AI 赋能 08]({{< relref "ai-empowerment/ai-empowerment-08-ai-coding-harness-engineering.md" >}})。

**如果你主要做交付**，01 → 06。**主要做长线运营**，01 → 07。**做 SDK / Package**，01 → 05。

## 与其他文章 / 系列的关系

[AI 赋能游戏开发]({{< relref "ai-empowerment/ai-empowerment-series-index.md" >}}) 系列是 v0 实施层——CLAUDE.md（05）、Skill（06）、跨层开发流程（07）、Harness v0（08）。本系列从 v0 之后接续。

[代码质量到工程质量]({{< relref "code-quality" >}}) 系列讲质量判断、CI、Quality Gate；本系列只在 Harness 跟这些机制的接口处讨论它。

[交付工程]({{< relref "delivery-engineering" >}}) 和 [长线运营工程]({{< relref "live-ops-engineering" >}}) 是场景层；本系列 06 / 07 篇分别接这两条线。
