---
title: "Context Debugging：Packing、Compression、Pollution 与可重建性"
slug: "agent-engineering-13-context-debugging"
date: "2026-08-22"
description: "用 application-visible Context Snapshot 与 Receipt 定位 Assembly、Packing、污染、压缩和截断故障，并明确可审计与可重建的边界。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "Context Engineering"
  - "Debugging"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 140
weight: 3140
---

> **上一篇**：[Context Engineering：每一个 Step 到底应该看到什么]({{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}})

> **下一篇**：[Working Memory 与 Investigation State：当前任务正在想什么]({{< relref "ai-empowerment/agent-engineering-14-working-memory-investigation-state.md" >}})

昨天，构建诊断 Step 收到一份 Unity 编译日志。第一条可行动错误很清楚：`BuildMenu.cs` 中某个变量未定义，编译器报 `CS0103`。它按任务要求给出错误位置、证据引用，并把尚未确认的根因保留为 `UNKNOWN`。

今天，团队用同一句 Prompt 又跑了一次：

> 只根据本次构建证据定位第一个可行动失败点；无法确认时写 UNKNOWN；不要修改项目。

Prompt 的 digest 没变，Step 名字也没变，结果却变成了：`build succeeded`。

最自然的反应，是继续改 Prompt：再加一句“请严格分析”，补一个 `CS0103` 示例，或者要求模型“不要忽略错误”。但这些动作都绕过了一个更靠前的问题。

**先不要改 Prompt，先看这个 Step 当时看到了什么。**

昨天的 Step 可能拿到了 build 4310 的当前日志、`rev17` 的 State 和与它匹配的源码片段；今天的 Step 却可能只拿到 build 4291 的旧成功摘要，或者同时拿到了 `Build failed.` 与 `Build succeeded.` 两条未裁决记录。还有一种可能：材料最初都选对了，但在摘要、预算适配或请求物化时，真正需要的 `CS0103` Evidence 被裁掉了。

这几种故障最终都可能表现成一句错误回答，但修法完全不同。任务合同写错，才是 Prompt bug；任务合同没变，当前 Step 实际可见的材料却错了、旧了、越界了或被变换坏了，首先是 Context diagnosis。两者可能共存，却不能靠最终回答质量单点判定。

## 模型面对的是被打包后的 view，不是项目的全部真相

上一篇 Article 12 解决的是：**这个 Step 应该看到什么？** 它把一次请求拆成 Select、Order、Scope、Fit Budget，并把应用最终组装出的可见视图称为 `application-visible Context Snapshot`，再用 `Context Receipt` 记录来源、版本、选择、排除、冲突、预算与未知。

本篇继续追问：**当这个 view 错了，第一处分叉在哪里？**

同一个 `build succeeded` 可以来自当前权威构建，也可以来自 obsolete Plan、无关 history，甚至来自冲突中的一侧。句子一样，不代表 provenance 一样。类似地，`prompt_digest` 相同只说明任务文本可比，不说明 contributor set、revision、scope、order、representation、transform、budget 或 materialized bytes 相同。

因此，排查时至少要保留三个候选方向：

- **Prompt bug**：目标、约束、失败语义或输出合同本身表达错误。
- **Context bug**：合同可能正确，但本 Step 的来源、revision、scope、冲突、representation、budget 或 materialization 偏离合同。
- **Consumption candidate**：应用侧 Snapshot 与 Receipt 通过冻结检查，结果仍违反 deterministic contract。没有独立 runtime evidence 时，注意力、推理或 Provider 内因都只能保持 `UNKNOWN`。

这也是为什么“多带一些 Context”不是自动修复。相关且组织正确的材料当然可能有帮助；但旧 Plan、重复规则、无关 history 和不可信材料也会与当前 Evidence、工具 schema、输出余量竞争同一个有限容量。这里能安全保留的判断只有：**更多 context 不是通用可靠性保证**，而不是“越多越差”。

## 九种故障架构：先看材料怎样坏，再贴标签

Context Debugging 最容易写成一张名词表。更有效的方式，是从真实故障架构出发：先冻结预期，再找 application-visible artifact 的第一处差异，最后才决定标签。

### 1. Required contributor 根本没进来

当前 `CS0103` 日志被任务合同列为 required Evidence，但 candidate set 和 selected set 中都没有它，Receipt 里也找不到带理由的 omission record。

