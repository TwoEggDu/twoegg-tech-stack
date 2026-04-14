---
date: "2026-04-01"
title: "Unity AAB 超限实战：base > 200MB 时怎么排、怎么改、怎么做 CI"
description: "把 Unity 项目在 Google Play 上报 base 超限的排查顺序拆清楚：先确认有没有拆对，再看 first scene、Resources、StreamingAssets，再看 native/ABI，最后把门禁落到 CI 和基线。"
slug: "unity-android-base-module-over-200mb-diagnosis-and-fixes"
weight: 58
featured: false
tags:
  - "Unity"
  - "Android"
  - "AAB"
  - "Google Play"
  - "Build"
  - "CI"
series: "Unity Android 发布与包体"
primary_series: "unity-android-aab-pad"
series_role: "article"
series_order: 50
---

> 如果这篇只记一句话，那就记这句：`base > 200MB` 不是一个"把包再瘦一点"的泛问题，而是一个"你们到底有没有把内容放对层"的分层问题。

前面几篇已经把发布模型、平台口径和 Unity 映射讲清楚了。到了这一步，真正该问的不是"还能不能继续压缩"，而是：

- 是不是根本没拆对
- 是不是把首场景、Resources、StreamingAssets 塞进了 base
- 是不是 native 库和 ABI 把 base 顶爆了
- 是不是没有把这些规则变成 CI 门禁

这篇的目标很明确：给出一条能落地的排查顺序，而不是再讲一遍 AAB 历史。

## 1. 先确认：你们是不是"没拆对"

很多团队看到 `base > 200MB`，第一反应是去瘦资源、压贴图、裁音频。这个动作不一定错，但经常是第二步，不是第一步。

先判断一件事：

`你们现在的 AAB，真的已经把内容拆到 base + asset packs 了吗？`

这一步要先看配置，不先看内容。

### 先查这几个开关

- `Build App Bundle (Google Play)` 是否真的打开
- `Split Application Binary` 是否启用
- 自定义 `Gradle` 模板里是否还保留旧的、过时的打包假设
- 如果用了自定义 asset pack，`PLAY_ASSET_PACKS` 是否还在最终模板里生效

Unity 官方文档明确写过：AAB 路径下，`Split Application Binary` 会把应用拆成 `base module` 和 `asset packs`；如果旧模板里缺少 `PLAY_ASSET_PACKS`，Unity 自动生成的 asset pack 值就可能没有被正确带进去。

### 先看这三个症状

| 症状 | 更像哪一层的问题 | 先做什么 |
|---|---|---|
| 产物里看起来有 AAB，但 base 还是异常大 | 构建拓扑没拆对 | 先核对 `splitApplicationBinary`、Gradle 模板、PAD 是否真正生效 |
| 资源明明做了分包，上传后仍然进 base | 内容映射没生效 | 看 first scene、Resources、StreamingAssets 是否仍在 base 边界内 |
| 体积变化主要来自 `so` 或 ABI | 原生层没裁干净 | 先看架构选择和原生库占用，而不是继续压资源 |

### 这一步最常见的误区

- 把 `useAPKExpansionFiles` 当成 AAB 时代的关键开关
- 以为只要"资源框架换成 Addressables"就会自动进 asset pack
- 以为上传的是 `.aab`，那限制就应该看 `.aab` 文件本身大小

这些都不是这一步的结论。先确认拆分链条是否真的成立，再谈怎么瘦身。

## 2. 再看内容：first scene、Resources、StreamingAssets

如果拆分链条是成立的，下一步就不是看"哪类资源更大"，而是看"哪些东西被 Unity 视为 base 内容"。

Unity 的 `splitApplicationBinary` 文档已经把这条链说得很直白了：在 AAB 下，`base module` 里会放 executable、native 代码，以及 `first scene` 的数据；其余内容才会进 asset packs。

所以这一步的核心问题只有一个：

`你们是不是把本来该晚点加载的内容，提前放进了 first scene 或默认打包路径里？`

### 优先怀疑的三类内容

