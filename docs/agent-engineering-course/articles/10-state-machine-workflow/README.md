# Article 10｜State Machine 与 Workflow：确定性骨架和 Agent Decision Point

- Canonical ID: `10`
- Workspace: `10-state-machine-workflow`
- Part: `II｜从模型到 Agent`
- Course Weight: `L`
- Optional: `NO`
- Lifecycle Status: `PUBLISHED`
- Evidence Status: `PASS / 6 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`
- Required Lab: `NONE`（Lab 04 在 Article 11）
- Mode: `NORMAL_ARTICLE`
- Current Gate: `GIT_DIFF_VERIFY`
- Active Worker: `NONE`
- Next Allowed Action: `GIT_DIFF_VERIFY`
- Blocker: `NONE`

## Dependencies

- Article 08：Agent Loop 的 Turn / Step / Decide / Act / Observe / Stop 边界。
- Article 09：Plan 是剩余行动候选，不等于执行、Verified State、Authorization 或 Workflow；completion commit `7b9d733f33667fc8efab1708c682e67c13669846` 已在 remote `main` 验证。
- 读者已有传统状态机基础。

## Current Scope

本篇只建立 State、Transition、Guard、Invariant、Terminal State、Workflow 与 Agent Decision Point 的边界，回答确定性骨架和上下文判断应怎样分工。Checkpoint、Retry、Cancellation 与 Recovery 留给 Article 11；不做 BPM 教程、不引入 Multi-Agent、不展开分布式事务。

## Transaction Record

- Resume Reconciliation：`PASS`；Article 09 completion commit、workspace、Published Content与live remote equality已复核。
- PRECHECK：`PASS`；`main` clean，Article 10为`Part II / L / non-optional / NORMAL_ARTICLE`，workspace此前不存在。
- ARTICLE_KICKOFF：`PASS`；Master已取得唯一Article 10 transaction ownership。
- WORKSPACE_INIT：`PASS`；Master机械创建`README.md / article-card.md / research.md / evidence.md / review.md`五个PLANNED skeleton；`subagent-trace.md`是Runtime transaction record，不是Research或Article content。
- Research / Evidence：`PASS`；真实Researcher `/root/article_10_researcher`交付`10 Claims / 10 Evidence Cards`；`6 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`；AL-04 raw fact与`PROPOSAL / NOT EXECUTED` overlay分层；Master artifact / Allowed Writes / Gate validation=`PASS`。
- Outline：`PASS`；真实Author `/root/article_10_author_outline`创建`outline.md`；`10 / 10 COVERED`，Evidence等级、AL-04双层、Figures / Examples、Learning Check、Job Competency、publication plan与non-scope完整；New Core Facts=`NONE`；Master validation=`PASS`。
- Draft：`PASS`；fresh Author `/root/article_10_author_draft`创建`draft.md`；317行、10/10 Claim traceability、0未来relref、AL-04双层、Learning Check、shortest conclusion与Article 11 stop line均通过Master结构验证；New Core Facts=`NONE`。
- Review Cycle 0：`PASS_WITH_NOTES / 88`；Technical=`17 < 18`；`10-F01 MAJOR`与`10-F02 MINOR`均OPEN；route=`REVISION`。
- Revision Cycle 1：Revision Worker `/root/article_10_revision_cycle1` 已登记；只处理stale suggestion revision validation与Microsoft Functional Workflow canonical URL两个Finding。
- Revision Result：`PASS / READY_FOR_RECHECK`；F01 expected source/revision + atomic compare-and-commit已落盘，F02旧URL计数=`0`；Finding仍只能由Reviewer关闭。
- Review Recheck：fresh Reviewer `/root/article_10_reviewer_recheck_cycle1` 已登记；Allowed Writes仅为`review.md`。
- Review Recheck Result：`PASS / 96`；`10-F01 / 10-F02 CLOSED`，unclosed Findings=`0`，cycle=`1 / 3`，全部冻结质量阈值满足。
- Final Gate：独立Reviewer `/root/article_10_final_gate` 已登记；只允许复核发布资格并修改`review.md`。
- Final Gate Result：`FAIL / 10-F03 MINOR PUBLICATION`；Master实时复核确认Microsoft Learn的`/workflows/functional`当前重定向到`/concepts/workflows/functional`；发布未授权。
- Revision Cycle 2：Revision Worker `/root/article_10_revision_cycle2` 已登记；只修复`10-F03` locator，不改变Claim、Evidence等级、产品范围或Article 11 stop line。
- Revision Cycle 2 Result：`PASS / 10-F03 READY_FOR_RECHECK`；三份source artifact旧locator=`0 / 0 / 0`、target=`1 / 2 / 1`；初次Result Contract外层键缺失，已在零写入重发后纠正。
- Review Recheck Cycle 2：fresh Reviewer `/root/article_10_reviewer_recheck_cycle2` 已登记；只允许复核`10-F03`并修改`review.md`。
- Review Recheck Cycle 2 Result：`PASS / 96 / 0 OPEN`；`10-F03 CLOSED`，所有冻结质量阈值满足。
- Final Gate Cycle 2：fresh Reviewer `/root/article_10_final_gate_cycle2` 已登记；必须重新核对完整发布资格，不继承首次Final Gate结论。
- Final Gate Cycle 2 Result：`PASS / 96 / 0 OPEN / 10 of 10 VERIFIED`；`10-F01 / 10-F02 / 10-F03 CLOSED`，Publication Authorization=`GRANTED FOR PUBLISH GATE`。
- Publish Gate：fresh Publisher `/root/article_10_publisher` 已登记；只允许机械映射Published Content、Article 09单一next-link与本README的Publication Result candidate；Build尚未执行。
- Publication Result Candidate：`PASS / READY_FOR_BUILD_VERIFY`；Article 10 Published Content、Article 09单一next-link与三处载体链接机械映射已完成；静态检查与语义反向映射=`PASS`；Build=`NOT EXECUTED`。
- Publish Result：`PASS / MASTER VERIFIED`；Publisher初次Result Contract类型错误已在零写入重发后纠正；semantic exact SHA-256=`2E18D950E051823CFF7E80800EF138C7882804036FD3448ABA2A3DB396545F75`。
- Build Verify：独立Publisher execution `/root/article_10_build_verify` 已登记；Allowed tracked writes为空。
- Build Verify Result：`PASS / MASTER VERIFIED`；Hugo `0.157.0`，`1239 Pages / 0 ERROR / 0 WARNING`，exit=`0`；Article 09↔10 navigation与Article 10 rendered route通过，Article 11 route/link=`0`，tracked build-output变化=`0`。
- PRE_COMMIT_RECONCILIATION：`PASS`；Lifecycle=`PUBLISHED` completion-commit candidate；Article 11 pointer=`PRECHECK / NOT_STARTED`，workspace / Lab 04 / Published Content=`ABSENT`。

