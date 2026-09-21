# UE 全栈内容盘点快照

> 核查日期：2026-09-20。对象：`TwoEggDu/twoegg-tech-stack` 当前工作区。
> 本文是一次有范围的证据快照，不是长期维护的文章目录。贯通路径及新增桥梁内容的唯一权威计划见 [UE 全栈工程师成长路线](ue-fullstack-expert-series-plan.md)。

## 1. 快照身份与检查边界

- 分支：`main`；HEAD：`b27b4057fa87f64bc11110cd2cc9e69d738b1f19`。本次没有刷新远端，结论仅针对此本地工作区。
- 初始已有修改：`docs/agent-engineering-course/README.md`、`course-run-state.md`、`status.md`；初始未跟踪项：`.codex_tmp/`、`docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/`。本次不编辑这些文件，也不重置、清理或暂存。
- 已读根 [AGENTS.md](../AGENTS.md)、[CLAUDE.md](../CLAUDE.md)；后者与任务相关规则重复，未引入另一套规则。文件清单中没有 `docs/`、`content/` 下的子目录 AGENTS.md；`kb/CLAUDE.md` 属于未进入的知识沉淀层。
- 已读 [根规划入口](../doc-plan.md)、[系列规划方法](series-planning-method.md)、[文章生产流程](article-production-workflow.md)、[写作方法](article-writing-method.md)、[大纲模板](article-outline-template.md)。只为后续文章定边界，本次不生成逐篇工单。
- 先检索 docs/content 的文件名、标题、入口与目录，再按表中范围读正文；检索词包括 UE、GAS、Dedicated Server、Mass、RAII、移动语义、Enhanced Input、UMG、Asset Manager、数据库、缓存、幂等、同步、交付、测试、崩溃、性能等。
- 建文前检索 `TwinArena`、`UE 全栈`、`UE全栈`、`ue-fullstack`、`成长路线` 及相近计划文件名，未定位到同义权威计划。现有 Unreal 专题和游戏服务端总图是上游专题，不承担这次四领域贯通实践。
- `.codegraph/` 不存在，因此按仓库规则跳过 CodeGraph，使用文件列表和 `rg`。
- [源码路径登记](engine-source-roots.md) 的 Unity、Unreal 都为 `TODO`，根路径为空。本次没有访问引擎源码、运行 UE/DS/数据库或核查用户硬件，所有源码研究和工程实验仍是计划。

“已有正文”只证明文件有实质章节，不表示文章全部正确、示例已编译或作者已经掌握。下表的“直接复用”限定在已核查章节及所述概念用途；不向同系列未读文章外推质量结论。行号是此次快照定位，后续编辑可能变化。

## 2. 规划、正文与历史材料分别是什么

| 材料 | 本次核查范围 | 职责与当前观察 |
| --- | --- | --- |
| [UE 导读](../content/system-design/unreal-engine-series-index.md) | 全文及 frontmatter | 已有读者入口；推荐架构 → GAS → 网络。不是 UE 全栈工程实践 canonical plan |
| [引擎架构地图](game-engine-architecture-series-plan.md) | 全文 | 专题权威规划，按系统分层；不能据此要求读完内部机制再做第一个项目 |
| [引擎渲染栈](game-engine-rendering-stack-series-plan.md) | 全文 | 渲染基础地图，区别引擎流程、图形管线、API、驱动、GPU；本路线按需路由 |
| [游戏服务端总图](game-server-topic-plan.md) | 定位/边界/依赖、模块、建议文章队列及推进顺序 | 服务端主题权威入口；部分“待写”状态落后于实际文件 |
| [服务端 ECS](game-server-ecs-high-performance-series-plan.md) | 全文 | 专项权威规划；末尾所有 Part 标待写，但已存在对应正文文件；本次核查 08、10 的部分正文 |
| [技能系统](skill-system-series-plan.md) | 定位/边界、主线表、第二轮扩展表及相关正文 | 权威目录已扩展至 36；前面的“15/15、16 篇”是原主线口径，不能用它推断扩展未写 |
| [ET 正文](et-framework-series-plan.md) | 定位、前置、证据边界、篇级目录、当前状态 | 结尾“已完成规划”不是正文完成声明；实际另有 content 文件，未全读 |
| [ET 前置](et-framework-prerequisites-series-plan.md) | 定位、边界、目录、详细职责标题、阅读顺序/状态 | 前置概念可借用；不把 ET 的分布式运行时作为 TwinArena 第一版依赖 |
| [架构执行计划](game-engine-architecture-series-execution-plan.md)、[ET 工单总控](et-framework-workorders-master.md) 及 outline 文件 | 文件存在性/职责识别；未做执行内容审计 | 执行层，不能反向成为系列目录或工程跑通证明 |
| [历史 v26 总表](doc-plan-archive-v26.md)、[旧 docs/doc-plan.md](doc-plan.md) | 根入口对历史身份的说明；未读历史篇级正文 | 只保留线索身份，不修改、不用旧编号判定当前状态 |
| [技术底座优先级](tech-foundation-priority-plan.md) | 目标、当前判断、近期不优先扩写段落 | 原站点优先收口工程证据；本次用户授权增加学习路径，不因此改写全站近期排期 |

