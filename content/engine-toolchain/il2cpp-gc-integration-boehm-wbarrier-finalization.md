---
title: "IL2CPP 实现分析｜GC 集成：BoehmGC 的接入层、write barrier 与 finalization"
slug: "il2cpp-gc-integration-boehm-wbarrier-finalization"
date: "2026-04-14"
lastmod: "2026-10-25"
description: "区分 IL2CPP 的对象分配、引用保留、增量 GC 与资源释放，并用 Player 观察方案定位内存增长。"
weight: 55
featured: false
tags:
  - IL2CPP
  - Unity
  - GC
  - BoehmGC
  - Memory
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp"
---

热更模块退出了，Profiler 里的内存却没有明显下降。此时直接增加一次 `GC.Collect()`，可能暂时改变曲线，也可能完全无效。首先要弄清：对象是否仍被引用，收集器是否回收了对象，托管堆是否保留容量，以及增长是否来自 native metadata 或引擎资源。

这是 IL2CPP 核心机制 D6。前置是 [对象布局、GC 契约与终结语义]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})；解释器的根和写屏障落地放在 [HybridCLR GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})。

> 版本边界：Unity 行为以 2022.3 文档为基线；HybridCLR 接入以社区版 v6.1.0 及配套 il2cpp_plus v2022-6.1.0 为源码样本。下文 Player 实验是验证方案，未宣称已在该组合上实测。

## 先拆开四个时刻

```text
分配对象 → 建立和修改引用 → 对象不再可达 → 收集器择机回收
                                             ↓
                                 堆空间可能留给以后分配
```

业务退出、对象不可达、GC 完成和操作系统看到内存下降，并不是同一个事件。静态容器、事件委托、正在运行的任务以及 native 侧的强句柄，都可能让业务已放弃的对象继续存活。

`new` 也不意味着每次都新建一个独立托管对象：未装箱的 struct 可以内嵌在数组或类实例中；数组和所属实例仍有自己的分配。判断分配成本，应看实际存储和调用路径，而不是只搜索 `new` 或 `struct`。

## Boehm 是这里的实现选择，不是 AOT 的必然结论

