# Agent Engineering Course Factory Subagent Contracts

> Contract Status：`FOUNDATION_READY`
>
> 本文件冻结未来 Course Factory 可使用的八种 worker role。角色是课程内部执行职责，不宣称是行业统一的 Agent 分类。Master 每次只启动当前 Gate 必需的 worker；所有 worker 都服从 [course-factory.md](course-factory.md)、[production-workflow.md](production-workflow.md) 与 repository instructions。

## Shared worker rules

- Repository artifacts 是输入和交付面；隐藏 reasoning、聊天记忆和口头完成声明不是 durable evidence。
- 每个 worker 只能写 `Allowed Writes`，遇到超界需求必须停止并按 `Handoff Contract` 返回。
- worker 必须保存真实失败；不得为了完成 Gate 伪造 Evidence、Lab、Review 或 Build 结果。
- 角色之间通过已落盘 artifact、finding、gate result 和 run-state pointer 交接，不传递“相信我已经检查过”的隐式状态。
- Master Orchestrator 是 global durable execution state 的唯一 writer；其他 worker 只能返回 update candidate 或 recommended transition。
- Master Orchestrator、Researcher、Author、Reviewer、Revision Worker、Lab Engineer、Publisher 与 Part Auditor 的每次 execution 都必须返回且只返回一个符合下述 `Worker Result Contract` 的 envelope；`done`、裸 `PASS` 或自然语言文件说明均不是有效 handoff。
- **Branch authority = NONE for all eight roles.** Master Orchestrator、Researcher、Author、Reviewer、Revision Worker、Lab Engineer、Publisher 与 Part Auditor 均禁止执行 `git checkout -b`、`git switch -c`、`git branch <new-branch>`，禁止创建或 push `codex/*`、`article-*`、`feature/*`、`factory/*`、`temp/*` 或任何其他 Course Factory production branch。所有正式 Factory transaction 只在 `main` 上进行；wrong branch 必须交给 Master 按 clean-worktree safe recovery 规则处理，worker 不得自行绕过。

## Worker Result Contract

所有八种 worker role 使用同一个结构化协议。role-specific artifact、Finding、Publication Result、Revision Disposition、Lab Observation 或 Audit Report 仍按各自合同落盘，但不能替代 `worker_result` envelope。

```yaml
worker_result:
  role: AUTHOR
  article: "08"
  gate: OUTLINE
  execution_type: REAL_SUBAGENT
  status: PASS
  artifacts_created:
    - docs/agent-engineering-course/articles/08-agent-loop/outline.md
  artifacts_modified: []
  gate_completed: true
  next_allowed_gate: AUTHOR_DRAFT
  blocker: NONE
  notes:
    - "8 / 8 claim coverage; no new core fact required"
```

### Field contract

| Field | Contract |
|---|---|
| `role` | 必填；只使用 `MASTER_ORCHESTRATOR / RESEARCHER / AUTHOR / REVIEWER / REVISION_WORKER / LAB_ENGINEER / PUBLISHER / PART_AUDITOR`。 |
| `article` | 必填字符串；Article transaction 使用两位 Article ID，例如 `"08"`；Part / Course Audit 使用 canonical scope ID，例如 `"PART_II"` 或 `"COURSE_FINAL"`。 |
| `gate` | 必填；必须是本次 task brief 指定的当前 Factory Gate，不得用 `DONE`、`COMPLETE` 等模糊别名。 |
| `execution_type` | 必填；只使用 `REAL_SUBAGENT / MASTER_DETERMINISTIC`，并与 durable execution trace 一致。 |
| `status` | 必填；只使用 `PASS / FAIL / BLOCKED`，表示本次 worker execution 是否按角色合同返回了可消费结果，不等同于 Article Lifecycle。`PASS_WITH_NOTES` 等 role-specific decision 在 artifact 中保留，envelope 归一为 `PASS` 并在 `notes` 指向细节。 |
| `artifacts_created` | 必填列表；只列本次新建的 repository-relative path；没有则使用 `[]`。 |
| `artifacts_modified` | 必填列表；只列本次修改的 repository-relative path；没有则使用 `[]`。 |
| `gate_completed` | 必填布尔值；表示 assigned Gate execution 的 required outputs 是否完整产生，不等于 Gate decision `PASS`，也不是 Master 批准。Reviewer 完整产出 Findings 后可以为 `true`，即使下一 route 是 `REVISION`。 |
| `next_allowed_gate` | 必填；worker recommendation，只能是 Factory state machine 中的 exact Gate 或 `NONE`，不能自行推进 durable state。 |
| `blocker` | 必填；没有 blocker / return route 时使用 `NONE`；否则使用下表冻结的 stop reason 或 role-specific return code。`status: PASS` 且 `gate_completed: false` 的 fail-closed return 可以携带 `RETURN_TO_RESEARCH / RETURN_TO_REVIEW`。 |
| `notes` | 必填字符串列表；只放摘要、限制与 artifact 内细节指针。Master 不得依赖 `notes` 代替 required fields 或 repository evidence。 |

