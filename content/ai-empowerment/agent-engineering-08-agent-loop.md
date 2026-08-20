---
title: "Agent Loop：Turn、Step、Decide、Act、Observe 与 Stop"
slug: "agent-engineering-08-agent-loop"
date: "2026-08-20"
description: "Agent Loop 由 Host 在有界 Run 中反复提交 Step，把 Decision 经 Act、Observation 与 State 更新反馈给下一次 Decide，并独立判定停止与成功。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Agent Loop"
  - "Runtime Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 90
weight: 3090
---

> **上一篇**：[MCP 与外部能力边界：协议解决什么，宿主仍需解决什么]({{< relref "ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md" >}})

> **下一篇**：[Planning：Agent 为什么需要计划，又为什么不能迷信计划]({{< relref "ai-empowerment/agent-engineering-09-planning.md" >}})

团队把模型、一个日志解析 Tool 和一段 `while` 串起来后，很容易得到一条看起来已经闭合的链路：模型要求解析构建日志，Tool 返回 `CS0103`，模型随后说“问题已经定位”。

但这条链路至少还缺三项判断：这个结果怎样进入下一次 Decide？谁把它提交进任务 State？模型说“完成”时，目标、输出和 Evidence 是否真的满足？

前面的文章已经分别建立了这些边界：[Structured Output]({{< relref "ai-empowerment/agent-engineering-03-structured-output-machine-contract.md" >}})让候选结果可解析、可分层拒绝；[Function Calling]({{< relref "ai-empowerment/agent-engineering-05-function-calling-tool-use.md" >}})让模型表达行动意图；[Tool Runtime]({{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}})负责执行 gate、Result 与 Trace；[MCP]({{< relref "ai-empowerment/agent-engineering-07-mcp-external-capability-boundary.md" >}})标准化外部能力的协议边界。它们都没有单独证明一项任务已经形成反馈循环。

如果这篇只记一句话，我建议记住：

`Agent Loop 不是“模型调一次工具”，而是 Host 在有界 Run 中反复提交 Step：把 Decision candidate 经 Act、Tool Outcome、Observation 和 State 更新变成下一次 Decide 的输入，并独立判定 Continue / Stop 与 Success。`

> 证据范围：产品行为依据 2026-08-20 核对的 OpenAI Agents SDK Python 与 LangGraph / LangChain current hosted docs；本地行为只来自 Lab 03 的冻结 Windows / .NET deterministic fixture。本文的 Run / Turn / Step、Host-owned reducer 和 terminal contract 均为课程工作定义或设计 Proposal，不是行业统一标准。

## 为什么一次 Tool Use 还不是 Agent Loop

一次 client-executed Tool Use 可以闭合下面这段职责：

```text
Tool Call intent -> Host / Tool Runtime -> correlated Tool Result
```

它回答了“模型想调用什么”“Host 是否执行”“结果怎样关联回来”。但如果要称为本文讨论的反馈循环，结果右边还必须继续回答：

```text
Result -?-> Observation -?-> State -?-> next Decide
                              |
                              +------> Continue / Stop -?-> Success
```

问号不能靠一个 Tool success 自动补齐。假设 `parseMockLog` 成功返回 `CS0103`，它只说明这次解析产生了一个结果。任务目标若是“用日志和匹配源码解释 CS0103”，系统仍需读取对应源码、确认符号匹配并保存可复核 Evidence。协议 round trip 成功，也不能由 MCP 消息本身推出 Agent Loop、Permission、完整 Tool Runtime 或 Evidence 已经成立。

OpenAI Agents SDK Python 的 current Runner contract 展示了一种具体产品路径：模型可能给出 final output、handoff 或 tool call；tool call 执行并追加结果后，Runner 可以再次调用模型。这个事实证明该产品存在循环控制面，不证明所有 Agent 都使用相同对象、计数器或停止合同。

所以，Tool Use 可以属于 Agent Loop；一次 Tool Use 不足以证明本文定义的 Agent Loop。真正的问题已经从“工具能否被调用”转成：**谁把一次结果变成下一步输入，谁对状态和结束语义负责。**

这一区分并不是分类游戏，而会改变代码结构。若应用只把 Tool Result 拼进下一条 Prompt，下一次模型调用也许能看到它，但系统未必知道该结果是否通过 correlation、是否来自当前 invocation、是否应该改变任务事实。若应用只保存一段聊天历史，也不能据此判断目标还缺哪些条件、某次失败是否仍未解决、这次停止究竟是成功还是耗尽上限。

