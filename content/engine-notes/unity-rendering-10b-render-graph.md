+++
title = "RenderGraph：声明式渲染资源管理"
slug = "unity-rendering-10b-render-graph"
date = 2025-01-26
description = "RenderGraph 用声明式依赖图替代手动管理 RT 和 CommandBuffer，理解 Pass 如何声明资源读写关系、FrameResources 里的内置资源如何获取、以及与旧 API 的边界在哪里。"
[taxonomies]
tags = ["Unity", "RenderGraph", "URP", "RTHandle", "渲染管线"]
series = ["Unity 渲染系统"]
[extra]
weight = 1350
+++

如果只用一句话概括这篇：RenderGraph 把"我要用什么 RT、读还是写"从命令式的 SetRenderTarget 变成了声明式的依赖声明，引擎据此自动管理 RT 生命周期、剔除无用 Pass、并行调度资源。

---

## 从上一篇出发

10（URP 扩展实践）展示了旧式的 `Execute` + `CommandBuffer` + `RTHandle` 写法，并在最后提到 Unity 6 开始推荐 RenderGraph API。但只给了一个骨架级的代码示例。

这篇把 RenderGraph 的设计思路和关键 API 讲清楚，让你能判断什么情况下应该迁移，以及迁移的实际工作量。

---

## 旧写法的根本问题

用旧 API 写一个有临时 RT 的 Pass，开发者需要手动处理：

```csharp
// 旧写法：手动管理 RT 生命周期
public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
{
    // 手动分配（每帧或尺寸变化时）
    RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, descriptor, ...);

    // 手动声明：这个 Pass 要写入这张 RT
    ConfigureTarget(m_TempRT);
    ConfigureClear(ClearFlag.Color, Color.black);
}

public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get();
    // 手动 SetRenderTarget
    cmd.SetRenderTarget(m_TempRT);
    // ... 绘制命令 ...
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}

public void Dispose()
{
    // 手动释放
    m_TempRT?.Release();
}
```

问题：

1. **引擎不知道 Pass 之间的数据依赖**：RT 什么时候可以释放、Pass 能否并行，引擎完全不知道，只能按顺序串行执行
2. **无用 Pass 不能自动剔除**：某个 Pass 的输出如果没有被任何后续 Pass 消费，引擎无法得知这一点，仍然会执行这个 Pass
3. **内存峰值高**：所有 Pass 的 RT 在整帧内都存在，即使某张 RT 只在一个 Pass 里用到，也无法提前释放给下一个 Pass 复用

---

## RenderGraph 的核心思路：先声明，再执行

RenderGraph 把 Pass 的执行拆成两个阶段：

```
阶段一：Record（声明阶段，不执行任何 GPU 命令）
  每个 Pass 调用 RecordRenderGraph()
  声明：我需要哪些 RT（读 / 写）
  声明：我的执行函数是什么
  ↓
RenderGraph 内部：
  分析所有 Pass 的依赖关系，建立有向依赖图
  剔除没有任何 Pass 消费其输出的 Pass（Dead Code Elimination）
  计算每张 RT 的生命周期（最早被哪个 Pass 创建，最晚被哪个 Pass 使用）
  分配内存（相互不重叠的 RT 可以复用同一块显存）
  ↓
阶段二：Execute（执行阶段）
  按拓扑顺序执行各 Pass 的执行函数
  RT 在第一次被用到时分配，在最后一次被用到后立即释放
```

这套机制的好处：
- 引擎完全掌握资源生命周期 → 自动最小化显存峰值
- 未被消费的 Pass 自动跳过 → 减少不必要的渲染开销
- 依赖关系显式化 → 为将来的并行调度预留空间

---

## TextureHandle：RenderGraph 里的 RT 引用

在 RenderGraph 里，RT 不再用 `RTHandle` 或 `RenderTargetIdentifier` 来引用，而是用 **`TextureHandle`**：

