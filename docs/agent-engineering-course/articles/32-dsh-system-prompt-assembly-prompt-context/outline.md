# Article 32 Outline

Status: `OUTLINE COMPLETE / AUTHOR DRAFT ELIGIBLE`

## 0. Article contract

- Article type: `原理型源码追踪文`。
- Working title: `System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成`。
- Core problem: 身份、persona、Host guidance、Tool guidance、变量、动态 policy、任务和历史来自不同 owner；如果只保存最终 prompt 字符串，就无法回答某个 Step 的模型请求到底由什么组成、为什么与上一个 Step 不同。
- Core thesis: `模型请求不是一段 prompt，而是 stable system、dynamic snapshot 与 durable history 在 Step 边界的汇合；能解释它，必须同时保留 Effective Assembly、Context Snapshot 与 request receipt。`
- Structure: `问题空间 -> 三通道抽象模型 -> DSH exact schema / order / scope / render / request path -> two-Step diff -> provenance / compaction boundary -> BuildPilot proposal -> verification method -> evidence boundary`。
- Evidence ledger: `15 Claims / 15 Evidence Cards = 13 CONFIRMED / 0 PARTIAL / 2 PROPOSAL / 0 BLOCKED`。
- Runtime boundary: 两次 request 来自真实 `AgentLoop` Step，但 terminal adapter 是 repo-owned in-memory `MockAdapter`；不是 real DeepSeek Provider、SDK/HTTP wire、模型行为、token 或 cost 证据。
- Future boundary: Article 33—44、BuildPilot implementation 与 Part VII 不启动。

## 1. Planned frontmatter and navigation

### 1.1 Standard frontmatter

```yaml
---
title: "System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成"
slug: "agent-engineering-32-dsh-system-prompt-assembly-prompt-context"
date: "2026-08-30T00:00:00+08:00"
description: "从两次真实 AgentLoop Step 的 MockAdapter request diff 出发，解释 DSH 如何把 PromptSection、PromptContext、变量、Tool 与 Session history 组成可解释的模型请求。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Context Engineering"
  - "System Prompt"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 330
weight: 3330
---
```

Frontmatter checks:

- title/description 不含未转义英文双引号，外层双引号合法；
- slug 与计划 published path 一致；
- series/order/weight 承接 Article 31；
- 不写超出证据的 real-provider、complete provenance 或 generic compaction 表述。

### 1.2 Top and bottom navigation

开头和结尾均放：

```md
> **上一篇**：[Profile、Bundle、Provider 与 Capability Seam]({{< relref "ai-empowerment/agent-engineering-31-dsh-profile-bundle-capability-seam.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})
```

结尾追加无链接提示：

```md
> **下一篇**：Loop、Turn 与 Step：AgentLoop 怎样推进一次运行（计划中，发布后再补链接）。
```

Article 33 尚未发布，本 transaction 不创建 future `relref`，也不提前写 Article 33 的 trace 结论。

### 1.3 Official pinned links

开篇固定研究对象时链接：

- tag: `https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1`
- commit: `https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc`

具体实现段按需要链接 commit-pinned files：

- `packages/core/system-prompt/src/index.ts`
- `packages/core/scope/src/store.ts`
- `packages/core/agent-loop/src/agent.ts`
- `packages/core/agent-loop/src/runtime-context.ts`
- `packages/core/session/src/index.ts`
- `packages/llm/llm/src/index.ts`
- `packages/llm/llm-deepseek/src/serialize.ts`

所有 GitHub file links 必须以完整 commit 固定，不使用 mutable `main`。

## 2. Opening: same system and tools, why did messages become 2 -> 5?

### 2.1 First-screen problem

用 Lab 中最可反驳的一组结果开场，而不是先列 API：

```text
Step 1 request: system unchanged / tools=[flip_mode] / messages=2
Step 2 request: system unchanged / tools=[flip_mode] / messages=5
```

第二次 request 新增：

1. assistant 的 `flip_mode` tool call；
2. tool result `mode flipped`；
3. 新的 complete runtime snapshot：`mode=write-enabled; tick=2`。

第一问：如果“context 就是 system prompt”，为什么 system byte-identical，模型可见输入仍然发生了可归因变化？

第二问：如果只保存最终 request 字符串或消息数组，如何知道 `mode` 来自哪个 contributor、在哪一层被重组、旧 snapshot 是否仍在 history？

### 2.2 Three coarse beliefs to break

