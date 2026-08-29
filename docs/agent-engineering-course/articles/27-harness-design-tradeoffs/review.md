# Article 27 Review

Status: `NOT_STARTED`

Review begins only after Evidence Gate, Outline and full Draft pass.

## Review Cycle 0｜Fresh Independent Review

## Review metadata

- Review Gate: `REVIEW`
- Reviewer: `/root/a27_reviewer`
- Review Cycle: `0 / 3`
- Review Date: `2026-08-30`
- Execution Type: `REAL_SUBAGENT / FRESH CONTEXT`
- Decision: `PASS`
- Recommended Next Gate: `FINAL_GATE`
- Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Allowed Write Used: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/review.md`

## Context isolation

- Read repository article method, outline template, series planning method and production workflow.
- Read Course Factory reviewer contract, Agent Engineering production workflow and review checklist.
- Read canonical Agent Engineering series plan, course status, run state, Article 27 README/Card/Research/Evidence/Outline/Draft/Subagent Trace and relevant published Articles 24-26.
- Recomputed the frozen Draft identity directly from the current file.
- Spot-checked current primary sources for drift and proof ceilings: Microsoft Azure Gateway Aggregation, MCP Tools `2025-06-18`, OpenAI Agents SDK HITL/Tracing, OpenTelemetry sensitive-data guidance, GitHub rulesets, LangGraph persistence, Temporal workflow execution and LangChain product terminology.
- Did not read or rely on Author hidden reasoning, Author confidence or Author self-score. Did not edit Draft, Research, Evidence, Outline, README, status, run-state, published content, future Article workspaces, Git history or remotes.

## Required identity recompute

- Draft path: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/draft.md`
- Recorded frozen identity: `41491 bytes / 496 lines / SHA-256 BDB726D1AD21F4D24433E87BF50C3075394B3FC017A6CAE6F1137B9AFB17E290`
- Recomputed bytes: `41491`
- Recomputed physical lines: `496`
- Recomputed SHA-256: `BDB726D1AD21F4D24433E87BF50C3075394B3FC017A6CAE6F1137B9AFB17E290`
- Result: `PASS / IDENTITY_MATCH`

## Claim, card and status recompute

- Evidence register: `11` unique Claim IDs, exactly `27-C01` through `27-C11`.
- Evidence cards: `11` unique Evidence IDs, exactly `27-E01` through `27-E11`.
- Draft traceability table: `11` rows, exactly `27-C01` through `27-C11`.
- Status mix preserved in README, Research, Evidence and Draft: `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
- Required Lab boundary preserved: `NONE`.
- Experiment Count preserved: `0`.
- Runtime Observation preserved: `ABSENT`.
- BuildPilot boundary preserved as `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
- New core Claim/Card required: `NONE`.
- Result: `PASS`

## Findings

No Findings issued in namespace `A27-R0-F##`.

## Review checks

### Technical accuracy

- Result: `PASS`
- Notes:
  - Draft treats Harness adoption as an engineering investment decision, not as a mandatory maturity path.
  - Runtime, Harness, Host, Workflow, Tool, Policy, Trace, Evidence, Approval, Knowledge and Recovery remain distinct enough for the current Part V level.
  - Stage 0-4 is consistently framed as a reversible course proposal, not an external standard or maturity ranking.
  - BuildPilot remains a design case and never becomes runtime, CI, Unity/Jenkins scan, PR creation, deployment or production evidence.

### Evidence discipline

- Result: `PASS`
- Notes:
  - Every core conclusion is tied to the `27-C##` / `27-E##` evidence set.
  - `PARTIAL` claims use cautious wording such as `can reduce`, `can introduce` and `must be balanced`; no sentence upgrades them to universal law.
  - `PROPOSAL` claims cover Stage 0-4, no-build/remain-low-stage and BuildPilot V1 recommendations only.
  - No ROI, cost, latency, storage, reviewer-throughput, defect-reduction, lab, runtime or production-safety metric is invented.

### Teaching quality and engineering transfer

- Result: `PASS`
- Notes:
  - The article follows the required principle spine: problem space -> adoption judgment -> benefit/cost/risk model -> replaceability -> bloat/false safety -> no-build/stage model -> BuildPilot V1 -> evidence boundary.
  - It gives practical reader actions through the governance-word audit, adoption decision record and smallest-useful-slice checklist.
  - Benefits and costs are symmetric: centralization is presented as reducing duplicated drift while adding bottleneck, coupling, privacy, migration, approval and recovery burden.
  - No-build and remain-low-stage outcomes are presented as valid architecture choices, not as reluctance or immaturity.

### Course consistency and scope containment

