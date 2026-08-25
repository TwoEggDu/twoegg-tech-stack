# Article 20 Research｜Budget Engineering：Token、Step、Cost 与 Latency

## Research metadata

- Status: COMPLETE
- Evidence Gate Recommendation: PASS
- Core Claim count: 9
- Core BLOCKED Claims: 0
- Required Lab: NONE
- Experiment count: 0
- Runtime observation: ABSENT
- Source retrieval date: 2026-08-26 (Asia/Shanghai)
- BuildPilot posture: DESIGN / NOT IMPLEMENTED / NOT RUN
- Research boundary: 只研究 Budget 的 admission、reservation、enforcement、reconciliation、exhaustion 与 audit record；不声称运行结果、成本收益、质量提升或安全保证。

## Problem space

Agent Run 会同时消耗模型 Token、循环 Step、可计费资源和墙钟时间。四者有关联，却没有稳定的一一换算：一次 Step 可以包含一个模型调用和多个并行工具调用；输入可放进 Context Window 仍可能超出 Run 的 Token Budget；预估 Token 乘当前价目只能得到 estimate，不等于 provider cost record 或 invoice；一个 tool timeout 也不等于端到端 deadline。

因此，结束后汇总 `usage` 只是观测。Budget Engineering 必须在执行前回答是否接单、执行中回答是否还能启动下一项工作、耗尽时选择哪条合法终止路径，并在结束后保存 estimate、reservation、actual、remaining、decision 和 uncertainty。

## Source and drift register

