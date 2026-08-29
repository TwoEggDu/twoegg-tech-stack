# Article 25 Research｜Agent Runtime vs Harness：执行内核与工程控制面

## Research status

- Gate: `RESEARCH`
- Researcher: `REAL_SUBAGENT / FRESH CONTEXT`
- Access date: `2026-08-29 Asia/Shanghai`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Evidence Gate Recommendation: `PASS`
- Claim Coverage: `12 / 12`
- Evidence Cards: `12 / 12`
- Status Mix: `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`

## Boundary statement

Article 25 can define a teaching boundary for `Agent Runtime / Harness / Host / business Agent or Workflow`, but it must not claim that this split is an industry standard. Official sources use overlapping terms: Microsoft Agent Framework uses `Agent Harness` for runtime scaffolding that can include model calls, tool calls, conversation state, approval policy and UX; LangChain distinguishes runtime, framework and harness in its own product model; OpenAI product language can call newer infrastructure a `model-native harness`. Therefore Article 25 should teach responsibility allocation by owner, state, invariant, failure mode and replacement pressure, not by product class name.

This research does not create the Article 26 minimum capability model and does not decide the Article 27 adoption or bloat trade-off. BuildPilot remains a design case only: no implementation, no runtime log, no Unity scan, no Jenkins run and no production claim.

## Source manifest

### Local course sources

- `docs/agent-engineering-course/README.md` — current gate state, hard rules, Article 24 publication baseline and Article 25 object.
- `docs/agent-engineering-course/production-workflow.md` — Research and Evidence Gate entry/exit criteria.
- `docs/agent-engineering-course/subagent-contracts.md` — Researcher allowed writes, evidence-card requirements and closed-schema worker result.
- `docs/agent-engineering-course/status.md` — Article 25 gate row and required Lab `NONE`.
- `docs/agent-engineering-course/glossary.md` — course working definitions for Runtime, Harness, Host, Tool Runtime, Context, Evidence, Trace, Replay and Eval.
- `docs/agent-engineering-series-plan.md` — canonical Part V row for Articles 24–28 and concept progression.
- `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/article-card.md` — frozen Article 25 problem, core questions, BuildPilot allocation case and non-scope.
- `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md` — current Article 25 gate instructions.
- `content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md` — published Article 24 boundary: why shared Harness pressure exists, with Article 25 left to define Runtime vs Harness.
- Targeted prior articles used only for boundary continuity: Articles 06, 07, 10, 11, 12, 18, 19, 20, 21 and 22.

### External primary sources

- OpenAI Agents SDK, `Running agents`, `Tools`, `Human-in-the-loop`, `Sessions`, `Context`, `Guardrails`, `Sandbox agents` — official docs, accessed `2026-08-29`.
- OpenAI, `The next evolution of the Agents SDK` — official product terminology note, accessed `2026-08-29`.
- Microsoft Agent Framework, `Agent Harness`, `Tools overview`, `Workflow state`, `Functional workflow`, `Workflow checkpoints`, `Workflow human-in-the-loop`, `Agent skills` — official docs, accessed `2026-08-29`.
- LangChain/LangGraph, `Runtimes, frameworks, and harnesses`, `Deep Agents overview`, `Deep Agents context engineering`, `LangGraph persistence`, `LangGraph overview`, `Runtime` — official docs, accessed `2026-08-29`.
- Model Context Protocol specification `2025-06-18`, `Architecture`, `Tools`, `Roots`; draft `Authorization` used only as draft/background — official protocol specification, accessed `2026-08-29`.
- OpenTelemetry specification, `Trace API` and `Overview` — official observability specification, accessed `2026-08-29`.
- Temporal durable execution docs and AWS Durable Execution determinism docs — official durable execution references, accessed `2026-08-29`.
- Unity 2022.3 BuildReport API, Unity Asset Database manual and Addressables Analyze docs — official Unity evidence-surface references, accessed `2026-08-29`.
- NIST AI RMF Core — governance vocabulary reference, accessed `2026-08-29`.

