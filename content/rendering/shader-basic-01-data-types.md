---
title: "Shader 语法基础 01｜数据类型与精度：half、float、int 怎么选"
slug: "shader-basic-01-data-types"
date: "2026-03-26"
description: "HLSL 里的数据类型不只有 float。理解 half/float/int 的精度范围、swizzle 操作、向量分量访问，以及移动端为什么 half 更重要。"
tags:
  - "Shader"
  - "HLSL"
  - "URP"
  - "语法基础"
  - "数据类型"
  - "精度"
series: "Shader 手写技法"
weight: 4050
---
入门层写 Shader 时，所有变量都用了 `float`。这没问题，但不是最优解。HLSL 提供了多种数据类型，选对类型对移动端性能有直接影响。

---

## 三种基本数值类型

| 类型 | 位宽 | 范围 / 精度 | GPU 上的实际含义 |
|------|------|------------|-----------------|
| `float` | 32 位 | ±3.4×10³⁸，约 7 位十进制精度 | IEEE 754 单精度浮点 |
| `half` | 16 位 | ±65504，约 3 位十进制精度 | IEEE 754 半精度浮点 |
| `int` | 32 位 | ±2³¹ | 整数，不支持小数 |
| `uint` | 32 位 | 0 ~ 2³² | 无符号整数 |
| `bool` | 1 位逻辑 | true / false | GPU 通常当 int 处理 |

移动端 GPU（Adreno、Mali、Apple GPU）有专门的 16 位浮点运算单元，使用 `half` 时吞吐量通常是 `float` 的 **2 倍**。

---

## 向量类型

数值类型后面加数字表示向量：

```hlsl
float   x;       // 标量
float2  uv;      // 2 分量向量
float3  normal;  // 3 分量向量
float4  color;   // 4 分量向量

half3   col;     // half 精度的 3 分量向量
int2    coord;   // 整数 2 分量向量
```

---

## Swizzle：向量分量访问

HLSL 允许用 `.xyzw` 或 `.rgba` 访问向量的任意分量，并可以重排：

```hlsl
float4 v = float4(1, 2, 3, 4);

v.x      // 1，取单个分量
v.xy     // float2(1, 2)，取前两个
v.zw     // float2(3, 4)
v.xyz    // float3(1, 2, 3)

// 重排（swizzle）
v.zyxw   // float4(3, 2, 1, 4)，分量换位
v.xxxx   // float4(1, 1, 1, 1)，重复分量

// rgba 写法等价
v.r      // 等同于 v.x
v.rgba   // 等同于 v.xyzw
```

颜色处理时用 `.rgba` 语义更清晰；方向向量用 `.xyz`。两种写法在编译器看来完全相同。

---

## 矩阵类型

```hlsl
float4x4  mvp;    // 4×4 矩阵（行×列）
float3x3  normal; // 3×3 矩阵
float4x2  m;      // 4 行 2 列矩阵
```

矩阵用 `[row][col]` 或 `._m00`、`._m01` 等方式访问元素：

```hlsl
float4x4 m;
m[0][0]   // 第 0 行第 0 列
m._m00    // 等价写法
m._m03    // 第 0 行第 3 列（最后一列 = 位移分量）
```

---

## 类型转换

HLSL 中数值类型之间可以显式转换：

```hlsl
float f = 1.5;
half  h = (half)f;   // float → half，可能丢失精度
int   i = (int)f;    // float → int，截断小数部分（不是四舍五入）

// 向量也可以转换
float3 fv = float3(1, 2, 3);
half3  hv = (half3)fv;
```

隐式转换（不写括号）在 HLSL 里通常也能通过编译，但推荐显式写清楚，避免意外的精度损失。

---

## half 的适用场景

`half` 精度范围是 ±65504，精度约 0.001。适合以下场景：

| 场景 | 适合用 half | 原因 |
|------|------------|------|
| 颜色值 | ✅ | 0~1 范围，half 精度足够 |
| 法线向量 | ✅ | -1~1 范围，精度足够 |
| UV 坐标 | ✅（通常） | 0~1 范围，除非贴图极大 |
| 光照系数（NdotL） | ✅ | 0~1 |
| 世界空间坐标 | ❌ | 场景较大时超出 ±65504 |
| 时间 `_Time.y` | ❌ | 运行时间过长后超出精度范围 |
| 矩阵变换 | ❌ | 需要 float 精度，避免顶点抖动 |

**`_Time.y` 的坑**：`_Time.y` 随游戏时间线性增长，运行几小时后超过 65504，`half` 存不下会产生抖动或跳变。永远用 `float` 存时间。

---

## 实际写法建议

```hlsl
// Fragment Shader 里的颜色计算——用 half
half4 frag(Varyings input) : SV_Target
{
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    half  NdotL = saturate(dot(normalWS, lightDir));
    half3 diffuse = color.rgb * NdotL;
    return half4(diffuse, color.a);
}

// 顶点变换——用 float（矩阵运算精度要求高）
Varyings vert(Attributes input)
{
    float3 posWS = TransformObjectToWorld(input.positionOS.xyz);  // float
    output.positionHCS = TransformWorldToHClip(posWS);           // float
    output.normalWS = TransformObjectToWorldNormal(input.normalOS); // float
    // ... 传给 fragment 的法线可以在 fragment 里转 half
}
```

一句话原则：**顶点变换和坐标计算用 float；颜色、光照系数、法线插值用 half**。

---

## 常见问题

**Q：PC 上 half 和 float 有区别吗**

桌面 GPU 通常把 `half` 当 `float` 处理（硬件不区分），所以 PC 上用 `half` 没有性能收益，也不会变差。收益主要在移动端。

**Q：SV_Position 用 half 可以吗**

不行。`SV_POSITION` 必须是 `float4`，这是硬件要求。

**Q：CBUFFER 里的属性用 half 还是 float**

推荐 `float`。CBUFFER 上传时是 CPU 数据，强制用 float；在 fragment 里读取后再转 half。

---

## 小结

| 类型 | 位宽 | 适合 | 不适合 |
|------|------|------|--------|
| `float` | 32 | 坐标、矩阵、时间 | — |
| `half` | 16 | 颜色、法线、UV、光照系数 | 坐标、时间 |
| `int/uint` | 32 | 循环计数、纹理索引、位运算 | 连续数值计算 |

下一篇：HLSL 内置数学函数——`saturate`、`lerp`、`step`、`smoothstep`、`frac` 各自能做什么。
