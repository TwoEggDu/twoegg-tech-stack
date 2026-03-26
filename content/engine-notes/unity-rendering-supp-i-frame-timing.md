+++
title = "Unity 渲染系统补I｜帧时序与显示技术：VSync、Frame Pacing、输入延迟"
slug = "unity-rendering-supp-i-frame-timing"
date = 2026-03-26
description = "游戏渲染了 60fps，玩家感受到的未必是流畅的 60fps。帧时序（Frame Pacing）问题、VSync 模式选择、G-Sync/FreeSync 自适应同步、输入延迟链路——这些显示技术的细节直接影响玩家的手感和体验。"
weight = 1580
[taxonomies]
tags = ["Unity", "Rendering", "VSync", "Frame Pacing", "显示技术", "输入延迟", "G-Sync"]
[extra]
series = "Unity 渲染系统"
+++

"我们跑满 60fps"——这句话在开发中常被当作性能达标的标志，但它掩盖了一个重要细节：帧率是平均值，玩家感受到的是每一帧的实际时间间隔。16ms/16ms 和 8ms/24ms 的帧时间序列，平均帧率相同，玩家体验完全不同。帧时序（Frame Pacing）、VSync 模式、输入延迟链路——这些显示层的细节，直接决定手感和视觉流畅度。

---

## 显示刷新原理

### 显示器逐行扫描

传统显示器（包括现代 LCD 的驱动逻辑）以固定频率**逐行扫描**刷新：从屏幕左上角开始，逐行写入像素数据，直到右下角完成一帧，然后等待垂直回扫（Vertical Blanking Interval，VBI）信号后开始下一帧。刷新率（Refresh Rate，单位 Hz）决定这个过程每秒重复多少次。

### 撕裂（Screen Tearing）的产生

GPU 将渲染完成的帧写入**前缓冲（Front Buffer）**，前缓冲的内容被显示器读取显示。当 GPU 在显示器还没读完当前帧时就更新了前缓冲，显示器上半部分显示旧帧、下半部分显示新帧，产生**撕裂（Tearing）**。

撕裂是帧率与刷新率不同步的直接结果。GPU 帧率 > 刷新率时撕裂最明显（每显示周期 GPU 可以提交多帧）。

---

## VSync（垂直同步）

### 工作原理

VSync 要求 GPU 等待显示器的垂直消隐信号（VBI）后才能 Present 新帧到前缓冲。双缓冲（Double Buffering）下：GPU 渲染到后缓冲（Back Buffer），VBI 时刻交换前后缓冲。

**效果**：彻底消除撕裂。

**代价**：
1. **固定帧率档位**：如果 GPU 渲染用时超过一个刷新周期（16.67ms @ 60Hz），GPU 只能等下一个 VBI 信号，帧率从 60fps 直接掉到 30fps，再慢则掉到 20fps（整数分之一关系）。这个"悬崖效应"使轻微性能波动变成明显的帧率跳变。
2. **输入延迟增加**：最多增加一帧（16.67ms @ 60Hz）的延迟，因为 GPU 可能需要等待 VBI 信号才能 Present。

### Unity 中的 VSync 设置

`QualitySettings.vSyncCount`：
- `0`：关闭 VSync（允许撕裂，但帧率不受限制）
- `1`：每个 VBI 同步一次（60Hz 显示器 → 60fps 上限）
- `2`：每两个 VBI 同步一次（60Hz → 30fps 上限）

移动端 VSync 通常由平台系统强制开启，`vSyncCount` 设置在部分移动平台无效。

---

## 自适应同步（G-Sync / FreeSync）

### 核心思路的反转

VSync 的问题在于"GPU 等显示器"——如果 GPU 慢了，就只能等下一个固定刷新点。自适应同步（Adaptive Sync）反转了这个关系：**显示器等 GPU**，当 GPU 完成一帧时，显示器立即刷新，无需等待固定时间点。

结果：
- **无撕裂**：因为每次刷新都对应 GPU 完成的一帧
- **无 VSync 延迟**：不需要等待 VBI
- **无帧率悬崖**：帧率在支持范围内连续变化，不存在 60/30 跳变

### G-Sync vs FreeSync

