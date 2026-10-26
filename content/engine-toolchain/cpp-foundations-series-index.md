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
series_reading_hint: "共同起点01→02→03→10；其余按访问/容器/回调、布局/模板/生成、对象寿命/并发的系统需要选读。"
tags: ["C++", "源码阅读", "Piccolo", "Unreal", "HybridCLR", "IL2CPP"]
weight: 490
---

读引擎源码时，能认出类名和函数名，还不一定能解释一次赋值是否复制对象、一个回调何时失效，或者跳到的声明究竟由谁实现。这些问题会在不同项目中反复出现，适合先建立一套共用的 C++ 基础。

本专栏面向已经会编程、主要有 C#／Unity 经验的读者。先用独立小程序理解语言和工程规则，再进入各项目的对象系统与运行时约定。

**阅读分三层：公共 C++ 基础 → 各路线专项前置 → 源码主线。** 专项前置会重新说明通用规则在具体项目中怎样使用，并补充该项目特有的知识。

> **当前进度**：公共目录的 11 个正文单元 CXX-01～11 均已成稿；专项前置、引擎构建与运行状态仍按各路线分别推进。原有四篇材料继续保留在原系列，承担综合练习和项目案例；复用链接不增加公共正文数量。各篇的小程序、引擎运行和发布状态以正文记录为准。

## 现在从哪里开始

不必先读完十一篇。先建立共同起点，再按将要阅读的系统补课。

| 阶段 | 阅读顺序与问题 |
|---|---|
| 共同起点 | [01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}}) → [02 所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) → [03 类与调用]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}}) → [10 构建与符号]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}})；从第一课即可做最小编译 |
| 访问、容器、回调 | 按需读04、05、06：元素地址、捕获寿命与字节访问 |
| 布局、模板、生成 | 06 → 07 → 08，结合10理解声明与生产/消费 |
| 对象寿命与并发 | 09依赖02/06；11依赖02/04/05，进入存储或任务系统时补读 |

| 路线 | 首次进入必读 | 遇到对应系统再补 |
|---|---|---|
| Piccolo | 共同起点，随后对象/容器用04、PCPP-1 | 回调用05/PCPP-2；反射生成用07/08；交接用06/11/PCPP-3 |
| Unreal | 共同起点，UCPP-1对象、04/07与UCPP-2 | Delegate用05；UHT/UBT用08；任务用11 |
| HybridCLR | 共同起点，05/06与CLI/IL2CPP地图 | 存储用09；模板07、线程11随具体入口补读 |
| IL2CPP | 共同起点，CLI与产物地图 | 生成调用用05；对象存储用06/09；泛型07、线程11按需 |