## 3. 已有材料映射：正文证据

这是所检查材料的用途清单。编号 A01–A26 仅用于这次审计关联，不给原文章重新编号。

| 审计项 / 主题 | 已有文件 | 已核查范围及依据 | 内容状态 |
| --- | --- | --- | --- |
| A01 Unity → UE 世界模型 | [架构地图 02](../content/system-design/game-engine-architecture-02-gameobject-vs-actor.md) | 1–115 行：对象世界、职责边界、明确只用官方文档；不写创建教程 | 已有正文；本次未核源码 |
| A02 C++ 构建认知 | [构建与调试前置 02](../content/engine-toolchain/build-debug-02-cpp-vs-csharp-debug-and-release.md) | 20–104 行：原生编译链、优化与符号、C# 运行时差异 | 已有正文；不等于 C++ 所有权课程 |
| A03 UObject 生命周期 | [UE-01](../content/system-design/ue-01-uobject-system.md) | 125–171 行：创建/销毁、Outer 对象层级及 GC 断言 | 已有正文；有关 Outer/强持有说法待源码与实验核查 |
| A04 模块与构建 | [UE-07](../content/system-design/ue-07-module-system.md) | 99–168 行：UBT、Target、模块类型、启动和卸载 | 已有正文；没有本次版本的构建回执 |
| A05 玩法框架和 Travel | [UE 网络 04](../content/system-design/ue-net-04-player-controller.md) | 全文：存在关系表、登录、PlayerState/GameState、SeamlessTravel | 已有正文；局部表述待修订，见第 5 节 |
| A06 网络权威与复制 | [UE 网络 01](../content/system-design/ue-net-01-architecture.md) | 全文：NetDriver、角色、属性复制、相关性、ReplicationGraph | 已有正文；需要明确实现/版本边界 |
| A07 最小 DS 构建 | [UE 网络 06](../content/system-design/ue-net-06-dedicated-server.md)、[后端 DS 03](../content/system-design/game-backend-ded-srv-03-unreal-ds.md) | 两篇全文：Target、Cook、启动、资产缺失和 PIE 差异；没有随文可核对的 TwinArena 三进程产物 | 已有正文；需要工程实践和版本更新 |
| A08 移动同步 | [UE 网络 03](../content/system-design/ue-net-03-movement-sync.md) | 19–77 行：三端角色、SavedMove、修正重播 | 已有正文；自定义移动示例未审计 |
| A09 GAS 预测 | [GAS-08](../content/system-design/ue-gas-08-network-prediction.md)、[技能系统 16](../content/system-design/skill-system-16-gas-prediction-layer.md) | 前者全文；后者 259–307 行预测边界；两篇对伤害、效果移除的适用条件需统一 | 已有正文；预测细节待版本核查 |
| A10 客户端表现与技能回归 | [技能系统 09](../content/system-design/skill-system-09-animation-and-presentation-decoupling.md)、[技能系统 13](../content/system-design/skill-system-13-testing-and-regression.md) | 09 的 157–243 行：逻辑主时序与表现事件；13 的 346–394 行：配置、事件、边界场景、整场景测试 | 已有正文；可复用职责与测试分层 |
| A11 UE 渲染/线程地图 | [UE-04](../content/system-design/ue-04-rendering-architecture.md)、[UE-05](../content/system-design/ue-05-threading-model.md) | 分别 20–41、20–47 行：场景代理、RDG/RHI、线程分工/流水线 | 已有正文；只核查结构入口，代码未验证 |
| A12 UE 性能采集 | [UE 性能 01](../content/system-design/ue-perf-01-profiling-workflow.md) | 帧预算、66–163 行 Stat/Insights 采集与分析；示例数字无本次原始数据 | 已有正文；采集命令需修订验证 |
| A13 资源、内存、PSO | [UE 性能 04](../content/system-design/ue-perf-04-memory-streaming.md) | 内存分类、Streaming、软引用/异步加载、PSO Cache 章节；未据此核验全部 GC 参数 | 已有正文；已包含加载章节，不能说资源话题空白 |
| A14 复制优化 | [UE 网络 05](../content/system-design/ue-net-05-optimization.md) | 177–末尾：监控命令与 Iris，正文明确“目前（UE5.3）” | 已有正文；版本口径待更新 |
| A15 Mass 可选分支 | [Mass-06](../content/system-design/mass-06-actor-boundary.md) | 20–109 行：Entity 与表现分离、Actor/ISM 等表示及示例结构 | 已有正文；框架类型/代码/规模数字未验证 |
| A16 Tick 与异步 I/O | [SV-ECS-08](../content/system-design/sv-ecs-08-async-io-boundary.md) | 27–69 行：仿真与 I/O 接缝、队列图；输出队列图的消费者标注需复核 | 已有正文；非 UE 接入实现 |
| A17 仿真与持久化 | [SV-ECS-10](../content/system-design/sv-ecs-10-persistence-boundary.md) | 1–70 行：World 不宜直接整体入库、冷热状态与恢复取舍 | 已有正文；证明旧规划待写状态失配 |
| A18 事务与并发 | [数据库 04](../content/system-design/game-backend-db-04-transaction-and-concurrency.md) | 97–171 行乐观/悲观锁及原子扣减，另检索隔离级别相关段落；以 MySQL/InnoDB 为主 | 已有正文；不能原样当 PostgreSQL 隔离语义 |
| A19 奖励幂等 | [道具发放](../content/system-design/game-backend-economy-02-item-distribution.md) | 51–120 行：主键、ON CONFLICT、背包与流水同事务；不是只有标题 | 已有正文；缺目标工程故障注入证据 |
| A20 匹配与分配 | [匹配系统](../content/system-design/game-backend-sync-03-matchmaking.md) | 房间创建/DS 分配、Agones 预热池、监控章节 | 已有正文；框架偏重，不必用于第一版 |
| A21 对局生命周期 | [会话状态机](../content/system-design/game-backend-ded-srv-07-session-lifecycle.md) | 1–130 行：状态转换、DS 就绪、会话凭证/容量/重复加入 | 已有正文；不能误报入场验证完全缺失 |
| A22 重连与崩溃 | [DS 重连](../content/system-design/game-backend-ded-srv-08-reconnect.md) | 104–204 行：凭证、状态快照/事件、失败降级；提到 DS 崩溃但未构成 UE 恢复实验 | 已有正文；需工程实践 |
| A23 缓存与会话基础 | [缓存一致性](../content/system-design/game-backend-cache-02-consistency.md)、[ET-Pre-05](../content/et-framework-prerequisites/et-pre-05-session-request-response-timeout-and-heartbeat.md) | 缓存 29–61 行；ET-Pre-05 全文：会话、超时、心跳 | 已有正文；可按需复用，缓存保证需注明前提 |
| A24 负载与稳定性 | [压测](../content/system-design/game-backend-depth-03-load-testing.md)、[稳定性测试](../content/delivery-engineering/delivery-verification-testing-05-stability.md) | 压测 29–65 行含用户行为模型；稳定性 24–93、90–149 行含长跑、弱网、切后台 | 已有正文；没有 TwinArena 三端基线 |
| A25 协议、备份、迁移 | [协议版本](../content/delivery-engineering/delivery-server-versioning-04-protocol.md)、[数据库运维](../content/delivery-engineering/delivery-server-operations-05-database.md) | 协议 24–109 行；DB 运维 28–89、145–174 行：恢复验证/迁移/RPO/RTO | 已有正文；引擎复制兼容、PostgreSQL 操作需另验 |
| A26 崩溃与符号 | [Windows 崩溃 03](../content/engine-toolchain/crash-analysis-03-windows.md) | 93–127 行：PDB 匹配、dump；正文以 Unity IL2CPP 为例 | 已有正文；UE/Linux 路径未覆盖此次核查 |

