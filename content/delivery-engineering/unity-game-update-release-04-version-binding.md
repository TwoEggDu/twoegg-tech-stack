---
title: "04｜版本如何绑定：让每个客户端拿到兼容的资源（提纲）"
slug: "unity-game-update-release-04-version-binding"
date: "2026-10-23"
description: "提纲，正文待写：客户端应该选择哪一套代码、数据和资源，它们为什么兼容？"
draft: false
content_stage: "outline"
weight: 69
tags: ["Unity", "YooAsset", "交付工程", "热更新"]
series: "Unity 游戏更新与发布"
primary_series: "unity-game-update-release"
series_order: 4
type: "reading-series"
reading_title: "版本如何绑定：让每个客户端拿到兼容的资源"
reading_summary: "客户端应该选择哪一套代码、数据和资源，它们为什么兼容？"
---

> 贯穿案例：客户端 100 和 101 使用不同资源组合，100 仍能单独接受兼容补丁。

## 1. 先区分几种版本身份

- 区分源码版本、CI 构建号、客户端整包号和各资源包版本。
- 说明编号用于定位，兼容性需要独立依据，不能靠数字相同推断。

## 2. 客户端怎样携带自己的身份

- 设计首包中稳定可读的 BuildInfo，保留平台与版本检查入口。
- 说明首包配置不能依赖尚未下载的业务热更代码。

## 3. 两层清单各自负责什么

- 业务版本清单负责选择客户端策略与资源组合。
- YooAsset Manifest 负责选定 Package 内容的索引、依赖与文件身份。

## 4. 一份策略怎样绑定多个资源版本

- 将整包号映射到资源地址与 Res、HotCode、Data 的版本快照。
- 明确这三个是系列中的逻辑内容类别，具体 Package 名称由项目实现决定。

## 5. 强更下限与逐包强更目标

- 分别建模可继续运行的客户端范围和指定客户端的升级目标。
- 解释最新可用版本与最低兼容版本是不同语义；项目复用 LatestPackageNum 时必须明确其实际判定。

## 6. 旧客户端怎样继续获得更新

- 示例比较 100 与 101 的策略及独立热更。
- 讨论策略缺失、目标不存在和不兼容内容的拒绝规则，不默认回退到最新资源。

## 延伸阅读

- [YooAsset 资源管理与交付]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}})：回查 Package、Manifest、下载缓存与构建的框架机制，再区分项目自己的版本选择职责。

## 计划配图与表格

版本身份关系图；客户端到内容快照的映射表；清单职责图。

## 成稿前需要补齐

成稿前固定清单字段与示例契约；项目自定义强更规则与 YooAsset 自身能力分别说明。
