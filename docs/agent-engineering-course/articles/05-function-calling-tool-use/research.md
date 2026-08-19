# Article 05 Research｜Function Calling 与 Tool Use

- Research Phase：`RESEARCH`
- Research Status：`COMPLETE`
- Lifecycle Candidate：`EVIDENCE_READY`
- Evidence Status：`PASS_CANDIDATE`
- Evidence Gate Recommendation：`PASS`
- Required Lab：`NONE`
- Research Window：`2026-08-20（Asia/Shanghai）`
- Provider Calls：`NONE`
- Provider Runtime Evidence：`UNVERIFIED`
- Tool Execution：`NONE`
- Tool Schema Fixture：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`
- Message Traces：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT`
- Core Claim Summary：`6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`

> 本轮证据上限是“OpenAI Responses 与 Anthropic Messages 当前官方公开合同已核对”。没有读取 credentials，没有调用 Provider，也没有执行 Tool。文中的 Tool Schema 对照只是未执行的教学 fixture；官方消息 Trace 证明文档合同，不是本 transaction 的 runtime observation。

## Research Questions

| RQ | Question | Status | Primary Claims |
|---|---|---|---|
| `RQ-01` | Function Calling / Tool Use 在 current Provider contract 中分别怎样表达，哪些职责可以抽象，哪些字段必须按 Provider / API / model / version 核对？ | `ANSWERED` | `05-C01`—`05-C05` |
| `RQ-02` | Tool definition 的 name、description、parameters、enum 与 tool choice 能安全支持哪些结论，不能外推哪些模型选择效果？ | `ANSWERED` | `05-C02` |
| `RQ-03` | Tool Call ID、arguments、assistant tool-call item / content block 与 Tool Result 回注之间的最小消息时序是什么？ | `ANSWERED` | `05-C03`、`05-C04` |
| `RQ-04` | 模型提出不存在、不允许或 schema-invalid 的 Tool Call 时，official contract 与 Host responsibility 各能证明什么？ | `ANSWERED_WITH_SCOPE` | `05-C06` |
| `RQ-05` | 什么证据足以说明 `Tool Call != Executed`、`Tool Result != Evidence` 与 `Tool Use != Agent Loop`？ | `ANSWERED_WITH_COURSE_BOUNDARY` | `05-C01`、`05-C07`、`05-C08` |
| `RQ-06` | canonical 要求的 Tool Schema fixture / message trace 应是 official trace、local deterministic fixture、Provider roundtrip，还是明确的 `PROPOSAL / NOT_EXECUTED`？ | `ANSWERED` | Fixture / Trace Decision |
| `RQ-07` | 本篇与 Article 06 Tool Runtime、07 MCP、08 Agent Loop 的 stop line 应怎样保持？ | `ANSWERED` | Risk and Stop Lines |

## Research Answers

### 1. 稳定抽象只覆盖 client-executed tools

OpenAI 与 Anthropic 的当前官方文档都支持下面这条最小责任链，但字段、角色和 content shape 不相同：

```text
Tool definitions visible to the model
  -> model response contains one or more tool-call requests
  -> Host/application correlates the call and decides whether/how to handle it
  -> Host assembles and validates completed arguments
  -> Host may reject, report an error, or execute registered code
  -> Host returns a correlated tool result in the next model request
  -> model returns text or more tool calls
```

这里的范围必须写成 `client-executed function/tool`。OpenAI 还有 built-in tools，Anthropic 还区分 server-executed tools；这些能力可由 Provider 基础设施执行。它们反驳“所有 Tool 都一定由应用进程执行”，但不改变本篇核心边界：**模型侧产生的 client-tool call 是结构化请求，不是应用侧副作用已经发生的证明。**

`Function Calling` 也不是一个完整执行系统。两家官方流程都把“收到 call”与“应用执行”列为两个步骤；Host 可以在二者之间插入 registry lookup、完整参数组装、Parse、Schema / Domain Validation、Policy / Approval 与拒绝路径。Article 05 只建立这条 seam，Article 06 才正式展开 Tool Runtime pipeline。

