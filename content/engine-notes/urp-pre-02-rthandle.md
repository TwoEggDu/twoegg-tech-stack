+++
title = "URP 深度前置 02｜RenderTexture 与 RTHandle：临时 RT、RTHandle 体系"
slug = "urp-pre-02-rthandle"
date = 2026-03-25
description = "从 GetTemporaryRT 的旧模式讲到 RTHandle 体系：RTHandleSystem 如何统一管理 RT 的生命周期和分辨率缩放，ReAllocateIfNeeded 的正确用法，以及 Descriptor 各字段的实际含义。"
[taxonomies]
tags = ["Unity", "URP", "RenderTexture", "RTHandle", "渲染管线"]
series = ["URP 深度"]
[extra]
weight = 1510
+++

在 URP 里，"渲染目标的生命周期管理"是写自定义 Pass 时最容易出错的地方。这篇从头讲清楚 RenderTexture 是什么、旧的临时 RT 模式有什么问题，以及 RTHandle 体系如何解决这些问题。

---

## RenderTexture：显存里的一块画布

`RenderTexture` 是 Unity 对 GPU 渲染目标的封装——一块可以被 GPU 写入的显存区域。它可以作为：

- 相机的渲染目标（Camera 渲染到 RT 而不是屏幕）
- 后处理的中间缓冲
- 自定义 Pass 的输出（Shadow Map、深度图、ID 图等）

### 手动创建和释放

```csharp
// 创建：指定宽高、位深、格式
var rt = new RenderTexture(width, height, depthBits: 0, RenderTextureFormat.ARGB32);
rt.filterMode = FilterMode.Bilinear;
rt.wrapMode = TextureWrapMode.Clamp;
rt.Create(); // 可选：提前分配显存（否则在首次 SetRenderTarget 时延迟分配）

// 释放
rt.Release();     // 释放显存，保留 RenderTexture 对象（可以再次 Create）
Destroy(rt);      // 销毁对象（Unity Editor 里用 DestroyImmediate）
```

手动管理 RenderTexture 的问题：
- 每次分辨率变化（窗口缩放、Quality Settings 切换）需要手动检测并重建
- 跨 Pass 传递时，调用者和被调用者都要约定好 RT 的生命周期，容易忘释放
- 无法感知 DynamicResolution（动态分辨率）的缩放

---

## GetTemporaryRT：旧的临时 RT 模式

在 SRP 早期和 Built-in 管线时代，临时 RT 的标准写法是通过 `CommandBuffer` 申请和释放：

```csharp
// 申请临时 RT
int tempID = Shader.PropertyToID("_MyTempRT");
cmd.GetTemporaryRT(
    tempID,
    width, height,
    depthBuffer: 0,
    filter: FilterMode.Bilinear,
    format: RenderTextureFormat.ARGB32
);

// 使用
cmd.SetRenderTarget(tempID);
cmd.Blit(sourceID, tempID, material);

// 释放（必须，否则显存泄漏）
cmd.ReleaseTemporaryRT(tempID);
```

这个模式的工作机制：Unity 内部有一个临时 RT 池，`GetTemporaryRT` 从池里取（或新建），`ReleaseTemporaryRT` 归还。同一帧内尺寸相同的 RT 可以复用。

### 旧模式的问题

**问题一：每次分辨率变化都是新的 RT**

如果窗口大小变了，或者 DynamicResolution 调整了渲染分辨率，`GetTemporaryRT` 拿到的 RT 尺寸是调用时传入的固定值，不会自动更新。开发者必须在每一帧动态获取 `camera.pixelWidth` 和 `camera.pixelHeight`，手动传入。

**问题二：RTHandle 和旧 RT 的混用导致崩溃**

URP 2021 开始，内部 Pass 逐步迁移到 RTHandle API。如果自定义 Pass 仍然用 `RenderTargetIdentifier`（`GetTemporaryRT` 的产物）和 URP 内部的 RTHandle 混用，会在某些 URP 路径上触发 Assert 或渲染错误。

**问题三：生命周期追踪困难**

一个 Pass 里 `GetTemporaryRT`，另一个 Pass 里 `ReleaseTemporaryRT`，中间还要保证 int ID 对得上——容易出错，也难以 Review。

---

## RTHandle：统一管理的渲染目标

`RTHandle` 是 URP 2019+ 引入的 RT 封装，它解决了上面三个问题。

### RTHandle 是什么

`RTHandle` 不是 RenderTexture 的替代品，它是 RenderTexture 的**包装 + 引用计数 + 尺寸追踪**。

