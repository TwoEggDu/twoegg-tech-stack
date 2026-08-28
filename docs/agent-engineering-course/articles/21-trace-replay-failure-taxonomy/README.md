# Article 21｜Trace、Replay 与 Failure Taxonomy：错误究竟发生在哪一层

- Canonical ID: `21`
- Workspace: `21-trace-replay-failure-taxonomy`
- Part: `IV｜Reliable Agent Engineering`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `PRINCIPLE`
- Lifecycle Status: `PUBLISHED CANDIDATE`
- Evidence Status: `PASS / 12 of 12 / 1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED / 12 Cards`
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION RETRY 2 COMPLETE`
- Active Worker: `NONE`
- Blocker: `NONE`
- Lifecycle Candidate: `PUBLISHED`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION PASS`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article 21`
- Next Transaction Candidate: `Article 22 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 21 END_ARTICLE`

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == 59f8c44df5d10894335bf5cd97d5b27552a830fe`.
- Previous boundary: Article 20 has one exact completion commit，is contained by local / origin / live `main`，and resolves `END_ARTICLE`.
- Article 21 assets before kickoff: `0`；Article 23 / 24 assets: `0`.
- Human authorization covers this Article through `END_ARTICLE_21` or a contract-defined real blocker. It does not authorize Article 23 or 24.

## Current boundary

Research，Evidence Gate，Outline，Draft，Review/Revision/Recheck，Final Gate，mechanical Publish，independent Build and Pre-Commit Reconciliation are complete：`PASS / 91 / F01—F04 CLOSED / 0 OPEN`；Draft SHA-256=`4ceb7e56bf8aa153518d66444de8b59bc87cdebe9221df5832327462505021ef`，`51399 bytes / 620 lines`；Published body identity and Hugo=`1250 Pages / 0 WARNING / 0 ERROR` pass. BuildPilot remains `DESIGN / SYNTHETIC / NOT IMPLEMENTED / NOT RUN`. This is a persisted `PUBLISHED` candidate，not a prewritten commit/push/remote/`END_ARTICLE` result；Article 22 remains unstarted.

## Publisher / Build result

- Publisher execution: `/root/article21_publisher / REAL_SUBAGENT / PASS`.
- Published Path: `content/ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy.md`.
- Published Route: `/ai-empowerment/agent-engineering-21-trace-replay-failure-taxonomy/`.
- Front Matter Result: `PASS` — Hugo-compatible YAML，ASCII shortcode quotes，`series_order=220`，`weight=3220`.
- Series Result: `PASS` — Article 21 is linked as published；Article 22 stays planned，23 stays optional，24 stays planned.
- Internal Link Result: `PASS` — Article 20 -> 21，Article 21 -> 20 / course index；Article 21 -> 22=`ABSENT`.
- Semantic Diff Result: `PASS` — frozen Draft occurs exactly once at published byte offset `840`；extracted body=`51399 bytes / 620 lines / SHA-256 4ceb7e56bf8aa153518d66444de8b59bc87cdebe9221df5832327462505021ef`.
- Build Command: `hugo --gc --minify`.
- Build Result: `PASS / exit 0 / Hugo 0.157.0 / 1250 Pages / 0 WARNING / 0 ERROR`.
- Files Written: published Article 21；Article 20 page-tail navigation；series index；this publication evidence；one Publisher result in `subagent-trace.md`.
- Recommended Article Transition: `PRE_COMMIT_RECONCILIATION`，subject to Master artifact / state validation.
- Recommended Status Changes: Article 21 lifecycle candidate `PUBLISHED`；Publisher does not apply global durable state.
- Canonical Update Candidate: map Article 21 to the published content path；keep Article 22 planned，23 optional，24 planned；Master-only write.

### Persisted checkpoint projection

```text
Lifecycle Candidate: PUBLISHED
Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article 21
Next Transaction Candidate: Article 22 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 21 END_ARTICLE
```

This projection is the final repository state before Git verification：completion SHA、push、remote verification and `END_ARTICLE` remain derived runtime facts and are not prewritten.

## GIT_DIFF_VERIFY recovery

- Human Resume: `AUTHORIZED / 2026-08-28`.
- Failure reproduced: staged `git diff --cached --check` reported one terminal blank line in Published Content and one in `article-card.md`，exit=`2`; no commit or push had occurred.
- Root cause: both files ended in `0A0A` instead of the repository pattern's single terminal `0A`; run state also retained the active-gate semantics label.
- Retry 1 repair: remove only those two terminal blank lines；set `last_worker_result_semantics=LAST_PERSISTED_PRE_COMMIT_RESULT`；preserve the exact 15-file Article 21 transaction and every publication/evidence/build/future-asset invariant.
- Superseding boundary: the earlier persistence cut is superseded by the Retry 1 record in `subagent-trace.md`; repository writes return to zero after that record.

### Retry 2

- Human Resume: `AUTHORIZED / 2026-08-28`.
- Failure reproduced: after Retry 1，the frozen Draft remained byte-identical only at offset `839`，not the published identity boundary `840`; staged format check already passed and no commit/push occurred.
- Root cause: the first repair used a repeated course-index line as patch context，so it removed both the intended EOF blank and the distinct blank between the top navigation wrapper and Draft H1.
- Repair: restore exactly one newline after the top course-index wrapper using the following Draft H1 as unique context；leave the EOF at one terminal `0A` and leave Article knowledge bytes unchanged.
- Superseding boundary: Retry 2 replaces the Retry 1 cut；the complete 15-file GIT_DIFF_VERIFY must pass before commit.

## Historical Transaction Record

- Result: `PASS / LAST REPOSITORY WRITE`；Article 21 Lifecycle Candidate=`PUBLISHED`.
- Final / Evidence: `91 / 100 / 0 OPEN`；12 Claims；1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED；12 Cards.
- Publication / Build: frozen Draft byte identity PASS at offset `840`；Article20<->21 navigation PASS；public series index Article21 published；Hugo `1250 Pages / 0 WARNING / 0 ERROR`.
- Future Pointer: `READY / Article 22 / PRECHECK / NOT_STARTED / active worker NONE`；this pointer is not Article 22 PRECHECK or Kickoff.
- Git Evidence: Completion Evidence Source=`GIT_HISTORY + REMOTE_REFS`；Expected Completion Message=`Publish Agent Engineering Article 21`；completion SHA is not prewritten.
- Persistence Cut: from the final trace record repository writes=`ZERO`；only Git diff/stage/commit/push/remote and post-commit read-only verification are allowed.
