+++
title = "Unity 渲染系统 01d｜Frame Debugger 使用指南：逐 Draw Call 分析一帧画面"
description = "系统讲解 Unity Frame Debugger 的界面和用法，包括如何读懂 URP 的 Pass 顺序、检查材质参数和 Shader Keyword、定位批处理失效原因，以及常见渲染问题的定位流程。"
slug = "unity-rendering-01d-frame-debugger"
weight = 350
featured = false
tags = ["Unity", "Rendering", "FrameDebugger", "Debugging", "URP", "DrawCall"]
series = "Unity 渲染系统"
+++

> 如果只用一句话概括这篇，我会这样说：Frame Debugger 是 Unity 内置的"渲染回放器"，它把一帧画面里发生的每一次 Draw Call 和 Pass 切换都记录下来，让你能暂停在任意时刻，检查当时的材质参数、渲染目标和批处理状态。

前两篇把 Draw Call 和 Render Target 的概念讲清了。这篇用 Frame Debugger 把它们"看见"。

---

## 打开 Frame Debugger

```
Window → Analysis → Frame Debugger
```

点击 **Enable** 按钮，Unity 会暂停当前帧，进入帧调试模式。此时编辑器会冻结在那一帧的渲染结果，你可以自由浏览这帧的每一个渲染事件。

**两个使用场景：**
- **Play Mode 下**：调试运行时的真实渲染，包含所有动态物体和后处理效果，推荐首选
- **编辑器 Scene 下**：调试编辑器视图的渲染，适合快速检查场景设置，但不包含 Play Mode 专属逻辑

---

## 界面结构

Frame Debugger 分为左右两个区域：

**左侧：事件列表（Event List）**

显示这一帧里所有渲染事件的层级树，大致结构如下：

```
▼ Camera.Render（主相机）
  ▼ SetupForwardRendering
  ▼ RenderShadows          ← 阴影 Pass
      ▶ Draw Shadow [方向光]
  ▼ DrawOpaqueObjects      ← 不透明物体
    ▼ SRP Batch [12]        ← SRP Batcher 合批的 12 个 Draw Call
      ▶ RenderLoop.Draw
      ▶ RenderLoop.Draw
      ...
    ▶ Draw Mesh Instanced   ← GPU Instancing
  ▶ DrawSkybox             ← 天空盒
  ▼ DrawTransparentObjects ← 透明物体
      ▶ Draw Mesh
  ▼ PostProcessing         ← 后处理
      ▶ Blit（Bloom）
      ▶ Blit（Tonemapping）
      ▶ Blit（色彩校正）
```

点击任意一行，右侧更新为该事件的详细信息，Scene/Game 视图也会回退到这个事件执行完毕后的渲染状态。

**右侧：详情面板（Detail Panel）**

显示当前选中事件的：
- **Shader**：使用的 Shader 名称和 Pass 名称
- **Keywords**：当前激活的 Keyword 组合
- **Properties**：所有 Material 属性的当前值
- **Render Target 预览**：当前写入的 RT 内容

---

## 读懂 URP 的 Pass 顺序

URP 每帧的渲染分成固定的几个阶段，在 Frame Debugger 里能清楚看到：

| Pass 名称 | 做什么 | 写入哪个 RT |
|---|---|---|
| `DepthPrepass` | 把所有不透明物体的深度写入 Depth Buffer，不写颜色 | Depth Buffer |
| `Shadow Maps` | 从光源视角渲染场景，生成阴影深度贴图 | Shadow Map RT |
| `Opaque Forward` | 渲染所有不透明物体的颜色，利用 Depth Prepass 的深度剔除无效像素 | Color Buffer |
| `Skybox` | 对没有不透明物体覆盖的像素填充天空颜色 | Color Buffer |
| `Transparent` | 从后往前渲染半透明物体，Alpha 混合 | Color Buffer |
| `Post-processing` | 对整张 Color Buffer 做全屏效果（Bloom、Tonemapping 等） | 临时 RT → 最终 Color Buffer |

**调试技巧**：逐步点击这些 Pass 里的事件，观察 Scene/Game 视图的变化，能直观理解每个阶段为画面贡献了什么。

---

## 检查材质参数

选中某个 Draw Call 后，右侧 Properties 面板里能看到这次渲染时材质的实际参数值：

