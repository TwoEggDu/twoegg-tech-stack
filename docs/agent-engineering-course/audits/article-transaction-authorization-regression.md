# Article Transaction Authorization Contract Regression

- Status: `CONTRACT-SCENARIO REVIEW / NOT RUNTIME EXECUTION`
- Baseline: `bec466c04fbfcabed60d71a2da3998f71ff87f07`
- Scope: Course Factory 的单篇 Article continuation authority、显式收窄、停止条件与 multi-Article policy 分账。
- Limits: 下列均为合同推演，不启动 Article 17、不创建 Article workspace、不产出 Observed Result，也不证明任何 runtime 已执行。

## Scenario results

### ATAR-01 — 启动 Article 17 默认授权完整 transaction

- Initial State: Article 16 resolver=`END_ARTICLE`；Article 17=`PRECHECK / NOT_STARTED`；`article_authorization=INACTIVE`；旧run的`forbidden_articles`包含17。
- Human Instruction: “启动 Article 17”。
- Expected Gate Sequence: fresh reconciliation -> 清除或覆盖同一Article旧禁止项 -> PRECHECK -> ARTICLE_KICKOFF -> 全部适用生产Gate -> checkpoint/read-only tail -> END_ARTICLE_17。
- Expected Stop/Continue Decision: 每个中间Gate PASS后自动继续；只在真实blocker、明确stop line或END停止。
- Expected Repository Writes: 仅Article 17合同允许的阶段文件；最终唯一completion commit与一次push；END后writes=`ZERO`。
- Result: `PASS`。

### ATAR-02 — Evidence Gate PASS 自动进入 Outline

- Initial State: Article N在`EVIDENCE_GATE`，authorization=`ACTIVE / ARTICLE_TRANSACTION`，无stop line。
- Human Instruction: 既有“继续 Article N”授权。
- Expected Gate Sequence: Evidence validation PASS -> state=`EVIDENCE_READY` -> dispatch fresh Author for OUTLINE。
- Expected Stop/Continue Decision: 自动继续，不返回等待确认。
- Expected Repository Writes: 只写当前Article的Evidence durable state与随后获准的outline artifact；中间不commit/push。
- Result: `PASS`。

### ATAR-03 — Outline PASS 自动进入 Draft

- Initial State: Article N在`OUTLINE`，authorization有效，无blocker。
- Human Instruction: 既有完整Article授权。
- Expected Gate Sequence: Author result -> envelope/artifact/Gate validation -> `OUTLINE_READY` -> dispatch fresh Author for `AUTHOR_DRAFT`。
- Expected Stop/Continue Decision: Outline PASS不是stop condition。
- Expected Repository Writes: 当前Article `outline.md`及后续Gate允许的`draft.md`；不提前commit。
- Result: `PASS`。

### ATAR-04 — Review Finding 自动修订闭环

- Initial State: Article N在`REVIEW`，Reviewer返回可修复Finding，`review_cycle < MAX_REVIEW_CYCLES`；当前policy为`major_finding_unresolved=false / review_cycle_exhausted=true`且尚未exhausted。
- Human Instruction: 既有完整Article授权。
- Expected Gate Sequence: REVIEW -> REVISION -> REVIEW_RECHECK；关闭Findings后进入FINAL_GATE。
- Expected Stop/Continue Decision: 自动修订与复核；首轮或可修复`BLOCKER / MAJOR`不得命中hard lock或`HUMAN_DECISION_REQUIRED`，只有最大轮次后仍未关闭才停止。
- Expected Repository Writes: 仅当前Article draft/review/trace允许的修订；中间不commit/push。
- Result: `PASS`。

### ATAR-05 — END Article 17 不启动 Article 18

- Initial State: Article 17完成remote verification与只读post-commit reconciliation。
- Human Instruction: 原授权仅为Article 17。
- Expected Gate Sequence: END_ARTICLE_17 -> next pointer candidate Article 18 / PRECHECK / NOT_STARTED。
- Expected Stop/Continue Decision: 在Article 18 PRECHECK前停止；`next_article_authorized=false`。
- Expected Repository Writes: completion commit后repository writes=`ZERO`；不创建Article 18文件。
- Result: `PASS`。

### ATAR-06 — 人类明确只执行 Evidence Gate

