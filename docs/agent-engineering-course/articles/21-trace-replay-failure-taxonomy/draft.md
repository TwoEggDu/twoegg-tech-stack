# Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层

> 如果这篇只记一句话：`Trace 的价值不是把日志存得更多，而是用稳定身份、因果关系和副作用边界，保存“哪个合同先失守、谁看见了什么、怎样恢复、还不知道什么”。`

Article 20 最后给 Budget Record 留下了一个 `trace_ref`。这个引用很重要，却也容易制造错觉：只要 Budget、Provider、Tool 和 Runtime 都写了记录，失败就已经可以定位。

假设一次 BuildPilot 调查留下了四条记录：Provider Log 报 timeout，Tool Log 报 success，State 仍停在旧 revision，恢复摘要写着 `RESUME`。四条记录可以都是真的，但如果它们没有共同的 Run、Step、Tool Call 和 Event identity，也没有因果边与 effect receipt，我们仍然不知道：timeout 是最初发生的失败，还是外层 observer 看见的 symptom；Tool success 是否已被 State 接受；`RESUME` 又是实际发生的恢复，还是一个尚未执行的候选。

> **构造示例｜COURSE DESIGN / SYNTHETIC / NOT A RUNTIME TRACE / BUILDPILOT NOT IMPLEMENTED / NOT RUN**
>
> ```text
> Provider log: timeout
> Tool log:     success
> State:        old revision
> Recovery:     RESUME
>
> missing: shared identity + causal links + state/effect receipts
> verdict: failure layer remains UNKNOWN
> ```

问题不一定是日志太少，而是记录之间缺少可关联的控制事实。`more records != attributable failure`。有记录只说明载体存在，不说明身份闭合、因果闭合、State 可重建，更不说明外部副作用可以安全重演。

本文没有 Lab、没有实验，也没有 Article 21 的运行 Trace。Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`。后文的 Event Envelope、Replay 分账、三层 Failure 模型、七层 Failure Taxonomy 与 BuildPilot 示例，均属于 **COURSE PROPOSAL / DESIGN / NOT IMPLEMENTED / NOT RUN**；它们不证明确定性 Replay、exactly-once、真实根因、分类准确率、安全合规或生产收益。

## 先把四种记录分账

“日志”“指标”“链路”“审计”经常被放进同一个 observability 后端，但存储在一起不代表责任相同。

| View | 首要回答 | 典型结构 | 可以怎样关联 | 单独不能证明 |
|---|---|---|---|---|
| Log | 某组件在某时报告了什么 | discrete record、severity、body、resource、observed time | `run_id`、span/event ref | 完整因果、State reconstruction、effect status |
| Metric | 一段时间内数量、延迟、比率怎样变化 | timeseries、aggregation、dimensions | exemplar、run/span ref | 单次 Run 的每个 Decision、payload 与历史 |
| Trace | 一次 Run / 请求经过哪些相关 operation 或 event | span、event、parent、link、causal ref | business identity + provider correlation ID | 审批充分性、payload 完整性、确定性 Replay |
| Audit Record | 谁在何时何地对什么做了什么，结果怎样 | actor、action、target、time、outcome、policy/approval ref | request/decision/event ref | 完整运行 State、性能分布、可执行 Replay |

[OpenTelemetry Trace API、Logs Data Model 与 Metrics Data Model](https://opentelemetry.io/docs/specs/otel/)分别提供 span、离散日志记录与时序聚合的结构；[NIST SP 800-53 Rev. 5 AU-3](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final)则要求 audit record 关注 event type、time、place、source、outcome 与 identity。本文据此只作有限分账：这些记录家族回答的问题不同，可以共享 ID，也可以由同一后端承载。它们并不组成一个统一 Replay 标准。

例如同一个 `TURN_TIMEOUT`：Metric 中只是一次计数；Log 中是一条 Runtime message；Trace 中是某 observer 产生的 event；Audit Record 中可能只是操作员后来选择 Stop 的对象。任何一项单独出现，都不能直接给出 root cause。

这里的证据上限是 `PARTIAL`。OpenTelemetry hosted specification 页面在本文取证时显示 `1.60.0`，访问日为 `2026-08-26`，页面与某一 Git tag 的对应关系未独立固定，后续可能漂移；NIST AU-3 是审计控制，不是 Agent Trace schema。本文也不声称四分法穷尽所有行业词汇。

**四类记录是互补视图，不是四个可以互换的“可观测性开关”。**

## 从“有记录”到“这是同一次执行”

Provider `trace_id` 能把跨服务请求串起来，却不天然知道 Agent 的 Goal、Turn、Step、action intent 与 Retry attempt。Provider 决定其 ID 的 scope 和生命周期；Agent Runtime 则要为自己的业务与控制边界负责。

下面是本文采用的身份模型。

> **Identity hierarchy｜COURSE PROPOSAL / NOT A W3C OR OTEL REQUIREMENT / NOT IMPLEMENTED**

| Identity | Scope | 生命周期规则 | 不能被什么替代 |
|---|---|---|---|
| `run_id` | 一次有 terminal semantics 的 Agent execution | Rerun 必须产生新 `run_id` | provider request/trace ID |
| `turn_id` | Run 内一次外部输入到控制权交还 | 不承担 universal loop counter | SDK `max_turn` 或 graph super-step |
| `step_id` | 一次 committed Decision / Act / Observe / State transition | 绑定 state-before / state-after | Tool Call ID |
| `tool_call_id` | 一次 Tool action intent | 关联 request、receipt、result 与 attempts | Retry attempt ID |
| `attempt_id` | 同一 intent 的一次执行尝试 | Retry 产生新 attempt，保留原 intent 关联 | 新 Run identity |
| `event_id` | 单个 immutable Event Envelope | 不复用表达另一个 event | timestamp 或 sequence |

```text
Run
└─ Turn
   ├─ Step
   │  └─ Tool call intent
   │     ├─ Attempt A
   │     └─ Attempt B          # Retry
   └─ immutable events         # attach by refs, not by timestamp guesses
