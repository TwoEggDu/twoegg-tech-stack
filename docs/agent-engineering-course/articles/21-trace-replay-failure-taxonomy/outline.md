# Article 21 Outline｜Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层

## Outline contract

- Article Type: `PRINCIPLE`
- Course Weight: `L / Major Core Lesson`
- Teaching Spine: Article 20 `trace_ref` bridge -> problem space（有记录却仍无法定位）-> abstract model（identity / causality / event envelope）-> concrete mechanisms（Replay family / replayability manifest / failure records）-> engineering judgment（contract owner / unknown / redaction / effect boundary）-> BuildPilot design walk-through -> verification boundary -> Article 22 candidate-sample seam
- Core Claim Scope: `21-C01`—`21-C12` only；不新增 core Claim / Evidence Card
- Evidence Posture: `1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`
- Proposal Discipline: 课程 event envelope、Replay 六分法、occurrence/observation/recovery 三层与七层 Failure Taxonomy 均必须显式标注 `COURSE PROPOSAL`；不得归因于 OTel、CloudEvents、NIST、LangGraph、AWS、Azure 或 OpenAI Agents SDK
- Evidence Discipline: Product/hosted docs 只按访问日 `2026-08-26` 与各自产品语义使用；pinned standard/spec 只证明对应原语；无 Lab、无真实 Trace、无 BuildPilot failure corpus

> 如果这篇只记一句话：`Trace 的价值不是把日志存得更多，而是用稳定身份、因果关系和副作用边界，保存“哪个合同先失守、谁看见了什么、怎样恢复、还不知道什么”。`

## Teaching Spine

```text
Article 20 leaves trace_ref
  -> logs/metrics/error messages exist, but a failure still cannot be located
  -> separate Log / Metric / Trace / Audit Record
  -> bind run/turn/step/tool-call/attempt/event identities
  -> preserve scoped order + causal links + occurred/observed time
  -> store an event envelope with payload/redaction references
  -> declare Replay mode and effect policy
  -> freeze or disclose nondeterministic inputs and equivalence level
  -> separate failure occurrence / observation / recovery
  -> classify the earliest evidenced contract breach by owner
  -> retain root candidate / factors / symptoms / recovery / unknowns
  -> minimize sensitive payloads without hiding replay limitations
  -> walk a BuildPilot design sample, then hand only candidate slices + lineage to Article 22
```

### Spine checkpoints

| Stage | Reader transformation | Required artifact in the article | Failure if omitted |
|---|---|---|---|
| Problem space | 从“多打日志”转向“缺少可关联的控制事实” | 四视图分账 + 一个有日志仍无法归因的构造反例 | 文章退化成 observability 工具清单 |
| Abstract model | 能区分 identity、order、causality、time、payload 与 redaction | identity hierarchy + causal graph + event envelope | 事件只能靠时间邻近猜关系 |
| Concrete mechanism | 能声明 replay mode、manifest、effect boundary 与 equivalence | Replay family table + replayability manifest | 把 rerun/retry/resume 都叫 replay |
| Engineering judgment | 能按首先失守的 owned contract 分类，并保留 unknown | 三层 failure model + 七层 taxonomy + failure record | 把最外层 exception 当 root cause |
| Verification boundary | 能说清哪些只是 proposal、哪些不能由本篇验证 | BuildPilot design-only walk-through + Article 22 seam | 设计 shape 被误读成 runtime/Eval 事实 |

## Opening bridge｜Article 20 已留下 `trace_ref`，为什么“日志很多”仍无法定位

- Reader Question: Budget terminal、Tool Runtime JSONL、Provider request ID 和异常日志都存在时，为什么仍可能回答不了“错误究竟发生在哪一层”？
- Claims: `21-C01 PARTIAL`, `21-C02 PROPOSAL`, `21-C07 PROPOSAL`。
- Evidence Cards: `21-E01`, `21-E02`, `21-E07`。
- Planned teaching move:
  - 接住 Published Article 20：Budget record 只拥有 budget-local decision/reason 与 `trace_ref` seam，不拥有跨 Step 重建。
  - 构造一个不执行的 BuildPilot 反例：Provider log 报 timeout、Tool log 报 success、State 仍停在旧 revision、恢复记录写 `RESUME`；四条记录都“是真的”，但没有共同 run/step/tool-call/event identity，也没有 causal/effect link，因此无法判断 timeout 是 occurrence、symptom 还是 recovery 之后的旧观察。
  - 先立最短判断：`more records != attributable failure`；记录存在只说明有载体，不说明身份闭合、因果闭合、状态可重建或副作用可安全重演。
- Boundary / Non-goal:
  - 不把“缺日志”假定为唯一问题；不承诺 event envelope 一旦存在就能自动找到根因。
  - 不在开场列 OTel API、SDK 配置或 tracing 开关；外部标准只在抽象建立后作为有界依据。
- Example / Figure responsibility:
  - Figure `F21-01`: “四条都正确但互相对不上”的构造时间线；职责是制造读者痛点，不充当 runtime observation。
  - Mandatory label: `CONSTRUCTED COURSE EXAMPLE / NOT A RUNTIME TRACE / BUILDPILOT NOT RUN`。
- Section takeaway: **日志不足的常见根因不是记录太少，而是记录没有共同身份、因果边、状态边界和 effect receipt。**

## Part A｜问题空间：先把四种记录分账

### 1. Log、Metric、Trace、Audit Record 各回答什么，为什么任一单独存在都不证明可重放

- Reader Question: 一条 error log、一条失败率 metric、一条 distributed trace 和一条审批 audit record 分别能回答什么？
- Claims: `21-C01 PARTIAL`。
- Evidence Cards: `21-E01`。
- Planned comparison:

  | View | Primary question | Minimum shape | Useful correlation | Cannot alone prove |
  |---|---|---|---|---|
  | Log | 某组件在某时报告了什么 | discrete record、severity、body、resource、observed time | run/span/event ref | 完整因果、state reconstruction、effect status |
  | Metric | 一段时间内数量/延迟/比率怎样变化 | timeseries、aggregation、dimensions | exemplar/run/span ref | 单次 Run 的 decision/payload/history |
  | Trace | 一次 Run/请求经过哪些相关 operation/event | span/event/link/parent/causal refs | business + provider correlation IDs | audit authorization、payload completeness、deterministic replay |
  | Audit Record | 谁在何时何地对什么做了什么，结果如何 | actor/action/target/time/outcome/policy/approval refs | request/decision/event ref | 完整运行 State、性能分布、可执行 replay |

- Mechanism responsibility:
  - 用 OTel trace/log/metric 与 NIST AU-3 分别支撑“结构和主要问题不同”。
  - 明确四者可以共享 identity/ref，也可以由同一 backend 承载；“责任不同”不等于“必须四套系统”。
