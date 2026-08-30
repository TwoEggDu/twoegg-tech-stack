# Article 35 Repository Map — DSH Tool Registry and Execution Pipeline

## 1. Identity and safety boundary

- **Fixture identity**: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`, Git `cd5ef8148` (checked 2026-08-30). This map is static-source evidence only.
- **Safety source**: `SAFETY.md#Experimental status` and `#Sandbox limitations`. DSH can run model-generated code/commands, plugins, network/process/file operations; its sandbox is not a security guarantee. No fixture command, model, test, or plugin was run for this investigation.
- **Repository rule**: fixture `AGENTS.md#Registrations are effects` requires contributions to use `ctx.effect()` / `ctx.on()` and `register()` to return a disposer. That explains lifecycle ownership, but it is not proof of an active deployment.
- **Evidence limitation**: source presence and unit-test intent do **not** prove profile composition, provider response, host policy, storage backend, or UI runtime behavior. Each runtime claim needs a separately recorded execution.

## 2. Registry / model-discovery map

| Concern | Source: symbol / stable anchor | Caller → callee | Observation | Counterevidence / limitation |
|---|---|---|---|---|
| Registry owner | `packages/core/tools/src/index.ts`: `ToolRuntime` (class, lines 788-838) | composition → `new ToolRuntime` | `ToolRuntime` is service `tools`; constructor injects `systemPrompt` and contributes `wireSchemas`. | It contains no built-in product tools; composing a service is required before any tool exists. |
| Registration and duplicate handling | same: `ToolRuntime.register` (1030-1061), `ScopedLayers` use | tool plugin → `ctx.tools.register` → layer `tools.insert` | A tool needs `output { schema, render }`; unsupported output schema, non-positive `timeoutMs`, duplicate layer name, and reserved `run_code` reject. Return is the unregister disposer. | Raw `ToolDefinition` may supply its own `execute`; registry does not infer a parameter validator for it. |
| Scope / dedup / restrictions | same: `view` (1129-1191), `get` (1194-1205), `restrict` (1063-1096) | `schemas/get/execute` → `view(scope)` | Inherited entries shadow by nearest scope; every scope restriction intersects for inherited tools; local registrations remain visible; `run_code` is appended outside restrictions in PTC modes. | This is scoped in-process visibility, not OS authorization. A restriction is not an execution-time audit record. |
| Model schema and provider boundary | same: `wireSchemas` (976-1000), `schemas` (1227-1265); `packages/core/agent-loop/src/agent.ts`: `step` (339-363), `buildRequest` request assembly (533-541) | system-prompt assembly → `wireSchemas` → request header → LLM stream | Native model wire schema is only `name`, `description`, `parameters`; the request uses persisted `header.tools`. PTC exposes only `run_code`; `both` exposes both. | The source does not establish which LLM provider/model received a request, nor whether it obeyed schema. |
| Tool schema versus canonical output | `packages/core/tools/src/schema.ts`: `defineTool` (545-600); `packages/core/tools/src/index.ts`: `ToolOutputDefinition` (211-219) | first-party tool → `defineTool` wrapper → `ToolDefinition.execute` | `defineTool` validates model args before body and declares output schema + pure render/meta projections. | `validateArgs` is a helper; raw registration can bypass it, so “all tools validate input” is **NOT_PROVABLE** from registry alone. |

## 3. Model arguments versus host-only metadata

| Plane | Exact source / anchor | Observation | Boundary |
|---|---|---|---|
| Model-visible declaration | `core/tools/src/index.ts`: `schemaOf` (1254-1266), `schemas` (1233-1235) | Only name, description and parameters are projected. | No function, output schema, timeout, guard, scope, agent, signal, or presentation callback is sent in native tool schema. |
| Model-produced call payload | `core/agent-loop/src/tool-calls.ts`: `parseArguments` (103-110), `executeToolCalls` (59-100) | Tool-call block provides `id`, `name`, string `arguments`; JSON parses, malformed JSON survives as raw text. | Parsing is not validation; type validation is tool-owned through `defineTool` or an equivalent raw-tool body. |
| Host execution metadata | `core/tools/src/index.ts`: `ToolExecutionInput` (315-339), `ToolExecution` (373-385) | Host supplies/owns `callId`, root/parent relation, agent, abort signal and registry token; arguments become lossless JSON snapshot then deep-frozen (`createExecution`, 1363-1449). | `agent` and `signal` are capability/context references, never portable model arguments. |
| Host-only execution policy | `ToolDefinition.timeoutMs` / `isConcurrencySafe` (249-269) | Deadline and sibling-overlap metadata are explicit host behavior; schema projection excludes both. | Declaring a deadline does nothing unless timeout plugin is composed. |

## 4. Policy vocabulary: allow, deny, ask, multiple policy layers

