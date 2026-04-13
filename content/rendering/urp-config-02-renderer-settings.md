---
title: "URP 深度配置 02｜Universal Renderer Settings：渲染路径、Depth Priming、Native RenderPass"
slug: "urp-config-02-renderer-settings"
date: "2026-03-25"
description: "Universal Renderer 的核心配置项深度解读：Rendering Path 的选择依据、Depth Priming 减少 OverDraw 的机制、Native RenderPass 在 TBDR 上的带宽节省、Intermediate Texture 的存在理由与关闭时机。"
tags:
  - "Unity"
  - "URP"
  - "Renderer Settings"
  - "Depth Priming"
  - "Native RenderPass"
  - "渲染配置"
series: "URP 深度"
weight: 1540
---
`UniversalRenderer`（即 Renderer Asset，挂在 Pipeline Asset 的 Renderer List 里）控制的是渲染路径本身的行为——不是"渲什么"（那是 Pipeline Asset 的职责），而是"怎么渲"。这几个参数直接影响 GPU 的工作方式，是 URP 配置里最值得深入理解的部分。

---

## Rendering Path

```
Rendering Path
  ├─ Forward（默认）
  ├─ Deferred
  └─ Forward+
```

前置篇（URP前-03）已经讲清楚三条路径的架构原理，这里只补充在 Renderer Settings 里选择时的实际依据：

**选 Forward 的条件**：
- 场景光源少（Additional Lights ≤ 4）
- 需要 MSAA
- 目标平台包括中低端移动设备（G-Buffer 带宽吃不消）
- 项目有大量半透明物体

**选 Deferred 的条件**：
- PC 或主机，不考虑移动端
- 场景光源密集（城市夜景、室内多灯）
- 可以用 FXAA / TAA 替代 MSAA
- 需要屏幕空间效果（SSAO 等在 Deferred 下效率更高，G-Buffer 的法线可以直接用）

**选 Forward+ 的条件**：
- 需要多光源（> 8 个 Additional Lights），但目标平台也包括移动端
- 需要 MSAA
- 目标 GPU 支持 Compute Shader（绝大多数 2019 年后的手机都支持）

选择后会影响到你的自定义 Pass 能否访问 G-Buffer、`_CameraDepthTexture` 的保障方式等，详见前置篇三。

---

## Depth Priming Mode

```
Depth Priming Mode
  ├─ Disabled
  ├─ Auto（默认）
  └─ Forced
```

### 什么是 Depth Priming

**OverDraw**（过绘制）是 Forward 渲染的核心性能问题：如果多个不透明 Mesh 在同一像素上叠加，GPU 会对每一层都执行完整的 Fragment Shader，最终只有最上层的结果被写入颜色缓冲。被遮挡的像素算了个寂寞。

**Depth Priming** 的解法：
1. 先跑一个 **Depth Prepass**：只写深度，不写颜色（Shader 极简，速度很快）
2. 然后跑 **Opaque Pass**：Depth Test 模式改为 `Equal`，只有深度值等于 Prepass 结果的像素才执行完整 Fragment Shader

结果：每个可见像素只执行一次 Fragment Shader，被遮挡的像素在 Early-Z 阶段被 GPU 丢弃，不进入 Fragment 阶段。

```
没有 Depth Priming：
  Pass A 的像素进 Fragment → 写颜色
  Pass B 的像素进 Fragment → 写颜色（覆盖 A）
  Pass C 的像素进 Fragment → 写颜色（覆盖 B）
  → Fragment Shader 执行了 3 次，只有最后一次有效

有 Depth Priming：
  Depth Prepass → 写最终深度
  Opaque Pass：Pass A 的像素深度 ≠ Prepass → 丢弃
               Pass B 的像素深度 ≠ Prepass → 丢弃
               Pass C 的像素深度 = Prepass → 执行 Fragment
  → Fragment Shader 只执行 1 次
```

### 三个模式的含义

