# Article 33 Call Path

Status: `SOURCE_MAP PASS`

Pinned source: `deepseek-ai/deepseek-harness@cd5ef8148158c3a752a658978873241fdf8e2bbc` (`dsh-v0.1.2-alpha.1`).

## Path A — two Hosts converge on one Inbox

```text
Browser Client
  -> SessionCommands.prompt(request)
     packages/api/session-controller/src/commands.ts:283-335
  -> validate route / attachments
  -> createUserMessage({ source: { kind: "user", rpcId, ... } })
  -> request.mode === "steer" ? agent.steer(message) : agent.followup(message)

Headless run(task)
  packages/bundle/headless/src/index.ts:162-204
  -> agents.create(...)
  -> agent.followup(createUserMessage(task))
  -> agent.whenIdle()
  -> summarize durable events
  -> exit(completed ? 0 : 1)
```

Both paths enter:

```text
ReactLoopAgent.followup
  packages/core/agent-loop/src/agent.ts:129-131
  -> send(message, "next-turn", true)
  -> Inbox.splice("next-turn", ...)
  -> Session.append("agent/inbox/spliced", ...)
  -> wakeDriver()
```

Thus Inbox is a Host-neutral runtime queue, not the Browser Chat UI.

## Path B — durable Inbox projection becomes a Turn

```text
ReactLoopAgent.wakeDriver
  packages/core/agent-loop/src/agent.ts:179-200
  -> reserve Phase.running { abort: new AbortController(), turn, step: 0 }
  -> agents.withInitiator(agent, () => kick())

kick
  packages/core/agent-loop/src/agent.ts:217-230
  -> while (await turn()) {}

turn
  packages/core/agent-loop/src/agent.ts:252-337
  -> Session.append("turn/start", { turn })
  -> preStep("next-turn", { turn, step: 1 })

preStep
  packages/core/agent-loop/src/agent.ts:232-249
  -> Inbox.claim("next-turn", turn)
     packages/core/agent/src/inbox.ts:63-78
     -> remove every next-step item
     -> remove at most one next-turn item
     -> durable deletion splice(s)
     -> live agent/inbox/claimed notifications
```

The Turn owns the claim before any Step exists. Reject or an empty first admission closes this Turn without `step/start`.

## Path C — one admitted proposal becomes one Step

```text
preStep
  -> systemPrompt.assemble(assembleContextFor(agent, signal))
  -> renderContextSections / RuntimeContextProjection.project
  -> agent/pre-step waterfall
  -> reject | enter(messages, startsRequestSeries?)

turn
  -> Session.append("step/start", { turn, step })
  -> append every admitted message as "user/message"
  -> step(effectiveAssembly, startsRequestSeries)
  -> finally Session.append("step/end", { turn, step })
```

Source: `packages/core/agent-loop/src/agent.ts:232-300`.

## Path D — request, model stream, parse, durable assistant anchor

```text
ReactLoopAgent.step
  packages/core/agent-loop/src/agent.ts:339-435
  -> renderPrompt(assembly)
  -> Session.deriveMessages()
  -> buildRequest(..., signal)
     -> agent/request waterfall
     -> LlmRuntime.prepareCall(config, signal)
     -> append request/header and request/context when needed
     -> frozen GenerateOptions { messages, system?, tools?, sessionId, signal }
  -> preparedCall.stream(request) | ctx.llm.stream(request)
  -> for each StreamChunk:
       Session.append("assistant/chunk", ...)
       BlockAssembler.push(chunk)
  -> createAssistantMessage(BlockAssembler.blocks())
  -> Session.append("assistant/message", ..., sourceEventSeqs=chunk seqs)
```

Provider selection and iteration:

```text
LlmRuntime.prepareCall
  packages/llm/llm/src/index.ts:881-934
  -> bind exact adapter registration + resolved config
  -> streamWithRegistration
  -> llm/stream waterfall
  -> adapterStream
     packages/llm/llm/src/index.ts:958-1063
     -> adapter.prepareCall / adapter stream iterator
     -> adapter failures normalize to terminal finish chunks
```

Chunk parsing:

