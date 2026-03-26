---
title: "URP 深度配置 03｜Camera Stack：Base Camera、Overlay Camera 与多摄像机组织"
slug: "urp-config-03-camera-stack"
date: "2026-03-25"
description: "Camera Stack 是 URP 组织多摄像机渲染的核心机制。这篇讲 Base / Overlay Camera 的渲染顺序与资源复用逻辑、Clear Flags 的实际含义、Overlay Camera 的代价，以及常见多摄像机场景（UI 叠加、小地图、第一人称武器）的正确实现方式。"
tags:
  - "Unity"
  - "URP"
  - "Camera Stack"
  - "多摄像机"
  - "渲染配置"
series: "URP 深度"
weight: 1550
---
在 Built-in 管线里，多个摄像机直接叠加（每个 Camera 独立 Culling + 独立渲染），代价随摄像机数量线性增加。URP 的 Camera Stack 改变了这个模型：多个 Camera 共享 Base Camera 的 Culling 结果和深度，Overlay Camera 只负责"往上加内容"，而不是重新渲染整个场景。

---

## Base Camera 与 Overlay Camera 的本质区别

```
Base Camera
  ├─ 执行完整 Culling
  ├─ 清除颜色和深度缓冲（根据 Clear Flags）
  ├─ 渲染阴影、不透明、天空盒、半透明
  ├─ 执行后处理
  └─ 最终输出到 RT 或屏幕

Overlay Camera（叠加在 Base Camera 上）
  ├─ 不执行独立 Culling（复用 Base Camera 的 CullingResults）
  ├─ 不清除已有颜色（Clear Flags = Don't Clear）
  ├─ 不渲染天空盒（已有）
  ├─ 只渲染指定 Layer 的物体，叠加在 Base Camera 结果上
  └─ 不执行后处理（由 Base Camera 统一处理）
```

**关键点**：Overlay Camera 不是一个独立的完整渲染，它是 Base Camera 渲染流程的延伸。

---

## 如何配置 Camera Stack

在 Base Camera 的 Inspector 里（Camera 组件下的 `Stack` 列表）：

```
Main Camera（Base）
  Stack
    └─ [+] UI Camera（Overlay）
    └─ [+] Weapon Camera（Overlay）
```

在 Overlay Camera 的 Inspector 里，`Camera Type` 必须设为 `Overlay`（否则 URP 不允许把它加入 Stack）。

渲染顺序：Base Camera 先渲染，然后 Stack 列表里的 Overlay Camera 从上到下依次渲染，后面的叠加在前面的上面。

---

## Clear Flags 的含义

`Clear Flags` 控制 Camera 开始渲染之前如何清除缓冲区：

| Clear Flags | 颜色缓冲 | 深度缓冲 | 典型用途 |
|---|---|---|---|
| Sky Box | 渲染天空盒（用天空盒填充背景）| 清除 | 主场景摄像机 |
| Solid Color | 用 Background 色填充 | 清除 | 无天空盒的场景 |
| Depth Only | 不清除颜色（保留上一帧）| 清除 | 多 Camera 叠加，需要深度隔离 |
| Don't Clear | 不清除颜色 | 不清除 | Overlay Camera 默认 |
| Nothing | 不清除颜色 | 不清除 | （同 Don't Clear，不常用）|

**Overlay Camera 为什么用 Don't Clear**：它需要把内容叠加在 Base Camera 的结果上，而不是覆盖。清除颜色等于把 Base Camera 的结果抹掉了。

**Depth Only 的用途**：多个 Base Camera 叠加时（URP 支持多个 Base Camera 各自独立渲染到同一 RT），后面的 Base Camera 用 `Depth Only` 可以保留前面 Camera 的颜色，但重置深度，让自己的物体不被之前 Camera 的深度裁剪。

---

## Camera Stack 的渲染流程细节

URP 处理一帧时，按 Base Camera 为单位处理：

```
Base Camera:
  1. Culling（根据 Camera 的 CullingMask 和位置）
  2. Setup（设置 VP 矩阵等全局参数）
  3. Shadow Pass（如果有光源 + 开启阴影）
  4. Depth Prepass（如果 Depth Priming 开启）
  5. Opaque Pass
  6. Skybox Pass
  7. Transparent Pass
  8. Post-processing（如果此 Camera 开启了 Post Processing）

Overlay Camera 1:
  1. 不做 Culling（或用自己的 CullingMask 裁剪）
  2. 共享 Base Camera 的深度缓冲（可选，由 Clear Flags 决定）
  3. Opaque Pass（只渲染自己 CullingMask 里的物体）
  4. Transparent Pass

Overlay Camera 2:
  （同上）

最终输出（Blit 到屏幕或 Target RT）
```

### 深度复用与深度隔离

默认情况下，Overlay Camera **复用** Base Camera 的深度缓冲。这意味着：

- Overlay Camera 里的物体会被 Base Camera 渲染的物体遮挡（有正确的深度关系）
- 如果想让 Overlay Camera 的物体"永远在最前面"（比如第一人称武器不被墙壁遮挡），需要 **清除深度**

URP 里清除深度的方式：在 Overlay Camera 开始前，用一个 RendererFeature 执行 `cmd.ClearRenderTarget(clearDepth: true, clearColor: false, ...)`，或者在 Overlay Camera 的 `Before Rendering` 事件里插入 Clear Depth Pass。

实际上，更常见的做法是用 **Depth Range 分层**：主场景 Camera FOV = 60°，Near = 0.3，Far = 1000；武器 Camera FOV = 60°，Near = 0.01，Far = 2。武器 Camera 渲染范围 0.01–2 米，不会和场景深度冲突。

---

## Overlay Camera 的代价

