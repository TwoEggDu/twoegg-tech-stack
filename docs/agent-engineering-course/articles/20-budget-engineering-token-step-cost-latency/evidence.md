# Article 20 Evidence｜Budget Engineering：Token、Step、Cost 与 Latency

## Evidence metadata

- Status: COMPLETE
- Evidence Gate recommendation: PASS
- Claim coverage: 9 / 9
- Evidence Cards: 11
- Core BLOCKED Claims: 0
- Required Lab: NONE
- Experiments: 0
- Runtime observation: ABSENT
- BuildPilot: DESIGN / NOT IMPLEMENTED / NOT RUN
- Retrieval date for current hosted sources: 2026-08-26 (Asia/Shanghai)

## Claim Register

| Claim | Question / responsibility | Status | Evidence | Wording ceiling |
|---|---|---|---|---|
| 20-C01 | Four-dimensional Budget separation | PARTIAL | 20-E01, E02, E04, E05, E06, E07 | Call it a course taxonomy supported by distinct contracts, not an industry-unified model |
| 20-C02 | Context Window vs count/usage vs Token Budget | CONFIRMED | 20-E01, E02, E03 | Preserve provider/model/request identity；no fixed window or universal field mapping |
| 20-C03 | Step Budget and exact counting rule | PARTIAL | 20-E04, E05, E10 | Product caps prove bounded units；course Step reserves before work and increments `used` once at successful commit，not at both admission and result |
| 20-C04 | Cost estimate/reservation/actual | PARTIAL | 20-E07, E08, E09, E10 | FOCUS/provider facts support cost separation；reservation/incurred-pending and single-accounting transitions are course design |
| 20-C05 | Deadline/timeout/queue/service/critical path | PARTIAL | 20-E06, E10 | gRPC supports deadline/timeout；clock-domain resume，phase ledger and critical path are course design over visible data |
| 20-C06 | Admission/reservation/enforcement/reconciliation | PROPOSAL | 20-E10, E11 | Design lifecycle only；not implemented or standardized |
| 20-C07 | Exhaustion routes and independence from authority/quality | PROPOSAL | 20-E01, E04, E06, E10, E11 | Stop/degrade/approval/partial routes are policy design, not product guarantees |
| 20-C08 | Uncertainty and Budget audit record | PROPOSAL | 20-E07, E08, E09, E10, E11 | Minimal course record；not Article 21 Trace schema or provider billing schema |
| 20-C09 | BuildPilot design and Article 21/22 seam | PROPOSAL | 20-E10, E11 | DESIGN / NOT IMPLEMENTED / NOT RUN；no real usage, cost, latency, quality or benefit |

## Cross-Claim evidence matrix

| Evidence | C01 | C02 | C03 | C04 | C05 | C06 | C07 | C08 | C09 |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 20-E01 | X | X |  |  |  |  | X |  |  |
| 20-E02 | X | X |  |  |  |  |  |  |  |
| 20-E03 |  | X |  |  |  |  |  |  |  |
| 20-E04 | X |  | X |  |  |  | X |  |  |
| 20-E05 | X |  | X |  |  |  |  |  |  |
| 20-E06 | X |  |  |  | X |  | X |  |  |
| 20-E07 | X |  |  | X |  |  |  | X |  |
| 20-E08 |  |  |  | X |  |  |  | X |  |
| 20-E09 |  |  |  | X |  |  |  | X |  |
| 20-E10 |  |  | X | X | X | X | X | X | X |
| 20-E11 |  |  |  |  |  | X | X | X | X |

## Evidence Cards

### Evidence 20-E01｜OpenAI Responses separates capacity controls, usage and terminal status

