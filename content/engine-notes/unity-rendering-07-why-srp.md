+++
title = "为什么需要可编程渲染管线"
slug = "unity-rendering-07-why-srp"
date = 2025-01-26
description = "Built-in 渲染管线的架构性限制在实际项目中造成了哪些具体问题，SRP 用什么方式解决了这些问题，以及引入 SRP 的代价是什么。"
[taxonomies]
tags = ["Unity", "SRP", "渲染管线", "URP", "HDRP"]
series = ["Unity 渲染系统"]
[extra]
weight = 1000
+++

如果只用一句话概括这篇：Built-in 渲染管线的核心问题是"管线结构对开发者不透明，扩展点太少"，SRP 把管线执行权还给了开发者，但代价是开发者需要自己承担更多复杂性。

---

## 从上一篇出发

上一篇（06：Built-in 渲染管线）列出了 Built-in 的四个架构性限制：Shader 模型与管线绑定、Pass 数量由管线决定、多平台差异硬编码、无法控制 RT 布局。

这篇把这些限制具体化：在真实项目里，它们分别会遇到什么问题，为什么不能靠"多加 CommandBuffer"来解决，SRP 做了哪些根本性的架构改变。

---

## 限制一：换光照模型要绕开整个 Surface Shader 系统

**场景**：做一款手游，需要实现描边（Outline）+ 色阶化漫反射（Cel Shading）的卡通渲染风格。

在 Built-in 里，Surface Shader 假设光照模型是物理的（Standard PBR 或 Lambert）。开发者如果要换光照模型：

1. 必须放弃 `#pragma surface surf` 语法，手写完整的 Vertex/Fragment Shader
2. 手写 Shader 意味着要自己处理 Shadow Map 采样、Lightmap UV 采样、Light Probe SH 解码——这些在 Surface Shader 里是免费得到的
3. 如果还要支持多盏实时光（Additional Pass），需要手写 Additional Pass，并在两个 Pass 之间保持一致的光照结果

结果：一个"改改光照风格"的需求，变成了重写 Shader 体系的工程任务。

**SRP 的解法**：管线里的 Shader 模型是由 `RenderPipeline` 代码控制的，你想用什么 Shader、怎么处理多盏光，完全由管线开发者决定。URP 的 Lit Shader 是 URP 这条管线的默认实现，你可以替换它，也可以写新的不继承它的 Shader——管线不假设你用什么光照模型。

---

## 限制二：多光源导致 Draw Call 爆炸，无法针对平台优化

**场景**：移动端游戏，场景里有 15 盏点光源（商店、街灯等）。

在 Built-in Forward 路径下：

```
每个受多盏光影响的物体：
  1 次 Base Pass（处理最重要的 1 盏光）
  + N 次 Additional Pass（处理每盏额外的光，最多 4 盏，超出截断）

15 盏灯 × 50 个受影响的物体 = 最差情况接近 750 次 Additional Pass
```

切换到 Built-in Deferred 路径：

```
Geometry Pass：50 次 Draw Call（正常）
Lighting Pass：15 次（每盏灯一次 Lighting Pass）

看起来解决了——但移动端 TBDR GPU 上
G-Buffer 4 张 RT 的带宽压力是桌面端的 3 倍
Deferred 在移动端实际更慢
```

开发者陷入两难：Forward 在移动端灯多了性能差，Deferred 在移动端带宽成本高。他们需要的是 Tile-based Deferred Lighting 或 Clustered Forward——但 Built-in 里这两种路径根本不存在。

**SRP 的解法**：URP 在 Unity 2022+ 版本引入了 Forward+，使用 Tile/Cluster 结构把光源分配到屏幕空间格子里，Fragment Shader 只处理当前像素所在格子里的光源，多光源 Draw Call 爆炸问题从架构层面被解决。这种光照路径只有在"管线代码由开发者控制"的情况下才能被实现。

---

## 限制三：CommandBuffer 的扩展能力有上限

**场景**：需要在 Opaque 渲染之后、Transparent 渲染之前，插入一个自定义的屏幕空间效果（比如扫描线描边）。

在 Built-in 里，`CommandBuffer` 可以挂在特定的 `CameraEvent` 上：

```csharp
var cb = new CommandBuffer();
cb.name = "ScanlineOutline";
// 从 Camera RT 取当前帧画面，做处理，写回
cb.Blit(BuiltinRenderTextureType.CurrentActive, tempRT, outlineMaterial);
cb.Blit(tempRT, BuiltinRenderTextureType.CurrentActive);
camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cb);
```

这个方案能工作，但有几个隐患：

1. `BuiltinRenderTextureType.CurrentActive` 指向的 RT 内容依赖当前 Camera 状态，在 Deferred 路径下这个引用和 Forward 路径下不同，代码需要分支处理
2. 当场景里有多个 Camera 时，`camera.AddCommandBuffer` 会在每个 Camera 上都执行一遍，需要手动管理哪个 Camera 需要这个效果
3. 不同平台上 `CameraEvent` 的实际触发时机有细微差异（尤其是 XR 和 VR 场景），文档不完整

