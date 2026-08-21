---
title: "State Machine 与 Workflow：确定性骨架和 Agent Decision Point"
slug: "agent-engineering-10-state-machine-workflow"
date: "2026-08-21"
description: "State Machine / Workflow 把合法状态推进交给确定性程序，Agent 只在多个合法候选仍需语境判断时给出受约束的 suggestion。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Workflow Engineering"
  - "Runtime Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 110
weight: 3110
---

> **上一篇**：[Planning：Agent 为什么需要计划，又为什么不能迷信计划]({{< relref "ai-empowerment/agent-engineering-09-planning.md" >}})

先看一个工程里很常见的场景：团队让 Agent 调查一次构建失败。前一篇已经让 Plan 写得很清楚：解析日志、读取匹配源码、验证诊断、给出结论。Agent Loop 也能逐步执行：模型提出动作，Tool 返回结果，Host 把 Observation 提交到 State。

但系统仍可能在两个地方失真。它可能连续读取两次无关文件，却因为 history 变长而显得“在工作”；也可能在日志和源码 Evidence 都缺失时，直接输出 `SUCCEEDED`。如果运行时只相信模型的下一步建议，或者只相信 Plan item 的文字顺序，那么“计划了下一步”和“合法进入下一状态”就会被混成一件事。

这篇要补的不是一个更聪明的 prompt，而是一层不由模型自由改写的确定性骨架：哪些状态存在，哪些边允许走，哪些 Guard 必须满足，什么样的 Terminal 才能提交。Agent 仍然可以判断复杂语境；但它给出的只是 suggestion，不是 legal transition。

如果这篇只记一句话，我建议记住：

`State Machine / Workflow 的价值不是替 Agent 思考，而是把合法 State、Transition、Guard、Invariant 与 Terminal 交给确定性程序；Agent 只在多个合法候选仍需上下文判断时输出受 Schema 约束的 suggestion，最终能否推进仍由 Runtime 验证并提交。`

> 证据范围：本文依据 2026-08-21 核对的 W3C SCXML、AWS Step Functions、LangGraph、Microsoft Agent Framework、OpenAI Agents SDK 与 LangGraph Checkpointers current hosted docs，以及本仓 Article 08 / 09 Published Content 和 Lab 03 AL-04 frozen raw artifacts。`Agent Loop / State Machine / Workflow` 是课程比较轴，`10-C02` 保持 `PARTIAL`。`Stage / Step / Invariant`、legal transition commit protocol 与 Agent Decision Point 均为课程 `PROPOSAL`。Article 10 没有运行新的 Workflow / State Machine；AL-04 的 State Machine table 是 `PROPOSAL / NOT EXECUTED` overlay。

## 为什么自由 Loop 和 Plan 还不够

Article 08 回答的是：当前 State 下怎样安全推进一个 committed Step，并把 `Tool Outcome -> Observation -> State` 分开。Article 09 回答的是：跨多个 Step 时，Plan 怎样表达剩余行动候选，又为什么不能冒充执行事实。

但长任务里还有第三个问题：有些顺序不该每次重新想。构建失败调查可以允许 Agent 判断“下一步该读哪个文件”，但不能允许它在没有日志 Evidence 时进入 `LOG_READY`，也不能允许它在 required Evidence 缺失时提交 `SUCCEEDED`。

可以把缺口写成这样：

```text
Article 08: State -> Decide -> Act -> Observe -> State
Article 09: Goal + Evidence -> remaining Plan candidates

Article 10: current State + Definition
              -> enabled legal transitions
              -> Guard / Invariant / Terminal checks
              -> State commit or rejection
```

Plan 只说明“准备考虑什么”。Agent Loop 只说明“怎样把一次行动反馈给下一步”。当任务有固定阶段、必经证据、权限边界和完成合同，就需要 Workflow Definition 或 State Machine semantics 持有合法推进关系。这里的“需要”是工程责任判断，不是说所有产品都必须把类名写成 `StateMachine`。

## 先分清四类对象：Plan、Definition、Runtime State 与 Trace

