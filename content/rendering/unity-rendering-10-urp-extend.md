---
title: "怎么在 URP 里扩展渲染流程"
slug: "unity-rendering-10-urp-extend"
date: "2025-01-26"
description: "ScriptableRendererFeature + ScriptableRenderPass 的完整写法，RTHandle API 的用法，RenderGraph 的基本思路，以及描边、屏幕特效、自定义 Pass 注入的实现模式。"
tags:
  - "Unity"
  - "URP"
  - "RendererFeature"
  - "RenderPass"
  - "RTHandle"
  - "RenderGraph"
series: "Unity 渲染系统"
weight: 1300
---
如果只用一句话概括这篇：在 URP 里扩展渲染流程，本质是写一个 `ScriptableRendererFeature` 来创建并注册 `ScriptableRenderPass`，然后在 Pass 的 `Execute` 里用 `CommandBuffer` 提交 GPU 命令——关键是理解 RT 的生命周期管理，以及 RenderGraph 和旧 API 的边界。

---

## 从上一篇出发

上一篇（09：URP 架构）描述了 URP 的四层层级和默认 Pass 顺序，以及 `ScriptableRendererFeature` 和 `ScriptableRenderPass` 的职责。这篇把它转化为可以直接参考的实践模式。

---

## 完整的 RendererFeature + RenderPass 结构

下面是一个完整的全屏效果（Full Screen Pass）的写法骨架，涵盖了大部分自定义需求的共同结构：

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ─── Feature：管理配置和 Pass 生命周期 ───────────────────────────────────────
public class FullScreenEffectFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material effectMaterial;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public Settings settings = new Settings();
    private FullScreenEffectPass m_Pass;

    // Create 在以下时机被调用：
    //   - Renderer 首次创建时
    //   - Inspector 里修改了 Settings 参数时
    //   - 调用 ScriptableRenderer.RenderingFeatures 后
    public override void Create()
    {
        m_Pass = new FullScreenEffectPass(settings.effectMaterial, settings.passEvent);
    }

    // 每帧每个 Camera 渲染前调用
    // 在这里决定当前 Camera 是否需要这个 Pass
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 只在 Game 视图 Camera 里插入，跳过 Scene 视图和预览 Camera
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        if (settings.effectMaterial == null) return;

        renderer.EnqueuePass(m_Pass);
    }

    // Feature 被销毁时调用，释放 Pass 持有的资源
    protected override void Dispose(bool disposing)
    {
        m_Pass.Dispose();
    }
}

// ─── Pass：实际执行 GPU 命令 ──────────────────────────────────────────────────
public class FullScreenEffectPass : ScriptableRenderPass
{
    private Material m_Material;
    private RTHandle m_TempRT;       // 临时 RT，避免读写同一张 RT

    public FullScreenEffectPass(Material material, RenderPassEvent passEvent)
    {
        m_Material = material;
        renderPassEvent = passEvent;
        profilingSampler = new ProfilingSampler("FullScreenEffect");
    }

    // 每帧 Execute 前调用，在这里分配临时 RT
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0; // 临时 RT 不需要深度
        RenderingUtils.ReAllocateIfNeeded(
            ref m_TempRT,
            descriptor,
            name: "_FullScreenEffectTemp"
        );
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, profilingSampler))
        {
            // 取当前 Camera 的颜色 RT
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Blit 到临时 RT（避免 source 同时作为输入和输出）
            Blitter.BlitCameraTexture(cmd, source, m_TempRT, m_Material, 0);

            // 再 Blit 回 Camera RT
            Blitter.BlitCameraTexture(cmd, m_TempRT, source);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    // Camera 渲染结束后调用，清理临时资源
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandle 不在这里释放，在 Dispose 里统一释放
    }

    public void Dispose()
    {
        m_TempRT?.Release();
    }
}
```

---

## RTHandle：正确管理临时 RT

URP 2021+ 推荐使用 `RTHandle` 而不是直接使用 `RenderTargetIdentifier`。区别：

| | `RenderTargetIdentifier`（旧） | `RTHandle`（新） |
|---|---|---|
| 内存管理 | 手动 `cmd.GetTemporaryRT` / `ReleaseTemporaryRT` | 由 `RTHandleSystem` 统一管理 |
| 尺寸追踪 | 需要手动传分辨率 | 自动跟随 Camera 分辨率缩放 |
| 生命周期 | 每帧申请/释放 | 跨帧复用，分辨率变化时自动 ReAlloc |

使用 `RTHandle` 的关键函数：

```csharp
// 只在尺寸变化或首次分配时真正重新分配，否则复用
RenderingUtils.ReAllocateIfNeeded(
    ref m_RTHandle,          // 引用，函数内部可能替换
    descriptor,              // 从 cameraTargetDescriptor 派生
    FilterMode.Bilinear,
    TextureWrapMode.Clamp,
    name: "_MyRT"
);

// 释放
m_RTHandle?.Release();
m_RTHandle = null;
```

**为什么不能直接 Blit 到 Camera RT 自身：** GPU 不允许同一张 RT 同时作为 Shader 的输入 Texture 和输出 RT（读写冲突），必须先 Blit 到临时 RT，再 Blit 回来。

---

## 常见扩展场景

### 场景一：描边效果（对特定物体）

描边通常用 Stencil Buffer 配合 Pass 叠加实现：

```
Pass 1（插入到 AfterRenderingOpaques）：
  只渲染需要描边的物体
  写 Stencil = 1，不写颜色

