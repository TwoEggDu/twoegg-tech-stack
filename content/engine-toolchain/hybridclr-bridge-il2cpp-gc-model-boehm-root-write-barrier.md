---
title: "HybridCLR 桥接篇｜IL2CPP GC 模型：BoehmGC、GC root、write barrier 与解释器的关系"
date: "2026-04-13"
lastmod: "2026-10-25"
description: "沿固定 v6.1.0 源码理解解释器活跃栈的动态根、引用写屏障，以及托管对象与 metadata 的不同生命周期。"
weight: 62
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "GC"
  - "Memory"
series: "HybridCLR"
hybridclr_version: "v6.1.0 (5567f367)"
---

AOT 代码把对象交给热更方法后，调用线程并没有进入另一座独立的托管堆。解释器继续使用 IL2CPP 的对象体系，却把参数、局部值和执行状态放进自己的数据结构里。GC 要怎样发现这些引用，写入对象字段时又怎样保持跟踪正确？

这是桥接篇 F。收集器、内存指标和终结语义由 [IL2CPP D6]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 与 [ECMA GC 前置]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})承担；本篇聚焦解释器接入。

> 源码基线：HybridCLR v6.1.0，commit `5567f367502d916c6742873d8ed2ab13d0f6900a`；配套 il2cpp_plus v2022-6.1.0，commit `ce2e34097e099e458e14192bed96c28bc761392b`。以下是静态源码核对，未包含 Player 压力测试；不把这个历史样本当成所有版本的实现。

## 共享对象，额外维护执行状态

先区分三类东西：

| 内容 | 例子 | 生命周期由什么决定 |
|---|---|---|
| 托管对象 | 热更类实例、数组、装箱对象、委托 | 对 GC 可见的引用关系与收集过程 |
| 线程执行状态 | MachineState、解释器栈和帧数组 | 线程上的解释执行需求与 runtime 清理路径 |
| 程序与方法数据 | image、类型描述、转换后的指令缓存 | 装载、缓存及具体产品的卸载机制 |

一个线程暂时没有解释器帧，不代表它的栈容量已释放；一个热更对象被收集，也不代表该程序集的 metadata 可以回收。这两种混淆都会把正常缓存、生命周期遗漏和真正的泄漏混到一起。

## 根是可达性的起点，不是所有变量的清单

静态状态、活跃执行引用和显式强句柄是常见根来源。它们不是一份跨版本穷尽清单：静态字段可能为空、局部引用可能已不再活跃、弱句柄也不等同强根。

例如长期存在的 AOT 事件发布者持有委托，委托再指向热更订阅者，便形成一条保留链。解除订阅后是否还有其他根，要查快照；单次强制 GC 后对象还活着，并不能直接指向解释器错误。可运行对照见 [静态容器与事件生命周期实验]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})。

普通强 GCHandle 保留对象，Pinned 才增加固定地址要求。保守、非移动的收集器也不能替你管理句柄与回调的所有权：native 仍在使用句柄时不能释放，native 已停止使用后也不能永久遗留。

## MachineState 如何提供活跃根

这一版 `MachineState` 定义在 [interpreter/Engine.h](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/Engine.h)。可按下面的路径读，不必先通读解释器全部指令：

1. `InterpreterModule::GetCurrentThreadMachineState` 从 TLS 取当前线程状态，没有则创建。它不会提前为所有 OS 线程创建解释器。
2. `MachineState::InitEvalStack` 分配解释器栈，并把 **MachineState 实例与 GetGCRootData 回调**交给宿主的 `GarbageCollector::RegisterDynamicRoot`。
3. `GetGCRootData` 在栈存在且 `_stackTopIdx > 0` 时，报告从 `_stackBase` 开始、长度为 `_stackTopIdx * sizeof(StackObject)` 的区间；否则报告空区间。
4. 析构时，已分配的栈先取消动态根注册，再释放其 native 内存；帧和异常流数组也有对应清理。

这里注册的不是“永远扫描整个预分配栈容量”的固定区间。容量、活跃范围和是否已经释放是三个量。进入和退出解释器帧会影响活跃范围，而线程存活期间可以继续保留已分配容量。

