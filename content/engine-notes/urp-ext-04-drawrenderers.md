+++
title = "URP 深度扩展 04｜DrawRenderers 与 FilteringSettings：条件重绘物体"
slug = "urp-ext-04-drawrenderers"
date = 2026-03-25
description = "ScriptableRenderContext.DrawRenderers 是 URP 里重绘一组物体的核心 API。本篇讲 CullingResults 的来源、FilteringSettings 的过滤条件（Layer、RenderQueue、ShaderTagId）、DrawingSettings 的排序和 Pass 控制，以及 X 光透视、描边、自定义排序三个典型用法。"
[taxonomies]
tags = ["Unity", "URP", "DrawRenderers", "Renderer Feature", "渲染管线", "描边", "X光"]
series = ["URP 深度"]
[extra]
weight = 1560
+++

`DrawRenderers` 是 URP 里"重绘一组物体"的核心手段——不是 Blit 全屏，而是真正地重新渲染特定物体，可以换 Material、换 Pass、换渲染目标。X 光透视、描边、自定义排序渲染这三类效果都建立在这个 API 上。

---

## 核心 API 结构

```csharp
context.DrawRenderers(
    cullingResults,     // 当前帧的可见物体列表
    ref drawingSettings,    // 怎么画（Pass 选择、排序、Override Material）
    ref filteringSettings   // 画哪些（Layer、RenderQueue、ShaderTagId）
);
```

三个参数各司其职，分开理解。

---

## CullingResults：可见物体从哪来

`CullingResults` 是当前帧相机裁剪后的可见物体列表，由 URP 管线在 Pass 执行前已经计算好，通过 `renderingData.cullResults` 获取：

```csharp
public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
{
    var cullResults = renderingData.cullResults;
    // ...
}
```

**不能自己重新 Cull**：`DrawRenderers` 只能从已有的 `CullingResults` 里过滤，不能扩展可见范围。如果想画 Frustum 外的物体，需要用独立相机的 `camera.TryGetCullingParameters` + `context.Cull()` 得到新的 CullingResults，但这个开销较大，非必要不用。

---

## FilteringSettings：画哪些物体

```csharp
// 创建过滤条件
var filteringSettings = new FilteringSettings(
    renderQueueRange,   // RenderQueue 范围
    layerMask           // Layer 过滤
);

// 也可以用预设
var opaqueFilter   = FilteringSettings.defaultValue;            // 所有不透明物体
var transparentFilter = new FilteringSettings(RenderQueueRange.transparent);
```

### 按 RenderQueue 过滤

```csharp
// 只画不透明物体
var filter = new FilteringSettings(RenderQueueRange.opaque);

// 只画透明物体
var filter = new FilteringSettings(RenderQueueRange.transparent);

// 自定义范围（如只画 Queue 2000~2500）
var filter = new FilteringSettings(new RenderQueueRange(2000, 2500));
```

### 按 Layer 过滤

```csharp
// 只画 "XRay" Layer 的物体
int xrayLayer = LayerMask.GetMask("XRay");
var filter = new FilteringSettings(RenderQueueRange.opaque, xrayLayer);
```

### 按 ShaderTagId 过滤

`ShaderTagId` 对应 Shader 里 `Pass` 的 `LightMode` Tag，可以精确指定只执行哪个 Pass：

```csharp
// Shader 里：
// Tags { "LightMode" = "UniversalForward" }

// C# 里过滤：
var shaderTagIds = new ShaderTagId[] { new ShaderTagId("UniversalForward") };
```

在 `DrawingSettings` 里设置（见下文）。

---

## DrawingSettings：怎么画

```csharp
var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
{
    // 排序方式
    criteria = SortingCriteria.CommonOpaque  // 前到后（不透明推荐）
    // criteria = SortingCriteria.CommonTransparent  // 后到前（透明推荐）
};

var drawingSettings = new DrawingSettings(
    new ShaderTagId("UniversalForward"),  // 使用哪个 Pass
    sortingSettings
);

// 可以追加多个 ShaderTagId（按顺序尝试）
drawingSettings.SetShaderPassName(1, new ShaderTagId("SRPDefaultUnlit"));

// Override Material：强制所有物体用这个 Material 渲染
drawingSettings.overrideMaterial = _xrayMaterial;
drawingSettings.overrideMaterialPassIndex = 0;
```

**`overrideMaterial`**：这是最常用的功能——不管物体原来是什么 Material，都强制用这个 Material 渲染。X 光、描边 Pass 都用这个。

---

## 典型用法一：X 光透视效果

角色被墙遮挡时，透过墙显示轮廓。

**实现思路**：在正常渲染之后，关闭深度写入、使用 `ZTest Greater`，重新渲染角色，只有被遮挡（深度测试失败的正常方向变为通过）的部分才会画出来。

