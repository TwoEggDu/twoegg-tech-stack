---
title: "渲染入门：顶点为什么要经过五个坐标系"
slug: "unity-rendering-00b-coordinate-spaces"
date: "2025-01-26"
description: "从模型空间到屏幕像素，一个顶点经历的五个坐标系各自解决什么问题，MVP 矩阵是什么，NDC 和屏幕坐标的关系，以及 UV 坐标作为独立 2D 空间的作用。"
tags:
  - "Unity"
  - "坐标系"
  - "MVP变换"
  - "空间变换"
  - "渲染基础"
series: "Unity 渲染系统"
weight: 150
---
如果只用一句话概括这篇：一个顶点从"模型文件里的原始坐标"变成"屏幕上的像素"，需要经过五次坐标系转换——每次转换都在解决一个具体问题，理解这五次转换，就理解了渲染管线前半段的全部工作。

---

## 从上一篇出发

00a（渲染系统入门）说明了 Vertex Shader 的工作是"把顶点变换到屏幕坐标"。但为什么需要变换？直接用世界坐标画出来不行吗？

这篇把这个问题说清楚。

---

## 为什么需要多个坐标系

先想一个日常场景：

你做了一把椅子的 3D 模型。模型文件里，椅子的中心在原点 `(0, 0, 0)`，椅背高 `1` 米，椅腿长 `0.5` 米。

现在你把这把椅子放进游戏场景，摆在位置 `(5, 0, 3)`，旋转了 45 度。场景里还有另外 10 把一样的椅子，位置各不同。

**问题一：** 模型文件里的 `(0, 0, 0)` 和场景里的 `(0, 0, 0)` 是同一个点吗？不是。模型文件里的坐标是"相对于椅子自己的中心"，场景里的坐标是"相对于整个世界"。需要一次变换来转换。

**问题二：** 玩家的摄像机可能在场景里的任何位置，朝任意方向。GPU 怎么知道这把椅子在摄像机视野里的哪个位置？需要再做一次变换，把坐标转成"相对于摄像机"的表示。

**问题三：** 摄像机有视角范围（FOV）、近裁剪面、远裁剪面。视野外的物体不应该渲染，但 GPU 怎么高效地判断一个顶点是否在视野内？需要再做一次变换，把坐标规范化成方便做这个判断的形式。

这就是为什么需要五个坐标系：每个坐标系都在解决一个具体问题。

---

## 五个坐标系

```
模型空间（Object Space）
    ↓ × 模型矩阵 M（Transform 决定）
世界空间（World Space）
    ↓ × 视图矩阵 V（Camera 决定）
观察空间（View Space / Camera Space）
    ↓ × 投影矩阵 P（Camera FOV 等决定）
裁剪空间（Clip Space）
    ↓ 透视除法（硬件自动完成）
NDC（归一化设备坐标，Normalized Device Coordinates）
    ↓ 视口变换（硬件自动完成）
屏幕空间（Screen Space / 像素坐标）
```

MVP = Model × View × Projection，这三个矩阵合在一起就是"把顶点从模型空间变换到裁剪空间"的完整变换。

---

## 第一步：模型空间 → 世界空间（模型矩阵 M）

**模型空间（Object Space）**：顶点坐标相对于模型自身中心点的坐标，存在模型文件（Mesh）里。

你在建模软件里建椅子，椅子中心是原点，不管椅子以后被放在场景哪里，Mesh 里存的坐标始终是这个"以椅子为中心"的坐标。

```
模型空间坐标：椅腿顶部 = (0.2, 0.5, 0.2)  ← 相对于椅子中心
```

**世界空间（World Space）**：所有物体都放在同一个坐标系里，原点是世界的绝对中心。

**模型矩阵 M** = 你在 Unity Inspector 里给 GameObject 设置的 Transform（Position + Rotation + Scale）编码成的 4×4 矩阵。

```csharp
// C# 侧：你改的是 Transform
transform.position = new Vector3(5, 0, 3);
transform.rotation = Quaternion.Euler(0, 45, 0);
transform.localScale = Vector3.one;
```

```hlsl
// Vertex Shader 侧：Unity 自动把这个 Transform 编码成矩阵传入
// UNITY_MATRIX_M 就是这把椅子的模型矩阵
float3 worldPos = mul(UNITY_MATRIX_M, float4(objectPos, 1.0)).xyz;
// 椅腿顶部从 (0.2, 0.5, 0.2) 变换到了世界坐标 (5.something, 0.5, 3.something)
```

