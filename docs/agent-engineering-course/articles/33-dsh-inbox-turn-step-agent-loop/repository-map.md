# Article 33 Repository Map

Status: `SOURCE_MAP PASS`

## Pinned source boundary

- Repository: `deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Fixture check: `HEAD` equals the pinned commit and `git status --porcelain=v1 --untracked-files=all` was empty before investigation.
- Evidence level: source and owner-test routing only. The Source Investigator did not execute the four required traces and does not upgrade them to runtime observations.

## Vocabulary reconciliation

| Course term | Pinned-source object | Boundary |
|---|---|---|
| Inbox | `Inbox`, two durable pending lists: `next-turn` and `next-step` | Runtime projection over `agent/inbox/spliced`; not a Chat UI component. |
| Turn | `turn/start` through `turn/end` | May contain zero or more Steps. |
| Step | `step/start` through `step/end` | One model request plus the tool calls produced by that response. |
| Tool Batch | One assistant message's ordered `ToolCallBlock[]` handled by `executeToolCalls` | Scheduling unit inside one Step; not a child Agent or Multi-Agent abstraction. |
| Stop | A `TurnEndReason` or a completed tool result carrying `concludesTurn` | Includes completed, blocked, max-tokens, aborted and error; therefore not synonymous with success. |

## Host ingress and Inbox ownership

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/bundle/headless/src/index.ts` | `run`, 162-204 | Headless creates an Agent, calls `agent.followup(createUserMessage(...))`, awaits idle, then maps only `turn/end.reason.kind === "completed"` to exit 0. This is direct counter-evidence to Inbox being a Chat UI. |
| `packages/api/session-controller/src/commands.ts` | `SessionCommands.prompt`, 283-335 | Browser-facing RPC validates route/attachments, creates a durable user message, then calls `agent.steer` or `agent.followup`. The UI is upstream of this Host boundary. |
| same | `SessionCommands.cancel`, 424-442 | Browser cancellation resolves the live Agent and calls `cancel({ kind: "user" }, { keepInbox: true })`. |
| `packages/core/agent-loop/src/agent.ts` | `send` / `followup` / `steer` / `inject`, 120-147 | `followup` targets `next-turn` and wakes; `steer` targets `next-step` and wakes; `inject` targets `next-step` without waking. `cancel` optionally clears both queues and aborts active work. |
| `packages/core/agent/src/inbox.ts` | `Inbox`, 24-78 | Replays durable splices, exposes two ordered lists, and claims all `next-step` input plus at most one `next-turn` message for a turn boundary. |
| same | `mutate`, 157-219 | Validates unique message identities, appends `agent/inbox/spliced` before mutating the live projection, then emits inserted/discarded/claimed notifications. |
| `packages/core/agent/src/types.ts` | `InboxTarget` / `agent/inbox/spliced`, 28-45 | The two queue names and their durable mutation event are public session vocabulary. |

## Agent construction and driver ownership

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/agent/src/index.ts` | `AgentRegistry.create`, 387-406 | Host consumers create through the registered `AgentFactory`; the registry is not the loop implementation. |
| `packages/core/agent-loop/src/index.ts` | `AgentLoop.prepare`, 460-579 | Constructs `ReactLoopAgent`, publishes Session then Agent, emits `agent/session-start`, and owns reverse-order cancellation/quiescence/disposal. |
| same | `createAgent` / `setupAndPublish`, 607-645 | Async Host creation prepares the Session, runs unpublished setup, commits it, then publishes the live Agent. |
| `packages/core/agent-loop/src/agent.ts` | `ReactLoopAgent` constructor, 68-104 | Owns the Inbox, phase machine, scoped dispatch, Session, and runtime-context projection. |
| same | `wakeDriver` / `kick`, 171-230 | Waking input synchronously reserves a fresh `AbortController`; one driver repeatedly calls `turn()` until no queued work remains. |

## Turn and Step lifecycle

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/session/src/types.ts` | `TurnEndReasonMap`, 152-177 | Terminal variants are `completed`, `aborted`, `blocked`, `error`, `max-tokens`, and persistence-only `interrupted`. |
| same | `turn/start`, `turn/end`, `step/start`, `step/end`, 221-241 | A Turn opens before input claim and may close without a Step; a Step is explicitly one model call plus requested tool executions. |
| `packages/core/agent-loop/src/agent.ts` | `preStep`, 232-249 | Claims Inbox input, assembles prompt/tool state, projects dynamic context, and runs `agent/pre-step`; reject prevents Step creation. |
| same | `turn`, 252-337 | Appends `turn/start`, loops over proposed Steps, appends admitted `user/message`, always balances opened Steps with `step/end`, offers `agent/turn-stopping`, then appends one structured `turn/end`. |
| same | `turn`, 278-306 | Empty first admission closes a zero-Step completed Turn. A rejected admission closes blocked. Fresh next-step input prevents closure. |

