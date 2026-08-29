# Article 26 Evidence｜Harness 最小能力模型

## Evidence status

- Gate: `RESEARCH`
- Evidence Gate Recommendation: `PASS`
- Cards: `11`
- Core Blocked Claims: `0`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`
- Access date: `2026-08-29 Asia/Shanghai`
- Status Mix: `0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

## Evidence contract

Each `26-Enn` card maps to exactly one `26-Cnn` claim. Evidence statuses mean:

- `CONFIRMED`: directly supported by current primary documentation or local published course artifacts, with limited wording.
- `PARTIAL`: supported by primary sources plus course synthesis; wording must remain narrowed.
- `PROPOSAL`: course design model or BuildPilot design case; not implemented or run.
- `BLOCKED`: unavailable, contradictory or unsafe to claim. Article 26 has `0` blocked core claims after this research pass.

## Claim Register Snapshot

| Claim ID | Evidence ID | Status | Short claim |
|---|---|---|---|
| `26-C01` | `26-E01` | `PROPOSAL` | Minimum model is derived from invariants, not vendor menus. |
| `26-C02` | `26-E02` | `PARTIAL` | Identity / Session / Ownership is minimum core. |
| `26-C03` | `26-E03` | `PARTIAL` | Capability registry/version/trust filtering is minimum core. |
| `26-C04` | `26-E04` | `PARTIAL` | Context policy is minimum core; concrete assembly can remain Runtime-owned. |
| `26-C05` | `26-E05` | `PARTIAL` | Permission/approval/sandbox/policy form a deny-first authority gate. |
| `26-C06` | `26-E06` | `PARTIAL` | Trace/evidence/failure layer is minimum core; replay is conditional. |
| `26-C07` | `26-E07` | `PARTIAL` | Recovery boundary is minimum core; durable checkpoint engine is conditional. |
| `26-C08` | `26-E08` | `PARTIAL` | Budget/step/cost/latency control is conditional core. |
| `26-C09` | `26-E09` | `PARTIAL` | HITL/change request/intent confirmation is conditional core and BuildPilot-core. |
| `26-C10` | `26-E10` | `PROPOSAL` | Knowledge and eval/regression controls are conditional/deferred by use case. |
| `26-C11` | `26-E11` | `PROPOSAL` | BuildPilot minimum loop is a design-only, read-only, suggestion-first case. |

## Evidence Cards

