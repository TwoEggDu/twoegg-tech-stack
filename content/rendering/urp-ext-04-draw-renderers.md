---
title: "URP 深度扩展 04｜DrawRenderers 与 FilteringSettings：有选择地重绘物体"
slug: "urp-ext-04-draw-renderers"
date: "2026-03-25"
description: "在 ScriptableRenderPass 里用 DrawRenderers 实现 X 光、描边、自定义排序等效果：FilteringSettings 的层级与 RenderQueue 过滤、DrawingSettings 的排序键与 Shader Pass 选择、SortingCriteria 控制渲染顺序，以及 X 光效果的完整实现示例。"
tags:
  - "Unity"
  - "URP"
  - "DrawRenderers"
  - "FilteringSettings"
  - "描边"
  - "X光"
  - "渲染管线"
series: "URP 深度"
weight: 1620
---
> **读这篇之前**：本篇会用到 DrawRenderers 和 CommandBuffer 基础操作。如果不熟悉，建议先看：
> - [URP 深度前置 01｜CommandBuffer：Blit、SetRenderTarget、DrawRenderers]({{< relref "rendering/urp-pre-01-commandbuffer.md" >}})
> - [URP 深度扩展 01｜Renderer Feature 完整开发]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})

Renderer Feature 里除了全屏 Blit，另一类常见需求是：**把场景里某些物体再画一遍**，但用不同的 Shader、不同的时机、不同的 RT。

X 光效果（穿墙显示角色）、描边（选中高亮）、自定义透明排序——这些都依赖同一个 API：`context.DrawRenderers()`。

---

## DrawRenderers 的三个参数

```csharp
context.DrawRenderers(
    cullingResults,     // 当前相机的剔除结果
    ref drawingSettings,    // 怎么画：排序、Shader Pass 选择
    ref filteringSettings   // 画哪些：Layer、RenderQueue、是否可见
);
```

三个参数各自控制一个维度，组合起来决定"哪些物体用什么方式重画"。

---

## CullingResults：复用相机的剔除结果

`CullingResults` 是 URP 在当前帧已经做过 Frustum Culling 之后的结果，包含相机视野内所有可见的 Renderer。

在 `Execute()` 里直接从 `RenderingData` 取：

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cullingResults = renderingData.cullResults;
    // ...
}
```

注意：`CullingResults` 只包含通过 Frustum Culling 的物体。如果物体在视野外，这里拿不到，也画不到。X 光效果如果要显示视野外的角色，需要额外的 `context.Cull()` 单独剔除。

---

## FilteringSettings：过滤哪些物体

```csharp
// 构造：指定 RenderQueue 范围
var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

// 常用 RenderQueue 范围
RenderQueueRange.opaque         // 0 ~ 2500（不透明）
RenderQueueRange.transparent    // 2501 ~ 5000（透明）
RenderQueueRange.all            // 0 ~ 5000（全部）
new RenderQueueRange(2000, 2500) // 自定义范围
```

**按 Layer 过滤**（最常用）：

```csharp
var filteringSettings = new FilteringSettings(RenderQueueRange.all)
{
    // Layer Mask：只画指定 Layer 的物体
    layerMask = LayerMask.GetMask("Character", "Equipment"),

    // 排除指定 Layer（取反）
    // layerMask = ~LayerMask.GetMask("UI")
};
```

**按 Rendering Layer Mask 过滤**（URP 14+）：

```csharp
// Rendering Layer 是 URP 自己的分层，独立于 GameObject 的 Layer
// 可以在 Renderer 组件上设置，比 Layer 更灵活
var filteringSettings = new FilteringSettings(RenderQueueRange.all)
{
    renderingLayerMask = 1u << 2  // Rendering Layer 2
};
```

---

## DrawingSettings：怎么画

```csharp
// 构造：指定排序方式 + ShaderTagId
var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
{
    criteria = SortingCriteria.CommonOpaque  // 不透明标准排序
};