这不是“模型没认真看”，而是材料根本没有进入本 Step。首查项应是 contributor ID、disposition 与 omission reason。若应用按事前冻结的策略明确排除某项，它是 intentional omission；若 required item 无记录地消失，才进入 `Missing` 候选。

### 2. State 已经过期

Goal 要求处理 `rev17`，但装入的 State summary 仍标 `rev14`。自然语言看起来可能只差几个字符，控制含义却完全不同。

排查不需要猜旧值来自 cache、session merge 还是某个后台同步。先保存：

```text
required_revision = rev17
source_revision   = rev14
source_ref        = state-summary
```

当 frozen required revision 与实际 source revision 可比时，才有资格判 `Stale / REVISION_MISMATCH`。

### 3. Capability 过期，或作用域错误

History 里曾经出现过一个可写工具 schema，不代表当前 Stage 仍允许调用它。真正的能力边界可能已经切到 read-only，tenant、task、step、environment 或 time scope 也可能不同。

这类故障经常同时命中 `Stale` 与 `Wrong Scope`：schema revision 旧了，作用域也不匹配。修法不是在 Prompt 里重抄工具列表，而是回到当前 Host registry、policy view 与 frozen scope rule。

### 4. Evidence 指向旧构建

今天排查 build 4310，却装入 build 4291 的日志或旧 source-tree locator。它和 stale State 类似，但对象是外部 Evidence。若旧日志同时带着“构建成功”，还可能与当前失败记录形成冲突。

关键不是“新资料总比旧资料真”，而是任务要求哪个 investigation version。历史版本可能正是回归任务的合法输入；只有先冻结 scope，`Stale` 才有可审计含义。

### 5. Pollution 与 overpacking

obsolete Plan、old tool result、unrelated history、duplicate rule 和 untrusted material 被一起塞进请求，看上去像“信息充分”，实际却让当前 Evidence 的位置、预算和权重更难解释。

`Pollution` 不能由“回答不好”倒推。它需要一个事前冻结的 relevance / trust policy：哪些 contributor 本应排除或降权，却仍进入 selected set。`Overpacked` 也不能由 token 多直接判定；只有越过 budget policy、侵占 output reserve 或触发既定 transform threshold 时，才命中对应 predicate。

### 6. Conflict 被静默抹平

同一 Step 中存在两条 in-scope Evidence：

```text
build-job-41 -> Build failed.
build-job-42 -> Build succeeded.
```

如果没有 frozen resolution rule，正确的 application-visible 状态不是任选一边，而是保留双方 provenance、revision、order 和 `UNRESOLVED`。一旦 summary 只留下“构建成功”，问题发生在冲突保存或后续 representation，而不是模型神秘地“偏向乐观答案”。

### 7. Compression 改写了主张强度

压缩前的材料是：

```text
EV-1: SUPPORTED
EV-2: CONTRADICTS
root cause: UNKNOWN
```

压缩后变成：

```text
Root cause confirmed.
```

此时需要比较的不是两个字符串像不像，而是 frozen invariants：uncertainty 是否仍在、conflict 是否保留、provenance 是否可追、claim strength 是否被非法升级。具体 loss 必须由 pre/post bytes 或等价可验证材料支撑；没有 pre-transform evidence，就不能只凭最终句子宣布 `Compression Loss`。

### 8. Budget / truncation 删错了东西

一个稳健的 budget fitter 应先保留 required P0/P1 与 output reserve，再移除 optional history。若 required Evidence 已经放不下，应该返回 explicit fail-closed，而不是悄悄生成一份缺证据的 Snapshot。

这里要分开四类事件：应用主动省略、应用侧 trim、Provider 文档定义的 truncation / transform，以及 hard limit。它们可能得到相似的最终 view，但 actor、stage、control、event 与错误语义不同，不能被一个 `context_shortened=true` 吞掉。

### 9. Receipt 还在，原内容已经不在

Receipt 保存了 ref、digest、order 与 disposition，但 original bytes 已删除，locator 也不可解析。审查者仍能回答“当时有哪些 contributor、顺序和 digest”，却不能从 SHA-256 反推出原文。