所有字段都必须存在；空列表必须显式写为 `[]`，不得省略。当前 schema 是 closed schema：`worker_result` 根节点之外的包装、未知字段、错误类型或字段别名均使 envelope 无效。当前 schema 不支持删除或重命名：worker 不得删除 artifact；需要 rename 时必须返回 `BLOCKED / HUMAN_DECISION_REQUIRED`，不得把 rename 伪装为 create + modify。

### Common result mapping

| Result / route | `status` | `gate_completed` | `next_allowed_gate` | `blocker` |
|---|---|:---:|---|---|
| assigned Gate execution 完成并允许正常前进 | `PASS` | `true` | exact forward Gate | `NONE` |
| Reviewer Findings 需要修订 | `PASS` | `true` | `REVISION` | `NONE`；Findings 在 `review.md` |
| Revision Disposition 完成 | `PASS` | `true` | `REVIEW_RECHECK` | `NONE` |
| `RETURN_TO_RESEARCH` | `PASS` | `false` | `RESEARCH` | `RETURN_TO_RESEARCH` |
| `RETURN_TO_REVIEW` | `PASS` | `false` | `REVIEW` | `RETURN_TO_REVIEW` |
| `BLOCKED_EVIDENCE` | `BLOCKED` | `false` | `RESEARCH` | `BLOCKED_EVIDENCE` |
| `FAILED_LAB` | `FAIL` | `false` | `LAB_EXECUTE` | `FAILED_LAB` |
| `FAILED_REVIEW` | `FAIL` | `true` | `NONE` | `FAILED_REVIEW` |
| `FAILED_PUBLICATION` | `FAIL` | `true` | `PUBLISH` | `FAILED_PUBLICATION` |
| Part Audit `PASS` | `PASS` | `true` | exact next Article / Part Gate | `NONE`；`QUALITY_DEGRADATION_REVIEW` 只保留在 Audit Finding |
| Part Audit `FAIL` | `FAIL` | `true` | `NONE` | `PART_AUDIT_FINDINGS`；affected Article 与 repair scope 读取 Audit Report；durable stop 固定映射为 `factory_status: PAUSED / active_blocker: PART_AUDIT_FINDINGS / stop_reason: HUMAN_DECISION_REQUIRED`，由人类批准 targeted repair scope 后 Resume |
| repository / human stop | `BLOCKED` | `false` | `NONE` | `REPOSITORY_CONFLICT` 或 `HUMAN_DECISION_REQUIRED` |

`next_allowed_gate: NONE` 只允许用于 `FAIL / BLOCKED` 的无安全自动 route，或已到 `END_ARTICLE / COMPLETE` 的 terminal Master result。非 terminal 的 `status: PASS` result 必须给出 exact non-`NONE` Gate。`FAIL / BLOCKED` result 中的 non-`NONE` Gate 只是 recovery candidate：Master 保持 `current_gate` 不变且不得自动 dispatch，直到 Resume / recovery validation 明确批准该 rollback、retry 或 return route。这样 Researcher 在 `EVIDENCE_GATE` 返回 `FAILED_LAB -> LAB_EXECUTE` 时不会直接改写 current Gate。worker runtime 若中断且没有产生完整 envelope，Master 必须把它视为 `MISSING_OR_INVALID_WORKER_RESULT` validation failure；不得替 worker 补写 `PASS`。

### Worker Result lifecycle

`worker_result` 只是一个 Gate execution event。它不是 Article completion、Transaction completion 或 Factory completion，也不直接修改 global durable state。

```text
Worker Result
  -> Master Artifact Verification
  -> Master Gate Validation
  -> Master State Transition
  -> Next Worker Dispatch
```

### Master validation contract

Master 收到每一个 `worker_result` 后 MUST：

1. 验证根节点、11 个 exact fields、字段类型与 closed-schema 约束，并确认 `role / article / gate` 与 task brief 完全一致、`execution_type` 与真实 executor / durable trace 一致；
2. 验证 `artifacts_created` 中的每个 path 真实存在、与 actual diff 一致、属于该 role 的 `Allowed Writes`，并核对实际新增文件没有漏报；
3. 验证 `artifacts_modified` 与实际 diff 一致，且全部属于该 role 的 `Allowed Writes`；同时确认没有未声明的 delete / rename；
4. 验证 `gate_completed`、`status` 与 required outputs 满足当前 Gate Contract；不得因 worker 自报 `PASS` 或文件存在就批准 Gate；
5. 验证 `next_allowed_gate` 与 common result mapping 一致，且是当前 State Machine 的合法 transition；worker recommendation 不具有推进权；
6. `PRE_COMMIT_RECONCILIATION` 结束前，把验证后的投影写入 `course-run-state.md:last_worker_result`，并由 Master 统一更新其他 transaction-level durable state；该 Gate 后按 read-only exception 只验证 runtime envelope，不写 repository；
7. validation `PASS` 且 `status: PASS` 时，按已验证的 `next_allowed_gate` 继续并派发下一 required worker；`gate_completed: false` 只允许走上表冻结的 retry / return route。`status: FAIL / BLOCKED` 时 `next_allowed_gate` 只作为 durable recovery candidate，Master 保持当前 Gate且不自动 dispatch；无安全 route 或 validation failure 同样进入精确的 `PAUSED`、`BLOCKED` 或 `PAUSED / HUMAN_DECISION_REQUIRED` route。

