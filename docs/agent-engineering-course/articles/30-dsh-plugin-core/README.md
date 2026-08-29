# Article 30｜Everything is a Plugin：插件内核如何承载 Capability 与生命周期

- Canonical ID: `30`
- Workspace: `30-dsh-plugin-core`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `M`
- Optional: `NO`
- Article Type: `SOURCE_TRACE / LIFECYCLE`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `12 CONFIRMED / 3 PROPOSAL / 0 PARTIAL / 0 BLOCKED`
- Required Lab: `NONE`
- Required Evidence Work: `ONE REAL PLUGIN INSTALL -> REGISTER -> OPERATE -> DISPOSE TRACE`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `GIT_DIFF_VERIFY`
- Active Worker: `NONE`
- Blocker: `NONE`
- Expected Completion Message: `Publish Agent Engineering Article 30`
- Next Transaction Candidate: `Article 31 PRECHECK / REQUIRES ARTICLE 30 END_ARTICLE`

## Frozen DSH baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Full Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`

## Frozen boundaries

- 只追踪一个代表性真实插件，不遍历全部插件。
- Plugin Context、Model Context、Plugin Event、Session Event、Plugin 与 Tool 不得混同。
- 必须闭合 install、register service/event/effect、scope operate、dispose reversible effects。
- BuildPilot 默认 `SIMPLIFY` 为显式接口；不提前照搬 Everything-is-a-plugin。

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Repository Map](repository-map.md)
- [Call Path](call-path.md)
- [Lifecycle Trace](experiments/plugin-lifecycle-trace.md)
- [Outline](outline.md)
- [Draft](draft.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

## Publication Result

- Gate: `PUBLISH`
- Result: `PASS`
- Published Path: `content/ai-empowerment/agent-engineering-30-dsh-plugin-core.md`
- Frozen Body Integrity: `EXACT BYTE-EQUIVALENT / 36845 BYTES / 543 LINES / SHA256 6D7AC498159453327BA4D4383850B4F59DAC16262D61E34B30D3CF4C39C9242F`
- Front Matter: `date 2026-08-30T00:00:00+08:00 / series_order 310 / weight 3310`
- Source Navigation: `Article 29 -> Article 30 TOP + BOTTOM / SERIES INDEX -> Article 30 / Article 31 PLANNED WITHOUT RELREF`
- Hugo Verification: `PASS / hugo v0.157.0 extended / 1258 pages / 44 static files / 1 alias / 0 warnings / 0 errors`
- Rendered Navigation: `PASS / Article 29 -> 30 x2 / Article 30 -> 29 x1 / Article 30 -> course index x1 / series index -> 30 x1 / Article 31 link x0`
- Tracked Build Output: `NONE`
- Next Allowed Gate: `MASTER_STATE_UPDATE`
