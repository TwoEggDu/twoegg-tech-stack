---
title: "CachedShadows 阴影缓存 07｜阴影画质问题怎么查：锯齿、漂浮、漏光、抖动，到底该调哪一层"
slug: "cachedshadows-07-visual-quality-debug"
date: "2026-04-01"
description: "把 TopHeroUnity 里 CachedShadows 的画质问题拆成几层来查：先区分这是 Shadow Map 基础质量问题、阴影相机范围问题、缓存刷新节奏问题，还是动态叠加链问题，再去调分辨率、距离、相机覆盖和刷新策略。"
tags:
  - "Unity"
  - "URP"
  - "CachedShadows"
  - "Shadow"
  - "Quality"
  - "Troubleshooting"
series: "CachedShadows 阴影缓存"
primary_series: "cachedshadows"
series_role: "article"
series_order: 70
weight: 1770
---
前面几篇主要解决的是两类问题：

- 这套系统到底在干什么
- 它现在到底有没有真的生效

但现场里还有另一类问题，会比“有没有阴影”更常见，也更折磨人：

`阴影是有了，但就是不好看。`

比如：

- 边缘发虚、发锯齿
- 阴影像飘起来一样，贴不住物体
- 远处突然断掉
- 镜头一动就抖
- 平时还好，一到触发刷新那一帧就明显跳一下

这类问题如果不分层，很容易一路乱调：

- 先改刷新阈值
- 再改 shader
- 再改 shadow distance
- 最后连自己都不知道到底是哪一层起了作用

如果把这篇压成一句话，我会写成：

`CachedShadows 的画质问题不要混着查，先分清这是 Shadow Map 基础质量问题、阴影相机范围问题、缓存刷新节奏问题，还是动态叠加链问题，再去调对应那一层。`

---

## 先把“画质问题”分成四层

对于 `TopHeroUnity` 当前这套实现，我建议把画质问题先压到四层：

### 1. Shadow Map 基础质量层

这层主要回答：

`这张阴影图本身够不够细、够不够稳。`

典型参数包括：

- 阴影分辨率
- shadow map render scale
- shadow distance
- cascade 数量

这层的问题通常表现成：

- 阴影边缘粗
- 远处细节糊
- 走动时持续性闪烁或 shimmer

### 2. 阴影相机范围层

这层主要回答：

`阴影相机到底拍到了哪里，精度分配合不合理。`

在当前项目里，除了 `AndroidPipelineAssetLow` 自己的：

- `m_ShadowDistance: 50`
- `m_ShadowCascadeCount: 1`

`ForwardRendererAndroid` 上还额外有：

- `ShadowResolutionOverride: 1024`
- `ShadowMapRenderScale: 1`
- `OverrideMaxShadowDistance: 0`
- `MaxShadowDistanceOverride: 128`

此外还有 `CachedShadowsMainLightCameraOverride`，会直接改：

- `CameraOverride.nearClipPlane`
- `CameraOverride.farClipPlane`

这层的问题通常表现成：

- 阴影范围不对
- 近处精度被稀释
- 远处突然没影子
- 某些角度下阴影像被裁掉

### 3. 缓存刷新节奏层

这层主要回答：

`阴影图并不是“画得不好”，而是“更新时机不对”。`

这在 `Manual` 模式下尤其常见。  
如果问题只在“刷新前后”出现跳变，而不是持续每帧都难看，那更像是节奏问题，而不是阴影图质量问题。

典型表现是：

- 相机不动时阴影不变
- 相机稍微动一下，阴影突然整体跳一下
- 只有跨过某个角度或距离阈值时，影子才更新

### 4. 动态叠加链层

这层主要回答：

`静态底图和动态对象影子，是不是根本没拼好。`

当 `DynamicShadow=1` 时，画面最后看到的并不是单纯一张静态图。  
它是：

- 静态底图
- 动态对象叠加
- receiver 最终绑定

这层的问题通常表现成：

- 静态环境还行，角色影子很怪
- 只有动态角色阴影在抖
- 静态和动态的清晰度、方向或接触感不一致

---

## 先给一张“症状 -> 优先怀疑层级”速查表

