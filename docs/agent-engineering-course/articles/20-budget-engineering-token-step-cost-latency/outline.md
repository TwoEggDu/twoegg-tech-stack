# Article 20 Outline｜Budget Engineering：Token、Step、Cost 与 Latency

## Outline contract

- Article Type: PRINCIPLE
- Course Weight: M / Standard Core Lesson
- Teaching Spine: Article 19 action-authority bridge -> problem space -> four-dimension comparison -> provider-native measurement boundaries -> BudgetVector -> enforcement lifecycle -> exhaustion routing -> uncertainty/audit record -> BuildPilot design-only walk-through -> counterexamples -> ownership/verification boundary
- Core Claim Scope: `20-C01`—`20-C09` only；不新增核心 Claim / Evidence Card
- Evidence Posture: `1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`
- Numeric Discipline: 不写固定模型 Context Window、价格、service-tier 数值或经验阈值；需要具体值的位置使用 policy/provider identity、dated `price_snapshot_ref`、range 或 `UNKNOWN`

> 如果这篇只记一句话：`Budget 不是运行结束后的 usage 报表，而是一组在动作获权之后、资源消耗之前持续执行的多维准入与停止合同。`

## Opening bridge｜Article 19 已回答“能不能做”，为什么还要问“资源允不允许现在做”

- Reader Question: 一个 BuildPilot action candidate 已通过 Article 19 的 principal/action/resource/policy/approval/enforcement 检查，是否就应立刻启动？
- Claim / Evidence posture: `20-C01 PARTIAL`, `20-C06 PROPOSAL`, `20-C07 PROPOSAL`。
- Evidence Card locators:
  - `20-E10`：Article 20 `research.md` 的 “Abstract model” / “Enforcement matrix” / “Exhaustion routes”。
  - `20-E11`：canonical Part IV 20—22 行、Article 20 Frozen Boundaries、Published Article 19 ownership seam。
- Teaching Role:
  - 接住 Article 19 的 action-authority chain：Budget check 只约束已经获权且 otherwise eligible 的动作。
  - 用一个 `READ_ONLY_STARTUP_INVESTIGATION / DESIGN ONLY` 候选说明：authority inputs 已闭合，仍需检查 Token、Step、Cost、Latency 是否足以 admission；反过来，budget remains 也不能替动作签发 authority。
- Planned contrast:

  ```text
  action authority
    = 这次动作在当前 policy / approval / enforcement 下有资格执行

  budget admission
    = 这次动作在声明的 Token / Step / Cost / Latency 合同下有资源资格启动

  authority PASS != budget PASS
  budget remains != action authorized
  ```

- Wording Boundary: 不声称任何真实 Approval、Budget service、reservation、provider call 或 BuildPilot Runtime 已存在；不把 Budget 写成安全 gate、质量 gate 或动作权限来源。
- Section Takeaway: **Article 19 决定动作是否有权进入执行边界；Article 20 决定已获权动作是否还有资源资格进入下一执行点。**

## Part A｜问题空间：结束后统计 usage，为什么还不叫 Budget Engineering

### 1. 为什么结束后 usage 不是 Budget：四类资源不存在稳定换算

- Reader Question: 为什么不能只设一个 Token cap，或用一个金额/timeout 统一代表所有预算？
- Claim / Evidence posture: `20-C01 PARTIAL`。
- Evidence Card locators:
  - `20-E01`：OpenAI Responses `max_output_tokens`、`truncation`、response `usage`、`incomplete_details.reason`。
  - `20-E02`：OpenAI `POST /responses/input_tokens` preflight count。
  - `20-E04`：OpenAI Agents SDK “The agent loop” / “Exceptions” 中 `max_turns`、`MaxTurnsExceeded`。
  - `20-E05`：LangGraph Graph API “Recursion limit” / “Accessing and handling the recursion counter”。
  - `20-E06`：gRPC “Deadlines” 的 Overview / Client / Server / Propagation。
  - `20-E07`：OpenAI `GET /organization/costs` 的 amount/currency/time bucket/attribution。
- Planned four-dimension comparison:

  | Dimension | Unit contract | Estimate / capacity input | Actual evidence | Typical enforcement point | 不能推出 |
  |---|---|---|---|---|---|
  | Token | provider/model/request-surface-specific categories | input count/estimate + output reserve | provider response usage或后续 usage record | model call 前；response 后 reconciliation | Context Window fit = Run budget fit；跨 Provider 字段相同 |
  | Step | course committed Step 或 named runtime 的 exact unit | limit + counting rule | committed counter / product terminal | 新 Step admission 前；checkpoint/resume | cap reached = task complete；turn = node = super-step |
  | Cost | currency + pricing identity + accounting basis | usage estimate × dated price snapshot + known non-token items | source-qualified cost/billing record | Run admission；chargeable operation 前；reconcile | estimate/reservation = billed actual |
  | Latency | monotonic duration + wall-clock deadline identity | end-to-end deadline + child allowance | application-visible timestamps / source-qualified metrics | admission；queue dequeue；child call 前；resume | one timeout = end-to-end budget；hidden queue time known |

- Teaching Judgment: 四维 taxonomy 由不同现有合同支撑，但统一编排是课程模型；保持 `PARTIAL / COURSE TAXONOMY`，不称行业统一 Budget 标准。
- Section Takeaway: **Token、Step、Cost、Latency 会互相影响，却没有任何一维能安全替代另外三维。**

#### 观测、限制和决定必须分开

- Reader Question: 为什么 response usage、一个计数器或最终账单不能自动成为执行中的 Budget policy？
- Claim / Evidence posture: `20-C01 PARTIAL`, `20-C06 PROPOSAL`。
- Evidence Card locators: `20-E01`, `20-E04`—`20-E10`，逐项只使用各 Card 的 `Supported conclusion / Does not prove`。
- Planned responsibility split:
  - Measurement / receipt 回答“来源目前报告了什么”。
  - Budget contract 回答“哪个 scope 的 limit、unit、hard/soft policy 和 uncertainty 是什么”。
  - Enforcement decision 回答“在这个执行点 admit、reject、degrade、ask 还是 partial”。
  - Reconciliation 回答“estimate/reservation 与 actual/unknown 怎样闭合”。
- Anti-shortcut: `usage observed -> next action allowed` 与 `budget remains -> retry allowed` 均为非法跳跃；Article 11 的 retry/effect eligibility 和 Article 19 的 action authority 先独立成立。
- Section Takeaway: **Usage 是证据输入，Budget 是控制合同；只有执行点上的决定才能约束下一项工作。**

