---
date: "2026-03-20"
title: "HybridCLR 调用链实战｜跟着一个热更方法一路走到 Interpreter::Execute"
description: "保留 v6.1.0 的源码调用链，并用 v9 配套 Player 的原生断点，验证同一热更方法首次转换、再次复用和两次执行。"
weight: 34
featured: false
tags: [Unity, IL2CPP, HybridCLR, Runtime, SourceCode]
series: "HybridCLR"
hybridclr_version: "源码阅读 v6.1.0；Player 实测 runtime v9.0.1 / package v9.0.3"
---

热更 DLL 已经加载，反射也取到了方法，调用后返回了正确结果。这些现象能证明业务入口可用，却还没有回答：参数经过了哪个 invoker，什么时候转换方法体，第二次调用又复用了什么？

这篇沿着一个独立的 `HotUpdate.ReadingProbe.Add(int, int)` 回答这些问题。工程准备、版本身份和符号检查放在 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})，这里集中跟状态变化，不依赖业务项目的启动框架。

> **版本与证据范围**：前八跳保留 HybridCLR v6.1.0（`5567f367502d916c6742873d8ed2ab13d0f6900a`）的历史源码分析。后面的 Player 实测使用 runtime v9.0.1、package v9.0.3 与本机自定义 Unity；完整 commit 见起步篇。旧版路径、版本差异和实测表分别标明。

## 先把装载、准备与执行分开

一条调用链里有三种不同的状态变化：

| 阶段 | 输入 | 产生的状态 | 不能据此推定 |
|---|---|---|---|
| 装载 | 热更 DLL 字节 | InterpreterImage 与程序集注册信息 | 所有方法都已转换 |
| 方法准备 | 方法身份、image、token、方法体 | invoker 和解释执行需要的内部表示 | 已完成本次业务调用 |
| 执行 | 已准备的方法与本次参数 | 当前栈帧、指令执行和返回值 | 下一次无需执行 |

同一个方法第一次调用可能需要准备内部表示；第二次仍要处理新参数、建立执行现场并返回结果。我们要证明的是转换结果复用，而不是“第二次不经过解释器”。

## 固定一个能排除业务干扰的入口

实验只有两个程序集。AOT 入口读取本次编译的 `HotUpdate.dll`，调用 `Assembly.Load(byte[])`，随后通过反射获取 `HotUpdate.ReadingProbe.Add`。热更程序集中的方法只有整数加法，没有显式静态构造、模块初始化和预热代码。

```csharp
var assembly = Assembly.Load(dllBytes);
var method = assembly.GetType("HotUpdate.ReadingProbe", true)
    .GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
var first = (int)method.Invoke(null, new object[] { 2, 3 });
var second = (int)method.Invoke(null, new object[] { 7, 8 });
```

完整样本和加载位置见起步篇。这里复用同一个反射方法对象，只装载一次 DLL，预期结果为 `5` 和 `15`。原生调试时还要核对两次调用的 `MethodInfo*`、类型与签名，不能仅凭同名方法归为同一对象。

## 第一跳：DLL 字节怎样进入 runtime

在配套 `il2cpp_plus` 中，先沿 `icalls/mscorlib/System/AppDomain.cpp` 的 `LoadAssemblyRaw` 找到 `vm/MetadataCache.cpp` 的 `LoadAssemblyFromBytes`，再进入 HybridCLR 的 `metadata/Assembly.cpp`。这些路径用于定位接入点；公开正文不复制 Unity C++ 实现。

观察这一跳时先记录 DLL 的构建来源、字节长度和 SHA-256，再核对装载入口收到的长度。这样才能把磁盘文件与本次装载关联起来。仅凭程序集名相同，不能证明装载的是本次生成物。

## 第二跳：装载后得到什么

固定版本的 [Assembly.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/metadata/Assembly.cpp) 中，`LoadFromBytes` 调用 `Create`，后者创建和加载 `InterpreterImage`，初始化相关 metadata，并构造、注册程序集与 image 信息。

断点观察应放在相应对象创建之后：输入长度是否匹配，image 是否属于本次 DLL，程序集名是否为 `HotUpdate`。这一步把 DLL 接入 runtime 的 metadata 体系，为后面的类型、方法和 token 解析建立依据，并不意味着每个方法都已生成内部指令。

`LoadFromBytes` 后续还会处理模块初始化。因此“装载期绝不会执行方法”也不成立。本样本去掉模块初始化逻辑，目的是把 `Add` 的第一次触发留在显式反射调用处。

## 第三跳：方法为什么选中解释器 invoker

接下来沿 `vm/Class.cpp` 的方法初始化入口，观察 `MethodInfo` 的 `invoker_method`。在配套 runtime 中，invoker 选择会区分普通 AOT image 与解释 image；后者继续进入 `MetadataModule`、`InterpreterImage::GetMethodInvoker` 和 `InterpreterModule::GetMethodInvoker`。

