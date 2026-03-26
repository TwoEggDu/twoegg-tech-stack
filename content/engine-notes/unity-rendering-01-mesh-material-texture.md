---
date: "2026-03-24"
title: "Unity 渲染系统 01｜几何与表面：Mesh、Material、Texture 是什么数据，怎么变成像素"
description: "把 Mesh 的顶点结构、Material 与 Shader 的关系、Texture 的采样机制讲清楚，并完整追踪这三类资产从 Draw Call 提交到片元着色输出的数据路径。"
slug: "unity-rendering-01-mesh-material-texture"
weight: 200
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "Mesh"
  - "Material"
  - "Texture"
  - "Shader"
  - "PBR"
series: "Unity 渲染系统"
---
> 如果只用一句话概括这篇，我会这样说：Mesh 是形状数据，Material 是计算规则，Texture 是被 UV 地址查询的数据——三者不是"拼在一起的东西"，而是在管线的不同阶段各自发挥作用。

上一篇建立了渲染资产的全局地图。这篇把地图里最核心的一条路展开讲：**一个普通的 3D 物体，它的 Mesh、Material、Texture 各是什么数据结构，在管线里各自在哪一步被消费。**

这条路是理解其他所有渲染资产的基础——光照、动画、粒子、后处理，最终都要经过这条路才能产生像素。

---

## Mesh 是什么数据

"模型"这个词容易让人以为 3D 物体是某种"立体图片"。实际上，一个 Mesh 在内存里是两张表。

### 顶点缓冲（Vertex Buffer）

第一张表：每一行是一个顶点，每一列是这个顶点携带的一项数据。

| 数据项 | 类型 | 含义 |
|---|---|---|
| Position | float3 | 顶点在模型自身坐标系里的位置 |
| Normal | float3 | 该点所在表面的法线方向，用于光照计算 |
| Tangent | float4 | 切线方向，配合法线贴图使用（w 分量存手性，决定副切线方向） |
| UV0 | float2 | 第一套纹理坐标，用于采样 BaseColor / Normal / Metallic 等贴图 |
| UV1 | float2 | 第二套纹理坐标，通常用于采样 Lightmap（烘焙光照贴图） |
| Vertex Color | float4 | 可选，存储顶点颜色或额外数据（遮蔽值、区域权重等） |

不是所有 Mesh 都有全部字段——简单的模型可能只有 Position 和 UV0，复杂的会有全套。Unity 的 `Mesh` 类允许单独读写每个通道（`mesh.vertices`、`mesh.normals`、`mesh.uv` 等）。

### 索引缓冲（Index Buffer）

第二张表：一串整数，三个一组，每组指向顶点缓冲里的三个顶点，定义一个三角形。整个 Mesh 的表面就是这些三角形拼起来的。

```
索引缓冲示例：[0, 1, 2, 2, 1, 3, ...]
                 └─三角形1─┘  └─三角形2─┘

顶点0 ─── 顶点1
  │    ╲    │
  │      ╲  │
顶点2 ─── 顶点3
```

这种设计允许顶点复用——同一个顶点坐标可以被多个三角形引用，不用重复存储位置数据。

### Sub-mesh（子网格）

一个 Mesh 可以包含多个 Sub-mesh，每个 Sub-mesh 有自己的索引缓冲范围，对应 `MeshRenderer` 上的一个 Material Slot。这就是为什么一个模型可以有"身体用皮肤材质、眼睛用眼球材质、衣服用布料材质"——它们是同一个 Mesh 的不同 Sub-mesh，分别对应不同的 Material。

---

## Material 是什么数据

Material 经常和 Texture 混淆，也经常被理解成"贴图的容器"。实际上：

**Material = Shader（计算程序）+ 参数集（Shader 运行所需的输入值）**

### Shader：计算规则

Shader 是一段在 GPU 上运行的程序，由 Vertex Shader 和 Fragment Shader 两部分组成。

Vertex Shader 负责坐标变换——把顶点从模型空间变换到屏幕空间，同时把 UV、法线等数据传递给后续阶段。

Fragment Shader 负责计算颜色——它定义了"这个表面用什么公式算出颜色"。同样的贴图，用不同的 Fragment Shader，结果可以是"卡通渲染的石头"或"PBR 金属的石头"——贴图是一样的，但计算规则不同。

### 参数集：Shader 的输入

