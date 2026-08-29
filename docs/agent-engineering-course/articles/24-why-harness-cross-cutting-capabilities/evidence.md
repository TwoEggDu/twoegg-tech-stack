# Article 24 Evidence｜为什么最终需要 Harness：横切能力由谁承载

## Evidence Metadata

- Article: `24`
- Gate: `RESEARCH`
- Evidence Status: `READY_FOR_EVIDENCE_GATE`
- Research date: `2026-08-29`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN`
- Claim status count: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Evidence cards: `12`
- Evidence Gate recommendation: `PASS`

## Source Manifest

All external sources were accessed on `2026-08-29`. Public documentation may drift; downstream workers should re-check URLs if publication is delayed or if exact line references become necessary.

| Source ID | Source | Version / identity observed | Evidence scope |
|---|---|---|---|
| S-MCP-OVERVIEW | `https://modelcontextprotocol.io/specification/2025-06-18/server/index` | MCP specification, protocol revision 2025-06-18 | Server primitive split: Prompts, Resources, Tools; control hierarchy. |
| S-MCP-TOOLS | `https://modelcontextprotocol.io/specification/2025-06-18/server/tools` | MCP specification, protocol revision 2025-06-18 | Tool list/call, schemas, annotations, model-controlled invocation, security/user-confirmation guidance. |
| S-MCP-AUTH | `https://modelcontextprotocol.io/specification/draft/basic/authorization` | MCP Authorization page, current/draft hosted spec | Transport/resource-server authorization, OAuth-related roles, operation scopes, out-of-scope boundaries. |
| S-OAI-HITL | `https://openai.github.io/openai-agents-python/human_in_the_loop/` | OpenAI Agents SDK Python docs | HITL interruption, approval/rejection, serialized RunState/resume, fail-closed inspection, approval across handoffs/nested tools. |
| S-OAI-GUARDRAILS | `https://openai.github.io/openai-agents-js/guides/guardrails/` | OpenAI Agents SDK JS docs | Input/output/tool guardrail placement and blocking behavior. |
| S-OAI-TRACING | `https://openai.github.io/openai-agents-js/guides/tracing/` | OpenAI Agents SDK JS docs | Built-in tracing for generations/tool calls/handoffs/guardrails/custom events with deployment constraints. |
| S-MS-TOOL-APPROVAL | `https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval` | Microsoft Agent Framework docs | Approval middleware, tool-call interception, product/example `Harness Agent`. |
| S-MS-PROCESS | `https://learn.microsoft.com/en-us/semantic-kernel/frameworks/process/process-framework` | Microsoft Semantic Kernel docs, experimental process framework | Business process/workflow modeling, event transitions, auditability. |
| S-GH-PROTECTED | `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches` | GitHub Docs | Required reviews, stale approval dismissal, expected status checks, conversation resolution. |
| S-GH-CODEOWNERS | `https://docs.github.com/en/enterprise-server@3.20/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners` | GitHub Enterprise Server 3.20 docs | Owner routing, automatic review request, write-access requirement, invalid-line pitfalls. |
| S-OTEL | `https://opentelemetry.io/docs/specs/otel/` | OpenTelemetry Specification 1.60.0 | Observability signals, tracing/metrics/logs/resource/context/conformance. |
| S-NIST-RMF | `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/` | NIST AI RMF 1.0 Core page, update banner observed | Cross-cutting Govern function, lifecycle risk, documentation, measurement and uncertainty. |
| S-AZURE-OPS | `https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/design-for-operations` | Microsoft Azure Architecture guide | Operations, observability, standardized logging/tracing/metrics, automation. |
| S-AZURE-MICROSERVICES | `https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/microservices` | Microsoft Azure Architecture guide | Shared gateway/config/security concerns; avoiding repeated error-prone common logic. |
| S-UNITY-BUILDREPORT | `https://docs.unity.cn/2022.3/Documentation/ScriptReference/Build.Reporting.BuildReport.html` | Unity 2022.3 API docs | BuildReport build summary/files/steps/packed assets/stripping/platform info. |
| S-UNITY-ASSETDB | `https://docs.unity.cn/Manual/AssetDatabase.html` | Unity Manual | Asset import representation, dependency tracking, import settings, target platform, metadata/GUID/hash. |
| S-UNITY-ADDRESSABLES | `https://docs.unity.cn/Packages/com.unity.addressables%402.9/manual/analyze-addressables-window-reference.html` | Unity Addressables 2.9 manual | Analyze layout, duplicate bundle dependencies, explicit/implicit assets, fixable/unfixable checks. |
| S-ISO-29148 | `https://www.iso.org/obp/ui?_escaped_fragment_=iso:std:iso-iec-ieee:29148:ed-2:v1:en` | ISO/IEC/IEEE 29148 public OBP page | Requirement precision, constraints/conditions, management, traceability, objective verification/validation. |
| S-MADR | `https://github.com/adr/madr/blob/develop/template/adr-template.md` | MADR ADR template on GitHub develop branch | Decision record structure: status/date/decision-makers/context/options/outcome/consequences/confirmation. |
| S-KCS | `https://library.serviceinnovation.org/KCS/Knowledge-Centered_Success_Practices_Guide` | KCS Practices Guide 2027 page | Knowledge reuse/improvement/creation in workflow and automation. |
| S-COURSE | Local course files and published Articles 18-22 | Repository-local course canon | Evidence/permission/budget/trace/eval concepts and Article 24/25/26/27 boundaries. |

