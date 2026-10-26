# IL2CPP 与 HybridCLR 线程接入：提纲与证据

日期：2026-10-25。类型：原理篇。正文归 HybridCLR，与 IL2CPP、ECMA-335 索引共享入口，不增加 HCLR 编号。

## 问题与边界

原生 SDK 工作线程回调热更方法时，能进入函数不代表运行时登记、GC 可见性、业务同步与 Unity API 线程约束都已满足。本文串起这些边界，不重复 async 调度教程，也不提供脱离平台 ABI 的插件模板。

## 提纲

1. 从主线程正常、SDK 回调异常的问题出发。
2. 四层模型：线程登记、解释器 TLS 与根、共享数据同步、Unity 主线程。
3. 托管 Thread / ThreadPool 与外部 native 线程的接入差异；wrapper 的 attach 所有权。
4. MachineState 惰性创建、活跃根范围和退出清理；区分线程状态与方法缓存。
5. 托管工作线程最小实验；原生回调验证矩阵与退出顺序。
6. GC 与并发交叉问题、平台边界和排障表。

## 固定源码证据

- hybridclr v6.1.0，`5567f367502d916c6742873d8ed2ab13d0f6900a`：`interpreter/InterpreterModule.h` 的 TLS 获取／释放；`Engine.h` 的 InitEvalStack、GetGCRootData、析构；`InterpreterModule.cpp` 的 GetInterpMethodInfo。
- hybridclr_unity v6.1.0，`88e20505f87d57cbff5695be5f7baa35d626f619`：`Editor/MethodBridge/Generator.cs` 的 GenerateReversePInvokeWrappers 发出 ScopedThreadAttacher。
- il2cpp_plus v2022-6.1.0，`ce2e34097e099e458e14192bed96c28bc761392b`：`libil2cpp/vm/Thread.cpp` 的 Attach、UninitializeManagedThread、Detach；`ScopedThreadAttacher.cpp` 只释放自身建立的登记；`gc/BoehmGC.cpp` 动态根回调。正文只给行为与路径，不转载 Unity C++ 函数体。
- Unity 2022.3 GC 文档、2023.2 WebGL 平台限制；Microsoft GCHandleType 与事件退订说明。

## GC 旧文分工

- ECMA 前置：静态容器、事件委托保留与解除引用实验，GCHandle 类型语义。
- IL2CPP D6：分配／存活／回收／保留容量、增量 GC、终结与内存指标。
- HybridCLR Bridge-F：解释器动态根和写屏障接入，GC 与 metadata 卸载的不同生命周期。
- HCLR-30：收紧增量 GC 与商业能力推断，移除无测量条件的 GC 毫秒数；HCLR-27 修正活跃根范围，D3 修正 WebGL 线程绝对表述。

## 验收与待补

从文章提取 C# 示例做桌面 .NET 编译／运行检查；Hugo 构建与站内链接检查。早期桌面记录不能替代 Player 证据；新增托管线程 Player 验收见文末。原生插件回调与目标设备压力测试仍需另行验证。

## 本轮验证记录

- Windows，.NET SDK 10.0.402，Release，独立进程：从正文提取容器、事件退订和线程探针代码，编译运行成功。容器输出 `retained: True`／`after clear: False`；事件输出 `subscribed: True`／`unsubscribed: False`；线程探针输出 `caller=2, worker=4, results=5,15`。
- 线程桌面测试把 HotWorker 与探针编译在同一控制台程序集，只验证 C# 示例，不验证热更分界或 wrapper。当时未执行 Player／原生插件；后续托管线程与原生插件 Player 补测分别列于下。
- Hugo 0.157.0 extended 最终构建成功，1358 页、零 ERROR。整站页数包含共享工作区中其他文章，不代表本轮新增数量。
- 本轮 12 个内容页的 YAML、围栏及 177 个 relref 检查通过；已有页面的 slug／生成 URL 所依据的标题保持不变。
- 生成的 64 个运行时正文／索引页共 3510 个本地链接及锚点检查通过；三个索引均能进入新线程篇，新篇采用既有 runtime 正文布局。
- 本地浏览器检查了新篇面包屑、正文与目录样式。本文及 3 份 canonical 系列计划已回写；未改根 doc-plan 的篇级目录，未提交或推送。

