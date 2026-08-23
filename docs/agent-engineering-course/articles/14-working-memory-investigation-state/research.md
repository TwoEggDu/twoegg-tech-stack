# Research｜Article 14 Working Memory 与 Investigation State

## Status

- Gate: `RESEARCH`
- Research Status: `COMPLETE`
- Retrieved date: `2026-08-22`
- Required Lab: `NONE`
- Scope: Normal Article；研究当前 task / thread 范围内的可更新调查状态，不扩写 Session、Long-term / Project Memory 或 RAG。
- Evidence policy: 技术行为只采用官方文档、标准或原始论文。没有跨生态统一定义的部分明确标为 `COURSE PROPOSAL`。

## Executive Finding

当前官方生态并不存在一个可直接照搬的、跨产品统一的 `Working Memory` 对象：LangGraph 把 thread-scoped short-term memory 放进 graph state 并通过 checkpoint 保存；Google ADK 把 `session.state` 称为 session 的 scratchpad，同时把 `events` 视为历史；OpenAI Agents SDK 又明确区分本地应用 context、LLM conversation context、Session history 与可序列化 `RunState`。这些术语有交集，但不是同一个行业标准。

因此，本篇可以提出下述课程操作定义，但必须标 `COURSE PROPOSAL`：

> **Working Memory 是当前未完成任务的、可更新且带版本的工作投影；它保留继续判断下一步所必需的目标、已接受事实、仍在检验的解释、未决问题、证据引用和完成缺口。它可以只活在一次运行中，也可以为恢复而任务级持久化，但它既不是完整历史，也不拥有 Workflow 的权威迁移权。**

这里的“工作投影”说明 Working Memory 是从 History、Evidence 和 Workflow State 中选择、归纳出来的当前视图，而不是把三者复制一遍。

## Research Questions

### RQ1｜最小、可证据支持的 Working Memory 定义是什么？

可确认的共同部分只有：

1. 多个框架都允许在一次任务或会话范围内维护可更新 state。
2. state 与完整 conversation / event history 可以分开表示。
3. state 更新通常经过框架运行时、事件提交或 reducer，而不是靠“模型在文字里记住了”。
4. state 是否落盘、保存多久，与它的逻辑作用域不是一回事。

“当前未完成任务的可更新工作投影”是对这些机制的课程抽象，不是任何一家产品的原话。因此课程定义为 `PROPOSAL`；其构成证据为 `CONFIRMED / PRODUCT-SCOPED`。

Magentic-One 提供了一个强但有限的设计先例：Orchestrator 的 Task Ledger 在任务期间充当 short-term memory，记录 given / verified facts、需要查询或推导的 facts、educated guesses 和 task plan；Progress Ledger 记录是否完成、是否陷入循环、是否仍有进展和下一位 speaker。它说明“结构化调查台账”是已发表系统设计，但不能证明本篇的精确字段或 taxonomy 是行业标准。

### RQ2｜与相邻概念的边界是什么？

| 相邻概念 | 可确认的角色 | 与 Working Memory 的边界 | 实现上允许的重叠 | 不能推出 |
|---|---|---|---|---|
| Context Snapshot | 当前一步选择给模型看的有效信息 | Snapshot 是一次选择结果；Working Memory 是可更新的任务状态源之一 | Snapshot 可以包含 Working Memory 的投影 | “进入 prompt 的内容”自动成为权威状态 |
| History | 按时间记录 message / event / transition | History 回答“发生过什么”；Working Memory 回答“现在按什么继续” | 当前状态可以从 History 重放或归纳；state 也可含 messages channel | History 与当前投影必然分开存储，或二者永不重叠 |
| authoritative Workflow State | 已提交的阶段、守卫、权限、允许迁移 | Workflow State 决定“系统现在在哪、可否过门”；Working Memory 只能引用或提出更新 | 同一 checkpoint 可同时序列化二者 | 模型写入调查笔记即可推进 gate |
| Checkpoint | 为恢复 / 回放持久化的边界工件或状态版本 | Checkpoint 是 durability / recovery 机制；Working Memory 是可能被保存的内容 | checkpoint 可以完整保存某一版 working state | 每次 Working Memory 更新都必须建 checkpoint；checkpoint 等于 memory |
| Long-term Memory | 跨 session / thread 的用户、应用或项目知识 | Article 14 只讨论当前任务的调查状态 | task state 可持久化很久，但作用域仍可只是当前 task | “存进数据库”自动变成长时记忆 |
| Evidence | 支持或反驳 claim 的来源、观测或工件 | Working Memory 保存 claim 状态和 evidence refs；Evidence 本体独立保留 | 小证据摘要可进入工作状态 | 有来源引用就等于 claim 已被接受或为真 |

