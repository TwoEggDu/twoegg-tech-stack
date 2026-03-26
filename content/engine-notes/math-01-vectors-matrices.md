+++
title = "图形数学 01｜向量与矩阵：内存布局、列主序 vs 行主序、精度陷阱"
slug = "math-01-vectors-matrices"
date = 2026-03-26
description = "向量和矩阵是渲染管线的基础数据结构。这篇讲清楚它们在内存里的实际布局、列主序与行主序的差异对 HLSL/GLSL/C++ 代码的影响、以及 half/float/double 精度选择的陷阱。"
weight = 600
[taxonomies]
tags = ["数学", "图形学", "向量", "矩阵", "HLSL", "精度"]
[extra]
series = "图形数学"
+++

## 向量的内存布局

`float3` 在 C++ 里占 12 字节，`float4` 占 16 字节。GPU 硬件对 SIMD 操作有 16 字节对齐要求，所以即使声明 `float3`，编译器或驱动也经常把它 padding 到 16 字节。这在 cbuffer/uniform buffer 里是个经典陷阱：

```hlsl
// HLSL cbuffer 里，float3 后面跟 float 会错位
cbuffer PerObject : register(b0)
{
    float3 worldPos;   // 偏移 0，占 12 字节
    float  intensity;  // 偏移 12，总共 16 字节 — 恰好塞进一个 float4 slot，没问题
};

// 但这个会出错：
cbuffer PerObject : register(b0)
{
    float3 worldPos;   // 偏移 0
    float3 direction;  // 编译器把 direction 推到偏移 16（不是 12！）
};
```

HLSL 的 packing 规则是：每个变量不能跨 16 字节边界。`float3`（12 字节）后面再放一个 `float3` 时，第二个 `float3` 会被放到下一个 16 字节 slot 的开头（偏移 16），中间 4 字节被浪费。正确做法是改成 `float4`，或者显式控制 padding：

```hlsl
cbuffer PerObject : register(b0)
{
    float4 worldPosAndIntensity;  // xyz=worldPos, w=intensity
    float4 directionAndPad;       // xyz=direction, w=unused
};
```

C# 侧对应的 `Vector3`（12 字节）用 `SetVector` 传给 HLSL `float4` 时，第四个分量会被填为 0，不会引发问题。

---

## 行主序 vs 列主序

同一个 4×4 变换矩阵在内存里的字节序，因约定不同而相反。

**行主序（row-major）**：矩阵按行存储，`M[row][col]`，C/C++ 默认。矩阵的第一行 `[m00 m01 m02 m03]` 连续存在内存最前面。

**列主序（column-major）**：矩阵按列存储，`M[col][row]`，OpenGL/GLSL 默认。矩阵的第一列 `[m00 m10 m20 m30]` 连续存在内存最前面。

同一个平移矩阵，在行主序下平移分量在最后一行（`[tx ty tz 1]`），在列主序下在最后一列。

**HLSL 是行主序**，这直接影响 `mul()` 的语义：

```hlsl
// HLSL 里，mul(M, v) 等于数学上的 M * v（列向量）
// mul(v, M) 等于数学上的 v^T * M（行向量）

// Unity 的 UnityObjectToClipPos 本质上是：
float4 clipPos = mul(UNITY_MATRIX_MVP, float4(posOS, 1.0));
// 等同于：ClipPos = MVP * posOS（数学写法，列向量）
```

**GLSL 是列主序**，`mul` 对应的是列向量左乘：

```glsl
// GLSL 里 MVP * vec4(pos, 1.0) 就是数学意义上的矩阵乘列向量
gl_Position = MVP * vec4(posOS, 1.0);
```

两者在结果上等价（只要矩阵按对应约定构造），但如果手动在 C++ 里构造矩阵然后传给 Shader，就必须清楚自己用的是哪种约定。

### 转置规则

从一种主序切换到另一种时，需要转置矩阵。数学上 `(M^T)^T = M`，因此行主序下的矩阵 `M_row` 等于列主序下 `M_col` 的转置：

