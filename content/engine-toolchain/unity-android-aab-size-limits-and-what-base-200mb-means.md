---
date: "2026-04-01"
title: "Play Console 报 base 超 200MB，到底在算什么"
description: "把 Google Play 现在对 AAB、base module、feature module 和 asset pack 的体积口径讲清楚，解释为什么 .aab 文件大小不是直接答案，并厘清旧 APK 限制和当前 AAB 限制的边界。"
slug: "unity-android-aab-size-limits-and-what-base-200mb-means"
weight: 2061
featured: false
tags:
  - Unity
  - Android
  - AAB
  - Google Play
  - Package Size
series: "Unity Android 发布与包体"
primary_series: "unity-android-aab-pad"
series_role: "article"
series_order: 20
---

如果你在 Play Console 里看到的是 `base` 超了，而不是某个具体资源超了，先不要急着往 Unity 构建参数上找原因。

这类报错先回答的不是“怎么瘦身”，而是一个更基础的问题：

`Play Console 到底在按什么口径算大小？`

这一点不先统一，后面的讨论很容易混成三件完全不同的事：

- `.aab` 文件本身有多大
- Play Console 估出来的下载大小有多大
- 用户在某个设备上最终装下去以后，占了多少磁盘

这三件事不是一回事。

## 先把口径说死

Google Play 现在看的是 `compressed download size`，也就是 Play Console 在你上传 app bundle 后，按设备配置估出来的“压缩后下载大小”。

Play Console 帮助页现在给出的规则很直接：

- `Base module` 上限是 `200MB`
- `Individual feature modules` 上限也是 `200MB`
- `Individual asset packs` 上限是 `1.5GB`
- `All modules + install-time asset packs` 的累计上限是 `4GB`
- `On-demand / fast-follow asset packs` 的累计上限是 `4GB`
- 如果你是 `Level Up` 项目或 `Android XR` 相关发行，这个 on-demand / fast-follow 累计上限会放宽到 `30GB`

这也是为什么你不能拿“我导出的 `.aab` 只有几百 MB”来判断是否会过审。

`AAB` 是发布格式，不是终端安装格式。Google Play 会把它处理成按设备优化后的 APK，再按设备配置计算下载大小。

## 为什么你看到的是 base

在 AAB 体系里，`base module` 是最核心的那一块。

它承载的是安装后立刻需要的内容，也是 Google Play 先要保证能下、能装、能启动的那部分。

所以当 Play Console 报 `base` 超限时，它表达的不是“整个项目太大”，而是更具体的一句话：

`你放进基础模块的内容，按某个设备配置生成出来以后，压缩下载大小已经超过 200MB。`

这和 APK 时代的报错习惯不一样。
APK 时代常见的讨论是“主包不能超过 100MB”，配合 OBB 去挂大资源。
AAB 时代则是“主分发内容里，base、feature module、asset pack 各自有自己的边界”。

所以旧资料里常见的 `100MB / 150MB` 混用，很容易把人带偏：

- `100MB` 是 legacy APK 时代的上限
- `200MB` 是当前 AAB 体系里 `base module` 的限制
- 这两个数不是同一条规则

## base、feature module、asset pack 分别是什么

如果只用一句话记住三者，可以这样分：

- `base module` 是“安装就要在”的内容
- `feature module` 是“按功能拆出去”的代码和资源
- `asset pack` 是“按内容交付”的大块资源

这三者的区别，不在于文件名长什么样，而在于它们服务的交付边界不同。

### Base module

base 里通常应该放的是：

- 启动所需代码
- 首屏必须的资源
- 不能延迟到后续模块的最小内容集

它不是“默认放哪都行”的垃圾桶。
base 一旦塞进了太多常驻资源，后面你再怎么拆包，Play Console 看的还是这个基础模块的下载口径。

### Feature module

feature module 适合放按功能分开的内容。

它的关键不是“文件类型”，而是“功能边界”。
如果某一块内容可以晚一点再下，或者只在特定功能里才需要，那它就更像 feature module 的候选项，而不是 base 的必留项。

### Asset pack

asset pack 更像内容交付容器。

它适合承载体积大、但又不必跟首启绑定的资源，比如：

- 后续关卡内容
- 大体积场景资源
- 分章节下载的素材包

这也是为什么 Google Play 在 AAB 体系里同时引入了 Play Feature Delivery 和 Play Asset Delivery。
前者偏功能，后者偏资源。

## 为什么 `.aab` 文件大小不是直接答案

`.aab` 只是你上传给 Google Play 的发布包。

Play Console 真正关心的，是它给某个具体设备生成出来的结果。
同一个 bundle，面对不同设备，可能会被裁剪出不同的 APK 组合。

这意味着：

- `.aab` 本身可以比 `200MB` 大得多
- 但生成给某台设备的压缩下载大小不能把 `base` 顶穿
- 你在 Play Console 里看到的“大小”更接近分发结果，而不是源文件大小

从这个角度看，Play Console 现在的 App size 页面其实比“看文件大小”更有意义。
它给的是按设备配置估算后的下载大小，并且还能拆出：

- `Code / DEX`
- `Resources`
- `Assets`
- `Native libraries`
- `Other`

这套拆法比“包体多少 MB”更接近工程问题本身。

## 哪些条件差异需要单独记住

这里有几个容易被旧资料混淆的条件差异，最好一次记清：

1. `APK` 和 `AAB` 不是同一套限制

   - `APK` 仍然受 legacy 的 `100MB` 限制
   - `AAB` 的 `base module` 现在是 `200MB`

2. `feature module` 和 `asset pack` 也有自己的上限

   - feature module 不是无限大的“第二主包”
   - asset pack 不是无限大的“外置仓库”

3. 超过 `200MB` 以后，Google Play 还会给用户弹出非阻断提示

   - 这说明 Play 关注的不只是能不能装
   - 还关注下载体验和安装转化

4. 体积上限看的始终是压缩下载大小

   - 不是磁盘占用
   - 不是 bundle 原文件大小
   - 也不是你本地导出的某个中间目录大小

## 这条报错真正该怎么理解

看到 `base > 200MB`，你该得出的第一结论不是“Google Play 变小气了”。

更准确的结论是：

`你现在的首发内容边界，和 Google Play 对 AAB 的分发边界没有对齐。`

这个问题常常不是单个资源太大，而是几类内容一起被默认放进了 base：

- 首屏内容太重
- 常驻资源太多
- 原生库和 ABI 选择不够克制
- 本来可以按功能或按内容拆出去的东西，仍然留在基础模块里

所以这篇只负责把“数是怎么算的”说清楚。
下一篇再进入 Unity 里哪些设置真的在决定这些内容会不会留在 base。

## 一句话结论

`base 超 200MB` 不是在说 `.aab` 太大，而是在说你给 Google Play 的“安装即首发内容”超过了当前 AAB 规则允许的基础模块边界。

如果你要继续往下排查，下一篇应该直接看 Unity 构建设置和资源归属边界，而不是继续盯着 bundle 文件本身。
