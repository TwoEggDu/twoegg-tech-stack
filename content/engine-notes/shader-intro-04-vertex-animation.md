---
title: "Shader 手写入门 04｜让顶点动起来：Vertex Shader 动画"
slug: "shader-intro-04-vertex-animation"
date: "2026-03-26"
description: "在 Vertex Shader 里用 sin(_Time.y) 移动顶点位置，做出波浪和呼吸效果。理解顶点动画的执行时机、空间坐标操作方式，以及移动端的性能注意事项。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "入门"
  - "顶点动画"
  - "动画"
series: "Shader 手写技法"
weight: 4040
---
前几篇的改动都在 Fragment Shader 里——每个像素算颜色。这篇把目光转到 Vertex Shader：**在顶点阶段移动顶点位置**，让几何体本身产生动画。

草地、旗帜、水面、呼吸效果，都可以用这种方式实现。它的计算发生在光栅化之前，开销只和顶点数量有关，而不是像素数量。

---

## 顶点动画的基本思路

Vertex Shader 最终要输出 `SV_POSITION`——顶点在裁剪空间的位置。在做这个变换之前，先在**物体空间**里移动顶点，就能产生形变动画。

```hlsl
// 原始流程：
float4 posHCS = TransformObjectToHClip(positionOS);

// 加上顶点动画：先在物体空间偏移，再变换
positionOS.y += sin(_Time.y) * _Amplitude;
float4 posHCS = TransformObjectToHClip(positionOS);
```

移动发生在 `TransformObjectToHClip` 之前，GPU 就会用移动后的位置做后续计算——这就是顶点动画的本质。

---

## `_Time` 回顾

`_Time` 是 URP 内置的时间变量，四个分量分别是：

| 分量 | 值 | 常用场景 |
|------|----|----------|
| `_Time.x` | t / 20 | 超慢速动画 |
| `_Time.y` | t | 标准动画，最常用 |
| `_Time.z` | t × 2 | 快速动画 |
| `_Time.w` | t × 3 | 更快 |

`sin(_Time.y)` 每 2π 秒完成一次循环，约 6.28 秒。用 `_Speed` 乘以时间可以控制频率：`sin(_Time.y * _Speed)`。

---

## 示例一：上下呼吸动画

最简单的顶点动画——整个物体沿 Y 轴做正弦运动：

```hlsl
float offset = sin(_Time.y * _Speed) * _Amplitude;
positionOS.y += offset;
```

效果：物体整体上下浮动，像漂浮在水面上。

---

## 示例二：基于位置的波浪

草地、旗帜、水面的关键技巧：**每个顶点的相位不同**。用顶点的 X 坐标（或 Z 坐标）作为相位偏移，相邻顶点之间就会产生波浪形：

```hlsl
// 用顶点的世界 X 坐标做相位偏移
float wave = sin(_Time.y * _Speed + positionOS.x * _Frequency) * _Amplitude;
positionOS.y += wave;
```

`positionOS.x * _Frequency` 决定波的空间密度——值越大，同等长度内的波峰越多。

---

## 完整 Shader：波浪平面

```hlsl
Shader "Custom/VertexWave"
{
    Properties
    {
        _BaseColor  ("Base Color",  Color)  = (0.2, 0.8, 0.4, 1)
        _Amplitude  ("Amplitude",   Float)  = 0.1    // 振幅：波峰高度
        _Frequency  ("Frequency",   Float)  = 2.0    // 空间频率：波的密度
        _Speed      ("Speed",       Float)  = 2.0    // 时间频率：波的速度
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Amplitude;
                float  _Frequency;
                float  _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // ── 顶点动画：在变换前修改物体空间位置 ──────────────
                float wave = sin(_Time.y * _Speed + input.positionOS.x * _Frequency)
                             * _Amplitude;
                input.positionOS.y += wave;
                // ─────────────────────────────────────────────────────

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
```

把这个 Shader 赋给一个细分度较高的 Plane（Mesh 的顶点数越多，波浪越平滑），调整 `Amplitude`、`Frequency`、`Speed` 三个参数观察变化。

---

## 示例三：草地摆动

草地比单纯波浪多一个约束：**根部不动，顶部摆动**。用顶点的 Y 坐标作为权重：

```hlsl
// positionOS.y 在 0（根部）到 1（顶部）之间（取决于草的建模方式）
float weight = saturate(input.positionOS.y);   // 根部权重 0，顶部权重 1

float sway = sin(_Time.y * _Speed + input.positionOS.x * _Frequency)
             * _Amplitude * weight;            // 只有顶部才大幅摆动

input.positionOS.x += sway;
```

