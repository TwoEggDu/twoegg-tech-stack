# Article 32 Research

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## 1. Merged boundary and inputs

本篇是原理型源码追踪文，问题不是“DSH 的 system prompt 有哪些句子”，而是：身份、宿主、任务、变量、Tool guidance、动态状态与历史来自不同 owner 时，它们如何在每个 Step 收敛成一次可解释的模型请求。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Fixture identity check：HEAD 与 exact tag 一致，`git status --short` 为空。

本轮合并的 durable inputs：

- `repository-map.md`：production symbols、真实 scope/order/conflict rules、contributor channels、Provider boundary 与限定 provenance search；
- `call-path.md`：registration -> Effective Assembly -> render -> Session surface -> frozen GenerateOptions -> LLM adapter 的 exact source arrows；
- `experiments/prompt-assembly-trace.md`：两次真实 Agent Loop Step 到达内存 `MockAdapter` 的 request receipts、三个直接负例、`68/68` system-prompt owner tests 与 `5/5` focused AgentLoop tests。

证据纪律：

- source path 不等于 runtime request；本轮 request receipts 来自 repo-owned in-memory `MockAdapter`，不等于 real model/provider。
- `MockAdapter.requests` 能证明 final normalized request mechanics，不能证明外部 SDK/HTTP wire、模型行为、token usage 或 cost。
- `renderPrompt()` flatten 后，system-section provenance 已丢失；`PromptContext` 的 `source.sections` 只是 dynamic snapshot 的窄 provenance，不是全 assembly receipt。
- pinned production tree 没有 general `IContextContributor` / `Receipt`；这条 absence 只按 Source Investigator 的限定 symbol/tree search 表述，不推断设计意图。
- BuildPilot 的 `IContextContributor + Receipt` 只作 `PROPOSAL`，Part VII 未授权、未实现。

## 2. Problem Space：一段 prompt 为什么无法解释一次请求

把最终 system 字符串当成唯一事实，会同时丢掉四种边界：

1. **来源边界**：同一句 guidance 是身份、部署 persona、宿主 surface、Tool 插件，还是 waterfall 后处理器提供的？
2. **时间边界**：它是稳定 section，还是每个 Step 重新采样的 runtime context / user-role snapshot？
3. **scope 边界**：它是 global 默认值，还是某个 Agent scope 对同名条目的 shadow？
4. **证据边界**：注册表、已解析 assembly、渲染后 header 与 provider request 是四层不同证据。

Pinned source 还揭示一个容易写反的事实：任务与历史不是 `PromptSection`。用户任务先作为 inbox message 被 claim，进入 `user/message` durable event，再由 Session surface 派生进 request `messages`；Workspace instructions、time/tmux readings 等也可以走 user-message/history 通道。它们与 system prompt 在 `buildRequest()` 才汇合。

## 3. Abstract Model：三条输入通道、一个 Step 边界

```text
Stable prompt lane
  PromptSection providers + tool schema providers + prompt variables
    -> SystemPrompt.assemble(scope, signal)
    -> PromptAssembly { sections, contexts, tools, variables }
    -> renderPrompt() -> system

Dynamic snapshot lane
  PromptContext providers
    -> renderContextSections()
    -> joinContextSections()
    -> RuntimeContextProjection.project()
    -> optional sourced user/message appended after claimed inputs

Durable history lane
  user task + instructions + prior assistant/tool/context messages
    -> Session surface
    -> deriveMessages()
    -> request.messages

Step request boundary
  header.config + rendered system + ordered tools + derived messages
    -> frozen GenerateOptions
    -> adapter stream
```

这个模型先区分“Contribution”“Effective Assembly”“Request Receipt”三层：

| Layer | Minimum fields | Source can prove | Still missing without Lab |
|---|---|---|---|
| Contribution | kind/name/order/text provider/scope owner | 注册契约与静态 owner | 该 Profile 是否实际加载 |
| Effective Assembly | `sections[]`, `contexts[]`, `tools[]`, `variables{}` | 排序、shadow、provider 求值算法 | 目标 composition 的实际 dump |
| Rendered boundary | `system`, context snapshot sections | 插值、空项过滤、拼接规则 | 两 Step exact bytes / hash |
| Request receipt | provider/model/system/tools/messages/sessionId | `buildRequest()` 字段汇合路径 | adapter 实收 request、diff、real provider 情况 |

## 4. Concrete source contract

### 4.1 Pinned Section、PromptContext 与 provider fields

`packages/core/system-prompt/src/index.ts` 给出真实字段：