Overlay Camera 比 Built-in 管线的多 Camera 叠加更轻量，但不是免费的：

**有代价的部分**：
- Overlay Camera 仍然要执行 DrawRenderers（对自己 Layer 的物体）
- 每个 Overlay Camera 有一次 CullingMask 过滤（即使共享 Base Camera Culling 结果，仍需要在结果集里过滤 Layer）
- 如果 Overlay Camera 有自己的后处理（URP 14+ 支持），额外执行后处理

**没有代价的部分**（相比独立 Base Camera）：
- 不重新做完整 Culling（节省 CPU 时间）
- 不重新渲染 Shadow Map
- 不重新执行 Depth Prepass

**不要滥用 Overlay Camera**：每增加一个 Overlay Camera，就增加一组 DrawRenderers 调用。如果叠加物体很少（比如 HUD 只有几个 Quad），用一个 World Space Canvas（UI Camera）或者 Screen Space Overlay Canvas 反而更简单。

---

## 后处理在 Camera Stack 中的位置

默认情况下，**只有 Base Camera 执行后处理**，Overlay Camera 的物体会在后处理之后叠加（因此不受 Bloom、Vignette 等效果影响）。

这正是武器 Camera 的正确行为：武器不应该因为 Bloom 而发光，也不应该被 Depth of Field 虚化。

如果确实需要 Overlay Camera 的内容也受后处理影响（比如叠加的特效粒子需要 Bloom），需要把后处理移到最后一个 Overlay Camera 里，并关闭 Base Camera 的后处理（URP 14+ 支持每个 Camera 独立的后处理开关）。

---

## 常见多摄像机场景的正确实现

### 场景一：UI 叠加在 3D 场景上

**方案 A（推荐）：Screen Space Overlay Canvas**

```
Main Camera（Base，渲染 3D 场景）
Canvas（Screen Space - Overlay，不需要 Camera，直接在最顶层渲染）
```

Screen Space Overlay 的 UI 始终在最顶层，不需要 Camera Stack，没有额外渲染代价。

**方案 B（有 3D UI 需求）：Overlay Camera**

```
Main Camera（Base，渲染 3D 场景）
  Stack → UI Camera（Overlay，Layer = UI，Clear Flags = Don't Clear）
Canvas（Screen Space - Camera，指向 UI Camera）
```

用于需要 3D UI 效果（UI 在世界空间里，或者需要和场景有深度关系）时。

### 场景二：第一人称武器不被墙壁遮挡

```
Main Camera（Base，Near = 0.3，Far = 1000，Layer = Default）
  Stack → Weapon Camera（Overlay，Near = 0.01，Far = 2，Layer = Weapon）
```

武器 Camera 的 Near Clip 极近（0.01），Far Clip 极短（2 米），把武器渲染到一个和场景不重叠的深度范围。不需要清除深度，利用深度范围天然隔离。

### 场景三：小地图

```
Minimap Camera（Base，Orthographic，渲染到 Minimap RT）
Main Camera（Base，渲染主场景）
```

小地图 Camera 是独立的 Base Camera，渲染到一张 RenderTexture（`minimap.targetTexture = minimapRT`），UI 上的 RawImage 显示这张 RT。两个 Base Camera 完全独立，不需要 Stack。

**注意**：小地图 Camera 的 Culling 和 Shadow 代价是完整的（它是独立 Base Camera），如果小地图分辨率低（256×256），可以关闭 Shadow、后处理，设置简化的 Renderer。

### 场景四：分屏（Split Screen）

```
Player1 Camera（Base，Viewport Rect = {0, 0, 0.5, 1}，Render Target = Screen）
Player2 Camera（Base，Viewport Rect = {0.5, 0, 0.5, 1}，Render Target = Screen）
```

两个独立的 Base Camera 各占一半屏幕。注意：URP 的 Post-processing 在 Viewport 渲染时可能有边界问题，分屏推荐各自渲染到独立的 RT，最后合并显示。

---

## Camera Stack 常见问题

**问题：Overlay Camera 的物体颜色不对（发黄、曝光异常）**

原因：Overlay Camera 的颜色空间（sRGB / Linear）或 HDR 设置和 Base Camera 不一致。检查 Overlay Camera 是否也设置了 HDR，两者要一致。

**问题：Overlay Camera 的物体被场景遮挡（应该在最前面）**

原因：Overlay Camera 复用了 Base Camera 的深度缓冲，场景物体的深度挡住了 Overlay 物体。解决：在 Overlay Camera 的渲染开始前，清除深度（ClearDepth Pass）。

**问题：Frame Debugger 里 Overlay Camera 的 Pass 很多，看不懂顺序**

Overlay Camera 的 Pass 紧跟在 Base Camera 的 Post-processing 之后，按 Stack 顺序排列。每个 Camera 的起始标记是 `Camera 0 "Camera Name"`，可以折叠查看。

---

## 小结

| 概念 | 含义 |
|---|---|
| Base Camera | 完整渲染：Culling、Shadow、Opaque、Skybox、Transparent、Post-processing |
| Overlay Camera | 叠加渲染：复用 Base Culling，只绘制指定 Layer，不执行后处理 |
| Camera Stack | 一个 Base Camera + 若干 Overlay Camera 的组合，共享渲染资源 |
| Don't Clear | Overlay Camera 的默认 Clear Flags，保留 Base Camera 的颜色结果 |
| Depth 复用 | Overlay 默认复用 Base 的深度；武器等"始终最前"的物体需要清除深度或用深度范围分层 |

URP 配置层到这里结束。下一步是光照与阴影层（URP光照-01：光照系统全貌），从主光、Additional Lights 的上限机制开始，讲清楚 URP 光照的完整架构。
