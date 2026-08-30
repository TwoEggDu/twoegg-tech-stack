# Article 33 AgentLoop Four Traces

Status: `PASS / FIXTURE-SCOPED`

## 1. Identity, scope, and safety

- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag / commit: `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Runtime observed: Windows `10.0.19045` x64, Node `v24.18.1`, PowerShell `7.6.4`, workspace-local Vitest `4.1.8`.
- Package-manager note: `package.json` pins `pnpm@11.7.0` and `corepack pnpm --version` returned `11.7.0`; the host-global `pnpm --version` returned `11.19.0`.
- Scope: production `AgentLoop`, `SessionStore`, `SystemPrompt`, `ToolRuntime`, `AgentRegistry`, repo-owned `MockAdapter`, and deterministic in-memory tools only.
- Excluded: real Provider, credentials, network, billing, production data, command/FS tools, process kill, and external side-effect rollback.
- Instrumentation: two read-only `node --import tsx --input-type=module -e` observations instantiated the production services and printed normalized in-memory Session events. No source file or fixture file was changed.

## 2. Command receipt

All commands ran from the fixture root. Unless noted, exit code was `0` and stderr had no test failure.

| ID | Exact command | Exit / stdout summary |
|---|---|---|
| `CMD-00` | `pnpm exec vitest run packages/core/agent-loop/tests/loop.spec.ts -t "runs a simple turn: queued message"` | `1`; `vitest is not recognized`. This exposed a host-global pnpm execution-path problem, not a test failure. |
| `CMD-01` | `& '.\node_modules\.bin\vitest.CMD' run packages/core/agent-loop/tests/loop.spec.ts -t "runs a simple turn: queued message"` | `0`; 1 file passed, 1 test passed, 55 skipped. |
| `CMD-02` | `& '.\node_modules\.bin\vitest.CMD' run packages/core/agent-loop/tests/loop.spec.ts -t "round-trips tool calls"` | `0`; 1 file passed, 1 test passed, 55 skipped. |
| `CMD-03` | `& '.\node_modules\.bin\vitest.CMD' run packages/core/agent-loop/tests/tool-calls.spec.ts -t "starts at most the cap\|exclusive call between two parallel-safe calls forms a barrier\|commits tool/result in model order\|injects additional contexts in model call order"` | `0`; 1 file passed, 4 tests passed, 17 skipped. |
| `CMD-04` | `& '.\node_modules\.bin\vitest.CMD' run packages/core/agent-loop/tests/tool-calls.spec.ts -t "derived history pairs calls in model order"` | `0`; 1 file passed, 1 test passed, 20 skipped. |
| `CMD-05` | `& '.\node_modules\.bin\vitest.CMD' run packages/core/agent-loop/tests/tool-calls.spec.ts packages/core/agent-loop/tests/cancel.spec.ts -t "stops replenishing after abort\|does not run an exclusive barrier after a parallel group aborts\|cancel from an assistant/message observer skips execution\|cancel mid-stream finalizes the streamed prefix"` | `0`; 2 files passed, 4 tests passed, 54 skipped. |

Vitest emitted the same non-fatal warning in each run: Vite now has native tsconfig-path support and the detected `vite-tsconfig-paths` plugin could be removed. This did not change selection or results.

The two read-only observation commands used the same service/plugin sequence as `loop.spec.ts`: import production packages plus `tests/mock-adapter.ts`, register scripted responses/tools, create one Agent, send a followup, wait for idle, and print normalized JSON. Their important outputs are preserved below rather than repeating the long one-line commands.

## 3. `33-X01` — no-tool trace

Result: `PASS`.

Input was user text `no-tool`; `MockAdapter` returned deterministic text `hello there` and normal finish.

Observed receipt:

```text
requestCount=1; toolCallCount=0; toolResultCount=0; finalStatus=idle
seq 0  agent/inbox/spliced
seq 1  turn/start(turn=1)
seq 2  agent/inbox/spliced        # claim/deletion receipt
seq 3  step/start(turn=1,step=1)
seq 4  user/message
seq 5  request/header
seq 6  request/context
seq 7..21 assistant/chunk
seq 22 assistant/message(turn=1,step=1)
seq 23 step/end(turn=1,step=1)
seq 24 turn/end(turn=1,reason=completed)
```

The owner test independently asserted boundary order `turn/start -> step/start -> step/end -> turn/end`, durable Inbox receipt first, user/assistant history, usage, and final `turn/end`. Sequence numbers were strictly increasing and boundaries balanced. No unexpected runtime behavior occurred.

Conclusion: `33-C05` may be promoted to fixture-scoped `CONFIRMED`. This does not cover rejected/empty first admission, Tool, policy, or real Provider behavior.

## 4. `33-X02` — single-tool trace

Result: `PASS`.

Input was `use the tool`; Step 1 returned call `c1 echo({text:"ping"})`; deterministic tool output was `echo: ping`; Step 2 returned final text `done`.

Observed receipt:

```text
requestCount=2; finalStatus=idle
seq 0..6   inbox receipt -> turn/start(1) -> step/start(1,1) -> user -> request
seq 7..16  assistant chunks -> assistant/message(turn=1,step=1)
seq 17     tool/call(turn=1,step=1,callId=c1)
seq 18     tool/result(turn=1,step=1,callId=c1,sourceEventSeqs=[17])
seq 19     step/end(turn=1,step=1)
seq 20     step/start(turn=1,step=2)
seq 21..29 assistant chunks -> assistant/message(turn=1,step=2)
seq 30     step/end(turn=1,step=2)
seq 31     turn/end(turn=1,reason=completed)
```

The second request contained exactly `{type:"tool-result",toolCallId:"c1",content:[{type:"text",text:"echo: ping"}],isError:false}`. The owner test independently asserted two requests, the same result in request 2, and durable `tool/call` plus `tool/result`.

Conclusion: `33-C06` may be promoted to fixture-scoped `CONFIRMED`. Tool success incurred another model Step; it was not treated as a separate Turn or direct task-success oracle.

## 5. `33-X03` — multi-tool ordering trace

Result: `PASS` through a composite of pinned owner fixtures. The fixtures split the frozen falsifiers into deterministic tests instead of relying on one timing-sensitive monolith.

Normalized observations:

1. With `maxParallelToolCalls=2` and model order `c1,c2,c3,c4`, only `c1,c2` started initially. After `c1` settled, `c3` started; the first four durable scheduler events were `call:c1, call:c2, result:c1, call:c3`. All four results finally committed as `c1,c2,c3,c4`. Thus maximum in-flight was bounded by two and reached two.
2. For `parallel A1 -> exclusive A2 -> parallel A3`, observed body order was `r-start-A1, r-end-A1, w-A2, r-start-A3, r-end-A3`. The exclusive call overlapped with neither side.
3. For parallel `c1,c2`, `c2` was deliberately released first. Before `c1` release there was no durable result; after drain, results were `c1,c2`.
4. With settlement order `c2,c1`, plugin contexts committed as `ctx-c1,ctx-c2`, only after the last tool result. Derived next-request history likewise paired tool results as `c1,c2`.

Every call/result pair remained complete. No cap breach, exclusive overlap, settlement-order leak, pairing loss, or undrained started call was observed.

Conclusion: runtime portions of `33-C07` and `33-C08` may be promoted to fixture-scoped `CONFIRMED`. This Tool Batch is not Multi-Agent and says nothing about arbitrary Tool thread safety or external side-effect order.

## 6. `33-X04` — cancellation propagation trace

Result: `PASS` through pinned deterministic cancellation fixtures.

Normalized observations:

1. At cap two, calls `c1,c2` started. Cancellation stopped replenishment; after both started calls were released/drained, `c3,c4` had `tool/call` and synthetic `tool/result` records but their bodies never started. Both results had `isError=true`, `name=AbortError`, `code=ABORTED_BEFORE_DISPATCH`.
2. Result commit order stayed `c1,c2,c3,c4`. Accepted contexts from started calls were parked as `ctx-c1,ctx-c2`, not injected into the aborted Step; after a fresh waking followup they appeared in that order.
3. A pending exclusive barrier after an aborted parallel group never executed; its call still received a balanced synthetic aborted result.
4. Cancelling from the `assistant/message` observer yielded `turn/end({kind:"aborted",reason:{kind:"user"}})`, executed the dangerous tool body zero times, persisted a balanced `c1` error result, and exposed it in the next request. A later prompt completed with a fresh Turn reason `completed`.
5. Mid-stream cancellation retained visible text `partial` with `interrupted=true`; its assistant anchor preceded `step/end`, which preceded `turn/end`. The next request replayed the same visible prefix.

The observed semantics are cooperative drain plus replay balancing, not rollback. No post-cancel replenishment, missing synthetic result, unbalanced boundary, false `completed` outcome for the aborted Turn, or stale cancellation poisoning of later work was observed.

Conclusion: `33-C13` and `33-C14` may be promoted to fixture-scoped runtime `CONFIRMED`. This does not prove OS/process hard-kill, remote API cancellation, or reversal of already-performed external effects.

## 7. Evidence verdict and reproduction notes

- Required traces: `4/4 PASS`.
- Selected owner tests: `10/10 PASS` across the final evidence set (`1 + 1 + 4 + 1 + 4`; skipped tests were outside the selected contract).
- Inline observation runs: `2/2 exit 0`.
- `BLOCKED_EVIDENCE`: none for `33-X01—X04`.
- Unexpected behavior: only the global `pnpm exec` PATH failure and non-fatal Vite plugin warning described above. The workspace-local binary was already present; no install or network fallback was used.
- Reproduce with `corepack pnpm --version` to verify the manifest-pinned manager, then run `CMD-01—05` from the exact fixture. The inline observations are optional diagnostics; owner-test results are the durable acceptance proof.
- Final fixture verification: exact HEAD/tag retained; `git status --short` and `git diff --check` produced no output after all runs.
