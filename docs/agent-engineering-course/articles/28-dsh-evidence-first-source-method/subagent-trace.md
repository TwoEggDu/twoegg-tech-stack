# Article 28 Subagent Trace

## wr-a28-source-map-dispatch

- Execution ID: `/root/part_vi_a28_source_investigator`
- Allowed writes: `source-map.md`
- Gate: `SOURCE_MAP -> EXPERIMENT_DESIGN`
- Status: `PASS`

### Source Investigator result

- Master Validation: `PASS`; full file read, hash and fixture identity independently rechecked.

```yaml
worker_result:
  role: SOURCE_INVESTIGATOR
  article: "28"
  gate: SOURCE_MAP
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/source-map.md
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: EXPERIMENT_DESIGN
  blocker: NONE
  notes:
    - "Pinned DSH HEAD and tag both equal cd5ef8148158c3a752a658978873241fdf8e2bbc; fixture remained clean."
    - "Closed install/build/test and source CLI paths through profile boot and Loader settlement code, then stopped before the Article 29 runner-to-Agent path."
    - "Recorded source/generated boundaries, counter-evidence, evidence ceilings, and Article 29-37 route anchors without runtime upgrades."
    - "Source map verification: 197 lines, SHA-256 308DE2547007F9E63212DA5FD151375A7894FF4396C3BC56F97CE5A92846B9F2, zero trailing whitespace, terminal newline present."
```

## wr-a28-lab-dispatch

- Execution ID: `/root/part_vi_a28_lab_engineer`
- Allowed writes: `baseline-manifest.md`, `experiments/baseline-probes.md`
- Gate: `EXPERIMENT_DESIGN -> EXPERIMENT_EXECUTE -> RAW_OBSERVATION`
- Status: `PASS`

### Lab Engineer result

- Master Validation: `PASS`; both files read in full, diff check passed, hashes captured, and fixture HEAD/clean rechecked.

```yaml
worker_result:
  role: LAB_ENGINEER
  article: "28"
  gate: RAW_OBSERVATION
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/baseline-manifest.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/experiments/baseline-probes.md
  gate_completed: true
  next_allowed_gate: EVIDENCE_MERGE
  blocker: NONE
  notes:
    - "Identity/install/build probes completed; fixture remained pinned and clean."
    - "Build passed only after unchanged narrow host-access retry; sandbox failure is preserved."
    - "Full unit suite remains FAIL: 32 files and 129 tests failed."
    - "Isolated notices probe passed 27/27 but did not upgrade the full-suite result."
    - "Keyless read-only run exited 1 at MISSING_CREDENTIAL with no model output or credential value exposed."
```

## wr-a28-evidence-merge-dispatch

- Execution ID: `/root/part_vi_a28_researcher`
- Allowed writes: `research.md`, `evidence.md`
- Gate: `EVIDENCE_MERGE -> EVIDENCE_GATE`
- Status: `PASS`

### Evidence Merge result

- Master Validation: `PASS`; 16 Claims and 12 Cards reconciled against Source Map and raw probes, with no core blocked Claim.

```yaml
worker_result:
  role: RESEARCHER
  article: "28"
  gate: EVIDENCE_MERGE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/research.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/evidence.md
  gate_completed: true
  next_allowed_gate: EVIDENCE_GATE
  blocker: NONE
  notes:
    - "16 Claims: 6 CONFIRMED / 0 PARTIAL / 10 PROPOSAL / 0 BLOCKED."
    - "12 Evidence Cards: 4 CONFIRMED / 0 PARTIAL / 8 PROPOSAL / 0 BLOCKED."
    - "Full unit suite remains FAIL at 32 failed files / 129 failed tests; isolated 27/27 is classification only."
    - "Build remains host-access caveated; config dump is not activation; keyless run stops at MISSING_CREDENTIAL."
    - "Article 29-37 owning call paths and traces remain PENDING/DEFER."
```