- Source identity: OpenAI API Reference, “Create a model response”, current hosted contract retrieved 2026-08-26.
- Exact locator: `POST /responses`; `max_output_tokens`; `truncation`; response `usage`; `incomplete_details.reason`.
- URL: https://developers.openai.com/api/reference/cli/resources/responses/methods/create
- Supported Claims: 20-C01, 20-C02, 20-C07.
- Evidence type: current primary product API contract.
- Status contribution: CONFIRMED for this OpenAI surface only.
- Supported conclusion: `max_output_tokens` is an upper bound including visible output and reasoning tokens；response usage has OpenAI-specific input/output/detail fields；context overflow can truncate or fail；an incomplete response can identify `max_output_tokens` as reason.
- Does not prove: a Run-wide Token Budget；a context-window number；quality under a cap；Anthropic/Google-compatible usage semantics；that an incomplete response is a safe partial result.
- Limitations / drift: moving API reference；model fields and categories can change；no request was executed.
- Falsifier: if the current API removes or changes these fields, the product example must be re-retrieved and remapped rather than preserved from memory.

### Evidence 20-E02｜OpenAI input counting is a distinct preflight operation

- Source identity: OpenAI API Reference, “Get input token counts”, current hosted contract retrieved 2026-08-26.
- Exact locator: `POST /responses/input_tokens`; returns `response.input_tokens` with `input_tokens`; request accepts model, input, instructions, tools and continuation-related fields.
- URL: https://developers.openai.com/api/reference/python/resources/responses/subresources/input_tokens/methods/count
- Supported Claims: 20-C01, 20-C02.
- Evidence type: current primary product API contract.
- Status contribution: CONFIRMED for the existence and shape of this current operation.
- Supported conclusion: input counting can occur before response creation and is separate from the response usage receipt and the application's Run budget.
- Does not prove: exact future response usage, output tokens, cost, model quality or a cross-provider token count.
- Limitations / drift: current hosted reference；the page does not promise that a preflight count closes all future uncertainty after request mutation or Provider processing.
- Falsifier: removal of the endpoint or a contract that explicitly equates its result with complete final usage would require revision.

### Evidence 20-E03｜Anthropic count is an estimate and usage is provider-specific

- Source identity: Anthropic Claude Platform Docs, “Token counting” and Messages `POST /v1/messages`, current hosted docs retrieved 2026-08-26.
- Exact locator: Token Counting “How to count message tokens” note；Messages `max_tokens`；Message response `usage` and `stop_reason`.
- URLs: https://platform.claude.com/docs/en/build-with-claude/token-counting ; https://platform.claude.com/docs/en/api/messages/create
- Supported Claims: 20-C02.
- Evidence type: current primary product documentation/API contract.
- Status contribution: CONFIRMED for this Anthropic surface only.
- Supported conclusion: the count endpoint returns an input-token estimate that may differ slightly from actual input use；`max_tokens` is an output maximum and models can stop earlier；response usage carries Anthropic-specific cache/input/output/service fields.
- Does not prove: OpenAI-equivalent categories, identical tokenizer, a final cost, or that estimate error has a universal bound.
- Limitations / drift: moving docs；examples include current model names but this Article does not freeze or reuse their numeric behavior.
- Falsifier: if current Anthropic docs no longer call count an estimate or redefine fields, mappings must be refreshed and remain provider-scoped.

### Evidence 20-E04｜OpenAI Agents SDK `max_turns` has a product-specific unit and exhaustion terminal

