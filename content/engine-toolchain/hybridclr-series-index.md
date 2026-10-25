---
date: "2026-03-26"
title: "HybridCLR 系列索引｜先读哪篇，遇到什么问题该回看哪篇"
description: "从源码阅读起步进入运行时主链，再按泛型、工具链、排障和商业能力分组查阅。"
slug: "hybridclr-series-index"
type: "reading-guide"
layout: "engine-directory"
reading_kind: "runtime-index"
topic_key: "engine"
reading_title: "HybridCLR"
weight: 29
featured: false
tags:
  - "Unity"
  - "IL2CPP"
  - "HybridCLR"
  - "Index"
  - "Architecture"
series: "HybridCLR"
series_id: "hybridclr"
series_role: "index"
series_order: 0
series_nav_order: 30
series_title: "HybridCLR"
series_audience:
  - "Unity 客户端"
  - "热更新 / 工具链"
series_level: "进阶"
series_best_for: "当你想把 HybridCLR 从 build-time、runtime 到排障链路一起看清"
series_summary: "把 HybridCLR 的 build-time、runtime、AOT 泛型和排障链路收进一张入口图"
series_intro: "这组文章不是只讲某个按钮或单点 API，而是把 HybridCLR 从运行时主链、元数据、AOT 泛型到工具链、排障和项目落地拆成可顺读的一条线。先看入口页再沿着主线前进，能更快知道当前问题属于哪一层。"
series_reading_hint: "先看源码阅读起步和基础主线；案例与泛型手册按需查阅，商业能力作为选读。"
lastmod: "2026-10-25"
---

HybridCLR 把构建准备、动态装载、方法转换和解释执行连接到 IL2CPP。这个专栏帮助你先分清问题在哪一层，再找到应读的机制、源码入口或工程案例。

目前有 **30 篇编号正文、6 篇前置与桥接，以及源码阅读起步、线程接入、泛型调用综合实验 3 篇专题（共 39 篇）**，索引另计。其中两篇前置和泛型共享桥接与 ECMA-335 共用正文；综合实验由 ECMA-335、IL2CPP 和 HybridCLR 三栏共用一篇。商业能力属于选读研究，不能替代社区版主链的学习与验证。

## 推荐阅读顺序

| 阶段 | 入口 | 读到什么程度再继续 |
|---|---|---|
| C++ 公共基础 | [公共专栏]({{< relref "engine-toolchain/cpp-foundations-series-index.md" >}})，按需读函数指针、内存布局、对象寿命与构建 | 能解释指针/参数访问与原生调用；缺课处按目录补齐，不以目录存在视为掌握 |
| 规范前置 | [ECMA-335 前置主线]({{< relref "engine-toolchain/ecma335-series-index.md" >}}) | 分清 token、签名、调用、加载和初始化 |
| 实现底座 | [IL2CPP 索引]({{< relref "engine-toolchain/il2cpp-series-index.md" >}})，先读 [运行时地图]({{< relref "engine-toolchain/il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md" >}}) | 分清 metadata、native code 与 runtime |
| 动手准备 | [HybridCLR 源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) | 对齐版本、构建与进程，知道首轮断点看什么 |
| 基础主线 | HCLR-1～5，穿插泛型共享和 ABI 桥接 | 能描述一段热更方法如何被装载、转换与执行 |
| 三栏综合实验 | [泛型调用：签名 → AOT → 执行]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) | 用同一调用区分目标身份、原生实现、补充方法体与解释器内联 |
| 线程与 GC | GC 桥接篇 → [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) | 分清 runtime 登记、活跃根、业务同步和主线程限制 |
| 工程收束 | HCLR-6～9 | 能划分边界、按层排障并识别性能成本 |
| 按需深入 | 下方案例、泛型手册与商业能力 | 按自己的问题选读，不必把所有文章当成前置 |

只需补某个概念时，可直接读 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}}) 或 [类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})，不必重新顺读整个前置系列。

源码起步篇保留 v6.1.0 的历史阅读样本；起步与调用链篇已另用固定的 v9 配套组合，在本机自定义 Unity 2022.3.62f3 完成 Player 原生断点实验。两条版本基线分别标明，不代表全系列或其他平台都已验证。

## 配套实验仓库