这正是 `AUDITABLE != RECONSTRUCTABLE`：metadata audit 仍可进行，byte reconstruction 的前提却不成立。诚实的结论是 `NOT_RECONSTRUCTABLE` 或 `UNKNOWN`，不是根据相似文本补出一份“应该就是这样”的历史。

这九种架构不是另一套行业分类。它们只是把工程事故摊开。真正用于后续诊断的标签，需要有统一、可观察的 predicate。

## 从 Packing 链找到第一处分叉

本课程把 application-visible Context 的诊断面写成下面这条链：

```text
candidate sources
  -> selection
  -> scope filtering
  -> precedence / ordering
  -> representation
  -> compression / summarization
  -> budget fitting
  -> request materialization
  -> application-visible Context Snapshot
```

这是一种审查顺序，不是 Provider 内部统一 pipeline。每个变换点都应尽量留下可比较的 application-side artifact：candidate IDs、source / version、scope decision、order、pre/post digest、transformer version、budget ledger、output reserve 与最终 Snapshot digest。

### 三层故障定位

为避免把所有故障都叫“Context 不好”，本课程提出一个 **COURSE PROPOSAL**：把诊断先分成 Assembly、Packing 与 Consumption candidate 三层。

| Layer | 何时命中 | 典型修复 | 不该越过的边界 |
|---|---|---|---|
| Assembly Failure | candidate discovery、authority、scope、revision 或 conflict resolution 已使输入材料违约 | 修 source、registry、scope、revision 或 conflict policy | 不直接归因模型注意力或 Provider 截断 |
| Packing Failure | pre-transform 材料正确，但 order、representation、compression、budget、trim 或 materialization 使 Snapshot 违约 | 修 transformer、ordering、budget fitter、fail-closed contract | 不声称 Provider 内部采用同样算法 |
| Consumption candidate | application-visible Snapshot / Receipt 检查通过，deterministic contract 仍失败 | 增加独立 runtime / eval evidence，或保持 UNKNOWN | 不从最终回答单点推断注意力、推理、hallucination 或 Provider 内因 |

课程把 Assembly/Packing/Consumption 分层；fixture 能定位若干应用侧差异，模型/Provider 内因仍 UNKNOWN。

### 八类可观察标签

同样地，下面八类标签也是 **COURSE PROPOSAL**，不是 Provider / SDK 官方 taxonomy。它们非互斥、非穷尽，只能由 frozen predicate 触发。

| Label | 最小 observable predicate | 反误判边界 |
|---|---|---|
| `Missing` | required contributor 未进 candidate / selected，或 required field 缺失 | 有 reason 的 intentional omission 不算 Missing |
| `Stale` | revision / observed-at 落后于 frozen required revision / authoritative state | 历史版本可能正是 task scope |
| `Wrong Scope` | tenant / user / task / step / environment / time scope 不匹配 | scope rule 必须事前冻结 |
| `Conflict` | in-scope contributors 对同一 key / decision 不兼容且未裁决 | 多来源不自动等于冲突 |
| `Pollution` | obsolete、duplicate、out-of-scope 或 untrusted item 违反 frozen policy 仍被纳入 | 不从回答质量倒推 |
| `Overpacked` | 超过 frozen budget、侵占 reserve 或触发 local threshold | token 多本身不是质量失败 |
| `Compression Loss` | required provenance、scope、uncertainty、conflict、ordering、negative evidence 或 locator 在 transform 后不可验证或被强化 | Provider 文档不证明具体字段必丢 |
| `Truncation` | application 或 Provider-documented capacity policy 丢 item，或 hard limit 失败 / 停止 | 与 compaction、intentional omission 分账 |

同一个事件可以同时是 Stale 与 Wrong Scope，也可以先 Pollution、后 Overpacked、再 Truncation。Receipt 应记录原子事实与多个 diagnosis refs，而不是为了单选分类丢掉过程。

一个最小事件记录至少要能回答：谁做了变换、发生在哪一层、使用哪个 mechanism / version、影响哪些 contributor、为何 include / omit、pre/post digest 是什么、预算和 reserve 怎样变化、还有哪些 unknown。课程建议把 intentional omission、application trim、Provider-documented transform / truncation 与 hard limit 分开落账；Lab 05 只确认了 Case F 的 local dispositions。

## 一条可执行的 Context Debugging 协议

