# Agent Engineering Course Factory Runtime Contract

> Contract Status：`FOUNDATION_READY`
>
> Scope：未来从 Article 02 PRECHECK 开始，经显式 `ARTICLE_KICKOFF` 按 canonical 顺序生产至 Article 44，并在每篇、每个 Part 和全课程边界上可停止、可审计、可恢复。
>
> 本文件是 multi-article orchestration contract；篇内生产规则仍以 [production-workflow.md](production-workflow.md) 为准。课程结构、依赖、Optional 与特殊模式以 [canonical series plan](../agent-engineering-series-plan.md) 为准。

## 1. Runtime invariants

1. **Repository State > Agent Context**：Git 工作区、已提交 artifact、[status.md](status.md) 与 [course-run-state.md](course-run-state.md) 是 durable state；Chat / Agent context 只是 temporary working memory。任何 worker 都必须重新读取其 Required Reads，不得以“记得已经通过”替代仓库证据。
2. **Articles Execute Sequentially**：同一时刻最多存在一个 active Article transaction。禁止并行生产 Article 02、08、24、40 等多个正文对象。下一篇由 canonical dependency 与 Optional policy 决定，不由 worker 自选。
3. **Article = Transaction Boundary**：每篇均在 PRECHECK `PASS` 后以 `ARTICLE_KICKOFF` 开始，依次完成 Workspace Init、Research、Evidence、Outline、Draft、Review、Revision、Publish、Build、Master State Update、Git Diff Verify、Article Checkpoint Commit、Commit Verify 与 Repository Reconciliation，以 `END ARTICLE N` 结束。Article N 未 `PUBLISHED`，或该篇独立 checkpoint commit 未验证，均不得开始下一篇。
4. **Gate Failure Stops Factory**：不得为追求 44 篇完成率降低 Evidence、Lab、Review、Publication 或 Build Gate；真实结果可以使 Factory 进入 `PAUSED` 或 `BLOCKED`，不能产生假 `PASS`。
5. **Evidence Controls Wording**：Researcher 先建立 Claim 与 Evidence；Author 只能在已证明边界内组织叙事。不得先写结论，再要求 Researcher 搜索支持材料。
6. **Fresh Reviewer**：Author 与 Reviewer 逻辑隔离。Reviewer 不读取 Author 的隐藏推理、信心或自评分，只基于合同允许的 repository artifacts 独立出具 Findings 与 Gate decision。
7. **Lab Reality > Article Thesis**：observed output、失败路径和环境事实决定 Lab Evidence。不得伪造、选择性忽略或重新解释失败实验来迎合正文。
8. **Source / Runtime Confirmation Are Different**：尤其在 DSH 篇中，`DOC_CONFIRMED`、`SOURCE_CONFIRMED`、`RUNTIME_CONFIRMED` 必须分别记录。源码 path 或 symbol 存在，不等于 runtime 已走过该 path。
9. **Global Durable State Has One Writer**：Master Orchestrator 是 `status.md`、`course-run-state.md`、Factory-level checkpoint pointer、Part Audit global status 与 Course `COMPLETE` state 的唯一 writer。其他 worker 只能返回 state transition candidate。

## 2. Authority and conflict order

出现冲突时按以下顺序停下来核对，而不是自动覆盖低层文件：

1. Repository instructions：`AGENTS.md`、`CLAUDE.md` 与适用目录规则；
2. [canonical series plan](../agent-engineering-series-plan.md)：课程结构、依赖、Optional、Lab、DSH 与 BuildPilot 边界；
3. [production-workflow.md](production-workflow.md)：Article Lifecycle、Evidence 与篇内 Gate；
4. [status.md](status.md)：全课程事实台账；
5. [course-run-state.md](course-run-state.md)：Factory execution pointer；
6. 当前 Article workspace 与已发布正文；
7. 临时 Agent context。

`status.md` 与 `course-run-state.md` 的事实冲突不能靠上述排序静默修复；必须进入 `PAUSED / HUMAN_DECISION_REQUIRED`，记录差异后等待人类决定。

## 3. Factory state machine

Factory 全局状态只使用：

