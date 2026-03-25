+++
title = "URP 深度前置 01｜CommandBuffer：Blit、SetRenderTarget、DrawRenderers"
slug = "urp-pre-01-commandbuffer"
date = 2026-03-25
description = "深入 CommandBuffer 的三个核心操作：Blit 的正确用法与 UV 翻转陷阱、SetRenderTarget 各重载与 LoadAction/StoreAction 的开销含义、DrawRenderers 的过滤与排序控制。"
[taxonomies]
tags = ["Unity", "URP", "CommandBuffer", "Blit", "渲染管线"]
series = ["URP 深度"]
[extra]
weight = 1500
+++

这篇是 URP 深度系列的第一篇前置文章。系列八（unity-rendering-08）讲了 CommandBuffer 是什么，系列十（unity-rendering-10）展示了 Pass 骨架。这篇专门深入 CommandBuffer 最常用的三个操作——**Blit、SetRenderTarget、DrawRenderers**——把使用中真正会遇到的陷阱和选择讲清楚。

---

## Blit：全屏操作的正确写法

### cmd.Blit 的本质

`cmd.Blit(src, dst)` 做的事情：渲染一个覆盖全屏的四边形，把 `src` 作为 `_MainTex` 输入，把结果写入 `dst`。可以选择性地传入 Material，让这张全屏 Quad 走指定的 Shader。

```csharp
// 最基础的形式
cmd.Blit(sourceRT, destRT);

// 带 Material：把 sourceRT 作为 _MainTex 传给 Material 的 Pass 0
cmd.Blit(sourceRT, destRT, material);

// 指定 Material 的某个 Pass
cmd.Blit(sourceRT, destRT, material, passIndex);
```

### cmd.Blit 的 UV 翻转问题

`cmd.Blit` 在不同平台 UV 原点不同：

- **OpenGL / Metal / Vulkan**：UV 原点在左下角
- **DirectX**：UV 原点在左上角

Unity 内部在 `cmd.Blit` 里根据平台做了处理，大多数情况下没问题。但当 `src` 和 `dst` 的 RenderTexture 类型不一致时（比如一个是 Screen，一个是 RT），或者当 Camera 渲染到 RT 再 Blit 到屏幕时，**坐标映射不一致会导致图像翻转或错位**。

**URP 2021+ 的正确做法**：用 `Blitter` 类代替 `cmd.Blit`：

```csharp
// Blitter 会自动处理 UV 翻转和 DX / GL 差异
Blitter.BlitCameraTexture(cmd, source, dest);

// 带 Material 和 Pass 索引
Blitter.BlitCameraTexture(cmd, source, dest, material, passIndex);

// 更底层，提供精确的 UV 缩放和偏移
// scaleBias = (scaleX, scaleY, offsetX, offsetY)
// 全尺寸: new Vector4(1, 1, 0, 0)
Blitter.BlitTexture(cmd, source, dest, new Vector4(1, 1, 0, 0), material, passIndex);
```

`Blitter` 的 Shader 内部使用 `DYNAMIC_SCALING_BUILT_IN` 等宏处理了平台差异。开发者自定义的 Blit Shader 需要用 `Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl` 里提供的顶点着色器，而不是手写全屏 Quad 的顶点 Shader。

### 双 Blit 模式：避免读写同一张 RT

这是新手最容易踩的坑：**GPU 不允许同一张 RT 同时作为输入 Texture 和输出 RT**（Read-Write Hazard）。

```csharp
// ❌ 错误：source 和 dest 是同一张 RT
var cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
Blitter.BlitCameraTexture(cmd, cameraColor, cameraColor, material, 0);

// ✅ 正确：先 Blit 到临时 RT，再 Blit 回来
Blitter.BlitCameraTexture(cmd, cameraColor, m_TempRT, material, 0);
Blitter.BlitCameraTexture(cmd, m_TempRT, cameraColor);
```

部分驱动（尤其是移动端 Adreno / Mali）会静默读取旧数据，导致效果看起来"差一帧"或者有条带，而不是直接 Crash，所以这个问题在 PC 上不容易被发现。

### 哪些情况不需要双 Blit

如果 Shader 不采样 `_MainTex`（比如纯色覆盖、只写 Stencil 的操作），可以直接在 Camera RT 上渲染，不需要临时 RT。此时用 `SetRenderTarget` + `DrawProcedural` 或 `DrawRenderers` 即可。

---

## SetRenderTarget：控制渲染目标

### 基本重载

```csharp
// 只设置颜色 RT（深度复用当前 Depth Buffer）
cmd.SetRenderTarget(colorRT);

// 设置颜色 + 深度 RT
cmd.SetRenderTarget(colorRT, depthRT);

// RTHandle 版本（URP 2021+ 推荐）
cmd.SetRenderTarget(colorRTHandle, depthRTHandle);
```

### LoadAction 与 StoreAction：移动端优化的关键

`SetRenderTarget` 的完整重载：

