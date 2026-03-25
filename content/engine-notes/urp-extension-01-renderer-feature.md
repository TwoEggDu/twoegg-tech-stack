+++
title = "URP 深度扩展 01｜Renderer Feature 完整开发：ScriptableRendererFeature + ScriptableRenderPass"
slug = "urp-extension-01-renderer-feature"
date = 2026-03-25
description = "从零写一个完整的 URP Renderer Feature：ScriptableRendererFeature 与 ScriptableRenderPass 的职责分工、生命周期、Pass Event 插入时机的选择依据，以及真实项目里的两个完整示例（全屏后处理 Blit、自定义物体描边）。"
[taxonomies]
tags = ["Unity", "URP", "Renderer Feature", "ScriptableRenderPass", "渲染管线", "扩展开发"]
series = ["URP 深度"]
[extra]
weight = 1590
+++

理解了 URP 的配置层和光照层之后，真正的扩展能力来自 **Renderer Feature**。它是 URP 留给开发者的标准接入点：在渲染管线的任意位置插入自定义 Pass，执行 Blit、DrawRenderers、Compute，或者向 RT 写入任何内容。

这篇从零写两个完整示例，把前面的概念落到代码层面。

---

## ScriptableRendererFeature 与 ScriptableRenderPass 的分工

URP 的扩展开发围绕两个类：

```
ScriptableRendererFeature   ← 管理层，负责创建和注册 Pass
ScriptableRenderPass        ← 执行层，负责实际的渲染指令
```

**ScriptableRendererFeature** 的职责：
- 在 Universal Renderer 的 Inspector 里作为一个条目出现
- 持有配置参数（序列化到 Renderer Asset）
- 创建 `ScriptableRenderPass` 实例
- 在每帧调用 `AddRenderPasses()` 把 Pass 注册进队列

**ScriptableRenderPass** 的职责：
- 声明自己在管线的哪个时机执行（`renderPassEvent`）
- 在 `Execute()` 里记录 CommandBuffer 指令
- 在 `Configure()` 里声明 RT 依赖（可选）

两者的关系：Feature 是工厂，Pass 是执行单元。一个 Feature 可以持有多个 Pass（比如先写 G-Buffer，再做后处理）。

---

## 生命周期

```
Editor 侧：
  Universal Renderer Asset 被加载
    → Feature.Create()                ← 初始化 Pass 实例，读取设置

每帧：
  URP 遍历所有启用的 Feature
    → Feature.AddRenderPasses(renderer, ref renderingData)
        → renderer.EnqueuePass(myPass) ← 注册 Pass（可以按条件跳过）

  URP 按 renderPassEvent 排序所有 Pass，依次执行：
    → Pass.OnCameraSetup(cmd, ref renderingData)   ← 可选，设置 RT
    → Pass.Execute(context, ref renderingData)     ← 主要逻辑
    → Pass.OnCameraCleanup(cmd)                    ← 可选，释放临时 RT
```

`Create()` 只调用一次（Asset 加载时）。`AddRenderPasses()` 每帧每个 Camera 调用一次——如果你有多个 Camera，同一个 Feature 会为每个 Camera 各调用一次。

---

## Pass Event：插入时机的选择

`renderPassEvent` 是 `RenderPassEvent` 枚举，决定 Pass 在管线中的插入位置。常用值：

```
BeforeRenderingPrePasses          ← 所有 Prepass 之前（最早）
AfterRenderingPrePasses           ← Depth Prepass 完成后
BeforeRenderingOpaques            ← 不透明渲染之前
AfterRenderingOpaques             ← 不透明渲染完成后（天空盒之前）
AfterRenderingSkybox              ← 天空盒完成后
BeforeRenderingTransparents       ← 半透明渲染之前
AfterRenderingTransparents        ← 半透明渲染完成后
BeforeRenderingPostProcessing     ← 后处理之前
AfterRenderingPostProcessing      ← 后处理完成后
AfterRendering                    ← 所有渲染完成后（最晚）
```

**选择依据**：

| 需求 | 推荐时机 |
|---|---|
| 全屏后处理（Blit 颜色 RT）| `BeforeRenderingPostProcessing` 或 `AfterRenderingPostProcessing` |
| 描边、X 光（需要深度）| `AfterRenderingOpaques` |
| 往 RT 预写数据（供后续 Pass 用）| `BeforeRenderingOpaques` |
| 在半透明物体上叠加效果 | `AfterRenderingTransparents` |
| 调试叠加层（最顶层）| `AfterRendering` |

