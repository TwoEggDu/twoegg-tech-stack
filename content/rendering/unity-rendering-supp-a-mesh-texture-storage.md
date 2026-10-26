---
title: "Unity 渲染系统补A｜Mesh 与 Texture 存储基础：顶点格式、纹理压缩、Mip"
slug: "unity-rendering-supp-a-mesh-texture-storage"
date: "2026-03-26"
lastmod: "2026-10-26"
description: "Mesh 在显存里是什么格式？Texture 为什么要压缩，压缩成什么？Mip 链怎么生成、怎么影响采样？这篇把这些存储层细节讲清楚——它们直接决定内存占用、带宽消耗和最终画质。"
weight: 1500
tags:
  - "Unity"
  - "Rendering"
  - "Mesh"
  - "Texture"
  - "内存"
  - "纹理压缩"
  - "Mip"
series: "Unity 渲染系统"
---
> 版本与证据范围：本轮依据 Unity 6（6000.0）公开文档校正格式与存储口径。字节数为注明条件的计算值；未执行导入、GPU capture 或真机性能实验。

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

按上面这组属性计算，10 万顶点是 60 × 100,000 = **6 MB（约 5.72 MiB）顶点数据**。这只是布局示例，不含索引、蒙皮的骨骼索引/权重、对齐和可能的 CPU 副本；不能当作完整角色占用。

### 精度压缩降低显存

不是所有属性都需要 float32 精度：

| 属性 | 比较用格式 | 较低精度示例 | 属性字节变化 |
|------|---------|----------|------|
| Normal | float3 (12B) | Half4 (8B，含填充分量) | 约 33% |
| Tangent | float4 (16B) | Half4 (8B) | 50% |
| UV | float2 (8B) | Half2 (4B) | 50% |
| Color | float4 (16B) | Byte4 (4B) | 75% |

