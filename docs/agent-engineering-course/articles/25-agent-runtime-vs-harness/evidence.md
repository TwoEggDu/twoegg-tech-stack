# Article 25 Evidence｜Agent Runtime vs Harness：执行内核与工程控制面

## Evidence status

- Gate: `RESEARCH`
- Evidence Gate Recommendation: `PASS`
- Cards: `12`
- Core Blocked Claims: `0`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Access date: `2026-08-29 Asia/Shanghai`
- Status Mix: `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`

## Evidence contract

Each `25-Enn` card maps to exactly one `25-Cnn` claim. Evidence statuses mean:

- `CONFIRMED`: directly supported by current primary documentation or local published course artifacts, with limited wording.
- `PARTIAL`: supported by multiple sources but requires course-level synthesis or narrowed phrasing.
- `PROPOSAL`: course taxonomy or BuildPilot design allocation, not a confirmed external implementation.
- `BLOCKED`: unavailable, contradictory or unsafe to claim. Article 25 has `0` blocked core claims after this research pass.

Vendor terminology is evidence, not authority. Article 25 must not promote `Runtime / Harness / Host` into a universal industry standard.

## Claim Register Snapshot

| Claim ID | Evidence ID | Status | Short claim |
|---|---|---|---|
| `25-C01` | `25-E01` | `CONFIRMED` | Runtime owns execution progression. |
| `25-C02` | `25-E02` | `CONFIRMED` | Host is a separate application/container boundary. |
| `25-C03` | `25-E03` | `CONFIRMED` | Tool discovery/call is separate from permission and evidence acceptance. |
| `25-C04` | `25-E04` | `PARTIAL` | Workflow/state-machine mechanisms own structured transitions and durable state differently from a free-form loop. |
| `25-C05` | `25-E05` | `PARTIAL` | Context assembly and context policy are distinct concerns. |
| `25-C06` | `25-E06` | `PARTIAL` | Identity, permission, approval and sandbox controls are separable and may require resume state. |
| `25-C07` | `25-E07` | `PARTIAL` | Budget, trace, evidence, checkpoint and replay support audit/recovery but do not prove correctness alone. |
| `25-C08` | `25-E08` | `PARTIAL` | Business, execution, governance and host/UI state differ by owner and lifetime. |
| `25-C09` | `25-E09` | `PARTIAL` | Failure/retry/recovery/human takeover split execution mechanics from policy decisions. |
| `25-C10` | `25-E10` | `CONFIRMED` | Vendor terminology variance requires responsibility-based comparison. |
| `25-C11` | `25-E11` | `PROPOSAL` | Course boundary test: owner, state, invariant, failure, replacement. |
| `25-C12` | `25-E12` | `PROPOSAL` | BuildPilot allocation case is design-only and read-only/suggestion-first. |

## Evidence Cards

### 25-E01 — Runtime owns execution progression

- Claim ID: `25-C01`
- Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Primary sources:
  - OpenAI Agents SDK — Running agents: `https://openai.github.io/openai-agents-python/running_agents/`
  - OpenAI Agents SDK — Tools: `https://openai.github.io/openai-agents-python/tools/`
  - LangGraph overview: `https://docs.langchain.com/oss/python/langgraph/overview`
  - Microsoft Agent Framework — Agent Harness: `https://learn.microsoft.com/en-us/agent-framework/concepts/harness`
- Source date/access: official docs accessed `2026-08-29 Asia/Shanghai`.
- Direct support: OpenAI documents Runner methods and a loop that calls the model, dispatches tools or handoffs, and continues until final output or a stopping condition. LangGraph describes a runtime for long-running stateful agents. Microsoft harness docs also include model/tool call driving, which shows the execution loop is a recognizable responsibility even when product naming differs.
- Counter-evidence searched: Microsoft names some execution-loop scaffolding `Agent Harness`, not `Runtime`. LangChain uses `Runtime` in a product-specific way. Therefore the course can confirm the responsibility, not a universal term.
- Proves: execution progression is a real, documentable responsibility in agent systems.
- Does not prove: every framework has a separate `Agent Runtime` object or that Runtime excludes all policy hooks.
- Limitations: no Article 25 runtime was run; this is documentation evidence.
- Downstream wording ceiling: “In this course, Runtime names the execution progression owner: model calls, tool dispatch, handoffs, continuation and stop/error boundaries.”