| Policy path | Source: symbol / anchor | Caller → callee | Observation | Counterevidence / limitation |
|---|---|---|---|---|
| Pre-policy | `core/tools/src/index.ts`: event declaration `tools/pre-execute` (149-160), `prepareExecution` (1462-1505) | scheduler/`execute` → waterfall → `serviceAsk` / guards | Default is `{kind:'allow'}`. A pre listener may allow, deny, or ask; denial produces an error result before dispatch. | A waterfall listener that does not call `next()` intentionally short-circuits; source cannot tell deployed ordering. |
| Ask / user policy | `core/tools/src/index.ts`: `serviceAsk` (1677-1728); `interaction/user-approval/src/index.ts`: `ApprovalService.request/decide` (222-309) | pre `ask` → approval service → `approval/request` answerers | Only `allowed-once` permits dispatch. `rejected`, `cancelled`, unavailable service/answerer, agent-less call, or rogue response deny/fail closed; asked/decided events are appended in an open turn. | Approval service is optional; no source proof that a UI answerer is composed or a user responded. |
| Permanent guard | `core/tools/src/index.ts`: `guard` / `guardReason` (1099-1127) | allowed pre decision → global then scope guards | Guards run after extensible pre-policy; any non-empty reason denies and no guard can force-allow. | Guard is synchronous and is not a replacement for a sandbox/provider policy. |
| Post-policy | same: `postExecute` (1730-1780) | dispatched normalized result → `tools/post-execute` waterfall | `accept` can replace **content or canonical value** (not both); `block` makes a valueless error with feedback; both can attach next-step contexts. | A source hook can block model-facing result after side effects already occurred; it cannot undo them. |
| Codex bridge hook | `hooks/hooks-codex/src/index.ts`: PreToolUse / PostToolUse handlers (224-253) | bridge → pre/post waterfalls | Codex PreToolUse only maps deny; PostToolUse maps deny to `block` or appends context. Hook invocations/results are auditable when an agent/turn is available. | Config load can fail and register no hooks (81-97); bridge explicitly has no pre-tool approve/rewrite behavior. |

## 5. Timeout, cancellation, concurrency, and errors

| Concern | Source: symbol / anchor | Observation | Limitation |
|---|---|---|---|
| Cooperative timeout | `guard/timeout-policy/src/index.ts`: `apply` (50-80), `TOOL_TIMEOUT` (25) | Optional `tools/execute` wrapper reads `get(...).timeoutMs`, swaps in deadline signal, awaits quiescence, then replaces its own expiry with error code `TOOL_TIMEOUT`. | It cannot hard-kill uncooperative same-process code; no composition means no timeout enforcement. |
| Cancellation | `core/tools/src/index.ts`: `dispatchToolBody` (1526-1558), cancellation helpers (1508-1524) | Registry fuses caller and wrapper signals; pre-dispatch cancellation is `ABORTED_BEFORE_DISPATCH`; a started successful result becoming cancelled is `ABORTED`, after body settles. | A tool can still hang if it ignores the signal; static source does not prove every tool is cooperative. |
| Concurrency | `core/tools/src/index.ts`: `executionMode` (1268-1284); `core/agent-loop/src/tool-calls.ts`: `runGroup` (121-245) | Only literal `true` enables parallel. Exclusive calls form barriers; pool size is `agentLoop.maxParallelToolCalls`; dispatch overlaps but pre-policy, finalization, persistence and contexts commit in model order. | Classifier exceptions fail closed to exclusive. This proves design, not observed parallelism. |
| Error normalization | `core/tools/src/index.ts`: `dispatchScheduledExecution` (1561-1597), `finishScheduledExecution` (1622-1645), `createSuccessResult` (1791-1821) | Throws and invalid/lossy outputs become `isError`; success snapshots, validates output schema, renders model content, and optionally projects UI meta. Result observers cannot mutate or reject the finalized outcome. | A tool-specific error code/content may be implementation-owned; this map does not enumerate every product tool. |

## 6. Result, model, UI, and persistence are separate products

| Product | Source: symbol / anchor | Caller → callee | Observation | Counterevidence / limitation |
|---|---|---|---|---|
| Canonical result | `core/tools/src/index.ts`: `createSuccessResult` / `materializeFinalResult` (1791-1860) | body / wrapper → canonical value → `render` + optional `presentationMeta` | `value` is output-schema-validated; `content` is model-facing; top-level meta is a replayable UI payload. | Nested composite dispatch suppresses top-level meta. UI presenter functions are not the canonical result. |
| Durable session record | `core/agent-loop/src/tool-calls.ts`: `appendToolCall` / `appendToolResult` (248-288) | scheduler finalization → session append | Persists model call raw arguments, then user-role tool-result message, structured error info and meta; result cites call event sequence. | It persists no raw canonical `value`; reconstruction is from model-facing result/meta, not arbitrary tool internals. |
| Model feedback | same `appendToolResult` (276-287); `core/agent-loop/src/agent.ts` assistant-call path (408-434) | session event → derived messages → next LLM request | `createToolResultMessage` carries final content/isError; result contexts are separately accepted for next-step inbox. | Actual subsequent prompt/provider transmission requires runtime evidence. |
| Client UI projection | `client/ui-chat/src/client/conversation-nodes/tool.ts`: `rootCall/rootResult` (39-68), `toolDefinition` (230-264); `register.ts`: `registerConversationNodes` (21-35) | session `tool/call` / append `tool/result` → conversation node → chat view | UI pairs events by callId, uses durable content/error/meta, and synthesizes interruption only when a lifecycle is closed without result. | This is one client UI package, not proof every host/client presents it, or that `presentCall/presentResult` was used. |

