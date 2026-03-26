+++
title = "Unity 渲染系统补C｜LOD 与 Culling 系统：Frustum、Occlusion、HZB"
slug = "unity-rendering-supp-c-lod-culling"
date = 2026-03-26
description = "LOD 减少远处物体的面数，Culling 直接跳过不可见物体。这两个系统是 CPU 端渲染性能的核心。讲清 LOD Group 工作原理、Frustum Culling 的实现、Occlusion Culling 的烘焙与运行时机制，以及 HZB（Hierarchical Z-Buffer）的 GPU 端剔除思路。"
weight = 1520
[taxonomies]
tags = ["Unity", "Rendering", "LOD", "Culling", "性能优化", "HZB"]
[extra]
series = "Unity 渲染系统"
+++

渲染性能的第一道防线不是在 GPU 上优化 Shader，而是在 CPU 上决定哪些物体根本不需要提交。LOD（Level of Detail）和 Culling 两个系统协同工作：LOD 降低远处物体的几何复杂度，Culling 直接把不可见物体从渲染列表中剔除。一个场景里哪怕有数千个物体，真正每帧需要 GPU 处理的往往只有其中的一小部分。

---

## LOD Group：根据屏幕面积切换细节层级

### LODGroup 组件的工作原理

`LODGroup` 组件挂在一个父对象上，子对象分别承载不同细节程度的 Mesh。每个 LOD 级别有一个**屏幕空间面积阈值**（Screen Relative Height），单位是相对于屏幕高度的比例：

| LOD 级别 | 屏幕相对高度阈值 | 典型面数 |
|---------|------------|--------|
| LOD 0   | > 0.6      | 高模，10000+ 三角形 |
| LOD 1   | 0.3 ~ 0.6  | 中模，3000 三角形   |
| LOD 2   | 0.1 ~ 0.3  | 低模，500 三角形    |
| Culled  | < 0.1      | 不渲染              |

Unity 每帧计算物体 AABB 包围盒在屏幕上投影的高度占比，与各级阈值对比后决定激活哪个子 Mesh Renderer。这个计算在 CPU 上完成，没有 GPU 参与。

### Cross-fade：避免 LOD 切换跳变

LOD 切换时直接替换 Mesh 会产生明显的"闪烁"。`Fade Mode` 设置为 `Cross Fade` 后，Unity 会在切换时同时渲染两个级别，用 `unity_LODFade.x` 变量在 Shader 里做 dither 混合：

```hlsl
// URP Lit Shader 内部逻辑（简化）
#ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition(IN.positionCS.xy, unity_LODFade.x);
#endif
```

`Fade Transition Width` 控制过渡的屏幕高度区间宽度，建议设置 0.1~0.2，过大会导致较长时间同时渲染两个级别，反而更贵。

### LOD Bias：全局缩放阈值

`QualitySettings.lodBias`（默认值 1.0）是一个全局乘数，影响所有 LODGroup 的切换距离：

- `lodBias = 2.0`：切换距离翻倍，远处用更高精度 LOD（画质更好，性能更差）
- `lodBias = 0.5`：切换距离减半，更激进地使用低 LOD（性能更好，画质下降）

移动端可在低画质档设置 `lodBias = 0.5 ~ 0.7`，是最简单的性能调节旋钮之一。

---

## Frustum Culling：视锥体剔除

### 视锥体的数学表示

摄像机的可见区域是一个由 6 个平面围成的视锥体（Frustum）：Near、Far、Left、Right、Top、Bottom。每个平面用法向量 + 偏移量表示（平面方程 `Ax + By + Cz + D = 0`）。

### AABB vs 视锥体测试

Unity 用物体的**轴对齐包围盒（AABB）** 做快速剔除测试，而不是逐顶点测试：

1. 对视锥体的 6 个平面依次测试
2. 对每个平面，计算 AABB 8 个顶点中距离平面最远的"正极点"（p-vertex）
3. 若正极点在平面的负侧（外侧），则 AABB 完全在该平面外部，物体**完全不可见**，直接剔除
4. 6 个平面全部通过则判定可见

