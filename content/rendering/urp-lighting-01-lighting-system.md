---
title: "URP 深度光照 01｜URP 光照系统：主光、附加光、Light Layer、Light Cookie"
slug: "urp-lighting-01-lighting-system"
date: "2026-03-25"
description: "URP 光照系统的完整架构：主光与附加光的分工、逐顶点 vs 逐像素的精度代价、Forward+ 下的 Cluster Light List、Light Layer 的渲染层隔离、Light Cookie 的实现与用法。"
tags:
  - "Unity"
  - "URP"
  - "光照"
  - "Light Layer"
  - "Light Cookie"
  - "渲染管线"
series: "URP 深度"
weight: 1560
---
> **读这篇之前**：本篇会用到光照模型和 Shader 光照计算的基本概念。如果不熟悉，建议先看：
> - [Unity 渲染系统 02｜光照资产：实时光、Lightmap、Light Probe]({{< relref "rendering/unity-rendering-02-lighting-assets.md" >}})
> - [Shader 核心光照 01｜Blinn-Phong 高光]({{< relref "rendering/shader-lighting-01-blinn-phong.md" >}})

URP 的光照系统不是"把所有光照信息传给 Shader"那么简单，它在 CPU 侧做了大量分类、排序、裁剪工作，才把一份经过精简的光源数据送到 GPU。理解这套机制，才能明白为什么场景里某盏灯"不生效"，以及 Shader 里的 `_MainLightColor` 和 `_AdditionalLightsBuffer` 分别是什么。

---

## 主光（Main Light）与附加光（Additional Lights）的分工

URP 把场景里的所有灯分成两类：

**主光（Main Light）**：场景里唯一的高质量平行光。URP 的选取规则：在场景所有 Directional Light 里，优先选被标记为 Sun Source 的（`RenderSettings.sun`），如果没有，选最亮的那盏。

主光拥有专用的渲染路径：
- 独立的 Cascade Shadow Map（最多 4 个 Cascade）
- 在 Shader 里通过 `_MainLightColor`、`_MainLightDirection`、`_MainLightShadowmapTexture` 访问
- 不占用附加光槽位

**附加光（Additional Lights）**：除主光以外的所有光源（Point、Spot，以及没有被选为主光的 Directional Light）。附加光的渲染质量和数量上限由 Pipeline Asset 控制。

### 为什么要区分主光和附加光

主光是最影响场景视觉效果的光源（通常是太阳、月亮），需要最高质量的阴影。如果所有光源平等对待，Shadow Map 的预算（Atlas 空间 + GPU 时间）很快就不够用。

附加光通常是补光、装饰光，可以接受更低精度甚至无阴影。分两类后，主光可以独享 Shadow Map 资源，附加光根据重要性分配剩余资源。

---

## 附加光的精度分级

每个附加光可以以两种精度计算光照：

### 逐像素（Per Pixel）

在 Fragment Shader 里，对每个像素单独计算光照。结果精确，但附加光数量直接影响 Fragment Shader 的复杂度（每个附加光 = 一次光照循环迭代）。

Forward 路径下，URP 在 CPU 侧排序所有附加光（按距离 Camera 或物体的远近），取前 N 个（N = Pipeline Asset 里的 Per Object Limit，默认 4，最大 8）作为逐像素光源，剩余的降级为逐顶点光源。

### 逐顶点（Per Vertex）

在 Vertex Shader 里计算光照，结果在顶点间插值。计算精度低（Mesh 越粗，光照越糊），但几乎不增加 Fragment Shader 复杂度。适合远处的补光或不重要的装饰光。

Pipeline Asset 里可以选择 Additional Lights 的最低精度要求：
- `Per Pixel`：所有附加光都逐像素（最高质量，最高代价）
- `Per Vertex`：所有附加光都逐顶点（最低代价，精度差）

实际上，URP 的排序机制是：距离最近的 N 个用逐像素，剩余的用逐顶点（或完全忽略，取决于设置）。

### Forward+ 下的附加光

Forward+ 路径取消了"每物体最多 N 个附加光"的限制，改为按 Cluster（空间块）分配：

```
屏幕按 XY 分成 Tile（如 16×16 像素）
Tile 按 Z 分成若干 Cluster（深度切片）
每个 Cluster 有一个 Light List（影响该 Cluster 的附加光 Index 列表）

Fragment Shader 里：
  根据当前像素的屏幕坐标和深度，查对应 Cluster 的 Light List
  循环 Light List 里的附加光（通常 10-20 个，而不是场景全部）
```

Forward+ 下，Shader 里的附加光循环逻辑会被 `USE_FORWARD_PLUS` 宏切换：

```hlsl
#if USE_FORWARD_PLUS
    // 从 Cluster Light List 里取
    uint lightIndex = GetAdditionalLightIndex(i, positionCS);
    Light light = GetAdditionalLight(lightIndex, positionWS);
#else
    // 从 per-object Light List 里取（最多 N 个）
    Light light = GetAdditionalLight(i, positionWS);
#endif
```

---

## Light Layer（URP 14+ / Unity 2022.2+）

Light Layer 是 URP 引入的渲染层概念，独立于 GameObject 的 Physics Layer，专门用于控制"哪盏灯影响哪些物体"。

### 问题背景

假设场景里有两组物体：主场景物体（接受太阳光）和一个特效光圈（只影响角色，不应该照亮地面）。用 Physics Layer + Light Culling Mask 可以实现，但 Culling Mask 影响的是 Camera 的可见性，不够精确，且会破坏 SRP Batcher 的合批。

