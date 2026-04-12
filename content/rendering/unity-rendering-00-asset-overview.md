---
date: "2026-03-24"
title: "Unity 渲染系统 00｜游戏里有哪些渲染资产，它们各自在管线哪个阶段介入"
description: "以一帧画面为主线，把 Unity 里所有参与渲染的资产类型串起来，讲清楚每类资产是什么数据、在管线哪个阶段被消费、对最终像素起什么作用。"
slug: "unity-rendering-00-asset-overview"
weight: 100
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "Asset"
  - "Pipeline"
  - "Overview"
series: "Unity 渲染系统"
---
> 如果只用一句话概括这篇，我会这样说：Unity 里参与渲染的资产不止材质和贴图，它们按照介入管线的时机分成几类，每类扮演完全不同的角色，共同决定了屏幕上每一个像素的颜色。

很多人对渲染资产的认知是散的——知道材质要挂贴图，知道光源会影响颜色，知道后处理可以做 Bloom，但这些东西在流程上是什么关系，哪个在哪一步发挥作用，说不太清楚。

这篇要建立一张地图：**以一帧画面的渲染流程为主线，把所有渲染资产放进去，看它们各自在哪个阶段被消费。**

这张地图是后续各篇的索引——每篇会把其中一类资产展开讲细。

---

## 一帧画面的渲染是一条流水线

先把主线立起来。一帧画面从"引擎开始处理这帧"到"像素出现在屏幕上"，大致经过这几个阶段：

```
CPU 组织可见物体
    ↓
顶点处理（Vertex Stage）
    ↓
光栅化（Rasterization）
    ↓
片元着色（Fragment Stage）
    ↓
深度测试 + 混合输出
    ↓
后处理（Post-processing）
    ↓
屏幕输出
```

渲染资产不是在某一个阶段统一被消费的，而是**分散在不同阶段各自介入**。下面按阶段逐一说。

---

## 第一阶段：CPU 组织可见物体

这一阶段引擎在 CPU 上决定"这帧要画什么"。

**Camera** 定义了视锥体——空间里在这个范围内的物体才有资格被渲染。引擎做 Culling（剔除），把视锥体外的物体排除掉，不提交 Draw Call。

**Transform** 决定每个物体在世界空间的位置、旋转和缩放，这会影响后续顶点变换用的 Model Matrix。

这一阶段结束时，引擎拿到了一批"要画的物体"列表，每个物体知道自己用哪个 Mesh、哪个 Material，以及自己的 Transform。

---

## 第二阶段：顶点处理

GPU 拿到 Mesh 数据后，对每个顶点执行 Vertex Shader。这一阶段的核心是**坐标变换**，但在变换之前，有两类资产可以改变顶点的位置。

### Mesh（网格）

Mesh 是几何数据的来源。每个顶点携带：

- **Position**：顶点在模型自身坐标系里的位置
- **UV**：对应到贴图上的坐标，后续用来采样
- **Normal / Tangent**：表面朝向信息，用于光照计算
- **Vertex Color**：可选的额外数据通道

Mesh 的三角面决定了这个物体在屏幕上覆盖哪些像素——Mesh 不对，形状就不对。

### 骨骼动画（AnimationClip + Skeleton）

骨骼蒙皮在 Vertex Shader 执行时，把每个顶点按骨骼权重混合多个骨骼的变换矩阵，得到动画后的顶点位置。这意味着**动画改变的不是"显示效果"，而是顶点数据本身**——同一个站立和奔跑的角色，送进光栅化的是位置完全不同的两组顶点。

AnimationClip 存储的是骨骼的变换曲线，Skeleton（Avatar）定义骨骼层级结构，两者配合才能驱动蒙皮。

### Blend Shape（形态键）

Blend Shape 是另一种顶点变形方式——直接存储"目标形状"和"当前形状"之间的顶点偏移量，按权重插值。常用于面部表情、角色捏脸。和骨骼动画的区别是它不依赖骨骼层级，每个顶点独立偏移。

---

## 第三阶段：光栅化

光栅化是硬件自动完成的步骤，没有资产直接参与。它做两件事：

1. 判断三角形覆盖了屏幕上的哪些像素
2. 对每个被覆盖的像素，用重心坐标插值出该像素的 UV、法线、切线值

这一步的输出是"每个像素有了自己的 UV 坐标和法线方向"——这些值接下来在 Fragment Shader 里被消费。

---

## 第四阶段：片元着色

这是资产最密集介入的阶段，也是"颜色怎么算出来"的地方。

### Material + Shader

Material 是这一阶段的执行者。它包含两部分：

- **Shader**：运行在 GPU 上的程序，定义"这个表面按什么规则计算颜色"
- **参数集**：Shader 运行需要的输入——颜色值、浮点数、贴图引用

Shader 决定"用什么公式"，Material 决定"公式里的变量是什么"。同一个 URP/Lit Shader，金属感强的材质和粗糙感强的材质，用的是同一份程序，只是参数不同。

### Texture（贴图）

贴图不是"贴在表面的图"，而是 **Fragment Shader 用 UV 坐标查询的数据**。每种贴图的数据含义不同：

| 贴图类型 | 存储的数据 | Fragment Shader 怎么用它 |
|---|---|---|
| BaseColor / Albedo | 表面固有色 (RGB) | 作为漫反射颜色的基础输入 |
| Normal Map | 微观法线方向（编码在 RGB 里） | 解码后替换几何法线，制造凹凸感 |
| Metallic | 金属度 (0~1) | 控制高光颜色和漫反射强度 |
| Roughness | 粗糙度 (0~1) | 控制高光的集中程度 |
| Occlusion | 环境遮蔽强度 | 压暗缝隙和角落 |
| Emissive | 自发光颜色 (RGB) | 直接叠加到输出，不受光照影响 |

