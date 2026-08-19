---
title: "Prompt Engineering：任务合同、角色、示例与边界"
slug: "agent-engineering-02-prompt-engineering-contract-boundaries"
date: "2026-08-19"
description: "建立 Goal、Constraints、Inputs、Examples、Output Requirements 与 Failure Semantics 的 Prompt 审查模型，并区分 Provider contract、权限、事实、结构化输出和评测边界。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Prompt Engineering"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 30
weight: 3030
---

> **上一篇**：[模型调用到底发生了什么：LLM、Model API、Messages 与 Token]({{< relref "ai-empowerment/agent-engineering-01-model-api-messages-token.md" >}})

> 本文资料核对时间：2026-08-19。OpenAI 与 Anthropic 的字段、role、指令位置、优先级和模型支持范围都可能演进，使用时应重新核对当前 Provider、API、模型与版本 contract。本文的 Unity A/B 仅为未执行的合成教学提案，没有产生任何模型运行结果。

在上一篇里，我们把一次模型调用拆成了 Application、SDK / HTTP Client、Provider API、Model 和 Response。Application 已经知道怎样把输入交给 Model API，接下来的问题自然是：输入到底应该怎样写？

团队第一次处理这个问题时，很容易把 Prompt Engineering 理解成“把话说得更聪明一点”。输出不满意，就继续补上“请认真分析”“不要猜测”“严格按格式返回”；换一个模型后效果变化，再去寻找另一组所谓的万能措辞。

这种做法最大的问题不是 Prompt 不够长，而是变更不可审查。以一段 Unity 构建日志为例：

```text
Environment: Unity 2022.3.62f3, Android, CI stage CompileScripts
Log excerpt:
Assets/Editor/BuildMenu.cs(42,19): error CS0103: The name 'buildTarget' does not exist in the current context
BuildPipeline.BuildPlayer: Build failed
Process exited with code 1
```

先说明：这是合成教学材料，不是真实项目日志，也没有被发送给任何模型。

如果请求只有一句“请总结这段 Unity 构建日志”，我们还无法回答许多工程问题：目标是做摘要、定位第一个失败点，还是提出修复？允许根据经验猜测吗？证据不足时应该继续补全，还是明确返回未知？输出只给人阅读，还是准备交给程序消费？修改 Prompt 之后，又凭什么判断它变好了？

这些问题共同指向一个更稳的定义：**Prompt Engineering 首先是任务表达与审查问题，不是魔法措辞问题。**

## 抽象模型：把 Prompt 审查成六项任务职责

本课程使用六项职责审查一份任务 Prompt：Goal、Constraints、Inputs、Examples、Output Requirements 和 Failure Semantics。

| Element | 审查问题 | 它不负责什么 |
|---|---|---|
| Goal | 这次调用到底要完成什么？ | 不包含权限授予 |
| Constraints | 范围、禁止项、证据和质量约束是什么？ | 不等于外部 Policy enforcement |
| Inputs | 本次允许使用哪些数据，来源和可信边界是什么？ | 不负责完整 Context 生命周期 |
| Examples | 是否需要输入 / 输出示范来表达模式？ | 不等于 Knowledge Base |
| Output Requirements | 调用方需要什么呈现形状？ | 不等于 schema 或领域校验 |
| Failure Semantics | 输入或证据不足时怎样显式返回？ | 不替代异常处理、审批或审计 |

这不是行业统一 schema，也不是要求每次请求都机械填写六段的模板。它更像一张 code review 清单：某个任务不需要 example，可以不写；某个任务对证据边界很敏感，就应该把 Inputs、Constraints 和 Failure Semantics 写得更清楚。

以刚才的日志任务为例，六项职责可以被压缩成下面这份表达：

```text
Goal: 只根据所给日志定位第一个可行动的失败点。
Constraints: 不猜测根因或修复；每个结论必须引用日志原文；未知项写 UNKNOWN。
Inputs: <LOG>...</LOG>
Examples: 当前任务不提供。
Output Requirements: Status / Primary Failure / Evidence / Unknown / Next Check。
Failure Semantics: 若没有 compiler error，返回 INSUFFICIENT_EVIDENCE。
```

它的价值不在于英文标签本身，而在于任务条件变得可检查：我们能指出 Goal 是否漂移、Constraints 是否遗漏、Inputs 是否越界，也能讨论失败时应该返回什么。六项写全仍不能保证答案正确、安全或稳定；它只是把原本藏在作者脑中的期望显式化。

