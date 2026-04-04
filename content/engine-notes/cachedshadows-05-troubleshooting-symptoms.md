---
title: "CachedShadows 阴影缓存 05｜症状总表：没影子、不刷新、只有 Editor 有、Android 没有时先查什么"
slug: "cachedshadows-05-troubleshooting-symptoms"
date: "2026-04-01"
description: "把 CachedShadows 最常见的几类现场症状拆成输入层、管线层、触发层和构建层四个入口，给出适合 Unity 新人的最小排查顺序。"
tags:
  - "Unity"
  - "URP"
  - "CachedShadows"
  - "Shadow"
  - "Troubleshooting"
  - "Android"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 50
weight: 1750
---
前面几篇已经把主线先立住了：

- 它替代了 URP 主光阴影生成的哪一段
- 一帧里静态底图、动态叠加和 receiver 交接是怎么走的
- 从 Quality 到 Camera，这条链在 TopHeroUnity 里是怎么真正接起来的
- 运行时 Shader / Variant / Hidden Shader / preload 又分别站在哪一层

但项目现场真正最需要的，往往还是另一种文章：

`效果不对时，我先查什么，才能最快把问题缩到正确层级。`

这篇就按这个目标来写。  
它不想替代后面的 Frame Debugger、RenderDoc 或单点案例，而是先给你一个稳定的分流框架。

如果把这篇压成一句话，我会写成：

`CachedShadows 的问题不要一上来就猜“阴影算法坏了”，先把症状归到输入层、管线层、触发层和构建层，再去查对应的第一检查点。`

---

## 先把症状归到四层

对 TopHeroUnity 当前这条 CachedShadows 主光链来说，大多数现场问题都可以先压进下面四层：

### 1. 输入层

这里回答的是：

`这帧到底有没有资格产出主光阴影。`

典型检查项包括：

- 主光是不是方向光
- 主光有没有开阴影
- 当前 cull 结果里有没有 shadow caster bounds
- 静态和动态对象有没有分到正确路径

### 2. 管线层

这里回答的是：

`当前平台和当前相机，到底有没有真正命中你以为的那套 Pipeline / Renderer / Feature。`

典型检查项包括：

- Android 平台默认质量档是不是 Android LOW
- 当前是否真的用了 `AndroidPipelineAssetLow`
- 默认 Renderer 是否是 `ForwardRendererAndroid`
- `CachedShadowsRenderFeature` 是否真的挂在当前 Renderer 上

### 3. 触发层

这里回答的是：

`这帧静态底图有没有被要求刷新。`

典型检查项包括：

- `UpdateMode` 是不是 `Manual`
- 有没有挂 `CachedShadowsCameraManualUpdateFromMovement`
- 距离和角度阈值是不是太大
- `TriggerShadowsRenderForFrame()` 有没有真的被调用

### 4. 构建与资源层

这里回答的是：

`即使逻辑都对了，运行时有没有真的拿到它需要的 shader 本体和关键 variant。`

典型检查项包括：

- `StaticShaders.prefab` 是否进入构建物
- `ProcedurePreload` 是否真的加载了静态 shader 集
- `BundleSVC.shadervariants` 是否覆盖了主光阴影关键路径
- `Hidden/CopyShadowMap` 是否真的能在运行时被找到

只要先把这四层立住，后面的排查顺序就会稳很多。

---

## 先给一张症状总表

如果你只想先有一张速查表，先看这个：

| 症状 | 最可能落在哪一层 | 第一检查点 |
|---|---|---|
| 完全没影子 | 输入层 / 管线层 | 主光、caster、当前 Quality、当前 RendererFeature |
| 只有 Editor 有，真机没有 | 管线层 / 构建层 | Android 默认档位、Pipeline Asset、StaticShaders、preload |
| 有影子但不刷新 | 触发层 | `UpdateMode`、相机阈值、Manual 触发链 |
| 只有静态没有动态 | 输入层 / 触发层 | `DynamicShadow`、动态对象路径、动态叠加链 |
| 只有动态没有静态 | 输入层 | 主光、caster bounds、静态底图是否退空图 |
| 相机动一下才有影子 | 触发层 | `Manual` 模式、距离/角度阈值、首次触发时机 |

