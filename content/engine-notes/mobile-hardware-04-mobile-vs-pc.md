---
title: "移动端硬件 04｜移动端 vs PC / 主机：带宽、内存层级与驱动差异"
slug: "mobile-hardware-04-mobile-vs-pc"
date: "2026-03-28"
description: "移动端和 PC 的性能直觉是不同的操作系统。本篇从带宽、内存层级、驱动行为、图形 API 差异四个维度，建立移动端独有的性能判断体系，解释为什么 PC 上的经验直接迁移到移动端经常失效。"
tags:
  - "Mobile"
  - "Hardware"
  - "PC"
  - "性能对比"
series: "移动端硬件与优化"
weight: 2030
---

很多从 PC 游戏转向移动端开发的工程师会遇到同样的困惑：明明按 PC 的经验优化了，结果移动端还是卡。原因是**PC 和移动端的性能瓶颈模型根本不同**——PC 通常是 CPU 瓶颈或 DrawCall 瓶颈，移动端通常是带宽瓶颈或功耗瓶颈。

---

## 带宽差距的量化

这是最重要的单项差异：

| 平台 | 内存带宽 | 类型 |
|------|---------|------|
| RTX 4090 | 1,008 GB/s | GDDR6X（独立显存） |
| RTX 4080 | 717 GB/s | GDDR6X |
| RTX 4060（中端） | 272 GB/s | GDDR6 |
| PS5 | 448 GB/s | GDDR6（统一内存） |
| Xbox Series X | 336 GB/s | GDDR6 |
| 骁龙 8 Gen 3 | 77 GB/s | LPDDR5X（统一内存） |
| Apple A17 Pro | ~68 GB/s | LPDDR5 |
| 骁龙 7s Gen 2（主流） | 34 GB/s | LPDDR5 |
| 骁龙 6 Gen 1（低端） | 17 GB/s | LPDDR4X |

**旗舰手机的带宽约是 PC 中端独显的 28%，约是低端手机的 4.5 倍。**

### 带宽对具体操作的影响

```
一次 1080p 全屏读写（RGBA32）：
  数据量 = 1920 × 1080 × 4 bytes = 约 8MB

在 RTX 4090 上（1008 GB/s）：
  耗时 = 8MB / 1,008,000 MB/s = 约 0.008ms（几乎免费）

在骁龙 8 Gen 3 上（77 GB/s）：
  耗时 = 8MB / 77,000 MB/s = 约 0.1ms

看起来也很快，但问题在于：
  - 一帧内会有 10-20 次这样的读写（Bloom、SSAO、TAA、Blur...）
  - 总带宽消耗：0.1ms × 15 = 1.5ms（占 16.7ms 预算的 9%）
  - 同时 CPU 和 NPU 也在争抢这 77 GB/s
```

---

## 内存层级对比

PC 独显和移动端的内存架构完全不同：

```
PC 独显内存层级：
  GPU L1 Cache（每个 SM）：~32-128KB，极快
  GPU L2 Cache（共享）：~4-16MB，快
  VRAM（GDDR6X）：8-24GB，272-1008 GB/s
  RAM（DDR5）：32-128GB，51-88 GB/s（与 CPU 独立）

移动端 SoC 内存层级：
  GPU L1 Cache（Shader Core）：~8-32KB，极快
  GPU L2 Cache：~512KB-2MB，快
  Tile Buffer（SRAM，TBDR 特有）：~512KB-2MB，片上，几乎免费
  SLC / System Cache（CPU+GPU 共享）：~4-16MB，快
  LPDDR5X（CPU+GPU 共享）：6-16GB，17-77 GB/s
```

关键差异：
1. **移动端没有独立显存**：CPU 和 GPU 共享同一块 DRAM，互相竞争
2. **Tile Buffer 是移动端的救星**：TBDR 把一个 Tile 内的所有渲染计算放在片上 SRAM，避免频繁读写 DRAM
3. **SLC 命中可以大幅省带宽**：骁龙 8 Gen 3 的 6MB SLC，小纹理（LUT、Shadow Map 的小 Mip）可能命中

---

## 驱动行为：碎片化是移动端的最大痛点

PC 的 GPU 驱动（NVIDIA/AMD）成熟、行为一致，基本遵循 D3D/Vulkan 规范。

移动端驱动碎片化严重：

