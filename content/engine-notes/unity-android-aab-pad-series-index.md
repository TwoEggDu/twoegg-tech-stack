---
date: "2026-04-01"
title: "Unity Android AAB / PAD 系列索引：从 APK 心智切到 base module 与 asset packs"
description: "给 Unity Android 发布与包体这组文章补一个稳定入口：先讲 Google Play 为什么从 APK + OBB 走到 AAB + PAD，再把 Unity 设置、base module 超限和实战排查接成一条线。"
slug: "unity-android-aab-pad-series-index"
weight: 72
featured: false
tags:
  - "Unity"
  - "Android"
  - "AAB"
  - "PAD"
  - "Package Size"
  - "Index"
series: "Unity Android 发布与包体"
series_id: "unity-android-aab-pad"
series_role: "index"
series_order: 0
series_nav_order: 58
series_title: "Unity Android 发布与包体"
series_entry: true
series_audience:
  - "Unity 客户端"
  - "资源 / 构建 / 发布"
series_level: "进阶"
series_best_for: "当你已经在做 Unity Android 包体治理，但团队心智还停留在 APK / OBB 时代"
series_summary: "把 Google Play 规则、AAB / PAD、Unity 打包设置、base module 边界和超限排查串成一条可执行的工程链路"
series_intro: "这组文章不做泛泛的 Android 科普，也不重讲全部 Unity Build Settings。它只处理一个高频工程问题：为什么项目已经切到 AAB，团队却还在用 APK / OBB 时代的心智理解首包、资源拆分和上传限制，最后把问题集中爆在 base module 上。"
series_reading_hint: "第一次系统读，建议先看模型迁移和平台口径，再看 Unity 设置映射、内容来源和排查动作；如果你手上已经有 Play Console 的报错截图，可以从第 3 篇直接开始。"
---
> 这页是 “Unity Android 发布与包体” 的入口页。它不负责展开所有细节，只负责先把地图立住，让读者知道自己现在卡在平台规则、Unity 设置、资源来源，还是排查动作这一层。

如果你已经在看更大的资源交付主线，也可以顺手对照：

- [Unity 资源交付工程实践：分组、命名、版本、缓存、回滚和烟测基线]({{< relref "engine-notes/unity-resource-delivery-engineering-practices-baseline.md" >}})
- [Resources、StreamingAssets、AssetBundle、Addressables 到底各自该在什么场景下用]({{< relref "engine-notes/unity-resources-streamingassets-assetbundle-addressables-when-to-use.md" >}})
- [游戏预算管理 02｜包体预算怎么定：首包、全量包、热更和常驻资源为什么不能混成一个数字]({{< relref "engine-notes/game-budget-02-package-budget.md" >}})

这组文章补的不是“资源怎么分组”这类长期治理，而是 AAB 时代最容易让团队继续沿用旧心智的那一段：

`Google Play 到底在限制什么，Unity 到底把什么放进了 base，为什么你以为已经分包了，结果首包还是超了。`

## 这组文章要回答什么

这组文章主要回答 5 个问题：

1. Google Play 为什么从 `APK + OBB` 走到 `AAB + PAD`。
2. Play Console 报 `base > 200MB` 时，到底在算什么大小。
3. Unity 里哪些设置真的会改 `base module` 和 `asset packs` 的拓扑。
4. 为什么 `first scene`、`StreamingAssets`、`so` 和 ABI 这些内容最容易继续留在 `base`。
5. 真正遇到超限时，团队应该按什么顺序排、怎么改、怎么进 CI。

## 先给一句总判断

如果把这组文章压成一句话，我会这样描述：

`Unity Android 的 AAB 问题，真正难的不是“知道 AAB 是什么”，而是把团队从 APK / OBB 时代的心智切到“谁进 base、谁进 asset packs、平台到底按什么口径报错”的新地图上。`

所以这组文章故意不做百科式平铺，而是按工程链路拆成 5 篇：

- 先讲发布模型为什么改了
- 再讲 Play Console 到底在看什么
- 再讲 Unity 设置怎样映射到平台产物
- 再讲哪些内容最容易错误留在 `base`
- 最后收口成一套可执行的排查动作

## 最短阅读路径

如果你第一次系统读，我建议按这条最短路径走：

0. [Unity Android AAB / PAD 系列索引：从 APK 心智切到 base module 与 asset packs]({{< relref "engine-notes/unity-android-aab-pad-series-index.md" >}})
   先建地图，不然很容易把“平台限制”“Unity 设置”“资源来源”“排查动作”混成同一个问题。

1. [从 APK + OBB 到 AAB + PAD：Google Play 为什么改了发布模型]({{< relref "engine-notes/unity-android-from-apk-obb-to-aab-pad.md" >}})
   先把发布模型从“上传一个通用安装包”切到“上传一个发布格式，由平台按设备生成下载内容”。

2. [Play Console 报 base 超 200MB，到底在算什么]({{< relref "engine-notes/unity-android-aab-size-limits-and-what-base-200mb-means.md" >}})
   再统一 size 口径，避免把 `.aab` 文件大小、下载大小和安装后占用混在一起。

