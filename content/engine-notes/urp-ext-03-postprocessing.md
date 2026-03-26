---
title: "URP 深度扩展 03｜URP 后处理扩展：Volume Framework 与自定义效果"
slug: "urp-ext-03-postprocessing"
date: "2026-03-25"
description: "URP 后处理扩展的完整链路：Volume Framework 的参数暴露机制、自定义 VolumeComponent、在 Renderer Feature 里读取 Volume 参数驱动效果，以及 VolumeProfile 的运行时混合和区域触发。"
tags:
  - "Unity"
  - "URP"
  - "后处理"
  - "Volume Framework"
  - "Renderer Feature"
  - "渲染管线"
series: "URP 深度"
weight: 1550
---
URP 的后处理不是一个独立系统，而是 Volume Framework + Renderer Feature 两层配合的结果。理解这两层的分工，才能写出既能在 Inspector 里调参、又能在 Renderer Feature 里驱动效果的完整后处理扩展。

---

## Volume Framework 的两层分工

**Volume（场景层）**：挂在 GameObject 上的触发区域，携带一个 `VolumeProfile`，里面存着一组后处理参数。

**VolumeComponent（参数层）**：继承自 `VolumeComponent` 的数据类，定义具体的参数（强度、颜色、范围等），支持多 Volume 之间的权重混合。

**Renderer Feature（执行层）**：在渲染管线里读取当前混合好的 VolumeComponent 参数，执行实际的 Shader 效果。

三者的关系：Volume 负责"在哪里生效、权重多少"，VolumeComponent 负责"有哪些参数"，Renderer Feature 负责"怎么渲染"。

---

## 自定义 VolumeComponent

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Custom/Vignette Extra")]
public class VignetteExtra : VolumeComponent, IPostProcessComponent
{
    // VolumeParameter<T> 支持多 Volume 间插值混合
    public ClampedFloatParameter Intensity   = new ClampedFloatParameter(0f, 0f, 1f);
    public ColorParameter         Color       = new ColorParameter(Color.black, false, false, true);
    public ClampedFloatParameter Smoothness  = new ClampedFloatParameter(0.5f, 0f, 1f);
    public BoolParameter          Rounded     = new BoolParameter(false);

    // IPostProcessComponent 接口：告诉 URP 这个组件是否激活
    public bool IsActive() => Intensity.value > 0f;

    // 编辑器模式下是否生效（Scene 视图预览）
    public bool IsTileCompatible() => false;
}
```

**`VolumeComponentMenu` 特性**：决定这个组件在 `Add Override` 菜单里的路径，格式是 `"分类/名称"`。

**`VolumeParameter<T>` 类型**：URP 内置了常用的参数类型，支持按 Volume 权重插值：

| 类型 | 说明 |
|------|------|
| `FloatParameter` | 浮点，无范围限制 |
| `ClampedFloatParameter` | 浮点，有 min/max |
| `IntParameter` | 整型 |
| `BoolParameter` | 布尔（不插值，直接覆盖）|
| `ColorParameter` | 颜色，支持 HDR |
| `TextureParameter` | 贴图引用 |
| `Vector2Parameter` / `Vector4Parameter` | 向量 |

**`IPostProcessComponent`**：这个接口是可选的，但建议实现。URP 在每帧会查询 `IsActive()`，返回 false 时 Renderer Feature 可以跳过该 Pass，避免无效开销。

---

## 在 Renderer Feature 里读取 Volume 参数

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteExtraFeature : ScriptableRendererFeature
{
    private VignetteExtraPass _pass;
    private Material _material;

    public override void Create()
    {
        // 用 CoreUtils 创建 Material，避免泄漏
        _material = CoreUtils.CreateEngineMaterial("Hidden/Custom/VignetteExtra");
        _pass = new VignetteExtraPass(_material)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 从 VolumeStack 获取当前混合好的参数
        var stack = VolumeManager.instance.stack;
        var comp = stack.GetComponent<VignetteExtra>();

        // 组件不存在或未激活，跳过
        if (comp == null || !comp.IsActive()) return;

        _pass.Setup(comp);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }

    // -------------------------------------------------------
    private class VignetteExtraPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private VignetteExtra _comp;

        // Shader property IDs（避免每帧字符串查找）
        private static readonly int IntensityId  = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int ColorId      = Shader.PropertyToID("_VignetteColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_VignetteSmoothness");

        public VignetteExtraPass(Material material) => _material = material;

        public void Setup(VignetteExtra comp) => _comp = comp;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("VignetteExtra");

            // 把 Volume 混合好的参数设置到 Material
            _material.SetFloat(IntensityId, _comp.Intensity.value);
            _material.SetColor(ColorId, _comp.Color.value);
            _material.SetFloat(SmoothnessId, _comp.Smoothness.value);

            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, source, source, _material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
```

