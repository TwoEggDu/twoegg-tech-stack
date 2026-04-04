---
title: "GPU 渲染优化 02｜带宽优化：纹理压缩、RT 格式选择与 Resolve 时机"
slug: "gpu-opt-02-bandwidth"
date: "2026-03-25"
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
移动端 GPU 和 CPU 共享系统内存，带宽（Memory Bandwidth）是整个 SoC 的共享资源。GPU 读纹理、写 RT、Load/Store Tile 都在消耗这个资源。带宽打满的症状是帧时间随场景复杂度增加而线性上升，但 GPU 的 ALU 利用率并不高——不是计算慢，是数据搬运的速度跟不上。

---

## 纹理带宽：压缩格式的选择

纹理是 GPU 渲染时读取量最大的数据。一张 1024×1024 的未压缩 RGBA32 纹理占 4MB，GPU 每次采样时从系统内存读取；如果压缩到 1/8，变成 512KB，同样的采样次数带宽消耗降到原来的 1/8。

移动端的纹理压缩格式有三个主流选择：

---

### ASTC（Adaptive Scalable Texture Compression）

**支持平台**：iOS A8+（iPhone 6 及以上）、Android OpenGL ES 3.1+ / Vulkan（主流中端及以上）

**压缩率**：灵活可调，从 8×8 block（最高压缩，1bit/pixel）到 4×4 block（最低压缩，8bit/pixel）

常用 block size 的压缩率对比：

| Block Size | 压缩率（vs RGBA32）| 典型用途 |
|-----------|-----------------|---------|
| 4×4 | 1/2 | 法线贴图、需要高精度的贴图 |
| 6×6 | ~1/4.5 | 漫反射贴图 |
| 8×8 | 1/8 | 远景贴图、低精度贴图 |
| 12×12 | ~1/18 | UI 图标、低精度场景贴图 |

**ASTC 的优势**：
- 支持 HDR（浮点数据压缩），ETC2/PVRTC 不支持
- 支持任意 block size，可以精细控制质量/压缩率的平衡
- 支持 3D 纹理压缩（LUT、体积雾等场景）
- 硬件解压，采样时没有额外 CPU/GPU 代价

**选 ASTC 的配置建议**：
- 漫反射（Albedo）：`ASTC 6×6` 或 `ASTC 8×8`
- 法线贴图：`ASTC 4×4`（法线精度对光照影响明显，不建议过度压缩）
- Mask / Roughness / Metallic 贴图：`ASTC 8×8`
- UI 图标（不需要透明边缘精度）：`ASTC 12×12`

---

### ETC2

**支持平台**：Android OpenGL ES 3.0+（几乎所有现代 Android 设备），iOS 不支持

**压缩率**：
- ETC2 RGB：固定 4bit/pixel（压缩率 1/8 vs RGB24）
- ETC2 RGBA：固定 8bit/pixel（压缩率 1/4 vs RGBA32）

**ETC2 的局限**：
- block size 固定，不像 ASTC 可调
- 不支持 HDR
- 质量略低于 ASTC，尤其在高频细节区域有块状感

**什么时候用 ETC2**：需要支持低端 Android 设备（OpenGL ES 3.0 但不支持 ASTC）时用 ETC2 作为回退格式。现在新项目通常可以直接以 ASTC 为主，ETC2 作为 fallback。

Unity 的 `Platform Override` 可以为 Android 设置 ASTC 为首选，ETC2 为不支持时的回退。

---

### PVRTC

**支持平台**：Imagination PowerVR GPU（主要是老 iOS 设备，现代 Apple GPU 也支持但更推荐 ASTC）

**压缩率**：2bit/pixel 或 4bit/pixel

PVRTC 有一个独特限制：**纹理必须是正方形且边长是 2 的幂次**。不满足时 Unity 会自动 resize，导致内存浪费或质量下降。现代 iOS 设备全部支持 ASTC，PVRTC 基本不再推荐使用。

---

