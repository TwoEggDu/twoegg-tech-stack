---
title: "ECMA-335 基础层｜读懂 IL2CPP 与 HybridCLR 的前置主线"
slug: "ecma335-series-index"
type: "reading-guide"
layout: "engine-directory"
reading_kind: "runtime-index"
topic_key: "engine"
reading_title: "ECMA-335：读懂 IL2CPP 与 HybridCLR"
date: "2026-04-15"
lastmod: "2026-10-25"
description: "从 DLL、类型和签名，到 CIL、调用、程序集加载、静态初始化和反射，建立理解 IL2CPP 与 HybridCLR 的规范基础。"
weight: 8
series: "dotnet-runtime-ecosystem"
series_id: "ecma335"
series_title: "ECMA-335 前置主线"
series_role: "index"
series_order: 0
series_entry: true
series_audience: ["Unity 客户端工程师", "准备深入 IL2CPP 与 HybridCLR 的读者"]
series_level: "进阶"
series_best_for: "能写 C#，希望读懂 AOT 泛型、补充元数据、解释执行与调用桥接"
series_summary: "用 CLI 规范解释程序怎样被描述、解析、初始化和执行，再进入 IL2CPP 与 HybridCLR 的实现。"
series_intro: "元数据里有一个方法，为什么运行时还是不能调用它？这条主线逐层拆开描述、解析和执行的边界。"
series_reading_hint: "先读导读和九篇主线；原生互操作、并发与文件格式按需深入。"
tags: [ECMA-335, IL2CPP, HybridCLR, Index]
---

热更 DLL 已经加载，类型也能通过反射找到，第一次调用却失败了。要解释这种现象，仅记住“缺 metadata”或“缺 AOT 泛型”还不够：需要分清文件中的描述、运行时的解析结果，以及实际可执行的方法实现。

本系列主要为 **IL2CPP 与 HybridCLR** 服务。我们选取读懂这两套系统所需的 CLI 语义，用最小 C#／IL 示例把它们连起来。读完应能回答：一个调用引用了谁、参数如何被描述、类型何时初始化，以及为什么找到成员仍不代表能够执行。

规范基线为 ECMA-335 第六版。具体运行时的版本与功能边界由后续实现篇说明；泛型共享、裁剪和桥接策略不能反写成 CLI 的规范要求。

## 推荐阅读顺序

导读：[术语约定：runtime、toolchain、metadata 与 execution engine]({{< relref "engine-toolchain/ecma335-terminology-conventions.md" >}})。

| 顺序 | 文章 | 读完要能回答 |
|---|---|---|
| 1 | [DLL 与 CLI Metadata：文件入口、表、堆、token 和方法体]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) | DLL 里有哪些信息，如何定位一段 IL？ |
| 2 | [类型系统：值、引用、泛型与接口]({{< relref "engine-toolchain/ecma335-type-system-value-ref-generic-interface.md" >}}) | 类型身份、存储语义与可执行代码为什么要分开？ |
| 3 | [签名与引用解析：一个泛型调用如何找到目标方法]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}}) | token、签名和泛型上下文怎样组成完整调用目标？ |
| 4 | [CIL 与栈机模型：指令语义和最小验证规则]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) | 方法体怎样表达计算，哪些 IL 约束必须成立？ |
| 5 | [方法调用与异常处理：call、callvirt、constrained. 与 EH]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md" >}}) | 参数、分派、装箱和异常展开怎样连接？ |
| 6 | [程序集身份、引用与加载边界]({{< relref "engine-toolchain/ecma335-assembly-model-identity-versioning-loading.md" >}}) | 依赖解析、程序集加载和补充元数据分别解决什么？ |
| 7 | [类型加载与初始化：.cctor、beforefieldinit 与泛型静态字段]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}}) | 找到了类型，是否已经执行它的静态初始化？ |
| 8 | [对象布局、GC 契约与终结语义]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) | AOT 与解释执行怎样共同维护对象生命周期？ |
| 9 | [反射与特性：元数据可见到方法可调用]({{< relref "engine-toolchain/ecma335-custom-attributes-reflection-encoding.md" >}}) | 裁剪、特性构造与执行能力如何区分？ |

原 A0～A13 标号仅作为旧文对应关系保留，不再代表必读顺序。旧文章地址继续有效。

## 进入 IL2CPP 与 HybridCLR

