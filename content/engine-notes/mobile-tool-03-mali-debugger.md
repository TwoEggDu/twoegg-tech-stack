---
title: "性能分析工具 03｜Mali GPU Debugger：Counter 系统与带宽分析"
slug: "mobile-tool-03-mali-debugger"
date: "2026-03-28"
description: "Mali GPU Debugger（Arm Mobile Studio）提供 Counter 级别的 GPU 性能数据，是分析华为/三星/联发科设备 GPU 瓶颈的核心工具。本篇覆盖 Counter 系统架构、关键指标解读、带宽分析，以及与 Unity/Unreal 的集成。"
tags:
  - "Mobile"
  - "GPU"
  - "Mali"
  - "性能分析"
  - "工具"
series: "移动端硬件与优化"
weight: 2070
---

Mali GPU 是华为麒麟、三星 Exynos、联发科天玑系列的核心 GPU 架构。Mali GPU Debugger（现名 Arm Mobile Studio 套件中的 Performance Advisor）提供比 Unity/Unreal 内置 Profiler 更底层的 GPU 计数器数据。

---

## 工具套件概览

Arm Mobile Studio 包含三个工具：

```
Streamline         → 实时性能追踪（Counter 数据）
Mali Offline Compiler → 离线分析 Shader 指令数和 Register 占用
Performance Advisor → 自动生成性能报告，标注瓶颈
```

**下载**：Arm Developer 官网，免费，支持 Windows/Mac/Linux。

**支持的设备**：
```
Mali-G 系列（G51、G71、G76、G77、G78、G710、G715 等）
Mali-T 系列（T820、T830、T880）

对应设备：
  麒麟 9xx 系列（华为，Mali-G78/G710）
  Exynos 2x00 系列（三星，Mali-G78）
  天玑 9xxx 系列（联发科，Mali-G715/Immortalis）
```

---

## 连接设备

```bash
# 1. 确认设备可连接（与 Unity Profiler 类似）
adb devices

# 2. 安装 Mali GPU Debug Driver（需要 root 或 debug 模式设备）
# 大多数开发机（如三星 Galaxy 开发者版）支持免 root 调试

# 3. 或者：直接用 Streamline 通过 adb 连接
# Streamline → File → New Session → Android(adb)

# 注意：普通商业机型通常无法读取 GPU Counter
# 需要使用开发机（Developer Edition）或已解锁调试模式的设备
```

**设备要求的替代方案**：
- 无开发机时，可用 **Mali Offline Compiler** 分析 Shader 质量
- 可以用 **RenderDoc**（下文有说明）抓取 Frame Capture，不需要特殊驱动

---

## Counter 系统架构

Mali GPU 的 Counter 分为四个层次：

```
Layer 1: Shader Core（最内层，每个 EU 的统计）
  - 指令吞吐量（ALU / LS / Vary 利用率）
  - Register 使用率
  - Warp 占用率

Layer 2: Tiler（几何处理阶段，对应 TBDR 的 Binning 阶段）
  - Triangle 吞吐量
  - Varying 传递量
  - Culling 效率

Layer 3: Memory（内存子系统）
  - L2 Cache 命中率
  - External Bandwidth（最关键！）
  - MMU Translation

Layer 4: GPU Top-Level
  - 总 GPU 利用率
  - 各阶段时间比例
```

---

## Streamline 关键 Counter 解读

### GPU 利用率类

```
GPU_ACTIVE（最基础指标）
  = GPU 实际工作的时间比例
  < 70%：GPU 有空闲，可能是 CPU 供不上活（CPU 瓶颈）
  > 90%：GPU 满负载，需要继续分析具体瓶颈在哪个阶段

FRAGMENT_ACTIVE / VERTEX_ACTIVE
  = 片段着色器 / 顶点着色器的工作时间比例
  Fragment >> Vertex：填充率瓶颈（Shader 复杂或分辨率高）
  Vertex >> Fragment：几何量过大（顶点数多或 DrawCall 多）
```

### 带宽类（最重要）