- `PromptSection = { name, order, text, complete? }`
- `PromptContext = { name, order, text }`
- `text` 的 provider 形态是 `string | ((context: AssembleContext) => string)`；此版本没有一个名为 `PromptProvider` 的独立接口。
- `AssembleContext = { scope?, signal? }`，并由 Agent package merge-extend 可选 `agent`。
- `PromptAssembly = { sections, contexts, tools, variables }`；section/context 在 assembly 时已求值但尚未插值。
- `ContextSnapshotSection = { name, text }`；进入 message source 时可带 `form: 'snapshot', sections`，为动态快照保留 named contribution。

这里的 “Provider” 应按字段语义理解：Section/Context 的 `text` 回调、ToolProvider 与 VariableProvider 都是每次 assembly 求值的 contribution provider；不能凭文章术语发明一个源码中不存在的统一类型。

### 4.2 Ordered assembly

源码装配顺序为：

1. 解析 global + scope-chain variables，scope chain 从远到近覆盖同名值；
2. 按 name 合并 Section/Context，scoped same-name shadow global；
3. 收集 global 与 matching scoped tool providers；
4. Section 按 `order ASC`，同 order 按 code-unit `name ASC`；Context 只按 `order ASC`，相同 order 依赖现代 stable sort 保持 effective-map 注册/插入顺序，没有 name tie-break；
5. 求值 section/context text providers；
6. Tool 先按显式 `toolOrder` 或 name canonicalize；
7. 运行 scope-filtered `system-prompt/assemble` waterfall；
8. 如存在唯一 effective `complete` section，恢复其 waterfall 前 exact text，作为唯一 system section；runtime contexts/tools/variables 仍保留。

`renderPrompt()` 随后才做严格变量插值、过滤空 section，并用两个换行连接；`renderContextSections()` 对 context 做同一插值与空项过滤，`joinContextSections()` 添加 supersedes 前言。

### 4.3 Conflict、override 与 terminal semantics

- 同一 layer 内的 duplicate Section/Context/Variable 在注册时抛错，不会静默叠加。
- global 与 scoped layer 可有同名条目；scope assembly 以最近 scoped 条目 shadow global，这不是 same-layer duplicate。
- waterfall 返回值对 sections/contexts/tools/variables 原则上 authoritative，也允许 short-circuit；若启用 invariant companion，返回 assembly 的 duplicate/空名/非法字段会被拒绝。
- 单个 `complete: true` 是 system-section 的终结语义：waterfall 仍运行，但最终唯一 section 被恢复为注册时已求值的 exact contribution；多个 effective complete sections 直接失败。
- `complete` 不终止 Context、Tool、Variable 装配，也不终止 Agent turn；不能把它写成 request/turn terminal。

### 4.4 Variable semantics and bad-variable boundary

- 合法注册名匹配 `/^[a-z][a-z0-9_]*$/`。
- `provider`、`model`、`cwd` 由 AgentLoop 注册为 variables，并从当前 `context.agent` 读取。
- reference 在 render 阶段解析；unknown、registered-but-undefined、malformed complete group 都抛错。
- 替换后的 value 不二次扫描，所以 value 内的 `{{sneaky}}` 保持 literal。
- waterfall 可以在 render 前添加或覆盖 `assembly.variables`；因此最终变量值不仅来自 registry，receipt 应区分 original provider 与 transform。

### 4.5 Contributor map：不是所有 context 都走同一 API

| Concern | Representative source owner | Channel | Stability expectation |
|---|---|---|---|
| Harness identity | `SystemPrompt` constructor, `harness:identity` | PromptSection | stable while config/plugin set unchanged |
| Deployment identity/persona | `deployment:persona`, scoped preset may shadow | PromptSection | stable per effective Agent scope |
| Harness checkout / Web surface | `addHarnessSourceSection`, `app:web-surface` | PromptSection | host/composition dependent |
| Variables | AgentLoop `provider/model/cwd` providers | PromptAssembly variables | evaluated per assembly; may change by Agent/session |
| Tool guidance | individual tool plugins + Tools schema provider | PromptSection + `tools[]` | stable while visible tool set/config unchanged |
| Sandbox/approval state | `sandbox:policy` order 110, `approval:policy` order 115 | PromptContext -> system-owned snapshot user message | dynamic, current complete snapshot |
| Time/tmux state | pre-step listeners | sourced user message, not PromptContext registry | dynamic and plugin-scheduled |
| Workspace instructions | agent-instructions pre-step reconciliation | sourced durable user message | baseline plus changed scopes |
| User task | inbox claim | durable `user/message` | per turn/step input |
| Conversation history | Session surface / `deriveMessages()` | request `messages[]` | append/replace history |