反馈循环因此至少包含两种反馈：一种给下一次 Decide，告诉决策源“刚才真实观察到了什么”；另一种给控制面，告诉 Host“权威 State 发生了什么变化，是否仍允许继续”。前者没有后者，系统容易被模型叙述牵着走；后者没有前者，模型又无法依据新事实调整下一步。Agent Loop 要闭合的是两者之间可追踪的转换，不只是把响应再次发送给模型。

## 先限定词义：Run、Turn 与 Step 不是天然同一个计数器

Agent 生态里最危险的术语问题，不是大家选了不同名字，而是相同名字背后使用了不同计数单位。

OpenAI Agents SDK Python 的 current API reference 把 `max_turns` 中的一次 turn 定义为一次 AI invocation；同一套 running guide 又会把完整 `Runner.run(...)` 描述成一次 logical conversation turn，而一次 run 内可以出现多次 LLM call。LangGraph 的 `super-step` 则是 graph iteration，同一 super-step 可以运行多个并行 node。三者不能直接换算。

| 术语 | 当前只允许的表述 | 不能偷换成 |
|---|---|---|
| OpenAI `max_turn` | cited Python SDK current docs 中的一次 AI invocation | 用户对话轮、Tool 调用数、Lab Step |
| logical chat turn | cited guide 对完整 run 的外部分组描述 | `max_turn` counter |
| LangGraph `super-step` | graph iteration，可以包含多个 node | 一次模型调用或一次 Lab Step |
| Lab 03 `Step` | 本文课程定义的一次 committed iteration | SDK 通用计数单位 |

为了让后文可以实现和追踪，本文采用三个**局部工作定义（PROPOSAL）**：

- **Run**：从一个冻结 Goal 和初始 State 开始，到 Host 写出 terminal record 为止的一次 goal-bounded invocation。
- **Turn**：外部交互分组。Lab 03 中一个 `turn_id` 绑定一个输入目标和一个 Run，不承担 loop counter 语义。
- **Step**：一次 committed loop iteration。它读取 step-before State 和一个 Decision candidate，提交一条 `ACT` 路径或 `REQUEST_STOP` 校验路径，并留下 before / after state version。

```text
Run R1
└─ Turn T1                  # external grouping only
   ├─ Step S1  ACT
   ├─ Step S2  ACT
   └─ Step S3  REQUEST_STOP
```

Lab 03 观测到四个 Run 都保持 `turn_index=1`，同时合计提交了十个 Step。这只能说明 fixed implementation 符合上述课程映射，不能把它升级成 glossary 全局事实，更不能要求其他 SDK 复制这套层级。

为什么必须在开写 Runtime 前先做这件事？因为 counter unit 会直接进入限制、告警和事故复盘。如果配置叫 `max_steps=8`，实现却有的路径按模型调用计数、有的路径按 Tool call 计数，那么“第九步为何发生”将无法回答。Trace 中即使留下了大量记录，也无法与配置、文档和运行结果一一对应。

更稳的做法是让名字自带语境：`openai_max_turns`、`graph_super_steps`、`committed_steps` 分别表达具体合同，只有在设计文档明确映射后才允许聚合展示。本文没有提出统一命名规范，只要求每次使用 Turn / Step 都先回答“谁在计数、何时加一、达到上限前最后允许发生什么”。

## 一个 Step 提交什么：Decide、Act、Tool Outcome、Observation 与 State

术语计数冻结后，全文中心问题才真正出现：工具返回的对象，何时才有资格影响下一步？

current product contracts 已经提供了反例。OpenAI Agents SDK 的默认 loop 会把工具结果追加给模型；LangChain 中，string tool result 可以形成 `ToolMessage`，而要更新 graph state 则可以返回 `Command`，再由 reducer 应用更新。也就是说，在这些 cited products 中，**进入后续模型输入**与**更新 authoritative state**可以是不同操作。

本文在此基础上采用一套课程设计：

```text
State(n)
   |
   v
Decide -> Decision candidate
   |
   v
Host action gate -> Act -> Tool Outcome
                          |
                          v
                correlate / normalize
                          |
                          v
                     Observation
                          |
                          v
                 Host reducer commits
                          |
                          v
                      State(n+1)
```

这里的对象不能互换：

| 对象 | 谁产生 / 提交 | 主要消费者 | 它不自动等于什么 |
|---|---|---|---|
| Decision candidate | 模型或本文的 scripted substitute | Host gate | 已授权动作、成功裁决 |
| Tool Outcome | Tool Runtime | normalizer、Trace | Observation、Evidence、任务成功 |
| model-visible item | 具体 SDK 按协议组装 | 下一次模型调用 | authoritative State |
| Observation（课程抽象） | Host 做 correlation、normalization 与安全裁剪 | reducer、下一次 Decide | 跨 SDK 标准 API、完整 Evidence |
| State | Host reducer 提交 authoritative snapshot | 后续 Step、completion validator | 模型自由文本或 Tool 内部状态 |
| Evidence | 带来源、范围和解释链的可审计依据 | Claim / completion contract | 任意 Result 或任意 ID |

