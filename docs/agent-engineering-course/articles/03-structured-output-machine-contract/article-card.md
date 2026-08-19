# Article Card 03｜Structured Output：让模型输出成为机器可消费的合同

> 来源基线：`docs/agent-engineering-series-plan.md` 与 `docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 03 / Lab 01 canonical。本文档只实例化已冻结的课程字段，不预设 Research、Evidence 或 Lab 结论。

## 1. Positioning

- Part：`Part I｜从 LLM 到可编程模型`
- Type：`基础核心篇 / Lab Article`
- Weight：`L（Major Core Lesson）`
- Optional：`No`
- Mode：`LAB_ARTICLE`
- Required Lab：`Lab 01｜Structured Output`

本篇位于 Article 02 的 Prompt / Output Requirement 之后、Article 04 Model Adapter 之前。它负责把“希望模型怎样呈现”推进为程序可以解析、分层校验并明确接受、修复或失败的机器合同。

## 2. Why Now

Article 02 已能把目标、输入、约束、输出要求和失败语义写清，但程序仍可能只收到一段自由文本。后续 Tool Call、Action、Workflow State、Evidence 与 Eval 都需要明确字段与失败分类，因此本篇先研究 Structured Output 的最小可验证机制。

## 3. Reader Promise

完成本篇后，读者应能区分合法 JSON、Schema Valid、Domain Valid 与 Verified Result；能把 Candidate Output 放入 Parse → Schema Validate → Domain Validate → Accept / Repair / Fail 链路；并能说明 refusal、truncation、修复重试与证据不足为何不能混为一个错误。

## 4. Learning Questions

1. Structured Output 比“请输出 JSON”多了什么？
2. JSON Schema 能保证什么，不能保证什么？
3. Parse、Schema Validation 与 Domain Validation 怎样分层？
4. Refusal、截断与 repair retry 应怎样分类和停止？
5. 为什么 Structured Output 是 Tool Call、Evidence Contract 与 Eval 的前置，而不是它们的替代？

## 5. Prerequisites

- Article 01 已建立 Model Response 与 Provider contract 边界。
- Article 02 已建立 Output Requirement、Failure Semantics 与 Prompt 不替代机器校验的边界。
- 不要求读者已掌握 Tool Runtime、Agent Loop、Evidence Contract 或 BuildPilot final DTO。

## 6. Candidate Core Concepts

Structured Output、JSON Schema、Typed DTO、Parse、Schema Validation、Domain Validation、Refusal、Truncation、Repair、Accept / Fail。

这些词当前只是 Research 对象；Provider 差异、具体失败分类和可安全措辞由 Preliminary Evidence、Lab 01 与 Evidence Merge 决定。

## 7. Candidate Mental Model

```text
Candidate Output
→ Parse
→ Schema Validate
→ Domain Validate
→ Accept / Repair / Fail
```

这是 canonical 提供的教学候选，不是已验证的 Provider runtime trace，也不表示每一种失败都允许 repair。

## 8. Evidence Needs

- 至少一个当前 Provider 的 Structured Output 官方 API contract。
- JSON Schema 官方 specification / vocabulary 边界。
- C# / .NET JSON parsing、typed deserialization 与 validation 的当前官方表面。
- Lab 01 raw observations：合法、缺字段、类型错误、合法 JSON 但 domain-invalid、refusal / truncation / repair boundary。
- Expected 与 Observed 必须分开；行为性 Claim 在 Lab 完成前保持 `BLOCKED`。

## 9. Required Lab Route

- Lab ID：`Lab 01`
- Planned Path：`docs/agent-engineering-course/labs/lab-01-structured-output-validation/`
- Default Runtime：`C# / .NET`
- Local Runtime Candidate：`.NET SDK 10.0.301`
- Canonical Observation：Parse、Schema、Domain Validation
- Required Failure：合法 JSON 引用不存在的 ID，或等价的 schema-valid / domain-invalid fixture
- Instantiation Rule：只有 Researcher 完成 Preliminary Evidence、明确 Related Claim IDs 并冻结 Hypothesis / falsifier / inputs / acceptance criteria 后才能创建。

## 10. Planned Materials

- 自由文本诊断与 Typed DTO 对照。
- 简化 DiagnosisCandidate Schema 与错误样本。
- Parse / Schema / Domain 三层失败分类表。
- Refusal、truncation、repair / stop 的 fixture 集。
- Lab 01 raw output、validation error 与 reproduction record。

具体 Provider、schema、DTO 与 fixture 由 Researcher / Lab Design 冻结；WORKSPACE_INIT 不作决定。

## 11. Relation to Adjacent Articles

- Article 02：提供自然语言 Output Requirement 与 Failure Semantics 前置；本篇正式进入机器合同。
- Article 04：后续把 Provider streaming、error、retry 与 adapter normalization 接到当前 contract；本篇不设计完整 Gateway。
- Article 05—06：Tool Call 参数与 Tool Runtime validation 会复用 schema / domain boundary；本篇不执行工具。
- Article 18：Evidence Contract 会复用 typed result 与 domain validation；本篇不证明 Evidence 真实性。
- Article 22：Eval 会消费结构化结果与 failure labels；本篇不建立完整 evaluator。

## 12. Relation to DSH / BuildPilot

- DSH Article 33 / 35 会重新遇到 model step output、tool arguments 与 result validation；本篇不读取 DSH 源码。
- BuildPilot 未来的 Evidence、Hypothesis、DiagnosisResult 需要机器合同基础；本篇不冻结 BuildPilot final fields。

## 13. Confusion Risks

- `JSON text != Structured Output contract`
- `Parse Success != Schema Valid`
- `Schema Valid != Domain Valid`
- `Structured Result != Verified Result`
- `Repairable Syntax != Repairable Evidence Gap`
- `Refusal / Truncation != Domain Validation Error`

## 14. Non-scope

- Tool execution、Tool policy、Agent Loop 与 Workflow Runtime。
- 完整 Model Adapter / Gateway error taxonomy。
- Evidence 真伪判定与完整 Eval / Golden Dataset。
- DSH 源码级实现与 BuildPilot final DTO。
- 跨 Provider 性能、准确率、成本或可靠性普遍结论。

## 15. Learning Check Candidates

1. 输出合法但 Evidence ID 不存在，应该在哪一层失败？
2. 模型拒绝回答时，是否应强制修复成空 JSON？
3. 为什么 Enum 可以减少歧义，却不能证明状态真实？
4. 缺字段、类型错误、truncation 与 domain-invalid 是否应该共享同一种 retry？

## 16. Job Competency Mapping

计划覆盖：API contract reading、schema boundary、typed result design、failure taxonomy、C# validation pipeline、fixture / fault injection、raw observation 与 claim discipline。是否真正覆盖由 Lab、Outline、Draft 与 Reviewer Gate 验证。
