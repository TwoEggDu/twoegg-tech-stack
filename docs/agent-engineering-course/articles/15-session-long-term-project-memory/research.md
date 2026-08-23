# Research｜Article 15 Session、Long-term Memory 与 Project Memory

## Status

`RESEARCH_COMPLETE / EVIDENCE_GATE_RECOMMENDATION_PASS`

- Article: `15`
- Mode: `NORMAL_ARTICLE`
- Retrieved at: `2026-08-23`（Asia/Shanghai）
- Required Lab: `NONE`
- Core Claim count: `14`
- Core `BLOCKED`: `0`
- Source policy: current official / primary sources only；产品事实与课程抽象分层记录。

## Research Boundary

本篇是原理 / 架构边界篇，研究顺序遵循“问题空间 -> 抽象模型 -> 工程落点”，不按框架 API 展开。研究对象是跨 Run、跨 Session、跨任务保存信息时的 scope、authority、freshness 与 lifecycle，不是向量数据库或 RAG 教程。

冻结边界：

- Article 14 已建立 task-scoped、versioned Working Memory / Investigation State；本篇不重写其 taxonomy、schema 或 mutation pipeline。
- Article 16 才展开 Knowledge Base / RAG 的 Retrieve、Filter、Rerank、Inject、Cite；本篇只说明 Memory 与 KB 的职责分界，并用最小检索事实证明 `Stored != Retrieved != Injected`。
- Article 19 才展开 Permission、Approval、Sandbox；本篇只指出 memory scope mismatch 与敏感数据扩散风险，不设计完整授权系统。
- BuildPilot / Unity build `4310 -> 4472` 仅为 synthetic illustrative ceiling；没有真实 BuildPilot Runtime、真实 build artifact、设备观测或生产结论。
- Article 16 与 Part III Audit 不在本次 execution scope。

## Research Questions

| RQ | Question | Answer | Status |
|---|---|---|---|
| 15-RQ01 | 八个对象分别回答什么？ | Context回答本Step看什么；History回答按时间发生什么；Working Memory回答当前任务按什么继续；Session回答哪些交互与执行属于同一边界；Long-term Memory回答哪些信息跨Session复用；Project Memory回答哪些记忆绑定项目；Checkpoint回答中断后从哪里恢复；KB回答哪些带来源知识可被检索。物理存储可重叠，authority与lifecycle不应混同。 | ANSWERED |
| 15-RQ02 | 课程 Session 与 provider thread / conversation / run 是什么关系？ | 课程 Session 是可追踪、恢复或回放的交互与执行边界，具体产品对象只作实现映射。OpenAI Agents SDK `Session` 是 client-managed history；OpenAI `conversation_id` 是 server-managed resource；`previous_response_id` 是轻量 continuation；一次 `Runner.run(...)` 是 logical turn，内部可含多个 Agent / LLM call。 | ANSWERED |
| 15-RQ03 | 什么时候信息可以成为 memory write candidate？ | 当信息具有明确 scope、source / provenance、version或observed_at、可更新identity，并对未来任务有复用价值时才成为candidate；candidate仍不是durable fact。promotion authority与Evidence threshold是课程提案。 | ANSWERED |
| 15-RQ04 | Stored、Retrieved 与 Injected 为什么分开？ | 官方实现中写入store、按query / namespace取回、把结果加入model context是不同调用或阶段。存着不代表本次找到；找到不代表通过scope / trust / freshness检查；通过检查也不代表必然装入本Step。 | ANSWERED |
| 15-RQ05 | durable memory 最少需要哪些治理维度？ | 课程提出 `scope / source / provenance / observed_at / version / confidence / trust / expires_at / invalidated_at / update_of / conflict_set / deletion_state / forgetting_policy`。W3C PROV支持provenance、revision、derivation与invalidation，但完整集合不是标准或框架合同。 | ANSWERED |
| 15-RQ06 | Working Memory hypothesis 如何错误晋升？ | 把transcript、session state、model summary或extraction output的“已保存”误当“已证实”，会使hypothesis跨Session污染。官方文档证明extraction / consolidation / state commit存在，却不承诺semantic truth；promotion必须是独立的Host-owned decision。 | ANSWERED |
| 15-RQ07 | Project Memory 为什么不能冒充 Current Reality？ | OpenAI sandbox memory当前文档明确提示memory可能stale，应把它当guidance并信任current environment。项目记忆只能提供历史事实、决策或经验候选；当前源码、配置、build、环境或服务状态仍需重验。 | ANSWERED |
| 15-RQ08 | 更新、冲突、删除与遗忘是否一个动作？ | 不是。更新可产生revision；冲突可并存；删除针对特定resource / item；遗忘可以是retention / consolidation policy。OpenAI Conversations删除conversation不自动删除items，说明delete semantics必须按产品核验。 | ANSWERED |
| 15-RQ09 | Session persistence、Checkpoint 与 Long-term Memory 如何切开？ | 保存时长不决定类别。Checkpoint保存恢复所需control boundary；Session管理某一交互 / 执行边界及history；Long-term Memory按跨Session / thread scope复用。LangGraph明确区分thread-scoped checkpointer与cross-thread store。 | ANSWERED |
| 15-RQ10 | BuildPilot 4310 / 4472 案例的证据上限？ | 只能构造“旧build 4310的WeChat Mini Game启动瓶颈记忆被build 4472调查召回后，先核对build scope、observed_at、source、profile与current measurement”的教学链路；不得声称两个build、瓶颈、修复或回归真实存在。 | ANSWERED |
| 15-RQ11 | scope mismatch 的隐私风险如何表述？ | 只陈述最小边界：跨user / app / project / agent / thread / environment的错误namespace或共享layout可能让不该出现的memory成为retrieval candidate。读取、写入、删除与跨域共享授权留给Article 19。 | ANSWERED |
| 15-RQ12 | KB 与 Project Memory 的边界？ | Project Memory是项目作用域的事实、决策和经验记忆；KB是经过组织、可检索且带来源边界的知识集合。二者可共享设施，但scope、write authority、freshness与lifecycle不因此相同；检索算法与citation pipeline留给Article 16。 | ANSWERED |