### 25-E02 — Host is a separate application/container boundary

- Claim ID: `25-C02`
- Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_SPEC`
- Primary sources:
  - MCP 2025-06-18 Architecture: `https://modelcontextprotocol.io/specification/2025-06-18/architecture`
  - MCP 2025-06-18 Roots: `https://modelcontextprotocol.io/specification/2025-06-18/client/roots`
  - OpenAI Agents SDK — Sandbox agents: `https://openai.github.io/openai-agents-python/sandbox/guide/`
  - Microsoft Agent Framework — Agent Harness: `https://learn.microsoft.com/en-us/agent-framework/concepts/harness`
- Source date/access: official specs/docs accessed `2026-08-29 Asia/Shanghai`.
- Direct support: MCP assigns Host responsibilities including client creation, lifecycle, security/consent enforcement, authorization handling, context aggregation and coordination with the LLM. MCP Roots define filesystem boundaries exposed to servers. OpenAI sandbox docs expose another host/execution boundary around capabilities and sessions.
- Counter-evidence searched: MCP does not define a course-level `Harness`; OpenAI sandbox terms are beta/product-specific.
- Proves: Host can be treated as a boundary distinct from a business agent or one model-call loop.
- Does not prove: Host alone owns every governance decision.
- Limitations: MCP is a protocol model; application architectures can differ.
- Downstream wording ceiling: “Host is the surrounding application/environment boundary that coordinates clients, context, workspace and user interaction.”

### 25-E03 — Tool discovery/call is separate from permission and evidence acceptance

- Claim ID: `25-C03`
- Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_SPEC`
- Primary sources:
  - MCP 2025-06-18 Tools: `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`
  - OpenAI Agents SDK — Tools: `https://openai.github.io/openai-agents-python/tools/`
  - Microsoft Agent Framework — Tools overview: `https://learn.microsoft.com/en-us/agent-framework/agents/tools/`
  - Local Article 06 Tool Runtime and Article 07 MCP continuity.
- Source date/access: official specs/docs and local course artifacts accessed `2026-08-29 Asia/Shanghai`.
- Direct support: MCP separates listing tools, calling tools, schema/annotations and tool-result protocol behavior, while warning about user interaction and safety. OpenAI and Microsoft docs distinguish local/hosted tools, function tools and approval-related options. Course Article 06 already separates Tool Runtime stages from evidence acceptance.
- Counter-evidence searched: some platforms bundle discovery, call and approval into one SDK surface; that is implementation packaging, not proof the responsibilities are identical.
- Proves: tool availability and call success do not automatically settle authorization or claim validity.
- Does not prove: a single product module cannot implement multiple responsibilities.
- Limitations: local prior article evidence is course continuity, not external proof.
- Downstream wording ceiling: “Tool dispatch belongs to execution; permission and evidence acceptance are separate controls even if implemented nearby.”

### 25-E04 — Workflow/state mechanisms differ from a free-form agent loop

- Claim ID: `25-C04`
- Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Primary sources:
  - Microsoft Agent Framework — Workflow state: `https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/state`
  - Microsoft Agent Framework — Functional workflow: `https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/functional`
  - Microsoft Agent Framework — Workflow checkpoints: `https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints`
  - LangGraph persistence: `https://docs.langchain.com/oss/python/langgraph/persistence`
  - Temporal durable execution: `https://docs.temporal.io/workflow-execution`
- Source date/access: official docs accessed `2026-08-29 Asia/Shanghai`.
- Direct support: Microsoft documents workflow state, per-instance isolation and checkpointed supersteps. Functional workflows can be represented as async steps and wrapped as agents in that product. LangGraph persistence stores graph state checkpoints and enables HITL, memory, time travel and fault-tolerant execution. Temporal durable execution frames workflow execution around event history and replay.
- Counter-evidence searched: vendors differ on whether they call these mechanisms workflows, graphs, durable execution or agents.
- Proves: structured transition/durable state is a distinct responsibility cluster from simply prompting a model each turn.
- Does not prove: Workflow Engine is always separate from Runtime, or that every agent system must expose workflows.
- Limitations: cross-source synthesis is needed; claim remains `PARTIAL`.
- Downstream wording ceiling: “Workflow/state-machine mechanics give execution a structure; the exact product boundary varies.”

### 25-E05 — Context assembly differs from context policy

