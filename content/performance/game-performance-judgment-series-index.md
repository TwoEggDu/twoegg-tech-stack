---
date: "2026-03-28"
title: "游戏性能判断系列索引｜先看判断框架，再看症状、证据和排查工作流"
description: "给 游戏性能判断补一个稳定入口：先说明性能问题应该怎样被判断，再列出当前全部文章。"
slug: "game-performance-judgment-series-index"
weight: 169
featured: false
tags:
  - "Performance"
  - "Unity"
  - "Unreal"
  - "Index"
series: "游戏性能判断"
series_id: "game-performance-judgment"
series_role: "index"
series_order: 0
series_nav_order: 140
series_title: "游戏性能判断"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "性能优化"
series_level: "进阶"
series_best_for: "当你想把 CPU / GPU / I/O / Memory / Sync / Thermal 的性能判断拉回同一套诊断框架"
series_summary: "把性能问题从现象、平台直觉、能力指纹、资源行为和证据链，收成一套可执行的判断工作流。"
series_intro: '这组文章关心的不是"记几个优化技巧"，而是先建立一套性能判断框架：看到现象时先怀疑什么，怎样区分 CPU、GPU、I/O、Memory、Sync 和 Thermal 的问题，以及 Unity / Unreal 中这些问题通常怎样显形。'
series_reading_hint: "第一次进入这个系列，可以先走判断框架和一帧拆解这条共用主线；如果你是带着职责来读，也可以直接从客户端程序、美术、TA、策划四个入口进入。"
---
> 这组文章原本更像一条"判断框架 -> 证据链 -> 引擎显形 -> 方法收束"的主线，但现在已经可以稳定分成两层入口：所有人共用的主线，以及按职责进入的四个角色入口。

如果你这次最关心的是存储、I/O、小文件访问、读盘不等于可用和加载链这条线，可以直接先看 [存储设备与 IO 基础系列索引｜先立住存储硬件、文件系统和 OS I/O，再回到游戏加载链]({{< relref "engine-toolchain/storage-io-series-index.md" >}})。

## 四个角色入口

- [游戏性能判断入口｜客户端程序先看定位、证据链和引擎显形]({{< relref "performance/game-performance-entry-client-programmer.md" >}})
  适合先做瓶颈分类、证据链定位和 Unity / Unreal 现场映射的人。
- [游戏性能判断入口｜美术先看资源预算、自检指标和高风险效果]({{< relref "performance/game-performance-entry-artist.md" >}})
  适合先从贴图、LOD、UI、特效、阴影、后处理这些资源风险点入手的人。
- [游戏性能判断入口｜TA 先看渲染链路、材质治理和分档策略]({{< relref "performance/game-performance-entry-ta.md" >}})
  适合先把 Shader、材质、Variant、Renderer Feature 和质量分档拉回工程规则的人。
- [游戏性能判断入口｜策划先看预算边界、人数规模和体验一致性]({{< relref "performance/game-performance-entry-designer.md" >}})
  适合先判断玩法规模、首进体验、分档代价和一致性边界的人。

如果你还不确定自己该从哪进：

- 想先判断"到底卡在哪一层"，从客户端程序入口开始。
- 想先知道"哪些资源最容易超预算"，从美术入口开始。
- 想先把约束固化成材质、渲染和分档规则，从 TA 入口开始。
- 想先减少玩法和活动设计对性能的放大效应，从策划入口开始。

## 共用主线

如果你想先建立一套跨角色共用的性能直觉，建议按这条主线走：

1. [为什么某些操作会慢：给游戏开发的性能判断框架]({{< relref "performance/game-performance-judgment-framework.md" >}})
2. [一帧到底是怎么完成的：游戏里一个 Frame 到底在做什么]({{< relref "performance/game-performance-frame-breakdown.md" >}})
3. [什么事不能在什么时候做：游戏开发里最危险的时机管理]({{< relref "performance/game-performance-dangerous-operations-timing.md" >}})
4. [怎么判断你到底卡在哪：CPU / GPU / I/O / Memory / Sync / Thermal 的诊断方法]({{< relref "performance/game-performance-diagnosis-method.md" >}})
5. [Unity 里，这些性能问题通常怎么显形]({{< relref "performance/game-performance-unity-symptoms.md" >}}) 或 [Unreal 里，这些性能问题通常怎么显形]({{< relref "performance/game-performance-unreal-symptoms.md" >}})
6. [从现象到方法：把游戏性能判断连成一套工作流]({{< relref "performance/game-performance-methodology-summary.md" >}})

## 按常见问题进入

- 首次进场、首放技能、首开 UI 时卡一下：
  看 [什么事不能在什么时候做]({{< relref "performance/game-performance-dangerous-operations-timing.md" >}})。
- 读盘结束了，但玩家看到的资源还没准备好：
  看 [读盘完成，为什么还是不等于资源可用]({{< relref "performance/game-performance-read-does-not-mean-ready.md" >}})。
- 同一问题在手机和 PC 上结论完全不同：
  看 [手机和 PC 为什么要用不同的性能直觉]({{< relref "performance/game-performance-mobile-vs-pc-intuition.md" >}})。
- 你已经抓到现象，但团队总在不同层级上各说各话：
  先回 [判断框架]({{< relref "performance/game-performance-judgment-framework.md" >}})，最后再看 [方法收束]({{< relref "performance/game-performance-methodology-summary.md" >}})。

{{< series-directory >}}
