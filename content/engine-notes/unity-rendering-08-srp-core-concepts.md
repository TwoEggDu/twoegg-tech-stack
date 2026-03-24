+++
title = "SRP 核心概念：RenderPipelineAsset、RenderPipeline 与 ScriptableRenderContext"
slug = "unity-rendering-08-srp-core-concepts"
date = 2025-01-26
description = "SRP 的三个核心对象怎么组织在一起，CommandBuffer 在其中的角色，以及一帧的执行流程从哪里开始到哪里结束。"
[taxonomies]
tags = ["Unity", "SRP", "RenderPipeline", "CommandBuffer", "ScriptableRenderContext"]
series = ["Unity 渲染系统"]
[extra]
weight = 1100
+++

如果只用一句话概括这篇：SRP 把渲染管线拆成"配置数据（Asset）"、"执行逻辑（Pipeline）"、"GPU 命令接口（Context）"三层，开发者通过这三层拼出自己的渲染管线，而 CommandBuffer 是把命令攒起来批量提交的中间层。

---

## 从上一篇出发

上一篇（07：为什么需要 SRP）说明了 SRP 把"管线代码的控制权还给了开发者"。这篇具体讲这三个核心类是什么，它们如何协作，以及你在 URP 里做自定义 Pass 时，实际上是在和哪一层交互。

---

## 三层结构总览

```
Project Settings → Graphics → Scriptable Render Pipeline Settings
    ↓ 指定
RenderPipelineAsset（配置数据，存为 .asset 文件）
    ↓ 调用 CreatePipeline()
RenderPipeline（执行入口，每帧被 Unity 调用一次）
    ↓ 调用 Render(context, cameras)
ScriptableRenderContext（向 GPU 提交命令的接口）
    ↓ 通过 CommandBuffer 攒命令
    ↓ context.ExecuteCommandBuffer(cmd) 提交
    ↓ context.Submit() 真正发送到 GPU
```

---

## RenderPipelineAsset：管线的配置数据

`RenderPipelineAsset` 是一个 ScriptableObject，存在项目里作为 `.asset` 文件。它的职责是：

1. 保存管线的配置参数（阴影分辨率、HDR 是否开启、Post-processing 是否开启等）
2. 在 Unity 启动或参数改变时，通过 `CreatePipeline()` 方法实例化实际的 `RenderPipeline` 对象

```csharp
// 自定义管线的 Asset
[CreateAssetMenu(menuName = "Rendering/MyPipelineAsset")]
public class MyRenderPipelineAsset : RenderPipelineAsset
{
    public bool enableShadows = true;
    public ShadowResolution shadowResolution = ShadowResolution.Medium;

    // Unity 调用这个方法实例化管线执行对象
    protected override RenderPipeline CreatePipeline()
    {
        return new MyRenderPipeline(this);
    }
}
```

URP 里这个 Asset 就是 `UniversalRenderPipelineAsset`，在 Project Settings 或 Quality Settings 里指定。不同质量等级可以指定不同的 Asset，实现"低画质 / 高画质切换"。

---

## RenderPipeline：管线的执行入口

`RenderPipeline` 子类包含每帧的实际执行逻辑，核心是重写 `Render` 方法：

```csharp
public class MyRenderPipeline : RenderPipeline
{
    private MyRenderPipelineAsset asset;

    public MyRenderPipeline(MyRenderPipelineAsset asset)
    {
        this.asset = asset;
    }

    // Unity 每帧调用这个方法，传入所有需要渲染的 Camera
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // 对每个 Camera 分别渲染
        foreach (var camera in cameras)
        {
            RenderCamera(context, camera);
        }
        // 所有 Camera 完成后，提交命令到 GPU
        context.Submit();
    }

    private void RenderCamera(ScriptableRenderContext context, Camera camera)
    {
        // 1. 设置 Camera 的 VP 矩阵等全局参数
        context.SetupCameraProperties(camera);

        // 2. Culling
        if (!camera.TryGetCullingParameters(out var cullingParameters)) return;
        var cullingResults = context.Cull(ref cullingParameters);

        // 3. 提交 Draw Call
        var drawSettings = new DrawingSettings(...);
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
    }
}
```

关键点：
- `Render` 方法接收所有 Camera，开发者决定每个 Camera 的处理顺序和方式
- `context.Submit()` 在所有 Camera 处理完之后调用，一次性把所有命令提交给 GPU 驱动
- 在 `Submit()` 之前，所有命令都在 CPU 侧排队，没有真正发给 GPU

---

## ScriptableRenderContext：GPU 命令的提交接口

`ScriptableRenderContext` 是 SRP 里向 GPU 提交命令的主要接口。它有几类方法：

**场景渲染命令：**

```csharp
// 提交场景物体的 Draw Call（Renderer 列表）
context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);

// 提交天空盒 Draw Call
context.DrawSkybox(camera);

// 提交 Shadow Map 渲染
context.DrawShadows(ref shadowDrawSettings);
```

**CommandBuffer 执行：**

```csharp
// 把一个 CommandBuffer 里的命令追加到 context 的命令队列
context.ExecuteCommandBuffer(cmd);
cmd.Clear(); // 执行之后清空，CommandBuffer 可以复用
```

**提交：**

```csharp
// 把 context 积累的所有命令发送到 GPU 驱动
context.Submit();
```

`ScriptableRenderContext` 本身并不立即执行命令——它把命令攒起来，直到 `Submit()` 时才一次性发出。这是 SRP 性能设计的一部分：批量提交减少 API 调用开销。