### 26-E01｜Invariant-derived minimum, not vendor menu

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C01`
- Claim: Article 26 should derive the Harness minimum from cross-run invariants rather than copying a vendor feature list.
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course canonical / official-doc counter-evidence`
- Source: `docs/agent-engineering-series-plan.md`; `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/article-card.md`; `D:/DownLoad/part-v-codex-prompt.md`; Microsoft Agent Framework Agent Harness `https://learn.microsoft.com/en-us/agent-framework/get-started/harness`; OpenAI Agents SDK Sandbox `https://openai.github.io/openai-agents-python/sandbox/guide/`
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/article-card.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: Microsoft docs last updated `2026-08-25`; OpenAI current hosted docs, sandbox `Beta`; course plan current working tree.
- Reproduction: `Read canonical Article 26 card and Part V section 8; compare with Microsoft/OpenAI product surfaces.`
- Observation: Canonical prompt says Article 26 must first define invariants and then decide which candidate areas are minimum core vs extensions. Microsoft and OpenAI docs bundle several capabilities differently under product surfaces.
- Counter-evidence Searched: Microsoft `Agent Harness` bundles planning/todos/history and approval UX; OpenAI `SandboxAgent` uses capability terminology differently. These prevent vendor-name standardization.
- Interpretation: The source set supports a course design method: derive minimum responsibilities from invariants, while treating vendor menus as examples/counter-evidence.
- Proves: The article may use invariant-first classification as a course model.
- Does Not Prove: An external industry standard for the exact Article 26 minimum.
- Limitations: No runtime or implementation evidence; status remains `PROPOSAL`.
- Course Usage: Opening, model setup, candidate classification.
- BuildPilot Implication: `N/A` — protects wording boundary.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E02｜Identity / Session / Ownership

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C02`
- Claim: Identity / Session / Ownership is minimum core because authority, trace, evidence and recovery records need actor, owner, task and session scope.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official SDK docs / official product docs / local glossary`
- Source: OpenAI Agents SDK Sessions `https://openai.github.io/openai-agents-python/sessions/`; Microsoft Agent Framework Agent Harness `https://learn.microsoft.com/en-us/agent-framework/get-started/harness`; GitHub CODEOWNERS `https://docs.github.com/en/enterprise-server@3.20/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners`; `docs/agent-engineering-course/glossary.md`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: Microsoft docs last updated `2026-08-25`; GitHub Enterprise Server `3.20` page; OpenAI current hosted docs.
- Reproduction: `Read source pages for session persistence, harness session state and code-owner review ownership.`
- Observation: OpenAI Sessions store conversation history for a specific session and support resuming paused approval runs with the same session. Microsoft harness examples carry plan/todos/history through an `AgentSession`. GitHub CODEOWNERS defines responsible owners and can require owner review before merge.
- Counter-evidence Searched: OpenAI session is conversation memory, not the course's full Session definition. GitHub code ownership is repository-specific, not an Agent Harness model.
- Interpretation: Stable identity/session/ownership is a necessary attribution layer in the course model, but external sources do not standardize the exact ledger.
- Proves: Article 26 can mark the attribution ledger as minimum core.
- Does Not Prove: A universal `Session` object shape or that CODEOWNERS equals Harness ownership.
- Limitations: Cross-source synthesis; no BuildPilot runtime record.
- Course Usage: Minimum capability `A`.
- BuildPilot Implication: `ADOPT` — BuildPilot intake must bind owner request, workspace and session before analysis.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E03｜Capability registry, version and trust filtering

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C03`
- Claim: Capability registry/version/trust filtering is minimum core; existence, visibility, relevance, authority, execution and evidence must be separate.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_SPEC`
- Source Type: `protocol specification / official SDK docs`
- Source: MCP Tools `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`; MCP Schema `https://modelcontextprotocol.io/specification/2025-06-18/schema`; OpenAI Agents SDK Tools `https://openai.github.io/openai-agents-python/tools/`; OpenAI Agents SDK Sandbox `https://openai.github.io/openai-agents-python/sandbox/guide/`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: MCP protocol revision `2025-06-18`; OpenAI current hosted docs.
- Reproduction: `Read MCP tools/list, tools/call, tool definition and ToolAnnotations sections.`
- Observation: MCP servers declare tool capability, clients list tools with schemas, clients call tools by name/arguments, and tool annotations are hints that clients must not trust unless from trusted servers. OpenAI documents multiple tool categories and sandbox-native capabilities.
- Counter-evidence Searched: Product SDKs may show tools as one convenient list; OpenAI `Capability` in sandbox docs is not identical to course `Capability`.
- Interpretation: A Harness must not expose raw tool lists directly as authorization. It needs trusted/versioned capability views before Runtime dispatch.
- Proves: Tool/capability discovery is explicit and not sufficient for authority or evidence.
- Does Not Prove: A full registry product, governed evolution process or Part VI DeepSeek Harness design.
- Limitations: Version field semantics are course governance synthesis beyond MCP's basic tool schema.
- Course Usage: Minimum capability `B`.
- BuildPilot Implication: `ADOPT` — BuildPilot V1 should expose only read-only source/config/log/build-report capabilities with versions/trust notes.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E04｜Context policy envelope

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C04`
- Claim: Context Policy is minimum core even if Runtime owns concrete context assembly.
- Evidence Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Source Type: `official SDK docs / local course evidence`
- Source: OpenAI Agents SDK Sessions `https://openai.github.io/openai-agents-python/sessions/`; OpenAI Agents SDK Sandbox `https://openai.github.io/openai-agents-python/sandbox/guide/`; OpenTelemetry Baggage `https://opentelemetry.io/docs/specs/otel/baggage/api/`; local Articles 12 and 13.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: OpenAI current hosted docs; OpenTelemetry spec `1.60.0`; local published Article 25.
- Reproduction: `Compare session history retrieval, sandbox manifest/path grants and OTel baggage propagation/security notes.`
- Observation: OpenAI Sessions prepend stored history before new input and store new run items afterward. OpenAI sandbox manifests/path grants define workspace/materialization boundaries and warn that extra path grants are trusted configuration. OTel Baggage is propagated context and includes security/integrity cautions.
- Counter-evidence Searched: These sources document mechanisms, not a universal `ContextPolicy` interface.
- Interpretation: The Harness minimum needs a policy envelope for source, scope, sensitivity, freshness, retention and compaction decisions, while Runtime can still assemble the actual input.
- Proves: Context handling has policy-sensitive boundaries that cannot be reduced to prompt packing.
- Does Not Prove: A complete context engineering platform or that all context policy belongs to one process.
- Limitations: Course synthesis from multiple documented mechanisms.
- Course Usage: Minimum capability `C`.
- BuildPilot Implication: `ADOPT` — BuildPilot findings should cite included/excluded source material and preserve unknowns.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E05｜Authority gate: permission, approval, sandbox, policy

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C05`
- Claim: Permission, approval, sandbox and policy enforcement form a deny-first authority gate for actions.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official SDK docs / official product docs / protocol spec`
- Source: Microsoft tool approval `https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval`; Microsoft workflow HITL `https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop`; OpenAI Agents SDK Guardrails `https://openai.github.io/openai-agents-python/guardrails/`; OpenAI Agents SDK Sandbox `https://openai.github.io/openai-agents-python/sandbox/guide/`; MCP Tools `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`; local Article 19.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/19-permission-approval-hitl-sandbox/evidence.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: Microsoft docs last updated `2026-08-25`; MCP `2025-06-18`; OpenAI current hosted docs, sandbox `Beta`.
- Reproduction: `Read tool approval flow, workflow RequestPort/HITL, guardrail tripwires and sandbox manifest trust boundaries.`
- Observation: Microsoft approval tools can return user-input requests instead of executing; workflow HITL pauses and resumes through typed request/response channels. OpenAI tool guardrails can validate/block before and after function-tool execution; sandbox manifests/path grants define execution boundaries. MCP recommends human visibility/confirmation for tool invocations.
- Counter-evidence Searched: Microsoft harness can add auto-approval middleware, showing implementation variety; OpenAI guardrails do not apply uniformly to every hosted/built-in tool path.
- Interpretation: Article 26 can define a minimum authority gate contract without promising a full IAM/sandbox platform.
- Proves: Approval/policy/sandbox controls are real, action-constraining concerns.
- Does Not Prove: Production security, complete sandbox escape resistance, or BuildPilot write authority.
- Limitations: Multiple products expose different control surfaces; status remains `PARTIAL`.
- Course Usage: Minimum capability `D`.
- BuildPilot Implication: `ADOPT` — read-only/suggestion-first must be enforced at capability and action time.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E06｜Trace, evidence and failure layer

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C06`
- Claim: Trace/evidence/failure classification is minimum core, while full replay is conditional.
- Evidence Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Source Type: `observability specification / local course evidence`
- Source: OpenTelemetry Trace API `https://opentelemetry.io/docs/specs/otel/trace/api/`; OpenTelemetry Overview `https://opentelemetry.io/docs/specs/otel/overview/`; local Articles 18, 21 and 22.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/21-trace-replay-failure-taxonomy/evidence.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: OpenTelemetry Specification `1.60.0`; local course Articles 18/21/22 already published.
- Reproduction: `Read OTel SpanContext/Span/Event/Status semantics and compare with local Evidence/Trace articles.`
- Observation: OTel spans have trace/span identifiers, events, attributes and status. OTel status can be `Unset`, `Ok` or `Error`; events preserve operation detail, but OTel does not define claim acceptance. Local course articles separate Evidence, Trace, Replay, Failure Taxonomy and Eval.
- Counter-evidence Searched: Observability vendors may expose traces as debugging proof, but OTel itself does not make logs accepted evidence.
- Interpretation: A minimum Harness needs trace/evidence linkage and failure-layer vocabulary; it should not claim full replay or correctness from trace alone.
- Proves: Audit records and claim acceptance need different semantics.
- Does Not Prove: Article 26 runtime observation, complete replay, or regression coverage.
- Limitations: Combined Trace/Evidence/Failure layer is course synthesis.
- Course Usage: Minimum capability `E`.
- BuildPilot Implication: `ADOPT` — findings must carry observation/evidence status and unknowns.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E07｜Checkpoint and recovery decision boundary

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C07`
- Claim: A recovery boundary is minimum core, but a full durable checkpoint/replay engine is conditional.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official workflow docs / local lab evidence`
- Source: Microsoft workflow HITL `https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop`; Temporal activity timeouts/retry docs source `https://github.com/temporalio/documentation/blob/main/docs/develop/typescript/activities/timeouts.mdx`; Temporal durable execution tutorial source `https://github.com/temporalio/temporal-learning/blob/main/docs/tutorials/java/background-check/durable-execution.mdx`; local Articles 10 and 11.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/11-long-running-agent/evidence.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: Microsoft docs last updated `2026-08-25`; Temporal documentation `main` moving target, accessed `2026-08-29`; local Lab 04 fixture-scoped evidence.
- Reproduction: `Read Microsoft checkpoint/request behavior and Temporal retry/durable execution docs; compare with Article 11 checkpoint boundaries.`
- Observation: Microsoft workflow checkpoints save pending requests and re-emit them on restore. Temporal activity retry policy works with timeouts; durable execution/replay docs tie recovery to event history and deterministic code constraints. Local Article 11 states checkpoint/recovery requires known/unknown and in-flight boundaries.
- Counter-evidence Searched: Temporal's durable model is stronger than many agent harnesses and should not be mandated as the minimum.
- Interpretation: Article 26 can require a recovery decision boundary that tells Runtime when to resume, retry, reconcile, ask or stop; full replay/durable execution is conditional.
- Proves: Recovery must be designed, not reduced to "run again."
- Does Not Prove: Full replay safety, exactly-once side effects or production durability.
- Limitations: No Article 26 fault injection; external workflow docs are not agent-specific proof.
- Course Usage: Minimum capability `F`.
- BuildPilot Implication: `ADOPT` — failed/timeout read-only checks must preserve what is known, unknown and safe to rerun.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E08｜Budget / step / cost / latency conditional core

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C08`
- Claim: Budget/step/cost/latency control is conditional core for long, paid, rate-limited or latency-sensitive runs, not universal mandatory core.
- Evidence Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Source Type: `official SDK docs / local course evidence`
- Source: OpenAI Agents SDK Guardrails `https://openai.github.io/openai-agents-python/guardrails/`; OpenAI Agents SDK Sessions `https://openai.github.io/openai-agents-python/sessions/`; local Article 20 Budget Engineering.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/20-budget-engineering-token-step-cost-latency/evidence.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: OpenAI current hosted docs; local Article 20 published evidence.
- Reproduction: `Read guardrail blocking/parallel execution notes and Article 20 budget model.`
- Observation: OpenAI guardrails can block execution early or run in parallel; blocking can avoid starting a slow/expensive model, while parallel checks may not. Local Article 20 separates estimate, admission, reservation, actual usage and stop semantics.
- Counter-evidence Searched: A tiny one-shot assistant may not need a full shared budget ledger.
- Interpretation: Budget control is not a mandatory minimum for every low-risk agent, but becomes core once cost/time/step/resource constraints can affect user trust or recovery.
- Proves: Article 26 should classify budget as conditional core, not always mandatory.
- Does Not Prove: Any cost saving, latency number, token reduction or BuildPilot runtime budget.
- Limitations: No experiment; no provider usage was measured.
- Course Usage: Capability `G`, conditional classification table.
- BuildPilot Implication: `SIMPLIFY` — BuildPilot V1 can start with step/time/tool-call caps and explicit stop reasons, not a full cost platform.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E09｜HITL, Change Request and Intent Confirmation

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C09`
- Claim: HITL/change request/intent confirmation is conditional core generally and minimum core for BuildPilot's suggestion-first production workflow.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official product docs / repository governance docs`
- Source: Microsoft workflow HITL `https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop`; Microsoft tool approval `https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval`; GitHub CODEOWNERS `https://docs.github.com/en/enterprise-server@3.20/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners`; GitHub branch protection `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule`; local Article 19.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/19-permission-approval-hitl-sandbox/evidence.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: Microsoft docs last updated `2026-08-25`; GitHub docs current/Enterprise Server `3.20`.
- Reproduction: `Read request/response HITL, tool approval, CODEOWNERS review and branch protection checks.`
- Observation: Microsoft workflows can pause on external request events and resume when responses arrive; tool approvals require passing user approval/rejection back to the agent/session. GitHub can request code-owner review and require reviews/status checks before merge.
- Counter-evidence Searched: Informational assistants can answer without change requests; CODEOWNERS is repository governance, not an Agent standard.
- Interpretation: For BuildPilot, human review and scoped change request are not optional because the business boundary is read-only/suggestion-first and owner implements externally.
- Proves: Human decision and review gates can be first-class workflow/governance state.
- Does Not Prove: BuildPilot has implemented review routing, created a PR or modified code.
- Limitations: Cross-domain analogy from GitHub governance to course BuildPilot design.
- Course Usage: Capability `H`; BuildPilot loop steps 1, 2, 6, 7.
- BuildPilot Implication: `ADOPT` — owner review is core to the V1 loop.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E10｜Knowledge and Eval/Regression classification

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C10`
- Claim: Knowledge provenance/freshness and Eval/Regression hooks should be admitted only when the Harness uses knowledge or promises repeatability.
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `local course evidence / official docs`
- Source: local Articles 16 and 22; OpenAI Agents SDK Sessions `https://openai.github.io/openai-agents-python/sessions/`; OpenAI Agents SDK Sandbox `https://openai.github.io/openai-agents-python/sandbox/guide/`; GitHub branch protection `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/managing-a-branch-protection-rule`
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/16-knowledge-base-rag/evidence.md`; `docs/agent-engineering-course/articles/22-eval-golden-dataset-regression/evidence.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: OpenAI current hosted docs; GitHub current docs; local Part III/IV course evidence.
- Reproduction: `Compare local Knowledge/RAG and Eval articles with current session/sandbox/pull-request gate docs.`
- Observation: Local Article 16 already separates retrieval from correctness and freshness; Article 22 separates one result from regression evidence. OpenAI sessions preserve history across runs, while branch protection can require status checks before merging.
- Counter-evidence Searched: Article 26 has no new eval/lab result; a no-memory informational assistant may not need knowledge intake controls.
- Interpretation: The classification is a course design decision: knowledge controls are core when knowledge is used; regression hooks are needed once repeatability is promised, but a full eval platform can be deferred.
- Proves: Article 26 can keep these as conditional/deferred, preserving Article 22 and Article 27 scope.
- Does Not Prove: Any BuildPilot knowledge graph, eval score, golden dataset run or CI result.
- Limitations: Proposal-only classification; no runtime observation.
- Course Usage: Capabilities `I` and `J`; candidate classification table.
- BuildPilot Implication: `SIMPLIFY / DEFER` — keep provenance/freshness for accepted findings; defer full eval/governed capability evolution.
- Owner: `Researcher`
- Verified At: `2026-08-29`

