---
title: "Shader 手写入门 03｜加上光照：Lambert 漫反射"
slug: "shader-intro-03-lambert"
date: "2026-03-26"
description: "从 Unlit 到 Lit 的关键改动：读入法线、获取主光方向、用 dot(N,L) 计算漫反射。理解法线从物体空间到世界空间的变换，以及 URP 里怎么拿到主光源数据。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "入门"
  - "光照"
  - "Lambert"
series: "Shader 手写技法"
weight: 4030
---
前两篇的 Shader 是 Unlit——不受光照影响，每个像素的颜色只由贴图或固定颜色决定。实际游戏里大多数物体需要响应灯光：光照到的地方亮，背面暗。这篇加上最基础的漫反射，让 Shader 有了立体感。

---

## 漫反射的物理直觉

Lambert 漫反射是最简单的光照模型，对应的物理现象是：**光线越垂直打到表面，该点越亮**。

用数学表达：亮度 = `max(0, dot(法线方向, 光线方向))`

- 法线正对光源：`dot = 1`，最亮
- 法线与光线垂直：`dot = 0`，完全暗
- 法线背对光源：`dot < 0`，`max` 夹到 0，不产生负光

这个公式叫 **N·L**（Normal dot Light），是所有光照模型的基础。

---

## 从 Unlit 到 Lit 需要改什么

| 要素 | Unlit | Lit（Lambert） |
|------|-------|----------------|
| Tags | 无 LightMode | `LightMode = UniversalForward` |
| 顶点输入 | 位置、UV | 位置、UV、**法线** |
| Include | Core、ShaderVariablesFunctions | 加上 **Lighting.hlsl**、**Shadows.hlsl** |
| Vertex Shader | 变换位置，传 UV | 额外变换法线到世界空间 |
| Fragment Shader | 采样贴图返回颜色 | 获取主光，计算 N·L，乘以漫反射颜色 |

关键点有两个：**法线要变换到世界空间**，**主光数据要从 URP 的 API 里读**。下面逐步拆解。

---

## 法线：为什么要变换空间

顶点的法线存在**物体空间**（Object Space）里——也就是模型自身的坐标系。但光源方向是在**世界空间**（World Space）里描述的。两个向量在不同坐标系里，直接 dot 没有意义。

所以需要把法线变换到世界空间，再和世界空间的光源方向做 dot。

URP 提供了一个工具函数，把这个变换封装好了：

```hlsl
VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
float3 normalWS = normalInputs.normalWS;
```

如果不需要切线空间（这篇不需要），可以只传法线：

```hlsl
// 简化版：只变换法线
float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
```

`TransformObjectToWorldNormal` 内部处理了非均匀缩放的问题（用法线矩阵而不是模型矩阵），直接用就好。

---

## 主光源：URP 怎么拿到灯光数据

