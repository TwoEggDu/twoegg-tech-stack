# Article 27 Evidence｜Harness 设计取舍与采用

Status: `READY_FOR_EVIDENCE_GATE / RESEARCHER OWNED`

Gate: `RESEARCH`

Evidence Gate recommendation: `PASS`

Required Lab: `NONE`

Experiment Count: `0`

Runtime Observation: `ABSENT`

BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`

Access date for current hosted sources: `2026-08-30 Asia/Shanghai`

Status mix: `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

## Evidence contract

Article 27 is a principle/trade-off article. Evidence statuses are intentionally conservative:

- `CONFIRMED`: directly supported inside the stated source/version scope.
- `PARTIAL`: supported by official sources plus course synthesis, but not an agent-Harness universal law or runtime result.
- `PROPOSAL`: course adoption model or BuildPilot design choice; downstream text must use design language.
- `BLOCKED`: no core claim remains blocked after this pass.

No card may be used to claim ROI, lower latency, lower cost, reduced defect rate, production safety, BuildPilot runtime behavior or Article 28 / Part VI source behavior.

## Claim Register Snapshot

| Claim ID | Evidence ID | Status | Short claim |
|---|---|---|---|
| `27-C01` | `27-E01` | `PARTIAL` | Shared governance can reduce duplicated cross-cutting concerns, but central layers can create bottleneck, SPOF, coupling, cascading-failure and latency risks. |
| `27-C02` | `27-E02` | `PARTIAL` | Context, trace, evidence, policy and approval add token/storage/retention/privacy/latency/reviewer-attention costs. |
| `27-C03` | `27-E03` | `PARTIAL` | Replaceability should be driven by real variation pressure, not imagined future providers or plugin enthusiasm. |
| `27-C04` | `27-E04` | `PARTIAL` | Policy drift, misconfiguration, stale knowledge, wrong intent and trusted-looking hints can create false safety. |
| `27-C05` | `27-E05` | `PARTIAL` | HITL and recovery are stateful mechanisms that can create approval fatigue and checkpoint/recovery complexity. |
| `27-C06` | `27-E06` | `PROPOSAL` | Stage 0-4 adoption model is a course proposal; stage order is not maturity destiny. |
| `27-C07` | `27-E07` | `PROPOSAL` | Explicit no-build / remain-low-stage cases are valid design outcomes. |
| `27-C08` | `27-E08` | `PROPOSAL` | BuildPilot V1 should remain read-only/suggestion-first and prioritize restricted checks, Evidence, Trace, Change Request and Human Review. |
| `27-C09` | `27-E09` | `CONFIRMED` | Article 27 has Required Lab NONE, experiment 0, runtime observation absent, and BuildPilot not implemented/not run. |
| `27-C10` | `27-E10` | `PARTIAL` | Observability and auditability must be balanced against privacy, secrets, retention and redaction limits. |
| `27-C11` | `27-E11` | `PARTIAL` | Regression/eval/governed capability evolution should expand only after scoped need; no single trace, eval pass or proposal proves production quality. |

## Evidence Cards