- Claim ID: `25-C05`
- Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Primary sources:
  - OpenAI Agents SDK — Context: `https://openai.github.io/openai-agents-python/context/`
  - OpenAI Agents SDK — Sessions: `https://openai.github.io/openai-agents-python/sessions/`
  - LangChain Deep Agents — Context engineering: `https://docs.langchain.com/oss/python/deepagents/context-engineering`
  - MCP 2025-06-18 Architecture: `https://modelcontextprotocol.io/specification/2025-06-18/architecture`
  - Local Article 12 Prompt/Context/Session/Snapshot/Memory/Checkpoint boundary.
- Source date/access: official docs and local course artifacts accessed `2026-08-29 Asia/Shanghai`.
- Direct support: OpenAI docs distinguish local context from LLM-visible context and sessions/history. LangChain context engineering distinguishes input context, runtime context, compression, isolation and memory. MCP Host responsibilities include context aggregation. Article 12 already establishes course distinctions among prompt, context, session, snapshot, memory and checkpoint.
- Counter-evidence searched: sources usually describe context mechanics; fewer explicitly name “context policy.”
- Proves: there is a meaningful difference between assembling what the model sees and governing what it may see or retain.
- Does not prove: a universal “Context Policy” product interface exists.
- Limitations: policy split is a course synthesis from multiple documented responsibilities.
- Downstream wording ceiling: “Runtime can assemble context; Harness can define exposure, retention, redaction and budget policy.”

### 25-E06 — Identity, permission, approval and sandbox are separable controls

- Claim ID: `25-C06`
- Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Primary sources:
  - OpenAI Agents SDK — Human-in-the-loop: `https://openai.github.io/openai-agents-python/human_in_the_loop/`
  - OpenAI Agents SDK — Guardrails: `https://openai.github.io/openai-agents-js/guides/guardrails/`
  - OpenAI Agents SDK — Sandbox agents: `https://openai.github.io/openai-agents-python/sandbox/guide/`
  - Microsoft Agent Framework — Tools overview: `https://learn.microsoft.com/en-us/agent-framework/agents/tools/`
  - Microsoft Agent Framework — Workflow human-in-the-loop: `https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop`
  - MCP 2025-06-18 Architecture and Roots.
  - Local Article 19 Permission/Approval/Sandbox.
- Source date/access: official docs and local course artifacts accessed `2026-08-29 Asia/Shanghai`.
- Direct support: OpenAI HITL docs show tool approval can pause runs and resume with serialized state. OpenAI guardrail docs show blocking/validation surfaces. Sandbox docs expose capability boundaries. Microsoft docs include tool approval and workflow HITL with pending requests. MCP Host and Roots show permission/lifecycle and filesystem boundary responsibilities.
- Counter-evidence searched: products may collapse these into one “approval” or “sandbox” feature; not every source gives all four controls at equal depth.
- Proves: approval, permission and sandbox can be separately reasoned about and may interrupt or constrain execution.
- Does not prove: Article 25 can specify the complete permission model; that belongs to later capability/trade-off work.
- Limitations: `PARTIAL` because the course integrates multiple control types into one Harness boundary.
- Downstream wording ceiling: “Harness owns shared semantics for identity, permission, approval and sandbox; Runtime executes the paused/resumed steps.”

### 25-E07 — Budget, trace, evidence, checkpoint and replay are related but not equivalent

- Claim ID: `25-C07`
- Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Primary sources:
  - OpenTelemetry Trace API: `https://opentelemetry.io/docs/specs/otel/trace/api/`
  - OpenTelemetry Overview: `https://opentelemetry.io/docs/specs/otel/overview/`
  - LangGraph persistence: `https://docs.langchain.com/oss/python/langgraph/persistence`
  - Microsoft Agent Framework — Workflow checkpoints: `https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints`
  - Temporal durable execution: `https://docs.temporal.io/workflow-execution`
  - Local Articles 18 Evidence, 20 Budget, 21 Trace/Replay, 22 Eval.
