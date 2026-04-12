---
title: "Unreal 性能 03｜GPU 优化：DrawCall、Culling、LOD 与 Nanite / Lumen 代价"
slug: "ue-perf-03-gpu-optimization"
date: "2026-03-28"
description: "GPU 是 Unreal 项目最常见的瓶颈端。本篇从 DrawCall 削减、Visibility Culling 机制、LOD 配置，到 Nanite 的适用边界和 Lumen 的实际开销，建立完整的 GPU 优化判断体系。"
tags:
  - "Unreal"
  - "性能优化"
  - "GPU"
  - "Nanite"
  - "Lumen"
series: "Unreal Engine 架构与系统"
weight: 6250
---

`stat unit` 里 GPU 时间超预算，是 Unreal 项目最常见的 GPU 问题。本篇从 DrawCall 开始，一直到 Nanite 和 Lumen 的代价分析，建立系统化的 GPU 优化路径。

---

## GPU 瓶颈的快速定位

```bash
# 第一步：确认 GPU 是瓶颈
stat unit
# GPU > Frame 说明 GPU 是瓶颈

# 第二步：找到最贵的 Pass
stat gpu
# 输出示例：
#   BasePass:         8.4ms  ← 通常最贵
#   ShadowDepths:     3.8ms
#   PostProcessing:   3.4ms
#   Translucency:     1.9ms

# 第三步：确认 DrawCall 数量
stat scenerendering
# Mesh draw calls: 2400  ← 超过 1000 需要关注
```

**缩小范围的快捷方式**：
```bash
# 关闭某个系统，看 GPU 时间变化
r.Shadow.Enable 0         # 关闭阴影（如果下降很多 → 阴影是瓶颈）
r.PostProcessing.Enable 0  # 关闭后处理
r.DepthOfFieldQuality 0   # 关闭 DOF

# 降分辨率（如果 GPU 时间成比例降低 → 填充率瓶颈）
r.ScreenPercentage 50     # 降到 50% 分辨率
```

---

## DrawCall 与批处理

### DrawCall 的构成

Unreal 的 DrawCall 类型：

| 类型 | 说明 | 优化方向 |
|------|------|---------|
| Static Mesh | 场景静态物体 | 合并、Instancing |
| Skeletal Mesh | 角色 / 动态网格 | 减少数量 |
| Dynamic Mesh | 程序化生成 | 限制数量 |
| Shadows | 每个光源的 Shadow Pass | 减少动态光源 |

### ISM 与 HISM

大量相同 Mesh（树木、石头、建筑构件）用 Instanced Static Mesh：

```cpp
// HISM：带 Culling 的 Instanced Static Mesh（推荐）
UHierarchicalInstancedStaticMeshComponent* HISM =
    NewObject<UHierarchicalInstancedStaticMeshComponent>(this);
HISM->SetStaticMesh(TreeMesh);

// 批量添加实例
for (const FVector& Location : TreeLocations)
{
    FTransform Transform;
    Transform.SetLocation(Location);
    Transform.SetScale3D(FVector(FMath::RandRange(0.8f, 1.2f)));
    HISM->AddInstance(Transform);
}
// 1000 棵树 → 1 个 DrawCall（而不是 1000 个）
```

**HISM vs ISM**：
- ISM：简单 Instancing，所有实例同一 DrawCall
- HISM：在 ISM 基础上加入 LOD 和 Culling（远处实例自动剔除），大场景必用

### Merge Actor 工具

```
Editor → Window → Developer Tools → Merge Actors
将多个静态网格合并成一个，减少 DrawCall

适用：场景装饰物（不需要单独交互的物体）
不适用：需要独立 LOD 的物体、需要单独材质切换的物体
```

### 材质数量对 DrawCall 的影响

```
一个 Mesh 有几个 Materials Section，就有几个 DrawCall（即使是同一个 Mesh）。

// 检查材质数量（在编辑器中）
// Mesh Details → Material Slots

// 优化：合并材质，用纹理 Atlas 代替多材质
// 代价：UV 空间利用率降低，纹理精度可能下降
```

---

## Visibility Culling 机制

### Culling 的层级

```
Unreal 的 Culling 从粗到细：

1. Distance Culling（距离剔除）
   → 超过 Cull Distance 的 Actor 不提交渲染
   → 配置：CullDistanceVolume 或 Actor 的 OverrideCullDistance

2. Frustum Culling（视锥剔除）
   → 视角外的 Actor 不渲染
   → 引擎自动处理

3. Precomputed Visibility（预计算可见性）
   → 编辑器预计算每个 Cell 的可见 Primitive 集合
   → 运行时直接查表，不做 GPU 查询
   → 适合静态室内场景

4. Hardware Occlusion Query（硬件遮挡查询）
   → 用 GPU 判断 Primitive 是否被遮挡
   → 有 1-2 帧延迟（异步查询）
   → 适合大型开放世界
```

### CullDistanceVolume 配置

```cpp
// 在 Editor 中放置 CullDistanceVolume
// 配置不同尺寸的 Mesh 在不同距离消失

// 常见配置：
// 小物体（椅子、石头）：Cull Distance = 2000cm
// 中物体（树木、建筑构件）：Cull Distance = 10000cm
// 大物体（建筑）：不剔除

// 在 Actor 上单独配置
// Details → Rendering → Override Min Draw Distance
// Details → Rendering → Override Max Draw Distance
```

### 验证 Culling 效果

```bash
# 查看当前可见 Primitive 数量
stat scenerendering
# Visible static mesh elements: 3891

# 可视化 Occlusion Culling
r.HZBOcclusion 1          # 开启 HZB Occlusion
ShowFlag.VisualizeBuffer HZB  # 可视化 HZB

# 禁用 Occlusion Culling（对比测试）
r.AllowOcclusionQueries 0
```

