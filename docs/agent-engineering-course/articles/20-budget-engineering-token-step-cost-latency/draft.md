# Budget Engineering：Token、Step、Cost 与 Latency

> 如果这篇只记一句话：`Budget 不是运行结束后的 usage 报表，而是一组在动作获权之后、资源消耗之前持续执行的多维准入与停止合同。`

Article 19 已建立 action-authority chain：一个动作只有在 Principal、Action、Resource、Constraints、Policy、必要 Approval 与 use-time Enforcement 都对得上时，才有资格进入执行边界。

但“有权做”仍不等于“现在有足够资源做”。假设 BuildPilot 准备启动一次只读的启动性能调查，即使 authority inputs 已经闭合，Runtime 仍要回答：本次 Run 允许消耗多少 Token？还能推进多少 Step？预估与实际成本怎样对账？端到端 deadline 还剩多少？

反过来也一样：预算有余额，不能给本来无权执行的动作补发许可证。

> **两道独立 Gate｜COURSE RELATIONSHIP DIAGRAM / NOT RUN**
>
> ```text
> action authority
>   = 动作在当前 policy / approval / enforcement 下有资格执行
>
> budget admission
>   = 已获权动作在声明的 Token / Step / Cost / Latency 合同下
>     有资源资格启动或继续
>
> authority PASS != budget PASS
> budget remains != action authorized
> ```

本文建立的是一套课程级 Budget 工程模型。四维分离有产品合同、协议和账务规范作为有界依据，但统一生命周期、路由、审计记录与 BuildPilot envelope 都是 **COURSE PROPOSAL / NOT IMPLEMENTED**。本文 Required Lab=`NONE`，Experiment Count=`0`，Runtime Observation=`ABSENT`；BuildPilot 始终是 **DESIGN / NOT IMPLEMENTED / NOT RUN**。

## 1. 为什么结束后 usage 还不叫 Budget

运行结束后汇总 token usage、调用次数、账务金额和 elapsed time，回答的是“已经观察到什么”。Budget Engineering 还要在执行前回答是否接单，在执行中回答是否允许新增工作，在耗尽时选择合法终止路径，并在结束后把预估、占位、实际、剩余和未知对齐。

四类资源会相互影响，却不存在稳定的一一换算。

| Dimension | Unit contract | Estimate / capacity input | Actual evidence | Typical enforcement point | 不能推出 |
|---|---|---|---|---|---|
| Token | Provider、Model、request surface 限定的类别 | input count/estimate + output reserve | Provider response usage 或后续 usage record | model call 前；response 后对账 | Context fit 等于 Run budget fit；跨 Provider 字段相同 |
| Step | 课程 committed Step，或 named runtime 的精确单位 | limit + counting rule | committed counter 或产品 terminal | 新 Step admission 前；checkpoint/resume | 到顶等于任务完成；turn 等于 node 或 super-step |
| Cost | currency + pricing identity + accounting basis | usage estimate × dated price snapshot + 已知非 Token 项 | 有来源的 cost/billing record | Run admission；chargeable operation 前；reconcile | estimate/reservation 等于 billed actual |
| Latency | monotonic duration + wall-clock deadline identity | end-to-end deadline + child allowance | 应用可见时间戳或有来源的 metrics | admission；queue dequeue；child call 前；resume | 一个 timeout 等于端到端预算；隐藏 queue time 已知 |

OpenAI Responses 当前文档（2026-08-26 访问）分别暴露输出上限、response usage、截断与 incomplete reason；OpenAI Agents SDK 当前文档用自己的 turn 计数；gRPC 官方文档区分 deadline 与 timeout；OpenAI 的组织成本接口又是独立的历史管理 surface。它们共同支持“责任必须分开”，却没有定义一个行业统一的四维 Agent Budget。因此本篇的四维 taxonomy 只能写成 **source-supported course taxonomy / PARTIAL**。

这里还要把四种职责分开：

- measurement / receipt 说明某个来源目前报告了什么；
- budget contract 定义某个 scope 的 limit、unit、hard/soft policy 与 uncertainty；
- enforcement decision 决定在当前执行点 admit、reject、degrade、ask 还是 partial；
- reconciliation 解释 estimate/reservation 与 actual/unknown 怎样闭合。

`usage observed -> next action allowed` 是非法跳跃。`budget remains -> retry allowed` 也一样：Retry 的 effect knowledge、stable identity 与 failure eligibility 先由 Long-running Runtime 判断，动作权限先由 authority owner 判断，Budget 只限制已经合格的候选。

**这一节的结论：Usage 是证据输入，Budget 是控制合同；只有执行点上的决定才能约束下一项工作。**

## 2. Token 要分成四本账

“请求放得进窗口”“计数接口返回一个值”“response 报告 usage”“本次 Run 还有额度”，说的是四个不同对象。

| Object | 回答的问题 | Identity / evidence | 不能替代 |
|---|---|---|---|
| Context Window | 目标 Model / request contract 能容纳什么 | Provider + Model + request surface + current contract | Run policy、质量或价格 |
| Preflight count / estimate | 当前可见 request 预计占多少 | estimator + Provider/Model + request revision + observed_at | 最终 usage、输出或 actual cost |
| Response usage receipt | Provider 对该 response 报告了哪些原生类别 | provider receipt identity + native fields | 跨 Provider 统一字段、完整账单 |
| Run Token Budget | 应用对 Run/scope 允许消耗多少 | policy id/version + limit/reserve/remaining | Context fit、质量或 action authority |

这是本篇唯一达到 `CONFIRMED` ceiling 的核心区分。OpenAI 当前 Responses API 把 input token count 作为 response creation 之前的独立 operation，并在 response 上提供自己的 usage 结构；Anthropic 当前 Token Counting 文档明确把 count 称为估计，Messages response 则提供 Anthropic 自己的 usage 类别。两者都只证明各自当前 product surface，不证明 tokenizer、字段类别或账务语义可以互换。