```text
BlockAssembler.push
  packages/llm/llm/src/assembler.ts:37-95
  -> accumulate text/reasoning/tool-call deltas by block index
  -> authoritative block-end
  -> usage + finish reason
  -> blocks() / interruptedBlocks()
```

## Path E — no-tool stop path

```text
assistant/message has no ToolCallBlock
  -> step returns { kind: "completed" }
  -> if Inbox.nextStep is empty:
       dispatch serial agent/turn-stopping
       re-read Inbox.nextStep
  -> no steering => append turn/end(completed)
```

Source: `packages/core/agent-loop/src/agent.ts:426-429,292-306,323-336`.

Owner test: `packages/core/agent-loop/tests/loop.spec.ts:190-224`.

## Path F — single Tool causes a second Step

```text
assistant/message contains tool-call c1
  -> executeToolCalls(ctx, turn, step, [c1], signal, acceptContext)
  -> parseArguments(c1.arguments)
  -> append tool/call
  -> ToolRuntime scheduler.prepare
     -> tools/pre-execute -> ask/guards
  -> scheduler.dispatch
     -> tools/execute -> tool body
  -> scheduler.finalize
     -> tools/post-execute -> canonical result
  -> append tool/result(sourceEventSeqs=[tool/call seq])
  -> no concludesTurn => step returns null
  -> next Step derives prior assistant call + tool result from Session
  -> second model request
  -> text response => completed Turn
```

Sources: `packages/core/agent-loop/src/tool-calls.ts:59-101,121-289`; `packages/core/tools/src/index.ts:1458-1644`; `packages/core/agent-loop/src/agent.ts:428-434`.

Owner test: `packages/core/agent-loop/tests/loop.spec.ts:226-261`.

## Path G — Multi-tool Batch overlaps bodies but commits in model order

```text
ToolCallBlock[] in model order
  -> planned[] (arguments parsed before scheduling)
  -> executionMode(first)
  -> exclusive: group=[first], barrier
  -> parallel: candidate group=remaining calls
     -> fill rolling pool up to maxParallelToolCalls
     -> ordered prepare for each start
     -> dispatch bodies may overlap
     -> Promise.race observes settlement
     -> slots[index] retains each outcome
     -> commitReady advances only contiguous model-order slots
        -> post-execute / finish
        -> tool/result
        -> additionalContexts
        -> concludesTurn aggregate
```

Source: `packages/core/agent-loop/src/tool-calls.ts:70-100,121-245`.

Owner tests:

- overlap: `packages/core/agent-loop/tests/tool-calls.spec.ts:100-115`
- exclusive barrier: same, `117-142`
- model-order result/history after out-of-order settlement: same, `220-260`

This Batch is one Step's call scheduler. It neither creates nor coordinates Agents.

## Path H — Continue and Stop authorities

```text
finish(max-tokens)
  -> Step returns max-tokens; no tool dispatch

no tool calls
  -> Step returns completed

tool calls + no concluding success
  -> Step returns null -> another model Step

successful ToolRunContext.concludeTurn()
  -> ToolExecutionSuccess.concludesTurn=true
  -> scheduler aggregate concluded=true
  -> Step returns completed
  -> already queued next-step input still drains before Turn closes

agent/turn-stopping listener calls agent.steer(...)
  -> Inbox gains next-step work
  -> another Step runs

agent/pre-step reject
  -> Turn ends blocked, no proposed Step
```

Sources: `packages/core/agent-loop/src/agent.ts:273-306,426-434`; `packages/core/tools/src/index.ts:405-421,556-581,1814-1821`; `packages/core/agent/src/runtime-types.ts:268-285`.

## Path I — Policy and timeout normally feed the next model Step

```text
tools/pre-execute
  -> allow | deny(reason) | ask(reason?)
  -> ask goes through approval service
  -> deny/non-grant becomes canonical isError ToolExecutionResult
  -> tool/result is committed
  -> result cannot carry concludesTurn
  -> AgentLoop continues so the model can observe the error
```

Source: `packages/core/tools/src/index.ts:583-592,1458-1505,1677-1727`.

```text
tool.timeoutMs present
  -> timeout-policy tools/execute wrapper
  -> deadline(original signal, timeoutMs)
  -> derived signal reaches Tool body
  -> wait for Tool quiescence
  -> own expiry maps to isError TOOL_TIMEOUT result
  -> ordinary Tool-result continuation semantics
```

