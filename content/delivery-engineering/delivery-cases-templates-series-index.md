---
date: "2026-04-14"
title: "案例与模板系列索引｜交付的实战参考"
description: "给交付工程专栏 V19 补一个系统入口：事故案例集、交付检查清单、配置模板集、成熟度评估表——把 19 卷的知识转化为可直接使用的工具。"
slug: "delivery-cases-templates-series-index"
weight: 1800
featured: false
tags:
  - "Delivery Engineering"
  - "Case Study"
  - "Template"
  - "Checklist"
  - "Index"
series: "案例与模板"
series_id: "delivery-cases-templates"
series_role: "index"
series_order: 0
series_nav_order: 19
series_title: "案例与模板"
series_entry: true
series_audience:
  - "技术负责人/主程"
  - "版本/发布负责人"
  - "QA/测试负责人"
  - "DevOps / 构建工程师"
  - "项目经理/制作人"
series_level: "通用"
series_best_for: "当你需要可直接拿来用的清单、模板和参考案例，而不想从头翻 18 卷文章"
series_summary: "四篇文章把 V01-V18 的知识浓缩为实战工具：事故案例集、交付检查清单、配置模板集、成熟度评估表——交付工程专栏的完结篇。"
series_intro: "V01-V18 从概念到实践覆盖了交付工程的所有维度。V19 回到实战——把 18 卷的知识转化为四种可直接使用的工具：从事故中学习的案例集、每个交付节点的检查清单、CI/质量门/报告/复盘的标准模板、评估团队当前水平的成熟度矩阵。"
series_reading_hint: "V19 是独立的工具箱，每篇都可以直接使用。需要深入理解时再回溯对应的 Volume。"
delivery_layer: "case"
delivery_volume: "V19"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L3"
  - "L4"
  - "L5"
---

## 系列导读

交付工程专栏的前 18 卷讲了"为什么"和"怎么做"，V19 提供"拿来就用"的工具。四篇文章分别对应四种实战工具形态：

- **案例集**：从真实事故中提炼的教训，每个案例有症状-根因-修复-预防的完整闭环
- **检查清单**：从各 Volume 中提炼的关键检查项，覆盖交付链路的每个关键节点
- **模板集**：CI 管线、质量门、发布报告、事故复盘的标准模板，拿来填字段就能用
- **成熟度评估**：量化团队当前水平、规划改进路径的评估矩阵

## 文章列表

| 序号 | 标题 | 核心价值 |
|------|------|---------|
| 01 | [交付事故案例集]({{< relref "delivery-engineering/delivery-cases-templates-01-incident-cases.md" >}}) | 四类典型事故的症状、根因、修复、预防 |
| 02 | [交付检查清单]({{< relref "delivery-engineering/delivery-cases-templates-02-checklists.md" >}}) | 构建前、发布前、灰度观察、版本复盘四张清单 |
| 03 | [模板集]({{< relref "delivery-engineering/delivery-cases-templates-03-templates.md" >}}) | CI 管线、质量门、发布报告、事故复盘四套模板 |
| 04 | [成熟度评估与改进路线图]({{< relref "delivery-engineering/delivery-cases-templates-04-maturity-assessment.md" >}}) | 8 阶段 x 5 级别评估矩阵 + 改进路线图模板 |

## 阅读建议

- **L1 全景线**：四篇通读，建立交付工程的工具箱
- **L2 工程线**：重点读 01 案例集 + 02 检查清单，直接用于日常工作
- **L3 深度线**：重点读 03 模板集，用于搭建团队的交付标准
- **L4 管理线**：重点读 04 成熟度评估，用于规划团队的改进路径
- **L5 速查线**：四篇都是速查工具，需要时按需查阅

## 前置知识

- V19 的每篇文章都标注了对应的 Volume 来源，需要深入理解时可以回溯
- V01 交付总论——理解成熟度模型的完整定义（V19-04 的评估矩阵基于 V01-05）
- V15-V17——理解缺陷闭环、CI/CD、灰度上线（V19-01 的案例和 V19-02 的清单大量引用）