**Decide 产生的是候选，不是全部决策权。** Host 仍可以检查工具名、参数、Policy、重复 action 和当前限制，再决定是否允许 Act。工具执行结束后，raw Outcome 也先经过 correlation 与 normalization；只有 reducer 才提交新 State。

这套 Host-only authoritative reducer 是 `08-C05` 的**课程安全设计 Proposal**。Lab 可以证明当前实现遵守它，却不能反向证明 OpenAI、LangGraph 或其他框架必须采用同一设计。

把 reducer 放在 Host 一侧，首先是为了让状态变化具备唯一提交点。模型可以提议 `ACT` 或 `REQUEST_STOP`，Tool 可以返回成功或失败，normalizer 可以形成 Observation，但它们都不能绕过 reducer 修改 authoritative snapshot。这样，评审者才能从 `State(n)`、Decision、Observation 追到 `State(n+1)`，并判断这次提交是否符合当前 Goal contract。

这不要求每个系统都建立一个名为 `HostReducer` 的 class。Reducer 可以是函数、actor、graph state update 或其他实现；Proposal 约束的是责任：旧 State 和已接受输入必须经过一个可审查的提交边界，不能让模型自由文本、Tool 内部对象和任务 State 共享同一个可变引用。

Observation 也不是给 Result 换一个名字。correlation 先回答“它属于哪次调用”，normalization 再回答“哪些字段可以进入任务控制面”，安全裁剪则回答“哪些内容可以进入下次 Decide”。如果这三个问题没有 owner，系统可能把迟到结果绑定到错误 Step，把 handler 的临时对象当成稳定合同，或把不应暴露的完整内容直接送进模型输入。

失败路径更能看清分层。Lab 03 的 `AL-02` 中，Tool Outcome 是 `FAILED / MOCK_PARSE_FAILED`；Host 仍成功把它正规化为 `PASS / TOOL_FAILURE / MOCK_PARSE_FAILED` 的 Observation，而且 Observation 用相同 record digest 回指 raw Outcome。这里两个 status 同时成立并不矛盾：前者说“工具执行失败”，后者说“这个失败已被可靠观察”。reducer 随后把 typed failure 写入 State，而不是把 normalization PASS 涂成工具成功。

因此，一条更稳的局部公式是：

`Tool Outcome -> correlate / normalize -> Observation -> Host reducer -> State(n+1) -> next Decide input`

它描述本文设计，不是统一 SDK payload。Observation 也只是任务推进输入；是否足以支撑某项事实主张，仍要经过独立 Evidence contract。

这也解释了 State 与 history 的差别。history 可以记录“发生过两次读取”，authoritative State 则要回答“目标所需的源码证据是否已经获得”“未解决失败还有几个”。只追加 history 会改变完整快照，却未必改变 Goal 相关事实。后面的 AL-04 正是用 full-state digest 与 goal-state digest 分离这两类变化。

## Continue / Stop 是组合判定，Stopped 不等于 Succeeded

循环可以因多种原因结束。OpenAI Agents SDK current contract 中存在 final output、handoff、`max_turns` 以及可配置的 tool-use stop behavior；LangChain 的 `return_direct` 也能在工具结果处短路 loop。这些产品事实共同反驳了“停止只来自模型吐出一个 final token”。

对本文的最小 Host 来说，停止判定分两层：

```text
Should stop?
  <- REQUEST_STOP candidate / limit / policy / error / cancellation

What outcome?
  <- output contract / goal facts / required Evidence / unresolved failures
```

`REQUEST_STOP` 只是一项 Decision candidate。Host 仍需验证输出 shape、Goal invariant、Evidence provenance 与 unresolved failure。与此同时，外部 guard 可以因为到达 limit 而终止 Run，即使模型从未请求停止。

所以 terminal 至少要拆成三个字段：

| Termination reason | Lifecycle | Outcome | 本文含义 |
|---|---|---|---|
| `GOAL_SATISFIED` | `STOPPED` | `SUCCEEDED` | Output、Goal、Evidence、未解决失败合同全通过 |
| `STOP_CONTRACT_FAILED` | `STOPPED` | `FAILED` | 候选声称成功，但事实或 Evidence 不足 |
| `UNRESOLVED_TOOL_FAILURE` | `STOPPED` | `FAILED` | State 中仍有未解决的 normalized tool failure |
| `MAX_STEPS_EXHAUSTED` | `STOPPED` | `INCOMPLETE` | 外部 Step counter 到限，不冒充完成 |
| `CANCELLED` | `STOPPED` | `INCOMPLETE` | 只保留设计边界；Lab 03 未执行该轨迹 |
| `HOST_FAILURE` | `STOPPED` | `FAILED` | Runtime 自身失败；四条 Lab 轨迹未覆盖 |

