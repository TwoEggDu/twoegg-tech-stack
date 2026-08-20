# Article 08｜Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop

- Canonical ID: `08`
- Workspace: `08-agent-loop`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `PASS / 6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Required Lab: `Lab 03 Minimal Agent Loop`
- Lab Status: `VERIFIED / EVIDENCE_MERGED`
- Mode: `LAB_ARTICLE`
- Current Gate: `END_ARTICLE`
- Next Allowed Action: `ARTICLE_09_PRECHECK_NOT_STARTED`
- Blocker: `NONE`

## Dependencies

- Article 03：Structured Output 的 Parse / Schema / DTO / Domain validation 边界；checkpoint `857fe9f` verified。
- Article 05：Function Calling 只表达结构化行动候选，Host 决定是否执行。
- Article 06：Tool Runtime 的 validate / policy / execute / result / trace 管线；checkpoint `199d4e1` verified。
- Article 07：MCP 协议成功不能单独证明 Agent Loop、Permission、Tool Runtime gates 或完整 Evidence；checkpoint `f3de0f2` pushed / live-remote verified。

## Current Scope

本篇是 required Lab Article，只负责正式建立 Agent Loop 的 Turn / Step / Decide / Act / Observe / State / Stop 边界。Planning、Workflow / State Machine、Long-running recovery、Context / Memory、多 Agent、DeepSeek Harness source verification 与 BuildPilot Runtime 均留给后续课程对象。

## Transaction Record

- PRECHECK：`PASS`；canonical 顺序为 `07 -> 08 -> 09`，Article 08 为 `L / non-optional / LAB_ARTICLE`。
- Repository boundary：Article 07 local / live `origin/main` 均为 `f3de0f2a7b1e06c530900627183bd364ca0b4314`，left/right=`0/0`；PRECHECK 时 worktree clean。
- ARTICLE_KICKOFF：`PASS`；Master 已取得唯一 Article 08 transaction ownership。
- WORKSPACE_INIT：`PASS`；只创建 `README.md / article-card.md / research.md / evidence.md / review.md` 的 `PLANNED` skeleton。
- Worker Start：真实 Researcher `/root/article_08_researcher` 已启动；Allowed Writes 仅为 `research.md / evidence.md / labs/lab-03-minimal-agent-loop/README.md`。
- Research / Preliminary Evidence：`COMPLETE`；`8 Claims / 8 Evidence Cards`；`4 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 2 PROPOSAL`；Evidence Gate=`NOT_READY`。
- Lab 03 Design：`FROZEN / COMPLETE / NOT EXECUTED`；Design SHA-256=`242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`；555 lines。
- Lab Worker Start：真实 Lab Engineer `/root/article_08_lab_engineer` 已启动；Allowed Writes 仅为 `labs/lab-03-minimal-agent-loop/` 内 implementation / fixtures / tests / observations。
- Lab Execute / Observation：`COMPLETE`；formal restore / build / test / run-a / run-b / independent verifier均 exit `0`；4 cases、10 STEP、4 TERMINAL、10 states、7 Tool Outcomes、7 Observations、10 decisions、1 SUCCEEDED；run-a/run-b六文件pairwise byte-identical。
- Frozen Design Integrity：first `30,312` bytes SHA-256仍为`242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`。
- Evidence Merge Start：真实 Researcher `/root/article_08_researcher` 已启动；Lab status与Claim升级尚未由Master预判。
- Evidence Merge / Gate：Researcher=`PASS recommendation`，Master artifact check=`PASS`；final Claim=`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`；C03/C05保持课程`PROPOSAL`，C07/C08仅在fixed Host / deterministic fixture scope内`CONFIRMED`。
- Outline Worker Start：真实 Author `/root/article_08_outliner` 已启动；Allowed Writes仅为`outline.md`。
- Outline Worker Failure：`/root/article_08_outliner`、`/root/article_08_outliner_resume`、`/root/article_08_outliner_minimal` 三个真实 Author task 均在只读阶段长时间无响应；Master在确认`outline.md`始终不存在后依次中断。没有越界写入、Draft、Review或Article 09资产。
- Fresh Resume Reconciliation：2026-08-20 重新核验 local `HEAD`、fresh-fetched `origin/main` 与 live `ls-remote refs/heads/main` 均为 `1045264057f1eced21f8e7438b43bb7448a67091`；worktree clean；Published Content、`outline.md`、`draft.md` 均不存在。Factory 已恢复为 `RUNNING / OUTLINE`，等待 fresh real Author durable output。
- Fresh Author Retry Failure：`/root/article_08_author_outline` 与最小上下文 `/root/article_08_author_outline_minimal` 均在重复等待和明确收敛消息后无 durable output，已安全中断；结合三次历史失败，当前判定为`SUBAGENT_RUNTIME_UNAVAILABLE`。没有越界写入、Draft、Review、Published Content 或Article 09资产。
- Fresh Author Recovery：repository reconciliation 确认 `HEAD == origin/main == cfd763c0ba52f6d2cfacd3dc7f8323b913529eec` 与 clean worktree 后，唯一 fresh real Author `/root/article_08_author_outline_fresh` 以最小 dependency / final Evidence / Lab Observation 上下文创建 `outline.md`；Author self-check=`8 / 8 COVERED / NO NEW CORE FACT REQUIRED / PASS_RECOMMENDED`，Master artifact / write-boundary check=`PASS`。
- Author Draft Worker Start：2026-08-20 19:06 Master 从 live repository state 恢复 `AUTHOR_DRAFT`，在隔离分支 `codex/article-08-production` 登记 fresh Author `/root/article_08_author_draft`；Allowed Writes 仅为当前 Article `draft.md`，必须返回 closed-schema `worker_result`。
- Author Draft Gate：`PASS`；`/root/article_08_author_draft` 只创建 `draft.md` 并返回 schema-valid envelope。Master 验证 Allowed Writes / actual diff、`8 / 8` Claim traceability、Learning Check、最短结论、Proposal / fixed-fixture scope 与 non-scope 均满足 Draft Gate。
- Review Worker Start：fresh Reviewer `/root/article_08_reviewer_cycle0` 已登记；只允许修改 `review.md`，不接收 Author hidden reasoning、confidence 或 self-score。
- Initial Review：`92 / 100 / REVISION_REQUIRED`；`08-F01 OPEN MINOR` 要求在 pre-decision guard terminal 路径显式提交 terminal record / trace，同时保留 guard-before-Decide 与 no-consumed-Step 边界。
- Revision Worker Start：`/root/article_08_revision_cycle1` 只允许定向修改 `draft.md` 与在 `review.md` 记录 `READY_FOR_RECHECK` disposition；不得自行关闭 Finding。
- Revision Disposition：`08-F01 READY_FOR_RECHECK`；guard terminal 现在先提交 terminal record 与 terminal-only trace，再退出，并明确不消费新 Decision / Step。Master 验证修订未扩展 Evidence 或后续篇章。
- Review Recheck Start：fresh Reviewer `/root/article_08_reviewer_recheck_cycle1` 已登记；只复核原 Finding scope 并决定 `CLOSED / OPEN / ESCALATED`。
- Review Recheck：`PASS / cycle 1 / 92 / 100`；`08-F01 CLOSED`，当前无未关闭 Finding。Master 已验证 artifact、score threshold 与 `REVIEW_RECHECK -> FINAL_GATE` mapping。
- Final Gate Worker Start：Reviewer `/root/article_08_final_gate` 已登记；只允许在 `review.md` 作独立 Final Gate decision，不得修改 Draft 或创建 Published Content。
- Final Gate：`PASS / 92 / 100 / 0 OPEN`；Master 验证 Review、Claim、Evidence / Lab scope 与 State Machine mapping 后把 Lifecycle 推进为 `FINAL`。
- Publisher Start：`/root/article_08_publisher` 已登记；只允许机械映射 frozen Draft、补齐 front matter / internal navigation 与 publication result，不得改知识内容或 global durable state。
- Publish Gate：`PASS`；Master 已验证 Published Content、Article 07 next-link、front matter / relref / Lab link、semantic mapping 与 Allowed Writes；Build 仍未执行。
- Build Verify Start：Publisher `/root/article_08_build_verify` 已登记；只运行真实 Hugo command 并把结果写入本 README Publication Result，不得修改知识内容或 global durable state。
- Build Verify：`PASS`；`hugo --gc --minify` exit `0`，Hugo `0.157.0`，`1237 Pages / 0 ERROR / 0 WARNING`，rendered Article 07↔08 navigation=`PASS`。
- Master State Reconciliation：`PASS`；Reviewer Final、Publisher、Build、workspace、Published Content、canonical candidate 与 global state 已对齐，Lifecycle=`PUBLISHED`。
- Git Diff Verify：`PASS`；branch=`codex/article-08-production`；10 个 transaction paths、0 Article 09、0 delete / rename / unrelated path；`git diff --check=PASS`；Master final Hugo=`1237 Pages / 0 ERROR / 0 WARNING / exit 0`。
- Article Checkpoint / Commit Verify：`PASS`；commit=`d4693bd6d78ed63a669e181516e28247460fee11`；message=`Publish Agent Engineering Article 08`；10-file scope、clean worktree、log / show 与 `git diff HEAD^ HEAD --check` 均已验证；local branch 尚未 push。
- Article 08 Published Content：`content/ai-empowerment/agent-engineering-08-agent-loop.md`
- Lab 03 directory：`docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/`；Design、implementation、tests、raw observations 与 Evidence Merge 已由 recovery checkpoint `1045264` 保存。
- Article 09 workspace：`NONE`

## Stop Line

Reviewer Final、Publish、Build、Master State Reconciliation、Git Diff Verify、独立 checkpoint commit 与 commit verification 均已通过，`END ARTICLE 08` 成立。Factory 为 `READY / PRECHECK`，pointer 指向 Article 09 但不代表 Article 09 已启动；Article 09 workspace 不存在，Research / Evidence / Lab / Draft 均未开始。

## Publication Result Candidate

- Publisher Result Candidate：`PASS`；仅表示本次 `PUBLISH` 机械映射与静态检查候选，不是 Article completion 或 `PUBLISHED` 决策。
- Published Path：`content/ai-empowerment/agent-engineering-08-agent-loop.md`
- Published Route：`/ai-empowerment/agent-engineering-08-agent-loop/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-08-agent-loop/`
- Front Matter Result：`PASS`；title=`Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop`；slug=`agent-engineering-08-agent-loop`；date=`2026-08-20`；`draft: false`；tags=`Agent Engineering / AI Engineering / Agent Loop / Runtime Engineering`；`series: Agent Engineering`、`primary_series: agent-engineering`、`series_role: article`、`series_order: 90`、`weight: 3090`；YAML 字符串与 shortcode 参数均使用 ASCII 双引号。
- Series Result：`PASS / SOURCE CANDIDATE`；Article 08 只添加上一篇 Article 07 导航，不添加未发布 Article 09；Article 07 只机械新增一条指向 Article 08 的“下一篇” `relref`。
- Internal Link Result：`PASS / STATIC`；FINAL Draft 中 4 个 Published Article 03 / 05 / 06 / 07 repository-relative links 已机械转换为 Hugo `relref`；Lab 03 Design、execution log 与 run-a 的 5 个 raw artifacts 共 7 个 workspace-relative links已机械转换为 `https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/...` URL；Published Content 不再保留 repository-relative Markdown link。
- Semantic Diff Result：`PASS`；移除 FINAL Draft 唯一 H1，并排除 Published front matter 与顶部上一篇导航，再反向归一 11 个发布载体链接后，Published knowledge body 与 frozen Draft 逐字符 exact match；reconstructed Draft SHA-256=`EEEAFD60C8B38637A38B0C2D397124C64D5E5CBADF9DA694E8150C2B369B5192`；knowledge semantics change=`0`。
- Static Publication Checks：`PASS`；front matter、5 个 shortcode ASCII quotes、Article 09 link=`0`、repository-relative Markdown link=`0`、Lab GitHub blob link=`7`、code fence marker=`12 / paired`、trailing whitespace=`0`；Article 07 diff 仅新增一条下一篇导航。Publisher相对启动基线只新增或修改白名单中的3个文件；其余dirty transaction files均已在Publisher启动前存在，未被本Gate修改。
- Build Command Candidate：`hugo --gc --minify`
- Build Result：`PASS / DURABLE BUILD EVIDENCE`；Hugo=`v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64`；command=`hugo --gc --minify`；exit=`0`；Pages=`1237`；Paginator pages=`0`；Non-page files=`0`；Static files=`44`；Processed images=`0`；Aliases=`0`；Cleaned=`0`；Total=`7442 ms`。
- Warnings：`0 / NONE IN HUGO OUTPUT`
- Errors：`0 / NONE IN HUGO OUTPUT`
- Rendered Route / Navigation Result：`PASS`；`public/ai-empowerment/agent-engineering-08-agent-loop/index.html`存在（`49,691` bytes）；`public/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary/index.html`存在（`45,297` bytes）；rendered Article 07“下一篇”href=`/twoegg-tech-stack/ai-empowerment/agent-engineering-08-agent-loop/`，rendered Article 08“上一篇”href=`/twoegg-tech-stack/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary/`。
- Files Written：`content/ai-empowerment/agent-engineering-08-agent-loop.md`（新建）；`content/ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md`（仅新增下一篇导航）；`docs/agent-engineering-course/articles/08-agent-loop/README.md`（仅追加本 Publication Result candidate）。
- Recommended Next Gate：`PRECHECK`（Article 09 transaction 尚未启动）
- Recommended Article Transition：`PUBLISHED / checkpoint d4693bd verified / END ARTICLE 08`。
- Recommended Status Changes：由 Master 在 Reviewer Final PASS、Publisher PASS、独立 Build PASS 与 repository consistency 全部验证后，统一更新 Article README lifecycle、`status.md`、`course-run-state.md` 与 checkpoint metadata。
- Canonical Update Candidate：canonical Article 08 row should link to `../content/ai-empowerment/agent-engineering-08-agent-loop.md`；Publisher 未修改 `docs/agent-engineering-series-plan.md`。
- Checkpoint Readiness：`PASS / d4693bd6d78ed63a669e181516e28247460fee11 VERIFIED`。
- Publisher Boundary：未修改 frozen Draft、Review、Evidence、Lab、canonical、course README、`status.md`、`course-run-state.md`、theme、CI或Article 09；未stage、commit、push或创建PR。
