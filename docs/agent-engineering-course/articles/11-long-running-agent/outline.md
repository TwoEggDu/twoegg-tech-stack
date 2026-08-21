# Article 11 Detailed Outline｜Long-running Agent

> Outline Gate candidate：`PASS_RECOMMENDED`。本文件是 Author 的 Detailed Outline 候选，不是正文、Formal Review、Final Gate 或发布批准。

## 0. Article decision

### Article type

- 类型：`原理 / 长任务恢复机制篇（M / LAB_ARTICLE）`。
- 选择理由：本篇承接 Article 10 的确定性 State / Workflow 骨架，回答执行跨越超时、取消、暂态失败和响应丢失后，Runtime 如何保存可恢复控制面并选择 Retry、Resume、Reconcile、Compensate、Ask 或 Stop。重点是恢复合同与副作用不确定性，不适合写成某个 Workflow 产品的 API 教程。
- 结构：`问题空间 -> 抽象模型 -> 具体机制 -> 工程判断 -> 验证边界`。

### 最短 thesis

`Long-running Agent 的 Recovery 不是“再跑一次”，而是先用 Checkpoint 还原已知与未知，再依据副作用语义、稳定身份、Retry Budget、取消来源和恢复边界，决定 Retry、Reconcile、Resume、Compensate、Ask 或 Stop。`

### Reader Change

读前，读者容易把 timeout 当成 work stopped，把 cancellation request 当成 rollback，把 Retry Budget 当成安全许可，把 current State JSON 当成 Checkpoint，或把 Resume 理解为从原代码行继续。读后，读者应能：

1. 区分 Timeout、Cancellation、Retry、Resume、Replay 与 Recovery，拒绝用单一 `FAILED / CANCELLED` 互相推出；
2. 用一组恢复问题审查 Checkpoint：同一 run 是谁、哪些 action 已提交、什么仍在进行、budget 还剩多少、下一安全动作是什么；
3. 在执行 Retry 前先判断 effect 是否明确未发生、可查询、幂等、可补偿或仍为 unknown；
4. 说明 cancellation request、listener observation、work stopped 与 side-effect rollback 是四类不同事实；
5. 只从明确的 Recovery Boundary 恢复，并理解 Resume 可以借助 Replay，却不保证不重执行 boundary 后的调用；
6. 以 `known / unknown / unverified / next safe action` 输出 partial result，而不是把中断伪装成 success；
7. 用 Lab 04 的 LR-01—LR-08 区分正例、受控失败与拒绝恢复，并保留 fixture / product / course-schema ceiling；
8. 明确 Article 11 到 recovery control plane 为止，不展开 Article 12 的 Context / Memory 质量问题。

### Teaching Spine

1. **Problem space**：任务变长后，失败不再只是“这一调用报错”，而是“已经发生到哪、外部 effect 是否已经提交、还能从哪里安全继续”变得不确定。
2. **Abstract model**：Recovery 先分类 control fact，再读取 Checkpoint 中的 identity、committed history、in-flight uncertainty、budget 与 continuation，最后选择有限的恢复动作。
3. **Concrete mechanism**：Checkpoint candidate、Retry decision table、cancellation / timeout trace、resume validator、partial-result contract 与 fail-closed stop condition。
4. **Engineering judgment**：Retry eligibility 先由 effect semantics 与 stable identity决定，budget 只限制次数；无法判定副作用时，lookup / same-intent replay、compensate、ask 或 stop 优先于 blind retry。
5. **Verification boundary**：Lab 04 在当前 Windows / .NET、single coordinator、本地 fake store和named faults中观察了8个冻结case；它不证明production、distributed、OS-crash、cross-platform或exactly-once保证。

---

## 1. 为什么任务一变长，失败就不再只是一个错误码

- **Reader Question**：Article 10 已经有合法 State 和 Workflow，为什么长任务还需要新的工程层？
- **Section Goal**：建立问题空间。Workflow 知道什么 transition 合法，却不自动知道 interruption 前后哪些事实已持久化、side effect 是否发生、能否再次投递。
- **Core Claim**：Timeout、Cancellation、Retry、Resume、Replay 与 Recovery 是不同控制事实；单一 `FAILED / CANCELLED` 无法回答请求来源、工作停止、已发生副作用、重试资格或恢复位置。
- **Claim IDs / Evidence IDs**：`11-C01`、`11-C05`、`11-C06`；`11-E01`、`11-E03`、`11-E05`、`11-E09`。
- **Wording strength**：
  - `11-C01 CONFIRMED / FIXTURE-SCOPED`：可写“需要分开记录”，不写“所有系统共享同一状态机”。
  - `11-C05 CONFIRMED / FIXTURE-SCOPED`：只使用 pre-effect cooperative cancellation 观测。
  - `11-C06 CONFIRMED / COURSE-RUNTIME-SCOPED`：只说明本课程 Runtime 的 boundary behavior。
- **Opening scenario duty**：
  1. Agent 调用外部 Tool 创建 finding；
  2. store 已写入，但 Runtime 在读到响应前中断；
  3. 下一进程只看到“调用失败”；
  4. 若直接重试，可能得到一个结果，也可能创建第二条副作用。
- **Required contrast**：

  ```text
  short failure question:
    this call returned what?

  long-running recovery question:
    what was committed?
    what may already have happened?
    what is still unknown?
    what action is still safe?
  ```

- **Example / Figure Duty**：F11-01 用“响应丢失窗口”画出 `effect persisted -> response lost -> state uncertain`，不要先贴产品 API。
- **Guardrail / Counter-evidence**：
  - Article 06 已区分 timeout / caller cancellation，也暴露 single-process de-dup 的 crash window；它没有证明 cross-process recovery。
  - Article 10 的 Runtime State 只表达当前已提交位置，不自动等于 durable Checkpoint。
- **Boundary / Stop Line**：不把所有长任务都宣判为必须 checkpoint；是否值得持久化要看重跑成本、副作用、人工等待和恢复价值，本篇只建立判断合同。
- **Bridge**：先把容易混成一个“失败处理”的六个词拆开，再讨论 Checkpoint 保存什么。

---

## 2. 先分类再恢复：Timeout、Cancellation、Retry、Resume、Replay 与 Recovery