因此“System Prompt Assembly 与 PromptContext”不能被压成一个字符串 builder：稳定 instruction prefix、dynamic current snapshot 和 durable conversation history 使用不同 owner 与更新语义。

## 5. Stable / Dynamic Context and two-Step request diff

Lab 使用 production service chain `LlmRuntime -> SessionStore -> SystemPrompt -> ToolRuntime -> AgentRegistry -> AgentLoop -> MockAdapter`，让第一步 mock response 调用 `flip_mode`，把 dynamic `mode` 从 `read-only` 改为 `write-enabled`，第二步正常停止。两次真实 Agent Loop Step 都到达 terminal in-memory `MockAdapter`；没有 credential、网络、token 或 cost path。

### 5.1 Effective Assembly observations

| Field | Step 1 | Step 2 | Interpretation |
|---|---|---|---|
| sections | `harness:identity, deployment:persona, alpha-section, zeta-section` | identical | equal-order Section 按 name，稳定 lane 未变 |
| contexts | `zeta-context=mode=read-only; alpha-context=tick=1` | `zeta-context=mode=write-enabled; alpha-context=tick=2` | equal-order Context 保持 zeta 后 alpha 的注册顺序并重新求值 |
| variables | `provider=mock; model=mock; persona_name=trace-agent` | identical | per-assembly effective values 未变 |
| tools | `flip_mode` | identical | visible tool set/order 未变 |

Normalized Effective Assembly SHA-256：Step 1 `0420ABBCE3215D69C33564A5575944777FE1C57C27D4A1B0978B417271584551`；Step 2 `17AABD2AA449A9EDC23A349A15CC34DC880591CDB954DEBC3859F99229011473`。Hash 不同来自 dynamic contexts，不代表 system/tools 改变。

### 5.2 Final MockAdapter request diff

两步 rendered `system` byte-identical，tool list 均为 `['flip_mode']`，route 均为 `mock/mock`。Request 1 normalized SHA-256 为 `72326D5189BF92BC67C41745A3F61358291B670E0CDB07D0972927C9120B78CA`，包含 `2` 条 messages：原始 user task + 第一次 complete runtime snapshot。

Request 2 normalized SHA-256 为 `5705EE4D9EF5B6A6F3654D92D3EFC8D058D2C301AC3B2721F5976C4FB735AE89`，messageCount 从 `2 -> 5`：

```diff
+ assistant/mock: "switching" + tool-call flip_mode
+ tool result: "mode flipped", isError=false
+ @deepseek-ai/dsh-system-prompt snapshot:
+   zeta-context = mode=write-enabled
+   alpha-context = tick=2
```

第二个 request 仍保留第一个 snapshot；普通 Step change 不重写旧 event，而是 append 一个带 “supersedes earlier” 语义的完整新 snapshot。`source.form='snapshot'` 与 ordered `source.sections[{name,text}]` 保留 dynamic contribution 的 exact attribution。

这闭合了三通道模型：stable Section/Tool/Variable 进入不变 header lane，变化的 PromptContext 进入新 durable snapshot，assistant/tool 历史随 Session surface 一起增长。它只证明 DSH assembly/request mechanics with MockAdapter，不证明真实 DeepSeek Provider、wire request、模型输出质量、token 或 cost。

## 6. Effective Assembly provenance and transformations

当前源码已保留部分 provenance：

- pre-render assembly 的 Section/Context 有 `name`；
- dynamic context message source 可保留 exact ordered `{ name, text }` sections；
- Tool schema 保留 name；
- request/header durably 保存 rendered `system`、tools 与 route config；
- user/instruction/time/tmux messages 带各自 `source`。

但这些并不构成一个统一的 Effective Assembly receipt：

- rendered `system` 是扁平字符串，不携带逐 section span/source；
- variables 只有 effective map，没有 provider owner、旧值、override chain；
- waterfall 可以 mutate/replace/short-circuit，却没有通用 transformation ledger；
- registry order 与 scope-shadow decision 没有被 request/header 逐项记录。

Source Investigator 已闭合限定 production-tree search：pinned DSH 没有 general `IContextContributor` 或统一 `Receipt` 类型/持久化对象。可确认的边界是：

