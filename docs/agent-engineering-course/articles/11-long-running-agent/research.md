# Article 11 Research｜Long-running Agent

## Status

- Gate：`EVIDENCE_GATE`
- Research：`COMPLETE`
- Preliminary Evidence：`COMPLETE`
- Lab 04 Design：`FROZEN / IMPLEMENTED / EXECUTED`
- Lab 04 Observation：`COMPLETE / PASS / LR-01—LR-08 OBSERVED`
- Evidence Merge：`COMPLETE`
- Evidence Gate：`PASS`
- Next allowed Gate：`OUTLINE`
- Required Lab：`Lab 04 State Machine + Checkpoint`
- Lab Card：`docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/README.md`
- Retrieved scope：`2026-08-21`（Asia/Shanghai）

> 本文件已合并 Lab 04 的真实Observation。`PASS`只表示`lab04-fixture-v1` / 当前Windows + .NET Host的冻结行为与证据合同一致；不把verifier green、本地文件或named interruption升级为production、distributed或OS-crash事实。

## Product and version scope

1. **课程抽象**：Checkpoint、Retry Budget、Recovery Boundary、Partial Result schema 与 Lab 04 State Machine 都是课程工作定义或设计 Proposal，不是行业统一接口。
2. **.NET**：取消事实只按 Microsoft 当前托管线程取消文档解释为 cooperative cancellation；Lab 目标是 `C# / net10.0 / .NET 10 SDK`，实际 SDK、Host、Runtime、OS 与 timezone 必须由 Lab Engineer 在执行时记录。本轮没有运行 `dotnet`。
3. **HTTP**：幂等与通信失败重试只按 RFC 9110 §9.2.2 的 HTTP method semantics 使用，不把它扩写成通用 exactly-once 或 idempotency-key store 协议。
4. **LangGraph**：只引用 2026-08-21 retrieved 的 current hosted Python docs；没有固定 package version、tag 或 source commit。其 checkpoint、task-result restore、replay 与 side-effect 规则只证明该产品当前文档范围。
5. **AWS Step Functions**：只引用 current Standard Workflow redrive 文档；14-day eligibility、same input / definition、successful-state preservation 与 failed Task rerun都是产品规则，不是通用 Recovery 定义。
6. **本地 Lab**：只允许 BCL-only、no-provider、no-network、no-credential、single-host fake external store。它不能证明真实远端服务、分布式事务、并发竞争、进程强杀、磁盘断电或生产可靠性。

## Research Questions and answers

### RQ-01｜Timeout、Cancellation、Retry、Resume 与 Recovery 怎样区分？

| Term | 本篇工作边界 | 不自动证明 |
|---|---|---|
| Timeout | 等待或执行超过某个 deadline / duration budget 后形成的控制事实 | 底层工作已经停止、外部副作用未发生 |
| Cancellation | requester 发出停止请求，listener 在 cooperative boundary 观察并响应 | 强制终止、回滚、成功保存 checkpoint |
| Retry | 在明确 policy、identity 与 budget 下再次尝试同一 action intent | 安全、幂等、恢复到正确 State |
| Resume | 从一个已识别的 durable continuation boundary 继续同一 run | 精确回到原代码行、不会重执行、等同 replay |
| Replay | 根据历史或 checkpoint 重演一段控制路径；哪些步骤重算由具体产品定义 | 外部副作用安全、等同 resume |
| Recovery | 失败分类后选择 Resume、Retry、Compensate、Ask 或 Stop，并保存不确定性 | “再跑一次”、所有外部系统可回滚 |

Microsoft 的取消合同直接支持“请求与响应分离、取消是协作式”的窄结论。LangGraph current docs明确说 Functional API resume 回到 checkpoint boundary并replay forward，而不是回到同一代码行；AWS redrive又采用自己的unsuccessful-step规则。这些反例共同说明 Resume / Replay / Recovery不能写成跨产品同义词。

### RQ-02｜哪些内容应进入 Checkpoint？

Lab 04冻结的最小checkpoint candidate包含：

- schema / fixture version、run / case identity；
- authoritative State、state revision、last committed sequence；
- completed action identity、intent digest、result / Evidence reference；
- remaining action与continuation / next safe action；
- in-flight action identity、idempotency key、phase与不确定状态；
- retry budget（max、used、remaining）与last failure classification；
- cancellation requested / observed / origin；
- known / unknown / unverified / next safe action partial result；
- payload integrity digest。