- **Reader Question**：六个术语分别回答哪一个控制问题？
- **Section Goal**：建立全文术语表和最小恢复链，避免把不同产品的 resume / replay 规则写成统一算法。
- **Core Claim**：Recovery 是失败分类后的策略选择；Retry、Resume、Replay、Compensate、Ask 与 Stop 都可能是其中一条路径，但彼此不能替代。
- **Claim IDs / Evidence IDs**：`11-C01`、`11-C06`；`11-E01`、`11-E03`、`11-E04`、`11-E05`、`11-E09`。
- **Wording strength**：产品规则必须分别标出 .NET、LangGraph current docs 与 AWS Standard Workflow scope；课程统一术语只是 working definition。
- **Required term table duty**：

  | Term | 本篇工作边界 | 不自动证明 |
  |---|---|---|
  | Timeout | 等待或执行超过 deadline / duration budget 后形成的控制事实 | 底层工作已停止、effect 未发生 |
  | Cancellation | requester 发出停止请求，listener 在 cooperative boundary 观察并响应 | 强制 kill、rollback、checkpoint 已保存 |
  | Retry | 在明确 policy、identity 与 budget 下再次尝试同一 action intent | 安全、幂等、恢复到正确 State |
  | Resume | 从已识别的 durable continuation boundary 继续同一 run | 回到原代码行、不重执行、等同 Replay |
  | Replay | 根据 history / checkpoint 重演控制路径，重算范围由产品定义 | side-effect safety、等同 Resume |
  | Recovery | 分类后选择 Resume、Retry、Reconcile、Compensate、Ask 或 Stop，并保存不确定性 | “再跑一次”、所有外部系统可回滚 |

- **Central model duty**：

  ```text
  Failure / Cancel / Timeout
      -> classify origin + effect uncertainty
      -> load and validate Checkpoint
      -> Retry / Reconcile / Resume / Compensate / Ask / Stop
      -> continue only within explicit Recovery Boundary
  ```

- **Product counter-evidence duty**：
  - LangGraph current docs：resume 可从 checkpoint boundary replay forward，未完成 task可能再次执行；不能写成“从原代码行继续”。
  - AWS Step Functions current docs：redrive保留successful steps并重调度failed Task，说明产品恢复规则不同。
  - .NET cancellation：requester / listener cooperative contract；不能从 token requested 推出 rollback。
- **Example / Figure Duty**：T11-01 术语表；F11-02 分类树。图注必须写 `COURSE WORKING DEFINITIONS / PRODUCT RULES DIFFER`。
- **Boundary / Stop Line**：不比较产品优劣，不做 LangGraph / AWS 教程，不提出跨产品统一 enum。
- **Bridge**：分类只告诉我们“发生了什么类型的中断”，还必须有 durable artifact 回答“发生到哪里”。

---

## 3. Checkpoint 不是 State 的截图，而是一份恢复判定输入

- **Reader Question**：只把 current State 序列化到磁盘，为什么还不能安全 Resume？
- **Section Goal**：从“字段清单”提升到“恢复问题清单”：Checkpoint 必须让 Runtime 区分已提交、剩余、in-flight 与 unknown。
- **Core Claim**：对本课程 candidate schema，durable run identity、authoritative State / revision、completed actions / Evidence、remaining actions、in-flight identity、budget、cancellation、continuation、partial result 与 integrity共同构成恢复判定输入；只有 current State 不足以处理 in-flight uncertainty。
- **Claim IDs / Evidence IDs**：`11-C02`、`11-C08`、`11-C09`；`11-E03`、`11-E04`、`11-E07`、`11-E12`、`11-E13`。
- **Wording strength**：
  - `11-C02 CONFIRMED / PROPOSAL-CONFORMANCE`：只能写“课程最小 candidate 在冻结case中需要回答这些问题”，不写“完整生产 schema”。
  - `11-C08`只支持本 Runtime 对缺字段 fail closed与冻结产物可复现。
  - `11-C09 PRODUCT-DOC-SCOPED`：实现可重叠，证明职责不同。
- **Required recovery-question table duty**：

  | 恢复问题 | Candidate fields | 缺失时的风险 |
  |---|---|---|
  | 这是哪个执行？ | schema / fixture version、run / case / goal identity | 把不同 run 拼接或误恢复 |
  | 当前提交到哪里？ | state、revision、last committed sequence | stale continuation、重复提交 |
  | 什么已经完成？ | completed action、intent digest、result / Evidence refs | 重跑已提交行为 |
  | 什么仍需完成？ | remaining actions、continuation / next safe action | 无法解释下一步为何安全 |
  | 什么可能正在发生？ | in-flight action、idempotency key、phase、attempt、result status | 把 unknown 当作未执行 |
  | 还允许尝试几次？ | retry max / used / remaining、last failure class | 无限重试或错误计账 |
  | 谁请求停止？ | requested / observed / origin | timeout 与 caller cancel 混写 |
  | 已知与未知分别是什么？ | partial result + provenance | 把 incomplete 涂成 success |
  | artifact 是否仍可接受？ | integrity digest、version invariant | 在损坏或不兼容状态上继续 |

- **What not to save duty**：不保存完整隐藏推理 / CoT、credential、绝对临时路径、PID / wall-clock等非确定性字段，也不把未验证自然语言结论当成 authoritative State。
- **Checkpoint / Memory boundary**：
  - 可以写：同一 persistence capability 可能同时服务 thread-scoped memory 与 recovery。
  - 必须写：本篇按证明职责区分——Checkpoint 要回答 control position、continuation 与 in-flight uncertainty；Memory presence 不自动回答这些问题。
  - 不得写：Checkpoint 与 Memory 必然是两套物理存储。
- **Negative example duty**：LR-06 的 State 已到 `REGISTERING_FINDING`，却没有 `in_flight_action`；resume validator 在新的 fake-store access 前返回 `RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`，start / resume store access count 都为 `1`。
- **Example / Figure Duty**：
  - T11-02：恢复问题表。
  - F11-03：`State snapshot` 与 `Recovery Checkpoint` 的包含关系，禁止画成 industry schema mandate。