Master 是 raw `worker_result` 的唯一 validator，也是 state transition 的唯一 owner。任何 worker 都不得写 `last_worker_result`、把 `next_allowed_gate` 当成已批准状态，或用自然语言 handoff 绕过本协议。checkpoint 前，Master 必须把完整 raw envelope 记录到 canonical durable location：Article transaction 写入当前 Article `subagent-trace.md` 的 `Worker Result Records` section；Part / Course Audit 写入对应 durable Audit Report。每条 record 必须有 stable record ID，并同时保存 execution / task ID、bounded task brief snapshot、raw envelope、Master validation result 与验证时间。没有收到 envelope 时 record 明确写 `raw_envelope: MISSING`；收到不可解析 payload 时写 `raw_envelope: INVALID` 并原样保存 invalid payload，二者都不能被解释或补全为 envelope。`course-run-state.md:last_worker_result.result_ref` 必须指向 schema-valid pre-commit record；invalid / missing record 只由 `last_worker_result_error.result_ref` 引用。projection 不替代 raw record。

`PRE_COMMIT_RECONCILIATION` 是 repository-result persistence cut：后续 `GIT_DIFF_VERIFY`、`ARTICLE_CHECKPOINT_COMMIT`、`ARTICLE_COMMIT_VERIFY`、`PUSH_MAIN`、`REMOTE_VERIFY` 与 `POST_COMMIT_RECONCILIATION_READ_ONLY` 仍必须产生相同 closed-schema runtime envelope 并由 Master 验证，但不得把 envelope、validation 或 `END_ARTICLE` 写回 repository。checkpoint 内的 final trace 只记录截至 Pre-Commit Reconciliation 的已验证结果与后续 Gate intent，不伪造尚未发生的 diff / commit / push result。此阶段的 durable evidence 是 final commit diff、Git commit graph、commit content、`origin/main` 与 remote `refs/heads/main`；context reset 后必须重新执行只读检查。该 exception 只改变 result persistence，不改变 `Worker Result -> Master Validation -> State Transition -> Next Gate` 控制流。

Master 自己执行 `MASTER_DETERMINISTIC` Gate 时也必须先序列化相同 envelope，再以 repository truth、实际 diff、Gate Contract 与 State Machine 做 deterministic validation；只到 `PRE_COMMIT_RECONCILIATION` 为止可以写 durable state，之后必须遵守 read-only exception。Master 自报字段不是验证证据。context reset 后的 fresh Master 必须按 Resume Contract 重新核对 pre-commit projection，并重放 Git / post-commit read-only verification。

## 1. Master Orchestrator

### Purpose

确定性编排单一 Course Factory transaction：在 `main` 上读取 durable state、选择 next safe action、在 PRECHECK 后执行显式 `ARTICLE_KICKOFF`、启动正确 worker、验证交付物存在、推进 Gate、在 commit 前完成全部 state reconciliation、创建唯一 Article completion commit、push / remote verify，并以只读 reconciliation 结束 transaction。

### Inputs

- repository instructions、canonical、Course Factory contract；
- `course-run-state.md`、`status.md`、Git state；
- 当前 Article / Audit 的 worker result 与 Gate result。

### Required Reads

- `AGENTS.md`、`CLAUDE.md` 与适用目录 instructions；
- `docs/agent-engineering-series-plan.md`；
- `course-factory.md`、`production-workflow.md`、`status.md`、`course-run-state.md`；
- 当前 transaction 的 workspace 与 latest relevant commit。

### Allowed Writes

- `course-run-state.md` 的 transaction-level pointer；
- `status.md` 中已由 Gate 证明的状态；
- PRECHECK `PASS` 后的 `ARTICLE_KICKOFF` durable transaction ownership；
- `PLANNED` Article workspace 的 deterministic skeleton（仅在 PRECHECK 与 `ARTICLE_KICKOFF` 均 `PASS` 后）；
- Article README lifecycle、Factory-level checkpoint pointer、Part Audit global status 与 Course `COMPLETE` state；
- 必要的 canonical publication metadata（只应用 Publisher 返回且已验证的 candidate）；
- 当前 transaction 的 checkpoint metadata；
- worker task brief 与 audit handoff artifact（若仓库已有对应位置）。

上述写权限只存在于 `PRE_COMMIT_RECONCILIATION` 完成前。`ARTICLE_CHECKPOINT_COMMIT` 之后 Master 对 repository files 的 Allowed Writes 为 `NONE`。

### Forbidden Actions

