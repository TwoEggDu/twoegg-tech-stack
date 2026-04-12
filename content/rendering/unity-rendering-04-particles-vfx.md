---
date: "2026-03-24"
title: "Unity 渲染系统 04｜粒子与特效：Particle System 的几何生成与渲染机制"
description: "讲清楚 Particle System 怎么在 CPU 上管理粒子状态、动态生成几何体（Billboard/Mesh/Trail），以及粒子渲染和普通 Mesh 渲染路径的异同，顺带对比 VFX Graph 的架构差异。"
slug: "unity-rendering-04-particles-vfx"
weight: 700
featured: false
tags:
  - "Unity"
  - "Rendering"
  - "Particles"
  - "VFX"
  - "Billboard"
  - "ParticleSystem"
  - "VFXGraph"
series: "Unity 渲染系统"
---
> 如果只用一句话概括这篇，我会这样说：粒子特效在渲染层面没有任何魔法——它是大量动态生成的小 Mesh 批量走了一遍标准的顶点→光栅化→片元着色路径，只是这些 Mesh 的位置、大小、颜色每帧都由 CPU 上的粒子模拟系统计算出来。

前几篇讲的形状变形（骨骼/Blend Shape）作用于一个有固定顶点数量的 Mesh。粒子特效不同——它的几何体数量本身就是动态的：一个爆炸效果可能在一帧内生成 500 个粒子，下一帧这些粒子开始死亡，数量减少。

这篇讲粒子系统是怎么把这些动态几何体变成屏幕上的特效的。

---

## Particle System 的整体架构

Unity 的 Particle System（Shuriken）分成两层：

**模拟层（CPU）**：维护所有存活粒子的状态——位置、速度、颜色、大小、生命周期剩余时间。每帧按模拟规则更新这些状态（重力、速度衰减、颜色渐变等），并生成新粒子、销毁到期粒子。

**渲染层（GPU）**：把模拟层输出的粒子数据转换成 GPU 可以处理的几何体，走标准的渲染管线。

两层的分界点是：**粒子的位置/颜色/大小确定之后，怎么变成屏幕上的像素。**

---

## 粒子的三种几何体生成方式

### Billboard（广告牌）

最常见的方式。每个粒子生成一个**面向相机的四边形**（2 个三角形，4 个顶点）：

```
粒子位置 → 以该位置为中心
         → 生成一个正方形四边形
         → 四边形始终旋转到朝向摄像机
         → UV 覆盖整张 [0,1] 范围（或图集里的子区域）
```

Billboard 的核心是**朝向计算**：Vertex Shader 拿到粒子中心位置后，根据相机方向在顶点坐标上叠加一个偏移，使四边形的法线始终朝向相机。这样无论从哪个角度看，粒子都是面朝你的。

Billboard 模式有几个变体：
- **Free Billboard**：完全朝向相机（火焰、烟雾、光晕）
- **Horizontal Billboard**：四边形始终水平，只做水平方向旋转（地面贴花、落地阴影）
- **Vertical Billboard**：四边形始终竖直（草地、灌木的简化表示）
- **Stretched Billboard**：四边形沿速度方向拉伸（雨滴、速度线）

### Mesh 粒子

每个粒子是一个完整的 3D Mesh（不一定是四边形），可以是任意形状。每个粒子 Instance 渲染一次这个 Mesh，使用粒子的 Transform（位置/旋转/大小）作为 Model Matrix。

适合：碎片、落叶、石块等有真实 3D 形状的粒子。

Unity 对 Mesh 粒子支持 **GPU Instancing**——如果大量粒子使用同一个 Mesh 和 Material，它们可以合并成一次 `DrawMeshInstanced` 调用，大幅减少 Draw Call。

### Trail（拖尾）

拖尾是一种特殊的几何生成方式——不是每帧独立的几何体，而是沿粒子历史路径**动态生成一段带状 Mesh**：

```
当前帧  → 记录粒子当前位置
若干帧后 → 沿这些历史位置点，生成一段宽度均匀（或渐变）的带状四边形序列
```

Trail 的 UV 沿路径方向平铺，让贴图纹理沿轨迹滚动——常用于刀光、魔法轨迹、尾焰。

---

## 粒子的渲染路径

粒子几何体生成后，走和普通 Mesh 完全相同的路径：

```
粒子顶点数据（Position/UV/Color）
    → Vertex Shader（Billboard 额外做朝向计算 + MVP 变换）
    → 光栅化（三角形覆盖像素）
    → Fragment Shader（采样贴图 + 颜色计算）
    → 深度测试 + 混合
```

粒子特效视觉上的"发光"、"半透明"、"叠加"效果，都来自 **Material 的 Blend Mode 配置**：

