# Article 21 Research｜Trace、Replay 与 Failure Taxonomy

## Research Metadata

- Article: `21`
- Gate: `RESEARCH / EVIDENCE_GATE`
- Researcher execution: `/root/article21_researcher`
- Access date for external sources: `2026-08-26`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`
- Evidence result: `PASS CANDIDATE`

本文是 Article 21 的研究与证据边界，不是实现报告。文中的 event contract、replay 分类和 failure taxonomy 是课程级 `PROPOSAL`；OpenTelemetry、W3C Trace Context、CloudEvents、NIST、LangGraph、AWS、Azure 与 OpenAI Agents SDK 只提供可核验的原语、产品语义或反例，不被写成行业唯一标准。

## Claim Register

| Claim ID | 核心 Claim | Status | Evidence Cards |
|---|---|---:|---|
| `21-C01` | Log、Metric、Trace、Audit Record 是互补视图；任一单独存在都不证明可重放 | `PARTIAL` | `21-E01` |
| `21-C02` | 课程 Trace 必须显式区分 run / turn / step / tool-call / event identity，且不能把 provider trace/span ID 当完整业务身份 | `PROPOSAL` | `21-E02` |
| `21-C03` | 因果与顺序需要 scope 内 sequence、parent/causal link；单靠 wall-clock timestamp 不能给出完整因果关系 | `PARTIAL` | `21-E03` |
| `21-C04` | event contract 应分离所有事件共用的 base envelope 与按 `event_type` 条件必需的 causal/state/policy/approval/payload references | `PROPOSAL` | `21-E04` |
| `21-C05` | Replay、Resume、Retry、Rerun、Simulation、Projection 必须分开声明 | `PROPOSAL` | `21-E05` |
| `21-C06` | replayability 取决于事件、版本、非确定性输入、外部响应与 effect receipt 的冻结/记录边界；缺失时不得宣称确定性 | `PARTIAL` | `21-E06` |
| `21-C07` | failure occurrence、observation、recovery 是三个独立层；恢复成功不抹去原失败 | `PROPOSAL` | `21-E07` |
| `21-C08` | Failure Taxonomy 按最早有证据的 contract breach occurrence set 分类，并显式区分 `SINGLE / CO_PRIMARY / BOUNDARY / UNKNOWN` | `PROPOSAL` | `21-E08` |
| `21-C09` | Trace 应分别记录 root failure candidate、contributing factor、symptom、recovery decision/outcome 与 unknown | `PROPOSAL` | `21-E09` |
| `21-C10` | 敏感 payload、tool output 与 approval evidence 应最小化、引用化、受权访问并记录 redaction；缺失内容会限制 replayability | `PARTIAL` | `21-E10` |
| `21-C11` | Trace 只能向 Article 22 提供候选样本和 lineage；Golden Dataset、oracle、metric、threshold 与 regression verdict 仍属 Eval | `PROPOSAL` | `21-E11` |
| `21-C12` | Article 21 没有 Lab/runtime 事实，BuildPilot 只能是设计样例 | `CONFIRMED` | `21-E12` |

### Status count

- `CONFIRMED`: `1`
- `PARTIAL`: `4`
- `PROPOSAL`: `7`
- `BLOCKED`: `0`
- Core Claims with Evidence Card: `12 / 12`

## Source Register and Drift Boundary

| Source | Precise identity / version | 本文使用边界 |
|---|---|---|
| [OpenTelemetry Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/)、[Logs Data Model](https://opentelemetry.io/docs/specs/otel/logs/data-model/)、[Metrics Data Model](https://opentelemetry.io/docs/specs/otel/metrics/data-model/) | hosted OTel specification pages display `1.60.0`；accessed `2026-08-26` | 支撑 span/log/metric 原语；hosted docs 会漂移，本文未把页面与某一 git tag 做独立映射，也不把 OTel 当完整 audit/replay 标准 |
| [W3C Trace Context](https://www.w3.org/TR/2021/REC-trace-context-1-20211123/) | W3C Recommendation, `23 November 2021` | 支撑 `trace-id` / `parent-id` 传播；不支撑课程 run/turn/step/event schema |
| [Lamport, Time, Clocks, and the Ordering of Events](https://www.microsoft.com/en-us/research/publication/time-clocks-ordering-events-distributed-system/) | CACM 21(7), 1978, primary paper | 支撑 happens-before 与 partial order；不规定 Agent event contract |
| [CloudEvents core spec](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md) | pinned tag `v1.0.2` | 支撑 event identity/type/source/time/data 的 envelope precedent；不支撑 Agent 层级与 replay 语义 |
| [in-toto Resource Descriptor](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md) | pinned tag `v1.0`, Resource Descriptor | 支撑 URI/digest/content descriptor precedent；不规定 Trace payload schema |
| [LangGraph Time Travel](https://docs.langchain.com/oss/python/langgraph/use-time-travel)、[Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)、[Backward Compatibility](https://docs.langchain.com/oss/python/langgraph/backward-compatibility) | current hosted product docs；accessed `2026-08-26`；未声称 tag mapping | 证明一种产品的 replay/resume 行为与 nondeterminism caveat；不能泛化为行业语义 |
| [AWS Step Functions Redrive](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html) | current hosted AWS docs；accessed `2026-08-26` | 证明 redrive 通常保留成功步骤并从未成功步骤继续；`States.DataLimitExceeded` 下 Parallel / Inline Map / Distributed Map 会连同原先成功的 branch / iteration / child workflow 重跑；是产品语义，不是通用 replay 定义 |
| [Azure Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) | hosted Microsoft guidance, last updated `2026-03-28` | 支撑 append-only events、rehydration 与 projection distinction；是架构指导，不是标准 |
| [RFC 9110 §9.2.2](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2) | Internet Standard RFC 9110, June 2022 | 支撑 idempotent request retry 的有限边界；不证明 exactly-once 或 side-effect receipt |
| [NIST SP 800-53 Rev. 5 AU-3](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final) | NIST SP 800-53 Rev. 5 / current CSRC page notes release `5.2.0`；accessed `2026-08-26` | 支撑 audit record 的 what/when/where/source/outcome/identity 与 privacy boundary；不是 Trace schema |
| [NIST SP 800-61 Rev. 3](https://nvlpubs.nist.gov/nistpubs/specialpublications/nist.sp.800-61r3.pdf)、[SP 800-184](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-184.pdf) | NIST primary publications | 支撑 detection/analysis/recovery、root cause/recovery 概念；不提供 Agent failure taxonomy |
| [OTel sensitive-data guidance](https://opentelemetry.io/docs/security/handling-sensitive-data/) | hosted guidance, modified `2026-01-14` | 支撑 minimization/filter/hash/redaction 与 hashing limitation；实现者仍负责敏感性判定 |
| [OpenAI Agents SDK tracing](https://openai.github.io/openai-agents-python/tracing/)、[configuration](https://openai.github.io/openai-agents-python/config/) | current hosted official docs；accessed `2026-08-26`；未声称 tag mapping | 支撑一个产品的 trace content 与 sensitive-data 开关；不证明默认适合任意合规环境 |
| [NIST AI RMF 1.0](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10) | NIST AI 100-1, January 2023；NIST page notes revision in progress in 2026 | 支撑 TEVV/metric/benchmark/uncertainty 属 Eval；不规定 Golden Dataset schema |

## 1. 四种观测记录的边界

| 类型 | 首要问题 | 典型结构 | 不能单独证明 |
|---|---|---|---|
| Log | “组件在某时说了什么/报告了什么？” | discrete record、severity、body、resource、observed time | 完整因果链、状态可重建、动作确已生效 |
| Metric | “一段时间内数量/延迟/比率如何变化？” | timeseries、aggregation、dimensions | 单次 Run 的每个决策与 payload |
| Trace | “一次请求/Run 经过了哪些有关系的 operation/event？” | trace/span/event/link、parent/causal references | 审计授权充分性、payload 完整性、确定性 replay |
| Audit Record | “谁在何时何地对什么做了什么，结果怎样？” | actor/identity、action、target、time、outcome、policy/approval refs | 全部运行状态、性能分布、可再执行性 |

边界结论：四者可以共享 ID 或互相引用，但不是同义词。`Trace exists` 只证明有记录载体；只有当记录覆盖声明所需的 identity、state、versions、nondeterministic inputs 和 effects，才能进一步讨论 reconstruction 或 controlled re-execution。

## 2. Trace identity 与因果模型

### 2.1 课程级 identity（PROPOSAL）

| Identity | Scope | 角色 |
|---|---|---|
| `run_id` | 一次有终止语义的 Agent 执行 | 全局聚合边界；rerun 必须产生新 `run_id` |
| `turn_id` | Run 内一次用户/系统输入到对应控制权交还 | 对话边界；不是 provider request ID |
| `step_id` | Turn 内一次显式 Decision/Act/Observe/State transition | 顺序与恢复边界 |
| `tool_call_id` | 一次 tool invocation intent | 关联 request、receipt、result 与 retry attempt |
| `event_id` | 单个 immutable event envelope | 去重、引用与 lineage；不能被重用表达另一事件 |
| `attempt_id` | 某 step/tool-call 的一次执行尝试 | 区分 retry；原 action intent 保持可关联 |

Provider 的 `trace_id` / `span_id` 可以作为 `correlation_ids` 保存，但其 scope、生命周期与稳定性由 provider 定义，不能悄悄替代课程身份。

### 2.2 顺序与因果（PROPOSAL）

最小关系由三部分组成：

1. `sequence`：只在声明的 scope（例如同一 `run_id` + producer）内提供稳定顺序；
2. `parent_event_id`：表示直接结构父关系；
3. `caused_by[]` / `links[]`：表示跨分支、异步回调或多输入因果依赖。

`occurred_at` 表示生产者认为事件发生的时间，`observed_at` 表示收集器观察到的时间。二者都不能单独替代 causal link；并发分支可能只有 partial order。

## 3. Event Envelope 与 Payload Reference（COURSE PROPOSAL）

Base envelope 与 specialization 分开验证，不能用占位值把不存在的关系伪装成已知事实：

```yaml
base_required:
  - schema_version
  - event_id
  - event_type
  - source
  - run_id
  - sequence
  - occurred_at
  - observed_at
  - actor_ref
