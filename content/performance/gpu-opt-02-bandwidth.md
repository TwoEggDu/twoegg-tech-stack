---
title: "GPU 渲染优化 02｜带宽优化：纹理压缩、RT 格式选择与 Resolve 时机"
slug: "gpu-opt-02-bandwidth"
date: "2026-03-25"
lastmod: "2026-10-26"
description: "带宽是移动端 GPU 最核心的瓶颈之一。本篇讲清楚纹理压缩格式的选择逻辑（ASTC/ETC2/PVRTC）、RT 格式对带宽的影响、Resolve 时机的控制，以及如何用工具量化带宽消耗。"
tags:
  - "移动端"
  - "GPU"
  - "带宽优化"
  - "纹理压缩"
  - "ASTC"
  - "ETC2"
  - "RT格式"
  - "性能优化"
series: "移动端硬件与优化"
weight: 2220
---
> 版本与证据范围：设置与格式依据 Unity 6（6000.0）公开资料修订；字节表为理论计算，没有相应设备的带宽或耗时实测。

移动端 GPU 和 CPU 通常共享系统内存，带宽（Memory Bandwidth）是整个 SoC 的共享资源。GPU 的纹理访问、RT 与 Tile 数据写回都可能消耗它。如果 GPU 时间上升而算术单元不繁忙，可以把数据访问列为候选原因；仍需结合缓存、纹理单元和外部访存指标，不能据此单独确认带宽已经打满。

---

## 纹理带宽：压缩格式的选择

纹理是常见的大量读取来源。一张 1024×1024 的 RGBA32 纹理基础层为 4 MiB；若实际 GPU 数据量降到 1/8，则为 512 KiB。采样可能命中缓存，因此这个存储比例不能直接推导外部带宽也下降到 1/8。源 PNG 大小、包压缩率、GPU 驻留量和采样流量需要分开记录。

移动端的纹理压缩格式有三个主流选择：

---

### ASTC（Adaptive Scalable Texture Compression）

**支持条件**：确认目标 GPU、API 和所需 ASTC LDR/HDR 格式能力。仅有 GLES 3.1 或 Vulkan 不能保证全部 ASTC 能力，平台回退也需检查实际产物。见 [Unity 纹理格式资料](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-compression-formats.html)。

**位率**：二维 ASTC 每块为 128 bit，`bpp = 128 / (块宽 × 块高)`。例如 4×4 为 8 bpp，8×8 为 2 bpp；下表统一以 RGBA32 的 32 bpp 作基准，比例不含 Mip 尾部取整和分配开销。

常用 block size 的压缩率对比：

| Block Size | 位率；数据量 / RGBA32 | 候选检查内容 |
|-----------|-----------------|---------|
| 4×4 | 8 bpp；1/4 | 法线、透明边缘和高频细节 |
| 6×6 | 约 3.56 bpp；1/9 | 画质与数据量对照 |
| 8×8 | 2 bpp；1/16 | 遮罩、远景与梯度误差 |
| 12×12 | 约 0.89 bpp；1/36 | 内容能否接受较大块误差 |

**ASTC 的优势**：
- 在支持设备上可以用专用格式存储 LDR 或 HDR 数据，需分别确认能力。
- 从支持的离散块尺寸中选择数据量与画质；并非任意尺寸。
- 支持 GPU 压缩采样时无需先生成完整 RGBA32 副本，但仍消耗纹理单元、缓存与带宽。
- 本表只讨论二维纹理，不能把相同结论直接扩到体纹理。

块大小定义见 [Unity TextureFormat](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureFormat.html)；逐级 Mip 的计算见 [Mesh 与 Texture 存储基础]({{< relref "rendering/unity-rendering-supp-a-mesh-texture-storage.md" >}})。

**选 ASTC 的比较起点，均需检查实际内容**：
- 漫反射（Albedo）：`ASTC 6×6` 或 `ASTC 8×8`
- 法线贴图：`ASTC 4×4`（法线精度对光照影响明显，不建议过度压缩）
- Mask / Roughness / Metallic：比较块大小对参数和高光的误差，不能仅凭“遮罩”统一用大块。
- UI 图标：优先检查目标像素尺寸、细线、文字和透明边缘，再决定格式；不统一指定 12×12。

---

### ETC2

**支持条件**：ETC2 是 GLES 3.0 的标准压缩能力之一；其他 API 和 Apple 设备也需按具体格式查询，不能概括成“iOS 不支持”。

**压缩率**：
- ETC2 RGB：4 bpp，相对 RGB24 为 6:1；相对 RGBA32 为 8:1。
- ETC2 RGBA8：8 bpp，相对 RGBA32 为 4:1；只有 1-bit Alpha 需求时还可比较 ETC2 RGBA1（4 bpp）。