- **Boundary / Stop Line**：不覆盖partial disk write、bit rot、schema migration、concurrent writer、lease或crash-consistent storage。
- **Bridge**：Checkpoint 还原了 effect uncertainty；下一步不是先看 budget，而是先判断这次 action 是否有资格重送。

---

## 4. Retry：先判断副作用语义，再消耗 Budget

- **Reader Question**：发生 transient failure 且 budget 仍有余额，为什么仍可能不能 Retry？
- **Section Goal**：建立 Retry eligibility 与 Retry Budget 的顺序，正面处理 lost response 和非幂等副作用。
- **Core Claim**：Retry Budget 只限制尝试次数，不提供 retry safety；自动 Retry 还需要知道 original 未 apply，或同一 intent 可以通过 stable identity、lookup / idempotent effect contract安全重放。
- **Claim IDs / Evidence IDs**：`11-C03`、`11-C04`、`11-C07`；`11-E02`、`11-E03`、`11-E10`、`11-E11`。
- **Wording strength**：
  - `11-C03 / C04 CONFIRMED / FIXED-STORE-SCOPED`。
  - RFC 9110只支持HTTP idempotent method / lost-response语义，不扩成通用 idempotency-key store。
  - 禁用 `exactly-once` 保证语态。
- **Required decision order duty**：

  ```text
  same action intent?
    -> stable action identity / intent digest?
    -> effect state is ABSENT_KNOWN / QUERYABLE / IDEMPOTENT /
       COMPENSATABLE / UNKNOWN?
    -> failure class is retryable?
    -> retry budget remains?
    -> RETRY / LOOKUP-RECONCILE / COMPENSATE / ASK / STOP
  ```

- **Required decision table duty**：

  | Effect knowledge | Identity / contract | Budget | Safe candidate | 禁止推导 |
  |---|---|---:|---|---|
  | 明确未 apply | same intent | 有 | Retry | 所有 transient 分类都正确 |
  | 已 apply，可按 stable identity 查询 | same action / key | 有 | Lookup / reconcile，必要时 same-intent replay | exactly-once |
  | 已 apply，有明确补偿合同 | compensation identity / authority | 视 policy | Compensate 后再评估 | rollback 必然成功 |
  | 结果 unknown，无法查、无法幂等、无法补偿 | 不足 | 任意 | Ask / Stop | budget 仍有就盲重试 |
  | permanent / invariant failure | 任意 | 任意 | Stop / Escalate | 换异常名继续重试 |

- **Lab case duties**：
  - LR-03：pre-apply transient once，attempts=`2`、effect=`1`，说明已知未 apply + budget 内可 Retry。
  - LR-07：transient always，attempts=`2`后 `RETRY_BUDGET_EXHAUSTED / INCOMPLETE`，effect=`0`，说明有安全重试资格也必须受 budget 停止。
  - LR-04：apply then lost response，effect 已为`1`；fresh resume用 same action / intent / key 得到 existing effect，final 仍为`1`。
  - LR-05：相同 unknown window 后用 new delivery blind append，effect 从`1`变`2`，terminal=`DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`。
- **Engineering judgment duty**：LR-05 是被实验接受的negative case，不是“推荐实现通过”。`8 / 8 accepted`指8个case都符合冻结判据，不是8个case都成功。
- **Example / Figure Duty**：F11-04 用 LR-04 / LR-05 双泳道对照 stable identity reconcile 与 blind redelivery；T11-03 是 retry decision table。
- **Guardrail / Counter-evidence**：
  - Idempotent intended effect不等于每次日志、计费或旁路副作用 exactly once。
  - fake store和checkpoint是同机不同文件，不是分布式事务或独立 service。
  - Lab没有backoff、jitter、真实HTTP、rate limit或通用 transient classifier。
- **Bridge**：Retry 处理“是否再次尝试同一 intent”；取消处理“谁要求停、停在何处”，不能用 Retry 规则替代。

---

## 5. Cancellation 与 Timeout：保留来源、观察点和最后安全边界

- **Reader Question**：用户已经点了取消，为什么 Runtime 还要继续写 Checkpoint 或 partial result？
- **Section Goal**：把 cancellation request、listener observation、work stopped和rollback分开；说明取消后的恢复资格来自 safe boundary 与 in-flight state，而不是“cancelled”标签。
- **Core Claim**：Cancellation 是 cooperative request / observation contract；Timeout 是独立 origin。只有在显式safe boundary且side-effect uncertainty可判定时，Runtime才能提出 Resume candidate。
- **Claim IDs / Evidence IDs**：`11-C01`、`11-C05`、`11-C06`、`11-C07`；`11-E01`、`11-E09`、`11-E13`。
- **Wording strength**：`FIXTURE-SCOPED`。只说明 LR-02 的 pre-effect cancellation 与 LR-08 的 deterministic timeout，不外推 mid-I/O、forced kill、race或rollback。
- **Required fact split duty**：

  | Fact | 最小记录 | 不可省略的判断 |
  |---|---|---|
  | request issued | origin、requested | 谁发出停止请求 |
  | listener observed | state、sequence、observed | Runtime 在哪里响应 |
  | work stopped | phase / terminal evidence | 是否仍有底层工作 |
  | side effect state | absent / applied / unknown | 能否恢复或重送 |
  | continuation | last committed boundary、next safe action | 取消后允许做什么 |

- **LR-02 / LR-08 contrast duty**：
  - LR-02 START：`CANCELLED / INCOMPLETE / CALLER`，effect / access / attempt=`0 / 0 / 0`；Checkpoint保留已完成Evidence、剩余action与`REGISTER_FINDING`。
  - LR-02 RESUME：different PID，final effect=`1`，committed evidence action不重跑。
  - LR-08：`TIMED_OUT / INCOMPLETE / TIMEOUT`，effect / access / attempt=`0 / 0 / 0`，trace没有caller-cancel event。
- **Why persist after cancellation duty**：最后安全状态不是为了拒绝用户停止，而是为了让下一次执行知道哪些事实已提交、哪些 action 未开始、是否有 unknown side effect，以及下一动作是否仍安全。
- **Example / Figure Duty**：F11-05 画 `request -> observed at checkpoint -> terminal -> explicit resume`；T11-04 对照 LR-02 / LR-08 origin与terminal。
- **Guardrail / Counter-evidence**：
  - 不写“token requested 后 Tool 已停止”。
  - 不写“取消后总能继续”。
  - 不把 Lab 的named safe seam写成真实slow I/O或OS crash。