Unity 2022.3 的 [.NET 概览](https://docs.unity3d.com/2022.3/Documentation/Manual/overview-of-dot-net-in-unity.html)说明 Mono 与 IL2CPP 使用 Boehm 收集器。讨论这个版本的 IL2CPP，应以它的集成行为为准，不能套用桌面 CoreCLR 的分代和压缩经验。

保守扫描会把某些看起来像堆地址的值也视作可能的引用，因此可能额外保留对象；不能据此保证某个对象在一次收集后消失，也不能把所有长期增长都归因于这种保留。

旧稿把原因写成“C++ codegen 无法提供精确栈布局，所以 IL2CPP 只能用 Boehm”，结论过强。普通 C++ 编译产物不会自动满足托管精确根枚举的要求，但 AOT 系统可以设计专门的根跟踪和编译器协作。**是否 AOT，与收集器是否精确，是两个设计维度。** 本篇解释已有集成，不推导其他架构不可能实现。

## 分配、根与写入协议各管一段

从运行时职责看，可以沿三条线阅读：

| 链路 | 需要保证什么 | 排查时看什么 |
|---|---|---|
| 类型与分配 | 大小、引用布局、对象初始化相互匹配 | 类型、数组元素、装箱和真实分配栈 |
| 根与可达性 | 仍在使用的对象对 GC 可见 | 静态状态、活跃线程、句柄及扩展根 |
| 引用修改 | 写入满足当前收集模式的跟踪协议 | 字段、数组、含引用值类型复制及互操作 |

源码定位可从 `libil2cpp/vm/Object.cpp`、`gc/GarbageCollector.h`、`gc/BoehmGC.cpp` 和 `gc/WriteBarrier.h` 开始；路径与接口随版本变化。这里保留职责和入口，不把简化伪代码写成 Unity 实现源码。

普通 `GCHandleType.Normal` 保持强引用，但不提供 pinned 地址契约；Weak 允许目标被回收，Pinned 则额外涉及固定地址。句柄转成 `IntPtr` 后通常是传给 native 的不透明标识，不能直接当作对象数据指针。用完需要按所有权释放。具体语义见 [GCHandleType](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.gchandletype?view=net-8.0)。

外部线程进入托管世界也属于根管理边界。只拿到一个函数地址，还不能证明线程已向 runtime 和 GC 正确登记，详见 [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

## 增量 GC 改变工作分布，不承诺没有停顿

Unity 2022.3 的[增量 GC 文档](https://docs.unity3d.com/2022.3/Documentation/Manual/performance-incremental-garbage-collection.html)描述了把收集工作分散到多个帧的方式，并明确其非压缩特征。增量不等于另起后台线程并发完成全部收集，也不等于总耗时一定更少。

应用在相邻收集片段之间仍会修改引用关系。写屏障用于帮助收集器追踪这些修改；它不是 `lock`，也不是保证业务跨线程可见性的内存屏障。引用修改过多可能增加重新扫描工作，因此开启增量后仍需测量。

这里不能给出“每帧固定 1～3 ms”或“开启就无感”的承诺。应记录目标设备、实际运行模式、分配速率、存活对象规模、引用修改模式与帧时间分布。Unity 版本和平台也会限制可用模式；不要把桌面 Player 的设置直接推广到 WebGL。

## Finalization 不能承担业务退出协议

终结处理可能延后对象内存回收，执行时机也不由业务调用点决定。不要依赖终结器完成退订、停止线程、等待 SDK 回调或销毁必须在主线程操作的引擎对象；这些需要明确的生命周期入口。

`Dispose`、释放 GCHandle、退订事件和 `UnityEngine.Object.Destroy` 的作用各不相同。Dispose 并不会让 GC 立刻回收对象；Destroy 也不等于清掉所有托管引用。先显式关闭自己拥有的资源，再用引用快照检查遗漏。终结与资源释放的契约见 [ECMA 前置篇]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})。

## 用同一张表区分内存增长

| 观察对象 | 常见来源 | 应收集的证据 |
|---|---|---|
| 托管存活对象 | 静态集合、委托、闭包、任务、强句柄 | 对象数、引用链及注册／退订记录 |
| 托管堆保留容量 | 历史峰值、空闲块与分配需求 | used 与 reserved 的变化，能否复用空闲空间 |
| runtime native 内存 | metadata、转换后的指令、解释器线程栈 | 加载／首次转换次数、线程状态数量、native 分配采样 |
| 引擎 native 资源 | 纹理、网格、缓冲和插件资源 | 引擎对象快照、句柄数与释放调用 |
| 进程总体内存 | 上述内存加代码、映射和其他系统开销 | OS 指标及工具口径，不能直接等同托管存活量 |

这些是诊断分类，不是一组可以直接相加的工具计数；不同 Profiler 指标可能重叠。GC 回收对象后，分配器仍可能保留空间供以后复用。“没有马上归还 OS”和“对象泄漏”必须分别证明。

## 动手验证：在 Player 中区分保留与释放

先运行 [ECMA GC 篇的静态容器与事件实验]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}})，理解强引用链，再移到独立 IL2CPP Player。若加入热更对象，固定 HybridCLR、DLL 和生成物版本，给 AOT 持有者与热更订阅者分别命名，方便快照定位。

1. **建立基线**：完成加载和一次预热，记录设备、Unity patch、架构、构建选项和增量模式。记录对象数、托管 used／reserved 与 native 指标。
2. **建立保留链**：AOT 的长期存活发布者订阅热更对象的实例方法，另设一组静态容器保留对象。主动丢弃业务入口，观察快照中的两条根路径。
3. **退出业务**：停止新任务与回调，等待在途工作结束；分别退订、移除容器元素、释放拥有的句柄。留一组故意未退订的对照，但保证测试结束后清理。
4. **观察回收**：在隔离实验里触发收集并取后续快照，检查目标对象与根路径。清理托管对象后，再观察 metadata、解释器状态和堆容量是否仍保留。
5. **重复多轮**：区分首次加载／转换形成的稳定增长与每轮都增加的存活对象。无法回收时先找引用链和在途执行，不用单次 RSS 变化作结论。

测试 GC 模式性能时，应单独重建两份相同业务负载的 Player，比较分配、GC 采样和帧时间分布；不要把上面为观察生命周期而强制收集的运行当成帧耗时基准。

<!-- DATA-TODO: 固定 Unity 2022.3 patch、HybridCLR v6.1.0 配套源码与目标设备，补 AOT 持有热更对象的引用快照、退出记录及增量开关对照。 -->

## 接到 HybridCLR

解释执行共享 IL2CPP 的托管对象体系，但解释器自己的 native 状态需要额外接入。GC 桥接篇继续解释活跃解释器栈如何被枚举、引用写入如何通知宿主；线程接入篇解释线程退出如何清理这些状态。程序集 metadata 的释放则是另一层能力，不能由一次 GC 完成来推定。

**先证明谁还持有对象，再区分对象回收、堆容量和 native 资源，才能解释内存曲线。**

## 系列位置

- 上一篇：[泛型代码生成]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}})
- 下一篇：[ECMA-335 覆盖度]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}})
- 返回：[IL2CPP 索引]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})
