---
title: "CachedShadows 阴影缓存 01｜它替代了 URP 主光阴影链路里的哪一段"
slug: "cachedshadows-01-overview"
date: "2026-04-01"
description: "从 TopHeroUnity 的真实低配 Android 配置出发，讲清 CachedShadows 不是另一套光照系统，而是在 URP 主光阴影生成链路中插入“自定义生成 + 仍按 URP 语义接收”的缓存方案。"
tags:
  - "Unity"
  - "URP"
  - "Shadow"
  - "CachedShadows"
  - "Android"
  - "Renderer Feature"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 10
weight: 1710
---
很多人第一次看到 `AndroidPipelineAssetLow` 里的 `m_MainLightShadowsSupported = 0`，会立刻得出一个结论：

`这档位已经把主光阴影关了，所以低配 Android 不可能再有阴影。`

如果项目完全走 URP 原生主光阴影，这个判断大体没错。但 TopHeroUnity 不是这条路。它在低配 Android 上把 **URP 原生主光阴影生成** 关掉了，同时又在 Renderer 上挂了 `CachedShadowsRenderFeature`，用另一条链路去生成并复用主光阴影。

这篇先不讲“它一帧里怎么缓存、怎么刷新、怎么排查”。这篇只做一件事：

`把 CachedShadows 在整条 URP 阴影链路里站的位置讲清楚。`

---

## 先把问题说准

如果一句话把这篇压到最短，我会写成：

`CachedShadows 不是另一套光照系统，它替代的是“主光阴影图怎么生成”，但接收阴影时仍然尽量沿用 URP 的主光阴影语义。`

这句话里有两个关键词最重要：

- `替代的是生成`
- `接收仍然沿用 URP 语义`

也就是说，`m_MainLightShadowsSupported = 0` 并不自动等于“屏幕上绝对不可能再有阴影”。它更准确表达的是：

`不要让 URP 默认那套 main light shadow pass 去生成主光阴影。`

但如果你后面又接了一套自定义 Renderer Feature，在合适的时机自己生成 shadow map，并把 receiver 侧需要的全局纹理、矩阵和 keyword 再补回去，那么 shader 仍然可能正常采样到主光阴影。

TopHeroUnity 的低配 Android，走的就是这个思路。

---

## 先把 URP 原生主光阴影链路压成最小模型

先不要一上来就看项目代码。先把 URP 原生主光阴影缩成最小模型，会更容易看懂 CachedShadows 到底插在哪里。

如果只保留和“主光阴影”直接相关的部分，URP 的默认链路可以粗略压成三步：

```text
1. Pipeline Asset / Shadow Settings
   决定当前项目是否支持主光阴影、分辨率、级联数、距离等

2. Shadow Pass
   在 BeforeRenderingShadows 附近，从主光视角渲染 shadow map

3. Receiver Side
   在正常场景着色时，用 _MainLightShadowmapTexture、世界到阴影空间矩阵、
   _MAIN_LIGHT_SHADOWS 系列 keyword 去判断当前像素是否在阴影里
```

这里最容易混掉的一点是：

`“阴影有没有”不是一个单点开关，而是生成链和接收链两边都成立，画面上才会有。`

换句话说，至少有两件事要同时成立：

- 有人把主光阴影图画出来
- receiver shader 也知道该怎么去采样这张图

普通 URP 是同一套系统负责这两件事。  
CachedShadows 则是把第一件事接管了，但第二件事尽量仍然沿用 URP 的约定。

---

## CachedShadows 插进来的位置在哪

放回 TopHeroUnity 里，这条链首先从低配 Android 的 Pipeline Asset 开始。

`AndroidPipelineAssetLow.asset` 里最关键的几项是：

- `m_DefaultRendererIndex: 0`
- `m_MainLightShadowsSupported: 0`
- `m_MainLightShadowmapResolution: 1024`
- `m_ShadowDistance: 50`
- `m_ShadowCascadeCount: 1`

这组配置有一个很强的信号：

- `m_MainLightShadowsSupported: 0` 说明不走 URP 原生主光阴影生成
- 但分辨率、距离、级联数仍然有值，说明“阴影参数本身”并没有被彻底抹掉

接着再往下看默认 Renderer。  
这个低配 Asset 的默认 Renderer 是 `ForwardRendererAndroid.asset`，而这个 Renderer 上挂着启用中的 `CachedShadowsRenderFeature`。它的关键配置是：

- `UpdateMode: 2`
- `AutomaticallyToggleURPShadowSettings: 0`
- `DynamicShadow: 1`

