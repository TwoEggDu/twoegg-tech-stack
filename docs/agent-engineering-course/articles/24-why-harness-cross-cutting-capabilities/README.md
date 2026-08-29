# Article 24｜为什么最终需要 Harness：横切能力由谁承载

- Canonical ID: `24`
- Workspace: `24-why-harness-cross-cutting-capabilities`
- Part: `V｜Harness Engineering`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `PRINCIPLE`
- Lifecycle Status: `PUBLISHED CANDIDATE`
- Evidence Status: `PASS / 12 CLAIMS / 12 CARDS / 3 CONFIRMED / 6 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION COMPLETE`
- Active Worker: `NONE`
- Blocker: `NONE`
- Lifecycle Candidate: `PUBLISHED / PUBLISHER RECOMMENDATION VERIFIED`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION PASS`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article 24`
- Next Transaction Candidate: `Article 25 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 24 END_ARTICLE`

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == a6763629aaaeb0520b219423fd5ef9c6b442aba4`.
- Previous boundary: Article 22 resolves `END_ARTICLE`; the original and targeted Part IV audits both pass, and the targeted re-audit checkpoint is present on all three current main refs.
- Optional route: Article 23 is `ADVANCED / OPTIONAL / SKIPPED / PLANNED / NOT_STARTED`; it has zero production assets and does not block Part V.
- Future-asset guard before kickoff: Article 24—28 workspaces, Published Content and image assets all counted `0`.
- Human authorization covers the bounded continuous sequence Article 24—27 plus Part V Audit. Article 28 is forbidden.

## Current boundary

Research、Evidence Gate、Outline、Draft、Review/Revision/Recheck、Final Gate、mechanical Publish、independent Build与Pre-Commit Reconciliation均通过：Final=`94 / A24-R0-F01 CLOSED / 0 OPEN`；Draft block=`41730 bytes / 474 lines / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`；Published Content精确包含该block一次；Hugo=`1252 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`。这是persisted `PUBLISHED` candidate，不预写completion commit、push、remote verification或`END_ARTICLE`；Article 25仍为未启动的PRECHECK pointer，Article 28 forbidden且零资产。

## Publisher / Build result candidate

- Publisher execution: `/root/a24_publisher / REAL_SUBAGENT / PASS`.
- Published Path: `content/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities.md`.
- Published Route: `/ai-empowerment/agent-engineering-24-why-harness-cross-cutting-capabilities/`.
- Front Matter Result: `PASS` — Hugo-compatible YAML，ASCII shortcode quotes，frozen Outline metadata preserved: `date=2026-08-29T00:00:00+08:00`，`series_order=250`，`weight=3250`.
- Series Result: `PASS` — public course index turns Article 24 into an `is-published` relref row；Article 23 remains optional/unlinked；Articles 25—27 remain planned/unlinked；Article 28 remains untouched.
- Internal Link Result: `PASS` — Article 22 links to Article 24 at top and bottom；Article 24 links to Article 22 and the course index at top and bottom；no Article 25 relref was added.
- Semantic Diff Result: `PASS / EXACT BYTE IDENTITY` — Published Content contains the frozen Draft as one contiguous block at byte offset `903`，with exactly one occurrence；Draft block is `41730 bytes / 474 LF / SHA-256 F721336104862EF5F1CF675469A4C4263DF88C4A218E6A8BDE5B8AC975E91040`.
- Build Command: `hugo --gc --minify`.
- Build Result: `PASS / exit 0 / Hugo 0.157.0 / 1252 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`.
- Warnings / Errors: final build emitted no `WARNING` or `ERROR` lines.
- Files Written: created published Article 24；modified Article 22 top/bottom navigation；modified course index Article 24 row；updated this README candidate result only.
- Recommended Article Transition: `MASTER_STATE_UPDATE`，subject to Master artifact / state validation.
- Recommended Status Changes: Article 24 lifecycle candidate `PUBLISHED` for Master reconciliation only；Publisher does not apply global durable state and does not mark this workspace `PUBLISHED`.
- Canonical Update Candidate: Master may map Article 24 to the published content path while preserving Article 23 as `Advanced / Optional / SKIPPED / PLANNED / NOT_STARTED` and keeping Article 25—28 assets untouched.

### Checkpoint readiness recommendation

```text
Lifecycle Candidate: PUBLISHED
Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article 24
Next Transaction Candidate: Article 25 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 24 END_ARTICLE
```

This is a future-safe Publisher readiness recommendation，not a claim that Master State Update，Pre-Commit Reconciliation，the completion commit，push，remote verification，`PUBLISHED` durable state，`END_ARTICLE` or Article 25 kickoff has occurred.
