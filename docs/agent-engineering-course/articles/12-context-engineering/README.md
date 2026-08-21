# Article 12｜Context Engineering：每一个 Step 到底应该看到什么

- Canonical ID: `12`
- Workspace: `12-context-engineering`
- Part: `III｜Context Engineering 与 Memory`
- Course Weight: `L`
- Optional: `NO`
- Lifecycle Status: `PUBLISHED / PRE_COMMIT_RECONCILIATION PASS`
- Evidence Status: `PASS / 9 of 9 TRACEABLE / 0 CORE BLOCKED`
- Required Lab: `NONE`（为未来 Lab 05 产出 3 个 Context Snapshot）
- Mode: `STANDARD_ARTICLE`
- Current Gate: `PRE_COMMIT_RECONCILIATION / PASS`
- Active Worker: `NONE`
- Next Allowed Action: `GIT_DIFF_VERIFY`
- Blocker: `NONE`

## Dependencies

- Article 02：Prompt 的目标、输入、约束与输出合同。
- Article 06：Tool Schema、Tool Result 与 Trace 边界。
- Article 08：一次可提交的 Step 与 Agent Loop。
- Article 09：Plan、History 与执行事实的分离。
- Article 10—11：Workflow State、Checkpoint 与 Recovery Boundary。

## Current Scope

本篇只研究单个 Model Step 的 Context 怎样从 Prompt、State、History、Tool Schema / Result 与 Environment 中选择、排序、装配、预算并形成可追溯 Snapshot / Receipt。Context 不等于 Prompt、Session、Memory 或永久历史；不展开向量检索、长期 Memory、具体 Compaction 算法与 Article 13 Context Debugging。

## Transaction Record

