---
title: "URP 架构详解：从 Asset 到 RenderPass 的层级结构"
slug: "unity-rendering-09-urp-architecture"
date: "2025-01-26"
description: "URP 如何把 SRP 的三层抽象落地为具体的四层层级（Asset → Renderer → RendererFeature → RenderPass），以及 URP 默认 Pass 顺序的每一步在做什么。"
tags:
  - "Unity"
  - "URP"
  - "渲染管线"
  - "RendererFeature"
  - "RenderPass"
series: "Unity 渲染系统"
weight: 1200
---
如果只用一句话概括这篇：URP 是 SRP 的一个官方实现，它在 SRP 的三层抽象之上加了 Renderer 和 RendererFeature 两个层，让开发者可以在不碰管线主干代码的情况下，以"插件"方式插入自定义渲染逻辑。

---

## 从上一篇出发

上一篇（08：SRP 核心概念）描述了 SRP 的三层结构：`RenderPipelineAsset` 保存配置、`RenderPipeline` 执行每帧逻辑、`ScriptableRenderContext` 提交 GPU 命令。URP 是在这套机制之上构建的，但它在中间加了两层，使得开发者可以更方便地扩展渲染流程。

---

## URP 的四层层级

```
UniversalRenderPipelineAsset（.asset 文件，挂在 Project Settings）
  ↓ 持有一个或多个
UniversalRenderer / Renderer2D（Renderer，决定渲染路径）
  ↓ 持有列表
ScriptableRendererFeature（RendererFeature，可插拔的渲染扩展）
  ↓ 创建并注册
ScriptableRenderPass（RenderPass，实际执行 GPU 命令的最小单元）
```

每一层的职责：

| 层 | 类型 | 职责 |
|---|---|---|
| `UniversalRenderPipelineAsset` | ScriptableObject (.asset) | 全局配置参数（阴影、HDR、MSAA、质量等级） |
| `UniversalRenderer` | ScriptableObject (.asset) | 选择渲染路径（Forward / Deferred / Forward+）；持有 RendererFeature 列表 |
| `ScriptableRendererFeature` | 抽象类 | 在 Renderer 初始化时创建 RenderPass，并注册到管线 |
| `ScriptableRenderPass` | 抽象类 | 实际执行：设置 RT、提交 Draw Call、执行 Blit |

---

## UniversalRenderPipelineAsset：全局参数

`UniversalRenderPipelineAsset` 存储的是整条管线的配置参数，这些参数在 Inspector 里可以直接编辑：

```
Rendering
  ├─ Depth Texture    ← 是否在 Opaque 之后生成 _CameraDepthTexture（供后续 Pass 采样）
  ├─ Opaque Texture   ← 是否生成 _CameraOpaqueTexture（Transparent 阶段可采样不透明结果）
  └─ Renderer List    ← 可以指定多个 Renderer，按平台或质量等级切换

Shadows
  ├─ Max Distance     ← 阴影可见距离
  ├─ Cascade Count    ← Shadow Map 级联数（1–4）
  └─ Shadow Resolution← 每个 Cascade 的 Shadow Map 分辨率

Post-processing
  └─ Grading Mode     ← HDR / LDR 色调映射模式
```

不同质量等级可以指定不同的 Asset，实现"低/中/高画质切换"：

```
Quality Settings
  Low  → URPAsset_Low.asset  （关闭阴影、关闭 Opaque Texture）
  High → URPAsset_High.asset （开启 4 级联阴影、开启 Depth Texture）
```

---

## UniversalRenderer：渲染路径与 RendererFeature 列表

`UniversalRenderer` 是实际执行渲染的 Renderer，它决定：

1. **渲染路径**：Forward（默认）/ Deferred / Forward+
2. **RendererFeature 列表**：按顺序列出所有插入的自定义扩展

在 Inspector 里，`UniversalRenderer` 的配置大致如下：

```
Renderer Features
  ┌─────────────────────────────────────────┐
  │ + Add Renderer Feature                  │
  ├─────────────────────────────────────────┤
  │ ✓ Screen Space Ambient Occlusion (SSAO) │
  │ ✓ Full Screen Pass Render Feature       │
  │ ✓ MyOutlineFeature （自定义）            │
  └─────────────────────────────────────────┘
```

