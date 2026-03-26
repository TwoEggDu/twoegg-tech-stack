---
title: "URP 深度扩展 06｜2022.3 → Unity 6 迁移指南：Breaking Change 与迁移策略"
slug: "urp-ext-06-migration"
date: "2026-03-25"
description: "从 Unity 2022.3 LTS（URP 14）升级到 Unity 6（URP 17）的实际变更清单：RenderGraph 强制化、ScriptableRenderPass API 变更、RenderingData 拆分、Shader API 差异、以及渐进式迁移策略。"
tags:
  - "Unity"
  - "URP"
  - "Unity 6"
  - "迁移"
  - "升级"
  - "渲染管线"
series: "URP 深度"
weight: 1580
---
Unity 2022.3 LTS 到 Unity 6 的 URP 升级不是简单的版本号跳跃，`ScriptableRenderPass` 的核心 API 发生了结构性变化。本篇梳理实际会遇到的 Breaking Change，以及一套不用一次性全改就能先让项目跑起来的迁移策略。

---

## 变更全景

| 类别 | 2022.3 LTS（URP 14） | Unity 6（URP 17）| 影响程度 |
|------|---------------------|-----------------|---------|
| Pass 执行入口 | `Execute()` | `RecordRenderGraph()` | ★★★ 必须处理 |
| 渲染数据结构 | `RenderingData` 整体传入 | 拆分为 `ContextContainer` 多个子结构 | ★★★ 必须处理 |
| 临时 RT 创建 | `cmd.GetTemporaryRT` | `renderGraph.CreateTexture` | ★★☆ 推荐改 |
| 渲染目标绑定 | `ConfigureTarget()` | `builder.SetRenderAttachment()` | ★★★ 必须处理 |
| Blit 方法 | `Blitter.BlitCameraTexture(cmd, ...)` | `Blitter.BlitTexture(ctx.cmd, ...)` | ★★☆ 参数调整 |
| VolumeStack 获取 | `VolumeManager.instance.stack` | 同，无变化 | ✅ 无需改 |
| RTHandle 体系 | 主要 RT 句柄 | 仍可用，但推荐换 `TextureHandle` | ★☆☆ 可选 |
| Shader 关键字 | 同 | 同 | ✅ 无需改 |

---

## 变更一：Execute → RecordRenderGraph（必须处理）

这是影响最大的变化。Unity 6 里，`Execute()` 方法仍然存在但被标记为过时，如果你的 Pass 只有 `Execute()` 没有 `RecordRenderGraph()`，Unity 6 会自动用 `AddUnsafePass` 包装它——能运行，但有 Warning，且无法享受 RenderGraph 的优化。

**最小改动迁移策略**：先加一个空的 `RecordRenderGraph()`，把原来 `Execute()` 里的逻辑搬到 `AddUnsafePass` 里，消除 Warning，然后再逐步改成 `AddRasterRenderPass`：

```csharp
// 第一步：加 RecordRenderGraph，用 AddUnsafePass 包装旧逻辑
// 这样可以先消除 Warning，功能不变
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    using (var builder = renderGraph.AddUnsafePass<PassData>("MyPass", out var passData))
    {
        // 填充 passData（见下一节）
        builder.AllowPassCulling(false);

        builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
        {
            // 把原来 Execute() 里的内容搬到这里
            // ctx.cmd 就是原来的 CommandBuffer
            var cmd = CommandBufferPool.Get();
            // ... 原来的逻辑 ...
            ctx.cmd.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        });
    }
}

// 第二步（之后）：改成 AddRasterRenderPass，享受 RenderGraph 优化
```

---

## 变更二：RenderingData 结构拆分（必须处理）

Unity 6 里，`RenderingData` 被拆分成多个独立结构，通过 `ContextContainer` 获取：