| 症状 | 更像哪一层的问题 | 第一个检查点 |
|---|---|---|
| 阴影整体发糙、边缘锯齿明显 | Shadow Map 基础质量层 | 分辨率、render scale、distance |
| 阴影像飘起来，贴不住接触面 | Shadow Map 基础质量层 / 偏移层 | 先回到 Shadow Map bias 思路 |
| 远处突然没影子 | 阴影相机范围层 | `m_ShadowDistance`、far clip、distance override |
| 只有某个角度突然整片跳一下 | 缓存刷新节奏层 | `Manual`、距离阈值、角度阈值 |
| 走路时持续闪烁，不是“隔一段才跳” | 基础质量层 | 分辨率、距离、单 cascade 精度 |
| 静态环境还行，角色阴影特别怪 | 动态叠加链层 | `DynamicShadow`、动态叠加 pass |
| 只有动态角色更新，静态底图总像旧的 | 刷新节奏层 | producer 是否重刷、触发时机 |
| 某些镜头下整片阴影被截断 | 阴影相机范围层 | `CachedShadowsMainLightCameraOverride` |

这张表的目的不是替代正文，而是先帮你决定：

`现在应该先去调哪一层，而不是一上来就全改。`

---

## 症状一：边缘锯齿、糊、粗，更像是“阴影图本身不够细”

如果你看到的问题是：

- 阴影边缘整体粗
- 远近都粗，不是某个刷新瞬间才粗
- 相机不动时它也一直粗

那优先怀疑的不是 `Manual` 触发，也不是 `CopyShadowMap`，而是：

`这张 shadow map 本身的精度就不够。`

在 `TopHeroUnity` 当前低配 Android 链路里，最先值得看的就是：

- `m_MainLightShadowmapResolution: 1024`
- `m_ShadowDistance: 50`
- `m_ShadowCascadeCount: 1`
- `ShadowResolutionOverride: 1024`
- `ShadowMapRenderScale: 1`

这组参数本身就是偏保守、偏低配的。  
所以如果你的直观感受是“能用，但不精细”，先不要惊讶，因为单 cascade + 中等分辨率 + 50 的阴影距离，本来就不是为了极致细节设计的。

更稳的判断顺序通常是：

1. 先看阴影距离是不是给得过大。
2. 再看 resolution / render scale 是否过低。
3. 再判断当前平台档位是不是本来就在接受“低配可用，但不追求细”的画质。

不要先去怀疑缓存机制本身。  
因为缓存只决定“什么时候重画”，不决定“重画出来的图天生有多细”。

---

## 症状二：阴影漂浮、漏光、接触感差，先回到 Shadow Map 基础问题

这类问题最容易被误判成：

`是不是 CachedShadows 的静态底图和动态叠加没对齐。`

但如果你看到的是：

- 阴影整体和物体有距离
- 接触边总像悬空
- 某些表面出现典型的 Shadow Acne / Peter Panning 感

那更应该先回到普通 Shadow Map 问题去想，而不是先怀疑缓存链。

原因很简单：