```csharp
cmd.SetRenderTarget(
    colorRT,
    loadAction: RenderBufferLoadAction.DontCare,   // 加载时的行为
    storeAction: RenderBufferStoreAction.Store,    // 存储时的行为
    depthRT,
    depthLoadAction: RenderBufferLoadAction.Load,
    depthStoreAction: RenderBufferStoreAction.DontCare
);
```

这两个参数在桌面端基本无关紧要，但在**移动端 TBDR 架构**（Mali、Adreno、Apple GPU）里影响巨大：

| LoadAction | 含义 | 代价 |
|---|---|---|
| `Load` | 把 RT 内容从主存加载到 Tile Memory | 有带宽开销（主存 → GPU） |
| `Clear` | 用指定颜色清除（GPU 自行填充，不读主存） | 低，推荐 |
| `DontCare` | 不加载，Tile 内容未定义（适合完全覆写的情况） | 最低 |

| StoreAction | 含义 | 代价 |
|---|---|---|
| `Store` | 把 Tile Memory 的结果写回主存 | 有带宽开销（GPU → 主存） |
| `DontCare` | 不写回，结果丢弃（适合只是中间 Pass 的临时结果） | 最低 |
| `StoreAndResolve` | Store + MSAA Resolve | MSAA 时用 |

**实际应用规则**：

```
临时 RT（只在当前 Pass 里用，不传给后续 Pass）：
  LoadAction  = DontCare（不需要之前的内容）
  StoreAction = DontCare（结果不需要保留）

需要在 Pass 内容之上叠加绘制（Additive）：
  LoadAction  = Load（需要已有内容）
  StoreAction = Store

全屏覆写（Bloom、SSAO 等）：
  LoadAction  = DontCare
  StoreAction = Store（最终结果要保留）
```

### 设置 Cube Face 或 Array Slice

```csharp
// 渲染到 Cubemap 的某个面（用于实时 Reflection Probe 等）
cmd.SetRenderTarget(cubemapRT, mipLevel: 0, face: CubemapFace.PositiveX);

// 渲染到 Texture2DArray 的某一层
cmd.SetRenderTarget(arrayRT, mipLevel: 0, depthSlice: layerIndex);
```

### ClearRenderTarget 的时机

`cmd.ClearRenderTarget` 必须在 `cmd.SetRenderTarget` 之后调用：

```csharp
cmd.SetRenderTarget(colorRT, depthRT);
cmd.ClearRenderTarget(
    clearDepth: true,
    clearColor: true,
    backgroundColor: Color.black
);
```

在移动端，`ClearRenderTarget` 对应的 LoadAction 效果等同于 `Clear`，比先 Load 再在 GPU 上清除更高效（驱动可以在 Tile 开始时直接填充，不需要读主存）。

---

## DrawRenderers：渲染场景物体

`context.DrawRenderers` 是 SRP 里让场景中的 Renderer 参与渲染的核心方法。它不通过 `CommandBuffer`，而是直接调用 `ScriptableRenderContext`：

```csharp
context.DrawRenderers(
    cullingResults,
    ref drawingSettings,
    ref filteringSettings
);
```

### CullingResults 的来源

```csharp
// Execute 方法拿不到 context.Cull 的结果，要从 renderingData 取
var cullingResults = renderingData.cullResults;
```

URP 的 Culling 在主管线里已经执行过了，`renderingData.cullResults` 包含了当前 Camera 可见的所有 Renderer。自定义 Pass 直接复用这个结果，不需要（也不能在 Execute 里）重新 Cull。

### FilteringSettings：哪些物体参与渲染

```csharp
// 只渲染不透明物体（RenderQueue 0–2500）
var filterSettings = new FilteringSettings(RenderQueueRange.opaque);

// 只渲染半透明物体（RenderQueue 2501–5000）
var filterSettings = new FilteringSettings(RenderQueueRange.transparent);

// 自定义 RenderQueue 范围
var filterSettings = new FilteringSettings(new RenderQueueRange(2000, 2500));

// 加上 LayerMask 过滤
filterSettings.layerMask = LayerMask.GetMask("Player", "Enemy");

// 加上 RenderingLayerMask（URP 独有，不依赖 GameObject Layer）
filterSettings.renderingLayerMask = (uint)(1 << 2); // 第 3 个 RenderingLayer
```

**LayerMask vs RenderingLayerMask 的区别**：

- `LayerMask`：对应 GameObject 的 Layer，最多 32 个，是 Unity 引擎层面的概念
- `RenderingLayerMask`：URP / HDRP 引入的纯渲染概念，也是 32 bit，可以独立于物理 Layer 使用。适合"只在某些摄像机下可见""只投射/接收特定阴影"这类渲染规则

### DrawingSettings：用哪个 Shader Pass 渲染