- Result: `PASS`
- Notes:
  - Canonical Article 27 scope is satisfied: Part V, Article Type `PRINCIPLE`, Course Weight `M`, Optional `NO`, Required Lab `NONE`.
  - Published Article 24 is used only as the why-Harness-pressure bridge, Article 25 as the Runtime/Harness boundary bridge, and Article 26 as the minimum-model bridge.
  - The Draft adds Article 27-specific adoption, bloat, replaceability, no-build and BuildPilot V1 restraint instead of re-teaching Articles 24-26.
  - Article 28 / Part VI / DeepSeek Harness source reading and Part VII BuildPilot implementation architecture remain explicitly forbidden and not started.

### Source, version and publication risk

- Result: `PASS_WITH_NOTES`
- Notes:
  - Live spot-check date: `2026-08-30 Asia/Shanghai`.
  - Microsoft Azure Gateway Aggregation supports both aggregation benefits and risks such as coupling, SPOF, bottleneck, cascading failure and latency: `https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-aggregation`.
  - MCP Tools `2025-06-18` supports tool discovery/call/schema and says tool annotations require trust handling; this matches the Draft's `tool visible != authorized` boundary: `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`.
  - OpenAI Agents SDK HITL and Tracing support pause/resume state and sensitive trace payload concerns, but not BuildPilot runtime or measured outcome claims: `https://openai.github.io/openai-agents-python/human_in_the_loop/`, `https://openai.github.io/openai-agents-python/tracing/`.
  - OpenTelemetry sensitive-data guidance supports privacy/minimization/retention/redaction caution; it does not prove trace retention is automatically safe: `https://opentelemetry.io/docs/security/handling-sensitive-data/`.
  - GitHub rulesets, LangGraph persistence, Temporal workflow execution and LangChain product terminology remain analogy/source-boundary support only; the Draft keeps them out of universal Harness claims.
  - Draft `relref` preflight: `4 / 4` shortcode occurrences use ASCII double quotes and resolve to two existing targets: `content/ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md` and `content/ai-empowerment/agent-engineering-series-index.md`.
  - Placeholder/static hygiene preflight: no `DATA-TODO`, `EXPERIENCE-TODO`, `TODO`, `TBD`, `FIXME` or `XXX` marker was found in the Draft; code fences are paired; trailing-whitespace scan is clean.
  - Future asset guard: Article 27 published content is absent, Article 28 workspace/content/static assets are absent, and Article 27 static image assets are absent as expected before Publisher.
  - Hugo build was not run at Review Gate because this worker is restricted to appending `review.md` and the Draft is not yet mapped into Hugo content; build verification remains a downstream Publisher/Build Gate responsibility.

## Five-dimensional score

| Dimension | Score | Basis |
|---|---:|---|
| Technical accuracy | `19 / 20` | Core boundaries are sound; no runtime, maturity-standard or BuildPilot implementation overclaim found. |
| Evidence discipline | `19 / 20` | 11/11 Claims and Cards match; proof ceilings are repeated in opening, traceability and source-boundary sections. |
| Teaching quality | `19 / 20` | The article teaches adoption judgment from concrete pressure and counter-pressure rather than from a module list. |
| Engineering transfer | `19 / 20` | Governance-word audit, adoption record, Stage table and BuildPilot V1 matrix are directly usable. |
| Readability and compression | `18 / 20` | Medium-weight draft is dense but coherent; repetition with 24-26 is limited to bridge context. |
| **Total** | **`94 / 100`** | **No actionable Finding remains open.** |

Threshold result: `PASS_FOR_REVIEW`. Total `94 / 100`; all core dimensions meet current course expectations.

## Open Finding Summary

| Severity | Open count | Finding IDs |
|---|---:|---|
| BLOCKER | `0` | `NONE` |
| MAJOR | `0` | `NONE` |
| MINOR | `0` | `NONE` |
| EDITORIAL | `0` | `NONE` |
| **Total actionable** | **`0`** | **`NONE`** |

## Gate decision

- Review Decision: `PASS`
- Findings Requiring Edits: `NONE`
- Recommended Route: `FINAL_GATE`
- Blocker: `NONE`
- Recheck Required: `NO`
- Return To Research Required: `NO`
- New Lab Required: `NO`
- Publication Eligibility: `NOT_EVALUATED_AT_REVIEW_GATE`
- Non-claim boundary: Review `PASS` is not Published Content, not Hugo Build verification, not checkpoint commit, not push/remote verification, and not `END_ARTICLE`.

## Final Gate Review｜/root/a27_final_reviewer

### Final Gate metadata

