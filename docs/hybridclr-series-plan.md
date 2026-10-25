# HybridCLR 系列计划

更新：2026-10-25。本文是该系列唯一的 canonical 篇级目录。已写表示正文存在，不代表 Player 或设备验证完成。

## 定位与边界

依赖 C++ 公共基础、ECMA-335 前置层和 IL2CPP 实现层，覆盖社区版执行主链、工具链、工程排障与能力选型；商业功能属于公开资料与架构推断研究。共享文章只维护一个正文，不复制到多个系列。

## 阅读入口

- 发布索引：`content/engine-toolchain/hybridclr-series-index.md`。
- 源码跟读准备：`hybridclr-source-reading-start.md`，已写；保留 v6.1.0 源码阅读；v9 配套组合已在本机自定义 Unity 完成 Player 断点验证。
- 线程接入专题：`hybridclr-thread-attachment-gc-and-runtime-state.md`，已写；与 IL2CPP 共用，源码核对完成，Player／native 回调实验待补。
- 数量：30 篇编号正文 + 6 篇前置桥接 + 2 篇专题，共 38 篇，索引另计。
- 篇级文件均位于 `content/engine-toolchain/`，原 HCLR 编号及 URL 保留。

## 公共基础之后的源码专项前置

公共语言目录归 [C++ 公共基础 plan](cpp-foundations-series-plan.md)，已在本系列索引前置区回链。优先 CXX-01～02、05～06、09～10；遇到模板与线程再补 07、11。CLI/托管类型仍由 ECMA-335 解释，不能用 C++ 模板或 RAII 替代。

已有六篇前置/桥接解释机制，源码阅读起步解释版本、构建和第一条调用链；下列补充训练怎样逐句读具体 C++ 和状态变化。

| 次序 | 专项拟题 / 工作 | 状态与复用 | 完成标准 |
|---|---|---|---|
| HCPP-1 | 方法指针、参数槽与返回值怎样组成一次调用 | 待补；复用 Bridge-C 的 ABI 模型 | 公共 CXX-05～06 之后，固定源码/生成物，解释指针类型、槽位访问、参数重建与返回位置；伪代码和实际输出分列 |
| HCPP-2 | 从 metadata 索引到方法体与解释帧 | 待补；复用 Pre-A/B、Bridge-E、源码阅读起步 | 公共 CXX-06、09 与 CLI 之后，追输入边界、token/index、缓存、指令与帧状态；分别记录首次/再次调用 |
| HCPP-3 | AOT/解释执行的对象与线程协作 | 待补；复用 Bridge-F 与现有线程/GC 材料 | 公共 CXX-02、09、11 之后，区分 native 容器/RAII 与托管对象规则，按固定版本核对线程/GC 接口和有效访问条件 |

补写顺序 HCPP-1 → 2 → 3，各项先补公共依赖。三项不计入已有 30 篇编号正文、六篇桥接和源码起步；如已有正文能够容纳，就在其内补跟读段落，不为目录数量另造重复文章。版本、Player 和设备验证状态继续逐篇登记。

## 前置与桥接

| 标识 | 文件 | 状态 |
|---|---|---|
| Pre-A | `hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md` | 已写；与 ECMA-335 共用 |
| Pre-B | `hybridclr-pre-cil-instruction-set-stack-machine-model.md` | 已写；与 ECMA-335 共用 |
| Bridge-C | `hybridclr-bridge-abi-cross-boundary-calling-convention.md` | 已写 |
| Bridge-D | `hybridclr-bridge-il2cpp-generic-sharing-rules.md` | 已写；与 ECMA-335 共用 |
| Bridge-E | `hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md` | 已写 |
| Bridge-F | `hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md` | 已加强；v6.1.0 活跃根、写屏障与 metadata 生命周期 |

## 编号正文