```
RTHandleSystem（单例）
  ├─ 持有一个"最大历史分辨率" maxWidth, maxHeight
  ├─ 每个 RTHandle 持有一个 RenderTexture
  │    ├─ 实际分辨率 = maxWidth, maxHeight（允许比 Camera 分辨率大）
  │    └─ 每帧通过 scaleRatio 缩放采样区域
  └─ 当 Camera 分辨率增大到超过历史最大值时，重新分配所有 RTHandle
```

**关键设计**：RTHandle 的实际 RT 尺寸不等于 Camera 分辨率——它保持历史最大分辨率，通过 `scaleRatio`（缩放比例）在 Shader 里决定实际使用的 UV 范围。这样可以在分辨率减小时复用 RT，只在分辨率增大时才真正重新分配。

### RTHandleSystem 的缩放机制

```csharp
// RTHandle 的 scaleRatio 属性：当前 Camera 分辨率 / RTHandle 实际分辨率
// 在 Shader 里取 UV 时要用这个缩放
float2 uv = input.texcoord * _RTHandleScale.xy;
// 或者用 URP 提供的宏：
float2 uv = input.texcoord * GetRTHandleScale(_MyRT);
```

这就是为什么 `Blitter.BlitCameraTexture` 比 `cmd.Blit` 更重要——前者会自动处理 `scaleRatio`，后者不会，导致在动态分辨率下出现画面被截断或拉伸。

---

## RTHandle 的分配方式

### 方式一：RTHandles.Alloc（持久 RT）

适合全局的 RT，比如 Shadow Map、GBuffer。

```csharp
// 按固定尺寸分配
RTHandle m_ShadowRT = RTHandles.Alloc(
    1024, 1024,
    depthBufferBits: DepthBits.Depth32,
    colorFormat: GraphicsFormat.None,
    name: "_ShadowMap"
);

// 按 Camera 分辨率的比例分配（DynamicResolution 友好）
RTHandle m_ColorRT = RTHandles.Alloc(
    Vector2.one,                         // scaleFunc: 1.0 = Camera 分辨率的 100%
    depthBufferBits: DepthBits.None,
    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
    name: "_MyColorRT"
);

// 释放（在 Dispose/OnDestroy 里调用）
m_ColorRT?.Release();
```

### 方式二：RenderingUtils.ReAllocateIfNeeded（自适应 RT）

这是 ScriptableRenderPass 里最常用的分配方式。它在以下情况下重新分配 RT：

- **首次调用**：m_RTHandle 为 null，直接分配
- **分辨率变化**：新的 Descriptor 描述的尺寸与当前 RT 不匹配
- **格式变化**：GraphicsFormat 或 depthBufferBits 不匹配

```csharp
private RTHandle m_TempRT;

public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
{
    // 从 Camera 的 Descriptor 派生，确保和 Camera RT 尺寸一致
    var descriptor = renderingData.cameraData.cameraTargetDescriptor;

    // 去掉深度，只要颜色
    descriptor.depthBufferBits = 0;

    // 只在需要时重新分配（尺寸或格式变化）
    RenderingUtils.ReAllocateIfNeeded(
        ref m_TempRT,
        descriptor,
        FilterMode.Bilinear,
        TextureWrapMode.Clamp,
        name: "_MyTempRT"
    );
}

public void Dispose()
{
    m_TempRT?.Release();
    m_TempRT = null;
}
```

### 方式三：CommandBuffer.GetTemporaryRT（不推荐，保留兼容）

仍然可用，但不能和 RTHandle API 混用（不能把 GetTemporaryRT 的 ID 传给需要 RTHandle 的地方）。

---

## RenderTextureDescriptor：RT 的完整描述

`RenderTextureDescriptor` 是 RT 的参数集合，URP 里最常见的来源是从 Camera 派生：

```csharp
var descriptor = renderingData.cameraData.cameraTargetDescriptor;
```

这个 Descriptor 包含了：

```csharp
descriptor.width           // 宽度（像素，考虑了动态分辨率）
descriptor.height          // 高度
descriptor.depthBufferBits // 深度缓冲位数（0, 16, 24, 32）
descriptor.colorFormat     // 旧的颜色格式 API（和 graphicsFormat 二选一）
descriptor.graphicsFormat  // 新的颜色格式（GraphicsFormat.R8G8B8A8_UNorm 等）
descriptor.msaaSamples     // MSAA 采样数（1, 2, 4, 8）
descriptor.dimension       // 维度（2D / Cube / 3D / 2DArray）
descriptor.volumeDepth     // 2DArray 的层数，或 3D 的深度
descriptor.useMipMap       // 是否生成 MipMap
descriptor.autoGenerateMips// 是否自动生成（写入后自动更新 Mip Chain）
descriptor.sRGB            // 是否是 sRGB 空间
descriptor.enableRandomWrite// 是否允许 Compute Shader 随机写入（UAV）
descriptor.memoryless      // 是否是 Memoryless RT（Metal/Vulkan 上可以不写回主存）
```

