---
title: "Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery"
slug: "agent-engineering-11-long-running-agent"
date: "2026-08-21"
description: "Long-running Agent 先用 Checkpoint 还原已知与未知，再依据副作用语义、稳定身份、Retry Budget、取消来源与恢复边界选择 Retry、Reconcile、Resume、Ask 或 Stop。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Runtime Engineering"
  - "Recovery Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 120
weight: 3120
---

> **上一篇**：[State Machine 与 Workflow：确定性骨架和 Agent Decision Point]({{< relref "ai-empowerment/agent-engineering-10-state-machine-workflow.md" >}})

> **下一篇**：[Context Engineering：每一个 Step 到底应该看到什么]({{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}})

先看一个长任务里最危险的时间窗口：Agent 调用外部 Tool 注册一条 finding，外部 store 已经写入，但 Runtime 在收到响应前中断。新进程启动后，只看见“上一次调用失败”。

如果它直接再发一次，会出现两种完全不同的结果：外部系统也许识别出同一 action，返回已有记录；也可能把它当成新请求，再创建一条副作用。两条路径表面上都叫“重试”，工程含义却完全不同。

[Article 10]({{< relref "ai-empowerment/agent-engineering-10-state-machine-workflow.md" >}})已经建立 State、合法 Transition、Guard 与 Terminal 的确定性骨架。但 State 只回答“当前提交到哪里”，不会自动回答：哪些 Action 已完成，哪个副作用可能已经发生，取消由谁发起，Retry Budget 还剩多少，下一次执行还能从哪里安全继续。

如果这篇只记一句话，我建议记住：

`Long-running Agent 的 Recovery 不是“再跑一次”，而是先用 Checkpoint 还原已知与未知，再依据副作用语义、稳定身份、Retry Budget、取消来源和恢复边界，决定 Retry、Reconcile、Resume、Compensate、Ask 或 Stop。`

> 证据范围：取消语义依据当前 Microsoft 托管线程取消文档；幂等与响应丢失边界依据 RFC 9110 §9.2.2；Resume / Replay 的产品差异依据 2026-08-21 核对的 LangGraph 与 AWS Step Functions current hosted docs。本地行为只来自 `lab04-fixture-v1`、当前 Windows 10.0.19045 / .NET SDK 10.0.301、single coordinator 与本地 fake store。Checkpoint candidate、Recovery decision 与 partial-result schema 是课程 Proposal；Lab 通过不证明 production、distributed、OS-crash、cross-platform 或 exactly-once。

## 长任务改变的不是时长，而是失败后还剩多少确定事实

短调用最常问的是：这次调用返回了什么？长任务中断后，问题会变成：

```text
what was committed?
what may already have happened?
what is still unknown?
what action is still safe?
```

任务跨越多个 Tool、异步系统或人工等待后，`FAILED`已经不够描述现场。等待超时，不代表底层工作停止；用户请求取消，不代表外部副作用回滚；响应没回来，不代表请求没有生效；还有重试次数，也不代表重试安全。

这正好接上前几篇的责任边界：[Article 06]({{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}})让 Tool Outcome、取消来源和副作用执行 seam 可见，但其 single-process de-dup 没有关闭 crash window；[Article 08]({{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}})让每个 Step 提交 State，并把 `STOPPED` 与 `SUCCEEDED`分开，但没有实现 Recovery；[Article 09]({{< relref "ai-empowerment/agent-engineering-09-planning.md" >}})让 Plan 保持候选身份，却不负责把计划恢复成运行事实。

所以，Long-running 不是简单地给 Loop 增加更长的 timeout。它新增了一层恢复控制面：中断发生后，系统必须先重建事实边界，再决定是否继续。

这也不意味着运行时间一长就必须保存完整 Checkpoint。一个短、确定、无外部副作用、重跑成本极低的 Compile Case，直接从头执行也许更简单。真正改变设计的，是任务是否跨越昂贵工作、不可重复的外部行为、人工等待或不可立即重建的控制事实。Checkpoint有写入、版本、校验和维护成本；只有恢复价值足以覆盖这些成本时，它才是合理边界。本文建立的是“怎样判断能否恢复”，不是“给每个函数套一层持久化”。

更重要的是，恢复问题从一开始就不能只由错误类型驱动。同一个timeout，在副作用之前发生时可能允许重试；在副作用之后、响应之前发生时却只能标记unknown；若同时缺少稳定identity，就连自动查询也没有依据。错误名相同，不代表现场事实相同。Long-running Runtime真正要保存的，是决定下一动作所需的事实，而不是一份更长的异常堆栈。

