---
date: "2026-03-28"
title: "游戏引擎架构地图系列索引｜先立引擎分层地图，再看内容生产、运行时和平台抽象"
description: "先以职责地图认识引擎，再用引擎无关的依赖补篇拆清框架、专业系统、资源服务和平台边界，连接内容生产、运行时与具体实现。"
slug: "game-engine-architecture-series-index"
weight: 8
featured: false
tags:
  - "Engine"
  - "Architecture"
  - "Unity"
  - "Index"
series: "游戏引擎架构地图"
series_id: "game-engine-architecture"
series_role: "index"
series_order: 0
series_nav_order: 100
series_title: "游戏引擎架构地图"
series_entry: true
series_audience:
  - "客户端 / 引擎开发"
  - "技术负责人"
series_level: "入门到进阶"
series_best_for: "当你想把内容生产、运行时、专业子系统和跨平台抽象放回同一张引擎分层地图里"
series_summary: "用职责、接口与数据交接认识引擎，再用模板编辑、角色状态和内容交付追清各部分怎样协作。"
series_intro: '从一个具体变化进入引擎，找出谁持有状态、谁提供接口、谁消费结果和谁负责清理。先用00建立职责地图，再用00A区分接口依赖与资源边界，随后结合Unity、Unreal等公开资料解释具体选择。'
series_reading_hint: '第一次读可从火球导读进入00和00A；有具体问题时按问题跳读。职责分类和阅读顺序都不代表引擎的严格运行流水线。'
---
第一次接触这个主题，可以从[火球导读]({{< relref "rendering/player-input-and-game-content-to-screen.md" >}})认识内容、实例和画面的关系，再读[00职责地图]({{< relref "system-design/game-engine-architecture-00-overview.md" >}})与[00A接口依赖和资源边界]({{< relref "system-design/game-engine-architecture-00a-layers-dependencies-resource-boundaries.md" >}})。这些图是阅读与设计的参考模型，不把某一种目录组织当作行业标准。

## 按问题阅读

| 当前想弄清什么 | 入口与例子 |
| --- | --- |
| 修改内容怎样进入世界 | [01内容生产]({{< relref "system-design/game-engine-architecture-01-content-production.md" >}})的模板改值 → [02对象世界]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}})的角色、组件与共享资产 |
| 系统怎样执行和交接结果 | [03运行机制]({{< relref "system-design/game-engine-architecture-03-runtime-foundation.md" >}})的动画任务 → [04专业系统]({{< relref "system-design/game-engine-architecture-04-professional-subsystems.md" >}})的角色落地 |
| 资源怎样交付，平台差异放哪里 | [05资产与交付]({{< relref "system-design/game-engine-architecture-05-asset-and-delivery.md" >}})的产物分支 → [06平台适配]({{< relref "system-design/game-engine-architecture-06-platform-abstraction.md" >}})的能力和降级 |
| 批量数据处理会改变哪些设计 | [07 DOTS与Mass]({{< relref "system-design/game-engine-architecture-07-dots-and-mass.md" >}})的创作数据、实体更新和专业表示 |

需要时间与生命周期模型时，接[GEF-02：启动、帧循环与退出]({{< relref "engine-toolchain/game-engine-foundations-02-startup-frame-shutdown.md" >}})；需要追真实代码时，进入[Piccolo系列]({{< relref "reading/piccolo-engine.md" >}})。两者分别补公共原理与具体实现，不把概念图当作某台引擎的调用链。

{{< series-directory >}}

