---
title: "版本管理 06｜分支与合并：并行开发之后，修改怎样汇合"
slug: "version-management-06-branching-and-merging"
date: "2026-10-29"
description: "从共同基线和修改传播理解分支与合并，再对照 Git、SVN、P4 的机制与验证边界。当前为提纲。"
draft: false
content_stage: "outline"
weight: 3966
tags: ["版本管理", "分支", "合并"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 6
reading_title: "分支与合并"
---

> 本篇为提纲，正文待写；下列示例与实验尚未执行。

**核心问题：两条工作线的修改，怎样汇合成正确的共同版本？**

## 为什么复制一份工程还不够

- 用线上修复与新功能同时推进的教学场景，提出来源、目标、共同基线与传播范围四个问题。
- 分清保存两份内容、保留关系和证明某个修复已经进入另一版本。

## 合并需要哪些历史信息

- 用三方合并的最小例子解释共同祖先、两边修改与结果；区分文本冲突和语义冲突。
- 单列删除与移动、配置变化和二进制资源等条件，不以工具没有报冲突作为功能正确的证据。

## 三种工具怎样表示分支与集成

- 对照 Git 的提交图与引用、SVN 的分支路径及 mergeinfo、P4 的 Streams 与集成记录。
- 解释 merge、rebase、选择传播部分修改等操作改变的对象；按具体版本核对，不强行一一对应命令。

## 一个修复怎样确认传播完成

- 核对原始修改、合并时适配及目标分支行为，记录遗漏、重复和依赖条件。
- 本篇维护机制；长期分支数量、发布节奏与冻结规则继续阅读[交付分支策略]({{< relref "delivery-engineering/delivery-version-management-02-branching.md" >}})。

## 拟验证：无文本冲突，仍然行为错误

- 构造两个分别能通过局部检查、合并后产生契约不一致的修改，检查工具结果与功能验证的差别。
- 再补一个显式文本冲突的对照，保留分支关系、合并记录和解决依据。

<!-- DATA-TODO: 补共同基线、修改传播、文本冲突与语义冲突的隔离实验；SVN mergeinfo 与 P4 集成行为须按选定版本核对。 -->
