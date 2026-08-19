# Article 02 Research

- Research Phase：`RESEARCH`
- Research Status：`COMPLETE`
- Lifecycle Candidate：`EVIDENCE_READY`（仅供 Master 更新 global state）
- Evidence Status：`PARTIAL`（`02-C04` 已收窄；无核心 `BLOCKED`）
- Evidence Gate Recommendation：`PASS`
- Research Window：`2026-08-19（Asia/Shanghai）`
- Provider Counter-check：OpenAI + Anthropic
- Google Counter-check：`NOT_REQUIRED`（两家当前 contract 已足以证明 Provider-specific 边界）
- Required Lab：`NONE`
- A/B Fixture：`COURSE_PROPOSAL / NOT_EXECUTED`

## Scope And Method

本篇研究的是：怎样把一次模型请求写成可审查、可修改、可复测的任务合同，以及 Prompt 在 Agent Engineering 中不负责什么。它不把任何一家 Provider 的 message 字段、role 集合或 instruction hierarchy 包装成行业统一标准。

来源面保持最小闭合：OpenAI 当前官方 Prompt Engineering、Model Guidance、Structured Outputs 与 Evaluation Best Practices；Anthropic 当前官方 Prompting Best Practices、Messages API 与 mid-conversation system message guidance；OWASP 与 OpenAI 官方 Agent Safety 用于 Prompt Injection、authorization、least privilege 与 approval 边界。所有外部来源均于 `2026-08-19（Asia/Shanghai）` 实时核对。

Google 未加入 source manifest。OpenAI 与 Anthropic 已提供足够的跨 Provider 反例；继续加入第三家只会扩大检索面，不改变本篇可安全结论。

## Research Question Answers

| RQ | Status | Answer | Claim / Evidence |
|---|---|---|---|
| `RQ-01` | `ANSWERED` | Prompt Engineering 的可迁移职责不是寻找“万能措辞”，而是把目标、约束、输入、可选示例、输出要求与失败语义写成可检查的任务表达，并预先定义成功标准。六项是课程 review checklist，不是行业统一 schema，也不要求每次请求机械填满全部字段。 | `02-C01` / `02-E01` |
| `RQ-02` | `ANSWERED` | 工程上应区分稳定应用指令、当次用户目标与动态输入。OpenAI 当前把 developer / user 类比为函数定义与参数，并建议用 typed arguments 管理动态值；Anthropic 的 system 与 messages 承载方式不同。因此责任分层可迁移，实际字段必须按 Provider contract 映射。 | `02-C02` / `02-E02` |
| `RQ-03` | `ANSWERED` | role 与 instruction priority 是 Provider、模型和版本敏感的 contract。OpenAI 当前 developer 指令优先于 user；Anthropic 通用 Messages reference 使用顶层 `system`，而当前部分模型又支持有严格位置约束的 mid-conversation `system` message。不能发布固定跨 Provider role enum 或完整优先级阶梯。 | `02-C03` / `02-E03` |
| `RQ-04` | `PARTIAL` | few-shot 示例可以示范输出格式、语气、结构与任务模式；示例应贴近用例、覆盖差异并保持清晰结构。官方资料也提示缺少多样性会让模型拾取非预期模式。现有最小来源不足以量化通用 token 成本、staleness 或 bias，也不能证明所有任务准确率都会提升，正文必须采用收窄措辞。 | `02-C04` / `02-E04` |
| `RQ-05` | `ANSWERED` | XML / Markdown 标题、标签与其他定界方式能帮助模型区分 instructions、context、examples 与 variable input；OWASP 也把 clear separation 列为防御层之一。但 Provider 与 OWASP 均要求叠加输入验证、输出验证、最小权限、审批和隔离，因此 delimiter 不是 Prompt Injection immunity。 | `02-C05` / `02-E05` |
| `RQ-06` | `ANSWERED` | Prompt 能表达期望行为，却不能授予真实系统权限或替代 authorization、approval、least privilege 与 fail-closed validation。动态状态与当前事实也必须由 Application 观察、检索或校验后提供；把一句“不要删除”写进 Prompt 不会改变工具凭据、文件权限或后端 policy。 | `02-C06` / `02-E06` |
| `RQ-07` | `ANSWERED` | 自然语言 Output Requirement 可以引导展示格式，但不等于 schema adherence、类型校验或领域正确性。OpenAI 当前明确区分普通提示 / JSON mode 与 Structured Outputs；即使 schema adherence 成立，Structured Outputs 仍可能包含内容错误。完整 Parse / Validate / Repair 留给 Article 03。 | `02-C07` / `02-E07` |
| `RQ-08` | `ANSWERED` | 最低 Prompt change contract 应保存可 review 的 prompt builder / template 版本、显式动态变量、变更原因、代表性 fixtures、成功标准与判定记录。Prompt ID 不是可迁移必需项；OpenAI 当前正在弃用 reusable prompt objects 和托管 Evals platform，因此课程只保留 code/config version + fixtures + criteria 这一稳定责任。 | `02-C08` / `02-E08` |
| `RQ-09` | `ANSWERED_AS_PROPOSAL` | 固定 Unity 日志 A/B 只是课程教学 fixture。若未来锁定同一模型快照、参数与输入后执行，可观察字段遵循、证据引用和 UNKNOWN 标记；本轮 `NOT_EXECUTED`，不能产生 observed result，更不能证明 accuracy、鲁棒性、因果优越性或生产收益。 | `02-C09` / `02-E09` |

