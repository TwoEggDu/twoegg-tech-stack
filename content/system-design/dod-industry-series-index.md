---
date: "2026-04-13"
title: "数据导向行业横向对比系列索引｜Overwatch、DOOM、Flecs / EnTT 怎么做 DOD"
description: "三篇横向对比：Overwatch ECS、idTech7 (DOOM) 的 Job 模型、以及 Flecs 和 EnTT 两个开源框架的设计取舍。"
slug: "dod-industry-series-index"
weight: 1
featured: false
tags:
  - "DOD"
  - "ECS"
  - "Industry"
  - "Index"
series: "数据导向行业横向对比"
series_id: "dod-industry"
series_role: "index"
series_order: 0
series_nav_order: 70
series_title: "数据导向行业横向对比"
series_audience:
  - "引擎 / 架构程序"
series_level: "进阶"
series_best_for: "当你想看 DOD/ECS 在工业级项目中到底怎么落地"
series_summary: "拆解 Overwatch ECS、idTech7 Job 模型和 Flecs/EnTT 开源框架的设计决策"
series_intro: "这组文章不是 ECS 教程，而是把三个有据可查的工业级 DOD 实践拆开看：Blizzard 怎么用 ECS 做 Overwatch、id Software 怎么用 Job Graph 做 DOOM、以及 Flecs 和 EnTT 作为开源框架各自的设计取舍。"
series_reading_hint: "三篇独立成文，可以按兴趣跳读。"
---
> 这页是数据导向行业横向对比的系列入口。三篇案例各自独立，最后一篇把判断依据收成选型决策地图。可以按兴趣跳读，也可以顺序读完再看选型。

## 行业案例

1. [DOD 行业案例 01｜Overwatch ECS（GDC 2017）：ECS 的架构价值和性能价值可以分开]({{< relref "system-design/dod-industry-01-overwatch-ecs.md" >}})
   Overwatch 的 ECS 是 Managed 的——没有 cache-friendly 布局，没有 SIMD 优化——但它成功解决了逻辑隔离和可维护性问题。这个案例证明 ECS 的架构价值和性能价值可以分开。

2. [DOD 行业案例 02｜id Tech 7 / DOOM Eternal：不用 ECS 框架，用 Job Graph 手工管数据流]({{< relref "system-design/dod-industry-02-idtech7-doom.md" >}})
   DOOM Eternal 没有 ECS 框架，但渲染和游戏逻辑都是数据导向的——靠手工设计的 Job Graph 管理数据流，让 CPU 核心充分并行。这个案例证明 DOD 不等于必须有 ECS 框架。

3. [DOD 行业案例 03｜Flecs 与 EnTT：独立 ECS 框架的设计哲学，Minecraft Bedrock 为什么选 EnTT]({{< relref "system-design/dod-industry-03-flecs-entt.md" >}})
   在 Unity DOTS 和 Unreal Mass 之外，C++ 独立 ECS 框架是服务端和跨平台项目的另一条路。Flecs 功能完整、内置工具链；EnTT 极简、header-only、控制权在开发者手里。两种哲学各有适用场景。

## 选型决策

4. [DOD 行业案例 04｜选型决策地图：DOTS / Mass / Flecs / EnTT / 自研，什么项目选哪条路]({{< relref "system-design/dod-industry-04-selection-map.md" >}})
   读完前三篇案例后，把判断依据收成一张可操作的决策地图。引擎绑定、性能瓶颈类型、是否需要服务端部署、团队规模——四个维度决定选型空间。

## 如果你带着具体问题来

- 想知道 ECS 到底是为了性能还是架构：
  先看 [01 Overwatch ECS]({{< relref "system-design/dod-industry-01-overwatch-ecs.md" >}})，Blizzard 的案例把这两种价值拆得最清楚。

- 项目不想引入 ECS 框架，但想用 DOD 思路：
  先看 [02 id Tech 7 / DOOM Eternal]({{< relref "system-design/dod-industry-02-idtech7-doom.md" >}})，Job Graph 是不依赖框架的 DOD 实践路径。

- 在 Flecs 和 EnTT 之间犹豫：
  先看 [03 Flecs 与 EnTT]({{< relref "system-design/dod-industry-03-flecs-entt.md" >}})，再看 [04 选型决策地图]({{< relref "system-design/dod-industry-04-selection-map.md" >}}) 把约束条件对号入座。

{{< series-directory >}}
