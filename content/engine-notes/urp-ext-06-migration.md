+++
title = "URP 深度扩展 06｜2022.3 → Unity 6 迁移指南：Breaking Change 清单与迁移策略"
slug = "urp-ext-06-migration"
date = 2026-03-25
description = "URP 从 Unity 2022.3 LTS（URP 14）升级到 Unity 6（URP 17）的完整迁移指南：API Breaking Change 逐条说明、ScriptableRenderPass Execute→RecordRenderGraph 迁移流程、RTHandle→TextureHandle 替换规则、已删除 API 的替代方案，以及分阶段迁移策略。"
[taxonomies]
tags = ["Unity", "URP", "Unity6", "迁移", "渲染管线", "升级"]
series = ["URP 深度"]
[extra]
weight = 1640
+++

Unity 6 发布后，很多团队面临迁移决策。这篇梳理 URP 14（Unity 2022.3 LTS）到 URP 17（Unity 6）的**渲染相关 Breaking Change**，重点讲 Renderer Feature 开发者实际会撞到的问题，以及对应的处理方式。

> 说明：本篇聚焦渲染管线扩展开发层的变化（Renderer Feature / RenderPass）。项目全量升级还涉及物理、动画、UI 等系统，本篇不覆盖。

---

## 变化总览

Unity 6 对 URP 的改动可以分三类：

**1. 推荐路径变更（有兼容层，旧代码能跑）**
- `Execute()` → `RecordRenderGraph()`
- `RenderTargetIdentifier` → `RTHandle` → `TextureHandle`

**2. API 调整（需要修改）**
- `SetupRenderPasses()` 签名变化
- 部分 `RenderingData` 成员被废弃
- `Blitter` API 小幅调整

**3. 行为变更（不改代码也会影响结果）**
- RenderGraph 默认开启后，Pass 执行顺序和资源管理逻辑变化
- Native RenderPass 合并更激进，错误使用 LoadAction 会更明显地影响性能

---

## Breaking Change 逐条

### 1. ScriptableRenderPass.Execute() 变为 UnsafePass

**变化**：`Execute()` 仍然有效，但在 Unity 6 里被包成 `UnsafePass` 执行，控制台出现 Warning。

```
[Warning] Pass 'MyPass' uses the obsolete Execute API.
Migrate to RecordRenderGraph for best performance.
```

**影响**：功能正常，但不享受 RenderGraph 的自动 Load/Store 优化和依赖裁剪。

**处理方式**：
- 短期：可以用 `ConfigureInput` + `requiresDepthTexture` 等标记抑制 Warning，继续用 Execute
- 长期：迁移到 `RecordRenderGraph()`（见后文）

---

### 2. SetupRenderPasses() 签名变化

**2022.3（URP 14）**：
```csharp
public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
```

**Unity 6（URP 17）**：
```csharp
// RenderingData 参数被废弃，改用 FrameData
public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
// 同时新增：
public override void SetupRenderPasses(ScriptableRenderer renderer, ContextContainer frameData)
```

**处理方式**：Unity 6 对旧签名保留兼容，但推荐切换到 `ContextContainer` 版本：

```csharp
// Unity 6 推荐写法
public override void SetupRenderPasses(ScriptableRenderer renderer, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();
    _pass.Setup(resourceData.activeColorTexture);
}
```

---

### 3. RenderingData 部分成员废弃

Unity 6 把 `RenderingData` 里的数据逐步迁移到 `ContextContainer` 的各个 FrameData 容器里：

| 旧写法（2022.3） | 新写法（Unity 6） |
|----------------|----------------|
| `renderingData.cameraData.camera` | `frameData.Get<UniversalCameraData>().camera` |
| `renderingData.cameraData.cameraType` | `frameData.Get<UniversalCameraData>().cameraType` |
| `renderingData.cullResults` | `frameData.Get<UniversalRenderingData>().cullResults` |
| `renderingData.lightData` | `frameData.Get<UniversalLightData>()` |
| `renderingData.shadowData` | `frameData.Get<UniversalShadowData>()` |

**处理方式**：`RenderingData` 本身在 Unity 6 里未被删除，旧代码编译通过，只是部分成员标了 `[Obsolete]`。迁移时按需替换。

---

### 4. RTHandle 在 RenderGraph 路径下无法直接使用

在 `RecordRenderGraph()` 里，不能直接把 `RTHandle` 传给 Pass 的 PassData，必须先用 `ImportTexture` 转换：

```csharp
// ❌ 错误：不能把 RTHandle 直接放进 PassData
passData.texture = _myRTHandle;  // TextureHandle 类型，但传了 RTHandle

// ✅ 正确：先 Import，得到 TextureHandle
TextureHandle handle = renderGraph.ImportTexture(_myRTHandle);
passData.texture = handle;
```

**处理方式**：凡是 `RTHandle` 类型的外部资源，在 `RecordRenderGraph()` 里都需要先 `ImportTexture`。

---

### 5. cmd.Blit 在 RenderGraph Pass 里不可用

在 `RecordRenderGraph()` 的 `SetRenderFunc` 里，只能使用 `RasterCommandBuffer`，而 `cmd.Blit` 需要 `CommandBuffer`：

```csharp
// ❌ 在 SetRenderFunc 里不能用 cmd.Blit
builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
{
    ctx.cmd.Blit(src, dst);  // 编译报错：RasterCommandBuffer 没有 Blit 方法
});

// ✅ 改用 Blitter.BlitTexture
builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
{
    Blitter.BlitTexture(ctx.cmd, data.sourceHandle, new Vector4(1,1,0,0), 0, false);
});
```