同一时机下有多个 Pass 时，可以用 `renderPassEvent + offset`（整数偏移）精确控制顺序：

```csharp
renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1;
```

---

## 示例一：全屏灰度后处理

最简单的完整示例——把屏幕颜色 Blit 成灰度。

### Feature

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GrayscaleFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        [Range(0f, 1f)] public float intensity = 1f;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new();
    GrayscalePass _pass;

    public override void Create()
    {
        _pass = new GrayscalePass(settings);
        name = "Grayscale";
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 只在 Game View / Scene View 执行，跳过反射探针相机
        if (renderingData.cameraData.cameraType == CameraType.Reflection) return;
        if (settings.material == null) return;

        _pass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(_pass);
    }
}
```

### Pass

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrayscalePass : ScriptableRenderPass
{
    static readonly int s_IntensityId = Shader.PropertyToID("_Intensity");

    readonly GrayscaleFeature.Settings _settings;
    RTHandle _cameraColorHandle;
    RTHandle _tempRT;

    public GrayscalePass(GrayscaleFeature.Settings settings)
    {
        this.renderPassEvent = settings.passEvent;
        _settings = settings;
    }

    public void Setup(RTHandle cameraColorHandle)
    {
        _cameraColorHandle = cameraColorHandle;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // 分配一张和屏幕相同规格的临时 RT
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;  // 后处理不需要深度
        RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_GrayscaleTemp");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_settings.material == null) return;

        CommandBuffer cmd = CommandBufferPool.Get("Grayscale");

        _settings.material.SetFloat(s_IntensityId, _settings.intensity);

        // 把当前颜色 RT → 临时 RT（材质处理）
        Blitter.BlitCameraTexture(cmd, _cameraColorHandle, _tempRT, _settings.material, 0);
        // 结果写回颜色 RT
        Blitter.BlitCameraTexture(cmd, _tempRT, _cameraColorHandle);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandle 由 RenderingUtils.ReAllocateIfNeeded 管理，不需要手动释放
    }

    public void Dispose()
    {
        _tempRT?.Release();
    }
}
```

### Shader（灰度材质）

```hlsl
Shader "Custom/Grayscale"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                half gray = dot(color.rgb, half3(0.299, 0.587, 0.114));
                color.rgb = lerp(color.rgb, gray.rrr, _Intensity);
                return color;
            }
            ENDHLSL
        }
    }
}
```

**关键点**：URP 12+ 推荐用 `Blitter.BlitCameraTexture` 替代旧版 `cmd.Blit`，配合 `Blit.hlsl` 里的 `Vert` 和 `_BlitTexture`，不需要自己写全屏三角形。

---

## 示例二：X 光描边（仅对指定 Layer）

这个示例展示如何结合 `DrawRenderers` 在不透明渲染之后，对特定 Layer 的物体绘制描边效果（被遮挡时仍然可见）。

### Feature

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material outlineMaterial;
        public LayerMask layer = 0;
        public Color outlineColor = Color.white;
        [Range(0f, 5f)] public float outlineWidth = 2f;
    }

    public Settings settings = new();
    OutlinePass _pass;

    public override void Create()
    {
        _pass = new OutlinePass(settings)
        {
            // 在不透明之后、天空盒之前——深度已有，可以判断遮挡
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }
}
```

### Pass

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlinePass : ScriptableRenderPass
{
    static readonly int s_ColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int s_WidthId = Shader.PropertyToID("_OutlineWidth");

    readonly OutlineFeature.Settings _settings;
    FilteringSettings _filteringSettings;
    ShaderTagId _shaderTagId;

    public OutlinePass(OutlineFeature.Settings settings)
    {
        _settings = settings;
        // 只绘制 settings.layer 里的物体
        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.layer);
        // 使用 UniversalForward Pass（或你自定义的 Pass Tag）
        _shaderTagId = new ShaderTagId("UniversalForward");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_settings.outlineMaterial == null) return;

        CommandBuffer cmd = CommandBufferPool.Get("Outline");

        // 关闭深度写入，开启深度测试为 Always（X 光穿透效果）
        // 注意：这里通过材质设置控制，Pass 本身不直接设置渲染状态
        _settings.outlineMaterial.SetColor(s_ColorId, _settings.outlineColor);
        _settings.outlineMaterial.SetFloat(s_WidthId, _settings.outlineWidth);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 构造 DrawRenderers 参数
        var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
        var drawSettings = CreateDrawingSettings(
            _shaderTagId,
            ref renderingData,
            sortingCriteria
        );
        // 强制所有物体使用描边材质覆盖
        drawSettings.overrideMaterial = _settings.outlineMaterial;
        drawSettings.overrideMaterialPassIndex = 0;

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);

        CommandBufferPool.Release(cmd);
    }
}
```