var drawingSettings = new DrawingSettings(
    new ShaderTagId("UniversalForward"),  // 使用哪个 LightMode Pass
    sortingSettings
);
```

### ShaderTagId：选择哪个 Pass 来画

Shader 里每个 Pass 有 `Tags { "LightMode" = "..." }`，DrawRenderers 会用指定 LightMode 的 Pass 来渲染：

```
"UniversalForward"      → URP 主光照 Pass
"SRPDefaultUnlit"       → 无光照 Pass（自定义效果常用）
"DepthOnly"             → 只写深度
"DepthNormals"          → 写深度 + 法线
"Meta"                  → Lightmap 烘焙用
```

**自定义 LightMode**：

如果想让某些物体用专属的描边 Shader，在 Shader 里定义：

```hlsl
Pass
{
    Tags { "LightMode" = "OutlinePass" }
    // 描边逻辑...
}
```

DrawRenderers 时指定：

```csharp
var drawingSettings = new DrawingSettings(
    new ShaderTagId("OutlinePass"), sortingSettings
);
```

这样只有带 `OutlinePass` 的 Shader 才会被这次 DrawRenderers 调用到。

### SortingCriteria：控制渲染顺序

```csharp
SortingCriteria.CommonOpaque        // 前到后（减少 OverDraw）
SortingCriteria.CommonTransparent   // 后到前（透明正确混合）
SortingCriteria.BackToFront         // 强制后到前
SortingCriteria.QuantizedFrontToBack // 量化的前到后（性能更好）
SortingCriteria.None                // 不排序（按 CPU 提交顺序）
```

---

## 完整示例：X 光效果

X 光效果：角色被遮挡时，在遮挡物后面显示角色轮廓（半透明填充或描边）。

### 实现思路

```
正常渲染流程：角色被障碍物遮挡，深度测试失败，角色不可见

X 光 Pass（插在 AfterRenderingOpaques）：
1. 关闭深度写入，ZTest = Greater（只渲染被遮挡的部分）
2. 用特殊 Shader 绘制角色（半透明轮廓色）
3. 结果叠加在颜色 RT 上
```

### Feature 代码

```csharp
public class XRayFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public LayerMask characterLayer = 1 << 8;  // "Character" Layer
        public Material xrayMaterial;
        [ColorUsage(true, true)]
        public Color xrayColor = new Color(0f, 0.8f, 1f, 0.3f);
    }

    public Settings settings = new Settings();
    private XRayPass _pass;

    public override void Create()
    {
        _pass = new XRayPass(settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.xrayMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }
}
```

### Pass 代码

```csharp
public class XRayPass : ScriptableRenderPass
{
    private readonly XRayFeature.Settings _settings;
    private static readonly int XRayColorId = Shader.PropertyToID("_XRayColor");