不进入checkpoint的内容：完整隐藏推理 / CoT、环境凭证、绝对临时路径、wall-clock / PID等非确定性字段、未通过Evidence contract的自然语言结论。Checkpoint保存“可恢复控制面”，不等于长期Memory；LangGraph current docs甚至把checkpointer的thread-scoped graph state与cross-thread Store明确区分，但这仍只是该产品的术语边界。

这份字段表是`PROPOSAL`。Lab只能验证fixed fixture是否因保留或缺失in-flight identity而走到冻结结果，不能证明它是production checkpoint的充分schema。

### RQ-03｜幂等性为什么决定 Retry？

RFC 9110 §9.2.2的窄结论是：当通信在读取响应前失败时，只有请求语义已知幂等，或客户端能检测原请求未应用，才有自动重试依据；非幂等请求不应盲目自动重试。它还提醒：重复请求的“intended effect”可相同，但每次日志等旁路副作用仍可不同。

因此本篇把Retry判定拆成：

```text
same action intent?
  -> stable action identity / intent digest?
  -> side effect absent, queryable, idempotent, compensatable, or unknown?
  -> retry class transient or permanent?
  -> retry budget remaining?
  -> RETRY / LOOKUP / COMPENSATE / ASK / STOP
```

Lab 04需要同时保留一个受控的幂等正例和一个non-idempotent unsafe comparator。前者若同一key被再次送达，fake external store只能有一条effect；后者在“apply succeeded but response lost”后盲目重试，应在本地fake store中暴露duplicate risk。两者在运行前均为`Expected`，不是Observation。

### RQ-04｜外部副作用发生但响应丢失时，能恢复什么？

可恢复的是**诚实的控制状态**，不一定是原调用的成功响应：

- 可以保留action identity、intent digest、idempotency key、调用已开始和响应未知；
- 若外部系统支持按identity查询或幂等`CreateOrGet`，可以lookup / replay same intent，再把已存在结果提交进State；
- 若无法查询、无法幂等、无法补偿，就只能报告`UNKNOWN_SIDE_EFFECT`，停止自动Retry并请求人工判断；
- 不得从timeout、lost response或缺少result推断“副作用没有发生”。

RFC 9110的通信失败例子与LangGraph的re-execution警告共同支撑这个风险边界。Lab 04才负责验证fixed fake store的具体行为。

### RQ-05｜取消后怎样恢复？

取消必须先形成可审计事实：谁请求、在哪个state / sequence被观察、是否已有in-flight action、最后一个committed checkpoint在哪里。只有在没有unknown side effect，或unknown已有安全reconciliation策略时，才能从continuation boundary继续。

Lab 04的取消case冻结在side effect **开始前**的安全checkpoint：首次进程观察caller cancellation，保存`CANCELLED / INCOMPLETE`与`next_safe_action=REGISTER_FINDING`；第二个fresh process显式resume同一run。这个case不证明取消mid-I/O，也不证明强制终止。

### RQ-06｜Checkpoint缺少 in-flight action 会怎样？

若checkpoint state表示action execution已进入，但缺少action ID、intent digest或idempotency identity，Runtime无法区分“从未执行”“已执行但响应丢失”“应当查询”与“可以重试”。Lab 04冻结一个损坏checkpoint负例：resume validator必须在调用fake external store前fail closed，输出`RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`，不得伪成功或猜测Retry。

### RQ-07｜Long-running Task 怎样输出 partial result？

本篇采用课程Proposal：

```text
known       = 已由 committed State / accepted Evidence 支持的事实引用
unknown     = 发生过但结果无法确定的 action / side-effect identity
unverified  = Goal 仍要求、但尚无 accepted Evidence 的条件
next_safe_action = 当前证据、权限、幂等与 budget 下仍允许的唯一下一动作，或 NONE
```

`partial result`不能把unknown压成failed，也不能把未验证项写成known。Lab 04会在cancellation、retry-budget exhausted、unknown side effect与invalid checkpoint路径中验证结构和provenance；执行前该行为Claim为`BLOCKED`。

### RQ-08｜怎样证明 fresh-process reproducibility？

Lab Engineer必须用独立OS processes执行start / resume，而不是在一个进程里重建对象假装restart；formal run A / B还要使用不同Lab-owned temp roots，比较normalized checkpoint、trace、partial-result与fake-store artifacts。normalized artifact不得包含时间、PID、绝对路径、随机ID。byte-identical只能证明fixed fixture / binary / normalization可复现，不证明真实模型、网络或生产系统确定性。

