# ECMA-335 与 HybridCLR 专栏修订记录

日期：2026-10-25。范围：本轮审读发现的知识性问题、版本混用、重复职责与阅读导航。保留既有 URL、历史实验及工作区其他改动。本轮不新增 Unity 实验，不将文档检查写成 Player 验证。

## 修订口径

- ECMA-335 仍按第六版解释语义，不把 IL2CPP 的生成策略或 HybridCLR 实现提升为规范要求。
- 首轮源码阅读统一到 runtime v9.0.1 `83929787581bce6c7219418f62348a10c33582ee`、package v9.0.3 `6cadd24d101f4cbd00cd80190106f6621132d24d`、il2cpp_plus v2022.3.x-9.0.1 `ba3a76c9979d14cee404fb5d44301773e0768866`。
- v6.1.0 只作为明确标注的历史对照。旧 async 案例缺少完整构建与回归证据，保留观察、撤回已确定根因的标题和结论。
- 商业能力依据查阅日的官方 9.x 文档。未公开的实现、项目收益与安全性保持未知，不以社区源码拼接闭源实现。

## 主要问题与处理

| 问题 | 本轮处理 |
|---|---|
| 缺少 native 泛型实例被写成补充 metadata 也无效 | 统一按目标身份、可用实现和实际执行路径排查；补充方法体可支持解释执行，显式 native 引用是另一条选择 |
| FullySharedGenericAny、空 RefMethods 或 Development 差异被直接归因于 async 递归 | 撤回必然因果；把构建输入、状态机、方法身份与重入路径分成可验证假设 |
| 声明泛型类型就被当作保留泛型方法实例 | 使用具体调用模板，说明 class/struct 状态机、方法参数和裁剪约束，区分桌面编译与 AOT 验证 |
| metadata 加载、裁剪、构建顺序混淆 | 区分普通 Assembly.Load 与补充同源 image；首次使用前完成补充；以配套 GenerateAll 管理构建依赖 |
| v6/v9 路径与缓存名称混用 | v9 主链区分原始 MethodBodyCache 与已转换 interpData，旧路径放入历史表 |
| ABI 与解释器寻址单位错误 | 区分普通大结构参数与 x8 间接返回，注明 HFA/HVA；区分指令字节位置、8 字节槽位索引与值宽度 |
| ECMA 示例缺少从规则到观察的连接 | 补值复制／地址操作完整例子；补 filter、finally、catch 顺序的可运行示例 |
| FGS、DHE、热重载和访问策略被描述成未经验证的内部实现 | 分清公开契约与未知实现；修正不注入时的脏传播、保留 metadata 复用和 Rule/Target 配置 |
| 未注明口径的性能与加固结论 | 去掉无依据倍数，厂商结果保留来源与样本条件，不推定项目收益或完整安全保证 |
| 多篇重复讲相同泛型修法、索引堆叠每轮日志 | 分工为机制、决策、规则、模板、工作流、场景和检查；索引改为问题与实验入口表 |

## 阅读组织

ECMA 保持导读加九篇主线，文件格式、验证、安全历史与并发按需阅读。HybridCLR 从规范和 IL2CPP 底座进入源码起步，再按需读解释器基础、HCLR-1～5、泛型综合实验、GC/线程及工程收束。商业专题退出必读主线。

原有 HCLR 编号和公开链接保留。重命名的历史案例及商业专题显式固定旧 slug，索引采用新标题。C++ 专项跟读归入现有 ABI、调用链及 GC 文章，不另建内容重复的待写文章。

GC 桥接解释接入机制，线程篇保留登记与退出所有权，详细 GC 数据以 IL2CPP GC 篇和实验仓库为依据。已有固定提交继续保留；`incremental-barriers` 仍标明本地记录，未借用前一轮提交冒充它的版本。

## 验证

- 独立交叉复核覆盖主干、泛型工程化、两篇实现桥接、源码起步、GC／线程与商业能力；发现的历史案例标题和 FGS 性能归因残留已修正。
- ECMA 两个新增完整示例从正文提取，在 Windows、.NET SDK 10.0.402、Release 编译运行。值复制示例输出为 `1/2`、共享同一对象 True、替换后 False、reset 后 `0/True`、数组 `1/True`；实际 IL 含 ldobj、stobj、initobj、ldloca.s、ldelema。异常示例输出 `filter: cleaned=False` → `inner finally` → `catch: cleaned=True`。
- HCLR18、21 两份完整 C# 模板使用桌面 Roslyn、C# 9、本机 Unity 的 .NET Standard 2.1 与 CoreModule 引用编译通过。只验证示例可编译，未执行这些模板的裁剪、IL2CPP 或 Player 测试。
- InjectRules 与 AccessPolicy 的 XML 可解析，标签、默认行为及规则作用范围对照官方文档核验。
- 使用仓库 `.codex-bin/hugo.exe` v0.158.0 构建，输出到独立临时目录。全站 1410 页，零 ERROR。
- 核对两个系列涉及的 54 个内容页：YAML、97 个代码围栏、445 个 relref 均通过；生成页面的 2560 个站内链接、锚点与资源引用通过。
- 54 个修改前后 permalink 一致；其中 7 篇因更正标题显式固定了原 slug。索引采用 reading_title，正文采用 title，与实际模板和渲染标题分别核对。
- 27 个不同的 v9 固定源码文件链接在相应锁定 checkout 中存在；关键方法、参数单位和版本差异另行逐项检查。
- 限定文件 `git diff --check` 通过。本轮为文档与桌面示例验证，沿用既有 Player 记录，没有新增 Unity 运行时验收。Git 交付状态以提交历史为准。