表中只比较属性本身的字节数，具体顶点格式和通道需要目标平台支持，不能直接等同于导入器默认布局。Unity 的 **Vertex Compression** 用于降低符合条件网格的运行时顶点精度；模型导入器的 **Mesh Compression** 主要压缩磁盘上的网格数据，加载时解压，不能按文件缩小比例推算运行时顶点缓冲。两者的条件与影响见 [Unity 网格压缩类型](https://docs.unity3d.com/6000.0/Documentation/Manual/types-of-mesh-data-compression.html)。

### 顶点格式对 SRP Batcher 的影响

顶点布局决定顶点数据如何绑定和读取，但不能仅凭布局就推断某种批处理会成功。SRP Batcher 主要复用材质常量数据，要求 Shader 的常量缓冲声明符合约定；传统 GPU Instancing 则通常要求相同 Mesh、Material 和兼容实例数据。它们与静态/动态批处理的条件不同，见 [GPU Instancing 与 SRP Batcher]({{< relref "rendering/unity-rendering-01b2-gpu-instancing-srp-batcher.md" >}})及 [Unity SRP Batcher 文档](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher.html)。

### 索引缓冲精度

`Mesh.indexFormat` 默认是 UInt16（2 bytes/索引）；较大索引范围可使用 UInt32（4 bytes/索引），还需检查设备支持。模型导入器的 `Index Format = Auto` 可按顶点数量选择格式；脚本创建 Mesh 时不能依赖“超出后自动升级”，应在写入索引前配置所需格式。见 [Mesh.indexFormat](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh-indexFormat.html)与 [ModelImporterIndexFormat.Auto](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporterIndexFormat.Auto.html)。

---

## Texture 存储：压缩格式

### 为什么要压缩

一张 1024×1024 的 RGBA32 非压缩贴图，基础层数据为 1024 × 1024 × 4 = **4 MiB**。PNG/TGA 源文件大小不能代替这个数。目标设备支持相应 GPU 压缩格式时，可以保持块压缩数据并由硬件采样解码；格式不支持时的回退可能增加占用。GPU 压缩与下载包压缩是两层不同的处理。

### 主流压缩格式

**PC/主机平台：**

| 格式 | 位率 | 特点 |
|------|--------|------|
| DXT1 / BC1 | 4 bpp | RGB 或有限的 1-bit Alpha |
| DXT5 / BC3 | 8 bpp | RGBA，独立编码 Alpha |
| BC4 | 4 bpp | 单通道，适合部分遮罩数据 |
| BC5 | 8 bpp | 双通道，可用于法线 XY，Z 在 Shader 中重建 |
| BC6H | 8 bpp | HDR RGB，可用于环境数据 |
| BC7 | 8 bpp | RGBA，实际质量取决于内容和编码设置 |

**Android（OpenGL ES/Vulkan）：**

| 格式 | 位率 | 支持条件 |
|------|--------|---------|
| ETC2 RGB | 4 bpp | 核对目标 API 和设备格式能力 |
| ETC2 RGBA8 | 8 bpp | 完整 Alpha；1-bit Alpha 还有 ETC2 RGBA1 |
| ASTC LDR | 随块大小变化 | 需设备支持；不能仅由 Android 版本、GLES 3.1 或 Vulkan 名称判断 |

**iOS（Metal）：**

| 格式 | 说明 |
|------|------|
| ASTC | 支持该格式的 Apple GPU 可用；核对具体目标与数据类型 |
| PVRTC | 旧项目可能使用；是否保留由实际支持设备决定 |

格式位率与平台条件以 [Unity TextureFormat](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.html)和[平台纹理格式资料](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-compression-formats.html)为准；HDR、LDR 与压缩体纹理的能力需要分别确认。

### ASTC 块大小选择

下面计算二维 ASTC LDR 数据：每块固定 128 bit（16 bytes），`bpp = 128 / (块宽 × 块高)`。压缩比统一相对 **RGBA32 的 32 bpp**，不含 Mip 和分配开销。

| 块大小 | 位率与压缩比（RGBA32） | 质量检查方向 |
|--------|-------------|---------|
| 4×4 | 8 bpp → 4:1 | 对误差敏感的法线、边缘与细节 |
| 6×6 | 约 3.56 bpp → 9:1 | 比较质量与数据量的一个候选 |
| 8×8 | 2 bpp → 16:1 | 检查远景、遮罩和梯度误差 |
| 10×10 | 1.28 bpp → 25:1 | 需更严格检查压缩伪影 |
| 12×12 | 约 0.89 bpp → 36:1 | 仅在目标内容允许误差时采用 |

推导依据见 [ASTC 4×4](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.ASTC_4x4.html)和 [ASTC 8×8](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.ASTC_8x8.html)。块大小属于格式选择；编码质量/编码速度设置是另一项取舍。相同块大小下，增加 Alpha 不会自动让每块 128 bit 翻倍，但可能改变压缩误差。

### 什么贴图不适合压缩

- **Render Texture / UAV**：运行时写入，无法预压缩
- **精密数据贴图**（Heightmap、精确 Mask）：块压缩会引入色块误差
- **梯度贴图**（Sky Gradient、Ramp）：检查色带；较小块、高质量编码或未压缩格式都是待比较方案

---

## Mip 链：为什么需要、怎么生成

### 走样问题（Aliasing）

假设一张 1024×1024 贴图均匀覆盖 4×4 个屏幕像素，每个像素的覆盖在每个方向约为 256 个 texel，面积约为 256×256 个 texel。直接取极少的样本可能导致闪烁和走样；实际采样足迹还取决于投影与 UV 导数。

### Mip 链的解法

预生成一系列缩小版本（每级宽高减半），渲染时根据像素覆盖的贴图面积自动选择合适的 Mip 层级：

```
Mip 0: 1024×1024  (原始)
Mip 1:  512×512
Mip 2:  256×256
Mip 3:  128×128
...
Mip 10:    1×1

完整二维 Mip 链的 texel 数 ≈ 基础层 × 4/3
```

这个比例适用于规则缩小的二维纹理估算。块压缩格式要对每一级的块数向上取整，小尺寸尾部 Mip 仍至少占一块；实际资源分配还可能有对齐和元数据。

### Mip 选择：LOD 计算

GPU 根据**屏幕空间导数**（`ddx` / `ddy`）计算 UV 变化率，推算出合适的 Mip 级别：

- UV 变化慢（物体近）→ 选低 Mip 编号（高清）
- UV 变化快（物体远）→ 选高 Mip 编号（模糊）

### 各向异性过滤（Anisotropic Filtering，AF）

倾斜表面的采样足迹可能在两个方向差异很大，普通各向同性过滤容易损失其中一个方向的细节。AF 沿较长的足迹改善过滤；×4 / ×8 / ×16 表示各向异性等级，不能直接当成每个像素固定的采样指令数。选择等级要比较斜视画质与目标设备成本。

### Unity 的 Mip 相关设置

| 设置 | 位置 | 说明 |
|------|------|------|
| Generate Mip Maps | Texture Import → Advanced | 是否生成 Mip 链 |
| Mip Map Filter | 同上 | Box（快）/ Kaiser（抗锯齿更好）|
| Streaming Mipmaps | 同上 | 按需加载 Mip，降低内存峰值（需开启 Texture Streaming）|
| Aniso Level | Texture Import 与项目质量设置 | 最大各向异性等级，效果和成本需实测 |
| Mip Map Bias | 纹理/API 对应设置 | 偏移采样 LOD；正值倾向较低分辨率，但不自动释放已驻留的高分辨率 Mip |

### Mip Streaming

Unity 的 **Mipmap Streaming** 对启用流送的纹理，结合相机需求、优先级与内存预算选择加载的 Mip。它与只改变采样 Bias 不同，可能减少高分辨率 Mip 驻留，也会引入加载与清晰度变化；要检查实际加载层级和占用，不能仅凭开启选项承诺收益。见 [Unity Mipmap Streaming](https://docs.unity3d.com/6000.0/Documentation/Manual/TextureStreaming.html)。

---

## 内存占用速查表

下表是完整 Mip 链的**理论数据量**，单位 MiB（1 MiB = 1,048,576 bytes），不含驱动分配、CPU 副本或格式回退。ASTC 按各级 `ceil(宽/6) × ceil(高/6) × 16 bytes` 求和。

| 贴图尺寸 | 格式 | 带 Mip | 理论数据量 |
|---------|------|--------|------|
| 1024×1024 | RGBA32（未压缩）| 是 | 约 5.333 MiB |
| 1024×1024 | DXT5 / BC3 | 是 | 约 1.333 MiB |
| 1024×1024 | ASTC 6×6 | 是 | 约 0.597 MiB |
| 2048×2048 | ASTC 6×6 | 是 | 约 2.382 MiB |
| 4096×4096 | ASTC 6×6 | 是 | 约 9.500 MiB |

---

## 小结

- **顶点格式**：区分磁盘 Mesh Compression 与运行顶点布局，按属性与目标能力选精度。
- **纹理压缩**：先确认数据用途和格式支持，再比较块大小、质量与占用。
- **Mip 链**：缩小显示的表面通常需要 Mip 来改善采样；其额外存储不适合一概套到所有 UI 或数据纹理。
- **各向异性**：在斜视表面比较等级，不将等级当固定采样次数。
- **Mip Streaming**：根据实际场景评估驻留收益与加载行为，采样 Bias 本身不等于内存释放。