#### 1) first scene 太重

first scene 的问题不是"场景复杂"这么简单，而是它天然是 install-time 的入口。

如果主菜单、启动校验、首屏 UI、引导资源、默认角色、启动音效，全都塞在 build index 0 的那一张场景里，base 就会直接长大。

典型表现：

- 首场景就加载大量贴图、音频和 prefab
- 只要启动游戏，很多资源就已经被判定为"必须安装时可用"
- 你们明明有后续分包，但首包体积还是被第一张场景拖住

#### 2) `Resources` 还在承担"图省事"职责

`Resources` 最常见的问题，不是技术上不能用，而是它经常被团队用成"先塞进去，之后再说"。

一旦这种习惯形成，Resources 很容易变成：

- 启动期公共资源仓库
- 临时依赖收纳箱
- 各模块都敢顺手引用的默认出口

最后的结果通常不是"加载更方便"，而是：

- 资源边界失控
- 依赖回收困难
- base 和后续资产包的责任分界变模糊

#### 3) `StreamingAssets` 被误当成"天然不进 base"

`StreamingAssets` 这个目录最容易制造错觉。

很多人以为它只是"原样带进包里"，但在 AAB 路径里，Unity 的 asset pack 规则里已经明确提到：remaining scenes、resources、streaming assets 都可能进入 asset packs；反过来，如果你仍把它们当成统一的首包内置文件，就会把 install-time 体积继续抬高。

所以这里要区分两件事：

- 它是不是原样文件
- 它在你的项目里是不是首包必需

如果它是首包必需，那它就应该老老实实进入 base。  
如果它不是首包必需，但你们还把它放在 install-time 路径里，那就是分层错误，不是容量错误。

### 这一步的结论

如果 `first scene`、`Resources`、`StreamingAssets` 里有一大批"非首启必需"内容，那么问题不是"base 太大"，而是"你们把内容层级做错了"。

这时该改的是归属，不是单纯压缩。

## 3. 再看原生层：native、ABI 和第三方插件

当资源层已经基本站稳，base 还是超限，就该怀疑原生层。

Unity Android 项目里，`base` 变大的另一个高频来源，不是资源，而是：

- `libil2cpp.so`
- `libunity.so`
- 第三方原生插件
- 多 ABI 同时打包

### 先看 ABI

如果项目还在保留 `ARMv7`，而目标用户又主要是现代设备，那这通常不是一个"保险"，而是一个明确的体积成本。

简单说：

- 保留更多 ABI，包就更大
- 每多一套原生库，base 就多一份 native 成本
- 对大多数新项目来说，`ARM64` 往往是更合理的默认选择

这一步不一定是"全删掉 32 位"这么简单，但至少要先问：

`我们保留这些 ABI，换来的覆盖率，是否真的值得这份 base 成本？`

### 再看原生库

原生层的典型排查对象是：

- `libil2cpp.so` 是否异常大
- `libunity.so` 是否带入了不必要的内容
- 第三方 SDK 是否静态链接了太多东西
- 符号、调试信息、未裁剪架构是否被错误保留

这一步不要把锅先甩给资源团队。很多时候，base 真正的超限点不是贴图，而是原生代码和多架构拷贝。

### 这一层的判断顺序

| 现象 | 更可能的原因 | 先做什么 |
|---|---|---|
| 去掉一批资源后，base 只降了一点 | native 占比过高 | 看 ABI、原生库、插件 |
| 不同平台差异很大 | 原生依赖和编译配置不一致 | 比较不同 target 的产物和依赖树 |
| Debug / Release 差距很大 | 符号、裁剪、编译策略不同 | 看裁剪和发布配置，而不是再缩资源 |

## 4. 最后才上 CI：把体积问题变成门禁

如果前面三层都看完了，还想让问题不反复，最后就必须把它放进 CI。

这一步的目标不是"自动打一次包"，而是：

`把 base 的责任层、阈值和回归信号固定下来。`

### CI 最少要做三件事

#### 1) 固定一个基线

