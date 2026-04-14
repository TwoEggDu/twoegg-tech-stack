---
title: "URP 深度扩展 09｜RenderGraph 迁移实战：三个 Feature 的完整改写过程"
slug: "urp-ext-09-rendergraph-migration-cases"
date: "2026-04-14"
description: "ext-02 讲了 RenderGraph 的 API 概念。这篇拿灰度后处理、描边、Compute+Raster 混合三个典型 Feature，逐行走完从 Execute() 到 RecordRenderGraph() 的完整迁移过程，附迁移检查清单。"
tags:
  - "Unity"
  - "URP"
  - "RenderGraph"
  - "Unity 6"
  - "Renderer Feature"
  - "迁移"
series: "URP 深度"
weight: 1646
---
> **读这篇之前**：本篇是 ext-02 的实战伴侣。你需要熟悉 Execute API 旧写法和 RenderGraph 声明式模型。如果不熟悉，建议先看：
> - [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})
> - [URP 深度扩展 02｜RenderGraph 实战：Unity 6 的新写法]({{< relref "rendering/urp-ext-02-rendergraph.md" >}})

ext-02 讲了 RenderGraph 的核心概念：TextureHandle、UseTexture、SetRenderAttachment、PassData。概念懂了，真正迁移时还是会卡在细节上。这篇用三个真实场景，逐行走完从 `Execute()` 到 `RecordRenderGraph()` 的完整改写过程。

---

## 案例一：灰度后处理（单 Pass 双 Blit）

灰度后处理是最常见的入门级 Renderer Feature：读相机颜色 → 材质处理 → 写回。涉及两次 Blit，需要一张临时 RT 做中转。

### 旧写法（Execute API）

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get("Grayscale");
    Blitter.BlitCameraTexture(cmd, _cameraColor, _tempRT, _material, 0);
    Blitter.BlitCameraTexture(cmd, _tempRT, _cameraColor);
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```

旧写法的问题：`_tempRT` 需要在 `OnCameraSetup` 里手动 `RenderingUtils.ReAllocateHandleIfNeeded` 创建，在 `OnCameraCleanup` 里释放。`CommandBufferPool` 的 Get/Release 也是手动管理。

### 新写法（RecordRenderGraph）

先定义 PassData：

```csharp
private class PassData
{
    public TextureHandle Source;
    public Material Material;
}
```

然后是完整的 RecordRenderGraph：

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();
    var desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
    desc.name = "_GrayscaleTempRT";
    TextureHandle tempRT = renderGraph.CreateTexture(desc);

    // Pass 1: camera → temp（灰度处理）
    using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale_Effect", out var passData))
    {
        passData.Source = resourceData.activeColorTexture;
        passData.Material = _material;
        builder.UseTexture(passData.Source, AccessFlags.Read);
        builder.SetRenderAttachment(tempRT, 0);
        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
        {
            Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
        });
    }

    // Pass 2: temp → camera（写回）
    using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale_Blit", out var passData))
    {
        passData.Source = tempRT;
        builder.UseTexture(passData.Source, AccessFlags.Read);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
        {
            Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), 0);
        });
    }
}
```

### 迁移关键点

**RTHandle 手动生命周期 → RenderGraph 自动管理**：旧写法需要 `ReAllocateHandleIfNeeded` + `OnCameraCleanup` 释放。新写法只需 `renderGraph.CreateTexture(desc)`，RenderGraph 自动决定何时分配、何时释放、是否复用。

**CommandBufferPool 消失**：不再需要 `CommandBufferPool.Get` / `Release`。RenderGraph 在 `SetRenderFunc` 的回调里通过 `ctx.cmd` 提供 CommandBuffer，生命周期由框架管理。

**资源访问必须声明**：每个 Pass 读什么、写什么，必须通过 `UseTexture` 和 `SetRenderAttachment` 显式声明。漏声明不会报编译错误，但 RenderGraph 会认为该资源没有被使用，可能裁剪整个 Pass。

---

## 案例二：描边（多 Pass + Depth 读取）

描边效果是多 Pass 协作的典型场景：第一个 Pass 把需要描边的物体渲染到一张 mask 纹理，第二个 Pass 读取 mask 和深度信息做边缘检测。