- Gate: `FINAL_GATE`
- Reviewer: `/root/a27_final_reviewer`
- Review Date: `2026-08-30`
- Execution Type: `REAL_SUBAGENT / FRESH CONTEXT`
- Decision: `PASS`
- Recommended Next Gate: `PUBLISH`
- Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Allowed Write Used: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/review.md`

### Independent evidence recheck

- Draft identity recomputed as `41491 bytes / 496 physical lines / SHA-256 BDB726D1AD21F4D24433E87BF50C3075394B3FC017A6CAE6F1137B9AFB17E290`; result: `PASS / IDENTITY_MATCH`.
- Current Review state is `PASS / 94 / 0 OPEN`; Review Cycle 0 remains valid.
- Evidence register contains exactly `27-C01` through `27-C11`; Evidence cards contain exactly `27-E01` through `27-E11`; Draft traceability table covers `11 / 11`.
- Evidence posture remains `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`; no `PARTIAL` or `PROPOSAL` claim is upgraded by the Draft.
- Required Lab remains `NONE`; Experiment Count remains `0`; Runtime Observation remains `ABSENT`.
- BuildPilot remains explicitly labelled `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`; the Draft does not claim Unity scan, Jenkins access, PR creation, project mutation, deployment, runtime trace, cost/latency/defect improvement or production verification.

### Final quality and boundary recheck

- TwoEgg principle-article spine is intact: Article 24-26 bridge -> adoption problem -> benefit/cost/risk model -> replaceability -> bloat and false safety -> no-build and reversible stages -> restrained BuildPilot V1 -> evidence boundary.
- Symmetric trade-offs are preserved: shared governance `can reduce` duplicated drift while a central layer `can introduce` bottleneck, coupling, SPOF, latency, privacy, approval fatigue, migration and recovery burden.
- Stage 0-4 is explicitly a course adoption model, not an external standard, maturity ranking or one-way upgrade path; Stage 0/1/2 remain valid destinations with rollback and no-build options.
- Replaceability is tied to real variation pressure, migration need, second consumer or independently versioned lifecycle; the Draft does not recommend pluginization by default.
- Risk coverage includes policy drift, misconfiguration, stale knowledge, wrong intent, trusted-looking tool hints, stale approval, redacted trace limits, memory freshness, eval boundary, approval fatigue, privacy/observability conflict and recovery uncertainty.
- BuildPilot V1 remains restrained: `ADOPT` only read checks, Evidence, Trace reference, Change Request, Human Review and unknown/stale labels; `SIMPLIFY` budget/capability handling; `DEFER` knowledge graph, multi-trial eval, governed capability evolution, durable replay, autonomous modification, PR creation and deployment; `REJECT` all already-running or benefit claims.
- Article 28 / Part VI / DeepSeek Harness source reading and Part VII BuildPilot architecture are not started or preview-written; after Article 27 completes its publication transaction, the durable handoff is to Part V Audit and STOP, not Part VI.

### Source and publication preflight

- Current primary-source spot check found no contradiction to the Draft's proof ceilings: Microsoft Gateway Aggregation supports both aggregation benefit and coupling/SPOF/bottleneck/latency/cascading-failure risk; MCP Tools `2025-06-18` supports tool discovery/call/schema while requiring trust, access control, confirmation, timeout and audit caution; OpenAI Agents SDK HITL/Tracing supports pause-resume state and sensitive trace payload concerns; OpenTelemetry sensitive-data guidance supports minimization, consent, storage/review and redaction caution; GitHub rulesets support stale approval/status-check boundaries; LangChain/LangGraph/Temporal/NIST support terminology, persistence/recovery and measurement-governance boundaries without proving BuildPilot runtime or measured outcomes.
- Source URLs checked: `https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-aggregation`; `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`; `https://openai.github.io/openai-agents-python/human_in_the_loop/`; `https://openai.github.io/openai-agents-python/tracing/`; `https://opentelemetry.io/docs/security/handling-sensitive-data/`; `https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets`; `https://docs.langchain.com/oss/python/concepts/products`; `https://docs.langchain.com/oss/javascript/langgraph/persistence`; `https://docs.temporal.io/workflow-execution`; `https://airc.nist.gov/airmf-resources/airmf/5-sec-core/`.
- Draft `relref` preflight: `4 / 4` shortcodes use ASCII double quotes and resolve to existing `content/ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md` and `content/ai-empowerment/agent-engineering-series-index.md`.
- Placeholder/static hygiene preflight: no `DATA-TODO`, `EXPERIENCE-TODO`, `TODO`, `TBD`, `FIXME` or `XXX` marker was found in the Draft; `14` fence markers are paired; trailing-whitespace scan is clean; Draft has no frontmatter block.
- Future asset guard: Article 27 published content is still absent as expected before Publisher; Article 27 named static/image assets are absent; Article 28 workspace, published content and named static/image assets are absent.
- Hugo build was not run at Final Gate because this worker is restricted to appending `review.md` and the Draft is not yet published into Hugo content; build verification remains the downstream Publisher/Build Gate responsibility.

### Five-dimensional score