上述角色边界是课程综合，状态为 `PARTIAL / COURSE-SYNTHESIS`。产品资料能直接证明若干局部差异，但不能证明这张表是标准术语表。
### RQ3｜Investigation State 最小需要哪些字段？

精确 schema 没有跨生态标准；下表全部是 `COURSE PROPOSAL`。Magentic-One、W3C PROV 与各框架 state 机制只提供设计依据。

| 字段 | 最小语义 | 依据强度 | 课程边界 |
|---|---|---|---|
| `goal` | 当前要判定什么，以及完成判据 | 强先例：task、task plan、task complete | 不复制 Workflow gate；只保存调查目标 |
| `current_hypotheses[]` | 仍可被检验的候选解释 | 强先例：educated guesses，但命名不同 | 必须与 accepted facts 分开 |
| `accepted_facts[]` | 在给定 scope / revision 下已通过课程接受规则的事实 | 强先例：given / verified facts | “accepted”由 host policy 决定，不由模型自封 |
| `unresolved[]` | 尚缺资料、仍冲突或证据不足的问题 | 强先例：facts to look up / derive | `UNKNOWN` 不是 `false` |
| `evidence_refs[]` | 指向来源、工件、locator、版本与检索时间 | W3C PROV 支持 derivation / attribution / revision 记录 | 引用不是 Evidence 本体，也不自动证明真实性 |
| `rejected[]` | 已退出 active set 的 hypothesis、理由、反证、适用 scope | 无统一字段；由防循环需求导出 | `COURSE PROPOSAL`，必须保留 rejection reason |
| `pending_actions[]` | 下一步检查、owner / authority、前置条件 | task plan / next instruction 有先例 | 不是 Workflow command；执行仍受 host guard |
| `confidence` | 可选的定性置信或证据强度 | 只有 guarded guesses / uncertainty 的弱先例 | 非必填；不得伪装成校准概率 |
| `completion_gaps[]` | 距离 completion criteria 还缺什么 | task-complete / progress 判断有先例 | 不等于 Workflow 已允许过门 |
| `revision` | 乐观并发、冲突检测、回放定位用的版本 | checkpoint / state version 机制有产品先例 | 具体格式由 host 定义 |

最小 schema 建议不强制数值 `confidence`。更可靠的做法是保存 `status + evidence_refs + counter_evidence + next_test`；只有确有校准方案时才加入数值。

### RQ4｜如何避免 OBSERVED / INFERRED / HYPOTHESIS / REJECTED / UNKNOWN 互相升级？

五个标签的精确定义也是 `COURSE PROPOSAL`。建议在存储模型中把“认知种类”和“生命周期处置”分成两个轴，因为 `REJECTED` 实际上不是与 observation 同层的证据类型。

| 读者标签 | 存储建议 | 进入条件 | 明确不代表 |
|---|---|---|---|
| `OBSERVED` | `kind=OBSERVATION` | 有可定位的原始输出、事件、文件或命令结果，并保存 source / locator / version | 根因已经确定；观测在所有环境都成立 |
| `INFERRED` | `kind=INFERENCE` | 由已列出的 observations 和显式推理规则推出 | 直接看到；因果已经实验确认 |
| `HYPOTHESIS` | `kind=HYPOTHESIS, disposition=ACTIVE` | 是可检验的候选解释，附下一项检查或可证伪条件 | accepted fact；可以据此越过风险 gate |
| `REJECTED` | `kind=HYPOTHESIS, disposition=REJECTED` | 指定预测失败、反证或 scope 冲突，并保留理由与 refs | 永久、跨版本的“绝对错误” |
| `UNKNOWN` | `kind=UNKNOWN` | 任务要求回答，但当前资料缺失、冲突或不足 | `false`；某个未经写出的 hypothesis |

状态迁移建议：

```text
raw artifact
  -> OBSERVED (provenance validated)
  -> HYPOTHESIS or INFERRED (explicit rule + refs)
  -> accepted_facts (host acceptance policy)

HYPOTHESIS
  -> REJECTED (counter-evidence / failed prediction)
  -> accepted_facts (required checks pass)
  -> remains ACTIVE / UNKNOWN (evidence insufficient)
```

