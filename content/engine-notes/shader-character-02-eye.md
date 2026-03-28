---
title: "游戏常用效果｜眼睛渲染：角膜折射、焦散高光与瞳孔缩放"
slug: "shader-character-02-eye"
date: "2026-03-28"
description: "从解剖结构出发，实现眼睛的角膜折射偏移、焦散高光和瞳孔缩放，以及湿润感的光照搭配。"
tags: ["Shader", "HLSL", "URP", "角色渲染", "眼睛", "折射", "角膜"]
series: "Shader 手写技法"
weight: 4510
---

角色眼睛是玩家视线最集中的地方，也是"廉价感"和"质感"差距最明显的地方。眼睛渲染不是单纯的皮肤着色，它涉及到**光学折射、湿润表面反射和生理结构**三个维度，每一层偷工减料都会被玩家直觉感知到。

---

## 眼球的分层结构

在着色器里，眼球通常拆成两层网格，或者用一层网格配合 Shader 内折射来模拟分层效果：

- **巩膜（Sclera）**：眼白部分，本质是皮肤，有轻微的血丝纹路和湿润反光。
- **虹膜（Iris）**：彩色的纹理区，平面内凹于角膜之下，渲染时需要模拟被角膜折射"放大"的视觉效果。
- **瞳孔（Pupil）**：虹膜中央的黑色区域，通过 UV 缩放来控制大小。
- **角膜（Cornea）**：覆盖虹膜和瞳孔的透明曲面，负责产生镜面高光和折射虹膜。

单网格方案中，整个眼球是一个略微前凸的网格，Shader 内部通过**视线向量与法线的夹角**计算折射偏移量，把采样虹膜贴图的 UV 向外偏移，模拟角膜曲面的折射效果。

## 角膜折射

折射的物理本质是光线穿过不同折射率介质时方向改变。游戏里不做真正的光线弯曲，而是**将虹膜贴图的 UV 按视线方向偏移**，让眼球从侧面看时虹膜像"向外扩张"了一圈。

```hlsl
// 折射偏移：V 是视线方向（指向相机），N 是表面法线
// refraction 越大，偏移越明显
float2 CornealRefraction(float3 V, float3 N, float2 uv, float refraction)
{
    // 计算视线在切线平面上的投影
    float3 viewTangent = normalize(V - dot(V, N) * N);
    // 偏移量正比于视角倾斜程度（grazing angle）
    float grazingFactor = 1.0 - saturate(dot(V, N));
    float2 offset = viewTangent.xy * grazingFactor * refraction;
    return uv + offset;
}
```

这个偏移效果在正对眼睛时几乎为零，斜看时虹膜会向外偏移，眼球看起来有内凹的空间感。`refraction` 参数在 0.02~0.06 之间视觉效果比较自然。

## 瞳孔缩放

瞳孔大小需要可控，用来配合动画（惊恐时扩张、受强光时收缩）或特效（技能释放时瞳孔变形）。实现方式是在 UV 上做以虹膜中心为原点的缩放。

```hlsl
// irisUV 是以 (0.5, 0.5) 为中心的虹膜区域 UV
// pupilScale < 1 时瞳孔扩大，> 1 时瞳孔缩小
float2 PupilScale(float2 irisUV, float pupilScale)
{
    float2 centered = irisUV - 0.5;
    // 只缩放瞳孔区域内部，避免影响虹膜纹理外圈
    float dist = length(centered);
    float pupilRadius = 0.25; // 瞳孔在 UV 空间内的半径
    float t = smoothstep(0.0, pupilRadius, dist);
    float2 scaledUV = centered * lerp(pupilScale, 1.0, t) + 0.5;
    return scaledUV;
}
```

`t` 在瞳孔中心为 0，边缘为 1，缩放只作用于中心区域，虹膜外圈 UV 不受影响，避免纹理撕裂。

## 焦散高光

眼睛的高光不是普通的 GGX。角膜曲面很光滑，高光应该非常**锐利**；同时眼球是球体，高光在不同视角下会沿球面拉伸成一个略微椭圆形的形状。

```hlsl
// 眼睛焦散高光：在 N·H 基础上增加各向异性拉伸
float EyeSpecular(float3 N, float3 H, float3 T, float sharpness, float aniso)
{
    float NdotH = saturate(dot(N, H));
    float TdotH = dot(T, H);
    // 沿切线方向拉伸高光，模拟角膜曲面形变
    float anisoFactor = sqrt(1.0 - aniso * TdotH * TdotH);
    float spec = pow(NdotH * anisoFactor, sharpness);
    return spec;
}
```

`sharpness` 推荐值 200~600（远高于皮肤的 32~64），`aniso` 控制椭圆拉伸程度，0.3~0.5 比较自然。高光颜色通常是纯白色，不受发色或皮肤颜色影响。