这张表是课程 terminal design，不是所有 Runtime 共用的 enum。真正可迁移的是工程判断：**boundedness 与 correctness 是两种性质。** `lifecycle=STOPPED` 只说明 Run 不再推进，不能推出 `outcome=SUCCEEDED`。

这种拆分还保留了失败后的处理空间。`FAILED` 表示 completion contract 已经得出失败结论，`INCOMPLETE` 则表示运行因上限或中断停下，但任务结论没有被涂成成功。二者在产品层可能采用不同展示和后续动作；本文不设计 retry 或 recovery，只要求 terminal record 不把不同原因压扁。

停止条件同时来自候选与控制面时，还需要明确优先级。Lab 03 只冻结了自己的顺序：每次请求新 Decision 前先检查 cancellation 与 `max_steps`；进入 `REQUEST_STOP` 后，再检查 unresolved tool failure、output、Goal 和 Evidence。这个顺序是 Lab Proposal，不是行业统一 precedence，但任何实现都必须把自己的顺序写清，否则同一 State 可能因判断先后不同得到不同 terminal。

尤其不能让“最后一个好消息”覆盖先前事实。一个 output shape 通过，只能说明结构合同满足；一个 `requested_outcome=SUCCEEDED`，只说明决策源提出了成功候选；一次 Tool success，也不能抹掉 State 中另一项 unresolved failure。completion validator 必须面对当前完整 State，而不是只看最后一条消息。

同理，计数器必须带单位。OpenAI `max_turns` 计 AI invocation，LangGraph recursion limit 计 graph super-step，Lab 03 `max_steps` 计 committed Step。它们共享“外部有界终止”的抽象，却不共享数值含义。本文也不把 `max_steps` 扩写成 token、cost 或 latency budget；那属于后续预算工程。

Cancellation 在这里只保留 cooperative boundary：Host 停止等待、发出取消请求、底层工作停止、外部副作用回滚是不同事实。Lab 03 没有运行 cancellation case；checkpoint、resume 与 recovery 留给 Article 11。

## 最小 Host Loop：每个 Step 只提交一次

不绑定具体 Agent SDK，一个可审计的最小骨架可以写成：

```text
state = start(goal)
while state.lifecycle == RUNNING:
    terminal = pre_decision_guard(state, cancellation, max_steps)
    if terminal:
        state = host_reducer.commit_terminal_once(state, terminal)
        append_terminal_trace(state)
        break

    decision = decision_source.decide(read_only(state))
    if decision.kind == ACT:
        action = host_gate(decision)
        outcome = tool_runtime.execute(action)
        observation = correlate_and_normalize(outcome)
        state = host_reducer.commit_once(state, decision, observation)
        append_step_trace(state)
        continue

    terminal = validate_completion(
        state, decision.output, goal, evidence, unresolved_failures)
    state = host_reducer.commit_terminal_once(state, terminal)
    append_step_and_terminal_trace(state)
```

其中 guard 分支在下一次 Decide 前生效；它没有消费新的 Decision，因此只提交 terminal record 并写入 terminal trace，不新增一个已消费 Decision 的 Step。

落到 C# 工程语境时，不必先寻找一个“AgentLoop 框架类”，可以先把责任面写成几个窄接口。下面仍是**课程 Proposal**，只表达变化轴，不是要求照抄名称：

| 责任面 | 最小输入 | 最小输出 | 不应拥有的权力 |
|---|---|---|---|
| `IDecisionSource` | Goal、只读 State view、上一条 Observation | Decision candidate | 执行 Tool、写 State、裁决 outcome |
| `IToolRuntime` | 已通过 Host gate 的 action | correlated Tool Outcome | 直接宣布任务完成 |
| `IObservationNormalizer` | Tool Outcome 与 correlation context | normalized Observation | 绕过 reducer 修改 State |
| `IStateReducer` | old State、Decision、Observation 或 terminal decision | new State revision | 调用真实 Tool |
| `ICompletionValidator` | State、candidate output、Goal / Evidence contract | termination reason + outcome candidate | 伪造缺失 Evidence |
| `ITraceSink` | 已提交的 Step / terminal fields | 可关联记录 | 反向改变运行事实 |

