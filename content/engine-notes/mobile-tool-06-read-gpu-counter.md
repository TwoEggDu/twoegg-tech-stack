---
title: "性能分析工具 06｜跨厂商 GPU Counter 对照：读懂 Adreno / Mali / Apple GPU 数据"
slug: "mobile-tool-06-read-gpu-counter"
date: "2026-03-28"
description: "移动端覆盖三大 GPU 厂商（高通 Adreno、Arm Mali、Apple GPU），每家工具和 Counter 命名不同，但背后的物理含义相通。本篇建立跨厂商 Counter 对照表，以及统一的瓶颈判断框架。"
tags:
  - "Mobile"
  - "GPU"
  - "性能分析"
  - "工具"
  - "跨平台"
series: "移动端硬件与优化"
weight: 2100
---

移动端 GPU 性能分析的最大挑战在于：三家 GPU 厂商各自有一套命名体系，同一个概念在不同工具里叫法完全不同。本篇统一这些概念，让你在切换工具时不需要重新学习。

---

## 三大 GPU 的工具对应

| 厂商 | GPU 系列 | 主要工具 | Frame Capture |
|------|---------|---------|--------------|
| 高通 | Adreno 5xx/6xx/7xx | Snapdragon Profiler | ✅ 内置 |
| Arm | Mali-G/T 系列 | Arm Mobile Studio (Streamline) | 需配合 RenderDoc |
| Apple | A/M 系列 | Xcode Instruments | ✅ 内置 |

---

## 核心概念跨厂商对照

### GPU 总利用率

```
概念：GPU 硬件在一帧内有多少时间在实际工作（不等待）

Snapdragon Profiler → GPU Busy %
Streamline (Mali)  → GPU_ACTIVE cycles / Total cycles
Xcode Instruments  → GPU Time / Frame Time

解读逻辑（三个工具一致）：
  > 90%：GPU 满负载（GPU 是瓶颈）
  < 70%：GPU 有空闲，可能 CPU 提交不够快（CPU 是瓶颈）
  50-70%：混合瓶颈，需要进一步细分
```

### Early-Z / HSR 效率

```
概念：在进入 Fragment Shader 之前，被提前剔除的无效片段比例

Snapdragon Profiler → LRZ Kill Rate（LRZ = Layer Rate Z）
Streamline (Mali)  → FRAG_QUADS_EZS_KILLED / FRAG_QUADS_RAST
Xcode Instruments  → Hidden Surface Removal Efficiency

解读逻辑（三个工具一致）：
  高（> 50%）：前到后排序正确，Early-Z 正常工作
  接近 0：Early-Z 失效

失效的通用原因（所有 GPU 相同）：
  1. Fragment Shader 里有 discard / Alpha Test
  2. 不透明物体从后到前渲染（应该从前到后）
  3. 半透明物体开了 Depth Write（应该关闭）
  4. 使用了 Depth Prepass 但顺序不对
```

### 外部内存带宽

```
概念：GPU 从片外 DRAM 读写的数据量

Snapdragon Profiler → AXI Bus Read/Write Bytes
Streamline (Mali)  → EXTERNAL_READ_BYTES / EXTERNAL_WRITE_BYTES
Xcode Instruments  → GPU Read Bytes / GPU Write Bytes

解读逻辑（三个工具一致）：
  带宽越高 = 功耗越高，热量越大
  参考阈值（1080p @ 60fps）：
    Read: < 12-15 GB/s（良好）
    Write: < 5 GB/s（良好）

超限的通用原因（所有 GPU 相同）：
  1. 纹理未压缩（RGBA32 应换成 ASTC/ETC2）
  2. Framebuffer 分辨率过高
  3. Render Pass 间切换过多（每次切换都要 Resolve）
  4. 大量半透明叠加（Overdraw）
```

### 着色器瓶颈

```
概念：着色器阶段的具体限制因素

Snapdragon Profiler：
  Fragment Shader % / Vertex Shader %（各阶段占用比例）
  ALU% / EFU%（数学运算 vs 特殊函数）

Streamline (Mali)：
  FRAG_SHADER_ALU_UTIL（ALU 利用率）
  FRAG_SHADER_LOAD_STORE_UTIL（纹理/存储访问）

Xcode Instruments：
  ALU Limiter / Texture Sample Limiter（哪个是瓶颈）

解读逻辑（三个工具一致）：
  ALU 高 → 简化 Shader 数学
  纹理采样 / LS 高 → 减少采样次数，使用更小/更好的纹理
  Memory / Bandwidth 高 → 压缩纹理，减小分辨率
```

---

## 瓶颈诊断通用框架

无论使用哪家 GPU 的工具，诊断流程是相同的：

```
Step 1：确认是 GPU 还是 CPU 瓶颈
  工具：Unity Profiler / Unreal stat unit
  信号：GPU 时间 > Frame 时间预算

Step 2：GPU 内部——确认利用率
  工具：GPU Busy / GPU_ACTIVE
  如果利用率低（< 70%）→ 很可能是 CPU 没有及时提交工作
  如果利用率高（> 90%）→ GPU 本身是瓶颈，继续 Step 3

Step 3：定位 GPU 内部的瓶颈层
  A. 带宽问题？→ AXI/EXTERNAL READ 超过阈值
  B. Shader 问题？→ ALU 或纹理采样是 Limiter
  C. 几何量问题？→ Vertex Shader 时间 > Fragment Shader 时间

Step 4：针对具体瓶颈类型进行 Frame Capture
  使用 Snapdragon/RenderDoc/Xcode 的 Frame Capture
  找到具体哪个 DrawCall/Shader/纹理是来源

Step 5：修改并验证
  修改前后在相同场景下对比 Counter 数据
```

