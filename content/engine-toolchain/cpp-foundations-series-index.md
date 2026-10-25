---
title: "C++ 公共基础｜从语言与工程基础走向四条源码路线"
slug: "cpp-foundations-series-index"
date: "2026-10-25"
description: "先理解 C++ 的对象、所有权、调用、内存与构建，再进入 Piccolo、Unreal、HybridCLR 和 IL2CPP 的专项前置与源码主线。"
type: "reading-guide"
layout: "engine-directory"
reading_kind: "foundation-index"
topic_key: "engine"
reading_title: "C++ 公共基础"
series: "C++ 公共基础"
series_id: "cpp-foundations"
series_title: "C++ 公共基础"
series_role: "index"
series_order: 0
series_entry: true
series_audience: ["有 C# / Unity 经验的开发者", "准备阅读引擎与运行时源码的读者"]
series_level: "基础到进阶"
series_best_for: "能写业务代码，希望逐句解释引擎与运行时中的 C++"
series_summary: "共用语言与工程基础，按项目补专项前置，再进入具体源码。"
series_intro: "围绕对象怎样存活、调用怎样发生、内存怎样被访问和代码怎样进入程序，建立跨项目复用的 C++ 阅读能力。"
series_reading_hint: "从公共第一课的值与访问开始，再借用现有文章学习所有权、类与生成代码；其余公共课按计划补齐。"
tags: ["C++", "源码阅读", "Piccolo", "Unreal", "HybridCLR", "IL2CPP"]
weight: 490
---

读引擎源码时，能认出类名和函数名，还不一定能解释一次赋值是否复制对象、一个回调何时失效，或者跳到的声明究竟由谁实现。这些问题会在不同项目中反复出现，适合先建立一套共用的 C++ 基础。

本专栏面向已经会编程、主要有 C#／Unity 经验的读者。先用独立小程序理解语言和工程规则，再进入各项目的对象系统与运行时约定。

**阅读分三层：公共 C++ 基础 → 各路线专项前置 → 源码主线。** 专项前置会重新说明通用规则在具体项目中怎样使用，并补充该项目特有的知识。

> **当前进度**：公共目录规划了 11 个正文单元，CXX-01 已写，其余 5 个待整理、5 个待写。原有四篇材料继续保留在原系列，承担综合练习和项目案例；复用链接不增加公共正文数量。各篇的小程序、引擎运行和发布状态以正文记录为准。

## 现在从哪里开始

先读 [CXX-01：一次传参到底复制、借用还是只读访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})。用一个独立 C++17 程序分清对象与指针变量、const、auto 和借用寿命，再进入下面的既有材料。其余公共课继续整理，Piccolo 文章保留具体项目案例。

| 用途 | 已有文章 | 先掌握什么 |
|---|---|---|
| 综合练习 | [从 C# 到 C++：对象由谁持有，代码怎样变成程序]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) | 按需复做 RAII、基本 move、vector 失效，以及多文件编译、链接与调试 |
| 所有权 | [智能指针与对象生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}}) | 谁负责保活与释放；业务清理、容器移除和析构怎样区分 |
| 类与调用 | [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}}) | 静态/动态类型、覆盖、切片、虚析构，以及怎样找到真正执行的实现 |
| 模板与生成 | [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}}) | 类型参数、名字字符串、宏、外部生成文件和运行时注册分别发生在哪里 |

编译或断点遇到困难时，补读 [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})。普通 C++ 示例通过，只说明相应示例的结果；具体项目还需要自己的构建与调试基线。

<a id="chapters"></a>

## 公共课目录与补写状态

“待整理”表示主要材料已有，但仍需整理为独立公共课；“待写”表示尚缺完整单元，现有零散说明不能替代它。编号用 CXX，与 Piccolo 原有 CPP 编号分开。

