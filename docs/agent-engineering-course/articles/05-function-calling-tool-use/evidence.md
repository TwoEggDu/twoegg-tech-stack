# Article 05 Evidence Register｜Function Calling 与 Tool Use

- Gate Status：`PASS_CANDIDATE / AWAITING_MASTER_VERIFICATION`
- Evidence Recommendation：`PASS`
- Required Lab：`NONE`
- Provider Calls：`NONE`
- Provider Runtime Evidence：`UNVERIFIED`
- Tool Execution：`NONE`
- Tool Schema Fixture：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`
- Message Traces：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT`
- Claim Summary：`6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`

> Gate PASS 只覆盖 current official documentation contract 与明确标注的课程 working boundary。它不证明任何 Provider request、model choice、tool execution、side effect、runtime error recovery 或 Agent Loop 已在本 transaction 发生。

## Claim Register

| Claim ID | Claim | Status | Evidence | Wording guard |
|---|---|---|---|---|
| `05-C01` | 对 client-executed tools，model tool call 是结构化调用请求；Host/application 另行决定 route / reject / execute，故 `Tool Call != Executed` | `CONFIRMED` | `S-01`、`S-03`、`S-07` | 不覆盖 Provider server/built-in tools；不声称本轮已执行 |
| `05-C02` | Tool name / description / input schema 与 tool choice 是 Provider-scoped model-visible contract；可以改变可见能力与可表达参数，不能在无实验时声称提高选择或参数质量 | `CONFIRMED` | `S-01`、`S-02` | exact fields / strict / choice values 必须带 Provider/API/model/date scope |
| `05-C03` | Tool call 与 Tool Result 必须通过 Provider-specific identifier 关联并进入下一次 model request；OpenAI 用 `call_id`，Anthropic 用 `tool_use.id -> tool_use_id` | `CONFIRMED` | `S-01`、`S-03` | 不写成统一 wire schema；official trace 不是 local observation |
| `05-C04` | Streaming arguments fragment 与 completed arguments 是不同状态；fragment 先按 call/block 归属累积，completion 后才 Parse / Validate | `CONFIRMED` | `S-01`、`S-04`、`S-05`、`R-01`、`R-02` | fine-grained invalid JSON counter-example 只按 Anthropic feature scope；strict mode 另行核对 |
| `05-C05` | 一条 model response 可以包含多个 call；是否 concurrent / sequential execute 由 Provider control surface、Tool semantics 与 Host 决策共同限定，每个结果都需独立 correlation | `CONFIRMED` | `S-01`、`S-06` | 不声称本轮观察到 parallel calls；不把“多个”写成“必须并行” |
| `05-C06` | Host 必须按 registry 处理 function name、拒绝 unknown、处理 incomplete / invalid arguments，并在 schema 之后独立做 domain / authorization decision；`Schema Valid != Authorized` | `CONFIRMED` | `S-01`、`S-05`、`S-07`、`R-01` | 不声称模型实际生成 unknown call；权限系统实现留给 06 / 19 |
| `05-C07` | Generic Tool Result 是回注给模型的 content；没有额外 provenance / verification contract 时，`Tool Result by itself != Evidence` | `PARTIAL` | `S-01`、`S-03`、`R-03` | 某些具体 server/search result 可带 citations；完整 Evidence Contract 留给 18 |
| `05-C08` | Tool Use 可以属于 Agent Loop，但一次 Tool Use 不足以证明课程定义下的 goal-driven multi-step Agent Loop | `PARTIAL` | `S-01`、`S-07`、`R-03`、`R-04` | 属于课程 working boundary；不主张行业统一 Agent 定义；正式机制留给 08 |

## Source Manifest

网络来源均于 `2026-08-20（Asia/Shanghai）` 实时打开；只使用 current primary / official documentation。Hosted docs 未 pinned，发布前需重查。

