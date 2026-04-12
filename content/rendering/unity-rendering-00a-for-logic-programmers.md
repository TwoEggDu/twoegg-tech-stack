---
title: "渲染入门：CPU、GPU 与 Shader 的分工"
slug: "unity-rendering-00a-rendering-basics"
date: "2025-01-26"
description: "从 Unity 已有的组件概念（GameObject、Component、MonoBehaviour、Update）出发，建立渲染系统的认知框架：CPU 负责组织命令，GPU 负责并行执行，Shader 是跑在 GPU 上的像素计算函数。"
tags:
  - "Unity"
  - "渲染入门"
  - "GPU"
  - "渲染管线"
series: "Unity 渲染系统"
weight: 75
---
如果只用一句话概括这篇：渲染系统和游戏逻辑系统的根本区别是"谁在执行代码"——游戏逻辑跑在 CPU 上的 C# 里，渲染跑在 GPU 上的 Shader 里，理解这个分工，是理解整套渲染系列的起点。

---

## 从 Unity 组件认识渲染系统

Unity 的组件系统（GameObject + Component + MonoBehaviour）是大多数人进入 Unity 的起点。渲染系统建立在这套组件系统之上，但它有自己的执行者（GPU）、自己的语言（HLSL Shader）、自己的运行时机。

把两个系统的概念对应起来，是建立渲染认知的第一步。

### MeshRenderer → 一个渲染请求

GameObject 挂上 `MeshRenderer` 之后，它在渲染层面的意思是：

```
MeshRenderer 告诉渲染系统：
  "我这里有一个 Mesh（几何形状数据）
   和一个 Material（表面外观数据）
   还有 Transform（在世界里的位置/旋转/缩放）
   请把它画到屏幕上"
```

在 C# 里写 `transform.position = new Vector3(1, 0, 0)` 改变的是这个请求里的"位置"。

### Material + Shader → GPU 上的"函数"

在 Inspector 里打开一个 Material，能看到颜色、贴图、金属度等参数。这些参数会被传给 **Shader**。

Shader 是运行在 **GPU** 上的程序，用 HLSL（类 C 的语言）写的。它的工作很简单：

```
给定：一个三角面上某个点的位置、法线、UV 等信息
返回：这个点对应的屏幕像素是什么颜色
```

Material Inspector 里的每个参数，都是在设置这个"函数"的输入值。

类比：MonoBehaviour 是"挂在 GameObject 上、由 Unity 每帧调用 Update 的 C# 类"；Shader 是"挂在 Material 上、由 GPU 对每个像素调用的 HLSL 函数"。

### Camera → 决定从哪个视角渲染

`Camera` 组件的本质是告诉渲染系统：

```
Camera 告诉渲染系统：
  "请从我的位置，以我的视角（FOV），
   把场景里所有可见的 MeshRenderer 渲染到屏幕上"
```

调 `Camera.fieldOfView`、`Camera.backgroundColor`，就是在改这个请求的参数。

### Light → 传给 Shader 的参数

`Light` 组件本身不在 GPU 上"发光"——它的参数（颜色、强度、方向）被 Unity 收集起来，每帧通过常量缓冲区传给 GPU，让 Shader 在计算像素颜色时使用。

改 `light.intensity`，本质上是改了传给所有受影响 Shader 的一个参数值。

---

## CPU 和 GPU 的分工

理解这个分工是理解渲染最重要的一步：

```
每帧渲染流程（简化版）：

CPU 侧（C# / Unity 引擎）：
  ① 执行 MonoBehaviour.Update()（游戏逻辑）
  ② 收集场景里所有 MeshRenderer 的状态（位置、材质参数）
  ③ 做 Culling：哪些物体在摄像机视野里
  ④ 向 GPU 提交"Draw Call"：
     "把这个 Mesh，用这个 Material，放在这个位置，画出来"

    ↑ CPU 把命令塞进队列，不等 GPU 完成就继续执行
    ↓

GPU 侧（并行执行，数千个核心同时工作）：
  ⑤ 对 Mesh 的每个顶点执行 Vertex Shader（变换到屏幕坐标）
  ⑥ 光栅化：把三角形覆盖的屏幕像素找出来
  ⑦ 对每个像素执行 Fragment Shader（计算颜色）
  ⑧ 写入 Framebuffer（Color Buffer、Depth Buffer）
  ⑨ 所有物体完成后，把 Framebuffer 推到屏幕显示
```

`Update()` 在第①步，它改变的数据通过第②③④步传给 GPU，GPU 在第⑤⑥⑦⑧步用到这些数据。