## wr-a28-evidence-gate

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: EVIDENCE_GATE
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: OUTLINE
  blocker: NONE
  notes:
    - "Pinned identity, source boundaries, six baseline source records and direct raw probes were independently validated."
    - "No runtime overclaim: completed Agent Run remains zero and the full suite remains failed."
    - "All Article 28 core Claims are evidence-bounded and none is BLOCKED."
```

## wr-a28-author-outline-dispatch

- Execution ID: `/root/part_vi_a28_author`
- Allowed writes: `outline.md`
- Gate: `OUTLINE -> AUTHOR_DRAFT`
- Status: `PASS`

### Author Outline result

- Master Validation: `PASS`; 619 lines, 16/16 Claims, 12/12 Cards and all four runtime ceilings were directly checked.

```yaml
worker_result:
  role: AUTHOR
  article: "28"
  gate: OUTLINE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/outline.md
  gate_completed: true
  next_allowed_gate: AUTHOR_DRAFT
  blocker: NONE
  notes:
    - "Problem space -> abstract model -> baseline -> engineering boundary -> Article 29-37 routing."
    - "Coverage is 16/16 Claims and 12/12 Evidence Cards."
    - "Full-suite failure, host-access caveat, config-dump ceiling and credential boundary are retained."
    - "SHA-256 58E53E6774B93D59EEFFAA1174568C14FE2F77454A03829776B3085B5623CFB2."
```

## wr-a28-author-draft-dispatch

- Execution ID: `/root/part_vi_a28_author`
- Allowed writes: `draft.md`
- Gate: `AUTHOR_DRAFT -> REVIEW`
- Status: `PASS`

### Author Draft result

- Master Validation: `PASS`; 550 lines / 37021 bytes / no frontmatter, all Claims/Cards and evidence ceilings directly checked.

```yaml
worker_result:
  role: AUTHOR
  article: "28"
  gate: AUTHOR_DRAFT
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/draft.md
  gate_completed: true
  next_allowed_gate: REVIEW
  blocker: NONE
  notes:
    - "Complete Evidence-first draft follows problem space -> model -> pinned baseline -> routes."
    - "Coverage is 16/16 Claims and 12/12 Evidence Cards."
    - "No Hugo frontmatter; SHA-256 BA7FD5027B0CBFCA36BFCD4A6F7A38EE163A20B9CF9E58A8E3BDBCF47912604C."
    - "Five-layer identity stays question-only and Part VII remains not started."
```

## wr-a28-review-dispatch

- Execution ID: `/root/part_vi_a28_reviewer`
- Allowed writes: `review.md`
- Gate: `REVIEW -> REVISION or FINAL_GATE`
- Status: `PASS_WITH_FINDINGS`

### Review result

- Master Validation: `PASS_WITH_FINDINGS`; finding is evidence-bounded and repairable without new Lab work.

```yaml
worker_result:
  role: REVIEWER
  article: "28"
  gate: REVIEW
  execution_type: REAL_SUBAGENT
  status: PASS_WITH_FINDINGS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/review.md
  gate_completed: true
  next_allowed_gate: REVISION
  blocker: NONE
  notes:
    - "Score 94/100; one MAJOR, zero MINOR, zero BLOCKER."
    - "A28-RV-001: durable baseline stores structured observations and sanitized excerpts, not the complete raw terminal stream."
    - "All other source, runtime, route, safety and stop boundaries passed."
```

## wr-a28-revision-dispatch

- Execution ID: `/root/part_vi_a28_revision_worker`
- Allowed writes: `research.md`, `evidence.md`, `baseline-manifest.md`, `experiments/baseline-probes.md`, `draft.md`, `review.md`
- Gate: `REVISION -> REVIEW_RECHECK`
- Status: `PASS`

### Revision result

- Master Validation: `PASS`; only A28-RV-001 wording scope changed and all measured outcomes remained identical.

```yaml
worker_result:
  role: REVISION_WORKER
  article: "28"
  gate: REVISION
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/research.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/evidence.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/baseline-manifest.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/experiments/baseline-probes.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/draft.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/review.md
  gate_completed: true
  next_allowed_gate: REVIEW_RECHECK
  blocker: NONE
  notes:
    - "A28-RV-001 is READY_FOR_RECHECK, not self-closed."
    - "Ideal raw Trace standard and actual structured/sanitized durable record are now distinct."
    - "Revised draft: 556 lines / 38066 bytes / SHA-256 78C583223CE33314689A72D742978C697117914776EFEBCD69FFD9FF744331DE."