```csharp
// 指定 Shader Tag ID：只渲染包含 "UniversalForward" Pass 的 Renderer
var shaderTagId = new ShaderTagId("UniversalForward");

// SortingSettings 控制绘制顺序
var sortingSettings = new SortingSettings(camera)
{
    criteria = SortingCriteria.CommonOpaque  // 前到后（减少 OverDraw）
    // 或 SortingCriteria.CommonTransparent  // 后到前（半透明正确混合）
};

var drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);

// 如果 Renderer 需要匹配多个 Pass 类型（比如兼容 Built-in Shader）
drawingSettings.SetShaderPassName(1, new ShaderTagId("ForwardBase"));
drawingSettings.SetShaderPassName(2, new ShaderTagId("SRPDefaultUnlit"));
```

URP 内置的常用 ShaderTagId：

| ShaderTagId | 对应场景 |
|---|---|
| `"UniversalForward"` | URP Forward 路径的主 Pass |
| `"UniversalForwardOnly"` | 仅 Forward（Deferred 路径下不参与 GBuffer） |
| `"SRPDefaultUnlit"` | 无光照 Pass（粒子、UI 等） |
| `"DepthOnly"` | Depth Prepass |
| `"DepthNormals"` | 同时写深度和法线（SSAO 用） |
| `"ShadowCaster"` | Shadow Map 生成 |

### 完整的 DrawRenderers 示例：渲染特定物体到自定义 RT

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cmd = CommandBufferPool.Get("CustomObjectPass");

    // 设置目标 RT
    cmd.SetRenderTarget(
        m_CustomRT,
        RenderBufferLoadAction.DontCare,
        RenderBufferStoreAction.Store,
        m_CustomDepth,
        RenderBufferLoadAction.DontCare,
        RenderBufferStoreAction.DontCare
    );
    cmd.ClearRenderTarget(true, true, Color.clear);

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    // 渲染目标 Layer 上的不透明物体
    var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
    {
        criteria = SortingCriteria.CommonOpaque
    };
    var drawingSettings = new DrawingSettings(
        new ShaderTagId("UniversalForward"), sortingSettings
    );
    var filterSettings = new FilteringSettings(RenderQueueRange.opaque)
    {
        layerMask = m_TargetLayerMask
    };

    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filterSettings);

    CommandBufferPool.Release(cmd);
}
```

注意 `cmd.Clear()` 和 `context.ExecuteCommandBuffer(cmd)` 的搭配：先提交 SetRenderTarget + Clear 命令（这两个操作必须通过 CommandBuffer），再直接调用 `context.DrawRenderers`（DrawRenderers 不通过 CommandBuffer）。

### RendererList：RenderGraph 时代的 DrawRenderers 替代

在 RenderGraph API 里，`DrawRenderers` 对应的是 `RendererList`：

```csharp
// 声明阶段：创建 RendererList
var listDesc = new RendererListDesc(
    new ShaderTagId[] { new ShaderTagId("UniversalForward") },
    renderingData.cullResults,
    renderingData.cameraData.camera
)
{
    renderQueueRange = RenderQueueRange.opaque,
    layerMask = m_TargetLayerMask,
    sortingCriteria = SortingCriteria.CommonOpaque
};
var rendererListHandle = renderGraph.CreateRendererList(listDesc);

// 执行阶段：
ctx.cmd.DrawRendererList(rendererListHandle);
```

---

## CommandBufferPool：正确的生命周期

`CommandBufferPool` 管理 CommandBuffer 对象池，避免每帧 GC：

```csharp
// 从池里取（有名字便于 Frame Debugger 识别）
var cmd = CommandBufferPool.Get("MyPassName");

// ... 填充命令 ...

context.ExecuteCommandBuffer(cmd);

// 归还到池
CommandBufferPool.Release(cmd);
// Release 内部会调用 cmd.Clear()，不需要手动 Clear
```

**常见错误**：

```csharp
// ❌ 取了但忘了 Release（慢慢积累，不会立即崩溃）
var cmd = CommandBufferPool.Get();
// ... 忘了 Release

// ❌ Execute 之后没有 Release 就 Clear 再 Execute（重复提交 Clear 后的空 Buffer）
context.ExecuteCommandBuffer(cmd);
cmd.Clear();
context.ExecuteCommandBuffer(cmd); // 这次提交是空的，浪费 API 调用

// ✅ 标准模式
var cmd = CommandBufferPool.Get("Name");
// 填命令
context.ExecuteCommandBuffer(cmd);
CommandBufferPool.Release(cmd);
```

---

## 小结

| 操作 | 推荐写法 | 主要陷阱 |
|---|---|---|
| 全屏 Blit | `Blitter.BlitCameraTexture` | UV 翻转；不能同张 RT 同时读写 |
| 设置 RT | `cmd.SetRenderTarget` + LoadAction/StoreAction | 移动端 Load 有带宽开销；Clear 先于 Load 更高效 |
| 渲染场景物体 | `context.DrawRenderers` + FilteringSettings | ShaderTagId 不匹配会静默不渲染；DrawRenderers 不通过 CommandBuffer |
| CommandBuffer 生命周期 | `CommandBufferPool.Get` / `Release` | 取了不释放；Execute 后不要 Clear 再 Execute |

下一篇（URP前-02）深入 RenderTexture 和 RTHandle 的完整生命周期，以及 `ReAllocateIfNeeded` 背后的尺寸管理体系。
