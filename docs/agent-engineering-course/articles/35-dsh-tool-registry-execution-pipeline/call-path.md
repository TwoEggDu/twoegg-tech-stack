# Article 35 Call Path — DSH Tool Registry and Execution Pipeline

## Evidence header

- **Fixture**: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` at `cd5ef8148`.
- **Safety**: fixture `SAFETY.md` says DSH may execute model-generated commands and load plugins; this document uses read-only static source evidence. `AGENTS.md#Registrations are effects` requires lifecycle-owned registrations/disposers.
- **Reading rule**: arrows below are source call paths, not a claim that a fixture profile, provider, tool, spill backend, session store, or client UI was actually running.

## 1. End-to-end native call path

```text
ToolDefinition registration
  plugin -> ctx.tools.register(definition)
  -> ToolRuntime.layers.effect / layer.tools.insert

Model discovery and provider request
  systemPrompt.tools -> ToolRuntime.wireSchemas(scope)
  -> schemaOf(name, description, parameters only)
  -> Agent.step -> buildRequest -> request.header.tools -> llm.stream(request)

Model response to durable tool completion
  assistant tool-call block
  -> executeToolCalls
  -> parseArguments
  -> ToolRuntime scheduler.prepare
  -> createExecution (JSON snapshot + deep freeze + registry token)
  -> tools/pre-execute waterfall
  -> optional approval ask -> monotonic guards
  -> tools/execute waterfall -> definition.execute
  -> output snapshot + schema validation + render/meta projection
  -> tools/post-execute waterfall -> optional content/value replacement or block
  -> finalizeContent -> materialize -> tools/result observers
  -> append tool/call then tool/result session events
  -> derived user tool-result message for the next model request
  -> client toolDefinition pairs events by callId into a UI card
```

### Source-backed stages

| Stage | File + symbol / stable anchor | Caller → callee | Observation | Counterevidence / limitation |
|---|---|---|---|---|
| Register | `packages/core/tools/src/index.ts`: `ToolRuntime.register` (1036-1060) | plugin → `ctx.tools.register` → scoped layer | Validates mandatory output declaration/output JSON Schema, reserves `run_code`, returns exact disposer. | Registration source does not show which plugin/profile invoked it. |
| Discovery / scope | same: `view` (1151-1191), `wireSchemas` (980-1000), `schemaOf` (1255-1266) | prompt contributor → visible scope map → provider schema | Inherited definitions resolve by scope, restrictions intersect, local definitions shadow; model schema contains only name/description/parameters. | `schemas()` is not provider delivery; `timeoutMs`, callbacks, agent/signal and output schema do not cross this boundary. |
| Provider request | `packages/core/agent-loop/src/agent.ts`: `step` (339-363), `buildRequest` return (533-541) | `step` → `buildRequest` → `llm.stream` | Header tools become request `tools`, alongside messages/system/sessionId/signal. | No runtime trace establishes provider/model or response conformance. |
| Call ingress | `core/agent-loop/src/agent.ts`: tool-call branch (428-434); `core/agent-loop/src/tool-calls.ts`: `executeToolCalls` (59-100) | assistant blocks → scheduler | Agent selects `tool-call` blocks, takes initiating agent/session, parses raw JSON string; empty input maps to `{}`, malformed JSON remains string. | Parser accepts malformed syntax as a raw candidate; it is not semantic argument validation. |
| Canonicalize input | `core/tools/src/index.ts`: `createExecution` (1363-1449) | scheduler prepare → `snapshotJsonValue` → `deepFreeze` | Runtime mints token/root ID, snapshots lossless JSON arguments, retains caller signal and host agent/parent metadata. | A snapshot failure returns an error result; it does not prove raw tools validate otherwise valid JSON. |
| Validate | `core/tools/src/schema.ts`: `defineTool` (545-588) | tool definition wrapper → parameter JSON-Schema validation → tool body | First-party typed tool throws `ToolArgsError(INVALID_ARGS)` before body when violations exist. | Registry-wide validation for every raw `ToolDefinition` is **NOT_PROVABLE**; raw registration owns its own validation. |
| Pre policy | `core/tools/src/index.ts`: `prepareExecution` (1462-1505) | scheduler → `tools/pre-execute` → `serviceAsk` / `guardReason` | Default allow; deny/ask terminate before dispatch; guards are monotonic after pre listeners. | Multiple listener order is composition-dependent. |
| Execute | same: `dispatchScheduledExecution` (1568-1597), `dispatchToolBody` (1531-1558) | `tools/execute` waterfall → resolved definition → `execute(args, exec)` | Around wrappers may replace signal but registry fuses original caller cancellation; body throw becomes result error. | Tool side effects are not rolled back by later policy. |
| Normalize and finalize | same: `createSuccessResult` (1791-1821), `postExecute` (1741-1780), `finishScheduledExecution` (1630-1675) | body/wrapper outcome → output validation/render/meta → post waterfall → finalizer → observer | Success value validates against output schema; render/meta are snapshots; post accept may replace content **or** value, block becomes error; observers cannot mutate/throw into final outcome. | Finalizer or projection can itself fail and is normalized; source does not prove a given tool's projector is total. |
| Persist | `core/agent-loop/src/tool-calls.ts`: `appendToolCall` / `appendToolResult` (262-288) | ordered scheduler commit → `session.append` | Persists raw call argument string, paired user-role result content/isError, optional error info and UI meta; result cites call event seq. | Canonical `value` is not persisted as a generic field. |
| Model / UI projection | `core/agent-loop/src/agent.ts`: `session.deriveMessages` use (353); `client/ui-chat/.../tool.ts`: `toolDefinition` (230-264) | event surface → next request / UI node | Tool result is fed as model conversation material; UI pairs `tool/call` with append `tool/result` by callId and renders durable content/error/meta. | Subsequent model delivery and actual screen rendering need runtime evidence. |

