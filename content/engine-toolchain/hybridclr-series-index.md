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
lastmod: "2026-10-26"
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
| 基础主线 | 先按需补[解释器基础]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}})，再读 HCLR-1～5，穿插泛型共享与 ABI | 能描述一段热更方法如何被装载、转换与执行 |
| 三栏综合实验 | [泛型调用：签名 → AOT → 执行]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) | 用同一调用区分目标身份、原生实现、补充方法体与解释器内联 |
| 线程与 GC | [GC 桥接篇]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md" >}}) → [IL2CPP 与 HybridCLR 的线程接入]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) | 分清 runtime 登记、活跃根、业务同步和主线程限制 |
| 工程收束 | HCLR-6～9 | 能划分边界、按层排障并识别性能成本 |
| 按需深入 | 下方案例、泛型手册与商业能力 | 按自己的问题选读，不必把所有文章当成前置 |

只需补某个概念时，可直接读 [签名与引用解析]({{< relref "engine-toolchain/ecma335-signatures-and-reference-resolution.md" >}}) 或 [类型加载与初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md" >}})，不必重新顺读整个前置系列。

第一轮源码阅读统一使用 runtime v9.0.1／package v9.0.3，固定提交与调试入口见源码起步篇；v6.1.0 仅作明确标注的历史对照。现有 Player 证据限于本机自定义 Editor 的固定组合。商业资料另外注明文档版本与未验证部分，不与社区源码混用。

配图按问题放在对应正文：先用[整体架构图]({{< relref "engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md#runtime-architecture" >}})定位组件，再用[首次／再次调用时序]({{< relref "engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md#first-repeat-call" >}})跟执行。构建与排障可直接查[产物依赖图]({{< relref "engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md#build-artifacts" >}})、[修法决策图]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md#fix-decision-map" >}})；生命周期问题对应[活跃根区间]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md#active-root-range" >}})、[线程接入所有权]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md#callback-attachment" >}})和[回调退出清理时序]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md#callback-shutdown" >}})。

## 前置与桥接

| 入口 | 适合插入的位置 |
|---|---|
| [DLL 与 CLI Metadata：文件入口、表、堆、Token 和方法体]({{< relref "engine-toolchain/hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md" >}}) | HCLR-1 前；完整前置也可走 ECMA-335 主线 |
| [CIL 与栈机模型：指令语义和最小验证规则]({{< relref "engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md" >}}) | HCLR-1 前；理解 CIL 与内部指令的区别 |
| [HybridCLR 桥接篇｜从泛型语义到 IL2CPP 代码共享]({{< relref "engine-toolchain/hybridclr-bridge-il2cpp-generic-sharing-rules.md" >}}) | HCLR-2 后；遇到共享与实例化问题时回看 |
| [HybridCLR 桥接篇｜ABI 与跨边界调用：为什么 interpreter 调 AOT 需要一层 bridge]({{< relref "engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md" >}}) | HCLR-3 后、HCLR-5 前 |
| [HybridCLR 桥接篇｜解释器基础：dispatch loop、stack vs register、IR 是什么]({{< relref "engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md" >}}) | CIL 之后、HCLR-1／5 之前；先理解槽位、转换与分派 |
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
| HCLR-14 | [HybridCLR 真实案例诊断｜从 TypeLoadException 到 async native crash 的历史取证]({{< relref "engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md" >}}) |
| HCLR-15 | [HybridCLR 崩溃定位专题｜从 native crash 调用栈读出 HybridCLR 的层次]({{< relref "engine-toolchain/hybridclr-crash-analysis.md" >}}) |
| HCLR-16 | [HybridCLR 打包工程化｜GenerateAll 必须进 CI 流程，Development 一致性与 Launcher-only 场景]({{< relref "engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md" >}}) |
| HCLR-17 | [HybridCLR 案例续篇｜async 崩溃的证据边界与两条验证路线]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md" >}}) |
| HCLR-18 | [HybridCLR 案例｜Dictionary&lt;ValueTuple, 热更类型&gt; 的 MissingMethodException 与 object 替代法]({{< relref "engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md" >}}) |

## AOT 泛型工程化

按问题选读：HCLR-19 负责修法决策，20 负责共享规则，21 负责实例引用模板，22 串起工作流，23 做场景检索，24 负责验证防线。重复定义回链到对应正文，不需要连续重读六篇。

| 编号 | 文章 |
|---|---|
| HCLR-19 | [HybridCLR 修法决策｜DisStripCode、link.xml、补充元数据分别在什么时候用]({{< relref "engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md" >}}) |
| HCLR-20 | [HybridCLR 共享类型判断｜为什么报错是 ValueTuple&lt;int,string&gt;，你写的却可能是 ValueTuple&lt;int,object&gt;]({{< relref "engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md" >}}) |
| HCLR-21 | [HybridCLR DisStripCode 写法手册｜值类型、引用类型、嵌套泛型、委托分别该怎么写]({{< relref "engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md" >}}) |
| HCLR-22 | [HybridCLR AOTGenericReferences、DisStripCode、补 metadata 到底怎么配合]({{< relref "engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md" >}}) |
| HCLR-23 | [HybridCLR AOT 泛型高频坑型录｜UniTask、LINQ、Dictionary、委托、自定义泛型容器怎么排]({{< relref "engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md" >}}) |
| HCLR-24 | [HybridCLR AOT 泛型回归防线｜怎么把这些坑前移到 Generate、CI 和构建检查里]({{< relref "engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md" >}}) |

## 商业能力研究（选读）

下列文章讨论公开契约、适用前提与社区源码可确认的机制。商业实现不公开的部分不能作为已核验源码结论；官方性能数字需要连同版本、测试条件和原始出处理解，也不代表本项目实测。目录不把这些数字作为效果承诺。

| 编号 | 文章 |
|---|---|
| HCLR-25 | [HybridCLR DHE 工作流｜DHAO、MetaVersion 与函数级分流的公开契约]({{< relref "engine-toolchain/hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md" >}}) |
| HCLR-26 | [HybridCLR DHE 函数注入与脏函数传染｜为什么改一个函数会导致一片函数走解释器]({{< relref "engine-toolchain/hybridclr-dhe-function-injection-dirty-contagion.md" >}}) |
| HCLR-27 | [HybridCLR 解释器指令优化｜从社区版 HiOpcode 到商业优化的证据边界]({{< relref "engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md" >}}) |
| HCLR-28 | [HybridCLR 程序集热重载｜卸载条件、metadata 复用与状态恢复]({{< relref "engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md" >}}) |
| HCLR-29 | [HybridCLR 代码加固与访问控制｜保护对象、配置契约与能力边界]({{< relref "engine-toolchain/hybridclr-code-encryption-access-control-policy.md" >}}) |
| HCLR-30 | [HybridCLR 加载与内存优化｜社区源码路径、商业数据与增量 GC 边界]({{< relref "engine-toolchain/hybridclr-load-acceleration-metadata-memory-incremental-gc.md" >}}) |

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

## 配套实验与复现入口

实验仓库为 [hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)。先按下面的问题选择实验，再 checkout 对应固定提交；详细计数、哈希和原生事件保留在正文与实验记录中。

| 实验 | 主要回答的问题 | 固定记录 |
|---|---|---|
| [调用链与泛型四组对照]({{< relref "engine-toolchain/runtime-generic-call-experiment.md" >}}) | 目标身份、native 实现和解释路径怎样对应？ | [e0220099](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md) |
| [对象保留与托管线程]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) | 谁保留对象，线程状态怎样复用与退出？ | [a387d28](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md) |
| [原生线程回调]({{< relref "engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md" >}}) | wrapper 谁来接入，怎样等待回调结束再释放？ | [b915632](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md) |
| [回调下的 GC 保留与压力]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) | 句柄与活跃引用怎样跨收集保持可用？ | [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md) |
| [对象快照与活跃根区间]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) | 分配容量、实际报告区间与对象保留路径有什么区别？ | [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md) |
| [跨增量片段的引用修改]({{< relref "engine-toolchain/il2cpp-gc-integration-boehm-wbarrier-finalization.md" >}}) | 如何关联持续写入、屏障、脏页与重扫？ | [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/README.md) |
| [Prefab／AssetBundle 资源挂载]({{< relref "engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md#resource-mounting-player" >}}) | DLL可调用为何不等于资源组件可恢复？ | 五组各3个本地Player已验证；固定记录 [74da063](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/README.md) |
| [类型初始化与首次调用]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#type-initialization-player" >}}) | 加载、.cctor、转换缓存与方法体执行怎样区分？ | 30进程＋2份native记录；3个热更内部捕获差异保留，固定记录 [46ac71c](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/README.md) |
| [跨 AOT／解释器异常传播]({{< relref "engine-toolchain/runtime-cross-exception-handling-seh-setjmp-interpreter.md#exception-flow-player" >}}) | 内部catch与finally为何跳过，活动方法上下文如何保持一致？ | 96普通＋8native；原始3个逃逸由隔离单点反事实恢复，固定记录 [f5ba510](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/README.md) |
| [async／UniTask Player 实测]({{< relref "engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md#async-unitask-player" >}}) | 同步完成、真正挂起与恢复后异常怎样区分？ | 60 普通＋12 native；class／struct 两次恢复，metadata 对照均有可用 native builder；固定记录 [b34ac10](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/README.md) |
| [constrained.／装箱 Player 实测]({{< relref "engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md#constrained-boxing-player" >}}) | 值副本、ref、装箱和 fallback 分别修改什么，最终走哪种实现？ | 72 普通（69 完成／3 缺实现对照）＋27 native；按值补充后恢复，ref 仍 native；固定记录 [5638131](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/README.md) |
| [并发首次调用与类型初始化]({{< relref "engine-toolchain/ecma335-type-loading-and-initialization.md#concurrent-initialization-player" >}}) | 冷缓存、.cctor竞争、失败与泛型静态状态怎样分开验证？ | 54普通＋8native；成功发布、失败不重试、封闭类型隔离；固定记录 [8ac20ea](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b/evidence/2026-10-26-concurrent-initialization/README.md) |