**ETC2 的局限**：
- block size 固定，不像 ASTC 可调
- 不支持 HDR
- 压缩误差取决于内容、编码和比较位率，不能用格式名称直接排固定画质名次

**什么时候用 ETC2**：需要支持低端 Android 设备（OpenGL ES 3.0 但不支持 ASTC）时用 ETC2 作为回退格式。现在新项目通常可以直接以 ASTC 为主，ETC2 作为 fallback。

`Platform Override` 指定目标导入格式，不自动建立一套 ASTC/ETC2 双资源包路由。需要多格式交付时，应明确构建与分发方案；不支持格式时实际如何回退、是否解压，要另行确认。

---

### PVRTC

**支持平台**：Imagination PowerVR GPU（主要是老 iOS 设备，现代 Apple GPU 也支持但更推荐 ASTC）

**压缩率**：2bit/pixel 或 4bit/pixel

PVRTC 在 Unity 导入中有尺寸与形状限制，需按实际格式和导入设置确认是否缩放。新项目先按设备能力评估 ASTC 等候选；兼容旧设备时再确认是否需要保留 PVRTC 路径。

---

### 实际设置建议

Unity 的纹理压缩设置在 Import Settings 里按平台独立配置：

```
Texture Type: Default / Normal Map
Format:
  iOS → ASTC 6×6（漫反射）/ ASTC 4×4（法线）
  Android → ASTC 6×6（主力）/ ETC2（fallback）
```

**注意 Alpha 通道**：相同块大小的 ASTC RGB(A) 每块仍为 128 bit。ETC2 RGB 与 RGBA8 则为 4/8 bpp；二值 Alpha 可另比较 RGBA1。拆出 Mask 需要把新增纹理、分辨率、采样和共同使用范围一起计入；两张同尺寸 4 bpp 图的数据量并不比一张 8 bpp 图更少。Alpha 存储与是否启用透明混合是两件事。

---

## RT 格式：每种格式的带宽代价

RT（Render Target）格式决定了每帧写入和读取 RT 的带宽代价。在 URP 里，相机颜色 RT 的格式由 Pipeline Asset 和 HDR 开关控制。

下面是 1920×1080、单采样图像写一次的**理论有效载荷**，不是实测总线流量或带宽速率；未计 Tile、压缩、对齐、重复写入和 Load/Store 行为：

| 格式 | 位深 | 1080P 数据量（写一次）| 典型用途 |
|------|------|-------------------|---------|
| R8G8B8A8_UNorm | 32bit | 约 7.91 MiB | LDR 颜色 RT |
| R16G16B16A16_SFloat | 64bit | 约 15.82 MiB | HDR 颜色 RT（含 Alpha）|
| B10G11R11_UFloatPack32 | 32bit | 约 7.91 MiB | 无 Alpha 的 HDR 候选 |
| R16_SFloat | 16bit | 约 3.96 MiB | 单通道数据 RT，不等于深度附件 |
| Depth16 | 16bit | 约 3.96 MiB | 深度缓冲，不含模板 |
| Depth32 | 32bit | 约 7.91 MiB | 深度缓冲，不含模板 |

**HDR RT 的格式选择**：
- `R16G16B16A16_SFloat`：本表中位深较高，数据载荷为 RGBA8 的两倍；是否值得使用取决于精度、动态范围和 Alpha 需求。
- `B10G11R11_UFloatPack32`：R/G 11 bit、B 10 bit，无 Alpha、不能表达负颜色；32 bit 数据量不代表与 RGBA8 的实际流量完全相同。
- URP 的选择受版本、HDR 精度选项、渲染路径和平台支持影响；用 capture 确认实际格式，不预设一个通用的 With/Without Alpha 菜单。

**深度格式**：
- 检查近远裁剪面、反向 Z、深度与模板需求后，比较可用格式的质量和成本。
- Shadow Map 的实际格式由管线和平台选择；不能把不存在的通用 `ShadowmapFormat.Bit16` 设置当作 URP 配置入口。

---

## Resolve：多采样 RT 的写回时机

当开启 MSAA 时，GPU 内部保存多份采样数据（如 2x MSAA = 每像素 2 份颜色和深度）。**Resolve** 是把多份采样数据合并成一份的过程，通常在 RT 写回系统内存之前执行。

**TBDR 上 Resolve 的特殊优势**：

在支持的 tile-based 渲染路径中，可以在片上保留多采样数据并完成 Resolve，只把单采样结果写回，从而避免存储和重读完整的多采样表面。这个优势依赖附件的生命周期、store action、pass 组织与硬件支持。

