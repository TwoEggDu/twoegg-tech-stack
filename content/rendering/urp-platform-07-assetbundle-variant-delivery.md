---
title: "URP 深度平台 07｜AssetBundle 里的 Shader Variant 交付：多档位项目的打包、预热与版本对齐"
slug: "urp-platform-07-assetbundle-variant-delivery"
date: "2026-04-14"
description: "URP 多质量档 + AssetBundle 是 Shader Variant 问题最容易爆发的场景。本篇讲清楚 Player Build 与 AB Build 的变体差异、三种解决方案的优缺点、热更新时的版本对齐策略、以及 CI 验证方法。"
tags:
  - "Unity"
  - "URP"
  - "AssetBundle"
  - "Shader Variant"
  - "热更新"
  - "构建"
series: "URP 深度"
weight: 1710
---
> **读这篇之前**：Shader Variant 的完整生命周期见：
> - [Shader 变体全流程总览]({{< relref "rendering/unity-shader-variant-full-lifecycle-overview.md" >}})
>
> URP 多档位配置见：
> - [URP 深度平台 02｜多平台质量分级]({{< relref "rendering/urp-platform-02-quality.md" >}})

Shader Variant 的完整原理和排障方法在变体系列里已经讲透了。这篇不重复那些内容，只聚焦一个具体场景：**URP 项目 + 多质量档 + AssetBundle 打包**——这个三角关系下，变体问题为什么特别容易爆发，以及怎么解。

---

## 一、问题定义：为什么这个三角关系容易出事

一个 URP 项目如果同时满足下面三个条件，Shader Variant 出问题的概率会比普通项目高一个量级：

1. **多 Quality Level**：项目有 Low / Medium / High 三档（或更多），每档对应一个独立的 `UniversalRenderPipelineAsset`。
2. **不同档位的 Feature Toggle 不同**：Low 档关掉了 HDR、SSAO、Additional Lights；High 档全开。这些开关直接决定了 URP 内部哪些 `shader_feature` keyword 被激活。
3. **用 AssetBundle 做资源交付**：材质和 Shader 打进 AB，运行时从 AB 加载。

三个条件分别看都很正常，但组合到一起时，问题出在 Unity 构建 AssetBundle 的方式上。

核心矛盾可以用一句话概括：

`Player Build 看到了所有 Quality Level 的 Pipeline Asset，AssetBundle Build 只看到一个。`

这意味着 AB 里的变体集合是按某一个 Pipeline Asset 编译的，但运行时激活的可能是另一个 Pipeline Asset——keyword 不匹配，变体找不到，粉红色材质就来了。

---

## 二、Player Build 与 AssetBundle Build 的变体差异

理解这个问题需要知道两种构建路径在变体处理上的根本区别。

**Player Build** 的行为：

- Unity 遍历 `QualitySettings` 里所有 Quality Level 对应的 `UniversalRenderPipelineAsset`。
- 从每个 Pipeline Asset 收集它激活的 keyword 集合。
- 对所有 Quality Level 的 keyword 取**并集**，作为 Shader Prefiltering 的输入。
- 结果：Player 里包含了**所有档位可能用到的变体**。

**AssetBundle Build** 的行为：

- Unity 不读 `QualitySettings`。
- 它只看 `GraphicsSettings.defaultRenderPipeline`（或 `GraphicsSettings.currentRenderPipeline`）指向的那一个 Pipeline Asset。
- 变体裁剪只根据这一个 Pipeline Asset 的 keyword 来做。
- 结果：AB 里只包含**一个档位对应的变体子集**。

关于这个差异的技术细节，见：[Player Build 与 AssetBundle Build 的变体差异]({{< relref "rendering/unity-shader-variant-build-receipts-player-vs-ab.md" >}})。

实际后果举一个例子：

- 构建 AB 时，`GraphicsSettings` 里的默认 Pipeline Asset 是 Low 档。
- Low 档关闭了 `_ADDITIONAL_LIGHTS`。
- AB 构建阶段 Unity 认为不需要 `_ADDITIONAL_LIGHTS` 的变体，直接裁掉。
- 运行时用户设备被判定为 High 档，切换到 High Pipeline Asset，URP 启用了 `_ADDITIONAL_LIGHTS`。
- Shader 在 AB 里找不到对应变体——fallback 到 error shader，屏幕上看到粉红色。

这个问题的阴险之处在于：**开发机上通常不会触发**。因为开发机往往跑的就是默认档位，只有在真机切到非默认档位时才出现。

---

## 三、三种解决方案

| 方案 | 做法 | 优点 | 缺点 |
|---|---|---|---|
| A: Always Included + SVC | 把关键 Shader 加入 `Always Included Shaders`，用 `ShaderVariantCollection` 显式登记需要的变体 | 简单可控，不依赖构建脚本 | 包体增大；SVC 需要手动维护，漏了就缺变体 |
| B: 构建脚本切换 Pipeline Asset | 打 AB 前用脚本把每个 Quality Level 的 Pipeline Asset 依次设为 `GraphicsSettings` 默认，每次构建一批 AB，最后取并集 | 变体覆盖完整，和 Player Build 对齐 | 构建时间翻倍甚至更多；脚本复杂，要处理增量构建缓存失效 |
| C: Shader 放 Player Build，不进 AB | Shader 和关键材质走 Player Build（Resources 或 Preloaded Assets），AB 里只放非 Shader 资源（Mesh、Texture、Prefab 引用已内置的材质） | 变体完全由 Player Build 控制，最安全 | Shader 不能通过 AB 热更新 |

