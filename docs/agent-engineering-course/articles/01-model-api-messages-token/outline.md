# Article 01 Detailed Outline｜模型调用到底发生了什么：LLM、Model API、Messages 与 Token

- Lifecycle Status：`OUTLINE_READY`
- Evidence Gate：`PASSED`
- Outline Gate：`PASSED`
- Article Type：`原理 / 基础机制篇`
- Target Length：`约 4,500—6,000 中文字`
- Target Reading Time：`12—16 分钟`
- Main Example：`OpenAI .NET Responses API（官方 2.x 文档面）`
- Counter-check：`Anthropic Messages / Google GenerateContent`
- Required Lab：`N/A`

## 1. Reader Transformation

读者从“调用 SDK 就是调用模型”的模糊认识，转变为能够沿一次 Single Model Call 指出 Application、SDK / HTTP Client、Provider API Contract、Structured Input、Model Selector、Response Envelope 与 Application Handling 的位置；同时知道哪些是可迁移职责，哪些必须回到具体 Provider 的当前 contract 核对。

## 2. Opening｜“我不就是调了一个 API 吗？”

- Teaching Question：一行 `CreateResponseAsync` 背后，工程上究竟跨过了哪些边界？
- Core Thesis：一次模型调用不是一个不可分割的黑盒动作；应用可观察部分至少包含构造请求、客户端序列化、远程 API contract、响应交付和应用处理。
- Claim IDs：`01-C02`
- Evidence IDs：`01-E02`
- Opening Scene：一段 C# 调用成功返回文本，但团队随后遇到三类争论——“换模型是不是只换字符串”“历史消息是不是模型记忆”“HTTP 200 是不是答案正确”。
- Wording Strength：`可按应用可观察边界拆解`，不写成 Provider 内部真实拓扑。
- Scope Boundary：不解释 Transformer、推理服务编排或 Provider 内部路由。
- Bridge：先把最容易混成一团的五个名词拆开。

## 3. Section 1｜先把五个对象分开：Model、Provider、API、SDK、Application

- Teaching Question：当工程师说“我们调用 GPT / Claude / Gemini”时，究竟可能指哪些不同对象？
- Core Thesis：Model 是能力对象；Provider 是提供模型服务与 contract 的主体 / 平台；Model API 是远程软件契约；SDK 是调用契约的客户端封装；Application 负责组织输入、调用、消费结果与处理失败。
- Claim IDs：`01-C01`、`01-C09`、`01-C10`、`01-C12`
- Evidence IDs：`01-E01`、`01-E09`、`01-E10`、`01-E12`
- Presentation：五层职责表，列出“负责什么 / 不等于什么 / 换 Provider 时要查什么”。
- Key Distinctions：
  - `Model != Provider`
  - `Model selector != endpoint / deployment`
  - `SDK != REST API`
  - `Application != SDK sample`
- Example：同一 OpenAI Responses 调用分别写成 SDK 与 raw HTTP，让读者看到语法不同但共同依赖远程 API contract。
- Wording Strength：五层是课程抽象；产品命名、部署方式和 schema 依 Provider。
- Scope Boundary：不在本篇设计 Adapter / Gateway；留给 Article 04。
- Bridge：对象分开后，再沿一次真实调用把它们串起来。

## 4. Section 2｜一次 Single Model Call 的可观察链路

- Teaching Question：从 C# 方法调用到应用拿到结果，哪些步骤是我们可以验证的？
- Core Thesis：应用构造结构化输入，SDK / HTTP Client 将其变成符合 Provider contract 的请求；Provider 返回 response 或 error；应用再提取内容、读取 metadata 并决定下一步。
- Claim IDs：`01-C02`、`01-C07`、`01-C10`
- Evidence IDs：`01-E02`、`01-E07`、`01-E10`
- Figure 1：`Single Model Call Engineering Map`

