# Article 09 Research

## Status

- Gate：`RESEARCH`
- Research Status：`COMPLETE / EVIDENCE_GATE_CANDIDATE`
- Owner：`RESEARCHER`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`
- Retrieved Date：`2026-08-20`
- Core Claim Summary：`5 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`

## Frozen scope reconciliation

本篇是 Article 08 Agent Loop 到 Article 10 State Machine / Workflow 的桥。Article 08 已把一次有界 Run 内的 `Decision candidate -> Act -> Tool Outcome -> Observation -> authoritative State -> Continue / Stop` 分开；Article 09 只新增跨多个 Step 的候选步骤表示及其修订，不重讲 Tool Runtime，也不提前展开确定性 Workflow、Checkpoint、Retry、Cancellation 或 Recovery。

本轮只使用一手来源：原论文、官方文档、官方仓库示例与本仓已冻结 raw artifacts。产品名、版本、课程工作定义和 fixture observation 分层记录。`Implicit Plan / Visible Plan / Structured Plan` 与 `Keep / Revise / Replace` 是课程 taxonomy，不宣称行业统一；Chain-of-Thought 不作为必须保存的 Plan artifact。

## Answered Research Questions

### RQ-09-01｜Planning 为什么会出现在多步 Agent 中？

**Answer：ANSWERED / NARROWED。** 当目标包含依赖、顺序和执行前未知量时，系统需要表达“接下来准备做什么”以及哪些前提仍未成立。ReAct 原论文展示 reasoning 与 action / observation 交错，并明确提到根据外部信息追踪、更新 action plan 与处理 exception；Plan-and-Solve 则把“先拆成子任务，再按计划执行”作为两段式 prompting。两者支持 Planning 能表达跨步意图，不支持“有 Plan 就会事实正确或执行成功”。

本课程因此把 Plan 收窄为：`Goal + Current Evidence` 条件下，对**剩余候选行动**的表示。它帮助避免每一步完全失忆式地局部选择，但其内容仍可能错误、过时或越权。

### RQ-09-02｜Implicit、Visible 与 Structured Plan 怎样区分？

**Answer：ANSWERED / COURSE TAXONOMY。** 三者按可观察合同区分，不按“模型有没有想过”区分：

| 形态 | 本课程工作定义 | 可审计面 | 不自动拥有的能力 |
|---|---|---|---|
| Implicit Plan | 没有独立、持久化 Plan object；下一意图从当前 loop / history 中逐步形成 | Decision / Tool call / result sequence | 完整长期步骤、revision diff |
| Visible Plan | 面向人或 Trace 的剩余步骤列表 | plan version、item、reason | 机器可校验 schema、授权 |
| Structured Plan | 有 schema 的步骤、依赖、状态或 version artifact | parser、validator、diff、consumer | 执行、Verified State、Workflow invariant |

Microsoft Semantic Kernel current planning page把 function-calling feedback loop 作为主要 planning / execution 方式，并记录旧 Stepwise / Handlebars planners 已移除；LangGraph.js 官方 Plan-and-Execute notebook则显式保存 `plan: string[]` 与 `pastSteps`。这两个一手实现共同反驳“Planning 必须有统一的显式 Planner / Plan class”。

### RQ-09-03｜ReAct、Plan-and-Execute、Planner / Executor 各强调什么？

**Answer：ANSWERED / PARTIAL。** 这些词跨论文与产品存在漂移，只能作轻量控制方式比较：

| Pattern | 强调点 | 当前证据允许的最小表述 | 不能推出 |
|---|---|---|---|
| ReAct | reasoning、action、observation 交错 | 原论文中 reasoning 可创建、追踪、更新高层 action plan，并利用 external observation | 所有 ReAct runtime 都保存结构化 Plan；公开完整 CoT 是生产要求 |
| Plan-and-Solve | 先分解，再按计划解题 | 原论文是 prompting strategy，用于多步 reasoning task | 等同工具型 Agent 的 Plan-and-Execute architecture |
| Plan-and-Execute | 先生成多步 Plan，再逐项执行并重访剩余 Plan | LangGraph.js 官方 notebook分别保存 `plan`、`pastSteps`，执行 `plan[0]`，再进入 `replan` | 是当前 LangGraph 唯一推荐架构；示例已完成权限、Evidence 或生产验证 |
| Planner / Executor | 把“生成/修订候选 Plan”和“执行已选步骤”拆成责任面 | cited notebook 的 `planner`、`agentExecutor`、`replanner` 是不同 runnable / node | Planner 必须是另一个 Agent、另一个模型或另一个进程 |

因此 `09-C03` 保持 `PARTIAL / PATTERN-SCOPED`；正文不得把这张表写成行业分类标准。

### RQ-09-04｜什么时候 Keep、Revise 或 Replace？Re-planning 与 Retry 有何不同？

**Answer：ANSWERED / MIXED EVIDENCE。** ReAct 与 LangGraph.js example 都支持“新 observation / past result 可以更新剩余计划”。本课程进一步提出一个可审查的 decision taxonomy：

- `KEEP`：新 Observation 没有否定剩余 Plan 的前提，只提交当前进展并保留剩余步骤；
- `REVISE`：目标不变、主路径仍成立，但剩余步骤、顺序、参数或前提需局部修改；
- `REPLACE`：关键前提、目标或允许的执行路径已失效，废弃剩余 Plan，建立另一条候选路径；
- `STOP / ESCALATE`：没有安全候选路径，或需要授权 / 人类输入，不得硬凑新 Plan。

`KEEP / REVISE / REPLACE` 是 `PROPOSAL`，不是来源中的标准 enum。Retry 只表示在既定 retry policy 下再次尝试相同意图；Re-planning 改变的是后续候选意图或序列。具体 retry、backoff 与恢复语义留给 Article 11。

### RQ-09-05｜为什么 Plan 不等于 Execution、Observation、Verified State、Authorization 或 Workflow？

**Answer：ANSWERED。** 对象按 producer / authority 分开：

| Object | 最小含义 | 事实来源 / owner | Plan 能否替代 |
|---|---|---|---|
| Plan candidate | 接下来可能做什么 | Planner role / model / code | — |
| Execution | 某 action 是否真实发出并由 runtime处理 | Tool Runtime / executor trace | 否 |
| Observation | action outcome 经关联与正规化后观察到什么 | Article 08 Host boundary | 否 |
| Verified State | 已接受的 Observation / Evidence 对权威任务事实造成什么更新 | reducer / verifier | 否 |
| Authorization | 该 action 是否被当前 policy / approval允许 | policy / guardrail / human approval | 否 |
| Workflow | 允许的 stage、edge、guard 与 invariant | 程序 / orchestration definition | 否 |

LangGraph.js example在 state 中把 `plan` 与 `pastSteps` 分开，并由 graph edges控制 `planner -> agent -> replan`；2026-08-20 retrieved current official OpenAI Agents SDK docs显示 model-emitted tool call仍可能等待 approval、被人拒绝，或被 tool guardrail跳过 / 替换 / tripwire 阻断。其中 tool guardrail 行为限定于 custom `function_tool` pipeline，不覆盖 hosted / built-in tools 或 handoff；HITL 也受 tool-type 支持范围约束。`openai-agents 0.22.0` 仅作为当日 PyPI / tag version anchor，docs-current 与 tag 未做逐项 source mapping。这些都是产品范围证据，不是跨 SDK 的统一 schema。

### RQ-09-06｜哪些机制有权拒绝或收窄模型计划？

**Answer：ANSWERED / PRODUCT-SCOPED + COURSE PROPOSAL。** 至少可区分：

1. capability availability：工具可以按 runtime context启用 / 禁用；
2. tool input guardrail / policy：执行前校验，可跳过、返回拒绝信息或终止；
3. human approval：敏感调用暂停，批准后才执行，拒绝结果返回 run；
4. workflow guard / invariant：程序只允许定义好的 edge / transition；
5. evidence / completion gate：没有所需 Observation / Evidence 时不得把 Plan item记成已完成；
6. budget / limit：可以禁止继续消费动作，但具体预算工程留给 Article 20。

前 1—3 有 2026-08-20 retrieved current official OpenAI Agents SDK docs 的产品范围证据；其中 tool guardrail 只覆盖 custom `function_tool` pipeline，不覆盖 hosted / built-in tools 或 handoff，HITL 支持范围也由 tool type 决定。第 4 有 LangGraph.js example structure；第 5—6 是课程控制面 Proposal 与 Article 08 已有边界。不得声称所有 SDK 在同一顺序实现这些 gate。

### RQ-09-07｜是否必须把 Chain-of-Thought 持久化为 Plan？

**Answer：ANSWERED / NO。** cited sources已经出现多种不等价载体：ReAct 研究轨迹包含 verbal reasoning；LangGraph.js example保存紧凑的 `string[] plan` 和 `pastSteps`；Semantic Kernel current guidance甚至可以只通过 function-call / result feedback loop形成 next decision。由此只能推出“Plan artifact 可以独立设计”，不能要求保存模型完整私有推理。课程要求持久化的是可审计候选步骤、version、change reason 与 evidence reference，而不是 Chain-of-Thought。

### RQ-09-08｜能否取得一条 Observation 反证 Initial Plan 的 bounded trace？

**Answer：ANSWERED / FIXTURE-SCOPED。** 复用 Article 08 Lab 03 `AL-02` raw artifacts建立 planning overlay，未修改 Lab、未声称 fixed Host 自带 Planner：

1. `Initial Plan v1`：`PROPOSAL`；先解析 `build.log`，解析成功后读取匹配源码，最后验证 Goal Evidence。
2. `Executed Step`：`OBSERVED`；AL-02 step 1执行 `parse_mock_log`。
3. `Tool Outcome`：`OBSERVED`；`FAILED / MOCK_PARSE_FAILED / FI_PARSE_TYPED_FAILURE`。
4. `Observation`：`OBSERVED`；`AL-02/step-01` 为 `TOOL_FAILURE`，normalization `PASS`，无 Evidence ID。
5. `Verified State`：`OBSERVED`；revision 1仍有 `REQ_LOG / REQ_SOURCE`，`unresolved_tool_failure_count=1`，accepted Goal Evidence为空。
6. `Plan decision`：`PROPOSAL / REPLACE`；“读取匹配源码”的前提（已经取得 diagnostic path / symbol）未成立，废弃 v1 剩余路径；候选 v2先定位 / 解除 parse failure，无法在授权与预算内解除则 STOP / ESCALATE。

该 overlay 证明当前 raw Observation 与 State 足以**反驳 v1 前提**，并使 Replace recommendation可审查；它不证明 Lab 03 runtime执行过 re-planning、模型会生成 v2、retry必然失败或 v2能够成功。

## Source Manifest

| ID | Source | Kind | Version / date scope | Retrieved | Used for | Key limitation |
|---|---|---|---|---|---|---|
| `09-S01` | [ReAct: Synergizing Reasoning and Acting in Language Models](https://arxiv.org/abs/2210.03629) | Original paper | arXiv `2210.03629v3`, 2023-03-10; ICLR 2023 | 2026-08-20 | interleaved reason / act / observe；plan update与exception | research setup，不是当前 SDK contract；不要求结构化 Plan或生产公开 CoT |
| `09-S02` | [Plan-and-Solve Prompting](https://arxiv.org/abs/2305.04091) | Original paper | arXiv `2305.04091v3`, 2023-05-26; ACL 2023 | 2026-08-20 | “先拆计划、再执行子任务”的原始 prompting definition | reasoning benchmark，不是 tool runtime / planner-executor系统证据 |
| `09-S03` | [LangGraph.js Plan-and-Execute official notebook](https://raw.githubusercontent.com/langchain-ai/langgraphjs/main/examples/plan-and-execute/plan-and-execute.ipynb) | Official repository example | unpinned `main` as retrieved；notebook内含 historical model/API names | 2026-08-20 | `plan`、`pastSteps`、planner / executor / replanner、graph routing | 未执行 current compatibility；不是唯一推荐 architecture或生产保证 |
| `09-S04` | [Semantic Kernel Planning](https://learn.microsoft.com/en-us/semantic-kernel/concepts/planning) | Official documentation | page last updated 2025-06-11；current retrieval | 2026-08-20 | automatic function-calling planning loop；legacy planner removal | 产品术语；页面未绑定本轮某个 package build |
| `09-S05` | [OpenAI Agents SDK Guardrails](https://openai.github.io/openai-agents-python/guardrails/) | Official documentation | current official docs as retrieved 2026-08-20 | 2026-08-20 | custom `function_tool` pre/post guardrails can block / skip / tripwire | 不覆盖 hosted / built-in tools 或 handoff；未与 `v0.22.0` tag逐项source mapping |
| `09-S06` | [OpenAI Agents SDK Human-in-the-loop](https://openai.github.io/openai-agents-python/human_in_the_loop/) | Official documentation | current official docs as retrieved 2026-08-20 | 2026-08-20 | model tool call可暂停、批准或拒绝 | SDK-specific；HITL approval scope与tool type有关；未与 `v0.22.0` tag逐项source mapping |
| `09-S07` | [PyPI openai-agents](https://pypi.org/project/openai-agents/) | Official package registry | `0.22.0`, uploaded 2026-08-19；source tag `v0.22.0`, commit `4df9ecfae1761ca6fea67cc5a20b383c1d492024` | 2026-08-20 | 当日 PyPI / tag version anchor only | docs-current与tag代码未逐项做 source mapping；不单独证明guardrail / HITL行为 |
| `09-S08` | [Article 08 published content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md) | Repository dependency | Article 08 completion `d4693bd6d78ed63a669e181516e28247460fee11` | 2026-08-20 | Decision candidate、Observation / State、terminal边界 | 课程工作定义与 fixed fixture scope，不是行业标准 |
| `09-S09` | [Lab 03 run-a trace](../../labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl)、[Tool Outcomes](../../labs/lab-03-minimal-agent-loop/observations/run-a/tool-outcomes.jsonl)、[Observations](../../labs/lab-03-minimal-agent-loop/observations/run-a/observations.jsonl)、[States](../../labs/lab-03-minimal-agent-loop/observations/run-a/states.jsonl) | Frozen repository observation | Lab 03 fixed Windows / .NET deterministic fixture, run-a | 2026-08-20 | `AL-02` planning overlay 的 observed execution / state | fixture无 Planner；Plan v1/v2与Replace均为本篇 Proposal |

## Counter-evidence and terminology drift

1. **显式 Planner 不是必要条件。** Semantic Kernel 已移除 Stepwise / Handlebars planners并转向 function-calling loop；因此不得把“有独立 Planner class”写成 Planning 定义。
2. **ReAct 不是 Plan-and-Execute 的别名。** ReAct强调逐步交错；Plan-and-Execute example先产生多步列表再逐项执行 / replan；两者都能调整方向，但控制节奏不同。
3. **Plan-and-Solve 不是 Agent runtime architecture。** 原论文是 prompting method；只能用于“先规划再求解”的历史概念来源。
4. **Planner / Executor 是责任分离，不是部署拓扑。** LangGraph.js example中 planner是 prompt + structured model runnable，executor是 ReAct agent runnable；不能推出必须多 Agent或多模型。
5. **计划更新不保证正确。** ReAct paper自身保留失败 trajectory；LangGraph notebook仅示例结构；二者都不能证明任意 revision都会提升结果。
6. **guardrail / approval scope不统一。** 2026-08-20 retrieved current official OpenAI docs把 tool guardrails限定在 custom `function_tool` pipeline，不覆盖 hosted / built-in tools 或 handoff；HITL支持范围也由 tool type决定。`0.22.0` 仅是当日 PyPI / tag version anchor，docs-current与tag未做逐项source mapping；正文不能写成全部 Tool Runtime的统一机制。
7. **Lab 03 没有 Planning runtime。** AL-02只提供 Outcome / Observation / State；本篇添加的 Plan版本与Replace decision必须始终标 `PROPOSAL / ANALYSIS OVERLAY`。

## Candidate Claim Register Input

| Claim ID | Candidate wording | Status candidate | Source mapping | Course use |
|---|---|---|---|---|
| `09-C01` | 本课程把 Plan 定义为 Goal与Current Evidence条件下的剩余行动候选表示；可隐式、可见或结构化 | `PROPOSAL` | S01—S04 | 全文最小模型 |
| `09-C02` | cited products显示Planning可以没有独立显式Plan object，也可以保存结构化Plan；实现形态不是单一标准 | `CONFIRMED / CITED-PRODUCTS-SCOPED` | S03、S04 | implicit / visible / structured边界 |
| `09-C03` | ReAct、Plan-and-Solve / Plan-and-Execute、Planner / Executor强调不同控制节奏，不能做API一一映射 | `PARTIAL / PATTERN-SCOPED` | S01—S04 | 轻量模式比较 |
| `09-C04` | 新Observation或已执行结果可以成为更新剩余Plan的输入；AL-02中的failure observation反驳v1下一步前提 | `CONFIRMED / SOURCE + FIXTURE-SCOPED` | S01、S03、S09 | revision触发与bounded trace |
| `09-C05` | Keep / Revise / Replace / Stop是本课程的plan disposition taxonomy | `PROPOSAL` | C04的抽象 | revision decision table |
| `09-C06` | 在cited implementation与fixture中，Plan、executed result / past step、Observation、State与Workflow routing是可分对象 | `CONFIRMED / CITED-IMPLEMENTATION + FIXTURE-SCOPED` | S03、S08、S09 | 边界模型 |
| `09-C07` | 2026-08-20 retrieved current official OpenAI Agents SDK docs显示，model-emitted tool call仍可被approval或tool guardrail暂停、拒绝或阻断；tool guardrail限于custom `function_tool` pipeline，HITL受tool-type支持范围约束 | `CONFIRMED / OPENAI-CURRENT-OFFICIAL-DOCS-RETRIEVED-2026-08-20` | S05—S07；`0.22.0`仅为当日PyPI / tag version anchor，docs-current与tag未逐项source mapping | Plan / call不等于Authorization |
| `09-C08` | 可审计Plan应持久化候选步骤、version、change reason与evidence reference，而非要求保存完整Chain-of-Thought | `PROPOSAL` | S01、S03、S04 | Plan artifact design |
| `09-C09` | Plan或Plan item本身不能证明action已执行、事实已验证或目标已成功 | `CONFIRMED / OBJECT-CONTRACT + FIXTURE-SCOPED` | S03、S08、S09 | learning check与坏实现 |

## Explicit non-scope

- 不做 Planning 论文综述、tree search / MCTS / beam search比较或 planner benchmark。
- 不把 ReAct reasoning trace写成所有产品必须暴露 / 持久化的 Chain-of-Thought。
- 不把 LangGraph.js example写成 current API compatibility或生产推荐证明。
- 不展开 Article 10 的 State Machine、Workflow invariant、compensation或Agent Decision Point。
- 不展开 Article 11 的 Retry、Checkpoint、Cancellation、Resume与Recovery。
- 不实现新 Lab，不修改 Lab 03，不执行模型 / Provider，不宣称本篇观察了自动 re-planning。
- 不读取 DSH，不实现 BuildPilot Runtime。

## Return-to-research conditions

出现以下任一项时必须返回 `RESEARCH`：

1. Draft要声称某 SDK / Agent普遍具有显式 Plan object；
2. 要把 ReAct、Plan-and-Solve与Plan-and-Execute写成严格等价；
3. 要声称 AL-02 runtime自动生成或执行 Revised Plan；
4. 要把 Tool call / Plan item写成已授权、已执行、已验证或已成功；
5. 要给 OpenAI guardrail / approval超出 cited tool scope的统一保证；
6. 要引入 Planning algorithm性能、模型质量或生产可靠性结论。

## Research Gate Checklist

- [x] Article Card questions均已 answered / narrowed；无未记录 blocked question。
- [x] Source manifest记录 URL、title、date / version、retrieved date与限制。
- [x] 术语漂移与 counter-evidence已保存。
- [x] Claim Register input含 exact wording、status、scope、source mapping与course usage。
- [x] `Plan / Execution / Observation / Verified State / Authorization / Workflow` 已分离。
- [x] 至少一条 bounded overlay trace含 observed failure与 `PROPOSAL` Plan replacement。
- [x] Article 10 / 11与Chain-of-Thought non-goal保持冻结。

## Researcher recommendation

`PASS_RECOMMENDED -> EVIDENCE_GATE -> OUTLINE`。所有核心行为性 Claim均有一手资料或 bounded observation；无 `BLOCKED`。`09-C03` 已按 pattern scope收窄，`09-C01 / C05 / C08` 保持 `PROPOSAL`，不得在后续升级为行业事实。Master仍需独立验证 artifact、Allowed Writes与Gate transition。