| 用途 | 已有文章 | 先掌握什么 |
|---|---|---|
| 综合练习 | [从 C# 到 C++：对象由谁持有，代码怎样变成程序]({{< relref "engine-toolchain/ufs-02-csharp-to-cpp-ownership.md" >}}) | 按需复做 RAII、基本 move、vector 失效，以及多文件编译、链接与调试 |
| 所有权的项目应用 | [智能指针与对象生命周期]({{< relref "engine-toolchain/piccolo-cpp-01-smart-pointers-lifetime.md" >}}) | 把公共课的拥有模型放回 Piccolo；追踪业务清理、容器移除和析构 |
| 类与调用的项目应用 | [类、继承与虚函数]({{< relref "engine-toolchain/piccolo-cpp-02-classes-inheritance-virtual-functions.md" >}}) | 把公共课模型放回 Piccolo 的组件与 RHI；沿声明、创建点、调用条件和覆盖定义追实现 |
| 模板与生成的项目应用 | [模板、宏与反射代码生成]({{< relref "engine-toolchain/piccolo-cpp-03-templates-macros-reflection.md" >}}) | 类型参数、名字字符串、宏、外部生成文件和运行时注册分别发生在哪里 |

编译或断点遇到困难时，补读 [构建与调试前置]({{< relref "engine-toolchain/build-debug-prereqs-series-index.md" >}})。普通 C++ 示例通过，只说明相应示例的结果；具体项目还需要自己的构建与调试基线。

<a id="chapters"></a>

## 公共课目录与验证范围

既定正文均已有实际文件。下面列的是各篇的知识与独立验证范围，不能由公共课成稿推断四个项目的专项和运行已完成。编号用 CXX，与 Piccolo 原有 CPP 编号分开。

| 单元 | 要解决的问题 | 状态 |
|---|---|---|
| [CXX-01 值与访问]({{< relref "engine-toolchain/cpp-foundations-01-values-references-signatures.md" >}}) | 传参、const、auto、指针和引用究竟表达什么 | 已写，含完整程序、编译失败练习和验证记录 |
| [CXX-02 生命周期与所有权]({{< relref "engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md" >}}) | 作用域、RAII、独占/共享/弱观察怎样确定释放责任 | 已写，含资源清理、保活、断环与复制失败练习 |
| [CXX-03 类与调用]({{< relref "engine-toolchain/cpp-foundations-03-classes-dispatch-destruction.md" >}}) | 重载、覆盖、切片和析构怎样影响实际执行 | 已写，含调用选择、值切片、析构链及编译失败练习 |
| [CXX-04 移动与容器]({{< relref "engine-toolchain/cpp-foundations-04-move-containers-invalidation.md" >}}) | 复制、移动、扩容、删除之后，地址和迭代器还能不能用 | 已写，含操作选择、noexcept、访问失效与身份重查练习 |
| [CXX-05 函数指针与回调]({{< relref "engine-toolchain/cpp-foundations-05-callables-callback-lifetime.md" >}}) | 行为怎样被保存；Lambda、捕获和 this 的寿命怎样约束调用 | 已写，含调用形式、捕获复制、取消边界及编译失败练习 |
| [CXX-06 布局与字节访问]({{< relref "engine-toolchain/cpp-foundations-06-layout-bytes-casts.md" >}}) | 大小、对齐、偏移、指针运算和类型转换怎样连接内存与值 | 已写，含结构观察、表示复制、小端解码及失败边界 |
| [CXX-07 模板阅读]({{< relref "engine-toolchain/cpp-foundations-07-template-reading.md" >}}) | 推导、特化、类型工具和转发怎样选择实现 | 已写，含候选选择、类型检测、转发与显式实例化练习 |
| [CXX-08 宏与代码生成]({{< relref "engine-toolchain/cpp-foundations-08-macros-code-generation.md" >}}) | 一段代码由谁产生，在哪个阶段被使用 | 已写，含宏/条件程序、实际生成、构建依赖与显式注册实验 |
| [CXX-09 存储与对象]({{< relref "engine-toolchain/cpp-foundations-09-storage-object-lifetime-ub.md" >}}) | 分配、构造、析构、释放和未定义行为的边界在哪里 | 已写，含原位构造、异常清理、union 与对齐释放实验 |
| [CXX-10 编译链接与符号]({{< relref "engine-toolchain/cpp-foundations-10-build-link-symbols.md" >}}) | 头文件、翻译单元、库和正在运行的程序怎样对应 | 已写，含三文件工程、失败变体、静态库与 GDB 验证 |
| [CXX-11 并发与数据交接]({{< relref "engine-toolchain/cpp-foundations-11-concurrency-handoff.md" >}}) | 数据交给其他线程后，谁保证寿命、互斥和可见性 | 已写，含线程收尾、锁快照、关闭通道、原子发布与更新反例 |

前面三课建立共同起点，CXX-10 补齐多文件构建与调试，最小编译练习从第一课即可开始。CXX-04～09 已补移动与容器、回调、布局、模板、生成代码和存储/对象寿命，CXX-11 补齐公共并发与交接；这套起点不要求先学完所有高级主题。

## 进入 Piccolo 之前

公共基础先覆盖对象与所有权、虚调用、容器、模板/宏和基本构建。随后在 [Piccolo 专项入口]({{< relref "reading/piccolo-engine.md" >}}) 补充具体约定：

- 从现有三篇 C++ 前置认识 Piccolo 的持有关系、组件/RHI 接口和反射生成协议。
- 从 [构建与调试基线]({{< relref "engine-toolchain/piccolo-engine-01-build-debug-baseline.md" >}}) 核对源码、生成文件、配置与程序。
- 从 [对象容器修改后，持有者、借用与遍历怎样变化]({{< relref "engine-toolchain/piccolo-pcpp-01-object-containers-borrowing-iteration.md" >}}) 核对对象表擦除、局部保活与组件包装复制；先用 CXX-04 掌握公共规则，再核对项目的包装和删除协议。
- 从 [窗口与编辑器回调怎样绑定、调用和注销]({{< relref "engine-toolchain/piccolo-pcpp-02-window-editor-callbacks.md" >}}) 核对成员函数绑定、事件转接与捕获寿命；先读 CXX-05 公共规则，再核对项目的注册/退出事实。
- 从 [状态缓冲怎样交接，哪些地方真的跨线程]({{< relref "engine-toolchain/piccolo-pcpp-03-state-buffer-thread-boundaries.md" >}}) 核对交换槽角色、描述所有权和真实线程入口，包含独立交接程序；布局规则先读 CXX-06，并发与发布/关闭规则先读 CXX-11，再回到项目核对真实执行与复用条件。

之后进入架构、启动、一帧、对象恢复与渲染主线。图形知识继续走 Piccolo 的渲染前置。

## 进入 Unreal 之前

公共基础先覆盖对象、容器、回调、模板与构建，再从 [Unreal 专项入口]({{< relref "reading/unreal-engine.md" >}}) 补引擎自己的约定。

- 已有 [UE 工程地图]({{< relref "engine-toolchain/ufs-03a-project-build-map.md" >}})、[旋转 Actor]({{< relref "engine-toolchain/ufs-03b-first-rotating-actor.md" >}})、对象/反射与 GC 专题，可以衔接最小工程和 UObject 生命周期。
- **对象专项已写**：[UCPP-1：普通对象与 UObject 的边界]({{< relref "engine-toolchain/ucpp-01-uobject-ownership-boundaries.md" >}})，复用已有 Actor、对象和 GC 材料，补 Outer/强持有/弱观察与结束协议；完整自有 UE 样例及步骤已写，UE 编译、运行与 GC 对照待验证。
- **容器与模板专项已写**：[UCPP-2：容器、字符串与模板惯用法]({{< relref "engine-toolchain/ucpp-02-containers-strings-template-idioms.md" >}})，复用 CXX-04/07，核对引擎容器的访问失效、文本角色及 MoveTemp/Forward 约定；UE 编译和运行待验证。
- **回调专项已写**：[UCPP-3 Delegate与Lambda]({{< relref "engine-toolchain/ucpp-03-delegates-lambda-callback-lifetime.md" >}})，含订阅、延迟副本与重复清理；UE编译运行待验。随后读[UCPP-4生成与模块接线]({{< relref "engine-toolchain/ucpp-04-uht-ubt-generated-module-wiring.md" >}})和[UCPP-5任务交接]({{< relref "engine-toolchain/ucpp-05-task-thread-data-handoff.md" >}})，三篇均有完整自有代码，UE工程实测待验。

重点是重新确认该项目怎样创建、保存和结束对象，不能仅按名字把引擎类型与标准库类型一一替换。

## 进入 HybridCLR 之前

公共基础优先补函数指针、布局与字节访问、存储/对象寿命和原生调试。规范与运行时知识另走 [ECMA-335]({{< relref "engine-toolchain/ecma335-series-index.md" >}}) 和 [IL2CPP 运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})。

