# Article 28｜怎样把 DeepSeek Harness 当作 Evidence-first 源码教材

- Canonical ID: `28`
- Workspace: `28-dsh-evidence-first-source-method`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `S`
- Optional: `NO`
- Article Type: `STAGE_NAVIGATION / SOURCE_METHOD`
- Lifecycle Status: `PUBLISH_CANDIDATE`
- Evidence Status: `EVIDENCE_GATE PASS / 16 CLAIMS / 12 CARDS / 0 BLOCKED`
- Required Lab: `NONE`
- Required Evidence Work: `BASELINE INSTALL / BUILD / TEST / RUN PROBES`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `PRE_COMMIT_RECONCILIATION`
- Active Worker: `NONE`
- Blocker: `NONE`
- Expected Completion Message: `Publish Agent Engineering Article 28`
- Persisted Checkpoint: `PRE_COMMIT_RECONCILIATION RETRY 1 PASS`
- Completion Resolution: `PENDING GIT HISTORY + REMOTE REFS`
- Next Transaction Candidate: `Article 29 PRECHECK / REQUIRES ARTICLE 28 END_ARTICLE`

## Frozen DSH baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Full Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Selected At: `2026-08-29`
- Verified At: `2026-08-30 Asia/Shanghai`
- External Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Fixture Policy: `EXTERNAL / NOT VENDORED / NOT COMMITTED`

## Frozen boundaries

- 本篇建立 Part VI 的 Evidence Baseline、验证层级和 Article 29—37 路由，不逐模块解释 DSH。
- 所有行为结论只适用于固定 commit；Developer Preview 不等于 production-ready。
- `SOURCE_CONFIRMED` 与 `RUNTIME_CONFIRMED` 独立；README、目录或类名不构成生命周期证明。
- BuildPilot 只允许 `ADOPT / SIMPLIFY / REJECT / DEFER` 教学决策，不启动 Article 38 或 Part VII 设计。

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Baseline Manifest](baseline-manifest.md)
- [Source Map](source-map.md)
- [Baseline Probes](experiments/baseline-probes.md)
- [Outline](outline.md)
- [Draft](draft.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

## Publish candidate

- Publish Gate: `PASS`
- Published Path: `content/ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md`
- Approved Front Matter: `title / slug / date / description / draft=false / tags / series / primary_series / series_role / series_order=290 / weight=3290`
- Publisher Top Navigation: `Article 27 previous + Course Index / exactly once`
- Draft Top Navigation: `EXCLUDED FROM BODY EXTRACTION`
- Published Bottom Navigation: `NONE ADDED / DRAFT ENDING PRESERVED`
- Article 27 Navigation: `TOP + BOTTOM NEXT LINK TO ARTICLE 28`
- Course Index: `ARTICLE 28 PUBLISHED / ARTICLE 29 STILL PLANNED AND UNLINKED`
- Semantic Body Identity: `PASS / normalized-LF exact contiguous H1-to-EOF match / SHA-256 60BA15EA373BAD5D649F2C69ACF924F60DA202E8B0DB7028680DB84F39F3053B / 37772 UTF-8 bytes / 553 lines`
- Hugo Build: `PASS / hugo --gc --minify / exit 0 / 1256 pages / 0 WARNING / 0 ERROR`
- Commit / Push: `NOT PERFORMED BY PUBLISHER`

## Pre-commit reconciliation candidate

- Branch / Refs: `main / HEAD == origin/main == live main == 03c1649b7915d39dda91f67a8cc8b0257306bb4d`
- Exact Transaction: `18 files / Article 28 only plus Article 27 and course navigation/state surfaces`
- DSH Fixture: `cd5ef8148158c3a752a658978873241fdf8e2bbc / clean`
- Future Asset Guard: `Article 29—44 production assets = 0`
- Fresh Hugo: `1256 Pages / 44 Static / 1 Alias / 0 WARNING / 0 ERROR`
- Diff Check: `FIRST CUT INVALIDATED / article-card.md EOF blank normalized / RETRY 1 PASS`
- Next Gate: `GIT_DIFF_VERIFY -> ARTICLE_CHECKPOINT_COMMIT`