---

### 6. VolumeManager API 微调

`VolumeManager.instance.stack` 在 Unity 6 里被标为过时，推荐通过相机获取：

```csharp
// 2022.3 写法
var stack = VolumeManager.instance.stack;
var component = stack.GetComponent<MyEffect>();

// Unity 6 推荐写法（在 AddRenderPasses 里）
var stack = VolumeManager.instance.GetStack(renderingData.cameraData.camera);
var component = stack.GetComponent<MyEffect>();
```

多相机场景下，新写法能正确获取每个相机各自的 Volume Stack，行为更准确。

---

## Execute → RecordRenderGraph 迁移流程

以扩展-01 的灰度效果为例，从 Execute API 迁移到 RecordRenderGraph：

### Step 1：添加 RecordRenderGraph 方法

```csharp
// 保留旧的 Execute 作为后备（Unity 6 在 RenderGraph 禁用时走这里）
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    // 原有实现保留
}

// 新增 RecordRenderGraph
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    // 新实现
}
```

### Step 2：迁移资源获取

```csharp
// 旧：从 SetupRenderPasses 传入 RTHandle
private RTHandle _cameraColorHandle;
public void Setup(RTHandle handle) { _cameraColorHandle = handle; }

// 新：在 RecordRenderGraph 里直接从 frameData 获取
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();
    TextureHandle cameraColor = resourceData.activeColorTexture;
    // ...
}
```

### Step 3：迁移临时 RT

```csharp
// 旧：RTHandle + ReAllocateIfNeeded
private RTHandle _tempRT;
RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, ...);

// 新：CreateTransientTexture，无需手动管理生命周期
var desc = renderGraph.GetTextureDesc(cameraColor);
TextureHandle tempHandle = renderGraph.CreateTransientTexture(desc);
// 不需要 Dispose，RenderGraph 自动回收
```

### Step 4：迁移 Execute 逻辑到 SetRenderFunc

```csharp
// 旧
public override void Execute(...)
{
    var cmd = CommandBufferPool.Get();
    _material.SetFloat("_Intensity", _intensity);
    Blitter.BlitCameraTexture(cmd, _cameraColorHandle, _tempRT, _material, 0);
    Blitter.BlitCameraTexture(cmd, _tempRT, _cameraColorHandle);
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
}

// 新
using (var builder = renderGraph.AddRasterRenderPass<PassData>("Grayscale", out var passData))
{
    passData.source = cameraColor;
    passData.material = _material;
    passData.intensity = _intensity;

    builder.UseTexture(passData.source, AccessFlags.Read);
    builder.SetRenderAttachment(tempHandle, 0, AccessFlags.Write);

    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
    {
        data.material.SetFloat("_Intensity", data.intensity);
        Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1,1,0,0), data.material, 0);
    });
}
```

---

## 分阶段迁移策略

项目升级不需要一次迁移所有 Renderer Feature。推荐分三阶段：

### 阶段一：升级到 Unity 6，暂不迁移 API

- 升级 Unity 版本
- 旧的 `Execute()` Pass 以 UnsafePass 形式运行
- 修复编译错误（主要是 `SetupRenderPasses` 签名、废弃 API）
- 验证渲染结果正确

**目标**：项目在 Unity 6 里能正常运行，不追求 RenderGraph 优化。

### 阶段二：高频 Pass 迁移 RecordRenderGraph

- 识别每帧必定执行的 Pass（主后处理、全局 AO、描边）
- 逐个迁移到 `RecordRenderGraph()`
- 用 Render Graph Viewer 验证依赖关系正确

**目标**：核心渲染路径享受 RenderGraph 的 Load/Store 自动优化，减少 TBR 带宽消耗。

### 阶段三：清理 UnsafePass

- 迁移剩余低频或边缘 Feature
- 移除旧版 `Execute()` 实现
- 清理 `Dispose()` 里手动管理的 `RTHandle`（迁移后改由 RenderGraph 管理）

**目标**：代码库统一使用 RenderGraph API，减少维护负担。

---

## 不值得迁移的情况

有些情况可以长期保留 `Execute()` API：

- **编辑器工具 Feature**：只在编辑器里用，每帧不一定触发，性能不敏感
- **项目不打算升级 Unity 6**：2022.3 LTS 维护到 2025 年底，如果项目生命周期在此之前结束，没有升级必要
- **第三方插件**：等插件官方更新，不要自己 Fork 改

---

## 小结

| 变化 | 影响 | 处理方式 |
|------|------|---------|
| `Execute()` 变 UnsafePass | Warning，功能正常 | 逐步迁移 `RecordRenderGraph` |
| `SetupRenderPasses` 签名 | 旧签名兼容，新签名推荐 | 按需切换 |
| `RenderingData` 成员废弃 | `[Obsolete]` 警告 | 替换为 `ContextContainer` 写法 |
| `RTHandle` 需 Import | RenderGraph Pass 内编译错误 | `renderGraph.ImportTexture()` |
| `cmd.Blit` 不可用 | `RasterCommandBuffer` 无 Blit | 改用 `Blitter.BlitTexture` |
| `VolumeManager.stack` 废弃 | 多相机下行为差异 | 改用相机关联的 Stack 获取 |

扩展开发层（6 篇）到这里全部完成。下一层是**平台与优化层**：URP平台-01 移动端专项配置，URP平台-02 多平台质量分级。
