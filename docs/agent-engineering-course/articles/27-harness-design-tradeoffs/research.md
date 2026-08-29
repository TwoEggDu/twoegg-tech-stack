# Article 27 Research｜Harness 设计取舍与采用

Status: `READY_FOR_EVIDENCE_GATE / RESEARCHER OWNED`

Gate: `RESEARCH`

Research date: `2026-08-30 Asia/Shanghai`

Required Lab: `NONE`

Experiment Count: `0`

Runtime Observation: `ABSENT`

BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`

## Research framing

Article 24 established the pressure for a shared Harness boundary. Article 25 separated Runtime, Harness, Host and Business Agent responsibilities. Article 26 proposed a minimum capability model around Capability, Policy, Session, Trace and Recovery. Article 27 asks the counter-question: even if the model is coherent, when is it worth building, when should it stay small, and when should a team explicitly not build a Harness?

This research keeps two layers separate:

- Source-backed mechanisms: official architecture, protocol, agent framework, governance, observability and workflow documents show that shared controls, approval, tracing, durable state, security and review gates are real concerns with known implementation costs.
- Course adoption judgment: the Stage 0-4 adoption model, BuildPilot V1 preference and `ADOPT / SIMPLIFY / REJECT / DEFER` decisions are course proposals. They are not external standards, runtime results, ROI measurements, latency benchmarks or defect-reduction evidence.

## Source manifest

All external sources below were freshly accessed on `2026-08-30 Asia/Shanghai`. Hosted product documentation can drift; downstream workers should re-check current docs if exact line references or version-specific behavior become necessary.

| Source ID | Source | Evidence scope | Does not prove |
|---|---|---|---|
| `S-AZURE-GATEWAY-AGG` | Microsoft Azure Architecture Center, Gateway Aggregation pattern: `https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-aggregation` | Aggregation can reduce chattiness, but may introduce single point of failure, bottleneck, service coupling, cascading failure and extra latency/resource concerns. | That every Agent Harness needs a gateway, or that centralization is always cheaper. |
| `S-AZURE-MICROSERVICES` | Microsoft Azure Architecture Center, Microservices architecture style: `https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices` | Cross-cutting concerns may be offloaded; gateway should not contain domain knowledge; shared dependencies can create coupling. | Agent-Harness-specific empirical failure rates. |
| `S-AZURE-OAI-GATEWAY` | Microsoft Azure Architecture Center, gateway in front of model deployments: `https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/azure-openai-gateway-multi-backend` | AI gateway topology can introduce global/regional single points of failure, human-caused configuration risk, routing complexity and unauthorized exposure risk. | That BuildPilot has any gateway or multi-backend implementation. |
| `S-MCP-TOOLS` | MCP 2025-06-18 Tools: `https://modelcontextprotocol.io/specification/2025-06-18/server/tools` | Tool discovery/call/schema, untrusted annotations, HITL/security recommendations, access control, timeouts, audit logging. | That MCP security guidance alone solves Harness governance. |
| `S-MCP-AUTH` | MCP 2025-06-18 Authorization: `https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization` | Optional HTTP authorization flow, token audience/resource binding, PKCE, token theft and confused-deputy concerns. | Universal agent authorization architecture or stdio auth behavior. |
| `S-OAI-RUNNING` | OpenAI Agents SDK Running agents: `https://openai.github.io/openai-agents-python/running_agents/` | Agent loop, `max_turns`, run config, tracing controls, tool execution options and durable execution integrations. | Provider-independent budget/cost/latency guarantees. |
| `S-OAI-HITL` | OpenAI Agents SDK Human-in-the-loop: `https://openai.github.io/openai-agents-python/human_in_the_loop/` | Sensitive tool calls can pause, surface interruptions, serialize `RunState` and resume after approval/rejection. | That approval cannot create fatigue, or that every approval is correctly scoped. |
| `S-OAI-TRACING` | OpenAI Agents SDK Tracing/config: `https://openai.github.io/openai-agents-python/tracing/` and `https://openai.github.io/openai-agents-python/config/` | Tracing can include sensitive model/tool inputs and can be configured; tracing identity can be carried in run config/metadata. | Compliance, redaction completeness or replayability. |
| `S-OTEL-SENSITIVE` | OpenTelemetry Handling sensitive data: `https://opentelemetry.io/docs/security/handling-sensitive-data/` | Telemetry can capture sensitive/personal data; implementers own consent, minimization, protection, storage and review. | That more observability is automatically safe or always permitted. |
| `S-OTEL-SEMCOV` | OpenTelemetry Semantic Conventions: `https://opentelemetry.io/docs/specs/semconv/` and HTTP semantic convention stability notes | Common naming helps correlation; conventions can be mixed/stable/experimental and migration may require duplicate emission. | That a single trace schema never drifts or carries no migration cost. |
| `S-GITHUB-RULESETS` | GitHub rulesets available rules: `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets` | Required PRs/reviews, stale approval dismissal, most-recent-reviewable-push approval, status checks and code-owner review gates. | That BuildPilot is integrated with GitHub or that GitHub gates equal Harness. |
| `S-GITHUB-CODEOWNERS` | GitHub CODEOWNERS: `https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners` | Owner routing and path-based review request semantics. | That owner mapping is always valid or sufficient for business authority. |
| `S-LANGCHAIN-PRODUCTS` | LangChain docs, Frameworks/runtimes/harnesses: `https://docs.langchain.com/oss/python/concepts/products` | Product terminology varies; harnesses, runtimes and frameworks carry different value props and when-to-use guidance. | That the course taxonomy is an industry standard. |
| `S-LANGGRAPH-HITL` | LangChain/LangGraph HITL and persistence docs: `https://docs.langchain.com/oss/python/langchain/human-in-the-loop`, `https://docs.langchain.com/oss/javascript/langgraph/persistence` | HITL requires checkpointing/persistence to pause and resume; production persistence differs from prototype memory. | That every Harness needs LangGraph or that checkpointing removes all recovery risk. |
| `S-TEMPORAL` | Temporal docs, Workflow execution and retry/activity docs: `https://docs.temporal.io/workflow-execution` and `https://docs.temporal.io/develop/typescript/failure-detection` | Durable execution, history/replay, retries, timeouts and failure detection are explicit workflow concerns. | Agent-specific exactly-once effects or BuildPilot durability. |
| `S-NIST-RMF` | NIST AI RMF Core: `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/` | Measurement, uncertainty, documentation, privacy risk, tracking evolving risks and documenting what will not/cannot be measured. | Concrete Harness staging, ROI, threshold or metric schema. |
| `S-COURSE` | Local canonical plan, Article 27 card, published Articles 18-22 and 24-26, current Article 27 workspace | Course boundaries, BuildPilot design-only status, prior Evidence/Permission/Budget/Trace/Eval/Harness vocabulary. | Industry standardization or runtime behavior. |