## Stable Abstraction And Provider Mapping

### 课程任务合同候选

| Element | Review Question | Boundary |
|---|---|---|
| Goal | 这次调用到底要完成什么？ | 不包含权限授予 |
| Constraints | 范围、禁止项、证据与质量约束是什么？ | 不是外部 policy enforcement |
| Inputs | 本次可用数据、来源与可信边界是什么？ | Prompt 不吞掉完整 Context 生命周期；留给 12—13 |
| Examples | 是否需要输入 / 输出示范来表达模式？ | Few-shot 不等于 Knowledge Base；留给 16 |
| Output Requirements | 调用方需要什么呈现形状？ | 不等于 Structured Output validation；留给 03 |
| Failure Semantics | 输入不足、证据不足或无法完成时怎样显式返回？ | 不替代异常处理、审批或审计 |

这张表是课程用于 Prompt review 的抽象模型。OpenAI 当前常见组织包含 Identity、Instructions、Examples、Context，并在 GPT-5.6 guidance 中要求写清 goal、context、constraints、required evidence、success criteria 与 output format；Anthropic 使用另一套组织建议。证据支持这些职责需要被显式表达，不支持把六项宣称为 Provider wire schema、行业标准或普遍最优模板。

### Provider counter-check

| Responsibility | OpenAI current contract | Anthropic current contract | Portable conclusion |
|---|---|---|---|
| Stable application instruction | `instructions` / developer message；developer 高于 user | 顶层 `system`；部分当前模型另支持受位置约束的 mid-conversation `system` message | “稳定应用指令”可抽象，字段和生命周期不可直接等同 |
| Per-request goal / input | user message / `input`；developer 与 user 被类比为函数定义与参数 | user message；完整 messages request 仍受当前 model contract 约束 | 当次目标与输入必须显式映射到当前 API |
| Dynamic values | 官方建议使用 typed function arguments / schemas，并生成 `instructions` 与 `input` | application-observed state 可作为新的 system-level fact，但不可信外部内容不得放入 system message | 动态事实由 Application 提供；不要把 raw untrusted text 提升为高权重指令 |
| Examples | 常放在 developer message 的 Examples section | 当前 prompting guide 建议 relevant、diverse、structured examples | 示例用于示范模式；承载位置与建议数量不是通用标准 |
| Priority / role model | developer 当前优先于 user | top-level system + model-specific mid-conversation system rules | 不发布跨 Provider 固定 role enum 或完整 hierarchy |

## Boundary Findings

### Prompt != Context / Current Facts

Prompt 可以承载本次提供的 context，但不会自行完成检索、时效验证、记忆治理或状态同步。OpenAI 当前 Prompt Engineering 把 Context 定义为调用方提供的额外相关信息；Anthropic 当前 mid-conversation system message 指南把文件变化、权限模式、工具变化等描述为 Application 先观察到的 state change。正文因此只能说：**Prompt 可以表达怎样使用已提供事实，不能靠措辞把未观察、未检索或未校验的信息变成 current fact。** 完整 Context / Memory / RAG 机制留给 Article 12—17。

### System Prompt != Permission / Policy

OWASP 要求 tool call 对照 user permission 与 session context 校验，并使用最小权限、参数验证和高风险审批；OpenAI 当前 Agent Safety 同样要求保持 tool approvals，并指出 structured outputs 与 isolation 只能降低、不能完全消除 injection risk。系统 / developer instruction 可以告诉模型“应该怎样做”，但不能赋予、撤销或证明真实权限。Article 19 再展开 Permission、Approval、Sandbox 与 Human-in-the-loop。

### Few-shot != Knowledge Base

示例负责展示模式。它们不提供知识来源治理、更新策略、检索、权限或时效保证。由于示例本身也是请求输入，陈旧或不代表真实分布的示例仍可能影响输出；本篇只把这点写成工程风险，不量化影响，也不把 few-shot 等同于知识库。