Source: `packages/guard/timeout-policy/src/index.ts:41-80`.

No production source defines a generic Agent Turn/Step/cost budget in this pinned version. Per-request `maxTokens` and per-Tool `timeoutMs` are separate limits.

## Path J — model error and recovery

```text
adapter select / dispatch / iteration failure
  -> LlmRuntime.adapterStream emits terminal error|aborted finish chunk
  -> BlockAssembler.finish
  -> agent/request-error waterfall
  -> { kind: "retry" } => repeat request attempt inside the same Step
  -> undefined => throw LlmError
  -> turn catch => turn/end({ kind: "error", structured failure })

middleware / consumer / Tool scheduler failure
  -> remains thrown
  -> turn catch => turn/end(error)
```

Sources: `packages/llm/llm/src/index.ts:958-1037`; `packages/core/agent-loop/src/agent.ts:388-405,309-329`.

Owner tests: `packages/core/agent-loop/tests/request-error.spec.ts:30-142`.

## Path K — cancellation signal crosses the whole Loop

```text
Browser SessionCommands.cancel
  packages/api/session-controller/src/commands.ts:424-442
  -> agent.cancel({ kind: "user" }, { keepInbox: true })

or any owner calls ReactLoopAgent.cancel
  packages/core/agent-loop/src/agent.ts:141-147
  -> optional Inbox.clear()
  -> phase.abort.abort(cause)  // first cause wins

same phase.abort.signal reaches:
  -> SystemPrompt assembly + agent/pre-step
  -> agent/request + LlmRuntime.prepareCall
  -> frozen GenerateOptions.signal + adapter stream
  -> executeToolCalls
  -> ToolExecutionInput.signal
  -> ToolRuntime fuses it with any wrapper signal
```

At convergence:

```text
stream cancellation
  -> BlockAssembler.interruptedBlocks()
  -> append assistant/message(interrupted=true) for visible prefix, if any

Tool Batch cancellation
  -> stop replenishment
  -> drain started Tool promises
  -> commit started results in model order
  -> synthesize ABORTED_BEFORE_DISPATCH call/result pairs for unstarted calls

turn catch observes signal.aborted
  -> turn/end({ kind: "aborted", reason: signal.reason })
  -> driver reaches idle
```

Sources: `packages/core/agent-loop/src/agent.ts:339-386,309-336`; `packages/core/agent-loop/src/tool-calls.ts:198-245,248-289`; `packages/core/tools/src/index.ts:1526-1558`.

Owner tests:

- active Turn abort and queued-tail behavior: `packages/core/agent-loop/tests/cancel.spec.ts:384-403`
- cancellation after assistant tool call balances replay without dispatch: same, `405-458`
- visible streamed prefix finalization and later history: same, `482-513`

## Four required Lab routes

| Route | Frozen owner entry | Required fresh observation |
|---|---|---|
| `no-tool` | `loop.spec.ts:190-224` | exact ordered event types, request count, Turn reason |
| `single-tool` | `loop.spec.ts:226-261` | two requests; call/result correlation; second-request result |
| `multi-tool` | `tool-calls.spec.ts:100-115,220-260` | overlap evidence plus model-order result commit |
| `cancellation` | `cancel.spec.ts:384-458,482-513` | signal state, interrupted/synthetic records, aborted reason, quiescence |

All four use `MockAdapter` (`packages/core/agent-loop/tests/mock-adapter.ts:5-132`). A fresh passing Lab run may establish fixture-scoped runtime behavior only; it does not prove real Provider latency, network cancellation, billing, or external-side-effect rollback.

## Boundary summary

- `Inbox != Chat UI`: two Host surfaces share it.
- `Turn != Step`: Turn opens before claim and can have zero or many Steps.
- `Tool Batch != Multi-Agent`: it schedules Tool bodies only.
- `Stop != Success`: every `TurnEndReason` stops, but only `completed` is the Headless success exit.
- `Cancel != Rollback`: cancellation is cooperative and waits for started work; external effects already performed are not reversed by this loop.
