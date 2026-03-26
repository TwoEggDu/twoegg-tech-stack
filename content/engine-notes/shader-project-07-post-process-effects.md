---
title: "项目实战 07｜后处理特效组合：夜视仪、血屏、扫描波"
slug: "shader-project-07-post-process-effects"
date: "2026-03-26"
description: "游戏中常见的屏幕空间特效：夜视仪（绿色噪点 + 渐晕）、受伤血屏（边缘红色脉冲）、扫描波（从中心扩散的波纹）。每种效果都用 Renderer Feature + Blit Shader 实现，可独立开关、叠加组合。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "项目实战"
  - "后处理"
  - "Renderer Feature"
  - "屏幕特效"
series: "Shader 手写技法"
weight: 4460
---
这三种后处理特效在很多类型的游戏里都会用到——FPS 的受伤反馈、恐怖游戏的夜视、技能释放的扫描波。它们都基于进阶层介绍的 Renderer Feature 结构，每种效果一个独立的 Feature + Shader。

---

## 效果一：夜视仪（Night Vision）

特征：绿色调 + 颗粒噪点 + 圆形渐晕（模拟目镜）+ 高亮区域曝光。

```hlsl
Shader "Custom/PostProcess/NightVision"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "NightVision"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _NoiseStrength;    // 噪点强度
                float _Brightness;       // 整体亮度增益
                float _VignettePower;    // 渐晕强度
            CBUFFER_END

            // 简单哈希噪声
            float hash(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv  = input.texcoord;
                half4  col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // ── 亮度（模拟图像增强器）──────────────────────
                half  lum  = dot(col.rgb, half3(0.299, 0.587, 0.114)) * _Brightness;

                // ── 颗粒噪点（随时间变化）─────────────────────
                float noise    = hash(uv + _Time.xx * 0.1) * _NoiseStrength;
                lum = saturate(lum + noise - _NoiseStrength * 0.5);

                // ── 绿色调 ────────────────────────────────────
                half3 green = half3(lum * 0.2, lum, lum * 0.3);

                // ── 圆形渐晕（目镜边缘变暗）──────────────────
                float2 centered = uv - 0.5;
                float  dist     = length(centered);
                float  vignette = pow(saturate(1.0 - dist * 2.0), _VignettePower);

                // ── 扫描线（微弱，模拟 CRT 目镜）─────────────
                float scan = sin(uv.y * 300.0) * 0.04 + 0.96;

                return half4(green * vignette * scan, 1.0);
            }
            ENDHLSL
        }
    }
}
```

**C# Feature（强度控制）：**
```csharp
public class NightVisionFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent insertPoint = RenderPassEvent.AfterRenderingPostProcessing;
        public Material material;
        [Range(0, 1)] public float noiseStrength = 0.1f;
        [Range(1, 5)] public float brightness    = 2.5f;
        [Range(1, 8)] public float vignettePower = 3.0f;
        public bool enabled = false;
    }
    // ... (标准 Feature 结构，参考进阶-03)
}
```

---

## 效果二：受伤血屏（Hit Vignette）

受伤时屏幕边缘出现红色脉冲，越接近死亡颜色越浓：

```hlsl
Shader "Custom/PostProcess/HitVignette"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "HitVignette"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _VignetteColor;   // 渐晕颜色（红色/橙色）
                float  _Intensity;       // 总强度（0=无效果，1=最强）
                float  _Softness;        // 边缘柔化
                float  _Pulse;           // 脉冲值（0~1，由 C# 的 sin 曲线驱动）
            CBUFFER_END

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv  = input.texcoord;
                half4  col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // ── 边缘渐晕距离 ──────────────────────────────
                float2 centered = uv - 0.5;
                float  dist     = length(centered);

                // smoothstep：内圆（_Softness）到 0.5 之间的边缘
                float  vignetteShape = smoothstep(0.5 - _Softness, 0.5, dist);

                // 总强度 × 脉冲（受伤瞬间亮，然后逐渐消退）
                float  vignetteAlpha = vignetteShape * _Intensity * _Pulse;

                // ── 颜色叠加（边缘偏向血色）──────────────────
                half3 vignette = lerp(col.rgb, _VignetteColor.rgb, vignetteAlpha);

                // ── 中心轻微去饱和（受伤时视觉模糊感）────────
                half  lum  = dot(col.rgb, half3(0.299, 0.587, 0.114));
                half3 gray = lerp(col.rgb, half3(lum, lum, lum), vignetteAlpha * 0.3);
                vignette   = lerp(vignette, gray, 0);  // 可选：叠加去饱和

                return half4(vignette, 1.0);
            }
            ENDHLSL
        }
    }
}
```