## Stop Line

PRE_COMMIT_RECONCILIATION已完成，这是Article 10最后一个repository-write Gate。后续只允许GIT_DIFF_VERIFY、唯一Article checkpoint commit、Commit Verify、一次Push Main、Remote Verify与只读Post-Commit Reconciliation；不得再修改repository files，也不得启动Article 11。

## Publication Result

- Publisher Result：`PASS / MASTER VERIFIED`；机械映射、静态检查与语义反向映射已由Master依据actual artifacts复核。
- Published Path：`content/ai-empowerment/agent-engineering-10-state-machine-workflow.md`
- Published Route：`/ai-empowerment/agent-engineering-10-state-machine-workflow/`
- Canonical URL Candidate：`https://twoeggdu.github.io/twoegg-tech-stack/ai-empowerment/agent-engineering-10-state-machine-workflow/`
- Front Matter Result：`PASS`；title=`State Machine 与 Workflow：确定性骨架和 Agent Decision Point`；slug=`agent-engineering-10-state-machine-workflow`；date=`2026-08-21`；`draft: false`；tags=`Agent Engineering / AI Engineering / Workflow Engineering / Runtime Engineering`；`series: Agent Engineering`、`primary_series: agent-engineering`、`series_role: article`、`series_order: 110`、`weight: 3110`；YAML字符串与shortcode参数均使用ASCII双引号。
- Series Result：`PASS / SOURCE CANDIDATE`；Article 10只提供上一篇Article 09，不添加未发布Article 11；Article 09只机械新增一条指向Article 10的“下一篇”`relref`。
- Internal Link Result：`PASS / STATIC`；FINAL Draft中的Article 08 / 09 repository-relative links已机械转换为Hugo `relref`；AL-04 trace已机械转换为GitHub `blob/main` URL；Published Content不再保留repository-relative Markdown link，也没有Article 11 future `relref`。
- Semantic Diff Result：`PASS`；移除FINAL Draft唯一H1，并排除Published front matter与顶部上一篇导航，再反向归一三处发布载体链接后，Published knowledge body与frozen Draft逐字符exact match；reconstructed Draft body SHA-256=`2E18D950E051823CFF7E80800EF138C7882804036FD3448ABA2A3DB396545F75`；knowledge semantics change=`0`。
- Static Publication Checks：`PASS`；shortcode ASCII quotes=`3 / 3`；Article 11 future `relref`=`0`；repository-relative Markdown link=`0`；AL-04 GitHub blob link=`1`；code fence marker=`10 / paired`；trailing whitespace=`0`；Article 09的Article 10 next-link=`1`。
- Build Command：`hugo --gc --minify`
- Build Result：`PASS`；Hugo `0.157.0`；`1239 Pages`；exit code `0`；Article 10 rendered route存在；Article 09→10与Article 10→09 navigation通过。
- Warnings：`0`
- Errors：`0`
- Files Written by Publisher：`content/ai-empowerment/agent-engineering-10-state-machine-workflow.md`（新建）；`content/ai-empowerment/agent-engineering-09-planning.md`（仅新增一条Article 10下一篇导航）；`docs/agent-engineering-course/articles/10-state-machine-workflow/README.md`（仅登记Publication Result candidate与Build未执行）。
- Next Gate：`GIT_DIFF_VERIFY`；repository writes after this reconciliation=`ZERO`。
- Article Transition：`PUBLISHED / COMPLETION-COMMIT CANDIDATE`；只有checkpoint commit、push、remote equality与只读reconciliation通过后才能`END ARTICLE 10`。
- Status Changes：`MASTER APPLIED / PRE_COMMIT_RECONCILIATION PASS`。
- Canonical Result：Article 10 row已链接到`../content/ai-empowerment/agent-engineering-10-state-machine-workflow.md`。
- Checkpoint Readiness：`READY_FOR_GIT_DIFF_VERIFY`；completion SHA不得自引用，稍后由Git history提供。
- Publisher Boundary：未修改frozen Draft、Research、Evidence、Outline、Review、canonical、course README、`status.md`、`course-run-state.md`、trace、theme、CI或Article 11；未执行Git branch / stage / commit / push，也未创建PR。