- Source date/access: official docs and local course artifacts accessed `2026-08-29 Asia/Shanghai`.
- Direct support: OpenTelemetry defines trace/span correlation and operation metadata, not claim acceptance. LangGraph and Microsoft checkpoint docs preserve execution state for resume/HITL. Temporal durable execution uses event history/replay for recovery. Local Articles 18–22 explicitly separate Evidence, Budget, Trace, Replay and Eval.
- Counter-evidence searched: platforms sometimes present traces/checkpoints as debugging or reliability features; they still do not by themselves establish truth or authorization.
- Proves: these controls form an audit/recovery support cluster but should not be collapsed into one concept.
- Does not prove: any Article 25 lab result, performance metric or production behavior.
- Limitations: `PARTIAL` because the combined boundary is course synthesis.
- Downstream wording ceiling: “Trace shows what happened; checkpoint helps resume; evidence decides what a claim is allowed to rely on.”

### 25-E08 — Business, execution, governance and host/UI state differ by owner and lifetime

- Claim ID: `25-C08`
- Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Primary sources:
  - Microsoft Agent Framework — Workflow state: `https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/state`
  - OpenAI Agents SDK — Sessions: `https://openai.github.io/openai-agents-python/sessions/`
  - OpenAI Agents SDK — Context: `https://openai.github.io/openai-agents-python/context/`
  - MCP 2025-06-18 Architecture: `https://modelcontextprotocol.io/specification/2025-06-18/architecture`
  - Local glossary and Articles 10, 12, 18, 19, 20, 21.
- Source date/access: official docs and local course artifacts accessed `2026-08-29 Asia/Shanghai`.
- Direct support: Microsoft workflow state docs distinguish shared/private workflow state and runtime kwargs. OpenAI sessions/context docs distinguish persisted conversation history, local context and model-visible context. MCP separates Host coordination from server capabilities. Local course artifacts define separate Evidence, Trace, Budget and Context concepts.
- Counter-evidence searched: no source names the exact four-state model as a standard.
- Proves: owner/lifetime is a useful and supported way to separate state responsibilities.
- Does not prove: a canonical four-bucket external architecture.
- Limitations: course taxonomy; use as design lens, not source quotation.
- Downstream wording ceiling: “Ask who owns the state and how long it should survive; do not store every state in the runtime loop.”

### 25-E09 — Failure/retry/recovery/human takeover split mechanics from decisions

- Claim ID: `25-C09`
- Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Primary sources:
  - OpenAI Agents SDK — Human-in-the-loop: `https://openai.github.io/openai-agents-python/human_in_the_loop/`
  - Microsoft Agent Framework — Workflow human-in-the-loop: `https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop`
  - Microsoft Agent Framework — Workflow checkpoints: `https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints`
  - LangGraph persistence: `https://docs.langchain.com/oss/python/langgraph/persistence`
  - AWS Durable Execution determinism: `https://docs.aws.amazon.com/durable-execution/patterns/best-practices/determinism/`
  - Local Articles 11 and 21.
- Source date/access: official docs and local course artifacts accessed `2026-08-29 Asia/Shanghai`.
- Direct support: OpenAI approval flows can pause/resume runs. Microsoft workflow HITL stores pending request state in checkpoints. LangGraph persistence enables HITL/fault-tolerant execution. AWS durable execution determinism docs explain replay/cached-step constraints. Local course articles already distinguish long-running recovery and trace/replay failure analysis.
- Counter-evidence searched: durable systems can automate retry mechanics, but the documentation does not say every retry is business-safe.
- Proves: recovery mechanics and recovery decisions are different layers of concern.
- Does not prove: a complete incident-response policy for agents.
- Limitations: no Article 25 runtime fault injection or replay experiment.
- Downstream wording ceiling: “Runtime can retry or resume; Harness decides when retry, approval, escalation or owner takeover is allowed.”

### 25-E10 — Vendor terminology variance requires responsibility-based comparison

- Claim ID: `25-C10`
- Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Primary sources:
  - Microsoft Agent Framework — Agent Harness: `https://learn.microsoft.com/en-us/agent-framework/concepts/harness`
  - LangChain — Runtimes, frameworks, and harnesses: `https://docs.langchain.com/oss/python/concepts/products`
  - LangChain Deep Agents overview: `https://docs.langchain.com/oss/python/deepagents/overview`
  - OpenAI — The next evolution of the Agents SDK: `https://openai.com/index/the-next-evolution-of-the-agents-sdk/`
  - MCP 2025-06-18 Architecture: `https://modelcontextprotocol.io/specification/2025-06-18/architecture`