**C# 控制脉冲：**
```csharp
public class HitVignetteController : MonoBehaviour
{
    public Material vignettemat;
    float _pulseTimer = 0;
    float _currentHP  = 100;

    public void OnHit(float newHP)
    {
        _currentHP = newHP;
        _pulseTimer = 1.0f;   // 触发脉冲
    }

    void Update()
    {
        if (_pulseTimer > 0)
        {
            _pulseTimer -= Time.deltaTime * 2.0f;  // 0.5 秒消退
            _pulseTimer  = Mathf.Max(0, _pulseTimer);
        }

        float healthFactor = 1.0f - Mathf.Clamp01(_currentHP / 100f);
        float pulse = Mathf.Max(_pulseTimer, healthFactor * 0.5f);  // 低血量常驻

        vignettemat.SetFloat("_Pulse",     pulse);
        vignettemat.SetFloat("_Intensity", 0.8f);
    }
}
```

---

## 效果三：扫描波（Scan Wave）

从某个世界坐标点向外扩散的球形/圆形波纹，常用于技能释放、探测扫描、爆炸冲击波：

```hlsl
Shader "Custom/PostProcess/ScanWave"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "ScanWave"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float3 _WaveOriginWS;    // 波纹世界坐标原点
                float  _WaveRadius;      // 当前波纹半径（C# 逐帧增大）
                float  _WaveWidth;       // 波纹厚度
                float4 _WaveColor;       // 波纹颜色
                float  _WaveIntensity;   // 强度
                float4x4 _InvVP;         // 逆 VP 矩阵（C# 传入，用于重建世界坐标）
            CBUFFER_END

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv  = input.texcoord;
                half4  col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // ── 从深度图重建世界坐标 ──────────────────────
                float rawDepth  = SampleSceneDepth(uv);

                // NDC 坐标（注意 OpenGL/DX 差异）
                float4 ndc = float4(uv * 2.0 - 1.0,
                    #if UNITY_REVERSED_Z
                        rawDepth,
                    #else
                        rawDepth * 2.0 - 1.0,
                    #endif
                    1.0);

                // 反投影到世界空间
                float4 worldPos4 = mul(_InvVP, ndc);
                float3 worldPos  = worldPos4.xyz / worldPos4.w;

                // ── 计算到波纹原点的距离 ──────────────────────
                float dist      = length(worldPos - _WaveOriginWS);

                // 波纹：在 [_WaveRadius - _WaveWidth, _WaveRadius] 范围内显示
                float waveMask  = smoothstep(_WaveRadius - _WaveWidth, _WaveRadius, dist)
                                * (1.0 - smoothstep(_WaveRadius, _WaveRadius + 0.1, dist));

                // ── 叠加波纹颜色 ──────────────────────────────
                half3 waveEffect = lerp(col.rgb, _WaveColor.rgb, waveMask * _WaveIntensity);

                return half4(waveEffect, 1.0);
            }
            ENDHLSL
        }
    }
}
```

**C# 控制波纹扩散：**
```csharp
public class ScanWaveEffect : MonoBehaviour
{
    public Material waveMaterial;
    public float    expandSpeed = 15.0f;
    public float    maxRadius   = 50.0f;

    float _radius = 0;
    bool  _active = false;

    public void Trigger(Vector3 worldOrigin)
    {
        _radius = 0;
        _active = true;
        waveMaterial.SetVector("_WaveOriginWS", worldOrigin);
    }

    void Update()
    {
        if (!_active) return;

        _radius += Time.deltaTime * expandSpeed;
        waveMaterial.SetFloat("_WaveRadius", _radius);

        // 逆 VP 矩阵（用于在 Shader 里重建世界坐标）
        Matrix4x4 vp    = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
        waveMaterial.SetMatrix("_InvVP", vp.inverse);

        if (_radius > maxRadius) _active = false;
    }
}
```

---

## 多效叠加策略

同时开启多个后处理效果时，注意叠加顺序和性能：

```
推荐叠加顺序（RenderPassEvent 从小到大）：
    1. 受伤血屏（AfterRenderingOpaques）
    2. 扫描波（AfterRenderingTransparents）
    3. 夜视仪（AfterRenderingPostProcessing，在 URP 内置后处理之后）
```

同时开启多个 Blit 效果时，考虑合并到一个 Shader 里（减少 RT 切换次数）——见进阶-03 的性能说明。

---

## 小结

| 效果 | 核心技术 | C# 控制 |
|------|---------|--------|
| 夜视仪 | 亮度增益 + 哈希噪点 + 圆形渐晕 | `_NoiseStrength`，`_Brightness`，`enabled` |
| 血屏 | 边缘 smoothstep 渐晕 + 颜色叠加 | `_Pulse`（sin 脉冲），`_Intensity`（HP 相关）|
| 扫描波 | 深度图重建世界坐标 + 球形距离遮罩 | `_WaveRadius`（每帧增加），`_WaveOriginWS` |

下一篇：Shader 调试与性能分析工作流——从发现问题到定位根因，整理一套完整的 Shader 调试方法论。
