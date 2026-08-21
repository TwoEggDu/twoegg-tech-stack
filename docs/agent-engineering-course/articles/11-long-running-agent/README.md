# Article 11｜Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery

- Canonical ID: `11`
- Workspace: `11-long-running-agent`
- Part: `II｜从模型到 Agent`
- Course Weight: `M`
- Optional: `NO`
- Lifecycle Status: `PUBLISHED / PRE_COMMIT_RECONCILIATION PASS`
- Evidence Status: `PASS / 9 of 9 TRACEABLE / 0 CORE BLOCKED / C08 SPLIT-SCOPED`
- Required Lab: `Lab 04 State Machine + Checkpoint`
- Mode: `LAB_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION / PASS`
- Active Worker: `NONE`
- Next Allowed Action: `GIT_DIFF_VERIFY`
- Blocker: `NONE`

## Dependencies

- Article 06：Tool Runtime 的 validation、policy、execution、Result 与 Trace 边界。
- Article 08：Agent Loop 的 committed Step、Observation、State 与 Stop / Success 边界。
- Article 09：Plan 是剩余行动候选，不是执行或恢复事实。
- Article 10：State / Transition / Guard / Invariant / Terminal 与 Agent Decision Point；completion commit `b35b1f3225f9715f123496d39457f529362b997d` 已在remote `main`验证。

## Current Scope

本篇只研究Long-running Task新增的failure surface，以及Checkpoint、Retry Budget、Idempotency、Timeout、Cancellation、Resume、Compensation与Recovery Boundary怎样形成显式合同。Required Lab 04必须真实验证取消后恢复、幂等Retry、checkpoint完整性与raw resume trace；不设计分布式事务，不承诺回滚所有外部副作用，不展开Article 12 Context Engineering。

## Transaction Record