- **Bridge**：即使取消点安全，Resume仍要定义“从哪个 boundary继续”以及boundary之后哪些调用可能 Replay。

---

## 6. Resume 与 Recovery：从 Boundary 继续，不是从原代码行复活

- **Reader Question**：Resume、Replay 与 Recovery之间怎样组合，为什么恢复成功仍可能重新调用 Tool？
- **Section Goal**：建立 explicit Recovery Boundary、fresh-process resume与有限恢复动作；把 same-run continuation 和 blind restart分开。
- **Core Claim**：Resume 从产品或课程定义的 Checkpoint Boundary 继续；Replay可以是实现Resume的一种机制，boundary后的调用可能重执行。Recovery则负责在当前 effect knowledge下选择 Resume、Retry / Reconcile、Compensate、Ask或Stop。
- **Claim IDs / Evidence IDs**：`11-C04`、`11-C05`、`11-C06`、`11-C08`；`11-E03`、`11-E05`、`11-E09`、`11-E11`、`11-E12`、`11-E13`。
- **Wording strength**：
  - course Runtime的fresh-process行为为 `COURSE-RUNTIME-SCOPED`。
  - LangGraph / AWS规则为product-scoped counter-evidence。
  - compensation只作为课程恢复候选，不声称Lab已执行或任意外部系统可回滚。
- **Required resume protocol duty**：

  ```text
  load checkpoint
    -> validate schema / integrity / fixture version
    -> validate current state + in-flight invariant
    -> reconstruct known / unknown / remaining / budget
    -> classify effect state
    -> select explicit recovery action
    -> commit a new trace event before new side effect
  ```

- **Fresh-process evidence duty**：
  - 每个formal suite有12个独立Runtime phase process；START / RESUME使用不同PID。
  - LR-02 Resume不重跑已committed evidence action。
  - LR-04 Resume会再次进入store调用边界，但使用same identity取得existing effect；这正说明`Resume != no re-execution`。
- **Recovery decision duty**：
  - safe pre-effect checkpoint：Resume到next safe action；
  - result unknown + queryable stable identity：Lookup / reconcile，再决定是否提交；
  - reversible effect + explicit compensation contract：Compensate，再以新Evidence决定下一步；
  - identity缺失或integrity / invariant失败：Refuse before new effect；
  - no safe automatic path：Ask / Stop，并输出partial result。
- **Negative evidence duty**：
  - LR-05证明blind restart / redelivery可重复effect。
  - LR-06证明“有State JSON”但缺in-flight identity时应fail closed。
- **Example / Figure Duty**：F11-06 恢复决策树；F11-07 LR-02与LR-04的fresh-process boundary对照。
- **Guardrail / Counter-evidence**：
  - named interruption不是OS crash、power loss或process-tree kill。
  - local JSON没有证明crash consistency、durability SLA或cross-machine恢复。
  - 不写“Resume 一定跳过所有已完成节点”或“Replay 一定从头执行”。
- **Bridge**：当自动恢复不安全或budget已耗尽，系统仍要交付一份不会伪造完成的结果。

---

## 7. Partial Result：把不完整性变成证据合同

- **Reader Question**：任务没有成功结束时，怎样返回对后续人或Runtime真正有用的结果？
- **Section Goal**：把 partial result从“尽量写点内容”升级为四类证据状态与唯一安全下一动作。
- **Core Claim**：本课程partial-result schema必须分别表达 `known / unknown / unverified / next safe action`，且每项可回指Checkpoint、Trace或fake store；unknown和unverified不能进入known。
- **Claim IDs / Evidence IDs**：`11-C07`、`11-C02`、`11-C08`；`11-E12`、`11-E13`。
- **Wording strength**：`11-C07 CONFIRMED / COURSE-SCHEMA-CONFORMANCE`；必须称为course proposal / fixed-case conformance，不写成行业标准。
- **Required schema duty**：

  ```text
  known
    = committed State / accepted Evidence支持的事实引用

  unknown
    = 已发生或可能发生、但结果无法确定的action / effect identity

  unverified
    = Goal仍要求、但尚无accepted Evidence的条件

  next_safe_action
    = 当前Evidence、authority、idempotency和budget下仍允许的动作，或NONE
  ```

- **Case mapping duty**：
  - LR-02：known保留completed evidence，registration仍未完成，next=`REGISTER_FINDING`。
  - LR-04：unknown保留same action / effect identity，next是same-identity reconcile而非新delivery。
  - LR-05：duplicate effects已知，Goal未满足，terminal FAILED；不能把其中一条effect挑成“成功”。
  - LR-06：unknown in-flight无法由损坏Checkpoint恢复，next=`NONE`。
  - LR-07：known evidence保留，registration / verification / Goal仍unverified，next=`ASK_OR_STOP`。
  - LR-08：timeout origin已知，side effect未开始，case不自动提供Resume candidate。
- **Stop condition duty**：
  - retry budget exhausted；
  - in-flight identity / integrity invariant缺失；
  - effect unknown且无法query / idempotent replay / compensate；
  - required authority或人工判断缺失；
  - 已观察duplicate side effect；
  - 继续动作会把unknown伪装成known。
- **Example / Figure Duty**：T11-05 partial-result字段与provenance表；用LR-04和LR-07做一unknown、一exhausted对照。
- **Guardrail / Counter-evidence**：不写“partial result一定足够完整”；Lab字段是scripted fixed Goal下的schema conformance，不验证真实model evidence quality。
- **Bridge**：上述机制需要一组同时包含正例和反例的实验来约束；Lab 04不能只展示green terminal。

---

## 8. Lab 04：Expected 不等于 Observed，8 / 8 accepted 不等于8次成功

