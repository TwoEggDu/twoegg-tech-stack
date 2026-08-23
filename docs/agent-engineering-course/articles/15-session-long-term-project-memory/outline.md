# Detailed Outline｜Article 15 Session、Long-term Memory 与 Project Memory：事实、经验和作用域

## Outline Contract

- Article: 15
- Gate: OUTLINE
- Type: PRINCIPLE
- Mode: NORMAL_ARTICLE
- Required Lab: NONE
- Teaching path: problem space -> abstract model -> concrete mechanism -> engineering boundaries -> Learning Check -> shortest conclusion
- Claim coverage: 14 / 14
- Status ceiling: CONFIRMED=7 / PARTIAL=1 / PROPOSAL=6 / BLOCKED=0
- Next Gate candidate: AUTHOR_DRAFT

## Teaching Thesis

> COURSE PROPOSAL：记忆系统的工程价值，不是让 Agent “什么都记得”，而是让跨 Session 信息在写入和召回时保留作用域、来源、新鲜度与处置状态；历史记忆只能贡献候选，不能冒充当前现实。

读者变化：

1. 从“模型记错了”推进到定位 write-side promotion 与 read-side eligibility 两类故障。
2. 能按 scope、authority、lifecycle 区分 Context、History、Working Memory、Session、Long-term Memory、Project Memory、Checkpoint 与 Knowledge Base。
3. 先使用稳定课程 Session 定义，再映射 provider conversation / thread / run。
4. 能设计 MemoryWriteCandidate 写路径与 Stored / Retrieved / Eligible / Injected 召回路径。
5. 能判断旧 build 经验何时只应 historical-only，怎样处理 freshness、conflict、delete 与 forget。
6. 知道本文不进入完整 RAG、Evidence Contract 或 Permission 系统。

## Dependency Contract

| Dependency | 只承接 | 不重复 |
|---|---|---|
| Article 11 | Checkpoint 保存恢复所需 committed boundary、known / unknown、in-flight 与 continuation | Retry、Cancellation、Resume、Reconcile、Compensate |
| Article 12 | Context Snapshot 是本 Step 的 application-visible selected view；Receipt 记录选择与来源 | Select / Order / Scope / Fit Budget 与完整 Receipt schema |
| Article 13 | stale、wrong scope、conflict 等可在 application-visible artifact 定位 | Packing taxonomy、Reconstruction Ladder、Lab 05 |
| Article 14 | Working Memory 是 task-scoped、versioned projection；模型只提 candidate，Host 拥有 semantic acceptance | Investigation State schema、两轴 taxonomy、mutation pipeline |

向后只留接口：KB 可与 Project Memory 共用设施，但检索链留给后文；promotion 只保留 Evidence refs 与 authority seam；scope mismatch 只写 privacy / contamination risk，不设计完整 permission / approval / sandbox。

## Failure-driven Teaching Spine

~~~text
Session A：active hypothesis 被 summary / extraction 保存
  -> 坏实现把“已保存”当“已证实”
  -> Project Memory 中出现伪 FACT

Session B：project / build / environment 已变化
  -> 旧记录被召回
  -> 坏实现跳过 scope / freshness / conflict
  -> historical hypothesis 以 current truth 注入 Context
  -> Agent 自信地走向错误动作

修复：
  write  = candidate -> Host promotion decision
  recall = Stored -> Retrieved -> Eligible -> Injected
  lifecycle = update / conflict / invalidate / delete / forget 分账
~~~

这是 COURSE PROPOSAL / FAILURE PATTERN。正文必须称 promotion bug candidate / bad implementation pattern，不得写成已观察到某框架漏洞或真实 BuildPilot 事故。

---

## 0. 开场：Agent 明明“记得”，为什么反而把工程带偏

- Section responsibility：用工程故障结构开场。前一轮调查留下“X 可能是原因”，下一轮却读成“X 已确认”；当前源码、配置、build 或环境已经变化。先展示错误复用、跳过 current evidence 与错误行动，不先列定义或 API。
- Reader question：跨 Session 记住更多信息，为什么可能比忘掉更危险？
- Claims：15-C08 PROPOSAL / EC-08；15-C09 CONFIRMED / EC-09。
- Content duty：拆出 write-side distortion（hypothesis 被错误晋升）与 read-side distortion（historical record 被当 current truth）；给出“memory 是历史贡献者，不是 current reality mirror”。
- Example duty：匿名旧 build 场景，不使用真实性能数值；4310 / 4472 留到 §8。
- Figure 1：四格时间线 hypothesis -> extracted record -> stale recall -> wrong action；每格分别写 observed 与 incorrectly inferred。
- Forbidden：不写“框架会自动把假设写成事实”；不声称真实事故、损失或普遍质量退化。
- Transition：要修复它，先别问用什么数据库；先问哪些不同责任都被叫成了“记忆”。

