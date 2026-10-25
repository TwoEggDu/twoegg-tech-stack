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

> 版本边界：Unity 行为以 2022.3 文档为基线；HybridCLR 接入以社区版 v6.1.0 及配套 il2cpp_plus v2022-6.1.0 为源码样本。文末新增的 Player 实测使用本机自定义 Unity 2022.3.62f3 与 v9 配套组合；它不构成 v6.1.0 的实测结果。

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

### 已完成：热更对象的保留与释放对照

2026-10-25，在 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab) 新增独立 `projects/lifetime-threads` 工程。环境沿用已验证组合：本机自定义 Unity `2022.3.62f3_96770f904ca7`、HybridCLR package v9.0.3 / runtime v9.0.1、il2cpp_plus v2022.3.x-9.0.1；Windows x64、IL2CPP Development、C++ Debug、Minimal stripping。依赖提交仍由根目录 `lab-lock.json` 固定。

这轮把 AOT 的持有者与热更对象分开：`RetentionHub` 保存静态数组、事件式委托列表与短弱引用，`LifetimeHot.Target` 位于动态加载的 HotUpdate.dll，不含终结器。构建后确认 HotUpdate 没有编入 AOT 程序集或代码注册。三个组各创建 16 个对象：无强引用、静态数组持有、实例委托订阅持有。

创建工作在短寿命托管线程中完成，等待该线程退出后再观察，避免创建线程的活跃栈继续保留对象。每阶段跨帧执行三次 `GC.Collect()` / `GC.WaitForPendingFinalizers()`，再读取 `WeakReference.IsAlive`。运行日志记录 `GCMode=Enabled`、`isIncremental=True`。无调试器启动三个独立 Player，结果完全一致，均正常退出：

| 观察阶段 | 无强引用组 | 静态数组组 | 事件订阅组 |
|---|---:|---:|---:|
| held：尚未解除持有 | 0 / 16 | 16 / 16 | 16 / 16 |
| static-cleared：清空静态数组 | 0 / 16 | 0 / 16 | 16 / 16 |
| all-cleared：再清空委托列表 | 0 / 16 | 0 / 16 | 0 / 16 |

表中是弱引用仍存活的目标数。委托列表由这个封闭样本独占，因此可整体清空；业务代码应解除自己拥有的订阅，不能借此清掉其他模块的订阅。本轮不触发事件，也不靠终结器计数。

这组对照支持一个具体结论：AOT 静态数组和实例委托能保留本样本的热更对象，解除它们后，目标弱引用在本次观察窗口内失效。校验器要求持有组不能提前丢失，却允许已解除组暂时仍有存活对象并如实记录；它不会把“每次必须立即归零”写成运行时保证。

复现时在实验仓库依次执行 `python tools/lifetime_lab.py build --editor <Unity.exe路径>`、`python tools/lifetime_lab.py run`。构建、运行记录写入独立的 `artifacts/lifetime/`，构建前后和运行前均检查源码清单与哈希。本次构建 ID 为 `70021d1d-8d18-4bdf-8066-446dd9898b80`；对应实验仓库 [固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)：[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/projects/lifetime-threads)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)、[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)。复现本轮时先 checkout 到该提交；旧基线 `e0220099` 继续用于此前调用链与泛型实验。

本轮没有采集 Memory Profiler 根快照、托管堆 used／reserved 或 RSS，也没有测量每次 GC 的耗时。弱引用失效不证明程序集已卸载、native metadata 已释放或内存已经归还 OS。线程状态的另一条生命周期见 [托管工作线程实测]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。

<!-- DATA-TODO: 独立负载下的增量开关性能对照仍待补；对象快照与活跃根区间见后续实验。 -->

## 已完成：原生回调中的 GC 引用保留与有界压力

