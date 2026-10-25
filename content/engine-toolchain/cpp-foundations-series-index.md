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
series_reading_hint: "先读 CXX-01～03，再用 CXX-10 练习多文件构建与调试，再读 CXX-05～06 和 CXX-04 的回调、布局与容器；下一篇整理模板阅读。"
tags: ["C++", "源码阅读", "Piccolo", "Unreal", "HybridCLR", "IL2CPP"]
weight: 490
---

读引擎源码时，能认出类名和函数名，还不一定能解释一次赋值是否复制对象、一个回调何时失效，或者跳到的声明究竟由谁实现。这些问题会在不同项目中反复出现，适合先建立一套共用的 C++ 基础。

本专栏面向已经会编程、主要有 C#／Unity 经验的读者。先用独立小程序理解语言和工程规则，再进入各项目的对象系统与运行时约定。

**阅读分三层：公共 C++ 基础 → 各路线专项前置 → 源码主线。** 专项前置会重新说明通用规则在具体项目中怎样使用，并补充该项目特有的知识。

> **当前进度**：公共目录规划了 11 个正文单元，CXX-01～06、10 已写，其余 2 个待整理、2 个待写。原有四篇材料继续保留在原系列，承担综合练习和项目案例；复用链接不增加公共正文数量。各篇的小程序、引擎运行和发布状态以正文记录为准。

## 现在从哪里开始

先读 [CXX-01：一次传参到底复制、借用还是只读访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}})，分清对象与指针变量、const、auto 和借用寿命。再读 [CXX-02：对象与资源由谁结束]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}})，理解 RAII、拥有关系与引用环。接着读 [CXX-03：一次成员调用执行谁]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}})，用独立程序区分重载、覆盖、切片和虚析构。再读 [CXX-10：源文件怎样成为正在调试的程序]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}})，沿同一个多文件工程练习编译、链接、静态库与断点。接着读 [CXX-05：行为怎样被保存并稍后调用]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}})，区分函数指针、捕获和取消后的任务副本。再读 [CXX-06：从一段字节读到一个值]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}})，核对布局、类型访问和字节解码的前提。接着读 [CXX-04：复制、移动和容器修改以后，哪些访问还有效]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}})，把元素地址、拥有关系、索引和身份分开判断。随后进入下面的综合练习和项目案例，其余公共课继续补齐。

| 用途 | 已有文章 | 先掌握什么 |
|---|---|---|
| 综合练习 | [从 C# 到 C++：对象由谁持有，代码怎样变成程序]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) | 按需复做 RAII、基本 move、vector 失效，以及多文件编译、链接与调试 |
| 所有权的项目应用 | [智能指针与对象生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}}) | 把公共课的拥有模型放回 Piccolo；追踪业务清理、容器移除和析构 |
| 类与调用的项目应用 | [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}}) | 把公共课模型放回 Piccolo 的组件与 RHI；沿声明、创建点、调用条件和覆盖定义追实现 |
| 模板与生成 | [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}}) | 类型参数、名字字符串、宏、外部生成文件和运行时注册分别发生在哪里 |

编译或断点遇到困难时，补读 [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})。普通 C++ 示例通过，只说明相应示例的结果；具体项目还需要自己的构建与调试基线。

<a id="chapters"></a>

## 公共课目录与补写状态

“待整理”表示主要材料已有，但仍需整理为独立公共课；“待写”表示尚缺完整单元，现有零散说明不能替代它。编号用 CXX，与 Piccolo 原有 CPP 编号分开。