### Delimiter != Injection Immunity

Anthropic 当前文档说明 XML tags 可以减少 instructions、context、examples 与 variable inputs 的混淆；OWASP 将 structured prompts with clear separation 列为一层防御。但 OWASP 与 OpenAI 仍要求输入 / 输出验证、least privilege、approval、guardrails、isolation 与 adversarial testing。Delimiter 改善可辨识性，不建立 security boundary。

### Output Requirement != Structured Output Validation

OpenAI 当前 Structured Outputs 文档明确：自然语言要求或 JSON mode 不保证指定 schema；Structured Outputs 才提供 schema adherence。文档同时说明 Structured Outputs 仍可能包含内容错误。因此本篇只建立自然语言 Output Requirement 的职责和边界，不提前讲 Article 03 的 schema、parse、validate、repair 或 domain validation。

## Version And Test Contract

最低可迁移记录建议：

1. 保存 prompt builder / template 的代码或配置版本与变更原因。
2. 把动态变量列成显式、可 review 的输入，不使用不可追踪的隐式拼接。
3. 固定一组代表性输入、边界输入、成功标准和判定方式；变更前后使用相同条件。
4. 记录 Provider、模型快照 / 版本与关键生成参数。
5. 保存原始输出与判定；不把手写“理想结果”冒充 runtime observation。
6. Prompt ID 可以作为某个 Provider 的附加元数据，但不是跨 Provider 必需字段。

版本敏感说明：OpenAI 当前 Prompt Engineering 文档写明 reusable prompt objects 自 `2026-06-03` 起弱化创建入口，`/v1/prompts` 计划于 `2026-11-30` 关闭，并建议新工作把 production prompts 保存在应用代码中。OpenAI 当前 Evaluation Best Practices 也写明托管 Evals platform 将于 `2026-10-31` 对既有用户转只读、`2026-11-30` 关闭。课程因此依赖可迁移的测试责任，不绑定即将退出的托管对象。

## A/B Teaching Fixture Design

- Fixture ID：`02-FX01`
- Classification：`SYNTHETIC_COURSE_PROPOSAL`
- Execution Status：`NOT_EXECUTED`
- Provider / Model / Parameters：`NOT_SELECTED`
- Runtime Observation：`NONE`
- Claim Status：`PROPOSAL`

固定合成输入（不是项目真实日志）：

```text
Environment: Unity 2022.3.62f3, Android, CI stage CompileScripts
Log excerpt:
Assets/Editor/BuildMenu.cs(42,19): error CS0103: The name 'buildTarget' does not exist in the current context
BuildPipeline.BuildPlayer: Build failed
Process exited with code 1
```

Prompt A：

```text
请总结这段 Unity 构建日志。
```

Prompt B（任务合同版）：

```text
Goal: 只根据所给日志定位第一个可行动的失败点。
Constraints: 不猜测根因或修复；每个结论必须引用日志原文；未知项写 UNKNOWN。
Input: <LOG>...</LOG>
Output Requirements: Status / Primary Failure / Evidence / Unknown / Next Check。
Failure Semantics: 若没有 compiler error，返回 INSUFFICIENT_EVIDENCE。
```

若未来执行，必须锁定同一 Provider、模型快照、参数和输入，并保存两份原始输出。只可检查：规定字段是否出现、Primary Failure 是否直接来自日志、Evidence 是否引用输入、未知信息是否显式标记。一次 A/B 不能证明 accuracy 提升、跨模型鲁棒性、因果优越性或生产收益；手写预期只能标 `ILLUSTRATIVE_EXPECTED`，不得标 `OBSERVED`。

## Research Conclusions

- 9 个 RQ 均已落盘；`02-C01`—`02-C08` 无 `BLOCKED`，`02-C04` 为已收窄 `PARTIAL`。
- `02-C09` 仅为 `PROPOSAL / NOT_EXECUTED`，不能产生 runtime 或 accuracy 结论。
- 可以安全进入 Outline 的主线是：任务合同职责 → stable / dynamic 分层 → Provider-specific role contract → examples / delimiters 的有限作用 → permission / facts / structured output / eval 边界。
- Article 03 / 12—17 / 19 / 22 的完整机制均不在本篇展开。
- Evidence Gate 建议 `PASS`；Lifecycle 只返回 `EVIDENCE_READY` candidate，由 Master 决定并更新 durable global state。

## Research Stop Line

Researcher 在 `research.md` 与 `evidence.md` 完成后停止。不创建或修改 Outline、Draft、Review、Published Content、`status.md`、`course-run-state.md` 或任何全局状态；不执行 A/B fixture；不 commit。
