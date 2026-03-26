+++
title = "Shader 语法基础 03｜向量与矩阵：空间变换的几何含义"
slug = "shader-basic-03-vector-matrix"
date = 2026-03-26
description = "Shader 里的坐标变换为什么需要矩阵？理解模型矩阵、视图矩阵、投影矩阵的作用，法线矩阵为什么和模型矩阵不同，以及切线空间（TBN）是什么。"
[taxonomies]
tags = ["Shader", "HLSL", "URP", "语法基础", "向量", "矩阵", "坐标变换"]
series = ["Shader 手写技法"]
[extra]
weight = 4070
+++

坐标变换是 Shader 的核心机制。为什么顶点位置要乘矩阵？法线为什么不能用同一个矩阵？切线空间是干什么用的？这篇从几何直觉出发把这些问题讲清楚。

---

## 向量运算回顾

向量加减：两向量逐分量运算：

```hlsl
float3 a = float3(1, 0, 0);
float3 b = float3(0, 1, 0);
float3 c = a + b;   // float3(1, 1, 0)
```

**标量乘法**：缩放向量：

```hlsl
float3 v = float3(1, 2, 3) * 2.0;   // float3(2, 4, 6)
```

**点积（dot）**：

```hlsl
dot(a, b) = a.x*b.x + a.y*b.y + a.z*b.z
           = |a| * |b| * cos(θ)
```

点积结果是标量。归一化后的向量点积等于夹角的余弦——这就是为什么 `dot(N, L)` 能表示光照强度。

**叉积（cross）**：

```hlsl
cross(a, b) = 垂直于 a 和 b 的向量，长度 = |a| * |b| * sin(θ)
```

叉积用于求法线（两条边叉积）、构建切线空间。

---

## 坐标空间体系

顶点从模型文件到屏幕，经历了一系列空间变换：

```
物体空间 (Object Space)
    ↓  × 模型矩阵 (Model Matrix)
世界空间 (World Space)
    ↓  × 视图矩阵 (View Matrix)
观察空间 (View Space / Camera Space)
    ↓  × 投影矩阵 (Projection Matrix)
裁剪空间 (Clip Space)
    ↓  透视除法（GPU 自动完成）
NDC（标准化设备坐标）
    ↓  视口变换（GPU 自动完成）
屏幕空间 (Screen Space)
```

URP 把这些矩阵打包好了，可以直接用内置函数：

```hlsl
// 物体空间 → 裁剪空间（最常用，一步到位）
float4 posHCS = TransformObjectToHClip(positionOS.xyz);

// 物体空间 → 世界空间
float3 posWS = TransformObjectToWorld(positionOS.xyz);

// 世界空间 → 裁剪空间
float4 posHCS = TransformWorldToHClip(posWS);
```

---

## 矩阵是什么

4×4 矩阵可以同时表示**旋转、缩放、位移**。列向量写法下：

```
[sx*r00  r01  r02  tx]   [x]   [变换后的x]
[r10  sy*r11  r12  ty] × [y] = [变换后的y]
[r20  r21  sz*r22  tz]   [z]   [变换后的z]
[0    0    0     1  ]   [1]   [1        ]
```

最后一行 `[0,0,0,1]` 和 `w=1` 配合，让位移能用矩阵乘法表达——这就是齐次坐标。

HLSL 里手动乘矩阵：

```hlsl
float4x4 matrix;
float4   v = float4(pos, 1.0);
float4   result = mul(matrix, v);   // 矩阵在左，向量在右
```

---

## 法线矩阵：为什么不能用模型矩阵

**问题**：物体做了非均匀缩放（X 轴缩 2 倍，Y 轴不变），法线如果用同一个矩阵变换，就会偏转，不再垂直于表面。

**原因**：法线是方向向量，不是位置向量，它必须满足"垂直于表面"的约束。点积 `N · T = 0`（法线垂直于切线），经过缩放矩阵 M 变换后：

```
(M⁻¹)ᵀ × N  ·  M × T = N · T = 0
```

所以法线要用**模型矩阵的逆转置**（Inverse Transpose）来变换。