常见的修改模式：

```csharp
// 派生一个只有颜色、无深度、半分辨率的临时 RT
var desc = renderingData.cameraData.cameraTargetDescriptor;
desc.depthBufferBits = 0;
desc.width /= 2;
desc.height /= 2;
desc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat; // HDR
```

---

## Memoryless RT：移动端的零带宽临时缓冲

`descriptor.memoryless = RenderTextureMemoryless.Color` 告诉 GPU：这张 RT 不需要写回主存。

在 TBDR 架构（Mali、Adreno、Apple GPU）上，Tile Memory 里的数据在 Tile 处理完之后本可以直接丢弃——但如果 StoreAction = Store，GPU 会把 Tile 结果写回主存，产生带宽消耗。Memoryless RT 等效于告诉驱动"这张 RT 的生命周期不超过一次 Render Pass，无需写回"。

典型使用场景：
- Depth Buffer（用于深度测试，但最终不需要作为 Texture 采样）
- MSAA 的中间缓冲（Resolve 之后原始多采样数据不需要保留）
- GBuffer（Deferred 路径中，Lighting Pass 读完 GBuffer 后即丢弃）

```csharp
descriptor.memoryless = RenderTextureMemoryless.Depth;   // 深度 Memoryless
descriptor.memoryless = RenderTextureMemoryless.Color;   // 颜色 Memoryless
descriptor.memoryless = RenderTextureMemoryless.MSAA;    // MSAA Memoryless
// 可以组合
descriptor.memoryless = RenderTextureMemoryless.Depth | RenderTextureMemoryless.MSAA;
```

注意：Memoryless RT 在 PC 上不生效（不是 TBDR 架构，API 忽略），只在 iOS Metal 和支持的 Android 驱动上有效。

---

## RTHandle 在自定义 Pass 中的完整生命周期

```csharp
public class MyPass : ScriptableRenderPass
{
    private RTHandle m_TempRT;
    private static readonly int k_TempRTId = Shader.PropertyToID("_MyTempRT");

    public MyPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        profilingSampler = new ProfilingSampler("MyPass");
    }

    // ① 每帧：在这里分配或重分配 RT（分辨率可能已变化）
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, name: "_MyTempRT");

        // 告诉 URP 这个 Pass 会写入哪些 RT（用于 RenderGraph 兼容路径）
        ConfigureTarget(m_TempRT);
        ConfigureClear(ClearFlag.All, Color.black);
    }

    // ② 每帧：执行 GPU 命令
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, profilingSampler))
        {
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, source, m_TempRT, m_Material, 0);
            Blitter.BlitCameraTexture(cmd, m_TempRT, source);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    // ③ 每帧：清理（注意：不在这里 Release RT）
    public override void OnCameraCleanup(CommandBuffer cmd) { }

    // ④ Feature 被销毁时：真正释放 RT
    public void Dispose()
    {
        m_TempRT?.Release();
        m_TempRT = null;
    }
}
```

---

## 小结

| 方式 | 适用场景 | 生命周期管理 |
|---|---|---|
| `RenderTexture`（手动）| 持久存在、不随 Camera 分辨率变化 | 手动 Release/Destroy |
| `GetTemporaryRT`（旧）| 老项目兼容、简单临时 RT | 每帧 Get/Release，对称调用 |
| `RTHandle + ReAllocateIfNeeded`（推荐）| 所有新 URP Pass | OnCameraSetup 分配，Dispose 释放 |
| Memoryless RT | 移动端中间缓冲（Depth、MSAA、GBuffer）| 同 RTHandle，但标记 memoryless |

- **RTHandle 不等于每帧重新分配**：只在尺寸/格式变化时才真正分配，否则复用
- **Memoryless RT** 在 TBDR 上等效于"不写回主存"，减少带宽
- **Blitter 而不是 cmd.Blit** 的原因之一就是它感知 RTHandle 的 scaleRatio

下一篇（URP前-03）对比 Forward、Deferred、Forward+ 三条渲染路径的架构差异，以及在 URP 里选哪条路径、会影响哪些自定义 Pass 写法。
