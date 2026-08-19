# Article Card 04｜Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异

> 来源基线：`docs/agent-engineering-series-plan.md` 与 `docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 04 canonical。本文档只实例化已冻结字段，不预设 Research 或 Evidence 结论。

## 1. Positioning

- Part：`Part I｜从 LLM 到可编程模型`
- Type：`基础工程篇 / Normal Article`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`

本篇位于 Article 03 Structured Output 之后、Article 05 Function Calling / Tool Use 之前。canonical 职责是把单一 API 调用包装为可替换、可观察的 Model Capability，使后续 Agent Loop 不绑定一家 SDK。

## 2. Why Now

Article 03 已建立结构化合同，但真实 Provider 在消息、Streaming、错误与 Usage 等表面存在差异。本篇需要先冻结这些差异应该在哪一层暴露或归一化，再进入模型表达行动意图的课程阶段。

## 3. Learning Questions

1. Model Adapter 应封装什么，不能吞掉什么？
2. Streaming 与最终 Structured Result 怎样共存？
3. 哪些模型错误可以 Retry？
4. LLM Gateway 与 Model Adapter 有什么差别？
5. Provider 切换为何不能只替换 URL？

## 4. Prerequisites

- Article 01：Model API、Messages、Token 与 Provider contract。
- Article 02：Prompt 任务合同与工程边界。
- Article 03：Structured Output 与机器可消费合同。

## 5. Candidate Core Concepts

Model Adapter、Gateway、Streaming Event、Finish Reason、Rate Limit、Transient Error、Retry Policy、Capability Negotiation。

这些术语当前只是 Research 对象；跨 Provider 的共同边界、具体字段和安全措辞必须由 current official evidence 决定。

## 6. Candidate Mental Model

```text
Domain Request
→ Model Adapter
→ Provider API / Stream
→ Normalized Events + Final Result + Usage
```

这是 canonical 提供的教学候选，不是某家 Provider 的真实 runtime trace，也不表示所有差异都应该被归一化。

## 7. Evidence Needs

- 至少两家 Provider 的当前官方 API contract。
- 当前 Streaming Event schema 与终止 / Usage 边界。
- 当前错误码、限流与 Retry 指南。
- 具体能力、字段和支持范围必须标注核对日期。
- 必须记录 counter-evidence、Provider-specific 差异及 proves / does-not-prove。

## 8. Canonical Content Boundary

- 研究 Provider 差异为何上溢，以及 Adapter 对请求、响应、错误、Usage 的边界。
- 研究增量文本、Tool 参数片段、Usage 与完成事件的处理责任。
- 区分传输重试、业务重试与不应自动重试的失败。
- 区分 Adapter 与集中路由、凭证、限流、审计的 Gateway。
- 不搭建 API Gateway 服务，不讲负载均衡和模型部署。
- 不把 Gateway 写成 Agent Runtime，也不提前展开 Article 05 的 Tool Use。

## 9. Engineering Example Boundary

本篇没有 Required Lab。canonical 允许使用 Fake Provider 作为教学示例，模拟增量事件、429 与截断；若未真实执行，必须明确标为 `PROPOSAL / NOT_EXECUTED`，不得写成 runtime evidence。

## 10. Downstream Boundary

- DeepSeek Harness：Article 31、33 再研究 Provider / Capability Seam 与 Agent Step 调用。
- BuildPilot：未来影响 `IModelAdapter`、Usage 归一化和模型路由；当前不要求实现 Gateway。