```

[W3C Trace Context Recommendation（2021-11-23）](https://www.w3.org/TR/2021/REC-trace-context-1-20211123/)定义了 `trace-id`、`parent-id` 与传播字段；OpenTelemetry 也提供 SpanContext、parent 与 link。这些是 correlation 原语，不是 `run / turn / step / tool-call / attempt / event` 层级的来源。因此 Provider `trace_id`、`span_id`、`request_id` 在课程模型中只进入 `correlation_ids`，不能静默覆盖 Runtime 自己的 identity。

这个模型也接回三篇前置文章。Article 06 的 same invocation ID / same digest 只证明 fixed single-process Tool seam；Article 08 的 Step 是课程 committed iteration；Article 11 的 Resume 需要 same logical Run 与 durable continuation boundary。它们可以被 Trace 关联，却不能互相冒充身份。

Retry 与 Rerun 是最直接的检查题：Retry 保留同一 action intent / `tool_call_id`，产生新的 `attempt_id`；Rerun 从声明输入开启一份新 Run，产生新的 `run_id`。如果一个实现无法说清这两个 ID 怎样变化，它的“重试记录”和“重新运行记录”就无法可靠分开。

**Provider correlation identity 可以串服务，但不能替 Agent 的 Run、Step、action intent 与 attempt 定义生命周期。**

## 时间顺序不是因果顺序

把全部记录按 wall-clock timestamp 排序，是调查的常用起点，却不是因果证明。不同 producer 的时钟可能偏移，collector 可能延迟接收，异步 callback 也可能晚于后续分支才被观察到。并发系统里，很多事件只有 partial order，没有天然的全局总序。

[Lamport 1978 年论文《Time, Clocks, and the Ordering of Events in a Distributed System》](https://www.microsoft.com/en-us/research/publication/time-clocks-ordering-events-distributed-system/)建立了 happens-before 与 partial order；OpenTelemetry Logs 区分 event timestamp 与 observed timestamp，Trace API 也保留 parent/link。它们支持“时间与因果是两件事”这一窄结论，不规定本文的字段名。

本文采用三种关系和两种时间。

> **Causal model｜COURSE PROPOSAL / NO GLOBAL TOTAL-ORDER SERVICE**

1. `sequence.scope + value`：只在声明的 scope，例如 `run_id + producer_id` 内给出稳定顺序。
2. `parent_event_id`：表达直接结构父关系，例如某 Step 下产生的 Tool intent。
3. `caused_by[] / links[]`：表达跨分支、异步 callback 或多输入聚合的因果依赖。
4. `occurred_at`：producer 认为事件发生的时间。
5. `observed_at`：collector 实际观察到事件的时间。

```text
Step starts
  ├─ Tool A requested ────────> A result arrives late ──┐
  └─ Tool B requested -> B result observed ─────────────┤
                                                        v
                                              State commit

timestamp order: B observed, State collector sees A, ...
causal order:    State commit depends on A and B
```

`occurred_at` 早于另一事件，最多说明某个时钟下的先后观察；没有 producer order、parent/link 或其他 Evidence，就不能自动补一条 `caused_by`。同理，collector 更晚看见一个 event，也不能把它改写成更晚发生。

本文不实现 logical clock、vector clock、distributed ordering service 或 global total order。`sequence != timestamp != causality`。

**时间告诉你“看起来先后如何”，因果边才告诉你“这个事件依赖了什么”。**

## Event contract：先分 Base Envelope，再按类型要求字段

Event Envelope 的第一职责，是让一条记录可以被定位、关联和判断边界；它不应默认把完整 prompt、Tool output、stack trace 和 approval 内容复制进一行。

下面是课程提出的 requiredness matrix。字段组参考了 [CloudEvents v1.0.2 core specification](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md) 的 `id/source/type/time/data` envelope 与 [in-toto Resource Descriptor v1.0](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md) 的 URI/digest/content descriptor，但完整 schema 不是 CloudEvents 或 in-toto 的要求，也不声称与它们兼容。[OpenTelemetry Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/#span)也明确允许没有 parent 的 root span；它不要求每条记录都有 parent 或 link。

> **Event contract｜COURSE PROPOSAL / SYNTHETIC SHAPE / NOT IMPLEMENTED**

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

这份 matrix 分开了两类约束：Base Envelope 让所有 Event 都能被定位；specialization 再按 `event_type` 声明条件必需字段。`run.started` 是 root event，可以没有 `parent_event_id` 或 `caused_by`；非 Tool event 可以没有 `tool_call_id` / `attempt_id`。State、Policy、Approval 与 Payload refs 只在相应事件合同存在时 required。若合同要求的 reference 尚未取得，就让 validation 失败或显式记录 gap，不能填一个 fabricated placeholder 假装真实关系存在。

可以把它理解成两层：

```text
thin Base Envelope + event-type specialization
  identity + type/source + scoped order + conditional causality/control refs
        |
        +---- payload_ref / evidence_ref ----> restricted payload store
                                                  |
                                                  +-> access / deletion / redaction gap