在 [HybridCLR 专项入口]({{< relref "engine-toolchain/hybridclr-series-index.md" >}}) 复用现有 ABI、解释器与 GC 桥接，再读 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。

已有三组训练已吸收入现有正文：[ABI桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})读方法指针、参数槽和返回值；[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})与源码起步读metadata、转换和解释帧；[共享线程专题]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})读对象、接入和退出。固定自定义Editor/v9已有首次/再次调用、对象保留、原生回调、活跃根和串行跨片段证据。其他ABI、完整根归因、并发/长期压力按具体问题补，不再安排HCPP独立文章。

## 进入 IL2CPP 之前

公共基础优先补对象、间接调用、布局、存储和编译链接，同时建立 ECMA-335 的托管语义。再从 [IL2CPP 专项入口]({{< relref "engine-toolchain/il2cpp-series-index.md" >}}) 进入产物地图和核心机制。

三项训练已分别嵌入[D2同一程序对应阅读]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md#icpp-1" >}})、[D3描述/对象/存储]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md#icpp-2" >}})、[D5分派/注册/泛型]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md#icpp-3" >}})。完整自有输入、实际桌面CIL和既有Player证据的对应已写；新样本Unity构建、stripped IL及native虚分派仍待验，不新增ICPP独立正文。

## 公共课之后继续什么

1. CXX-01～11 已写，共同起点已包括对象与调用、构建、容器与回调、布局、模板、生成代码、存储/对象寿命，以及并发与数据交接。
2. 专项逐句跟读按各路线推进，核对 UE 的容器、Delegate、生成链和任务，Piccolo 的实际交接，以及各 runtime 的对象、执行和线程约定。
3. 每条路线继续补自己的构建、断点、运行与退出证据；公共程序通过不替代项目验收。

现有 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})、[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})、[UE GC]({{< relref "system-design/ds-23-unreal-gc.md" >}}) 和 [硬件多核与内存序]({{< relref "performance/hardware-cpu-f05-multicore-false-sharing.md" >}}) 继续承担自己的专题；公共课补齐进入它们之前的语言基础。

**公共课回答 C++ 的规则，专项前置回答项目的约定，源码主线再追踪一次真实的执行与状态变化。**

## 源码语法短速查

先判断名字、类型和调用，再判断拥有与业务协议。以下只作阅读入口，细节回原课。

| 看到什么 | 先怎样读 | 回看 |
|---|---|---|
| `scope::Type`、`::name` | 限定作用域；可能是命名空间、类或全局名字 | CXX-01签名与别名 |
| `using P = T*`、`typedef` | 别名是整个类型的另一个名字，先展开再读const | CXX-01 |
| `Ctor(...) : member(...)` | 成员初始化发生在函数体之前，按声明顺序 | CXX-03 |
| `explicit Ctor` | 约束隐式转换，不等于禁止直接初始化 | CXX-02观察包装 |
| `= default` / `= delete` | 请求默认定义 / 明确禁止该函数参与使用 | CXX-03/04 |
| `*wrapper`、`wrapper->field` | 先查运算符成员，不能直接认作原生指针 | CXX-02观察包装 |
| `if (wrapper)` | 可用显式布尔转换；判真具体保证由包装定义 | CXX-02 |
| `enum class`、`Flags & Mask` | 枚举值、底层表示、位操作及判零分开看；作用域枚举不自动转int，项目是否定义位操作要查声明 | CXX-01类型；CXX-08条件与宏 |
| `typename T::value_type` | 依赖类型名先读作类型，具体定义到实例化再核对 | CXX-07依赖名 |

本轮新增自有样例集中在仓库 `examples/cpp-foundations/`，说明与正常/失败命令见该目录README；它只覆盖本轮补充，不声称既有全部练习已集中。
