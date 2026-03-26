---
title: "HDRP 的定位与取舍"
slug: "unity-rendering-11-hdrp-positioning"
date: "2025-01-26"
description: "HDRP 和 URP 在核心架构上的本质差异：Deferred-first 光照、物理正确的 Lit Shader、更完整的 Volume 系统，以及它适合什么项目、不适合什么项目。"
tags:
  - "Unity"
  - "HDRP"
  - "URP"
  - "渲染管线"
  - "Deferred"
  - "PBR"
series: "Unity 渲染系统"
weight: 1400
---
如果只用一句话概括这篇：HDRP 是 Unity 在 SRP 上构建的高端渲染管线，它用更复杂的架构换取更高保真度的视觉效果——适合 PC/主机高画质项目，不适合移动端，也不适合对渲染管线有深度自定义需求的项目。

---

## 从上一篇出发

前几篇把 URP 从架构到扩展写法都讲清楚了。HDRP 是 Unity 的另一条官方 SRP 实现。它和 URP 同样建立在 `RenderPipelineAsset` / `RenderPipeline` / `ScriptableRenderContext` 这套基础之上，但在此之上做出了截然不同的设计选择。

---

## HDRP 的核心设计差异

### 1. 渲染路径：Deferred-first

URP 的默认路径是 Forward（一个物体一次 Draw Call，Fragment Shader 里完整计算光照），HDRP 的默认路径是 **Deferred Shading**：

```
HDRP Deferred 流程：

GBuffer Pass（每个不透明物体一次 Draw Call）
  → RT0: 漫反射颜色 (Albedo)
  → RT1: 法线 (World Normal)
  → RT2: 材质参数 (Roughness, Metallic, AO 等)
  → RT3: 自发光 (Emission)
  → Depth: 深度

Lighting Pass（对 GBuffer 做全屏光照计算）
  → Tile-based / Cluster Light Culling（屏幕分成 8×8 Tile，每个 Tile 只处理影响它的光源）
  → 直接光：逐光源累加
  → 间接光：Lightmap / Light Probe / SSGI / Screen Space Reflection
  → 输出 HDR 颜色 Buffer
```

这意味着：
- 场景里有 100 盏点光源，每盏光只影响覆盖它的像素，不增加 Draw Call 数量
- 每个可见像素只做一次完整的 PBR 光照计算（已通过 Depth Test，不存在 OverDraw 的光照浪费）
- 半透明物体仍然走 Forward（Deferred 不支持透明度），但不透明部分由 Deferred 完整处理

### 2. 物理正确的 Lit Shader

HDRP 的 Lit Shader 比 URP 的 Lit 更"物理正确"，它强制要求输入符合物理范围：

| 参数 | URP 的范围 | HDRP 的要求 |
|---|---|---|
| Albedo | 0–1，无限制 | 避免纯黑（< 0.02）或纯白（> 0.95），否则能量不守恒 |
| Emissive | 0–1 HDR | 使用真实物理单位（尼特，cd/m²）|
| Smoothness | 0–1 | 同上，但配合 Specular Response Curve |
| Exposure | 曝光为后处理参数 | 场景本身就在 EV100 曝光单位下工作 |

HDRP 还内置了 URP 没有的材质类型：
- **Subsurface Scattering（SSS）**：皮肤、蜡、玉石的次表面散射（光线在材质内部传播后出射）
- **Translucency**：树叶、薄布料的透射光效果
- **Hair**：各向异性高光的头发渲染（Marschner 模型）
- **Eye**：角膜折射 + 虹膜次表面散射

这些材质类型需要 G-Buffer 里存储额外的材质数据，这是 HDRP G-Buffer 格式比 URP 更复杂的原因。

### 3. 更完整的 Volume 系统

HDRP 的 Volume 系统涵盖的效果比 URP 多得多：

| 类别 | URP Volume 支持 | HDRP Volume 支持 |
|---|---|---|
| 基础后处理 | Bloom / Tonemapping / Color Grading / SSAO / DOF / Vignette | 同上，参数更精细 |
| 全局光照 | 不支持（需要 Lightmap） | SSGI（屏幕空间全局光照） |
| 反射 | Reflection Probe（离线） | SSR（屏幕空间反射，实时） |
| 天空 | 简单 HDR 天空盒 | 物理天空（大气散射模拟，时间驱动日夜循环） |
| 云 | 不支持 | 体积云（Ray Marching 模拟） |
| 雾 | 简单线性雾 | 体积雾（Light Scattering，光束穿透雾的视觉效果） |
| 曝光 | 后处理 Tonemapping 控制 | Automatic / Fixed / Physical Camera（快门 / ISO / 光圈）|
| 阴影 | Cascade Shadow Map | 同上 + PCSS（Percentage Closer Soft Shadows，接触软化阴影）|

### 4. 物理摄像机模型

HDRP 的 Camera 对应一台真实物理摄像机：

```
HDRP Camera 参数（对应真实摄像机）：
  Aperture：光圈（f/1.4 到 f/22），影响 DOF 和曝光
  Shutter Speed：快门速度（1/60s 到 1/8000s），影响运动模糊和曝光
  ISO：感光度（100 到 6400），影响曝光
  Focus Distance：对焦距离，用于 DOF 计算
```

这意味着场景里的光源强度也必须用物理单位（流明、坎德拉）来设置，才能和摄像机曝光正确配合。这对需要做摄影写实渲染的项目（建筑可视化、影视过场）是优势，但对游戏项目来说调光工作量更大。

---

## HDRP 的扩展机制

