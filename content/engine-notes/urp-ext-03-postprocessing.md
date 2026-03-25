+++
title = "URP 深度扩展 03｜URP 后处理扩展：Volume Framework + 自定义 VolumeComponent"
slug = "urp-ext-03-postprocessing"
date = 2026-03-25
description = "URP 后处理的完整扩展路径：Volume Framework 的运行机制、自定义 VolumeComponent 的参数声明、配套 Renderer Feature 的写法，以及 Override 优先级与 Blend 权重。用一个自定义色调映射效果串通整个流程。"
[taxonomies]
tags = ["Unity", "URP", "后处理", "Volume", "VolumeComponent", "Renderer Feature"]
series = ["URP 深度"]
[extra]
weight = 1610
+++

URP 的后处理不是一个黑盒，而是一套开放的扩展框架——**Volume Framework**。你可以用它定义自己的后处理效果，在 Inspector 里配置参数，并通过 Renderer Feature 在管线里执行。

这篇讲清楚 Volume Framework 的运行机制，以及如何在它的体系里插入自定义效果。

---

## Volume Framework 是什么

Volume Framework 解决的问题：**同一个场景里，不同区域有不同的后处理配置**。

比如：室外是高对比度的 Tonemapping，进入室内后平滑过渡到低饱和度的画面风格；角色靠近火焰时 Bloom 增强。这些需求靠全局参数做不到，Volume Framework 提供了一套按区域 + 权重混合的参数管理方案。

### 三个核心概念

**Volume**：挂在 GameObject 上的组件，持有一组 `VolumeComponent` 配置。

```
Volume 有两种模式：
- Global（isGlobal = true）：全场景生效，权重由 Priority 决定
- Local：有 Collider，相机进入范围后按距离 / Blend Distance 计算权重
```

**VolumeComponent**：一组参数的容器，继承自 `VolumeComponent`，每个参数用 `VolumeParameter<T>` 包装。

**VolumeProfile**：存在磁盘上的 Asset，里面包含一组 VolumeComponent 实例。Volume 组件引用 VolumeProfile。

### 运行时是怎么工作的

每帧，`VolumeManager` 根据相机位置：
1. 收集所有激活的 Volume（Global 全收集，Local 按距离过滤）
2. 按 Priority 排序
3. 把所有 VolumeComponent 参数按权重 Blend：低优先级 → 高优先级叠加
4. 最终结果写入 `VolumeStack`

Renderer Feature 从 `VolumeStack` 读取最终参数，执行渲染。

---

## 自定义 VolumeComponent

### 参数声明

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/My Tonemapping", typeof(UniversalRenderPipeline))]
public class MyTonemapping : VolumeComponent, IPostProcessComponent
{
    // VolumeParameter<T> 包装的参数，支持在 Volume 之间 Lerp
    public ClampedFloatParameter contrast = new ClampedFloatParameter(1f, 0f, 2f);
    public ClampedFloatParameter saturation = new ClampedFloatParameter(1f, 0f, 2f);
    public ColorParameter tint = new ColorParameter(Color.white, false, false, true);
    public BoolParameter enabled = new BoolParameter(false);

    // IPostProcessComponent 接口：告诉 URP 这个效果是否激活
    public bool IsActive() => enabled.value && contrast.value != 1f;
    public bool IsTileCompatible() => true; // 移动端 Tile 兼容
}
```

**VolumeParameter 常用类型**：

| 类型 | 用途 | 示例 |
|------|------|------|
| `FloatParameter` | 浮点数 | 强度、半径 |
| `ClampedFloatParameter` | 限制范围的浮点数 | 0~1 的强度 |
| `IntParameter` | 整数 | 采样次数 |
| `ColorParameter` | 颜色 | 色调、雾颜色 |
| `BoolParameter` | 开关 | 是否激活某功能 |
| `TextureParameter` | 纹理 | LUT 贴图 |
| `CubemapParameter` | Cubemap | 环境反射 |
| `Vector2Parameter` | 二维向量 | UV 偏移 |

### 参数的 Override 机制

每个 `VolumeParameter` 都有一个 `overrideState`，Inspector 里左侧的复选框就是它：

```csharp
// 只有 overrideState == true 的参数才会参与 Blend
// 没有勾选 Override 的参数使用默认值，不会被这个 Volume 影响
public ClampedFloatParameter contrast = new ClampedFloatParameter(1f, 0f, 2f);
// contrast.overrideState 默认为 false，需要在 Inspector 里勾选才生效
```

这意味着一个 Volume Profile 可以只覆盖部分参数，其余参数保留全局默认值，非常适合做局部差异化配置。

---

## 配套 Renderer Feature

有了 VolumeComponent，还需要对应的 Renderer Feature 来读取参数并执行效果：

```csharp
public class MyTonemappingFeature : ScriptableRendererFeature
{
    private MyTonemappingPass _pass;