### 27-E01｜Centralization value and central bottleneck risk

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C01`
- Claim: Shared governance can reduce duplicated cross-cutting concerns, but central layers can create bottleneck, single-point-of-failure, coupling, cascading-failure and latency risks.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `architecture guidance`
- Source: Microsoft Azure Gateway Aggregation pattern `https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-aggregation`; Azure Microservices architecture style `https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices`; Azure gateway in front of model deployments `https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/azure-openai-gateway-multi-backend`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `Problems and considerations; cross-cutting concerns; just enough implementation`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current hosted Microsoft Learn pages as retrieved.
- Reproduction: read the Gateway Aggregation problems/considerations, microservices gateway/cross-cutting guidance, and AI gateway warning/just-enough sections.
- Observation: Azure guidance says aggregation/offloading can reduce chattiness and centralize concerns, while warning about service coupling, single points of failure, bottlenecks, cascading failures, latency, routing complexity and human-caused gateway configuration risk.
- Counter-evidence Searched: the same sources include valid use cases for gateways and offloading; they do not say centralization is always wrong.
- Interpretation: Harness centralization should be treated as a trade-off: useful for repeated governance drift, risky when it becomes a central queue or fragile routing/control layer.
- Proves: shared layers have both documented benefits and documented architectural costs.
- Does Not Prove: agent-Harness-specific empirical rates, cost savings, defect reduction, or that BuildPilot uses such a gateway.
- Limitations: analogy from architecture/gateway guidance to Agent Harness; status remains `PARTIAL`.
- Course Usage: opening problem space and central bottleneck section.
- BuildPilot Implication: `SIMPLIFY` — prefer a small read-only governance slice before a platform.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E02｜Cost surfaces: token, storage, latency, retention and attention

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C02`
- Claim: Context, trace, evidence, policy and approval add token, storage, retention, privacy, latency and reviewer-attention costs.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC + COURSE_EVIDENCE`
- Source Type: `official SDK documentation + local course evidence`
- Source: OpenAI Agents SDK Running agents `https://openai.github.io/openai-agents-python/running_agents/`; OpenAI Agents SDK Human-in-the-loop `https://openai.github.io/openai-agents-python/human_in_the_loop/`; OpenTelemetry Handling sensitive data `https://opentelemetry.io/docs/security/handling-sensitive-data/`; local Article 20 Budget evidence; local Article 21 Trace evidence.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/20-budget-engineering-token-step-cost-latency/evidence.md`; `docs/agent-engineering-course/articles/21-trace-replay-failure-taxonomy/evidence.md`
- Symbol: `Budget lifecycle; sensitive data; HITL pause/resume`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current hosted docs plus published/local course evidence.
- Reproduction: inspect run config for turn/tracing/tool execution controls, HITL pause/resume state, OTel sensitive-data responsibilities, and prior course budget/trace evidence.
- Observation: OpenAI docs expose turn limits, run config, tool execution and tracing controls; HITL docs require interruption/resume state; OTel warns telemetry can capture sensitive or personal data and requires implementer-owned consent/minimization/storage practices. Article 20 already separates token/step/cost/latency budgets.
- Counter-evidence Searched: small one-shot assistants may not need shared cost ledgers, persistent traces or human review.
- Interpretation: every governance artifact has carrying cost; Article 27 should make those costs explicit before recommending a Harness stage.
- Proves: cost classes exist and must be considered.
- Does Not Prove: concrete token counts, storage size, latency, pricing, reviewer load or savings.
- Limitations: no measurements were run; no provider billing read occurred.
- Course Usage: cost matrix and Stage 0-4 costs.
- BuildPilot Implication: `SIMPLIFY` — use step/time/tool-call caps and minimal evidence records first.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E03｜Replaceability requires real variation pressure

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C03`
- Claim: Replaceability should be driven by real variation pressure, not imagined future providers or plugin enthusiasm.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC + INFERENCE`
- Source Type: `official product documentation`
- Source: LangChain frameworks/runtimes/harnesses `https://docs.langchain.com/oss/python/concepts/products`; MCP 2025-06-18 Tools `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`; MCP Authorization `https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization`; Article 25 and Article 26 evidence.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/evidence.md`; `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/evidence.md`
- Symbol: `vendor terminology variance; capability registry`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current hosted docs and course Article 25/26 local evidence.
- Reproduction: compare LangChain product role table with MCP tool/auth separation and local Article 25/26 responsibility matrices.
- Observation: LangChain names frameworks, runtimes and harnesses as different product layers; MCP models tools and optional authorization in protocol-specific terms; Articles 25/26 already show terminology variance and capability/trust separation.
- Counter-evidence Searched: product docs do contain real harness/framework/runtime categories, so dismissing all boundaries as arbitrary would be false.
- Interpretation: the evidence supports responsibility-based comparison, but not speculative pluginization. A seam is justified by second consumers, second providers, second hosts, migration needs or independently changing lifecycle.
- Proves: names and packaging vary, so replaceability must be contract-based.
- Does Not Prove: a universal plugin architecture or that every capability needs a provider interface on day one.
- Limitations: variation pressure test is a course design heuristic.
- Course Usage: replaceability and extension seam section.
- BuildPilot Implication: `DEFER` — keep fixed read-only capabilities until second implementations appear.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E04｜False safety from drift, misconfiguration, stale knowledge and wrong intent

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C04`
- Claim: Policy drift, misconfiguration, stale knowledge, wrong intent and trusted-looking hints can create false safety.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC + COURSE_EVIDENCE`
- Source Type: `protocol/security docs + repository governance docs`
- Source: MCP Tools `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`; MCP Authorization `https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization`; GitHub rulesets `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets`; GitHub CODEOWNERS `https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners`; Article 15/16/18/19 evidence.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/18-evidence-contract/evidence.md`; `docs/agent-engineering-course/articles/19-permission-approval-hitl-sandbox/evidence.md`
- Symbol: `tool annotations; token audience; stale approvals; evidence/authority boundary`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: MCP protocol revision `2025-06-18`; current GitHub docs; local course evidence.
- Reproduction: read MCP tool annotation security guidance, MCP token audience/security concerns, GitHub stale approval/ruleset behavior and local evidence/permission boundaries.
- Observation: MCP says annotations are untrusted unless from trusted servers and recommends access controls/confirmation/audit logging; MCP authorization includes audience/resource binding and confused-deputy concerns; GitHub can dismiss stale approvals when approved diffs change; course evidence separates knowledge/provenance/authority.
- Counter-evidence Searched: security and review mechanisms reduce risk when configured correctly; the claim is about false safety under drift or misconfiguration, not a claim that governance is useless.
- Interpretation: Article 27 should require stale/unknown/needs-review states rather than treating configuration, memory or approval as permanent truth.
- Proves: trusted-looking metadata, tokens and approvals have scope and drift risks.
- Does Not Prove: any actual BuildPilot misconfiguration or stale knowledge incident.
- Limitations: cross-domain synthesis; no runtime incident corpus.
- Course Usage: false-safety and no-build/rollback sections.
- BuildPilot Implication: `ADOPT` — keep `UNKNOWN / STALE / NOT_PROVEN / NEEDS_REVIEW` labels.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E05｜Approval fatigue and recovery complexity

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C05`
- Claim: HITL and recovery are stateful mechanisms that can create approval fatigue and checkpoint/recovery complexity.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC + COURSE_EVIDENCE`
- Source Type: `official agent/workflow documentation`
- Source: OpenAI Agents SDK HITL `https://openai.github.io/openai-agents-python/human_in_the_loop/`; OpenAI RunState reference `https://openai.github.io/openai-agents-python/ref/run_state/`; LangChain HITL `https://docs.langchain.com/oss/python/langchain/human-in-the-loop`; LangGraph persistence `https://docs.langchain.com/oss/javascript/langgraph/persistence`; Temporal workflow execution `https://docs.temporal.io/workflow-execution`; local Article 11 and 21 evidence.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/11-long-running-agent/evidence.md`; `docs/agent-engineering-course/articles/21-trace-replay-failure-taxonomy/evidence.md`
- Symbol: `RunState; interruptions; checkpointer; workflow history/replay`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current hosted product docs and local course evidence.
- Reproduction: inspect HITL docs for pause/approve/reject/resume; inspect persistence/checkpoint docs; compare with Article 11/21 recovery boundaries.
- Observation: OpenAI and LangChain describe HITL as interruption plus persisted state/resume; LangGraph persistence supports HITL/fault tolerance/time travel; Temporal uses workflow history and replay semantics for durable execution.
- Counter-evidence Searched: some tools can be automatically approved or rejected by policy; not every workflow needs human approval.
- Interpretation: approval and recovery need careful placement. Excess approvals can reduce reviewer attention, and recovery without known/unknown/effect boundaries can repeat unsafe work.
- Proves: HITL/recovery are stateful and carry complexity.
- Does Not Prove: a measured fatigue rate, reviewer throughput loss, or exactly-once recovery.
- Limitations: approval fatigue is a course/organizational inference, not measured in this task.
- Course Usage: approval fatigue, recovery complexity and Stage rollback sections.
- BuildPilot Implication: `ADOPT / SIMPLIFY` — route only meaningful owner decisions; keep recovery to read-only re-check boundaries first.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E06｜Stage 0-4 adoption model

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C06`
- Claim: Stage 0-4 adoption model is a course proposal; stage order is not maturity destiny.
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course synthesis`
- Source: Article 27 card; Article 24-26 published content/evidence; `27-E01` through `27-E05`.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/article-card.md`; current `research.md`
- Symbol: `Stage 0-4 adoption proposal`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current Article 27 research transaction.
- Reproduction: read the Article 27 card requirements and compare positive/negative evidence from 27-E01 through 27-E05.
- Observation: Article 27 card requires a graduated adoption proposal with entry signals, benefits, costs, exit/rollback and no-build cases. The source evidence supports the existence of trade-offs but does not define this exact staging.
- Counter-evidence Searched: no primary source was found that standardizes this exact Stage 0-4 model for Agent Harness adoption.
- Interpretation: the model is acceptable only as course design, not industry maturity standard.
- Proves: internal traceability of the proposed staging to article requirements and evidence.
- Does Not Prove: universal maturity ladder, ROI, implementation correctness or production readiness.
- Limitations: not run, not validated by teams, no survey or experiment.
- Course Usage: main adoption framework.
- BuildPilot Implication: `SIMPLIFY` — start at Stage 1/2.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E07｜No-build and remain-low-stage cases

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C07`
- Claim: Explicit no-build / remain-low-stage cases are valid design outcomes.
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `architecture guidance + course synthesis`
- Source: Azure Gateway Aggregation pattern `https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-aggregation`; LangChain frameworks/runtimes/harnesses `https://docs.langchain.com/oss/python/concepts/products`; NIST AI RMF Core `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/`; Article 27 card.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/article-card.md`
- Symbol: `no-build cases`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current hosted docs plus Article 27 card.
- Reproduction: compare official "when to use / not suitable / considerations" style guidance with Article 27's required no-build scope.
- Observation: Azure patterns include suitability and consideration boundaries; LangChain frames product choices by use case; NIST RMF says risks or characteristics that will not or cannot be measured should be documented.
- Counter-evidence Searched: none of these sources uses the exact phrase "do not build a Harness"; this is course synthesis.
- Interpretation: a responsible adoption model must let teams document why they remain local or decline a Harness.
- Proves: no-build documentation is a defensible course design posture.
- Does Not Prove: which real organization should or should not adopt.
- Limitations: proposal only.
- Course Usage: no-build section and Stage 0 table.
- BuildPilot Implication: `REJECT` — reject Stage 3/4 expansion without observed need.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E08｜BuildPilot restrained V1

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C08`
- Claim: BuildPilot V1 should remain read-only/suggestion-first and prioritize restricted checks, Evidence, Trace, Change Request and Human Review.
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `repository contract + official-doc support`
- Source: Article 24-26 published content/evidence; Article 27 card; GitHub rulesets/CODEOWNERS docs; OpenAI HITL; Unity read-only source surfaces previously recorded in Article 24.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/article-card.md`; Article 24-26 evidence files.
- Symbol: `BuildPilot V1 recommendation`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current Article 27 transaction; BuildPilot design-only.
- Reproduction: read Article 27 card, published Article 24-26 BuildPilot sections and external review/HITL docs.
- Observation: Prior Part V articles repeatedly freeze BuildPilot as read-only/suggestion-first; GitHub and HITL docs support scoped review/approval analogies; Article 24 records Unity read-only evidence surfaces without adapter/runtime evidence.
- Counter-evidence Searched: no BuildPilot implementation, run log, Unity scan, Jenkins call, PR, deployment or benchmark evidence exists in the allowed Article 27 scope.
- Interpretation: V1 should adopt only the smallest governance loop needed by the course case and defer knowledge graph, full eval, governed capability evolution and autonomous changes.
- Proves: the restrained V1 recommendation is internally consistent with course boundaries.
- Does Not Prove: BuildPilot feasibility, benefit, runtime correctness, cost or user adoption.
- Limitations: proposal only.
- Course Usage: BuildPilot adoption section.
- BuildPilot Implication: `ADOPT / SIMPLIFY / DEFER / REJECT` matrix.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E09｜Article reality and forbidden claims

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C09`
- Claim: Article 27 has Required Lab NONE, experiment 0, runtime observation absent, and BuildPilot not implemented/not run.
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Type: `repository-local course contract`
- Source: Article 27 card; Article 27 fresh research brief; course status; production workflow.
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/article-card.md`; `.superpowers/sdd/part-v-codex-prompt/article-27-research-brief.md`; `docs/agent-engineering-course/status.md`; `docs/agent-engineering-course/production-workflow.md`
- Symbol: `Required Lab NONE; Runtime Observation ABSENT; Article 28 forbidden`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current workspace transaction.
- Reproduction: read the Article 27 card and fresh research brief; verify status row says Article 27 is in research and Article 28 remains forbidden.
- Observation: Article 27 card freezes Required Lab `NONE`, BuildPilot `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`, Experiment Count `0` and Runtime Observation `ABSENT`; the fresh brief forbids Article 28/Part VI and runtime/lab/ROI inventions.
- Counter-evidence Searched: inspected Article 27 folder and relevant status/brief files; no lab, runtime, content, outline, BuildPilot implementation or Article 28 production artifact is authorized by this task.
- Interpretation: downstream writing must preserve these labels as hard evidence ceilings.
- Proves: local article reality and non-scope.
- Does Not Prove: future Article 27 publication, Article 28 readiness, or any BuildPilot behavior.
- Limitations: repository-local fact only; future authorized transactions can change state.
- Course Usage: status preface, final evidence boundary, reviewer restrictions.
- BuildPilot Implication: `REJECT` — reject any runtime/benefit/implementation claim.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E10｜Observability versus privacy and secrets

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C10`
- Claim: Observability and auditability must be balanced against privacy, secrets, retention and redaction limits.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `observability/security documentation`
- Source: OpenTelemetry Handling sensitive data `https://opentelemetry.io/docs/security/handling-sensitive-data/`; OpenTelemetry Semantic Conventions `https://opentelemetry.io/docs/specs/semconv/`; OpenAI Agents SDK Tracing `https://openai.github.io/openai-agents-python/tracing/`; NIST AI RMF Core `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `data minimization; sensitive trace payloads; privacy risk; semantic convention stability`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: current hosted docs; OTel page notes moving documentation/stability contexts.
- Reproduction: read OTel sensitive-data guidance and semantic convention overview/stability notes; inspect OpenAI tracing sensitive-data controls and NIST RMF Measure privacy/documentation categories.
- Observation: OTel says telemetry may capture PII, credentials, tokens and user behavior; implementers own consent, minimization, storage and review. OpenAI tracing can include sensitive LLM/tool inputs/outputs unless configured. Semantic conventions aid correlation but can require migration. NIST RMF includes privacy-risk documentation and tracking evolving risks.
- Counter-evidence Searched: observability remains necessary for debugging; the claim is not "do not trace."
- Interpretation: Article 27 should present trace expansion as a governance choice with privacy/retention/redaction cost, not a free reliability upgrade.
- Proves: more telemetry can improve auditability while increasing sensitive-data responsibility.
- Does Not Prove: compliance, anonymization success, safe retention or replay completeness.
- Limitations: no data classification, trace export or redaction test was run.
- Course Usage: observability/privacy trade-off section.
- BuildPilot Implication: `SIMPLIFY` — store references/minimized evidence first; avoid raw broad trace retention.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