## 1. 为什么“被记住”仍然可能是错的

- Section responsibility：拆掉 storage、duration、retrieval 自动授予 truth / authority 的粗糙等式。
- Reader question：被保存、存在很久、被搜索到，分别最多证明什么？
- Claims：15-C04 CONFIRMED / EC-04；15-C05 CONFIRMED（Eligible 与四段命名为 COURSE PROPOSAL）/ EC-05；15-C06 PROPOSAL / EC-06。
- Core contrast：

~~~text
persisted        != semantically accepted
stored for long  != cross-session authority
retrieved        != eligible current input
~~~

- Table 1：粗糙等式 / 实际只证明 / 仍缺检查 / 后文落点。
- Example duty：thread-scoped checkpoint 即使持久化 90 天，scope 仍可只是当前 thread；managed write 只证明 representation 被保存。
- Forbidden：不写 short-term=RAM、long-term=database、vector store=long-term truth；不评价 summary 一定可信或一定错误。
- Transition：介质和时长不能切边界，下一步先画逻辑责任图。

## 2. 抽象模型：七个对象，加一条 Knowledge Base 边界

- Section responsibility：全文中心模型；按问题、scope、authority、lifecycle 分层，强调 logical role != physical store。
- Reader question：七对象分别回答什么，KB 为什么不能吞进 Memory？
- Claims：15-C02 PROPOSAL / EC-02；15-C04 CONFIRMED / EC-04；15-C14 PROPOSAL / EC-14。
- Required label：整张矩阵标 COURSE PROPOSAL / NOT INDUSTRY STANDARD。

| Object | 回答的问题 | Scope / authority | 不负责 |
|---|---|---|---|
| Context Snapshot | 本 Step 实际看见什么？ | step-scoped；application assembly | 完整 History、durable truth |
| History | 按时间发生过什么？ | event / message sequence | 自动裁决当前有效 statement |
| Working Memory | 当前未完成任务按什么继续？ | task-scoped versioned projection；Host acceptance | workflow gate、permission、跨任务 truth |
| Session | 哪些交互与执行属于同一可追踪、恢复或回放边界？ | session boundary；mapping-dependent | 单次 request / model call / 固定 provider resource |
| Long-term Memory | 哪些信息跨 Session / thread 复用？ | cross-session / cross-thread policy | 因持久化时长自动成立 |
| Project Memory | 哪些 facts / decisions / experiences 绑定 project？ | project-scoped policy | current repo / build / runtime 镜像 |
| Checkpoint | 中断后从哪一 committed boundary 恢复？ | Runtime / workflow authority | Memory 分类、完整 Context / History |
| Knowledge Base | 哪些带来源边界的组织化知识可检索？ | source / collection lifecycle | 未治理聊天历史、自动 current truth |

- Figure 2：History、Working Memory、Long-term / Project Memory、KB 作为 Context Assembly 候选；Checkpoint恢复控制边界；Session治理连续性。图注：它不是数据库拓扑，物理容器可以重叠。
- Example duty：同一句 build failed 在 History 是事件，在 Working Memory 是 accepted observation ref，在 Project Memory 是旧 build record，在 Context 是本 Step contributor。
- Forbidden：不要求八个独立数据库；不宣称 Project Memory 与 KB 同义或绝对互斥；不进入 embedding / chunking / vector DB。
- Transition：责任图站住后，最容易混乱的是 Session 的跨产品名称。

## 3. 稳定课程 Session 定义，再做产品术语映射

- Section responsibility：课程责任先行，再映射 checked products，阻止 run / thread_id / conversation_id / SDK Session 硬对齐。
- Reader question：课程 Session 与 provider thread / conversation / run 是什么关系？
- Claims：15-C01 CONFIRMED / EC-01；15-C03 CONFIRMED / EC-03。
- Stable definition：COURSE PROPOSAL——Session 是一次可追踪、恢复或回放的交互与执行边界，可以拥有、引用或治理 History；它不是单次 request、单个 model call，也不预设一个 provider resource。与 Articles 12 / 14 保持一致。

