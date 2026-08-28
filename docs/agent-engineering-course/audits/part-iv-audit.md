# Agent Engineering Part IV Audit

- Audit Scope: `PART_IV / Articles 18—22`
- Gate: `PART_IV_AUDIT`
- Audit Cycle: `1 / FRESH PART AUDIT`
- Auditor: `/root/part_iv_auditor_cycle1`
- Execution Type: `REAL_SUBAGENT / FRESH PART CONTEXT`
- Audit Date: `2026-08-28 / Asia/Shanghai`
- Decision: `PASS`
- Open Findings: `0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`
- Gate Effect: `READY_FOR_AUDIT_CHECKPOINT_GIT_DIFF_VERIFY`
- Stop Boundary: Article 23 remains `Advanced / Optional / SKIP / PLANNED / NOT_STARTED / ZERO ASSETS`; Article 24 remains `PLANNED / NOT_STARTED / FORBIDDEN / ZERO ASSETS`. This Auditor does not start either Article.

## Scope, method and independence

This Cycle 1 audit read the repository instructions; the TwoEgg article method and all required references; Course Factory, production workflow and Subagent Contract; canonical series plan, course run state, status, glossary and course/lab indexes; the Part III audit precedent; and the complete Article 18—22 workspaces, evidence, reviews, traces, READMEs and Published Content.

The audit independently checked:

1. the Part IV learning progression and cross-Article concept/term ownership;
2. Evidence Contract and exact closed-schema Worker Result records;
3. Permission / Authorization / Approval / HITL / Sandbox boundaries;
4. Budget boundaries against authority, Trace and Eval;
5. Trace / Replay / Failure Taxonomy boundaries and failure retention;
6. Eval / Golden Dataset / Regression executability and proof ceiling;
7. Lab 06 frozen design, source, tests, raw observations, process records, hashes and repeatability;
8. BuildPilot consistency;
9. Draft-to-Published containment, public navigation, canonical/index state and a fresh Hugo build;
10. unique Git completion commits, ancestor/scope/diff integrity and current local/remote refs;
11. Article 23/24 asset absence and quality-degradation/template-copy signals.

This is an evidence-led audit of the durable repository and current Git/remote state. It did not modify any Article, Lab, Published Content, global state, run state, status, series plan or Git metadata. It did not rerun Lab 06 because those commands write build/spec-temporary output and could disturb the frozen observation transaction; instead it independently parsed the retained implementation and raw results, recomputed the recorded hashes and compared repeatability bytes. The only durable write is this report.

## Git, remote and completion evidence

Before this report was created, the repository was on `main` with a clean tree and index. The following refs were independently resolved:

| Ref | SHA | Result |
|---|---|---|
| local `HEAD` | `99bff931b02356358edd1357c2abd1c44621e720` | MATCH |
| local `origin/main` | `99bff931b02356358edd1357c2abd1c44621e720` | MATCH |
| live `refs/heads/main` from `git ls-remote origin` | `99bff931b02356358edd1357c2abd1c44621e720` | MATCH |

Live remote access succeeded; there is no remote-verification limitation for this cycle.

Each required completion subject occurs exactly once, each commit is an ancestor of current `HEAD`, and `git diff <sha>^ <sha> --check` passed:

| Article | Completion commit | Subject | Files in commit | Resolver result |
|---|---|---|---:|---|
| 18 | `a0d8d1b2fa5380f9a4150f72b962ac15fe11a96b` | `Publish Agent Engineering Article 18` | 15 | `END_ARTICLE` |
| 19 | `73a0f628e5580226f4c65890f81372d7ededd43d` | `Publish Agent Engineering Article 19` | 15 | `END_ARTICLE` |
| 20 | `59f8c44df5d10894335bf5cd97d5b27552a830fe` | `Publish Agent Engineering Article 20` | 15 | `END_ARTICLE` |
| 21 | `470c362567d71aa4b7e5d951406b9af92b5b1adf` | `Publish Agent Engineering Article 21` | 15 | `END_ARTICLE` |
| 22 | `99bff931b02356358edd1357c2abd1c44621e720` | `Publish Agent Engineering Article 22` | 67 | `END_ARTICLE` |

