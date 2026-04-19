---
date: "2026-04-16"
title: 'AI 赋能游戏开发系列索引｜从知识闭环到开发工作流'
description: "不讲 AI 概念，讲游戏团队怎么用 AI 解决知识管理和开发效率的实际问题。每篇都有落地案例，来自 20 人 Unity MMO 团队的真实实践。"
slug: "ai-empowerment-series-index"
weight: 1
featured: true
tags:
  - "AI Engineering"
  - "Knowledge Management"
  - "Developer Productivity"
  - "Index"
series: "AI 赋能游戏开发"
series_id: "ai-empowerment"
series_role: "index"
series_order: 0
series_nav_order: 50
series_title: "AI 赋能游戏开发"
series_entry: true
series_audience:
  - "技术负责人 / 主程"
  - "有 AI 工具使用经验的开发者"
  - "负责团队知识管理的人"
series_level: "进阶"
series_best_for: "当你已经知道该用 AI，但不知道怎么在团队里系统地落地"
series_summary: "一个知识管理闭环（Dify + LKB + Wiki）加一套 AI 辅助开发工作流（CLAUDE.md + Skill + 跨层联动），全部来自真实项目实践。"
series_intro: "这组文章解决的核心问题是：游戏团队用 AI 工具到底能解决什么实际问题，以及怎么把零散的 AI 工具使用变成系统化的团队能力。不写 prompt 教程，不绑定特定产品，讲工作流设计和落地路径。"
series_reading_hint: "先读 01 建立闭环全貌，再按你当前最关心的方向选择知识管理线或开发工作流线。"
---

## 这组文章要解决什么

很多团队用 AI 的方式是这样的：个别开发者自己用 ChatGPT 问问题、用 Copilot 补补代码，效率确实提升了一些，但这些提升停留在个人层面，没有变成团队能力。

与此同时，团队层面的问题依然存在：

- 策划反复问同一个问题，开发反复回答
- 新人入职，老人口口相传，文档永远不全
- 竞品分析、策划文档看完就忘，没有系统沉淀
- 代码里的业务逻辑只有写的人懂，其他人查不到

这组文章不是在讲"AI 能做什么"，而是在讲**怎么把 AI 工具组织成一个系统，让团队的知识能沉淀、能检索、能回答、能闭环**。

## 先给一句总判断

**AI 赋能团队的核心不是某个工具，而是一个闭环：Dify 暴露知识缺口，LKB 填补缺口，Wiki 沉淀知识，Dify 变得越来越能答。**

## 文章列表

### 知识管理线（01-04）

从闭环总论开始，逐步展开每个环节的工程化方案。

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [团队知识管理的闭环：从"反复被问"到"AI 能答"]({{< relref "ai-empowerment/ai-empowerment-01-team-knowledge-closed-loop.md" >}}) | 知识散落在脑子里和聊天记录里，怎么系统化？ |
| 02 | [Dify + Ollama：20 人团队的内网 AI 问答层]({{< relref "ai-empowerment/ai-empowerment-02-dify-team-qa.md" >}}) | 怎么让全团队都能从文档里获得 AI 回答？ |
| 03 | [LKB：用 AI 消化原始资料生成结构化知识]({{< relref "ai-empowerment/ai-empowerment-03-lkb-knowledge-engine.md" >}}) | 策划文档和竞品资料怎么变成高质量知识文档？ |
| 04 | [知识缺口发现与回流：让系统越用越好]({{< relref "ai-empowerment/ai-empowerment-04-knowledge-gap-feedback.md" >}}) | Dify 答不了的问题怎么变成下次能答的文档？ |

### AI 上下文线（05-06）

让 AI 理解你的项目和业务规则。

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 05 | [CLAUDE.md：让 AI 理解你的项目]({{< relref "ai-empowerment/ai-empowerment-05-claude-md-project-context.md" >}}) | AI 对你的项目一无所知，怎么给它上下文？ |
| 06 | [Skill 系统：给 AI 注入领域规则]({{< relref "ai-empowerment/ai-empowerment-06-skill-domain-knowledge.md" >}}) | 通用 AI 不懂你的配置表约定和代码生成规范，怎么办？ |

### 开发工作流线（07）

AI 在游戏开发跨层联动中的实际角色。

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 07 | [AI 辅助开发工作流：从协议到 UI 的跨层联动]({{< relref "ai-empowerment/ai-empowerment-07-dev-workflow-integration.md" >}}) | 游戏开发涉及 5+ 层，AI 在每一层该做什么、不该做什么？ |

## 推荐阅读顺序

**如果你是技术负责人**，先读 01 建立闭环全貌，再读 02（Dify 落地）和 04（缺口回流）了解团队级方案。

**如果你是开发者**，先读 01 了解整体，然后跳到 05（CLAUDE.md）和 06（Skill）——这是你每天都会用到的。

**如果你正在推动团队知识沉淀**，按 01 → 02 → 03 → 04 顺序读完知识管理线，这是一条完整的落地路径。

## 与其他文章的关系

[游戏工程团队引入 AI 的判断框架]({{< relref "essays/ai-integration-judgment-framework.md" >}})是决策层——帮你判断一个场景该不该用 AI。

本系列是实践层——确定要用之后，具体怎么在团队里落地。

[长线运营 · AI 集成]({{< relref "live-ops-engineering/liveops-ai-series-index.md" >}})是场景层——聚焦运营期的六大高价值场景。