| Dimension | Score | Threshold |
|---|---:|---:|
| Technical Accuracy | `19 / 20` | `>= 18` |
| Evidence Discipline | `19 / 20` | `>= 18` |
| Teaching Quality | `19 / 20` | `>= 17` |
| Engineering Transfer | `19 / 20` | `>= 17` |
| Readability & Compression | `18 / 20` | contributes to total |
| **Total** | **`94 / 100`** | **`>= 88`** |

Threshold result: `PASS_FOR_FINAL_GATE`. Total score is above `88 / 100`; all explicitly thresholded dimensions meet the current course contract.

## Final Gate decision

- Final Gate Decision: `PASS`
- Recommendation: `PUBLISH`
- Publication Eligibility: `ELIGIBLE_FOR_PUBLISH_GATE`
- Findings Requiring Edits: `NONE`
- Remaining Open Findings: `0`
- Blocker: `NONE`
- Next Allowed Gate: `PUBLISH`
- Exact route: `FINAL_GATE -> PUBLISH`
- Non-claim boundary: Final Gate `PASS` is not Published Content, not Hugo Build verification, not checkpoint commit, not push/remote verification, not Part V Audit and not `END_ARTICLE`.

## EOF Normalization Recheck｜/root/a27_eof_reviewer

### Recheck metadata

- Gate: `REVIEW_RECHECK`
- Reviewer: `/root/a27_eof_reviewer`
- Review Date: `2026-08-30`
- Execution Type: `REAL_SUBAGENT / FRESH CONTEXT`
- Decision: `PASS`
- Recommended Next Gate: `PRE_COMMIT_RECONCILIATION`
- Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Allowed Write Used: `docs/agent-engineering-course/articles/27-harness-design-tradeoffs/review.md`

### Independent EOF evidence

- Normalized Draft identity recomputed directly from bytes: `41490 bytes / 495 physical lines / SHA-256 259C682BD84C557BCEFF20171595F24D8097B8C3E27A5155EF8069DC7FCD3E9F`; result: `PASS / IDENTITY_MATCH`.
- Prior reviewed Draft semantic equivalence: adding back one trailing LF to the normalized Draft produces prior frozen SHA-256 `BDB726D1AD21F4D24433E87BF50C3075394B3FC017A6CAE6F1137B9AFB17E290`; this matches the Review and Final Gate record for `41491 bytes / 496 lines`, so the only Draft delta is the terminal blank line.
- EOF state: normalized Draft, Outline and Published Content all end with one LF and do not end with a blank line. Current identities are Draft `41490 / 495 / 259C682B...CD3E9F`, Outline `30350 / 646 / 75DC7153...944DE`, Published Content `42408 / 517 / 10C8E972...5AF9`.
- Published exact body mapping remains intact: `content/ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md` contains the normalized Draft as exactly `1` contiguous byte occurrence at offset `918`.
- `git diff --check` exits `0` and no longer reports EOF blank lines; output contains only LF-to-CRLF working-copy warnings. `git diff --cached --check` still reports the old three EOF blanks because the index has not been refreshed after normalization; that is stale staged evidence for the next `PRE_COMMIT_RECONCILIATION` to replace, not a failure of the working-tree normalization.

### Boundary revalidation

- Evidence set remains exactly `11 / 11`: Evidence register has `27-C01` through `27-C11`, Evidence cards have `27-E01` through `27-E11`, and Draft traceability covers `27-C01` through `27-C11`.
- Evidence posture remains `1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`; Required Lab remains `NONE`, Experiment Count remains `0`, Runtime Observation remains `ABSENT`.
- No-build, Stage 0-4 and BuildPilot boundaries remain unchanged: Stage 0-4 is a course proposal, no-build / remain-low-stage stays valid, and BuildPilot remains `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
- Navigation remains unchanged after EOF normalization: Article 26 links to Article 27 at top and bottom; Article 27 links back to Article 26; the course index marks Article 27 as published and keeps Article 28 planned/unlinked.
- Future boundary remains unchanged: Article 28 workspace/content/static asset search returns no Article 28 production artifact; course state still marks Article 28 `FORBIDDEN / ZERO ASSETS`; Part V Audit and STOP remain the only next transaction candidate after Article 27 reaches `END_ARTICLE`.
- No Hugo build was run in this recheck. The existing Publisher/Build result remains historical candidate evidence only; this gate verifies EOF normalization and routes back to Master for a fresh persistence cut.

### Recheck decision

- EOF Normalization Recheck Decision: `PASS`
- Findings Requiring Edits: `NONE`
- Remaining Open Findings: `0`
- Blocker: `NONE`
- Recommended Route: `REVIEW_RECHECK -> PRE_COMMIT_RECONCILIATION`
- Non-claim boundary: This recheck is not a new Hugo build, not staging, not checkpoint commit, not push/remote verification, not Part V Audit, not Article 28 kickoff and not `END_ARTICLE`.
