# Article 04 Evidence Register

- Evidence Phase：`RESEARCH / COMPLETE`
- Evidence Status：`PASS_CANDIDATE`
- Evidence Gate：`PASS_RECOMMENDED`
- Claim Count：`8`
- Claim Summary：`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`
- Evidence Card Count：`8`
- Retrieved / Verified At：`2026-08-20（Asia/Shanghai）`
- Required Lab：`NONE`
- Provider Calls：`NONE`
- Runtime Evidence：`UNVERIFIED`

> 本 Register 证明的是检索日可访问的官方公开合同与由其支持的收窄解释。它不证明 Provider 在本 transaction 中被调用，也不证明所有 SDK 语言、模型、区域、云托管渠道或未来版本都具备相同行为。

## Claim Register

| Claim ID | Core claim | Status | Primary evidence | Scope / guardrail |
|---|---|---|---|---|
| `04-C01` | Provider switch 不是替换 URL：请求、stream、terminal、usage、error 与 SDK retry 合同都会上溢 | `CONFIRMED` | `S-01`–`S-13` | 仅 OpenAI Responses 与 Anthropic Messages 当前官方合同；不推出所有 Provider |
| `04-C02` | Adapter 应封装 Provider 调用合同与保真归一化，不拥有领域 DTO、领域验证、Agent loop 或 recovery | `PARTIAL` | `04-C01`、`R-02`–`R-04` | 课程 working boundary；不是官方/行业标准 |
| `04-C03` | Streaming partial 与 terminal/final validation 分阶段；tool args fragment 不是最终 JSON/DTO；Streaming event 不是 Agent event | `CONFIRMED` | `S-01`、`S-02a`–`S-02c`、`S-06`、`S-08`–`S-10`、`R-03` | 官方合同证据；runtime event 顺序未实测 |
| `04-C04` | transport/API、refusal/truncation、parse/schema/domain failure 必须分层，HTTP status 单独不足以决定 retry | `CONFIRMED` | `S-03`、`S-06`、`S-08`–`S-13`、`R-03` | 分类稳定；具体错误码仍需 Provider scope |
| `04-C05` | 自动 transport retry 需要 eligible category、唯一 owner、request state、replay safety/idempotency、budget 与 stop gate；不能给跨 Provider 固定次数 | `PARTIAL` | `S-03`–`S-05`、`S-11`–`S-13`、`S-16` | 精确默认仅 OpenAI .NET 与 Anthropic C#；无统一 exactly-once 证据 |
| `04-C06` | transport retry、semantic/business retry 与 recovery 是三种不同动作 | `PARTIAL` | `S-03`–`S-13`、`R-03`、`R-04` | 课程术语分类；Provider 官方文档未统一采用这三个名称 |
| `04-C07` | Adapter 与 Gateway 的职责和部署边界不同；Gateway 流量治理本身不等于 Agent Runtime | `PARTIAL` | `S-14`、`S-15`、`S-15a`、`R-04` | 产品反例表明无行业唯一 Gateway 定义；按课程责任切分 |
| `04-C08` | capability descriptor/negotiation 必须显式暴露不可归一化差异，并以 native/fallback/unsupported fail closed | `PROPOSAL` | `04-C01`、`S-07`、`S-08`、`R-02`–`R-04` | 设计提案；未实现、未执行、无统一 schema 标准主张 |

## Source Manifest

所有网络来源均为 primary/official source，并于 `2026-08-20（Asia/Shanghai）` 实时打开核对。