每次 Fragment Shader 执行，都用当前像素插值出的 UV 去这些贴图里取对应的值。

### 光照资产：四条路径汇合

Fragment Shader 算颜色需要知道"这个表面受到多少光"。Unity 里的光照来自四条路径，全部在 Fragment 阶段汇合：

**实时 Light（直接光）**：点光源、方向光、聚光灯的参数（方向、颜色、强度）由引擎传入 Shader，每帧重新计算。适合动态光源。

**Lightmap（烘焙间接光）**：把静态场景的间接光提前计算好、存进贴图。Fragment Shader 用物体的第二套 UV（Lightmap UV）去采样这张贴图，得到这个像素接收到的间接光强度。烘焙后不再实时计算，性能开销低，但无法响应动态变化。

**Light Probe（动态物体间接光）**：Lightmap 只能用于静态物体，动态物体（角色、可移动道具）用 Light Probe。它存的是空间中若干采样点的光照环境（球谐系数），动态物体的 Fragment Shader 根据物体位置插值出周围的间接光贡献。

**Reflection Probe（环境反射）**：存储空间中某个位置的周围环境（Cubemap），Fragment Shader 用反射方向采样这个 Cubemap，得到镜面间接反射颜色。金属感强或粗糙度低的表面，这个贡献很显著。

---

## 第五阶段：深度测试与混合

深度测试决定哪个像素"遮住"哪个——离相机更近的像素保留，更远的丢弃。这一步在硬件里自动完成。

混合模式（Blend Mode）在 Material 里配置，决定这个像素和已有颜色缓冲怎么合并——不透明物体直接覆盖，半透明物体按 Alpha 混合，粒子特效通常用加法混合。

---

## 第六阶段：特殊路径

有三类资产走的不是上面的标准路径：

### Particle System（粒子）

粒子系统在 CPU 上维护大量粒子的状态（位置、速度、生命周期），每帧动态生成对应数量的小 Mesh（通常是面向相机的四边形 Billboard，或者自定义 Mesh）。每个粒子按自己的 Transform 走一遍标准的顶点→光栅化→片元着色路径，只是数量很多、每个很小、生命周期短。

粒子的 Material 决定它们的外观——常见的烟雾、火焰、魔法效果，通常是半透明材质加上特定的 Blend Mode（加法、柔光等）。

### Skybox（天空盒）

天空盒填充的是没有任何几何体覆盖的背景像素。实现上有多种方式（全景贴图、Cubemap、程序化），但核心是：**渲染完场景里所有不透明物体之后，对深度缓冲里"没有被覆盖"的像素，用天空盒材质填色。**

### UI（Canvas）

UI 走独立的渲染路径。Canvas 收集所有 UI 元素（Image、Text、RawImage），经过自己的批处理逻辑，最后用 UI 专用的 Shader 渲染，叠加在场景画面之上。字体本质上是字形纹理图集，Text 组件用 UV 映射在图集里取对应字形渲染。

---

## 第七阶段：后处理

所有场景内容渲染完之后，后处理作用于整张帧缓冲区。

**Post-processing Volume** 是配置容器，里面存着各种后处理效果的参数（Bloom 的阈值、Tonemapping 的曲线、Color Grading 的颜色矩阵、SSAO 的半径等）。Volume 通过"覆盖区域 + 混合权重"机制，允许不同区域使用不同的后处理设置。

实际执行时，每个后处理效果是一个**全屏 Pass**：把帧缓冲区的颜色作为输入纹理，执行一段 Shader 程序，输出处理后的颜色。多个效果串联执行。

**Render Texture** 是一类特殊资产——可以让一个 Camera 把渲染结果不写入屏幕，而是写入一张 Render Texture，然后把这张 Texture 作为另一次渲染的贴图输入。常用于镜面反射、监控摄像机画面、小地图等场景。

---

## 汇总：资产、阶段、作用

```
CPU 组织可见物体
  └─ Camera（定义视锥体）
  └─ Transform（Model Matrix）

顶点处理
  └─ Mesh（顶点数据：Position / UV / Normal / Tangent）
  └─ AnimationClip + Skeleton（骨骼蒙皮，改变顶点位置）
  └─ Blend Shape（顶点偏移）

光栅化（硬件自动，按 UV/法线插值到每个像素）

片元着色
  └─ Material + Shader（计算规则）
  └─ Texture（BaseColor / Normal / Metallic / Roughness / Occlusion / Emissive）
  └─ 实时 Light（直接光参数）
  └─ Lightmap（烘焙静态间接光）
  └─ Light Probe（动态物体间接光）
  └─ Reflection Probe（环境镜面反射）

深度测试 + 混合（硬件 + Material 的 Blend Mode 配置）

特殊路径
  └─ Particle System（大量小 Mesh 批量走标准路径）
  └─ Skybox（填充背景像素）
  └─ UI Canvas（独立路径，叠加在场景之上）

后处理
  └─ Post-processing Volume（全屏 Pass 参数配置）
  └─ Render Texture（渲染结果作为贴图输入）
```

---

## 这张地图怎么用

建立这张地图之后，遇到渲染问题时，定位方向就清晰了：

- 形状不对、穿模、变形错误 → 看 Mesh 或骨骼动画
- 颜色不对、表面效果不对 → 看 Material / Shader / Texture
- 光照不对、阴影不对 → 看 Light / Lightmap / Light Probe
- 反射不对 → 看 Reflection Probe
- 全局色调不对、Bloom 太强或没有 → 看 Post-processing Volume
- 特效外观不对 → 看 Particle System 的 Material

后续各篇会把这张地图里的每一类资产展开讲——它的内部数据结构是什么，Fragment Shader 怎么消费它，常见的配置问题出在哪里。