这张表的目的不是替代正文，而是告诉你：

`先去哪一层切。`

---

## 症状一：完全没影子，先查主光、caster、Quality、Feature

这是最典型的“上游直接断掉”的情况。

对 CachedShadows 来说，真正值得先看的不是复杂的缓存逻辑，而是最便宜、最一票否决的四件事：

### 1. 主光还在不在

`CachedShadowsRenderPass.Setup()` 一上来就查 `mainLightIndex`。  
如果主光不存在，这条链直接退空图。

### 2. 主光有没有开阴影

如果主光自身 `Light.shadows == None`，CachedShadows 也不会硬给你造阴影。

### 3. 当前 cull 结果里有没有 shadow caster bounds

如果 `GetShadowCasterBounds(...)` 失败，说明这一帧根本没有合法投影体进入主光阴影生产链。  
这时你后面再怎么看 UpdateMode，都不会有正常结果。

### 4. 当前平台和当前 Camera 真的是那套 CachedShadows Renderer 吗

这一点在项目里特别容易被忽略。  
如果你根本没命中 `AndroidPipelineAssetLow -> ForwardRendererAndroid -> CachedShadowsRenderFeature` 这条链，前面三项即使都满足，也可能完全不是你以为的行为。

所以“完全没影子”最小排查顺序应该是：

1. 看主光
2. 看 caster
3. 看当前 Quality / Pipeline Asset
4. 看当前 Renderer 上有没有 CachedShadowsFeature

不要一开始就去猜 `CopyShadowMap` 或 keyword。

---

## 症状二：只有 Editor 有，真机没有，先怀疑平台和构建边界

这类问题特别常见，因为 Editor 会同时掩盖两类边界：

- 当前用的不是 Android LOW
- 编辑器资源查找比真机宽松

所以“只有 Editor 有，Android 没有”时，最该先查的是下面几项。

### 1. Editor 当前质量档和 Android 平台默认档位是不是一回事

在这份工程里：

- `m_CurrentQuality: 5`
- `Android: 3`

这两个值不是一回事。  
前者更接近当前编辑器会话档位，后者才是 Android 平台默认档位。

如果你没有显式切到 Android LOW，就直接在 Editor 里验证，很多时候你根本没在看同一套资产。

### 2. Android LOW 是否真的绑定了 `AndroidPipelineAssetLow`

如果平台档位没命中这套 Asset，后面一切关于 `ForwardRendererAndroid` 和 `CachedShadowsRenderFeature` 的判断都会失真。

### 3. `StaticShaders.prefab` 和 preload 链是否真的成立

Editor 能找到 shader，不代表构建物里一定有。  
在 TopHeroUnity 里，`ProcedurePreload` 会显式加载 `StaticShaders.prefab`，再通过 `DPShaderLoader` 把静态 shader 查找链接起来。

如果这里断了，最典型的现场就是：

`编辑器里一切正常，真机某些隐藏 shader 路径直接空掉。`

### 4. 主光 receiver variant 是否真的还在

如果 `_MAIN_LIGHT_SHADOWS` 这批路径在构建里被裁了，画面上也可能表现成“真机没有阴影”。

所以这个症状优先不要先怪 caster，应该先问：

`是不是我验证的平台档位不对，或者运行时资源边界根本没接上。`

---

## 症状三：有影子，但不刷新，先查 UpdateMode 和相机触发器

这类问题最容易被误判成“缓存坏了”，但在 CachedShadows 里，很多时候它其实只是：

`没人告诉静态底图现在该重画。`

先看 `UpdateMode`：

- `EveryFrame`：每帧都重画静态底图
- `EverySecond`：每秒重画一次
- `Manual`：完全靠外部触发

如果你看到的是“有，但一卡一卡”，很可能只是：

`EverySecond 本来就只有秒级刷新。`