- Resume / PRECHECK：`PASS`；`main == origin/main == b35b1f3225f9715f123496d39457f529362b997d`，worktree clean，Article 10 unique completion commit / workspace / Published Content已复核。
- ARTICLE_KICKOFF：`PASS`；Master已取得唯一Article 11 transaction ownership。
- WORKSPACE_INIT：`PASS`；Master机械创建五个PLANNED content skeleton；`subagent-trace.md`是transaction record，不是Research或Article content。
- Research / Preliminary Evidence / Lab Design：Researcher `/root/article_11_researcher_lab_design` 已登记；只允许更新`research.md / evidence.md`并创建Lab 04 frozen README，不得实现或运行fixture。
- Preliminary Evidence / Lab Design Result：`PASS / MASTER VERIFIED`；9 Claims均有preliminary mapping，C01—C08行为Claim保持`PARTIAL / BLOCKED + REQUIRED`，C09为非Lab产品反证边界；Lab README仅含frozen Design，LR-01—08与16项Acceptance Criteria完整；环境目标与本机`.NET 10.0.301 / Host 10.0.9 / Windows 10.0.19045 win-x64`一致。
- Lab Execute / Observation：Lab Engineer `/root/article_11_lab_engineer` 已登记；只允许实现并执行frozen Design，不能解释或升级Claim Status。
- Lab Execute / Observation Result：`PASS / MASTER VERIFIED`；offline build=`0 warnings / 0 errors`，LR-01—08两套suite均完成，每套12个fresh child PIDs；105个normalized files byte-identical，aggregate SHA-256=`27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a`；first failures与限制已保留。
- Evidence Merge：fresh Researcher `/root/article_11_researcher_evidence_merge` 已登记；只有它可以解释raw Observation并更新Claim Status。
- Evidence Merge / Evidence Gate Result：`PASS / MASTER VERIFIED`；C01—C09完成`Experiment -> Observation -> Interpretation -> Claim Status`闭环，9 / 9 traceability、0核心BLOCKED；Lab 04=`CONFIRMED / EVIDENCE_MERGED`。Master重跑static contract与run A/B compare均PASS；结论保持fixture / proposal / course-runtime / course-schema / product-doc scope，不升级为production或distributed保证。
- Outline Result：`PASS / MASTER VERIFIED`；`outline.md`唯一新增，C01—C09=`9 / 9 COVERED`，Problem Space -> Abstract Model -> Concrete Mechanism -> Engineering Judgment -> Verification完整；Lab 04八case、LR-05 / LR-06负例、Learning Check、job competency、scope ceilings与Article 12 stop line均存在；`NO NEW CORE FACT REQUIRED`。
- Author Draft Result：`PASS / MASTER VERIFIED`；`draft.md`唯一新增，5922 CJK chars、9 / 9 Claim traceability、20 links（11 local均存在）；Expected / Observed、LR-01—08、LR-05 / LR-06负例、partial result、scope ceilings、Learning Check与Article 12 stop line均存在，`git diff --check`通过。
- Review Cycle 0 Result：`PASS execution / BLOCKED decision / MASTER VERIFIED`；score=`92 / 100`，Evidence Discipline=`17 < 18`；`11-R0-F01 MAJOR`指出C08把未执行的integrity mismatch分支混入CONFIRMED，`11-R0-F02 MINOR`指出LangGraph checkpointer细节locator漂移。Open=`1 MAJOR + 1 MINOR`，Final Gate不合格。
- Revision Cycle 1 Result：`PASS / MASTER VERIFIED / READY_FOR_RECHECK`；Research / Evidence / Draft将C08拆为missing-in-flight与run A/B已确认、integrity mismatch=`PROPOSAL / NOT_OBSERVED`；LangGraph Checkpointers与Persistence overview职责 / locator分离；Review只追加两项Disposition，未自行关闭Finding，Lab未修改或重跑。
- Review Recheck Cycle 1 Attempt 0：Reviewer `/root/article_11_reviewer_recheck_cycle1` 在任何write前主动报告Windows path exclusion失效、意外看到forbidden trace snippets；Master确认`review.md`mtime未变后安全中断，禁止该execution裁决Finding。
- Review Recheck Cycle 1 Retry 1 Result：`PASS / MASTER VERIFIED`；`11-R0-F01 / F02 CLOSED`，Open Findings=`0`；score=`94 / 100`，Technical / Evidence / Teaching / Engineering=`19 / 19 / 19 / 19`，全部门槛通过，Final Gate=`ELIGIBLE`。
- Final Gate Result：`PASS / MASTER VERIFIED`；score=`94 / 100`、C01—C09=`9 / 9`、Open Findings=`0`，C08三层证据与Lab负例、Article 12 stop line保持；Draft可机械发布，不替代Publisher / Build Gate。
- Publish Result：`PASS / MASTER VERIFIED / READY_FOR_BUILD_VERIFY`；Article 11 Published Content已创建，Article 10仅新增单一next link；反向归一后Final Draft与Published knowledge body逐字符相等，5个ASCII relrefs、7个GitHub blob links、0 relative links，Build尚未执行。
- Build Verify Result：`PASS / MASTER VERIFIED`；Worker definitive run与Master独立重跑均为Hugo `0.157.0 / exit 0 / 1240 Pages / 0 warnings / 0 errors`；Article 11 route / title / semantic marker、Article 10 -> 11和Article 11 -> 10 rendered navigation均PASS。首次sandbox launcher denial没有启动Hugo，保留为execution note，不计为build failure。
- PRE_COMMIT_RECONCILIATION：`PASS / LAST REPOSITORY WRITE`；Lifecycle、Published path、Review / Lab / Build、canonical Article 11 link、status、course README与Factory pointer已对齐。Article 12 workspace / Published Content仍absent；pointer candidate=`READY / Article 12 PRECHECK`，不等于Article 12 Kickoff。
- GIT_DIFF_VERIFY Attempt 0：`FAIL / RETURNED TO PRE_COMMIT_RECONCILIATION`；发现Lab build留下50个undeclared `bin/obj` generated files，不在Lab Engineer artifact白名单内，未stage、未commit。Master只删除四个经absolute-path / inside-Lab / leaf=`bin|obj`验证的generated directories。
- PRE_COMMIT_RECONCILIATION Retry 1：`PASS / LAST REPOSITORY WRITE`；untracked candidate从288降为238（Article workspace 8、Lab 04 declared source/raw artifacts 229、Published Article 11 1），`bin/obj` remains=`0`；状态与Article 12 stop line不变。此记录后repository writes再次关闭为ZERO。

## Stop Line

Article 11已形成`PUBLISHED` completion-commit candidate；尚未完成Git checkpoint、push或remote verification。Article 12 workspace / Published Content均未创建，只有Article 11 `END ARTICLE`后才能执行Article 12 PRECHECK / KICKOFF。

## Publication Result