- Effective Assembly 捕获可在 render 前保留 section/context names；
- Context Snapshot 通过 `source.sections` 持久保留 dynamic context 的窄 provenance；
- `renderPrompt()` 返回 flat string 后，final `GenerateOptions.system` 与 request/header 无 section names；
- Tool schema 保留 tool name，但 anonymous tool-provider identity 不进入 request；
- waterfall transform 没有通用 transformation ledger。

因此可以确认“有局部 provenance、无 general receipt”，但不能把限定 absence 推断为官方动机或永远不会增加的能力。

## 7. PromptContext compaction re-injection：narrow source/runtime closure

Pinned source 中存在动态 PromptContext 的 compaction-aware 再投影路径：

1. `RuntimeContextProjection` 跟踪最新 retained system-prompt-owned user message seq；
2. 当 replacement surface event 的 `sourceEventSeqs` 包含该 seq 时，把 retained 状态置为 `null`；
3. 下一次 `preStep()` 重新 assemble 当前 contexts；
4. `project()` 因 retained 不再等于当前 snapshot，生成新的完整 snapshot user message。

这说明“当前版本绝对没有 compaction 后重注入机制”不成立；但其范围必须限定为 system-prompt-owned runtime-context snapshot。Stable system prompt 本来就在每个 Step 重渲染进 request header，不是被 compaction 从 history 删除后再注入。Time/tmux/agent-instructions 各有独立调度与 durable semantics，也不能自动归入这条通用机制。

Focused AgentLoop owner run exit `0`，`5/5` selected tests passed（另有 `51` skipped by filter），覆盖：stable system/variable 到 adapter request、bad variable 阻断 request 后可修复、changed context 新增 snapshot、replacement 移除 retained snapshot 后 unchanged current context 被重新发出、active context 变空时发出 cleared marker。

这闭合的是 `RuntimeContextProjection` 的窄机制，不是 “任意 compaction 后重注入所有 invariant”：stable system 每 Step 独立重组；time/tmux/instructions 有各自调度；被 summary 替换的任意 task/plugin message 不会被这条路径自动再生。

## 8. Negative cases, owner tests and retained failures

### 8.1 Three direct negatives

| Label | Observed boundary | Exact outcome |
|---|---|---|
| duplicate section, same layer | registration | `prompt section "duplicate-demo" is already registered ...` |
| invalid variable name | registration | `invalid prompt variable name "Bad-Name" (must match /^[a-z][a-z0-9_]*$/)` |
| unknown variable reference | render after assembly | `unknown prompt variable "{{missing}}" in section "unknown-variable-demo"; registered variables: (none)` |

Probe process exit `0` 是因为三个 expected errors 均被捕获并打印，不代表操作本身成功。它证明 registration、assembly 与 render 是不同失败边界：一个仍含未插值文本的 assembly 不等于可发 request。

### 8.2 Owner suites

- system-prompt owner suite：`3 files / 68 passed / 68`，覆盖 order、equal-order、duplicate、bad/undefined/malformed variable、scope shadow、waterfall、complete conflict、lifecycle 与 invariant validation；
- focused AgentLoop suite：`1 file / 5 passed / 5 selected / 51 skipped`，覆盖 stable render、bad-variable recovery、changed snapshot、compaction 后 re-emission 与 cleared marker。

### 8.3 Retained harness mistakes

- `pnpm exec tsx -` 在 Windows exit `1`，报 `tsx is not recognized`；改用不写文件的 `node --import tsx/esm --input-type=module -` 后成功。这是 launcher observation，不是 DSH failure。
- 初次手写 `{ scope: agent.scope }` assembly context 缺 `agent`，导致 persona 中 `{{model}}` undefined、exit `1`。真实主链使用 `assembleContextFor(agent, signal)` 同时提供 `agent` 与 `scope`；最终实验改为在真实 loop waterfall 内捕获 assembly。

所有有效实验均未读取 credential value、未发真实 Provider 网络请求、未开放 listener；实验后 external fixture 仍为 exact pinned HEAD 且 clean。

## 9. Claim register

