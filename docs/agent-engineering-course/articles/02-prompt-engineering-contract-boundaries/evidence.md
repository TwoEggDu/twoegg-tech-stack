# Article 02 Evidence Register

- Evidence Status：`PARTIAL`
- Evidence Gate：`PASS_RECOMMENDED`
- Claim Count：`9`
- Claim Summary：`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Evidence Card Count：`9`
- Retrieved / Verified At：`2026-08-19（Asia/Shanghai）`
- Provider Strategy：`OpenAI primary + Anthropic counter-check`
- Security Strategy：`OWASP authority + OpenAI provider guidance`
- Google Counter-check：`NOT_REQUIRED`
- Required Lab：`NONE`
- A/B Fixture：`NOT_EXECUTED`

## Claim Register

| Claim ID | 可进入 Outline 的收窄主张 | Status | Evidence |
|---|---|---|---|
| `02-C01` | 本课程把 Prompt Engineering 定义为任务表达与审查问题；使用 Goal、Constraints、Inputs、Examples、Output Requirements、Failure Semantics 六项检查职责，但不把它宣称为行业统一 schema 或每次请求的强制模板。 | `CONFIRMED` | `02-E01` |
| `02-C02` | 稳定应用指令、当次用户目标与动态输入应分层；职责可迁移，字段、承载位置和生命周期必须按 Provider contract 映射。 | `CONFIRMED` | `02-E02` |
| `02-C03` | role 集合与 instruction priority 是 Provider、模型与版本敏感 contract；不能建立跨 Provider 固定 role enum 或完整 hierarchy。 | `CONFIRMED` | `02-E03` |
| `02-C04` | few-shot 可示范格式、语气、结构与任务模式；示例应 relevant、diverse、structured。现有证据不支持量化通用 token / staleness / bias 影响，也不支持“所有任务准确率提升”。 | `PARTIAL` | `02-E04` |
| `02-C05` | delimiter / data marking 能改善 instructions、context、examples 与 variable input 的可辨识性，但只是 defense-in-depth 的一层，不提供 Prompt Injection immunity。 | `CONFIRMED` | `02-E05` |
| `02-C06` | Prompt 可以表达期望行为，但不授予真实权限，也不替代 authorization、least privilege、approval、状态观测或当前事实核验。 | `CONFIRMED` | `02-E06` |
| `02-C07` | 自然语言 Output Requirement 不等于 schema adherence、类型校验或领域正确性；Structured Output 的完整机制留给 Article 03。 | `CONFIRMED` | `02-E07` |
| `02-C08` | 最低 Prompt change contract 是 code/config version、显式变量、变更原因、代表性 fixtures、成功标准和判定记录；Prompt ID 与某个托管 Evals 平台都不是可迁移必需项。 | `CONFIRMED` | `02-E08` |
| `02-C09` | `02-FX01` 只是 `NOT_EXECUTED` 课程 A/B 提案；未来单次受控执行也只能产生有限观察，不能自动证明 accuracy 或生产收益。 | `PROPOSAL` | `02-E09` |

## Source Manifest

所有外部来源均于 `2026-08-19（Asia/Shanghai）` 实时打开核对。以下版本描述只表示当日公开文档面，不保证未来字段、模型支持矩阵或产品生命周期不变。

| Source ID | Primary Source | Used For | Version / Change Sensitivity |
|---|---|---|---|
| `OA-PE` | [OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering) | Prompt 职责、developer / user、prompt organization、few-shot、code versioning 与 representative fixtures | OpenAI 当前 Responses / prompting 文档；role 与 deprecation timeline 版本敏感 |
| `OA-MG` | [OpenAI Model guidance](https://developers.openai.com/api/docs/guides/latest-model) | Goal、context、constraints、required evidence、success criteria、output format 与 same-eval comparison | GPT-5.6 当前指导；不作为全 Provider 普遍模板 |
| `OA-EVAL` | [OpenAI Evaluation best practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices) | Objective、dataset、metrics、continuous evaluation、典型 / 边界 / adversarial cases、vibe-based eval 反模式 | 方法责任可迁移；托管 Evals platform 生命周期版本敏感 |
| `OA-SO` | [OpenAI Structured model outputs](https://developers.openai.com/api/docs/guides/structured-outputs) | 自然语言格式要求、JSON mode、schema adherence 与内容错误边界 | model / schema support 版本敏感；Article 03 再核对 |
| `OA-SAFE` | [OpenAI Safety in building agents](https://developers.openai.com/api/docs/guides/agent-builder-safety) | approvals、guardrails、structured data flow、isolation 与 residual injection risk | Agent Builder 产品表达版本敏感；只提取 security responsibility |
| `AN-PROMPT` | [Anthropic Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices) | clear instructions、context、examples、XML tags 与 system role guidance | 当前 Claude model guidance；示例数量与性能描述不外推到其他 Provider |
| `AN-MSG` | [Anthropic Create a Message API reference](https://platform.claude.com/docs/en/api/messages/create) | 顶层 `system`、Messages request、user / assistant 基线 | 当前 API reference；需与 model-specific capability pages 一起读 |
| `AN-MID` | [Anthropic Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages) | 当前部分模型的 mid-conversation `system`、位置约束、application-observed state 与 untrusted-content boundary | 支持矩阵与 placement rules 高度版本敏感 |
| `OWASP-PI` | [OWASP LLM Prompt Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html) | Prompt Injection、clear separation、input/output validation、permission check、least privilege 与 HITL | 安全实践持续演进；不把示例代码当完整生产实现 |

## Evidence Cards

### Evidence 02-E01｜Prompt Engineering 是任务表达与审查问题

- Claim ID：`02-C01`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC_SYNTHESIS + COURSE_ABSTRACTION`
- Source：[OpenAI Model guidance](https://developers.openai.com/api/docs/guides/latest-model)；[OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)；[Anthropic Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI GPT-5.6 / Responses 当前 guidance，Anthropic current Claude guidance
- Observation：OpenAI 当前 model guidance 要求 outcome-focused prompt 写清 goal、relevant context、constraints、required evidence、success criteria 与 output format；Prompt Engineering 页面建议用 Identity、Instructions、Examples、Context 组织 developer message。Anthropic 要求清晰、明确地说明 desired output、constraints 与必要 context。
- Counter-evidence：三份官方指南没有共同声明 Goal、Constraints、Inputs、Examples、Output Requirements、Failure Semantics 是行业 schema；不同模型还可能需要不同 prompting 方法。
- Interpretation：可以把“把任务条件显式化并用成功标准审查”确认为稳定工程职责；六项结构是课程把多家职责压缩成的 review checklist。
- Proves：本篇可把 Prompt Engineering 从“魔法措辞”重述为可检查的任务表达问题。
- Does Not Prove：不证明六项齐全就能保证正确答案，也不证明所有请求必须使用固定顺序或字段名。
- Limitations / Course Usage：Failure Semantics 是课程为证据不足 / 输入缺失场景增加的工程责任，不宣称由某家 Provider 发明；用于问题空间与抽象模型。

### Evidence 02-E02｜Stable Instruction、User Goal 与 Dynamic Input 应分层

- Claim ID：`02-C02`
- Evidence Status / Class：`CONFIRMED / CROSS_PROVIDER_OFFICIAL_DOC`
- Source：[OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)；[Anthropic Create a Message](https://platform.claude.com/docs/en/api/messages/create)；[Anthropic Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI current prompt-in-code guidance，Anthropic current Messages / model-specific guidance
- Observation：OpenAI 当前把 developer 与 user 类比为函数定义与参数，并建议 prompt builder 用 typed arguments / schemas 管理 customer data、files、task options 等动态值。Anthropic 把起始 system instruction 放在顶层 `system`，用户输入放在 messages；它的 mid-conversation guidance 又把文件变化、工具变化、剩余预算等定义为 Application 先观察到、再提供的动态事实。
- Counter-evidence：Anthropic 当前部分模型允许追加 system-level state update；“稳定”与“动态”并不等于永远绑定某一个固定 role。
- Interpretation：稳定规则、当次目标、动态事实是审查职责分层，不是跨 Provider wire mapping。
- Proves：应用应显式管理动态变量，并按当前 Provider contract 决定放入 `instructions`、developer、system、user 或其他字段。
- Does Not Prove：不证明字符串模板本身可信，也不证明所有动态数据都应提升为 system / developer authority。
- Limitations / Course Usage：可信 application state 与不可信外部内容必须继续分开；完整 Context assembly 留给 Article 12—13。

### Evidence 02-E03｜Role / Instruction Hierarchy 必须按当前 Contract 核对

- Claim ID：`02-C03`
- Evidence Status / Class：`CONFIRMED / VERSION_SENSITIVE_CROSS_PROVIDER_DOC`
- Source：[OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)；[Anthropic Create a Message](https://platform.claude.com/docs/en/api/messages/create)；[Anthropic Using the Messages API](https://platform.claude.com/docs/en/build-with-claude/working-with-messages)；[Anthropic Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI current role priority；Anthropic current generic reference + model-specific support matrix
- Observation：OpenAI 当前说明 developer instruction 优先于 user。Anthropic generic Messages reference 仍说明起始 system prompt 使用顶层 `system`；当前 feature guidance 同时说明 Claude Fable 5、Mythos 5、Opus 4.8、Opus 5、Sonnet 5 等支持受 placement rules 约束的 mid-conversation `role: system`，且后来的 system instruction 可覆盖较早 system instruction。
- Counter-evidence：只读 Anthropic generic reference 会得出“messages 没有 system role”的过强结论；只读 model-specific feature page又会错误外推到所有模型与所有位置。
- Interpretation：role set、priority、placement 和 lifecycle 必须以 Provider + API + model + version 为联合 scope。
- Proves：课程不能发布跨 Provider 固定 role enum 或完整统一 hierarchy；面对陌生 Provider 必须读 current contract。
- Does Not Prove：不否认“应用指令与用户输入需要区分”这一稳定职责，也不证明任一 Provider 的层级更安全。
- Limitations / Course Usage：这是高版本敏感 Claim；正文必须保留核对日期与 model-specific 例外，用于 Provider boundary 表。

### Evidence 02-E04｜Few-shot 的帮助范围与未证明边界

- Claim ID：`02-C04`
- Evidence Status / Class：`PARTIAL / PROVIDER_OFFICIAL_GUIDANCE`
- Source：[OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)；[Anthropic Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices)；[OpenAI Evaluation best practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI / Anthropic current prompting guidance
- Observation：OpenAI 把 few-shot 描述为在 Prompt 中放入少量 input / output examples，让模型拾取任务模式，并建议覆盖多样输入。Anthropic 把 examples 用于 steer output format、tone、structure，并要求 relevant、diverse、structured；其多样性建议明确用于避免拾取 unintended patterns。
- Counter-evidence：OpenAI 同时说明不同模型甚至同一家族不同 snapshot 可能需要不同 Prompt，并要求用 tests / evals 验证。Anthropic 的准确率 / 一致性与示例数量建议是 Provider guidance，不构成跨模型普遍定律。
- Interpretation：示例的模式表达职责可以确认；对 accuracy、token cost、staleness 与 bias 的影响必须在具体 workload 上测量，不能从指南直接量化。
- Proves：正文可以说“few-shot 可示范格式、语气、结构与任务模式；示例应相关、多样、结构清楚”。
- Does Not Prove：不证明 few-shot 总能提高 accuracy，不证明越多越好，也不证明示例是 Knowledge Base 或 current fact source。
- Limitations / Course Usage：正文不得给通用提升百分比，不量化 token / staleness / bias；把陈旧或单一分布示例写成工程风险而非已测结论。

### Evidence 02-E05｜Delimiter 改善分离，但不是 Injection Immunity

- Claim ID：`02-C05`
- Evidence Status / Class：`CONFIRMED / PROVIDER_GUIDANCE + SECURITY_AUTHORITY`
- Source：[Anthropic Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices)；[OWASP LLM Prompt Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html)；[OpenAI Safety in building agents](https://developers.openai.com/api/docs/guides/agent-builder-safety)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；当前 prompting / agent-security guidance
- Observation：Anthropic 说明 XML tags 能帮助模型区分 instructions、context、examples 与 variable inputs。OWASP 把 structured prompts with clear separation 列为防御层，同时要求 input sanitization、output validation、HITL 与 least privilege。OpenAI 说明 structured outputs 与 isolation 能显著降低、但不能完全移除 injection risk。
- Counter-evidence：OWASP 明确列出 direct、indirect、encoded、multimodal 与 persistent injection；清楚标签并不能覆盖这些攻击面。
- Interpretation：delimiter 是可读性与 defense-in-depth 控制，不是 authorization 或 security boundary。
- Proves：正文可同时讲“标签减少混淆”与“标签不提供免疫”。
- Does Not Prove：不证明某一种 XML / Markdown 格式普遍最佳，也不证明过滤器或 guardrail 本身不可绕过。
- Limitations / Course Usage：不在本篇设计完整 security pipeline；Article 19 再讲 Permission / Approval / Sandbox。

### Evidence 02-E06｜Prompt 不替代 Permission、State 或 Fact Verification

- Claim ID：`02-C06`
- Evidence Status / Class：`CONFIRMED / SECURITY_AUTHORITY + PROVIDER_DOC`
- Source：[OWASP LLM Prompt Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html)；[OpenAI Safety in building agents](https://developers.openai.com/api/docs/guides/agent-builder-safety)；[Anthropic Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages)；[OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；当前 LLM application / agent security guidance
- Observation：OWASP 要求 tool calls 对照 user permissions 与 session context 校验、执行 tool-specific parameter validation 并按 least privilege 限制 scope。OpenAI 要求保留 tool approvals、guardrails 与隔离。Anthropic 把 files changed、auto-approve setting、available tools、budget 等描述为 Application 观察到后再注入的 state change，并禁止把 raw tool output / retrieved document 直接提升为 system authority。OpenAI 则把 Context 描述为调用方提供的额外相关信息。
- Counter-evidence：安全 Prompt 与清晰 policy examples 能改善模型行为，但 Provider 文档仍要求 Prompt 外的 approval、validation 与 permission control。
- Interpretation：Prompt 是行为表达层；authorization、credential / filesystem scope、application state、retrieval 与 fact validation 是外部执行 / 数据责任。
- Proves：`Prompt says do not delete` 不等于删除权限被撤销；未观察或未检索的 current fact 不能靠措辞得到验证。
- Does Not Prove：不否认 Prompt 可以成为 defense-in-depth 的一层，也不在本篇定义完整 state / retrieval / approval architecture。
- Limitations / Course Usage：正文只建立边界；Context / RAG / Memory 留给 12—17，Permission / Approval / Sandbox 留给 19。

### Evidence 02-E07｜Output Requirement 不等于 Structured Output Validation

- Claim ID：`02-C07`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_PRODUCT_DOC`
- Source：[OpenAI Structured model outputs](https://developers.openai.com/api/docs/guides/structured-outputs)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI current Structured Outputs / JSON mode contract
- Observation：OpenAI 当前说明 JSON mode 只保证 valid JSON，不保证指定 schema；Structured Outputs 才提供 schema adherence。相同文档还明确指出 Structured Outputs 仍可能包含内容错误，并要求处理 refusal 等边界。
- Counter-evidence：自然语言“请按 JSON 返回”确实可能引导展示格式，但该行为不等于 API-level schema guarantee。
- Interpretation：Output Requirement 属于自然语言任务合同；schema、parse、validation、repair 与 domain correctness 是后续层。
- Proves：正文可安全写 `Output Requirement != Structured Output validation`，并桥接 Article 03。
- Does Not Prove：不证明 Structured Outputs 能保证事实或领域值正确，也不证明所有 Provider 有相同 schema contract。
- Limitations / Course Usage：本篇不展示完整 JSON Schema 或 repair loop；Article 03 必须按当时 current Provider docs 重新核对。

### Evidence 02-E08｜Prompt 变更需要版本、Fixture 与判定标准

- Claim ID：`02-C08`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_ENGINEERING_GUIDANCE`
- Source：[OpenAI Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)；[OpenAI Evaluation best practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；OpenAI current prompt-in-code 与 evaluation guidance
- Observation：OpenAI 当前建议把 production prompts 存在 application code，使用 typed inputs、code review、tests、representative fixtures 与 evaluation checks。Evaluation guidance 要求定义 objective / success criteria、收集 dataset、定义 metrics、run and compare、在每次 change 持续评价，并把“看起来可用”列为 anti-pattern；dataset 应包含 typical、edge、adversarial cases。
- Counter-evidence：Prompt ID 与托管 Evals object 都是产品实现，不是上述工程责任本身。OpenAI 当前 reusable prompt objects 计划于 `2026-11-30` 关闭；托管 Evals platform 计划于 `2026-10-31` 转只读、`2026-11-30` 关闭。
- Interpretation：稳定接口是可 review 的 prompt source + explicit variables + change reason + frozen fixtures + criteria + recorded outputs / judgments，而不是依赖某个 Provider object ID。
- Proves：Prompt 变更不能只凭“输出更顺眼”批准；必须在同一代表性 fixture 与明确标准下比较。
- Does Not Prove：不证明一个 fixture 就是完整 Eval，不规定必须使用 OpenAI Evals API，也不证明固定模型参数会消除非确定性。
- Limitations / Course Usage：本篇只建立最低 change / test interface；Golden Dataset、grader calibration、continuous eval system 留给 Article 22。

### Evidence 02-E09｜Unity A/B 只能作为未执行课程提案

- Claim ID：`02-C09`
- Evidence Status / Class：`PROPOSAL / DESIGN_ONLY / NOT_EXECUTED`
- Source：[Article 02 Research - A/B Teaching Fixture Design](research.md)；[OpenAI Evaluation best practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices)
- Retrieved / Version Scope：`2026-08-19（Asia/Shanghai）`；课程 fixture 冻结日；无 Provider / model / runtime version
- Observation：`02-FX01` 已冻结合成 Unity log、Prompt A、Prompt B 与有限检查项，但 `Provider / Model / Parameters = NOT_SELECTED`、`Runtime Observation = NONE`。OpenAI eval guidance要求 objective、dataset、metrics、comparison 与 typical / edge / adversarial coverage，并反对 vibe-based evaluation。
- Counter-evidence：没有执行输出、重复样本、边界样本、human labels 或 accuracy metric；即使 Prompt B 手写得更完整，也不是 observed behavior。
- Interpretation：该 fixture 只能教“怎样冻结输入与检查合同完整性”；未来一次执行至多产生 format / evidence-use observation，不能升级为 accuracy claim。
- Proves：`N/A`；这是课程设计提案，不是模型行为证据。
- Does Not Prove：不证明 Prompt B 比 Prompt A 更准确、更鲁棒、更安全或更适合生产，也不证明合同元素与 accuracy 存在因果关系。
- Limitations / Course Usage：必须在正文标 `NOT_EXECUTED`；任何 illustrative expected output 不得标 observed。完整 A/B、数据集和统计判定留给 Article 22。

## Evidence Gate Checklist

- [x] 9 个 Claim 均有正式 Evidence Card
- [x] 核心行为性 Claim 无 `BLOCKED`
- [x] `02-C04 PARTIAL` 已给出正文收窄措辞与禁止外推项
- [x] Provider-specific role / priority 与 stable responsibility 已分开
- [x] Anthropic 当前 mid-conversation system role 例外已按 model / placement / version 记录
- [x] Prompt Injection / permission boundary 有 OWASP 与 Provider 官方证据
- [x] 自然语言 Output Requirement 与 schema / domain validation 已分开
- [x] Prompt version / fixture / criteria 与 Provider Prompt ID 已分开
- [x] `02-FX01 = NOT_EXECUTED / PROPOSAL`，没有 accuracy 或 runtime 结论
- [x] Article 03 / 12—17 / 19 / 22 边界清楚

## Evidence Gate

- 核心 Claim：`7 CONFIRMED / 1 PARTIAL / 0 BLOCKED`
- 课程 Fixture：`1 PROPOSAL / NOT_EXECUTED`
- Required Lab：`NONE`
- Blocker：`NONE`
- Outcome：`PASS_RECOMMENDED`

`02-C04` 只有在正文保持“示范模式 + relevant / diverse / structured + 必须用具体 Eval 验证”的收窄措辞时才能进入 Outline。`02-C09` 只能以未执行课程提案出现。Researcher 建议 Master 将篇级 Lifecycle 作为 `EVIDENCE_READY` candidate 处理；Researcher 不直接写 global state。

## Stop Line

Evidence 交付到此停止。不创建或修改 Outline、Draft、Review、Published Content、`status.md`、`course-run-state.md` 或其他文件；不执行 A/B fixture；不 commit。
