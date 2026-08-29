# Article 26｜Harness 的最小能力模型：Capability、Policy、Session、Trace 与 Recovery

- Canonical ID: `26`
- Workspace: `26-harness-minimum-capability-model`
- Part: `V｜Harness Engineering`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `PRINCIPLE`
- Lifecycle Status: `PUBLISHED CANDIDATE`
- Evidence Status: `PASS / 11 CLAIMS / 11 CARDS / 0 CONFIRMED / 8 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION COMPLETE`
- Active Worker: `NONE`
- Blocker: `NONE`
- Lifecycle Candidate: `PUBLISHED / PUBLISHER RECOMMENDATION VERIFIED`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION PASS`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article 26`
- Next Transaction Candidate: `Article 27 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 26 END_ARTICLE`

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == 07000ceb94dd244e5f312d7787a6c83795c47f58`.
- Previous boundary: Article 25 has one exact completion commit, is contained by local/origin/live `main`, and resolves `END_ARTICLE`.
- Article 23 remains `ADVANCED / OPTIONAL / SKIPPED / NOT_STARTED / ZERO ASSETS`.
- Future-asset guard before kickoff: Article 26—28 workspaces, Published Content and image assets all counted `0`.
- Continuous authorization covers Article 26 through `END_ARTICLE`, then Article 27 and Part V Audit; Article 28 is forbidden.

## Current boundary

Article 26 Research、Evidence Gate、Outline、Draft、Review/Revision/Recheck、Final Gate、Publish、independent Build与Pre-Commit Reconciliation均通过：`A26-R0-F01/F02 CLOSED / 0 OPEN`；Draft block=`56217 bytes / 704 lines / SHA-256 B3CF1FE5BF7AB896CECADC79471E9988EC42525668971B50B73C228CCE6C0D00`；Hugo=`1254 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`。这是persisted `PUBLISHED` candidate；Article 27仍为未启动PRECHECK pointer，Article 28 forbidden。

## Publisher / Build result candidate

- Publisher execution: `/root/a26_publisher / REAL_SUBAGENT / PASS`.
- Published Path: `content/ai-empowerment/agent-engineering-26-harness-minimum-capability-model.md`.
- Published Route: `/ai-empowerment/agent-engineering-26-harness-minimum-capability-model/`.
- Front Matter Result: `PASS` — Hugo-compatible YAML，ASCII shortcode quotes，frozen Outline metadata preserved: `date=2026-08-30T00:00:00+08:00`，`series_order=270`，`weight=3270`.
- Series Result: `PASS` — public course index turns Article 26 into an `is-published` relref row；Article 27 remains planned/unlinked；Article 28 remains untouched.
- Internal Link Result: `PASS` — Article 25 links to Article 26 at top and bottom；Article 26 links to Article 25 and the course index at top and bottom；no Article 27 relref was added.
- Semantic Diff Result: `PASS / EXACT BYTE IDENTITY` — Published Content contains the frozen Draft as one contiguous block at byte offset `946`，with exactly one occurrence；Draft block is `56217 bytes / 704 LF / SHA-256 B3CF1FE5BF7AB896CECADC79471E9988EC42525668971B50B73C228CCE6C0D00`.
- Build Command: `hugo --gc --minify`.
- Build Result: `PASS / exit 0 / Hugo 0.157.0 / 1254 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`.
- Warnings / Errors: final build emitted no `WARNING` or `ERROR` lines.
- Files Written: created published Article 26；modified Article 25 top/bottom navigation；modified course index Article 26 row；updated this README candidate result only.
- Recommended Article Transition: `MASTER_STATE_UPDATE`，subject to Master artifact / state validation.
- Recommended Status Changes: Article 26 lifecycle candidate `PUBLISHED` for Master reconciliation only；Publisher does not apply global durable state and does not mark this workspace `PUBLISHED`.
- Canonical Update Candidate: Master may map Article 26 to the published content path while keeping Article 27 planned/unlinked and Article 28 untouched.

### Checkpoint readiness recommendation

```text
Lifecycle Candidate: PUBLISHED
Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article 26
Next Transaction Candidate: Article 27 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 26 END_ARTICLE
```

This is a future-safe Publisher readiness recommendation，not a claim that Master State Update，Pre-Commit Reconciliation，the completion commit，push，remote verification，`PUBLISHED` durable state，`END_ARTICLE` or Article 27 kickoff has occurred.
