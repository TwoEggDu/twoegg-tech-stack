# ECMA-335 系列计划：IL2CPP 与 HybridCLR 前置层

更新：2026-10-25。本文是 ECMA-335 系列唯一的 canonical 篇级目录；生态总规划仅保留模块路由。

## 定位与边界

目标读者能写 C#，需要理解 IL2CPP、HybridCLR 的 metadata、AOT 泛型、解释执行和调用边界。按问题组织规范知识，不追求 ECMA-335 全覆盖。规范基线为第六版，具体运行时能力标注版本，不引用受限 Unity C++ 源码。

已确认方案：复用旧地址，合并基础内容，分离必读、实现桥接、按需分支与历史材料；新增签名解析、类型初始化两篇。ABI、解释器、GC 实现、裁剪和 async 复用已有专题。

## 主线目录

以下文件均位于 `content/engine-toolchain/`。已写表示本地正文已落盘，不代表已发布或真机验证。

| 顺序 | 文件 | 来源与状态 |
|---|---|---|
| 导读 | `ecma335-terminology-conventions.md` | 原 A0，已写；已统一解释执行、热重载与 AOT 术语 |
| 1 | `hybridclr-pre-cli-metadata-typedef-methoddef-token-stream.md` | 原 A1／Pre-A，合并 A13 的基础定位路径 |
| 2 | `ecma335-type-system-value-ref-generic-interface.md` | 原 A3，补强泛型语义与引用边界 |
| 3 | `ecma335-signatures-and-reference-resolution.md` | 新增，已写；含可运行读取程序与三组实参对照 |
| 4 | `hybridclr-pre-cil-instruction-set-stack-machine-model.md` | 原 A2／Pre-B，合并 A8 的最小验证规则；增加 ldobj／stobj／initobj／ldelema 的可运行对照 |
| 5 | `ecma335-execution-model-calling-convention-exception-handling.md` | 原 A4，补强 constrained. 与装箱；增加 filter 先于栈展开的完整对照 |
| 6 | `ecma335-assembly-model-identity-versioning-loading.md` | 原 A5，收拢到身份、依赖、加载 |
| 7 | `ecma335-type-loading-and-initialization.md` | 新增，已写 |
| 8 | `ecma335-memory-model-object-layout-gc-contract-finalization.md` | 原 A6，收拢到布局、GC 与终结契约；已补静态容器与事件退订实验，链接实现层内存分类 |
| 9 | `ecma335-custom-attributes-reflection-encoding.md` | 原 A9，改为反射可见性与可调用性主线 |

## 分支与旧文迁移

| 旧编号 | 文件 | 新职责 |
|---|---|---|
| A7 | `hybridclr-bridge-il2cpp-generic-sharing-rules.md` | 泛型实现桥接，保留原地址；已补嵌套泛型 Add 调用推导 |
| A8 | `ecma335-cli-verification-il-type-safety.md` | IL 重写与验证附录 |
| A10 | `ecma335-pinvoke-native-interop-marshaling-spec.md` | 原生互操作分支 |
| A11 | `ecma335-cli-security-strong-name-cas.md` | 历史背景，退出主线 |
| A12 | `ecma335-threading-memory-model-volatile-barriers.md` | 并发进阶分支 |
| A13 | `ecma335-cli-file-format-pe-cli-header.md` | PE／CLI 文件格式查阅附录 |

不删除旧 URL，不重新分配旧 A 编号。顺序以索引和本文为准：导读 1 篇、主线 9 篇、桥接 1 篇、按需及历史 5 篇；索引另计。跨系列复用文章只维护一个源文件。

### 导航元数据

索引使用 `series_id: ecma335`、`series_title: ECMA-335 前置主线`；上述 16 篇成员用 `primary_series: ecma335` 明确归属，避免按生态大系列统计出其他 runtime 的文章。`series_order` 的 10～19 对应导读及九篇主线，39 对应共享桥接，40～44 对应按需与历史材料。索引正文仍按主线和分支分组，不把 16 篇都当成必读。