- Publisher Result：`PASS / READY_FOR_BUILD_VERIFY`；机械发布映射、静态检查与语义反向映射通过；本结果不声明Article Lifecycle=`PUBLISHED`。
- Published Path：`content/ai-empowerment/agent-engineering-11-long-running-agent.md`
- Published Route：`/ai-empowerment/agent-engineering-11-long-running-agent/`
- Front Matter Result：`PASS`；title=`Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery`；slug=`agent-engineering-11-long-running-agent`；date=`2026-08-21`；`draft: false`；tags=`Agent Engineering / AI Engineering / Runtime Engineering / Recovery Engineering`；`series: Agent Engineering`、`primary_series: agent-engineering`、`series_role: article`、`series_order: 120`、`weight: 3120`；frontmatter shape与ASCII引号检查通过。
- Series Result：`PASS / SOURCE CANDIDATE`；Article 11顶部只提供上一篇Article 10，不创建Article 12下一篇；Article 10只机械新增一条指向Article 11的“下一篇”`relref`。
- Internal Link Result：`PASS / STATIC`；FINAL Draft中的Article 06 / 08 / 09 / 10链接已机械转换为Hugo `relref`；Article workspace Evidence、两处Lab README引用与四个raw observation链接已机械转换为GitHub `blob/main` URL；Published Content的repository-relative Markdown link=`0`，Article 12 `relref`=`0`。
- Semantic Diff Result：`PASS`；移除FINAL Draft唯一H1，并排除Published frontmatter与顶部上一篇导航，再反向归一11处发布载体链接后，Published knowledge body与frozen Draft逐字符exact match；reconstructed Draft body SHA-256=`553697EF6B8C84D4F530BE0FD7C572F782061A0A1E74BA1DE83CDC81A35714A7`；knowledge semantics change=`0`。
- Static Publication Checks：`PASS`；shortcode ASCII quotes=`5 / 5`；Chinese shortcode quotes=`0`；repository-relative Markdown link=`0`；GitHub blob link=`7`；code fence marker=`8 / paired`；placeholder marker=`0`；trailing whitespace=`0`；extra EOF blank lines=`0`；Article 10的Article 11 next-link=`1`；Article 12 `relref`=`0`。
- Build Commands：exact command=`hugo --gc --minify`；working directory=`E:\workspace\TechStackShow`。
- Hugo Version：`hugo v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64 BuildDate=2026-02-25T16:38:33Z VendorInfo=gohugoio`。
- Build Result：`PASS`；definitive Hugo execution exit code=`0`；Pages=`1240`；Paginator pages=`0`；Non-page files=`0`；Static files=`44`；Processed images=`0`；Aliases=`1`；Cleaned=`0`；Total=`7248 ms`。
- Build Output（verbatim）：

  ```text
  Start building sites …
  hugo v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64 BuildDate=2026-02-25T16:38:33Z VendorInfo=gohugoio


                    │ ZH - CN
  ──────────────────┼─────────
   Pages            │    1240
   Paginator pages  │       0
   Non-page files   │       0
   Static files     │      44
   Processed images │       0
   Aliases          │       1
   Cleaned          │       0

  Total in 7248 ms
  ```

- Execution Environment Note：第一次sandbox launch没有启动Hugo，PowerShell对WinGet安装路径中的`hugo.exe`返回`Access denied`；使用所需process permission原样重跑同一命令后得到上面的definitive Hugo exit=`0`。前者是launcher denial，不是Hugo build failure。
- Warnings：`0`；definitive Hugo output未出现warning。
- Errors：`0`；definitive Hugo output未出现error。
- Generated Route Result：`PASS`；`public/ai-empowerment/agent-engineering-11-long-running-agent/index.html`存在，包含title=`Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery`与semantic marker=`Long-running Agent 的 Recovery 不是`；对应Published Route=`/ai-empowerment/agent-engineering-11-long-running-agent/`。
- Rendered Navigation Result：`PASS`；rendered Article 10页面包含`href=/twoegg-tech-stack/ai-empowerment/agent-engineering-11-long-running-agent/`与Article 11标题；rendered Article 11页面包含`href=/twoegg-tech-stack/ai-empowerment/agent-engineering-10-state-machine-workflow/`与Article 10标题。
- Build Output / Repository Write Boundary：`public/`由`.gitignore:1`忽略；generated HTML是ignored build output，不计入repository artifacts。Build前后tracked / untracked status集合一致，Hugo没有引入tracked repository write；本Build Verify worker唯一repository artifact write是本README的Build Result更新。
- Files Written：`content/ai-empowerment/agent-engineering-11-long-running-agent.md`（新建）；`content/ai-empowerment/agent-engineering-10-state-machine-workflow.md`（仅新增一条Article 11下一篇导航）；`docs/agent-engineering-course/articles/11-long-running-agent/README.md`（仅追加本Publication Result）。
- Recommended Article Transition：`PRE_COMMIT_RECONCILIATION`
- Recommended Status Changes：`NONE`；Lifecycle、current worker与global durable state仍由Master单独验证和写入。
- Canonical Update Candidate：`NONE`；Publisher未修改canonical。
- Checkpoint Readiness：`READY_FOR_PRE_COMMIT_RECONCILIATION / NOT READY FOR CHECKPOINT`；Build已独立PASS，仍必须由Master完成Pre-Commit Reconciliation、独立checkpoint commit、Commit Verify、push与remote verification。
- Publish Gate Worker Boundary：未修改frozen Draft、Research、Evidence、Outline、Review、canonical、`status.md`、`course-run-state.md`、trace、Lab、theme或CI；未运行Hugo，未执行Git branch / stage / commit / push，也未创建PR或Article 12 workspace。
