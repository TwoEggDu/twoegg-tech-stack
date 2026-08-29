# Article 24 Review｜为什么最终需要 Harness：横切能力由谁承载

## Review metadata

- Review Gate: `REVIEW`
- Reviewer: `/root/a24_reviewer`
- Review Cycle: `0 / 3`
- Review Date: `2026-08-29`
- Execution Type: `REAL_SUBAGENT / FRESH CONTEXT`
- Decision: `FAIL`
- Recommended Next Gate: `REVISION`
- Open Findings: `1 MAJOR / 0 BLOCKER / 0 MINOR / 0 EDITORIAL`
- Allowed Write Used: `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/review.md`

## Required identity recompute

- Draft path: `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/draft.md`
- Recomputed bytes: `41730`
- Recomputed lines: `474`
- Recomputed SHA-256: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
- Recorded frozen SHA-256 in Article README / state: `60786290380F3BAEB4F61FF818D94CE79476A60E8D8E85FE8C77FBE5E83D7F6C`
- Result: `FAIL / SHA_MISMATCH`

The byte and line identity matches the recorded handoff, but the cryptographic identity does not. This is the only blocking review issue found in Cycle 0.

## Findings

### A24-R0-F01

- Severity: `MAJOR`
- Category: `EVIDENCE`
- Location: `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/README.md:33`; `docs/agent-engineering-course/status.md:6`; `docs/agent-engineering-course/course-run-state.md:70`; current Draft file `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/draft.md`
- Claim: Article 24 handoff records the full Draft as `41730 bytes / 474 lines / SHA-256 60786290380F3BAEB4F61FF818D94CE79476A60E8D8E85FE8C77FBE5E83D7F6C`.
- Evidence: Fresh recomputation with `Get-FileHash -Algorithm SHA256`, `certutil -hashfile ... SHA256`, and direct byte hashing all returned `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`; `(Get-Item).Length` returned `41730`; `(Get-Content).Count` returned `474`.
- Why It Matters: The Draft is otherwise publication-ready, but the Review contract explicitly requires recomputing Draft byte/line/SHA identity. A mismatched frozen hash means the Publisher/Master cannot prove which exact Draft text passed Review, and a later Draft/Published identity check could publish an artifact that does not match the recorded author handoff.
- Required Disposition: Reconcile the frozen Draft identity before Final Gate. Either restore the intended Draft whose SHA is `60786290380F3BAEB4F61FF818D94CE79476A60E8D8E85FE8C77FBE5E83D7F6C`, or, if the current Draft is authoritative, update the Article 24 README / status / run-state identity records to `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040` through the proper Master-owned state path. Then rerun byte/line/SHA verification and return to Reviewer recheck.

## Review checks

### Technical accuracy

- Result: `PASS_WITH_FINDING_SCOPE`
- Notes:
  - The article correctly introduces Harness as a course-defined shared carrying boundary, not as an industry standard component.
  - Prompt, Tool wrapper, Workflow, CI/Review gate and team convention are separated with clear ownership limits.
  - Trace, Evidence, Budget, Eval, Approval, Recovery and Knowledge remain adjacent but distinct control surfaces.
  - No technical claim was found that requires a new Research Card beyond `24-C01` through `24-C12`.

### Evidence discipline

