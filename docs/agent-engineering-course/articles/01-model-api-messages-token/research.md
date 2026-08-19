# Article 01 Research Conclusion Index

- Research Phase：`A2 EVIDENCE-FIRST RESEARCH`
- Research Status：`COMPLETE`
- Lifecycle Status：`EVIDENCE_READY`
- Evidence Status：`CONFIRMED`（`11 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Research Window：`2026-08-19（Asia/Shanghai）`
- Provider Strategy：`OpenAI primary example + Anthropic / Google counter-check`
- Lab：`N/A`；未执行 API 调用或 Token 实验

## Research Questions

| RQ | Status | Main Finding | Claim IDs | Remaining Boundary | Course Impact |
|---|---|---|---|---|---|
| RQ-01 | `ANSWERED` | Model 是请求选择的能力对象；Provider 暴露服务与 API contract；SDK 是调用 REST API 的客户端封装；Application 负责构造请求与处理结果。具体命名依 Provider。 | `01-C01`、`01-C10` | 不定义所有生态唯一的 Provider / deployment 拓扑。 | 先分层，再读代码。 |
| RQ-02 | `ANSWERED` | 从应用可观察边界可拆为 request construction、SDK / HTTP serialization、Provider endpoint、response delivery 与 application handling；公开资料不支持展开 Provider 内部推理管线。 | `01-C02` | Server-side validation / routing 的内部顺序保持未知。 | Figure 1 只画公开 contract。 |
| RQ-03 | `ANSWERED` | Messages / input 是请求中的结构化输入。OpenAI 明示手工管理时每次 text generation request 独立且 stateless；Anthropic Messages API 也把多轮描述为 stateless conversation。 | `01-C03` | Provider 可提供 conversation object / previous response 等状态服务，但这仍不是“模型自己长期记住”。 | `Messages != Memory` 成为主线模型。 |
| RQ-04 | `ANSWERED` | Role 是 Provider contract：OpenAI Responses input message 当前允许 system / developer / user / assistant，另有顶层 `instructions` instruction mechanism；Anthropic 的 generic / conversation-start baseline 是常规 messages 使用 user / assistant、system instruction 使用顶层 `system`，但截至 2026-08-19，Claude Fable 5、Mythos 5、Opus 4.8、Opus 5、Sonnet 5 另支持受 placement rules 约束的 mid-conversation `role: system`；Google contents 常见 user / model，systemInstruction 独立。 | `01-C04` | 各 Provider 的角色语义、model support、placement、instruction mechanism 和版本持续演进。 | 不建立通用 Role enum。 |
| RQ-05 | `ANSWERED` | Token 是模型处理输入输出的计量粒度；API 可返回 input / output usage，也可预先计数。字符估算不精确，结构、文件和多模态内容也可能消耗 token。 | `01-C05` | 不研究 tokenizer 算法、价格表和优化。 | Token 从名词变成可观察工程数据。 |
| RQ-06 | `ANSWERED` | Context Window 是模型 / API contract 的 token 容量边界，通常涉及输入与输出；不是字符数、Message 条数或文件字节数。 | `01-C06` | 精确窗口必须绑定模型版本；Context 选择与压缩留给 12—13。 | 只讲容量边界，不讲治理。 |
| RQ-07 | `ANSWERED` | 三家 API 都返回生成内容与元数据，但 schema 不同：OpenAI output / usage，Anthropic content / stop_reason / usage，Google candidates / finishReason / usageMetadata。 | `01-C07` | 不把字段名统一化；Structured Output 留给 03。 | 建立 response envelope 观察法。 |
| RQ-08 | `ANSWERED` | OpenAI 与 Anthropic 都把 HTTP streaming 表达为 SSE 增量事件；它改变应用接收 / 消费结果的方式，不自动证明隐藏推理内容被暴露。 | `01-C08` | 事件生命周期、断线恢复与 backpressure 留给 04。 | 只做地图级定位。 |
| RQ-09 | `ANSWERED` | API request 中的 model selector 与 API / endpoint 是不同 contract 元素；同一 API 可选择多个模型，SDK 也可配置兼容 endpoint。 | `01-C09` | deployment、region、alias 的完整映射留给 04。 | `Model != Provider != API`。 |
| RQ-10 | `ANSWERED` | 一个当前 OpenAI .NET Responses API 最小调用足以作为主对象；补一段等价 raw HTTP request 说明 SDK 与 API 的边界，再用 response envelope 观察 usage。 | `01-C12` | 示例不执行，不宣称 SDK 永久稳定；绑定检索日期。 | 避免扩成 SDK 教程。 |
| RQ-11 | `ANSWERED` | HTTP / transport、authentication、rate limit、request validation、successful stop / refusal 与 bad answer 属于不同层；例如 429 是 API 限流或额度层，不是模型答案质量。 | `01-C11` | Retry / normalization 留给 04，完整 taxonomy 留给 21。 | 建立最低故障分层。 |

## Stable Core vs Provider Contract

### 可迁移的最低工程抽象

- Application 构造输入，通过客户端调用 Provider API，并处理 response / failure。
- Model selector、structured input、generated content 与 usage / finish metadata 是常见职责位置。
- Token 是输入输出与 context capacity 的工程计量单位。
- Conversation continuity 必须由请求内容或 Provider 的状态机制显式承载，不能从“看起来连续”推导模型长期记忆。

### Provider-specific Contract

- endpoint、model / deployment selector、role 集合、system instruction 位置。
- request / response schema、usage 字段、finish / stop reason、stream event。
- SDK 类型名、方法名、preview / stable 状态和异常映射。

## Code Decision

- 主例：OpenAI 官方 .NET SDK 的 `ResponsesClient` 最小调用。
- 对照：同一 `/v1/responses` 的 raw HTTP request，只显示 model + input + headers。
- 观察：response output 与 token usage 作为 envelope 概念；不依赖未经确认的 C# usage 属性名。
- 不创建 demo project，不运行真实请求，不引入 API key 或依赖。

## Research Stop Line

- 不展开 Prompt Engineering、Structured Output、Gateway、Retry、Agent Loop、Context Engineering 或 Memory implementation。
- 不执行 tiny experiment；官方 contract 已足以支撑本文最低 Claim。
- 下一步进入 A3 Detailed Outline。
