# Article 26 Research｜Harness 最小能力模型

## Research status

- Gate: `RESEARCH`
- Researcher: `REAL_SUBAGENT / FRESH CONTEXT / RETRY1`
- Access date: `2026-08-29 Asia/Shanghai`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Evidence Gate Recommendation: `PASS`
- Claim Coverage: `11 / 11`
- Evidence Cards: `11 / 11`
- Status Mix: `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

## Boundary statement

Article 26 consumes Article 24's shared-governance pressure and Article 25's Runtime / Harness / Host / business boundary. It does not repeat why Harness is needed, does not re-litigate whether Runtime and Harness are separate, and does not write Article 27's adoption, cost or bloat framework.

The minimum model below is not a vendor feature menu. Official sources show useful pieces: Microsoft Agent Framework's `Agent Harness` bundles planning, todos, history, approvals and terminal UX; OpenAI Agents SDK exposes sessions, guardrails, sandbox capabilities and runner state; MCP defines tool discovery/call schemas and warns that tool annotations are untrusted unless from trusted servers; OpenTelemetry defines trace/span/event/status semantics; Temporal documents retry and durable execution constraints; GitHub CODEOWNERS/branch protection shows ownership and review gates. Those sources support the responsibility areas, but the final "minimum core" classification is a course model and must be worded as such.

## Source manifest

### Local course sources

- `docs/agent-engineering-series-plan.md` — canonical Part V row: Article 26 owns the minimum capability model; Article 27 owns trade-off/adoption.
- `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/article-card.md` — frozen problem space, core questions, BuildPilot boundary and Required Lab `NONE`.
- `D:/DownLoad/part-v-codex-prompt.md` sections 8 / 10 / 11 — Article 26 capability candidates, Evidence contract and cross-article duplicate guard.
- `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md` — published Article 25 boundary and explicit Article 26 non-scope.
- `docs/agent-engineering-course/glossary.md` — course definitions for Harness, Runtime, Capability, Session, Checkpoint, Recovery, Evidence, Trace, Replay, Eval.
- Prior local articles used as continuity, not new external proof: Articles 06, 07, 10, 11, 18, 19, 20, 21 and 22.

### External primary sources

- Microsoft Agent Framework — Step 6 Agent Harness, Tool approvals, Workflow HITL; docs last updated `2026-08-25`, accessed `2026-08-29`.
- OpenAI Agents SDK — Agents, Tools, Sessions, Guardrails, Sandbox agents; current hosted docs accessed `2026-08-29`; sandbox docs mark the feature `Beta`.
- Model Context Protocol specification `2025-06-18` — Tools and Schema Reference; accessed `2026-08-29`.
- OpenTelemetry Specification `1.60.0` — Trace API, Overview and Baggage; accessed `2026-08-29`.
- Temporal documentation / documentation source — durable execution, replay, activity timeout and retry policy; accessed `2026-08-29`.
- GitHub Docs — CODEOWNERS and branch protection review/status rules; accessed `2026-08-29`.

## Invariants first

The Harness minimum exists only if it protects durable invariants across more than one model call, tool, run, workflow or owner decision. Article 26 uses these invariants:

| Invariant | If broken | Capability pressure |
|---|---|---|
| `I1 Stable actor / session / ownership` | A later run cannot explain who acted, for whom, or under which task boundary. | Identity, Session, Ownership Ledger |
| `I2 Capability visibility is not capability authority` | A listed tool or skill can be mistaken for a safe, relevant and authorized action. | Capability Registry, Version, Trust Filter |
| `I3 Context provenance and isolation survive compaction/reuse` | Old, sensitive or out-of-scope material can be silently reused. | Context Policy Envelope |
| `I4 Authority is checked at use time` | A UI approval, stale policy or broad permission becomes a blank cheque. | Permission, Approval, Sandbox, Policy Enforcement |
| `I5 Observation is not accepted evidence` | Tool output, logs or traces become conclusions without an acceptance rule. | Evidence Contract, Trace Linkage |
| `I6 Recovery starts from known / unknown separation` | Retry, resume or replay can duplicate side effects or hide missing state. | Checkpoint/Recovery Boundary |
| `I7 Scarce resources have stop semantics` | A multi-step run burns token, time, cost or tool calls without a shared stop rule. | Budget/Step/Cost/Latency Control |
| `I8 Human decision is a state transition, not a chat aside` | Review, rejection, clarification or adoption loses scope and cannot be audited. | HITL, Change Request, Intent Confirmation |
| `I9 Knowledge has source, freshness and intake policy` | RAG/memory turns stale or untrusted material into current project fact. | Knowledge Provenance/Freshness Controls |
| `I10 Regression is separate from one successful run` | A fixed behavior can regress without a stable test/eval hook. | Eval/Golden/Regression Hook |

## Ten candidate areas classified

| Candidate area from Part V prompt | Article 26 classification | Rationale / wording ceiling |
|---|---|---|
| Identity / Session / Ownership | `MINIMUM CORE` | Without a stable actor/session/task boundary, no later permission, trace, evidence or recovery record is attributable. |
| Context Assembly and isolation | `MINIMUM CORE for policy and isolation; Runtime owns concrete assembly` | Harness must define what can be exposed, retained, compacted, cited or reused. Runtime may perform the actual packing. |
| Tool/Skill Capability Registry and version | `MINIMUM CORE` | MCP proves discovery/call/schema are explicit protocol facts; tool annotations are not trusted authority. |
| Permission, Approval, Sandbox and Policy Enforcement | `MINIMUM CORE` | Any Harness that exposes external actions needs a deny-first use-time policy path; implementation depth varies by risk. |
| Execution Control, State, Checkpoint and Recovery | `MINIMUM CORE as boundary contract; durable checkpoint engine is CONDITIONAL CORE` | Harness does not own every step loop, but must define stop/resume/retry/recover semantics that Runtime obeys. |
| Trace, Evidence, Replay and Failure Taxonomy | `MINIMUM CORE for Trace/Evidence/Failure layer; Replay is CONDITIONAL CORE` | Trace/evidence/failure labels are needed for audit. Full replay depends on determinism, environment and side effects. |
| Budget / Step / Cost / Latency Control | `CONDITIONAL CORE` | Mandatory when runs are long, paid, rate-limited, risky or user-visible in latency; not mandatory for a tiny local one-shot assistant. |
| Human-in-the-loop and Change Request | `CONDITIONAL CORE; MINIMUM CORE for BuildPilot` | Suggestion-first production work requires owner review and scoped change requests; low-risk informational agents may defer it. |
| Evaluation, Golden Cases and Regression hook | `ENVIRONMENT-SPECIFIC EXTENSION, often DEFERRED from the first Harness slice` | The hook should exist once behavior becomes relied upon, but Article 26 should not force a full eval platform into the minimum. |
| Knowledge provenance, freshness and Intent confirmation | `CONDITIONAL CORE` | Required whenever memory/RAG/project knowledge affects action or claim. For BuildPilot, intent confirmation and source/freshness are core. |

## Minimum capability model

| Capability | Problem / invariant | Inputs | Outputs | Dependencies | Trust boundary | Failure / degradation | Observable evidence | Interfaces |
|---|---|---|---|---|---|---|---|---|
| `A. Identity + Session + Ownership Ledger` | Protects `I1`; later decisions must know actor, owner, task, session and scope. | user/owner identity, task id, session id, host/workspace id, timestamp, prior session reference. | session envelope, ownership record, actor binding, continuation boundary. | Host identity/session source; Runtime run id; policy store. | Host/UI state is not sufficient authority; session product objects are not automatically course `Session`. | Missing actor/session => fail closed; ambiguous owner => ask; stale session => start new boundary or require confirmation. | session record, owner decision record, trace correlation id. | Runtime consumes run/session id; business Agent reads owner goal; Tool receives scoped actor; Workflow stores task boundary; Policy checks actor/scope; KB/RAG tags source scope. |
| `B. Capability Registry + Version + Trust Filter` | Protects `I2`; tool existence cannot equal visibility, relevance or authority. | tool/skill/MCP descriptors, schema, version, source trust, environment, actor/risk profile. | allowed capability view, denied/hidden list, version pin or freshness warning. | Host registry, MCP/tool servers, policy engine, version metadata. | Descriptors and annotations from untrusted servers are hints, not authority. | Unknown version/source => hide or require review; schema mismatch => block call; missing capability => report gap. | registry snapshot, selected capability id/version, denied reason. | Runtime dispatches only allowed view; business Agent selects relevance; Tool provides schema; Workflow references capability id; Policy filters; KB/RAG records source/version. |
| `C. Context Policy Envelope` | Protects `I3`; context assembly must obey exposure, provenance, isolation, retention and compaction rules. | task scope, candidate context items, source refs, sensitivity, freshness, token budget, reuse policy. | model-visible context plan, excluded items, citation/receipt requirements, compaction/reuse limits. | Host file/session inputs, Runtime context assembler, KB/RAG retriever, budget control. | Retrieved, remembered or previous-run material is not current truth without policy. | Missing provenance => include as unknown or exclude; over budget => degrade with receipt; sensitive/out-of-scope => redact/block. | context receipt, source list, exclusion/unknown record. | Runtime packs; business Agent prioritizes; Tool/RAG supplies observations; Workflow binds step need; Policy enforces; KB/RAG supplies provenance/freshness. |
| `D. Authority Gate: Permission + Approval + Sandbox + Policy` | Protects `I4`; every external action needs use-time authority under current policy. | actor, capability/action, resource, frozen request digest, parameters, risk class, approval state, sandbox limits. | allow/deny/approval-required, scoped approval request/response, sandbox execution envelope. | Identity ledger, capability registry, Host UI, policy engine, sandbox client/runtime. | Approval UI event is not authority unless bound to actor/action/resource/scope/expiry/request digest. | Deny by default on missing policy/approval; stale approval => ask again; sandbox mismatch => block or downgrade to read-only. | policy decision id, approval record, sandbox manifest/scope, denied reason. | Runtime asks before dispatch; business Agent receives decision; Tool executes only scoped call; Workflow pauses/resumes; Policy owns decision; KB/RAG cannot grant authority. |
| `E. Trace + Evidence + Failure Layer` | Protects `I5`; occurrence, observation and accepted claim must remain separate. | run/step/tool events, observations, claim ids, evidence rules, failure layer taxonomy, source refs. | trace events/spans, observation references, evidence status, failure classification, unknown list. | Runtime events, Tool results, OpenTelemetry-like trace model, Evidence contract. | Trace/log presence is not proof; tool success is not business acceptance. | Incomplete trace => lower evidence status; unaccepted observation => keep as observation; unclear failure layer => classify unknown. | trace/span/event ids, evidence card, claim register, failure layer. | Runtime emits events; business Agent cites accepted evidence only; Tool returns observation; Workflow records state transition; Policy may require evidence; KB/RAG ingests accepted records only. |
| `F. Checkpoint + Recovery Decision Boundary` | Protects `I6`; resume/retry/replay must start from known/unknown and side-effect boundary. | committed state, in-flight action, last known evidence, approvals, budget, capability versions, continuation reason. | resume/retry/reconcile/compensate/ask/stop decision, recovery preconditions, checkpoint pointer. | Runtime/workflow state, Identity ledger, Authority Gate, Trace/Evidence, optional durable workflow engine. | Checkpoint file is not safe replay by itself; replay needs deterministic inputs/environment and side-effect rules. | Missing in-flight identity => stop/ask; side-effect uncertain => reconcile before retry; version drift => require review. | checkpoint record, recovery decision record, replay eligibility flag. | Runtime performs resume/retry; business Agent restructures report; Tool side effects are reconciled; Workflow stores checkpoint; Policy decides retry authority; KB/RAG receives final accepted lesson. |
| `G. Budget / Step / Cost / Latency Control` | Protects `I7`; long or paid runs need admission, accounting and stop/degrade semantics. | token/cost/time/step estimates, actual usage, latency deadline, risk tier, user budget. | budget envelope, reservation, actual ledger, stop/degrade/ask decision. | Runtime usage, provider/tool cost signals, policy, trace. | Budget grant is not permission or evidence. | Unknown estimate => conservative cap; exhausted budget => stop/degrade/ask; duplicate retry => account separately unless policy says otherwise. | budget ledger, stop reason, usage deltas. | Runtime checks before/after steps; business Agent reports partial scope; Tool usage counted; Workflow gates long branches; Policy owns thresholds; KB/RAG can be curtailed by freshness/value. |
| `H. HITL + Change Request + Intent Confirmation` | Protects `I8`; human review must become scoped state, not loose chat. | ambiguity/finding, current evidence, proposed change request, options, owner identity, expiry/review policy. | clarification request, approval/rejection, change request, owner decision, review trail. | Host UI, business Agent report, Authority Gate, Workflow pause/resume. | Human text does not authorize unrelated actions; owner implementation remains outside BuildPilot. | Ambiguous intent => ask before action; rejection => stop or revise suggestion; no response => wait/expire. | change request record, review decision, re-verification request. | Runtime pauses/resumes; business Agent prepares CR; Tool remains read-only unless separately authorized; Workflow routes review; Policy stores approval; KB/RAG ingests final accepted decision with provenance. |
| `I. Knowledge Provenance / Freshness / Intake Control` | Protects `I9`; memory/RAG must not turn stale or untrusted context into current fact. | source uri, retrieval time, authoritativeness, freshness rule, intent link, acceptance status. | cited knowledge item, stale/unknown marker, intake/update candidate, rejection reason. | KB/RAG system, Evidence contract, Context Policy, owner review. | Memory and RAG retrieval are not current project proof by default. | Stale source => label or refresh; low trust => do not use for authority; conflicting sources => preserve conflict. | source manifest, freshness stamp, accepted/rejected intake record. | Runtime retrieves; business Agent uses with caveat; Tool/RAG supplies content; Workflow may gate intake; Policy limits access; KB owns storage and freshness. |
| `J. Eval / Golden / Regression Hook` | Protects `I10`; repeatable behavior needs a regression path once relied upon. | scenario id, golden input/output/rubric, trace/evidence reference, environment/version. | regression hook, eval result or not-run marker, drift finding. | Eval framework, CI or workflow, evidence/trace corpus. | One demo or one successful run is not regression evidence. | No golden case => do not claim regression coverage; flaky eval => downgrade; env mismatch => not comparable. | eval manifest/result, skipped/not-run marker, failing case. | Runtime may execute; business Agent uses result as evidence only after acceptance; Tool provides check outputs; Workflow schedules; Policy may require gate; KB stores lessons. |

Minimum model wording: A small Harness does not need every advanced implementation on day one, but it must have enough contract surface to answer these questions: who is acting, what capability is visible and allowed, what context is in scope, why an action is authorized, what happened, what can be claimed, where recovery resumes, when to stop, when to ask a human, and whether knowledge/regression claims are current.

## BuildPilot minimum loop

BuildPilot remains design-only. Its minimum closed loop for a read-only requirement-change analysis is:

1. `Requirement intake` — Host records owner request, workspace/project boundary and session/actor. Harness marks `READ_ONLY / SUGGESTION_FIRST`; business Agent parses candidate intent and ambiguity.
2. `Intent confirmation` — if requirement, target platform, file scope or acceptance question is ambiguous, Harness routes a clarification before tool work. No modification authority is granted.
3. `Capability discovery` — Registry exposes only read-only capabilities relevant to source/config/build-report/log inspection. Unknown or write-capable tools stay hidden or require separate approval.
4. `Restricted checks` — Runtime runs permitted reads; Tool Runtime returns observations. Context Policy records source, freshness and exclusion decisions.
5. `Finding` — business Agent turns observations into candidate findings. Evidence layer labels `OBSERVED / INFERRED / UNKNOWN / NOT_PROVEN`.
6. `Change Request` — BuildPilot produces an evidence-backed suggestion with impact, risk, proposed owner action and re-verification plan. It does not edit code, create PRs, run Jenkins or deploy.
7. `Human Review` — owner accepts, rejects, asks for revision or implements externally. Harness stores decision scope and expiry.
8. `Re-verification` — Runtime reruns allowed read-only checks after external owner action when in scope; Evidence records what changed and what remains unknown.
9. `Evidence and knowledge intake` — accepted findings and decisions enter the knowledge layer with provenance/freshness; rejected or uncertain claims remain excluded or marked unknown.

Deferred for Article 26 / BuildPilot V1: autonomous code modification, branch/PR creation, production deployment, full eval platform, governed capability evolution, multi-project knowledge graph, cost optimization strategy and Article 27 adoption staging.

## Claim Register

| Claim ID | Status | Evidence | Research claim | Wording ceiling |
|---|---|---|---|---|
| `26-C01` | `PROPOSAL` | `26-E01` | The Article 26 minimum must be derived from cross-run invariants, not a vendor feature checklist. | Present as course method, not industry standard. |
| `26-C02` | `PARTIAL` | `26-E02` | Identity / Session / Ownership is minimum core because every authority, trace, evidence and recovery record needs attribution and scope. | Source-supported synthesis; do not equate with any single SDK session object. |
| `26-C03` | `PARTIAL` | `26-E03` | Capability Registry + Version + Trust Filter is minimum core; existence/visibility/relevance/authority/execution/evidence are separate questions. | MCP confirms schema/discovery and untrusted annotations; course adds versioned governance semantics. |
| `26-C04` | `PARTIAL` | `26-E04` | Context Policy is minimum core even when Runtime performs concrete context assembly. | Do not claim every framework has a separate context-policy module. |
| `26-C05` | `PARTIAL` | `26-E05` | Permission, Approval, Sandbox and Policy Enforcement form a deny-first authority gate. | Product flows differ; minimum is the boundary contract, not a full IAM platform. |
| `26-C06` | `PARTIAL` | `26-E06` | Trace + Evidence + Failure Layer is minimum core; replay is conditional. | Trace is not evidence acceptance; full replay needs stronger deterministic evidence. |
| `26-C07` | `PARTIAL` | `26-E07` | Checkpoint + Recovery Decision Boundary is minimum core as a stop/resume/retry contract; durable workflow implementation is conditional. | Checkpoint does not imply safe replay or side-effect safety. |
| `26-C08` | `PARTIAL` | `26-E08` | Budget / Step / Cost / Latency is conditional core for long, paid, rate-limited or latency-sensitive runs. | Not mandatory for every low-risk one-shot assistant. |
| `26-C09` | `PARTIAL` | `26-E09` | HITL + Change Request + Intent Confirmation is conditional core generally and minimum core for BuildPilot's suggestion-first workflow. | Human decision must be scoped; BuildPilot owner implements externally. |
| `26-C10` | `PROPOSAL` | `26-E10` | Knowledge provenance/freshness and Eval/Regression hooks should be admitted only when the Harness uses knowledge or promises repeatability. | Conditional/deferred classification is course design; no runtime evidence. |
| `26-C11` | `PROPOSAL` | `26-E11` | BuildPilot's minimum closed loop can map intake -> discovery -> restricted checks -> finding -> CR -> review -> re-verification -> evidence/knowledge intake. | `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`. |

## Counter-evidence and limitations

- Microsoft Agent Framework is direct terminology counter-evidence: its `Agent Harness` includes planning, todo tracking, history persistence and approval UX that this course may split among Runtime, Harness and Host.
- OpenAI `SandboxAgent` uses `Capability` for sandbox-native behavior; the course `Capability` is a governed ability contract, so wording must not conflate them.
- MCP tool annotations include useful read-only/destructive/idempotent/open-world hints, but the spec says clients should not base decisions on untrusted annotations. A Harness must therefore add trust and policy interpretation.
- OpenTelemetry Trace APIs support correlation, spans, events and status; they do not provide claim acceptance, authorization or replay safety by themselves.
- Temporal durable execution/retry sources show recovery mechanisms, but do not imply every Agent Harness needs Temporal or full deterministic replay.
- GitHub CODEOWNERS and branch protection demonstrate ownership/review/status-gate patterns, not a general Agent Harness standard.
- No Article 26 lab was required or run. No BuildPilot code exists, no Unity project was scanned, no Jenkins job was called, no PR/change was created, and no production outcome was observed.

## Evidence Gate recommendation

Recommendation: `PASS`.

Reasoning:

- `11 / 11` claims map to `11 / 11` Evidence Cards.
- `0` core claims are `BLOCKED`.
- `PARTIAL` claims have explicit wording ceilings.
- `PROPOSAL` claims are clearly marked as course model or BuildPilot design case.
- Required Lab is `NONE`; Experiment Count is `0`; Runtime Observation is `ABSENT`.
- Article 27 trade-off/adoption, Part VI DeepSeek Harness source claims and Part VII BuildPilot implementation remain out of scope.

Downstream Author/Reviewer guardrail: reject any draft sentence that turns this minimum model into an external standard, treats BuildPilot as implemented, treats tool visibility as authority, treats Trace as accepted Evidence, or claims full replay/regression coverage without a future lab/source/runtime artifact.