- 不负责 Research、Draft writing、technical self-review、Evidence interpretation 或 Lab implementation；
- WORKSPACE_INIT 不得写 Research Answer、Evidence Conclusion、Claim Confirmation、Teaching Thesis、Outline、Draft 或 Review Finding；
- 不代替 Reviewer 判技术正确，不代替 Researcher升级 Claim；
- 不并行启动多篇 Article，不自动降低 Gate，不处理无关 dirty changes；
- 不在当前 Article 的独立 checkpoint commit、`PUSH_MAIN`、`REMOTE_VERIFY` 与 `POST_COMMIT_RECONCILIATION_READ_ONLY` 全部通过前启动下一篇 PRECHECK 或写入下一篇 workspace；Pre-Commit Reconciliation 可以写 next-Article pointer candidate，但 pointer 不等于 Kickoff；
- 不使用 `git add .` broad-stage，不把下一篇、无关用户修改、theme / CI / unrelated docs 混入 Article checkpoint；
- 不创建任何 branch，不执行 `git checkout -b`、`git switch -c` 或 `git branch <new-branch>`；wrong branch 只能在 clean worktree 下安全切换到既有 `main`，否则停止；
- 不在 checkpoint 后修改任何 repository file或创建 reconciliation commit；
- 不在缺少明确授权时 push、发布外部内容或改变 canonical；获得授权后只允许 `git push origin main`。

### Required Outputs

- reconciled current state；
- PRECHECK result 与显式 `ARTICLE_KICKOFF` result；
- PRECHECK 通过后的 WORKSPACE_INIT result，包含 workspace path、机械实例化字段和未决判断；
- selected worker 与 bounded task brief；
- validated `worker_result`、artifact / Allowed Writes check 与 `last_worker_result` durable projection；
- Gate transition 或精确 stop report；
- current-transaction-only diff verification、唯一 checkpoint commit evidence、`ARTICLE_COMMIT_VERIFIED`、`PUSH_MAIN`、`REMOTE_VERIFY`、read-only reconciliation result 与 resume pointer。

### Gate Responsibility

负责 PRECHECK、`ARTICLE_KICKOFF`、WORKSPACE_INIT、global state transition、Pre-Commit Reconciliation、Git Diff Verify、Article Checkpoint Commit、Commit Verify、Push Main、Remote Verify 与 Post-Commit Reconciliation Read-Only：确认上一个 Gate 的 required outputs 与 decision 已存在，再推进下一 Gate。Master 不重新判 Evidence、Review 或 Lab 内容。只有 Reviewer Final PASS、Publisher PASS、Build PASS、Pre-Commit Reconciliation PASS、Lifecycle `PUBLISHED`、`ARTICLE_COMMIT_VERIFIED = PASS`、main push / remote equality 与 read-only reconciliation 同时成立，Master 才能逻辑结束当前 Article transaction。

### Stop Conditions

- durable state 冲突、completion commit 缺失或 unrelated dirty tree 无法安全隔离；
- current branch 不是 `main` 且无法在 clean worktree 下安全恢复，或 local / origin / remote main divergence；
- worker 返回 Gate failure、越权输出或缺少 required artifact；
- 需要人类改变 canonical、Optional、课程架构或权限。
- canonical / template 无法提供 WORKSPACE_INIT 的必须字段，且填写需要实质性课程判断。

### Handoff Contract

给 worker：当前 Article、Gate、Required Reads、Allowed Writes、expected outputs、stop line 与 `Worker Result Contract`。收回：一个完整 `worker_result` envelope，以及 role-specific durable artifacts；`done`、裸 `PASS`、文件清单或自然语言 next action 均不能替代 envelope。Publisher 的 Publication Result 与 Part Auditor 的 global update candidate 继续作为 role-specific artifact / result 落盘。Master 只有在 envelope、repository artifact、实际 diff、Allowed Writes、Gate Contract 与 State Machine 全部一致时，才写 `last_worker_result` 并统一更新 `status.md`、run state 与 checkpoint metadata；这些 repository writes 必须在 Pre-Commit Reconciliation 冻结。每篇 Article 必须直接在 `main` 显式 stage 本 transaction 文件，以 `Publish Agent Engineering Article NN` 创建唯一 completion commit，read-only verify 后只 push `origin main` 并核验 local / origin / remote equality；checkpoint 后不得写 trace、state 或 README，也不得 handoff 到下一 Article。

### Context Isolation Rules

不把前一 worker 的隐藏推理、confidence 或 self-score传给下一 worker。Reviewer task brief 只包含可读 repository artifacts、review scope 与 contract。

### Model / Reasoning Guidance

优先低变异、规则遵循和状态核对；涉及冲突 reconciliation 时提高推理深度，但不扩展任务范围。Master 的产出应短、结构化、可重放。

## 2. Researcher

### Purpose

在已完成 WORKSPACE_INIT 的 Article workspace 中，把 Article Card 与 Research Questions 转成可审计 Claim Register、Evidence Cards、counter-evidence、version scope 和明确的不确定边界；Lab Article 中同时拥有 Preliminary Evidence、Lab Design 与 Evidence Merge。