Unity 会在每帧按顺序初始化这个列表里的每个 RendererFeature，让它们把自己的 RenderPass 注册进来。

一个场景里可以有多个 Camera，每个 Camera 可以指定使用哪个 Renderer（通过 Camera 的 `Renderer` 字段）：

```
主摄像机    → UniversalRenderer（Forward，带 Post-processing）
UI 摄像机  → UniversalRenderer（Forward，不带 Post-processing）
小地图摄像机 → UniversalRenderer_Minimap（简化版本，不带 Shadow）
```

---

## ScriptableRendererFeature：可插拔的渲染扩展

`ScriptableRendererFeature` 是 URP 里扩展渲染管线的主要入口。它的两个方法：

```csharp
public class MyOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material outlineMaterial;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public Settings settings = new Settings();
    private MyOutlinePass m_OutlinePass;

    // 管线初始化时调用（Renderer 创建时 / 参数改变时）
    public override void Create()
    {
        m_OutlinePass = new MyOutlinePass(settings.outlineMaterial, settings.passEvent);
    }

    // 每帧每个 Camera 渲染前调用，在这里把 Pass 注册进来
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_OutlinePass);
    }
}
```

`AddRenderPasses` 每帧都会被调用，可以在这里根据条件决定是否注册 Pass（比如只在主摄像机注册，或只在特效开启时注册）。

---

## ScriptableRenderPass：实际执行的最小单元

`ScriptableRenderPass` 是真正执行 GPU 命令的地方。每个 Pass 有两个关键属性：

**`renderPassEvent`**：声明这个 Pass 在哪个阶段执行

```csharp
public enum RenderPassEvent
{
    BeforeRendering,
    BeforeRenderingShadows,
    AfterRenderingShadows,
    BeforeRenderingPrePasses,        // ← Depth Prepass 之前
    AfterRenderingPrePasses,         // ← Depth Prepass 之后
    BeforeRenderingGbuffer,
    AfterRenderingGbuffer,
    BeforeRenderingOpaques,          // ← Opaque 之前
    AfterRenderingOpaques,           // ← Opaque 之后（常用插入点）
    BeforeRenderingSkybox,
    AfterRenderingSkybox,
    BeforeRenderingTransparents,
    AfterRenderingTransparents,      // ← Transparent 之后（另一个常用插入点）
    BeforeRenderingPostProcessing,
    AfterRenderingPostProcessing,    // ← 后处理之后
}
```

**`Execute`**：Pass 执行时的逻辑

```csharp
public class MyOutlinePass : ScriptableRenderPass
{
    private Material m_Material;

    public MyOutlinePass(Material material, RenderPassEvent passEvent)
    {
        m_Material = material;
        renderPassEvent = passEvent;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("MyOutline");

        // 从渲染数据里取当前 Camera RT
        var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;

        cmd.Blit(cameraColorTarget, cameraColorTarget, m_Material);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
```

URP 收集所有注册的 Pass，按照 `renderPassEvent` 的值排序，在对应阶段依次调用每个 Pass 的 `Execute`。

---

## URP 的默认 Pass 顺序

URP 内置了一套默认的 Pass 序列。以 Forward 路径为例，一帧的 Pass 顺序如下：

```
RenderPassEvent 值   Pass 名称                    做什么
─────────────────────────────────────────────────────────────────────
100  BeforeRendering
150  BeforeRenderingShadows
200  ShadowCaster Pass           → 从光源视角渲染场景，生成 Shadow Map
                                    （多个 Cascade 各一次，Spot/Point 光各一次）
250  AfterRenderingShadows
300  DepthOnlyPass               → 从主 Camera 渲染场景深度到 _CameraDepthTexture
                                    （Opaque 物体，ZWrite On）
350  AfterRenderingPrePasses
400  DrawOpaqueObjects           → 正向渲染所有不透明物体（使用 Shadow Map + Lightmap）
450  AfterRenderingOpaques       ← ★ 最常用的自定义 Pass 插入点
500  DrawSkybox                  → 渲染天空盒（在 Opaque 之后，利用已有深度避免 OverDraw）
550  AfterRenderingSkybox
600  CopyDepthPass               → 复制深度到 _CameraDepthTexture（如果之前未生成）
650  CopyColorPass               → 复制颜色到 _CameraOpaqueTexture（如果 Asset 里开启）
700  DrawTransparentObjects      → 渲染半透明物体（从后到前，ZWrite Off）
750  AfterRenderingTransparents  ← ★ 另一个常用插入点
800  BeforeRenderingPostProcessing
900  UberPostProcess             → 所有后处理效果合并到一次 Pass（Bloom+Tonemapping+...)
950  AfterRenderingPostProcessing
```