## 7. Large-result behavior: two distinct mechanisms

- **Spill exists (optional)** — `packages/spill/spill-policy/src/index.ts`, `apply` (110-231): for an oversized all-text top-level result, a composed `tools/post-execute` policy saves full text to session-owned `spillStore`, replaces model content with head/tail preview + locator, and preserves a successful call if storage is absent/fails. It also bounds PTC dispatch-log copies. This proves an implementation exists, not that a spill backend, config cap, artifact, or retrieval UI was live.
- **Summary/pruning exists (separate, post-persistence)** — `packages/compaction/compaction-tool-result-pruner/src/index.ts`, `ToolResultPruner.pruneSession` (124-184): current-surface `tool/result` text can be head/middle/tail pruned, with `compaction/prune` and a replacement event. It is replay-safe, model-free compaction, not execution-time spill and not a semantic LLM summary.
- **NOT_PROVABLE**: a universal “large result always spills/summarizes” guarantee. Both facilities are conditional plugins with configuration and backend prerequisites.

## 8. Targeted negative-test designs — planned only, not executed

All commands below are exact focused candidates from fixture root and intentionally **not run** in this source-investigation gate.

| Design | Target source / existing test anchor | Required negative assertion | Command to run later |
|---|---|---|---|
| Invalid/hostile model arguments | `core/tools/src/schema.ts`: `defineTool` (545-588); `core/tools/tests/tools.spec.ts`: `ToolRuntime` invalid-output/argument cases | Malformed JSON remains raw through `parseArguments`; a typed `defineTool` call returns structured `INVALID_ARGS` and does not invoke body. | `pnpm exec vitest run packages/core/tools/tests/tools.spec.ts -t "invalid arguments"` |
| PTC direct-call bypass | `core/tools/src/index.ts`: `collapses` (1307-1325); `core/tools/tests/ptc.spec.ts`: `denies a model-direct native-tool call under PTC mode as UNKNOWN_TOOL` | Model-direct native tool is denied before pre-policy; nested call with parent token remains eligible. | `pnpm exec vitest run packages/core/tools/tests/ptc.spec.ts -t "denies a model-direct native-tool call under PTC mode as UNKNOWN_TOOL"` |
| Ask without grant | `core/tools/src/index.ts`: `serviceAsk` (1688-1727); `interaction/user-approval/src/index.ts`: `decide` (269-309); `core/tools/tests/tools.spec.ts`: `an ask decision degrades to deny when no approval seam is mounted` | Missing service/answerer, `never`, cancellation, or rogue result cannot execute body; only `allowed-once` is a grant. | `pnpm exec vitest run packages/core/tools/tests/tools.spec.ts -t "an ask decision degrades to deny when no approval seam is mounted"` |
| Deadline/cancel does not abandon work | `guard/timeout-policy/src/index.ts`: `apply` (55-80); `core/tools/tests/tools.spec.ts`: `waits for an uncooperative started body before returning ABORTED`; `guard/timeout-policy/tests/timeout-policy.spec.ts` | Expiry emits `TOOL_TIMEOUT`; caller cancellation waits for a started body to settle and uses abort taxonomy. | `pnpm exec vitest run packages/guard/timeout-policy/tests/timeout-policy.spec.ts packages/core/tools/tests/tools.spec.ts -t "uncooperative started body|TOOL_TIMEOUT"` |
| Spill failure / durable ordered result | `spill/spill-policy/src/index.ts`: `spillReplacement` (130-187); `agent-loop/src/tool-calls.ts`: `appendToolResult` (268-288); `spill-policy/tests/spill-policy.spec.ts`, `agent-loop/tests/tool-calls.spec.ts` | Missing/failing spill store keeps original successful result; parallel dispatch still persists paired results in model order with call sequence provenance. | `pnpm exec vitest run packages/spill/spill-policy/tests/spill-policy.spec.ts packages/core/agent-loop/tests/tool-calls.spec.ts -t "saveText fails|model order"` |

## 9. Investigation exit

`SOURCE_MAP PASS` — the map establishes static source call ownership and explicit unknowns for fixture `cd5ef8148`. Next gate: `EXPERIMENT_EXECUTE` (profile composition, real provider/tool execution, persisted artifacts, and UI/runtime observations must be captured separately).
