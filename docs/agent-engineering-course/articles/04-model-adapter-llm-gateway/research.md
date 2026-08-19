# Article 04 Research

- Research Phase：`RESEARCH`
- Research Status：`COMPLETE`
- Lifecycle Candidate：`OUTLINE_READY`
- Evidence Status：`PASS_CANDIDATE`
- Evidence Gate Recommendation：`PASS`
- Required Lab：`NONE`
- Research Window：`2026-08-20（Asia/Shanghai）`
- Provider Calls：`NONE`
- Runtime Evidence：`UNVERIFIED`
- Core Claim Summary：`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`

> 研究结论的证据上限是“当前官方公开合同已核对”，不是“已调用 Provider 并观测到运行时行为”。本篇没有 Required Lab，也没有执行 Fake Provider；如后续需要 Fake Provider，只能另立 `PROPOSAL / NOT_EXECUTED` 验证项。

## Research Questions

| RQ | Question | Status | Primary Claims |
|---|---|---|---|
| `RQ-01` | 哪些 Provider 差异会从 SDK 上溢到应用，哪些可以由 Adapter 安全归一化？ | `ANSWERED` | `04-C01`、`04-C02` |
| `RQ-02` | Adapter 应怎样处理请求、响应、错误、Usage，同时不吞掉领域 DTO 与能力差异？ | `ANSWERED` | `04-C02`、`04-C08` |
| `RQ-03` | 增量文本、Tool 参数片段、完成事件、Usage 与最终 Structured Result 怎样共存？ | `ANSWERED` | `04-C03` |
| `RQ-04` | 限流、网络、超时、拒绝、截断与 Schema / Domain 失败应怎样分类？ | `ANSWERED` | `04-C04` |
| `RQ-05` | 传输重试与业务重试怎样分层，自动 Retry 需要哪些安全前提和停止条件？ | `ANSWERED` | `04-C05`、`04-C06` |
| `RQ-06` | Model Adapter 与 LLM Gateway 的职责、部署与状态边界是什么？ | `ANSWERED` | `04-C07` |
| `RQ-07` | 为什么 Provider 切换不是替换 URL，Capability Negotiation 应保留哪些不可归一化信息？ | `ANSWERED` | `04-C01`、`04-C08` |

## Research Answers

### 1. 问题空间：Provider 差异会沿合同上溢，切换不是替换 URL

同样是“一次模型调用”，OpenAI Responses 与 Anthropic Messages 当前公开合同至少在以下位置不同。表中字段与默认行为只适用于所列 Provider/API/SDK，检索日期均为 `2026-08-20（Asia/Shanghai）`。

| Contract Surface | OpenAI scope | Anthropic scope | Why it leaks upward |
|---|---|---|---|
| 请求结构 | Responses API 使用 Responses 输入/指令与输出项模型 | Messages API 使用顶层 `system` 与 `messages`，消息角色合同不同 | Prompt 组装、历史消息、缓存与模型选择不能靠换 URL 保持语义 |
| 文本流事件 | `response.created` → `response.output_text.delta` → `response.completed`，并可能有 `error` | `message_start` → content block events → `message_delta` → `message_stop`；成功 HTTP 后仍可能出现 SSE `error` | 消费端必须按 Provider 状态机解码，不能把任意 chunk 当最终答案 |
| Tool 参数流 | `response.function_call_arguments.delta` 的 `delta` 是部分字符串 | `input_json_delta.partial_json` 是部分 JSON 字符串，应累积后再解析 | 片段本身不是可验证的 DTO，更不是可执行工具参数 |
| 终止语义 | 完整 `Response` 有 `status`，可出现 `incomplete`/`failed` 等；refusal 是输出内容类型 | `stop_reason` 可为 `end_turn`、`max_tokens`、`tool_use`、`refusal` 等，流开始时可为 `null` | “流结束”“生成完成”“业务成功”不是同一件事 |
| Usage | 完整 Response 携带 OpenAI Responses usage 合同 | Messages usage 包含 input/output 与 cache 相关维度；流中 usage 出现在消息事件上 | 统一总数会丢失 Provider 计费/限额语义，预算层需保留来源与维度 |
| 错误与限流 | 429 既可能是速率限制，也可能是余额/配额/组织限额；5xx/503 有官方重试指导 | 429、500、504、529 等有各自语义；流在 HTTP 200 后仍可能报错 | 仅按 HTTP 状态或异常基类无法决定 retry、fallback 或升级 |
| 当前官方 SDK 默认重试 | OpenAI .NET 当前 README：408、429、500、502、503、504 最多自动额外重试 3 次 | Anthropic C# 当前文档：连接错误、408、409、429、5xx 默认重试 2 次 | 上层再包一层 retry 会形成次数乘法；默认值不能跨语言、版本或 Provider 外推 |