URP 把场景主光源的数据打包成一个结构体 `Light`，通过 `GetMainLight()` 获取：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 在 Fragment Shader 里
Light mainLight = GetMainLight();
float3 lightDir = mainLight.direction;   // 世界空间，从表面指向光源（已归一化）
float3 lightColor = mainLight.color;     // 光源颜色 × 强度
```

注意 `mainLight.direction` 已经是**从表面指向光源**的方向，和法线 dot 直接得到正确结果，不需要反转。

---

## 完整 Shader

```hlsl
Shader "Custom/LambertLit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap   ("Base Map",   2D)    = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Opaque"
            "RenderPipeline"   = "UniversalPipeline"
            "Queue"            = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // 阴影关键字（让物体接收阴影用，这篇先带上，下篇深讲）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── 常量缓冲区 ──────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // ── 顶点输入 ─────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;       // ← 新增：物体空间法线
                float2 uv         : TEXCOORD0;
            };

            // ── 顶点输出（传给片元） ──────────────────────────────────
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;   // ← 新增：世界空间法线
                float2 uv          : TEXCOORD1;
            };

            // ── Vertex Shader ────────────────────────────────────────
            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // 法线变换：物体空间 → 世界空间
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // ── Fragment Shader ──────────────────────────────────────
            half4 frag(Varyings input) : SV_Target
            {
                // 1. 法线归一化（插值后长度会偏移，需重新归一化）
                float3 normalWS = normalize(input.normalWS);

                // 2. 采样贴图，乘以颜色属性
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                baseColor *= _BaseColor;

                // 3. 获取主光源
                Light mainLight = GetMainLight();

                // 4. Lambert N·L
                float NdotL = max(0.0, dot(normalWS, mainLight.direction));

                // 5. 最终颜色：漫反射颜色 × 光源颜色 × N·L
                half3 diffuse = baseColor.rgb * mainLight.color * NdotL;

                return half4(diffuse, baseColor.a);
            }
            ENDHLSL
        }
    }
}
```

---

## 每一步做了什么

**法线归一化**

顶点法线从 Vertex Shader 传到 Fragment Shader 时，GPU 对相邻顶点之间的值做线性插值。插值后的向量长度不再是 1，必须重新 `normalize`，否则 dot 的结果会偏小。

**`TRANSFORM_TEX`**

和前篇一样，把 Tiling/Offset 应用到 UV 上，这行不要忘。

**`#pragma multi_compile _ _MAIN_LIGHT_SHADOWS ...`**

这两行关键字声明让 Shader 能接收阴影。这篇不深讲阴影，但如果不加，场景里开了阴影的地方会显示错误。先把这两行模板记住，下篇再细讲。

**`mainLight.color`**

注意最终结果是 `baseColor × lightColor × NdotL`，不是 `baseColor × NdotL`。光源是白色（1,1,1）时没有区别，但换成彩色灯光时，光色会影响最终效果——这是正确的物理行为。

---

## 效果对比

| | Unlit | Lambert |
|--|-------|---------|
| 球体正面 | 纯色平铺 | 亮部清晰 |
| 球体背面 | 同样亮度 | 完全暗 |
| 转动灯光 | 无变化 | 明暗跟着转 |
| 立体感 | 无 | 有 |

---

## 目前缺什么

这个 Lambert Shader 还不完整，它缺少：

1. **阴影投射（ShadowCaster Pass）**：场景里有其他物体，它无法向地面投阴影
2. **环境光**：背面完全黑，真实情况下背面还有天光/反弹光
3. **附加光源**：点光、聚光都被忽略了

这些在后面的篇章里逐步加入。这篇的目标是先跑通最小光照流程，把 N·L 理解清楚。

---

## 常见问题

**Q：法线朝向不对，球体明暗反了**

检查建模软件导出时有没有翻转 Y 轴。Unity 导入 FBX 时，部分软件导出的法线需要勾选 Import Settings 里的 Normals → Calculate。

**Q：整个物体全黑**

`normalWS` 传入前没有 `normalize`，或者法线变换用了 `TransformObjectToWorld`（位移变换）而不是 `TransformObjectToWorldNormal`（法线变换）。法线不能用普通矩阵变换。

**Q：有光照但没阴影（光照到处都亮）**

ShadowCaster Pass 缺失，或者 `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS` 没加。下一篇补上。

**Q：去掉贴图只想用纯颜色**

把 `_BaseMap` 对应的贴图槽留默认值（白色），`baseColor` 就等于 `_BaseColor`，效果和纯色一样。

---

## 小结

| 概念 | 要点 |
|------|------|
| Lambert 漫反射 | `max(0, dot(N, L))`，N 法线，L 光方向 |
| 法线空间变换 | `TransformObjectToWorldNormal`，不能用普通模型矩阵 |
| 插值后归一化 | `normalize(input.normalWS)`，Fragment 里必须做 |
| 获取主光源 | `GetMainLight()`，方向已是从表面指向光源 |
| LightMode Tag | `UniversalForward`，Unlit 不需要，Lit 必须加 |

下一篇：在 Vertex Shader 里移动顶点——用 `sin(_Time.y)` 做波浪动画。