- Boundary / Non-goal:
  - `PARTIAL` 语态：不称四分法穷尽行业术语，不称 OTel 是唯一 observability/Trace 标准。
  - `trace exists` 不升级为 audit complete、replayable、root-cause confirmed 或 production reliable。
- Example / Figure responsibility:
  - Table `T21-01` 即本节主视觉；每格最后一列专门阻止“有一种信号就足够”的捷径。
  - Example `EX21-01`: 同一 timeout 在 Metric 中只是计数，在 Log 中是 message，在 Trace 中是 observer event，在 Audit 中是 operator decision target；不判断 root cause。
- Section takeaway: **四类记录是互补视图，不是四个可互换的“可观测性开关”。**

### 2. 从“有记录”到“这是同一次执行”：为什么必须分开 run / turn / step / tool-call / attempt / event identity

- Reader Question: Provider `trace_id` 或一条 Tool invocation ID 已存在，为什么仍不足以标识整个 Agent Run？
- Claims: `21-C02 PROPOSAL`。
- Evidence Cards: `21-E02`；dependency boundary 读取 Published Articles 06、08、11。
- Proposed identity model:

  | Identity | Scope | Lifecycle rule | Must not be replaced by |
  |---|---|---|---|
  | `run_id` | 一次有 terminal semantics 的 Agent execution | rerun 产生新 `run_id` | provider request/trace ID |
  | `turn_id` | Run 内一次外部输入到控制权交还 | 不承担 universal loop counter | OpenAI max-turn / graph super-step |
  | `step_id` | 一次 committed Decision/Act/Observe/State transition | 绑定 state-before/after | tool call ID |
  | `tool_call_id` | 一次 Tool action intent | 关联 request/result/effect receipt | retry attempt ID |
  | `attempt_id` | 同一 intent 的一次执行尝试 | retry 产生新 attempt，保留原 intent | 新 run identity |
  | `event_id` | 单个 immutable event envelope | 不复用表达另一个 event | timestamp 或 sequence |

- Mechanism responsibility:
  - W3C `trace-id/parent-id` 与 OTel SpanContext 只作为 correlation precedents。
  - Provider `trace_id` / `span_id` / `request_id` 进入 `correlation_ids`，不覆盖课程 business/control identities。
  - 接回 Article 06：same invocation ID/same digest 的 single-process replay 只是一条 Tool seam；接回 Article 08：Step 是课程 committed iteration；接回 Article 11：Resume 需要 same logical run 与 continuation boundary。
- Boundary / Non-goal:
  - 全表标 `COURSE PROPOSAL`；不声称 W3C/OTel 定义了 Agent hierarchy。
  - 不定义 ID generator、global uniqueness service、跨组织 federation 或 storage key layout。
- Example / Figure responsibility:
  - Figure `F21-02`: `Run -> Turn -> Step -> Tool call -> Attempt` nesting，旁挂 immutable Events；职责是展示 scope/lifecycle，而不是类图。
  - Example: retry 使用相同 `tool_call_id` / intent digest、新 `attempt_id`；rerun 使用新 `run_id`。
- Section takeaway: **Provider correlation identity 可以帮助串服务，但不能替 Agent 的 Run、Step、action intent 与 retry attempt 定义生命周期。**

## Part B｜抽象模型：用因果与 Event Envelope 保存可重建控制事实

### 3. 时间顺序为什么不是因果顺序：sequence、parent 与 causal links 怎样分工

- Reader Question: 把所有记录按 timestamp 排序，为什么仍可能错判谁导致了谁？
- Claims: `21-C03 PARTIAL`。
- Evidence Cards: `21-E03`。
- Planned causal model:
  1. `sequence.scope + value`：只在明确 scope（例如 `run_id + producer_id`）内提供稳定顺序。
  2. `parent_event_id`：直接结构父关系，例如一个 Step 下的 Tool intent。
  3. `caused_by[] / links[]`：跨分支、异步 callback、多输入聚合的因果依赖。
  4. `occurred_at`：producer 认为事件发生的时间；`observed_at`：collector 实际观察时间。
- Engineering judgment:
  - 并发事件可能只形成 partial order；若没有 causal edge，禁止从时间邻近自动补一条因果关系。
  - clock skew、delayed ingestion、重试与不同 collector 会让 total timestamp sort 产生错误叙事。
- Boundary / Non-goal:
  - Lamport paper支撑 happens-before/partial order，不规定本文字段名；OTel支撑 timestamp/observed timestamp/links precedent，不证明课程 schema 充分。
  - 不实现 logical clock、vector clock、distributed ordering service 或 global total order。
- Example / Figure responsibility:
  - Figure `F21-03`: 两个并发 Tool call、一个 late callback、一个聚合 State commit；标出 wall-clock order 与 causal edges 不同。
  - Table mini-callout: `sequence != timestamp != causality`。
- Section takeaway: **时间告诉你“看起来先后如何”，因果边才告诉你“这个事件依赖了什么”。**

### 4. 最小 Event Envelope：索引事实与大/敏感 payload 为什么要分离

- Reader Question: 一条可关联事件至少要保存哪些字段？为什么不能把完整 prompt、Tool output 和 approval 内容都塞进一行？
- Claims: `21-C04 PROPOSAL`，并为 `21-C10 PARTIAL` 预留 redaction seam。
- Evidence Cards: `21-E04`, `21-E10`。
- Proposed envelope groups:
  - Schema and identity: `schema_version`, `event_id`, `event_type`, `source`。
  - Base required: `schema_version`, `event_id`, `event_type`, `source`, `run_id`, scoped `sequence`, `occurred_at`, `observed_at`, `actor_ref`。
  - Base optional: `turn_id`, `step_id`, `parent_event_id`, `caused_by[]`, `correlation_ids`；root event 可以没有 parent/cause。
  - Tool specialization: `tool.result_observed` 要求 `step_id`, `tool_call_id`, `attempt_id`, `payload_ref`；非 Tool event 不要求 Tool/attempt identity。
  - Time and actor: `occurred_at`, `observed_at`, `actor_ref`。
  - Conditional control references: State / Policy / Approval / Payload refs 只在相应 `event_type` 合同存在时 required；缺失就 validation fail 或显式 gap，不写 fabricated placeholder。
  - Payload descriptor（适用时）: `uri`, `digest`, `media_type`, `schema_ref`, `size`。
  - Disclosure state: `redaction.state`, `policy_ref`, affected fields；`correlation_ids` 保留 OTel/provider IDs。
- Planned schema sketch: 使用 Research 中 `base_required / base_optional / event_types` requiredness matrix，不复制看似真实的占位 ID；至少对照 `run.started` 与 `tool.result_observed`。
- Engineering judgment:
  - Envelope 保存可索引、可关联、可判边界的事实；payload store 保存经授权的大对象或敏感内容。
  - digest 只在 algorithm/use 正确时支撑 content identity/integrity checking；不证明真实性、适用性、可访问性或授权。