## Research questions and answers

### RQ1｜When does unified governance outweigh a central bottleneck?

Source-backed mechanism: architecture guidance supports consolidating cross-cutting concerns when repeated local implementations create inconsistent security, logging, routing, throttling or operational behavior. The same guidance also warns that gateways and aggregation layers can become single points of failure, bottlenecks, coupling points, cascading-failure sources and latency amplifiers.

Course answer: Harness pressure is justified when shared governance facts must survive across multiple agents, tools, workflows, hosts, approval paths or recovery paths. It is not justified merely because a team wants a cleaner diagram. The burden of proof belongs to the new shared layer: it should reduce repeated governance drift more than it adds queueing, coordination, migration and operational risk.

### RQ2｜What costs do Context, Trace, Evidence and Policy add?

Source-backed mechanism: OpenAI and OpenTelemetry documentation show that traces, run configs, sensitive payload capture, token/turn limits, tool execution behavior, sessions and telemetry handling are explicit concerns. They add storage, retention, privacy review, configuration, migration and debugging obligations. GitHub review gates add wait states; HITL systems require persisted state for pause/resume.

Course answer: Article 27 should name at least five cost classes: token/context cost, storage/retention cost, direct usage/cost reconciliation, user-visible latency/queueing, and operator/reviewer attention. A Harness that records more without retention/redaction policy creates a second risk surface rather than reliability.