因此，应用至少要保留：

```text
provider / model / request_surface / request_revision
estimate_source / observed_at
output_reserve
provider_native_receipt_ref
run_policy_id / run_limit / remaining
```

字符数不能代替 Provider-native count；preflight estimate 不能冒充最终 usage；response usage 不能冒充完整账单；“窗口放得下”也不能冒充 Run 预算充足。Article 01 已经建立 Token 与 Context Window 基础，Article 12 已经建立 Context selection、fit 与 receipt；本篇只消费这些边界来做 Run-level admission 与 reconciliation。

本文不写任何当前模型窗口数，也不把某个 Provider 的相似字段强制映射为通用字段。未来若要加入具体例子，必须重新绑定精确 Model contract、request surface 与访问日期。

**这一节的结论：Context Window 是 capacity，count/usage 是 Provider-scoped measurement，Run Token Budget 才是应用资源 policy。**

## 3. Step Budget 先冻结计数单位

“最多若干 Step”如果没有 counting rule，仍是一句不可审计的话。第一次尝试算不算？一次模型调用带多个 Tool call 怎么算？并行 node 各算一次，还是共享父级单位？Retry 和 resume 后是否沿用原计数？

两个当前产品例子足以说明不能只看名称：

- OpenAI Agents SDK 当前 Running agents 文档（2026-08-26 访问）把一个 turn 定义为一次 AI invocation，包含其中的 Tool calls；超过 `max_turns` 会产生 `MaxTurnsExceeded`。这是 OpenAI Agents SDK 的产品单位。
- LangGraph 当前 Graph API 文档（2026-08-26 访问）让 `recursion_limit` 约束 graph super-step，并以 `GraphRecursionError` 表达耗尽；一个 super-step 可以包含并行 node。它不是一次 AI invocation。

课程里的 Step 则沿用 Article 10：一次 committed loop iteration 或本地可审计执行单元。三者都能设置上限，却不能互相换算。

一份可审计 counting rule 至少要冻结：

1. `unit_name`：committed Step、AI invocation、graph super-step，还是其他明确单位；
2. `scope`：Run、Stage、branch 或 retry group；
3. pre-Step admission 只按 `step_attempt_id` 创建一个 in-flight reservation；first successful committed Step 才计入 `used`；
4. Retry 是否再次计数，从哪个 persisted counter 继续；
5. Tool fan-out / parallel node 由谁计数；
6. successful Step commit 将 reservation 替换为 `used + 1`，且只执行一次；abort-before-commit release；checkpoint/resume 后 `used/in_flight_reserved/remaining_to_admit` 保持同一 identity。

Step Budget 的 enforcement 点应在“接纳一个新单位”之前，但此时只做 eligibility check 与单一 in-flight reservation，不增加 `used`；`remaining_to_admit = limit - used - in_flight_reserved`。State Machine 仍负责 legal transition，Long-running Runtime 仍负责 Retry/effect eligibility；只有 Article 10 定义的 Step 成功 commit 时，Budget 才按同一 `step_attempt_id` 把 reservation 替换成唯一一次 `used + 1`。若 commit 前中止则 release；新进程恢复时也不能把计数器或 in-flight reservation 清零，否则同一个 Run 会因重启获得一份新预算。

到达上限只能说明：在当前 counting contract 下，不再接纳新 Step。合法 terminal 应保留 `BUDGET_EXHAUSTED / INCOMPLETE` 或本地等价语义，不能改写成 task success、bad quality 或 unsafe effect。

**这一节的结论：Step 上限只有和 unit、scope、Retry、fan-out 与 resume persistence 绑定后才有意义。**

## 4. Cost 要按单一 identity 在 estimate、reservation、pending 与 actual 间转换

把预估 Token 乘一个网页上的当前单价，只能得到带假设的估算，不能得到 actual cost。价格可能漂移，service tier、折扣、cache、Tool 与非 Token line item 也可能没有闭合；更重要的是，Provider usage receipt 与账务记录本来就是不同来源。

| State | Minimum identity | 能证明 | 不能证明 |
|---|---|---|---|
| Estimate | usage/range + assumptions + currency + dated `price_snapshot_ref` + model/service identity | 在明确假设下的预估 | Provider cost record、invoice、精确 actual |
| Reservation | internal reservation id + scope + amount/range + create/release/commit state | 当前 policy 暂扣了并发可用额度 | Provider 已计费、FOCUS 标准字段、原子或无竞态 |
| Incurred pending | `charge_id` + measured usage/known charge range + measurement ref + completeness | 已发生但尚未由账务来源结算的 conservative outstanding | source-qualified actual、invoice finality |
| Actual | source-qualified cost/billing record + freshness + accounting basis | 该来源在该时点报告的 monetary record | 所有 line item 可归因于一个 Run、即时 invoice finality |

OpenAI 当前 Organization Costs API 和 Anthropic 当前 Usage and Cost API（均于 2026-08-26 访问）都把历史 usage/cost reconciliation 放在与单次 response 不同的管理 surface；它们的可用性、归因和字段仍是产品限定的。FOCUS 1.4 则在 billing-data 标准范围内区分 List、Effective 与 Billed Cost，并明确 Billed Cost 不是估计或推断。这些来源支持 cost basis 与 source identity 必须显式，却没有定义 Agent admission 或 reservation protocol。

`reservation` 与 `incurred_pending` 是本课程的 Proposal：并发 Run 在 admission 后先做内部 hold，避免各自都看到同一份可用额度。它不是 Provider billing primitive，也不证明实现具备 atomicity 或 race freedom。