2026-10-25，在同一份环境锁上新增 `projects/gc-callbacks`，build_id 为 `148235df-910b-441f-94b2-b7348eb02d8c`。仍使用本机自定义 Unity `2022.3.62f3_96770f904ca7`、package v9.0.3 / runtime v9.0.1、配套 il2cpp_plus v2022.3.x-9.0.1，Windows x64 IL2CPP Development、C++ Debug、Minimal stripping；GCMode=Enabled、isIncremental=True。

这次把“显式句柄保留”与“回调活跃局部引用”分组，防止强句柄掩盖活跃引用的问题：

| 组别 | 持有与观察方法 | 三个独立 Player 的共同结果 |
|---|---|---|
| handle | 热更 Context 由短寿命托管线程创建，Normal GCHandle 保留；native 保存不透明 token；第二次回调等待时收集3次 | 返回74/75、checksum149；启动前、回调中及Join后尚未Free时存活；Free后弱引用在本次窗口失效 |
| stack | 不创建Context句柄；热更回调内新建Node，仅以局部变量继续使用，AOT只记录短弱引用；等待时收集3次 | 回调返回257；放行后完整检查256字节内容，Join后弱引用在本次窗口失效 |
| stress | Normal句柄保留Context；1024次回调，每次32节点、每节点256字节payload，修改64槽对象图及Head/Next | count1024、checksum786944、errors0；32个检查点均存活；停止/Join/Free后弱引用失效 |

共九个无调试器独立进程，均正常退出。stack组没有借用Normal句柄保留目标，但这仍是活跃回调跨收集的数据完整性对照，没有采集根快照来证明唯一保留来源。`GC.KeepAlive` 放在放行后的使用末尾，明确探针需要的局部引用生命周期。释放后的弱引用检查是三轮显式收集内的观察；验收器允许仍有存活对象并如实记录。

压力组分配的payload总计 **8,388,608字节（8 MiB）**，不含对象头、数组头和互操作开销，不等于峰值堆大小。槽位在第二次回调起应全部非空；校验每槽预期Id、payload、最新Head身份、Head.Next及其他Next已清空，防止“旧对象内容没坏”掩盖丢引用或陈旧引用。

压力组每32次回调停在检查点，主线程分四帧请求 `CollectIncremental(1_000_000)`，放行后再继续分配和修改引用：总计32检查点、128请求。负载窗口内不调用显式Full；开始前和Join后的完整收集用于生命周期检查，另行计数。

三轮结果均为：128次请求返回false，负载开始到Join后的 **`GC.CollectionCount(0)` 差值为256**。这个窗口包含Unity自动收集及显式增量请求的影响，不能将差值分摊给单个API，也不能写成256次完整Full GC。[Unity API文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Scripting.GarbageCollector.CollectIncremental.html)说明false表示返回时没有剩余工作；本次同时确认isIncremental=True，因此不能把false解读成“没有发生GC”。

本轮未直接观察到同一标记周期跨多帧仍有剩余工作的过程。对象校验、分配负载和收集计数支持这个有界样本的结果，不能外推为所有写屏障路径正确、长期无泄漏或GC耗时达标。

handle/stack还各运行一个匹配PDB的CDB进程，验证三次Full入口位于目标回调中、MachineState根注册之后和scope退出之前。handle的FreeContext入口在原生Join之后，stack的token为0且没有FreeContext调用。详细所有权与停止顺序见 [线程篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。断点没有测量native释放量或实际根扫描区间；stress的1024次负载没有逐次原生断点跟踪。

复现入口为 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab) 中的 `tools/gc_callback_lab.py`，依次执行 `build --editor <Unity.exe路径>`、`run`、`trace --cdb <x64-cdb.exe路径>`、`verify`。本轮固定提交为 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，包含[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/results.json)。产物和原始日志在忽略目录 `artifacts/gc-callbacks/`；旧 `b915632` 仍只标识前一轮原生回调实验。

<!-- DATA-TODO: 持续跨片段引用修改和独立性能采样仍待补；快照与实际根区间见下一节。 -->

## 已完成：对象快照与解释器活跃根区间