- **Reader Question**：Lab 04真正观察了什么，哪些结论仍然不能从green verifier推出？
- **Section Goal**：用LR-01—LR-08完整验证Teaching Spine，并明确Expected / Observed / Interpretation三层。
- **Core Claim**：在`lab04-fixture-v1`、当前Windows / .NET Host、single coordinator、本地fake store与named deterministic seams下，8个case都符合冻结判据；其中LR-05和LR-06的失败 / 拒绝正是required negative evidence。
- **Claim IDs / Evidence IDs**：`11-C01`—`11-C08`；`11-E07`—`11-E13`。
- **Wording strength**：每个case按自己的fixture / fixed-store / proposal / course-runtime / course-schema ceiling叙述；Lab总PASS不能覆盖单case terminal含义。
- **Expected / Observed split duty**：
  - Design在执行前冻结了Hypothesis、Fault、Expected Observable与16条Acceptance Criteria。
  - Observed只来自execution log、process evidence、checkpoint / trace / partial result / fake-store / case-result raw artifacts。
  - Researcher在执行后才完成 `Experiment -> Observation -> Interpretation -> Claim Status`。
- **Required case matrix duty**：

  | Case | Observed terminal / counts | 教学职责 | 证据上限 |
  |---|---|---|---|
  | LR-01 baseline | `SUCCEEDED`；effect=`1`；attempt=`1` | baseline合法成功 | fixed fixture |
  | LR-02 cancel + resume | START `CANCELLED / INCOMPLETE` effect=`0`；fresh RESUME后success effect=`1` | safe-boundary cooperative cancel与continuation | fixture-scoped |
  | LR-03 transient retry | success；attempts=`2`；effect=`1` | pre-apply retry + budget accounting | fixed-store-scoped |
  | LR-04 lost response | START `UNKNOWN_SIDE_EFFECT` effect=`1`；same-identity RESUME后仍`1` | unknown window与reconcile | fixed-store-scoped |
  | LR-05 unsafe comparator | START unknown effect=`1`；RESUME `DUPLICATE_SIDE_EFFECT_DETECTED / FAILED` effect=`2` | blind retry negative evidence | fixed-store-scoped |
  | LR-06 missing in-flight | START invalid candidate effect=`1`；RESUME `RECOVERY_REFUSED`，access不增加 | Checkpoint invariant fail closed | proposal conformance |
  | LR-07 exhausted | `RETRY_BUDGET_EXHAUSTED / INCOMPLETE`；attempts=`2`；effect=`0` | budget停止与partial result | course-schema + fixed store |
  | LR-08 timeout | `TIMED_OUT / INCOMPLETE / TIMEOUT`；attempt=`0`；effect=`0` | Timeout与caller cancellation分离 | fixture-scoped |

- **Execution summary duty**：
  - `.NET SDK 10.0.301 / Host 10.0.9 / Windows 10.0.19045 X64 / China Standard Time`。
  - offline restore=`0`；accepted Release build=`0 warnings / 0 errors`；static contract、run-a、run-b、compare均exit=`0`。
  - 每suite执行8个case、12个fresh Runtime phase processes。
  - run A / B中105个normalized files byte-identical，aggregate SHA-256=`27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a`；suite sentinel按设计不同且不在normalized set。
  - network / Provider / credential counters=`0 / 0 / 0`。
- **Failure-ledger duty**：保留first build `CS5001`、first static generated-source false positive和CIM OS-edition probe `Access denied`。说明最小patch后的green chain支持accepted run，但这些失败不是production recovery evidence。
- **Reproducibility wording duty**：只能写“frozen binary / fixture / normalization下的105个artifact可复现”；不写“Agent执行天然确定”“跨平台确定”或“production可靠”。
- **Example / Figure Duty**：
  - T11-06：上面的8-case matrix是主体，不贴大段JSON。
  - F11-08：三层证据图 `Expected -> Observed -> Interpretation`，明确green verifier不是Claim owner。
  - Artifact links：Lab README、execution log、verification summary、process evidence、run-a / run-b case artifacts。
- **Boundary / Must-not-imply**：
  - named interruption != OS crash / power loss / partial disk write；
  - local files != distributed transaction；
  - single coordinator不覆盖concurrency / lease / split-brain；
  - no network / Provider / credential不验证真实HTTP、远程store、授权或模型行为；
  - byte identity不是performance、availability或reliability指标。
- **Bridge**：Lab给出机制证据后，把它转成一组可用于设计评审的工程判断，而不是产品口号。

---

## 9. 一个坏 Long-running Runtime 通常怎么坏

- **Reader Question**：即使系统有checkpoint文件、retry配置和resume按钮，仍可能在哪些地方制造危险恢复？
- **Section Goal**：把C01—C08转成可执行的design-review heuristic。
- **Core Claim**：坏实现通常不是缺少一个“恢复”入口，而是丢失identity、effect uncertainty、budget顺序、safe boundary或partial-result provenance。
- **Claim IDs / Evidence IDs**：`11-C01`—`11-C08`；`11-E09`—`11-E13`。
- **Bad implementation examples**：
  1. 用一个`CANCELLED`同时表达caller request、timeout、listener observed与work stopped。
  2. 只保存current State，不保存completed / remaining / in-flight identity与continuation。
  3. 看到transient异常和budget余额就Retry，不先判effect是否可能已发生。
  4. 每次delivery生成新identity，随后把重复effect称为“至少成功了一次”。
  5. Resume时直接进入外部store，之后才验证checkpoint integrity / invariant。
  6. 把Replay写成“不会重执行”，或把restart写成“从原位置恢复”。
  7. budget耗尽后仍继续，或把attempt计数从第一次重试而非第一次投递开始。
  8. partial result只写一段摘要，把unknown和unverified省略或归入known。
  9. verifier green后删除first failures和negative terminal，只留下success case。
  10. 把stable key称为exactly-once，把本地fake store称为distributed-safe。
- **Review questions duty**：
  - Checkpoint能否回答“哪个action可能已发生”？
  - Retry decision在budget检查前还是后判断effect semantics？
  - cancellation trace是否分别记录requested / observed / origin？
  - Resume前是否先验证schema、integrity与in-flight invariant？
  - no-safe-path时系统能否返回Ask / Stop与honest partial result？