The five commits form the expected forward sequence and contain the bounded Article transaction assets: current workspace, Published Content, adjacent navigation/index projection and owned state evidence; Article 22 additionally contains Lab 06 and its index projection. None contains Article 23 or Article 24 production assets.

Article 22's durable README/status wording remains the intentionally persisted pre-commit candidate (`PUBLISHED CANDIDATE / PRE_COMMIT_RECONCILIATION PASS`) because a commit cannot truthfully prewrite its own SHA or later push result. Current Git history plus equal live refs now satisfies the factory resolver and yields `END_ARTICLE`. This persisted-checkpoint/runtime-derived distinction is expected contract behavior, not a stale-state Finding.

## Article progression and evidence audit

| Article | Owned teaching responsibility | Evidence posture | Review / publication evidence | Audit result |
|---|---|---|---|---|
| 18 | Claim、Evidence、Observation、Inference、Proposal、Unknown；source/version/scope/limitations/falsifier；acceptance and lifecycle | `10 / 10 Claims`; `8 Cards`; `2 CONFIRMED / 2 PARTIAL / 6 PROPOSAL / 0 BLOCKED` | Final `95 / 100 / 0 OPEN`; frozen Draft hash `f6cd06c0cc98d310a5617cadc2e2fedfe1f1657cc30790ef3a63d8bfd2924646` | PASS |
| 19 | Permission、Authorization、Approval、HITL、Sandbox；request binding、expiry/revocation、resume revalidation、TOCTOU | `10 / 10 Claims`; `12 Cards`; `3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED` | Final `93 / 100 / 0 OPEN`; all review findings closed | PASS |
| 20 | Token、Step、Cost、Latency as separate dimensions；reservation/replace accounting；deadline/timeout/clock-domain uncertainty | `9 / 9 Claims`; `11 Cards`; `1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED` | Final `91 / 100 / 0 OPEN`; all review findings closed | PASS |
| 21 | Logs/Metrics/Traces/Audit separation；causal event envelope；Replay modes/manifest；occurrence/observation/recovery；seven-layer taxonomy | `12 / 12 Claims`; `12 Cards`; `1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED` | Final `91 / 100 / 0 OPEN`; all review findings closed | PASS |
| 22 | Demo/Test/Benchmark/Eval/Regression separation；versioned Eval Contract；Golden lifecycle；Case/oracle/scorer/metric/gate；comparability-first verdict | `12 / 12 Claims`; `12 Cards`; `3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED` | Final `95 / 100 / 0 OPEN`; Lab 06 `VERIFIED / EVIDENCE_MERGED / FIXTURE-SCOPED` | PASS |

The teaching sequence is coherent and cumulative:

`accepted Claim -> authorized action -> admitted resource consumption -> reconstructable/classifiable execution -> repeatable quality verdict`.

Each Article consumes the preceding contract without treating it as a substitute for its own responsibility. The series consistently preserves `UNKNOWN`, `REQUIRED`, `NOT_RUN`, `BLOCKED` and `INCOMPARABLE` rather than filling missing facts with plausible-looking values.

## Mandatory cross-Article criteria