这里必须冻结单值不变量。对同一 `charge_id`，reservation、incurred-pending、source-qualified actual 与 released 只能占一个 bucket；transition 是 replace，不是把新旧 bucket 相加。response/result 返回后，已测 usage 或已知 charge 先替换 reservation 成 `incurred_pending`；只释放 reservation 中明确未使用的 delta。若 attribution、price 或 line-item completeness 未闭合，就继续保留 conservative outstanding，不能因为账单未到而释放。source-qualified actual 到达后，再用它替换 pending。

Cost 的可准入余额按同一 accounting basis 计算：

```text
remaining = limit - settled_actual - conservative(outstanding)
```

这里 outstanding 对每个 `charge_id` 只取当前 reservation 或 incurred-pending，绝不同时取两者；hard admission 使用 outstanding range 的 upper bound，若没有 finite bound 则 remaining 为 `UNKNOWN` 并 STOP。estimate 只提供决策输入，不直接扣减 remaining。

如果 usage、price、currency、service tier、discount、cache 或非 Token line item 缺失，系统应保存 range 或 `UNKNOWN`。最危险的做法不是“不知道”，而是把 estimate 复制到 actual 字段，制造无法追溯的伪精确。

本文不列当前价格、金额或价格有效期。任何具体估算都必须在执行时绑定带日期的 `price_snapshot_ref`；没有这个 identity，cost 只能保持有界 estimate 或 `UNKNOWN`。

**这一节的结论：Estimate 是计算，reservation 是内部占位，actual 是有来源和新鲜度的账务事实。**

## 5. Latency 不是给每个调用加一个 timeout

给每个 Tool 一个 timeout，仍然可能在 queue 中等很久，也可能在之前的 Step 已经用掉大部分端到端预算。并行分支的 duration 全部相加，还会重复计算同时发生的时间。

gRPC 官方 Deadlines guide（2026-08-26 访问）提供一个窄而稳定的边界：deadline 是调用方不愿再等待的绝对时间点；timeout 是允许某个操作持续的最大时长；传播到下游时应扣除已经流逝的时间。这个结论只属于 gRPC 语义，不自动定义 Agent Runtime。

本课程据此提出 application-visible latency ledger：

```text
admitted_at
  -> queued_at -> dequeued_at
  -> started_at
     -> child spans + known dependency edges
  -> completed_at
```

- **Deadline** 保存 wall-clock identity；每次 dequeue、resume 或 propagation 都重新计算 remaining。
- **Timeout** 是 child operation 的 duration allowance，必须装进当前 remaining，却不覆盖此前已经消耗的时间。
- **Queue time** 只有 `queued_at/dequeued_at` 两端都由应用可见时才能计算。
- **Service time** 只有 owned boundary 的 start/complete 齐全时才能计算。
- **Critical path** 只能在应用已知 execution DAG 上求最长 dependency path；它不是所有 span 之和，也不是对 Provider 隐藏调度的猜测。

每个 monotonic stamp 还必须绑定 `clock_domain_id / host_id / boot_id / checkpoint_segment_id`。只有 runtime 证明属于兼容 same clock domain 的 stamp 才能相减；process boundary 必须重新验证 identity。跨不兼容 process、host 或 reboot boundary 时，不能拿新 monotonic origin 减旧值，而要读取 checkpoint 中持久化的 absolute deadline，结合当前可信 wall clock 与显式 `uncertainty_bound/policy`，按 `safe_remaining = max(0, absolute_deadline - current_wall_clock - uncertainty_bound)` 保守计算。若 clock trust 或 uncertainty 无法界定，remaining 必须写 `UNKNOWN`，hard latency policy 路由 `BLOCKED/STOP`，不能重置 deadline 或假装还有余额。phase receipt 只解释 attribution，不从已经扣过的 end-to-end remaining 再扣一次；跨域 gap 保持 `UNKNOWN`。

gRPC 只直接支撑 deadline/timeout 与传播时扣除 elapsed 的窄语义。上述 persisted absolute deadline、clock-domain identity 与 fail-closed resume 是本课程的 Agent Runtime Proposal，不是 gRPC 已替本文解决的机制。Provider 内部 queueing 不可见时同样写 `UNKNOWN`，不能拿网络总耗时倒推出隐藏阶段。

这套 phase ledger 和 critical-path 计算仍是 **COURSE PROPOSAL / PARTIAL**：gRPC 只直接支撑 deadline/timeout 区分，没有证明本文的 Agent latency schema 已实现或正确。

**这一节的结论：Timeout 管一个子操作，deadline 管端到端等待边界；queue、service 与 critical path 只能从可见时间证据计算。**

## 6. `BudgetVector`：统一控制面，不统一语义

把四维放在同一个 envelope 中，目的不是换算成一个总分，而是让 admission 和 enforcement 能在同一控制面上看到各自的合同、证据和未知。

> **COURSE PROPOSAL / PROVIDER-NATIVE RECEIPTS / NOT A UNIVERSAL PROVIDER SCHEMA / NOT IMPLEMENTED**
>
> ```text
> BudgetVector = {
>   token:   {limit, estimated, reserved, actual, remaining, unit_contract},
>   step:    {limit, in_flight_reserved, used, remaining_to_admit, counting_rule},
>   cost:    {limit, estimated, reserved, incurred_pending, actual, remaining, currency, price_snapshot_ref},
>   latency: {deadline, elapsed, remaining, clock_domain, host_id, boot_id, checkpoint_segment_id, phase_receipts}
> }
> ```

统一的是字段职责：每一维都能回答 limit 是什么、estimate 从哪里来、actual 是否存在、remaining 怎样计算、何时需要停止。没有统一的是原生语义：

