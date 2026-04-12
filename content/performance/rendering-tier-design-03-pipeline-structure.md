---
title: "渲染系统分档设计 03｜渲染链怎么设计：Camera、Pass、RT 与中间结果的组织原则"
slug: "rendering-tier-design-03-pipeline-structure"
date: "2026-04-01"
description: "从系统设计角度讲清楚一条面向分档的渲染链应该怎样组织 Camera、Pass、Render Target 和中间结果，以及怎样避免因为多余的 RT 切换把移动端和高端机一起拖慢。"
tags:
  - "Unity"
  - "URP"
  - "Rendering"
  - "RenderTarget"
  - "Performance"
  - "Device Tiering"
series: "渲染系统分档设计"
primary_series: "device-tiering"
series_order: 3
weight: 1920
featured: false
---

渲染链不是特效列表，也不是 URP 勾选项清单，而是一条把业务意图、可见结果和 GPU 存储位置连接起来的依赖图。真正该先定的，不是“要开多少个 Pass”，而是“哪些结果必须先产生，哪些结果可以不落地，哪些结果只能保留在这一段链路里”。

如果只记住一件事，我希望是这句：

**Camera 负责边界，Pass 负责顺序，RT 负责落点。**

一旦把这三者混在一起，后面就很容易长出一堆看起来“功能很全”，实际却到处都是 `CopyColor`、`CopyDepth`、`FinalBlit` 和额外 Overlay Camera 的链路。

## 先把依赖关系讲清楚

在 {{< relref "rendering/unity-rendering-01c-render-target-and-framebuffer.md" >}} 里，我们已经把 Render Target、Depth Buffer 和 Framebuffer 的概念拆开了。这一篇要往前再走一步：不只是知道 GPU 把结果写到哪里，而是知道**为什么要在这个时刻把它写到那里**。

一个更稳的理解方式是：

- `Camera` 决定“我看哪一组对象，最后输出到哪里”
- `Pass` 决定“我在什么顺序里读写哪些结果”
- `RT` 决定“某个中间结果要不要真的从片上落到一个独立缓冲里”

所以，渲染链设计的本质不是拼接算法，而是控制依赖：

- 后面是否真的需要前面产出的场景颜色
- 后面是否真的需要前面产出的深度
- 中间结果是否会被多次复用
- 这一段能不能留在 tile memory 里，不要变成系统内存往返

这也是为什么 {{< relref "rendering/urp-platform-01-mobile.md" >}} 一直强调移动端配置不能照搬 PC。移动端真正贵的，往往不是“多画了一次”，而是“本来可以不落地的结果被迫多落了一次”。

## Camera、Pass、RT 各自负责什么

| 对象 | 它负责什么 | 它不负责什么 |
| --- | --- | --- |
| Camera | 定义观察边界、输出边界和叠加关系 | 不负责决定某个算法该怎么写 |
| Pass | 定义执行顺序和数据依赖 | 不应该承载全局业务意图 |
| RT | 定义中间结果的落点 | 不负责决定谁先谁后 |

这个表看起来简单，但它是整条链是否健康的分水岭。

一个常见错误是：把“我想做一个特效”直接翻译成“我先新建一个 RT，再做一次 Blit，再让下一个 Pass 读它”。这在桌面端有时还能忍，在移动端很容易变成一串无意义的 Load/Store。

另一个常见错误是：把多个视觉目标都塞到一个 Camera Stack 里，然后靠 Overlay Camera 叠出结果。Overlay Camera 并不是不能用，而是它一旦和多个 full-screen pass、opaque texture、depth texture 混在一起，就很容易把“本来只需要一条链”变成“每层都在切 RT”。

如果你想继续展开 `Camera Stack` 的边界和代价，可以回看 {{< relref "rendering/urp-config-03-camera-stack.md" >}}。那篇更专门地讨论了 Base / Overlay 的职责和什么时候不该继续堆叠。

## 三种链路模板

更适合讨论的是链路模板，而不是单个开关。

| 模板 | 目标 | 典型结构 | 适合什么档位 | 主要风险 |
| --- | --- | --- | --- | --- |
| 轻量链 | 以最低成本完成主体渲染 | 单个 Base Camera，Opaque -> Transparent -> 少量必要 Post | 低档 / 中档 | 把不必要的颜色纹理和全屏处理也带进来 |
| 重效果链 | 在复杂场景里保留高质量特性 | Shadow Map -> 必要时 Depth Prepass -> Opaque -> 受控的 Copy/Blit -> Transparent -> Post | 高档 / 内容密集场景 | 过早引入中间 RT，导致 tile cache 失效 |
| 错误链 | 看起来功能很多，实则每层都在打断前一层 | 多个 Overlay Camera + 多段全屏 Blit + 强制 Intermediate Texture | 不建议 | RT 切换过多，带宽和延迟都被放大 |

### 轻量链

轻量链不是“功能残缺”，而是把链路压缩到只保留必要依赖。

典型形态是：

- 一个 Base Camera
- 不主动开启 `Opaque Texture`
- 只有当后续效果明确需要深度时才开启 `Depth Texture`
- 透明和后处理尽量保持单次穿过
- `Native RenderPass` 能合并的就合并

