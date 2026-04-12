---
date: "2026-04-01"
title: "CachedShadows 阴影缓存专题索引｜先看懂工作原理，再学会验证与排查"
description: "给 TopHeroUnity 的 CachedShadows 补一条稳定入口：先看懂它替代了 URP 阴影链路里的哪一段，再顺着配置、生效、资源交付、验证与排查一路读下去。"
slug: "cachedshadows-series-index"
weight: 1700
featured: false
tags:
  - "Unity"
  - "URP"
  - "CachedShadows"
  - "Shadow"
  - "Index"
series: "CachedShadows 阴影缓存"
series_id: "cachedshadows"
series_role: "index"
series_order: 0
series_nav_order: 61
series_title: "CachedShadows 阴影缓存"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "图形 / 渲染程序"
series_level: "进阶"
series_best_for: "当你想把 TopHeroUnity 里的 CachedShadows 从工作原理、项目生效链、Shader 交付边界到验证排查一起看清"
series_summary: "把 CachedShadows 放回 TopHeroUnity 的真实工程里，讲清它为什么存在、怎么接管 URP 主光阴影、怎么验证，以及效果不对时该先查哪里。"
series_intro: "这组文章处理的不是抽象的“阴影基础”或零散的“某个选项怎么点”，而是 TopHeroUnity 当前主光 CachedShadows 这套真实系统。它从 URP 原生主光阴影链路出发，讲清 CachedShadows 替代了哪一段、一帧里如何缓存和叠加、为什么低配 Android 还能有影子，以及为什么编辑器正常、真机却可能失效。"
series_reading_hint: "如果你已经知道 Shadow Map、URP 层级、Renderer Feature 和 Quality 各在管什么，建议先按 01、02、03 顺着主线读，再看 04 和 05。若你是从 0 基础进来，先补 Shadow Map、URP 架构、Pipeline Asset、Renderer Feature 和多平台质量分级，再回到本索引。带着问题来查时，可以直接跳到 05 症状总表，再反查对应原理或资源边界文章。"
---
这组文章不是在重讲 `Shadow Map`、`Renderer Feature` 或 `SVC` 的基础，而是在补它们和项目真实系统之间那条最容易缺失的桥。

如果你现在面对的是下面这些问题，这组文章就是按这些问题来组织的：

- `AndroidPipelineAssetLow` 明明把主光阴影关了，为什么低配 Android 仍然可能有影子
- CachedShadows 到底替代了 URP 阴影链路里的哪一段
- 一帧里静态阴影、动态阴影和 receiver 之间到底怎么交接
- 为什么相机一动才刷新
- 为什么编辑器有，打包后却可能没有
- 效果不对时应该先查哪一层，而不是凭感觉乱试

## 先给一句总判断

如果只用一句话概括这整个专题，我会这样说：

`TopHeroUnity 的 CachedShadows 不是另一套光照系统，而是一套“接管主光阴影生成、仍沿用 URP receiver 语义、并且把静态成本尽量缓存下来”的工程化阴影方案。`

## 如果你是从 0 基础进来

这组文章本身不是从 `Shadow Map`、`URP` 层级结构或 `Renderer Feature` 入门开始讲的。  
如果你对下面这些词还不熟：

- `Shadow Map`
- `Pipeline Asset`
- `Renderer / Renderer Feature`
- `Quality`
- `Shader Variant`

更稳的顺序是先补这几篇，再回到本专题：

1. [Shadow Map 机制：生成、级联与阴影质量问题]({{< relref "rendering/unity-rendering-02b-shadow-map.md" >}})
2. [URP 架构详解：从 Asset 到 RenderPass 的层级结构]({{< relref "rendering/unity-rendering-09-urp-architecture.md" >}})
3. [URP 深度配置 01｜Pipeline Asset 解读：每个参数背后的渲染行为]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})
4. [URP 深度扩展 01｜Renderer Feature 完整开发：从零写一个 ScriptableRendererFeature]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})
5. [URP 深度平台 02｜多平台质量分级：三档配置的工程实现]({{< relref "rendering/urp-platform-02-quality.md" >}})
6. 回到这篇索引，再顺着 `01 -> 02 -> 03` 往下读