| State | Meaning | Allowed transition |
|---|---|---|
| `READY` | durable state 已对齐，无 active worker；允许执行 `next_action`，但尚未开始事务 | `RUNNING`、`PAUSED` |
| `RUNNING` | 单一 Article、Part Audit 或 Final Audit transaction 正在执行 | `READY`、`PAUSED`、`BLOCKED`、`COMPLETE` |
| `PAUSED` | 已安全停止；需要重试、修订或人类判断后才能继续 | `READY`、`RUNNING`、`BLOCKED` |
| `BLOCKED` | 核心证据、Lab 环境或 repository condition 无法在当前权限与范围内解除 | `PAUSED`、`READY` |
| `COMPLETE` | Article 44、所有 required Lab、Part Audit 和 Course Final Audit 全部通过 | terminal；课程架构变更需新合同 |

`READY` 取代含义模糊的 `IDLE`：它明确表示已经完成恢复核对并存在一个未来允许动作。Factory state 不替代下列既有 Article Lifecycle：

```text
PLANNED
  -> RESEARCHING
  -> BLOCKED / EVIDENCE_READY
  -> OUTLINE_READY
  -> DRAFTING
  -> REVIEW
  -> FINAL
  -> PUBLISHED
```

## 4. Standard Article transaction

```text
PRECHECK
  -> ARTICLE_KICKOFF
  -> WORKSPACE_INIT
  -> RESEARCH
  -> EVIDENCE_GATE
  -> OUTLINE
  -> AUTHOR_DRAFT
  -> REVIEW
  -> REVISION            (only when Findings require it)
  -> REVIEW_RECHECK      (same Findings, fresh reviewer context)
  -> FINAL_GATE
  -> PUBLISH
  -> BUILD_VERIFY
  -> MASTER_STATE_UPDATE
  -> GIT_DIFF_VERIFY
  -> ARTICLE_CHECKPOINT_COMMIT
  -> ARTICLE_COMMIT_VERIFY
  -> REPOSITORY_RECONCILIATION
  -> END_ARTICLE
```

### 4.1 Gate rules

- `PRECHECK`：执行 Resume Contract，确认 canonical entry、依赖、Article mode、workspace scope 与 clean/isolate policy。此 Gate 通过前不得实例化 workspace。
- `ARTICLE_KICKOFF`：PRECHECK `PASS` 后由 Master 显式取得当前 Article transaction ownership，把 Factory 置为 `RUNNING`，记录 current Article、当前 Gate 与唯一 active worker。Kickoff 只建立事务身份与恢复点，不写 Research、Evidence、Outline、Draft 或 Published Content；未完成 Kickoff 不得执行 WORKSPACE_INIT。
- `WORKSPACE_INIT`：Owner 为 Master Orchestrator；这是 deterministic infrastructure action，不是 content production。Master 只根据 canonical、[workspace template](templates/article-workspace-template.md) 与 repository naming convention 机械创建当前 Article workspace。
- `RESEARCH`：Researcher 生成 Research Questions、Claim Register、Evidence Cards、counter-evidence 与 version scope。
- `EVIDENCE_GATE`：Normal Article 在 Research 完成后进入；Lab Article 只能在 `PRELIMINARY_EVIDENCE -> LAB_DESIGN -> LAB_EXECUTE -> LAB_OBSERVATION -> EVIDENCE_MERGE` 完成后进入。核心行为性 Claim 不得为 `BLOCKED`；`PARTIAL` 必须收窄；required Lab 必须已有真实结果，才可进入 `EVIDENCE_READY`。
- `OUTLINE`：Author 在 Evidence Gate 通过后建立 Detailed Outline、Teaching Spine、Figures、Examples、Learning Check 与 competency mapping。
- `AUTHOR_DRAFT`：只依据批准提纲和 Evidence 写作；新核心事实触发 `RETURN_TO_RESEARCH`。
- `REVIEW`：Fresh Reviewer 第一轮只写 Findings 和 Gate decision，不直接修改正文。
- `REVISION / REVIEW_RECHECK`：Revision Worker 只修 Finding scope；Reviewer 独立复核并决定关闭、保留或升级 Finding。
- `FINAL_GATE`：Reviewer `PASS` 且不存在未关闭的 `BLOCKER / MAJOR` 才可进入 `FINAL`。
- `PUBLISH`：Publisher 只处理发布载体、metadata、链接、publication evidence 与渲染兼容性，不改冻结知识内容；只返回 recommended state transition，不直接写 global durable state。
- `BUILD_VERIFY`：执行仓库真实 Hugo / lint / link commands。Build `PASS` 才允许发布状态成立。
- `MASTER_STATE_UPDATE`：Master 核对 Reviewer Final PASS、Publisher PASS、Build PASS、workspace、published content、canonical、`status.md` 与 run state 的一致性，统一把 Lifecycle 写为 `PUBLISHED`。这只是 checkpoint candidate；在独立 commit 验证前，Article transaction 仍未完成。
- `GIT_DIFF_VERIFY`：必须运行 `git status`、`git diff --stat` 与 `git diff`，确认所有变更只属于当前 Article transaction；发现下一篇、无关用户修改、theme / CI / 无关 docs 或无法安全隔离的修改时，返回 `REPOSITORY_CONFLICT`。
- `ARTICLE_CHECKPOINT_COMMIT`：只显式 stage 当前 Article workspace、该篇 required Lab、published content / assets 与经验证的 state / canonical / navigation 更新，并以 `Publish Agent Engineering Article NN` 创建一个独立本地 commit。禁止 `git add .`、禁止混入未来 Article、禁止 push。
- `ARTICLE_COMMIT_VERIFY`：commit 后立即运行 `git status`、`git log -1 --oneline` 与 `git show --stat --oneline HEAD`，确认 message、files scope、Lifecycle 与工作树遗留均正确。只有 `ARTICLE_COMMIT_VERIFIED = PASS` 才能进入 Repository Reconciliation。
- `REPOSITORY_RECONCILIATION`：重新读取 durable state、Published Content 与 Git history，从 repository truth 计算下一允许动作；不得在当前 Article checkpoint commit 成功并验证前切换 `current_article` 或启动下一篇。