## Part B｜抽象模型：把四维 Budget 写成向量，不压成一个 `remaining`

### 2. Token 三分账：Context Window、preflight count / response usage、Run Token Budget

- Reader Question: “请求放得进窗口”“计数接口返回 N”“本次 Run 还有额度”分别在说什么？
- Claim / Evidence posture: `20-C02 CONFIRMED`。
- Evidence Card locators:
  - `20-E01`：OpenAI Responses capacity controls、usage 与 incomplete terminal 分离。
  - `20-E02`：OpenAI input counting 是 response creation 前的独立 operation。
  - `20-E03`：Anthropic Token Counting “How to count message tokens” note；Messages `max_tokens`、response `usage`、`stop_reason`。
- Planned distinction table:

  | Object | 回答的问题 | Identity / evidence | 不能替代 |
  |---|---|---|---|
  | Context Window | 目标 model/request contract 能容纳什么 | provider + model + request surface + current contract | Run-wide policy、质量或价格 |
  | Preflight count / estimate | 当前可见 request 预计占多少 | estimator + provider/model + request revision + observed_at | 最终 usage、输出、cost actual |
  | Response usage receipt | Provider 对该 response 报告了哪些 native categories | provider receipt identity + native fields | 跨 Provider统一字段、完整账单 |
  | Run Token Budget | application 对 Run/scope 允许消耗多少 | policy id/version + limit/reserve/remaining | Context fit、质量或 action authority |

- Article 01 / 12 Boundary:
  - Article 01 owns Token 与 Context Window 基础；Article 12 owns Context Select/Order/Scope/Fit Budget 与 Context Receipt。
  - 本篇只消费这些对象，建立 Run-level admission、reserve、actual 与 remaining；不重讲 tokenization、Context Assembly 或污染诊断。
- Provider Boundary: OpenAI 与 Anthropic receipt 保持 source-native；不把相似名称强制映射成相同 token categories。
- Numeric Boundary: 不出现任何固定窗口值；未来如需示例，必须重新绑定 exact model contract 与访问日期。
- Section Takeaway: **能放进窗口是 capacity 判断，count/usage 是 provider-scoped measurement，Run Token Budget 才是应用的资源 policy。**

### 3. Step Budget：先冻结计数单位，再讨论上限

- Reader Question: “最多 10 Step”为什么仍可能是一句不可审计的话？第一轮、retry、tool fan-out、并行 node 到底怎么算？
- Claim / Evidence posture: `20-C03 PARTIAL`。
- Evidence Card locators:
  - `20-E04`：OpenAI Agents SDK 将 turn 定义为一次 AI invocation including tool calls；`MaxTurnsExceeded` 是产品限定 terminal。
  - `20-E05`：LangGraph `recursion_limit` 计 graph super-steps；parallel nodes 可共享一个 super-step。
  - `20-E10`：Published Article 10 的 Step seam + Article 20 counting-rule synthesis。
- Required counting-rule checklist:
  1. `unit_name`：课程 committed Step、AI invocation、graph super-step 或其他明确 unit。
  2. `scope`：Run / Stage / branch / retry group。
  3. pre-Step admission 只按 `step_attempt_id` 创建一个 in-flight reservation；first successful committed Step 才计入 `used`。
  4. retry 是否再次计数，以及从哪个 persisted counter 继续。
  5. tool fan-out / parallel node 是各自计数还是由父 unit 计数。
  6. `remaining_to_admit = limit - used - in_flight_reserved`；successful Step commit 将 reservation 替换为 `used + 1`，只执行一次；abort-before-commit release；三者在 checkpoint/resume 后保持同一 identity。
- Article 10 / 11 Boundary: Article 10 owns committed Step 与 legal transition；Article 11 owns checkpoint、retry eligibility、effect uncertainty 与 recovery。Article 20 只规定“被该 counting rule 接纳的单位如何扣减”，不重新定义 State/Retry。
- Terminal Boundary: cap reached 只能写 `BUDGET_EXHAUSTED / INCOMPLETE` 或本地等价 budget terminal；不等于 success、bad quality、unsafe effect 或统一 Failure Taxonomy。
- Section Takeaway: **Step 上限只有和 counting rule、scope、resume persistence 绑定后才可审计；到顶只证明不再接纳新 Step。**

### 4. Cost 单值分账：estimate、reservation、incurred-pending、actual 各自证明什么

- Reader Question: 预估用量乘一个价格，为什么不能直接记为实际成本？并发 Run 为什么还需要 reservation？
- Claim / Evidence posture: `20-C04 PARTIAL`；其中 reservation 明确为课程设计。
- Evidence Card locators:
  - `20-E07`：OpenAI Organization Costs `GET /organization/costs`，独立的历史 cost surface。
  - `20-E08`：Anthropic “Usage and Cost API” overview / usage tracking / cost reconciliation / availability limits。
  - `20-E09`：FOCUS 1.4 §3.1.7 Billed Cost、§3.1.35 Effective Cost、§3.1.40 List Cost。
  - `20-E10`：estimate/reservation/actual 课程 synthesis。
- Planned accounting-state table:

  | State | Minimum identity | 能证明 | 不能证明 |
  |---|---|---|---|
  | Estimate | estimated usage/range + assumptions + currency + dated `price_snapshot_ref` + model/service tier | 在明确假设下的预估 | provider cost record、invoice、精确 actual |
  | Reservation | internal reservation id + scope + amount/range + create/release/commit state | 当前 policy 暂扣了并发可用额度 | Provider 已计费、FOCUS 标准字段、atomic/race-free |
  | Incurred pending | `charge_id` + measured usage/known charge range + measurement ref + completeness | 已发生但尚未由账务来源结算的 conservative outstanding | source-qualified actual、invoice finality |
  | Actual | source-qualified cost/billing record + freshness + accounting basis | 该来源在该时点报告的 monetary record | 所有 line item 可归因于一个 Run、即时 invoice finality |