```
EXTERNAL_READ_BYTES / EXTERNAL_WRITE_BYTES
  = 每秒从片外 DRAM 读/写的字节数

参考阈值（Mali-G78，1080p @ 60fps）：
  良好：< 8 GB/s
  警告：8-12 GB/s
  超限：> 12 GB/s（开始影响功耗和性能）

超限的常见原因：
  1. 纹理未压缩（RGBA32 替代 ASTC）
  2. Framebuffer 分辨率过高
  3. 大量全屏 Overdraw（半透明粒子堆叠）
  4. Resolve 次数过多（每次 Resolve = 写整个 Framebuffer）
```

```
L2_EXT_READ_BYTES vs L2_READ_BYTES
  L2_READ_BYTES = 总读取量（命中 L2 的不算 DRAM 带宽）
  L2_EXT_READ_BYTES = 实际走到 DRAM 的读取量

  缓存命中率 = (L2_READ_BYTES - L2_EXT_READ_BYTES) / L2_READ_BYTES
  命中率 < 70%：纹理缓存压力大（纹理过大、采样模式不友好）
```

### 着色器效率类

```
FRAG_SHADER_ALU_UTIL（片段着色器 ALU 利用率）
  > 80%：ALU 是瓶颈，Shader 数学计算量大
  < 50%：Shader 效率低，可能被 Memory 访问打断

FRAG_SHADER_LOAD_STORE_UTIL（LS 利用率）
  > 60%：Shader 里的纹理采样 / 存储访问过多

FRAG_QUADS_EZS_KILLED（Early-Z 剔除比例）
  高 = Early-Z 工作效率好（大量片段在进入 Shader 前被剔除）
  接近 0 = Early-Z 失效（Shader 里有 discard 或写 gl_FragDepth）
```

---

## 带宽分析实战

### 识别带宽超限的来源

在 Streamline 中，将以下 Counter 同时显示：

```
追踪列表：
  GPU_ACTIVE
  EXTERNAL_READ_BYTES（主要）
  EXTERNAL_WRITE_BYTES
  FRAG_QUADS_EZS_KILLED（Early-Z 效率）
  TEXTURE_FILT_FULL_RATE（纹理全精度采样率）
```

**带宽来源的拆解方法**：

```bash
# Step 1：关闭后处理
# 如果 EXTERNAL_READ_BYTES 下降 > 30%，后处理是主要来源
# 逐个关闭 Bloom/SSAO/DOF 判断各自占比

# Step 2：关闭粒子效果
# Particle System → Stop/Disable
# 如果带宽下降，粒子 Overdraw 是来源

# Step 3：降低分辨率 50%
# 如果带宽成比例下降，Framebuffer 操作是主要来源（Resolve / MSAA）
# 如果带宽没有成比例下降，是纹理采样带宽（与分辨率关系不大）
```

### Framebuffer Resolve 引起的带宽峰值

```
TBDR 的 Framebuffer 操作：
  On-Chip Tile 存储 → Render 完成后 Resolve 到 DRAM

  每次 Resolve = 写入 整个 Framebuffer 大小的数据
  1080p RGBA16 = 1080 × 1920 × 8 bytes = 15.7MB / 次 Resolve
  60fps = 15.7 × 60 = 943 MB/s（仅 Framebuffer Resolve）

  如果有多个 RT（MRT / Post-process），每个 RT 都需要 Resolve：
  3 个 RT = 943 × 3 = 2.8 GB/s（仅 Framebuffer 开销）

优化：
  - 减少 Pass 数量（合并 Pass 减少 Resolve 次数）
  - 使用 Subpass / Framebuffer Fetch（在 Tile 上直接读前一 Pass 的结果）
  - 避免不必要的 MSAA Resolve
```

---

## Mali Offline Compiler 分析 Shader

Mali Offline Compiler（malioc）不需要连接设备，直接分析 GLSL/SPIRV：

