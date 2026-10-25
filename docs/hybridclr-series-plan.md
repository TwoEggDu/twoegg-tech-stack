# HybridCLR 系列计划

更新：2026-10-25。本文是该系列唯一的 canonical 篇级目录。已写表示正文存在，不代表 Player 或设备验证完成。

## 定位与边界

依赖 C++ 公共基础、ECMA-335 前置层和 IL2CPP 实现层，覆盖社区版执行主链、工具链、工程排障与能力选型；商业功能属于公开资料与架构推断研究。共享文章只维护一个正文，不复制到多个系列。

## 阅读入口

- 发布索引：`content/engine-toolchain/hybridclr-series-index.md`。
- 源码跟读准备：`hybridclr-source-reading-start.md`，已写；保留 v6.1.0 源码阅读；v9 配套组合已在本机自定义 Unity 完成 Player 断点验证。
- 线程接入专题：`hybridclr-thread-attachment-gc-and-runtime-state.md`，已写；与 IL2CPP 共用，源码核对完成，托管工作线程与原生回调正常路径 Player 实验完成；有界GC保留/压力已实测；长期压力与其他平台待补。
- 泛型调用综合实验：`runtime-generic-call-experiment.md`，已写；与 ECMA-335、IL2CPP 共用，固定自定义 Editor/v9 环境的四组 Player 对照与原生断点验证完成。
- 数量：30 篇编号正文 + 6 篇前置桥接 + 3 篇专题，共 39 篇，索引另计。
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

“源码阅读起步 → 调用链篇”的共用 Player 实验及三栏泛型调用综合实验已完成，对象保留与托管线程验证也已完成，原生回调正常路径也已完成；原生回调GC保留与有界压力也已完成；对象快照与实际根区间验证也已完成；接下来补完整动态根路径归因、持续跨片段引用修改和性能测量。顺序见 [Player 源码跟读执行计划](./hybridclr-player-reading-execution-plan.md)，泛型实验单独记录于 `runtime-generic-call-experiment-outline.md`。环境继续使用本机自定义 Unity 2022.3.62f3、package v9.0.3 / runtime v9.0.1 原始配套源码；旧两篇保留 v6.1.0 阅读说明，新增综合实验归本系列、三栏共用。

| 工作 | 状态与验收 |
|---|---|
| 源码阅读起步 | 已补自定义 Editor 身份、v9 固定组合、构建与符号记录、自有样本；不推广为全版本兼容 |
| GC 与线程接入 | 正文、源码定位、对象保留、托管线程及原生回调正常路径实测已完成；对象快照及实际活跃根区间已验证；完整动态根路径归因、持续跨片段引用修改与长期 native 回调压力待补 |
| 三栏泛型调用综合实验 | 已写并实测；同一 DLL，显式 AOT seeds × 补充元数据四组对照；P0/M0 的 int/结构体异常、其余成功，原生与内联解释路径均有断点证据 |
| 性能与预热篇 | 待加强；固定设备、构建配置和采样条件 |
| 调用链篇 | 已修正 v6.1.0 旧路径，新增 v9 差异表与实测：同一方法 Transform 一次、Execute 两次，返回 5、15 |
| 商业能力篇 | 待复核；逐条标注公开事实、推断与数据来源 |

旧 `hybridclr-structural-diagnosis-and-bridge-articles-plan.md` 保留为历史诊断与补桥执行材料，不再作为当前进度目录。源码起步的提纲与证据见 `hybridclr-source-reading-start-outline.md`。

## GC 与线程修订（2026-10-25）

Bridge-F 明确动态根回调及活跃范围；HCLR-27 同步修正根描述，HCLR-28 修正 GC 与 native metadata、类型初始化及卸载顺序的混淆，HCLR-30 收紧增量 GC 与商业能力推断。商业篇其余实现与性能表述仍保留待复核状态，本轮不代表全篇完成源码验证。新增专题的提纲、证据与验证边界见 `hybridclr-thread-attachment-outline.md`。

## 配套实验仓库同步（2026-10-25）