- Single-accounting rule: 同一 `charge_id` 只能处于 reservation、incurred-pending、source-qualified actual 或 released 中一个 bucket；transition 是 replace，不是把新 bucket 再相加。`remaining = limit - settled_actual - conservative(outstanding)`，outstanding 只取该 `charge_id` 当前 reservation 或 incurred-pending；hard admission使用upper bound，缺finite bound则`UNKNOWN/STOP`。response/result 后 reservation 转为 incurred-pending，释放仅限未使用 delta；归因/完整性未知时继续保守占用，不能提前 release。source-qualified actual 到达后再替换 pending。
- Unknown rule: usage、price、currency、service tier、discount、cache 或 non-token line item 缺失时，保留 range / `UNKNOWN`；不得合成精确 actual，也不得把未知 pending 当作零。
- Concurrency Boundary: reservation 的教学职责是解释“防止并发 oversubscription 的内部 hold”；不声明原子性、race freedom 或 provider billing support。
- Numeric Boundary: 不写任何单价、具体金额或固定价格有效期。
- Section Takeaway: **Estimate 是有假设的计算，reservation 是内部资源占位，actual 是有来源和新鲜度的账务事实；三者不能互相改名。**

### 5. Latency 分层：deadline、timeout、queue/service time 与 critical path

- Reader Question: 为什么给每个 Tool 一个 timeout，仍不能证明端到端 latency 受控？并行分支为什么不能把 duration 相加？
- Claim / Evidence posture: `20-C05 PARTIAL`。
- Evidence Card locators:
  - `20-E06`：gRPC “Deadlines” Overview / Client / Server / Deadline Propagation；deadline 是 point in time，timeout 是 duration，传播时扣除 elapsed。
  - `20-E10`：application-visible phase ledger 与 critical-path 课程设计。
- Planned latency ledger:

  ```text
  admitted_at
    -> queued_at -> dequeued_at
    -> started_at
       -> child operation spans / dependency edges
    -> completed_at
  ```

- Required distinctions:
  - Deadline：caller 不愿再等待的 absolute point；resume / propagation 重新计算 remaining。
  - Timeout：某个 child operation 的最大 duration；不得超过 remaining allowance，但不覆盖之前已耗时。
  - Queue time：只有 application 可见的 queued/dequeued 两端时间戳齐全才计算。
  - Service time：只有 owned boundary 的 start/complete 齐全才计算；provider-internal queueing 不可见则 `UNKNOWN`。
  - Critical path：只对 application 已知 execution DAG 的最长 dependency path 计算；不是所有 span 求和，也不是 hidden provider scheduling 事实。
- Clock Contract:
  - 每个 monotonic stamp 冻结 `clock_domain_id / host_id / boot_id / checkpoint_segment_id`；只有 runtime 证明 compatibility 的 same-domain stamps 才相减，process boundary 必须重验 identity。
  - 跨不兼容 process/host/reboot boundary 时，不比较 monotonic origin；使用 checkpoint 持久化的 absolute deadline、当前可信 wall clock 与 `uncertainty_bound/policy`，按 `safe_remaining = max(0, absolute_deadline - current_wall_clock - uncertainty_bound)` 保守计算。
  - clock trust 或 uncertainty 无法界定时，`remaining=UNKNOWN`；hard latency policy 必须 `BLOCKED/STOP`，不能重置 deadline。
  - phase receipt 只作 attribution，不从已经扣过的 end-to-end remaining 再扣一次；跨域 gap 保持 `UNKNOWN`。
- gRPC Boundary: gRPC 只支撑 deadline/timeout 与传播时扣除 elapsed 的窄语义；上述持久化 Agent resume clock contract 是课程 Proposal，不归因于 gRPC。
- Section Takeaway: **timeout 管一个子操作，deadline 管调用方的端到端等待边界；queue/service/critical path 只能从可见且有依赖关系的时间证据计算。**

### 6. `BudgetVector`：统一控制面，不统一 Provider 语义

- Reader Question: 怎样把四维状态放进一个 envelope，又不假装它们能换算成同一种单位？
- Claim / Evidence posture: `20-C01 PARTIAL`, `20-C02 CONFIRMED`, `20-C03/C04/C05 PARTIAL`。
- Evidence Card locators: `20-E01`—`20-E10`；四维 shape 直接定位 `20-E10` 的 “Abstract model”。
- Proposed model:

  ```text
  BudgetVector = {
    token:   {limit, estimated, reserved, actual, remaining, unit_contract},
    step:    {limit, in_flight_reserved, used, remaining_to_admit, counting_rule},
    cost:    {limit, estimated, reserved, incurred_pending, actual, remaining, currency, price_snapshot_ref},
    latency: {deadline, elapsed, remaining, clock_domain, host_id, boot_id, checkpoint_segment_id, phase_receipts}
  }
  ```

- Mandatory Label: `COURSE PROPOSAL / PROVIDER-NATIVE RECEIPTS / NOT A UNIVERSAL PROVIDER SCHEMA / NOT IMPLEMENTED`。
- Adapter rule: provider receipt 原样保存，course-side field 通过显式 adapter/ref 指向；不能静默丢掉 native categories、terminal reason 或 uncertainty。
- Per-dimension invariant: 一个 `consumption_id` 同时只在一个 accounting bucket；`remaining_to_admit = limit - settled - conservative(outstanding)`。Token receipt 替换 reservation；Step 在 successful commit 唯一增加 `used`；Cost 以同一 `charge_id` 逐级 replace；Latency end-to-end delta 只扣一次。
- Section Takeaway: **统一的是 admission / enforcement 的控制接口，不是 Token、turn、cost record 或 timeout 的 Provider 原生语义。**

## Part C｜控制链：Budget 必须在执行前后持续生效

### 7. 从 Declare 到 Reconcile 的 enforcement lifecycle

- Reader Question: Budget 应该在哪些时点 estimate、admit、reserve、revalidate、扣减、释放和 reconcile？
- Claim / Evidence posture: `20-C06 PROPOSAL`。
- Evidence Card locators:
  - `20-E10`：Research “Admission, reservation, enforcement and reconciliation” + “Enforcement matrix”。
  - `20-E11`：Article 10/11/19 ownership seams 与 BuildPilot ceiling。
- Proposed lifecycle:

  ```text
  DECLARE
    -> ESTIMATE
    -> ADMIT | REJECT | REQUEST_APPROVAL
    -> RESERVE by consumption_id
    -> REVALIDATE before each Step / chargeable call
    -> REPLACE reservation with committed usage / incurred_pending, or RELEASE unused/aborted amount
    -> REPLACE incurred_pending with source-qualified actual when available
    -> RECONCILE
    -> COMPLETE | EXHAUSTED | CANCELLED | PARTIAL
  ```

