+++
title = "URP 深度扩展 01｜Renderer Feature 完整开发：从零写一个 ScriptableRendererFeature"
slug = "urp-ext-01-renderer-feature"
date = 2026-03-25
description = "系统讲解 ScriptableRendererFeature + ScriptableRenderPass 的完整开发流程：类职责划分、RenderPassEvent 时机选择、临时 RT 的申请与释放、常见陷阱。以一个灰度后处理为例，写出一个生产可用的 Renderer Feature。基于 Unity 2022.3 LTS（URP 14）。"
[taxonomies]
tags = ["Unity", "URP", "Renderer Feature", "ScriptableRenderPass", "渲染管线", "扩展开发"]
series = ["URP 深度"]
[extra]
weight = 1590
+++

前三层（前置基础、Pipeline 配置、光照阴影）讲的是 URP 已经提供了什么、每个参数背后是什么行为。扩展开发层要解决的问题是：**当 URP 内置功能不满足需求时，怎么往管线里插入自己的渲染逻辑**。

Renderer Feature 是 URP 给出的标准扩展点。这篇从零写一个完整的 Renderer Feature，把架构讲清楚。

> 版本说明：本篇基于 Unity 2022.3 LTS（URP 14），使用 `Execute()` API。Unity 6（URP 17）引入了 `RecordRenderGraph()` 作为推荐路径，差异在 URP扩展-02 专篇讲解。

---

## 整体架构：两个类，两层职责

一个 Renderer Feature 由两个类组成：

```
ScriptableRendererFeature        ← 配置层：在 Inspector 里暴露参数，管理 Pass 的生命周期
    └── ScriptableRenderPass     ← 执行层：在某个渲染时机插入 GPU 指令
```

**ScriptableRendererFeature** 负责：
- 在 Inspector 里序列化配置
- 创建和持有 Pass 实例
- 把 Pass 注册给 Renderer

**ScriptableRenderPass** 负责：
- 在指定时机被 Renderer 调用
- 设置 RenderTarget
- 录制 CommandBuffer 发给 GPU

两者分离的原因：Feature 的生命周期跟随 Renderer Asset（编辑器持久），Pass 的执行每帧发生一次或多次（根据相机数量）。如果把配置和执行混在一起，相机数量变化时状态管理会非常混乱。

---

## ScriptableRendererFeature：三个必须理解的方法

```csharp
public class MyFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;
        [Range(0f, 1f)] public float intensity = 1f;
    }

    public Settings settings = new Settings();
    private MyRenderPass _pass;

    // 1. Create()：Feature 被创建或 Settings 变更时调用
    //    做且只做：初始化 Pass 实例
    public override void Create()
    {
        _pass = new MyRenderPass(settings);
        // 不要在这里访问 RenderingData，此时还没有渲染帧信息
    }

    // 2. AddRenderPasses()：每帧每个相机调用一次
    //    做且只做：决定是否把 Pass 加入队列
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null) return;

        // 注意：2022.3 里在这里设置 Pass 的 renderPassEvent
        _pass.renderPassEvent = settings.renderPassEvent;
        renderer.EnqueuePass(_pass);
    }

    // 3. SetupRenderPasses()：URP 14+ 新增，AddRenderPasses 之后调用
    //    专门用来访问 renderer.cameraColorTargetHandle 等资源
    //    在 2022.3 LTS 之前用 AddRenderPasses 代替即可
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _pass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }
}
```

**常见错误**：在 `Create()` 里访问 `renderer.cameraColorTargetHandle`。此时 RT 还未分配，会得到错误的 Handle。必须在 `SetupRenderPasses()` 里拿。

---

## RenderPassEvent：插入时机选择

`renderPassEvent` 决定 Pass 在一帧中的执行位置。URP 把一帧的渲染分成若干阶段，每个阶段前后都有对应的事件点：