Shader 程序里有占位符——颜色值、浮点数、贴图引用，这些值由 Material 提供：

```
Material A（石头）：
  _BaseMap        → 石头纹理贴图
  _NormalMap      → 石头法线贴图
  _Metallic       → 0.0（非金属）
  _Roughness      → 0.85（很粗糙）
  _BaseColor      → (1, 1, 1, 1)（不额外染色）

Material B（金属板）：
  _BaseMap        → 金属板纹理贴图
  _NormalMap      → 金属板法线贴图
  _Metallic       → 0.95（几乎纯金属）
  _Roughness      → 0.15（很光滑）
  _BaseColor      → (0.8, 0.8, 0.9, 1)（微微蓝色调）
```

这两个 Material 可以用同一个 Shader，只是参数不同，渲染结果就会有很大差异。

### Shader Variant 与 Material

Material 上还有一组激活的 Keyword（关键字），决定使用 Shader 的哪个变体。比如一个材质开启了 `_NORMALMAP`，Unity 就选用"带法线贴图计算"的那个变体；关掉这个 Keyword，就用"不做法线贴图计算"的变体（更省）。

这个机制在上一个系列的文章里有详细说明——简单理解就是，**Material 上的 Keyword 状态是"选哪个编译版本的 Shader 程序"的开关**。

---

## Texture 是什么数据

Texture 最容易被误解成"贴在模型表面上的图片"。更准确的理解是：

**Texture 是一个二维数组，Fragment Shader 用 UV 坐标作为地址去查询它。**

一张 1024×1024 的 BaseColor 贴图，就是 1024×1024 个颜色值（每个值通常是 R/G/B/A 四个分量）。Fragment Shader 执行时，用当前像素的 UV 坐标（比如 `(0.25, 0.75)`）换算成数组里的位置，取出那里的颜色值。

这个"取值"操作叫做**纹理采样（Texture Sampling）**。采样时 UV 不总是精确落在一个像素格子上，GPU 会用双线性插值（Bilinear）或各向异性过滤（Anisotropic）在相邻格子之间插值，让结果更平滑。

### UV 坐标从哪来

每个像素的 UV 坐标，是在光栅化阶段从三角形的三个顶点 UV 插值出来的。顶点 A 的 UV 是 `(0, 0)`，顶点 B 是 `(1, 0)`，顶点 C 是 `(0.5, 1)`，那么这个三角形内部的每个像素，都有一个根据它在三角形里的位置算出的 UV 值。

这就是为什么贴图的细节能"跟着"模型表面走——本质上是 UV 坐标在光栅化时被连续插值，使得每个像素都能在贴图上找到对应的位置。

### 不同类型贴图存储不同含义的数据

贴图里存的不一定是"颜色"。不同类型的贴图，每个通道的含义不同：

**Normal Map（法线贴图）**：RGB 通道存的是切空间法线方向，编码方式是 `normal = rgb * 2 - 1`。Fragment Shader 解码后得到一个偏移后的法线方向，代替几何法线参与光照计算——让平坦的面表现出凹凸感，但实际几何形状不变。

**Metallic / Roughness**：通常打包在同一张贴图的不同通道里（比如 R 通道存金属度，A 通道存平滑度）。Fragment Shader 取出这些值，输入给 PBR 公式。

**Occlusion Map**：存储环境遮蔽强度（0 = 完全遮蔽，1 = 无遮蔽），Fragment Shader 用它压暗角落和缝隙，不需要实时计算。

---

## 数据路径：从 Draw Call 到像素

把三类资产放进管线，完整追踪一次。

### CPU 提交 Draw Call

引擎为每个可见物体提交一次 Draw Call，包含：

```
Draw Call = {
    顶点缓冲指针    → 从哪个 Mesh 取数据
    索引缓冲指针    → 画哪些三角形（支持 Sub-mesh）
    Material 绑定  → 用哪个 Shader + 哪组参数
    Transform 矩阵 → 这个物体在世界里的位置/旋转/缩放
}
```

### Vertex Shader：坐标变换 + 数据传递

GPU 拿到顶点缓冲后，对每个顶点执行 Vertex Shader，核心工作是 MVP 矩阵变换：

```
Model Space
    × Model Matrix（Transform）
    → World Space
    × View Matrix（Camera 变换）
    → View Space
    × Projection Matrix（透视/正交）
    → Clip Space
    ÷ w（透视除法，硬件自动）
    → NDC（归一化设备坐标，范围 -1 到 1）
    → Screen Space（按视口映射到像素坐标）
```

