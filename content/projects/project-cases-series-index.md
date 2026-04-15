---
date: "2026-04-15"
title: "项目案例系列索引｜不是功能列表，是约束、决策和结果的闭环"
description: "用真实项目案例证明交付能力：每篇围绕约束是什么、决策怎么做、结果怎么验证。"
slug: "project-cases-series-index"
weight: 1
featured: true
tags:
  - "Projects"
  - "Case Study"
  - "Index"
series: "项目案例"
series_id: "project-cases"
series_role: "index"
series_order: 0
series_entry: true
series_audience:
  - "技术负责人 / 主程"
  - "面试官（评估工程交付能力）"
  - "高级工程师（学习项目复盘方法）"
series_level: "进阶"
series_best_for: "当你想了解我在真实项目中怎么做判断、怎么解决问题、怎么推动落地"
series_summary: "每篇案例都不是在讲技术细节，而是在回答三个问题：约束是什么、选择了什么、为什么不选别的。"
series_intro: "技术深度可以通过知识文章证明，但交付能力只能通过项目案例证明。这组文章是我在不同项目中积累的真实经历，每篇都围绕一个核心矛盾展开：接手复杂项目的诊断方法论、线上事故的根因追踪、性能治理的分层策略、多团队协作的治理落地、工具平台化的边界判断。"
---

## 这组文章要解决什么

面试官问"你做过什么"时，功能列表没有说服力。他们真正想听的是：你遇到了什么约束、做了什么取舍、结果怎么样。

这组案例覆盖了六种不同类型的工程挑战，每篇都是从问题出发、经过决策过程、到结果验证的完整闭环。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [特效性能检查器案例]({{< relref "projects/vfx-checker-case.md" >}}) | 怎么把特效质量从人工 review 变成发布前自动门禁？ |
| 02 | [从项目内工具到跨项目平台]({{< relref "projects/project-to-platform.md" >}}) | 工具链从项目脚本到平台能力，三个阶段各有什么关键转变？ |
| 03 | [复杂 Unity 项目接手方法论]({{< relref "projects/case-complex-project-intake.md" >}}) | 接手一个跑了两年的项目，前两周怎么诊断而不是急着改？ |
| 04 | [热更新上线事故复盘]({{< relref "projects/case-hotupdate-production-incident.md" >}}) | 20% 用户卡加载，从 CDN 缓存到 AB 依赖断裂的根因是什么？ |
| 05 | [性能优化项目]({{< relref "projects/case-performance-optimization.md" >}}) | 从 22fps 到 45fps，分层治理的逻辑是什么？ |
| 06 | [多团队工程治理]({{< relref "projects/case-multi-team-governance.md" >}}) | 从 1 个团队到 3 个团队并行，治理怎么从口头约定变成自动化？ |
| 07 | [从脚本到平台的边界判断]({{< relref "projects/case-script-to-platform.md" >}}) | 什么时候该把项目脚本抽成跨项目平台？什么时候不该？ |
| 08 | [我的工程边界]({{< relref "projects/case-my-engineering-boundary.md" >}}) | 能独立交付什么、需要协作什么、不做什么？ |
| 09 | [工程文化不是口号]({{< relref "projects/case-engineering-culture-three-things.md" >}}) | CI 红灯当天修、review 48h 响应、崩溃率周报——怎么落地？ |
| 10 | [一次重大技术选型的完整决策过程]({{< relref "projects/case-tech-selection-decision.md" >}}) | 面对多个可行方案，怎么排除、权衡、说服团队？ |
| 11 | [跨版本大规模重构/迁移]({{< relref "projects/case-large-scale-migration.md" >}}) | 怎么把大变更拆成可控、可回滚、可验证的小步骤？ |
| 12 | [从零搭建交付管线]({{< relref "projects/case-build-system-from-zero.md" >}}) | 从零起步到 5 个项目复用，管线怎么从 0→1→N？ |
| 13 | [资产管线设计]({{< relref "projects/case-asset-pipeline-design.md" >}}) | 从资源导入到热更发布的完整链路怎么设计和演进？ |
| 14 | [工具链与业务团队的协作]({{< relref "projects/case-toolchain-business-collaboration.md" >}}) | 怎么发现堵点、收集需求、确保基建工作真的有人用？ |

## 阅读建议

- **面试官推荐**：03（诊断方法论）、04（事故复盘）、10（热更选型决策）、12（交付管线 0→N）
- **工程师参考**：06（多团队治理）、07（平台边界）、13（资产管线设计）
- **团队协作**：09（工程文化落地）、14（工具链与业务团队协作）
- **风险管理**：11（大规模迁移）