**Disabled**：不做 Depth Prepass，没有 Depth Priming。适合场景几乎没有 OverDraw 的情况（开放世界顶视角、2D 场景）。

**Auto**（默认）：URP 自动判断是否需要 Depth Prepass。判断标准：
- 是否有 RendererFeature 声明需要深度（`ConfigureInput(ScriptableRenderPassInput.Depth)`）
- 是否开启了 SSAO 等依赖深度的效果

**Forced**：无条件跑 Depth Prepass，然后 Opaque Pass 用 `ZTest Equal`。适合：
- 场景 OverDraw 严重（大量 Mesh 堆叠的室内场景）
- Fragment Shader 非常复杂（PBR 材质 + 多次纹理采样），节省的 OverDraw 代价超过 Depth Prepass 的代价

### 什么时候 Depth Priming 反而亏

Depth Prepass 本身也有代价（一次额外的几何遍历）。如果场景 OverDraw 很低（每个像素平均被覆盖次数 < 1.5），Depth Priming 的代价超过节省，反而变慢。

判断方法：用 Unity Profiler 的 GPU 模块，对比开关 Depth Priming 前后的 `DrawOpaqueObjects` 耗时。

---

## Native RenderPass

```
Native RenderPass
  ├─ 关闭（默认 PC）
  └─ 开启（推荐 Mobile / Vulkan / Metal）
```

### 什么是 Native RenderPass

在 Vulkan 和 Metal API 里，有一个 **RenderPass** 概念（和 Unity ScriptableRenderPass 同名但完全不同）：它是 API 层面对"一组 RT 的一次绘制会话"的描述。

正常情况下，如果 URP 的一个 Pass 结束后，下一个 Pass 需要读取上一个 Pass 的 RT，GPU 必须：
1. 把 Tile Memory 的内容写回主存（Store）
2. 下一个 Pass 开始时从主存读回（Load）

这就是带宽消耗。

**Native RenderPass 开启后**：URP 会把多个连续的 Pass 合并为一个 Vulkan/Metal RenderPass（Subpass）。同一 RenderPass 内，相邻 Subpass 之间可以直接在 Tile Memory 里传递数据，**不经过主存**。

```
不开启 Native RenderPass：
  Pass A 写颜色 → Store 到主存 → Pass B Load 颜色 → Pass B 写入 → Store...

开启 Native RenderPass（TBDR 上）：
  Subpass A 写颜色 → Tile Memory
  Subpass B 直接从 Tile Memory 读颜色 → 写入
  → 整个过程不写回主存，带宽接近零
```

在 Deferred 路径下，这个优化非常显著：G-Buffer 写入和 Lighting Pass 读取可以合并为一个 Native RenderPass，G-Buffer 完全在 Tile Memory 里流转，不需要写回主存再读出来。

### 什么情况下不要开启

- **PC + DX11**：DX11 不支持 Subpass，开启 Native RenderPass 没有效果（URP 内部有平台检测，实际上 DX11 下会忽略这个设置）
- **自定义 Pass 在两个 Subpass 之间插入了不兼容的操作**：如果你的 RendererFeature 在 G-Buffer Pass 和 Lighting Pass 之间插入了一个读取深度 Texture（而不是 Subpass Input）的操作，会破坏 Native RenderPass 的合并，退化到常规 Store/Load

**如何确认有效**：开启后，用 Mali Graphics Debugger 或 Xcode GPU Frame Capture，可以看到多个 Pass 被标记为同一 RenderPass 的 Subpass，而不是独立的 RenderPass。

---

## Intermediate Texture

```
Intermediate Texture
  ├─ Auto（默认）
  ├─ Always
  └─ Never
```

### 什么是 Intermediate Texture

URP 不一定把最终渲染结果直接写入 Backbuffer（屏幕的显示缓冲区）。在很多情况下，它会先渲染到一张中间 RT（Intermediate Texture），最后 Blit 到屏幕。

