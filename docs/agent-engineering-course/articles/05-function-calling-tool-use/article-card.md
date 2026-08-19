# Article Card 05｜Function Calling 与 Tool Use：模型如何表达行动意图

> 来源基线：`docs/agent-engineering-series-plan.md` 与 `docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 05 frozen section。本文件只机械实例化既有课程职责，不预设 Research / Evidence 结论。

## 1. Positioning

- Part：`Part II｜从模型到 Agent`
- Type：`基础机制篇 / Agent 能力起点篇`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`

本篇区分“模型建议调用”与“宿主实际执行”。

## 2. Why Now

Article 03 让输出可被程序解析，Article 04 让模型调用的 Provider 差异有明确归属。下一步的问题是：模型如何从生成答案转向表达对外部能力的调用意图。

## 3. Learning Questions

1. Function Calling 是协议还是执行系统？
2. Tool Schema 怎样影响选择和参数？
3. Tool Call、Tool Result 与普通消息有什么关系？
4. 模型可以请求不存在或不允许的 Tool 吗？
5. Tool Use 为什么还不是完整 Agent？

## 4. Prerequisites

- Article 03：Structured Output、Parse / Schema / DTO / Domain Validation。
- Article 04：Model Adapter、Provider streaming / terminal / error boundary。

## 5. Candidate Core Concepts

Function Calling、Tool Use、Tool Schema、Tool Choice、Tool Call ID、Arguments、Tool Result。

这些词只是 Research 对象；跨 Provider 名称、消息时序、schema subset、parallel / sequential semantics 与安全措辞由 current Evidence Gate 决定。

## 6. Candidate Mental Model

```text
Available Tool Schemas
→ Model emits Tool Call Intent
→ Host decides whether / how to execute
→ Tool Result returns to model
```

这是 canonical 的教学候选，不是已验证的行业统一协议，也不表示 Tool Call 已执行、已授权或已产生可信 Evidence。

## 7. Canonical Content Spine

1. 从结构化结果到行动意图：区分 Tool Call 与业务 Structured Result，使用消息时序图。
2. Tool Schema 与选择：名称、描述、参数和枚举如何影响模型选择；候选材料为两版 Calculator Schema 对照。
3. Host 的决定权：模型只提出 intent；注册、权限与执行仍在宿主；候选负例为伪造 `deleteFile` call。
4. Tool Result 回注：Call ID、错误、结果内容与下一次请求；原始 Result 仍需 Runtime 处理。
5. 为什么还不是 Agent：一次 Tool Use 可以属于普通应用；持续 Loop 与状态尚未出现，并引出 Article 06。

## 8. Engineering Example Boundary

本篇不设独立 Lab。Calculator 示例只验证 Tool Call intent 的表达边界，不直接执行副作用；是否需要实际 Provider roundtrip、固定本地 fixture 或只使用 official trace，由 Researcher 按 Evidence requirement 判定并保留真实状态。

## 9. Relation to DSH

Article 35 再把本篇回收到 Tool Registry 与 Tool Pipeline 的模型侧入口；本篇不读取 DSH 源码。

## 10. Relation to BuildPilot

本篇只建立“模型可以请求 parse / read / search，但 Harness 保留执行权”的前置边界；不设计或实现 BuildPilot Runtime。

## 11. Evidence Requirements

- current official Function Calling / Tool Use documentation；
- Tool Schema fixture；
- message trace；
- Provider-specific fields、tool choice、call ID、arguments 与 result-return semantics 的 version scope；
- counter-evidence，防止把 official happy path 外推为 Tool Runtime / Agent Loop guarantee。

## 12. Confusion Risks

- `Function Calling != Tool Runtime`
- `Tool Call != Executed`
- `Tool Result != Evidence`
- `Tool Use != Agent Loop`
- `Schema Valid Arguments != Authorized Action`

## 13. Non-scope

- 权限、审批、超时、取消、重试与副作用治理。
- 完整 Tool Runtime 的 Validate / Policy / Execute / Result / Trace pipeline（Article 06）。
- MCP Transport（Article 07）。
- 多 Step、Agent Loop、Planning、Workflow 与 long-running state（Article 08—11）。
- DSH source verification 或 BuildPilot implementation。

## 14. Learning Check Candidates

1. 模型生成 `deleteFile` Tool Call，文件是否已经删除？
2. Tool 参数符合 Schema，是否意味着允许执行？
3. 一次查天气并回答的应用一定是 Agent 吗？

## 15. Weight

`M（Standard Core Lesson）`。围绕一个完整问题建立标准知识单元，控制边界，不扩展成 Tool Runtime 或 Agent Loop 专题大全。

## 16. Concept Maturity

`Mechanism`。正式解释 Function Calling / Tool Use 核心对象如何交互并产生可观察结果；权限、运行与持续循环留给后续文章。

## 17. Job Competency Mapping

候选能力包括：读取 Provider tool contract、审查 model-visible schema、追踪 tool call / result correlation、区分 intent / validation / authorization / execution，以及识别一次 Tool Use 与 Agent Loop 的边界。是否真正覆盖由 Evidence、Outline、Draft 与 Reviewer Gate 验证。