3. [Unity 里哪些设置真的决定 base module 和 asset packs]({{< relref "engine-notes/unity-android-build-settings-that-decide-base-module-and-asset-packs.md" >}})
   这时再回头看 Unity，知道哪些开关真会改 AAB 拓扑，哪些只是 APK 时代残留心智。

4. [为什么你的 Unity 资源还在进 base：首场景、StreamingAssets、so 和 ABI]({{< relref "engine-notes/unity-android-why-assets-still-go-into-base-module.md" >}})
   再看内容来源，知道哪些东西最容易表面“已经拆包”，实际上仍然留在安装时下载里。

5. [Unity AAB 超限实战：base > 200MB 时怎么排、怎么改、怎么做 CI]({{< relref "engine-notes/unity-android-base-module-over-200mb-diagnosis-and-fixes.md" >}})
   最后把前 4 篇收口成排查流和治理顺序。

## 如果你是带着报错来查

如果你不是系统读，而是已经拿着 Play Console 报错来定位，我更建议按问题跳：

### 1. 你们团队还在争论 “是不是没开 OBB / expansion files”

先看：

- [从 APK + OBB 到 AAB + PAD：Google Play 为什么改了发布模型]({{< relref "engine-notes/unity-android-from-apk-obb-to-aab-pad.md" >}})
- [Unity 里哪些设置真的决定 base module 和 asset packs]({{< relref "engine-notes/unity-android-build-settings-that-decide-base-module-and-asset-packs.md" >}})

这一组负责把 `useAPKExpansionFiles` 放回 APK 时代语境，把 `splitApplicationBinary` 和 PAD 放回 AAB 时代语境。

### 2. 你们现在最混乱的是 “到底超的是哪个大小”

先看：

- [Play Console 报 base 超 200MB，到底在算什么]({{< relref "engine-notes/unity-android-aab-size-limits-and-what-base-200mb-means.md" >}})

这一篇只负责统一平台口径，不提前跳到资源和构建细节。

### 3. 你们已经开了 AAB / PAD，但首包还是很大

先看：

- [Unity 里哪些设置真的决定 base module 和 asset packs]({{< relref "engine-notes/unity-android-build-settings-that-decide-base-module-and-asset-packs.md" >}})
- [为什么你的 Unity 资源还在进 base：首场景、StreamingAssets、so 和 ABI]({{< relref "engine-notes/unity-android-why-assets-still-go-into-base-module.md" >}})

前者负责看“拆对了没有”，后者负责看“东西到底留在哪了”。

### 4. 你们现在就是要尽快止血

直接看：

- [Unity AAB 超限实战：base > 200MB 时怎么排、怎么改、怎么做 CI]({{< relref "engine-notes/unity-android-base-module-over-200mb-diagnosis-and-fixes.md" >}})

这篇不是为了补概念，而是为了给一次超限事故一个稳定的排查顺序。

## 这组文章和现有旧文怎么分工

如果你已经看过下面这些旧文，这组文章会更容易挂回原来的知识地图：

- [Unity Player Settings 总览｜哪些参数真的会影响裁剪、构建速度和运行时]({{< relref "engine-notes/unity-player-settings-build-runtime-overview.md" >}})
- [Unity 资源交付工程实践：分组、命名、版本、缓存、回滚和烟测基线]({{< relref "engine-notes/unity-resource-delivery-engineering-practices-baseline.md" >}})
- [Resources、StreamingAssets、AssetBundle、Addressables 到底各自该在什么场景下用]({{< relref "engine-notes/unity-resources-streamingassets-assetbundle-addressables-when-to-use.md" >}})
- [Unity on Mobile｜Android 专项：Vulkan vs OpenGL ES、Adaptive Performance 与包体优化]({{< relref "engine-notes/mobile-unity-android.md" >}})
- [游戏预算管理 02｜包体预算怎么定：首包、全量包、热更和常驻资源为什么不能混成一个数字]({{< relref "engine-notes/game-budget-02-package-budget.md" >}})

它们各自已经讲过：

- 更大的 Player Settings 地图
- 更大的资源交付治理地图
- 资源归属方式的分工
- Android 移动平台上的综合专项
- 包体预算应该怎样拆账

这组新文章补的是中间那条最容易让团队继续沿用旧心智的桥：

`从 Google Play 规则，到 Unity 打包设置，到 base module 边界，到真实排查动作。`

## 为什么这组文章值得单独成串

因为这类问题最典型的症状不是“不会用某个 Unity API”，而是下面这些判断同时在团队里打架：

- “我们不是已经改成 AAB 了吗？”
- “那是不是没开 OBB？”
- “为什么 `.aab` 文件看起来没那么大，Play Console 还报错？”
- “不是已经上 Addressables 了吗，怎么资源还在 base？”
- “到底该让谁先改：构建、资源、程序、TA 还是发布？”

只要这几个问题还混在一起，团队就会不断在同一类问题上重复消耗。

这组文章要做的，不是再提供一批零散技巧，而是先把这几层重新拆开。

{{< series-directory >}}