任一 Gate 返回 `FAIL` 或缺少必需 artifact 时，Master 不得进入下一 Gate。

### 4.2 WORKSPACE_INIT contract

PRECHECK 与 `ARTICLE_KICKOFF` 均 `PASS` 后，Master 才能执行：

1. 从 canonical 读取 Article ID、Title、Part、Weight、Optional、Dependencies、Lab requirement 与 Mode；
2. 按 repository naming convention 确定 workspace slug；
3. 创建 `docs/agent-engineering-course/articles/<id>-<slug>/`；
4. 只创建 `PLANNED` 阶段允许存在的 `README.md`、`article-card.md`、`research.md`、`evidence.md`、`review.md`。

`outline.md` 只能在 Article 进入 `RESEARCHING` 后按当前 workflow 创建空骨架；`draft.md` 只能在 `DRAFTING` 创建；`assets/` 只在出现真实资产时创建。

Master 在 WORKSPACE_INIT 只能写 canonical metadata、template skeleton、initial lifecycle、initial evidence / Lab status、dependency reference 与 `NOT_STARTED` section。不得写 Research Answer、Evidence Conclusion、Claim Confirmation、Teaching Thesis、Outline、Draft 或 Review Finding。Article Card 只能机械实例化 canonical / template 已有字段；必须字段若需要实质性课程判断，返回 `HUMAN_DECISION_REQUIRED`，不得猜测。

## 5. Review contract and cycle limit

```text
MAX_REVIEW_CYCLES = 3
one cycle = Reviewer Findings -> Revision Worker changes -> Reviewer Recheck
```

- Reviewer 第一轮 Findings 本身不计作一个完成 cycle；完成一次修订并复核后，`review_cycle += 1`。
- 第三轮复核后仍有 `BLOCKER` 或 `MAJOR`：`factory_status = PAUSED`、`stop_reason = FAILED_REVIEW`、`human_decision_required = true`。
- `MINOR / EDITORIAL` 可在三轮内继续闭合；只要 Reviewer 能安全确认关闭，不自动打断整个课程。
- 不允许无限 revision loop，也不允许 Revision Worker 自行关闭自己的 Finding。

### 5.1 Review quality baseline

沿用现有五维 Review Score：Technical Accuracy、Evidence Discipline、Teaching Quality、Engineering Transfer、Readability & Compression。当前 Factory Foundation 采用最近一次获批 Article 01 Gate 的最低线作为现行课程基线：

