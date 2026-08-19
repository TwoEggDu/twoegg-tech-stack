# Article 01 Evidence Register

- Evidence Status：`CONFIRMED`
- Evidence Gate：`PASSED`
- Claim Count：`12`
- Claim Summary：`11 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Evidence Card Count：`12`
- Retrieved / Verified At：`2026-08-19（Asia/Shanghai）`
- Provider Strategy：`OpenAI primary + Anthropic / Google counter-check`

## Claim Register

| Claim ID | 可进入 Outline 的收窄主张 | Status | Evidence |
|---|---|---|---|
| 01-C01 | 本课程把 Model、Provider、Model API、SDK 与 Application 按能力、服务主体 / 平台、远程软件契约、客户端封装与应用职责分开；具体产品命名和部署仍依 Provider。 | `CONFIRMED` | `01-E01` |
| 01-C02 | Single Model Call 可按应用可观察边界拆成 request construction、SDK / HTTP serialization、Provider API request、response delivery 与 application handling；该模型不描述 Provider 未公开内部管线。 | `CONFIRMED` | `01-E02` |
| 01-C03 | Messages / input 是当前请求的结构化输入；手工重发历史或使用 Provider state mechanism 才形成连续上下文，不能据此声称 Model 自身具有跨请求 Long-term Memory。 | `CONFIRMED` | `01-E03` |
| 01-C04 | Message role 与 system instruction 的字段、model support 和 placement 属于 Provider API contract；Anthropic 的 generic / top-level baseline 与当前部分模型的 mid-conversation `role: system` 例外必须同时保留，不能外推为跨 Provider 固定 role enum。 | `CONFIRMED` | `01-E04` |
| 01-C05 | Token 是模型处理输入输出的计量粒度，API 可据此报告 usage 或预先计数；字符、单词、文件大小与 Token 不存在可跨模型固定换算。 | `CONFIRMED` | `01-E05` |
| 01-C06 | Context Window 是绑定模型 contract 的 token capacity；公开文档把输入、输出及部分其他 token 纳入边界，因此不能直接等同字符数、Message 数或文件大小。 | `CONFIRMED` | `01-E06` |
| 01-C07 | Response 常见职责包括 generated content、usage 与 finish / stop metadata，但具体 envelope 与字段名依 Provider / API 版本变化。 | `CONFIRMED` | `01-E07` |
| 01-C08 | HTTP Streaming 通过增量事件让 Application 更早消费输出；它是 response delivery contract，不自动等于公开模型隐藏推理过程。 | `CONFIRMED` | `01-E08` |
| 01-C09 | Model selector 与 Provider API / endpoint 是不同 contract 元素；面对新 Provider 必须查其 model、endpoint / deployment 和 schema 映射。 | `CONFIRMED` | `01-E09` |
| 01-C10 | 官方 OpenAI .NET SDK 是访问 OpenAI REST API 的客户端库，并由 OpenAPI specification 生成；SDK API 与远程 API 不应混称。 | `CONFIRMED` | `01-E10` |
| 01-C11 | Transport / HTTP、authentication、rate limit、request validation、successful stop / refusal 与 bad answer 是不同 failure / completion layer；429 不证明模型推理失败。 | `CONFIRMED` | `01-E11` |
| 01-C12 | 课程用单一 Provider 示例落地抽象，但不把该 Provider 的 role、schema、SDK 类型或 endpoint 写成 Industry Definition。 | `PROPOSAL` | `01-E12` |

## Evidence Cards

### Evidence 01-E01｜五层职责边界

- Claim ID：`01-C01`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Text generation](https://developers.openai.com/api/docs/guides/text)；[OpenAI .NET library](https://github.com/openai/openai-dotnet)；[Gemini GenerateContent API](https://ai.google.dev/api/generate-content?hl=en)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；官方页面当日版本，OpenAI .NET `2.x` 文档面
- Observation：OpenAI 把 Responses API 用于 direct model requests；.NET 库是访问 OpenAI REST API 的客户端；Google endpoint 以 Provider URL + model path 接收请求。
- Counter-evidence：三处 contract 均把 model selector、remote API 和 client application 分开。
- Interpretation：能力对象、服务 / contract、客户端和应用职责可分层；Provider 是课程用于服务主体 / 平台的工程词。
- Proves：可建立 Model / Provider / API / SDK / Application 分层。
- Does Not Prove：不证明所有部署都远程或所有 Provider 采用同一拓扑。
- Limitations / Course Usage：应用边界抽象；用于 Section 1。

### Evidence 01-E02｜公开请求—响应边界

- Claim ID：`01-C02`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Text generation](https://developers.openai.com/api/docs/guides/text)；[Responses TypeSpec](https://github.com/openai/openai-dotnet/blob/main/specification/base/typespec/responses/operations.tsp)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI Responses API 当日 contract
- Observation：官方页面并列 C# SDK 与 `POST /v1/responses`，请求含 model / input，响应含 output；TypeSpec 定义 POST body 到 Response / SSE / error response。
- Counter-evidence：未用 SDK 类名推测 Provider validation、routing、tokenization 或 inference 的内部顺序。
- Interpretation：只画 Application 可观察的 request / transport / response / handling，内部标 unknown boundary。
- Proves：Figure 1 的 contract 链路。
- Does Not Prove：不证明服务端组件数量或调用路径。
- Limitations / Course Usage：以 OpenAI 落地，只提取职责；用于 Section 2。

### Evidence 01-E03｜Messages 不等于长期记忆

- Claim ID：`01-C03`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Conversation state](https://developers.openai.com/api/docs/guides/conversation-state)；[Anthropic Create a Message](https://platform.claude.com/docs/en/api/messages/create)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI / Anthropic 当日 contract
- Observation：OpenAI 明示手工管理时每个 text generation request 独立且 stateless；应用追加历史或传 `previous_response_id`。Anthropic 把 Messages API 多轮用法描述为 stateless conversation，并要求请求包含 prior turns。
- Counter-evidence：OpenAI conversation object / previous response 是 Provider 状态机制，不是 Model 自身长期记忆的证明。
- Interpretation：连续体验来自请求输入或外部状态服务；Messages 是 contract 表达。
- Proves：重发历史 `!=` 模型自己记住。
- Does Not Prove：不否认产品可以实现 Session / Memory。
- Limitations / Course Usage：Memory 分类留给 14—15；Section 3 核心。

### Evidence 01-E04｜Role 是 Provider Contract

- Claim ID：`01-C04`
- Evidence Status / Class：`CONFIRMED / VERSION_SENSITIVE_OFFICIAL_DOC`
- Source：[OpenAI Responses API Reference](https://platform.openai.com/docs/api-reference/responses)；[Anthropic Create a Message](https://platform.claude.com/docs/en/api/messages/create)；[Anthropic Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages)；[Gemini GenerateContent](https://ai.google.dev/api/generate-content?hl=en)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；三家当日公开 contract。Anthropic 结论联合读取 generic Messages API Reference 与 model-specific feature page，不外推到未列出的模型或未来版本。
- Observation：OpenAI Responses 的 input message role 当前允许 system / developer / user / assistant；顶层 `instructions` 可插入 system 或 developer instruction，字符串形式等价于 developer-role text input。Anthropic generic / conversation-start baseline 仍把 system instruction 放在顶层 `system`，常规 messages 使用 user / assistant；当前 feature page 同时列出 Claude Fable 5、Claude Mythos 5、Claude Opus 4.8、Claude Opus 5 与 Claude Sonnet 5 支持 messages 数组中的 mid-conversation `role: system`。该 message 不能作为首条，必须紧跟 user turn（或以 server tool result 结束的 assistant turn），并且只能位于数组末尾或紧接 assistant turn 之前；其他 placement 返回 400。Google contents 使用 user / model，systemInstruction 独立。
- Counter-evidence：只读 Anthropic generic API Reference 会得到“messages 没有 system role”的过强全集结论；只读 feature page 又会把 model / placement 受限例外误写成所有 Anthropic 模型与任意位置都支持。
- Interpretation：不能建立跨 Provider 固定 role enum；同一 Provider 内也必须按 API + model + feature + placement + version 联合核对。
- Proves：role / system instruction 的表示方式是 Provider-specific；OpenAI Responses 不能省略 system 后再把其余三项写成完整 role set；Anthropic 顶层 `system` 是通用起始基线，但截至核对日存在明确的 model-specific mid-conversation `role: system` 例外。
- Does Not Prove：不证明所有 Anthropic 模型支持该例外，不证明 `role: system` 可放在 messages 的任意位置，也不证明存在跨 Provider 统一 role enum 或完整 instruction hierarchy。
- Limitations / Course Usage：model support、placement 与 API 会演进；这里只记录字段集合、起始基线和例外边界，不展开 Prompt priority 教程；用于 Section 3 对照表。

### Evidence 01-E05｜Token 不是字符

- Claim ID：`01-C05`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Counting tokens](https://developers.openai.com/api/docs/guides/token-counting)；[Gemini Tokens](https://ai.google.dev/gemini-api/docs/tokens)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；当日 token counting contract
- Observation：OpenAI 可对 Responses payload 计数，并说明 character-based estimation 对文件 / 图片不准确；Google 定义 tokenization、input/output usage，并说明文本、文件和多模态输入都会 tokenized。
- Counter-evidence：Google 的约 4 characters 只是英文直觉；OpenAI 明确简单字符估算有边界。
- Interpretation：Token 是 model / contract-specific 计量，不能用字符固定替代。
- Proves：`Token != Character`，usage 是可观察数据。
- Does Not Prove：不提供中文换算、BPE 数学或价格优化。
- Limitations / Course Usage：tokenization 随模型和输入变化；用于 Section 4。

### Evidence 01-E06｜Context Window 是 Token Capacity

- Claim ID：`01-C06`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Conversation state](https://developers.openai.com/api/docs/guides/conversation-state)；[Anthropic Context windows](https://platform.claude.com/docs/en/build-with-claude/context-windows)；[Gemini Tokens](https://ai.google.dev/gemini-api/docs/tokens)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；不固化具体模型窗口数值
- Observation：OpenAI 定义单次请求最大 token 数并包含 input、output、reasoning；Anthropic 列出 system、messages、tools 与 output；Google 定义 combined input / output token limit。
- Counter-evidence：不以某个模型的 128k / 1M 营销数值当通用定义。
- Interpretation：Context Window 是 model-specific token capacity。
- Proves：它不等于文件字节、字符或 Message 条数。
- Does Not Prove：不证明长上下文质量或治理策略。
- Limitations / Course Usage：各模型 overflow 行为不同；Section 4 / Figure 2。

### Evidence 01-E07｜Response 不只是字符串

- Claim ID：`01-C07`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Text generation](https://developers.openai.com/api/docs/guides/text)；[Anthropic Messages](https://platform.claude.com/docs/en/api/messages/create)；[Gemini GenerateContent](https://ai.google.dev/api/generate-content?hl=en)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；三家当前 response schema
- Observation：OpenAI 有 output items / output_text helper 和 usage；Anthropic 有 content / stop_reason / usage；Google 有 candidates / finishReason / usageMetadata。
- Counter-evidence：字段不同，因此不建立统一 JSON schema。
- Interpretation：generated content、usage、finish metadata 是常见职责位置而非共同字段名。
- Proves：Application 需要处理 response envelope。
- Does Not Prove：不证明所有 API 必含相同 metadata。
- Limitations / Course Usage：tools / multimodal 留后文；Section 5。

### Evidence 01-E08｜Streaming 是增量 Delivery

- Claim ID：`01-C08`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Streaming](https://developers.openai.com/api/docs/guides/streaming-responses)；[Anthropic Streaming](https://platform.claude.com/docs/en/build-with-claude/streaming)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；HTTP SSE contract
- Observation：OpenAI 说明默认整包返回，streaming 允许提前处理输出；Anthropic 通过 SSE 发送 message / content-block 增量事件并可聚合为完整 Message。
- Counter-evidence：reasoning / thinking 是另行定义的内容类型，不由 `stream=true` 自动公开。
- Interpretation：Streaming 改变 delivery / consumption；内容可见性由 event schema 决定。
- Proves：Streaming 不等于自动公开 Hidden Reasoning。
- Does Not Prove：不解释 backpressure、恢复或 latency 收益。
- Limitations / Course Usage：只覆盖 HTTP SSE；Section 6。

### Evidence 01-E09｜Model Selector 与 Endpoint 分离

- Claim ID：`01-C09`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Text generation](https://developers.openai.com/api/docs/guides/text)；[OpenAI .NET library](https://github.com/openai/openai-dotnet)；[Gemini GenerateContent](https://ai.google.dev/api/generate-content?hl=en)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；当前 request / SDK options
- Observation：OpenAI `/v1/responses` body 单独携带 model，官方 SDK 可配置 endpoint；Gemini endpoint path 包含 `{model=models/*}`。
- Counter-evidence：没有把 OpenAI-compatible endpoint 当作行为完全兼容。
- Interpretation：选择模型与调用 Provider contract 是相关但不同的问题。
- Proves：新 Provider 要分别查 endpoint、model / deployment 与 schema。
- Does Not Prove：不证明跨 Provider 可无损替换。
- Limitations / Course Usage：完整 abstraction 留给 04；Section 1。

### Evidence 01-E10｜SDK 是 REST API 客户端

- Claim ID：`01-C10`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI .NET repository](https://github.com/openai/openai-dotnet)；[OpenAI OpenAPI repository](https://github.com/openai/openai-openapi)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI .NET `2.x` 文档面
- Observation：官方仓库称 .NET library 为访问 REST API 的客户端并由 OpenAPI specification 生成；OpenAPI 描述 endpoint、auth、request / response schema 并生成 official SDK。
- Counter-evidence：SDK convenience method 与 raw HTTP 语法不同但指向同一远程 contract。
- Interpretation：SDK 隐藏客户端样板，但不是 Model 或远程 API 本身。
- Proves：`SDK != API`。
- Does Not Prove：不证明 SDK 没有 retry / helper 行为。
- Limitations / Course Usage：以 OpenAI 为主例；Section 1 / C# 示例。

### Evidence 01-E11｜API Failure 与 Bad Answer 分层

- Claim ID：`01-C11`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC`
- Source：[OpenAI Error codes](https://developers.openai.com/api/docs/guides/error-codes)；[Anthropic Stop reasons](https://platform.claude.com/docs/en/build-with-claude/handling-stop-reasons)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；当日 error / stop contract
- Observation：OpenAI 分列 connection、timeout、authentication、bad request、rate limit、server error；Anthropic 明确 stop_reason 是成功 response 的完成原因，与 request processing error 不同。
- Counter-evidence：不把 refusal / max tokens 一律归为 HTTP error，也不把语义错误归入 API status。
- Interpretation：传输 / contract 成功与生成内容正确是两条轴。
- Proves：429 不说明推理失败，HTTP 200 也不说明答案正确。
- Does Not Prove：不提供 retry policy 或完整 taxonomy。
- Limitations / Course Usage：应用 parse / quality 为课程检查点；Section 5。

### Evidence 01-E12｜One Provider Example 不是 Industry Definition

- Claim ID：`01-C12`
- Evidence Status / Class：`PROPOSAL / DESIGN_PROPOSAL`
- Source：[Article Card](article-card.md)；`01-E04`、`01-E07`、`01-E10`
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；课程 v3.1 / Article 01
- Observation：跨 Provider 对照显示 role、system instruction 和 response schema 不同；三家完整 SDK 横评不服务 Single Model Call 主线。
- Counter-evidence：对称展开三家会增加教程篇幅并模糊稳定职责。
- Interpretation：用 OpenAI .NET 落地，Anthropic / Google 只作边界反例。
- Proves：`N/A`，课程设计选择。
- Does Not Prove：不声称 OpenAI contract 是行业标准。
- Limitations / Course Usage：04 必须重新研究 Provider 差异；全篇示例策略。

## Evidence Gate

- 核心 Claim：`11 CONFIRMED / 0 PARTIAL / 0 BLOCKED`
- 课程示例策略：`1 PROPOSAL`
- Tiny Experiment：`NOT REQUIRED`
- Lab：`N/A`
- Outcome：`PASS`

所有核心 Claim 均可按证明范围进入 Outline；下一步为 A3 Detailed Outline。