- `System prompt 是一段静态模板`：错在忽略 Section provider、scope shadow、waterfall 与逐 Step render。
- `所有 Context 都走 PromptContext`：错在任务、time/tmux、workspace instructions 与 Session history 使用不同 channel。
- `final provider request 就是完整 provenance receipt`：错在 `renderPrompt()` 已把 system-section names flatten 掉。

### 2.3 Short judgment and evidence boundary

在第一屏给出短判断：

> 一次模型请求不是把几段字符串拼起来，而是多条拥有不同 scope、排序、更新与持久化语义的输入通道，在一个 Step 边界收敛。

随后固定：official tag / full commit / external clean fixture；`15 / 15 Claims`、`13 CONFIRMED / 2 PROPOSAL`；request receipt 终止于 `MockAdapter.requests`；identity check 只证明研究版本，不证明机制。

Claims: `32-C01`, `32-C11`。

## 3. Problem space: a flat prompt loses four boundaries

### 3.1 Source boundary

同一句 model-facing guidance 可能来自 Harness identity、deployment / agent persona、Host surface、Tool guidance、dynamic sandbox / approval state、task、instruction 或 history。最终文本相同，也不能推断它们拥有相同 owner、权限或更新机制。

### 3.2 Time boundary

区分：

- 每个 Step 都重新 assemble/render，但通常保持稳定的 system lane；
- 每个 Step 重新采样、变化时追加新 complete snapshot 的 dynamic lane；
- 由 Session surface 保留、append 或 replacement 的 durable history lane。

强调 `PromptSection.text` 也可以是 callback，所以 “stable” 是通道/缓存预期，不是 type-level 永不变化保证。

### 3.3 Scope boundary

- global contribution；
- ancestor scope；
- nearest agent scope；
- same-layer duplicate 与 cross-scope same-name shadow 是两种不同语义。

不要使用“后写覆盖前写”概括全部行为。

### 3.4 Evidence boundary

| Layer | 最小对象 | 能证明 | 不能单独证明 |
|---|---|---|---|
| Contribution | name/order/provider/scope owner | 注册契约 | 当前 composition 已加载 |
| Effective Assembly | sections/contexts/tools/variables | 本 Step 的排序、shadow、provider 求值与 waterfall 后结果 | 已成功 render/request |
| Rendered boundary | flat system + named context sections | 插值与 model-facing text | 全来源与 transform ledger |
| Request receipt | provider/model/system/tools/messages/sessionId | terminal adapter 实收输入 | real SDK/HTTP、模型行为、token/cost |

Claims: `32-C07`, `32-C08`, `32-C09`, `32-C10`, `32-C12`。

## 4. Abstract model: three input lanes converge at one Step boundary

### 4.1 Core diagram

```text
Stable system lane
  PromptSection providers + prompt variables + tool guidance
    -> PromptAssembly.sections / variables / tools
    -> renderPrompt()
    -> request.system + request.tools

Dynamic snapshot lane
  PromptContext providers
    -> PromptAssembly.contexts
    -> renderContextSections()
    -> RuntimeContextProjection.project()
    -> sourced user/message snapshot when changed or retention removed

Durable history lane
  claimed task + instructions + prior assistant/tool/context events
    -> Session surface
    -> deriveMessages()
    -> request.messages

Step boundary
  route + rendered system + ordered tools + derived messages
    -> frozen GenerateOptions
    -> terminal adapter
```

### 4.2 Three artifacts, not one

定义全文反复使用的三个对象：

1. `Contribution`：某个 owner 提供的一项候选输入；
2. `Effective Assembly`：scope/order/provider/waterfall 都已求解，但 system 尚未 flatten 的 per-Step 状态；
3. `Request Receipt`：terminal adapter 实际拿到的 normalized request。

动态通道还有一个专属持久对象：`Context Snapshot`，它在 `source.sections` 中保留 `{name,text}` 的窄 provenance。

### 4.3 Why lanes must stay separate

- stable lane 适合 byte/hash 对比与可缓存 prefix，但仍要逐 Step 重组；
- dynamic lane 需要 current complete snapshot 与 supersedes 语义；
- history lane 要尊重 Session surface、tool call/result 与 compaction replacement；
- terminal semantics 必须说明终止哪个 lane，不能从 system section 泛化到整个 turn。

这一节先建立引擎/框架无关模型，避免文章退化成 DSH API 清单。

Claims: `32-C07`, `32-C12`, `32-C13`。