### 2. Provider contract 可以对照，不能拼成统一字段

| Contract surface | OpenAI Responses current guide | Anthropic Messages current docs | Stable course interpretation |
|---|---|---|---|
| Tool definition | function tool 使用 `type`、`name`、`description`、`parameters`，可带 `strict` | client tool 使用 `name`、`description`、`input_schema`，current docs 也提供 strict tool use | 都向模型提供名称、说明和输入结构；字段形状与 schema subset 按 Provider scope 核对 |
| Tool choice | `auto`、`required`、forced function、allowed tools、`none` | `auto`、`any`、specific `tool`、`none`；parallel 控制位在 `tool_choice` 内 | API 可约束“是否/可选哪些 Tool”，不证明选择质量或业务允许执行 |
| Tool call | output item `type=function_call`，含 `call_id`、`name`、JSON-encoded `arguments` | assistant `tool_use` content block，含 `id`、`name`、object `input` | 都是 model response 中的调用请求；不能用一家字段命名覆盖另一家 |
| Tool result | input item `type=function_call_output`，用相同 `call_id` 关联 | 下一条 user message 中的 `tool_result` block，用 `tool_use_id` 关联 | Result 必须和 call 相关联并进入下一次 model request；消息 role / item shape 不统一 |
| Argument streaming | `response.function_call_arguments.delta` 后有 `...arguments.done` / completed item | `input_json_delta.partial_json`；block 完成后 final `tool_use.input` 为 object | fragment 只可缓冲；完成边界后才 Parse / Validate |
| Multiple calls | 支持的 model 可在一轮产生多个 function calls；`parallel_tool_calls=false` 可限制为至多一个 | 一条 assistant response 可含多个 `tool_use`；Host 自行决定 concurrent / sequential execution | “同一响应有多个 call”不等于“Host 必须并行执行”；必须逐 call 关联结果 |

OpenAI 的 strict mode 与 Anthropic 的 strict tool use 都是 Provider-scoped schema contract。它们可以反驳“Tool arguments 永远只是 best effort”，但不能推出 `Schema Valid == Domain Valid == Authorized == Executed`。Schema 只约束声明的输入形状；registry、领域事实、权限与副作用仍是另一组 Host 决策。

### 3. Tool Schema 是 model-visible contract，不是本轮观测到的效果提升

两家官方文档都把 tool name、description 与 input schema 放入 model-visible tool definition；tool choice 又能限制零个、至少一个、指定 Tool 或禁止 Tool。可安全确认的是：

- 修改 name / description / schema 会修改模型收到的能力合同；
- `enum`、required fields 与 object shape 会修改可表达的参数空间；
- strict 语义与支持的 JSON Schema subset 必须按 Provider / API / model / version 核对；
- forced / required / none 是 API-level choice control，不等于 Host authorization。

本轮没有 A/B Provider call，因此不能写：

- 更长 description 已观察到提高 Tool 选择准确率；
- enum 已观察到降低错误率；
- Schema B 比 Schema A 对某个模型更可靠；
- forced tool choice 提高了最终任务质量。

官方 best-practice 文本可以作为设计指导，不能替代本课程自己的 runtime observation。

### 4. Call / Result correlation 是完整往返的骨架

OpenAI Responses 官方示例把 model output 中的 `function_call.call_id` 带到下一次 input 的 `function_call_output.call_id`；Anthropic Messages 官方示例把 assistant `tool_use.id` 带到下一条 user `tool_result.tool_use_id`。因此课程可以稳定抽象：

```text
assistant/model tool-call request
  -> Host decision / optional execution
  -> correlated tool-result content
  -> next model request
  -> text or additional calls
```

这个抽象不能删除中间的 Host decision，也不能把 Result 当成模型调用之外自动生成的事实。SDK Tool Runner 可以机械管理循环和消息格式，但只是在某个 SDK/runtime 中承载责任，不会让 call/result correlation、validation 或 authorization 消失。

### 5. Arguments fragment 与 completed arguments 必须分开

