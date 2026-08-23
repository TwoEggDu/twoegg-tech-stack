# Evidence｜Article 14 Working Memory 与 Investigation State

## Gate Status

- Gate under review: `EVIDENCE_GATE`
- Research owner recommendation: `PASS CANDIDATE`
- Retrieved date: `2026-08-22`
- Required Lab: `NONE`
- Core behavior claims: `12`
- Core behavior Claim `BLOCKED` count: `0`
- Status counts: `CONFIRMED 5 / PARTIAL 2 / PROPOSAL 5 / BLOCKED 0`
- Review boundary: Master must independently verify this candidate before advancing the workflow state.

## Research Questions — Answer Register

| RQ | Answer | Evidence | Status |
|---|---|---|---|
| RQ1 | Working Memory 可作为当前未完成任务的可更新、带版本工作投影；这是课程操作定义，不是行业统一术语 | LangGraph state / memory、Google ADK state、OpenAI context / RunState、Magentic-One ledger | `PROPOSAL` supported by product-scoped facts |
| RQ2 | Snapshot 是一步的选定视图；History 是时间序列；Workflow State 有迁移权；Checkpoint 是恢复边界；Long-term Memory 跨 session / thread；Evidence 是独立支持物 | `14-C03`—`14-C05`, `14-C10` | `PARTIAL` |
| RQ3 | goal、hypotheses、accepted facts、unresolved、evidence refs、rejected、pending、completion gaps、revision 构成课程最小建议；数值 confidence 非必需 | Magentic-One、W3C PROV、framework state versions | `PROPOSAL` |
| RQ4 | OBSERVED / INFERRED / HYPOTHESIS / UNKNOWN 是认知种类；REJECTED 更适合做 hypothesis disposition；每次升级需 refs 与 host rule | provenance + course synthesis | `PROPOSAL` |
| RQ5 | model / tool / operator 可 propose；runtime 经 event / reducer / state service commit；semantic acceptance 仍需 host policy，不能由 model 自封 | LangGraph reducers、ADK Event / SessionService 与 direct-mutation warning | runtime path `CONFIRMED`；acceptance policy `PROPOSAL` |
| RQ6 | 丢 active-state 中的冗余、可重算与外置大工件；持久化恢复 / 交接必需的 goal、revision、claim states、refs、pending 与 gaps | ADK persistence scopes、LangGraph checkpoints + course risk policy | mechanism `CONFIRMED`；selection `PROPOSAL` |
| RQ7 | CS0103 只支持“名称在当前 context 不存在”；Unity Console 可报告脚本编译错误。真实 BuildPilot 根因、修复、完整 build outcome 均不可伪造 | Microsoft CS0103、Unity 2022.3 Console | `CONFIRMED / NARROW` |

## Source Register

所有来源均在 `2026-08-22` 实时读取；技术主张只依赖以下 primary / official sources。