**`VolumeManager.instance.stack.GetComponent<T>()`**：这是获取当前相机位置混合好的 Volume 参数的标准方式。URP 会根据相机位置和各 Volume 的权重/范围自动计算混合结果，这里直接读最终值即可。

---

## Volume 的混合机制

### Global vs Local Volume

**Global Volume**：没有 Collider，全场景生效，权重由 `Weight` 参数控制（0~1）。通常用作全局基准后处理配置。

**Local Volume**：需要挂 Collider（设为 Trigger），相机进入区域时生效，可以设置 `Blend Distance` 控制边界过渡距离。

多个 Volume 叠加时，按 Priority（数值越高优先级越高）排序，参数按权重插值混合。

### 运行时动态修改 Volume 参数

```csharp
// 获取场景里的 Volume 组件
var volume = GetComponent<Volume>();

// 方法 1：直接修改 Profile（影响所有使用这个 Profile 的 Volume）
var vignette = volume.profile.TryGet<VignetteExtra>(out var comp);
comp.Intensity.value = 0.5f;

// 方法 2：使用独立 Profile（只影响这一个 Volume，推荐）
volume.profile = Instantiate(volume.profile); // 复制一份
volume.profile.TryGet<VignetteExtra>(out comp);
comp.Intensity.value = 0.5f;
```

直接修改共享 Profile 会影响所有使用这个 Profile 的 Volume，运行时调整一般应先 `Instantiate` 复制一份。

### 代码触发区域后处理（淡入淡出）

```csharp
// 用 DOTween 或 Coroutine 做 Volume 权重过渡
IEnumerator FadeInEffect(Volume volume, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        volume.weight = Mathf.Lerp(0f, 1f, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }
    volume.weight = 1f;
}
```

---

## 常见问题

### Volume 参数改了但效果没变

检查顺序：
1. `VolumeLayer` 是否正确——Camera 组件的 `Volume Mask` 和 Volume 所在 Layer 必须匹配
2. `IsActive()` 返回是否为 true（Intensity 是否 > 0）
3. Renderer Feature 的 `AddRenderPasses` 里是否正确 Enqueue 了 Pass

### 编辑器 Scene 视图里看不到效果

Scene 视图有独立的相机，默认不使用 Post Processing。在 Scene 视图的 `Gizmos` 菜单里开启 `Post Processing` 选项，或者给 Scene Camera 加 `VolumeProfile`。

### Shader 里读不到 Volume 的贴图参数

`TextureParameter` 设置给 Material 时用 `material.SetTexture()`，注意贴图为 null 时要有默认值兜底，否则 Shader 采样 null 贴图会报错：

```csharp
var tex = _comp.LutTexture.value;
_material.SetTexture(LutTexId, tex != null ? tex : Texture2D.whiteTexture);
```

---

## 小结

- Volume Framework 的分工：Volume 控制区域和权重，VolumeComponent 存参数，Renderer Feature 执行效果
- `VolumeParameter<T>` 支持多 Volume 间权重插值，`IPostProcessComponent.IsActive()` 控制 Pass 是否入队
- `VolumeManager.instance.stack.GetComponent<T>()` 获取当前混合好的参数，在 `AddRenderPasses` 里读取
- Global Volume 全局基准，Local Volume + BlendDistance 做区域过渡
- 运行时修改 Profile 参数先 `Instantiate` 复制，避免影响共享资源

下一篇：URP扩展-04，DrawRenderers 与 FilteringSettings——在特定条件下重绘一组物体，实现 X 光透视、描边、自定义排序等效果。