## Claim Register

| Claim ID | Claim | Evidence Status | Lab Dependency | Scope / Caveat |
|---|---|---|---|---|
| `11-C01` | Timeout、Cancellation、Retry、Resume、Replay与Recovery是不同控制事实，不能由单一`FAILED/CANCELLED`互相推出。 | `CONFIRMED / FIXTURE-SCOPED` | `SATISFIED` | LR-02 / 03 / 08分别origin、decision与terminal；Replay与Recovery仍由official product docs限定各自范围。 |
| `11-C02` | 可恢复checkpoint需绑定durable run identity、authoritative State / revision、continuation、completed actions / Evidence、budget与in-flight action identity；只序列化current State不足以决定安全恢复。 | `CONFIRMED / PROPOSAL-CONFORMANCE` | `SATISFIED` | LR-02 / 04按candidate schema恢复，LR-06缺失in-flight后fail closed；不证明schema对production充分。 |
| `11-C03` | Retry只有在原请求未应用可判定，或同一intent可幂等重放时才具备自动执行依据；Retry Budget不能替代幂等判断。 | `CONFIRMED / FIXED-STORE-SCOPED` | `SATISFIED` | LR-03在apply前重试后effect=1；LR-04 same identity恢复后effect=1；LR-07 budget耗尽即停止。 |
| `11-C04` | side effect已应用而响应丢失会形成`UNKNOWN_SIDE_EFFECT`窗口；盲目重试可能重复副作用，stable identity + lookup / idempotent create可收窄风险。 | `CONFIRMED / FIXED-STORE-SCOPED` | `SATISFIED` | LR-04 controlled path effect保持1；LR-05 unsafe comparator以两条真实store record结束为FAILED，是negative evidence。 |
| `11-C05` | cancellation request不是工作已停止或副作用已回滚；只有在显式安全boundary与可判定in-flight状态下才可resume。 | `CONFIRMED / FIXTURE-SCOPED` | `SATISFIED` | LR-02只观测pre-effect cooperative cancel + fresh resume；不证明mid-I/O、强制终止或rollback。 |
| `11-C06` | Resume从产品或课程定义的checkpoint boundary继续；Replay可能重执行checkpoint后的调用，因此`Resume != Replay`且两者都不自动安全。 | `CONFIRMED / COURSE-RUNTIME-SCOPED` | `SATISFIED` | LR-02 / 04的START、RESUME是不同PID并以checkpoint继续；跨产品语义仍以LangGraph / AWS为反例。 |
| `11-C07` | partial result必须分别表达known / unknown / unverified / next safe action，不能把中断伪装成success。 | `CONFIRMED / COURSE-SCHEMA-CONFORMANCE` | `SATISFIED` | LR-02、04—08的terminal artifact保留provenance与不确定性；只证明course schema一致性。 |
| `11-C08` | LR-06缺少required in-flight identity时，Runtime在任何新side effect前拒绝恢复；相同fixture的fresh-process normalized artifacts可复现。Checkpoint integrity mismatch也必须在新side effect前fail closed，但这是课程设计要求 / Proposal。 | `CONFIRMED / PROPOSAL-CONFORMANCE`（missing in-flight）；`CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED`（run A/B）；`PROPOSAL / NOT_OBSERVED`（integrity mismatch） | missing-in-flight与run A/B：`SATISFIED`；integrity mismatch：`NOT_OBSERVED` | LR-06拒绝前后store access仍为1；run A/B的105个normalized files byte-identical。LR-01—LR-08没有注入digest mismatch，不声称观察到integrity拒绝；不证明crash consistency或跨平台可复现。 |
| `11-C09` | Checkpoint与Memory可能由同一产品persistence layer承载，但证明职责不同；checkpoint presence不自动证明恢复安全。 | `CONFIRMED / PRODUCT-DOC-SCOPED` | `NOT_REQUIRED` | LangGraph current docs同时把checkpointer用于memory与fault tolerance；课程只区分证明职责，不定义行业互斥taxonomy。 |

## Source Manifest

