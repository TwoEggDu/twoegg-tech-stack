# IL2CPP 系列计划

更新：2026-10-26。本文是该系列唯一的 canonical 篇级目录。目录含 10 篇正文，另引用 1 篇归属 HybridCLR 的共享线程专题，索引另计。

## 定位与边界

连接 ECMA-335 的程序描述与 HybridCLR 的动态扩展：先理解产物分工，再沿转换器、runtime、metadata、泛型、GC、能力边界和裁剪阅读。公开仓只描述行为与实验，不转载 Unity 受限 C++ 实现。

## 公共基础之后的源码专项前置

公共语言目录归 [C++ 公共基础 plan](cpp-foundations-series-plan.md)，已在本系列索引回链。优先 CXX-01～03、05～06、09～10；模板/线程随目标入口补 07、11。CLI 类型、调用和泛型语义继续由 ECMA-335 前置层负责。

| 次序 | 专项拟题 / 工作 | 状态与复用 | 完成标准 |
|---|---|---|---|
| ICPP-1 | 同一程序的 C#、IL 与生成代码怎样对应 | 已嵌入D2；完整自有输入及实际桌面CIL已验，固定Unity构建/stripped IL/native接线待验 | 公共 CXX-01、05、10 之后，以允许公开的自有样本核对参数、入口、返回值及构建身份；描述转换产物与 runtime 的区别 |
| ICPP-2 | metadata、运行时对象与原生存储怎样分开 | 已嵌入D3；同一样本字段/持有路径与观察表已写，复用已有Player持有/根证据；新样本Unity对应待验 | 公共 CXX-02、06、09 之后，解释描述、索引、对象身份、存储访问与 GC 协作；不把 C++ 析构或某平台布局当托管保证 |
| ICPP-3 | 方法分派、注册表和泛型上下文怎样接线 | 已嵌入D5；同一样本虚调用/泛型接线及旧四组Player证据已整理；新native虚分派待验 | 公共 CXX-03、05、07 与 CLI 之后，对照生成调用与 runtime 表，分清 C++ 模板实例化、托管泛型语义与运行时共享策略 |

顺序 ICPP-1 → 2 → 3，每项先补公共依赖，再读对应 D 篇。优先补入现有正文或其跟读案例；需要独立成篇时在本 plan 确定唯一文件和状态。三项已嵌入D2/D3/D5，不增加既有10篇正文；内容与桌面CIL检查完成，新样本Unity patch生成物/断点仍待验。执行与证据统一见[ICPP跟读记录](il2cpp-icpp-reading-outline.md)。

## 阅读顺序与状态

文件均在 `content/engine-toolchain/`。已写仅表示正文存在。

| 顺序 | 文件 | 职责与状态 |
|---|---|---|
| 导读 | `il2cpp-runtime-map-global-metadata-gameassembly-libil2cpp.md` | 已写；唯一运行时地图入口 |
| D1 | `il2cpp-architecture-csharp-to-cpp-to-native-pipeline.md` | 已写；核心机制 |
| D2 | `il2cpp-converter-il-to-cpp-code-generation.md` | 已写；核心机制 |
| D3 | `il2cpp-libil2cpp-runtime-metadatacache-class-runtime.md` | 已写；核心机制 |
| D4 | `il2cpp-global-metadata-dat-format-loading-binding.md` | 已写；核心机制 |
| D5 | `il2cpp-generic-code-generation-sharing-instantiation.md` | 已写；核心机制 |
| D6 | `il2cpp-gc-integration-boehm-wbarrier-finalization.md` | 已加强；内存分类、根与写入协议、Player 生命周期验证方案 |
| D7 | `il2cpp-ecma335-coverage-supported-unsupported.md` | 已写；核心机制 |
| D8 | `il2cpp-managed-code-stripping-linker-linkxml.md` | 已写；核心机制 |
| 按需排查 | `il2cpp-global-metadata-deep-dive.md` | 已重整；构建同源、格式兼容、符号匹配，保留旧 URL |

共享专题：`hybridclr-thread-attachment-gc-and-runtime-state.md`，已写；D3、D6 后阅读，固定源码核对完成，托管线程、原生回调正常路径及GC保留/有限压力Player验证完成；长期与其他平台待补。