- Boundary / Non-goal:
  - Whole schema 必须标 `COURSE PROPOSAL`；CloudEvents 和 in-toto 只是 envelope/resource-descriptor precedent。
  - 不声称 CloudEvents compliance，不实现 storage、immutability、retention、schema registry 或 access-control service。
- Example / Figure responsibility:
  - Figure `F21-04`: “thin envelope -> restricted payload/evidence store”的引用图；职责是展示 diagnostics 与 disclosure 分层。
  - Example event: `tool.result_observed`，payload 仅保留 digest/ref，不出现真实 secret/tool output。
- Section takeaway: **Event Envelope 应保存关系和引用，不应把所有原文复制到一个默认可见的 Trace 中。**

## Part C｜具体机制：Replay 不是一个动作，而是一组必须声明边界的操作

### 5. Replay、Resume、Retry、Rerun、Simulation、Projection 怎样切开

- Reader Question: 团队说“把这次 Run replay 一遍”时，究竟要重建 State、重调外部服务，还是只生成一个视图？
- Claims: `21-C05 PROPOSAL`。
- Evidence Cards: `21-E05`；dependency seam 读取 Articles 06/11。
- Required comparison:

  | Mode | Identity relation | External actions | Primary claim | Mandatory metadata |
  |---|---|---:|---|---|
  | Reconstruction Replay | 原 `run_id` + new reconstruction session | No | fold retained events into state/view | source event set + reducer/schema version |
  | Controlled Execution Replay | source run + new replay execution ID | Maybe | 从声明 boundary 重新执行 | boundary + adapters + side-effect policy |
  | Resume | same logical `run_id` from checkpoint | Maybe | 继续未完成 work | checkpoint + continuation + revalidation |
  | Retry | same action intent + new `attempt_id` | Yes | 再次尝试同一 intent | eligibility + budget + idempotency/effect knowledge |
  | Rerun | new `run_id` | Yes | 从声明输入开启新 Run | source input refs + new identity |
  | Simulation | new simulation ID | fake/frozen only | hypothetical behavior | fake/frozen adapters + no production effects |
  | Projection | projection ID + source-set digest | No | derived read-only view | projection version + source digest |

- Mechanism responsibility:
  - LangGraph time travel、AWS Redrive、Azure Event Sourcing 分别作为产品/模式反例，证明同一“replay”词可包含不同跳过、重跑和投影语义。AWS 的常规行为是保留成功步骤并从未成功步骤继续，但 `States.DataLimitExceeded` 下 Parallel / Inline Map / Distributed Map 会连同原先成功的 branch / iteration / child workflow 重跑。
  - 每次操作至少声明 `mode / boundary / source_manifest / side_effect_policy / expected equivalence`。
- Boundary / Non-goal:
  - 六/七分法是课程 proposal，不称行业标准，也不把 cited product 映射强行一一对齐。
  - 不实现 replay engine、state reducer、fake adapter、AWS/LangGraph integration 或 DSH Article 34 的 append-only session mechanism。
- Example / Figure responsibility:
  - Figure `F21-05`: Replay family decision tree，第一问“只读重建还是执行动作”，第二问“same run continuation or new execution”。
  - Counterexample: “same prompt 再发一次”最多是 rerun/controlled execution candidate，不是 deterministic replay。
- Section takeaway: **不写 mode、boundary 与 side-effect policy 的 replay 声明，工程上等于没有说明要做什么。**

### 6. 非确定性与 effect boundary：什么时候可以说“可重放”，什么时候必须说“只能受控再执行”

- Reader Question: 保存同一 prompt 和 checkpoint 后，为什么仍不能承诺相同输出或安全副作用？
- Claims: `21-C06 PARTIAL`。
- Evidence Cards: `21-E06`；Published Articles 06、11、20 作为课程 seam。
- Replayability manifest responsibilities:
  - source event set、event schema/digest；
  - reducer/runtime/code version；
  - provider/model/model snapshot/sampling config；
  - policy/tool/adapter/config version；
  - input/state/checkpoint version；
  - time/random seed/scheduler/concurrency order；
  - external request/response/timeout/rate limit/dependency version；
  - side-effect intent/receipt/idempotency key/reconciliation result；
  - payload availability/redaction/access authorization；
  - known gaps + declared equivalence level。
- Equivalence ladder:
  - `same event fold` -> `same logical state` -> `same normalized outcome class`；每一级都必须有对应证据。
  - `bit-for-bit deterministic` 只在明确 frozen scope 与真实验证下可用；本篇无实验，因此不使用。
- Effect decision:
  - Reconstruction/Projection 默认不得执行生产 side effect。
  - Controlled replay/Resume/Retry 在 effect status unknown 时先 lookup/reconcile；Article 11 的 stable identity seam 仍不提供 exactly-once。
  - Budget remains 不批准 Retry；Article 20 only bounds already eligible work。
- Boundary / Non-goal:
  - LangGraph caveat 与 RFC 9110 只支撑 product replay 可重新执行非确定性工作、HTTP idempotency有限语义；不证明 manifest 完整。
  - 不声称跨 Provider、跨版本、跨时间、跨外部环境或真实模型 deterministic。
- Example / Figure responsibility:
  - Table `T21-02`: nondeterministic input -> required record/freeze -> missing-data consequence。
  - Figure `F21-06`: side-effect fork (`KNOWN_ABSENT / KNOWN_PRESENT_QUERYABLE / UNKNOWN`) -> execute/reconcile/ask-stop；职责是守住 Article 11 effect seam，不扩成 Recovery 教程。
- Section takeaway: **Replayability 是一份有版本、有缺口、有等价级别的声明，不是“我们保存了 prompt”这一事实。**

## Part D｜工程判断：失败要按发生、观察、恢复分层，再按 contract owner 分类

### 7. Failure occurrence、observation、recovery 为什么必须保存为三个独立事件层

- Reader Question: Runtime 最后 fallback 成功，为什么不能把原 failure 改写成“没有失败”？
- Claims: `21-C07 PROPOSAL`。
- Evidence Cards: `21-E07`。
- Proposed three-layer model:

  ```text
  Occurrence: 哪个 contract 在哪一层首先失守？
      -> emits / causes
  Observation: 哪个 observer 在何时以什么 symptom 看见它？
      -> triggers
  Recovery: 谁依据哪条 policy 选择 retry/resume/fallback/abort，结果怎样？
  ```

- Required separation:
  - 三层各自有 event identity、actor、occurred/observed time 与 evidence refs。
  - observation 可迟到、可不完整、可在外层出现；recovery success 是 outcome，不反向删除 occurrence。
  - 一个 timeout 可是 occurrence（deadline contract breach），也可能只是 observer symptom；要看最早有证据的 failed contract。
- Boundary / Non-goal:
  - NIST incident guidance只支撑 detection/analysis/recovery是不同 concerns；三层 exact event model 是课程 proposal。
  - 不写 incident-response 流程、SRE taxonomy、自动 root-cause engine 或 recovery implementation。
