+++
title = "Unity 渲染系统补L｜Virtual Texturing：原理、UDIM 与 Unreal 的 Virtual Heightfield"
slug = "unity-rendering-supp-l-virtual-texturing"
date = 2026-03-26
description = "传统贴图方式要求所有纹理常驻显存，大型开放世界场景的贴图总量远超显存容量。Virtual Texturing（虚拟纹理）借鉴虚拟内存的思路，只把当前可见的纹理 Tile 加载到显存。这篇讲清楚 VT 的原理、UDIM 工作流，以及 Unreal 的 Virtual Heightfield Mesh。"
weight = 1610
[taxonomies]
tags = ["Unity", "Rendering", "Virtual Texturing", "UDIM", "开放世界", "性能优化"]
[extra]
series = "Unity 渲染系统"
+++

开放世界游戏的贴图总量往往以 TB 计——地形、建筑、植被、道路，每一类都有不同分辨率的 Albedo、Normal、Roughness 贴图。显存永远装不下这些数据的全部。

Virtual Texturing 的答案是：不装全部，只装"此刻摄像机看到的部分"。这个思路直接类比操作系统的虚拟内存分页机制，把一张巨大的虚拟纹理切成小 Tile，按需加载到有限的物理显存中。

---

## 一、Virtual Texturing 原理

### 核心数据结构

Virtual Texturing 系统由两张关键纹理支撑：

**Virtual Page Table（虚拟页表纹理）：**
- 一张低分辨率纹理，每个 Texel 对应虚拟纹理中的一个 Tile
- 存储每个 Tile 在 Physical Tile Cache 中的物理坐标（U, V 偏移）和当前 Mip 级别
- 每帧可部分更新（只更新发生变化的条目）

**Physical Tile Cache（物理 Tile 缓存纹理）：**
- 一张实际占用显存的纹理，大小固定（如 4096×4096，分割为若干 128×128 的 Tile 槽位）
- 所有已加载的 Tile 数据实际存储于此
- 采用 LRU（Least Recently Used）策略淘汰长期不可见的 Tile

**工作流程示意：**

```
Shader 采样虚拟纹理
    ↓
查询 Virtual Page Table（得到 Tile 的物理坐标）
    ↓
采样 Physical Tile Cache（得到实际颜色/法线数据）
    ↓
若 Page Miss（Tile 未加载）→ 标记为待请求 → 异步从磁盘加载 → 写入 Physical Cache → 更新 Page Table
```

### Mip 层级与 VT

Virtual Texturing 同样维护 Mip 层级（VT Mip），但其含义与传统 Mipmap 略有不同：

- 远处物体的 VT Mip 更高（低分辨率 Tile），近处 Mip 更低（高分辨率 Tile）
- 每个 Mip 级别都有独立的 Page Table 和对应的 Tile 集合
- 系统根据 Shader 中计算出的屏幕空间导数（`ddx`/`ddy`）决定应请求哪个 Mip 的 Tile

**Tile 尺寸与 Border（边界像素）：**

Tile 并非只存有效像素，边缘还会存储若干像素的 Border（通常 4px），用于双线性/三线性采样时的跨 Tile 过渡，避免 Tile 缝隙处出现接缝。

### Streaming VT vs Procedural VT

两种 VT 变体适用于不同场景：

| 类型 | 数据来源 | 典型用途 | 主要优势 |
|------|----------|----------|----------|
| Streaming VT（SVT） | 磁盘预制贴图，异步流式加载 | 地形、大型静态环境 | 支持超大纹理，低内存占用 |
| Procedural VT（PVT） | GPU 实时合成（材质混合、高度图等） | 动态材质层混合、地形细节 | 无磁盘 IO，运行时生成 |

Streaming VT 依赖高效的磁盘 IO（NVMe SSD 显著提升体验），而 Procedural VT 依赖 GPU 计算能力。两者可以组合使用：宏观地形用 SVT，近处细节用 PVT 实时混合。

---

## 二、Unity 的 Virtual Texturing

### 功能现状

Unity 从 2020.2 版本引入 **Streaming Virtual Texturing（SVT）**，目前（Unity 6 时代）仍标注为实验性（Experimental）功能。