## 4. 已有材料映射：如何进入路线

阶段“一/二/三”指主计划的三期；模块编号指模块 0–12。处理方式是对上述已核查内容的建议，不是发布状态变更。

| 审计项 | 适用阶段 | 处理方式 | 缺少的前置或实践 / 判断依据 |
| --- | --- | --- | --- |
| A01 | 一，模块 0/3 | 直接复用 | 世界模型对照可读；实际创建/接管/切图需生命周期实验 |
| A02 | 一，模块 1/2 | 增加前置说明 | 原生链说明已有，RAII、所有权、移动语义与模板阅读测评未找到可直接替代单元 |
| A03/A05 | 一，模块 3 | 修订或更新 | 用分进程对象矩阵、出生/销毁日志校验；避免将 Outer、所有权、GC 持有混同 |
| A04 | 一，M0 | 增加工程实践 | 从空工程到包、日志、符号、复现说明的版本化回执 |
| A06 | 一，M1 | 增加前置说明 | TCP/UDP、延迟、连接、状态/事件；说明常规复制与可选 Graph 的边界 |
| A07 | 一，M0/M1 | 修订或更新；增加工程实践 | 补源码构建条件、锁定工具链，两个打包客户端和 Linux DS 实测 |
| A08 | 一，M1/M3；二，模块 9 | 增加工程实践 | 先普通移动，后故意制造修正；不强制先改 CharacterMovement |
| A09 | 一，M3 | 修订或更新；增加工程实践 | 基础网络先行；成功、拒绝、死亡取消、重复表现同一用例矩阵 |
| A10 | 一，模块 4/6 | 直接复用 | 复用表现边界/回归顺序；UE 输入、UI、动画、碰撞、AI 的实际挂接另补 |
| A11 | 一，M4；二，模块 8 | 增加前置说明 | 先能抓 Trace 定位，再读线程与渲染；RDG 代码及 Nanite/Lumen 专项后置 |
| A12 | 一，M0/M4 | 修订或更新 | 区分 Stat 记录与 Insights Trace；重跑命令并交付可打开的原始文件 |
| A13 | 一，模块 7；二，内容更新 | 增加工程实践 | 持有/释放、切图循环、Cook 缺包、首用冷暖对照；不是新写“软引用是什么” |
| A14 | 二，模块 9 | 修订或更新 | 同一版本/负载比较常规复制、Graph、Iris；不照搬 UE5.3 状态 |
| A15 | 二/三，按需 | 待核查，暂不下结论 | Representation 和类型先核版本；没有需求和测量前不上 Mass |
| A16/A17 | 二，模块 9/10 | 增加前置说明；修订或更新 | 借用问题地图；队列保证、恢复可丢状态和 UE 游戏线程边界重新验证 |
| A18 | 一，M2 | 增加前置说明 | PostgreSQL 默认隔离、约束、冲突/死锁重试和索引的独立实验 |
| A19 | 一，M2 | 增加工程实践 | 多实例并发、同键异结果、提交成功响应丢失、跨进程重试；不能只做先查后写 |
| A20/A21 | 一，M2 | 新增桥梁文章 | 最小匹配→分配→凭证→DS 入场→结果提交；复用状态机，首版无需 Agones |
| A22 | 一，M3/M5 | 增加工程实践 | DS 存活时重连、DS 死亡后中止、已持久化结果查询分别验收 |
| A23 | 一，M2；二，缓存 | 增加前置说明 | 超时不证明未提交；缓存可重建与持久账本分开；ET Actor 不等于 AActor |
| A24 | 一，M4/M5 | 增加工程实践 | 真正移动/战斗/结算的负载，实测延迟和长跑窗口；区分 UDP 仿真与 HTTP 代理 |
| A25 | 一，M5；二，升级 | 修订或更新；增加工程实践 | 应用协议兼容不证明 UE 复制兼容；备份方法按 PostgreSQL 核查，不搬 Binlog 命令或示例阈值 |
| A26 | 一，M0/M5 | 新增桥梁文章 | 复用符号精确匹配原则，补 UE 客户端/DS 构建身份和 Linux core 的取证练习 |