```

Envelope 保存可索引的事实与 references；有 payload 的事件才应用 payload descriptor / redaction contract，大对象、敏感 payload、Tool artifact 与 Approval Evidence 进入受权存储。`digest` 在算法和使用方式正确时可以帮助检查 content identity / integrity，却不证明内容真实、适用、可访问、已授权或可公开。

这里还没有 storage、immutability、retention、schema registry 或 access-control implementation。真正落地时，每个 implementation 还要证明 Event 是否不可变、payload 是否可取得、digest 怎样验证，以及 schema 演进如何处理；字段写进 YAML 不会自动关闭这些问题。

**Event Envelope 应保存关系和引用，不应把所有原文复制到默认可见的 Trace 中。**

## Replay 不是一个动作

团队说“把这次 Run replay 一遍”时，可能想做至少七件不同的事：重建视图、重新执行、继续未完成工作、重试一个 action、从头再跑、在假环境里模拟，或生成只读投影。只用一个 `replay=true`，会同时丢失 identity、boundary 和 side-effect policy。

> **Replay family｜COURSE PROPOSAL / NOT AN INDUSTRY STANDARD / NO REPLAY ENGINE**

| Mode | Identity relation | 外部动作 | 主要声明 | 必须保存的 metadata |
|---|---|---:|---|---|
| Reconstruction Replay | 原 `run_id` + 新 reconstruction session | No | fold retained events，重建 State / view | source event set + reducer/schema version |
| Controlled Execution Replay | source Run + 新 replay execution ID | Maybe | 从声明 boundary 重新执行后续步骤 | boundary + adapter/version + side-effect policy |
| Resume | same logical `run_id` from checkpoint | Maybe | 继续未完成工作 | checkpoint + continuation + revalidation |
| Retry | same action intent + new `attempt_id` | Yes | 再次尝试同一 intent | eligibility + budget + authority + effect knowledge |
| Rerun | new `run_id` | Yes | 从声明输入开启新 Run | source input refs + new identity |
| Simulation | new simulation ID | fake/frozen only | hypothetical behavior | fake/frozen adapters + no production effects |
| Projection | projection ID + source-set digest | No | 从 events 派生只读视图 | projection version + source digest |

产品语义本身就是反例。[LangGraph Time Travel](https://docs.langchain.com/oss/python/langgraph/use-time-travel)会跳过 checkpoint 之前的节点，并重新执行之后的节点；[AWS Step Functions Redrive](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html)通常保留成功步骤并从未成功步骤继续，但它有 state-specific exception：若 `Parallel`、Inline Map 或 Distributed Map 因 `States.DataLimitExceeded` 失败，Redrive 会重跑整个相关 state，包括原先成功的 branch、iteration 或 child workflow；[Azure Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)又把 event rehydration 与 read-model projection 分开。这些 hosted 文档均按 `2026-08-26` 访问时的产品/指导语义使用，未绑定构建 tag，后续可能漂移，不能外推为统一行业定义，也不能据此承诺“成功工作绝不重跑”或 exactly-once。

因此，每次操作至少要声明：

```text
mode
+ source identity / source manifest
+ start boundary
+ new-or-same execution identity
+ side-effect policy
+ expected equivalence
```

“把同一 prompt 再发一次”最多是 Rerun 或 Controlled Execution Replay candidate。模型、Provider、时间和外部环境都可能变化，它不是 deterministic replay。Resume 也不等于“没有任何重执行”：Article 11 已经展示过，Resume 从 durable boundary 继续，boundary 之后哪些工作会再次发生取决于具体 Runtime 与产品合同。

**不写 mode、boundary 与 side-effect policy 的 Replay 声明，工程上等于没有说明要做什么。**

## Replayability 是一份 Manifest，不是一句能力标签

保存相同 prompt 和 checkpoint，仍没有冻结模型版本、采样、时间、随机、调度顺序、外部响应、Tool effect 与 payload 可访问性。能否重建、能否受控再执行、能否得到某种等价结果，需要按缺口逐项声明。

[LangGraph Backward Compatibility](https://docs.langchain.com/oss/python/langgraph/backward-compatibility)与 Time Travel 文档明确提醒 replay 可能重新执行 LLM、API、interrupt 等非确定性工作，并受 task/interrupt 顺序、时间、随机与网络影响。[RFC 9110 §9.2.2](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2)只在 HTTP method 语义范围内讨论 idempotent request 的自动 Retry 边界，也没有提供 exactly-once effect 保证。这支持“缺输入时不能宣称确定性”的有限判断，不证明下面 Manifest 已经充分。

| Nondeterministic / versioned input | 应冻结或记录什么 | 缺失后的诚实结论 |
|---|---|---|
| source events | event-set identity、schema、digest、known gaps | 无法证明 fold 输入相同 |
| reducer/runtime/code | exact version / artifact identity | 只能比较输出，不能归因 reducer 一致 |
| model/provider | provider、model snapshot、sampling/config | 不得承诺相同生成结果 |
| policy/tool/adapter/config | version、decision/effect contract | 无法证明执行边界相同 |
| input/state/checkpoint | identity、revision、integrity、continuation | 无法证明从同一控制位置开始 |
| time/random/scheduler | clock input、seed、concurrency order | 只能声明未冻结 nondeterminism |
| external I/O | request/response、timeout、rate limit、dependency version | 不能把远端当前状态当历史响应 |
| side effect | intent、receipt、idempotency key、reconciliation result | effect unknown 时不得盲目重执行 |
| payload/redaction/access | availability、redaction state、authorization | reconstruction / diagnosis / execution replay 受限 |

等价级别也要逐级写：`same event fold`、`same logical state`、`same normalized outcome class` 各自需要不同 Evidence。`bit-for-bit deterministic` 只有在明确 frozen scope 与真实验证下才可使用；本文实验数为 `0`，不作这个声明，也不跨 Provider、版本、时间或环境泛化。

副作用边界尤其不能省略：

```text
effect status
  ├─ KNOWN_ABSENT --------------------------> eligible action may execute
  ├─ KNOWN_PRESENT + QUERYABLE --------------> lookup / reconcile first
  └─ UNKNOWN --------------------------------> reconcile / ask / stop

Budget remains does not change this classification.
Approval exists does not prove effect status.
```

Reconstruction Replay 与 Projection 默认不执行生产副作用。Controlled Execution Replay、Resume 与 Retry 如果遇到 unknown effect，要先 lookup / reconcile；稳定 identity 只是查询与 same-intent replay 的 seam，不是 exactly-once。Article 20 的 Budget 也只限制已经合格的工作，不能为 Retry 补资格。

**Replayability 是一份有版本、有缺口、有等价级别的声明，不是“我们保存了 prompt”这一事实。**

## Failure 要分发生、观察与恢复

一个 Runtime 最终 fallback 成功，不会让原先的 failure 消失；一个外层 timeout 也不必然是第一处失守的 contract。为了避免后来的 observation 或 recovery 覆盖原事件，本文把 failure 拆成三层。

> **Failure layers｜COURSE PROPOSAL / ADAPTED FROM INCIDENT CONCERNS / NOT A NIST EVENT MODEL**

```text
Occurrence
  哪个 contract 在哪一层首先失守？
      |
      v emits / causes
Observation
  哪个 observer 在何时以什么 symptom 看见它？
      |
      v triggers
Recovery
  谁依据哪条 policy 选择 Retry / Resume / Fallback / Abort，结果怎样？
