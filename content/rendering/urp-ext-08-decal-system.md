---
title: "URP 深度扩展 08｜Decal System：URP 贴花的配置、Technique 选择与移动端优化"
slug: "urp-ext-08-decal-system"
date: "2026-04-14"
description: "URP 内置的 Decal Renderer Feature 提供了 DBuffer 和 Screen Space 两种 Technique。本篇讲清楚两种方式的区别、配置方法、与渲染路径的关系、移动端的优化策略和常见问题。"
tags:
  - "Unity"
  - "URP"
  - "Decal"
  - "贴花"
  - "Renderer Feature"
  - "渲染管线"
series: "URP 深度"
weight: 1644
---
> **读这篇之前**：本篇假设你已经了解 Renderer Feature 的开发模式。如果不熟悉，建议先看：
> - [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})
>
> Decal 投影的数学原理见 → [Shader 核心技法 08｜Decal 贴花]({{< relref "rendering/shader-technique-08-decal.md" >}})

弹孔、血迹、地面油渍、墙面弹痕——这类动态出现在任意表面上的贴图效果，手写 Decal Shader 能实现，但 URP 14 以后已经内置了完整的 Decal Renderer Feature，大多数场景下直接用内置方案就够了。这篇讲的就是这个内置方案：怎么加、怎么选 Technique、怎么在移动端用好它。

---

## URP Decal Renderer Feature 概览

从 URP 14（Unity 2022.3 LTS）开始，Decal 作为内置 Renderer Feature 提供，不再需要手动写 Pass。

**添加方式**：

1. 打开你的 Universal Renderer Data
2. Inspector 底部点 **Add Renderer Feature → Decal**
3. Feature 添加完成后，会出现 Technique 选择和 Max Draw Distance 等配置项

添加后，URP 会在渲染流程中自动插入 Decal 相关的 Pass。你不需要手动管理 Pass 的注入时机——这是 Renderer Feature 机制帮你做的事情（如果不清楚这个机制，回看 ext-01）。

Decal Renderer Feature 提供两种 Technique：**DBuffer** 和 **Screen Space**。选哪种取决于你的渲染路径和目标平台。

---

## DBuffer vs Screen Space

这两种 Technique 的核心区别是：Decal 在渲染管线的什么阶段介入，以及能修改哪些表面属性。

| | DBuffer | Screen Space |
|---|---|---|
| **渲染路径** | 仅 Deferred | Forward + Deferred 均可 |
| **原理** | 写入 DBuffer（额外的 G-Buffer），在 Lighting 前修改表面属性 | 在不透明物体渲染后叠加，直接修改最终颜色 |
| **精度** | 高——可以修改法线、金属度、光滑度等完整 PBR 属性 | 中——主要叠加颜色和法线，金属度和光滑度受限 |
| **带宽** | 需要额外的 RenderTarget 存放 DBuffer 数据 | 不需要额外 RT |
| **移动端适用性** | 需要 Deferred 路径，移动端较少使用 | Forward 路径直接可用，移动端首选 |

**选择建议**：

- 移动端项目 → **Screen Space**。移动端普遍用 Forward 路径，DBuffer 不可用
- 主机/PC 项目用了 Deferred 路径 → **DBuffer**。精度更高，Decal 可以正确影响光照
- 如果需要 Decal 修改金属度和光滑度 → 必须 **DBuffer**，Screen Space 对这些属性的支持有限

一句话：**DBuffer 更精确但受限于 Deferred 路径，Screen Space 更通用但精度有妥协。**

### 为什么 Screen Space 在移动端更便宜

DBuffer 需要在 Lighting 之前写入额外的 G-Buffer RT（通常是 3 张：Albedo、Normal、MAOS）。在 TBR 架构下，这意味着：

1. 额外 3 张 RT 的 Store 操作（Tile Memory → 系统内存）
2. Lighting Pass 读取时额外 3 次 Load 操作
3. 打断 Native RenderPass 的 Pass 合并——因为 DBuffer Pass 插在 G-Buffer 和 Lighting 之间