- Example / Figure responsibility:
  - Figure `F21-07`: 内层 State version conflict -> Runtime 观察为 turn timeout -> Recovery 选择 resume/fallback；全部标 `CONSTRUCTED`。
  - Counterexample: `fallback_succeeded` 不得把 `occurrence.status` 改成 `NONE`。
- Section takeaway: **恢复成功说明系统后来做了什么，不说明原先的失败从未发生。**

### 8. 七层 Failure Taxonomy：按“首先失守的 owned contract”分类，而不是按最响亮的异常名分类

- Reader Question: Model、Policy、Tool、Runtime、State、Infrastructure 与 External Dependency 同时出现 breach 时，怎样区分唯一 primary、并发 co-primary、owner boundary 与证据不足？
- Claims: `21-C08 PROPOSAL`。
- Evidence Cards: `21-E08`。
- Mandatory label: `COURSE PROPOSAL / NOT AN INDUSTRY STANDARD / NOT VALIDATED AGAINST A FAILURE CORPUS`。
- Classification rule:
  1. 列出 component/contract owner map 与已证实 breach occurrences。
  2. 在 causal/contract partial order 中求最早 occurrence set。
  3. 记录 `classification_status = SINGLE | CO_PRIMARY | BOUNDARY | UNKNOWN`、`occurrence_event_ids[]` 与 `primary_layers[]`。
  4. `CO_PRIMARY` 表示证据充分但存在多个独立最小 breach；`BOUNDARY` 表示已证实的 owned contract 横跨 owner；`UNKNOWN` 只表示证据不足。
  5. 只有 Evidence 支撑 causal/contract ordering 时，才把其它 breach 降为 contributing factor 或 symptom。
- Seven-layer table:

  | Layer | First-owned contract that may breach | Example symptom only | Non-inference |
  |---|---|---|---|
  | Model | response validity/capability/grounding/declared model contract | malformed output/refusal/hallucinated field | 不把所有 bad result 都归模型 |
  | Policy | authorization/routing/guardrail/budget-retry eligibility | denied action/wrong route/budget exhausted | deny 可是正确 control outcome |
  | Tool | schema/adapter/invocation/result/effect contract | validation error/tool exception/ambiguous effect | outer timeout 不自动归 Tool |
  | Runtime | scheduling/dispatch/correlation/checkpoint/recovery orchestration | lost callback/duplicate dispatch/deadlock | provider error 不自动归 Runtime |
  | State | version/precondition/serialization/invariant/commit | stale version/corrupt checkpoint/conflict | history change 不等于 State commit |
  | Infrastructure | owned compute/network/storage/platform capacity | process crash/disk unavailable/queue outage | 第三方 outage 不归 owned infra |
  | External Dependency | provider/third-party/service contract outside owned stack | upstream 429/outage/remote state changed | provider message 不自动给 root cause |

- Boundary / Non-goal:
  - 七层不声称 exhaustive、mutually exclusive、statistically validated 或适合所有组织。
  - 不把 policy deny、budget exhaustion 或 cancellation 一律叫 failure；必须先读取其 declared terminal contract。
  - Article 22 才能用 corpus/labels/metrics 检查 taxonomy 的 operational value；本篇不预做。
- Example / Figure responsibility:
  - Figure `F21-08`: contract-owner ladder，职责是帮助定位“谁拥有先失守的合同”，不是技术栈分层图。
  - Exercise example: upstream 429 -> External Dependency occurrence；Runtime timeout 是 symptom；retry-budget exhausted 是 recovery/policy terminal，不把三者并列为三个 root cause。
  - Concurrent counterexample: 并发分支分别发生 Tool schema breach 与 Runtime callback-loss breach，二者无 causal edge 且都阻止聚合；记录 `CO_PRIMARY` + `[TOOL, RUNTIME]`，不能任选一个降为 factor，也不能写成 `UNKNOWN`。
- Section takeaway: **分类应保存最早 breach occurrence set；证据充分但非唯一时写 CO_PRIMARY/BOUNDARY，证据不足才写 UNKNOWN。**

### 9. Failure Record：root candidate、contributing factor、symptom、recovery outcome 与 unknown 怎样分账

- Reader Question: 如果外层 exception 不是 root cause，Trace 中应该怎样保存当前最强判断与仍未知内容？
- Claims: `21-C09 PROPOSAL`。
- Evidence Cards: `21-E09`。
- Proposed record fields:
  - `failure_id`, `classification_status`, `occurrence_event_ids[]`, `primary_layers[]`, `failed_contract_ref`。
  - `root_failure.status = CONFIRMED | CANDIDATE | UNKNOWN`, code, evidence refs。
  - `contributing_factors[]`：有 evidence ref 的放大/触发条件。
  - `symptoms[]`：observer、code、event ID；不因 message 清晰而升级成 root。
  - `recovery`：decision event、mode、outcome；成功/失败/带缺口均保留。
  - `unknowns[]`：例如外部 effect status、丢失 payload、不可比较版本。
- Engineering judgment:
  - 默认用 `CANDIDATE` 或 `UNKNOWN`，只有独立 Evidence 关闭替代解释后才用 `CONFIRMED`。
  - Root candidate 是当前可审查判断，不是永不变化的真值；新 Evidence 应追加 revision/relationship，不覆盖旧观察。
  - 与 Article 18 对齐：Trace/failure record 可以成为 Evidence source，不能自动完成 Claim acceptance。
- Boundary / Non-goal:
  - OTel exception/status只是 observation precedent；NIST支持 root-cause/recovery concerns，不规定本字段集合。
  - 不实现自动 RCA、Bayesian confidence、causal inference engine 或 organization-wide incident code registry。
- Example / Figure responsibility:
  - Code block `EX21-02`: 15—20 行 failure record，root status=`CANDIDATE`，unknowns 非空；禁止使用真实 incident ID。
  - Figure responsibility: 不另画新图，复用 `F21-07` 三层关系，避免把 schema 与 process画成两条重复主线。
- Section takeaway: **Trace 要保存“当前知道到哪里”，而不是把最外层 symptom 改名成 root cause。**

## Part E｜治理边界：诊断可见性不能以默认复制敏感数据为代价

### 10. Sensitive payload、Tool output、approval evidence 与 redaction 怎样兼顾可诊断性和权限边界

- Reader Question: 不保存完整 prompt/Tool output 会损失 replayability；保存完整内容又会扩大泄露面，Trace 应怎样诚实取舍？
- Claims: `21-C10 PARTIAL`。
- Evidence Cards: `21-E10`；Published Article 19 提供 approval identity/scope seam。
- Proposed four-step boundary:
  1. **Minimize**：能用 typed field、digest、schema/object ref 回答的问题，不默认保存 raw body。
  2. **Reference**：公开/常规 Trace 只含 payload/evidence refs 与 redaction state；restricted store 保存授权原文。
  3. **Bind approval evidence**：保存 approval request/decision IDs、decider identity、scope、expiry/consumed/revocation state refs，不只写 `approved=true`。
  4. **Degrade explicitly**：payload deleted/inaccessible/redacted 时，列出 diagnostics、reconstruction、controlled replay 哪些能力失效。