| Source ID | Source | Product / version scope | Proves | Does not prove |
|---|---|---|---|---|
| `S-LG-PERSIST` | [LangGraph Persistence](https://docs.langchain.com/oss/python/langgraph/persistence) | 当前 hosted Python OSS docs；未固定 package version | step checkpoint、thread、StateSnapshot、state history、replay、`update_state` 新建 checkpoint | 跨产品 Working Memory 标准；业务语义验证 |
| `S-LG-GRAPH` | [LangGraph Graph API](https://docs.langchain.com/oss/python/langgraph/graph-api) | 当前 hosted Python OSS docs；未固定 package version | node 返回 update；每个 key 由 reducer 合并；state 可含 messages | reducer 自动判定 claim 为真 |
| `S-LG-MEM` | [LangGraph Memory](https://docs.langchain.com/oss/python/langgraph/add-memory) | 当前 hosted Python OSS docs；未固定 package version | short-term memory 为 thread-scoped state；long-term store 跨 thread | 本课程 schema |
| `S-ADK-STATE` | [Google ADK Session State](https://adk.dev/sessions/state/) | 当前 hosted docs；标注 Python / Go / Java / Kotlin `v0.1.0+`、TypeScript `v0.2.0+`；未锁定安装版本 | state scratchpad、scope prefix、SessionService persistence、delta 更新、direct mutation 风险 | 所有框架都有相同 scope；semantic acceptance |
| `S-ADK-SESSION` | [Google ADK Session](https://adk.dev/sessions/session/) | 当前 hosted docs；未固定 SDK package version | events / state 分离；Runner 把 update 封装成 Event；SessionService append + update | Session 等于 Working Memory |
| `S-ADK-MEM` | [Google ADK Memory](https://adk.dev/sessions/memory/) | 当前 hosted docs；未固定 SDK package version | current session state 与跨 past interactions MemoryService 的边界 | Article 15 的长期记忆设计 |
| `S-OAI-CONTEXT` | [OpenAI Agents SDK Context management](https://openai.github.io/openai-agents-python/context/) | 当前 hosted Python SDK docs；未固定 package version | local application context 与 LLM-visible conversation context 不同 | local context 自动持久化 |
| `S-OAI-RUNSTATE` | [OpenAI Agents SDK RunState](https://openai.github.io/openai-agents-python/ref/run_state/) | 当前 hosted Python SDK docs；未固定 package version | 可序列化 run snapshot 支持 pause / resume，含 generated items / approvals 等 | 通用 checkpoint 或 Working Memory schema |
| `S-OAI-SESSION` | [OpenAI Agents SDK Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/) | 当前 hosted Python SDK docs；未固定 package version | Session protocol 维护特定 session 的 conversation history | history 就是 Investigation State |
| `S-TEMP-EVENT` | [Temporal Event History](https://docs.temporal.io/workflow-execution/event) | 当前 hosted docs；未固定 Server / SDK version | Event History 是 append-only log，可用于跟踪进度与恢复应用状态 | History 与 State 必须物理隔离 |
| `S-M1-PAPER` | [Magentic-One paper](https://arxiv.org/abs/2411.04468) | 2024 original paper / arXiv `2411.04468` | Task / Progress Ledger 与 task-duration short-term memory 设计先例 | 行业标准；课程精确字段；可靠生产效果 |
| `S-PROV` | [W3C PROV Overview](https://www.w3.org/TR/prov-overview/), [Constraints](https://www.w3.org/TR/prov-constraints/), [PROV-O](https://www.w3.org/TR/prov-o/) | W3C Recommendations，2013 | entity / activity / agent、derivation、revision、invalidation、attribution | claim 为真或已通过 Evidence Gate |
| `S-CS0103` | [Microsoft C# CS0103](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103) | 当前 compiler-message docs；未绑定 Unity Roslyn 具体版本 | 名称在当前 class / namespace / scope / context 不存在；存在多种检查方向 | 单一具体根因 |
| `S-UNITY-CONSOLE` | [Unity 2022.3 Console](https://docs.unity3d.com/2022.3/Documentation/Manual/Console.html), [scriptCompilationFailed](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/EditorUtility-scriptCompilationFailed.html) | Unity `2022.3` docs | Console 展示脚本编译错误；日志有 compilation error 时 API 返回 true | BuildPilot 真实运行、根因、修复、完整 build outcome |

## Claim Register

| Claim ID | Core claim | Status | Evidence refs | Required wording boundary |
|---|---|---|---|---|
| `14-C01` | 被检查的官方生态使用不同的 product-scoped state / context / session / memory 构造 | `CONFIRMED` | `S-LG-*`, `S-ADK-*`, `S-OAI-*` | 只说“所检查来源不同”，不声称穷尽行业 |
| `14-C02` | Working Memory 是当前未完成任务的可更新、带版本工作投影 | `PROPOSAL` | `S-LG-MEM`, `S-ADK-STATE`, `S-M1-PAPER` | 必须标 `COURSE PROPOSAL` |
| `14-C03` | Working Memory 与 Context Snapshot、History、Workflow State、Checkpoint、Long-term Memory、Evidence 有角色边界 | `PARTIAL` | `S-LG-PERSIST`, `S-ADK-*`, `S-OAI-*`, `S-TEMP-EVENT`, `S-PROV` | 角色综合，不声称物理存储绝不重叠 |
| `14-C04` | History 记录时间序列，current state 是当前投影；state 可由 history 恢复，也可包含 messages | `CONFIRMED` | `S-ADK-SESSION`, `S-TEMP-EVENT`, `S-LG-GRAPH` | product-scoped，不写成唯一实现 |
| `14-C05` | task / thread-scoped state 可以持久化；持久化时长不自动把它变成 Long-term Memory | `CONFIRMED` | `S-LG-PERSIST`, `S-LG-MEM`, `S-ADK-STATE`, `S-ADK-MEM` | scope 与 storage duration 分开表述 |
| `14-C06` | state update 可由 runtime 通过 reducer / event / service 受管提交，direct mutation 可能绕过保证 | `CONFIRMED` | `S-LG-GRAPH`, `S-LG-PERSIST`, `S-ADK-STATE`, `S-ADK-SESSION` | 不把 managed commit 等同 semantic truth |
| `14-C07` | model output 只是 mutation suggestion；host policy 验证后才可成为 accepted fact | `PROPOSAL` | `S-ADK-STATE`, `S-LG-GRAPH` | runtime authority 有事实依据；精确验证管线是课程设计 |
| `14-C08` | Investigation State 采用 goal / hypotheses / accepted / unresolved / refs / rejected / pending / gaps / revision | `PROPOSAL` | `S-M1-PAPER`, `S-PROV`, `S-LG-PERSIST` | 精确字段全部标 `COURSE PROPOSAL`；confidence 可选 |
| `14-C09` | OBSERVED / INFERRED / HYPOTHESIS / REJECTED / UNKNOWN 使用受控边界，REJECTED 作为 disposition | `PROPOSAL` | `S-PROV` + course synthesis | 不是 W3C 或任一产品标准 taxonomy |
| `14-C10` | evidence refs / provenance 可记录来源、派生、revision 与 invalidation，但不能自行证明 claim 为真 | `PARTIAL` | `S-PROV` | provenance 能力确认；课程 acceptance 语义为 proposal |
| `14-C11` | 只持久化恢复、交接、防重复副作用所需状态；冗余 / 可重算项退出 active state | `PROPOSAL` | `S-LG-PERSIST`, `S-ADK-STATE` | 是风险导向课程 policy，不是框架强制清单 |
| `14-C12` | CS0103 只证明名称在当前 context 不存在；Unity Console observation 不证明 BuildPilot 根因或修复 | `CONFIRMED` | `S-CS0103`, `S-UNITY-CONSOLE` | synthetic case only；无 Runtime / Lab claim |
## Evidence Cards

### EC-14-01｜Product terms are scoped, not universal

- Claim: `14-C01`
- Status: `CONFIRMED`
- Sources / scope: `S-LG-*`, `S-ADK-*`, `S-OAI-*`；当前 hosted docs，具体 package versions 未锁定。
- Observation: 三组官方资料分别使用 graph state / thread memory、session scratchpad / events、local context / LLM context / Session / RunState，边界并不相同。
- Proves: 本篇必须逐产品解释术语，不能假装存在一个共同对象。
- Does not prove: 全行业没有其他定义；任何单一厂商术语优先。
- Counter-evidence handled: 名称有相似处，且都可能保存 messages；因此 claim 只限定于被检查来源的定义差异。

### EC-14-02｜Course Working Memory definition

- Claim: `14-C02`
- Status: `PROPOSAL`
- Sources / scope: `S-LG-MEM`, `S-ADK-STATE`, `S-M1-PAPER`。
- Observation: thread / session state 与 task ledger 都提供任务期内可更新状态的先例。
- Proves: 课程抽象有工程与研究设计支点。
- Does not prove: “工作投影”、精确字段、revision policy 是行业规范。
- Required article label: `COURSE PROPOSAL`，第一次定义和 schema 旁均需出现。

### EC-14-03｜Boundary matrix

- Claim: `14-C03`
- Status: `PARTIAL`
- Sources / scope: `S-LG-PERSIST`, `S-ADK-SESSION`, `S-ADK-MEM`, `S-OAI-CONTEXT`, `S-OAI-RUNSTATE`, `S-OAI-SESSION`, `S-PROV`。
- Observation: 产品资料分别把 selected context、events / history、current state、resumable run state 和 cross-session memory 分成不同职责；provenance 标准又描述来源与派生。
- Proves: 角色分拆有直接来源支撑。
- Does not prove: 课程七分法是标准；实现必须物理分库。
- Counter-evidence handled: state 可含 messages，checkpoint 可含 state，history 可恢复 state；边界按职责而非物理容器表述。

### EC-14-04｜History and current projection

- Claim: `14-C04`
- Status: `CONFIRMED`
- Sources / scope: `S-ADK-SESSION`, `S-TEMP-EVENT`, `S-LG-GRAPH`。
- Observation: ADK Session 同时有 chronological events 与 state；Temporal Event History 是 append-only log 并用于恢复；LangGraph state channel 可包含 messages。
- Proves: “发生过什么”和“现在按什么继续”是可分角色，且 history 可以产生 current state。
- Does not prove: history / state 必须物理隔离；所有 state 都可完整重建。
- Counter-evidence handled: 直接写明 reconstruction 与 storage overlap。

### EC-14-05｜Persistence duration is not memory scope

- Claim: `14-C05`
- Status: `CONFIRMED`
- Sources / scope: `S-LG-PERSIST`, `S-LG-MEM`, `S-ADK-STATE`, `S-ADK-MEM`。
- Observation: LangGraph checkpointer 与 ADK persistent SessionService 能让 thread / session state 跨运行保存；两者仍把 cross-thread / cross-session memory 另列。
- Proves: 短期 task state 可以 durable；scope 不能只由保存时长推断。
- Does not prove: 所有 task state 都应持久化；Article 15 的 retention policy。
- Counter-evidence handled: 数据库保存很久仍可能只是一个 thread 的恢复状态。

### EC-14-06｜Managed update path

- Claim: `14-C06`
- Status: `CONFIRMED`
- Sources / scope: `S-LG-GRAPH`, `S-LG-PERSIST`, `S-ADK-STATE`, `S-ADK-SESSION`。
- Observation: LangGraph node 返回 updates 并由 reducer 应用；`update_state` 建新 checkpoint。ADK Runner / Event / SessionService 应用 state delta，且官方警告 direct mutation 绕过历史、持久化、线程安全与时间戳维护。
- Proves: 可靠状态改变需要 runtime 管理的提交路径；model 文字本身不是持久 mutation。
- Does not prove: reducer 或 SessionService 检查 claim 的业务真实性。
- Counter-evidence handled: ADK `output_key` 可把模型文本经运行时写入 state，证明 managed commit 与 semantic acceptance 必须分开。
### EC-14-07｜Suggestion is not accepted fact

- Claim: `14-C07`
- Status: `PROPOSAL`
- Sources / scope: `S-ADK-STATE`, `S-LG-GRAPH`。
- Observation: 官方框架说明谁应用 update，却未承诺自动完成 evidence-based semantic validation。
- Proves: 必须避免把 runtime write success 写成 truth guarantee。
- Does not prove: 本篇提出的 schema / identity / revision / evidence-ref validator 是厂商要求。
- Course rule: model、tool、operator 只提交 `MutationCandidate`；host policy 决定 allowed mutation 与 claim acceptance，reducer 负责确定性合并。

### EC-14-08｜Investigation State fields

- Claim: `14-C08`
- Status: `PROPOSAL`
- Sources / scope: `S-M1-PAPER`, `S-PROV`, `S-LG-PERSIST`。
- Observation: Magentic-One 的 Task Ledger 有 verified facts、facts to look up / derive、educated guesses、task plan；Progress Ledger 有 task complete / progress / next speaker；PROV 支持 refs 与 derivation；checkpoint 有 state version 先例。
- Proves: goal、fact、unknown、hypothesis、plan、gap、ref 与 revision 各有设计动机。
- Does not prove: 精确字段名、必填性、JSON / YAML 结构或 confidence score。
- Boundary: `confidence` 默认可选且只可定性；无校准证据时不得输出伪概率。

### EC-14-09｜Epistemic taxonomy

- Claim: `14-C09`
- Status: `PROPOSAL`
- Sources / scope: `S-PROV` 提供 provenance / derivation / invalidation 概念；精确 taxonomy 为课程综合。
- Observation: provenance 可以区分来源、派生与失效关系，但 W3C 不定义本篇五个标签。
- Proves: taxonomy 可以要求 observation locator、inference derivation 与 rejection counter-evidence。
- Does not prove: 五标签是标准 ontology。
- Boundary: storage 中建议 `kind=OBSERVATION|INFERENCE|HYPOTHESIS|UNKNOWN`，`disposition=ACTIVE|REJECTED`；读者层仍可显示五个标签。

### EC-14-10｜Evidence references are not truth

- Claim: `14-C10`
- Status: `PARTIAL`
- Sources / scope: `S-PROV`，W3C Recommendations 2013。
- Observation: PROV 描述 entity / activity / agent、generation、derivation、revision、invalidation 与 attribution。
- Proves: evidence ref 至少应保存 source / locator / version / relation，而非只留一段摘要。
- Does not prove: 被引用内容可信、claim 为真、课程 Evidence Gate 已通过。
- Boundary: Evidence 本体独立保存；Working Memory 只存 ref、受控摘要和 claim status。

### EC-14-11｜Discard / persist policy

- Claim: `14-C11`
- Status: `PROPOSAL`
- Sources / scope: `S-LG-PERSIST`, `S-ADK-STATE`。
- Observation: 产品提供 ephemeral 与 persistent backend、checkpoint / thread 等机制，但不规定本课程的最小保存清单。
- Proves: durability 可以按 backend / boundary 选择。
- Does not prove: 冗余消息、accepted fact、rejected summary 等具体保留规则是产品要求。
- Course rule: 恢复 / handoff / 防重复副作用所需字段任务级持久化；重复、可廉价重算、已外置大工件退出 active state，但不得删除 Evidence / History。

### EC-14-12｜Bounded CS0103 example

- Claim: `14-C12`
- Status: `CONFIRMED`
- Sources / scope: `S-CS0103`（当前 Microsoft compiler message docs，未绑定 Unity Roslyn 版本）；`S-UNITY-CONSOLE`（Unity 2022.3）。
- Observation: CS0103 意味着 name 在当前 context 不存在；Unity Console 能显示脚本编译错误并提供日志细节。
- Proves: 一个带 identifier / file / line 的 Console artifact 可以被登记为 `OBSERVED`，随后提出多个待检验原因。
- Does not prove: 缺 `using`、asmdef、define、生成代码中的任一原因；BuildPilot 真实运行；修复有效；整个 build 终态。
- Counter-evidence handled: 官方诊断页给出多个检查方向；示例必须标 synthetic / illustrative，`Required Lab: NONE`。
## Counter-evidence Register

| Counter ID | Disconfirming case | Claim impact | Resolution |
|---|---|---|---|
| `14-CE01` | LangGraph、ADK、OpenAI 对 state / memory / session / context 用词不同 | `14-C01`, `14-C02` | 不宣布行业统一定义；Working Memory 标 `COURSE PROPOSAL` |
| `14-CE02` | LangGraph state 可有 messages channel | `14-C03`, `14-C04` | History / state 按职责区分，不声称物理隔离 |
| `14-CE03` | Temporal 可从 append-only Event History 恢复应用状态 | `14-C03`, `14-C04` | 明写 history 可重建 / 产生 current state |
| `14-CE04` | LangGraph checkpoint 本身是 state snapshot | `14-C03` | 写“Checkpoint 可保存 Working Memory”，不写“二者从不重叠” |
| `14-CE05` | persistent backend 可让 short-term state 跨进程长期存在 | `14-C05` | 以 thread / task scope 区分，不用存储时长命名 |
| `14-CE06` | ADK `output_key` 可把 model final text 经 Runner 写进 state | `14-C06`, `14-C07` | 分开 runtime commit 与 semantic acceptance；后者仍为课程 policy |
| `14-CE07` | PROV 描述 provenance，不裁定 truth | `14-C09`, `14-C10` | evidence ref 不能直接升级 accepted fact |
| `14-CE08` | 没有来源提供跨任务校准的 confidence 数值 | `14-C08` | confidence 可选；默认使用 status / refs / counter-evidence / next test |
| `14-CE09` | CS0103 有多种可能原因 | `14-C12` | diagnostic 只登记 observation；各根因分别作为 hypothesis |
| `14-CE10` | Unity Console error 不等于完整 build terminal receipt | `14-C12` | 不声称 BuildPilot 真实 build outcome；案例只做 state evolution |

## Version and Product Scope

| Area | Locked scope | Drift risk | Article wording rule |
|---|---|---|---|
| LangGraph | `2026-08-22` 当前 hosted Python OSS docs；未锁定 package version | API / page semantics 可能随发布变化 | 使用“当前官方文档描述”，不写永久契约 |
| Google ADK | `2026-08-22` hosted docs；state 页标注 Python / Go / Java / Kotlin `v0.1.0+`、TS `v0.2.0+`；未锁定安装版本 | prefix / SessionService 行为或支持语言可能变化 | 保留版本说明；不跨 SDK 推广 |
| OpenAI Agents SDK | `2026-08-22` 当前 hosted Python SDK docs；未锁定 package version | Session / RunState API 可变化 | 只用于概念反例与当前行为，不给兼容承诺 |
| Temporal | `2026-08-22` 当前 hosted docs；未锁定 Server / SDK version | 文档组织与 API 可能变化 | 只引用 Event History 的角色 |
| Magentic-One | 2024 原始论文，arXiv `2411.04468` | 研究实现与后续版本可能变化 | 称“设计先例”，不称标准或生产保证 |
| W3C PROV | 2013 W3C Recommendations | 标准稳定，但映射到课程 taxonomy 是综合 | 明确 provenance != truth |
| C# CS0103 | `2026-08-22` 当前 Microsoft compiler-message docs；未绑定 Unity Roslyn version | 编译器诊断细节可能随版本变化 | 只使用稳定的最窄语义 |
| Unity | Unity `2022.3` Manual / Scripting API | 其他 Unity 版本 UI / compiler integration 可变化 | 示例明确限定 Unity 2022.3 |

## Mutation Authority Boundary

事实层允许写：

```text
node / agent output
  -> runtime-managed update
  -> reducer or event/session service
  -> new state / checkpoint
```

课程层必须标提案：

```text
model / tool / operator proposes MutationCandidate
  -> host validates schema + identity + base_revision + allowed fields
  -> host checks evidence refs + transition guard + conflicts
  -> deterministic reducer applies
  -> runtime commits revision + mutation event
  -> host acceptance policy changes claim disposition
```

两层不能合并。`commit succeeded` 只证明更新被运行时接受，不证明其中 claim 为真。

## Discard / Persist Boundary

- Default discard from active Working Memory: 重复 History 文本、纯格式 scratch、可廉价重算中间量、已被替代的细计划、已外置保存的大工件正文。
- Task-durable: goal / completion criteria、revision、accepted facts + refs、active hypotheses + next tests、unresolved、rejected reason + refs、pending actions + authority、completion gaps。
- Checkpoint when: context reset / process restart / handoff、恢复昂贵、需要审计、并发 writer、下一动作有外部副作用或重试风险。
- Never infer: “丢出 active state”不等于删除 History / Evidence；“持久化”不等于升级 Long-term Memory。

以上选择均为 `COURSE PROPOSAL`；框架来源只证明可选的 persistence mechanism。

## Limitations

1. 未锁定 LangGraph、ADK、OpenAI Agents SDK 与 Temporal 的具体安装版本；文章必须保留 retrieved date 与 hosted-doc scope。
2. 没有找到跨生态规范化 Working Memory schema；`14-C02`、`14-C07`—`14-C09`、`14-C11` 不得升级为 `CONFIRMED`。
3. W3C PROV 不提供 evidence quality、truth 或课程 acceptance gate。
4. 本次没有真实 Unity / BuildPilot artifact、compiler invocation、asmdef graph、define set、rerun receipt 或 terminal build receipt。
5. `Required Lab: NONE`；synthetic CS0103 演进只能展示 taxonomy 和 mutation control，不能承担 Runtime 事实证明。
6. 不研究 reducer algebra、CRDT、distributed transactions、confidence calibration 或 retention governance。

## Article 15 / 16 Non-scope

- Article 15 reserved: Session identity / continuity、cross-session or Project Memory、consolidation、retention / deletion、memory lifecycle。
- Article 16 reserved: RAG、embedding、vector database、chunking、retrieval ranking、external knowledge indexing、retrieval evaluation。
- Article 14 只保存 task-scoped evidence refs 与 investigation projection；不把 retrieval 或 durable project knowledge 写成本文能力。

## Evidence Gate Checklist

- [x] 七个 Research Questions 均有明确答案与状态。
- [x] 技术行为只依赖 official docs、标准或原始论文。
- [x] 每个来源记录 retrieved date、product / version scope、proves / does-not-prove。
- [x] 每个核心 claim 有 evidence refs、wording boundary 与 Evidence Card。
- [x] counter-evidence 已登记并反映到 claim wording。
- [x] 所有精确课程定义、schema、taxonomy、semantic validation 与 retention policy 均降级为 `PROPOSAL`。
- [x] CS0103 案例限定为 synthetic；未伪造 Unity / BuildPilot Runtime。
- [x] Article 15 / 16 非范围已显式保护。
- [x] Required Lab 为 `NONE`。
- [x] Core behavior Claim `BLOCKED` count = `0`。

## Evidence Gate Recommendation

- Recommendation: `PASS CANDIDATE`
- Core claims: `12`
- `CONFIRMED`: `5`
- `PARTIAL`: `2`
- `PROPOSAL`: `5`
- `BLOCKED`: `0`
- Gate rationale: 核心行为 claim 没有证据硬阻塞；不充分处均已收窄或诚实标为 `COURSE PROPOSAL`，反证、版本范围与 non-scope 均已保存。
- Authority boundary: 本文件不自行推进 authoritative Workflow State；Master 独立复核后才可执行 `EVIDENCE_GATE`。