前一轮看到对象跨收集仍可用，还不能直接回答“运行时向GC报告了哪一段栈”。2026-10-25，在独立 `projects/gc-roots` 中继续验证，build_id `993e5b9e-1f71-4949-b339-39ff10d359b1`，仍使用上述自定义Editor／v9组合、Windows x64 IL2CPP Development、C++ Debug和Minimal stripping。

这次分别运行9个无调试器对照Player、6个快照Player和9个CDB Player，均退出0。快照与原生断点分进程采集，不拼成同一时刻。CDB记录收集器调用 `GetGCRootData` 的入口和真实返回缓冲，调用栈包含 `GC_push_roots`；不只根据字段计算预期长度。

| 对照 | 每个进程的3次实际返回，三进程一致 | 目标引用在区间中的字节偏移 |
|---|---|---|
| Normal句柄保留Context | frameTop1、stackTop36、非空base、288字节 | 24 |
| 无Context句柄的活跃局部Node | frameTop1、stackTop36、非空base、288字节 | 48、264 |
| 解释方法返回、托管线程仍等待 | backing base仍非空，frameTop0、stackTop0，返回null／0 | 无报告槽位 |

三组的分配容量均为131072槽，`StackObject` 为8字节，即1 MiB分配容量；活跃根回调报告的只是288字节，空闲时则为空。验收器逐项核对返回base、长度、线程、根生命周期和完整槽位序列。目标地址来自自有观察入口，保存在调试器伪寄存器，不另加Normal或Pinned句柄。它证明“该引用出现在GC查询到的区间中”，仍不证明这是唯一根，也没有记录保守标记器逐字访问的过程。

对象快照使用 `Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot`，捕获标志为 `ManagedObjects`。handle组三个进程各取回调前、回调活跃、Join后仍持有、Free后四份快照：前三阶段强句柄表中均有同一 `GcHot.Context`，Tag73、Head为空，并可沿Slots字段找到长度64、元素全空的 `GcHot.Node[]`；Free后该热更强句柄目标消失。弱引用失效仍只作为这次收集窗口中的独立观察，不能通过堆中残留字节判断对象活着。

stack组三个进程各取回调前、活跃、退出后三份快照，均无热更强句柄目标，但活跃回调跨收集后仍能完整检查Node数据。**21份快照的ManagedStacks条目均为0**；该快照与本次解码范围没有给出解释器动态根边。因此，快照强句柄表中没有Node，不意味着Node没有根或已经回收。原生动态根区间由前面的CDB记录单独举证。

为避免只相信自有解析器，另用未修改的Unity官方Memory Profiler1.1.0读取器，在独立Editor分析工程中读取21份快照。9类条目共189组数据缓冲的数量与内容哈希全部一致；该包没有加入Player依赖。类型和字段来自快照描述，原始内存、PDB和完整调用栈保留在本机忽略目录。

复现入口为实验仓库的 `tools/gc_roots_lab.py`（build/run/inspect/trace/verify）及 `tools/check_snapshots_unity.py`，本轮固定提交为 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，包含[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)及[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/results.json)；`449a994` 继续作为前轮GC保留／压力基线。线程生命周期对照见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})，模型区分见 [GC桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})。

<!-- DATA-TODO: 后续补完整动态根路径归因、持续跨增量片段引用修改、长期与性能采样；当前快照和区间记录不能替代。 -->

## 接到 HybridCLR

解释执行共享 IL2CPP 的托管对象体系，但解释器自己的 native 状态需要额外接入。GC 桥接篇继续解释活跃解释器栈如何被枚举、引用写入如何通知宿主；线程接入篇解释线程退出如何清理这些状态。程序集 metadata 的释放则是另一层能力，不能由一次 GC 完成来推定。

**先证明谁还持有对象，再区分对象回收、堆容量和 native 资源，才能解释内存曲线。**

## 系列位置

- 上一篇：[泛型代码生成]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}})
- 下一篇：[ECMA-335 覆盖度]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}})
- 返回：[IL2CPP 索引]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})
