# Article 13｜Context Debugging：Packing、Compression、Pollution 与可重建性

- Canonical ID: `13`
- Workspace: `13-context-debugging`
- Part: `III｜Agent 的信息、状态与知识`
- Course Weight: `L`
- Optional: `NO`
- Lifecycle Status: `PUBLISHED / PRE_COMMIT_RECONCILIATION PASS`
- Evidence Status: `PASS / 9 of 9 TRACEABLE / 3 CONFIRMED / 6 PROPOSAL / 0 BLOCKED`
- Required Lab: `Lab 05 Context Debugging / EVIDENCE_MERGED / EVIDENCE_GATE_PASS / FIXTURE-SCOPED`
- Mode: `LAB_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION PASS / GIT_DIFF_VERIFY NEXT`
- Active Worker: `NONE`
- Next Allowed Action: `GIT_DIFF_VERIFY`
- Blocker: `NONE`

## Dependencies

- Article 02：Prompt 的任务合同与边界。
- Article 08：Step、Observation 与 authoritative State。
- Article 10：State revision、合法推进与 stale suggestion。
- Article 11：Checkpoint、Recovery 与重建边界。
- Article 12：effective Context、application-visible Context Snapshot、Receipt、Select / Order / Scope / Fit Budget。

## Current Scope

本篇只回答：当一个 Step 看见了错误、过期、冲突、污染、压缩或截断后的 Context 时，怎样定位失真层、保留 UNKNOWN，并用 Snapshot / Receipt 与固定 fixture 做可重复比较。Article 12 的装配基础不从零重讲；Article 14 的 Working Memory lifecycle、Article 15—16 的长期 Memory / RAG，以及 Provider 内部未披露变换均不在本篇展开。

## Transaction Record

- PRECHECK：`PASS`；`main`、clean worktree、local `HEAD`、`origin/main` 与 live remote main 均对齐 `57597c974de62c0d2cd04a3a6cc30b49380e43da`；Article 12 唯一 completion commit `a87f058ae2642870ade75fa7f23ac4396f17b94c` 已验证；Article 13 workspace / Published Content / Lab 05 在 kickoff 前均 absent；active worker=`NONE`。
- ARTICLE_KICKOFF：`PASS`；Master `/root` 于 `2026-08-22T11:22:22+08:00` 取得唯一 Article 13 transaction ownership；未创建 Article 14 artifact。
- WORKSPACE_INIT：`PASS`；Master 只创建五个 `PLANNED` content skeleton；`subagent-trace.md` 是 transaction record，不是 Research 或 Article content；`outline.md / draft.md / Lab 05` 仍不存在。
- Production Gates：Research / Preliminary Evidence、Lab 05 Design / Execute / Observation / Evidence Merge、Evidence Gate、Outline、Draft、两轮Revision / Recheck、Final Gate Cycle 2、Publisher与Build Verify均=`PASS`；F01—F05=`CLOSED`，Review=`91 / 100`，Hugo=`1242 Pages / 0 WARNING / 0 ERROR`。
- PRE_COMMIT_RECONCILIATION：`PASS / LAST REPOSITORY WRITE`；Published Content、Article 12↔13导航、Course Index、canonical、Lab index、status、course README与Factory pointer已对齐。Article 14 workspace/content=`ABSENT`，pointer candidate=`READY / Article 14 / PRECHECK / NOT_STARTED`，不等于Article 14 Kickoff。此记录后repository writes=`ZERO`，只允许Git diff/stage/commit/push/remote只读流程。

## Stop Line

Article 13 publication candidate与Lab 05 evidence已通过全部Gate和Master reconciliation；completion commit、push与remote verification仍由后续runtime Gate证明，不在本README预写。Article 14只保留`PRECHECK / NOT_STARTED` pointer，workspace/content不存在且本transaction不启动Article 14。

## Publication Result

- Result：`PASS / PUBLISH CANDIDATE`；Gate=`PUBLISH`；execution=`REAL_SUBAGENT`；next allowed gate=`BUILD_VERIFY`。
- Published Path：`content/ai-empowerment/agent-engineering-13-context-debugging.md`；Published Route candidate=`/ai-empowerment/agent-engineering-13-context-debugging/`。
- Front Matter Result：`PASS`；title / slug / date / draft / tags / series / primary_series / series_role / series_order=`140` / weight=`3140` 与 Publisher task brief 一致，字符串与 shortcode 均使用 ASCII double quotes。
- Series Result：`PASS`；Article 12 仅新增一条 Article 13 下一篇导航；Course Index 将 Article 13 改为已发布并链接正文、Lab 05 改为已验证并链接 Article 13；Article 14 保持计划中且无链接。
- Internal Link Result：`PASS`；Article 13 只有一条 Article 12 上一篇导航、无 Article 14 下一篇导航；Index 中 Article 13 relref=`2`；Article 14 relref=`0`。
- Semantic Diff Result：`PASS / EXACT`；方法为 LF 归一后，从 frozen Draft 删除唯一 H1 与其后空行，再与 Published Content 删除 front matter、唯一上一篇导航及其后空行后的 knowledge body 逐字符比较。digest 计算明确排除 semantic body 的单一 terminal LF，再对 UTF-8 bytes 计算 SHA-256；expected / actual SHA-256 均为 `2B54738DA14B5707518DD9F4A8BA40FCB25EAB41B587209178B9940C5CEB7EBD`。
- Static Checks：`PASS`；published H1=`0`、fence markers=`16 / paired`、TODO / DATA-TODO / EXPERIENCE-TODO=`0`、trailing whitespace=`0`。
- Build Commands：`hugo --gc --minify`（repository root）。
- Build Result：`PASS`；Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；process exit code=`0`；`1242 Pages / 0 Paginator / 0 Non-page / 44 Static / 0 Processed images / 1 Aliases / 0 Cleaned`；Hugo total=`6166 ms`；Warnings=`0`；Errors=`0`。
- Files Written：`content/ai-empowerment/agent-engineering-13-context-debugging.md`（新建）；`content/ai-empowerment/agent-engineering-12-context-engineering.md`（仅新增 Article 13 下一篇导航）；`content/ai-empowerment/agent-engineering-series-index.md`（仅更新 Article 13 与 Lab 05 发布映射）；本 README（仅追加 Publication Result）。
- Recommended Article Transition：`GIT_DIFF_VERIFY`；Build Verify与Master PRE_COMMIT_RECONCILIATION均=`PASS`。
- Canonical Update Candidate：`APPLIED / MASTER VERIFIED`；Article 13 link与Lab 05 fixture-scoped status已对齐。
- Checkpoint Readiness：`READY / COMPLETION COMMIT PENDING`；后续commit SHA由Git history提供，push / remote verification保持runtime-only，本文档不预写`END_ARTICLE`。
