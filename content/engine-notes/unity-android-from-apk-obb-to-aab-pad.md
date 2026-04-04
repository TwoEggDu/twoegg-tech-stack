---
date: "2026-04-01"
title: "从 APK + OBB 到 AAB + PAD：Google Play 为什么改了发布模型"
description: "把 Android 发布从 APK + OBB 迁移到 AAB + PAD 这条线讲清楚：AAB 是发布格式，不是终端安装包；OBB 属于 APK 时代；Google Play 为什么把分发责任收回到平台侧。"
slug: "unity-android-from-apk-obb-to-aab-pad"
weight: 2210
featured: false
tags:
  - "Unity"
  - "Android"
  - "Google Play"
  - "AAB"
  - "PAD"
  - "Delivery"
series: "Unity Android 发布与包体"
primary_series: "unity-android-aab-pad"
series_role: "article"
series_order: 10
---

> 如果这篇只记一句话，我建议记这句：`AAB 不是“另一个 APK”，它是 Google Play 用来生成设备专属安装包的发布格式；OBB 也不是 AAB 时代的主路，它属于 APK 时代的扩展文件方案。`

很多团队第一次撞到 Android 发布迁移，脑子里会同时装着三件事：

- 以前明明是 `APK + OBB`
- 现在为什么变成了 `AAB + PAD`
- 这到底是“打包方式换了”，还是“交付责任换了”

如果不先把这三个问题拆开，后面就很容易把技术问题写成一句模糊判断：

`是不是没开某个开关，所以资源才没进对地方？`

这篇只做一件事：把发布模型迁移的逻辑讲清楚。Unity 具体怎么配、`base` 为什么会超、哪些资源最容易留在首包里，放到后两篇再说。

## 先把结论摆出来

Google Play 之所以从 `APK + OBB` 走向 `AAB + PAD`，核心不是“换个文件后缀”，而是平台把两件事收回来了：

- 安装包怎么生成
- 大资源怎么分发

在旧模型里，开发者要自己面对一个“通用 APK”：

- 代码和资源尽量塞进一个安装包
- 超出的内容靠 OBB 补
- 用户拿到的是你事先打好的成品

在新模型里，开发者上传的是 `Android App Bundle`，Google Play 再根据设备配置生成并分发优化后的安装内容：

- 不同 `ABI`、语言、屏幕密度、资源变体会被拆分
- `base module` 保留启动必需内容
- 大资源可以交给 `Play Asset Delivery`

所以这次迁移的本质，不是“包更大了”或者“包更小了”，而是：

`从开发者自己做最终装箱，改成平台负责最后一公里分发。`

## APK + OBB 时代，问题出在“开发者自己装箱”

`APK + OBB` 的模型本身不难理解：`APK` 负责可执行代码和基础资源，`OBB` 负责放不进 APK 的大块补充内容。

它能工作，但有几个天然问题：

- 首包和扩展包是两套物理文件
- 用户安装后到底先拿到什么、后拿到什么，要靠项目自己兜底
- 更新、缓存、回滚、校验和热修补，都要团队自己设计
- 开发者很容易把“文件怎么拆”当成“内容边界怎么定义”

最典型的后果不是技术上做不到，而是工程上越来越像补丁堆补丁：

- 首包先放一点
- 后面不够再加 OBB
- 再不够就自己分包下载
- 最后每个团队都在维护一套“自己的资源分发规则”

这种方式短期看很灵活，长期看会把交付边界变得很脆弱。因为真正难的从来不是“怎么把文件放上去”，而是：

`哪些内容应该在安装时就拿到，哪些内容应该延后，哪些内容应该跟设备配置一起变化。`

## AAB 解决的不是“打包”，而是“分发”

`AAB` 的关键点在于，它不是最终安装包，而是发布给 Google Play 的输入。

Google Play 会基于这个输入，生成针对具体设备的优化安装内容。也就是说：

- 开发者上传的是“全集”
- 平台分发的是“设备所需子集”
- 用户下载的不是你手上那个单一成品，而是平台生成的设备专属结果

这也是为什么 Google 官方把 `Android App Bundle` 直接定义成 `publishing format`，并明确说明它会把 APK 的生成和签名交给 Google Play。换句话说，`AAB` 负责告诉平台“我有哪些内容”，平台再负责告诉设备“你该拿哪一部分”。

这件事带来的直接收益有两个：

- 用户下载更小
- 多 APK 维护成本下降

Google Play 官方文档也明确写了这一层变化：从 `2021 年 8 月` 起，新应用必须使用 `Android App Bundle` 发布；而从 `2023 年 6 月 30 日` 起，新的和已有的 TV 应用更新都必须使用 AAB，不能再用 APK 发布。

这不是“推荐迁移”，而是平台约束变了。

## PAD 接过了 OBB 的班，但它不是 OBB 的简单替身

很多人会把 `Play Asset Delivery` 看成“OBB 的新名字”，这不准确。

PAD 更像是 `AAB` 时代的资源分发机制。它把大资源拆成 `asset packs`，由 Google Play 托管和分发，而不是让项目自己找一条 CDN 路径去兜。

Google 官方给 PAD 的定位很明确：

- 它用于游戏的大体积资源交付
- 它替代的是旧的 OBB 方案
- 它支持 `install-time`、`fast-follow`、`on-demand` 三种交付方式

这三种模式的差别，不在“文件到底放哪”，而在“什么时候可用”：

- `install-time`：安装时一起交付，启动就能用
- `fast-follow`：安装完成后自动拉取，不阻塞进入游戏
- `on-demand`：运行时按需下载

这就把旧时代最模糊的一件事变清楚了：

`资源不再只是“能不能下载”，而是“应该在什么时机、以什么方式可用”。`

## Unity 团队真正要换掉的，是心智，不只是开关

对 Unity 团队来说，这次迁移最容易犯的错，是把它理解成一个纯配置问题：

- 只要打开某个选项就行
- 只要把 OBB 换成 PAD 就行
- 只要 AAB 开了，平台会自动帮我把一切拆对

实际上更应该换的是下面这套心智：

1. 先问“这部分内容属于安装时必须到位，还是可以延后”
2. 再问“这部分内容属于 `base module`，还是应该成为 `asset pack`”
3. 最后才问“Unity 里哪个开关负责把这个边界表达出来”

如果不先问前两句，只盯着最后一个开关，很容易出现一种很熟悉的错觉：

`我已经在用 AAB 了，为什么资源还是像旧时代一样堆在首包里？`

答案通常不是“平台失效了”，而是项目的内容边界仍然是 APK 时代的写法。

## 这条迁移线，后面几篇会继续往下拆

这篇只负责把大方向立住：

- `APK + OBB` 是开发者自己装箱的时代
- `AAB + PAD` 是平台接管分发的时代
- 资源边界从“文件拆分”转向“交付时机和设备适配”

接下来两篇会继续往下拆：

- 为什么 Play Console 会报 `base` 超 `200MB`
- Unity 到底哪些设置会把内容送进 `base module`，哪些会变成 `asset pack`

## 参考

- [About Android App Bundles](https://developer.android.com/guide/app-bundle)
- [Play Asset Delivery](https://developer.android.com/guide/app-bundle/asset-delivery)
- [APK Expansion Files](https://developer.android.com/google/play/expansion-files)
- [Add or test APK expansion files](https://support.google.com/googleplay/android-developer/answer/2481797)