```

## wr-a28-review-recheck-dispatch

- Execution ID: `/root/part_vi_a28_reviewer`
- Allowed writes: `review.md`
- Gate: `REVIEW_RECHECK -> FINAL_GATE`
- Status: `PASS`

### Review Recheck result

- Master Validation: `PASS`; A28-RV-001 closed from direct artifact evidence and all regression boundaries retained.

```yaml
worker_result:
  role: REVIEWER
  article: "28"
  gate: REVIEW_RECHECK
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/review.md
  gate_completed: true
  next_allowed_gate: FINAL_GATE
  blocker: NONE
  notes:
    - "A28-RV-001 CLOSED; zero open findings; revised score 98/100."
    - "Draft identity: 38066 bytes / SHA-256 78C583223CE33314689A72D742978C697117914776EFEBCD69FFD9FF744331DE."
    - "All direct outcomes and Part VI boundaries remained unchanged."
```

## wr-a28-final-gate-dispatch

- Execution ID: `/root/part_vi_a28_final_reviewer`
- Allowed writes: `review.md`
- Gate: `FINAL_GATE -> PUBLISH`
- Status: `PASS`

### Final Gate result

- Master Validation: `PASS`; exact Draft identity, zero findings, evidence boundaries and publication preflight validated.

```yaml
worker_result:
  role: REVIEWER
  article: "28"
  gate: FINAL_GATE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/review.md
  gate_completed: true
  next_allowed_gate: PUBLISH
  blocker: NONE
  notes:
    - "Draft SHA-256 78C583223CE33314689A72D742978C697117914776EFEBCD69FFD9FF744331DE / 38066 bytes."
    - "16/16 Claims, 12/12 Cards, zero open findings, score 98/100."
    - "Publication preflight PASS; PUBLISH and Hugo remain downstream."
```

## wr-a28-publisher-dispatch

- Execution ID: `/root/part_vi_a28_publisher`
- Allowed writes: published Article 28, published Article 27 navigation, public course index, Article 28 README
- Gate: `PUBLISH -> MASTER_STATE_UPDATE`
- Status: `PASS`

### Publisher result

- Master Validation: `PASS`; exact H1-to-EOF identity, frontmatter, navigation, index and fresh Hugo independently verified.

```yaml
worker_result:
  role: PUBLISHER
  article: "28"
  gate: PUBLISH
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - content/ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md
  artifacts_modified:
    - content/ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md
    - content/ai-empowerment/agent-engineering-series-index.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/README.md
  gate_completed: true
  next_allowed_gate: MASTER_STATE_UPDATE
  blocker: NONE
  notes:
    - "Published H1-to-EOF exact identity: SHA-256 60BA15EA373BAD5D649F2C69ACF924F60DA202E8B0DB7028680DB84F39F3053B / 37772 bytes / 553 lines."
    - "Article 27 links to Article 28 at top and bottom; index publishes 28 while 29 stays planned/unlinked."
    - "Fresh Master Hugo: 1256 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR."
```

## wr-a28-master-state-update

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: MASTER_STATE_UPDATE
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-series-plan.md
    - docs/agent-engineering-course/README.md
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/README.md
  gate_completed: true
  next_allowed_gate: PRE_COMMIT_RECONCILIATION
  blocker: NONE
  notes:
    - "Global surfaces mark only a PUBLISHED candidate; completion commit and END_ARTICLE remain pending."
    - "Article29 remains NOT_STARTED and requires Article28 END_ARTICLE; Article38-44 remain forbidden."
```

## wr-a28-pre-commit-reconciliation

