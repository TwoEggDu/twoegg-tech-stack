---
title: "12｜联合验收：画面、玩法和性能怎样一起检查（提纲）"
slug: "game-art-production-12-joint-acceptance"
date: "2026-10-25"
description: "章节提纲，正文待写：固定观察条件，区分问题与原因，确定修复责任、影响范围和重新验收的内容。"
draft: false
featured: false
content_stage: "outline"
weight: 92
tags: ["美术生产", "客户端", "协作"]
series: "游戏美术生产与客户端协作"
primary_series: "game-art-production-collaboration"
series_order: 12
type: "reading-series"
reading_title: "联合验收：画面、玩法和性能怎样一起检查"
reading_summary: "固定观察条件，区分问题与原因，确定修复责任、影响范围和重新验收的内容。"
---

> **章节提纲 · 正文待写。** 本页列出后续正文的内容安排；拟制作的素材、示例与验证尚未完成。

## 本篇要回答的问题

多个岗位怎样针对同一份内容判断完成，并把问题送回正确位置？

## 内容提纲

### 1. 分别确定验收目标

定义视觉、功能、体验和性能各自的观察目标，指定参与人员和最终决定的归属。

### 2. 固定可对照的观察条件

固定资源/配置/程序版本、镜头、光照、质量档、场景规模与设备，使观察可对照。

### 3. 从现象逐步收集证据

按“现象 → 复现 → 候选原因 → 证据”组织问题，区分制作、导入、组装、驱动和运行环境等层次。

### 4. 按层次定位候选问题

用角色形变、材质观感、特效残留和场景通行等候选问题展示排查方法；未经实验的根因只作假设。

### 5. 讨论收益、成本与风险

讨论视觉收益、制作成本、运行成本和改动风险；预算及取舍基于具体项目，不给统一面数或耗时阈值。

### 6. 记录修复与重新验收

明确修复、复查、依赖变更通知与接受例外的记录；把重复问题反馈给制作方法和工具。

## 拟制作的图与示例

联合验收表、问题记录样例、修改影响检查表；读者能够区分“已观察到异常”和“已证明原因”。

贯穿教学示例是一名持杖角色进入遗迹庭院并施法；本篇先用相关的局部产物解释问题。示例制作后再补过程图与实际观察记录。

## 工具与文件传递（拟展开）

- **工具与制作**：说明制作软件预览、引擎观察、Profiler 与 Frame Debugger 等工具各自能提供什么证据。
- **原稿与导出**：沿一个候选异常区分原稿、导出物、导入设置、引用与运行状态，展示定位信息如何关联。
- **进入游戏与反馈**：修正后回到同一观察条件复查；工具截图、静态检查与设备运行结果分别登记。

## 本篇边界

本篇建立跨岗位验收方法；Profiler 等工具的完整使用、性能方法论和版本发布分别链接已有系列。

## 前置与延伸阅读

先按需阅读：[10｜客户端：怎样把美术资产接成运行中的表现]({{< relref "delivery-engineering/game-art-production-10-client-integration.md" >}})、[11｜综合示例：角色进入关卡并释放一次技能]({{< relref "delivery-engineering/game-art-production-11-integrated-example.md" >}})。

- [资源规范与准入]({{< relref "delivery-engineering/delivery-content-pipeline-03-asset-standards.md" >}})：按需补充相关机制与协作背景。

[回到系列首页与完整目录]({{< relref "delivery-engineering/game-art-production-collaboration-series-index.md" >}})。