- OpenAI Responses usage 继续以 OpenAI receipt 保存；
- Anthropic Messages usage 继续以 Anthropic receipt 保存；
- OpenAI Agents SDK turn 不改名成 universal Step；
- LangGraph super-step 不改名成一次模型调用；
- Provider cost record 与 FOCUS cost basis 不被改造成课程 reservation 字段。

课程侧 adapter 只能显式引用和映射这些 receipt，并保存 Provider、Model、request、version/retrieval identity 与不确定性。它不能静默丢掉 native categories、terminal reason 或 source freshness。

四维共享的不是单位，而是单值分账纪律：一个 `consumption_id` 同时只在一个 accounting bucket；`remaining_to_admit = limit - settled - conservative(outstanding)`。Token receipt 替换 reservation；Step 只在 successful commit 唯一增加 `used`；Cost 以同一 `charge_id` 从 reservation 替换到 pending 再替换到 actual；Latency 的 end-to-end delta 只扣一次，phase receipts 不重复扣减。

**这一节的结论：BudgetVector 统一的是控制接口，不是 Provider 的 Token、turn、cost 或 timeout 语义。**

## 7. 从 Declare 到 Reconcile 的控制链

Budget 如果只在 Run 开始时检查一次，很快就会过期：request 可能改变，queue 会消耗时间，Retry 会改变 Step 和 Cost，checkpoint/resume 之间 reservation 也可能失效。课程提出下面的生命周期：

> **COURSE PROPOSAL / NOT IMPLEMENTED**
>
> ```text
> DECLARE
>   -> ESTIMATE
>   -> ADMIT | REJECT | REQUEST_APPROVAL
>   -> RESERVE by consumption_id
>   -> REVALIDATE before each Step / chargeable call
>   -> REPLACE reservation with committed usage / incurred_pending, or RELEASE unused/aborted amount
>   -> REPLACE incurred_pending with source-qualified actual when available
>   -> RECONCILE
>   -> COMPLETE | EXHAUSTED | CANCELLED | PARTIAL
> ```

不同检查点承担不同责任：

| Enforcement point | Token | Step | Cost | Latency | Required decision evidence |
|---|---|---|---|---|---|
| Run admission | expected input/output range | limit + counting rule | estimate range + currency + dated snapshot 或 `UNKNOWN` | end-to-end deadline | policy version、uncertainty、allowed routes |
| Queue dequeue / resume | request 变化则 recount | persisted used/in-flight reservation | outstanding charge identity/freshness | same-domain monotonic delta；否则 absolute deadline + bounded uncertainty；不可界定则 `UNKNOWN/STOP` | checkpoint + clock-domain/host/boot/segment identity + revalidation |
| Before Step commit | reserve planned call by consumption id | reserve one in-flight unit；不增加 `used` | reserve predicted incremental range | remaining time | current State + legal transition |
| Before Provider/Tool call | native preflight if available | declared Tool/turn rule | reserve chargeable range | child timeout within remaining deadline | action authority + Retry eligibility |
| After response/result | replace reservation with native usage receipt | successful Step commit时replace reservation并唯一`used + 1`；abort则release | reservation -> measured/incurred-pending；只release unused delta；actual可pending | comparable phase timestamps | response/result + consumption identity |
| Terminal/reconcile | settled + outstanding/unknown | used + in-flight/remaining-to-admit | pending -> source-qualified actual；未知outstanding保守保留 | elapsed + known same-domain receipts + unknown gaps | completion/exhaustion route |

顺序不能反过来：先确认 legal transition、action authority 和 Retry/effect eligibility，再 reserve Budget。一个 consumption identity 只占一个 bucket，后续用 replace 做 commit/pending/actual 转移，不能重复相加。Step `used` 只在 successful commit 增加一次；Cost remaining 始终扣除 settled 与 conservative outstanding。Budget PASS 不能批准 transition、Retry 或副作用。Resume 也必须加载原 `budget_id`、used/in-flight reservation、Cost outstanding、absolute deadline 与 clock-domain/host/boot/segment identity 后重验；跨域无法按 uncertainty policy 界定时 fail closed，不能因为换了进程就刷新额度。

这条 lifecycle 没有经过实现或并发验证，不证明 reservation 原子、账目无竞态或 Runtime 正确。它的价值在于把“新增消耗前检查，receipt 返回后对账”变成显式合同。

**这一节的结论：真正的 Budget control 在每个新增消耗边界前重验，在每份 receipt 返回后 commit、release 或 reconcile。**

## 8. 耗尽后只有四类诚实路由

某一维即将超过限制时，系统不应只抛一个“预算不足”异常，也不应自动把任务包装成成功。每一维都应声明 `hard/soft` policy 与允许的 route。

| Route | Eligible when | Must preserve | 不能越过 |
|---|---|---|---|
| STOP | hard limit 将被超过；关键事实未知且 policy fail closed | exhaustion dimension、decision point、known/unknown | authority、effect uncertainty、required Goal invariants |
| DEGRADE | policy 明确把某些工作标为 optional，且便宜路径仍保持 Goal/Evidence/authority invariants | omitted work、changed assumptions、remaining uncertainty | required context/evidence、hard policy、质量保证 |
| REQUEST_APPROVAL | policy 允许请求一份有界预算增量 | frozen request、requested delta、reason、expiry/policy identity | Article 19 hard deny 或 otherwise forbidden action |
| PARTIAL_RESULT | 已完成工作能与 unknown/unverified 诚实分开 | known、unknown、unverified、budget reason、next safe action | 把 incomplete 包装成 success |

