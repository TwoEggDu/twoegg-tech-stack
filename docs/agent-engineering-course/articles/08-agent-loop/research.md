# Article 08 Research｜Agent Loop

- Research Phase：`COMPLETE`
- Research Status：`COMPLETE / CURRENT PRIMARY SOURCES SCOPED`
- Lifecycle Candidate：`EVIDENCE_READY`（状态迁移由 Master 决定）
- Evidence Gate Recommendation：`PASS`
- Preliminary Evidence Decision：`PROCEED_TO_LAB_EXECUTION`
- Required Lab：`Lab 03 Minimal Agent Loop`
- Lab Dependency：`REQUIRED / EXECUTED / OBSERVED / MERGED`
- Provider / external runtime calls：`NONE`
- Local Lab runtime execution：`COMPLETE BY LAB ENGINEER / NOT RERUN BY RESEARCHER`
- Retrieval Date：`2026-08-20`（Asia/Shanghai）
- Claim Summary：`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Evidence Merge：`COMPLETE`

> Research、Preliminary Evidence、Lab Design 与 Researcher Evidence Merge 均已完成。Lab Engineer 的 raw Observation 与本文件的解释分层保留；Lab 结果只证明 frozen deterministic fixture / fixed Host conformance，不证明真实模型、Provider 或行业通用行为。

## 1. Research decision

Lab 03 已执行并完成 Evidence Merge。Researcher 推荐 Evidence Gate `PASS`；是否关闭 Gate 并进入 Outline 由 Master 决定。

current official contracts 已足够回答两个事实层问题：一是某些 SDK/框架如何循环调用模型、执行工具、回送结果与结束；二是 `turn`、`step` 的含义并不跨产品统一。它们不足以证明本课程设计的 Host reducer、Observation normalization、四种终止轨迹和伪完成拒绝真的按设计运行。因此需要 Lab 03 生成行为证据。

本篇采用以下写法：

1. 把 OpenAI Agents SDK 的 `turn`、LangGraph 的 `super-step` 保留为产品术语，不互相换算。
2. 把本文 `Run / Step / Observation` 明示为课程工作定义，不包装成行业标准。
3. 把 Tool Result、model-visible item、Observation 与 authoritative state update 分层。
4. 把“停止运行”和“成功完成目标”拆开；限额、错误、取消均可终止，但不因此成功。
5. 用 deterministic substitute 代替 Provider，使四条最小轨迹可复现；不声称它验证真实模型质量。

## 2. Current primary source set

| ID | Source | Product / version scope | Retrieved | Used for | Does not prove |
|---|---|---|---|---|---|
| S-01 | [OpenAI Agents SDK Python — Running agents](https://openai.github.io/openai-agents-python/running_agents/) | current hosted docs；package release identity cross-check `openai-agents 0.22.0` | 2026-08-20 | Runner loop；final output、handoff、tool call 后续循环；`max_turns`；一次 `run` 可含多次 LLM call | 不证明课程 `Step`；不证明工具成功等于目标成功；hosted docs 不是 immutable snapshot |
| S-02 | [OpenAI Agents SDK Python — Run reference](https://openai.github.io/openai-agents-python/ref/run/) | current hosted docs；Python SDK | 2026-08-20 | `max_turns` 计数的是 AI invocation；final output 结束循环 | 不证明不同 SDK 的 `turn` 同义；不证明外部副作用可回滚 |
| S-03 | [OpenAI Agents SDK Python — Agents / tool use behavior](https://openai.github.io/openai-agents-python/agents/) | current hosted docs；Python SDK | 2026-08-20 | default 回送工具结果；`stop_on_first_tool`、`StopAtTools`、custom handler 等 runtime-configurable stop behavior | 不证明某一种 stop policy 是所有 Agent 的标准；不证明输出满足业务目标 |
| S-04 | [PyPI — openai-agents](https://pypi.org/project/openai-agents/) | `0.22.0`，uploaded 2026-08-19；verified source commit `4df9ecfae1761ca6fea67cc5a20b383c1d492024` | 2026-08-20 | 为 S-01～S-03 记录当前 release identity 和日期边界 | PyPI 元数据本身不证明 loop behavior；hosted docs 仍可能领先或落后该 tag |
| S-05 | [LangGraph — Graph API overview](https://docs.langchain.com/oss/python/langgraph/graph-api) | current hosted docs；未固定本地 package | 2026-08-20 | shared state、node update、reducer、edge；`super-step`；recursion limit 计 graph super-steps | 不证明 OpenAI `turn` 可映射成 graph step；不证明课程 reducer 已实现 |
| S-06 | [LangChain — Tools](https://docs.langchain.com/oss/python/langchain/tools) | current hosted docs；未固定本地 package | 2026-08-20 | string tool result 进入 `ToolMessage`；`Command` 可更新 graph state；`return_direct` 可短路 loop | 不证明任意 tool result 自动成为课程 Observation 或 authoritative state |
| S-07 | [LangGraph — Workflows and agents](https://docs.langchain.com/oss/python/langgraph/workflows-agents) | current hosted docs；未固定本地 package | 2026-08-20 | agent feedback loop 示例；LLM decision、tool node、ToolMessage correlation、conditional stop | 示例不是跨框架规范；不能替代 Lab 03 的固定轨迹 |

### 2.1 Local published dependencies

| ID | Dependency | Reused boundary | Not inherited |
|---|---|---|---|
| R-01 | Published Article 03 + evidence | Structured Output 只保证 shape / validation boundary | 不继承“结构正确即事实正确/任务成功” |
| R-02 | Published Article 05 + evidence | tool call 是调用意图；correlated result 必须回到后续请求；Tool Result 不自动等于 Evidence | 不把一次 Tool Use 当 Agent Loop |
| R-03 | Published Article 06 + evidence | Tool Runtime 拥有执行 gate、normalized result、trace；cancellation 是 cooperative boundary | 不继承真实工具或外部副作用已经安全闭环 |
| R-04 | Published Article 07 + evidence | MCP 只证明协议边界、capability discovery 与调用/结果 envelope | **不继承“Article 07 已经安全调用外部能力”**，也不继承 Agent Loop、permission、runtime 或 Evidence closure |
| R-05 | canonical glossary / series plan | 使用已冻结的 Model、Tool、Tool Runtime、Evidence 边界 | 不擅自全局定义 `Run / Turn / Step / Observation`；本文工作定义不回写 glossary |

## 3. Research answers

### 3.1 Run / Turn / Step 不能脱离产品 scope

在 OpenAI Agents SDK Python 的 current contract 中，Runner 的循环以模型调用推进：模型可能给出 final output、handoff 或 tool call；tool call 被执行并把结果追加后继续。该 SDK 的 `max_turns` 把一次 AI invocation（包含其 tool calls）算作一个 turn。与此同时，同一份 running guide 又把整个 `Runner.run(...)` 描述为一个 logical conversation turn，而一次 run 可以包含一个或多个 agent、一个或多个 LLM call。

这不是可以抹平的措辞差异。它说明即使在同一产品文档内，`turn` 也可能分别用于“loop counter 的模型调用单位”和“外部会话的一次逻辑交互”。文章必须带限定语，例如 `OpenAI max_turn`、`logical chat turn`，不能直接写成“一个 Turn 就是……”。

LangGraph 的 `super-step` 是 graph iteration：同一 super-step 可运行多个并行 node，顺序 node 则位于不同 super-step。它既不是 OpenAI 的 model invocation，也不是本 Lab 的一个工具调用。

#### 课程工作定义（PROPOSAL）

- **Run**：Lab `RunAsync` 接受一个冻结目标和初始 state 后，直到产生一个 terminal record 的一次 goal-bounded invocation。
- **Turn**：只作为外部交互分组 ID。本 Lab 一个 `turn_id` 绑定一个输入目标和一个 Run；它不承担 loop counter 语义。
- **Step**：本文用于讲解的原子 committed loop iteration。一个 Step 接受 step-before state，取得一个 scripted Decision，并提交该 Decision 的结果：`ACT` 包含 action gate、最多一次工具 outcome、Observation normalization 与一次 Host reducer；`REQUEST_STOP` 包含 completion validation。每个 Step 必须有 before / after state version。

这些定义仅对 Article 08 与 Lab 03 生效。它们不是 OpenAI turn，也不是 LangGraph super-step；后续文章若使用其他 runtime 必须重新声明 scope。

### 3.2 Tool Result、Observation 与 State Update 是三件事

current sources 支持以下产品内事实：

- OpenAI Agents SDK default loop 会执行 tool call，把 tool result 追加到模型输入并再次调用模型。
- LangChain tool 返回 string 时，该值会成为 `ToolMessage`，供模型下一次处理；若工具要更新 graph state，需返回 `Command`，更新再由 state reducer 应用。
- LangChain 的 `return_direct` 还能直接短路 loop，而不再次询问模型。

因此“拿到 Tool Result”不能在不说明 runtime contract 的情况下写成“Agent 已经观察并更新状态”。本文分四层：

1. **Tool Outcome**：Tool Runtime 的执行结果，含 correlation、status、code、data / error。
2. **Model-visible item**：某个 SDK 按自身协议追加给模型的 result/message。
3. **Observation（课程抽象）**：Host 对 Tool Outcome 做 correlation check、normalization 与安全裁剪后，允许进入下一次 Decide 输入的记录。
4. **Authoritative State Update（课程设计）**：Host reducer 根据旧 state、Decision 与 Observation 产生新 state；模型和工具都不直接覆盖 authoritative snapshot。

通用公式只能写成本文设计：

`ToolOutcome -> correlate/normalize -> Observation -> Host reducer -> State(n+1) -> next Decide input`

它不是 current SDK 的统一 API 形状。Lab 必须验证实现是否真的只把 normalized Observation 送入下一步，并为每次 reducer 提交 state version。

### 3.3 Decide 的来源不等于决策权的全部归属

在典型 agent 示例里，LLM 产生 action / final 候选；但 runtime 仍可：

- 检查工具名、参数、重复 action 与 policy；
- 选择是否执行；
- 把 raw outcome 正规化；
- 拒绝 completion；
- 按 max step、错误、取消或 policy 终止；
- 通过 tool-use behavior 直接使用工具输出或继续询问模型。

所以本文把 model/provider 的输出称为 **Decision candidate**。Lab 03 为了可复现，冻结 `ScriptedDecisionSource v1` 作为 deterministic substitute。它验证 loop control 与 state transition，不验证 LLM 是否会自主做出好决定。

authoritative reducer 由 Host 拥有是本文的安全设计选择（PROPOSAL），不是从 cited SDK 推导出的行业强制要求。

### 3.4 Stop 是组合判定，不是一个 token

current OpenAI contract 已显示多个停止来源：无 tool call 的 final output、`max_turns` 超限、handoff、tool-use behavior 的 `stop_on_first_tool` / `StopAtTools` / custom handler。LangChain `return_direct` 也能让 runtime 在工具结果处短路。

本文据此区分：

- **model completion signal**：Decision candidate 是 `REQUEST_STOP`；
- **requested outcome**：Decision source 希望得到的 outcome，仅是输入；
- **output contract**：结构与必填字段有效；
- **goal / evidence invariant**：state 是否真的满足本 case 的完成条件；
- **runtime guard**：max step、重复 action、policy、tool error；
- **interruption**：cancellation request；
- **terminal record**：Host 最终派生的 termination reason 与 run outcome。

`lifecycle == STOPPED` 不推出 `outcome == SUCCEEDED`。至少需要保留：

| Termination reason | Lifecycle | Outcome | Meaning |
|---|---|---|---|
| `GOAL_SATISFIED` | `STOPPED` | `SUCCEEDED` | REQUEST_STOP 通过 output、goal 与 evidence contract |
| `STOP_CONTRACT_FAILED` | `STOPPED` | `FAILED` | 决策源声称成功，但事实状态或 evidence 不足 |
| `UNRESOLVED_TOOL_FAILURE` | `STOPPED` | `FAILED` | normalized tool failure 尚未解决，不能被 stop signal 涂绿 |
| `MAX_STEPS_EXHAUSTED` | `STOPPED` | `INCOMPLETE` | 外部 loop counter 到限；不是成功 |
| `CANCELLED` | `STOPPED` | `INCOMPLETE` | cooperative cancellation 被 Host 观察；不声称撤销已发生 external side effects |
| `HOST_FAILURE` | `STOPPED` | `FAILED` | loop/runtime 自身失败 |

本 Lab 只实测前四种 termination；`CANCELLED` 保留 schema / precedence 设计但不列入本篇四条必需轨迹。取消后的 checkpoint、resume、recovery 属于 Article 11。

### 3.5 Max turn、Max step 与预算不可偷换

- OpenAI `max_turns` 计 AI invocation。
- LangGraph recursion limit 计 graph super-step。
- Lab 03 `max_steps` 计本文定义的 committed Step。

三者只共享“外部有界终止”这一抽象，不共享计数单位。Lab 不模拟 token、成本、延迟或跨资源组合预算；预算工程留给 Article 20。本文仅证明明确的 `max_steps` 可终止 Run，而且终止原因不能被记录为 success。

## 4. Counter-evidence and corrections

| Tempting statement | Counter-evidence | Article 08 correction |
|---|---|---|
| “一个 Turn 就是一轮用户对话” | OpenAI `max_turns` 计 AI invocation；同页又把完整 run 称 logical chat turn | 每次写 `turn` 都加产品/计数 scope；正文主要使用本文 Step |
| “一个 Step 就是一轮模型调用” | LangGraph `super-step` 是 graph tick，可含并行 nodes；课程 Step 还提交 observation/state | 把 Step 标成教学抽象，不映射产品术语 |
| “Tool Result 会自动更新 Agent state” | LangChain 区分 ToolMessage 与 Command/state reducer | 分离 Tool Outcome、model-visible item、Observation、state update |
| “模型输出 final 就成功” | runtime 有 output contract、state invariant、max limit 与 custom stop behavior | REQUEST_STOP 是 candidate；Host validation 决定 outcome |
| “达到 max turn 是任务完成” | OpenAI 超限是 bounded termination，不等于 final success | termination 与 outcome 分字段记录 |
| “取消会撤销已执行工具” | cooperative cancellation 只能停止后续工作；Article 06 已冻结 side-effect 边界 | 本篇不承诺 rollback；Article 11 再谈恢复 |
| “Article 07 已证明 Agent 能安全调用外部能力” | Article 07 只关闭 MCP protocol boundary，未建立 permission/runtime/evidence closure | 只复用协议 envelope，不继承安全执行事实 |

## 5. Preliminary evidence

### 5.1 Claim disposition before Lab

| Claim | Status | Preliminary basis | Remaining gap |
|---|---|---|---|
| 08-C01：OpenAI Agents SDK current loop 按 final / handoff / tool-result continuation 推进，`max_turns` 对 model invocation 计数 | `CONFIRMED`（product-scoped） | S-01, S-02, S-04 | hosted docs 非 immutable；不外推到其他 SDK |
| 08-C02：`Run / Turn / Step` 没有可跨 cited products 直接复用的统一计数语义 | `CONFIRMED` | S-01, S-02, S-05 | 只覆盖 cited products，不声称穷举全行业 |
| 08-C03：本文使用 goal-bounded Run、external grouping Turn、committed loop Step | `PROPOSAL` | 基于 C02 的课程术语设计 | Lab 可验证 trace 一致性，但不能把它变成行业标准 |
| 08-C04：在 cited products 中，tool result 进入后续模型输入与 authoritative state update 可以是不同操作 | `CONFIRMED`（product-scoped） | S-01, S-03, S-06, S-07 | 课程 normalization / reducer 尚未运行 |
| 08-C05：Lab authoritative state 只由 deterministic Host reducer 提交，Decision 只是 candidate | `PROPOSAL` | 安全与可复现性设计 | 需 Lab trace/state snapshots 证明实现遵守 |
| 08-C06：停止来源可以属于 runtime/config，不只属于模型；limit stop 不等于 success | `CONFIRMED`（product-scoped） | S-01, S-02, S-03, S-05, S-06 | 本 Lab 的 terminal schema 仍未运行 |
| 08-C07：completion contract 能区分真实成功与证据不足/未解决失败的伪完成 | `PARTIAL / LAB REQUIRED` | Article 03/05/06 边界 + Lab design | 需 AL-01、AL-02、AL-04 raw trace/state/output |
| 08-C08：四条固定轨迹能可重复地区分 success、tool failure、max-step stop、duplicate + pseudo-final | `PARTIAL / LAB REQUIRED` | Lab design | 需 build、两次 fresh-process run、fault injection 与 byte comparison |

### 5.2 Behavior evidence gap

以下缺口都不能靠文档检索关闭：

1. deterministic Decision 是否按冻结顺序进入每个 Step；
2. 相同 action fingerprint 在不同 invocation ID 下是否被识别为 repeat / no progress；
3. failed Tool Outcome 是否正规化为 failure Observation，且不能被下一次成功请求涂绿；
4. 每步是否恰好提交一次 state version，并区分 full-state digest 与 goal-state digest；
5. max-step gate 是否在请求第三个 Decision 前终止；
6. REQUEST_STOP 是否必须同时通过 output、goal、evidence 与 unresolved-failure contract；
7. 两次 fresh-process 执行的 normalized trace / state artifacts 是否 byte-identical。

因此 Required Lab 条件已满足：已有 Preliminary Evidence，且存在明确、可证伪的行为 gap。允许创建唯一 Lab 目录并冻结 Design；不得把 Design 误作 Observation。

### 5.3 Preliminary Evidence decision

`PROCEED_TO_LAB_EXECUTION`

理由：source contract 足以建立产品边界与教学抽象；Lab 03 的四条 case、deterministic substitute、state/trace schema、falsifier 和 acceptance criteria 均可安全冻结。Evidence Gate 仍为 `NOT_READY`，Outline / Draft 仍被阻塞。

## 6. Lab design handoff

- Canonical Lab Card：`docs/agent-engineering-course/labs/lab-03-minimal-agent-loop/README.md`
- Owner after handoff：Lab Engineer
- Required execution：build + four cases + two fresh-process suites + raw artifact preservation
- Provider：none；`ScriptedDecisionSource v1`
- Network / credential：none
- Evidence Merge owner：Researcher
- Design status：`FROZEN / EXECUTED / OBSERVED / MERGED`

Lab Engineer 只能实现冻结 case matrix。任何会改变 trajectory、stop precedence、schema、fixture 或 acceptance criteria 的变更都必须先退回 Researcher 解冻 Design。

## 7. Stop lines

Article 08 到此停止：

- 不进入 Article 09 的 Planning、plan quality、replanning 或 search strategy。
- 不进入 Article 10 的 workflow orchestration、durable state machine、parallel branch 或 compensation。
- 不进入 Article 11 的 long-running checkpoint、resume、recovery、human approval wait。
- 不进入 Article 12+ 的 context compaction、memory、knowledge retrieval。
- 不进入 Article 20 的 token/cost/latency budget engineering。
- 不把 Article 07 的 MCP protocol closure 改写为安全外部执行 closure。
- 不执行 Lab、不写 Outline / Draft、不宣布 Evidence Gate PASS。

## 8. Researcher handoff status

- Research：`COMPLETE`
- Preliminary Evidence：`COMPLETE`
- Lab Design：`FROZEN`
- Lab Execution：`COMPLETE BY LAB ENGINEER`
- Lab Observation：`COMPLETE`
- Evidence Merge：`COMPLETE`
- Evidence Gate Recommendation：`PASS`
- Evidence Gate Closure：`MASTER DECISION PENDING`

## 9. Researcher Evidence Merge｜2026-08-20

### 9.1 Experiment

Researcher 重新读取并交叉核对：

- frozen Lab Design 的前 `30312` bytes；
- appended Lab Engineer Observations；
- `observations/execution-log.md` 的环境、命令、退出码与失败 ledger；
- run-a / run-b 各六个 raw normalized artifacts；
- `cases.json`、fixture hashes；
- `LabRunner.cs` 的 input validation、pre-decision guard、Tool Outcome normalization、Host reducer、completion contract 与 digest 实现；
- BCL-only independent spec runner 的 schema、cross-reference、digest、case、NOT_RUN 与 byte-equality assertions。

Researcher 没有重新执行 Lab 命令，也没有修改 source/tests/raw artifacts。

### 9.2 Observation

- frozen prefix：`30312 bytes / SHA-256 242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`，与执行日志一致。
- observed environment 精确匹配 Windows `10.0.19045`、.NET SDK `10.0.301`、Host `10.0.9`、`net10.0`。
- locked restore、Release build、BCL spec、formal run-a、formal run-b、independent verifier 最终均 exit `0`；build 为 `0 warnings / 0 errors`。
- run-a / run-b 六个对应文件逐 byte 相等；每 run 是 `4 cases / 10 STEP / 4 TERMINAL / 10 state snapshots / 7 Tool Outcomes / 7 Observations / 7 tool calls / 10 decision calls / 1 SUCCEEDED`。
- AL-01：`GOAL_SATISFIED / SUCCEEDED`，Goal + Output + Evidence + unresolved-failure contract 全 PASS。
- AL-02：failed Tool Outcome `MOCK_PARSE_FAILED` 被 `PASS / TOOL_FAILURE` Observation 通过同一 record digest 引用；REQUEST_STOP 仍为 `UNRESOLVED_TOOL_FAILURE / FAILED`。
- AL-03：`steps=2 / decisions=2 / tools=2`；`al03-decision-03` 留在 `remaining_decision_ids`，terminal 是 `MAX_STEPS_EXHAUSTED / INCOMPLETE`。
- AL-04：不同 invocation ID 的 action fingerprint 相同；semantic payload digest 相同、correlated record digest 不同；full-state digest 均变化、goal-state digest 均不变、两步均 `NO_PROGRESS`；`EV-FAKE` 被拒绝，terminal 是 `STOP_CONTRACT_FAILED / FAILED`。
- `cases.json` 不含 expected termination/outcome/success/count/digest/assertion 字段；runtime 有 anti-self-fulfilling validation。

### 9.3 Evidence interpretation

这些 Observation 足以证明：在 frozen Windows/.NET 环境、固定 fixture、`ScriptedDecisionSource v1` 与当前 fixed Host 实现中，Result -> Observation -> Host state transition、completion validation、pre-decision max-step guard、repeat/no-progress 判断与四种 terminal outcome 按 Design 工作，并可在两个 fresh processes 中复现。

它们不证明：

- 真实模型会选择正确 action、会 recovery 或会自行停止；
- OpenAI / LangGraph 或其他 SDK 必须采用本文 Host reducer 与 schema；
- Provider、MCP、权限、网络、生产可靠性或 external side-effect rollback；
- cancellation trajectory；
- Article 09 Planning、10 Workflow/State Machine、11 long-running recovery、12+ Context/Memory 或 20 budget engineering。

### 9.4 Claim status after merge

| Claim | Final status | Merge decision |
|---|---|---|
| 08-C01 | `CONFIRMED / PRODUCT-SCOPED` | official OpenAI Python SDK current contract；Lab 不改变其 scope |
| 08-C02 | `CONFIRMED / CITED-PRODUCTS-SCOPED` | official terminology counter-evidence；Lab 只符合课程映射 |
| 08-C03 | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` | 工作定义仍是教学抽象，不升级行业事实 |
| 08-C04 | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` | upstream contract + 7 Result/Observation cross-references |
| 08-C05 | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` | Host-only state ownership是课程设计；当前实现符合 |
| 08-C06 | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` | upstream stop/limit contract + AL-03 bounded incomplete |
| 08-C07 | `CONFIRMED / FIXED-HOST-FIXTURE-SCOPED` | AL-01、AL-02、AL-04 证明 fixed completion contract 区分 success / unresolved failure / pseudo-completion |
| 08-C08 | `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED` | four cases、two fresh processes、raw artifacts 与 verifier 全满足 |

### 9.5 Preserved failures and interruptions

execution log 保留了：CIM access denied、一次 compile-name collision、fixture EOF extra blank line、不可用 NuGet testhost 路径、一次 live-reference snapshot digest mismatch。它们都发生在 final green chain 之前，修正后由最终 build/spec/raw artifact verification 覆盖；没有 case failure 被涂绿。

两次 Master interruption 发生在正式命令结束后的 Markdown 日志交付阶段，当时无 Lab command 在运行。它们属于 orchestration/log-delivery interruption，不是 runtime case failure；第一次之后的 restore/build/test/verifier 复核均 exit `0`，第二次后只检查已有日志与 artifacts，未启动新命令。

### 9.6 Final recommendation

- Lab Status Candidate：`VERIFIED / EVIDENCE_MERGED`
- Claim Summary：`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Evidence Gate Recommendation：`PASS`
- Blocker：`NONE`
- Exact next action：Master 独立复核本 Merge，关闭 Evidence Gate 后分派真实 Outliner；Researcher 不创建 Outline / Draft。