- Planned enforcement matrix:

  | Enforcement point | Token | Step | Cost | Latency | Required decision evidence |
  |---|---|---|---|---|---|
  | Run admission | expected input/output range | limit + counting rule | estimate range + currency + dated snapshot or `UNKNOWN` | end-to-end deadline | policy version、uncertainty、allowed routes |
  | Queue dequeue / resume | request changed则 recount | persisted used/in-flight reservation | outstanding charge identity/freshness | same-domain monotonic delta；否则 absolute deadline + bounded uncertainty；不可界定则 UNKNOWN/STOP | checkpoint + clock-domain/host/boot/segment identity + revalidation |
  | Before Step commit | reserve planned call by consumption id | reserve one in-flight unit；不增加 `used` | reserve predicted incremental range | remaining time | current State + legal transition |
  | Before provider/tool call | native preflight if available | declared tool/turn rule | reserve chargeable maximum/range | child timeout within remaining deadline | Article 19 authority + Article 11 retry eligibility |
  | After response/result | replace reservation with native usage receipt | successful Step commit 时 replace reservation 并唯一 `used + 1`；abort则release | reservation -> measured/incurred-pending；只release unused delta；actual可 pending | comparable phase timestamps | response/result + consumption identity |
  | Terminal/reconcile | settled + outstanding/unknown | used + in-flight/remaining-to-admit | pending -> source-qualified actual；未知 outstanding 保守保留 | elapsed + known same-domain receipts + unknown gaps | completion/exhaustion route |

- Ordering Invariant: `authority / legal transition / retry eligibility PASS` 先于 Budget consume；Budget PASS 不能反向批准动作。
- Single-accounting Invariant: 一个 consumption identity 只占一个 bucket；transition 使用 replace，不能同时把 reservation、pending 与 actual 相加。Step `used` 只在 successful commit 增加一次；Cost remaining 扣除 settled 与 conservative outstanding。
- Resume Invariant: checkpoint 中的 used/in-flight reservation、cost outstanding、absolute deadline 与 clock-domain/host/boot/segment identity 必须加载并重验，不能因为新进程而重置；不兼容 clock domain 无法按 policy 界定时 fail closed。
- Proposal Boundary: 生命周期不声称标准化、原子 reservation、并发正确性或已经运行。
- Section Takeaway: **真正的 Budget control 不是末尾加总，而是在每个会新增消耗的边界前重验，在 receipt 返回后对账。**

### 8. Exhaustion routing：Stop、Degrade、Request Approval、Partial Result

- Reader Question: 某一维耗尽时，系统什么时候必须停，什么时候可以降级、请求追加额度或返回部分结果？
- Claim / Evidence posture: `20-C07 PROPOSAL`。
- Evidence Card locators:
  - `20-E01`：OpenAI incomplete/max-output product-scoped terminal 只作有限例子。
  - `20-E04`：OpenAI Agents SDK `MaxTurnsExceeded` product-scoped terminal。
  - `20-E06`：gRPC deadline expiry / cooperative stop boundary。
  - `20-E10`：hard/soft route synthesis。
  - `20-E11`：Article 19 authority、Article 22 quality ownership。
- Planned route table:

  | Route | Eligible when | Must preserve | 不能越过 |
  |---|---|---|---|
  | STOP | hard limit would be exceeded；critical facts unknown under fail-closed policy | exhaustion dimension、decision point、known/unknown | authority、effect uncertainty、required Goal invariants |
  | DEGRADE | policy explicitly marks work optional and cheaper path preserves Goal/Evidence/authority invariants | omitted work、changed assumptions、remaining uncertainty | required context/evidence、hard policy、质量保证 |
  | REQUEST_APPROVAL | policy allows a bounded budget increase request | frozen request、requested delta、reason、expiry/policy identity | Article 19 hard deny 或 otherwise forbidden action |
  | PARTIAL_RESULT | completed work can be honestly separated from unknown/unverified work | known、unknown、unverified、budget reason、next safe action | 把 incomplete 包装成 success |

- Dimension rule: 每维声明 `hard/soft` 与 allowed routes；不假设所有维度耗尽都走同一路。
- Quality Boundary: exhaustion 是 resource-policy terminal；它不证明结果错误或质量退化。降级是否维持质量属于 Article 22 Eval/Regression，不在本篇作结论。
- Failure-taxonomy Boundary: 本篇只保存 budget-local reason 与 route；跨层 failure classification 由 Article 21 正式建立。
- Section Takeaway: **耗尽路由的职责是诚实结束或收窄工作，不是把资源不足改名成成功，也不是借追加预算绕过权限。**

### 9. Budget audit record：把 estimate、reservation、incurred-pending、actual、remaining、decision 与 uncertainty 绑在一起

- Reader Question: 最小记录要保存哪些 identity 和未知，才能解释“为什么当时允许/拒绝下一项工作”？
- Claim / Evidence posture: `20-C08 PROPOSAL`。
- Evidence Card locators:
  - `20-E07`：OpenAI historical cost record identity/freshness surface。
  - `20-E08`：Anthropic historical usage/cost reconciliation boundary。
  - `20-E09`：FOCUS 1.4 cost-basis distinctions。
  - `20-E10`：Research “Minimal Budget audit record”。
  - `20-E11`：Article 21/22 ownership seam。
- Proposed minimum record:

  ```text
  budget_record:
    identity: {budget_id, run_id, scope, parent_budget_id, policy_id, policy_version}
    contract: {dimensions, unit_contracts, hard_or_soft, effective_at, expires_at}
    estimate: {value_or_range, source, assumptions, observed_at}
    accounting: {dimension, consumption_id, state, basis, transition_from, transitioned_at}
    reservation: {reservation_id, amount_or_range, created_at, released_or_replaced_at}
    incurred_pending: {value_or_range, measurement_ref, completeness, observed_at}
    actual: {value_or_unknown, source_ref, source_freshness, accounting_basis, observed_at}
    remaining: {value_or_range, basis: limit-settled-conservative_outstanding, computed_at}
    clock: {clock_domain_id, host_id, boot_id, checkpoint_segment_id, absolute_deadline, uncertainty_bound, uncertainty_policy}
    decision: {point, action, route, reason_code, decided_at}
    uncertainty: {unknown_fields, bounds, stale_after, reconciliation_state}
    refs: {checkpoint_ref, provider_receipt_refs, authority_ref, trace_ref}
  ```

- Honest-record rules:
  - 缺 actual 时写 `UNKNOWN/PENDING_RECONCILIATION`，不复制 estimate。
  - 同一 consumption id 的 reservation / incurred-pending / actual 互斥；transition 用 replace；只有 unused/aborted/proven-absent amount 可 release。
  - remaining 可以是 range；basis固定为 limit减settled与conservative outstanding，并记录 computed_at/source freshness。
  - monotonic stamps 缺兼容 clock domain 时不相减；使用持久化absolute deadline与bounded uncertainty，无法界定则`UNKNOWN/BLOCKED/STOP`。
  - provider receipt 用 ref + native identity；不把 Budget record 伪装成 invoice。
