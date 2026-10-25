---
title: "IL2CPP 与 HybridCLR 的线程接入｜托管线程、原生回调、解释器状态与 GC"
slug: "hybridclr-thread-attachment-gc-and-runtime-state"
date: "2026-10-25"
description: "从工作线程进入热更代码的问题出发，串起 runtime attach、MachineState、GC 根、退出清理和 Unity 主线程边界。"
weight: 63
featured: false
tags: ["Unity", "IL2CPP", "HybridCLR", "Threading", "GC"]
series: "HybridCLR"
hybridclr_version: "runtime v9.0.1 (83929787) / package v9.0.3"
lastmod: "2026-10-25"
---

同一个热更方法，从 Unity 主线程调用正常，换成 SDK 的工作线程回调后却失败。给业务数据加了锁，问题仍然存在；补上线程 attach 后，又发现访问场景对象不对。这里至少跨越了四种不同的边界。

这篇接在 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 与 [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})之后，解释一条线程怎样进入、执行并退出 IL2CPP／HybridCLR。async 的调度、SynchronizationContext 和取消协议继续看 [Unity 异步运行时系列]({{< relref "engine-toolchain/unity-async-runtime-series-index.md" >}})。

> 本篇核对了固定源码中的接入与清理路径。保留早期桌面编译／运行记录，并新增固定 v9 组合的真实 IL2CPP Player 托管工作线程、原生插件回调、在途停止，以及原生回调下GC引用保留、有界分配压力、对象快照与实际活跃根区间结果；长期与其他设备压力测试仍待补充。源码版本并不等同于一套已经验证的 Unity patch 和平台组合。

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

沿用源码起步篇与当前实验的固定 v9 组合：

| 仓库 | tag / commit | 本篇重点 |
|---|---|---|
| hybridclr | v9.0.1 / `83929787581bce6c7219418f62348a10c33582ee` | InterpreterModule、Engine、GetInterpMethodInfo |
| hybridclr_unity | v9.0.3 / `6cadd24d101f4cbd00cd80190106f6621132d24d` | 生成 Reverse P/Invoke wrapper 的模板 |
| [il2cpp_plus](https://github.com/focus-creative-games/il2cpp_plus/tree/ba3a76c9979d14cee404fb5d44301773e0768866) | v2022.3.x-9.0.1 / `ba3a76c9979d14cee404fb5d44301773e0768866` | Thread、ScopedThreadAttacher、GC 接入 |

旧稿 v6.1.0 的阅读信息保留在起步篇的历史对照中；本篇已重新核对 v9 的相同入口。源码获取和实际 Player 调试环境见起步篇。公开正文只描述 Unity runtime 的行为和文件位置，不转载其 C++ 函数体。

## 托管线程与外部原生线程，起点不同

从支持托管多线程的 Player 上使用 `System.Threading.Thread` 或 ThreadPool 时，线程的启动与运行时登记由相应运行时路径管理。业务代码不应再给这些线程补一组无条件 attach／detach。

SDK 自己创建的原生线程则可能尚未接入 runtime。它进入托管代码时，需要检查**实际回调入口**是否已经承担登记职责。拿到的地址是合法回调 wrapper，还是直接把某个内部方法入口当成函数指针，差别很大。

在固定的 hybridclr_unity 样本中，[Generator.cs 的 GenerateReversePInvokeWrappers](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/CodeGen/MethodBridge/Generator.cs)会在生成的 Reverse P/Invoke wrapper 中使用 `ScopedThreadAttacher`。配套 `libil2cpp/vm/ScopedThreadAttacher.cpp` 的行为是：当前线程未登记时才接入；退出时只清理由这个作用域建立的登记。

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

进入解释执行时，[InterpreterModule.h](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.h)中的 `GetCurrentThreadMachineState` 从 TLS 取状态，没有才分配 `MachineState`。其解释器栈、帧与异常流存储按需初始化。

由 [Engine.h](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/Engine.h) 可以看到，解释器栈初始化时注册动态根回调。回调提供 `_stackBase` 到当前 `_stackTopIdx` 对应的活跃范围，而不是把全部预留容量永久当成活跃栈。

这带来三个可观察的区别：

- 同一线程连续调用可复用 MachineState；换线程不能复用它的帧地址。
- 返回一个方法会退出相应帧，但不等于立即释放该线程的整个解释器栈容量。
- 线程池线程长期存在，状态也可能继续存在；是否泄漏要结合线程与清理路径判断。

方法转换结果则是另一类共享数据。[GetInterpMethodInfo](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp)使用 metadata 锁检查／生成缓存，并在发布 `interpData` 前有内存屏障。**方法的已转换指令可以复用，执行中的线程栈不能因此合并。** 这把 runtime 内部锁也不保护你自己的静态字典、事件列表或对象字段。

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

2026-10-25，使用 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab) 的 `projects/lifetime-threads`，在与 [对象保留实验]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 相同构建中运行独立的 `--threads` 进程。构建 ID 为 `70021d1d-8d18-4bdf-8066-446dd9898b80`，实际环境是本机自定义 Unity `2022.3.62f3_96770f904ca7`、package v9.0.3 / runtime v9.0.1、配套 il2cpp_plus v2022.3.x-9.0.1，Windows x64 IL2CPP Development。本节环境与前文源码基线一致；它是本机自定义 Editor 组合的观察，不代表所有 Unity patch。

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

