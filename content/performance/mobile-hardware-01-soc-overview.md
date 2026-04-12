---
title: "移动端硬件 01｜SoC 总览：CPU / GPU / 内存 / 闪存共享一块芯片意味着什么"
slug: "mobile-hardware-01-soc-overview"
date: "2026-03-28"
description: "移动端 SoC 把 CPU、GPU、内存控制器、ISP 全部集成在一块芯片上，这个架构决定了移动端性能的上限和瓶颈模式与 PC 完全不同。理解 SoC 的资源竞争关系，是移动端优化的起点。"
tags:
  - "Mobile"
  - "Hardware"
  - "SoC"
  - "性能优化"
series: "移动端硬件与优化"
weight: 2015
---

移动端和 PC 最根本的架构差异不是处理器频率，而是**所有组件共享同一块芯片、同一条内存总线**。这个约束决定了移动端性能优化的方向和 PC 完全不同。

---

## SoC 的全貌

以骁龙 8 Gen 3 为例，一块 SoC 上集成了：

```
骁龙 8 Gen 3（4nm，2023年）
  ├─ CPU：1×Prime (3.3GHz) + 3×Gold (3.15GHz) + 2×Gold+ (2.96GHz) + 2×Silver (2.27GHz)
  ├─ GPU：Adreno 750（同比性能提升 25%）
  ├─ DSP（Hexagon）：AI / 信号处理
  ├─ ISP（Spectra）：相机图像处理
  ├─ 内存控制器：LPDDR5X 双通道（理论带宽 77 GB/s）
  ├─ 存储控制器：UFS 4.0
  └─ 调制解调器（X75）：5G

对比：Apple A17 Pro（3nm，2023年）
  ├─ CPU：2×Performance + 4×Efficiency
  ├─ GPU：6 核（Apple GPU 架构）
  ├─ Neural Engine：16 核，35 TOPS
  └─ 内存：LPDDR5，理论带宽约 68 GB/s
```

关键点：**CPU、GPU、NPU 共享同一条内存总线**，它们争夺的是同一份带宽预算。

---

## 共享带宽是核心约束

| 平台 | 内存带宽 | GPU 架构 |
|------|---------|---------|
| 骁龙 8 Gen 3 | 77 GB/s（LPDDR5X） | Adreno 750 |
| Dimensity 9300 | 77 GB/s（LPDDR5X） | Immortalis-G920 |
| Apple A17 Pro | ~68 GB/s（LPDDR5） | Apple GPU 6-core |
| RTX 4090（PC） | 1,008 GB/s（GDDR6X） | — |
| RTX 4060（PC 中端） | 272 GB/s（GDDR6） | — |

**旗舰手机的总内存带宽约是 PC 中端独显的 1/4，约是旗舰独显的 1/13。**

这意味着：
- PC 上"几乎免费"的 Bloom Pass（全屏读写一次 RT）在移动端要消耗 2-4ms
- 4K 纹理在 PC 上加载速度极快，在移动端受限于内存带宽
- 粒子效果的 Overdraw（多层半透明叠加）在 PC 上可以接受，在移动端可以直接打穿帧率

---

## CPU 大小核架构对游戏的影响

现代骁龙 / Dimensity 采用三种核心配置：

```
Prime Core（超大核）：1 个，最高频率，最高功耗
  → 适合单线程敏感的主循环（GameThread、RenderThread）
  → 运行温度高，长时间工作会触发降频

Gold/Gold+ Core（大核）：3-4 个，高性能，较低功耗
  → Unity/Unreal Worker Thread、Job System 的主要承载

Silver Core（小核）：4 个，低性能，极低功耗
  → 后台任务、I/O、网络
  → 如果 GameThread 被调度器迁移到小核，帧率会骤降
```

**游戏开发中的影响**：

Android 的调度器（EAS，Energy-Aware Scheduler）会根据负载和温度决定在哪个核心运行线程。高负载场景下主线程通常在 Prime Core，但：
- 散热不良时 Prime Core 降频，调度器可能把线程迁移到 Gold Core
- 有时错误的 Thread Priority 设置会导致 GameThread 跑在小核上

```java
// Android NDK：设置线程亲和性（强制跑在大核）
// 不推荐在生产环境直接使用，但在压测时有用
#include <sched.h>
cpu_set_t cpuset;
CPU_ZERO(&cpuset);
CPU_SET(7, &cpuset);  // CPU 7 通常是 Prime Core（具体编号看设备）
pthread_setaffinity_np(pthread_self(), sizeof(cpu_set_t), &cpuset);
```

