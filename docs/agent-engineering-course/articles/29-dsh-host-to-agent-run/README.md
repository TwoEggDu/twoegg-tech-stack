# Article 29｜DeepSeek Harness 总图：从 Host 启动到一次 Agent Run

- Canonical ID: `29`
- Workspace: `29-dsh-host-to-agent-run`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `M`
- Optional: `NO`
- Article Type: `ARCHITECTURE_MAP / SOURCE_TRACE`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `12 CONFIRMED / 3 PROPOSAL / 0 PARTIAL / 0 BLOCKED`
- Required Lab: `NONE`
- Required Evidence Work: `HOST_TO_AGENT_RUN SOURCE PATH + BOUNDED TRACE`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `GIT_DIFF_VERIFY`
- Active Worker: `NONE`
- Blocker: `NONE`
- Expected Completion Message: `Publish Agent Engineering Article 29`
- Next Transaction Candidate: `Article 30 PRECHECK / REQUIRES ARTICLE 29 END_ARTICLE`

## Frozen DSH baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Full Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Selected At: `2026-08-29`
- External Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Fixture Policy: `EXTERNAL / NOT VENDORED / NOT COMMITTED`

## Frozen boundaries

- 本篇必须闭合至少一条 Host/profile 到 Agent Run 的静态 source path；目录名不能补路径。
- Runtime Trace 能安全取得则合并；凭证或环境阻断时保留 raw failure 和 runtime gap。
- 总图只路由 Article 30—37，不替专题文章提前证明生命周期、事件、Tool、Replay 或 Recovery。
- BuildPilot 只接收 `ADOPT / SIMPLIFY / REJECT / DEFER` 输入；Article 38—44 保持零资产。

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Repository Map](repository-map.md)
- [Call Path](call-path.md)
- [Runtime Trace](experiments/host-agent-run-trace.md)
- [Outline](outline.md)
- [Draft](draft.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

## Publication Result

- Published Path: `content/ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md`
- Front Matter: `PASS / date 2026-08-30T00:00:00+08:00 / series_order 300 / weight 3300`
- Frozen Body: `PASS / H1-to-EOF exact byte-equivalent to draft.md`
- Frozen Body SHA-256: `0B6D75F81EAEC814C235B0278033227583FB2F5915996052AD713FBE73A882D7`
- Frozen Body Size: `36017 bytes / 450 content lines / terminal LF preserved`
- Navigation: `PASS / Article 28 top+bottom -> 29 / Article 29 -> 28 / Article 30 has no future relref`
- Series Index: `PASS / Article 29 published link / Article 30 planned without link`
- Hugo Validation: `PASS / hugo --gc --minify / 1257 pages / 44 static files / 1 alias / 0 build errors`
- Rendered Validation: `PASS / Article 28 -> 29 twice / Article 29 -> 28 once / canonical series index shows 29 published and 30 planned`
- Tracked Build Output: `NONE`