场景里的 10 把椅子用同一个 Mesh，但每把有不同的模型矩阵 M——这就是为什么同一个 Mesh 文件能在场景不同位置出现。

---

## 第二步：世界空间 → 观察空间（视图矩阵 V）

**观察空间（View Space）**：坐标系原点在摄像机位置，Z 轴指向摄像机朝向（通常 -Z 是"前方"）。

为什么需要这一步？GPU 做透视投影时，计算"一个点在摄像机看来有多远、在视野哪个方向"这件事，在观察空间里计算最方便（摄像机就是原点）。

**视图矩阵 V** = Camera 的 Transform 的逆矩阵（把世界坐标系"搬到"摄像机中心）。

```hlsl
// UNITY_MATRIX_V 是视图矩阵，Unity 根据 Camera 的 Transform 自动生成
float3 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0)).xyz;
// 现在 (0, 0, -5) 表示"在摄像机正前方 5 个单位"
// (2, 0, -5) 表示"在摄像机正前方 5 单位，偏右 2 单位"
```

---

## 第三步：观察空间 → 裁剪空间（投影矩阵 P）

**裁剪空间（Clip Space）**：这个坐标系专门为了让 GPU 硬件判断"顶点是否在视野内"而设计的。

**投影矩阵 P** 由两个 Camera 参数决定：

- `FOV`（视野角度）：决定视锥的宽度
- `Near` / `Far`（近/远裁剪面）：决定可见的深度范围

透视投影矩阵把视锥体变形成一个标准的立方体：

```
透视投影前（观察空间视锥）：     透视投影后（裁剪空间）：
      /|                            |‾‾‾‾|
    /  |                            |    |
  /    |  ← FOV 决定宽度            |    |
摄像机  |                            |____|
  \    |                           (-1,-1,-1) 到 (1,1,1) 的立方体
    \  |
      \|
```

变换到裁剪空间后，GPU 只需要判断顶点的 x、y、z 是否在 [-w, w] 范围内（其中 w 是裁剪坐标的第四分量），就能决定是否裁剪，非常高效。

```hlsl
// UNITY_MATRIX_P 是投影矩阵
float4 clipPos = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));
// clipPos.xyz / clipPos.w 在 [-1, 1] 范围内 → 在视野内
// clipPos.xyz / clipPos.w 超出 [-1, 1] → 在视野外，裁剪掉
```

三个矩阵通常合并成一个 MVP 矩阵一次性计算：

```hlsl
// Unity 预先计算好了 M×V×P 的乘积
float4 clipPos = mul(UNITY_MATRIX_MVP, float4(objectPos, 1.0));

// 也可以写成更通用的形式（Unity 推荐）：
float4 clipPos = TransformObjectToHClip(objectPos); // 内置函数，本质是乘 MVP
```

---

## 第四步：裁剪空间 → NDC（透视除法）

**NDC（Normalized Device Coordinates，归一化设备坐标）**：x、y、z 都在 [-1, 1] 范围内的统一坐标系。

这一步由 GPU 硬件自动完成，不需要 Shader 代码：

```
NDC 坐标 = 裁剪坐标.xyz / 裁剪坐标.w
```

除以 w 的意义：透视投影会把远处的物体压缩（产生近大远小效果），w 分量存储了深度信息，除以 w 就完成了透视压缩。

```
NDC 结果：
  x ∈ [-1, 1]：-1 是屏幕左边，+1 是屏幕右边
  y ∈ [-1, 1]：-1 是屏幕下边，+1 是屏幕上边
  z ∈ [-1, 1]（或 [0, 1]，取决于平台）：用于深度测试
```

---

## 第五步：NDC → 屏幕空间（视口变换）

最后，GPU 把 NDC 坐标映射到实际的像素坐标：

```
屏幕像素 x = (NDC.x + 1) / 2 × 屏幕宽度
屏幕像素 y = (NDC.y + 1) / 2 × 屏幕高度
（某些平台 y 轴方向相反，Unity 会自动处理）
```

这步也由硬件自动完成。Fragment Shader 执行时，已经知道自己在处理哪个像素坐标了。