- Redaction record responsibilities:
  - `state = NONE | PARTIAL | FULL | UNAVAILABLE`（课程字段候选）；
  - `policy_ref`, affected fields, reason, performed_at/actor ref；
  - original payload digest/ref 是否仍可经授权访问，若否则写 gap。
- Engineering judgment:
  - hash 不等于匿名化；低熵/可枚举值可能被猜测。
  - exception、stack trace、model/tool data 与 approval context 都可能敏感。
  - “关闭敏感 tracing 开关”不证明 compliance，也不证明剩余 Trace 仍可 replay。
- Boundary / Non-goal:
  - `PARTIAL` 语态；OTel/OpenAI only support handling/settings precedents，NIST only supplies audit/privacy concerns。
  - 不设计法规合规方案、DLP、KMS、retention schedule、secret store 或 access-control implementation。
- Example / Figure responsibility:
  - Figure `F21-09`: public envelope / restricted evidence store / deletion-redaction gap 三层图。
  - Example callout: redacted tool output仍保留digest/schema/ref，但 manifest写 `controlled_execution_replay=LIMITED`；不声称可恢复原文。
- Section takeaway: **Redaction 不是把内容删掉后继续宣称 Trace 完整，而是让缺失内容及其诊断/replay 代价可见。**

## Part F｜具体落地：用 BuildPilot Design 走一遍，不把设计写成运行事实

### 11. BuildPilot design walk-through：从一条 Tool Observation 到 STATE failure candidate 与 Recovery record

- Reader Question: 前面的 identity、event、replay、failure 与 redaction contract 放到 BuildPilot 时，最小设计链怎样站住？
- Claims: `21-C02`—`21-C10` 按原 ceiling组合使用；`21-C12 CONFIRMED` 冻结现实边界。
- Evidence Cards: `21-E02`—`21-E10`, `21-E12`。
- Mandatory label: `BUILDPILOT COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN / NOT A RUNTIME TRACE`。
- Constructed scenario:
  - Goal: 只读地调查一份“身份尚需固定”的 build result；不触发真实 Jenkins、Unity、Provider、filesystem write 或发布。
  - Source seams: Article 18 `evidence_ref`、Article 19 `approval_ref`、Article 20 `budget_ref` 均只作为 `REQUIRED/NOT_CREATED` references；不伪造 accepted/approved/admitted 事实。
  - Event chain design:
    1. `run.started` 绑定 design-only run/turn identity。
    2. `tool.call.requested` 绑定 tool intent、attempt、state-before ref。
    3. `tool.result_observed` 引用 payload digest/redaction state。
    4. `state.commit.rejected` 构造一个 version/precondition conflict candidate。
    5. Runtime later observes `TURN_TIMEOUT` symptom。
    6. Recovery design event chooses `RESUME` candidate with outcome `NOT_RUN`。
  - Failure classification exercise:
    - `classification_status: SINGLE` 与 `primary_layers: [STATE]` 只作为 **constructed candidate**，因为例子故意指定唯一最早 breach 为 `state-transition/v-design`；不得改写成 BuildPilot 真实 root cause。
    - delayed callback 是 contributing factor candidate；turn timeout 是 symptom；effect status 与 payload accessibility 保留 unknown。
- Walk-through checks:
  1. 所有 event 是否有 immutable ID 与 declared scope？
  2. timestamp 之外是否有 causal links？
  3. payload/redaction 是否限制 reconstruction/controlled replay？
  4. chosen replay mode 是 reconstruction、resume 还是 retry，effect policy 是否允许？
  5. occurrence、observation、recovery 是否分别保存？
  6. classification status / occurrence set / primary layers 是否按 causal/contract ordering，而非 timeout message 或强制单选？
  7. root status 是否保持 `CANDIDATE`，unknown 是否保留？