## 5. Concrete implementation I: exact DSH contracts and vocabulary

### 5.1 Exact fields

| Type | Exact fields | 说明 |
|---|---|---|
| `PromptSection` | `name`, `order`, `text`, `complete?` | `text` 可为 string 或 assembly-time callback |
| `PromptContext` | `name`, `order`, `text` | 动态 model-context contribution |
| `AssembleContext` | `scope?`, `signal?`；Agent package 扩展可选 `agent` | 手写 scope 不自动提供 agent variables |
| `PromptAssembly` | `sections`, `contexts`, `tools`, `variables` | waterfall 前后使用的四通道对象 |
| `ContextSnapshotSection` | `name`, `text` | durable dynamic snapshot 的 named attribution |

必须明确：pinned tree 没有独立 `PromptProvider` interface。这里的 provider 是角色：Section/Context 的 `text` callback、VariableProvider、ToolProvider，不能为了行文方便发明统一现有 API。

### 5.2 Representative owner/channel map

| Concern | Representative owner | Current channel |
|---|---|---|
| Harness identity | `SystemPrompt` constructor | `PromptSection` |
| deployment / scoped persona | core config + persona preset | same-name Section shadow |
| Harness checkout / Web surface | boot / web bundle | `PromptSection` |
| provider/model/cwd | AgentLoop | variables |
| Tool guidance + schema | tool plugins + Tools | Section + `tools[]` |
| sandbox / approval | policy services | `PromptContext` |
| time / tmux | pre-step listeners | sourced user message, not PromptContext registry |
| workspace instructions | instruction reconciler | durable sourced user message |
| task | inbox claim | durable user message |
| prior transcript | Session surface | `deriveMessages()` |

写出反例：`time-context` 虽然是动态 context，却不注册 `PromptContext`；“所有 context 统一经过一个 current API”不成立。

Claims: `32-C02`, `32-C08`。

## 6. Concrete implementation II: order, scope, duplicates, override and complete

### 6.1 Assembly sequence

按照 source 顺序叙述，不能用模糊“合并配置”替代：

1. evaluate global variables；
2. farthest-to-nearest scope overlays 覆盖 same-name variable；
3. name-merge Section/Context，nearest scoped entry shadows global；
4. additive collect global + matching scoped Tool providers；
5. Sections 按 `order ASC, code-unit name ASC`；
6. Contexts 只按 `order ASC`，equal-order 保持 stable effective-map insertion order；
7. evaluate Section/Context text providers；
8. tools 按 explicit `toolOrder` 或 canonical name 排序；
9. scope-filtered `system-prompt/assemble` authoritative waterfall；
10. restore active complete section / runtime-context suppression invariant；
11. return Effective Assembly。

### 6.2 Conflict matrix

| Situation | Pinned behavior | 容易写错成什么 |
|---|---|---|
| same-name Section/Context/Variable in same layer | registration throws | silent append / last wins |
| same name across global and scoped layer | nearest scope shadows | duplicate error |
| equal-order Sections | name tie-break | registration order |
| equal-order Contexts | stable insertion order; no name tie-break | copied Section comparator |
| additive tool providers return duplicate names | no registry dedup proven | automatically rejected |
| waterfall adds/replaces fields | return is authoritative | registry dump is final assembly |

### 6.3 Complete is lane-local terminal semantics

- `completeSections.length > 1`：assembly fails；
- 唯一 `complete` contribution 先被 resolve；
- waterfall 仍运行；
- waterfall 后恢复该 exact contribution 为唯一 system section；
- contexts/tools/variables 仍可保留 transform 结果；
- `complete` 不结束 request、Step、turn 或 Agent run。

短句：`complete` 终止的是 system-section lane 的竞争，不是 Agent Loop。

Claims: `32-C03`, `32-C04`, `32-C05`。

## 7. Concrete implementation III: bad variables fail later than assembly

### 7.1 Variable contract

- registration name 必须匹配 `/^[a-z][a-z0-9_]*$/`；
- `provider`, `model`, `cwd` 每次 assembly 从 active agent/session 读取；
- interpolation 在 `renderPrompt()` / `renderContextSections()` 阶段发生；
- unknown、registered-but-undefined、malformed group 均 fail；
- substituted value 不二次扫描，因此 value 内 `{{sneaky}}` 保持 literal；
- waterfall 可在 render 前添加/覆盖 `assembly.variables`。

### 7.2 Three direct negatives

