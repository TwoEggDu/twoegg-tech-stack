# Article 01 Outline Skeleton｜模型调用到底发生了什么

- Outline Type：`A1 SCOPE SKELETON`
- Lifecycle Status：`PLANNED`
- Evidence Dependency：`BLOCKED`
- Article Type：`PRINCIPLE / FOUNDATION`
- Course Weight：`M`
- Detailed Outline Gate：`NOT_STARTED`

> 本文件不是 Detailed Outline，更不是正文。它只固定未来论证顺序、读者问题、候选图与停止线；所有核心结论均等待 A2 Evidence。

## Intended Reader Change

从“应用把 prompt 发给模型，模型返回字符串”，转变为能够按 Application、Client / SDK、Provider API Contract、Structured Input、Tokenized Context、Model Generation、Structured Response 与 Application Handling 检查一次调用。

## Candidate Teaching Spine

1. 从一段看似简单的 C# 调用提出问题：代码究竟直接调用了什么？
2. 先拆 Model / Provider / API / SDK / Application，不从某家 API 字段开场。
3. 建立一次 Single Model Call 的候选请求—响应工程模型，并标出公开边界与不可推测区域。
4. 把 Messages / Input 拆成结构化请求表达，优先纠正 `Messages = Memory`。
5. 用 Token / Context Window 建立输入输出规模与限制的最低直觉，不进入算法或预算优化。
6. 把 Response Content、Usage、Finish Metadata 与 Failure Layer 分开。
7. 用最小 C# 示例落地抽象，并明确 Provider-specific contract。
8. 用 Streaming 做地图级定位，再把正式机制交给 Article 04。

## Planned Sections

### Opening｜“调用模型”这句话压缩了哪些层

- Reader Question：一行 SDK 调用是直接调用 Model，还是通过 Provider API Contract 请求模型能力？
- Evidence Needed：`01-C01`、`01-C10`
- Boundary：不开 Transformer / Training / SDK 安装教程。

### Section 1｜先把 Model、Provider、API、SDK、Application 拆开

- Reader Question：五个对象分别承担什么，哪些命名依 Provider 而变？
- Evidence Needed：`01-C01`、`01-C09`、`01-C10`
- Boundary：Adapter / Gateway 的正式设计留给 Article 04。

### Section 2｜一次请求—响应的最小工程链

- Reader Question：从 Application 可观察边界看，一次调用有哪些阶段？
- Candidate Figure：`Application -> SDK / HTTP -> Provider API -> Request -> Model -> Response -> Application Handling`
- Evidence Needed：`01-C02`、`01-C07`
- Boundary：图只表示课程工程职责，不画 Provider 未公开内部执行架构。

### Section 3｜Messages 是请求输入，不是长期 Memory

- Reader Question：message、role、content、history 与 model context 有什么关系？
- Evidence Needed：`01-C03`、`01-C04`
- Boundary：Prompt 设计留给 02；Context Engineering 留给 12—13；Memory 留给 14—15。

### Section 4｜Token 与 Context Window 的最低工程含义

- Reader Question：为什么字符数、文件大小和 Messages 数量不能直接替代 Token / Context Window？
- Evidence Needed：`01-C05`、`01-C06`
- Boundary：不讲 BPE / tokenizer 数学；Cost / Latency 优化留给 20。

### Section 5｜Response 不只是字符串

- Reader Question：generated content、usage、finish metadata、status / error 应如何分层？
- Evidence Needed：`01-C07`、`01-C11`
- Boundary：Structured Output contract 留给 03；Error / Retry 留给 04；完整 Failure Taxonomy 留给 21。

### Section 6｜用最小 C# 示例把层次落回代码

- Reader Question：哪些代码属于 Application，哪些只是 SDK convenience API，哪些字段属于 Provider Contract？
- Evidence Needed：`01-C12` 及被选 Provider 的正式 Evidence Cards
- Candidate Examples：最小调用、Messages、Usage；是否合并待 A2 / Detailed Outline 决定。
- Boundary：不做 SDK 功能巡礼，不实现 Lab。

### Section 7｜Streaming 只做位置感

- Reader Question：Streaming 主要改变 Application 怎样接收输出，还是改变模型能力？
- Evidence Needed：`01-C08`
- Boundary：不讨论 hidden reasoning；正式 Streaming lifecycle 留给 Article 04。

### Closing｜从单次调用走向 Prompt Contract

- Reader Question：理解调用边界以后，下一步为什么是 Prompt Engineering？
- Bridge：Article 02 将把 request / input 中的 Prompt 从“存在”推进到“怎样形成任务合同”。
- Boundary：不提前回答 Article 02。

## Figure Candidates

1. `Figure 1｜Single Model Call 请求—响应工程模型`：A1 只记录候选链路，A2 后确认字段和边界。
2. `Figure 2｜Messages、Context 与 Memory 的非等价关系`：是否需要取决于 `01-C03 / 01-C06` 的证据和 Detailed Outline。

## Learning Check Placement

未来至少覆盖：SDK / API 边界、Messages / Memory、Context Window / 字符数、Streaming、HTTP 429 / Wrong Answer。A1 不写参考答案。

## A1 Stop Line

- 不填写 Core Thesis 的确定性答案。
- 不绑定 Evidence ID，因为尚未创建 Evidence Cards。
- 不创建 `draft.md`。
- 不进入 Detailed Outline Gate。