公开仓库：[hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)；文章固定提交 [`e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)（`e0220099d293f7a1a4601ee9931b49a10dd519f9`）。三个专栏索引同步复现与证据入口，源码阅读起步、调用链实战和泛型综合实验共用该仓；不新增正文或改变主线篇数。

新仓库三份 Player 已重新构建，Add 与四组泛型普通运行、原生断点均符合预期，公开证据见 [本次记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md)。正文区分原始隔离实验与新仓库构建的哈希、进程地址；自定义 Editor 与平台限制不变。本节记录文章与索引的同步内容；线上可见状态以 Pages 部署结果为准。

## 对象保留与托管线程 Player 实测（2026-10-25）

- 复用 hybridclr-runtime-lab 新增 projects/lifetime-threads，独立 artifacts/lifetime；环境仍为本机自定义 Unity 2022.3.62f3_96770f904ca7、package v9.0.3 / runtime v9.0.1、配套 il2cpp_plus v2022.3.x-9.0.1。
- build_id：70021d1d-8d18-4bdf-8066-446dd9898b80。GC 与 threads 各三个无调试器独立进程通过；另一个匹配 PDB 的 CDB 进程完成方法／线程／状态关联。
- GC 每组 16 个目标：held 为 0/16/16，清空静态数组后 0/0/16，清空事件列表后 0/0/0；三轮一致。GCMode=Enabled、isIncremental=True。这里是弱引用观测，不是根快照或内存容量测量。
- 两个托管 worker 各返回 5/15 并 Join；两份 MachineState 生命周期重叠；同一 Add 四次 Execute、一轮 Transform、相同非空 interpData；各 worker 的 FREE、析构、动态根注销入口完整匹配。并发首次转换、native 回调、活跃根区间与性能均未验收。
- 结果回填 IL2CPP D6、HybridCLR Bridge-F 和共享线程篇；三个索引更新验证状态。实验仓库已推送 [固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)均使用固定地址；旧 e0220099 保留为调用链／泛型实验的版本。

## 原生回调 Player 实测（2026-10-25）

- 用户已授权继续原生插件闭环。新增实验仓库 projects/native-callbacks 与自有 native/native-callbacks/plugin.cpp；原有环境锁不变，build_id=65d3ddb9-3bb0-4ce9-99b9-59f4ff1cc82b。
- registered/raw 各三个独立 Player 退出0；两组再分别用匹配 GameAssembly/NativeProbe private PDB 取证。registered 两次5/15，raw 三次5/15/23。
- registered wrapper 的 owned=0，复用1个状态；raw 每次scope拥有登记，3次Attach、6个Detach重载入口、3轮状态/根清理。同一原生OS ID出现managed ID 5/6/7。两组目标方法分别只Transform 1次，首次Execute缓存明确为空，后续复用。
- raw 在第三次回调Hold时stop，Join(0)未完成；放行后回调结束/线程退出/Join，最后释放委托，无第四次回调。只验收正常单任务路径；GC压力、多原生线程竞争、上下文GCHandle、异常注入、强杀和其他平台未覆盖。
- 实验仓库已推送 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb)，[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/results.json)使用固定地址；a387d28仍是前序对象保留/托管线程基线。线程正文及三个索引同步状态，不新增正文篇数。

## 原生回调 GC 保留与有界压力（2026-10-25）

- 原实验环境锁不变，build_id=148235df-910b-441f-94b2-b7348eb02d8c；自有新工程 projects/gc-callbacks、native/gc-callbacks/plugin.cpp、artifacts/gc-callbacks。
- handle、stack、stress各三个独立Player退出0。Normal句柄的Context在Free前保留；无上下文句柄的活跃回调Node跨3次收集继续校验；退出/Join/解除持有后，弱引用在观察窗口内均失效。释放后仍存活的情况保留为观察，不强制验收归零。
- 压力组1024回调、每次32节点、256字节payload，共8,388,608字节payload；64槽Id/占用、Head身份、Next及payload检查通过，checksum786944/errors0。
- 32检查点共128增量请求，预算1,000,000ns；more均0，整个负载窗口GC.CollectionCount(0)差值均256。窗口排除前/后显式持有检查；计数不单API归因，不解释为256次完整Full或性能结果。
- handle/stack分别匹配PDB取证：活跃回调内根注册后、scope退出前各3次Full入口，Join后handle组FreeContext1次，stack0次。stress未逐次原生断点跟踪。没有实际根区间、根快照、唯一根归因、持续跨片段标记引用修改或写屏障完整性结论。
- 31项Python测试和4项直接编译热更源码的桌面破坏测试通过，后者不作为Player/GC验证。独立审查提出的对象图关系、检查点相位、显式收集计数门槛已修正。
- 实验仓库已推送固定提交 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，见[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)；b915632仍为前序原生回调正常路径版本。GC/线程三篇正文及三个索引已回填，不增加正文篇数。

## 对象快照与活跃根区间验证（2026-10-25）

- 独立 `projects/gc-roots`，build_id `993e5b9e-1f71-4949-b339-39ff10d359b1`；环境锁不变，旧gc-callbacks源码与产物回执保留。
- 最终24个Player进程退出0：handle/stack/idle各3个无快照对照、handle/stack各3个快照进程、三组各3个CDB进程。额外ABI检查仅作调试器布局验证，不算业务通过进程。
- 实际GC调用栈含GC_push_roots，GetGCRootData入口/返回缓冲成对采样。每组9个返回：活跃handle与stack均36槽/288字节，目标偏移24与48/264；idle线程仍存活且backing容量131072槽，但解释帧/stackTop0，实际返回null/0。根注册/注销完整匹配。
- 21份ManagedObjects快照：handle四阶段强目标1/1/1/0，同一Context→Slots64槽数组路径可见；stack三阶段无热更强句柄目标。所有ManagedStacks条目0，未提供解释器动态根边，不作唯一根归因。
- 独立Editor分析工程使用官方Memory Profiler1.1.0，21份快照共189组条目数量和缓冲哈希与自有读取器一致。包仅作分析依赖，不修改Player配置。57项Python测试通过。
- 实验仓库已推送固定提交 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，入口为[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/results.json)与同目录三组自有root-events；真实.snap、PDB和原始调用栈仅在忽略目录。449a994仍是前轮保留/压力基线。
- 三篇正文和三个索引补入实际返回区间与对象快照的证据边界。完整动态根路径归因、持续跨增量片段引用修改、性能、长期压力及其他平台仍待补；正文篇数不变。