| 效果 | Blend Mode | 表现 |
|---|---|---|
| 普通半透明（烟雾）| SrcAlpha + OneMinusSrcAlpha | 按 Alpha 混合，越透明越能透出背景 |
| 加法叠加（火焰、光晕）| One + One | 颜色相加，越叠越亮，适合发光效果 |
| 柔和叠加（柔和光晕）| One + OneMinusSrcColor | 叠加时不会过曝 |
| 不透明粒子（碎片）| 无混合 | 直接覆盖背景 |

这就是为什么**改 Particle System 的 Material 就能完全改变特效的视觉风格**——几何体不变，只是混合规则变了。

---

## 粒子渲染的特殊挑战

### 半透明排序问题

半透明物体（包括大多数粒子）需要**从后往前**渲染，后面的先画，前面的后画，Alpha 混合才能正确叠加。

Unity 的 Particle System 在同一个粒子系统内部支持按距离排序，但**多个粒子系统之间的排序是近似的**，依赖于各自的 Sorting Layer 和 Order in Layer 设置。大量半透明粒子交叉时，可能出现排序错误（两个半透明效果互相"穿过"）。

### 深度写入问题

半透明粒子通常**不写入深度缓冲**（`ZWrite Off`），否则后方的粒子会被前方的粒子的深度值遮挡——但实际上应该透过去。

但不写深度也带来问题：粒子的 Fragment Shader 不能利用 Early Z Rejection（深度测试在 Fragment Shader 之前就拒绝被遮挡的片元），导致大量粒子叠加时存在 **Overdraw**——同一个像素被多个粒子的 Fragment Shader 重复着色，但只有最上层的结果保留。

这是粒子特效的主要性能开销来源之一：不是 Draw Call 数量，而是像素级的 Overdraw。

### Soft Particle（软粒子）

普通粒子在和场景几何体相交处会有硬边，因为粒子的 Alpha 值没有考虑到"这个像素附近有没有不透明物体"。

**Soft Particle** 通过在 Fragment Shader 里采样深度缓冲，计算粒子当前像素的深度和场景深度的差值，差值小的像素（接近场景表面的部分）做 Alpha 淡出处理——消除硬边。

代价是 Fragment Shader 里多一次深度缓冲采样，以及需要开启深度贴图的正确配置。

---

## VFX Graph：GPU 驱动的粒子

Unity 的 **VFX Graph** 是另一套粒子系统，和 Particle System 的根本区别是：

| | Particle System（Shuriken） | VFX Graph |
|---|---|---|
| **模拟在哪** | CPU | GPU（Compute Shader） |
| **粒子数量上限** | 几千～几万（CPU 开销限制） | 数百万（GPU 并行） |
| **灵活性** | 模块化 Inspector | 节点图，可视化编程 |
| **物理交互** | 有限（碰撞等） | 可以读取深度缓冲做碰撞 |
| **渲染路径** | 传统 Draw Call | 同样走 Vertex→Fragment，但几何生成在 GPU |

VFX Graph 的粒子状态（位置/速度/颜色）完全在 GPU 显存里，不回传 CPU。这使得几百万粒子的模拟成为可能——但也意味着 CPU 侧无法直接读取单个粒子的状态，游戏逻辑不能直接用粒子位置做碰撞检测等操作。

**选择建议**：
- 游戏内常规特效（技能、受击、环境）：Particle System 足够，工具链成熟
- 需要大量粒子的视觉效果（数万以上）、电影级特效：VFX Graph
- 两者可以混用——同一个场景里既有 Particle System 也有 VFX Graph

---

## 粒子特效的常见性能问题

**Overdraw（过度绘制）**：大量半透明粒子叠加，每个像素被绘制多次。可以用 Frame Debugger 的 Overdraw 模式目视确认，高亮的区域就是 Overdraw 严重的地方。缓解方式：降低粒子数量、缩小粒子面积、减少同屏叠加层数。

**Draw Call 过多**：每个粒子系统是一个 Draw Call，大量小特效累积后数量可观。使用 GPU Instancing（Mesh 粒子）、合并粒子系统、或用 VFX Graph 减少 Draw Call。

**大贴图粒子**：粒子 Material 用了高分辨率贴图，但实际屏幕上粒子很小——浪费带宽和内存。粒子贴图通常不需要超过 512×512，图集（多个粒子效果打包在一张贴图里）能进一步减少贴图绑定切换。

---

## 和下一篇的关系

粒子是特效的几何层。还有一层覆盖在整张画面上的处理——后处理。Bloom 让粒子的发光区域产生光晕扩散，Tonemapping 把 HDR 的高亮压缩到可显示范围。下一篇讲后处理管线：Volume 系统和全屏 Pass 的工作机制。