共享案例：`runtime-generic-call-experiment.md`，已写；D5 后阅读，与 ECMA-335、HybridCLR 共用。固定自定义 Editor/v9 环境的两份 Player、四组运行与原生断点已验证，记录显式实例带来的原生产物变化及补元数据后的不同执行路径。

发布索引：`il2cpp-series-index.md`。推荐从导读进入 D1～D8，排障专题按需阅读；之后接 HybridCLR 源码阅读起步。

## 去重决策

原两篇“运行时地图”同名且重复。保留 runtime-map 为概念入口；deep-dive 改名并收拢成产物一致性排查，重复的 runtime 分工和文件格式解释通过链接复用。与 D4 的边界：D4 解释结构和绑定，排查专题解释证据与检查顺序。

## 待补强

- 近期先与 HybridCLR 共用 [Player 源码跟读执行计划](./hybridclr-player-reading-execution-plan.md)，完成环境、构建、模块／符号和首次／再次调用记录；本机自定义 Unity 配套 v9 的隔离实验已完成构建、PDB 与方法级断点验收；首次转换、再次复用及两次解释执行均有记录。旧 v6.1.0 失败记录保留，不视为旧版通过。
- 转换器/runtime/泛型篇已补同一自有C#、桌面实际IL和连续观察表；尚缺同源Unity构建的stripped IL、生成物与native运行时对应。
- 三栏泛型调用综合实验已完成，执行与证据见 `runtime-generic-call-experiment-outline.md`；不将功能结果推广为性能或其他平台验收。
- 当前不声称全系列经过统一 Unity patch 的 Player 验证。

旧 `il2cpp-runtime-map-article-plan.md` 是导读单篇的历史执行材料，不承担本系列进度管理。

## GC 与线程修订（2026-10-25）

修正 AOT 必然使用保守 GC、增量等同并发／无停顿等过强结论；D3 区分 WebGL 托管与原生线程能力。D6 的观察方案区分托管存活、堆容量、runtime native 内存与引擎资源。线程篇提纲与证据见 `hybridclr-thread-attachment-outline.md`。

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

- 独立projects/incremental-barriers，build_id70a8a176-4eee-43b2-8aa8-a75754ac0bfb；Editor／v9／Windows x64锁不变，GCMode改Manual作为控制变量。旧4861bed与gc-roots回执保留，本轮固定提交为 [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a)，不借用旧SHA标识。
- AOT／hot各3个普通Player、各3个匹配PDB的CDB Player全部退出0；连续12帧more=True，MARK_END完成数1→2，修改期间不变；原生GC_gc_no4→5。writer预热后常驻，串行握手；紧凑收尾，图与标记检查在Join前。
- 字段／引用数组／含引用结构体字段和数组赋值；实际CIL解析、目标槽屏障、dirty前后位、后续目标3页重扫关联。Box标记1／目标0，末尾marker4（返回前）目标0，完成后12目标全1；checksum12078。AOT16／hot24是重载入口次数，非写入或性能比例；每进程仅2次dirty新置0→1。
- hot每进程6次根查询入口top0／frames0（基线1、目标周期收尾2、周期后／退出清理3），注册注销所有者匹配；scrub后当前worker栈的当前目标精确地址12次均0处。不是所有辅助根缺失证明，也未重新采根回调返回缓冲。
- 96项Python测试通过。源码、产物、原始日志、CIL和验收工具有哈希绑定；公开结果／自有事件在实验仓库evidence/2026-10-25-incremental-barriers。完整动态根归因、屏障反事实与完整路径、并发／长期／性能及其他平台仍待补。
- 三篇GC／线程正文、三个索引同步本样本结果，不新增正文篇数，不把实现行为上升为ECMA-335规范要求。本轮源码与公开证据已随 [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a) 发布，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/results.json)。

## Prefab／AssetBundle 资源挂载实测（2026-10-26）

共享实验 `projects/resource-mounting` 已完成固定自定义 Editor／v9／Windows x64 Player 验证：code、bundle、late、unregistered、resources 五组各三个新进程。正向字段/子引用/生命周期通过；负向在资源读取阶段即缺组件，清单缺项组仍能加载DLL并调用静态方法。只改复制Player启动清单的单变量对照与主包Resources的独立资产对照分别说明，不推广为平台/版本演进或性能验收。