## 对象保留与托管线程 Player 实测（2026-10-25）

- 复用 hybridclr-runtime-lab 新增 projects/lifetime-threads，独立 artifacts/lifetime；环境仍为本机自定义 Unity 2022.3.62f3_96770f904ca7、package v9.0.3 / runtime v9.0.1、配套 il2cpp_plus v2022.3.x-9.0.1。
- build_id：70021d1d-8d18-4bdf-8066-446dd9898b80。GC 与 threads 各三个无调试器独立进程通过；另一个匹配 PDB 的 CDB 进程完成方法／线程／状态关联。
- GC 每组 16 个目标：held 为 0/16/16，清空静态数组后 0/0/16，清空事件列表后 0/0/0；三轮一致。GCMode=Enabled、isIncremental=True。这里是弱引用观测，不是根快照或内存容量测量。
- 两个托管 worker 各返回 5/15 并 Join；两份 MachineState 生命周期重叠；同一 Add 四次 Execute、一轮 Transform、相同非空 interpData；各 worker 的 FREE、析构、动态根注销入口完整匹配。并发首次转换、native 回调、活跃根区间与性能均未验收。
- 结果回填 IL2CPP D6、HybridCLR Bridge-F 和共享线程篇；三个索引更新验证状态。实验仓库已推送 [固定提交 `a387d28`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/a387d2807f4251fce0e5fc24e8d1debddb4ca79d)，[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/results.json)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/a387d2807f4251fce0e5fc24e8d1debddb4ca79d/evidence/2026-10-25-lifetime-threads/README.md)均使用固定地址；旧 e0220099 保留为调用链／泛型实验的版本。

本轮文章验收：Hugo 0.157.0 extended 构建成功，1379 页、零 ERROR；页数包含共享工作区其他文章。3 篇正文与 3 个索引生成页的 328 个站内链接／锚点通过；新增实测段经独立复核，与实验摘要数字、版本和限制一致。后续按用户授权补固定提交链接并进行文章交付；线上可见状态仍以 Pages 部署为准。

## 原生回调 Player 实测（2026-10-25）

- 用户已授权继续原生插件闭环。新增实验仓库 projects/native-callbacks 与自有 native/native-callbacks/plugin.cpp；原有环境锁不变，build_id=65d3ddb9-3bb0-4ce9-99b9-59f4ff1cc82b。
- registered/raw 各三个独立 Player 退出0；两组再分别用匹配 GameAssembly/NativeProbe private PDB 取证。registered 两次5/15，raw 三次5/15/23。
- registered wrapper 的 owned=0，复用1个状态；raw 每次scope拥有登记，3次Attach、6个Detach重载入口、3轮状态/根清理。同一原生OS ID出现managed ID 5/6/7。两组目标方法分别只Transform 1次，首次Execute缓存明确为空，后续复用。
- raw 在第三次回调Hold时stop，Join(0)未完成；放行后回调结束/线程退出/Join，最后释放委托，无第四次回调。只验收正常单任务路径；GC压力、多原生线程竞争、上下文GCHandle、异常注入、强杀和其他平台未覆盖。
- 实验仓库已推送 [固定提交 `b915632`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/b9156325820312f4d290c4af8158f76a3a6f9beb)，[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/README.md)与[完整结果与哈希](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/b9156325820312f4d290c4af8158f76a3a6f9beb/evidence/2026-10-25-native-callbacks/results.json)使用固定地址；a387d28仍是前序对象保留/托管线程基线。线程正文及三个索引同步状态，不新增正文篇数。

