# Article 05｜Function Calling 与 Tool Use：模型如何表达行动意图

- Canonical ID：`05`
- Workspace：`05-function-calling-tool-use`
- Part：`Part II｜从模型到 Agent`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`NORMAL_ARTICLE`
- Lifecycle Status：`PUBLISHED`
- Evidence Status：`PASS`（`6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`）
- Required Lab：`NONE`
- Current Gate：`GIT_DIFF_VERIFY`
- Next Allowed Action：`VERIFY_AND_COMMIT_ARTICLE_05_CHECKPOINT`
- Blocker：`NONE；Provider runtime remains UNVERIFIED by scope`
- Published Content：`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`
- Publisher Result：`PASS`
- Build Result：`PASS`；Hugo `0.157.0`；`1234 Pages / 0 ERROR / 0 WARNING`；process `exit 0`

## Canonical responsibility

本篇是 Part II 的 Agent 能力起点篇。它只负责区分“模型提出 Tool Call intent”与“Host 实际决定、执行并回注 Tool Result”，建立 Function Calling、Tool Schema、Tool Choice、Call ID、Arguments 与 Tool Result 的最低机制边界。

本篇不讲权限、超时、MCP Transport 或多 Step；不把一次 Tool Use 写成完整 Agent Loop，也不提前吞掉 Article 06 的 Tool Runtime。

## Preconditions

- Article 03 `PUBLISHED`：Structured Output / Parse / Schema / DTO / Domain boundary 已建立。
- Article 04 `PUBLISHED`：Provider Adapter / Streaming / Error / Retry boundary 已建立；checkpoint `ac10060b82d21534a014d7a4bef3b3e03f7bd475` verified。
- Part I Audit `PASS`：checkpoint `b7fafc5f2e490a5d6590da1cfb54d9f2ced5968c` verified；`PI-F01`—`PI-F03` 保持 `OPEN MINOR`，不阻断 Article 05。
- Article 05 PRECHECK / ARTICLE_KICKOFF / WORKSPACE_INIT：`PASS`。

## Production assets

- [Article Card](article-card.md)：canonical 定位、问题、边界、示例职责与 Evidence requirement 的机械实例化。
- [Research](research.md)：`COMPLETE`；已核对 OpenAI Responses 与 Anthropic Messages current official contracts，并保存 Provider scope、counter-evidence、fixture / trace decision 与 stop lines。
- [Evidence](evidence.md)：`PASS`；`6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`；Master 已验证 8 Claim / 8 Cards、必需字段、source scope、fixture / trace 标签与 0 core BLOCKED。
- [Review](review.md)：`PASS / 95`；fresh first formal review，`0 BLOCKER / 0 MAJOR / 0 MINOR / 0 EDITORIAL`，Lifecycle transition `REVIEW -> FINAL` 已由 Master 核对。
- [Outline](outline.md)：`PASS`；`8 / 8` Claims mapped，`new core facts=0`，`RETURN_TO_RESEARCH=NONE`。
- [Draft](draft.md)：`FINAL`；唯一 H1、无 frontmatter，C01—C08 semantic coverage `8 / 8`，外链 `7 / 7` 属于 Evidence source whitelist；Provider calls / Tool execution 仍为 `NONE / NONE`，runtime 仍为 `UNVERIFIED`。
- Published Content：`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`；Publisher / Build=`PASS`。

## Publication Evidence

