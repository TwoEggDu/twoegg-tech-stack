# Article 25 Subagent Trace

## Transaction identity

- Start SHA: `752a87de878830da1a7724d87d5f648d45ff3abb`
- Production branch: `main`
- Continuous range: `25 -> 26 -> 27 -> Part V Audit -> STOP`
- Forbidden Article: `28`
- Previous resolution: `Article 24 END_ARTICLE / local-origin-live equality PASS`

## Worker Result Records

### wr-a25-precheck-master

- Execution ID: `/root/a25_precheck_master`
- Bounded brief: reconcile Article 24 completion, clean main and local/origin/live equality; prove Article 25—28 asset absence and no Article 25 completion commit.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: PRECHECK
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: ARTICLE_KICKOFF
  blocker: NONE
  notes:
    - "HEAD, origin/main and live main equal 752a87de878830da1a7724d87d5f648d45ff3abb; tree and index are clean."
    - "Article 24 resolves END_ARTICLE with one exact completion commit; Article 25-28 production assets are zero."
```

### wr-a25-kickoff-master

- Execution ID: `/root/a25_kickoff_master`
- Bounded brief: activate only the Article 25 transaction under the already-authorized bounded continuous run.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: ARTICLE_KICKOFF
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/README.md
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
  gate_completed: true
  next_allowed_gate: WORKSPACE_INIT
  blocker: NONE
  notes:
    - "Article 25 transaction authorization is active through END_ARTICLE_25 or a contract-defined blocker."
    - "Article 28 remains forbidden; no Article 26-28 asset is created."
```

### wr-a25-workspace-init-master

- Execution ID: `/root/a25_workspace_init_master`
- Bounded brief: mechanically instantiate only the Article 25 PLANNED/RESEARCHING workspace from canonical metadata and the authorized Part V prompt.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: WORKSPACE_INIT
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/article-card.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/research.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/evidence.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/subagent-trace.md
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: RESEARCH
  blocker: NONE
  notes:
    - "Only canonical metadata, questions, source plan, boundaries and NOT_STARTED sections are created; no research conclusion, outline, draft or published content is written."
```

### wr-a25-researcher

- Execution ID: `/root/a25_researcher`
- Bounded brief: research the Runtime/Harness/Host/business responsibility boundary; write only `research.md` and `evidence.md`; preserve vendor terminology variance, BuildPilot proposal boundary and Article 26—27 non-scope.
- Dispatch Status: `COMPLETED`
- Master Validation: `PASS`（schema、allowed writes、12/12 mapping、source authority、counter-evidence、terminology variance、BuildPilot/non-scope与future-asset guard通过）

```yaml
worker_result:
  role: RESEARCHER
  article: "25"
  gate: RESEARCH
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/research.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/evidence.md
  gate_completed: true
  next_allowed_gate: EVIDENCE_GATE
  blocker: NONE
  notes:
    - "Created 25-C01 through 25-C12 and one-to-one 25-E01 through 25-E12."
    - "Evidence Gate recommendation PASS with 4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED."
    - "BuildPilot and Article 26-27 non-scope boundaries are preserved; no lab or runtime observation exists."
```

### wr-a25-evidence-gate-master

- Execution ID: `/root/a25_evidence_gate_master`
- Bounded brief: independently validate claim/card coverage, primary-source support, terminology counter-evidence, wording ceilings, BuildPilot boundary and future assets.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: EVIDENCE_GATE
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/subagent-trace.md
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
    - docs/agent-engineering-course/README.md
  gate_completed: true
  next_allowed_gate: OUTLINE
  blocker: NONE
  notes:
    - "12 claims map one-to-one to 12 cards; 4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED."
    - "Independent official-source spot checks confirm execution/host responsibilities and vendor terminology variance; the course split remains explicitly non-standard."
    - "Required Lab NONE, experiment 0, runtime observation absent; Article 26-28 assets remain zero."
```

### wr-a25-author-outline

- Execution ID: `/root/a25_author`
- Bounded brief: write only content-complete `outline.md`; preserve evidence posture, non-standard taxonomy, BuildPilot proposal boundary and Article 26—27 non-scope.
- Dispatch Status: `COMPLETED`
- Master Validation: `PASS`（allowed write、content completeness、12/12 coverage、five-question model、evidence/terminology/BuildPilot/future boundaries通过）

```yaml
worker_result:
  role: AUTHOR
  article: "25"
  gate: OUTLINE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/outline.md
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: AUTHOR_DRAFT
  blocker: NONE
  notes:
    - "Created a 762-line content-complete outline with 12/12 claim coverage."
    - "Preserved exact evidence posture, non-standard course taxonomy, BuildPilot proposal boundary and Article 26-27 non-scope."
```