### Inputs

- canonical Article entry、Article Card、Glossary；
- Research Questions、relevant dependencies；
- mode-specific source / Lab requirements。
- 已存在的 workspace skeleton 与 WORKSPACE_INIT result。

### Required Reads

- repository instructions、canonical 与 `production-workflow.md`；
- 当前 Article `README.md`、`article-card.md`、`research.md`、`evidence.md`；
- relevant earlier published articles；
- DSH / Lab / BuildPilot contract（适用时）。

### Allowed Writes

- 当前 Article 的 `research.md`、`evidence.md`；
- 当前 required Lab 的 Lab Card / Lab Design durable artifact；
- Evidence 所需的只读取证记录、source manifest 或 raw Lab observation 的 interpretation；
- Evidence-related `Article Card Update Candidate`，不得直接重写课程 Positioning；
- Research / Evidence 状态候选，不直接改全课程状态。

### Forbidden Actions

- 不写 Outline 或 Draft，不 Publish；
- 不为教学叙事预设结论，不忽略 counter-evidence；
- 不把版本敏感记忆当 current fact；
- 不把 `BLOCKED / PARTIAL / PROPOSAL` 自行包装成 `CONFIRMED`。
- 不执行 Lab，不修改 raw observation、hypothesis 或 acceptance criteria 来适配结果。

### Required Outputs

- answered / blocked Research Questions；
- Claim Register；
- 每个核心 Claim 的 Evidence Card；
- source、retrieved date、version scope、observation、counter-evidence、proves / does-not-prove、limitations；
- Evidence Gate recommendation。
- Lab Article 的 Preliminary Evidence、`Lab Dependency: REQUIRED` annotation、frozen Lab Design；Lab Design 至少包含 Lab ID、Related Article / Claim IDs、Research Question、Hypothesis、What Would Falsify It、Fixture Boundary、Environment、Inputs、Variables、Expected Observable、Fault Injection、Commands / Execution Needs、Acceptance Criteria、Evidence Mapping、Limitations、Safety / Permission Constraints；
- Lab 后的 Evidence Merge，包括 Claim Status、Proves、Does Not Prove、Limitations 与 Course Usage 更新。

### Gate Responsibility

Normal Article 在 Research 后对 `EVIDENCE_GATE` 提供事实材料与建议。Lab Article 先产出 Preliminary Evidence 与 Lab Design，待 Lab Engineer 返回 raw observation 后执行 `EVIDENCE_MERGE`；只有 Merge 完成且核心行为性 Claim 得到足够证据、真实 Lab 支撑或已收窄，才建议 Evidence Gate `PASS`。

### Stop Conditions

- 核心 Evidence 无法获得或来源互相冲突；
- required Lab / runtime evidence 尚未执行时，返回冻结 Lab Design 并 handoff 给 Lab Engineer；若 Lab 无法真实执行，返回 `FAILED_LAB`；
- 需要改变 canonical 才能继续；
- source access、版本或权限不足以支持安全结论。

### Handoff Contract

Normal Article 向 Master / Author 交付 final Claim / Evidence 与 Gate recommendation。Lab Article 先向 Lab Engineer 交付 durable Lab Design，再接收 raw observation 执行 Evidence Merge，最后向 Author 交付 final Evidence、Lab Observation、Lab Limitations 与 Evidence Gate result。Evidence 不足时返回 `BLOCKED_EVIDENCE` 或 `FAILED_LAB`，不生成叙事草稿。

### Context Isolation Rules

不读取未来 Author 的隐藏 thesis 或措辞偏好。只根据问题、来源与观测形成 Evidence；不能被“文章想证明什么”反向约束。

### Model / Reasoning Guidance

优先 primary / official docs、spec、source、paper 与可复现实验；版本敏感事实必须实时复核。复杂源码与跨 Provider 主张需要较高推理深度和 counter-check。

## 3. Author

### Purpose

在 Evidence Gate `PASS` 后，把已证明的 Claim 转化为 Detailed Outline、Teaching Spine、Figures、Examples、Draft、Learning Check 与 Job Competency mapping。

### Inputs

- approved Article Card；
- Research / Evidence artifacts 与 Gate decision；
- Glossary、dependency articles、course writing method。

### Required Reads

- repository article-writing instructions；
- canonical、Article Card、Research、Evidence、Glossary；
- relevant dependency articles；
- 当前 Article 的 mode；Lab Article 还必须读取 final Evidence、raw Lab Observation 与 Lab Limitations。

### Allowed Writes

- 当前 Article 的 `outline.md`、`draft.md`；
- 正文所需的非证据性图示草案与 Learning Check；
- Outline / Draft gate checklist。

### Forbidden Actions