跨系列复用的 Pre-A、Pre-B、Bridge-D 保留 `series: HybridCLR` 和原 slug。现有模板仍可在 HybridCLR 中识别它们；所选顺序保留该入口的前置位置和桥接位置，不修改全站模板。

## 两篇新文的执行提纲

**签名解析（原理篇）**：从同名方法和局部 token 的歧义出发，建立模块作用域 → 引用／定义 → 签名 → 类型／方法实参 → 实现的链路。用两个 DLL 中的 `Box<T>.Echo<U>(T, U)` 调用核验 MemberRef、TypeSpec、MethodSpec；区分签名与约束记录，不展开 RGCTX 内部结构。

**类型初始化（原理篇）**：区分程序集加载、类型解析／准备、CLI 类型初始化和业务初始化。用精确／宽松初始化、泛型静态字段和失败的 .cctor 做实验；一次日志次序不作为规范承诺。运行时 Class::Init 的名字不能替代对 .cctor 执行状态的检查。

## 验证与维护

依据为 ECMA-335 第六版、Microsoft 的 C#／CLI 说明与 HybridCLR 官方文档，直接在正文附出处。示例区分语义示意、预期观察与本机实测，不虚构 Unity 或设备结果。

完成后检查 relref、slug、前后篇、生态入口、YAML 和 Hugo 构建。保留无关工作区改动，不把本地修改等同于提交、发布或运行时验收。

## 本次验证记录（2026-10-23）

- .NET SDK 10.0.402、Release：编译 Shared 与 HotEntry 两个类库，核验 Echo 签名、MethodSpec 实参、MemberRef → TypeSpec 关系及调用结果。编译器在该例中生成 call，正文已按实际产物说明。
- 同版本控制台实验：核验 BeforeFieldInit 标志、精确初始化顺序、封闭泛型静态字段独立，以及失败 .cctor 不因第二次调用而重试。
- Hugo 0.157.0 extended：全站构建成功，生成 1345 页，零 ERROR；输出放在临时目录。
- 本次涉及的 20 个内容页面：YAML、代码围栏和 230 个 relref 检查通过；生成 HTML 中 250 个本地引擎工具链链接可解析，标题与源文件一致。
- 原 Pre-A、Pre-B、Bridge-D 没有显式 slug，网址由旧标题生成。本次固定旧 slug，并用旧稿与新稿的 Hugo permalink 对比确认 3 个地址完全一致。
- 首轮结构调整已提交为 `e4d0c9f`；本记录未核验线上部署，也未执行 Unity Player／HybridCLR 真机验证。

## 复审修订与验证（2026-10-23）

- 统一 HybridCLR 的 CIL → 内部寄存器指令 → 解释执行表述；区分增量更新与程序集卸载重载，移除导读中无测试条件的性能和回收比例。
- 同步修正直接衔接的 IL2CPP runtime 篇，区分 CLI token、转换后索引、运行时对象与执行入口；补回签名和类型初始化的前置链接。
- 从正文提取读取程序与 C# 示例，以 .NET SDK 10.0.402、Windows、Release 编译运行：三种泛型调用的签名／实参记录均符合文内对照表，调用结果为 ok、ok、8。
- 从 GC 篇提取静态容器实验，在独立桌面进程无调试器运行，得到 `retained: True`、`after clear: False`；不把它作为保守 GC 或 Player 的回收时机保证。
- 泛型共享桥接新增具体 Add 调用的归约推导，依据 9.x 官方文档；未声称已验证某个 Unity 主包的 native 实现。
- Hugo 0.157.0 extended 全站构建通过，1348 页、零 ERROR；本轮涉及的 18 个内容页 YAML、代码围栏及 slug 检查通过，19 个文件中 114 个 relref 目标存在。
- 生成栏目页中的 ECMA 卡片为 16 篇，前三篇为导读、metadata、类型系统；HybridCLR 卡片篇数和前三个入口与修订前一致。泛型代码表示在 HTML 中正确保留。
- 本轮验证不包含线上部署核验或 Unity／HybridCLR Player 验证。