- Master Validation: `INVALIDATED / cached diff check found article-card.md new blank line at EOF / no commit performed`.

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: PRE_COMMIT_RECONCILIATION
  execution_type: MASTER_DETERMINISTIC
  status: FAIL
  artifacts_created:
    - content/ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/README.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/article-card.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/research.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/evidence.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/baseline-manifest.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/source-map.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/experiments/baseline-probes.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/outline.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/draft.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/review.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/subagent-trace.md
  artifacts_modified:
    - content/ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md
    - content/ai-empowerment/agent-engineering-series-index.md
    - docs/agent-engineering-series-plan.md
    - docs/agent-engineering-course/README.md
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
  gate_completed: false
  next_allowed_gate: PRE_COMMIT_RECONCILIATION
  blocker: NONE
  notes:
    - "Exact 18-file transaction frozen; HEAD/origin/live main are equal at 03c1649b7915d39dda91f67a8cc8b0257306bb4d."
    - "Fresh Hugo passed at 1256 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR."
    - "DSH fixture remains pinned and clean; Article29-44 production asset count remains zero."
    - "INVALIDATED: cached diff check found one new blank line at article-card.md EOF; no commit was performed."
```

## wr-a28-pre-commit-reconciliation-retry1

- Status: `PASS / article-card.md terminal blank normalized only`.

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: PRE_COMMIT_RECONCILIATION
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/article-card.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/README.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/subagent-trace.md
    - docs/agent-engineering-course/course-run-state.md
  gate_completed: true
  next_allowed_gate: GIT_DIFF_VERIFY
  blocker: NONE
  notes:
    - "Only the terminal blank in article-card.md and truthful checkpoint records changed after the invalidated first cut."
    - "Exact staged 18-file transaction now passes git diff --cached --check."
    - "No semantic Draft, Published Content, evidence, navigation or baseline outcome changed."
```

## wr-a28-research-dispatch

- Execution ID: `/root/part_vi_a28_researcher`
- Allowed writes: `research.md`, `evidence.md`
- Gate: `RESEARCH -> SOURCE_MAP`
- Status: `RUNNING`

### Research result

- Master Validation: `PASS` after a zero-write closed-schema retry.

```yaml
worker_result:
  role: RESEARCHER
  article: "28"
  gate: RESEARCH
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/research.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/evidence.md
  gate_completed: true
  next_allowed_gate: SOURCE_MAP
  blocker: NONE
  notes:
    - "16 Claims / 12 Evidence Cards / 4 CONFIRMED / 2 PARTIAL / 10 PROPOSAL / 0 BLOCKED"
    - "Pinned official docs/source only; current-page verification was not executed and does not override pinned conclusions."
    - "Baseline runtime probes remain PENDING."
    - "Fixture HEAD is pinned and clean; git diff --check passed."
```

## wr-a28-precheck

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: PRECHECK
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: ARTICLE_KICKOFF
  blocker: NONE
  notes:
    - "Article24-27 and Part V Audit resolve complete from Git history and live refs; HEAD/origin/live are equal at 03c1649b7915d39dda91f67a8cc8b0257306bb4d."
    - "Article28-38 production assets were zero before kickoff; course tree/index were clean."
    - "Official DSH tag resolves to pinned full commit cd5ef8148158c3a752a658978873241fdf8e2bbc."
```

## wr-a28-article-kickoff

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: ARTICLE_KICKOFF
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: WORKSPACE_INIT
  blocker: NONE
  notes:
    - "Human Part VI authorization supersedes the old Article28 forbidden marker only after fresh reconciliation."
    - "Article28 owns the active transaction; Article29-44 remain not started."
```

## wr-a28-workspace-init

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "28"
  gate: WORKSPACE_INIT
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/README.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/article-card.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/research.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/evidence.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/baseline-manifest.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/source-map.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/experiments/baseline-probes.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/outline.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/draft.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/review.md
    - docs/agent-engineering-course/articles/28-dsh-evidence-first-source-method/subagent-trace.md
  artifacts_modified:
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
  gate_completed: true
  next_allowed_gate: RESEARCH
  blocker: NONE
  notes:
    - "Workspace contains canonical metadata and empty role-owned skeletons only."
    - "No Research answer, Evidence conclusion, Source Map, Outline, Draft, Review or Published Content was prewritten."
```
