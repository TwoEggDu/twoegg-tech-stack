---
title: "URP 深度扩展 02｜RenderGraph 实战：Unity 6 的新写法"
slug: "urp-ext-02-rendergraph"
date: "2026-03-25"
description: "Unity 6（URP 17）将 RenderGraph 设为默认渲染路径，推荐写法从 Execute() 改为 RecordRenderGraph()。本篇讲清楚为什么要换、新写法的核心概念（TextureHandle、ImportResource、UseTexture）、和旧写法的对比迁移，以及 RenderGraph Viewer 的调试方法。"
tags:
  - "Unity"
  - "URP"
  - "RenderGraph"
  - "Unity 6"
  - "Renderer Feature"
  - "渲染管线"
series: "URP 深度"
weight: 1600
---
> 版本说明：本篇针对 Unity 6（URP 17）的 RenderGraph API。Unity 2022.3 LTS 的写法见 URP扩展-01。两套 API 可以共存，但 Unity 6 中用旧写法会走 UnsafePass 包装，有警告且无法享受 RenderGraph 的自动优化。

---

## 为什么要换写法

Unity 2022.3 里，`ScriptableRenderPass` 的主入口是 `Execute()`，开发者手动管理 RT 的创建、绑定、释放。

Unity 6 引入 RenderGraph 作为默认路径，核心思路变了：

**旧方式（命令式）**：我要创建一张 RT，然后绑定它，然后往里画，然后释放它。

**新方式（声明式）**：我声明这个 Pass 会读哪些资源、写哪些资源，具体的创建和释放由 RenderGraph 自动管理。

这个转变带来三个实际收益：

1. **自动资源生命周期**：RT 的创建和释放由 RenderGraph 根据依赖关系自动决定，不再需要手动 `GetTemporaryRT` / `ReleaseTemporaryRT`
2. **自动 Pass 裁剪**：如果某个 Pass 的输出没有被后续 Pass 读取，RenderGraph 自动跳过它，不执行也不分配 RT
3. **依赖图可视化**：RenderGraph Viewer 能看到所有 Pass 的资源依赖关系

---

## 新旧写法结构对比

### 旧写法（Execute API，URP 14 / Unity 2022.3）

```csharp
public class MyRenderPass : ScriptableRenderPass
{
    private RTHandle _tempRT;

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor desc)
    {
        RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_MyTempRT");
        ConfigureTarget(_tempRT);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get("MyPass");

        Blitter.BlitCameraTexture(cmd,
            renderingData.cameraData.renderer.cameraColorTargetHandle,
            _tempRT, _material, 0);
        Blitter.BlitCameraTexture(cmd,
            _tempRT,
            renderingData.cameraData.renderer.cameraColorTargetHandle);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose() => _tempRT?.Release();
}
```

### 新写法（RecordRenderGraph API，URP 17 / Unity 6）

```csharp
public class MyRenderPass : ScriptableRenderPass
{
    // Pass 数据结构：只存这个 Pass 需要的资源句柄
    private class PassData
    {
        public TextureHandle Source;
        public TextureHandle Destination;
        public Material Material;
    }

    private Material _material;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        // 声明临时 RT，RenderGraph 自动管理生命周期
        var desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        desc.name = "_MyTempRT";
        desc.clearBuffer = false;
        TextureHandle tempRT = renderGraph.CreateTexture(desc);

        // Pass 1：效果写入临时 RT
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("MyEffect", out var passData))
        {
            passData.Source      = resourceData.activeColorTexture;
            passData.Destination = tempRT;
            passData.Material    = _material;

            builder.UseTexture(passData.Source, AccessFlags.Read);
            builder.SetRenderAttachment(passData.Destination, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
            });
        }

        // Pass 2：写回相机颜色
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("MyEffect_Blit", out var passData))
        {
            passData.Source      = tempRT;
            passData.Destination = resourceData.activeColorTexture;

            builder.UseTexture(passData.Source, AccessFlags.Read);
            builder.SetRenderAttachment(passData.Destination, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), 0);
            });
        }
    }
}
```