### 旧写法思路

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get("Outline");

    // 渲染 mask
    cmd.SetRenderTarget(_maskRT, _depthRT);
    cmd.ClearRenderTarget(true, true, Color.clear);
    // ... DrawRenderers 画描边对象 ...

    // 边缘检测：读 mask + depth，写回相机
    cmd.SetGlobalTexture("_MaskTex", _maskRT);
    cmd.SetGlobalTexture("_CameraDepthTexture", _depthRT);
    Blitter.BlitCameraTexture(cmd, _cameraColor, _cameraColor, _outlineMaterial, 0);

    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```

旧写法的 `_maskRT` 和 `_depthRT` 都需要手动创建和释放，且 `SetRenderTarget` 同时设置颜色和深度附件。

### 新写法（RecordRenderGraph）

```csharp
private class MaskPassData
{
    public RendererListHandle RendererList;
}

private class OutlinePassData
{
    public TextureHandle MaskTexture;
    public TextureHandle DepthTexture;
    public TextureHandle CameraColor;
    public Material OutlineMaterial;
}

public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();
    var renderingData = frameData.Get<UniversalRenderingData>();
    var cameraData = frameData.Get<UniversalCameraData>();

    // 创建 mask RT
    var maskDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
    maskDesc.name = "_OutlineMask";
    maskDesc.colorFormat = GraphicsFormat.R8_UNorm;
    TextureHandle maskRT = renderGraph.CreateTexture(maskDesc);

    // 创建深度 RT
    var depthDesc = renderGraph.GetTextureDesc(resourceData.activeDepthTexture);
    depthDesc.name = "_OutlineDepth";
    TextureHandle depthRT = renderGraph.CreateTexture(depthDesc);

    // Pass 1: 渲染 mask
    using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Outline_Mask", out var passData))
    {
        builder.SetRenderAttachment(maskRT, 0);
        builder.SetRenderAttachmentDepth(depthRT);

        // 构建 RendererList（替代旧的 DrawRenderers）
        var drawSettings = RenderingUtils.CreateDrawingSettings(
            new ShaderTagId("OutlineMask"), renderingData, cameraData, SortingCriteria.CommonOpaque);
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque, _layerMask);
        passData.RendererList = renderGraph.CreateRendererList(
            new RendererListParams(renderingData.cullResults, drawSettings, filterSettings));
        builder.UseRendererList(passData.RendererList);

        builder.SetRenderFunc((MaskPassData data, RasterGraphContext ctx) =>
        {
            ctx.cmd.ClearRenderTarget(true, true, Color.clear);
            ctx.cmd.DrawRendererList(data.RendererList);
        });
    }

    // Pass 2: 边缘检测
    using (var builder = renderGraph.AddRasterRenderPass<OutlinePassData>("Outline_Edge", out var passData))
    {
        passData.MaskTexture = maskRT;
        passData.DepthTexture = depthRT;
        passData.CameraColor = resourceData.activeColorTexture;
        passData.OutlineMaterial = _outlineMaterial;

        builder.UseTexture(maskRT, AccessFlags.Read);
        builder.UseTexture(depthRT, AccessFlags.Read);
        builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

        builder.SetRenderFunc((OutlinePassData data, RasterGraphContext ctx) =>
        {
            data.OutlineMaterial.SetTexture("_MaskTex", data.MaskTexture);
            data.OutlineMaterial.SetTexture("_DepthTex", data.DepthTexture);
            Blitter.BlitTexture(ctx.cmd, data.CameraColor, new Vector4(1, 1, 0, 0), data.OutlineMaterial, 0);
        });
    }
}
```

### 迁移关键点

**TextureHandle 跨 Pass 传递**：`maskRT` 和 `depthRT` 在第一个 Pass 声明为写入目标，在第二个 Pass 声明为读取。RenderGraph 根据这些声明自动建立依赖关系，保证 Pass 1 在 Pass 2 之前执行。传递方式很直接，就是用同一个 TextureHandle 变量。

**深度附件声明**：旧写法 `cmd.SetRenderTarget(color, depth)` 对应新写法 `builder.SetRenderAttachment(color, 0)` + `builder.SetRenderAttachmentDepth(depth)`。深度附件用独立方法设置，不占颜色附件的 index。

**RendererList 替代 DrawRenderers**：RenderGraph 要求用 `CreateRendererList` + `UseRendererList` 替代直接的 `DrawRenderers` 调用，这样 RenderGraph 才能正确跟踪该 Pass 的依赖。

---

## 案例三：Compute + Raster 混合

有些效果需要 Compute Shader 做预处理（比如生成噪声图、模糊计算），然后 Raster Pass 读取结果做最终渲染。旧写法里这只是一个 CommandBuffer 里的连续操作，新写法需要拆成两种不同类型的 Pass。

### 旧写法思路

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get("ComputeBlur");

    // Compute：写入模糊结果
    cmd.SetComputeTextureParam(_computeShader, _kernelIndex, "_Result", _blurRT);
    cmd.SetComputeTextureParam(_computeShader, _kernelIndex, "_Source", _cameraColor);
    cmd.DispatchCompute(_computeShader, _kernelIndex, _threadGroupsX, _threadGroupsY, 1);

    // Raster：读取模糊结果合成
    cmd.SetGlobalTexture("_BlurTex", _blurRT);
    Blitter.BlitCameraTexture(cmd, _cameraColor, _cameraColor, _compositeMaterial, 0);

    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```