---

## 常用 Counter 对照表

### 带宽类

| 概念 | Adreno (Snapdragon) | Mali (Streamline) | Apple (Xcode) |
|------|--------------------|--------------------|---------------|
| DRAM 读取 | AXI Read Bytes | EXTERNAL_READ_BYTES | GPU Read Bytes |
| DRAM 写入 | AXI Write Bytes | EXTERNAL_WRITE_BYTES | GPU Write Bytes |
| L2 命中率 | L2 Hit Rate | L2_READ_BYTES / L2_EXT_READ_BYTES | L2 Cache Miss Rate |
| 纹理带宽 | Texture Fetch | TEXTURE_FILT | Texture Sample |

### 着色器效率类

| 概念 | Adreno (Snapdragon) | Mali (Streamline) | Apple (Xcode) |
|------|--------------------|--------------------|---------------|
| Fragment 利用率 | Fragment Shader % | FRAG_SHADER_ACTIVE | Fragment Utilization |
| ALU 开销 | Shader ALU % | FRAG_SHADER_ALU_UTIL | ALU Limiter |
| 纹理采样开销 | Texture Filter | FRAG_SHADER_LS_UTIL | Texture Sample Limiter |
| Early-Z 效率 | LRZ Kill Rate | FRAG_QUADS_EZS_KILLED | HSR Efficiency |

### GPU 整体类

| 概念 | Adreno (Snapdragon) | Mali (Streamline) | Apple (Xcode) |
|------|--------------------|--------------------|---------------|
| GPU 利用率 | GPU Busy % | GPU_ACTIVE | GPU Time / Frame |
| 顶点吞吐量 | Vertex Primitives | PRIM_RAST | Vertex Invocations |
| 片段吞吐量 | Fragment Invocations | FRAG_QUADS_RAST | Fragment Invocations |

---

## Overdraw 的跨平台测量

Overdraw 是三个平台都关心的问题，测量方法略有不同：

```
通用计算公式：
  Overdraw = Fragment Shader Invocations / (屏幕像素数 × FPS)

Adreno (Snapdragon Profiler)：
  Fragment Invocations（直接读 Counter）
  / (1920 × 1080 × 60) = 可接受范围 1-4x

Mali (Streamline)：
  FRAG_QUADS_RAST × 4（每个 quad = 4 个片段）
  / (1920 × 1080 × 60)

Apple (Xcode)：
  Frame Capture → Overlay → Quad Overdraw（可视化热图）
  或 Fragment Invocations Counter

实际优化阈值（所有平台相同）：
  < 2x：优秀
  2-4x：正常（含 UI 层叠）
  > 6x：需要优化（通常来自粒子/半透明叠加）
```

---

## 无工具的快速粗判

当没有专业工具时，可以用以下方法快速判断问题方向：

```bash
# Android 通用：降低分辨率 50%，测试帧率变化
# Unity: urpAsset.renderScale = 0.5f;
# Unreal: r.ScreenPercentage 50

# 如果帧率提升 > 40%：
#   → 填充率/带宽是瓶颈（分辨率相关）
#   → 优化：降低渲染分辨率，减少后处理，压缩纹理

# 如果帧率提升 < 10%：
#   → DrawCall / 几何量是瓶颈（与分辨率无关）
#   → 优化：合并 Mesh，减少 DrawCall，使用 LOD

# 关闭所有后处理
# Unity: URP Global Volume → Post Processing 全关
# 如果帧率提升显著 → 后处理是瓶颈

# 关闭动态阴影
# Unity: Light → Shadow Type → No Shadows
# 如果帧率提升显著 → Shadow 是瓶颈
```

---

## 各平台瓶颈的特殊性

```
Adreno（骁龙）的特殊瓶颈：
  Binning Pass 过慢 → DrawCall 数量过多（Adreno TBDR 特有）
  LRZ 失效代价比 Mali 更显著（LRZ 设计上比 Mali EZS 更激进）

Mali（麒麟/Exynos/天玑）的特殊瓶颈：
  Varying 数据量大 → Vertex→Fragment 传递数据过多
  Tiler 过载 → Triangle 数量极多时 Tiler 成为独立瓶颈

Apple GPU 的特殊瓶颈：
  Tile Memory 超限 → MRT 太多或单帧数据量超过 On-Chip 容量，
                     强制写到 DRAM（代价极高）
  Compute → Fragment 同步代价 → Compute Shader 完成后 Fragment 必须等待
```

---

## 性能数据记录模板

在实际项目中，建议用统一模板记录每次测试：

```
场景：[场景名称]
设备：[设备型号 + 芯片]
分辨率：[渲染分辨率]
日期：[YYYY-MM-DD]

GPU 时间：__ms（预算 __ms）
GPU 利用率：__%

带宽：
  DRAM Read: __ GB/s（阈值 12 GB/s）
  DRAM Write: __ GB/s

着色器：
  ALU Limiter: __%
  Texture Limiter: __%

Early-Z 效率：__%

DrawCall 数量：__（预算 __）
三角面数量：__

Overdraw：__x

主要瓶颈：[带宽 / Shader / DrawCall / 几何量]
本次优化：[具体操作]
优化后 GPU 时间：__ms（提升 __%）
```