正文回填 HCLR-4；三栏引用同一篇。实验数据、哈希与边界见实验仓库 `evidence/2026-10-26-resource-mounting/` 和本仓 [资源挂载执行记录](./hybridclr-resource-mounting-outline.md)。本轮实验固定提交为 [74da063](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/74da063e0fe2a4cbac894acfa77d3b0df4074356)，已推送并核验远端；[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/74da063e0fe2a4cbac894acfa77d3b0df4074356/evidence/2026-10-26-resource-mounting/results.json)均锁定该提交。文稿与索引随本次文档提交交付，Pages 在推送后另行验收。

## 类型初始化与首次调用实测（2026-10-26）

独立projects/type-initialization，固定自定义Editor/v9/Windows x64锁不变。build_id5b177115-e8ae-4e58-8808-2a0bc54b092a，43输入/227产物哈希绑定。四组问题加失败外层捕获对照，共30普通Player（27完成/3热更内部catch异常逃逸差异）与2独立native取证。两份场景仅命名空间不同；最终AOT/hot DLL的标志、.cctor与自有CIL已读取，热更排除AOT。

严格初始化/泛型静态状态与失败不重跑由实际事件记录；beforefieldinit只记录一种允许时机。失败call在两侧try[29,47)内，热更内部捕获差异保留，精确根因未定。native记录显示Ping先转换，再执行.cctor；失败时Ping缓存非空而方法体未执行。

结果回填既有类型初始化、源码起步、IL2CPP runtime三篇，不新增正文；三个索引复用入口。本轮固定提交为 [46ac71c](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/46ac71c74cd068bc177d653dd143b7bb9dc482bd)，已推送并核验远端；[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/46ac71c74cd068bc177d653dd143b7bb9dc482bd/evidence/2026-10-26-type-initialization/results.json)均锁定该提交。74da063仍为前轮资源版本；文稿与索引随本次文档提交交付，Pages另行验收。无并发、循环初始化、其他平台或性能结论。执行记录见[类型初始化实验](./hybridclr-type-initialization-outline.md)。

## 跨 AOT／解释器异常传播与初始化逃逸定位（2026-10-26）

独立projects/exception-flow，固定自定义Editor/v9/Windows x64。原始基线0628a07c与隔离反事实c811dcee：43输入、自有最终AOT/hot DLL与4份生成代码完全相同，各227产物；919运行时输入仅公开Interpreter_Execute.cpp一个宏变化。临时nextImi保留方法准备顺序，初始化成功后才切换活动imi；共享运行时和旧实验不改。

两版各48个普通Player，原始45处理成功/3hh-cctor-ret两次均逃逸且finally0，反事实48处理成功且每次finally1。另每版3个关键native进程及1个内联控制，共8个。原始6个关键现场活动imi=被调PingRet、EH0，caller EH3；偏移304位于caller[48,328)，反事实保持caller imi/EH3；被调缓存两版都准备。短void实际命中InitClassStaticCtor、活动上下文保持caller。

异常处理篇集中解释根因与坐标，类型初始化篇回填后续证据，源起步/runtime篇同步状态，三个索引复用同一入口。普通抛出/手工TypeInitializationException与其他方向对照未变，不把返回类型单独当因果变量。本轮固定提交为 [f5ba510](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/f5ba510382e170ac06d2fd7033db363e6c58f62e)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/evidence/2026-10-26-exception-flow/results.json)与[公开最小补丁](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/f5ba510382e170ac06d2fd7033db363e6c58f62e/patches/exception-flow-delay-active-imi.patch)。实验已推送并核验远端；46ac71c仍是前轮类型初始化版本。文稿随本次文档提交交付，Pages另行验收。范围不含并发、性能、其他版本或完整filter/嵌套异常语义。执行记录见[异常传播实验](./hybridclr-exception-flow-outline.md)。

## async／UniTask 跨边界验证（2026-10-26）

