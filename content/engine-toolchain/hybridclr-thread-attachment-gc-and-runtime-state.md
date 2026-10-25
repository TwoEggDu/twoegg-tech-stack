---
title: "IL2CPP 与 HybridCLR 的线程接入｜托管线程、原生回调、解释器状态与 GC"
slug: "hybridclr-thread-attachment-gc-and-runtime-state"
date: "2026-10-25"
description: "从工作线程进入热更代码的问题出发，串起 runtime attach、MachineState、GC 根、退出清理和 Unity 主线程边界。"
weight: 63
featured: false
tags: ["Unity", "IL2CPP", "HybridCLR", "Threading", "GC"]
series: "HybridCLR"
hybridclr_version: "v6.1.0 (5567f367)"
---

同一个热更方法，从 Unity 主线程调用正常，换成 SDK 的工作线程回调后却失败。给业务数据加了锁，问题仍然存在；补上线程 attach 后，又发现访问场景对象不对。这里至少跨越了四种不同的边界。

这篇接在 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 与 [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})之后，解释一条线程怎样进入、执行并退出 IL2CPP／HybridCLR。async 的调度、SynchronizationContext 和取消协议继续看 [Unity 异步运行时系列]({{< relref "engine-toolchain/unity-async-runtime-series-index.md" >}})。

> 本篇核对了固定源码中的接入与清理路径。保留早期桌面编译／运行记录，并新增固定 v9 组合的真实 IL2CPP Player 托管工作线程结果；原生插件回调与设备压力测试仍待补充。源码版本并不等同于一套已经验证的 Unity patch 和平台组合。

## 先把线程问题拆成四层

| 层次 | 要回答的问题 | 解决后仍不能推出什么 |
|---|---|---|
| runtime 线程登记 | IL2CPP 是否认识当前线程，GC 是否按平台协议跟踪它？ | 可以任意调用 Unity API |
| 解释器状态与根 | 当前线程的参数、帧和引用由谁保存、枚举和清理？ | 多个线程访问同一对象没有竞争 |
| 业务同步 | 共享数据如何发布、互斥与停止使用？ | 所有引用写入都满足 GC 协议 |
| 引擎线程约束 | 当前操作是否允许在此线程执行？ | await 之后自然回到 Unity 主线程 |

attach 是把当前 OS 线程接入运行时，不是把它迁移到主线程，也不改变业务代码的同步责任。TLS 中有解释器状态，也不是线程安全证书。

```text
线程来源 → runtime 登记 → 方法入口 → 当前线程解释器状态
                                ↓
                    托管引用可见 + 正确引用写入
                                ↓
                     发布结果 → 主线程消费
```

## 固定版本再读路径

沿用源码起步篇的历史样本，便于对照而非追逐 main：