因此，Provider switch 是合同迁移：除 endpoint/credential 外，还要核对请求形状、事件状态机、终止原因、Usage 口径、错误类型、SDK 内建 retry、能力与限制。`04-C01` 只证明这些差异存在并会影响调用方，不证明所有 Provider 永远无法归一化。

### 2. 抽象模型：Adapter 归一化模型调用合同，领域 DTO 留在上层

本篇采用以下分层模型：

```text
Domain Request / Task Policy
        ↓
Provider-neutral Model Request
        ↓
Model Adapter
        ↓
Provider SDK / HTTP / SSE
        ↓
Normalized partial events + provider terminal state + usage + raw diagnostics
        ↓
Parse → Schema → DTO → Domain Validation
        ↓
Final Structured Result
```

这是课程工作模型，不是某家 Provider 的官方架构，也不是行业统一接口标准。

Adapter 应封装：

- Provider 请求映射与 SDK/HTTP 调用边界；
- Provider streaming 状态机解码，并发出带 Provider scope 的规范化事件；
- Provider 错误到稳定类别的映射，同时保留原始错误类型、状态、request id 与 headers；
- Provider 终止状态、refusal、incomplete/truncation、Usage 的保真映射；
- 当前模型/API 的 capability descriptor，显式返回 `native / fallback / unsupported`；
- SDK/Gateway 是否已经拥有 transport retry，以及本次实际 attempt 元数据。

Adapter 不应封装：

- 领域 DTO、领域不变量和业务成功条件；
- 把 Provider 的“生成完成”改写成“业务完成”；
- Agent loop、tool dispatch、workflow checkpoint 或跨步骤 recovery；
- 未经上层策略批准的 prompt 改写、模型切换、降级或无限 retry；
- 用某一家字段假装成跨 Provider 的统一事实，或静默丢弃未知终止原因、Usage 维度与原始诊断信息。

把领域 DTO 留在 Adapter 上层，延续 Article 03 的证据链：Provider envelope 先通过 Parse / Schema / DTO / Domain gate，才能形成最终结构化结果。Adapter 可以交付“可验证候选及其 provenance”，但不拥有领域真值。

### 3. 机制：partial、terminal、validation 是三个阶段

| Phase | Typical input | Allowed output | Explicitly not proven |
|---|---|---|---|
| `STREAM_PARTIAL` | 文本 delta、tool arguments delta、usage update、provider progress event | UI 增量展示、缓冲、可观测记录 | JSON 完整、Schema 有效、DTO 可构造、工具可执行、任务完成 |
| `PROVIDER_TERMINAL` | OpenAI 完整 Response/terminal event；Anthropic message stop/stop reason；或 provider stream error | 终止状态、原始 stop/incomplete/refusal/error、最终 usage、聚合候选 | 领域约束满足、Agent 已停止、外部副作用成功 |
| `FINAL_VALIDATED_RESULT` | 聚合候选 + terminal 状态已允许继续 | Parse/Schema/DTO/Domain 全部通过的最终结果 | 下游副作用已提交；这仍需 Article 11 的 workflow/recovery 证据 |

OpenAI 当前官方生成类型将 `response.function_call_arguments.delta.delta` 定义为部分字符串；Anthropic 当前 streaming 文档将 `input_json_delta.partial_json` 定义为部分 JSON 字符串，并要求累积到 content block 结束后再解析。两家来源共同支持一个保守边界：

1. 片段只进入 buffer，不进入 DTO；
2. Provider 相应 block/item 结束后才能尝试 JSON parse；
3. Provider terminal 状态仍需判定 refusal/incomplete/error；
4. 随后才进入 Article 03 的 Schema / DTO / Domain gate；
5. 只有最终验证通过，才可能交给 Article 05 的 Tool 系统决定是否执行。