- PRECHECK：`PASS`；`main == origin/main == live remote == e7f88c03151707d00b7d307645e99cf4710f3363`，worktree clean；Article 02 / 06 / 08 / 09 / 10 / 11 Published Content 存在；Article 12 workspace / Published Content 在 kickoff 前均 absent。
- ARTICLE_KICKOFF：`PASS`；Master `/root` 于 `2026-08-21T18:36:03+08:00` 取得唯一 Article 12 transaction ownership。
- WORKSPACE_INIT：`PASS`；Master 只创建五个 `PLANNED` content skeleton；`subagent-trace.md`是 transaction record，不是 Research 或 Article content；`outline.md / draft.md`仍不存在。
- Research：fresh Researcher `/root/article_12_researcher` 已登记；Allowed Writes 仅为当前 Article `research.md / evidence.md`，不得创建 Outline / Draft、修改global state、实现Lab 05或启动Article 13。
- Research / Evidence Gate Result：`PASS / MASTER VERIFIED`；C01—C09=`9 / 9 TRACEABLE / 0 CORE BLOCKED`；6组current official source scope、反证、Request Breakdown、Contributor Priority、Context Receipt schema + sample与3个`PROPOSAL / DESIGN INPUT ONLY` Context Snapshot齐全。课程taxonomy / Receipt / priority不升级为行业标准，provider-managed context只记录known / unknown，不承诺完整重建。
- Outline：fresh Author `/root/article_12_author_outline` 已登记；只允许创建`outline.md`，不得创建Draft或修改Evidence / global state。
- Outline Result：`PASS / MASTER VERIFIED`；`outline.md`是唯一新增artifact，C01—C09=`9 / 9 COVERED`，Teaching Spine、四类边界、必需表 / Receipt / 3 Proposal Snapshot、Learning Check、competency、source plan与length budget齐全；`NO NEW CORE FACT REQUIRED`。
- Author Draft：fresh Author `/root/article_12_author_draft` 已登记；只允许创建`draft.md`，不得修改frozen Outline / Evidence或写Published Content。
- Author Draft Result：`PASS / MASTER VERIFIED`；`draft.md`为唯一新增artifact，C01—C09、16个links（6个local均存在）、Request Breakdown / Priority / Receipt / 3 Proposal Snapshot、四类边界、Learning Check与最短结论齐全；8个fence markers成对，placeholder / TODO=`0`，无新核心事实或Lab / Runtime observation。
- Review Cycle 0：fresh Reviewer `/root/article_12_reviewer_cycle0` 已登记；只允许更新`review.md`，不得修正文稿。
- Review Cycle 0 Result：`PASS execution / BLOCKED decision / MASTER VERIFIED`；score=`85 / 100`，Open Findings=`2 MAJOR + 2 MINOR`。`12-R0-F01`要求分离effective model Context、application-visible Snapshot与Receipt audit能力；`F02`恢复课程Session定义；`F03`标明开场为构造场景；`F04`移出Author-only审计附录。Final Gate不合格。
- Revision Cycle 1：Revision Worker `/root/article_12_revision_cycle1` 已登记；只允许在4项Finding范围内最小修改Research / Evidence / Outline / Draft并向Review追加Disposition，不得自行关闭Finding。
- Revision Cycle 1 Result：`PASS / MASTER VERIFIED / READY_FOR_RECHECK`；F01分离effective Context / application-visible Snapshot / Receipt audit，F02恢复课程Session定义，F03明确构造场景，F04移除Author-only appendix；Research / Evidence / Outline / Draft同步，Review只追加Disposition，未自行关闭Finding。
- Review Recheck Cycle 1：fresh Reviewer `/root/article_12_reviewer_recheck_cycle1` 已登记；只复核4项原Finding及Disposition，不读取Revision hidden reasoning。
- Review Recheck Cycle 1 Result：`PASS / MASTER VERIFIED`；`12-R0-F01`—`F04 CLOSED`，Open Findings=`0`；score=`93 / 100`，Technical=`19`、Evidence=`19`、Teaching=`18`、Engineering=`19`，全部阈值通过，Final Gate=`ELIGIBLE`。
- Final Gate：fresh independent Reviewer `/root/article_12_final_gate` 已登记；只做冻结artifact与publication eligibility最终确认，不替代Publisher / Build。
- Final Gate Result：`PASS / MASTER VERIFIED`；score=`93 / 100`、C01—C09=`9 / 9`、Open Findings=`0`，Context / Snapshot / Receipt / Session边界、Proposal / NOT_EXECUTED与Article 13 stop line保持；Draft可机械发布，不替代Publisher / Build Gate。
- Publish：Publisher `/root/article_12_publisher` 已登记；只允许创建Article 12 Published Content、为Article 11添加单一next link，并记录当前Article Publication Result；不得Build或修改global state / canonical。
- Publish Result：`PASS / MASTER VERIFIED / READY_FOR_BUILD_VERIFY`；Published Content创建、Article 11单一next link与Publication Result均在白名单内。Master独立反向归一后frozen Draft与Published knowledge body逐字符相等；frontmatter / relref / navigation / fence / placeholder checks通过，Article 13 ref=`0`；Build仍未执行。
- Build Verify：Publisher `/root/article_12_build_verify` 已登记；只允许运行Hugo、核验rendered route / navigation并更新本README Build Result，不得改Published Content或global state。
- Build Verify Result：`PASS / MASTER VERIFIED`；Worker definitive run与Master独立重跑均为Hugo `0.157.0 / exit 0 / 1241 Pages / 0 warnings / 0 errors`；Article 12 route / title / shortest-thesis marker与Article 11↔12 rendered navigation全部PASS。首次sandbox launcher denial未启动Hugo，保留为execution note。
- PRE_COMMIT_RECONCILIATION：`PASS / RETRY 1 / LAST REPOSITORY WRITE`；首次GIT_DIFF_VERIFY在commit前因`article-card.md`末尾多一空行而正确失败，未产生commit或push。Master回到本Gate，只删除该终止空行并同步recovery记录；Lifecycle、Published path、Evidence / Review / Build、canonical Article 12 link、status、course README与Factory pointer仍对齐。Article 13 workspace / Published Content仍absent；pointer candidate=`READY / Article 13 PRECHECK`，不等于Article 13 Kickoff。此retry记录后repository writes=`ZERO`，GIT_DIFF_VERIFY必须从完整14-path scope重启。

## Stop Line

Article 12已形成`PUBLISHED` completion-commit candidate；尚未完成Git checkpoint、push或remote verification。Article 13 workspace、Lab 05实现与Published Content均未创建；完成Article 12 remote verification后本任务必须停止，不执行Article 13 PRECHECK / Kickoff。

## Publication Result

