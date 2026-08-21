---
title: "Planning：Agent 为什么需要计划，又为什么不能迷信计划"
slug: "agent-engineering-09-planning"
date: "2026-08-20"
description: "Plan 是 Goal 与 Current Evidence 条件下的剩余行动候选；它必须随 Observation 修订，并接受 Policy、Workflow、Evidence 与 Authorization 的拒绝。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Agent Planning"
  - "Runtime Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 100
weight: 3100
---

> **上一篇**：[Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop]({{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}})
> **下一篇**：[State Machine 与 Workflow：确定性骨架和 Agent Decision Point]({{< relref "ai-empowerment/agent-engineering-10-state-machine-workflow.md" >}})

先看一个构造的教学评审场景：假设团队让 Agent 调查一次构建失败，系统给出一份看起来颇有条理的计划：解析日志、定位报错源码、检查配置、验证修复，界面甚至把其中一些计划项标成了 `done`。

问题是，日志解析可能根本没有成功，源码路径可能还没取得，删除旧文件的动作也可能从未获批。计划写得完整，只能说明系统表达了“接下来想做什么”；它没有证明这些动作已经执行，更没有证明相应事实已经进入权威状态。

上一篇 [Agent Loop]({{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}}) 已经把一次有界 Run 内的 `Decision candidate -> Act -> Tool Outcome -> Observation -> State -> Continue / Stop` 分开。它回答的是“当前 State 下怎样安全推进一步”。当目标跨越多个 Step，新的问题才出现：系统怎样保留方向，怎样在新证据出现后修改方向，又怎样防止计划反过来覆盖运行事实？

如果这篇只记一句话，我建议记住：

`Plan 只是 Goal 与 Current Evidence 条件下的剩余行动候选；它必须随 Observation 修订，并接受 Policy、Workflow、Evidence 与 Authorization 的拒绝，不能冒充执行事实或完成状态。`

> 证据范围：ReAct、Plan-and-Solve 的表述来自原论文；产品形态来自 2026-08-20 核对的 Semantic Kernel current planning page、LangGraph.js official Plan-and-Execute notebook，以及2026-08-20 retrieved current official OpenAI Agents SDK docs。`openai-agents 0.22.0`只作为当日PyPI / tag version anchor，docs-current与tag未做逐项source mapping。`Implicit / Visible / Structured Plan`、`KEEP / REVISE / REPLACE / STOP` 与最小 Plan artifact 都是课程 Proposal，不是行业统一标准。Lab 03 只提供冻结 fixture 的运行事实，没有运行 Planner 或自动 re-planning。

## Planning 为什么出现，又为什么最容易制造“纸面完成”

一步任务可以每次只看眼前：读取一个已知文件，得到结果，然后结束。多步调查却常常包含依赖、顺序和执行前未知量。例如“解释一次编译失败”至少可能经过：

```text
解析日志
  -> 取得 diagnostic path / symbol
  -> 读取匹配源码
  -> 检查日志与源码是否共同满足 Goal Evidence
```

第二步依赖第一步产出的 locator，第三步又依赖前两步都留下可接受 Evidence。如果系统每次 Decide 都只看到局部结果，它可能忘记仍缺哪项前提，重复选择看似有用但不推进 Goal 的动作，或者在中途把新的子目标误当成最终目标。

Planning 的价值在于表达跨 Step 的剩余方向：哪些行动是候选，先后依赖是什么，哪些未知仍待验证。它降低的是“每一步都从零猜下一步”的盲目性，不是事实错误率，也不是成功保证。ReAct 研究展示了 reasoning、action 与 observation 交错，并允许外部反馈参与高层行动计划的追踪和更新；Plan-and-Solve 则展示“先分解、再按计划求解”的 prompting 节奏。它们都没有把计划文本变成执行记录。

因此，下面两条路径的差别只在于是否保留剩余意图，两者都必须回到运行事实：