- **Example / Figure Duty**：将每个坏法至少映射到LR-02、LR-04、LR-05、LR-06或LR-07之一，不新增case或observed fact。
- **Boundary / Must-not-imply**：清单是由fixed evidence转写的review heuristic，不声称穷举所有distributed / production failure mode。
- **Bridge**：最后把“已经能审什么”与“仍未证明什么”明确分开，并在Article 12边界停止。

---

## 10. 工程边界：Recovery 是显式合同，不是可靠性总证明

- **Reader Question**：完成本文后，系统已经获得了哪些能力，又有哪些能力仍然完全没有被证明？
- **Section Goal**：收束工程判断、证据ceiling和Article 12 stop line。
- **Core Claim**：Article 11只建立long-running recovery control plane：checkpoint boundary、failure classification、retry eligibility、cancellation provenance、resume / recovery decision、in-flight effect uncertainty与partial result；它不等于production reliability或Context / Memory系统。
- **Claim IDs / Evidence IDs**：`11-C02`—`11-C09`；`11-E03`—`11-E13`。
- **This article owns**：
  - control fact分类；
  - course Checkpoint candidate与fail-closed invariant；
  - Retry Budget和effect semantics的判断顺序；
  - safe-boundary cancellation与fresh-process resume；
  - lost-response unknown window；
  - lookup / idempotent replay、compensate、ask、stop的有限决策面；
  - partial-result evidence contract；
  - Lab 04 fixed-scope verification。
- **This article does not own / prove**：
  - distributed transaction、exactly-once、cross-service atomicity；
  - rollback所有外部副作用；
  - OS crash、power loss、partial disk write、crash-consistent checkpoint；
  - concurrent caller、lease、lock、race、split-brain；
  - real HTTP、remote store、Provider、credential、authorization与rate limit；
  - production performance、availability、retention或cross-platform determinism；
  - checkpoint schema对所有系统充分；
  - model planning quality、decision determinism或context reconstruction quality。
- **Article 12 stop line**：
  - Checkpoint / Memory可以共用persistence capability；实现重叠不等于证明职责相同。
  - Article 11只问“执行控制面怎样从已知boundary继续”。
  - 跨run长期Memory、context选择 / 排序 / 压缩 / 重建 / 质量、knowledge retention和模型决策确定性属于Article 12及后续，不由Lab 04 PASS推出。
- **BuildPilot / DSH boundary**：不读取DeepSeek Harness源码，不实现或预演BuildPilot Runtime；canonical提到的后续关联只作为课程路由，不在本文展开。
- **Closing takeaway**：`能恢复的不是“上一次进程”，而是被 Checkpoint、identity、effect semantics 与 partial result共同限定的一段控制事实；边界说不清时，正确动作不是重跑，而是拒绝、询问或停止。`

---

## 11. Figures and tables plan

| ID | 位置 | 形式 | 教学职责 | Claim / Evidence binding | 禁止表达 |
|---|---|---|---|---|---|
| F11-01 | Section 1 | lost-response timeline | 建立effect已发生但result unknown的问题空间 | C04 / E02, E11 | response lost = effect absent |
| T11-01 | Section 2 | terminology table | 分开Timeout / Cancellation / Retry / Resume / Replay / Recovery | C01, C06 / E01, E03, E05, E09 | universal product taxonomy |
| F11-02 | Section 2 | recovery classification tree | 展示classify后才选择有限动作 | C01, C03-C07 | Recovery = Retry |
| T11-02 | Section 3 | checkpoint recovery-question table | 让字段服务恢复问题，而不是堆schema | C02, C08, C09 / E04, E12 | production-sufficient schema |
| F11-03 | Section 3 | State vs Checkpoint | 区分current control position与durable continuation / in-flight evidence | C02, C09 | Checkpoint / Memory物理互斥 |
| T11-03 | Section 4 | retry decision table | 先判effect semantics，再判failure与budget | C03, C04 / E02, E10, E11 | key = exactly-once |
| F11-04 | Section 4 | LR-04 / LR-05 dual lane | 对照same-identity reconcile与blind duplicate | C04 / E11 | 两条路径都算success |
| F11-05 | Section 5 | cancellation timeline | 分开request、observed、terminal、resume | C01, C05 / E01, E09 | cancellation = rollback |
| F11-06 | Section 6 | recovery decision tree | 展示Resume / Reconcile / Compensate / Ask / Stop | C04-C08 | Lab observed compensation |
| T11-05 | Section 7 | partial-result contract | 分开known / unknown / unverified / next safe action | C07 / E12, E13 | industry-standard schema |
| T11-06 | Section 8 | LR-01—LR-08 matrix | 同时呈现正例、失败与拒绝 | C01-C08 / E07-E13 | 8 / 8 = 8 successes |
| F11-08 | Section 8 | Expected / Observed / Interpretation layers | 防止Design或verifier冒充Observation / Claim owner | C01-C08 / E07, E08 | expected = observed |

图表实施规则：优先使用Markdown table与简单text / Mermaid示意；Draft不需要生成新的位图。所有Lab图题都必须带`lab04-fixture-v1 / current Windows + .NET / fixed scope`说明。

---

## 12. Learning Check

### Check 1｜Lost response 与 Retry资格

- **题目**：Tool可能已经创建远端资源，但Runtime在读取响应前超时。只因为failure被标为transient且budget还有一次，能否直接Retry？
- **期望能力**：先把effect写成unknown；寻找stable identity、lookup / idempotent contract或compensation；没有安全路径时Ask / Stop。
- **Claims**：C03、C04、C07。

### Check 2｜Cancellation来源与最后安全状态

- **题目**：用户已经发出取消，为什么Runtime仍要记录listener observed位置、in-flight action和partial result？
- **期望能力**：说明request不等于work stopped / rollback；这些字段决定下一次是否存在安全Resume candidate。
- **Claims**：C01、C05、C07。

### Check 3｜Checkpoint完整性

- **题目**：Checkpoint有`state=REGISTERING_FINDING`，却没有action ID、intent digest或idempotency key。能否根据current State猜测“尚未执行”并继续？
- **期望能力**：不能；in-flight uncertainty无法解析，应在新副作用前fail closed，保留unknown并给出`NONE / ASK`。
- **Claims**：C02、C08。

### Check 4｜Resume 与 Replay