---

## CommandBuffer：灵活插入任意 GPU 命令

`CommandBuffer` 是一个命令列表，可以在里面记录任意 GPU 操作：

```csharp
var cmd = new CommandBuffer();
cmd.name = "MyCustomPass";

// 设置 Render Target
cmd.SetRenderTarget(myColorRT, myDepthRT);

// 清除 RT
cmd.ClearRenderTarget(true, true, Color.black);

// 全屏 Blit（把当前帧画面经过 Material 处理后写入目标 RT）
cmd.Blit(sourceRT, destinationRT, postProcessMaterial);

// 设置全局 Shader 参数
cmd.SetGlobalTexture("_BloomTexture", bloomRT);

// 把这些命令提交给 context
context.ExecuteCommandBuffer(cmd);
cmd.Clear();
```

`CommandBuffer` 的作用：

1. **灵活性**：`DrawRenderers` 只能渲染场景 Renderer，但 CommandBuffer 可以做 Blit、设置 RT、设置全局参数等 `context` 本身不直接提供的操作
2. **复用**：同一个 `CommandBuffer` 可以在多帧之间复用（`cmd.Clear()` 之后重新填充）
3. **命名**：`cmd.name` 会在 Frame Debugger 里作为 Pass 名字显示，便于调试

在 URP 里，`ScriptableRenderPass` 的 `Execute` 方法接收一个 `CommandBuffer` 参数，开发者在这里填入当前 Pass 要执行的命令：

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

    // 填入当前 Pass 的命令
    cmd.Blit(m_Source, m_Dest, m_Material);

    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```

---

## 一帧的完整执行流程

把三层结构连起来，一帧的执行顺序如下：

```
Unity Engine 每帧调用：
  RenderPipeline.Render(context, cameras)

  foreach camera in cameras:
    context.SetupCameraProperties(camera)
      → 设置 View/Projection 矩阵等全局 Shader 参数

    cullingResults = context.Cull(cullingParameters)
      → CPU 侧计算可见 Renderer 列表

    [管线里每个 RenderPass 依次执行]
      Pass A: cmd.SetRenderTarget(depthRT) + DrawRenderers(opaque)
        context.ExecuteCommandBuffer(cmdA)
        context.DrawRenderers(cullingResults, ...)

      Pass B: cmd.Blit(colorRT, shadowRT, shadowMaterial)
        context.ExecuteCommandBuffer(cmdB)

      Pass C: context.DrawSkybox(camera)

      ... 更多 Pass ...

  context.Submit()
    → 所有命令一次性发往 GPU 驱动
    → GPU 开始并行执行
```

注意 `context.Submit()` 的位置：它在所有 Camera 处理完之后才调用，CPU 在此之前只是在组织命令列表，不等待 GPU。

---

## 在 Frame Debugger 里的对应关系

Frame Debugger 里的每个层级和 SRP 代码的对应关系：

```
Frame Debugger 显示的层级                   对应 SRP 代码
─────────────────────────────────────────────────────────
▼ Camera 0 "Main Camera"              ← RenderCamera() 的一次执行
  ▼ "MyDepthPass"                     ← CommandBuffer.name = "MyDepthPass"
      Draw Mesh "Cube"                ← context.DrawRenderers(...)
      Draw Mesh "Sphere"              ← context.DrawRenderers(...)
  ▼ "MyShadowPass"                    ← 另一个 CommandBuffer
      Blit                            ← cmd.Blit(...)
  ▼ "MyOpaquePass"
      Draw Mesh "Ground"
      ...
```

每个 `CommandBuffer.name` 形成一个层级节点，`ExecuteCommandBuffer` 的调用顺序决定层级的顺序。这就是为什么给 CommandBuffer 取有意义的名字在调试中非常重要。

---

## SRP 的核心 API 演进（简要）

SRP 的底层 API 从 2019 到 2023 有几次重要变化，在阅读网上的 URP 自定义 Pass 教程时需要注意版本：

| Unity 版本 | 主要 RT API | 特点 |
|---|---|---|
| 2019–2020 | `RenderTargetIdentifier` | 早期 API，直接用 ID 引用 RT |
| 2021–2022 | `RTHandle` | 自动管理 RT 生命周期和尺寸缩放 |
| 2022+ | `RenderGraph` | 声明式依赖图，自动管理资源，减少手动 SetRenderTarget |

`RenderGraph` 是 Unity 6（2023+）推荐的写法。旧版本的 `CommandBuffer + SetRenderTarget` 仍然工作，但 Unity 官方的新文档和示例已经以 `RenderGraph` 为主。

---

## 小结

| 概念 | 职责 | 开发者主要在哪里用 |
|---|---|---|
| `RenderPipelineAsset` | 管线配置数据，启动时实例化管线 | 调参数、指定 Renderer |
| `RenderPipeline` | 每帧执行入口，控制 Camera 顺序和整体流程 | 从零写管线时 |
| `ScriptableRenderContext` | 向 GPU 提交命令的接口，最终 Submit | 写自定义 Pass |
| `CommandBuffer` | 命令列表，灵活记录任意 GPU 操作 | 写自定义 Pass |

下一篇将把这套机制落地到 URP 的具体实现：`UniversalRenderPipelineAsset → Renderer → RendererFeature → RenderPass` 的层级结构，以及 URP 的默认 Pass 顺序是怎么组织的。