### 实际设置建议

Unity 的纹理压缩设置在 Import Settings 里按平台独立配置：

```
Texture Type: Default / Normal Map
Format:
  iOS → ASTC 6×6（漫反射）/ ASTC 4×4（法线）
  Android → ASTC 6×6（主力）/ ETC2（fallback）
```

**注意 Alpha 通道**：ASTC 的 RGB 和 RGBA 格式都支持，但 ETC2 的 RGBA（含 Alpha）压缩率是 RGB 的一半。如果 Alpha 通道只是 Mask（0 或 1），可以考虑把 Alpha 单独存一张贴图并用 ETC2 RGB，比存 ETC2 RGBA 更省。

---

## RT 格式：每种格式的带宽代价

RT（Render Target）格式决定了每帧写入和读取 RT 的带宽代价。在 URP 里，相机颜色 RT 的格式由 Pipeline Asset 和 HDR 开关控制。

常见格式的带宽对比（1080P，每帧一次全屏写入）：

| 格式 | 位深 | 1080P 带宽（写一次）| 典型用途 |
|------|------|-------------------|---------|
| R8G8B8A8_UNorm | 32bit | ~8MB | LDR 颜色 RT |
| R16G16B16A16_SFloat | 64bit | ~16MB | HDR 颜色 RT（高精度）|
| B10G11R11_UFloatPack32 | 32bit | ~8MB | HDR 颜色 RT（推荐）|
| R16_SFloat | 16bit | ~4MB | 深度 RT（部分用途）|
| Depth16 | 16bit | ~4MB | 深度缓冲（低精度）|
| Depth32 | 32bit | ~8MB | 深度缓冲（高精度）|

**HDR RT 的格式选择**：
- `R16G16B16A16_SFloat`：精度最高，但带宽是 LDR 的两倍，移动端通常不必要
- `B10G11R11_UFloatPack32`：同样是 32bit，支持 HDR 范围（R 和 G 各 11bit，B 10bit），无 Alpha 通道，带宽与 LDR 相同，**移动端 HDR 首选**
- 在 URP Pipeline Asset 里，HDR Mode 选 `With Alpha`（R16G16B16A16）还是 `Without Alpha`（B10G11R11）取决于是否需要 Alpha 通道

**深度格式**：
- 移动端通常用 Depth16（16bit）而不是 Depth32，减少带宽且精度对大多数场景够用
- Shadow Map 也推荐 16bit（`ShadowmapFormat.Bit16`），在 URP 的 Shadow 设置里可以配置

---

## Resolve：多采样 RT 的写回时机

当开启 MSAA 时，GPU 内部保存多份采样数据（如 2x MSAA = 每像素 2 份颜色和深度）。**Resolve** 是把多份采样数据合并成一份的过程，通常在 RT 写回系统内存之前执行。

**TBDR 上 Resolve 的特殊优势**：

在 TBDR 架构上，MSAA 的多份采样数据存在片上 Tile Buffer 里，Resolve 在 Tile 内部完成，**合并后的结果才写回系统内存**。这意味着 MSAA 的 Resolve 不产生额外的系统内存带宽。

这是为什么 MSAA 在移动端代价远低于 PC 的根本原因——PC 是 IMR 架构，MSAA 数据写在显存里，Resolve 需要读写显存，带宽代价翻倍；移动端 TBDR 的 Resolve 在 Tile Buffer 里发生，零额外带宽。

> **注意：Apple GPU 的片上存储可以被 Metal API 直接读写（称为 Tile Memory），是比普通 Tile Buffer 更强大的可编程扩展，详见 [Apple GPU 深度｜Tile Memory 与 Memoryless RT]({{< relref "engine-notes/zero-b-deep-02-apple-gpu.md" >}})。**

**Resolve 失效的情况**：

如果中途读取了 MSAA RT（比如 Depth Texture 采样软粒子），GPU 必须提前 Resolve 并写回系统内存，TBDR 的 Resolve 优势消失。

