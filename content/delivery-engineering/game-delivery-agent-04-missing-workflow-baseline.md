---
title: "04｜普通 Missing 怎样处理：一条不调用模型的闭环（提纲）"
slug: "game-delivery-agent-04-missing-workflow-baseline"
date: "2026-10-26"
description: "章节提纲，正文待写：以普通漏提交的示意场景设计定位、候选、确认补交与共享复检的固定流程；未执行实验。"
draft: false
featured: false
content_stage: "outline"
research_stage: "design-outline"
validation_status: "not-run"
weight: 3940
tags: ["交付工程", "Agent", "Missing", "研究提纲"]
series: "游戏交付 Agent 研究"
primary_series: "game-delivery-agent"
series_role: "article"
series_order: 40
type: "reading-series"
reading_title: "普通 Missing 怎样处理：一条不调用模型的闭环"
reading_summary: "在证据与规则明确时，固定流程能把问题处理到哪一步？"
---

> **文稿状态：章节提纲，正文待写。实验状态：未执行。** 本篇准备使用普通资源漏交的设计示例，不是新增的真实事故记录。拟议流程未实现、未执行，不代表已经取得无模型处理的验证结果。

## 本篇要回答的问题

如果检查报告能定位对象，本地又能取得匹配的依赖和提交状态，怎样用固定流程完成处理？本篇准备形成后续模型对照需要的共同基线。

## 1. 给出可以判定的样例条件

- 设计一份共享版本引用缺失、本地存在目标依赖的示意输入。
- 明确场景引用、目标资源及 meta 的对应关系、两边版本和检查范围。
- 将“确实漏交”作为需要工具证据支持的条件，不根据 Missing 报错直接成立。

## 2. 从检查报告进入对象定位

- 沿一次 CheckRun 建立或关联问题，保留原始报告入口。
- 使用 MissingFind 的拟议定位入口打开相应对象，并核对本地输入差异。
- 记录无法定位或版本不匹配时的分支，避免沿错误对象继续处理。

## 3. 从依赖预览生成精确候选

- 核对本地依赖、资源标识与 SVN 状态，说明每个候选文件为什么纳入。
- 研究资源、meta 与必要目录如何一起预览，以及无关本地修改如何排除。
- 设计候选确认前的状态复查，保留未确认与不可取得证据的状态。

## 4. 从确认补交进入共享版本复检

- 由使用者确认修正范围，按项目既有提交规则取得实际 revision。
- 关联 RepairAttempt 与检查任务，跟踪共享版本上的执行结果。
- 根据覆盖范围和检查回执判断原问题是否消除，区分本地通过、已提交与共享复检通过。

## 5. 哪些情况应该离开正常路径

- 列出依赖预览为空、本地与共享版本引用身份不同、工具运行失败等情况。
- 区分可以走固定异常分支的问题与需要进一步调查的问题。
- 将调查入口交给第 05 篇，不把每次异常都自动转换成模型任务。

## 计划图表与待补证据

- 计划图表：不调用模型的完整时序；每步输入、规则、执行者和回执表。
- 待补材料：可复现的最小场景、缺失依赖与 meta、独立检查副本、补交前后报告。
- 待设计验证：普通漏交完整路径、无关修改排除、未提交却本地通过、检查未运行。
- 验证状态：样本与流程尚未执行，不能宣称闭环成功率或人工时间变化。

## 本篇边界与后续阅读

- [第 05 篇：何时值得调用模型]({{< relref "delivery-engineering/game-delivery-agent-05-agent-decision-points.md" >}})承接正常流程无法决定下一步的情况。
- [第 08 篇：对照验证]({{< relref "delivery-engineering/game-delivery-agent-08-comparative-evaluation.md" >}})规划怎样比较基线与新增模型。
- [资源完整性实践]({{< relref "engineering-journey/production-resource-completeness.md" >}})提供已有工具背景，历史观察与本篇未来实验分开记录。

<!-- DATA-TODO: 准备可复现的漏提交样本及补交前后报告；固定输入、规则和关闭判据，保留失败样本。 -->