```text
失忆式局部选择：State -> choose one action -> execute -> observe -> 再临时猜一步

有候选计划：    Goal + Evidence -> remaining candidates
                                  -> execute one allowed step
                                  -> observe -> inspect / update candidates
```

如果 Plan item 写着“检查配置并确认已生效”，但 Tool 尚未执行，这句话仍只是 candidate。没有 correlated execution record、Observation、authoritative State update 与 completion Evidence，就不能把它写成“已验证”。这正是 Planning 最危险的错觉：计划越像项目看板，人越容易把纸面进度误读成事实进度。

## 抽象模型：Plan 是剩余行动候选，不是另一份 State

本文采用一个明确标为 **课程 Proposal** 的最小定义：

`Plan = Goal + Current Evidence 条件下，对剩余候选行动的表示。`

“剩余”意味着已经执行的历史不应伪装成下一步；“候选”意味着每一项都可能被证据、授权或确定性约束拒绝；“Current Evidence”意味着 Plan 有版本语境，旧计划不会因为曾经合理就永远有效。

这个定义也给出了一条很实用的评审方法。看到任何 Plan item，先不要问“描述是否聪明”，而要问它依赖哪些当前事实：输入是否已经取得，前一步是否留下了可接受结果，动作是否在当前能力与权限内，完成判据是否仍未满足。把这些前提写清后，Plan才是一组可以被新Observation检验的候选；不写前提时，它只是按自然语言排列的愿望清单，也无法解释后来为什么改变方向。

同样，已经完成的步骤不应继续留在“剩余候选”里承担事实证明。它们应该由execution history、Observation和Verified State表达。Plan可以引用这些对象，说明为什么下一项仍然合理，却不能复制一段过去式文字并据此宣布State已经更新。这样切开后，计划版本负责方向变化，运行记录负责发生历史，权威State负责当前事实，三者才能各自被复核。

```text
Goal + Current Evidence
          |
          v
 Remaining Candidate Intent
          |
          +--> implicit in loop / history
          +--> visible plan list
          +--> structured plan artifact
```

为了讨论可观察合同，本文把 Plan 分成三种形态。这个三分法同样是课程 taxonomy，不是在判断“模型脑中是否规划过”。

| 形态 | 本文工作定义 | 可以审计什么 | 不自动拥有的能力 |
|---|---|---|---|
| Implicit Plan | 没有独立、持久化的 Plan object；下一意图从 loop / history 中逐步形成 | Decision、Tool call、result sequence | 完整长期步骤、revision diff |
| Visible Plan | 面向人或 Trace 的剩余步骤列表 | plan version、item、change reason | 机器可校验 schema、执行权、授权 |
| Structured Plan | 带 schema 的步骤、依赖、状态或版本 artifact | parser、validator、diff、consumer | 执行、Verified State、Workflow invariant |

在本轮引用的产品资料中，两种不同形态同时存在。Semantic Kernel current planning page 把 function call 与 result 的反馈循环作为当前主要路径，并记录旧式 Stepwise / Handlebars planners 已移除；LangGraph.js 官方示例则显式保存 `plan: string[]` 和 `pastSteps`。这只能说明：在这些 cited products 中，Planning 可以逐步形成，也可以成为显式对象，独立 `Planner` class 不是共同必要条件。

它不能说明两种产品能力等价，也不能说明 structured plan 天然更可靠。LangGraph.js notebook 是未在本轮执行 compatibility run 的官方示例，Semantic Kernel 页面也没有绑定本轮具体 package build。即使多个对象放在同一个 state container 中，“同容器”也不等于同语义、同 producer 或同 authority。

## 四种常见说法只比较控制节奏

Planning 讨论很容易被术语带走：ReAct、Plan-and-Solve、Plan-and-Execute、Planner / Executor 仿佛是四个可以横向评分的产品选项。其实它们来自不同论文与实现语境，最多先帮助我们辨认控制节奏。

