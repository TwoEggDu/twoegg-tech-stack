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
- 线程桌面测试把 HotWorker 与探针编译在同一控制台程序集，只验证 C# 示例，不验证热更分界或 wrapper。当时未执行 Player／原生插件；本轮 Player 补测另列于下，原生插件仍待补。
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
