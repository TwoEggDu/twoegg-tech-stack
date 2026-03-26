---
title: "Unity 渲染系统补F｜渲染算法对比：GI 方案（Lightmap/Probe/SSGI/Lumen）"
slug: "unity-rendering-supp-f-gi-comparison"
date: "2026-03-26"
description: "全局光照（GI）让场景里的间接光照真实可信。主流方案从静态烘焙（Lightmap）到动态探针（Light Probe），从屏幕空间（SSGI）到 Lumen，各有适用场景和代价。这篇做一次横向对比，帮助选择合适的方案。"
weight: 1550
tags:
  - "Unity"
  - "Rendering"
  - "GI"
  - "全局光照"
  - "Lightmap"
  - "Light Probe"
  - "Lumen"
  - "SSGI"
series: "Unity 渲染系统"
---
只有直接光照的场景总有一种说不清的"假"——阴影太硬、背光面太黑、相邻物体的颜色不互相影响。现实世界里，光线在表面之间反弹，产生色溢、环境光遮蔽、柔和的间接阴影。这就是全局光照（GI，Global Illumination）要解决的问题。从离线烘焙到实时计算，GI 方案的技术选择直接决定了项目的画质上限和性能成本。

---

## GI 的核心问题：为什么不能只有直接光照

直接光照只计算光源→表面的一次照射，忽略了：

1. **光线弹射（Indirect Bounces）**：光打在红墙上，红色光会弹射到旁边的白墙，产生红色色溢（Color Bleeding）
2. **环境光遮蔽（Ambient Occlusion）**：凹陷、缝隙处的间接光照被遮挡，产生接触处的暗色晕影
3. **天空光/环境光**：来自整个半球的低频环境光照，让背光面不是纯黑

解决这些问题的方法有两条路：**离线预计算（烘焙）** 把间接光照信息存到纹理/数据结构里，运行时直接查表；**实时计算** 每帧动态计算间接光，性能代价极高。实际项目通常是两者的组合。

---

## Lightmap：烘焙 GI 的基石

### 工作原理

Lightmap 把间接光照信息预计算后存储为纹理：

1. 场景中每个静态物体展开第二套 UV（UV2，光照 UV），确保每个像素对应唯一的世界空间位置
2. Progressive Lightmapper（CPU 或 GPU 版本）对每个 Lightmap 像素发射光线，追踪间接弹射
3. 结果存储为 `.exr`/`.hdr` 格式的 Lightmap 纹理，运行时直接采样

```
Lightmap 纹理采样（Shader 内部）：
色彩 = 直接光照（实时） + 间接光照（Lightmap 采样）
```

### UV2 展开要求

质量好的 Lightmap 依赖正确的 UV2：

- **无重叠（No Overlap）**：每个 UV2 像素对应唯一的表面位置，否则不同位置的光照信息混叠
- **无间距不足（Adequate Padding）**：UV 岛之间需要足够间距（通常 2~4 像素），避免 Bilinear 过滤时相邻岛渗色
- **尽量拉直（Aligned Edges）**：沿 Lightmap 网格对齐的边缘减少锯齿

Unity 可以自动生成 UV2（`Generate Lightmap UVs` 选项），但复杂模型建议美术手动展开以控制像素密度分布。

### 静态物体限制

Lightmap 只支持**标记为 Lightmap Static 的物体**。动态物体（角色、载具）无法烘入 Lightmap，它们的间接光照需要用其他方案（Light Probe）补充。

### 内存与质量参数

| 参数 | 说明 | 典型值 |
|-----|-----|-------|
| 分辨率 | Lightmap 纹理分辨率 | 1024×1024 / 2048×2048 |
| Texels Per Unit | 每单位世界长度对应的 Lightmap 像素数 | 移动端 4~10，PC 20~40 |
| 内存占用 | 1024×1024 RGBA HDR（EXR）≈ 4MB | 复杂室内场景可能需要 20+ 张 |
| Bounces | 光线弹射次数 | 1~3 次，次数越多越慢 |

一个大型室内场景可能需要 10~30 张 Lightmap，总内存 40~120MB，烘焙时间数小时。

### 适用场景评估

- **强烈推荐**：静态室内场景、固定光源的关卡
- **不适合**：昼夜交替（光照变化）、玩家可改变场景结构的游戏
- **更新代价**：任何静态物体或光源变化都需要重新烘焙

---

## Light Probe：动态物体的 GI 补丁

### 球谐函数存储低频间接光

Light Probe 在场景中放置若干采样点，每个采样点存储来自四面八方的间接光照信息。存储格式是 **球谐函数（Spherical Harmonics，SH）**：

