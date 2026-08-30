# System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成

> **上一篇**：[Profile、Bundle、Provider 与 Capability Seam]({{< relref "ai-empowerment/agent-engineering-31-dsh-profile-bundle-capability-seam.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

同一个 Agent、同一个 model、同一组 Tool，连续两个 Step 发给模型的请求会一样吗？

这听起来像一个 prompt 文本比较问题。但在本篇固定版本的 DeepSeek Harness（下称 DSH）实验里，两次 request 的 system 完全一致，Tool list 也都是 `flip_mode`，message 数量却从 `2` 变成了 `5`。

```text
Step 1 request: system unchanged / tools=[flip_mode] / messages=2
Step 2 request: system unchanged / tools=[flip_mode] / messages=5
```

第二次 request 多了三项：

1. assistant 发出的 `flip_mode` tool call；
2. Tool 返回的 `mode flipped`；
3. 一份新的 runtime-context snapshot，内容从 `mode=read-only; tick=1` 变成了 `mode=write-enabled; tick=2`。

如果 Context 就是一段 system prompt，为什么 system 一个字节都没变，模型可见输入仍然变化了？如果只看最终 request，又怎样知道 `mode` 来自哪个 owner、以什么顺序加入、旧 snapshot 为什么仍然存在？

问题的本体不是“几段字符串怎么拼”，而是多种输入如何在一个 Step 边界汇合。身份、persona、Host guidance、Tool guidance、变量、动态 policy、当前任务和历史，各自拥有不同的 scope、排序、更新与持久化语义。

如果这篇只记一句话，我建议记这个：

> 一次模型请求不是一段 prompt，而是 stable system、dynamic snapshot 与 durable history 在 Step 边界的汇合；要解释它，必须把 Effective Assembly、Context Snapshot 与 terminal request receipt 分开记录。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 是 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。实验前后，外部 fixture 的 `HEAD` 与 tag target 都等于该 SHA，working tree、index 与 diff 为空。这个检查只固定研究对象，不替下面任何机制结论背书。

本文证据账为 `15 / 15 Claims`、`15 / 15 Evidence Cards`：`13 CONFIRMED / 0 PARTIAL / 2 PROPOSAL / 0 BLOCKED`。两次 request 确实经过 repo-owned `AgentLoop`，但终点是内存 `MockAdapter`；本轮没有读取 credential value，没有访问真实 LLM Provider，没有 SDK/HTTP wire、模型输出、网络、token 或 cost 证据。

## 1. 为什么一段 final prompt 解释不了一次请求

把最终 system 字符串当成唯一事实，会丢掉至少四种边界。

### 1.1 来源边界

同一句 model-facing guidance 可能由不同 owner 提供：

- Harness identity；
- deployment persona 或 agent-scoped persona；
- Web / Headless Host surface；
- Tool guidance；
- sandbox 与 approval 的当前状态；
- task、workspace instructions、time/tmux 插件或历史消息。

这些内容最后都可能被模型看到，但不代表它们属于同一 API，也不代表它们拥有相同权限。Tool guidance 是提示，不是 Tool authority；Prompt 里写“当前只读”也不能替代 Provider 对写操作的拒绝。

### 1.2 时间边界

有些内容每个 Step 都重组，却通常保持稳定；有些内容每个 Step 重新采样，只在变化时追加新 snapshot；还有一些内容作为 durable event 留在 Session surface，直到 append、replacement 或 compaction 改变当前 history。

“Stable”在这里表示它位于 system/header lane，适合做字节或 hash 对比，不表示永远不变。固定源码中的 `PromptSection.text` 可以是 callback；如果 callback 或 waterfall 的结果变化，rendered system 也会变化。

### 1.3 Scope 边界

同名 contribution 可能位于 global、ancestor scope 或当前 Agent scope。same-layer duplicate 会立即失败；global 与 scoped layer 的同名条目则允许 nearest scope shadow global。

这两种情况不能都概括成“后写覆盖前写”：一个是注册完整性错误，另一个是明确的作用域覆盖。

### 1.4 证据边界

一次 request 至少经过四层对象：

| 证据层 | 最小对象 | 这一层能证明什么 | 单靠这一层仍然不知道什么 |
|---|---|---|---|
| Contribution | name、order、provider、scope owner | 注册契约与静态 owner | 目标 scope 是否真的使用 |
| Effective Assembly | `sections[]`、`contexts[]`、`tools[]`、`variables{}` | 本 Step 的排序、shadow、provider 求值与 waterfall 后结果 | render 是否成功、adapter 是否收到 |
| Rendered boundary | flat `system`、named context sections | 插值与 model-facing text | 完整来源与 transformation ledger |
| Request receipt | provider、model、system、tools、messages、sessionId | terminal adapter 实际收到什么 | 真实 SDK/HTTP、模型行为、token/cost |

所以，注册表不是 Effective Assembly，Effective Assembly 不是 request receipt，terminal adapter 收到的 request 也不是完整 provenance receipt。

## 2. 先建立一个不依赖 DSH 类名的三通道模型

在进入源码之前，先把一次请求拆成三条输入通道。

```text
Stable system lane
  identity + persona + host/tool guidance + variables
    -> ordered effective sections
    -> rendered system + ordered tools

Dynamic snapshot lane
  current policy / approval / delegated state
    -> named current snapshot
    -> sourced user message when changed or retention removed

Durable history lane
  claimed task + instructions + prior assistant/tool/context events
    -> current Session surface
    -> request messages

Step boundary
  route + system + tools + messages
    -> frozen request
    -> terminal adapter
```

这套模型包含三个不能混用的 artifact。

### 2.1 Contribution

Contribution 是某个 owner 提供的一项候选输入。它至少需要 name 或 id、order、scope、provider 与 lane。它回答“谁想贡献什么”，还没有回答最终是否入选。

### 2.2 Effective Assembly

Effective Assembly 是本 Step 已完成 scope merge、排序、provider 求值和 transformation 的结果。它回答“当前决策最终选择了什么”，但还可能在严格变量插值时失败，也不证明 request 已经到达 adapter。

### 2.3 Context Snapshot 与 Request Receipt

Context Snapshot 是 dynamic lane 的完整当前视图。它作为有 source metadata 的 user message 进入 durable history，既保存组合后的文本，也可以保留各 named section。

Request Receipt 则是 terminal adapter 实际收到的 normalized request。它回答“这次操作真正送到了哪个 adapter 边界”，但 rendered system 已经是扁平字符串，不能反推每个 system section 的来源。

三通道必须分开，是因为它们的更新语义不同：stable lane 每 Step 重组；dynamic lane 需要 newest complete snapshot 和 supersedes 语义；history lane 要服从 Session surface 与 replacement。一个 lane 的 terminal flag，也不能隐式终止其他 lane 或整个 Agent turn。

## 3. DSH 的真实数据合同：没有一个万能 PromptProvider

固定源码的核心 contract 位于 [`packages/core/system-prompt/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/system-prompt/src/index.ts)。真实字段是：

| Type | Exact fields | 当前职责 |
|---|---|---|
| `PromptSection` | `name`, `order`, `text`, `complete?` | system lane 的 named contribution |
| `PromptContext` | `name`, `order`, `text` | dynamic model-context contribution |
| `AssembleContext` | `scope?`, `signal?` | 一次 assembly 的 scope 与取消信号 |
| `PromptAssembly` | `sections`, `contexts`, `tools`, `variables` | waterfall 前后使用的四 lane effective object |
| `ContextSnapshotSection` | `name`, `text` | dynamic snapshot 的 named attribution |

`PromptSection.text` 与 `PromptContext.text` 都可以是 string，也可以是接收 `AssembleContext` 的 callback。Agent package 还会把可选 `agent` 合并进 assembly context，让 AgentLoop 注册的 `provider`、`model`、`cwd` variables 可以从 active agent/session 取值。

这里有一个术语陷阱：pinned source 没有名为 `PromptProvider` 的独立 interface。Section/Context 的 `text` callback、VariableProvider 与 ToolProvider 都承担 contribution provider 的角色；model Provider 的 `LlmAdapter` 又是另一条 seam。为了行文方便把它们叫“provider”，不能反过来发明一个源码中不存在的统一类型。

同样，`PromptContext` 也不是“所有模型上下文”的总容器。任务、time/tmux、workspace instructions 和历史有自己的通道，最终只是在 request 边界汇合。

## 4. 多来源 Context 并不走同一条 API

固定源码里的代表性 owner 可以画成下面这张表：

| Concern | Representative owner | Current channel | 更新预期 |
|---|---|---|---|
| Harness identity | `SystemPrompt` constructor | `PromptSection` | composition 不变时稳定 |
| deployment / scoped persona | core config + persona preset | same-name Section shadow | per effective Agent scope |
| Harness checkout / Web surface | boot / web bundle | `PromptSection` | Host/composition dependent |
| provider/model/cwd | AgentLoop | variables | per assembly 求值 |
| Tool guidance | individual Tool plugins | `PromptSection` | visible Tool set dependent |
| Tool schemas | Tools service | `tools[]` | scope-filtered provider result |
| sandbox / approval | policy services | `PromptContext` | dynamic complete snapshot |
| time / tmux | pre-step listeners | sourced user message | plugin-scheduled dynamic input |
| workspace instructions | instruction reconciler | durable sourced user message | baseline + changed scopes |
| user task | inbox claim | durable user message | per turn input |
| prior transcript | Session surface | `deriveMessages()` | append / replacement history |

最重要的反例是 time-context。它当然属于广义的动态上下文，但固定实现不是 `SystemPrompt.context()` registration，而是一个 `agent/pre-step` listener：先让前面的 waterfall 继续，再追加带 source 的 `UserMessage`。

两种动态输入最后都可能成为 user-role message，但只有 `PromptContext` 使用 `RuntimeContextProjection` 的 complete-snapshot、change suppression 与 retained-state 机制。把所有 Context 强行解释成一个 API，会抹掉不同 owner 的持久化与 freshness 语义。

## 5. Effective Assembly：order、scope 与 conflict 怎样真正求解

`SystemPrompt.assemble()` 不是简单地遍历数组。它依赖 [`packages/core/scope/src/store.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/scope/src/store.ts) 的 scoped layers，并对四类 contribution 使用不同规则。

完整顺序是：

1. 求值 global variables；
2. 按 farthest ancestor 到 nearest exact scope 覆盖同名 variable；
3. 按 name merge Sections 与 Contexts，nearest scoped entry shadow global；
4. additive 收集 global 与 matching scoped Tool providers；
5. Sections 按 `order ASC`，同 order 再按 code-unit `name ASC`；
6. Contexts 只按 `order ASC`，同 order 依赖 stable effective-map insertion order；
7. 求值 Section/Context text providers；
8. Tools 按 explicit `toolOrder` 或 canonical name 排序；
9. 运行 scope-filtered `system-prompt/assemble` authoritative waterfall；
10. 恢复 active complete section 与 runtime-context suppression invariant；
11. 返回 Effective Assembly。

### 5.1 Section 与 Context 的 equal-order rule 不一样

Sections 有明确的 name tie-breaker。实验中 `zeta-section` 先注册、`alpha-section` 后注册，两者 order 都是 `100`，最终仍然是 `alpha-section` 在前。

Contexts 只比较 numeric order。实验中 `zeta-context` 先注册、`alpha-context` 后注册，两者 order 都是 `50`，最终保持 `zeta-context`、`alpha-context` 的 effective-map insertion order。

把 Section comparator 复制到 Context，会得到一个看似合理、实际错误的 precedence 解释。

### 5.2 Same-layer duplicate 与 cross-scope override

每个 exact layer 内，Section、Context、Variable 都使用 named registry。第二次注册同名条目会立即 throw，不会静默叠加。

但 global 与 scoped layer 可以出现同名条目。例如 global `deployment:persona` 可以被 agent scope 的同名 persona shadow。scope chain 从 farthest ancestor 走到 nearest exact scope，因此最近 scope 是 effective winner。

这不是 unrestricted last-write-wins。只有被当前 viewing scope 纳入 chain 的同名条目才参与 shadow；same-layer duplicate 仍然失败。

### 5.3 Tool provider 是 additive lane

Tool providers 不是 name-shadowing registry。所有 global 与 matching scoped provider 都可以贡献 schemas，再单独排序。当前 `SystemPrompt` path 没有证明 duplicate tool name 会被 registry 自动去重；section/context invariant 也不能替 Tool lane 补上这个结论。

### 5.4 Waterfall 是 authoritative transform

`system-prompt/assemble` waterfall 可以 mutate、replace，甚至 short-circuit assembly。注册表只表示候选来源；waterfall 返回值才是 transform 后的 effective object。

因此，只打印 registry 无法替代 Effective Assembly receipt。后处理器添加或覆盖的变量、Section、Context 和 Tool，需要在 render 前重新捕获。

## 6. `complete` 只终止 system-section lane

`PromptSection.complete` 很容易被写成“完整 prompt，因此后面所有东西都不再加入”。固定源码不是这个语义。

assembly 会先找 effective complete sections：

- 数量大于一个：直接失败；
- 只有一个：保存它已经求值的 exact contribution；
- `system-prompt/assemble` waterfall 仍然运行；
- waterfall 结束后，把该 contribution 恢复为唯一 system section；
- transformed contexts、tools 与 variables 仍然保留，除非各自有独立 suppression/invariant。

所以 `complete` 的 terminal semantics 是：终止 system-section lane 的竞争。它不终止 PromptContext、Tool、Variable 装配，也不终止 request、Step、turn 或 Agent run。

把一个局部 terminal flag 写成整个执行链的 terminal，是 Context 系统最危险的语义升级之一。

## 7. Variable 在 render 才闭合，assembly success 仍可能是假阳性

变量注册名必须匹配：

```text
/^[a-z][a-z0-9_]*$/
```

AgentLoop 注册的 `provider`、`model`、`cwd` 每次 assembly 都从 active agent/session 求值。Section 与 Context 的文本先进入 Effective Assembly，严格 interpolation 则在 `renderPrompt()` 与 `renderContextSections()` 发生。

这使注册、assembly 与 render 成为三个不同失败边界：

```text
REGISTERED -> ASSEMBLED -> RENDERED -> REQUEST RECEIVED
```

unknown variable、registered-but-undefined value、malformed complete group 都会让 render 失败。替换后的 value 不会再次被扫描，所以 value 内出现的 `{{sneaky}}` 保持 literal，不会触发第二轮模板展开。

本轮跑了三个直接负例：

### 7.1 Same-layer duplicate section

第二次注册 `duplicate-demo` 立即返回：

```text
prompt section "duplicate-demo" is already registered
```

失败发生在 registration，不会进入 assembly 或 request。

### 7.2 Invalid variable name

注册 `Bad-Name` 立即返回：

```text
invalid prompt variable name "Bad-Name" (must match /^[a-z][a-z0-9_]*$/)
```

这同样是 registration failure。

### 7.3 Unknown variable reference

Section 文本包含 `{{missing}}` 时，assembly 仍可以保存这段未插值文本；`renderPrompt()` 才返回：

```text
unknown prompt variable "{{missing}}" in section "unknown-variable-demo"; registered variables: (none)
```

探针进程整体 `exit 0`，只是因为三个 expected errors 都被 catch 并打印。不能把 process exit 改写成三项操作成功。

这组反例说明，日志里只有“assembly complete”仍然不够。一个可审查 gate 至少要记录失败发生于 registration、assembly、render 还是 adapter receipt。

## 8. 一次真实 Step 的 exact source path

现在把抽象模型落回 DSH 的真实 symbol。

### 8.1 Registration 到 Effective Assembly

```text
SystemPrompt.section/context/variable/tools
  -> PromptLayer + ScopedLayers registries
  -> ReactLoopAgent.preStep()
  -> SystemPrompt.assemble(assembleContextFor(agent, signal))
  -> system-prompt/assemble waterfall
  -> PromptAssembly
```

关键入口在 [`packages/core/agent-loop/src/agent.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/agent.ts)。每个 admitted Step 都会先 claim inbox messages，再使用 `assembleContextFor(agent, signal)` 发起 assembly。

这里保留一个失败实验很有价值：早期 probe 手写 `{ scope: agent.scope }`，随后 persona 的 `{{model}}` 变成 undefined。原因不是 DSH 随机失效，而是 scope selection 只选择 scoped registry，没有自动提供 AgentLoop variable providers 所需的 `agent`。

最终实验不再让手写 assembly context 冒充真实主链，而是在 real loop 的 waterfall 中捕获 authoritative Effective Assembly。

### 8.2 Dynamic Context 到 Session surface

```text
PromptAssembly.contexts
  -> renderContextSections()
  -> joinContextSections()
  -> RuntimeContextProjection.project()
  -> optional source-attributed UserMessage

claimed messages + optional snapshot
  -> agent/pre-step waterfall
  -> append user/message to Session surface
```

`renderContextSections()` 插值并过滤空 Context，但保留 `{name,text}`。`joinContextSections()` 生成 complete snapshot 文本，并加上“当前 snapshot supersedes earlier runtime-context snapshots”的语义前言。

`RuntimeContextProjection.project()` 比较当前文本与 retained snapshot：相同时不追加；变化、retention 被 replacement 移除，或 active set 被清空时，才生成新的 sourced message。

### 8.3 Stable system、history 与 Tool 在 request 边界汇合

```text
PromptAssembly.sections
  -> renderPrompt()
  -> flat system

Session surface
  -> Session.deriveMessages()
  -> ordered current messages

PromptAssembly.tools
  -> ordered schemas

route + system + tools + messages
  -> ReactLoopAgent.buildRequest()
  -> LlmRuntime.prepareCall()
  -> canonicalHeader(route/system/tools)
  -> deep-frozen GenerateOptions
  -> terminal adapter
```

Session 的 `deriveMessages()` 才是当前 history surface 的 authority。raw non-surface event 不会自动进入 request；compaction replacement 会 shadow 旧 nodes，并使 derived-message cache 在 generation 变化后重建。

源码还能继续闭合到 [`LlmRuntime`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/llm/llm/src/index.ts)、DeepSeek adapter、[`serializeRequest()`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/llm/llm-deepseek/src/serialize.ts) 与 `/chat/completions` fetch：flat system 会成为一条 wire `system` message，后面接 serialized history。

但这只是 source path。本轮 Lab 的 terminal 是 `MockAdapter`，没有走真实 DeepSeek HTTP。因此，后面的 runtime 结论必须停在 “AgentLoop request mechanics reached terminal MockAdapter”。

## 9. 两个真实 AgentLoop Step：Stable 不变，Dynamic 与 History 可归因变化

实验挂载了 repo-owned production service chain：

```text
LlmRuntime -> SessionStore -> SystemPrompt -> ToolRuntime
           -> AgentRegistry -> AgentLoop -> MockAdapter
```

注册的代表性 contribution 包括：

| Kind | Registration | Order / value | 用途 |
|---|---|---:|---|
| built-in section | `harness:identity` | `-1000` | stable identity |
| configured section | `deployment:persona` | `0` | `persona_name` / `model` interpolation |
| section | `zeta-section` | `100` | equal-order observation |
| section | `alpha-section` | `100` | equal-order observation |
| context | `zeta-context` | `50` | dynamic `mode` provider |
| context | `alpha-context` | `50` | per-assembly incrementing `tick` |
| variable | `persona_name` | `trace-agent` | template provenance |
| loop variables | `provider`, `model` | `mock`, `mock` | request route facts |
| tool | `flip_mode` | one schema | 触发第二 Step 并改变 mode |

Mock 的第一条 response 请求 `flip_mode`，Tool 把 `mode` 从 `read-only` 改为 `write-enabled`；第二条 response 正常停止。两个真实 Step 都到达 terminal in-memory `MockAdapter`。

### 9.1 两份 Effective Assembly

| Field | Step 1 | Step 2 | Interpretation |
|---|---|---|---|
| sections | identity, persona, alpha, zeta | identical | equal-order Section 按 name |
| contexts | zeta=`mode=read-only`, alpha=`tick=1` | zeta=`mode=write-enabled`, alpha=`tick=2` | equal-order Context 保持插入序并逐 Step 求值 |
| variables | provider=`mock`, model=`mock`, persona_name=`trace-agent` | identical | selected values 未变 |
| tools | `flip_mode` | identical | visible Tool set 未变 |

Normalized Effective Assembly SHA-256：

- Step 1：`0420ABBCE3215D69C33564A5575944777FE1C57C27D4A1B0978B417271584551`
- Step 2：`17AABD2AA449A9EDC23A349A15CC34DC880591CDB954DEBC3859F99229011473`

两个 hash 不同来自 dynamic contexts。它不表示 system 或 tools 变化。

两步 rendered system byte-identical：

```text
You are an AI agent powered by DeepSeek Harness.

Persona: trace-agent; model=mock.

Alpha stable for trace-agent.

Zeta stable.
```

### 9.2 Request 1：两条 messages

Request 1 normalized SHA-256：

```text
72326D5189BF92BC67C41745A3F61358291B670E0CDB07D0972927C9120B78CA
```

它包含：

1. user task：`exercise two steps`；
2. system-prompt-owned runtime snapshot：

```text
Current runtime context. This snapshot supersedes earlier runtime-context snapshots.

mode=read-only

tick=1
```

snapshot source 中保留：

```json
{
  "kind": "plugin",
  "plugin": "@deepseek-ai/dsh-system-prompt",
  "form": "snapshot",
  "sections": [
    { "name": "zeta-context", "text": "mode=read-only" },
    { "name": "alpha-context", "text": "tick=1" }
  ]
}
```

### 9.3 Request 2：五条 messages

Request 2 normalized SHA-256：

```text
5705EE4D9EF5B6A6F3654D92D3EFC8D058D2C301AC3B2721F5976C4FB735AE89
```

provider/model 仍是 `mock/mock`，system byte-identical，Tool list 仍是 `['flip_mode']`。变化只在 Session-derived messages：

```diff
messageCount: 2 -> 5
+ assistant/model(mock): "switching" + flip_mode tool-call
+ user/tool: "mode flipped", isError=false
+ user/plugin(@deepseek-ai/dsh-system-prompt, form=snapshot):
    zeta-context = "mode=write-enabled"
    alpha-context = "tick=2"
```

第二个 request 仍然包含第一份 snapshot。普通 Step change 不 rewrite 旧 event，而是 append 新的 complete snapshot，并在 model-facing text 中声明它 supersedes earlier snapshots。

这让两件事同时成立：

- 最新 snapshot 才是当前 effective dynamic state；
- 旧 snapshot 仍是 durable history 中可归因的历史输入。

### 9.4 这组 trace 证明了什么

它证明：

- real AgentLoop 在每个 Step 重新 assembly；
- stable system/tools/route 与 dynamic/history 可以独立变化；
- assistant tool call、Tool result 与新 snapshot 能逐项解释 `messages 2 -> 5`；
- `source.form='snapshot'` 与 ordered `source.sections` 为 PromptContext 保留 exact attribution。

它不证明：

- 真实 DeepSeek Provider、SDK/HTTP request 或外部模型行为；
- token usage、cost、latency 或输出质量；
- 所有部署的 PromptSection 都 byte-stable；
- normalized SHA 覆盖 AbortSignal、timestamps、generated IDs 或 wire serialization。

这里最重要的证据纪律是：真实的是 AgentLoop 与 Step progression；mock 的是 terminal model adapter。不能因为 request shape 真实，就把 Provider 边界悄悄升级。

## 10. Provenance：Context Snapshot 保留了什么，flat system 又丢了什么

当前实现并非毫无 provenance。

在 render 前：

- `PromptAssembly.sections[]` 保留 system contributor name 与 resolved text；
- `PromptAssembly.contexts[]` 保留 dynamic contributor name 与 resolved text；
- Tool schemas 保留 Tool name；
- variables 保留 effective key/value map。

进入 durable history 后：

- PromptContext snapshot 的 `source.sections` 保留 ordered `{name,text}`；
- task、instruction、time/tmux 等 message 可以保留各自 source；
- request/header 保存 route、flat system 与 ordered tools。

但 `renderPrompt()` 返回的是一个 flat string。到 final `GenerateOptions.system` 和 `request/header` 时，system-section names 与 spans 已经消失。最终 request 无法独立回答：“这 80 个字符来自 identity，后面 120 个字符来自 scoped persona，哪一段又被 waterfall 改写？”

另外几类 provenance 也没有统一留下：

- variables 只有 effective value，没有 provider owner、旧值与 override chain；
- anonymous Tool provider identity 不进入 request；
- waterfall mutate/replace/short-circuit 没有 general transformation ledger；
- scope shadow 的 loser 没有作为 decision item 进入 final header。

因此，final request 是一次 operation input receipt，不是完整 Effective Assembly provenance receipt。PromptContext 的 `source.sections` 是一个很有价值、但范围明确的窄 receipt。

Source Investigator 对 pinned production tree 做了限定搜索，没有找到 general `IContextContributor` 或统一 `Receipt` type/object。这个结论只能表述为“在选定 frozen tree 与搜索范围中未找到”，不能推断官方动机、未来版本或永久 absence。

## 11. Compaction 后会不会重注入 Context？答案只在窄范围成立

如果 compaction replacement 删除了旧 runtime snapshot，下一次 request 会不会失去当前 policy state？

固定源码中的 [`RuntimeContextProjection`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/runtime-context.ts) 提供了一条专门路径：

```text
Session replacement event
  sourceEventSeqs includes retained runtime snapshot seq
    -> retained = null

next preStep
  -> reassemble current PromptContexts
  -> render complete current snapshot
  -> project() sees no retained equal snapshot
  -> append new sourced snapshot
```

如果 replacement 发生后 active context set 已变空，则 projection 发出 explicit cleared marker，而不是沉默地保留旧状态。

本轮 focused AgentLoop owner run 结果是：

```text
1 file passed
5 / 5 selected tests passed
51 skipped by filter
```

五项分别覆盖：

1. stable system + variable 到达 adapter request；
2. bad variable 阻断 request，后续修复后的 turn 可继续；
3. changed PromptContext 追加新 snapshot；
4. replacement 移除 retained snapshot 后，unchanged current snapshot 被重新发出；
5. active contexts 变空后发出 clear marker。

但这里必须停住。

Stable system 不是“从 compacted history 被重新注入”。它本来就不依赖 history 保存，而是每个 Step 重新 assemble/render 到 request header。time、tmux、agent-instructions 又有各自 scheduling 与 durable semantics。任意被 summary 替换的 task/plugin message，也不会自动被 `RuntimeContextProjection` 再生。

所以准确结论是：当前版本对 system-prompt-owned PromptContext snapshot 有 compaction-aware re-projection。它不是“任意 compaction 后重放所有 invariant”的通用机制。

## 12. Owner tests、直接负例与失败尝试怎样分账

本轮除两步 request trace 外，还运行了两个 owner test group。

### 12.1 System Prompt owner suite

```text
3 files passed
68 / 68 tests passed
```

它覆盖 order、equal-order tie、duplicate Section/Context/Variable、strict variable 的 unknown/undefined/malformed/nested cases、scope shadow、authoritative waterfall、complete conflict、lifecycle disposal 与 invariant validation。

### 12.2 Focused AgentLoop owner suite

```text
1 file passed
5 / 5 selected tests passed
51 skipped by filter
```

它闭合的是 stable render、bad-variable recovery、changed snapshot 与 narrow compaction re-emission，不是完整 AgentLoop 回归。被 filter 跳过的 `51` 项不能计入“通过”。

### 12.3 两个 retained harness mistakes

第一个失败发生在 Windows launcher：

```text
pnpm exec tsx -
-> exit 1
-> 'tsx' is not recognized as an internal or external command
```

最终使用不写文件的：

```text
node --import tsx/esm --input-type=module -
```

这是一条 launcher observation，不是 DSH prompt failure。

第二个失败是手写 `{scope: agent.scope}` assembly context，导致 persona 的 `{{model}}` undefined。它真实暴露了实验 harness 与 production path 的偏差；最终通过 real AgentLoop waterfall 捕获 assembly，而不是修改文章措辞掩盖失败。

失败命令保留下来，是为了把 orchestration mistake、registration/render failure 与 product behavior 分开。只有这样，读者才能判断后续命令为什么变化，哪些结果仍可复现。

## 13. 这条链暴露出的六类工程风险

### 13.1 Order collision risk

Section 与 Context 不共享 equal-order rule。如果插件作者默认“同 order 都按 name 排”，Context precedence 可能与预期不同。更稳的做法是使用 sparse explicit order，并让 receipt 保存最终位置与 tie-break basis。

### 13.2 Snapshot accumulation risk

普通 dynamic change 会 append full superseding snapshot。消费者必须读取 current Session surface 与 newest snapshot 语义，不能扫描到第一条 owner message 就当作当前状态。

### 13.3 Late-render risk

unknown 或 undefined variable 在 render 阶段才失败。只记录 registration 或 assembly success，会把从未到达 adapter 的 request 误判为 ready。

### 13.4 Waterfall opacity risk

registry 不是 Effective Assembly。waterfall 可以替换 variables、sections、contexts 或 tools；如果不记录 transform，final value 无法可靠反推来源。

### 13.5 Provenance and secret risk

记录 provenance 不等于记录所有 plaintext。credential、secret variable 和敏感 Context 需要 redaction policy。安全 receipt 更适合保留 owner、scope、decision、transform 与 output hash，而不是复制秘密原文。

### 13.6 Compaction over-generalization risk

PromptContext 的 retained-snapshot projection 是 owner-specific recovery。要让其他 input 可再生，必须为每个 owner 定义 freshness、replace semantics、source marker 与 negative tests，不能借用一条局部机制声称全系统完成。

## 14. BuildPilot：`IContextContributor + Receipt` 只能是候选设计

DSH 当前已经展示出多 owner、多 channel 的事实，也展示了局部 provenance 与 flatten loss。但 pinned tree 没有 general `IContextContributor` 或统一 `Receipt` abstraction。

因此，下面不是 DSH API，也不是 BuildPilot 已完成实现，只是 Part VI 的迁移候选：

```ts
// PROPOSAL ONLY — not current DSH API and not implemented.
interface IContextContributor {
  readonly id: string
  readonly lane: 'stable' | 'dynamic' | 'history'
  contribute(input: ContributionInput): Promise<ContextContribution>
}
```

为什么 interface 必须显式带 `lane`？因为统一 observability 不应该抹平 durability：stable contribution 进入可对比的 system prefix；dynamic contribution 进入 current snapshot；history contribution 只能来自 durable records。把三者都压成 `string contribute()`，只是把原来的歧义搬进新接口。

对应 receipt 候选是：

```ts
// PROPOSAL ONLY — not current DSH API and not implemented.
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

第一版候选约束应包括：

- contributor id 在同一 scope 唯一；
- shadowed loser 也要留 decision，不能只保存 winner；
- variable replacement、filter、redaction、compaction replacement 都记录 transform；
- terminal 只终止声明的 lane；
- receipt 不保存 secret plaintext；
- future acceptance tests 覆盖 duplicate、override、bad variable、complete 与 compaction。

这两个对象都只有 `PROPOSAL` 身份。当前没有 BuildPilot ADR、代码、runtime、migration 或 performance/security review；Article 38 与 Part VII 也没有开始。

## 15. 动手验证：不调用真实模型，也能审计一次 Context Assembly

这套实验方法可以迁移到其他 Agent Runtime，不要求使用 DSH，也不要求消费真实 Provider 配额。

### 15.1 冻结研究对象

1. 记录 official repository、tag、full SHA、OS、Node/package-manager/PowerShell 版本。
2. 在实验前保存 `HEAD`、tag target、working tree、index 与 diff。
3. 使用外部 isolated fixture，不修改目标 source/config/lockfile。
4. 不读取 credential value，不开放 listener，不发真实网络请求。

本轮环境是 Windows 10 x64、Node `24.18.1`、pnpm `11.7.0`、PowerShell `7.6.4`。

### 15.2 同时捕获三份 artifact

对每个 Step 分别保存：

1. Effective Assembly：ordered section/context names、tools、safe variables 与 normalized hash；
2. Context Snapshot：combined text 与 ordered `source.sections`；
3. terminal request：route、system hash、Tool names、message count 与 source-aware diff。

如果只保存第三份，system provenance 已经 flatten；如果只保存第一份，又不知道 render/request 是否完成。

### 15.3 用 deterministic Tool 制造一个动态变化

让第一条 mock response 调用一个只修改实验状态的 Tool，保证出现第二 Step。第二条 mock response 正常停止。这样变化来自可控 Tool result，而不是 nondeterministic model text。

比较时至少分列：

- provider/model；
- rendered system bytes/hash；
- ordered tools；
- message count；
- each message role/source/text summary；
- dynamic snapshot sections；
- Effective Assembly hash。

### 15.4 分阶段跑三个负例

分别观察：

- same-layer duplicate 在 registration 失败；
- invalid variable name 在 registration 失败；
- unknown variable reference 在 render 失败。

不要只保存 exception message，要保存 expected stage、observed stage、exit code 与“是否到达 adapter”。

### 15.5 窄范围验证 compaction re-emission

用 owner test 或 deterministic surface replacement 证明：

- replacement shadow retained snapshot 后，current active snapshot 被再次发出；
- active set 变空后发 clear marker；
- 其他 plugin/task messages 不自动进入这条结论。

### 15.6 用一张表限制结论

| Observation | 可以说 | 不能说 |
|---|---|---|
| registration exists | contribution contract present | 当前 scope 已使用 |
| Effective Assembly captured | selected scope/order/providers resolved | render/request succeeded |
| MockAdapter received request | AgentLoop mechanics reached terminal mock | real Provider/network/model worked |
| `source.sections` present | PromptContext parts attributable | flat system provenance complete |
| replacement owner test passed | PromptContext snapshot re-projects | all invariants auto-reinject |

最后重新检查 fixture identity 与 clean state。本轮最终仍是 pinned commit，`git status --short` 为空。

## 16. Evidence Boundary：本篇建立了什么，还没有建立什么

### 16.1 已建立

- source 与 Lab evidence 绑定 official frozen revision，fixture 检查时 clean；
- `PromptSection`、`PromptContext`、`AssembleContext`、`PromptAssembly`、`ContextSnapshotSection` 的 exact fields 已闭合；
- pinned source 没有 standalone `PromptProvider` type；
- Section/Context/Variable/Tool 使用确定但不完全相同的 merge/order 规则；
- same-layer duplicate rejection 与 cross-scope shadow 分账；
- `complete` 是 system-section lane terminal，不是 turn terminal；
- strict variables 的 registration/render 边界与三个直接负例已观察；
- identity/Host/Tool/policy/time/instructions/task/history 的 representative owner/channel map 已闭合；
- 每 Step `assemble -> render -> Session surface -> frozen request` 的 source path 已闭合；
- 两个真实 AgentLoop Step 到达 terminal MockAdapter；
- selected trace 中 system/tools/route 稳定，messages 从 `2 -> 5`，变化可归因；
- Effective Assembly names、PromptContext `source.sections` 的窄 provenance 与 flat-system loss 已闭合；
- PromptContext replacement 后的窄 re-projection source/runtime 已闭合；
- system-prompt owner suite `68 / 68`，focused AgentLoop `5 / 5 selected`；
- BuildPilot `IContextContributor + Receipt` 形成 proposal。

### 16.2 未建立

- 真实 DeepSeek Provider runtime、SDK/HTTP wire receipt 或 model behavior；
- credential availability/value、network、latency、token、cost；
- 所有 Profile、plugin contributor 或 deployment；
- 每个 PromptSection 在所有 Step 都 byte-stable；
- flatten 后仍存在 general system-section provenance；
- waterfall 与 variable override 的通用 persisted transformation ledger；
- task、time、tmux、instructions 或任意 plugin message 的 generic compaction re-injection；
- 官方为什么没有 general receipt abstraction，或未来版本是否会增加；
- BuildPilot ADR、实现、runtime、Article 33 结论、Article 38 或 Part VII start。

## 17. Claim 与 Evidence Card 对照

下面把 `15 / 15` Claims 映射回正文。`PROPOSAL` 是课程工程判断，不冒充 DSH fact。

| Claim | Status | 本文落点 | Evidence Card | 表述上限 |
|---|---|---|---|---|
| `32-C01` frozen revision / clean fixture | `CONFIRMED` | 开篇、§15—16 | `32-E01` | identity 不证明机制 |
| `32-C02` exact contracts / no standalone PromptProvider | `CONFIRMED` | §3 | `32-E02` | pinned source only |
| `32-C03` deterministic assembly algorithm | `CONFIRMED` | §§5、9 | `32-E03` | selected algorithm/runtime observation |
| `32-C04` duplicate / scope shadow / waterfall scope | `CONFIRMED` | §§5、7、12 | `32-E04` | 不写 unrestricted last-wins |
| `32-C05` complete section semantics | `CONFIRMED` | §6 | `32-E05` | not turn terminal |
| `32-C06` strict variables / bad-variable boundaries | `CONFIRMED` | §§7、12—13 | `32-E06` | selected negatives + owner coverage |
| `32-C07` stable system vs dynamic snapshot | `CONFIRMED` | §§1—2、8—9 | `32-E07` | stable 是 lane 预期，不是 immutable type |
| `32-C08` representative multi-owner map | `CONFIRMED` | §§1、4 | `32-E08` | representative，不是 exhaustive |
| `32-C09` per-Step convergence to request | `CONFIRMED` | §§8—9 | `32-E09` | source + MockAdapter only |
| `32-C10` header and Session history ownership | `CONFIRMED` | §§1、8 | `32-E10` | 不证明外部 persistence/wire |
| `32-C11` messages `2 -> 5` diff | `CONFIRMED` | 开篇、§9 | `32-E11` | mock mechanics，不是 real model |
| `32-C12` narrow provenance / flattened loss | `CONFIRMED` | §§1—2、10、13 | `32-E12` | bounded absence search only |
| `32-C13` PromptContext replacement re-projection | `CONFIRMED` | §§2、11—13 | `32-E13` | not generic reinjection |
| `32-C14` BuildPilot `IContextContributor` | `PROPOSAL` | §14 | `32-E14` | no current DSH API / implementation |
| `32-C15` BuildPilot `AssemblyReceipt` | `PROPOSAL` | §14 | `32-E15` | future design only |

最终状态：

```text
Claims: 15 / 15
Evidence Cards: 15 / 15
Claim status: 13 CONFIRMED / 0 PARTIAL / 2 PROPOSAL / 0 BLOCKED
AgentLoop Steps: 2
Terminal adapter: MockAdapter
Request messages: 2 -> 5
Stable in selected trace: provider/model/system/tools
Changed: assistant/tool history + PromptContext snapshot
Direct negatives: 3
System Prompt owner tests: 68 / 68
Focused AgentLoop tests: 5 / 5 selected / 51 skipped
Real Provider / model / network / token / cost: NOT TESTED
BuildPilot IContextContributor + Receipt: PROPOSAL ONLY
Part VII: NOT STARTED
```

## 18. 后续文章只接 owner，不在这里抢答

Article 29 已建立 Host/Profile 到 Agent Run 的总图；Article 30 深入 plugin lifecycle；Article 31 拆开 Profile composition 与 Capability activation。本篇只承担多来源 Context 到 model request 的 assembly、render、snapshot 与 receipt 边界。

下一篇将进入 Loop、Turn 与 Step，研究 AgentLoop 怎样推进一次运行。它尚未发布，因此当前不创建 future link，也不提前给出 Article 33 的完整 loop trace 结论。

Article 34—37 分别拥有 Session continuation、完整 Tool pipeline、Recovery/observability 与 extension mapping。Article 38—44、BuildPilot implementation 和 Part VII 都保持 `NOT STARTED`。

## 19. 学习检查

1. 为什么 system byte-identical 仍然可能得到不同的 model request？
2. Stable system、Dynamic PromptContext 与 durable history 分别进入 request 的哪个字段？
3. `PromptSection`、`PromptContext`、`PromptAssembly` 与 `ContextSnapshotSection` 的 exact fields 是什么？
4. 为什么不能在 pinned DSH 中发明一个现有 `PromptProvider` interface？
5. 为什么 time-context 是动态上下文，却不是 `SystemPrompt.context()` contributor？
6. same-layer duplicate 与 cross-scope same-name shadow 有什么不同？
7. equal-order Section 与 equal-order Context 的排序规则为什么不能混用？
8. `system-prompt/assemble` waterfall 为什么使 registry dump 不等于 Effective Assembly？
9. `complete: true` 到底终止什么，为什么不等于 request/turn terminal？
10. invalid variable name、unknown reference 与 undefined value 分别在哪个边界失败？
11. 手写 `{scope: agent.scope}` 为什么不能替代 `assembleContextFor(agent, signal)`？
12. 两次 request 的 messages `2 -> 5` 具体新增了什么？哪些字段保持稳定？
13. 为什么第二个 request 仍然保留第一份 runtime snapshot？
14. `source.sections` 保留了什么 provenance？`renderPrompt()` 又丢了什么？
15. 为什么 MockAdapter receipt 不能写成 real DeepSeek Provider call？
16. compaction replacement 后，PromptContext 怎样被窄范围 re-project？
17. stable system 为什么不是从 history 被“重新注入”？
18. `68/68` 与 focused `5/5` 分别覆盖什么证据层？
19. BuildPilot 为什么需要显式 lane 的 `IContextContributor` 候选？
20. Receipt 为什么既要记录 shadow/reject decision，又不能保存 secret plaintext？

## 20. 最短结论

回到开场，messages 从 `2` 变成 `5` 不是“prompt 神秘变化”。Stable system 与 Tool 没变，assistant/tool history 正常增长，PromptContext 又追加了一份有明确 supersedes 语义的新 snapshot。三条通道分别推进，最终在 Step request 边界汇合。

Effective Assembly 解释本 Step 选择了什么；Context Snapshot 为动态贡献保留窄 provenance；terminal MockAdapter request 证明真实 AgentLoop 实际把什么送到了 mock 边界。任何一层都不能单独冒充另外两层，更不能冒充 real-provider wire evidence。

最后压成一句：

> 不要只问“最终 prompt 是什么”；要问哪些 owner 以什么 scope、顺序和更新语义进入了本 Step，以及 assembly、snapshot 与 request 三份收据能否互相对上。

> **上一篇**：[Profile、Bundle、Provider 与 Capability Seam]({{< relref "ai-empowerment/agent-engineering-31-dsh-profile-bundle-capability-seam.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

> **下一篇**：Loop、Turn 与 Step：AgentLoop 怎样推进一次运行（计划中，发布后再补链接）。
