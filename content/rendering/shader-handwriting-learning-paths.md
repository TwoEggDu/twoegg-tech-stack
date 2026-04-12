---
date: "2026-03-28"
title: "Shader 学习分层入口｜先分清入门基础、光照与常用技法、项目实战，再决定从哪条线进去"
description: "给 Shader 手写技法系列补一个二级分流入口：当总目录已经很长时，先按学习阶段和目标进入，比直接在 70 多篇文章里横跳更稳。"
slug: "shader-handwriting-learning-paths"
weight: 3995
featured: false
tags:
  - "Shader"
  - "HLSL"
  - "Unity"
  - "Index"
  - "Learning Path"
---

> 这是 `Shader 手写技法系列索引` 下面的二级入口页。它不替代总索引，而是专门解决一个问题：`当文章已经很多时，你到底该从哪条线进入。`

如果你还没看过总入口，先回：

- [Shader 手写技法系列索引｜从第一个 Shader 到进阶技法与项目实战]({{< relref "rendering/shader-handwriting-series-index.md" >}})

## 为什么需要这一页

`Shader` 这一组内容现在已经不适合只靠一张长目录顺着看了。

因为它其实已经自然长成了三条不同的学习路径：

- 从零入门，先把 `ShaderLab / HLSL / 顶点片元 / 空间 / 调试` 立住
- 在会写的基础上，继续补 `光照 / 常用效果 / 场景技法`
- 直接为了项目落地，进入 `完整方案 / 高级专题 / 调试与扩展`

如果不先分流，读者很容易出现两种典型问题：

- 还没立住基础，就提前扎进 `SSR / GPU Driven / Compute Shader`
- 明明只是想解决项目里的某类效果，却被大长目录拖回基础顺序

## 路线一：从零入门线

这条线适合：

- 第一次手写 Shader
- 还没把 `Properties / Pass / vertex / fragment / 坐标空间 / 调试` 立住
- 之前只会改现成 Shader，不知道每一层到底在干什么

建议顺序：

1. [Shader 手写入门 00｜我的第一个 Shader：让物体显示纯色]({{< relref "rendering/shader-intro-00-first-shader.md" >}})
2. [Shader 手写入门 01｜时间动画]({{< relref "rendering/shader-intro-01-time-animation.md" >}})
3. [Shader 手写入门 02｜纹理与 UV]({{< relref "rendering/shader-intro-02-texture-uv.md" >}})
4. [Shader 手写入门 03｜Lambert 漫反射]({{< relref "rendering/shader-intro-03-lambert.md" >}})
5. [Shader 手写入门 04｜顶点动画]({{< relref "rendering/shader-intro-04-vertex-animation.md" >}})
6. [Shader 基础 01｜数据类型]({{< relref "rendering/shader-basic-01-data-types.md" >}})
7. [Shader 基础 03｜向量与矩阵]({{< relref "rendering/shader-basic-03-vector-matrix.md" >}})
8. [Shader 基础 07｜ShaderLab 结构]({{< relref "rendering/shader-basic-07-shaderlab-structure.md" >}})
9. [Shader 基础 08｜Vertex Shader]({{< relref "rendering/shader-basic-08-vertex-shader.md" >}})
10. [Shader 基础 09｜Fragment Shader]({{< relref "rendering/shader-basic-09-fragment-shader.md" >}})
11. [Shader 基础 06｜调试]({{< relref "rendering/shader-basic-06-debugging.md" >}})

这条线的目标不是“做出很炫的效果”，而是先建立最小稳定直觉：

- 一个 Shader 文件到底由哪些层组成
- 数据怎样从 CPU、顶点阶段一路传到片元阶段
- 为什么很多错误其实是空间、插值或调试方式没立住

## 路线二：光照与常用技法线

这条线适合：

- 你已经能写基础 Shader
- 你现在更关心“常见效果怎么拆”
- 你想把 `光照模型` 和 `项目里常用技法` 接起来

建议先走核心光照主线：

1. [Shader 核心光照 01｜Blinn-Phong]({{< relref "rendering/shader-lighting-01-blinn-phong.md" >}})
2. [Shader 核心光照 02｜法线贴图]({{< relref "rendering/shader-lighting-02-normal-map.md" >}})
3. [Shader 核心光照 03｜阴影]({{< relref "rendering/shader-lighting-03-shadows.md" >}})
4. [Shader 核心光照 04｜Additional Lights]({{< relref "rendering/shader-lighting-04-additional-lights.md" >}})
5. [Shader 核心光照 05｜PBR]({{< relref "rendering/shader-lighting-05-pbr.md" >}})
6. [Shader 核心光照 06｜IBL]({{< relref "rendering/shader-lighting-06-ibl.md" >}})

然后按你遇到的问题切入常用技法：

### 你主要在做角色 / NPR

- [Shader 技法 03｜Cel Shading]({{< relref "rendering/shader-technique-03-cel-shading.md" >}})
- [Shader 技法 04｜Outline]({{< relref "rendering/shader-technique-04-outline.md" >}})
- [Shader Character 01｜Hair]({{< relref "rendering/shader-character-01-hair.md" >}})
- [Shader Character 02｜Eye]({{< relref "rendering/shader-character-02-eye.md" >}})