### 26-E11｜BuildPilot minimum closed loop

- Article: `26 Harness 的最小能力模型`
- Claim ID: `26-C11`
- Claim: BuildPilot can illustrate a minimum closed loop from requirement intake through capability discovery, restricted checks, Finding, Change Request, Human Review, re-verification, Evidence and knowledge intake.
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course design / local published boundary`
- Source: `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/article-card.md`; `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md`; `docs/agent-engineering-series-plan.md`; `docs/agent-engineering-course/README.md`
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/research.md`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-29 Asia/Shanghai`
- Version Scope: current working tree; no external runtime.
- Reproduction: `Read Article 26 card and Article 25 published BuildPilot boundary; verify no Article 26 lab/runtime/build artifact exists.`
- Observation: Article 26 card freezes BuildPilot as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`. Article 25 explicitly says owner implements outside BuildPilot and BuildPilot does not create PRs, modify files, call Jenkins or deploy.
- Counter-evidence Searched: Current Article 26 workspace contains only skeleton/research/evidence files; no BuildPilot code, trace, lab observation, Unity scan, Jenkins run, PR or production result exists.
- Interpretation: BuildPilot can be used only as a design case for capability allocation and closed-loop teaching.
- Proves: The article can map a read-only suggestion-first loop as a proposal.
- Does Not Prove: BuildPilot exists, runs, scans Unity, produces correct findings, saves time, reduces defects, or changes code.
- Limitations: Proposal-only; all implementation/performance claims are forbidden.
- Course Usage: BuildPilot section; downstream Author must preserve design-only wording.
- BuildPilot Implication: `ADOPT` as teaching case, `NOT IMPLEMENTED / NOT RUN` as evidence ceiling.
- Owner: `Researcher`
- Verified At: `2026-08-29`

## Evidence Gate recommendation

Recommendation: `PASS`.

Evidence Gate checks:

- Core claims covered: `11 / 11`.
- Evidence cards covered: `11 / 11`.
- Blocked core claims: `0`.
- Required Lab: `NONE`.
- Experiment Count: `0`.
- Runtime Observation: `ABSENT`.
- `PARTIAL` claims have narrowed wording ceilings and cannot be upgraded without implementation, lab or stronger direct source evidence.
- `PROPOSAL` claims are marked as course model/design, not external fact.

Required Reviewer attention:

- Reject any sentence that says the Article 26 model is an industry-standard Harness checklist.
- Reject any sentence that makes every candidate area mandatory without the classification nuance.
- Reject any sentence that treats a visible tool/schema/annotation as authority.
- Reject any sentence that treats Trace, Checkpoint, Replay, Eval or BuildPilot as already implemented or runtime-verified.
- Reject any expansion into Article 27 adoption stages, Part VI DeepSeek Harness source claims or Part VII BuildPilot implementation.