| Pattern | 这里只解释什么 | 当前证据允许的最小表述 | 不能推出 |
|---|---|---|---|
| ReAct | reasoning、action、observation 交错 | 原论文中可借 external observation 追踪、更新高层 action plan并处理 exception | 所有实现都保存 structured plan，或生产系统必须公开完整 CoT |
| Plan-and-Solve | 先分解，再按步骤求解 | 原论文是一种多步 reasoning task 的 prompting strategy | 等同工具型 Plan-and-Execute runtime architecture |
| Plan-and-Execute | 先生成多步 Plan，再执行并重访剩余 Plan | cited LangGraph.js example 分开 `plan`、`pastSteps`，执行首项后进入 replan | 是唯一推荐架构，或已证明生产可靠性 |
| Planner / Executor | 分开生成 / 修订候选与执行已选步骤的责任 | cited example 中由不同 runnable / node 承担 | 必须使用另一个 Agent、模型或进程 |

这项比较的 Evidence 状态是 `PARTIAL / PATTERN-SCOPED`。它们不是互斥分类：被引用的 Plan-and-Execute 示例中，executor 本身就可以使用 ReAct agent。也正因为如此，本篇不做 API 一一映射，不比较效果、成本、延迟或 benchmark，更不把 Plan-and-Solve 的论文设置扩写成 Tool Runtime、Authorization 或 Workflow 证据。

真正值得带回工程设计的不是“选哪个名词”，而是四个问题：候选在什么时候产生？一次执行后什么结果会被保留？谁重访剩余候选？生成候选与执行候选是否拥有不同权力？

## Revision / Re-planning：改变剩余候选，不改写过去事实

行动一旦产生新结果，旧 Plan 的前提就要重新接受检查。ReAct 与 cited LangGraph.js example 都支持一个有限结论：external Observation 或已执行结果可以成为更新剩余 Plan 的输入。它们不要求每个 Observation 都触发 revision，也不保证 re-planning 会得到更好的答案。

把这个过程写成控制链，可以得到：

```text
Goal + State(n) + Plan(v1)
     -> execute one allowed step
     -> Outcome -> Observation -> State(n+1)
     -> inspect remaining assumptions
     -> KEEP | REVISE | REPLACE | STOP / ESCALATE
     -> Plan(v1 or v2 candidate)
```

为了让“为什么改计划”可审查，本文提出下面这组 disposition。它是 **课程 Proposal**，不是来源中的标准 enum：

| Disposition | 课程条件 | 最小审计记录 |
|---|---|---|
| `KEEP` | 新 Observation 没有否定剩余步骤的前提 | plan version、accepted observation reference |
| `REVISE` | Goal 与主路径仍成立，但步骤、顺序、参数或前提需局部修改 | from / to version、change reason、evidence reference |
| `REPLACE` | 关键前提、Goal 或允许路径失效，需要废弃剩余路径 | invalidated assumption、replacement candidate、authority check |
| `STOP / ESCALATE` | 没有安全候选路径，或需要授权 / 人类输入 | blocker、stop reason、required authority |

假设日志已成功解析，只是输出路径格式变化，那么 Goal 与主路径仍成立，调整下一步参数更接近 `REVISE`。如果日志解析没有产生任何 diagnostic locator，而旧 Plan 的下一项正是“读取 locator 指向的源码”，关键前提已经失效，继续照表执行就没有意义，更接近 `REPLACE`。如果替代路径需要新的权限且当前拿不到，就应 `STOP / ESCALATE`，而不是为了让计划显得完整再编一步。

Re-planning 与 Retry 也不能混成一个按钮。这里的 Re-planning 改变后续候选意图或顺序；Retry 只表示在既定 policy 下再次尝试相同意图。是否重试、退避、恢复以及如何判断暂态失败，属于 Article 11 的责任，本篇不提前定义。相同 Observation 在不同 policy、budget 或 authorization 下，也可能导向不同 disposition。

## Plan 的权力边界：候选步骤不能批准、执行或自证完成

Planning 最关键的架构判断不是 Plan 长什么样，而是谁有权把它变成事实。至少要把下面六个对象分开：