### 你主要在做透明、屏幕空间和后处理类效果

- [Shader 技法 05｜Transparency]({{< relref "rendering/shader-technique-05-transparency.md" >}})
- [Shader 技法 06｜Refraction]({{< relref "rendering/shader-technique-06-refraction.md" >}})
- [Shader 技法 10｜Screen Space UV]({{< relref "rendering/shader-technique-10-screen-space-uv.md" >}})
- [Shader 技法 11｜Emission 与 Bloom]({{< relref "rendering/shader-technique-11-emission-bloom.md" >}})

### 你主要在做环境、天气和地形

- [Shader Weather 01｜Skybox]({{< relref "rendering/shader-weather-01-skybox.md" >}})
- [Shader Weather 03｜Rain / Snow]({{< relref "rendering/shader-weather-03-rain-snow.md" >}})
- [Shader Weather 04｜Volumetric]({{< relref "rendering/shader-weather-04-volumetric.md" >}})
- [Shader Terrain 01｜Heightmap]({{< relref "rendering/shader-terrain-01-heightmap.md" >}})
- [Shader Terrain 04｜Custom Terrain Shader]({{< relref "rendering/shader-terrain-04-custom-shader.md" >}})

如果你走这条线，建议不要跳过：

- [Shader 基础 05｜宏、变体与编译开关]({{< relref "rendering/shader-basic-05-macros-variants.md" >}})
- [Shader 基础 10｜URP Lit 拆解]({{< relref "rendering/shader-basic-10-urp-lit-breakdown.md" >}})

因为这两篇会把“会写效果”和“能落到项目里”接起来。

## 路线三：项目实战与高级专题线

这条线适合：

- 你已经能独立写常见效果
- 你现在更关心完整方案、性能、调试、扩展边界
- 你是在真实项目里带着问题来查

建议先从项目型文章进入：

1. [项目实战 01｜卡通角色完整 Shader]({{< relref "rendering/shader-project-01-toon-character.md" >}})
2. [项目实战 02｜写实武器 Shader]({{< relref "rendering/shader-project-02-realistic-weapon.md" >}})
3. [项目实战 03｜真实水体]({{< relref "rendering/shader-project-03-realistic-water.md" >}})
4. [项目实战 04｜草地系统]({{< relref "rendering/shader-project-04-grass-system.md" >}})
5. [项目实战 06｜UI 特效]({{< relref "rendering/shader-project-06-ui-effects.md" >}})
6. [项目实战 07｜后处理效果]({{< relref "rendering/shader-project-07-post-process-effects.md" >}})
7. [项目实战 08｜调试工作流]({{< relref "rendering/shader-project-08-debug-workflow.md" >}})

如果你更偏高级专题，按问题切：

### 你关心 Renderer Feature / RenderGraph / 管线扩展

- [Shader 进阶 03｜Renderer Feature]({{< relref "rendering/shader-advanced-03-renderer-feature.md" >}})
- [URP 扩展 01｜Renderer Feature]({{< relref "rendering/urp-ext-01-renderer-feature.md" >}})
- [URP 扩展 02｜RenderGraph]({{< relref "rendering/urp-ext-02-rendergraph.md" >}})

### 你关心 Compute / GPU 驱动 / 现代图形专题

- [Shader 进阶 14｜Compute Shader]({{< relref "rendering/shader-advanced-14-compute-shader.md" >}})
- [Shader 进阶 16｜Ray Tracing]({{< relref "rendering/shader-advanced-16-raytracing.md" >}})
- [Shader 进阶 18｜GPU Driven]({{< relref "rendering/shader-advanced-18-gpu-driven.md" >}})
- [Shader 进阶 19｜GPU Scene]({{< relref "rendering/shader-advanced-19-gpu-scene.md" >}})

### 你关心性能、移动端和项目治理

- [Shader 进阶 10｜移动端优化]({{< relref "rendering/shader-advanced-10-mobile-optimization.md" >}})
- [Unity Shader Variant 系列索引]({{< relref "rendering/unity-shader-variants-series-index.md" >}})
- [URP 深度系列索引]({{< relref "rendering/urp-deep-dive-series-index.md" >}})

## 该怎么选

- 如果你现在还经常分不清 `Pass / 语义 / 空间 / 顶点片元分工`，走路线一。
- 如果你已经能写基础代码，但想系统掌握“常见效果为什么这样拆”，走路线二。
- 如果你已经在项目里做整套方案、调试复杂问题或碰到管线扩展，走路线三。

## 最后一个建议

这页是二级入口，不是完整目录。

如果你已经知道自己要找哪篇，或者想看完整覆盖范围，还是应该回总入口：

- [Shader 手写技法系列索引｜从第一个 Shader 到进阶技法与项目实战]({{< relref "rendering/shader-handwriting-series-index.md" >}})
