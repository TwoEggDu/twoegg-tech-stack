# HybridCLR 系列计划

更新：2026-10-25。本文是该系列唯一的 canonical 篇级目录。已写表示正文存在，不代表 Player 或设备验证完成。

## 定位与边界

依赖 ECMA-335 前置层和 IL2CPP 实现层，覆盖社区版执行主链、工具链、工程排障与能力选型；商业功能属于公开资料与架构推断研究。共享文章只维护一个正文，不复制到多个系列。

## 阅读入口

- 发布索引：`content/engine-toolchain/hybridclr-series-index.md`。
- 源码跟读准备：`hybridclr-source-reading-start.md`，已写；固定 v6.1.0 源码，Player 断点验证待补。
- 篇级文件均位于 `content/engine-toolchain/`，原 HCLR 编号及 URL 保留。

## 前置与桥接

| 标识 | 文件 | 状态 |
|---|---|---|
| Pre-A | `hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md` | 已写；与 ECMA-335 共用 |
| Pre-B | `hybridclr-pre-cil-instruction-set-stack-machine-model.md` | 已写；与 ECMA-335 共用 |
| Bridge-C | `hybridclr-bridge-abi-cross-boundary-calling-convention.md` | 已写 |
| Bridge-D | `hybridclr-bridge-il2cpp-generic-sharing-rules.md` | 已写；与 ECMA-335 共用 |
| Bridge-E | `hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md` | 已写 |
| Bridge-F | `hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md` | 已写 |

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

| 工作 | 状态与验收 |
|---|---|
| 源码阅读起步 | 正文与静态源码定位已完成；Unity patch、Player 断点与日志待记录 |
| 三栏泛型调用综合实验 | 待写；同一例子连通规范、AOT 与解释执行 |
| 性能与预热篇 | 待加强；固定设备、构建配置和采样条件 |
| 调用链篇 | 待加强；逐断点的输入输出与首次／再次对照，回查旧路径名称 |
| 商业能力篇 | 待复核；逐条标注公开事实、推断与数据来源 |

旧 `hybridclr-structural-diagnosis-and-bridge-articles-plan.md` 保留为历史诊断与补桥执行材料，不再作为当前进度目录。源码起步的提纲与证据见 `hybridclr-source-reading-start-outline.md`。