OpenAI Responses 的 incomplete reason、OpenAI Agents SDK 的 `MaxTurnsExceeded` 与 gRPC deadline expiry，只是各自产品/协议限定的 terminal 例子。STOP、DEGRADE、REQUEST_APPROVAL、PARTIAL_RESULT 的统一路由是课程 Proposal，不是这些产品提供的通用保证。

`REQUEST_APPROVAL` 只请求预算变化。它不能覆盖 action authority 的 hard deny，也不能让原本不允许的 Tool 获权。`DEGRADE` 只能省略 optional work；如果 required Evidence、authority 或 Goal invariant 会被破坏，就必须 STOP。`PARTIAL_RESULT` 则要保留哪些已完成、哪些未知、哪些仍未验证，以及下一项安全动作为什么存在或为何为 `NONE`。

耗尽是 resource-policy terminal，不是质量结论。降级后质量是否保持，需要固定任务、判据、数据集与回归比较，由 Article 22 的 Eval / Golden Dataset / Regression 负责。

**这一节的结论：耗尽路由负责诚实停止或收窄工作，不能把资源不足改名成成功，也不能借追加预算绕过权限。**

## 9. Budget record 要允许 pending 与未知存在

事后要解释“为什么当时允许或拒绝下一项工作”，不能只保存最终余额。最小记录还要绑定 scope、policy、estimate、reservation、actual、decision 与 uncertainty。

> **MINIMUM COURSE RECORD / PROPOSAL / NOT AN INVOICE / NOT A TRACE SCHEMA**
>
> ```text
> budget_record:
>   identity: {budget_id, run_id, scope, parent_budget_id, policy_id, policy_version}
>   contract: {dimensions, unit_contracts, hard_or_soft, effective_at, expires_at}
>   estimate: {value_or_range, source, assumptions, observed_at}
>   accounting: {dimension, consumption_id, state, basis, transition_from, transitioned_at}
>   reservation: {reservation_id, amount_or_range, created_at, released_or_replaced_at}
>   incurred_pending: {value_or_range, measurement_ref, completeness, observed_at}
>   actual: {value_or_unknown, source_ref, source_freshness, accounting_basis, observed_at}
>   remaining: {value_or_range, basis: limit-settled-conservative_outstanding, computed_at}
>   clock: {clock_domain_id, host_id, boot_id, checkpoint_segment_id, absolute_deadline, uncertainty_bound, uncertainty_policy}
>   decision: {point, action, route, reason_code, decided_at}
>   uncertainty: {unknown_fields, bounds, stale_after, reconciliation_state}
>   refs: {checkpoint_ref, provider_receipt_refs, authority_ref, trace_ref}
> ```

记录规则比字段名更重要：

- actual 不存在时写 `UNKNOWN` 或 `PENDING_RECONCILIATION`，不复制 estimate；
- 同一 consumption id 的 reservation / incurred-pending / actual 互斥，transition用replace；只有unused/aborted/proven-absent amount可release；
- remaining 可以是 range，但basis固定为limit减settled与conservative outstanding，并保存computed_at与来源新鲜度；
- monotonic stamps 缺兼容clock domain时不相减；改用persisted absolute deadline与bounded uncertainty，无法界定则`UNKNOWN/BLOCKED/STOP`；
- Provider receipt 保留 native identity，通过 ref 关联；
- budget-local reason 只解释资源决定，不把质量、权限或副作用状态吞进来。

`trace_ref` 只是一条接口缝。本文不定义跨 Step、Tool、Provider 与 State transition 的 event schema，不讨论 reconstruction、side-effect re-execution 或跨层 Failure Taxonomy；这些属于 Article 21。Budget record 也不会自动变成 Eval dataset，更不能证明某个降级策略防止了回归；这些属于 Article 22。

**这一节的结论：可审计不是把未知填满，而是让每个 estimate、hold、actual、decision 和未决账项都能回到来源与时点。**

## 10. BuildPilot `BudgetEnvelope`：让缺口先可见

下面把模型落到 BuildPilot 的只读启动性能调查。它不是 Runtime 示例，而是一份可审查的设计草案。

> **CONSTRUCTED COURSE DESIGN / DESIGN ONLY / NOT IMPLEMENTED / NOT RUN / NO FIXED PRICE OR WINDOW**
>
> ```yaml
> budget_envelope:
>   classification: DESIGN_ONLY_NOT_IMPLEMENTED_NOT_RUN
>   scope: RUN/startup-investigation-readonly
>   authority_ref: REQUIRED_FROM_ARTICLE_19_NOT_CREATED
>   token:
>     unit_contract: PROVIDER_MODEL_REQUEST_IDENTITY_REQUIRED
>     estimate: UNKNOWN
>     output_reserve: POLICY_REQUIRED
>     hard_limit: POLICY_REQUIRED
>     actual_receipt_refs: []
>   step:
>     counting_rule: committed_step_v1
>     increment_point: SUCCESSFUL_STEP_COMMIT_EXACTLY_ONCE
>     in_flight_reservations: []
>     retry_rule: EXPLICIT_REQUIRED
>     limit: POLICY_REQUIRED
>     used: NOT_STARTED_DESIGN_PLACEHOLDER
>   cost:
>     currency: REQUIRED
>     estimate: UNKNOWN
>     price_snapshot_ref: REQUIRED_OR_UNKNOWN
>     accounting_rule: LIMIT_MINUS_SETTLED_MINUS_CONSERVATIVE_OUTSTANDING
>     reservations: []
>     incurred_pending: []
>     actual: UNKNOWN
>     remaining: UNKNOWN
>   latency:
>     end_to_end_deadline: REQUIRED_NOT_SET
>     child_timeout_rule: MUST_FIT_REMAINING
>     clock_domain_id: REQUIRED
>     host_id: REQUIRED
>     boot_id: REQUIRED
>     checkpoint_segment_id: REQUIRED
>     uncertainty_policy: FAIL_CLOSED_IF_UNBOUNDED
>     phase_receipts: []
>   routes: [STOP, DEGRADE_OPTIONAL_WORK, REQUEST_APPROVAL, PARTIAL_RESULT]
>   audit_ref: NOT_CREATED
>   trace_ref: ARTICLE_21_SEAM_ONLY
> ```

