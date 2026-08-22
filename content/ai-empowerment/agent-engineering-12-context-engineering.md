---
title: "Context Engineering：每一个 Step 到底应该看到什么"
slug: "agent-engineering-12-context-engineering"
date: "2026-08-21"
description: "Context Engineering 将 effective Context 与应用可见的 Context Snapshot 分开，并用 Receipt 审计每个 Step 的选择、排除、预算与未知。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Context Engineering"
  - "Observability"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 130
weight: 3130
---

> **上一篇**：[Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery]({{< relref "ai-empowerment/agent-engineering-11-long-running-agent.md" >}})
> **下一篇**：[Context Debugging：Packing、Compression、Pollution 与可重建性]({{< relref "ai-empowerment/agent-engineering-13-context-debugging.md" >}})

> 如果这篇只记一句话：`先审查这个 Step 的effective Context可能由什么构成，再讨论它为什么答错；应用只描述、审计和比较自己的Context Snapshot。`

这是一个构造的评审场景：假设一个诊断 Agent 读到 Unity 编译日志后，把“修复一个变量未定义”写成了“项目构建已成功”。它对应下文 `INV-12-01` 的 `COURSE DESIGN / NOT_EXECUTED`，没有 runtime evidence。团队往往先回头改 Prompt：加一句“请严格分析”、补一段示例、换一套更长的措辞。但在真正查错前，有一个更短也更尖锐的问题：**这个 Step 当时到底看到了什么？**

它是否拿到了当前构建日志，还是旧 revision 的摘要？是否看到了可调用工具的边界，还是把历史里的过期 schema 当成能力？是否保留了输出余量，还是把窗口装满后让 Provider 或调用方截断？只保存最后一段 Prompt，通常答不出这些问题。

因此，本篇把 Context 保持为某个 Step 实际可见的有效 token / 信息集合；而 Context Engineering 审查应用可构建的 `application-visible Context Snapshot`：在特定 Provider、模型和 request contract 下，应用把可见来源选择、排序、限定作用域并放入预算，形成一个可解释的视图。effective Context还可能包含 Provider-managed additions、transformations 与 unknowns。Receipt只描述、审计和比较Snapshot。它们都是课程工作定义，不是任何 SDK 的 wire schema，更不是跨 Provider 的统一接口。

这也先切开四个常见误解：`Prompt != Context`；`Context != Session`；`Tool Result != permanent history`；`Snapshot != Memory / Checkpoint`。上一篇关于 [Prompt 合同]({{< relref "ai-empowerment/agent-engineering-02-prompt-engineering-contract-boundaries.md" >}}) 讨论任务如何表达；本篇讨论某次 Step 实际带了哪些材料。两者相关，但不是同一个对象。

## Context Assembly：把请求看成一次可审查的构建

在 [Agent Loop]({{< relref "ai-empowerment/agent-engineering-08-agent-loop.md" >}}) 中，Step 是一次有明确输入和结果边界的推进单位。对每个 Step，本课程采用下面这个 **Context Assembly** 心智模型：

```text
Instruction + Current Goal + Working State + History
  + Capabilities + External Facts
       -> Select -> Order -> Scope -> Fit Budget -> Snapshot
       -> Model Step
```