`Streaming event != Agent event`：模型流事件描述一次 Provider 调用内部的传输/生成进度；Agent event 描述运行时 step、tool dispatch、state transition、stop 等更高层状态。将前者直接升级为后者会跳过验证与运行时状态机。

### 4. 错误分类：先判断失败在哪一层，再决定动作

| Category | Examples | Detection boundary | Retry meaning | Owner |
|---|---|---|---|---|
| `TRANSPORT_NETWORK` | 连接失败、连接重置、无法确认是否收到请求 | SDK/HTTP client | 可能重放同一请求，但必须先过 replay-safety 与预算 gate | SDK/Adapter/Gateway 中恰好一层 |
| `TRANSPORT_TIMEOUT` | connect/read/overall timeout；结果状态可能未知 | SDK/HTTP client + request id | 不是天然安全；未知提交状态不能等同“未执行” | transport owner + upper policy |
| `PROVIDER_RATE_LIMIT` | 429 且官方错误类别确认为 rate limit | Provider error body + headers | 按 `Retry-After` 或 bounded backoff；失败请求也可能消耗限额 | transport policy |
| `PROVIDER_QUOTA_OR_ACTION` | OpenAI 429 余额/配额/组织限额等 | Provider error code/message | 通常需补额度、调整限额或人工处理，不应按暂时限流盲重试 | operator/business policy |
| `PROVIDER_TRANSIENT_5XX` | OpenAI 500/503；Anthropic 500/504/529 等 | status + provider error type | 仅在官方列为 eligible、重放安全、预算允许时 bounded retry | transport policy |
| `STREAM_ERROR_AFTER_200` | Anthropic SSE `error` | stream event decoder | HTTP 成功不代表流成功；是否 retry 取决于已消费状态与 replay safety | Adapter + upper policy |
| `MODEL_REFUSAL` | Provider 以成功 envelope 返回 refusal | Provider output/stop reason | 不是 transport retry；若允许只能是显式 semantic/business attempt | task policy |
| `MODEL_TRUNCATION_OR_INCOMPLETE` | max tokens、context limit、incomplete status | terminal state/reason | 可能改变预算、输入、模型或策略后新开业务 attempt | task policy |
| `PARSE_FAILURE` | 聚合文本不是合法 JSON | local parser | 不重放同一 transport；可按显式 repair policy 新开 attempt | structured-output layer |
| `SCHEMA_FAILURE` | JSON 合法但不符合 Schema | schema validator | 属于 semantic/business retry 候选，不是网络重试 | structured-output layer |
| `DOMAIN_FAILURE` | DTO 可构造但违反业务不变量 | domain validator | 由业务规则决定修复、拒绝或升级 | domain layer |

关键反例是：同一个 HTTP 429 并不自动等于“等待后重试”；OpenAI 当前错误文档同时列出临时 rate limit 与余额/配额/组织限制。另一个反例是 Anthropic SSE 可在 HTTP 200 后发出 error，因此“已收到成功 status”也不是最终成功证据。

### 5. Retry 分层：transport retry、semantic retry、recovery 不可互换

| Mechanism | What repeats or changes | Appropriate trigger | Must not silently do |
|---|---|---|---|
| `TRANSPORT RETRY` | 重放同一逻辑 Provider 请求 | 官方列出的暂时连接/限流/5xx 等类别 | 改 prompt、改 Schema、换模型、掩盖 refusal/domain failure |
| `SEMANTIC / BUSINESS RETRY` | 创建新的业务 attempt，可能改变 prompt、输入、预算、Schema 或模型 | refusal、truncation、parse/schema/domain failure，且业务策略明确允许 | 冒充底层自动重试；复用旧 attempt 的“成功”标签 |
| `RECOVERY` | 从持久化状态恢复 workflow，核对已完成 step/副作用并继续或补偿 | 崩溃、进程重启、长任务中断、未知副作用状态 | 仅靠重新请求模型来恢复整个 Agent |

自动 transport retry 只有在以下 gate 全部满足时才可进入候选：

