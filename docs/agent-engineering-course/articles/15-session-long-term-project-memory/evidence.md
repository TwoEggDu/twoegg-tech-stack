# Evidence｜Article 15 Session、Long-term Memory 与 Project Memory

## Status

`EVIDENCE_READY_CANDIDATE / RESEARCHER_RECOMMENDS_PASS`

- Evaluated at: `2026-08-23`（Asia/Shanghai）
- Core Claims: `14 / 14` covered
- `CONFIRMED=7`
- `PARTIAL=1`（已收窄）
- `PROPOSAL=6`
- `BLOCKED=0`
- Required Lab: `NONE`

本文件只记录Evidence Gate材料与recommendation；Master是Gate与durable state的唯一owner。

## Evidence Discipline

- `CONFIRMED`：current official / primary source直接支持窄化后的产品或框架事实。
- `PARTIAL`：来源支持一部分链路；正文必须保留未观测部分，不得升级。
- `PROPOSAL`：课程综合模型、schema、policy或synthetic scenario；不冒充行业标准或产品合同。
- `BLOCKED`：核心行为缺证据且无法安全收窄；本次为`0`。
- OpenAI sandbox Agent memory是**Beta**；Semantic Kernel Agent Memory是**experimental**；所有未锁package版本的hosted docs均按`retrieved_at=2026-08-23`限定。

## Claim Register

| Claim ID | Claim summary | Status | Evidence cards | Draft boundary |
|---|---|---|---|---|
| 15-C01 | Session、thread / conversation、run没有跨生态一一映射 | `CONFIRMED` | EC-01 | 仅做checked-product mapping，不声称统一taxonomy |
| 15-C02 | 八对象按scope / authority / lifecycle分层 | `PROPOSAL` | EC-02 | 标`COURSE PROPOSAL`；logical role可共享physical store |
| 15-C03 | OpenAI SDK Session、Conversation / previous response与run分别站在history、continuation、logical turn层 | `CONFIRMED` | EC-03 | 不引用旧Assistants Thread / Run作为current contract |
| 15-C04 | durable thread checkpoint不因保存很久自动成为long-term memory | `CONFIRMED` | EC-04 | 使用LangGraph为产品例证，不外推统一实现 |
| 15-C05 | Stored、Retrieved、Eligible、Injected必须分账 | `CONFIRMED` | EC-05 | 三个产品事实确认；Eligible与四段式为课程命名 |
| 15-C06 | MemoryWriteCandidate需Host-owned promotion后才成为durable fact / decision / experience | `PROPOSAL` | EC-06 | extraction / write success不等于semantic acceptance |
| 15-C07 | memory governance envelope覆盖scope到forgetting全生命周期字段 | `PROPOSAL` | EC-07 | W3C PROV只证明部分vocabulary，不证明完整schema |
| 15-C08 | Working Memory hypothesis直接写成durable fact是promotion bug | `PROPOSAL` | EC-08 | synthetic failure pattern，Required Lab NONE |
| 15-C09 | Project Memory是historical guidance；Current Reality需重新核验 | `CONFIRMED` | EC-09 | Project Memory名称为课程映射；OpenAI事实限Beta |
| 15-C10 | update / conflict / delete / forget不是同一event | `CONFIRMED` | EC-10 | deletion不自动等于all copies erased / compliance satisfied |
| 15-C11 | BuildPilot 4310 -> 4472只作synthetic verification illustration | `PROPOSAL` | EC-11 | 不得声称build、瓶颈、修复或回归存在 |
| 15-C12 | memory scope / layout影响共享，错配存在privacy / contamination risk | `PARTIAL` | EC-12 | 只写risk；无越权实验、无完整permission design |
| 15-C13 | conflict不默认last-write-wins，应保留来源、revision与resolution state | `PROPOSAL` | EC-13 | W3C不规定resolution policy |
| 15-C14 | Project Memory与KB可交叉，但完整RAG pipeline留给Article 16 | `PROPOSAL` | EC-14 | 不写embedding / chunking / retrieval tutorial |

## Evidence Cards

### EC-01｜术语跨产品不一一映射

