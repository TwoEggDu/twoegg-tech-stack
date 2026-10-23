---
title: "Unreal 性能优化"
description: "从性能采样进入 CPU、GPU、内存与流送四条已有主线，再按动画、物理、UI 与验证缺口继续深入。"
url: "/reading/performance/unreal/"
reading_kind: "engine"
topic_key: "performance"
performance_engine: "unreal"
---

这条路线先串起本站已有的四篇 Unreal 性能文章。建议按“采样与归因 → 对应系统 → 相同条件复测”阅读；已有明确现象时，可以直接跳到 CPU、GPU 或内存与流送。

**阅读状态**：以下链接均有正文；其中的命令、CVar、工具操作和示例参数仍需结合目标 Unreal 版本核对。本页列出已有讨论与后续缺口，不将正文存在视为真机实验已经完成。

## 01 · 性能采样与瓶颈定位

先确定 Game、Render、RHI 与 GPU 之间的耗时和等待，再追到具体系统，避免只根据帧率或一个汇总数字修改代码。

**已有阅读**

- [性能分析工作流]({{< relref "system-design/ue-perf-01-profiling-workflow.md" >}})：已有 Stat、Unreal Insights 与 GPU Visualizer 的入门讨论。
- [Unreal 中性能问题怎样显形]({{< relref "performance/game-performance-unreal-symptoms.md" >}})：从通用诊断框架认识引擎中的症状。

> **待补内容**
>
> - 固定 Unreal 版本核对采样入口、Trace 通道和文件格式，补齐从捕获到时间线归因的可复现实验。
> - 打包运行、Editor / PIE、目标硬件及采样开销的对照记录。

## 02 · CPU、Tick 与系统更新

从 Game Thread 超预算进入更新频率、任务依赖、物理、AI 与导航，分清实际计算与等待，再讨论降低频率或并行化。

**已有阅读**

- [CPU 优化：Game Thread、Tick 与 TaskGraph]({{< relref "system-design/ue-perf-02-cpu-optimization.md" >}})：已有 Tick 调度、物理碰撞、AI 导航和任务并行的讨论。

> **待补内容**
>
> - Chaos 物理的独立诊断路线，目前主要散见 CPU 文章。
> - 动画求值、骨骼更新与蒙皮的性能专篇。
> - Slate / UMG 的更新、布局、绘制与输入诊断专篇。
> - 用明确的依赖和同步等待记录验证任务并行的收益。

## 03 · GPU 与渲染成本

按 Pass、可见性、几何、材质和画质能力拆成本，分别观察 CPU 的渲染准备与 GPU 执行。

**已有阅读**

- [GPU 优化：DrawCall、Culling、LOD 与 Nanite / Lumen]({{< relref "system-design/ue-perf-03-gpu-optimization.md" >}})：已有实例化、可见性、细节层次、阴影和后处理的讨论。

> **待补内容**
>
> - 固定版本与渲染路径，分别验证几何、材质、透明、阴影和后处理的帧时间影响。
> - Nanite / Lumen 的功能边界、平台条件和同场景画质 / 性能对照；原文参数不能直接作为通用预算。

## 04 · 内存、加载与流送

将引用持有、资源驻留、纹理流送、首次管线准备和 GC 分开，避免把所有首次卡顿归因于磁盘，或把所有内存增长归因于泄漏。

**已有阅读**

- [内存与流送：资产预算、Texture Streaming、PSO 与 GC]({{< relref "system-design/ue-perf-04-memory-streaming.md" >}})：已有引用、异步加载、驻留、PSO 和回收的讨论。
- [读盘完成不等于资源可用]({{< relref "performance/game-performance-read-does-not-mean-ready.md" >}})：作为文件、运行时对象与首次使用之间的通用边界说明。

> **待补内容**
>
> - 内存记录、引用持有与资源退出的完整对照案例。
> - 流送和异步加载收尾的采样，以及场景切换时延与峰值内存的联合验证。
> - PSO 缓存与预缓存流程的版本核对、覆盖检查和首次使用实验。

## 05 · 优化验证与回归

固定设备、版本、场景、操作和冷热状态，比较关键帧、持续帧时间、内存与画质，再决定是否保留改动。

**已有阅读 · 通用方法**

- [性能诊断方法]({{< relref "performance/game-performance-diagnosis-method.md" >}})：从现象形成可验证的假设。
- [设备矩阵、基线与性能回归]({{< relref "performance/rendering-tier-design-06-validation-loop.md" >}})：作为复测组织方法，具体采样接入仍需 Unreal 专项补齐。

> **待补内容**
>
> - Unreal 打包产物上的固定场景、Trace 留存与多设备回归实例。
> - 把上述 CPU、GPU、流送实验接入同一份可比较的结果记录。