```bash
# 安装 Arm Mobile Studio 后，malioc 在 bin 目录
# 分析 Fragment Shader
malioc -c Mali-G78 -V fragment_shader.spv

# 输出示例：
# Fragment Shader:
#   8 work registers used
#   3 uniform registers used
#   Performance:
#     Varying Interpolation: 0.000
#     Texturing: 1.000    ← 每个 Shader Cycle 有 1 次纹理采样
#     Load/Store: 0.000
#     ALU:  0.625         ← ALU 利用率 62.5%
#     Total Cycles: 1.600
```

**关键指标解读**：

```
Total Cycles（每片段的总 Cycle 数）
  < 1.0：良好（每 Cycle 完成超过 1 个片段）
  1.0-2.0：正常
  > 3.0：Shader 较重，考虑简化

Texturing（纹理采样占 Cycle 比例）
  > 0.5：纹理采样是主要开销
  → 减少采样次数，使用更小的纹理

ALU 占比 > 0.8：数学计算密集
  → 简化数学表达式，用 LUT 替代复杂函数（三角函数等）

Work Registers > 32：Register 溢出风险
  → 编译器可能把 Register 溢出到 L1 缓存，降低效率
```

---

## 与 RenderDoc 配合使用

Mali 设备上，RenderDoc 可以抓取 Frame Capture（不需要特殊驱动）：

```bash
# 1. 在 Android 上安装 RenderDoc
# https://renderdoc.org → Android → 下载 APK

# 2. 在 Unity 中启用 RenderDoc 支持
# Edit → Graphics → Enable Frame Debugger / RenderDoc Integration

# 3. 通过 RenderDoc PC 客户端连接并抓取 Frame
# File → Attach to Running Instance → 选择 App

# 4. 分析每个 DrawCall 的：
#    - Input/Output Texture（检查分辨率和格式）
#    - Mesh（顶点数）
#    - Shader（GLSL 源码）
#    - GPU Timings（如果设备支持 timestamp query）
```

**RenderDoc 的主要用途**（在 Mali Debugger 之外）：
- 查看 Overdraw 可视化（Overlay → Quad Overdraw）
- 检查每个 DrawCall 的实际绘制内容
- 验证 Culling 是否正常（消失的 DrawCall）
- 检查纹理格式是否正确（是 ASTC 还是 RGBA32）

---

## Performance Advisor 自动报告

Arm Performance Advisor 基于 Streamline 的 Counter 数据，自动生成报告：

```
Streamline 录制完成后：
File → Export → Streamline Data (.apc)

在 Performance Advisor 中打开：
  → 自动分析并生成 HTML 报告

报告包含：
  1. 性能概要（帧时间、GPU 利用率）
  2. 带宽使用（外部带宽与预算对比）
  3. 热点警告（哪个 Counter 超出正常范围）
  4. 优化建议（自动推断可能的原因）
```

---

## 常见问题

```
问题 1：Streamline 连接后看不到 GPU Counter
  原因：设备不是开发版，GPU 驱动不暴露 Counter
  解决：
    a. 使用三星/华为开发者版设备
    b. 改用 RenderDoc 做 Frame Capture 分析
    c. 改用 Mali Offline Compiler 分析 Shader 质量

问题 2：外部带宽（EXTERNAL_READ_BYTES）异常高
  常见来源（按优先级排查）：
    1. 纹理未使用压缩格式（在 Frame Capture 中验证）
    2. 后处理 Pass 太多（逐个关闭对比）
    3. 粒子 Overdraw（关闭粒子系统对比）
    4. Shadow Map 分辨率过高（降至 1024 测试）

问题 3：Early-Z 命中率（FRAG_QUADS_EZS_KILLED）接近 0
  原因：
    a. Shader 里有 discard（Alpha Test）
    b. 不透明物体的渲染顺序不对（没有从前到后排序）
    c. Depth Prepass 未开启
  解决：
    - 减少 Alpha Test 使用（改 Alpha Blend 或改不透明）
    - 开启 URP 的 Depth Priming（Editor → URP Asset → Depth Priming Mode）
```