宿主端在配套 `libil2cpp/gc/BoehmGC.cpp` 的动态根集合中保存回调，在根枚举时取得区间并交给收集器。`RegisterDynamicRoot` 是这套宿主集成接口的名字，不应写成 Boehm 原生提供了一个同名 `GC_register_dynamic_root` API。

这也说明为什么随意把托管指针存到 native 内存里并不充分：关键在于该区域是否被正确枚举、引用是否仍在有效范围、所在线程是否正确接入。线程创建与退出的完整链路见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

## 根枚举与写屏障解决不同时间段的问题

根枚举告诉 GC 从哪里开始追踪；写屏障帮助它跟踪引用关系的变化。把解释器栈登记成根，并不意味着解释器对任意对象、静态字段或数组的写入都可以跳过宿主协议。

以“已扫描的容器随后获得一个新引用”为例，增量收集的不同片段与业务执行交错时，需要有机制让相关变化被重新考虑。这个例子解释的是协议需求，不是每次漏 barrier 都会立即回收目标的确定性重现步骤。

v6.1.0 可沿两层入口查看：

- [Interpreter_Execute.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/Interpreter_Execute.cpp)：字段、数组、间接写入和含引用值类型复制等执行分支。
- [Il2CppCompatibleDef.h](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/Il2CppCompatibleDef.h)：`HYBRIDCLR_SET_WRITE_BARRIER` 的兼容封装及条件。

原始 CIL 先被转换成内部指令，再由解释器分派执行。是否选择某个带 barrier 的指令变体，需要同时看转换阶段、类型中是否含引用，以及执行端和宿主的配置；不能仅凭指令名称判断所有路径都已覆盖。

也不要反向写成“关闭增量后可以自行删掉 barrier”。运行时拥有不同配置下的实现选择，业务和插件应遵守其接口契约。栈上的临时槽位与堆上引用字段可能使用不同跟踪方式，但“只要注册成根就永远不需要写入处理”同样不是通用规则。

GC write barrier 不使共享 `List` 的并发操作变安全。跨线程发布和互斥另看 [线程内存模型]({{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}})。

## 线程多了，为什么 native 内存也会涨

在这套实现中，进入解释执行的线程可各自拥有 MachineState；解释器栈和帧数组属于 native 状态。频繁新建线程和复用线程池，表现出的分配与清理节奏可能不同。不要把“一次热更调用返回”视作“当前线程所有解释器内存已释放”。

验证时分别记录：进入过解释器的线程、仍活跃的帧、栈容量、动态根注册／注销，以及线程退出清理是否发生。具体退出入口和断点表见线程接入篇。仅观察托管堆大小无法回答这组问题。

## GC 完成不是卸载程序集的充分条件

旧稿把卸载压成“对象不可达 → 完整 GC → 释放 metadata”，省掉了真正困难的执行和所有权边界。至少还需要确认：

- 不再创建指向旧模块的新任务、订阅与回调；在途执行已退出。
- 旧模块的委托、句柄、反射对象以及相关缓存已按产品协议处理。
- 运行时内部描述符和跨模块依赖不再访问将被释放的数据。
- 使用的是明确提供卸载／热重载能力的产品与版本，并完成它要求的步骤。

GC 只回答其对象体系中的可达性与回收问题，不会自动证明 native 缓存和执行入口可以失效。社区版普通动态加载也不能因为手工收集一次就获得任意卸载能力。产品能力继续看 [程序集热重载专题]({{< relref "engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md" >}})，其中的商业实现描述需保留公开事实与推断的区分。

## 验证时分别记录什么

| 场景 | 关键观察 | 不能据此推出什么 |
|---|---|---|
| AOT 静态事件持有热更实例 | 委托目标与根路径，退订前后对象数 | 单次 GC 证明程序集已卸载 |
| 工作线程反复调用热更方法 | MachineState 复用、帧退出、线程清理 | 线程池不退出就是状态泄漏 |
| 开启增量后修改引用 | 相同输入下的完整性检查、GC 模式和崩溃证据 | 跑几轮不崩就证明 barrier 完整 |
| 多轮加载后内存增长 | 托管对象、native metadata、转换缓存分别计数 | 全部增长都来自 GC |