1. 先读 [IL2CPP 实现分析]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})，将 DLL 输入、构建产物和 runtime 职责对应起来。
2. 遇到泛型问题，接着读 [泛型共享桥接篇（原 A7／Bridge-D）]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) 和 [AOT 泛型与补充元数据]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}})。共享规则属于实现层，要结合版本理解。
3. 准备动手跟源码时，先看 [HybridCLR 源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})，再进入 [HybridCLR 系列]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})。ABI、解释器 IR、GC 接入、裁剪、async 和工程排障继续在该系列展开。
4. 完成签名、调用和泛型前置后，做 [泛型调用综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})：用同一个 `Echo<T>` 对照实际 call／MethodSpec、AOT 产物和 Player 执行路径。该案例已在本机自定义 Unity 与固定 v9 组合实测，三栏共用一篇，不改变导读 + 九篇规范主线。

## 配套实验仓库

完成签名、调用与泛型前置后，对照实际 DLL 的 MethodSpec 和签名输出，再看对应 Player 结果。三个专栏共用公开仓库 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)；调用链与泛型实验对应 [固定提交 `e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)。

- [复现说明与固定环境](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/README.md)：包含 Add、generic-p0、generic-p1 三个隔离工程，先 checkout 到对应提交再运行。
- [本次实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md)：构建标识、结果摘要、DLL 哈希与 CLI 签名输出。
- 对象保留与托管工作线程：[固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，可查看[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/projects/lifetime-threads)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)。

原生插件回调与在途停止也已补入 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})：对照已登记线程与插件新建线程，核对 wrapper、临时接入／清理和 Join 后释放。本轮对应 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb)，入口为[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/b9156325820312f4d290c4af8158f76a3a6f9beb/projects/native-callbacks)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/results.json)。

原生回调下的 [GC引用保留与有界压力]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 也已补入：Normal句柄、无上下文句柄的活跃回调引用，以及1024回调的分配／引用修改三组各运行三个Player。线程所有权见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮固定提交为 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，可查看[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)。

后续 [对象快照与活跃根区间验证]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 也已完成：实际GC查询中，活跃解释帧报告288字节，目标引用位于其中；解释方法返回、线程仍等待时返回空区间。另取21份对象快照，核对Normal句柄的保留路径及Free后的目标变化，并用官方读取器交叉校验。活跃／空闲／退出关系见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮固定提交为 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，入口为[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)；旧 `449a994` 仍为前轮实验基线。

当前验证范围是本机自定义 Unity `2022.3.62f3_96770f904ca7`、固定 HybridCLR v9 组合与 Windows x64 Development Player。已新增 [对象保留与释放对照]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 和 [托管工作线程实测]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})；完整动态根路径归因、持续跨片段引用修改、长期压力、多原生线程竞争、性能和其他平台继续按专题的待验证项阅读。

## 带着问题按需查阅

| 场景 | 入口 |
|---|---|
| SDK、原生插件、托管与 native 互调 | [P/Invoke 与 Marshaling（原 A10）]({{< relref "engine-toolchain/ecma335-pinvoke-native-interop-marshaling-spec.md" >}}) → [ABI 桥接]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) |
| 对象无法回收、事件与容器保留 | [GC 主线与生命周期实验]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) → [IL2CPP GC 与内存观察]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) |
| 原生线程回调热更代码、解释器线程状态与 GC | [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})，属于实现专题，不改变规范主线 |
| 多线程可见性、volatile 和屏障 | [线程内存模型（原 A12）]({{< relref "engine-toolchain/ecma335-threading-memory-model-volatile-barriers.md" >}}) |
| 分析 DLL、损坏文件或 coded index | [PE／CLI 文件格式附录（原 A13）]({{< relref "engine-toolchain/ecma335-cli-file-format-pe-cli-header.md" >}})；基础定位路径已并入第 1 篇 |
| IL 重写、unsafe 或验证工具报错 | [IL 验证附录（原 A8）]({{< relref "engine-toolchain/ecma335-cli-verification-il-type-safety.md" >}})；最小规则已并入第 4 篇 |
| 旧 .NET 安全模型、strong name 与 CAS | [历史背景（原 A11）]({{< relref "engine-toolchain/ecma335-cli-security-strong-name-cas.md" >}})，不作为热更新前置 |

如果目标是 CoreCLR、Mono 或 LeanCLR，也可以复用这些规范知识，再从 [.NET Runtime 生态总图]({{< relref "engine-toolchain/dotnet-runtime-ecosystem-series-index.md" >}}) 进入相应实现。

**这条主线的终点，是能区分程序的描述、解析、初始化与执行，再解释 IL2CPP 和 HybridCLR 为什么需要各自的机制。**