| Product object（retrieved 2026-08-23） | Checked responsibility | 不等同 |
|---|---|---|
| OpenAI Agents SDK Session | client-managed conversation history | Conversations resource、Project Memory |
| OpenAI conversation_id | server-managed named conversation | application-visible 全部 Context、跨项目 truth |
| OpenAI previous_response_id | lightweight server-managed continuation | durable Session taxonomy |
| OpenAI Runner.run(...) | logical turn，可含多个 Agent / LLM calls | 单个固定 model request、整个 Session |
| Google ADK Session | user-agent conversation thread，含 Events / State / identity | cross-session MemoryService |
| LangGraph thread_id + checkpointer | thread-scoped state / checkpoints | cross-thread Store、统一 Session 标准 |

- Counter-evidence duty：ADK 明确把 Session 称为 thread，所以结论不是“Session 不能叫 thread”，而是跨生态无统一一一映射。
- Version guard：只写 current hosted docs；不使用旧 Assistants Thread / Run 作为 current contract。
- Forbidden：不写一次 run 只调用一次模型；不写 provider conversation 就是 Long-term / Project Memory。
- Transition：Session 只定义连续性；跨 Session 留下信息还需独立写入资格。

## 4. 写路径：MemoryWriteCandidate 与 Promotion Authority

- Section responsibility：承接 Article 14 typed hypothesis，冻结 candidate、semantic acceptance 与 durable revision 的 authority seam；不是数据库 schema 教程。
- Reader question：何时只是可写候选，谁能把它晋升为 future Session 可复用的 fact / decision / experience？
- Claims：15-C06 PROPOSAL / EC-06；15-C07 PROPOSAL / EC-07；15-C08 PROPOSAL / EC-08。
- Required label：流程、decision vocabulary、envelope 全标 COURSE PROPOSAL / NOT FRAMEWORK CONTRACT。

~~~text
Observation / user statement / Working Memory entry / Session event
  -> MemoryWriteCandidate
  -> validate scope + provenance + claim kind + freshness + policy
  -> PROMOTE | KEEP_CANDIDATE | REJECT | CONFLICT | INVALIDATE
  -> durable memory revision
~~~

- Candidate duty：至少可表达 future reuse value、scope、source / provenance、observed_at / source_version、claim kind、update / conflict identity、promotion authority；未知写 UNKNOWN。
- Table 2：治理问题 / 字段 / 缺失规则：
  - 属于谁：tenant / user / org / project / environment / version range；
  - 从哪里、何时观察：source_ref / provenance / observed_at / source_version；
  - 是什么 claim：FACT / DECISION / EXPERIENCE / PREFERENCE / HYPOTHESIS / UNKNOWN；
  - 证据边界：evidence_refs / confidence / trust；confidence 非校准概率，trust 不等于 truth；
  - 怎样演化：expires_at / invalidated_at / update_of / conflict_set / deletion_state / forgetting_policy；
  - 谁批准：promotion status / authority / rule。
- Promotion bug example：HYPOTHESIS / ACTIVE -> summarizer says confirmed -> extractor writes FACT -> later Session retrieves -> no Host semantic acceptance。只展示 authority 断点，不复制 Article 14 schema。
- Forbidden：不指控 OpenAI sandbox memory / Semantic Kernel 存在漏洞；不说所有 extraction 都错；W3C PROV 只支撑部分 provenance / revision vocabulary；不展开完整 Evidence threshold。
- Transition：写入资格只回答“什么可以留下”；future Step 仍不能因它存在就直接看见它。

## 5. 召回路径：Stored、Retrieved、Eligible、Injected 分账

- Section responsibility：建立 read path 与 trace responsibility；只讲召回后资格，不讲检索、排序或质量评估。
- Reader question：memory 已存储且查询返回，为什么还不能直接进入 Context？
- Claims：15-C05 CONFIRMED（Eligible / 四段命名 COURSE PROPOSAL）/ EC-05；15-C09 CONFIRMED / EC-09；15-C12 PARTIAL / EC-12。