1. **分类明确**：错误属于该 Provider/SDK 当前官方列出的 retry-eligible 范围，而不是 refusal、quota、schema 或 domain failure；
2. **重试所有者唯一**：知道 SDK、Gateway、Adapter 哪一层已经在 retry，避免 `SDK × Gateway × App` 次数相乘；
3. **请求状态明确**：没有把已经收到 terminal 结果、已接受的 tool call 或未知副作用状态误当成“请求未发生”；
4. **重放安全**：操作语义已知可安全重放，或可判断原请求未被应用；RFC 9110 对非幂等请求自动重试给出同样的谨慎边界；
5. **Provider 指导优先**：优先遵守 `Retry-After`，否则才使用带 jitter 的 exponential backoff；
6. **预算有界**：限制额外 attempt 数、总 elapsed time、token/cost 与队列等待；失败请求也可能计入 rate limit；
7. **停止条件可审计**：达到上限、遇到不可重试类别、deadline/budget 耗尽或状态不确定时停止并升级。

这些条件没有推出一套跨 Provider 固定次数。当前精确默认值只能分别写为：OpenAI 官方 .NET SDK 当前 main 文档“最多额外 3 次”；Anthropic 官方 C# SDK 当前文档“默认 2 次”。未找到覆盖两家 create API 的统一 exactly-once 或幂等键保证，因此 `04-C05` 保持 `PARTIAL`。

### 6. 工程边界：Adapter、Gateway、Agent Runtime

| Component | Course working responsibility | Typical deployment/state | Not equivalent to |
|---|---|---|---|
| `Model Adapter` | 把应用侧模型请求映射到特定 Provider；解码 stream/terminal/error/usage；暴露 capability 与 raw diagnostics | 应用进程内库或服务内模块；一次调用的短期状态 | Domain validation、Gateway、Agent Runtime |
| `LLM Gateway` | 跨应用/Provider 的集中入口与流量策略，例如 credential 隔离、routing、rate/quota、retry/fallback policy、audit/telemetry | 独立网络服务/平台控制面，拥有租户与流量策略状态 | 某个 Provider Adapter；完整 Agent Runtime |
| `Agent Runtime` | 管理 goal/plan/step、tool dispatch、loop、state、stop、checkpoint/recovery | 长于单次模型调用的执行状态 | 模型代理或流量网关 |

这些产品证据必须逐项归属：Cloudflare AI Gateway 当前 overview 直接列出多 Provider / model 接入、analytics / logging、rate limiting、request retry 与 model fallback；Azure API Management AI Gateway tier (preview) 的 public-preview overview 直接列出集中 endpoint、按 model / tool 的 backend routing、runtime / backend credentials、policies、request / token limits、OpenTelemetry telemetry，以及 model / MCP tool 管理。Azure API Management 的 all-tier capabilities 页面另行说明 authentication、backend load balancing、token limits / quotas、observability 与 model / agent / tool governance，但能力可用性依 service tier 而异，unified model API 与 Microsoft Foundry integration 等子能力又分别标为 preview。三份页面证明的是各自不同的能力组合，不是两家各自拥有这些能力的并集。故本篇只能把上表作为课程责任切分，不能宣称存在一个行业唯一的 Gateway 定义，也不能宣称任何名为 Gateway 的产品都不含 Agent 功能。

`Gateway != Agent Runtime` 的准确含义是：集中流量治理本身不提供 Article 10/11 所要求的 step/loop/tool/state/stop/recovery 证明。某产品即使同时提供两类能力，也应分别验证其 gateway plane 与 runtime plane，而不是因产品名推导职责闭合。

### 7. Capability negotiation：不可归一化的差异必须可见

建议将 capability descriptor 作为 `PROPOSAL`，不声称已有统一标准或已实现。最小维度包括：

| Dimension | Example values | Why Adapter must expose it |
|---|---|---|
| `provider / api / model / version-scope` | OpenAI Responses、Anthropic Messages、具体 model/date | 字段与能力随 API、模型和版本变化 |
| `instruction_and_message_model` | 顶层 instructions/system、允许角色、content blocks | 避免把一家消息合同硬塞给另一家 |
| `structured_output` | native strict、JSON-only、prompt fallback、unsupported | fallback 的可靠性不等同 native guarantee |
| `stream_text` | native/unsupported；event vocabulary | UI/consumer 是否可增量消费 |
| `stream_tool_arguments` | raw fragments、buffered final、unsupported | 决定何时可 parse/validate，不能把 fragment 当 DTO |
| `stream_usage` | incremental/cumulative/final-only + dimensions | 预算层需正确累计且避免重复计数 |
| `terminal_semantics` | stop reasons、refusal、incomplete/truncation | 不能把未知 reason 归一成 success |
| `retry_contract` | owner、eligible classes、default attempts、Retry-After support | 防止嵌套 retry 与版本外推 |
| `limits_and_modalities` | context/output limits、input/output modalities | routing 必须根据真实要求 fail closed |
| `extension_policy` | unknown event/reason preservation | 两家官方合同都允许演进，未知类型不能被静默吞掉 |