### 新写法（RecordRenderGraph）

```csharp
private class ComputePassData
{
    public TextureHandle Source;
    public TextureHandle Result;
    public ComputeShader ComputeShader;
    public int KernelIndex;
    public int ThreadGroupsX;
    public int ThreadGroupsY;
}

private class CompositePassData
{
    public TextureHandle BlurTexture;
    public TextureHandle CameraColor;
    public Material CompositeMaterial;
}

public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();

    // 创建 Compute 输出 RT
    var blurDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
    blurDesc.name = "_ComputeBlurRT";
    blurDesc.enableRandomWrite = true;  // Compute 写入必须开启
    TextureHandle blurRT = renderGraph.CreateTexture(blurDesc);

    // Pass 1: Compute Pass
    using (var builder = renderGraph.AddComputePass<ComputePassData>("Compute_Blur", out var passData))
    {
        passData.Source = resourceData.activeColorTexture;
        passData.Result = blurRT;
        passData.ComputeShader = _computeShader;
        passData.KernelIndex = _kernelIndex;
        passData.ThreadGroupsX = _threadGroupsX;
        passData.ThreadGroupsY = _threadGroupsY;

        builder.UseTexture(passData.Source, AccessFlags.Read);
        builder.UseTexture(passData.Result, AccessFlags.ReadWrite);

        builder.SetRenderFunc((ComputePassData data, ComputeGraphContext ctx) =>
        {
            ctx.cmd.SetComputeTextureParam(data.ComputeShader, data.KernelIndex, "_Source", data.Source);
            ctx.cmd.SetComputeTextureParam(data.ComputeShader, data.KernelIndex, "_Result", data.Result);
            ctx.cmd.DispatchCompute(data.ComputeShader, data.KernelIndex,
                data.ThreadGroupsX, data.ThreadGroupsY, 1);
        });
    }

    // Pass 2: Raster 合成
    using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Compute_Composite", out var passData))
    {
        passData.BlurTexture = blurRT;
        passData.CameraColor = resourceData.activeColorTexture;
        passData.CompositeMaterial = _compositeMaterial;

        builder.UseTexture(blurRT, AccessFlags.Read);
        builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

        builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) =>
        {
            data.CompositeMaterial.SetTexture("_BlurTex", data.BlurTexture);
            Blitter.BlitTexture(ctx.cmd, data.CameraColor, new Vector4(1, 1, 0, 0), data.CompositeMaterial, 0);
        });
    }
}
```

### 迁移关键点

**AddComputePass vs AddRasterRenderPass**：Compute Pass 使用 `renderGraph.AddComputePass<T>()`，回调签名是 `(T data, ComputeGraphContext ctx)`。Raster Pass 使用 `renderGraph.AddRasterRenderPass<T>()`，回调签名是 `(T data, RasterGraphContext ctx)`。两者的 `ctx.cmd` 类型不同，不能混用。

**enableRandomWrite**：Compute Shader 的输出纹理必须在创建时设置 `enableRandomWrite = true`，否则 GPU 无法写入。旧写法里这个属性在 `RenderTextureDescriptor` 上设置，新写法在 `TextureDesc` 上设置，名字和含义一致。