```csharp
// TextureHandle 是 RenderGraph 管理的 RT 的"门票"
// 它本身不持有 GPU 资源，只是一个 RenderGraph 内部的引用 ID
TextureHandle colorHandle;
TextureHandle depthHandle;
```

有两类 TextureHandle：

**1. 导入的外部资源（Imported）：** 已经存在的 RT（如 Camera 的颜色缓冲），通过 `Import` 进入 RenderGraph 的管辖：

```csharp
// FrameResources 里已经帮你 Import 好了 Camera 的内置 RT
TextureHandle cameraColor = frameResources.cameraColor;
TextureHandle cameraDepth = frameResources.cameraDepth;
```

**2. 在图内创建的资源（Transient）：** Pass 自己需要的临时 RT，在图内声明：

```csharp
// 创建一张临时 RT（只存在于 RenderGraph 的生命周期内，由图管理分配和释放）
TextureHandle tempHandle = UniversalRenderer.CreateRenderGraphTexture(
    renderGraph,
    descriptor,   // 描述格式和尺寸
    "_MyTempRT",  // 调试名称
    false         // clearColor
);
```

---

## 完整的 RenderGraph Pass 写法

```csharp
public class MyFullScreenPass : ScriptableRenderPass
{
    private Material m_Material;

    // Pass 需要的数据，声明为结构体（会被传入 SetRenderFunc）
    private class PassData
    {
        public TextureHandle source;
        public TextureHandle dest;
        public Material material;
    }

    public MyFullScreenPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // ★ RenderGraph 路径：声明阶段
    public override void RecordRenderGraph(RenderGraph renderGraph,
                                           FrameResources frameResources,
                                           ref RenderingData renderingData)
    {
        // 从 FrameResources 取 Camera 的内置 RT
        TextureHandle cameraColor = frameResources.cameraColor;

        // 创建一张临时 RT（和 Camera RT 相同格式）
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        TextureHandle tempRT = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, descriptor, "_MyPassTemp", false
        );

        // 声明 Pass：把逻辑和资源依赖一起注册进 RenderGraph
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("MyFullScreenPass", out var passData))
        {
            // 填充 PassData（会在 SetRenderFunc 里被使用）
            passData.source = cameraColor;
            passData.dest = tempRT;
            passData.material = m_Material;

            // 声明资源依赖
            builder.UseTexture(cameraColor);           // 声明读取 cameraColor
            builder.SetRenderAttachment(tempRT, 0);    // 声明写入 tempRT（作为 RT 0）

            // 声明执行函数（Lambda，在 Execute 阶段被调用）
            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                // ctx.cmd 是 RasterCommandBuffer，只能提交 Raster 类命令
                Blitter.BlitTexture(ctx.cmd, data.source,
                    new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // 第二个 Pass：把 tempRT Blit 回 cameraColor
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("MyFullScreenPass_Copy", out var passData))
        {
            passData.source = tempRT;
            passData.dest = cameraColor;

            builder.UseTexture(tempRT);
            builder.SetRenderAttachment(cameraColor, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.source,
                    new Vector4(1, 1, 0, 0), 0);
            });
        }
        // tempRT 在最后一次被 UseTexture 的 Pass 执行后自动释放
        // 不需要手动 Release
    }

    // 旧 API 兼容路径（Unity 6 之前的版本仍走这个方法）
    // 两个方法可以同时存在，引擎根据版本选择调用哪个
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // ... 旧写法 ...
    }
}
```

---

## FrameResources：内置资源的获取方式

`FrameResources`（全名 `UniversalResourceData`，在较新版本里）提供了当前帧所有 URP 内置 RT 的 TextureHandle：