### 尚未找到可直接复用闭环的缺口

以下“确认缺失”仅指本次检索范围内，未定位到满足该交付物的内容单元；不是断言整个仓库从未提过相关概念。

| 需要的交付物 | 处理方式 | 已检查的相邻材料与缺口边界 |
| --- | --- | --- |
| Unity/C# 开发者的 C++ 最小能力闸门 | 确认缺失，需要新增 | A02、A03 是相邻内容，不能替代所有权/RAII/编译链接/数据竞争的独立练习；通识只补最低门槛 |
| 一个从空项目到独立包的 UE 工程基线 | 确认缺失，需要新增 | A04、A07 有构建片段；未找到统一版本、配置、符号、失败复现的工程交付记录 |
| 客户端基础系统的 UE 实践路径 | 新增桥梁文章 | A01、A05、A10 和专业子系统地图有概念；未找到覆盖输入/镜头/UI/动画/碰撞/AI/音频特效/设置存档的同项目实践 |
| TwinArena 三进程联机与业务闭环 | 确认缺失，需要新增 | A07、A19–A22 各有单点章节；未找到同一 UE＋Linux DS＋ASP.NET Core＋PostgreSQL 的可运行接线与故障证据 |
| 三端联合证据包与可交接工程 | 新增桥梁文章 | A24–A26 已有方法；需补版本身份、原始数据、容量口径、诊断复现和第三方运行结果 |

