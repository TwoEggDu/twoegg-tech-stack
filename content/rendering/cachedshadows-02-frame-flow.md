---
title: "CachedShadows 阴影缓存 02｜一帧里到底发生了什么：静态缓存、动态叠加、手动刷新"
slug: "cachedshadows-02-frame-flow"
date: "2026-04-01"
description: "沿着 TopHeroUnity 当前主光 CachedShadows 的真实实现，拆开一帧中的 Setup、静态阴影缓存、动态对象叠加和 receiver 交接，讲清它到底省掉了哪部分阴影成本。"
tags:
  - "Unity"
  - "URP"
  - "Shadow"
  - "CachedShadows"
  - "Android"
  - "Render Pass"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 20
weight: 1720
---
上一篇只回答了一个问题：

`CachedShadows 到底替代了 URP 主光阴影链路里的哪一段。`

这一篇继续往下拆，但仍然只盯着 TopHeroUnity 当前这条主光链，不展开 additional lights。  
我们现在要回答的是：

`一帧里到底发生了什么，什么被缓存，什么被重画，动态对象又是怎么补上去的。`

如果把这篇压成一句话，我会先写成：

`CachedShadows 不是简单地“把阴影存起来”，而是把主光阴影拆成“静态底图、动态补丁、receiver 交接”三段，并按不同更新节奏重新组织。`

---

## 先把“缓存”这件事说准确

很多人第一次听到 `CachedShadows`，脑子里会自动浮现一种模糊理解：

`是不是它把上一帧的阴影结果直接存下来，下帧继续用？`

这个理解只对了一半，而且很容易把你带偏。

TopHeroUnity 当前这套主光 CachedShadows，真正缓存的不是“最终屏幕上的阴影效果”，而是更靠前的一层：

`主光 shadow map 里相对稳定的那部分阴影生产结果。`

如果把它压成三类对象，会更容易看懂：

### 1. 静态 caster

这部分是地形、建筑、静态环境物体之类“不会每帧大变”的投影体。  
它们最适合缓存，因为每帧重画最浪费。

### 2. 动态 caster

这部分是角色、怪、动态机关之类“当前帧状态可能变化”的投影体。  
它们不能像静态物体那样长期复用，否则阴影会明显滞后。

### 3. receiver

这不是某个单独组件，而是所有最终会采样主光阴影的 shader 侧路径。  
对 receiver 来说，它不关心你是怎么生成 shadow map 的；它只关心：

- 阴影图在哪里
- 世界坐标怎么变进阴影空间
- 当前该走哪条阴影分支

把这三层拆开之后，“缓存阴影”的真实意思就清楚了：

`尽量缓存静态 caster 产生的底图，再把动态 caster 按当前帧状态叠上去，最后把结果按 URP receiver 能识别的协议交出去。`

---

## 一帧里大致怎么走

把 `CachedShadowsRenderFeature`、`CachedShadowsRenderPass` 和 post pass 合起来，一帧主线大致可以压成这样：

```text
CachedShadowsRenderFeature.AddRenderPasses
  -> CachedShadowsRenderPass.Setup
  -> CachedShadowsRenderPass.Execute
       -> 判断这帧是否需要重画静态底图
       -> 如果需要，渲染静态主光 shadow map
       -> 如果开启 DynamicShadow，复制静态底图并叠加动态对象阴影
  -> CachedShadowsPostShadowRenderSettingsPass.Execute
       -> PostShadowPassConfigureKeywords
       -> 把 keyword、矩阵和 receiver 常量补回去
```

如果只看文件分工：

- `CachedShadowsRenderFeature.cs` 决定这两个 pass 什么时候进当前 Renderer
- `CachedShadowsRenderPass.cs` 决定“这帧该怎么生产 shadow map”
- `CachedShadowsPostShadowRenderSettingsPass.cs` 决定“怎么把这张图重新接回 receiver 侧”
- `CopyShadowMap.shader` 只负责一件事：把静态主光阴影图的深度内容拷给动态叠加用的 RT

这四者放在一起，才是一条完整的一帧链路。

---

## Setup 阶段先检查什么

`Setup()` 是这套系统的第一道闸门。  
它并不是一上来就画，而是先判断：

`这帧到底有没有资格进入 CachedShadows 主光链。`

它最关键的前提检查有这几项：

### 1. 有没有主光

如果 `mainLightIndex == -1`，直接退到空图。  
没有主光，就没有这条主光 shadow map 链。

### 2. 主光本身是不是在投影

如果 `light.shadows == LightShadows.None`，同样直接退到空图。  
这一步说明 CachedShadows 不是无中生有，它仍然尊重灯自身的阴影开关。

### 3. 当前 cull 结果里有没有 shadow caster bounds

如果 `GetShadowCasterBounds(...)` 失败，说明当前这帧并没有有效的投影体参与主光阴影。  
这时它也不会硬画一张“假阴影图”，而是退到空图。