`overrideMaterial` 是 DrawRenderers 的关键能力——不管物体原本用什么材质，强制用描边材质渲染。`filteringSettings` 的 Layer Mask 确保只处理指定 Layer 的物体。

---

## 常见错误与注意事项

### 1. Blit 时 RT 引用错误

不要持久缓存 `renderer.cameraColorTargetHandle`，这个值在不同帧、不同 Camera 之间可能会变。正确做法是在 `AddRenderPasses()` 调用 `_pass.Setup(renderer.cameraColorTargetHandle)` 每帧刷新。

### 2. 临时 RT 泄漏

用 `RenderingUtils.ReAllocateIfNeeded` 分配的 RTHandle，在 Feature 被销毁时需要在 `Dispose()` 里手动 `Release()`：

```csharp
protected override void Dispose(bool disposing)
{
    _pass.Dispose();
}
```

Feature 的 `Dispose()` 在 Renderer Asset 卸载或在 Editor 里修改设置时触发。

### 3. 多 Camera 重复执行

`AddRenderPasses()` 对每个 Camera 各调用一次。如果你的 Pass 代价高（比如 Compute Shader），需要加条件过滤：

```csharp
public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
{
    var camera = renderingData.cameraData.camera;
    // 只在主相机执行，跳过小地图相机、UI 相机等
    if (camera != Camera.main) return;
    renderer.EnqueuePass(_pass);
}
```

### 4. 旧版 cmd.Blit 不能用于 URP 的 RT

URP 的颜色 RT 在 Metal / Vulkan 下是 Texture Array（用于 XR）。`cmd.Blit(src, dst)` 无法正确处理这种情况，必须用 `Blitter.BlitCameraTexture`。

### 5. RenderGraph 兼容性

Unity 6（URP 17+）默认开启 RenderGraph。用上述 API 写的 Pass 在 RenderGraph 模式下通过 `UnsafePass` 包装执行，功能正常但无法享受 RenderGraph 的资源自动管理优化。RenderGraph 的完整写法见 URP扩展-02。

---

## 配置清单

写一个 Renderer Feature 的完整步骤：

```
1. 创建继承 ScriptableRendererFeature 的类
   - 序列化配置参数（[Serializable] Settings 内部类）
   - Create() 里实例化 Pass，赋值 renderPassEvent
   - AddRenderPasses() 里按条件调用 renderer.EnqueuePass()
   - Dispose() 里释放 Pass 持有的 RTHandle

2. 创建继承 ScriptableRenderPass 的类
   - 构造函数接收 Settings 引用
   - OnCameraSetup() 分配临时 RT（用 RenderingUtils.ReAllocateIfNeeded）
   - Execute() 里 Get CommandBuffer → 记录指令 → ExecuteCommandBuffer → Release
   - OnCameraCleanup() 做必要清理

3. 在 Universal Renderer Asset 的 Inspector 里
   - Add Renderer Feature → 选择你的 Feature 类
   - 配置参数，指定材质

4. 验证
   - Frame Debugger 检查 Pass 出现在正确位置
   - 多 Camera 场景下确认 Pass 只在目标 Camera 执行
```

---

## 小结

| 概念 | 要点 |
|---|---|
| ScriptableRendererFeature | 配置持有者 + Pass 注册器，`Create()` 一次，`AddRenderPasses()` 每帧每 Camera |
| ScriptableRenderPass | 渲染指令执行单元，`renderPassEvent` 决定插入位置 |
| RenderPassEvent | 枚举 + 整数偏移精确控制顺序；后处理用 `BeforeRenderingPostProcessing`，描边用 `AfterRenderingOpaques` |
| Blitter.BlitCameraTexture | URP 12+ 全屏 Blit 的正确方式，替代 cmd.Blit |
| overrideMaterial | DrawRenderers 强制覆盖材质，用于描边、X 光、自定义渲染层 |
| RTHandle 管理 | `RenderingUtils.ReAllocateIfNeeded` 分配，`Dispose()` 里 Release |

下一篇（URP扩展-02）讲 RenderGraph 实战：Pass 声明方式、资源 Handle 体系、Import/Export，以及如何把上面写的 Pass 迁移到 RenderGraph 模式。
+++
