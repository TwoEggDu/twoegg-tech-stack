---
date: "2026-04-14"
title: "CI/CD 管线系列索引｜把手动操作变成自动化管线"
description: "给交付工程专栏 V16 补一个系统入口：从管线本质出发，覆盖架构设计、构建自动化、质量门、部署自动化和工具选型六篇文章。"
slug: "delivery-cicd-pipeline-series-index"
weight: 1500
featured: false
tags:
  - "Delivery Engineering"
  - "CI/CD"
  - "Pipeline"
  - "Index"
series: "CI/CD 管线"
series_id: "delivery-cicd-pipeline"
series_role: "index"
series_order: 0
series_nav_order: 16
series_title: "CI/CD 管线"
series_entry: true
series_audience:
  - "客户端/引擎开发"
  - "技术负责人/主程"
  - "DevOps / 构建工程师"
series_level: "进阶到高级"
series_best_for: "当你想把手动构建和发布流程变成自动化管线、想搞清楚 CI/CD 在游戏项目中怎么落地、想对比 Jenkins / GitHub Actions / GitLab CI 的选型"
series_summary: "从 CI/CD 管线的五阶段模型（触发→构建→检查→部署→通知）到具体实践：管线架构设计、构建脚本与缓存、质量门自动化、多平台部署自动化、CI 工具选型。"
series_intro: "V15 讲了缺陷闭环——发现问题后怎么管理、怎么收敛。V16 回到自动化本身——怎么让构建、检查、部署这些重复工作由机器完成，把人从手动操作中解放出来。"
delivery_layer: "principle"
delivery_volume: "V16"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L3"
  - "L4"
---

## 系列导读

手动构建是交付效率的最大瓶颈——不是因为一次构建慢，而是因为每次构建都依赖一个人记住全部步骤、正确操作、不出差错。CI/CD 管线的本质是把"人脑中的操作流程"变成"机器执行的代码流程"，让每次提交都能得到快速、一致、可靠的反馈。

V14 讲了性能预算和质量基线，V15 讲了缺陷闭环。V16 把这些质量约束集成到自动化管线中——让编译检查、资源验证、性能基线、部署发布全部由管线驱动，不再依赖人工操作。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [CI/CD 管线的本质]({{< relref "delivery-engineering/delivery-cicd-pipeline-01-fundamentals.md" >}}) | CI 和 CD 的区别是什么？五阶段管线模型怎么理解？ |
| 02 | [管线架构设计]({{< relref "delivery-engineering/delivery-cicd-pipeline-02-architecture.md" >}}) | 单管线和多端并行怎么选？扇入扇出和失败隔离怎么做？ |
| 03 | [构建自动化]({{< relref "delivery-engineering/delivery-cicd-pipeline-03-build-automation.md" >}}) | 构建脚本怎么设计？环境怎么管理？缓存怎么加速？ |
| 04 | [质量门自动化]({{< relref "delivery-engineering/delivery-cicd-pipeline-04-quality-gates.md" >}}) | 编译、资源、Shader、性能——五类质量检查怎么接入 CI？ |
| 05 | [部署自动化]({{< relref "delivery-engineering/delivery-cicd-pipeline-05-deployment-automation.md" >}}) | fastlane、Gradle、微信 CLI——多平台部署怎么自动化？ |
| 06 | [CI 工具选型]({{< relref "delivery-engineering/delivery-cicd-pipeline-06-tool-selection.md" >}}) | Jenkins、GitHub Actions、GitLab CI 各适合什么场景？ |

## 与其他系列的关系

V13 验证与测试系列定义了"验证什么"，V14 性能与稳定性系列定义了"预算和基线"，V16 解决"怎么自动执行这些验证"。V16 是 V13 和 V14 的执行层——前者定义标准，后者提供管线。V17 灰度上线系列将讲管线产出的构建如何通过灰度策略逐步推送给用户。