**AccessFlags.ReadWrite**：Compute 输出纹理用 `AccessFlags.ReadWrite` 声明，而不是 `SetRenderAttachment`。`SetRenderAttachment` 只用于 Raster Pass 的颜色/深度附件，Compute 的随机读写用 `UseTexture` + `AccessFlags.ReadWrite`。

---

## 迁移检查清单

下面这张表是 Execute API 到 RecordRenderGraph API 的逐项对应：

| 旧 API | 新 API | 注意 |
|--------|--------|------|
| `CommandBufferPool.Get()` / `Release()` | builder 自动提供 `ctx.cmd` | 不再需要手动获取和释放 |
| `GetTemporaryRT` / `ReleaseTemporaryRT` | `renderGraph.CreateTexture()` | 生命周期自动管理，无需手动释放 |
| `ConfigureTarget()` | `builder.SetRenderAttachment()` | 在 Pass 配置阶段声明，不在执行阶段 |
| `cmd.SetRenderTarget()` | `builder.SetRenderAttachment()` | 同上 |
| 读取纹理（采样） | `builder.UseTexture(handle, AccessFlags.Read)` | 必须声明，否则 RenderGraph 可能裁剪该 Pass |
| 写入纹理（颜色附件） | `builder.SetRenderAttachment(handle, index)` | index 是颜色附件槽位 |
| 写入深度附件 | `builder.SetRenderAttachmentDepth(handle)` | 独立方法，不占颜色附件 index |
| Compute 写入 | `builder.UseTexture(handle, AccessFlags.ReadWrite)` | 纹理需开启 `enableRandomWrite` |
| `RTHandle` 手动分配 | `renderGraph.CreateTexture(desc)` | 从 `GetTextureDesc` 拷贝参数再修改 |
| `DrawRenderers` | `CreateRendererList` + `UseRendererList` | RenderGraph 需要跟踪绘制依赖 |

### 常见迁移错误

**忘记 UseTexture 导致 Pass 被裁剪**：这是最常见的问题。RenderGraph 通过资源声明判断 Pass 是否有效。如果一个 Pass 没有任何 `UseTexture` 或 `SetRenderAttachment` 声明，RenderGraph 认为它没有副作用，直接跳过。症状是 Feature 没有任何报错但也没有效果。

**在新 API 里使用旧的 RTHandle**：`RecordRenderGraph` 内部只能用 `TextureHandle`，不能传入旧的 `RTHandle`。如果 Feature 同时保留了 `Execute()` 和 `RecordRenderGraph()`（兼容期），要注意两套资源不能混用。

**PassData 字段没赋值就进入 SetRenderFunc**：`SetRenderFunc` 的 lambda 捕获的是 PassData 对象的引用。如果在 `using` 块里忘记给 `passData.XXX` 赋值，lambda 执行时读到的是默认值（null / 0）。编译不会报错，运行时崩在 NullReferenceException。

**Compute 纹理忘记 enableRandomWrite**：`renderGraph.CreateTexture()` 创建的纹理默认不开启随机写入。Compute Pass 的输出纹理必须设置 `desc.enableRandomWrite = true`，否则 Dispatch 时 GPU 写入无效，画面全黑但无报错。

### 验证方法

迁移完成后，打开 **Window > Analysis > Render Graph Viewer**，检查：
1. 你的 Pass 是否出现在依赖图中（没出现说明被裁剪了）
2. Pass 之间的资源依赖箭头是否正确（读写关系是否和预期一致）
3. 临时纹理是否被正确复用（RenderGraph 会自动合并生命周期不重叠的纹理）

如果 Pass 被裁剪，回去检查 `UseTexture` 和 `SetRenderAttachment` 的声明是否完整。

---

## 相关文章

- [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}}) — Execute API 旧写法的完整流程
- [URP 深度扩展 02｜RenderGraph 实战：Unity 6 的新写法]({{< relref "rendering/urp-ext-02-rendergraph.md" >}}) — RenderGraph 核心概念
- [URP 深度扩展 06｜2022.3 → Unity 6 迁移指南]({{< relref "rendering/urp-ext-06-migration.md" >}}) — Breaking Change 全景与迁移策略