- Article 21 Boundary: `trace_ref` 只是 future seam；本篇不规定 cross-step event schema、correlation、reconstruction、re-execution 或 Failure Taxonomy。
- Article 22 Boundary: 记录不自动形成 Eval dataset，也不证明 degradation policy 保持质量或修复能防回归。
- Section Takeaway: **可审计 Budget record 不追求把未知填满，而要让每个 estimate、hold、actual、decision 和未决账项都能回到来源与时点。**

## Part D｜具体设计：BuildPilot read-only investigation 的 `BudgetEnvelope`

### 10. Design-only envelope 与 fail-closed walk-through

- Reader Question: 在不实现 Runtime 的前提下，BuildPilot 的只读启动性能调查怎样表达一份可审查 Budget？
- Claim / Evidence posture: `20-C09 PROPOSAL`。
- Evidence Card locators:
  - `20-E10`：Article 20 research “Concrete design: BuildPilot budget envelope”。
  - `20-E11`：canonical BuildPilot Design v1 ceiling 与 Articles 20—22 ownership。
- Global Label: `CONSTRUCTED COURSE DESIGN / DESIGN ONLY / NOT IMPLEMENTED / NOT RUN / NO FIXED PRICE OR WINDOW`。
- Proposed envelope sketch:

  ```yaml
  budget_envelope:
    classification: DESIGN_ONLY_NOT_IMPLEMENTED_NOT_RUN
    scope: RUN/startup-investigation-readonly
    authority_ref: REQUIRED_FROM_ARTICLE_19_NOT_CREATED
    token:
      unit_contract: PROVIDER_MODEL_REQUEST_IDENTITY_REQUIRED
      estimate: UNKNOWN
      output_reserve: POLICY_REQUIRED
      hard_limit: POLICY_REQUIRED
      actual_receipt_refs: []
    step:
      counting_rule: committed_step_v1
      increment_point: SUCCESSFUL_STEP_COMMIT_EXACTLY_ONCE
      in_flight_reservations: []
      retry_rule: EXPLICIT_REQUIRED
      limit: POLICY_REQUIRED
      used: 0_DESIGN_PLACEHOLDER_NOT_RUNTIME
    cost:
      currency: REQUIRED
      estimate: UNKNOWN
      price_snapshot_ref: REQUIRED_OR_UNKNOWN
      accounting_rule: LIMIT_MINUS_SETTLED_MINUS_CONSERVATIVE_OUTSTANDING
      reservations: []
      incurred_pending: []
      actual: UNKNOWN
      remaining: UNKNOWN
    latency:
      end_to_end_deadline: REQUIRED_NOT_SET
      child_timeout_rule: MUST_FIT_REMAINING
      clock_domain_id: REQUIRED
      host_id: REQUIRED
      boot_id: REQUIRED
      checkpoint_segment_id: REQUIRED
      uncertainty_policy: FAIL_CLOSED_IF_UNBOUNDED
      phase_receipts: []
    routes: [STOP, DEGRADE_OPTIONAL_WORK, REQUEST_APPROVAL, PARTIAL_RESULT]
    audit_ref: NOT_CREATED
    trace_ref: ARTICLE_21_SEAM_ONLY
  ```

- Placeholder Boundary: `used: 0_DESIGN_PLACEHOLDER_NOT_RUNTIME` 只表示样例初始 shape，不是观测值；正文可改用 `NOT_STARTED` 避免被误读为真实 counter。
- No numeric example: 不填模型名、window、token ceiling、单价、预算金额、timeout 秒数或真实 deadline。
- Section Takeaway: **BuildPilot envelope 的价值是让缺失 identity、estimate、reservation、actual 和 route 都可见，而不是展示一套已经工作的预算系统。**

#### 从 admission 到 partial result，只演示设计判断

- Reader Question: 这份 envelope 在哪些检查点可以继续，在哪些缺口必须停止或请求决策？
- Claim / Evidence posture: `20-C06`, `20-C07`, `20-C08`, `20-C09` 均为 `PROPOSAL`。
- Evidence Card locators: `20-E10`, `20-E11`；Token/Step/Cost/Latency 的窄事实分别回指 `20-E01`—`20-E09`。
- Walk-through:
  1. `authority_ref` 缺失 -> Budget check 不运行代授权；返回 Article 19 authority boundary。
  2. provider/model/request identity 未选 -> Token estimate 与 unit contract 保持 `UNKNOWN`；不能用字符数猜 capacity。
  3. price snapshot/currency 缺失 -> cost actual 不可合成；按 policy reject、request bounded input 或以明确 UNKNOWN 路由。
  4. admission PASS（仅设计条件）-> 创建 internal reservation candidate；不声称 provider 已扣费。
  5. queue dequeue/resume -> 加载 persisted Step used/in-flight reservation、Cost outstanding、absolute deadline与clock-domain/host/boot/segment identity；same-domain用monotonic delta，跨域用absolute deadline + bounded uncertainty，无法界定则`UNKNOWN/BLOCKED/STOP`。
  6. before Step/provider/tool -> 先验证 legal transition、authority、retry/effect eligibility，再以consumption id reserve四维 incremental allowance；Step尚不增加`used`。
  7. receipt / successful Step commit -> 同一identity做replace：Step reservation转为唯一`used + 1`；Cost reservation转为measured/incurred-pending，只有unused delta可release；source-qualified actual到达后再替换pending。
  8. 任一 hard dimension 将超限 -> 不启动新工作；按冻结 route STOP / REQUEST_APPROVAL / PARTIAL_RESULT，只有 optional work 才可 DEGRADE。
  9. terminal -> 保存 Budget record 与 `trace_ref` seam；不构造 Article 21 event stream，不评价 Article 22 quality/regression。
- Runtime Boundary: 这不是 provider call、queue simulation、cost calculation、deadline test、BuildPilot demo 或 Lab observation。
- Section Takeaway: **设计 walk-through 的合格结果可以是停下并保留未知；它不需要假装每个缺口都能自动闭合。**

## Part E｜工程判断：用反例守住相邻责任边界

### 11. 一个 Budget 设计通常怎样写坏