OpenAI Responses 的 current guide 展示 `response.output_item.added`、多条 `response.function_call_arguments.delta`、`response.function_call_arguments.done` 与 `response.output_item.done`；Anthropic current streaming docs 把 `input_json_delta.partial_json` 定义为 partial JSON string，并在 content block 完成后才得到 final object。

因此可以确认：

1. fragment 只进入 keyed buffer；
2. 用 output index / item id / content block index 保持并发 call 的片段归属；
3. 到 Provider 定义的 block/item completion 后才取得 completed arguments candidate；
4. candidate 随后才进入 Parse / Schema / DTO / Domain / Policy；
5. fragment、completed candidate、validated arguments、authorized action 与 executed result 是五个不同状态。

Anthropic fine-grained tool streaming 是更强的 counter-example：该模式明确跳过 server-side buffering / JSON validation，累计后可能得到 invalid or incomplete JSON；官方要求 parse guard，无法解析时不能执行 Tool，而应返回 error result。这个结论只适用于该 current feature scope，不能反推 Anthropic standard buffered mode 或 OpenAI strict mode 也同样不校验。

### 6. Unknown / invalid / unauthorized 的 Host responsibility

当前证据不支持“模型一定会生成不存在的 Tool”这一 runtime 行为主张，因为本轮没有 Provider call。它支持的是更窄、也更重要的工程合同：

- OpenAI 官方路由示例显式对未注册 function name 抛出 `Unknown function`；
- Anthropic fine-grained streaming 官方文档明确 invalid JSON 时不能执行 Tool；
- 两家 client-tool 流程都由应用代码执行 operation，Provider tool call 本身没有绕过 Host；
- strict schema 只证明 scoped input-schema conformance，不包含本地 registry、domain rule、permission、approval 或 side-effect policy。

因此 Host 必须 fail closed：按允许的 registry 解析 name，等待完整参数，执行 Parse / Schema / Domain / Policy，再决定 reject / error / execute。`Tool Call != Executed` 与 `Schema Valid != Authorized` 是这条责任链的直接边界；权限模型本身留给 Article 06 与 19。

### 7. Tool Result 只是返回内容，Evidence 需要额外合同

OpenAI `function_call_output` 可以是 string / JSON-like content；Anthropic `tool_result.content` 可以是 string 或多种 content blocks，还可带 `is_error`。这些合同证明 Result 是回注给模型的消息内容，并没有为任意 Result 自动提供真实性、来源、采集时间、claim-to-source mapping 或独立 verification。

所以本篇只采用窄结论：`Tool Result by itself != Evidence`。某些 server tool result、search result 或业务 Tool 可以携带 citations / provenance；那是具体 Tool/Provider 的额外合同，不是 generic Tool Result block 的普遍保证。正式 Evidence Contract 留给 Article 18，故 `05-C07` 保持 `PARTIAL`，不提前建立完整 Evidence schema。

### 8. 一次 Tool Use 不是课程定义下的 Agent Loop

官方文档可以把 tool-use roundtrip 包装为 multi-step flow、SDK Tool Runner 或 agentic loop；这说明 Tool Use 能成为 loop 的一部分。它不能证明任何一次 request → call → result → response 都已经具备课程 glossary 中 Agent 所需的 goal-driven multi-step progression、runtime state 与 stop semantics。

本篇只确认必要性关系，不声称行业统一定义：`Tool Use can participate in an Agent Loop`，但 `one Tool Use is not sufficient evidence of an Agent Loop`。Article 08 才正式定义 Turn、Step、Decide、Act、Observe 与 Stop。因此 `05-C08` 保持 `PARTIAL / COURSE WORKING BOUNDARY`。

## Tool Schema Fixture Decision