`saturate` 把值夹到 [0, 1]，确保即使模型 Y 坐标超出预期范围也不会出错。

---

## 顶点动画与法线

顶点位置变了，法线却没变——这会导致光照看起来不对（顶点在动，明暗没在动）。

**简单处理**：如果动画幅度小，忽略法线更新，大多数情况下视觉上可接受。

**精确处理**：对动画函数求导，得到切线，叉积出新法线。以波浪为例：

```hlsl
// 波浪函数 f(x) = sin(t * speed + x * freq) * amp
// 对 x 求导：df/dx = cos(t * speed + x * freq) * freq * amp
float dfdx = cos(_Time.y * _Speed + input.positionOS.x * _Frequency)
             * _Frequency * _Amplitude;

// 修正后的法线（简化版，适合 Y 轴位移的波浪）
float3 correctedNormal = normalize(float3(-dfdx, 1.0, 0.0));
```

入门阶段先跳过这个细节，理解位置动画本身即可。

---

## 世界空间 vs 物体空间做动画

上面的例子都在**物体空间**里做动画——用的是模型自己的坐标系。另一种方式是先变换到世界空间再偏移：

```hlsl
// 先拿到世界空间位置
float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

// 在世界空间里做动画
positionWS.y += sin(_Time.y + positionWS.x * _Frequency) * _Amplitude;

// 再变换到裁剪空间
output.positionHCS = TransformWorldToHClip(positionWS);
```

**区别**：
- 物体空间动画：随物体移动/旋转，动画效果相对于物体本身（旗帜随旗杆转）
- 世界空间动画：使用世界坐标做相位，不同位置的物体自然同步（整片草地的波浪方向一致）

草地、水面通常用世界空间，旗帜、角色附件通常用物体空间。

---

## 性能注意事项

**顶点动画只和顶点数量有关，和像素无关**。这一点在移动端有实际意义：

| 方案 | 代价 | 适合场景 |
|------|------|----------|
| 顶点动画（Shader） | 顶点数 × 计算量 | 草地、水面、旗帜 |
| 骨骼动画（CPU） | 骨骼数 × 蒙皮计算 | 角色、复杂形变 |
| 纹理动画（UV 滚动） | 几乎为零 | 水流、传送带纹理 |

草地场景里几千个草片用顶点动画是合理的；用骨骼动画就会爆掉 CPU。

**移动端注意**：`sin`、`cos` 在 GPU 上是快速硬件指令，不需要担心。真正的开销是顶点数——草地的 LOD 和密度控制比 Shader 本身更重要。

---

## 常见问题

**Q：动画效果抖动、不流畅**

Mesh 的顶点数太少。波浪动画需要足够的顶点密度才能显示出平滑曲线。可以在 Unity 里用 Plane 并调高 Subdivisions，或者导入高细分的模型。

**Q：Amplitude 为 0 但物体还是在动**

检查是否有其他组件（Animator、Transform 动画）同时在修改这个物体。

**Q：多个同材质的物体，希望各自独立摇摆**

用物体空间动画，不同位置的物体天然有不同的 `positionOS` 分布，相位自然错开。如果是世界空间动画，需要在 Properties 里加一个 `_PhaseOffset` 并赋不同值。

**Q：顶点动画能用 GPU Instancing 吗**

可以。只要 CBUFFER 遵守 SRP Batcher 规则，或者用 `UNITY_INSTANCING_BUFFER` 给每个实例传不同参数（比如不同的相位偏移），两者兼容。

---

## 小结

| 概念 | 要点 |
|------|------|
| 顶点动画原理 | 变换前修改 `positionOS`，GPU 用新位置做后续计算 |
| 波浪公式 | `sin(time * speed + position * frequency) * amplitude` |
| 根部锁定 | 用 Y 坐标作权重，`saturate(posOS.y)` 做遮罩 |
| 物体 vs 世界空间 | 物体空间随物体走；世界空间用于跨物体的统一效果 |
| 性能 | 开销 ∝ 顶点数，移动端控制草地密度比优化 Shader 更有效 |

---

入门层到这里结束。五篇覆盖了：ShaderLab 骨架、时间动画、UV 贴图采样、Lambert 光照、顶点动画。接下来进入**语法基础层**，系统补齐 HLSL 的数据类型、内置函数、宏体系和 Shader 变体机制。