### 4. 方向光阴影矩阵能不能成功提取

后面每个 cascade 还要走 `ExtractDirectionalLightMatrix(...)`。  
如果这一步拿不到合法结果，同样没法继续生产正常的主光 shadow map。

更重要的是，它这里还做了一个很有代表性的动作：

```csharp
renderingData.shadowData.supportsMainLightShadows = false;
```

这句不是在说“世界里再也不允许有主光阴影”，而是在说：

`当前这帧接下来的主光阴影生成，不要走 URP 默认那条支持路径。`

也就是说，Setup 的作用不只是检查输入条件，还在明确把“生成权”从 URP 原生主光阴影链路切走。

---

## 为什么会退回空图

这套系统遇到前提不成立时，不是简单 `return`，而是走 `SetupForEmptyRendering(...)`。

它会做几件非常具体的事：

- 把 `m_CreateEmptyShadowmap` 设成 `true`
- 分配一张极小的空 shadow map
- 后续 `Configure` / `Execute` 阶段继续按“空图模式”完成收尾

这样做的目的不是“糊弄一下”，而是为了让整条渲染链保持稳定：

- receiver 侧仍然能拿到一套一致的 shadow 输入结构
- shader 不会因为某一帧缺资源就采到未定义内容
- 系统可以从“正常阴影”平滑退回“安全空态”

如果你把它压成一句话：

`空图不是报错结果，而是这套系统的安全降级态。`

这点非常重要。因为后面很多“为什么这帧突然没影子”的现象，本质上不是渲染崩了，而是 Setup 合法地判断“这帧只能退空图”。

---

## 静态底图是怎么来的

如果前提检查都通过了，接下来真正最贵的部分是：

`主光静态阴影底图怎么生成。`

这一段的核心思路是：

`只把静态 caster 画进主 shadow map。`

在 `RenderMainLightCascadeShadowmap()` 里，代码明确把 `ShadowDrawingSettings.objectsFilter` 设成了：

`ShadowObjectsFilter.StaticOnly`

这意味着主光底图生产时，只看静态投影体。  
于是它得到的是一张“尽量稳定、适合跨帧复用”的阴影底图。

如果 UpdateMode 允许复用，那么下一帧就不需要重新把整批静态 caster 全画一遍。  
这就是 CachedShadows 真正省掉大头成本的地方。

换句话说，它不是把“最终阴影效果”缓存了，而是把“最贵、最稳定的阴影生产部分”缓存了。

---

## DynamicShadow 为什么不是“另起一张新图”

很多人看到 `DynamicShadow`，会以为它的意思是：

`再给动态物体单独搞一套完全独立的 shadow map。`

TopHeroUnity 当前这套实现不是这么干的。

它走的是另一条更节省的思路：

`先把静态主光阴影底图拷到动态 RT 上，再把动态 caster 叠上去。`

这段逻辑在 `RenderDynamicObjectShadowmap()` 里很清楚：

1. 先判断 `DynamicShadow` 是否开启  
没开就直接返回。

2. 用 `ArtShaderUtil.Find("Hidden/CopyShadowMap")` 创建 `_copyDepthMat`

3. 先把 `m_MainLightShadowmapTexture` 拷到 `m_DynamicObjectShadowmapTexture`

4. 再把 `ShadowObjectsFilter.DynamicOnly` 的对象画上去

也就是说，动态阴影不是“从空白开始另画一套”，而是：

`在静态底图之上，补当前帧动态对象的那一层变化。`

这背后有两个好处：

- 静态环境阴影不需要每帧重建
- 动态对象仍然能保持当前帧的投影变化

所以“静态缓存 + 动态叠加”这句话不是一种形容，而是非常字面的实现方式。

---

## `Hidden/CopyShadowMap` 在动态叠加里到底做了什么

`Hidden/CopyShadowMap` 在这条链里很容易被低估。  
它看起来只是个“拷贝 shader”，但实际上承担的是：

`把静态主光阴影底图的深度内容，原样搬进动态叠加的目标 RT。`

这点之所以重要，是因为这里拷的不是普通颜色图，而是阴影深度内容。

也就是说，动态叠加开始之前，`m_DynamicObjectShadowmapTexture` 里已经有一份静态底图。  
后面动态 caster 画上去时，是在同一张深度结果上继续补，而不是另起炉灶。

如果没有这一步，动态对象阴影就会失去和静态底图的连续性。  
receiver 侧也就拿不到一张同时包含“静态环境 + 当前动态对象”的统一 shadow map。

所以 `CopyShadowMap` 的角色更准确的说法应该是：

`它是动态阴影叠加的底图复制器。`

---

## UpdateMode 到底改变了什么

`CachedShadowsUpdateMode` 现在最关键的三种模式是：

- `EveryFrame`
- `EverySecond`
- `Manual`

这里最容易误解的是：

`它们改变的不是“要不要有阴影”，而是“静态底图什么时候重画”。`