```text
Application
  ├─ choose model / build input / set parameters
  ↓
SDK or HTTP Client
  ├─ serialize / authenticate / send
  ↓  ─────────── Public API Contract Boundary ───────────
Provider Model API
  ├─ validate request / deliver response contract
  ↓
[Provider-internal execution: not inferred in this article]
  ↓
Response or API Error
  ↓
Application Handling
  ├─ extract content / inspect usage and finish metadata
  └─ parse / validate / display / continue
```

- Code Example A：官方 OpenAI .NET `ResponsesClient` 最小调用，标注 Application、model selector、input 和 output helper。
- Code Example B：等价职责的 `POST /v1/responses` 请求片段，只用于显露 remote contract，不扩成 curl 教程。
- Boundary Callout：图中的“Provider-internal execution”是未知边界，不能从 SDK 调用顺序反推服务端 pipeline。
- Wording Strength：`可观察链路 / contract chain`，不使用“所有 Provider 必然按此执行”。
- Scope Boundary：不讲重试、超时治理、流生命周期与 Adapter；留给 Article 04。
- Bridge：链路中的 `input / messages` 最容易被误解为 Memory，需要单独拆开。

## 5. Section 3｜Messages 是请求输入，不是模型长期记忆

- Teaching Question：为什么把历史对话放回请求，不能证明模型“自己记住了”？
- Core Thesis：Messages / input 是当前请求的结构化输入；连续对话可以来自应用重发历史或 Provider state mechanism，但这与 Model 自身跨请求 Long-term Memory 是不同命题。
- Claim IDs：`01-C03`、`01-C04`
- Evidence IDs：`01-E03`、`01-E04`
- Figure 2：`Conversation Continuity Sources`

```text
Previous turns
   ├─ Application re-sends history ─┐
   ├─ Provider conversation state ──┼─> Current request context ─> Model call
   └─ Product memory retrieval ─────┘

Messages / input = current request representation
Long-term memory = external product / runtime capability, not proven by messages alone
```

- Provider Counter-check Table：
  - OpenAI：developer / user / assistant；instructions / input 依具体 API。
  - Anthropic：input messages 使用 user / assistant；system 为顶层参数。
  - Google：contents 使用 user / model；systemInstruction 独立。
- Teaching Payoff：面对新 Provider，不复制一个跨平台固定 role enum；先读它的当前 API contract。
- Wording Strength：只说公开 contract 的当前差异，不推导模型内部如何理解 role。
- Scope Boundary：Working Memory、Session、Long-term Memory 的正式分类留给 Article 14—15；Prompt 优先级留给 Article 02。
- Bridge：当输入越来越长，工程边界不是字符数，而是 Token 与 Context Window。

## 6. Section 4｜Token 与 Context Window：容量单位，不是字符换算题

- Teaching Question：为什么“这份文档只有几万字”不足以判断能否放进一次请求？
- Core Thesis：Token 是模型 / contract 相关的输入输出计量粒度；Context Window 是绑定模型 contract 的 token capacity，通常共同容纳输入、输出和 contract 指定的其他 token。
- Claim IDs：`01-C05`、`01-C06`
- Evidence IDs：`01-E05`、`01-E06`
- Explanatory Sequence：
  1. 文本或多模态输入被 tokenized；Token 不等于字符、单词或字节。
  2. Request 中不只有用户正文，role、边界、工具定义等也可能贡献输入 Token。
  3. Context Window 是单次请求可用的 token capacity；给输出留空间也是 contract 约束的一部分。
  4. Usage / count-tokens API 是可观察数据，比固定字符估算可靠。
- Mini Example：同一“字数”的中文、英文和结构化 payload 不能假设相同 Token 数；不提供虚假的固定换算率。
- Boundary Callout：`Context Window != 文件大小`，也不等于“模型能高质量理解窗口内任意内容”。
- Wording Strength：`通常 / 依模型与 API contract`；不固化 128k、1M 等易过时数值。
- Scope Boundary：Tokenizer 数学、BPE 实现、价格和 Token / Cost 优化留给后续；Article 20 正式讨论 Budget Engineering。
- Bridge：调用容量满足后，返回的也不只是一个字符串。

## 7. Section 5｜Response Envelope 与三层结果判断