| 特性 | NVIDIA G-Sync | AMD FreeSync (VESA Adaptive-Sync) |
|------|------------|----------------------------------|
| 标准 | NVIDIA 专有 | VESA 开放标准 |
| 硬件要求 | 显示器需 G-Sync 模块 | 显示器支持 Adaptive-Sync 即可 |
| 工作范围 | 通常 1~刷新率上限 | 通常 40~144Hz（依显示器） |
| 低帧率行为 | G-Sync 模块处理，稳定 | 低于下限时退回 VSync |
| 价格 | 显示器较贵 | 成本低 |

G-Sync Compatible 认证允许部分 FreeSync 显示器在 NVIDIA GPU 上使用 G-Sync 功能。

### Unity 的支持

Unity 本身不直接控制自适应同步，但关闭 VSync（`vSyncCount = 0`）并通过 `Application.targetFrameRate` 或不限帧率运行时，底层 DXGI/Vulkan 会与驱动协商自适应同步。PC 端通常只要：
1. 显示器和 GPU 支持
2. 驱动开启 G-Sync/FreeSync
3. 游戏以 Exclusive Fullscreen 或 Borderless Fullscreen 运行

自适应同步就会自动工作，无需代码干预。

---

## Frame Pacing（帧时序）

### 问题描述

"平均 60fps"可以由完全均匀的 16.67ms 间隔实现，也可以由交替的 8ms/25ms 实现——后者在 Profile 工具里显示"60fps"，但玩家会感到明显卡顿。

人眼对帧间时间的**变化**（Jitter）极其敏感，即使平均帧率相同，帧时间的不均匀也会被感知为卡顿（Stutter）。

### 导致 Frame Pacing 问题的原因

- **CPU/GPU 负载不均衡**：某些帧有额外的逻辑计算（AI、物理模拟）
- **垃圾回收（GC）暂停**：Mono GC 在某帧触发 Full Collect
- **资源异步加载完成回调**
- **系统级别的抢占**：操作系统调度打断游戏线程

### Android Frame Pacing Library（Swappy）

Android 的 Frame Pacing 问题尤为突出，原因是 Android 的 Present 时序（SurfaceFlinger 的 VSYNC 机制）与游戏渲染线程之间存在复杂的同步关系。

Google 提供的 **Android Frame Pacing Library（Swappy）**解决了这一问题：
- 在游戏的 Present 调用和 Android 的 SurfaceFlinger 之间插入精确的时序控制
- 根据目标帧率（30/60fps）选择最优的等待策略，避免意外的帧时间抖动
- Unity Android 构建在 2019.3+ 默认集成 Swappy

### Unity 的 Frame Timing Stats

Unity 提供 `FrameTimingManager`（需要在 Project Settings 中启用 `Frame Timing Stats`）：

```csharp
FrameTiming[] timings = new FrameTiming[1];
FrameTimingManager.CaptureFrameTimings();
FrameTimingManager.GetLatestTimings(1, timings);
Debug.Log($"CPU: {timings[0].cpuFrameTime}ms, GPU: {timings[0].gpuFrameTime}ms");
```

可以区分 CPU 时间和 GPU 时间，帮助定位 Frame Pacing 抖动的根本原因。

---

## Triple Buffering vs Double Buffering

### Double Buffering

前缓冲（显示）+ 后缓冲（渲染），VSync 时刻交换。如果 GPU 渲染完成但还未到 VBI，GPU 等待，这段等待时间是浪费的。

### Triple Buffering

前缓冲 + 两个后缓冲。GPU 完成一帧后可以立即开始下一帧（写入第二个后缓冲），不需要等待 VSync。VSync 时刻，从两个后缓冲中选取最新完成的帧交换到前缓冲。

**优点**：GPU 利用率更高，帧率在 VSync 开启时不会出现悬崖（从 60 掉到 30），可以保持在 30~60fps 之间连续。

**缺点**：
- 增加一帧渲染延迟（最坏情况下，新缓冲已在队列中，但显示的仍是旧帧）
- 内存占用增加（多一个帧缓冲）
- 在高帧率目标（如 120fps）下，这一帧延迟（约 8ms）相对更显眼

---

