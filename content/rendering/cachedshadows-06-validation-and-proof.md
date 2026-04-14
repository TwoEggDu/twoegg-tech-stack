---
title: "CachedShadows 阴影缓存 06｜怎么证明当前阴影来自哪条链路：Frame Debugger、RenderDoc、日志各看什么"
slug: "cachedshadows-06-validation-and-proof"
date: "2026-04-01"
description: '把 TopHeroUnity 里 CachedShadows 的验证拆成一条可落地的证据链：先证明当前平台确实命中了这套 Feature，再证明这一帧真的执行了缓存阴影 pass、动态叠加链和 receiver 交接，而不是只凭"屏幕上看见了影子"来猜。'
tags:
  - "Unity"
  - "URP"
  - "CachedShadows"
  - "Shadow"
  - "Validation"
  - "RenderDoc"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 60
weight: 1760
---
前一篇把 `CachedShadows` 最常见的几类症状先收成了输入层、管线层、触发层和构建层四个入口。  
但真到现场往下查时，你很快会遇到另一个更难的问题：

`我现在看到的这个阴影，到底怎么证明它来自 CachedShadows，而不是别的链路？`

这件事在 `TopHeroUnity` 里尤其容易被误判，因为这套实现并没有重新发明一套全新的 receiver 语义。  
它仍然会把结果回填到 `_MainLightShadowmapTexture`、主光阴影 keyword 和 receiver 常量里。  
也就是说，**屏幕上有影子**，甚至 **shader 里看到了主光阴影全局量**，都还不足以证明它一定来自 `CachedShadows`。

如果把这篇压成一句话，我会写成：

`验证 CachedShadows 不能只看"有没有影子"，而要按"当前链路命中 -> 阴影生产 pass 执行 -> 动态叠加发生 -> receiver 交接完成 -> 排除 URP 原生路径"这条证据链来收敛。`

---

## 为什么"看见了影子"不等于"证明了来源"

对新人来说，最自然的判断往往是：

- 画面里有影子
- shader 里也能走主光阴影分支

于是就下结论：

`那应该就是 CachedShadows 生效了。`

这一步其实不够严谨，原因有两个。

### 1. CachedShadows 复用了 URP receiver 侧的名字和语义

它在 producer 侧接管主光阴影生成，但在 receiver 侧仍然会回填：

- 主光阴影 texture
- 主光阴影矩阵
- 主光阴影 keyword
- soft shadow / cascade 等接收端常量

所以你在 shader 里看见 `_MainLightShadowmapTexture`，只说明"当前 receiver 正在按主光阴影路径取样"，并不能单独证明"这张图一定是 `CachedShadows` 生成的"。

### 2. 编辑器的很多现象会掩盖运行时边界

这个项目里，Editor 非 Play Mode 会强制 `reRenderShadows = true`。  
这意味着你在 Scene/Game 里拖一拖相机、看到阴影每帧都很正常，并不能直接说明 `Manual Update`、动态叠加链、预加载 shader 链在真机也都没问题。

所以验证这套系统，不能只靠"眼睛看结果"，而要靠**证据链**。

---

## 把验证收成一条证据链

对于 `TopHeroUnity` 当前这套实现，我建议把验证拆成五步：

1. 证明当前平台和当前相机真的命中了 `CachedShadows` 这套 Feature。
2. 证明这一帧确实执行了缓存阴影的 producer pass。
3. 如果开了 `DynamicShadow`，再证明动态叠加链也执行了。
4. 证明 receiver 侧确实拿到了这条链回填的阴影结果。
5. 再反过来证明：这不是 URP 原生主光阴影在工作。

这五步里，前四步是"正证据"，最后一步是"排除法"。

---

## 第一步：先证明当前链路真的命中了 CachedShadows

这一步最容易被跳过，但它其实是所有验证的起点。

如果当前平台压根没命中：

- `AndroidPipelineAssetLow`
- `ForwardRendererAndroid`
- `CachedShadowsRenderFeature`

那后面所有 Frame Debugger 和 RenderDoc 观察都可能是在看另一条链。

在 `TopHeroUnity` 里，当前低配 Android 链路的关键事实是：

- `AndroidPipelineAssetLow.asset` 把 `m_MainLightShadowsSupported` 设成了 `0`
- 默认 renderer index 指向 `0`
- `ForwardRendererAndroid.asset` 上挂了启用中的 `CachedShadowsRenderFeature`
- 主相机 prefab 的 `m_RendererIndex` 是 `-1`，也就是走 renderer 默认值

所以第一步你应该先回答的是：

`我当前验证的这一帧，到底是不是走在 Android LOW -> AndroidPipelineAssetLow -> ForwardRendererAndroid -> CachedShadowsRenderFeature 这条链上。`

如果这里没站稳，后面的结论都不稳。  
这一层怎么拆得更细，可以直接回看 [03｜从 Quality 到 Camera：TopHeroUnity 里一个阴影是怎么真正被启用的]({{< relref "rendering/cachedshadows-03-activation-chain.md" >}})。