## 湿润感：Rim Light 与菲涅尔

眼睛边缘的湿润感来自两方面：

**巩膜-眼睑交界处**：由于湿润液体积聚，光线在此产生强烈的菲涅尔反射，形成细亮边。这个效果可以用 Fresnel 项乘上一个边缘遮罩实现。

**角膜整体**：角膜对环境光有强反射，在暗场景中尤其明显。可以采样一张低精度的 Cubemap 或使用 SH（球谐环境光），乘上菲涅尔系数叠加。

```hlsl
// 菲涅尔湿润感
float FresnelRim(float3 N, float3 V, float rimPower, float rimStrength)
{
    float fresnel = pow(1.0 - saturate(dot(N, V)), rimPower);
    return fresnel * rimStrength;
}
```

`rimPower` 在 3~5 之间，`rimStrength` 在 0.3~0.8 之间。如果场景是室内暖光，可以把 rim 颜色偏暖黄；室外冷光场景偏蓝白。

## 完整眼睛 Fragment Shader

```hlsl
half4 EyeFragment(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    float3 N = normalize(input.normalWS);
    float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));
    Light mainLight = GetMainLight();
    float3 L = mainLight.direction;
    float3 H = normalize(L + V);

    // ---- 折射偏移后采样虹膜 ----
    float2 refractedUV = CornealRefraction(V, N, uv, _RefractionStrength);

    // 瞳孔缩放（只对虹膜区域内应用）
    float2 irisUV = PupilScale(refractedUV, _PupilScale);

    // 区分巩膜和虹膜区域（用贴图 alpha 或 UV 距离）
    float distFromCenter = length(uv - 0.5) * 2.0;
    float irisMask = 1.0 - smoothstep(_IrisRadius - 0.05, _IrisRadius, distFromCenter);

    float4 scleraColor = tex2D(_ScleraMap, uv) * _ScleraColor;
    float4 irisColor   = tex2D(_IrisMap,   irisUV) * _IrisColor;
    float4 baseColor   = lerp(scleraColor, irisColor, irisMask);

    // ---- 漫反射 ----
    float NdotL = saturate(dot(N, L));
    // 巩膜区域允许更多环境漫反射，虹膜相对暗（模拟内凹阴影）
    float shadowFactor = lerp(1.0, 0.6, irisMask);
    float3 diffuse = baseColor.rgb * mainLight.color * (NdotL * shadowFactor * 0.8 + 0.2);

    // ---- 焦散高光（只在角膜区域，即虹膜+瞳孔上方） ----
    float3 T = normalize(input.tangentWS);
    float specIntensity = EyeSpecular(N, H, T, _SpecSharpness, _SpecAniso);
    // 高光 mask：只在虹膜+部分巩膜边缘有强高光
    float specMask = lerp(0.3, 1.0, irisMask);
    float3 specular = specIntensity * specMask * mainLight.color * _SpecColor.rgb;

    // ---- 湿润 Rim ----
    float rim = FresnelRim(N, V, _RimPower, _RimStrength);
    float3 rimColor = rim * _RimColor.rgb;

    // ---- 环境反射（角膜）----
    float3 envReflect = 0;
    #ifdef _ENV_REFLECTION
        float3 reflDir = reflect(-V, N);
        envReflect = texCUBE(_EnvCubemap, reflDir).rgb * _EnvStrength;
        float envFresnel = pow(1.0 - saturate(dot(N, V)), 3.0);
        envReflect *= envFresnel * irisMask; // 只在角膜区域反射
    #endif

    float3 finalColor = diffuse + specular + rimColor + envReflect;
    return half4(finalColor, 1.0);
}
```

## 材质参数参考

| 参数               | 典型值          | 说明                             |
|--------------------|-----------------|----------------------------------|
| `_RefractionStrength` | 0.03 ~ 0.05  | 角膜折射强度，太大会失真         |
| `_PupilScale`      | 0.8 ~ 1.3       | 1.0 为原始大小，<1 瞳孔放大      |
| `_IrisRadius`      | 0.35 ~ 0.42     | 虹膜在 UV 空间的半径             |
| `_SpecSharpness`   | 300 ~ 500       | 焦散高光锐利度                   |
| `_SpecAniso`       | 0.3 ~ 0.5       | 高光椭圆拉伸                     |
| `_RimPower`        | 3.0 ~ 5.0       | 菲涅尔边缘陡峭度                 |
| `_RimStrength`     | 0.4 ~ 0.7       | 湿润边缘亮度                     |

眼睛着色器不需要非常复杂，关键在于折射偏移的微小量和高光的极高锐利度这两个细节——正是这两点撑起眼睛"活的"质感。巩膜的血丝纹路可以直接烘焙进贴图，不需要额外着色计算。