| Criterion | Decision | Evidence |
|---|---|---|
| Concept and terminology consistency | PASS | Article 18 owns Claim acceptance; 19 owns action authority; 20 owns resource admission/accounting; 21 owns execution reconstruction/classification; 22 owns fixed-workload quality judgment. The same terms are used as handoff seams, not aliases. |
| Evidence Contract | PASS | Fact/observation/inference/proposal/unknown distinctions remain explicit. All five evidence sets have complete Claim coverage, zero `BLOCKED`, typed posture and `Does Not Prove`/limitation ceilings. |
| Worker Result closed schema | PASS | Article 18/19/20/21/22 traces contain `12 / 15 / 13 / 15 / 15` fenced Worker Result envelopes respectively. Mechanical parsing found zero invalid envelopes; every record has exactly the eleven root fields `role, article, gate, execution_type, status, artifacts_created, artifacts_modified, gate_completed, next_allowed_gate, blocker, notes`. |
| Permission / Approval / Sandbox | PASS | Article 19 separates permission ceiling, request authorization, explicit approval, HITL pause/resume and per-mechanism sandbox limits. Approval does not imply execution; decision idempotency does not imply action exactly-once; seccomp is not promoted to a complete sandbox. |
| Budget vs authority | PASS | Article 20 orders legal transition/action authority before reservation. Budget PASS cannot authorize a Tool, Retry or side effect; budget-change approval cannot override hard deny. |
| Budget vs Trace/Eval | PASS | Budget records expose `trace_ref` only as a seam and do not claim cross-step Trace. Exhaustion/degradation is a resource-policy outcome, not evidence that quality is preserved; quality judgment is deferred to Article 22. |
| Token / Step / Cost / Latency | PASS | Units, estimate/actual, enforcement points and unknowns remain separate. Accounting uses reservation-to-qualified-actual replacement, not additive double counting. Deadline is an absolute point; timeout is a child duration; incompatible clock domains fail closed. |
| Trace / Replay / Failure Taxonomy | PASS | Article 21 distinguishes record types and distinguishes reconstruction, effect-suppressed simulation and re-execution. It does not promise deterministic replay or exactly-once. Failure occurrence, observation and recovery are separate; primary status supports `SINGLE / CO_PRIMARY / BOUNDARY / UNKNOWN`. |
| Trace vs Eval | PASS | Trace emits candidate slices plus lineage; it does not declare a Golden sample, metric or regression verdict. Article 22 owns acceptance, scoring, comparison and release judgment. |
| Eval / Golden / Regression executability | PASS | Article 22 defines versioned identity, case schema, oracle/scorer versions, metrics, critical-case gates and comparability checks. The required Lab exercises baseline, known regression, missing input and scorer-version mismatch through a public CLI and deterministic JSON output. |
| Golden governance | PASS | Candidate-to-Golden promotion is an explicit reviewed lifecycle; leakage/wear, provenance, versioning and retirement are visible. A passed example or repaired incident is not automatically Golden. |
| BuildPilot consistency | PASS | Articles 18—22 consistently state `DESIGN / NOT IMPLEMENTED / NOT RUN`. Lab 06 is a course-owned deterministic fixture and is never relabeled as BuildPilot Runtime, production traffic, model output or measured product benefit. |
| Missing dependency / forward reference | PASS | All required predecessors are published. Future Articles appear only as prose/control boundaries. Article 23/24 have no live content link and no production asset. |
| Learning progression / job competency | PASS | The Part teaches evidence review, authority/risk review, budget accounting, causal/failure analysis and eval/release judgment using inspectable artifacts and stop conditions rather than API cataloguing. |

## Lab 06 independent evidence audit

Lab 06 contains `51` durable files across frozen design, two BCL-only `net10.0` projects, fixtures, source/tests, process records, raw observations and evidence merge. There are no package or project references, Provider/model/network/credential dependencies or hidden production inputs.

### Design, RED/GREEN and test integrity

- Lab Design and Expected Observable were frozen before runtime observations; the later Observation and Researcher Interpretation/Evidence Merge sections do not rewrite the hypothesis or acceptance criteria.
- RED retains a successful restore/build followed by public-Spec failure: `0 / 5`, Spec exit `1`; the incomplete Runtime shell exits `64`.
- GREEN retains the same public Spec path and records `5 / 5`, exit `0`.
- The tests invoke the public CLI and validate normalized output artifacts; they do not call private implementation helpers or read a hidden expected-result directory.
- The formal verifier records `2 / 2` checks passed.