```

[NIST SP 800-61 Rev. 3](https://nvlpubs.nist.gov/nistpubs/specialpublications/nist.sp.800-61r3.pdf)与 [NIST SP 800-184](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-184.pdf)把 detection/analysis、root-cause learning 与 recovery 作为不同关注点。它们不规定 Agent 的三层 Event 模型；上面的 identity、actor、timestamp 和 Evidence ref 分账是课程 Proposal。

三层分别保存，才能表达这些真实可能性：

- Observation 可以迟到、缺失或在外层出现。
- 同一个 timeout 既可能是 deadline contract 的 occurrence，也可能只是更内层 failure 的 symptom。
- Recovery success 只说明系统后来做了什么，不反向删除 occurrence。
- Recovery failure 也不能自动证明最初的 root candidate 错误。

> **构造链｜SYNTHETIC / NOT A RUNTIME OBSERVATION**
>
> ```text
> State version precondition rejected        # occurrence candidate
>     -> Runtime later reports TURN_TIMEOUT  # observer symptom
>     -> Recovery proposes RESUME             # decision candidate, NOT RUN
> ```

如果只保留最后一个 `RESUME_SUCCEEDED`，调查者会失去最初的 State conflict；如果只保留 `TURN_TIMEOUT`，又会把 observer 看到的现象误当成 causal root。

**恢复成功说明系统后来做了什么，不说明原先的失败从未发生。**

## 七层 Failure Taxonomy：先保存最早 breach set，再谈 owner

下面的七层不是技术栈分层图，也不是把所有错误硬塞进一个唯一盒子。它服务的问题是：在当前 Evidence 下，最早失守的 contract occurrence set 是什么，它是否真的只有一个 owner？

> **COURSE PROPOSAL / NOT AN INDUSTRY STANDARD / NOT EXHAUSTIVE / NOT MUTUALLY EXCLUSIVE / NOT VALIDATED AGAINST A FAILURE CORPUS**

| Primary layer candidate | 首先可能失守的 owned contract | 例子只表示 symptom candidate | 禁止的推断 |
|---|---|---|---|
| Model | response validity、capability、grounding、declared model contract | malformed output、refusal、hallucinated field | 不把所有 bad result 都归模型 |
| Policy | authorization、routing、guardrail、budget/retry eligibility | denied action、wrong route、budget exhausted | deny 可能是正确 control outcome |
| Tool | schema、adapter、invocation、result、effect contract | validation error、Tool exception、ambiguous effect | outer timeout 不自动归 Tool |
| Runtime | scheduling、dispatch、correlation、checkpoint/recovery orchestration | lost callback、duplicate dispatch、deadlock | Provider error 不自动归 Runtime |
| State | version/precondition、serialization、invariant、commit | stale version、corrupt checkpoint、conflict | history changed 不等于 State committed |
| Infrastructure | owned compute、network、storage、platform capacity | process crash、disk unavailable、queue outage | 第三方 outage 不归 owned infra |
| External Dependency | Provider、third-party、outside service contract | upstream 429、outage、remote state changed | upstream message 不自动给 root cause |

分类步骤比层名更重要：

1. 先列 component / contract owner map。
2. 列出 Evidence 已支持的 breach occurrences，并在 causal / contract partial order 中求最早 occurrence set。
3. 用 `classification_status` 显式记录 `SINGLE / CO_PRIMARY / BOUNDARY / UNKNOWN`，并保存 `occurrence_event_ids[]` 与 `primary_layers[]`。
4. `SINGLE` 表示只有一个最早 breach；`CO_PRIMARY` 表示两个或更多独立 breach 同为偏序最小元素；`BOUNDARY` 表示 breach 已有证据，但一个 owned contract 横跨多个 owner；`UNKNOWN` 只表示证据不足。
5. 只有 Evidence 支撑 causal / contract ordering，证明某 breach 依赖另一更早 breach 时，才把它降为 contributing factor 或 symptom。仅凭时间先后或并发出现不够。

例如 upstream 返回 429。若当前 Evidence 只证明第三方 rate-limit contract 拒绝请求，occurrence candidate 可以是 `External Dependency`；Runtime 的 turn timeout 是 symptom；Retry Budget 后来耗尽，则是 Recovery/Policy terminal。不能把三者并列成三个 root cause，也不能因为最后看到 `BUDGET_EXHAUSTED` 就把最初 occurrence 改成 Policy failure。

再看一个最小并发反例：两个并发分支分别发生 `Tool` schema breach 与 `Runtime` callback-loss breach，二者都阻止后续聚合，且没有 causal edge 证明一个先于或导致另一个。这里 Evidence 并不缺；正确记录是 `classification_status: CO_PRIMARY`、两个 occurrence events 与 `primary_layers: [TOOL, RUNTIME]`。任选一层并把另一层降为 factor，会发明不存在的因果从属；写 `UNKNOWN`，又会把“证据充分但非唯一”误写成“证据不足”。

Policy deny、Budget exhaustion 与 Cancellation 也不应被默认标为 failure。它们可能正是 declared terminal contract 的正确结果。分类前必须先问：系统承诺的 contract 是什么，谁拥有它，这次是否真的违反。

本文没有 failure corpus，不能证明七层的覆盖率、精度、互斥性或 operational value。后续 Eval 即使要检查，也必须另行冻结 workload、label/oracle 与判据。

**Failure classification 应保存最早 breach occurrence set：证据充分但非唯一时写 `CO_PRIMARY / BOUNDARY`，证据不足才写 `UNKNOWN`。**

## Failure Record：不要把 symptom 改名成 root cause

外层 exception 很有价值，但它首先是一条 observation。OpenTelemetry 的 [Exception recording](https://opentelemetry.io/docs/specs/otel/trace/exceptions/)与 [HTTP exception semantic conventions](https://opentelemetry.io/docs/specs/semconv/http/http-exceptions/)提供 exception type/message/stack/status 的记录原语；在本文取证时，相关 hosted semantic-conventions 页面显示 `1.44.0`，访问日为 `2026-08-26`，后续可能漂移。它们没有把 exception event 定义为 root cause。

因此，一条 failure record 至少应让 root candidate、factor、symptom、recovery 与 unknown 分账。

> **Failure Record｜COURSE PROPOSAL / SYNTHETIC / NOT A PRODUCT SCHEMA / NOT RUN**

```yaml
failure_id: REQUIRED_NOT_CREATED
classification_status: SINGLE      # SINGLE | CO_PRIMARY | BOUNDARY | UNKNOWN
occurrence_event_ids: [REQUIRED_NOT_CREATED]
primary_layers: [STATE]             # constructed classification only
failed_contract_ref: state-transition/v-design
root_failure:
  status: CANDIDATE                # CONFIRMED | CANDIDATE | UNKNOWN
  code: VERSION_PRECONDITION_REJECTED
  evidence_refs: [SYNTHETIC_DESIGN_REF]
contributing_factors:
  - factor: delayed_callback_candidate
    evidence_refs: [SYNTHETIC_DESIGN_REF]
symptoms:
  - observer: runtime-design
    code: TURN_TIMEOUT
    event_id: REQUIRED_NOT_CREATED
recovery:
  decision_event_id: REQUIRED_NOT_CREATED
  mode: RESUME
  outcome: NOT_RUN
