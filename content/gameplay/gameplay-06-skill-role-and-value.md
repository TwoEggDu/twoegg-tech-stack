---
title: "GamePlay 06｜技能定位：一个技能要创造什么玩法价值（提纲）"
slug: "gameplay-06-skill-role-and-value"
date: "2026-10-23"
description: "章节提纲，正文待写：怎样先说明技能提供什么解法，再讨论它的强度和参数？"
draft: false
content_stage: "outline"
featured: false
tags: ["GamePlay", "游戏设计", "技能与战斗设计"]
series: "GamePlay：可玩性与游戏设计"
primary_series: "gameplay-design"
series_order: 6
weight: 60
topic_key: "gameplay"
---

> **当前状态：章节提纲，正文待写。** 真实游戏拆解与验证材料待补；下文设计情境是假设示例。

## 本篇的问题

怎样先说明技能提供什么解法，再讨论它的强度和参数？

## 从一个设计情境开始

设计一项可以穿过危险区域的冲刺能力，对照纯加速、闪避与远程清除障碍，列出它们各自解决的处境。

## 内容骨架

### 1. 从问题与处境定义技能

- 确定玩家面对的目标、限制和现有能力，说明新增技能要改变哪一类选择。

### 2. 写清技能的设计身份

- 以动作、目标、用途、优势条件和代价描述技能定位，避免只用伤害类型命名。

### 3. 比较替代方案与使用理由

- 列出什么时候使用、什么时候放弃，以及被其他行动替代的条件。

### 4. 让规则与反馈支持定位

- 检查距离、方向、目标限制和可读反馈是否能表达预期用途。

### 5. 形成最小设计规格

- 整理使用情境、规则、例外与验收问题；执行框架与 API 转交技术系列。

## 案例与验证材料

- 补一个真实技能在两类场景中的使用记录，确认规则并记录玩家可以选择的替代行动。
- 观察玩家如何描述技能用途、是否在预期情境使用，以及非预期用法是否具有设计价值。

<!-- DATA-TODO: 补一个真实技能在两类场景中的使用记录，确认规则并记录玩家可以选择的替代行动。 记录可确认的规则、材料来源、游玩条件、反例与未验证项。 -->

## 计划图表

技能定位卡；情境—行动—优势—代价矩阵。

## 沿全栈框架继续看技术实现

本篇聚焦设计与玩家体验，具体技术按需延伸：

- [全栈框架：技术阅读总入口]({{< relref "reading/frameworks.md" >}})
- [技能、效果与状态的概念边界]({{< relref "system-design/skill-system-01-boundaries.md" >}})
- [技能数据模型]({{< relref "system-design/skill-system-02-data-model.md" >}})

## 继续阅读

- [返回 GamePlay 阅读地图]({{< relref "gameplay/gameplay-series-index.md" >}})
- [上一篇：05]({{< relref "gameplay/gameplay-05-core-gameplay-loop.md" >}})
- [下一篇：07]({{< relref "gameplay/gameplay-07-skill-cost-constraints.md" >}})
