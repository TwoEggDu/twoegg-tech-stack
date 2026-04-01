---
date: "2026-03-24"
title: "Unity 渲染系统 01c｜Render Target 与帧缓冲区：GPU 把渲染结果写到哪里"
description: "讲清楚 Color Buffer、Depth Buffer、Stencil Buffer、G-Buffer 和 MRT 各自是什么，GPU 怎么在多个 Render Target 之间切换，以及这些概念在 Frame Debugger 和 RenderDoc 里对应什么。"
slug: "unity-rendering-01c-render-target-and-framebuffer"
weight: 300
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "RenderTarget"
  - "Framebuffer"
  - "DepthBuffer"
  - "GBuffer"
  - "MRT"
series: "Unity 渲染系统"
---
> 如果只用一句话概括这篇，我会这样说：GPU 不是直接"往屏幕上画"，而是把每个阶段的结果写进一块内存缓冲区，最后把其中一块缓冲区显示到屏幕——这些缓冲区就叫 Render Target，理解它们是读懂 Frame Debugger 和 RenderDoc 的前提。

上一篇讲了 Draw Call 是 CPU 向 GPU 发出的工作请求。这篇要回答另一个问题：

**GPU 执行完 Fragment Shader 之后，输出的颜色写到哪里？为什么 Frame Debugger 里能看到渲染目标在不停切换？**

---

## GPU 不直接写屏幕

一个常见的误解是"GPU 直接把渲染结果输出到屏幕"。实际上 GPU 和显示器之间还有一个缓冲层：

```
GPU 渲染 → 写入 Framebuffer（帧缓冲区）→ 显示控制器读取 → 屏幕刷新
```

**Framebuffer（帧缓冲区）** 是 GPU 显存里的一块内存区域，存储着"当前这帧要显示的内容"。显示控制器以固定频率（60Hz、120Hz）从这块内存读取数据，发送给屏幕。

GPU 渲染的结果写进这块内存，而不是直接点亮屏幕上的像素。

---

## 帧缓冲区的组成

一个完整的帧缓冲区通常包含几个独立的缓冲层，每层存储不同类型的数据：

### Color Buffer（颜色缓冲）

存储每个像素的最终颜色，格式通常是 RGBA（每通道 8 位，共 32 位）或 RGBA16F（HDR 渲染时用浮点格式）。

这就是最终显示到屏幕上的那张"图"。

### Depth Buffer（深度缓冲）

存储每个像素到相机的距离（深度值），精度通常是 24 位或 32 位浮点数。

**用途**：深度测试时，GPU 比较新像素的深度值和 Depth Buffer 里已有的值——更近的通过测试，写入 Color Buffer；更远的被丢弃。这就是"后面的物体被前面的物体遮挡"的实现机制。

**Depth Prepass**：URP 在渲染不透明物体之前，有一个专门的 Depth Prepass——先把场景里所有不透明物体的深度值写入 Depth Buffer，不写颜色。这样在后续的颜色渲染阶段，被遮挡的像素在深度测试阶段就直接被丢弃，不需要执行 Fragment Shader，节省了大量无效的着色计算。

### Stencil Buffer（模板缓冲）

通常和 Depth Buffer 打包在一起（D24S8 格式：24 位深度 + 8 位模板），每个像素存储一个 8 位整数。

**用途**：作为渲染遮罩。比如"只对模板值等于 1 的像素执行描边效果"——先渲染物体时把模板值写成 1，再渲染描边 Pass 时只处理模板值为 1 的像素范围。UI 遮罩、Portal 效果、描边、角色轮廓等特效经常用到。

---

## Render Target：可写入的纹理

**Render Target（RT）** 是一个更通用的概念：任何 GPU 可以向其写入渲染结果的纹理都叫 Render Target。屏幕的 Color Buffer 是一个 RT，但 RT 不只是屏幕缓冲——任何中间渲染结果都可以写入临时 RT。

Unity 里的 `RenderTexture` 资产就是一个可配置的 RT：指定分辨率、颜色格式、是否有深度缓冲。一个 Camera 可以把渲染结果写入 RenderTexture，而不是屏幕，然后把这张 RT 当作贴图用在其他地方（镜面、监控画面、小地图等）。

---

## 为什么渲染过程中 RT 会切换

现代渲染管线不是一次性把所有东西画完——它分成多个 **Pass**，每个 Pass 可能写入不同的 RT。

这些 Pass 不是为了"把一帧拆得更复杂"，而是因为不同阶段要先生产不同类型的中间结果，后面的阶段才能继续使用。一个简单的判断方法是：**谁是后面要采样或依赖的数据，谁就得先生成；谁必须基于已经形成的颜色结果做混合或整屏处理，谁就只能放后面。**

下面这组顺序更适合理解成"常见 Pass 类型与依赖关系示意"，不是所有项目都会完整出现：

```
Shadow Map Pass   → 写入 Shadow Map RT（一张独立的深度贴图）
Depth Prepass     → 写入 Depth Buffer（只写深度，不写颜色，可选）
Opaque Pass       → 写入 Color Buffer + Depth Buffer（并采样 Shadow Map）
Transparent Pass  → 基于已有 Color Buffer 做 Alpha 混合（从后往前，通常不写深度）
Post-processing   → 读取 Color Buffer → 处理 → 写入另一张 RT → 最终输出
```

