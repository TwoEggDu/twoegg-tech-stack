---
title: "URP 深度前置 03｜Forward、Deferred、Forward+：三条渲染路径对比"
slug: "urp-pre-03-rendering-paths"
date: "2026-03-25"
description: "从光照计算的本质矛盾出发，讲清楚 Forward 为什么慢、Deferred 如何解决多光源问题以及它的代价、Forward+ 如何在移动端友好性和多光源效率间取得平衡，以及在 URP 里选择路径对自定义 Pass 的具体影响。"
tags:
  - "Unity"
  - "URP"
  - "渲染路径"
  - "Forward"
  - "Deferred"
  - "Forward+"
series: "URP 深度"
weight: 1520
---
> **读这篇之前**：本篇会用到 Fragment Shader、深度测试、G-Buffer 等概念。如果不熟悉，建议先看：
> - [Unity 渲染系统 01｜几何与表面：Mesh、Material、Texture]({{< relref "rendering/unity-rendering-01-mesh-material-texture.md" >}})
> - [Shader 语法基础 09｜Fragment Shader 完整写法]({{< relref "rendering/shader-basic-09-fragment-shader.md" >}})

三条渲染路径要解决的是同一个问题：**场景里有很多光源，怎么让每个 Mesh 的每个像素知道自己被哪些光照亮，以及如何高效地完成这个计算**。

---

## 问题的根源：光照计算的组合爆炸

一个场景有 N 个 Mesh、M 个光源。每个像素的颜色 = 该像素在 M 个光源下的光照贡献之和。

朴素实现：每个 Mesh 对每个光源各渲染一次，最后 Additive 混合。

```
总绘制次数 = N × M

50 个 Mesh × 8 个光源 = 400 次 Draw Call
```

这是 Built-in Forward 的早期行为（每个光源一个 Pass），性能灾难。三条路径都是在想办法优化这个问题。

---

## Forward 路径：把多光源合并到一个 Pass

### 核心思路

不再是每个光源一个 Pass，而是在一个 Pass 里循环所有影响该 Mesh 的光源：

```hlsl
// Shader 里的伪代码
float3 color = AmbientLight();
for (int i = 0; i < lightCount; i++)
{
    color += CalculateLighting(surfaceData, lightData[i]);
}
return color;
```

```
总绘制次数 = N（每个 Mesh 一次）
每次 Draw Call 里：Shader 循环 lightCount 个光源
```

### 性能特征

**优势**：
- 天然支持 MSAA（多采样抗锯齿）——因为每个像素只被处理一次，MSAA 开销线性增加
- 半透明物体天然处理（Alpha Blend 在写入时就对）
- 硬件 Early-Z 优化有效（不透明物体前到后排序，已被遮挡的像素被 GPU 丢弃）

**劣势**：
- 光源数量是 Shader 里的常数（URP Forward 默认最多 8 个 Additional Light，超过会被忽略）
- 每个 Mesh 的 Shader 都要计算所有光源，即使某个光源根本不影响该 Mesh（浪费 ALU）
- 光源数量增加，Shader 变体数量也增加（Unity 用 Shader 关键字控制 1 / 2 / 4 / 8 个光源版本）

### URP Forward 的光源限制

URP Pipeline Asset 里：

```
Additional Lights
  ├─ Per Object Limit：4（每个物体最多 4 个 Additional Light）
  └─ Per Pixel: 启用（逐像素精确光照）
       Per Vertex: 备选（逐顶点近似，移动端省 GPU）
```

每个 Object 上的光源通过 Unity 的光源排序系统选出最近/最亮的 N 个。超出数量的光源会被忽略，或者降级为 Vertex Light（顶点光照插值，精度低）。

---

## Deferred 路径：把光照和几何分离

### 核心思路

把一帧拆成两个阶段：

**阶段一：Geometry Pass（填充 G-Buffer）**

渲染所有不透明几何体，不做光照计算，只把表面属性写入几张 RT：

```
G-Buffer 布局（以 URP Deferred 为例）：

RT0: Albedo (RGB) + 未使用 (A)              [RGBA32]
RT1: Metallic + AO + Roughness + 未使用      [RGBA32]
RT2: World Normal (RGB) + 未使用 (A)         [RGBA32]
RT3: 自发光 (RGB) + 未使用 (A)               [RGBA32]
Depth: 深度值                                [D32 / D24S8]
```