```cpp
// C++ 传矩阵给 OpenGL（列主序）
glm::mat4 model = ...;           // glm 默认列主序
glUniformMatrix4fv(loc, 1, GL_FALSE, &model[0][0]);  // GL_FALSE = 不转置

// 如果矩阵是行主序构造的：
glUniformMatrix4fv(loc, 1, GL_TRUE, &model[0][0]);   // GL_TRUE = 需要转置
```

Unity 的 `UNITY_MATRIX_M`、`UNITY_MATRIX_VP` 等内置矩阵已经处理好了行/列主序问题。但如果手动传自定义矩阵（比如 Compute Shader 里传逆 VP 矩阵用于世界坐标重建），需要注意：

```csharp
// C# 侧
Matrix4x4 invVP = (proj * view).inverse;
// Unity 的 Matrix4x4 是行主序，直接 SetMatrix 时
// HLSL 会以列主序解读 — 相当于收到了转置矩阵
// 正确做法是转置后传入：
material.SetMatrix("_InvVP", invVP.transpose);
```

---

## 常用向量操作 HLSL 快速参考

```hlsl
float3 a = float3(1, 0, 0);
float3 b = float3(0, 1, 0);

float  d  = dot(a, b);           // 点积，结果标量，判断夹角
float3 c  = cross(a, b);         // 叉积，结果垂直于 a 和 b
float3 n  = normalize(a);        // 单位向量，length = 1
float  l  = length(a);           // 向量长度
float3 r  = reflect(-L, N);      // 反射向量，用于高光计算
float3 lr = lerp(a, b, 0.5);     // 线性插值
float  sc = saturate(dot(N, L)); // 钳制到 [0,1]，等价于 clamp(x, 0, 1)
float  sq = rsqrt(l);            // 快速倒数平方根，比 1/sqrt() 快
```

`dot(N, L)` 是漫反射计算的核心；`cross(tangent, normal)` 用于构建 TBN 矩阵；`reflect` 和 `refract` 直接对应 Phong/Snell 公式。

---

## 精度陷阱：half / float / double

| 类型     | 位数 | 精度（十进制）| 范围             | 适用场景          |
|----------|------|--------------|------------------|-------------------|
| `half`   | 16   | ~3 位        | ±65504           | UV、法线、颜色    |
| `float`  | 32   | ~7 位        | ±3.4 × 10^38     | 世界坐标、矩阵    |
| `double` | 64   | ~15 位       | ±1.8 × 10^308    | CPU 物理模拟      |

**half 的常见错误**：

```hlsl
// 错误：大地图世界坐标用 half，坐标 > 1000 时精度只剩 0.5 米，出现顶点抖动
half3 worldPos = TransformObjectToWorld(posOS);  // 精度不足

// 正确：世界坐标始终用 float
float3 worldPos = TransformObjectToWorld(posOS);
```

`half` 能表示的最大精确整数是 2048（`half` 的尾数只有 10 位，2^10 = 1024，含符号后能精确到 ±2048 的整数）。坐标超出这个范围后，每步精度开始以倍数衰减。

移动平台 GPU（Mali、Adreno）对 `half` 有原生硬件支持，用 `half` 替代 `float` 可以明显提升吞吐（某些架构下 ALU 吞吐翻倍）。颜色值（0~1）、UV（0~1）、法线（-1~1）完全可以用 `half`；世界坐标、深度值、变换矩阵必须用 `float`。

GPU 里的 `double` 支持因平台而异：桌面 GPU 通常支持，但吞吐只有 `float` 的 1/2 到 1/64；移动 GPU 基本不支持。图形代码里几乎不用 `double`，精度需求靠算法（如 Camera-Relative Rendering）解决，而不是靠提升类型精度。

---

## 小结

- `float3` cbuffer 对齐陷阱：不跨 16 字节边界，老老实实用 `float4` 打包。
- HLSL 行主序，GLSL 列主序；`mul(M, v)` 和 `mul(v, M)` 在 HLSL 里含义相反。
- 手动传矩阵给 Unity Shader 时记得 `matrix.transpose`。
- `half` 给 UV/颜色/法线用，世界坐标和矩阵必须 `float`。
- `dot`、`cross`、`normalize`、`saturate` 是最高频的向量操作，烂熟于心。