这种链路的核心目标是让最终颜色尽可能晚一点才落到独立 RT 上。对低档设备来说，最重要的不是“效果数目少”，而是“效果数目少的同时，结构也别散”。

### 重效果链

高档设备并不意味着可以随便多切 RT。它的正确做法是：允许更多效果，但尽量不改变链路骨架。

更合理的结构通常是：

- 先有 Shadow Map
- 再根据实际需要决定是否做 Depth Prepass
- Opaque 阶段尽量把主要几何一次写完
- 只有确实要被后续采样的结果，才引入 `CopyColor` 或 `CopyDepth`
- Transparent 和 Post 仍然尽量保持顺序清晰，不要在中间插入多个无意义的临时 RT

这和 {{< relref "performance/gpu-opt-06-urp-pipeline-config.md" >}} 里讲的 URP 关键配置是一致的：开关是结果，链路才是原因。
如果你想对照更接近真实 URP 的执行顺序，可以继续看 {{< relref "rendering/unity-rendering-09-urp-architecture.md" >}}。

### 错误链

错误链最常见的表象是：

- 画面功能很多
- 每个功能都能单独调
- 但 Frame Debugger 里一眼看过去就是一连串 `FinalBlit`、`CopyColor`、`CopyDepth`

这类链路的问题不是“它做了太多”，而是“它把可以共享的结果拆碎了”。在移动端 TBDR 架构里，这种拆碎非常容易把原本可以留在 tile memory 的内容赶到系统内存里。

## 中间结果怎么分级

不是所有中间结果都值得落地。更稳的做法，是先区分三类：

1. **一次性消费结果**  
   例如某个效果只在下一段 Pass 被读一次，读完就丢。这样的结果应该尽量短命。

2. **多次消费结果**  
   例如深度纹理、场景颜色纹理，后面有多个效果共同依赖。这样的结果才适合真正落成独立 RT。

3. **结构性结果**  
   例如 Shadow Map、GBuffer 这类本来就属于系统层的中间状态，它们值得落地，但也应该只在确实需要时出现。

| 中间结果 | 典型用途 | 什么时候保留 | 什么时候取消 |
| --- | --- | --- | --- |
| Depth Texture | Soft Particles、SSAO、少量后处理 | 有明确下游消费时 | 下游不读深度时 |
| Opaque Texture | Refraction、Distortion、部分屏幕空间效果 | 下游确实需要场景颜色时 | 只是“顺手开了”时 |
| Shadow Map | 光照采样 | 方向光或关键点光需要时 | 灯光本身不参与可见性时 |
| 临时 Color RT | 复杂后处理链、双缓冲效果 | 无法原地读写，且确实要 fork 时 | 只为单个效果额外开一层时 |

如果一个中间结果既不是结构必需，也不是多次复用，那它通常就不该存在。

## 在 URP 里怎么落地

这部分和 {{< relref "rendering/urp-platform-01-mobile.md" >}}、{{< relref "performance/gpu-opt-06-urp-pipeline-config.md" >}} 是直接相连的。

渲染链的顺序先定好，再去看 URP 资产里的开关：

- `Native RenderPass`：目标是让相邻 Pass 留在同一个 tile 过程中完成
- `Depth Priming`：只在 overdraw 明显严重且收益可证明时考虑
- `Intermediate Texture`：尽量当作例外，而不是默认路径
- `Render Scale`：一旦不是 1.0，就要高度警惕是否引入了不必要的中间 RT

对低档设备来说，理想情况通常是：

- 单个 Base Camera
- 最少的 Overlay
- 只保留必要的深度和颜色依赖
- 不让 `CopyColor` 和 `CopyDepth` 成为默认常态

对高档设备来说，理想情况不是“多开几个特效”，而是“同一张图里多出一些分支，但主干还在”。

## 这篇文章想让你最后记住什么

- Camera、Pass、RT 是三种不同职责，不要混用
- 渲染链的目标是控制依赖，而不是单纯增加表达力
- 中间结果越少越好，但前提是不要破坏必要依赖
- 高端和低端的差别，最好体现在参数和分支上，而不是体现在链路骨架上

如果你在 Frame Debugger 里看到的不是“清晰的主干 + 少量必要分支”，而是一串看不懂的 RT 轮换，那通常说明问题不在 shader，而在链路设计。

下一篇要讨论的就是：这条链上到底哪些特性应该保留，哪些应该降级，哪些应该直接替换掉。那就是 {{< relref "performance/rendering-tier-design-04-feature-matrix.md" >}} 要解决的事。

## 系列内导航

- 上一篇：[渲染系统分档设计 02｜怎么评价一套渲染系统：GPU Time 之外的五维健康度]({{< relref "performance/rendering-tier-design-02-health-model.md" >}})
- 下一篇：[渲染系统分档设计 04｜特性怎么分高中低档：阴影、透明、后处理、AO、反射的保留顺序与 fallback]({{< relref "performance/rendering-tier-design-04-feature-matrix.md" >}})
- 回到入口：[机型分档专题入口｜先定分档依据，再接配置、内容、线上治理与验证]({{< relref "performance/device-tiering-series-index.md" >}})
