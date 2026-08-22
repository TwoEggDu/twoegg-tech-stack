# Article 04｜Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异

- Canonical ID：`04`
- Canonical Title：`Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异`
- Workspace：`04-model-adapter-llm-gateway`
- Part：`Part I｜从 LLM 到可编程模型`
- Course Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Lifecycle Status：`PUBLISHED`
- Evidence Status：`PASS`（`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`）
- Required Lab：`NONE`
- Lab Status：`N/A`
- Current Gate：`END_ARTICLE / CHECKPOINT_VERIFIED`
- Next Allowed Action：`NONE / GLOBAL_POINTER_OWNS_CURRENT_COURSE_TRANSACTION`
- Blocker：`NONE`
- Review Findings：`04-F01 CLOSED`；Unclosed Findings=`NONE`
- Published Content：`content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`
- Publisher Result：`PASS`
- Build Result：`PASS`；Hugo `0.157.0`；`1233 Pages / 0 ERROR / 0 WARNING`；process `exit 0`

## 本篇职责

按照 canonical，本篇负责研究怎样把单一 Provider API 调用收进可替换、可观察的 Model Capability，并建立 Adapter、Gateway、Streaming、Error、Retry 与 Provider Capability 的责任边界。

本篇不预设研究结论，不搭建 API Gateway 服务，不讲负载均衡或模型部署，也不提前实现后续 Agent Runtime。

## 生产资产

- [Article Card](article-card.md)：canonical 定位、依赖、边界与证据要求。
- [Research](research.md)：`COMPLETE`；7 个 Research Questions 均已回答，Provider Calls `NONE`，Runtime `UNVERIFIED`。
- [Evidence](evidence.md)：`PASS`；8 个 Claims / 8 个 Evidence Cards，`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`。
- [Outline](outline.md)：`PASS`；8/8 Claims mapped，new core facts=`0`，`RETURN_TO_RESEARCH=NONE`。
- [Draft](draft.md)：`FINAL`；8/8 Claims covered，new core facts=`0`，`RETURN_TO_RESEARCH=NONE`；Published body 已机械映射。
- [Review](review.md)：首轮=`REVISION_REQUIRED / 91`；Cycle 1 fresh recheck 关闭 `04-F01`，Final Gate=`PASS / 93`。

ARTICLE_KICKOFF、WORKSPACE_INIT、RESEARCH、EVIDENCE_GATE、OUTLINE、AUTHOR_DRAFT、REVIEW_RECHECK、PUBLISH、BUILD_VERIFY 与 MASTER_STATE_UPDATE 均已通过。Reviewer Final Gate=`PASS / 93`；Publisher 与 Build=`PASS`；Master 已核对 workspace、Published Content、navigation、canonical candidate 与 global state，Lifecycle 进入 `PUBLISHED` checkpoint candidate。Article 04 transaction 仍需 GIT_DIFF_VERIFY、独立 checkpoint commit 与 commit verification 才算完成。

## Publication Evidence