这不等于“移动端 MSAA 零成本”或“PC 带宽固定翻倍”：覆盖/深度样本、片上容量、混合和可能的 store/resolve 仍有成本。Apple 的 [MSAA 示例](https://developer.apple.com/documentation/metal/improving-edge-rendering-quality-with-multisample-antialiasing-msaa)展示了片上 Resolve 与需要额外 pass 的路径，应按实际捕获区分。

> **注意：Apple GPU 的片上存储可以被 Metal API 直接读写（称为 Tile Memory），是比普通 Tile Buffer 更强大的可编程扩展，详见 [Apple GPU 深度｜Tile Memory 与 Memoryless RT]({{< relref "rendering/zero-b-deep-02-apple-gpu.md" >}})。**

**需要检查额外保存与读取的情况**：

后续 pass 若要使用相机颜色或深度，管线可能复制、Resolve，或先执行深度预通道。具体采取哪种路径取决于 API、URP 版本和 Renderer Feature 的输入声明，不能从功能名直接推断。

```
// 在 capture 中检查，而不是预先断言：
// 1. _CameraDepthTexture 来自深度预通道还是 Copy Depth？
// 2. 颜色读取是否增加 Copy/Resolve、附件保存和新 render pass？
// 3. 多采样附件是否被保留到后续 pass，实际 store action 是什么？
```

URP 的深度生成受 Renderer 设置、MSAA、平台和各 pass 需求影响。移除不需要的输入请求可能减少工作，但改动后要用 Frame Debugger/capture 确认；不能凭修改 stencil reference 就宣布必然触发 Resolve。

Soft Particles、SSAO 等需要深度的功能适合作为检查入口；它们是否增加预通道、复制或外部访存需要实际证据。颜色 Resolve 与深度处理也须分别观察。

---

## 用工具量化带宽

### Xcode GPU Frame Capture（iOS）

在设备和工具版本支持的 GPU 计数器中检查 render pass、附件 load/store、纹理访问和外部内存流量。指标名称与可用性因设备变化，不能把一个固定菜单当作所有版本的入口。

**A/B 对比方法**：固定场景、分辨率、设备热状态和采样区间，只改变一项格式或 pass 需求。capture 用于确认结构；稳定耗时与流量另用重复采样比较。

完整的 Xcode GPU Frame Capture 工作流，包括 Memoryless RT 验证和 Shader 热点分析，见 [性能分析工具 05｜Xcode GPU Frame Capture]({{< relref "performance/mobile-tool-05-xcode-gpu-capture.md" >}})。

---

### Snapdragon Profiler（Android Adreno）

连接设备后，在 **GPU Counters** 里关注：
- L2 读写相关计数：覆盖哪些访问以对应 GPU 文档为准，不能等同所有纹理或所有 RT。
- 算术与纹理单元活跃度：用于定位候选方向；纹理单元忙不单独证明外部带宽饱和。

完整的 Snapdragon Profiler 连接和 Counter 解读方法见 [性能分析工具 04｜Snapdragon Profiler]({{< relref "performance/mobile-tool-04-snapdragon-profiler.md" >}})。

---

### Mali Graphics Debugger（Android Mali）

按所用 Mali GPU 与 Arm 工具版本查看外部读写、缓存和纹理访问计数；确认计数作用域、单位与采样区间。Frame capture 与连续性能计数可能来自不同工具，不预设旧版 Mali Graphics Debugger 提供全部指标。

工具背景见 [性能分析工具 03｜Mali GPU Debugger]({{< relref "performance/mobile-tool-03-mali-debugger.md" >}})，硬件延伸见 [Mali 现代架构深度]({{< relref "rendering/zero-b-deep-01-mali-modern-architecture.md" >}})。不同厂商的流量需在匹配工作负载和指标口径下比较，不预设哪一家更高。

---

## 小结

- **纹理压缩**：统一位率和基准，先确认格式能力；理论存储比不等于实际流量比。
- **RT 格式**：依据精度、Alpha、深度/模板和平台能力选择，确认实际附件格式。
- **MSAA Resolve**：检查片上驻留与附件生命周期，按实际 pass 区分预通道、复制、Resolve 和 store。
- **量化工具**：Xcode 看 Tile Memory Load/Store，Snapdragon Profiler 看 L2 带宽，Mali Debugger 看 External Memory 读写；各工具的详细使用见专项文章
- 带宽优化的优先级：先量化确认是带宽瓶颈，再按纹理 → RT 格式 → Load/Store 顺序排查