这份 shape 故意不填 Model、窗口、Token ceiling、单价、金额、timeout 或真实 deadline。`UNKNOWN / REQUIRED / NOT_CREATED / NOT_RUN` 不是未完成的装饰，而是防止设计值被误读成运行事实。

按 fail-closed 顺序走一遍：

1. `authority_ref` 缺失：Budget 不代签 authority，返回 Article 19 边界。
2. Provider/Model/request identity 未定：Token unit 与 estimate 保持 `UNKNOWN`，不能用字符数猜。
3. currency 或 dated `price_snapshot_ref` 缺失：不能合成 cost actual；按 policy reject、请求有界输入或保留 UNKNOWN 路由。
4. 仅在设计条件下 admission PASS：创建 internal reservation candidate，不声称 Provider 已扣费。
5. queue dequeue/resume：加载 persisted Step used/in-flight reservation、Cost outstanding、absolute deadline 与 clock-domain/host/boot/segment identity；same-domain用monotonic delta，跨域用absolute deadline + bounded uncertainty，无法界定则`UNKNOWN/BLOCKED/STOP`。
6. Step/Provider/Tool 前：先验 legal transition、authority、Retry/effect eligibility，再按consumption id reserve四维 incremental allowance；Step尚不增加`used`。
7. receipt / successful Step commit：同一identity做replace。Step reservation转为唯一`used + 1`；Cost reservation转为measured/incurred-pending，只release unused delta；source-qualified actual到达后再替换pending。
8. 任一 hard dimension 将超限：不启动新工作；STOP、REQUEST_APPROVAL 或 PARTIAL_RESULT，只有 optional work 才能 DEGRADE。
9. terminal：保存 Budget record 与 `trace_ref` seam；不伪造 Article 21 event stream，也不评价 Article 22 quality/regression。

这个 walk-through 没有执行 Provider call、queue simulation、cost calculation、deadline test 或 Lab。它的合格结果完全可以是“停止，并把未知留下”。

**这一节的结论：BuildPilot envelope 的价值是暴露缺失 identity、estimate、reservation、actual 与 route，不是展示一套已经工作的预算系统。**

## 11. 一个 Budget 设计通常怎样写坏

| Shortcut | Why wrong | Minimum correction |
|---|---|---|
| `fits_context = within_run_token_budget` | capacity 与 Run policy 不同 | 分开 Model contract、estimate/usage、limit/reserve |
| `character_count = token_actual` | tokenizer、request surface、Provider 不同 | Provider-native count/receipt + uncertainty |
| `max_turns = max_steps everywhere` | AI invocation、super-step、course Step 不同 | 冻结 counting rule/scope/retry/fan-out |
| `step cap reached = task complete` | cap 只限制新工作 | terminal 写 exhausted/incomplete |
| `usage × current public price = actual` | price drift、tier、discount、cache、非 Token 项和账务基础未闭合 | dated snapshot estimate；actual 等 source-qualified record |
| `reserved = spent` | hold 可能 release 或 pending | 保存 reservation lifecycle + reconcile |
| `reservation + pending + actual` | 同一 charge 被重复扣减，或 pending 期间提前释放 | 同一 `charge_id` 在互斥 bucket 间 replace；remaining 扣 settled + outstanding |
| `timeout = deadline = latency budget` | child duration 忽略 earlier elapsed、queue 与 parent | deadline + remaining + child allocation |
| `new process monotonic - old process monotonic` | host、boot 或 clock origin 可能不可比 | 冻结 clock-domain/host/boot/segment；跨域用 absolute deadline + bounded uncertainty 或 fail closed |
| `sum(child durations) = elapsed` | 并行分支会重复计算 | 使用 application-visible DAG critical path |
| `budget remains = retry/action allowed` | authority、effect 与 failure eligibility 独立 | 先过 Article 19 与 Article 11 的 gate |
| `degrade = quality preserved` | 没有 Eval/Regression evidence | 只称 policy route；留给 Article 22 验证 |
| `budget record = complete trace` | 单一 decision record 不拥有跨 Step 重建 | 只留 `trace_ref`；交给 Article 21 |

这些捷径的共同问题，是把一个数字或一个 PASS 传播成其他责任面的保证。Budget 可以拒绝新增消耗，却不能证明动作获权、Retry 安全、结果正确或系统可靠。

**这一节的结论：Budget 最危险的错误不是算错，而是让一个责任面的数字替另一个责任面作决定。**

## 12. Ownership 与验证边界

Budget 横跨 Context、State、Recovery 与 Authority，但不能把相邻主题重新吞掉。

| Owner | Owns | Article 20 consumes | Article 20 does not do |
|---|---|---|---|
| Article 01 | Model API、Messages、Token、Context Window basics | capacity/usage distinction | 不重讲 tokenization 或 Provider API 入门 |
| Article 10 | State Machine、committed Step、legal transition | Step unit seam + enforcement point | 不重定义 State、Guard 或 commit authority |
| Article 11 | Checkpoint/Resume、Retry eligibility、effect uncertainty、Recovery | persist/revalidate budget state | 不用余额批准 Retry，不设计 compensation/exactly-once |
| Article 12 | Context Select/Order/Scope/Fit Budget、Context Receipt | request estimate/revision seam | 不重讲 assembly、pollution 或 Provider-hidden context |
| Article 19 | Permission/Approval/HITL/Sandbox 与 action authority | `authority_ref` + budget-change approval seam | Budget 不授予动作权限，不越过 hard deny |
| Article 21 | cross-step Trace、Replay、Failure Taxonomy | `trace_ref` 与 budget-local reason seam | 不定义 event schema、correlation、reconstruction/re-execution 或跨层 taxonomy |
| Article 22 | Eval、Golden Dataset、Regression | future quality/regression question | 不声称 cap/degrade 提高质量或防回归 |

