# Agent Engineering Course Factory Runtime Contract

> Contract Status：`FOUNDATION_READY`
>
> Scope：未来从 Article 02 PRECHECK 开始，经显式 `ARTICLE_KICKOFF` 按 canonical 顺序生产至 Article 44，并在每篇、每个 Part 和全课程边界上可停止、可审计、可恢复。
>
> 本文件是 multi-article orchestration contract；篇内生产规则仍以 [production-workflow.md](production-workflow.md) 为准。课程结构、依赖、Optional 与特殊模式以 [canonical series plan](../agent-engineering-series-plan.md) 为准。

## 1. Runtime invariants

1. **Repository State > Agent Context**：Git 工作区、已提交 artifact、[status.md](status.md) 与 [course-run-state.md](course-run-state.md) 是 durable state；Chat / Agent context 只是 temporary working memory。任何 worker 都必须重新读取其 Required Reads，不得以“记得已经通过”替代仓库证据。
2. **Articles Execute Sequentially**：同一时刻最多存在一个 active Article transaction。禁止并行生产 Article 02、08、24、40 等多个正文对象。下一篇由 canonical dependency 与 Optional policy 决定，不由 worker 自选。
3. **Article = Transaction Boundary**：每篇均在 PRECHECK `PASS` 后以 `ARTICLE_KICKOFF` 开始，依次完成 Workspace Init、Research、Evidence、Outline、Draft、Review、Revision、Publish、Build、Master State Update、Pre-Commit Reconciliation、Git Diff Verify、Article Checkpoint Commit、Commit Verify、Push Main、Remote Verify 与只读 Post-Commit Reconciliation，以 `END ARTICLE N` 结束。Article N 未 `PUBLISHED`，或该篇独立 checkpoint commit / push / remote verification 未通过，均不得开始下一篇。
4. **Gate Failure Stops Factory**：不得为追求 44 篇完成率降低 Evidence、Lab、Review、Publication 或 Build Gate；真实结果可以使 Factory 进入 `PAUSED` 或 `BLOCKED`，不能产生假 `PASS`。
5. **Evidence Controls Wording**：Researcher 先建立 Claim 与 Evidence；Author 只能在已证明边界内组织叙事。不得先写结论，再要求 Researcher 搜索支持材料。
6. **Fresh Reviewer**：Author 与 Reviewer 逻辑隔离。Reviewer 不读取 Author 的隐藏推理、信心或自评分，只基于合同允许的 repository artifacts 独立出具 Findings 与 Gate decision。
7. **Lab Reality > Article Thesis**：observed output、失败路径和环境事实决定 Lab Evidence。不得伪造、选择性忽略或重新解释失败实验来迎合正文。
8. **Source / Runtime Confirmation Are Different**：尤其在 DSH 篇中，`DOC_CONFIRMED`、`SOURCE_CONFIRMED`、`RUNTIME_CONFIRMED` 必须分别记录。源码 path 或 symbol 存在，不等于 runtime 已走过该 path。
9. **Global Durable State Has One Writer**：Master Orchestrator 是 `status.md`、`course-run-state.md`、Factory-level checkpoint pointer、Part Audit global status 与 Course `COMPLETE` state 的唯一 writer。其他 worker 只能返回 state transition candidate。
10. **Worker Completion Does Not End Transaction**：worker 完成或失败并返回 assigned execution，只代表当前 Gate 的 worker execution 已结束；控制流 MUST 回到 Master。A worker result is a state transition event.

    It is NOT：

    - Article transaction completed；
    - Factory session completed；
    - human confirmation required。

    每次收到 worker result 后，Master MUST：

    1. 核验 worker 声明的文件清单与 repository 中实际产生的 artifacts；
    2. 按当前 Gate contract 验证 required outputs、写入边界与 Gate decision；
    3. 更新由 Master 独占写入的 durable state；
    4. 根据已验证结果确定 next allowed Gate；
    5. Gate `PASS` 时派发下一 Gate 所需 worker；Gate `FAIL`、artifact 缺失或越权时，进入合同定义的 pause、block、retry 或 human-decision route。

    Gate `PASS` 后，Master 自动继续当前 Article transaction。The Master continues the Article transaction automatically. Master 不得仅因 worker task 已结束就等待新的人类提示。`active_worker = NONE`、worker task 显示 completed / failed，或 worker 已返回 handoff，均不是 stop condition。A completed worker task is NOT a stop condition. 只有显式、已落盘的合同 stop condition 或 `END_ARTICLE` 才能结束这条控制流。
11. **Main-Only Production**：Course Factory 的 Article、Part Audit 与 Course Audit 正式 transaction 只允许直接在 `main` 上执行。八种 role 均无 branch creation authority；禁止 `git checkout -b`、`git switch -c`、`git branch <new-branch>`，也禁止创建 `codex/*`、`article-*`、`feature/*`、`factory/*`、`temp/*` 或任何其他 production branch。不得用 branch 隔离绕过 dirty tree、divergence 或 state conflict。
12. **One Article = One Commit = One Push**：Article N 的全部 workspace、publication、navigation、canonical 与最终 durable state 必须在唯一 `Publish Agent Engineering Article NN` completion commit 前冻结并一并提交。该 commit 验证后只允许一次 `main -> origin/main` push；Pre-Commit Reconciliation 之后 tracked / worktree repository-file writes 必须为 `ZERO`。Required `git push` / `git fetch` 只允许更新 Git remote / tracking refs，不允许改变 checkpoint content。Git history 与 remote main 是 completion SHA 的权威来源，不得为回写 SHA、`END_ARTICLE` 或 reconciliation 结果创建第二个 Article completion commit。

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

