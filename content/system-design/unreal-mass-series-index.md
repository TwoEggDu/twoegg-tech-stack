---
date: "2026-03-28"
title: "Unreal Mass 深度系列索引｜先立数据导向边界，再看 Processor、LOD 和 Actor 协作"
description: "给 Unreal Mass 深度系列补一个稳定入口：先说明 Mass 在 Unreal 里到底解决什么问题，再按 Processor、结构变化、LOD、Signal 和 Actor 边界组织阅读路径。"
slug: "unreal-mass-series-index"
weight: 6110
featured: false
tags:
  - "Unreal"
  - "Mass"
  - "ECS"
  - "Index"
  - "Architecture"
series: "Unreal Mass 深度"
series_id: "unreal-mass"
series_role: "index"
series_order: 0
series_nav_order: 112
series_title: "Unreal Mass 深度"
series_entry: true
series_audience:
  - "Unreal 客户端 / 引擎开发"
  - "大规模 AI / Simulation"
series_level: "进阶"
series_best_for: "当你想把 Unreal Mass 从概念映射、Processor 调度、结构变化到 Actor 协作一次看清"
series_summary: '把 Unreal Mass 从"为什么需要第二套运行时模型"，一路接到 Processor、LOD、Signal、Actor 边界和 City Sample 案例。'
series_intro: "这组文章关心的不是某个 API 怎么写，而是 Unreal 为什么要在 Actor 体系旁边再建立一套面向大规模同构对象的运行时模型。先把 Mass 和传统 UE 架构的边界立住，再看调度、结构变化、LOD、Signal 和真实案例。"
series_reading_hint: "第一次进入这个系列，建议先顺读 01 到 06，把问题空间、执行模型和边界都立住；City Sample 更适合在主线看完后回头作为验证案例。"
---

> 如果你是从 Unreal 总入口进来的，但你真正关心的是 `MassEntity / MassProcessor / City Sample` 这一支，那么这页比直接从 UObject、GAS 或网络复制主线切入更合适。

这页不再重复解释 Unreal 的传统对象系统，而是专门把 `Mass` 这条数据导向支线单独立出来。

## 先给一句总判断

`Mass 不是"把 Actor 改写成 ECS"这么简单，而是 Unreal 在大规模同构对象场景里额外建立的一套高密度运行时模型。`

所以这组文章真正想回答的是：

- 为什么 Actor 体系在海量实体场景下会显得太重
- Mass 的 `Fragment / Tag / Processor / Signal / LOD` 分别在解决哪一层问题
- Mass 和 Actor、Gameplay 系统、场景内容之间到底该怎么分边界

## 最短阅读路径

如果你第一次系统读，我建议按下面这条顺序走：

1. [Unreal Mass 01｜Mass Framework 架构全景：Fragment、Tag、Trait、EntityHandle 与 DOTS 概念对照]({{< relref "system-design/mass-01-framework-overview.md" >}})
2. [Unreal Mass 02｜Processor 执行模型：查询、阶段、并行调度和数据访问规则]({{< relref "system-design/mass-02-processor-execution.md" >}})
3. [Unreal Mass 03｜结构变化：Entity 创建、销毁、组合变化为什么要延迟处理]({{< relref "system-design/mass-03-structural-change.md" >}})
4. [Unreal Mass 04｜LOD 与可伸缩性：为什么海量实体必须先解决"看不见也别全算"]({{< relref "system-design/mass-04-lod.md" >}})
5. [Unreal Mass 05｜Signal：为什么不是所有逻辑都该靠每帧 Tick 推进]({{< relref "system-design/mass-05-signals.md" >}})
6. [Unreal Mass 06｜Actor 边界：什么该留在 Actor，什么该下沉到 Mass]({{< relref "system-design/mass-06-actor-boundary.md" >}})
7. [Unreal Mass 07｜City Sample 案例：把前面的概念放回真实工程结构里]({{< relref "system-design/mass-07-city-sample-case.md" >}})

## 如果你是带着问题来查

### 1. 你最先困惑的是"UE 已经有 Actor，为什么还要再来一套 Mass"

先看：

- [Unreal Mass 01｜Mass Framework 架构全景：Fragment、Tag、Trait、EntityHandle 与 DOTS 概念对照]({{< relref "system-design/mass-01-framework-overview.md" >}})

### 2. 你最先想搞清楚的是 Processor 到底怎么跑、怎么排阶段、怎么并行

先看：

- [Unreal Mass 02｜Processor 执行模型：查询、阶段、并行调度和数据访问规则]({{< relref "system-design/mass-02-processor-execution.md" >}})

### 3. 你已经开始写逻辑，但一遇到 Entity 增删改就开始混乱

先看：

- [Unreal Mass 03｜结构变化：Entity 创建、销毁、组合变化为什么要延迟处理]({{< relref "system-design/mass-03-structural-change.md" >}})

### 4. 你关心的是大规模仿真怎么真正撑住，而不是单个实体功能怎么写

先看：

- [Unreal Mass 04｜LOD 与可伸缩性：为什么海量实体必须先解决"看不见也别全算"]({{< relref "system-design/mass-04-lod.md" >}})
- [Unreal Mass 05｜Signal：为什么不是所有逻辑都该靠每帧 Tick 推进]({{< relref "system-design/mass-05-signals.md" >}})

### 5. 你现在最大的困惑是 Actor、Pawn、MassEntity 到底谁负责什么

先看：

- [Unreal Mass 06｜Actor 边界：什么该留在 Actor，什么该下沉到 Mass]({{< relref "system-design/mass-06-actor-boundary.md" >}})

### 6. 你已经理解概念，但想知道 Epic 在真实项目里怎样把它们拼起来

先看：

- [Unreal Mass 07｜City Sample 案例：把前面的概念放回真实工程结构里]({{< relref "system-design/mass-07-city-sample-case.md" >}})

## 相邻入口

- 如果你还没立住数据导向运行时的共性问题，先回 [数据导向运行时系列索引]({{< relref "engine-toolchain/data-oriented-runtime-series-index.md" >}})
- 如果你想先看 Unreal 传统对象系统、反射、GC、GAS 和网络复制主线，回 [Unreal Engine 架构与系统系列索引]({{< relref "system-design/unreal-engine-series-index.md" >}})

{{< series-directory >}}