`CachedShadows` 接管的是主光阴影的生产与缓存节奏，不会把 Shadow Map 的基础物理限制变没。`

所以这类问题的更稳处理方式是：

1. 先确认这是不是普通的偏移 / 精度 / 采样问题。
2. 再确认它是不是“静态和动态都这样”。
3. 只有在“只有动态特别怪”时，才进一步怀疑动态叠加链。

如果你还没把 `Shadow Acne / Peter Panning / Bias` 这套基础概念吃透，建议回看：

- [unity-rendering-02b-shadow-map.md]({{< relref "rendering/unity-rendering-02b-shadow-map.md" >}})
- [urp-lighting-02-shadow.md]({{< relref "rendering/urp-lighting-02-shadow.md" >}})

---

## 症状三：远处突然断掉，或者某些镜头下整片范围不对，更应该先看“阴影相机拍到哪里”

这类问题经常看起来像：

- 阴影到某个距离就硬断
- 某些镜头角度下阴影范围明显不合理
- 角色明明还在画面里，但影子像被截走了

这里要优先怀疑的不是 shader，也不是构建，而是：

`当前阴影相机的覆盖范围和精度分配不合理。`

在这套实现里，有三层参数会一起影响它：

### 1. Pipeline Asset 自己的主光阴影距离

`AndroidPipelineAssetLow.asset` 里直接给了：

- `m_ShadowDistance: 50`
- `m_ShadowCascadeCount: 1`

这决定了低配 Android 主光阴影的基础可见范围和分层策略。

### 2. Renderer Feature 自己的额外覆盖配置

`ForwardRendererAndroid.asset` 里还有：

- `OverrideMaxShadowDistance`
- `MaxShadowDistanceOverride`

如果这层被打开，它会进一步影响最终阴影相机的远裁剪范围。

### 3. `CachedShadowsMainLightCameraOverride`

这层更直接。  
它会改：

- `nearClipPlane`
- `farClipPlane`

如果 near/far 调得太激进，就会出现：

- 近处精度反而不稳
- 远处被硬切
- 某些视角下阴影范围很奇怪

所以碰到这类问题，更值得先问的是：

`先把“相机到底拍到哪里”弄清楚，再谈别的。`

---

## 症状四：镜头一动阴影就突然跳一下，更像“刷新时机”而不是“画质本身”

这个症状特别典型，也特别容易误判。

如果你观察到的是：

- 不动的时候阴影稳定
- 一旦跨过某个距离或角度，整片阴影突然更新
- 不是连续抖，而是“停很久，然后整体换一版”

那它更像：

`Manual 模式下的缓存刷新边界可见了。`

这和“阴影图质量差”不是同一个问题。

在 `TopHeroUnity` 当前项目里：

- `UpdateMode: 2`
- 主相机上的 `CachedShadowsCameraManualUpdateFromMovement` 会在位移超过 `5`、旋转超过 `15` 时触发刷新

所以如果症状刚好长这样，更像是在提醒你先回到：

1. 当前是不是 `Manual`
2. 相机触发脚本是不是在工作
3. 距离阈值和角度阈值是不是太保守

这类问题不要先靠加分辨率解决。  
因为分辨率再高，也只会得到“更清楚地跳一下”的结果。

---

## 症状五：移动时持续抖，不是“过阈值才跳”，更像基础精度问题

要把两种“看起来都像抖”的问题分开：

### 第一种：离散跳变

表现是：

- 平时不变
- 某一刻整片切换

这更像刷新节奏问题。

### 第二种：连续闪烁 / shimmer

表现是：

- 镜头移动时每帧都在轻微抖
- 不是整片切换，而是边缘一直闪

这更像：

- 分辨率不足
- 阴影距离过大导致 texel 密度太低
- 单 cascade 精度不够
- 普通 Shadow Map 的稳定性问题

这两类问题如果不先分开，你会非常容易把“基础阴影精度不足”误调成“刷新阈值不对”，或者反过来。

---

## 症状六：静态环境还行，角色阴影特别怪，更像是动态叠加链没有接稳

当 `DynamicShadow=1` 时，静态和动态不是一回事。

所以如果你看到：

- 场景建筑、地面影子基本正常
- 但角色、怪物、动态机关影子特别怪

那最先怀疑的应该不是主缓存底图，而是：

`动态叠加链有没有完整闭环。`

这时更值得确认的是：

- 动态对象 pass 有没有执行
- `Hidden/CopyShadowMap` 有没有先把静态底图拷到动态图
- 最终 receiver 绑定的是哪张图

也就是说，这类问题最合适的切入点不是“再调 Android LOW 的全局分辨率”，而是先证明：

`静态底图和动态叠加到底有没有按预期拼在一起。`

这一层怎么拿证据，可以直接回看上一篇：[06｜怎么证明当前阴影来自哪条链路：Frame Debugger、RenderDoc、日志各看什么]({{< relref "rendering/cachedshadows-06-validation-and-proof.md" >}})。

---

## 把画质判断顺序收成一条主线

如果你现在的状态是“已经确认有阴影，但画面效果不对”，前面的判断也可以收成一条很短的主线：

1. 先分清是“持续难看”还是“刷新瞬间跳变”。
2. 如果是持续难看，先查 Shadow Map 基础质量层。
3. 如果是某个阈值才跳，先查 `Manual` 刷新节奏层。
4. 如果远处断、范围怪、某些镜头被裁，先查阴影相机范围层。
5. 如果只有角色怪、静态环境还行，再查动态叠加链层。

这条主线的关键其实只有一句话：

`先判断症状属于哪一层，再动参数。`

不然你会在：

- 分辨率
- 阴影距离
- camera override
- Manual 阈值
- dynamic shadow 开关

这几组互相影响的参数之间来回打转。

---

## 这篇真正想留下来的结论

`CachedShadows` 会带来新的刷新节奏问题和新的动态叠加边界，但它并不会让 Shadow Map 的基础规律失效。  
所以画质问题最稳的查法，不是把一切都归因到“缓存阴影”，而是先分清：这到底是阴影图本身的精度问题、阴影相机范围问题、缓存刷新节奏问题，还是静态与动态拼接问题。只有层级分对了，参数调整才会有方向。

如果你现在已经把“原理、链路、交付、验证、画质、取舍”都串起来了，这组专题的一期主线就基本完整了。下一步更适合做的，不再是继续堆原理，而是挑一个真实 case，把“Editor 有、真机没有”或“相机移动才刷新”做成完整事故复盘。  