任何一步都不能因为模型换了措辞而自动升级。`OBSERVED` 也不是“真实世界事实”的无条件同义词：它只承诺某个带 provenance 的工件确实报告了该内容。

### RQ5｜谁可提出、验证、接受或拒绝 mutation？

官方资料能确认的是“运行时提交路径”：

- LangGraph node 返回 state update，框架按每个 key 的 reducer 应用；`update_state` 会创建新 checkpoint，而不是原地改旧 checkpoint。
- Google ADK 推荐通过 Context、`output_key` 或 `EventActions.state_delta` 更新；Runner 把更新封装为 Event，SessionService 在 append event 时应用 state delta。文档明确警告直接修改取回的 `session.state` 会绕过 event history、持久化、线程安全和时间戳维护。
- OpenAI Agents SDK 的 local context 是应用对象，不会自动进入 LLM；可恢复 `RunState` 是一个显式序列化边界。

但这些机制并不自动提供业务语义验证。Google ADK 的 `output_key` 甚至可以把模型最终文本经受管运行时直接写入 state。这是重要反证：**“框架提交了更新”不能升级为“host 已确认内容为真”。**

因此本篇的权限模型应标 `COURSE PROPOSAL`：

| 动作 | 默认主体 | 必做检查 |
|---|---|---|
| propose | model、tool、operator | 只产生 `MutationCandidate`，不得声称已接受 |
| validate | host policy / deterministic validator | schema、allowed fields、identity / revision、transition guard、evidence refs、冲突 |
| reduce | deterministic reducer | 合并规则、幂等、冲突结果；reducer 不是语义真值裁判 |
| commit | runtime / state store | 写新 revision、记录 mutation event，必要时 checkpoint |
| accept / reject claim | host-defined acceptance policy，必要时 human | 对照证据阈值、scope 与风险级别 |

推荐 mutation envelope：

```yaml
mutation_candidate:
  base_revision: 7
  actor: model
  operation: add_hypothesis
  value:
    statement: "缺失的 using 可能导致 CS0103"
    evidence_refs: ["obs-console-001"]
    next_test: "检查符号声明、命名空间与编译程序集"
```

只有 host 验证并由 reducer 生成 revision 8 后，才是 committed working state。即便提交成功，它仍可能只是 `HYPOTHESIS`。
### RQ6｜什么可以丢，什么应持久化？

没有统一的持久化清单；以下是 `COURSE PROPOSAL`，以能否安全恢复、交接和避免重复副作用为判断依据。

| 默认可从 active state 丢弃 | 应任务级持久化 | 需要 checkpoint 的触发条件 |
|---|---|---|
| 重复的聊天原文；纯排版草稿；可廉价重算的中间变换；已被替代的详细计划；已经外置保存的大体积证据正文 | goal / completion criteria；revision；accepted facts + evidence refs；active hypotheses + next tests；unresolved；rejected summary；pending actions + authority；completion gaps | 跨进程 / context reset / handoff；即将执行高风险或有副作用动作；恢复成本高；需要审计；并发 writer 需要版本冲突检测 |

两个限制：

1. “从 active state 丢弃”不等于删除 Evidence 或 History。保留稳定 locator / digest 后，可以不把大工件重复塞进 Working Memory。
2. “短期”不等于“只在内存里”。Google ADK 的 Database / Vertex session service 与 LangGraph checkpointer 都能让 task / thread state 跨进程保存；作用域仍可只是当前任务。

如果任务短、无副作用、重算便宜且无需交接，可以不建 durable checkpoint。反过来，若 pending action 可能重复发布、提交或调用外部系统，必须在动作前后持久化 identity、revision、effect receipt 与 continuation point；这一点承接 Article 11 的 Checkpoint 边界，但本篇不重讲长任务恢复协议。

### RQ7｜Unity / BuildPilot CS0103 案例最多能安全支持到哪里？

本篇只能使用一个明确标为 synthetic / illustrative 的受控状态演进，不得写成已在真实 BuildPilot Runtime 跑过的 Lab。

安全的起点：

```text
OBSERVED:
  Unity Console artifact 报告 C# compiler diagnostic CS0103，
  并包含未解析 identifier、文件与行号。
```