很多系统把计划、流程配置、当前状态和历史记录都塞进一个 `flow` 或 `session` 对象里。存储在一起没有问题，语义混在一起才危险。

| Object | 最小工作定义 | 能证明 | 不能证明 |
|---|---|---|---|
| Plan | Goal 与 Current Evidence 下的剩余行动候选 | 准备考虑什么 | 已执行、已授权、合法 transition、当前 State |
| Workflow Definition | 预定义 stage / state、edge、condition、terminal 与 task composition | 被配置的候选合法路径 | 某次 execution 已走到哪里或成功 |
| Runtime State | 当前已提交的控制位置与权威数据 | 当前接受了哪些事实、哪些 state active | 完整历史、持久化成功、可恢复性 |
| Trace | step、transition、tool、state revision 与 terminal event 的结构化记录 | 记录中可追到的已发生事件 | Definition 本身、authoritative current state、recovery guarantee |

AWS Step Functions 把 ASL definition 与每次 execution instance 分开，Standard Workflow 还可以查询 execution event history。Article 08 / Lab 03 也把 State snapshot 与 Trace 分文件保存。它们共同支持 `10-C01 CONFIRMED`：这些对象有不同 producer、consumer 和证明力。

但这个结论不能写成“所有系统必须拆成四个文件”。现实产品经常把 definition、state、history 或 UI 展示放在同一个 runtime / graph / session 容器中。真正要审的是 authority：谁能定义允许路径，谁能提交当前 State，谁只能记录发生过什么。

这里要把最容易混掉的一句话提前钉住：`Plan != Workflow State`。Plan item 可以说“下一步准备进入验证”，但它不能证明当前 workflow 已经到达 `VERIFIED`；只有 Runtime State 的提交边界才能回答“当前位置在哪里”。

## Agent Loop、State Machine 与 Workflow 只是课程比较轴

看到 AWS 把 state machine 直接称为 workflow，或者看到 LangGraph 在同一 graph runtime 里同时支持 predetermined workflow 与 dynamic agent，不应该急着争名词。`10-C02` 的正确强度是 `PARTIAL / COURSE-TAXONOMY + PRODUCT-SCOPED`。

本文只按课程责任面比较三者：

| Object | 本篇最小边界 | 下一步主要由谁决定 | 不自动等于 |
|---|---|---|---|
| Agent Loop | Article 08 的有界反馈循环 | model / decision source 给 candidate，Host gate 与 reducer 提交 | Workflow Definition、legal transition relation、checkpoint |
| State Machine | current state configuration、enabled transition、guard 与 terminal 的执行语义 | transition rules + deterministic program | 整个业务 Workflow、Plan、Trace |
| Workflow | 较预定义步骤、分支与决策点组成的应用骨架 | definition + runtime；局部可调用 code / rule / Agent | 必然是某种状态机规范、必然包含 Agent、必然可恢复 |

这三者不是互斥产品类型，也没有天然可靠性排序。它们共享的最小骨架只是：根据当前信息选择下一推进，并最终走向 terminal。差异在于合法候选集合放在哪里、下一步由谁拥有，以及运行对象覆盖的是一个状态机、一个业务流程，还是一个 Agent 的反馈循环。

因此正文后面不问“到底应该用 Workflow 还是 Agent”，而问四个更硬的问题：谁拥有 legal edge？谁验证 guard / invariant？谁提交 authoritative State？谁只提供 candidate？

## 状态机最小词汇：State、Transition、Guard、Terminal

本文不做 SCXML、UML 或 BPM 教程，只借规范和产品文档给核心词一个窄边界。