    public override void Create()
    {
        _pass = new MyTonemappingPass(RenderPassEvent.AfterRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 从 VolumeStack 读取当前相机的参数
        var stack = VolumeManager.instance.stack;
        var component = stack.GetComponent<MyTonemapping>();

        // 没激活就不加入队列，不占用任何渲染资源
        if (!component.IsActive()) return;

        _pass.Setup(component, renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(_pass);
    }
}
```

```csharp
public class MyTonemappingPass : ScriptableRenderPass
{
    private MyTonemapping _component;
    private RTHandle _cameraColorHandle;
    private RTHandle _tempRT;
    private Material _material;

    private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int TintId = Shader.PropertyToID("_Tint");

    public MyTonemappingPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        // Material 引用来自 Resources 或注入，不要每帧 new
        _material = CoreUtils.CreateEngineMaterial("Custom/MyTonemapping");
    }

    public void Setup(MyTonemapping component, RTHandle cameraColorHandle)
    {
        _component = component;
        _cameraColorHandle = cameraColorHandle;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var desc = cameraTextureDescriptor;
        desc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, FilterMode.Bilinear, name: "_TonemappingTemp");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, profilingSampler))
        {
            // 把 VolumeComponent 的最终混合值传给 Shader
            _material.SetFloat(ContrastId, _component.contrast.value);
            _material.SetFloat(SaturationId, _component.saturation.value);
            _material.SetColor(TintId, _component.tint.value);

            Blitter.BlitCameraTexture(cmd, _cameraColorHandle, _tempRT, _material, 0);
            Blitter.BlitCameraTexture(cmd, _tempRT, _cameraColorHandle);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        _tempRT?.Release();
        CoreUtils.Destroy(_material);
    }
}
```

---

## 插入时机的选择

自定义后处理通常有两个时机选择：

**`AfterRenderingTransparents`（推荐）**

插在 URP 内置后处理之前。效果会参与 URP 的 TAA、Bloom 等处理，行为符合直觉。注意：如果 URP 的 Post Processing 是开着的，这个时机的 RT 还没有经过 Tonemapping，是线性空间（HDR）值。

**`AfterRenderingPostProcessing`**

插在 URP 内置后处理之后。RT 已经是 sRGB 空间的 LDR 值，直接叠加屏幕效果（UI 叠加、扫描线、噪声等）适合放在这里。

```csharp
// 根据效果类型选择
public class MyTonemappingFeature : ScriptableRendererFeature
{
    public RenderPassEvent insertionPoint = RenderPassEvent.AfterRenderingTransparents;
    // ...
}
```

---

## 全局 Volume vs Local Volume

**全局 Volume 的典型用法**：

```csharp
// 场景里放一个 Global Volume，挂默认 Profile
// 包含场景基础的 Tonemapping / Color Grading / AO 等配置
// Priority = 0（最低）
```

**Local Volume 的典型用法**：

```csharp
// 室内区域放一个 Local Volume，Blend Distance = 3f
// 只覆盖 Color Grading 参数（室内偏暖）
// Priority = 10（高于全局）
// 相机从室外进入时，3m 范围内平滑过渡
```

**Priority 规则**：数值越大，优先级越高，覆盖低优先级的参数。同 Priority 时，最后激活的 Volume 优先。

---

## 常见问题

**Q：Volume 的参数已经改了，但效果没变**

检查：
1. VolumeComponent 里对应参数的 `overrideState` 是否为 true（Inspector 里是否勾选了复选框）
2. `IsActive()` 返回值是否为 true
3. Renderer Feature 里是否正确读取了 `VolumeManager.instance.stack`

**Q：自定义效果在 Scene 视图不正常**

Scene 视图相机有自己的 Volume Stack。如果 Scene 视图没有对应的 Volume 配置，效果可能不如预期。在 Pass 的 Execute 里加判断：

```csharp
if (renderingData.cameraData.cameraType == CameraType.SceneView) return;
```

**Q：多个相机时，Volume 参数是否各自独立**

是的。每个相机有独立的 `VolumeStack`，根据各自的位置计算混合结果。Split Screen 场景下两个相机可以有完全不同的后处理参数。

**Q：Runtime 里动态改 Volume 参数**

```csharp
// 获取 Volume 组件，直接改参数
var volume = GetComponent<Volume>();
if (volume.profile.TryGet<MyTonemapping>(out var tonemapping))
{
    tonemapping.contrast.value = 1.5f;
    tonemapping.contrast.overrideState = true;
}
```

如果要做渐变，管理一个 Local Volume 的 Weight 更简洁：

```csharp
volume.weight = Mathf.Lerp(0f, 1f, t);
```

---

## 小结

- Volume Framework = 按区域 + 权重混合的参数管理体系，不是单纯的后处理系统
- 自定义效果 = `VolumeComponent`（参数定义）+ `ScriptableRendererFeature`（执行逻辑）
- 从 `VolumeManager.instance.stack.GetComponent<T>()` 读取最终混合值，在 `AddRenderPasses` 里判断是否激活
- 插入时机：HDR 空间处理用 `AfterRenderingTransparents`，屏幕叠加效果用 `AfterRenderingPostProcessing`
- Local Volume 的 Blend Distance 控制过渡平滑度，Priority 控制覆盖顺序

下一篇：URP扩展-04，DrawRenderers 与 FilteringSettings——在 Pass 里有选择地重绘某些物体（描边、X 光、自定义排序）。