固定自定义 Editor／v9／Windows x64，额外锁定 UniTask2.5.10/7c0f199。独立 projects/async-unitask，build_id c2a81f08-12d1-4f70-b333-d4bd26705388，44输入／264产物，919runtime与原始prepared一致。60普通Player：AOT class、hot class/struct的同步、两次跨帧、前后异常按实际事件完成；30结果／30预期异常，finally各一次。12匹配PDB进程验证hot MoveNext三Execute/一Transform及恢复缓存复用。

同一Player的metadata on/off均能运行；class命中native引用共享builder，struct命中带FullySharedGenericAny名字的native入口，补充不强制替换。样本显式preserve整个UniTask，off不等于无可用native实例；无metadata缺失失败对照，也不宣布历史Android根因闭合。HCLR17集中补结果，14保留历史边界与回链，23同步场景覆盖；三索引复用入口，不新增正文。本轮固定提交为 [b34ac10](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b34ac104fa72504d1d2a480ca936ea57a7a0fb22)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b34ac104fa72504d1d2a480ca936ea57a7a0fb22/evidence/2026-10-26-async-unitask/results.json)。实验已提交推送；文稿随本次文档提交交付，Pages另行核验；执行记录见[async实验](./hybridclr-async-unitask-outline.md)。

## constrained.／装箱调用实测（2026-10-26）

独立projects/constrained-boxing，沿用固定自定义Editor/v9/Windows x64；build_id 9e084001-a41b-42c2-8fbd-62efc0d147d4，48输入／236产物、919原始runtime。72普通Player：69完成，3个cross/off/value在GenericCalls.ByValue<HotCounter>缺可用AOT实例处停止，补充最终匹配Probe.AOT metadata后恢复。ref on/off均命中native ByRef共享入口，不推广普遍免补metadata；cross/box只作非泛型语义控制。

27个匹配PDB独立进程：原21加value on/off各3。hot箱对象两次接口调用指向同payload，unbox.any副本this不同；override无自有接收者Box命中，fallback有PlainCounter Box；按值on进入ByValue解释帧，ref仍native。on/ref准备期也会读取ByValue body，不当作实际执行证明。四篇既有正文分工：类型系统维护副本身份、调用语义集中完整实验、AOT泛型与调用链篇回链实际路径；三索引复用入口，主线篇数及URL不变。本轮固定提交为 [5638131](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/563813195813cc2d618bdf301e12ab1af0556d79)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/563813195813cc2d618bdf301e12ab1af0556d79/evidence/2026-10-26-constrained-boxing/results.json)。实验已推送；文稿随本次文档提交交付，Pages另行核验。执行记录见[本轮记录](./hybridclr-constrained-boxing-outline.md)。

## 并发首次调用与类型初始化（2026-10-26）

独立 projects/concurrent-initialization，原始固定自定义 Editor/v9/Windows x64；build_id d2edc1a5-8237-4f19-b2c6-bc260c0167b2，919 runtime 输入与 prepared 树相同。54 普通 Player：AOT/hot 的 Cold、Strict、Failed、封闭泛型各2/4 workers各3进程，mixed直接AOT/hot Bridge共享同一AOT Strict各2/4 workers各3进程。全部退出0；162次成功返回／60次预期初始化失败，成功完整发布、失败不重试、泛型静态状态隔离。

8 个独立匹配PDB进程检查释放前空缓存、最终缓存与执行身份、ClassInit竞争现场及AOT Strict等待栈。方法入口最终用硬件断点，软件探索偏差另归档；不把命中数当所有调度下的执行／转换次数。类型初始化篇集中语义与矩阵，线程篇维护门闩/Join，源码起步维护冷缓存与两条准备路径，IL2CPP runtime区分元数据准备/类型静态构造；三个索引复用同一入口，不新增主线正文。

本轮固定提交为 [8ac20ea](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b)，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b/evidence/2026-10-26-concurrent-initialization/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/8ac20eae2fe9734dc7f691e1028594be9d0dbd3b/evidence/2026-10-26-concurrent-initialization/results.json)。实验仓库已推送并核验远端，文稿随本次文档提交交付，Pages另行核验；前轮5638131不是本轮源码版本。不覆盖循环/递归初始化、原生回调竞争、长期压力及其他平台。执行记录见[本轮大纲](./hybridclr-concurrent-initialization-outline.md)。