只有术语还不够。下面是一条 **COURSE PROPOSAL / deterministic local protocol**，目标是把一次“回答漂移”收窄成可检查、可停止、可回归的 application-visible 事件。

### 第一步：冻结失败 Step

记录 `run_id`、`step_id`、workflow State revision、task contract、Prompt digest、Provider / API / model / feature scope。Prompt digest 是比较字段之一，不是完整 identity。

### 第二步：建立 known-good control

选择一份通过 frozen invariants 的 Snapshot / Receipt。Control 必须是 artifact，不是“上次答案看起来不错”。没有 control，就先冻结 required contributor、scope、revision、conflict rule、budget policy 与 output reserve。

### 第三步：先比较 contributor，再比较自然语言

对 source、revision、scope、authority、trust、disposition 与 order 做结构化 diff。Missing、Stale、Wrong Scope、Pollution 和 Conflict 通常能在这一层先暴露。

### 第四步：沿 packing chain 比较

检查 representation、pre/post digest、transformer version、budget / reserve、omitted set 与 request materialization。这里定位 Compression Loss、Overpacked 或 Truncation。

### 第五步：按 predicate 多标签诊断

每个标签都附 actor、stage、mechanism、reason 与 evidence refs。不从“答案差”倒推原因，也不强迫一个事件只能有一个标签。

### 第六步：只落到最窄的 failure layer

Assembly 或 Packing 有证据才落层。两层都通过而结果仍失败时，只记录 `CONSUMPTION_CANDIDATE`；如果缺少真实 runtime / eval evidence，内部原因仍是 `UNKNOWN`。

### 第七步：只重建到前提满足的层级

先 audit metadata，再检查 retained bytes / locator、parser / schema、rule engine / policy。任何一级缺前提，就在该级停止。后文会给出 Reconstruction Ladder。

### 第八步：局部修复并回归

只修第一处分叉，保存原 failure，然后用 frozen input 重跑 normalized Snapshot、Receipt 与 verdict。真实模型行为需要独立 eval，不能由这个 deterministic protocol 代替。

调试记录不必变成新的庞大平台。最小输出可以是：

```text
failing_step_identity
control_snapshot_ref / failing_snapshot_ref
first_divergence_stage
atomic_observations[] / diagnoses[]
failure_layer = ASSEMBLY | PACKING | CONSUMPTION_CANDIDATE | UNKNOWN
highest_reconstruction_level
unsupported_claims[]
repair_scope / regression_refs
```

它也必须允许失败：没有 revision baseline 就不判 Stale，没有 pre-transform bytes / invariant 就不判具体 Compression Loss，只有 output diff 就不判 Provider cause。required Evidence 无法放入 usable input 时，应 explicit fail closed，而不是交付 silent Snapshot。

## Lab 05：把协议放进一个固定、可重复的夹具

前面的协议要想从“看起来合理”变成工程资产，至少要证明两件事：它能在已知故障上给出预期的 application-visible verdict；同一输入重跑时，产物不会被时间、进程号或随机 ID 悄悄改变。

Lab 05 为此建立了 `lab05-fixture-v1`。它运行在 Windows 10 build 19045、X64 / win-x64、.NET SDK `10.0.301`、Runtime `10.0.9`、`net10.0`，只使用 BCL。Provider、模型、网络和凭据全部为 `NONE`。

它的执行边界很窄：frozen contributors 进入本地 Runtime CLI，经 deterministic selector、conflict-preserving packer、可选的 named bad compressor、budget fitter，生成 application-visible Snapshot、Receipt、diagnostics、transform event、budget result 与 reconstruction verdict；独立 Spec runner 只通过 public CLI 和 normalized artifacts 验证行为。

```text
frozen contributors + policies
  -> local Runtime CLI
  -> Snapshot / Receipt / diagnostics
  -> independent behavioral verifier
```

这里的 `BAD_COMPRESSOR_V1` 是故意写坏的本地 fault injection seam，不模拟任何 Provider algorithm。Case F 的 budget units 也是人工整数，不是 OpenAI / Anthropic tokenizer、billing token 或真实 output limit。

### 先保留真正的 RED，再谈 GREEN

Lab 没有先实现行为再补测试。independent Specs 与 input-only fixture 先冻结，Runtime 只提供可编译、可启动但返回 `NOT_IMPLEMENTED` 的 shell。