| Source ID | Source | Type | Version / Date / Locator | Supports | Does Not Prove |
|---|---|---|---|---|---|
| `11-S01` | [Microsoft Learn: Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) | Official .NET docs | current hosted docs；updated`2026-03-17`；retrieved`2026-08-21`；overview + Listening and Responding | cancellation是requester / listener cooperative model；listener决定怎样及时停止 | 任意handler必然停止、rollback、checkpoint或resume |
| `11-S02` | [RFC 9110: HTTP Semantics §9.2.2](https://www.rfc-editor.org/rfc/rfc9110.html#name-idempotent-methods) | IETF Standards Track RFC | RFC 9110；June 2022；retrieved`2026-08-21` | idempotent intended effect；lost response后为何可重试幂等请求；非幂等请求不应盲重试 | 通用idempotency-key schema、exactly-once、Lab行为 |
| `11-S03` | [LangGraph Functional API](https://docs.langchain.com/oss/python/langgraph/functional-api) | Official product docs | current Python docs；retrieved`2026-08-21`；Tasks / Durable execution / Idempotency | task result persistence、checkpoint boundary replay、side effect重执行风险、idempotency建议 | package-version-pinned contract、行业统一resume或checkpoint schema |
| `11-S04` | [LangGraph Checkpointers](https://docs.langchain.com/oss/python/langgraph/checkpointers) | Official product docs | current Python docs；retrieved`2026-08-21`；Why use checkpointers > Pending writes；Core concepts > Threads / Checkpoints / Super-steps；Get and update state > StateSnapshot fields / Replay；Durability modes | thread / checkpoint identity、StateSnapshot字段、super-step boundary、pending writes、replay后的调用重执行与durability mode取舍 | 本课程schema、所有节点不重跑、任一mode的production durability guarantee |
| `11-S05` | [LangGraph Persistence overview](https://docs.langchain.com/oss/python/langgraph/persistence) | Official product docs | current hosted docs；retrieved`2026-08-21`；Persistence；Checkpointer vs. store | checkpointer用于thread-scoped state / short-term memory与fault tolerance，Store用于cross-thread long-term data | `Checkpoint != Memory`是行业统一术语；checkpoint字段、pending writes、replay或durability mode细节 |
| `11-S06` | [AWS Step Functions: Redrive executions](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html) | Official product docs | current docs；retrieved`2026-08-21`；Standard Workflows | redrive从unsuccessful step继续、保留successful step结果 / history、Task可重跑、same definition / input | 其他workflow产品的resume规则、side-effect幂等、无限期恢复 |
| `11-S07` | [Microsoft Learn: Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) | Official .NET docs | current docs；retrieved`2026-08-21`；`net10.0` | Lab target framework locator | 当前机器SDK patch、Lab build / run成功 |
| `11-S08` | Article 06 Published Content | Repository dependency | `content/ai-empowerment/agent-engineering-06-tool-runtime.md` | cancellation origin、single-process idempotency seam、unknown side-effect gap | durable retry / recovery已实现 |
| `11-S09` | Article 08 / 09 Published Content | Repository dependencies | `agent-engineering-08-agent-loop.md` / `09-planning.md` | committed Step、State / terminal、Plan candidate与Verified State分离 | checkpoint / resume / recovery行为 |
| `11-S10` | Article 10 Published Content | Repository dependency | `content/ai-empowerment/agent-engineering-10-state-machine-workflow.md` | legal transition、authoritative State与Article 11 checkpoint bridge | current State序列化即支持recovery |

## Counter-evidence and terminology audit

### Retry != Recovery

- **Counter-evidence searched**：RFC 9110允许幂等HTTP request在lost response后重试；AWS redrive重跑failed Task；LangGraph resume可能重执行未完成task。
- **Result**：Retry可以是Recovery策略之一，但产品恢复还可能保留successful steps、lookup existing result、compensate、ask或stop。二者不能互换。
- **Wording boundary**：只写“Recovery分类后可能选择Retry”，不写“Recovery就是Retry”。

### Timeout != Cancellation

- **Counter-evidence searched**：.NET cancellation是协作式request / listener合同；AWS把timed out execution作为可redrive失败类别之一。
- **Result**：timeout可以触发cancellation request或terminal policy，但不能证明listener观察、work停止或副作用未发生。
- **Wording boundary**：Trace必须分别保存origin / reason，不压成一个`CANCELLED`。

### Resume != Replay

- **Counter-evidence searched**：LangGraph Functional API明确resume会回到checkpoint boundary并replay forward；AWS redrive保留successful steps、重新调度failed Task；两个产品边界不同。
- **Result**：Replay可成为Resume实现机制，但哪些步骤跳过 / 重跑由产品和checkpoint粒度定义。
- **Wording boundary**：不承诺“从原代码行继续”，不承诺“resume不重执行任何调用”。

### Checkpoint != Memory

- **Counter-evidence searched**：LangGraph current docs把checkpointer同时描述为thread-scoped memory与fault-tolerance机制，并把cross-thread long-term data放到Store。
- **Result**：同一persistence capability可以同时服务memory与recovery；因此不能靠存储组件名字区分。课程只按证明职责区分：Checkpoint必须回答control position、continuation与in-flight action，Memory不自动回答这些问题。
- **Wording boundary**：不建立行业互斥taxonomy。

### Idempotency != exactly-once

- **Counter-evidence searched**：RFC 9110说明相同intended effect仍允许server记录每次请求等额外side effects；Article 06 Lab只验证single-process de-dup。
- **Result**：stable key、lookup和same-intent replay可以降低duplicate business effect，却不证明network、storage、trace或所有旁路行为exactly once。
- **Wording boundary**：正文禁用`exactly-once`保证语态。

### External side effect may be irreversible

- **Counter-evidence searched**：RFC 9110的lost-response场景明确允许原请求可能已成功；LangGraph docs警告resume可重复执行side effect；AWS failed Task会被重新调度。
- **Result**：没有query / idempotency / compensation contract时，正确输出是unknown + ask / stop，而不是伪造rollback或success。

## Lab 04 evidence merge summary

### Experiment

- `lab04-fixture-v1`的LR-01—LR-08在run A / B两个独立Lab-owned root中执行；每个START / RESUME phase均为fresh Runtime child process。
- 冻结fault覆盖caller cancellation、transient before apply、apply then lose response、unsafe blind redelivery、missing in-flight action、retry exhausted与timeout before apply。
- Release build、static contract、formal suites与normalized compare均有命令、exit code、raw trace、checkpoint、partial result与fake-store artifact。

### Observation

- LR-02在side effect前观测`CALLER`取消，effect=`0`；fresh RESUME后effect=`1`，trace中`EVIDENCE_ACTION_COMPLETED`未重跑。
- LR-03的第1次pre-apply失败后在budget内retry，attempts=`2`、effect=`1`；LR-07在attempts=`2`耗尽budget后停止，effect=`0`。
- LR-04在store flush后记录`RESULT_UNKNOWN`，fresh RESUME以same action / intent / key得到existing effect，final effect仍=`1`。
- LR-05 unsafe comparator以两个delivery identity产生两条真实fake-store record，并以`DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`停止。
- LR-06在`REGISTERING_FINDING`缺少in-flight action时返回`RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`，resume前后store access count仍=`1`。
- LR-02、04—08的non-success partial result分开known / unknown / unverified / next safe action并带provenance。
- run A / B各有106个文件；suite sentinel `.lab04-run-root`按设计不同，纳入normalized compare的105个artifact逐字节相同，aggregate SHA-256=`27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a`。
- 执行保留了first build `CS5001`、first static-contract generated-source false positive与CIM OS probe `Access denied`；最终接受运行为Release build `0 warnings / 0 errors`、static contract PASS、network / Provider / credential counters全为`0`。

### Evidence interpretation

这些Observation支持课程Runtime在fixed fixture下根据origin、budget、effect semantics、stable identity、checkpoint invariant与partial-result contract选择Stop / Retry / Reconcile / Resume / Refuse。LR-05是必要的negative evidence：它不是“运行成功”，而是用真实duplicate record反证blind retry不安全。

证据上限不变：named interruption不是OS crash / power loss / partial disk write；fake store与checkpoint是同机不同文件，不是distributed transaction；single coordinator不覆盖race / lease / split-brain；no network / Provider / credential不验证真实HTTP、远程服务、模型或授权；CIM probe失败意味着OS edition没有CIM证据，仅由`dotnet --info` / `RuntimeInformation`支持Windows build / architecture。

### Claim status and gate

- Claim traceability：`9 / 9`。
- Core `BLOCKED`：`0`。
- C01—C08：按Claim Register升级到fixture / proposal / course-runtime / course-schema的限定上限。
- C09：升级到`CONFIRMED / PRODUCT-DOC-SCOPED`，不需Lab扩张为行业taxonomy。
- Evidence Gate：`PASS`。
- Next allowed Gate：`OUTLINE`。

## Article 12 stop line

Article 11只负责长任务的恢复控制面：checkpoint boundary、retry / cancellation / resume / recovery decision、in-flight side-effect uncertainty与partial result。它不吞掉Article 12的Memory / Context问题；跨run长期记忆、context选择/重建/质量与模型决策确定性不由checkpoint presence或Lab 04 PASS推出。
