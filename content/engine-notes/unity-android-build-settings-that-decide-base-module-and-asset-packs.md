---
date: "2026-04-01"
title: "Unity 里哪些设置真的决定 base module 和 asset packs"
description: "把 Build App Bundle、Split Application Binary、useAPKExpansionFiles 和 PLAY_ASSET_PACKS 放到同一张图里，讲清 Unity 在 AAB 时代到底是谁在决定 base、asset packs 和 APK/OBB 的分叉。"
slug: "unity-android-build-settings-that-decide-base-module-and-asset-packs"
weight: 2262
featured: false
tags:
  - "Unity"
  - "Android"
  - "AAB"
  - "Play Asset Delivery"
  - "Build Settings"
series: "Unity Android 发布与包体"
primary_series: "unity-android-aab-pad"
series_role: "article"
series_order: 30
---

> 先记一句最短结论：`Build App Bundle` 选发布格式，`Split Application Binary` 决定 Unity 有没有把内容拆成 `base module` 和 `asset packs`，`useAPKExpansionFiles` 只管 APK 时代的 `.obb`，而 `PLAY_ASSET_PACKS` 决定自定义 Gradle 模板能不能把这些包真正带进最终的 AAB。

这篇只做一件事：把 Unity Android 构建里最容易混在一起的几个开关，放回它们各自负责的层。  
不讲资源到底该不该进首包，不讲哪张贴图太大，也不讲怎么拆内容。那些是下一篇的事。

## 先把四个名词放回各自的层

如果把 Unity 的 Android 发布链路压成一张图，最少要分成四层：

| 设置 | 主要管什么 | 打开后的结果 | 最容易被误解成 |
|---|---|---|---|
| `Build App Bundle (Google Play)` | 选择发布格式 | Unity 走 AAB；不勾则走 APK | 这就是“拆包”开关 |
| `Split Application Binary` | 选择是否进入 Unity 的内容拆分路径 | AAB 时生成 `base module + asset packs`；APK 时生成 `apk + .obb` | AAB 独有的附属开关 |
| `useAPKExpansionFiles` | APK 时代的扩展文件机制 | 把数据放进 `.obb` | 还能决定 AAB 里 base 进不进得去 |
| `PLAY_ASSET_PACKS` | 自定义 Gradle 模板里的 asset pack 入口 | 让 Unity 生成的 asset packs 被正确带进最终 AAB | 可有可无的模板变量 |

这四个东西的关系不是并列替代，而是层级关系。  
先选发布格式，再决定是否拆内容，最后才轮到模板把 asset packs 带进构建产物。

## `Build App Bundle`：先决定你是在发 APK 还是 AAB

Unity 的 `Build App Bundle (Google Play)` 只回答一个问题：**这次构建的发布格式是什么。**

