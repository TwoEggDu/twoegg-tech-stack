+++
title = "Unity 渲染系统补A｜Mesh 与 Texture 存储基础：顶点格式、纹理压缩、Mip"
slug = "unity-rendering-supp-a-mesh-texture-storage"
date = 2026-03-26
description = "Mesh 在显存里是什么格式？Texture 为什么要压缩，压缩成什么？Mip 链怎么生成、怎么影响采样？这篇把这些存储层细节讲清楚——它们直接决定内存占用、带宽消耗和最终画质。"
weight = 1500
[taxonomies]
tags = ["Unity", "Rendering", "Mesh", "Texture", "内存", "纹理压缩", "Mip"]
[extra]
series = "Unity 渲染系统"
+++

渲染管线讲的是"数据怎么流动"，这篇讲的是"数据本身长什么样"。Mesh 的顶点格式选择影响顶点缓冲大小；Texture 的压缩格式影响显存占用和采样带宽；Mip 链的生成方式影响远处贴图的画质和性能。这些存储层细节，是性能优化和 Profile 分析的基础。

---

## Mesh 存储：顶点格式

### 顶点数据的内存布局

Mesh 的顶点数据在 GPU 里以**顶点缓冲（Vertex Buffer）**的形式存储。每个顶点是一段连续内存，包含若干属性。典型顶点格式：

```
Position  (float3,  12 bytes)
Normal    (float3,  12 bytes)
Tangent   (float4,  16 bytes)   ← w 存手性（±1）
UV0       (float2,   8 bytes)
UV1       (float2,   8 bytes)   ← Lightmap UV
Color     (byte4,    4 bytes)
─────────────────────────────
合计：                60 bytes / 顶点
```

10 万顶点的角色：60 × 100,000 = **6 MB 顶点缓冲**。

### 精度压缩降低显存

不是所有属性都需要 float32 精度：

| 属性 | 默认格式 | 可压缩格式 | 节省 |
|------|---------|----------|------|
| Normal | float3 (12B) | SNorm16×3 (6B) 或 Byte4 (4B) | 50%~67% |
| Tangent | float4 (16B) | SNorm16×4 (8B) | 50% |
| UV | float2 (8B) | Half2 (4B) | 50% |
| Color | float4 (16B) | Byte4 (4B) | 75% |

Unity 的 **Mesh Compression** 设置（Mesh Import Settings → Mesh Compression）可以自动压缩法线/UV 精度，但会牺牲少量精度，皮肤动画角色需谨慎。

### 顶点格式对 SRP Batcher 的影响

SRP Batcher 要求同一 Shader 的 CBUFFER 布局一致，但顶点格式本身只影响能否使用 GPU Instancing。不一致的顶点格式会拆分 DrawCall 合批。

### 索引缓冲精度

索引缓冲默认 UInt16（2 bytes/索引），支持最多 65535 个顶点。超过时自动升级到 UInt32（4 bytes/索引）。移动端检查模型顶点数，避免不必要的 UInt32 索引缓冲。

---

## Texture 存储：压缩格式

### 为什么要压缩

一张 1024×1024 的 RGBA 非压缩贴图：1024 × 1024 × 4 = **4 MB**。移动游戏有几百张贴图，全部非压缩会耗尽显存。压缩贴图直接在 GPU 里解码采样，不需要先解压到内存——这是硬件压缩和普通文件压缩的关键区别。

### 主流压缩格式

**PC/主机平台：**

| 格式 | 压缩比 | 特点 |
|------|--------|------|
| DXT1 / BC1 | 6:1 | RGB，无 Alpha 或 1-bit Alpha，PC 必备 |
| DXT5 / BC3 | 4:1 | RGBA，Alpha 质量较好 |
| BC4 | 8:1 | 单通道（R），适合遮罩/法线 X 分量 |
| BC5 | 4:1 | 双通道（RG），法线贴图（只存 XY，Z 重建）|
| BC6H | 6:1 | HDR RGB，浮点范围，适合 Cubemap/IBL |
| BC7 | 4:1 | 高质量 RGBA，编码慢但画质最好 |

**Android（OpenGL ES/Vulkan）：**

| 格式 | 压缩比 | 支持情况 |
|------|--------|---------|
| ETC2 RGB | 6:1 | Android 4.3+（OpenGL ES 3.0+）必备 |
| ETC2 RGBA | 4:1 | 同上，带 Alpha |
| ASTC | 可变（4:1~25:1）| Android 5.0+（OpenGL ES 3.1+），质量最佳，推荐 |

**iOS（Metal）：**

