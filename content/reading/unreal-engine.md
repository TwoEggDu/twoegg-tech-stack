---
title: "Unreal"
description: "从 C++ 工程与对象系统出发，深入渲染、蓝图、GAS、网络复制和 Mass，理解 Unreal 各个系统怎样协作，以及它们对项目开发的约束。"
url: "/reading/engine/unreal/"
layout: "engine-directory"
reading_kind: "engine"
topic_key: "engine"
engine_name: "unreal"
---

按下面的栏目查找内容，也可以进入[架构与系统完整目录]({{< relref "system-design/unreal-engine-series-index.md" >}})连续阅读。

> **阅读状态说明**
>
> - **工程实践待验证**：已有正文和步骤，文中注明的 UE 编译、运行或打包结果仍需实际验证。
> - **待补内容**：现有路径尚未覆盖完整的专项说明、版本核对或实验。
>
> 各篇涉及的 Unreal 版本、工具操作和配置，以正文说明为准。

## C++ 入门、工程与模块

从 C# 开发经验进入 C++，理解对象所有权、工程输入、模块依赖，以及代码怎样经过编译和打包成为可运行程序。

- [C++ 公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})：先补值与借用、所有权、虚调用，再按需要补容器、回调、模板与编译链接；CXX-01～11 的公共正文已齐备，UE 专项与工程验证继续分别推进。
- [UCPP-1：普通对象与 UObject 的边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})：衔接创建、指针、保活、Outer 与业务结束；含完整自有 UE 对照样例，**UE 编译/运行与 GC 观察待验证**。
- [UCPP-2：容器、字符串与模板惯用法]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}})：读懂 TArray/TMap、名字与显示文字，以及移动/转发边界；UE 编译与运行待验证。
- [从 C# 到 C++：所有权与编译链接]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}})：通过普通 C++ 练习理解值、借用、RAII、unique_ptr，并区分编译、链接与运行错误。
- [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})：认识 .uproject、模块、Target、UHT，以及 Build／Cook／Stage／Package。**工程实践待验证：UE 工程创建、编译与打包仍需实际执行确认。**
- [第一个旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}})：连接 C++、组件、反射、蓝图默认值与关卡实例。**工程实践待验证：UE 编译、运行、断点与独立包行为仍需实际确认。**
- [模块系统与构建工具]({{< relref "system-design/ue-07-module-system.md" >}})：理解 UBT、.Build.cs 与模块依赖，建立拆分代码和开发插件的工程视角。

第一次进入 UE C++，可以按“公共 C++ 基础 → UE 工程地图 → 旋转 Actor → UCPP-1 对象与持有边界 → UCPP-2 容器/名字/转发 → 对象/反射/GC”阅读，再回看模块系统。公共入口复用上面的普通 C++ 首课，不需要把同一练习做两遍。

## 公共基础之后，补 UE 的约定

现有工程地图、Actor 和下方对象专题可先用来衔接。后续专项前置按以下顺序补齐，逐项解释 UE 在普通 C++ 之外增加了什么：

1. **对象与指针边界（已写，UE 实验待验证）**：[UCPP-1：普通对象与 UObject 的边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})，分开普通对象、UObject 与 Actor 的创建、GC 可见持有、弱观察、Outer 与结束协议。
2. **容器与模板惯用法（已写，UE 实验待验证）**：[UCPP-2：容器、字符串与模板惯用法]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}})，把 TArray/TMap 的查询、修改与访问有效性接到 FString/FName/FText、MoveTemp/Forward；含完整自有两文件样例及两个待验证的编译失败变体。
3. **Delegate 与 Lambda（待写）**：绑定、捕获、弱引用、解绑和延迟执行怎样约束访问对象的寿命。
4. **UHT/UBT（待补桥）**：复用工程地图、模块和反射专题，补生成文件的生产、消费与模块接线。
5. **任务线程（待补桥）**：复用线程专题，补任务捕获、对象访问和跨线程数据交接练习。

读对象系统前先完成第一项；读事件、异步和渲染线程前再补相应后续项。高级模板和任务系统不是运行第一个 Actor 的前提。

## 对象系统、反射与蓝图

理解对象、类型、默认配置和实例之间的关系，以及反射、序列化、GC 与蓝图执行怎样围绕对象系统协作。