### RQ3｜How can replaceability avoid coupling to one model, framework, tool or vendor?

Source-backed mechanism: LangChain explicitly separates frameworks, runtimes and harnesses by product role; MCP separates protocol primitives and optional authorization; OpenAI/Microsoft/LangGraph/Temporal package execution, tracing, approval and persistence differently. This is direct evidence that names and feature boundaries vary by ecosystem.

Course answer: replaceability should be proven by real variation pressure: a second provider, second host, second workflow, second policy consumer, second evidence sink or a migration requirement. Before that pressure appears, define a narrow contract and keep implementation local. Do not build a plugin platform just because a future replacement is imaginable.

### RQ4｜How do policy drift, wrong configuration, stale knowledge and wrong intent create false safety?

Source-backed mechanism: MCP treats tool annotations as untrusted unless from trusted servers and requires access control/security handling; MCP authorization warns about audience binding, token theft and confused-deputy risks. GitHub rules can dismiss stale approvals when the approved diff changes; CODEOWNERS has routing constraints. OTel warns that telemetry sensitivity is contextual and implementers must review what libraries emit.

Course answer: a Harness can create false safety when a visible capability is treated as authorized, a read-only hint is treated as guaranteed, a stale approval is reused, a redacted trace is treated as complete replay evidence, a memory item is treated as current fact, or a requirement guess is treated as owner intent. Therefore Stage 1-4 all need explicit `UNKNOWN / STALE / NOT_PROVEN / NEEDS_REVIEW` exits.

### RQ5｜What is the cost of approval fatigue and checkpoint/recovery complexity?

Source-backed mechanism: HITL documentation shows approval can pause execution and resume from saved state. LangGraph persistence is required for production HITL/resume; Temporal-style durable execution relies on event history, replay and retry semantics. These sources support that human gates and recovery are stateful execution mechanisms, not chat decoration.

Course answer: approval is valuable only when routed to the right owner at the right risk boundary with enough context to decide. Too many low-value approvals train reviewers to click through. Recovery is valuable only when it separates committed state, pending action, side-effect uncertainty, stale policy and budget. Otherwise it becomes a replay button that repeats confusion.

### RQ6｜When should a team explicitly not build a Harness?

Source-backed mechanism: official architecture guidance repeatedly includes "when not to use" and "problems and considerations" sections for shared layers. LangChain's product docs also frame different tools by when they are useful.

Course answer: no-build is correct when the system is a single low-risk assistant, a one-off script, a fixed tool workflow, a short-lived prototype, a team without ownership capacity, or a domain where evidence and authority can be handled better by existing CI/review/process gates. `Defer` is a design decision, not a failure to mature.

## Stage 0-4 adoption proposal

Stage order is not a maturity ladder. Some teams should remain at Stage 0, 1 or 2 indefinitely; some should never build a Harness. Moving upward requires observed pressure, not ambition.

| Stage | Entry signals | Build | Benefits | Costs and risks | Exit / rollback | Explicit not to build |
|---|---|---|---|---|---|---|
| Stage 0｜No Harness | Single user, single low-risk workflow, no external side effects, no shared approvals, no cross-run evidence promises. | Plain prompt, script, checklist, or local workflow. | Fastest path; minimal overhead; no new platform owner. | Manual discipline; little reuse; weak audit. | Move to Stage 1 only after repeated need for evidence/permission/trace consistency. | Do not build a Harness for a one-off document helper or throwaway prototype. |
| Stage 1｜Local disciplined workflow | One team repeats the same task and needs read-only evidence or bounded approvals, but tool set and host are stable. | Local conventions, structured output, read-only checks, evidence notes, simple approval checklist. | Reduces ambiguity without platform cost. | Still relies on local discipline; drift possible across workflows. | Roll back to Stage 0 if the workflow does not repeat or review cost exceeds value. | Do not create registry/plugin/session stores yet. |
| Stage 2｜Modular monolith Harness slice | Two or more workflows share permission, evidence, trace, budget or review semantics. | Shared policy/evidence/session/trace contracts in one codebase; fixed core and narrow extension points. | Same governance words mean the same thing; easier review and recovery. | Central bottleneck, config precedence bugs, migration burden, local coupling. | Split or simplify if one team becomes a queue for all changes; delete unused extension points. | Do not make everything a plugin; do not add write automation by default. |
| Stage 3｜Governed extension architecture | Multiple hosts/providers/capabilities need independent lifecycle or second implementations. | Versioned capability registry, effective config dump, provider adapters, owner-routed review, bounded recovery, retention/redaction policy. | Real replaceability; safer migration; better auditability. | More state, compatibility work, policy drift, approval fatigue, latency and storage cost. | Freeze new adapters; retire unused capability versions; collapse extension points with one consumer. | Do not expand unless there is a second real consumer or migration. |
| Stage 4｜Platform / ecosystem Harness | Multiple teams rely on the Harness as shared infrastructure with governed capability evolution and quality gates. | Change-controlled policy/versioning, rollout/sunset process, observability/privacy governance, regression/eval hooks, audit/reporting, operational ownership. | Shared infrastructure for high-risk, multi-team agent work. | Platform team bottleneck, false safety, governance theater, high migration and privacy burden. | Sunset capabilities, enforce owner budget, move domain logic out, or demote back to Stage 2/3. | Do not use Stage 4 to prove maturity; avoid if team cannot staff operations and governance. |