必须逐条保留 exact boundary 与结果：

1. duplicate same-layer section：registration error，包含 `prompt section "duplicate-demo" is already registered`；
2. bad variable name：registration error，包含 `invalid prompt variable name "Bad-Name"` 与 regex；
3. unknown reference：assembly 可返回未插值文本，但 render error，包含 `unknown prompt variable "{{missing}}" in section "unknown-variable-demo"`。

探针进程 `exit 0` 仅因为 expected errors 被捕获并打印，不能写成这三项操作成功。

### 7.3 Engineering implication

只保存 “assembly succeeded” 会制造 readiness 假阳性。最小 gate 应区分：

```text
REGISTERED -> ASSEMBLED -> RENDERED -> REQUEST RECEIVED
```

Claims: `32-C04`, `32-C06`。

## 8. Concrete implementation IV: exact per-Step call path

### 8.1 Registration to Effective Assembly

```text
SystemPrompt.section/context/variable/tools
  -> ScopedLayers / PromptLayer registries
  -> ReactLoopAgent.preStep()
  -> SystemPrompt.assemble(assembleContextFor(agent, signal))
  -> system-prompt/assemble waterfall
  -> PromptAssembly
```

保留失败实验的教学价值：手写 `{scope: agent.scope}` 缺少 `agent`，persona 中 `{{model}}` 变成 undefined；真实主链必须使用 `assembleContextFor(agent, signal)`。scope selection 不等于 agent-owned variable context。

### 8.2 Effective Assembly to Session surface

```text
assembly.contexts
  -> renderContextSections()
  -> joinContextSections()
  -> RuntimeContextProjection.project()
  -> optional sourced UserMessage

claimed inbox messages + runtime snapshot
  -> agent/pre-step waterfall
  -> Session user/message append
```

说明 time/tmux/instructions 可以在 pre-step channel 追加自己的 sourced message，因此不是 PromptContext registry 的别名。

### 8.3 Session surface to frozen request

```text
assembly.sections -> renderPrompt() -> flat system
Session surface -> deriveMessages() -> ordered messages
assembly.tools -> ordered schemas
Agent route + agent/request waterfall
  -> LlmRuntime.prepareCall()
  -> canonicalHeader(route/system/tools)
  -> deep-frozen GenerateOptions
  -> MockAdapter.requests in this Lab
```

补充 source-only downstream path：`LlmRuntime -> DeepSeekAdapter -> serializeRequest -> fetch /chat/completions` 已在源码闭合；但本轮 Lab 没有走它，所以正文只能写 source path，不能写 real-provider runtime success。

Claims: `32-C09`, `32-C10`。

## 9. The centerpiece: replayable two-Step Effective Assembly and request diff

### 9.1 Fixture and boundary

```text
LlmRuntime -> SessionStore -> SystemPrompt -> ToolRuntime
           -> AgentRegistry -> AgentLoop -> MockAdapter
```

强调：这是 repo-owned real AgentLoop 的 two-Step execution；`MockAdapter` 是 terminal in-memory provider substitute。没有 credential、外网、真实模型、token 或 cost。

### 9.2 Effective Assembly diff

| Field | Step 1 | Step 2 | Meaning |
|---|---|---|---|
| sections | identity, persona, alpha, zeta | identical | Section equal-order 按 name |
| contexts | zeta=`read-only`, alpha=`tick=1` | zeta=`write-enabled`, alpha=`tick=2` | Context providers per-Step resample |
| variables | mock/mock/trace-agent | identical | effective values stable in fixture |
| tools | `flip_mode` | identical | visible set stable |

保留 assembly SHA：

- Step 1 `0420ABBCE3215D69C33564A5575944777FE1C57C27D4A1B0978B417271584551`
- Step 2 `17AABD2AA449A9EDC23A349A15CC34DC880591CDB954DEBC3859F99229011473`

解释 hash 差异来自 dynamic contexts，不代表 system/tools 变化。

### 9.3 Request receipt diff

保留 request SHA：

- Step 1 `72326D5189BF92BC67C41745A3F61358291B670E0CDB07D0972927C9120B78CA`
- Step 2 `5705EE4D9EF5B6A6F3654D92D3EFC8D058D2C301AC3B2721F5976C4FB735AE89`

Stable fields：provider/model `mock/mock`；rendered system byte-identical；tools 都是 `['flip_mode']`。

Changed fields：