这里还有一个重要边界：Inputs 是调用方已经提供的材料。Prompt 可以告诉模型怎样使用这些材料，却不能靠一句“请使用最新构建号”，让一个未观察、未检索、未校验的构建号自动成为 current fact。

## 具体机制：稳定指令、当次目标与动态输入分开管理

任务职责建立后，还要回答一个落地问题：哪些内容应该稳定维护，哪些内容应该随请求变化？

可以先不考虑任何 Provider 字段，把内容拆成三类：

```text
Stable Application Instruction
  + Per-request User Goal
  + Dynamic Input / Application-observed Facts
  -> 按当前 Provider contract 映射
  -> 一次具体请求
```

Stable Application Instruction 表达相对稳定的应用身份、工作范围和共同约束；Per-request User Goal 表达这一次要完成的目标；Dynamic Input 则承载日志、分支、构建号、文件内容或 Application 已观察到的状态。这样拆分的直接收益，是动态值不必偷偷拼进一段长期字符串，review 时也能看见“规则改了”还是“本次参数变了”。

但这三类职责**不固定对应一套跨 Provider role**。根据 2026-08-19 核对的当前公开 contract：

| Responsibility | OpenAI 当前表达 | Anthropic 当前表达 | 可迁移判断 |
|---|---|---|---|
| 稳定应用指令 | `instructions`、developer 等当前机制 | 顶层 `system`；部分当前模型另有受位置约束的 mid-conversation system 机制 | 职责可抽象，字段与生命周期不可直接等同 |
| 当次目标 / 输入 | user message、`input` 等当前表达 | user message 与当前 Messages contract | 当次目标必须映射到当前 API |
| 动态值 / 事实 | 由应用显式管理 typed / reviewed inputs | 由 Application 观察后按当前 contract 提供 state update | 动态事实来自 Application，不来自措辞 |

OpenAI 当前文档说明 developer instruction 相对 user 的优先级，这只是 OpenAI 当前 contract 的事实。Anthropic 的通用 Messages contract 使用顶层 `system`，部分当前模型又支持带 placement rules 的 mid-conversation system message。把这些内容压成 `system > developer > user > tool`，会制造一个并不存在的统一 hierarchy。

因此，面对一家新 Provider，正确动作不是先发明共同 role enum，而是核对：当前 API 有哪些 instruction mechanism？放在哪里？哪些模型支持？优先级和位置规则是什么？生命周期怎样定义？稳定的是这些审查问题，不稳定的是字段答案。

同样，“稳定”和“动态”描述的是内容职责，不是永久绑定某个 role。动态事实也不因为放进更高权重的位置就变得可信；Application 仍然要知道它从哪里来，并避免把原始、不可信的外部内容直接提升为高权重指令。

## Examples 与 Delimiters：帮助表达，不提供普遍保证

Few-shot example 的职责，是用少量输入 / 输出示范让模型看到任务模式。根据本篇已经核对的 OpenAI 与 Anthropic 当前指南，可以安全写出的范围是：example 可以示范格式、语气、结构与任务模式；示例应当与用例相关、覆盖差异并保持清楚的结构。

这不等于“加 few-shot 就会提高所有任务的准确率”，也不等于示例越多越好。当前证据不足以给出通用的 token 成本、staleness 或 bias 量化结论。示例是否有帮助、是否携带旧假设，必须放回具体 workload 验证，而不能从一份 Provider 指南外推成普遍定律。

Example 也不是 Knowledge Base。它负责展示模式，不提供知识来源、更新策略、检索、权限或时效保证。如果一个示例写着旧分支名，模型可能把这个模式带进输出，但这不意味着旧分支名成为了经过核验的 current fact。

Delimiter 的边界也类似。把数据写成：

```text
<INSTRUCTIONS>
只根据输入日志定位第一个失败点。
</INSTRUCTIONS>

<LOG>
...
</LOG>
```

可以帮助区分 instructions、context、examples 与 variable input。XML tag、Markdown 标题或其他 data marking 都可以承担这种“让边界更可辨识”的职责；本篇不宣称其中某一种格式普遍最佳。

更重要的是：**delimiter 不是 Prompt Injection immunity。** 清楚分隔只是 defense-in-depth 的一层。输入 / 输出验证、最小权限、高风险审批和隔离仍然是 Prompt 外部的工程责任。把不可信文本放进 `<LOG>`，不会让它自动无害；把一句规则放进 `<INSTRUCTIONS>`，也不会产生真实授权边界。

