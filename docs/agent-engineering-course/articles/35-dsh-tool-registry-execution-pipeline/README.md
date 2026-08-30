# Article 35｜Tool Registry 与 Tool Execution Pipeline

- Canonical ID: `35`
- Workspace: `35-dsh-tool-registry-execution-pipeline`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `SOURCE_TRACE / REQUIRED_SOURCE_EXPERIMENT`
- Lifecycle Status: `PUBLISHED CANDIDATE / PRE_COMMIT_RECONCILIATION PASS / INCOMPLETE`
- Evidence Status: `EVIDENCE_GATE PASS / 12 CLAIMS / 12 FINAL CARDS / 35-X01—X05 PASS / 0 BLOCKED`
- Required Evidence Work: `SOURCE MAP / FULL CALL PATH / FIVE SAME-CALL NEGATIVE TRACES`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `GIT_DIFF_VERIFY`
- Active Worker: `NONE`
- Review Cycle: `2`
- Findings: `A35-R1-F01—F05 CLOSED / 0 OPEN`
- Final Gate Findings: `A35-FG-F01/F02 CLOSED / 0 OPEN`
- Blocker: `NONE`
- Expected Completion Message: `Publish Agent Engineering Article 35`
- Next Allowed Gate: `GIT_DIFF_VERIFY`

## Frozen baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`

## Evidence boundary

- Source Map / Call Path=`PASS`; final Evidence Cards=`12 / 12`; five required traces=`PASS`.
- Cycle 0 `22 passed` and Recovery Attempt 1 `exit 0 / selected 0 of 5` remain `NOT_ACCEPTED`.
- Runtime evidence is limited to temporary source-owned instrumentation, pinned DSH components, repo-owned MockAdapter, and in-memory Tool/approval/spill fixtures.
- No real Provider, production Tool/service/deployment, external side effect, actual client UI, hard kill/rollback, universal spill/summary, production-safety, Part VII, or BuildPilot Runtime evidence exists.
- Article 36—37 remain unstarted; Article 38—44 remain forbidden and zero-assets.

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Repository Map](repository-map.md)
- [Call Path](call-path.md)
- [Negative Trace Contract](experiments/tool-execution-negative-traces.md)
- [Outline](outline.md)
- [Draft](draft.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

## Current review disposition

- Review Cycle 1 first recheck=`F01/F02/F03/F05 CLOSED / F04 OPEN MAJOR`.
- Revision Worker repaired `A35-R1-F01`, `A35-R1-F02`, and `A35-R1-F05` within Draft/Evidence/Review.
- Master Cycle 2 kept invalid/missing records visible, added deterministic validation metadata, and established fresh current-time `RESEARCH -> SOURCE_MAP -> OUTLINE -> AUTHOR_DRAFT` authority without inventing historical envelopes.
- Final Gate Recheck 2=`PASS / A35-R1-F01—F05 + A35-FG-F01/F02 CLOSED / 0 OPEN / ELIGIBLE_FOR_PUBLISH`.

## Publication candidate

- Published Path: `content/ai-empowerment/agent-engineering-35-dsh-tool-registry-execution-pipeline.md`.
- Frontmatter: `2026-08-30T00:00:00+08:00 / series_order 360 / weight 3360`.
- Draft/Published H1-to-EOF: `EXACT / 38999 bytes / 737 LF lines / SHA-256 8F2EED28885E65C3B921564102A97FC528D854C8CC17F346054B9A7CB961E764`.
- Navigation: `Article 34 -> 35 UNIQUE / series index -> 35 UNIQUE / Article 36 relref ZERO`.
- Hugo production build: `PASS / Hugo 0.157.0 / 1263 Pages / 44 Static / 1 Alias / exit 0`.
- Persisted checkpoint: `PRE_COMMIT_RECONCILIATION RETRY 1 PASS / 36-file transaction / completion subject count before commit 0`.
- Git Diff Revision: `two new raw-text whitespace findings normalized with before/after hashes preserved in recovery manifest; semantic evidence unchanged`.
- Completion resolution: `DERIVED_FROM_GIT_HISTORY + REMOTE_REFS / currently INCOMPLETE`.
- Next transaction candidate: `Article 36 PRECHECK after ResolveArticleCompletion(35) == END_ARTICLE`.
