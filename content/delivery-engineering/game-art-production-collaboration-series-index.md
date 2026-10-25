---
title: "游戏美术生产与客户端协作"
slug: "game-art-production-collaboration-series-index"
date: "2026-10-25"
description: "沿原画、角色、绑定、动画、场景资产、地编、特效、TA 与客户端，认识美术内容从设计到验收的过程；第 01—02 篇已有正文，其余 10 篇为提纲。"
weight: 80
featured: false
draft: false
tags: ["美术生产", "客户端", "TA", "Index"]
series: "游戏美术生产与客户端协作"
series_id: "game-art-production-collaboration"
series_role: "index"
series_order: 0
series_nav_order: 21
series_title: "游戏美术生产与客户端协作"
series_entry: true
series_audience: ["客户端工程师", "美术与技术美术", "内容与工具链维护者"]
series_level: "入门到进阶"
series_best_for: "希望理解美术制作过程、看懂交付物并参与客户端联调。"
series_summary: "通过十二篇章节连接美术制作、交付与运行时表现；第 01—02 篇已有正文，其余 10 篇为提纲。"
series_intro: "原画建立视觉方案，角色与场景美术制作资产，绑定和动画让角色运动，地编组织空间，特效表达动作与反馈，TA 和客户端共同支持内容接入与迭代。"
series_reading_hint: "先读全貌，再沿角色或场景支线进入；已有交接问题时按问题跳转。"
type: "reading-series"
topic_key: "delivery"
reading_lead: "从一张设计稿，到游戏里能行动、能交互的角色与场景。"
reading_route: ["理解需求与制作", "连接资产与运行表现", "联合验收与修改"]
reading_status: "第 01—02 篇已有正文，其余 10 篇为提纲。"
reading_groups: [{"title": "从需求到视觉方案", "intro": "先认识各岗位的工作关系，再理解设计信息怎样交给后续制作。", "first": 1, "last": 2}, {"title": "让角色成形与运动", "intro": "沿模型、绑定与动画，理解外形、形变和动作怎样连接。", "first": 3, "last": 5}, {"title": "搭建场景与动态反馈", "intro": "制作可组合的场景资产，组织空间，并让特效传达动作与状态。", "first": 6, "last": 8}, {"title": "集成、协作与验收", "intro": "理解 TA 和客户端的工作，用统一示例汇合产物并检查结果。", "first": 9, "last": 12}]
---

本系列以实时 3D 游戏为主要语境，面向希望理解美术制作的客户端工程师，以及需要认识上下游的美术与 TA。先讲通用的制作过程和判断，再进入具体工具与引擎。

[从第 01 篇开始]({{< relref "delivery-engineering/game-art-production-01-overview.md" >}}) · [查看 12 篇目录](#chapters) · [按问题查找](#questions)

## 内容怎样汇合 {#workflow}

角色与场景沿不同支线制作，最终与特效一起进入游戏表现。TA、客户端及验证工作从早期参与；下图表示内容关系，实际制作会反复迭代。

{{< art-production-map >}}

## 系列目录 {#chapters}

{{< reading-series-chapters >}}

## 选择阅读路线 {#routes}

- **角色制作与接入**：[01 全貌]({{< relref "delivery-engineering/game-art-production-01-overview.md" >}}) → [02 原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}}) → [03 角色]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}}) → [04 绑定与蒙皮]({{< relref "delivery-engineering/game-art-production-04-rigging-skinning.md" >}}) → [05 动画]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}}) → [10 客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}}) → [11 综合示例]({{< relref "delivery-engineering/game-art-production-11-integrated-example.md" >}})。
- **场景制作与地编**：[01 全貌]({{< relref "delivery-engineering/game-art-production-01-overview.md" >}}) → [02 原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}}) → [06 场景资产]({{< relref "delivery-engineering/game-art-production-06-environment-assets.md" >}}) → [07 地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}}) → [09 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}}) → [10 客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}}) → [12 联合验收]({{< relref "delivery-engineering/game-art-production-12-joint-acceptance.md" >}})。
- **客户端协作快速入门**：[01 全貌]({{< relref "delivery-engineering/game-art-production-01-overview.md" >}}) → [09 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}}) → [10 客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}}) → [11 综合示例]({{< relref "delivery-engineering/game-art-production-11-integrated-example.md" >}}) → [12 联合验收]({{< relref "delivery-engineering/game-art-production-12-joint-acceptance.md" >}})。

