---
date: "2026-10-21"
title: "Addressables 与 YooAsset 对比与选型"
description: "连接 Addressables 与 YooAsset 的独立阅读首页，按运行时、构建产物和交付治理组织对比、选型及故障案例。"
slug: "unity-addressables-yooasset-comparison-series-index"
weight: 53
featured: false
tags: ["Unity", "Addressables", "YooAsset", "Comparison", "Index"]
series: "Addressables 与 YooAsset 对比与选型"
series_id: "addressables-yooasset-comparison"
series_role: "index"
series_order: 0
series_nav_order: 53
series_title: "Addressables 与 YooAsset 对比与选型"
series_entry: true
series_audience: ["Unity 客户端", "资源与构建工具链"]
series_level: "进阶"
series_best_for: "已经接触两套资源框架，需要对照设计、判断选型或梳理迁移与故障处理路径。"
series_summary: "连接 Addressables 与 YooAsset 的独立阅读首页，按运行时、构建产物和交付治理组织对比、选型及故障案例。"
series_intro: "连接 Addressables 与 YooAsset 的独立阅读首页，按运行时、构建产物和交付治理组织对比、选型及故障案例。"
series_reading_hint: "先按推荐顺序阅读主线，再按问题进入案例或两个系统的对比。"
---

比较 Addressables 和 YooAsset，需要把资源定位、加载、构建、版本和缓存放到相同的问题上，再结合项目的交付条件判断。这个入口连接两套系统各自的主线文章、同层对照、选型讨论和故障案例。

> 版本范围：YooAsset 独立主线已按 **3.0.6** 重审。下方早期选型、对比和故障案例仍沿各自旧基线，相关 API、默认值及性能判断尚需独立复核；阅读当前 YooAsset 实现请先进入 [YooAsset 首页]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}})。

## 先选阅读路线

| 你的目的 | 建议入口 |
|---|---|
| 先完整理解 YooAsset | [YooAsset 首页]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}}) |
| 先完整理解 Addressables | [Addressables 首页]({{< relref "engine-toolchain/unity-addressables-series-index.md" >}}) |
| 已了解两者，希望比较设计与选型 | 从下面的选型讨论和三个对比维度开始 |
| 正在处理下载、更新、回滚或内存问题 | 跳到本页的故障与迁移案例 |

## 选型与打包策略

| 顺序 | 文章 | 阅读重点 |
|---|---|---|
| 1 | [先明确资源交付的主要问题]({{< relref "engine-toolchain/unity-addressables-yooasset-main-battlefield-of-resource-delivery.md" >}}) | 从团队面对的资源交付问题出发，确定比较维度。 |
| 2 | [Addressables、YooAsset 与自研怎样选择]({{< relref "engine-toolchain/unity-addressables-yooasset-or-self-built-how-to-choose.md" >}}) | 把框架能力、团队维护责任和项目条件放到一起判断。 |
| 3 | [Group 划分与 Bundle 布局]({{< relref "engine-toolchain/unity-addressables-yooasset-grouping-bundle-layout-best-practices.md" >}}) | 检查分组和打包粒度怎样影响依赖、冗余与交付。 |

## 同类问题的三个对比维度

| 顺序 | 文章 | 阅读重点 |
|---|---|---|
| 1 | [运行时调度对比]({{< relref "engine-toolchain/unity-addressables-vs-yooasset-runtime-scheduling-comparison.md" >}}) | 定位、加载调度和资源生命周期如何组织。 |
| 2 | [构建与产物对比]({{< relref "engine-toolchain/unity-addressables-vs-yooasset-build-artifact-comparison.md" >}}) | 构建输入、Bundle 布局、索引产物和内容更新如何组织。 |
| 3 | [版本与交付治理对比]({{< relref "engine-toolchain/unity-addressables-vs-yooasset-governance-comparison.md" >}}) | 版本控制、下载、缓存和回滚需要哪些机制与项目配套。 |

## 回到各自机制核对

| 共同问题 | Addressables | YooAsset |
|---|---|---|
| 运行时加载 | [运行时加载链路]({{< relref "engine-toolchain/unity-addressables-runtime-internals-loadassetasync-to-bundle-loaded.md" >}}) | [运行时加载链路]({{< relref "engine-toolchain/unity-yooasset-runtime-internals-loadassetasync-to-bundle-loaded.md" >}}) |
| 资源索引与版本 | [Catalog 与内容更新]({{< relref "engine-toolchain/unity-addressables-catalog-encoding-loading-cost-update.md" >}}) | [Manifest 与版本索引]({{< relref "engine-toolchain/unity-yooasset-manifest-version-fileindex-lookup.md" >}}) |
| 构建输入与产物 | [Group 与构建产物]({{< relref "engine-toolchain/unity-addressables-build-time-group-schema-to-bundle-output.md" >}}) | [资源收集与构建产物]({{< relref "engine-toolchain/unity-yooasset-build-time-collector-to-bundle-output.md" >}}) |
| 框架边界与项目职责 | [框架边界与项目职责]({{< relref "engine-toolchain/unity-addressables-boundaries-what-projects-must-build.md" >}}) | [框架边界与项目职责]({{< relref "engine-toolchain/unity-yooasset-boundaries-what-projects-must-build.md" >}}) |

生命周期与下载缓存还可以分别深入 [Addressables 引用计数]({{< relref "engine-toolchain/unity-addressables-refcount-lifecycle-release-harder-than-load.md" >}}) 和 [YooAsset 下载缓存]({{< relref "engine-toolchain/unity-yooasset-downloader-cache-system-resume-verify.md" >}})。这两篇回答不同子问题，应结合上面的共同维度阅读。

## 故障与迁移案例

| 顺序 | 文章 | 阅读重点 |
|---|---|---|
| 1 | [下载中断与恢复]({{< relref "problem-solving/addressables-yooasset-download-interruption-resume-recovery.md" >}}) | 下载失败后，从续传、重试和校验路径定位问题。 |
| 2 | [清单与 Bundle 不一致]({{< relref "problem-solving/addressables-yooasset-half-update-catalog-manifest-bundle-mismatch.md" >}}) | 区分索引已经更新和所需文件已经就绪。 |
| 3 | [线上版本回滚]({{< relref "problem-solving/addressables-yooasset-online-rollback-paths-and-costs.md" >}}) | 梳理回滚时的版本、清单、文件保留与缓存条件。 |
| 4 | [句柄泄漏与内存增长]({{< relref "problem-solving/addressables-yooasset-handle-leak-memory-growth.md" >}}) | 以 Addressables 为主，追踪引用未释放的问题。 |
| 5 | [两个框架之间的迁移]({{< relref "problem-solving/addressables-yooasset-migration-path-compatibility-pitfalls.md" >}}) | 检查地址、运行时接口、构建产物与缓存的迁移边界。 |

## 阅读与判断方式

先区分底层 AssetBundle 能力、框架提供的管理机制和项目自行组织的发布流程。比较某项能力时，核对文章注明的 Unity 与框架版本，再根据实际构建产物、运行日志和设备表现判断是否适合自己的项目。

需要补资源管线背景时，可以先读 [Unity 资源交付方案]({{< relref "delivery-engineering/delivery-resource-pipeline-04-unity-solutions.md" >}})；继续深入实现时，回到 [Addressables 首页]({{< relref "engine-toolchain/unity-addressables-series-index.md" >}}) 或 [YooAsset 首页]({{< relref "engine-toolchain/unity-yooasset-series-index.md" >}})。