- [UObject、UClass 与 CDO]({{< relref "system-design/ue-01-uobject-system.md" >}})：建立对象、类型信息与默认对象的基本关系。
- [反射与序列化]({{< relref "system-design/ue-02-reflection-serialization.md" >}})：理解 UHT、UPROPERTY、UFUNCTION 与生成信息。
- [GC 与对象生命周期]({{< relref "system-design/ue-03-gc.md" >}})：理解引用追踪、对象持有与回收边界。
- [Blueprint VM]({{< relref "system-design/ue-06-blueprint-vm.md" >}})：追踪蓝图字节码的编译与执行。
- [Unreal GC 深入阅读]({{< relref "system-design/ds-23-unreal-gc.md" >}})：进一步讨论智能指针、引用关系与两阶段销毁。

来自 Unity 的读者，也可以先看[GameObject 与 Actor 的区别]({{< relref "system-design/game-engine-architecture-02-gameobject-vs-actor.md" >}})，建立两种对象世界的对应关系。

接着用[同一组问题比较Unity与Unreal]({{< relref "system-design/game-engine-architecture-08-unity-unreal-same-tasks.md" >}})，沿警戒参数、外观交付和工具隔离追出实际接点，分清Prefab、Blueprint Class与组件模板的语义。

## 渲染、线程与世界流送

沿一帧画面的生成和大世界内容的加载，理解渲染抽象、跨线程协作与世界组织方式。

- [渲染架构：RHI 与 RDG]({{< relref "system-design/ue-04-rendering-architecture.md" >}})：理解图形接口抽象、渲染资源依赖与任务组织。
- [Game、Render 与 RHI 线程]({{< relref "system-design/ue-05-threading-model.md" >}})：理解一帧工作怎样跨线程传递，以及同步和访问边界。
- [World Partition、HLOD 与流送]({{< relref "system-design/unreal-world-partition-hlod-streaming.md" >}})：从世界分块、内容加载与远景表示，理解大世界管理的工程结构。

遇到渲染耗时或流送卡顿时，可以继续进入性能分析栏目。

## GAS 与技能系统

从核心组件进入属性、效果、技能生命周期、异步任务和网络预测，理解一个技能怎样从激活走到状态与表现落地。

**[从 Gameplay Ability System 总览开始]({{< relref "system-design/ue-gas-01-overview.md" >}})**

### 按机制查找

- [AttributeSet]({{< relref "system-design/ue-gas-02-attribute-set.md" >}})：属性定义、初始化与网络同步。
- [GameplayEffect]({{< relref "system-design/ue-gas-03-gameplay-effect.md" >}})：属性修改、持续时间、周期与叠加。
- [GameplayAbility]({{< relref "system-design/ue-gas-04-gameplay-ability.md" >}})：技能激活、执行、结束、消耗与冷却。
- [AbilityTask]({{< relref "system-design/ue-gas-05-ability-task.md" >}})：技能内部的异步操作与生命周期。
- [GameplayTag]({{< relref "system-design/ue-gas-06-gameplay-tag.md" >}})：状态描述、激活条件与查询。
- [GameplayCue]({{< relref "system-design/ue-gas-07-gameplay-cue.md" >}})：技能逻辑与视觉、音效反馈的连接。
- [GAS 网络预测]({{< relref "system-design/ue-gas-08-network-prediction.md" >}})：预测键、预测窗口与服务器验证。

### 深入执行链与设计边界

- [GAS 运行时执行链]({{< relref "system-design/skill-system-15-gas-runtime-chain.md" >}})：沿 AbilitySystemComponent、GameplayEffect 与 AttributeSet 追踪技能和属性变化。
- [GAS 网络预测层]({{< relref "system-design/skill-system-16-gas-prediction-layer.md" >}})：进一步理解预测、异步任务与多端状态同步。
- [Unity 自研技能系统与 GAS 的思想映射]({{< relref "system-design/skill-system-14-unity-self-built-vs-unreal-gas.md" >}})：比较概念、职责与边界，判断哪些设计可以借鉴。

## 网络复制与 Dedicated Server

理解客户端和服务器怎样分工，状态与请求怎样跨网络传递，以及服务器程序怎样构建、启动和部署。