如果你看到的是“静止时不变，动一下才变”，那更像是：

`Manual 模式 + 相机移动触发器在工作。`

接着看相机上的 `CachedShadowsCameraManualUpdateFromMovement`：

- `ReRenderAfterDistance`
- `ReRenderAfterAngleCheck`

这两个阈值没达到，它就不会调用 `TriggerShadowsRenderForFrame()`。

所以这类问题最短的判断方式是：

1. 先看是不是 `Manual`
2. 再看相机脚本在不在
3. 再看阈值是不是太保守
4. 最后再看 feature 是不是被脚本成功找到

如果你把相机轻轻动一下，阴影就立刻更新了，往往说明：

`主链基本是通的，问题更可能落在触发时机，而不是阴影生产本身。`

---

## 症状四：只有静态，没有动态，或者反过来，说明什么

这类问题最容易让人误以为“项目里有两套完全独立的阴影系统”。  
其实不是。

在当前实现里，主光 CachedShadows 更像是：

- 静态底图一条路径
- 动态对象叠加一条路径
- 最后仍然收成一张给 receiver 用的主光阴影结果

所以：

### 只有静态，没有动态

优先怀疑：

- `DynamicShadow` 没开
- 动态对象没进动态阴影路径
- `Hidden/CopyShadowMap` 或动态叠加链有问题

### 只有动态，没有静态

优先怀疑：

- 主光本身没对上
- `GetShadowCasterBounds(...)` 失败
- 静态底图在 Setup 阶段就退到了空图

### 两边都不对

这时通常不要先纠结静态/动态分类，先回到更上游的：

- 主光
- caster
- Quality / Pipeline Asset
- 当前 Renderer / Feature

因为很多时候两边一起错，不是分类问题，而是整条主光输入链就没成立。

---

## 症状五：相机动一下才有影子，几乎就是“触发链在工作”

这个现象在 CachedShadows 里非常有代表性。

如果你看到的是：

`相机不动时影子不更新，动一下之后突然出来或刷新了`

先不要急着怀疑灯、caster 或 shader。  
更大的概率是：

`Manual 模式和相机位移/旋转阈值刚好在起作用。`

也就是说，这通常不是“系统坏了”的证据，反而经常是：

`系统其实是通的，只是你现在看到的是“按触发条件更新”的真实行为。`

最值得先看的就是：

- 当前是不是 `Manual`
- `ReRenderAfterDistance` 是否过大
- `ReRenderAfterAngleCheck` 是否过大
- 场景里是否缺少首次触发时机

如果一动相机就能更新，通常能先排除掉一大批“主光不存在”“Feature 没挂上”“shader 根本找不到”这类更重的问题。

---

## 一条最小排查顺序

如果你不想记很多分支，只想要一条最短路径，我建议按这个顺序走：

1. 先看主光是不是 Directional，且 `LightShadows` 不是 `None`
2. 再看当前是否真的有合法 shadow caster bounds
3. 再看当前平台 / 当前质量档是不是命中了你以为的 Pipeline Asset
4. 再看当前 Camera 最终使用的 Renderer 上有没有 `CachedShadowsRenderFeature`
5. 再看 `UpdateMode` 是什么，是否依赖 Manual 触发
6. 再看相机触发器和阈值
7. 最后再看 `StaticShaders.prefab`、preload、SVC 和隐藏 shader 边界

这条顺序的原则很简单：

`先查最便宜、最上游、最可能一票否决的问题；再查刷新时机；最后才查构建和资源边界。`

---

## 这篇真正想留下来的结论

如果最后只允许留一句话，我会把这篇收成这样：

`CachedShadows 的现场排查不要从“阴影算法对不对”开始，而应该先判断问题落在输入层、管线层、触发层还是构建层；一旦层级判断对了，排查成本会立刻下降。`

下一篇会继续把“猜测”收成“证据”：

`Frame Debugger、RenderDoc 和日志，到底该怎么配合，才能证明当前阴影到底来自哪条链路。`
