# Article 27｜Harness 的设计取舍：可替换性、复杂度、Bloat 与演化

- Canonical ID: `27`
- Workspace: `27-harness-design-tradeoffs`
- Part: `V｜Harness Engineering`
- Course Weight: `M`
- Optional: `NO`
- Article Type: `PRINCIPLE`
- Lifecycle Status: `PUBLISHED CANDIDATE`
- Evidence Status: `PASS / 11 CLAIMS / 11 CARDS / 1 CONFIRMED / 7 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION COMPLETE`
- Active Worker: `NONE`
- Blocker: `NONE`
- Lifecycle Candidate: `PUBLISHED / NORMALIZED IDENTITY VERIFIED`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION RETRY 1 PASS`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article 27`
- Next Transaction Candidate: `Part V Audit / REQUIRES ARTICLE 27 END_ARTICLE`

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == 1ed76a3075c912e33553b4508757dd1066e7a201`.
- Previous boundary: Article 26 has one exact completion commit, is contained by local/origin/live `main`, and resolves `END_ARTICLE`.
- Article 23 remains `ADVANCED / OPTIONAL / SKIPPED / NOT_STARTED / ZERO ASSETS`.
- Future-asset guard before kickoff: Article 27—28 workspaces, Published Content and image assets all counted `0`.
- Authorization covers Article 27 through `END_ARTICLE`, then Part V Audit and STOP; Article 28 is forbidden.

## Current boundary

Article 27 EOF normalization已由fresh Reviewer定点复核：normalized Draft=`41490 bytes / 495 lines / SHA-256 259C682BD84C557BCEFF20171595F24D8097B8C3E27A5155EF8069DC7FCD3E9F`，加回单一trailing LF可重现旧SHA，Published exact occurrence=`1`，语义、11/11与所有边界不变。PRE_COMMIT_RECONCILIATION retry 1通过；下一事务仅Part V Audit，Article28 forbidden。

## Publisher / Build result candidate

- Publisher execution: `/root/a27_publisher / REAL_SUBAGENT / PASS`.
- Published Path: `content/ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md`.
- Published Route: `/ai-empowerment/agent-engineering-27-harness-design-tradeoffs/`.
- Front Matter Result: `PASS` — Hugo-compatible YAML，ASCII shortcode quotes，frozen Outline metadata preserved: `date=2026-08-30T00:00:00+08:00`，`series_order=280`，`weight=3280`.
- Series Result: `PASS` — public course index turns Article 27 into an `is-published` relref row；Article 28 remains planned/unlinked.
- Internal Link Result: `PASS` — Article 26 links to Article 27 at top and bottom；Article 27 links to Article 26 and the course index；no Article 28 relref was added.
- Semantic Diff Result: `PASS / EXACT BYTE IDENTITY AFTER EOF NORMALIZATION` — Published Content contains the normalized Draft as one contiguous block at byte offset `918`，with exactly one occurrence；Draft block is `41490 bytes / 495 lines / SHA-256 259C682BD84C557BCEFF20171595F24D8097B8C3E27A5155EF8069DC7FCD3E9F`.
- Build Command: `hugo --gc --minify`.
- Build Result: `PASS / exit 0 / Hugo 0.157.0 / 1255 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`.
- Warnings / Errors: final build emitted no `WARNING` or `ERROR` lines.
- Files Written: created published Article 27；modified Article 26 top/bottom navigation；modified course index Article 27 row；updated this README candidate result only.
- Recommended Article Transition: `MASTER_STATE_UPDATE`，subject to Master artifact / state validation.
- Recommended Status Changes: Article 27 lifecycle candidate `PUBLISHED` for Master reconciliation only；Publisher does not apply global durable state and does not mark `END_ARTICLE`.
- Canonical Update Candidate: Master may map Article 27 to the published content path while keeping Article 28 planned/unlinked and untouched.

### Checkpoint readiness recommendation

```text
Lifecycle Candidate: PUBLISHED
Persisted Checkpoint: PRE_COMMIT_RECONCILIATION RETRY 1 PASS
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article 27
Next Transaction Candidate: Part V Audit / REQUIRES ARTICLE 27 END_ARTICLE
```

This is a future-safe Publisher readiness recommendation，not a claim that Master State Update，Pre-Commit Reconciliation，the completion commit，push，remote verification，`PUBLISHED` durable state，`END_ARTICLE`，Part V Audit or Article 28 kickoff has occurred.