- Reader Question: 哪些看似省事的等式会把 capacity、measurement、policy、authority、quality 或 time semantics 混在一起？
- Claim / Evidence posture: 不新增 Claim；只把 `20-C01`—`20-C09` 转成 design-review counterexamples。
- Planned counterexample table:

  | Shortcut | Why wrong | Minimum correction |
  |---|---|---|
  | `fits_context = within_run_token_budget` | capacity 与 Run policy 不同 | 分开 model contract、estimate/usage、limit/reserve |
  | `character_count = token_actual` | tokenizer/request surface/provider 不同 | provider-native count/receipt + uncertainty |
  | `max_turns = max_steps everywhere` | AI invocation、super-step、course Step unit 不同 | 冻结 counting rule/scope/retry/fan-out |
  | `step cap reached = task complete` | cap 只限制新工作 | terminal 写 exhausted/incomplete |
  | `usage × current public price = actual` | price drift、tier/discount/cache/non-token/billing basis 未闭合 | dated snapshot estimate；actual 等 source-qualified record |
  | `reserved = spent` | hold 可能 release/pending | 保存 reservation lifecycle + reconcile |
  | `reservation + pending + actual` | 同一charge被重复扣减，或pending期提前释放 | 同一`charge_id`在互斥bucket间replace；remaining扣settled+outstanding |
  | `timeout = deadline = latency budget` | child duration 忽略 earlier elapsed/queue/parent | absolute deadline + remaining + child allocation |
  | `new process monotonic - old process monotonic` | host/boot/clock origin可能不可比 | 冻结clock-domain/host/boot/segment；跨域用absolute deadline+bounded uncertainty或fail closed |
  | `sum(child durations) = elapsed` | parallel branches double-count | 使用 application-visible DAG critical path |
  | `budget remains = retry/action allowed` | authority、effect、failure class 独立 | 先过 Article 19 / Article 11 gates |
  | `degrade = quality preserved` | 没有 Eval/Regression evidence | 只称 policy route；交给 Article 22 验证 |
  | `budget record = complete trace` | 单一 decision record 不拥有跨 step reconstruction | 只留 `trace_ref`；交给 Article 21 |

- Section Takeaway: **预算系统最危险的捷径，是把一个数字或 PASS 传播成其他责任面的保证。**

### 12. Ownership 与 verification callout：本篇必须停在哪些边界

- Reader Question: Article 20 与 01/10/11/12/19/21/22 分别拥有哪一段问题？
- Claim / Evidence posture: `20-C09 PROPOSAL` + `20-E11` repository ownership confirmation。
- Boundary matrix:

  | Owner | Owns | Article 20 consumes | Article 20 does not do |
  |---|---|---|---|
  | Article 01 | Model API、Messages、Token、Context Window basics | capacity/usage distinction | 不重讲 tokenization/provider API 入门 |
  | Article 10 | State Machine/Workflow、committed Step、legal transition | Step unit seam + enforcement point | 不重定义 State、Guard、commit authority |
  | Article 11 | Checkpoint/Resume、Retry eligibility、effect uncertainty、Recovery | persist/revalidate budget state | 不用余额批准 Retry，不设计 compensation/exactly-once |
  | Article 12 | Context Select/Order/Scope/Fit Budget、Context Receipt | request estimate/revision seam | 不重讲 assembly、pollution、provider-hidden context |
  | Article 19 | Permission/Approval/HITL/Sandbox 与 action authority | `authority_ref` + budget-change approval seam | Budget 不授予动作权限、不越过 hard deny |
  | Article 21 | cross-step Trace、Replay、Failure Taxonomy | `trace_ref`、budget-local decision/reason seam | 不定义 event schema、correlation、reconstruction/re-execution、跨层 taxonomy |
  | Article 22 | Eval、Golden Dataset、Regression | future quality/regression question | 不声称 cap/degrade 改善质量或防止回归 |

- Canonical Boundary: ownership 是 repository-local course contract，不称行业统一架构。
- Section Takeaway: **Article 20 只拥有资源准入、扣减、耗尽与对账；动作权、恢复、Trace 和质量回归各有独立 owner。**

#### 验证边界：本篇能建立什么，不能证明什么

- Reader Question: Required Lab 为 NONE、Runtime Observation 为 ABSENT 时，这篇设计的可信上限是什么？
- Claim / Evidence posture: `20-C01`—`20-C09`，严格遵守各自 status ceiling。
- Can establish:
  - `20-C02 CONFIRMED`：Context Window、preflight estimate、provider response usage 与 application Run Token Budget 是不同对象；Provider 语义保持 source-native。
  - `20-C01/C03/C04/C05 PARTIAL`：现有产品/协议/规范支持四维分离、产品限定计数、cost basis 与 deadline/timeout 窄边界；统一 taxonomy、phase ledger 与 critical path 仍含课程 synthesis。
  - `20-C06/C07/C08/C09 PROPOSAL`：课程可提出 lifecycle、route、audit record 与 BuildPilot envelope，并明确 falsifier/unknown。
- Must remain absent:
  - 当前模型窗口、价格、service tier、tokenizer 误差界或通用 timeout 数值。
  - 真实 token/cost/latency receipt、reservation、queue observation、critical-path measurement 或 provider billing read。
  - atomic reservation、race freedom、runtime correctness、成本节省、低延迟、任务质量、安全或 production readiness。
  - Article 21 Trace/Replay/Failure Taxonomy 的完整 schema/算法/实现。
  - Article 22 Eval/Golden Dataset/Regression 的 dataset、metric、threshold 或结论。
- Frozen reality: Required Lab=`NONE`；Experiments=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。
- Section Takeaway: **来源足以切清合同和设计边界，但没有 Lab/Runtime，就不能把 Budget proposal 写成运行收益或生产保证。**

## Part F｜Learning Check 与职业能力映射

### Learning Check（题目 + answer expectations）

1. Article 19 的 action authority 已 PASS，为什么仍可能拒绝本次 Run admission？反过来 budget remains 为什么不能授权动作？
   - Expected answer: authority 与 resource admission 是串联且独立的 gates；前者回答能不能做，后者回答已获权动作是否有资源资格。任一 PASS 都不替代另一项。
2. Context Window、preflight token count、response usage 与 Run Token Budget 分别回答什么？
   - Expected answer: capacity、estimate、provider-native receipt、application policy 四分账；必须绑定 provider/model/request identity，不能用字符数或单一字段跨 Provider 外推。
3. 设计 `max_steps` 时至少要冻结哪些 counting-rule facts？
   - Expected answer: unit、scope、retry、tool fan-out/parallel semantics、checkpoint/resume persistence；pre-Step admission只reserve，successful committed Step以同一`step_attempt_id`唯一`used + 1`，abort-before-commit release；cap reached只说明exhausted/incomplete。
