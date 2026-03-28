---
date: "2026-03-28"
title: "游戏性能判断入口｜美术先看资源预算、自检指标和高风险效果"
description: "给美术的性能入口页：先理解平台预算和资源规格，再看 Draw Call、Overdraw、LOD、UI、阴影、后处理和特效这些最常见的高风险点。"
slug: "game-performance-entry-artist"
weight: 171
featured: false
tags:
  - Performance
  - Index
  - Art
  - Unity
  - Rendering
series: "游戏性能判断"
primary_series: "game-performance-judgment"
series_role: "appendix"
series_order: 2
---
> 这个入口面向美术。目标不是把你变成 Profiler 工程师，而是帮你在资源提交前就知道哪些内容最容易把项目推到危险区。

如果你最关心的是贴图、LOD、UI、特效、阴影、后处理、透明叠加和机型分档，这页比直接从 CPU / GPU 理论篇硬啃更合适。

## 第一轮先读这几篇

1. [手机和 PC 为什么要用不同的性能直觉]({{< relref "engine-notes/game-performance-mobile-vs-pc-intuition.md" >}})
   先接受一个事实：同样一套资源，在手机和 PC 上的风险模型不是同一件事。
2. [每档资产规格清单：贴图压缩、LOD 与包体分层]({{< relref "engine-notes/device-tier-asset-spec-texture-and-package.md" >}})
   这是最适合美术拿来做自检的预算入口。
3. [性能预算不够用时，什么该最后砍：移动端视觉效果性价比排序]({{< relref "engine-notes/device-tier-visual-tradeoff-priority.md" >}})
   帮你判断哪些效果最贵，哪些效果该最后保。
4. [Unity 渲染系统 01b｜Draw Call 是什么：CPU 每次向 GPU 发出什么请求]({{< relref "engine-notes/unity-rendering-01b-draw-call-and-batching.md" >}})
   先把 Draw Call、材质切换和合批边界看清。
5. [GPU 渲染优化 01｜Draw Call 与 Overdraw：移动端的合批策略与 Alpha 排序]({{< relref "engine-notes/gpu-opt-01-drawcall-overdraw.md" >}})
   这是美术最容易直接影响的一条成本链。
6. [Unity 渲染系统补C｜LOD 与 Culling 系统：Frustum、Occlusion、HZB]({{< relref "engine-notes/unity-rendering-supp-c-lod-culling.md" >}}) 和 [Unity 渲染系统补D｜UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas]({{< relref "engine-notes/unity-rendering-supp-d-ui-rendering.md" >}})
   一个管场景层级，一个管界面层级。

## 按你手上的问题跳转

- 场景一复杂，手机就开始掉帧或发热：
  先看 [Draw Call 与 Overdraw]({{< relref "engine-notes/gpu-opt-01-drawcall-overdraw.md" >}})、[LOD 与 Culling]({{< relref "engine-notes/unity-rendering-supp-c-lod-culling.md" >}})、[移动端阴影]({{< relref "engine-notes/gpu-opt-04-shadow-mobile.md" >}})、[后处理在移动端的取舍]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})。
- 特效一叠就炸，或者某些粒子看起来“没多少”却很重：
  看 [Unity 渲染系统 04｜粒子与特效：Particle System 的几何生成与渲染机制]({{< relref "engine-notes/unity-rendering-04-particles-vfx.md" >}}) 和 [后处理在移动端的取舍]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})。
- UI 首开、切页或叠层时明显抖动：
  看 [UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas]({{< relref "engine-notes/unity-rendering-supp-d-ui-rendering.md" >}})。
- 同一套内容做了低中高三档后，看起来越来越不像同一款游戏：
  看 [四个档位的玩家应该感受到同一款游戏：体验一致性设计]({{< relref "engine-notes/device-tier-experience-consistency.md" >}}) 和 [性能预算不够用时，什么该最后砍]({{< relref "engine-notes/device-tier-visual-tradeoff-priority.md" >}})。

## 美术入口的边界

- 这页不要求你独自负责最终瓶颈定位。
- 它更关心资源自检和高风险效果识别，而不是线程调度或运行时证据链。
- 如果你发现问题已经落到材质规则、Shader 关键词、质量分档和渲染配置治理，下一步应转到 [游戏性能判断入口｜TA 先看渲染链路、材质治理和分档策略]({{< relref "engine-notes/game-performance-entry-ta.md" >}})。

如果你想回到总地图，去看 [游戏性能判断系列索引｜先看判断框架，再按角色入口或问题类型进入正文]({{< relref "engine-notes/game-performance-judgment-series-index.md" >}})。