### 27-E11｜Eval, regression and governed capability evolution are delayed pressure

- Article: `27 Harness 的设计取舍`
- Claim ID: `27-C11`
- Claim: Regression/eval/governed capability evolution should expand only after scoped need; no single trace, eval pass or proposal proves production quality.
- Evidence Status: `PARTIAL`
- Evidence Class: `COURSE_EVIDENCE + OFFICIAL_DOC`
- Source Type: `local course evidence + official evaluation/risk framework`
- Source: Article 22 evidence; Article 26 evidence; NIST AI RMF Core `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/`; GitHub rulesets `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets`
- Repository: `E:/workspace/TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/22-eval-golden-dataset-regression/evidence.md`; `docs/agent-engineering-course/articles/26-harness-minimum-capability-model/evidence.md`
- Symbol: `Lab06 scope; Eval ceiling; conditional/deferred classification`
- Call Path: `N/A`
- Experiment: `N/A for Article 27`
- Fixture: `N/A for Article 27; prior Lab06 remains fixture-scoped Article 22 evidence`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-30 Asia/Shanghai`
- Version Scope: local Article 22/26 evidence and current NIST/GitHub docs.
- Reproduction: read Article 22 Evidence Gate scope, Article 26 deferred/conditional capabilities, NIST MEASURE uncertainty/documentation text and GitHub gate semantics.
- Observation: Article 22 confirms only fixture-scoped deterministic Lab06 behavior and requires boundaries for stochastic eval; Article 26 defers full eval/governed capability evolution; NIST requires uncertainty/documentation and tracking risks over time; GitHub gates show review/status checks are scoped workflow controls.
- Counter-evidence Searched: regression hooks can be valuable and GitHub gates are real production controls; Article 27 should not reject them universally.
- Interpretation: Harness evolution should add eval/regression/capability-governance only when the system has a real repeatability, release or multi-capability promise.
- Proves: expansion has prerequisites and claim ceilings.
- Does Not Prove: any BuildPilot eval run, production quality, statistical significance, or capability evolution implementation.
- Limitations: Article 27 itself runs no lab; prior Lab06 cannot be generalized.
- Course Usage: Stage 3/4 entry signals and defer/reject matrix.
- BuildPilot Implication: `DEFER` — add full eval and governed capability evolution after real usage proves need.
- Owner: `Article 27 Researcher`
- Verified At: `2026-08-30`

## Evidence Classification Summary

| Status | Count | Claims |
|---|---:|---|
| `CONFIRMED` | 1 | `27-C09` |
| `PARTIAL` | 7 | `27-C01`, `27-C02`, `27-C03`, `27-C04`, `27-C05`, `27-C10`, `27-C11` |
| `PROPOSAL` | 3 | `27-C06`, `27-C07`, `27-C08` |
| `BLOCKED` | 0 | None |

## Evidence Gate recommendation

Recommendation: `PASS`.

Evidence Gate checks:

- Core claims covered: `11 / 11`.
- Evidence cards covered: `11 / 11`.
- Blocked core claims: `0`.
- Required Lab: `NONE`.
- Experiment Count: `0`.
- Runtime Observation: `ABSENT`.
- BuildPilot remains `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
- Stage 0-4 adoption model is explicitly `PROPOSAL`; it is not an external maturity standard.
- `PARTIAL` claims have narrowed wording ceilings and cannot be upgraded without runtime, implementation, organizational or stronger direct evidence.

Required downstream restrictions:

- Do not claim Harness is mandatory, mature-by-stage, industry-standard or universally cost-effective.
- Do not claim centralization always improves safety, latency, cost or quality.
- Do not invent ROI, cost, token, storage, latency, reviewer-throughput or defect-reduction numbers.
- Do not claim BuildPilot exists, runs, scans Unity/Jenkins, creates PRs, modifies code, deploys or verifies production behavior.
- Do not start Article 28, Part VI, DeepSeek Harness source reading or BuildPilot implementation architecture.