成功完成 Release build 后，mandatory RED 的 Spec exit=`1`，Runtime shell exit=`3`，Cases A–G `7 / 7` 都因缺失 public behavior 而失败。这是行为 RED，不是编译失败。

最小实现后的第一次 build 确实又遇到一个工程失败：三个 `JsonValue.Create` method-group site 报 `CS0411`。原始 compiler output 被保留，改成 explicit lambdas 后，Release build 才达到 `0 warnings / 0 errors`。随后 GREEN exit=`0`，`15 / 15` assertions 通过；RED 后没有削弱 Spec，也没有改写 fixture bytes 来迎合结果。

执行记录还保留了三个恢复过的 tooling 问题：non-escalated command helper 的 `helper_unknown_error`、初次 wrapper 没有保存 start timestamp、第一版 PowerShell secondary-audit helper 尝试了无效的 `ReadOnlySpan<byte>` cast。这些失败既没有被藏掉，也没有被包装成 Lab 的行为 RED。

### Cases A–G 的实际 Observation

| Case | Frozen role / fault | Observed public behavior |
|---|---|---|
| `A — Baseline Good Context` | current Goal / State、正确 Evidence / capability、有界 history | emits `GOOD_CONTEXT`；required contributors、ordering ledger 与 output reserve 都被保留 |
| `B — Stale Context` | required State=`rev17`，summary=`rev14` | `STALE + REVISION_MISMATCH`；保留 `expected=rev17`、`actual=rev14`、source ref 与 provenance |
| `C — Pollution` | old tool result、obsolete plan、unrelated history | `POLLUTION`；识别 `C-OLD-TOOL`、`C-OBSOLETE-PLAN`、`C-UNRELATED-HISTORY`，不读取模型回答质量 |
| `D — Conflict` | `Build failed.` 与 `Build succeeded.` | `CONFLICT_UNRESOLVED`；保留双方与 `build-job-41 / build-job-42` provenance，不自动选边 |
| `E — Compression Loss` | `EV-1 SUPPORTED + EV-2 CONTRADICTS + UNKNOWN root cause` 经 named transform | `BAD_COMPRESSOR_V1` 精确输出 `Root cause confirmed.`；verifier 检出 `UNCERTAINTY`、`CONFLICT`、`PROVENANCE`、`CLAIM_STRENGTH` loss |
| `F — Truncation / Budget` | optional pressure 与 required overflow | optional history 先删，P0/P1 与 four output units 保留；required overflow 返回 `REQUIRED_EVIDENCE_BUDGET_EXCEEDED / FAIL_CLOSED`，Snapshot 显式为 `ABSENT` |
| `G — Reconstruction Boundary` | Receipt 有 ref/digest/order/disposition，bytes 与 locator 不可用 | metadata=`AUDITABLE`；bytes=`NOT_RECONSTRUCTABLE`；记录 `ORIGINAL_BYTES_ABSENT`、`LOCATOR_UNRESOLVABLE`、`DIGEST_NOT_CONTENT`；Provider internal=`UNKNOWN_UNSUPPORTED` |

A 提供 control，B–G 分别让 stale、pollution、conflict、named compression loss、budget fail-closed 与 reconstruction ceiling 有了真实 application-visible Observation。Missing、Wrong Scope、Overpacked 和 V4 event separation 没有作为已执行 variant 出现在 Observation summary 中，不能把 mandatory A–G 写成 full taxonomy coverage。

### 两次 fresh process 的 repeatability

formal run A 与 run B 各由 fresh Runtime process 完成，每个 manifest 列出 58 个 normalized files。compare 覆盖 59 个文件，包括 manifest 与 independent Spec result；relative file set、byte length、direct bytes、per-file SHA-256 与 aggregate SHA-256 全部相同。

aggregate SHA-256 为：

```text
621cde0ec3c1ed7b7f6334dac4d0187b522625f37e220957e48b6dcac3bd3f50
```

最终 closure 按固定顺序重跑 restore、Release build、GREEN、run A、verify A、run B、verify B 与 compare，`8 / 8` command exit 都是 `0`。

这说明 frozen offline protocol 在同一 Windows/.NET fixture 可重复；不外推生产/跨平台。它证明的是 normalized application-visible artifacts 的本地 conformance，不是真实模型准确率、Provider 内部 Context，也不是生产系统的 availability 或 determinism。