```
BeforeRendering                   帧最开始，几乎什么都没画
BeforeRenderingShadows            阴影 Map 绘制前
AfterRenderingShadows             ← 可以在这里读/写 Shadow Map
BeforeRenderingPrePasses          Depth Prepass / GBuffer 填充前
AfterRenderingPrePasses           ← Depth / Normal 已经写好，可以读
BeforeRenderingGbuffer            Deferred 路径专用
AfterRenderingGbuffer
BeforeRenderingDeferredLights
AfterRenderingDeferredLights
BeforeRenderingOpaques            ← 不透明物体绘制前（少用）
AfterRenderingOpaques             ← 不透明物体画完，天空盒还没
BeforeRenderingSkybox
AfterRenderingSkybox              ← 天空盒画完，半透明还没开始
BeforeRenderingTransparents
AfterRenderingTransparents        ← 所有几何体画完，后处理还没开始（最常用）
BeforeRenderingPostProcessing
AfterRenderingPostProcessing      ← URP 内置后处理完成后
AfterRendering                    帧结束
```

**选择依据**：

| 需求 | 推荐事件点 |
|------|-----------|
| 需要读深度图（如自定义描边、软粒子） | `AfterRenderingPrePasses` 之后 |
| 需要读不透明物体的颜色（如折射、扭曲） | `AfterRenderingOpaques` 之后 |
| 全屏后处理（读相机颜色，写回） | `AfterRenderingTransparents` |
| 在 URP 后处理之后叠加效果 | `AfterRenderingPostProcessing` |
| UI 层之上叠加（如扫描线、噪声） | `AfterRendering` |

**陷阱**：`AfterRendering` 在部分平台上 RT 已经 Resolve，再写入可能失效。叠加在 UI 上方的效果建议用 Overlay Camera 而不是在这个阶段插入。

---

## ScriptableRenderPass：完整实现

```csharp
public class MyRenderPass : ScriptableRenderPass, System.IDisposable
{
    private readonly MyFeature.Settings _settings;
    private RTHandle _cameraColorHandle;
    private RTHandle _tempRT;
    private static readonly int TempTexId = Shader.PropertyToID("_TempTex");

    public MyRenderPass(MyFeature.Settings settings)
    {
        _settings = settings;
        profilingSampler = new ProfilingSampler("MyFeature");
    }

    // Setup()：由 Feature.SetupRenderPasses() 调用
    // 把 cameraColorTargetHandle 传进来，避免在 Execute 时再去查
    public void Setup(RTHandle cameraColorTargetHandle)
    {
        _cameraColorHandle = cameraColorTargetHandle;
    }

    // Configure()：可选，在 Execute 前调用
    // 用于声明这个 Pass 要写入哪些 RT，让 URP 知道 Load/Store 行为
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // 申请临时 RT（与相机 RT 同规格）
        var desc = cameraTextureDescriptor;
        desc.depthBufferBits = 0; // 纯颜色 RT 不需要深度
        RenderingUtils.ReAllocateIfNeeded(
            ref _tempRT,
            desc,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            name: "_TempRT"
        );
    }

    // Execute()：核心执行，每帧每相机调用
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 不影响 Scene 视图（可选，按需去掉）
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;

        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, profilingSampler))
        {
            // 更新 Material 参数
            _settings.material.SetFloat("_Intensity", _settings.intensity);

            // 把相机颜色 Blit 到临时 RT（通过 Material 处理）
            Blitter.BlitCameraTexture(cmd, _cameraColorHandle, _tempRT, _settings.material, 0);

            // 把处理结果 Blit 回相机颜色 RT
            Blitter.BlitCameraTexture(cmd, _tempRT, _cameraColorHandle);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        _tempRT?.Release();
    }
}
```

---

## Blitter.BlitCameraTexture vs cmd.Blit

在 URP 12+ 里应该用 `Blitter.BlitCameraTexture` 而不是 `cmd.Blit`：

| | `cmd.Blit` | `Blitter.BlitCameraTexture` |
|---|---|---|
| RT 坐标系 | 可能发生 UV 翻转（Metal/Vulkan） | 自动处理翻转 |
| 输入参数 | `RenderTargetIdentifier` | `RTHandle` |
| Shader 输入名 | `_MainTex` | `_BlitTexture` |
| 推荐场景 | 传统 Built-in / 兼容写法 | URP 12+ 标准写法 |