为什么需要 Intermediate Texture：
- **后处理**：后处理效果需要读取颜色 RT，再写入——如果直接在 Backbuffer 上操作，就是读写同张 RT（不合法）
- **MSAA Resolve**：MSAA 的多采样缓冲需要 Resolve 到单采样 RT，才能被后续 Pass 采样
- **Camera Viewport 不是全屏**：Camera 只渲染到屏幕的一个区域时，需要一张独立的 RT

### 三个模式

**Auto**（默认）：URP 自动判断是否需要 Intermediate Texture。判断条件：
- 有后处理效果
- 开启了 MSAA
- Camera Viewport 不是全屏
- 有 RendererFeature 声明了 `ScriptableRenderPassInput.Color`

满足任一条件就分配 Intermediate Texture；否则直接渲染到 Backbuffer。

**Always**：强制使用 Intermediate Texture，无论是否需要。适合调试（确认某个效果是否因为缺少 Intermediate Texture 而表现异常）。

**Never**：强制不使用，直接渲染到 Backbuffer。**谨慎使用**：如果有后处理或 MSAA，选 Never 会导致渲染错误或警告。适合极度优化的、没有任何后处理的移动端项目，每帧节省一次 Blit。

### 对自定义 Pass 的影响

当 Intermediate Texture = Never 时，`renderingData.cameraData.renderer.cameraColorTargetHandle` 直接指向 Backbuffer。Backbuffer 的 RT 在部分平台上有限制（不能被 Shader 采样），所以如果你的自定义 Pass 需要先读取颜色 RT 再写入，必须开启 Intermediate Texture（用 Auto 或 Always）。

---

## Shadow Normal Bias 与 Filtering Quality

```
Shadows（Renderer 层的参数，和 Pipeline Asset 里的 Bias 是两套）
  ├─ Shadow Normal Bias
  └─ Filtering Quality（Soft Shadow 品质，覆盖 Asset 里的设置）
```

`UniversalRenderer` 里也有 Shadow 参数，它覆盖 Pipeline Asset 里的对应设置（Renderer 优先级更高）。多 Renderer 场景下，可以为不同 Camera 指定不同的 Shadow 精度。

---

## Renderer Settings 调参流程

```
1. Rendering Path
   移动端     → Forward
   多光源 PC  → Deferred 或 Forward+
   移动 + 多光 → Forward+

2. Depth Priming
   开放世界 / 2D          → Disabled（OverDraw 低，额外 Pass 不值得）
   有 SSAO / Depth 依赖   → Auto（URP 自动判断）
   室内密集 OverDraw      → Forced（测试后决定）

3. Native RenderPass
   Vulkan / Metal 平台    → 开启（尤其 Deferred）
   DX11 / 低端驱动        → 关闭

4. Intermediate Texture
   有后处理 / MSAA        → Auto（默认）
   极简移动端无后处理      → Never（节省一次 Blit）
```

---

## 导读

- 上一篇：[URP 深度配置 01｜Pipeline Asset 解读：每个参数背后的渲染行为]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})
- 下一篇：[URP 深度配置 03｜Camera Stack：Base Camera、Overlay Camera 与多摄像机组织]({{< relref "rendering/urp-config-03-camera-stack.md" >}})

## 小结

| 参数 | 核心作用 | 移动端建议 |
|---|---|---|
| Rendering Path | 决定光照架构 | Forward 或 Forward+ |
| Depth Priming | 减少 OverDraw | Auto；室内密集场景考虑 Forced |
| Native RenderPass | 在 TBDR 上避免 G-Buffer 写回主存 | 开启（Vulkan / Metal）|
| Intermediate Texture | 后处理和 MSAA 的中间缓冲 | Auto；无后处理时 Never |

下一篇（URP配置-03）讲 Camera Stack：Base Camera 和 Overlay Camera 的工作机制、渲染顺序的实际代价，以及多摄像机场景的正确组织方式。