本篇能建立的上限是：

- `20-C02 CONFIRMED`：Context Window、preflight estimate、Provider response usage 与 Run Token Budget 是不同对象；
- `20-C01/C03/C04/C05 PARTIAL`：现有来源支持四维分离、产品限定计数、cost basis 与 deadline/timeout 的窄边界，统一 taxonomy、phase ledger 与 critical path 仍含课程 synthesis；
- `20-C06/C07/C08/C09 PROPOSAL`：课程提出 lifecycle、route、audit record 与 BuildPilot envelope，并显式保留 falsifier 与 unknown。

本篇不能证明：

- 当前模型窗口、价格、service tier 或通用 timeout 数值；
- 真实 Token/Cost/Latency receipt、reservation、queue observation、critical-path measurement 或 billing read；
- atomic reservation、race freedom、Runtime correctness、成本节省、低时延、任务质量、安全或 production readiness；
- Article 21 的完整 Trace/Replay/Failure Taxonomy；
- Article 22 的 Eval dataset、metric、threshold 或 regression verdict。

冻结事实仍是：Required Lab=`NONE`；Experiments=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。

### Claim Traceability（9 / 9）

| Claim | Ceiling | 正文主落点 | Evidence Cards | 保留的边界 |
|---|---|---|---|---|
| `20-C01` | PARTIAL | 1、6、12 | `20-E01`、`20-E02`、`20-E04`、`20-E05`、`20-E06`、`20-E07` | 四维是课程 taxonomy，不称行业统一模型 |
| `20-C02` | CONFIRMED | 2、6、12 | `20-E01`、`20-E02`、`20-E03` | Provider/Model/request identity；无固定 window 或 universal mapping |
| `20-C03` | PARTIAL | 3、6、11 | `20-E04`、`20-E05`、`20-E10` | pre-Step reserve，successful commit唯一`used + 1`；cap不保证完成或质量 |
| `20-C04` | PARTIAL | 4、6、11 | `20-E07`、`20-E08`、`20-E09`、`20-E10` | 同一charge在reservation/pending/actual间互斥replace；remaining扣settled+outstanding |
| `20-C05` | PARTIAL | 5、6、11 | `20-E06`、`20-E10` | gRPC只支撑deadline/timeout；cross-domain resume是course proposal，unbounded uncertainty fail closed |
| `20-C06` | PROPOSAL | 1、7、10 | `20-E10`、`20-E11` | lifecycle uses single-accounting replace；Budget不授予action/Retry authority |
| `20-C07` | PROPOSAL | 8、10、11 | `20-E01`、`20-E04`、`20-E06`、`20-E10`、`20-E11` | routes 是 policy design，不保证 quality/safety |
| `20-C08` | PROPOSAL | 9、10、12 | `20-E07`、`20-E08`、`20-E09`、`20-E10`、`20-E11` | record含consumption/remaining/clock basis；不是invoice、full Trace或Eval schema |
| `20-C09` | PROPOSAL | 10、12 | `20-E10`、`20-E11` | envelope含single-accounting与clock fail-closed；BuildPilot DESIGN / NOT IMPLEMENTED / NOT RUN |

Coverage=`9 / 9`；Evidence posture=`1 CONFIRMED / 4 PARTIAL / 4 PROPOSAL / 0 BLOCKED`；new core Claim/Card=`NONE`。

## Learning Check

1. Article 19 的 action authority 已 PASS，为什么仍可能拒绝 Run admission？反过来 budget remains 为什么不能授权动作？
2. Context Window、preflight count、response usage 与 Run Token Budget 分别回答什么？
3. 一份可审计 `max_steps` 至少要冻结哪些 counting-rule facts？
4. Cost estimate、reservation、incurred-pending 与 actual 为什么不能互换？`remaining` 按什么 basis 计算？
5. Deadline、timeout、queue time、service time 与 critical path 怎样分开？checkpoint/resume 跨 clock domain 时怎么办？
6. 为什么 Budget check 要出现在 admission、resume、Step/call 前和 receipt 后？
7. hard limit 将被超过时，STOP、DEGRADE、REQUEST_APPROVAL、PARTIAL_RESULT 怎样选？
8. Budget record 为什么允许 actual/remaining 为 `UNKNOWN` 或 range？`trace_ref` 为什么不代表 Trace 已完成？
9. `DEGRADE` 后任务看起来仍完成，为什么本文不能声称质量未下降？
10. BuildPilot `BudgetEnvelope` 为什么必须保留 `DESIGN / NOT IMPLEMENTED / NOT RUN` 与 `UNKNOWN/REQUIRED`？

### 参考答案

