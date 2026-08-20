# Article 09 Evidence

## Status

- Gate：`EVIDENCE_GATE_CANDIDATE`
- Owner：`RESEARCHER`
- Article：`09`
- Retrieved Date：`2026-08-20`
- Evidence Status：`PASS_RECOMMENDED`
- Claim Summary：`5 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 3 PROPOSAL`
- Required Lab：`NONE`

本文件只为 Article 09 的候选 Claim 建立逐 Claim 证据合同。`PROPOSAL` 表示课程工作定义或设计建议，不伪装成来源事实；`PARTIAL` 表示证据只允许模式级比较。所有产品事实限定在对应官方资料与版本范围内，所有 Lab 事实限定在冻结 fixture 的 raw artifacts 内。

## Claim Register

| Claim ID | Exact wording | Status | Scope | Evidence Cards | Counter-evidence / limitation | Course usage |
|---|---|---|---|---|---|---|
| `09-C01` | 本课程把 Plan 定义为 Goal 与 Current Evidence 条件下的剩余行动候选表示；可隐式、可见或结构化 | `PROPOSAL` | course taxonomy | `09-E01` | 来源没有统一 Plan schema；显式 Planner 不是必要条件 | 全文最小模型 |
| `09-C02` | cited products 显示 Planning 可以没有独立显式 Plan object，也可以保存结构化 Plan；实现形态不是单一标准 | `CONFIRMED` | cited-products-scoped | `09-E02` | Semantic Kernel 与 LangGraph.js 是不同产品、不同 abstraction | 三种 Plan 形态边界 |
| `09-C03` | ReAct、Plan-and-Solve / Plan-and-Execute、Planner / Executor 强调不同控制节奏，不能做 API 一一映射 | `PARTIAL` | pattern-scoped | `09-E03` | 术语会漂移；ReAct executor 也可嵌入 Plan-and-Execute | 轻量模式比较 |
| `09-C04` | 新 Observation 或已执行结果可以成为更新剩余 Plan 的输入；AL-02 failure observation 反驳 v1 下一步前提 | `CONFIRMED` | source + fixture-scoped | `09-E04`、`09-E05` | AL-02 没有 Planner；Replace 只是分析 overlay | revision 触发与 bounded trace |
| `09-C05` | Keep / Revise / Replace / Stop 是本课程的 plan disposition taxonomy | `PROPOSAL` | course taxonomy | `09-E06` | 不是来源中的标准 enum，也不保证新计划正确 | revision decision table |
| `09-C06` | 在 cited implementation 与 fixture 中，Plan、executed result / past step、Observation、State 与 Workflow routing 是可分对象 | `CONFIRMED` | cited implementation + fixture | `09-E07` | 不宣称所有 SDK 使用相同字段或 producer | 边界模型 |
| `09-C07` | 2026-08-20 retrieved current official OpenAI Agents SDK docs显示，model-emitted tool call 仍可被 approval 或 tool guardrail 暂停、拒绝或阻断；tool guardrail限于custom `function_tool` pipeline，HITL受tool-type支持范围约束 | `CONFIRMED` | OpenAI current-official-docs-retrieved-2026-08-20 | `09-E08` | 不覆盖hosted / built-in tools与handoff；`0.22.0`仅为当日PyPI / tag version anchor，docs-current与tag未逐项source mapping | Plan / call 不等于 Authorization |
| `09-C08` | 可审计 Plan 应持久化候选步骤、version、change reason 与 evidence reference，而非要求保存完整 Chain-of-Thought | `PROPOSAL` | course artifact design | `09-E09` | 来源呈现不同载体，未规定统一持久化字段 | Plan artifact design |
| `09-C09` | Plan 或 Plan item 本身不能证明 action 已执行、事实已验证或目标已成功 | `CONFIRMED` | object contract + fixture | `09-E10` | 必须结合 runtime outcome、Observation、State 与 completion evidence | learning check 与坏实现 |

## Evidence Cards

### 09-E01｜Plan 的课程工作定义

