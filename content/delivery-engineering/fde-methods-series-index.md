---
title: "FDE 方法与实践：从现场问题到工程落地"
slug: "fde-methods-series-index"
date: "2026-10-28"
description: "先理解 FDE 的工作方式与责任，再研究需求发现、交付热点判断和能力沉淀，最后回到真实案例。基础篇与方法篇已有正文，案例补充后续推进。"
draft: false
featured: false
weight: 3950
tags: ["FDE", "交付工程", "工程方法", "Index"]
series: "FDE 方法与实践"
series_id: "fde-methods"
series_role: "index"
series_order: 0
series_nav_order: 23
series_title: "FDE 方法与实践"
series_entry: true
series_audience: ["研发工具与生产管线工程师", "交付工程师", "技术负责人"]
series_level: "基础与实践"
series_best_for: "希望深入实际工作，发现值得解决的问题，并把有效方案沉淀为团队持续使用的能力。"
series_summary: "从 FDE 基础进入内部交付场景的方法，再通过真实案例理解现场反馈怎样推动工程改进。基础篇与方法篇已有正文，包含三份实践记录模板。"
series_intro: "工程师怎样从现场困难走到可使用、可验证的解决方案？本专栏按基础理论、交付方法与真实案例逐步展开。"
series_reading_hint: "先读基础篇建立完整认知，再读方法篇练习需求发现、热点判断与能力沉淀；Missing 案例后续补充。"
---

[返回工程与交付]({{< relref "reading/delivery.md" >}})

## 这组文章准备回答什么

工具和产品进入实际工作以后，使用者还会遇到信息不足、交接受阻和需要反复手工处理的问题。工程师怎样了解这些困难，确定值得投入的目标，并让解决方案进入日常使用？

本专栏从 Forward Deployed Engineering（FDE）出发，先理解现场工作、工程落地与反馈之间的关系，再研究这些方法怎样用于游戏团队的交付改进。

**当前进度：基础篇与交付方法篇已有正文，Missing 案例的方法视角补充待推进。**

从[第一篇：FDE 基础——从现场问题到工程落地]({{< relref "delivery-engineering/fde-01-foundations.md" >}})开始。文章以通用检查系统的示意场景，解释现场信息怎样影响工程决策，以及如何判断实际采用和能力回流。

接着读[第二篇：将 FDE 方法用于交付工程]({{< relref "delivery-engineering/fde-02-delivery-method.md" >}})。文章串联现场走访、问题核实、热点判断、能力建设和使用复验，附三份可复制的记录模板。

## 阅读与写作顺序

| 顺序 | 主题 | 准备回答的问题 | 当前状态 |
| --- | --- | --- | --- |
| 1 | [FDE 基础：从现场问题到工程落地]({{< relref "delivery-engineering/fde-01-foundations.md" >}}) | 为什么需要深入现场？目标、约束、实施、采用与结果怎样相连，责任和适用边界在哪里？ | 已有正文；贯穿场景为教学推演 |
| 2 | [将 FDE 方法用于交付工程]({{< relref "delivery-engineering/fde-02-delivery-method.md" >}}) | 怎样发现需求、判断交付热点、选择解决方式，并把有效做法沉淀为可维护能力？ | 已有正文与三份模板；场景为教学推演 |
| 3 | Missing 工具链案例 | 现场走访和使用反馈怎样推动工具链逐步形成，已有结果能够说明什么？ | 原案例已有正文，方法视角的补充待推进 |

基础篇保留通用视角，第二篇再落到内部交付场景。方法篇的流程与模板是本站的实践框架，示意推演不等于已经在项目中实施或验证。

## 从已有实践了解问题背景

- [生产管线实践总览]({{< relref "engineering-journey/production-pipeline-overview.md" >}})：了解资源、配置、开发检查与跨工种交接怎样进入日常生产。
- [本地资源完整，为什么手机包里仍然缺内容？]({{< relref "engineering-journey/production-resource-completeness.md" >}})：阅读 Missing 检查、对象定位、依赖提交与使用反馈的既有案例。后续补充会继续核对走访中的具体观察与设计判断。

案例保留真实发生过程与证据范围；新的方法分析不自动成为历史项目已经执行过的流程。

## 与工程实现怎样连接

FDE 方法关注问题怎样被理解、解决并验证。[交付工程]({{< relref "delivery-engineering/_index.md" >}})中的资源管线、CI/CD 与版本专题继续解释具体工程机制。工具、规则、文档与协作流程如何选择，由问题及其约束决定。