这个算法的代价是 O(6) 次平面测试，非常廉价。Unity 在主线程的 Culling 阶段自动对所有 Renderer 执行此测试，开发者无需手动干预。

### Layer Culling Mask

`Camera.cullingMask` 是一个 32 位 bitmask，可以让摄像机完全跳过特定 Layer 的物体，连 AABB 测试都不做：

```csharp
// 让摄像机不渲染第 8 层
camera.cullingMask &= ~(1 << 8);

// 也可以按层名操作
int layer = LayerMask.NameToLayer("UI3D");
camera.cullingMask &= ~(1 << layer);
```

多摄像机方案中（主摄像机 + UI 摄像机 + 小地图摄像机），合理设置每个摄像机的 `cullingMask` 是避免重复渲染的基础。

---

## Occlusion Culling：遮挡剔除

### 核心思路

Frustum Culling 只判断物体是否在视锥体内，无法判断"在视锥体内但被遮挡"的情况。Occlusion Culling 解决这个问题：如果物体 A 被物体 B 完全遮挡，物体 A 不需要渲染。

Unity 的 Occlusion Culling 基于 **Umbra** 中间件，采用**离线烘焙 + 运行时查询**的架构。

### 烘焙流程：构建 PVS

PVS（Potentially Visible Set，潜在可视集合）是核心数据结构。烘焙步骤：

1. **标记 Occluder（遮挡体）**：Inspector 中勾选 `Occluder Static`，通常是大型不透明建筑墙体
2. **标记 Occludee（被遮挡体）**：勾选 `Occludee Static`，所有可能被遮挡的物体
3. **设置参数**：
   - `Smallest Occluder`：最小遮挡体尺寸（越小烘焙越精细，数据量越大）
   - `Smallest Hole`：最小穿透孔洞尺寸（控制光线穿透的最小间隙）
4. **执行烘焙**：Window → Rendering → Occlusion Culling → Bake

烘焙结果存储为 `.asset` 文件，包含场景空间的 Cell 划分和每个 Cell 的 PVS 表。

### Portal/Cell 运行时机制

运行时，Umbra 把空间划分为若干 **Cell**，相邻 Cell 之间通过 **Portal**（门口/窗口）相连。每帧 CPU 查询：

1. 确定摄像机所在 Cell
2. 从当前 Cell 出发，沿 Portal 扩展可达 Cell 的集合
3. 只有可达 Cell 内且通过视锥体测试的物体才提交渲染

这个查询在 CPU 上完成，典型开销为 0.1~0.5ms（取决于场景复杂度和 Cell 数量）。

### 静态物体 vs 动态物体

| 属性 | 静态物体 | 动态物体 |
|-----|---------|---------|
| 可作为 Occluder | 是 | 否 |
| 可被遮挡剔除 | 是（烘焙时纳入 PVS） | 是（运行时动态查询） |
| 要求 | 必须勾选 Occludee Static | 无需勾选，但精度较低 |

动态物体（如角色、载具）只能作为 Occludee，不能作为 Occluder。如果场景中大量动态物体互相遮挡，Occlusion Culling 无法帮助。

### 代价与适用场景

**烘焙代价：**
- 大型室内场景烘焙时间可达数小时
- `.asset` 数据文件通常 1~50MB

**运行时代价：**
- CPU 查询 0.1~0.5ms/帧
- 内存占用（烘焙数据常驻）

**适用场景：** 室内场景、城市街道、走廊类关卡。开阔地形、无大型遮挡体的场景收益极小，烘焙纯属浪费。

---

## HZB：GPU 端层级深度缓冲剔除

### 原理

HZB（Hierarchical Z-Buffer，层级深度缓冲）是一种 GPU 端的遮挡剔除方法，不依赖离线烘焙：

1. **构建 Mip 链**：将上一帧的深度缓冲（Depth Buffer）生成一系列降采样 Mip 层级。每个 Mip 层级存储对应区域的**最大深度值**（最近的遮挡物）：