4. Cost estimate、reservation 与 actual 为什么不能互换？缺价格或账务来源时怎样记录？
   - Expected answer: estimate不扣账；同一`charge_id`从reservation replace为measured/incurred-pending，再由source-qualified actual replace；仅unused delta可release。remaining按limit-settled-conservative outstanding计算；缺项保存range/UNKNOWN，不合成精确值或提前释放。
5. Deadline、timeout、queue time、service time 与 critical path 怎样分开？
   - Expected answer: deadline是absolute point，timeout是child duration；queue/service需要同一可比clock domain内的边界时间戳；critical path依赖可见DAG。checkpoint保存clock-domain/host/boot/segment；跨域用absolute deadline+bounded uncertainty，无法界定则UNKNOWN/BLOCKED/STOP。该resume contract是课程设计，不是gRPC保证。
6. 为什么 Budget check 要出现在 admission、resume、Step/call 前和 receipt 后？
   - Expected answer: request、elapsed、outstanding、used/remaining会变化；新消耗前按identity reserve，receipt后以replace而非重复相加完成commit/pending/release/reconcile；resume必须重验budget与clock identity，不能重置预算。
7. hard limit 将被超过时，STOP、DEGRADE、REQUEST_APPROVAL、PARTIAL_RESULT 怎样选？
   - Expected answer: route 来自每维 hard/soft policy；required invariant/authority 未满足必须 stop；只有 optional work 可 degrade；approval 只请求预算变化；partial 保留 known/unknown/unverified。
8. Budget audit record 为什么允许 actual/remaining 为 `UNKNOWN` 或 range？`trace_ref` 又为什么不代表 Trace 已完成？
   - Expected answer: future billing/hidden queue/price attribution 可能未闭合，诚实 uncertainty 比伪精确更可审计；record 只保存 decision seam，Article 21 才拥有 cross-step Trace/Replay/Failure Taxonomy。
9. `degrade` 后任务看起来仍完成，为什么本文不能声称质量未下降？
   - Expected answer: Budget route 不是质量证据；需要 Article 22 的 fixed tasks、criteria、dataset 与 regression comparison。
10. BuildPilot `BudgetEnvelope` 为什么必须保留 `DESIGN / NOT IMPLEMENTED / NOT RUN` 以及 `UNKNOWN/REQUIRED` 字段？
    - Expected answer: 当前无 Lab、provider call、cost read 或 runtime receipt；占位字段暴露缺口，防止设计 shape 被误读为真实运行结果。

### Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Multi-dimensional resource modeling | four-dimension comparison + `BudgetVector` | 能为每维写清 unit、estimate/actual、enforcement point 与 non-inference | course taxonomy，非行业统一标准 |
| Provider contract reasoning | Token/Step/Cost native-contract comparison | 能保留 provider/model/version/source identity，不做静默 normalization | moving docs，no runtime |
| Reliable control design | enforcement lifecycle + matrix | 能把 authority/retry eligibility 与 Budget reservation 排序，按identity单值转移，并在 resume 重验 | Proposal，no atomic/race guarantee |
| Cost/latency engineering judgment | single-accounting cost ledger + clock-domain latency decomposition | 能识别settled/outstanding remaining basis、price/source freshness、deadline/timeout与cross-domain uncertainty | no fixed price/window，no measured outcome |
| Exhaustion and partial-result design | route table + counterexamples | 能让 hard/soft policy、unknown 与 incomplete terminal 可审计 | 不证明 quality/safety |
| Cross-system architecture | Articles 01/10/11/12/19/21/22 boundary matrix | 能把 capacity、state、recovery、context、authority、trace、eval 分层 | repository-local ownership |
| Evidence discipline | 9/9 coverage + status ceilings | 能区分 CONFIRMED/PARTIAL/PROPOSAL，并让 UNKNOWN 保持可见 | Required Lab NONE；runtime ABSENT |

### Closing bridge to Article 21

- Closing sentence: `预算的工程价值，不在于把所有消耗换算成一个数字，而在于每次新增工作之前都能回答“还能不能做”，耗尽之后仍能说明“为什么停、留下了什么未知”。`
- Next bridge:
  - Article 20 只留下 `budget_id/run_id/checkpoint_ref/provider_receipt_refs/authority_ref/trace_ref` 等 seam 和 budget-local decision reason。
  - Article 21 才正式回答如何把跨 Step、Tool、Provider 与 State transition 的事件关联成 Trace，怎样区分 reconstruction 与 side-effect re-execution，以及 Failure Taxonomy 怎样定位错误层。
  - Article 22 才用固定任务、判据、Golden Dataset 与 Regression 回答 cap/degrade/recovery 改动是否让质量变好或再次变坏。
- Mandatory boundary sentence: **本篇不预写 Article 21 的 event schema / replay algorithm / failure classes，也不预写 Article 22 的 metrics / dataset / thresholds / regression verdict。**

## Claim-to-section coverage（9 / 9）

| Claim | Status ceiling | Primary sections | Evidence Cards / exact locators | Mandatory wording / boundary |
|---|---|---|---|---|
| `20-C01` | PARTIAL | Opening, 1, 6, 12 | `20-E01` Responses caps/usage/terminal；`E02` input count；`E04` SDK max_turns；`E05` LangGraph super-step；`E06` gRPC deadline；`E07` org costs | 四维是 source-supported course taxonomy，不称 industry-unified model |
| `20-C02` | CONFIRMED | 2, 6, 12 | `20-E01` `POST /responses`；`E02` `POST /responses/input_tokens`；`E03` Anthropic Token Counting + Messages usage | provider/model/request identity；无固定 window；无 universal field mapping |
| `20-C03` | PARTIAL | 3, 6, 11, 12 | `20-E04` Running agents “agent loop/exceptions”；`E05` Graph API recursion limit/counter；`E10` Article 10 Step seam | product units不等于 universal Step；pre-Step reserve，successful commit唯一`used + 1`；cap不保证 completion/quality |
| `20-C04` | PARTIAL | 4, 6, 11, 12 | `20-E07` `GET /organization/costs`；`E08` Anthropic Usage and Cost API；`E09` FOCUS 1.4 §§3.1.7/3.1.35/3.1.40；`E10` synthesis | reservation/pending是course design；同一charge互斥replace；remaining扣settled+outstanding |
| `20-C05` | PARTIAL | 5, 6, 11, 12 | `20-E06` gRPC Overview/Client/Server/Propagation；`E10` phase ledger/critical path design | gRPC只支撑deadline/timeout；cross-domain resume是course proposal，unbounded uncertainty fail closed |
| `20-C06` | PROPOSAL | Opening, 1, 7, 10, 12 | `20-E10` lifecycle/enforcement matrix；`E11` ownership/BuildPilot ceiling | design lifecycle only；single-accounting replace；Budget不授予 action/retry authority；not implemented |
| `20-C07` | PROPOSAL | Opening, 8, 10, 11, 12 | `20-E01`, `E04`, `E06` product terminals；`E10` routing synthesis；`E11` authority/quality seams | STOP/DEGRADE/APPROVAL/PARTIAL是 policy design；不保证 quality/safety |
| `20-C08` | PROPOSAL | 9, 10, 12 | `20-E07`, `E08`, `E09` source-qualified cost boundaries；`E10` record；`E11` Article 21/22 seam | record含consumption/remaining/clock basis；不是 invoice、full Trace 或 Eval schema |
| `20-C09` | PROPOSAL | 10, 12, Closing | `20-E10` BuildPilot envelope；`E11` canonical Part IV + BuildPilot ceiling | envelope含single-accounting与clock fail-closed；`DESIGN / NOT IMPLEMENTED / NOT RUN` |