### 已补入的 Player 证据

2026-10-25，沿用本机自定义 Unity 2022.3.62f3 与固定 HybridCLR v9 配套组合，完成两组独立 Player 对照。它们补充运行观察，不替换前文 v6.1.0 的源码基线。

- [IL2CPP GC 篇]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})：三个独立进程中，AOT 静态数组／实例委托各保留 16 个热更对象，解除对应引用后，目标弱引用在观察窗口内全部失效。记录 GCMode=Enabled、isIncremental=True。
- [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})：两托管工作线程各执行 Add 两次；原生断点观察到不同且同时存活的 MachineState，共享同一方法缓存，以及各线程退出时的 FREE、析构、动态根注销入口。

这次看到的是对象存活对照和线程清理调用链，没有采集活跃根扫描区间，也没有通过增量引用压力负载验证所有写屏障路径。对象被收集、状态进入清理和 native 分配量下降，仍须分别举证。本轮自有样本与运行证据对应实验仓库 [固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，入口为[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/projects/lifetime-threads)和[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)。

### 新增：原生回调活跃引用与有界 GC 压力

同一固定v9环境中，新GC回调实验（build_id `148235df-910b-441f-94b2-b7348eb02d8c`）把两种持有路径分开验证：Normal GCHandle保留热更Context；以及不创建Context句柄、仅由活跃热更回调继续使用的局部Node。各三个独立Player在回调等待期间显式收集3次，放行后对象内容正确；退出并解除对应持有后，短弱引用均在本次观察窗口失效。

两组聚焦PDB断点将Full入口定位在回调活跃、状态根注册后、scope退出前；handle的FreeContext在Join后，stack的token为0且没有FreeContext。它支持本样本活跃引用跨收集可用，未通过根快照或GetGCRootData返回区间证明唯一根来源。

另三轮压力Player各完成1024回调、8 MiB payload分配及64槽对象图修改，数据检查通过。128次增量请求均返回false，同时负载窗口内GC.CollectionCount(0)差值256。它没有直接展示同一标记周期跨片段仍在进行的过程，不足以证明所有解释指令的写屏障正确。分配量也不是托管堆峰值、native释放量或性能指标。

详细数据和采样口径见 [IL2CPP GC篇]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})，句柄、回调与Join的顺序见 [线程篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮样本与摘要已固定在实验仓库提交 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，见[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)。

<!-- DATA-TODO: 持续跨片段引用修改、完整动态根路径归因与独立性能仍待补；实际返回区间和对象快照见后续实验。 -->

### 新增：分配容量、报告根区间与对象快照分别取证

后续 `gc-roots` 实验沿用固定自定义Editor／v9组合，build_id `993e5b9e-1f71-4949-b339-39ff10d359b1`。实际GC调用链中，handle与stack三轮各采3次返回，均为stackTop36、288字节，目标引用分别位于偏移24和48／264。空闲托管线程的解释方法已返回，尽管backing栈仍分配131072槽，frameTop／stackTop均为0，三轮各3次真实返回均为null／0。

这把“分配了1 MiB解释器栈”与“当前报告288字节活跃根”分开，也把根的所属线程与执行收集的线程分开。记录包括真实返回缓冲、连续槽位和注册／注销生命周期，不只引用源码公式；仍没有证明唯一根来源或标记器逐字遍历。

另外21份对象快照解析到强句柄目标→Context→64槽数组的路径，Free后热更强句柄目标消失；官方Memory Profiler1.1.0读取器对189组缓冲的数量与哈希交叉校验通过。所有快照ManagedStacks条目为0，未提供解释器动态根边。**对象快照中的可见引用路径、原生分配所有权的root字段和解释器提供的动态根区间，需要分别解释。**

详细矩阵见 [IL2CPP GC篇]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})，活跃／空闲／退出的顺序见 [线程篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮已推送实验仓库固定提交 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，可查看[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)；前轮 `449a994` 只作为基线。

**解释器接入 GC，必须同时维护可见的活跃引用、正确的写入协议和有所有者的 native 状态。**

## 系列位置

- 前置：[IL2CPP GC 集成]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})
- 继续：[线程接入与退出清理]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})
- 返回：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})