~~~text
Stored Memory
  -> Retrieved Candidates
  -> Eligible Candidates
       scope / provenance / freshness / trust / conflict / purpose
  -> Injected Context Contributors
  -> Context Snapshot + Receipt
~~~

| Stage | 最小可观察事实 | 不自动证明 |
|---|---|---|
| Stored | object exists in store / file / provider resource | 本次会找到、内容正确 |
| Retrieved | query / namespace 返回 candidate | scope 正确、新鲜、可信 |
| Eligible（COURSE PROPOSAL） | Host policy 允许作为本 Step contributor | 必须注入、模型必使用 |
| Injected | application 放入 Context Snapshot | Provider完整使用、输出正确 |

- Product example duty：只用一句话映射 LangGraph put / search / message assembly、ADK add / search、Semantic Kernel extraction / query / context provision、OpenAI sandbox summary + on-demand search，证明动作可分，不写 SDK 教程。
- Fail-closed：scope / freshness / conflict 不明时保留 UNKNOWN / REJECT / HISTORICAL_ONLY（COURSE PROPOSAL）与 reason。
- Figure 3：四阶段 recall ledger，每段保留 candidate ref、revision、eligibility reason 与 Context Receipt ref。
- Forbidden：不讲 query、embedding、filter、rerank、citation、eval；不说 retrieve 后正确或 inject 后必被模型采用。
- Transition：Eligible 的中心不是相似度，而是属于谁、对应哪个版本、与现状是否兼容。

## 6. Scope、Freshness、Conflict：Project Memory 不是 Current Reality

- Section responsibility：给出 recall judgment order；旧记录可保留历史价值，但不能覆盖 current authoritative artifact。
- Reader question：Project Memory 与 current repo / config / build / service 冲突时信谁，能否 newest-wins？
- Claims：15-C09 CONFIRMED / EC-09；15-C12 PARTIAL / EC-12；15-C13 PROPOSAL / EC-13。

~~~text
same tenant / user / org / project / environment?
  -> applicable version / profile / time horizon?
  -> provenance + current authoritative source available?
  -> conflict present?
  -> CURRENTLY_ELIGIBLE | HISTORICAL_ONLY | CONFLICT | INVALIDATE | UNKNOWN
~~~

整套 verdict vocabulary 标 COURSE PROPOSAL。

- Current-reality duty：Project Memory 提供 locator、historical fact、decision rationale、experience candidate；当前源码、配置、build artifact、measurement 或 service response需重新取证。OpenAI sandbox memory事实保留 Beta / retrieved 2026-08-23。
- Scope risk table：只写 official docs 已确认 session / user / app / thread / layout会改变共享；窄化 inference 为错误 tenant / user / project / environment mapping可能让 out-of-scope内容成为 retrieval candidate。
- PARTIAL guard：只写 privacy / contamination risk；不写已发生泄漏、ACL失败、攻击链或合规影响。
- Conflict policy（COURSE PROPOSAL）：不默认 last-write-wins；保留 competing values、scope、source、version、observed_at、conflict_set 与 UNRESOLVED / SUPERSEDED / INVALIDATED / SCOPED_BOTH_VALID。不同 build 的两条记录可能各自在自己的scope有效。
- Figure 4：old memory -> current verification -> disposition -> action；不得 old memory -> action。
- Forbidden：不写 current environment 永远正确；不设计 permission lattice、approval、sandbox、ACL、identity proof；不展开 CAS / transaction。
- Transition：即使当前可用，记录也不会永远有效；lifecycle event必须分开。

## 7. 生命周期：Update、Conflict、Delete、Forget 不是同一动作

- Section responsibility：把 active / deleted 布尔值拆成可审查事件；删除语义按具体 resource / backend 核验。
- Reader question：过期、修订、冲突、删除请求、不再召回，能否都写 deleted？
- Claims：15-C07 PROPOSAL / EC-07；15-C10 CONFIRMED / EC-10；15-C13 PROPOSAL / EC-13。

| Event | 改变什么 | 不自动证明 |
|---|---|---|
| Update / revision | 新版本或替代关系；保留 update_of、source、version | 旧记录物理消失 |
| Conflict | competing records 并存；保留 conflict_set / resolution | 新记录胜出 |
| Invalidate / expire | 某 scope / time 不再有效 | backend 已删除 |
| Delete request / result | 特定 resource / item 的产品定义删除 | backup / log / derived copy全删、合规满足 |
| Forgetting / retention / consolidation | policy 不再保留或主动使用某内容 | 等于 resource deletion |