Screen Space 在不透明物体渲染完成后执行，只在已经存在的颜色 RT 上叠加一次 Blit。不增加额外 RT，不打断 Pass 合并。代价是一次全屏 Draw（覆盖 Decal 投影范围的像素），而不是额外的 RT 切换。

在移动端带宽受限的环境下，这个差距可以很显著——特别是在 1080P+ 分辨率下，3 张额外 RT 的 Load/Store 带宽开销远大于一次局部区域的 Fragment 计算。

### 真正需要 DBuffer 的场景

只有当你需要 Decal **在光照计算之前修改表面属性**时，DBuffer 才不可替代：

- 地面水坑改变粗糙度（让反射更强）
- 雪地覆盖改变金属度和法线
- 受损表面改变 AO

如果 Decal 只是叠颜色和法线（弹孔、血迹、涂鸦），Screen Space 完全够用。

---

## 配置步骤

### 1. 创建 Decal 材质

新建材质，Shader 选择 **Shader Graphs/Decal**（或 `Universal Render Pipeline/Decal`）。这个 Shader 是 URP 内置的，不需要自己写。

材质上可以控制 Decal 影响哪些通道：

- **Affect Albedo**：是否修改颜色
- **Affect Normal**：是否叠加法线贴图
- **Affect MAOS**（Metal, AO, Smoothness）：是否修改 PBR 属性（仅 DBuffer 模式下完整生效）

### 2. 设置 Decal Projector 组件

在场景中创建一个 GameObject，添加 **Decal Projector** 组件：

- **Material**：指定上一步创建的 Decal 材质
- **Size**：投影体积的宽、高、深度。Decal 本质上是一个 Box 投影，这个 Size 控制 Box 的尺寸
- **Pivot**：投影中心点的偏移
- **Projection Depth**：投影深度，决定 Decal 能"穿透"多深。值太小会导致部分表面接收不到投影，值太大会穿透到不该出现的表面

### 3. 排序控制

- **Draw Order**：同一位置有多个 Decal 时，Draw Order 决定叠加顺序，值大的画在上面
- **Rendering Layer Mask**：控制 Decal 投影到哪些物体上，跟灯光的 Rendering Layer 是同一套机制

---

## 移动端优化策略

Decal 在移动端最容易出的问题是：开发时只放了几个看着没问题，上线后玩家场景里同时出现几十个弹孔，帧率直接崩。

### 限制可见数量

- **Max Draw Distance**：Decal Projector 上设置最大绘制距离。远处的 Decal 不需要画，这个参数直接裁掉
- 总量控制：游戏逻辑层面限制同屏 Decal 总数。比如弹孔场景，超过 20 个就移除最早的

### 减少 Draw Call

- 使用**图集纹理（Atlas）**：多个 Decal 共享同一张纹理图集和同一个材质，可以被合批。每个 Decal 一张独立贴图 = 每个 Decal 一个 Draw Call
- 同材质的 Decal 尽量集中管理

### Technique 选择

- 移动端用 **Screen Space + Forward 路径**，这是标准组合
- 不要在移动端强行开 Deferred 只为了用 DBuffer Decal——Deferred 本身的带宽开销在移动端已经很重

### 注意 Native RenderPass 兼容性

Decal 的 Screen Space Pass 可能会**打断 Native RenderPass 的 Pass 合并**。如果你的项目依赖 Native RenderPass 来降低带宽（比如在 Vulkan/Metal 上合并 Opaque + Skybox + Transparent），加了 Decal 后要用 Frame Debugger 检查 Pass 是否被拆开了。如果被拆开了，需要权衡 Decal 数量和带宽增量。

### 纹理尺寸

Decal 贴图不需要很大分辨率。弹孔、裂缝这类效果，256x256 甚至 128x128 就够了。降的是纹理分辨率，不影响投影面积。

---

## Decal 与 RenderGraph（Unity 6）

Unity 6（URP 17）全面切换到 RenderGraph 之后，内置的 Decal Renderer Feature 已经适配了 RenderGraph——你不需要做额外工作。

但如果你要**在 Decal 基础上做自定义修改**（比如自定义 Decal 的混合模式），需要注意：