- **Claim**: `15-C01`
- **Status**: `CONFIRMED`
- **Sources**:
  - [OpenAI Agents SDK — Running agents](https://openai.github.io/openai-agents-python/running_agents/)（retrieved 2026-08-23；current hosted docs）
  - [Google ADK — Session](https://adk.dev/sessions/session/)（retrieved 2026-08-23；current hosted docs）
  - [LangGraph — Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)（retrieved 2026-08-23；current hosted docs）
- **Observation**: OpenAI SDK把`session`、`conversation_id`、`previous_response_id`列为不同state strategy；ADK把Session定义为single conversation thread；LangGraph用`thread_id`定位checkpointer state，并以Store表达cross-thread data。
- **Proves**: 相似术语在checked products中承担不同资源 / abstraction职责，文章必须先定义课程责任再映射。
- **Does not prove**: 行业只有这三套术语；所有provider thread都等于ADK Session或LangGraph thread；旧Assistants API仍是current recommended surface。
- **Counter-evidence / limitation**: ADK明确把Session叫thread，说明“Session绝不能叫thread”也错误。正确结论是无统一一一映射，而不是禁止产品自己的命名。
- **Version scope**: hosted docs未锁全部package；只陈述retrieval date当日可见合同。

### EC-02｜Context到Knowledge Base的八对象模型

- **Claim**: `15-C02`
- **Status**: `PROPOSAL`
- **Sources**:
  - repository glossary与published Articles 11-14（课程authority）
  - [LangGraph — Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)
  - [Google ADK — Conversational memory](https://adk.dev/sessions/memory/)
- **Observation**: checked products分别区分current thread state、events / history、cross-session memory与store；课程已有Context Snapshot、Working Memory与Checkpoint工作定义。
- **Proves**: 多个不同职责在真实实现中确实存在，分层具有工程依据。
- **Does not prove**: `Context / History / Working Memory / Session / Long-term / Project / Checkpoint / KB`是行业标准八分法；每层必须独立数据库。
- **Proposal content**: 八对象的完整职责表、authority / lifecycle切分与`logical role != physical store`。
- **Limitations**: Project Memory是课程工作定义；KB与Memory在ADK等产品中可能以同一searchable service呈现。

### EC-03｜OpenAI Session、Conversation / previous response 与 Run

- **Claim**: `15-C03`
- **Status**: `CONFIRMED`
- **Sources**:
  - [OpenAI Agents SDK — Running agents](https://openai.github.io/openai-agents-python/running_agents/)
  - [OpenAI Agents SDK — Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/)
  - [OpenAI Responses API — Create response](https://developers.openai.com/api/reference/cli/resources/responses/methods/create)
  - [OpenAI Conversations API — Create conversation](https://developers.openai.com/api/reference/python/resources/conversations/methods/create)
- **Observation**: SDK docs将`result.to_input_list()` / `session`归为client-managed，将`conversation_id` / `previous_response_id`归为OpenAI-managed；同一次`Runner.run`是logical turn且可包含多个Agent / LLM call。Session protocol保存特定session的conversation history。Responses API把Conversation items前置到request，并在完成后自动加入input/output items。
- **Proves**: Session history abstraction、server-managed named conversation、lightweight response continuation与logical run / turn是不同层。
- **Does not prove**: 一个SDK run只会发一次model request；Conversation本身是Long-term / Project Memory；provider-managed state等于application-visible全部Context。
- **Counter-evidence / limitation**: SDK明确提醒同一次call不要混用client-managed Session与server-managed conversation settings；这进一步反驳“它们只是同一ID的别名”。
- **Version scope**: current hosted OpenAI docs；不使用已404的旧Assistants deep-dive作为current证据。

### EC-04｜Durability不改变Scope

- **Claim**: `15-C04`
- **Status**: `CONFIRMED`
- **Sources**:
  - [LangGraph — Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)
  - [LangGraph — Memory overview](https://docs.langchain.com/oss/python/concepts/memory)
  - [Google ADK — State](https://adk.dev/sessions/state/)
- **Observation**: LangGraph明确把checkpointer归为single-thread short-term memory、Store归为cross-thread long-term memory；ADK说明Session State是否跨进程持久取决于SessionService，同时用prefix区分session / user / app / temp scope。
- **Proves**: physical persistence与logical scope是两个维度；thread state即便落数据库仍可保持thread-scoped。
- **Does not prove**: short-term必在RAM；long-term必须是vector store；任何保存90天的数据自动拥有跨任务authority。
- **Limitations**: “Long-term Memory”具体内容与生命周期依产品；课程按scope优先分类。

### EC-05｜Stored、Retrieved、Eligible、Injected分账

- **Claim**: `15-C05`
- **Status**: `CONFIRMED`
- **Sources**:
  - [LangGraph — Add memory](https://docs.langchain.com/oss/python/langgraph/add-memory)
  - [Google ADK — Memory](https://adk.dev/sessions/memory/)
  - [Microsoft Semantic Kernel — Using memory with Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-memory)
  - [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)
- **Observation**: LangGraph示例分别执行`store.put`、`store.search`并把返回值组成system message；ADK分别提供`add_session_to_memory` / `add_events_to_memory`与`search_memory`；Semantic Kernel experimental provider从messages提取memory、每次invocation查询并把结果加入context；OpenAI sandbox memory先注入summary，再按需搜索索引与打开rollout summary。
- **Proves**: 写入、查询、提供给model context在多个official implementations中是可分离动作。
- **Does not prove**: 每个产品都实现统一四阶段pipeline；检索结果正确、可信、新鲜；注入后模型必然使用；retrieval quality已被评估。
- **Course abstraction**: `Eligible`是Host在Retrieved与Injected之间做scope / freshness / trust / conflict检查的proposal。
- **Article 16 boundary**: 不展开query、embedding、filter、rerank、citation或eval。

### EC-06｜MemoryWriteCandidate与Promotion Authority

- **Claim**: `15-C06`
- **Status**: `PROPOSAL`
- **Sources**:
  - [Google ADK — Memory](https://adk.dev/sessions/memory/)
  - [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)
  - [LangGraph — Memory overview](https://docs.langchain.com/oss/python/concepts/memory)
- **Observation**: ADK要求application显式把session / events加入memory；OpenAI sandbox memory由conversation extraction与layout consolidation生成文件；LangGraph允许hot-path或background update并提示large profile update可能error-prone。
- **Proves**: ingestion / extraction / consolidation是实现选择，且写入内容可能由model或application logic生成。
- **Does not prove**: 任一写入已通过Evidence验证；model summary是fact；framework提供统一promotion authority。
- **Proposal**: `MemoryWriteCandidate -> validate -> PROMOTE / KEEP_CANDIDATE / REJECT / CONFLICT / INVALIDATE`，promotion由Host policy拥有。
- **Limitation**: Article 18才建立完整Evidence Contract；本篇只保存authority seam与refs。

### EC-07｜Memory Governance Envelope

- **Claim**: `15-C07`
- **Status**: `PROPOSAL`
- **Sources**:
  - [W3C PROV-O](https://www.w3.org/TR/prov-o/)（stable Recommendation；retrieved 2026-08-23）
  - [LangGraph — Persistence / Store item](https://docs.langchain.com/oss/python/langgraph/persistence)
  - [Google ADK — Memory](https://adk.dev/sessions/memory/)
- **Observation**: PROV-O提供Entity / Activity / Agent、derivation、revision、generation、invalidation与time表达；LangGraph Store item示例含namespace、key、created_at、updated_at；ADK MemoryEntry可含id、author、timestamp、custom metadata。
- **Proves**: provenance、scope identifier、timestamp、revision / invalidation等治理信息有primary / official先例。
- **Does not prove**: 完整course envelope是W3C / LangGraph / ADK schema；confidence是校准概率；trust等于truth；TTL、deletion与forgetting语义统一。
- **Proposal fields**: scope、source、provenance、observed_at、source_version、confidence、trust、expires_at、invalidated_at、update_of、conflict_set、deletion_state、forgetting_policy、promotion authority。
- **Limitation**: 字段可按风险裁剪，但缺失必须显式`UNKNOWN`，不能制造伪精确性。

### EC-08｜Working Memory Hypothesis Promotion Bug

- **Claim**: `15-C08`
- **Status**: `PROPOSAL`
- **Sources**:
  - published Article 14：Working Memory model只提交typed hypothesis / candidate，Host才拥有semantic acceptance
  - [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)
  - [Microsoft Semantic Kernel — Using memory with Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-memory)
- **Observation**: OpenAI current Beta实现用memory-generating model执行extraction / consolidation；Semantic Kernel experimental provider从每条message提取memories并在后续invocation使用。两者均未承诺抽取内容是已证事实。
- **Proves**: 未经独立semantic acceptance的model-derived内容确实可能被持久化并影响future run；因此promotion boundary是实际工程问题。
- **Does not prove**: 两个产品存在已知promotion漏洞；所有extracted memory都错误；课程policy已运行。
- **Synthetic failure**: `HYPOTHESIS/ACTIVE -> summary says confirmed -> durable FACT -> later session injects it as truth`。
- **Required wording**: 必须称`promotion bug candidate / bad implementation pattern`，不能称“框架会自动把假设写成事实”。

### EC-09｜Project Memory不是Current Reality

- **Claim**: `15-C09`
- **Status**: `CONFIRMED`
- **Source**: [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)（retrieved 2026-08-23；Beta）
- **Observation**: docs明确说明memory可能stale，agent应把memory当guidance并信任current environment；live update可在发现stale memory时更新`MEMORY.md`。
- **Proves**: 在该current OpenAI产品面中，memory不应覆盖current environment；staleness与update是first-class concern。
- **Does not prove**: 所有Project Memory都使用OpenAI layout；current environment永远无误；自动update已正确解决冲突。
- **Course mapping**: project-scoped memory可以提供历史locator、decision与experience，但当前repo / config / build / service事实要重新取证。
- **Version limitation**: Beta behavior可能变化，正文需保留retrieval date / beta scope。

### EC-10｜Update、Conflict、Delete、Forget分离

- **Claim**: `15-C10`
- **Status**: `CONFIRMED`
- **Sources**:
  - [OpenAI Agents SDK — Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/)
  - [OpenAI Conversations — Delete conversation](https://developers.openai.com/api/reference/typescript/resources/conversations/methods/delete)
  - [OpenAI Conversations — Delete item](https://developers.openai.com/api/reference/ruby/resources/conversations/subresources/items/methods/delete)
  - [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)
  - [Google ADK — Session](https://adk.dev/sessions/session/)
  - [LangGraph — Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)
- **Observation**: SDK Session有pop / clear；ADK有delete_session；OpenAI Conversations API明确“delete conversation”不删除items并提供独立item delete；OpenAI sandbox consolidation按recency移除older raw memories；LangGraph docs建议prune old checkpoints或retention policy。
- **Proves**: resource deletion、item deletion、history clear、retention pruning与consolidation forgetting是不同操作，不能共用一个含糊布尔值。
- **Does not prove**: 删除API擦除backups / logs / derived memories；forgetting满足privacy法规；所有后端同步完成；冲突可用删除解决。
- **Counter-evidence**: “删除conversation即可删除一切”的泛化被OpenAI current API直接反驳。
- **Course decision**: lifecycle需分别记录update / conflict / invalidation / delete request / delete result / forgetting policy。

### EC-11｜BuildPilot / Unity 4310 -> 4472证据上限

- **Claim**: `15-C11`
- **Status**: `PROPOSAL`
- **Source class**: Article Card engineering anchor + course synthetic scenario；无runtime source。
- **Scenario**:
  - build 4310 memory candidate：`WeChat Mini Game startup bottleneck may be associated with X`；scope=`project P / WX / build 4310 / observed_at T1`；kind=`HYPOTHESIS or EXPERIENCE_CANDIDATE`。
  - build 4472 recall：只把旧记录作为Retrieved Candidate；核对project / platform / build / profile / source revision，取得current artifact / measurement后再corroborate、conflict、update或invalidate。
- **Proves**: 仅证明课程schema可以表达跨build freshness检查与historical-only disposition。
- **Does not prove**: build 4310 / 4472真实存在；X是瓶颈；启动性能改善 / 退化；BuildPilot Runtime存在；memory pipeline已执行。
- **Required label**: `SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM`。
- **Article 16 boundary**: 不讨论怎样检索到该candidate，只讨论召回后怎样判scope与freshness。

### EC-12｜Scope Mismatch与Privacy / Contamination Risk

- **Claim**: `15-C12`
- **Status**: `PARTIAL`
- **Sources**:
  - [Google ADK — State](https://adk.dev/sessions/state/)
  - [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)
  - [Microsoft Semantic Kernel — Using memory with Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-memory)
- **Observation**: ADK prefixes区分session / user / app / temp scope；OpenAI memory isolation由layout而非agent name决定，相同layout与conversation ID会共享consolidated memory；Semantic Kernel Mem0Provider提供Application / Agent / Thread / User scoping options。
- **Proves**: scope key / layout configuration会改变哪些主体、会话或agent共享memory。
- **Does not prove**: 已发生隐私泄漏；任一产品的完整ACL失败；特定攻击路径或合规影响。
- **Narrow inference**: 如果Host把错误tenant / user / project / environment映射到可共享namespace，out-of-scope内容可能成为retrieval candidate；因此在Injected前应做scope eligibility检查。
- **Why PARTIAL**: 共享机制由official docs确认；privacy incident与enforcement没有runtime / security test。
- **Article 19 boundary**: 不设计permission lattice、approval、sandbox、identity proof或audit enforcement。

### EC-13｜Conflict与Revision Policy

- **Claim**: `15-C13`
- **Status**: `PROPOSAL`
- **Sources**:
  - [W3C PROV-O](https://www.w3.org/TR/prov-o/)
  - [OpenAI Agents SDK — Sandbox Agent memory](https://openai.github.io/openai-agents-python/sandbox/memory/)
- **Observation**: PROV-O可表达derivation、revision、primary source与invalidation；OpenAI current docs承认memory可能stale并允许live update。
- **Proves**: 记忆对象可拥有来源、revision与invalidation关系；stale update是current product concern。
- **Does not prove**: W3C定义memory conflict；newest wins；高confidence wins；live update能安全合并并发冲突。
- **Proposal**: conflicting memories保留`conflict_set`、各自scope / source / version / observed_at与`UNRESOLVED | SUPERSEDED | INVALIDATED | SCOPED_BOTH_VALID`，直到Host policy裁决。
- **Limitation**: 实际并发、transaction与CAS留给实现；本篇只冻结语义边界。

### EC-14｜Project Memory与Knowledge Base边界

- **Claim**: `15-C14`
- **Status**: `PROPOSAL`
- **Sources**:
  - [Google ADK — Memory](https://adk.dev/sessions/memory/)
  - [LangGraph — Memory overview](https://docs.langchain.com/oss/python/concepts/memory)
  - canonical Article 15 / 16 boundary（repository authority）
- **Observation**: ADK把MemoryService描述为可跨sessions、也可包含external sources的searchable knowledge；LangGraph用Store保存cross-session user / application data并可search。产品上Memory与knowledge search可以交叉。
- **Proves**: memory与searchable knowledge在真实框架中可能共享service / store，不能用物理组件强行分界。
- **Does not prove**: Memory与KB同义；任意chat history是KB；检索到的内容可信；Article 15应展开RAG。
- **Course boundary**: Project Memory按project scope治理facts / decisions / experiences；KB按organized knowledge与source boundary治理。Article 16拥有Retrieve / Filter / Rerank / Inject / Cite。
- **Forbidden expansion**: vector DB、embedding、chunking、retriever implementation、ranking与retrieval eval。

## Cross-claim Traceability

| Required topic | Covered by |
|---|---|
| Context / History / Working Memory / Session / Long-term / Project / Checkpoint / KB | 15-C02, EC-02 |
| Session vs provider thread / conversation / run | 15-C01, 15-C03, EC-01, EC-03 |
| Memory Write Candidate | 15-C06, EC-06 |
| Stored vs Retrieved vs Injected | 15-C05, EC-05 |
| scope / source / provenance / timestamp / version / confidence / trust | 15-C07, EC-07 |
| expiry / invalidation / update / conflict / deletion / forgetting | 15-C07, 15-C10, 15-C13, EC-07, EC-10, EC-13 |
| Working Memory hypothesis promotion bug | 15-C08, EC-08 |
| Project Memory != Current Reality | 15-C09, EC-09 |
| BuildPilot / Unity 4310 vs 4472 ceiling | 15-C11, EC-11 |
| Article 14 / 16 / 19 boundaries | EC-08, EC-11, EC-12, EC-14 |
| privacy / scope mismatch without full permissions | 15-C12, EC-12 |
| no RAG tutorial | 15-C14, EC-05, EC-14 |

## Evidence Gate

### Result

`RESEARCHER_RECOMMENDATION: PASS`

### Gate Checks

- [x] 每个核心Claim只有`CONFIRMED / PARTIAL / PROPOSAL`之一；`BLOCKED=0`。
- [x] 每个核心Claim都有Evidence Card。
- [x] 每张Card写明source、retrieved date / version scope、proves、does-not-prove、counter-evidence或limitation。
- [x] 产品事实与课程抽象分开；Beta / experimental面已标明。
- [x] `PARTIAL` 15-C12已收窄为risk，不写已发生泄漏。
- [x] BuildPilot案例标为synthetic、not executed、no runtime claim。
- [x] Article 14 / 16 / 19边界明确；没有Article 16资产或RAG教程内容。
- [x] 删除、遗忘、invalidation与update没有混成同一动作。

### Next Allowed Gate

`OUTLINE`（仅在Master验证本Evidence Gate后）。

### Blocker

`NONE`