## Claim-to-Evidence Matrix

| Claim ID | Evidence Card | Status | Primary sources |
|---|---|---|---|
| 24-C01 | 24-E01 | `CONFIRMED` | S-MCP-OVERVIEW, S-MS-PROCESS, S-OAI-HITL, S-OAI-GUARDRAILS, S-OAI-TRACING, S-MS-TOOL-APPROVAL, S-COURSE |
| 24-C02 | 24-E02 | `CONFIRMED` | S-MCP-TOOLS, S-MCP-AUTH |
| 24-C03 | 24-E03 | `PARTIAL` | S-OAI-HITL, S-OAI-GUARDRAILS, S-MS-TOOL-APPROVAL |
| 24-C04 | 24-E04 | `PARTIAL` | S-OTEL, S-NIST-RMF, S-COURSE |
| 24-C05 | 24-E05 | `PARTIAL` | S-AZURE-OPS, S-AZURE-MICROSERVICES, S-NIST-RMF |
| 24-C06 | 24-E06 | `PROPOSAL` | S-AZURE-MICROSERVICES, S-MS-TOOL-APPROVAL, S-COURSE |
| 24-C07 | 24-E07 | `PROPOSAL` | S-COURSE, S-MCP-OVERVIEW, S-MCP-TOOLS, S-OAI-HITL, S-OAI-GUARDRAILS, S-OAI-TRACING |
| 24-C08 | 24-E08 | `PARTIAL` | S-GH-PROTECTED, S-GH-CODEOWNERS |
| 24-C09 | 24-E09 | `PARTIAL` | S-ISO-29148, S-MADR, S-KCS, S-NIST-RMF |
| 24-C10 | 24-E10 | `PARTIAL` | S-UNITY-BUILDREPORT, S-UNITY-ASSETDB, S-UNITY-ADDRESSABLES |
| 24-C11 | 24-E11 | `PROPOSAL` | 24-E08, 24-E09, 24-E10, S-COURSE |
| 24-C12 | 24-E12 | `CONFIRMED` | S-COURSE |

## Evidence Cards

### 24-E01 — Public systems separate local primitives and control layers

- Claim ID: `24-C01`
- Status: `CONFIRMED`
- Evidence type: External documentation + course boundary confirmation
- Sources:
  - S-MCP-OVERVIEW
  - S-MS-PROCESS
  - S-OAI-HITL
  - S-OAI-GUARDRAILS
  - S-OAI-TRACING
  - S-MS-TOOL-APPROVAL
  - S-COURSE