| ID | Source | Retrieved / Version Scope | Access | Used by |
|---|---|---|---|---|
| `S-01` | [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling) | OpenAI API current hosted guide；核心字段以 Responses contract 为主，tool choice / parallel / strict 按页面当前 API/model notes；2026-08-20 | `OPENED_CURRENT` | C01—C07 |
| `S-02` | [Anthropic Define tools](https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools) | Anthropic Messages current docs；choice / strict / model-thinking compatibility 按页面 scope；2026-08-20 | `OPENED_CURRENT` | C02 |
| `S-03` | [Anthropic Handle tool calls](https://platform.claude.com/docs/en/agents-and-tools/tool-use/handle-tool-calls) | Anthropic Messages client-tool current contract；2026-08-20 | `OPENED_CURRENT` | C01, C03, C07 |
| `S-04` | [Anthropic Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming) | Anthropic Messages streaming current docs；2026-08-20 | `OPENED_CURRENT` | C04 |
| `S-05` | [Anthropic Fine-grained tool streaming](https://platform.claude.com/docs/en/agents-and-tools/tool-use/fine-grained-tool-streaming) | `eager_input_streaming=true` feature scope；2026-08-20 | `OPENED_CURRENT` | C04, C06 |
| `S-06` | [Anthropic Parallel tool use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/parallel-tool-use) | Anthropic Messages current docs；2026-08-20 | `OPENED_CURRENT` | C05 |
| `S-07` | [Anthropic How tool use works](https://platform.claude.com/docs/en/agents-and-tools/tool-use/how-tool-use-works) | Anthropic client/server tool conceptual contract；2026-08-20 | `OPENED_CURRENT` | C01, C06, C08 |
| `R-01` | `content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md` | published local Article 03 | `READ_LOCAL` | C04, C06 |
| `R-02` | `content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md` | published local Article 04 | `READ_LOCAL` | C04 |
| `R-03` | `docs/agent-engineering-course/glossary.md` | current course glossary | `READ_LOCAL` | C07, C08 |
| `R-04` | `docs/agent-engineering-series-plan.md` + `docs/agent-engineering-course-plan-v3.1-review.md` | canonical + frozen Article 05 / `05—06` hard rule | `READ_LOCAL` | C08 and Gate decision |
| `R-05` | `docs/agent-engineering-course/audits/part-i-audit.md` | verified Part I Audit | `READ_LOCAL` | scope only；PI-F01—03 untouched |

## Evidence Cards

### `05-C01` — Model call intent and Host execution are separate

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-03`、`S-07`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI current Function Calling guide；Anthropic Messages current client/server tool docs。
- **Observation**：OpenAI 把“receive a tool call”与“execute code on the application side”列为不同步骤；Anthropic client-tool contract 同样由应用读取 `tool_use`、运行实际代码、再返回 `tool_result`。两家都把 model output 与 application operation 分开。
- **Counter-evidence**：OpenAI built-in tools 与 Anthropic server-executed tools 可由 Provider 基础设施运行；SDK Tool Runner 也可自动承载应用侧循环。
- **Interpretation**：稳定抽象只覆盖 client-executed tools：call 是请求，执行是 Host/runtime 的后续决定。自动化 owner 可变化，责任 seam 不消失。
- **Proves**：`Tool Call != Executed`；Function Calling contract 本身不等于完整 Tool Runtime。
- **Does Not Prove**：所有 Tool 都在应用进程运行；本文执行过 Tool；任何 call 已授权、成功或产生副作用。
- **Recheck notes**：发布前重查两家 client/server tool 分类；正文每次写 Host execution 时都保留 client-executed scope。

### `05-C02` — Tool definitions and choice are scoped model contracts

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-02`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI current Function Calling guide（主要以 Responses 为 scope）；Anthropic Messages current Define tools docs。
- **Observation**：OpenAI function definition 暴露 name、description、parameters 与 strict，tool choice 支持 auto / required / forced / allowed / none；Anthropic 暴露 name、description、input_schema，tool choice 支持 auto / any / tool / none，并有 model/thinking compatibility notes。
- **Counter-evidence**：两家字段形状、choice vocabulary、strict 要求与兼容范围不同；同一家不同 API 也可使用不同 item/message shape。
- **Interpretation**：课程只能抽象“model-visible capability + input shape + selection constraint”，不能定义跨 Provider wire schema。
- **Proves**：name / description / schema / choice 是模型调用 contract 的一部分；enum / types / required 会收窄可表达参数空间。
- **Does Not Prove**：更详细 description、更多 enum 或 forced choice 已观察到提高选择准确率、参数质量或任务成功率。
- **Recheck notes**：Calculator A/B 只能标 `SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`；任何效果措辞都必须返回 Provider A/B experiment。

### `05-C03` — Call/result correlation crosses the next request

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-03`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI Responses current documented flow；Anthropic Messages current client-tool flow。
- **Observation**：OpenAI model output 的 `function_call.call_id` 被复制到后续 input 的 `function_call_output.call_id`；Anthropic assistant `tool_use.id` 被复制到下一条 user `tool_result.tool_use_id`，随后再次调用 Messages API。
- **Counter-evidence**：OpenAI Chat Completions 使用 tool role / `tool_call_id`；Anthropic 把 call/result 放在 assistant/user content blocks。两家不是同一 wire format。
- **Interpretation**：稳定抽象是 `assistant/model call -> Host decision -> correlated result -> next model request`；identifier 名称与 message placement 保留 Provider scope。
- **Proves**：Tool Result 不是脱离 call 的普通字符串；Host 必须保留 correlation，parallel calls 更需一一对应。
- **Does Not Prove**：官方示例在本轮实际运行；result 真实；next response 正确；Tool 已授权或成功。
- **Recheck notes**：保持两条 official trace 并列；不要把课程 normalized sequence 伪装成 official payload。

### `05-C04` — Argument fragments are not completed validated arguments

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-04`、`S-05`、`R-01`、`R-02`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI Responses streaming current guide；Anthropic standard streaming current docs；Anthropic fine-grained tool streaming current feature scope。
- **Observation**：OpenAI 用 arguments delta 后接 arguments done / completed item；Anthropic standard streaming 的 `partial_json` 是 partial string，final `tool_use.input` 才是 object；fine-grained 模式明确可能产生 invalid/incomplete JSON 并要求 guard parse。
- **Counter-evidence**：OpenAI strict mode 与 Anthropic strict/standard buffering 可以在各自 scope 提供更强 schema guarantee；SDK accumulator helper 可降低手工组装成本。
- **Interpretation**：fragment、completed candidate、schema/domain-valid arguments、authorized action、executed result 必须分态；helper/strict 不能把早期 fragment 提前升级。
- **Proves**：stream fragment 只应缓冲并按 call/block 归属；completion 后才进入 Parse / Validate。
- **Does Not Prove**：本文实际观测过 event sequence、断流、max-token truncation 或 invalid JSON；两家所有 mode 的 validation 行为相同。
- **Recheck notes**：若文中使用 invalid JSON 例子，必须明确是 Anthropic fine-grained official contract，不是 local/provider observation。

### `05-C05` — Multiple calls do not prescribe Host concurrency

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-06`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI current guide，parallel availability / limitations 按其 model notes；Anthropic Messages current Parallel tool use docs。
- **Observation**：OpenAI 允许一个 response 出现多个 function calls 并提供 `parallel_tool_calls=false`；Anthropic 允许一个 assistant turn 出现多个 `tool_use` blocks，明确 API 不规定 Host 执行顺序，Host 可 concurrent、sequential 或混合，并逐个返回 correlated result。
- **Counter-evidence**：不是所有 model / tool 组合都支持相同 parallel 语义；有副作用、共享状态或依赖的 calls 不适合盲并发。
- **Interpretation**：`multiple calls in one response` 是 model/API output shape；`parallel execution` 是 Host + Tool semantics decision。
- **Proves**：单 call trace 不是唯一形态；每个 call 都要独立保留 id、arguments、decision 与 result。
- **Does Not Prove**：本轮出现过 multiple/parallel calls；某种顺序总是正确；Provider 会自动解决副作用冲突。
- **Recheck notes**：正文只需一段 scoped 说明，不扩成并发调度教程；完整 policy/trace 留给 Article 06。

### `05-C06` — Unknown, invalid, and unauthorized remain Host gates

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-05`、`S-07`、`R-01`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI current routing sample / strict notes；Anthropic fine-grained invalid-input path 与 client-tool execution contract；published Article 03 local boundary。
- **Observation**：OpenAI official `callFunction` sample 对未注册 name 抛出 unknown-function error；Anthropic fine-grained docs 说 invalid JSON 时不能运行 Tool，应返回 `is_error` result；client-tool operation 由应用代码执行。Article 03 已证明 schema-valid 只覆盖声明结构，不证明领域事实或执行。
- **Counter-evidence**：scoped strict tool mode 可以保证输出符合 input schema；因此不能笼统声称所有 completed arguments 都可能 schema-invalid。当前也没有实际观测模型生成 unknown name。
- **Interpretation**：Host 应按 registry fail closed、等待完整参数、解析并执行 schema/domain/policy gate；strict 只跳过某些结构性失败，不能生成本地 authorization。
- **Proves**：`Schema Valid != Authorized`；unknown/invalid 不能直接 execute；Provider call item 不会自动绕过 Host。
- **Does Not Prove**：模型实际会在本轮生成 unknown call；本篇已实现 permission / approval / timeout；error 反馈后模型一定 recover。
- **Recheck notes**：unknown call 只能作为 synthetic negative teaching case，标签 `NOT_EXECUTED`；Policy 实现必须推迟到 06/19。

### `05-C07` — Generic Tool Result is not Evidence by itself

- **Status**：`PARTIAL`
- **Source**：`S-01`、`S-03`、`R-03`
- **Retrieved / Version Scope**：`2026-08-20`；两家 current client-tool result contracts；current course glossary。
- **Observation**：OpenAI function call output 可承载 string / JSON-like content；Anthropic tool result 可承载 string 或多种 content blocks 并可标 error。Generic contract 没有统一要求 provenance、retrieval time、claim mapping 或 verification。
- **Counter-evidence**：特定 server/search tools、document blocks 或业务 Tool 可以返回 citations、source metadata 或已验证 artifact；这些额外合同可能让某个 result 成为 Evidence 输入。
- **Interpretation**：只采用窄命题：result content 不因位于 tool-result envelope 就自动成为 Evidence；需要额外 Evidence Contract 和验证。
- **Proves**：`Tool Result by itself != Evidence` 是安全的课程边界。
- **Does Not Prove**：Tool Result 永远不能作为 Evidence；本篇已经定义 Evidence schema；某个 server tool citations 不可信。
- **Recheck notes**：保持 `PARTIAL / COURSE WORKING BOUNDARY`；不得提前展开 Article 18。

### `05-C08` — One tool use is insufficient evidence of an Agent Loop

- **Status**：`PARTIAL`
- **Source**：`S-01`、`S-07`、`R-03`、`R-04`
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI/Anthropic current conceptual docs；current course glossary/canonical。
- **Observation**：两家文档都允许 call -> result -> next response，并可继续更多 calls；Anthropic 把持续处理写成 tool-use / agentic loop。课程 glossary 则把 Agent 定义为围绕目标、由模型参与推进并处理多步行动/反馈的软件系统，正式 Loop 在 Article 08 展开。
- **Counter-evidence**：SDK Tool Runner 或 Provider server-side loop 可能在一次 API abstraction 内部自动持续多轮；生态可把这类能力称为 agentic。
- **Interpretation**：Tool Use 是 Agent Loop 可能使用的机制，但一次 Tool Use 只能证明一个 call/result 往返，不能单独证明课程的 goal/state/step/stop 闭环。
- **Proves**：`one Tool Use is not sufficient evidence of an Agent Loop`；Article 05 应把 Loop 正式定义推迟到 08。
- **Does Not Prove**：行业存在唯一 Agent 定义；所有 single-turn tool 应用都“不是 Agent”；官方产品术语错误。
- **Recheck notes**：正文使用“课程定义下不足以证明”而非绝对分类；不要提前讲 Planning / Workflow / long-running。

## Fixture / Trace Boundary

### Tool Schema Fixture

- Status：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`
- Form：仅在 Research 中保存 `Calculator-A` free-form expression 与 `Calculator-B` enum + typed operands 对照表。
- Provider request / response：`NONE / NONE`
- Expected result：`NONE`
- Observed result：`NONE`
- Allowed use：解释 model-visible contract 与可表达参数空间的差异。
- Forbidden use：选择准确率、schema adherence、错误率、latency、token 或最终质量比较。

### Message Trace 1 — OpenAI Responses

- Status：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`
- Sequence：request with function tools → `function_call(call_id, name, arguments)` → application routing / optional execution → `function_call_output(call_id, output)` in next input → next response。
- Proves：OpenAI Responses current documented correlation / continuation shape。
- Does Not Prove：本地 Provider/runtime、Tool side effect、output truth 或 final-answer quality。

### Message Trace 2 — Anthropic Messages

- Status：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`
- Sequence：request with tools → assistant `tool_use(id, name, input)` → application routing / optional execution → next user `tool_result(tool_use_id, content)` → next assistant response。
- Proves：Anthropic Messages current documented correlation / content-block placement。
- Does Not Prove：本地 Provider/runtime、Tool side effect、result truth 或 final-answer quality。

### Runtime boundary

- Provider Roundtrip：`NONE / UNVERIFIED`
- Tool Execution：`NONE`
- Local code fixture：`NONE`
- Required Lab：`NONE`

Actual Provider roundtrip is not required for the narrowed document-contract Claims above. It becomes required if later stages add model-selection quality、actual schema adherence、parallel frequency/order、runtime error recovery、SDK Tool Runner behavior 或 side-effect Claims；出现这些措辞必须 `RETURN_TO_RESEARCH`。

## Evidence Gate

| Status | Count | Claim IDs |
|---|---:|---|
| `CONFIRMED` | 6 | `05-C01`—`05-C06` |
| `PARTIAL` | 2 | `05-C07`、`05-C08` |
| `BLOCKED` | 0 | `NONE` |
| `PROPOSAL` | 0 | `NONE` |

### Gate checklist

| Requirement | Result | Notes |
|---|---|---|
| 每个核心 Claim 有 Evidence Card | `PASS` | 8 Claims / 8 Cards，一一对应 |
| current primary sources | `PASS` | OpenAI + Anthropic official docs，retrieved 2026-08-20 |
| Provider differences scoped | `PASS` | Responses item vs Messages content block 未归并为统一 wire schema |
| Counter-evidence present | `PASS` | server tools、strict、Tool Runner、parallel、rich results、agentic wording |
| Tool Schema fixture state truthful | `PASS` | `SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED` |
| Message Trace state truthful | `PASS` | 两家 `OFFICIAL EXAMPLE / NOT LOCALLY EXECUTED` |
| Provider/runtime observation | `NONE / UNVERIFIED` | 未假设 credentials，未调用 Provider |
| Core behavioral `BLOCKED` | `PASS` | `0`；runtime-only Claims 未进入 Register |
| `PARTIAL` wording narrowed | `PASS` | C07 只说 generic result 不自动成为 Evidence；C08 只说一次 Tool Use 不足以证明课程 Agent Loop |
| Required Lab | `NONE` | 未创建 Lab、code fixture 或 assets |
| Article 06 hard-rule leakage | `PASS` | 本篇不声称 Tool Runtime failure injection；该证据仍必须由 Article 06 独立取得 |

### Recommendation：`PASS`

Official current contracts are sufficient for Article 05's narrowed mechanism Claims：tool definitions、tool choice surface、call/result correlation、arguments completion seam、multiple-call shape 与 Host decision boundary。真实 Provider roundtrip 对任何“实际模型行为”Claim 仍是必要证据，但本 Register 不包含这种 Claim，因此不能把 runtime `UNVERIFIED` 误写成 blocker，也不能借 PASS 把它升级为 confirmed。

### Blocker

`NONE` for Outline entry after Master verification.

### Next action

`OUTLINE`。必须保留全部 Provider scope、两项 `PARTIAL`、fixture / trace 标签和 runtime `UNVERIFIED`；不得创建 Lab 或提前进入 Article 06。

## Stop Line

Researcher 只交付 Research / Evidence 与 Gate recommendation。Master 未验证 Gate 前，不得创建 Outline / Draft；后续若出现未登记的 runtime Claim、schema/description 效果 Claim、Tool Runtime failure path 或 Agent Loop 机制，必须返回 Research，不得用本 Gate PASS 降级措辞。