- Initial State: Article N在EVIDENCE_GATE前，默认完整授权尚未激活。
- Human Instruction: “本次只执行 EVIDENCE_GATE”。
- Expected Gate Sequence: 执行并验证EVIDENCE_GATE；PASS后记录下一Gate为OUTLINE但不派发Author。
- Expected Stop/Continue Decision: 命中`EXPLICIT_HUMAN_STOP_LINE`后投影`factory_status=PAUSED / active_blocker=NONE / stop_reason=EXPLICIT_HUMAN_STOP_LINE / human_decision_required=false`；`current_gate=OUTLINE / next_action=CONTINUE_ARTICLE_N_AT_OUTLINE`，authorization=`INACTIVE / scope=NONE / article=N / continue_until=NONE / auto_continue_after_gate_pass=false / matched stop line / next_article_authorized=false`。
- Expected Repository Writes: 只允许Evidence Gate当前Article变更；不创建outline/draft，不commit/push，除非独立Git合同另有明确许可。
- Result: `PASS`。

### ATAR-07 — Evidence BLOCKED

- Initial State: Article N Research / Evidence存在无法收窄的核心Claim缺口。
- Human Instruction: 完整Article授权有效。
- Expected Gate Sequence: EVIDENCE_GATE -> `BLOCKED_EVIDENCE`。
- Expected Stop/Continue Decision: 停止并报告阻塞Claim、缺失证据与恢复条件；不进入OUTLINE。
- Expected Repository Writes: 仅当前Evidence/trace允许的blocker记录；不创建outline/draft。
- Result: `PASS`。

### ATAR-08 — Required Lab失败

- Initial State: Article N的核心Claim依赖required Lab，Lab无法安全完成或验证失败。
- Human Instruction: 完整Article授权有效。
- Expected Gate Sequence: LAB_EXECUTE / LAB_OBSERVATION -> `FAILED_REQUIRED_LAB`，不进入Evidence PASS。
- Expected Stop/Continue Decision: 真实blocker，停止并给出准确恢复条件。
- Expected Repository Writes: 仅Lab raw failure、observation与trace允许文件；不伪造Observed Result。
- Result: `PASS`。

### ATAR-09 — Build失败

- Initial State: Article N已通过Final与Publish候选，但Hugo Build Verify失败。
- Human Instruction: 完整Article授权有效。
- Expected Gate Sequence: BUILD_VERIFY -> `FAILED_BUILD` / publication failure route。
- Expected Stop/Continue Decision: 停止；不得进入checkpoint commit/push。
- Expected Repository Writes: 保留当前Article可诊断的允许变更；无completion commit、无push。
- Result: `PASS`。

### ATAR-10 — Repository冲突

- Initial State: Resume或Gate边界发现unrelated dirty change、错误branch或local/origin/live divergence。
- Human Instruction: 任意Article START / CONTINUE。
- Expected Gate Sequence: reconciliation -> `REPOSITORY_CONFLICT`，不派发worker。
- Expected Stop/Continue Decision: fail closed并等待用户处理或扩大授权。
- Expected Repository Writes: `ZERO`；不得覆盖、stash、提交或混入无关变更。
- Result: `PASS`。

### ATAR-11 — Resume不重复已完成worker

- Initial State: durable paused checkpoint已验证上一Gate PASS，active worker=`NONE`、authorization=`INACTIVE`，current/next Gate明确。
- Human Instruction: “继续 Article N”。
- Expected Gate Sequence: fresh Resume Reconciliation -> validate persisted result/repository/remote -> idempotent `ARTICLE_AUTHORIZATION_RESUME` at current Gate -> dispatch only next required worker。
- Expected Stop/Continue Decision: authorization恢复为`ACTIVE / ARTICLE_TRANSACTION / continue_until=END_ARTICLE`；不回放PRECHECK、Kickoff、已完成Gate或旧worker，也不因worker已结束而停止。
- Expected Repository Writes: 只允许next Gate的当前Article writes；不重写已通过artifact。
- Result: `PASS`。

### ATAR-12 — continuous_run=false不截断当前Article

- Initial State: Article N authorization=`ACTIVE`，`continuous_run.enabled=false`。
- Human Instruction: “继续 Article N”。
- Expected Gate Sequence: 当前及剩余Gate连续执行至END或blocker。
- Expected Stop/Continue Decision: 当前Article内部继续；continuous policy只在END后生效。
- Expected Repository Writes: 遵守当前Article one-commit/one-push边界；不写下一Article。
- Result: `PASS`。

### ATAR-13 — END后auto_continue=false阻止下一篇

- Initial State: Article N resolver=`END_ARTICLE`，`auto_continue_after_end_article=false`。
- Human Instruction: 原授权仅覆盖Article N。
- Expected Gate Sequence: END_ARTICLE_N -> N+1 pointer candidate only。
- Expected Stop/Continue Decision: 停止；不运行N+1 PRECHECK。
- Expected Repository Writes: END后`ZERO`；N+1 workspace/content均不存在。
- Result: `PASS`。

