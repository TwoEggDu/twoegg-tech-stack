---
title: "02｜一次报错怎样追到复检：版本、问题与修正记录（提纲）"
slug: "game-delivery-agent-02-check-finding-repair-records"
date: "2026-10-26"
description: "章节提纲，正文待写：研究检查、问题、修正与版本的关联，设计状态与证据记录；新增服务未实现，验证未执行。"
draft: false
featured: false
content_stage: "outline"
research_stage: "design-outline"
validation_status: "not-run"
weight: 3920
tags: ["交付工程", "Agent", "Missing", "研究提纲"]
series: "游戏交付 Agent 研究"
primary_series: "game-delivery-agent"
series_role: "article"
series_order: 20
type: "reading-series"
reading_title: "一次报错怎样追到复检：版本、问题与修正记录"
reading_summary: "怎样把同一个问题、几次处理和各版本检查关联起来？"
---

> **文稿状态：章节提纲，正文待写。实验状态：未执行。** CheckRun、Finding、RepairAttempt 是拟议的记录名称；尚未实现存储或状态管理，也未执行对应验证。

## 本篇要回答的问题

怎样知道某个 Missing 在哪个版本被发现、经历哪些修正，以及哪次检查支持它已经消除？本篇准备定义记录关系与正常状态迁移，作为后续工具接入的共同约定。

## 1. 一次检查与一个问题分别是什么

- 将一次执行的检查范围、输入和结果，与可重复出现的具体问题分开。
- 研究同一 Missing 在连续版本中出现时怎样关联，不把每次提醒都当成新问题。
- 区分问题身份与观察版本，记录无法可靠匹配对象时的处理办法。

## 2. 检查输入怎样形成可回查的快照

- 计划记录项目、分支、实际 revision、工作副本状态、工具与规则版本、执行环境和范围。
- 研究混合版本、外部依赖和本地未提交修改怎样影响输入说明。
- 对照真实工具能取得的字段，标出缺项与后续补充要求，不默认拥有完整快照。

## 3. 三类记录怎样关联

- CheckRun：拟保存一次检查的输入、执行状态、结果完整性和原始报告。
- Finding：拟保存问题位置、规则、各次观察和当前处理状态。
- RepairAttempt：拟保存修正依据、变更范围、本地预检、正式提交与共享版本复检。
- 研究一次修正影响多个问题、同一问题经历多次修正的关系。

## 4. 正常路径怎样推进状态

- 从发现、定位、准备修正到等待提交与共享复检，列出每次推进所需的回执。
- 将执行成功且发现问题、执行成功且通过、工具失败和未执行分别表达。
- 明确模型建议属于待处理信息，正式提交和检查事实由相应工具确认。

## 5. 向后续章节提供哪些契约

- 为第 03 篇准备事件与结果的最小字段草案，并标出可选字段及未知状态。
- 为第 04 篇准备普通漏交的记录时间线。
- 将重复事件、旧结果晚到、恢复与关闭判据的异常处理留给第 07 篇。

## 计划图表与待补证据

- 计划图表：三类记录关系图、修正与版本的时间线、正常状态迁移表。
- 待补材料：一次真实检查的输入说明、完整成功与失败报告、修正提交关联方式。
- 待设计验证：连续报错、一次修正多个问题、本地通过但未提交、复检没有实际执行。
- 验证状态：仅列研究场景，存储、关联与状态迁移均未实现或运行。

## 本篇边界与后续阅读

- [第 03 篇：工具要交出什么]({{< relref "delivery-engineering/game-delivery-agent-03-existing-tool-contracts.md" >}})把记录需求映射到实际工具。
- [第 07 篇：修正怎样才算完成]({{< relref "delivery-engineering/game-delivery-agent-07-verification-and-recovery.md" >}})处理异步与异常，不在本篇重复展开。
- [Evidence Contract]({{< relref "ai-empowerment/agent-engineering-18-evidence-contract.md" >}})提供事实与结论关联的通用背景。

<!-- DATA-TODO: 核对实际检查输入与报告格式，取得成功、发现问题、工具失败三类脱敏样本；验证问题身份与修正关联。 -->