## 工程边界：Prompt 能表达期望，不能吞掉外部系统责任

Prompt 最容易被高估的地方，是把“告诉模型应该怎样做”误写成“系统已经保证这样做”。下面四组边界必须分开：

| Prompt 可以表达 | 仍需外部系统负责 | 本篇停在哪里 |
|---|---|---|
| “不要删除文件” | 工具权限、凭据 / 文件系统范围、最小权限和审批 | 只说明 Prompt 不授予或撤销真实权限 |
| “只使用当前分支和构建号” | Application 对状态的观察、检索、更新与校验 | 只说明 current fact 必须由调用方提供 |
| “用标签区分日志与指令” | 输入 / 输出验证、隔离与 injection 防御 | 只说明 delimiter 不是安全边界 |
| “按指定字段返回” | schema adherence、类型与领域校验 | 只说明自然语言要求不是机器保证 |

第一种反模式，是在 system 或 developer instruction 里写“不要删除文件”，然后宣布删除已经被禁止。Prompt 只能表达期望行为；真正能否删除，取决于工具是否被暴露、凭据和文件系统范围是什么、执行前是否需要 approval。清晰指令有价值，但不能代替 authorization、least privilege 或 sandbox。

第二种反模式，是把动态事实写死进长期模板。分支、构建号、权限模式和可用工具都会变化。Prompt 可以规定“如何使用已提供事实”，却不能自行完成状态同步、检索或事实核验。本篇只切清责任，不展开这些信息怎样选择、压缩、持久化或检索。

第三种反模式，是看到模型返回了一段整齐 JSON，就说机器合同已经成立。自然语言 Output Requirement 可以引导呈现形状，但不等于 schema adherence、类型校验，更不保证领域值正确。机器可消费输出的完整合同属于下一篇；本篇到“任务希望怎样呈现”这一层为止。

这三个反模式背后是同一个判断：**Prompt 是行为表达层，不是执行控制面、事实来源或结果验证器。**

## Prompt 变更也需要版本与固定 Fixture

既然措辞本身不提供保证，团队就不能用“这版看起来更好”批准 Prompt 变更。最低可迁移的 Prompt change contract 可以保留下面这些记录：

| Record | Review Question |
|---|---|
| Prompt source version | 哪个 code / config version 产生了本次请求？ |
| Explicit variables | 哪些值是本次动态输入，来自哪里？ |
| Change reason | 为什么修改，准备影响哪些检查项？ |
| Representative fixtures | 用哪些固定输入做前后比较？ |
| Success criteria | 比较前怎样定义满足与不满足？ |
| Environment | Provider、模型快照 / 版本与关键参数是什么？ |
| Raw outputs / judgments | 原始输出在哪里，依据什么做判定？ |

Prompt ID 可以是某个 Provider 的附加元数据，但不是跨 Provider 必需字段。更稳定的接口是：可 review 的代码或配置、显式变量、变更原因、冻结输入、预先定义的标准，以及保存下来的原始输出和判定。

“同一 fixture、同一条件、同一标准”提供的是最低可比边界，不是完整 Eval。一个样本不足以代表真实分布，固定参数也不等于消除了所有变化。本篇只要求 Prompt 变更能够被复看、复测；Golden Dataset、grader、metrics 和 regression system 留到后文正式展开。

## 验证边界：`02-FX01` 没有被执行

回到开头的 Unity 日志。下面这组 A/B 只用于展示怎样冻结教学问题，必须先保留它的真实状态：

```text
Fixture ID: 02-FX01
Classification: SYNTHETIC_COURSE_PROPOSAL
Execution Status: NOT_EXECUTED
Provider / Model / Parameters: NOT_SELECTED
Runtime Observation: NONE
Claim Status: PROPOSAL
```

固定的合成输入就是开头那段 Unity 2022.3.62f3 / Android / `CompileScripts` 日志，不来自真实项目。

Prompt A 只有一句：

```text
请总结这段 Unity 构建日志。
```

Prompt B 把任务职责显式化：

```text
Goal: 只根据所给日志定位第一个可行动的失败点。
Constraints: 不猜测根因或修复；每个结论必须引用日志原文；未知项写 UNKNOWN。
Input: <LOG>...</LOG>
Output Requirements: Status / Primary Failure / Evidence / Unknown / Next Check。
Failure Semantics: 若没有 compiler error，返回 INSUFFICIENT_EVIDENCE。
```