### ATAR-14 — Article N授权不得泄漏至N+1

- Initial State: Article N完整授权有效，N+1无独立人类授权。
- Human Instruction: “启动 Article N”。
- Expected Gate Sequence: 只执行N的Gate并结束于END_ARTICLE_N。
- Expected Stop/Continue Decision: 即使next pointer已知，也在N+1 PRECHECK前停止。
- Expected Repository Writes: 仅Article N transaction范围；Article N+1文件为`ZERO`。
- Result: `PASS`。

### ATAR-15 — Pointer candidate不等于Kickoff

- Initial State: `current_article=N / current_gate=PRECHECK / factory_status=READY`，authorization=`INACTIVE`。
- Human Instruction: 无Article N START / CONTINUE指令。
- Expected Gate Sequence: 无；仅可只读报告candidate。
- Expected Stop/Continue Decision: 不运行PRECHECK，不执行ARTICLE_KICKOFF。
- Expected Repository Writes: `ZERO`。
- Result: `PASS`。

### ATAR-16 — Worker PASS不是Article completion

- Initial State: 当前worker返回schema-valid PASS，next Gate合法，Article尚未完成。
- Human Instruction: 完整Article授权有效。
- Expected Gate Sequence: validate result -> transition -> dispatch next worker；最终completion仍需commit/push/remote resolver。
- Expected Stop/Continue Decision: worker PASS触发继续，不产生END_ARTICLE。
- Expected Repository Writes: 仅当前/下一Gate允许的Article变更；不提前写completion SHA或END事实。
- Result: `PASS`。

### ATAR-17 — 显式收窄优先于默认完整授权

- Initial State: Article N可继续多个Gate，尚无ACTIVE授权。
- Human Instruction: “继续 Article N，但完成Draft后停止，不要启动Review”。
- Expected Gate Sequence: 从真实checkpoint执行到AUTHOR_DRAFT并验证；记录REVIEW为next allowed gate但不派发Reviewer。
- Expected Stop/Continue Decision: `explicit_stop_line=AFTER_AUTHOR_DRAFT`覆盖默认范围；投影`PAUSED / active_blocker=NONE / stop_reason=EXPLICIT_HUMAN_STOP_LINE / human_decision_required=false / current_gate=REVIEW / next_action=CONTINUE_ARTICLE_N_AT_REVIEW`，authorization重置`INACTIVE`并保留matched stop line。
- Expected Repository Writes: 只到Draft Gate允许的当前Article artifacts；不创建Review结果，不commit/push除非该边界合同另有许可。
- Result: `PASS`。

## Contract regression verdict

- Scenario count: `17 / 17`
- Scenario result: `17 PASS / 0 FAIL`
- Runtime execution: `NOT_RUN`
- Article 17 production: `NOT_STARTED`

## Independent read-only review

- Reviewer: `/root/factory_auth_reviewer`（fresh real Reviewer）
- Review mode: `READ_ONLY / NO REPOSITORY WRITES`
- Initial verdict: `FAIL / 1 BLOCKER / 2 MAJOR / 0 MINOR`

| Finding | Required Disposition | Applied Disposition | Recheck |
|---|---|---|---|
| `BLOCKER-01` | 普通Review Finding不得被当前`stop_on`提前截断 | `major_finding_unresolved=false`；仅`review_cycle_exhausted`且最大轮次后仍有未关闭`BLOCKER / MAJOR`才形成终态锁；ATAR-04补齐断言 | `CLOSED` |
| `MAJOR-01` | 冻结mid-Article CONTINUE的authorization再激活路径 | 新增fresh Resume Reconciliation后的幂等`ARTICLE_AUTHORIZATION_RESUME`，从durable current Gate恢复且不回放PRECHECK、Kickoff、worker或已通过Gate；ATAR-11补齐断言 | `CLOSED` |
| `MAJOR-02` | 闭合`EXPLICIT_HUMAN_STOP_LINE` durable schema映射 | 新增唯一`PAUSED / active_blocker=NONE / stop_reason=EXPLICIT_HUMAN_STOP_LINE / human_decision_required=false`投影、resume Gate与authorization reset；ATAR-06/17补齐断言 | `CLOSED` |

- Recheck verdict: `PASS / 0 BLOCKER / 0 MAJOR / 0 MINOR`
- Reviewer write confirmation: `ZERO`
- Article 17 Gate / worker / production execution: `ZERO / NOT_STARTED`