这些接口可以在一个进程、甚至一个小项目中实现，不需要为了“分层”部署成多个服务。分开的价值是让测试能替换 Decision source、注入 Tool failure、检查 reducer 前后版本，并确认 completion validator 没有读取一条未经接受的结果。Lab 03 使用 scripted substitute，正是为了把模型选择质量从 Host control-plane 的验证问题中拿走。

实际实现还需要一个 Run coordinator 维护顺序和生命周期，但 coordinator 不应成为“什么都做”的上帝对象。它可以调用这些责任面、保存当前 State 与 counter，并把已得出的 terminal 写入 Trace；每个判断本身仍应有可定位的 owner。否则类名虽然分开，所有语义仍藏在一段巨大的循环条件里，失败时还是只能看到“loop exited”。

这个顺序里有三个容易被实现细节破坏的控制点。

第一，guard 必须站在下一次 Decide 前。否则到达上限后仍会多消费一次 Decision，甚至额外触发 Tool 或副作用。第二，每个 Step 恰好提交一次 state revision，Trace 才能关联 before / after snapshot。第三，completion validator 从 State 与合同派生 outcome，不能复制 `requested_outcome=SUCCEEDED`。

这只是 Article 08 / Lab 03 的**最小 Proposal 骨架**，不是 production reference architecture。它没有 planning queue、workflow node、checkpoint persistence、parallel branch 或 human approval wait；这里的 Trace 也只承担 Step correlation，完整 Trace / Replay / Eval 仍属于后续课程。

所谓“每个 Step 只提交一次”，不是要求一次 Step 只能写一行日志，而是要求 authoritative state transition 只有一个确定的 after version。实现当然可以记录多个内部事件；但当 Step 对外可见时，应能指出唯一的 `revision_before -> revision_after`。若 Tool 先改一次 State，normalizer 又改一次，最后 completion 代码再补一次，评审者就无法判断哪一份 snapshot 是下次 Decide 真正看见的事实。

Trace 的职责也因此变得具体：它至少要把 Decision ID、invocation correlation、Observation reference、state version、control decision 与 terminal fields 串起来。日志很多不代表链路完整；缺少其中任一关联时，团队仍可能只能凭时间邻近猜测“这条 Result 大概影响了那个 State”。

pre-decision guard 则承担副作用前的最后一道外部边界。检查得太晚，即使随后正确写出 `MAX_STEPS_EXHAUSTED`，多取得的 Decision 或多执行的 Tool 也已经成为真实历史。停止标签不能让已经发生的动作倒退成 `NOT_RUN`。

State version 则为这些责任提供共同锚点。每个 Step 都读取明确的 before revision，并在成功提交后得到 after revision；下一次 Decide 只接收 after snapshot。若系统允许一条迟到 Observation 悄悄写回旧 revision，就必须另有冲突处理合同；Lab 03 没有覆盖并发或迟到结果，所以本文只保留 sequential、single-action-per-Step 的边界，不从当前 trace 推导并发安全。

## Lab 03：一条正例与三条反例

Lab 03 没有调用真实模型或 Provider，而是使用 `ScriptedDecisionSource v1`。这样做牺牲了模型行为真实性，换来对 Host control-plane 的直接、可复现检验：script 只能依序提出候选，不能写 State、执行 Tool 或计算 authoritative outcome；`cases.json` 也不含 expected termination、outcome、success、count 或 digest 字段，避免 Runner 从输入里照抄答案。

> 以下结果只适用于 2026-08-20 冻结的 Windows 10.0.19045 / .NET SDK 10.0.301 / Host 10.0.9 / net10.0 环境、固定 fixture、ScriptedDecisionSource v1 与当前 fixed Host 实现。两次 fresh-process 运行的六个 normalized artifacts 逐文件 byte-identical；这证明当前 deterministic fixture 可复现，不证明真实模型或 Provider 的 determinism、planning quality 或 production reliability。

每次 formal run 得到 `4 cases / 10 STEP / 4 TERMINAL / 10 state snapshots / 7 Tool Outcomes / 7 Observations / 7 tool calls / 10 decision calls / 1 SUCCEEDED`。这些 count 用来检查 artifact 完整性，不是性能指标。

两次 fresh process 各自产生六个 normalized artifacts：manifest、case results、Observations、State snapshots、Tool Outcomes 与 Trace。对应文件逐 byte 相等，说明固定输入、当前 binary 与归一化规则没有把 wall-clock、PID、绝对路径或随机 ID 混入这些 artifacts。它证明的是可复查性，不是系统面对未知输入时行为稳定，更不是模型输出可复现。