## Step assembly, model call, parse, and events

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/agent-loop/src/agent.ts` | `step`, 339-435 | Flattens system assembly, derives Session history, builds a frozen request, streams the prepared/current adapter, appends each `assistant/chunk`, assembles one `assistant/message`, then branches on finish/tool calls. |
| same | `buildRequest`, 442-541 | Runs `agent/request`, resolves `LlmRuntime.prepareCall`, logs request header/context changes, and places the same Turn signal on `GenerateOptions.signal`. |
| `packages/llm/llm/src/index.ts` | `LlmRuntime.prepareCall`, 881-934 | Binds one adapter registration and resolved exact-model config to a one-shot stream call. |
| same | `adapterStream` / `streamWithRegistration`, 958-1063 | Adapter selection/dispatch/iteration failures become terminal failure chunks; `llm/stream` remains the middleware waterfall. |
| `packages/llm/llm/src/assembler.ts` | `BlockAssembler.push`, 37-95 | Parses ordered raw chunks into text/reasoning/tool-call partials, usage, finish reason, and replay state. |
| same | `assembled` / `interruptedBlocks`, 129-178 | Drops unsafe tool calls on max-token truncation; cancellation preserves only safely visible text/reasoning prefixes. |
| `packages/core/agent-loop/src/tool-calls.ts` | `parseArguments`, 103-109 | Tool arguments parse as JSON; empty becomes `{}` and invalid JSON remains text for the Tool pipeline to reject/handle. |

## Tool Batch concurrency, order, and aggregation

| File | Symbol / lines | Source fact |
|---|---|---|
| `packages/core/agent-loop/src/tool-calls.ts` | `executeToolCalls`, 59-101 | Converts model-order blocks to executions, dynamically classifies each next group, treats exclusive calls as barriers, and aggregates whether any successful result concluded the Turn. |
| same | `runGroup`, 121-246 | Ordered `prepare` stages feed a bounded rolling pool; only dispatch bodies overlap. Later calls are reclassified before start. |
| same | `commitReady`, 145-159 | Settled slots commit `tool/result`, deferred contexts, and `concludesTurn` strictly in model order even if bodies settle out of order. |
| same | abort branch, 198-245 | Abort stops replenishment, drains started calls, commits them in order, and synthesizes balanced aborted results for unstarted calls. |
| `packages/core/tools/src/index.ts` | `ToolExecutionMode`, 341-347 | Only calls explicitly classified `parallel` may overlap; all others are exclusive. |
| same | staged Tool pipeline, 1458-1644 | Runs pre-policy/ask/guards, dispatches with fused cancellation, runs post-policy, materializes and notifies one canonical result. |

## Continue / Stop decision matrix

| Input/outcome | Owner | Pinned behavior |
|---|---|---|
| model response has no tool calls | `ReactLoopAgent.step`, 426-429 | returns `completed`; Turn may still receive steering at `agent/turn-stopping`. |
| model finish is max-tokens | same, 426 | returns `max-tokens`; tool calls from the truncated response are not dispatched. |
| tool calls, no concluding success | same, 428-434 | returns `null`; the Turn performs another Step so the model can consume results. |
| successful tool calls `concludeTurn()` | `ToolRunContext.concludeTurn`, `tools` 405-421; scheduler 145-159 | result carries `concludesTurn`; Step returns completed, but already queued next-step input still drains. |
| `agent/pre-step` reject | `turn`, 273-276 | Turn ends `blocked` without opening that Step. |
| terminal model/extension failure | `step` 388-405; `turn` 309-329 | unhandled request failure or thrown extension error ends `error`; retry is owned only by `agent/request-error`. |
| explicit cancellation | `cancel`, 141-147; `turn`, 309-326 | shared signal aborts; Turn ends `aborted` with the first typed cause. |

## Policy, Budget, Error, and Cancellation boundaries

- Tool policy is not a Turn-success oracle. `tools/pre-execute` `deny`/failed `ask` becomes an `isError` tool result (`packages/core/tools/src/index.ts:1458-1505`), which normally returns to the model in the next Step.
- Tool timeout is cooperative and result-scoped. `packages/guard/timeout-policy/src/index.ts:55-80` derives a deadline from the caller signal and maps its own expiry to `TOOL_TIMEOUT`; it does not define a Turn budget.
- The pinned AgentLoop has **no built-in Turn/Step/cost budget**. `AgentOptions.maxTokens` is a per-request output ceiling (`packages/core/agent/src/runtime-types.ts:24-34`); max-token finish is a non-success Turn reason. Repository search found no production listener enforcing a generic Turn budget.
- Model failures are normalized at the LLM boundary, offered once to `agent/request-error`, and retried only when a listener returns `{ kind: "retry" }`; otherwise they become a structured Turn error.
- Cancellation uses one active phase `AbortController`. Its signal reaches prompt assembly/pre-step, request middleware, adapter preparation/stream, and each Tool execution. Tool wrappers may derive a signal, but the Tool registry fuses the original caller signal back in (`packages/core/tools/src/index.ts:1526-1558`).

## Required Trace owner-test anchors

These are reproducible fixture candidates for the Lab Engineer, not results from this Source Investigator.

| Required trace | Owner fixture | Assertions available |
|---|---|---|
| no-tool | `packages/core/agent-loop/tests/loop.spec.ts:190-224` | ordered Turn/Step boundaries, Inbox receipt, user/assistant messages, derived history |
| single-tool | same, 226-261 | two model requests, durable call/result, result visible in second request |
| multi-tool | `packages/core/agent-loop/tests/tool-calls.spec.ts:100-115,221-260` | parallel-safe overlap plus model-order result/history despite out-of-order settlement |
| cancellation | `packages/core/agent-loop/tests/cancel.spec.ts:384-458,482-513` | aborted Turn, tail dropping, pre-dispatch synthetic result, interrupted visible prefix and later replay |

The tests use `packages/core/agent-loop/tests/mock-adapter.ts:5-132`; therefore a fresh passing run can prove fixture-scoped loop behavior, not a real DeepSeek Provider/network run.

## Counter-evidence and absences

- Inbox has no UI rendering responsibility; Headless and Browser hosts both feed it.
- A Turn can have zero, one, or multiple Steps; `agent/pre-step` rejection/empty input proves zero-Step is valid.
- A Tool Batch is a scheduler over calls from one assistant message. No child Agent is created by `executeToolCalls`.
- Stop is not success: `blocked`, `max-tokens`, `aborted`, and `error` are durable stop reasons.
- Parallel dispatch does not imply parallel result visibility: pre/post policy and result/context commits remain model-ordered.
- Policy denial, Tool error, and Tool timeout normally become model-visible error results and do not automatically end the Turn.
- Cancellation is cooperative: the registry drains started Tool promises; it does not hard-kill same-process code or roll back external effects.
- No generic loop budget/cost limiter was found in the pinned production path. A course design must not relabel `maxTokens` or Tool `timeoutMs` as one.

## Source verdict

`PASS` for `SOURCE_MAP`: critical production symbols close both Host ingress paths through durable Inbox projection, Turn/Step construction, request assembly, adapter streaming, chunk parsing, Tool Batch scheduling, continuation/stop decisions, and cancellation propagation. All four required Trace fixtures are identified, but runtime status remains pending the Lab Engineer.