- Source identity: OpenAI Agents SDK Python, “Running agents”, current hosted docs retrieved 2026-08-26；official release index/tag snapshot [`v0.22.0`](https://github.com/openai/openai-agents-python/releases/tag/v0.22.0), released 2026-08-19 and marked Latest at retrieval.
- Exact locator: “The agent loop” and “Exceptions” sections；`max_turns`；`MaxTurnsExceeded`.
- URLs: https://openai.github.io/openai-agents-python/running_agents/ ; https://github.com/openai/openai-agents-python/releases/tag/v0.22.0
- Supported Claims: 20-C01, 20-C03, 20-C07.
- Evidence type: current official SDK documentation.
- Status contribution: PARTIAL because it is a product example, not a general Step contract.
- Supported conclusion: this SDK defines a turn as one AI invocation including tool calls；exceeding the configured turn limit raises a distinct exception and indicates the task did not complete within that limit.
- Does not prove: course Step equals SDK turn；tool calls each consume a turn；a turn cap controls exact Token/cost/time；cap exhaustion means task failure, bad quality or unsafe effect.
- Limitations / drift: hosted docs move and are not proven built from or pinned to `v0.22.0`；the release snapshot supplies replayable release identity only，not semantics-to-tag binding；no SDK execution occurred.
- Falsifier: a version-pinned source showing a different counting rule for the cited API would narrow or replace this example.

### Evidence 20-E05｜LangGraph recursion limit counts super-steps, not OpenAI turns

- Source identity: LangChain official LangGraph Graph API, current hosted docs retrieved 2026-08-26；official release index/tag snapshot [`1.2.11`](https://github.com/langchain-ai/langgraph/releases/tag/1.2.11), released 2026-08-11 and the newest `langgraph` package release listed at retrieval.
- Exact locator: “Recursion limit” and “Accessing and handling the recursion counter”.
- URLs: https://docs.langchain.com/oss/python/langgraph/graph-api#recursion-limit ; https://github.com/langchain-ai/langgraph/releases/tag/1.2.11
- Supported Claims: 20-C01, 20-C03.
- Evidence type: current official product documentation.
- Status contribution: PARTIAL product comparison.
- Supported conclusion: `recursion_limit` limits super-steps and exhaustion raises `GraphRecursionError`; a super-step can include parallel nodes, so this unit is not interchangeable with an AI invocation or course committed Step.
- Does not prove: all LangGraph versions use the same default；one super-step has fixed Token/cost/time；the hosted page is built from observed repository tag `1.2.11`.
- Limitations / drift: moving docs；the release snapshot supplies replayable release identity only，while docs-to-tag identity remains unproven；no execution occurred.
- Falsifier: a pinned target version with different super-step semantics requires version-scoped remapping.

### Evidence 20-E06｜gRPC distinguishes deadline from timeout and deducts elapsed time

- Source identity: gRPC official guide, “Deadlines”, current page retrieved 2026-08-26.
- Exact locator: Overview；Deadlines on the Client/Server；Deadline Propagation.
- URL: https://grpc.io/docs/guides/deadlines/
- Supported Claims: 20-C01, 20-C05, 20-C07.
- Evidence type: official protocol/runtime guide.
- Status contribution: CONFIRMED within gRPC semantics；PARTIAL for course latency decomposition.
- Supported conclusion: a deadline is a point in time；a timeout is a maximum duration；propagation should account for elapsed time；deadline expiry does not itself stop all spawned application work unless the application cooperates.
- Does not prove: one Agent-wide latency schema；provider queue/service times；critical path；persisted cross-process/host/reboot clock comparison；timeout rollback or external-effect cancellation.
- Limitations / drift: guide spans multiple gRPC languages whose enablement defaults differ；this Article uses only the stable conceptual distinction.
- Falsifier: if gRPC changes these definitions, latency wording must be rechecked; application-specific decomposition remains proposal regardless.

### Evidence 20-E07｜OpenAI cost records are a separate historical administration surface

- Source identity: OpenAI API Reference, Organization Costs, current hosted contract retrieved 2026-08-26.
- Exact locator: `GET /organization/costs`; time buckets；grouping by project/line item/API key；result amount value/currency.
- URL: https://developers.openai.com/api/reference/python/resources/admin/subresources/organization/subresources/usage/methods/costs
- Supported Claims: 20-C01, 20-C04, 20-C08.
- Evidence type: current primary product API contract.
- Status contribution: CONFIRMED for existence of this current cost record surface；PARTIAL for application reconciliation design.
- Supported conclusion: organization cost data is retrieved separately from a model response and carries monetary/time/attribution identity; response token usage alone is not the cost record.
- Does not prove: synchronous per-request final cost；invoice finality；the course reservation schema；that all cost can be attributed to one run.
- Limitations / drift: Admin API access and aggregation granularity apply；current response schema can change；no authenticated read was performed.
- Falsifier: if the endpoint is removed or its cost meaning changes, this product mapping must be refreshed.

### Evidence 20-E08｜Anthropic separates response usage from historical usage/cost reconciliation

- Source identity: Anthropic Claude Platform Docs, “Usage and Cost API”, current hosted docs retrieved 2026-08-26.
- Exact locator: overview；accurate usage tracking；cost reconciliation；availability/key-type limits；FAQ notes on line items/service tiers.
- URL: https://platform.claude.com/docs/en/manage-claude/usage-cost-api
- Supported Claims: 20-C04, 20-C08.
- Evidence type: current primary product administration documentation.
- Status contribution: CONFIRMED for this product surface；PARTIAL for general design.
- Supported conclusion: historical usage and cost are administration/reconciliation data distinct from response counting, with availability and attribution limitations.
- Does not prove: a universal cost schema, reservation, invoice finality for every returned row, or OpenAI-equivalent fields.
- Limitations / drift: API differs by organization/product and is unavailable on some surfaces；no Admin API access or runtime observation occurred.
- Falsifier: current availability/schema changes require a fresh product-scoped card.

### Evidence 20-E09｜FOCUS 1.4 separates List, Effective and Billed Cost

- Source identity: FinOps Open Cost and Usage Specification, publication version 1.4.
- Exact locator: §3.1.7 Billed Cost；§3.1.35 Effective Cost；§3.1.40 List Cost.
- URLs: https://focus.finops.org/docs/specification/v1-4/columns/cost-and-usage/billed-cost/ ; https://focus.finops.org/docs/specification/v1-4/columns/cost-and-usage/effective-cost/ ; https://focus.finops.org/docs/specification/v1-4/columns/cost-and-usage/list-cost/
- Supported Claims: 20-C04, 20-C08.
- Evidence type: version-fixed primary billing-data specification.
- Status contribution: CONFIRMED within FOCUS 1.4 billing-data scope.
- Supported conclusion: List Cost is derived from list unit price × pricing quantity；Effective Cost reflects recognized usage/commitment economics；Billed Cost reflects invoiced amounts and must not be estimated or inferred. Cost basis and source must therefore be explicit.
- Does not prove: Agent request estimate/reservation lifecycle；provider invoice availability at run completion；exact OpenAI/Anthropic field mapping.
- Limitations / drift: FOCUS is a billing dataset standard, not an Agent Runtime or admission protocol；future versions may evolve.
- Falsifier: a later chosen FOCUS version with materially different definitions requires a new version-scoped mapping, not silent upgrade.

### Evidence 20-E10｜Course Budget lifecycle, uncertainty and exhaustion routing synthesis

- Source identity: design synthesis constrained by 20-E01—E09 and Published Articles 10/11/12/19, repository/current-source snapshot 2026-08-26.
- Exact locator: Article 20 `research.md`, “Abstract model” single-accounting invariant，`20-C05` clock-domain resume contract，“Enforcement matrix”, “Exhaustion routes” and “Minimal Budget audit record”.
- Supported Claims: 20-C03, 20-C04, 20-C05, 20-C06, 20-C07, 20-C08, 20-C09.
- Evidence type: COURSE PROPOSAL / source-informed design.
- Status contribution: PROPOSAL only.
- Supported conclusion: the proposed lifecycle uses one consumption identity in one bucket at a time；course Step reserves before work and increments `used` exactly once at successful commit；Cost replaces reservation with measured/incurred-pending and later source-qualified actual while computing remaining against conservative outstanding；cross-domain latency resume uses a persisted absolute deadline with bounded clock uncertainty or fails closed. These design rules are internally traceable to distinct source boundaries and keep unknowns explicit.
- Does not prove: implementation feasibility, atomic reservation, race freedom, correct critical path, runtime performance, cost savings, quality, safety or production readiness.
- Limitations / drift: Required Lab NONE；experiments 0；runtime ABSENT；no provider calls or billing reads.
- Falsifier: an internal contradiction, unhandled concurrent reservation race, inability to preserve provider-native receipts, or future runtime observation that violates a proposed invariant requires revision or implementation research.

### Evidence 20-E11｜Repository ownership and BuildPilot ceiling

- Source identity: canonical Agent Engineering series plan；Article 20 card/README；Published Articles 01, 10, 11, 12 and 19；repository snapshot 2026-08-26.
- Exact locator: `docs/agent-engineering-series-plan.md` Part IV rows 20—22；Article 20 Frozen Boundaries；published sections on Token/Context, Step, retry/checkpoint budget, Context Budget and Article ownership.
- Supported Claims: 20-C06, 20-C07, 20-C08, 20-C09.
- Evidence type: repository primary contract.
- Status contribution: CONFIRMED for ownership boundaries；PROPOSAL for BuildPilot design.
- Supported conclusion: Article 20 owns Token/Step/Cost/Latency budget design；Article 21 retains Trace/Replay/Failure Taxonomy；Article 22 retains Eval/Golden Dataset/Regression；BuildPilot remains design-only and Article 20 requires no Lab.
- Does not prove: future Article 21/22 content, BuildPilot implementation/runtime, or any cost/latency/quality result.
- Limitations / drift: repository-local ownership can change only through authorized canonical edits；this worker did not edit canonical/global files.
- Falsifier: an authorized canonical revision reassigning these topics requires remapping before Draft.

## Provider contract preservation table

| Surface | Native unit / field example | Safe course use | Forbidden normalization |
|---|---|---|---|
| OpenAI Responses | input count；`max_output_tokens`；OpenAI response `usage` | provider-native receipt + adapter mapping | assume Anthropic-identical token categories or billing |
| Anthropic Messages | estimated preflight `input_tokens`；Anthropic usage/cache/service fields | provider-native estimate/receipt with uncertainty | treat estimate as exact final usage or OpenAI field equivalent |
| OpenAI Agents SDK | AI-invocation `turn` | product example for declared Step unit | rename all framework steps to turns |
| LangGraph | graph `super-step` | product example for declared graph cap | equate super-step to one LLM/tool call |
| OpenAI/Anthropic admin cost | historical cost/usage records | reconciliation source with freshness | call synchronous estimate “actual invoice” |
| FOCUS 1.4 | List/Effective/Billed cost bases | vocabulary for source-qualified monetary records | claim Agent reservation is a FOCUS field |

## Evidence Gate

- Decision: **RECOMMEND PASS**
- Coverage: 9 / 9 core Claims；11 Evidence Cards；Core BLOCKED Claims: 0.
- Status counts: CONFIRMED 1；PARTIAL 4；PROPOSAL 4；BLOCKED 0.
- Required Lab path: N/A；Required Lab NONE；Experiments 0；Runtime observation ABSENT.
- Source discipline: all moving product pages carry retrieval date and product scope；OpenAI Agents SDK `v0.22.0` (2026-08-19) and LangGraph `1.2.11` (2026-08-11) have exact official tag URLs but are not asserted as hosted-doc builds；FOCUS is pinned to publication 1.4；no current price/window number is embedded.
- Uncertainty discipline: estimate, reservation and actual remain distinct；hidden provider queueing, future billing, discount and non-token line items can remain `UNKNOWN`.
- Ownership discipline: audit refs stop before Article 21 Trace/Replay/Failure Taxonomy；quality claims stop before Article 22 Eval/Regression.
- Gate owner after worker result: MASTER_ORCHESTRATOR validates the package；worker recommendation is `OUTLINE` because every core Claim is traceable and none is BLOCKED.