- Boundary / Non-goal:
  - 不创建真实 IDs、timestamps、payloads、approval decisions、budget receipts、runtime events 或 failure corpus。
  - 不声称 BuildPilot 有 Trace store、reducer、Replay engine、Failure classifier、redaction pipeline、准确率、成本、延迟、可靠性或 production benefit。
  - Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`。
- Example / Figure responsibility:
  - Figure `F21-10`: BuildPilot design sequence，逐节点标 `DESIGN EVENT`，最后分出 FailureRecord 与 ReplayabilityManifest。
  - Inline schema：只展示关键 refs 与 `UNKNOWN/NOT_RUN`，不得使用看似真实的生产值。
- Section takeaway: **设计样例的合格结果不是“成功 replay”，而是每个 identity、boundary、candidate 与 unknown 都没有被伪造成运行事实。**

### 12. Verification boundary 与 Article 22 seam：Trace 只能提供 candidate samples 和 lineage

- Reader Question: 一条结构完整的 failure trace 为什么还不是 Golden Dataset，也不能证明某次修复不会再坏？
- Claims: `21-C11 PROPOSAL`, `21-C12 CONFIRMED`。
- Evidence Cards: `21-E11`, `21-E12`。
- Article 21 may hand off:
  - normalized event/failure slices；
  - input/output/effect references + provenance；
  - model/policy/tool/runtime/state version manifest；
  - redaction/access constraints；
  - recovery outcome、unknowns、source trace digest；
  - candidate taxonomy label + label provenance/status。
- Article 22 exclusively owns:
  - sample acceptance/rejection into Golden Dataset；
  - oracle/label correctness 与 reviewer process；
  - train/test/regression split、representativeness；
  - metric、threshold、baseline、pass/fail/regression verdict；
  - Lab 06 Trace + Eval design/execute/observation/evidence merge。
- Engineering judgment:
  - Trace is a lineage source, not a self-validating sample。
  - taxonomy candidate不等于 gold label；recovery succeeded不等于 quality improved；replay reproduced不等于 regression test passed。
  - 本篇的七层 taxonomy 未经 failure corpus 验证；Article 22 即使未来评估，也必须另行冻结 workload、oracle 与 criteria。
- Verification boundary:
  - Can establish: signal/identity/causality/replay/failure/redaction 的有界来源与课程设计；12/12 Claim traceability。
  - Cannot establish: deterministic replay、exactly-once、root-cause accuracy、taxonomy coverage/precision、security/compliance、BuildPilot behavior、Eval/Regression outcome。
  - Frozen reality: Required Lab `NONE`; Experiment `0`; Runtime `ABSENT`; BuildPilot `DESIGN / NOT IMPLEMENTED / NOT RUN`。
- Boundary / Non-goal:
  - 不创建 Article 22 workspace、dataset、sample file、label、metric、threshold、Lab 06 或 regression plan。
  - 不提前写 DSH Article 34 的 append-only Session Event runtime/source conclusions。
- Example / Figure responsibility:
  - Figure `F21-11`: `Trace slice + lineage -> candidate sample inbox || Article 22 curation/eval gate`；中间使用明确的“不自动通过” Gate。
  - Table `T21-03`: Article 21 output vs Article 22 owner；职责是课程边界收口。
- Section takeaway: **Trace 可以交付可追溯候选样本，只有 Eval 合同才能决定它是不是 Golden、修复是不是 Regression PASS。**

## Engineering anti-patterns｜一条 Trace / Replay 设计通常怎样写坏

- Reader Question: 哪些短路等式会把记录、因果、重建、重执行、根因、恢复与评估混成一件事？
- Claims: 不新增 Claim；只将 `21-C01`—`21-C12` 转成 design-review counterexamples。
- Evidence Cards: `21-E01`—`21-E12`。
- Boundary / Non-goal: 反模式是 evidence-backed review heuristics，不是 failure frequency conclusion 或行业穷举。
- Example / Figure responsibility: 使用一张表完成，不新增主图。

| Shortcut | Responsibility swallowed | Minimum correction |
|---|---|---|
| `more logs = root cause found` | identity/causality/contract owner | bind run/step/event + evidence refs；unknown stays unknown |
| `provider trace_id = Agent run_id` | business/control lifecycle | provider IDs stay under correlation IDs |
| `timestamp sort = causality` | partial order/async links | scoped sequence + parent/caused-by links |
| `trace exists = replayable` | versions/nondeterminism/effects/payload access | replayability manifest + equivalence level |
| `same prompt again = deterministic replay` | new execution identity/environment | classify as rerun/controlled replay candidate |
| `resume = no re-execution` | checkpoint/product-specific boundary | declare continuation and effect policy |
| `retry budget remains = retry allowed` | Article 11 effect/eligibility + Article 19 authority | qualify first, budget only bounds eligible work |
| `outer exception = root cause` | occurrence/factor/symptom distinction | root status candidate/unknown + evidence refs |
| `fallback succeeded = no failure` | recovery history | preserve occurrence + recovery outcome |
| `seven layers = industry standard` | proposal/evidence ceiling | label COURSE PROPOSAL; validate later |
| `hash = anonymous` | disclosure risk | sensitivity assessment + access/redaction metadata |
| `trace slice = Golden Dataset sample` | curation/oracle/eval | hand candidate + lineage to Article 22 |
| `BuildPilot schema = runtime` | implementation/observation | preserve DESIGN / NOT IMPLEMENTED / NOT RUN |

## Claim-to-section coverage（12 / 12）

| Claim | Status ceiling | Primary sections | Evidence Card | Mandatory wording / boundary |
|---|---|---|---|---|
| `21-C01` | `PARTIAL` | Opening, 1, anti-patterns | `21-E01` | signals互补；任一单独不证明 replay；不称 exhaustive taxonomy |
| `21-C02` | `PROPOSAL` | Opening, 2, 11 | `21-E02` | hierarchy 是课程 contract；provider IDs 只作 correlation |
| `21-C03` | `PARTIAL` | 3, 11 | `21-E03` | timestamp 与 causality分开；字段名/充分性不称标准 |
| `21-C04` | `PROPOSAL` | 4, 11 | `21-E04` | base/specialization 分开；root/non-Tool 可省关系字段；refs 按事件合同 required |
| `21-C05` | `PROPOSAL` | 5, 11 | `21-E05` | Replay family是课程分账；产品语义不泛化 |
| `21-C06` | `PARTIAL` | 6, 11 | `21-E06` | no deterministic/bit-identical/cross-provider claim；effect boundary explicit |
| `21-C07` | `PROPOSAL` | Opening, 7, 11 | `21-E07` | three layers are course event model；recovery success不抹 occurrence |
| `21-C08` | `PROPOSAL` | 8, 11 | `21-E08` | `SINGLE/CO_PRIMARY/BOUNDARY/UNKNOWN` + occurrence set；no exhaustiveness/validation claim |
| `21-C09` | `PROPOSAL` | 9, 11 | `21-E09` | root defaults CANDIDATE/UNKNOWN；symptom/factor/recovery separate |
| `21-C10` | `PARTIAL` | 4, 10, 11 | `21-E10` | redaction/minimization不证明 compliance 或 replayability |
| `21-C11` | `PROPOSAL` | 12, closing | `21-E11` | only candidate slices + lineage；no Golden/oracle/metric/verdict |
| `21-C12` | `CONFIRMED` | Outline contract, 11, 12 | `21-E12` | Required Lab NONE；Experiment 0；Runtime ABSENT；BuildPilot design-only |

Coverage=`12 / 12`；Evidence Cards=`12 / 12`；Status mix=`1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。

## Figures and examples plan

| ID | Form | Teaching responsibility | Mandatory label / restraint |
|---|---|---|---|
| `F21-01` | four-record timeline | 说明“都有记录仍无法定位” | constructed, no runtime trace |
| `T21-01` | Log/Metric/Trace/Audit table | 四视图分账与 non-inference | OTel/NIST scoped；not exhaustive |
| `F21-02` | identity hierarchy | 展示 run/turn/step/tool-call/attempt/event scope | COURSE PROPOSAL |
| `F21-03` | partial-order graph | 区分 timestamp/sequence/causality | no total-order implementation |
| `F21-04` | thin envelope + payload store | 分离索引事实、payload与redaction | COURSE PROPOSAL；no storage implementation |
| `F21-05` | Replay family tree | 先分只读重建/执行，再分 same/new identity | product terms remain scoped |
| `T21-02` | nondeterminism manifest table | 显示缺失输入如何降低 equivalence claim | no deterministic claim |
| `F21-06` | effect-state fork | 守住 reconcile/ask-stop 边界 | Article 11 seam；no recovery implementation |
| `F21-07` | occurrence/observation/recovery chain | 防止 symptom/recovery覆盖发生层 | COURSE PROPOSAL |
| `F21-08` | contract-owner ladder | 承担七层 taxonomy 选择规则 | COURSE PROPOSAL / unvalidated |
| `EX21-02` | failure record YAML | 分账 root candidate/factor/symptom/recovery/unknown | synthetic values only |
| `F21-09` | disclosure layers | 展示 public refs/restricted payload/redaction gap | no compliance claim |
| `F21-10` | BuildPilot sequence | 把全篇模型落到一个设计链 | DESIGN / NOT IMPLEMENTED / NOT RUN |
| `F21-11` | Trace-to-Eval gate | Article 21只交 candidate+lineage | no Article 22 artifact |