---

## LOD 系统

### Static Mesh LOD

```
LOD 0（最高细节）：2000 三角面，距离 < 500cm
LOD 1（中等）：  800 三角面，距离 500-2000cm
LOD 2（低细节）： 200 三角面，距离 2000-8000cm
LOD 3（极低）：   50 三角面，距离 > 8000cm

配置依据：占屏幕面积（Screen Size）而非绝对距离
  LOD 0 → 1.0（占满屏幕）到 0.1（占 10%）
  LOD 1 → 0.1 到 0.02
  LOD 2 → 0.02 到 0.005
  LOD 3 → 0.005 到 0
```

### HLOD（Hierarchical LOD）

适合开放世界：远景的整个建筑群合并成一个低面数网格：

```
World Settings → LOD System → Enable HLOD
HLOD Level 1: 合并半径 = 1000cm，在 Cull Distance > 5000cm 时替换
HLOD Level 2: 合并半径 = 5000cm，在 Cull Distance > 20000cm 时替换
```

构建 HLOD：`Build → Build LODs → Build Hierarchical LODs`

---

## Nanite 深度分析

### Nanite 适用场景

```
✅ 适合用 Nanite：
  - 复杂不透明静态网格（建筑、地形细节、岩石）
  - 高面数资产（扫描资产、摄影测量）
  - 场景中大量重复实例的复杂网格

❌ 不适用 Nanite：
  - 需要 Vertex Shader 偏移的网格（草、布料、顶点动画）
  - 透明 / 半透明材质
  - Skeletal Mesh（角色）
  - 双面材质（特定情况）
  - 材质中使用 World Position Offset 的网格
```

### Nanite 的 GPU 代价变化

```
传统渲染（无 Nanite）：
  GPU 时间 ∝ DrawCall × Shader 复杂度 × 顶点数

Nanite 渲染：
  GPU 时间 ≈ 可见像素数 × Shader 复杂度
  （DrawCall 和顶点数几乎无关紧要）

实测（UE5，室外场景，1000 棵树，每棵 50K 面）：
  无 Nanite：GPU = 28ms（DrawCall 是瓶颈）
  有 Nanite：GPU = 11ms（像素着色是瓶颈）

注意：Nanite 有固定开销（Rasterize / Material Pass），
在低面数资产上可能比传统渲染慢。
```

### Nanite 相关 CVar

```bash
r.Nanite.MaxPixelsPerEdge 1.0    # 每条边占的像素数（越小质量越高，越慢）
r.Nanite.MaxPixelsPerEdge 4.0    # 降低质量换性能（粗糙 LOD）

stat nanite                      # Nanite 统计（Rasterized Primitives / Visible Clusters）
r.Nanite.Visualize clusters      # 可视化 Nanite Cluster（调试用）
```

---

## Lumen 代价分析

### Lumen 的两种模式

| 模式 | GPU 代价 | 质量 | 适用平台 |
|------|---------|------|---------|
| Software Ray Tracing | 中等（2-4ms） | 中（依赖 Distance Field） | 所有平台 |
| Hardware Ray Tracing | 高（4-8ms） | 高（物理准确） | RTX / RX 6000 系列 |

**移动端 Lumen**：不支持，必须关闭。

### 关闭 Lumen 的替代方案

```bash
# 关闭 Lumen 全局光照
r.Lumen.Enable 0

# 改用 Static Lighting（烘焙光照）
# → 性能最好，但不支持动态 GI

# 或使用 Sky Light + Reflection Capture（近似 GI）
# → 有限动态性，GPU 代价极低
```

### Lumen 调优

```bash
# 降低 Lumen 质量（节省 GPU）
r.Lumen.Reflections.Allow 0          # 关闭 Lumen 反射（改用 SSR 或 Reflection Capture）
r.Lumen.GlobalIllumination.Allow 1   # 保留 GI
r.Lumen.Quality 0                    # 0=低质量，1=中，2=高
r.Lumen.Scene.ViewDistance 10000     # 限制 Lumen 更新距离

# 实测：高质量 Lumen（1080p）
#   Software RT：约 4-6ms
#   Hardware RT：约 6-10ms
#   关闭后节省：相当于预算的 25-40%
```

---

## Shadow 优化

```bash
# Cascade Shadow Map 配置（最常见的 Shadow 开销）
r.Shadow.CSM.MaxCascades 2          # 移动端降到 1-2 级
r.Shadow.MaxResolution 1024         # 阴影图分辨率（默认 2048）
r.Shadow.DistanceScale 0.5          # 缩小阴影距离

# 关闭点光源动态阴影（代价极高）
r.Shadow.EnablePointLightShadows 0

# Virtual Shadow Maps（UE5，更适合 Nanite 场景）
r.Shadow.Virtual.Enable 1           # 更好的阴影质量，代价与 CSM 相当
r.Shadow.Virtual.MaxPhysicalPages 2048  # 限制内存用量
```

---

## 后处理优化

```bash
# 关闭开销最高的后处理
r.DepthOfFieldQuality 0             # DOF：每帧 1-3ms
r.BloomQuality 3                    # Bloom 降质（默认 5）：节省 0.5-1ms
r.MotionBlurQuality 0               # Motion Blur：每帧 1-2ms

# TAA vs MSAA 的选择
r.AntiAliasingMethod 1              # 1=FXAA（最快）
r.AntiAliasingMethod 2              # 2=TAA（中等）
r.AntiAliasingMethod 4              # 4=MSAA（质量好，移动端友好）
```
