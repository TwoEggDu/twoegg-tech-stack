---
title: "Shader 手写入门 01｜让颜色动起来：_Time 与 Shader 的执行时机"
slug: "shader-intro-01-time-animation"
date: "2026-03-26"
description: "用 _Time 让颜色随时间变化，理解 Shader 每帧执行一次的机制。顺带讲清楚 sin/cos 在 Shader 里怎么用，以及为什么 Shader 里不能用 if 做时间判断。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "入门"
  - "动画"
  - "_Time"
series: "Shader 手写技法"
weight: 4010
---
上一篇写了一个显示纯色的 Shader。颜色是固定的——你在 Inspector 里改，物体颜色才变。这一篇让颜色自己动起来：不需要任何 C# 代码，Shader 自己随时间变化。

---

## _Time 是什么

`_Time` 是 Unity 内置的 Shader 变量，每帧由 CPU 自动传给所有 Shader。它是一个 `float4`，四个分量分别是：

| 分量 | 值 |
|------|---|
| `_Time.x` | time / 20 |
| `_Time.y` | time（秒数，游戏开始后的经过时间）|
| `_Time.z` | time × 2 |
| `_Time.w` | time × 3 |

最常用的是 `_Time.y`，就是以秒为单位的游戏时间。

---

## 让颜色随时间变化

```hlsl
Shader "Custom/TimeColor"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // sin(_Time.y) 的值在 -1 到 1 之间循环
                // * 0.5 + 0.5 把范围映射到 0 ~ 1
                half r = sin(_Time.y) * 0.5 + 0.5;
                half g = sin(_Time.y + 2.094) * 0.5 + 0.5;  // 相位偏移 2π/3
                half b = sin(_Time.y + 4.189) * 0.5 + 0.5;  // 相位偏移 4π/3
                return half4(r, g, b, 1.0);
            }

            ENDHLSL
        }
    }
}
```

赋给物体，不需要任何 C# 代码，物体颜色会在三原色之间循环流动。

---

## sin 为什么适合做循环动画

`sin(x)` 的输出在 -1 到 1 之间平滑循环，周期是 2π（约 6.28）。

```
sin(0)   = 0
sin(π/2) = 1    (约 1.57 秒)
sin(π)   = 0    (约 3.14 秒)
sin(3π/2)= -1   (约 4.71 秒)
sin(2π)  = 0    (约 6.28 秒，完成一次循环)
```

`* 0.5 + 0.5` 把 [-1, 1] 映射到 [0, 1]，因为颜色分量不能是负数。

**控制速度**：把 `_Time.y` 乘以一个系数：

```hlsl
half r = sin(_Time.y * 3.0) * 0.5 + 0.5;  // 快 3 倍
half r = sin(_Time.y * 0.5) * 0.5 + 0.5;  // 慢 2 倍
```

**控制相位差**：加不同的偏移量让 RGB 不同步：

```hlsl
// 2π/3 ≈ 2.094，三个颜色均匀错开 1/3 周期
half r = sin(_Time.y) * 0.5 + 0.5;
half g = sin(_Time.y + 2.094) * 0.5 + 0.5;
half b = sin(_Time.y + 4.189) * 0.5 + 0.5;
```

---

## Shader 的执行时机

理解 `_Time` 的关键是理解 **Shader 什么时候执行**。

每一帧，Unity 渲染每个可见物体时，都会调用一次这个物体材质的 Shader：
1. 对 Mesh 上的每个顶点，执行一次 `vert()`
2. 光栅化：确定三角形覆盖的像素
3. 对覆盖的每个像素，执行一次 `frag()`

所以 `frag()` 每帧被调用几十万甚至几百万次（取决于物体占屏幕的像素数）。`_Time.y` 在同一帧里对所有像素是同一个值（CPU 在帧开始时统一上传），但每帧值都不一样，所以颜色会随时间变化。

---

## 暴露速度参数到 Inspector

把速度和变化幅度提成 Property，方便调节：

```hlsl
Shader "Custom/TimeColor"
{
    Properties
    {
        _Speed      ("Speed",     Float) = 1.0
        _BaseColor  ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _Speed;
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                half pulse = sin(t) * 0.5 + 0.5;
                half3 color = lerp(_BaseColor.rgb * 0.2, _BaseColor.rgb, pulse);
                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}
```

`lerp(a, b, t)`：在 a 和 b 之间插值，t=0 返回 a，t=1 返回 b，t=0.5 返回中间值。这里用来在暗色和亮色之间做呼吸灯效果。

---

## 为什么 Shader 里不能用 _Time 做条件判断

你可能想写这样的代码：

```hlsl
// ❌ 看起来合理，但有问题
if (_Time.y > 3.0)
    return half4(1, 0, 0, 1);  // 3秒后变红
else
    return half4(0, 0, 1, 1);  // 3秒前是蓝色
```

这段代码在语法上是对的，能编译，也能跑。问题是：**GPU 不擅长分支**。

GPU 同时处理几十个像素（一个 Warp/Quad），所有像素执行同一条指令。遇到 `if` 时，如果不同像素的条件结果不同，GPU 必须两个分支都执行，只保留各自需要的结果——两条路径的代价都要付。

对于 `_Time.y > 3.0` 这种 Uniform 条件（所有像素的值相同），GPU 实际上可以优化：条件相同时整个 Warp 走同一个分支。所以这个特定写法性能损失不大。但这是个特例，而不是规则——养成用数学代替分支的习惯：

```hlsl
// ✅ 用 step 代替 if：step(3.0, _Time.y) 在 _Time.y >= 3 时返回 1，否则 0
half t = step(3.0, _Time.y);
return lerp(half4(0,0,1,1), half4(1,0,0,1), t);
```

`step(edge, x)`：x >= edge 返回 1，否则返回 0。没有分支，纯数学。

---

## 小结

- `_Time.y` 是游戏运行秒数，每帧由 CPU 统一传给所有 Shader
- `sin(_Time.y) * 0.5 + 0.5`：把 -1~1 的正弦波映射到 0~1，做循环动画
- `lerp(a, b, t)`：在两个值之间插值，是 Shader 里比 if-else 更常用的选择工具
- `step(edge, x)`：无分支的条件选择，x >= edge 时为 1
- Shader 每帧对每个像素执行一次 frag()，性能要求高，用数学代替分支

下一篇：给 Shader 加一张贴图，理解 UV 坐标是什么。