## Research questions answered

### RQ1. 公开 Agent SDK、protocol、durable workflow 与 host 文档实际把哪些执行责任拆开？

The public documents repeatedly split at least these responsibilities:

- model-call loop and stopping/turn-limit control;
- local or hosted tool registration, discovery, schema and dispatch;
- host/container coordination, client lifecycle, consent/permission and context aggregation;
- approval pause/resume and human decision surfaces;
- workflow state, checkpoint, replay and durable recovery;
- trace/observability correlation;
- external sandbox or execution environment;
- domain/business decision logic.

This supports Article 25’s split, but only as a course model. Product names are not stable enough to be used as the taxonomy.

Related claims: `25-C01`, `25-C02`, `25-C03`, `25-C10`.

### RQ2. 模型调用、tool dispatch、wait/resume、state transition 和 checkpoint 的最小执行闭环是什么？

The minimum execution loop is: receive task/input, assemble or load state/context, call model, interpret model output, dispatch tools or handoffs when requested, append results, continue until final output, interruption, approval wait, error or max-turn boundary. OpenAI Agents SDK documents a Runner loop; Microsoft workflows document supersteps and checkpoints; LangGraph documents checkpointed graph state; Temporal/AWS durable execution docs describe replay/cached-step behavior as a recovery mechanism.

The course should phrase this as Runtime owning execution progression, while Harness can own shared gates and policies that constrain that progression.

Related claims: `25-C01`, `25-C04`, `25-C07`, `25-C09`.

### RQ3. 哪些控制必须跨多个 Run / Agent / Workflow 保持共享语义，不能只留在 Runtime 局部？

Controls that must stay semantically shared include identity, permission, sandbox scope, approval policy, budget accounting, trace shape, evidence acceptance criteria, checkpoint/replay policy, registry/discovery rules and human takeover procedures. One runtime loop can execute these controls, but if every Agent or Workflow redefines them locally, Article 24’s boundary pressure returns as drift.

Related claims: `25-C06`, `25-C07`, `25-C08`, `25-C11`.

### RQ4. Context assembly 与 context policy 如何区分？

Context assembly is the concrete operation of selecting, ordering, compressing and injecting model-visible or tool-visible material for a run. Context policy decides what may be exposed, retained, redacted, summarized or rehydrated across runs and users. OpenAI and LangChain docs both distinguish runtime/local context from model-visible context; course Article 12 already separates Prompt, Context, Session, Snapshot, Memory and Checkpoint. Article 25 can use this as a Runtime/Harness split, but should avoid claiming a universal industry boundary.

Related claims: `25-C05`, `25-C08`.

### RQ5. 业务 state、execution state、governance state 与 host/UI state 有哪些不同 owner 和 lifetime？

Business state belongs to the domain problem and should remain meaningful if the execution engine is replaced. Execution state belongs to a run, graph, workflow or durable execution mechanism. Governance state records permission, approval, budget, evidence and audit decisions. Host/UI state belongs to the surrounding application, workspace and user interaction surface. Microsoft workflow docs explicitly separate runtime kwargs from shared workflow state and note workflow instance isolation; MCP architecture assigns context aggregation and permission enforcement to the Host; local course articles already distinguish evidence/trace/budget/replay controls.

This is source-supported but still a course allocation model, so it should be treated as `PARTIAL`, not universal fact.

Related claims: `25-C08`, `25-C11`.

### RQ6. Framework、Runtime、Harness、Workflow Engine、Host 为何不能仅靠产品名分类？

Because vendors reuse the same words differently. Microsoft’s `Agent Harness` includes responsibilities that the course would split between Runtime, Harness and Host. LangChain’s official product table distinguishes runtime/framework/harness, but defines them for its own stack. OpenAI uses `harness` as product terminology while its SDK docs expose specific lower-level responsibilities. MCP defines Host/Client/Server without adopting the course’s Runtime/Harness terms. Article 25 should therefore use responsibility questions, not product labels.