- Result：`PASS / PUBLISHER + BUILD VERIFIED`；Published Content=`content/ai-empowerment/agent-engineering-12-context-engineering.md`；route=`/ai-empowerment/agent-engineering-12-context-engineering/`；Build=`PASS / 1241 Pages / 0 warnings / 0 errors`；next gate=`PRE_COMMIT_RECONCILIATION`。
- Frontmatter：`title=Context Engineering：每一个 Step 到底应该看到什么`、`slug=agent-engineering-12-context-engineering`、`date=2026-08-21`、evidence-bounded Context / Snapshot / Receipt description、`draft=false`、tags=`Agent Engineering / AI Engineering / Context Engineering / Observability`、`series=Agent Engineering`、`primary_series=agent-engineering`、`series_role=article`、`series_order=130`、`weight=3130`；YAML and relref quotation is ASCII.
- Series / navigation：Published Article 12 has exactly one previous navigation line to Article 11 and no Article 13 next navigation; Article 11 has exactly one added next navigation line to Article 12, immediately after its existing previous navigation.
- Mechanical fidelity method：read frozen `draft.md`; from Published Content strip its leading YAML frontmatter and the added Article 11 previous-navigation line; restore Draft H1 plus its blank line; restore exactly six Hugo relref carriers to their original repository-relative Markdown targets (`02`, `08`, `09`, `10`, `06`, `11`); compare exact UTF-8 characters. Result=`equal`; Draft SHA-256 UTF-8=`93d63549c64110bc0933471a587141654ead20fe52d1c23e6b2ad8d261bad4b3`; reconstructed SHA-256 UTF-8=`93d63549c64110bc0933471a587141654ead20fe52d1c23e6b2ad8d261bad4b3`.
- Static publication checks：repository-relative Markdown links=`0`; ASCII-shaped relrefs=`7` (`6` converted dependencies + `1` previous navigation); Article 13 relrefs=`0`; fence markers=`8` / paired; `TODO` / `DATA-TODO` / `EXPERIENCE-TODO`=`0`; trailing-whitespace lines=`0`; `git diff --check`=`PASS`. Official external links remain unchanged.
- Files written in this gate：created `content/ai-empowerment/agent-engineering-12-context-engineering.md`; modified only `content/ai-empowerment/agent-engineering-11-long-running-agent.md` for its single next link and this Article 12 `README.md` for this result.
- Boundaries preserved：no semantic knowledge-body rewrite; frozen Draft / Research / Evidence / Outline / Review, global state, canonical plan, trace, labs, Git actions, Hugo build, and Article 13 were not modified or executed.

## Build Result

- Result：`PASS / PUBLISHER VERIFIED`；Gate=`BUILD_VERIFY`；execution=`REAL_SUBAGENT`；next gate=`PRE_COMMIT_RECONCILIATION`。
- Hugo：`hugo v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64 BuildDate=2026-02-25T16:38:33Z VendorInfo=gohugoio`。
- Commands：`hugo version` exit `0`；`hugo --gc --minify` exit `0`。The first sandbox launcher attempt was denied before Hugo started; the same commands were rerun with required process permission. This launcher denial is an execution note, not a Hugo failure.
- Definitive Hugo output：
  - `Start building sites …`
  - `hugo v0.157.0-7747abbb316b03c8f353fd3be62d5011fa883ee6+extended windows/amd64 BuildDate=2026-02-25T16:38:33Z VendorInfo=gohugoio`
  - `Pages 1241`
  - `Paginator pages 0`
  - `Non-page files 0`
  - `Static files 44`
  - `Processed images 0`
  - `Aliases 1`
  - `Cleaned 0`
  - `Total in 6046 ms`
- Warnings=`0`；errors=`0`；definitive Hugo exit=`0`。
- Render verification：`public/ai-empowerment/agent-engineering-12-context-engineering/index.html` exists；rendered Article 12 title=`Context Engineering：每一个 Step 到底应该看到什么`；shortest-thesis semantic marker=`先审查这个 Step 的effective Context可能由什么构成，再讨论它为什么答错；应用只描述、审计和比较自己的Context Snapshot。`。
- Navigation verification：rendered Article 11 contains href to Article 12 and the Article 12 title；rendered Article 12 contains href to Article 11 and the Article 11 title。
- Repository boundary：pre-build and post-build `git status --short` path sets are identical (`content/ai-empowerment/agent-engineering-11-long-running-agent.md`, `docs/agent-engineering-course/README.md`, `docs/agent-engineering-course/course-run-state.md`, `docs/agent-engineering-course/status.md`, `content/ai-empowerment/agent-engineering-12-context-engineering.md`, `docs/agent-engineering-course/articles/12-context-engineering/`)；`public/` is ignored. This README is the only repository file modified by this gate; no Published Content, Draft, Evidence, Review, globals, canonical, Git, labs, or Article 13 changes.
- Verification：`git diff --check`=`PASS` after this README update. Build gate completed; `blocker=NONE`。
