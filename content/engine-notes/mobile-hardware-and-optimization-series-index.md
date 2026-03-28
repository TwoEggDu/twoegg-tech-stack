---
date: "2026-03-28"
title: "移动端硬件与优化系列索引｜从 SoC、TBDR 和热约束，到 GPU / CPU / 工具链 / 平台碎片化"
description: "给移动端硬件与优化补一个稳定入口：先建立 SoC、TBDR、带宽、热和平台碎片化的直觉，再按 GPU、CPU、工具链和 Android/iOS 专题进入正文。"
slug: "mobile-hardware-and-optimization-series-index"
weight: 2010
featured: false
tags:
  - "Mobile"
  - "GPU"
  - "Optimization"
  - "Index"
series: "移动端硬件与优化"
series_id: "mobile-hardware-and-optimization"
series_role: "index"
series_order: 0
series_nav_order: 150
series_title: "移动端硬件与优化"
series_entry: true
series_audience:
  - "图形程序"
  - "移动端优化"
series_level: "进阶"
series_best_for: "当你想把移动端性能问题从 SoC、TBDR、带宽和热，一路追到 GPU / CPU / 工具链和平台差异"
series_summary: "把移动端性能问题从 SoC、TBDR、带宽和热约束，一路接到 GPU / CPU 优化、分析工具和平台碎片化。"
series_intro: "这组文章现在已经不只是图形优化目录。它实际覆盖五层：硬件直觉（SoC / TBDR / 档次 / 持续性能 / 耗电）、GPU 渲染优化、CPU 与内存优化、真机分析工具、Android / iOS 平台专项。真正的目标不是记技巧，而是先判断问题属于哪一层，再决定该看带宽、Shader、GC、Profiler、系统版本还是驱动。"
series_reading_hint: "第一次系统读，建议先按硬件基础 01 → 05 建立直觉，再按你当前问题进入 GPU、CPU、工具链或平台专题；如果你已经有现象，可直接用下面的问题索引跳转。"
---
这组文章现在已经长到不适合只靠一份线性目录阅读了。

如果你把它当成“移动端 GPU 优化合集”，会漏掉一半真正会决定结果的前提：设备档次、持续性能、热降频、分析工具、Android 系统规则和驱动碎片化。

所以这页索引更适合承担两个任务：

1. 先帮你判断问题大概属于哪一层。
2. 再把你送到最该看的那几篇，而不是从头顺读 20 多篇正文。

## 这组文章现在分哪五层

- 硬件基础：先立住 SoC、TBDR、机型档次、持续性能、热和耗电这些底层直觉。建议先看 [移动端硬件 01｜SoC 总览]({{< relref "engine-notes/mobile-hardware-01-soc-overview.md" >}})、[移动端硬件 02｜TBDR 架构详解]({{< relref "engine-notes/hardware-02-tbdr.md" >}})、[移动端硬件 02｜设备档次]({{< relref "engine-notes/mobile-hardware-02-device-tiers.md" >}})、[移动端硬件 02b｜持续性能]({{< relref "engine-notes/mobile-hardware-02b-sustained-performance.md" >}})、[移动端硬件 03｜功耗与发热]({{< relref "engine-notes/mobile-hardware-03-power-thermal.md" >}})、[移动端硬件 05｜耗电]({{< relref "engine-notes/mobile-hardware-05-battery-power.md" >}})。
- GPU 渲染优化：把 Draw Call、Overdraw、带宽、Shader、阴影、后处理、URP 配置和 Instancing 的取舍放回移动端成本模型里。优先看 [GPU 渲染优化 01｜Draw Call 与 Overdraw]({{< relref "engine-notes/gpu-opt-01-drawcall-overdraw.md" >}})、[GPU 渲染优化 02｜带宽优化]({{< relref "engine-notes/gpu-opt-02-bandwidth.md" >}})、[GPU 渲染优化 03｜Shader 优化]({{< relref "engine-notes/gpu-opt-03-shader.md" >}})、[GPU 优化 04｜移动端阴影]({{< relref "engine-notes/gpu-opt-04-shadow-mobile.md" >}})、[GPU 渲染优化 05｜后处理]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})、[GPU 优化 06｜URP 移动端 Pipeline 配置]({{< relref "engine-notes/gpu-opt-06-urp-pipeline-config.md" >}})、[GPU 优化 07｜GPU Instancing 深度]({{< relref "engine-notes/gpu-opt-07-instancing-deep.md" >}})。
- CPU 与内存优化：不是所有掉帧都来自 GPU。GC、IL2CPP、Update 调度、Profiler 读法和内存预算同样是移动端主线。建议看 [CPU 性能优化 01｜GC 压力]({{< relref "engine-notes/cpu-opt-01-gc-pressure.md" >}})、[CPU 性能优化 02｜IL2CPP vs Mono]({{< relref "engine-notes/cpu-opt-02-il2cpp-vs-mono.md" >}})、[CPU 性能优化 03｜Update 调用链优化]({{< relref "engine-notes/cpu-opt-03-update-scheduling.md" >}})、[CPU 性能优化 04｜Profiler CPU 深度分析]({{< relref "engine-notes/cpu-opt-04-profiler-cpu-deep.md" >}})、[CPU 性能优化 05｜内存预算管理]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}})。
- 分析工具：移动端优化如果没有真机证据，基本都会误判。建议先掌握 [性能分析工具 01｜Unity Profiler 真机连接]({{< relref "engine-notes/mobile-tool-01-unity-profiler-device.md" >}})、[性能分析工具 03｜Mali GPU Debugger]({{< relref "engine-notes/mobile-tool-03-mali-debugger.md" >}})、[性能分析工具 04｜Snapdragon Profiler]({{< relref "engine-notes/mobile-tool-04-snapdragon-profiler.md" >}})、[性能分析工具 05｜Xcode GPU Frame Capture]({{< relref "engine-notes/mobile-tool-05-xcode-gpu-capture.md" >}})、[性能分析工具 06｜跨厂商 GPU Counter 对照]({{< relref "engine-notes/mobile-tool-06-read-gpu-counter.md" >}})。
- 平台专项与碎片化：Android / iOS 的问题不只是渲染设置不同，还包括系统版本、厂商定制和驱动行为差异。建议看 [Unity on Mobile｜Android 专项]({{< relref "engine-notes/mobile-unity-android.md" >}})、[Unity on Mobile｜iOS 专项]({{< relref "engine-notes/mobile-unity-ios.md" >}})、[Android 版本演进]({{< relref "engine-notes/android-os-version-evolution.md" >}})、[Android 厂商定制]({{< relref "engine-notes/android-oem-customization.md" >}})、[Android Vulkan 驱动架构]({{< relref "engine-notes/android-vulkan-driver.md" >}})。