Microsoft 官方定义只允许推出“该名称在当前 class / namespace / scope / context 中不存在”。Unity 2022.3 官方文档允许说明 Console 会展示脚本编译错误，并可查看详细日志。二者合起来仍不能证明：

- 一定缺少 `using`；
- 一定是 asmdef 引用、条件编译或生成代码的问题；
- BuildPilot 的真实仓库、环境或任务确实出现过该错误；
- 已找到根因或修复有效；
- 整个构建以该错误为终态失败，除非另有完整 build receipt。

安全的状态演进示例：

```text
rev 1 OBSERVED  : artifact reports CS0103 for BuildReceiptWriter
rev 2 HYPOTHESIS: symbol declaration was excluded by an active define
rev 3 HYPOTHESIS: required assembly reference is missing
rev 4 REJECTED  : define hypothesis rejected after compiled define set contains the declaration path
rev 5 UNKNOWN   : assembly graph has not yet been captured
completion_gap  : obtain asmdef graph + compiler invocation/profile + rerun receipt
```

这是课程示例，不是生产事实。每次 revision 只展示“调查状态怎样被受控更新”，不虚构 BuildPilot 运行结果。
## Primary Source Register

所有来源均于 `2026-08-22` 实时读取。

| Source ID | Primary source | Product / version scope | Relevant fact | Does not prove |
|---|---|---|---|---|
| `S-LG-PERSIST` | [LangGraph Persistence](https://docs.langchain.com/oss/python/langgraph/persistence) | 当前 hosted Python OSS docs；页面未固定 package version | checkpoints、threads、StateSnapshot、history、replay、`update_state` 新建 checkpoint | 跨框架 Working Memory 标准 |
| `S-LG-GRAPH` | [LangGraph Graph API](https://docs.langchain.com/oss/python/langgraph/graph-api) | 当前 hosted Python OSS docs；页面未固定 package version | nodes 返回 updates；reducers 决定 key 更新；state 可含 messages | reducer 自动验证语义真实性 |
| `S-LG-MEM` | [LangGraph Memory](https://docs.langchain.com/oss/python/langgraph/add-memory) | 当前 hosted Python OSS docs；页面未固定 package version | short-term memory 为 thread-level state；long-term store 跨 thread | 课程字段 schema |
| `S-ADK-STATE` | [Google ADK Session State](https://adk.dev/sessions/state/) | 当前 hosted docs；文档标注 Python / Go / Java / Kotlin `v0.1.0+`、TypeScript `v0.2.0+`；未锁定具体安装版本 | scratchpad、scope prefix、SessionService 持久化、推荐 state delta 更新、直接 mutation 风险 | 所有 runtime 都有相同 scope 或 semantic validator |
| `S-ADK-SESSION` | [Google ADK Session](https://adk.dev/sessions/session/) | 当前 hosted docs；未固定 SDK package version | Session 是 conversation thread；events 与 state 分开；Runner / Event / SessionService lifecycle | Session 与 Working Memory 是行业同义词 |
| `S-ADK-MEM` | [Google ADK Memory](https://adk.dev/sessions/memory/) | 当前 hosted docs；未固定 SDK package version | Session / state 与跨 past interactions 的 MemoryService 区分 | Article 15 的长期记忆设计结论 |
| `S-OAI-CONTEXT` | [OpenAI Agents SDK Context management](https://openai.github.io/openai-agents-python/context/) | 当前 hosted Python SDK docs；未固定 package version | local context 与 LLM conversation context 是两类概念 | local context 是持久记忆或 checkpoint |
| `S-OAI-RUNSTATE` | [OpenAI Agents SDK RunState](https://openai.github.io/openai-agents-python/ref/run_state/) | 当前 hosted Python SDK docs；未固定 package version | 可序列化、可恢复 run snapshot；保存 generated items / approvals 等 | 所有 Working Memory 都应采用 RunState schema |
| `S-OAI-SESSION` | [OpenAI Agents SDK Session protocol](https://openai.github.io/openai-agents-python/ref/memory/session/) | 当前 hosted Python SDK docs；未固定 package version | Session protocol 读取、追加特定 session 的 conversation history | History 就是当前 Investigation State |
| `S-TEMP-EVENT` | [Temporal Event History](https://docs.temporal.io/workflow-execution/event) | 当前 hosted Temporal docs；未固定 Server / SDK version | Event History 是 append-only log，服务用它跟踪进度并恢复应用状态 | 当前状态必须与 history 分库存放 |
| `S-M1-PAPER` | [Magentic-One original paper](https://arxiv.org/abs/2411.04468) | 2024 paper / arXiv `2411.04468`，研究系统而非标准 | Task Ledger / Progress Ledger 的字段与 task-duration short-term memory | 本课程精确字段、taxonomy 或可靠生产效果 |
| `S-PROV` | [W3C PROV Overview](https://www.w3.org/TR/prov-overview/), [PROV Constraints](https://www.w3.org/TR/prov-constraints/), [PROV-O](https://www.w3.org/TR/prov-o/) | W3C Recommendations，2013 | entity / activity / agent、derivation、revision、invalidation、attribution | claim 为真、满足课程证据门或已被接受 |
| `S-CS0103` | [Microsoft C# CS0103](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103) | 当前 C# compiler message docs；未绑定 Unity Roslyn 具体版本 | 名称在当前 context 不存在；列出多种检查方向 | 单凭 code 断定具体根因 |
| `S-UNITY-CONSOLE` | [Unity 2022.3 Console](https://docs.unity3d.com/2022.3/Documentation/Manual/Console.html), [scriptCompilationFailed](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/EditorUtility-scriptCompilationFailed.html) | Unity `2022.3` documentation | Console 展示脚本编译错误；编译错误可由日志状态反映 | BuildPilot 真实运行、根因、修复或完整 build outcome |

## Counter-evidence and Disconfirming Cases

1. **术语反例**：LangGraph 把 thread state 称作 short-term memory，Google ADK 把 session state 称作 scratchpad，OpenAI Agents SDK 把 Session 主要用于 history；不能把任一命名推广为行业定义。
2. **存储反例**：Database / checkpoint 可让“短期”状态跨进程长期存在，说明 storage duration 不能单独决定 memory category。
3. **结构反例**：LangGraph state 可以有 messages channel；History 与 current state 的逻辑边界不保证物理隔离。
4. **恢复反例**：Temporal 从 append-only Event History 恢复应用状态；“History 不是 State”不能写成“History 无法产生 State”。
5. **checkpoint 反例**：LangGraph checkpoint 是一个 state snapshot；“Checkpoint 不等于 Working Memory”不能写成二者从不包含彼此。
6. **权限反例**：Google ADK `output_key` 可把模型输出通过受管流程写入 state；runtime authority 不自动等于 semantic validation。
7. **provenance 反例**：W3C PROV 记录派生和归属，不判定 claim 为真。
8. **置信度反例**：本次来源没有给出跨任务可比、已校准的 confidence score 语义；数字置信度不得作为核心字段硬写。
9. **诊断反例**：CS0103 对应多个可能原因；单条诊断不能锁定 `using`、asmdef、define 或生成代码中的任何一个。

## Open Questions and Limitations

- 没有找到跨 LangGraph、Google ADK、OpenAI Agents SDK 与 workflow systems 共同采用的 Working Memory schema；精确字段必须维持 `COURSE PROPOSAL`。
- 没有一手来源证明 model-produced mutation 默认接受语义校验；本篇必须把 runtime commit 与 claim acceptance 拆开。
- reducer 的代数性质、并发 CRDT、分布式事务不属于本篇；只保留 revision / deterministic reducer 的最小边界。
- 数字 confidence 的校准、评测与更新规则证据不足；默认不用。
- 本次不运行 Unity / BuildPilot Lab；CS0103 仅为基于官方诊断含义的 synthetic investigation-state example。

## Article 15 / 16 Non-scope

- 不设计 Session identity、跨 session continuity、Long-term Memory、Project Memory、memory consolidation、retention / deletion policy；这些留给 Article 15。
- 不设计 RAG、embedding、vector store、retrieval ranking、chunking、citation injection 或 knowledge retrieval evaluation；这些留给 Article 16。
- Article 14 只允许保存 task-scoped `evidence_refs`；“根据查询取回外部知识”不是本篇主线。

## Research Conclusion

研究可以进入独立 `EVIDENCE_GATE` 验证。核心行为主张都能由官方 / 原始来源确认、收窄，或诚实降级为 `COURSE PROPOSAL`；没有需要靠猜测补齐的 `BLOCKED` claim。文章应把主线放在：**模型产生候选变化，host 按证据和 revision 验证，reducer 提交一版可恢复但不越权的 Investigation State。**