| 格式 | 说明 |
|------|------|
| ASTC | Apple A8（iPhone 6）后支持，iOS 推荐格式 |
| PVRTC | 旧格式，iOS A7 及以前，现已基本弃用 |

### ASTC 块大小选择

ASTC 的压缩比由**块大小**决定：

| 块大小 | 压缩比（RGBA） | 适用场景 |
|--------|-------------|---------|
| 4×4 | 8 bpp → 8:1 | 最高质量（法线贴图、角色皮肤）|
| 6×6 | 3.56 bpp → ~18:1 | 通用 UI、环境贴图 |
| 8×8 | 2 bpp → 32:1 | 低质量，远景/遮罩 |
| 10×10 | 1.28 bpp → 50:1 | 极低质量 |

Unity 的 ASTC Quality 设置（ASTC 4x4/6x6/8x8）对应以上块大小。

### 什么贴图不适合压缩

- **Render Texture / UAV**：运行时写入，无法预压缩
- **精密数据贴图**（Heightmap、精确 Mask）：块压缩会引入色块误差
- **梯度贴图**（Sky Gradient、Ramp）：色带明显，需 BC7/ASTC 4×4 高质量

---

## Mip 链：为什么需要、怎么生成

### 走样问题（Aliasing）

一张 1024×1024 贴图渲染到 4×4 像素的远处地面时，每个像素需要"代表" 1024/4=256 个贴图像素的颜色。直接采样某一个 texel 会导致画面闪烁和走样。

### Mip 链的解法

预生成一系列缩小版本（每级宽高减半），渲染时根据像素覆盖的贴图面积自动选择合适的 Mip 层级：

```
Mip 0: 1024×1024  (原始)
Mip 1:  512×512
Mip 2:  256×256
Mip 3:  128×128
...
Mip 10:    1×1

总内存 = 原始大小 × 4/3 ≈ 原始的 1.33 倍
```

### Mip 选择：LOD 计算

GPU 根据**屏幕空间导数**（`ddx` / `ddy`）计算 UV 变化率，推算出合适的 Mip 级别：

- UV 变化慢（物体近）→ 选低 Mip 编号（高清）
- UV 变化快（物体远）→ 选高 Mip 编号（模糊）

### 各向异性过滤（Anisotropic Filtering，AF）

标准 Mip 对倾斜表面（地面）效果差——地面的 UV 在水平方向拉伸大、垂直方向小，选同一 Mip 会导致一个方向模糊。AF 在各向异性方向多采样几个 Mip 来修正。AF×4 / ×8 / ×16 代表采样次数，AF×8 是性能和质量的常见均衡点。

### Unity 的 Mip 相关设置

| 设置 | 位置 | 说明 |
|------|------|------|
| Generate Mip Maps | Texture Import → Advanced | 是否生成 Mip 链 |
| Mip Map Filter | 同上 | Box（快）/ Kaiser（抗锯齿更好）|
| Streaming Mipmaps | 同上 | 按需加载 Mip，降低内存峰值（需开启 Texture Streaming）|
| Aniso Level | 同上 | 1（关）~ 16（最高质量），通常用 4~8 |
| Global Mip Bias | URP Asset → Quality | 全局偏移 Mip 级别（负值 = 更清晰，正值 = 更模糊省内存）|

### Mip Streaming

Unity 的 **Texture Streaming**（Project Settings → Quality → Streaming Mipmaps）允许只加载当前摄像机需要的 Mip 层级，其余层级留在磁盘/AssetBundle 中。大型开放世界场景可以显著降低纹理内存峰值。

---

## 内存占用速查表

| 贴图尺寸 | 格式 | 带 Mip | 内存 |
|---------|------|--------|------|
| 1024×1024 | RGBA32（未压缩）| 是 | ~5.3 MB |
| 1024×1024 | DXT5 / BC3 | 是 | ~1.4 MB |
| 1024×1024 | ASTC 6×6 | 是 | ~0.6 MB |
| 2048×2048 | ASTC 6×6 | 是 | ~2.4 MB |
| 4096×4096 | ASTC 6×6 | 是 | ~9.5 MB |

---

## 小结

- **顶点格式**：按需选精度，Normal/UV 可用 Half 压缩节省带宽
- **纹理压缩**：PC 用 BC7/DXT，Android 用 ASTC，不能用 float32 非压缩
- **Mip 链**：几乎所有贴图都应开启，额外 1/3 内存换来无走样和更好带宽利用
- **各向异性**：地面/墙面等斜视表面至少开 AF×4
- **Mip Streaming**：大世界必备，降低纹理内存峰值