Status：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`

不创建代码 fixture、不调用 Provider。只保留下面的文档内对照，供 Outline 解释“model-visible contract 发生了什么变化”：

| Fixture | Name / description | Parameter surface | What it may illustrate | What it cannot prove |
|---|---|---|---|---|
| `Calculator-A` | `calculate`；说明只写“calculate a value” | 单个 free-form `expression: string` | model-visible contract 较开放，Host 需要解析更宽输入 | 模型一定选错；错误率；安全性；运行效果 |
| `Calculator-B` | `calculate_binary`；明确二元算术和允许 operation | `operation: enum(add, subtract)` + `left/right: number` | enum / types / required fields 收窄可表达参数空间 | 选择率提升；参数 adherence 提升；跨 Provider 一致性 |

这不是实验：没有 prompt、model、temperature、sample size、Expected / Observed、输出或统计结果。若正文需要任何“提高/降低/更可靠”的行为性结论，必须返回 Research 并执行另行授权的 Provider A/B observation；本 transaction 不具备该证据。

## Message Trace Decision

Status：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT`

| Trace | Official sequence | Correlation | Evidence class |
|---|---|---|---|
| OpenAI Responses | request with function tools → model `function_call` output → application routes / executes → next input contains `function_call_output` → next response | `function_call.call_id == function_call_output.call_id` | `OFFICIAL EXAMPLE / NOT LOCALLY EXECUTED` |
| Anthropic Messages | request with tools → assistant `tool_use` block → application routes / executes → next user message contains `tool_result` → next assistant response | `tool_use.id == tool_result.tool_use_id` | `OFFICIAL EXAMPLE / NOT LOCALLY EXECUTED` |

两条 Trace 足以证明各自 current documentation contract 和课程的最小 correlation 抽象；它们不证明本地 credentials、network、SDK、model response、Tool side effect 或 final answer 已在本轮运行。

## Provider Roundtrip Decision

- Credentials：`NOT_ASSUMED / NOT_READ`
- Provider request：`NONE`
- Provider response：`NONE`
- Runtime observation：`UNVERIFIED`
- Required for current Gate：`NO, FOR THE NARROW DOCUMENT-CONTRACT CLAIMS ONLY`

理由：Article 05 是 `NORMAL_ARTICLE / Required Lab NONE`，核心机制是两家 current official contract 中可直接复查的 tool definition、call item/block、correlation、result-return 与 streaming completion seam。两家官方页面都给出了完整 documented roundtrip；再做一次 live call 只会观察某个 Provider/API/model/request 的样本，不能升级为跨 Provider 普遍事实。

实际 roundtrip 在以下措辞出现时才是必要证据：

- “模型在 Schema B 下更常选择正确 Tool”；
- “某模型总能 / 不能遵守 schema”；
- “parallel calls 的实际频率 / 顺序是……”；
- “unknown tool、invalid arguments 或 tool error 在 runtime 中实际怎样恢复”；
- “SDK Tool Runner 已执行、重试或产生某条 trace”。

本篇不保留这些 Claim，因此没有因未调用 Provider 而留下核心 `BLOCKED`。冻结计划中的 `05—06` hard rule 被拆到真实责任边界：Article 05 用两家 `OFFICIAL EXAMPLE` 闭合 Function Calling documented roundtrip；Article 06 仍必须独立取得 Tool Runtime failure-injection 的真实 evidence，不能从本篇 PASS 借用或推定。

## Stable Abstraction and Provider Differences

### Stable abstraction

- Tool definition 是给模型看的候选能力合同，不是本地 registry 本身。
- Tool call 是 model output 中的行动请求，不是已授权、已执行或已成功。
- Host 负责完整参数、registry、validation、policy 与 execution decision。
- Result 必须与 call correlation，并作为后续 model request 的输入。
- 一条响应可以含多个 call，但 execution order / concurrency 由 Host 与 Tool semantics 决定。
- Tool result content 需要额外 provenance / verification 才能成为 Evidence。
- Tool-use roundtrip 可以属于普通 AI Application；是否构成 Agent Loop 需要后续状态 / step / stop 合同。

### Provider-specific facts that must retain scope

- OpenAI Responses 的 item types、`call_id`、JSON-encoded `arguments`、`function_call_output`、streaming event names、tool choice values 与 parallel model notes。
- Anthropic Messages 的 assistant/user content block placement、`tool_use.id`、`tool_result.tool_use_id`、`input_json_delta.partial_json`、`is_error`、result ordering rules 与 `disable_parallel_tool_use` placement。
- strict schema subset、tool choice compatibility、parallel availability、server-executed tool semantics 与 model limitations。