- Reproduction:
  - Open MCP server overview and verify the three server primitives: Prompts, Resources, Tools.
  - Open Microsoft Process Framework and verify process/workflow framing.
  - Open OpenAI/Microsoft SDK docs and verify guardrail, tracing, and approval mechanisms are separate pages/mechanisms.
  - Open local glossary/series plan and verify Harness is course-defined and Article 24 begins Part V.
- Observation:
  - MCP distinguishes predefined instructions/templates, structured data/content, and executable functions.
  - Microsoft Process Framework frames business processes as structured activity/task sequences.
  - OpenAI and Microsoft docs place HITL, guardrails, tracing, and approval in separate runtime/framework mechanisms rather than in a single prompt.
  - Local glossary defines Harness as a course term for reusable controls/constraints around Runtime, with Runtime formalized later in Article 25.
- Counter-evidence searched:
  - A Microsoft page uses a product/example phrase `Harness Agent`, but that is not the same as this course’s Harness model.
  - No external source found that standardizes the course’s exact Harness definition.
- Interpretation:
  - The primitive landscape is genuinely scattered. Article 24 can claim the need for a shared boundary, but must label the boundary name as course vocabulary.
- Proves:
  - Public systems separate local primitives and several control mechanisms.
  - Course Harness is not externally standardized by these sources.
- Does Not Prove:
  - That every agent system must implement a Harness.
  - That the course Harness is an industry standard.
  - That Article 24 may define the full runtime architecture.
- Course usage:
  - Use in the opening section to show the scattered-responsibility problem.

### 24-E02 — Tool discovery is not permission, trust, or evidence acceptance

- Claim ID: `24-C02`
- Status: `CONFIRMED`
- Evidence type: Protocol documentation
- Sources:
  - S-MCP-TOOLS
  - S-MCP-AUTH
- Reproduction:
  - Open MCP Tools page and verify tool declaration/list/call/schema/annotation/security guidance.
  - Open MCP Authorization page and verify authorization scope and out-of-scope statements.
- Observation:
  - MCP tools are discoverable and callable by models through declared schemas and list/call APIs.
  - Tool annotations are advisory and must not be treated as trusted unless supplied by trusted servers.
  - MCP recommends human confirmation for sensitive operations, input validation, access control, timeout/rate-limit handling, and audit logging.
  - Authorization is optional in MCP, transport/resource-server scoped, and does not define every authorization-server behavior.
- Counter-evidence searched:
  - MCP does define an authorization layer for some HTTP MCP deployments, so “MCP has no auth” would be false.
  - However, MCP authorization does not collapse permission, approval, trust, evidence, budget, and trace into tool discovery.
- Interpretation:
  - The article can confidently say: registering a tool means the model can discover/request it; it does not mean the current user/request is authorized, the output is trusted evidence, or the operation is safe.
- Proves:
  - Tool discovery/call and governance are distinct concerns.
- Does Not Prove:
  - That the course Harness must use MCP.
  - That MCP security guidance is insufficient for all use cases.
- Course usage:
  - Use to rebut “put everything in the tool wrapper”.

### 24-E03 — Approval and guardrails are executable controls, not only prompt text

- Claim ID: `24-C03`
- Status: `PARTIAL`
- Evidence type: Product/framework documentation
- Sources:
  - S-OAI-HITL
  - S-OAI-GUARDRAILS
  - S-MS-TOOL-APPROVAL
- Reproduction:
  - Open OpenAI HITL docs and verify interruption, approval/rejection, serialized state/resume, and fail-closed inspection.
  - Open OpenAI guardrail docs and verify input/output/tool guardrail families plus blocking placement.
  - Open Microsoft Agent Framework tool approval docs and verify function wrapping/interception/middleware.
