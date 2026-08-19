# Article 01 Research Plan

- Research Phase：`A1 PLANNING ONLY`
- Research Status：`NOT_STARTED`
- Lifecycle Status：`PLANNED`
- Evidence Status：`BLOCKED`
- Planned Provider Strategy：`One Primary Provider + Cross-provider Counter-check`
- Allowed Next Phase：`A2 Evidence-first Research`

> 本文件只记录研究问题与取证计划，不回答问题，不把候选模型写成已确认事实。

## Research Questions

| RQ | 研究问题 | Why It Matters | Expected Evidence Type | Version Sensitivity | Later Article Boundary | Status |
|---|---|---|---|---|---|---|
| RQ-01 | LLM、Model、Model API、Provider、SDK、Application 分别是什么？哪些是稳定抽象、课程工作定义或 Provider-specific contract？ | 决定全篇分层语言，防止把模型能力、服务契约和客户端代码混作一层。 | `OFFICIAL_DOC`、`OFFICIAL_API_REFERENCE`、`PROVIDER_SDK_DOC`；跨 Provider 对照 | High：产品命名、SDK 和 API 会变 | 04 正式处理 Adapter / Provider 差异 | `NOT_STARTED` |
| RQ-02 | 从应用可观察角度，一次最小 Model API 调用可拆成哪些阶段？哪些 Provider 内部阶段不可公开确认？ | 建立请求—响应主图，同时限制对服务端内部实现的推测。 | `OFFICIAL_API_REFERENCE`、`PROVIDER_SDK_DOC`；必要时 `EXPERIMENT` | High：endpoint、schema、transport 会变 | 04 展开 Streaming / Error / Retry | `NOT_STARTED` |
| RQ-03 | Messages / Input、message object、role、content、conversation history 与 model context 是什么关系？ | `Messages = Memory` 是后续 Memory 课程最危险的错误前提。 | `OFFICIAL_API_REFERENCE`、`OFFICIAL_DOC`；跨 Provider schema 对照 | High：Input 形态和字段持续演进 | 12—15 正式处理 Context / Memory | `NOT_STARTED` |
| RQ-04 | System / Developer / User / Assistant 等角色是行业统一标准，还是 Provider API Contract？ | 防止把一家 Provider 的角色集合写成通用模型机制。 | 多家 `OFFICIAL_API_REFERENCE`、必要时 `OPEN_SPEC` | High：角色名、优先级和支持范围会变 | 02 处理 instruction hierarchy / task contract | `NOT_STARTED` |
| RQ-05 | Token 在 Model API 中承担哪些最低工程角色：input、output、tokenization、usage accounting、cost 与 latency 关联？ | Token 是 Context Window 与 Usage 的共同计量入口，但不能提前吞掉预算工程。 | `OFFICIAL_DOC`、`OFFICIAL_API_REFERENCE`、必要时 `PRIMARY_PAPER` | High：Tokenizer、计费字段和模型版本会变 | 20 正式处理 Budget Engineering | `NOT_STARTED` |
| RQ-06 | Context Window 限制什么？它与 Messages 数量、字符数、文件大小、Memory 和 Context Engineering 分别是什么关系？ | 建立 token 边界，避免使用字符数或“记忆容量”替代。 | `OFFICIAL_MODEL_DOC`、`OFFICIAL_API_REFERENCE`、必要时 tokenizer 说明 | High：不同模型 / 版本窗口不同 | 12—13 处理 Context 选择、压缩与污染 | `NOT_STARTED` |
| RQ-07 | Model API Response 至少可能包含哪些公开可观察部分：content、response envelope、usage、finish / stop metadata、status / error？ | 帮助读者把生成内容与协议元数据、失败信息分开。 | `OFFICIAL_API_REFERENCE`、`PROVIDER_SDK_DOC` | High：response schema 与 metadata 会变 | 03 处理 Structured Output；04 处理 Response / Error 生命周期 | `NOT_STARTED` |
| RQ-08 | Streaming 改变的是模型能力、服务端生成过程，还是 Application 接收 / 消费输出的方式？哪些结论能由公开接口证明？ | 防止把 streamed output 与 hidden reasoning / chain-of-thought 混淆。 | `OFFICIAL_API_REFERENCE`、`PROVIDER_SDK_DOC`；必要时 `EXPERIMENT` | High：stream protocol 和事件类型会变 | 04 正式展开 Streaming lifecycle | `NOT_STARTED` |
| RQ-09 | 为什么 Provider API 不能直接等同于 Model？Model、endpoint、deployment、API 与 Provider 之间有哪些可确认映射？ | 防止从模型名推断唯一 API，也防止从 API 名推断模型内部实现。 | `OFFICIAL_API_REFERENCE`、Provider model / deployment docs | High：model aliases、deployment 与 endpoint 变化快 | 04 正式建立 Adapter / Gateway | `NOT_STARTED` |
| RQ-10 | Article 01 最少需要多少真实代码，才能解释一次 Model Call 而不变成 SDK 教程？是否同时需要 C# SDK、raw HTTP、Messages 和 Usage 示例？ | 控制 M 权重与示例教学职责，服务 C# / .NET 读者。 | A2 证据可用性 + `COURSE_DESIGN_REVIEW`；必要时最小可运行 Example | Medium：取决于 SDK 可读性与稳定性 | 03 后 Lab 01 承担正式可运行合同验证 | `NOT_STARTED` |
| RQ-11 | Transport、Authentication、Rate Limit、Schema Validation、Provider Error、Refusal / Stop、Application Parse Failure 与 Wrong Answer 应如何做最低分层？ | 防止把 API Error 和 Model Output Quality 混作“模型失败”。 | `OFFICIAL_API_REFERENCE`、official error guide、SDK exception docs | High：错误码和异常类型会变 | 04 处理 Error / Retry；21 处理完整 Failure Taxonomy | `NOT_STARTED` |

## Research Priority

1. `HIGH`：RQ-03，验证 `Messages != Long-term Memory` 的最小可证边界。
2. `HIGH`：RQ-01 / RQ-02 / RQ-09，建立 Model、Provider、API、SDK、Application 的分层。
3. `HIGH`：RQ-05 / RQ-06，建立 Token 与 Context Window 的最低工程边界。
4. `MEDIUM-HIGH`：RQ-07 / RQ-11，拆 Response Envelope 与 Failure Layer。
5. `MEDIUM`：RQ-04 / RQ-08 / RQ-10，确认角色、Streaming 和代码教学策略。

## Planned Source Order

1. 主 Provider 官方 API 概览与 Reference。
2. 主 Provider 官方 C# / .NET SDK 文档与代码样例。
3. Anthropic / Google 官方 API 文档进行反查。
4. 必要时查 Open Specification 或 Primary Paper；不以二手博客替代 API Contract。
5. 只有文档无法解决 serialization、usage 或 streaming 边界时，才在后续阶段设计最小实验。

## A1 Stop Line

- 不记录 Research Findings。
- 不填写答案或引用具体版本结论。
- 不创建正式 Evidence Cards。
- 不把任何 RQ 标为 `ANSWERED`。
- 下一步只能进入 A2 Evidence-first Research。