- [网络架构：Actor 复制与 NetDriver]({{< relref "system-design/ue-net-01-architecture.md" >}})：建立连接、Actor 复制与相关性判断的整体视图。
- [RPC 类型与使用边界]({{< relref "system-design/ue-net-02-rpc.md" >}})：理解 Server、Client 与 NetMulticast 调用。
- [角色移动同步与预测校正]({{< relref "system-design/ue-net-03-movement-sync.md" >}})：理解移动输入、客户端预测与服务器校正。
- [PlayerController、GameMode 与登录流程]({{< relref "system-design/ue-net-04-player-controller.md" >}})：理解玩家接入、控制关系与规则归属。
- [复制、带宽与优先级优化]({{< relref "system-design/ue-net-05-optimization.md" >}})：进入条件复制、Delta 序列化、相关性与调度。
- [Dedicated Server 构建与部署]({{< relref "system-design/ue-net-06-dedicated-server.md" >}})：理解服务器构建、启动参数与服务器专属代码路径。

需要进一步研究服务器产物时，继续看[Dedicated Server 的 Target、Cook、打包与启动]({{< relref "system-design/game-backend-ded-srv-03-unreal-ds.md" >}})。

## Mass 与大规模实体仿真

理解数据导向实体怎样组织、调度和改变结构，以及仿真数据怎样与 Actor 和表现系统协作。

**[进入 Unreal Mass 深度专题]({{< relref "system-design/unreal-mass-series-index.md" >}})**

- [Mass 架构与概念地图]({{< relref "system-design/mass-01-framework-overview.md" >}})：认识 Fragment、Tag、Trait、EntityHandle，并与 DOTS 概念对照。
- [Processor 执行与调度]({{< relref "system-design/mass-02-processor-execution.md" >}})：理解查询、数据访问声明、依赖与执行阶段。
- [Mass 与 Actor 的协作边界]({{< relref "system-design/mass-06-actor-boundary.md" >}})：理解实体数据、视觉表示与传统对象体系怎样衔接。
- [City Sample 人群与交通案例]({{< relref "system-design/mass-07-city-sample-case.md" >}})：把框架机制放回具体场景，观察系统分工和项目选择。

结构变化、LOD 与 Signal 等完整篇目，见 Mass 专题目录。

## 性能分析与优化

先通过采样区分计算、等待和资源准备，再进入具体系统；修改之后，在相同条件下复测帧时间、内存与画质。

**[进入 Unreal 性能优化阅读路径]({{< relref "reading/unreal-performance.md" >}})**

### 已有阅读

- [性能分析工作流]({{< relref "system-design/ue-perf-01-profiling-workflow.md" >}})：从 Stat、Unreal Insights 与 GPU Visualizer 进入采样和归因。
- [CPU、Tick 与 TaskGraph]({{< relref "system-design/ue-perf-02-cpu-optimization.md" >}})：讨论系统更新、物理与 AI 开销，以及任务并行。
- [GPU、可见性、LOD 与渲染成本]({{< relref "system-design/ue-perf-03-gpu-optimization.md" >}})：讨论提交、剔除、阴影、后处理及 Nanite／Lumen 的相关成本。
- [内存、Texture Streaming、PSO 与 GC]({{< relref "system-design/ue-perf-04-memory-streaming.md" >}})：连接引用持有、资源驻留、首次使用与回收。

### 待补内容

- **采样与版本核对**：固定 Unreal 版本，补齐采集入口、Trace 通道，以及 Editor／PIE 与打包运行的对照。
- **专项诊断**：补充 Chaos 物理、动画求值与蒙皮、Slate／UMG 的独立诊断路径。
- **渲染验证**：补充固定场景下的成本归因，以及 Nanite／Lumen 的功能、平台和画质条件对照。
- **内存与流送验证**：补充引用退出、场景切换峰值、PSO 缓存覆盖与首次使用实验。
- **性能回归**：补充打包产物上的固定场景、多设备复测与可比较的结果记录。

现有机制说明与优化建议可用于形成排查方向；具体效果需要结合目标版本、设备和场景验证。

---

**继续阅读**

[引擎研发]({{< relref "reading/engine.md" >}}) · [战斗与玩法]({{< relref "reading/gameplay.md" >}}) · [服务端框架]({{< relref "reading/server.md" >}}) · [工程与交付]({{< relref "reading/delivery.md" >}})