| 旧写法（2022.3） | 新写法（Unity 6）|
|----------------|----------------|
| `renderingData.cameraData.camera` | `frameData.Get<UniversalCameraData>().camera` |
| `renderingData.cameraData.renderer.cameraColorTargetHandle` | `frameData.Get<UniversalResourceData>().activeColorTexture` |
| `renderingData.cullResults` | `frameData.Get<UniversalRenderingData>().cullResults` |
| `renderingData.lightData` | `frameData.Get<UniversalLightData>()` |
| `renderingData.shadowData` | `frameData.Get<UniversalShadowData>()` |

**`AddRenderPasses` 里的 `RenderingData` 参数**：在 Unity 6 里这个参数仍然存在，但推荐在 `RecordRenderGraph` 里改用 `ContextContainer`。`AddRenderPasses` 里如果只用来判断是否 Enqueue，可以暂时不改。

---

## 变更三：ConfigureTarget 废弃（必须处理）

旧写法里，`Configure()` 方法里调用 `ConfigureTarget()` 声明渲染目标：

```csharp
// 旧写法（2022.3）
public override void Configure(CommandBuffer cmd, RenderTextureDescriptor desc)
{
    ConfigureTarget(_myRT);
    ConfigureClear(ClearFlag.Color, Color.clear);
}
```

新写法里，渲染目标在 `RecordRenderGraph` 的 Pass Builder 里声明：

```csharp
// 新写法（Unity 6）
builder.SetRenderAttachment(textureHandle, 0);
builder.SetRenderAttachmentDepth(depthHandle, AccessFlags.Write);
```

`Configure()` 方法在 Unity 6 里已废弃，和 `Execute()` 一样会被包进 `UnsafePass`。

---

## 变更四：临时 RT 创建方式

```csharp
// 旧写法（2022.3）：手动创建、手动释放
cmd.GetTemporaryRT(tempRTId, desc);
// ... 使用 ...
cmd.ReleaseTemporaryRT(tempRTId);

// 或者用 RTHandle
RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_TempRT");
// Dispose 里手动释放
_tempRT?.Release();

// 新写法（Unity 6）：RenderGraph 自动管理
var desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
TextureHandle temp = renderGraph.CreateTexture(desc);
// 不需要手动释放，Pass 结束后自动回收
```

---

## 变更五：Blitter API 参数调整

```csharp
// 旧写法（2022.3）
Blitter.BlitCameraTexture(cmd, source, destination, material, passIndex);
Blitter.BlitCameraTexture(cmd, source, destination); // 无 Material 版

// 新写法（Unity 6，RasterGraphContext 里）
Blitter.BlitTexture(ctx.cmd, source, new Vector4(1, 1, 0, 0), material, passIndex);
Blitter.BlitTexture(ctx.cmd, source, new Vector4(1, 1, 0, 0), passIndex); // 无 Material 版
```

`new Vector4(1, 1, 0, 0)` 是 `scaleBias` 参数（xy = scale，zw = bias），全屏 Blit 时用 `(1, 1, 0, 0)` 表示不缩放不偏移。

---

## 渐进式迁移策略

一次性把所有 Pass 改成 RenderGraph 写法风险高，推荐分三步：

### 第一步：升级能跑，消除红色 Error

升级 Unity 版本后，项目可能有编译错误（API 改名、命名空间变化）。先修编译错误，不管 Warning。这一步目标是"能进 Play 模式"。

常见编译错误：
- `RenderingData.cameraData.renderer` → `UniversalResourceData`（需要改获取方式）
- 某些 `UniversalRenderPipeline` 的静态方法已移除或改名

### 第二步：消除 Warning，包装旧逻辑