## 5. 重复、风险与修订归属

1. **阅读顺序**：现 UE 导读适合已有 UE 使用经验、想按内部子系统查阅的人。新路线面向 UE 初学的资深 Unity 开发者，先少量对象/框架与基础客户端，再基础网络和独立 DS，再 GAS 预测。后续只在原导读补这条分流说明，不改旧篇号；本次不修改 content。
2. **DS 双入口**：UE 网络 06 偏网络端构建速览，后端 DS 03 偏 Target/Cook/部署。保留原归属；新桥梁只讲三进程验收与业务接入，引用两者，不再开第三篇 DS API 百科。
3. **对象和权限的说法**：UE 网络 04 的“PlayerController 是唯一一个同时存在两端的 Actor”与同文 GameState/Pawn 的表格相冲突；需在原文修订。UE-01 的 Outer 与 GC 持有表述须按锁定版本核源码及最小实验，本次不宣称已验证正确替代实现。
4. **GAS 两种层次**：GAS-08 是入口，技能系统 16 是执行链与边界深入。把二者预测范围及效果移除/伤害/表现清理的口径送回各原专题核查，不新造一套 GAS 权威目录。GAS 预测、移动预测、世界回滚分开验收。
5. **版本与证据**：UE 网络 05 仍引用 UE5.3 的“目前”；UE 性能 01 将 `stat startfile` 直接描述为 Insights `.utrace`，需要用锁定版官方采集路径和实际文件复核；DS 文的构建时长、性能文的耗时、Mass 的规模数不能成为本项目基准。没有来源/原始记录的数字只作说明性例子。
6. **奖励已写，不要重复造文**：A19 后文已有主键冲突和同事务，不能只截取前面的 SELECT 就认定它没有并发防护。真正要补的是授权 DS、结果冲突、响应丢失和进程恢复的整链实测。
7. **旧规划状态**：SV-ECS-10 有实质正文，而服务端总图与 ECS plan 仍标待写。建议下一次在其原 canonical plan 专项回写；本审计不变成替代状态表。技能 plan 的第一轮计数与第二轮扩展也要分开阅读。
8. **分布式不前置**：A20 已进入 Open Match/Agones 语境，本次以模块化单体和一局一 DS 进程连接它的职责模型；不因此安装消息队列、Redis、Kubernetes 或 ET。
9. **交付边界**：资源更新、代码重发版、业务协议、UE 网络兼容、数据库迁移分别立契约。Live Coding 不能由其开发期用途直接推出正式热更新方案；地图流送不证明跨服务器世界分区。
10. **许可边界**：现文出现简化源码结构不等于获得整段再发布许可。本次只链接原材料；后续源码研究先确认访问权与公开范围，以行为、调用路径和自有最小实验形成公开证据。