```diff
messageCount: 2 -> 5
+ assistant "switching" + flip_mode tool call
+ tool result "mode flipped", isError=false
+ new @deepseek-ai/dsh-system-prompt snapshot
  zeta-context = mode=write-enabled
  alpha-context = tick=2
```

必须说明：request 2 仍保留旧 snapshot；普通 Step change 不 rewrite 旧 event，而是 append 带 supersedes 文本的新 complete snapshot。最新 snapshot 是 effective dynamic state，但两个事件仍可归因。

### 9.4 What it proves / does not prove

Proves：real AgentLoop 每 Step 重新 assembly；stable system/tools 与 dynamic/history 可以独立变化；`source.form='snapshot'` 和 ordered `source.sections` 保留 PromptContext 窄 provenance；messages `2 -> 5` 可逐项解释。

Does not prove：real DeepSeek Provider、SDK/HTTP payload、模型行为、token、cost、quality、latency；所有 deployment 的 system 都 byte-stable；normalized SHA 覆盖 AbortSignal、timestamps、generated IDs 或 wire serialization。

Claims: `32-C03`, `32-C07`, `32-C09`, `32-C11`。

## 10. Effective Assembly provenance: what survives and what is lost

### 10.1 Existing narrow provenance

- pre-render `PromptAssembly.sections[]` / `contexts[]` 保留 name 与 resolved text；
- `ContextSnapshotSection` 与 durable `source.sections` 保留 dynamic context 的 ordered `{name,text}`；
- Tool schema 保留 tool name；
- request/header 保留 route、flat system 与 tools；
- task/instruction/time/tmux message 各自保留 source metadata。

### 10.2 Flattening loss

在 `renderPrompt()` 后：section spans/names 不再存在于 final `system`；variables 只有 effective map；anonymous tool provider identity 不进入 request；waterfall 没有统一 transformation ledger；scope-shadow loser 没有进入最终 request/header。

所以 final provider request 是操作输入 receipt，不是完整 assembly provenance receipt。

### 10.3 Safe wording for bounded absence

限定搜索结论：pinned production tree 没有 general `IContextContributor` 或统一 `Receipt` type/object。只能写“在选定 frozen tree 与 search scope 中未找到”，不能推断官方动机、永久 absence 或未来版本。

Claims: `32-C12`。

## 11. Compaction: narrow PromptContext re-projection, not generic invariant replay

### 11.1 Exact source/runtime path

```text
Session replacement event
  sourceEventSeqs includes retained runtime snapshot seq
    -> RuntimeContextProjection.retained = null

next preStep
  -> reassemble current PromptContexts
  -> render complete current snapshot
  -> project() sees no retained equal snapshot
  -> append new sourced snapshot
```

如果 active context 已清空，则发 explicit cleared marker。

### 11.2 Stable system behaves differently

Stable system 不从 compacted history “再注入”；它本来就每 Step 重新 assemble/render 到 request header。byte-equal 时 header 可沿用，变化时记录 change。

### 11.3 Generalization ceiling

- 当前闭合的是 system-prompt-owned PromptContext snapshot；
- time/tmux/agent-instructions 有各自 scheduling/durable semantics；
- 任意被 compaction 移除的 task/plugin message 不会被这条路径自动再生；
- 不得写成“所有 invariants 在 compaction 后自动 re-inject”。

### 11.4 Owner runtime result

focused AgentLoop command：`1 file / 5 passed / 5 selected / 51 skipped`，覆盖 stable system + variable、bad-variable recovery、changed snapshot、replacement 后 unchanged snapshot re-emission、active context 变空后的 clear marker。

Claims: `32-C13`。

## 12. Test evidence and retained failures

### 12.1 Owner suites

```text
system-prompt owner suite: 3 files / 68 passed / 68
focused AgentLoop owner run: 1 file / 5 passed / 5 selected / 51 skipped
direct negatives: 3 expected errors captured
fixture after commands: pinned HEAD / clean
```

`68/68` 覆盖 order、equal-order、duplicate、scope shadow、bad/undefined/malformed variable、waterfall、complete conflicts、lifecycle 与 invariant validation。

### 12.2 Retained harness mistakes

1. Windows 下 `pnpm exec tsx -` exit `1` / `tsx is not recognized`；改为 non-writing `node --import tsx/esm --input-type=module -`；这是 launcher observation，不是 DSH failure。
2. 手写 `{scope: agent.scope}` 漏 `agent`，导致 `{{model}}` undefined；最终在 real loop waterfall 捕获 assembly。这说明人工近似 context 不能冒充 request path。