| Object | 最小含义 | Evidence / authority source | Plan 能否替代 |
|---|---|---|---|
| Plan candidate | 接下来可能做什么 | planner role、model 或 code | — |
| Execution | action 是否真实发出并被 runtime 处理 | executor / Tool Runtime trace | 否 |
| Observation | outcome 经关联与正规化后观察到什么 | Article 08 的 Host boundary | 否 |
| Verified State | accepted Observation / Evidence 如何更新权威任务事实 | reducer / verifier | 否 |
| Authorization | action 是否被当前 policy / approval 允许 | policy、guardrail、human approval | 否 |
| Workflow | 允许的 stage、edge、guard 与 invariant | program / orchestration definition | 否 |

也就是说：

`Plan != Execution != Observation != Verified State != Authorization != Workflow`

更完整的权力路径应当是：

```text
Plan item / model tool call
     -> capability / policy / approval / workflow gate
     -> permitted execution
     -> Outcome -> Observation -> Verified State
     -> completion evidence
```

例如 Plan 提出“删除旧文件”，只说明删除是候选行动。它不会因为出现在列表中就取得文件写入权，模型进一步发出 tool call也仍不等于授权。在2026-08-20 retrieved current official OpenAI Agents SDK docs的范围内，custom `function_tool` input guardrail可以在执行前 skip、替换输出或触发tripwire；human-in-the-loop flow也可以暂停pending tool approval，再由人批准或拒绝。Guardrail行为不覆盖hosted / built-in tools或handoff，HITL支持范围也受tool type约束。`openai-agents 0.22.0`只是2026-08-20当日PyPI / tag version anchor；docs-current与tag源码未做逐项source mapping，因此本文不把这些文档行为命名为已核验的“0.22.0 contract”。这是明确产品与tool-type范围内的例证，不是所有SDK共用的gate顺序。

审批通过也只回答“允许尝试执行”，不回答“已经执行”，更不回答“事实正确”。Tool Runtime 还要产生 correlated Outcome，Host 还要形成 Observation，reducer / verifier还要决定哪些内容进入 Verified State，completion contract最后才判断 Goal 是否满足。

这条链的价值在故障现场尤其明显。若一个动作被policy拒绝，系统应该留下的是拒绝结果以及对剩余Plan的影响，而不是一条伪造的执行失败；若动作执行成功但结果不满足Goal，系统应该保留成功Outcome与未完成State，而不是把两者压成失败；若Observation已经接受，Plan也只能据此提出下一候选，不能回头改写原始Outcome。把authority拆开，才能定位问题发生在候选生成、执行许可、工具运行、事实接受还是完成裁决。

对多人协作也一样：提出调查方向的人、维护工具权限的人、定义完成Evidence的人未必是同一角色。Plan若同时拥有这些权力，任何一处误判都会在同一个对象里被自我确认；Plan只保留candidate authority时，各责任面才有机会独立拒绝、收窄或要求补证据。

因此，Planner 自己把 item 标成 `done` 仍不够。一个 structured plan当然可以拥有 `status` 字段，但这个字段必须由有 authority 的 runtime / reducer 根据运行证据更新；不能让提出候选的角色同时给自己签发完成证明。Budget 也可以拒绝继续消费动作，但 token、step、cost 与 latency 的具体工程属于 Article 20，这里只保留拒绝边界。

## 可审计 Plan 应保存什么，又不必保存什么

计划需要可回看，不等于必须保存模型的完整 Chain-of-Thought。被引用资料本身已经展示了不同载体：ReAct 研究轨迹包含 verbal reasoning，LangGraph.js example保存紧凑 `plan` / `pastSteps`，Semantic Kernel current guidance则可以通过 function-call feedback loop形成下一次 decision。它们没有共同规定生产系统必须持久化完整私有推理。

本文建议保存最小、可检查的候选与变更依据。这仍是 **课程 Proposal**：