核心能力：
- 支持超大虚拟纹理（理论上限取决于 Page Table 分辨率，通常支持 16K×16K 及以上）
- 物理 Tile Cache 大小可配置，直接控制显存占用上限
- 支持 URP 和 HDRP

### 启用步骤

1. **Project Settings → Player → Other Settings → Virtual Texturing**：勾选启用
2. 重新导入相关纹理，在纹理导入设置中启用 `Virtual Texture` 模式
3. Shader 端：使用 `VTProperty` 宏声明虚拟纹理属性，通过 `SampleVirtualTexture` 内置函数采样

**HDRP 材质示例（StackLit Shader）：**

```hlsl
// 声明 VT 属性
[VirtualTexture] _AlbedoVT ("Albedo VT", 2D) = "white" {}

// 采样
VTProperty vtProp = GetVTProperty(_AlbedoVT);
float4 albedo = SampleVirtualTexture(vtProp, uv);
```

### 限制与注意事项

| 限制 | 说明 |
|------|------|
| 平台支持 | 仅 PC（Windows/macOS/Linux）和主机；移动端不支持 |
| 动态纹理 | 不适合频繁变化的动态纹理（Page Table 更新开销高） |
| Shader 复杂度 | 需要额外的 Page Table 查询 Pass，增加 Shader 开发成本 |
| 调试工具 | Unity 提供 VT Debug View（帧调试器中查看 Tile 加载状态） |
| 功能稳定性 | 实验性阶段，API 可能随版本变动 |

**适用建议：** 地形系统、大型静态建筑群是最适合 SVT 的场景。角色贴图、特效纹理不建议使用 VT，传统 Mip Streaming 已够用。

---

## 三、UDIM（U-Dimension）工作流

### 什么是 UDIM

UDIM 并不是一种"优化技术"，而是一种**美术工作流规范**，用于管理高分辨率角色/道具的多张纹理贴图。

传统 UV 展开要求所有 UV 坐标在 [0,1] 范围内，意味着一个复杂角色（身体、头部、手部、装备）要么共用一张大纹理（密度不均），要么用多张贴图（材质数量爆炸）。

UDIM 的解法：**允许 UV 坐标超出 [0,1]，每个 UV 格对应一张独立贴图**。

### UDIM 坐标系与命名规范

UDIM 坐标从 1001 开始编号，按行列排列：

```
1011  1012  1013  1014 ...  (V = 1~2，第二行)
1001  1002  1003  1004 ...  (V = 0~1，第一行)
```

计算公式：`UDIM = 1001 + U_index + V_index × 10`

其中 U_index 为 U 方向格子号（0-9），V_index 为 V 方向格子号（从 0 开始）。

**文件命名规范：**

```
character_body_albedo.1001.png   ← UV (0~1, 0~1) 范围的贴图
character_body_albedo.1002.png   ← UV (1~2, 0~1) 范围的贴图
character_body_albedo.1011.png   ← UV (0~1, 1~2) 范围的贴图
```

### 为什么美术喜欢 UDIM

| 传统多材质方案 | UDIM 方案 |
|---------------|-----------|
| 每个部位独立材质，Draw Call 多 | 单材质，GPU 批次友好 |
| 各部位贴图分辨率独立，难以统一缩放 | 所有 UDIM Tile 统一分辨率，缩放一致 |
| 在 DCC 工具中跨材质预览困难 | DCC 工具原生支持，实时预览完整角色 |
| 交付给引擎时需要手动管理多张贴图关联 | 单一逻辑贴图，路径管理简洁 |

一个典型影视级角色可能使用 10 张 4K UDIM Tile，等效于一张 ~12K 分辨率的贴图，但工作流保持可管理。

### Unity 的 UDIM 支持

Unity 2022+ 引入对 UDIM 的编辑器支持：

- 纹理导入器支持识别 UDIM 命名规范，自动将多张 Tile 关联为一个 `Texture2DArray` 或 UDIM Texture 资产
- HDRP 的 Lit 材质和 StackLit 材质支持 UDIM 贴图输入
- Shader Graph 中提供 **UDIM 节点**，可根据 UV 坐标自动选择对应 Tile

**DCC 工具支持一览：**

| 工具 | UDIM 支持情况 |
|------|---------------|
| Substance Painter | 原生支持，UDIM 工作流完善 |
| Mari（Foundry） | UDIM 的发源地，支持最为深度 |
| ZBrush | 支持 UDIM 展UV 和贴图烘焙 |
| Maya / Blender | 支持 UDIM UV 显示和导出 |