Negotiation 的输出应是 `native / explicit fallback / unsupported`，并携带 Provider scope；若任务要求 strict structured output 或 tool-argument streaming，而候选 Provider 只支持较弱 fallback，路由层应显式拒绝或请求上层选择，而不是假装兼容。

## Source Manifest

所有网页均于 `2026-08-20（Asia/Shanghai）` 实时打开核对；版本敏感结论只适用于表中 Scope。

| ID | Source | Provider / Version scope | Observation used |
|---|---|---|---|
| `S-01` | [OpenAI Streaming API responses](https://developers.openai.com/api/docs/guides/streaming-responses) | OpenAI Responses API current docs | SSE typed events；text delta 与 completed/error 分离 |
| `S-02a` | [OpenAI Python function call arguments delta type](https://github.com/openai/openai-python/blob/main/src/openai/types/responses/response_function_call_arguments_delta_event.py) | official `openai-python` current main, generated from OpenAPI | function arguments `delta` 是部分字符串 |
| `S-02b` | [OpenAI Python response completed type](https://github.com/openai/openai-python/blob/main/src/openai/types/responses/response_completed_event.py) | official `openai-python` current main | completed event 携带完整 Response |
| `S-02c` | [OpenAI Python Response type](https://github.com/openai/openai-python/blob/main/src/openai/types/responses/response.py) | official `openai-python` current main | status、incomplete details、output、usage 合同 |
| `S-03` | [OpenAI Error codes](https://developers.openai.com/api/docs/guides/error-codes) | OpenAI API current docs | 429 子类、5xx/503 与 retry guidance |
| `S-04` | [OpenAI Rate limits](https://developers.openai.com/api/docs/guides/rate-limits) | OpenAI API current docs | Retry-After/backoff/jitter/bounds；失败请求也消耗限额；SDK 内建 retry |
| `S-05` | [OpenAI official .NET SDK README](https://github.com/openai/openai-dotnet) | official `openai-dotnet` current main | 408/429/500/502/503/504 最多额外重试 3 次 |
| `S-06` | [OpenAI Structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs) | OpenAI Responses current guide | incomplete reason 与 refusal 必须先于结构化成功判定 |
| `S-07` | [OpenAI API overview](https://developers.openai.com/api/reference/overview) | OpenAI API current docs | request/rate-limit headers；新增 streaming event type 是兼容演进的一部分 |
| `S-08` | [Anthropic Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming) | Anthropic Messages API current docs | event flow、partial JSON、usage、HTTP 200 后 SSE error、未知 event |
| `S-09` | [Anthropic Messages create](https://platform.claude.com/docs/en/api/messages/create) | Anthropic Messages API current docs | system/messages、stop reason、usage fields |
| `S-10` | [Anthropic Handling stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons) | Anthropic Messages current guide | refusal、max_tokens、tool_use 等终态语义 |
| `S-11` | [Anthropic Errors](https://platform.claude.com/docs/en/api/errors) | Anthropic API current docs | 429/500/504/529；SDK transient retry；stream error 边界 |
| `S-12` | [Anthropic Rate limits](https://platform.claude.com/docs/en/api/rate-limits) | Anthropic direct API current docs | Retry-After、RPM/ITPM/OTPM 与 cache 维度；云平台 scope 不同 |
| `S-13` | [Anthropic C# SDK](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp) | Anthropic official C# SDK current docs；文档标记 SDK 为 beta，v10+ 为 official package | 默认 2 次；连接、408/409/429/5xx；SSE exception |
| `S-14` | [Cloudflare AI Gateway](https://developers.cloudflare.com/ai-gateway/) | Cloudflare AI Gateway current docs；2026-08-20 | Provider / model integrations、analytics / logging、rate limiting、request retry、model fallback |
| `S-15` | [Azure API Management AI Gateway tier (preview)](https://learn.microsoft.com/en-us/azure/api-management/ai-gateway-overview) | Azure API Management AI Gateway tier（public preview）；2026-08-20 | centralized endpoint、model / tool backend routing、runtime / backend credentials、policies、request / token limits、OpenTelemetry telemetry、models / MCP tools |
| `S-15a` | [AI gateway in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/genai-gateway-capabilities) | Azure API Management all tiers；capability availability varies by service tier；unified model API 与 Microsoft Foundry integration 分别标 preview；2026-08-20 | authentication、backend load balancing、token limits / quotas、observability、model / agent / tool governance |
| `S-16` | [RFC 9110 §9.2.2 Idempotent Methods](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2) | HTTP semantics standard | 非幂等请求自动重试需要已知安全语义或确认原请求未应用 |

Repository sources：`R-01` Article 01；`R-02` Article 02；`R-03` Article 03；`R-04` course glossary；`R-05` Article 04 canonical/full plan。它们分别用于 Provider/API/SDK 分层、Prompt 与 Provider contract 边界、Envelope→Parse→Schema→DTO→Domain gate、术语边界与本篇范围。

## Counter-evidence And Risks

1. 官方 SDK 会自动 retry create 请求，反驳“所有生成请求都绝不能自动 retry”。本研究要求尊重具体 SDK eligible categories，并在额外 retry 前核对重放安全、ownership 与预算。
2. Gateway 产品能力并不一致：Cloudflare `S-14` 才直接列出 request retry / model fallback；Azure preview tier `S-15` 与 all-tier `S-15a` 支撑的是各自明确列出的 endpoint、routing、credential / policy、limits、telemetry、resiliency 与 model / agent / tool 范围，且 Azure 能力仍受 preview / service-tier 边界约束。因此 Adapter/Gateway/Runtime 表是课程责任切分，不是行业命名标准。
3. OpenAI 将新增 streaming event type 视为兼容变更，Anthropic 也要求容忍未知事件。封闭枚举若没有 raw/unknown 分支会把未来合同误判为 success 或 crash。
4. Usage 不能只保留 total；两家维度与累计语义不同，统一总数不能证明相同计费语义。
5. 本 transaction 未发起 Provider 调用；网络故障、SSE 顺序、Retry-After 与 SDK attempt 次数均未实测，所有行为性陈述限于 `DOC_CONFIRMED`。
6. API 家族文档不证明每个模型、区域、账户、云托管渠道能力相同；capability descriptor 必须带具体 scope。
7. 未找到覆盖两家 create API 的统一 exactly-once/idempotency-key 保证；不能把 timeout 后重放写成默认安全，也不能给固定跨 Provider 次数。

## Evidence Gate Candidate

- Recommendation：`PASS`
- Core behavioral claims：`8 registered / 0 BLOCKED`
- Claim mix：`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL`
- Provider official sources：`OpenAI + Anthropic`，另有 `Cloudflare + Microsoft` Gateway 产品证据与 `RFC 9110`
- Required Lab：`NONE`
- Provider Calls：`NONE`
- Fake Provider：`NONE；后续如需仅可另立 PROPOSAL / NOT_EXECUTED`
- Runtime proof：`UNVERIFIED；不得在 Outline/Draft 升格为 runtime-confirmed`

Gate 可以通过的前提是：SDK retry 默认值始终带 Provider/语言/日期 scope；Gateway 只写课程 working boundary；自动 retry 只作为 gated policy；partial 不写成 final，Streaming event 不写成 Agent event；capability descriptor 保持 `PROPOSAL`；官方文档合同不升级为本 transaction runtime evidence。

## Next Action

交给 Outliner：以 `04-C01 → 04-C03/04-C04 → 04-C05/04-C06 → 04-C07 → 04-C08` 组织“问题空间 → 抽象模型 → streaming/error/retry 机制 → Adapter/Gateway 边界 → capability 验证点”；所有高风险句必须链接 `evidence.md` 对应 Evidence Card，并保留 `Provider Calls NONE / Runtime UNVERIFIED`。