- Observation:
  - OpenAI HITL pauses agent execution for sensitive tool calls, stores approval decisions by call/tool identity, and resumes the original run with RunState.
  - OpenAI guardrails have placement semantics; tool guardrails run around function-tool invocation, while agent-level guardrails may not cover every workflow agent.
  - Microsoft approval wraps tools and uses middleware to intercept model-requested tool calls before continuing.
- Counter-evidence searched:
  - These examples are SDK-specific; they do not prove a universal control architecture.
  - Teams can implement simple approval manually for a narrow single-agent prototype.
- Interpretation:
  - The article may claim that serious approval/guardrail behavior needs executable state and placement. It should not claim OpenAI/Microsoft docs require this course Harness.
- Proves:
  - Approval/guardrail behavior is implemented as runtime/framework control in real systems.
- Does Not Prove:
  - That a prompt is useless.
  - That all approval must be centralized.
  - That BuildPilot currently has such middleware.
- Course usage:
  - Use to explain “not a longer System Prompt”.

### 24-E04 — Trace, evidence, budget, and eval are related but independent control surfaces

- Claim ID: `24-C04`
- Status: `PARTIAL`
- Evidence type: Standard documentation + course dependency chain
- Sources:
  - S-OTEL
  - S-NIST-RMF
  - S-COURSE
- Reproduction:
  - Open OpenTelemetry specification overview and verify signals/conformance categories.
  - Open NIST AI RMF Core and verify cross-cutting governance, documentation, measurement, uncertainty, and lifecycle-risk language.
  - Read local Articles 18-22 for Evidence Contract, Permission/Approval, Budget, Trace/Replay, and Eval boundaries.
- Observation:
  - OpenTelemetry standardizes observability signals and context propagation, but does not decide business evidence acceptance.
  - NIST frames governance as cross-cutting and measurement/documentation as continuous risk management.
  - Course Articles 18-22 intentionally separate evidence, permission, budget, trace/replay, and eval.
- Counter-evidence searched:
  - Some products combine tracing, evaluation, and logs in one observability UI; this does not erase the semantic distinction.
- Interpretation:
  - Article 24 can argue Harness is where these adjacent controls are coordinated, not where they are collapsed into one metric.
- Proves:
  - Public standards/frameworks support distinct observability/governance/measurement concerns.
  - The course already established separate semantic contracts.
- Does Not Prove:
  - That any trace automatically satisfies evidence.
  - That all eval data belongs in Harness.
- Course usage:
  - Use as bridge from Part IV to Part V.

### 24-E05 — Duplication creates drift pressure

- Claim ID: `24-C05`
- Status: `PARTIAL`
- Evidence type: Architecture/operations guidance by analogy
- Sources:
  - S-AZURE-OPS
  - S-AZURE-MICROSERVICES
  - S-NIST-RMF
- Reproduction:
  - Open Azure Design for Operations and verify guidance on standardized logs/metrics/traces and operational functions.
  - Open Azure Microservices architecture guidance and verify offloading security/common tasks and avoiding repeated error-prone logic.
  - Open NIST RMF Core and verify governance is cross-cutting across lifecycle functions.
- Observation:
  - Azure operations guidance says inconsistent logging formats can make useful retrieval difficult or impossible.
  - Azure microservice guidance warns that embedding security/token validation/common tasks in many services complicates maintenance and creates repetitive/error-prone code.
  - NIST governance is explicitly cross-cutting and connected to documentation/accountability across lifecycle activities.
- Counter-evidence searched:
  - These are not agent-Harness-specific sources.
  - A small, single workflow may tolerate local duplicated rules.
- Interpretation:
  - The drift argument is strong as engineering reasoning and architecture analogy, but should remain `PARTIAL` rather than `CONFIRMED` as a universal empirical finding about agent systems.
- Proves:
  - Cross-cutting operational/security/observability logic is known to drift when repeated locally.
- Does Not Prove:
  - The exact failure rate or cost of duplicated agent governance.
  - That centralization is always cheaper.