- Teaching Question：为什么拿到 HTTP 200 和一段文本，还不能说“这次调用成功完成了任务”？
- Core Thesis：Response 常见职责包括 generated content、usage 和 finish / stop metadata；工程上还要分开判断 transport / API contract 是否成功、generation 是否正常结束、application task 是否得到正确结果。
- Claim IDs：`01-C07`、`01-C11`
- Evidence IDs：`01-E07`、`01-E11`
- Code Example C：概念化 response envelope，标注 `output/content`、`usage`、`finish/stop metadata`；明确字段名不是跨 Provider 统一 schema。
- Three-layer Table：
  1. Transport / API：连接、认证、请求校验、限流、服务端错误。
  2. Generation completion：正常停止、长度上限、refusal 或 Provider-specific stop metadata。
  3. Application quality：答案是否正确、格式是否可解析、是否满足业务约束。
- Key Counterexamples：
  - HTTP 429 不证明模型“推理失败”。
  - HTTP 200 不证明答案正确。
  - stop reason 是成功 response 的完成信息，不等同于 HTTP error。
- Wording Strength：只建立分层，不给通用 retry matrix。
- Scope Boundary：完整 Failure Taxonomy 留给 Article 21；Parse / Validate / Repair 留给 Article 03 与 Lab 01。
- Bridge：Response 还可以整包交付，也可以增量交付。

## 8. Section 6｜Streaming 改变的是交付方式

- Teaching Question：开启 streaming 后，到底变了什么？
- Core Thesis：HTTP streaming 通过 SSE 等增量事件让 Application 在生成完成前开始消费输出；它改变 response delivery / consumption，不自动改变 Model Capability，也不自动暴露隐藏推理。
- Claim IDs：`01-C08`
- Evidence IDs：`01-E08`
- Comparison：
  - Non-streaming：等待完整 response，再统一处理。
  - Streaming：按 event schema 接收增量，最终仍需聚合、判断结束并处理错误。
- Boundary Callout：stream event 中能看到什么由公开 schema 决定；`stream=true` 不是“显示模型脑内过程”的开关。
- Wording Strength：限定为 HTTP SSE 公开 contract，不把所有实时协议归一。
- Scope Boundary：backpressure、断线恢复、增量解析与 retry 留给 Article 04。
- Bridge：至此可以把一次调用压缩成一张可迁移检查表。

## 9. Section 7｜面对一个新 Provider，先检查哪些 contract？

- Teaching Question：如果明天换一家 Provider，哪些知识可以直接迁移，哪些必须重新确认？
- Core Thesis：稳定的是职责问题，不稳定的是字段答案；工程师应按 contract checklist 重新核对，而不是照搬主示例。
- Claim IDs：`01-C01`、`01-C04`、`01-C07`、`01-C09`、`01-C12`
- Evidence IDs：`01-E01`、`01-E04`、`01-E07`、`01-E09`、`01-E12`
- Transfer Checklist：
  1. endpoint / authentication / API version 是什么？
  2. model 或 deployment 如何选择？
  3. input / messages schema 与 role 集合是什么？
  4. system instruction 放在哪里？
  5. response content、usage、finish metadata 在哪里？
  6. streaming event 与 error contract 如何表达？
  7. token count / context limit 从哪里获取？
- Wording Strength：这是一张阅读 API contract 的课程检查表，不是统一 Provider interface。
- Scope Boundary：不在此设计兼容层；Article 04 再把差异收敛为 Adapter / Gateway。
- Bridge：完成 Learning Check，转入 Article 02 的 Prompt contract。

## 10. Closing｜模型调用是契约链，不是一行魔法

- Recap：
  - `Model != Provider != API != SDK != Application`
  - `Messages != Long-term Memory`
  - `Token != Character`
  - `Context Window != File Size`
  - `Streaming != Hidden Reasoning`
  - `API Success != Correct Answer`
- Forward Link：Article 02 将在这条链路上继续回答“Prompt 如何成为可维护、可测试的输入 contract”；不提前展开 Prompt Engineering。

## 11. Learning Check Plan