HDRP 也基于 SRP，但它的扩展方式和 URP 不同。

URP 有 `ScriptableRendererFeature`（清晰的插件接口），HDRP 的扩展需要直接修改管线更深层的代码，或者使用以下方式：

- **Custom Pass Volume**：HDRP 特有的扩展点，用于在特定阶段插入自定义渲染，类似 URP 的 RendererFeature，但配置方式不同（通过场景中的 Volume 组件控制）
- **HDRP Asset 的配置**：很多效果（体积雾、SSR）在 HDRP Asset 里有全局开关，需要先开启才能在 Volume 里激活
- **Shader Graph**：HDRP 有自己的 Shader Graph 节点（HDLit、HDUnlit），与 URP 的节点不通用

总体来说，HDRP 的自定义扩展门槛更高，文档更稀疏，主要面向专业渲染工程师。

---

## 适合用 HDRP 的项目

**适合：**

- PC / 主机平台，画质是核心卖点（AAA 游戏、影视级过场、建筑可视化）
- 场景需要大量实时动态光源（城市夜景、室内复杂光照）
- 需要写实皮肤（SSS）、头发（Hair Shader）、玻璃（透射）等材质
- 需要物理天空 + 体积云 + 体积雾的自然环境效果
- 团队有专职渲染工程师，能处理管线调试和性能优化

**不适合：**

- 移动端（HDRP 不支持 iOS / Android / WebGL）
- 需要对渲染管线做大幅自定义的项目（HDRP 扩展接口比 URP 复杂很多）
- 卡通/风格化渲染（HDRP 的 Shader 系统强绑定物理正确假设，绕开它比 URP 难）
- 小团队或原型项目（HDRP 的调试和配置学习成本比 URP 高）

---

## URP vs HDRP：架构对比

| | URP | HDRP |
|---|---|---|
| 目标平台 | Mobile / PC / Console / Web | PC / Console（高端） |
| 默认渲染路径 | Forward（可选 Deferred / Forward+）| Deferred-first |
| 多光源策略 | Forward+ Tile Lighting（2022+）| Tile/Cluster Deferred |
| Shader 模型 | Lit（PBR，可扩展）| HDLit（更完整 PBR，含 SSS/Hair/Eye）|
| 后处理范围 | 基础效果集 | 扩展效果集（物理天空/体积云/SSGI/SSR）|
| 物理摄像机 | 不支持 | 支持（快门/光圈/ISO）|
| 扩展机制 | ScriptableRendererFeature（清晰插件接口）| Custom Pass Volume（相对复杂）|
| 适合自定义渲染 | 是（URP 是常见卡通渲染的基础）| 有限（强物理假设） |
| 调试复杂度 | 中 | 高（G-Buffer 多、Volume 参数多）|
| 移动端支持 | 是 | 否 |
| 文档完善度 | 好 | 中（高级功能文档稀疏）|

---

## 从 URP 迁移到 HDRP 的代价

如果项目已经在 URP 上开发了一段时间，迁移到 HDRP 需要：

1. **Shader 全部重写**：URP Lit / Unlit Shader 和 HDRP HDLit / HDUnlit 不兼容，Shader Graph 的节点也不通用，所有材质需要重新设置参数
2. **Light 重新调光**：URP 的灯光强度单位（Lumen 或任意）和 HDRP 的物理单位不同，全场景重调
3. **Post-processing 重新配置**：两套 Volume Profile 不通用，效果参数需要重新调
4. **第三方插件兼容性检查**：很多插件只支持 URP，迁移到 HDRP 需要确认每个插件是否有 HDRP 版本

实际项目中，从 URP 到 HDRP 的迁移通常被认为是"近似于重做视觉资产"的工作量，一般只在项目非常早期或有明确高画质需求时才做。

---

## 系列总结

本系列（Unity 渲染系统）到这里就全部写完了。用一张地图回顾：

```
[游戏渲染资产篇]
  00 综述：所有渲染资产的全景图
  01 几何与表面：Mesh / Material / Texture → 像素
  01b Draw Call 与批处理：CPU 每次向 GPU 发什么
  01c Render Target 与帧缓冲区：GPU 把结果写到哪里
  01d Frame Debugger：用 Unity 内置工具诊断渲染问题
  01e RenderDoc 入门：捕获第一帧并读懂它
  01f RenderDoc 进阶：顶点数据 / Pipeline State / Shader Debugger
  02 光照资产：实时光 / Lightmap / Light Probe / Reflection Probe
  03 骨骼动画：蒙皮矩阵 / Blend Shape
  04 粒子与特效：Billboard / Mesh / Trail / VFX Graph
  05 后处理：Volume 系统 / Bloom / Tonemapping / SSAO / DOF

[渲染管线篇]
  06 Built-in 固定渲染管线：Camera Culling / Forward / Deferred / 限制
  07 为什么需要 SRP：Built-in 的架构瓶颈 / SRP 的解法和代价
  08 SRP 核心概念：RenderPipelineAsset / RenderPipeline / Context / CommandBuffer
  09 URP 架构：四层层级 / 默认 Pass 顺序 / Frame Debugger 对应关系
  10 URP 扩展：RendererFeature + RenderPass 实践 / RTHandle / RenderGraph
  11 HDRP 定位：Deferred-first / 物理 Lit / Volume 系统 / URP vs HDRP
```

上游的"Unity 资产系统与序列化"系列负责这些渲染资产被打包和加载进内存的部分，下游的"Unity Shader Variant 治理"系列负责 Shader 在打包和运行时的变体管理问题。三个系列共同构成 Unity 渲染工程的完整知识地图。
