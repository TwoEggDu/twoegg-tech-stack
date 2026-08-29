# Article 25 Review｜Agent Runtime vs Harness：执行内核与工程控制面

## Review metadata

- Review Gate: `REVIEW`
- Reviewer: `/root/a25_reviewer`
- Review Cycle: `0 / 3`
- Review Date: `2026-08-29`
- Execution Type: `REAL_SUBAGENT / FRESH CONTEXT`
- Decision: `PASS`
- Recommended Next Gate: `FINAL_GATE`
- Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Allowed Write Used: `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md`

## Context isolation

- Read repository article method, outline template, series planning method and production workflow.
- Read Course Factory runtime contract, Agent Engineering production workflow, Reviewer contract and review checklist.
- Read canonical Part V row, glossary, course status, Article 25 README/Card/Research/Evidence/Outline/Draft and Published Article 24.
- Spot-checked current official source pages used for terminology variance and Host/Harness/approval boundaries: OpenAI Agents SDK announcement, Microsoft Agent Framework Harness and Tool Approval, LangChain Runtimes/Frameworks/Harnesses, and MCP Architecture.
- Did not read or rely on Author hidden reasoning, Author confidence or Author self-score. Did not edit Draft, Evidence, Research, Outline, README, status, run-state, published content, future Article workspaces, Git history or remotes.

## Required identity recompute

- Draft path: `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/draft.md`
- Recorded frozen identity in Article README / status: `39916 bytes / 561 lines / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4`
- Recomputed bytes: `39916`
- Recomputed physical lines: `561`
- Recomputed SHA-256: `9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4`
- Result: `PASS / IDENTITY_MATCH`

## Claim, card and status recompute

- Evidence register: `12` unique Claim IDs, exactly `25-C01` through `25-C12`.
- Evidence cards: `12` unique Evidence IDs, exactly `25-E01` through `25-E12`.
- Draft traceability table: `12` rows, exactly `C01` through `C12`.
- Status mix preserved in README, Research, Evidence and Draft: `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`.
- Required Lab boundary preserved: `NONE`.
- Experiment Count preserved: `0`.
- Runtime Observation preserved: `ABSENT`.
- New core Claim/Card required: `NONE`.
- Result: `PASS`

## Findings

No open Findings. No `A25-R0-FNN` Finding is emitted in Cycle 0.

## Review checks

### Technical accuracy

- Result: `PASS`
- Notes:
  - The Draft keeps the Runtime/Harness/Host split as course-defined responsibility taxonomy, not industry or vendor standard.
  - Runtime is consistently framed as execution progression: model call, step loop, tool dispatch, wait/resume, local execution state and stop/error boundaries.
  - Harness is consistently framed as shared governance semantics: identity, permission, approval, sandbox, budget, trace, evidence, checkpoint/replay policy, registry/discovery and recovery convention.
  - Host, Business Agent/Workflow, Agent Framework and Workflow Engine remain comparison lenses with distinct owner/state/lifetime boundaries.
  - Current official source spot-checks support the draft's terminology-variance premise: Microsoft packages model/tool calls, state, context, approvals and UX inside an Agent Harness; LangChain separates runtimes/frameworks/harnesses in its own product model; OpenAI uses model-native harness language; MCP defines Host/Client/Server instead of the course taxonomy.

### Evidence discipline

- Result: `PASS`
- Notes:
  - `CONFIRMED`, `PARTIAL` and `PROPOSAL` strengths are visible in the opening proof ceiling and the Claim Traceability table.
  - `PARTIAL` claims stay as course synthesis or engineering judgment; no `PARTIAL` claim is promoted to universal fact.
  - `PROPOSAL` claims are limited to the five-question teaching model and BuildPilot design allocation.
  - Tool availability, permission, execution success, trace existence, checkpoint, replay and accepted evidence are not collapsed.
  - The Draft does not introduce runtime logs, metrics, screenshots, Unity/Jenkins/CI runs, BuildReports, PRs, production writes or device observations.

### Teaching quality and engineering transfer

- Result: `PASS`
- Notes:
  - The structure follows the required principle-article progression: Article 24 bridge -> product-name problem space -> five-question abstract model -> responsibility ledger -> state/context/registry/failure mechanisms -> BuildPilot design case -> replacement pressure -> bounded conclusion.
  - The article leads with engineering confusion and responsibility ownership, not API lists.
  - The four-state-owner model and registry/discovery split are concrete enough for design review, while still preserving evidence ceilings.
  - The BuildPilot section gives a usable responsibility allocation without presenting BuildPilot as implemented or capable of direct production mutation.

### Course consistency and scope containment