顺序背后的原因可以直接记成四句：

- `Shadow Map Pass` 往往在前面，因为不透明物体做光照时要先拿到阴影结果。
- `Depth Prepass` 也是服务型 Pass：它先把深度写好，让后面的不透明着色少做无效 Fragment，也能给依赖深度纹理的效果提供输入；如果项目里没人依赖它，这个 Pass 也可能根本不存在。
- `Transparent Pass` 必须放在不透明物体后面，因为透明混合依赖已经写好的背景颜色，而且半透明通常不写深度，只能按从后往前的顺序叠上去。
- `Post-processing` 几乎总在最后，因为它处理的对象已经不是某个单独物体，而是整张相机颜色结果。

每次从一个 RT 切换到另一个 RT，在图形 API 层面叫做 **Render Target 切换**（或 Framebuffer 切换）。这个切换有性能代价，尤其在移动端的 TBDR（Tile-Based Deferred Rendering）架构上，频繁切换 RT 会打断 tile 缓存，造成显著的带宽开销。

这就是为什么 Frame Debugger 里能看到渲染过程中 RT 在不断变化。

---

## G-Buffer：延迟渲染的中间缓冲

前向渲染（Forward Rendering）在 Fragment Shader 里直接计算光照，每个物体画一次。

延迟渲染（Deferred Rendering）把这件事拆成两步：

**第一步：几何 Pass（Geometry Pass）**
把每个物体的表面信息写入多张 RT（统称 **G-Buffer**）：

```
G-Buffer 组成（HDRP 为例）：
  RT0：Albedo（RGB）+ Smoothness（A）
  RT1：Normal（RGB）+ ...
  RT2：Metallic / AO / ...
  Depth Buffer：深度
```

**第二步：光照 Pass（Lighting Pass）**
对每个像素，从 G-Buffer 里读出表面信息，计算所有光源的光照贡献，写入最终的 Color Buffer。

这样做的好处是：光照计算只对最终可见的像素做一次，不浪费在被遮挡的像素上（无论场景里有多少物体叠加）。代价是需要多张 RT，显存占用更高，且对透明物体支持差。

---

## MRT：同时写入多个 Render Target

**MRT（Multiple Render Targets，多渲染目标）** 允许一次 Fragment Shader 执行同时向多张 RT 写入不同数据。

G-Buffer 的几何 Pass 就是 MRT 的典型应用——Fragment Shader 一次执行，同时输出 Albedo、Normal、Metallic 到不同的 RT，而不是对同一场景跑多次。

Unity 里通过 `SV_Target0`、`SV_Target1`、`SV_Target2` 等语义，在一个 Fragment Shader 里输出多个值到不同的 RT 绑定槽。

---

## Double Buffering（双缓冲）与 VSync

屏幕和 GPU 之间有一个同步问题：GPU 向 Color Buffer 写入时，如果显示控制器同时在读取同一块内存，会出现"撕裂"——屏幕上半截显示旧帧，下半截显示新帧。

解决方式是**双缓冲（Double Buffering）**：准备两个 Color Buffer，GPU 向后台缓冲（Back Buffer）写入，显示控制器从前台缓冲（Front Buffer）读取；GPU 写完之后，两个缓冲互换角色（Swap/Present）。

**VSync（垂直同步）** 在屏幕刷新信号到来时才执行 Swap，保证不会在屏幕刷新中途切换。代价是 GPU 有时必须等待屏幕刷新，帧率被锁定为屏幕刷新率的整数因子。

---

## 在 Frame Debugger 里看 Render Target

Frame Debugger 右上角有一个 RT 预览窗口，显示当前选中的 Draw Call 写入的是哪张 RT，以及 RT 的当前内容。

点击不同的 Draw Call，可以看到 RT 在不同阶段的变化：

- Depth Prepass 阶段：预览窗口里只有深度图（灰度，近处白，远处黑）
- Opaque Pass 阶段：Color Buffer 逐步被填充
- Shadow Pass 阶段：切换到 Shadow Map RT，显示深度值
- Post-processing 阶段：Color Buffer 经过多次全屏处理，画面逐步变化

可以通过下拉菜单切换预览哪个通道（R/G/B/A/Depth），这在调试法线贴图（看 G-Buffer 的 Normal RT）或遮蔽值时很有用。

---

## 在 RenderDoc 里看 Render Target

RenderDoc 的 **Texture Viewer** 和 **Pipeline State** 面板能看到更完整的 RT 信息：

- **Output Merger（OM）** 面板：显示当前 Draw Call 绑定了哪些 RT，每个槽位绑定的是什么格式的纹理
- **Texture Viewer**：可以打开任意一张 RT，查看任意 mip、任意通道的内容，还能用 Range 调整显示范围（方便查看 HDR 值）

接下来会先补一篇调试视角的补桥文，把 Draw Call、Pass 和 Render Target 这些概念映射到工具界面上；再分别用 Frame Debugger 和 RenderDoc 把这篇讲的结构"看见"。

---

## 和下一篇的关系

有了 Draw Call（01b）和 Render Target（01c）的概念基础，接下来先补一篇调试视角的补桥文：为什么工具里总在看 Draw Call、Pass 和 RT，以及这三个对象分别对应渲染链的哪一层。把这层桥接好之后，再进入 Frame Debugger，工具就会真正用起来。
