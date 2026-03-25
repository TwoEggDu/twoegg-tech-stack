+++
title = "URP 深度扩展 02｜RenderGraph 实战：Unity 6 / URP 17 的新写法"
slug = "urp-ext-02-rendergraph"
date = 2026-03-25
description = "Unity 6（URP 17）将 RenderGraph 设为默认渲染框架。本篇讲清楚 RenderGraph 的核心概念（TextureHandle、ImportResource、UseTexture）、RecordRenderGraph 的完整写法，以及与旧版 Execute() API 的对比迁移——用同一个灰度效果示例，两版代码对比着看。"
[taxonomies]
tags = ["Unity", "URP", "RenderGraph", "Unity6", "渲染管线", "扩展开发"]
series = ["URP 深度"]
[extra]
weight = 1600
+++

上一篇用 `Execute()` API 在 Unity 2022.3 LTS 里写了一个完整的 Renderer Feature。这篇切换到 Unity 6（URP 17），用同样的灰度效果演示 RenderGraph 写法，重点讲清楚两个问题：**RenderGraph 的核心概念是什么**，以及**和旧写法相比改了哪些地方**。

> 版本说明：本篇基于 Unity 6（URP 17）。代码在 Unity 2022.3 里无法直接运行——RenderGraph API 在 URP 14 里是实验性的，接口与 URP 17 有差异。

---

## 为什么 Unity 6 要推 RenderGraph

旧的 `Execute()` 模式有一个根本性问题：**Renderer 不知道 Pass 之间的资源依赖关系**。

每个 Pass 自己申请 RT，自己释放，Renderer 无法提前知道：
- 哪些 RT 在 Pass 执行后就不需要了（无法自动释放）
- 哪些相邻 Pass 可以合并成一个 Native RenderPass（TBR 关键优化）
- 哪些 Pass 根本没有下游消费者（可以被剔除）

RenderGraph 解决方案：**先声明，再执行**。

每个 Pass 在执行前，必须明确告诉 RenderGraph：
- 我会读哪些纹理（`UseTexture(handle, AccessFlags.Read)`）
- 我会写哪些纹理（`UseTexture(handle, AccessFlags.Write)`）

RenderGraph 拿到所有 Pass 的依赖声明后，可以：
1. 自动判断哪些 Pass 是死代码（输出没人读），直接剔除
2. 自动推断 Load/Store Action，减少 TBR 的带宽消耗
3. 自动管理临时 RT 的生命周期，Frame 结束后自动回收

---

## 核心概念：三个你必须理解的东西

### 1. TextureHandle：RenderGraph 管理的纹理句柄

RenderGraph 里的纹理不是 `RTHandle`，而是 `TextureHandle`。它是一个轻量级引用，背后的实际内存由 RenderGraph 统一管理。

```csharp
// 旧写法：直接持有 RTHandle
private RTHandle _tempRT;

// RenderGraph 写法：每帧通过 graph.CreateTransientTexture() 创建句柄
// 不需要自己管理生命周期，RenderGraph 自动回收
TextureHandle tempHandle = graph.CreateTransientTexture(desc);
```

`TextureHandle` 分两类：
- **Transient（临时）**：在 Pass 录制阶段创建，只在当前帧内有效，RenderGraph 自动管理内存
- **Imported（外部导入）**：把 RTHandle 导入 RenderGraph，用于相机 RT 等需要跨帧保留的纹理

### 2. ImportResource：把外部 RT 带进 RenderGraph

相机颜色 RT 是 URP 管理的，不是 RenderGraph 创建的。要在 RenderGraph 里读写它，必须先导入：

```csharp
// 在 RecordRenderGraph() 里
TextureHandle cameraColor = renderGraph.ImportTexture(cameraColorHandle);
```

导入后就变成 `TextureHandle`，可以传给 Pass 的 PassData 使用。

### 3. PassData：Pass 的数据容器

RenderGraph 的 Pass 数据必须通过 `PassData` 结构传递，不能直接捕获外部变量（闭包），因为录制和执行是分离的两个阶段：

```csharp
// 录制阶段：声明 PassData，填入数据
using (var builder = renderGraph.AddRasterRenderPass<MyPassData>("MyPass", out var passData))
{
    passData.sourceTexture = cameraColor;    // ← 存句柄
    passData.material = _material;           // ← 存 Material 引用

    builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
    builder.SetRenderAttachment(outputHandle, 0, AccessFlags.Write);

    builder.SetRenderFunc((MyPassData data, RasterGraphContext context) =>
    {
        // 执行阶段：通过 data 访问数据，不能捕获外部变量
        Blitter.BlitTexture(context.cmd, data.sourceTexture, ...);
    });
}
```

---

## RecordRenderGraph：完整写法

把扩展-01 的灰度效果，用 RenderGraph 重写：

**Feature 层（变化不大）**

```csharp
public class GrayscaleFeatureV2 : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;
        [Range(0f, 1f)] public float intensity = 1f;
    }

    public Settings settings = new Settings();
    private GrayscalePassV2 _pass;

    public override void Create()
    {
        _pass = new GrayscalePassV2(settings);
        _pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null) return;
        renderer.EnqueuePass(_pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _pass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        // RenderGraph 管理的资源不需要在这里手动释放
    }
}
```

