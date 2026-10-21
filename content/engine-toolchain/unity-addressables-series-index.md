---
date: "2026-10-21"
title: "Addressables 资源管理与交付"
description: "按运行时加载、Catalog、引用计数、资源构建和框架边界，系统阅读 Addressables 的资源管理与交付链路。"
slug: "unity-addressables-series-index"
weight: 51
featured: false
tags: ["Unity", "Addressables", "Index"]
series: "Addressables 资源管理与交付"
series_id: "addressables"
series_role: "index"
series_order: 0
series_nav_order: 51
series_title: "Addressables 资源管理与交付"
series_entry: true
series_audience: ["Unity 客户端", "资源与构建工具链"]
series_level: "进阶"
series_best_for: "希望理解 Addressables 的内部职责，或定位加载、引用释放与内容更新问题。"
series_summary: "按运行时加载、Catalog、引用计数、资源构建和框架边界，系统阅读 Addressables 的资源管理与交付链路。"
series_intro: "按运行时加载、Catalog、引用计数、资源构建和框架边界，系统阅读 Addressables 的资源管理与交付链路。"
series_reading_hint: "先按推荐顺序阅读主线，再按问题进入案例或两个系统的对比。"
---

Addressables 在 Unity 资源包之上组织资源定位、异步加载、依赖和生命周期管理。这里把运行时、Catalog、引用计数、构建和框架边界收成一条阅读路径，并连接相关的内存与更新案例。

## 从哪里开始

先理解 Addressables 与 AssetBundle 的层次关系，再沿下面五篇主线阅读。带着内存问题进入时，可以先看引用计数；带着内容更新问题进入时，可以先看 Catalog 和构建产物。

前置入口：[Addressables 与 AssetBundle 的关系]({{< relref "engine-toolchain/unity-addressables-and-assetbundle-format-vs-management-layer.md" >}})。

| 顺序 | 文章 | 阅读重点 |
|---|---|---|
| 1 | [运行时加载链路]({{< relref "engine-toolchain/unity-addressables-runtime-internals-loadassetasync-to-bundle-loaded.md" >}}) | 认识定位、调度、Provider 和句柄在加载过程中的分工。 |
| 2 | [Catalog 与内容更新]({{< relref "engine-toolchain/unity-addressables-catalog-encoding-loading-cost-update.md" >}}) | 理解资源目录的数据组织、加载成本和更新路径。 |
| 3 | [引用计数与生命周期]({{< relref "engine-toolchain/unity-addressables-refcount-lifecycle-release-harder-than-load.md" >}}) | 理解句柄、操作缓存和资源释放之间的关系。 |
| 4 | [Group 与构建产物]({{< relref "engine-toolchain/unity-addressables-build-time-group-schema-to-bundle-output.md" >}}) | 从 Group、Schema 和构建脚本追到 Bundle、Catalog 与内容更新状态。 |
| 5 | [框架边界与项目职责]({{< relref "engine-toolchain/unity-addressables-boundaries-what-projects-must-build.md" >}}) | 识别下载、发布和多环境管理中需要项目继续补齐的环节。 |

## 按问题查找

| 当前问题 | 阅读入口 |
|---|---|
| key 怎样定位到资源，加载由谁调度？ | [运行时加载链路]({{< relref "engine-toolchain/unity-addressables-runtime-internals-loadassetasync-to-bundle-loaded.md" >}}) |
| Catalog 保存什么，更新时需要观察什么？ | [Catalog 与内容更新]({{< relref "engine-toolchain/unity-addressables-catalog-encoding-loading-cost-update.md" >}}) |
| 资源由谁持有，何时释放，为什么内存不下降？ | [引用计数与生命周期]({{< relref "engine-toolchain/unity-addressables-refcount-lifecycle-release-harder-than-load.md" >}}) |
| Group 配置怎样变成 Bundle 和索引产物？ | [Group 与构建产物]({{< relref "engine-toolchain/unity-addressables-build-time-group-schema-to-bundle-output.md" >}}) |
| 资源框架接入以后，项目还要补什么？ | [框架边界与项目职责]({{< relref "engine-toolchain/unity-addressables-boundaries-what-projects-must-build.md" >}}) |

## 相关故障与交付案例

| 顺序 | 文章 | 阅读重点 |
|---|---|---|
| 1 | [句柄泄漏与内存增长]({{< relref "problem-solving/addressables-yooasset-handle-leak-memory-growth.md" >}}) | 以 Addressables 为主，追踪引用未释放的问题。 |
| 2 | [清单与 Bundle 不一致]({{< relref "problem-solving/addressables-yooasset-half-update-catalog-manifest-bundle-mismatch.md" >}}) | 区分索引已经更新和所需文件已经就绪。 |
| 3 | [下载中断与恢复]({{< relref "problem-solving/addressables-yooasset-download-interruption-resume-recovery.md" >}}) | 下载失败后，从续传、重试和校验路径定位问题。 |

## 构建前置与其他入口

- [BuildPipeline、SBP 与 Addressables Build Script 的分层]({{< relref "engine-toolchain/unity-buildpipeline-sbp-addressables-build-script-layering.md" >}})：区分引擎构建能力、可编程管线与框架构建脚本。
- [Group 划分与 Bundle 布局]({{< relref "engine-toolchain/unity-addressables-yooasset-grouping-bundle-layout-best-practices.md" >}})：把配置选择接回依赖和资源包粒度。
- [YooAsset 首页]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}})：系统阅读另一套框架。
- [Addressables 与 YooAsset 对比首页]({{< relref "engine-toolchain/unity-addressables-yooasset-comparison-series-index.md" >}})：沿共同问题对照两套系统。