## 按你现在遇到的问题进入

- 同一场景在不同安卓机上差距很大：先看 [设备档次]({{< relref "engine-notes/mobile-hardware-02-device-tiers.md" >}})、[持续性能]({{< relref "engine-notes/mobile-hardware-02b-sustained-performance.md" >}}) 和 [Android 厂商定制]({{< relref "engine-notes/android-oem-customization.md" >}})。
- 开头满帧，玩 20 分钟后开始掉：先看 [功耗与发热]({{< relref "engine-notes/mobile-hardware-03-power-thermal.md" >}})、[耗电]({{< relref "engine-notes/mobile-hardware-05-battery-power.md" >}}) 和 [持续性能]({{< relref "engine-notes/mobile-hardware-02b-sustained-performance.md" >}})。
- 明显是 GPU bound，但不知道该先砍哪里：先看 [TBDR 架构]({{< relref "engine-notes/hardware-02-tbdr.md" >}})、[带宽优化]({{< relref "engine-notes/gpu-opt-02-bandwidth.md" >}})、[Shader 优化]({{< relref "engine-notes/gpu-opt-03-shader.md" >}})、[后处理]({{< relref "engine-notes/gpu-opt-05-postprocess-mobile.md" >}})。
- 帧率抖动更像 CPU / GC / 内存问题：先看 [GC 压力]({{< relref "engine-notes/cpu-opt-01-gc-pressure.md" >}})、[Update 调度]({{< relref "engine-notes/cpu-opt-03-update-scheduling.md" >}})、[CPU Profiler 深度分析]({{< relref "engine-notes/cpu-opt-04-profiler-cpu-deep.md" >}})、[内存预算]({{< relref "engine-notes/cpu-opt-05-memory-budget.md" >}})。
- 只在部分安卓机上出图形错、花屏或 Vulkan 问题：先看 [Android 版本演进]({{< relref "engine-notes/android-os-version-evolution.md" >}})、[Android 厂商定制]({{< relref "engine-notes/android-oem-customization.md" >}})、[Android Vulkan 驱动架构]({{< relref "engine-notes/android-vulkan-driver.md" >}})。
- 已经抓到真机数据，但不会读 Counter：先看 [Unity Profiler 真机连接]({{< relref "engine-notes/mobile-tool-01-unity-profiler-device.md" >}})，再按 GPU 厂商进入 [Mali]({{< relref "engine-notes/mobile-tool-03-mali-debugger.md" >}})、[Adreno]({{< relref "engine-notes/mobile-tool-04-snapdragon-profiler.md" >}})、[Apple GPU]({{< relref "engine-notes/mobile-tool-05-xcode-gpu-capture.md" >}}) 和 [跨厂商对照]({{< relref "engine-notes/mobile-tool-06-read-gpu-counter.md" >}})。

## 第一次系统读，建议按这条主线

1. 先读硬件基础：SoC → TBDR → 设备档次 → 持续性能 → 功耗与发热 → 耗电。
2. 再读 GPU 优化主线：Draw Call / Overdraw → 带宽 → Shader → 阴影 / 后处理 / URP / Instancing。
3. 最后补 CPU、工具链和平台碎片化，这样遇到真实项目问题时更容易判断“该从哪条证据链下手”。

{{< series-directory >}}