## Claim Inventory

| Claim ID | Core claim | Status | Wording boundary |
|---|---|---|---|
| 15-C01 | `Session`、provider conversation / thread 与 run 没有跨生态统一的一一映射；课程先按责任定义，再做产品映射。 | `CONFIRMED` | 只覆盖已检查的OpenAI Agents SDK、OpenAI Responses / Conversations与Google ADK，不穷尽行业。 |
| 15-C02 | Context、History、Working Memory、Session、Long-term Memory、Project Memory、Checkpoint与KB应按scope、authority、lifecycle分层。 | `PROPOSAL` | 课程综合模型，不是任何框架官方taxonomy；物理容器允许重叠。 |
| 15-C03 | OpenAI Agents SDK `Session`是client-managed history；`conversation_id` / `previous_response_id`是OpenAI-managed continuation；一次SDK run是logical turn。 | `CONFIRMED` | current hosted SDK docs scope；不外推到旧Assistants Thread / Run。 |
| 15-C04 | durable session / checkpoint与long-term memory的分类首先取决于thread / cross-thread scope，而非保存多久。 | `CONFIRMED` | LangGraph checkpointer / store是产品例证；课程分类仍按glossary。 |
| 15-C05 | `Stored -> Retrieved -> Eligible -> Injected` 是应单独留痕的动作；前一动作成立不自动证明后一动作。 | `CONFIRMED` | 官方实现证明Stored / Retrieved / Injected可分；`Eligible`与四段式是课程抽象。 |
| 15-C06 | Memory Write Candidate只有通过Host-owned promotion policy后才可成为durable fact / decision / experience。 | `PROPOSAL` | framework write、extraction或consolidation不是semantic truth judge。 |
| 15-C07 | durable memory envelope应携带scope、source、provenance、timestamp / observed_at、version、confidence、trust、expiry、invalidation、update、conflict、deletion与forgetting信息。 | `PROPOSAL` | 课程schema candidate；W3C PROV只支撑其中部分概念。 |
| 15-C08 | 把Working Memory的active hypothesis因“被保存 / 摘要 / 提取”直接晋升为durable fact，是promotion bug。 | `PROPOSAL` | 由Article 14 authority boundary与当前ingestion facts推导；未执行Lab。 |
| 15-C09 | Project Memory必须作为historical guidance / candidate；涉及Current Reality时重新核验scope、freshness与authoritative source。 | `CONFIRMED` | OpenAI sandbox memory的stale guidance是current beta事实；Project Memory名称是课程映射。 |
| 15-C10 | update、conflict、delete与forget是不同lifecycle event；产品删除语义必须逐项核验。 | `CONFIRMED` | Conversation delete不自动删除items；recency forgetting不代表通用合规删除。 |
| 15-C11 | BuildPilot `4310 -> 4472` 只可作为synthetic recall / verification场景。 | `PROPOSAL` | build IDs、瓶颈、修复、性能变化与production result全未观测。 |
| 15-C12 | namespace / layout的scope选择决定哪些memory可共享；错误映射存在privacy / contamination risk。 | `PARTIAL` | 官方docs确认scope与共享行为；泄漏事故未实验，正文只写risk。 |
| 15-C13 | Memory conflict不应默认last-write-wins；至少保留competing values、scope、version、source与resolution state。 | `PROPOSAL` | W3C revision / invalidation不规定本课程conflict policy。 |
| 15-C14 | Project Memory与Knowledge Base可以交叉，但KB的retrieve / filter / rerank / inject / cite pipeline由Article 16负责。 | `PROPOSAL` | 本篇不讲embedding、chunking、vector DB、retriever或eval。 |