## 6. 未核查范围与证据限制

- 没有全读整个站点、整个 UE/GAS/ET/技能/渲染/服务端系列。GAS-01–07、UE 网络 02、UE 对象反射/GC 的其他文章、客户端 UI/AI/动画的泛专题等多为定位级检查，未判为全篇可直接复用。
- 未核查原文章所有外部链接、源码片段、示例 API、商业案例和数据来源；没有背书其所有技术结论。
- 没有核验 ET 当前公开包、许可、运行环境；没有遍历私有 GameEngineDev；没有复制私仓源码或商业数据。
- 没有证明所有已存在文章均已部署到线上站点；Hugo 构建成功也不证明公开站点更新。
- 未读取所有执行层文档或历史归档正文，不用其勾选计算覆盖率。检索未命中只说明未找到候选，实际写作前仍需按具体问题再次查重。
- 官方在线核查只支持工具链和资料入口等有限事实，详见主计划“版本基线与官方资料”。本机 UE、Linux、Android、数据库、权限、磁盘与构建能力均待确认。
- 个人能力全部待测；本次没有把文件存在标为“已阅读/可独立实现/可独立诊断/外部验收”。

## 7. 本次交付检查

- 修改白名单：本快照、主计划、根 `doc-plan.md`。原专题、历史归档、content 和既有用户修改不在编辑范围。
- 编辑前 Hugo：`hugo v0.157.0+extended`，`hugo` 退出 0，1263 页，未报告 ERROR。首次沙箱执行因权限未启动；经执行权限提升后获得此有效基线。
- 编辑后结构检查：三份文件中的 127 处本地 Markdown 链接均指向存在的文件；代码围栏闭合，未发现冲突标记或行尾空白。仅核存在性，不代表链接目标正文已全部审计。
- 依赖检查：13 个模块均有目标、前置、材料、缺口、实践、故障、通过标准、连接；20 个待写 UFS 单元的两张表一致，前置图无未知节点和依赖循环；首批明确为 9 个单元。全部只是规划单元，没有对应空文章文件。
- 范围检查：根索引仅新增 1 行系列路由；`git diff --name-only -- content` 无输出；3 个用户既有修改文件在本次留存的哈希快照前后保持一致。原系列 plan 与历史归档未编辑，没有新增第二份原专题目录。
- 差异检查：`git diff --check` 通过；两个未跟踪新文档另做空白/冲突/链接检查，无问题。对空文件的 `git diff --no-index --check` 未报告空白错误，退出 1 表示存在新增文件差异，不把它写成退出 0。
- 编辑后 Hugo：执行 `hugo` 退出 0，1263 页、44 个静态文件，与编辑前一致，未报告 ERROR；没有本次新增构建错误。docs 规划不生成站点文章，构建只证明站点未被本次索引编辑破坏，不证明计划中的工程有效。
- 未执行：UE/DS/ASP.NET Core/PostgreSQL 实现与运行、设备/负载/恢复实验、源码验证、线上站点验证、个人能力测评和外部评审。原因：本次范围是文档规划，且环境/权限/运行证据待确认；未 commit/push。