- Evidence Gate 未 `PASS` 不得启动；
- 不把 `BLOCKED` Claim 改为 `CONFIRMED`；
- 不为叙事发明新核心事实、扩写后续 Article 或改变 canonical；
- 不重新解释原始 Lab 结果来获得比 final Evidence 更强的 Claim；
- 不写 Published Content，不做自己的 Formal Review。

### Required Outputs

- claim-to-section coverage；
- problem space -> abstract model -> concrete mechanism -> engineering judgment -> verification boundary 的 Teaching Spine；
- Detailed Outline、Figures / Examples 职责、Draft；
- Learning Check、Job Competency coverage 与显式 non-scope。

### Gate Responsibility

负责 Outline Gate 与 Draft Gate 的候选产物；不能批准自己进入 `FINAL`。

### Stop Conditions

- 写作需要超出 Evidence 的核心 Claim；
- dependency / glossary / Lab result 发生矛盾；
- Article scope 无法在 canonical 边界内完成。

### Handoff Contract

Evidence 足够时向 Reviewer 交付 Outline、Draft、Claim coverage 和已知限制。Evidence 不足时返回 `RETURN_TO_RESEARCH`，列出所需 Claim / Evidence，不自行补结论。

### Context Isolation Rules

可以读取 Researcher 的 repository outputs，但不接收其未落盘隐藏推理。不得向 Reviewer传递自己的 confidence、self-score 或辩护性上下文。

### Model / Reasoning Guidance

以教学结构、概念桥接和工程判断为主；复杂 Article 需要足够推理深度，但应压缩重复定义，避免用篇幅掩盖 Evidence 缺口。

## 4. Reviewer

### Purpose

在 fresh context 中独立审查技术、Evidence、课程一致性、Reader Value、Job Competency、Lab 与 Publication 风险，并做 Gate decision。

### Inputs

- review scope 与 Finding schema；
- canonical、Glossary、Article Card、Research、Evidence、Outline、Draft；
- relevant dependency article 与 Lab result（适用时）。
- Lab Article 的 frozen Lab Design、raw Observation、failure output 与 final Evidence Merge（适用时）。

### Required Reads

- repository review instructions、`production-workflow.md`、review checklist；
- canonical 与当前 Article 全部可审查 artifact；
- claim-relevant primary source 或 source/runtime evidence；
- dependency / glossary 中与本篇直接相关的条目。
- Lab Article 的 expected / observed 分离、Evidence traceability、failure case 与 Claim wording boundary。

### Allowed Writes

- 当前 Article 的 `review.md`；
- Findings、score、Gate decision、recheck result；
- Part Auditor 不在本角色内。

### Forbidden Actions

- 禁止读取 Author hidden reasoning、Author confidence、Author self-score；
- 第一轮只输出 Findings，不直接改正文；
- 不替 Revision Worker repair，不为通过门禁降低 Severity；
- 不把“不喜欢文风”伪装成技术 Finding。

### Required Outputs

每个 Finding 必须包含：

```text
Finding ID
Severity: BLOCKER | MAJOR | MINOR | EDITORIAL
Category: TECHNICAL | EVIDENCE | COURSE | READER_VALUE |
          JOB_COMPETENCY | LAB | PUBLICATION
Location
Problem
Supporting Evidence
Why It Matters
Required Disposition
```

另需输出五维 score、未关闭 Finding 汇总与 `PASS / FAIL / PASS_WITH_NOTES` Gate decision。

Lab Article Review 还必须明确检查：Expected Observable 与 Observed Result 是否分离、Lab Evidence 能否追到 raw output、Claim wording 是否超过 Observation、失败路径是否完整保留。

### Gate Responsibility

负责 Review Gate、Review Recheck 与 Final Gate decision。Reviewer `PASS` 是进入 `FINAL` 的必要条件，不是 Build / Publication Gate 的替代。

### Stop Conditions

- required artifact 缺失或 Evidence 无法追踪；
- 需要新 Research / Lab 才能判断；
- context 被 Author 的隐藏评分或说服性材料污染；
- 三轮复核后仍有 `BLOCKER / MAJOR`。

### Handoff Contract

第一轮向 Master / Revision Worker 交付 Findings，不改正文。recheck 时逐项返回 `OPEN / CLOSED / ESCALATED` 与依据；只有 Reviewer 可以关闭 Finding。

### Context Isolation Rules

每次首轮 Review 使用 fresh context。recheck 只读取原 Finding、Revision Disposition、变更后 artifact 与必要 evidence；不读取 Revision Worker 的隐藏推理。

### Model / Reasoning Guidance

使用独立、怀疑式、证据优先的高质量审查；复杂技术 / 源码 / Lab 篇应提高推理深度。评分必须由具体 Finding 和 artifact 支撑，不能机械集中在固定区间。

## 5. Revision Worker

### Purpose

只在 Reviewer Findings 的边界内执行最小必要修订，并产出可复核的 Revision Disposition。

### Inputs

- current artifacts；
- Reviewer Findings 与 Required Disposition；
- supporting Evidence 与 repository rules。

### Required Reads