### wr-a25-author-draft

- Execution ID: `/root/a25_author`
- Bounded brief: write only full `draft.md` from the validated outline and evidence package; add no new fact or runtime claim.
- Dispatch Status: `COMPLETED`
- Master Validation: `PASS`（allowed write、full body、no-frontmatter、12/12 traceability、evidence/terminology/BuildPilot/future boundaries与diff check通过）

```yaml
worker_result:
  role: AUTHOR
  article: "25"
  gate: AUTHOR_DRAFT
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/draft.md
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: REVIEW
  blocker: NONE
  notes:
    - "Created full publication-quality body without frontmatter; 39916 bytes / 561 lines / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4."
    - "Preserved 12/12 and 4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED with all required boundaries."
```

### wr-a25-reviewer-cycle0

- Execution ID: `/root/a25_reviewer`
- Bounded brief: independently review frozen Draft/Evidence and write Findings/decision only; no repair.
- Dispatch Status: `COMPLETED`
- Master Validation: `PASS`（schema、allowed write、Draft identity、12/12 Claims/Cards、score thresholds、source/terminology/BuildPilot/future boundaries与`REVIEW -> FINAL_GATE` mapping通过）

```yaml
worker_result:
  role: REVIEWER
  article: "25"
  gate: REVIEW
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
  gate_completed: true
  next_allowed_gate: FINAL_GATE
  blocker: NONE
  notes:
    - "Fresh Review PASS with score 93/100 and zero open findings."
    - "Draft identity 39916 bytes / 561 lines / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4 independently matched."
    - "12/12 traceability, evidence ceiling, teaching-taxonomy caveat, BuildPilot design-only boundary and Article 26/27 containment all passed."
```

### wr-a25-reviewer-final-gate

- Execution ID: `/root/a25_final_reviewer`
- Bounded brief: independently evaluate the frozen knowledge artifact, zero-finding state, scores, evidence/source/publication preflight and scope boundaries; append Final Gate decision only.
- Dispatch Status: `COMPLETED`
- Master Validation: `PASS`（schema、allowed write、Draft identity、12/12 Claims/Cards、zero findings、evidence/source/terminology/BuildPilot/future boundaries与`FINAL_GATE -> PUBLISH` mapping通过）

```yaml
worker_result:
  role: REVIEWER
  article: "25"
  gate: FINAL_GATE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
  gate_completed: true
  next_allowed_gate: PUBLISH
  blocker: NONE
  notes:
    - "Final Gate PASS with zero open findings and publication eligibility confirmed."
    - "Draft identity independently matched 39916 bytes / 561 lines / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4."
    - "12/12 Claims/Cards, exact evidence posture, Required Lab NONE, terminology caveat, BuildPilot design-only and Article 26/27 non-preemption passed."
```

### wr-a25-publisher

- Execution ID: `/root/a25_publisher`
- Bounded brief: mechanically publish the frozen Draft with validated frontmatter/navigation/series index, run real Hugo build, and write Article-local publication evidence only.
- Dispatch Status: `COMPLETED`
- Master Validation: `PASS`（schema、allowed writes、frontmatter、navigation、series index、exact Draft block identity、fresh Hugo build与future-asset guard均通过）

```yaml
worker_result:
  role: PUBLISHER
  article: "25"
  gate: PUBLISH
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md
  artifacts_modified:
    - content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md
    - content/ai-empowerment/agent-engineering-series-index.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md
  gate_completed: true
  next_allowed_gate: MASTER_STATE_UPDATE
  blocker: NONE
  notes:
    - "Published Article 25 with frozen metadata and exact central Draft block identity."
    - "Article 24 and course index navigation pass; Article 26 has no relref or production asset."
    - "Hugo --gc --minify passed with 1253 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR."
```

### wr-a25-master-state-update

- Execution ID: `/root/a25_master_state_update`
- Bounded brief: validate Final/Publisher/Build results, apply canonical Article 25 publication mapping and prepare global `PUBLISHED` candidate without claiming Git completion.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: MASTER_STATE_UPDATE
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-series-plan.md
    - docs/agent-engineering-course/README.md
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/subagent-trace.md
  gate_completed: true
  next_allowed_gate: PRE_COMMIT_RECONCILIATION
  blocker: NONE
  notes:
    - "Final Gate PASS / 93 / 0 open, Publisher PASS and independent Hugo build PASS are mutually consistent."
    - "Canonical publication mapping and global PUBLISHED candidate are applied; no commit, push, remote or END_ARTICLE result is claimed."