## 后续仍需独立验证

async／UniTask 历史根因，Prefab／AssetBundle 加载回归，constrained.、装箱、异常和类型初始化跨运行时样本，固定设备性能，以及 GC 完整根归因、屏障反事实、并发／长期压力与其他平台。文档已经写明这些边界；本轮未将其改成已完成。

## 本轮文件范围

以下是相对本轮开始快照的实际变动，共 40 个正文／索引页面、3 份已有规划文件和本修订记录。它不是整个工作区的 Git 未提交清单。

```text
content/engine-toolchain/hybridclr-advanced-capability-selection-community-metadata-fgs-dhe.md
content/engine-toolchain/hybridclr-aot-generic-guardrails-generate-ci-build-checks.md
content/engine-toolchain/hybridclr-aot-generic-pitfall-patterns-unitask-linq-dictionary-delegate-custom-container.md
content/engine-toolchain/hybridclr-aot-generics-and-supplementary-metadata.md
content/engine-toolchain/hybridclr-aotgenericreferences-disstripcode-metadata-how-to-work-together.md
content/engine-toolchain/hybridclr-assembly-hot-reload-unload-metadata-reclaim.md
content/engine-toolchain/hybridclr-best-practice-assembly-loading-strip-and-guardrails.md
content/engine-toolchain/hybridclr-boundaries-and-tradeoffs.md
content/engine-toolchain/hybridclr-bridge-abi-cross-boundary-calling-convention.md
content/engine-toolchain/hybridclr-bridge-il2cpp-gc-model-boehm-root-write-barrier.md
content/engine-toolchain/hybridclr-bridge-interpreter-basics-dispatch-stack-register-ir.md
content/engine-toolchain/hybridclr-call-chain-follow-a-hotfix-method.md
content/engine-toolchain/hybridclr-case-async-crash-root-cause-and-two-fixes.md
content/engine-toolchain/hybridclr-case-dictionary-valuetuple-hotfix-type-missing-method.md
content/engine-toolchain/hybridclr-case-typeload-and-async-native-crash.md
content/engine-toolchain/hybridclr-ci-pipeline-generate-all-and-development-flag.md
content/engine-toolchain/hybridclr-code-encryption-access-control-policy.md
content/engine-toolchain/hybridclr-crash-analysis.md
content/engine-toolchain/hybridclr-dhe-function-injection-dirty-contagion.md
content/engine-toolchain/hybridclr-dhe-internal-dhao-format-diff-algorithm-function-routing.md
content/engine-toolchain/hybridclr-dhe-why-not-just-faster-interpreter.md
content/engine-toolchain/hybridclr-disstripcode-writing-patterns-valuetype-reftype-nestedgeneric-delegate.md
content/engine-toolchain/hybridclr-faq-10-most-confused-judgments.md
content/engine-toolchain/hybridclr-fix-decision-disstrip-linkxml-metadata.md
content/engine-toolchain/hybridclr-full-generic-sharing-why-not-metadata-upgrade.md
content/engine-toolchain/hybridclr-interpreter-instruction-optimization-hiopcode-ir-transforms.md
content/engine-toolchain/hybridclr-load-acceleration-metadata-memory-incremental-gc.md
content/engine-toolchain/hybridclr-monobehaviour-and-resource-mounting-chain.md
content/engine-toolchain/hybridclr-performance-and-prejit-strategy.md
content/engine-toolchain/hybridclr-pre-cil-instruction-set-stack-machine-model.md
content/engine-toolchain/hybridclr-principle-from-runtimeapi-to-interpreter-execute.md
content/engine-toolchain/hybridclr-series-index.md
content/engine-toolchain/hybridclr-sharing-type-judgment-valuetuple-int-string-valuetuple-int-object.md
content/engine-toolchain/hybridclr-source-reading-start.md
content/engine-toolchain/hybridclr-thread-attachment-gc-and-runtime-state.md
content/engine-toolchain/hybridclr-toolchain-what-generate-buttons-do.md
content/engine-toolchain/hybridclr-troubleshooting-diagnose-by-layer.md
content/engine-toolchain/ecma335-execution-model-calling-convention-exception-handling.md
content/engine-toolchain/ecma335-series-index.md
content/engine-toolchain/ecma335-terminology-conventions.md
docs/ecma335-series-plan.md
docs/hybridclr-series-plan.md
docs/hybridclr-player-reading-execution-plan.md
docs/ecma335-hybridclr-revision-20261025.md
```