至少记录这几项：

- `base module` 下载体积
- `asset pack` 总体积
- `native libraries` 体积
- `first scene` 体积

没有基线，团队很容易只会说"这版好像大了"，但说不出大在哪一层。

#### 2) 每次构建都出分项报告

可用的工具建议包括：

- Play Console 的 App size / size breakdown
- `bundletool`
- `APK Analyzer`
- Unity `Build Report`

这些工具的分工不同：

- Play Console 看最终分发口径
- `bundletool` 看接近发布的估算
- `APK Analyzer` 看产物内部占比
- `Build Report` 看 Unity 构建阶段带了什么

#### 3) 给不同层设置不同门槛

不要只设一个"总包体不能超过 X"的粗线。

更稳的做法是分别设：

- `base` 上限
- `first scene` 上限
- `native libraries` 上限
- `asset packs` 上限

这样一来，某次回归发生时，你能立刻知道是资源层、native 层，还是首场景层先出了问题。

### 一个实用的门禁顺序

1. 先判定是否真的拆进了 `base + asset packs`
2. 再判定 `first scene` 是否超重
3. 再判定 `Resources`、`StreamingAssets` 是否误入首包
4. 再判定 native / ABI 是否膨胀
5. 最后才是把这些项接进 CI 阈值和回归报警

这条顺序比"先压资源、再删 shader、再裁贴图"更稳，因为它先解决边界，再解决容量。

## 5. 一张收口表

| 责任层 | 最先怀疑什么 | 最先做什么 |
|---|---|---|
| 构建拓扑层 | `splitApplicationBinary`、PAD、Gradle 模板 | 先确认是否真的拆对 |
| 首包内容层 | first scene、`Resources`、`StreamingAssets` | 先把非首启内容移出 install-time 路径 |
| 原生层 | ABI、`libil2cpp.so`、`libunity.so`、第三方插件 | 先减掉不必要的架构和 native 负担 |
| 流程层 | 没有 size baseline、没有门禁、没有回归报告 | 先把分项体积接进 CI |

## 结尾

`base > 200MB` 不是一个单点优化题，它更像一张分层试卷。

如果你们先把 `base` 当成"一个需要压缩的包"，就会陷入无穷无尽的瘦身动作；如果你们先把它当成"内容是否放对层"的问题，就会很快发现真正该改的是归属、边界和门禁。

这也是这组文章前四篇最终要收敛到的地方：

- 先搞清楚 AAB 是什么
- 再搞清楚 Google Play 到底在限制什么
- 再搞清楚 Unity 到底把什么放进 base
- 最后把这些规则变成 CI

### 推荐继续看

- [Unity Player Settings 总览｜哪些参数真的会影响裁剪、构建速度和运行时]({{< relref "engine-toolchain/unity-player-settings-build-runtime-overview.md" >}})
- [Unity 资源交付工程实践：分组、命名、版本、缓存、回滚和烟测基线]({{< relref "engine-toolchain/unity-resource-delivery-engineering-practices-baseline.md" >}})
- [Resources、StreamingAssets、AssetBundle、Addressables 到底各自该在什么场景下用]({{< relref "engine-toolchain/unity-resources-streamingassets-assetbundle-addressables-when-to-use.md" >}})
- [包体预算怎么定：首包、全量包、热更和常驻资源为什么不能混成一个数字]({{< relref "performance/game-budget-02-package-budget.md" >}})

### 官方依据

- [Google Play app size limits](https://support.google.com/googleplay/android-developer/answer/9859372)
- [Add or test APK expansion files](https://support.google.com/googleplay/android-developer/answer/2481797)
- [Android App Bundles](https://developer.android.com/guide/app-bundle)
- [Unity Play Asset Delivery](https://docs.unity3d.com/cn/2022.3/Manual/play-asset-delivery.html)
- [Unity `PlayerSettings.Android.splitApplicationBinary`](https://docs.unity3d.com/cn/2023.2/ScriptReference/PlayerSettings.Android-splitApplicationBinary.html)