```
Mip 0: 原始深度图（1920×1080）
Mip 1: 960×540，每像素存 2×2 区域的最大深度
Mip 2: 480×270，每像素存 4×4 区域的最大深度
...
```

2. **GPU 端查询**：对每个待渲染物体的 AABB，在 HZB 中选取合适的 Mip 层级进行深度比较。若 AABB 的最近深度（最小 z 值）大于 HZB 对应区域的最大深度，说明物体完全被遮挡，可以跳过。

3. **Compute Shader 执行**：整个剔除过程在 GPU 上运行，避免了 CPU-GPU 回读（readback）的瓶颈。

### Unity 中的支持情况

| 管线 | HZB / GPU Occlusion Culling | 说明 |
|-----|---------------------------|-----|
| Built-in | 不支持 | — |
| URP | 部分支持（Unity 6+） | GPU Occlusion（Depth Priming 相关） |
| HDRP | 支持 | `GPU Resident Drawer` + `GPU Occlusion Culling`，Unity 2023.1+ |

HDRP 的 GPU Occlusion Culling 在 `Camera` 设置中启用，依赖 GPU Resident Drawer 管理物体实例。对大型开放世界场景（数千个网格）效果显著，CPU Occlusion Culling 无法覆盖的动态大场景适用。

**注意：** HZB 使用上一帧深度图，快速移动的摄像机可能出现一帧延迟的误剔除（ghost culling），实际中通常用保守测试（conservative test）来缓解。

---

## SpeedTree 与植被 LOD

SpeedTree 资产天然支持多 LOD 级别，最低 LOD 会替换为 **Billboard**（摄像机朝向的四边形贴片）。Unity 的 LODGroup 对 Billboard 有专门支持：

- `LOD X` 设置为 Billboard 模式后，自动使用 `BillboardRenderer` 渲染
- Billboard 自动旋转朝向摄像机（Spherical Billboard 或 Cylindrical Billboard）
- 风动效果（Wind Zone）通过 Shader 的顶点偏移实现，在 LOD 切换时需要同步 `unity_LODFade` 保证过渡连续

植被密集场景中，GPU Instancing 与 LODGroup 配合使用是标准方案：同一 LOD 级别的所有植被实例合并为一批 Instanced Draw Call，CPU 端的 LOD 选择结果直接传入 GPU 实例缓冲。

---

## 移动端建议

移动端 CPU 较弱，Occlusion Culling 的运行时查询代价（0.3ms+）可能不划算：

| 策略 | 说明 | 适用场景 |
|-----|-----|---------|
| 激进 LOD Bias | `lodBias = 0.5`，提前切换低 LOD | 通用 |
| 手动分区剔除 | 按地图格子手动设置物体 Active | 大地图游戏 |
| 远距离强制 Cull | LODGroup 最后一级设小 Cull 距离 | 密集物体场景 |
| Occlusion Culling | 仅室内/走廊类场景使用 | 有大型遮挡体时 |
| 放弃 Occlusion Culling | 开阔地形直接不用 | 无收益时节省烘焙和内存 |

GPU 端 HZB 在移动端支持有限（OpenGL ES 不支持 Compute Shader 的部分特性），通常不是移动端的优化方向。

---

## 小结

LOD 和 Culling 是渲染管线中最前置的性能杠杆，优化收益往往大于后续所有 GPU 优化的总和。核心要点：

- **LODGroup** 用屏幕空间面积比例驱动切换，`lodBias` 是最简单的全局调节旋钮
- **Frustum Culling** 是 Unity 自动完成的 O(6) AABB 测试，代价极低
- **Occlusion Culling** 需要烘焙，适合有大型遮挡体的室内场景，开阔场景不要用
- **HZB** 是 GPU 端动态剔除，HDRP（Unity 2023+）已内置支持，对大场景效果显著
- 移动端优先用激进 LOD，谨慎评估 Occlusion Culling 的 CPU 开销