在 [Unity 的 Android 构建说明](https://docs.unity3d.com/cn/2022.2/Manual/android-BuildProcess.html) 里，这个开关对应的就是 `APK` 和 `Android App Bundle (AAB)` 两条路。  
在 [Google Play 的发布说明](https://docs.unity3d.com/cn/2022.2/Manual/android-distribution-google-play.html) 里，Unity 也直接把它写成了 Google Play 的 AAB 发布入口。

这里最容易忽略的一点是：  
`Build App Bundle` 不是“资源要不要拆出去”的开关，它只是告诉 Unity 这次最终要产出 `apk` 还是 `aab`。

如果你启用了 `Export Project`，这个选项还会被 `Export for App Bundle` 取代。  
也就是说，AAB 不是 Unity 里一个“额外模式”，而是 Android 发布格式的一种分支。

## `Split Application Binary`：真正决定 Unity 有没有进入拆分拓扑

真正决定“base module / asset packs”这条链路的，是 `Split Application Binary`。

Unity 的脚本 API 说明得很直白：当这个开关打开时，Unity 会把 player executable 和 data 拆开。  
在 APK 路径下，它生成的是 `.apk + .obb`；在 AAB 路径下，它生成的是 `base module + asset packs`。  
Unity 官方对 AAB 的描述也一致：`base module` 放 executable、插件和第一场景，`asset packs` 放剩余场景、资源和 Streaming Assets。

这意味着：

- 你想走 Google Play 的 AAB 拆分路径，`Build App Bundle` 和 `Split Application Binary` 都要对。
- 你只开 `Build App Bundle`，但没进拆分路径，就不是 Unity 官方文档里描述的 PAD 拆分拓扑。
- 你只开 `Split Application Binary`，但还是 APK，那么你得到的是 `.obb`，不是 AAB 的 `asset packs`。

如果把它压成一句话，就是：

`Build App Bundle` 决定“发什么”，`Split Application Binary` 决定“怎么切”。

## `useAPKExpansionFiles`：它是 APK 时代的答案，不是 AAB 的答案

这是最常见的误解点。

`useAPKExpansionFiles` 这个名字本身就已经说明了它服务的对象：**APK Expansion Files**。  
Unity 早期文档里，它的语义就是：把主包和数据拆成 `apk + .obb`，其中 `.obb` 才是扩展数据文件。

所以，`useAPKExpansionFiles` 解决的是 APK 时代的发布问题。  
它不负责 AAB，也不负责 Play Asset Delivery。

现在再看这个误解就很清楚了：

- “没开 `useAPKExpansionFiles`，所以 AAB 报错了” 这句话本身就把两个时代混在一起了。
- 如果你在发 AAB，真正该看的不是 `useAPKExpansionFiles`，而是 `Build App Bundle`、`Split Application Binary`，以及后面的 PAD 模板是否完整。
- 如果你在发 APK，而且确实需要扩展文件，那才是 `useAPKExpansionFiles` 的场景。

Unity 的 PAD 文档也写得很直接：  
如果你要把应用发布到**不支持 AAB 的分发渠道**，你应该回到 APK 发布格式和 APK expansion files。  
换句话说，`useAPKExpansionFiles` 不是“修 AAB 报错”的开关，它是“回到 APK 旧路径”的开关。

Google Play 的帮助页也把这条时间线说得很清楚：`2021 年 8 月` 之后的新应用要求用 AAB，`2023 年 6 月 30 日` 之后 TV 应用更新也必须用 AAB。  
在这个时间点之后，再把 OBB 当成 AAB 的解决方案，就已经是路径错位了。

## `PLAY_ASSET_PACKS`：旧 Gradle 模板最容易漏掉的那一层

即使你已经开了 AAB 和 `Split Application Binary`，还会卡在一个更隐蔽的地方：**自定义 Gradle 模板。**

Unity 的 PAD 文档明确说了，`PLAY_ASSET_PACKS` 是 Unity 用来把 asset packs 写进最终 AAB 的模板变量。  
如果你的 `mainTemplate.gradle` 是在 Unity 支持 PAD 之前就拷出来的，它可能根本没有这个变量。

这类问题的典型特征是：

- 你以为自己已经“开了 AAB 和拆分”。
- 但是最终 AAB 里并没有按预期把 asset packs 挂进去。
- 结果看起来像“Unity 失效了”，实际上是旧模板把这条变量链路截断了。

所以处理这类项目时，最稳的做法不是在旧模板上硬补，而是：

1. 用当前 Unity 版本重新生成 Gradle 模板。
2. 再把你自己的改动一项项迁回去。
3. 确认 `PLAY_ASSET_PACKS` 仍然存在。

这一步很枯燥，但它比“反复怀疑 Unity 构建器”更接近真相。

## 一张最小映射表

如果你现在只想快速判断“这个项目到底走到了哪一步”，可以先看这张最小表：

| 组合 | 产物形态 | 说明 |
|---|---|---|
| `Build App Bundle` 关，`Split Application Binary` 关 | `apk` | 最普通的 APK 路径 |
| `Build App Bundle` 关，`Split Application Binary` 开 | `apk + .obb` | APK 时代的扩展文件路径 |
| `Build App Bundle` 开，`Split Application Binary` 关 | AAB，但没有进入 Unity 官方文档描述的 PAD 拆分路径 | 这时不要把 `useAPKExpansionFiles` 当成修复手段 |
| `Build App Bundle` 开，`Split Application Binary` 开，且模板完整 | `aab + base module + asset packs` | 这是 Google Play 上真正要走的路径 |

这张表里最重要的不是产物名字，而是层次：

`AAB` 不是“更大的 APK”，`asset packs` 也不是“新名字的 OBB”。  
它们是两套不同的发布模型。

## 所以，遇到 AAB 报错时先看什么

如果你现在的目标是把问题快速缩到设置层，而不是内容层，优先顺序应该是：

1. `Build App Bundle` 是否真的打开了。
2. `Split Application Binary` 是否打开了。
3. 自定义 Gradle 模板里是否还保留 `PLAY_ASSET_PACKS`。
4. 有没有把 `useAPKExpansionFiles` 当成 AAB 的修复按钮。

做到这一步，你才能再去问下一层的问题：  
哪些内容进了 `base module`，哪些内容进了 `asset packs`，哪些内容根本就不该留在首包里。

下一篇就专门回答这个问题。

## 参考

- [Unity: Delivering to Google Play](https://docs.unity3d.com/cn/2022.2/Manual/android-distribution-google-play.html)
- [Unity: Set up Play Asset Delivery](https://docs.unity3d.com/cn/2022.2/Manual/android-asset-packs-set-up.html)
- [Unity: Asset packs in Unity](https://docs.unity3d.com/cn/2023.2/Manual/android-asset-packs-in-unity.html)
- [Unity: `PlayerSettings.Android.splitApplicationBinary`](https://docs.unity3d.com/cn/2023.2/ScriptReference/PlayerSettings.Android-splitApplicationBinary.html)
- [Unity: `PlayerSettings.Android.useAPKExpansionFiles`](https://docs.unity3d.com/cn/2017.3/ScriptReference/PlayerSettings.Android-useAPKExpansionFiles.html)
- [Google Play app size limits](https://support.google.com/googleplay/android-developer/answer/9859372)
- [APK expansion files](https://support.google.com/googleplay/android-developer/answer/2481797)