- Result: `FAIL_ON_IDENTITY / CONTENT_BOUNDARY_PASS`
- Notes:
  - `12 / 12` Draft Claim IDs are present: `24-C01` through `24-C12`.
  - `12 / 12` Draft Evidence IDs are present: `24-E01` through `24-E12`.
  - Evidence posture is preserved exactly as `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
  - `PARTIAL` and `PROPOSAL` claims are mostly phrased with analogy, proposal or design-case ceilings.
  - Finding `A24-R0-F01` blocks Final Gate until frozen Draft identity is reconciled.

### Teaching quality

- Result: `PASS`
- Notes:
  - The Draft follows the required progression: Part IV handoff -> problem pressure -> abstract cross-cutting model -> Harness boundary -> BuildPilot design case -> engineering anti-patterns -> Article 25 bridge.
  - The article does not open with APIs or product documentation. Public sources are used to support responsibility separation, not to become the article's main teaching surface.
  - The BuildPilot example is concrete enough for a C# / Unity reader while retaining evidence limits.

### Course consistency and scope containment

- Result: `PASS`
- Notes:
  - Canonical Article 24 scope is respected: Part V first Harness principle article, `L`, non-optional, Required Lab `NONE`.
  - Article 25 Runtime/Harness split, Article 26 Capability model and Article 27 cost/bloat/adoption discussion are deferred rather than completed here.
  - Article 23 remains optional/skipped; no Article 25-28 workspace, published content or image asset was found.
  - Article 21/22 handoff is consistent: Article 21 gives trace/replay boundaries, Article 22 closes eval/regression, and Article 24 picks up the unresolved "who carries shared semantics" problem.

### BuildPilot / Lab / runtime boundary

- Result: `PASS`
- Notes:
  - BuildPilot remains `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
  - Required Lab remains `NONE`; Experiment Count remains `0`; Runtime Observation remains `ABSENT`.
  - The Draft explicitly says BuildPilot has not run Unity, Jenkins, CI, Addressables Analyze, device tests, PR creation, production writes or runtime measurements.
  - `INTENT_DRIFT` is constrained by evidence; code-only inference remains `CANDIDATE_INTENT`.

### Source, relref and Hugo/publication risk

- Result: `PASS_WITH_SOURCE_DRIFT_LIMIT`
- Notes:
  - Single Draft `relref` structurally points to existing `content/ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md`.
  - Draft has no frontmatter, which is expected at Draft gate; the Outline includes a valid publication metadata plan for Publisher.
  - External URLs are structurally present in Research/Evidence/Draft. I did not live-browse hosted sources because no source contradiction requiring escalation was found; Evidence already preserves drift risk and says downstream workers should re-check URLs if publication is delayed or exact line references become necessary.
  - No published Article 24 content exists yet, which matches current gate.

## Five-dimensional score

| Dimension | Score |
|---|---:|
| Technical accuracy | 92 / 100 |
| Evidence discipline | 82 / 100 |
| Teaching quality / readability | 91 / 100 |
| Course consistency / scope containment | 94 / 100 |
| Publication / source risk | 78 / 100 |

Overall score: `87 / 100`

Threshold result: `FAIL_FOR_FINAL_GATE` because one open `MAJOR` finding remains. The completed Review artifact is valid for routing to `REVISION`; it is not eligible for `FINAL_GATE` until `A24-R0-F01` is closed by recheck.

## Gate decision

- Review Decision: `FAIL`
- Findings Requiring Edits: `A24-R0-F01`
- Recommended Route: `REVISION`
- Blocker: `NONE`
- Recheck Required: `YES`

## Revision Disposition｜Cycle 0

- Revision Execution ID: `/root/a24_revision`
- Scope: `A24-R0-F01` only
- Draft identity recomputed before revision: SHA-256 `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`; `41730 bytes`; `474 physical lines`
- Authority Boundary: This record proposes `READY_FOR_RECHECK` only; Finding closure and global status / run-state identity projection remain Master / Reviewer owned.

### A24-R0-F01 Disposition