1. 使用官方 SDK 时，更准确地说是在通过什么调用什么？
   - Reference Idea：Application 通过 SDK 封装访问 Provider API；API 再选择 Model / deployment。
2. 为什么 SDK 不能与 Model API 混称？
   - Reference Idea：SDK 是客户端库，API 是远程 contract；两者语法与版本生命周期不同。
3. 为什么重发历史 Messages 不等于模型具有长期记忆？
   - Reference Idea：历史成为当前请求输入；连续性来源可能在 Application 或 Provider state。
4. 为什么不能用固定字符数换算 Token 或 Context Window？
   - Reference Idea：tokenization 依模型与输入类型；请求结构也会占 Token。
5. Streaming 主要改变了哪一层？
   - Reference Idea：response delivery / application consumption，不自动改变模型能力或内容可见性。
6. HTTP 200、stop reason 和“答案正确”为什么要分开判断？
   - Reference Idea：分别属于 API contract、generation completion 与 application quality。

## 12. Claim Coverage Matrix

| Claim ID | Main Placement | Support Type | Omission Risk |
|---|---|---|---|
| `01-C01` | Section 1 / 7 | 五层职责表、迁移检查表 | 低 |
| `01-C02` | Opening / Section 2 | Figure 1、SDK + HTTP 对照 | 低 |
| `01-C03` | Section 3 | Figure 2、stateless counter-check | 低 |
| `01-C04` | Section 3 / 7 | 三 Provider role 对照 | 低 |
| `01-C05` | Section 4 | Token 解释、反固定换算 | 低 |
| `01-C06` | Section 4 | Context capacity 解释 | 低 |
| `01-C07` | Section 2 / 5 / 7 | response envelope、职责表 | 低 |
| `01-C08` | Section 6 | streaming / non-streaming 对照 | 低 |
| `01-C09` | Section 1 / 7 | selector / endpoint 区分 | 低 |
| `01-C10` | Section 1 / 2 | SDK / HTTP 同职责对照 | 低 |
| `01-C11` | Section 5 | 三层结果判断 | 低 |
| `01-C12` | Section 1 / 7 | Provider-specific 标注 | 低 |

## 13. Evidence Omission List

- 不写具体模型的 Context Window 数值：易随模型版本变化，且不服务概念定义。
- 不写当前价格或 Token 单价：属于 Budget Engineering，不服务本篇主线。
- 不推测 Provider 内部 validation、routing、tokenization、inference 的组件顺序：官方公开 contract 不足以证明。
- 不把 OpenAI response 字段复制成统一 schema：Anthropic / Google 已提供反例。
- 不运行 Tiny Experiment：官方 schema、state、token、streaming 与 error 文档足以支撑本篇 Claim；正文明确这是文档证据篇。

## 14. Job Competency Coverage

| Competency | Article Evidence of Learning |
|---|---|
| 阅读基础 Model API 调用代码 | 能沿 SDK 示例指出 model、input、response handling |
| 区分 Model / Provider / API / SDK / Application | 能完成五层职责表与反例解释 |
| 解释 Messages 与 Memory 边界 | 能指出连续上下文的三种可能来源 |
| 解释 Token / Context Window | 能拒绝字符 / 文件大小固定换算 |
| 区分调用失败与输出质量 | 能使用三层结果判断定位问题 |
| 迁移到新 Provider | 能按 checklist 核对 contract 而非照搬 SDK |

## 15. A3 Outline Gate

- [x] 主线从问题空间进入抽象模型，再用一个具体 Provider 落地
- [x] 12 个 Claim 全部覆盖，0 个 `BLOCKED`
- [x] 每个主体 Section 均有 Teaching Question、Core Thesis、证据、边界与 Bridge
- [x] 2 张图均有明确教学职责
- [x] OpenAI 主示例与跨 Provider 反查职责分离
- [x] Learning Check 与 Job Competency Mapping 可验证
- [x] Article 02 / 03 / 04 / 08 / 14—15 / 20—21 边界未越界
- [x] Lab 保持 `N/A`

Outcome：`PASS`。下一步允许进入 A4 Draft。