## Counter-evidence and Alternatives

1. **Server tools / built-in tools**：Provider 可以替应用执行某些 Tool；因此只把 Host-execution 抽象用于 client-executed tools。
2. **Strict tool use**：scoped strict mode 可以保证输入匹配 schema；因此不写“模型参数永远不可靠”，只保留 authorization / domain seam。
3. **SDK Tool Runner**：SDK 可以自动循环、执行和回注；因此不写“每个应用必须手写 loop”，只要求责任和证据仍可定位。
4. **Parallel call response**：同一 assistant/model response 可以含多个 call；因此不把单 call Trace 外推为协议唯一形态。
5. **Rich result types**：某些 result 可携带 document、search result、citation 或 provider-managed metadata；因此只写“generic Tool Result 不自动等于 Evidence”。
6. **Provider terminology**：OpenAI 当前把 function calling 也称 tool calling，Anthropic 使用 tool use；共同教学抽象不是行业统一 wire protocol。
7. **Agentic wording**：Anthropic official conceptual docs 会把持续 tool use 描述为 agentic loop；课程只判断一次 Tool Use 不足以证明完整 Agent，不否定产品术语。

## Source Manifest

所有网络来源均为 current primary / official documentation，并于 `2026-08-20（Asia/Shanghai）` 实时打开核对。Hosted docs 未固定 commit 或版本号，后续发布前必须重查。