**Pass 层（核心变化）**

```csharp
public class GrayscalePassV2 : ScriptableRenderPass
{
    // PassData：必须是 class，RenderGraph 会管理它的内存
    private class PassData
    {
        public TextureHandle sourceTexture;
        public Material material;
        public float intensity;
    }

    private readonly GrayscaleFeatureV2.Settings _settings;
    private RTHandle _cameraColorHandle;

    public GrayscalePassV2(GrayscaleFeatureV2.Settings settings)
    {
        _settings = settings;
        profilingSampler = new ProfilingSampler("GrayscaleV2");
    }

    public void Setup(RTHandle cameraColorHandle)
    {
        _cameraColorHandle = cameraColorHandle;
    }

    // ★ 新方法：替代旧版 Execute()
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        // 1. 导入相机 RT，进入 RenderGraph 的依赖跟踪体系
        TextureHandle cameraColor = resourceData.activeColorTexture;

        // 2. 创建临时 RT（Transient，RenderGraph 自动管理生命周期）
        var desc = renderGraph.GetTextureDesc(cameraColor);
        desc.name = "_TempTex";
        desc.clearBuffer = false;
        TextureHandle tempTexture = renderGraph.CreateTransientTexture(desc);

        // === Pass 1：相机颜色 → 临时 RT（通过 Material 处理）===
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale_Blit", out var passData))
        {
            passData.sourceTexture = cameraColor;
            passData.material = _settings.material;
            passData.intensity = _settings.intensity;

            // 声明：我要读 cameraColor
            builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
            // 声明：我要写 tempTexture
            builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                data.material.SetFloat("_Intensity", data.intensity);
                Blitter.BlitTexture(ctx.cmd, data.sourceTexture,
                    new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // === Pass 2：临时 RT → 相机颜色（Copy Back）===
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale_CopyBack", out var passData))
        {
            passData.sourceTexture = tempTexture;

            builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
            builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.sourceTexture,
                    new Vector4(1, 1, 0, 0), 0, false);
            });
        }
    }

    // 旧版 Execute() 不再需要，但为了兼容 2022.3，可以保留一个空实现
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
}
```

---

## 两版写法对比

| 对比项 | Execute API（2022.3 LTS） | RecordRenderGraph（Unity 6） |
|--------|--------------------------|------------------------------|
| 纹理类型 | `RTHandle` | `TextureHandle` |
| 临时 RT 申请 | `RenderingUtils.ReAllocateIfNeeded` | `renderGraph.CreateTransientTexture` |
| 临时 RT 释放 | `Dispose()` 手动释放 | RenderGraph 自动管理 |
| 资源依赖声明 | 无（隐式） | 显式（`UseTexture` / `SetRenderAttachment`） |
| Pass 数据传递 | 直接访问成员变量 | 通过 `PassData` 结构体传递 |
| 相机 RT 获取 | `renderer.cameraColorTargetHandle` | `resourceData.activeColorTexture` |
| TBR 优化潜力 | 依赖手动配置 Load/Store | RenderGraph 自动推断 |

---

## 过渡期：Unity 6 里旧代码怎么运行

Unity 6 没有直接删除 `Execute()` API。旧版 Pass 在 Unity 6 里会被自动包成 **UnsafePass** 执行：

```
// 控制台 Warning 示例
Pass 'MyRenderPass' is using the old Execute API.
Consider migrating to RecordRenderGraph for better performance.
```

UnsafePass 可以正常运行，但：
- 无法享受 RenderGraph 的自动 Load/Store 优化
- RenderGraph 的依赖裁剪对它无效
- 在 Render Graph Viewer 里会显示为黑盒

迁移建议：如果项目在 Unity 6 上跑，高频调用的 Feature 值得迁移；边缘功能的 Feature 可以暂时保留 Execute API。

---

## Render Graph Viewer：调试工具

Unity 6 的 **Window → Analysis → Render Graph Viewer** 可以查看当前帧所有 Pass 的资源流向：

- 绿色节点：正常 Pass
- 橙色节点：UnsafePass（旧版 Execute API）
- 灰色节点：被裁剪的 Pass（输出无人读取）
- 连线：纹理读写依赖关系

调试 RenderGraph 问题时，先打开这个工具，确认 Pass 是否被正确识别、依赖关系是否正确连接。

---

## 小结

- RenderGraph 的核心理念：**先声明依赖，再执行**，让 Renderer 统一管理资源生命周期
- `TextureHandle`：RenderGraph 管理的纹理引用，分 Transient（临时）和 Imported（外部导入）两种
- `RecordRenderGraph()`：新的 Pass 入口，通过 `AddRasterRenderPass` 录制，通过 `PassData` 传数据
- 旧版 `Execute()` 在 Unity 6 里以 UnsafePass 形式兼容运行，功能正常但不推荐
- 迁移不是必须立刻做的——按项目实际情况和 Unity 版本决定

下一篇：URP扩展-03，Volume Framework + 自定义 VolumeComponent + 后处理写法。