Light Layer 的解法：

```
Rendering Layer Mask（每个 Renderer 上设置）：
  地面 Mesh:   Layer 0（默认）
  角色 Mesh:   Layer 0 + Layer 1

Light（每盏灯上设置 Rendering Layer Mask）：
  太阳:     照 Layer 0（地面和角色都受影响）
  特效光:   照 Layer 1（只影响角色）
```

### 配置方式

在 Pipeline Asset 里开启 Rendering Layers（URP 14+ 才有此选项）：

```
Pipeline Asset → Rendering Layers → Enable
```

然后在 Renderer 组件的 Rendering Layer Mask 里选择该物体所在的 Layer；在 Light 组件里设置它能影响的 Rendering Layer Mask。

两者的位运算交集不为空时，灯才影响该物体。

### 对 Shadow 的影响

Light Layer 同样影响阴影：只有物体和光源的 Rendering Layer Mask 有交集时，该物体才会向这盏灯的 Shadow Map 里写入（产生投影），也才会接受这盏灯的阴影。

---

## Light Cookie

Light Cookie 是一张纹理，贴在光源上作为"遮光模板"——光源颜色乘以 Cookie 纹理的颜色/透明度，产生复杂的光影图案。

**典型用途**：
- 窗户投下的光影格（Directional Light + Window Cookie）
- 聚光灯照在地面的光斑图案（Spot Light + Cookie）
- 街灯、舞台灯的复杂光型

### 在 URP 里的配置

URP 12（Unity 2021.2）起，Light Cookie 通过 Light 组件的 `Cookie` 字段直接赋值纹理：

```
Light 组件
  ├─ Cookie: [Texture2D / Cubemap]    ← 直接拖入
  ├─ Cookie Size: X 1 / Y 1           ← Directional Light 的 Cookie 覆盖尺寸（世界单位）
  └─ Use Cookie Alpha: 勾选           ← 用纹理 Alpha 控制光照强度（黑 = 无光，白 = 全光）
```

### Cookie Atlas

URP 把所有 Light Cookie 合并到一张 Atlas（Cookie Atlas），统一在 Shader 里采样，而不是每个灯独立绑定一张 Texture。Atlas 大小在 Pipeline Asset 里配置（默认 1024×1024，可以调到 2048×2048 或 4096×4096）。

Cookie 过多时（超出 Atlas 容量），远处的 Cookie 会被从 Atlas 里淘汰，离摄像机近的优先保留。

### Directional Light Cookie 的工作方式

Directional Light Cookie 投影到场景的方式类似正交投影：Cookie 纹理的 UV 从世界坐标沿光源方向投影到每个表面：

```hlsl
// Shader 里（URP 内部），Directional Light Cookie 采样
float2 cookieUV = TransformWorldToLightCookieUV(positionWS, light);
float cookieAttenuation = SampleMainLightCookie(cookieUV);
mainLight.color *= cookieAttenuation;
```

开发者不需要手动实现这段，使用 URP 提供的 `GetMainLight()` 或 `GetAdditionalLight()` 函数时，Cookie 遮挡已经内置在返回值里。

---

## URP 光照的 Shader 接口

了解这些内置函数，自定义 Shader 才能正确利用 URP 的光照系统：

```hlsl
// 主光（含阴影衰减 + Cookie）
Light mainLight = GetMainLight(inputData);
float3 lighting = mainLight.color * mainLight.distanceAttenuation
                * mainLight.shadowAttenuation;

// 附加光（Forward 路径）
int additionalLightsCount = GetAdditionalLightsCount();
for (int i = 0; i < additionalLightsCount; i++)
{
    Light light = GetAdditionalLight(i, positionWS, shadowMask);
    lighting += LightingLambert(light.color, light.direction, normalWS)
              * light.distanceAttenuation
              * light.shadowAttenuation;
}

// 也可以直接用内置的 PBR 光照函数
BRDFData brdfData;
InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);
float3 color = GlobalIllumination(brdfData, bakedGI, occlusion, positionWS, normalWS, viewDirWS);
color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirWS);
```

---

## 导读

- 上一篇：[URP 深度配置 03｜Camera Stack：Base Camera、Overlay Camera 与多摄像机组织]({{< relref "rendering/urp-config-03-camera-stack.md" >}})
- 下一篇：[URP 深度光照 02｜URP Shadow 深度：Cascade 机制、Shadow Atlas、Bias 调参]({{< relref "rendering/urp-lighting-02-shadow.md" >}})

## 小结

| 概念 | 要点 |
|---|---|
| 主光 | 唯一高质量 Directional Light；独占 Cascade Shadow Map；Shader 里用 `GetMainLight()` |
| 附加光 | 按距离排序取前 N 个逐像素，其余逐顶点；Forward+ 改为 Cluster 索引无上限 |
| Light Layer | URP 14+ 的渲染层，控制"哪盏灯照哪些物体"，独立于 Physics Layer |
| Light Cookie | 纹理遮光模板；URP 把所有 Cookie 合并到 Atlas；`GetMainLight()` 内置 Cookie 遮挡 |

下一篇（URP光照-02）专门深入 Shadow：Cascade 的分割机制、Shadow Atlas 的布局、Bias 调参的精确方法，以及移动端 Shadow 代价的具体数字。