```yaml
# COURSE PROPOSAL / ANALYSIS OVERLAY
plan_version: 2
remaining_candidate_steps:
  - "先解除 parse failure，再决定是否读取匹配源码"
change_reason: "AL-02/step-01 未产生 diagnostic locator"
evidence_references:
  - "AL-02/step-01 observation"
```

这四类字段分别回答“当前是哪一版”“还准备做什么”“为什么变化”“依据指向哪里”。它们让 reviewer可以对比版本，却不会自动使 Plan 正确。有 `evidence_references` 不等于引用真实、已接受；有 `status` 不等于 action 已执行；有 version不等于 revision合理。字段的 producer、引用对象与 verifier仍需要单独合同。

这种设计把可解释性压缩到工程真正需要的审计面：候选、版本、变更原因和证据引用。它不讨论模型内部推理机制，也不在本篇给出隐私或存储策略结论，更不把四个字段包装成跨行业 schema。

## AL-02 双轨案例：失败被观察到，不等于 Runtime 已自动改计划

Article 08 的 Lab 03 提供了一条适合检查边界的冻结轨迹。这里必须同时画两条泳道：下层是 raw artifact 中真实发生的运行事实，上层是 Article 09 为解释 Planning 加上的候选 overlay。

| Sequence | Layer | Classification | 本文允许的结论 |
|---|---|---|---|
| 1 | Initial Plan v1 | `PROPOSAL` | 候选顺序是 parse log -> read matched source -> verify Goal Evidence |
| 2 | Execution | `OBSERVED` | AL-02 step 1调用 `parse_mock_log` |
| 3 | Tool Outcome | `OBSERVED` | `FAILED / MOCK_PARSE_FAILED / FI_PARSE_TYPED_FAILURE` |
| 4 | Observation | `OBSERVED` | kind=`TOOL_FAILURE`，normalization=`PASS`，没有 accepted Evidence ID |
| 5 | Verified State | `OBSERVED` | `REQ_LOG / REQ_SOURCE`仍 unresolved，accepted Goal Evidence为空 |
| 6 | Disposition | `PROPOSAL / REPLACE` | v1 的 locator前提不成立；候选v2先解除parse failure，否则stop / escalate |
| 7 | Runtime re-plan / v2 execution | `NOT OBSERVED` | 不得声称发生 |

在 raw 轨迹中，Step 1确实执行了 `parse_mock_log`，Tool 返回 typed failure。Host 成功把失败正规化成 Observation，并在 State revision中保留 unresolved tool failure；这只表示失败被可靠观察，不表示工具成功。由于没有取得 diagnostic locator与 Goal Evidence，v1 下一项“读取匹配源码”的前提没有成立。

我们因此可以提出一个可审查的 `REPLACE` 建议：废弃 v1 的剩余路径，候选 v2先定位或解除 parse failure；若当前授权和边界内没有安全路径，则停止或升级。这一判断是 Planning overlay，不是 Lab 03 runtime输出。Lab 03没有 Planner，没有观察模型生成 v1 / v2，也没有观察 automatic revision、v2 execution或成功恢复。

更不能拿后续某个 successful request倒推早期前提成立。冻结 run 的 terminal仍是 `UNRESOLVED_TOOL_FAILURE / FAILED`。它证明 Observation足以反驳旧前提，也证明 Plan item不能抹掉 unresolved failure；它不证明 Retry必然失败、候选 v2能够成功、真实模型具有 planning quality或生产系统可靠。

raw证据可从 run-a 的 [Tool Outcomes](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/tool-outcomes.jsonl)、[Observations](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/observations.jsonl)、[State snapshots](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/states.jsonl) 与 [Trace](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl) 逐层复核。

## 一个坏 Planning 实现通常怎么坏

显式列表、replanner和状态字段都齐全，仍然可能制造伪进展。常见问题不是“没有计划”，而是 candidate、authority与fact被压成了同一个对象：

