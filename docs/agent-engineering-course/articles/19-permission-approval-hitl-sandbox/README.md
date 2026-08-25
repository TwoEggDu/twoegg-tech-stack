# Article 19｜Permission、Approval、Human-in-the-loop 与 Sandbox

- Canonical ID: 19
- Workspace: 19-permission-approval-hitl-sandbox
- Part: IV｜Reliable Agent Engineering
- Course Weight: L
- Optional: NO
- Article Type: PRINCIPLE
- Lifecycle Candidate: PUBLISHED
- Evidence Status: PASS / 10 of 10 / 3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED / 12 Cards
- Required Lab: NONE
- Mode: NORMAL_ARTICLE
- Active Worker: NONE
- Blocker: NONE
- Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
- Completion Resolution: DERIVED_FROM_GIT_HISTORY
- Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
- Expected Completion Message: Publish Agent Engineering Article 19
- Next Transaction Candidate: Article 20 PRECHECK / NOT_STARTED / REQUIRES ARTICLE 19 END_ARTICLE

## Precheck and kickoff

- Start refs: `HEAD == origin/main == live main == a0d8d1b2fa5380f9a4150f72b962ac15fe11a96b`.
- Previous boundary: Article 18 has one exact completion commit, is contained by local / origin / live `main`, and resolves `END_ARTICLE`.
- Article 19 assets before kickoff: `0`; Article 20 / 23 / 24 assets: `0`.
- Human authorization covers this Article through `END_ARTICLE_19` or a contract-defined real blocker. It does not authorize Article 23 or 24.

## Production assets

- Article Card: article-card.md
- Research: research.md
- Evidence: evidence.md
- Review: review.md
- Subagent Trace: subagent-trace.md
- Outline: PASS / 10 of 10 Claims / no new core Claim
- Draft: REVISED / SHA-256 5B64D6B16CF0CC57E9D56F6B9DCE7A568DDE86A8E9079535E443B0F8C99FADD4 / 580 lines / 10 of 10 Claims
- Published Content: `content/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox.md` / route `/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox/`

## Publication Result

- Published Path: `content/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox.md`
- Published Route: `/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox/`；rendered site path `/twoegg-tech-stack/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox/`
- Front Matter Result: `PASS`；exact title / slug / same calendar date `2026-08-26` with timezone-qualified value `2026-08-26T00:00:00+08:00` / `draft: false` / series metadata / `series_order: 200` / `weight: 3200` are present and Hugo accepted the YAML. The timezone qualifier prevents the current Shanghai publication date from being omitted as a future page by Hugo's UTC clock.
- Series Result: `PASS`；public series index Article 19 is `is-published` with one ASCII-quote `relref` and status `已发布`；Articles 20—22 remain `is-planned`，Article 23 remains `is-optional`，Article 24 remains unchanged and planned，with no future-Article link added.
- Internal Link Result: `PASS`；Article 19 has exactly one `上一篇` relref to Article 18 and no `下一篇` or Article 20 relref；Article 18 has exactly one `下一篇` relref to Article 19；rendered Article 18↔19 navigation exists.
- Semantic Diff Result: `PASS`；after removing frontmatter and the single Previous Article 18 wrapper，Published Content body is byte-identical UTF-8/LF text to frozen `draft.md`；SHA-256=`5B64D6B16CF0CC57E9D56F6B9DCE7A568DDE86A8E9079535E443B0F8C99FADD4`；physical lines=`580`.
- Files Written: created `content/ai-empowerment/agent-engineering-19-permission-approval-hitl-sandbox.md`；modified `content/ai-empowerment/agent-engineering-18-evidence-contract.md`、`content/ai-empowerment/agent-engineering-series-index.md`、this README and `subagent-trace.md` only；no publication asset was created.
- Recommended Article Transition: `MASTER_STATE_UPDATE`；Publisher result is a publication candidate and does not itself set Lifecycle `PUBLISHED`.
- Recommended Status Changes: Master may project Article 19 Publication / Build `PASS` and prepare the future-safe `PUBLISHED` candidate after independent validation；Publisher did not modify global durable state.
- Canonical Update Candidate: Master-owned canonical Article 19 publication metadata may be evaluated during reconciliation；not applied by Publisher.
- Checkpoint Readiness: Lifecycle Candidate=`PUBLISHED`；Persisted Checkpoint=`PRE_COMMIT_RECONCILIATION PASS`；Completion Resolution=`DERIVED_FROM_GIT_HISTORY`；Completion Evidence Source=`GIT_HISTORY + REMOTE_REFS`；Expected Completion Message=`Publish Agent Engineering Article 19`；Next Transaction Candidate=`Article 20 PRECHECK / NOT_STARTED`.

## Build Result

- Build Command: `hugo --gc --minify`
- Hugo Version: `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；BuildDate=`2026-02-25T16:38:33Z`；VendorInfo=`gohugoio`.
- Initial Sandbox Attempt: `BLOCKED / exit code 1`；PowerShell `ResourceUnavailable`，launch of WinGet `hugo.exe` returned `拒绝访问`，so this attempt produced no Hugo build result.
- First Executed Build: `FAIL / exit code 1`；Hugo reported two `REF_NOT_FOUND` errors because bare `date: "2026-08-26"` omitted Article 19 as a future page at the current clock；the only correction was the same-date timezone-qualified frontmatter value `2026-08-26T00:00:00+08:00`.
- Final Result: `PASS`；fresh completion verification exit code=`0`；Pages=`1248`；Paginator pages=`0`；Non-page files=`0`；Static files=`44`；Processed images=`0`；Aliases=`1`；Cleaned=`0`；Total=`6035 ms`.
- Warnings: `0` in the final build.
- Errors: `0` in the final build.
- Render Verification: `PASS`；Article 19 route/title and Previous Article 18 link exist；Article 19 has no rendered Article 20 link；Article 18 renders the next link to Article 19；course index renders the published Article 19 target while Articles 20—24 preserve planned/optional status and no public links.

## Current boundary

Research，Evidence Gate，Outline，Draft，independent Review，Final Gate，mechanical Publish，independent Build and Pre-Commit Reconciliation are complete：`PASS / 93 / 0 OPEN`；10 Claims map to 12 Evidence Cards with zero `BLOCKED`；Hugo=`1248 Pages / 0 WARNING / 0 ERROR`. Required Lab is `NONE`；runtime observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`. This is a persisted `PUBLISHED` candidate，not a prewritten commit/push/END result；Article 20 remains unstarted.

## Historical Transaction Record

- Result: `PASS / LAST REPOSITORY WRITE`；Article 19 Lifecycle Candidate=`PUBLISHED`.
- Final / Evidence: `93 / 100 / 0 OPEN`；10 Claims；3 CONFIRMED / 2 PARTIAL / 5 PROPOSAL / 0 BLOCKED；12 Cards.
- Publication / Build: frozen Draft byte identity PASS；Article18<->19 navigation PASS；public series index Article19 published；Hugo `1248 Pages / 0 WARNING / 0 ERROR`.
- Future Pointer: `READY / Article 20 / PRECHECK / NOT_STARTED / active worker NONE`；this pointer is not Article 20 PRECHECK or Kickoff.
- Git Evidence: Completion Evidence Source=`GIT_HISTORY + REMOTE_REFS`；Expected Completion Message=`Publish Agent Engineering Article 19`；completion SHA is not prewritten.
- Persistence Cut: from this record repository writes=`ZERO`；only Git diff/stage/commit/push/remote and post-commit read-only verification are allowed.