六类 artifact 各自回答不同问题。Tool Outcome 保存原始执行 disposition 与 correlation；Observation 保存 normalizer 接受的下一层对象；State snapshot 保存 reducer 提交后的权威事实；Trace 把 before / after、Decision、Outcome 与 Observation reference 串起来；case result 只汇总最终 terminal；manifest 则用于检查文件与 digest 完整性。若只保留最后一份 case result，读者能看到“FAILED”，却无法复核失败是怎样从 raw Outcome 传到 State 和 terminal 的。

| Case | Observed terminal | 这条轨迹负责证明什么 | 明确限制 |
|---|---|---|---|
| `AL-01` | `GOAL_SATISFIED / SUCCEEDED` | 唯一正例；Goal、Output、Evidence、unresolved-failure contract 全部 PASS | 只证明 fixed fixture 的成功 |
| `AL-02` | `UNRESOLVED_TOOL_FAILURE / FAILED` | failed Outcome 可成为 failure Observation；后续 requested success 不能涂绿失败 | 没有实现 recovery |
| `AL-03` | `MAX_STEPS_EXHAUSTED / INCOMPLETE` | limit 在下一次 Decide 前生效；有界停止不等于成功 | `max_steps` 不是 SDK turn 或成本预算 |
| `AL-04` | `STOP_CONTRACT_FAILED / FAILED` | 识别语义重复与 no progress；拒绝 fake Evidence 和伪完成 | fingerprint 与 contract 只属于 fixed Host |

### AL-01：成功需要合同闭合

前两个 Step 分别解析 `build.log`、读取匹配的 `BuildMenu.cs`。两个 Tool Outcome 经 Observation 产生 `EV-LOG-001` 与 `EV-FILE-001`，reducer 提交 `CS0103`、路径、行号、符号和 `source_match=true`。第三个 Step 才请求停止。

最终 `output_contract_status=PASS`、`success_contract_status=PASS`，terminal 为 `GOAL_SATISFIED / SUCCEEDED`。它不是因为模型“说完成”而成功，而是因为 fixed completion contract 在当前 State 上全部通过。

这里还可以看到 Structured Output 的正确位置：第三个 Decision 的 output shape 是 completion contract 的一个输入，不是整个成功合同。若 `status`、`summary` 或 Evidence IDs 结构不合法，Host 可以拒绝；即使结构合法，Evidence provenance 和 State facts 仍必须单独成立。

### AL-02：观察到失败，不等于修复了失败

named fault 让 `parse_mock_log` 返回 typed failed Outcome：`MOCK_PARSE_FAILED`。Observation normalization 为 PASS，并精确引用该 Outcome 的 record digest；State 则保留一个 unresolved tool failure，Goal Evidence 仍为空。

下一 Step 的候选请求 `SUCCEEDED`，输出 shape 本身也通过，但 Host 派生的 terminal 仍是 `UNRESOLVED_TOOL_FAILURE / FAILED`。这条轨迹把三个结论分开：失败被看见、失败进入 State、失败仍未解决。

如果实现把 Observation 的 `normalization_status=PASS` 直接映射成 Tool success，这个 case 就会被错误涂绿。正确的读取方式是沿 digest 回看来源：Observation 的处理成功，引用的是一条 disposition 为 FAILED 的 Tool Outcome；State 因此应保留 typed failure，而不是产生 Goal Evidence。

### AL-03：上限必须在下一次 Decide 前生效

这个 case 的 `max_steps=2`。两个 ACT 分别解析日志和读取无关源码，第二次读取没有满足仍缺失的 source requirement。随后 pre-decision guard 直接写出 `MAX_STEPS_EXHAUSTED / INCOMPLETE`。

raw artifact 显示 `steps=2 / decisions=2 / tools=2`，第三个 `al03-decision-03` 仍在 `remaining_decision_ids`。这比“没有第三次 Tool call”更强：第三个 Decision 本身就没有被消费，避免了 off-by-one 后再补停止标签。

第二个 Step 虽然成功读取了 `Unrelated.cs`，也产生了非目标 Evidence，但 required source requirement 仍未满足。这进一步说明 Tool success 与 Goal progress 是两条判断：动作可以成功执行，任务仍可以在到达边界时保持 `INCOMPLETE`。

### AL-04：历史变化不等于目标进展

前两个 Step 用不同 invocation ID 两次读取同一份无关源码。两次 action fingerprint 相同，semantic payload digest 相同；因为 correlation 信息不同，result-record digest 不同。每次写入历史后 full-state digest 都变化，但 goal-state digest 始终不变，两步都被标成 `NO_PROGRESS`。

