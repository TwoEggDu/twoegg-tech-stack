# Article 20｜Budget Engineering：Token、Step、Cost 与 Latency

- Canonical ID: 20
- Workspace: 20-budget-engineering-token-step-cost-latency
- Part: IV｜Reliable Agent Engineering
- Course Weight: M
- Optional: NO
- Article Type: PRINCIPLE
- Lifecycle Candidate: PUBLISHED
- Evidence Status: PASS / 9 of 9 / 1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED / 11 Cards
- Required Lab: NONE
- Mode: NORMAL_ARTICLE
- Active Worker: NONE
- Blocker: NONE
- Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
- Completion Resolution: DERIVED_FROM_GIT_HISTORY
- Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
- Expected Completion Message: Publish Agent Engineering Article 20
- Next Transaction Candidate: Article 21 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 20 END_ARTICLE

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == 73a0f628e5580226f4c65890f81372d7ededd43d`.
- Previous boundary: Article 19 has one exact completion commit，is contained by local / origin / live `main`，and resolves `END_ARTICLE`.
- Article 20 assets before kickoff: `0`；Article 21 / 23 / 24 assets: `0`.
- Human authorization covers this Article through `END_ARTICLE_20` or a contract-defined real blocker. It does not authorize Article 23 or 24.

## Production assets

- Article Card: article-card.md
- Research: PASS / 9 Claims
- Evidence: PASS / 11 Cards / 0 BLOCKED
- Outline: PASS / 12 teaching units / 9 of 9 Claims / no new core Claim
- Draft: REVISED / SHA-256 031B873C7C027D22E0D7EB9649D96CFE222AAACBF9EE19CE89B3C7C9F4759E49 / 44197 bytes / 475 lines / 9 of 9 Claims
- Review: CYCLE 1 PASS / 91 / F01—F03 CLOSED / 0 OPEN / 0 ESCALATED
- Subagent Trace: subagent-trace.md
- Published Content Candidate: `content/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency.md` / route `/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency/`

## Publication Result Candidate

- Published Path: `content/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency.md`
- Published Route: `/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency/`；rendered site path `/twoegg-tech-stack/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency/`
- Front Matter Result: `PASS`；exact title / slug / timezone-qualified date `2026-08-26T00:00:00+08:00` / description / `draft: false` / tags / series metadata / `series_order: 210` / `weight: 3210` are present and Hugo accepted the YAML.
- Series Result: `PASS`；public series index Article 20 is `is-published` with one ASCII-quote `relref` and status `已发布`；Articles 21—22 remain `is-planned`，Article 23 remains `is-optional`，and Article 24 remains unchanged and planned，with no future-Article link added.
- Internal Link Result: `PASS`；Article 20 has exactly one `上一篇` relref to Article 19 and no `下一篇` or Article 21 relref；Article 19 has exactly one `下一篇` relref to Article 20；rendered Article 19↔20 navigation exists.
- Semantic Diff Result: `PASS`；after removing frontmatter and the single Previous Article 19 wrapper，Published Content body is byte-identical UTF-8/LF text to frozen `draft.md`；body offset=`721` bytes；SHA-256=`031B873C7C027D22E0D7EB9649D96CFE222AAACBF9EE19CE89B3C7C9F4759E49`；bytes=`44197`；frozen physical lines=`475`.
- Files Written: created `content/ai-empowerment/agent-engineering-20-budget-engineering-token-step-cost-latency.md`；modified `content/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox.md`、`content/ai-empowerment/agent-engineering-series-index.md`、this README and `subagent-trace.md` only；no publication asset was created.
- Recommended Article Transition: `MASTER_STATE_UPDATE`；Publisher result is a publication/build candidate and does not itself set Lifecycle `PUBLISHED`.
- Global / Canonical / Git Boundary: Publisher did not modify global durable state，canonical series plan or Git state；no commit，push or next-Article start was performed.

## Build Result Candidate

- Build Command: `hugo --gc --minify`
- Hugo Version: `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；BuildDate=`2026-02-25T16:38:33Z`；VendorInfo=`gohugoio`.
- Initial Sandbox Attempt: `BLOCKED / exit code 1`；PowerShell `ResourceUnavailable`，launch of the installed WinGet `hugo.exe` returned `拒绝访问`，so this attempt produced no Hugo build result.
- Controlled Build Result: `PASS / exit code 0`；Pages=`1249`；Paginator pages=`0`；Non-page files=`0`；Static files=`44`；Processed images=`0`；Aliases=`1`；Cleaned=`0`；final verification Total=`6037 ms`.
- Warnings: `0`.
- Errors: `0`.
- Render Verification: `PASS`；Article 20 route/title and Previous Article 19 link exist；Article 20 has no rendered Article 21 link；Article 19 renders the next link to Article 20；the course index renders the published Article 20 target while Articles 21—24 preserve planned/optional status and no public links.

## Current boundary

Research，Evidence Gate，Outline，Draft，independent Review，Revision/Recheck，Final Gate，mechanical Publish，independent Build and Pre-Commit Reconciliation are complete：`PASS / 91 / 0 OPEN`；9 Claims map to 11 Evidence Cards with zero `BLOCKED`；Hugo=`1249 Pages / 0 WARNING / 0 ERROR`. Required Lab is `NONE`；runtime observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`. This is a persisted `PUBLISHED` candidate，not a prewritten commit/push/END result；Article 21 remains unstarted.

## Historical Transaction Record

- Result: `PASS / LAST REPOSITORY WRITE`；Article 20 Lifecycle Candidate=`PUBLISHED`.
- Final / Evidence: `91 / 100 / 0 OPEN`；9 Claims；1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED；11 Cards.
- Publication / Build: frozen Draft byte identity PASS；Article19<->20 navigation PASS；public series index Article20 published；Hugo `1249 Pages / 0 WARNING / 0 ERROR`.
- Future Pointer: `READY / Article 21 / PRECHECK / NOT_STARTED / active worker NONE`；this pointer is not Article 21 PRECHECK or Kickoff.
- Git Evidence: Completion Evidence Source=`GIT_HISTORY + REMOTE_REFS`；Expected Completion Message=`Publish Agent Engineering Article 20`；completion SHA is not prewritten.
- Persistence Cut: from this record repository writes=`ZERO`；only Git diff/stage/commit/push/remote and post-commit read-only verification are allowed.