### EveryFrame

这一帧一定重画静态底图。  
既然整张主光底图本来就每帧重建，那动态叠加路径反而没有必要再单独跑一遍。

所以在这套实现里，`EveryFrame` 模式下会直接跳过“先拷静态图再叠动态对象”的那条特殊路径。

### EverySecond

静态底图最多一秒更新一次。  
这意味着大头成本被摊薄了，但静态阴影变化也天然存在秒级滞后。

动态对象仍然可以继续按当前帧叠加，所以你会看到一种非常典型的组合：

`旧的静态底图 + 当前的动态对象阴影`

这就是它比“每秒把整个世界全重画一次”更划算的地方。

### Manual

静态底图什么时候重画，完全交给外部触发。  
`TriggerRefreshShadows()` / `TriggerShadowsRenderForFrame()` 这一组入口，就是在把“重画权限”往外透。

这类模式最适合：

- 地图变化很少
- 镜头变化不频繁
- 希望把主光阴影成本尽量挤到特定事件发生时

但要记住：

`Manual 不是“阴影停机”，而是“静态底图的重画时机不再自动推进”。`

---

## 为什么 receiver 侧仍然能正常采样

前面无论是静态底图，还是动态叠加，其实都还只是在“生产端”工作。  
真正让 shader 能正常吃到结果的，是后面的 `PostShadowPassConfigureKeywords()`。

这一步非常关键，因为它做的是“生产端 -> 消费端”的真正交接。

它至少会做三类事：

### 1. 补 keyword

例如：

- `MainLightShadows`
- `MainLightShadowCascades`
- `SoftShadows`

这决定了 receiver shader 该走哪条主光阴影分支。

### 2. 补 receiver 常量

例如：

- 世界到阴影空间的矩阵
- shadow 参数
- cascade sphere
- shadow offsets
- shadowmap size

这些东西决定了 shader 怎么解释这张图。

### 3. 在特定模式下切换最终采样的 shadow map

尤其是：

`DynamicShadow && UpdateMode == Manual`

这时 receiver 最终绑定的，不再是原始静态主光阴影图，而是已经叠好动态对象的那张 `m_DynamicObjectShadowmapTexture`。

所以这一层真正回答的是：

`receiver 采样到的，到底是哪张图，以及该按什么规则解释它。`

如果少了这一步，就算前面把 shadow map 生产得再漂亮，receiver 也可能走错分支或者根本不知道该怎么用。

---

## 为什么 Editor 观察经常会误导你

这套系统在编辑器下专门做过一个“方便观察”的调试分支。

在 `CachedShadowsRenderPass.Execute()` 里，你能看到：

```csharp
#if UNITY_EDITOR
if (!Application.isPlaying)
{
    _previousRenderAtTime = Time.unscaledTime;
    reRenderShadows = true;
}
#endif
```

对应的 editor 脚本里，也直接给出提示：

`Note: While not in play mode, shadows will always update every frame.`

这意味着什么？

意味着你在 Editor 非 Play Mode 下看到的，并不是“真实缓存行为”，而更接近：

`为了让你在编辑器里方便观察，强制每帧刷新。`

这会直接带来两类误判：

### 1. 你以为 Manual / EverySecond 没区别

因为编辑器调试分支把它们都“伪装”得更接近 EveryFrame 了。

### 2. 你以为阴影刷新链路在真机上一定没问题

实际上你看到的，只是编辑器帮你把重画频率抬高了。  
真正的运行时阈值、Manual 触发、动态叠加是否稳定，必须到 Play Mode 或真机里看。

所以后面一切排查里，都要先把这条边界记住：

`Editor 非 Play Mode 看到的 CachedShadows 行为，默认不等于运行时行为。`

---

## 这一帧最容易在哪些地方被误判

如果把这一篇压回现场，最容易误判的点主要有三个：

### 1. 把“没刷新”误判成“没阴影”

很多时候问题不是阴影生产链断了，而是静态底图根本没到该重画的时候。

### 2. 把“动态阴影单独叠加”误判成“又开了一套阴影系统”

实际上它仍然是在同一套主光 shadow map 语义上工作，只是把静态和动态分了不同节奏。

### 3. 把 Editor 观察结果误判成运行时证据

这类系统只要编辑器调试分支存在，就必须先把“编辑器方便观察”和“运行时真实行为”分开。

---

## 这篇真正想留下来的结论

如果最后只允许留一句话，我会把这篇收成这样：

`TopHeroUnity 当前这套主光 CachedShadows，本质上是在用“静态底图缓存 + 动态对象叠加 + receiver 侧重新交接”的方式，替代 URP 每帧整场景重建主光阴影的默认做法。`

下一篇会继续把这条链从代码层收回到项目层：

`从 Quality 到 Pipeline Asset，再到 Renderer、Camera 和运行时触发器，一个阴影到底是怎么在 TopHeroUnity 里真正被启用的。`