- Course usage:
  - Use as the main pressure: local governance copies eventually disagree.

### 24-E06 — Harness is shared control plane, not God Object

- Claim ID: `24-C06`
- Status: `PROPOSAL`
- Evidence type: Course design proposal with architecture analogy
- Sources:
  - S-AZURE-MICROSERVICES
  - S-MS-TOOL-APPROVAL
  - S-COURSE
- Reproduction:
  - Verify local course glossary says Harness is reusable controls/constraints around Runtime.
  - Verify Azure microservice guidance separates gateway/security/common concerns from service business logic.
  - Verify Microsoft approval middleware intercepts tool calls without being the whole business process.
- Observation:
  - Shared control components can carry cross-cutting concerns while keeping domain services or workflows focused.
  - Microsoft’s tool approval middleware is a focused cross-cutting control, not an owner of business intent.
  - Course documents reserve complete Runtime/Harness/Capability details for Articles 25-27.
- Counter-evidence searched:
  - A central control plane can become overgrown if it absorbs business policy and domain planning.
  - The article must acknowledge this risk and keep Harness narrowly scoped.
- Interpretation:
  - This is a design stance for the course, supported by architecture analogy, not a settled external definition.
- Proves:
  - It is reasonable to separate shared controls from business logic.
- Does Not Prove:
  - The final course Harness API.
  - That all agent systems should use the same shape.
- Course usage:
  - Use immediately after introducing Harness to prevent God Object misunderstanding.

### 24-E07 — Initial Article 24 Harness responsibility set

- Claim ID: `24-C07`
- Status: `PROPOSAL`
- Evidence type: Synthesis of prior course contracts and public primitives
- Sources:
  - S-COURSE
  - S-MCP-OVERVIEW
  - S-MCP-TOOLS
  - S-OAI-HITL
  - S-OAI-GUARDRAILS
  - S-OAI-TRACING
- Reproduction:
  - Read local Articles 18-22 and glossary to list evidence, permission, budget, trace/replay, eval, Runtime, Harness, and Capability boundaries.
  - Verify public docs expose corresponding primitives: tool discovery/security, approval, guardrails, tracing.
- Observation:
  - The responsibility list is not invented out of one source; it is a synthesis of prior course terms plus recurring public control surfaces.
  - Identity/context/recovery/knowledge/capability discovery are needed to connect the prior controls across runs, but exact models are deferred.
- Counter-evidence searched:
  - No source gives this exact complete list as a normative “Harness capability list”.
  - Some platforms package these concerns differently.
- Interpretation:
  - Keep this list as “Article 24 initial responsibility set” or “pressure map”, not a final API.
- Proves:
  - The proposed Harness list is grounded in real categories from earlier course work and public systems.
- Does Not Prove:
  - Complete coverage.
  - Stable naming.
  - That each item deserves equal weight.
- Course usage:
  - Use as a midway table in the article.

### 24-E08 — Owner routing, review gates, stale review, and status checks

- Claim ID: `24-C08`
- Status: `PARTIAL`
- Evidence type: Platform documentation
- Sources:
  - S-GH-PROTECTED
  - S-GH-CODEOWNERS
- Reproduction:
  - Open GitHub Protected Branches and verify required reviews, stale approval dismissal, status checks, and conversation resolution.
  - Open GitHub CODEOWNERS and verify path-to-owner mapping, review request behavior, write-access requirement, and invalid-line pitfalls.
- Observation:
  - GitHub can require PR review before merge, dismiss stale approvals when diffs change, require status checks from an expected app, and require conversation resolution.
  - CODEOWNERS maps paths to owners and auto-requests review, but invalid owner lines or missing write access can break routing.
- Counter-evidence searched:
  - GitHub is source-control specific and not an agent Harness.
  - Teams may use other review systems.
- Interpretation:
  - Review governance needs durable state and owner mapping beyond the local suggestion text.