如果你一路读到 `04｜为什么编辑器有阴影、打包后可能没了` 时，发现自己卡在 `SVC / Hidden Shader / Variant` 这一层，再补这两篇：

- [SVC、Always Included、Stripping 到底各自该在什么场景下用]({{< relref "rendering/unity-svc-always-included-stripping-when-to-use-which.md" >}})
- [Unity Shader Variant 全流程总览：从生产、保留、剔除到运行时使用]({{< relref "rendering/unity-shader-variant-full-lifecycle-overview.md" >}})

这样读会比一上来直接冲 `CachedShadows 01` 更稳。

## 推荐阅读顺序
如果你已经大致知道 `Shadow Map`、`URP` 层级、`Renderer Feature` 和 `Quality` 各在管什么，建议按这个顺序：

0. 索引本身
1. [CachedShadows 阴影缓存 01｜它替代了 URP 主光阴影链路里的哪一段]({{< relref "rendering/cachedshadows-01-overview.md" >}})
2. [CachedShadows 阴影缓存 02｜一帧里到底发生了什么：静态缓存、动态叠加、手动刷新]({{< relref "rendering/cachedshadows-02-frame-flow.md" >}})
3. [CachedShadows 阴影缓存 03｜从 Quality 到 Camera：TopHeroUnity 里一个阴影是怎么真正被启用的]({{< relref "rendering/cachedshadows-03-activation-chain.md" >}})
4. [CachedShadows 阴影缓存 04｜为什么编辑器有阴影、打包后可能没了：SVC、Hidden Shader、StaticShaders 各管什么]({{< relref "rendering/cachedshadows-04-shader-delivery.md" >}})
5. [CachedShadows 阴影缓存 05｜症状总表：没影子、不刷新、只有 Editor 有、Android 没有时先查什么]({{< relref "rendering/cachedshadows-05-troubleshooting-symptoms.md" >}})
6. [CachedShadows 阴影缓存 06｜怎么证明当前阴影来自哪条链路：Frame Debugger、RenderDoc、日志各看什么]({{< relref "rendering/cachedshadows-06-validation-and-proof.md" >}})
7. [CachedShadows 阴影缓存 07｜阴影画质问题怎么查：锯齿、漂浮、漏光、抖动，到底该调哪一层]({{< relref "rendering/cachedshadows-07-visual-quality-debug.md" >}})
8. [CachedShadows 阴影缓存 08｜为什么低端 Android 选缓存阴影，而不是一直全量实时阴影]({{< relref "rendering/cachedshadows-08-tradeoffs-and-tiering.md" >}})

## 按主题分组去读

### 一、先看懂它到底是什么

- [01｜它替代了 URP 主光阴影链路里的哪一段]({{< relref "rendering/cachedshadows-01-overview.md" >}})
- [02｜一帧里到底发生了什么]({{< relref "rendering/cachedshadows-02-frame-flow.md" >}})

这一组回答的是：

- 它到底替代了什么
- 它不是在重写什么
- 静态缓存、动态叠加、receiver 交接分别站在哪

### 二、再看它在项目里为什么真的会生效

- [03｜从 Quality 到 Camera 的生效链路]({{< relref "rendering/cachedshadows-03-activation-chain.md" >}})
- [04｜Shader / Hidden Shader / preload 的交付边界]({{< relref "rendering/cachedshadows-04-shader-delivery.md" >}})

这一组回答的是：

- 当前平台到底有没有命中这套 Pipeline / Renderer / Feature
- 运行时到底从哪里拿到关键 shader
- 为什么 `featureReferences` 为空仍然可能工作
- 为什么 SVC 和 Hidden Shader 不是一回事

### 三、最后再看怎么验证和排查

- [05｜症状总表：先查什么]({{< relref "rendering/cachedshadows-05-troubleshooting-symptoms.md" >}})
- [06｜怎么证明当前阴影来自哪条链路]({{< relref "rendering/cachedshadows-06-validation-and-proof.md" >}})
- [07｜阴影画质问题怎么查]({{< relref "rendering/cachedshadows-07-visual-quality-debug.md" >}})