这说明项目在低配 Android 上的真实策略不是“完全不要阴影”，而是：

`关掉 URP 原生主光阴影生成，再把阴影这件事交给 CachedShadowsRenderFeature。`

如果把这一段压成最小结构，大致是这样：

```text
Android Quality
  -> AndroidPipelineAssetLow
      -> ForwardRendererAndroid
          -> CachedShadowsRenderFeature
              -> CachedShadowsRenderPass
              -> CachedShadowsPostShadowRenderSettingsPass
```

也就是说，CachedShadows 不是一个游离在项目外面的脚本工具，而是正儿八经挂在当前 Renderer 上、进入当前相机渲染队列的一段 URP 扩展。

---

## 它具体替代的是哪一段

这时就可以回答这篇最核心的问题了：

`CachedShadows 替代的是“主光阴影图由谁、在什么时候、以什么刷新策略去生成”这一段。`

你可以直接从 `CachedShadowsRenderFeature` 和 `CachedShadowsRenderPass` 里看到这个意图。

在 `CachedShadowsRenderFeature` 这一层，项目做了几件关键事：

1. `Create()` 里创建了两个 pass。
一个真正负责阴影生成，一个负责阴影生成后的 receiver 配置补齐。

2. `AddRenderPasses()` 里把这两个 pass 加进当前 Renderer。
这意味着它不是离线烘焙，也不是脚本层模拟，而是真正在当前相机的渲染链路里执行。

3. 运行时把 `URPGraphicsSettings.MainLightCastShadows` 设成 `false`。
这一步是在明确告诉管线：

`不要再让 URP 默认那套主光阴影去做这件事。`

到了 `CachedShadowsRenderPass.Setup()` 里，这个意图更直接：

```csharp
renderingData.shadowData.supportsMainLightShadows = false;
```

这行代码的意义不是“整个世界再也不允许有主光阴影”，而是：

`当前这帧接下来不要按 URP 默认逻辑去走 supportsMainLightShadows 那条生成路径。`

与此同时，这个 pass 并没有就此放弃阴影。它后面紧接着又自己做了原本应由主光阴影生成阶段完成的事情：

- 找主光
- 检查主光有没有开阴影
- 检查是否存在 shadow caster bounds
- 提取方向光 shadow matrix
- 准备 shadow atlas / slice / 级联信息
- 渲染或复用自己的主光 shadow map

所以更准确的说法应该是：

`它关掉的是 URP 原生主光阴影生成的执行权，不是“世界上不允许再有主光阴影”这个事实。`

---

## 它没有替代什么

反过来说，CachedShadows 也有很多事情并没有接管。

这点不讲清，后面很容易把它误解成“自定义阴影系统 = 自己重写了所有光照逻辑”。

它没有替代的部分主要有：

- 没有重写一整套 BRDF 或光照模型
- 没有把主光照明逻辑从 Lit / Toon / 自定义材质里全部拿掉
- 没有要求项目所有 receiver shader 改成另一套完全不同的采样协议
- 没有把“主光阴影是否参与着色”这件事改成纯脚本判断

换句话说，它不是在做：

`我自己发明一套完全独立于 URP 的阴影语言。`

它更像是在做：

`我自己生产 shadow map，但仍尽量把产物包装成 URP receiver 能认出来的样子。`

这就是为什么后面你会看到它在 post pass 里又去补 keyword、补 receiver constants。

---

## 为什么 receiver 侧还能像“正常 URP 阴影”一样工作

这是整个方案成立的关键，也是很多人第一次看时最容易漏掉的一步。

如果一个系统只是把 shadow map 自己画出来，但没有把 receiver 侧要用到的状态补齐，那么材质照样可能“看不见”这张图。

TopHeroUnity 里的 CachedShadows 并没有停在“我把图画出来了”这一步。  
它后面还有一个 `CachedShadowsPostShadowRenderSettingsPass`，最终会调用：

`PostShadowPassConfigureKeywords()`

在这里，它手动做了几件非常关键的事：

- 设置 `MainLightShadows` keyword
- 设置 `MainLightShadowCascades` keyword
- 设置 `SoftShadows` keyword
- 调用 `SetupMainLightShadowReceiverConstants(...)`

这组操作的意义是：

`把 receiver shader 侧期望看到的那批“主光阴影运行时状态”重新补回去。`

所以 shader 在采样时，看到的仍然是熟悉的那套语义：

- 当前是否开启主光阴影
- 当前是不是 cascade 版本
- 当前 shadow map 纹理在哪里
- 世界坐标怎么变到阴影空间