    public XRayPass(XRayFeature.Settings settings)
    {
        _settings = settings;
        profilingSampler = new ProfilingSampler("XRay");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, profilingSampler))
        {
            // 把 XRay 颜色传给 Material
            _settings.xrayMaterial.SetColor(XRayColorId, _settings.xrayColor);

            // 提交已录制的命令，确保 RT 状态正确
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 排序：角色通常是不透明物体，用 CommonOpaque
            var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            // 使用自定义 LightMode "XRayPass"
            var drawingSettings = new DrawingSettings(
                new ShaderTagId("XRayPass"), sortingSettings)
            {
                overrideMaterial = _settings.xrayMaterial,          // 强制替换 Material
                overrideMaterialPassIndex = 0,
                perObjectData = PerObjectData.None                   // 不需要光照数据
            };

            // 只画 Character Layer
            var filteringSettings = new FilteringSettings(RenderQueueRange.all)
            {
                layerMask = _settings.characterLayer
            };

            context.DrawRenderers(
                renderingData.cullResults,
                ref drawingSettings,
                ref filteringSettings
            );
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
```

### X 光 Shader

```hlsl
Shader "Custom/XRay"
{
    Properties
    {
        _XRayColor ("XRay Color", Color) = (0, 0.8, 1, 0.3)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Tags { "LightMode" = "XRayPass" }

            // 关键：只渲染被遮挡的部分（深度大于当前深度缓冲的部分）
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _XRayColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _XRayColor;
            }
            ENDHLSL
        }
    }
}
```

---

## overrideMaterial vs 自定义 LightMode

两种方式都能让物体用指定 Shader 渲染，适用场景不同：

| | `overrideMaterial` | 自定义 `LightMode` |
|---|---|---|
| 原理 | 强制替换所有物体的 Material | 只触发有对应 LightMode Pass 的 Shader |
| 物体选择性 | 无法区分，所有过滤后的物体都替换 | 只有 Shader 里写了该 LightMode 的物体才响应 |
| 适用场景 | 全部替换（轮廓、阴影替代效果） | 只有特定物体需要（特定角色的发光效果） |

---

## 多 Pass 描边

描边效果通常需要两个 Pass：

```
Pass 1：沿法线方向把 Mesh 放大一点，写入背面（Cull Front），画纯色
Pass 2：正常绘制正面，覆盖 Pass 1 的中心区域，露出边缘描边
```

用 DrawRenderers 实现时，加两次 DrawRenderers 调用，或者在 Shader 里放两个 Pass（第一个 LightMode 为 `OutlineBack`，第二个为 `OutlineFront`），在 DrawingSettings 里分别引用。

```csharp
// 先画背面（放大的描边层）
var outlineSettings = new DrawingSettings(
    new ShaderTagId("OutlineBack"), sortingSettings);
context.DrawRenderers(cullingResults, ref outlineSettings, ref filteringSettings);

// 再画正面（正常颜色，覆盖中心）
var normalSettings = new DrawingSettings(
    new ShaderTagId("OutlineFront"), sortingSettings);
context.DrawRenderers(cullingResults, ref normalSettings, ref filteringSettings);
```

---

## 常见问题

**Q：DrawRenderers 画出来的物体不接受光照**

检查 `DrawingSettings` 里的 `perObjectData`：

```csharp
drawingSettings.perObjectData =
    PerObjectData.Lightmaps |
    PerObjectData.LightProbe |
    PerObjectData.OcclusionProbe;
```

默认是 `None`，需要显式声明需要哪些光照数据。

**Q：想用 overrideMaterial，但某些物体不想被覆盖**

`overrideMaterial` 无法按物体区分。改用自定义 LightMode：只在需要响应的物体 Shader 里加对应 Pass，其他 Shader 没有这个 Pass 就不会被渲染。

**Q：DrawRenderers 的结果在 Scene 视图里有错误**

Scene 视图的 `CullingResults` 和 Game 视图不同。如果 Pass 只需要在游戏运行时生效：

```csharp
if (renderingData.cameraData.cameraType == CameraType.SceneView) return;
```

---

## 导读

- 上一篇：[URP 深度扩展 03｜URP 后处理扩展：Volume Framework 与自定义效果]({{< relref "rendering/urp-ext-03-postprocessing.md" >}})
- 下一篇：[URP 深度扩展 05｜RenderDoc 调试 URP 自定义 Pass]({{< relref "rendering/urp-ext-05-renderdoc.md" >}})

---

## 小结

- `FilteringSettings`：控制画哪些物体——Layer、RenderQueue、Rendering Layer Mask
- `DrawingSettings`：控制怎么画——ShaderTagId 选 Pass、SortingCriteria 控顺序、overrideMaterial 强制替换
- `SortingCriteria`：不透明用 `CommonOpaque`（前到后），透明用 `CommonTransparent`（后到前）
- X 光效果核心：`ZTest Greater + ZWrite Off`，只渲染被遮挡的像素
- `overrideMaterial` 全量替换，自定义 LightMode 按 Shader 选择性响应——按需求选择

下一篇：URP扩展-05，RenderDoc 调试 URP 自定义 Pass——怎么捕获帧、查 RT 内容、定位 Pass 执行顺序问题。