1. Authority 与 resource admission 是串联且独立的 gate：前者回答能不能做，后者回答已获权动作是否有资源资格。任一 PASS 都不能替代另一项。
2. 它们依次回答 Model capacity、当前 request estimate、Provider-native actual usage receipt、application Run policy；都要绑定 Provider/Model/request identity。
3. unit、scope、Retry、Tool fan-out/parallel semantics、checkpoint/resume persistence；pre-Step admission只reserve，successful committed Step以同一`step_attempt_id`唯一`used + 1`，abort-before-commit release。到顶只说明exhausted/incomplete。
4. Estimate不扣账；同一`charge_id`从reservation replace为measured/incurred-pending，再由source-qualified actual replace；仅unused delta可release。`remaining = limit - settled_actual - conservative(outstanding)`；缺项保存range/`UNKNOWN`，不合成精确值或提前释放。
5. Deadline是absolute point，timeout是child duration；queue/service需要同一可比clock domain内的边界时间戳；critical path需要应用可见DAG。checkpoint保存clock-domain/host/boot/segment；跨域用absolute deadline+bounded uncertainty，无法界定则`UNKNOWN/BLOCKED/STOP`。这套resume规则是课程设计，不是gRPC保证。
6. request、elapsed、outstanding与used/remaining会持续变化；新增消耗前按identity reserve，receipt后以replace而非重复相加完成commit/pending/release/reconcile；resume必须重验budget与clock identity，不能重置预算。
7. route 来自每维 hard/soft policy；required invariant 或 authority 未满足必须 STOP；只有 optional work 可 DEGRADE；Approval 只请求预算变化；Partial 保留 known/unknown/unverified。
8. future billing、price attribution 与隐藏阶段可能未闭合，诚实 uncertainty 比伪精确更可审计。Budget record 只保存 decision seam，Article 21 才拥有 cross-step Trace/Replay/Failure Taxonomy。
9. Budget route 不是质量证据；需要 Article 22 的 fixed tasks、criteria、Golden Dataset 与 regression comparison。
10. 当前没有 Lab、Provider call、billing read 或 Runtime receipt；这些标签与占位字段用于暴露缺口，防止 schema shape 被误读为真实结果。

## Job Competency Mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| 多维资源建模 | 四维表 + `BudgetVector` | 能写清 unit、estimate/actual、enforcement point 与 non-inference | 课程 taxonomy，非行业统一标准 |
| Provider contract reasoning | Token/Step/Cost 原生合同对照 | 保留 Provider/Model/source identity，不做静默 normalization | moving docs，no runtime |
| Reliable control design | lifecycle + enforcement matrix | 把 authority/Retry eligibility 与 Budget consume 排序，在 resume 重验 | Proposal，no atomic/race guarantee |
| Cost/latency judgment | single-accounting cost ledger + clock-domain latency decomposition | 识别 settled/outstanding remaining basis、source freshness、deadline/timeout 与 cross-domain uncertainty | no fixed price/window，no measured outcome |
| Exhaustion design | route table + partial result | 让 hard/soft、unknown 与 incomplete 可审计 | 不证明 quality/safety |
| Cross-system architecture | ownership matrix | 把 capacity、state、recovery、authority、trace 与 eval 分层 | repository-local ownership |
| Evidence discipline | 9/9 traceability | 让 CONFIRMED/PARTIAL/PROPOSAL 与来源强度一致 | Required Lab NONE；runtime ABSENT |

## 参考资料

以下 moving product contract 均以 **2026-08-26** 的访问结果为本文边界；后续若字段或行为变化，应重新检索并按产品范围改写，不能从本文记忆外推。

- [OpenAI Responses API：Create a model response](https://developers.openai.com/api/reference/cli/resources/responses/methods/create)（OpenAI 产品限定的输出上限、usage、truncation 与 incomplete reason）
- [OpenAI Responses API：Get input token counts](https://developers.openai.com/api/reference/python/resources/responses/subresources/input_tokens/methods/count)（OpenAI 产品限定的 preflight input count operation）
- [Anthropic：Token counting](https://platform.claude.com/docs/en/build-with-claude/token-counting)；[Messages API](https://platform.claude.com/docs/en/api/messages/create)（Anthropic 产品限定的 estimate、output cap 与 native usage）
- [OpenAI Agents SDK Python：Running agents](https://openai.github.io/openai-agents-python/running_agents/)（OpenAI Agents SDK 产品限定的 turn 与 `MaxTurnsExceeded`）；[official `v0.22.0` release snapshot](https://github.com/openai/openai-agents-python/releases/tag/v0.22.0)（released 2026-08-19；不证明 hosted docs 由该 tag 构建）
- [LangGraph Graph API：Recursion limit](https://docs.langchain.com/oss/python/langgraph/graph-api#recursion-limit)（LangGraph 产品限定的 super-step 与 exhaustion）；[official `1.2.11` release snapshot](https://github.com/langchain-ai/langgraph/releases/tag/1.2.11)（released 2026-08-11；不证明 hosted docs 由该 tag 构建）
- [gRPC：Deadlines](https://grpc.io/docs/guides/deadlines/)（gRPC 范围内的 deadline、timeout 与 elapsed propagation）
- [OpenAI Organization Costs API](https://developers.openai.com/api/reference/python/resources/admin/subresources/organization/subresources/usage/methods/costs)（OpenAI 产品限定的历史 cost record surface）
- [Anthropic：Usage and Cost API](https://platform.claude.com/docs/en/manage-claude/usage-cost-api)（Anthropic 产品限定的历史 usage/cost reconciliation surface）
- FOCUS Specification 1.4：[Billed Cost](https://focus.finops.org/docs/specification/v1-4/columns/cost-and-usage/billed-cost/)、[Effective Cost](https://focus.finops.org/docs/specification/v1-4/columns/cost-and-usage/effective-cost/)、[List Cost](https://focus.finops.org/docs/specification/v1-4/columns/cost-and-usage/list-cost/)（version-fixed billing-data vocabulary，不是 Agent reservation/admission protocol）
- Published Articles 01、10、11、12、19（课程内 Token/Context、Step、Checkpoint/Retry、Context Receipt 与 action-authority 边界）

## 最短结论

`预算的工程价值，不是把所有消耗换算成一个数字，而是在每次新增工作前都能回答“还能不能做”，耗尽后仍能说明“为什么停、留下了什么未知”。`