base_optional:
  - turn_id
  - step_id
  - parent_event_id
  - caused_by
  - correlation_ids
event_types:
  run.started:
    required: []
    forbidden: [tool_call_id, attempt_id]
  tool.result_observed:
    required: [step_id, tool_call_id, attempt_id, payload_ref]
  state.transition.recorded:
    required: [step_id, state_before_ref, state_after_ref]
  policy.decision.recorded:
    required: [policy_ref]
  approval.decision.recorded:
    required: [approval_ref]
```

`run.started` 是 root event，可以没有 `parent_event_id` 或 `caused_by`；非 Tool event 可以没有 `tool_call_id` / `attempt_id`。State、Policy、Approval 与 Payload refs 只在相应 `event_type` 合同存在时 required。若合同要求的 reference 尚未取得，事件 validation 应失败或显式记录缺口，而不是写 fabricated placeholder。Envelope 保存可索引事实与 references；大或敏感 payload、tool artifact 和 approval artifact 不必内嵌。`digest` 能支撑 identity/integrity 检查，但不能自动证明 payload 合法、真实、可访问或可安全公开；有 payload 时，`redaction.state` 必须说明 replay/review 是否受限。

## 4. Replay 家族的语义切分

| 名称 | Identity | 是否执行外部动作 | 关键声明 |
|---|---|---:|---|
| Reconstruction Replay | 保留原 `run_id`，生成新的 reconstruction session ID | 否 | 按 retained events/reducer 重建 state/view；结果受 event completeness 和 reducer version 约束 |
| Controlled Execution Replay | 原 Run 为 source；新的 replay execution ID | 可能 | 从声明边界重新执行后续步骤；必须显式处理模型、时间、随机、外部服务与 side effect |
| Resume | 同一逻辑 `run_id` 从 durable checkpoint 继续 | 可能 | 继续未完成工作；实现内部可能 replay，但语义不是“重放全部历史” |
| Retry | 同一 action intent，新 `attempt_id` | 是 | 仅在 retry eligibility、budget、authority、idempotency/effect reconciliation 允许时重试 |
| Rerun | 新 `run_id` | 是 | 从声明输入开始一次新的 Run；不承诺与原 Run 相同 |
| Simulation | 新 simulation ID，引用 source Run | 仅 fake/frozen adapter | hypothetical execution，不允许生产 side effect；输出是模拟事实 |
| Projection | projection ID + source event-set digest | 否 | 从 events 派生只读视图；不是执行，也不是恢复 |

产品术语存在反例：LangGraph 的 time travel 会跳过 checkpoint 前节点并重新执行后续节点；AWS Step Functions Redrive 通常保留成功步骤并从未成功步骤继续，但 `States.DataLimitExceeded` 下的 Parallel、Inline Map 与 Distributed Map 会连同原先成功的 branch、iteration 或 child workflow 重跑。因此本文必须给每次操作写 `mode`、`boundary`、`source_manifest` 与 `side_effect_policy`，不能只写“replay”，也不能把“成功步骤绝不重跑”当成 Redrive 保证。

## 5. Replayability Manifest 与非确定性边界

受支持的 replay 声明至少应引用：

- source event set、event schema 与 digest；
- reducer/runtime/code version；
- model/provider/model snapshot、sampling/configuration；
- policy/tool/adapter/config version；
- input/state/checkpoint version；
- time、random seed、scheduler/concurrency order；
- external request、response、timeout、rate limit 与 dependency version；
- side-effect intent、receipt、idempotency key 与 reconciliation result；
- payload availability、redaction state、access authorization；
- known gaps 与 expected equivalence level。

允许的 equivalence claim 应逐级声明，例如：`same event fold`、`same logical state`、`same normalized outcome class`。没有充分证据时，不得写 `bit-for-bit deterministic`，更不得跨 provider/version/time/environment 泛化。

## 6. Failure 的三层模型

```text
Occurrence: 哪个 contract 在哪一层首先失守？
    ↓ emits / causes
