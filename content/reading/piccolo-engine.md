---
title: "Piccolo"
description: "沿工程启动、一帧执行、对象、资源与渲染链路，理解一台具体引擎中的系统协作。当前提供前置阅读、官方材料与待写方向。"
url: "/reading/engine/piccolo/"
layout: "engine-directory"
reading_kind: "engine"
topic_key: "engine"
engine_name: "piccolo"
reading_status: "专题筹备中"
---

> **已有内容与待写内容**
>
> 当前尚无 Piccolo 专篇。下面可点击的站内链接均为已有的公共前置或其他引擎的对照阅读，不代表已经完成 Piccolo 实现分析。
>
> Piccolo 篇目均为**待写建议拟题**，尚未形成正文或正式提纲。源码阅读、构建与运行实践尚待开展。

**官方材料**

- [Piccolo 官方仓库与构建说明](https://github.com/BoomingTech/Piccolo)
- [Piccolo 官方 Wiki](https://github.com/BoomingTech/Piccolo/wiki)

后续主线拟从工程启动进入一帧执行，再沿对象、资源、渲染与编辑器深入；具体实现以届时选定的源码版本为准。

## 工程、构建与启动

理解怎样建立工程基线，引擎从哪里启动，以及各个系统怎样完成初始化与退出。

**已有前置阅读**

- [从 C# 到 C++：所有权与编译链接]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})：普通 C++ 练习，补充生命周期、RAII、编译与链接基础。
- [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})：补充构建配置、符号、断点与调试原理。

**待写建议**

- **Piccolo 工程地图：源码、依赖、构建目标与运行产物（待写）**
- **Piccolo 启动链：从程序入口到系统初始化与退出（待写）**

本站的 Piccolo 构建与运行实践尚待补充。

## 一帧执行与系统协作

理解一帧工作怎样推进，以及输入、逻辑、场景更新和渲染之间怎样交换数据。这条完整链路将作为后续研究各个系统的主入口。

**已有前置阅读**

- [引擎运行时底座]({{< relref "system-design/game-engine-architecture-03-runtime-foundation.md" >}})：理解执行模型、任务、对象元信息等机制在引擎中的位置。
- [CPU 与内存体系]({{< relref "performance/hardware-cpu-index.md" >}})：研究数据布局、缓存和并行成本时查阅。

**待写建议**

- **Piccolo 的一帧：从主循环追踪系统调用与数据流（待写）**
- **Piccolo 的更新顺序：系统依赖、数据交接与同步边界（待写）**

## 场景、对象与组件

理解世界中的对象怎样被描述、创建、更新和销毁，以及组件怎样参与运行。

**已有对照阅读**

- [GameObject 与 Actor：两种世界组织方式]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}})：提供比较对象模型的观察角度，正文讨论的是 Unity／Unreal。

**待写建议**

- **Piccolo 的世界模型：场景、对象、组件与生命周期（待写）**
- **从一个场景对象追踪数据变化：配置、运行状态与表现同步（待写）**

## 资源、反射与序列化

研究资源文件怎样描述对象、类型信息怎样参与读写，以及数据怎样恢复成可运行的场景。

**已有前置与对照阅读**

- [资产与发布在引擎中的位置]({{< relref "system-design/game-engine-architecture-05-asset-and-delivery.md" >}})：理解导入、资产、构建与交付之间的关系。
- [Unity 资产系统与序列化]({{< relref "engine-toolchain/unity-asset-system-and-serialization-series-index.md" >}})：作为另一种实现的对照阅读。

**待写建议**

- **Piccolo 资源链：从磁盘文件到场景与运行时数据（待写）**
- **Piccolo 的类型信息与序列化：代码生成、字段读写与对象恢复（待写）**

具体机制、标题与范围仍需围绕选定源码版本核实。

## 渲染链路与图形接口

研究场景数据怎样进入渲染系统，经过资源准备、渲染阶段和图形接口提交形成画面。

**已有前置阅读**

- [游戏图形系统]({{< relref "rendering/game-graphics-stack-series-index.md" >}})：建立引擎、图形 API、驱动与 GPU 的整体链路。
- [图形 API 基础]({{< relref "rendering/graphics-api-series-index.md" >}})：理解命令、资源与平台后端的抽象。
- [图形数学]({{< relref "rendering/graphics-math-series-index.md" >}})：补充空间变换、可见性与几何判断。

**待写建议**

- **Piccolo 渲染入口：从场景对象到一帧画面（待写）**
- **沿一个渲染阶段深入：资源、命令与 GPU 提交（待写）**

先建立完整链路，再选择局部阶段深入；具体篇目待源码核对后确定。

## 编辑器、调试与最小改动验证

研究编辑器怎样改变运行数据，怎样观察系统行为，以及一次改动怎样证明生效。

**已有前置阅读**

- [为什么引擎首先是一套内容生产工具]({{< relref "system-design/game-engine-architecture-01-content-production.md" >}})：理解编辑器工作流怎样影响引擎结构。
- [RenderDoc 阅读入口]({{< relref "rendering/renderdoc-reading-entry.md" >}})：补充图形捕获与读图方法，Piccolo 的具体接入步骤仍待补充。

**待写建议**

- **Piccolo 编辑器与运行时：一次属性修改怎样影响场景（待写）**
- **Piccolo 最小验证：断点、日志、画面与图形捕获怎样互相印证（待写）**

后续实践文章将分别记录源码版本、构建结果和运行观察，方便复现。

---

**继续阅读**

[引擎研发]({{< relref "reading/engine.md" >}}) · [游戏引擎架构地图]({{< relref "system-design/game-engine-architecture-series-index.md" >}})
