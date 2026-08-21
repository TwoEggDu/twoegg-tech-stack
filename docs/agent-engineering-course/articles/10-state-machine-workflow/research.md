# Article 10 Research

## Status

- Gate：`RESEARCH`
- Research Status：`COMPLETE / EVIDENCE_GATE_CANDIDATE`
- Owner：`RESEARCHER`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`
- Retrieved Date：`2026-08-21`
- Core Claim Summary：`6 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`

## Frozen scope reconciliation

本篇是 Article 08 Agent Loop、Article 09 Planning 到 Article 11 Long-running / Checkpoint 的边界篇。Article 08 已把一次有界 Run 内的 `Decision candidate -> Act -> Tool Outcome -> Observation -> authoritative State -> Continue / Stop` 分开；Article 09 已把 Plan 限定为剩余行动候选。本篇只研究：怎样用确定性的 Workflow / State Machine 骨架约束合法推进，并把真正依赖上下文与 Evidence 的选择保留为 Agent Decision Point。

研究结论分三层保存：

- `CONFIRMED`：W3C 规范、官方产品文档或本仓 frozen raw artifact 直接支持的窄化事实；
- `PARTIAL`：产品术语与实现边界互相重叠，只能作 scoped comparison；
- `PROPOSAL`：课程为了教学与工程审查建立的工作定义、transition table 与 Agent Decision Point contract。

本篇不实现 Lab 04，不验证 checkpoint / retry / cancellation / resume / recovery，不做 BPM 教程、不引入 Multi-Agent，也不把 BuildPilot Design 写成已存在 Runtime。

## Answered Research Questions

### RQ-10-01｜Plan、Workflow Definition、Runtime State 与 Trace 分别由谁产生、由谁消费、能证明什么？

**Answer：ANSWERED / OBJECT-CONTRACT + PRODUCT-SCOPED。** 四类对象按 producer、consumer 与证明力分开：

| Object | 最小工作定义 | 主要 producer | 主要 consumer | 能证明 | 不能证明 |
|---|---|---|---|---|---|
| Plan | Goal 与 Current Evidence 下的剩余行动候选 | model、planner role 或 code | Agent Loop、reviewer、workflow gate | 某时刻准备考虑什么 | 已执行、已授权、合法 transition、当前 State |
| Workflow Definition | 预先定义的 stage / state、edge、condition、terminal 与 task composition | developer / orchestration code / declarative definition | workflow runtime、validator、visualizer | 被配置的执行拓扑与候选合法路径 | 某次 execution 已走到哪里、分支已发生、任务已成功 |
| Runtime State | 某个 execution 当前已提交的控制位置与业务数据 | runtime / reducer / state machine processor | guard、router、下一 Step、completion validator | 当前接受了哪些事实、哪些 state active / next candidate | 完整历史、持久化成功、跨中断可恢复 |
| Trace | 对 step、transition、tool、state revision 与 terminal event 的结构化记录 | runtime instrumentation / event history | debugger、auditor、replay / eval consumer | 记录中可追到的已发生事件与关联 | 定义本身、绝对完整性、authoritative current state、可恢复性 |

AWS Step Functions 明确把 ASL JSON definition 与每次 `execution` instance 分开，并为 Standard Workflow 提供 execution event history API；Article 08 / Lab 03 又把 State snapshot 与 Trace 分文件保存。两组材料共同支持“对象可分”，但不要求所有产品使用同一字段或存储拓扑。

### RQ-10-02｜Agent Loop、State Machine 与 Workflow 的最小共同点和关键差异是什么？

**Answer：ANSWERED / PARTIAL + COURSE TAXONOMY。** 三者都可以反复读取当前信息、选择下一动作或 transition，并走向 terminal；差异主要在“谁决定下一步、允许集合在哪里、运行对象多大”。

| Object | 本篇只使用的最小边界 | 下一步主要由谁决定 | 不自动等于 |
|---|---|---|---|
| Agent Loop | Article 08 的有界反馈循环；每个 committed Step 把 Observation 反馈到下一次 Decide | model / decision source 给 candidate，Host gate 与 reducer提交 | Workflow Definition、合法 transition relation、checkpoint |
| State Machine | 对当前 state configuration、enabled transition、guard 与 terminal 的执行语义 | transition selection rules + deterministic program | 整个业务 Workflow、Plan、Trace |
| Workflow | 通过较预定义的步骤、分支与决策点推进任务的应用骨架，可组合 function、task、state machine 与 Agent call | definition + runtime；局部 decision point 可交给 code、rule 或 Agent | 必然是某一种状态机规范、必然包含 Agent、必然可恢复 |

LangGraph current docs把 workflow描述为预定 code path、把 Agent描述为动态决定过程；AWS Step Functions却直接把 state machine称为 workflow，并把每个 state称为 step。这个反例要求正文把上表标为课程工作定义，不能写成行业统一分类。

### RQ-10-03｜State、Stage、Step、Transition、Guard、Invariant 与 Terminal State 怎样建立不重叠的课程工作定义？

**Answer：ANSWERED / MIXED CONFIRMED + PROPOSAL。**

| Term | 本课程工作定义 | Evidence status | 关键边界 |
|---|---|---|---|
| State | 一次 execution 当前已提交的控制位置与相关权威数据；形式状态机可表示为 active state configuration | `CONFIRMED / SCXML-SCOPED + COURSE MAPPING` | 不是 history、Plan 或 checkpoint |
| Stage | Workflow 中用于治理、可视化或责任分组的粗粒度阶段，可包含多个 State / Step | `PROPOSAL` | 不是所有引擎的标准对象；不得与 Step 数量一一对应 |
| Step | 延续 Article 08：一次 committed loop iteration或本地可审计执行单元 | `PROPOSAL / REPOSITORY-LOCAL` | AWS 把 state称为 step，说明产品词义不同；必须写明计数单位 |
| Transition | 从当前 source configuration 到 target configuration 的一次合法状态变化 | `CONFIRMED / SCXML-SCOPED` | 不是 model suggestion、tool call 或 Plan item |
| Guard | transition 是否 enabled 的布尔前置条件；失败意味着该 transition 本次不可提交 | `CONFIRMED / SCXML cond MAPPING` | 不负责生成开放式候选，也不证明副作用已执行 |
| Invariant | 在所有 reachable State 中都必须成立的 predicate；本课程要求 transition commit 前后检查适用 invariant | `PROPOSAL / SOURCE-INFORMED` | 不等于单条 edge 的 guard；也不是“最好如此”的 prompt |
| Terminal State | 使当前 state machine / workflow execution停止继续推进的合法结束状态 | `CONFIRMED / SCXML + AWS-SCOPED` | terminal 不自动等于 success；Article 08 已把 outcome 与 stop reason分开 |

SCXML 1.0 normative text定义 active state configuration、enabled transition、`cond` 与 top-level `final`；Lamport 的 TLA+材料把 invariant表述为在所有 reachable states 成立的 state predicate。Stage、Step 的粒度以及“commit 前后检查”的实现是课程 Proposal。

### RQ-10-04｜哪些 Transition 必须由确定性程序验证，哪些位置才适合 Agent Decision Point？

**Answer：ANSWERED / SOURCE-INFORMED DESIGN PROPOSAL。** 任何会改变 authoritative State 或进入 terminal 的 transition，都必须由程序在提交点验证：

1. source state / revision 是否仍是当前值；
2. target 是否存在且属于 definition 允许的 edge；
3. guard、policy、authorization 与 required Evidence 是否满足；
4. applicable invariant 在提交后是否仍成立；
5. terminal reason / outcome 是否由当前 State 与 completion contract 派生，而不是复制模型自报值。

Agent Decision Point只适合放在“确定性过滤后仍有多个合法候选，且选择依赖非结构化、多源或语境化 Evidence”的位置。Agent读取允许的 State view、Evidence refs与可选 Plan，输出 schema-bounded transition suggestion；runtime随后重新执行相同 legal-transition checks。若条件可由布尔规则、枚举、权限或数据完整性直接判定，就不应要求模型每次重想。

### RQ-10-05｜Workflow 调 Agent、Agent 调受控 Workflow Tool 与 code orchestration 三种控制形态怎样比较，不能推出什么？

**Answer：ANSWERED / PRODUCT-SCOPED + COURSE COMPARISON。**

| Shape | Control owner | Agent freedom | Deterministic boundary | Current official evidence |
|---|---|---|---|---|
| Workflow -> Agent | Workflow先决定何时进入 Agent node / function，Agent只解决该 bounded input | node内部可动态选工具或形成候选 | entry / exit schema、allowed next edge、postcondition仍归 workflow | Microsoft Agent Framework current Functional Workflow docs直接在 `@workflow` 内调用 agent，并可用 `@step`保存结果 |
| Agent -> controlled Workflow Tool | Agent决定是否请求一个窄入口；Tool内部的 stage / transition不暴露给模型自由跳转 | 可选择“是否调用”和合约内参数 | tool schema、policy、workflow内部 guards / invariants | Microsoft current docs允许 workflow包装成 Agent并作为另一 Agent的 tool；OpenAI Agents SDK current docs允许 Python function包装成受 runtime pipeline治理的 FunctionTool |
| Code orchestration | Application code持有 sequence、branch、loop与结果检查 | Agent只在被调用点产生输出或候选 | code本身决定 flow并检查 structured output | OpenAI Agents SDK current docs明确区分 LLM-driven 与 code orchestration，并说明两者可混合 |

三者是本课程按 control owner 划出的比较轴，不是互斥产品类型。来源不能推出哪一种永远更可靠、所有 workflow-as-agent 都自动受同样 tool guard、或使用 Agent 就必须引入 Multi-Agent。

### RQ-10-06｜一条自由 Loop 的坏 Trace 能否证明重复、漏步或非法转移；它不能证明什么通用结论？

**Answer：ANSWERED / FIXTURE-SCOPED + PROPOSAL OVERLAY。** 复用 Lab 03 `AL-04`，不创建新 Lab：

| Sequence | Layer | Classification | Inspectable result |
|---|---|---|---|
| 1 | Initial State | `OBSERVED` | `REQ_LOG / REQ_SOURCE` unresolved，accepted Goal Evidence为空 |
| 2 | Step 1 | `OBSERVED` | 读取 `Unrelated.cs`；Tool成功但 `goal_relevant=false`，`NO_PROGRESS` |
| 3 | Step 2 | `OBSERVED` | 用相同 action fingerprint再次读取同一文件；`repeat_detected=true`，goal-state digest不变 |
| 4 | Stop request | `OBSERVED` | scripted decision请求 `SUCCEEDED`并引用 `EV-FAKE` |
| 5 | Terminal | `OBSERVED` | `STOP_CONTRACT_FAILED / FAILED`；两项 requirement仍 unresolved |
| 6 | Workflow mapping | `PROPOSAL / NOT EXECUTED` | proposed `INTAKE -> LOG_READY -> SOURCE_READY -> VERIFIED -> SUCCEEDED` 中，前两次读取不能越过 `LOG_READY` guard；success transition也应被拒绝 |

raw trace直接证明 fixed fixture中发生了语义重复、无 Goal progress、required Evidence缺失和伪 success被拒绝。它**没有**观察到 Workflow runtime或非法 transition被实际提交；“漏过固定 stage”与“哪个 edge非法”只属于分析 overlay。它也不证明真实模型普遍会重复、状态机能自动修复 planning quality或 production reliability。

### RQ-10-07｜当前 State 与跨中断 Checkpoint 的边界是什么，正文应在哪里停止并交给 Article 11？

**Answer：ANSWERED / PRODUCT-SCOPED STOP LINE。** 当前 State回答“现在接受了什么、下一步可能是什么”；Checkpoint至少还要回答“这份 snapshot是否已持久化、属于哪个 execution/thread、从哪个边界恢复、下一执行单元是什么、哪些结果已持久化且不应重做”。

LangGraph current checkpointer docs中的 `StateSnapshot` 除 values外还包含 `next`、thread / checkpoint identity、metadata、parent与tasks，并明确只从 checkpoint boundary恢复；replay会重新执行 checkpoint之后的 LLM / API等节点。该产品事实足以反驳“把当前 State对象序列化一下就已经解决 recovery”，但不能定义通用 checkpoint schema。

正文只保留一句桥：`State描述当前位置；Checkpoint把可恢复位置、持久化边界与continuation metadata绑定起来。` Retry、cancellation、resume、replay、副作用去重 / compensation与durability tradeoff全部交给Article 11和Lab 04。

### RQ-10-08｜现实产品如何组合 Workflow / Agent / Runtime 职责，哪些反例会推翻“唯一正确架构”的写法？

**Answer：ANSWERED / COUNTER-EVIDENCE CONFIRMED。** 至少有四类直接反证：

1. AWS Step Functions把 state machine直接称为 workflow，并把 state称为 step；课程不能强迫产品术语一致。
2. LangGraph在同一 graph runtime中同时承载 predetermined workflow path与动态 Agent loop；二者不是必须分成两个服务。
3. Microsoft Agent Framework允许 workflow内调用 Agent，也允许把 workflow包装成 Agent-compatible object，甚至作为另一 Agent的 tool；“谁包谁”并非单向。
4. OpenAI Agents SDK明确允许 LLM orchestration与code orchestration混合；code、tool runtime与model decision可以共同承担控制面。

因此正文只能坚持职责问题：谁拥有 legal edge、谁提交 State、谁验证 guard / invariant、谁产生候选；不能把某个类名、graph形态、部署拓扑或单一产品写成唯一正确架构。

## Source Manifest

| ID | Source | Kind | Version / date scope | Retrieved | Used for | Key limitation |
|---|---|---|---|---|---|---|
| `10-S01` | [W3C SCXML 1.0 Recommendation](https://www.w3.org/TR/scxml/) | Normative standard | W3C Recommendation, 2015-09-01 | 2026-08-21 | state configuration、enabled transition、`cond`、final semantics | SCXML含层级 / 并行语义；不等于所有业务workflow实现 |
| `10-S02` | [Using TLC to Check Inductive Invariance](https://lamport.azurewebsites.net/tla/inductive-invariant.pdf) | Primary technical note | Leslie Lamport, 2018-08-23 | 2026-08-21 | invariant与reachable state边界 | 形式化定义；不规定本文runtime API |
| `10-S03` | [AWS Step Functions: state machine concepts](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-statemachines.html) | Official product docs | current service docs | 2026-08-21 | definition、execution instance、state / step术语、data flow | AWS-specific；Standard与Express历史能力不同 |
| `10-S04` | [AWS ASL state machine structure](https://docs.aws.amazon.com/step-functions/latest/dg/statemachine-structure.html) | Official product docs | ASL default version `1.0`; current docs | 2026-08-21 | `StartAt`、`States`、`Next`、`End`、terminal | 产品schema；不定义Agent Decision Point |
| `10-S05` | [AWS GetExecutionHistory](https://docs.aws.amazon.com/step-functions/latest/apireference/API_GetExecutionHistory.html) | Official API reference | current API docs | 2026-08-21 | Trace / execution history为event list | 不支持Express；API history不保证本文Trace完整合同 |
| `10-S06` | [LangGraph: Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents) | Official product docs | current hosted docs; package version未绑定 | 2026-08-21 | predetermined workflow与dynamic agent对照、同runtime组合 | 示例和术语产品特定；未执行compatibility run |
| `10-S07` | [Microsoft Agent Framework: Functional Workflow API](https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/functional) | Official product docs | current page; Python Functional API标`experimental` | 2026-08-21 | native-code workflow、workflow调用Agent、`@step` | experimental且未绑定package build；不得外推稳定API |
| `10-S08` | [Microsoft Agent Framework: Using Workflows as Agents](https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents) | Official product docs | page updated 2026-07-29 | 2026-08-21 | workflow包装为Agent、可作为Agent tool、composition | 产品composition能力；不证明任意tool guard或架构更优 |
| `10-S09` | [OpenAI Agents SDK: Agent orchestration](https://openai.github.io/openai-agents-python/multi_agent/) | Official product docs | current hosted docs; package version未绑定 | 2026-08-21 | LLM-driven / code-driven orchestration可混合 | 页面使用多Agent示例；本篇只取control-owner事实，不展开Multi-Agent |
| `10-S10` | [OpenAI Agents SDK: Tools](https://openai.github.io/openai-agents-python/tools/) | Official product docs | current hosted docs; package version未绑定 | 2026-08-21 | FunctionTool包装Python function及runtime pipeline边界 | 不证明任意workflow已自动获得内部guard / invariant |
| `10-S11` | [LangGraph: Checkpointers](https://docs.langchain.com/oss/python/langgraph/checkpointers) | Official product docs | current hosted docs; `DeltaChannel >=1.2 beta`仅为页面局部 | 2026-08-21 | checkpoint fields、super-step boundary、resume / replay边界 | 产品特定；本文不展开recovery语义 |
| `10-S12` | [Article 08 Published Content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md) | Repository dependency | published 2026-08-20 | 2026-08-21 | Agent Loop、Step、State、terminal边界 | 课程工作定义 + frozen fixture scope |
| `10-S13` | [Article 09 Published Content](../../../../content/ai-empowerment/agent-engineering-09-planning.md) | Repository dependency | published 2026-08-20 | 2026-08-21 | Plan candidate、Workflow non-equivalence | 课程taxonomy，不是行业标准 |
| `10-S14` | [Lab 03 AL-04 raw artifacts](../../labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl) | Frozen repository observation | Windows / .NET deterministic fixture, run-a | 2026-08-21 | repeat、no progress、missing requirements、failed terminal | 无Workflow runtime；illegal-transition mapping是Proposal |

## Counter-evidence and terminology drift

1. **Workflow与State Machine不总是两层。** AWS直接把二者当同义产品对象；正文只能按课程责任面区分。
2. **Stage与Step没有跨生态统一粒度。** AWS把state叫step，LangGraph按super-step计graph tick，Article 08按committed loop iteration计Step。
3. **Agent与Workflow可以双向组合。** Microsoft current docs同时展示workflow调用Agent与workflow-as-agent / agent tool；不得固定成单向嵌套。
4. **Code与LLM control可以混合。** OpenAI official docs明确允许mix and match；不得写成二选一。
5. **Definition、State、Trace、Checkpoint可以同容器但仍不同语义。** 产品可能在同一graph / session object暴露多种字段；同容器不等于同authority。
6. **Guard不等于Invariant。** Guard只决定某条transition本次是否enabled；Invariant要求所有reachable State保持成立。
7. **Terminal不等于Success。** SCXML top-level final表示终止，AWS另有Succeed / Fail / `End: true`，Article 08也把Stopped与Succeeded分开。
8. **AL-04没有观察Workflow。** repeat、no-progress与failed terminal是raw facts；stage skip、illegal edge与guard rejection均是`PROPOSAL / NOT EXECUTED`。
9. **Current State不等于Checkpoint。** LangGraph checkpoint带identity、next、metadata、parent / tasks和durability边界；但这仍不是行业统一schema。

## Candidate Claim Register Input

| Claim ID | Candidate wording | Status candidate | Source mapping | Course use |
|---|---|---|---|---|
| `10-C01` | Plan、Workflow Definition、Runtime State与Trace拥有不同producer、consumer与证明力，不能互相替代 | `CONFIRMED / PRODUCT + REPOSITORY-SCOPED` | S03—S05、S12—S14 | 第一层对象边界 |
| `10-C02` | Agent Loop、State Machine与Workflow共享“当前信息到下一推进”的骨架，但decision owner与scope不同；这是课程taxonomy | `PARTIAL / COURSE-TAXONOMY + PRODUCT-SCOPED` | S01、S03、S06、S12 | 核心对照 |
| `10-C03` | State configuration、Transition、Guard与Terminal可由SCXML / AWS窄化定义，并映射到课程runtime | `CONFIRMED / SPEC + PRODUCT-SCOPED` | S01、S04 | 状态机基础 |
| `10-C04` | Stage、Step与Invariant采用本文工作定义；Stage是粗粒度治理分组，Step沿用Article 08，Invariant是所有reachable State保持的predicate | `PROPOSAL / SOURCE-INFORMED` | S02、S03、S06、S12 | 术语去重 |
| `10-C05` | 改变authoritative State的legal transition由程序验证；Agent只提交candidate suggestion | `PROPOSAL / SOURCE-INFORMED CONTROL DESIGN` | S01、S04、S09、S13 | 中心工程判断 |
| `10-C06` | Workflow调Agent、Agent调受控Workflow Tool与code orchestration在current official products中均可构造且可组合 | `CONFIRMED / CITED-PRODUCTS-SCOPED` | S07—S10 | 三种控制形态 |
| `10-C07` | Agent Decision Point只在多个合法候选仍需语境判断时使用，输入/输出受schema与guard约束 | `PROPOSAL / COURSE INTERFACE DESIGN` | S05、S06、S09、S10 | decision point contract |
| `10-C08` | AL-04直接证明fixed fixture中的repeat、no-progress、missing requirements与fake-success rejection；illegal-transition只属overlay | `CONFIRMED / FIXTURE-SCOPED + PROPOSAL OVERLAY` | S12、S14 | bounded bad trace |
| `10-C09` | Current State不自动具备Checkpoint的持久化identity、continuation与resume boundary | `CONFIRMED / LANGGRAPH-CURRENT-DOCS-SCOPED` | S11 | Article 11 bridge |
| `10-C10` | cited products组合Workflow / Agent / Runtime职责，证据反驳“唯一正确架构” | `CONFIRMED / COUNTER-EVIDENCE PRODUCT-SCOPED` | S03、S06—S10 | engineering boundary |

## Explicit non-scope

- 不做BPM、UML或SCXML教程，不展开层级 / 并行state的完整语义。
- 不把课程State / Stage / Step命名要求写成所有SDK必须遵守的标准。
- 不引入Multi-Agent topology、handoff治理或shared state。
- 不执行新Lab，不修改Lab 03，不声称AL-04运行过Workflow或Agent Decision Point。
- 不展开Checkpoint storage、Retry、Cancellation、Resume、Replay、Recovery、side-effect idempotency或compensation；这些属于Article 11 / Lab 04。
- 不把Microsoft experimental Functional Workflow API写成stable package contract。
- 不把current hosted docs绑定到未逐项核对的package版本。
- 不读取DSH源码，不实现或预演BuildPilot Runtime。

## Return-to-research conditions

出现以下任一项必须返回`RESEARCH`：

1. Draft要把课程taxonomy写成行业唯一State Machine / Workflow架构；
2. 要声称model suggestion本身就是legal transition或authoritative State update；
3. 要把Stage、Step、State或super-step跨产品等同；
4. 要声称AL-04观察了illegal workflow transition、自动修复或production behavior；
5. 要把current State serialization直接写成Checkpoint / recovery已解决；
6. 要展开Article 11的retry、cancellation、resume、replay或side-effect语义；
7. 要给Microsoft experimental API、OpenAI / LangGraph current docs补上未核验package-version保证。

## Research Gate Checklist

- [x] 8个Frozen Research Questions均已answered / narrowed；无未记录blocked question。
- [x] Source Manifest记录title、URL、date / version scope、retrieved date与limitation。
- [x] Plan、Workflow Definition、Runtime State与Trace已分离。
- [x] Agent Loop、State Machine、Workflow、State、Stage、Step、Transition、Guard、Invariant、Terminal与Checkpoint已逐项分离。
- [x] 至少三种control-owner形态有current official product evidence，且未写成互斥标准。
- [x] AL-04严格区分`OBSERVED`与`PROPOSAL / NOT EXECUTED`。
- [x] Counter-evidence覆盖产品术语重叠、双向composition与code / LLM混合控制。
- [x] Article 11 / Lab 04 stop line保持冻结。
- [x] 没有`BLOCKED` Claim或必须新增Lab才能成立的核心行为结论。

## Researcher recommendation

`PASS_RECOMMENDED -> EVIDENCE_GATE -> OUTLINE`。核心source / product / fixture事实均可在窄scope内使用；`10-C02`保持`PARTIAL`，`10-C04 / C05 / C07`保持`PROPOSAL`。后续Author必须把“model suggestion”和“legal transition”画成两层，不得把AL-04 overlay写成observed workflow，不得跨过Article 11 stop line。