- Published Path：`content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`
- Published Route：`/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway/`
- Front Matter：`PASS`；Hugo production build 已实际解析 YAML；title 与 canonical exact match；slug、`draft: false`、四个正文机械 tags、series、`primary_series`、`series_role: article`、`series_order: 50` 与 `weight: 3050` 均符合冻结 metadata。日期使用同一上海日历日的显式 offset：`2026-08-20T00:00:00+08:00`。
- Series / Internal Links：`PASS`；Article 04 Draft 自带的顶部“上一篇”原样指向 Article 03，Article 03 结尾既有“下一篇 Model Adapter / Gateway”桥接机械加入 Article 04 `relref`；source 与 rendered HTML 中 04 → 03、03 → 04 各一次，shortcode 均使用 ASCII 双引号。Article 04 没有 Article 05 `relref`；自动 series directory 按 `series_order` 渲染 00、01、02、03、04。
- Semantic Diff：`PASS`；移除 FINAL Draft 唯一 H1 与 Published front matter 后，Published knowledge body 与 FINAL Draft 逐字符 exact match：`276` 行、`16,368` characters；双方 UTF-8 SHA-256 均为 `e1b823b37899a02d47929cedb30d0ace7ed1fdc38009d8744c87cea385871264`；知识内容变化=`0`。
- Markdown / External Links：`PASS`；code fence markers=`10` 且成对；FINAL Draft 与 Published Content 的 `13` 个 external URLs 数量、顺序和字符串 exact match。Azure `AI Gateway tier (preview)`、all-tier / service-tier labels 与“不同 scope 不能彼此补成产品全集”的 `04-C07` guard 均原样保留。
- Build Command：`hugo --gc --minify`
- Build Evidence：首次 sandboxed 启动因 `hugo.exe` `拒绝访问`，没有产生有效 build result；按权限流程以同一命令重跑后，Hugo `v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；`1233 Pages`；`0 WARNING`；`0 ERROR`；process `exit 0`；Hugo total `5872 ms`，command wrapper `6021 ms`。
- Final Reverify：Publication Evidence 落盘后再次运行 `hugo --gc --minify`；Hugo `0.157.0`；`1233 Pages / 0 WARNING / 0 ERROR`；process `exit 0`；Hugo total `5880 ms`，command wrapper `6184 ms`。
- Route / Series Render：`PASS`；Article 04 route、Article 04 → 03 previous href、Article 03 → 04 next href、Article 04 canonical title、Azure scope labels、C07 guard 与 series directory 00 → 04 顺序均已在 `public/` 产物中定位。
- Files Written：`content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`；`content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md`（仅一个结尾桥接 `relref`）；`docs/agent-engineering-course/articles/04-model-adapter-llm-gateway/README.md`（仅 publication evidence / Publisher / Build result）。
- Recommended Transition：`PUBLISHED candidate only`；只允许 Master 进入 state reconciliation / Git Diff Verify，Publisher 不直接写 `PUBLISHED`。
- Canonical Update Candidate：canonical Article 04 当前 plain-title entry 可由 Master 在 repository reconciliation 后链接到 Published Path；Publisher 未应用 canonical 修改。
- Checkpoint Readiness：`READY_FOR_MASTER_STATE_UPDATE_AND_GIT_DIFF_VERIFY`；尚未执行 Master reconciliation、独立 Article 04 checkpoint commit 或 commit verification。
- Publisher Boundary：Lifecycle 保持 `FINAL`；Publisher 未修改 canonical、`status.md`、`course-run-state.md`、course README、Research、Evidence、Outline、Draft、Review、Article 05、theme 或 CI，未 stage / commit / push / 创建 PR。
- Master State Reconciliation：`PASS`；Article 04 workspace lifecycle、course status、run state、canonical publication link、Published Content、Article 03 navigation、Reviewer Final PASS 与 Publisher / Build evidence 已由 Master 对齐。Lifecycle=`PUBLISHED` 只是 checkpoint candidate，独立 Article 04 commit 验证前不得启动 Article 05。
- Master Pre-commit Reverification：首次 sandboxed 启动因 `hugo.exe` `拒绝访问` 没有形成有效 build；按权限流程以同一 `hugo --gc --minify` 重跑后，Hugo `0.157.0`，`1233 Pages / 0 ERROR / 0 WARNING`，process `exit 0`，Hugo total `5858 ms`。Master 只据有效重跑结果判定 `PASS`。

## Evidence Constraints

- `04-C02`、`04-C05`、`04-C06`、`04-C07` 必须保持 `PARTIAL` 的课程 working boundary / Provider scope。
- `04-C08` 必须保持 `PROPOSAL / NOT_EXECUTED`，不得写成统一标准或已实现 capability negotiation。
- OpenAI .NET 与 Anthropic C# 的默认 retry 数字必须同时保留 Provider、SDK language 与 `2026-08-20` 日期范围。
- Provider Calls=`NONE`、Runtime Evidence=`UNVERIFIED`；不得把 docs-only contract 写成 runtime observation。

## Stop Line

Article 04 已完成 Reviewer、Publisher、Build、Master Reconciliation、独立 completion commit 与 commit verification；checkpoint=`ac10060b82d21534a014d7a4bef3b3e03f7bd475`，Lifecycle=`PUBLISHED`，后续 Part I Audit checkpoint也已验证。本 workspace 不再路由 Article 05 或任何后续 transaction；当前课程对象与下一动作只由 global run state 决定。