先跑 Add 的首次转换与再次调用，再跑泛型四组对照，观察补充方法体、内联与缓存。三个专栏共用公开仓库 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)；调用链与泛型实验对应 [固定提交 `e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)。

- [复现说明与固定环境](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/README.md)：包含 Add、generic-p0、generic-p1 三个隔离工程，先 checkout 到对应提交再运行。
- [本次实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md)：构建标识、结果摘要、DLL 哈希与 CLI 签名输出。
- 对象保留与托管工作线程：[固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，可查看[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/projects/lifetime-threads)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)。

原生插件回调与在途停止也已补入 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})：对照已登记线程与插件新建线程，核对 wrapper、临时接入／清理和 Join 后释放。本轮对应 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb)，入口为[样本工程](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/b9156325820312f4d290c4af8158f76a3a6f9beb/projects/native-callbacks)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/results.json)。

原生回调下的 [GC引用保留与有界压力]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 也已补入：Normal句柄、无上下文句柄的活跃回调引用，以及1024回调的分配／引用修改三组各运行三个Player。线程所有权见 [线程接入篇]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})。本轮固定提交为 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，可查看[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)。

当前验证范围是本机自定义 Unity `2022.3.62f3_96770f904ca7`、固定 HybridCLR v9 组合与 Windows x64 Development Player。已新增 [对象保留与释放对照]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 和 [托管工作线程实测]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})；根快照、持续跨片段引用修改、长期压力、多原生线程竞争、性能和其他平台继续按专题的待验证项阅读。

## 公共基础之后，补 HybridCLR 的约定

公共 C++ 说明指针、调用和内存的规则；这里还要把规则对应到方法指针、参数槽、解释帧和运行时状态。CLI 语义与 IL2CPP 实现底座继续沿上表阅读，不能用 C++ 模板或 RAII 替代。

**先读已有材料**：下方 ABI、解释器、泛型共享和 GC 桥接，以及源码阅读起步。**专项待补顺序**：

1. 方法指针、参数槽与返回值的逐句阅读：接公共函数指针/布局课，补一段调用的类型与数据流练习。
2. metadata 索引、方法体与解释帧的对应阅读：接 CLI、公共字节访问/存储课，补输入边界、缓存和首次/再次调用的状态变化。
3. AOT/解释执行之间的对象与线程协作：接公共并发课与现有 GC 专题，补项目专用的有效访问条件和验证记录。

这些专项单元仍待整理或补写，不计入已有编号正文与六篇桥接数量。它们训练怎样读具体实现，现有桥接继续解释机制。

## 前置与桥接

| 入口 | 适合插入的位置 |
|---|---|
| [DLL 与 CLI Metadata：文件入口、表、堆、Token 和方法体]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) | HCLR-1 前；完整前置也可走 ECMA-335 主线 |
| [CIL 与栈机模型：指令语义和最小验证规则]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) | HCLR-1 前；理解 CIL 与内部指令的区别 |
| [HybridCLR 桥接篇｜从泛型语义到 IL2CPP 代码共享]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) | HCLR-2 后；遇到共享与实例化问题时回看 |
| [HybridCLR 桥接篇｜ABI 与跨边界调用：为什么 interpreter 调 AOT 需要一层 bridge]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) | HCLR-3 后、HCLR-5 前 |
| [HybridCLR 桥接篇｜解释器基础：dispatch loop、stack vs register、IR 是什么]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}}) | HCLR-27 前；解释器结构背景 |
| [HybridCLR 桥接篇｜IL2CPP GC 模型：BoehmGC、GC root、write barrier 与解释器的关系]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}}) | GC、热重载及内存专题前 |

如果对 asmdef、Roslyn 和 Player 脚本编译不熟悉，可先补 [Unity 脚本编译管线]({{< relref "engine-toolchain/unity-script-compilation-pipeline-series-index.md" >}})，它是外部公共前置。

## 源码跟读与运行时实验

| 入口 | 读完要能回答 |
|---|---|
| [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) | 怎样对齐版本、构建与首轮调用断点？ |
| [泛型调用综合实验]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) | 为什么同一 `Echo<T>` 的 string、int、结构体调用会走不同路径？补元数据改变了什么？ |
| [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) | 谁接入线程、谁持有解释器状态和根、谁负责退出？ |

泛型综合实验已在固定 v9 配套组合、本机自定义 Editor 下完成两份 Player 与四组运行，包含原生共享实现和补充方法体后内联解释的证据。线程专题与 IL2CPP 共用，放在源码起步和 GC 桥接之后；已补托管线程与原生插件回调的 Player 结果，桌面探针保留为早期示例验证。

## 基础主线与工程收束

| 编号 | 文章 |
|---|---|
| HCLR-1 | [HybridCLR 原理拆解｜从 RuntimeApi 到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md" >}}) |
| HCLR-2 | [HybridCLR AOT 泛型与补充元数据｜为什么代码能编译，到了 IL2CPP 运行时却不一定能跑]({{< relref "engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md" >}}) |
| HCLR-3 | [HybridCLR 工具链拆解｜LinkXml、AOTDlls、MethodBridge、AOTGenericReference 到底在生成什么]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md" >}}) |
| HCLR-4 | [HybridCLR MonoBehaviour 与资源挂载链路｜为什么资源上挂着热更脚本也能正确实例化]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}}) |
| HCLR-5 | [HybridCLR 调用链实战｜跟着一个热更方法一路走到 Interpreter::Execute]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}}) |
| HCLR-6 | [HybridCLR 的边界与 trade-off｜不要把补充 metadata、AOT 泛型、MethodBridge、MonoBehaviour、DHE 混成一件事]({{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}}) |
| HCLR-7 | [HybridCLR 最佳实践｜程序集拆分、加载顺序、裁剪与回归防线]({{< relref "engine-toolchain/hybridclr-best-practice-assembly-loading-strip-and-guardrails.md" >}}) |
| HCLR-8 | [HybridCLR 故障诊断手册｜遇到报错时先判断是哪一层坏了]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}}) |
| HCLR-9 | [HybridCLR 性能与预热策略｜哪些逻辑留在解释器，哪些该前移或回到 AOT]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}}) |

## 能力边界与选型

HCLR-10～12 涉及高级能力与选型，HCLR-13 用来回看常见混淆。先掌握基础主线，再根据项目需要阅读。

| 编号 | 文章 |
|---|---|
| HCLR-10 | [HybridCLR Full Generic Sharing｜为什么它不是补充 metadata 的升级版]({{< relref "engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md" >}}) |
| HCLR-11 | [HybridCLR DHE｜为什么它不是普通解释执行更快一点]({{< relref "engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md" >}}) |
| HCLR-12 | [HybridCLR 高级能力选型｜社区版主线、补 metadata、Full Generic Sharing、DHE 分别该在什么时候上]({{< relref "engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md" >}}) |
| HCLR-13 | [HybridCLR 高频误解 FAQ｜10 个最容易混掉的判断]({{< relref "engine-toolchain/hybridclr-faq-10-most-confused-judgments.md" >}}) |

## 案例、崩溃与构建流程

| 编号 | 文章 |
|---|---|
| HCLR-14 | [HybridCLR 真实案例诊断｜从 TypeLoadException 到 async 栈溢出，一次完整的 native crash 符号化分析]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}}) |
| HCLR-15 | [HybridCLR 崩溃定位专题｜从 native crash 调用栈读出 HybridCLR 的层次]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}}) |
| HCLR-16 | [HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}}) |
| HCLR-17 | [HybridCLR 案例续篇｜async 崩溃的真正根因与两种修法]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}}) |
| HCLR-18 | [HybridCLR 案例｜Dictionary&lt;ValueTuple, 热更类型&gt; 的 MissingMethodException 与 object 替代法]({{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}) |

## AOT 泛型工程化

这六篇分别承担决策、规则、写法、协作关系、案例检索和自动检查职责。先用 HCLR-19 判断问题，再选择对应手册，避免把每篇当成同一套通用修法。

| 编号 | 文章 |
|---|---|
| HCLR-19 | [HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) |
| HCLR-20 | [HybridCLR 共享类型判断｜为什么报错是 ValueTuple&lt;int,string&gt;，你写的却可能是 ValueTuple&lt;int,object&gt;]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) |
| HCLR-21 | [HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}) |
| HCLR-22 | [HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}) |
| HCLR-23 | [HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排]({{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}}) |
| HCLR-24 | [HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}}) |

## 商业能力研究（选读）

下列文章讨论公开文档、社区版约束与架构推断。商业实现不公开的部分不能作为已核验源码结论；官方性能数字需要连同版本、测试条件和原始出处理解，也不代表本项目实测。目录不把这些数字作为效果承诺。

| 编号 | 文章 |
|---|---|
| HCLR-25 | [HybridCLR DHE 内部机制｜dhao 文件格式、差分算法与函数级分流实现]({{< relref "engine-toolchain/hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md" >}}) |
| HCLR-26 | [HybridCLR DHE 函数注入与脏函数传染｜为什么改一个函数会导致一片函数走解释器]({{< relref "engine-toolchain/hybridclr-dhe-function-injection-dirty-contagion.md" >}}) |
| HCLR-27 | [HybridCLR 解释器指令优化｜标准优化与高级优化在 HiOpcode 层到底做了什么]({{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}}) |
| HCLR-28 | [HybridCLR 程序集热重载｜IL2CPP 为什么不能卸载程序集，热重载版怎么做到的]({{< relref "engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md" >}}) |
| HCLR-29 | [HybridCLR 代码加密与访问控制｜DLL 加密怎么与解释器配合，访问控制策略拦在哪一层]({{< relref "engine-toolchain/hybridclr-code-encryption-access-control-policy.md" >}}) |
| HCLR-30 | [HybridCLR 加载加速、metadata 内存优化与 Incremental GC 适配｜商业版在运行时资源消耗上做了什么]({{< relref "engine-toolchain/hybridclr-load-acceleration-metadata-memory-incremental-gc.md" >}}) |

## 如果你不是系统读，而是带着问题来查

| 问题 | 先读哪里 |
|---|---|
| 不知道怎么准备源码与调试环境 | [源码阅读起步]({{< relref "engine-toolchain/hybridclr-source-reading-start.md" >}}) |
| 能加载 DLL，但调用进不了解释器 | [HCLR-5]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md" >}})；先确认方法身份与实际调用路径 |
| 不确定补 metadata、保 AOT 实例还是防裁剪 | [HCLR-19]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) → [HCLR-22]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}) |
| 热更 MonoBehaviour 挂载失败 | [HCLR-4]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md" >}}) → [HCLR-8]({{< relref "engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md" >}}) |
| native crash 或启动失败 | [HCLR-15]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}})；文件混用另看 [IL2CPP 产物一致性排查]({{< relref "engine-toolchain/il2cpp-global-metadata-deep-dive.md" >}}) |
| Generate 与最终打包不一致 | [HCLR-16]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}}) → [HCLR-24]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}}) |
| GC 后热更对象仍存活，或 native 内存增长 | [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}})；容器与事件实验见 [ECMA GC 前置]({{< relref "engine-toolchain/ecma335-memory-model-object-layout-gc-contract-finalization.md" >}}) |
| SDK 回调、工作线程调用或退出清理异常 | [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}})；async 调度另看 [Unity 异步运行时]({{< relref "engine-toolchain/unity-async-runtime-series-index.md" >}}) |
| 首调卡顿或跨边界调用过多 | [HCLR-9]({{< relref "engine-toolchain/hybridclr-performance-and-prejit-strategy.md" >}})；需要补设备测量来支持最终判断 |
| 是否要用 FGS、DHE 或热重载 | 先 [HCLR-6]({{< relref "engine-toolchain/hybridclr-boundaries-and-tradeoffs.md" >}})、12，再选择商业专题 |

## 当前边界与待补强

本系列已经覆盖机制、源码入口、案例和工程防线。配套实验仓库已提供可构建的隔离工程、复现脚本和固定提交证据；已有调用链、泛型、对象保留与托管线程的固定提交及证据入口；各平台接入与业务资源加载流程仍未覆盖。

基础 Add、三栏泛型调用实验之外，现已补入 [热更对象保留与释放]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) 和 [托管工作线程]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) 的 Player 记录。下一步仍是 GC 引用快照、活跃根区间、持续跨片段引用修改与长期压力验证，以及首调、预热、稳定执行和跨边界成本测量。

**先对齐版本和运行现场，再沿主链理解机制，最后按问题回到对应专题。**