---

## 四、Unreal Engine 的 Virtual Heightfield Mesh（VHFM）

### 背景：传统地形 LOD 的局限

传统地形系统（包括 Unity 的 Terrain）预生成多套 LOD Mesh：

- LOD 0（近处）：高密度三角形网格
- LOD 1、2、3...：逐级减少顶点，近处细节丰富，远处粗糙

这种方案在大型开放世界中暴露出明显局限：LOD 切换产生 Popping（闪烁），地形几何精度受限于预制 LOD 层级数，无法按摄像机距离真正连续变化。

### Virtual Heightfield Mesh 的工作方式

VHFM 是 Unreal Engine 中 Virtual Texturing 与几何生成系统的深度结合：

1. 地形高度图存储为虚拟纹理（Streaming VT）
2. 运行时，根据摄像机距离和视锥体，确定每个屏幕区域所需的几何精度
3. GPU 按需从高度图 VT 中读取对应 Mip 的高度数据，**实时生成 Mesh**
4. 三角形密度随摄像机距离连续变化，无离散 LOD 级别

结合 UE5 的 **Nanite**（虚拟几何系统）：

- Nanite 使用可见性缓冲（Visibility Buffer）和软件光栅化，将微三角形的绘制开销摊平
- 理论上支持 10 亿量级的三角形实时渲染，不受传统 Draw Call 数量限制
- VHFM + Nanite 的组合使"无缝开放世界无 LOD 跳变"成为工程上可实现的目标

### VHFM 与传统 Terrain 对比

| 对比维度 | 传统地形（Unity Terrain / UE Legacy） | VHFM + Nanite（UE5） |
|----------|--------------------------------------|----------------------|
| LOD 策略 | 预生成离散 LOD | 运行时按需生成，连续密度 |
| 几何精度 | 受限于最高 LOD 层级 | 理论上受限于高度图分辨率 |
| LOD Popping | 存在（可通过 Fading 缓解） | 无离散跳变 |
| CPU/GPU 负担 | CPU 管理 LOD 切换 | GPU 密集（Compute + 光栅化） |
| 工具链成熟度 | 极成熟 | UE5 较成熟，Unity 无等价方案 |

### Unity 的现状

Unity 目前没有与 Nanite + VHFM 直接等价的官方方案：

- Unity Terrain 系统仍采用传统 LOD Mesh 方式
- GPU Terrain 方案（如 Zibra AI 的 GPU Landscape、社区开源项目）存在，但不是官方维护
- Unity 路线图中提到 GPU-Driven Rendering 和 Cluster-based Rendering 的研究，但截至 Unity 6 尚未落地为 Terrain 的实质性升级

---

## 五、选型指南

根据项目规模和目标平台，以下是贴图系统的推荐选型：

| 项目规模 | 平台 | 推荐方案 |
|----------|------|----------|
| 小型 / 中型项目 | 全平台 | 传统 Texture + Mip Streaming，简单可靠，无额外复杂度 |
| 大型开放世界 | Unity（PC/主机） | Streaming Virtual Texturing（实验性）+ Addressables 流式加载 |
| 影视级角色 | Unity / Unreal | UDIM 工作流 + HDRP 或 UE5 材质系统 |
| 大型开放世界 | Unreal Engine 5 | Nanite + Streaming VT + Virtual Heightfield Mesh |
| 移动端大世界 | Unity | Mip Streaming + 贴图压缩（ASTC）+ 手动 LOD 管理，VT 暂不可用 |

---

## 小结

Virtual Texturing 是解决"显存容量 vs 场景规模"矛盾的核心工具，但它引入了额外的复杂度：Page Table 管理、Shader 采样路径变更、调试工具依赖。对于大多数中小型项目，传统 Mip Streaming 已经足够；VT 的真正价值在地形和大型静态场景中最为突出。

UDIM 则是一个独立的美术协作规范，与 VT 正交，主要服务于影视级角色管线。Unity 2022+ 对其支持已相对完整。

Unreal 的 VHFM 代表了几何与纹理系统深度融合的方向，Unity 在这个维度目前仍在追赶阶段——这是选择引擎时值得纳入考量的技术差距之一。