### 12.3 Reproduction discipline

- frozen repo/tag/SHA/host tool versions；
- external read-only fixture；
- inline harness 不写 DSH source；
- expected / observed / interpretation / does-not-prove 四栏；
- 最后重新检查 exact HEAD 与 clean tree。

Claims: `32-C01`, `32-C04`, `32-C06`, `32-C13`。

## 13. Engineering risks from the trace

### 13.1 Order collision

Section 与 Context 的 equal-order rule 不同；插件若假定通用 name tie-break，会误判 effective precedence。建议显式 sparse order，receipt 记录最终位置与 tie-break basis。

### 13.2 Snapshot accumulation

普通变化 append full superseding snapshot。消费者必须尊重 newest snapshot / current Session surface，不能扫描第一条 owner message 当 current truth。

### 13.3 Late-render failure

unknown/undefined variable 在 render 才失败。只记录 registry 或 assembly success 会把未发出的 request 错判为 ready。

### 13.4 Waterfall and override opacity

registry state 不等于 Effective Assembly；waterfall 是 authoritative transform。若没有 transform receipt，最终值无法反推来源与中间决策。

### 13.5 Provenance and secret risk

保留 provenance 不等于记录所有 plaintext。receipt 应保存安全摘要/hash、owner 与 decision；credential、secret variable 或敏感 Context 需要 redaction policy。

### 13.6 Compaction over-generalization

PromptContext retained-snapshot re-projection 不能当成 generic invariant framework。每个可再生 input 都要明确 owner、freshness、replace semantics 与 negative test。

Claims: `32-C06`, `32-C12`, `32-C13`。

## 14. BuildPilot transfer: IContextContributor + Receipt stays proposal-only

### 14.1 State the absence first

先写清：DSH pinned tree 没有 general `IContextContributor` / unified `Receipt` abstraction。BuildPilot 设计不是把现有 DSH type 改名，也不是已实现迁移。

### 14.2 Proposed minimum interface

```ts
// PROPOSAL ONLY — not current DSH API and not implemented.
interface IContextContributor {
  readonly id: string
  readonly lane: 'stable' | 'dynamic' | 'history'
  contribute(input: ContributionInput): Promise<ContextContribution>
}
```

一个 interface 不能抹平 durability differences，所以 `lane` 是合同字段，不是注释。

### 14.3 Proposed safe receipt

```ts
// PROPOSAL ONLY
interface AssemblyReceipt {
  readonly requestId: string
  readonly contributions: readonly {
    id: string
    source: string
    scope: string
    order: number
    transforms: readonly string[]
    outputHash: string
    decision: 'included' | 'shadowed' | 'empty' | 'rejected'
  }[]
}
```

候选约束：same-scope id unique；shadowed loser 也留 decision；stable/dynamic/history 分槽；variable replacement、filter、redaction、compaction replacement 记录 transform；terminal 只终止声明 lane；receipt 不保存 secret plaintext；future acceptance tests 覆盖 duplicate/override/bad variable/complete/compaction。

### 14.4 Decision ceiling

- `IContextContributor`: `PROPOSAL`；
- `AssemblyReceipt`: `PROPOSAL`；
- BuildPilot ADR/code/runtime/migration: `NOT STARTED`；
- Article 38 / Part VII: `NOT STARTED`。

Claims: `32-C14`, `32-C15`。

## 15. Hands-on verification: audit one request assembly without calling a real model

### 15.1 Freeze the source boundary

记录 official repository、tag、full SHA、fixture、OS、Node/pnpm/PowerShell；前后检查 HEAD/tag/clean。明确不读取 credential、不发网络。

### 15.2 Capture all three artifacts

对每个 Step 保存：

1. Effective Assembly：ordered section/context names、tools、safe variables/hash；
2. Context Snapshot：combined text + ordered `source.sections`；
3. terminal adapter request：route、system hash、tool names、message count、source-aware diff。

### 15.3 Force one controlled dynamic change

通过 deterministic mock tool 更新一个 dynamic provider，保证出现第二 Step；比较 stable fields 与新增 history，避免把 nondeterministic model output 混入实验。

### 15.4 Run the three negatives

- same-layer duplicate；
- invalid variable registration；
- unknown variable render。

分别记录 failure stage，不能只保存 exception 文本。

### 15.5 Verify compaction narrowly

