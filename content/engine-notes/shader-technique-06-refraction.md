+++
title = "Shader 核心技法 06｜折射：Camera Opaque Texture 与水下扭曲"
slug = "shader-technique-06-refraction"
date = 2026-03-26
description = "真实的玻璃、水面折射需要采样背后的场景颜色。URP 的 Camera Opaque Texture 提供了已完成不透明渲染的场景快照。用屏幕 UV 加法线扰动，实现折射扭曲效果。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "技法", "折射", "水面", "Camera Opaque Texture"]
series = ["Shader 手写技法"]
[extra]
weight = 4220
+++

玻璃、水面、热浪的折射效果，本质是"看到背后场景的扭曲版本"。URP 提供了 Camera Opaque Texture，在透明物体渲染前保存了一份场景快照，可以直接在 Shader 里采样。

---

## Camera Opaque Texture

**开启方式**：URP Asset → `Opaque Texture` 勾选。

开启后，URP 在渲染透明物体之前，把当前帧的不透明渲染结果复制到一张贴图——`_CameraOpaqueTexture`。透明物体 Shader 里可以用屏幕 UV 采样它，得到背后的场景颜色。

```hlsl
// 声明（URP 自动提供，不需要在 Properties 里写）
TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);
```

---

## 屏幕空间 UV

采样 Opaque Texture 需要**屏幕空间 UV**（当前像素在屏幕上的位置，0~1）：

```hlsl
// 在 Vertex Shader 里：
output.screenPos = ComputeScreenPos(posCS);

// 在 Fragment Shader 里，透视除法得到屏幕 UV：
float2 screenUV = input.screenPos.xy / input.screenPos.w;
```

`ComputeScreenPos` 返回 `[0, w]` 范围的值，除以 `w` 后得到 `[0, 1]`。

---

## 折射扭曲实现

用法线贴图的 XY 分量作为 UV 偏移，对 Opaque Texture 做扭曲采样：

```hlsl
// 采样法线贴图（动画滚动，模拟水面波动）
float2 waveUV1 = input.uv + float2(_Time.y * 0.05, _Time.y * 0.03);
float2 waveUV2 = input.uv - float2(_Time.y * 0.04, _Time.y * 0.02) + float2(0.5, 0.5);

float3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, waveUV1));
float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, waveUV2));
float3 blendNormal = normalize(normal1 + normal2);   // 双层法线叠加

// 用法线 XY 扰动屏幕 UV
float2 offset    = blendNormal.xy * _RefractionStrength;
float2 refractUV = screenUV + offset;

// 采样折射后的场景
half3 refraction = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractUV).rgb;
```

---

## 完整折射 Shader（水面）

```hlsl
Shader "Custom/Refraction"
{
    Properties
    {
        _WaveNormal        ("Wave Normal Map",  2D)    = "bump" {}
        _RefractionStrength("Refraction Strength", Range(0, 0.1)) = 0.02
        _WaterColor        ("Water Color",     Color)  = (0.1, 0.4, 0.6, 0.8)
        _WaterColorStrength("Water Color Blend", Range(0,1)) = 0.3
        _Specular          ("Specular",        Float)  = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WaveNormal_ST;
                float  _RefractionStrength;
                float4 _WaterColor;
                float  _WaterColorStrength;
                float  _Specular;
            CBUFFER_END

            TEXTURE2D(_WaveNormal);          SAMPLER(sampler_WaveNormal);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 positionWS  : TEXCOORD4;
                float2 uv          : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pi = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   ni = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = pi.positionCS;
                output.screenPos   = ComputeScreenPos(pi.positionCS);
                output.positionWS  = pi.positionWS;
                output.normalWS    = ni.normalWS;
                output.tangentWS   = ni.tangentWS;
                output.bitangentWS = ni.bitangentWS;
                output.uv          = TRANSFORM_TEX(input.uv, _WaveNormal);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ── 屏幕 UV ──────────────────────────────────────
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // ── 双层波纹法线 ──────────────────────────────────
                float2 uv1 = input.uv + float2( 0.05, 0.03) * _Time.y;
                float2 uv2 = input.uv + float2(-0.04, 0.06) * _Time.y + 0.5;

                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv1));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv2));
                float3 normalTS = normalize(n1 + n2);

                // 切线空间 → 世界空间
                float3 normalWS = normalize(
                    input.tangentWS   * normalTS.x +
                    input.bitangentWS * normalTS.y +
                    input.normalWS    * normalTS.z
                );

                // ── 折射偏移 ─────────────────────────────────────
                float2 offset    = normalTS.xy * _RefractionStrength;
                float2 refractUV = saturate(screenUV + offset);   // clamp 防止采到边界外
                half3  refraction = SAMPLE_TEXTURE2D(_CameraOpaqueTexture,
                                        sampler_CameraOpaqueTexture, refractUV).rgb;

                // ── 水体颜色混合 ──────────────────────────────────
                half3 waterColor = lerp(refraction, _WaterColor.rgb, _WaterColorStrength);

                // ── 镜面高光 ──────────────────────────────────────
                Light  light   = GetMainLight();
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(light.direction + viewDir);
                half   NdotH   = saturate(dot(normalWS, halfDir));
                half   spec    = pow(NdotH, 128.0) * _Specular;

                half3 finalColor = waterColor + light.color * spec;
                return half4(finalColor, _WaterColor.a);
            }
            ENDHLSL
        }
    }
}
```

---

## 边界问题处理

扰动后的 `refractUV` 可能超出 `[0,1]`，采样到画面边缘以外的区域。处理方案：

```hlsl
// 方案 1：saturate 夹住（简单，边缘会有拉伸）
float2 refractUV = saturate(screenUV + offset);

// 方案 2：靠近边缘时减弱扰动（更自然）
float2 edgeMask  = min(screenUV, 1.0 - screenUV) * 10.0;  // 边缘处趋近 0
float  edgeFade  = saturate(min(edgeMask.x, edgeMask.y));
float2 refractUV = screenUV + offset * edgeFade;
```

---

## 性能注意

- `_CameraOpaqueTexture` 要求 URP 在渲染透明物体前做一次 RT 复制，**有额外带宽开销**
- 移动端谨慎使用，大面积水面尤其注意
- 简化方案：固定底色 + 法线高光，不采样 Opaque Texture，适合移动端中低档

---

## 小结

| 概念 | 要点 |
|------|------|
| Opaque Texture | URP Asset 开启，透明物体渲染前的场景快照 |
| 屏幕 UV | `ComputeScreenPos` → 除以 w |
| 折射扰动 | 法线 XY 作为偏移，加到屏幕 UV 上 |
| 边界处理 | `saturate` 或渐变遮罩 |
| 移动端 | 有额外带宽，谨慎用于大面积水面 |

下一篇：高度雾——基于世界 Y 坐标的自定义雾效，超越 Unity 内置雾的简陋表现。