Related claims: `25-C10`, `25-C11`.

### RQ7. Failure classification、retry、recovery、approval 与 human takeover 怎样分离 decision 与 execution？

Execution mechanisms can retry, resume from checkpoints, re-emit pending approval events and replay durable state. The decision that a retry is safe, that a human approval is required, or that the owner must take over belongs to a policy/governance layer. OpenAI HITL docs show approval as a paused run state; Microsoft HITL docs tie pending requests to checkpoints; LangGraph persistence makes HITL and fault-tolerant execution possible, but does not by itself decide evidence acceptance or business authority.

Related claims: `25-C06`, `25-C07`, `25-C09`.

### RQ8. BuildPilot 同一需求变更链如何按 Host / Business / Runtime / Harness 分账？

Use the frozen Article 25 design case:

1. Host receives owner interaction and project/workspace context.
2. BuildPilot business logic decides candidate check targets and domain order for a Unity requirement change.
3. Runtime executes the task graph/model calls/read-only tools/steps and returns observations.
4. Harness applies identity, permission, evidence, budget, trace, approval and recovery constraints.
5. Owner implements the actual project change outside BuildPilot.
6. Runtime re-verifies selected checks after owner action.
7. Harness stores the auditable governance result.

This is a `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST` example. It is suitable for Article 25 as an allocation narrative, not as implementation evidence.

Related claims: `25-C11`, `25-C12`.

### RQ9. 哪些结论只能是课程 Taxonomy 或架构 Proposal？

The exact four-layer split and the five-question boundary test are course taxonomy. The BuildPilot allocation is a design proposal. The claim that runtime/harness/host/business state have distinct owners is source-supported, but the specific course names and article diagram should be marked as teaching model.

Related claims: `25-C08`, `25-C10`, `25-C11`, `25-C12`.

### RQ10. Article 26/27 的内容怎样明确留白？

Article 25 may name capability groups only to draw a boundary, but must not enumerate the full minimum capability model. It may say poor allocation causes bloat or adoption friction, but must not provide the Article 27 trade-off framework. It can end by handing the reader from “how to tell what layer owns a responsibility” to “what the minimum Harness must contain,” without filling that model.

Related claims: `25-C11`, `25-C12`.

## Responsibility split for Article 25

| Layer | Article 25 wording ceiling | Owns | Does not prove / does not own |
|---|---|---|---|
| Host | Application/container/workspace/user-interaction boundary. | Environment integration, client lifecycle, UI/owner interaction, context aggregation, filesystem roots or workspace surface. | It is not automatically the policy brain or the Agent Runtime. |
| Business Agent / Workflow | Domain judgment and business task structure. | What the task means, which domain order matters, when business state changes are valid. | It should not silently own shared permission, evidence, budget or audit semantics. |
| Agent Runtime | Execution progression. | Model calls, step loop, tool dispatch, handoff execution, wait/final/error boundaries, local run state. | A running loop is not enough to prove shared governance, evidence acceptance or business authority. |
| Harness | Shared engineering control plane. | Identity, permission, approval, sandbox, budget, trace, evidence, checkpoint/replay policy, registry/discovery rules and recovery conventions. | Harness is a course term here; it is not a universal product label. |

## Claim Register