更根本的问题：Built-in 的 `CommandBuffer` 是在固定管线上"打补丁"，插入点和执行顺序仍然是不透明的。开发者无法确切知道自己的 Pass 在哪里被执行，也无法改变管线中其他 Pass 的顺序。

**SRP 的解法**：SRP 里的每个 `ScriptableRenderPass` 是管线的一等公民，你明确声明它插入的阶段（`RenderPassEvent`），URP 按照所有 Pass 的声明统一排序和执行。没有隐式依赖，没有平台分支，每个 Pass 的职责和执行时机完全透明。

---

## 限制四：RT 布局固定，无法改变 G-Buffer 内容

**场景**：需要在延迟渲染里存储自定义的屏幕空间数据（比如角色 ID，用于后处理中对特定角色做描边）。

Built-in Deferred 的 G-Buffer 格式是硬编码的四张 RT，你无法增加第五张 RT，也无法改变现有 RT 里存储的内容（比如把 RT0 的 Occlusion 换成角色 ID）。

唯一的绕过方式是在 Geometry Pass 结束后额外渲染一遍场景，把角色 ID 写到另一张 RT——即双倍 Draw Call，双倍顶点处理开销。

**SRP 的解法**：在 SRP（HDRP）里，G-Buffer 的 RT 数量和格式是可以修改的，如果项目需要在 G-Buffer 里存自定义数据，直接改管线代码的 G-Buffer 分配就行。

---

## SRP 的架构变化：把管线代码还给开发者

SRP（Scriptable Render Pipeline）的核心设计就是一句话：

> **Unity 不再内置一个渲染管线，而是提供一套 API，让开发者自己实现渲染管线。**

具体来说，SRP 提供了：

```
RenderPipelineAsset（管线配置数据）
  → 实例化 RenderPipeline（管线执行入口）
      → 每帧调用 Render(ScriptableRenderContext context, Camera[] cameras)
          → 开发者用 context 向 GPU 提交所有命令
```

在这个模型下：

- **Culling 策略**：开发者调用 `context.Cull(cullingParameters)`，可以控制剔除参数
- **Pass 顺序**：开发者决定先提交哪些 Draw Call、何时切换 RT、何时执行全屏 Pass
- **Shader 选择**：Draw Call 用什么 Shader、用哪个 Pass，由开发者在 `DrawingSettings` 里指定
- **RT 管理**：哪些 RT 存在、什么时候分配和释放，都由管线代码控制

Unity 在 SRP 之上提供了两个官方实现：

| | URP | HDRP |
|---|---|---|
| 定位 | 性能优先，移动端到主机都适用 | 画质优先，PC/主机高端项目 |
| 光照路径 | Forward / Forward+ | Deferred-first |
| Shader 模型 | PBR（Lit）可扩展 | 复杂 Lit（厚材料模型） |
| 可扩展性 | RendererFeature + RenderPass | 更复杂，需修改管线代码 |

开发者也可以完全不用 URP/HDRP，从零写自己的 `RenderPipeline` 子类——这在游戏卡通渲染、工具软件等对渲染有特殊需求的项目中实际发生过。

---

## SRP 的代价

SRP 并不是免费的架构升级，它带来了几个真实代价：

**1. 学习成本更高**

Built-in 里"给 MeshRenderer 挂一个 Material 就能渲染"的直觉不再有效。开发者需要理解 `ScriptableRenderContext`、`CommandBuffer`、`RTHandle`、`RenderGraph`，才能做哪怕简单的自定义效果。

**2. 第三方资产兼容性**

大量 Built-in Shader（Legacy、Standard Shader 的精确版本）在 URP 里不工作（渲染成粉色）。从 Built-in 迁移到 URP 的主要工作量就是升级或替换这些 Shader。

**3. API 持续变化**

SRP 的底层 API 在 Unity 2019–2023 之间经历了多次 Breaking Change：`CommandBuffer` → `RTHandle` API → `RenderGraph` API，每次变化都需要升级自定义 Pass 代码。

**4. 调试复杂性**

Built-in 里你只需要 Frame Debugger 就能看清楚一帧发生了什么。SRP 里管线代码本身可能是自定义的，Frame Debugger 展示的 Pass 顺序来自管线代码，需要同时理解工具输出和管线源码才能有效诊断问题。

---

## 小结

| | Built-in | SRP (URP/HDRP) |
|---|---|---|
| 光照模型 | 固定（Standard PBR / Lambert） | 可替换 |
| Pass 控制 | 管线决定，开发者插入 | 开发者完全控制 |
| 多光源策略 | Forward 多 Pass / Deferred 固定 | 可实现 Forward+ / Cluster |
| RT 布局 | 固定 G-Buffer | 完全可控 |
| 入门难度 | 低（开箱即用） | 高（需理解管线架构） |
| 扩展上限 | CommandBuffer 有限插入点 | 无上限（自写管线） |

理解了这张对比表，就理解了 SRP 的设计动机。下一篇将深入 SRP 的核心概念：`RenderPipelineAsset`、`RenderPipeline`、`ScriptableRenderContext` 三者的关系，以及 `CommandBuffer` 在 SRP 里扮演的角色。