## 跨系列衔接（2026-10-25）

主线结构保持导读 + 九篇主线。索引新增 HybridCLR 源码阅读起步入口；后续实现层分别由 [IL2CPP 系列计划](./il2cpp-series-plan.md) 和 [HybridCLR 系列计划](./hybridclr-series-plan.md) 管理。共享案例 `runtime-generic-call-experiment.md` 已写并完成固定环境 Player 验证：实际 call → MemberRef／MethodSpec → AOT 产物 → 原生或补充方法体的解释路径。三栏引用同一正文，不增加规范必读篇数。执行与证据见 `runtime-generic-call-experiment-outline.md`。

## GC 与线程衔接（2026-10-25）

GC 前置增加事件／实例委托保留与退订实验，区分 Normal、Weak、Pinned 句柄；索引与并发分支接入共享的 IL2CPP／HybridCLR 线程专题。主线仍为导读 + 九篇，未把运行时线程登记当成 ECMA 内存模型内容。桌面实验与实现层 Player 实测分别记录，待验证范围见下文。

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

## 持续跨增量片段的引用修改与写屏障（2026-10-25）

- 独立projects/incremental-barriers，build_id70a8a176-4eee-43b2-8aa8-a75754ac0bfb；Editor／v9／Windows x64锁不变，GCMode改Manual作为控制变量。旧4861bed与gc-roots回执保留，本轮尚未提交，不借用旧SHA标识。
- AOT／hot各3个普通Player、各3个匹配PDB的CDB Player全部退出0；连续12帧more=True，MARK_END完成数1→2，修改期间不变；原生GC_gc_no4→5。writer预热后常驻，串行握手；紧凑收尾，图与标记检查在Join前。
- 字段／引用数组／含引用结构体字段和数组赋值；实际CIL解析、目标槽屏障、dirty前后位、后续目标3页重扫关联。Box标记1／目标0，末尾marker4（返回前）目标0，完成后12目标全1；checksum12078。AOT16／hot24是重载入口次数，非写入或性能比例；每进程仅2次dirty新置0→1。
- hot每进程6次根查询入口top0／frames0（基线1、目标周期收尾2、周期后／退出清理3），注册注销所有者匹配；scrub后当前worker栈的当前目标精确地址12次均0处。不是所有辅助根缺失证明，也未重新采根回调返回缓冲。
- 96项Python测试通过。源码、产物、原始日志、CIL和验收工具有哈希绑定；公开结果／自有事件在实验仓库evidence/2026-10-25-incremental-barriers。完整动态根归因、屏障反事实与完整路径、并发／长期／性能及其他平台仍待补。
- 三篇GC／线程正文、三个索引同步本样本结果，不新增正文篇数，不把实现行为上升为ECMA-335规范要求。后续提交发布时再补本轮固定链接。

## 本轮内容一致性修订（2026-10-25）

主线篇数与旧 URL 不变。导读统一 JIT／解释执行与分层编译术语；CIL 篇通过值复制、引用字段共享、地址操作样例连接抽象规则；调用与异常篇补齐 filter → finally → catch 的可执行观察。源码阅读跳转以 HybridCLR 固定 v9 为主，规范语义仍以 ECMA-335 第六版为依据。

索引将多轮实验日志收成“问题 → 正文 → 固定记录”表，不把实现样本当成规范必读。实验的自定义 Editor、平台、Manual 控制及未提交增量样本的边界继续保留。当前文档验证与 Player 历史证据分开记录，见 `ecma335-hybridclr-revision-20261025.md`。

## 主线配图（2026-10-25）

签名篇补跨模块引用关系图，类型初始化篇补职责与触发窗口图；调用与异常篇以 Mermaid 更新既有两阶段图，明确 filter 与展开清理。索引直接链接图所在小节，主线篇数和旧 URL 不变。配图范围与文档验证见 [配图记录](./ecma335-hybridclr-diagrams-20261025.md)，不增加运行时验收结论。