## 输入延迟链路

### 完整链路

玩家的操作从手指动作到光子到达眼睛，经历完整链路：

```
物理操作（手指/鼠标）
    ↓  USB/蓝牙轮询延迟（1~8ms）
操作系统输入事件
    ↓  游戏帧开始时读取
游戏逻辑（Input Sampling）
    ↓  Update → Rendering 管线
GPU 渲染完成
    ↓  Present 等待 VSync
前缓冲更新
    ↓  显示器扫描时间（1/2 帧到 1 帧，取决于像素位置）
光子到达眼睛
```

典型链路延迟：
- **无 VSync，低配置**：~50ms（理想情况）
- **有 VSync，60fps**：~80~100ms
- **有 VSync，30fps**：~130~180ms
- **竞技游戏（无 VSync + 高刷 144Hz）**：~20~30ms

### Unity Input System 的低延迟模式

Unity 新 Input System 提供 `InputSystem.settings.updateMode`：
- `ProcessEventsInDynamicUpdate`（默认）：在 `Update()` 开始时处理输入事件
- `ProcessEventsInFixedUpdate`：在 `FixedUpdate()` 处理，适合物理驱动的角色

对于需要最低延迟的场景（竞技游戏、VR），可以启用 **Low-Latency Input**（减少缓冲队列等待时间）。VR 平台（OpenXR）还提供专门的 Late Latching 机制，在 GPU 提交前最后一刻更新头部姿态，将头部追踪延迟压到 2~5ms。

---

## 移动端特殊性

### iOS ProMotion（最高 120Hz）

搭载 ProMotion 的 iPhone/iPad（iPhone 13 Pro 起）支持 1~120Hz 自适应刷新。在 Unity 中，通过 `Application.targetFrameRate = 120` 解锁 120fps 渲染，iOS 会自适应调整刷新率。

注意：120fps 的渲染代价约为 60fps 的两倍，需要充分的性能优化支撑。

### Android 可变刷新率

Android 的高刷支持高度碎片化：
- 设备型号众多，刷新率从 60Hz 到 165Hz 不等
- 需要通过 `Display.supportedModes` 枚举支持的刷新率并动态设置
- Android 12+ 提供更稳定的 `SurfaceControl` API

Unity 从 2022.1+ 提供 `Screen.SetResolution()` 配合 `Screen.currentResolution.refreshRateRatio` 管理刷新率。

### Application.targetFrameRate

Unity 在移动端的标准做法：
- `Application.targetFrameRate = 60`：60fps 目标（60Hz 设备）
- `Application.targetFrameRate = 30`：30fps 省电模式（策略类、SLG 等低交互游戏）
- `Application.targetFrameRate = -1`：不限制（VSync 接管，或不合理的耗电）

---

## 实践建议

**测量 Frame Pacing**：
- Unity Profiler 的 CPU Usage 模块：观察帧时间的波动，是否有周期性的 GC Spike
- Android GPU Inspector / Perfetto：可视化 SurfaceFlinger 的 Present 时序
- iOS Instruments：使用 Core Animation 工具查看帧时间分布

**关键指标**：
- **P95 帧时间**（第 95 百分位帧时间）比平均帧时间更能反映玩家感受
- **帧时间标准差**：衡量均匀性，理想值 < 2ms（60fps 目标下）
- **Missed Frame 比率**（超过目标帧时间 50% 的帧占比）

**移动端 Frame Pacing 清单**：
- 确认 Swappy 已集成（Unity Android 2019.3+）
- 对象池减少 GC 压力
- 异步加载使用 `LoadSceneAsync` + `allowSceneActivation = false` 控制完成时机
- 在帧时间稳定的帧完成 Texture Upload，而不是集中在一帧

---

## 小结

帧时序是一个容易被帧率平均值掩盖的质量维度。VSync 消除撕裂但引入延迟和帧率悬崖；G-Sync/FreeSync 以显示器等 GPU 的方式兼顾两者；Frame Pacing 保证帧间时间的均匀性；输入延迟链路的每个环节都有可优化的空间。真正的流畅感需要帧率、帧时序、延迟三个维度同时达标——单独优化其中一个，玩家仍然可能感受到问题。
