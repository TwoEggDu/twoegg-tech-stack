---
title: "CachedShadows 阴影缓存 03｜从 Quality 到 Camera：TopHeroUnity 里一个阴影是怎么真正被启用的"
slug: "cachedshadows-03-activation-chain"
date: "2026-04-01"
description: "沿着 TopHeroUnity 的 Quality、Pipeline Asset、Renderer、Camera 和运行时触发器，把 CachedShadows 真正生效的链路串成一条完整路径。"
tags:
  - "Unity"
  - "URP"
  - "CachedShadows"
  - "Android"
  - "Quality"
  - "Renderer"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 30
weight: 1730
---
前两篇已经把两件事先立住了：

- CachedShadows 替代的是 URP 原生主光阴影生成那一段
- 它一帧里会把阴影拆成静态底图、动态叠加和 receiver 交接

但项目现场真正最容易把人卡住的，往往不是“原理没懂”，而是另一类更具体的问题：

`我明明看到了 CachedShadowsRenderFeature，也知道相机上挂了触发脚本，为什么实际效果还是不生效？`

这类问题最容易错在只盯着单点看。  
更稳的办法是把链路钉死：

`Quality -> Pipeline Asset -> Default Renderer -> 当前 Camera -> 运行时刷新触发`

只要这五层里有一层没对上，CachedShadows 就可能“看起来存在”，但并没有真的在你以为的平台和时机上生效。

---

## 先把这条生效链压成一句话

如果把 TopHeroUnity 当前低配 Android 的真实链路压成最小模型，大致是这样：

```text
Android 平台默认质量档
  -> AndroidPipelineAssetLow
      -> ForwardRendererAndroid
          -> CachedShadowsRenderFeature
              -> 当前 Camera 使用默认 Renderer
                  -> CachedShadowsCameraManualUpdateFromMovement 按阈值触发刷新
```

注意这条链里没有哪个环节是“可有可无”的：

- 没有正确命中 Android 的质量档，后面可能根本不是这套 Pipeline Asset
- Pipeline Asset 默认 Renderer 不对，当前相机就不会带上这个 Feature
- Camera 没落到默认 Renderer，Feature 也只是“资产里存在”
- 运行时触发链没起作用，Manual 模式下阴影底图也不会按你想象的时机刷新

所以 CachedShadows 在项目里从来不是“挂了一个 feature 就完了”，而是一个跨配置层和运行时层的完整生效链。

---

## 第一层：Quality 真的是哪一档

这一层最容易被误判，因为 `QualitySettings.asset` 里同时存在两种完全不同的信息：

- 当前编辑器会话正在使用哪一档
- 某个平台默认会落到哪一档

在 TopHeroUnity 当前工程里，`QualitySettings.asset` 至少有两个你必须分开的事实：

- `m_CurrentQuality: 5`
- `Android: 3`

这两个值表达的不是同一件事。

### `m_CurrentQuality: 5` 是什么

它更接近“当前编辑器会话使用的质量索引”。  
也就是说，你在 Editor 里进 Play，默认先继承的是这一档，而不是自动替你切成 Android 低配。

### `Android: 3` 是什么

它表示 Android 平台默认绑定的质量档索引。  
而这份工程里，索引 `3` 对应的是：

`Android LOW`

这一步对理解 CachedShadows 很关键，因为很多误判都来自一句错觉：

`Editor 里看到的就是 Android LOW 会跑的。`

不是。

更准确的说法应该是：

`Editor 当前档位和 Android 平台默认档位，是两件不同的事。`

所以如果你在 Editor 里没有显式切到 Android LOW，再直接进 Play 去看阴影，很可能压根没在验证你真正关心的那条低配 Android 链。

---

## 第二层：Android LOW 具体指向哪个 Pipeline Asset

知道“平台默认档位是 Android LOW”还不够。  
下一层必须继续追：

`Android LOW 这一档，到底绑定了哪个 URP Pipeline Asset。`

在 TopHeroUnity 里，这条链最后落到的是：

`AndroidPipelineAssetLow.asset`

这个 Asset 里最关键的两个字段是：

