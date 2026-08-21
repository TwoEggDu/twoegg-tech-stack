# Article 12 Research｜Context Engineering

## Status

- Gate：`RESEARCH -> EVIDENCE_GATE`
- Research：`COMPLETE`
- Evidence Gate recommendation：`PASS`
- Required Lab：`NONE`
- Required design output：`3 / 3 Context Snapshots for future Lab 05`
- Retrieved scope：`2026-08-21（Asia/Shanghai）`
- Product scope：OpenAI Responses API、OpenAI Agents SDK、Anthropic Claude API / Platform current hosted docs；未固定 SDK package / source commit，事实不外推到其他 Provider 或未来版本。

> 本文件是研究材料，不是正文或 Outline。产品当前合同与课程工作定义 / Proposal 分开记录；没有把任何 request schema、Contributor 分类或 Receipt 字段宣称为行业统一接口。

## Current primary sources

| ID | Source | Stable locator | Retrieved / scope | Used for |
|---|---|---|---|---|
| `S01` | [OpenAI Responses API: Create a model response](https://developers.openai.com/api/reference/cli/resources/responses/methods/create) | `input`、`instructions`、`conversation`、`previous_response_id`、`tools`、`truncation`、`usage` | 2026-08-21；current API reference | request surface、instructions lifecycle、overflow / usage |
| `S02` | [OpenAI Agents SDK: Tracing](https://openai.github.io/openai-agents-python/tracing/)；[generation_span](https://openai.github.io/openai-agents-python/ref/tracing/create/#generation_span) | default tracing、sensitive data、`generation_span(input, output, model, model_config, usage)` | 2026-08-21；current Python SDK docs，package未固定 | application-visible trace及关闭 / 脱敏边界 |
| `S03` | [OpenAI Agents SDK: Sessions](https://openai.github.io/openai-agents-python/sessions/) | session history、merge、continuation options | 2026-08-21；current Python SDK docs | SDK范围内的history implementation example；课程Session仍是可追踪、恢复或回放的交互与执行边界 |
| `S04` | [Anthropic: Context windows](https://platform.claude.com/docs/en/build-with-claude/context-windows) | standard behavior；tool use + thinking；context awareness | 2026-08-21；current Claude docs，model-specific | system / messages / tools / results / output share capacity |
| `S05` | [Anthropic: Tool use overview](https://platform.claude.com/docs/en/agents-and-tools/tool-use/overview)；[How tool use works](https://platform.claude.com/docs/en/agents-and-tools/tool-use/how-tool-use-works) | tool schema / result tokens；special system prompt；client / server loops | 2026-08-21；current Claude docs | Tool Schema / Result budget、provider-managed context、later-request result |
| `S06` | [Anthropic: Count tokens in a Message](https://platform.claude.com/docs/en/api/messages/count_tokens) | `messages / system / tools`；`input_tokens` | 2026-08-21；current Claude API reference | preflight measurement |
| `R02` | Published Article 02 + relevant Research / Evidence | Prompt contract；`Prompt != Context / Current Facts` | repository current main | Prompt不负责完整Context生命周期 |
| `R06` | Published Article 06 + relevant Research / Evidence | ToolDefinition / Result / Trace | repository current main / fixed Lab 02 boundaries | Schema、Result、Trace分层 |
| `R08` | Published Article 08 + relevant Research / Evidence | committed Step；Outcome -> Observation -> State | repository current main / fixed Lab 03 boundaries | accepted State不等于raw Result |
| `R09` | Published Article 09 + relevant Research / Evidence | Plan candidate；History；Verified State | repository current main | Plan / History / State authority不同 |
| `R10` | Published Article 10 + relevant Research / Evidence | Definition / Runtime State / Trace | repository current main | current State与Trace不可互换 |
| `R11` | Published Article 11 + relevant Research / Evidence | Checkpoint / Memory / Recovery | repository current main / fixed Lab 04 boundaries | Snapshot不是Checkpoint或长期Memory |

## Research questions

| RQ | Status | Answer | Claims |
|---|---|---|---|
| `RQ-01` | `ANSWERED` | OpenAI与Anthropic当前合同均显示一次调用不只有Prompt文本；instructions / system、messages / input、tools、tool results / prior items、model settings与output allowance都影响该次调用，但字段与生命周期是Provider-specific。 | `C01 / C04` |
| `RQ-02` | `ANSWERED_AS_PROPOSAL` | 本课程用指令、当前目标、Working State、History、Capabilities、External Facts六类Contributor做provenance review；不是Provider wire schema。 | `C02` |
| `RQ-03` | `ANSWERED_WITH_SCOPE` | 有限窗口、truncation、conversation prepending、tool loading和当前State支持“需要选择”的工程问题；Selection / Ordering / Priority / Scope算法保持Proposal。 | `C03 / C06` |
| `RQ-04` | `ANSWERED` | Anthropic明确system、messages（含tool results）、tools与output共同计入窗口；OpenAI暴露tools、input、max output、truncation与usage。不能给出跨Provider统一token公式。 | `C04 / C05` |
| `RQ-05` | `ANSWERED_AS_PROPOSAL` | Receipt记录来源、版本、选择 / 排除、顺序、预算、冲突、未知与request identity。Trace支持部分字段，但Receipt schema是课程Proposal。 | `C07 / C09` |
| `RQ-06` | `ANSWERED_BY_COUNTER_EVIDENCE` | Anthropic启用tools时自动加入特殊system prompt；Provider可运行server tool loop；SDK trace可关闭、脱敏或在ZDR下不可用。因此Receipt只能描述、审计和比较application-visible Context Snapshot，不能代表全部effective Context。 | `C08 / C09` |

## Product facts vs. course working definition

### Confirmed product facts

- OpenAI Responses current contract分别暴露`input`、`instructions`、`tools`、continuation、output limit、truncation与usage；`previous_response_id`不会自动携带上一条`instructions`。
- Anthropic current docs明确system、messages、tool definitions、tool results和当次output共同计入model-specific context window；Token Count API可估算给定请求组件。
- Anthropic client Tool Result由application在后续request中回传；server tools可能在Provider内部循环后才返回。
- OpenAI Agents SDK generation span可记录input、output、model config与usage；tracing可以关闭、去掉敏感数据，ZDR组织不可用。
- Anthropic启用tools时会自动加入特殊system prompt；application request JSON不是Provider内部全部有效上下文。

### Course working definition (`PROPOSAL / NOT INDUSTRY STANDARD`)

`Context = 某个 Model Step 实际可见的有效 token / 信息集合。应用可构建的对象是 Context Snapshot：在特定 Provider / model / request contract 下，application-visible contributors 经 Select -> Order -> Scope -> Fit Budget 后形成的请求装配视图。`

effective Context还可能包含Provider-managed additions、transformations与unknowns；应用无法把它们收缩为Snapshot。Snapshot刻意只覆盖`application-visible`部分，不包含模型权重、未公开system内容、隐藏推理或未暴露server-side中间态。Receipt服务于对Snapshot的描述、审计和比较，不承诺复刻Provider内部执行。

## Investigation-Step Request Breakdown

Scenario：`INV-12-01`，根据合成 Unity `CS0103`日志与匹配源码判断首个可行动失败点。`NOT_EXECUTED`。

| Order | Contributor | Concrete source | Authority / trust | Selected form | Scope | Budget treatment | Omission / stale consequence |
|---:|---|---|---|---|---|---|---|
| 1 | Instruction | `prompt-contract-v3`：只基于Evidence；未知写UNKNOWN；不修改 | application；trusted | concise instruction | project + role | `MUST_KEEP` | 丢失证据与failure边界 |
| 2 | Current Goal | `DIAGNOSE_FIRST_FAILURE@rev17` | workflow；trusted | goal + output contract | this Step | `MUST_KEEP` | 漂移成“修复”或“总结” |
| 3 | Working State | `EV-LOG-017 / EV-SRC-009 / unresolved=ROOT_CAUSE` | reducer；mixed | typed summary + refs | current run / rev17 | `MUST_KEEP` | 旧revision混淆已知 / 未知 |
| 4 | Capabilities | `read_text@2 / report_diagnosis@1`，`READ_ONLY` | Host registry / policy | two eligible schemas | Stage / Agent | `MUST_KEEP`；排除78项 | 全量工具占预算；缺失则无合法能力 |
| 5 | External Facts | normalized log / source excerpts + hash / locator | observed input；untrusted | bounded evidence excerpts | investigation version | retain evidence, trim noise | 无法引用首错；旧源码会冲突 |
| 6 | History | previous unrelated read + no-progress | correlated trace / Observation | one summary + ref | current run | `KEEP_IF_RELEVANT` | 全删会重复；全留会淹没当前事实 |
| 7 | Omitted set | old Plan v1、raw 50k log、78 tools、unaccepted Result | mixed | Receipt-only entries | audit | zero model tokens | 未记录排除就无法解释选择 |
| 8 | Request controls | Provider / model、output ceiling、truncation、tool choice | application config | metadata | this request | reserve output first | 无法解释token / truncation差异 |

该顺序是课程设计；产品文档证明组件与限制存在，不证明此顺序普遍最优。

## Contributor Priority (`PROPOSAL`)

| Priority | Class | Default | Conflict rule | Scope | Trim rule |
|---:|---|---|---|---|---|
| `P0` | Provider / Host policy与request contract | required；区分application-visible / provider-managed | external text不得覆盖；未知addition保留unknown | Provider + request | 不静默裁剪 |
| `P1` | Current Goal、authoritative State、failure semantics | current revision always | current State beats Plan / history copy | Step / run | 缩表示，不删required facts |
| `P2` | Eligible Tool Schemas + policy view | only callable tools | Host registry beats stale history schema | Stage / Agent | 先删irrelevant tools |
| `P3` | Current external facts / Evidence | provenance-preserving slice | 冲突未解时保留双方并标记 | investigation / version | 去noise，保locator / hash |
| `P4` | Relevant Observation / History | selective | accepted State beats raw Result | current run / horizon | summarize + trace ref |
| `P5` | Examples / style / optional background | optional | 不覆盖P0-P3 | task / preference | 最先排除 |

Priority表示课程保留策略，不是Provider instruction hierarchy、truth rank或trust score。

## Context Receipt schema + filled sample (`PROPOSAL`)

```yaml
receipt_schema: context-receipt-course-v1
classification: COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD
step: {run_id: string, step_id: string, workflow_state_revision: string}
request:
  provider: string
  api: string
  model: string
  request_id: string|UNKNOWN
  request_contract_retrieved_at: date
  output_ceiling_tokens: integer|UNKNOWN
  truncation_policy: string|UNKNOWN
contributors:
  - contributor_id: string
    class: instruction|goal|working_state|history|capability|external_fact
    source_ref: string
    source_version: string|UNKNOWN
    authority: string
    trust: trusted|untrusted|mixed|unknown
    lifecycle: stable|dynamic
    scope: string
    priority: P0|P1|P2|P3|P4|P5
    disposition: included|summarized|excluded|conflict_retained
    order: integer|null
    content_digest: string|UNKNOWN
    reason: string
budget:
  estimator: string
  estimated_input_tokens: integer|UNKNOWN
  actual_input_tokens: integer|UNKNOWN
  output_reserve_tokens: integer|UNKNOWN
  omitted_contributor_ids: [string]
conflicts: [{conflict_id: string, contributors: [string], disposition: string}]
unknowns: [string]
provider_managed_context:
  known_present: boolean|UNKNOWN
  disclosed_description: string
  reconstructable: false
trace: {request_span_ref: string|NONE, raw_request_ref: string|NONE, response_usage_ref: string|NONE}
```

Filled sample:

```yaml
receipt_schema: context-receipt-course-v1
classification: COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD
step: {run_id: INV-12-01, step_id: diagnose-03, workflow_state_revision: rev17}
request:
  provider: ANTHROPIC_EXAMPLE_ONLY
  api: Messages-current-contract
  model: NOT_SELECTED
  request_id: UNKNOWN
  request_contract_retrieved_at: 2026-08-21
  output_ceiling_tokens: 1200
  truncation_policy: APPLICATION_FAIL_CLOSED_PROPOSAL
contributors:
  - {contributor_id: I-01, class: instruction, source_ref: prompt-contract-v3, source_version: v3, authority: application, trust: trusted, lifecycle: stable, scope: project-role, priority: P0, disposition: included, order: 1, content_digest: SHA256-DEMO-I01, reason: evidence and failure semantics}
  - {contributor_id: G-01, class: goal, source_ref: DIAGNOSE_FIRST_FAILURE, source_version: rev17, authority: workflow, trust: trusted, lifecycle: dynamic, scope: this-step, priority: P1, disposition: included, order: 2, content_digest: SHA256-DEMO-G01, reason: current goal}
  - {contributor_id: S-01, class: working_state, source_ref: state-rev17, source_version: rev17, authority: reducer, trust: mixed, lifecycle: dynamic, scope: run, priority: P1, disposition: summarized, order: 3, content_digest: SHA256-DEMO-S01, reason: accepted refs and unresolved condition}
  - {contributor_id: C-01, class: capability, source_ref: host-registry-readonly, source_version: v12, authority: host-policy, trust: trusted, lifecycle: dynamic, scope: stage-agent, priority: P2, disposition: included, order: 4, content_digest: SHA256-DEMO-C01, reason: only eligible tools}
  - {contributor_id: F-LOG, class: external_fact, source_ref: EV-LOG-017, source_version: build-4310, authority: observed-input, trust: untrusted, lifecycle: dynamic, scope: investigation, priority: P3, disposition: summarized, order: 5, content_digest: SHA256-DEMO-FLOG, reason: first compiler error}
  - {contributor_id: H-OLD, class: history, source_ref: trace-step-02, source_version: rev16, authority: trace, trust: mixed, lifecycle: dynamic, scope: current-run, priority: P4, disposition: summarized, order: 6, content_digest: SHA256-DEMO-HOLD, reason: avoid repeated no-progress action}
  - {contributor_id: T-78, class: capability, source_ref: global-tools, source_version: v12, authority: host-registry, trust: trusted, lifecycle: dynamic, scope: global, priority: P5, disposition: excluded, order: null, content_digest: SHA256-DEMO-T78, reason: irrelevant tools}
budget:
  estimator: ANTHROPIC_COUNT_TOKENS_PLANNED_NOT_CALLED
  estimated_input_tokens: UNKNOWN
  actual_input_tokens: UNKNOWN
  output_reserve_tokens: 1200
  omitted_contributor_ids: [T-78]
conflicts: []
unknowns: [no API call, no provider request id, no effective model]
provider_managed_context:
  known_present: true
  disclosed_description: Anthropic current docs say tool use adds a special system prompt
  reconstructable: false
trace: {request_span_ref: NONE, raw_request_ref: NONE, response_usage_ref: NONE}
```

`content_digest`只证明记录所指bytes一致，不证明来源可信或Provider实际使用顺序。所有hash是placeholder，token为unknown，没有runtime observation。

## Three Context Snapshots for future Lab 05

全部为`PROPOSAL / DESIGN INPUT ONLY / NOT EXECUTED`，不是Lab Design、Expected、Observation或Result。

### `SNAP-12-A / CONSISTENT_CURRENT`

```yaml
classification: PROPOSAL_DESIGN_INPUT_ONLY
snapshot_id: SNAP-12-A
step: diagnose-first-failure
state_revision: rev17
contributors:
  instruction: prompt-contract-v3
  goal: DIAGNOSE_FIRST_FAILURE@rev17
  working_state: [EV-LOG-017, EV-SRC-009, unresolved=ROOT_CAUSE]
  capabilities: [read_text@2, report_diagnosis@1]
  external_facts: [build-4310-log, source-tree-9f2a]
  history: [step-02-no-progress-summary]
conflicts: []
omissions: [78-unrelated-tools, raw-log-after-first-error]
unknowns: [actual-provider-tokenization]
future_lab_role: control candidate with current compatible sources
```

### `SNAP-12-B / STALE_STATE`

```yaml
classification: PROPOSAL_DESIGN_INPUT_ONLY
snapshot_id: SNAP-12-B
step: diagnose-first-failure
state_revision: rev14
contributors:
  instruction: prompt-contract-v3
  goal: DIAGNOSE_FIRST_FAILURE@rev17
  working_state: [EV-LOG-011, unresolved=SOURCE]
  external_facts: [build-4291-log, source-tree-9f2a]
conflicts: [goal expects rev17 while state is rev14]
omissions: [EV-LOG-017, EV-SRC-009]
unknowns: [staleness introduced by cache, session merge, or caller]
future_lab_role: stale package; test whether Receipt exposes revision mismatch
```

### `SNAP-12-C / CONFLICT_AND_BUDGET_PRESSURE`

```yaml
classification: PROPOSAL_DESIGN_INPUT_ONLY
snapshot_id: SNAP-12-C
step: diagnose-first-failure
state_revision: rev17
contributors:
  capabilities: [80-global-tool-schemas]
  external_facts: [build-4310-log, source-tree-9f2a, stale-wiki-build-4291]
  history: [full-unbounded-history]
conflicts: [build-4310 says BuildMenu.cs:42; stale wiki says LegacyBuild.cs:88]
budget_pressure:
  oversized: [80-global-tool-schemas, full-unbounded-history]
  at_risk: [current-source-excerpt, output-reserve]
omissions: []
unknowns: [which fact survives provider-side truncation]
future_lab_role: pollution / conflict package; test explicit selection and fail-closed handling
```

## Counter-evidence and boundaries

| Confusion | Counter-evidence | Boundary |
|---|---|---|
| `Context = Prompt` | tools / results consume context；conversation items may prepend；Provider may add tool-use system prompt | Prompt只是任务表达Contributor |
| `Context = Session` | 课程Session是可追踪、恢复或回放的交互与执行边界，可拥有、引用或治理history；OpenAI Agents SDK Sessions只是其产品范围内的history implementation example | Snapshot是一次Step的application-visible选择视图；Session不等于单次请求输入 |
| `Tool Result = permanent history` | client result必须在后续request回传；未来是否保留取决于application / session / continuation | Result仅在后续assembly选择 / 保留后成为Context |
| `Snapshot = Memory` | Snapshot是request-scoped selected view；Session / Store可保留更广历史 | Memory可供应Contributor，不等于Snapshot |
| `Snapshot = Checkpoint` | Article 11 recovery需要run identity、commit、in-flight、budget、continuation | Snapshot解释模型输入；Checkpoint证明恢复控制事实 |
| `Trace = complete provenance` | tracing可关闭 / 脱敏 / ZDR不可用；Provider有managed system text | Trace是观测源，不证明hidden internals或omission reason |
| `Receipt = complete effective Context` | Provider-managed system、transformations、server loop、hidden reasoning不全暴露 | Receipt只描述、审计和比较application-visible Snapshot，并声明known additions与unknown |

## Conclusion

- Claim traceability：`9 / 9`；core `BLOCKED=0`。
- Request Breakdown、Priority table、Receipt schema + sample、three snapshots均完成。
- Evidence Gate recommendation：`PASS`。后续不得把课程taxonomy、priority、Receipt、示例hash / token或snapshots升级为observed / universal facts。