| 单元 | 要解决的问题 | 状态 |
|---|---|---|
| [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}}) | 传参、const、auto、指针和引用究竟表达什么 | 已写，含完整程序、编译失败练习和验证记录 |
| [CXX-02 生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) | 作用域、RAII、独占/共享/弱观察怎样确定释放责任 | 已写，含资源清理、保活、断环与复制失败练习 |
| [CXX-03 类与调用]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}}) | 重载、覆盖、切片和析构怎样影响实际执行 | 已写，含调用选择、值切片、析构链及编译失败练习 |
| [CXX-04 移动与容器]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}}) | 复制、移动、扩容、删除之后，地址和迭代器还能不能用 | 已写，含操作选择、noexcept、访问失效与身份重查练习 |
| [CXX-05 函数指针与回调]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}}) | 行为怎样被保存；Lambda、捕获和 this 的寿命怎样约束调用 | 已写，含调用形式、捕获复制、取消边界及编译失败练习 |
| [CXX-06 布局与字节访问]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}}) | 大小、对齐、偏移、指针运算和类型转换怎样连接内存与值 | 已写，含结构观察、表示复制、小端解码及失败边界 |
| CXX-07 模板阅读 | 推导、特化、类型工具和转发怎样选择实现 | 按补写顺序下一篇待整理；现有模板篇覆盖基础部分 |
| CXX-08 宏与代码生成 | 一段代码由谁产生，在哪个阶段被使用 | 待整理，先读现有模板与生成篇 |
| CXX-09 存储与对象 | 分配、构造、析构、释放和未定义行为的边界在哪里 | 待写 |
| [CXX-10 编译链接与符号]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}}) | 头文件、翻译单元、库和正在运行的程序怎样对应 | 已写，含三文件工程、失败变体、静态库与 GDB 验证 |
| CXX-11 并发与数据交接 | 数据交给其他线程后，谁保证寿命、互斥和可见性 | 待写；硬件内存序材料仅作补充 |

前面三课建立共同起点，CXX-10 补齐多文件构建与调试，最小编译练习从第一课即可开始。CXX-04～06 已补移动与容器、回调与布局，接着整理模板与生成代码；存储和并发按目标系统补充，不要求先学完所有高级主题。

## 进入 Piccolo 之前

公共基础先覆盖对象与所有权、虚调用、容器、模板/宏和基本构建。随后在 [Piccolo 专项入口]({{< relref "reading/piccolo-engine.md" >}}) 补充具体约定：

- 从现有三篇 C++ 前置认识 Piccolo 的持有关系、组件/RHI 接口和反射生成协议。
- 从 [构建与调试基线]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}}) 核对源码、生成文件、配置与程序。
- 从 [对象容器修改后，持有者、借用与遍历怎样变化]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}}) 核对对象表擦除、局部保活与组件包装复制；先用 CXX-04 掌握公共规则，再核对项目的包装和删除协议。
- 从 [窗口与编辑器回调怎样绑定、调用和注销]({{< relref "engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md" >}}) 核对成员函数绑定、事件转接与捕获寿命；先读 CXX-05 公共规则，再核对项目的注册/退出事实。
- 从 [状态缓冲怎样交接，哪些地方真的跨线程]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}}) 核对交换槽角色、描述所有权和真实线程入口，包含独立交接程序；布局规则先读 CXX-06，公共 CXX-11 并发完整单元仍待写。

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

1. CXX-01～06、10 已写，共同起点已包括访问、生命周期、成员调用、构建、回调、布局，以及复制移动与容器失效。
2. 下一篇整理 CXX-07 模板阅读，先复用现有模板材料，再补齐推导、特化、类型工具与转发；专项逐句跟读继续按各路线推进。
3. 接着整理 CXX-08 宏与生成代码，再补 CXX-09 存储与对象寿命，并衔接 UE 的容器、Delegate 与生成链专项。
4. 补并发与数据交接，再进入各项目的任务、线程与运行时协作。

现有 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})、[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})、[UE GC]({{< relref "system-design/ds-23-unreal-gc.md" >}}) 和 [硬件多核与内存序]({{< relref "performance/hardware-cpu-f05-multicore-false-sharing.md" >}}) 继续承担自己的专题；公共课补齐进入它们之前的语言基础。

**公共课回答 C++ 的规则，专项前置回答项目的约定，源码主线再追踪一次真实的执行与状态变化。**