Blitter 对应的 Shader 需要包含 URP 的 `Packages/com.unity.render-pipelines.universal/Shaders/Utils/Blitter.hlsl`，输入贴图名改为 `_BlitTexture`：

```hlsl
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blitter.hlsl"

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord;

    half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

    // 示例：灰度处理
    half gray = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
    color.rgb = lerp(color.rgb, gray.xxx, _Intensity);

    return color;
}
```

---

## 完整示例：灰度后处理 Feature

把上面的代码组合起来，完整的灰度效果 Feature 大约 120 行。几个关键组织原则：

**1. Settings 嵌套在 Feature 里**

```csharp
// Settings 不要单独放文件，嵌套在 Feature 里，Inspector 显示最干净
[System.Serializable]
public class Settings { ... }
public Settings settings = new Settings();
```

**2. Pass 实例只创建一次**

```csharp
// Create() 只在 Feature 初始化时调用，不要每帧 new Pass
public override void Create()
{
    _pass = new GrayscalePass(settings);
}
```

**3. 临时 RT 用 ReAllocateIfNeeded**

```csharp
// 不要每帧 GetTemporaryRT，用 ReAllocateIfNeeded 复用
RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, ...);
// 释放在 Dispose() 里，不要在 FrameCleanup() 里
```

**4. 在 Inspector 里禁用 Feature**

Renderer Feature 有内置的 `isActive` 字段，勾掉 Inspector 里的复选框即可禁用，不需要在代码里加额外判断。

---

## 生命周期总结

```
编辑器启动 / Renderer Asset 变更
    → Feature.Create()           创建 Pass 实例

每帧，每个相机
    → Feature.AddRenderPasses()  决定是否加入队列
    → Feature.SetupRenderPasses() 拿到 cameraColorTargetHandle
    → Pass.Configure()           声明 RT、申请临时资源
    → Pass.Execute()             录制 CommandBuffer
    → Pass.FrameCleanup()        每帧结束清理（通常为空）

Feature 被删除 / 退出
    → Feature.Dispose()          释放 Pass 持有的资源
```

`FrameCleanup()` 在早期版本里用来 `cmd.ReleaseTemporaryRT`，但在 URP 12+ 里推荐把 RT 的生命周期管理在 `Configure()`（申请）和 `Dispose()`（释放）里，`FrameCleanup` 通常可以不重写。

---

## 常见问题

**Q：Pass 里能用 `renderer.cameraColorTargetHandle` 吗？**

不推荐直接在 Pass 的 Execute 里调用。应该通过 `Setup()` 从 Feature 传入，时机更可控，也方便测试。

**Q：`Configure()` 里不申请 RT，直接在 `Execute()` 里用 `cmd.GetTemporaryRT` 行吗？**

技术上可以，但 URP 无法提前知道这个 Pass 需要哪些 RT，会影响 Native RenderPass 的合并优化（TBR 设备上性能较差）。推荐在 `Configure()` 里声明。

**Q：Feature 没有效果，但也没报错**

常见原因：
1. Material 为 null → `AddRenderPasses` 里的 null 检查提前返回
2. `renderPassEvent` 插在了不透明物体之前，但 RT 里还没有内容
3. Shader 里读的是 `_MainTex`，但 `Blitter` 传的是 `_BlitTexture`
4. URP Renderer Settings 里没有启用这个 Feature

**Q：在 Scene 视图也应用了效果怎么办**

```csharp
if (renderingData.cameraData.cameraType == CameraType.SceneView) return;
```

---

## 横向理解：Renderer Feature 和哪些东西相似

### CommandBuffer + Camera.AddCommandBuffer（Built-in 前身）

Built-in 管线里没有 Renderer Feature，用的是：

```csharp
// Built-in 写法
var cmd = new CommandBuffer { name = "MyEffect" };
cmd.Blit(BuiltinRenderTextureType.CameraTarget, tempRT, material);
cmd.Blit(tempRT, BuiltinRenderTextureType.CameraTarget);
camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cmd);
```

逻辑结构是一样的：选时机（`CameraEvent`）、录指令（`CommandBuffer`）、执行。