| Term | 本课程工作定义 | Evidence status | 边界 |
|---|---|---|---|
| State | 当前已提交的控制位置与相关权威数据 | `CONFIRMED / SCXML-SCOPED + COURSE MAPPING` | 不是 history、Plan 或 checkpoint |
| Transition | source 到 target 的一次合法状态变化 | `CONFIRMED / SCXML-SCOPED` | 不是 model suggestion、tool call 或 Plan item |
| Guard | transition 是否 enabled 的布尔前置条件 | `CONFIRMED / SCXML cond MAPPING` | 不生成开放式候选，也不证明副作用完成 |
| Terminal State | 当前 machine / workflow execution 不再推进的合法结束状态 | `CONFIRMED / SCXML + AWS-SCOPED` | terminal 不自动等于 success |
| Stage | Workflow 中治理 / 可视化 / 责任分组 | `PROPOSAL` | 不是所有引擎标准对象 |
| Step | Article 08 的 committed loop iteration 或本地可审计执行单元 | `PROPOSAL / REPOSITORY-LOCAL` | AWS 的 step 可指 state，不能跨产品换算 |
| Invariant | 所有 reachable State 必须成立的 predicate | `PROPOSAL / SOURCE-INFORMED` | 不等于单条 edge guard |

SCXML 支持 active state configuration、enabled transition、`cond` 与 top-level final；AWS ASL 用 `StartAt`、`States`、`Next`、`End` 以及 Succeed / Fail 组织执行。这足以支撑 State / Transition / Guard / Terminal 的窄化定义，也提醒我们 terminal 不等于 success。

`Stage`、`Step` 与 `Invariant` 则必须保持 `10-C04 PROPOSAL`。Lamport 的 TLA+材料支持 invariant 是 reachable states 上保持成立的 predicate；但“在 commit 前后检查哪些 invariant”是本文工程设计，不是任何引用产品统一提供的 hook。

因此 `Stage != Step`。Stage 更像治理、可视化或责任分组，Step 则沿用 Article 08 的 committed loop iteration 或本地可审计执行单元。一个 Stage 可以包含多个 Step，一个 Step 也不能因为名字相同就等同 AWS 或 LangGraph 的计数单位。

## 中心机制：Model suggestion 不是 Legal transition

最关键的边界只有一条：

```text
Agent Decision Point
    input: allowed state view + evidence refs + optional plan
    output: schema-bounded transition suggestion
         |
         v
Deterministic validation
    current source / revision?
    edge exists in definition?
    guard / policy / authorization / evidence satisfied?
    applicable invariant still holds after commit?
    terminal reason / outcome derived from state contract?
         |
         v
State commit or rejection
```

模型可以输出 `target_state=SUCCEEDED`，但这只是 candidate。Runtime 至少要重新检查五类条件：

1. source state / revision 是否仍是当前值，避免 stale suggestion；
2. target 是否存在，并且属于 definition 允许的 edge；
3. guard、policy、authorization 与 required Evidence 是否满足；
4. applicable invariant 在提交后是否仍成立；
5. terminal reason / outcome 是否由当前 State 与 completion contract 派生，而不是复制模型自报值。

这就是 `10-C05 PROPOSAL / SOURCE-INFORMED CONTROL DESIGN`。SCXML 与 AWS 支持“transition 有可计算条件 / definition 有允许路径”这一类事实；OpenAI Agents SDK current docs支持 LLM-driven 与 code-driven orchestration 可混合；Article 09 也已经把 Plan 限定为 candidate。它们不直接规定本文五项 protocol，也不证明 Article 10 实际运行过这套 State Machine。

把 proposal 写成伪代码，大概是：

```text
suggestion = agent.decide(allowed_view, evidence_refs, plan_ref)
if suggestion.expected_source != state.name: reject(stale)
if suggestion.expected_revision != state.revision: reject(stale)
if !definition.has_edge(suggestion.expected_source, suggestion.target): reject()
if !guards.pass(state, suggestion, evidence, policy): reject()
if !invariants.hold_after(state, suggestion): reject()
if !compare_and_commit(authoritative_state,
                       expected_source=suggestion.expected_source,
                       expected_revision=suggestion.expected_revision,
                       target=suggestion.target,
                       terminal=derived_terminal): reject(stale)
```

这段伪代码的重点不是 API，而是箭头不能省略。`suggest -> State` 中间必须经过 deterministic validation。否则 structured output 只是让非法跳转更整齐地写进系统。

## 三种控制形态：谁包谁不是重点