## Engineering / Evidence Boundary：工程上能落什么，证据能说到哪里

前面先把问题、机制和 Lab 走完，现在再统一收束工程取舍与证据边界。

### 让 Context regression 具备可比较证据

第一，版本化的不应只有 Prompt。Step identity、required revision / scope、authority / trust、conflict rule、packer / compressor / budget policy version 都要一起冻结。否则下一次 diff 只能比较两段最终文本，无法解释材料从哪里开始变化。

第二，transform 必须有 pre/post 边界。digest 能验证“候选 bytes 是否相同”，却不能反推出已经遗失的内容。若系统确实需要 byte reconstruction，就要保存 immutable bytes，或者保存可解析 locator、retention contract 与 canonicalization version。

第三，把 omission、application trim、Provider-documented transform / truncation 与 hard limit 分开记录。至少保留 actor、stage、mechanism、control / version、disposition 和 reason。课程建议分开落账；Lab 只确认 Case F local dispositions。

第四，把 conflict 与 `UNKNOWN` 当作正式数据，而不是等待 summary 消除的噪声。provenance、negative evidence、locator 和 claim strength 都可以成为 transform invariant。required Evidence 放不下时，先保留 output reserve，然后 explicit fail closed。

第五，为 application-visible packer 建 deterministic regression fixture：保留真实 RED、fault injection、GREEN、fail-closed path 和 fresh-process compare。它能防止装配逻辑回归，但不替代真实模型 eval。

### Provider mechanism 与“更多 Context”的来源范围

截至 `2026-08-22`，公开资料足以支持两条收窄后的判断。