## 先分类，再谈恢复

Timeout、Cancellation、Retry、Resume、Replay 与 Recovery 经常被压成“失败后继续”，但它们回答的是不同问题。下面是本篇工作定义，不是跨产品统一 taxonomy：

| Term | 本篇回答的问题 | 不自动证明 |
|---|---|---|
| Timeout | 等待或执行是否超过 deadline / duration budget | 底层工作已停止、effect 未发生 |
| Cancellation | 谁发出停止请求，listener 在哪里观察并响应 | 强制终止、rollback、checkpoint 已保存 |
| Retry | 是否再次尝试同一 action intent | 安全、幂等、回到正确 State |
| Resume | 是否从已识别的 durable continuation boundary 继续同一 run | 回到原代码行、不重执行 |
| Replay | 是否依据 history / checkpoint 重演一段控制路径 | 外部副作用安全、等同 Resume |
| Recovery | 分类后选择 Resume、Retry、Reconcile、Compensate、Ask 或 Stop | “再跑一次”、所有系统可回滚 |

[Microsoft 的取消合同](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)把 cancellation描述为 requester 与 listener 的协作模型，它没有承诺 rollback。[LangGraph Functional API](https://docs.langchain.com/oss/python/langgraph/functional-api)说明恢复会从 checkpoint boundary 向前 replay，未完成 task 可能再次执行；[AWS Step Functions Redrive](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html)又保留 successful steps，并重新调度失败 Task。产品规则不同，反而说明 Resume、Replay 与 Recovery 不能互换。

本文采用的最小链路是：

```text
Failure / Cancel / Timeout
    -> classify origin + effect uncertainty
    -> load and validate Checkpoint
    -> Retry / Reconcile / Resume / Compensate / Ask / Stop
    -> continue only within explicit Recovery Boundary
```

分类不是为了给异常换名字，而是为了阻止错误推导。系统如果只保存一个 `CANCELLED`，恢复时就无法知道这是 caller request、timeout policy、listener observation，还是已经确认 work stopped。不同事实必须分开记录。

可以把一次响应丢失沿时间线拆开：Runtime先记录action即将执行，外部store随后提交effect，响应在返回途中丢失，当前进程形成terminal。这里“调用没有得到结果”是known，“effect是否存在”起初是unknown；如果store支持按同一identity查询，查询结果才会把unknown收窄成known。若新进程跳过这一步，直接把terminal映射成Retry，就等于用控制标签覆盖了尚未解决的业务事实。

同样，Recovery不是第七种异常处理分支，而是对已有事实做策略选择。Retry回答“同一intent能否再次投递”，Resume回答“同一run从哪个boundary继续”，Reconcile回答“外部effect现状能否按identity查清”，Compensate回答“是否存在获授权的逆向动作”，Ask与Stop则承认当前自动化边界不足。把这些动作都命名为“resume”，会让审计者看不出系统究竟重放了调用、读取了已有结果，还是只是重新启动了一次任务。

## Checkpoint 不是 State 的磁盘截图

把 `current_state=REGISTERING_FINDING`序列化到 JSON，只保存了一个控制位置。它没有说明 registration 是否已经调用、使用了哪个 action identity、响应是否丢失，也没有说明下一步为什么安全。

对本课程的 Checkpoint candidate，更实用的设计方法不是先堆字段，而是先列恢复问题：

| 恢复问题 | Candidate fields | 缺失后的风险 |
|---|---|---|
| 这是哪个执行？ | schema / fixture version、run / case / goal identity | 把不同 run 拼接 |
| 当前提交到哪里？ | state、revision、last committed sequence | 用 stale continuation 继续 |
| 什么已经完成？ | completed action、intent digest、result / Evidence refs | 重跑已提交行为 |
| 什么仍需完成？ | remaining actions、continuation / next safe action | 无法解释下一步 |
| 什么可能正在发生？ | in-flight action、idempotency key、phase、attempt、result status | 把 unknown 当作未执行 |
| 还允许尝试几次？ | max / used / remaining、last failure class | 无限重试或错误计账 |
| 谁请求停止？ | requested / observed / origin | 混淆 timeout 与 caller cancel |
| 已知和未知是什么？ | partial result + provenance | 把 incomplete 涂成 success |
| artifact 还能否使用？ | integrity digest、version invariant（课程设计要求） | 在损坏状态上继续 |

这份表是课程 Checkpoint candidate。Lab 04只确认其中被冻结case实际覆盖的区分能力，不证明它对生产系统充分；其中integrity digest mismatch的拒绝仍是课程设计要求 / Proposal，LR-01—LR-08没有注入或观察该路径。生产环境还要面对 schema migration、partial disk write、并发 writer、lease、bit rot等问题，本篇没有覆盖。

表里的字段也不是彼此独立的资料栏。`state + revision + last committed sequence`共同标记权威提交位置；`completed_actions`与`remaining_actions`解释这个位置为什么成立；`in_flight_action`则专门保存提交边界外最危险的空窗。若State已经进入action执行阶段，in-flight却为空，字段之间就违反了恢复invariant。Runtime此时不能选择自己喜欢的一份字段继续，而要把整份candidate判为不可恢复。

Completed与history也不能混为一谈。Trace里出现过一次Tool call，只能说明记录中有这个事件；只有result、Evidence引用与State revision按合同提交后，它才有资格进入completed action。反过来，一个completed action即使不需要再次执行，也仍应保留identity和result reference，让后续Resume能够说明为什么跳过它。否则“没有重跑”究竟来自正确continuation，还是因为数据丢失，无法从artifact中区分。

Retry Budget同样属于checkpoint，而不应在每次进程启动时重置。它至少需要保存max、used、remaining和last failure class，并明确第一次投递是否计入attempt。Lab 04冻结的是“第一次投递计入used”的课程规则；别的产品可以采用不同计数合同，但不能让同一run因重启而获得一份全新预算。计数单位不稳定，所谓“最多重试两次”就失去可审计含义。

Checkpoint也不应该保存完整隐藏推理、credential、绝对临时路径，或把 PID、wall-clock这类非确定性字段写进归一化控制事实。自然语言结论若还没有通过 Evidence contract，也不能因为进了 checkpoint 就变成 authoritative State。

最关键的负例是 LR-06：State 已经是 `REGISTERING_FINDING`，却缺少 `in_flight_action`。Runtime没有猜测“既然没结果，那就还没执行”，而是在任何新 fake-store access 前返回 `RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`。start 与 resume 后的 store access count 都是 `1`。这说明“有 State JSON”不等于“有安全恢复依据”；关键不确定性无法还原时，正确行为是 fail closed。

因此，审查Checkpoint时应优先问三个问题：它能否证明最后一次权威提交；它能否指出提交之后哪个action可能正在发生；它能否给出一个由当前证据支持的next safe action。三者少任何一个，文件仍可以用于诊断，却不应被Runtime当成自动恢复许可。

## Retry：先判断资格，再消耗 Budget

`transient=true`和`budget_remaining=1`仍然不足以批准 Retry。自动重送前至少要回答：这是不是同一 action intent？是否有稳定 identity？原 effect 明确未发生、可查询、可幂等重放、可补偿，还是仍为 unknown？

[RFC 9110 §9.2.2](https://www.rfc-editor.org/rfc/rfc9110.html#name-idempotent-methods)给出的窄边界是：通信在读取响应前失败时，只有请求语义已知幂等，或客户端能检测原请求未应用，自动重试才有依据。这里讨论的是 HTTP semantics；它不是通用 idempotency-key store 规范，也没有给出 exactly-once保证。

一个更稳的判断顺序是：

```text
same action intent?
  -> stable action identity / intent digest?
  -> effect is ABSENT_KNOWN / QUERYABLE / IDEMPOTENT /
     COMPENSATABLE / UNKNOWN?
  -> failure class is retryable?
  -> retry budget remains?
  -> RETRY / LOOKUP-RECONCILE / COMPENSATE / ASK / STOP
```

| Effect knowledge | Identity / contract | Budget | 安全候选 |
|---|---|---:|---|
| 明确未 apply | same intent | 有 | Retry |
| 已 apply，可按稳定 identity 查询 | same action / key | 有 | Lookup / reconcile，必要时 same-intent replay |
| 已 apply，有明确补偿合同与权限 | compensation identity | 视 policy | Compensate 后重新评估 |
| 结果 unknown，无法查、无法幂等、无法补偿 | 不足 | 任意 | Ask / Stop |
| permanent / invariant failure | 任意 | 任意 | Stop / Escalate |

Lab 04把这套顺序拆成四条互相约束的轨迹。LR-03在 effect apply 前发生一次 transient failure，第二次尝试成功：attempts=`2`、effect=`1`。LR-07同样是 pre-apply fault，但每次都失败；attempts达到`2`后返回 `RETRY_BUDGET_EXHAUSTED / INCOMPLETE`，effect=`0`。前者说明具备 Retry 资格时仍需计预算，后者说明预算耗尽必须停止。

真正的中心对照是 LR-04与LR-05。两者都先让 store持久化一条 effect，再丢失响应，START都进入 `UNKNOWN_SIDE_EFFECT`。LR-04在 fresh RESUME中沿用同一 action、intent 与 key，通过 `CreateOrGet`取得已有记录，最终effect仍为`1`。LR-05却用新的 delivery identity盲目 append，最终出现两条真实 store record，terminal为 `DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`。

稳定 key在这个 fixed fake store里收窄了重复业务 effect风险，但它不证明日志、计费、网络、存储或旁路行为 exactly once。幂等性是 Retry资格的一部分，不是一次性可靠性盖章。

稳定identity还是必要而非充分条件。Runtime不仅要确认key相同，还要确认action intent与payload digest没有发生语义变化；否则“同key”可能把两个不同请求错误折叠。查询得到已有记录后，也不能立刻把任务写成success：返回记录仍要与当前run、intent和Goal所需事实匹配，再由State提交边界接受。LR-04证明的是fixed store中same identity与same intent的受控行为，不是任意外部API都具备这个合同。

这也解释了为什么Budget检查应放在资格判断之后。如果effect为unknown且没有lookup、幂等或补偿路径，那么budget是十次还是一次都不改变安全结论；继续执行只会把未知风险重复十次。反过来，即使effect明确未发生，permanent failure或invariant failure也不该因为budget仍有余额而被改名成transient。Budget负责限制“已经允许的尝试”，不负责批准尝试。

在真实设计评审中，可以把每个自动Retry要求改写成一张证据卡：same intent由什么字段证明，effect状态由什么来源判定，failure class由谁产生，budget从哪个checkpoint读取，最终decision写到哪条Trace。若其中任何答案只是“框架一般会处理”，就还没有形成可复核的Retry合同。

把开头“创建finding后响应丢失”的场景完整走一遍，会更容易看清顺序。执行前，Runtime先提交包含run、action、intent digest与idempotency key的in-flight checkpoint；外部调用返回前进程中断，恢复时把result status保持为unknown。新进程首先验证checkpoint，再用同一identity查询外部effect。若查到的记录与intent匹配，就把已有result提交进State；若能够明确查到effect不存在，且failure仍属于可重试类别，再检查budget并决定是否重送；若查询本身不可用或结果无法对应当前intent，就保留unknown并Ask / Stop。这个流程没有把“查不到结果”自动翻译成“请求没执行”，也没有让剩余budget越过effect判断。

这里还有一个常被忽略的提交点：外部查询成功不等于整个任务成功。Reconcile只收窄了某个action的effect状态，Runtime仍需验证result、更新completed / remaining、推进State revision，并重新检查Goal与terminal contract。否则系统只是把“重复创建”风险换成了“把错误记录接回当前run”的风险。Recovery解决的是继续执行的合法性，不代替最终事实验证。

## Cancellation 与 Timeout：请求停止不等于已经停下

Cancellation至少包含四类事实：request issued、listener observed、work stopped、side-effect state。Timeout则是另一种 origin。把它们压成一个 exception，恢复时就无法判断底层工作是否仍在进行，也无法判断是否存在 unknown effect。

Lab 04只验证了一个很窄的 cooperative safe boundary。LR-02在 committed evidence之后、side effect之前观察 caller cancellation。START结果是 `CANCELLED / INCOMPLETE / CALLER`，effect / access / attempt=`0 / 0 / 0`；Checkpoint保留已完成 Evidence、剩余 action与`next_safe_action=REGISTER_FINDING`。随后另一个 PID显式RESUME，同一run最终effect=`1`，已提交的 Evidence action没有重跑。

LR-08在同样的 pre-effect范围内形成 deterministic timeout：`TIMED_OUT / INCOMPLETE / TIMEOUT`，effect / access / attempt=`0 / 0 / 0`，Trace中没有 caller-cancel event。两条轨迹说明 origin、observation、terminal 与 continuation 可以分开保存；它们不证明 mid-I/O cancellation、forced kill、timeout race或rollback。

取消后仍落最后安全状态，不是在拒绝用户停止，而是在给下一次执行留下事实：哪些内容已经提交，什么尚未开始，是否存在 in-flight unknown，下一动作还能否安全执行。请求停止与保存停止位置承担不同职责。

在实现上，取消信号可能要经过Run coordinator、当前Step、Tool Runtime与具体handler，但“传播了token”仍不等于每一层都完成停止。控制面至少应分别记录request origin、哪个listener观察到信号、观察时的State / sequence、handler是否已经进入，以及最终terminal。只有额外的执行证据才能把“已请求”升级为“工作已确认停止”。本篇不规定统一的token API，强调的是这些事实不能被一个布尔值吞掉。

Timeout也不应偷偷改写成caller cancellation。它可以触发一条相似的cooperative stop路径，但origin仍要保留，因为后续策略可能不同：caller也许明确不希望自动继续，timeout则可能需要重新评估budget和外部effect。Lab 04只证明两种origin在冻结pre-effect seam中被分开记录，没有证明任何通用策略优先级。

## Resume 与 Recovery：从 Boundary 继续，不是从原代码行复活

Resume开始前，Runtime应先验证 Checkpoint，而不是先调用外部系统再补验证：

```text
load checkpoint
  -> validate schema / integrity / fixture version  # course design requirement
  -> validate current state + in-flight invariant
  -> reconstruct known / unknown / remaining / budget
  -> classify effect state
  -> select explicit recovery action
  -> record decision before a new side effect
```

安全的 pre-effect checkpoint可以 Resume到 next safe action；响应未知但有稳定查询身份时，先 Reconcile再提交结果；有明确补偿合同、身份与权限时，Compensate可以成为候选；identity缺失、integrity失败或没有安全自动路径时，应 Refuse、Ask或Stop。这里的integrity fail closed是课程设计要求 / Proposal；LR-01—LR-08实际观察到的只有LR-06因missing in-flight state invariant在任何新side effect前拒绝，没有执行digest mismatch。Lab 04也没有执行compensation，它只是 canonical Recovery decision surface中的候选，不能写成Observed行为。

Fresh process并不意味着“从原位置继续”。LR-02没有重跑已提交 Evidence action；LR-04却再次进入store调用边界，只是使用同一 identity取得已有effect。这正说明 `Resume != no re-execution`：Replay可以服务于Resume，但boundary之后重算哪些行为，由具体Runtime与产品规则决定。

LR-05反证了blind restart，LR-06反证了缺identity时的猜测恢复。两条负例共同给出一条工程底线：当 in-flight side effect仍为unknown，系统必须优先保留不确定性；无法建立lookup、idempotent replay、compensation或人工判断路径时，Stop不是失败兜底，而是唯一诚实的控制动作。

Recovery Boundary因此应是显式合同，而不是“找最近一份文件继续”。它要同时绑定same run identity、可接受的schema / fixture version、最后权威revision、continuation和恢复前必须通过的invariant。Resume命令只是请求进入这个边界；validator通过后，Runtime才有资格生成新的decision与Trace。把边界验证放在外部effect之后，会让拒绝结果来得太晚：即使最终terminal写得正确，新的副作用已经成为不可撤销历史。

Reconcile、Compensate与Ask也各有authority边界。Reconcile需要外部系统支持按稳定identity读取；Compensate需要明确的逆向语义、身份与执行权限，而且补偿本身也可能失败；Ask需要把known、unknown和可选动作交给人，而不是只显示一个“是否重试”按钮。Lab 04实际观察了same-identity reconcile和fail-closed refusal，没有观察补偿或真人接棒，因此正文只能把后两者保留为课程decision候选。

## Partial Result：把不完整性写成证据合同

长任务没有成功结束时，一句“处理失败，请稍后再试”几乎不能帮助恢复。本篇采用一份课程 partial-result proposal：

| Field | 含义 | 典型 provenance |
|---|---|---|
| known | 已由 committed State / accepted Evidence支持的事实引用 | checkpoint、accepted trace event |
| unknown | 已发生或可能发生、但结果无法判定的action / effect identity | in-flight checkpoint、fake-store view |
| unverified | Goal仍要求、但还没有accepted Evidence的条件 | goal contract、remaining actions |
| next_safe_action | 当前Evidence、authority、idempotency与budget下仍允许的动作，或`NONE` | recovery decision record |

这份schema的关键不是字段名，而是禁止跨栏：unknown不能为了生成顺畅摘要被放进known，unverified不能因为计划中出现过就被省略，next safe action也不能在缺少authority时猜出来。

LR-04把same action / effect identity保留为unknown，并把下一动作限定为same-identity reconcile；LR-06因损坏checkpoint无法还原in-flight identity，next=`NONE`；LR-07保留known evidence，同时把registration、verification与Goal列为unverified，next=`ASK_OR_STOP`；LR-05已经知道出现duplicate，因此必须以FAILED收口，不能挑其中一条effect包装成“至少成功一次”。

`11-C07`的证据上限是 `COURSE-SCHEMA-CONFORMANCE`。这些scripted fixed cases证明字段分离与provenance一致，不证明真实模型生成的内容一定完整，也不把它升级为行业标准。

Provenance在这里不是装饰字段。`known`中的每一项应能回到committed checkpoint或accepted trace；`unknown`应指向具体action / effect identity，而不是一句泛泛的“状态不明”；`unverified`应回到Goal contract中尚未满足的条件；`next_safe_action`则要能解释它为何仍被当前policy、budget和effect semantics允许。没有这种回指，四个列表只是四段自然语言，仍可能被同一错误结论同时污染。

Partial Result还承担人与Runtime之间的接棒合同。操作员看到LR-07时，需要知道已有Evidence无需重做、registration尚未完成、budget已经耗尽，以及自动路径已停止；下一位Runtime读取同一artifact时，也应得到相同控制事实。它不是“尽量多返回一些内容”，而是让未完成状态可以被复核、被继续或被明确拒绝。

## Lab 04：8 / 8 accepted 不等于8次成功

[Lab 04](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/README.md)在执行前由Researcher冻结Hypothesis、Fault、Expected Observable与16条Acceptance Criteria；Lab Engineer随后实现并运行，Observed只来自execution log、process evidence、checkpoint、trace、partial result、fake store与case result。Researcher最后才执行 `Experiment -> Observation -> Interpretation -> Claim Status`。因此：Expected不是Observed，verifier green也不是Claim owner。

| Case | Observed terminal / counts | 教学职责 | 证据上限 |
|---|---|---|---|
| LR-01 baseline | `SUCCEEDED`；effect=`1`；attempt=`1` | baseline成功 | fixed fixture |
| LR-02 cancel + resume | START `CANCELLED / INCOMPLETE` effect=`0`；fresh RESUME后success effect=`1` | safe-boundary cancellation | fixture-scoped |
| LR-03 transient retry | success；attempts=`2`；effect=`1` | pre-apply retry与budget | fixed-store-scoped |
| LR-04 lost response | START unknown effect=`1`；same-identity RESUME后仍`1` | unknown与reconcile | fixed-store-scoped |
| LR-05 unsafe comparator | RESUME后 `DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`；effect=`2` | blind retry负证据 | fixed-store-scoped |
| LR-06 missing in-flight | `RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`；access不增加 | invariant fail closed | proposal conformance |
| LR-07 exhausted | `RETRY_BUDGET_EXHAUSTED / INCOMPLETE`；attempts=`2`；effect=`0` | budget停止与partial result | course-schema + fixed store |
| LR-08 timeout | `TIMED_OUT / INCOMPLETE / TIMEOUT`；attempt=`0`；effect=`0` | Timeout与caller cancel分离 | fixture-scoped |

`8 / 8 accepted`的意思是八个case都符合冻结判据。LR-05必须真实产生duplicate并FAILED，LR-06必须在新side effect前拒绝恢复；如果把它们改成成功，Lab反而不应通过。

这也是Expected、Observed与Interpretation必须分层的原因。Expected在运行前定义什么会支持或反驳Hypothesis；Observed只记录命令、exit code、Trace、Checkpoint、store record和terminal；Interpretation才决定这些观测能把Claim提升到什么范围。若Runtime读取Expected答案，或Reviewer只看最终green summary，实验就会退化成实现对照答案，而不是对thesis施加约束。

八个case的组合职责也不能被一个总PASS覆盖。LR-01提供baseline，LR-02与LR-08拆分取消和超时，LR-03与LR-07约束Retry资格与Budget，LR-04与LR-05对照可协调恢复和盲重送，LR-06只验证missing in-flight state invariant。去掉任一负路径，都可能让同一套“成功恢复”叙事失去反证面。

当前accepted环境为 `.NET SDK 10.0.301 / Host 10.0.9 / Windows 10.0.19045 X64 / China Standard Time`。offline restore、最终Release build、static contract、run A、run B与compare都exit=`0`；build为`0 warnings / 0 errors`。每个suite有8个case、12个fresh Runtime phase process，START / RESUME使用不同PID。run A / B纳入compare的105个normalized files逐字节一致，aggregate SHA-256为`27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a`。network / Provider / credential计数都是`0`。

失败历史同样保留：第一次build因缺少入口得到`CS5001`；第一次static check误扫SDK generated GlobalUsings而false positive；CIM OS edition probe返回`Access denied`。最小patch后的green chain支持当前accepted run，但这些失败不是production recovery evidence。105份artifact一致只说明frozen binary / fixture / normalization下可复现，不说明Agent天然确定、跨平台一致、性能达标或生产可靠。

## 怎样审查一个 Long-running Runtime

坏实现通常不是没有`resume`按钮，而是按钮背后缺少恢复判定输入。可以用下面十个问题做设计评审：

1. timeout、caller cancellation、listener observed与work stopped是否分别记录？
2. Checkpoint能否回答run identity、completed、remaining、in-flight、budget与continuation？
3. Retry前先判断effect semantics，还是只看异常名和剩余次数？
4. 响应丢失后，系统是否保留unknown，而不是默认“未执行”？
5. stable identity用于lookup / same-intent replay，还是每次delivery都生成新身份？
6. Resume前是否先验证schema、integrity、state / in-flight invariant？
7. Replay可能重执行哪些boundary后调用，是否有明确合同？
8. budget耗尽、identity缺失或effect不可判定时，能否Ask / Stop并fail closed？
9. partial result是否分开known、unknown、unverified与next safe action，并保留provenance？
10. negative terminal和first failure是否仍在证据里，还是只留下green success？

这些问题对应的是可检验的工程判断：能否给failure surface分类，能否设计durable control state，能否在副作用不确定时拒绝盲重试，能否用Trace与raw artifact说明恢复边界。能力不需要用职位标签自我宣传；一份能指出“什么时候必须停止”的设计，通常比一个总说“可恢复”的架构图更有说服力。

评审结论也不宜只写“支持Checkpoint / Retry / Resume”。更有用的交付是一张决策矩阵：哪些路径已经在fixed fixture中Observed，哪些只是course proposal，哪些依赖具体产品文档，哪些仍为production non-scope。这样，当外部store、并发模型或OS边界变化时，团队能指出要补哪条Evidence，而不是把旧Lab的PASS继续外推。

## 工程边界：Recovery 是显式合同，不是可靠性总证明

Article 11负责的是long-running recovery control plane：Checkpoint boundary、failure classification、Retry eligibility、cancellation provenance、Resume / Recovery decision、in-flight effect uncertainty与honest partial result。

它没有证明：

- distributed transaction、cross-service atomicity或exactly-once；
- 所有外部副作用都能rollback或compensate；
- OS crash、power loss、partial disk write或crash-consistent checkpoint；
- concurrent caller、lease、lock、race或split-brain；
- 真实HTTP、remote store、Provider、credential、authorization或rate limit；
- production performance、availability、retention或cross-platform determinism；
- 本课程checkpoint schema对所有系统充分；
- model planning quality、decision determinism或context reconstruction quality。

Checkpoint与Memory可以由同一个persistence capability承载，但实现重叠不等于证明职责相同。Article 11只问“执行控制面怎样从已知boundary继续”；跨run长期Memory、Context的选择、排序、压缩、重建与质量，以及knowledge retention和模型决策确定性，属于Article 12及后续。本篇在这里停止，不把Lab 04 PASS扩写成Context或Memory质量证据。

这些限制不是给方案降级，而是恢复合同的一部分。只有把“当前能够自动做什么”“必须查询什么”“何时需要人”“哪些现场仍为unknown”同时写清，系统才可能在失败后保持同一套事实标准。反过来，一句没有范围的“支持断点续跑”，会把最需要审查的副作用窗口、身份合同和拒绝条件全部藏起来。

## Claim-to-section traceability

| Claim | Evidence最终状态 | 正文主落点 | Draft disposition |
|---|---|---|---|
| `11-C01` | `CONFIRMED / FIXTURE-SCOPED` | 分类、Cancellation / Timeout、Lab | 六类control fact分开，不做统一产品状态机 |
| `11-C02` | `CONFIRMED / PROPOSAL-CONFORMANCE` | Checkpoint、LR-06 | candidate只对冻结case必要，不称production充分 |
| `11-C03` | `CONFIRMED / FIXED-STORE-SCOPED` | Retry、LR-03 / 04 / 07 | eligibility先于budget，不外推真实HTTP |
| `11-C04` | `CONFIRMED / FIXED-STORE-SCOPED` | lost response、LR-04 / 05 | reconcile与duplicate并列，禁用exactly-once保证 |
| `11-C05` | `CONFIRMED / FIXTURE-SCOPED` | Cancellation、LR-02 | 只写pre-effect cooperative safe boundary |
| `11-C06` | `CONFIRMED / COURSE-RUNTIME-SCOPED` | Resume / Replay、fresh process | 不承诺从原代码行继续或不重执行 |
| `11-C07` | `CONFIRMED / COURSE-SCHEMA-CONFORMANCE` | Partial Result、LR-02 / 04—08 | 四类字段保持course proposal |
| `11-C08` | missing in-flight=`CONFIRMED / PROPOSAL-CONFORMANCE`；run A/B=`CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED`；integrity mismatch=`PROPOSAL / NOT_OBSERVED` | LR-06、run A / B | 只确认missing in-flight在新side effect前拒绝与105份artifact一致；digest mismatch未执行 |
| `11-C09` | `CONFIRMED / PRODUCT-DOC-SCOPED` | Checkpoint / Memory边界 | 实现可重叠，证明职责不同 |

Coverage：`9 / 9`。Core `BLOCKED=0`。正文没有把fixture、fixed-store、proposal、course-runtime、course-schema、deterministic-fixture或product-doc ceiling升级。

## Learning Check

1. Tool可能已经创建远端资源，但Runtime在读到响应前超时。failure被标为transient且budget还有一次，能否直接Retry？
2. 用户已发出取消，为什么仍要记录listener observed位置、in-flight action和partial result？
3. Checkpoint只有`state=REGISTERING_FINDING`，没有action ID、intent digest或idempotency key，能否猜测“尚未执行”并继续？
4. LR-04 Resume再次进入store调用边界，为什么仍可成为安全候选？这是否证明exactly-once？
5. Lab报告`8 / 8 accepted`，LR-05为什么仍以FAILED结束？
6. Retry Budget耗尽后，只输出“任务未完成，请稍后再试”缺少哪些证据？
7. 一个产品用同一个checkpointer保存thread state并支持fault tolerance，能否推出Checkpoint与Memory完全相同，或必须拆成两套存储？
8. 一个短、确定、无外部副作用且重跑成本极低的Compile Case，是否一定需要本文完整candidate schema？

### 参考思路

1. 不能。先把effect标为unknown，再寻找stable identity、lookup / idempotent contract或compensation；没有安全路径就Ask / Stop。
2. cancellation request不等于work stopped或rollback；这些字段决定下一次是否存在安全Resume boundary。
3. 不能。in-flight uncertainty无法还原，应在新副作用前fail closed，保留unknown并返回`NONE / ASK`。
4. 它使用同一run / action / intent / key查询已有effect，fake store最终仍为1；只证明fixed-store behavior，不证明exactly-once。
5. acceptance表示case符合冻结判据；LR-05必须产生duplicate并FAILED，才能成为blind retry的负证据。
6. 缺少known、unknown、unverified、next safe action及其provenance，容易把未验证条件藏进摘要。
7. 两种结论都过强。实现可以重叠，本篇只按是否回答control position、continuation与in-flight uncertainty区分证明职责。
8. 不一定。先看重跑成本、副作用、人工等待与恢复价值；本篇candidate不是所有任务的强制schema。

## 最短结论

`能恢复的不是“上一次进程”，而是被 Checkpoint、identity、effect semantics 与 partial result共同限定的一段控制事实；边界说不清时，正确动作不是重跑，而是拒绝、询问或停止。`

## 参考资料

- [Microsoft：Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
- [RFC 9110 §9.2.2：Idempotent Methods](https://www.rfc-editor.org/rfc/rfc9110.html#name-idempotent-methods)
- [LangGraph：Functional API](https://docs.langchain.com/oss/python/langgraph/functional-api)
- [LangGraph：Checkpointers](https://docs.langchain.com/oss/python/langgraph/checkpointers)
- [LangGraph：Persistence overview](https://docs.langchain.com/oss/python/langgraph/persistence)
- [AWS Step Functions：Redrive executions](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html)

### 本地证据资产

- [Article 11 Evidence Register](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/articles/11-long-running-agent/evidence.md)
- [Lab 04 Design / Observation / Interpretation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/README.md)
- [Lab 04 Execution Log](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/observations/execution-log.md)
- [Lab 04 Verification Summary](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/observations/verification-summary.json)
- [Lab 04 run-a process evidence](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/observations/process-evidence-run-a.json)
- [Lab 04 run-b process evidence](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/observations/process-evidence-run-b.json)