第三个候选用 `EV-FAKE` 请求成功。Host 拒绝了这个没有 Tool Outcome / Observation provenance 的 ID，同时 Goal 所需的日志与匹配源码也仍缺失，最终是 `STOP_CONTRACT_FAILED / FAILED`。

这条轨迹不证明一套通用重复检测算法，更不证明 planning quality。它只证明 current fixed Host 没有把 invocation ID 差异或 history 增长误当成 Goal progress，也没有接受模型自报的 fake Evidence。

action fingerprint 排除 invocation ID，才能让“同一语义动作的再次送达”可比较；result-record digest 则保留 correlation，因此两次记录仍然不同。两个 digest 的职责并不冲突：前者服务于语义重复判断，后者服务于具体执行追踪。本文只使用 frozen canonicalization，不把它包装成跨语言标准。

### 实验结论必须连同失败历史保存

最终 locked restore、Release build、BCL spec、run-a、run-b 与 independent verifier 都 exit `0`，build 为 `0 warnings / 0 errors`。但 execution log 同时保留了 CIM access denied、一次 compile-name collision、fixture EOF 多余空行、不可用 NuGet testhost 路径和一次 live-reference snapshot digest mismatch。修正后的 green chain 支持当前 source 与 final artifacts，不会把这些失败升级成 production recovery evidence。

两次 Master interruption 发生在 formal commands 已结束后的 Markdown 交付阶段，当时没有 Lab command 在运行。因此它们是 orchestration / log-delivery interruption，不是 runtime cancellation observation。

这些限制共同决定了正文能使用的动词。可以说“fixed Host 在四条冻结轨迹中区分了四种 terminal”“两次 normalized artifact 逐文件一致”；不能说“Agent 已经可靠”“模型会从失败恢复”“Provider 行为可复现”或“生产环境不会无限循环”。前两句有 raw artifact 与 Researcher Evidence Merge 支撑，后四句都需要本 Lab 没有提供的新 Evidence。

可复核入口包括 [Lab 03 Design / Observation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/README.md)、[execution log](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/execution-log.md)，以及 run-a 的 [terminal results](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/case-results.jsonl)、[Tool Outcomes](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/tool-outcomes.jsonl)、[Observations](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/observations.jsonl)、[State snapshots](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/states.jsonl)与 [Trace](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl)。

## 一个坏 Loop 通常怎么坏

坏实现往往不是少写一个 `while`，而是把不同边界压成几个布尔值：

1. 把模型调用、Tool 调用和 graph tick 都叫 `step`，导致 limit 与 Trace 无法解释。
2. 把 raw Tool Result 直接写进 authoritative State，绕过 correlation、normalization 和 reducer。
3. 只看 history / full-state 是否变化，不看 goal-state 是否进展，让重复动作伪装成工作。
4. 接受任意 Evidence ID 或模型自报完成，让结构正确的输出制造 pseudo-success。
5. 在取得下一次 Decision 后才检查 limit，产生 off-by-one，甚至多一次副作用。
6. 用 `done=true` 同时表达 `STOPPED`、`SUCCEEDED`、`FAILED` 和 `INCOMPLETE`。

AL-02 直接反驳第 2、4、6 项；AL-03 暴露第 5 项；AL-04 暴露第 3、4 项。它们是当前 confirmed boundary 与 fixed Lab 轨迹支持的 design-review heuristics，不是所有 Agent failure mode 的穷举。

把这份清单用于 code review 时，可以连续追问六个问题：当前 counter 的单位是什么？Decision 是否只是 candidate？每个 Outcome 怎样关联成 Observation？谁提交唯一 State revision？Continue / Stop 在下一次副作用前还是后判定？terminal 是否同时保存 lifecycle、reason 与 outcome？如果实现只能回答“框架会处理”，就还需要回到该框架的具体合同和运行证据，不能用 Agent 这个名字替代责任说明。

## 工程边界：最小 Loop 不应吞掉整个 Agent System

Article 08 只建立一次有界 Run 内的 committed Step、`Result -> Observation -> State` 转换，以及 Continue / Stop 与 terminal semantics。它位于 Model / Tool Runtime 之上，但还远未等于完整 Agent Runtime 或 Harness。

本篇明确负责：

- Run / Turn / Step 的局部 scope；
- Decision candidate、Host action gate 与每 Step 一次 state commit；
- Tool Outcome、Observation、State 与 Evidence 的分层；
- Continue / Stop、termination reason 与 outcome；
- max-step boundedness 与 pseudo-completion 拒绝。

本篇明确不负责：

