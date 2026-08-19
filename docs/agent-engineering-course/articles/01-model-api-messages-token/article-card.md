# Article Card 01｜模型调用到底发生了什么：LLM、Model API、Messages 与 Token

> 来源基线：`docs/agent-engineering-series-plan.md` 与 Article 01 A1 Production Kickoff。本文档只冻结研究职责，不预设 A2 的证据结论。

## 1. Positioning

Part I 的基础原理篇，也是 Article 00 世界地图之后的第一个机制入口。它回答：当一个应用“调用模型”时，从应用可观察边界看，工程上有哪些对象、数据和契约参与了一次 Single Model Call。

## 2. Why Now

Article 00 已把 Model 与 Application 分开，但读者仍可能把“调用 SDK”“调用 Provider API”“调用模型”压缩成一句话。后续 Prompt、Structured Output、Model Adapter、Function Calling 和 Agent Loop 都依赖这里先把最小调用边界切清。

## 3. Reader Promise

学完后，读者能够面对一段 Model API 调用代码，指出 Application、SDK / HTTP Client、Provider API Contract、Structured Input、Model Capability、Response Envelope 与 Application Handling 分别位于哪里，并知道哪些说法需要按 Provider 重新核对。

## 4. Learning Outcomes

读者应该能够：

1. 区分 LLM / Model、Model API、Provider、SDK 与 Application。
2. 用公开 API / Application 边界解释一次最小 Model Call 的候选阶段。
3. 解释 Messages / Input 为什么是请求中的结构化输入表达，而不能直接等同于模型长期 Memory。
4. 说明 Token 至少与输入、输出、Context Window、Usage、Cost 和 Latency 相关，但不进入 Budget Engineering。
5. 识别 Response Content、Usage、Finish Metadata、Transport / API Error 和 Application Handling 所属的不同层。
6. 面对新 Provider 时优先检查 API contract，而不是把某个 SDK 写法当作行业定义。

## 5. Prerequisites

- Article 00 已完成并发布。
- 具备 HTTP、JSON、异步调用、错误处理和 C# 接口的基础认识。
- 不要求理解 Transformer、模型训练、Tokenizer 算法、Agent Loop 或 Tool Calling。

## 6. Core Concepts

LLM / Model、Application、Provider、Model API、API Contract、SDK、HTTP Client、Request、Input / Messages、Role、Content、Token、Tokenization、Context Window、Response Envelope、Usage、Finish / Stop Metadata、Streaming、Failure Layer。

这些词在 A1 只作为研究对象；最终定义与措辞强度由 A2 Evidence 决定。

## 7. Core Mental Model

候选“请求—响应”工程模型：

```text
Application
   ↓
SDK / HTTP Client
   ↓
Provider Model API
   ↓
Request
   ├─ model / deployment selector
   ├─ messages / input
   └─ generation parameters
   ↓
Model Execution（只研究公开可确认边界）
   ↓
Response
   ├─ generated content
   ├─ usage
   ├─ finish metadata
   └─ errors / status
   ↓
Application Handling
```

这是 `RESEARCH CANDIDATE`，不是已证实的行业统一流程，也不表示 Provider 内部必须按图部署。A2 需要逐层验证可观察边界、Provider-specific contract 与不可推测区域。

## 8. Evidence Needs

- 各层概念的官方定义与公开 API 边界。
- 主 Provider 的 API Reference、SDK 文档、Request / Response Schema 与 Token Usage 说明。
- 至少一个跨 Provider 反查，用于区分稳定抽象和单一 Provider Contract。
- Messages / Input、Role、Context Window、Usage、Finish Reason、Streaming 与 Error 的版本敏感证据。
- 若文档不能清楚证明 serialization、usage 或 streaming 行为，再评估最小 Experiment；A1 不执行实验。

## 9. Examples

未来示例的教学职责候选：

- Example A｜最小 C# Model Call：只展示 Application、Request、Model Selector 与 Response Handling。
- Example B｜Structured Messages：只展示 role / content 与“当前请求输入”的位置。
- Example C｜Usage Metadata：展示 Token 作为 API 中可观察的工程数据。

