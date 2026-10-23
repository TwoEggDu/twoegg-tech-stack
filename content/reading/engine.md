---
title: "引擎研发"
description: "理解游戏引擎怎样组织内容、管理对象、执行代码并生成画面，以及这些机制如何影响工具开发、性能与交付。"
layout: "engine-directory"
reading_kind: topic
topic_key: engine
---

按 Piccolo、Unreal、Unity 进入具体实现，也可以直接查找架构、图形、运行时和计算基础专题。

[阅读建议：从客户端开发走向引擎研发]({{< relref "system-design/engine-programmer-reading-entry.md" >}})

## 按引擎进入

### [Piccolo｜沿具体实现理解引擎协作]({{< relref "reading/piccolo-engine.md" >}})

围绕启动流程、一帧执行、资源加载与系统交互，梳理各个模块怎样组成一台能够运行的游戏引擎。

**专题筹备中。** 当前入口提供已有前置阅读、官方材料与待写方向，尚无 Piccolo 专篇。

### [Unreal｜理解核心系统与扩展边界]({{< relref "reading/unreal-engine.md" >}})

从 UObject、反射、序列化与 GC 出发，深入渲染架构、线程模型、Gameplay、网络复制，以及 Mass 的实体组织与调度。

- [架构与系统]({{< relref "system-design/unreal-engine-series-index.md" >}})：连接对象系统、渲染与线程、Blueprint、GAS 和网络复制。
- [Mass 深度]({{< relref "system-design/unreal-mass-series-index.md" >}})：理解 Processor、结构变化、LOD，以及 Mass 与 Actor 的协作边界。

### [Unity｜从开发行为深入引擎机制]({{< relref "reading/unity-engine.md" >}})

沿资产导入、对象恢复、脚本编译、渲染与数据导向运行时，理解编辑器中的内容怎样进入游戏，以及工具和项目代码应当遵守哪些系统边界。

- [资产与序列化]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}})：理解资产身份、引用关系与运行时对象恢复。
- [渲染系统]({{< relref "rendering/unity-rendering-series-index.md" >}})：从渲染资产、空间与光照进入 SRP、URP 和管线扩展。
- [脚本编译管线]({{< relref "engine-toolchain/unity-script-compilation-pipeline-series-index.md" >}})：追踪编译、处理、域重载与构建流程。
- [DOTS 工程实践]({{< relref "system-design/dots-engineering-index.md" >}})：理解实体、数据、系统及其工程组织方式。

## 引擎架构与数据导向

理解引擎的整体分层，以及对象规模、数据布局和执行成本怎样影响系统设计。

- [游戏引擎架构地图]({{< relref "system-design/game-engine-architecture-series-index.md" >}})：把内容生产、世界模型、运行时基础、专业子系统、资产与发布放回同一张地图。
- [数据导向运行时]({{< relref "engine-toolchain/data-oriented-runtime-series-index.md" >}})：沿数据布局、结构变化、调度和表示层边界，理解 DOTS、Mass 与自研 ECS 的共同问题。
- [数据导向实战案例]({{< relref "system-design/dod-case-series-index.md" >}})：通过 RTS 单位管理、弹幕与混合架构，讨论适用范围。
- [数据导向行业对比]({{< relref "system-design/dod-industry-series-index.md" >}})：比较 Overwatch、idTech、Flecs 与 EnTT 等实现的组织方式和设计取舍。

## 图形系统与 GPU

沿一帧画面从引擎到屏幕的路径，理解图形抽象、空间计算、硬件执行和渲染技术。

- [游戏图形系统]({{< relref "rendering/game-graphics-stack-series-index.md" >}})：看清引擎、图形 API、驱动与 GPU 怎样共同完成一帧画面。
- [图形 API]({{< relref "rendering/graphics-api-series-index.md" >}})：理解图形 API 的职责及其与引擎后端的关系。
- [图形数学]({{< relref "rendering/graphics-math-series-index.md" >}})：把向量、矩阵、四元数和相交计算用于空间与几何判断。
- [GPU 架构与执行模型]({{< relref "rendering/zero-b-deep-series-index.md" >}})：理解 GPU 执行、带宽与 Tile Memory 对渲染的约束。
- [DLSS 进化论]({{< relref "rendering/dlss-evolution-series-index.md" >}})：沿超分辨率、重建与帧生成，理解渲染链路和硬件能力的变化。

## 语言运行时：.NET 与 CLR

理解代码从编译产物进入执行的过程，以及类型、对象、内存、泛型和程序集加载怎样由运行时实现。

- [.NET Runtime 生态总图]({{< relref "engine-toolchain/dotnet-runtime-ecosystem-series-index.md" >}})：查看规范与各实现之间的关系，选择阅读路径。
- [ECMA-335／CLI]({{< relref "engine-toolchain/ecma335-series-index.md" >}})：建立元数据、IL、类型系统、程序集与执行模型的基础。
- [CoreCLR]({{< relref "engine-toolchain/coreclr-series-index.md" >}})：深入类型加载、JIT、GC、泛型与线程。
- [Mono]({{< relref "engine-toolchain/mono-series-index.md" >}})：理解解释执行、JIT、AOT、GC 与嵌入式运行时。
- [IL2CPP]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})：追踪 IL 到 C++ 的转换，以及生成代码、元数据与运行时的配合。
- [HybridCLR]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：理解解释执行与 IL2CPP 的协作，以及热更新的工程边界。
- [LeanCLR]({{< relref "engine-toolchain/leanclr-series-index.md" >}})：研究独立 CLR 的元数据、解释器、对象模型与嵌入方式。
- [运行时横向对比]({{< relref "engine-toolchain/runtime-cross-series-index.md" >}})：围绕执行、GC、泛型、异常和加载比较不同实现。

## 计算基础

从硬件、存储和数据结构出发，理解引擎中常见成本的来源。

- [CPU 与内存体系]({{< relref "performance/hardware-cpu-index.md" >}})：理解流水线、Cache、SIMD、内存带宽与多核并行。
- [存储、文件系统与 OS I/O]({{< relref "engine-toolchain/storage-io-series-index.md" >}})：理解存储设备、页缓存、预读与异步 I/O，再回到游戏加载问题。
- [数据结构与算法]({{< relref "system-design/data-structures-and-algorithms-series-index.md" >}})：将复杂度、缓存与图论连接到空间查询、GC 和内存分配。

## 构建与调试基础

理解编译配置、调试信息与平台异常，建立从运行现象回到代码和产物的排查路径。

- [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})：分清 Debug／Release、编译链、断点、符号及 Unity 调试开关。
- [崩溃分析]({{< relref "engine-toolchain/crash-analysis-series-index.md" >}})：连接平台异常、符号化与 Unity／IL2CPP 排障。

---

**继续查找**

[性能优化]({{< relref "reading/performance.md" >}}) · [工程与交付]({{< relref "reading/delivery.md" >}}) · [全站技术目录]({{< relref "technical-topics.md" >}})
