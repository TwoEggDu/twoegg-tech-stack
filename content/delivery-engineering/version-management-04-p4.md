---
title: "版本管理 04｜P4 的工作模型：Depot、Workspace、Changelist 与 Shelf"
slug: "version-management-04-p4"
date: "2026-10-29"
description: "从仓库路径和工作区映射进入待提交变更、搁置与正式提交，理解 P4 中修改的组织方式。当前为提纲。"
draft: false
content_stage: "outline"
weight: 3964
tags: ["版本管理", "P4", "Perforce"]
type: "reading-series"
topic_key: "delivery"
series: "版本管理"
primary_series: "version-management"
series_role: "article"
series_order: 4
reading_title: "P4 的工作模型"
---

> 本篇为提纲，正文待写；下列示例与实验尚未执行。

**核心问题：一组修改在 P4 中怎样被组织、交接和正式提交？**

## 同一份仓库内容，为什么会出现在不同工作目录

- 从一份资源在仓库中的路径、本地路径和取得的修订进入，解释 Depot 与 Workspace 的关系。
- 区分工作区映射、磁盘内容和服务器记录的同步状态。

## 打开修改与 Changelist 怎样关联

- 解释 open for edit、默认及编号 Changelist、待提交状态和已提交状态。
- 引出磁盘改动与工具记录不一致时的 reconcile，明确适用工作区选项和前置条件。

## Shelf 怎样支持提交前交接

- 对照工作区文件、搁置内容与仓库已提交内容，解释 shelve、unshelve 和 submit 的不同目的。
- 核对搁置后继续编辑时各份内容如何变化，不将“已经上传 Shelf”视为正式版本已更新。

## 将模型落到资源制作与客户端接入

- 用资源原稿与引擎资产的修改组织说明工作区及任务范围；不预设两者必须放进同一 Depot。
- 引出 Streams、集成和文件锁；具体分支与锁语义归第 06、07 篇。

## 拟验证：搁置之后继续编辑，再由另一工作区取得

- 在自建测试服务与两个工作区中记录映射、同步状态、Changelist、Shelf 和提交后的内容。
- 分别核对服务端、CLI 与图形客户端版本、权限和配置；没有可用环境时保留未验证状态。

<!-- DATA-TODO: 准备自建 P4 测试服务，记录服务端/客户端版本、工作区选项、权限和 Shelf/提交内容；当前未执行，不使用生产 Depot 做实验。 -->