- Proves:
  - Real engineering platforms distinguish suggestion/change, owner review, stale revalidation, and merge gate.
- Does Not Prove:
  - BuildPilot integration with GitHub.
  - That GitHub semantics should be copied exactly into Harness.
- Course usage:
  - Use for BuildPilot Change Request and Human Review handoff.

### 24-E09 — Requirement, intent, and knowledge lifecycle support the design chain

- Claim ID: `24-C09`
- Status: `PARTIAL`
- Evidence type: Standards/practice documentation + course inference
- Sources:
  - S-ISO-29148
  - S-MADR
  - S-KCS
  - S-NIST-RMF
- Reproduction:
  - Open ISO public terms and verify requirement precision, constraints/conditions, management, traceability, and objective verification/validation.
  - Open MADR template and verify decision records capture status/date/decision makers/context/options/outcome/consequences/confirmation.
  - Open KCS guide and verify knowledge reuse/improvement/creation in workflow.
  - Open NIST AI RMF Core and verify documentation/accountability and continuous measurement.
- Observation:
  - Requirements practice emphasizes unambiguous conditions and traceability.
  - ADR practice records context/problem, decision, options, consequences, and confirmation.
  - KCS emphasizes creating/reusing/improving knowledge in the workflow.
  - NIST emphasizes documentation for review/accountability and ongoing measurement.
- Counter-evidence searched:
  - These are separate practices; no source provides the exact BuildPilot chain.
  - MADR template is a GitHub project template, not a formal standard.
- Interpretation:
  - BuildPilot’s Requirement Contract → Intent Ledger → Knowledge Store chain should be presented as a synthesis/proposal grounded in established engineering practices.
- Proves:
  - The component practices are legitimate and independently documented.
- Does Not Prove:
  - That BuildPilot’s exact schema exists.
  - That knowledge capture always improves outcomes.
- Course usage:
  - Use to keep BuildPilot scenario concrete without overclaiming implementation.

### 24-E10 — Unity read-only evidence surfaces are available

- Claim ID: `24-C10`
- Status: `PARTIAL`
- Evidence type: Unity documentation
- Sources:
  - S-UNITY-BUILDREPORT
  - S-UNITY-ASSETDB
  - S-UNITY-ADDRESSABLES
- Reproduction:
  - Open Unity BuildReport 2022.3 docs and verify BuildPipeline.BuildPlayer returns BuildReport with files, steps, scenes/assets, packed assets, stripping info, and summary.
  - Open Unity AssetDatabase docs and verify imported asset representation, dependency tracking, import settings, target platform, metadata, GUID/hash.
  - Open Unity Addressables Analyze docs and verify duplicate dependency and layout inspection features.
- Observation:
  - Unity exposes build and asset/import relationship data through documented surfaces.
  - Addressables Analyze can inspect bundle/layout dependency risks and distinguishes checks from fixes.
- Counter-evidence searched:
  - These docs do not prove BuildPilot has adapters for these APIs.
  - Some evidence requires an actual Unity project build or package version; none was run here.
- Interpretation:
  - Article 24 can name these as plausible read-only evidence categories for the design case. It must not claim collected evidence from a live Unity project.
- Proves:
  - Public Unity surfaces exist for several BuildPilot evidence categories.
- Does Not Prove:
  - BuildPilot implementation.
  - Runtime availability in this repo.
  - That every Unity project can expose sufficient evidence without setup.
- Course usage:
  - Use in the concrete Unity scenario section.

### 24-E11 — BuildPilot requirement-change scenario is a bounded course proposal

- Claim ID: `24-C11`
- Status: `PROPOSAL`
- Evidence type: Synthesis / design-case assignment
- Sources:
  - 24-E08
  - 24-E09
  - 24-E10
  - S-COURSE
- Reproduction:
  - Read Article 24 card and parent prompt requirements.
  - Verify BuildPilot status in local article card/README: design case only, suggestion-first, no production modification.
  - Combine evidence from review gates, requirements/knowledge practices, and Unity read surfaces.