- Finding ID: `A24-R0-F01`
- Files Changed: `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/README.md`, `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/review.md`
- Before: Review Cycle 0 recorded stale frozen SHA-256 `60786290380F3BAEB4F61FF818D94CE79476A60E8D8E85FE8C77FBE5E83D7F6C` for the Draft identity, while fresh recomputation returned `41730 bytes / 474 lines / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`.
- After: Article README records the authoritative current Draft identity as `41730 bytes / 474 lines / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`; Draft bytes remain unchanged.
- What Changed: Reconciled only the Article README frozen Draft identity record to the verified current Draft SHA and appended this disposition. Did not edit `draft.md`, Research, Evidence, Outline, trace, global status / run-state, Published Content or future assets.
- Evidence Impact: `NONE`; content review result remains passed, `12 / 12` Claims and `12 / 12` Evidence Cards are untouched, and evidence posture remains `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
- Proposed Status: `READY_FOR_RECHECK`

## Reviewer Recheck｜Cycle 0

- Recheck Execution ID: `/root/a24_rechecker`
- Scope: `A24-R0-F01` only
- Decision: `CLOSED`
- Closed Finding: `A24-R0-F01`
- Current Draft path: `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/draft.md`
- Current Draft bytes: `41730`
- Current Draft physical lines: `474`
- Current Draft SHA-256:
  - `Get-FileHash -Algorithm SHA256`: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
  - `certutil -hashfile ... SHA256`: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
  - Direct .NET byte hash: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
- Line count evidence:
  - `(Get-Content -LiteralPath ...).Count`: `474`
  - `[System.IO.File]::ReadLines(...)` count: `474`
- README identity check: current Article README records `41730 bytes / 474 lines / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`.
- Draft edit check: current Draft identity equals the Revision Disposition's pre-revision Draft identity `41730 bytes / 474 physical lines / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`; therefore the revision did not edit Draft bytes.
- Remaining Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Recommended Next Gate: `FINAL_GATE`
- Blocker: `NONE`

## Final Gate｜Fresh Independent Review

- Final Gate Execution ID: `/root/a24_final_reviewer`
- Review Date: `2026-08-29`
- Execution Type: `REAL_SUBAGENT / FRESH INDEPENDENT REVIEWER`
- Gate: `FINAL_GATE`
- Decision: `PASS`
- Recommendation: `PUBLISH`
- Publication Eligibility: `ELIGIBLE_FOR_PUBLISH`
- Explicit non-claim: this Final Gate decision does not claim Published Content exists, does not claim Build PASS, does not claim commit / push / remote verification, and does not claim `END_ARTICLE`.

### Frozen Draft identity

- Result: `PASS`
- Current Draft path: `docs/agent-engineering-course/articles/24-why-harness-cross-cutting-capabilities/draft.md`
- Expected frozen identity: `41730 bytes / 474 lines / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
- Fresh verification:
  - Byte count: `41730`
  - Physical line count: `474`
  - `Get-FileHash -Algorithm SHA256`: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
  - `certutil -hashfile ... SHA256`: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
  - Direct .NET byte hash: `F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`
- Disposition: the frozen Draft identity is current and reconciled. `A24-R0-F01` remains closed.

### Claims, Evidence Cards and proof ceiling

- Result: `PASS`
- Evidence matrix: `12 / 12` rows, `12` unique Claim IDs and `12` unique Evidence IDs.
- Evidence card register: `12 / 12` unique card headers.
- Draft traceability table: `12 / 12` unique `Claim / Evidence` pairs.
- Status ceiling: `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`.
- No new core Claim or Evidence Card is required for Final Gate.
- Proof ceiling is preserved: Article 24 defines a course Harness design model; it does not convert BuildPilot, Lab, runtime, CI, Unity, Jenkins, device, PR, production-write or telemetry claims into observed evidence.

### Finding closure and review cycle