1. 把“列出了步骤”写成“已完成分解与验证”，让 Plan文字替代 runtime record。
2. Planner自行把 item标成 `done`，没有 correlated Outcome、accepted Observation或authoritative State update。
3. 把 ReAct、Plan-and-Solve、Plan-and-Execute写成互斥技术选型，再做 API逐项对照。
4. 任何 Tool failure都机械 Retry，不检查原路径前提是否失效，也不记录 change reason。
5. Plan中出现敏感 action就直接执行，绕过 capability、policy、approval或workflow guard。
6. 为了“可解释”持久化完整 CoT，却没有plan version、remaining candidates与evidence reference。
7. 把 `STOP`、`REPLACE`或“无安全路径”当成难看结果而隐藏，硬凑一个看似完整的新 Plan。

这不是 Planning failure mode 的穷举，而是由本文九条 Claim 直接转写的 design-review heuristic。评审一套实现时，与其问“有没有 Planner”，不如连续追问：候选与执行是否分开？谁更新 Plan version？哪条 Observation推翻了哪个前提？谁有权批准动作？谁提交 Verified State？谁用 completion Evidence关闭 item？回答不了这些问题，显式 Plan只会让错误看起来更有组织。

还可以用一次最小追踪反查这些问题：任选一个被标记为完成的item，沿它的evidence reference找到Observation，再沿correlation找到Tool Outcome，最后确认哪个reducer / verifier把事实提交到了哪一版State。任一环缺失，就把item退回candidate或unverified，而不是用Planner的自然语言解释补洞。再任选一次Plan版本变化，确认change reason明确指向新Observation或约束变化，并能说明旧版本中哪项前提失效。这个检查不保证计划质量，却能阻止候选描述冒充运行事实。

## 工程边界：让 Plan 可变，让约束稳定

Planning适合持有随 Evidence变化的部分：remaining candidates、候选顺序、参数、前提以及变更理由。它不拥有真实执行、Observation normalization、Verified State提交、授权、固定 routing / invariant或完成裁决。

```text
          可变候选层
 Goal + Evidence -> Plan(vN) -> candidate action
                                  |
                                  v
          确定性控制边界
 capability / policy / approval / workflow / evidence gate
                                  |
                                  v
          execution -> observation -> verified state
```

这意味着“模型是否可以重新想”与“哪些规则不能每次重想”必须同时设计。Plan可以建议换一条调查路径，却不能修改必须先获批才能删除文件的规则；Plan可以调整候选顺序，却不能把缺失 Evidence的 item直接写成完成；Plan可以因新 Observation变化，却不能倒写已经发生的 Outcome。

本文只建立权力分界，不展开后续系统：Article 10将讨论 State Machine / Workflow怎样持有stage、edge与invariant；Article 11处理Checkpoint、Retry、Cancellation、Resume与Recovery；Article 20处理token、step、cost与latency Budget。本篇也不做Planning算法或论文综述，不讨论tree search、MCTS、beam search和planner benchmark，不把Chain-of-Thought设计成必须持久化的Plan，不读取DeepSeek Harness源码，也不实现或预演BuildPilot Runtime。

## Claim-to-section traceability

| Claim | Evidence状态 | 正文主落点 | Draft disposition |
|---|---|---|---|
| `09-C01` | `PROPOSAL / COURSE TAXONOMY` | 问题空间、抽象模型 | Plan始终写成remaining candidate，不升级为行业定义 |
| `09-C02` | `CONFIRMED / CITED-PRODUCTS-SCOPED` | 三种Plan形态 | 只说明cited products形态并存，不比较产品能力 |
| `09-C03` | `PARTIAL / PATTERN-SCOPED` | 四种常见说法 | 只比较节奏与责任面，不做API映射或排名 |
| `09-C04` | `CONFIRMED / SOURCE + FIXTURE-SCOPED` | Revision、AL-02 | 只说明Observation可成为更新输入并反驳v1前提 |
| `09-C05` | `PROPOSAL / COURSE TAXONOMY` | disposition表、AL-02 | `KEEP / REVISE / REPLACE / STOP`不写成标准enum |
| `09-C06` | `CONFIRMED / CITED-IMPLEMENTATION + FIXTURE-SCOPED` | authority边界、AL-02 | 六类对象与authority分开，不外推统一字段 |
| `09-C07` | `CONFIRMED / OPENAI-CURRENT-OFFICIAL-DOCS-RETRIEVED-2026-08-20` | authorization例子 | 保留custom `function_tool`、hosted / built-in tools、handoff、HITL tool-type与docs-current / tag未逐项mapping边界；`0.22.0`仅为当日version anchor |
| `09-C08` | `PROPOSAL / COURSE ARTIFACT DESIGN` | 最小Plan artifact | 不要求保存完整CoT，不宣称统一schema |
| `09-C09` | `CONFIRMED / OBJECT-CONTRACT + FIXTURE-SCOPED` | 纸面完成、authority、AL-02 | Plan item不证明执行、验证或成功 |