是否保留三个示例、是否同时展示 raw HTTP 与 SDK，由 A2 Evidence 和后续 Detailed Outline 决定。本篇不写成 SDK 教程。

## 10. Provider Strategy

采用 `One Primary Provider + Cross-provider Counter-check`：

- 主示例优先考虑 OpenAI 的 C# / .NET 调用路径。
- Anthropic / Google 只承担概念反查，不扩成三家 SDK 教程。
- 所有示例必须明确 `Example Provider != Industry Definition`。
- Provider、endpoint、deployment、request schema、role 与 response metadata 的差异必须按版本记录。

Provider 最终选择仍待 A2 取证，不在 A1 冻结到具体 SDK 版本。

## 11. Relation to DSH

Article 01 只建立 Model Call 基础边界。它将为 28—37 阅读 DSH 时区分 Model、Provider、API Contract、Plugin / Adapter 与 Runtime 调用职责提供前置词汇；本篇不读取 DSH 源码，也不从 DSH 反推通用定义。

## 12. Relation to BuildPilot

BuildPilot 将来需要选择 Provider、组织请求并处理响应，但 Article 01 不设计 BuildPilot Adapter、Gateway 或 Runtime。这里只建立后续设计必须共享的 Single Model Call 语言。

## 13. Confusion Risks

HIGH PRIORITY：

- `Model != Model API`
- `SDK != API`
- `Messages != Memory`
- `Context Window != 字符数 / 文件大小`
- `Streaming != 暴露模型隐藏推理过程`
- `API Error != Model Reasoning Failure`
- `Example Provider != Industry Definition`

## 14. Non-scope

- Transformer、训练、Tokenizer 数学和 BPE 实现。
- Prompt 结构、few-shot、instruction hierarchy 与优化（Article 02）。
- JSON Schema、Structured Output、Parse / Validate / Repair（Article 03 与 Lab 01）。
- Adapter、Gateway、Retry、Error Normalization 与 Streaming Lifecycle（Article 04）。
- Function Calling、Tool Runtime 与 MCP（Article 05—07）。
- Agent Loop、Turn、Step、State、Action、Observation 与 Stop（Article 08）。
- Context 选择、排序、压缩、污染与重建（Article 12—13）。
- Working Memory、Session 与 Long-term Memory（Article 14—15）。
- Token / Step / Cost / Latency 的预算控制与优化（Article 20）。
- 完整 Failure Taxonomy（Article 21）。

## 15. Learning Check

候选问题：

1. 使用 OpenAI / Anthropic SDK 时，更准确地说是在调用什么？
2. 为什么 SDK 不是 Model 的一部分，也不能直接等同于 API？
3. 把多条历史 Messages 再次放入请求，为什么不自动证明 Model 拥有长期 Memory？
4. `128k Context Window` 为什么不能直接解释为 128k 个中文字符？
5. Streaming 主要改变 Model Capability，还是 Application 接收输出的方式？
6. HTTP 429 与“模型回答错误”为什么属于不同的问题层？

A1 不提供答案；答案必须由后续 Evidence 和正文共同建立。

## 16. Weight

`M（Standard Core Lesson）`。需要建立可迁移的最小模型和一条具体调用路径，但不展开 Provider Gateway、Agent Loop 或 Budget Engineering。

## 17. Concept Maturity

- LLM / Model：由 Article 00 的 Introduction 进入 Foundation。
- Application：从世界地图位置感进入一次调用的应用职责。
- Provider：在本篇只建立 API Contract 边界，正式 Adapter / Gateway 留给 04。
- Messages / Token / Context Window / Response：首次正式建立最低工程含义。
- Streaming / Error：只做地图级定位，正式机制留给 04 / 21。

## 18. Job Competency Mapping

完成本篇后，Agent Engineer 应能够：

- 阅读并解释基础 Model API 调用代码。
- 区分 Model / Provider / API / SDK / Application。
- 解释 Messages 与 Memory 的边界。
- 解释 Token / Context Window 的基本工程含义。
- 区分 API Failure 与 Model Output Quality。
- 面对新 Provider 时先检查 API contract、schema、version 与 usage contract，而不是只背 SDK。

这些是职业能力目标，不新增正文主题。
