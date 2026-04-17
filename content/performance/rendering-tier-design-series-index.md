---
date: "2026-04-13"
title: "渲染系统分档设计系列索引｜从契约到验证闭环的完整分档治理"
description: "给渲染系统分档设计补一个入口：从分档契约、健康度模型、管线结构、特性矩阵、材质 Shader 治理到验证闭环。"
slug: "rendering-tier-design-series-index"
weight: 1
featured: false
tags:
  - "Performance"
  - "Tiering"
  - "Rendering"
  - "Index"
series: "渲染系统分档设计"
series_id: "rendering-tier-design"
series_role: "index"
series_order: 0
series_nav_order: 60
series_title: "渲染系统分档设计"
series_audience:
  - "客户端 / 图形程序"
  - "TA"
series_level: "进阶"
series_best_for: "当你想把渲染分档从拍脑袋配参数升级成有契约、有矩阵、有验证的工程治理"
series_summary: "把渲染分档从契约定义、健康度模型、管线结构、特性矩阵、材质治理到验证闭环串成一条完整链"
series_intro: "这组文章不是在教你怎么配 Quality Settings，而是在回答：不同档位的渲染配置怎样从契约定义开始，经过健康度评估、管线结构设计、特性矩阵裁剪、材质 Shader 治理，最后用验证闭环收住。"
series_reading_hint: "建议按编号顺序读，6 篇形成完整闭环。"
last_reviewed: "2026-04-17"
---
> 这页是渲染系统分档设计的系列入口。6 篇文章从契约定义走到验证闭环，建议按顺序读完，也可以带着具体问题跳到对应位置。

## 6 篇主线

1. [渲染系统分档设计 01｜先定体验合同和预算合同：低档保什么，高档加什么]({{< relref "performance/rendering-tier-design-01-contracts.md" >}})
   起点不是参数表，而是两份合同：体验合同定义每一档保什么体验底线，预算合同定义每一档花什么。后面所有配置、Shader 门禁和验证都从这两份合同出发。

2. [渲染系统分档设计 02｜怎么评价一套渲染系统：GPU Time 之外的五维健康度]({{< relref "performance/rendering-tier-design-02-health-model.md" >}})
   只看 GPU Time 只知道超没超，不知道是哪一层在耗。这篇把健康度拆成 GPU 总压力、外部带宽 / RT 压力、可见性剔除效率、Shader / Fragment 压力、几何 / Tiler 压力五维，建立项目可用的量化基线。

3. [渲染系统分档设计 03｜渲染链怎么设计：Camera、Pass、RT 与中间结果的组织原则]({{< relref "performance/rendering-tier-design-03-pipeline-structure.md" >}})
   Camera 负责边界，Pass 负责顺序，RT 负责落点。这篇从依赖关系出发讲清楚一条面向分档的渲染链应该怎样组织，以及怎样避免多余的 RT 切换把移动端和高端机一起拖慢。

4. [渲染系统分档设计 04｜特性怎么分高中低档：阴影、透明、后处理、AO、反射的保留顺序与 fallback]({{< relref "performance/rendering-tier-design-04-feature-matrix.md" >}})
   特性不是开关，而是保留、降级、替换、关闭四种处理方式。这篇把阴影、透明、后处理、AO、反射等特性拆成一张可执行的分档矩阵，回答哪些值得保留、哪些适合降级、哪些应该直接替换。

5. [渲染系统分档设计 05｜材质与 Shader 治理：关键词、变体、模板与质量门禁]({{< relref "performance/rendering-tier-design-05-material-shader-governance.md" >}})
   分档最后失效，往往不是 Quality Level 没配对，而是材质和 Shader 没被纳入同一套治理规则。这篇把关键词归属、变体保留与剔除、Renderer Feature 绑定和构建时质量门禁收成一条治理链。

6. [渲染系统分档设计 06｜怎么保证长期质量：设备矩阵、黄金场景、热机、线上回写与回归]({{< relref "performance/rendering-tier-design-06-validation-loop.md" >}})
   设计完了还要证明它长期没坏。这篇把设备矩阵选型、黄金场景固定、热机与长时运行测试、线上回写和回归门禁串成一条验证闭环。

## 如果你带着具体问题来

- 不知道分档的目标应该怎么定：
  先看 [01 体验合同和预算合同]({{< relref "performance/rendering-tier-design-01-contracts.md" >}})，把"低档保什么、高档加什么"这句话写成可执行的边界。

- 帧时间达标了，但总感觉系统不健康：
  先看 [02 五维健康度]({{< relref "performance/rendering-tier-design-02-health-model.md" >}})，把 GPU Time 之外的带宽、剔除、Shader 和几何压力一起拉出来。

- 渲染链越改越乱，到处是 CopyColor 和额外 Camera：
  先看 [03 渲染链组织原则]({{< relref "performance/rendering-tier-design-03-pipeline-structure.md" >}})，从依赖关系重新理清 Camera、Pass、RT 的职责。

- Shader 变体膨胀、关键词谁都能加：
  先看 [05 材质与 Shader 治理]({{< relref "performance/rendering-tier-design-05-material-shader-governance.md" >}})，把关键词归属和构建时门禁收住。

{{< series-directory >}}