第一，**更多 context 不是通用可靠性保证**。Liu 等人的 [Lost in the Middle，TACL 2024](https://aclanthology.org/2024.tacl-1.9/) 只覆盖论文列出的 multi-document QA、key-value retrieval 与 2023-era models；Shi 等人的 [Irrelevant Context，ICML 2023](https://proceedings.mlr.press/v202/shi23a.html) 只覆盖 GSM-IC 与论文列出的 models / prompts。它们提供的是有限反例，不证明 2026 当前模型有相同降幅，更不证明“Context 越多越差”。

第二，truncation、compaction、context editing 与 hard limit 是不同且 versioned 的机制，不能统一写成“Context 被截了”。当前 [OpenAI Responses create](https://developers.openai.com/api/reference/cli/resources/responses/methods/create) 文档中的 deprecated truncation control，在 `auto` 超窗时从会话开头丢 item，`disabled` 默认返回 400；[OpenAI Compaction](https://developers.openai.com/api/docs/guides/compaction) 的示例以 `gpt-5.3-codex` 展示 server-side compaction，[Compact response](https://developers.openai.com/api/reference/java/resources/responses/methods/compact) 则是 request-selected model 的 compact endpoint。Anthropic 的 [Context windows](https://platform.claude.com/docs/en/build-with-claude/context-windows) 显示 overflow / stop 行为依页面列出的模型代际与 feature 而异；[Context editing](https://platform.claude.com/docs/en/build-with-claude/context-editing) 使用 beta header `context-management-2025-06-27` 与features `clear_tool_uses_20250919` / `clear_thinking_20251015`，[Compaction](https://platform.claude.com/docs/en/build-with-claude/compaction) 使用 beta header `compact-2026-01-12` 与 feature `compact_20260112`，页面示例模型为 `claude-opus-5`。

这些都是 retrieved-date 下的 Provider / API / model / feature scope。它们不证明某个生产 request 已触发对应机制，也不证明任何 Provider compaction 会复现 Lab Case E。

### Receipt 的能力上限

Context Receipt 只负责 **describe / audit / compare application-visible Context Snapshot**。它不是 Provider request trace、complete effective Context、hidden system text、reasoning trace、final internal token sequence 或 full-token replay。

[OpenAI token counting](https://developers.openai.com/api/docs/guides/token-counting) 与 [Anthropic token counting](https://platform.claude.com/docs/en/build-with-claude/token-counting) 能改善 model-scoped budget 观测，但 count 不是 content / provenance receipt。[OpenAI Agents SDK Python hosted tracing docs](https://openai.github.io/openai-agents-python/tracing/)（retrieved 2026-08-22，package version 未固定）在启用、未脱敏且可用时可以补充 input / output evidence；tracing 可关闭、可能排除敏感内容，在 ZDR 条件下也不可用，因此不能假定它稳定存在或完整。

该 fixture Receipt 可 describe/audit/compare app-visible Snapshot；不保证 Provider/full-token reconstruction。

### Reconstruction Ladder

为了避免把 metadata audit 误叫“重放”，本课程提出下面这套 **COURSE PROPOSAL**。各级前提独立，不会因为有 L0 Receipt 就自动拥有 L1–L3。

| Level | Minimum prerequisite | 最多支持 | 前提不足时 |
|---|---|---|---|
| `L0 Metadata audit` | identity、scope、revision、disposition、transform、digest | describe / audit / compare fields | `NOT_AUDITABLE / UNKNOWN` |
| `L1 App-visible bytes` | retained immutable bytes，或 resolvable locator + retention + canonicalization | fixture-scoped byte reconstruction / equality | digest 不能反推内容；`NOT_RECONSTRUCTABLE` |
| `L2 Semantic` | L1-equivalent material + frozen parser / schema / invariant | fixture-scoped facts、conflict、unknown | 不承诺完整 semantic equivalence；保持 UNKNOWN |
| `L3 Decision` | frozen rule engine、inputs、policy version | deterministic application decision replay | 不重放真实模型输出或内部推理 |
| `L4 Provider-internal / full-token` | Provider 提供完整、可验证的接口与证据 | 本篇未满足 | `UNKNOWN / UNSUPPORTED` |

Case G 只证明一条 negative boundary：digest metadata 可以 audit，却不能恢复不存在的 bytes。如果另一个系统保留了 immutable bytes 或可解析 locator，L1 verdict 可以不同。G 证明 digest metadata 可 audit 但不能恢复 bytes；L2/L3 未完整证明，L4 UNKNOWN/UNSUPPORTED。

### 本篇停止在哪里

本篇不把 Prompt Workshop 或“请更认真”当 Context repair，不证明真实模型的 accuracy、hallucination、attention、reasoning 或 context rot，也不证明 Provider 内部截断算法、hidden Context 或 complete token sequence。

Lab 05 只覆盖一个 offline deterministic fixture，不外推到 production、cross-provider、cross-platform、large-scale、distributed、security 或 multi-tenant behavior。

讨论也停在单次 Step 的 application-visible Context Debugging：不展开 Article 14 的 Working Memory lifecycle、mutation、persistence；不展开 Articles 15–16 的 Long-term / Project Memory、Vector DB、Embedding、Retriever、Reranker 或 RAG architecture。

## Learning Check

1. Prompt digest 没变，但昨天 Snapshot 含 current `CS0103` Evidence，今天只含旧 `build succeeded` summary。第一步应该冻结什么？为什么不能先改 Prompt？
2. summary 删除 `UNKNOWN`、source revision 与 unresolved conflict 后写成 `Root cause confirmed.`。哪些 invariant 失守？能否据此指控 Provider compaction？
3. Receipt 只有 ref、digest、order，original contributor 与 locator 已不存在。为什么 L0 仍可 audit，L1 却不成立？
4. required Evidence 已超过 usable input，系统仍返回 silent Snapshot。它位于哪一 failure layer？正确的 disposition 是什么？

### 参考思路

1. 先冻结 run / Step / State revision、required contributors 与两份 Snapshot / Receipt；Prompt 相同不代表 Context 相同。
2. uncertainty、conflict、provenance 与 claim strength 都失守；若 Observation 只来自 `BAD_COMPRESSOR_V1`，结论只能留在该 fixture，不能指控 Provider。
3. metadata 能说明“当时记录过什么”，digest 能比较候选 bytes，却不能恢复遗失内容；因此是 `AUDITABLE`、`NOT_RECONSTRUCTABLE`。
4. 这是 Packing / budget path。保留 output reserve，返回 explicit `REQUIRED_EVIDENCE_BUDGET_EXCEEDED / FAIL_CLOSED`，不要交付缺证据的 Snapshot。

## 最短结论

`先不要改 Prompt；先冻结失败 Step 的 application-visible Snapshot，沿 Assembly / Packing 逐层比较，证据不足就停在 UNKNOWN。`