| 单元 | 要解决的问题 | 状态 |
|---|---|---|
| [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}}) | 传参、const、auto、指针和引用究竟表达什么 | 已写，含完整程序、编译失败练习和验证记录 |
| CXX-02 生命周期与所有权 | 作用域、RAII、独占/共享/弱观察怎样确定释放责任 | 下一篇待整理，先读 UFS 综合练习与智能指针篇 |
| CXX-03 类与调用 | 重载、覆盖、切片和析构怎样影响实际执行 | 待整理，先读现有类与虚函数篇 |
| CXX-04 移动与容器 | 复制、移动、扩容、删除之后，地址和迭代器还能不能用 | 待写；UFS 综合练习已有起点 |
| CXX-05 函数指针与回调 | 行为怎样被保存；Lambda、捕获和 this 的寿命怎样约束调用 | 待写 |
| CXX-06 布局与字节访问 | 大小、对齐、偏移、指针运算和类型转换怎样连接内存与值 | 待写 |
| CXX-07 模板阅读 | 推导、特化、类型工具和转发怎样选择实现 | 待整理；现有模板篇覆盖基础部分 |
| CXX-08 宏与代码生成 | 一段代码由谁产生，在哪个阶段被使用 | 待整理，先读现有模板与生成篇 |
| CXX-09 存储与对象 | 分配、构造、析构、释放和未定义行为的边界在哪里 | 待写 |
| CXX-10 编译链接与符号 | 头文件、翻译单元、库、生成文件和正在运行的程序怎样对应 | 待整理；UFS 综合练习与构建专题可先读 |
| CXX-11 并发与数据交接 | 数据交给其他线程后，谁保证寿命、互斥和可见性 | 待写；硬件内存序材料仅作补充 |

前面三课建立共同起点，最小编译练习从第一课开始。再补移动与容器、函数指针、布局，随后进入模板与生成代码；存储和并发按目标系统补充，不要求先学完所有高级主题。

## 进入 Piccolo 之前

公共基础先覆盖对象与所有权、虚调用、容器、模板/宏和基本构建。随后在 [Piccolo 专项入口]({{< relref "reading/piccolo-engine.md" >}}) 补三件具体事情：

- 从现有三篇 C++ 前置认识 Piccolo 的持有关系、组件/RHI 接口和反射生成协议。
- 从 [构建与调试基线]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}}) 核对源码、生成文件、配置与程序。
- **待补专项**：容器擦除与回调寿命、状态交接与实际线程边界；有了通用规则后，再沿项目具体调用点验证。

之后进入架构、启动、一帧、对象恢复与渲染主线。图形知识继续走 Piccolo 的渲染前置。

## 进入 Unreal 之前

公共基础先覆盖对象、容器、回调、模板与构建，再从 [Unreal 专项入口]({{< relref "reading/unreal-engine.md" >}}) 补引擎自己的约定。

- 已有 [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})、[旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}})、对象/反射与 GC 专题，可以衔接最小工程和 UObject 生命周期。
- **待补专项**：普通对象与 UObject 指针的使用边界、TArray/TMap 等容器与模板惯用法、Delegate 与 Lambda 的绑定/取消，以及任务线程中的数据寿命。

重点是重新确认该项目怎样创建、保存和结束对象，不能仅按名字把引擎类型与标准库类型一一替换。

## 进入 HybridCLR 之前

公共基础优先补函数指针、布局与字节访问、存储/对象寿命和原生调试。规范与运行时知识另走 [ECMA-335]({{< relref "engine-toolchain/ecma335-series-index.md" >}}) 和 [IL2CPP 运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。

在 [HybridCLR 专项入口]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) 复用现有 ABI、解释器与 GC 桥接，再读 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。

**待补专项**是逐句读懂方法指针、参数槽、返回值、解释帧和 metadata 索引怎样连接；已有机制解释还不能代替这组 C++ 跟读练习。

## 进入 IL2CPP 之前

公共基础优先补对象、间接调用、布局、存储和编译链接，同时建立 ECMA-335 的托管语义。再从 [IL2CPP 专项入口]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) 进入产物地图和核心机制。

**待补专项**包括：同一小程序的 C#、IL、生成代码对应阅读；托管对象与原生存储的边界；metadata、代码注册与方法分派的接线；C++ 模板和托管泛型分别在哪一层发挥作用。公共课解释语言，专项课解释 runtime 如何用这些语言机制实现自己的规则。

## 接下来先补什么

1. 值与访问的第一课已写；下一篇整理所有权与 RAII，再补类与虚调用、编译链接与符号。第一课已经提供最小编译练习。
2. 新增函数指针与回调、布局与字节访问、移动与容器失效；随后补各路线对应的逐句跟读。
3. 整理模板、宏与生成代码，再补存储与对象寿命，并衔接 UE 的容器、Delegate 与生成链专项。
4. 补并发与数据交接，再进入各项目的任务、线程与运行时协作。

现有 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})、[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})、[UE GC]({{< relref "system-design/ds-23-unreal-gc.md" >}}) 和 [硬件多核与内存序]({{< relref "performance/hardware-cpu-f05-multicore-false-sharing.md" >}}) 继续承担自己的专题；公共课补齐进入它们之前的语言基础。

**公共课回答 C++ 的规则，专项前置回答项目的约定，源码主线再追踪一次真实的执行与状态变化。**