上述 Player 证据限于本机自定义 Unity `2022.3.62f3_96770f904ca7`、固定 package v9.0.3／runtime v9.0.1 和 Windows x64。跨增量片段组将 GCMode 改为 Manual，采用常驻托管 worker 的串行握手；不是原生回调竞争或所有屏障路径的验收。其本地记录为 `evidence/2026-10-25-incremental-barriers/`，build_id `70a8a176-4eee-43b2-8aa8-a75754ac0bfb`，不能借用前轮提交充当本轮源码版本。

## 当前边界与待补强

已有机制说明、固定源码入口、公开实验工程与上述 Player 证据。历史 async 案例的根因仍需构建身份与修复前后复现来确认，不能把重复栈或一次配置差异当成确定因果。商业能力部分以公开契约为主，闭源内部实现保留未知。

接下来优先加强现有文章：

1. 调用语义：扩展 readonly／Nullable 与其他接口形态、跨 AOT／解释器 filter／嵌套 finally，以及循环／递归初始化；现有 constrained.／装箱与有界并发初始化样本见实验表。
2. 工程复现：扩展 async／UniTask 调度／取消与 metadata 缺失失败对照；资源 schema 演进和更广裁剪回归。
3. 性能测量：固定设备与构建，分别测首次准备、预热、稳定执行和跨边界调用。

GC 后续仍有完整动态根归因、屏障反事实、长期与多线程竞争等待验证项；现有样本不推广为所有平台。C++ 逐句跟读训练可补进 ABI、调用链和 GC 专题，不为增加数量另开重复文章。

**先对齐版本和运行现场，再沿主链理解机制，最后按问题回到对应专题。**