unknowns:
  - external_effect_status
  - payload_accessibility
```

默认使用 `CANDIDATE` 或 `UNKNOWN`。只有独立 Evidence 关闭了关键替代解释，才有资格升级为 `CONFIRMED`。新 Evidence 到来时，也应追加 revision / relationship，不要覆盖当时真正观察到的 symptom。

这与 Article 18 的 Evidence Contract 保持一致：Trace / Failure Record 可以成为 Evidence source，但 event 存在、字段完整或 message 清晰，都不会自动完成 Claim acceptance。`root_failure.status` 表达当前可审查判断强度，不是永不变化的真值。

**Trace 要保存“当前知道到哪里”，而不是把最外层 symptom 改名成 root cause。**

## Redaction：让缺失与代价可见

保存完整 prompt、Tool output、Approval context 与 stack trace，会扩大敏感信息的复制面；完全不保存，又可能让 reconstruction、诊断与受控再执行失去关键输入。工程问题不是在“全收”与“全删”之间选一个口号，而是让 disclosure 与能力损失都可审计。

[OpenTelemetry Handling Sensitive Data](https://opentelemetry.io/docs/security/handling-sensitive-data/)建议 minimization、filtering、hashing 与 redaction，并提醒 hashing 的限制；该 hosted 页面在本文取证时标注 modified `2026-01-14`，访问日为 `2026-08-26`。[OpenAI Agents SDK tracing](https://openai.github.io/openai-agents-python/tracing/)与 [configuration](https://openai.github.io/openai-agents-python/config/)展示当前产品的 trace content 与 sensitive-data controls；同样按 `2026-08-26` hosted docs 使用，未绑定构建 tag，后续可能漂移。它们都不证明一个开关就能满足合规，也不证明 redacted Trace 仍可 Replay。

本文采用四步边界：

1. **Minimize**：能用 typed field、digest、schema/object ref 回答的问题，不默认保存 raw body。
2. **Reference**：常规 Trace 只保留 payload/evidence refs 与 redaction state；restricted store 保存经授权原文。
3. **Bind approval evidence**：保存 Approval request/decision refs、decider identity、scope、expiry、consumed/revocation state，不只写 `approved=true`。
4. **Degrade explicitly**：payload 被删除、不可访问或 redact 后，明确 diagnostics、reconstruction、controlled replay 哪些能力失效。

> **Redaction record｜COURSE PROPOSAL / NOT A COMPLIANCE SCHEMA**

| Field | 作用 | 缺失时不能推断 |
|---|---|---|
| `state = NONE \| PARTIAL \| FULL \| UNAVAILABLE` | 表达 disclosure 状态 | payload 完整 |
| `policy_ref`、reason、actor/time refs | 解释按什么规则处理 | redaction 合法或充分 |
| affected fields | 定位哪些输入不可见 | 其他字段无敏感信息 |
| original payload ref/digest/access state | 判断是否仍可受权复核 | hash 可恢复原文 |
| replay impact | 下调 reconstruction / execution capability | Trace 仍完全 replayable |

Hash 不等于匿名化。低熵或可枚举的 secret、邮箱、ID 仍可能被猜测；digest 也不会授予 payload 访问权。Exception、stack trace、模型输入输出、Tool body 和 Approval reason 都可能包含敏感数据。

如果 Tool output 已 redacted，但 envelope 仍保留 digest/schema/ref，Replayability Manifest 应写 `controlled_execution_replay=LIMITED` 或更窄的本地状态，而不是继续宣称完整。诚实的 redaction 不是把内容删掉后假装一切不变，而是把缺口及其代价保留下来。

**Redaction 不是把内容删掉后继续宣称 Trace 完整，而是让缺失内容及其诊断/Replay 代价可见。**

## BuildPilot：把全部合同走一遍

> **BUILDPILOT COURSE DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN / NOT A RUNTIME TRACE**

下面只设计一条只读调查链：目标是一份“精确身份尚需固定”的 build result。它不会触发 Jenkins、Unity、Provider、filesystem write 或发布。Article 18 的 `evidence_ref`、Article 19 的 `approval_ref`、Article 20 的 `budget_ref` 都只保留为 `REQUIRED / NOT_CREATED`，不伪造 accepted、approved 或 admitted 事实。

```text
DESIGN EVENT: run.started
  -> binds proposed Run / Turn scope

DESIGN EVENT: tool.call.requested
  -> binds action intent, attempt, state-before

DESIGN EVENT: tool.result_observed
  -> refers to payload digest + redaction state

DESIGN EVENT: state.commit.rejected
  -> constructed version/precondition conflict candidate

DESIGN EVENT: runtime symptom observed
  -> TURN_TIMEOUT, caused-by remains explicit

DESIGN EVENT: recovery decision candidate
  -> RESUME / outcome=NOT_RUN
```

关键 refs 可以压缩成下面这份 shape：

> **SYNTHETIC DESIGN SHAPE / NO REAL IDS OR TIMESTAMPS**

```yaml
run_identity: REQUIRED_NOT_CREATED
tool_call:
  identity: REQUIRED_NOT_CREATED
  intent_digest: REQUIRED_NOT_COMPUTED
  attempt: REQUIRED_NOT_CREATED
  authority_ref: REQUIRED_FROM_ARTICLE_19_NOT_CREATED
  budget_ref: REQUIRED_FROM_ARTICLE_20_NOT_CREATED
observation:
  payload_ref: REQUIRED_NOT_ACQUIRED
  redaction_state: UNAVAILABLE
state_transition:
  contract_ref: state-transition/v-design
  result: REJECTED_CONSTRUCTED
failure:
  classification_status: SINGLE
  occurrence_event_ids: REQUIRED_NOT_CREATED
  primary_layers: [STATE]
  root_status: CANDIDATE
  symptom: TURN_TIMEOUT_CONSTRUCTED
  contributing_factor: DELAYED_CALLBACK_CANDIDATE
  effect_status: UNKNOWN
recovery:
  mode: RESUME_CANDIDATE
  outcome: NOT_RUN
