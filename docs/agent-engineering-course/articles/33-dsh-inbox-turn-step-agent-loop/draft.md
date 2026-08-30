# Inbox、Turn、Step 与 Agent Loop

> **上一篇**：[System Prompt Assembly 与 PromptContext]({{< relref "ai-empowerment/agent-engineering-32-dsh-system-prompt-assembly-prompt-context.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

一次 Agent Run，究竟等于几次模型回复？

在本篇固定实验的 single-tool trace 里，答案至少不是“一次”。Host 只投递了一条 user input，Runtime 只打开了一个 Turn，但模型收到了两次 request：第一次要求调用 `echo`，Tool result 被持久化以后，第二次 request 再把这个 result 送回模型，最终才得到 no-tool response。

```text
Host input: 1
Turn: 1
Step: 2
Model request: 2
Tool call/result pair: 1
```

如果把 Agent Run 描述成“模型回复一次”，Tool result 回到模型的第二段链路就无处安放。如果把 Turn 和 Step 当成同义词，取消、重试、steering 与 replay 又会共用一个含糊的 `done`。

问题的本体不是 DeepSeek Harness（下称 DSH）里有没有一个 `while`，而是一次运行怎样获得可审计的边界：Host input 何时进入 durable Inbox，什么事件打开 Turn，什么条件才打开 Step，一批 Tool 怎样并发却有序提交，以及 Policy、Budget、Error、Cancellation 最终由谁决定继续或停止。

如果这篇只记一句话，我建议记这个：

> Agent Loop 的可靠性不来自一个循环语句，而来自 Inbox、Turn、Step 与 Tool Batch 各有明确 owner、durable receipt 和 typed termination；“停止”也永远不能自动升级成“成功”。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 是 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。实验前后，外部 fixture 的 `HEAD` 与 tag target 均等于该 SHA，working tree 与 diff 为空。

本文证据账为 `15 / 15 Claims`、`15 / 15 Evidence Cards`：`14 CONFIRMED / 0 PARTIAL / 1 PROPOSAL / 0 BLOCKED`。四条 required Trace 全部通过，selected owner tests 为 `10 / 10`，另有两条 read-only observation `2 / 2 exit 0`。

但 runtime 终点只是 production `AgentLoop` 配合 repo-owned `MockAdapter` 与 deterministic in-memory Tools。本轮没有真实 Provider、credential、network、billing、生产数据、OS hard-kill 或外部副作用 rollback 证据。

## 1. 一个 `done` 会吞掉哪些工程事实

许多原型把一次 Agent 调用写成下面这样：

```text
receive prompt
  -> call model
  -> maybe run tools
  -> done
```

它可以跑通 demo，却回答不了五个上线后常见的问题。

第一，输入从哪里来？Browser Chat、Headless runner、SDK 或 extension 都可能投递任务。Runtime 若把 Inbox 写成 UI 内部队列，第二种 Host 出现时就要重写执行主链。

第二，什么叫一次运行？一次 user intent 可能先后经历 no-tool response、Tool call、Tool result 回送、steering input 和 retry。用 model call 数量定义运行，会让一次逻辑工作被拆成多个互不相干的片段。

第三，什么叫一步？Prompt assembly、history derivation、model stream、parse、assistant anchor 和 Tool Batch 必须在哪个边界闭合，决定了取消后是否能 replay，也决定了故障诊断能否对上事件。

第四，谁有权继续？Tool success 不一定终止 Turn，Tool error 也不一定让 Turn error。队列里是否还有 next-step input、Tool 是否留下待解释的 result、extension 是否 steer，都可能改变下一步。

第五，谁有权宣告成功？`completed` 只是一种 Runtime end reason。业务目标是否完成、外部副作用是否落地、远端系统是否确认，属于更高层证据。

因此，一条可靠的 Loop 至少要把 delivery、durability、iteration、side-effect scheduling 和 termination 拆开。否则所有异常最后都会落成一个无法审计的 `done=false`。

## 2. 先建立一个不依赖 DSH 类名的五层模型

在进入源码之前，先把一次 Agent Run 抽象成五层。

```text
Host
  -> Inbox / durable delivery event
  -> Turn
     -> Step 1
        -> assembly -> model -> parse -> optional Tool Batch
     -> Step 2 ...
  -> typed Turn termination
```

### 2.1 Host：表达 intent，不拥有执行循环

Host 负责把输入、route、attachment 与 delivery mode 交给 Runtime。Browser、Headless、SDK 可以是不同表面，但不应该各自实现一套 Turn/Step 状态机。

### 2.2 Inbox：持久化待处理输入，不等于 Chat UI

Inbox 是 Runtime 的 Host-neutral queue。它至少要记录 target、position、message identity 与 durable mutation receipt；UI notification 只是这份 durable state 的 live projection。

### 2.3 Turn：一次可持久化推进区间

Turn 从一个 durable `turn/start` 开始，到一个 typed `turn/end` 结束。它可以包含零个、一个或多个 Step。

零 Step 并不是理论漏洞：如果 first claim 为空，或者 `agent/pre-step` extension 拒绝 admission，Turn 已经打开，却不应伪造一次 model request。Tool Policy 位于 Step 内的 Tool pipeline，形成 Tool outcome，不拥有这条 zero-Step admission rejection。

### 2.4 Step：一次 model request 与其 response debt

Step 不是 Tool call。它包含本次 assembly、进入 Session 的 messages、frozen request、model stream/parse、assistant anchor，以及该 response 产生的 Tool Batch。

如果 Tool result 需要模型继续解释，下一次 model request 是同一 Turn 的下一个 Step。

### 2.5 Tool Batch：同一 response 的 ordered side-effect schedule

Tool Batch 是一个 Step 内由 assistant message 产生的 ordered calls。它可以有 bounded parallelism 和 exclusive barrier，但没有 Agent creation、delegation 或 handoff，因此不是 Multi-Agent。

这五层之外还需要一份 typed termination receipt。它至少区分 `completed`、`blocked`、`max-tokens`、`aborted` 与 `error`，而不是只给一个 boolean。

## 3. 两个 Host 怎样汇合到同一条 Inbox seam

固定源码里，Browser 和 Headless 已经构成一个直接反例：Inbox 不属于 Chat UI。

Browser-facing 路径从 [`SessionCommands.prompt`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/api/session-controller/src/commands.ts) 开始。它验证 route 与 attachments，创建带 source metadata 的 user message，再根据 mode 调用 `agent.steer()` 或 `agent.followup()`。

Headless 路径从 [`packages/bundle/headless/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/headless/src/index.ts) 的 `run` 开始。它创建 Agent，调用 `followup(createUserMessage(task))`，等待 idle，再从 durable `turn/end` 推导进程 exit code。

两条路径最终都进入 [`ReactLoopAgent`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/agent.ts) 的公共 delivery seam：

```text
Browser SessionCommands.prompt
  -> agent.steer | agent.followup

Headless run
  -> agent.followup

Agent delivery seam
  -> send(message, target, wakeup)
  -> Inbox.splice(...)
  -> wakeDriver() when required
```

三个入口的语义不能压成“往队列 append”：

| Entry | Target | Wakeup | Engineering meaning |
|---|---|---:|---|
| `followup` | `next-turn` | yes | 为下一 Turn 投递一条 waking input |
| `steer` | `next-step` | yes | 让 active/next Turn 在 Step 边界吸收 steering |
| `inject` | `next-step` | no | 注入 next-step input，但不独立唤醒 driver |

[`Inbox`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent/src/inbox.ts) 维护 `next-turn` 与 `next-step` 两个 ordered lists。Mutation 会先 append durable `agent/inbox/spliced`，再更新 live projection，并发出 inserted、discarded 或 claimed notification。

这就是第一处分账：durable splice 证明 Session 里发生过什么；live notification 让当前进程响应变化。二者相关，但不是同一份证据。

## 4. Turn 为什么不是 Step 的别名

driver 的主链可以压成：

```text
wakeDriver
  -> reserve fresh AbortController
  -> kick
     -> while (await turn()) {}
```

`turn()` 会先 append `turn/start`，再尝试 `preStep("next-turn")`。也就是说，Turn ownership 先于 first claim，更先于 `step/start`。

如果 first admission 为空，Turn 可以直接以 completed 结束；如果 `agent/pre-step` reject，Turn 以 blocked 结束。两种路径都可能是：

```text
turn/start
  -> claim / admission
  -> turn/end

Step count = 0
```

只有 admitted proposal 才会 append `step/start`。一旦 Step 打开，`step/end` 必须在 `finally` 里平衡；Turn 则由它自己的 `finally` 写出一个 structured `turn/end`。

这一层设计不是为了“日志好看”。如果取消发生在 stream 中间，或者 Tool scheduler 抛错，balanced Step/Turn boundaries 是 replay、诊断和后续恢复还能找到锚点的前提。

## 5. 一个 Step 怎样闭合 assembly、model、parse 与 events

Article 32 已经解释了多来源 Context 怎样在 Step 边界组成 request。本篇继续向下看执行所有权。

### 5.1 `preStep`：还没开 Step，先决定是否准入

`preStep` 依次完成：

1. 从 Inbox claim 对应 target 的 messages；
2. 调用 System Prompt assembly；
3. project dynamic context；
4. 运行 `agent/pre-step` waterfall；
5. 返回 reject 或 admitted messages。

Assembly failure 或 reject 发生时，不能伪造 `step/start`。这使“提议一次 Step”和“真正打开一次 Step”成为两个边界。

### 5.2 admitted messages 先成为 durable history

Turn append `step/start` 后，会把 admitted messages 写成 `user/message`。随后 `step()` 从 Session surface derive 当前 history，而不是从一个临时数组假装历史已经持久化。

### 5.3 request 在模型调用前被冻结

`step()` render system、derive messages，再进入 `buildRequest()`。后者运行 `agent/request` waterfall，解析 route，调用 `LlmRuntime.prepareCall()`，记录 request header/context，并构造带同一 Turn signal 的 frozen `GenerateOptions`。

模型边界不是简单的 `adapter.generate(prompt)`：adapter selection、dispatch、async iteration 都可能失败，失败会先被 LLM Runtime 正规化为 terminal finish chunk。

### 5.4 stream chunk 先持久化，再形成 assistant anchor

每个 `StreamChunk` 先 append 为 `assistant/chunk`，再交给 [`BlockAssembler`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/llm/llm/src/assembler.ts) 解析 text、reasoning、tool-call partial、usage 与 finish reason。

stream 结束后，assembled blocks 形成一个 `assistant/message`，并用 source event seqs 指回 chunks。这个 durable assistant anchor 之后，Runtime 才决定是否执行 Tool Batch。

完整 Step 主链是：

```text
preStep
  -> step/start
  -> user/message*
  -> render + derive history + build frozen request
  -> assistant/chunk*
  -> BlockAssembler
  -> assistant/message
  -> optional tool/call + tool/result*
  -> step/end
```

Source path 可以证明这些 owner 与顺序；它不能证明真实 DeepSeek HTTP wire、真实模型输出或生产网络行为。后面的 runtime trace 也只到 MockAdapter。

## 6. Trace X01：没有 Tool 时，Turn 怎样自然闭合

X01 使用 production AgentLoop、Session/SystemPrompt/Tool services 与 repo-owned MockAdapter。输入是 `no-tool`，MockAdapter 返回确定性文本和 normal finish。

Observed receipt 是：

```text
requestCount=1
toolCallCount=0
toolResultCount=0
finalStatus=idle

agent/inbox/spliced
turn/start(1)
agent/inbox/spliced          # claim receipt
step/start(1,1)
user/message
request/header
request/context
assistant/chunk*
assistant/message(1,1)
step/end(1,1)
turn/end(1,completed)
```

所有 seq 严格递增，Step 与 Turn boundaries 平衡。它建立的是：在该 deterministic fixture 中，no-tool normal finish 产生 `1 Turn / 1 Step / 1 request / 0 tool events`，最后 Agent 回到 idle。

它没有建立 rejected admission、zero-Step path、Policy、Cancellation 或 real Provider behavior。

## 7. Trace X02：一个 Tool result 为什么产生下一 Step

X02 的第一条 mock response 产生：

```text
callId=c1
tool=echo
arguments={text:"ping"}
```

deterministic Tool 返回 `echo: ping`。Runtime append `tool/call` 与 `tool/result`，result 的 `sourceEventSeqs` 指回 call event。因为这个 Tool success 没有 `concludesTurn`，Step 1 返回的不是 Turn terminal，而是“仍有 response debt”。

于是同一个 Turn 打开 Step 2。第二次 request 精确包含：

```text
type=tool-result
toolCallId=c1
text="echo: ping"
isError=false
```

第二条 mock response 返回 final text，没有新 Tool call，Turn 才以 completed 结束。

```text
Turn 1
  Step 1
    request 1
    assistant tool-call c1
    tool/call c1
    tool/result c1
  Step 2
    request 2 includes result c1
    assistant final text
  turn/end(completed)
```

Observed total 是 `1 Turn / 2 Steps / 2 requests / 1 linked call-result pair`。这直接拆开了三个常见误解：Tool call 不是新 Turn，Tool success 不是 task success，下一次模型调用也不一定代表新 user intent。

## 8. Tool Batch：并发执行与有序提交必须分开

当一个 assistant message 产生多个 Tool calls，最粗糙的实现是 `Promise.all(calls)`。它忽略了 exclusive Tool、并发上限、policy stage 与 durable result order。

固定源码的 scheduler 位于 [`tool-calls.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/tool-calls.ts)，核心链是：

```text
executeToolCalls
  -> parse ordered ToolCallBlocks
  -> classify execution mode
  -> exclusive group | parallel group
  -> runGroup
     -> ordered prepare
     -> bounded dispatch pool
     -> independent settlement slots
     -> commitReady in model order
```

只有明确分类为 `parallel` 的 calls 才允许 body overlap；其他 calls 是 exclusive。exclusive call 构成 barrier，必须等前一组完成，且后一组不能提前跨过它。

parallel group 也不是“全部一起发”。`fillPool` 只补充到 `maxParallelToolCalls`，某个 body settle 后才滚动补位。

更关键的是，dispatch order、settlement order 与 durable commit order 是三件事。body 可以反序完成，但 `commitReady` 只在 model-order slots 连续就绪时推进，按原 call order finalize：

- `tool/result`；
- deferred additional context；
- `concludesTurn` aggregation。

这样下一次 request 看到的 Tool history 仍与 assistant message 中的 call order 对齐，不受线程调度时机影响。

## 9. Trace X03：Overlap、barrier 与 model-order commit

X03 用多组 deterministic owner fixtures 闭合一个 timing-sensitive 问题，而不是依赖随机 sleep。

### 9.1 bounded overlap

在 `maxParallelToolCalls=2`、model order `c1,c2,c3,c4` 下，最初只启动 `c1,c2`。释放 `c1` 后，`c3` 才启动。最大 in-flight 没有超过 2，而且确实到达 2。

### 9.2 exclusive barrier

对 `parallel A1 -> exclusive A2 -> parallel A3`，body order 是：

```text
start A1 -> end A1 -> run A2 -> start A3 -> end A3
```

`A2` 与两侧都没有 overlap。

### 9.3 out-of-order settlement, in-order visibility

`c2` 被故意先 release。此时 durable result 不能越过未完成的 `c1`；最终 results、additional contexts 与 derived next-request history 都按 `c1,c2` 提交。

```text
settlement: c2 -> c1
durable results: c1 -> c2
contexts: ctx-c1 -> ctx-c2
next request tool history: c1 -> c2
```

X03 证明的是 selected owner fixture 下的 cap、overlap、barrier 与 ordered aggregation。它不证明任意 Tool thread-safe，不证明外部 side effects 按 model order 发生，也没有创建任何 child Agent。

因此：Tool Batch 不等于 Multi-Agent。

## 10. Continue / Stop：下一步不是一个 owner 的决定

一次 Step 结束后，Runtime 不能只读 `done`。固定路径至少包含下面这些决策输入：

| Current outcome | Immediate candidate | 仍可能改变什么 |
|---|---|---|
| response 无 Tool call | `completed` | `agent/turn-stopping` listener 可 steer next-step input |
| finish=`max-tokens` | `max-tokens` | 这是非成功 terminal，截断 Tool calls 不 dispatch |
| Tool calls，无 concluding success | continue | Tool results 要在下一 Step 回模型 |
| successful Tool 调用 `concludeTurn()` | `completed` | 已排队 next-step input 仍须 drain |
| `agent/pre-step` reject | `blocked` | 该 proposal 不打开 Step |
| unhandled model/extension failure | `error` | 当前 Turn 关闭，driver 仍可服务后续 Turn |
| active signal aborted | `aborted` | cause 使用 first typed abort reason |

这里有两个 ownership 层。

Step 根据 model finish 与 Tool Batch 产生一个 candidate。Turn 再检查 next-step Inbox，并 dispatch serial `agent/turn-stopping`。listener 可以在这个窗口 steer，新 debt 会要求 another Step。

因此，`concludesTurn` 也不是一把全局开关。它只表示一个 successful Tool result 提出 completed candidate；已经存在的 steering input 仍然有处理权。

更不能把 `turn/end(completed)` 写成“业务任务成功”。Headless Host 的确只把 completed 映射为 exit 0，但那是 Host 的 exit policy，不是外部世界已确认的证明。

## 11. Policy、Budget 与 Error 分别站在哪一层

### 11.1 Policy denial 是 Tool outcome，不是 Turn verdict

Tool pipeline 的 `deny` 或 approval refusal 会跳过 Tool body，却仍 materialize 一个 canonical `isError=true` result。这个 result 通常进入下一 Step，让模型看到拒绝原因。

所以 Policy denial 不应直接改写成 Turn error，也不能被忽略成“没有执行就没有事件”。完整 policy merge、approval 与 Tool pipeline 属于 Article 35；本篇只确定它与 Loop 的交界。

### 11.2 pinned alpha 没有通用 Turn/Step/cost budget

固定版本里的 `maxTokens` 是 per-request output ceiling。模型以 `max-tokens` finish 时，这个 reason 会成为当前 Turn 的 sticky non-success terminal。

`maxParallelToolCalls` 是并发 cap；单 Tool 的 `timeoutMs` 是 result-scoped timeout。三者都不是总 token、总 cost、总 Step 或总 Turn budget。

对 pinned production path 的 bounded search 没有找到 generic Turn/Step/cost budget listener。这个结论必须保持版本与搜索范围：它不是“DSH 永远不会有预算”，也不替 Article 36 的 usage/cost/recovery 研究抢答。

### 11.3 Error retry 有明确 owner

adapter selection、dispatch 或 iteration failure 会先正规化为 terminal finish，再交给 `agent/request-error` waterfall。只有 listener 显式返回 `{kind:"retry"}`，当前 Step 才重试 request。

未处理的模型错误成为 structured `LlmError` 并关闭当前 Turn；middleware、consumer 或 extension thrown failure 则走另一条 structured UNKNOWN/error 路径。Tool `isError` result 通常不 throw 到 Turn，因为它需要先回模型。

这三种 failure 不应该共用一个 catch-all 文本。

## 12. Cancellation：一条 signal spine 怎样穿过 Loop

Browser 取消入口会解析 live Agent，再调用：

```text
agent.cancel({kind:"user"}, {keepInbox:true})
```

`ReactLoopAgent.cancel()` 对 active phase 的 `AbortController` 调用 abort，first cause wins。同一 signal 继续进入：

```text
System Prompt assembly
  -> agent/pre-step
  -> agent/request
  -> LlmRuntime.prepareCall
  -> frozen GenerateOptions.signal
  -> adapter stream
  -> executeToolCalls
  -> ToolExecutionInput.signal
```

这是一个 Turn-scoped cancellation spine，而不是 global sticky flag。driver 后续处理新的 waking input 时，会得到 fresh controller。

但 signal 传播不等于副作用回滚。same-process Tool 只能 cooperative observe abort；已经执行的文件写入、HTTP request 或远端 mutation 不会因为 Session 多写一条 aborted event 就自动撤销。

## 13. Trace X04：Cancel 是 cooperative drain，不是 rollback

X04 在 cap=2 的 Tool Batch 中启动 `c1,c2`，随后用 deterministic latch 触发 cancellation。

Observed behavior 是：

1. scheduler 停止 replenishment；
2. 已启动的 `c1,c2` 被等待并 drain；
3. 未启动的 `c3,c4` body 执行次数为零；
4. 未启动 calls 仍有 synthetic `tool/call` / `tool/result` pair；
5. synthetic results 为 `isError=true`、`code=ABORTED_BEFORE_DISPATCH`；
6. result commit order 仍是 `c1,c2,c3,c4`；
7. 当前 Turn 以 `aborted(user)` 结束；
8. 后续 waking prompt 用 fresh signal 开启新 Turn，并正常 completed。

另一个 selected fixture 在 assistant message observer 触发 cancel。危险 Tool body 运行零次，但 `c1` 仍得到 balanced error result，下一次 request 可以 replay 这份记录。

mid-stream cancellation 则保留已经可见的 text prefix `partial`，assistant anchor 标记 `interrupted=true`，再依次写 `step/end` 与 `turn/end`。后续 request 能看到相同 prefix，而不是凭空丢失用户已经看见的输出。

这套语义叫 cooperative drain plus replay balancing。它没有证明：

- OS/process hard-kill；
- 真实 Provider 接受 remote cancel；
- server 已停止生成或计费；
- 已发生的外部 side effect 被 rollback。

所以：Cancel 不等于 Rollback。

## 14. 四个不得等同的边界

前面的源码与四条 Trace 可以压成四条主边界。

### 14.1 Inbox 不等于 Chat UI

Browser 与 Headless 两种 Host 都经公共 Agent seam 写入 Inbox。Inbox owner 是 Runtime queue 与 durable splice，不是聊天界面。

### 14.2 Turn 不等于 Step

Turn 在 first claim 前打开，可以有 zero Step；single-tool trace 又展示一个 Turn 有 two Steps。两端反例同时成立。

### 14.3 Tool Batch 不等于 Multi-Agent

Tool scheduler 只调度一个 assistant message 的 calls。它没有创建 Agent、分配子任务、handoff context 或聚合多个 Agent 的 lifecycle。

### 14.4 Stop 不等于 Success

`completed`、`blocked`、`max-tokens`、`aborted`、`error` 都会让当前 Turn 停止。只有更高层验收证据才能判断业务目标是否完成。

这四条边界看似是术语校正，实际决定 schema。只要把它们混掉，receipt、retry、budget、replay 与 UI state 就会跟着混掉。

## 15. BuildPilot implication：只是一组 Part VII 候选

DSH 的 source path 支持一个迁移判断：BuildPilot 将来不应只保存 `done`，而可以候选采用显式 lifecycle receipts 与单一 cancellation spine。

```ts
// PROPOSAL ONLY — not current DSH API and not implemented.
type TerminationReason =
  | { kind: 'completed' }
  | { kind: 'blocked'; reason: string }
  | { kind: 'max-tokens' }
  | { kind: 'aborted'; cause: string }
  | { kind: 'error'; errorRef: string }

interface TurnReceipt {
  readonly turnId: string
  readonly startEventRef: string
  readonly stepIds: readonly string[]
  readonly endReason: TerminationReason
}

interface StepReceipt {
  readonly stepId: string
  readonly requestRef: string
  readonly assistantRef?: string
  readonly toolBatchRef?: string
  readonly endReason: 'continue' | TerminationReason
}
```

这不是 DSH API，也不是 BuildPilot 已完成设计。当前没有 ADR、implementation、migration、runtime、performance 或 security review。

第一轮 acceptance candidates 可以来自本篇四条 Trace：no-tool one-Step、single-tool two-Step、parallel/barrier ordered commit、cancel synthetic balancing。但是否采用、怎样命名、怎样与 Part VII architecture 集成，必须留给 Part VII；当前没有 ADR、代码或 runtime，Part VII 也没有启动。

## 16. 验证与 Evidence Boundary

四条 Trace 都应保存 event seq、turn/step/callId、request count、Tool start/settle/commit order、Turn reason、signal identity 与命令退出状态。X01—X04 分别以“多开 Step”“result 未回 request 2”“cap/barrier/order 失守”“cancel 后继续 dispatch 或旧 signal 污染新 Turn”为 falsifier。

本轮先记录了 host-global `pnpm exec` 的 PATH failure，再使用 fixture 已存在的 workspace-local Vitest；没有 install 或 network fallback。失败命令与最终绿灯同时保留，避免把 environment gate 和 product-behavior gate 混成一个结果。

Source 建立 owner、branch 与 call path；selected owner tests 和 read-only observations只建立 MockAdapter/in-memory fixture behavior；bounded absence 只表示 pinned production search 未发现 generic budget。它们都不能证明 real Provider、network、model、billing、hard-kill、任意 Tool thread safety或 external rollback。

最终 evidence receipt 是：

```text
Claims / Cards: 15 / 15
14 CONFIRMED / 0 PARTIAL / 1 PROPOSAL / 0 BLOCKED
Required traces: 4 / 4 PASS
Selected owner tests: 10 / 10 PASS
Inline observations: 2 / 2 exit 0
Runtime: production AgentLoop + MockAdapter + deterministic in-memory Tools
Real Provider / network / billing: NOT TESTED
BuildPilot lifecycle receipts: PROPOSAL ONLY
Part VII: NOT STARTED
```

## 17. 后续文章只接 owner

上一篇负责 stable system、dynamic snapshot 与 durable history 组成 request；本篇只负责 request 所处的 Loop、Turn、Step、Tool Batch 与 termination。Article 34 才研究 Session continuation、Replay 与 Fork；Article 35 才展开完整 Tool pipeline；Article 36 负责 Cost、Compaction、Recovery；Article 37 再做 extension mapping。

Article 38—44 与 Part VII 保持 `NOT STARTED`。当前不创建 Article 34 future link，也不提前写它们的结论。

## 18. 学习检查

1. Browser 与 Headless 怎样进入公共 Inbox seam？
2. 为什么 Turn 可以有 zero or multiple Steps？
3. X02 为什么是一个 Turn、两个 Steps？
4. parallel settlement 为什么不能决定 durable result order？
5. Tool Batch 为什么不是 Multi-Agent？
6. `concludesTurn` 为什么仍可能被 next-step debt 改变？
7. 三种 cap 为什么都不是 generic budget？
8. cancellation spine 穿过哪些 active boundaries？
9. synthetic aborted result 解决什么 replay 问题？
10. Stop 与 external rollback 为什么都不能升级成 Success？

## 19. 最短结论

回到开场：一个 Host input 产生一个 Turn、两个 Steps 和两次 model requests，并不是异常绕路。第一次 response 留下 Tool debt，Tool result 被有序提交，下一 Step 再让模型消费这份 durable evidence，最后才形成 Turn terminal。

No-tool、single-tool、multi-tool 与 cancellation 四条 Trace 分别闭合了自然结束、跨 Step 回送、有界并发与 cooperative abort；它们共同说明，Loop 的正确性依赖边界和 receipt，而不是依赖一个 `done`。

最后压成一句：

> 不要用“一次模型回复”描述 Agent Run；用 durable Turn、可平衡的 Steps、model-order Tool receipts 与 typed termination，才能解释它怎样继续、为什么停止，以及停止到底证明了什么。

> **上一篇**：[System Prompt Assembly 与 PromptContext]({{< relref "ai-empowerment/agent-engineering-32-dsh-system-prompt-assembly-prompt-context.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

> **下一篇**：Session continuation、Replay 与 Fork（计划中，发布后再补链接）。