```csharp
// X 光 Renderer Feature
public class XRayFeature : ScriptableRendererFeature
{
    public Material XRayMaterial;
    public LayerMask XRayLayer;

    private XRayPass _pass;

    public override void Create()
    {
        _pass = new XRayPass(XRayMaterial, XRayLayer)
        {
            // 在所有不透明物体画完之后执行
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (XRayMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }

    private class XRayPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private readonly LayerMask _layer;

        public XRayPass(Material material, LayerMask layer)
        {
            _material = material;
            _layer = layer;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("XRay");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(
                new ShaderTagId("UniversalForward"), sortingSettings)
            {
                overrideMaterial = _material,
                overrideMaterialPassIndex = 0
            };

            // 只画 XRay Layer 的物体
            var filteringSettings = new FilteringSettings(
                RenderQueueRange.opaque, _layer);

            context.DrawRenderers(
                renderingData.cullResults,
                ref drawingSettings,
                ref filteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
```

**X 光 Material 的 Shader**（关键设置）：

```hlsl
// ZTest Greater：只有被遮挡的像素才通过深度测试
ZTest Greater
// ZWrite Off：不写深度，避免影响后续渲染
ZWrite Off
// 半透明输出
Blend SrcAlpha OneMinusSrcAlpha
```

---

## 典型用法二：描边效果

描边有多种实现方案，`DrawRenderers` 适合做**基于 Pass 的描边**（背面扩展法）。

**原理**：先正常渲染物体，再用描边 Shader 重绘一次，描边 Shader 只渲染背面（`Cull Front`），沿法线方向在 Clip Space 扩展顶点，形成外描边。

```csharp
// 描边 Pass 在正常渲染之后入队
renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

// DrawingSettings：用描边 Material 覆盖
drawingSettings.overrideMaterial = _outlineMaterial;  // Cull Front + 顶点扩展 Shader
drawingSettings.overrideMaterialPassIndex = 0;

// FilteringSettings：只画需要描边的物体（专用 Layer 或特定 Queue）
var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, _outlineLayer);
```

**描边 Material 的顶点着色器关键代码**：

```hlsl
VertexOutput Vert(VertexInput v)
{
    VertexOutput o;
    // Clip Space 法线扩展描边
    float4 clipPos = TransformObjectToHClip(v.positionOS);
    float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP,
                            mul((float3x3)UNITY_MATRIX_M, v.normalOS));
    float2 offset = normalize(clipNormal.xy) * (_OutlineWidth / _ScreenParams.xy);
    clipPos.xy += offset * clipPos.w;
    o.positionCS = clipPos;
    return o;
}
```

---

## 典型用法三：自定义排序渲染

URP 默认按 RenderQueue + 深度排序。有些效果需要打破这个顺序，比如：始终在最前面渲染 UI 元素、按距离排序半透明特效。

```csharp
// 按距离从后往前排序（半透明物体标准做法）
var sortingSettings = new SortingSettings(camera)
{
    criteria = SortingCriteria.CommonTransparent
};

// 自定义排序轴（不用相机方向，改用世界 Y 轴排序）
sortingSettings.customAxis = Vector3.up;
sortingSettings.criteria = SortingCriteria.CustomAxis;

// 强制在最后渲染（不受 RenderQueue 限制）
// → 改 renderPassEvent 为 AfterRenderingTransparents
// → FilteringSettings 里 RenderQueueRange 覆盖目标物体
```

---

## RenderStateBlock：精细控制渲染状态

如果不想用 Override Material（保留原 Material），只想改深度测试、模板、混合状态，用 `RenderStateBlock`：

```csharp
var stateBlock = new RenderStateBlock(RenderStateMask.Depth)
{
    depthState = new DepthState(
        writeEnabled: false,
        compareFunction: CompareFunction.Always  // 永远通过深度测试
    )
};

context.DrawRenderers(
    cullResults,
    ref drawingSettings,
    ref filteringSettings,
    ref stateBlock  // 覆盖深度状态，其他状态保持原 Shader
);
```

`RenderStateMask` 可以组合：`RenderStateMask.Depth | RenderStateMask.Stencil` 同时覆盖深度和模板状态，但保持原 Material 的混合和颜色设置。

---

## 小结

- `DrawRenderers` = 从当前帧 CullingResults 里过滤 + 用指定设置重新渲染
- `FilteringSettings`：按 RenderQueue、Layer、ShaderTagId 过滤要画哪些物体
- `DrawingSettings`：按什么顺序画、用哪个 Shader Pass、是否 Override Material
- `overrideMaterial`：强制所有物体用同一个 Material，X 光和描边的核心
- `RenderStateBlock`：保留原 Material，只覆盖深度/模板/混合等渲染状态
- X 光：`ZTest Greater + ZWrite Off`，描边：`Cull Front + 顶点扩展`

下一篇：URP扩展-05，RenderDoc 调试 URP 自定义 Pass——捕获帧、查看 RT 内容、追踪 Blit 链、定位 Shader 问题。