```
_BaseColor:    (1.0, 0.8, 0.6, 1.0)
_Roughness:    0.75
_Metallic:     0.0
_BaseMap:      [Texture2D: rock_albedo]
_NormalMap:    [Texture2D: rock_normal]
Keywords:      _NORMALMAP _RECEIVE_SHADOWS_OFF
```

**常见用途：**

- 材质颜色不对：检查 `_BaseColor` 是否被代码改过
- 反射/高光不对：检查 `_Metallic` 和 `_Roughness` 的实际值
- 法线效果没生效：检查 Keywords 里有没有 `_NORMALMAP`，以及 `_NormalMap` 贴图引用是否正确
- 代码运行时修改材质参数后验证：在 Play Mode 里暂停，检查参数是否如预期

---

## 定位批处理失效原因

选中某个 Draw Call，右侧会显示 **"Why this draw call is not batched"** 区域（如果它本可以合批但没有）。

常见原因：

| 原因 | 说明 |
|---|---|
| `Different materials` | 和上一个 Draw Call 使用了不同的 Material 实例 |
| `MaterialPropertyBlock is used` | 使用了 `MaterialPropertyBlock`，会打断 SRP Batcher |
| `Renderer does not support GPU instancing` | Shader 未开启 Instancing 支持 |
| `Static batching disabled` | 物体没有标记 Static，或静态合批被关闭 |
| `Mesh is not compatible` | 顶点格式不同，无法合并 |
| `Different shader keywords` | Keyword 组合不同，走不同的 Shader Variant |

看到批处理失效的原因后，可以针对性地调整：同一批物体尽量共享 Material 实例，需要每实例差异时用 GPU Instancing + `MaterialPropertyBlock`（注意 `MaterialPropertyBlock` 会打断 SRP Batcher，需要权衡）。

---

## 逐步回退定位问题的工作流

Frame Debugger 最有价值的用法是**逐步回退定位渲染问题**：

**问题：某个物体渲染结果不对**

1. Enable Frame Debugger，找到这个物体对应的 Draw Call（可以通过物体名或 Shader 名搜索）
2. 点击这个 Draw Call，检查 Properties 里的材质参数是否符合预期
3. 检查 Keywords 里的 Keyword 组合——是否激活了预期的变体
4. 看 RT 预览——颜色是否在这步就已经错了，还是被后续 Pass 覆盖
5. 往前逐步回退，找到第一个"画面开始不对"的事件

**问题：某个效果没有出现（比如 Bloom 没有）**

1. 展开 Post-processing 节点，检查 Bloom Pass 是否存在
2. 如果不存在，检查 Volume 配置和 Camera 的 Post-processing 开关
3. 如果存在但没效果，点击 Bloom 的 Blit 事件，检查输入 RT 的亮度是否超过 Bloom 阈值

**问题：Draw Call 数量异常高**

1. 看 Frame Debugger 里 Draw Call 总数和 SRP Batch 的合批情况
2. 找没有被合批的 Draw Call，查看"Why not batched"原因
3. 按原因分类处理：相同材质的物体尽量共享 Material，动态物体考虑 GPU Instancing

---

## 几个实用细节

**逐帧步进**：Frame Debugger 顶部有左右箭头，可以逐个事件前进/后退，观察画面变化——这比一次性看完整帧更容易发现异常。

**搜索过滤**：左侧事件列表支持文本过滤，输入 Shader 名或物体名可以快速定位目标 Draw Call。

**RT 通道切换**：右侧 RT 预览右上角有下拉菜单，可以单独查看 R/G/B/A/Depth 通道——查法线贴图效果或深度值时很有用。

**编辑器 vs 真机**：Frame Debugger 只能在编辑器里用，看不到真机的渲染。真机上的渲染问题需要用 RenderDoc（Android/PC）或 Xcode Metal Debugger（iOS）。

---

## Frame Debugger 做不到的事

Frame Debugger 是一个高层工具，它有明确的边界：

- **看不到顶点数据**：无法验证顶点位置、UV、法线的原始值是否正确
- **看不到贴图的实际 mip 层级**：无法确认采样的是哪个 mip，以及 mip 内容是否正确
- **看不到 Shader 的逐像素执行**：无法追踪某个像素的颜色是怎么算出来的
- **看不到 GPU Pipeline State 的完整细节**：Blend State、Depth State 等在 Frame Debugger 里只能看到部分

这些更深层的问题，需要 RenderDoc。下一篇讲 RenderDoc 的入门用法。