| GPU | 驱动来源 | 典型问题 |
|-----|---------|---------|
| Adreno（高通骁龙） | 高通 | 部分 Vulkan 扩展支持不完整；低版本驱动有 Shader 编译 Bug |
| Mali（联发科/三星） | Arm | Shader 精度行为（mediump 实际精度因版本而异）；某些 GLSL 特性未优化 |
| PowerVR（老款 iPhone/iPad Mini） | Imagination | GLSL 优化器激进（可能改变语义）；几乎已退出主流 |
| Apple GPU | Apple | 最一致，但只走 Metal，不支持 Vulkan |

**精度行为差异的例子**：

```glsl
// mediump float 在规范中保证 10 位精度（约 0.001 精度）
// 但实际行为：
//   Adreno 高端：mediump = highp（驱动自动提升精度）
//   Mali：mediump = 真正的 10 位（可能出现精度问题）
//   Apple：mediump = half（16 位），与规范一致

mediump float uv = position.x / 1000.0;
// Mali 上：uv 精度可能不足，导致纹理采样出现条纹
// Adreno 上：自动用 highp，没问题
// 解决：显式用 highp，或避免需要高精度的除法运算
```

---

## 图形 API 差异

| API | 平台 | 特点 |
|-----|------|------|
| DirectX 12 | PC（Windows） | 低级 API，高性能，仅 Windows |
| Vulkan | Android + PC | 低级 API，跨平台，移动端支持因设备而异 |
| Metal | iOS / macOS | Apple 专属，与硬件深度整合，性能好 |
| OpenGL ES 3.x | Android（旧） | 高级 API，驱动开销高，但兼容性好 |

**OpenGL ES vs Vulkan on Android**：

```
OpenGL ES 的隐式同步问题：
  驱动在内部管理 CPU-GPU 同步
  → 某些操作会导致 CPU 等 GPU 完成（隐式 Flush）
  → 开发者无法控制这个同步点
  → 表现为 CPU 帧时间出现不规律的尖峰

Vulkan 的显式控制：
  开发者手动管理 Pipeline Barrier、Semaphore
  → 可以精确控制同步点
  → 正确使用时 CPU 开销比 OpenGL ES 低 15-25%
  → 错误使用时可能比 OpenGL ES 更慢（过多 Barrier）
  → Unity 的 Vulkan 后端已处理大部分同步细节
```

**Unity 的 API 选择配置**：
```
Player Settings → Other Settings → Graphics APIs (Android)
  推荐顺序：Vulkan（首选）, OpenGL ES 3.x（Fallback）

  注意：勾选 "Auto Graphics API" 让 Unity 自动选择
  不推荐：仅保留 OpenGL ES（错过 Vulkan 性能收益）
         仅保留 Vulkan（低端设备可能崩溃）
```

---

## 主机 vs 移动端

| 指标 | PS5 | Xbox Series X | 旗舰手机 |
|------|-----|--------------|---------|
| GPU 算力 | 10.28 TFLOPS | 12 TFLOPS | ~2 TFLOPS |
| 内存带宽 | 448 GB/s | 336 GB/s | 77 GB/s |
| 内存容量 | 16GB GDDR6 | 16GB GDDR6 | 6-16GB LPDDR5 |
| TDP | ~200W | ~200W | ~10W |
| 散热 | 风扇 + 大型散热 | 风扇 + 大型散热 | 无风扇 |

主机的 GPU 算力约是旗舰手机的 5 倍，带宽约 6 倍，TDP 约 20 倍。

**对开发的启示**：
- 主机/PC 上的 Global Illumination、高质量 SSAO、Screen Space Reflections，在移动端通常需要用烘焙或简化方案替代
- 主机游戏的 LOD 策略（通常 2-3 个 LOD 级别）在移动端需要更激进（4-5 个级别）
- PC 上"可选的"优化（批处理、纹理压缩）在移动端是"必须的"

---

## 经验迁移的正确姿势

```
PC 经验 → 移动端时需要重新验证：

✅ 可以直接迁移：
  - DrawCall 越少越好（移动端 CPU 开销更高）
  - 纹理 Mip 生成（减少 Cache Miss）
  - LOD 系统（减少顶点数）
  - 避免每帧 new 对象（GC 压力）

⚠️ 需要调整的：
  - 后处理数量（PC 上可以用很多，移动端严格控制）
  - 动态阴影级联数（PC 4级，移动端 1-2 级）
  - 实时光源数量（PC 可以用很多，移动端 ≤ 2 个实时光）

❌ PC 经验在移动端完全失效：
  - "全屏 RT 读写很便宜" → 移动端带宽是瓶颈，每次全屏读写都有代价
  - "Deferred 渲染更适合多光源" → TBDR 上 G-Buffer 需要写回 DRAM，代价极高
  - "开更多线程能提速" → 移动端热密度高，多核满载会加速降频
```