```

### wr-a25-pre-commit-reconciliation

- Execution ID: `/root`
- Bounded brief: perform the final writable checkpoint reconciliation, freeze the exact Article 25 transaction and set Article 26 only as a PRECHECK/NOT_STARTED pointer candidate.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: PRE_COMMIT_RECONCILIATION
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md
    - content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md
    - content/ai-empowerment/agent-engineering-series-index.md
    - docs/agent-engineering-series-plan.md
    - docs/agent-engineering-course/README.md
    - docs/agent-engineering-course/course-run-state.md
    - docs/agent-engineering-course/status.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/article-card.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/research.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/evidence.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/outline.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/draft.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/subagent-trace.md
  gate_completed: true
  next_allowed_gate: GIT_DIFF_VERIFY
  blocker: NONE
  notes:
    - "Exact 15-file transaction is frozen; diff/stage/commit/push/remote verification remain runtime facts."
    - "Article 26 is PRECHECK / NOT_STARTED only and has zero production assets; Article 28 remains forbidden and zero-assets."
```

### wr-a25-part-v-audit-revision-cycle1

- Execution ID: `/root/part_v_a25_revision_cycle1`
- Bounded brief: repair only `PV-AUD-F02` by removing the duplicate draft-internal first-screen navigation from Article25 Draft and Published Content; preserve publisher shell, bottom navigation, teaching content and evidence.
- Master Validation: `PASS`（exact 11-field envelope、three-file allowed-write scope、single navigation-block deletion、Draft/Published identity与`REVISION -> REVIEW_RECHECK` mapping通过）

```yaml
worker_result:
  role: REVISION_WORKER
  article: "25"
  gate: REVISION
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/draft.md
    - content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
  gate_completed: true
  next_allowed_gate: REVIEW_RECHECK
  blocker: NONE
  notes:
    - "Removed only duplicate draft-internal top navigation from Article 25 Draft and Published body; preserved publisher top nav and bottom nav."
    - "Draft identity: 39742 bytes / 559 lines / SHA-256 EB43977112FD2940A5E8D01B728CA6FE0DCCD60D1CCC296C1987E1E964217CD3."
    - "Published contains repaired Draft exactly once; fresh Hugo passed with 1255 Pages and no WARNING/ERROR."
```

### wr-a25-part-v-audit-review-recheck-cycle1

- Execution ID: `/root/part_v_a25_reviewer_cycle1`
- Bounded brief: independently recheck `PV-AUD-F02`, Draft/Published identity, navigation preservation and fresh Hugo; append only to Article25 Review.
- Master Validation: `PASS`（exact 11-field envelope、review-only write、independent identity/navigation/build evidence与`REVIEW_RECHECK -> PART_V_AUDIT` mapping通过）

```yaml
worker_result:
  role: REVIEWER
  article: "25"
  gate: REVIEW_RECHECK
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
  gate_completed: true
  next_allowed_gate: PART_V_AUDIT
  blocker: NONE
  notes:
    - "PV-AUD-F02 is CLOSED_FOR_ARTICLE_25; only the draft-internal duplicate top navigation block was removed from Draft and Published."
    - "Draft appears in Published exactly once; publisher top navigation and bottom navigation are preserved."
    - "Fresh Hugo passed with 1255 Pages / 44 Static / 1 Alias and no ERROR or WARNING line."
```

### wr-a25-part-v-audit-pre-commit-reconciliation-cycle1

- Execution ID: `/root`
- Bounded brief: freeze only the five-file Article25 `PV-AUD-F02` targeted repair after fresh Revision, Review Recheck, identity and Hugo verification; keep the in-progress Part V Audit report/global state outside this fix commit.
- Master Validation: `PASS`

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "25"
  gate: PRE_COMMIT_RECONCILIATION
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/README.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/draft.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/review.md
    - docs/agent-engineering-course/articles/25-agent-runtime-vs-harness/subagent-trace.md
  gate_completed: true
  next_allowed_gate: GIT_DIFF_VERIFY
  blocker: NONE
  notes:
    - "PV-AUD-F02 is CLOSED_FOR_ARTICLE_25 after fresh Revision and independent Review Recheck."
    - "Repaired Draft identity is 39742 bytes / 559 lines / SHA-256 EB43977112FD2940A5E8D01B728CA6FE0DCCD60D1CCC296C1987E1E964217CD3 and appears in Published exactly once."
    - "Fresh Hugo passed with 1255 Pages / 44 Static / 1 Alias and no WARNING/ERROR; Article28 remains forbidden and asset-free."
```