变化一目了然：不再手动 `GetTemporaryRT` / `Release`，不再用 `CommandBufferPool`，资源读写关系通过 `UseTexture` / `SetRenderAttachment` 显式声明。

---

## 核心概念逐一拆解

### TextureHandle

RenderGraph 里不直接用 `RTHandle`，而是用 `TextureHandle`。它是 RenderGraph 管理的资源引用，**只在当前帧有效，不能跨帧持有**。

```csharp
// 正确：在 RecordRenderGraph 里获取和使用
TextureHandle tex = resourceData.activeColorTexture;

// 错误：不能存为成员变量跨帧使用
// private TextureHandle _cachedHandle;  下一帧这个 Handle 就失效了
```

### CreateTexture vs ImportTexture

**`CreateTexture`**：在 RenderGraph 内部创建临时 RT，生命周期完全由 RenderGraph 管理，Pass 结束后自动释放。这是最常见的用法。

**`ImportTexture`**：把外部的 `RTHandle` 导入 RenderGraph，用于需要跨帧保留的 RT（TAA 历史帧缓存、持久化 RT 等）。

```csharp
// 跨帧 RT 自己管理，Import 进 RenderGraph 使用
private RTHandle _persistentRT;

public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    TextureHandle handle = renderGraph.ImportTexture(_persistentRT);
    // 之后正常使用 handle
}
```

### UseTexture 和 SetRenderAttachment

建立 Pass 资源依赖关系的两个关键调用，RenderGraph 靠这些声明推断执行顺序和资源生命周期：

```csharp
// 声明读取（Shader 采样输入）
builder.UseTexture(textureHandle, AccessFlags.Read);

// 声明写入（渲染目标）
builder.SetRenderAttachment(textureHandle, 0); // 0 是 color attachment 索引

// 声明深度写入
builder.SetRenderAttachmentDepth(depthHandle, AccessFlags.Write);
```

如果漏了 `UseTexture` 声明，RenderGraph 不知道这个 Pass 依赖那张贴图，可能在贴图就绪之前执行这个 Pass，导致采样到错误内容或黑屏。

### ContextContainer 和 frameData

`frameData` 是每帧重置的数据容器，通过 `Get<T>()` 获取当前帧的渲染数据：

```csharp
// 相机资源（颜色 RT、深度 RT、G-Buffer 等）
var resourceData = frameData.Get<UniversalResourceData>();
TextureHandle colorRT  = resourceData.activeColorTexture;
TextureHandle depthRT  = resourceData.activeDepthTexture;
TextureHandle normalRT = resourceData.cameraNormalsTexture; // 需要开启 DepthNormals Prepass

// 相机参数
var cameraData = frameData.Get<UniversalCameraData>();

// 光照数据
var lightData = frameData.Get<UniversalLightData>();
```

### AddRasterRenderPass vs AddComputePass vs AddUnsafePass

| Pass 类型 | 用途 | 命令缓冲区类型 |
|-----------|------|---------------|
| `AddRasterRenderPass` | 光栅化（Blit、DrawRenderers）| `RasterCommandBuffer` |
| `AddComputePass` | Compute Shader 调度 | `ComputeCommandBuffer` |
| `AddUnsafePass` | 旧 API 兜底 | `UnsafeCommandBuffer`（完整 CommandBuffer）|

大多数后处理和自定义 Pass 用 `AddRasterRenderPass`。`AddUnsafePass` 是迁移过渡用的，旧 `Execute()` 里的逻辑可以直接搬进去运行，但无法享受 RenderGraph 的自动优化。

---