Coverage: `9 / 9`；Status mix: `1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`；new core Claim/Card: `NONE`。

## Source and visual plan

### Source plan

- Draft 不新增核心 Claim 或 Evidence Card；每节只消费 `20-E01`—`20-E11` 的 `Supported conclusion / Does not prove / Limitations`。
- Moving product contracts 均保留 retrieval date `2026-08-26` 与 product scope；OpenAI Agents SDK `v0.22.0`（released 2026-08-19）和LangGraph `1.2.11`（released 2026-08-11）仅作为exact official tag/release snapshot，不暗示hosted docs由对应tag构建；不复制当前 model window、price、default limit 或 service-tier 数字。
- Version-fixed anchor 仅 FOCUS publication `1.4`；正文明确其为 billing-data standard，不是 Agent admission/reservation protocol。
- Repository evidence只用于课程 ownership、Step/Context/authority seams 和 BuildPilot ceiling，不外推为行业标准或 runtime fact。

### Visual and table plan

1. **Opening two-gate contrast**: `action authority != budget admission`；标注 `COURSE RELATIONSHIP DIAGRAM / NOT RUN`。
2. **Four-dimension comparison table**: unit/estimate/actual/enforcement/non-inference；职责是阻止 scalar Budget。
3. **Token distinction table**: Context Window vs count/usage vs Run Budget；职责是落实唯一 CONFIRMED Claim。
4. **Cost accounting-state table**: estimate/reservation/incurred-pending/source-qualified actual与remaining basis；标注transitions为`COURSE PROPOSAL`。
5. **Latency ledger diagram**: deadline/timeout/queue/service/critical path + clock-domain/host/boot/segment；标注 application-visible boundary 与 cross-domain fail-closed。
6. **Enforcement lifecycle + matrix**: 展示何时 revalidate/consume/release/reconcile；标注 `PROPOSAL / NOT IMPLEMENTED`。
7. **Exhaustion route table**: STOP/DEGRADE/REQUEST_APPROVAL/PARTIAL_RESULT；显示 authority/quality non-substitution。
8. **Inline Budget record and BuildPilot envelope**: 只用 schema sketch，不创建 asset、不伪造 runtime screenshot；所有缺值保持 `UNKNOWN/REQUIRED/NOT RUN`。

## Explicit non-scope

- 不实现 Budget service、reservation store、pricing adapter、deadline propagation、queue instrumentation、critical-path calculator 或 BuildPilot Runtime。
- 不执行 Lab、provider request、billing API、latency measurement、queue simulation 或成本计算；Required Lab=`NONE`，Experiments=`0`，Runtime Observation=`ABSENT`。
- 不固化 model Context Window、price、service tier、timeout、deadline 或预算额度数字。
- 不把 OpenAI turn、LangGraph super-step、course committed Step 或 Provider usage fields 标准化成相同单位。
- 不声称 reservation atomic/race-free，不声称 estimate 等于 invoice，不声称 visible phase ledger覆盖 provider-internal scheduling。
- 不让 Budget PASS 替代 Article 19 authority、Article 10 legal transition 或 Article 11 retry/effect safety。
- 不声称 cap、STOP、DEGRADE 或追加预算提高正确率、质量、安全、可靠性、成本收益或生产表现。
- 不提前完成 Article 21 cross-step Trace、Replay、reconstruction/re-execution 或 Failure Taxonomy；只保留 refs/seams。
- 不提前完成 Article 22 Eval、Golden Dataset、metrics、thresholds 或 Regression verdict。
- 不创建 Draft、Review、Lab、assets、Published Content、global/canonical 文件或未来 Article artifact。

## Outline Gate self-check

- [x] M-weight Principle structure follows problem space -> abstract model -> concrete BuildPilot design -> engineering/verification boundary；没有 API-first 开场。
- [x] Numbered teaching units=`12`，Learning Check / competency / coverage 作为 gate appendices，不扩成 L-weight 第二主线。
- [x] `20-C01`—`20-C09` coverage=`9 / 9`；Evidence Cards=`20-E01`—`20-E11` only；new core Claim/Card=`NONE`。
- [x] Status ceilings preserved: `C02 CONFIRMED`；`C01/C03/C04/C05 PARTIAL`；`C06/C07/C08/C09 PROPOSAL`；`BLOCKED=0`。
- [x] Article 19 action-authority bridge、four-dimension comparison、Token/Context/usage distinction、Step counting rule、Cost estimate/reservation/actual、Latency decomposition、enforcement lifecycle、exhaustion routing、uncertainty/audit record均有明确教学落点与 Card locator。
- [x] Counterexamples、Learning Check + answer expectations、Job Competency、source/visual duties完整。
- [x] Article 21 retains cross-step Trace/Replay/Failure Taxonomy ownership；Article 22 retains Eval/Golden Dataset/Regression ownership。
- [x] No fixed price/window/service-tier/timeout/deadline numbers；Provider native semantics preserved。
- [x] Required Lab=`NONE`；Experiments=`0`；Runtime Observation=`ABSENT`。
- [x] BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`；no implementation、runtime、usage、cost、latency、quality、safety、benefit or production Claim。
- [x] No Draft/Review/Lab/runtime/content/global/canonical/Git/future-Article write is included in this Author output。