这里的箭头描述课程的审查顺序，不是 Provider 的内部调用图，也不宣称每次调用都采用同一算法。当前 OpenAI Responses API 的公开 request surface 包含 `input`、`instructions`、`tools`、延续、截断和 usage；Anthropic 当前文档则明确 system、messages、tools、tool results 与当次输出会共同占用模型相关的 context window。它们足以说明一次调用不只有“最后那段 Prompt”，却不足以推出统一字段、统一 token 公式或统一的最佳装配策略。[OpenAI Responses API](https://developers.openai.com/api/reference/cli/resources/responses/methods/create) 与 [Anthropic Context Windows](https://platform.claude.com/docs/en/build-with-claude/context-windows) 的适用范围都应以 2026-08-21 核对的当前产品合同为限。

为让 provenance 可审查，本课程把 application-visible contributor 暂分为六类。它们是 **COURSE PROPOSAL / provenance taxonomy**，不互斥、不完备，也不是 Provider role mapping；同一段 bytes 在不同 Step 里可能承担不同责任。

| Contributor | 回答的问题 | 典型内容 | 不应被误写成 |
|---|---|---|---|
| Instruction | 应怎样完成任务？ | 证据边界、失败语义、输出约束 | 当前事实或真实权限 |
| Current Goal | 这一步要推进什么？ | 本 Step 目标、输出合同 | 长期 Plan 的全文 |
| Working State | 已接受的当前控制事实是什么？ | revision、已接受证据引用、未解决条件 | raw Result 或 Trace 的替身 |
| History | 已发生且可能相关的过程是什么？ | 有界 Observation 摘要、引用 | 永久全量对话 |
| Capabilities | 这一步可合法调用什么？ | eligible tool schema、host policy view | 历史中抄来的过期工具表 |
| External Facts | 当前可引用的外部材料是什么？ | 日志、源码片段、配置、locator | 已验证结论或高权重指令 |

`Stable / Dynamic` 也只能作为课程的生命周期审查标签：相对稳定的项目级 Instruction 可能长期复用，当前 Goal、State、工具资格和外部事实通常随 Step 变化；它不表示信息永远不变，更不映射某个 Provider 的固定角色。类似地，[Planning]({{< relref "ai-empowerment/agent-engineering-09-planning.md" >}}) 中的旧 Plan 可以是历史材料，却不能压过当前 authoritative State；[State Machine 与 Workflow]({{< relref "ai-empowerment/agent-engineering-10-state-machine-workflow.md" >}}) 中的 Trace 可以记录过程，也不能直接等同 State。

## 同一调查 Step 应带什么：一个未执行的课程设计

下面的 `INV-12-01` 是合成 Unity `CS0103` 日志和匹配源码的课程设计示例。任务只是在材料中判断第一个可行动失败点；它**不是** BuildPilot Runtime，没有发起 API 调用，也没有模型观察结果。

### Investigation-Step Request Breakdown（课程设计示例 / NOT_EXECUTED）

| Order | Contributor | Concrete source / selected form | Authority / trust | Scope | Budget treatment | Omission / stale consequence |
|---:|---|---|---|---|---|---|
| 1 | Instruction | `prompt-contract-v3`；只基于 Evidence、未知写 `UNKNOWN`、不修改；concise instruction | application / trusted | project + role | `MUST_KEEP` | 丢失证据与 failure 边界 |
| 2 | Current Goal | `DIAGNOSE_FIRST_FAILURE@rev17`；goal + output contract | workflow / trusted | this Step | `MUST_KEEP` | 漂移成“修复”或“总结” |
| 3 | Working State | `EV-LOG-017 / EV-SRC-009 / unresolved=ROOT_CAUSE`；typed summary + refs | reducer / mixed | current run / rev17 | `MUST_KEEP` | 旧 revision 混淆已知 / 未知 |
| 4 | Capabilities | `read_text@2 / report_diagnosis@1`，`READ_ONLY`；two eligible schemas | Host registry / policy | Stage / Agent | `MUST_KEEP`；排除 78 项 | 全量工具占预算；缺失则无合法能力 |
| 5 | External Facts | normalized log / source excerpts + hash / locator；bounded evidence excerpts | observed input / untrusted | investigation version | 保留 evidence，裁 noise | 无法引用首错；旧源码会冲突 |
| 6 | History | previous unrelated read + no-progress；one summary + ref | correlated trace / Observation | current run | `KEEP_IF_RELEVANT` | 全删会重复；全留会淹没当前事实 |
| 7 | Omitted set | old Plan v1、raw 50k log、78 tools、unaccepted Result；Receipt-only entries | mixed | audit | zero model tokens | 未记录排除就无法解释选择 |
| 8 | Request controls | Provider / model、output ceiling、truncation、tool choice；metadata | application config | this request | reserve output first | 无法解释 token / truncation 差异 |

这个表的关键不在于“第 1 行永远应该先于第 2 行”，而在于每一行都回答了来源、authority、scope、裁剪理由和遗漏代价。有限窗口、可选工具和当前 State 使选择成为工程问题；逐 Step 复核是课程建议，不是已经由产品文档证明的最优算法。没有进入模型的材料也不该消失，它仍应留在 Receipt 中，作为可解释的 omitted set。

还要把四个容易混在一起的动作拆开。**Select** 决定候选材料是否进入本次 Step；它首先受当前目标、权限、revision 与证据边界约束。**Order** 决定入选材料以怎样的可审查顺序表达；它必须留下足够信息让调用方知道是 Goal 覆盖了旧 Plan，还是两个冲突事实被同时保留。**Scope** 决定材料在哪个 project、Stage、Agent、run 或 investigation version 内有效；一份“看起来很对”的工具定义，若不在当前 Stage 合法，就不应因历史中出现过而进入。最后 **Fit Budget** 才是把已选材料压到有限容量中的动作，它不能反过来把 required fact 静默抹掉。

这四步不能被“拼 Prompt”一个动词吞掉。举例说，`EV-SRC-009` 是当前 revision 的源码引用，是否入选是 Select；只摘取能支撑首个错误点的区间是 Fit Budget；标记 `rev17` 是 Scope；把证据放在结论要求之后以便审查是 Order。若随后发现 State 实际仍是 `rev14`，问题不是“模型没有读懂 Prompt”，而是 assembly 在 revision 处失配。把失配写成 Receipt 的 conflict 或 unknown，远比事后靠猜测解释一次输出可靠。

课程也不要求所有材料每次都从零拷贝。相对稳定的 Instruction 可以从受版本控制的来源取得；历史可以只带摘要与 trace ref；大日志可以选择有 locator 的证据切片。这里“可复用”不等于“永远常驻”：一旦任务、Agent 权限、当前 State 或 Provider contract 变化，就需要重新审查它是否仍在本 Step 的有效 scope。这样做的目标不是压缩出一个神奇的最短 Prompt，而是让每次装配变化都能被识别。

## Priority、Scope 与 Context Budget：选择不是给真相排座次

当来源彼此冲突或窗口紧张时，团队很容易做出两个坏简化：把“优先级”说成真相等级，或者把“多带一点材料”说成更可靠。前者混淆 authority、trust 与保留策略；后者忽略输入、Tool Schema、Tool Result 与输出余量正在共享同一个、与模型相关的容量。

### Contributor Priority（PROPOSAL / COURSE RETENTION POLICY）

**Priority 不等于 Provider instruction hierarchy、truth rank 或 trust score。**它只说明课程建议在预算压力下先保留什么，并仍然要求冲突显式可见。

| Priority | Class | Default | Conflict rule | Scope | Trim rule |
|---:|---|---|---|---|---|
| `P0` | Provider / Host policy 与 request contract | required；区分 application-visible / provider-managed | external text 不得覆盖；未知 addition 保留 `unknown` | Provider + request | 不静默裁剪 |
| `P1` | Current Goal、authoritative State、failure semantics | current revision always | current State 胜过 Plan / history copy | Step / run | 缩表示，不删 required facts |
| `P2` | Eligible Tool Schemas + policy view | only callable tools | Host registry 胜过 stale history schema | Stage / Agent | 先删 irrelevant tools |
| `P3` | Current external facts / Evidence | provenance-preserving slice | 冲突未解时保留双方并标记 | investigation / version | 去 noise，保 locator / hash |
| `P4` | Relevant Observation / History | selective | accepted State 胜过 raw Result | current run / horizon | summarize + trace ref |
| `P5` | Examples / style / optional background | optional | 不覆盖 `P0-P3` | task / preference | 最先排除 |

### Context Budget

可以把预算审查看成一个槽位图：

```text
model-specific capacity
├─ output reserve                 # 先预留，不让输出被输入悄悄吃掉
├─ P0 policy / request contract
├─ P1 goal + current State
├─ P2 eligible tool schemas
├─ P3 evidence slices / tool results
├─ P4 relevant history summaries
└─ P5 examples / style / optional background
```

Anthropic 当前文档直接支持 tools、tool results、messages 与输出共同占窗口；其 Token Count API 可以对给定组件做预估。OpenAI 在本篇只作为 request surface、truncation 与 usage 的例证，不把它外推成相同 tokenizer、同一计费公式或相同截断行为。预算还涉及容量、输出余量和成本；它对答案质量的影响必须在具体 workload 中验证，不能仅凭 token 数得出“更多必然更好”或“更少必然更准”。

预算决策最需要避免的，是把“裁剪”伪装成“没有发生”。例如 80 个全局工具 schema 可能覆盖了真正可用的两个只读工具，也可能挤掉当前源码片段和输出 reserve；但本篇没有执行调用，不能声称任一包会使模型表现更好或更差。可以安全保留的判断只有：容量压力要求调用方有选择契约，且选择之后应能回查被排除项、保留依据和未知的实际 token 数。若应用无法在 required contributor 与输出余量之间给出诚实的 assembly，就应记录 fail-closed 设计意图或返回上游补充材料，而不是静默改写证据。

冲突也不应只靠一个优先级数字解决。`P1` 的 current State 可压过旧 Plan 或 history copy，是因为它们对“当前控制事实”的 authority 不同；两段 External Facts 若都可引用却指向不同文件行，则不是把较旧的一段删掉就算解决，应保留双方、标记冲突并说明本 Step 是否因此只能给出 `UNKNOWN`。Priority 帮助安排保留和裁剪；authority 解释谁能定义当前控制事实；trust 说明来源的可信边界。三者分开，Receipt 才不会把未经解决的矛盾伪装成一条平滑叙述。

工具这一行尤其容易被遗漏。[Tool Runtime]({{< relref "ai-empowerment/agent-engineering-06-tool-runtime.md" >}}) 里的 ToolDefinition、合法性验证、Policy、Result 和 Trace 各有边界；这里仅说明：若某个 schema 或后续 tool result 被选入本次请求，它就是 Context 的 contributor。Result 没有被下一次 assembly 选中，不会自动成为永久历史；Trace 也不会自动给出它为什么被选中。

## Context Receipt：为一次可见装配开收据

最终 Prompt 文本或一个 request trace 都可能很有用，却不足以回答“这段材料来自哪个 revision”“为什么 78 个工具被排除”“冲突是如何保留的”。OpenAI Agents SDK 的 generation span 可以记录应用可见的 input、output、model config 与 usage，但 tracing 可以关闭、脱敏，ZDR 组织也不可用；这正好说明 Trace 不是 complete provenance。[Agents SDK Tracing](https://openai.github.io/openai-agents-python/tracing/) 是实现表面的一部分，不是完整复现器。

本课程因此提出一份可选的观察性合同：`context-receipt-course-v1 / COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD`。Receipt 应当在 assembly 时记录选择决定，而不是事后从最终文本猜来源。

```yaml
receipt_schema: context-receipt-course-v1
classification: COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD
step: {run_id: string, step_id: string, workflow_state_revision: string}
request:
  provider: string
  api: string
  model: string
  request_id: string|UNKNOWN
  request_contract_retrieved_at: date
  output_ceiling_tokens: integer|UNKNOWN
  truncation_policy: string|UNKNOWN
contributors:
  - contributor_id: string
    class: instruction|goal|working_state|history|capability|external_fact
    source_ref: string
    source_version: string|UNKNOWN
    authority: string
    trust: trusted|untrusted|mixed|unknown
    lifecycle: stable|dynamic
    scope: string
    priority: P0|P1|P2|P3|P4|P5
    disposition: included|summarized|excluded|conflict_retained
    order: integer|null
    content_digest: string|UNKNOWN
    reason: string
budget:
  estimator: string
  estimated_input_tokens: integer|UNKNOWN
  actual_input_tokens: integer|UNKNOWN
  output_reserve_tokens: integer|UNKNOWN
  omitted_contributor_ids: [string]
conflicts: [{conflict_id: string, contributors: [string], disposition: string}]
unknowns: [string]
provider_managed_context:
  known_present: boolean|UNKNOWN
  disclosed_description: string
  reconstructable: false
trace: {request_span_ref: string|NONE, raw_request_ref: string|NONE, response_usage_ref: string|NONE}
```

字段组对应的审查问题很直接：`step` 说明是哪一次 Step、哪个 State revision；`request` 固定可见请求边界；`contributors` 说明来源、版本、可信边界、顺序和 disposition；`budget` 与 omitted ids 说明容量取舍；`conflicts`、`unknowns` 和 `provider_managed_context` 让未决与不可见部分不能被伪装成确定性；`trace` 只保存可回指的实现证据。

下面是完整的 **PROPOSAL / NOT_EXECUTED** sample。它使用 `INV-12-01 / diagnose-03 / rev17`，没有发送过请求。

```yaml
receipt_schema: context-receipt-course-v1
classification: COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD
step: {run_id: INV-12-01, step_id: diagnose-03, workflow_state_revision: rev17}
request:
  provider: ANTHROPIC_EXAMPLE_ONLY
  api: Messages-current-contract
  model: NOT_SELECTED
  request_id: UNKNOWN
  request_contract_retrieved_at: 2026-08-21
  output_ceiling_tokens: 1200
  truncation_policy: APPLICATION_FAIL_CLOSED_PROPOSAL
contributors:
  - {contributor_id: I-01, class: instruction, source_ref: prompt-contract-v3, source_version: v3, authority: application, trust: trusted, lifecycle: stable, scope: project-role, priority: P0, disposition: included, order: 1, content_digest: SHA256-DEMO-I01, reason: evidence and failure semantics}
  - {contributor_id: G-01, class: goal, source_ref: DIAGNOSE_FIRST_FAILURE, source_version: rev17, authority: workflow, trust: trusted, lifecycle: dynamic, scope: this-step, priority: P1, disposition: included, order: 2, content_digest: SHA256-DEMO-G01, reason: current goal}
  - {contributor_id: S-01, class: working_state, source_ref: state-rev17, source_version: rev17, authority: reducer, trust: mixed, lifecycle: dynamic, scope: run, priority: P1, disposition: summarized, order: 3, content_digest: SHA256-DEMO-S01, reason: accepted refs and unresolved condition}
  - {contributor_id: C-01, class: capability, source_ref: host-registry-readonly, source_version: v12, authority: host-policy, trust: trusted, lifecycle: dynamic, scope: stage-agent, priority: P2, disposition: included, order: 4, content_digest: SHA256-DEMO-C01, reason: only eligible tools}
  - {contributor_id: F-LOG, class: external_fact, source_ref: EV-LOG-017, source_version: build-4310, authority: observed-input, trust: untrusted, lifecycle: dynamic, scope: investigation, priority: P3, disposition: summarized, order: 5, content_digest: SHA256-DEMO-FLOG, reason: first compiler error}
  - {contributor_id: H-OLD, class: history, source_ref: trace-step-02, source_version: rev16, authority: trace, trust: mixed, lifecycle: dynamic, scope: current-run, priority: P4, disposition: summarized, order: 6, content_digest: SHA256-DEMO-HOLD, reason: avoid repeated no-progress action}
  - {contributor_id: T-78, class: capability, source_ref: global-tools, source_version: v12, authority: host-registry, trust: trusted, lifecycle: dynamic, scope: global, priority: P5, disposition: excluded, order: null, content_digest: SHA256-DEMO-T78, reason: irrelevant tools}
budget:
  estimator: ANTHROPIC_COUNT_TOKENS_PLANNED_NOT_CALLED
  estimated_input_tokens: UNKNOWN
  actual_input_tokens: UNKNOWN
  output_reserve_tokens: 1200
  omitted_contributor_ids: [T-78]
conflicts: []
unknowns: [no API call, no provider request id, no effective model]
provider_managed_context:
  known_present: true
  disclosed_description: Anthropic current docs say tool use adds a special system prompt
  reconstructable: false
trace: {request_span_ref: NONE, raw_request_ref: NONE, response_usage_ref: NONE}
```

这里的 `ANTHROPIC_EXAMPLE_ONLY`、`NOT_SELECTED`、`UNKNOWN`、1200 的输出意图和 `APPLICATION_FAIL_CLOSED_PROPOSAL` 都是在表达合同或设计意图，不是已发请求的回读。`SHA256-DEMO-*` 是**占位 digest**，只示范“所指 bytes 一致”的字段形状；它不证明来源可信，也不证明 Provider 实际使用顺序。估算器未被调用，input token 均为 `UNKNOWN`，三个 Trace ref 均为 `NONE`，因而这份样例没有 runtime observation。

把 Receipt 称为“收据”，不是要求保存所有原文。它的最小职责是让下一位审查者能沿字段反问：`S-01` 的 accepted refs 来自哪个 State revision？`F-LOG` 是原始事实、摘要还是已验证结论？`T-78` 为什么没有进入模型？输出上限是调用方预留，还是 Provider 实际 usage？如果这些问题只能通过阅读某人的口头说明回答，装配就仍然不可审查。相反，Receipt 也不应收集超出用途的内部信息；它只保存满足本次 provenance、预算和边界复查所需的引用与摘要。

这份 schema 特意把 `request_id`、实际 input token 和 Trace ref 允许为 `UNKNOWN` 或 `NONE`。未知不是待修复的格式错误，而是证据状态：本样例没有请求，就不应为让表格好看而填写一个看似真实的 ID 或 token。相同原则也适用于 provider-managed context：文档披露的 special system prompt 可以记为 known present；其具体文本和内部位置不可重构，就必须保持 `reconstructable: false`。这让 Receipt 同时保存“已知存在”和“不能声称看见”的边界。

Provider-managed 部分同样不能被省略。Anthropic 当前 tool-use 文档披露启用工具时会加入特殊 system prompt，但应用 JSON 仍不是 Provider 内部全部effective Context。Receipt 可以声明已知 addition、transformation risk 和 unknown，却不能从 Trace 推出 hidden system text、隐藏推理或 server-side loop 的中间状态；它只描述、审计和比较 **application-visible Context Snapshot**，不能代表 Provider-internal Context。

## 三个 Snapshot：给未来验证留下可比较的设计输入

Snapshot 是一次 Step 的 selected view；Receipt 是解释这个 view 怎样形成的记录。下面三个包全都标为 **PROPOSAL / DESIGN INPUT ONLY / NOT EXECUTED**。它们用于未来 Lab 05 比较“可见 package 的差异”，不是 Expected、Observation、Result、模型表现或修复算法。

| Snapshot | Required visible package | Conflict / omission / unknown | Future-Lab role |
|---|---|---|---|
| `SNAP-12-A / CONSISTENT_CURRENT` | `rev17`；`prompt-contract-v3`；`DIAGNOSE_FIRST_FAILURE@rev17`；State `[EV-LOG-017, EV-SRC-009, unresolved=ROOT_CAUSE]`；capabilities `[read_text@2, report_diagnosis@1]`；facts `[build-4310-log, source-tree-9f2a]`；history `[step-02-no-progress-summary]` | conflicts `[]`；omits `78-unrelated-tools`、`raw-log-after-first-error`；unknown actual-provider-tokenization | current compatible sources 的 control candidate |
| `SNAP-12-B / STALE_STATE` | goal `rev17`，但 State `rev14`：`[EV-LOG-011, unresolved=SOURCE]`；facts `[build-4291-log, source-tree-9f2a]` | goal expects `rev17` while state is `rev14`；omits `EV-LOG-017`、`EV-SRC-009`；unknown staleness source | stale package；测试 Receipt 是否暴露 revision mismatch |
| `SNAP-12-C / CONFLICT_AND_BUDGET_PRESSURE` | `rev17`；capabilities `[80-global-tool-schemas]`；facts `[build-4310-log, source-tree-9f2a, stale-wiki-build-4291]`；history `[full-unbounded-history]` | conflict `BuildMenu.cs:42` vs `LegacyBuild.cs:88`；过大 tools/history 威胁当前源码片段与 output reserve；unknown truncation survivor | pollution / conflict package；测试选择与 fail-closed 标记 |

A 仅展示一个来源相容的候选包，不是“正确答案”的观察；B 只要求 Receipt 让 revision mismatch 可见，不推断真实原因来自 cache、Session merge 还是调用方；C 只把冲突与预算压力暴露出来，不实施 Provider truncation、污染诊断或 fail-closed runtime。设计输入之所以值得冻结，是因为未来若要验证，首先要能比较“模型看到了什么不同”，而不能把答案差异先写成已知结论。

三个 Snapshot 还说明：同一任务名不保证同一 Context。A 与 B 的目标文字都指向 `rev17`，但 B 的 State 和外部日志停在 `rev14`；若没有 State revision、omission 与 unknown 字段，两个 package 很容易在最终 Prompt 的自然语言里看起来相似。C 则不是“故意喂坏模型”的实验结果，它只把工程上应被显式承认的竞争关系摆在台面：全局能力、旧资料、无界历史与当前证据都想占用同一容量。未来的比较应先验证这些差异是否被忠实记录，不能越过这一层直接宣布模型发生了某种规律性的退化。

## 工程边界：不要让 Context 吞掉相邻系统

Context Assembly 是一次请求装配层，不是保存所有历史的总容器。下面这张表把本篇与依赖文章的职责切开。

| Object | 它负责什么 | 它不负责什么 |
|---|---|---|
| Prompt | 表达目标、约束、输出与失败语义 | 选择完整 Context、提供 current fact 或授予权限 |
| Tool Result | 一次工具执行的结果材料 | 自动变成永久 History 或 accepted State |
| History | 与当前 Step 相关的过程记录或摘要 | 取代当前 authoritative State |
| Session | 一次可追踪、恢复或回放的交互与执行边界；可拥有、引用或治理 history | 单次请求的完整 Snapshot；[OpenAI Agents SDK Sessions](https://openai.github.io/openai-agents-python/sessions/) 只是 product-scoped history implementation example |
| Memory | 跨时段保存或供应可用信息的职责 | 本篇不展开长期记忆、向量检索或 retrieval |
| Checkpoint | 保存恢复所需的控制事实 | 当前 Step 实际带入模型的完整 Context；见 [Long-running Agent]({{< relref "ai-empowerment/agent-engineering-11-long-running-agent.md" >}}) |
| Snapshot | 某一次 Step 的已选可见 package | Memory、Session 或可恢复执行位置 |
| Receipt | 来源、版本、选择、排除、冲突、未知与 trace ref 的课程记录 | Provider 内部完整重放器或行业标准 |

这种分层也决定了排查顺序：当回答错误，先查 Receipt 是否显示了错误 revision、被排除的关键事实、过期 capability 或不足的输出余量；不要先把 Session、Memory、Checkpoint、raw Result 和 Prompt 全部改一遍。若 Receipt 显示的装配已经合理，才有资格继续提出模型行为、任务合同或后续诊断问题。本篇停止在“记录并比较可见装配”，不进入具体 Compaction、长期 Memory、vector retrieval、私有源码或 Runtime 实现。

## 验证边界与 Learning Check

Receipt 的价值不是制造一份看似完整的日志，而是把能检查的和不能检查的都写出来。它不能证明 Provider 内部 tokenization、隐藏 system text、reasoning、server-loop 中间态或模型实际消耗顺序；应用侧 Trace 可能被关闭或脱敏，Provider 也可能加入已披露但不可重构的上下文。因此，“有 Receipt”不等于“已完整复现一次调用”。

审查一个 assembly 时，可以先问：每个 required contributor 有没有 source/version 和 authority？当前 State 是否与 Goal 同 revision？工具是否真在该 Stage 合法？冲突、遗漏、token 和 provider-managed unknown 是否显式？这些问题把设计 Proposal、产品合同和未来实验保持在不同证据层。

### Learning Check

1. 当前 Workflow State 没有进入请求，这是 Prompt 问题还是 Context 问题？
2. 80 个 Tool Schema 常驻，首先会挤占什么？
3. 只保存最终 Prompt，能解释每段材料的来源、版本和裁剪原因吗？

### 参考思路

1. 首先是 Context Contributor 的遗漏。应先检查 Receipt 的 selection、revision 和 disposition；不要以重写 Prompt 代替确认当前 State 是否被装配。
2. 它们会和 input、Tool Result 和 output reserve 共同占用模型相关容量。先按当前 Stage 选择 eligible tools；质量后果仍要放回具体 workload 验证。
3. 不能。最终 Prompt 无法天然携带 omitted set、冲突处理、source provenance 和 provider-managed unknown。Receipt 可以描述、审计和比较这些 application-visible facts，但不代表 Provider Context。

## 最短结论

`先审查这个 Step 的effective Context可能由什么构成，再讨论它为什么答错；应用只描述、审计和比较自己的Context Snapshot。`

## 参考资料

- [OpenAI Responses API：Create a model response](https://developers.openai.com/api/reference/cli/resources/responses/methods/create)（2026-08-21 检索；当前 request surface）
- [OpenAI Agents SDK：Tracing](https://openai.github.io/openai-agents-python/tracing/)（2026-08-21 检索；application-visible trace scope）
- [OpenAI Agents SDK：Sessions](https://openai.github.io/openai-agents-python/sessions/)（2026-08-21 检索；session history scope）
- [Anthropic：Context windows](https://platform.claude.com/docs/en/build-with-claude/context-windows)（2026-08-21 检索；model-specific window scope）
- [Anthropic：Tool use overview](https://platform.claude.com/docs/en/agents-and-tools/tool-use/overview)（2026-08-21 检索；tool schema / result 与 provider-managed addition scope）
- [Anthropic：Count tokens in a Message](https://platform.claude.com/docs/en/api/messages/count_tokens)（2026-08-21 检索；给定请求组件的预估 scope）