- 自定义 Pass 要走 `RecordRenderGraph()` 路径，不能再用旧的 `Execute()` API
- 在 RenderGraph Viewer（Window → Analysis → Render Graph Viewer）里可以看到 Decal 相关 Pass 在依赖图中的位置，确认你的自定义 Pass 与 Decal Pass 的执行顺序是否正确

---

## 常见问题

**Decal 不显示**

逐项排查：
1. Renderer Data 上加了 Decal Renderer Feature 没有？
2. Decal Projector 上指定了材质没有？材质的 Shader 是 Decal 系列的吗？
3. Projection Depth 是否覆盖到了目标表面？把 Depth 调大试试
4. 目标物体的 Rendering Layer 是否在 Decal 的 Mask 范围内？

**Decal 穿透到背面**

Projection Depth 设置过大，Decal 投影穿过了目标表面到达了背后的物体。缩小 Depth，或者在 Decal 材质上启用 Depth Test 来限制投影范围。

**性能突然变差**

大概率是同屏 Decal 数量过多。检查 Max Draw Distance 是否合理设置，以及游戏逻辑层面是否有 Decal 总数上限。用 Frame Debugger 看 Decal Pass 里有多少个 Draw Call。

**Decal 在某些物体上不显示**

检查 Rendering Layer Mask。Decal Projector 有自己的 Layer Mask 设置，目标物体的 Rendering Layer 必须在这个 Mask 里。

**DBuffer Decal 在 Forward 路径下不工作**

DBuffer 只在 Deferred 路径下生效。如果你的项目用的是 Forward 路径，切换到 Screen Space Technique。

**Decal 在某些角度突然消失或闪烁**

两个常见原因：
1. **Projection Depth 刚好卡在表面边缘**——Decal 的投影体积是一个 Box，当表面法线和投影方向接近平行时，深度测试结果不稳定。解决方法：适当增大 Projection Depth，或在 Decal 材质里降低边缘区域的 Alpha
2. **Decal 与 Z-Fighting**——Decal 的 Screen Space 模式在 Fragment Shader 里根据深度重建世界坐标，当 Decal 投影面和几何面几乎共面时，浮点精度不足会导致闪烁。在 RenderDoc 里抓一帧可以确认：如果 Decal 输出的深度值和几何面的深度值只差最后几位，就是 Z-Fighting

**Decal 叠在半透明物体上不生效**

这是设计限制，不是 bug。Screen Space Decal 在不透明 Pass 之后执行，半透明物体还没画。DBuffer Decal 写入 G-Buffer，半透明物体不走 G-Buffer。两种 Technique 都不能直接 Decal 到半透明表面上——如果确实需要，只能在半透明物体的 Shader 里手动采样 Decal 纹理。

**从 10 个 Decal 到 200 个：性能怎么变化**

Screen Space 模式下，每个可见 Decal 是一次 Draw Call（投影 Box 的 Mesh Draw）。10 个 Decal = 10 次 Draw Call，200 个 = 200 次 Draw Call。CPU 侧的提交开销是线性增长的。

GPU 侧，Decal 的 Fragment Shader 只在投影覆盖的像素上执行。10 个不重叠的弹孔 Decal，GPU 开销很低（每个只覆盖几百像素）。但 200 个弹孔密集分布在同一面墙上，OverDraw 会很严重——某些像素可能被 10+ 个 Decal 的 Fragment Shader 执行。

实际项目里的经验法则：同屏可见 Decal 控制在 20-30 个以内，配合 Max Draw Distance 裁剪远处的。超过这个数量，先在目标设备上用 Frame Debugger 确认 Decal Pass 的 Draw Call 数量和 GPU 耗时。

---

## 导读

- Renderer Feature 的完整开发模式 → [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})
- Decal 投影的数学原理和手写方案 → [Shader 核心技法 08｜Decal 贴花]({{< relref "rendering/shader-technique-08-decal.md" >}})
- 移动端 URP 的专项配置 → [URP 深度平台 01｜移动端专项配置]({{< relref "rendering/urp-platform-01-mobile.md" >}})