| 编号 | 文件 | 状态 |
|---|---|---|
| HCLR-1 | `hybridclr-principle-from-runtimeapi-to-interpreter-execute.md` | 已写 |
| HCLR-2 | `hybridclr-aot-generics-and-supplementary-metadata.md` | 已写 |
| HCLR-3 | `hybridclr-toolchain-what-generate-buttons-do.md` | 已写 |
| HCLR-4 | `hybridclr-monobehaviour-and-resource-mounting-chain.md` | 已写 |
| HCLR-5 | `hybridclr-call-chain-follow-a-hotfix-method.md` | 已写 |
| HCLR-6 | `hybridclr-boundaries-and-tradeoffs.md` | 已写 |
| HCLR-7 | `hybridclr-best-practice-assembly-loading-strip-and-guardrails.md` | 已写 |
| HCLR-8 | `hybridclr-troubleshooting-diagnose-by-layer.md` | 已写 |
| HCLR-9 | `hybridclr-performance-and-prejit-strategy.md` | 已写 |
| HCLR-10 | `hybridclr-full-generic-sharing-why-not-metadata-upgrade.md` | 已写 |
| HCLR-11 | `hybridclr-dhe-why-not-just-faster-interpreter.md` | 已写 |
| HCLR-12 | `hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md` | 已写 |
| HCLR-13 | `hybridclr-faq-10-most-confused-judgments.md` | 已写 |
| HCLR-14 | `hybridclr-case-typeload-and-async-native-crash.md` | 已写 |
| HCLR-15 | `hybridclr-crash-analysis.md` | 已写 |
| HCLR-16 | `hybridclr-ci-pipeline-generate-all-and-development-flag.md` | 已写 |
| HCLR-17 | `hybridclr-case-async-crash-root-cause-and-two-fixes.md` | 已写 |
| HCLR-18 | `hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md` | 已写 |
| HCLR-19 | `hybridclr-fix-decision-disstrip-linkxml-metadata.md` | 已写 |
| HCLR-20 | `hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md` | 已写 |
| HCLR-21 | `hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md` | 已写 |
| HCLR-22 | `hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md` | 已写 |
| HCLR-23 | `hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md` | 已写 |
| HCLR-24 | `hybridclr-aot-generic-guardrails-generate-ci-build-checks.md` | 已写 |
| HCLR-25 | `hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md` | 已写；商业能力选读，待复核证据与版本 |
| HCLR-26 | `hybridclr-dhe-function-injection-dirty-contagion.md` | 已写；商业能力选读，待复核证据与版本 |
| HCLR-27 | `hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md` | 已写；商业能力选读，待复核证据与版本 |
| HCLR-28 | `hybridclr-assembly-hot-reload-unload-metadata-reclaim.md` | 已写；商业能力选读，待复核证据与版本 |
| HCLR-29 | `hybridclr-code-encryption-access-control-policy.md` | 已写；商业能力选读，待复核证据与版本 |
| HCLR-30 | `hybridclr-load-acceleration-metadata-memory-incremental-gc.md` | 已写；商业能力选读，待复核证据与版本 |

## 接下来补强

近期先做“源码阅读起步 → 调用链篇”的共用 Player 实验，再做三栏泛型调用综合实验、GC／线程验证和性能测量。步骤与验收统一放在 [Player 源码跟读执行计划](./hybridclr-player-reading-execution-plan.md)。已用本机自定义 Unity 2022.3.62f3 完成新版隔离实验：package v9.0.3 / runtime v9.0.1 原始配套源码构建成功，完整 PDB、同一方法首次转换／再次复用、两次 Execute 与返回值均已记录。两篇保留 v6.1.0 阅读说明并单列 v9 实测，不增加新正文数量。

| 工作 | 状态与验收 |
|---|---|
| 源码阅读起步 | 已补自定义 Editor 身份、v9 固定组合、构建与符号记录、自有样本；不推广为全版本兼容 |
| GC 与线程接入 | 正文、固定源码定位已完成；桌面实验、Player 根快照与 native 回调验证分别记录 |
| 三栏泛型调用综合实验 | 待写；同一例子连通规范、AOT 与解释执行 |
| 性能与预热篇 | 待加强；固定设备、构建配置和采样条件 |
| 调用链篇 | 已修正 v6.1.0 旧路径，新增 v9 差异表与实测：同一方法 Transform 一次、Execute 两次，返回 5、15 |
| 商业能力篇 | 待复核；逐条标注公开事实、推断与数据来源 |

旧 `hybridclr-structural-diagnosis-and-bridge-articles-plan.md` 保留为历史诊断与补桥执行材料，不再作为当前进度目录。源码起步的提纲与证据见 `hybridclr-source-reading-start-outline.md`。

## GC 与线程修订（2026-10-25）

Bridge-F 明确动态根回调及活跃范围；HCLR-27 同步修正根描述，HCLR-28 修正 GC 与 native metadata、类型初始化及卸载顺序的混淆，HCLR-30 收紧增量 GC 与商业能力推断。商业篇其余实现与性能表述仍保留待复核状态，本轮不代表全篇完成源码验证。新增专题的提纲、证据与验证边界见 `hybridclr-thread-attachment-outline.md`。