<!-- DATA-TODO: 并发首次转换及线程池仍待补；活跃根区间见后续gc-roots实验。 -->

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

### 已完成：原生线程回调与在途停止

2026-10-25，沿用同一份 `lab-lock.json`，在 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab) 新增 `projects/native-callbacks`。环境仍是本机自定义 Unity `2022.3.62f3_96770f904ca7`、package v9.0.3 / runtime v9.0.1、配套 il2cpp_plus v2022.3.x-9.0.1，Windows x64 IL2CPP Development、C++ Debug、Minimal stripping；日志记录 GCMode=Enabled、isIncremental=True。本次 build_id 为 `65d3ddb9-3bb0-4ce9-99b9-59f4ff1cc82b`。

自有 `native/native-callbacks/plugin.cpp` 通过 Win32 `CreateThread` 模拟 SDK 线程。回调 ABI 为 cdecl `int(int,int)`，仅传整数；AOT 启动层持有委托，热更静态 `CallbackHot.NativeEntry.Call` 完成加法并记录线程身份，异常在回调边界转换为错误返回与失败计数。回调不调用 Unity API，HotUpdate.dll 不进入 AOT 代码注册。这个封闭样本只支持一个受控任务，不充当通用 SDK 接入库。

固定 package 的 [wrapper 生成器](https://github.com/focus-creative-games/hybridclr_unity/blob/6cadd24d101f4cbd00cd80190106f6621132d24d/Editor/CodeGen/MethodBridge/Generator.cs) 根据 `MonoPInvokeCallback` 为该方法生成签名 `Bi4i4i4` 的 `__ReversePInvokeMethod_0`。实际跟踪的是“插件 → wrapper → N2M 桥 → Interpreter::Execute”，并关联传给插件的函数指针与 wrapper 入口地址。

普通运行分两组，各启动三个独立、无调试器 Player，均退出 0：

| 对照 | 运行结果 |
|---|---|
| registered：托管 Thread 调用插件，再同步回调热更代码 | 返回 5、15；回调 managed ID 与原托管线程一致，返回插件后身份仍保留；worker Join 成功 |
| raw：插件创建原生线程并重复回调 | 返回 5、15、23；同一 OS ID 下，三次回调的 managed ID 分别为 5、6、7 |
| raw 的第三次回调仍在途时停止 | 回调先进入 Hold；主线程请求 stop，Join(0) 返回未完成；放行后 Join 成功，最终次数为 3，没有第四次回调 |
| 委托寿命 | 等对应线程 Join 成功后才清空 AOT 强引用；native 也在等待线程结束后清空函数指针 |

之后两组各另启一个 CDB 进程。GameAssembly 与自有 NativeProbe 均加载本构建匹配的 private PDB，实际记录如下：

| 观察项 | registered | raw |
|---|---|---|
| 目标 OS ID | 152068 | 119432 |
| 回调 managed ID | 5、5 | 5、6、7 |
| ScopedThreadAttacher 的 owned 字段 | 两次均为 0 | 三次各为非零 |
| 目标线程命中 Attach 入口 | 0 次 | 3 次 |
| 回调期间命中 Detach 重载入口 | 0 次 | 6 次，即每次回调经过两个重载入口 |
| MachineState 创建／根注销入口 | 各 1 次，正常托管 worker 退出时清理 | 各 3 次，每次回调返回插件前清理 |
| 目标方法 Transform | 1 次 | 1 次 |
| Execute 入口的 interpData | 首次明确为空，第二次非空 | 首次明确为空，后两次地址相同且非空 |

**原生线程号相同，不代表跨回调保留同一托管登记或解释器状态。** 在这个固定版本与入口下，registered 的 wrapper 没有取得登记所有权，回调返回时继续保留线程状态；raw 则在每次回调里临时接入，并在返回插件前沿 Detach、FREE、析构、动态根注销入口清理。这里的 6 次 Detach 是 3 条调用链里的两个重载入口，不能算成 6 次独立解除登记。

方法缓存与线程状态也分开：首次 Execute 入口尚无缓存，随后命中一次 Transform，后续回调复用方法缓存。raw 的三次状态创建没有导致目标方法重复转换。地址只在各自进程内有意义；本次不同状态地址不能推出以后永不复用地址。

在途停止的原生事件顺序为“第三次回调进入 → 主线程 stop → 第三次回调返回 → 原生线程结束 → 主线程 Join 完成”。配合 Player 的 `joinedBefore=False` 与 Join 后释放记录，可核对正常退出协议。等待均有超时；失败清理会请求 stop、放行并等待线程，等待失败时保留委托根引用直到进程结束。异常注入、强杀线程及真实 SDK 的关闭竞争尚未覆盖。

在实验仓库执行 `python tools/callback_lab.py build --editor <Unity.exe路径>`、`run`，再执行 `trace --cdb <x64-cdb.exe路径>` 与 `verify`。四个子命令都使用同一个 `tools/callback_lab.py`，输出独立放在 `artifacts/callbacks/`。验收器同时检查构建、源码／产物／日志哈希、返回值、wrapper 地址、登记所有权、状态／根清理、方法转换以及停止与释放顺序；字段缺失不能冒充明确空值。

本轮对应实验仓库 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb)。复现前先 checkout 到该提交，可查看[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/b9156325820312f4d290c4af8158f76a3a6f9beb/projects/native-callbacks)、[自有原生插件](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/native/native-callbacks/plugin.cpp)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/results.json)；[registered 事件](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/registered-native-events.txt)和[raw 事件](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/raw-native-events.txt)提供脱敏断点摘要。旧 `a387d28` 继续标识对象保留与托管线程实验。