---

## 完整的变换链总结

```hlsl
// Vertex Shader 里的变换链（展开版）
float3 worldPos = mul(UNITY_MATRIX_M, float4(objectPos, 1.0)).xyz;  // 模型→世界
float3 viewPos  = mul(UNITY_MATRIX_V, float4(worldPos, 1.0)).xyz;   // 世界→观察
float4 clipPos  = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));        // 观察→裁剪

// 等价的简写（Unity 自动合并 MVP）
float4 clipPos  = TransformObjectToHClip(objectPos);

// Vertex Shader 必须输出裁剪空间坐标（SV_POSITION）
// GPU 硬件负责后续的透视除法和视口变换
return clipPos;  // 输出到 SV_POSITION
```

每个矩阵 Unity 都帮你自动计算和传入：
- `UNITY_MATRIX_M`：来自 GameObject 的 Transform，每帧更新
- `UNITY_MATRIX_V`：来自 Camera 的 Transform，每帧更新
- `UNITY_MATRIX_P`：来自 Camera 的 FOV / Near / Far，变化时更新

---

## UV 坐标：一个独立的 2D 坐标系

除了上面的空间变换，顶点还携带一个独立的坐标：**UV 坐标**。

UV 是一个 2D 坐标，范围通常是 [0, 1] × [0, 1]，表示这个顶点对应到贴图（Texture）的哪个位置：

```
UV (0, 0) → 贴图左下角
UV (1, 0) → 贴图右下角
UV (0.5, 0.5) → 贴图正中央
UV (2, 1) → 超出 1 的部分，根据 Wrap Mode 决定（Repeat = 贴图平铺，Clamp = 拉伸边缘）
```

UV 坐标不参与 MVP 变换——它只在 Fragment Shader 里用于采样贴图：

```hlsl
// Fragment Shader 里用 UV 采样贴图
float2 uv = input.uv0;  // 从 Vertex Shader 插值传来
float4 color = tex2D(_MainTex, uv);  // 在贴图的 uv 位置取颜色
```

一个模型通常有两套 UV：
- `UV0`：表面贴图用（可以重叠，可以 Tiling）
- `UV1`：Lightmap 专用（不能重叠，唯一覆盖整张 Lightmap）

---

## 法线的变换：不能直接乘 M 矩阵

最后一个常见的坑：**法线的变换方式和位置不同**。

原因：如果模型有非均匀缩放（X 方向缩放和 Y 方向缩放不同），直接把模型矩阵 M 应用到法线上，法线会变歪：

```
原始表面：水平面，法线朝上 (0, 1, 0)
非均匀缩放 Scale (2, 1, 1)（X 方向拉伸两倍）
  → 位置变换后，面变成倾斜的
  → 如果法线也直接乘 M，法线变成 (0, 1, 0)（仍朝上，方向错了！）
  → 正确的法线应该跟着面的倾斜旋转
```

**正确的法线变换矩阵** = M 矩阵的逆矩阵的转置（Inverse Transpose）：

```hlsl
// Unity 提供了预计算好的法线变换矩阵
float3 worldNormal = mul((float3x3)UNITY_MATRIX_I_M, objectNormal);
// 或者直接用 Unity 内置函数（推荐）
float3 worldNormal = TransformObjectToWorldNormal(objectNormal);
```

这就是为什么 Vertex Shader 里位置和法线要用不同的 API 来变换，而不是统一乘同一个矩阵。

---

## 小结

```
变换步骤          矩阵          Unity 名称           解决什么问题
─────────────────────────────────────────────────────────────────────
物体→世界         M           UNITY_MATRIX_M      把模型放到场景正确位置
世界→观察         V           UNITY_MATRIX_V      以摄像机为参考原点
观察→裁剪         P           UNITY_MATRIX_P      统一视锥为标准立方体，便于裁剪
裁剪→NDC         /w          硬件自动             完成透视压缩（近大远小）
NDC→像素         视口变换      硬件自动             映射到实际分辨率的像素坐标
```

你在 Unity 里改 `transform.position` 影响的是 M 矩阵；改 `Camera.fieldOfView` 影响的是 P 矩阵；改 `Camera.transform` 影响的是 V 矩阵。Shader 里的 `TransformObjectToHClip` 把这三步合在一起做完。
