# Article 32｜System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成

- Canonical ID: `32`
- Workspace: `32-dsh-system-prompt-assembly-prompt-context`
- Part: `VI｜DeepSeek Harness`
- Course Weight: `L`
- Optional: `NO`
- Article Type: `SOURCE_TRACE / REQUIRED_EXPERIMENT`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `PASS / 15 CLAIMS / 15 CARDS / 13 CONFIRMED / 2 PROPOSAL / 0 BLOCKED`
- Required Lab: `PROMPT ASSEMBLY / TWO-STEP REQUEST DIFF / NEGATIVE CASES`
- Required Evidence Work: `ORDER + CONFLICT + REQUEST TRACE`
- Mode: `DSH_SOURCE_MODE`
- Current Gate: `GIT_DIFF_VERIFY`
- Active Worker: `NONE`
- Blocker: `NONE`
- Expected Completion Message: `Publish Agent Engineering Article 32`
- Next Transaction Candidate: `Article 33 PRECHECK / REQUIRES ARTICLE 32 END_ARTICLE`

## Publication Verification

- Published Path: `content/ai-empowerment/agent-engineering-32-dsh-system-prompt-assembly-prompt-context.md`
- Body: `EXACT / 44649 BYTES / SHA256 07C08FD844792558A57CB13FFAFED5233F329CFF113B6B0B3F73BE546ACDA154`
- Hugo: `1260 PAGES / 44 STATIC / 1 ALIAS / 0 ERROR`

## Frozen DSH baseline

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Full Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`

## Frozen boundaries

- 必须闭合多个 contributor 到 PromptContext / Provider / request 的真实装配路径。
- 必须取得两个 Step 的 request diff，并保留 duplicate section、bad variable 等负例。
- Stable/Dynamic Context、来源与变换、终结语义必须分层。
- 当前版本没有 compaction 后重注入机制时必须明确标记 absent。
- BuildPilot 仅输出 `IContextContributor + Receipt` 候选方向，不得写成已实现。

## Workspace artifacts

- [Article Card](article-card.md)
- [Research](research.md)
- [Evidence](evidence.md)
- [Repository Map](repository-map.md)
- [Call Path](call-path.md)
- [Prompt Assembly Trace](experiments/prompt-assembly-trace.md)
- [Outline](outline.md)
- [Draft](draft.md)
- [Review](review.md)
- [Subagent Trace](subagent-trace.md)

## Publication Candidate

- Gate: `PUBLISH`
- Result: `PASS / CANDIDATE READY FOR MASTER VERIFICATION`
- Published Path: `content/ai-empowerment/agent-engineering-32-dsh-system-prompt-assembly-prompt-context.md`
- Frozen Body Integrity: `EXACT BYTE-EQUIVALENT / 44649 BYTES / 845 LINES / SHA256 07C08FD844792558A57CB13FFAFED5233F329CFF113B6B0B3F73BE546ACDA154`
- Front Matter: `date 2026-08-30T00:00:00+08:00 / series_order 330 / weight 3330`
- Source Navigation: `Article 31 -> Article 32 UNIQUE NEXT LINK / SERIES INDEX -> Article 32 / Article 33 PLANNED WITHOUT RELREF`
- Static Publication Check: `PASS / FRONTMATTER UNIQUE / ARTICLE 31 NEXT LINK x1 / SERIES INDEX LINK x1 / ARTICLE 33 RELREF x0 / DIFF CHECK CLEAN`
- Hugo Verification: `NOT RUN / PENDING MASTER VERIFICATION`
- Next Allowed Gate: `MASTER_PUBLISH_VERIFY`