- Product counter-example duty：OpenAI Conversations delete conversation 不自动 delete items；与 SDK Session clear、ADK delete_session、checkpoint pruning / consolidation 并列，只证明操作不同，不写 API 教程。
- Figure 5（COURSE PROPOSAL）：CANDIDATE -> PROMOTED -> ACTIVE；再分 SUPERSEDED / CONFLICT / INVALIDATED / EXPIRED / DELETE_REQUESTED -> DELETE_RESULT_RECORDED / FORGOTTEN_BY_POLICY；REJECTED 由 candidate分支。
- Forbidden：不声称 all copies erased、privacy / compliance satisfied；不以 delete 偷偷解决 conflict。
- Transition：把写入、召回与生命周期放进 Unity 跨 build 调查。

## 8. 具体落地：BuildPilot / Unity build 4310 到 4472

本节第一行必须原样标：

SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM

- Section responsibility：用 synthetic Unity / build engineering case落地抽象；只展示 scope / freshness / disposition，不证明性能或 Runtime。
- Reader question：旧 build 启动性能经验被新 build 召回后，应怎样使用而不是怎样相信？
- Claims：15-C11 PROPOSAL / EC-11；回扣 C05/EC-05、C06/EC-06、C09/EC-09、C13/EC-13。

~~~text
Session A / build 4310
  MemoryWriteCandidate:
  “WeChat Mini Game startup bottleneck may be associated with X”
  scope=project P / WX / build 4310 / profile WX / observed_at T1
  kind=HYPOTHESIS or EXPERIENCE_CANDIDATE

Session B / build 4472
  Retrieved Candidate
  -> reject direct Current Reality use
  -> verify project + platform + build + profile + source revision
  -> acquire current artifact / measurement
  -> corroborate | update | conflict | invalidate | historical-only
~~~

Example duties：

1. Write：4310 hypothesis不能因 summary / write success成为 FACT。
2. Recall：build mismatch先落 HISTORICAL_ONLY / NEEDS_CURRENT_VERIFICATION（COURSE PROPOSAL）。
3. Verify：只列 current build identity、platform/profile、source revision、artifact/measurement类型，不编造路径、bytes或指标。
4. Conflict：current measurement不一致时保留两份 scoped record；如果只是scope不同，可 SCOPED_BOTH_VALID。
5. Lifecycle：旧记录可以 update、invalidate、historical-only，不为收口直接 delete。

- Figure 6：4310 write path 与 4472 recall path双通道，中间以 memory revision连接；action前必须有 current verification gate。
- Proves only：课程 policy能表达跨 build freshness、historical-only 与 conflict disposition。
- Does not prove：build ID、X瓶颈、修复、回归、性能变化、BuildPilot Runtime、memory pipeline、设备或生产结果真实存在。
- Forbidden：无虚构日志 / 数字 / hash / 截图；不讲如何用向量检索命中 candidate；不写成产品功能说明。
- Transition：案例的工程判断是保留历史价值，同时拒绝历史越过当前证据。

## 9. 坏实现、工程边界与取舍

- Section responsibility：收束为设计评审表，明确最小护栏与相邻系统边界。
- Reader question：最少要保留哪些 seam，Memory 不应吞掉什么？
- Claims：汇总 C02/EC-02、C05/EC-05、C06/EC-06、C09/EC-09、C10/EC-10、C12/EC-12、C14/EC-14。

| Bad implementation | Failure | Minimum guard |
|---|---|---|
| transcript / summary 直接变 durable fact | promotion bug | Candidate + Host decision |
| 用 duration 分类 | task state 获得错误跨任务 authority | scope-first classification |
| retrieved 就 injected | stale / wrong-scope / conflict 污染 | explicit Eligible + Receipt |
| Project Memory 覆盖 current repo / build | 历史冒充现实 | current authoritative verification |
| newest-wins | scoped facts 被抹掉 | conflict_set + resolution |
| 一个 deleted 布尔值 | resource / retention / derived copy语义丢失 | lifecycle分账 |
| Memory 吞掉 KB / RAG | 退化为检索教程 | 只保留 interface boundary |
| scope risk 扩成权限系统 | 越过本文责任 | 只保留risk与policy seam |