同时，Vertex Shader 把 UV0、UV1、法线、切线等数据原样传出去，准备在光栅化后被每个像素继承。

### 光栅化：三角形变像素，插值数据

光栅化器：

1. 对每个三角形，判断覆盖了哪些像素
2. 对每个被覆盖的像素，用重心坐标计算插值：

```
像素 P 的 UV = 顶点A_UV × α + 顶点B_UV × β + 顶点C_UV × γ
（α + β + γ = 1，由 P 在三角形内的位置决定）
```

法线、切线、顶点色也做同样的插值。**从这一步开始，每个像素有了自己的 UV 坐标和法线方向。**

### Fragment Shader：采样贴图，执行 PBR 计算

每个像素执行一次 Fragment Shader，这是 Material 的 Shader 程序真正运行的地方。

**第一步：用 UV 采样贴图，取出材质参数**

```
albedo    = sample(BaseColorTexture, uv) × _BaseColor
metallic  = sample(MetallicTexture, uv).r
roughness = 1.0 - sample(MetallicTexture, uv).a
normalTS  = decode_normal(sample(NormalMap, uv))
occlusion = sample(OcclusionMap, uv).r
```

**第二步：把切空间法线变换到世界空间**

法线贴图存的是相对于表面自身的法线偏移（切空间）。Fragment Shader 用顶点传来的法线和切线向量，构建 TBN 矩阵，把切空间法线变换到世界空间，才能参与光照计算。

```
N（世界空间法线）= normalize(TBN × normalTS)
```

**第三步：PBR 光照计算**

拿到 `albedo`、`metallic`、`roughness`、世界空间法线 `N`、光照方向 `L`、视线方向 `V` 之后，PBR BRDF 计算出这个像素应该反射多少光：

```
直接光漫反射  = albedo × (1 - metallic) × max(dot(N, L), 0) × lightColor
直接光高光    = F(metallic, V, H) × D(roughness, N, H) × G(roughness, N, V, L)
                / (4 × dot(N, V) × dot(N, L))
间接光漫反射  = albedo × (1 - metallic) × irradiance（来自 Light Probe 或 Lightmap）
间接光高光    = reflectionColor（来自 Reflection Probe）× F(metallic, V, N)
自发光        = emissive

最终颜色 = (直接光漫反射 + 直接光高光 + 间接光漫反射 + 间接光高光) × occlusion + 自发光
```

这是 PBR 渲染的基本结构。实际的 URP/Lit 实现会更复杂（多光源、阴影、雾效等），但核心逻辑是这个框架。

### 深度测试与输出

Fragment Shader 输出颜色后，硬件比较这个像素的深度和深度缓冲：更近的像素写入颜色缓冲，更远的丢弃。最终颜色缓冲里的内容显示到屏幕。

---

## 几个容易混淆的地方

**Mesh 的颜色不是存在顶点里的**

大多数情况下，像素颜色来自 Fragment Shader 对贴图的采样，不是顶点色。顶点色是一个额外数据通道，有时候用来存遮蔽值、区域混合权重等，用途由 Shader 决定。

**改 Material 参数不等于改 Shader**

`material.SetFloat("_Roughness", 0.5f)` 只是修改参数集里的一个值，不触发任何 Shader 重新编译。改 Keyword（`material.EnableKeyword`）才会切换 Shader 变体。

**UV 不是只有一套**

UV0 用于采样表面贴图（BaseColor、Normal 等），UV1 通常用于采样 Lightmap。一个 Mesh 同时有两套 UV 是正常的，它们各自负责不同的采样任务。

**法线贴图改变的是光照计算，不是几何形状**

法线贴图让平面看起来有凹凸感，但实际的三角面仍然是平的。从侧面看，模型轮廓不会有任何变化。要真正改变几何形状，需要改 Mesh 数据，或者在 Vertex Shader 里做 Displacement（位移）。

---

## 和下一篇的关系

这篇只追踪了"表面自身"的颜色来源。但 PBR 计算里，"间接光漫反射"和"间接光高光"两项来自其他资产——Lightmap、Light Probe、Reflection Probe。这三类光照资产是怎么产生的、怎么被 Fragment Shader 采样，下一篇展开讲。