- Total `>= 88`
- Technical Accuracy `>= 18`
- Evidence Discipline `>= 18`
- Teaching Quality `>= 17`
- Engineering Transfer `>= 17`

这是 repository course policy 的当前基线，不是行业事实。若未来 [production-workflow.md](production-workflow.md) 或 canonical 经人类批准形成不同正式阈值，以新仓库合同为准；worker 不得自行降低阈值。

## 6. Stop reason taxonomy

`stop_reason` 只能使用：

| Stop Reason | Use when |
|---|---|
| `NONE` | 没有 active stop condition |
| `BLOCKED_EVIDENCE` | 核心 Evidence 无法获得或无法安全收窄 |
| `FAILED_LAB` | required Lab 未能真实 build / run / test，或结果不足以支持 Gate |
| `FAILED_REVIEW` | Review cycle 上限后仍有 `BLOCKER / MAJOR`，或 Review contract 无法满足 |
| `FAILED_PUBLICATION` | front matter、路径、链接、Hugo、lint 或发布一致性失败 |
| `HUMAN_DECISION_REQUIRED` | canonical、课程架构、Optional 路由或高杠杆判断需要人类决定 |
| `REPOSITORY_CONFLICT` | dirty tree、状态文件、commit 历史或用户修改发生无法安全自动合并的冲突 |

`QUALITY_DEGRADATION_REVIEW` 是 Reviewer / Part Auditor 的调查 Finding，不是新的 Stop Reason。调查后若形成真实 Gate failure，再映射到上表。

## 7. Resume contract

每次启动、context reset 或 interrupted session 后，Master 必须按顺序：

1. 读取 [course-run-state.md](course-run-state.md)；
2. 读取 [status.md](status.md)；
3. 读取当前 Article workspace；若 PRECHECK 尚未通过且 workspace 不存在，只确认不存在，不创建；
4. 检查对应 Published Content 是否存在以及是否与 Lifecycle 匹配；
5. 检查 `git status`、Git `HEAD`、`git log` 与 latest relevant commit；
6. 把 `last_successful_commit` 作为 checkpoint hint 与 history 对照；
7. 检查当前 Gate 的 required artifacts 是否真实存在；
8. 对齐 canonical dependency、Article Lifecycle、Evidence / Lab 状态、active gate 与实际文件；
9. 对已标记 `PUBLISHED` 的最近 Article，核验其独立 completion commit、commit message 与 files scope；
10. 只在无冲突时确定 next safe action，并把 Factory 置为 `READY` 或 `RUNNING`。

不得默认“从 Article 02 开始”，也不得相信上一次对话的口头完成声明。不得把 `git checkout <last_successful_commit>` 当成默认恢复动作，也不得因为 pointer 比 HEAD 落后一个 state commit 就 rewind repository。若 `status.md`、run state、workspace、Published Content、required artifacts 或 Git history 不一致，写入冲突摘要并以 `PAUSED / REPOSITORY_CONFLICT` 或 `PAUSED / HUMAN_DECISION_REQUIRED` 停止。

## 8. Dirty working tree policy

- 启动和每个 Checkpoint 前都检查 working tree。
- 与当前 transaction 无关的 uncommitted changes 不得覆盖、暂存或混入 checkpoint；按仓库 workflow 隔离，无法安全隔离则 `PAUSED / REPOSITORY_CONFLICT`。
- 只显式 stage 本 Article / Audit 的目标文件或 hunks。
- 禁止 `git reset --hard`、`git clean -fd`、强制 checkout 或任何删除用户工作的清理方式。
- Master 不得把“工作区已脏”解释为授权修复无关文件。

## 9. Commit and checkpoint policy