---

## 第二步：证明"阴影生产 pass"真的执行了

只要当前 Feature 命中了，下一步就该去看：**这一帧到底有没有真的生产 shadow map。**

对于 `CachedShadows`，最值钱的证据不是"看见了阴影"，而是你能在工具里看到它自己的 pass 和命令缓冲名。

### 这一步最直接的证据是什么

最直接的入口就是找这两个 command buffer 名字：

- `[Cached Shadow] Render Main Light Shadows`
- `[Cached Shadow] Render Dynamic Object Shadows`

第一个说明主缓存阴影图的生产 pass 执行了。  
第二个说明动态对象阴影叠加链也执行了。

如果你在目标帧里能稳定看到第一个名字，就已经拿到了"producer 确实跑了"的强证据。  
如果你还能进一步看到它前后创建和写入了主光 shadowmap 相关 render target，那证据就更完整。

### 为什么这一步比"画面里有影子"更可靠

因为 producer pass 是**来源证据**。  
画面里的阴影只是**结果现象**。  
来源证据比结果现象更能回答"到底是谁干的"。

---

## 第三步：如果开了 DynamicShadow，再证明动态叠加链真的发生了

`TopHeroUnity` 当前的 `ForwardRendererAndroid.asset` 里：

- `UpdateMode: 2`
- `DynamicShadow: 1`

也就是 `Manual + DynamicShadow`。

这意味着你不能只证明"静态底图生产过"，还要分清楚：

- 静态底图有没有生产
- 动态对象有没有叠加到最终供 receiver 使用的那张图上

### 动态链里真正要确认的是什么

如果动态阴影路径真的走通，你至少应该能看到两件事：

1. `[Cached Shadow] Render Dynamic Object Shadows`
2. `Hidden/CopyShadowMap`

这里的逻辑很重要：

- 先把主缓存阴影图拷到动态阴影图
- 再把动态 caster 叠加上去
- 最终在特定模式下把动态图作为主光阴影纹理绑定给 receiver

所以如果你看到：

- 有主缓存 pass
- 没有动态 pass

那更像是"只有静态底图在工作"。  
如果你看到：

- 动态 pass 有
- 但 `Hidden/CopyShadowMap` 或动态对象投影 draw 没成立

那就说明"动态叠加链没有完整闭环"。

---

## 第四步：证明 receiver 侧真的拿到了这条链的结果

producer 跑了还不够。  
阴影图如果没有被正确交接给 receiver，画面上也仍然可能表现成"像是没影子"。

`CachedShadows` 在后置 pass 里会手动做几件事：

- 设置主光阴影相关 keyword
- 设置 soft shadow / cascade 相关 keyword
- 绑定主光阴影纹理
- 设置 world-to-shadow 矩阵和 receiver 常量

这一步的关键不是"看到这些全局量存在"，而是：

`把它和前面的 producer / dynamic 证据串起来。`

也就是说，更稳的判断方式不是：

`我看到了 _MainLightShadowmapTexture，所以一定是 CachedShadows。`

而是：

`同一帧里我先看到了 CachedShadows 的生产 pass，再看到 post pass 把主光阴影全局量和 keyword 设回去，所以 receiver 侧拿到的是这条链的结果。`

这才是完整证据。

---

## 第五步：最后再排除"其实是 URP 原生主光阴影"

如果你只做前四步，有时仍然会有人质疑：

`会不会只是原生 URP 阴影也在工作，CachedShadows 只是碰巧一起存在？`

在这个项目里，这一步其实有比较明确的排除证据。

### 1. Feature 启用时会主动关闭 URP 原生主光阴影开关

`CachedShadowsRenderFeature.OnEnable()` 和运行时启用逻辑里，会把 `URPGraphicsSettings.MainLightCastShadows` 设成 `false`。

### 2. RenderPass Setup 里还会把 `supportsMainLightShadows` 改成 `false`

这意味着这条链本身就是按"关闭原生主光阴影生产，由自定义 pass 接管"这个思路在跑。

所以如果你同时观察到：

- 当前平台链路命中 `CachedShadowsRenderFeature`
- `MainLightCastShadows` 被这套系统主动关掉
- `supportsMainLightShadows` 在 pass 里被改成 `false`
- 但目标帧仍然出现了主光阴影结果
- 并且同帧里能看到 `CachedShadows` 自己的 producer / dynamic / post 证据

那就已经足够排除"只是 URP 原生在工作"的猜测。

---

## 再把 Frame Debugger、RenderDoc 和日志放回不同位置

如果把这几种工具也放回同一条证据链里，它们各自回答的问题其实并不一样。

### Frame Debugger 更适合先确认"这件事有没有成立"

Frame Debugger 更像是管线级证据。  
它最适合先回答：

