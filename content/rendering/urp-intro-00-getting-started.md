---
title: "URP 从零上手｜新建项目、认识三件套、改第一个参数看到画面变化"
slug: "urp-intro-00-getting-started"
date: "2026-04-14"
description: "不讲渲染管线理论，只做一件事：10 分钟内建好 URP 项目、认识 Pipeline Asset / Universal Renderer / Renderer Feature 三个面板、改两个参数看到画面变化。"
tags:
  - "Unity"
  - "URP"
  - "入门"
  - "渲染管线"
series: "URP 深度"
weight: 1492
---
URP 不是"另一套渲染引擎"，它就是 Unity 渲染流程的一种组织方式。你已经在用的灯光、材质、摄像机都还在，URP 只是把渲染的执行顺序和配置方式换了一套结构。

这篇只有一个目标：让你在 10 分钟内建好 URP 项目、认识三个核心面板、改一个参数看到画面变化。原理和底层机制全部在后续文章里展开。

---

## 新建 URP 项目

打开 Unity Hub，点 **New project**，选择 **3D (URP)** 模板。

如果你的 Unity Hub 里看不到这个模板，确认 Unity 版本在 2021.3 以上——URP 模板从这个版本起是默认选项。

项目创建完成后，打开默认场景，你会看到一个带光照和阴影的示例场景。这个场景已经运行在 URP 上了——URP 模板帮你做好了所有配置。

---

## 认识 URP 的三件套

URP 的配置分布在三个资产上，它们之间是**从上到下的挂载关系**：

```
Project Settings → Graphics
  └─ Pipeline Asset（全局渲染配置）
       └─ Universal Renderer（渲染器，决定怎么画）
            └─ Renderer Feature 列表（可选的扩展功能）
```

### 第一个：Pipeline Asset

**在哪找**：`Edit → Project Settings → Graphics`，最上面的 **Scriptable Render Pipeline Settings** 字段里挂的就是它。也可以在 Project 窗口搜索 `t:UniversalRenderPipelineAsset`。

**它是什么**：URP 的全局配置入口。HDR、阴影、抗锯齿、渲染精度——所有影响画面质量和性能的全局开关都在这里。

**长什么样**：选中它后，Inspector 里会看到几个折叠区块——Rendering、Quality、Lighting、Shadows、Post-processing。每个区块里的参数都直接控制渲染行为。

### 第二个：Universal Renderer

**在哪找**：选中 Pipeline Asset 后，Inspector 里有一个 **Renderer List**，里面挂的就是 Universal Renderer。点击它可以跳转到 Renderer 的 Inspector。

**它是什么**：决定"用什么方式画"。渲染路径（Forward / Deferred）、Depth Priming、Native RenderPass 等选项在这里配置。

### 第三个：Renderer Feature 列表

**在哪找**：Universal Renderer 的 Inspector 底部，有一个 **Renderer Features** 区域和一个 **Add Renderer Feature** 按钮。

**它是什么**：URP 的扩展点。当 URP 内置功能不够用时（比如你想加一个自定义后处理、描边效果），就在这里挂自己写的 Renderer Feature。

现在先知道它在哪就行，后续 [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}}) 会从零写一个完整的 Renderer Feature。

---

## 改第一个参数：关掉阴影

1. 选中你的 Pipeline Asset
2. 找到 **Shadows** 区块 → **Main Light** → 取消勾选 **Cast Shadows**
3. 看 Scene View 或 Game View——场景里所有物体的阴影消失了
4. 重新勾上 Cast Shadows，阴影回来

一句话总结：**Pipeline Asset 里的每个开关都直接控制渲染行为。** 关掉阴影就是告诉 URP"这一帧不需要做 Shadow Map 渲染"，GPU 直接跳过这步。

---

## 改第二个参数：调 Render Scale

1. 还是在 Pipeline Asset 里，找到 **Quality** 区块 → **Render Scale**
2. 把滑块拖到 **0.5**
3. 看 Game View——整体画面变糊了，但 UI 元素不受影响
4. 拖到 **1.5**——画面变锐（超采样），但 GPU 压力增大
5. 拖回 **1.0** 恢复默认

一句话总结：**Render Scale 控制实际渲染分辨率。** 0.5 意味着只用一半分辨率渲染，然后放大到屏幕。移动端项目常用 0.75–0.85 配合上采样，在中低端机上换取帧率。后续 [URP 深度配置 01｜Pipeline Asset 解读]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}}) 会逐个讲清楚每个参数背后的渲染行为。

---

## 下一步去哪

你现在已经知道 URP 的三件套在哪、改参数能直接影响画面。接下来看你的需求：

- **想逐个了解 Pipeline Asset 每个参数的含义** → [URP 深度配置 01｜Pipeline Asset 解读：每个参数背后的渲染行为]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})
- **想先补渲染基础概念**（什么是 Pass、Draw Call、Render Target）→ [URP 架构详解：从 Asset 到 RenderPass 的层级结构]({{< relref "rendering/unity-rendering-09-urp-architecture.md" >}})
- **遇到问题**（粉色材质、后处理不生效、阴影消失）→ [URP 常见问题速查]({{< relref "rendering/urp-troubleshooting-quick-reference.md" >}})
- **准备好系统学习 URP** → 回到 [URP 深度系列索引]({{< relref "rendering/urp-deep-dive-series-index.md" >}})，按完整阅读顺序从前置三篇开始