### Raw result matrix

| Scenario | Independently parsed raw result | Required disposition | Result |
|---|---|---|---|
| baseline | `8 / 8`; aggregate `1.0`; critical `2 / 2 = 1.0`; overall `PASS` | comparable success | MATCH |
| known regression | `7 / 8`; aggregate `0.875` threshold-pass; critical `1 / 2 = 0.5`; `C01=REGRESSION`; seven `UNCHANGED`; overall `FAIL`; native exit `2` | critical gate must dominate aggregate | MATCH |
| missing `N06` | manifest `comparable=false`; one missing/one unknown; `N06=UNKNOWN`; overall `FAIL`; native exit `2` | missing evidence must not become zero/success | MATCH |
| scorer v2 | `INCOMPARABLE`; ordinary aggregate/delta absent; overall `FAIL`; native exit `3` | version mismatch must stop ordinary comparison | MATCH |

All `10 / 10` recorded SHA-256 values were independently recomputed and match current bytes. Run A/B baseline files are byte-identical; Run A/B known-regression files are byte-identical. The observed aggregate pass plus critical/overall fail is retained rather than normalized away. The failed ad-hoc verification attempt and outer-shell/native-exit distinction remain disclosed.

The evidence ceiling is correct: Lab 06 proves only deterministic behavior for its fixed fixtures, scorer, schema, thresholds and environment. It does not prove real Agent/model output quality, Trace curation quality, production traffic, cross-Provider/model/environment generalization, statistical significance, security/compliance or business outcome.

## Published Content, navigation and Hugo

### Draft/Published integrity

| Article | Draft bytes | Published bytes | Audit comparison | Result |
|---|---:|---:|---|---|
| 18 | `31943` | `32809` | Published text contains the exact frozen Draft; additional bytes are publication wrapper/navigation | PASS |
| 19 | `44803` | `45677` | Published text contains the exact frozen Draft; additional bytes are publication wrapper/navigation | PASS |
| 20 | `44197` | `45095` | Published text contains the exact frozen Draft; additional bytes are publication wrapper/navigation | PASS |
| 21 | `51399` | `52893` | Published text contains the exact frozen Draft; additional bytes are publication wrapper/navigation | PASS |
| 22 | `29637` | `29637` | exact byte/hash identity: `30405404bc5f7e51a9a981ae4b3f07846af4711bc952e5773464fbea9c3efc2c` | PASS |

No Published Content silently drops or rewrites the reviewed Draft body.

### Source and rendered navigation

- Public source adjacency is continuous: `17 -> 18 -> 19 -> 20 -> 21 -> 22`.
- Article 22 links back to Article 21 and to the course index, and has no next link to optional/unpublished Article 23.
- The public series index lists Articles 18—22 as published, Article 23 as unlinked `高级 · 可选`, and Article 24 as unlinked `计划中` in Part V.
- Fresh rendering produced all five Part IV routes. Rendered Article 22 contains previous/course navigation and no next token; the other Part IV pages preserve their applicable adjacent links.

The first sandboxed launcher attempt could not start the installed executable outside the workspace (`Access denied`). The authorized rerun succeeded; therefore this is not a build limitation. Fresh result:

- command: `hugo --gc --minify`
- Hugo: `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`
- exit: `0`
- pages: `1251`
- paginator pages: `0`
- non-page files: `0`
- static files: `44`
- processed images: `0`
- aliases: `1`
- cleaned: `0`
- warnings/errors: none emitted
- total: `8777 ms`

## Future-Article asset guard

Counts were taken before this report write from the production asset locations:

| Future Article | Workspace directories | Published Content files | Image assets | State |
|---|---:|---:|---:|---|
| 23 | `0` | `0` | `0` | `Advanced / Optional / SKIP / PLANNED / NOT_STARTED` |
| 24 | `0` | `0` | `0` | `PLANNED / NOT_STARTED / FORBIDDEN` |