| 仓库 | tag / commit | 本篇重点 |
|---|---|---|
| hybridclr | v6.1.0 / `5567f367502d916c6742873d8ed2ab13d0f6900a` | InterpreterModule、Engine、GetInterpMethodInfo |
| hybridclr_unity | v6.1.0 / `88e20505f87d57cbff5695be5f7baa35d626f619` | 生成 Reverse P/Invoke wrapper 的模板 |
| [il2cpp_plus](https://github.com/focus-creative-games/il2cpp_plus/tree/ce2e34097e099e458e14192bed96c28bc761392b) | v2022-6.1.0 / `ce2e34097e099e458e14192bed96c28bc761392b` | Thread、ScopedThreadAttacher、GC 接入 |

源码获取和实际 Player 调试环境见起步篇。公开正文只描述 Unity runtime 的行为和文件位置，不转载其 C++ 函数体。

## 托管线程与外部原生线程，起点不同

从支持托管多线程的 Player 上使用 `System.Threading.Thread` 或 ThreadPool 时，线程的启动与运行时登记由相应运行时路径管理。业务代码不应再给这些线程补一组无条件 attach／detach。

SDK 自己创建的原生线程则可能尚未接入 runtime。它进入托管代码时，需要检查**实际回调入口**是否已经承担登记职责。拿到的地址是合法回调 wrapper，还是直接把某个内部方法入口当成函数指针，差别很大。

在固定的 hybridclr_unity 样本中，[Generator.cs 的 GenerateReversePInvokeWrappers](https://github.com/focus-creative-games/hybridclr_unity/blob/88e20505f87d57cbff5695be5f7baa35d626f619/Editor/MethodBridge/Generator.cs)会在生成的 Reverse P/Invoke wrapper 中使用 `ScopedThreadAttacher`。配套 `libil2cpp/vm/ScopedThreadAttacher.cpp` 的行为是：当前线程未登记时才接入；退出时只清理由这个作用域建立的登记。

因此不能写成“所有 native 回调都要在 C# 回调体里手工 attach”，也不能写成“任何函数指针都会自动 attach”。先核对最终生成物和调用栈，确认命中的是这条 wrapper 路径；回调声明、调用约定、生成数量、裁剪和版本配套仍需正确。

如果集成层确实负责直接接入，原则可以写成下面的**所有权伪代码**，它不是可直接编译的插件实现：

```text
确认 runtime 已初始化且允许接入
记录当前线程此前是否已登记
若未登记：通过该版本支持的接口登记，并记下“由本层拥有”
进入受支持的托管调用边界，处理结果和异常
退出全部由本层发起的托管调用
仅当登记由本层拥有时，在当前线程解除登记
```

`il2cpp_thread_attach`／`il2cpp_thread_detach` 的可用性、导出方式与宿主支持属于具体构建约束。不要仅凭函数名设计通用 SDK，更不要 detach Unity 主线程或线程池拥有的线程。运行时开始关闭后也不能继续接受迟到回调。

## 接入 runtime 之后，解释器还有自己的 TLS

配套 `libil2cpp/vm/Thread.cpp` 的 `Thread::Attach` 先检查当前登记，再进入 GC 线程注册和托管线程初始化路径。线程身份、线程静态数据与解释器帧不是同一个数据结构。

进入解释执行时，[InterpreterModule.h](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/InterpreterModule.h)中的 `GetCurrentThreadMachineState` 从 TLS 取状态，没有才分配 `MachineState`。其解释器栈、帧与异常流存储按需初始化。

由 [Engine.h](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/Engine.h) 可以看到，解释器栈初始化时注册动态根回调。回调提供 `_stackBase` 到当前 `_stackTopIdx` 对应的活跃范围，而不是把全部预留容量永久当成活跃栈。

这带来三个可观察的区别：

- 同一线程连续调用可复用 MachineState；换线程不能复用它的帧地址。
- 返回一个方法会退出相应帧，但不等于立即释放该线程的整个解释器栈容量。
- 线程池线程长期存在，状态也可能继续存在；是否泄漏要结合线程与清理路径判断。

方法转换结果则是另一类共享数据。[GetInterpMethodInfo](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/InterpreterModule.cpp)使用 metadata 锁检查／生成缓存，并在发布 `interpData` 前有内存屏障。**方法的已转换指令可以复用，执行中的线程栈不能因此合并。** 这把 runtime 内部锁也不保护你自己的静态字典、事件列表或对象字段。

## 退出需要沿原来的所有权返回

在配套 `Thread.cpp` 中，`UninitializeManagedThread` 包含 GC 线程注销、线程静态数据与登记清理，并调用 `FreeThreadLocalMachineState`。后者删除当前 TLS 状态；MachineState 析构会注销已建立的动态根并释放相关 native 数组。

这是固定版本里的配套路径，不是“OS 线程消失就一定清理所有解释器状态”的通用保证。强杀线程、宿主错误的退出顺序或版本混配，都可能绕开预期生命周期。

尤其要注意 wrapper 的作用域：一个原先未登记的 SDK 线程，如果每次回调都由 wrapper 临时接入并退出，就可能在每次调用后清理状态；长期由集成层持有登记的线程则有不同的复用范围。不要看到相同 OS 线程号就断言 MachineState 必须一直是同一个。

建议把停机协议设计为：停止产生新回调 → 等待在途回调结束 → 解除回调注册并释放持有的委托／句柄 → 完成由本层拥有的线程登记清理。具体顺序还受 SDK 的注销与等待契约约束。`GC.Collect()` 无法代替其中的等待与所有权交接。

## 动手验证一：从托管工作线程进入热更方法

先使用支持托管线程的桌面 IL2CPP Player。沿起步篇准备 AOT 启动层与单独的热更 DLL；程序集加载、补充元数据和必要的生成步骤在启动层先完成。本实验不在多个线程上并发加载程序集。

热更 DLL 放一个没有 Unity API、共享可变字段和锁的纯方法：

```csharp
public static class HotWorker
{
    public static int Add(int a, int b) => a + b;
}
```

AOT 启动层保留下面的探针。把已加载热更程序集中的 `HotWorker.Add` 对应的 `MethodInfo` 传入，目标必须是这个 `static int Add(int, int)`：

```csharp
using System;
using System.Reflection;
using System.Threading;

public static class ManagedThreadProbe
{
    public static string Run(MethodInfo add)
    {
        if (add == null) throw new ArgumentNullException(nameof(add));
        int caller = Thread.CurrentThread.ManagedThreadId;
        int workerId = 0;
        int first = 0, second = 0;
        Exception failure = null;

        var worker = new Thread(() =>
        {
            try
            {
                workerId = Thread.CurrentThread.ManagedThreadId;
                first = (int)add.Invoke(null, new object[] { 2, 3 });
                second = (int)add.Invoke(null, new object[] { 7, 8 });
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        worker.Start();
        worker.Join();
        if (failure != null)
            throw new InvalidOperationException("Worker call failed", failure);
        if (workerId == caller || first != 5 || second != 15)
            throw new InvalidOperationException("Unexpected result or thread");
        return $"caller={caller}, worker={workerId}, results={first},{second}";
    }
}
```

`Join` 在这里只用于等待这个有限、纯计算实验结束后读取结果；它会阻塞调用线程。不要把此写法直接放进逐帧逻辑，更不要用它等待一个还需要主线程协助才能结束的 SDK 回调。产品里可用受同步保护的结果队列，在主线程消费；队列协议与取消由异步运行时系列展开。

探针中反射调用的参数数组和装箱也会分配，不能用它测解释器纯算术吞吐。主线程拿到返回字符串后再输出日志，工作线程不触碰 Unity 对象。

### Player 断点要证明什么

| 位置 | 观察项 | 解释 |
|---|---|---|
| runtime 托管线程启动路径 | 当前线程身份与 GC 登记 | 这不是外部 native 线程手工接入实验 |
| GetCurrentThreadMachineState | TLS 值与状态地址 | 第一次需要时创建，同一线程第二次复用 |
| Interpreter::Execute | 方法身份、线程号、当前帧 | 证明调用实际走解释执行 |
| GetInterpMethodInfo / Transform | 首次是否生成，后续是否命中缓存 | 已预热时可能一开始就命中，不能强求再次转换 |
| GetGCRootData | 活跃范围与栈顶变化 | 观察机会取决于收集发生时机 |
| UninitializeManagedThread / FreeThreadLocalMachineState | worker 退出时的清理 | 注销动态根与释放线程状态 |

只看到 `5,15` 无法证明走过 HybridCLR。Editor Mono 或普通 .NET 也能输出相同结果；必须结合构建后端、热更 DLL 身份和 Execute 断点。

桌面核验记录：2026-10-25，Windows、.NET SDK 10.0.402、Release。从本文提取 HotWorker 与 ManagedThreadProbe，在同一控制台程序集内运行，得到 `caller=2, worker=4, results=5,15`。这只验证示例的编译、工作线程调用与结果交接，不验证 AOT／热更程序集分界、wrapper 或上述 Player 断点。

### 已完成：两个托管工作线程的 Player 对照

2026-10-25，使用 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab) 的 `projects/lifetime-threads`，在与 [对象保留实验]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 相同构建中运行独立的 `--threads` 进程。构建 ID 为 `70021d1d-8d18-4bdf-8066-446dd9898b80`，实际环境是本机自定义 Unity `2022.3.62f3_96770f904ca7`、package v9.0.3 / runtime v9.0.1、配套 il2cpp_plus v2022.3.x-9.0.1，Windows x64 IL2CPP Development。前文 v6.1.0 源码阅读与本节实测版本分别标记。

实测样本比上面的教学探针多一个 worker 和完成／放行事件。两线程同时存活，worker1 等待 worker0 的两次调用完成后再调用同一个 `LifetimeHot.Samples.Add`；每个 worker 计算结束后都等待统一放行，再退出并 Join。这样可以同时区分线程独有状态与方法共享缓存，避免先销毁再分配造成地址复用的歧义。工作线程不调用 Unity API。

三个无调试器的独立 Player 中，两线程各自返回 `5,15`，`Join=True`，其 managed ID、OS ID 均彼此不同且不同于主线程；进程退出码均为 0。之后另启一个 CDB 进程，使用该构建的完整 PDB，核对热更 DLL、`InterpreterInvoke` 调用栈与下面的断点记录：

| 观察项 | 实际记录 |
|---|---|
| 主线程 | managed=1，OS=137848 |
| worker0 | managed=5，OS=141284；MachineState=`00000172054163b0` |
| worker1 | managed=6，OS=149628；MachineState=`0000017205416580` |
| 目标 MethodInfo | 四次 Execute 均为 `00000170f0145930`，方法名 Add，参数各为 `(2,3)`、`(7,8)` |
| 方法转换与缓存 | worker0 首次 Execute 前 Transform 1 次；四次 `interpData` 均为非空 `00000171d04dd208` |
| 每个 worker 的状态 | 创建 1 次、动态根注册入口 1 次，第二次调用之前没有销毁／重新创建 |
| 每个 worker 的退出 | 在该线程命中 FREE → 析构 → 动态根注销入口，各 1 次；state/root 地址对应 |

两份状态均创建后才出现首次销毁。断点证据支持本样本中**线程各有状态、方法转换结果被复用**，并观察到了正常退出时的清理调用链。这里的 Execute 是函数入口断点；首次状态创建发生在入口命中之后，符合函数内部按需获取 TLS 的顺序。

调试器线程号以十六进制输出，验收器转成十进制后与 Player 日志关联；所有地址只在这一个调试进程内有意义。清理断点证明入口被调用，未统计释放了多少 native 内存，也没有捕获 `GetGCRootData` 的实际扫描区间。主线程及其他 runtime 线程的记录不计入两个目标 worker 的生命周期。

复现时在对象实验的 build / run 后执行：

```powershell
python tools/lifetime_lab.py trace --cdb <x64-cdb.exe路径>
python tools/lifetime_lab.py verify
```

验收器要求同一构建、匹配文件哈希、完整 Player 结果、四次目标执行、共享缓存、不同且生命周期重叠的状态，以及对应的清理顺序。本轮对应 [固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，复现前先 checkout 到该提交；可核对[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/projects/lifetime-threads)、[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)与[原生事件摘要](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/native-events.txt)。原始日志与 PDB 留在本地忽略目录。

这次故意把两线程的目标调用串行化，未验证并发首次转换竞争、线程池复用或原生回调 attach／detach。正常 `System.Threading.Thread` 的成功路径，也不能代替异常中止、宿主关机与设备压力测试。

<!-- DATA-TODO: 后续补活跃根区间、并发首次转换及线程池实验。 -->

## 动手验证二：把原生回调单独做成一组

这组实验需要一个目标平台原生插件，不能用上面的托管 Thread 结果替代。先限定回调只传递整数和返回状态，再逐步加入对象引用与生命周期。

| 对照 | 要记录什么 |
|---|---|
| 在已登记线程回调 | wrapper 是否复用现有登记，返回后仍由原所有者管理 |
| SDK 新建线程首次回调 | wrapper 与 ScopedThreadAttacher 是否命中，是否建立线程与解释器状态 |
| 同一 SDK 线程多次回调 | 登记的作用域、状态创建／清理次数，而非只看线程号 |
| 停止期间仍有回调在途 | 是否停止新请求、等待完成后才释放委托和上下文句柄 |
| 增量模式下重复分配和引用写入 | 实际 GC 模式、对象完整性、引用保留与释放记录 |

托管侧必须在 native 使用期间持有回调委托；如需传递上下文，可使用有明确释放者的句柄，而不是把任意对象地址长期交给 SDK。异常在约定的回调边界内转换成结果或日志，不让它越过未定义异常传播契约的 native ABI。

这组测试不需要故意绕开 wrapper 或漏登记来制造崩溃；正常路径的身份、所有权和清理记录已经能回答接入是否符合预期。压力运行未崩只能增加信心，不能替代所有引用写入路径的正确性分析。

<!-- DATA-TODO: 在目标平台实现最小 native 插件，保留 wrapper 生成物、回调 ABI、线程退出记录及增量 GC 压测条件。 -->

## Unity 主线程与 Web 平台的边界

线程登记正确后，Unity API 的线程限制仍然存在。默认把场景对象和未明确声明支持后台调用的引擎操作留在主线程；后台线程只处理允许的纯数据工作，再通过可靠的同步机制传回结果。同步上下文与线程亲和性的讨论见 [Unity 官方异步说明](https://docs.unity3d.com/2023.2/Documentation/Manual/AwaitSupport.html)。

平台支持也不能写成一句“WebGL 不支持多线程”。[Unity 2023.2 WebGL 限制](https://docs.unity3d.com/2023.2/Documentation/Manual/webgl-technical-overview.html)区分了 C# 托管多线程与有条件的 C/C++ 原生线程支持。原生线程开关存在，不代表这篇 `System.Threading.Thread` 实验就能移植到该版本 WebGL；小游戏平台还要核对其适配层和宿主能力。

## 按症状回到正确的层

| 症状 | 先查什么 |
|---|---|
| native 回调一进来就失败 | ABI、wrapper、登记、生成物与版本是否匹配 |
| 能算数，一碰场景对象就出问题 | Unity API 的线程要求，结果是否在主线程消费 |
| 多线程首次调用时状态异常 | 类型初始化、方法准备、加载时序与源码版本；不要只看业务锁 |
| worker 结束后 native 内存持续增长 | 退出路径、TLS 与动态根清理、线程数量、方法缓存分别计数 |
| GC 期间出现偶发错误 | 引用可见性、写入协议、回调／句柄寿命，同时排查竞争与越界 |
| 退出时卡死 | 是否等待了依赖当前线程继续执行的工作，是否在持锁状态等待 |

**线程接入只解决运行时身份；正确执行还需要活跃引用可见、共享数据同步、退出所有权和引擎线程约束同时成立。**

## 系列位置

- 前置：[源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})、[GC 桥接]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})
- 并发语义：[volatile、屏障与线程内存模型]({{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}})
- 返回：[HybridCLR 索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})、[IL2CPP 索引]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})