本轮没有选择 Provider、模型或参数，没有调用 API，也没有两份原始输出。因此，`02-FX01` 当前不提供任何模型行为证据，更不存在 runtime observation 或 accuracy improvement 结论。Prompt B 只是比 Prompt A 暴露了更多可审查的任务条件；这句话描述的是文本结构，不是模型效果。

如果未来执行，首先要锁定同一 Provider、模型快照、参数和输入，再保存两份原始输出。一次受控运行也只能检查有限项目：规定字段是否出现；Primary Failure 是否直接来自日志；Evidence 是否引用输入；未知信息是否显式标记。

即使这些检查全部满足，也不能自动推出准确率提高、跨模型鲁棒性、因果优越性或生产收益。手写的理想输出只能叫 `ILLUSTRATIVE_EXPECTED`，不能冒充 `OBSERVED`。更完整的质量判断需要更广的样本、边界案例、判据和重复验证，但这些机制不在本篇展开。

## 最后应该怎样审查一份 Prompt？

下一次看到一份任务 Prompt，可以先沿下面的顺序检查：

1. Goal、Constraints、Inputs、可选 Examples、Output Requirements 和 Failure Semantics 是否足以表达当前任务？
2. 稳定应用指令、当次目标与动态事实是否被显式分开？
3. 字段、role、位置和优先级是否来自当前 Provider contract，而不是想象中的统一 hierarchy？
4. Few-shot 和 delimiter 是否只承担模式表达与边界标记，没有被写成效果或安全保证？
5. Permission、current facts、schema validation 等外部责任有没有被 Prompt 吞掉？
6. 变更是否有版本、固定 fixture、成功标准、原始输出和判定记录？

这比收集一份“最佳 Prompt 词库”慢一点，却能让团队知道自己改了什么、没有证明什么，以及下一层工程责任在哪里。

## Learning Check

1. 把“请帮我分析 Unity 日志”拆成六项任务职责。哪些项可以按任务省略，为什么？
2. Stable Instruction、Per-request Goal、Dynamic Input 是否固定对应 system、developer、user 三个 role？
3. 在 system / developer instruction 中写“不要删除文件”，是否已经撤销删除权限？
4. Few-shot 示例能安全说明哪些作用？哪些结论必须放回具体 workload 验证？
5. Prompt B 输出字段更整齐，是否已经证明诊断更正确？
6. 修改一个生产 Prompt，最低应该保存哪些版本与测试记录？
7. `02-FX01` 当前能提供什么模型行为证据？

### 参考思路

1. 六项是 review checklist，不是强制表单；应优先说清 Goal、可用 Inputs、关键 Constraints，以及证据不足时的 Output / Failure Semantics。
2. 不是。职责可以迁移，字段、位置、优先级与生命周期必须按当前 Provider、API、模型和版本映射。
3. 没有。真实权限、least privilege、approval 与 sandbox 属于 Prompt 外部工程控制。
4. 可以说它示范格式、语气、结构与任务模式；不能据此宣称通用准确率提升，也不能量化本篇没有测量的 token、staleness 或 bias 影响。
5. 没有。自然语言格式遵循、schema adherence、领域正确性与 accuracy 是不同判断。
6. 至少保存 code / config version、显式变量、变更原因、代表性 fixtures、成功标准、Provider / model / parameters、原始输出与判定记录。
7. 没有。它是 `SYNTHETIC_COURSE_PROPOSAL / NOT_EXECUTED`，只能展示怎样冻结输入和有限检查项。

## 最短结论

`Prompt 的工程价值，不在于让一句话显得更聪明，而在于让任务、边界和变更变得可审查。`

## 参考资料

- [OpenAI：Prompt engineering](https://developers.openai.com/api/docs/guides/prompt-engineering)
- [OpenAI：Model guidance](https://developers.openai.com/api/docs/guides/latest-model)
- [OpenAI：Evaluation best practices](https://developers.openai.com/api/docs/guides/evaluation-best-practices)
- [OpenAI：Structured model outputs](https://developers.openai.com/api/docs/guides/structured-outputs)
- [OpenAI：Safety in building agents](https://developers.openai.com/api/docs/guides/agent-builder-safety)
- [Anthropic：Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices)
- [Anthropic：Create a Message](https://platform.claude.com/docs/en/api/messages/create)
- [Anthropic：Mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages)
- [OWASP：LLM Prompt Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html)