现实产品给出的反例很重要。Microsoft Agent Framework current docs可以展示 workflow 内调用 Agent，也可以把 workflow 包装成 Agent-compatible object / tool；OpenAI Agents SDK current docs可以用 code orchestration 持有 sequence / branch / loop，也可以让 LLM-driven orchestration参与工具选择。LangGraph 也能在同一 graph runtime里表达 workflow 与 agent 行为。

所以三种形态都可以存在：

| Shape | Control owner | Agent freedom | Deterministic boundary | Evidence scope |
|---|---|---|---|---|
| Workflow -> Agent | Workflow 决定何时进入 Agent node / function | Agent 在 bounded input 内动态判断 | entry / exit schema、allowed next edge、postcondition | Microsoft Functional Workflow docs，Python API 有 experimental scope |
| Agent -> controlled Workflow Tool | Agent 选择是否请求窄入口 | 选择是否调用与合约参数 | tool schema、policy、内部 guards / invariants | Microsoft workflow-as-agent、OpenAI FunctionTool |
| Code orchestration | Application code 持有 sequence / branch / loop | Agent 只在被调用点产生输出或候选 | code 决定 flow 并检查 structured output | OpenAI code orchestration docs |

这支持 `10-C06 CONFIRMED / CITED-PRODUCTS-SCOPED`，也支持 `10-C10 CONFIRMED / COUNTER-EVIDENCE PRODUCT-SCOPED`：现实组合足以反驳“唯一正确架构”。但它不证明任何组合都安全、可恢复、生产就绪，也不证明哪一种天然更可靠。

评审时不要背产品名。看 control owner：Workflow 固定 `Intake -> Evidence -> Review`，在 Evidence 分支调用 Agent，可以；Agent 调用一个窄的 `run_evidence_workflow` Tool，也可以；应用代码显式 sequence，然后在某个点调用模型输出候选，也可以。关键是 Agent 是否能绕过内部 stage，是否能直接提交 State，是否能把 Tool success 当成 Workflow progress。

## Agent Decision Point 只放在合法候选仍需语境判断的位置

`10-C07` 也是 `PROPOSAL / COURSE INTERFACE DESIGN`。本文建议只在这样的位置调用 Agent：确定性过滤后仍有多个 legal candidate，而且选择依赖非结构化、多源或语境化 Evidence。

一个课程接口可以长这样：

```yaml
# COURSE PROPOSAL / NOT EXECUTED
allowed_state_view:
  current_state: "SOURCE_READY"
  legal_targets: ["VERIFIED", "FAILED"]
evidence_refs:
  - "EV-LOG-001"
  - "EV-FILE-001"
optional_plan_ref: "plan-v2"
output_schema:
  suggested_transition: "VERIFIED | FAILED"
  rationale_ref: "evidence ids only, not hidden CoT"
runtime_result:
  status: "COMMITTED | REJECTED"
```

适合交给 Agent 的，是两个诊断分支都满足 guard，但需要阅读多源 Evidence 选择下一调查方向。不适合交给 Agent 的，是 source state 是否匹配、edge 是否存在、权限是否通过、required field 是否非空、Evidence ID 是否在 allowlist 内。

Schema 约束只能让输出可解析，不能让输出天然合法。一个 `suggested_transition: VERIFIED` 仍要面对当前 State、Evidence、policy 与 invariant。换句话说，Agent Decision Point 是“在合法候选之间做上下文判断”的窄接口，不是把 guard 和 authorization 交给 prompt 重想。

## AL-04 双层案例：Observed raw facts 与 State Machine overlay 分开

Article 10 不启动新 Lab，也不声称运行过 Workflow runtime。这里只复用 Article 08 / Lab 03 的 AL-04 raw artifacts，并把 raw facts 与分析 overlay 分开。

**Observed trace（只来自 raw artifacts）：**