| ID | Source | Retrieved / version scope | Access | Used by |
|---|---|---|---|---|
| `S-01` | [OpenAI Streaming API responses](https://developers.openai.com/api/docs/guides/streaming-responses) | OpenAI Responses API current docs；2026-08-20 | `OPENED_CURRENT` | C01, C03 |
| `S-02a` | [OpenAI Python function arguments delta type](https://github.com/openai/openai-python/blob/main/src/openai/types/responses/response_function_call_arguments_delta_event.py) | official generated type, current main；2026-08-20 | `OPENED_CURRENT` | C01, C03 |
| `S-02b` | [OpenAI Python response completed type](https://github.com/openai/openai-python/blob/main/src/openai/types/responses/response_completed_event.py) | official generated type, current main；2026-08-20 | `OPENED_CURRENT` | C03 |
| `S-02c` | [OpenAI Python Response type](https://github.com/openai/openai-python/blob/main/src/openai/types/responses/response.py) | official generated type, current main；2026-08-20 | `OPENED_CURRENT` | C01, C03 |
| `S-03` | [OpenAI Error codes](https://developers.openai.com/api/docs/guides/error-codes) | OpenAI API current docs；2026-08-20 | `OPENED_CURRENT` | C01, C04, C05, C06 |
| `S-04` | [OpenAI Rate limits](https://developers.openai.com/api/docs/guides/rate-limits) | OpenAI API current docs；2026-08-20 | `OPENED_CURRENT` | C01, C04, C05, C06 |
| `S-05` | [OpenAI official .NET SDK](https://github.com/openai/openai-dotnet) | official repository current main；2026-08-20 | `OPENED_CURRENT` | C01, C05, C06 |
| `S-06` | [OpenAI Structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs) | OpenAI Responses current guide；2026-08-20 | `OPENED_CURRENT` | C03, C04 |
| `S-07` | [OpenAI API overview](https://developers.openai.com/api/reference/overview) | OpenAI API current docs；2026-08-20 | `OPENED_CURRENT` | C01, C08 |
| `S-08` | [Anthropic Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming) | Anthropic Messages current docs；2026-08-20 | `OPENED_CURRENT` | C01, C03, C04, C06, C08 |
| `S-09` | [Anthropic Messages create](https://platform.claude.com/docs/en/api/messages/create) | Anthropic Messages current docs；2026-08-20 | `OPENED_CURRENT` | C01, C03, C04, C06 |
| `S-10` | [Anthropic Handling stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons) | Anthropic Messages current guide；2026-08-20 | `OPENED_CURRENT` | C01, C03, C04, C06 |
| `S-11` | [Anthropic Errors](https://platform.claude.com/docs/en/api/errors) | Anthropic API current docs；2026-08-20 | `OPENED_CURRENT` | C01, C04, C05, C06 |
| `S-12` | [Anthropic Rate limits](https://platform.claude.com/docs/en/api/rate-limits) | Anthropic direct API current docs；2026-08-20 | `OPENED_CURRENT` | C01, C04, C05, C06 |
| `S-13` | [Anthropic C# SDK](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp) | current docs；SDK 标记 beta，v10+ 为 official package；2026-08-20 | `OPENED_CURRENT` | C01, C04, C05, C06 |
| `S-14` | [Cloudflare AI Gateway](https://developers.cloudflare.com/ai-gateway/) | Cloudflare AI Gateway current docs；2026-08-20 | `OPENED_CURRENT` | C07 |
| `S-15` | [Azure API Management AI Gateway tier (preview)](https://learn.microsoft.com/en-us/azure/api-management/ai-gateway-overview) | Azure API Management AI Gateway tier（public preview）；2026-08-20 | `OPENED_CURRENT` | C07 |
| `S-15a` | [AI gateway in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/genai-gateway-capabilities) | Azure API Management all tiers；capability availability varies by service tier；unified model API 与 Microsoft Foundry integration 分别标 preview；2026-08-20 | `OPENED_CURRENT` | C07 |
| `S-16` | [RFC 9110 §9.2.2](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2) | HTTP semantics standard；2026-08-20 retrieval | `OPENED_CURRENT` | C05 |
| `R-01` | `content/ai-empowerment/agent-engineering-01-model-api-messages-token.md` | published local Article 01 | `READ_LOCAL` | C01 |
| `R-02` | `content/ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md` | published local Article 02 | `READ_LOCAL` | C02, C08 |
| `R-03` | `content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md` | published local Article 03 | `READ_LOCAL` | C02, C03, C04, C05, C06, C08 |
| `R-04` | `docs/agent-engineering-course/glossary.md` | course glossary current workspace | `READ_LOCAL` | C02, C06, C07, C08 |
| `R-05` | `docs/agent-engineering-series-plan.md` + `docs/agent-engineering-course-plan-v3.1-review.md` | Article 04 canonical/full-plan scope | `READ_LOCAL` | all course usage |

## Evidence Cards

### `04-C01` — Provider switch is a contract migration

- **Status**：`CONFIRMED`
- **Source**：`S-01`–`S-13`；cross-article boundary `R-01`、`R-02`
- **Retrieved / Version Scope**：`2026-08-20（Asia/Shanghai）`；OpenAI Responses current docs + official Python generated types + official .NET current main；Anthropic Messages current docs + official C# docs（SDK 标记 beta，v10+ 为 official package）。
- **Observation**：两家官方合同在请求结构、SSE event vocabulary、tool arguments fragments、terminal/stop semantics、usage dimensions、error taxonomy 与 SDK retry defaults 上存在可点名差异。OpenAI .NET 当前写明最多额外 3 次，Anthropic C# 当前写明默认 2 次，eligible status 也不完全相同。
- **Counter-evidence**：两家都提供 streaming、tool input、usage、errors 与 SDK retry，说明可以抽象共同能力；并非每处差异都必须暴露给领域业务。
- **Interpretation**：共同点支持 Adapter 接口，差异要求保留 Provider scope/capability/raw metadata；切换需合同迁移而非只换 URL。
- **Proves**：至少 OpenAI Responses 与 Anthropic Messages 的当前合同差异会上溢到正确性、预算与 retry 策略。
- **Does Not Prove**：所有 Provider 都有同样差异；任何 Provider 永远不可切换；本文已执行切换或 Provider call。
- **Limitations**：模型、SDK 语言、区域、云托管与未来版本未穷举；结论为 `DOC_CONFIRMED`。
- **Course Usage**：作为开篇问题空间和 capability negotiation 的事实基础；禁止将任一 Provider 字段写成统一 contract。

### `04-C02` — Adapter owns provider translation, not domain truth

- **Status**：`PARTIAL`
- **Source**：事实前提 `04-C01`；课程链 `R-02`、`R-03`、`R-04`。
- **Retrieved / Version Scope**：Provider 差异前提检索于 `2026-08-20`；课程 workspace current。
- **Observation**：Provider-specific request/event/error/usage 需要集中翻译；Article 03 已把 Provider envelope、Parse、Schema、DTO、Domain 分层，glossary 也把 Adapter、Gateway、Runtime 分开。
- **Counter-evidence**：部分 SDK/框架会生成类型化对象，部分 Gateway 也会做 request/response transform；现实实现可把多层部署在同一进程或产品内。
- **Interpretation**：课程采用责任边界而非进程边界：Adapter 交付保真的模型调用证据，领域 DTO/验证与 Agent loop 保持在上层。
- **Proves**：这种切分与已发布课程证据链相容，并能防止 Provider completion 被误写为 business completion。
- **Does Not Prove**：存在行业唯一 Adapter 定义；所有类型化 SDK 都设计错误；Adapter 必须是独立服务。
- **Limitations**：属于课程 engineering judgment，故不升格为 `CONFIRMED`。
- **Course Usage**：Outline 应明确“应/不应封装”清单，并延续 Article 03 的 DTO/Domain gate。

### `04-C03` — Partial stream events are not final validated results

- **Status**：`CONFIRMED`
- **Source**：`S-01`、`S-02a`–`S-02c`、`S-06`、`S-08`–`S-10`、`R-03`、`R-04`。
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI Responses 与 Anthropic Messages current docs/types。
- **Observation**：OpenAI text/tool delta 与 completed Response 是不同事件，function arguments delta 是部分字符串；Anthropic content block delta 与 message stop 分离，`partial_json` 需要累积后解析。两家还分别暴露 incomplete/refusal/stop reason/stream error。
- **Counter-evidence**：官方 SDK 可提供“累积为最终 Message/Response”的 helper，简单文本 UI 也可以边到边显示；这降低应用处理负担，但没有把早期 fragment 变成已验证 DTO。
- **Interpretation**：必须区分 `STREAM_PARTIAL`、`PROVIDER_TERMINAL`、`FINAL_VALIDATED_RESULT`；tool args 只在 block/item 完成后进入 parse，terminal 合法后再进入 Schema/DTO/Domain。
- **Proves**：partial != final；provider completion != domain success；stream event 是调用内部事件而非 Agent runtime step/state 事件。
- **Does Not Prove**：事件一定按某次实际调用完全照文档顺序出现；已在 runtime 观测；终态后下游副作用已成功。
- **Limitations**：Provider Calls `NONE`；SSE 实际重连、乱序/断流未实验。
- **Course Usage**：作为 streaming 主图与 Article 03/05/10 的边界；禁止从 delta 直接构造并执行工具参数。

### `04-C04` — Failure classification precedes retry policy

- **Status**：`CONFIRMED`
- **Source**：`S-03`、`S-06`、`S-08`–`S-13`、`R-03`。
- **Retrieved / Version Scope**：`2026-08-20`；两家 Provider 当前官方错误、限流、streaming、stop/structured-output 文档。
- **Observation**：OpenAI 429 可表达临时 rate limit 或 quota/billing/action 类问题；Anthropic 可在 HTTP 200 后通过 SSE 发出 error；refusal/max-token/incomplete 属于生成终态；parse/schema/domain 是本地验证层失败。
- **Counter-evidence**：某些 SDK 会把多个状态映射到统一异常基类，方便普通调用者；但异常基类不足以证明重试语义一致。
- **Interpretation**：至少区分 network、timeout、rate limit、quota/action、transient 5xx、stream-after-200、refusal、truncation/incomplete、parse、schema、domain。
- **Proves**：单看 HTTP status、`Exception` 或“没有最终文本”不能决定 retry；错误来源和终态语义必须保留。
- **Does Not Prove**：每个错误类别都有唯一动作；所有 4xx 永不 retry；所有 5xx 必须 retry。
- **Limitations**：具体 code/type 随 Provider 和版本变化；需要 raw diagnostics/unknown 分支。
- **Course Usage**：错误分类表；为 `04-C05` 的 retry gate 提供输入。

### `04-C05` — Automatic transport retry is gated and provider-scoped

- **Status**：`PARTIAL`
- **Source**：`S-03`–`S-05`、`S-11`–`S-13`、`S-16`。
- **Retrieved / Version Scope**：`2026-08-20`；OpenAI API current + official .NET current main；Anthropic API current + official C# current docs；RFC 9110。
- **Observation**：两家官方 SDK 都会对部分连接/HTTP 错误进行 bounded automatic retry，但 eligible sets/default counts 不同。两家限流指导要求遵守 Retry-After/退避并设限。RFC 对非幂等请求自动重试要求已知重放安全或确认原请求未应用。
- **Counter-evidence**：SDK 的默认 retry 表明“所有模型 create 都禁止自动 retry”不成立；Provider 可以根据客户端掌握的信息安全处理部分失败。
- **Interpretation**：上层 Adapter/Gateway 若再 retry，必须核对分类、唯一 owner、request state、replay safety/idempotency、Provider guidance、attempt/time/token/cost budget 与 stop condition。
- **Proves**：不能给跨 Provider 一刀切次数，也不能忽略 SDK 已有 retry；timeout/网络错误不是自动等同“服务端未处理”。
- **Does Not Prove**：OpenAI/Anthropic create API 提供 exactly-once；所有 eligible 状态在任何请求上下文都安全；OpenAI 其他语言 SDK 也固定 3 次；Anthropic 其他语言 SDK 也固定 2 次。
- **Limitations**：没有运行 SDK 或抓取 attempt；没有闭合跨两家 API 的幂等键保证，因此保持 `PARTIAL`。
- **Course Usage**：Retry checklist；精确数字必须伴随 Provider + SDK language + retrieval date，不得转写成课程默认策略。

### `04-C06` — Transport retry, semantic retry, and recovery are distinct

- **Status**：`PARTIAL`
- **Source**：Provider事实 `S-03`–`S-13`；课程链 `R-03`、`R-04`。
- **Retrieved / Version Scope**：Provider docs `2026-08-20`；course workspace current。
- **Observation**：官方文档把 transport/API errors 与 refusal/stop/incomplete 分开；Article 03 把 parse/schema/domain failure 与 upstream retry eligibility 分开；glossary 把 Retry 与 Recovery 分开。
- **Counter-evidence**：现实 SDK/框架可能把 retry、fallback、repair 统称 recovery，名称并不统一；某个 orchestrator 也可能集中实现三者。
- **Interpretation**：课程按“重放同一请求”“修改语义后新 attempt”“从持久化 workflow state 恢复”区分三种动作，部署在同一组件也不合并证据语义。
- **Proves**：该分类能防止网络重试掩盖 refusal/schema/domain failure，并保持 Article 11 recovery 边界。
- **Does Not Prove**：Provider 官方采用课程三分法；semantic retry 一定成功；recovery 可以只靠模型重试完成。
- **Limitations**：术语分类是课程 working model，故为 `PARTIAL`。
- **Course Usage**：单列三层 retry/recovery 表；本篇不展开 Article 11 的 checkpoint/compensation 实现。

### `04-C07` — Adapter and Gateway differ; traffic governance is not runtime closure

- **Status**：`PARTIAL`
- **Source**：`S-14`、`S-15`、`S-15a`、`R-04`。
- **Retrieved / Version Scope**：Cloudflare AI Gateway current docs；Azure API Management AI Gateway tier（public preview）与 Azure API Management all-tier capabilities current docs；`2026-08-20`；course glossary current。Azure all-tier 页面明确 capability availability varies by service tier，unified model API 与 Microsoft Foundry integration 等子能力分别标为 preview。
- **Observation**：
  - Cloudflare `S-14` 直接列出多 Provider / model 接入、analytics / logging、rate limiting、request retry 与 model fallback。
  - Azure preview tier `S-15` 直接列出集中 endpoint、按 model / tool 的 backend routing、runtime / backend credentials、policies、request / token limits、OpenTelemetry telemetry，以及 model / MCP tool 管理；该 tier 为 public preview，features、APIs 与 limits 可变。
  - Azure all-tier `S-15a` 直接列出 authentication、backend load balancing、token limits / quotas、observability 与 model / agent / tool governance；它不保证每个 service tier 都具有全部能力，且其中部分子能力另标 preview。
- **Counter-evidence**：三份页面的能力集合和稳定性边界不同；`S-15` 没有直接列出 request retry / model fallback，`S-15a` 也不能证明每个 service tier 都拥有其能力全集。因此产品名称不能给出干净行业边界，也不能把跨产品能力并集反推给任一产品。
- **Interpretation**：上表仅定义课程责任：Adapter 做 Provider contract translation；Gateway 做跨调用/租户流量治理；Runtime 对 Agent 执行状态负责。产品可组合职责，但证据仍需分开。
- **Proves**：现实 Gateway 存在上述逐产品、逐 scope 的 traffic-plane 能力实例；这些能力本身不证明 Agent loop/recovery，也不构成任一产品的完整能力保证。
- **Does Not Prove**：存在行业唯一 Gateway 定义；Gateway 产品绝不提供 Agent 功能；Adapter/Gateway 必须分别部署。
- **Limitations**：仅取两项官方产品实例；结论收窄为课程 working boundary。
- **Course Usage**：澄清 Adapter ≠ Gateway、Gateway ≠ Agent Runtime；不展开网关实现、LB 或模型部署。

### `04-C08` — Capability descriptor must preserve non-normalizable differences

- **Status**：`PROPOSAL`
- **Source**：差异前提 `04-C01`；合同演进 `S-07`、`S-08`；课程边界 `R-02`–`R-04`。
- **Retrieved / Version Scope**：Provider docs `2026-08-20`；course workspace current。
- **Observation**：两家在 message/instruction、stream events、tool fragments、terminal states、usage、retry 与 unknown-event evolution 上不同；静态“支持 LLM”布尔值不足以表达任务要求。
- **Counter-evidence**：已有 SDK/Gateway 可能提供自身 capability discovery；简单单 Provider 应用也可通过配置固定能力，不一定需要动态 negotiation。
- **Interpretation**：课程建议 descriptor 至少携带 provider/api/model/version scope，以及 structured output、text/tool streaming、usage/terminal semantics、retry ownership、limits/modalities 与 extension policy；结果为 `native / explicit fallback / unsupported`。
- **Proves**：无；这是从已确认差异导出的设计提案。
- **Does Not Prove**：存在官方统一 descriptor schema；已实现路由协商；fallback 与 native guarantee 等价。
- **Limitations**：`PROPOSAL / NOT_EXECUTED`；未写代码、未跑 Fake Provider、未做 interoperability test。
- **Course Usage**：作为本篇结尾的设计输出和后续 Outline 验证问题；禁止写成已交付能力。

## Evidence Gate Recommendation

### Recommendation：`PASS`

| Gate item | Result | Evidence |
|---|---|---|
| 两家 Provider 当前 primary/official sources | `PASS` | OpenAI `S-01`–`S-07`；Anthropic `S-08`–`S-13` |
| Streaming schema / terminal / usage / error | `PASS` | `04-C01`、`04-C03`、`04-C04` |
| Retry guidance 与 SDK default scope | `PASS_WITH_LIMITATION` | `04-C05`；仅 OpenAI .NET 与 Anthropic C# 精确默认 |
| Adapter/Gateway/Runtime 边界 | `PASS_WITH_LIMITATION` | `04-C02`、`04-C07`；课程 working boundary |
| Capability negotiation | `PASS_AS_PROPOSAL` | `04-C08 / NOT_EXECUTED` |
| Core behavioral claims blocked | `PASS` | `0 BLOCKED` |
| Required Lab / Provider call | `NOT_REQUIRED / NONE` | transaction scope |

### Residual blocker

`NONE` for Outline entry. Runtime behavior remains `UNVERIFIED` by design and must not be upgraded in later stages without a separately authorized Provider call or executable lab.

### Next action

Outliner should bind every behavioral section to the Claim IDs above, retain all `PARTIAL/PROPOSAL` labels, and keep exact retry defaults Provider/SDK/date-scoped. No narrative sentence may claim runtime observation, universal Gateway semantics, cross-Provider idempotency, or a final structured result before terminal + Parse/Schema/DTO/Domain gates.
