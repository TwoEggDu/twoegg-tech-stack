---
title: "IL2CPP 实现分析索引｜运行时地图、核心机制与产物排查"
slug: "il2cpp-series-index"
type: "reading-guide"
layout: "engine-directory"
reading_kind: "runtime-index"
topic_key: "engine"
reading_title: "IL2CPP 实现分析"
description: "从 C# 到 native，沿转换器、元数据与运行时理解 Unity AOT 的执行链路。"
date: "2026-04-15"
lastmod: "2026-10-26"
weight: 49
series: "dotnet-runtime-ecosystem"
series_id: "il2cpp-index"
tags: [IL2CPP, Unity, AOT, Index]
---

本系列按“产物地图 → 八篇核心机制 → 产物排查”组织，共 10 篇正文，另与 HybridCLR 共用线程接入专题和泛型调用综合实验。先建立 metadata、native code 与 runtime 的分工，再进入 IL2CPP 的构建与执行链路，最后衔接 HybridCLR。

## 前置知识

先按 [C++ 公共基础]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}}) 补值与借用、对象/所有权、函数指针、布局/存储与编译链接。CXX-01～11 公共正文已齐备；IL2CPP 专项跟读、托管语义与目标工程验证仍分别推进。

建议先完成 [ECMA-335 前置主线]({{< relref "engine-toolchain/ecma335-series-index.md" >}})。读 D3 前区分 [类型信息准备与静态初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})，读 D5 前确认 [签名与泛型上下文]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}})。

## 公共基础之后，补 IL2CPP 的约定

已有运行时地图与 D2～D6 可作为专项材料。接下来按以下顺序补跟读，均不计入当前 10 篇正文：

1. **C#、IL 与生成代码对应阅读（待整理）**：固定一个构建样本，逐句解释参数、调用入口与返回值；接公共函数指针和构建课。
2. **metadata、对象与存储边界（待补）**：接公共布局/对象寿命课，分清描述、运行时对象、代码入口与 GC 协作，避免把 C++ 的析构规则直接套到托管对象。
3. **方法分派与泛型接线（待补）**：接公共模板课和 CLI 调用/泛型知识，对照生成调用与 runtime 表；C++ 模板实例化不能替代托管泛型语义。

先建立产物地图，再在读到对应 D 篇前补专项；不用等待所有高级内容完成才开始第一轮阅读。具体生成代码和布局随版本与平台核对，公开正文遵守源码许可边界。

## 推荐阅读顺序

先读 [IL2CPP 运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}})，再按 D1～D8 阅读。遇到启动失败或文件混用时，跳到后面的产物排查专题。

## 核心机制

| # | 主题 | 一句话 |
|---|------|--------|
| D1 | [架构总览：从 C# → C++ → native 的完整管线]({{< relref "engine-toolchain/il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md" >}}) | il2cpp.exe + libil2cpp + global-metadata 三件套的协作关系 |
| D2 | [il2cpp.exe 转换器：IL → C++ 代码生成策略]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md" >}}) | 转换器怎么把 CIL 指令映射到 C++ 函数调用 |
| D3 | [libil2cpp runtime：MetadataCache、Class、Runtime 三层结构]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md" >}}) | 运行时的类型初始化和方法分派入口 |
| D4 | [global-metadata.dat：格式、加载与 runtime 的绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}) | metadata 文件的二进制布局和运行时查找路径 |
| D5 | [泛型代码生成：共享、特化与 Full Generic Sharing]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) | 引用类型共享 + 值类型特化的代码膨胀控制策略 |
| D6 | [GC 集成：BoehmGC 的接入层、write barrier 与 finalization]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) | 根、写入协议、终结与托管／native 内存增长的区分 |
| D7 | [ECMA-335 覆盖度：哪些支持、哪些不支持、为什么]({{< relref "engine-toolchain/il2cpp-ecma335-coverage-supported-unsupported.md" >}}) | Reflection.Emit、动态程序集等缺失特性的技术原因 |
| D8 | [Managed Code Stripping：裁剪策略与 link.xml]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) | UnityLinker 的裁剪级别和保留规则 |

---

## 共享运行时专题