- Observation:
  - The required scenario has a plausible engineering chain:
    1. Requirement Contract candidate.
    2. Missing/ambiguous/contradictory condition detection.
    3. Read-only evidence over C#, config tables, asset rules, and build evidence.
    4. Evidence-backed Finding.
    5. Change Request with owner/review link.
    6. Owner implementation.
    7. Re-verification.
    8. Intent Ledger / Knowledge Store.
    9. Future rule/test/gate candidate.
- Counter-evidence searched:
  - No BuildPilot repository, run log, schema, UI, or integration test was found or authorized in Article 24.
  - No Unity runtime/editor operation was required.
- Interpretation:
  - The scenario is safe if every statement is framed as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED`.
- Proves:
  - The scenario can be grounded in documented practices and Unity-readable surfaces.
- Does Not Prove:
  - Runtime behavior.
  - Product existence.
  - Change authority.
  - Automated code/config/art edits.
- Course usage:
  - Use as the article’s main concrete example.

### 24-E12 — Article reality and non-scope are confirmed locally

- Claim ID: `24-C12`
- Status: `CONFIRMED`
- Evidence type: Local repository source/config
- Sources:
  - S-COURSE
  - `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/README.md`
  - `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/article-card.md`
  - `docs/agent-engineering-series-plan.md`
  - `docs/agent-engineering-course/glossary.md`
- Reproduction:
  - Read the Article 24 README and article card.
  - Read the series plan row for Article 24.
  - Read glossary entries for Harness, Runtime, Capability.
- Observation:
  - Article 24 lifecycle is `RESEARCHING`.
  - Required Lab is `NONE`.
  - Experiment Count is `0`.
  - Runtime Observation is `ABSENT`.
  - BuildPilot is `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED`.
  - Article 25 formalizes Runtime; Article 26 formalizes minimum Capability model; Article 27 handles cost/adoption/non-adoption.
- Counter-evidence searched:
  - No local article file in the allowed workspace establishes BuildPilot as implemented/run.
  - No lab file was required for Article 24.
- Interpretation:
  - Downstream draft must preserve all non-scope labels.
- Proves:
  - Article 24 must not claim runtime/lab/product evidence.
- Does Not Prove:
  - Anything about future Article 25-27 completion.
- Course usage:
  - Use as mandatory wording guardrail and final claim traceability note.

## Evidence Classification Summary

| Status | Count | Claims |
|---|---:|---|
| `CONFIRMED` | 3 | 24-C01, 24-C02, 24-C12 |
| `PARTIAL` | 6 | 24-C03, 24-C04, 24-C05, 24-C08, 24-C09, 24-C10 |
| `PROPOSAL` | 3 | 24-C06, 24-C07, 24-C11 |
| `BLOCKED` | 0 | None |

## Evidence Gate Decision

- Recommendation: `PASS`
- Next allowed gate: `EVIDENCE_GATE`
- Gate completed by this worker: `RESEARCH`
- Reason:
  - 12 claims have mapped evidence cards.
  - 0 core claims are `BLOCKED`.
  - Every `PROPOSAL` claim is explicitly bounded as course model/design synthesis.
  - Required Lab is `NONE`.
  - Experiment Count is `0`.
  - Runtime Observation is `ABSENT`.
  - BuildPilot is explicitly `NOT_IMPLEMENTED` and `NOT_RUN`.

## Required Downstream Restrictions

- Do not publish a claim that Harness is an industry standard term.
- Do not claim BuildPilot exists, runs, modifies Unity projects, opens PRs, or owns production changes.
- Do not convert `PARTIAL` or `PROPOSAL` evidence into `CONFIRMED` wording.
- Do not define the full Runtime/Harness split before Article 25.
- Do not define the full minimum Capability model before Article 26.
- Do not expand into Article 27 cost/adoption/non-adoption analysis except for one forward pointer.