```

这个 scenario 故意指定 `state-transition/v-design` 的 version precondition 是唯一最早 breach，所以练习中的 `classification_status: SINGLE` 与 `primary_layers: [STATE]` 只是 constructed candidates。它们不是 BuildPilot 真实 root cause。Delayed callback 只有在 causal/contract ordering 得到支持时才有资格成为 contributing factor candidate；`TURN_TIMEOUT` 是 observer symptom；payload access 与 effect status 继续保持 `UNKNOWN`。

评审时依次检查：

1. 每个 Event 是否有 immutable identity 与明确 scope？当前答案是 `REQUIRED_NOT_CREATED`，所以没有运行事实。
2. timestamp 之外是否有 causal links？设计要求有，当前没有 observation。
3. payload/redaction 是否限制 reconstruction 或 controlled replay？是，当前状态为 `UNAVAILABLE / LIMITED`。
4. 需要的是 Reconstruction、Resume 还是 Retry？当前只提出 Resume candidate，没有 execution authority。
5. occurrence、observation、recovery 是否分别保存？shape 分开了，但没有 Runtime events。
6. classification status、occurrence set 与 primary layers 是否按 causal/contract ordering，而不是 timeout message 或强制单选？设计上是，但没有 corpus 或真实 Failure Evidence。
7. root status 是否保持 `CANDIDATE`，unknown 是否保留？是。

这个 walk-through 没有创建真实 ID、时间、payload、Approval、Budget receipt、Runtime event 或 failure corpus。它不证明 BuildPilot 拥有 Trace store、reducer、Replay engine、Failure classifier、redaction pipeline，也没有准确率、成本、时延、可靠性或 production benefit。

设计样例的合格结果不是“成功 Replay”，而是每个 required、candidate、unknown 与 `NOT_RUN` 都没有被伪造成运行事实。

## Trace 只把候选样本交给 Eval

结构完整的 failure trace 仍不是 Golden Dataset。它可能缺 payload、带错误的 root candidate、只覆盖一个版本，甚至把 recovery success 错当成质量提升。Trace 能提供的是 lineage，不是自我验证。

[NIST AI RMF 1.0](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10)在 MEASURE 中讨论 repeatable TEVV、metrics、methodologies、benchmarks 与 uncertainty。该 publication identity 为 NIST AI 100-1（January 2023）；NIST 页面在 2026 年标注 revision work，本文访问日为 `2026-08-26`。它支持“Eval 还需要方法、度量和基准”这一窄结论，不规定本课程 Golden Dataset schema。

| Article 21 可以交付 | Article 22 才能决定 |
|---|---|
| normalized event / failure slice | sample 是否进入 Golden Dataset |
| input/output/effect refs 与 provenance | oracle / label 是否正确、怎样 review |
| model/policy/tool/runtime/state version manifest | train/test/regression split 与 representativeness |
| redaction/access constraints | metric、threshold、baseline |
| recovery outcome、unknowns、source trace digest | pass/fail 与 regression verdict |
| candidate taxonomy label + provenance/status | taxonomy label 是否成为 gold |

`Trace slice + candidate label` 不等于 Golden sample；`Replay reproduced` 不等于 Regression PASS；`Recovery succeeded` 也不等于 quality improved。Article 22 才拥有 sample curation、oracle、metric、threshold、baseline、Eval/Regression verdict，以及 Lab 06 的 Design、Execute、Observation 与 Evidence Merge。

本文不会创建 Article 22 workspace、dataset、label、metric、threshold、Lab 06 或 regression plan，也不提前写 DSH Article 34 的 append-only Session Event 源码/运行结论。

> **冻结的验证边界**
>
> - Required Lab: `NONE`
> - Experiment Count: `0`
> - Runtime Observation: `ABSENT`
> - BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`
> - Seven-layer taxonomy: `COURSE PROPOSAL / NOT CORPUS-VALIDATED`

**有可追溯 Trace，只说明 Eval 有了候选数据来源；它不说明样本是 Golden，也不说明修复已经通过 Regression。**

## 一条 Trace / Replay 设计通常怎样写坏

| Shortcut | 被吞掉的责任 | 最小修正 |
|---|---|---|
| `more logs = root cause found` | identity、causality、contract owner | 绑定 Run/Step/Event 与 Evidence refs；unknown 保持 unknown |
| `provider trace_id = Agent run_id` | business/control lifecycle | Provider IDs 只进入 correlation IDs |
| `timestamp sort = causality` | partial order、async links | scoped sequence + parent/caused-by links |
| `trace exists = replayable` | version、nondeterminism、effect、payload access | Replayability Manifest + equivalence level |
| `same prompt again = deterministic replay` | new identity 与 environment | 归为 Rerun / Controlled Replay candidate |
| `resume = no re-execution` | checkpoint/product boundary | 声明 continuation 与 side-effect policy |
| `retry budget remains = retry allowed` | effect/eligibility + authority | 先过 Article 11/19，Budget 只限制合格工作 |
| `outer exception = root cause` | occurrence/factor/symptom | root status 保持 Candidate/Unknown + Evidence refs |
| `failure 必须只有一个 primary layer` | partial order、co-primary 与 owner boundary | 保存 occurrence set + `SINGLE/CO_PRIMARY/BOUNDARY/UNKNOWN` |
| `fallback succeeded = no failure` | recovery history | 保留 occurrence 与 recovery outcome |
| `seven layers = industry standard` | Proposal/Evidence ceiling | 明示 COURSE PROPOSAL，后续另行验证 |
| `hash = anonymous` | disclosure risk | sensitivity assessment + access/redaction metadata |
| `trace slice = Golden Dataset sample` | curation/oracle/Eval | 只交 candidate + lineage 给 Article 22 |
| `BuildPilot schema = runtime` | implementation/observation | 保留 DESIGN / NOT IMPLEMENTED / NOT RUN |

这些坏法有同一个模式：让一种记录或一个 PASS 替其他责任面作决定。修复不是再加一个 `replayable=true` 或 `root_cause=true`，而是把 identity、evidence、effect、unknown 与 owner 放回各自合同。

## 本篇能建立什么，不能证明什么

本篇可以安全建立的上限是：

- Log、Metric、Trace、Audit Record 回答不同问题，任一单独存在都不证明 Replay；这是带来源限制的 `PARTIAL` 结论。
- Provider correlation identity 与 Agent business/control identity 应显式分开；本文层级是课程 Proposal。
- timestamp、observed time 与 causal relation 不是同一概念；本文字段组合不是标准。
- Event Envelope、Replay family、Failure 三层模型、七层 taxonomy 与 Failure Record 都是可审查的课程设计。
- replayability 必须披露版本、非确定性输入、external I/O、effect receipt、payload access 与 equivalence level；本文没有 deterministic Evidence。
- Sensitive-data handling 与 redaction 会改变可诊断性和 replayability，不能用开关、hash 或删除动作冒充合规与完整性。
- 当前仓库事实确认 Article 21 Required Lab=`NONE`、Experiment=`0`、Runtime=`ABSENT`，BuildPilot 未实现、未运行。