| ID | Source | Retrieved / Version Scope | Observation used |
|---|---|---|---|
| `S-01` | [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling) | OpenAI API current hosted guide；主要使用 Responses contract，另把 guide 中 Chat Completions 形状视为同 Provider 的不同 API surface；2026-08-20 | client-tool flow、function definition、`call_id`、result return、unknown function sample、tool choice、strict、parallel、streaming delta/done |
| `S-02` | [Anthropic Define tools](https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools) | Anthropic Messages current hosted docs；example model / thinking compatibility 只按页面 scope；2026-08-20 | `name` / `description` / `input_schema`、tool choice、strict、assistant `tool_use` block |
| `S-03` | [Anthropic Handle tool calls](https://platform.claude.com/docs/en/agents-and-tools/tool-use/handle-tool-calls) | Anthropic Messages client-tool current contract；2026-08-20 | `tool_use.id`、`tool_result.tool_use_id`、user/assistant block placement、`is_error`、next-request ordering |
| `S-04` | [Anthropic Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming) | Anthropic Messages streaming current docs；2026-08-20 | `input_json_delta.partial_json`、content block completion、final input object |
| `S-05` | [Anthropic Fine-grained tool streaming](https://platform.claude.com/docs/en/agents-and-tools/tool-use/fine-grained-tool-streaming) | Anthropic fine-grained feature current docs；`eager_input_streaming=true` scope；2026-08-20 | unbuffered / unvalidated fragments、invalid JSON guard、do-not-execute / error-result path |
| `S-06` | [Anthropic Parallel tool use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/parallel-tool-use) | Anthropic Messages current docs；2026-08-20 | multiple `tool_use` blocks、Host-selected execution order、one correlated result per call、disable semantics |
| `S-07` | [Anthropic How tool use works](https://platform.claude.com/docs/en/agents-and-tools/tool-use/how-tool-use-works) | Anthropic client/server tool conceptual contract；2026-08-20 | client-vs-server execution boundary、documented tool-use loop |
| `R-01` | `content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md` | published local Article 03 | Parse / Schema / DTO / Domain boundary；schema-valid 不等于事实 / Tool execution |
| `R-02` | `content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md` | published local Article 04 | Provider/API scope、arguments fragment buffer / terminal boundary、runtime `UNVERIFIED` handoff |
| `R-03` | `docs/agent-engineering-course/glossary.md` | current course glossary | Tool、Agent、Host、Evidence working definitions；不补写 PI-F03 涉及的缺失术语 |
| `R-04` | `docs/agent-engineering-series-plan.md` + `docs/agent-engineering-course-plan-v3.1-review.md` | canonical + frozen Article 05 section / `05—06` hard rule | title、Part II responsibility、no Lab、Evidence assets、05 → 06 → 08 stop lines |
| `R-05` | `docs/agent-engineering-course/audits/part-i-audit.md` | verified Part I Audit | `PI-F01`—`PI-F03` remain `OPEN MINOR`；不修复、不借用不成立的 glossary support wording |

## Risk and Stop Lines

### Wording guards

- 必须写 `client-executed tool` 后才能使用“Host 执行”；不得覆盖 server/built-in tools。
- 必须写 Provider / API scope 后才能使用具体字段名、tool choice、strict 或 parallel semantics。
- description / enum 只能写成 model-visible contract 与 schema constraint；不得写成已观察效果提升。
- arguments fragment 只能写 buffer / assemble；completed candidate 仍需 validation / policy。
- `Tool Call != Executed`；`Schema Valid != Authorized`；`Tool Result != Evidence`；`Tool Use != Agent Loop`。
- official example 只能标 `DOC_CONFIRMED / OFFICIAL EXAMPLE`，不得写成 local Provider runtime observation。

### Stop lines

- Article 05 不实现 Validate / Policy / Execute / Timeout / Retry / Trace；这些属于 Article 06。
- Article 05 不讲 MCP transport / discovery / interoperability；这些属于 Article 07。
- Article 05 不正式定义 Turn / Step / loop state / stop；这些属于 Article 08。
- Article 05 不定义完整 Evidence schema；这些属于 Article 18。
- 若 Outline / Draft 需要 model adherence、selection quality、parallel frequency、error recovery 或 side-effect behavior，必须 `RETURN_TO_RESEARCH`；当前 Gate 不支持这些 runtime Claim。

## Evidence Gate Recommendation

### Recommendation：`PASS`

| Gate item | Result | Evidence |
|---|---|---|
| 两家 Provider current primary contracts | `PASS` | OpenAI `S-01`；Anthropic `S-02`—`S-07` |
| Tool definition / tool choice | `PASS_WITH_WORDING_GUARD` | `05-C02`；只证明 contract surface，不证明效果提升 |
| Call ID / result correlation / next request | `PASS` | `05-C03`；两家 official examples |
| Fragment vs completed arguments | `PASS` | `05-C04`；OpenAI delta/done + Anthropic partial/final/invalid scoped counter-example |
| Unknown / invalid / authorization Host boundary | `PASS_WITH_SCOPE` | `05-C06`；不声称实际观测 unknown Tool call |
| Tool Result / Evidence 与 Tool Use / Agent Loop | `PASS_WITH_COURSE_BOUNDARY` | `05-C07`、`05-C08`；保持 `PARTIAL`，正式定义留后文 |
| Tool Schema fixture | `PASS_AS_SYNTHETIC_TEACHING_FIXTURE` | `NOT_EXECUTED`；无效果 Claim |
| Message trace | `PASS_AS_OFFICIAL_EXAMPLE` | 两家 documented roundtrip；不是 local runtime observation |
| Core behavioral claims blocked | `PASS` | `0 BLOCKED`；所有 runtime-only 措辞已排除 |
| Required Lab / Provider call | `NONE / NONE` | Normal Article boundary |

### Residual blocker

`NONE` for Outline entry, provided the Master accepts this Evidence Gate and the Outliner retains every Provider scope / `PARTIAL` / `UNVERIFIED` guard. Provider runtime、Tool execution、selection quality 与 failure recovery 仍为 `UNVERIFIED`，但本篇没有用它们支撑核心 Claim。

### Next action

`OUTLINE`。Outliner 应按 `05-C01`—`05-C08` 绑定问题空间、稳定抽象、两家消息映射、Host stop line 与学习检查；不得把 Calculator fixture 写成 A/B 实验结果，不得创建 Lab，不得提前展开 Article 06 或 08。