D5 之后可读 [泛型调用综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}})：同一个 `Echo<T>` 从 CLI 签名追到 AOT 符号与运行时分派，比较显式实例引用和补充元数据。已在本机自定义 Unity、固定 v9 组合完成两份 Player、四组独立运行与原生断点验证；不推广为全平台结果。

D3 与 D6 之后可读 [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})：串起托管线程、原生回调 wrapper、解释器 TLS、GC 根与退出清理。正文归 HybridCLR，只维护一份；其中托管工作线程已补固定 v9 环境的 Player 返回值、缓存与清理调用链记录；原生插件单线程回调与在途停止也已实测；持续跨片段的串行托管写入见后续实验；长期GC压力、原生回调并发写和其他平台仍待补。

## 配套实验仓库

读完 D5 后，用 P0/P1 两份工程对照显式实例引用、主包产物和原生调用路径。三个专栏共用公开仓库 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)；调用链与泛型实验对应 [固定提交 `e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)。

- [复现说明与固定环境](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/README.md)：包含 Add、generic-p0、generic-p1 三个隔离工程，先 checkout 到对应提交再运行。
- [本次实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md)：构建标识、结果摘要、DLL 哈希与 CLI 签名输出。
- 对象保留与托管工作线程：[固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，可查看[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/projects/lifetime-threads)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)。

原生插件回调与在途停止也已补入 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})：对照已登记线程与插件新建线程，核对 wrapper、临时接入／清理和 Join 后释放。本轮对应 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb)，入口为[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/b9156325820312f4d290c4af8158f76a3a6f9beb/projects/native-callbacks)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/results.json)。

原生回调下的 [GC引用保留与有界压力]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 也已补入：Normal句柄、无上下文句柄的活跃回调引用，以及1024回调的分配／引用修改三组各运行三个Player。线程所有权见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮固定提交为 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，可查看[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)。

后续 [对象快照与活跃根区间验证]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 也已完成：实际GC查询中，活跃解释帧报告288字节，目标引用位于其中；解释方法返回、线程仍等待时返回空区间。另取21份对象快照，核对Normal句柄的保留路径及Free后的目标变化，并用官方读取器交叉校验。活跃／空闲／退出关系见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮固定提交为 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，入口为[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)；旧 `449a994` 仍为前轮实验基线。

本轮已补入 [同一增量周期的持续引用修改]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}})：AOT／hot各3个普通与3个调试Player，在连续12帧more=True、周期不变时写字段、引用数组和含引用结构体，关联实际标记、屏障、脏页与重扫，checksum12078。[线程篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})补充预热常驻worker与串行握手；不是外部原生回调或并发写验收。实验仓库本地记录为 `evidence/2026-10-25-incremental-barriers/`，build_id `70a8a176-4eee-43b2-8aa8-a75754ac0bfb`，本轮固定提交为 [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/results.json)。

当前验证范围是本机自定义 Unity `2022.3.62f3_96770f904ca7`、固定 HybridCLR v9 组合与 Windows x64 Development Player。已新增 [对象保留与释放对照]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 和 [托管工作线程实测]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})；完整动态根路径归因、写屏障反事实与完整路径、长期压力、多原生线程竞争、性能和其他平台继续按专题的待验证项阅读。

## 共享资源挂载实验

[Prefab／AssetBundle 资源挂载对照]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md#resource-mounting-player" >}}) 已在固定自定义 Editor／v9／Windows x64 完成 code、bundle、late、unregistered、resources 五组各三个Player。区分DLL可执行、启动清单登记、组件字段/引用恢复与生命周期；实验记录为 `evidence/2026-10-26-resource-mounting/`，本轮固定提交为 [74da063](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/74da063e0fe2a4cbac894acfa77d3b0df4074356)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/results.json)。它是实现层样本，不表示全平台或字段版本兼容性已验收。

## 共享类型初始化实验

[类型初始化与首次调用]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#type-initialization-player" >}}) 补入最终AOT/hot DLL标志与自有CIL、四组问题及失败捕获边界对照。30个普通Player中27完成、3个热更内部catch异常逃逸，另有2个native取证进程；Ping缓存已准备不等于方法体已执行。[runtime篇]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md#type-initialization-runtime" >}})说明职责区别，[源码起步篇]({{< relref "engine-toolchain/hybridclr-source-reading-start.md#type-initialization-first-call" >}})说明证据分层。本轮固定提交为 [46ac71c](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/46ac71c74cd068bc177d653dd143b7bb9dc482bd)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/results.json)。

## 共享异常传播实验

[跨 AOT／解释器异常传播]({{< relref "engine-toolchain/runtime-cross-exception-handling-seh-setjmp-interpreter.md#exception-flow-player" >}}) 用相同自有DLL比较原始运行时与新工程内的单点反事实。两版96普通Player、8个native进程：活动imi提前切换导致查错EH表，原始3个内部catch/finally缺失对照在延后赋值后恢复，其他方向与抛出对照保持原结果。[类型初始化篇]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#initializer-escape-cause" >}})同步后续证据；本轮固定提交为 [f5ba510](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/f5ba510382e170ac06d2fd7033db363e6c58f62e)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/results.json)与[公开最小补丁](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/patches/exception-flow-delay-active-imi.patch)。不表示上游或其他平台已修复。

## 共享 async／UniTask 实验

[async／UniTask Player 实测]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md#async-unitask-player" >}})沿用固定自定义 Editor／v9 环境，以 60 普通＋12 native 进程区分同步完成、两次跨帧恢复和前后异常。实际 class／struct 热更状态机都通过现有 native builder 调用解释器 MoveNext；on/off 对照没有产生 metadata 缺失失败，不把补充后的成功写成修复。样本保留整个 UniTask 程序集，历史 Android 根因与其他平台仍待验证。本轮固定提交为 [b34ac10](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b34ac104fa72504d1d2a480ca936ea57a7a0fb22)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/results.json)。

## 按问题查阅

| 问题 | 阅读入口 |
|---|---|
| 分不清 metadata、GameAssembly 与 runtime | [运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) |
| 要理解 metadata 文件结构与绑定 | [D4：格式、加载与绑定]({{< relref "engine-toolchain/il2cpp-global-metadata-dat-format-loading-binding.md" >}}) |
| 启动崩溃，怀疑文件混用或符号不匹配 | [产物一致性排查]({{< relref "engine-toolchain/il2cpp-global-metadata-deep-dive.md" >}}) |
| GC 后内存不下降，或退出后对象仍存活 | [D6：内存分类与生命周期对照]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) → [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}}) |
| SDK 线程回调热更方法、线程退出后状态未释放 | [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) |
| 泛型方法不能执行 | [D5：泛型代码生成]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md" >}}) → [HybridCLR AOT 泛型专题]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) |
| 反射目标被裁剪 | [D8：裁剪与保留规则]({{< relref "engine-toolchain/il2cpp-managed-code-stripping-linker-linkxml.md" >}}) |

## 延伸阅读

理解 IL2CPP 的限制后，可以接着看 HybridCLR 怎么在 IL2CPP 之上补回解释执行能力：

- [HybridCLR 源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}})：准备版本、调试工程和第一条调用链。
- [HybridCLR 系列索引]({{< relref "engine-toolchain/hybridclr-series-index.md" >}})：从基础机制进入案例与工程防线。

## 接下来补强什么

源码阅读起步、三栏泛型调用、对象保留对照和托管工作线程已完成各自固定环境的 Player 验证。对象快照与实际活跃根区间也已采证；后续补完整动态根路径归因、长期原生回调压力与固定采样条件的性能测量；已有结果不代表全系列经过统一版本验收。

## 同一自有程序的连续跟读

| 入口 | 已有内容与证据 | 仍待验证 |
|---|---|---|
| [ICPP-1 / D2]({{< relref "engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md#icpp-1" >}}) | 完整C#输入、真实桌面CIL、方法身份与返回 | 同源Unity IL/stripped IL、生成物与native路径 |
| [ICPP-2 / D3]({{< relref "engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md#icpp-2" >}}) | 同对象的描述/实例/字段/持有表；复用既有Player保留/快照 | 新Counter的Unity布局/实际对象对应与根归因 |
| [ICPP-3 / D5]({{< relref "engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md#icpp-3" >}}) | 同输入的虚/泛型CIL；复用已有四组泛型Player | 新样本native虚分派、注册和泛型上下文 |

三项嵌入既有正文，不增加篇数；桌面检查、旧Player证据与新Unity待验分开。