Canonical plan, run state, public index and Article 22 all agree that Article 23 may be skipped and that completing Article 22 does not authorize Article 24 production. There is no prewritten Research, Card, Draft, Evidence, Published Content, image or link for either Article.

## Quality degradation, template-copy and evidence density

| Signal | Observation | Decision |
|---|---|---|
| Final scores | `95, 93, 91, 91, 95`; all meet dimension and total thresholds; earlier findings are closed | no end-of-Part score collapse |
| Claim/Card density | `10/8, 10/12, 9/11, 12/12, 12/12`; every Claim traceable and zero `BLOCKED` | no shrinking evidence coverage |
| Draft size | `386, 580, 475, 620, 433` lines | variation follows topic complexity; no monotonic padding/shrink pattern |
| Teaching shape | Evidence state machine; authority control chain; multi-dimensional ledger; event/replay/taxonomy; eval lifecycle and executable Lab | article-specific mechanisms, not copied heading-only templates |
| Concrete evidence | Articles 18—21 keep design claims bounded; Article 22 adds a real frozen fixture with RED/GREEN, failure cases, repeatability and hashes | evidence density rises where the Lab contract requires it |
| Repetition | Repeated BuildPilot/unknown/exactly-once boundaries serve cross-Article safety invariants and are applied to distinct mechanisms | purposeful consistency, not actionable filler |

No systematic quality degradation, rubric inflation, template-copy substitution, expected-only Lab pattern or fabricated evidence warrants a Finding.

## Findings

| ID | Severity | Status | Affected paths | Repair scope |
|---|---|---|---|---|
| `NONE` | N/A | No actionable findings | N/A | N/A |

Open severity totals: `BLOCKER 0 / MAJOR 0 / MINOR 0 / EDITORIAL 0`.

## Gate decision and stop line

**Part IV Gate: `PASS`.**

All mandatory Articles 18—22 resolve to `END_ARTICLE`; concept/evidence/authority/budget/trace/eval boundaries are coherent; Lab 06 raw evidence is reproducible at the retained-artifact level and correctly fixture-scoped; publication/navigation/Hugo checks pass; BuildPilot remains design-only; and Article 23/24 production assets remain zero.

The next allowed control-flow candidate is `PRECHECK` only after Master validates this result and completes the separate audit checkpoint transaction (`git diff` scope verification, `Audit Agent Engineering Part IV`, single push and live-remote verification). Per the bounded authorization, the run stops after the Part IV Audit checkpoint. This Auditor does not edit global state, stage, commit, push, start Article 23 or start Article 24.

## Worker Result Record

- Record ID: `wr-part-iv-audit-cycle1-20260828`
- Execution ID: `/root/part_iv_auditor_cycle1`
- Bounded brief: fresh Part IV audit; Articles 18—22, Lab 06, Git/remote, publication/navigation/build, future-asset and degradation checks; only this report may be created; no repair, global-state mutation, commit or push.
- Master Validation: `PASS`; exact eleven-field envelope, report readback, five `END_ARTICLE` resolutions, Lab 06 retained evidence, Hugo result, audit-only artifact scope and Article 23/24 zero-asset boundary were independently verified.
- Raw envelope:

```yaml
worker_result:
  role: PART_AUDITOR
  article: "PART_IV"
  gate: PART_IV_AUDIT
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/audits/part-iv-audit.md
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: PRECHECK
  blocker: NONE
  notes:
    - "Cycle 1 PASS: 0 open BLOCKER / 0 open MAJOR / 0 open MINOR / 0 open EDITORIAL findings."
    - "Articles 18—22 resolve to END_ARTICLE from unique completion commits; HEAD, origin/main and live main equal 99bff931b02356358edd1357c2abd1c44621e720."
    - "Lab 06 raw results, 10/10 recorded hashes, Run A/B byte equality, 2/2 formal verifier, publication/navigation and fresh Hugo 1251-page build checks passed; Article 23/24 production asset counts remain zero."
```