- 原 Finding、受影响位置、Research / Evidence；
- 当前 Outline / Draft / Published Content（按 Gate）；
- 与修订直接相关的 canonical / glossary / dependency。

### Allowed Writes

- Finding 明确涉及的当前 Article artifacts；
- `review.md` 中 Revision Disposition 候选记录，但不得写 `CLOSED` decision。

### Forbidden Actions

- 不顺手重写整篇、扩展新章节、增加无 Evidence Claim 或改变 canonical；
- 不处理 Finding 之外的偏好性修改；
- 不自行把 Finding 标为 `CLOSED`；
- 不代替 Publisher 改发布 metadata，除非 Finding 明确属于该范围。

### Required Outputs

```text
Finding ID
Files Changed
What Changed
Evidence Impact
Proposed Status
```

`Proposed Status` 只能是 `READY_FOR_RECHECK` 或 `BLOCKED`。

### Gate Responsibility

不做 Gate decision；只把 revision transaction 交回同一 Review scope 的 fresh Reviewer recheck。

### Stop Conditions

- 修复需要新核心 Evidence、Lab 或 canonical change；
- Finding 互相冲突或 Required Disposition 不可安全实现；
- 目标文件含无关用户修改且无法安全分离。

### Handoff Contract

向 Reviewer 交付逐 Finding 的变更文件、变更内容、Evidence 影响与未解问题；不得使用“已修好”替代可检查 diff。

### Context Isolation Rules

只读取 Findings 和必要 artifact，不需要 Author 的创作过程。不得向 Reviewer注入自己的完成信心或关闭结论。

### Model / Reasoning Guidance

偏好局部、低风险、可审计修改；需要足够推理理解 Finding 的根因，但不得扩大解法范围。

## 6. Lab Engineer

### Purpose

只在 required Lab Article 中执行 Researcher 已冻结的 Lab Design：实现 fixture，执行 build / test / run / fault injection，并保存真实 runtime observation 与失败证据。

### Inputs

- Researcher 冻结并落盘的 Lab Design、Claim / Evidence needs；
- repository environment、allowed commands 与 fixture boundary；
- current Article / Lab template。

### Required Reads

- repository instructions、canonical Lab route；
- `labs/README.md`、Lab template、Article Research / Evidence；
- environment / build / test instructions；
- 安全与权限边界。

### Allowed Writes

- 当前 Lab 的 fixture、implementation、tests、raw logs / observations 与 execution artifact。

### Forbidden Actions

- 不把 expected result 写成 observed result；
- 不隐藏 failing case、跳过 fault injection 或篡改环境来迎合 thesis；
- 不修改 hypothesis、acceptance criteria 或扩大实验问题来让 Lab 通过；
- 不写 Article Draft、不发布、不扩大到生产系统实现；
- 不执行未获授权的外部部署、上传或危险变更。
- 不修改 Article Evidence 或决定 Claim Status；Evidence interpretation 由 Researcher 在 `EVIDENCE_MERGE` 完成。

### Required Outputs

- environment / version；
- exact commands 与 exit codes；
- fixture 与 implementation boundary；
- build / test / run result；
- runtime output 与 fault injection result；
- observed behavior 与 unexpected behavior；
- failure output；
- reproduction notes 与 limitations。

### Gate Responsibility

提供 `LAB_EXECUTE / LAB_OBSERVATION` 的真实执行证据，不决定 Claim Status 或 Evidence Gate。只有 required build / run / fault injection 真实完成才返回 execution complete；无法完成时返回 `FAILED_LAB`。Claim 是否收窄由 Researcher 在 Evidence Merge 决定。

### Stop Conditions

- 环境、依赖、权限或 fixture 无法安全建立；
- build / run / test 失败且当前范围内无法修复；
- 实际结果反驳核心 thesis 或无法区分 source/runtime。

### Handoff Contract

只向 Researcher 交付原始 observation、命令、exit code、日志位置、失败与运行边界；明确哪些是 expected、哪些是 observed。失败也是正式输出。Researcher完成 Evidence Merge 后，Author 才读取 final Evidence 与 Lab artifacts。

### Context Isolation Rules

不读取 Author 希望得到的结论；只读取 Lab question 与 acceptance criteria。先记录原始结果，再解释结果。

### Model / Reasoning Guidance

强调可复现性、故障定位和环境精确性；复杂 Lab 使用高推理深度，但任何推理都不能替代实际 command output。

## 7. Publisher

### Purpose

只在 Article `FINAL` 后把冻结正文安全映射到 Hugo content，校验 metadata、路径、链接、render / build，并向 Master 返回结构化 Publication Result；Publisher 不写 global durable state，也不自行宣布 `PUBLISHED`。

### Inputs

- FINAL Draft、final review decision；
- repository Hugo / front matter / series rules；
- target content path 与 publication metadata requirements。

### Required Reads

- `AGENTS.md` / `CLAUDE.md` 的 Hugo、front matter、shortcode 与 Git 规则；
- FINAL Draft、review.md、Article README；
- related published pages、series metadata、`status.md`；
- 真实 build / CI command。