| Source | Exact version / locator | Used for | Drift boundary |
|---|---|---|---|
| OpenAI Responses API reference | current hosted reference, `POST /responses` and `POST /responses/input_tokens`, retrieved 2026-08-26 | input count、`max_output_tokens`、usage、truncation/incomplete distinction | moving product contract；不固化 model window、price 或跨 Provider 字段语义 |
| OpenAI Organization Costs API | current hosted reference, `GET /organization/costs`, retrieved 2026-08-26 | historical cost record has amount/currency/time bucket and attribution fields | moving Admin API；不等于 per-request synchronous invoice |
| OpenAI Agents SDK Python | current hosted `Running agents`, retrieved 2026-08-26；official release index/tag snapshot: [`v0.22.0`](https://github.com/openai/openai-agents-python/releases/tag/v0.22.0), released 2026-08-19 | `max_turns` counts agent-loop turns / LLM calls and raises `MaxTurnsExceeded` | hosted docs can move；the hosted page is not proven built from `v0.22.0`；only product-scoped example |
| Anthropic Token Counting / Messages API | current hosted docs, retrieved 2026-08-26 | count is estimate；message response has provider-specific usage；`max_tokens` is an output cap | moving product contract；no field normalization with OpenAI |
| Anthropic Usage and Cost API | current hosted docs, retrieved 2026-08-26 | historical usage/cost reconciliation is separate from response counting | availability/key/product surface varies；not a universal billing schema |
| LangGraph Graph API | current hosted docs, retrieved 2026-08-26；official release index/tag snapshot: [`1.2.11`](https://github.com/langchain-ai/langgraph/releases/tag/1.2.11), released 2026-08-11 | `recursion_limit` counts graph super-steps and raises `GraphRecursionError` | hosted docs are not proven built from `1.2.11`；product-scoped comparison only |
| gRPC Deadlines guide | current official guide, retrieved 2026-08-26 | deadline is a point in time；timeout is a maximum duration；elapsed time must be deducted on propagation | gRPC semantics；not a universal Agent runtime implementation |
| FOCUS Specification | Publication version `1.4`, §§3.1.7, 3.1.35, 3.1.40 | Billed Cost is invoiced, not estimated/inferred；Effective Cost and List Cost are distinct | billing-data standard；does not define Agent reservation or admission |
| Published Articles 01/10/11/12/19 | repository snapshot 2026-08-26 | Token/Context、Step、checkpoint/retry、Context Budget、authority ownership seams | repository-local course contract；later authorized edits may drift |

No current price or context-window number is copied into this package. A future concrete estimate must bind a dated `price_snapshot_ref` and exact model/service tier; otherwise cost remains `UNKNOWN` or an explicitly bounded estimate.

## Abstract model

### Four budget dimensions are correlated, not interchangeable

| Dimension | Unit contract | Capacity / estimate input | Actual evidence | Typical enforcement point | Must not be inferred |
|---|---|---|---|---|---|
| Token | provider/model/request-surface-specific token categories | input count or estimate + output reserve | provider response usage or later usage record | before model call; after response reconciliation | Context Window fit = Run budget fit；same fields across Providers |
| Step | course committed Step or a named runtime's exact unit | `max_steps` and counting rule | committed counter / product result | before a new Step is admitted; at checkpoint/resume | cap reached = task success；turn = node = super-step |
| Cost | currency + pricing identity + accounting basis | usage estimate × dated price snapshot plus non-token items | cost/billing record with source and freshness | run admission; before chargeable operation; reconciliation | estimate/reservation = billed actual |
| Latency | monotonic duration + wall-clock deadline identity | end-to-end deadline and child timeout allocation | application-visible timestamps / provider-visible metrics | admission; queue dequeue; before child call; on resume | one timeout = end-to-end latency；hidden provider queue time is known |

The stable course abstraction is a vector, not a scalar:

```text
BudgetVector = {
  token:   {limit, estimated, reserved, actual, remaining, unit_contract},
  step:    {limit, in_flight_reserved, used, remaining_to_admit, counting_rule},
  cost:    {limit, estimated, reserved, incurred_pending, actual, remaining, currency, price_snapshot_ref},
  latency: {deadline, elapsed, remaining, clock_domain, host_id, boot_id, checkpoint_segment_id, phase_receipts}
}
```

This is a course proposal. Provider response fields remain in provider-native receipts and are adapted explicitly; they are not silently coerced into one alleged universal schema.

Every dimension also freezes a single-accounting invariant. One `consumption_id` may occupy exactly one accounting bucket at a time；a transition replaces the previous bucket rather than adding a second copy：

```text
available -> reserved/outstanding -> settled
                   \-> released

remaining_to_admit = limit - settled - conservative(outstanding)
```

Estimate is informational and never reduces remaining. Token reservation is replaced by the provider-native usage receipt；a missing receipt stays outstanding or routes fail closed. A Step first reserves one in-flight unit before work and increments `used` exactly once at the successful Article 10 Step commit；abort-before-commit releases that reservation. Cost moves by the same `charge_id` from reservation to measured/incurred-pending, then to source-qualified actual；only an unused delta or a charge proven absent may be released. Latency subtracts an end-to-end delta once；phase receipts explain attribution and are not subtracted again.

## Answers to the eight approved questions

### 1. Why four budgets cannot replace one another

**20-C01 — PARTIAL.** Product and protocol contracts expose separate output-token caps, loop-turn/super-step caps, monetary cost records and time deadlines. This supports separation, but the four-axis taxonomy and their orchestration are the course model. A Step cap limits work admitted into the loop; it does not cap exact Token, money or elapsed time. A cost limit does not prove the remaining Context fits. A deadline does not specify how many retries or model calls are safe.

### 2. Context Window, Token usage and Token Budget

**20-C02 — CONFIRMED.** Context Window is a model/request capacity contract; preflight token count may be an estimate; response usage reports categories defined by that Provider; Token Budget is an application policy over a Run or scope. OpenAI currently separates input-token count, `max_output_tokens`, response `usage`, truncation and incomplete reason. Anthropic explicitly says its preflight count is an estimate and exposes its own usage categories. Therefore applications must preserve `estimate_source`, provider/model identity, reserve and actual receipt instead of treating a character count or one provider field as universal truth.

### 3. Step Budget without a completion promise

**20-C03 — PARTIAL.** OpenAI Agents SDK currently defines a turn as one AI invocation including any tool calls and raises `MaxTurnsExceeded` when the configured limit is exceeded. LangGraph's current docs count super-steps and raise `GraphRecursionError`. Article 10 defines course Step as a committed loop iteration or local auditable unit and explicitly forbids cross-product conversion. A Step Budget must therefore freeze `counting_rule`, `scope`, whether the first attempt/retry/tool fan-out counts, and persist `used/in_flight_reserved/remaining_to_admit` across resume. For the course `committed_step_v1` rule, `remaining_to_admit = limit - used - in_flight_reserved`；pre-Step admission only creates one reservation keyed by `step_attempt_id`；`used` increments exactly once at successful Step commit, where that reservation is replaced, and abort-before-commit releases it. Reaching the cap means `BUDGET_EXHAUSTED / INCOMPLETE`, not successful completion, adequate quality or safe external effects.

### 4. Cost estimate, reservation and actual

**20-C04 — PARTIAL.** FOCUS 1.4 distinguishes List, Effective and Billed Cost, and requires Billed Cost to reflect invoiced rather than estimated/inferred values. OpenAI and Anthropic expose historical cost/usage administration surfaces separate from per-response usage. The course therefore separates `estimate`, `reservation`, `incurred_pending` and source-qualified `actual`: estimate is informational；reservation is an internal hold；after a response/result, measured usage or a known charge replaces the matching reservation as `incurred_pending` while billing remains unavailable；actual is a provider cost/billing record with source, freshness and accounting basis. For each `charge_id`, these buckets are mutually exclusive. `remaining = limit - settled_actual - conservative(outstanding)`，where outstanding is the active reservation or incurred-pending range, never both；hard admission uses its upper bound, and a missing finite bound yields `UNKNOWN/STOP`. Only the unused delta may be released；if attribution or completeness is unknown, the conservative outstanding amount remains held. Reservation and incurred-pending are course proposals, not provider billing primitives. If usage, price, currency, service tier, discounts or non-token line items are unknown, the record must preserve an interval or `UNKNOWN`, never synthesize exact actual cost.

### 5. Deadline, timeout, queue/service time and critical path

**20-C05 — PARTIAL.** gRPC confirms the narrow distinction: deadline is an absolute point after which the caller is unwilling to wait; timeout is a maximum duration, and already elapsed time must be deducted when propagating a remaining allowance. The course separately proposes an application-visible latency ledger: `admitted_at -> queued_at/dequeued_at -> started_at/completed_at`, plus child-operation receipts. Every monotonic stamp binds `clock_domain_id + host_id + boot_id + checkpoint_segment_id`；only compatible same-domain stamps may be subtracted. A process boundary must revalidate that identity. Across an incompatible process/host/reboot boundary, the runtime must use a persisted absolute deadline plus a current trusted clock and an explicit uncertainty bound/policy；when trusted, `safe_remaining = max(0, absolute_deadline - current_wall_clock - uncertainty_bound)`. If trust or uncertainty cannot be bounded, remaining is `UNKNOWN` and a hard latency policy routes `BLOCKED/STOP` rather than resetting the deadline. `queue_time` and `service_time` require both comparable boundary timestamps；unknown gaps and provider-internal waiting stay `UNKNOWN`. For parallel branches, summing all child durations overstates elapsed time；critical path is the longest dependency path in the application's known execution DAG, not the sum of all spans and not a claim about hidden provider scheduling. This resume contract is course design, not gRPC semantics.

### 6. Admission, reservation, enforcement and reconciliation

**20-C06 — PROPOSAL.** Minimum lifecycle:

```text
DECLARE
  -> ESTIMATE
  -> ADMIT | REJECT | REQUEST_APPROVAL
  -> RESERVE by consumption_id
  -> before-each-step / before-each-chargeable-call REVALIDATE
  -> REPLACE reservation with committed usage / incurred_pending, or RELEASE unused/aborted amount
  -> REPLACE incurred_pending with source-qualified actual when available
  -> RECONCILE
  -> COMPLETE | EXHAUSTED | CANCELLED | PARTIAL
```

Checks occur at run admission, queue dequeue, before each Step, before each provider/tool call, after every receipt, before retry and after resume. Replacement by the same consumption identity, rather than reserve-plus-actual addition, is the single-accounting rule. A check only constrains an action already authorized and otherwise eligible; Article 19 authority and Article 11 retry/effect safety remain separate prerequisites.

### 7. Exhaustion routes

**20-C07 — PROPOSAL.** Each dimension has a declared `hard/soft` policy and allowed routes. `STOP` is mandatory when a hard limit would be exceeded or required facts are unknown under fail-closed policy. `DEGRADE` may choose a cheaper model, smaller optional context, fewer optional branches or a summary only when Goal/Evidence/authority invariants remain satisfied. `REQUEST_APPROVAL` asks for an explicit budget increase but does not override hard authority policy. `PARTIAL_RESULT` preserves completed/unknown/unverified work and a budget-exhaustion reason. A budget cap never approves a retry, proves quality or makes a side effect safe.

### 8. Minimal Budget audit record

**20-C08 — PROPOSAL.** The minimum record binds:

```text
budget_record:
  identity: {budget_id, run_id, scope, parent_budget_id, policy_id, policy_version}
  contract: {dimensions, unit_contracts, hard_or_soft, effective_at, expires_at}
  estimate: {value_or_range, source, assumptions, observed_at}
  accounting: {dimension, consumption_id, state, basis, transition_from, transitioned_at}
  reservation: {reservation_id, amount_or_range, created_at, released_or_replaced_at}
  incurred_pending: {value_or_range, measurement_ref, completeness, observed_at}
  actual: {value_or_unknown, source_ref, source_freshness, accounting_basis, observed_at}
  remaining: {value_or_range, basis: limit-settled-conservative_outstanding, computed_at}
  clock: {clock_domain_id, host_id, boot_id, checkpoint_segment_id, absolute_deadline, uncertainty_bound, uncertainty_policy}
  decision: {point, action, route, reason_code, decided_at}
  uncertainty: {unknown_fields, bounds, stale_after, reconciliation_state}
  refs: {checkpoint_ref, provider_receipt_refs, authority_ref, trace_ref}
```

This is a Budget decision record, not Article 21's full cross-step Trace/Replay/Failure Taxonomy. `trace_ref` is only a seam. It also does not define Article 22's Eval dataset or regression judgment.

## Concrete design: BuildPilot budget envelope

**20-C09 — PROPOSAL.** BuildPilot may use a design-only `BudgetEnvelope` for a read-only investigation:

```text
BudgetEnvelope DESIGN ONLY
  scope: RUN / startup-investigation
  token: provider-native estimate + output reserve + hard run ceiling
  step: committed_step_v1; reserve before work; increment used exactly once at successful Step commit; retry counting explicit
  cost: currency + bounded estimate + price_snapshot_ref or UNKNOWN + remaining basis + reservation/incurred_pending/actual transitions
  latency: end_to_end_deadline + child timeout + clock-domain/host/boot/segment identity + application-visible phase stamps
  routes: STOP | DEGRADE_OPTIONAL_WORK | REQUEST_APPROVAL | PARTIAL_RESULT
  audit: estimate/reservation/actual/remaining/decision/uncertainty refs
```

This envelope is `DESIGN / NOT IMPLEMENTED / NOT RUN`. It has no real price, runtime usage, latency distribution, production effect or benefit result.

## Enforcement matrix

| Enforcement point | Token | Step | Cost | Latency | Required decision evidence |
|---|---|---|---|---|---|
| Run admission | expected input/output range | max + counting rule | estimate range + currency + price snapshot or UNKNOWN | end-to-end deadline | policy version, uncertainty, route |
| Queue dequeue / resume | recount if request changed | persisted used/in-flight reservation | outstanding charge identity/freshness | same-domain monotonic delta，or absolute deadline + bounded uncertainty；else UNKNOWN/STOP | checkpoint + clock-domain/host/boot/segment identity and revalidation |
| Before Step commit | reserve planned call by consumption id | reserve one in-flight unit；do not increment `used` | reserve predicted incremental range | remaining time | current State and legal transition |
| Before provider/tool call | provider-native preflight where available | tool/turn accounting rule | reserve chargeable maximum/range | child timeout within remaining deadline | Article 19 authority + Article 11 retry eligibility |
| After response/result | replace reservation with provider-native usage receipt | on successful Step commit，replace reservation and increment `used` exactly once；abort releases | replace reservation with measured/incurred-pending；release only unused delta；actual may stay pending | comparable phase timestamps | response/result + consumption identity |
| Terminal/reconcile | settled + outstanding/unknown | used + in-flight/remaining-to-admit | replace pending with source-qualified actual；retain unknown outstanding conservatively | elapsed + known same-domain receipts + unknown gaps | completion or exhaustion route |

## Counter-evidence and rejected shortcuts

- `fits_context = within_token_budget` is rejected: capacity, usage receipt and run policy are different objects.
- `max_turns = max_steps everywhere` is rejected: OpenAI's AI invocation and LangGraph's super-step are product-specific units; Article 10 also forbids cross-product Step conversion.
- `usage × current public price = actual` is rejected: current price may drift; discounts, service tiers, cache, tools and billing corrections can differ; FOCUS Billed Cost explicitly excludes estimates/inference.
- `reserved = spent` is rejected: an internal hold must be released or reconciled; it is not a provider invoice.
- `reservation + pending + actual` is rejected: the same consumption identity transitions between mutually exclusive buckets；it is never summed in more than one bucket.
- `timeout = deadline = latency budget` is rejected: a child timeout omits elapsed queueing, earlier work and sibling/parent paths.
- `new process monotonic - old process monotonic` is rejected unless both stamps carry a runtime-certified compatible clock-domain/host/boot identity；otherwise use the persisted absolute deadline with bounded uncertainty or fail closed.
- `sum(child durations) = end-to-end latency` is rejected under parallelism; only known dependency order can support a critical-path calculation.
- `budget remains = action allowed` is rejected by Articles 11 and 19: retry eligibility, authority and effect knowledge are independent gates.
- `budget exhausted = failed quality` is rejected: exhaustion proves a resource-policy terminal, while correctness/quality regression belongs to Article 22.

## Article ownership boundaries

- Article 01 owns Model API, Messages, Token and Context Window basics; Article 12 owns Context selection/fit/receipt. Article 20 only consumes those distinctions for Run-level enforcement.
- Article 10 owns State Machine/Workflow and the course Step definition; Article 11 owns checkpoint/resume, retry eligibility, effect uncertainty and recovery. Article 20 records budget state at those seams without redefining them.
- Article 19 owns permission/approval/HITL/sandbox. `REQUEST_APPROVAL` can request a budget change but cannot authorize an otherwise forbidden action.
- Article 21 owns cross-step Trace, Replay and Failure Taxonomy. Article 20 exposes stable record and reference seams only.
- Article 22 owns Eval, Golden Dataset and Regression. Article 20 makes no claim that a cap or degradation policy improves quality.

## Evidence Gate recommendation

- Recommendation: **PASS**
- Coverage: 9 / 9 core Claims trace to 11 Evidence Cards.
- Status mix: CONFIRMED = C02；PARTIAL = C01/C03/C04/C05；PROPOSAL = C06/C07/C08/C09；BLOCKED = 0.
- Required Lab: NONE；Experiments: 0；Runtime observation: ABSENT.
- Pricing/window discipline: no current price or context-window number is embedded；future examples require dated source identity.
- Provider discipline: OpenAI, Anthropic and LangGraph semantics remain source-native and are not normalized as universal contracts；OpenAI Agents SDK `v0.22.0` (2026-08-19) and LangGraph `1.2.11` (2026-08-11) are replayable release-index snapshots only，not claims that hosted docs were built from those tags.
- Gate ceiling: later stages may write PARTIAL as bounded synthesis and PROPOSAL as design only；no Claim may be upgraded to runtime, cost, latency, quality or safety evidence.
- Next allowed Gate recommendation: **OUTLINE**.