这就是为什么从 shader 视角看，它仍然“像是在采样 URP 的主光阴影”；但从生成链视角看，它已经不是 URP 默认那套 pass 画出来的了。

如果把这一层压成一句话：

`CachedShadows 的关键不是只画出一张图，而是把这张图重新接回 URP receiver 侧能识别的协议。`

---

## 放回 TopHeroUnity：低配 Android 为什么还能有影子

到这里，就可以把你最关心的那个现象收回来解释了：

`为什么 AndroidPipelineAssetLow 明明写着 m_MainLightShadowsSupported = 0，低配 Android 理论上仍然可能有影子？`

答案不是一句“因为项目做了自定义功能”这么含糊。更准确的链路是：

1. `AndroidPipelineAssetLow` 关闭了 URP 原生主光阴影支持。

2. 同一个 Asset 仍然把默认 Renderer 指向 `ForwardRendererAndroid`。

3. `ForwardRendererAndroid` 上挂着启用中的 `CachedShadowsRenderFeature`。

4. 这个 Feature 在渲染时主动禁用 URP 默认主光阴影生成路径，并插入自己的阴影 pass。

5. 自定义 pass 仍然会去找主光、caster bounds、阴影矩阵，并自己生成或复用主光 shadow map。

6. 阴影生成结束后，post pass 再把 receiver shader 需要的 keyword 和常量补回去。

所以低配 Android 的真实逻辑不是：

`URP 原生主光阴影关了 -> 世界没有阴影`

而是：

`URP 原生主光阴影关了 -> 阴影生成职责转交给 CachedShadows -> 只要这条链路成立，画面上仍然可以有阴影`

这也是为什么你静态核对工程时，会得到一个比“理论上可能”更准确的结论：

`从配置链和代码链看，这套低配方案本来就是为了在关闭 URP 原生主光阴影后，仍然保留阴影效果。`

---

## 这套方案成立还依赖哪些前提

不过这里要立刻补一句：

`不走 URP 原生主光阴影，不等于 CachedShadows 就一定能稳定生效。`

至少还要满足几个前提：

### 1. 场景里真的有主光

`CachedShadowsRenderPass.Setup()` 会先查 `mainLightIndex`。  
如果主光不存在，或者当前可见光里没有能当主光的方向光，它会直接退回空 shadow map。

### 2. 主光本身要允许投影

如果 `light.shadows == LightShadows.None`，这条链也会在 Setup 阶段直接终止。

### 3. 当前 cull 结果里要能找到 shadow caster bounds

这套系统不是无中生有。如果 `GetShadowCasterBounds(...)` 失败，说明当前没有有效的阴影投射体，它同样会退回空图。

### 4. receiver shader 要有对应的主光阴影采样路径

后面专题会单独展开这一点，但先记一句：

`生成出来的阴影图想真正被用到，receiver shader 侧仍然得有 _MAIN_LIGHT_SHADOWS 这批路径。`

### 5. 运行时资源链路不能把关键 shader 丢掉

比如 `Hidden/CopyShadowMap` 这种隐藏 shader，如果打包后被裁掉，动态叠加那条链就会出问题。  
这不是这篇的重点，后面会单独讲 `StaticShaders.prefab`、`BundleSVC.shadervariants` 和预加载链路。

---

## 这一篇故意没有展开什么

为了让主线不散，这篇故意没展开下面几件事：

- 一帧里静态阴影和动态阴影到底怎么拼
- `Manual / EverySecond / EveryFrame` 的刷新差异
- 为什么相机移动会触发刷新
- 为什么 `featureReferences` 为空仍然可能正常工作
- 为什么 Editor 看到了不代表 Android 一定没问题
- 为什么打包后可能因为 shader / variant / preload 链路失效

这些都很重要，但它们属于下一层问题。

如果这一篇就把“它插在哪”“它替代了什么”“它为什么还能接回 receiver 侧”讲清楚，后面的缓存、刷新、排查才不会变成一堆互相打架的 checklist。

---

## 这篇真正想留下来的结论

如果最后只允许留一句话，我会把这篇收成这样：

`TopHeroUnity 的 CachedShadows 不是在“URP 阴影关闭后硬把阴影又打开”，而是在关闭 URP 原生主光阴影生成之后，用 Renderer Feature 接管了生成链，并把结果重新接回 URP receiver 侧。`

下一篇会继续往下拆：

`一帧里到底发生了什么，什么被缓存，什么被重画，静态阴影和动态阴影又是怎么叠在一起的。`