Minimum viable governance（全部 COURSE PROPOSAL）：

1. durable record至少表达 scope、source/provenance、observed_at/version、claim kind、lifecycle；
2. 写入先 candidate，再由明确 authority promotion；
3. recall把 Stored / Retrieved / Eligible / Injected分账；
4. current reality重新取证，conflict不静默覆盖；
5. deletion与forgetting按资源合同分别记录。

Tradeoffs：字段可按风险裁剪，但未知显式 UNKNOWN；logical roles可共用 store，但不能取消 scope / authority guard；高复用价值不等于高真实性；本篇无 Runtime Lab，只能评价设计可审查性。

Explicit non-scope：

- 不重写 Article 14 schema / taxonomy / mutation pipeline；
- 不写 embedding、chunking、vector DB、retriever、filter、rerank、cite、retrieval eval；
- 不展开完整 Evidence Contract；
- 不展开 permission、approval、sandbox、ACL、identity proof、security enforcement；
- 不实现 BuildPilot Runtime，不生成后续文章资产。

- Transition：读者若掌握边界，应能在没有具体 API 时判断下面的事故。

## 10. Learning Check

- Section responsibility：验证工程判断，不考名词背诵；答案必须给 evidence status或disposition。
- Questions：
  1. HYPOTHESIS / ACTIVE 被 summarizer写成 confirmed且store成功，现在是durable fact吗？下一步记录什么？
  2. thread checkpoint在数据库保留90天，为何不自动成为Long-term Memory？
  3. Project Memory被search返回，但project相同、build不同、observed_at较旧；当前处于哪一步，还缺什么？
  4. 4310与4472记录不一致，为什么不能 newest-wins？什么决定 conflict / superseded / scoped-both-valid？
  5. delete conversation成功后，能否写“所有memory已删除且满足合规”？
  6. SDK Session、conversation_id、previous_response_id、Runner.run能否互换？映射前先固定什么？
  7. 错误project namespace使另一项目记录成为candidate，本文最多能下什么结论？
  8. Project Memory与KB共用 searchable store，是否证明同义？

Reference-answer duties：

1. 仍是candidate；Host按scope、refs、kind、rule决定 PROMOTE / KEEP / REJECT / CONFLICT / INVALIDATE（COURSE PROPOSAL）。
2. 分类看thread / cross-thread scope与reuse authority，不看duration / medium。
3. 已Retrieved，尚未自动Eligible；需build/profile/source revision/current artifact或measurement。
4. 不同scope可各自有效；保留scope/source/version/observed_at/resolution。
5. 不能；conversation/item删除可分，backup/log/derived memory与合规均未证明。
6. 不能；先固定课程Session连续性责任，再映射history/resource/continuation/logical turn。
7. 只写sharing mechanism与privacy / contamination risk；不写incident、ACL failure、attack或compliance。
8. 不能；Project Memory按project-scoped facts/decisions/experiences治理，KB按organized knowledge/source boundary治理。

Coverage：Q1=C06/C08；Q2=C04；Q3=C05/C09/C11；Q4=C13；Q5=C10；Q6=C01/C03；Q7=C12；Q8=C02/C14。

## 11. Job Competency Mapping

| Competency | 可观察产物 | 不夸大 |
|---|---|---|
| Scope / state modeling | 七对象+KB矩阵 | 非行业taxonomy |
| Authority design | Candidate / promotion seam | 非production policy |
| Reliability | recall ledger / current verification | 无Runtime Lab，不证明提升 |
| Lifecycle reasoning | update/conflict/delete/forget分账 | 不证明物理擦除或合规 |
| Cross-product abstraction | Session product map | checked examples不穷尽生态 |
| Evidence discipline | 14 Claim status ceiling | synthetic case非真实性能证据 |

## 12. 最短结论

只用一句话收口，不新增机制：

> COURSE PROPOSAL：记忆不是当前真相的副本；写入先审晋升，召回再审作用域、新鲜度与冲突。

## Figures / Tables Responsibilities