Unity 的 `Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount` 可以查看可用 Worker 线程数。

---

## GPU：为什么移动 GPU 选 TBDR

PC GPU（如 NVIDIA）使用 **IMR（Immediate Mode Rendering）**：每个 DrawCall 立刻执行，结果写回 VRAM，下一个 DrawCall 再读取。

移动 GPU 使用 **TBDR（Tile-Based Deferred Rendering）**：

```
TBDR 渲染流程：

第一阶段（Binning/Tiling Pass）：
  → 处理所有顶点变换
  → 确定每个图元属于哪个 Tile（通常 32×32 像素）
  → 结果存入 Parameter Buffer（写一次 DRAM）

第二阶段（Rendering Pass）：
  对每个 Tile：
    → 从 Parameter Buffer 读取该 Tile 的图元列表（读一次 DRAM）
    → 在片上 Tile Buffer（SRAM）中完成所有着色
    → 只把最终颜色写回 DRAM（写一次 DRAM）

IMR 流程（对比）：
  每个图元：
    → 读 Depth Buffer（DRAM）
    → 写 Color Buffer（DRAM）
    → 可能再读 Color Buffer（Alpha Blend）（DRAM）
```

TBDR 把"每个像素的中间结果"留在片上，**每帧 DRAM 带宽消耗约是 IMR 的 1/3**。这是移动端 GPU 在有限带宽下能运行的核心原因。

---

## 共享 LLC（Last Level Cache）

现代高端 SoC（骁龙 8 Gen 2/3、Apple A17）的 CPU 和 GPU 共享一个大 L3 / System Level Cache（SLC）：

- 骁龙 8 Gen 3：SLC 约 6MB
- Apple A17 Pro：System Cache 约 16MB

当 GPU 访问一个纹理时：
1. 先查 GPU L1 Cache（命中 → 极快）
2. 查 GPU L2 Cache（命中 → 快）
3. 查 SLC（命中 → 省一次 DRAM 访问）
4. 访问 DRAM（最慢）

**工程意义**：频繁访问的小纹理（如 LUT、Noise 纹理）命中 SLC 的概率高；超大纹理（4096×4096）基本只能从 DRAM 读取。这是移动端纹理尺寸优化的硬件依据。

---

## 设备层级分类与画质预设建议

| 层级 | 代表 SoC | GPU 性能 | 建议画质 |
|------|---------|---------|---------|
| 旗舰 | 骁龙 8 Gen 3、A17 Pro、Dimensity 9300 | >2 TFLOPS | 高画质，2×MSAA，动态阴影 |
| 高端 | 骁龙 8 Gen 1/2、A15、Dimensity 9200 | 1-2 TFLOPS | 中高画质，1× 阴影，限 60fps |
| 主流 | 骁龙 7s Gen 2、Dimensity 8200 | 0.5-1 TFLOPS | 中画质，烘焙阴影，30fps |
| 低端 | 骁龙 6xx、Dimensity 7xx | <0.3 TFLOPS | 低画质，关闭后处理，30fps |

```csharp
// Unity 设备层级检测示例
public static int GetDeviceTier()
{
    // SystemInfo.graphicsDeviceType + 内存 + 性能粗判断
    int memory = SystemInfo.systemMemorySize;
    string gpu = SystemInfo.graphicsDeviceName.ToLower();

    if (memory >= 6144 && (gpu.Contains("adreno 7") || gpu.Contains("mali-g71") || gpu.Contains("apple")))
        return 3; // 旗舰

    if (memory >= 4096)
        return 2; // 高端

    if (memory >= 3072)
        return 1; // 主流

    return 0; // 低端
}
```

---

## 实际限制：SoC 不能无限快

SoC 受限于两个硬上限：

1. **功耗墙（Power Wall）**：骁龙 8 Gen 3 满载约 10-12W，手机散热只能持续散走 4-6W；超过后触发热降频
2. **带宽墙（Bandwidth Wall）**：77 GB/s 是共享带宽上限；CPU+GPU+NPU 同时高负载时会相互争抢

这意味着**移动端性能优化的核心是减少带宽消耗和减少热功耗**，而不仅仅是减少计算量。
