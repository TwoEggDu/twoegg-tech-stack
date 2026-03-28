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

先把边界说清楚：美术第一轮不需要先搞懂 `CPU bound`、`GPU bound`、`Draw Call`、`Culling` 这些术语。你更应该先回答 3 个问题：

- 哪些资源最容易超预算。
- 预算不够时哪些效果该先砍，哪些该最后砍。
- 低档设备上的玩家还能不能感受到“这是同一款游戏”。

## 第一轮先读这几篇

1. [每档资产规格清单：贴图压缩、LOD 与包体分层]({{< relref "engine-notes/device-tier-asset-spec-texture-and-package.md" >}})
   这是最适合美术拿来做自检的预算入口。
2. [性能预算不够用时，什么该最后砍：移动端视觉效果性价比排序]({{< relref "engine-notes/device-tier-visual-tradeoff-priority.md" >}})
   帮你判断哪些效果最贵，哪些效果该最后保。
3. [四个档位的玩家应该感受到同一款游戏：体验一致性设计]({{< relref "engine-notes/device-tier-experience-consistency.md" >}})
   帮你判断哪些是“画质可以降”，哪些是“降了就变味”。
4. [什么事不能在什么时候做：游戏开发里最危险的时机管理]({{< relref "engine-notes/game-performance-dangerous-operations-timing.md" >}})
   先知道哪些内容最不适合在首开 UI、首放特效、切场景、进战斗的瞬间一起压上去。

## 第二轮再看这些

下面这些文章更偏实现层。它们有价值，但不适合拿来做美术第一轮入口：

- 做场景、植被、角色资源时，再看 [Unity 渲染系统补C｜LOD 与 Culling 系统：Frustum、Occlusion、HZB]({{< relref "engine-notes/unity-rendering-supp-c-lod-culling.md" >}})。
- 做 UI 时，再看 [Unity 渲染系统补D｜UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas]({{< relref "engine-notes/unity-rendering-supp-d-ui-rendering.md" >}})。
- 做特效时，再看 [Unity 渲染系统 04｜粒子与特效：Particle System 的几何生成与渲染机制]({{< relref "engine-notes/unity-rendering-04-particles-vfx.md" >}})。
- 想知道为什么透明叠加和材质切换会贵，再看 [GPU 渲染优化 01｜Draw Call 与 Overdraw：移动端的合批策略与 Alpha 排序]({{< relref "engine-notes/gpu-opt-01-drawcall-overdraw.md" >}})。
- 想知道阴影和后处理为什么总是最先吃预算，再看 [GPU 优化 04｜移动端阴影：Shadow Map 代价、CSM 配置与软阴影替代方案]({{< relref "engine-notes/gpu-opt-04-shadow-mobile.md" >}}) 和 [GPU 渲染优化 05｜后处理在移动端的取舍与降质策略]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})。

## 接下来最值得补的桥接文章

- 美术资源性能自检表：贴图、材质、LOD、UI、特效提交前先看什么
- 为什么这个资源看起来不重，进项目却会贵：透明、阴影、后处理与特效叠层的成本直觉
- 低档不是粗暴砍效果：角色、场景、UI 与特效怎么降级而不变味

## 按你手上的问题跳转

- 场景一复杂，手机就开始掉帧或发热：
  先看 [每档资产规格清单]({{< relref "engine-notes/device-tier-asset-spec-texture-and-package.md" >}})，再按需要进 [LOD 与 Culling]({{< relref "engine-notes/unity-rendering-supp-c-lod-culling.md" >}})、[移动端阴影]({{< relref "engine-notes/gpu-opt-04-shadow-mobile.md" >}})、[后处理在移动端的取舍]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})。
- 特效一叠就炸，或者某些粒子看起来“没多少”却很重：
  先看 [性能预算不够用时，什么该最后砍]({{< relref "engine-notes/device-tier-visual-tradeoff-priority.md" >}})，再看 [粒子与特效]({{< relref "engine-notes/unity-rendering-04-particles-vfx.md" >}}) 和 [后处理在移动端的取舍]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})。
- UI 首开、切页或叠层时明显抖动：
  先看 [什么事不能在什么时候做]({{< relref "engine-notes/game-performance-dangerous-operations-timing.md" >}})，再看 [UI 渲染：Canvas 合批、Rebuild、Overdraw、Atlas]({{< relref "engine-notes/unity-rendering-supp-d-ui-rendering.md" >}})。
- 同一套内容做了低中高三档后，看起来越来越不像同一款游戏：
  看 [四个档位的玩家应该感受到同一款游戏：体验一致性设计]({{< relref "engine-notes/device-tier-experience-consistency.md" >}}) 和 [性能预算不够用时，什么该最后砍]({{< relref "engine-notes/device-tier-visual-tradeoff-priority.md" >}})。

## 美术入口的边界

- 这页不要求你独自负责最终瓶颈定位。
- 它更关心资源自检和高风险效果识别，而不是线程调度或运行时证据链。
- 第一轮先不要硬啃 `Draw Call`、`Culling`、`Pass`、`Shader` 这些术语；等你先知道“自己该保什么、该砍什么、哪里最危险”之后，再回来看它们会顺得多。
- 如果你发现问题已经落到材质规则、Shader 关键词、质量分档和渲染配置治理，下一步应转到 [游戏性能判断入口｜TA 先看渲染链路、材质治理和分档策略]({{< relref "engine-notes/game-performance-entry-ta.md" >}})。

如果你想回到总地图，去看 [游戏性能判断系列索引｜先看判断框架，再按角色入口或问题类型进入正文]({{< relref "engine-notes/game-performance-judgment-series-index.md" >}})。
