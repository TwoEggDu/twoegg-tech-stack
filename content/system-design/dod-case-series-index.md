---
date: "2026-04-13"
title: "数据导向实战案例系列索引｜RTS 单位管理、弹幕系统、混合架构三个落地场景"
description: "三篇实战案例：ECS 管理千级 RTS 单位、纯 DOD 弹幕系统、OOP+ECS 混合架构的边界判断。"
slug: "dod-case-series-index"
weight: 1
featured: false
tags:
  - "DOD"
  - "ECS"
  - "Case Study"
  - "Index"
series: "数据导向实战案例"
series_id: "dod-case"
series_role: "index"
series_order: 0
series_nav_order: 71
series_title: "数据导向实战案例"
series_audience:
  - "Gameplay / 系统程序"
series_level: "进阶"
series_best_for: "当你想看 DOD/ECS 在具体游戏场景中怎么落地"
series_summary: "用 RTS 单位管理、弹幕系统、混合架构三个案例展示 DOD 的实际落地边界"
series_intro: "这组文章不讲 ECS 理论，只讲三个具体场景：千级 RTS 单位怎么用 ECS 管理、弹幕系统为什么适合纯 DOD、以及现有 OOP 项目怎么局部引入 ECS 而不推翻全局。"
series_reading_hint: "三篇独立成文，按你当前项目最接近的场景选读。"
---
> 这页是数据导向实战案例的系列入口。三篇案例各自独立，按你当前项目最接近的场景选读即可。

## 实战案例

1. [DOD 实战案例 01｜大规模单位调度（RTS）：ECS + Jobs 完整实现，从 1000 到 100000 单位的扩展路径]({{< relref "system-design/dod-case-01-rts-units.md" >}})
   RTS 单位是 ECS 最经典的使用对象——数量大、结构同构、每帧更新。这篇从 Archetype 设计出发，把 1000 到 100000 单位的扩展路径拆开：Query 优化、LOD 触发、渲染端接入各自在哪个量级开始变关键。

2. [DOD 实战案例 02｜弹幕系统（5000+ 子弹）：碰撞检测、生命周期、VFX 同步的 ECS 实现]({{< relref "system-design/dod-case-02-bullet-system.md" >}})
   弹幕系统是 DOTS 常见的入门案例，但深度实现涉及大量边界问题：子弹生命周期归零时怎么触发 VFX、碰撞检测用不用 Unity Physics、命中同一目标的多颗子弹怎么去重伤害。这篇逐一讲清这些边界。

3. [DOD 实战案例 03｜混合架构设计：ECS 仿真层 + GameObject 表现层的稳定边界策略]({{< relref "system-design/dod-case-03-hybrid-architecture.md" >}})
   真实项目永远是混合的——UI 跑在 Canvas 上，音效挂在 AudioSource 里，相机是 Managed 对象。混合架构不是妥协，是理性选择。这篇讲清楚 ECS 仿真层和 GameObject 表现层的边界怎样设计，数据权威和同步时机怎样保证。

## 如果你带着具体问题来

- 项目有大量同构单位，想知道 ECS 能扛到什么量级：
  先看 [01 RTS 单位调度]({{< relref "system-design/dod-case-01-rts-units.md" >}})，从 1000 到 100000 的扩展路径逐级拆解。

- 想用 ECS 做高频小对象（子弹、粒子、投射物）：
  先看 [02 弹幕系统]({{< relref "system-design/dod-case-02-bullet-system.md" >}})，Archetype 精简、碰撞检测方案选择和 VFX 跨边界触发是核心问题。

- 现有 OOP 项目想局部引入 ECS，不知道边界画在哪：
  先看 [03 混合架构设计]({{< relref "system-design/dod-case-03-hybrid-architecture.md" >}})，搞清楚哪些数据是权威、哪些是镜像，以及哪些边界设计会让混合架构越来越难改。

{{< series-directory >}}
