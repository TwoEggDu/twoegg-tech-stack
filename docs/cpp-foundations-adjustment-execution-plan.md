# C++ 公共基础与四条源码路线调整执行计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 默认在当前对话顺序执行；不自动启动子代理、不自动提交或推送。

**Goal:** 在保持公共 CXX-01～11 主干稳定的前提下，补齐源码阅读接缝、项目专项和可复现验证，使读者能够从语言规则进入一条实际执行路径。

**Architecture:** 公共课维护语言规则，各路线维护自己的协议与证据，本文件仅调度跨系列调整。篇名、状态和验收仍由各系列 canonical plan 管理；本执行计划不成为第二份篇级目录。

**Tech Stack:** Hugo、Markdown、普通 C++17 自有示例、既有 TwinArena UE 工程，以及各路线已有源码与实验环境。

**Spec:** [公共课 canonical plan](cpp-foundations-series-plan.md)、[UE plan](ue-fullstack-expert-series-plan.md#cpp-source-prerequisites)、[Piccolo plan](piccolo-engine-series-plan.md#公共基础之后的专项前置)、[HybridCLR plan](hybridclr-series-plan.md#公共基础之后的源码专项前置)、[IL2CPP plan](il2cpp-series-plan.md#公共基础之后的源码专项前置)。本计划落实 2026-10-25 对话中的整体检查与完整调整计划请求。

## 1. 基线、目标与边界

当前检查基于工作区文件，不等于线上页面审计；HybridCLR、IL2CPP 的现有修订和实验记录需按其当前文件核对，不用较早对话覆盖它们。

| 层次 | 当前基线 | 本轮调整目标 |
|---|---|---|
| 公共 C++ | 11 篇正文已成稿，覆盖访问、拥有、分派、移动/容器、回调、布局、模板、生成、对象寿命、构建和并发 | 补语法入口、声明/定义接缝与失败状态；维持 11 个正文单元 |
| Piccolo | CPP 项目材料与 PCPP-1/2/3 已写 | 复用正文，推进构建、事件退出、实际线程与 GPU 证据 |
| Unreal | UCPP-1/2 已写，UE 编译运行待验证 | 按原计划完成 UCPP-3/4/5，并逐项完成已有样例的工程验证 |
| HybridCLR | 三项源码专项训练已归入现有桥接、源码起步与线程正文 | 修正公共入口的过期安排；只按具体问题增强现有训练和验证 |
| IL2CPP | 机制正文已有，ICPP-1/2/3 仍待整理或补桥 | 在既有正文中形成连续的样本跟读，不预设另开三篇 |

### Global Constraints

- 根 `doc-plan.md` 只放系列级路由，不回填篇级清单；`docs/doc-plan-archive-v26.md` 只读。
- 公共正文编号仍为 CXX-01～11；UCPP、PCPP、ICPP 不计入公共正文数量；不恢复已被 HybridCLR 当前计划吸收的 HCPP 独立文章安排。
- 保留旧 URL、正文原归属、原编号和历史验证记录；新增观察追加日期与环境，不把历史结果改写成新环境通过。
- 普通自有示例沿用 C++17；原 GCC 8.3.0 / Windows x64 记录是历史验证环境，不是四项目的工具链要求。新编译器另记身份。
- UE 文章目标沿用 5.8.2；既有依据为 5.8 官方文档。执行时固定实际版本、源码 revision、目标与配置，替代版本不能静默冒充 5.8.2。
- HybridCLR 的 runtime/package/il2cpp_plus 组合及自定义 Editor 限制以源码起步篇和实验记录为准，不将本机组合推广为任意官方安装环境。
- 不复制 Unity 受限 C++ 实现到公开仓；自有样本的生成物也先核对公开边界。UE 同样核对引用许可，不直接搬运未经许可的引擎实现。
- 内容、官方资料、源码核对、实验、发布与个人能力分别记录。未具备实际环境时可以完成正文，工程验证保持待执行。
- 保留其他对话的脏工作区改动；每批提交必须有明确文件/补丁范围。写作或本计划本身不构成提交、推送授权。

### Review Focus

| 易漏条件 | 应有行为 | 负责验收 |
|---|---|---|
| 文件存在但只有示意，或链接指向未写文件 | 不标为完整跟读，不制造未来正文链接 | A01、A09～A11、A15 |
| 指针仍非空、下标仍合法、对象仍存活 | 分开检查拥有、访问有效性与业务身份，不拿存活替代允许调用 | A02、A05、A07、A08 |
| 捕获副本已进入队列，随后解绑或关闭 | 分开解释阻止新派发、已复制任务、正在执行任务与完成确认 | A05、A07 |
| 失败后资源已释放，但业务状态只更新了一半 | 同时核对资源与不变量，不把 RAII、noexcept 或断言当恢复协议 | A04 |
| 生成文件存在、链接成功、日志看似正常 | 继续核对模块/符号身份、注册与真实执行，不提升为全平台或性能保证 | A06、A08～A13 |

## 2. 调整量与优先级

本轮核心产出为：一次入口/状态修正、三组公共旧文补充、三篇既定 UE 专项、三组 IL2CPP 既有正文训练，以及三条路线的定向验证。示例集中管理为可选增强，不阻塞核心内容收口。

| 批次 | 优先级 | 任务 | 完成后读者得到什么 |
|---|---|---|---|
| 第一批：入口和公共接缝 | P0/P1 | A01 → A02 → A03 → A04 | 状态可信，常见声明和失败路径能够读通 |
| 第二批：UE 连续专项 | P1 | A05 → A06 → A07；A08 有环境即穿插 | 从对象、容器进入回调、生成接线和任务交接 |
| 第三批：运行时样本跟读 | P1/P2 | A09 → A10 → A11 | 从同一自有程序串起 IL、生成调用、metadata、对象和分派 |
| 第四批：项目证据补齐 | P2 | A12、A13；持续补 A08 | 已有正文的关键路径有对应运行证据 |
| 第五批：复现增强与收口 | P3/P1 | 可选 A14；必须 A15 | 示例易复做，目录、证据和公开状态一致 |

依赖关系：A01 为状态基线；A03/A04 可基于已有公共课实施；A05 依赖已写 CXX-02/05 与 UCPP-1/2；A06 依赖 CXX-07/08/10；A07 依赖 A05 与 CXX-11；A09～A11 顺序跟读同一自有样本。A08/A12/A13 缺实际环境时登记阻塞点，继续不依赖该环境的内容任务。

## 3. 分项任务

以下新增路径是执行预定位置，只有实际成稿并通过对应检查后才能在公开内容中加入 relref。每项结束先回写所属 canonical plan，再同步入口。

### A01：纠正过期安排，整理四路线入口

**文件：** 修改 `docs/cpp-foundations-series-plan.md`、`content/engine-toolchain/cpp-foundations-series-index.md`；按需同步 `content/reading/piccolo-engine.md`、`content/reading/unreal-engine.md`。HybridCLR/IL2CPP 计划仅在其当前责任或状态确有变化时修改。

**输入/输出：** 以各路线当前 canonical 状态为输入，输出一份准确分流入口；不新设跨路线篇级状态总表。

- [x] 将公共入口对 HybridCLR 的笼统“待补专项”改为已有哪些训练入口、还缺哪些特定证据；公共补写表移除 HCPP-1/2 的独立补篇暗示。
- [x] Piccolo 明确 PCPP-1/2/3 已成稿；UE 明确 UCPP-1/2 已写、3～5 待补；IL2CPP 维持 ICPP-1/2/3 的实际状态。
- [x] 将公共入口那段很长的逐篇阅读串改为阶段表：共同起点 01→02→03→10；随后访问/容器/回调、布局/模板/生成、对象寿命/并发按依赖选读。四条路线给出“首次进入必读”和“遇到对应系统再补”，不要求先读完 11 篇。
- [x] 核对所有已写目标文件、公开 relref 与本地规划链接；未写文章只展示题名/状态，不添加假链接。验收：公共正文数量仍为 11，路线状态与各自 plan 无矛盾。

### A02：补源码语法阅读提示，复用既有解释

**文件：** 修改 `content/engine-toolchain/cpp-foundations-series-index.md`、`cpp-foundations-01-values-references-signatures.md`、`cpp-foundations-02-lifetime-ownership-raii.md`、`cpp-foundations-03-classes-dispatch-destruction.md`、`cpp-foundations-07-template-reading.md`，后四项均位于 `content/engine-toolchain/`；对应 outline 追加此次补充与验证。

**输出：** 公共索引末尾不超过 12 项的短速查；规则细节落回原课，不新增正文编号。

- [x] 速查覆盖限定名/命名空间、类型别名、构造初始化、`explicit`、`=default/delete`、包装的 `operator*`/`operator->`/布尔转换、枚举/标志位的阅读入口；已有解释只链接。
- [x] CXX-01 补“别名展开后再读 const/指针/函数签名”；CXX-02 用自有只读观察包装解释运算符背后的成员调用，不把包装判真当成业务有效证明。
- [x] CXX-03 仅补初始化列表中成员声明顺序与初始化/赋值的接缝；CXX-07 复用既有 using、typename 与依赖类型解释，避免再写一遍模板课。
- [x] 对新增完整自有片段做类型/重载编译检查；正常与预期拒绝分开，保留检查配置。验收：读者能解释 UCPP-2 的 `*FString` 和指针包装语法，而不误以为所有 `*` 都是原生指针解引用。

### A03：补前向声明、不完整类型和定义位置

**文件：** 修改 `content/engine-toolchain/cpp-foundations-10-build-link-symbols.md`、`content/engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md`、`docs/cpp-foundations-10-build-link-symbols-outline.md`。

**输出：** CXX-10 一个独立的 `Handle.h`、`Handle.cpp`、`main.cpp` 自有 C++17 小工程，CXX-02 仅回链。

- [x] 解释仅声明类型即可表达的接口，与按值成员、成员访问、sizeof 等需要完整定义的场景；区分“知道类名”和“知道对象布局”。
- [x] 用前向声明的 `Impl` 与 `unique_ptr<Impl>` 展示定义位置和析构实例化的关系；把需要完整类型的相关特殊成员定义放在可见 Impl 定义的 .cpp，不作所有智能指针统一规则。
- [x] 分别构造不完整类型按值成员的编译失败，以及将需要完整类型的操作放到调用方不可见定义处造成的诊断；补正确版本并重新构建。
- [x] 验收：正常三文件工程编译链接运行通过；预期失败记录具体阶段和诊断；原 CXX-10 构建/符号练习仍成立。不开成完整 PImpl 或编译优化专题。

### A04：补失败协议与状态保证

**文件：** 修改 `content/engine-toolchain/cpp-foundations-02-lifetime-ownership-raii.md`、`cpp-foundations-04-move-containers-invalidation.md`、`cpp-foundations-09-storage-object-lifetime-ub.md` 及各自 outline。

**输出：** CXX-02 主解释，04/09 补边界和回链；不另开完整异常体系课程。

- [x] 区分预期业务失败的返回结果、异常退出、前置条件/内部不变量断言，以及日志；标准 assert 与 UE check/ensure 的实际构建行为由 UE 专项另核对。
- [x] 明确“资源不泄漏”与“业务更新整体成功”分开验收；补基本/强保证与不抛出约定的最小阅读提示，不声称 noexcept 自动回滚或自动成功。
- [x] 用“准备新状态→验证→提交替换”的自有例子演示失败保留旧状态；用受控返回失败和受控异常两条合法路径核对资源计数与状态不变量，不执行 UB 或析构抛出。
- [x] CXX-04 衔接已有 noexcept/复制移动选择；CXX-09 衔接部分构造清理。验收：失败被观察到、原状态符合约定、正常路径可继续执行；不把恢复策略推广到禁异常引擎配置。

### A05：写 UCPP-3 回调专项

**新增：** `docs/ucpp-03-delegates-lambda-callback-lifetime-outline.md`、`content/engine-toolchain/ucpp-03-delegates-lambda-callback-lifetime.md`。

**复用/同步：** CXX-02/05、UCPP-1/2、`content/system-design/pattern-prerequisites-04-delegate-callback-event.md`；更新 UE plan、Unreal 阅读入口、公共索引，以及 UCPP-2 的前向导读。原材料只在确有重复或相关处补回链。

- [x] 先在 UE plan 登记新文件；核对目标版本 Delegate/Lambda 绑定协议，区分原生/动态、单播/多播的必要差别，不展开完整事件架构。
- [x] 选一个订阅者、一个事件源和一份订阅标识，追绑定、保存、执行、解绑和结束；分开 raw、共享/弱持有、UObject 相关绑定，按实际 API 的可用形式选择最小集合。
- [x] 自有样例覆盖正常广播、显式解绑、接收者结束、已复制/延迟任务、重复清理；不执行裸指针悬垂调用，不把弱绑定或解绑推成任意线程的取消屏障。
- [x] 验收：解释捕获是否拥有目标、哪条路径停止新调用、已派发任务如何处置；官方依据、静态检查与 UE 实测单列。无环境时记录预期，不登记为工程通过。

### A06：写 UCPP-4 生成文件与模块接线

**新增：** `docs/ucpp-04-uht-ubt-generated-module-wiring-outline.md`、`content/engine-toolchain/ucpp-04-uht-ubt-generated-module-wiring.md`。

**复用/同步：** CXX-07/08/10、`ufs-03a-project-build-map.md`（engine-toolchain）、`ue-02-reflection-serialization.md`、`ue-07-module-system.md`（system-design）；更新 UE plan、Unreal 阅读入口、公共索引和对应旧文回链。

- [ ] 在原 TwinArena 中新增一个最小反射声明，按实际版本跟踪源头、生成器输入、生成产物、C++ 消费、链接与注册生效；不复制引擎生成实现到公仓。
- [x] 解释 Public/Private 头、模块依赖与导出宏各负责什么；区分 include、Build.cs 依赖、链接导出和运行时模块装载。
- [ ] 一次只引入一种可恢复故障：UHT 输入/声明错误、依赖或头可见性错误、跨模块导出/链接错误；保存阶段、命令、日志与恢复。
- [ ] 验收：每种故障能归到生产者/消费者和具体阶段；有条件时完成一次全新构建与一次增量构建。旧文“已核对/待验证”只按实际证据更新，不把生成文件存在当成注册成功。

### A07：写 UCPP-5 任务交接专项

**新增：** `docs/ucpp-05-task-thread-data-handoff-outline.md`、`content/engine-toolchain/ucpp-05-task-thread-data-handoff.md`。

**复用/同步：** CXX-05/11、UCPP-1/3、`content/system-design/ue-05-threading-model.md`；更新 UE plan、Unreal 阅读入口、公共索引及线程旧文回链。

- [x] 沿“Game Thread 取值快照→后台纯数据工作→Game Thread 应用结果”组织一个受控任务，按目标版本核对具体任务 API，不令后台任意访问 UObject。
- [x] 明确快照/结果的拥有者、同步或任务依赖、回投线程、目标的弱观察和业务代次；分开对象仍存在与结果仍适用于当前请求。
- [ ] 验证完成、目标结束、旧代次结果、停止接受新任务和退出等待/排空；不把单个原子取消标志写成任务已经完成。
- [ ] 验收：每次交接能画出所有权和完成关系；实际线程结论有日志/Trace 或相应证据。GPU 完成另交渲染专项，不由 CPU 任务完成推断。

### A08：落实 UCPP-1～5 的工程证据

**文件/工程：** `docs/engine-source-roots.md` 仅在真实取得合法源码后登记；复用 TwinArena、`docs/ue-fullstack-m0-execution-plan.md` 和各 UCPP outline/正文，不另建 UE 工程。实验工程实际路径从已有 M0 记录核对，不虚构目录。

- [ ] 固定引擎/工具链、source_revision、目标/配置、build_id/run_id、日志与包/符号对应；源码访问和公开许可分开登记。
- [ ] UCPP-1 核对仅 Outer、GC 可见强持有、解除持有、弱观察与 Actor 退出；排查隐藏强引用，不用暂时存活证明保活规则。
- [ ] UCPP-2 执行正常样例和两个独立失败/恢复，记录文化、断言配置与实际结果；未测分配器、性能和完整本地化继续保留限制。
- [ ] UCPP-3/4/5 各执行对应最小验收矩阵。所需 PIE、独立包、编译日志/符号按问题选取，不以一次 PIE 覆盖所有形态。
- [x] 无环境时写明缺失条件和已完成部分；有环境后逐项回填，M0 和个人能力仍按原标准独立验收。

### A09：整理 ICPP-1 的同一程序对应阅读

**文件：** 修改 `content/engine-toolchain/il2cpp-converter-il-to-cpp-code-generation.md`；地图与架构正文只加路由/必要修正；新增 `docs/il2cpp-icpp-reading-outline.md` 作为三项训练的共同执行记录；更新 IL2CPP plan、索引及公共入口。

- [x] 先登记 ICPP-1 归入 D2，复用地图/D1 讲解，不创建第二份 IL2CPP 目录。
- [x] 固定一个自有小程序集和实际 Unity patch/构建配置；用静态加法、实例方法/字段、一个虚调用和一个泛型调用形成后续共用样本，避免每项换例子。
- [ ] 对照 C#、裁剪后 IL、允许公开的产物描述与原生断点/日志，记录方法身份、参数、返回、模块和符号；共享/内联/裁剪导致没有预想独立符号时如实解释，不能伪造一一对应。
- [ ] 验收：读者能从自己的方法跟到实际路径；概念示意与固定构建观察分开。若产物不能公开，发布自有输入、操作步骤、观察结果和许可允许的指引。

### A10：补 ICPP-2 的描述、对象与存储边界

**文件：** 修改 `content/engine-toolchain/il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md`，在 `il2cpp-global-metadata-dat-format-loading-binding.md`、`il2cpp-gc-integration-boehm-wbarrier-finalization.md` 仅补必要接点与回链；同步公共执行 outline、IL2CPP plan 和索引。

- [x] 复用 A09 样本，分别追类型/方法描述、索引/句柄、实例身份、字段访问和存储；补“它描述谁、当前指向谁、由谁管理”的观察表。
- [x] 回链 CXX-02/06/09 与现有 GC/线程专题，不以 native 包装析构代替托管回收，不把某版结构偏移写成通用格式。
- [ ] 验收：至少一个字段读取与一个持有/解除持有场景能对应到已有或新增证据；没有根快照时不声称完整 GC 根归因；不重复已经完成的共享 GC 实验。

### A11：补 ICPP-3 的分派、注册与泛型接线

**文件：** 修改 `content/engine-toolchain/il2cpp-generic-code-generation-sharing-instantiation.md`，在 D2/D3 对应上述文件中补注册/调用入口；复用 `hybridclr-bridge-il2cpp-generic-sharing-rules.md`、`runtime-generic-call-experiment.md`；同步 IL2CPP plan、索引和公共执行 outline。

- [x] 沿 A09 的虚调用与泛型调用，区分托管身份、生成调用、方法入口/调用包装、注册信息和泛型上下文；实际名称按固定版本核对。
- [x] 对已有泛型综合实验逐项复用证据，缺的调用形态才补；不能用 C++ 模板特化解释托管泛型身份或 runtime 共享策略。
- [ ] 验收：至少各有一条虚调用和泛型调用的路径说明与观察；无法取得实际接线证据的格子保持待验证，不把机制概述直接改成训练已完成。

### A12：补 Piccolo 运行证据，维持正文复用

**文件：** `docs/piccolo-engine-build-debug-baseline-outline.md`、`docs/piccolo-object-containers-borrowing-outline.md`、`docs/piccolo-window-editor-callbacks-outline.md`、`docs/piccolo-state-buffer-thread-boundaries-outline.md`；有新证据再更新对应 01、PCPP-1/2/3 正文、Piccolo plan 和入口。

- [x] 先复核固定 revision 与现有完整构建阻塞，成功后核对 exe、符号、配置和资源，失败则记录实际阶段与下一步。
- [ ] PCPP-1 观察对象容器移除、局部强持有、逻辑清理与最终释放；PCPP-2 观察实际事件、退出解绑和接收者寿命。
- [ ] PCPP-3 分别取得真实 CPU 线程/交接证据与按需 GPU 同步/捕获；不能由双缓冲或独立线程小程序推断引擎并行。
- [x] 验收：每条运行结论有对应操作、原始观察和限制；编译仍受阻时不重复增加基础文章掩盖工程缺口。

### A13：HybridCLR 只补当前尚缺的协议证据

**文件：** 以当前 `docs/hybridclr-series-plan.md` 为工作源；具体落点为现有 Bridge-C/E/F、源码起步、共享线程专题、泛型综合实验及其原 outline。正文均位于 `content/engine-toolchain/`，复用 A01 已建立的入口。

- [x] 先对照最新固定实验身份和结果，排除已经完成的首次/再次调用、对象保留、原生回调、活跃根与串行增量片段训练，不照旧规划重做。
- [ ] 将候选缺口分开：其他 ABI/平台、constrained./装箱、异常展开、类型初始化，以及并发/长期压力；选择由实际跟读问题触发的最小一项推进，不把它们全部设成入门前置。
- [ ] 新实验沿用原实验仓库和许可边界；在本仓公开行为、步骤与自有证据，不复制受限实现；平台/性能结论只在实际测得后增加。
- [ ] 验收：新增内容确实回答当前未闭合问题，公共入口准确指向已有训练；不新增 HCPP 编号，不重复讲公共 C++。

### A14：可选的公共示例集中管理

**预定新增：** `examples/cpp-foundations/README.md`、`examples/cpp-foundations/CMakeLists.txt` 与按既有课编号组织的自有正常/失败示例；公共索引与对应 outline 只在样例实际可用后加入口。

- [x] 先核对仓库是否已有适合复用的示例位置；若已有则复用并在本计划登记实际位置，不另建实验仓或测试框架。
- [x] 从正文逐段核对提取，保留文件名、正常/失败分组和编译标准；默认构建只含正常目标，预期失败以独立命令运行，不执行失败产物或 UB。
- [x] 验收：正常目标可统一构建并按需运行；预期失败确实被拒绝且阶段正确；正文与样例一致。没有额外收益的重复测试不新增。
- [x] 首轮只实现示例复做，不扩大为跨平台 CI、性能基准或综合引擎框架；MSVC/Clang、Sanitizer、动态库装载作为明确标识的后续验证增强。

### A15：全量导航与证据收口

**文件：** 公共与四路线的 canonical plan、公开索引/阅读入口，以及本轮实际修改的正文和 outline；根 `doc-plan.md` 仅在系列级状态确实变化时调整对应行。

- [x] 核对公共 11 篇、UE 5 项专项、Piccolo 既有专项以及 ICPP 三项训练的实际状态；训练嵌入旧文不虚增正文数量。
- [x] 检查前置、前向导读、旧文回链、文件目标和生成页锚点；确认没有新孤立页、未来 relref 或宏/模板尖括号被 Markdown 吞掉。
- [x] 对有内容改动的批次运行 `hugo`，零 ERROR 方可提交；按仓库规则单列计划中未写文章的 REF_NOT_FOUND。共享工作区必要时按精确候选构建隔离预览，记录实际构建范围。
- [x] 生成页回归只覆盖本批受影响页面及其本地链接/资源/锚点，核对样例呈现；新事实有相应来源或实验，不用构建通过证明技术结论。
- [x] 提交授权到来后按精确文件/补丁交付；复用未变的验证结果，推送后回读远端 SHA。没有实际检查 Pages 部署时，不宣称线上已更新。

## 4. 每批执行与验收规则

每次只交付一个可独立审阅的单元：A01、A02、A03、A04 各一批；每篇 UCPP 及目录/回链一批；ICPP 每项及对应旧文一批；运行证据按实验问题成批，不把全部后续工作打成一个提交。

每批记录：任务号、具体文件/补丁、依赖、引用依据、正常和失败预期、实际命令/结果、未验证范围、所属 plan 回写。旧文补充只运行有意义的新片段与受影响检查；新增边界后才扩大验证，不为小修机械重跑全部实验。

环境或许可阻塞时：说明缺哪项身份/工具/权限或产物，保留已经完成的正文与资料核对。状态维持“已写/实验待验证”，继续其他就绪任务，不请求用户重复确认已经明确的编辑范围。

## 5. 完成标准与建议执行队列

**内容收口：** A01～A07、A09～A11、A15 的内容/导航项完成；公共维持 11 单元，UE 三篇新增桥梁存在，ICPP 三组训练有实际阅读内容。A08/A12/A13 的运行项必须独立报告，未完成时不得把四路线称为工程验收全部通过。A14 是增强项，不影响正文齐备判断。

**证据收口：** 对上述实际采用的工程路径完成各自验收；所有尚缺平台、调用形态、压力、文化/本地化或性能范围继续列明，不以有限实验追求“覆盖全部行为”的虚假结论。

建议顺序：**A01 状态与入口 → A02 语法接缝 → A03 声明与定义 → A04 失败协议 → A05 UCPP-3 → A06 UCPP-4 → A07 UCPP-5 → A09/A10/A11 IL2CPP 跟读 → A15 收口**。A08 有 UE 环境即从 UCPP-1/2 开始穿插；A12/A13 根据项目实际需要和可用环境推进；A14 在核心内容稳定后执行。

本轮A01～A07、A09～A11的内容与导航已实施，A14示例复做及A15静态站点收口完成。工程相关未执行格继续保留：UE环境、Piccolo完整编译、同一样本Unity/native接线及HybridCLR新类型初始化Player。具体完成/部分/阻塞、命令与结果见[执行记录](cpp-foundations-adjustment-execution-report.md)，不增加发布或个人能力状态。
