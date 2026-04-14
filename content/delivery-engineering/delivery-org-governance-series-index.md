---
date: "2026-04-14"
title: "组织治理系列索引｜交付的人和流程维度"
description: "给交付工程专栏 V18 补一个系统入口：从角色职责、发布审批、多团队协作、工程文化到跨项目平台五篇文章，覆盖交付体系中'人'的维度。"
slug: "delivery-org-governance-series-index"
weight: 1700
featured: false
tags:
  - "Delivery Engineering"
  - "Organization"
  - "Governance"
  - "Index"
series: "组织治理"
series_id: "delivery-org-governance"
series_role: "index"
series_order: 0
series_nav_order: 18
series_title: "组织治理"
series_entry: true
series_audience:
  - "技术负责人/主程"
  - "版本/发布负责人"
  - "QA/测试负责人"
series_level: "进阶到高级"
series_best_for: "当你发现交付出问题不是因为工具不行而是因为流程不清、职责不明、团队协作卡顿、文化拖后腿"
series_summary: "从角色职责与 RACI 矩阵、发布审批与变更管理、多团队分支协作、工程文化建设到跨项目平台规模化——交付体系中'人和流程'的完整维度。"
series_intro: "V01-V17 讲了交付链路上的每个技术环节——从版本管理到 CI/CD 到灰度上线。V18 退一步，看交付链路背后的组织问题：谁负责什么、审批怎么走、多团队怎么协作、文化怎么建、工具怎么从项目级升到平台级。"
series_reading_hint: "L1 全景线通读五篇建立完整认知；L4 管理线重点读 01-03 理解核心机制"
delivery_layer: "principle"
delivery_volume: "V18"
delivery_reading_lines:
  - "L1"
  - "L4"
---

## 系列导读

交付工程不只是技术问题——工具再好，如果职责不清、审批卡顿、团队各自为战、文化鼓励甩锅，交付效率照样上不去。V01-V17 覆盖了交付链路的技术维度，V18 覆盖组织维度：角色定义、流程设计、协作机制、文化建设、规模化路径。

一个常见误区：团队觉得"我们缺一个好用的 CI 工具"，其实问题是"没人知道谁该审批、谁该通知、出了问题谁负责"。工具解决效率问题，组织治理解决协作问题。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [角色与职责]({{< relref "delivery-engineering/delivery-org-governance-01-roles.md" >}}) | 交付链路上有哪些角色？RACI 矩阵怎么用？职责边界怎么定？ |
| 02 | [发布审批流程设计]({{< relref "delivery-engineering/delivery-org-governance-02-release-approval.md" >}}) | 审批流程怎么设计？变更分级怎么做？紧急通道怎么走？ |
| 03 | [多团队协作]({{< relref "delivery-engineering/delivery-org-governance-03-multi-team.md" >}}) | 3+ 团队并行开发怎么管分支？发布列车怎么协调？ |
| 04 | [工程文化]({{< relref "delivery-engineering/delivery-org-governance-04-culture.md" >}}) | 质量文化、复盘文化、自动化文化——怎么建？ |
| 05 | [从项目工具到跨项目平台]({{< relref "delivery-engineering/delivery-org-governance-05-platform-scaling.md" >}}) | 交付体系怎么从单项目脚本升级到跨项目平台？ |

## 阅读建议

- **L1 全景线**：五篇通读，建立交付组织治理的完整认知
- **L4 管理线**：重点读 01 角色职责 + 02 审批流程 + 03 多团队协作，理解交付管理的核心机制

## 前置知识

- V01 交付总论——理解交付飞轮模型和成熟度框架（组织治理是飞轮的润滑剂）
- V04 版本管理——理解分支策略（多团队协作依赖分支设计）
- V16 CI/CD 管线——理解自动化管线（审批流程嵌入管线执行）
- V17 灰度上线——理解发布流程（审批是发布前的最后一道关卡）
