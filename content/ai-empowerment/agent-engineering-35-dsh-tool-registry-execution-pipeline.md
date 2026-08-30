---
title: "Tool Registry 与 Tool Execution Pipeline"
slug: "agent-engineering-35-dsh-tool-registry-execution-pipeline"
date: "2026-08-30T00:00:00+08:00"
description: "从 Registry 可见性与五类 SAME-CALL Trace 出发，拆解 DSH Tool 调用的参数、Policy、执行、持久化和 Model/UI 结果视图。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Tool Runtime"
  - "Tool Registry"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 360
weight: 3360
---

# Tool Registry 与 Tool Execution Pipeline

> **上一篇**：[Append-only Session Event：Replay、Resume、Fork 与 Projection]({{< relref "ai-empowerment/agent-engineering-34-dsh-append-only-session-event.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

“模型已经看见 Tool schema”到底证明了什么？

它最多证明：在这次请求的 model-facing view 中，某个名称、描述和参数结构被投影了出去。它没有自动证明这个调用已经获得权限，没有证明参数经过了某个统一 validator，没有证明 Tool body 真正开始执行，也没有证明执行结果已经以同一种形态进入模型、UI 和持久化 Session。

真实 Tool Runtime 至少同时维护五本账：

```text
Discovery     谁注册、在哪个 scope 可见
Input         模型原始参数怎样成为执行输入
Authority     这次调用是否允许、拒绝或需要询问
Execution     body 是否开始、怎样结束、怎样被取消
Projection    哪个结果给模型、给 UI、给 Session
```

只要把其中任意两本合并，系统就会出现危险的等号：

```text
Registered      = Authorized
Canonicalized   = Safe
Schema-valid    = Side-effect-safe
Provider call   = Tool execution
UI presentation = Model content
Cancel requested = Side effect stopped
```

这些等号都不成立。

所以，本篇真正要闭合的不是“怎样注册一个函数”，而是两条互相连接、又不能混成一条的链：

```text
Registry -> Scope / Dedup -> Model View

Call -> Canonicalize -> Validate -> Policy
     -> Execute -> Normalize -> Persist
     -> Model / UI Views
```

第二条是分析问题时的抽象顺序，不应未经核对就当成具体框架的实际 stage order。固定版 DeepSeek Harness（下称 DSH）恰好给出了一个重要反例：pre-policy 先于 definition dispatch，而 typed `defineTool` 的参数验证位于 definition 的 execute wrapper 内、typed body 之前；raw registration 并没有一个由 Registry 统一提供的同等验证保证。

如果这篇只记一句话，我建议记这个：

> Tool Runtime 不是“把函数交给模型调用”，而是让 capability visibility、input ownership、authority decision、execution terminal 与每一种 result view 都有独立、可关联、不过度承诺的证据。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 为 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。固定版 README 将项目标为 Developer Preview，`SAFETY.md` 明确它没有经过安全审计，不能当作 secure 或 production-ready 系统。

本轮证据账为 `12 / 12 Claims`、`12 / 12 Evidence Cards`。静态 Source Map 闭合了 Registry、policy、execute、result、persistence 与 client projection 的 owner path；五类 required source experiment 最终以 `1 file / 5 tests / exit 0` 取得 `13` 条 SAME-CALL records。

但本轮运行证据只来自临时 source-owned test instrumentation 中组合的 pinned DSH runtime components、repo-owned `MockAdapter`，以及 in-memory Tool / approval / spill fixtures；production service / deployment = `NOT RUN`。没有真实 Provider、生产 Tool、外部副作用、实际 client UI 或 production safety 证据。

## 1. 为什么“Tool 已注册”远远不够

一个最小 demo 往往只做三件事：

```text
register(name, schema, callback)
send schema to model
invoke callback(arguments)
```

这条路径足以演示 Function Calling，却不足以支撑工程运行。

第一个问题是可见性。Tool 是全局可见，还是只对某个 Agent、某个 profile、某个 child scope 可见？重名时覆盖、拒绝还是按 scope shadow？卸载插件以后，旧定义是否还留在 Registry？

第二个问题是输入。模型发来的是一段 raw argument text；JSON parse、lossless snapshot、schema validation 与 Host metadata 注入分别由谁负责？`callId`、AbortSignal 与 agent identity 显然不应被模型伪造进 arguments。

第三个问题是权限。schema 正确，只表示数据形状可被某个 validator 接受；“删除文件”的参数再合法，也不代表这次删除已经获得授权。

第四个问题是执行。超时是强杀还是 cooperative signal？取消发生时，started body 与尚未 dispatch 的 sibling 如何结算？parallel body 的完成顺序是否会改变 Session 中的结果顺序？

第五个问题是结果。Tool 内部的 canonical value、给模型看的 content、给 UI 的 presentation metadata、持久化 event 与 next-step context，是否真是同一份东西？

这些问题不应被一个 `ToolResult` 名字吞掉。

## 2. 先建立两条链与五本账

在进入 DSH 类名之前，先建立一个实现无关的最小模型。

### 2.1 Registry 链回答“模型能看见什么”

```text
Tool Contribution
  -> Registry Identity
  -> Scope / Dedup / Restriction
  -> Model-facing Schema View
```

这条链负责 identity 与 visibility。它不负责某一次调用的 permission，也不负责 body 的 side effect。

### 2.2 Execution 链回答“这次调用发生了什么”

```text
Model-produced Call
  -> Parse / Canonicalize
  -> Validate
  -> Policy / Approval
  -> Execute
  -> Normalize / Post Policy
  -> Persist
  -> Model / UI Views
```

这里的箭头首先是一份核对清单：每段都必须找到 owner、输入、输出、failure branch 与 receipt。具体实现可以调整 stage placement，但不能让某个阶段因为“藏在 callback 里”而从证据链消失。

把两条链合起来，至少要留下五本账：

| Ledger | 最小问题 | 不能被什么替代 |
|---|---|---|
| Discovery | 谁注册、哪个 scope 可见、何时 dispose | schema list |
| Input | raw text、parsed value、canonical snapshot、Host metadata | 最终 callback 参数 |
| Authority | allow、deny、ask、guard、approval | Registry visibility |
| Execution | start、settle、timeout、cancel、error、concurrency | 一个 `success` boolean |
| Projection | canonical value、model content、UI meta、Session event | UI 上的一张卡片 |

后面的源码与 Trace，都围绕这五本账展开。

## 3. Tool 怎样进入 Registry

Pinned DSH 的 Registry owner 是 [`ToolRuntime`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/tools/src/index.ts)。代表性注册入口是 `ToolRuntime.register`。

固定源码中的这条路径是显式 contribution：插件调用 `ctx.tools.register(definition)`，定义进入当前 scoped layer。注册时会核对必要的 output declaration、output JSON Schema、`timeoutMs` 正数约束、reserved `run_code` 与同 layer duplicate；注册返回的值是对应 disposer。

这和“扫描目录自动发现函数”不是一回事。Source Map 闭合的是显式注册路径，没有证明所有 extension 或 profile 都只能通过一种发现机制，也没有证明某个具体 profile 在运行时已经装配了某个 Tool。

注册成功也只有很窄的含义：这个 definition 满足了当前 Registry 的注册合同。它没有获得某次调用的授权。

```text
Registration receipt
  = definition accepted into one Registry layer

Registration receipt
  != execution permission
  != body started
  != side effect safe
```

这就是第一条必须钉死的边界：Registry 不是 Permission。

## 4. Scope、dedup 与 restriction 只管理 capability view

同一 Tool name 可能从 ancestor scope、current scope 或多个插件进入视图。Pinned owner 仍在 `ToolRuntime` 的 `view`、`get` 与 `restrict`。

静态路径给出的组合规则是：

- 同一 layer 的重复名称会拒绝；
- inherited definitions 按最近 scope shadow 更远 scope；
- 多个 scope restriction 对 inherited surface 取交集；
- local registration 仍可保留在自己的可见 surface；
- disposer 负责撤销原 contribution。

这里的 restriction 是 in-process capability visibility，不是操作系统权限，也不是一份执行时的审计记录。一个名称从 model view 中消失，不等于底层文件系统已经拒绝访问；一个名称仍然可见，也不等于当前调用已经被 policy 放行。

PTC mode 进一步说明“可见”和“可执行 reachability”可以分开：model-facing surface 可以只暴露 reserved `run_code`，nested call 再通过 Host-owned parent token 到达 native Tool。本文只使用这个反例说明边界，不把它展开成 PTC 教程，更不把 PTC routing 写成 sandbox。

## 5. Model View 只是一种投影

模型并不会收到整个 executable definition。

`ToolRuntime.wireSchemas`、`schemas` 与 `schemaOf` 把 visible definition 投影为 model-facing schema。固定源码中，native view 只包含：

```text
name
description
parameters
```

下列信息不属于这个 model schema：

- execute function；
- output schema；
- timeout budget；
- concurrency classifier；
- scope 与 restriction；
- AbortSignal；
- agent、parent、runtime token；
- presentation callback。

上游路径在 [`agent.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/agent.ts) 的 `step / buildRequest`：已经持久化的 header tools 进入 request，再交给 `llm.stream`。

静态 source 能证明字段投影与 request owner，不能证明某个真实 Provider 收到了请求、怎样序列化它，更不能证明模型遵守 schema。本轮没有真实 Provider wire capture。

在本文闭合的 DSH native client-tool path 中，Provider 位于模型调用边界：它可以返回 Tool Call 的名称和 arguments，但不是这条路径中的本地 Tool，也不拥有本地 callback 或其外部副作用。这个结论不外推到 Provider-managed built-in / server-executed tools；那些 Tool 可以由 Provider 侧管理或执行，不在本轮 pinned source 与 runtime Trace 的证据边界内。

所以第二条边界是：Provider 不是 Tool。

## 6. 从 raw arguments 到 canonical execution input

模型返回的 arguments 先是一段字符串。

在 [`tool-calls.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/tool-calls.ts) 中，`executeToolCalls -> parseArguments` 尝试 JSON parse。空输入可以变成 `{}`；malformed JSON 不会凭空变成合法对象，而是保留为 raw candidate，继续让下游给出可归属的错误。

进入 `ToolRuntime.createExecution` 后，Runtime 会建立 lossless JSON snapshot 并 deep-freeze，同时注入 Host-owned execution metadata：

```text
model-owned
  arguments

host-owned
  callId
  rootCallId / parent
  agent
  abort signal
  registry token
```

这一步很容易被命名为 canonicalization，但必须控制它的含义。

Canonical snapshot 解决的是“执行期间大家看到同一份不可变输入”。它不回答“这次调用是否允许”，也不回答“参数代表的业务操作是否安全”。

```text
Canonical != Authorized
```

`callId` 也只是 correlation identity，不是 authentication token。把两者混淆，会让可追踪性被误写成权限。

## 7. Validate 在哪里：typed path 与 raw registration 必须分开

DSH 的 typed helper 位于 [`schema.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/tools/src/schema.ts) 的 `defineTool`。

该 wrapper 在进入 typed body 前执行参数 schema validation，失败时产生带 `INVALID_ARGS` 的 `ToolArgsError`。这条路径经过实验验证。

但它不能被扩张成“Registry 会在所有 policy 之前统一验证任何 Tool”。原因有两个。

第一，pinned stage ownership 中，pre-policy 在 dispatch definition 之前；typed `defineTool` 的 validation 位于 definition execute wrapper 内。因此，抽象图中的 `Validate -> Policy` 只是我们希望逐项核对的安全链，不是这里已经证明的统一实际顺序。

第二，raw `ToolDefinition` 可以直接注册自己的 execute。固定 Registry 没有提供证据，证明所有 raw definition 自动获得与 `defineTool` 相同的 input validator。

Trace `35-X01` 正好只确认 typed path：

| callId | raw args | body start | terminal |
|---|---|---:|---|
| `x01-valid` | `{"path":"/ok"}` | `1` | success |
| `x01-malformed` | `{"path":` | `0` | `INVALID_ARGS` |
| `x01-schema` | `{}` | `0` | `INVALID_ARGS` |

三条记录各自都有一个 `tool/call`、一个 `tool/result`，并与 next-request / derived-history 的同 callId result 对上。它证明 malformed 与 schema-invalid 输入没有进入 selected typed body。

它不证明 raw registration 自动 validation，更不证明参数虽然结构正确，文件删除、网络调用或远端 mutation 就是安全的。

所以第三条边界是：schema validation 不是 side-effect safety guarantee。

## 8. Allow、Deny、Ask 不是一个可交换投票

课程前面的 Lab 02 使用过 `Deny > Ask > Allow` 的聚合规则。那是课程 fixture 内确认的一种设计，不能投射成 DSH 的事实。

Pinned DSH 的 pre-policy owner 是 `tools/pre-execute` 与 `prepareExecution`。它是 composition-ordered waterfall：listener 可以调用 `next()` 让后续 listener 继续，也可以直接返回 allow、deny 或 ask 并短路。

```text
tools/pre-execute waterfall
  -> allow -> guards -> dispatch
  -> deny  -> error result, body not dispatched
  -> ask   -> ApprovalService
```

这不是先收集所有 vote，再按固定优先级归并。listener 的 composition order 与是否调用 `next()` 都有语义。

`ask` 会进入同文件中的 `serviceAsk`，再到 [`ApprovalService.request / decide`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/interaction/user-approval/src/index.ts)。固定路径中只有 `allowed-once` 允许 dispatch；`rejected`、`cancelled`、service/answerer unavailable、没有 agent 或 rogue response 都不会静默放行。

随后，`guard / guardReason` 形成 monotonic deny seam：guard 可以 abstain 或给出拒绝理由，不能把前面的拒绝强行改成 allow。

Trace `35-X02` 同时保存三个 call：

| callId | pre decision | approval | body / sentinel | result |
|---|---|---|---:|---|
| `x02-allow` | delegate / allow | none | `1` | success |
| `x02-deny` | deny | none | `0` | error |
| `x02-ask` | ask | rejected | `0` | error |

`x02-ask` 还有一对 linked `approval/asked` 与 `approval/decided(rejected)`。三条都有 terminal Session event 和 next-history correlation。

这个实验确认 selected waterfall/approval path fail closed。它没有运行真实 UI，也没有证明人类实际响应，更没有把 approval、permission 与 sandbox 合成一层。

## 9. Execute 前后还有哪些 hook

pre-policy 通过后，`dispatchScheduledExecution -> dispatchToolBody` 进入 `tools/execute` waterfall，再到 definition execute。

一个成功的 body value 仍需要经过 `createSuccessResult`：

1. snapshot canonical output；
2. 按 output schema 校验；
3. render model-facing content；
4. 可选生成 presentation meta。

之后还有 `tools/post-execute` waterfall。post decision 可以：

- `accept`：替换 model content 或 canonical value，但不能同时替换两者；
- `block`：把已经产生的 outcome 改成 valueless error，并附加 feedback；
- 附加给下一 Step 的 context。

最后才进入 finalizer、lossless materialization 与 `tools/result` observer。

这条顺序有一个非常现实的安全含义：post-policy 发生在 body 之后。如果 Tool 已经写文件、发请求或提交远端事务，post block 可以阻止结果继续以成功形式进入模型，却不能倒转已经发生的 side effect。

```text
Post block != Rollback
```

同样，deny、unknown tool、pre-hook throw、body throw、post-hook throw 与 finalizer failure 可能最终都让模型看到 error content，但它们不共享同一个 failure owner。

`35-X01 / X02` 的 stage records 覆盖了 selected pre / execute / post / result path；本轮没有用 runtime 穷举所有 unknown、pre、body、post、finalizer failure branch。因此正文只能说 source path 已闭合、selected branches 已观察，不能把一份最终错误 JSON 当成全路径证明。

## 10. Timeout 是可选 wrapper，而且是 cooperative 的

单个 Tool 的 `timeoutMs` 只是 definition metadata。没有 compose timeout plugin，它不会自动变成执行 deadline。

固定 timeout owner 在 [`timeout-policy`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/guard/timeout-policy/src/index.ts) 的 `apply`。它作为 `tools/execute` wrapper：

```text
read timeoutMs
  -> derive deadline signal
  -> delegate execution with that signal
  -> wait delegated work settle
  -> if own deadline won, classify TOOL_TIMEOUT
```

关键字是 wait。

它不能强杀一个忽略 signal 的 same-process body。deadline 到达只表示 wrapper 发出了 cooperative cancellation；Tool 何时真正停下，仍取决于 body 是否观察 signal、是否完成 cleanup、远端系统是否有自己的取消合同。

Trace `35-X03` 使用 fake timer 与 latch，而不是随机 sleep：

```text
body.start
-> advance 100ms
-> signal.abort observed
-> Session result count still 0
-> test releases cleanup latch
-> body.settle
-> TOOL_TIMEOUT result appended
```

同组 10,000ms control 正常成功。这个对照确认 timeout terminal 出现在 cooperative drain 之后。

它没有证明 hard kill、外部工作停止、远端请求取消、计费终止或 side effect rollback。

## 11. Caller cancellation 也不等于 rollback

caller signal 与 wrapper signal 会在 Registry execution 中融合。固定源码区分两个边界：

- body 尚未 dispatch：`ABORTED_BEFORE_DISPATCH`；
- body 已开始并在 settle 后被取消归类：`ABORTED`。

Trace `35-X04` 把 `maxParallelToolCalls` 设为 `1`，用两个声明 parallel-safe 的 calls 构造 started 与 held：

```text
x04-started: body start count = 1
x04-held:    body start count = 0

cancel after started receipt
-> started observes signal
-> before cleanup release, no terminal result
-> release and drain
-> started = ABORTED
-> held    = ABORTED_BEFORE_DISPATCH
-> independent follow-up completes
```

started sentinel 已经变成 `1`。实验没有假装取消会把它减回零；它把 sentinel 当作“已经发生过”的 observed state。

这就是第四条边界：Cancel 不是 Rollback。

后续 follow-up 能完成，只证明这个 deterministic controller 在 drain 以后没有永久污染下一轮。完整 run-level cancellation、resume 与 recovery 属于 Article 36，本篇不提前替它下结论。

## 12. 并发执行与结果顺序是两本账

Tool concurrency owner 分布在 `ToolRuntime.executionMode` 与前述 `tool-calls.ts` 的 `runGroup`。

只有 `isConcurrencySafe()` 返回 literal `true` 才进入 parallel candidate；classifier 失败会 fail closed 到 exclusive。exclusive call 形成 barrier，parallel group 则受 `maxParallelToolCalls` 限制。

更重要的是：

```text
dispatch order
settlement order
durable commit order
```

三者不能混成一个时间轴。

body 可以 overlap，也可以反序 settle；pre-policy、finalization、result persistence 与 additional contexts 仍按 model call order commit。这让下一次 model history 保持与 assistant tool-call blocks 相同的顺序。

这部分调度矩阵由 Article 33 的 dependency evidence 已经确认，本篇只闭合 Registry execution mode 接入 scheduler 的 source seam，并用 `35-X04` 的 cap `1` 负例校验 cancellation 下的 started/held 分界。

它不能推出 external side effects 也会按 model order 发生。并发 Tool 对外部世界的真实写入顺序，仍需要 Tool-specific transaction evidence。

## 13. 一个 Tool Result 其实有五种产品

最容易让架构失真的命名，是把所有东西都叫 `result`。

Pinned path 至少包含五条 lane：

| Lane | Owner | 给谁使用 | 关键边界 |
|---|---|---|---|
| canonical value | `createSuccessResult / materializeFinalResult` | runtime、post-policy | 不是 generic durable field |
| model content | definition `render`、`appendToolResult` | 下一次 model request | 不等于 UI meta |
| presentation meta | optional `presentationMeta` | replayable client projection | actual screen 未运行 |
| persisted result | `appendToolCall / appendToolResult` | Session、Replay、Trace | 保存 content/error/meta，不保存任意 raw value |
| additional context | post decision / ordered commit | 后续 Step | 不等于 Tool content |

### 13.1 Canonical value

canonical value 是 Tool output schema 所约束的 runtime value。它可以被 post-policy 处理，但不会作为一个任意对象默认塞进通用 Session event。

### 13.2 Model content

`render` 把 canonical value 转为模型可消费的 `ContentBlock[]`。后续 `appendToolResult` 会把 final content 与 `isError` 放进 user-role tool-result message，供下一次 History projection 使用。

### 13.3 UI presentation

client 侧源码位于 [`client/ui-chat/.../tool.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/client/ui-chat/src/client/conversation-nodes/tool.ts)。它按 callId 配对 durable `tool/call` / `tool/result`，读取 content、error 与 meta 形成 conversation node。

Source presence 只证明这个 client projection path 存在。本轮没有启动 client，也没有实际 screen render。所以不能写成“用户界面已经显示了某种卡片”。

### 13.4 Persisted result

`tool/call` 保存 model-produced raw argument string；`tool/result` 保存 final model-facing content、structured error 与 optional meta，并用 source sequence 关联 call event。

它没有通用字段自动持久化任意 canonical value。这是刻意的 lane separation，不是“把结果存丢了”。

### 13.5 Next-step context

post-policy 可以附加 context，scheduler 在 ordered commit 时把它送入后续 Step。它不是 UI meta，也不应该被误写成 canonical return value。

因此第五条边界是：UI Presentation 不是 Model Content。

## 14. Persist 以后，结果怎样回到模型与 UI

在 `tool-calls.ts` 中，scheduler commit 会先 append `tool/call`，再 append `tool/result`。下一次 `session.deriveMessages()` 把 current surface 投影为 model history，`Agent.step` 再用这份 History 构造 request。

这与上一篇 Article 34 的边界相接：Session event 是 durable fact，Model History 与 UI Transcript 是不同 Projection。

本轮五类 negative trace 都保存了：

```text
callId
raw arguments
stage sequence
body start / settle
normalized result
Session call/result pair
next-request match
derived-history match
```

每条 accepted record 的 Session result 与 next-history content hash 对上。这能确认 selected fixture 中 final model content 被持久化并进入 next model view。

它仍不能确认真实 Provider 收到下一次 request，也不能确认 client UI 怎样渲染。Runtime observation 的上限必须和 source path 分开。

## 15. 大结果：有 spill，但不是 universal summary

固定源码中确实存在大结果处理机制，但不是“任何大结果都会自动总结”。

execution-time owner 位于 [`spill-policy`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/spill/spill-policy/src/index.ts) 的 `apply / spillReplacement`。它是一个 optional post-policy，需要显式 compose、配置 inline cap、可用 agent-owned Session 与 spill backend。

对 oversized all-text result，它会尝试：

```text
save full text
-> produce bounded head/tail preview
-> attach locator + retrieval hint
-> replace model/log projection
```

如果 storage 不存在或失败，source-defined fallback 是 best-effort 保留原来的成功 inline result，不是把它改成 error，也不是凭空生成 summary。

Trace `35-X05` 保存了三条：

| callId | input | storage | model/session projection |
|---|---:|---|---|
| `x05-small` | `4 bytes` | no attempt | inline `tiny` |
| `x05-spill` | `1,600 bytes` | saved；full hash = stored hash | `200 bytes` bounded preview + `/spill/big-ok.txt` |
| `x05-fallback` | `1,000 bytes` | injected failure | exact full inline hash/length retained |

三条 record 都显式写了 `semanticSummary:false`。

仓库另有 post-persistence 的 `ToolResultPruner.pruneSession`，会做 deterministic head/middle/tail prune 与 replacement event。它与 execution-time spill 是两种机制，也不是语义 LLM summary。完整 Compaction 主线属于 Article 36。

因此，当前证据支持：optional spill、bounded preview、locator 与 exact fallback。它不支持 universal spill、retention、authorization、later availability、retrieval UI 或 semantic summarizer。

## 16. 五类负例为什么不能只看“测试通过”

本篇实验最重要的结果，不只是最终五个绿灯，还包括两轮没有被接受的历史。

### 16.1 Cycle 0：22 passed，仍然 BLOCKED_EVIDENCE

第一轮执行了固定仓库已有的 focused owner tests：

```text
35-X01  5 passed
35-X02  7 passed
35-X03  3 passed
35-X04  2 passed
35-X05  5 passed
total   22 passed / 0 failed
```

这些 tests 分别观察到了 invalid args、deny/approval、timeout、cancel 与 spill 的局部性质。

但 frozen acceptance 要求每一类都有同一次 call 的 raw ingress、body count、stage、terminal result、Session event 与 next-history correlation。已有 tests 的观察分散在互补 case 中，不能拼成一条不存在的 end-to-end trace。

所以 `22 passed` 仍被记录为 `NOT_ACCEPTED / BLOCKED_EVIDENCE`。绿灯证明 test 自己断言的内容，不证明缺失 observer 已经隐含成立。

### 16.2 Recovery Attempt 1：exit 0，selected 仍是 0/5

恢复实验创建了一个临时、untracked、source-owned Vitest instrumentation file。第一次命令 `exit 0`，但额外的 top-level suite prefix 改变了完整 test name，anchored pattern 实际选中 `0/5`。

```text
Test Files 1 skipped
Tests      5 skipped
Exit       0
Accepted   NO
```

如果只看 exit code，这一轮会被错误写成成功。真实 acceptance 还必须看 discovery count。

bounded correction 只删除 suite wrapper，没有改变五个 case name、hypothesis、falsifier、input、assertion、安全边界或预算。Attempt 1 的 source、patch 与 output 全部保留，没有被最终绿灯覆盖。

### 16.3 Recovery Cycle 1：13 条 SAME-CALL records

最终 preserved capture 使用同一条 frozen command，结果为：

```text
1 file / 5 tests / exit 0
35-X01 = 3 records
35-X02 = 3 records
35-X03 = 2 records
35-X04 = 2 records
35-X05 = 3 records
total   = 13 records
```

每条 JSONL 都有完整 top-level/nested schema；13 个 callId 唯一；每条都有 `1` 个 Session call、`1` 个 Session result、`1` 个 next-request match 与 `1` 个 derived-history match。

实验 instrumentation 被保存为 course source copy 与 new-file patch，随后只删除 external fixture 中的 exact temporary path。cleanup 后 fixture：

```text
HEAD          = cd5ef8148158c3a752a658978873241fdf8e2bbc
status        = empty
unstaged diff = empty
staged diff   = empty
```

这使 runtime observation 与 pinned pristine source 的关系可解释：instrumentation 没有冒充 untouched behavior，它的 source/patch 与 cleanup receipt 都在证据包里。

## 17. Corepack preflight failure为什么也要写进文章

accepted experiment、MockAdapter 与 Tool body 没有 Provider request、网络操作或真实副作用。但 executor turn 中更早有一次错误 cwd 的裸 `corepack pnpm --version` preflight，尝试访问 npm registry，并被 `EACCES` 阻止。

这次尝试不是五类 experiment command，也没有进入 Provider 或 Tool body。后来从 fixture cwd 重新核对，项目固定 pnpm 为 `11.7.0`，Vitest 为 `4.1.8`。

因此，准确表述是：

```text
accepted experiment / Provider / tool-body network requests = zero
whole executor turn network attempts = not zero
```

把 blanket `NETWORK_REQUESTS=ZERO` 从 manifest 直接抄成“整个执行过程没有网络尝试”，会抹掉真实失败。证据合同的价值，就在于失败发生在实验外也要保留，而不是只留下对结论最有利的一段。

## 18. 五类 Trace 各自证明什么

最终 acceptance 可以收束成下面这张表：

| Trace | Confirmed in selected fixture | Still not proved |
|---|---|---|
| `35-X01` bad arguments | typed valid body `1`；invalid body `0`；`INVALID_ARGS`；Session/next correlated | raw registration automatic validation |
| `35-X02` deny/ask | allow body `1`；deny/ask `0`；approval audit pair；ordered waterfall | vote merge、真实 UI、人类权限体系 |
| `35-X03` timeout | signal -> wait -> cleanup -> settle -> `TOOL_TIMEOUT` | hard kill、remote stop、rollback |
| `35-X04` cancel | started/held 分界、drain、typed abort terminals、fresh follow-up | external side-effect rollback、generic recovery |
| `35-X05` large result | optional full save、200-byte preview/locator、exact inline fallback | universal summary、retention/access/retrieval |

五条同时提供了 result/session/next-history correlation，因此可以支撑本篇 bounded teaching claims。它们不是 DSH 全仓 test health，也不是 production certification。

Article 28 曾记录完整 unit suite 在当时 Windows/sandbox 环境失败。本篇 focused tests 通过，不能反向升级那个全仓结果。

## 19. 怎样设计自己的 Tool Runtime

把上面的 source facts 抽掉类名，真正值得迁移的是三个显式合同。

### 19.1 Registry contract

```text
ToolIdentity
Scope
ModelSchemaProjection
ExecutableLookup
Disposer
```

它回答 capability 是否存在、对谁可见，以及何时撤销。不要让 permission 与 Registry lifecycle 共用一个 boolean。

### 19.2 Execution contract

```text
raw argument digest
canonical argument reference
validation owner/result
policy + approval receipt
body start/settle
timeout/cancel signal receipt
typed terminal
```

它回答一次调用经历了哪些 stage。只保存最终文本，无法区分 deny、invalid args、body throw 与 post block。

### 19.3 Projection contract

```text
canonical value reference
model content reference
UI metadata reference
Session call/result seq
next-step context reference
spill locator
redacted diagnostics
```

它回答不同 consumer 拿到什么。默认把完整 canonical value、secret arguments 或大结果复制进所有 lane，既浪费上下文，也扩大泄漏面。

## 20. BuildPilot 候选：ToolExecutionReceipt，只到 Proposal

对未来 BuildPilot，可以考虑一份显式 receipt：

```ts
// COURSE PROPOSAL ONLY — not a current DSH or BuildPilot API.
interface ToolExecutionReceipt {
  callId: string
  toolIdentity: string
  scopeRef: string
  rawArgumentsDigest: string
  validation: 'passed' | 'failed' | 'not-applicable' | 'unknown'
  policy: 'allow' | 'deny' | 'ask' | 'unknown'
  bodyState: 'not-started' | 'started' | 'settled'
  terminalKind: string
  modelContentRef?: string
  sessionEventRef?: string
  spillRef?: string
  diagnosticRef?: string
}
```

这份草案只是在回收本篇已经闭合的分账问题。它不是 DSH 内置统一类型，也不是 BuildPilot 已完成的 schema、ADR、代码或 runtime。

统一 receipt 也有风险：如果把 raw arguments、canonical value、credentials 与 diagnostics 全塞进去，它会变成新的敏感数据聚合点。因此更合理的方向是保存 digest / reference 与 redacted diagnostics，而不是默认复制全部内容。

是否采用、简化、拒绝或延后哪些具体机制，要等 Article 37 的最终 DSH 决策矩阵。本文只把 `ToolExecutionReceipt` 标为 `COURSE_PROPOSAL / DEFER`，不启动 Part VII。

## 21. Evidence Boundary

本篇可以确认：

- official DSH fixed tag / full commit 与 Developer Preview / SAFETY posture；
- pinned source 中的显式 registration、scope view、dedup/restrict/dispose 与 model schema projection；
- `tool-calls -> parseArguments -> createExecution -> pre-policy -> execute -> normalize/post -> result -> Session -> next model view` owner path；
- typed `defineTool` selected validation path；
- ordered waterfall、ask/approval 与 monotonic guard seam；
- optional timeout wrapper、cooperative cancellation、explicit concurrency 与 model-order commit；
- canonical value、model content、UI meta、Session event 与 additional context 的分离；
- optional in-memory spill、bounded preview/locator 与 exact inline fallback；
- `35-X01—X05` 在 pinned fixture + MockAdapter + in-memory instrumentation 下的 accepted observation。

本篇不能确认：

- 某个真实 profile 实际注册了哪些 Tool；
- raw direct registration 自动获得 typed validation；
- 真实 Provider wire/request/response 与模型 schema conformance；
- production Tool、文件/命令/网络或远端副作用；
- Approval UI、真实人类决定、完整 permission/sandbox 安全；
- actual client UI render；
- timeout/cancel hard kill、rollback、remote quiescence、计费停止或 run-level recovery；
- universal spill、retention、authorization、later retrieval 或 semantic summary；
- production readiness、安全审计、跨平台整体 test health；
- Article 36/37 结论、Part VII Architecture 或 BuildPilot Runtime。

证据分类始终分开：

```text
OFFICIAL_DOC         文档姿态
PINNED_SOURCE        owner / symbol / call path
RUNTIME_OBSERVATION  selected fixture 的实际记录
EXPERIMENT           frozen design + falsifier + accepted trace
INFERENCE            从多张卡推导的工程解释
COURSE_PROPOSAL      未来 BuildPilot 候选
```

Source exists 不等于 runtime executed；test passes 不等于 production guarantee；MockAdapter 不等于 real Provider；extension seam 不等于 capability already built in。

## 22. 学习检查

1. Registry、Permission 与 Provider 为什么是三个不同 owner？
2. model schema、raw arguments、canonical snapshot 与 Host metadata 分别属于哪一层？
3. 为什么 typed `defineTool` 的验证不能推广到 raw registration？
4. DSH pre-policy 为什么是 waterfall，而不是 `Deny > Ask > Allow` vote merge？
5. post-policy block 为什么无法保证 side-effect safety？
6. timeout 与 caller cancellation 为什么都需要 signal、drain 和 terminal 三类 receipt？
7. dispatch、settlement 与 durable commit order 为什么必须分账？
8. canonical value、model content、UI meta、Session result 与 additional context 分别服务谁？
9. spill failure 为什么保留 inline success，而不是被写成 summary？
10. Cycle 0 的 `22 passed` 与 Recovery Attempt 1 的 `exit 0` 为什么都不满足 acceptance？

## 23. Claim 与 Evidence Traceability

| Claim | Final status | 本文落点 | Evidence ceiling |
|---|---|---|---|
| `35-C01` fixed identity / safety posture | `DOC_CONFIRMED` | 开篇、Section 21 | 文档不证明 Tool behavior 或 production safety |
| `35-C02` registration / scope / dispose | `SOURCE_CONFIRMED` | Sections 3—4 | 不推断某 profile 实际 composition |
| `35-C03` model schema / executable / Host metadata 分 lane | `SOURCE_CONFIRMED` | Sections 5—6 | 无真实 Provider wire capture |
| `35-C04` raw / canonical / typed validation | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED_FOR_TYPED_PATH` | Sections 6—7、`35-X01` | raw registration 不继承 typed guarantee |
| `35-C05` ordered policy / approval | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED` | Section 8、`35-X02` | 不是 vote merge，不替代 permission / sandbox |
| `35-C06` error stage ownership | `SOURCE_CONFIRMED / SELECTED_BRANCHES_OBSERVED` | Section 9、`35-X01—X02` | 未 runtime 穷举所有 failure branch |
| `35-C07` timeout / cancel | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED` | Sections 10—11、`35-X03—X04` | cooperative only；无 hard kill / rollback |
| `35-C08` concurrency / ordered commit | `SOURCE_CONFIRMED / DEPENDENCY_BOUNDED` | Section 12 | 不重称 Article 33 全部 runtime matrix |
| `35-C09` result lanes / persistence | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED_FOR_SESSION_AND_NEXT_MODEL_VIEW` | Sections 13—14 | actual UI / real Provider 未运行 |
| `35-C10` optional spill / fallback | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED_FOR_OPT_IN_SPILL` | Section 15、`35-X05` | 无 universal summary / retention / retrieval guarantee |
| `35-C11` five-case source experiment | `EXPERIMENT_CONFIRMED` | Sections 16—18 | Cycle 0 与 Attempt 1 保持 `NOT_ACCEPTED` |
| `35-C12` ToolExecutionReceipt | `COURSE_PROPOSAL / DEFER` | Section 20 | 不是 DSH 或 BuildPilot 已实现事实 |

覆盖结果：`12 / 12 Claims`、`12 / 12 Evidence Cards`、`35-X01—X05` 五类 required traces；所有 source、runtime、experiment 与 proposal 结论继续按层分账。

## 24. 最短结论

一条 Tool call 从来不只是“模型给参数，Host 调函数”。模型先看到 Registry 的一个有限投影；raw arguments 经过解析与 canonical snapshot；typed validation、policy、approval、guard、execute wrapper、body、post-policy 与 finalizer 各有自己的 owner；最后 canonical value、model content、UI meta、Session result 与 next context 再走向不同消费者。

五类负例把最容易被成功路径遮住的边界逐一暴露出来：bad arguments 没进入 typed body，deny/ask 没有 fail open，timeout 与 cancel 都等待 cooperative drain，大结果 spill 失败时保留 exact inline fallback。与此同时，Cycle 0 和错误选中 `0/5` 的 Attempt 1 也继续留在证据账里。

最后压成一句：

> 先分清 Tool 能否被看见、这次调用能否被允许、body 是否真的执行，以及哪个结果被谁消费；只有这些 receipt 能按 callId 对上，Tool Runtime 才从“会调用函数”变成可审计的工程系统。

> **上一篇**：[Append-only Session Event：Replay、Resume、Fork 与 Projection]({{< relref "ai-empowerment/agent-engineering-34-dsh-append-only-session-event.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})