- `m_DefaultRendererIndex: 0`
- `m_MainLightShadowsSupported: 0`

它们一起表达的含义非常重要：

### `m_MainLightShadowsSupported: 0`

这是在说：

`不要走 URP 原生主光阴影支持路径。`

它管的是“内建主光阴影能力是否启用”，不是“当前项目是否绝对不允许再有阴影”。

### `m_DefaultRendererIndex: 0`

这是在说：

`这个 Pipeline Asset 仍然有默认 Renderer，而且默认是 index 0。`

也就是说，低配 Android 的真实策略不是“整条阴影链都没了”，而是：

`关掉 URP 原生主光阴影生成，但仍然通过默认 Renderer 去挂自定义的 CachedShadows 路径。`

这两个字段并存，不矛盾，恰恰是这套方案能成立的前提。

---

## 第三层：默认 Renderer 为什么能把 Feature 带进来

接着要往下追的是：

`默认 Renderer 0 到底是谁，它有没有把 CachedShadowsRenderFeature 带进当前相机。`

在这份工程里，Android LOW 默认 Renderer 0 指向的是：

`ForwardRendererAndroid.asset`

而这个 Renderer 上，能明确看到启用中的：

`CachedShadowsRenderFeature`

同时它的关键配置也已经写死：

- `UpdateMode: 2`
- `AutomaticallyToggleURPShadowSettings: 0`
- `DynamicShadow: 1`

如果只看这里，最多只能得出一个中间结论：

`这条 Feature 已经被编进了 Android LOW 对应的默认 Renderer。`

但还不能立刻下结论说“当前 Camera 一定会跑它”。  
因为下一层还要看：

`当前 Camera 最终用的是不是这个默认 Renderer。`

---

## 第四层：为什么当前 Camera 会落到这个 Renderer 上

这一层最容易被误读成“Feature 是挂在 Camera 上的”。  
实际上不是。

在 URP 里，Feature 是挂在 `Renderer Asset` 上的，Camera 只是“选择使用哪个 Renderer”。

TopHeroUnity 的 `GameMainCamera.prefab` 里，关键字段是：

`m_RendererIndex: -1`

这句的真实含义不是“没有 Renderer”，而是：

`当前 Camera 不单独指定 Renderer，而是沿用当前 Pipeline Asset 的默认 Renderer。`

把它和上一层拼起来，链路就清楚了：

1. Android 平台默认质量档落到 `Android LOW`
2. `Android LOW` 绑定 `AndroidPipelineAssetLow.asset`
3. 这个 Asset 的默认 Renderer 是 index `0`
4. index `0` 对应 `ForwardRendererAndroid.asset`
5. `GameMainCamera` 的 `m_RendererIndex = -1`
6. 所以这台相机最终会吃到 `ForwardRendererAndroid`
7. 因而也会带上 `ForwardRendererAndroid` 里的 `CachedShadowsRenderFeature`

这就是“RendererFeature 会成为当前 Camera 的一部分”的真正含义。

它不是说 Camera 上挂了某个 MonoBehaviour 就天然拥有这个 feature，  
而是说：

`Camera 在渲染时选择了某个 Renderer，而这个 Renderer 内部带着这条 Feature。`

---

## 第五层：为什么 `featureReferences` 为空仍然可能工作

这一步是项目现场特别容易误判的点。

在 `GameMainCamera.prefab` 上，你能看到：

- `featureReferences: []`
- `additionalFeatureReferences: []`
- `ReRenderAfterDistance: 5`
- `ReRenderAfterAngleCheck: 15`

很多人看到空数组，会直接下意识觉得：

`脚本没有拖引用，那它不会触发任何阴影刷新。`

这个结论对一半，但不完整。

因为 `CachedShadowsCameraManualUpdateFromMovement.cs` 并不只走手工引用链。  
它在 `Update()` 里还有一条“从当前 Camera 的 Renderer 里自发现 Feature”的逻辑：

1. 先拿 `UniversalAdditionalCameraData`
2. 再拿当前 `scriptableRenderer`
3. 再从 `rendererFeatures` 里找 `CachedShadowsRenderFeature`
4. 找到了并且 `UpdateMode == Manual`，就调用 `TriggerShadowsRenderForFrame()`