- **One completed Article = one independently verifiable checkpoint commit.** 每个 Article transaction 只在 Reviewer Final Gate `PASS`、Publisher `PASS`、Build Verification `PASS`、Master State Reconciliation `PASS`、Lifecycle `PUBLISHED` 且 `ARTICLE_COMMIT_VERIFIED = PASS` 后完成。
- commit 未成功并验证前，禁止启动下一 Article 的 PRECHECK、workspace、Research、Evidence、Draft 或 Lab。
- Article commit 包含本次 transaction 的完整正式成果：current workspace、required Lab、published content / assets、必要 navigation / canonical publication metadata，以及经验证的 Article README、`status.md` 与 run state。不得包含 Article N+1 或 unrelated changes。
- checkpoint 前必须运行 `git status`、`git diff --stat`、`git diff`；只显式 stage 当前 transaction 文件或 hunks，再检查 `git diff --cached --stat` 与 `git diff --cached`。
- 正常 message 使用 `Publish Agent Engineering Article NN`；不得用 `WIP`、`update` 或一个 commit 合并多篇 Article。Lab 不单独创建正常生产 commit，而是与其 Article 同一 checkpoint。
- commit 后必须运行 `git status`、`git log -1 --oneline` 与 `git show --stat --oneline HEAD`。message、scope、Lifecycle 或残留任一不符，`ARTICLE_COMMIT_VERIFIED = FAIL`，Factory STOP。
- Article workspace、published content、`status.md` 与 run-state 的状态更新属于同一逻辑 transaction；如果 Git commit 自引用导致 hash 无法写入自身，`last_successful_commit` 只记录最近一个已知可恢复的 durable checkpoint hint。它不是 blind checkout target、当前 HEAD 的绝对真相或 Resume 的唯一依据。
- 只有环境即将终止且当前 Article 无法继续时，才允许 repository contract 范围内的明确 `Checkpoint Article NN at <GATE>` recovery commit。它不得标 `PUBLISHED`，不得使用 `Publish` message，run state 必须指向真实 resume Gate，且不得开始下一篇。
- 不为满足数量制造空 commit；只在本地 commit，不 push、不创建 PR，除非用户另行明确授权。

## 10. Dependency-aware read policy

Article N 的 Required Reads 至少包括：

- canonical 中 Article N 的位置、weight、Optional、dependencies 与 mode；
- current glossary；
- Article Card 与 Research Questions；
- [status.md](status.md) 和 run state；
- canonical 标识的依赖文章与直接相关的 earlier published articles；
- 当前 mode 所需 Lab、DSH 或 BuildPilot contract。

不要求每篇重读 Article 00—N-1 全部正文。Master 应按 dependency graph 选择最小充分上下文，避免历史文章无限占用 context。

## 11. Execution modes

### 11.1 Normal Article Mode

```text
PRECHECK -> ARTICLE_KICKOFF -> WORKSPACE_INIT -> RESEARCH -> EVIDENCE_GATE
         -> OUTLINE -> AUTHOR_DRAFT -> REVIEW
         -> REVISION -> REVIEW_RECHECK          (when required)
         -> FINAL_GATE -> PUBLISH -> BUILD_VERIFY
         -> MASTER_STATE_UPDATE -> GIT_DIFF_VERIFY
         -> ARTICLE_CHECKPOINT_COMMIT -> ARTICLE_COMMIT_VERIFY
         -> REPOSITORY_RECONCILIATION -> END_ARTICLE
```

Master 只启动当前 Gate 需要的 worker；不为了形式 spawn 无任务角色。

### 11.2 Lab Article Mode

当前 canonical required Lab articles：`03`、`06`、`08`、`11`、`13`、`22`。每次 PRECHECK 必须再次从 canonical 核验。

```text
PRECHECK
  -> ARTICLE_KICKOFF
  -> WORKSPACE_INIT
  -> RESEARCH
  -> PRELIMINARY_EVIDENCE
  -> LAB_DESIGN
  -> LAB_EXECUTE
  -> LAB_OBSERVATION
  -> EVIDENCE_MERGE
  -> EVIDENCE_GATE
  -> OUTLINE
  -> AUTHOR_DRAFT
  -> REVIEW
  -> REVISION / REVIEW_RECHECK     (when required)
  -> FINAL_GATE
  -> PUBLISH
  -> BUILD_VERIFY
  -> MASTER_STATE_UPDATE
  -> GIT_DIFF_VERIFY
  -> ARTICLE_CHECKPOINT_COMMIT
  -> ARTICLE_COMMIT_VERIFY
  -> REPOSITORY_RECONCILIATION
  -> END_ARTICLE
```

Lab pipeline ownership：

