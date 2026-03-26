+++
title = "Shader 核心技法 10｜屏幕空间 UV：扫描线、X-Ray 与屏幕坐标特效"
slug = "shader-technique-10-screen-space-uv"
date = 2026-03-26
description = "用屏幕坐标而非模型 UV 来驱动 Shader 效果——扫描线、全息投影、X-Ray 透视、噪点叠加。理解 SV_POSITION 和 ComputeScreenPos 的差别，以及屏幕空间坐标的常见用法。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "技法", "屏幕空间", "扫描线", "全息", "X-Ray"]
series = ["Shader 手写技法"]
[extra]
weight = 4260
+++

有些效果不依赖模型的 UV，而是基于像素在**屏幕上的位置**——全息投影的扫描线不管模型怎么旋转，扫描线都保持水平；X-Ray 透视在屏幕空间显示被遮挡的物体轮廓。

---

## 获取屏幕坐标

有两种方式拿到屏幕坐标：

**方式一：`SV_POSITION` 的 XY（Fragment Shader 里）**

```hlsl
// Fragment Shader 的输入 SV_POSITION 在 Fragment 阶段是屏幕像素坐标：
// positionHCS.xy = 像素的屏幕坐标（单位：像素，非归一化）
float2 screenPixel = input.positionHCS.xy;

// 归一化到 [0,1]
float2 screenUV = screenPixel / _ScreenParams.xy;
// _ScreenParams = (width, height, 1/width, 1/height)
```

**方式二：`ComputeScreenPos`（Vertex Shader 里预算）**

```hlsl
// Vertex Shader：
output.screenPos = ComputeScreenPos(posCS);

// Fragment Shader：
float2 screenUV = input.screenPos.xy / input.screenPos.w;
// 结果：[0,1]，考虑了透视
```

两种方式的差别：`SV_POSITION.xy` 是像素整数坐标（更直接，适合像素级图案）；`ComputeScreenPos` 是插值的屏幕空间位置（精确，适合需要与世界坐标配合的效果）。

---

## 一、扫描线

水平扫描线用 `sin(screenUV.y * frequency)` 实现：

```hlsl
float2 screenUV  = input.positionHCS.xy / _ScreenParams.xy;
float  scanline  = sin(screenUV.y * _ScanlineFreq + _Time.y * _ScanlineSpeed);
scanline         = scanline * 0.5 + 0.5;   // 0~1
scanline         = lerp(1.0, scanline, _ScanlineStrength);

half3 color = albedo.rgb * scanline;
```

`_ScanlineFreq` 控制扫描线密度，`_ScanlineSpeed` 控制向下滚动速度。

---

## 二、全息投影效果

组合扫描线 + 菲涅耳边缘光 + 时间闪烁：

```hlsl
// 扫描线（屏幕空间 Y）
float2 screenUV = input.positionHCS.xy / _ScreenParams.xy;
float  scanline = sin(screenUV.y * _ScanFreq - _Time.y * _ScanSpeed) * 0.5 + 0.5;

// 边缘光（Rim）
float NdotV = saturate(dot(normalWS, viewDir));
float rim   = pow(1.0 - NdotV, 3.0);

// 时间闪烁（随机断裂感）
float flicker = sin(_Time.y * 7.3) * sin(_Time.y * 13.7) * 0.1 + 0.9;

// 全息颜色
half3 holoColor = _HoloColor.rgb * (scanline * 0.3 + rim * 0.7) * flicker;
return half4(holoColor, (scanline * 0.5 + rim) * _HoloColor.a);
```

---

## 三、X-Ray 透视

让物体在被其他物体遮挡时仍然可见（用不同颜色/样式渲染），常用于游戏角色穿墙可见：

```hlsl
// Pass 1：正常渲染（ZTest LEqual，标准深度测试）
Pass { Tags { "LightMode" = "UniversalForward" } ... }

// Pass 2：X-Ray Pass（ZTest Greater，只在被遮挡时绘制）
Pass
{
    Name "XRay"
    Tags { "LightMode" = "SRPDefaultUnlit" }

    ZTest  Greater   // 深度测试改为：当前深度 > 深度缓冲（即被遮挡）
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

    HLSLPROGRAM
    #pragma vertex   vert_xray
    #pragma fragment frag_xray
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _XRayColor;
        float  _XRayIntensity;
    CBUFFER_END

    struct Attr { float4 pos:POSITION; float3 n:NORMAL; };
    struct Vary { float4 hcs:SV_POSITION; float3 normalWS:TEXCOORD0; float3 posWS:TEXCOORD1; };

    Vary vert_xray(Attr i) {
        Vary o;
        o.hcs      = TransformObjectToHClip(i.pos.xyz);
        o.posWS    = TransformObjectToWorld(i.pos.xyz);
        o.normalWS = TransformObjectToWorldNormal(i.n);
        return o;
    }

    half4 frag_xray(Vary input) : SV_Target
    {
        // 边缘光：X-Ray 只在轮廓处明显
        float3 viewDir = normalize(GetWorldSpaceViewDir(input.posWS));
        float  NdotV   = saturate(dot(normalize(input.normalWS), viewDir));
        float  rim     = pow(1.0 - NdotV, 3.0);
        return half4(_XRayColor.rgb, rim * _XRayIntensity * _XRayColor.a);
    }
    ENDHLSL
}
```

---

## 四、屏幕空间噪声叠加

用屏幕坐标采样噪声，不跟随模型旋转（世界空间固定的噪声图案）：

```hlsl
// 屏幕坐标噪声（颗粒感、电视雪花）
float2 noiseUV   = input.positionHCS.xy / _ScreenParams.xy * _NoiseScale;
noiseUV         += _Time.y * 0.01;   // 缓慢漂移
float  noise     = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
half3  color     = albedo.rgb * (1.0 + (noise - 0.5) * _NoiseStrength);
```

---

## 五、屏幕空间网格/像素化

把屏幕分成固定大小的格子，做像素化效果：

```hlsl
// 每 _PixelSize 像素对齐到同一格子
float2 pixelCoord = floor(input.positionHCS.xy / _PixelSize) * _PixelSize;
float2 pixelUV    = pixelCoord / _ScreenParams.xy;

// 用格子中心的 UV 采样（而不是像素精确位置）
// 效果：图像被像素化
```

---

## 小结

| 效果 | 核心技术 | 关键点 |
|------|---------|--------|
| 扫描线 | `sin(screenUV.y * freq)` | 屏幕 Y 坐标驱动 |
| 全息 | 扫描线 + Rim + 闪烁 | 半透明 + Additive 混合 |
| X-Ray | `ZTest Greater` 第二个 Pass | 被遮挡时才绘制 |
| 屏幕噪声 | 屏幕 UV 采样噪声贴图 | 不跟随模型旋转 |
| 像素化 | `floor(xy / size)` | 量化屏幕坐标 |

| 坐标来源 | 用途 |
|---------|------|
| `SV_POSITION.xy`（Fragment） | 像素整数坐标，适合格子/扫描线 |
| `ComputeScreenPos`（Vertex） | 归一化屏幕坐标，适合与世界坐标配合 |

下一篇：自发光与 Bloom 联动——HDR 颜色值如何驱动 Bloom，自发光强度的控制与动画。