- Published Path：`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`
- Published Route：`/ai-empowerment/agent-engineering-05-function-calling-tool-use/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-05-function-calling-tool-use/`
- Front Matter：`PASS`；Hugo production build 已实际解析 YAML；title 与 canonical exact match；slug=`agent-engineering-05-function-calling-tool-use`；日期=`2026-08-20T00:00:00+08:00`，在本地构建时间 `2026-08-20 06:01 +08:00` 前且不是 future content；`draft: false`；四个 tags、`series: Agent Engineering`、`primary_series: agent-engineering`、`series_role: article`、`series_order: 60` 与 `weight: 3060` 均符合 canonical / series convention。
- Series / Internal Links：`PASS`；Article 05 顶部“上一篇”只指向 Article 04，Article 04 只把既有“下一篇”二字包成 Article 05 `relref`，Article 05 未创建 Article 06 `relref`。source 与 rendered HTML 中 05 → 04、04 → 05 各一次，05 → 06 为 `0`；shortcode 参数均使用 ASCII 双引号。自动 series directory 按 `series_order` 渲染 00、01、02、03、04、05。
- Semantic Diff：`PASS`；移除 FINAL Draft 唯一 H1，并排除 Published front matter 与顶部 previous navigation 后，Published knowledge body 与 FINAL Draft 逐行、逐字符 exact match：`231` 行、`10,362` characters、UTF-8 `13,880` bytes；知识内容变化=`0`。
- Markdown / External Links：`PASS`；code fence markers=`20` 且成对；FINAL Draft 与 Published Content 的 `7` 个 external URLs 数量、顺序和字符串 exact match。
- Build Command：`hugo --gc --minify`
- Build Evidence：首次 sandboxed 启动因 `hugo.exe` `拒绝访问`，没有产生有效 build result；按权限流程以同一命令重跑后，Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；`1234 Pages`；`0 WARNING`；`0 ERROR`；process `exit 0`；Hugo total `5947 ms`，command wrapper `6333 ms`。
- Final Reverify：Publication Evidence 落盘后再次运行同一 `hugo --gc --minify`；Hugo `0.157.0`；`1234 Pages / 0 WARNING / 0 ERROR`；process `exit 0`；Hugo total `5902 ms`，command wrapper `6277 ms`。
- Route / Series Render：`PASS`；Article 05 route、Article 05 → 04 previous href、Article 04 → 05 next href、Article 05 canonical title、最短结论与 series directory 00 → 05 顺序均已在 `public/` 产物中定位；Article 05 → 06 href=`0`。
- Files Written：`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`；`content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`（仅既有“下一篇”的 Article 05 `relref`）；`docs/agent-engineering-course/articles/05-function-calling-tool-use/README.md`（仅 Publisher / Build evidence）。
- Recommended Transition：`PUBLISHED candidate only`；只允许 Master 进入 state reconciliation / Git Diff Verify，Publisher 不直接写 `PUBLISHED`。
- Recommended Status Changes：由 Master 在核对 Reviewer Final PASS、Publisher PASS、Build PASS 与 repository consistency 后，将 Article 05 Lifecycle / global status / run state 对齐为真实状态；Publisher 未写 global durable state。
- Canonical Update Candidate：canonical Article 05 当前 plain-title entry 可由 Master 在 repository reconciliation 后链接到 Published Path；Publisher 未应用 canonical 修改。
- Checkpoint Readiness：`READY_FOR_MASTER_STATE_UPDATE_AND_GIT_DIFF_VERIFY`；尚未执行 Master reconciliation、独立 Article 05 checkpoint commit 或 commit verification。
- Publisher Boundary：Lifecycle 保持 `FINAL`；Publisher 未修改 canonical、glossary、`status.md`、`course-run-state.md`、course README、Research、Evidence、Outline、Draft、Review、Article 06、Lab、theme 或 CI，未 stage / commit / push / 创建 PR。
- Master State Reconciliation：`PASS`；Article 05 workspace lifecycle、course status、run state、canonical publication link、Published Content、Article 04 navigation、Reviewer Final PASS 与 Publisher / Build evidence 已由 Master 对齐；post-reconciliation `hugo --gc --minify` 再验证为 `1234 Pages / 0 WARNING / 0 ERROR / exit 0 / 5897 ms`。Lifecycle=`PUBLISHED` 只是 checkpoint candidate，独立 Article 05 commit verification 前不得启动 Article 06。

## Research / Evidence boundary

- Provider Calls / Tool Execution：`NONE / NONE`。
- Provider Runtime Evidence：`UNVERIFIED`。
- Tool Schema Fixture：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`；只说明 model-visible contract 差异，不证明选择或参数质量提升。
- Message Traces：OpenAI Responses 与 Anthropic Messages `OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`。
- `05—06` hard rule：本篇只用 official examples 闭合 Function Calling documented roundtrip；Article 06 的 Tool Runtime failure-injection 仍需独立真实证据。

## WORKSPACE_INIT evidence

- Canonical title / Part / Weight / Optional：已从 `docs/agent-engineering-series-plan.md` 机械读取。
- Detailed responsibility / examples / boundaries：已从 `docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 05 frozen section 机械实例化。
- Workspace slug：`05-function-calling-tool-use`，符合 repository naming convention。
- Initial files：仅 `README.md`、`article-card.md`、`research.md`、`evidence.md`、`review.md`。
- Article 05 workspace / Published Content 在 PRECHECK 前均不存在；启动时 Git baseline 为 `b7fafc5f2e490a5d6590da1cfb54d9f2ced5968c`，worktree clean。

## Stop Line

Reviewer、Publisher、Build 与 Master State Reconciliation 均为 `PASS`，Lifecycle=`PUBLISHED`。当前只允许 Master 执行 GIT_DIFF_VERIFY、Article 05 checkpoint commit 与 commit verification；本篇不得把 official example / synthetic fixture 写成 Provider runtime observation；不得启动 Article 06。