```
// 触发提前 Resolve 的操作（在 MSAA 开启时避免）：
// 1. 在 Opaque Pass 中途采样 _CameraDepthTexture
// 2. Renderer Feature 中途读取相机颜色 RT
// 3. 设置 DepthStencilState 的 stencilRef 在 MSAA Pass 中途改变
```

在 URP 里，`_CameraDepthTexture` 的生成时机受 `Depth Texture Mode` 控制。如果不需要在 Opaque Pass 中途采样深度，把这个设置关掉或推迟，避免触发提前 Resolve。

**这条"零带宽"结论的失效条件**：开启了 Soft Particles（软粒子）、屏幕空间反射（SSR）、SSAO，或任何在 Opaque Pass 之间读取深度的 Renderer Feature，都会让 Depth Buffer 提前 Resolve 并写入系统内存。此时"MSAA 在移动端很便宜"不再成立，需要权衡是否关闭相应功能。

---

## 用工具量化带宽

### Xcode GPU Frame Capture（iOS）

捕获帧后，在 **GPU → Memory** 标签页查看：
- `Tile Memory Load/Store`：每帧的 Load 和 Store 总量，直接反映 RT 切换带宽代价
- `Buffer/Texture Reads/Writes`：纹理采样和 RT 写入的总带宽

**A/B 对比方法**：同一场景，改变一个格式设置前后各捕获，对比 `Tile Memory Load` 数值。

完整的 Xcode GPU Frame Capture 工作流，包括 Memoryless RT 验证和 Shader 热点分析，见 [性能分析工具 05｜Xcode GPU Frame Capture]({{< relref "engine-notes/mobile-tool-05-xcode-gpu-capture.md" >}})。

---

### Snapdragon Profiler（Android Adreno）

连接设备后，在 **GPU Counters** 里关注：
- `L2 cache read bandwidth`：所有纹理采样的带宽
- `L2 cache write bandwidth`：RT 写入的带宽
- `% Time ALU Active` vs `% Time Texture Active`：如果 Texture Active 占比高，说明是带宽/纹理瓶颈而非计算瓶颈

完整的 Snapdragon Profiler 连接和 Counter 解读方法见 [性能分析工具 04｜Snapdragon Profiler]({{< relref "engine-notes/mobile-tool-04-snapdragon-profiler.md" >}})。

---

### Mali Graphics Debugger（Android Mali）

在 **Performance** 视图里：
- `External memory read bytes` / `External memory write bytes`：系统内存读写总量
- `Tile buffer read requests`：Tile Buffer 读取次数，高说明 Load 频繁

完整的 Mali GPU Debugger 带宽 Counter 对照见 [性能分析工具 03｜Mali GPU Debugger]({{< relref "engine-notes/mobile-tool-03-mali-debugger.md" >}})。如果想理解 Mali 的带宽消耗为何比 Adreno 更高，见 [Mali 现代架构深度 § 带宽模型]({{< relref "engine-notes/zero-b-deep-01-mali-modern-architecture.md" >}})。

---

## 小结

- **纹理压缩**：iOS 用 ASTC，Android 以 ASTC 为主、ETC2 为 fallback；漫反射 6×6~8×8，法线 4×4
- **RT 格式**：HDR 首选 `B10G11R11`（32bit，无 Alpha），避免 `R16G16B16A16`（64bit）；深度优先用 16bit
- **MSAA Resolve**：TBDR 上 Resolve 在 Tile Buffer 内完成，零额外带宽；但开启 Soft Particles / SSR / SSAO 等需要深度的功能时，会触发提前 Resolve，此时带宽优势消失
- **量化工具**：Xcode 看 Tile Memory Load/Store，Snapdragon Profiler 看 L2 带宽，Mali Debugger 看 External Memory 读写；各工具的详细使用见专项文章
- 带宽优化的优先级：先量化确认是带宽瓶颈，再按纹理 → RT 格式 → Load/Store 顺序排查