| Claim ID | Status | Evidence | Research claim | Wording ceiling |
|---|---|---|---|---|
| `25-C01` | `CONFIRMED` | `25-E01` | Agent Runtime can be taught as the owner of execution progression: model call loop, tool dispatch, handoff/continuation and stop/error/max-turn boundaries. | Say “in this course model” when naming it Runtime. Do not imply all SDKs expose a class with this name. |
| `25-C02` | `CONFIRMED` | `25-E02` | Host is a separate application/container boundary that can coordinate clients, lifecycle, permissions, context aggregation, workspace roots and user interaction. | MCP confirms Host responsibilities; it does not define the full course Harness. |
| `25-C03` | `CONFIRMED` | `25-E03` | Tool discovery, schema, call dispatch and result handling are separate from permission approval and evidence acceptance. | Tool-call success is not safety, authorization or evidence acceptance. |
| `25-C04` | `PARTIAL` | `25-E04` | Workflow/state-machine mechanisms own structured transitions and durable state differently from a free-form agent loop. | Product boundaries vary; use as an analytical distinction, not a universal hierarchy. |
| `25-C05` | `PARTIAL` | `25-E05` | Context assembly is an execution-time operation; context policy is a governance decision about exposure, retention, redaction and budget. | Do not claim a source uses the exact course split. |
| `25-C06` | `PARTIAL` | `25-E06` | Identity, permission, approval and sandbox are separable controls that may pause, constrain or reject execution and may need resume state. | Sources show pieces; Article 25 may integrate them as course Harness responsibilities. |
| `25-C07` | `PARTIAL` | `25-E07` | Budget, trace, evidence, checkpoint and replay are related recovery/audit supports, but none of them alone proves correctness or authorizes a business decision. | Preserve Article 18–22 distinctions; no runtime observation is available for Article 25. |
| `25-C08` | `PARTIAL` | `25-E08` | Business state, execution state, governance state and host/UI state should be separated by owner and lifetime. | This is a source-supported course allocation, not an externally standardized four-state model. |
| `25-C09` | `PARTIAL` | `25-E09` | Failure/retry/recovery/human takeover must split execution mechanics from policy decisions about safe retry, approval and escalation. | Do not equate checkpoint replay with repair or human authorization. |
| `25-C10` | `CONFIRMED` | `25-E10` | Same-product multi-layer implementations and vendor terminology variance mean Article 25 must compare responsibilities, not product labels. | Explicitly mention terminology variance as counter-evidence to industry-standard wording. |
| `25-C11` | `PROPOSAL` | `25-E11` | The course boundary test should ask owner, state, invariant, failure and replacement questions to place responsibilities among Host, Business, Runtime and Harness. | Present as teaching model. Do not cite it as vendor doctrine. |
| `25-C12` | `PROPOSAL` | `25-E12` | BuildPilot requirement-change flow can illustrate allocation: Host receives, business logic plans, Runtime executes/re-verifies, Harness governs/audits, owner implements. | Must remain `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`. |

## Counter-evidence and limitations

- Microsoft Agent Framework’s `Agent Harness` page is direct counter-evidence against saying “Harness never includes runtime.” Its harness can drive model and tool calls while also managing state, context, approvals and UX.
- LangChain uses `Runtime`, `Framework` and `Harness` as product categories, but its definitions do not exactly match the course split.
- MCP defines Host/Client/Server and tool capabilities, not a Runtime/Harness taxonomy.
- OpenAI documents Runner, tools, HITL, sessions, guardrails and sandbox concerns separately, while product language may use `harness` differently.
- Article 25 has no lab, runtime observation or BuildPilot implementation evidence. Any statement about BuildPilot must stay as design allocation.
- Official docs are current as of `2026-08-29`; SDK beta/product surfaces may drift.

## Evidence Gate recommendation

Recommendation: `PASS`.

Reasoning:

- All 12 Article 25 core research claims map one-to-one to Evidence Cards `25-E01` through `25-E12`.
- No core claim is `BLOCKED`.
- `PARTIAL` claims have narrowed wording ceilings.
- `PROPOSAL` claims are explicitly course taxonomy or BuildPilot design case, not source-confirmed behavior.
- Required Lab is `NONE`; Experiment Count is `0`; Runtime Observation is `ABSENT`.
- Article 26 and Article 27 content remains out of scope.

Downstream Writer/Reviewer guardrail: if a draft sentence sounds like “the industry calls X Runtime and Y Harness,” rewrite it to “in this course, we use X/Y to separate responsibilities.” If a BuildPilot sentence sounds implemented or run, rewrite it to `design case / read-only / suggestion-first`.