本例的普通静态加法方法预期绑定 `InterpreterInvoke`。其他方法形态存在不同分支，例如委托相关入口，不要把本例扩展成所有方法的唯一实现。

这一跳完成的是调用方式的绑定。`invoker_method` 已经可用，与 `interpData` 已经包含转换结果，是两个不同的观察点。

## 第四跳：反射怎样进入已经绑定的 invoker

`MethodInfo.Invoke` 处理参数和返回值，再由 runtime 调用该方法绑定的 invoker。跟踪 `vm/Runtime.cpp` 时，重点核对目标方法身份、传入参数，以及函数指针最终指向哪里。

对本例而言，反射调用把控制权交给 `InterpreterInvoke`。反射查找成功不能替代这个观察；同样的托管反射 API 也可以调用 AOT 方法。

第一轮不必单步每一层反射内部实现。先确认本次调用确实抵达目标方法的 invoker，再回看中间的参数封送和异常处理。

## 第五跳：InterpreterInvoke 怎样准备参数

在 [InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/InterpreterModule.cpp) 的 Unity 2021+ 分支中，`InterpreterInvoke` 先检查 `method->interpData`；没有转换结果时调用 `GetInterpMethodInfo`。随后按方法描述准备 `StackObject` 参数槽，把调用参数转换为解释器需要的布局，再调用 `Interpreter::Execute`。

因此实际顺序是“准备方法表示 → 转换参数 → 执行”。在断点中依次看：

- 入口的 `method->interpData` 是否为空。
- `imi->argStackObjectSize` 与参数布局是否对应当前签名。
- 参数转换后，槽中的两个整数是否为本次输入。

该入口的临时参数区使用 native 栈分配；这不能推出“每一次解释器内部调用都会新增同样的 native 调用层”，也不能据此断言 native 栈一定先溢出。跨边界调用与解释器内部建帧需要分别追踪。

## 第六跳：第一次怎样准备 InterpMethodInfo

