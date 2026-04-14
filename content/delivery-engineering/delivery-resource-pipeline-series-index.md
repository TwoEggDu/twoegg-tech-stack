---
date: "2026-04-14"
title: "资源管线系列索引｜从源资产到运行时文件的完整工程链路"
description: "给交付工程专栏 V05 补一个稳定入口：先讲资源管线任何引擎都要解决的四个问题，再落到 Unity 的 AssetBundle、Addressables 和 YooAsset。"
slug: "delivery-resource-pipeline-series-index"
weight: 400
featured: false
tags:
  - "Delivery Engineering"
  - "Resource Pipeline"
  - "AssetBundle"
  - "Addressables"
  - "Index"
series: "资源管线"
series_id: "delivery-resource-pipeline"
series_role: "index"
series_order: 0
series_nav_order: 5
series_title: "资源管线"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "工具链工程师"
  - "技术负责人 / 主程"
series_level: "进阶"
series_best_for: "当你想搞清楚 AssetBundle 和 Addressables 到底解决什么问题、打包策略怎么定、资源版本怎么管、加载生命周期怎么控制"
series_summary: "把资源管线从原理层（打包/加载/版本/生命周期四个核心问题）到实践层（Unity AB/Addressables/YooAsset）完整覆盖。"
series_intro: "这组文章不从某个资源管理方案的 API 文档开始，而是先问'任何引擎的资源管线都要解决哪几个问题'。理解了问题，再看 AssetBundle、Addressables、YooAsset 分别怎么回答这些问题，选型和排障都会清楚得多。"
delivery_layer: "principle"
delivery_volume: "V05"
delivery_reading_lines:
  - "L1"
  - "L2"
---

## 这组文章要解决什么

V02 讲了资源怎么从 DCC 工具进入引擎。V05 接上来：资源进入引擎后，怎样打包、加载、管理版本和控制生命周期，最终到达用户设备的运行时。

资源管线是交付链路中体积最大、复杂度最高的环节。打包策略影响包体大小和加载速度，版本管理影响热更新的正确性，生命周期管理影响内存稳定性。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [资源管线的本质]({{< relref "delivery-engineering/delivery-resource-pipeline-01-fundamentals.md" >}}) | 任何引擎的资源管线都要解决哪四个问题？ |
| 02 | [资源序列化]({{< relref "delivery-engineering/delivery-resource-pipeline-02-serialization.md" >}}) | 资产怎么变成字节？序列化格式的选型影响什么？ |
| 03 | [打包策略设计]({{< relref "delivery-engineering/delivery-resource-pipeline-03-bundling-strategy.md" >}}) | 分组原则是什么？依赖怎么管？冗余怎么控制？ |
| 04 | [Unity 实践：AB vs Addressables vs YooAsset]({{< relref "delivery-engineering/delivery-resource-pipeline-04-unity-solutions.md" >}}) | 三套方案各自的定位、优劣和选型依据是什么？ |
| 05 | [资源加载与生命周期]({{< relref "delivery-engineering/delivery-resource-pipeline-05-loading-lifecycle.md" >}}) | 同步/异步怎么选？引用计数怎么管？泄漏怎么防？ |
| 06 | [Catalog 与 Manifest 版本比对]({{< relref "delivery-engineering/delivery-resource-pipeline-06-catalog-versioning.md" >}}) | 客户端怎么知道"需要下载哪些新资源"？版本比对的工程设计。 |

## 推荐阅读顺序

01 是纯原理层，任何引擎都适用。02-03 在原理层展开打包和序列化。04 是 Unity 实践层的选型对比。05-06 覆盖运行时加载和版本管理。

如果你不用 Unity，读 01 + 02 + 03 + 05 + 06，跳过 04。

## 与其他栏目的关系

本站的引擎工具链栏有完整的 AssetBundle 系列（18 篇）和 Addressables 系列（11 篇），提供 API 级别的技术深挖。V05 从交付工程视角讲"这些方案在交付链路中的位置和工程选型"，深度技术细节请到引擎工具链栏阅读。