- **题目**：为什么LR-04 Resume会再次进入store调用边界，却仍然可以是安全候选？这是否证明exactly-once？
- **期望能力**：same run / action / intent / key用于query / `CreateOrGetExisting`，final fake-store effect仍为1；这只证明fixed-store behavior，不证明exactly-once或distributed safety。
- **Claims**：C04、C06。

### Check 5｜读懂negative case

- **题目**：Lab报告`8 / 8 accepted`，LR-05为什么仍以`FAILED`结束？
- **期望能力**：acceptance表示case符合冻结判据；LR-05必须真实产生duplicate并失败，才能作为blind retry的negative evidence。
- **Claims**：C04、C08。

### Check 6｜Partial Result

- **题目**：Retry Budget耗尽后，输出只写“任务未完成，请稍后再试”有什么证据缺口？
- **期望能力**：分别列known、unknown、unverified和next safe action，并保留provenance；不能把没有验证的Goal条件省略。
- **Claims**：C07。

### Check 7｜Checkpoint 与 Memory边界

- **题目**：一个产品用同一个checkpointer保存thread state并支持fault tolerance，是否说明Checkpoint与Memory完全相同，或必须拆成两套存储？
- **期望能力**：两种结论都过强；实现可重叠，本篇只按是否回答control position、continuation与in-flight uncertainty区分证明职责。
- **Claims**：C09。

### Check 8｜是否值得Checkpoint

- **题目**：一个短、确定、无外部副作用且重跑成本极低的Compile Case，是否一定要保存本文完整candidate schema？
- **期望能力**：不一定。先判断重跑成本、side effect、人工等待与恢复价值；本篇candidate由long-running fixed cases验证，不是所有任务的强制schema。
- **Claims**：C02、C08。

---

## 13. Claim-to-section coverage

| Claim | Final status inherited from Evidence | Primary section | Supporting sections | Coverage duty | Result |
|---|---|---|---|---|---|
| `11-C01` | CONFIRMED / FIXTURE-SCOPED | 2 | 1, 5, 8 | 六类control facts分离；LR-02 / 03 / 08保留origin、decision与terminal | COVERED |
| `11-C02` | CONFIRMED / PROPOSAL-CONFORMANCE | 3 | 6, 7, 8 | checkpoint candidate回答identity / completed / remaining / in-flight / budget / continuation | COVERED |
| `11-C03` | CONFIRMED / FIXED-STORE-SCOPED | 4 | 8, 9 | retry eligibility先于budget；LR-03 / 04 / 07 | COVERED |
| `11-C04` | CONFIRMED / FIXED-STORE-SCOPED | 4 | 1, 6, 7, 8 | lost-response unknown；LR-04 reconcile与LR-05 duplicate对照 | COVERED |
| `11-C05` | CONFIRMED / FIXTURE-SCOPED | 5 | 1, 6, 8 | cancellation request != stopped / rollback；LR-02 safe-boundary resume | COVERED |
| `11-C06` | CONFIRMED / COURSE-RUNTIME-SCOPED | 6 | 2, 5, 8 | Resume从boundary继续、可能Replay；fresh-process proof | COVERED |
| `11-C07` | CONFIRMED / COURSE-SCHEMA-CONFORMANCE | 7 | 4-6, 8 | known / unknown / unverified / next safe action与provenance | COVERED |
| `11-C08` | CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED | 3, 8 | 6, 9 | LR-06 pre-effect refuse；12 fresh phases；105 normalized files equality | COVERED |
| `11-C09` | CONFIRMED / PRODUCT-DOC-SCOPED | 3 | 10 | Checkpoint / Memory实现可重叠、证明职责不同；Article 12 stop line | COVERED |

Coverage result：`9 / 9 COVERED`；Core `BLOCKED = 0`。没有新增Claim ID，没有把fixture、fixed-store、proposal、course-runtime、course-schema、deterministic-fixture或product-doc scope升级。

---

## 14. Job Competency mapping

| Competency | 文章中的可观察产出 | 对应章节 |
|---|---|---|
| Runtime / lifecycle建模 | 分开Timeout、Cancellation、Retry、Resume、Replay、Recovery与terminal | 1, 2, 5, 6 |
| 可靠性与failure semantics | 对lost response、unknown side effect、budget exhaustion与invalid checkpoint作fail-closed决策 | 3, 4, 6, 7 |
| 状态与持久化边界 | 用恢复问题设计Checkpoint candidate，并区分State、Trace、Checkpoint与Memory职责 | 3, 10 |
| 副作用与幂等工程 | 依据identity、effect semantics、lookup / replay / compensation选择动作，不把key写成exactly-once | 4, 6 |
| 可观测性与证据纪律 | 保留origin、attempt、effect count、partial-result provenance与negative terminal | 5, 7, 8 |
| 实验与故障注入设计 | 解释Expected / Observed分离、LR-01—08 fault matrix、fresh-process与normalized compare | 8 |
| 技术决策 / Tech Lead判断 | 用decision table和review heuristics决定Retry、Refuse、Ask或Stop，并说明证明上限 | 4, 9, 10 |

表达要求：能力通过设计判断、反例和验证边界隐式呈现；正文不出现求职自夸或职位宣言。

---

## 15. Source and link plan

### External primary / official sources

| Source | 用途 | 放置位置 | Scope label |
|---|---|---|---|
| Microsoft Learn：Cancellation in Managed Threads | requester / listener cooperative cancellation | Sections 2, 5 | current hosted docs；不证明rollback / resume |
| RFC 9110 §9.2.2 | idempotent intended effect与lost-response retry边界 | Section 4 | HTTP semantics only |
| LangGraph Functional API | checkpoint boundary replay、task result persistence、side-effect re-execution risk | Sections 2, 4, 6 | current Python docs；package / commit unpinned |
| LangGraph Persistence | thread / checkpoint / pending writes / replay；checkpointer与Store术语反例 | Sections 3, 6, 10 | current hosted docs；product-scoped |
| AWS Step Functions Redrive | successful step preservation、failed Task reschedule、product-specific recovery | Sections 2, 6 | current Standard Workflow docs |

### Internal published dependency links

