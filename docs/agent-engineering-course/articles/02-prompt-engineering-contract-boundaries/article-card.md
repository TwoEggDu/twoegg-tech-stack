# Article Card 02｜Prompt Engineering：任务合同、角色、示例与边界

> 来源基线：`docs/agent-engineering-series-plan.md` 与 `docs/agent-engineering-course-plan-v3.1-review.md` 的 Article 02 结构。本文件只实例化既有课程字段，不预设 Research / Evidence 结论。

## 1. Positioning

- Part：`Part I｜从 LLM 到可编程模型`
- Type：`原理篇`
- Weight：`M（Standard Core Lesson）`
- Optional：`No`
- Mode：`NORMAL_ARTICLE`
- Required Lab：`NONE`

本篇位于 Article 01 的 Model API / Messages / Role / Token 之后、Article 03 Structured Output 之前。研究职责是把“怎样写 Prompt”重新定义为可审查的任务表达问题，并明确 Prompt 在 Agent Engineering 中不负责哪些运行时能力。

## 2. Why Now

Article 01 已建立模型只能依据当前请求中可见输入生成结果。下一步需要研究怎样表达目标、约束、输入、示例、输出要求与失败语义，才能为 Article 03 的机器可消费输出合同和后续 Tool / Agent 机制提供前置。

## 3. Reader Promise

完成本篇后，读者应能审查一份任务 Prompt 的合同元素与分层位置，识别 Few-shot、动态事实、Prompt Injection、权限、状态和事实校验之间的边界，并知道 Prompt 变更为什么需要版本与固定 Fixture，而不是只凭“看起来更好”判断。

## 4. Learning Questions

1. Prompt Engineering 真正解决什么问题？
2. System / Developer / User / Dynamic Input 在不同 Provider contract 中怎样表达，哪些职责可以抽象、哪些必须按 Provider 核对？
3. Few-shot 示例为什么可能同时帮助格式表达并携带旧假设或污染？
4. Prompt 为什么不能代替权限、状态持久化、当前事实校验与 Eval？
5. Prompt ID、模板变量、变更原因和固定 Fixture 怎样形成最低版本化与测试边界？

## 5. Prerequisites

- Article 01 已发布。
- 读者理解 Messages、Role、Token、Context Window 与 Provider-specific API contract 的最低含义。
- 不要求理解 Structured Output、Tool Runtime、Context Engineering、RAG、Memory 或 Agent Loop。

## 6. Candidate Core Concepts

Instruction、Goal、Constraint、Input Delimiter、Stable Instruction、Dynamic Input、Few-shot、Output Requirement、Failure Semantics、Prompt Version、Prompt Injection。

这些词当前只是 Research 对象；定义、Provider 差异与措辞强度由 Evidence Gate 决定。

## 7. Candidate Mental Model

```text
Goal + Constraints + Inputs + Examples + Output Requirement + Failure Semantics
→ Prompt / Request Contract
```

这是课程结构基线提供的研究候选，不是已确认的行业统一 schema，也不表示所有 Provider 使用相同 role 或 instruction hierarchy。

## 8. Evidence Needs

- 当前模型 Provider 的官方 Prompt / Message / instruction 指南与 API contract。
- 至少一个跨 Provider counter-check，用于分离稳定职责与 Provider-specific role / instruction 表达。
- Prompt Injection 的权威安全资料，用于证明 Prompt 与 Policy / authorization boundary 的差异。
- Prompt 版本、固定输入与 evaluation / fixture 的权威或一手工程资料。
- 一个固定 Unity 日志摘要输入的 A/B teaching fixture；只观察合同完整性与输出格式，不宣称准确率提升。

## 9. Planned Examples

- Example A：模糊的 Unity 日志摘要请求与任务合同版本对照。
- Example B：Stable Instruction、User Goal 与 Dynamic Project Facts 的分层请求图。
- Example C：有 / 无 Few-shot 的对照，用于观察输出结构和旧假设风险。
- Example D：`Prompt says do not delete` 与真实 permission / approval boundary 的反模式对照。

是否保留全部示例、具体 Provider 表达与最终措辞由 Research / Evidence / Outline Gate 决定。

## 10. Relation to Adjacent Articles

- Article 01：提供 Messages、Role、Token 与 Context Window 前置。
- Article 03：正式建立 Structured Output、Schema、Parse / Validate / Repair；本篇只研究自然语言层的 Output Requirement。
- Article 12—13：正式建立 Context assembly、selection、compression、pollution 与 debugging；本篇不吞掉完整 Context 生命周期。
- Article 19：正式建立 Permission、Approval、Human-in-the-loop 与 Sandbox；本篇只建立 Prompt 不是安全边界。
- Article 22：正式建立 Eval / Golden Dataset / Regression；本篇只引入 Prompt 版本进入固定 Fixture 的最低接口。
- Article 32：再把本篇映射到 DSH System Prompt Assembly 与多来源 Section。

## 11. Relation to BuildPilot

只研究未来 Harness Identity、Deployment Persona、任务 Prompt 与动态项目事实为什么需要分层；不设计或实现 BuildPilot Runtime。

## 12. Confusion Risks

- `Prompt != Context`
- `System Prompt != Policy / Authorization`
- `Few-shot != Knowledge Base`
- `Output Requirement != Structured Output validation`
- `Prompt wording != current facts`
- `Cleaner format != higher diagnostic accuracy`

## 13. Non-scope

- 提示词技巧大全或“魔法措辞”清单。
- Structured Output / JSON Schema / Domain Validation（Article 03）。
- 完整 Context 生命周期、packing、compression、pollution 与 reconstruction（Article 12—13）。
- RAG、Knowledge Base、Memory 与 Skill（Article 14—17）。
- Tool permission、approval、sandbox 与执行策略（Article 19）。
- 完整 Eval / Golden Dataset / regression system（Article 22）。
- DSH 源码或 BuildPilot Runtime。

## 14. Learning Check Candidates

1. 把“不要删除文件”写进 System Prompt，是否已经建立安全边界？
2. 每天变化的项目分支与构建号，应该固化进长期指令吗？
3. Prompt A 输出更整齐，是否已经证明诊断更正确？
4. 面对陌生 Provider，哪些 role / instruction 事实必须查当前 API contract？

## 15. Job Competency Mapping

本篇计划覆盖：Prompt contract review、instruction / input boundary、cross-provider contract reading、injection / permission boundary、Prompt versioning 与 fixed-fixture evaluation awareness。是否真正覆盖由 Outline、Draft 与 Reviewer Gate 验证。