## 2. PTC and host metadata path

```text
PTC scope -> ToolRuntime.wireSchemas
  native: all visible schema
  ptc: only reserved run_code schema
  both: both surfaces

model-direct native name under PTC
  -> ToolRuntime.collapses(..., parent absent)
  -> UNKNOWN_TOOL final result before pre-policy

run_code inner SDK call
  -> ToolExecutionInput.parent = opaque outer token
  -> resolveExecution permits native visible tool
  -> nested result has no top-level presentationMeta
  -> PTC dispatch-log event may have a separately bounded durable copy
```

| Fact | Source / anchor | Observation | Limitation |
|---|---|---|---|
| PTC mode selection | `core/tools/src/index.ts`: constructor/config (788-838), `wireSchemas` (976-1000), `collapses` (1307-1325) | PTC model-direct calls can only name `run_code`; nested calls marked by `parent` bypass collapse. | This is registry routing, not an OS sandbox. |
| Host-only data | same: `ToolExecutionInput` (315-339), `ToolExecution` (373-385) | `callId`, rootCallId, agent, parent token, and signal are host metadata; only `arguments` originate in model call text. | `callId` is correlation, not authentication/authorization. |
| Schema/mode countercheck | `core/tools/tests/tools.spec.ts`: `schemas() excludes timeoutMs — the budget must never reach the model`; `core/tools/tests/ptc.spec.ts`: `denies a model-direct native-tool call under PTC mode as UNKNOWN_TOOL` | Existing targeted tests encode both boundaries. | Tests were deliberately not executed in this gate. |

## 3. Allow / deny / ask and hook paths

```text
tools/pre-execute
  allow -> guards -> execute
  deny -> error result -> post-execute -> finalizer -> persist
  ask -> ApprovalService.request
      allowed-once -> guards -> execute
      rejected/cancelled/unavailable/no agent/no service -> deny result

tools/post-execute
  accept(content OR canonical value) -> finalizer -> persist
  block(feedback) -> valueless error -> finalizer -> persist
```

| Branch | File + symbol / anchor | Caller → callee | Observation | Counterevidence / limitation |
|---|---|---|---|---|
| Ask policy | `core/tools/src/index.ts`: `serviceAsk` (1688-1728); `interaction/user-approval/src/index.ts`: `request/decide` (222-309) | pre `ask` → approval service → scoped answerer waterfall | Only `allowed-once` runs; `never` deterministically rejects before answerers; missing/throwing/rogue answerer is unavailable. Asked/decided audit events occur in an open turn. | No proof a human answerer is composed or that a user decision happened. |
| Hooks | `hooks/hooks-codex/src/index.ts`: PreToolUse/PostToolUse (224-253) | external hook bridge → same pre/post extension points | Codex bridge maps pre deny to deny and post deny to block; it may add context. | Config read failure yields no hook registrations (81-97); no hook-level allow/ask/rewrite path exists. |
| Restriction vs guard | `core/tools/src/index.ts`: `restrict` (1063-1096), `guard` (1099-1127) | scoped visibility → execution guard | Restriction hides inherited capability from view; guard rejects an execution after pre policy. | Neither asserts filesystem/network permission. |

## 4. Deadline, cancellation and scheduler path

```text
executeToolCalls
  -> mode = tools.executionMode(call)
  -> exclusive barrier OR bounded parallel pool
  -> scheduler.prepare in model order
  -> scheduler.dispatch may overlap
  -> scheduler.finalize/finish and append result in model order

abort before body -> ABORTED_BEFORE_DISPATCH
abort after start -> wait body quiescence -> ABORTED (unless body owns error)
timeout-policy composed -> deadline signal -> wait delegated body -> TOOL_TIMEOUT
```