- Result: `PASS`
- Notes:
  - Canonical Article 25 responsibility is satisfied: it answers Agent Runtime vs Harness and places Host relative to both.
  - Published Article 24's handoff is respected: Article 24 explains why the shared boundary appears; Article 25 splits execution and governance responsibility.
  - Article 26 remains future work for the minimum Harness capability model; Article 27 remains future work for bloat, cost, adoption and evolution trade-offs.
  - Article 23 remains optional/skipped and Article 28 remains forbidden/out of scope under current Factory state.
  - BuildPilot remains `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.

### Source, version and publication risk

- Result: `PASS_WITH_LIMITED_SOURCE_DRIFT_NOTE`
- Notes:
  - Source-sensitive claims rely on exact Evidence cards; Draft references are intentionally lighter than Evidence, but no sentence currently requires a new source or stronger claim status.
  - Live spot-check date: `2026-08-29`. OpenAI, Microsoft, LangChain and MCP source pages currently support the draft's limited terminology and responsibility-separation use.
  - Microsoft Learn pages are current but still version/drift-prone; if publication is delayed, Publisher should re-check exact links and dates before mapping references into public content.
  - The Draft contains one internal `relref`, and it targets existing Published Article 24.
  - No front matter is expected at Draft gate; Outline contains a valid publication metadata plan for the later Publisher.

## Five-dimensional score

| Dimension | Score | Basis |
|---|---:|---|
| Technical accuracy | 19 / 20 | Responsibility split matches glossary, evidence and live source spot-checks. |
| Evidence discipline | 19 / 20 | Claim/card/status identity is exact; proof ceilings are repeated at opening and traceability. |
| Teaching quality | 18 / 20 | Strong progression and concrete tables; some repetition is acceptable for an L-weight boundary article. |
| Engineering transfer | 19 / 20 | Five-question model, state owner split, registry chain and failure table are usable in real design review. |
| Readability and compression | 18 / 20 | Long but controlled; tables carry the density without turning into an SDK tutorial. |

Overall score: `93 / 100`

Threshold result: `PASS_FOR_FINAL_GATE`. There are no open `BLOCKER`, `MAJOR`, `MINOR` or `EDITORIAL` Findings.

## Gate decision

- Review Decision: `PASS`
- Findings Requiring Edits: `NONE`
- Recommended Route: `FINAL_GATE`
- Blocker: `NONE`
- Recheck Required: `NO`
- Publication Eligibility: `NOT_EVALUATED_AT_REVIEW_GATE`
- Non-claim boundary: Review `PASS` is not Published Content, not Hugo Build verification, not checkpoint commit, not push/remote verification, and not `END_ARTICLE`.

## Final Gate Review｜/root/a25_final_reviewer

### Final Gate metadata

- Gate: `FINAL_GATE`
- Reviewer: `/root/a25_final_reviewer`
- Review Date: `2026-08-29`
- Execution Type: `REAL_SUBAGENT / FRESH CONTEXT`
- Decision: `PASS`
- Recommended Next Gate: `PUBLISH`
- Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Allowed Write Used: `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md`

### Independent evidence recheck

- Draft identity recomputed as `39916 bytes / 561 lines / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4`; result: `PASS / IDENTITY_MATCH`.
- Current Review state is `PASS / 93 / 0 OPEN`; the prior Review's threshold result remains `PASS_FOR_FINAL_GATE`.
- Draft Claim Traceability table contains exactly `12` rows, `C01` through `C12`, matching Evidence Cards `25-E01` through `25-E12`.
- Evidence posture remains `4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`; no `PARTIAL` or `PROPOSAL` claim is upgraded in Draft wording.
- Required Lab remains `NONE`; Experiment Count remains `0`; Runtime Observation remains `ABSENT`.
- BuildPilot remains explicitly labelled `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`; Draft does not claim Unity scan, Jenkins access, PR creation, project mutation, deployment, runtime trace or production verification.

### Source and course-boundary recheck

- Live source spot-check date: `2026-08-29`. OpenAI Agents SDK running/HITL/context docs, Microsoft Agent Framework Harness/tool approval/workflow state docs, LangChain runtime/framework/harness and LangGraph persistence docs, MCP Architecture/Tools/Roots, Temporal workflow execution, OpenTelemetry Trace API and Unity BuildReport pages continue to support only the limited responsibility-separation and terminology-variance claims used by Article 25.
- Vendor terminology variance is explicit: Microsoft can package execution, state, approvals and UX under Agent Harness; LangChain uses runtime/framework/harness in its own product model; OpenAI product language uses model-native harness; MCP uses Host/Client/Server rather than the course taxonomy.
- The Runtime/Harness/Host/Business split is consistently labelled as a course teaching taxonomy, not an industry standard, vendor standard or universal architecture.
- Published Article 24 is used only as the prior bridge: it explains why shared Harness pressure appears; Article 25 answers responsibility allocation. Article 26's minimum capability model and Article 27's bloat/adoption/trade-off framework are named only as future boundaries, not implemented here.
- Article 25 Published Content is absent, and Article 26/27 workspace/content/image assets are absent; no future Article asset is preempted by this Final Gate.

### Final Gate decision

- Final Gate Decision: `PASS`
- Findings Requiring Edits: `NONE`
- Recommended Route: `PUBLISH`
- Blocker: `NONE`
- Publication Eligibility: `ELIGIBLE_FOR_PUBLISH_GATE`
- Non-claim boundary: Final Gate `PASS` is not Published Content, not Hugo Build verification, not checkpoint commit, not push/remote verification, and not `END_ARTICLE`.

## Revision Repair Record｜PV-AUD-F02 / Cycle 1

- Repair Gate: `REVISION`
- Worker: `/root/part_v_a25_revision_cycle1`
- Repair Date: `2026-08-30`
- Finding: `PV-AUD-F02 MINOR`
- Disposition: `FIXED_FOR_ARTICLE_25 / READY_FOR_REVIEW_RECHECK`
- Scope: removed only the duplicate draft-internal top `上一篇` navigation block from Draft and Published body.
- Preserved: publisher shell top navigation, Published bottom navigation, teaching content, evidence posture, Claim Traceability and all reference links.
- Draft identity after repair: `39742 bytes / 559 lines / SHA-256 EB43977112FD2940A5E8D01B728CA6FE0DCCD60D1CCC296C1987E1E964217CD3`.
- Published identity after repair: `41282 bytes / 589 lines / SHA-256 281C493F42E0968766BDFB6DFE57450C390AA9D84B9B94254C8444D7D6350AEE`.
- Verification: repaired Draft exact block appears in Published exactly `1` time; Published navigation scan keeps top `上一篇 / 下一篇 / 课程索引` and bottom `上一篇 / 下一篇 / 课程索引`.

## Reviewer Recheck｜PV-AUD-F02 / Cycle 1

- Recheck Gate: `REVIEW_RECHECK`
- Reviewer: `/root/part_v_a25_reviewer_cycle1`
- Recheck Date: `2026-08-30`
- Execution Type: `REAL_SUBAGENT / FRESH INDEPENDENT REVIEWER`
- Finding: `PV-AUD-F02 MINOR`
- Finding Disposition: `CLOSED_FOR_ARTICLE_25`
- Decision: `PASS`
- Recommended Next Gate: `PART_V_AUDIT`
- Allowed Write Used: `docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md`

### Recheck evidence

- Required scope read: Part V Audit `PV-AUD-F02`, Article 25 Revision Repair Record, current Draft, current Published content, Reviewer contract, review checklist and production workflow.
- Current diff for Draft and Published removes only the duplicate draft-internal top `上一篇` navigation block after the H1; no non-navigation body line, evidence table line, reference link or bottom navigation line is changed.
- Draft identity recomputed: `39742 bytes / 559 lines / SHA-256 EB43977112FD2940A5E8D01B728CA6FE0DCCD60D1CCC296C1987E1E964217CD3`.
- Published identity recomputed: `41282 bytes / 589 lines / SHA-256 281C493F42E0968766BDFB6DFE57450C390AA9D84B9B94254C8444D7D6350AEE`.
- Exact current Draft block appears in current Published content exactly `1` time.
- Current Draft contains no draft-internal top `上一篇` navigation block.
- Current Published content keeps publisher top navigation exactly once each for `上一篇`, `下一篇` and `课程索引`, and keeps bottom navigation exactly once each for the same three links.
- Semantic preservation check passed: current Draft equals the pre-repair Draft with only the duplicate top navigation block removed; current Published content equals the pre-repair Published content with only the duplicate draft-internal top navigation block removed.
- `git diff --check -- docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/draft.md content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md` exited `0`.
- Fresh `hugo --gc --minify` exited `0`: Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`; `Pages=1255`, `Static files=44`, `Aliases=1`, `Total=6152 ms`; captured output contained no `ERROR` or `WARNING` line.

### Recheck decision

`PV-AUD-F02` is `CLOSED_FOR_ARTICLE_25`. Article 25 now preserves publisher top navigation, bottom navigation, exact Draft-to-Published identity, and all non-navigation body semantics / evidence / links. This recheck does not close Article 26 or Article 27 instances of `PV-AUD-F02`, does not address `PV-AUD-F01`, and does not advance durable Course Factory state.
