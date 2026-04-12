---
title: "Shader 核心技法 12｜顶点色驱动：让美术直接在模型上刷数据"
slug: "shader-technique-12-vertex-color"
date: "2026-03-26"
description: "顶点色（Vertex Color）是 Shader 与美术之间的数据通道。R/G/B/A 四通道各自存不同含义的遮罩数据，驱动草地弯曲权重、多材质混合、AO、顶点动画强度。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "技法"
  - "顶点色"
  - "Vertex Color"
  - "遮罩"
series: "Shader 手写技法"
weight: 4280
---
顶点色是 Shader 里一种特殊的数据通道——每个顶点存一个 RGBA 颜色，在 Fragment Shader 里插值后可以用作遮罩、权重、混合系数。它不需要额外的贴图，由美术直接在 DCC 工具（Blender/Maya）或 Unity 里刷入。

---

## 顶点色的本质

顶点色不是"颜色"，而是**每顶点的 4 通道数据**，取值范围 [0, 1]。RGBA 四个通道各自可以存不同语义的信息：

| 通道 | 常见用法 |
|------|---------|
| R | 顶点动画权重（草地根部=0，顶部=1） |
| G | 湿润遮罩（0=干燥，1=湿润） |
| B | AO（环境光遮蔽，0=深阴影，1=无遮挡） |
| A | 多贴图混合权重（0=材质A，1=材质B） |

这只是惯例，实际含义完全由你定义。

---

## 在 Shader 里读取顶点色

```hlsl
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    float4 color      : COLOR;       // ← 顶点色
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv          : TEXCOORD0;
    half4  vertexColor : TEXCOORD1;  // ← 传给 Fragment
};

Varyings vert(Attributes input)
{
    Varyings output;
    // ...
    output.vertexColor = input.color;
    return output;
}

half4 frag(Varyings input) : SV_Target
{
    half4 vc = input.vertexColor;
    // vc.r, vc.g, vc.b, vc.a 各自用于不同目的
}
```

---

## 用法一：草地弯曲权重

顶点色 R 通道存弯曲权重：根部（接地）= 0，顶部 = 1。

```hlsl
// Vertex Shader 里：
float bendWeight = input.color.r;   // 0 = 根部不动，1 = 顶部全力摆动

float sway = sin(_Time.y * _Speed + input.positionOS.x * _Frequency) * _Amplitude;
input.positionOS.x += sway * bendWeight;
```

美术在 DCC 工具里把草片根部顶点刷成黑色（r=0），顶部刷成白色（r=1），Shader 直接读取，无需额外参数传递。

---

## 用法二：多贴图混合（地形混合）

顶点色 A 通道控制两种地面材质的混合（草地 vs 泥土）：

```hlsl
half a = input.vertexColor.a;   // 0=纯草，1=纯泥土

half4 grassSample = SAMPLE_TEXTURE2D(_GrassTex,  sampler_GrassTex,  input.uv);
half4 dirtSample  = SAMPLE_TEXTURE2D(_DirtTex,   sampler_DirtTex,   input.uv);

// 用顶点色 alpha 插值两种材质
half4 albedo = lerp(grassSample, dirtSample, a);
```

这是地形着色的轻量方案——比 Unity 内置地形系统更灵活，比多层贴图采样更可控。

---

## 用法三：顶点 AO

美术在 DCC 里烘焙环境遮蔽（AO）到顶点色 B 通道（0=被遮挡的暗处，1=无遮挡的亮处）：

```hlsl
half ao = input.vertexColor.b;   // 顶点 AO

// 叠加到间接光照或整体颜色
half3 ambient = half3(0.1, 0.1, 0.1) * ao;   // 暗处减弱环境光
finalColor.rgb = directLight + ambient;
```

比贴图 AO 内存占用更低，精度取决于顶点密度，适合不需要极高精度的场景物件。

---

## 用法四：湿润/雪覆盖效果

顶点色 G 通道存湿润程度，用于动态天气效果（程序刷或美术预刷）：

```hlsl
half wetness = input.vertexColor.g;   // 0=干燥，1=完全湿润

// 湿润时：粗糙度降低，颜色变深
half smoothness = lerp(_DryRoughness, _WetRoughness, wetness);
half3 wetColor  = lerp(albedo.rgb, albedo.rgb * 0.7, wetness);  // 湿润颜色更深
```

运行时通过 Compute Shader 或 CPU 动态修改顶点色，实现雨水效果的动态传播。

---

## 用法五：受击/死亡效果权重

顶点色 R 通道存角色部位权重，驱动溶解、变色、震动等受击反馈：

```hlsl
// 美术刷：头=1，躯干=0.5，腿=0
half hitWeight = input.vertexColor.r;

// 受击时：高权重部位先溶解
float dissolve = hitWeight * _HitProgress;
clip(noise - dissolve);

// 或者受击变色
half3 hitColor = lerp(albedo.rgb, _HitFlashColor.rgb, hitWeight * _HitFlash);
```

---

## 在 Unity 里查看和刷顶点色

**查看**：Scene View → Shading Mode → `Vertex Color`（显示模型的顶点色）。

**刷颜色工具**：
- Unity 内置地形系统有顶点色刷工具
- 第三方：Polybrush（Unity 官方插件，免费）
- DCC 工具：Blender 的顶点色绘制模式，导出时保留

**程序生成**：

```csharp
Mesh mesh = GetComponent<MeshFilter>().mesh;
Color[] colors = new Color[mesh.vertexCount];
for (int i = 0; i < mesh.vertexCount; i++)
{
    float y = mesh.vertices[i].y;           // 按高度赋值
    colors[i] = new Color(y, 0, 0, 1);      // R 通道存高度
}
mesh.colors = colors;
```

---

## 性能说明

顶点色是免费的数据通道——它存在顶点缓冲里，读取时没有额外的纹理采样开销，只有插值（GPU 自动完成）。唯一代价是：每顶点多 16 字节（float4）的顶点缓冲内存占用。

对于顶点数较多的 Mesh（草地、地形），这个开销很小。

---

## 小结

| 通道 | 典型用途 | 值的含义 |
|------|---------|---------|
| R | 动画权重、受击权重 | 0=不动/不受影响，1=全力/完全受影响 |
| G | 湿润、雪覆盖 | 0=干燥/无覆盖，1=完全湿润/覆盖 |
| B | AO、遮蔽 | 0=完全遮挡，1=无遮挡 |
| A | 材质混合 | 0=材质A，1=材质B |

| 读取方式 | 代码 |
|---------|------|
| 顶点输入 | `float4 color : COLOR` |
| 传给片元 | 通过 `TEXCOORD` 插值 |
| 性能 | 零额外纹理采样，只有内存占用 |

---

核心技法层到这里全部完成。12 篇涵盖了游戏 Shader 开发中最频繁遇到的实用效果。下一层进入**进阶技法**——深度交叉（软粒子）、屏幕空间反射、Stencil 高级用法、自定义后处理，以及移动端优化专题。