清理断点说明函数入口和调用顺序，不统计 native 内存释放量，也没有观察 GC 实际扫描的根区间。本轮记录了增量模式，但没有运行增量引用修改压力负载，不能据此确认所有写屏障路径或性能。

## 扩展验证：回调持有、活跃根与跨片段写入

前三节已经建立线程接入和退出协议。下面按引用的持有方式检查这个协议，完整 GC 数据集中在 IL2CPP GC 篇。

### 回调中的上下文句柄与活跃引用

2026-10-25，同一环境锁新增GC回调工程，build_id `148235df-910b-441f-94b2-b7348eb02d8c`。完整分组、数据和收集口径见 [IL2CPP GC篇的新实验]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})，这里聚焦线程与所有权。

这次回调ABI为cdecl `int(IntPtr,int,int)`。handle/stress组用Normal GCHandle保留热更Context，native仅保存句柄token，不保存对象地址，也没有固定对象地址的要求。Context由短寿命托管创建线程产生，创建线程Join后才开始观察；static回调委托只在AOT持有。stack组的token为0，回调中创建的热更Node只通过活跃局部引用继续使用，AOT观察者仅保存短弱引用。

handle、stack、stress 各三个独立 Player 通过。前两组在回调等待期间收集三次，放行后数据正确；压力组完成 1024 次回调与对象图校验。该轮的增量请求均返回 false，不能替代下文同一周期持续跨片段的验证。

handle/stack另各有匹配GameAssembly/NativeProbe private PDB的原生记录：

| 观察项 | handle | stack |
|---|---:|---:|
| 目标OS线程 | 154732 | 100240 |
| 原生回调数 | 2 | 1 |
| Attach入口／Detach重载入口 | 2／4 | 1／2 |
| 状态和动态根清理链 | 2 | 1 |
| 目标回调活跃、根注册后、scope退出前的Full入口 | 3 | 3 |
| Join后FreeContext入口 | 1 | 0 |

Detach数字仍是两个重载入口组成一条解除登记调用链。stack组没有上下文句柄可释放；不能用handle组的成功替代stack的结果，也不把它们的聚焦断点当成1024次压力负载的逐次调用链记录。

每组在最后一个回调关卡请求stop，`Join(0)`应尚未完成，放行后才允许原生线程退出。成功Join之后，handle/stress才Free Context，最后释放回调委托；失败等待时继续保留根引用，避免native仍可能回调时提前释放。九个进程中，解除对应持有后弱引用均在三轮显式收集窗口内失效；这是观察值，释放后仍存活也会被验收器记录。