用 owner test 或 deterministic surface replacement：retained snapshot 被 shadow 后 current snapshot re-emits；active context 清空后 clear marker emits；不把结果推广到其他 plugin messages。

### 15.6 Minimal observation table

| Observation | 可以说 | 不能说 |
|---|---|---|
| registration exists | contribution contract present | current scope used it |
| Effective Assembly captured | selected scope/order/providers resolved | render/request succeeded |
| MockAdapter received request | AgentLoop request mechanics reached terminal mock | real Provider/network/model worked |
| `source.sections` present | PromptContext parts attributable | flat system provenance complete |
| replacement owner test passed | PromptContext snapshot re-projects | all invariants auto-reinject |

## 16. Evidence Boundary

### 16.1 Established

- frozen source identity and clean fixture；
- exact PromptSection/PromptContext/AssembleContext/PromptAssembly/ContextSnapshotSection fields；
- no standalone pinned `PromptProvider` type；
- deterministic Section/Context/Variable/Tool assembly with differing tie rules；
- same-layer duplicate rejection and cross-scope shadow；
- lane-local `complete` semantics；
- strict variables and three direct negative boundaries；
- representative multi-owner, multi-channel contributor map；
- per-Step assembly -> render -> Session -> frozen request source path；
- real AgentLoop + MockAdapter two-Step request diff，messages `2 -> 5`；
- system/tools/route stable in selected trace, dynamic/history attributable；
- partial provenance and flat-system / transform-ledger loss；
- narrow PromptContext compaction-aware re-projection；
- `68/68` system-prompt owner tests and focused `5/5` AgentLoop tests；
- BuildPilot interface/receipt candidate as proposal only。

### 16.2 Not established

- real DeepSeek Provider runtime、SDK/HTTP wire receipt 或 model behavior；
- credential availability/value、network、latency、token、cost；
- every installed Profile/plugin contributor or all deployments；
- every PromptSection byte-stable across Steps；
- general system-section provenance after flatten；
- generic compaction re-injection for task/time/tmux/instructions/arbitrary plugin messages；
- official intent behind missing general receipt abstraction；
- BuildPilot ADR、implementation、runtime、Article 33 conclusion、Article 38 或 Part VII start。

## 17. Claim-to-section matrix

| Claim | Status | Planned sections | Evidence Card | Wording ceiling |
|---|---|---|---|---|
| `32-C01` pinned identity / clean fixture | `CONFIRMED` | §§2, 12, 16 | `32-E01` | identity 不证明机制 |
| `32-C02` exact contracts / no standalone PromptProvider | `CONFIRMED` | §5 | `32-E02` | pinned source only |
| `32-C03` deterministic assembly algorithm | `CONFIRMED` | §§6, 9 | `32-E03` | selected algorithm/runtime observation |
| `32-C04` duplicate / scope shadow / waterfall scope | `CONFIRMED` | §§6—7, 12 | `32-E04` | 不写 unrestricted last-wins |
| `32-C05` complete section semantics | `CONFIRMED` | §6 | `32-E05` | not turn terminal |
| `32-C06` strict variables / bad-variable boundaries | `CONFIRMED` | §§7, 12—13 | `32-E06` | selected negatives + owner coverage |
| `32-C07` stable system vs dynamic snapshot lane | `CONFIRMED` | §§3—4, 9 | `32-E07` | stable is lane expectation, not immutable type |
| `32-C08` representative multi-owner map | `CONFIRMED` | §§3, 5 | `32-E08` | representative, not exhaustive |
| `32-C09` per-Step convergence to request | `CONFIRMED` | §§3, 8—9 | `32-E09` | source path + MockAdapter only |
| `32-C10` request header and Session history ownership | `CONFIRMED` | §§3, 8 | `32-E10` | not external persistence/wire success |
| `32-C11` two-Step messages `2 -> 5` diff | `CONFIRMED` | §§2, 9 | `32-E11` | mock mechanics, not real model |
| `32-C12` narrow provenance / flattened loss | `CONFIRMED` | §§3—4, 10, 13 | `32-E12` | bounded absence search only |
| `32-C13` PromptContext replacement re-projection | `CONFIRMED` | §§4, 11—13 | `32-E13` | not generic reinjection |
| `32-C14` BuildPilot `IContextContributor` | `PROPOSAL` | §14 | `32-E14` | no current DSH API / no implementation |
| `32-C15` BuildPilot AssemblyReceipt | `PROPOSAL` | §14 | `32-E15` | future design only |