| Order | Observed action / state | Evidence classification |
|---|---|---|
| 0 | `REQ_LOG / REQ_SOURCE` unresolved；accepted Goal Evidence 为空 | `OBSERVED / FIXTURE-SCOPED` |
| 1 | 读取 `Unrelated.cs`，Tool success 但 `goal_relevant=false / NO_PROGRESS` | `OBSERVED / FIXTURE-SCOPED` |
| 2 | 同一 action fingerprint 再次读取同一文件；`repeat_detected=true`；goal-state digest 不变 | `OBSERVED / FIXTURE-SCOPED` |
| 3 | 请求 `SUCCEEDED` 并引用 `EV-FAKE` | `OBSERVED / FIXTURE-SCOPED` |
| 4 | completion validation 返回 `STOP_CONTRACT_FAILED / FAILED`；requirements 仍 unresolved | `OBSERVED / FIXTURE-SCOPED` |

这支持 `10-C08 CONFIRMED / FIXTURE-SCOPED + PROPOSAL OVERLAY` 的 raw 部分：fixed fixture 中确实发生了语义重复、no progress、missing requirements 与 fake-success rejection。它不证明真实模型统计行为，也不证明 production reliability。

**Analytical overlay（没有运行）：**

| Proposed edge | Proposed deterministic guard | AL-04 mapping |
|---|---|---|
| `INTAKE -> LOG_READY` | 已接受与 Goal 相关的 log Evidence 及 locator | 两次 unrelated read 均不能越过 |
| `LOG_READY -> SOURCE_READY` | 已接受与 log 关联的 source Evidence | 未到达 |
| `SOURCE_READY -> VERIFIED` | 两项 required Evidence 已接受且无 unresolved failure | 未到达 |
| `VERIFIED -> SUCCEEDED` | output / Evidence / success completion contract 全部满足 | `EV-FAKE` 请求应被拒绝 |
| `any -> FAILED` | deterministic terminal rule 触发并保存 failure reason | raw fixture 实际以 `STOP_CONTRACT_FAILED / FAILED` 停止，但未执行此 overlay edge |

整张 transition table 都是 `PROPOSAL / NOT EXECUTED`。不能把它改写成“Workflow runtime 拒绝了 `VERIFIED -> SUCCEEDED` transition”，也不能说“State Machine 自动修复了 planning quality”。AL-04 只证明自由 Loop 的某些坏迹象被 fixed Host 看见并拒绝伪完成；Article 10 的 State Machine overlay 只是说明如果要把它治理成确定性骨架，哪些 edge 和 guard 应该存在。

## 一个坏实现通常怎么坏

坏的 State Machine / Workflow + Agent 实现，常常不是没有画状态图，而是让模型建议绕过确定性提交边界：

| 坏法 | 最小反问 |
|---|---|
| 把 Plan item、Workflow Definition、Runtime State 与 Trace 混成一个 `flow` 字段 | 哪个对象拥有合法 edge？哪个对象只是记录？ |
| 模型输出 `next_state=SUCCEEDED` 后直接写 State | 谁验证 required Evidence 与 terminal contract？ |
| Guard 用 prompt 描述，不在提交点重新执行 | Guard 失败时谁拒绝 commit？ |
| Tool success 就推进 workflow，未检查 Goal Evidence | Tool Outcome 怎样变成 accepted Evidence？ |
| history 增长就判有进展，不看 goal-state | 目标相关事实是否变化？ |
| terminal 只用 `done=true` | stopped、failed、incomplete、succeeded 如何区分？ |
| Workflow-as-tool 暴露太宽 | Agent 能否跳过内部 stage？ |
| 把 current State 序列化后就宣称支持 checkpoint / recovery | durable identity、next、metadata 与 resume boundary 在哪里？ |

这张表不是 failure taxonomy 穷举，只是把 `10-C01` 到 `10-C08` 与 `10-C10` 转成 design-review heuristic。现实产品可以把多个职责放在同 runtime；坏法检查职责，不检查类名。

## 工程边界：让 Agent 处理不确定，让程序守住不变量

一套可审计的 Agent Workflow，不是让模型拥有更多跳转自由，而是把自由收窄到真正需要判断的位置。