- `PRELIMINARY_EVIDENCE`：Researcher 先完成 official doc / spec / source Evidence。依赖 Lab 的 Claim 不得提前标 `CONFIRMED`；保留正式 Evidence Status，并增加 operational annotation，例如 `Evidence Status: PARTIAL` + `Lab Dependency: REQUIRED`。
- `LAB_DESIGN`：Owner 为 Researcher。Researcher 根据 Claim、Evidence Gap 与 Research Question，在 `labs/lab-<nn>-<slug>/` 中使用 [Lab template](templates/lab-template.md)创建 durable Lab Card。Design 至少包含 Lab ID、Related Article / Claim IDs、Research Question、Hypothesis、What Would Falsify It、Fixture Boundary、Environment、Inputs、Variables、Expected Observable、Fault Injection、Commands / Execution Needs、Acceptance Criteria、Evidence Mapping、Limitations、Safety / Permission Constraints。`Expected Observable != Observed Result`。
- `LAB_EXECUTE / LAB_OBSERVATION`：Lab Engineer 只执行冻结 Design；不得修改 hypothesis、acceptance criteria 或问题范围。必须保存 Environment、Commands、Exit Codes、Build / Test Result、Runtime Output、Fault Injection Result、Observed / Unexpected Behavior、Reproduction Notes 与 Limitations，无论 PASS 或 FAIL。
- `EVIDENCE_MERGE`：Owner 回到 Researcher。Researcher读取 Preliminary Evidence、Lab Design、raw observation、failure output 与 runtime notes，再更新 Evidence Cards、Claim Status、Proves、Does Not Prove、Limitations 和 Course Usage。解释链必须是 `Experiment -> Observation -> Evidence Interpretation -> Claim Status`。
- `EVIDENCE_GATE`：只在 Evidence Merge 后运行。Researcher可以按真实结果收窄 Claim 或标 `PARTIAL / BLOCKED`，不能修改实验结果。required Lab 的 build / run / fault injection 无法真实完成时：`BLOCKED / FAILED_LAB`，Factory STOP。

只有 README、sample code 或 expected result 不算 Lab 完成。当前 template 和 `labs/` convention 是唯一 Lab artifact 路径，不创建第二套目录或 schema。

### 11.3 DSH Source Mode

适用于 Article `28—37`，以 canonical 为准。

- Article 28 PRECHECK 必须冻结 repository URL、pinned commit SHA、build / run entry、environment 与 source boundary。
- Article 29—37 使用同一 pinned commit；除非人类批准并由 Factory 执行显式 DSH version migration，禁止悄悄切换 latest main。
- Evidence 必须区分 `DOC_CONFIRMED / SOURCE_CONFIRMED / RUNTIME_CONFIRMED`，并记录 file、symbol、call path、runtime observation 与 limitations。
- source existence 不能升级为 runtime confirmation；无法建立所需运行证据时按 Claim 收窄或 `BLOCKED_EVIDENCE` 停止。

### 11.4 BuildPilot Mode

适用于 Article `38—44`。本课程冻结边界为 `BuildPilot Design v1`：允许产出问题空间、架构、接口、状态机、Schema、失败语义、治理与验证计划；不得实现或宣称存在 BuildPilot production Runtime。只有 canonical 经人类正式修改后才可改变该边界。

## 12. Optional Article policy

Article 23 在 canonical 中是 `Advanced / Optional`。Factory 不得把 Optional 静默改成 required，也不得自行删除它：

- Part IV PRECHECK / Audit 必须读取 canonical 的当前 Optional 决策；
- 若人类选择生产 23，则保持单篇顺序 `22 -> 23 -> Part IV Audit -> 24`；
- 若 canonical 仍允许跳过，则保持 Article 23 `PLANNED` 并记录 Optional 未生产，按 `22 -> Part IV Audit -> 24` 前进；
- 这不是新增 Article Lifecycle 状态，也不影响 required-course completion check。

## 13. Part Audit

Part boundary：Part I `01—04`、Part II `05—11`、Part III `12—17`、Part IV required `18—22`（23 按 Optional policy）、Part V `24—27`、Part VI `28—37`、Part VII `38—44`。

每个 Part 的最后一个实际生产对象完成独立 Article commit 并通过 `ARTICLE_COMMIT_VERIFY` 后，Master 必须启动 fresh Part Auditor，检查：

- Concept Drift
- Glossary Drift
- Contradiction
- Duplication
- Missing Dependency
- Forward Reference
- Learning Progression
- Job Competency Coverage
- required Lab evidence
- 当前 Part 特有的 DSH / BuildPilot boundary

