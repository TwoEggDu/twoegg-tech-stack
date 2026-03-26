+++
title = "Shader 进阶技法 03｜自定义后处理：Renderer Feature 完整写法"
slug = "shader-advanced-03-renderer-feature"
date = 2026-03-26
description = "URP 的后处理通过 Renderer Feature 插入渲染管线。理解 ScriptableRendererFeature / ScriptableRenderPass 的结构，写一个完整的全屏后处理效果（灰度、色差、像素化）。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "进阶", "后处理", "Renderer Feature", "全屏特效"]
series = ["Shader 手写技法"]
[extra]
weight = 4310
+++

URP 的后处理不再用 `OnRenderImage`，而是通过 **Renderer Feature** 插入渲染管线。写一个完整的后处理效果需要三个部分：C# 的 Feature + Pass 类，以及 HLSL Shader。

---

## 整体结构

```
ScriptableRendererFeature   ← 注册到 Renderer Asset，持有配置
    └─ ScriptableRenderPass ← 实际执行渲染，调度 GPU 工作
           └─ Shader (Blit) ← 全屏 quad，对 RT 做后处理
```

---

## Step 1：Renderer Feature 类

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrayscaleFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent insertPoint = RenderPassEvent.AfterRenderingPostProcessing;
        public Material material;
        [Range(0f, 1f)] public float intensity = 0.5f;
    }

    public Settings settings = new Settings();
    private GrayscalePass _pass;

    public override void Create()
    {
        _pass = new GrayscalePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 只在 Game/Scene View 执行，Preview Camera 跳过
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;
        if (settings.material == null) return;

        renderer.EnqueuePass(_pass);
    }
}
```

---

## Step 2：Render Pass 类

```csharp
public class GrayscalePass : ScriptableRenderPass
{
    private readonly GrayscaleFeature.Settings _settings;
    private RTHandle _tempRT;

    public GrayscalePass(GrayscaleFeature.Settings settings)
    {
        _settings = settings;
        renderPassEvent = settings.insertPoint;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_TempGrayscale");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_settings.material == null) return;

        CommandBuffer cmd = CommandBufferPool.Get("Grayscale");

        // 传参数给 Shader
        _settings.material.SetFloat("_Intensity", _settings.intensity);

        // 获取当前帧的 Camera Color Target
        RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

        // Blit：source → _tempRT（应用 Shader），再 Blit 回 source
        Blitter.BlitCameraTexture(cmd, source, _tempRT, _settings.material, 0);
        Blitter.BlitCameraTexture(cmd, _tempRT, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandle 由 RenderingUtils.ReAllocateIfNeeded 管理，无需手动释放
    }

    public void Dispose()
    {
        _tempRT?.Release();
    }
}
```

---

## Step 3：后处理 Shader

全屏后处理 Shader 使用 `Blitter` 专用结构（URP 14+ 推荐）：

```hlsl
Shader "Custom/PostProcess/Grayscale"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "Grayscale"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            // Blit.hlsl 提供：Vert 顶点 Shader（全屏 quad），_BlitTexture

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END

            half4 frag(Varyings input) : SV_Target
            {
                // _BlitTexture 和 sampler_LinearClamp 由 Blit.hlsl 提供
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // 灰度转换（亮度公式）
                half gray = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));

                // 按 intensity 混合
                color.rgb = lerp(color.rgb, half3(gray, gray, gray), _Intensity);
                return color;
            }
            ENDHLSL
        }
    }
}
```

---

## 接入使用

1. 在 Project 里创建 `GrayscaleFeature.cs`
2. 打开 URP Renderer Asset（`Assets/Settings/UniversalRenderer.asset`）
3. Add Renderer Feature → `Grayscale Feature`
4. 把后处理 Shader 赋给材质，材质赋给 Feature 的 `Settings.Material`

---

## 常用后处理效果模板

**色差（Chromatic Aberration）：**

```hlsl
half4 frag(Varyings input) : SV_Target
{
    float2 uv     = input.texcoord;
    float2 offset = (uv - 0.5) * _AberrationStrength;

    half r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).r;
    half g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).g;
    half b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset).b;

    return half4(r, g, b, 1.0);
}
```

**像素化（Pixelate）：**

```hlsl
half4 frag(Varyings input) : SV_Target
{
    float2 uv        = input.texcoord;
    float2 pixelSize = _PixelSize / _ScreenParams.xy;
    float2 pixelUV   = floor(uv / pixelSize) * pixelSize + pixelSize * 0.5;
    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, pixelUV);
}
```

**扫描线叠加：**

```hlsl
half4 frag(Varyings input) : SV_Target
{
    half4 color   = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
    float scanline = sin(input.texcoord.y * _ScreenParams.y * 3.14159) * 0.5 + 0.5;
    color.rgb     *= lerp(1.0, scanline, _ScanlineStrength);
    return color;
}
```

---

## Unity 2022.3 vs Unity 6 注意

Unity 2022.3（URP 14）：使用 `Blitter.BlitCameraTexture` + `Blit.hlsl`，是推荐的非弃用路径。

Unity 6（URP 17）：推荐迁移到 RenderGraph API（`RecordRenderGraph`），但旧的 `Execute()` + Blitter 路径仍然可用，走 UnsafePass 兼容层。

---

## 小结

| 组件 | 职责 |
|------|------|
| `ScriptableRendererFeature` | 注册到 Renderer，持有配置，创建 Pass |
| `ScriptableRenderPass` | 执行渲染，分配 RT，调度 Blit |
| 后处理 Shader | 全屏 quad，`include Blit.hlsl`，处理 `_BlitTexture` |
| `Blitter.BlitCameraTexture` | URP 14+ 推荐的全屏 Blit 接口 |

下一篇：屏幕空间反射（SSR）——用深度和法线在屏幕空间追踪反射光线，比反射探针更精确的动态反射。