几个细节值得注意：

**Shadow Map 在 Opaque 之前生成**：Opaque 渲染时采样 Shadow Map，所以 Shadow Map 必须先就绪。

**Depth Prepass 的条件**：只有当 Asset 里开启了 Depth Texture，或者某个 RendererFeature 需要深度图时，`DepthOnlyPass` 才会出现。否则深度直接在 Opaque 渲染时写入 Depth Buffer，不额外生成贴图。

**天空盒在 Opaque 之后**：天空盒只填充没有任何不透明物体覆盖的像素（利用 Depth Test），放在 Opaque 后渲染可以避免 OverDraw。

**UberPost 是合并 Pass**：URP 把 Bloom、Tonemapping、Color Grading、SSAO 等后处理效果合并到一个 Pass 里执行（通过 Shader 宏控制开关），减少全屏 Blit 次数。

---

## 在 Frame Debugger 里读 URP Pass 顺序

打开 Frame Debugger（Window → Analysis → Frame Debugger），你会看到类似这样的层级：

```
▼ Camera 0 "Main Camera"
  ▼ "MainLightShadowCasterPass"      ← Shadow Map
      Draw Mesh (shadowcaster)
      Draw Mesh (shadowcaster)
      ...
  ▼ "DepthOnlyPass"                  ← Depth Prepass
      Draw Mesh (depthOnly)
      ...
  ▼ "DrawOpaqueObjects"              ← Opaque Forward
      SRP Batch (30)
      Draw Mesh "Cube"
      ...
  ▼ "DrawSkyboxPass"                 ← 天空盒
      Draw Mesh (skybox)
  ▼ "MyOutlineFeature"               ← 自定义 RendererFeature 插入的 Pass
      Blit
  ▼ "DrawTransparentObjects"         ← 半透明
      Draw Mesh "Particle"
      ...
  ▼ "UberPostProcessPass"            ← 后处理
      Blit
```

每个 `CommandBuffer.name`（包括 URP 内置 Pass 的名字和自定义 Pass 的名字）都形成一个层级节点。自定义 Pass 会按照 `renderPassEvent` 的值插入到对应位置。

---

## 多 Camera 和 Camera Stack

URP 里如果需要多个 Camera 叠加（比如主场景 + UI + 小地图），需要使用 **Camera Stack**：

```
Base Camera（主场景）
  └─ Overlay Camera（UI，Clear Flags = Don't Clear）
  └─ Overlay Camera（调试线框）
```

Base Camera 做完整渲染；Overlay Camera 复用 Base Camera 的深度，只在上面添加内容。这比 Built-in 里多个独立 Camera 叠加效率更高，因为不需要重复做 Culling 和 Depth Pass。

---

## 小结

URP 在 SRP 的三层之上加了两层：

```
SRP 层            URP 落地
─────────────────────────────────────────────────
RenderPipelineAsset → UniversalRenderPipelineAsset（全局配置参数）
                    → UniversalRenderer（渲染路径 + RendererFeature 列表）
RenderPipeline      → UniversalRenderPipeline（内部实现，不需要开发者改）
ScriptableRenderContext → 每个 ScriptableRenderPass 的 Execute 里调用
```

开发者日常接触最多的是最底两层：`ScriptableRendererFeature`（创建和注册 Pass）和 `ScriptableRenderPass`（执行 GPU 命令）。

下一篇（10：在 URP 里扩展渲染流程）将把这套机制转化为可操作的实践：完整的 RendererFeature + RenderPass 写法、RTHandle API 的用法、RenderGraph 的基本概念，以及几个常见扩展场景（描边、屏幕特效、自定义 Pass 注入）的实现思路。