## BuildPilot V1 adoption recommendation

BuildPilot remains a design case only. Article 27 should recommend a restrained V1:

- `ADOPT`: restricted read checks, Evidence package, Trace reference, Change Request, Human Review, unknown/stale labels and re-verification plan.
- `SIMPLIFY`: budget as step/time/tool-call caps and stop reasons, not a full cost platform; capability registry as fixed read-only capability list with source/version/trust notes, not an open plugin marketplace.
- `DEFER`: multi-project knowledge graph, semantic/multi-trial eval, governed capability evolution, full durable replay, autonomous code modification, PR creation and production deployment.
- `REJECT`: any claim that BuildPilot already runs, scans Unity/Jenkins, creates PRs, modifies code, improves cost/latency, reduces defects, or proves production safety.

The V1 design should start at Stage 1/2, not Stage 3/4. It should expand only after repeated real usage proves that local evidence, permission, trace and review contracts are being duplicated or drifting.

## Counter-evidence and adoption traps

| Trap | Evidence-backed concern | Article 27 wording ceiling |
|---|---|---|
| "Centralize all governance" | Shared layers can become bottlenecks, single points of failure and coupling points. | Harness centralization is conditional; local workflow may be better. |
| "Every capability must be replaceable" | Product boundaries and terminology vary, but future replacement alone does not prove need. | Replaceability needs real variation pressure or migration risk. |
| "Trace everything" | Telemetry can capture sensitive data; implementers own minimization, consent and retention. | Observability is a risk surface; redaction limits proof. |
| "More approval means safer" | HITL pauses require state; review gates can go stale and create queueing. | Approval must be risk-routed and scoped; otherwise fatigue rises. |
| "Plugin architecture prevents bloat" | Extension layers can create configuration, version and lifecycle overhead. | Pluginization is a cost, not a default virtue. |
| "Knowledge memory improves every run" | Prior articles and OTel/NIST boundaries require provenance, freshness and uncertainty. | Stale knowledge and wrong intent must remain `UNKNOWN` or `NEEDS_REVIEW`. |
| "Eval/CI green means deployable" | Article 22 limits eval to fixed dataset/manifest/judge boundaries. | Regression hooks are useful, but production safety requires separate gates. |

## Research conclusion

Article 27 can safely argue that a Harness is an adoption decision, not a maturity badge. The right question is not "how complete can the Harness become?" but "which shared governance drift is already expensive enough to justify a new owner, new state, new policy surface and new failure modes?"

Evidence Gate can PASS if downstream writing preserves:

- `11 / 11` claim coverage with `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
- Required Lab `NONE`, Experiment Count `0`, Runtime Observation `ABSENT`.
- BuildPilot `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
- Stage 0-4 as course adoption proposal, not external standard.
- No Article 28 / Part VI / DSH source claim, no BuildPilot runtime claim, no ROI/cost/latency/defect-reduction metric.

Next allowed gate: `EVIDENCE_GATE`.