**推荐**：

大多数项目用**方案 C** 最稳妥。Shader 的变体集合由 Player Build 保证完整，AB 侧不需要再操心变体裁剪问题。代价是 Shader 变更必须走完整的客户端版本更新，不能热更。

如果项目确实需要热更新 Shader（比如频繁调整材质效果），用**方案 B**。但要做好构建脚本的维护成本预算，并且在 CI 里加上变体集合对比验证。

方案 A 适合小规模项目或作为兜底手段——少量关键 Shader 加 Always Included，SVC 覆盖核心变体——但随着项目规模增长，SVC 的维护会越来越脆弱。

---

## 四、热更新场景：版本对齐策略

即使选了方案 C（Shader 不进 AB），热更新也不代表可以忽略变体问题。关键场景是：

**新版本修改了 Pipeline 配置。**

一个真实的例子：

- 1.1.0 版本的 High 档 Pipeline Asset 没有启用 SSAO。
- 1.2.0 版本给 High 档加了一个 `ScreenSpaceAmbientOcclusion` Renderer Feature。
- 这个 Renderer Feature 引入了 `_SCREEN_SPACE_OCCLUSION` keyword。
- 1.2.0 的 Player Build 会编译包含这个 keyword 的变体。
- 但如果用户的 AB 还停留在 1.1.0（没有跟随客户端更新），那些 AB 里的材质在运行时可能因为 keyword 状态变化而走到意料之外的分支。

反过来也一样：如果 Shader 走 AB 热更（方案 B），但客户端的 Pipeline Asset 没有同步更新，新 Shader 编译时依据的 keyword 集合和运行时实际激活的不一致。

核心原则：

`Pipeline 配置变化时，Shader 交付物必须同步更新。`

具体做法：

- 在版本管理中，给 Pipeline Asset 的变更打语义化标签。
- 构建系统检测到 Pipeline Asset 变更时，强制触发 Shader 重新构建。
- 客户端版本号和 Shader Bundle 版本号之间维护一张兼容性矩阵，不兼容的组合在启动时拒绝加载。

关于热更新场景下 Shader 交付架构的完整设计，见：[热更新场景下的 Shader 交付架构]({{< relref "rendering/unity-shader-delivery-in-hotupdate.md" >}})。

---

## 五、CI 验证：把问题拦在构建阶段

变体问题最大的特点是**出了 bug 难定位**——粉红色材质可能只在特定档位、特定设备、特定 AB 版本组合下出现。最有效的策略是在 CI 里加自动化验证，把问题拦在构建阶段。

**1. 变体数量基线对比**

每次构建后，从 shader compilation log 中提取每个 Shader 的变体数量，和上一次成功构建的基线做对比。

- 变体数显著增加：可能是新增了 keyword 或关闭了某个 stripping 规则，需要人工确认是否合理。
- 变体数显著减少：可能是 Pipeline Asset 配置变更导致 keyword 丢失，这比增加更危险。

**2. 多档位变体集合对比**

分别用每个 Quality Level 的 Pipeline Asset 做一次 AB 构建（或模拟构建），输出各自的变体列表，然后做 diff。

- 如果某个档位的变体集合是另一个的真子集，说明配置一致性还在。
- 如果出现了只属于某一个档位的独有变体，需要确认运行时是否真的只在该档位下使用。

**3. 真机冒烟测试**

自动化测试在每个目标档位启动应用，遍历主要场景，截屏检测粉红色像素。这是最后一道防线，成本最高但最可靠。

关于 CI 集成的完整方案，见：[Shader Variant 数量监控与 CI 集成]({{< relref "rendering/unity-shader-variant-ci-monitoring.md" >}})。

---

## 小结

URP 多质量档 + AssetBundle 这个组合之所以容易出变体问题，根本原因是 Player Build 和 AB Build 对 Pipeline Asset 的读取范围不一致。理解了这个差异，解决方案的选择就清楚了：

- 能接受 Shader 不热更 → 方案 C，Shader 放 Player Build
- 必须热更 Shader → 方案 B，构建脚本保证多档位覆盖
- Pipeline 配置变更 → Shader 交付物必须同步更新
- 所有方案都需要 CI 验证兜底

---

**延伸阅读**：

- [Shader 变体全流程总览]({{< relref "rendering/unity-shader-variant-full-lifecycle-overview.md" >}})
- [URP 深度平台 02｜多平台质量分级]({{< relref "rendering/urp-platform-02-quality.md" >}})
- [热更新场景下的 Shader 交付架构]({{< relref "rendering/unity-shader-delivery-in-hotupdate.md" >}})