所以这里真正要记住的是：

- `featureReferences` 为空，只能说明“Inspector 手工引用链没配”
- 它不等于“运行时一定不会触发”
- 只要当前 Camera 最终落到的 Renderer 里真的有 `CachedShadowsRenderFeature`，脚本仍然可以自己找到它

这也是为什么这类问题不能只盯着 prefab 静态数据看。  
必须把“静态引用链”和“运行时自发现链”一起看。

---

## 第六层：相机脚本触发的不是“阴影开关”，而是“刷新时机”

到了这里，还要再补一句非常重要的边界：

`CachedShadowsCameraManualUpdateFromMovement` 不负责定义阴影是否存在，它负责定义“什么时候请求重画”。`

它真正做的事情很具体：

- 比较当前相机和上一次记录位置的距离
- 比较当前相机和上一次记录旋转的角度
- 距离超过 `ReRenderAfterDistance`
- 或角度超过 `ReRenderAfterAngleCheck`
- 就调用 `TriggerShadowsRenderForFrame()`

也就是说，它管的是：

`何时刷新缓存底图`

而不是：

`项目有没有阴影`

这点如果不分清，后面很容易出现一种典型误判：

`相机动一下阴影出来了，所以是灯或 caster 出问题。`

其实很多时候恰恰相反。  
相机动一下才出来，往往恰好说明：

- 主链基本是通的
- Feature 基本是生效的
- 问题大概率落在 Manual 模式的刷新时机

所以这一层更像一条“时机链”，不是“存在性链”。

---

## 为什么 Editor 里看到，不等于 Android LOW 真在跑

到这里，就可以把一个最常见的错觉彻底掰正：

`Editor 里有这个 Feature，也看到了阴影，所以 Android LOW 肯定也是这条链。`

这个推理不成立，至少有三层原因：

### 1. Editor 当前质量档不一定是 Android LOW

前面已经说过，`m_CurrentQuality: 5` 和 `Android: 3` 不是一回事。  
如果你不显式切档，Editor Play 默认看的是当前编辑器档位，不是 Android 低配档。

### 2. Scene View / 非 Play Mode 观察也会掺入编辑器特化逻辑

CachedShadows 在编辑器下本来就有“方便观察”的强制重画分支。  
这会让你在编辑器里看到比运行时更积极的刷新行为。

### 3. 真机还多了一层构建物和预加载边界

即使配置链和逻辑链在 Editor 里成立，到了真机还要再过：

- Shader / Variant 是否真的进构建物
- `StaticShaders.prefab` 是否真的走了 preload
- 运行时 loader 是否真的安装成功

所以“Editor 看到了”最多能证明：

`这套资产和代码路径在编辑器环境里大致存在。`

它不能直接证明：

`Android LOW 平台上的那条真实运行时链路已经被验证。`

---

## 把这条生效链收成一张图

如果你只想在脑子里留一条最短路径，我建议记这个：

```text
QualitySettings.asset
  -> Android 平台默认档位 = Android LOW
      -> AndroidPipelineAssetLow.asset
          -> Default Renderer = ForwardRendererAndroid
              -> CachedShadowsRenderFeature 已挂载
                  -> GameMainCamera 使用默认 Renderer
                      -> CachedShadowsCameraManualUpdateFromMovement 按阈值触发刷新
```

然后再额外记一句边界提醒：

`前四层决定“这条阴影链会不会被接上”，最后一层决定“它什么时候真的刷新”。`

---

## 这篇真正想留下来的结论

如果最后只允许留一句话，我会把这篇收成这样：

`在 TopHeroUnity 里，CachedShadows 的生效不是一个勾选框，而是一条从 Quality 到 Pipeline Asset，再到 Renderer、Camera 和运行时刷新脚本的完整链路；任何一层没对上，都会出现“它明明在资产里，但就是没生效”的假象。`

下一篇会从这条链再往外走一步，专门拆：

`为什么编辑器有阴影、打包后可能没了，SVC、Hidden Shader、StaticShaders 这几层到底各管什么。`
