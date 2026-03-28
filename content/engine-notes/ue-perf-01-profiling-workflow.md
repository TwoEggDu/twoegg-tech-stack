---
title: "Unreal 性能 01｜性能分析工作流：Stat 命令、Unreal Insights 与 GPU Visualizer"
slug: "ue-perf-01-profiling-workflow"
date: "2026-03-28"
description: "Unreal 性能优化的第一步不是改代码，而是定位瓶颈在哪个线程、哪个系统。本篇系统梳理 Stat 命令体系、Unreal Insights 的使用方法、GPU Visualizer 的解读，以及从症状到数据的完整定位流程。"
tags:
  - "Unreal"
  - "性能优化"
  - "Profiling"
  - "Unreal Insights"
series: "Unreal Engine 架构与系统"
weight: 6230
---

Unreal 性能优化最常见的误区是"凭感觉改代码"——在没有数据支撑的情况下优化，往往优化的不是真正的瓶颈。本篇建立一套**从症状到工具到数据**的完整分析流程。

---

## 理解帧时间预算

在开始分析之前，先确立目标：

```
60fps → 每帧预算 16.7ms
30fps → 每帧预算 33.3ms

Unreal 三线程模型中，帧时间取决于最慢的那条：
  帧时间 = MAX(GameThread, RenderThread, GPU)

典型分布（60fps 目标）：
  GameThread:   ≤ 12ms（留余量给 RenderThread 等待）
  RenderThread: ≤ 12ms
  GPU:          ≤ 14ms（通常是瓶颈端）
```

---

## 第一步：stat 命令快速定位

在游戏运行时（或 PIE），直接在控制台输入：

### stat fps / stat unit

```
stat fps        → 显示当前帧率和帧时间
stat unit       → 显示四条关键时间线

输出示例：
  Frame:  18.2ms    ← 总帧时间
  Game:   6.4ms     ← GameThread 耗时
  Draw:   11.8ms    ← RenderThread 耗时（CPU 侧渲染命令生成）
  GPU:    17.1ms    ← GPU 实际渲染耗时（超出帧预算！）
```

**读法**：
- `GPU` 最高 → GPU 瓶颈，去看 GPU Visualizer
- `Draw` 最高 → RenderThread 瓶颈，通常是 DrawCall 过多或 Visibility 计算
- `Game` 最高 → GameThread 瓶颈，去看 Tick / 物理 / AI

### stat unitgraph

以实时折线图显示 Game / Draw / GPU 三条线，方便观察帧率抖动的规律（是周期性抖动还是随机尖峰）。

### stat gpu

```
stat gpu        → GPU 各 Pass 耗时细分

输出示例（ms）：
  BasePass:         4.2
  ShadowDepths:     2.8
  Translucency:     1.1
  PostProcessing:   3.4
    Bloom:          1.2
    TemporalAA:     1.8
    Tonemapper:     0.4
  Total:           11.5
```

快速定位哪个 Pass 最贵，不需要打开 GPU Visualizer。

### stat scenerendering

```
stat scenerendering   → DrawCall 数量和可见 Primitive 统计

关注指标：
  Mesh draw calls:     1842   ← DrawCall 总数
  Static mesh draw calls: 1204
  Skeletal mesh draw calls: 48
  Visible static mesh elements: 3891  ← 可见静态网格数
```

### 其他常用 stat

```bash
stat game           # GameThread 各子系统耗时（Tick、物理、动画...）
stat anim           # 动画计算耗时
stat physics        # 物理模拟耗时
stat ai             # AI / NavMesh 耗时
stat streaming      # Asset Streaming 状态
stat memory         # 内存分类统计
stat rhi            # RHI 层指标（Triangles、DrawPrimitiveCalls）
stat startfile / stat stopfile  # 录制到文件供 Unreal Insights 分析
```

---

## 第二步：Unreal Insights 深度分析

`stat unit` 告诉你哪个线程慢，Unreal Insights 告诉你**为什么慢**。

### 启动录制

**方式一：运行时命令**
```bash
# 在游戏控制台输入
stat startfile          # 开始录制到 .utrace 文件
# 复现卡顿场景...
stat stopfile           # 停止录制
# 文件保存在 Saved/Profiling/UnrealInsights/
```

**方式二：启动参数**
```bash
MyGame.exe -trace=cpu,gpu,frame,bookmark -tracehost=localhost
# 同时启动 UnrealInsights.exe 连接
```

**方式三：Editor 内录制**
```
Tools → Run Unreal Insights → Session Browser → 选择运行中的进程
```

### Timing Insights 视图解读