Renderer Feature 是这套机制的**正式化版本**：
- `CameraEvent` → `RenderPassEvent`（更细的粒度）
- 直接挂相机 → 配置在 Renderer Asset 上（解耦相机与效果）
- 全局生效 → 支持 Camera Stack 下的分层控制
- 手动管理生命周期 → `Create / AddRenderPasses / Dispose` 明确分工

从 Built-in 迁移过来时，把 `Camera.AddCommandBuffer` 里的逻辑搬到 `Execute()` 里，`CameraEvent` 对应换成 `RenderPassEvent`，基本可以直接迁过去。

---

### Graphics.DrawMesh（命令式 vs 声明式）

`Graphics.DrawMesh` 是另一种插入自定义绘制的方式：

```csharp
// 在 Update 或 OnRenderObject 里调用
Graphics.DrawMesh(mesh, matrix, material, layer);
```

和 Renderer Feature 的关键差异：

| | `Graphics.DrawMesh` | `Renderer Feature` |
|---|---|---|
| 执行时机 | 加入当前帧渲染队列，时机不可控 | 精确指定 `RenderPassEvent` |
| 渲染目标 | 当前相机的 RT | 可以写任意 RT |
| 深度/模板控制 | 受 Material 的 ZTest/Stencil 控制 | 可在 Configure 里完全自定义 |
| 典型用途 | 动态生成的 Mesh、程序化几何体 | 全屏效果、多 Pass 特效、后处理 |

**什么时候用 DrawMesh**：你想绘制的是一个"物体"（有具体的 Mesh 和空间位置），只是它不通过 GameObject 存在，比如程序化生成的草地、调试可视化、BillboardCloud。

**什么时候用 Renderer Feature**：你想在特定渲染阶段插入自定义逻辑，比如全屏 Blit、重绘一组物体、生成中间 RT 供后续 Pass 使用。

两者不是互斥的——Renderer Feature 内部可以调用 `DrawMesh`，在精确时机绘制特定几何体。

---

### Unreal 里的对应物

Unreal 里实现类似功能有两条路：

**Post Process Material（最接近）**

在 Post Process Volume 里挂一个 Material，可以读 `SceneColor`、`SceneDepth`，写全屏效果。对应 Renderer Feature 里 `AfterRenderingTransparents` 时机的全屏 Blit。配置直观，但自定义程度有限（只能全屏 Blit，不能多 Pass、不能控制 RT）。

**Scene View Extensions（`ISceneViewExtension`）**

Unreal 的引擎扩展接口，允许在渲染管线的特定阶段注入自定义 Pass，可以读写任意 RT，控制 RHI 指令。和 Renderer Feature 的设计目标最接近，但只能在引擎模块（C++）层使用，蓝图/插件开发触及不到。

总结对应关系：

| Unity URP | Unreal |
|-----------|--------|
| Renderer Feature（全屏后处理） | Post Process Material |
| Renderer Feature（自定义 Pass） | Scene View Extension（C++ 引擎层）|
| RenderPassEvent 时机 | `ERendererExtensionPriority` / Hook 点 |
| VolumeComponent 参数 | Post Process Volume 参数 |

---

## 小结

- `ScriptableRendererFeature`：配置层，管理 Pass 生命周期，每个 Feature 对应一个 Pass 实例
- `ScriptableRenderPass`：执行层，`Configure()` 声明资源，`Execute()` 录制指令
- `RenderPassEvent`：按需求选择插入时机，后处理效果首选 `AfterRenderingTransparents`
- 临时 RT 用 `RenderingUtils.ReAllocateIfNeeded`，在 `Dispose()` 里释放
- Blit 用 `Blitter.BlitCameraTexture`，对应 Shader 里贴图名是 `_BlitTexture`
- Renderer Feature 是 Built-in `Camera.AddCommandBuffer` 的正式化继承者；`Graphics.DrawMesh` 适合绘制几何体而非管线插入；Unreal 对应物是 Post Process Material（轻量）和 Scene View Extension（完整）

下一篇：URP扩展-02，切换到 Unity 6（URP 17），用 `RecordRenderGraph()` 写同样的效果，看看 RenderGraph 的资源管理和依赖声明与 Execute API 的区别在哪。