| Claim ID | Falsifiable claim | Research status | Evidence card | Wording ceiling |
|---|---|---|---|---|
| `32-C01` | Research 绑定 official pinned tag/commit，初始 fixture clean。 | `CONFIRMED` | `32-E01` | identity 不证明机制 |
| `32-C02` | PromptSection、PromptContext、AssembleContext、PromptAssembly 与 ContextSnapshotSection 有上述 exact fields；无独立 `PromptProvider` 类型。 | `CONFIRMED` | `32-E02` | 仅 pinned source |
| `32-C03` | Section/Context/Tool/Variable providers 按确定算法进入 assembly，再经 waterfall。 | `CONFIRMED` | `32-E03` | source algorithm，不是 Profile runtime dump |
| `32-C04` | same-layer duplicate fail；scoped same-name shadows global；scope-filtered waterfall 只作用匹配 scope。 | `CONFIRMED` | `32-E04` | selected scope/duplicate semantics boundary |
| `32-C05` | 单个 complete section 在 waterfall 后恢复为唯一 system section；多个 complete fail；contexts/tools/variables 不被终止。 | `CONFIRMED` | `32-E05` | 不等于 turn terminal |
| `32-C06` | 变量在 render 阶段严格插值，unknown/undefined/malformed fail，replacement value 不二次扫描。 | `CONFIRMED` | `32-E06` | selected direct negatives；不覆盖所有 malformed forms |
| `32-C07` | Stable Section 与 Dynamic PromptContext 分属 system string 与 sourced user-message snapshot 两条通道。 | `CONFIRMED` | `32-E07` | stability 是语义边界，不保证字节恒定 |
| `32-C08` | 身份/宿主/tool guidance、任务/指令、动态 policy/time 与历史由多种 owner 和通道提供。 | `CONFIRMED` | `32-E08` | representative map，不穷举所有插件 |
| `32-C09` | AgentLoop 每个 pre-step assemble；rendered system/tools 与 Session-derived messages 在 buildRequest 汇合。 | `CONFIRMED` | `32-E09` | source path != adapter receipt |
| `32-C10` | request/header 持久化 route/system/tools；Session surface 是 derived history 的 authoritative source。 | `CONFIRMED` | `32-E10` | 不证明外部 persistence/real provider |
| `32-C11` | two-Step MockAdapter trace 中 request messageCount `2 -> 5`，system/tools/route 不变，dynamic context 与 assistant/tool history 可归因变化。 | `CONFIRMED` | `32-E11` | MockAdapter mechanics；不是 real provider |
| `32-C12` | Effective Assembly 有 pre-render names，PromptContext snapshot 有窄 `source.sections` provenance；flat system 丢失 section provenance，pinned tree 无 general receipt。 | `CONFIRMED` | `32-E12` | absence 只限 pinned production-tree search |
| `32-C13` | replacement shadow retained PromptContext snapshot 后，下一 pre-step re-emits current snapshot/clear marker 的 source path 与 focused owner runtime 闭合。 | `CONFIRMED` | `32-E13` | 不泛化到任意 context/invariant |
| `32-C14` | BuildPilot 可采用 `IContextContributor` 统一贡献接口。 | `PROPOSAL` | `32-E14` | 未实现、未 ADR |
| `32-C15` | BuildPilot 应为每次 effective assembly 生成 source/transform/output Receipt，并对 stable/dynamic 分槽。 | `PROPOSAL` | `32-E15` | future design only |

统计：`15 Claims = 13 CONFIRMED / 0 PARTIAL / 2 PROPOSAL / 0 BLOCKED`。

## 10. BuildPilot candidate transfer

```ts
// PROPOSAL ONLY — not current DSH API and not implemented.
interface IContextContributor {
  readonly id: string
  readonly lane: 'stable' | 'dynamic' | 'history'
  contribute(input: ContributionInput): Promise<ContextContribution>
}

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

候选约束：

- contributor id 在同一 scope 唯一；shadow 必须留 receipt，不能只保留 winner；
- stable lane 进入可缓存 prefix，dynamic lane 进入 current snapshot，history lane 只接受 durable records；
- variable replacement、filter、redaction、compaction replacement 都记录 transform；
- terminal 只终止声明的 lane，不隐式终止 Tool/history/turn；
- receipt 不保存 secret 原文，只保存安全摘要/hash 与受控诊断字段。

这些是 Part VI 课程迁移候选，不是 DSH 当前机制的改名，也不是 BuildPilot 已完成设计。

## 11. Evidence Merge result

`EVIDENCE_MERGE PASS / OUTLINE ELIGIBLE`。

15 个 Claim 与 15 张 Evidence Card 一一对应；最终为 `13 CONFIRMED / 2 PROPOSAL / 0 BLOCKED`。Pinned schema、exact order/scope/conflict、两次 MockAdapter request、三个直接负例、`68/68` owner suite、`5/5` focused loop、窄 PromptContext compaction re-emission 与 provenance loss boundary 已闭合。Real provider/model/wire/token/cost 与通用 compaction invariant 仍明确未证；BuildPilot `IContextContributor + Receipt` 仍仅为 proposal。Next allowed gate：`OUTLINE`。
