# Article 25｜Agent Runtime vs Harness：执行内核与工程控制面

- Canonical ID: `25`
- Workspace: `25-agent-runtime-vs-harness`
- Part: `V｜Harness Engineering`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `PRINCIPLE`
- Lifecycle Status: `PUBLISHED CANDIDATE`
- Evidence Status: `PASS / 12 CLAIMS / 12 CARDS / 4 CONFIRMED / 6 PARTIAL / 2 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION COMPLETE`
- Active Worker: `NONE`
- Blocker: `NONE`
- Lifecycle Candidate: `PUBLISHED / PUBLISHER RECOMMENDATION VERIFIED`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION PASS`
- Completion Resolution: `DERIVED_FROM_GIT_HISTORY`
- Completion Evidence Source: `GIT_HISTORY + REMOTE_REFS`
- Expected Completion Message: `Publish Agent Engineering Article 25`
- Next Transaction Candidate: `Article 26 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 25 END_ARTICLE`

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == 752a87de878830da1a7724d87d5f648d45ff3abb`.
- Previous boundary: Article 24 has one exact completion commit, is contained by local/origin/live `main`, and resolves `END_ARTICLE`.
- Article 23 remains `ADVANCED / OPTIONAL / SKIPPED / NOT_STARTED / ZERO ASSETS`.
- Future-asset guard before kickoff: Article 25—28 workspaces, Published Content and image assets all counted `0`.
- Continuous authorization covers Article 25 through `END_ARTICLE`, then Article 26—27 and Part V Audit; Article 28 is forbidden.

## Current boundary

Research、Evidence Gate、Outline、full Draft、fresh Review、独立Final Gate、mechanical Publish、independent Build与Pre-Commit Reconciliation均已通过：Final=`93 / 0 OPEN`；Draft block=`39916 bytes / 561 lines / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4`；Published Content精确包含该block一次；Hugo=`1253 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`。这是persisted `PUBLISHED` candidate，不预写completion commit、push、remote verification或`END_ARTICLE`；Article 26仍为未启动的PRECHECK pointer，Article 28 forbidden且零资产。

## Publisher / Build result candidate

- Publisher execution: `/root/a25_publisher / REAL_SUBAGENT / PASS`.
- Published Path: `content/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness.md`.
- Published Route: `/ai-empowerment/agent-engineering-25-agent-runtime-vs-harness/`.
- Front Matter Result: `PASS` — Hugo-compatible YAML，ASCII shortcode quotes，frozen Outline metadata preserved: `date=2026-08-29T00:00:00+08:00`，`series_order=260`，`weight=3260`.
- Series Result: `PASS` — public course index turns Article 25 into an `is-published` relref row；Article 26—27 remain planned/unlinked；Article 28 remains untouched.
- Internal Link Result: `PASS` — Article 24 links to Article 25 at top and bottom；Article 25 links to Article 24 and the course index at top and bottom；no Article 26 relref was added.
- Semantic Diff Result: `PASS / EXACT BYTE IDENTITY` — Published Content contains the frozen Draft as one contiguous block at byte offset `856`，with exactly one occurrence；Draft block is `39916 bytes / 561 LF / SHA-256 9239D92A45FDEC28ACF98EE4C88B1C9618737060A95AB1A08BE06F8F461BAAE4`.
- Build Command: `hugo --gc --minify`.
- Build Result: `PASS / exit 0 / Hugo 0.157.0 / 1253 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`.
- Warnings / Errors: final build emitted no `WARNING` or `ERROR` lines.
- Files Written: created published Article 25；modified Article 24 top/bottom navigation；modified course index Article 25 row；updated this README candidate result only.
- Recommended Article Transition: `MASTER_STATE_UPDATE`，subject to Master artifact / state validation.
- Recommended Status Changes: Article 25 lifecycle candidate `PUBLISHED` for Master reconciliation only；Publisher does not apply global durable state and does not mark this workspace `PUBLISHED`.
- Canonical Update Candidate: Master may map Article 25 to the published content path while keeping Article 26—27 planned/unlinked and Article 28 untouched.

### Checkpoint readiness recommendation

```text
Lifecycle Candidate: PUBLISHED
Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
Completion Resolution: DERIVED_FROM_GIT_HISTORY
Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
Expected Completion Message: Publish Agent Engineering Article 25
Next Transaction Candidate: Article 26 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 25 END_ARTICLE
```

This is a future-safe Publisher readiness recommendation，not a claim that Master State Update，Pre-Commit Reconciliation，the completion commit，push，remote verification，`PUBLISHED` durable state，`END_ARTICLE` or Article 26 kickoff has occurred.
