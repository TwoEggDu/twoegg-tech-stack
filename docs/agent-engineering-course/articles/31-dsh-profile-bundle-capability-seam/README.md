# Article 31｜Profile、Bundle、Provider 与 Capability Seam

- Canonical ID: `31`
- Workspace: `31-dsh-profile-bundle-capability-seam`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `M`
- Optional: `NO`
- Article Type: `SOURCE_TRACE / CONFIGURATION`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `PASS / 15 CLAIMS / 15 CARDS / 12 CONFIRMED / 3 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Required Evidence Work: `CONFIG SCHEMA + LOAD PATH + TWO EFFECTIVE CONFIG DUMPS`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `GIT_DIFF_VERIFY`
- Active Worker: `NONE`
- Blocker: `NONE`
- Expected Completion Message: `Publish Agent Engineering Article 31`
- Next Transaction Candidate: `Article 32 PRECHECK / REQUIRES ARTICLE 31 END_ARTICLE`

## Frozen DSH baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Full Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`

## Frozen boundaries

- 必须验证 Schema、加载/覆盖顺序、冲突与两个 Effective Config dump/diff。
- 至少闭合一个 Model、FS 或 Sandbox Provider 到 Consumer 的 Capability Seam。
- Web / Headless 共享核心与 Host 差异必须由实际 composition 证明。
- BuildPilot 只吸收能力集与只读 Profile；复杂叠层与多 Host 默认 `DEFER`。

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Repository Map](repository-map.md)
- [Call Path](call-path.md)
- [Effective Config Diff](experiments/effective-config-diff.md)
- [Outline](outline.md)
- [Draft](draft.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

## Publication Candidate

- Gate: `PUBLISH`
- Result: `PASS / CANDIDATE READY FOR MASTER VERIFICATION`
- Published Path: `content/ai-empowerment/agent-engineering-31-dsh-profile-bundle-capability-seam.md`
- Frozen Body Integrity: `EXACT BYTE-EQUIVALENT / 39021 BYTES / 674 LINES / SHA256 C70510DFB0B8DE33D0AD58518E2E29ED7CACA2F08B842EEA8695A27AF547BA8D`
- Front Matter: `date 2026-08-30T00:00:00+08:00 / series_order 320 / weight 3320`
- Source Navigation: `Article 30 -> Article 31 UNIQUE NEXT LINK / SERIES INDEX -> Article 31 / Article 32 PLANNED WITHOUT RELREF`
- Static Publication Check: `PASS / FRONTMATTER UNIQUE / ARTICLE 30 NEXT LINK x1 / SERIES INDEX LINK x1 / ARTICLE 32 RELREF x0 / DIFF CHECK CLEAN`
- Hugo Verification: `PASS / 1259 PAGES / 44 STATIC / 1 ALIAS / 0 ERROR`
- Next Allowed Gate: `GIT_DIFF_VERIFY`