- L1 阶 SH：4 个系数/通道 × 3 通道（RGB）= 12 个 float → 低频，近似漫反射环境光
- L2 阶 SH：9 个系数/通道 × 3 通道 = 27 个 float → 更高频，接近实际 Unity 使用（2 阶实际是 9 个系数）

运行时，动态物体（角色）在 3 个最近 Probe 构成的四面体内插值，得到近似的间接光照方向和颜色，以 SH 形式传入 Shader，添加到漫反射计算中。

**代价极低**：SH 计算是几十次 float 乘加，在顶点着色器中完成，几乎不影响 GPU 性能。

### Light Probe Group 布置密度

Probe 的布置原则：

- **光照变化大的区域放密**：窗边、门口、阴影边界
- **开阔均匀区域放疏**：大厅中央、空旷户外
- **避免放在几何体内部**：Probe 在墙里会采样到错误的光照

```
推荐密度参考：
- 室内关键区域：每 1~2 米一个 Probe
- 室外开阔场景：每 5~10 米一个 Probe
- 走廊/门口过渡区：加密到每 0.5 米
```

### LPPV：大型动态物体的 GI

普通 Light Probe 对整个物体应用同一个 SH 值，对大型物体（载具、建筑立面）会产生光照方向错误。**LPPV（Light Probe Proxy Volume）** 在物体的 AABB 范围内生成一个 3D Probe 网格，不同位置的顶点采样不同的 SH 值：

```
LPPV 参数：
- Resolution Mode: Automatic / Custom
- Custom 下可设置 X/Y/Z 方向的 Probe 数量（如 4×4×4 = 64 个虚拟 Probe）
```

LPPV 适合移动的大型物体，代价比普通 Light Probe 高一些，但仍远低于任何实时 GI 方案。

---

## Adaptive Probe Volume（APV）：Unity 2022+ 的自动化 Probe

### 解决手动摆放的痛点

传统 Light Probe Group 需要美术手动逐个摆放，大型场景可能需要放置数千个 Probe，工作量巨大且容易出错。**APV（Adaptive Probe Volume）** 是 Unity 2022.2 引入的新系统：

- **自动分布**：根据场景几何体自动在空间中分布 Probe，密度随几何复杂度自适应调整
- **Brick 层级结构**：类似 SVO（Sparse Voxel Octree），稀疏区域用大 Brick（低密度），复杂区域用小 Brick（高密度）
- **流式加载**：支持 Streaming，大型开放世界按视距加载 Probe 数据，不必全部常驻内存

### 与传统 Light Probe Group 的对比

| 维度 | Light Probe Group | Adaptive Probe Volume |
|-----|------------------|-----------------------|
| 布置方式 | 手动摆放 | 自动生成 |
| 密度控制 | 完全手动 | 自动 + 可手动调整权重 |
| 流式加载 | 不支持 | 支持 |
| 精度 | 取决于摆放质量 | 稳定，自适应 |
| Unity 版本 | 所有版本 | 2022.2+（URP/HDRP） |
| 当前状态 | 成熟稳定 | 实验性→逐步稳定 |

APV 是 Unity 未来 GI 工作流的方向，新项目（Unity 2023+）推荐评估使用。

---

## SSGI：屏幕空间全局光照

### 工作原理

SSGI（Screen Space Global Illumination）从当前帧已渲染的颜色缓冲和深度缓冲中估计间接光照：

1. 从当前像素发射若干条屏幕空间射线
2. 射线沿深度缓冲追踪，找到"命中"的表面
3. 采样命中表面的颜色作为间接光照来源
4. 累积多条射线的结果，加权平均

优点：完全动态，无需预计算，每帧实时更新。

### 固有缺陷

**只有屏幕内的信息可用**：

- 屏幕外的物体无法参与 GI 计算（摄像机转身时间接光照消失）
- 被遮挡的表面（在屏幕上但被前景遮挡）无法提供光照
- 边缘漏光（Edge Bleeding）：深度缓冲的不连续导致画面边缘出现光照泄漏

这些是 SSGI 的根本性缺陷，无法通过参数调整完全消除。

### Unity 中的支持

- **HDRP**：内置 SSGI（`Screen Space Global Illumination`，Volume 组件），用于高端 PC/主机
- **URP**：原生不支持 SSGI，需要自定义 Renderer Feature
- **移动端**：由于 Bandwidth 限制和 Tile-Based GPU 架构，SSGI 通常不适合移动端

---

## Lumen：Unreal Engine 的全动态 GI（对比参考）

Lumen 是 Unreal Engine 5 的核心 GI 系统，代表了当前实时 GI 的工程高峰。理解 Lumen 有助于判断 Unity 当前方案的定位：

### Lumen 的核心技术组合