```
┌─────────────────────────────────────────────────────────────────┐
│ Frame  │ 1    │ 2    │ 3    │ 4    │ 5    │ 6 (卡顿帧)         │
├────────┼──────┼──────┼──────┼──────┼──────┼────────────────────┤
│ Game   │▓▓▓▓  │▓▓▓▓  │▓▓▓▓  │▓▓▓▓  │▓▓▓▓  │▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │
│ Render │  ▓▓▓▓│  ▓▓▓▓│  ▓▓▓▓│  ▓▓▓▓│  ▓▓▓▓│    ▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │
│ RHI    │    ▓▓│    ▓▓│    ▓▓│    ▓▓│    ▓▓│              ▓▓▓▓ │
└─────────────────────────────────────────────────────────────────┘
```

点击卡顿帧，展开 GameThread 调用栈：

```
GameThread (28.4ms)
  └─ UWorld::Tick (26.1ms)
       ├─ AActor::TickActor (14.2ms)  ← 大量 Actor Tick
       │    ├─ AMyEnemy::Tick (8.1ms)  ← 敌人 AI 每帧全量更新
       │    └─ ...
       ├─ FPhysScene::Tick (7.3ms)
       └─ UNavigationSystem::Tick (4.6ms)  ← NavMesh 重建
```

这就能精确定位：是某个 Actor 的 Tick 太慢，还是物理、导航在拖累。

### 内存快照对比

```
Insights → Memory Insights → 选两个时间点做对比
可以看到哪些对象在增长（排查内存泄漏）
```

---

## 第三步：GPU Visualizer

`stat gpu` 给出数字，GPU Visualizer 给出**可视化的帧结构**。

### 打开方式

```
# 方式一：控制台命令
ProfileGPU

# 方式二：快捷键
Ctrl + Shift + ,（Editor 内）

# 方式三：命令行
-gpuprofile
```

### 解读 GPU Visualizer

```
┌──────────────────────────────────────────────────────────────┐
│ PrePass (Depth)          █████  2.1ms                        │
│ BasePass                 ██████████████████  8.4ms ← 最贵    │
│   Opaque                 █████████████  6.2ms                │
│   Masked                 █████  2.2ms                        │
│ ShadowDepths             ████████  3.8ms                     │
│   CascadedShadowMaps     ██████  2.8ms                       │
│   PointLight Shadows     ██  1.0ms                           │
│ Lighting                 ██████  2.9ms                       │
│ Translucency             ████  1.9ms                         │
│ PostProcess              █████████  4.2ms                    │
│   TemporalAA             ████  1.9ms                         │
│   Bloom                  ███  1.3ms                          │
│   Tonemapper             █  0.4ms                            │
│ Total                    ████████████████████████  23.3ms    │
└──────────────────────────────────────────────────────────────┘
```

**常见问题定位**：
- `BasePass` 过高 → 材质复杂度、Overdraw、DrawCall 过多
- `ShadowDepths` 过高 → 阴影 Cascade 数量、动态光源过多
- `PostProcess` 过高 → 后处理链太长（尤其 TAA / Bloom）
- `Translucency` 过高 → 半透明物体过多、排序开销

---

## 定位流程总结

```
症状：帧率下降 / 帧率不稳定
         │
         ▼
stat unit → 确定瓶颈线程（Game / Draw / GPU）
         │
    ┌────┼────┐
    ▼    ▼    ▼
   Game Draw  GPU
    │    │    │
    │    │   stat gpu + GPU Visualizer
    │    │   → 定位哪个 Pass 最贵
    │    │
    │  stat scenerendering
    │  → DrawCall 数量、可见面数
    │
Unreal Insights
→ 展开 GameThread 调用栈
→ 定位哪个系统 / Actor 耗时
         │
         ▼
找到根因 → 针对性优化
（见 ue-perf-02 CPU 优化 / ue-perf-03 GPU 优化）
```

---

## 控制台变量（性能调试常用）

```bash
# 关闭特定系统快速验证影响
r.Shadow.Enable 0           # 关闭阴影（验证阴影开销）
r.PostProcessing.Enable 0   # 关闭所有后处理
r.Lumen.Enable 0            # 关闭 Lumen
r.Nanite.Enable 0           # 关闭 Nanite

# 可视化
r.Wireframe 1               # 线框模式（看 Mesh 密度）
r.Overdraw                  # Overdraw 可视化
ShowFlag.Bounds 1           # 显示 Actor 包围盒（辅助 Culling 分析）

# 帧率限制（测试稳定性）
t.MaxFPS 60
```
