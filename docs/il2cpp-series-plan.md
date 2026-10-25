# IL2CPP 系列计划

更新：2026-10-25。本文是该系列唯一的 canonical 篇级目录。目录含 10 篇正文，另引用 1 篇归属 HybridCLR 的共享线程专题，索引另计。

## 定位与边界

连接 ECMA-335 的程序描述与 HybridCLR 的动态扩展：先理解产物分工，再沿转换器、runtime、metadata、泛型、GC、能力边界和裁剪阅读。公开仓只描述行为与实验，不转载 Unity 受限 C++ 实现。

## 公共基础之后的源码专项前置

公共语言目录归 [C++ 公共基础 plan](cpp-foundations-series-plan.md)，已在本系列索引回链。优先 CXX-01～03、05～06、09～10；模板/线程随目标入口补 07、11。CLI 类型、调用和泛型语义继续由 ECMA-335 前置层负责。

| 次序 | 专项拟题 / 工作 | 状态与复用 | 完成标准 |
|---|---|---|---|
| ICPP-1 | 同一程序的 C#、IL 与生成代码怎样对应 | 待整理；复用导读和 D1/D2，已有示意不等于固定构建实测 | 公共 CXX-01、05、10 之后，以允许公开的自有样本核对参数、入口、返回值及构建身份；描述转换产物与 runtime 的区别 |
| ICPP-2 | metadata、运行时对象与原生存储怎样分开 | 待补；复用 D3/D4/D6 | 公共 CXX-02、06、09 之后，解释描述、索引、对象身份、存储访问与 GC 协作；不把 C++ 析构或某平台布局当托管保证 |
| ICPP-3 | 方法分派、注册表和泛型上下文怎样接线 | 待补；复用 D2/D3/D5 及泛型共享桥接 | 公共 CXX-03、05、07 与 CLI 之后，对照生成调用与 runtime 表，分清 C++ 模板实例化、托管泛型语义与运行时共享策略 |

顺序 ICPP-1 → 2 → 3，每项先补公共依赖，再读对应 D 篇。优先补入现有正文或其跟读案例；需要独立成篇时在本 plan 确定唯一文件和状态。三项当前不计入既有 10 篇正文，固定 Unity patch 的生成物、断点与验证结果不能由本轮规划推定。

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

共享专题：`hybridclr-thread-attachment-gc-and-runtime-state.md`，已写；D3、D6 后阅读，固定源码核对完成，Player／原生回调验证待补。

共享案例：`runtime-generic-call-experiment.md`，已写；D5 后阅读，与 ECMA-335、HybridCLR 共用。固定自定义 Editor/v9 环境的两份 Player、四组运行与原生断点已验证，记录显式实例带来的原生产物变化及补元数据后的不同执行路径。

发布索引：`il2cpp-series-index.md`。推荐从导读进入 D1～D8，排障专题按需阅读；之后接 HybridCLR 源码阅读起步。

## 去重决策

原两篇“运行时地图”同名且重复。保留 runtime-map 为概念入口；deep-dive 改名并收拢成产物一致性排查，重复的 runtime 分工和文件格式解释通过链接复用。与 D4 的边界：D4 解释结构和绑定，排查专题解释证据与检查顺序。

## 待补强

- 近期先与 HybridCLR 共用 [Player 源码跟读执行计划](./hybridclr-player-reading-execution-plan.md)，完成环境、构建、模块／符号和首次／再次调用记录；本机自定义 Unity 配套 v9 的隔离实验已完成构建、PDB 与方法级断点验收；首次转换、再次复用及两次解释执行均有记录。旧 v6.1.0 失败记录保留，不视为旧版通过。
- 转换器与 runtime 篇：补固定构建样本的 C#、IL、生成物和运行时对象对应关系。
- 三栏泛型调用综合实验已完成，执行与证据见 `runtime-generic-call-experiment-outline.md`；不将功能结果推广为性能或其他平台验收。
- 当前不声称全系列经过统一 Unity patch 的 Player 验证。

旧 `il2cpp-runtime-map-article-plan.md` 是导读单篇的历史执行材料，不承担本系列进度管理。

## GC 与线程修订（2026-10-25）

修正 AOT 必然使用保守 GC、增量等同并发／无停顿等过强结论；D3 区分 WebGL 托管与原生线程能力。D6 的观察方案区分托管存活、堆容量、runtime native 内存与引擎资源。线程篇提纲与证据见 `hybridclr-thread-attachment-outline.md`。

## 配套实验仓库同步（2026-10-25）

公开仓库：[hybridclr-runtime-lab](https://github.com/TwoEggDu/hybridclr-runtime-lab)；文章固定提交 [`e0220099`](https://github.com/TwoEggDu/hybridclr-runtime-lab/commit/e0220099d293f7a1a4601ee9931b49a10dd519f9)（`e0220099d293f7a1a4601ee9931b49a10dd519f9`）。三个专栏索引同步复现与证据入口，源码阅读起步、调用链实战和泛型综合实验共用该仓；不新增正文或改变主线篇数。

新仓库三份 Player 已重新构建，Add 与四组泛型普通运行、原生断点均符合预期，公开证据见 [本次记录](https://github.com/TwoEggDu/hybridclr-runtime-lab/blob/e0220099d293f7a1a4601ee9931b49a10dd519f9/evidence/2026-10-25/README.md)。正文区分原始隔离实验与新仓库构建的哈希、进程地址；自定义 Editor 与平台限制不变。本节记录文章与索引的同步内容；线上可见状态以 Pages 部署结果为准。