- **Planning**：plan decomposition、plan quality、replanning 与 search strategy留给 Article 09；
- **Workflow / State Machine**：确定性编排、branch 与 compensation 留给 Article 10；
- **Long-running**：checkpoint、retry、cancellation trajectory、resume、recovery 与 human approval wait 留给 Article 11；
- **Context / Memory**：packing、compaction、working memory、session、long-term memory 与 RAG 留给 Article 12+；
- **Multi-Agent**：delegation、handoff topology 与共享状态治理不在本篇；
- **Budget**：`max_steps` 不扩写成 token、cost、latency budget engineering；
- **DSH / BuildPilot**：不借用 DeepSeek Harness 源码证明通用模型，也不实现或预演 BuildPilot Runtime；
- **生产可靠性**：Provider、network、MCP、permission、外部副作用、production load、完整 Trace / Replay / Eval 均未由 Lab 03 观察。

这条边界让下一篇的问题更清楚：本篇回答“当前 State 下怎样安全推进一步并决定是否停止”；Article 09 才回答“怎样形成和修订跨多步的计划”。

## Claim-to-section traceability

| Claim | Evidence 中的最终状态 | 正文主落点 | Draft disposition |
|---|---|---|---|
| `08-C01` | `CONFIRMED / PRODUCT-SCOPED` | Tool Use 缺口、停止判定 | 保留 OpenAI Python current contract scope |
| `08-C02` | `CONFIRMED / CITED-PRODUCTS-SCOPED` | Run / Turn / Step | 不做跨产品单位换算 |
| `08-C03` | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` | 局部术语、最小骨架 | Proposal 标签保留 |
| `08-C04` | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` | Outcome / Observation / State | 产品事实与 Lab conformance 分开 |
| `08-C05` | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` | Host-owned reducer | 不升级为框架强制要求 |
| `08-C06` | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` | Continue / Stop、AL-03 | limit unit 与 success 分开 |
| `08-C07` | `CONFIRMED / FIXED-HOST-FIXTURE-SCOPED` | terminal contract、AL-01/02/04 | 只用于 fixed Host / fixture |
| `08-C08` | `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED` | Lab 四轨迹与复现性 | 不外推真实模型或生产 |

Coverage：`8 / 8`。正文没有新增 Claim，没有把 Proposal 升格为行业事实，也没有把 deterministic fixture 写成 Provider 或 production evidence。

## Learning Check

1. 某 SDK 配置 `max_turns=8`，能否直接写成“允许八个 Tool Step”？为什么？
2. Tool 返回 `{status: ok}` 后，它成为下一次 Decide 输入前至少要跨过哪些层？谁提交 authoritative State？
3. `lifecycle=STOPPED` 之外，至少还需要检查哪些字段和合同，才能判断任务是否成功？
4. 相同语义动作使用不同 invocation ID 执行两次，full-state digest 变化而 goal-state digest 不变，随后 Decision candidate 请求成功，应怎样分类？
5. 为什么把 replanning、workflow branch、checkpoint / resume 与 token budget 全塞进这个最小 Loop，会破坏课程边界？

### 参考思路

1. 不能。先查产品 counter unit；cited OpenAI Python SDK 的 `max_turn` 是 AI invocation，不等于 Tool call 或 Lab Step。
2. 至少区分 Tool Outcome、correlation / normalization、Observation、Host reducer 与 State；Host-owned reducer 是本文 Proposal，不是 universal API。
3. 检查 termination reason、outcome、output contract、Goal / Evidence invariant 与 unresolved failure，不能从 STOPPED 直接推出 SUCCEEDED。
4. 先判定 repeat / no progress，再按 completion contract 验证。在 Lab 03 fixed contract 中应为 `STOP_CONTRACT_FAILED / FAILED`，不能因 history 变化判成功。
5. 它们分别属于 Article 09、10、11 与 20；提前吞入会让最小 Step 责任与后续系统职责失去边界。

## 最短结论

`一个 Agent Loop 是否可靠，不看它有没有 while，而看每个 Step 是否可提交、每个 Observation 是否可追溯、每次停止是否与成功分开判定。`

## 参考资料

- [OpenAI Agents SDK Python：Running agents](https://openai.github.io/openai-agents-python/running_agents/)
- [OpenAI Agents SDK Python：Run reference](https://openai.github.io/openai-agents-python/ref/run/)
- [OpenAI Agents SDK Python：Agents / tool use behavior](https://openai.github.io/openai-agents-python/agents/)
- [PyPI：openai-agents 0.22.0](https://pypi.org/project/openai-agents/)
- [LangGraph：Graph API overview](https://docs.langchain.com/oss/python/langgraph/graph-api)
- [LangChain：Tools](https://docs.langchain.com/oss/python/langchain/tools)
- [LangGraph：Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents)