Asset policy: Outline/Draft 优先使用 Markdown 表、ASCII/Mermaid-like text diagram；本 Gate 不创建 `assets/`，不生成截图，不伪造 Trace UI。若未来 Publisher 需要静态图，必须由后续授权 Gate 按相同 label/boundary 制作。

## Learning Check（题目 + answer expectations）

1. 已有 error log、failure-rate metric、distributed trace 和 approval audit record，为什么仍不能宣布 Run 可重放？
   - Expected answer: 四者回答不同问题；还缺 declared identity、causal/state/version/nondeterministic input、payload access 与 effect boundary。记录存在不等于完整、真实、可执行 replay。
2. Provider `trace_id` 为什么不能直接充当 `run_id`？retry 与 rerun 的 identity 又怎样变化？
   - Expected answer: provider ID scope/lifecycle由provider定义，只作correlation；retry保留action intent/tool-call关联并产生新attempt；rerun产生新run。
3. `occurred_at` 早于另一事件，为什么不能自动写 `caused_by`？
   - Expected answer: timestamp只提供时钟观察，因果需要producer order、parent/link或其他证据；并发系统只有partial order。
4. Event Envelope 为什么要把 payload 放到 reference/digest 后面？digest 又不能证明什么？
   - Expected answer: envelope保留索引/关系，payload可受权访问；digest最多支撑content identity/integrity check，不证明truth、authorization、availability或compliance。
5. Reconstruction Replay、Controlled Execution Replay、Resume、Retry、Rerun、Simulation、Projection 的关键区别是什么？
   - Expected answer: 区分identity relation、是否执行外部动作、continuation/source boundary与side-effect policy；projection/simulation不是生产执行，resume不是no re-execution。
6. 保存同一 prompt/checkpoint 后，为什么不能声称 deterministic replay？
   - Expected answer: model/provider/version/time/random/scheduler/external I/O/effect/payload availability仍可能变化；需manifest和equivalence level，本篇无实验。
7. fallback 成功后，occurrence、observation、recovery 应怎样保留？
   - Expected answer: occurrence保存first contract breach，observation保存observer symptom，recovery保存decision/outcome；success不删原failure。
8. 七层 taxonomy 如何区分 SINGLE、CO_PRIMARY、BOUNDARY 与 UNKNOWN？何时才能把另一 breach 降为 factor？
   - Expected answer: 先求最早有证据的 breach occurrence set；唯一是SINGLE，并发独立最小元素是CO_PRIMARY，跨owner合同是BOUNDARY，证据不足才UNKNOWN；只有causal/contract ordering有证据时才能降为factor。
9. root failure、contributing factor、symptom、recovery outcome 与 unknown 为什么不能塞进一个 error message？
   - Expected answer: 它们的证据强度、时点与责任不同；root应有candidate/confirmed/unknown状态和refs，外层message仅是observation。
10. Redaction 后 payload 不可访问，系统应怎样描述 replayability？
    - Expected answer: 显式记录redaction/deletion/access gap，并下调diagnostics/reconstruction/controlled replay能力；不能假装hash可恢复原文或自动匿名。
11. BuildPilot design walk-through 为什么即使结构完整也不能声称发现了STATE root cause？
    - Expected answer: scenario是constructed design，SINGLE/[STATE] classification与root status都只是candidate；Required Lab NONE、runtime ABSENT、BuildPilot not run。
12. Article 21 能给 Article 22 什么，不能给什么？
    - Expected answer: 可给candidate slices、lineage、version/redaction/effect/unknown refs；不能决定Golden acceptance、oracle/label、metric/threshold、baseline、regression verdict或Lab06结果。

## Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Observability architecture | four-view ledger + identity hierarchy | 能说明每种记录回答什么，并建立跨Run/Step/Tool correlation | 不声称 signals 穷尽或 backend 已实现 |
| Distributed causal reasoning | sequence/parent/caused-by + dual timestamps | 能拒绝 timestamp-as-causality，保留 partial order/unknown | 不实现 clock/order service |
| Event contract design | event envelope + payload/redaction refs | 能把 identity、control refs、payload identity与disclosure分账 | COURSE PROPOSAL，not standard/runtime |
| Reliable execution judgment | Replay family + replayability manifest + effect fork | 能区分 reconstruction、re-execution、resume/retry/rerun，并声明 equivalence | no deterministic/exactly-once claim |
| Failure analysis | three-layer model + seven-layer taxonomy + failure record | 能保存earliest occurrence set，区分single/co-primary/boundary/unknown与factor/symptom/recovery | taxonomy unvalidated，no RCA accuracy |
| Security/privacy boundary reasoning | minimization/reference/access/redaction design | 能说明数据缺失怎样限制diagnostics/replay，不把hash叫匿名化 | no compliance/security guarantee |
| Cross-system architecture | Articles 06/08/11/18/19/20/22 seams | 能让Tool、Loop、Recovery、Evidence、Authority、Budget、Eval各守owner | repository-local course model |
| Design communication | BuildPilot design sequence with NOT RUN labels | 能在具体案例中暴露required/unknown/candidate，而不伪造runtime | BuildPilot not implemented/not run |
| Evaluation readiness | Trace-to-Eval handoff table | 能交候选样本+lineage，同时拒绝自封Golden/Regression PASS | Article 22/Lab06 not started here |

## Reference plan

### Primary / normative anchors used in Draft

| Source | Draft responsibility | Evidence ceiling / drift note |
|---|---|---|
| OpenTelemetry Trace API / Logs Data Model / Metrics Data Model | signal structures、span/event/link、timestamp/observed timestamp | hosted `1.60.0` display；accessed `2026-08-26`；not replay/audit standard |
| W3C Trace Context Recommendation (2021-11-23) | `trace-id`/`parent-id` correlation precedent | does not define Agent identities |
| Lamport 1978 paper | happens-before and partial order | does not define event schema |
| CloudEvents v1.0.2 | id/source/type/time/data envelope precedent | pinned；course envelope not claimed compliant |
| in-toto Resource Descriptor v1.0 | URI/digest/content descriptor precedent | pinned；digest not truth/authorization |
| RFC 9110 §9.2.2 | narrow idempotent retry boundary | not exactly-once/effect receipt |
| NIST SP 800-53 Rev. 5 AU-3 | audit record content/privacy concerns | not Agent Trace schema |
| NIST SP 800-61 Rev. 3 / SP 800-184 | detection/analysis/recovery/root-cause concerns | not three-layer/seven-layer taxonomy |
| NIST AI RMF 1.0 | TEVV/metrics/benchmark/uncertainty belong to Eval | revision in progress noted；not Golden schema |

### Product / hosted documentation used as bounded counterexamples