存在 `BLOCKER / MAJOR` 时，只把受影响 Article 退回必要状态修复；不得整 Part 无差别重写。修复完成后使用 `Fix Agent Engineering Article NN after Part X audit` 独立 commit，再重新审计。Part Audit `PASS` 后，其 durable Audit Report / status update 必须使用 `Audit Agent Engineering Part X` 独立 commit 并完成 commit verification，才可进入下一 Part；不得混入下一 Article commit。Article 00—01 已作为 Foundation 发布；未来 Part I Audit 在 Article 04 后覆盖 01—04。

## 14. Course Final Audit

Article 44 `PUBLISHED` 后必须继续执行 Course Final Audit；此时 Factory 尚不能标 `COMPLETE`。Final Audit 至少确认：

- all required articles published；
- all required Labs verified；
- all Part Audits passed；
- DSH pinned-source integrity；
- BuildPilot Design consistency；
- broken links、series ordering 与 glossary consistency；
- course competency coverage；
- final Hugo build `PASS`。

全部通过后才写 `factory_status = COMPLETE`，并以独立 `Complete Agent Engineering course audit` commit 保存 Final Audit；不得混入 Article 44 checkpoint。

## 15. Quality degradation signals

Reviewer 与 Part Auditor 应调查下列信号，但不能仅凭信号自动判失败：

- 后期复杂 Article 的 Evidence 数异常减少；
- 连续大量 Article 都是 `0 MAJOR / 0 MINOR`；
- 所有评分长期集中在同一狭窄区间；
- Lab 只有 expected output，没有 observed output；
- 复杂源码篇没有 symbol / call path；
- cross-provider Article 永远只引用一家 Provider；
- 后期 Draft 明显变短且概念密度下降；
- 大量复制上一篇模板，却没有新的教学问题。

调查结果以 `QUALITY_DEGRADATION_REVIEW` Finding 进入 Review 或 Part Audit；必要时再映射到真实 Gate failure。

## 16. Human intervention philosophy

目标是只在高杠杆判断处请求人类，而不是追求 Human never participates。以下情况需要人类：核心 Evidence 无法解决、三轮 Review 仍有重大问题、canonical 矛盾、DSH source/runtime 歧义、Lab 环境无法安全恢复、repository state 冲突、课程架构或 Optional 路由需要改变。

普通 `MINOR / EDITORIAL` 不应自动停止整个 Factory；在合同允许的修订范围内闭合即可。

## 17. State update granularity

[course-run-state.md](course-run-state.md) 在以下 transaction-level 事件更新：`ARTICLE_KICKOFF`、worker start、Gate pass、Gate fail、Article `PUBLISHED` candidate、Article Commit Verify、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。Master Orchestrator 是唯一 global state writer；Publisher、Part Auditor 与其他 worker 只返回 recommended transition / update candidate。Master 验证 artifact 和 Gate result 后统一写 Article README lifecycle、`status.md`、run state、Factory checkpoint pointer、Part Audit global status、Course `COMPLETE` state 与必要 canonical publication metadata。不为每个小动作创建 commit；状态更新以 Gate / Transaction 为粒度，但每篇完成 Article 与每次 Part / Final Audit 都必须遵守各自独立 commit boundary。

## 18. Static interface dry-run

### Dry Run A｜Article 02 Normal Mode

```text
READY
  -> START_ARTICLE_02_PRECHECK                  [Master]
  -> PRECHECK PASS                              [Master decision from repository state]
  -> ARTICLE_KICKOFF                            [Master records transaction ownership]
  -> WORKSPACE_INIT                             [Master creates PLANNED skeleton]
  -> RESEARCH / EVIDENCE_GATE                   [Researcher]
  -> OUTLINE / AUTHOR_DRAFT                     [Author]
  -> REVIEW / REVISION / RECHECK                [Reviewer / Revision Worker]
  -> PUBLISH / BUILD_VERIFY                     [Publisher]
  -> MASTER_STATE_UPDATE / GIT_DIFF_VERIFY      [Master]
  -> ARTICLE_CHECKPOINT_COMMIT / VERIFY         [Master]
  -> REPOSITORY_RECONCILIATION                  [Master]
```

Static Result：`PASS`。每一步都有 owner、input、allowed writes、output 与 stop condition；本次只做合同模拟，没有创建 Article 02 workspace。