首次系统阅读可沿 01—12 推进。这是学习顺序；实际制作中的方案讨论、预览和验证需要提前开展。

## 按当前问题进入 {#questions}

- **概念图还缺什么，建模才能继续？** [02 原画]({{< relref "delivery-engineering/game-art-production-02-concept-art.md" >}})、[03 角色]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})。
- **静态模型正常，为什么一抬手就变形？** [03 角色]({{< relref "delivery-engineering/game-art-production-03-character-assets.md" >}})、[04 绑定与蒙皮]({{< relref "delivery-engineering/game-art-production-04-rigging-skinning.md" >}})。
- **动作能播放，为什么滑步或打断不自然？** [05 动画]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})、[10 客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})。
- **场景建模、关卡设计和地编分别做什么？** [01 全貌]({{< relref "delivery-engineering/game-art-production-01-overview.md" >}})、[06 场景资产]({{< relref "delivery-engineering/game-art-production-06-environment-assets.md" >}})、[07 地编]({{< relref "delivery-engineering/game-art-production-07-level-art.md" >}})。
- **特效在预览中正常，接入后为什么错位或残留？** [08 特效]({{< relref "delivery-engineering/game-art-production-08-vfx.md" >}})、[10 客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})、[11 综合示例]({{< relref "delivery-engineering/game-art-production-11-integrated-example.md" >}})。
- **哪些问题需要 TA、资源制作者或客户端共同处理？** [09 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}})、[10 客户端]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})、[12 联合验收]({{< relref "delivery-engineering/game-art-production-12-joint-acceptance.md" >}})。
- **改了骨架、材质或动作时长，需要重查哪些内容？** [04 绑定与蒙皮]({{< relref "delivery-engineering/game-art-production-04-rigging-skinning.md" >}})、[05 动画]({{< relref "delivery-engineering/game-art-production-05-animation.md" >}})、[09 TA]({{< relref "delivery-engineering/game-art-production-09-technical-art.md" >}})、[11 综合示例]({{< relref "delivery-engineering/game-art-production-11-integrated-example.md" >}})、[12 联合验收]({{< relref "delivery-engineering/game-art-production-12-joint-acceptance.md" >}})。

## 用同一个小场景串起来 {#example}

拟制作的教学示例是：**一名持杖角色进入小型遗迹庭院，对目标释放一次短距离施法技能。**

- 角色线逐步增加设计稿、模型与材质、骨架、待机/移动/施法动作及武器挂点。
- 场景线逐步增加地面、墙、门等模块，以及路线、摆放、光照和摄像机观察条件。
- 表现线增加起手、释放与目标反馈，明确触发、跟随、取消和结束。
- 集成与验收将这些产物汇合，再追踪一次动作时长修改的影响。

各篇先解释局部过程，第 11 篇再汇合。当前示例尚未制作，页面中的拟验证内容不代表已有运行结果。

## 按需继续深入 {#related}

- [美术资源生产管线]({{< relref "delivery-engineering/delivery-content-pipeline-02-art-production.md" >}})。
- [资源规范与准入]({{< relref "delivery-engineering/delivery-content-pipeline-03-asset-standards.md" >}})。
- [TA 职责与协作接口]({{< relref "rendering/ta-role-boundaries-and-deliverables.md" >}})。
- [骨骼蒙皮与 Blend Shape]({{< relref "rendering/unity-rendering-03-skeletal-animation.md" >}})。
- [Unity Importer 的职责]({{< relref "engine-toolchain/unity-importer-what-does-it-do-and-why-source-file-is-not-just-a-file.md" >}})。
- [技能表现的交付标准]({{< relref "system-design/skill-system-28-artist-vfx-delivery-standard.md" >}})。