这一组回答的是：

- 完全没影子先看哪层
- 为什么 Editor 有、Android 没有
- 为什么相机动一下才有影子
- 怎么用 Frame Debugger / RenderDoc / 日志把“猜测”收成“证据”
- 阴影质量不好时先调哪一层

### 四、如果你已经能看懂实现，再回来看工程取舍

- [08｜为什么低端 Android 选缓存阴影，而不是一直全量实时阴影]({{< relref "rendering/cachedshadows-08-tradeoffs-and-tiering.md" >}})

这篇不负责帮你第一次看懂实现，它更像是把这套系统放回移动端成本结构里，回答：

`为什么这个项目会选它，而不是别的方案。`

## 如果你不是系统读，而是带着问题来查

### 我只想知道“为什么低配 Android 明明关了主光阴影还能有影子”

- 先看 [01｜它替代了 URP 主光阴影链路里的哪一段]({{< relref "rendering/cachedshadows-01-overview.md" >}})
- 再看 [03｜从 Quality 到 Camera 的生效链路]({{< relref "rendering/cachedshadows-03-activation-chain.md" >}})

### 我只想知道“它一帧里到底怎么省成本”

- 先看 [02｜一帧里到底发生了什么]({{< relref "rendering/cachedshadows-02-frame-flow.md" >}})
- 再看 [08｜为什么低端 Android 选缓存阴影]({{< relref "rendering/cachedshadows-08-tradeoffs-and-tiering.md" >}})

### 我现在最关心“编辑器有，真机没有”

- 先看 [05｜症状总表]({{< relref "rendering/cachedshadows-05-troubleshooting-symptoms.md" >}})
- 再看 [04｜Shader / Hidden Shader / preload 的交付边界]({{< relref "rendering/cachedshadows-04-shader-delivery.md" >}})
- 最后看 [06｜怎么证明当前阴影来自哪条链路]({{< relref "rendering/cachedshadows-06-validation-and-proof.md" >}})

### 我现在最关心“有阴影，但就是不刷新”

- 先看 [05｜症状总表]({{< relref "rendering/cachedshadows-05-troubleshooting-symptoms.md" >}})
- 再回看 [02｜一帧里到底发生了什么]({{< relref "rendering/cachedshadows-02-frame-flow.md" >}})
- 再看 [03｜从 Quality 到 Camera 的生效链路]({{< relref "rendering/cachedshadows-03-activation-chain.md" >}})

### 我已经知道问题大概在哪，但想拿证据

- 直接看 [06｜怎么证明当前阴影来自哪条链路]({{< relref "rendering/cachedshadows-06-validation-and-proof.md" >}})

### 我现在不是“有没有影子”，而是“阴影画质很差”

- 先看 [07｜阴影画质问题怎么查]({{< relref "rendering/cachedshadows-07-visual-quality-debug.md" >}})
- 再补 [unity-rendering-02b-shadow-map.md]({{< relref "rendering/unity-rendering-02b-shadow-map.md" >}})
- 再补 [urp-lighting-02-shadow.md]({{< relref "rendering/urp-lighting-02-shadow.md" >}})

## 这组文章接下来暂时没覆盖什么

这个专题第一期只聚焦 TopHeroUnity 当前主光 CachedShadows 主线，还没有系统展开：

- `CachedAdditionalShadowsRenderFeature` 的附加光缓存链
- 一个完整的“Editor 有、真机没有”的真实事故复盘
- 项目内专用调试面板或最小日志工具怎么做
- 脱离 TopHeroUnity 的通用插件介绍和跨项目复用策略

这些内容如果后面继续写，应该作为第二期，而不应该塞进当前第一期主线里把边界写散。

## 最短结论

`这组文章最重要的不是让你背住 CachedShadows 的每个类名，而是先给你一张稳定地图：它替代了什么、怎么生效、为什么真机会失效、该怎么证明、又该先查哪一层。`

{{< series-directory >}}