- 这一帧到底有没有执行 `CachedShadows` 自己的 pass
- pass 的先后顺序是不是符合预期
- 关键 render target 和 draw call 大概落在哪一段

对这套系统来说，它尤其适合先确认：

- 为什么当前帧完全没有 producer pass
- 为什么动态叠加链没执行
- 为什么某一帧没有刷新，但下一帧突然有
- 为什么你以为是低配链路，结果根本没有 `CachedShadowsRenderFeature`

如果你只是想先把"猜测"收成"事实"，它通常是最便宜的一步。  
前置工具基础可以回看 [unity-rendering-01d-frame-debugger.md]({{< relref "rendering/unity-rendering-01d-frame-debugger.md" >}})。

### RenderDoc 更适合再确认"它具体是怎么成立的"

RenderDoc 更像是帧内事件级证据。  
当你已经大概知道问题落在哪一段，但还想继续确认：

- 这张 shadow map 这一帧到底被谁写了
- `Hidden/CopyShadowMap` 有没有真的发生
- 动态对象是不是只叠加到了动态图
- receiver draw call 当时绑定的到底是哪张纹理

这时它的价值就会比 Frame Debugger 更高。  
对 `CachedShadows` 来说，RenderDoc 最重要的意义，是把"来源、拷贝、叠加、绑定"这四步拆开看。它尤其适合区分"静态底图正常，但动态叠加没上去"这一类问题。

如果你前面还没熟悉 RenderDoc，可先回看：

- [unity-rendering-01e-renderdoc-basics.md]({{< relref "rendering/unity-rendering-01e-renderdoc-basics.md" >}})
- [unity-rendering-01f-renderdoc-advanced.md]({{< relref "rendering/unity-rendering-01f-renderdoc-advanced.md" >}})
- [urp-ext-05-renderdoc.md]({{< relref "rendering/urp-ext-05-renderdoc.md" >}})

### 日志更像把范围先压小的第一轮筛子

日志的价值不在于替代图形调试工具，而在于它能快速告诉你：

`这一帧到底走到了哪个分支。`

`CachedShadowsRenderPass.cs` 里已经有几条对排查很有价值的日志：

- `[CachedShadow] EMPTY: No Main Light`
- `[CachedShadow] EMPTY: Light.shadows=None, light=...`
- `[CachedShadow] EMPTY: No shadow caster bounds in cull results`
- `[CachedShadow] Execute: reRender=..., frame=..., refreshIdx=..., cascades=...`
- `[CorgiCachedShadows|PostShadowPassConfigureKeywords()] shadowLightIndex == -1 ...`

它们分别对应：

- 主光输入是不是压根没成立
- 灯本身是不是没开阴影
- 当前 cull 结果里是不是没有合法 caster
- 这一帧到底有没有要求刷新
- post pass 有没有拿到有效 shadow light index

所以日志最适合做的是先把问题范围压小。  
例如，看到 `EMPTY: No Main Light` 时，就没必要再先猜 `CopyShadowMap`；看到 `reRender=false` 时，也更应该先回头看 `Manual` 触发链，而不是先怀疑主缓存 pass 失效。

---

## 把验证顺序收成一条主线

如果不是做一次特别深入的事故复盘，而只是想把"猜"变成"证据"，前面的内容其实已经能收成一条很短的主线：

1. 先确认当前 Quality / Pipeline / Renderer / Feature 确实命中了 `CachedShadows`。
2. 用日志看这一帧是不是 `EMPTY`、是不是 `reRender=false`。
3. 用 Frame Debugger 找 `[Cached Shadow] Render Main Light Shadows`。
4. 如果 `DynamicShadow=1`，继续找 `[Cached Shadow] Render Dynamic Object Shadows` 和 `Hidden/CopyShadowMap`。
5. 再确认 post pass 后 receiver 侧确实拿到了主光阴影全局量。
6. 最后结合 `MainLightCastShadows=false` 和 `supportsMainLightShadows=false`，排除 URP 原生路径。

这条顺序的重点不在"工具越重越好"，而在于先确认链路、再确认来源、最后再做归因和排除。  
对刚接手项目的人来说，这比一上来就抓 RenderDoc 全帧细看更稳。

---

## 这篇真正想留下来的结论

`CachedShadows` 最容易让人误判的地方，不是它太复杂，而是它太像 URP 主光阴影了。  
它复用了 receiver 侧的名字和语义，所以你必须把"链路命中、producer pass、动态叠加、receiver 交接、排除原生路径"这几段证据一段段补齐，才能真的证明当前看到的阴影来自哪条链。

如果下一步你面对的已经不是"来源不明"，而是"来源我知道了，但为什么画面还是锯齿、漂浮、抖动、远处断掉"，那就继续看下一篇：[07｜阴影画质问题怎么查：锯齿、漂浮、漏光、抖动，到底该调哪一层]({{< relref "rendering/cachedshadows-07-visual-quality-debug.md" >}})。