### Dry Run B｜Article 03 Lab Mode

```text
PRECHECK / ARTICLE_KICKOFF / WORKSPACE_INIT     [Master]
  -> PRELIMINARY_EVIDENCE / LAB_DESIGN          [Researcher]
  -> LAB_EXECUTE / LAB_OBSERVATION              [Lab Engineer]
  -> EVIDENCE_MERGE / EVIDENCE_GATE             [Researcher]
  -> OUTLINE / AUTHOR_DRAFT                     [Author]
  -> REVIEW / REVISION / RECHECK                [Reviewer / Revision Worker]
  -> PUBLISH / BUILD_VERIFY                     [Publisher]
  -> MASTER_STATE_UPDATE / GIT_DIFF_VERIFY      [Master]
  -> ARTICLE_CHECKPOINT_COMMIT / VERIFY         [Master]
  -> REPOSITORY_RECONCILIATION                  [Master]
```

Static Result：`PASS`。Lab Design、raw observation 与 Evidence interpretation 各有唯一 owner；本次没有实例化 Lab 01 或执行实验。

### Contract interface audit

| Interface | Producer | Durable Artifact / Result | Consumer |
|---|---|---|---|
| PRECHECK -> ARTICLE_KICKOFF | Master PRECHECK | Precheck result + clean transaction boundary | Master ARTICLE_KICKOFF |
| ARTICLE_KICKOFF -> WORKSPACE_INIT | Master | durable transaction ownership | Master WORKSPACE_INIT |
| WORKSPACE_INIT -> Researcher | Master | PLANNED workspace skeleton | Researcher |
| Researcher -> Lab Engineer | Researcher | frozen Lab Design | Lab Engineer |
| Lab Engineer -> Researcher | Lab Engineer | raw Lab Observation + failure output | Researcher EVIDENCE_MERGE |
| Researcher -> Author | Researcher | final Evidence + Evidence Gate result | Author |
| Author -> Reviewer | Author | Outline + Draft + coverage | Reviewer |
| Reviewer -> Revision | Reviewer | Findings | Revision Worker |
| Revision -> Reviewer | Revision Worker | Revision Disposition + changed artifacts | Reviewer Recheck |
| Reviewer -> Publisher | Reviewer | Final Gate PASS + frozen Draft | Publisher |
| Publisher -> Master | Publisher | structured Publication Result + recommended transition | Master |
| Master -> ARTICLE_CHECKPOINT_COMMIT | Master | reconciled global state + verified current-Article-only diff | Git checkpoint |
| ARTICLE_CHECKPOINT_COMMIT -> NEXT_ARTICLE | Master | verified commit + repository reconciliation | next PRECHECK |

Audit Result：`Missing Producer = NONE`、`Missing Consumer = NONE`、`Ownership Conflict = NONE`。

## 19. Foundation Review History

| Stage | Record |
|---|---|
| Foundation initial commit | `eb53803 Prepare Agent Engineering Course Factory` |
| Independent review | 发现 Workspace Init、Lab Gate order、Lab Design owner、global state writer 与 checkpoint semantics 五个接口断点 |
| Targeted fix | 只修改 Factory contract、八角色合同、run-state field rule、篇内 workflow 与 Lab template / convention；未新增角色或启动 Article 02 |
| Final recheck | `CF-IR-F01 CLOSED`、`CF-IR-F02 CLOSED`、`CF-IR-F03 CLOSED`、`CF-IR-F04 CLOSED`、`CF-IR-F05 CLOSED` |
| Article Kickoff hotfix | 增加显式 `ARTICLE_KICKOFF`、逐篇 checkpoint commit、commit verification、Part / Final Audit 独立 commit 与 next-Article stop line；未启动 Article 02 |

## 20. Foundation stop line

当前只冻结合同：`factory_status = READY`、`current_article = 02`、`current_gate = PRECHECK`。`START_ARTICLE_02_PRECHECK` 是未来允许动作，不代表已执行；PRECHECK `PASS` 后仍必须显式执行 `ARTICLE_KICKOFF`，才能创建 workspace。本次 Foundation Hotfix 不得创建 Article 02 workspace、研究 Prompt Engineering、运行 Lab、读取 DSH 源码或实现 BuildPilot。