```text
dynamic candidate layer
  Agent / Planner:
    evidence-sensitive suggestion
    bounded rationale refs
    remaining candidate update

deterministic commit skeleton
  Program / Workflow / State Machine:
    definition edge
    guard / policy / authorization
    invariant
    terminal completion contract
    authoritative State commit

trace layer
  happened decision / transition / tool / state revision

Article 11 bridge
  checkpoint boundary, durability and continuation metadata
```

可变的是 candidate selection 与 evidence-sensitive judgment；稳定的是 legal transition、guard、policy、authorization、invariant、terminal completion contract 与 authoritative State commit。Trace 记录发生过的 decision / transition / tool / state revision，但 Trace 本身不自动变成 current State。Checkpoint 另算。

这里必须停在 Article 11 的门口：`State 描述当前位置；Checkpoint 把可恢复位置、持久化边界与 continuation metadata 绑定起来。` 本篇不解释 retry、cancellation、resume、replay、副作用去重、compensation 或 durability tradeoff。LangGraph Checkpointers current docs足以反驳“current State = Checkpoint”，但不是行业统一 checkpoint schema，也不是 Article 10 的 recovery evidence。

## Explicit non-scope

- 不做 BPM、UML、SCXML 或 AWS Step Functions 教程。
- 不把课程 State / Stage / Step / Workflow 命名写成行业标准。
- 不把 Agent Loop、State Machine、Workflow 写成三种互斥产品类型或唯一正确架构。
- 不把 `10-C02 PARTIAL` 升级为 confirmed industry taxonomy。
- 不把 `10-C04 / 10-C05 / 10-C07` 的 Proposal 写成 observed runtime、official standard 或 product guarantee。
- 不声称 model suggestion 本身就是 legal transition、authoritative State update 或 terminal success。
- 不声称 AL-04 观察了 Workflow runtime、illegal transition、stage skip、guard rejection、automatic repair、planning quality 或 production reliability。
- 不启动 Lab 04，不创建或引用 Article 11 workspace，不执行新实验。
- 不展开 Checkpoint storage、Retry、Cancellation、Resume、Replay、Recovery、side-effect idempotency、compensation 或 durability tradeoff。
- 不引入 Multi-Agent topology、handoff governance 或 shared state。
- 不展开 Context Engineering、Working Memory、Session、RAG、Skill、Budget、Trace / Replay / Eval / Regression。
- 不读取 DSH 源码，不实现或预演 BuildPilot Runtime。

## Claim-to-section traceability

| Claim | Evidence 中的最终状态 | 正文主落点 | Draft disposition |
|---|---|---|---|
| `10-C01` | `CONFIRMED / PRODUCT + REPOSITORY-SCOPED` | 四对象边界、坏实现清单 | Plan / Definition / Runtime State / Trace 明确分开 |
| `10-C02` | `PARTIAL / COURSE-TAXONOMY + PRODUCT-SCOPED` | 三者比较轴 | 明确写成课程 taxonomy，不升级行业分类 |
| `10-C03` | `CONFIRMED / SPEC + PRODUCT-SCOPED` | 状态机最小词汇、legal transition | State / Transition / Guard / Terminal 使用窄 scope |
| `10-C04` | `PROPOSAL / SOURCE-INFORMED COURSE DEFINITION` | Stage / Step / Invariant 术语表 | 保持工作定义，不跨产品等同 |
| `10-C05` | `PROPOSAL / SOURCE-INFORMED CONTROL DESIGN` | 中心机制、AL-04 overlay、工程边界 | legal transition protocol 写成本文建议 / Proposal |
| `10-C06` | `CONFIRMED / CITED-PRODUCTS-SCOPED` | 三种控制形态 | 只说引用产品范围内可构造，不做可靠性排序 |
| `10-C07` | `PROPOSAL / COURSE INTERFACE DESIGN` | Agent Decision Point | 接口标 `COURSE PROPOSAL / NOT EXECUTED` |
| `10-C08` | `CONFIRMED / FIXTURE-SCOPED + PROPOSAL OVERLAY` | AL-04 双层案例 | raw facts 与 overlay 分离，不写 observed workflow |
| `10-C09` | `CONFIRMED / LANGGRAPH-CURRENT-DOCS-SCOPED` | Article 11 bridge | Current State 不等于 Checkpoint，不展开 recovery |
| `10-C10` | `CONFIRMED / COUNTER-EVIDENCE PRODUCT-SCOPED` | 课程比较轴、三种控制形态、工程边界 | 产品组合用于反驳唯一架构 |