| Concern | Source / anchor | Observation | Counterevidence / limitation |
|---|---|---|---|
| Scheduler | `core/agent-loop/src/tool-calls.ts`: `runGroup` (121-245) | Only exact `true` from `isConcurrencySafe` permits pool overlap. A later exclusive call ends current group; completed results and contexts commit by model order. | Runtime scheduler failure drains started work and does not invent recovery results. |
| Cancellation | `core/tools/src/index.ts`: `dispatchToolBody` (1526-1558); `tool-calls.ts`: skipped pairs (248-258) | Caller/wrapper signal fusion protects cancellation; unstarted cancellation produces durable synthetic error pairs so replay is valid. | JS source cannot force an uncooperative tool to terminate. |
| Timeout plugin | `guard/timeout-policy/src/index.ts`: `apply` (55-80) | Optional wrapper honors per-definition budget, waits for settled delegate and substitutes `TOOL_TIMEOUT` only if its timer won. | Omitted plugin or missing `timeoutMs` leaves no deadline. |

## 5. Oversized output / compaction paths

| Path | File + symbol / anchor | Observation | Limitation |
|---|---|---|---|
| Execution-time spill | `spill/spill-policy/src/index.ts`: `apply` (110-231), `spillReplacement` (130-187) | Optional post-policy bounds oversized all-text model content by storing full text, then emits preview + locator; storage failure preserves original successful inline result. | Requires configured cap, agent-owned session and backend; no universal spill guarantee. |
| PTC durable-copy spill | same: `tools/ptc-dispatch-log` handler (211-231) | Bounds code-dispatch log copy without changing program value. | Does not prove a retrieval UI or artifact retention policy. |
| Post-persistence prune | `compaction/compaction-tool-result-pruner/src/index.ts`: `ToolResultPruner.pruneSession` (124-184) | Later current-surface rewrite emits `compaction/prune` and replacement `tool/result`; it is deterministic truncation, not semantic summary. | Composition/run timing is **NOT_PROVABLE** without an event log. |

## 6. Five targeted negative experiments — designs only

1. **Bad input never reaches typed body** — modify/extend `packages/core/tools/tests/tools.spec.ts` around `ToolRuntime`; issue invalid JSON/string argument via `executeToolCalls` and assert typed `defineTool` body has zero calls plus `INVALID_ARGS`. Candidate command: `pnpm exec vitest run packages/core/tools/tests/tools.spec.ts -t "invalid arguments"`.
2. **PTC direct bypass fails before policy** — target `packages/core/tools/tests/ptc.spec.ts`, existing anchor `denies a model-direct native-tool call under PTC mode as UNKNOWN_TOOL`; assert pre listener and body counts remain zero. Candidate: `pnpm exec vitest run packages/core/tools/tests/ptc.spec.ts -t "denies a model-direct native-tool call under PTC mode as UNKNOWN_TOOL"`.
3. **Ask does not fail open** — target `packages/core/tools/tests/tools.spec.ts`, existing anchor `an ask decision degrades to deny when no approval seam is mounted`; add `never` and unavailable answerer assertions, body count zero, audit pairing where service exists. Candidate: `pnpm exec vitest run packages/core/tools/tests/tools.spec.ts -t "ask decision degrades to deny"`.
4. **Deadline/cancel drains, not abandons** — target `packages/guard/timeout-policy/tests/timeout-policy.spec.ts` and `packages/core/tools/tests/tools.spec.ts`, anchor `waits for an uncooperative started body before returning ABORTED`; assert signal arrives, resolution waits for body settlement, result code matches timeout/cancel path. Candidate: `pnpm exec vitest run packages/guard/timeout-policy/tests/timeout-policy.spec.ts packages/core/tools/tests/tools.spec.ts -t "TOOL_TIMEOUT|uncooperative started body"`.
5. **Spill failure preserves result and order** — target `packages/spill/spill-policy/tests/spill-policy.spec.ts` plus `packages/core/agent-loop/tests/tool-calls.spec.ts`; make save fail, then assert original success content remains and two parallel calls append result events in call order with linked call seqs. Candidate: `pnpm exec vitest run packages/spill/spill-policy/tests/spill-policy.spec.ts packages/core/agent-loop/tests/tool-calls.spec.ts -t "saveText fails|model order"`.

None of these commands was executed; they are the exact `EXPERIMENT_EXECUTE` handoff, not runtime evidence.

## 7. Investigation exit

`SOURCE_INVESTIGATOR PASS` — static pipeline, policy branches, persistence, UI projection, spill/prune facilities, and their limits are source-mapped for `cd5ef8148`.

Next: `EXPERIMENT_EXECUTE`.