- Article 06：链接Tool Runtime的timeout / caller origin、single-process idempotency seam与unknown side-effect gap；不重复path / policy / result教程。
- Article 08：链接committed Step、State / terminal和`stopped != succeeded`；说明其Lab未实现recovery。
- Article 09：链接Plan candidate、Verified State与Authorization边界；不重讲Planning patterns。
- Article 10：链接legal transition、authoritative State与`State != Checkpoint` bridge；不重讲Workflow taxonomy。

### Lab links

- Lab 04 README：Frozen Design、Expected Observable、Acceptance Criteria、Observations、Evidence Merge、Limitations。
- Execution：`observations/execution-log.md`、`verification-summary.json`、`environment.md`、`dotnet-info.txt`。
- Fresh-process proof：`process-evidence-run-a.json`、`process-evidence-run-b.json`。
- Per-case evidence：run-a / run-b的checkpoint、trace、partial-result、case-result、fake-store view与manifest。
- Source / verifier / fixture：`src/LongRunningAgentLab/`、`tests/LongRunningAgentLab.Specs/`、`fixtures/cases.json`。

Link implementation note：Published Content阶段才写Hugo `relref`与GitHub blob链接；当前Outline只固定source / artifact职责，不新增外部事实或未验证链接。

---

## 16. Length budget

目标正文：`5,800—7,200`中文字（不含frontmatter、链接注释与图表caption），符合M / Standard Core Lesson。

| Section | Budget | 压缩策略 |
|---|---:|---|
| Opening + Section 1 | 500—650 | 用lost-response场景立问题，不复述Article 06—10 |
| Section 2 | 550—700 | 术语表承担定义，不逐产品展开 |
| Section 3 | 800—1,000 | 以恢复问题组织schema，不堆字段说明 |
| Section 4 | 850—1,050 | LR-04 / 05是中心对照；RFC只作窄锚点 |
| Section 5 | 500—650 | LR-02 / 08对照，避免扩写cancellation API |
| Section 6 | 650—850 | recovery protocol与decision tree为主 |
| Section 7 | 450—600 | partial-result表与两个case足够 |
| Section 8 | 1,050—1,300 | 八case矩阵、执行事实与failure ledger，不复制raw JSON |
| Section 9 | 350—500 | 反模式一一绑定已有case |
| Section 10 + closing | 300—450 | 完整保留证据ceiling与Article 12 stop line |

若超长，优先压缩产品例子、字段逐项解释和aggregate count说明；不删除LR-05 / LR-06 negative evidence、Expected / Observed边界、partial result、scope ceiling或Article 12 stop line。

---

## 17. New Core Facts Audit

| Proposed outline statement | Basis | Audit result |
|---|---|---|
| Timeout / Cancellation / Retry / Resume / Replay / Recovery分离 | C01 / E01, E03, E05, E09 | existing confirmed scoped fact |
| Checkpoint candidate回答identity、completed、remaining、in-flight、budget、continuation | C02 / E04, E07, E12 | existing Proposal conformance；标签保留 |
| Retry eligibility先于Budget | C03 / E02, E10 | existing fixed-store-scoped fact |
| lost response形成unknown；LR-04 / 05对照 | C04 / E02, E11 | existing fixed-store-scoped fact |
| cancellation request不等于work stopped / rollback | C05 / E01, E09 | existing fixture-scoped fact |
| Resume从boundary继续且可能Replay | C06 / E03, E05, E09, E11 | product facts + course-runtime observation |
| partial-result四字段与provenance | C07 / E12, E13 | existing course-schema conformance |
| invalid checkpoint fail closed + run A/B equality | C08 / E12, E13 | existing deterministic-fixture-scoped fact |
| Checkpoint / Memory实现可重叠、职责不同 | C09 / E04 | existing product-doc-scoped counter-evidence |
| compensation作为Recovery候选 | canonical mental model + research decision route | existing course proposal；明确`NOT OBSERVED IN LAB 04` |
| bad-runtime review heuristics | C01-C08直接教学转写 | no new core fact；不得写成行业穷举 |

Audit result：`NO NEW CORE FACT REQUIRED`。不存在需要退回Research的新核心Claim；Draft不得扩写production、distributed、OS-crash、real-network、Provider、concurrent、cross-platform、exactly-once、Memory / Context质量或模型确定性事实。

---

## 18. Outline Gate checklist

- [x] Article type明确为原理 / 长任务恢复机制篇，Mode=`LAB_ARTICLE`。
- [x] 最短thesis、Reader Change与Teaching Spine已定义。
- [x] 正文结构遵循`问题空间 -> 抽象模型 -> 具体机制 -> 工程判断 -> 验证边界`。
- [x] 每个正文section都有Reader Question、Core Claim、Claim / Evidence binding、Teaching Duty、Boundary与Bridge。
- [x] Checkpoint boundary、Retry Budget、Cancellation / Timeout source、Resume / Recovery decision、idempotency / compensation、in-flight uncertainty、partial result和stop conditions全部覆盖。
- [x] Lab 04明确保持`Expected != Observed`，LR-01—LR-08全部进入case matrix。
- [x] LR-05 unsafe duplicate与LR-06 missing-in-flight fail-closed作为required negative evidence保留。
- [x] fresh-process、run A/B 105 normalized files equality与deterministic-fixture scope同时保留。
- [x] Claim-to-section coverage为`9 / 9`，Core `BLOCKED = 0`。
- [x] fixture、fixed-store、proposal、course-runtime、course-schema、deterministic-fixture与product-doc ceiling逐项保留。
- [x] Learning Check与Job Competency mapping已定义，不露骨自我推销。
- [x] Explicit non-scope覆盖production、distributed、OS crash、concurrency、real network / Provider、cross-platform与exactly-once。
- [x] Article 12 Context / Memory stop line显式存在；没有扩写跨run Memory、context selection / reconstruction / quality或knowledge retention。
- [x] Figures / Tables、source / link plan、length budget和New Core Facts Audit已定义。
- [x] 没有待补占位，没有创建`draft.md`，没有修改Research / Evidence / Lab / Published Content / global state，没有启动Article 12。

Gate candidate：`PASS_RECOMMENDED`。最终Outline Gate结论由Master验证；Author只建议下一Gate为`AUTHOR_DRAFT`，不自批`FINAL`。