本轮验收：独立复核代码、真实日志与新增正文，未发现阻断问题；解析器两项漏检已补负例并修复，22 项测试通过。Hugo 0.157.0 extended 构建 1397 页、零 ERROR（共享工作区总量）；线程正文与三个索引的 245 个站内链接／锚点检查通过。后续按用户授权补固定实验链接并交付文章；线上状态以 Pages 部署为准。

## 原生回调 GC 保留与有界压力（2026-10-25）

- 原实验环境锁不变，build_id=148235df-910b-441f-94b2-b7348eb02d8c；自有新工程 projects/gc-callbacks、native/gc-callbacks/plugin.cpp、artifacts/gc-callbacks。
- handle、stack、stress各三个独立Player退出0。Normal句柄的Context在Free前保留；无上下文句柄的活跃回调Node跨3次收集继续校验；退出/Join/解除持有后，弱引用在观察窗口内均失效。释放后仍存活的情况保留为观察，不强制验收归零。
- 压力组1024回调、每次32节点、256字节payload，共8,388,608字节payload；64槽Id/占用、Head身份、Next及payload检查通过，checksum786944/errors0。
- 32检查点共128增量请求，预算1,000,000ns；more均0，整个负载窗口GC.CollectionCount(0)差值均256。窗口排除前/后显式持有检查；计数不单API归因，不解释为256次完整Full或性能结果。
- handle/stack分别匹配PDB取证：活跃回调内根注册后、scope退出前各3次Full入口，Join后handle组FreeContext1次，stack0次。stress未逐次原生断点跟踪。没有实际根区间、根快照、唯一根归因、持续跨片段标记引用修改或写屏障完整性结论。
- 31项Python测试和4项直接编译热更源码的桌面破坏测试通过，后者不作为Player/GC验证。独立审查提出的对象图关系、检查点相位、显式收集计数门槛已修正。
- 实验仓库已推送固定提交 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，见[GC 回调样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/449a9949122e791010624f4c799495d493cca9f4/projects/gc-callbacks)与[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/449a9949122e791010624f4c799495d493cca9f4/evidence/2026-10-25-gc-callbacks/README.md)；b915632仍为前序原生回调正常路径版本。GC/线程三篇正文及三个索引已回填，不增加正文篇数。

本轮文章验收：Hugo 0.157.0 extended 构建1401页、零ERROR（共享工作区总量）；3篇GC/线程正文和3个索引的349个站内链接/锚点通过。实验源码、产物、日志回执已复核；实验仓库已推送固定提交 [449a994](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/449a9949122e791010624f4c799495d493cca9f4)，文章按本轮授权同步提交发布。

## 对象快照与活跃根区间验证（2026-10-25）