自有样本与证据已固定在提交 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)：[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)、[自有原生插件](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/native/gc-callbacks/plugin.cpp)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)及[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/results.json)；通过 `tools/gc_callback_lab.py` 的build/run/trace/verify复现。前一轮 `b915632` 不包含新增GC样本。

本轮未验证回调异常注入、多原生线程竞争、真实 SDK 关闭竞争、实际根枚举内容或持续跨片段标记期的引用修改；后两项的独立后续样本见下文。收集期间本样本对象可用、scope按顺序清理，与写屏障完整性和长期泄漏仍是不同结论。

<!-- DATA-TODO: 继续补原生回调异常注入、多原生线程竞争及其他平台；串行托管worker的跨片段修改见后续实验。 -->

### 解释帧活跃、空闲与根注销对照

同一固定环境的后续 `gc-roots` 实验（build_id `993e5b9e-1f71-4949-b339-39ff10d359b1`）进一步采到了GC根回调的真实返回值。handle、stack、idle三组各有3个无调试器对照和3个匹配PDB的CDB进程；另有handle/stack各3个独立快照进程，均正常退出。

- **外部原生线程仍在热更回调中**：解释帧为1，stackTop36，实际报告288字节；Context引用在偏移24，局部Node引用在偏移48和264，均在区间内。每个进程收集3次，结果一致。
- **托管线程仍存活、解释方法已经返回**：线程先执行热更 `IdleCreate`，返回AOT后等待主线程放行。解释器栈仍已分配、容量131072槽，但frameTop／stackTop为0；3次GC查询均返回null／0。这里区分了“线程存在”“有MachineState”和“有活跃解释帧”。
- **线程退出**：按对应注册与所有者匹配根注销；采样不越过注销读取已释放状态。对象是否回收、根注销和native内存归还量继续分别判断。

这轮目标地址保存在调试器伪寄存器，没有向Player新增持有目标的句柄。收集线程与根所属工作线程分别记录；不是把“当前执行GC的线程”误认为MachineState所属线程。

21份对象快照另行验证Normal句柄目标及Context→Slots数组路径，Free后强句柄目标消失；快照ManagedStacks条目均为0，没有给出该解释器动态根边。完整矩阵及官方读取器的189组缓冲交叉校验见 [IL2CPP GC篇]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})。这支持本样本引用位于报告区间，不证明唯一根来源、全部写屏障或长期无泄漏。

本轮固定提交为 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，入口为[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)和[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/results.json)，复现命令为 `tools/gc_roots_lab.py` 与 `tools/check_snapshots_unity.py`；旧 `449a994` 不包含新增根快照工程。

### 常驻托管 worker 的跨增量片段写入

收集周期尚未结束时再创建／登记writer线程，可能让运行时先推进收集；这次探索确实在第一次写入前触发完成，已被拒绝。因此独立 `incremental-barriers` 样本改为**先创建、登记并预热一个常驻托管worker，再启动目标周期**。这不是外部原生线程回调实验。

沿用上述Editor／v9锁和Windows x64配置，GCMode作为控制变量切为Manual。主线程连续12帧分别推进增量片段，确认more=True和MARK_END完成数不变后，通过事件放行worker修改字段、引用数组或含引用结构体；等待writer返回、清理自有native栈后再推进下一帧。收集与writer按握手串行，worker不使用Unity场景API，也不宣称并发首次转换、并发GC或多线程写竞争已验证。

build_id `70a8a176-4eee-43b2-8aa8-a75754ac0bfb` 中，AOT／hot各3个普通Player与3个匹配PDB的调试Player均退出0。12次修改期间原生GC编号保持4，完成时为5，对象图checksum12078。最终图和标记检查放在Join之前；退出清理引发的后续GC不归入目标周期。写屏障、dirty与重扫的完整分层结果见 [IL2CPP GC篇]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})。

每个hot调试进程有一组归属该worker的动态根注册／注销，6次GC查询入口top0／frames0：1次在基线、2次在目标周期收尾、3次在周期后及退出清理；不能把退出清理查询解释为仍在等待下一次任务。目标周期中的两次查询发生在writer返回后的等待阶段。12次scrub后，当前活跃worker栈内均未找到当前候选的对齐精确指针；这不排除寄存器、内部指针、其他目标和其他线程。这里只采了根回调入口状态，真实返回缓冲的独立证据仍属于前轮gc-roots实验。

复现使用实验仓库 `tools/incremental_lab.py`，记录为 `evidence/2026-10-25-incremental-barriers/`；当前尚未提交，本轮不借用旧4861bed充当源码版本。固定依赖、Manual控制和单worker串行握手是结论成立的条件，原生回调竞争、异常退出与其他平台继续单独验证。

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