Coverage target: `15 / 15 Claims`, `15 / 15 Evidence Cards`, no orphan CONFIRMED claim and no PROPOSAL written as source fact.

## 18. Learning Check

1. 为什么 system byte-identical 仍然可能得到不同的 model request？
2. Stable system、Dynamic PromptContext 与 durable history 分别进入 request 的哪个字段？
3. `PromptSection`、`PromptContext`、`PromptAssembly` 与 `ContextSnapshotSection` 的 exact fields 是什么？
4. 为什么不能在 pinned DSH 中发明一个现有 `PromptProvider` interface？
5. 为什么 time-context 是“动态上下文”，却不是 `SystemPrompt.context()` contributor？
6. same-layer duplicate 与 cross-scope same-name shadow 有什么不同？
7. equal-order Section 与 equal-order Context 的排序规则为什么不能混用？
8. `system-prompt/assemble` waterfall 为什么使 registry dump 不等于 Effective Assembly？
9. `complete: true` 到底终止什么，为什么不等于 request/turn terminal？
10. invalid variable name、unknown reference 与 undefined value 分别在哪个边界失败？
11. 手写 `{scope: agent.scope}` 为什么不能替代 `assembleContextFor(agent, signal)`？
12. 两次 request 的 `messages 2 -> 5` 具体新增了什么？哪些字段保持稳定？
13. 为什么 request 2 仍然保留旧 runtime snapshot？
14. `source.sections` 保留了什么 provenance？`renderPrompt()` 又丢了什么？
15. 为什么 MockAdapter receipt 不能写成 real DeepSeek Provider call？
16. compaction replacement 后，PromptContext 怎样被窄范围 re-project？
17. stable system 为什么不是“从 history 被重新注入”？
18. `68/68` 与 focused `5/5` 分别覆盖什么证据层？
19. BuildPilot 为什么需要显式 lane 的 `IContextContributor` 候选？
20. Receipt 为什么既要记录 shadow/reject decision，又不能保存 secret plaintext？

## 19. Closing

先回扣开场：messages `2 -> 5` 不是“prompt 神秘变化”，而是三条通道按各自语义推进后的可解释结果——system/tools 保持稳定，assistant/tool history 增长，PromptContext append 新的 complete snapshot。

再压缩证据边界：Effective Assembly 解释本 Step 选择了什么；Context Snapshot 为动态贡献保留窄 provenance；MockAdapter request receipt 证明 AgentLoop 实际发给 terminal mock 什么；任何一层都不能单独冒充另两层，更不能冒充 real-provider wire evidence。

最短结论：

> 不要问“最终 prompt 是什么”；要问哪些 owner 以什么 scope、顺序和更新语义进入了本 Step，以及 assembly、snapshot 与 request 三份收据能否互相对上。

Bottom navigation follows §1.2；Article 33 只作无链接 next-owner 提示。

## 20. Author draft acceptance checklist

- [ ] 第一屏从 two-Step request diff 开始，不从 TypeScript API 开始。
- [ ] 问题空间、抽象模型、具体实现、工程边界四层完整。
- [ ] 15 Claims 与 15 Cards 全覆盖，`13 CONFIRMED / 2 PROPOSAL` 不串级。
- [ ] exact fields/symbols/call path、order/scope/duplicate/override/complete/bad-variable 全部出现。
- [ ] 多来源 owner/channel map 清楚，time-context 反例不被抹平。
- [ ] real AgentLoop + terminal MockAdapter 边界至少出现于开篇、中心实验、Evidence Boundary。
- [ ] system/tools/route stable、messages `2 -> 5` 与两个 request SHA 保留。
- [ ] `68/68`、focused `5/5`、三个 direct negatives 保留 exact interpretation。
- [ ] Effective Assembly provenance、Context Snapshot narrow provenance 与 flattened-system loss 分账。
- [ ] compaction 只写 PromptContext narrow re-projection，不泛化。
- [ ] BuildPilot `IContextContributor + Receipt` 明标 `PROPOSAL ONLY`，并写 DSH pinned tree 无 general abstraction。
- [ ] Article 31 relref 与课程索引合法；Article 33 只无链接提示。
- [ ] official tag/commit 与 source links 都 pinned。
- [ ] Learning Check 存在，末尾有一句最短结论。
- [ ] 不启动 Article 33—44、Article 38、Part VII 或 BuildPilot implementation。