固定版本的 `InterpreterModule::GetInterpMethodInfo` 从 [第 588 行附近](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/InterpreterModule.cpp#L588) 开始。它先处理 `RuntimeInitClassCCtor`，进入 metadata 锁后再次检查缓存，再选择 image、取得方法体、分配 `InterpMethodInfo` 并调用 Transform，最后经内存屏障发布到 `methodInfo->interpData`。

这里有三个必须分开的细节：

1. 类型初始化不能简化成“只做 metadata 布局”；显式静态构造可能引入额外调用。本样本刻意没有这类逻辑。
2. invoker 的快速缓存检查与锁内再次检查承担不同职责，不能删掉第二次检查来理解并发行为。
3. `interpData` 保存转换后的方法表示。它不是本次参数或返回值的缓存。

两次调用都应核对方法身份。只看到 `GetInterpMethodInfo` 命中，不能说明命中的是 `Add`；类型初始化和其他热更代码也可能走到这里。

## 第七跳：从哪个 image 取方法体，再转换什么

本版本在 `GetInterpMethodInfo` 中选择 image 并调用 `image->GetMethodBody(methodInfo->token, tempMethodBody)`。热更方法使用解释 image；AOT 解释路径还涉及同源 image，本例不负责验证后者。

拿到方法体后，调用 [HiTransform::Transform](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/transform/Transform.cpp#L150)，传入 image、MethodInfo、方法体和输出对象。Transform 解析、转换 CIL，形成解释器的内部表示。

**旧文的 `MethodBodyCache.cpp` 与 `Transform(methodInfo)` 写法不适用于本文固定的 v6.1.0。** 方法体读取发生在上层；不要拿其他版本的调用片段拼接这条链。方法体本身与转换结果 `InterpMethodInfo` 也不是同一层缓存。

在这一跳记录 image 来源、token、方法体输入，以及 Transform 返回后的 `codes`、`codeLength`。第二次调用没有进入目标 Transform，只有和同一方法的有效 `interpData` 一起观察，才构成复用证据。

## 第八跳：Execute 执行的是内部指令

[Interpreter_Execute.cpp](https://github.com/focus-creative-games/hybridclr/blob/5567f367502d916c6742873d8ed2ab13d0f6900a/hybridclr/interpreter/Interpreter_Execute.cpp#L1645) 中的 `Interpreter::Execute` 为从 native 进入的调用建立执行现场，随后按内部 `HiOpcode` 分派。第一次跟读只需要关注当前方法、参数、建帧后的指令指针和返回位置，不必遍历所有分支。

这里执行的是 Transform 的产物，不是直接拿原始 CIL 字节逐条分派。第二次复用转换结果后仍需进入 Execute，处理 `(7, 8)`，产生新的返回值 `15`。这也不是一次 native JIT 编译。

## 两次调用怎样形成证据闭环

以下是固定源码下的**预期观察表**。必须在新进程、未预热同一目标方法的条件下采集，不应照抄成实测结果。

| 位置 | 首次调用 | 再次调用 |
|---|---|---|
| InterpreterInvoke | 同一 Add；interpData 为空 | 同一 Add；interpData 已有值 |
| GetInterpMethodInfo | 取目标方法体并准备表示 | invoker 已有缓存时不需进入 |
| HiTransform::Transform | 目标 Add 命中一次 | 目标 Add 不再命中 |
| Interpreter::Execute | 参数 2、3 | 参数 7、8 |
| 返回日志 | 5 | 15 |

推荐按装载、invoker、方法准备、Transform、Execute 顺序下断点。记录目标 `MethodInfo*` 后，用地址条件过滤其他方法。首次已有缓存时先查预热和提前调用；再次转换时先查方法对象、泛型实例和进程身份。

只有正确返回值，证明的是样本功能结果；目标断点、缓存值和调用栈才支持这条执行路径。原生断点未绑定时，应先回到起步篇检查模块与 PDB。

## v9 Player 实测：同一方法转换一次、执行两次

2026-10-25，使用本机自定义 Unity `2022.3.62f3_96770f904ca7`，构建 Windows x64、IL2CPP、Development、C++ Debug Player。package v9.0.3 配套 runtime v9.0.1（`83929787581bce6c7219418f62348a10c33582ee`）和 il2cpp_plus v2022.3.x-9.0.1。这个实验没有沿用旧版兼容补丁，构建输入核对与完整版本表见 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})。

### 先核对与 v6 的路径差异

| 位置 | v6.1.0 阅读基线 | v9.0.1 实验基线 |
|---|---|---|
| GetInterpMethodInfo | 处理 RuntimeInitClassCCtor，选择 image 并读取 body | 锁内检查缓存，Class::Init 后调用 Transform |
| Transform 签名 | 接收 image、MethodInfo、body、输出对象 | 接收 MethodInfo，返回 InterpMethodInfo |
| 方法体来源 | 上层调用 image->GetMethodBody | Transform 内经 MethodBodyCache 读取 |
| 转换结果发布 | 写入 methodInfo->interpData | 仍写入 methodInfo->interpData |

因此，`MethodBodyCache` 是新版的真实入口，只是不应写进旧版链路。对应公开源码见 [v9 InterpreterModule.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/interpreter/InterpreterModule.cpp#L521) 和 [v9 Transform.cpp](https://github.com/focus-creative-games/hybridclr/blob/83929787581bce6c7219418f62348a10c33582ee/hybridclr/transform/Transform.cpp#L34)。本次缓存结论来自下面的实际断点，不靠两份源码“看起来相似”推定。

### 本次记录

CDB 从进程创建时设置断点，`lmv m GameAssembly` 指向本次构建的模块，显示 `private pdb symbols`。被加载的 HotUpdate.dll 长度为 4608 字节，SHA-256 为 `BA0096D4CA773599ED664FD1D47CC2507D7C55F36EF0BD0CF44349EE46A9645C`；最终 AOT 裁剪目录中没有 HotUpdate.dll。

| 观测项 | 第一次 | 第二次 |
|---|---|---|
| 托管目标 | HotUpdate.ReadingProbe.Add，Int32 Add(Int32, Int32) | 同一反射方法对象 |
| 原生 MethodInfo 地址 | `0x00000208f01353e8` | `0x00000208f01353e8` |
| invoker 入口 interpData | null | `0x00000208f0140ac8` |
| GetInterpMethodInfo / Transform | 目标方法各命中一次 | 目标方法均未再次命中 |
| Execute 入口 interpData | `0x00000208f0140ac8` | 同一地址 |
| Execute 参数 | 2、3 | 7、8 |
| 托管返回日志 | PROBE_FIRST=5 | PROBE_SECOND=15 |

地址只标识本次进程，重启后不能照抄为条件断点。记录中两个 Execute 都携带同一目标方法地址；首调的 Transform 栈上可见 `GetInterpMethodInfo → InterpreterInvoke → Runtime::InvokeWithThrow`，Execute 栈上可见 `InterpreterInvoke → Runtime::InvokeWithThrow`。因此可以把反射入口、准备阶段和两次解释执行关联起来。

最终日志包含 `PROBE_PASS`。同一构建脱离调试器再次启动也返回 5、15，进程退出码为 0；这是普通启动验收，缓存结论仍来自前一次原生跟踪。原始日志、PDB 和 runtime 留在本地，仓库的 `scripts/hybridclr-reading-probe/` 提供自有样本与本次 CDB 命令。

这组记录验证了当前样本的转换结果复用。它没有测量速度，不证明所有方法形态、AOT 泛型、GC、线程接入或移动端行为；v6.1.0 也没有因为这次 v9 成功而获得 Player 验收。

## 系列位置

准备环境从 [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) 进入；要解释跨边界参数约定，继续 [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}})；区分解释执行、补充元数据和其他能力时，回到 [边界与 trade-off]({{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}})。

**这条链的关键，是把程序集身份、方法准备和本次执行分别核对，再用同一方法的两次调用证明转换结果如何复用。**
