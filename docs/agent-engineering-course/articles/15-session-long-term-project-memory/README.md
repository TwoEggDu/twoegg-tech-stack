# Article 15｜Session、Long-term Memory 与 Project Memory：事实、经验和作用域

- Canonical ID: `15`
- Workspace: `15-session-long-term-project-memory`
- Part: `III｜Agent 的信息、状态与知识`
- Course Weight: `M`
- Optional: `NO`
- Lifecycle Status: `PUBLISHED / PRE_COMMIT_RECONCILIATION PASS`
- Evidence Status: `PASS / 7 CONFIRMED / 1 PARTIAL / 6 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Mode: `NORMAL_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION / COMPLETION CANDIDATE`
- Active Worker: `NONE`
- Next Allowed Action: `GIT_DIFF_VERIFY -> COMMIT -> PUSH -> REMOTE_VERIFY -> READ_ONLY RECONCILIATION`
- Blocker: `NONE`
- Completion Evidence Source: `GIT_HISTORY`
- Pre-Commit Candidate: `PUBLISHED`
- Completion Commit: `resolved from Git history by Resume / PRECHECK`
- Expected Completion Message: `Publish Agent Engineering Article 15`
- Next Transaction Pointer: `Article 16 PRECHECK candidate / NOT_STARTED / FORBIDDEN IN CURRENT RUN`

## Transaction Baseline

- PRECHECK：`PASS`；`main` clean，`HEAD == origin/main == live remote main == 95372e8917a2e4350d356c7ea0a3c91d14e46da3`。
- Article 14：唯一 completion commit `a53d151ba051403ff5ef369e5c3860a9fbded03d` 已在当前 main ancestry，Published Content、Final Gate、Build 与 `END_ARTICLE` 均已核验。
- Article 15：canonical=`Part III / M / non-optional / NORMAL_ARTICLE / Required Lab NONE`；PRECHECK 前 workspace 与 Published Content 均不存在。
- Article 16：workspace 与 Published Content 均不存在；当前 bounded run 禁止启动。
- Human Resume：已收到新的外部 `RESUME CONTINUOUS CANARY / START ARTICLE 15`，授权范围仅 Article 15 transaction。

## Production Assets

- [Article Card](article-card.md)：`READY FROM CANONICAL + HUMAN RESUME BRIEF`
- [Research](research.md)：`COMPLETE`
- [Evidence](evidence.md)：`PASS / 14 OF 14 TRACEABLE / 0 BLOCKED`
- [Outline](outline.md)：`PASS / 14 OF 14 CLAIMS`
- [Draft](draft.md)：`PASS / 342 LINES / 14 OF 14 CLAIMS`
- [Review](review.md)：`FINAL GATE PASS / 93 / 0 OPEN / 14 OF 14 TRACEABLE`
- [Subagent Trace](subagent-trace.md)：Article 15全部content Gate与Pre-Commit Reconciliation已记录

## Stop Line

`PRE_COMMIT_RECONCILIATION` 已通过并激活 persistence cut。此后 repository writes=`ZERO`；只允许Git diff/stage/唯一completion commit/单次push/remote与post-commit只读验证。`END_ARTICLE 15` 后立即停止Continuous Run；Article 16仅为PRECHECK pointer candidate，禁止执行PRECHECK或ARTICLE_KICKOFF。

## Publication Evidence Candidate

- Result：`PASS / PUBLISH CANDIDATE`；Gate=`PUBLISH`；execution=`REAL_SUBAGENT`；next allowed gate=`BUILD_VERIFY`。
- Frozen Input：`draft.md` SHA-256=`0fe407d1a04839a8af8729cb5aa2931682bef21aeb654852c1968f246cff111c` / `25565 bytes` / `342 lines`，与独立 Final Gate 授权身份精确一致；Publisher 未修改 Draft。
- Published Path：`content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md`；Published Route candidate=`/ai-empowerment/agent-engineering-15-session-long-term-project-memory/`。
- Front Matter Result：`PASS / STATIC`；title / slug / date=`2026-08-23T00:00:00+08:00` / draft=`false` / tags / series / primary_series / series_role / series_order=`160` / weight=`3160` 符合 task brief；YAML 字符串与 shortcode 均使用 ASCII double quotes。
- Series Result：`PASS / STATIC`；Course Index 仅把 Article 15 改为已发布并链接正文；Article 16 继续保持计划中且无链接。
- Internal Link Result：`PASS / STATIC`；Article 15 仅新增 Article 14 上一篇导航，Article 14 仅新增 Article 15 下一篇导航；frozen Draft 原有 Article 11—14 relref 全部保留；future Article relref=`0`。
- Semantic Diff Result：`PASS / EXACT`；LF 归一后，Published Content 删除 frontmatter、唯一上一篇导航及其后空行后的 semantic body，与 frozen Draft 逐字符相等；排除 semantic body 单一 terminal LF 后 expected / actual UTF-8 SHA-256 均为 `7411bde1cb7690daa6e8a97e9f8765edb852c6c664e8c325c04db87618a539b2`。
- Static Checks：`PASS`；published lines=`362`；fence markers=`18 / paired`；Article 14 next-link count=`1`；Index Article 15 relref count=`1`；Article 16 relref count=`0`；Draft hash unchanged。
- Build Commands：`NOT EXECUTED IN PUBLISH GATE`；独立 `BUILD_VERIFY` 应在 repository root 运行真实 Hugo command。
- Build Result：`NOT_YET_EXECUTED / NOT PASS`；Warnings=`NOT ASSESSED`；Errors=`NOT ASSESSED`。
- Files Written：`content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md`（新建）；`content/ai-empowerment/agent-engineering-14-working-memory-investigation-state.md`（仅新增 Article 15 下一篇导航）；`content/ai-empowerment/agent-engineering-series-index.md`（仅更新 Article 15 发布映射）；本 README（仅追加 Publication Evidence Candidate）。
- Recommended Article Transition：`BUILD_VERIFY`；Publisher 不自行宣布 Article 15 `PUBLISHED`。
- Recommended Status Changes：`PUBLISHED candidate only`；仅在 Reviewer Final PASS、Publisher PASS、独立 Build PASS 与 repository consistency 全部由 Master 验证后，才由 Master 更新 lifecycle 与 global durable state。
- Canonical Update Candidate：canonical Article 15 plain-title row 可由 Master 在后续 reconciliation 中链接到 `../content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md`；Publisher 未修改 canonical。
- Checkpoint Readiness：Completion Evidence Source=`GIT_HISTORY`；Pre-Commit Candidate=`PUBLISHED`；Completion Commit=`resolved from Git history by Resume / PRECHECK`；Expected Completion Message=`Publish Agent Engineering Article 15`；Next Transaction Pointer=`Article 16 PRECHECK candidate / NOT_STARTED / FORBIDDEN CURRENT RUN`。
- Publisher Boundary：未修改 frozen Draft / Review / Research / Evidence、canonical、global status / run-state、Article 16、theme 或 CI；未运行 Hugo，未执行 Git branch / stage / commit / push，也未创建 PR。

## Build Verification Result

- Result：`PASS`；fresh Publisher execution=`/root/article15_build_verify`；Gate=`BUILD_VERIFY`；next=`PRE_COMMIT_RECONCILIATION`。
- Worker / Master Build：`hugo --gc --minify`；Hugo=`0.157.0`；`1244 Pages / 0 WARNING / 0 ERROR / 0 REF_NOT_FOUND`；exit code=`0`。
- Future Check：`hugo list future`仅表头；Article 15 future hits=`0`；date唯一且为`2026-08-23T00:00:00+08:00`。
- Rendered Routes：Article 15 route存在；Article14→15=`1`；Article15→14=`2`（上一篇导航与正文边界引用）；Series Index→15=`1`。
- Forbidden Boundary：Article 16 route、rendered link、workspace与Published Content均不存在。
- Source Integrity：pre/post tracked status相同，9项source hash不变；Draft hash与semantic exact持续成立；`git diff --check`通过。

## Pre-Commit Reconciliation

- Result：`PASS / LAST REPOSITORY WRITE`；Article 15 Lifecycle=`PUBLISHED / PRE_COMMIT_RECONCILIATION PASS` completion candidate。
- Final / Evidence：`93 / 100 / 0 OPEN`；14/14 Claim traceable；`7 CONFIRMED / 1 PARTIAL / 6 PROPOSAL / 0 BLOCKED`。
- Publication / Build：published semantic body与frozen Draft精确一致；Hugo=`1244 Pages / 0 WARNING / 0 ERROR`；route/navigation/future-date checks=`PASS`。
- Canonical / Global：canonical Article 15已链接Published Content；status、course README与run-state对齐。
- Future Pointer：`READY / Article 16 / PRECHECK pointer candidate / NOT_STARTED / FORBIDDEN CURRENT RUN / active worker NONE`；`continuous_run.enabled=false`；该pointer不等于Article 16 PRECHECK执行或Kickoff。
- Git Evidence：Completion Evidence Source=`GIT_HISTORY`；Expected Completion Message=`Publish Agent Engineering Article 15`；completion SHA不在pre-commit文件中预写。
- Persistence Cut：从本记录起repository writes=`ZERO`；只允许Git diff/stage/commit/push/remote与post-commit只读验证，随后`END_ARTICLE 15 -> STOP`。