URP 封装好了这个：

```hlsl
// 正确的法线变换
float3 normalWS = TransformObjectToWorldNormal(normalOS);
// 内部等价于：mul((float3x3)UNITY_MATRIX_I_M_T, normalOS)
// 其中 I_M_T = 模型矩阵逆的转置
```

**结论**：永远不要用 `TransformObjectToWorld` 变换法线，要用 `TransformObjectToWorldNormal`。

---

## 切线空间（TBN）

法线贴图把光照细节存在贴图里，贴图上每个像素存的是"偏移后的法线方向"——但这个方向是相对于**表面自身**描述的，不是世界空间的方向。这个"表面自身的坐标系"就是切线空间。

切线空间由三个互相垂直的向量构成：

| 向量 | 方向 | 含义 |
|------|------|------|
| **T** Tangent（切线） | 沿 UV 的 U 方向 | 贴图横轴 |
| **B** Bitangent（副切线） | 沿 UV 的 V 方向 | 贴图纵轴 |
| **N** Normal（法线） | 垂直于表面 | 表面朝向 |

法线贴图里存的是切线空间下的法线方向（通常呈蓝紫色，因为 Z 轴为 1 对应颜色 (0.5, 0.5, 1)）。

使用法线贴图时需要把它从切线空间变换到世界空间：

```hlsl
// Vertex Shader 里构建 TBN 矩阵
VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
float3 T = normalInputs.tangentWS;
float3 B = normalInputs.bitangentWS;
float3 N = normalInputs.normalWS;

// Fragment Shader 里解码法线贴图并变换
float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
// 从切线空间变换到世界空间
float3 normalWS = normalize(T * normalTS.x + B * normalTS.y + N * normalTS.z);
```

或者直接用 URP 提供的 `TransformTangentToWorld`：

```hlsl
float3x3 TBN = float3x3(T, B, N);
float3 normalWS = TransformTangentToWorld(normalTS, TBN);
```

---

## URP 内置矩阵

不需要手动传矩阵，URP 提供了内置变量：

| 变量 | 含义 |
|------|------|
| `UNITY_MATRIX_M` | 模型矩阵（Object→World） |
| `UNITY_MATRIX_V` | 视图矩阵（World→View） |
| `UNITY_MATRIX_P` | 投影矩阵（View→Clip） |
| `UNITY_MATRIX_VP` | 视图×投影（World→Clip） |
| `UNITY_MATRIX_MVP` | 模型×视图×投影（Object→Clip） |
| `UNITY_MATRIX_I_M` | 模型矩阵的逆 |
| `UNITY_MATRIX_I_V` | 视图矩阵的逆（Camera 位置所在） |

实际写 Shader 时，用封装好的 `TransformXxx` 函数，不直接操作矩阵，更安全也更易读。

---

## 坐标空间辨识速查

| 场景 | 应该在哪个空间操作 | 原因 |
|------|-------------------|------|
| 顶点动画 | 物体空间 | 相对于模型本身，跟随物体移动 |
| 草地波浪相位 | 世界空间 | 多物体间坐标统一 |
| 光照计算（N·L） | 世界空间 | 光源在世界空间描述 |
| 法线贴图 | 切线空间（贴图）→ 世界空间（计算） | 贴图存的是切线空间偏移 |
| 屏幕效果 | NDC / 屏幕空间 | 基于像素位置 |

---

## 小结

| 概念 | 要点 |
|------|------|
| 坐标变换链 | 物体→世界→观察→裁剪，每步乘一个矩阵 |
| 法线矩阵 | 用逆转置，不能用模型矩阵；URP 用 `TransformObjectToWorldNormal` |
| 切线空间 | T/B/N 三轴，法线贴图存的是切线空间方向 |
| `mul(M, v)` | 矩阵在左，列向量在右 |
| 内置封装 | 优先用 `TransformXxx` 函数，不手动乘矩阵 |

下一篇：控制流与分支代价——为什么 GPU 里 `if` 是危险的，`step`/`lerp` 如何替代分支。