Pass 2（紧跟 Pass 1 之后）：
  对同一批物体，放大后渲染（法线外扩 或 屏幕空间膨胀）
  Stencil Test = Not Equal 1（只渲染边缘部分）
  写描边颜色
```

在 URP 里，这两个 Pass 都是 `ScriptableRenderPass`，都挂在同一个 `ScriptableRendererFeature` 里注册。`DrawRenderers` 时用 `LayerMask` 或 `RenderingLayerMask` 过滤只渲染目标物体。

### 场景二：屏幕空间效果（全屏后处理）

参考上面的完整骨架。关键选择：

- `passEvent = AfterRenderingOpaques`：在不透明物体渲染后，能采样 `_CameraDepthTexture` 和不透明颜色，但半透明物体还没渲染
- `passEvent = AfterRenderingTransparents`：半透明渲染后，可以拿到完整的颜色，但不透明纹理 `_CameraOpaqueTexture` 此时不保证可用
- `passEvent = BeforeRenderingPostProcessing`：所有物体渲染完，后处理之前；此时 URP 的 PostProcessing 还没执行，可以做"预处理"效果

### 场景三：额外的 Shadow Pass（自定义光源）

如果需要自定义光源（如投射自定义形状 Shadow 的手电筒），可以在 `BeforeRenderingShadows` 插入一个自定义 Shadow Pass，把结果存到自定义 RT，然后在 Opaque 阶段的 Shader 里手动采样这张 RT。

---

## RenderGraph：声明式资源管理（Unity 6+）

从 Unity 6（6000.x）开始，URP 官方推荐使用 `RenderGraph` API 代替手动管理 RT 和 CommandBuffer。

**旧方式（Compatibility Mode）：**

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get();
    cmd.SetRenderTarget(m_TempRT);
    cmd.ClearRenderTarget(true, true, Color.black);
    cmd.Blit(source, m_TempRT, m_Material);
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}
```

**新方式（RenderGraph）：**

```csharp
// Pass 需要额外实现 RecordRenderGraph 方法
public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources,
                                       ref RenderingData renderingData)
{
    // 声明需要的资源（RenderGraph 决定何时分配和释放）
    TextureHandle source = frameResources.cameraColor;
    TextureHandle dest = UniversalRenderer.CreateRenderGraphTexture(
        renderGraph, renderingData.cameraData.cameraTargetDescriptor,
        "_EffectTemp", false
    );

    // 声明 Pass
    using (var builder = renderGraph.AddRasterRenderPass<PassData>("MyEffect", out var passData))
    {
        passData.source = source;
        passData.dest = dest;
        passData.material = m_Material;

        builder.UseTexture(source);           // 声明读取
        builder.SetRenderAttachment(dest, 0); // 声明写入

        builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
        {
            Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
        });
    }
}
```

RenderGraph 的优势：

- **自动内存管理**：RenderGraph 知道哪些 RT 在哪里被用、在哪里不再需要，自动 Alloc 和 Release
- **自动 Pass 剔除**：如果某个 Pass 的输出没有被后续 Pass 消费，RenderGraph 可以跳过它（减少不必要的渲染）
- **可读的依赖图**：声明式写法使 Pass 之间的依赖关系显式化，便于维护

代价：RenderGraph API 学习曲线较陡，且与旧 API 不完全兼容（部分旧 Pass 写法需要重写）。Unity 在 6.x 里提供了兼容模式，旧 API 仍然工作但会被标记为过时。

---

## 调试自定义 Pass

**问题：Pass 没有执行**

Frame Debugger 里看不到你的 Pass 名字。可能原因：
- `AddRenderPasses` 里的条件判断（Camera 类型、Material 判空）过滤掉了
- `ScriptableRendererFeature` 在 Inspector 里没有勾选（左侧 checkbox 未启用）
- `Create()` 报了异常导致 `m_Pass` 为 null，但 `AddRenderPasses` 没有判空

**问题：Pass 执行了但效果不对**

在 Frame Debugger 里点击对应的 Blit 事件，看：
- 输入纹理（Source）是否正确（用 Texture 面板查看内容）
- 输出 RT（Dest）是否是预期的 RT
- Material 的参数是否正确传入（从 Shader Properties 面板确认）

**问题：颜色输出有移位或翻转**

不同平台 UV 原点不同（Metal/OpenGL 是下方，DX11 是上方），使用 `Blitter.BlitCameraTexture` 代替 `cmd.Blit` 可以自动处理 UV 翻转。

---

## 小结：扩展 URP 的决策树

```
需要自定义渲染逻辑？
  ↓
是否只需要全屏效果（后处理）？
  → YES：写 FullScreenPassRendererFeature（URP 内置，不需要从零写 Feature）
       或：写 ScriptableRendererFeature + ScriptableRenderPass，passEvent = AfterRenderingX

是否需要对特定物体渲染额外 Pass（描边、轮廓、特效）？
  → YES：ScriptableRenderPass 里用 DrawRenderers，配合 LayerMask / RenderingLayerMask 过滤物体

是否需要生成自定义 RT 供 Shader 采样（如自定义深度、ID 图）？
  → YES：OnCameraSetup 里用 RTHandle 分配，Execute 里 SetRenderTarget + DrawRenderers

Unity 版本是 6.x+？
  → YES：优先学 RenderGraph API（RecordRenderGraph 方法）
  → NO ：用旧 Execute + CommandBuffer 写法
```

下一篇（11：HDRP 的定位与取舍）是本系列的最后一篇，将对比 URP 和 HDRP 的核心架构差异，以及什么项目适合用 HDRP、从 URP 迁移到 HDRP 的主要代价。