Coverage：`14 / 14`；`CONFIRMED=7`，`PARTIAL=1`，`PROPOSAL=6`，`BLOCKED=0`。

## Terminology Comparison

| Object / term | Checked role and scope | Do not equate with |
|---|---|---|
| Context Snapshot（课程） | 某个Step被应用选择并物化给模型的可见package | Session store、History、Checkpoint、全部Project Memory |
| History（课程） | message / event / transition的时间序列 | 当前仍有效事实、accepted projection |
| Working Memory（课程） | 当前未完成任务的task-scoped、versioned projection | transcript、cross-session durable fact store |
| Session（课程） | 一次可追踪、恢复或回放的交互与执行边界，可拥有 / 引用 / 治理history | 单次request、单个model call、固定provider resource |
| OpenAI Agents SDK `Session` | client-managed conversation history；backend可替换 | OpenAI Conversations resource；sandbox Agent memory |
| OpenAI `conversation_id` | server-managed named Conversations API resource | SDK Session；Long-term / Project Memory truth store |
| OpenAI `previous_response_id` | 不创建conversation resource的轻量server-managed continuation | durable Session taxonomy；跨任务memory |
| OpenAI `Runner.run(...)` | 一个logical turn；内部可含多个agent / LLM calls | 整个Session；一个固定model request |
| Google ADK `Session` | 一个user-agent conversation thread，含Events、State与identity | cross-session MemoryService |
| LangGraph checkpointer | thread-scoped state / checkpoints，用于continuity与recovery | cross-thread Store |
| LangGraph Store | namespace下的application-defined cross-thread data | 自动truth authority；自动注入的完整RAG pipeline |
| Long-term Memory（课程） | 跨Session保留、检索和更新的信息 | 仅因存进数据库而成立的task state |
| Project Memory（课程） | 绑定project scope的事实、决策、经验候选 | 当前repo / build / runtime的权威镜像 |
| Checkpoint（课程） | committed position、identity、known / unknown、in-flight、budget与continuation boundary | Memory分类、完整History、当前Context |
| Knowledge Base（课程） | 经过组织、可检索且带来源边界的知识集合 | 未治理聊天历史；自动current truth |

## Minimum Course Model

### Read path

```text
Stored Memory
  -> Retrieved Candidates
  -> scope / provenance / freshness / trust / conflict eligibility
  -> Injected Context Contributors
  -> model output
```

- `Stored`：对象存在于store / files / provider resource。
- `Retrieved`：当前query / namespace / index返回候选。
- `Eligible`：Host policy判断scope、freshness、trust、conflict与用途允许进入本Step；`COURSE PROPOSAL`。
- `Injected`：候选真正进入application-visible Context Snapshot，并由Article 12 / 13的Receipt留痕。

### Write path