### Persisted checkpoint and resolved runtime state

Markdown persists checkpoint facts, expected completion identity, resolution rule, and next candidate; it does not persist live commit/push/remote progress.

```text
Persisted State != Resolved Runtime State.
Completion is derived from Git reality.

Canonical
-> Git Reality
-> Persisted Checkpoint
-> Derived Runtime State
-> Allowed Next Action
```

Markdown 只持久化 checkpoint 事实、expected completion identity、completion resolution rule 与 next transaction candidate；它不持久化 live commit / push / remote progress。Article completion 必须由 canonical、Git history 与当前 remote refs 在 runtime 只读推导，不新增 completion store，也不得把历史 trace 或 stale current prose 当作 execution authority。

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
  -> PRE_COMMIT_RECONCILIATION
  -> GIT_DIFF_VERIFY
  -> ARTICLE_CHECKPOINT_COMMIT
  -> ARTICLE_COMMIT_VERIFY
  -> PUSH_MAIN
  -> REMOTE_VERIFY
  -> POST_COMMIT_RECONCILIATION_READ_ONLY
  -> END_ARTICLE
```

### 4.1 Gate rules

Article N+1 may pass only if `ResolveArticleCompletion(N) == END_ARTICLE`.

- `PRECHECK`：执行 Resume Contract，确认 `current branch == main`、canonical entry、依赖、Article mode、workspace scope、clean tree 与 remote alignment。Article N+1 只有在 `ResolveArticleCompletion(N) == END_ARTICLE` 时才可通过 PRECHECK；pointer candidate 本身不授予 PRECHECK 或 Kickoff authority。此 Gate 通过前不得实例化 workspace；不得以创建 branch 作为隔离策略。
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
- `MASTER_STATE_UPDATE`：Master 核对 Reviewer Final PASS、Publisher PASS、Build PASS、workspace、published content、canonical、`status.md` 与 run state 的一致性，准备 Lifecycle `PUBLISHED` candidate；在 Pre-Commit Reconciliation、独立 commit、push 与 remote verification 通过前，Article transaction 仍未完成。
- `PRE_COMMIT_RECONCILIATION`：这是 checkpoint 前最后一个可写 Gate。Master 必须在这里完成 Article Lifecycle=`PUBLISHED`、`last_published_article` candidate、下一 Article `PRECHECK` pointer candidate、Article completion metadata、Article README、course README、`status.md`、`course-run-state.md`、截至本 Gate 的 final subagent trace，以及必要 canonical / navigation 更新。最终 state 可以表达 `Article N = PUBLISHED` 与 `next transaction = Article N+1 PRECHECK`，但 pointer 仍不等于 Kickoff；在 completion commit 真实存在前，未提交 candidate 不具有启动下一篇的 authority。不得预写尚未发生的 Git Diff Verify、commit SHA、push 或 remote verification result。
- 未来安全的 completion wording 固定为以下 stable six-field interface：

    ```text
    Lifecycle Candidate: PUBLISHED
    Persisted Checkpoint: PRE_COMMIT_RECONCILIATION PASS
    Completion Resolution: DERIVED_FROM_GIT_HISTORY
    Completion Evidence Source: GIT_HISTORY + REMOTE_REFS
    Expected Completion Message: Publish Agent Engineering Article NN
    Next Transaction Candidate: Article N+1 PRECHECK / NOT_STARTED
    ```

    不得把 `completion commit pending`、`GIT_DIFF_VERIFY NEXT`、待 push 或待 remote verification 写成永久 Current State；checkpoint 不得自引用 SHA，也不得以 post-commit write 或第二个 reconciliation commit 回写 completion。
- `GIT_DIFF_VERIFY`：必须运行 `git status`、`git diff --stat` 与 `git diff`，确认所有变更只属于当前 Article transaction；发现下一篇、无关用户修改、theme / CI / 无关 docs 或无法安全隔离的修改时，返回 `REPOSITORY_CONFLICT`。
- `ARTICLE_CHECKPOINT_COMMIT`：只显式 stage 当前 Article workspace、该篇 required Lab、published content / assets 与已经完成 Pre-Commit Reconciliation 的 state / canonical / navigation 更新，并以 `Publish Agent Engineering Article NN` 创建本 Article 唯一正式 completion commit。禁止 `git add .`、禁止混入未来 Article；commit 内容不得要求自引用自身 SHA。
- `ARTICLE_COMMIT_VERIFY`：commit 后只读运行 `git status`、`git log -1 --oneline`、`git show --stat --oneline HEAD` 与 `git diff HEAD^ HEAD --check`，确认 message、files scope、Lifecycle 与工作树遗留均正确。禁止修改任何 repository file。
- `PUSH_MAIN`：Commit Verify `PASS` 后只允许执行一次 `git push origin main`。禁止 push production branch、临时 branch 或不同 refspec；push failure 立即停止。
- `REMOTE_VERIFY`：push 后只读执行 `git fetch origin main`、`git rev-parse HEAD`、`git rev-parse origin/main` 与 `git ls-remote origin refs/heads/main`，要求 `local HEAD == origin/main == remote refs/heads/main`。
- `POST_COMMIT_RECONCILIATION_READ_ONLY`：只读取 Git history、local / origin / remote main、state files、workspace、Published Content 与 current pointer并返回 `PASS / FAIL`。禁止修改 `course-run-state.md`、`status.md`、任何 README / trace、canonical、Published Content 或其他 repository file；也不得为了记录 `END_ARTICLE`、真实 SHA 或 reconciliation result 再 commit。
- `END_ARTICLE`：只有 Commit Verify、Push Main、Remote Verify 与只读 Post-Commit Reconciliation 全部 `PASS` 才在 runtime 逻辑上成立。下一次 Resume 依靠 Git history 与 checkpoint 内已提交 pointer 决定 `START_ARTICLE_N+1_PRECHECK`，不需要 post-commit write。

### Deterministic completion resolver

`ResolveArticleCompletion(N)` 是只读 runtime resolver，只返回 `END_ARTICLE` 或 `INCOMPLETE / <reason>`，不修改 tracked / worktree repository files；只允许下述 Git remote refs、objects 与 `FETCH_HEAD` evidence refresh。解析顺序固定为：

1. 从 canonical identity 读取 Article N 的 published path、依赖、required Lab 与当前 transaction 允许的 artifact 类别。
2. 在任何 union history traversal 前执行 `fresh remote materialization`：
    - 先以 `git ls-remote origin refs/heads/main` 取得且只取得一个 advertised `live_main_sha`；空结果、多结果或 query failure 都是 `remote query/fetch failure`。
    - 再执行 `git fetch --no-tags origin refs/heads/main:refs/remotes/origin/main`，把该 branch tip 刷新到 Git objects、`refs/remotes/origin/main` 与 `FETCH_HEAD`；要求 refreshed `FETCH_HEAD == live_main_sha`，并以 `git cat-file -e <live_main_sha>^{commit}` 与 `git rev-list <live_main_sha>` 确认该 SHA 可在本地遍历。fetch 只是 `evidence refresh, not a completion state store`；不得创建 custom completion store。
    - `remote query/fetch failure` 或 advertised live SHA 无法在本地 materialize / traverse 时，立即返回 `INCOMPLETE / NEEDS_REMOTE_VERIFY`；fresh query 与 `FETCH_HEAD` / refreshed `origin/main` tip 不相等时返回 `INCOMPLETE / REPOSITORY_CONFLICT / REMOTE_MISMATCH`。两类失败都必须 fail closed，`do not return NEEDS_COMMIT`，也不得猜测 completion identity。
    - 只有 materialization 成功后，identity search universe 才固定为 `union of all existing local/origin/live main tips`：local `refs/heads/main`、refreshed `refs/remotes/origin/main` 与 materialized live `FETCH_HEAD`。missing local ref 记录为 absent；对 present tips 先按 SHA 去重，再遍历其 reachable histories 的并集，按 exact subject 查找 commit，并在计数前 `deduplicate by commit SHA`。
3. 该 union 中零个 exact-subject commit 返回 `INCOMPLETE / NEEDS_COMMIT`。
4. 该 union 中多个不同 SHA 的 exact-subject commit 返回 `INCOMPLETE / REPOSITORY_CONFLICT / AMBIGUOUS_COMPLETION_IDENTITY`；pause and report，`never auto-select, rewrite, or delete` 任一候选。
5. 验证该 commit scope 只包含当前 Article workspace、Published Content / assets、required Lab、允许的前篇 navigation、series index、canonical series plan、Article / Course / status / run-state checkpoint 与 final trace。
6. scope 含 Article N+1 workspace / content、future Lab、unrelated docs、theme、CI 或其他 transaction asset 时，返回 `INCOMPLETE / REPOSITORY_CONFLICT / INVALID_COMPLETION_SCOPE`。
7. 得到唯一 completion commit 后，若 `missing local main`，或 local `refs/heads/main`（branch=`main` 时即 current `HEAD`）不包含该 commit，返回 `INCOMPLETE / NEEDS_LOCAL_COMPLETION`。
8. local main 已包含，但 `missing origin/main`，或 `refs/remotes/origin/main` 不包含该 commit，返回 `INCOMPLETE / NEEDS_PUSH`。
9. local 与 origin 已包含，但 `missing live main`，或 live `refs/heads/main` tip 不包含该 commit，返回 `INCOMPLETE / NEEDS_REMOTE_VERIFY`。
10. containment 全部通过后，当前 local `refs/heads/main` / `HEAD`、`origin/main` 与 live `refs/heads/main` tips 必须彼此相等；不相等返回 `INCOMPLETE / REPOSITORY_CONFLICT / REMOTE_MISMATCH`。
11. 验证 branch=`main`、worktree 无冲突、artifact 与 checkpoint 一致，并完成 `POST_COMMIT_RECONCILIATION_READ_ONLY = PASS`；任一冲突都返回对应的 `INCOMPLETE` reason 并停止。
12. 全部通过时返回 `END_ARTICLE`，并固定 `repository write required = false`。

“包含 completion commit”与“当前 refs 相等”是两个独立条件：三个当前 main ref 必须包含该 exact completion commit，且当前 `HEAD == origin/main == live refs/heads/main`；后续合法 commit 可以位于旧 completion SHA 之后，因此当前 refs 不必永远等于旧 completion SHA。

任一 Gate 返回 `FAIL` 或缺少必需 artifact 时，Master 不得进入下一 Gate。

### 4.2 Gate failure authority and hard execution lock

**Recovery Candidate != Recovery Authority**。Gate 或 worker 可以用 `next_allowed_gate` 返回 `PUBLISH`、`LAB_EXECUTE`、`RESEARCH` 或其他 recovery candidate；它只回答“未来获得恢复授权后，可以从哪里重新评估”，不授权 Master 在当前 execution 中派发 worker、应用修复或重跑 Gate。Failure can propose recovery. Only authority can execute recovery.

Master 收到 `FAIL / BLOCKED` 后必须先归一化 blocker，再判断 active `continuous_run.stop_on.<condition>`，最后才允许考虑普通 recovery route。稳定映射优先复用现有 taxonomy：Build / publication verify failure -> `FAILED_PUBLICATION`，Git / state conflict -> `REPOSITORY_CONFLICT`，core Evidence blocked -> `BLOCKED_EVIDENCE`，required Lab failure -> `FAILED_LAB`，Review cycle exhausted -> `FAILED_REVIEW`；worker 不得发明 global blocker。

当对应 `stop_on` 为 `true` 时，**STOP POLICY WINS**，立即形成 hard execution lock：

```yaml
factory_status: PAUSED
active_blocker: <normalized blocker>
stop_reason: HUMAN_DECISION_REQUIRED
human_decision_required: true
```

若 failed execution 已结束，Master 同时把 active worker fields 清为 `NONE`；若仍在运行，则只等待或终止同一 execution，禁止重复 dispatch。该 hard lock 在逻辑上关闭本次 continuous auto-continue authority；不要求新增 schema 字段，也不要求改写 `continuous_run.auto_continue_after_end_article`，但效果必须等价于当前 execution 不再自动进入任何 recovery 或后续 Gate。

Hard lock 后、获得新的 Human Resume 前，禁止 dispatch recovery worker、执行 recovery candidate、重跑 failed Gate、应用 proposed fix、修改 Article content、继续当前 Article、进入下一 Gate、commit recovered Article、启动下一 Article 或运行下一 PRECHECK。Master 只允许核验 failure evidence、归一化并记录 blocker、保留 recovery candidate、在 persistence cut 前持久化 `PAUSED`、清理已结束的 active worker、报告 human action required，然后停止。若 failure 发生在 `PRE_COMMIT_RECONCILIATION` persistence cut 之后，只能以 runtime result、Git 与已有 trace 报告 `PAUSED / HUMAN RESUME REQUIRED`；不得为记录 pause 产生 repository write，继续遵守 `POST_COMMIT_WRITES_ZERO`。

### 4.3 WORKSPACE_INIT contract

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

Resume: `Repository Reconciliation -> resolver -> derived Factory state -> compare candidate pointer -> commit/push/verify/pause/next consideration.`

由 `stop_on` 命中形成的 hard lock 只能由**新的外部 human instruction**解除，例如“继续”“按 recovery candidate 修复”“恢复 Article 14”或“可以修复这个 build failure”。worker 返回 `next_allowed_gate`、Master 认为修复明显、Reviewer 建议 recovery、candidate 已存在、问题为 MINOR 或单行修复、retry 看似安全、以及 Master 先前计划继续，都不是 Human Resume。普通 context reset / interrupted-session reconciliation 仍可自动执行只读核对，但不得借此解除 hard lock。

收到有效 Human Resume 后也不得直接执行旧 candidate。Master 必须先完成以下 Repository Reconciliation，再根据当前事实选择 Resume current Gate、Return to previous Gate 或再次 `PAUSED`：

```text
Repository Reconciliation
-> resolver
-> derived Factory state
-> compare candidate pointer
-> commit / push / verify / pause / next consideration
```

Resume resolver target 固定为 `N = last_published_article`，且 N 必须同时是 `latest Article carrying the persisted PUBLISHED completion checkpoint`。在决定 next action 前必须交叉核对 `last_published_article = N`、`current_article = N+1` 仅为 next pointer candidate，以及 `Expected Completion Message = Publish Agent Engineering Article NN`（NN 由 N 格式化）；任一不一致都进入 `PAUSED / REPOSITORY_CONFLICT`。交叉核对通过后才执行 `ResolveArticleCompletion(N)` 并比较 derived Factory state 与 pointer candidate。`Do not resolve Article N+1 first.` Markdown 中的历史或 stale prose 不具有 execution authority。

每次启动、context reset 或 interrupted session 后，Master 必须按顺序：

1. 运行 `git status` 与 `git branch --show-current`，验证 `current branch == main`；
2. 若不在 `main`，只有在 worktree clean、无未提交用户修改且切换不会覆盖工作时，Master 才可运行 `git switch main` 与 `git pull --ff-only origin main`；不能安全恢复时立即 `PAUSED / REPOSITORY_CONFLICT`，不得创建新 branch；
3. 读取 [course-run-state.md](course-run-state.md)；
4. 读取 [status.md](status.md)；
5. 读取当前 Article workspace；若 PRECHECK 尚未通过且 workspace 不存在，只确认不存在，不创建；
6. 检查对应 Published Content 是否存在以及是否与 Lifecycle 匹配；
7. 检查 Git `HEAD`、`origin/main`、remote `refs/heads/main`、`git log` 与 latest relevant commit；
8. 把 `last_successful_commit` 只作为 checkpoint hint 与 history 对照；若为 `PENDING_SELF` 或旧值，以 Git history 为权威；
9. 检查当前 Gate 的 required artifacts 是否真实存在；
10. 对齐 canonical dependency、Article Lifecycle、Evidence / Lab 状态、active gate 与实际文件；
11. 对已标记 `PUBLISHED` 的最近 Article，核验唯一 `Publish Agent Engineering Article NN` completion commit、commit message、files scope 与 main remote equality；
12. 如果 context 丢失发生在 checkpoint 后，重新执行 `ARTICLE_COMMIT_VERIFY -> REMOTE_VERIFY -> POST_COMMIT_RECONCILIATION_READ_ONLY`；这些恢复检查仍然禁止 repository write；
13. 只在无冲突时确定 next safe action，并把 Factory 置为 `READY` 或 `RUNNING`。

不得默认“从 Article 02 开始”，也不得相信上一次对话的口头完成声明。不得把 `git checkout <last_successful_commit>` 当成默认恢复动作，也不得因为 pointer 落后 HEAD 就 rewind repository。Git history 是当前 Article completion SHA 的权威来源；不得为了同步 SHA 再写 Markdown。若 branch、`status.md`、run state、workspace、Published Content、required artifacts、Git history 或 remote main 不一致，以 `PAUSED / REPOSITORY_CONFLICT` 或 `PAUSED / HUMAN_DECISION_REQUIRED` 停止。

## 8. Main branch and dirty working tree policy

- Course Factory 的唯一 production branch 是 `main`。任何 role 都没有 branch creation authority，禁止创建或 push `codex/*`、`article-*`、`feature/*`、`factory/*`、`temp/*` 或其他 production branch。
- `PRECHECK` 与每次 Resume Reconciliation 都必须验证 `git branch --show-current` 为 `main`。只有 clean worktree 才允许 Master 安全切换到现有 `main` 并执行 `git pull --ff-only origin main`；禁止用新 branch 绕过冲突。
- 启动和每个 Checkpoint 前都检查 working tree。
- 与当前 transaction 无关的 uncommitted changes 不得覆盖、暂存或混入 checkpoint；按仓库 workflow 隔离，无法安全隔离则 `PAUSED / REPOSITORY_CONFLICT`。
- 只显式 stage 本 Article / Audit 的目标文件或 hunks。
- 禁止 `git reset --hard`、`git clean -fd`、强制 checkout 或任何删除用户工作的清理方式。
- Master 不得把“工作区已脏”解释为授权修复无关文件。

## 9. Commit and checkpoint policy

- **One completed Article = one independently verifiable completion commit = one push to main.** 每个 Article transaction 只在 Reviewer Final Gate、Publisher、Build、Pre-Commit Reconciliation、Commit Verify、Push Main、Remote Verify 与 Post-Commit Reconciliation Read-Only 全部 `PASS` 后完成。
- completion commit 未成功、未 push 或 remote 未验证前，禁止启动下一 Article 的 PRECHECK、workspace、Research、Evidence、Draft 或 Lab。
- Article commit 包含本次 transaction 的完整正式成果：current workspace、required Lab、published content / assets、必要 navigation / canonical publication metadata，以及已经表达 `Article N = PUBLISHED / Article N+1 = PRECHECK candidate` 的 Article README、course README、`status.md`、run state 与 final trace。不得包含 Article N+1 workspace 或 unrelated changes。
- Course Index publication status 由手工维护时，`PRE_COMMIT_RECONCILIATION` 必须同步当前 Article 的 Published / Planned 状态与真实 Hugo `relref`；未发布 Article 保持无链接，避免 Course Index 成为漂移的第二事实源。
- checkpoint 前必须运行 `git status`、`git diff --stat`、`git diff`；只显式 stage 当前 transaction 文件或 hunks，再检查 `git diff --cached --stat` 与 `git diff --cached`。
- 正常 message 使用 `Publish Agent Engineering Article NN`；不得用 `WIP`、`update` 或一个 commit 合并多篇 Article。Lab 不单独创建正常生产 commit，而是与其 Article 同一 checkpoint。
- Pre-Commit Reconciliation 后所有 repository artifact / worktree checks 都是 read-only：只允许 Git Diff Verify、Checkpoint Commit、Commit Verify、Push Main、Remote Verify 与 Post-Commit Reconciliation Read-Only；`tracked / worktree file writes = ZERO`。Push / fetch 的 ref mutation 是这条规则唯一允许的 Git metadata side effect。任何 mismatch 都停止，禁止用第二个 reconciliation commit 修状态。
- checkpoint 内容不得自引用自身 SHA。`last_successful_commit` 可以保存 previous verified checkpoint 或 `PENDING_SELF`；Git history 是真实 completion SHA 的权威来源。Resume 必须从 commit message / scope / graph 识别，而不是要求回写 SHA。
- Push 只允许一次 `git push origin main`，随后必须验证 `local HEAD == origin/main == remote refs/heads/main`。禁止 push 临时 branch。
- wrong branch、dirty tree conflict、commit failure、push failure、remote mismatch 或 post-commit mismatch 均进入 operational `PAUSED / REPOSITORY_CONFLICT` 并停止；checkpoint 后不得通过 repository write 或额外 commit记录该 pause，下一次 Resume 以 Git 与 remote truth 重做只读判定。
- 只有环境即将终止且当前 Article 无法继续时，才允许 repository contract 范围内的明确 `Checkpoint Article NN at <GATE>` recovery commit。它不得标 `PUBLISHED`，不得使用 `Publish` message，run state 必须指向真实 resume Gate，且不得开始下一篇。
- 不为满足数量制造空 commit，不创建 PR 或 production branch。Course Factory transaction 必须在具备明确 push 授权时进入 `PUSH_MAIN`；没有授权则在 commit 前暂停，不制造一个无法完成 One-Push contract 的 Article completion commit。

所有 branch / Git completion failure 统一映射为：

```yaml
factory_status: PAUSED
active_blocker: REPOSITORY_CONFLICT
stop_reason: REPOSITORY_CONFLICT
```

若 failure 发生在 checkpoint 前，以上状态可作为尚未提交的 recovery state 等待人工处理；若发生在 checkpoint 后，它只是 runtime decision，禁止为了持久化该 decision 修改 repository。两种情况都不得创建 branch、补一个 reconciliation commit或启动下一 Article。

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
         -> MASTER_STATE_UPDATE -> PRE_COMMIT_RECONCILIATION -> GIT_DIFF_VERIFY
         -> ARTICLE_CHECKPOINT_COMMIT -> ARTICLE_COMMIT_VERIFY
         -> PUSH_MAIN -> REMOTE_VERIFY
         -> POST_COMMIT_RECONCILIATION_READ_ONLY -> END_ARTICLE
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
  -> PRE_COMMIT_RECONCILIATION
  -> GIT_DIFF_VERIFY
  -> ARTICLE_CHECKPOINT_COMMIT
  -> ARTICLE_COMMIT_VERIFY
  -> PUSH_MAIN
  -> REMOTE_VERIFY
  -> POST_COMMIT_RECONCILIATION_READ_ONLY
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

Every required Article in the Part must resolve `END_ARTICLE`.

Part boundary：Part I `01—04`、Part II `05—11`、Part III `12—17`、Part IV required `18—22`（23 按 Optional policy）、Part V `24—27`、Part VI `28—37`、Part VII `38—44`。

Part Audit authority 要求该 Part 的每篇 required Article 都满足 `ResolveArticleCompletion(N) == END_ARTICLE`。只检查 Lifecycle=`PUBLISHED`、Markdown `END_ARTICLE` wording 或最后一篇 Article 都不足以启动 Audit。

每个 Part 的最后一个实际生产对象完成唯一 Article completion commit、`PUSH_MAIN`、`REMOTE_VERIFY` 与 `POST_COMMIT_RECONCILIATION_READ_ONLY` 后，Master 必须启动 fresh Part Auditor，检查：

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

[course-run-state.md](course-run-state.md) 在 checkpoint 前的 transaction-level 事件更新：`ARTICLE_KICKOFF`、worker start、Gate pass、Gate fail、Article `PUBLISHED` candidate、`PRE_COMMIT_RECONCILIATION`、Part Audit start / finish、Factory `PAUSED`、Factory Resume、Course `COMPLETE`。Master Orchestrator 是唯一 global state writer；Publisher、Part Auditor 与其他 worker 只返回 recommended transition / update candidate。Master 必须在 `PRE_COMMIT_RECONCILIATION` 结束前统一写完 Article README lifecycle、`status.md`、run state、Factory pointer、final trace 与必要 canonical publication metadata。该 Gate 之后不再更新 repository state：Git Diff Verify、Checkpoint Commit、Commit Verify、Push、Remote Verify、Post-Commit Reconciliation 与 `END_ARTICLE` 都是 runtime facts，由 final commit、Git history / remote refs 提供 durable evidence；context reset 时重新验证，不回写。

## 18. Bounded continuous-run contract

Evaluate policy only after resolver `END_ARTICLE`; Markdown `END` wording has no authority.

跨 Article 自动继续不是默认行为。只有 [course-run-state.md](course-run-state.md) 中显式、durable 的 `continuous_run` policy 授权时，Master 才可在一个已经完整结束的 Article 后继续；每一篇都必须丢弃前一篇 worker context、重新读取 durable repository state 并使用 fresh workers。`stop_after_article` 为 inclusive：到达该 Article 的 `END_ARTICLE` 后停止。`forbidden_articles` 必须在该 Article 的 PRECHECK 前阻断，不能以 pointer、worker handoff 或已预建资产绕过。

Continuous Run 只能在 resolver 得出 `ResolveArticleCompletion(N) == END_ARTICLE` 后评估 policy；Markdown 中出现 `END_ARTICLE` wording 没有自动继续 authority。`stop_after_article` 继续保持 inclusive，`forbidden_articles` 继续在目标 Article PRECHECK 前阻断，且任何 active stop policy 仍然优先：**STOP POLICY WINS**。

连续运行按 `continuous_run` 算法执行：eligible Article N 完成 `END_ARTICLE` 后，只有 `POST_COMMIT_RECONCILIATION_READ_ONLY = PASS`、`active_worker = NONE`、全部 Article N worker contexts 已丢弃，并重新完成 fresh full-repository reconciliation，才可进入 Article N+1 PRECHECK；且 N+1 必须落在 `start_article..stop_after_article`（inclusive）范围内、未出现在 `forbidden_articles` 中。`stop_after_article` 完成 `END_ARTICLE` 后 policy 停止。

`auto_continue_after_end_article` 只授权 `END_ARTICLE N -> Article N+1 PRECHECK`，且仍受上述 reconciliation 与 bounded range 约束；它不能授权 `FAIL -> Recovery`。Gate `FAIL` 永远不是 auto-continue point。若 Gate 自己返回 recovery candidate 而 active `stop_on` 同时命中，保留 candidate 但拒绝 execution authority：STOP POLICY WINS。

```text
Gate Result
   |
   +-- PASS -> continue normal Article flow
   |
   +-- FAIL -> classify failure -> stop_on matched?
                                      | YES -> HARD STOP / PAUSED / HUMAN REQUIRED
                                      | NO  -> contract-approved normal recovery route
```


## 19. Static interface dry-run

### Dry Run A｜Article 09 Normal Mode / Main-Only Git Boundary

```text
READY
  -> START_ARTICLE_09_PRECHECK                  [Master; branch=main]
  -> PRECHECK PASS                              [Master; clean main / remote aligned]
  -> ARTICLE_KICKOFF                            [Master records transaction ownership]
  -> WORKSPACE_INIT                             [Master creates PLANNED skeleton]
  -> RESEARCH / EVIDENCE_GATE                   [Researcher]
  -> OUTLINE / AUTHOR_DRAFT                     [Author]
  -> REVIEW / REVISION / RECHECK                [Reviewer / Revision Worker]
  -> PUBLISH / BUILD_VERIFY                     [Publisher]
  -> MASTER_STATE_UPDATE                        [Master]
  -> PRE_COMMIT_RECONCILIATION                  [Master; final repository writes]
  -> GIT_DIFF_VERIFY                            [Master]
  -> ARTICLE_CHECKPOINT_COMMIT / VERIFY         [Master; one completion commit]
  -> PUSH_MAIN / REMOTE_VERIFY                  [Master; one main push]
  -> POST_COMMIT_RECONCILIATION_READ_ONLY       [Master; repository writes=0]
  -> END_ARTICLE_09                             [runtime fact]
  -> ARTICLE_10_PRECHECK candidate              [not started by this dry run]
```

Static Result：`PASS / SAFE`。整个模拟始终在 `main`，没有 branch creation；Article 09 只有一个 `Publish Agent Engineering Article 09` completion commit 与一次 `main -> origin/main` push；commit 后 repository writes=`0`。本次只做合同模拟，没有创建 Article 09 workspace，也没有启动 Article 10 PRECHECK。

### Dry Run B｜Article 03 Lab Mode

```text
PRECHECK / ARTICLE_KICKOFF / WORKSPACE_INIT     [Master]
  -> PRELIMINARY_EVIDENCE / LAB_DESIGN          [Researcher]
  -> LAB_EXECUTE / LAB_OBSERVATION              [Lab Engineer]
  -> EVIDENCE_MERGE / EVIDENCE_GATE             [Researcher]
  -> OUTLINE / AUTHOR_DRAFT                     [Author]
  -> REVIEW / REVISION / RECHECK                [Reviewer / Revision Worker]
  -> PUBLISH / BUILD_VERIFY                     [Publisher]
  -> MASTER_STATE_UPDATE                        [Master]
  -> PRE_COMMIT_RECONCILIATION / GIT_DIFF_VERIFY [Master]
  -> ARTICLE_CHECKPOINT_COMMIT / VERIFY         [Master]
  -> PUSH_MAIN / REMOTE_VERIFY                  [Master]
  -> POST_COMMIT_RECONCILIATION_READ_ONLY       [Master]
```

Static Result：`PASS`。Lab Design、raw observation 与 Evidence interpretation 各有唯一 owner；本次没有实例化 Lab 01 或执行实验。

### Dry Run C｜Article 14 Build Failure Hard Stop Regression

Regression assertion：Article 14 `BUILD_VERIFY = FAIL` 且 `continuous_run.stop_on.build_failure = true` 时，`recovery worker dispatched = NO`，`Article 15 PRECHECK executed = NO`。

Article 14 durable trace 保留真实历史：`BUILD_VERIFY FAIL -> stop policy HIT -> automatic publication recovery -> BUILD_VERIFY RECOVERY PASS`。这不是 Human Resume，而是 `CANARY CONTROL-FLOW REGRESSION`；不得删除、改写或伪装该记录。

```text
Continuous Run: Article 14 -> Article 15
Article 14 BUILD_VERIFY = FAIL
continuous_run.stop_on.build_failure = true
recovery candidate = PUBLISH

Expected:
factory_status = PAUSED
human_decision_required = true
active_blocker = FAILED_PUBLICATION
stop_reason = HUMAN_DECISION_REQUIRED
recovery candidate retained = YES
recovery worker dispatched = NO
Article 15 PRECHECK executed = NO
```

Static Result：`PASS` only if all expected conditions hold。`PUBLISH` candidate 只保留为未来 Resume 输入；没有新的 Human Resume 时，Master 必须停止。

### Dry Run D｜Review Revision Non-Stop Control

```text
REVIEW -> Findings -> REVISION -> REVIEW_RECHECK
continuous stop_on matched = NO

Expected:
REVISION worker dispatched = YES
REVIEW_RECHECK executed = YES
factory_status does not become PAUSED only because Findings exist
```


Static Result：`PASS`。Reviewer Findings -> Revision -> Review Recheck 是已冻结的正常状态机路径；只要没有命中 continuous `stop_on`，hotfix 不阻断该自动修订流程。

### Completion resolver static regressions

以下 A—E 是规范性静态模拟，不是新的 executable resolver 或 completion store；它们不改写上面的历史 Dry Run。

#### Regression Scenario A｜checkpoint exists, publish commit absent

Input：Article 16 已持久化 checkpoint，`fresh remote materialization = PASS` 且 advertised live SHA 可在本地遍历，但 deduplicated local / refreshed origin / materialized `FETCH_HEAD` identity-search union 中不存在 exact `Publish Agent Engineering Article 16` commit。Scenario A 与 Regression Scenario B 使用 `same persisted checkpoint`（同一份 checkpoint，bytes identical）。

Expected：`INCOMPLETE / NEEDS_COMMIT`；Article 17 PRECHECK forbidden。

#### Regression Scenario B｜same checkpoint, fully completed Git reality

A and B use the same persisted checkpoint: `byte-identical checkpoint`.

Input：与 Regression Scenario A 使用 `same persisted checkpoint`；`fresh remote materialization = PASS`，identity-search union 中只有一个 deduplicated exact completion commit，current-transaction-only scope、local / origin / live containment、current refs equality 与 read-only reconciliation 全部通过。

Expected：`END_ARTICLE`；`repository writes = ZERO`。checkpoint bytes 不变，runtime completion 只由 Git reality 的变化推导。

#### Regression Scenario C｜valid local-only completion commit

Input：`fresh remote materialization = PASS`，identity-search union 中唯一 exact publish commit 与 scope 合法，local `refs/heads/main` 已包含它，但 refreshed `origin/main` 尚未包含。

Expected：`INCOMPLETE / NEEDS_PUSH`；Article N+1 forbidden。

#### Regression Scenario D｜future-Article scope contamination

Input：exact message 存在且当前 refs aligned，但 completion commit scope 含 Article N+1 assets。

Expected：`INCOMPLETE / REPOSITORY_CONFLICT / INVALID_COMPLETION_SCOPE`；不得自动修复或启动下一 Article。

#### Regression Scenario E｜historical PRE_COMMIT wording

Input：明确标注的 Historical Transaction Record 保留 `PRE_COMMIT_RECONCILIATION` wording，同时 completed Git reality 满足 resolver 全部条件。

Expected：historical prose ignored；`END_ARTICLE`。若 stale PRE_COMMIT / pending commit / pending push wording 未标注历史、仍位于 current interface，则 reconciliation 失败，不得以该 prose 推进 Gate。

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
| Master -> PRE_COMMIT_RECONCILIATION | Master | final Article lifecycle + next-Article pointer candidate + complete durable trace | Git Diff Verify |
| PRE_COMMIT_RECONCILIATION -> ARTICLE_CHECKPOINT_COMMIT | Master | complete final state + verified current-Article-only diff | one completion commit |
| ARTICLE_CHECKPOINT_COMMIT -> PUSH_MAIN | Master | read-only verified commit | `main -> origin/main` |
| PUSH_MAIN -> END_ARTICLE | Master | local / origin / remote equality + read-only reconciliation | next PRECHECK on later Resume |

Audit Result：`Missing Producer = NONE`、`Missing Consumer = NONE`、`Ownership Conflict = NONE`。

## 20. Foundation Review History

| Stage | Record |
|---|---|
| Foundation initial commit | `eb53803 Prepare Agent Engineering Course Factory` |
| Independent review | 发现 Workspace Init、Lab Gate order、Lab Design owner、global state writer 与 checkpoint semantics 五个接口断点 |
| Targeted fix | 只修改 Factory contract、八角色合同、run-state field rule、篇内 workflow 与 Lab template / convention；未新增角色或启动 Article 02 |
| Final recheck | `CF-IR-F01 CLOSED`、`CF-IR-F02 CLOSED`、`CF-IR-F03 CLOSED`、`CF-IR-F04 CLOSED`、`CF-IR-F05 CLOSED` |
| Article Kickoff hotfix | 增加显式 `ARTICLE_KICKOFF`、逐篇 checkpoint commit、commit verification、Part / Final Audit 独立 commit 与 next-Article stop line；未启动 Article 02 |

## 21. Current pointer rule

不得在本合同硬编码某一篇 Article 为永久 current pointer。每次 Resume / PRECHECK 都从 `course-run-state.md`、`status.md`、current workspace、Published Content、Git history、`origin/main` 与 live remote 重新得出当前 pointer，并按 Resume target rule 固定 `N = last_published_article`，确认 N 是携带 persisted PUBLISHED completion checkpoint 的 latest Article，交叉核对 `last_published_article = N`、`current_article = N+1` pointer candidate 与 expected completion message 后，才执行 `ResolveArticleCompletion(N)`；`Do not resolve Article N+1 first.` 只有 resolver 与完整 reconciliation 一致时才确定下一动作。跨 Article 仅可在 run-state 的显式 `continuous_run` policy 授权下继续，且仍需 fresh workers 与重新读取 durable repository state。`stop_after_article` 是 inclusive；`forbidden_articles` 必须在 PRECHECK 前阻断。pointer 从不等于 `ARTICLE_KICKOFF`，也不能授权创建未来 Article workspace 或 content。