Observation: 哪个 observer 以何种 symptom 看到了它？
    ↓ triggers
Recovery: 谁依据哪条 policy 做了 retry/resume/fallback/abort，结果是什么？
```

每一层都需要独立 event identity、actor、timestamp 和 evidence reference。外层 timeout 可能只是内层 state-lock contention 的 symptom；fallback 成功是 recovery outcome，不会把原 occurrence 改写成“没有失败”。

## 7. Failure Taxonomy（COURSE PROPOSAL）

分类先求当前因果/合同偏序中的最早 breach occurrence set，再记录状态，而不是强制挑一个 owner：

- `SINGLE`：集合中只有一个最早 breach，`primary_layers` 只有一层；
- `CO_PRIMARY`：两个或更多独立 breach 都是偏序最小元素，证据充分但没有唯一 primary；
- `BOUNDARY`：breach 已有证据，但一个 owned contract 横跨多个 owner，不能诚实压成单层；
- `UNKNOWN`：证据不足以定位 occurrence set 或 owner，和“证据充分但非唯一”分开。

只有 Evidence 支撑 causal/contract ordering，证明某 breach 依赖另一个更早 breach 时，才把它降为 contributing factor 或 symptom。单纯并发或时间先后不够。

| Layer | 首先失守的 contract | 示例 symptom（不是自动 root cause） |
|---|---|---|
| Model | response validity、capability、grounding、declared model contract | malformed output、refusal、hallucinated field |
| Policy | authorization、routing、guardrail、budget/retry eligibility | denied action、wrong route、budget exhausted |
| Tool | tool schema、adapter semantics、invocation/result contract | validation error、tool exception、ambiguous effect |
| Runtime | scheduling、dispatch、correlation、checkpoint/recovery orchestration | lost callback、duplicate dispatch、deadlock |
| State | version/precondition、serialization、transition invariant、commit | stale version、corrupt checkpoint、conflict |
| Infrastructure | compute/network/storage/platform capacity owned by execution stack | process crash、disk unavailable、queue outage |
| External Dependency | third-party/provider/service contract outside owned stack | upstream 429、provider outage、changed remote state |

最小并发反例：两个并发分支分别发生 `Tool` schema breach 与 `Runtime` callback-loss breach，二者都阻止后续聚合，且没有 causal edge 能证明一个先于或导致另一个。此时应记录 `CO_PRIMARY`、两个 occurrence event 与 `primary_layers: [TOOL, RUNTIME]`；把任一层降为 factor 会发明因果从属，写 `UNKNOWN` 又会错误表达成证据不足。

建议 failure record：

```yaml
failure_id: failure_...
classification_status: SINGLE   # SINGLE / CO_PRIMARY / BOUNDARY / UNKNOWN
occurrence_event_ids: [evt_...]
primary_layers: [STATE]
failed_contract_ref: state-transition/v4
root_failure:
  status: CANDIDATE       # CONFIRMED / CANDIDATE / UNKNOWN
  code: VERSION_CONFLICT
  evidence_refs: [evidence://...]
contributing_factors:
  - factor: delayed_tool_callback
    evidence_refs: [evt_...]
symptoms:
  - observer: runtime
    code: TURN_TIMEOUT
    event_id: evt_...
recovery:
  decision_event_id: evt_...
  mode: RESUME
  outcome: SUCCEEDED_WITH_GAP
unknowns: [external_effect_status]
```

## 8. Sensitive Data、Approval Evidence 与 Redaction

安全边界采用四步：

1. 默认最小采集：能用 typed field、digest、schema ref 与 object ref 回答问题时，不保存完整 prompt/tool body；
2. 分层访问：公开 Trace 只含 references/redaction state，受限 Evidence Store 保存经授权的原文；
3. approval 绑定：Trace 保存 `approval_request_id`、`approval_decision_id`、decider identity、scope、expires/consumed state 的 reference，而不是仅写 `approved=true`；
4. 显式降级：payload 被删除、不可访问或 redact 后，标出 diagnostics/reconstruction/controlled replay 哪些能力失效。

哈希不是自动匿名化；低熵或可枚举敏感值仍可能被猜测。exception message、stack trace、model/tool data 同样可能带敏感信息。

## 9. Article 22 输入边界

Article 21 可输出给 Article 22 的只是候选输入：

- normalized event/failure slices；
- input/output/effect references 与 provenance；
- model/policy/tool/runtime/state version manifest；
- redaction/access constraints；
- recovery outcome、unknowns 与 source trace digest。

Article 21 不决定：样本是否进入 Golden Dataset、label/oracle 是否正确、train/test/regression split、metric、threshold、baseline、pass/fail verdict。Trace 是 Eval 的 data lineage source，不是 Eval verdict。

## 10. BuildPilot Design Sample

BuildPilot 只用作命名演示：一个 `tool.result_observed` 事件可以引用 tool-call、state-before/after、approval decision 与 payload digest；一个 `STATE` failure 可以被 runtime 观察为 timeout，再由 `RESUME` recovery 结束。此处没有实现、运行、实验、生产数据或收益结论。

- BuildPilot status: `DESIGN / NOT IMPLEMENTED / NOT RUN`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`

## 11. Counter-evidence and Claim Downgrade

- OTel 提供的 trace/log/metric 字段并不包含完整 Agent replay manifest 或 audit authorization，因此 `21-C01`、`21-C03` 仅为 `PARTIAL`。
- CloudEvents 的 envelope 很小且通用；把课程字段写成“CloudEvents 要求”会越证据，故 `21-C04` 是 `PROPOSAL`。
- LangGraph replay、AWS redrive 与 Azure projection 的产品语义不同，反而证明统一使用“replay”会丢失边界，故 `21-C05` 是课程 proposal。
- 保存同一 prompt 不会冻结 model weights、provider behavior、time、randomness、tool/environment state 或 external effects；`21-C06` 不升级为 deterministic claim。
- OTel/OpenAI 的敏感数据开关与处理建议不能证明合规，也不能证明 redacted payload 仍可 replay；`21-C10` 保持 `PARTIAL`。
- NIST incident guidance 不定义 Agent 的七层 taxonomy；`21-C07` 至 `21-C09` 均保持 `PROPOSAL`。
- NIST AI RMF 支撑 Eval 需要 method/metric/benchmark，但不定义本课程 Golden Dataset；`21-C11` 保持 `PROPOSAL`。

## 12. Evidence Gate Decision

- Source identity/version/access boundary present: `YES`
- Core Claim coverage: `12 / 12`
- Evidence Cards: `12`
- `BLOCKED` core Claims: `0`
- Product-specific terms generalized as universal standard: `NO`
- Proposal presented as implementation/runtime fact: `NO`
- Article 22 Eval work performed: `NO`
- Required Lab/experiment/runtime boundary preserved: `YES`
- Research gate recommendation: `PASS`
- Next allowed gate: `OUTLINE`