**关键认知**：GPU 是高度并行的——它同时对数千个像素执行 Fragment Shader，而不是一个接一个。这就是为什么 Shader 的写法和 C# 的写法思路完全不同：Shader 里没有"遍历像素"的循环，并行化是 GPU 硬件自动处理的。

---

## CPU 与 GPU 的通信方式

C# 代码和 Shader 代码之间的通信有固定的几种方式：

**Material Properties（材质属性）：**

```csharp
// C# 侧：通过 Material API 设置
material.SetFloat("_Metallic", 0.8f);
material.SetColor("_BaseColor", Color.red);
material.SetTexture("_MainTex", myTexture);
```
```hlsl
// HLSL 侧：Shader 里声明同名变量，Unity 自动对接
float _Metallic;
float4 _BaseColor;
sampler2D _MainTex;
```

**Transform 矩阵：**

```csharp
// C# 侧：改 transform.position
transform.position = new Vector3(1, 0, 0);
```
```hlsl
// HLSL 侧：Unity 自动把 Transform 编码成矩阵传入
// Vertex Shader 里直接用 UNITY_MATRIX_M 取到这个变换
float4x4 UNITY_MATRIX_M; // Unity 自动注入，无需手动传递
```

**Shader Keyword（渲染特性开关）：**

```csharp
// C# 侧：打开/关闭某个渲染特性
material.EnableKeyword("_WET_SURFACE");
```
```hlsl
// HLSL 侧：对应的编译分支
#ifdef _WET_SURFACE
    // 湿表面的额外计算
#endif
```

Inspector 里的每个滑条和贴图槽，背后都是其中一种通信方式。

---

## 几个常见问题

**为什么改了 Material 的参数，所有用这个 Material 的物体都变了？**

Material 是一个共享的"参数包"，所有引用它的 MeshRenderer 都读同一份数据。要让两个物体显示不同，需要用不同的 Material，或者用 `MaterialPropertyBlock`（每个物体独立的参数，不创建 Material 副本）。

**为什么改颜色有时要写代码，有时只需要拖 Inspector 滑条？**

两者没有区别——Inspector 里的每个滑条背后就是一个 `material.SetFloat()` 调用，Unity 帮你生成了 UI。Shader 收到的参数值是一样的。

**粒子系统、UI、Sprite 也走渲染管线吗？**

都走，只是用了不同的 Mesh 生成方式。粒子用动态生成的 Quad（两个三角形组成的矩形），UI Canvas 生成 Mesh，Sprite 是一张透明 Quad——最终都是"Mesh + Material"的组合，走完全相同的渲染流程。

**Shader 出错了为什么材质变粉红色？**

粉色（Magenta）是 Unity 的"Shader 加载失败"占位颜色。Shader 代码编译出错 → GPU 没有可用的 Shader 程序 → Unity 显示粉色，方便定位哪些 Material 出了问题。

---

## 这套系列的阅读路径

按依赖关系，建议的入门顺序：

```
渲染资产基础（先建立数据层面的认知）
  00b：顶点为什么要经过五个坐标系（MVP 变换）
  01： Mesh / Material / Texture 怎么决定像素颜色
  02： 四条光照路径（实时光 / Lightmap / Light Probe / Reflection Probe）

性能相关（理解"为什么要合批"）
  01b：Draw Call 与批处理
  01b-2：GPU Instancing 与 SRP Batcher

动画与特效
  03：骨骼动画蒙皮
  04：粒子系统
  05：后处理（Bloom / SSAO / DOF）

渲染管线架构（理解 URP / HDRP 的设计逻辑）
  06-11：Built-in → SRP → URP → HDRP

调试工具（遇到问题时参考）
  01d：Frame Debugger 使用指南
  01e/01f：RenderDoc 入门与进阶
```

不必按顺序读完。如果是遇到了具体问题——"为什么帧率低"先读 01b，"为什么光照不对"先读 02，"想在 URP 里加自定义效果"先读 08–10。

---

## 贯穿全系列的三个问题

每次读到一个新概念，可以用这三个问题来定位它：

1. **这个数据在哪里**：在 CPU 内存里（C# 对象），还是在 GPU 显存里（Buffer / Texture）？
2. **谁在用这个数据**：CPU（Unity 引擎 / C# 代码），还是 GPU（Shader）？
3. **什么时候用**：每帧重新计算，还是预计算一次然后复用？

这三个问题能帮助把碎片化的概念连接成一个完整的系统。