| ID | Duty | Ceiling / guard |
|---|---|---|
| Figure 1 | 错误记忆跨Session放大 | C08 PROPOSAL + C09 CONFIRMED；非真实事故 |
| Table 1 | 三个危险等式 | C04/C05/C06；只写proves / missing |
| Core matrix | 七对象+KB | C02/C14 PROPOSAL；非数据库图 |
| Figure 2 | logical responsibility graph | physical store可重叠 |
| Product map | Session terminology | C01/C03 CONFIRMED；带retrieved date |
| Table 2 | envelope问题分组 | C07 PROPOSAL；非schema教程 |
| Figure 3 | recall ledger | C05；Eligible标PROPOSAL |
| Scope table / Figure 4 | current verification | C09 + C12 PARTIAL |
| Lifecycle table / Figure 5 | event分账 | C10 CONFIRMED；C07/C13 PROPOSAL |
| Figure 6 | 4310 -> 4472 | C11 PROPOSAL；强制三重synthetic标签 |
| Bad implementation table | 工程收束 | 不新增core claim |

## Claim-to-section Traceability

| Claim | Ceiling | Card | Primary section | Wording guard |
|---|---|---|---|---|
| 15-C01 | CONFIRMED | EC-01 | §3 | checked products无一一映射，不穷尽行业 |
| 15-C02 | PROPOSAL | EC-02 | §2 | COURSE PROPOSAL；role可共享store |
| 15-C03 | CONFIRMED | EC-03 | §3 | current docs；不用旧Assistants contract |
| 15-C04 | CONFIRMED | EC-04 | §1/§2 | durability不改变scope |
| 15-C05 | CONFIRMED | EC-05 | §5 | official facts证明动作可分；Eligible标proposal |
| 15-C06 | PROPOSAL | EC-06 | §4 | Host promotion是课程提案 |
| 15-C07 | PROPOSAL | EC-07 | §4/§7 | W3C只支持部分vocabulary |
| 15-C08 | PROPOSAL | EC-08 | §0/§4 | bad pattern，不指控产品漏洞 |
| 15-C09 | CONFIRMED | EC-09 | §6 | OpenAI事实限Beta；Project Memory为课程mapping |
| 15-C10 | CONFIRMED | EC-10 | §7 | deletion不等于all copies/compliance |
| 15-C11 | PROPOSAL | EC-11 | §8 | synthetic / not executed / no runtime claim |
| 15-C12 | PARTIAL | EC-12 | §6 | 只写risk，无incident / permission结论 |
| 15-C13 | PROPOSAL | EC-13 | §6/§7 | 不默认last-write-wins |
| 15-C14 | PROPOSAL | EC-14 | §2/§9 | 不展开RAG pipeline |

Coverage result：14 / 14；BLOCKED=0；没有新核心 Claim。所有课程综合在正文标 COURSE PROPOSAL，15-C12保持PARTIAL。

## Section Transition Map

~~~text
engineering failure
  -> why remembering can be wrong
  -> seven objects + KB
  -> stable Session + product map
  -> write candidate / promotion
  -> recall ledger
  -> scope / freshness / conflict
  -> deletion / forgetting lifecycle
  -> synthetic 4310 / 4472
  -> boundaries + Learning Check
  -> shortest conclusion
~~~

## Draft Evidence Discipline

1. 产品事实带Evidence Card与retrieved / Beta / experimental范围。
2. 课程定义、policy、verdict vocabulary、synthetic scenario显式标COURSE PROPOSAL或更强synthetic标签。
3. CONFIRMED只确认窄产品事实，不确认课程taxonomy或production outcome。
4. 15-C12保持PARTIAL，只写risk。
5. 4310 -> 4472在正文、图注、Learning Check均保留 SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM。
6. Draft若需要新核心事实，返回Research，不直接补入。

## Outline Gate Checklist

- [x] 第一屏为工程失败，不是定义 / API。
- [x] 问题空间 -> 抽象模型 -> 具体机制 -> 工程边界 -> Learning Check -> 最短结论。
- [x] 七对象、KB边界、稳定Session与product map齐全。
- [x] Write candidate / promotion和四段recall path齐全。
- [x] Scope、freshness、conflict、update、delete、forget分开。
- [x] 4310 -> 4472保持synthetic ceiling。
- [x] 每节有责任、读者问题、Evidence refs、例子/图表职责、禁区和transition。
- [x] Job Competency与显式non-scope齐全。
- [x] Claim coverage 14 / 14；status ceiling未升级。
- [x] 无新未研究核心主张。