- Source date/access: official docs/product pages accessed `2026-08-29 Asia/Shanghai`.
- Direct support: Microsoft’s harness category explicitly includes runtime scaffolding and application UX. LangChain defines runtime/framework/harness in its own product comparison and Deep Agents as a harness on top of LangGraph runtime. OpenAI uses `harness` in product language. MCP uses Host/Client/Server instead of Runtime/Harness.
- Counter-evidence searched: the variance itself is the counter-evidence against universal naming.
- Proves: Article 25 must compare responsibilities and invariants, not vendor labels.
- Does not prove: the course should adopt any one vendor taxonomy.
- Limitations: terminology may drift as docs evolve.
- Downstream wording ceiling: “Product class names are examples, not the boundary. Article 25’s split is a teaching taxonomy.”

### 25-E11 — Course boundary test is a proposal

- Claim ID: `25-C11`
- Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Primary sources:
  - `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/article-card.md`
  - `docs/agent-engineering-course/glossary.md`
  - `docs/agent-engineering-series-plan.md`
  - Article 24 published boundary.
  - Supporting external docs in `25-E01` through `25-E10`.
- Source date/access: local and external sources accessed `2026-08-29 Asia/Shanghai`.
- Direct support: The Article 25 card requires a principle article that uses ownership and invariants to separate Runtime, Harness, Host and business responsibilities. The earlier evidence cards show this is a useful decomposition, while also showing terminology variance.
- Counter-evidence searched: no primary source standardizes this exact five-question boundary test.
- Proves: the course can responsibly present the boundary test as a teaching model.
- Does not prove: the test is an industry-standard architecture method.
- Limitations: proposal only; Reviewer should reject any wording that makes it sound normative outside the course.
- Downstream wording ceiling: “When unsure, ask five questions: who owns it, what state it changes, what invariant it protects, how it fails, and whether it survives replacing the model/runtime/product.”

### 25-E12 — BuildPilot allocation case is design-only

- Claim ID: `25-C12`
- Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Primary sources:
  - `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/article-card.md`
  - `docs/agent-engineering-course/README.md`
  - `docs/agent-engineering-course/status.md`
  - Unity BuildReport API: `https://docs.unity.cn/2022.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html`
  - Unity Asset Database manual: `https://docs.unity.cn/Manual/AssetDatabase.html`
  - Unity Addressables Analyze: `https://docs.unity.cn/Packages/com.unity.addressables%402.9/manual/analyze-addressables-window-reference.html`
- Source date/access: local course files and Unity docs accessed `2026-08-29 Asia/Shanghai`.
- Direct support: The Article 25 card freezes the BuildPilot requirement-change allocation: Host receives owner/project interaction, BuildPilot business logic picks domain checks, Runtime runs read-only task/model/tool steps, Harness applies identity/permission/evidence/budget/trace/approval/recovery, owner implements outside BuildPilot, Runtime re-verifies and Harness stores audit result. Unity docs show plausible read-only evidence surfaces such as build reports, asset database/import dependency knowledge and Addressables analysis, but no BuildPilot run exists.
- Counter-evidence searched: there is no BuildPilot implementation artifact, runtime trace, Unity scan output or production deployment evidence for Article 25.
- Proves: BuildPilot can be used as a course design case for responsibility allocation.
- Does not prove: BuildPilot works, ran, modified a Unity project, improved a build or produced real diagnostics.
- Limitations: proposal-only, no lab, no runtime observation, no experiment.
- Downstream wording ceiling: always include `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST` meaning. The owner implements changes; BuildPilot only suggests and verifies read-only evidence.

## Evidence Gate recommendation

Recommendation: `PASS`.

Evidence Gate checks:

- Core claims covered: `12 / 12`.
- Evidence cards covered: `12 / 12`.
- Blocked core claims: `0`.
- Required Lab: `NONE`.
- Experiment Count: `0`.
- Runtime Observation: `ABSENT`.
- `PARTIAL` claims have explicit wording ceilings and cannot be upgraded without additional direct implementation or runtime evidence.
- `PROPOSAL` claims are clearly labeled as course taxonomy/design, not external facts.

Required Reviewer attention:

- Reject any draft sentence that implies the Runtime/Harness split is an industry standard.
- Reject any draft sentence that says BuildPilot was implemented, run, benchmarked, deployed or used to change a Unity project.
- Reject any Article 25 expansion that writes the Article 26 minimum capability model or Article 27 trade-off/adoption framework.