```text
Observation / user statement / Working Memory entry / Session event
  -> MemoryWriteCandidate
  -> validate scope + provenance + claim kind + freshness + policy
  -> PROMOTE | KEEP_CANDIDATE | REJECT | CONFLICT | INVALIDATE
  -> durable memory revision
```

`MemoryWriteCandidate`、promotion authority与decision vocabulary均为`COURSE PROPOSAL`。Model、extractor、summarizer或framework reducer可以产生candidate / representation，但不能仅凭写入成功自证语义为真。

### Candidate envelope（COURSE PROPOSAL）

```yaml
schema: memory-write-candidate-course-v1
candidate_id: string
kind: FACT | DECISION | EXPERIENCE | PREFERENCE | HYPOTHESIS | UNKNOWN
statement: string
scope:
  tenant: string|UNKNOWN
  user: string|NONE
  organization: string|NONE
  project: string|NONE
  environment: string|NONE
  version_range: string|UNKNOWN
source:
  source_ref: string
  provenance: string
  observed_at: datetime|UNKNOWN
  source_version: string|UNKNOWN
epistemic:
  confidence: string|UNKNOWN
  trust: TRUSTED|UNTRUSTED|MIXED|UNKNOWN
  evidence_refs: [string]
lifecycle:
  expires_at: datetime|NONE|UNKNOWN
  invalidated_at: datetime|NONE
  update_of: string|NONE
  conflict_set: [string]
  deletion_state: ACTIVE|DELETE_REQUESTED|DELETED|UNKNOWN
  forgetting_policy: string|UNKNOWN
promotion:
  status: CANDIDATE|PROMOTED|REJECTED|CONFLICT|INVALIDATED
  authority: string|UNKNOWN
  rule: string|UNKNOWN
```

`confidence`不等于校准概率；无可解释度量时应为描述或`UNKNOWN`。`trust`不等于truth。`expires_at`不等于已经invalidated；`deleted`不等于底层所有副本 / item已删除；`forgetting`也不等于合规删除。

## Source Register