- **Supports Claim**：`09-C01`
- **Status**：`PROPOSAL`
- **Sources**：
  - `09-S01` — [ReAct: Synergizing Reasoning and Acting in Language Models](https://arxiv.org/abs/2210.03629)，arXiv `2210.03629v3`，2023-03-10，retrieved 2026-08-20。
  - `09-S03` — [LangGraph.js Plan-and-Execute official notebook](https://raw.githubusercontent.com/langchain-ai/langgraphjs/main/examples/plan-and-execute/plan-and-execute.ipynb)，official repository `main` snapshot，retrieved 2026-08-20。
  - `09-S04` — [Semantic Kernel Planning](https://learn.microsoft.com/en-us/semantic-kernel/concepts/planning)，page last updated 2025-06-11，retrieved 2026-08-20。
- **Observation**：一手来源分别出现交错式 next decision、显式 `plan` / `pastSteps`、function-call / result feedback loop；没有跨产品统一 Plan object。
- **Proves**：课程有必要用与具体 SDK 解耦的最小工作定义，且候选步骤可以有不同可观察载体。
- **Does Not Prove**：该定义是行业标准；每个 Agent 必须预生成完整多步列表；Plan 内容必然正确。
- **Counter-evidence**：Semantic Kernel current guidance 不要求旧式显式 planner；因此“Plan 必须是独立对象”不能进入定义。
- **Limitations**：这是从多个 source shape 抽象出的课程 Proposal，不是论文或产品原句。
- **Course Usage**：正文开头定义 `Plan candidate`，并始终保留“候选”限定。

### 09-E02｜显式 Plan 不是唯一实现形态

- **Supports Claim**：`09-C02`
- **Status**：`CONFIRMED / CITED-PRODUCTS-SCOPED`
- **Sources**：
  - `09-S03` — LangGraph.js official notebook，unpinned `main` snapshot，retrieved 2026-08-20。
  - `09-S04` — Semantic Kernel Planning，last updated 2025-06-11，retrieved 2026-08-20。
- **Observation**：LangGraph.js notebook state显式声明 `plan: string[]` 与 `pastSteps`，而 Semantic Kernel current page把 function calling 的请求—结果—下一决策 feedback loop 作为 planning / execution 主路径，并记录旧 planners 已移除。
- **Proves**：在这两个 cited products 中，Planning 既可有显式列表，也可由逐步反馈回路形成；独立 Planner class 不是共同必要条件。
- **Does Not Prove**：两种产品在语义、可靠性或生产能力上等价；所有 implicit planning 都不可审计；structured plan 一定更好。
- **Counter-evidence**：LangGraph example 的确使用 planner、executor 与 replanner，说明显式结构在某些控制形态中有价值。
- **Limitations**：LangGraph notebook未做本轮 compatibility run；Semantic Kernel page未绑定本轮具体 package build。
- **Course Usage**：支撑 `Implicit / Visible / Structured` 三分法，但把三分法标成课程 taxonomy。

### 09-E03｜Planning patterns 只能做模式级比较

- **Supports Claim**：`09-C03`
- **Status**：`PARTIAL / PATTERN-SCOPED`
- **Sources**：
  - `09-S01` — ReAct original paper，arXiv v3 / ICLR 2023，retrieved 2026-08-20。
  - `09-S02` — [Plan-and-Solve Prompting](https://arxiv.org/abs/2305.04091)，arXiv `2305.04091v3`，2023-05-26，ACL 2023，retrieved 2026-08-20。
  - `09-S03` — LangGraph.js official Plan-and-Execute notebook，retrieved 2026-08-20。
  - `09-S04` — Semantic Kernel Planning，retrieved 2026-08-20。
- **Observation**：ReAct交错 reasoning、action 与 observation；Plan-and-Solve先制定分解步骤再解子任务；LangGraph example先生成列表、执行首项，再基于 `pastSteps` replan；Planner / Executor 是 example 内的责任分离。
- **Proves**：这些来源强调的控制节奏与载体不同，正文可以做窄化比较。
- **Does Not Prove**：它们是互斥分类；Plan-and-Solve 等于工具型 Plan-and-Execute；Planner 必须是另一个 Agent、模型或进程。
- **Counter-evidence**：LangGraph example 的 executor 本身使用 ReAct agent，显示 patterns 可以嵌套而非互斥。
- **Limitations**：术语在论文、框架与版本之间漂移；因此 Claim 保持 `PARTIAL`。
- **Course Usage**：只用一张轻量表解释控制节奏，不做 API 对照或优劣排名。

### 09-E04｜外部结果可以触发剩余 Plan 更新

- **Supports Claim**：`09-C04`
- **Status**：`CONFIRMED / SOURCE-SCOPED`
- **Sources**：
  - `09-S01` — ReAct original paper，arXiv v3 / ICLR 2023，retrieved 2026-08-20。
  - `09-S03` — LangGraph.js official notebook，retrieved 2026-08-20。
- **Observation**：ReAct描述利用环境反馈追踪、更新 action plan与处理 exception；LangGraph example把已执行结果写入 `pastSteps`，随后由 `replanner`更新剩余 `plan` 或返回最终 response。
- **Proves**：已执行结果 / external observation 可以成为修改剩余候选行动的输入。
- **Does Not Prove**：任何 Observation 都必须触发 revision；replanning 必然改善质量；来源规定了 `Keep / Revise / Replace` enum。
- **Counter-evidence**：如果新 Observation 未否定后续前提，保留剩余计划也可能是合理选择。
- **Limitations**：ReAct是研究设置；LangGraph是官方 example而非生产验收。
- **Course Usage**：作为 revision trigger 的来源证据，与 `09-E05` fixture overlay 配对。

### 09-E05｜AL-02：Observation 反证 Initial Plan 前提

- **Supports Claim**：`09-C04`
- **Status**：`CONFIRMED / FIXTURE-SCOPED + PROPOSAL OVERLAY`
- **Sources**：
  - `09-S09` — [AL-02 trace](../../labs/lab-03-minimal-agent-loop/observations/run-a/trace.jsonl)、[tool outcomes](../../labs/lab-03-minimal-agent-loop/observations/run-a/tool-outcomes.jsonl)、[observations](../../labs/lab-03-minimal-agent-loop/observations/run-a/observations.jsonl)、[states](../../labs/lab-03-minimal-agent-loop/observations/run-a/states.jsonl)，Lab 03 fixed fixture `run-a`，retrieved 2026-08-20。

| Sequence | Layer | Classification | Inspectable record |
|---|---|---|---|
| 1 | Initial Plan v1 | `PROPOSAL` | `parse build.log -> read diagnostic-matched source -> verify Goal Evidence` |
| 2 | Execution | `OBSERVED` | AL-02 step 1 invokes `parse_mock_log` |
| 3 | Tool Outcome | `OBSERVED` | `FAILED / MOCK_PARSE_FAILED / FI_PARSE_TYPED_FAILURE`；digest `B39ED180065C66D1115C1ACC0A50F98204FC6B066A44DC0046BF0091D51C13A4` |
| 4 | Observation | `OBSERVED` | `AL-02/step-01` kind `TOOL_FAILURE`；normalization `PASS`；no accepted Evidence ID |
| 5 | Verified State | `OBSERVED` | revision 1 keeps `REQ_LOG` and `REQ_SOURCE` unresolved；`unresolved_tool_failure_count=1`；accepted Goal Evidence empty |
| 6 | Disposition | `PROPOSAL / REPLACE` | v1 的“已有 diagnostic path / symbol”前提未成立；候选 v2先解除 parse failure，否则 `STOP / ESCALATE` |

- **Proves**：raw artifacts中确实发生 typed parse failure，且 authoritative state没有获得下一步所需 source locator或 Goal Evidence；因此 v1 的直接下一步前提被观测事实否定。
- **Does Not Prove**：Lab 03 runtime拥有 Planner；模型生成过 v1 / v2；runtime实际执行 Replace；retry必然失败；候选 v2能够成功。
- **Counter-evidence**：后续 raw request可能出现 successful tool outcome，但固定 run的 terminal仍为 `UNRESOLVED_TOOL_FAILURE / FAILED`；单个成功 request不能倒推 v1 当时前提成立或 Goal已完成。
- **Limitations**：Plan v1、v2及 disposition是 Article 09分析 overlay，不是 raw trace字段。
- **Course Usage**：作为唯一 bounded fixture；图表必须用 `OBSERVED` / `PROPOSAL` 双轨标注。

### 09-E06｜Keep / Revise / Replace / Stop taxonomy

- **Supports Claim**：`09-C05`
- **Status**：`PROPOSAL`
- **Sources**：`09-E04` 的 source observation与 `09-E05` 的 bounded fixture只提供抽象输入，不提供标准 enum。

| Disposition | Course condition | Required record |
|---|---|---|
| `KEEP` | 新 Observation未否定剩余步骤的前提 | plan version、accepted observation reference |
| `REVISE` | Goal与主路径仍成立，但步骤、顺序、参数或前提需局部修改 | from/to version、change reason、evidence reference |
| `REPLACE` | 关键前提、Goal或允许路径失效，剩余路径需废弃 | invalidated assumption、replacement candidate、authority check |
| `STOP / ESCALATE` | 无安全候选路径，或需要授权 / 人类输入 | blocker、stop reason、required authority |

- **Proves**：该 taxonomy可让 plan change可审查，并避免把所有失败机械归为 retry。
- **Does Not Prove**：这是行业标准；每次 Observation 都只有唯一 disposition；Re-plan必然成功。
- **Counter-evidence**：同一 Observation在不同 policy、budget与authorization下可能导向不同 disposition。
- **Limitations**：课程设计 Proposal；需要在正文中明确。
- **Course Usage**：用于 decision table，并把 Retry留给 Article 11。

### 09-E07｜Plan、Execution、Observation、State 与 Workflow 可分

- **Supports Claim**：`09-C06`
- **Status**：`CONFIRMED / CITED-IMPLEMENTATION + FIXTURE-SCOPED`
- **Sources**：
  - `09-S03` — LangGraph.js official notebook，retrieved 2026-08-20。
  - `09-S08` — [Article 08 published content](../../../../content/ai-empowerment/agent-engineering-08-agent-loop.md)，completion commit `d4693bd6d78ed63a669e181516e28247460fee11`，retrieved 2026-08-20。
  - `09-S09` — Lab 03 frozen raw artifacts，retrieved 2026-08-20。
- **Observation**：LangGraph state分别保存 `plan`、`pastSteps`，graph edges规定 `planner -> agent -> replan`；Article 08与Lab 03分别保存 Tool Outcome、Observation、State与terminal reason。
- **Proves**：在 cited implementation / fixture中，这些对象拥有不同字段、producer或routing authority，不能用一个 Plan item互相替代。
- **Does Not Prove**：所有框架必须使用相同文件或字段；graph state天然等同课程 Verified State；workflow细节已在本篇完成。
- **Counter-evidence**：某些简单实现可能把多个对象存在同一 state container；同容器不等于同语义或同 authority。
- **Limitations**：Article 08与Lab 03是课程固定合同；LangGraph notebook是产品 example。
- **Course Usage**：支撑 `Plan != Execution != Observation != Verified State != Workflow` 的边界图，Workflow深挖留给 Article 10。

### 09-E08｜Plan / Tool call 不等于 Authorization

- **Supports Claim**：`09-C07`
- **Status**：`CONFIRMED / OPENAI-CURRENT-OFFICIAL-DOCS-RETRIEVED-2026-08-20`
- **Sources**：
  - `09-S05` — [OpenAI Agents SDK Guardrails](https://openai.github.io/openai-agents-python/guardrails/)，official current docs，retrieved 2026-08-20。
  - `09-S06` — [OpenAI Agents SDK Human-in-the-loop](https://openai.github.io/openai-agents-python/human_in_the_loop/)，official current docs，retrieved 2026-08-20。
  - `09-S07` — [PyPI openai-agents](https://pypi.org/project/openai-agents/)，version `0.22.0` uploaded 2026-08-19；source tag `v0.22.0` / commit `4df9ecfae1761ca6fea67cc5a20b383c1d492024`，retrieved 2026-08-20；仅作为当日 PyPI / tag version anchor。
- **Observation**：2026-08-20 retrieved current official docs显示，custom `function_tool` input guardrail可在执行前 skip、replace output或tripwire；HITL flow可暂停 pending tool approval，并由人批准或拒绝。Guardrail docs不将该pipeline扩展到hosted / built-in tools或handoff，HITL支持范围也与tool type有关。
- **Proves**：在2026-08-20 retrieved current official docs的明确产品与tool-type范围内，模型提出tool call不自动取得执行授权。
- **Does Not Prove**：所有 SDK都按同样顺序执行 gate；guardrail覆盖 hosted / built-in tools、handoff或 `Agent.as_tool()`；HITL对所有tool type均可用；审批即事实验证；当前docs行为已逐项绑定到`v0.22.0` source contract。
- **Counter-evidence**：非敏感或已允许的 call可能无需人类审批；这不改变 authorization来自policy / approval而非Plan本身。
- **Limitations**：`0.22.0`只是2026-08-20当日PyPI / tag version anchor；docs-current与tag源码未做逐项source mapping，不能把current docs行为命名为已核验的`0.22.0 contract`。
- **Course Usage**：正文使用“2026-08-20 retrieved current official OpenAI Agents SDK docs”，并显式保留`function_tool`、hosted / built-in tools、handoff、HITL tool-type与docs-current / tag未逐项source mapping边界。

### 09-E09｜可审计 Plan 不要求保存完整 Chain-of-Thought

- **Supports Claim**：`09-C08`
- **Status**：`PROPOSAL`
- **Sources**：
  - `09-S01` — ReAct original paper，retrieved 2026-08-20。
  - `09-S03` — LangGraph.js official notebook，retrieved 2026-08-20。
  - `09-S04` — Semantic Kernel Planning，retrieved 2026-08-20。
- **Observation**：来源展示 verbal reasoning trajectory、紧凑 `string[] plan` / `pastSteps`、逐步 function-call feedback等不同载体；它们不构成“必须持久化完整私有推理”的共同要求。
- **Proves**：课程可以把审计对象设计为候选步骤、version、change reason、evidence reference，而不依赖完整 Chain-of-Thought。
- **Does Not Prove**：任何产品的内部推理机制；不保存 CoT就自动安全；四个字段是跨行业标准。
- **Counter-evidence**：研究轨迹中的自然语言 reasoning有助于解释实验行为，但研究展示方式不等于生产持久化合同。
- **Limitations**：字段集合是课程 Proposal，后续实现仍需隐私、安全与存储策略。
- **Course Usage**：正文显式写 non-goal，示例仅展示 inspectable plan diff。

### 09-E10｜Plan item 不能证明完成

- **Supports Claim**：`09-C09`
- **Status**：`CONFIRMED / OBJECT-CONTRACT + FIXTURE-SCOPED`
- **Sources**：
  - `09-S03` — LangGraph.js official notebook，retrieved 2026-08-20。
  - `09-S08` — Article 08 published content，completion commit `d4693bd6d78ed63a669e181516e28247460fee11`。
  - `09-S09` — Lab 03 frozen raw artifacts，retrieved 2026-08-20。
- **Observation**：LangGraph example把 plan与past execution分开；Article 08把 Tool Outcome、Observation、State与terminal verdict分开；AL-02即使具有候选下一步，也因 unresolved failure和缺失 Goal Evidence终止为失败。
- **Proves**：候选步骤的存在或文字状态不能替代runtime execution record、normalized Observation、authoritative State与completion Evidence。
- **Does Not Prove**：任一特定框架的 completion schema；有 evidence reference就必然真实；Article 10 / 11的workflow与recovery规则。
- **Counter-evidence**：一个结构化 Plan可以包含 status字段，但status仍需由有authority的runtime / reducer更新，而不是由Planner自证。
- **Limitations**：结论依赖课程对象合同与固定 fixture，不宣称统一字段名。
- **Course Usage**：用于坏实现“Planner把item标done就宣称成功”的反例和章节自测。

## Bounded Fixture / Trace Contract

Article 09只复用 `09-E05`，不创建新 Lab，也不修改 Lab 03。后续 Outline / Draft 必须保持以下标注：

- `OBSERVED`：tool request、typed Tool Outcome、normalized Observation、State revision、terminal verdict；
- `PROPOSAL`：Initial Plan v1、Revised Plan v2、`REPLACE` recommendation；
- `NOT OBSERVED`：自动 Planner、模型 revision、v2 execution、成功恢复；
- 禁止把 expected / candidate写成 observed；
- 禁止从后续某个 successful request倒推早期前提成立或最终 Goal完成。

## Evidence Gate Checklist

- [x] 每个 `09-Cxx` Claim都有对应 Evidence Card。
- [x] 每张 Evidence Card记录 source title / URL、version或date、retrieved date。
- [x] 每张 Evidence Card包含 `Proves`、`Does Not Prove`、counter-evidence、limitations与course usage。
- [x] `CONFIRMED` Claim均有限定 scope；没有把产品文档泛化成行业合同。
- [x] `09-C03` 保持 `PARTIAL`，未把 patterns写成互斥或等价。
- [x] `09-C01 / C05 / C08` 保持 `PROPOSAL`，未升级为来源事实。
- [x] AL-02 overlay严格区分 observed fixture与proposed Plan。
- [x] Plan、Execution、Observation、Verified State、Authorization、Workflow已分离。
- [x] Chain-of-Thought persistence、Article 10 Workflow与Article 11 Retry / Recovery保持 non-scope。
- [x] 没有 `BLOCKED` Claim或必须新增 Lab才能支撑的核心 Claim。

## Evidence Gate Recommendation

`PASS_RECOMMENDED -> OUTLINE`。

推荐理由：核心行为 Claim均由原论文、官方文档 / 仓库或冻结 raw fixture支撑；三项课程设计判断已明确标为 `PROPOSAL`，模式比较已保持 `PARTIAL`。AL-02提供一条有界、可检查的“新 Observation反证 Initial Plan前提”trace，同时没有把分析 overlay伪装成runtime observation。

Master仍需独立验证 Allowed Writes与Gate transition。后续 Author不得把本文件中的 `PROPOSAL`改写成行业事实，不得把 AL-02写成已执行自动 re-planning，也不得把 Plan或tool call写成已授权、已执行、已验证或已成功。