本篇不能证明：

- deterministic 或 bit-for-bit Replay，以及跨 Provider、版本、时间、外部环境的等价；
- exactly-once、safe Retry、complete Recovery 或任何外部 effect 已发生/未发生；
- 七层 taxonomy 穷尽、互斥、已被 failure corpus 验证，或能自动定位 root cause；
- redaction、hashing、Trace 开关已经带来安全、匿名、合规或足够访问控制；
- BuildPilot 已有 Trace store、Replay engine、Failure classifier、Runtime behavior 或 production benefit；
- 任何样本已经成为 Golden Dataset，或任何修复已经通过 Eval / Regression / Lab 06。

## Claim Traceability（12 / 12）

| Claim | Evidence ceiling | 正文落点 | 保留的边界 |
|---|---|---|---|
| `21-C01` | `PARTIAL` | 四种记录分账、反模式 | signals 互补；任一单独不证明 Replay；不称穷尽 taxonomy |
| `21-C02` | `PROPOSAL` | Identity hierarchy、BuildPilot | 课程 contract；Provider IDs 只作 correlation |
| `21-C03` | `PARTIAL` | 时间与因果 | timestamp/causality 分开；字段名与充分性不称标准 |
| `21-C04` | `PROPOSAL` | Base Envelope、event-type specialization、payload refs | root/non-Tool 可省关系字段；refs 按事件合同 required；CloudEvents/in-toto 只作先例 |
| `21-C05` | `PROPOSAL` | Replay family | 课程分账；LangGraph/AWS/Azure 语义不泛化 |
| `21-C06` | `PARTIAL` | Replayability Manifest、effect fork | 无 deterministic/bit-identical/cross-provider Claim |
| `21-C07` | `PROPOSAL` | Occurrence/Observation/Recovery | 三层为课程 Event 模型；Recovery success 不抹 occurrence |
| `21-C08` | `PROPOSAL` | 七层 Failure Taxonomy | `SINGLE/CO_PRIMARY/BOUNDARY/UNKNOWN` 分开；不称 exhaustive/validated |
| `21-C09` | `PROPOSAL` | Failure Record | root 默认 Candidate/Unknown；factor/symptom/recovery 分账 |
| `21-C10` | `PARTIAL` | Payload/ref/redaction | minimization/redaction 不证明 compliance 或 replayability |
| `21-C11` | `PROPOSAL` | Trace-to-Eval seam | 只交 candidate slices + lineage；不做 Golden/oracle/metric/verdict |
| `21-C12` | `CONFIRMED` | 开场、BuildPilot、验证边界 | Required Lab NONE；Experiment 0；Runtime ABSENT；BuildPilot design-only |

Coverage=`12 / 12`；Evidence posture=`1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED`；new core Claim=`NONE`。

## Learning Check

1. 已有 error log、failure-rate metric、distributed trace 和 approval audit record，为什么仍不能宣布 Run 可重放？
2. Provider `trace_id` 为什么不能直接充当 `run_id`？Retry 与 Rerun 的 identity 又怎样变化？
3. `occurred_at` 早于另一事件，为什么不能自动写 `caused_by`？
4. 为什么要把 Base Envelope 与 event-type specialization 分开？有 payload 时为什么放到 reference/digest 后面？
5. Reconstruction Replay、Controlled Execution Replay、Resume、Retry、Rerun、Simulation、Projection 的关键区别是什么？
6. 保存同一 prompt/checkpoint 后，为什么不能声称 deterministic Replay？
7. Fallback 成功后，Occurrence、Observation、Recovery 应怎样保留？
8. 七层 taxonomy 怎样区分 `SINGLE / CO_PRIMARY / BOUNDARY / UNKNOWN`？何时才能把另一 breach 降为 factor？
9. Root failure、contributing factor、symptom、recovery outcome 与 unknown 为什么不能塞进一个 error message？
10. Redaction 后 payload 不可访问，系统应怎样描述 replayability？
11. BuildPilot walk-through 为什么即使 shape 完整，也不能声称发现了 STATE root cause？
12. Article 21 能给 Article 22 什么，不能给什么？

### 参考答案

1. 四种记录回答不同问题；仍需 declared identity、causal/state/version inputs、nondeterministic inputs、payload access 与 effect boundary。记录存在不等于完整、真实或可执行 Replay。
2. Provider ID 的 scope/lifecycle 由 Provider 定义，只作 correlation。Retry 保留 action intent / Tool Call 关联，产生新 `attempt_id`；Rerun 产生新 `run_id`。
3. Timestamp 只提供某个时钟下的观察顺序；因果需要 producer order、parent/link 或其他 Evidence。并发系统可能只有 partial order。
4. Base Envelope 只要求所有事件共有的定位字段；Tool/State/Policy/Approval/Payload refs 按 `event_type` 条件必需，root/non-Tool event 不伪造关系。Payload 可在受权边界中存取；Digest 最多帮助检查 content identity/integrity，不证明 truth、authorization、availability、匿名或 compliance。
5. 要比较 identity relation、是否执行外部动作、continuation/source boundary、adapter 与 side-effect policy。Projection 是只读派生；Simulation 只能使用 fake/frozen adapter；Resume 也不承诺不重执行。
6. Model/Provider/version/time/random/scheduler/external I/O/effect/payload availability 仍可能变化。只有 Manifest、frozen scope 与真实验证才支持相应 equivalence；本文没有实验。
7. Occurrence 保存 first evidenced contract breach，Observation 保存 observer symptom，Recovery 保存 decision/outcome；成功的恢复不会删除原 Failure。
8. 先求最早有 Evidence 的 breach occurrence set：唯一最小元素是 `SINGLE`，多个独立最小元素是 `CO_PRIMARY`，已证实但跨 owner 的合同是 `BOUNDARY`，证据不足才是 `UNKNOWN`。只有 causal/contract ordering 有 Evidence 时才能把另一 breach 降为 factor。
9. 它们的时点、责任与 Evidence 强度不同。Root 需要 `CONFIRMED/CANDIDATE/UNKNOWN` 状态和 refs，外层 message 首先只是一条 observation。
10. 显式记录 redaction/deletion/access gap，并下调 diagnostics、reconstruction 与 controlled replay 能力；不能假装 hash 可恢复原文或自动匿名。
11. Scenario 是 synthetic course design，`STATE` 与 root status 都只是 constructed candidate。Required Lab NONE、Runtime ABSENT、BuildPilot NOT RUN。
12. 可以交 candidate slices、lineage、version/redaction/effect/unknown refs；不能决定 Golden acceptance、oracle/label、metric/threshold、baseline、Regression verdict 或 Lab 06 结果。