- 独立 `projects/gc-roots`，build_id `993e5b9e-1f71-4949-b339-39ff10d359b1`；环境锁不变，旧gc-callbacks源码与产物回执保留。
- 最终24个Player进程退出0：handle/stack/idle各3个无快照对照、handle/stack各3个快照进程、三组各3个CDB进程。额外ABI检查仅作调试器布局验证，不算业务通过进程。
- 实际GC调用栈含GC_push_roots，GetGCRootData入口/返回缓冲成对采样。每组9个返回：活跃handle与stack均36槽/288字节，目标偏移24与48/264；idle线程仍存活且backing容量131072槽，但解释帧/stackTop0，实际返回null/0。根注册/注销完整匹配。
- 21份ManagedObjects快照：handle四阶段强目标1/1/1/0，同一Context→Slots64槽数组路径可见；stack三阶段无热更强句柄目标。所有ManagedStacks条目0，未提供解释器动态根边，不作唯一根归因。
- 独立Editor分析工程使用官方Memory Profiler1.1.0，21份快照共189组条目数量和缓冲哈希与自有读取器一致。包仅作分析依赖，不修改Player配置。57项Python测试通过。
- 实验仓库已推送固定提交 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)，入口为[GC根区间样本](https://github.com/TwoEggDu/hybridclr-runtime-lab/tree/4861bedd35f9785038a15d34c8de38f261746867/projects/gc-roots)、[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/README.md)、[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/4861bedd35f9785038a15d34c8de38f261746867/evidence/2026-10-25-gc-roots/results.json)与同目录三组自有root-events；真实.snap、PDB和原始调用栈仅在忽略目录。449a994仍是前轮保留/压力基线。
- 三篇正文和三个索引补入实际返回区间与对象快照的证据边界。完整动态根路径归因、持续跨增量片段引用修改、性能、长期压力及其他平台仍待补；正文篇数不变。

本轮站点验收：仅覆盖本轮11个文件的隔离候选Hugo0.157.0 extended构建1405页、零ERROR；三篇正文和三个索引366个站内链接/锚点通过。独立复核修复注销所有者漏检及inspect步骤遗漏后通过；实验仓57项Python测试通过。实验仓库已推送固定提交 [4861bed](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/4861bedd35f9785038a15d34c8de38f261746867)；文章按后续用户授权随本次调整提交发布，Pages上线状态另行核验。

## 持续跨增量片段的引用修改与写屏障（2026-10-25）

- 独立projects/incremental-barriers，build_id70a8a176-4eee-43b2-8aa8-a75754ac0bfb；Editor／v9／Windows x64锁不变，GCMode改Manual作为控制变量。旧4861bed与gc-roots回执保留，本轮固定提交为 [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a)，不借用旧SHA标识。
- AOT／hot各3个普通Player、各3个匹配PDB的CDB Player全部退出0；连续12帧more=True，MARK_END完成数1→2，修改期间不变；原生GC_gc_no4→5。writer预热后常驻，串行握手；紧凑收尾，图与标记检查在Join前。
- 字段／引用数组／含引用结构体字段和数组赋值；实际CIL解析、目标槽屏障、dirty前后位、后续目标3页重扫关联。Box标记1／目标0，末尾marker4（返回前）目标0，完成后12目标全1；checksum12078。AOT16／hot24是重载入口次数，非写入或性能比例；每进程仅2次dirty新置0→1。
- hot每进程6次根查询入口top0／frames0（基线1、目标周期收尾2、周期后／退出清理3），注册注销所有者匹配；scrub后当前worker栈的当前目标精确地址12次均0处。不是所有辅助根缺失证明，也未重新采根回调返回缓冲。
- 96项Python测试通过。源码、产物、原始日志、CIL和验收工具有哈希绑定；公开结果／自有事件在实验仓库evidence/2026-10-25-incremental-barriers。完整动态根归因、屏障反事实与完整路径、并发／长期／性能及其他平台仍待补。
- 三篇GC／线程正文、三个索引同步本样本结果，不新增正文篇数，不把实现行为上升为ECMA-335规范要求。本轮源码与公开证据已随 [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a) 发布，见[实验记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/README.md)与[结构化结果](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a/evidence/2026-10-25-incremental-barriers/results.json)。

本轮最终验收：独立复核修正marker4返回前采样、根查询阶段和事件时序漏检；96项Python测试通过。仅覆盖本轮11路径的隔离候选Hugo0.157.0 extended构建1408页、零ERROR；3篇正文及3个索引的382个站内链接／锚点通过。实验源码与公开证据已随 [64938d5](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/64938d5c5040fbc2f63274ee4efd8ab6b3c9d07a) 推送；本文稿为后续文档同步，不据此宣称Pages已上线。

## A13 补证范围复核（2026-10-25）

本轮只读复用lab当前记录，不覆盖其他对话工作。`python -B tools/check_repo.py`检查408公开文件通过；`python -B -m unittest discover -s tests`96项通过；29/30/30份lifetime/gc-roots/incremental源码hash全匹配，六份既有跨片段普通Player日志原解析器重读通过。没有启动新的Player，没有把验收器单测当GC或并发运行证据。

选取当前首调样本未含显式`.cctor`的缺口，在源码起步篇登记单线程类型初始化顺序的最小输入/采证要求；本轮不写外部lab，不重复已有GC/回调/根实验。其他ABI、constrained./装箱、异常、并发/长期压力仍独立待补。
