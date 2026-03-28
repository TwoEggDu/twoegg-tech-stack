---
date: "2026-03-28"
title: "游戏性能判断入口｜客户端程序先看定位、证据链和引擎显形"
description: "给客户端程序的性能入口页：先建立 CPU / GPU / I/O / Memory / Sync / Thermal 的判断框架，再把证据链落到 Unity / Unreal 的现场表现。"
slug: "game-performance-entry-client-programmer"
weight: 170
featured: false
tags:
  - Performance
  - Index
  - Unity
  - Unreal
  - Client
series: "游戏性能判断"
primary_series: "game-performance-judgment"
series_role: "appendix"
series_order: 1
---
> 这个入口面向客户端程序。你的核心任务不是先背一堆优化技巧，而是先把问题定位对，再把证据链接回引擎现场。

如果你现在面对的是掉帧、首开卡顿、Main Thread 尖峰、GC.Alloc、GPU bound、加载后仍不可用这类问题，最稳的起点是这页。

## 第一轮先读这几篇

1. [为什么某些操作会慢：给游戏开发的性能判断框架]({{< relref "engine-notes/game-performance-judgment-framework.md" >}})
   先把 CPU / GPU / I/O / Memory / Sync / Thermal 这几类问题分开。
2. [一帧到底是怎么完成的：游戏里一个 Frame 到底在做什么]({{< relref "engine-notes/game-performance-frame-breakdown.md" >}})
   把卡顿放回真实时间轴，而不是只盯某个模块名。
3. [怎么判断你到底卡在哪：CPU / GPU / I/O / Memory / Sync / Thermal 的诊断方法]({{< relref "engine-notes/game-performance-diagnosis-method.md" >}})
   这是客户端程序最该反复回看的总诊断页。
4. [什么事不能在什么时候做：游戏开发里最危险的时机管理]({{< relref "engine-notes/game-performance-dangerous-operations-timing.md" >}})
   很多问题不是不能做，而是不能在玩家最敏感的时机做。
5. [Unity 里，这些性能问题通常怎么显形]({{< relref "engine-notes/game-performance-unity-symptoms.md" >}}) 或 [Unreal 里，这些性能问题通常怎么显形]({{< relref "engine-notes/game-performance-unreal-symptoms.md" >}})
   把底层问题映射回你每天在 Profiler / Insights 里真正看到的脸。

## 按你手上的问题跳转

- `Main Thread` 尖峰、`GC.Alloc`、生命周期风暴：
  看 [CPU 性能优化 01｜C# GC 压力：堆分配来源、零分配写法与对象池]({{< relref "engine-notes/cpu-opt-01-gc-pressure.md" >}})、[CPU 性能优化 03｜Update 调用链优化：减少 Update 数量与手动调度管理器]({{< relref "engine-notes/cpu-opt-03-update-scheduling.md" >}})、[CPU 性能优化 04｜Unity Profiler CPU 深度分析：调用栈、GC.Alloc 定位与 HierarchyMode]({{< relref "engine-notes/cpu-opt-04-profiler-cpu-deep.md" >}})。
- 读盘完成了，但资源还是没法立刻用：
  看 [读盘完成，为什么还是不等于资源可用]({{< relref "engine-notes/game-performance-read-does-not-mean-ready.md" >}}) 和 [为什么一个大整文件，往往比很多小散文件更稳]({{< relref "engine-notes/game-performance-big-files-vs-small-files.md" >}})。
- 同一问题在手机和 PC 上长得完全不像一回事：
  看 [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}}) 和 [从型号表到能力指纹：Android 与 PC 的分档判断怎么设计]({{< relref "engine-notes/from-model-table-to-capability-fingerprint-android-and-pc-tiering.md" >}})。
- 你已经抓到现象，但总是改不准：
  先回 [怎么判断你到底卡在哪]({{< relref "engine-notes/game-performance-diagnosis-method.md" >}})，最后再看 [从现象到方法：把游戏性能判断连成一套工作流]({{< relref "engine-notes/game-performance-methodology-summary.md" >}})。

## 这个入口刻意不替你做什么

- 它不把“优化”简化成某几条代码技巧。
- 它不默认 Unity 现场就等于根因本身。
- 它不鼓励一看到尖峰就直接开改。

如果你需要的是资源自检视角，转到 [游戏性能判断入口｜美术先看资源预算、自检指标和高风险效果]({{< relref "engine-notes/game-performance-entry-artist.md" >}})。如果你要把约束沉淀成 Shader、材质和质量分档规则，转到 [游戏性能判断入口｜TA 先看渲染链路、材质治理和分档策略]({{< relref "engine-notes/game-performance-entry-ta.md" >}})。