| 组件 | 说明 |
|-----|-----|
| **Mesh Distance Field** | 场景中每个 Mesh 预计算有向距离场（SDF），用于快速光线追踪 |
| **Global Distance Field** | 全场景合并 SDF，用于远距离低频光照 |
| **Screen Space Radiance Cache** | 屏幕空间辐射度缓存，利用时序积累提升质量 |
| **Surface Cache** | 每个 Mesh 表面的材质/光照缓存（低分辨率贴图），用于间接弹射 |
| **Hardware Ray Tracing（可选）** | 在支持 DXR 的 GPU 上用硬件光追替代 SDF 追踪，质量更高 |

Lumen 的关键特性：光源、几何体、材质任意变化，GI 在数帧内响应更新。代价：高端 PC（RTX 3080 级别）才能流畅运行，主机平台（PS5/Xbox Series X）通过专门优化可以达到 30~60fps。

### Unity 目前没有等价方案

Unity 目前（Unity 6）没有与 Lumen 等价的全动态 GI 系统。HDRP 的 Ray Tracing GI（依赖 DXR）可以实现类似效果，但性能要求更高且生态成熟度不及 Lumen。

---

## Enlighten Realtime GI：Unity 的旧实时 GI 方案

Unity 2017~2021 时代，内置的 **Enlighten Realtime GI** 基于**辐射度（Radiosity）算法**实现运行时 GI：

- 把场景分解为若干 Patch（平面区域）
- 预计算 Patch 间的辐射度传递矩阵
- 运行时每帧更新传递结果，支持动态改变光源颜色/强度（但不支持移动几何体）

**Unity 2022 已将 Enlighten Realtime GI 标记为弃用（Deprecated）**，Unity 6 中继续保留但不再开发。原因：

- 预计算数据量大
- 更新频率受限（约 15fps 的 GI 更新，有滞后感）
- 被 APV + 探针系统的工作流替代

---

## 选择矩阵

根据目标平台和场景动态性，推荐的 GI 方案组合：

### 按平台

| 平台 | 推荐方案 | 备注 |
|-----|---------|-----|
| 移动端（iOS/Android） | Lightmap + Light Probe | SSGI、RTGI 均不适合；APV 可评估 |
| 主机（PS5/Xbox Series X） | Lightmap + APV / HDRP SSGI | 可酌情使用低质量 SSGI |
| 高端 PC | Lightmap + HDRP SSGI / RTGI | RTX 显卡可用 Ray Tracing GI |
| VR | Lightmap + Light Probe | 性能预算极紧，避免实时 GI |

### 按场景动态性

| 场景类型 | 推荐方案 | 说明 |
|---------|---------|-----|
| **完全静态**（关卡固定，无昼夜） | Lightmap（主）+ Light Probe（动态物体） | 画质最好，代价最低 |
| **半动态**（固定几何，动态光源颜色/强度） | Lightmap（静态） + Light Probe（动态） | 光源颜色变化时 Lightmap 失效，需权衡 |
| **昼夜交替** | APV（多组烘焙插值） + Light Probe | 烘焙多套 Probe 数据，运行时按时间插值 |
| **全动态**（可破坏场景、实时生成关卡） | SSGI（HDRP，PC）或 Light Probe 近似 | 无法烘焙，只能实时方案或接受降质 |

### 代价汇总

| 方案 | 烘焙时间 | 运行时 CPU | 运行时 GPU | 内存 |
|-----|---------|----------|----------|-----|
| Lightmap | 高（分钟~小时） | 极低 | 极低（纹理采样） | 高（数十 MB） |
| Light Probe | 低（秒~分钟） | 极低 | 极低（SH 运算） | 低（<1MB） |
| APV | 中（分钟~小时） | 低 | 低 | 中（支持流式） |
| SSGI | 无 | 无 | 高（2~5ms） | 中（屏幕缓冲） |
| HDRP RTGI | 无 | 无 | 极高（仅 RTX） | 高 |

---

## 小结

GI 方案的选择是一个权衡游戏：画质、动态性、性能、工作量四者不可兼得。

- **Lightmap** 是画质和性能最优的静态 GI，静态场景的首选，代价是工作量和更新周期
- **Light Probe** 以极低代价为动态物体提供近似 GI，是所有平台的必选补充
- **APV** 是 Unity 未来的 Probe 工作流，解决手动摆放的工程痛点，新项目值得评估
- **SSGI** 是高端 PC/主机的动态 GI 选项，屏幕外信息缺失是无法回避的根本缺陷
- **Lumen** 代表全动态 GI 的工程高峰，目前是 Unreal 独有，Unity 无等价方案
- **Enlighten Realtime GI** 已弃用，新项目不要选择