| ID | Official / primary source | Retrieved | Version / product scope | Used for |
|---|---|---|---|---|
| S01 | [OpenAI Agents SDK — Running agents](https://openai.github.io/openai-agents-python/running_agents/) | 2026-08-23 | current hosted Python SDK docs；未锁package | client-managed Session、server-managed conversation / previous response、logical run / turn |
| S02 | [OpenAI Agents SDK — Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/) | 2026-08-23 | current hosted Python SDK API ref | Session history；get / add / pop / clear surface |
| S03 | [OpenAI Responses API — Create response](https://developers.openai.com/api/reference/cli/resources/responses/methods/create) | 2026-08-23 | current Responses API | conversation items prepend与automatic add；continuation modes |
| S04 | [OpenAI Conversations — Create](https://developers.openai.com/api/reference/python/resources/conversations/methods/create), [Delete conversation](https://developers.openai.com/api/reference/typescript/resources/conversations/methods/delete), [Delete item](https://developers.openai.com/api/reference/ruby/resources/conversations/subresources/items/methods/delete) | 2026-08-23 | current REST API，language pages仅示例 | named resource；resource / item delete semantics分离 |
| S05 | [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/) | 2026-08-23 | current hosted docs；**Beta** | memory vs Session；workspace files；stale guidance；live update；forgetting；layout isolation |
| S06 | [Google ADK — Session](https://adk.dev/sessions/session/) | 2026-08-23 | current hosted docs；page列Python 0.1.0、TS 0.2.0、Go / Java / Kotlin 0.1.0支持基线 | Session thread；Events / State / identity；delete_session |
| S07 | [Google ADK — State](https://adk.dev/sessions/state/) | 2026-08-23 | current hosted docs；rolling details | session / user / app / temp scopes与persistence |
| S08 | [Google ADK — Memory](https://adk.dev/sessions/memory/) | 2026-08-23 | current hosted docs；rolling services | Session / State vs long-term Memory；explicit add；search entries metadata |
| S09 | [LangGraph — Memory overview](https://docs.langchain.com/oss/python/concepts/memory) | 2026-08-23 | current hosted Python docs；未锁package | thread-scoped vs namespace-scoped cross-session memory；update questions |
| S10 | [LangGraph — Persistence](https://docs.langchain.com/oss/python/langgraph/persistence) | 2026-08-23 | current hosted Python docs | checkpointer vs Store；single-thread vs cross-thread；retention / pruning |
| S11 | [Microsoft Semantic Kernel — Using memory with Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-memory) | 2026-08-23 | last updated 2025-06-09；**experimental** | extraction、query、context provision；Application / Agent / Thread / User scopes；clear |
| S12 | [W3C Recommendation — PROV-O](https://www.w3.org/TR/prov-o/) | 2026-08-23 | stable W3C Recommendation (2013) | provenance；derivation、revision、generation、invalidation；不作truth judge |

## Counter-evidence and Limitations

1. ADK把Session称为conversation thread；OpenAI Agents SDK却把Session、Conversation API与previous response分成不同strategy。课程术语不得伪装成provider universal class。
2. ADK可把Session / Events加入memory；LangGraph可`put` JSON；OpenAI sandbox memory由模型extraction / consolidation。没有来源承诺这些写入天然正确、无冲突或永不过期。
3. ADK MemoryService与LangGraph Store有search surface，但retrieve ranking、filter、rerank、chunk、embedding、citation quality属于Article 16。
4. OpenAI SDK Session有`clear_session`；ADK有`delete_session`；OpenAI Conversation delete不删除items；Semantic Kernel experimental provider有clear surface。不能用“删除memory”概括所有对象。
5. OpenAI sandbox recency consolidation移除旧raw memory candidate是内容选择 / retention，不证明物理擦除、法规满足或所有副本消失。
6. W3C PROV不定义confidence calibration、trust、TTL、privacy、promotion authority或conflict resolution。
7. OpenAI sandbox memory是Beta，Semantic Kernel agent memory是experimental；只能作为current examples。
8. Project Memory不是统一产品对象。OpenAI workspace memory与custom namespace只提供映射先例。
9. 官方资料确认scope控制共享；本研究没有执行越权读取实验，也不提前设计Article 19权限闭环。
10. build `4310`与`4472`是synthetic labels，不得产生任何真实性能结论。

## BuildPilot / Unity Illustrative Ceiling

允许表达的链路：

```text
Session A / build 4310
  -> MemoryWriteCandidate:
     “WeChat Mini Game startup bottleneck may be associated with X”
     scope=project P / build 4310 / profile WX / observed_at=T1
     kind=HYPOTHESIS or EXPERIENCE_CANDIDATE

Session B / build 4472
  -> retrieve old candidate
  -> reject direct use as Current Reality
  -> verify project + platform + build + profile + source revision
  -> acquire current measurement / artifact
  -> corroborate, update, conflict, invalidate, or keep historical-only
```

证据上限：`X`、T1、两个build及任何性能结果都没有真实artifact；不能声称BuildPilot存在或执行过验证。

## Article Boundary Notes

- **Article 14**：承接typed hypothesis / accepted view / Evidence refs与revision；不复制Investigation State schema。本篇只处理何时生成MemoryWriteCandidate与怎样跨Session治理。
- **Article 16**：只说memory / KB可被search、结果仍需selection并进入Context Receipt；不写embedding、chunking、vector store教程、retriever、reranker、citation pipeline或retrieval eval。
- **Article 18**：promotion需要Evidence threshold，但完整Evidence Contract尚未展开；本篇只保留refs、claim kind与authority seam。
- **Article 19**：只指出tenant / user / org / project / environment / thread scope mismatch风险；不设计approval UI、permission lattice、sandbox enforcement或access-control实现。

## Evidence Gate Recommendation

`PASS`

- 14个核心Claim已分类，`BLOCKED=0`。
- 7个产品 / 框架事实由current official docs确认；1个隐私风险Claim收窄为`PARTIAL`；6个课程综合显式标`PROPOSAL`。
- 每个核心Claim在`evidence.md`有Evidence Card，含proves / does-not-prove / limitations / version scope。
- BuildPilot案例已限制为synthetic；Article 14 / 16 / 19边界清晰；没有RAG教程或未来Article资产。

建议下一Gate：`EVIDENCE_GATE -> OUTLINE`，由Master验证后推进；Researcher不直接更新全局状态。