## 完整示例：灰度后处理 Feature

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class GrayscaleFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
        public Material Material;
    }

    public Settings settings = new();
    private GrayscalePass _pass;

    public override void Create()
    {
        _pass = new GrayscalePass(settings);
        _pass.renderPassEvent = settings.Event;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.Material == null) return;
        renderer.EnqueuePass(_pass);
    }

    private class GrayscalePass : ScriptableRenderPass
    {
        private readonly Settings _settings;

        private class PassData
        {
            public TextureHandle Source;
            public TextureHandle Dest;
            public Material Material;
        }

        public GrayscalePass(Settings settings) => _settings = settings;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            // 预览相机不处理
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_GrayscaleTemp";
            desc.clearBuffer = false;
            TextureHandle temp = renderGraph.CreateTexture(desc);

            // Pass 1：灰度效果
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale", out var data))
            {
                data.Source   = src;
                data.Dest     = temp;
                data.Material = _settings.Material;

                builder.UseTexture(data.Source, AccessFlags.Read);
                builder.SetRenderAttachment(data.Dest, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.Source, new Vector4(1, 1, 0, 0), d.Material, 0);
                });
            }

            // Pass 2：写回相机颜色
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale_CopyBack", out var data))
            {
                data.Source = temp;
                data.Dest   = src;

                builder.UseTexture(data.Source, AccessFlags.Read);
                builder.SetRenderAttachment(data.Dest, 0);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.Source, new Vector4(1, 1, 0, 0), 0);
                });
            }
        }
    }
}
```

---

## 常见问题

### 效果没有出现：Pass 被自动裁剪

RenderGraph 会裁剪输出没有被后续 Pass 读取的 Pass。中间计算 Pass 最容易中招。

```csharp
builder.AllowPassCulling(false);
```

遇到效果不出现，先加这一行确认是不是裁剪问题。

### 旧 Feature 在 Unity 6 里的行为

Unity 6 里只有 `Execute()` 没有 `RecordRenderGraph()` 的 Pass，会被自动包装成 `AddUnsafePass` 执行。功能正确，但控制台有 Warning，无法在 RenderGraph Viewer 看到依赖关系，无法享受自动裁剪和资源优化。

**迁移策略**：先加 `RecordRenderGraph()` 方法，在里面用 `AddUnsafePass` 包住原来的逻辑消除 Warning，之后再逐步改成 `AddRasterRenderPass`。

---

## RenderGraph Viewer 调试

**Window → Analysis → Render Graph Viewer**（Unity 6 新增）

进入 Play 模式后可以查看：

- **Pass 列表**：当前帧所有 Pass 的执行顺序
- **资源依赖图**：哪个 Pass 读/写哪张 RT
- **被裁剪的 Pass**：显示为灰色，直接定位"Pass 为什么没执行"
- **RT 生命周期**：每张 RT 在哪个 Pass 创建，哪个 Pass 之后释放

效果不出现时，先开 Viewer 确认 Pass 状态，比盲改代码高效得多。

---

## 导读

- 上一篇：[URP 深度扩展 01｜Renderer Feature 完整开发：从零写一个 ScriptableRendererFeature]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})
- 下一篇：[URP 深度扩展 03｜URP 后处理扩展：Volume Framework 与自定义效果]({{< relref "rendering/urp-ext-03-postprocessing.md" >}})

---

## 小结

- RenderGraph 核心转变：**命令式 → 声明式**，资源依赖显式声明，生命周期自动管理
- `RecordRenderGraph()` 替代 `Execute()`，`TextureHandle` 替代 `RTHandle`
- `CreateTexture` 用于临时 RT，`ImportTexture` 用于跨帧持久 RT
- `UseTexture` + `SetRenderAttachment` 建立依赖关系，不能漏声明
- 旧代码用 `AddUnsafePass` 过渡，逐步改成 `AddRasterRenderPass`
- Pass 不出现先看 RenderGraph Viewer 确认是否被裁剪

下一篇：URP扩展-03，URP 后处理扩展——Volume Framework 的参数暴露机制、自定义 VolumeComponent、在 Renderer Feature 里读取 Volume 参数驱动效果。