**阶段二：Lighting Pass（逐光源画全屏或球/锥）**

对每个光源，画一个覆盖其影响范围的几何体（平行光画全屏 Quad，点光源画球，聚光灯画锥），在 Fragment Shader 里从 G-Buffer 读取表面属性，计算光照并 Additive 写入颜色 RT：

```
对每个光源：
  如果是平行光：画全屏 Quad（影响所有像素）
  如果是点光源：画一个半径 = 光源 Range 的球体
  如果是聚光灯：画一个锥体

  Fragment Shader：
    从屏幕坐标重建世界坐标（利用 Depth）
    读 G-Buffer 拿表面属性
    计算光照
    Additive 混合到颜色 RT
```

### 性能特征

**优势**：
- 光照计算只对可见像素执行（Depth Test 早于光照）——Early-Z 的极致利用
- 增加光源数量的代价 = 增加 Lighting Pass 次数，和 Mesh 数量无关
- 多光源场景（城市夜景、游戏大厅）性能好

**劣势**：
- **带宽消耗大**：G-Buffer 需要多张 RT，填写和读取都是带宽开销。每帧 G-Buffer 总大小：4 张 RGBA32 × 分辨率 × 2（读写） = 2K 分辨率下约 200MB/s
- **不支持 MSAA**：G-Buffer 里存的是解析后的值，MSAA 与延迟渲染冲突（可用 FXAA / TAA 代替）
- **半透明物体无法用 Deferred**：G-Buffer 只能存一个表面的属性，半透明层叠无法处理，需要 Forward 补跑
- **移动端代价高**：TBDR 架构下，G-Buffer 的多 RT 写入会消耗 Tile Memory，如果 G-Buffer 总大小超过 Tile Memory 容量，驱动会把 Tile 写回主存（带宽爆炸）

### URP 里 Deferred 的额外限制

URP Deferred 在 2021.2 引入，有一些额外约束：

- 需要 `UniversalRenderer` 里选 "Deferred"
- Accurate G-Buffer 启用时，G-Buffer 布局会增加一张 RT（精度更高的 Normal）
- 需要 Depth Priming（提前 Depth Pass 减少 GBuffer 阶段 OverDraw）

---

## Forward+：用 Tile / Cluster 索引限制每像素光源数

### 核心思路

Forward+ 把屏幕分成小格（Tile），每个 Tile 在 CPU/Compute 阶段预计算"这个格子里有哪些光源影响它"，然后每个 Tile 的 Fragment Shader 只循环该 Tile 对应的光源列表。

```
CPU/Compute 阶段（Light Culling）：
  把屏幕分成 N×M 个 Tile（例如每个 Tile = 16×16 像素）
  对每个 Tile，测试哪些光源的球体/锥体与该 Tile 的 Frustum Slice 相交
  把相交的光源 Index 写入 Tile Light List

Fragment 阶段：
  根据当前像素所在 Tile，查 Tile Light List
  只循环这个列表里的光源（通常 10-20 个，而不是场景全部 100+ 个）
```

URP 的 Forward+ 用的是 **Clustered** 变体（不只在 XY 方向分 Tile，还在 Z 方向分 Cluster / Froxel），更适合深度差异大的场景。

### 性能特征

**优势**：
- 理论上支持场景级别的大量光源（数百到上千），每个像素只处理影响它的光源
- 兼容 MSAA（和 Forward 一样，每个像素只处理一次表面着色）
- 无 G-Buffer 带宽问题（不需要写入多张 RT，再读出来）
- 移动端比 Deferred 友好（虽然 Light Culling 有 Compute Shader 开销，但没有 G-Buffer 带宽问题）

**劣势**：
- 需要 Compute Shader 支持（部分低端移动 GPU 不支持）
- Light Culling 的 Compute 开销不可忽视（光源数量越多，越重）
- Cluster 结构占用常量缓冲区（Uniform Buffer）带宽

### URP 里 Forward+ 的使用

URP 2022.2 (Unity 2022 LTS) 正式引入 Forward+：

```
Universal Renderer 里选择 "Forward+"
Pipeline Asset 里：
  Additional Lights → Per-Pixel（Forward+ 下这个限制被放宽）
```

Forward+ 模式下，URP 会在帧开始时运行 Compute Shader 做 Light Culling，结果存到 `_AdditionalLightsBuffer` 和 Cluster 索引数组，Shader 里通过宏 `USE_FORWARD_PLUS` 切换循环逻辑。