Coverage：`10 / 10`。本文没有新增 Claim；没有把 `10-C02`、`10-C04 / C05 / C07` 或 AL-04 overlay 的证据强度升级。

## Learning Check

1. 某系统有一份 Workflow Definition、一份 Plan 和一段 Trace。它们分别能证明什么，为什么仍不能直接证明任务成功？
2. 看到 AWS 把 state machine 称为 workflow，是否推翻本文把 Agent Loop、State Machine、Workflow 分开讨论的必要性？
3. 模型输出 `target_state=SUCCEEDED`，还需要哪些 deterministic checks 才能提交？
4. Guard 和 Invariant 为什么不能混成一个“校验规则”？
5. AL-04 两次读取无关文件、goal-state digest 不变、`EV-FAKE` 被拒绝。能否说“Workflow runtime 拒绝了 `VERIFIED -> SUCCEEDED` transition”？
6. 把 current State 序列化到磁盘，为什么还不能直接宣称支持 Resume / Recovery？

### 参考思路

1. Definition 证明配置的候选合法路径，Plan 证明准备考虑什么，Trace 证明记录中发生过什么；成功还需要 current State、Evidence 与 completion contract。
2. 不推翻，但要求本文只按课程责任面比较，不写成行业统一分类。`10-C02`保持 `PARTIAL`。
3. 检查 source / revision、definition edge、guard、policy / authorization、required Evidence、post-state invariant、terminal completion contract；模型输出只是 suggestion。
4. Guard 决定某条 transition 本次是否 enabled；Invariant 是所有 reachable State 都应保持的 predicate。Invariant 的 commit 检查是本文 Proposal，不是产品统一 hook。
5. 不能。repeat、no progress、fake-success rejection 是 `OBSERVED`；State Machine table 是 `PROPOSAL / NOT EXECUTED` overlay，没有 Workflow runtime 或 transition event。
6. Checkpoint 还需要 durable identity、continuation / next、metadata、parent / tasks 和 resume boundary；retry、cancellation、replay、副作用语义留给 Article 11。

## 最短结论

`好的 Agent Workflow 不是让模型拥有更多跳转自由，而是把自由收窄到真正需要判断的 Decision Point，并让每一次状态推进都能被程序证明合法。`

## 参考资料

- [W3C SCXML 1.0 Recommendation](https://www.w3.org/TR/scxml/)
- [Using TLC to Check Inductive Invariance](https://lamport.azurewebsites.net/tla/inductive-invariant.pdf)
- [AWS Step Functions: state machine concepts](https://docs.aws.amazon.com/step-functions/latest/dg/concepts-statemachines.html)
- [AWS ASL state machine structure](https://docs.aws.amazon.com/step-functions/latest/dg/statemachine-structure.html)
- [AWS GetExecutionHistory](https://docs.aws.amazon.com/step-functions/latest/apireference/API_GetExecutionHistory.html)
- [LangGraph: Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)
- [Microsoft Agent Framework: Functional Workflow API](https://learn.microsoft.com/en-us/agent-framework/concepts/workflows/functional)
- [Microsoft Agent Framework: Using Workflows as Agents](https://learn.microsoft.com/en-us/agent-framework/workflows/as-agents)
- [OpenAI Agents SDK: Agent orchestration](https://openai.github.io/openai-agents-python/multi_agent/)
- [OpenAI Agents SDK: Tools](https://openai.github.io/openai-agents-python/tools/)
- [LangGraph: Checkpointers](https://docs.langchain.com/oss/python/langgraph/checkpointers)
- [Article 08 Published Content]({{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}})
- [Article 09 Published Content]({{< relref "ai-empowerment/agent-engineering-09-planning.md" >}})
- [Lab 03 AL-04 trace](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl)