| Source | Draft responsibility | Mandatory restraint |
|---|---|---|
| LangGraph Time Travel / Persistence / Backward Compatibility | product-specific replay and nondeterminism caveat | accessed `2026-08-26`; no universalization/tag mapping |
| AWS Step Functions Redrive | usually preserve successful steps / continue unsuccessful；`States.DataLimitExceeded` may rerun successful branch/iteration/child workflow | not generic replay，not “successful work never reruns”，not exactly-once |
| Azure Event Sourcing pattern | rehydration vs projection precedent | architecture guidance, not standard |
| OTel Handling Sensitive Data | minimization/filter/hash/redaction guidance | does not prove compliance/anonymity |
| OpenAI Agents SDK tracing/configuration | product trace content and sensitive-data switches | does not prove safe defaults/compliance/replay |

### Repository-local references

- Canonical and Glossary: course ownership, Trace/Replay definitions, Article 21/22 seam。
- Published Article 06: Tool invocation/result/terminal Trace/idempotency seam；fixed JSONL is not full Trace/Replay/Failure Taxonomy。
- Published Article 08: Run/Turn/Step local definitions、Observation/State commit、STOPPED vs SUCCEEDED。
- Published Article 11: Checkpoint/Resume/Retry/effect unknown/reconcile boundaries；no exactly-once。
- Published Articles 18—20: Evidence acceptance、authority/approval references、Budget `trace_ref` and decision reason seams。
- Draft must not claim repository-local ownership is an industry architecture standard。

## Published title / slug recommendation

- Recommended published title: `Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层`
- Recommended slug: `agent-engineering-21-trace-replay-failure-taxonomy`
- Recommended path: `content/ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md`
- Recommended series metadata: `series_order: 220`, `weight: 3220`, `primary_series: agent-engineering`
- Alternate title A: `Agent Trace 与 Replay：怎样判断错误首先发生在哪一层`
- Alternate title B: `从日志到可重建 Trace：Replay 边界与七层 Failure Taxonomy`
- Selection judgment: 保留 canonical title；它同时覆盖问题空间（错误在哪层）与两条主机制（Trace/Replay），而 alternate B 容易让“可重建”被误读成“可确定性重放”。
- Slug boundary: 不使用 `observability` 或 `deterministic-replay`，避免把范围扩大成通用可观测性或未被证据支持的确定性保证。

## Explicit non-scope

- 不实现 Trace store、event bus、schema registry、causal-order service、Replay engine、state reducer、projection engine、failure classifier、redaction pipeline、Evidence Store 或 BuildPilot Runtime。
- 不执行 Lab、experiment、Provider call、external side effect、replay、simulation、failure injection、runtime capture 或 production incident analysis。
- 不把 Provider trace/span ID 当 Agent run/step identity，不把 timestamp sorting 当 causal proof。
- 不声称 deterministic/bit-for-bit replay、cross-provider/version/environment equivalence、exactly-once、safe retry 或 complete recovery。
- 不把 occurrence、observation、symptom、root candidate、contributing factor、recovery outcome 合并成一个 exception message。
- 不把七层 taxonomy 称为标准、穷尽、互斥、已通过 corpus 验证或能自动定位 root cause。
- 不保存/展示真实 prompt、Tool output、secret、credential、approval evidence、stack trace 或 production ID；不声称 hash/redaction 等于匿名、合规或安全。
- 不预写 DSH Article 34 的 append-only Session Event source/runtime conclusions。
- 不创建或修改 Article 22 artifact；不定义 Golden Dataset、oracle、labels、metrics、thresholds、baseline、Regression verdict 或 Lab 06。
- 不创建 Draft、Review、Published Content、assets、Lab/runtime/global/canonical/future-Article artifacts。
- Frozen reality: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。

## Closing bridge

- Closing sentence: `真正可诊断的失败，不是一条更详细的报错，而是一组能保留身份、因果、合同 owner、恢复结果与未知边界的事件。`
- Bridge to Article 22:
  - Article 21 输出 candidate event/failure slices、lineage、version/effect/redaction/unknown refs。
  - Article 22 决定哪些样本可以进入 Golden Dataset、oracle/label 如何审查、用什么 metric/threshold/baseline 判 Eval 与 Regression。
  - Lab 06 位于 Article 22 之后，当前仍 `PLANNED / BLOCKED / NOT_IMPLEMENTED`；本篇不设计、不执行、不预填结果。
- Mandatory final boundary sentence: **有可追溯 Trace，只说明 Eval 有了候选数据来源；它不说明样本是 Golden，也不说明修复已经通过 Regression。**

## OUTLINE Gate checklist

- [x] Article Type fixed as `PRINCIPLE`；L-weight structure follows problem space -> abstract model -> concrete mechanism -> engineering judgment -> verification boundary，not API-first。
- [x] Teaching Spine is explicit and begins with “有日志为何仍无法定位”，then separates signal/identity/causality/envelope/replay/failure/redaction before BuildPilot。
- [x] Numbered teaching units=`12`；每个 unit 明确 Reader Question、Claims、Evidence Cards、Boundary/Non-goal、Example/Figure responsibility 与 takeaway。
- [x] Claim coverage=`12 / 12`；Evidence Cards=`21-E01`—`21-E12` only；new core Claim/Card=`NONE`。
- [x] Status ceilings preserved exactly: `C12 CONFIRMED`；`C01/C03/C06/C10 PARTIAL`；`C02/C04/C05/C07/C08/C09/C11 PROPOSAL`；`BLOCKED=0`。
- [x] Log / Metric / Trace / Audit Record are separate views；none alone is written as replay proof。
- [x] run/turn/step/tool-call/attempt/event identity、provider correlation IDs、scoped sequence、parent/causal links、occurred/observed time and payload/redaction references all have teaching responsibility。
- [x] Replay/Resume/Retry/Rerun/Simulation/Projection are separated；replayability manifest covers nondeterministic inputs, versions, external I/O, effect receipts, access gaps and equivalence level。
- [x] Failure occurrence/observation/recovery are independent；seven-layer taxonomy is explicitly `COURSE PROPOSAL`；root candidate/factor/symptom/recovery/unknown remain separate。
- [x] Sensitive/redaction section preserves minimization, reference, access and diagnostic/replay degradation boundaries；no compliance/anonymity claim。
- [x] BuildPilot walk-through is concrete enough to apply the model, but every event/failure remains synthetic design；`DESIGN / NOT IMPLEMENTED / NOT RUN`。
- [x] Article 22 receives only candidate samples + lineage；no Eval/Golden/Regression/Lab 06 work is performed or pre-decided。
- [x] Learning Check includes expected answers；Job Competency is mapped to reader-visible outputs and explicit ceilings。
- [x] Reference plan separates primary/spec anchors, moving product examples and repository-local seams；retrieval/version boundaries are retained。
- [x] Published title/slug/path/series metadata recommendations are included and do not overclaim deterministic replay。
- [x] Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；no Draft/Review/content/assets/Lab/runtime/global/canonical/Git/future-Article write belongs to this Author result。
- [x] OUTLINE Gate recommendation: `PASS`；next allowed gate candidate: `AUTHOR_DRAFT`；Master validation remains outside this artifact.