### Allowed Writes

- 当前 Article 的 published content 与发布图片；
- front matter、series metadata、internal links；
- Article README 中的 publication evidence；
- publication-specific artifact。

### Forbidden Actions

- 不修改 frozen knowledge content；机械映射之外的语义变化必须返回 Review；
- 不把 current source 已推翻的 Claim 就地修掉；必须 `RETURN_TO_REVIEW`；
- 不修改无关 theme / CI / content，不 broad-stage，不在未授权时 push。
- 不直接写 `status.md`、`course-run-state.md`、Factory checkpoint pointer 或 Part / Course global status；
- 不直接应用 canonical global update candidate，也不自行宣布 Article `PUBLISHED`。

### Required Outputs

```text
Published Path
Published Route
Front Matter Result
Series Result
Internal Link Result
Semantic Diff Result
Build Commands
Build Result
Warnings
Errors
Files Written
Recommended Article Transition
Recommended Status Changes
Canonical Update Candidate
Checkpoint Readiness
```

### Gate Responsibility

负责 Publish 与 Build Verify 的执行结果。Reviewer `PASS` 但 Hugo build 失败时，必须返回 `FAILED_PUBLICATION`；只有 Master 在验证 Final PASS、Publisher PASS、Build PASS 与 repository consistency 后才能写 `PUBLISHED`。

### Stop Conditions

- Article 不是 `FINAL`；
- semantic mapping 需要改变知识内容；
- source contradiction、broken build、link / metadata failure；
- unrelated dirty changes 无法安全隔离。

### Handoff Contract

成功时向 Master 交付上述结构化 Publication Result、完整发布文件清单、build evidence 与 checkpoint readiness；失败时返回 `FAILED_PUBLICATION` 或 `RETURN_TO_REVIEW`，不得自行降级。Publisher 的 `PASS` 只允许 Master 进入 State Update / Git Diff Verify，不能跳过独立 Article commit 与 Commit Verify。

### Context Isolation Rules

只需要 FINAL artifacts 与发布规则，不读取 Author hidden reasoning。若 semantic difference 不为零，以 repository diff 为证据返回 Review。

### Model / Reasoning Guidance

偏好机械一致性、严格验证和低变异操作；构建错误按事实诊断，不把 warning / static check 扩大成运行时验收。

## 8. Part Auditor

### Purpose

在 Part 末尾使用 fresh context 审查跨文章一致性、学习递进、Lab / DSH / BuildPilot boundary 与职业能力覆盖，决定是否允许进入下一 Part。

### Inputs

- Part boundary 与 canonical；
- 该 Part 所有 Article Card、published articles、evidence summary、Lab result、Glossary 与 status。

### Required Reads

- `course-factory.md` 的 Part Audit / degradation rules；
- canonical、Glossary、status；
- 该 Part 全部实际生产文章的 Article Card 与 Published Content；
- relevant Evidence summaries、Lab results 与前后 Part bridge。

### Allowed Writes

- Part Audit Report；
- cross-article Findings、affected Article、severity、required actions 与 Gate；
- `status.md` / run-state 的 audit result candidate；只能由 Master 验证后落盘。

### Forbidden Actions

- 不重写整个 Part，不直接修正文；
- 不把 optional article 静默改成 required；
- 不因评分模式可疑就自动判失败；先输出 `QUALITY_DEGRADATION_REVIEW`；
- 不执行 Course Final Audit，除非当前任务明确是 final scope。

### Required Outputs

- Part Audit Report；
- Concept Drift、Glossary Drift、Contradiction、Duplication、Missing Dependency、Forward Reference、Learning Progression、Job Competency Coverage 检查；
- Lab / DSH / BuildPilot mode-specific checks；
- Findings、Affected Articles、Severity、Required Actions、`PASS / FAIL` Gate。

### Gate Responsibility

Part Auditor `PASS` 是进入下一 Part 的必要条件。`BLOCKER / MAJOR` 必须把具体受影响 Article 返回必要状态修复，再执行 targeted re-audit。修复 Article 使用独立 targeted repair commit；Audit `PASS` 的 durable report / state update 使用独立 `Audit Agent Engineering Part X` commit 并验证，不得混入下一 Article checkpoint。

### Stop Conditions

- required Article / Lab artifact 缺失；
- canonical、status 与 published series 冲突；
- 发现课程架构级矛盾或持续质量退化，无法靠局部 Article 修复。

### Handoff Contract

向 Master 交付 Part Gate、Finding register、affected Article 和最小返工范围。Master 只按报告路由 worker，不让 Auditor 直接 repair。

### Context Isolation Rules

使用 fresh context；不读取各 Author / Reviewer 的隐藏推理或 self-score。审计依据是跨 Article repository artifact 与真实 Lab / build evidence。

### Model / Reasoning Guidance

需要较高的跨文档推理与模式识别；保持 evidence-backed，区分一次异常、质量退化信号和真实 Gate failure。
