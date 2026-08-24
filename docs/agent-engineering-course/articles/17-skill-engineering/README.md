# Article 17｜Skill Engineering：按需加载领域方法，而不是再堆一层 Prompt

- Canonical ID: 17
- Workspace: 17-skill-engineering
- Part: III｜Agent 的信息、状态与知识
- Course Weight: M
- Optional: NO
- Article Type: PRINCIPLE
- Lifecycle Candidate: PUBLISHED
- Evidence Status: PASS / 8 CONFIRMED / 4 PARTIAL / 3 PROPOSAL / 0 BLOCKED / 15 claims / 12 Evidence Cards
- Required Lab: NONE
- Mode: NORMAL_ARTICLE
- Active Worker: NONE
- Blocker: NONE
- Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
- Completion Resolution: DERIVED_FROM_GIT_HISTORY
- Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
- Expected Completion Message: Publish Agent Engineering Article 17
- Next Transaction Candidate: Article 18 PRECHECK / NOT_STARTED / FORBIDDEN CURRENT RUN

## Precheck and kickoff

- Start refs: HEAD == origin/main == live main == 3799f212d35307f48c5e00e75507717f6abe5cd9.
- Previous completion: ResolveArticleCompletion(16) = END_ARTICLE; unique completion commit bf00d4e63f2f634d4b62afb5fe2ee44ae2051571.
- Article 16 workspace and Published Content have no post-completion content drift.
- Article 17 assets before kickoff: 0; Article 18 assets: 0; Part III Audit asset: 0.
- Human authorization covers Article 17 through END_ARTICLE_17 or a real blocker. Article 18 is not authorized.

## Production assets

- Article Card: article-card.md
- Research: research.md
- Evidence: evidence.md
- Review: review.md
- Subagent Trace: subagent-trace.md

- Outline: `PASS`；Draft: frozen and semantically identical to Published Content.
- Final Review: `PASS / 96 / 0 OPEN / Cycle 3`；17-F01 / 17-F02 CLOSED.
- Published Content: `content/ai-empowerment/agent-engineering-17-skill-engineering.md`；route `/ai-empowerment/agent-engineering-17-skill-engineering/`.
- Build Verify: `hugo --gc --minify` PASS；Hugo 0.157.0；1246 Pages / 0 ERROR / 0 WARNING.

## Publication Evidence

- Published Path: `content/ai-empowerment/agent-engineering-17-skill-engineering.md`
- Published Route: `/ai-empowerment/agent-engineering-17-skill-engineering/`
- Front Matter: PASS; title / slug / date / series metadata match the approved publication request.
- Internal Navigation: PASS; Article 17 links back to Article 16, and Article 16 links forward to Article 17. No Article 18 link was added.
- Semantic Diff: PASS; after removing the Hugo wrapper (front matter and previous-navigation block), published body equals frozen `draft.md` (normalized UTF-8 SHA-256: `72991B1130AA3ACB497B3F978110F40694E96DED75E3BDDEBCB643B21133CD08`).
- Build: `hugo --gc --minify` PASS; Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended`, `1246 Pages / 0 ERROR / 0 WARNING`, exit code `0`.

## Historical Pre-Commit Reconciliation

- Result: `PASS / LAST REPOSITORY WRITE`；Article 17 Lifecycle Candidate=`PUBLISHED`.
- Final / Evidence: `96 / 100 / 0 OPEN`；15 Claims；8 CONFIRMED / 4 PARTIAL / 3 PROPOSAL / 0 BLOCKED；experiment count 0 / Observed Result ABSENT.
- Publication / Build: frozen Draft semantic identity PASS；Article16↔17 navigation PASS；public series index Article17 published；Hugo `1246 Pages / 0 ERROR / 0 WARNING`.
- Future Pointer: `READY / Article 18 / PRECHECK pointer / NOT_STARTED / FORBIDDEN CURRENT RUN / active worker NONE`；this pointer is not Article 18 PRECHECK or Kickoff.
- Git Evidence: Completion Evidence Source=`GIT_HISTORY + REMOTE_REFS`；Expected Completion Message=`Publish Agent Engineering Article 17`；completion SHA is not prewritten.
- Persistence Cut: from this record repository writes=`ZERO`；only Git diff/stage/commit/push/remote and post-commit read-only verification are allowed；then `END_ARTICLE 17 -> STOP`.
