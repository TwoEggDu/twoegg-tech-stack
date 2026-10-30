---
title: "版本管理：从 Git、SVN、P4 到团队协作"
slug: "version-management-series-index"
date: "2026-10-29"
description: "从工作区、修改与历史出发，理解 Git、SVN、P4，再进入任务提交、分支合并、资源协作、审查和构建基线。前四篇已有正文，三种工具的隔离文本实验已完成，其余六篇为提纲。"
draft: false
featured: false
weight: 3960
tags: ["版本管理", "Git", "SVN", "P4", "Index"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
series_id: "version-management"
series_title: "版本管理"
series_role: "index"
series_order: 0
series_nav_order: 24
series_entry: true
series_audience: ["客户端与工具链工程师", "参与资源与配置提交的美术和策划", "版本与技术负责人"]
series_level: "基础到工程实践"
series_best_for: "希望理解 Git、SVN、P4 的工作模型，并解决任务提交、多人修改和版本交接中的问题。"
series_summary: "先建立工作区、变更与历史的共同模型，再分别理解三种工具，最后讨论游戏项目中的协作与交付。前四篇已有正文，三种工具的隔离文本实验已完成，其余六篇为提纲。"
series_intro: "一份本地修改，怎样成为团队可以审查、集成、构建和交付的版本？沿这条问题链理解版本控制机制与团队协作。"
series_reading_hint: "先读共同模型，再选择正在使用的工具；遇到具体协作问题时进入后半部分。共同模型与三种工具篇已有正文，后续工程章节待写。"
reading_title: "版本管理"
reading_lead: "从一份本地修改，走到团队共同使用的版本。"
reading_intro: "代码、资源和配置分别由不同的人修改，最终需要组成同一份可交接的内容。先理解修改怎样被选择、保存和共享，再讨论合并、审查与构建。"
reading_route: ["建立共同模型", "理解 Git、SVN、P4", "组织修改与版本交接"]
reading_status: "第 01～04 篇已有正文，其余 6 篇为提纲；Git、SVN、P4 隔离文本实验分别通过 23、19、19 项检查，引擎验证未执行。"
reading_groups:
  - title: "共同模型与三种工具"
    intro: "先认识工作区、修改与历史，再分别理解 Git、SVN、P4 的对象和状态变化。"
    first: 1
    last: 4
  - title: "提交、合并、资源与审查"
    intro: "从一个任务的修改范围进入多人协作，讨论依赖、冲突、锁与审查对象。"
    first: 5
    last: 8
  - title: "构建基线与工程选择"
    intro: "将仓库中的版本接到构建输入，再按项目约束讨论工具选择与迁移。"
    first: 9
    last: 10
---

同一个工程目录里，可能同时放着尚未完成的新功能、已经修好的缺陷，以及刚从美术那里接入的资源。现在只允许交付其中一个任务：应该提交哪些文件，怎样保留其他修改，别人又怎样拿到同一份内容？

本专栏从这类日常问题出发。先讲清工作区、变更集合、历史和共享基线，再分别理解 Git、SVN、P4，最后回到游戏团队中的代码、资源和配置协作。

**第 01～04 篇已有正文，其余 6 篇为提纲。** 可以先读共同模型，再选择 Git、SVN 或 P4，沿命令行实验理解各工具的状态变化；后续工程场景与引擎验证仍待补充。

## 系列目录 {#chapters}

{{< reading-series-chapters >}}

## 怎样选择阅读路径 {#routes}

- **第一次系统理解版本控制**：从[第 01 篇共同模型]({{< relref "delivery-engineering/version-management-01-model.md" >}})开始，再进入正在使用的工具，最后读第 05 篇任务提交。
- **已经使用 Git、SVN 或 P4**：分别进入[Git]({{< relref "delivery-engineering/version-management-02-git.md" >}})、[SVN]({{< relref "delivery-engineering/version-management-03-svn.md" >}})或 [P4]({{< relref "delivery-engineering/version-management-04-p4.md" >}})，对照自己的操作补齐状态模型；其余工具按需比较。
- **提交经常夹带修改或遗漏依赖**：从[任务提交]({{< relref "delivery-engineering/version-management-05-task-submission.md" >}})进入，再看[资源协作]({{< relref "delivery-engineering/version-management-07-assets.md" >}})与[变更审查]({{< relref "delivery-engineering/version-management-08-review.md" >}})。
- **需要多人并行开发或维护多个版本**：先看[分支与合并机制]({{< relref "delivery-engineering/version-management-06-branching-and-merging.md" >}})，再接到现有的[交付分支策略]({{< relref "delivery-engineering/delivery-version-management-02-branching.md" >}})。
- **需要固定构建输入或评估工具迁移**：阅读[构建基线]({{< relref "delivery-engineering/version-management-09-build-baseline.md" >}})与[选型和迁移]({{< relref "delivery-engineering/version-management-10-selection-and-migration.md" >}})，并按需回看工具模型。

## 用一个小型工程贯穿问题 {#example}

当前已完成三种工具的文本子集实验：Git 用本地裸仓库与双工作区检查暂存、提交和取得；SVN 用两个文件和工作副本检查混合修订；P4 用回环测试服务与双工作区检查 Shelf、普通提交与同步。后续再扩展自建教学工程，加入脚本、Prefab 与 `.meta`、配置及不适合文本合并的资源，研究依赖、冲突和审查。

每次实验只改变一个关键条件，记录操作前后的文件、工具状态与历史。教学场景、官方说明、实验结果和真实项目经历分别表达；当前三种工具的文本实验已完成，P4 测试服务已停止；完整引擎示例工程尚未创建。

## 与版本发布怎样衔接 {#related}

本专栏重点解释版本控制和协作机制。需要继续组织发布节奏时，可进入[版本与分支管理]({{< relref "delivery-engineering/delivery-version-management-series-index.md" >}})，阅读版本号、发布分支、内容冻结和环境配置。

想看这些问题如何出现在实际工作里，可以阅读[多个版本同时开发，怎样避免漏合修复和发错内容？]({{< relref "engineering-journey/production-parallel-versions.md" >}})。资源生产与交接的上下文见[游戏美术生产与客户端协作]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})，构建与自动化继续进入 [CI/CD 流水线]({{< relref "delivery-engineering/delivery-cicd-pipeline-series-index.md" >}})。