## Job Competency

| 能力 | 可观察产物 | 达标表现 | 明确上限 |
|---|---|---|---|
| Observability architecture | 四视图 ledger + identity hierarchy | 能说明每类记录回答什么，并建立跨 Run/Step/Tool correlation | 不称 signals 穷尽或 backend 已实现 |
| Distributed causal reasoning | sequence/parent/caused-by + dual timestamps | 能拒绝 timestamp-as-causality，保留 partial order 与 unknown | 不实现 clock/order service |
| Event contract design | Event Envelope + payload/redaction refs | 能把 identity、control refs、payload identity 与 disclosure 分账 | COURSE PROPOSAL，非标准/Runtime |
| Reliable execution judgment | Replay family + Manifest + effect fork | 能区分 reconstruction、re-execution、Resume/Retry/Rerun，并声明 equivalence | 不作 deterministic/exactly-once Claim |
| Failure analysis | 三层模型 + 七层 taxonomy + Failure Record | 能保存 earliest occurrence set，区分 single/co-primary/boundary/unknown 与 factor/symptom/recovery | taxonomy 未验证，无 RCA accuracy |
| Security/privacy boundary reasoning | minimization/reference/access/redaction | 能说明数据缺失怎样限制 diagnosis/Replay，不把 hash 叫匿名化 | 无 compliance/security guarantee |
| Cross-system architecture | Articles 06/08/11/18/19/20/22 seams | 能让 Tool、Loop、Recovery、Evidence、Authority、Budget、Eval 各守 owner | repository-local course model |
| Design communication | BuildPilot synthetic sequence | 能在具体案例中暴露 required/unknown/candidate，不伪造 Runtime | NOT IMPLEMENTED / NOT RUN |
| Evaluation readiness | Trace-to-Eval handoff | 能交 candidate sample + lineage，同时拒绝自封 Golden/Regression PASS | Article 22/Lab 06 不在本篇 |

## 参考资料

### Primary / normative anchors

- [OpenTelemetry Specification / Trace API / Logs / Metrics](https://opentelemetry.io/docs/specs/otel/)（hosted pages 显示 `1.60.0`；访问日 `2026-08-26`；未独立固定 page-to-tag mapping；不作为完整 Audit/Replay 标准）
- [W3C Trace Context Recommendation, 23 November 2021](https://www.w3.org/TR/2021/REC-trace-context-1-20211123/)（`trace-id` / `parent-id` correlation；不定义 Agent identity hierarchy）
- [Lamport, Time, Clocks, and the Ordering of Events in a Distributed System, CACM 21(7), 1978](https://www.microsoft.com/en-us/research/publication/time-clocks-ordering-events-distributed-system/)（happens-before / partial order；不定义 Agent Event schema）
- [CloudEvents Core Specification v1.0.2](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md)（pinned envelope precedent；课程 schema 不宣称 compliant）
- [in-toto Resource Descriptor v1.0](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md)（pinned URI/digest/content descriptor precedent；digest 不证明 truth/authorization）
- [RFC 9110 §9.2.2, June 2022](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2)（HTTP idempotent retry 的有限边界；不证明 exactly-once）
- [NIST SP 800-53 Rev. 5 / AU-3](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final)（current CSRC page notes release `5.2.0`；访问日 `2026-08-26`；不是 Agent Trace schema）
- [NIST SP 800-61 Rev. 3](https://nvlpubs.nist.gov/nistpubs/specialpublications/nist.sp.800-61r3.pdf)；[NIST SP 800-184](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-184.pdf)（detection/analysis/recovery/root-cause concerns；不定义本文三层或七层 taxonomy）
- [NIST AI RMF 1.0, NIST AI 100-1, January 2023](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10)（TEVV/metrics/benchmark/uncertainty；页面在 2026 年标注 revision work；访问日 `2026-08-26`；不定义 Golden Dataset schema）

### Product / hosted documentation used as bounded counterexamples

- [LangGraph Time Travel](https://docs.langchain.com/oss/python/langgraph/use-time-travel)、[Persistence](https://docs.langchain.com/oss/python/langgraph/persistence)、[Backward Compatibility](https://docs.langchain.com/oss/python/langgraph/backward-compatibility)（访问日 `2026-08-26`；未绑定 hosted-doc tag；只按 LangGraph 产品语义使用）
- [AWS Step Functions Redrive](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html)（访问日 `2026-08-26`；通常保留成功步骤，但 `States.DataLimitExceeded` 下 Parallel / Inline Map / Distributed Map 可重跑原先成功的 branch / iteration / child workflow；不是 generic Replay / exactly-once）
- [Azure Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)（页面 last updated `2026-03-28`；访问日 `2026-08-26`；架构指导，不是标准）
- [OpenTelemetry Handling Sensitive Data](https://opentelemetry.io/docs/security/handling-sensitive-data/)（页面 modified `2026-01-14`；访问日 `2026-08-26`；不证明 compliance/anonymity）
- [OpenAI Agents SDK tracing](https://openai.github.io/openai-agents-python/tracing/)；[configuration](https://openai.github.io/openai-agents-python/config/)（访问日 `2026-08-26`；未绑定 hosted-doc tag；产品 trace/sensitive-data controls，不证明安全默认值或 Replay）

### 课程内前置边界

- Published Article 06：Tool invocation/result/terminal Trace 与 single-process idempotency seam，不是完整跨 Step Trace。
- Published Article 08：Run/Turn/Step、Observation、State commit 与 `STOPPED != SUCCEEDED`。
- Published Article 11：Checkpoint/Resume/Retry、effect unknown 与 reconcile boundary，不保证 exactly-once。
- Published Articles 18—20：Evidence acceptance、Approval/authority refs、Budget `trace_ref` 与 decision reason seams。

## 最短结论

`真正可诊断的失败，不是一条更详细的报错，而是一组能保留身份、因果、合同 owner、恢复结果与未知边界的事件。`