| 属性 | 内容 | 可用阶段 |
|---|---|---|
| `cameraColor` | Camera 的最终颜色 RT | Opaque 之后 |
| `cameraDepth` | Camera 的深度 RT | Opaque 之后 |
| `cameraDepthTexture` | 复制出来的深度贴图（`_CameraDepthTexture`）| DepthPrepass 之后 |
| `cameraOpaqueTexture` | 不透明颜色的快照（`_CameraOpaqueTexture`）| Opaque 之后（需在 Asset 里开启）|
| `mainShadowsTexture` | 主光源 Shadow Map | Shadow Pass 之后 |
| `additionalShadowsTexture` | 额外光源 Shadow Map | Shadow Pass 之后 |
| `activeColorTexture` | 当前激活的颜色 RT（可能是 cameraColor 或中间 RT）| 任何阶段 |

在 `RecordRenderGraph` 里，通过参数 `FrameResources frameResources` 直接访问：

```csharp
TextureHandle shadowMap = frameResources.mainShadowsTexture;
TextureHandle depth = frameResources.cameraDepthTexture;
```

---

## 三类 Pass Builder

RenderGraph 提供三类 Pass，对应不同的 GPU 操作类型：

| Builder | 适用场景 | 可用命令类型 |
|---|---|---|
| `AddRasterRenderPass` | 渲染到 RT（Draw Call / Blit）| `RasterCommandBuffer`（含 Draw / Blit / SetRenderTarget）|
| `AddComputePass` | Compute Shader 计算 | `ComputeCommandBuffer`（含 Dispatch）|
| `AddUnsafePass` | 需要用到旧 API（兼容层）| 完整 `CommandBuffer`（无限制，但失去优化机会）|

`AddUnsafePass` 是从旧代码迁移的过渡方案——如果你有旧的 `Execute` 写法但还不想完全重写，可以先用 `AddUnsafePass` 把逻辑包裹起来，之后再逐步改成 `AddRasterRenderPass`。

---

## 旧 API 和 RenderGraph 的共存

Unity 6 里，旧 `Execute` 和新 `RecordRenderGraph` 可以同时存在于同一个 Pass 类里：

```csharp
public class MyPass : ScriptableRenderPass
{
    // Unity 6+：RenderGraph 路径
    public override void RecordRenderGraph(RenderGraph renderGraph,
                                           FrameResources frameResources,
                                           ref RenderingData renderingData)
    {
        // 新写法
    }

    // Unity 2022–2023：兼容路径
    public override void Execute(ScriptableRenderContext context,
                                 ref RenderingData renderingData)
    {
        // 旧写法
    }
}
```

引擎根据当前是否开启了 RenderGraph 模式（URP Asset 里有开关）决定调用哪个方法。这让你可以在保持旧项目正常运行的同时，逐步迁移到新 API。

---

## 什么时候需要迁移到 RenderGraph

**应该迁移：**
- 项目目标是 Unity 6+
- 有多个自定义 Pass，RT 数量较多，想降低显存峰值
- 出现了 RT 生命周期管理导致的 bug（RT 释放时机不对、内存泄漏）

**可以暂时不迁移：**
- 项目目标是 Unity 2022/2023，RenderGraph 还处于预览阶段
- 只有 1–2 个简单的全屏 Blit Pass，旧 API 工作良好
- 团队对旧 API 熟悉，迁移成本大于收益

**不建议在迁移过程中混用两套 API**（除非用 `AddUnsafePass` 封装旧代码），混用容易导致资源状态不一致。

---

## 小结

RenderGraph 做了三件事：

```
1. 声明替代命令
   旧：cmd.SetRenderTarget(rt) → 命令式，立即执行
   新：builder.SetRenderAttachment(handle, 0) → 声明式，记录依赖

2. 引擎接管 RT 生命周期
   旧：开发者手动 ReAllocate + Release
   新：在 CreateRenderGraphTexture 后不再需要手动管理

3. 图级别的优化机会
   自动剔除无用 Pass
   RT 显存复用（无重叠生命周期的 RT 可以共用一块显存）
   未来的并行调度基础
```

代价是 API 学习曲线更陡，调试时需要理解 RenderGraph 的两阶段（Record vs Execute）——Frame Debugger 里显示的仍然是 Execute 阶段的结果，但 Pass 是否被执行、RT 何时分配，需要结合声明阶段的逻辑来分析。