- Result: `PASS`
- Prior Review finding: `A24-R0-F01 MAJOR`.
- Recheck disposition: `A24-R0-F01 CLOSED`.
- Remaining open findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`.
- Current review cycle: `1 / 3`; this is within `MAX_REVIEW_CYCLES = 3`.
- The historical Cycle 0 `FAIL_FOR_FINAL_GATE` is superseded only for the closed stale-SHA finding; its content-review pass evidence remains usable and no unresolved finding remains.

### Teaching spine and engineering transfer

- Result: `PASS`
- TwoEgg spine is intact:
  - Problem space: ownership drift when instruction, tools, resources, approval, trace, budget, evidence and recovery are scattered across agents and workflows.
  - Abstract model: Harness as a shared control plane around agents, tools and workflows, not as prompt decoration and not as a God Object.
  - Concrete design case: BuildPilot-style Unity delivery scenario remains read-only, suggestion-first, evidence-bounded and explicitly not implemented.
- Reader value is clear for senior engineering / Tech Lead positioning: the article teaches how to separate cross-cutting governance responsibilities without over-claiming runtime capability.

### BuildPilot, Lab, runtime and future-article containment

- Result: `PASS`
- BuildPilot boundary: `COURSE PROPOSAL / DESIGN CASE / NOT IMPLEMENTED / NOT RUN / READ-ONLY / SUGGESTION-FIRST`.
- Required Lab: `NONE`.
- Experiment Count: `0`.
- Runtime Observation: `ABSENT`.
- Article 25 / 26 / 27 boundaries are preserved: runtime split, capability model and managed rollout remain future-article work, not Article 24 findings.
- Article 28 remains out of scope.
- Repository containment check found no Article 24 published content file and no Article 25-28 article workspace, content or static production assets.

### Source and relref publication preflight

- Result: `PASS_WITH_LIMITED_SOURCE_DRIFT_NOTE`
- Internal relref preflight: the only Draft `relref` targets existing Published Content `content/ai-empowerment/agent-engineering-22-eval-golden-dataset-regression.md`.
- Adjacent-course continuity checked against Published Article 22 and previously established Article 21 bridge: Article 24 correctly starts from Trace/Eval boundaries and asks who carries shared semantics.
- Live source preflight was performed on current public sources used by Research / Evidence / Draft:
  - MCP server overview / tools / authorization pages support the split between prompts, resources, tools, schema, invocation, confirmation, access control and validation.
  - OpenAI Agents SDK HITL, guardrails and tracing pages support approval, placement, pause/resume, tool guardrail and tracing surfaces.
  - Microsoft Agent Framework Tool Approval and Semantic Kernel Process Framework support tool approval and structured process/workflow responsibilities.
  - GitHub protected-branch and CODEOWNERS docs support owner routing, stale review dismissal and review governance examples.
  - NIST AI RMF Core, OpenTelemetry specification, Azure operations / microservices guidance, ISO 29148, MADR and KCS sources support governance, traceability, requirements, decision records and knowledge workflow parallels.
  - Unity BuildReport opened directly; Unity Asset Database and Addressables Analyze official `docs.unity.cn` pages resolved through search-index snippets when direct page rendering was unavailable in the browsing tool.
- Source disposition: no source contradiction or missing relref creates a Final Gate blocker. The Unity AssetDatabase / Addressables pages remain appropriately `PARTIAL` support for read-only evidence categories; Publisher should re-check exact source rendering if publication is delayed or exact line-level citations become necessary.

### Five-dimensional score

| Dimension | Score | Threshold |
|---|---:|---:|
| Technical Accuracy | 19 / 20 | ≥ 18 |
| Evidence Discipline | 19 / 20 | ≥ 18 |
| Teaching Quality | 19 / 20 | ≥ 17 |
| Engineering Transfer | 19 / 20 | ≥ 17 |
| Readability & Compression | 18 / 20 | contributes to total |

Overall score: `94 / 100`

Threshold result: `PASS_FOR_FINAL_GATE`. Total score is above `88 / 100`; all explicitly thresholded dimensions meet the current course contract.

## Final Gate decision

- Final Gate Decision: `PASS`
- Recommendation: `PUBLISH`
- Publication Eligibility: `ELIGIBLE_FOR_PUBLISH`
- Findings Requiring Edits: `NONE`
- Remaining Open Findings: `0`
- Blocker: `NONE`
- Next Allowed Gate: `PUBLISH`
- Non-claim boundary: Final Gate PASS is not Published Content, not Build Verify, not Git commit / push / remote verification, and not `END_ARTICLE`.