把所有只有 `Execute()` 的 Pass 都加上 `RecordRenderGraph()`，用 `AddUnsafePass` 包装旧逻辑：

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    using (var builder = renderGraph.AddUnsafePass<EmptyPassData>("Legacy_MyPass", out _))
    {
        builder.AllowPassCulling(false);
        builder.SetRenderFunc((EmptyPassData _, UnsafeGraphContext ctx) =>
        {
            // 原来 Execute() 里的内容
        });
    }
}
```

这一步完成后，项目可以正常运行且无 Warning，但还没有享受 RenderGraph 的优化。

### 第三步：逐 Pass 迁移到 AddRasterRenderPass

按重要性和频率排序，逐个把 `AddUnsafePass` 改成 `AddRasterRenderPass`。优先改调用频率高的 Pass（每帧执行的后处理 Pass），低频 Pass（初始化、烘焙辅助）可以留在 UnsafePass。

---

## 需要同步处理的 Shader 变化

URP 14 → URP 17 的 Shader 层变化相对少，但有几个需要注意：

**`TEXTURE2D_X` 宏**：URP 17 里部分内置贴图的采样宏从 `SAMPLE_TEXTURE2D` 改为 `SAMPLE_TEXTURE2D_X`（用于支持 XR 立体渲染）。如果你的自定义 Shader 采样 `_CameraOpaqueTexture` 或 `_CameraDepthTexture` 出现黑屏，检查是否需要用 `TEXTURE2D_X` 版本的宏。

**`_BlitTexture` 替代 `_MainTex`**：URP 17 的 `Blitter` 用 `_BlitTexture` 作为源贴图的属性名，如果你的全屏 Blit Shader 里用了 `_MainTex`，需要改为 `_BlitTexture`：

```hlsl
// 旧写法
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

// 新写法（URP 17 Blitter 规范）
TEXTURE2D_X(_BlitTexture); SAMPLER(sampler_BlitTexture);
float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
```

---

## 快速参考：新旧 API 对照表

```
Execute()                     → RecordRenderGraph()
Configure()                   → builder.SetRenderAttachment()
ConfigureTarget(_rt)          → builder.SetRenderAttachment(handle, 0)
ConfigureClear(...)           → builder.SetRenderAttachment(handle, 0, LoadAction.Clear)
renderingData.cameraData      → frameData.Get<UniversalCameraData>()
renderingData.cullResults     → frameData.Get<UniversalRenderingData>().cullResults
renderingData.lightData       → frameData.Get<UniversalLightData>()
cameraData.renderer.cameraColorTargetHandle  → resourceData.activeColorTexture
cmd.GetTemporaryRT(...)       → renderGraph.CreateTexture(...)
cmd.ReleaseTemporaryRT(...)   → （不需要，自动释放）
_tempRT?.Release()            → （不需要，自动释放）
Blitter.BlitCameraTexture(cmd, src, dst, mat, pass)
  → Blitter.BlitTexture(ctx.cmd, src, new Vector4(1,1,0,0), mat, pass)
```

---

## 小结

- 最大变化：`Execute()` + `Configure()` → `RecordRenderGraph()` + Builder 声明
- `RenderingData` 拆分：从整体参数改为 `frameData.Get<T>()` 按需获取
- 渐进式迁移：先跑起来 → 用 `AddUnsafePass` 消除 Warning → 再改 `AddRasterRenderPass`
- Shader 层：检查 `_MainTex` → `_BlitTexture`，`TEXTURE2D` → `TEXTURE2D_X`（采样相机 RT 时）
- 不需要一次性全改，`AddUnsafePass` 是合法的过渡方案，功能完全正确

---

**URP 深度系列（16 篇）全部完成。**

| 层 | 篇数 | 覆盖内容 |
|----|------|---------|
| 前置基础层 | 3 | CommandBuffer、RTHandle、渲染路径 |
| Pipeline 配置层 | 3 | Pipeline Asset、Renderer Settings、Camera Stack |
| 光照与阴影层 | 3 | 光照系统、Shadow 深度、SSAO |
| 扩展开发层 | 6 | Renderer Feature、RenderGraph（Unity 6）、后处理扩展、DrawRenderers、RenderDoc 调试、迁移指南 |
| 平台与优化层 | 2 | 移动端专项配置、三档质量分级 |