---

## 三条路径的对比总结

| | Forward | Deferred | Forward+ |
|---|---|---|---|
| 光源上限 | 低（URP 默认 8 个） | 高（理论无上限） | 高（理论无上限） |
| MSAA 支持 | ✓ | ✗（需 FXAA/TAA）| ✓ |
| 半透明处理 | 天然支持 | 需要 Forward 补跑 | 天然支持 |
| 带宽压力 | 低 | 高（G-Buffer 读写）| 低到中（无 G-Buffer）|
| 移动端友好 | ✓✓ | ✗（G-Buffer 带宽）| ✓（无 G-Buffer，但需 Compute）|
| OverDraw 浪费 | 有（不透明区可优化）| 无（Early-Z 后只处理可见像素）| 有（同 Forward）|
| Shader 复杂度 | 中 | 低（Pass 分离）| 高（Light List 索引逻辑）|
| Compute Shader 依赖 | 无 | 无 | 有（Light Culling）|

### 实际选择建议

**移动端游戏**：Forward（光源数量可控时）或 Forward+（需要多光源但希望避免 G-Buffer 带宽）

**PC/主机 写实场景，光源密集**：Deferred（城市、室内多光源场景）或 Forward+（MSAA 需求 + 多光源）

**PC，光源少，MSAA 重要**：Forward

---

## 渲染路径对自定义 Pass 的影响

选择不同路径会影响你的自定义 Pass 里能读到什么：

### G-Buffer 访问（仅 Deferred）

```csharp
// Deferred 路径下，AfterRenderingGbuffer 之后可以访问 G-Buffer
// URP 通过 Shader 关键字暴露 G-Buffer
// 在 Shader 里用 _GBuffer0, _GBuffer1, _GBuffer2, _GBuffer3 采样
```

在 Forward / Forward+ 下，G-Buffer 不存在，无法访问。

### _CameraDepthTexture 的来源不同

- **Forward**：需要额外的 Depth Prepass（`DepthOnlyPass`），Asset 里要开启 Depth Texture
- **Deferred**：G-Buffer Pass 本身就会写深度，`_CameraDepthTexture` 在 Lighting Pass 之后自然存在
- **Forward+**：同 Forward，也需要 Depth Prepass

自定义 Pass 需要深度图时，Deferred 路径更保险（深度总是存在）；Forward/Forward+ 路径需要在 Pipeline Asset 里确认开启 Depth Texture。

### RenderPassEvent 的可用范围

Deferred 路径下有 `BeforeRenderingGbuffer` / `AfterRenderingGbuffer` 事件；Forward 路径里这些事件虽然存在但没有实际的 G-Buffer Pass，Pass 会被跳过（不报错，但无实际作用）。

### MSAA

- Forward / Forward+：可以在 Pipeline Asset 里开启 2x / 4x MSAA，自定义 Pass 的 RT 也需要设置相应的 `msaaSamples`
- Deferred：MSAA 不可用，开启也会被 URP 忽略

---

## 导读

- 上一篇：[URP 深度前置 02｜RenderTexture 与 RTHandle：临时 RT、RTHandle 体系]({{< relref "rendering/urp-pre-02-rthandle.md" >}})
- 下一篇：[URP 深度配置 01｜Pipeline Asset 解读：每个参数背后的渲染行为]({{< relref "rendering/urp-config-01-pipeline-asset.md" >}})

## 小结

三条路径都是对"多光源 + 多物体"这个组合爆炸问题的不同解法：

- **Forward**：把多光源合并进单 Pass 循环，简单可靠，但光源上限低
- **Deferred**：几何和光照解耦，光源多也不影响 Draw Call 数，代价是 G-Buffer 带宽和不支持 MSAA
- **Forward+**：用 Tile/Cluster 空间索引，让每个像素只处理相关光源，兼顾多光源和 MSAA，代价是 Compute Shader 依赖

写自定义 Pass 时，要清楚当前项目用的哪条路径，因为这直接影响你能访问哪些 RT（G-Buffer 只在 Deferred 有）、深度图的保障程度（Deferred 天然有），以及 MSAA 是否对自定义 RT 生效。

URP 深度系列的前置基础到这里结束。下一篇（URP配置-01）进入正题：从 Pipeline Asset 的每一个选项出发，讲清楚它们背后的技术含义和对渲染流程的实际影响。