Coverage：`9 / 9`。`09-C03`保持`PARTIAL`；`09-C01 / C05 / C08`保持`PROPOSAL`；AL-02保持`OBSERVED / PROPOSAL / NOT OBSERVED`三种分类。

## Learning Check

1. 模型列出“检查配置并确认已生效”，但 Tool 尚未执行。它属于 Plan、Execution还是Verified State？
2. 一个 Runtime 没有独立 `Planner` class，只根据上一步 result形成下一次 Decision，能否直接判定“没有 Planning”？
3. 为什么不能把 ReAct、Plan-and-Solve、Plan-and-Execute与Planner / Executor做成一张API一一映射表？
4. AL-02 Step 1得到typed parse failure，State仍没有diagnostic locator。为何不能宣称“Runtime已replan并恢复”？
5. Plan包含“删除旧文件”，甚至模型已发出tool call，哪个层仍可拒绝？拒绝后能否写成“删除已验证完成”？
6. 为什么plan version、change reason与evidence reference比保存完整Chain-of-Thought更符合本篇审计目标？

### 参考思路

1. 只能算candidate Plan item。没有execution record、Observation、authoritative State update与completion Evidence，不能写成已执行或已验证。
2. 不能。cited products显示Planning既可从feedback loop逐步形成，也可显式保存；是否提供可观察artifact是另一项工程选择，三分法则是课程taxonomy。
3. 它们的来源语境和抽象层不同，强调控制节奏或责任面，还可以嵌套。本文对`09-C03`只作`PARTIAL / PATTERN-SCOPED`比较。
4. Outcome、Observation、State与failed terminal是`OBSERVED`；Plan v1 / v2与`REPLACE`是analysis overlay，v2 execution和恢复均为`NOT OBSERVED`。
5. capability、policy / guardrail、human approval或workflow guard仍可拒绝 / 收窄。拒绝不是执行，更不是Verified State或完成。
6. 这些字段使remaining candidates与变更依据可检查；它们是课程Proposal，不保证Plan正确，也不要求暴露完整私有推理。

## 最短结论

`Plan 应该告诉系统“接下来可以考虑什么”，不能替系统宣称“已经发生了什么”。`

## 参考资料

- [ReAct: Synergizing Reasoning and Acting in Language Models](https://arxiv.org/abs/2210.03629)
- [Plan-and-Solve Prompting](https://arxiv.org/abs/2305.04091)
- [LangGraph.js Plan-and-Execute official notebook](https://raw.githubusercontent.com/langchain-ai/langgraphjs/main/examples/plan-and-execute/plan-and-execute.ipynb)
- [Semantic Kernel Planning](https://learn.microsoft.com/en-us/semantic-kernel/concepts/planning)
- [OpenAI Agents SDK Guardrails](https://openai.github.io/openai-agents-python/guardrails/)
- [OpenAI Agents SDK Human-in-the-loop](https://openai.github.io/openai-agents-python/human_in_the_loop/)
- [PyPI：openai-agents 0.22.0（2026-08-20 version anchor only）](https://pypi.org/project/openai-agents/)
