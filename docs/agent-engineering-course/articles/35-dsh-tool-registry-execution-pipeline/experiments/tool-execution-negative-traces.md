# Article 35 Frozen Experiment Design｜Tool Execution Negative Traces

Status: `CYCLE 0 NOT ACCEPTED; RECOVERY ATTEMPT 1 NOT ACCEPTED; RECOVERY CYCLE 1 FINAL CAPTURE ACCEPTED / EVIDENCE_MERGE PASS`

## Common contract

- Related article / baseline: `35`, official DSH `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`; planned fixture root is `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`.
- Environment to record at execution: Windows version/architecture, `node --version`, fixture-local `pnpm --version`, exact `HEAD`, exact tag, `git status --porcelain=v1 --untracked-files=all`, selected test/config paths and exit code. Article 28's recorded baseline is Windows 10 x64, Node `v24.18.1`, pnpm `11.7.0`; it is a reference, not a substitute for the execution receipt.
- Fixture boundary: pinned production services plus repo-owned MockAdapter, deterministic in-memory tools/storage/approval and deferred latches. No Provider credential, network, real filesystem/command tool, production data, billing, or side effect outside an in-memory sentinel.
- Source-map prerequisite: resolve `TEST_FILE`, `VITEST_CONFIG`, trace-driver entry and exact result/session observers before execution. The command template is intentionally fail-closed: `node node_modules/vitest/vitest.mjs run <TEST_FILE> --config <VITEST_CONFIG> --testTimeout=30000`; no guessed package/test path is an authorized substitute. Record the resolved command verbatim and do not install, fetch, or modify pinned source merely to make it run.
- Required raw capture per case: immutable input fixture; model tool-call payload; body start/count/settlement receipt; ordered stage/policy/approval events; normalized final result (`isError`, terminal kind/code, model content, redacted diagnostics); Session event(s) and next-step/model-history receipt; spill/reference data where applicable; stdout/stderr, exit code, test count, fixture pre/post HEAD/status/diff, and an artifact manifest with hashes. Capture raw data under the Article 35 experiment artifact namespace, not by committing generated files into the pinned fixture.
- Safety/permission: all tool bodies default deny-side-effect and use an in-memory sentinel. Timeout/cancel must settle/drain started promises before cleanup. Redact argument/result secrets; enforce a 30-second test timeout and a 64 KiB in-memory result ceiling except the controlled large-result payload.
- Shared falsifier: missing any required receipt, a body invocation when a pre-body negative claims suppression, a non-deterministic sleep-based ordering proof, or a dirty pinned fixture means `NOT_ACCEPTED`, not a weakened PASS.

## 35-X01｜Bad arguments reject before typed body

- Related Claims / RQ: `35-C04; 35-C06 / 35-RQ04—05`.
- Hypothesis: for the **source-mapped typed-definition path**, malformed JSON or schema-invalid arguments produce an attributable invalid-argument terminal and do not invoke the typed body; raw argument text and runtime metadata remain distinguishable.
- What would falsify it: body count is nonzero; valid/invalid inputs become indistinguishable; raw payload is lost before terminal attribution; source map shows raw direct registration bypasses the selected validator while the claim remains broad.
- Fixture / Environment: common fixture; one `defineTool`-equivalent source-mapped typed test tool with a counter body, deterministic MockAdapter and in-memory Session.
- Inputs / Variables: one valid control call plus malformed JSON and one schema-invalid JSON call; vary only the raw argument string.
- Expected observable / acceptance: invalid cases have body count `0`, exactly one terminal result/session correlation each, explicit parse-or-schema attribution, and no next-step successful value; valid control has body count `1`. Raw capture must show stage order and callId linkage.
- Safety / commands / writes: no external effect; run resolved common Vitest command; writes only future raw captures/manifest under this experiment namespace; preserve fixture clean receipt.
- Budget / raw capture: ≤30 seconds, ≤64 KiB per normalized capture; save raw call, validation error, body counter, stages, result, Session event, next request/history and stdout/stderr.

## 35-X02｜Deny/approval refusal suppresses body without inventing vote merge

- Related Claims / RQ: `35-C05; 35-C06 / 35-RQ06—07`.
- Hypothesis: a source-mapped denial or approval-refusal path reaches a policy-attributable terminal with no tool-body side effect; it must be observed as actual waterfall/approval/guard behavior, not inferred as `Deny > Ask > Allow` voting.
- What would falsify it: denied/refused input invokes the body or sentinel; listener/approval ordering cannot be captured; final result is treated as proof of a merge rule without source map; result/session correlation is absent.
- Fixture / Environment: common fixture; one in-memory mutating sentinel tool, deterministic deny listener and, if source map requires, deterministic approval refuser.
- Inputs / Variables: allowed control, deny decision, and approval-refusal decision when both paths exist; vary only policy response.
- Expected observable / acceptance: control increments sentinel once; each available negative has sentinel/body count `0`, policy/approval decision receipt, one error result and one correlated Session event. If DSH has one path only, mark the other `NOT_APPLICABLE WITH SOURCE PROOF`, never fabricate it.
- Safety / commands / writes: sentinel stays in memory; run resolved common command; writes only raw capture/manifest; no real approval UI or permission provider.
- Budget / raw capture: ≤30 seconds; save ordered listener/approval/guard events, body/sentinel counts, final result, Session event, next-step receipt and command streams.

## 35-X03｜Timeout requests cooperative stop and classifies the terminal

- Related Claims / RQ: `35-C07; 35-C09 / 35-RQ08; 35-RQ10`.
- Hypothesis: the source-mapped timeout wrapper delivers its deadline signal and reports the source-defined timeout terminal only after the selected execution/next path settles; it does not prove forced termination or rollback.
- What would falsify it: no observable deadline signal; body settles successfully as ordinary success after timeout without an explicit source-mapped precedence rule; trace claims work stopped/rolled back without proof; no drain receipt.
- Fixture / Environment: common fixture; deferred in-memory body that records signal identity/aborted transition and only resolves when the driver releases it after timeout observation.
- Inputs / Variables: timeout shorter than deterministic latch release; control runs with a longer deadline. Vary only timeout policy.
- Expected observable / acceptance: trace records start -> deadline signal -> body observes abort -> controlled settlement/drain -> one timeout-classified result/session event (or source-proved different terminal); control succeeds. No claim about an OS kill or external side effect.
- Safety / commands / writes: no external work; run resolved common command; writes only raw data/manifest; cleanup waits for body settlement before fixture teardown.
- Budget / raw capture: ≤30 seconds wall clock; save timestamp/order-independent latch events, signal state, body settlement, result/session/next-step receipts and stdout/stderr.

## 35-X04｜Caller cancellation drains started work and preserves the boundary

- Related Claims / RQ: `35-C07; 35-C09 / 35-RQ08; 35-RQ10`.
- Hypothesis: a caller/Turn cancellation propagates the source-mapped signal to an already-started deterministic tool and produces a typed interrupted/cancel terminal after the required drain; it neither rolls back the sentinel nor proves remote cancellation.
- What would falsify it: cancel does not reach selected body; result/session terminal lacks correlation; an unstarted body runs after cancellation; trace calls the outcome rollback; subsequent clean control cannot run because the controller stayed poisoned.
- Fixture / Environment: common fixture; two latch-controlled in-memory tools (one started, one held pre-dispatch if scheduler seam permits), MockAdapter/Agent cancellation entry resolved by Source Map, and in-memory Session.
- Inputs / Variables: cancel injected only after started-body receipt; control follow-up after drain. Do not use sleep to race cancellation.
- Expected observable / acceptance: started body observes cancellation and settles; held/unstarted body has count `0` when source path supports the condition; one correlated cancel/interrupted result/session receipt; sentinel shows no fabricated rollback; clean follow-up admission/result is independently recorded.
- Safety / commands / writes: no external effects; run resolved common command; writes only capture/manifest; latch teardown is mandatory before cleanup.
- Budget / raw capture: ≤30 seconds; save phase/signal/body counters, dispatch/settle order, terminal result, Session event, follow-up receipt, fixture cleanliness and streams.

## 35-X05｜Large result uses a bounded projection or documented fallback

- Related Claims / RQ: `35-C09; 35-C10 / 35-RQ10—11`.
- Hypothesis: when the source-mapped opt-in spill policy is enabled, a deterministic oversized plain-text result retains a full local payload reference plus bounded model/log preview and locator; if spill storage fails, the actual source-defined fallback is captured rather than renamed a summary.
- What would falsify it: full payload/preview/locator cannot be correlated; content is silently truncated without source-defined result; semantic summary is claimed although no generator output exists; failed storage loses the terminal without recorded fallback.
- Fixture / Environment: common fixture; in-memory/local-temp spill backend selected by Source Map, deterministic text payload above mapped threshold but under 64 KiB, and a fault-injected spill-write failure variant.
- Inputs / Variables: under-threshold control, over-threshold successful spill, over-threshold failed spill; vary size/storage outcome only.
- Expected observable / acceptance: control stays inline; success captures full-payload hash/locator plus bounded preview and exact model/session projection; failure captures storage error and exact inline/error fallback. All records state `semantic summary: absent` unless a distinct source-mapped generator actually runs.
- Safety / commands / writes: local temporary/in-memory spill only; no retention/service upload; run resolved common command; delete only executor-created temporary spill data after its hash/receipt is captured, leaving pinned fixture clean.
- Budget / raw capture: ≤30 seconds, payload ≤64 KiB; save input/full hash, threshold/policy receipt, storage outcome, preview length/content hash, locator redacted as needed, result/session/next-step receipts and streams.

## Execution handoff

The next role must first complete `SOURCE_MAP` and `CALL_PATH`, substitute only source-proved test/config/entry names, then execute all applicable cases without changing this hypothesis/falsifier/acceptance contract. A missing source prerequisite or unavailable observer is a recorded `BLOCKED`/`NOT_ACCEPTED` case, not permission to broaden the fixture, use a real provider, create Lab 07, or mark Article 35 Evidence Gate passed.

## Lab Engineer execution / raw observation — 2026-08-30

Execution status: `EXECUTED / 5 OF 5 REQUIRED CATEGORIES ADDRESSED / BLOCKED_EVIDENCE`.

The frozen design above is unchanged. This section appends execution facts only; Evidence interpretation and Claim Status remain owned by the Researcher.

### Environment and fixture

- Fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`.
- Identity before and after: origin `https://github.com/deepseek-ai/deepseek-harness.git`; exact tag `dsh-v0.1.2-alpha.1`; full SHA `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Cleanliness before and after: `git status --porcelain=v1 --untracked-files=all`, unstaged diff stat and staged diff stat were all empty.
- Toolchain: Windows `10.0.19045.0` x64; Node `v24.18.1`; pnpm `11.7.0`; Vitest `4.1.8`; Git `2.53.0.windows.2`.
- Default Vitest config: fixture-root `vitest.config.ts`; all commands enforced `--testTimeout=30000`.
- Boundary: unchanged repo-owned tests, MockAdapter/in-memory stores/fake timers/deferred latches only. Provider credential/request, network, server, production tool, real filesystem/command side effect, Lab 07 and A36+ assets remained zero.
- Launcher observation: the first exact `corepack pnpm exec vitest ...` attempt failed before test discovery because Windows did not find `vitest`, although fixture-local `vitest.CMD` existed. Prepending that already-existing `.bin` directory to the process `PATH` restored the required command form. No fixture write occurred. See `raw/environment-and-fixture.txt`.

### Command and category receipts

| Case | Selected exact tests | Result | Frozen acceptance disposition |
|---|---:|---|---|
| `35-X01` | 5 | `1 file / 5 passed / 131 skipped / exit 0` | `FAILED_REQUIRED_SOURCE_EXPERIMENT / BLOCKED_EVIDENCE`: invalid result/code and valid control observed, but no same-call explicit body counter, raw malformed JSON ingress, Session pair or next-history receipt. |
| `35-X02` | 7 | `3 files / 7 passed / 182 skipped / exit 0` | `FAILED_REQUIRED_SOURCE_EXPERIMENT / BLOCKED_EVIDENCE`: deny suppression, ordered Session results, approval audit/refusal and allow control observed in complementary tests, but no one same-call ask-refusal body/sentinel + terminal Session + next-step trace. |
| `35-X03` | 3 | `1 file / 3 passed / 9 skipped / exit 0` | `FAILED_REQUIRED_SOURCE_EXPERIMENT / BLOCKED_EVIDENCE`: deadline signal, controlled drain, `TOOL_TIMEOUT` and fast control observed, but no correlated Session/next-history receipt. |
| `35-X04` | 2 | `2 files / 2 passed / 155 skipped / exit 0` | `FAILED_REQUIRED_SOURCE_EXPERIMENT / BLOCKED_EVIDENCE`: started drain, unstarted suppression, ordered results, parked contexts and follow-up observed, but no same-call signal-observed + drain + Session correlation. |
| `35-X05` | 5 | `2 files / 5 passed / 42 skipped / exit 0` | `FAILED_REQUIRED_SOURCE_EXPERIMENT / BLOCKED_EVIDENCE`: inline control, full save, bounded preview/locator, save failure fallback and independent result/history ordering observed, but no same-call hashes + locator + Session/model projection + failure correlation. |

Final selected receipt: `22 passed / 0 failed`; skipped counts are non-selected tests in the focused files, not skipped required categories. A previous launcher attempt failed before Vitest and is retained as unexpected behavior; it is not counted as a test run. Exact commands, exact test names, combined stdout/stderr summaries, exits and limitations are stored in `experiments/raw/35-x01-*.txt` through `35-x05-*.txt`.

### Failure and limitation disposition

- All five required negative categories were executed with unchanged repo-owned tests. No failing assertion was hidden or reclassified.
- Passing focused tests prove only their asserted observations. They do not satisfy missing frozen same-call observers by implication.
- No instrumentation copy was created and no patch was applied. `raw/instrumentation-diff.txt` records the zero diff.
- Because the common contract makes any missing required receipt `NOT_ACCEPTED`, the assigned Lab execution cannot hand off a pass candidate. The true blocker is `BLOCKED_EVIDENCE`; a Researcher must decide whether to design a new source-owned observation harness or narrow claims. The Lab Engineer does not modify hypotheses, falsifiers, acceptance, Evidence Cards, Claim Status, Article 36+, or global course state.

## Researcher Recovery Cycle 1 design snapshot｜single-file SAME-CALL observation harness

Historical design status at handoff: `RESEARCHER / EXPERIMENT_DESIGN PASS / NOT EXECUTED`. This section preserves the pre-execution snapshot and does not edit or reinterpret the frozen hypotheses, falsifiers, inputs, acceptance, safety, or budgets above. Cycle 0 remains `NOT_ACCEPTED`; its 22 green tests are not acceptance evidence. Actual Recovery execution and Evidence Merge are appended below.

### Exact temporary file and artifact contract

- Fixture root: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` at `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- The only fixture write allowed at the next gate is the exact untracked file `packages/core/agent-loop/tests/article-35-same-call-recovery.spec.ts` under that root. Do not edit an existing test or Vitest config.
- Before execution, copy the exact temporary file bytes to `experiments/raw/recovery-cycle-1/article-35-same-call-recovery.spec.ts`, save a new-file patch as `experiments/raw/recovery-cycle-1/article-35-same-call-recovery.patch`, and hash both. The course copy/patch is the instrumentation receipt; the fixture copy is disposable.
- Additional raw files are fixed as `command.txt`, `combined-output.txt`, `a35-recovery-traces.jsonl`, `environment-and-cleanliness.txt`, and `manifest.txt` beneath `experiments/raw/recovery-cycle-1/`. Raw artifacts are future execution outputs and are not created by this Researcher turn.

### Exact imports and local helpers

The temporary file must use exactly this import surface; aliases already resolve in the pinned fixture and `MockAdapter` is imported from the adjacent repo-owned helper:

```ts
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createHash } from 'node:crypto'
import { Context } from '@deepseek-ai/cordis'
import LlmRuntime, { createUserMessage, ToolCallId, type ContentBlock, type StreamChunk } from '@deepseek-ai/dsh-llm'
import SessionStore, { SessionId, type SessionEvent } from '@deepseek-ai/dsh-session'
import SystemPrompt from '@deepseek-ai/dsh-system-prompt'
import ToolRuntime, { defineContentToolFixture, TOOL_ABORTED, TOOL_ABORTED_BEFORE_DISPATCH, type PostToolDecision, type PreToolDecision } from '@deepseek-ai/dsh-tools'
import ApprovalService, { type ApprovalOutcome } from '@deepseek-ai/dsh-user-approval'
import * as timeoutPolicy from '@deepseek-ai/dsh-tool-call-timeout-policy'
import { TOOL_TIMEOUT } from '@deepseek-ai/dsh-tool-call-timeout-policy'
import { SpillLocator, SpillStore } from '@deepseek-ai/dsh-spill'
import type { SaveTextSpill, SpillRef } from '@deepseek-ai/dsh-spill'
import * as SpillPolicy from '@deepseek-ai/dsh-spill-policy'
import AgentRegistry, { type Agent } from '@deepseek-ai/dsh-agent'
import AgentLoop from '@deepseek-ai/dsh-agent-loop'
import { MockAdapter, textResponse } from './mock-adapter.ts'
```

The file contains only these test-local helpers, with the frozen responsibilities below:

| Helper | Exact responsibility |
|---|---|
| `sha256(text)` / `utf8Bytes(text)` | `node:crypto` SHA-256 hex and UTF-8 byte count; never write payloads to disk. |
| `rawMultiCall(calls)` | Build one assistant `StreamChunk[]` from exact `{ id, name, rawArgs }`; preserve `rawArgs` verbatim in each final tool-call block; finish with `{ kind: 'tool-calls' }`. |
| `createHarness(adapter, options)` | Compose, in order, `LlmRuntime -> SessionStore -> SystemPrompt({persona:''}) -> ToolRuntime -> optional ApprovalService -> optional timeoutPolicy -> optional RecordingSpillStore -> optional SpillPolicy({maxInlineBytes:200}) -> AgentRegistry -> AgentLoop({agents:[], maxParallelToolCalls})`; register adapter `mock`; no other plugin. |
| `waitForIdle(ctx, agent)` | Resolve only from the exact agent's `agent/status=idle` event. |
| `events(agent)` | Return a snapshot of `agent.session.events`; never mutate it. |
| `installStageObservers(ctx, stages)` | Observe `tools/pre-execute`, `tools/execute`, `tools/post-execute`, and `tools/result`; record monotonic local sequence, callId, enter/exit, returned decision/result code; every waterfall observer delegates with `next()` and never changes the result. |
| `resultFor(agent, callId)` / `callFor(agent, callId)` | Select exactly one correlated Session `tool/result` / `tool/call`; fail on zero or duplicate. |
| `nextProjection(adapter, agent, callId)` | Capture the matching `tool-result` block from `adapter.requests[1].messages` and from `agent.session.deriveMessages()`; include counts and normalized content/isError, not the whole request. |
| `normalizeResult(event)` | Emit only `isError`, error name/code/message, flattened text, content-block count and meta presence; no secret-bearing arbitrary object dump. |
| `emit(record)` | First validate every required schema field, then write one line `A35_TRACE <JSON>` to stdout. |
| `RecordingSpillStore` | In-memory `attempts` and `saves`; `saveText` records bytes/hash/source/suggestedName, throws only for tool `big-fail`, otherwise returns `SpillLocator('/spill/<suggestedName>')`; no filesystem. |

`afterEach` must always call `vi.useRealTimers()`. Latches use `Promise.withResolvers`; no `setTimeout`/sleep may establish ordering. A bounded microtask poll is permitted only to wait for a recorded state and must throw after 1,000 iterations.

### Exact test cases and assertions

| Test name | Calls / fixture | Required SAME-CALL assertions before `emit` |
|---|---|---|
| `A35 recovery / 35-X01 / SAME-CALL` | One MockAdapter step contains `x01-valid reader {"path":"/ok"}`, `x01-malformed reader {"path":`, `x01-schema reader {}`; second step is `textResponse('x01-done')`. One typed `defineContentToolFixture` records body starts by `exec.callId`. | `x01-valid` body `1` and success; both invalid callIds body `0`, one call + one result each, `INVALID_ARGS`, verbatim rawArgs, stage trace, and matching next-request + derived-history result. Emit one record per callId. |
| `A35 recovery / 35-X02 / SAME-CALL` | Calls `x02-allow`, `x02-deny`, `x02-ask` target one sentinel tool. Pre listener delegates allow, returns deny reason for deny, returns ask reason for ask. ApprovalService answerer returns `rejected`. | Sentinel/body only `x02-allow=1`; negatives `0`; policy records are ordered; `x02-ask` has one `approval/asked` + one linked `approval/decided(rejected)`; each call has exactly one terminal result and next projection. Do not derive a vote merge. |
| `A35 recovery / 35-X03 / SAME-CALL` | `vi.useFakeTimers()`; `x03-timeout` uses a 100ms definition whose body records the derived signal, awaits its abort, records `signal-observed`, then awaits a cleanup-release latch; `x03-control` uses the same body shape with a 10,000ms budget and immediate success. | After `advanceTimersByTimeAsync(100)`, timeout signal was observed but no `x03-timeout` Session result exists before cleanup release; after release, body settled then exactly one `TOOL_TIMEOUT` result/session/next projection appears. Control starts once and succeeds without aborted signal. No hard-kill/rollback field exists. |
| `A35 recovery / 35-X04 / SAME-CALL` | AgentLoop cap `1`; two parallel-safe calls `x04-started` and `x04-held` target one latch tool. Cancel only after started receipt. Started body records signal abort then awaits cleanup release. After drain/idle, submit a followup consumed by `textResponse('x04-followup')`. | Started body count `1`, held body count `0`; no result before started cleanup release; then `x04-started=TOOL_ABORTED`, `x04-held=TOOL_ABORTED_BEFORE_DISPATCH`, each correlated to Session/history. Followup creates a fresh completed turn/request. Sentinel is reported as observed state, never rollback. |
| `A35 recovery / 35-X05 / SAME-CALL` | Calls `x05-small -> small`, `x05-spill -> big-ok`, `x05-fallback -> big-fail`; payloads are respectively `tiny`, `'HEAD'.repeat(200)+'TAIL'.repeat(200)` (1,600 bytes), and `'x'.repeat(1_000)`. In-memory spill cap is 200 bytes; store fails only for `big-fail`. | Small is inline/no save; success attempt/save hashes equal input hash, locator is `/spill/big-ok.txt`, preview is `<=200` bytes and differs from full hash, exact Session and next projections equal preview; failed attempt records storage error/no locator and exact inline fallback hash/length in result/session/next projection. Every record says `semanticSummary:false`. |

### Frozen JSONL trace schema

Every `A35_TRACE` line is one object with every top-level key present (use `null`, `[]`, or `0`, never omission):

```json
{
  "schema":"a35-same-call-recovery-v1",
  "case":"35-X01",
  "callId":"x01-malformed",
  "rawArgs":"{\"path\":",
  "stages":[{"seq":1,"stage":"pre","phase":"enter","detail":null}],
  "policy":{"pre":null,"approvalAsked":0,"approvalDecided":0,"approvalOutcome":null,"timeoutMs":null,"spillCapBytes":null},
  "body":{"startCount":0,"settleCount":0,"sentinelCount":0,"signalObserved":false,"signalAborted":false,"drained":false},
  "normalizedResult":{"isError":true,"errorName":"ToolArgsError","code":"INVALID_ARGS","message":"<redacted-safe message>","contentBlockCount":1,"contentText":"<bounded text>","metaPresent":false},
  "session":{"callCount":1,"resultCount":1,"callSeq":0,"resultSeq":0,"rawArgs":"{\"path\":","resultCode":"INVALID_ARGS","resultIsError":true,"contentHash":"<sha256>"},
  "nextHistory":{"requestIndex":1,"requestMatchCount":1,"historyMatchCount":1,"resultIsError":true,"contentHash":"<sha256>","followupCompleted":null},
  "spill":{"inputBytes":0,"fullHash":null,"attemptCount":0,"saveCount":0,"storedHash":null,"locator":null,"previewBytes":0,"previewHash":null,"fallbackBytes":0,"fallbackHash":null,"storageError":null,"semanticSummary":false}
}
```

For X03/X04, `stages` additionally contains `body.start`, `signal.abort`, `body.cleanup-release`, `body.settle`, and `session.result` in observed sequence. For X05 only hashes, bounded preview text/hash and synthetic locator are emitted; the full 1,600/1,000-byte payload is not duplicated into JSONL.

### Exact execution and capture commands

From PowerShell, the next executor must preserve commands verbatim in `command.txt`; `$fixtureRoot`, `$articleRoot`, `$tempTest`, and `$rawRoot` are fixed values, not discovery globs:

```powershell
$fixtureRoot = 'C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814'
$articleRoot = 'E:\workspace\TechStackShow\docs\agent-engineering-course\articles\35-dsh-tool-registry-execution-pipeline'
$tempTest = Join-Path $fixtureRoot 'packages\core\agent-loop\tests\article-35-same-call-recovery.spec.ts'
$rawRoot = Join-Path $articleRoot 'experiments\raw\recovery-cycle-1'
git -C $fixtureRoot rev-parse HEAD
git -C $fixtureRoot status --porcelain=v1 --untracked-files=all
git -C $fixtureRoot diff --stat
git -C $fixtureRoot diff --cached --stat
Copy-Item -LiteralPath $tempTest -Destination (Join-Path $rawRoot 'article-35-same-call-recovery.spec.ts')
git -C $fixtureRoot diff --no-index -- /dev/null 'packages/core/agent-loop/tests/article-35-same-call-recovery.spec.ts' | Set-Content -LiteralPath (Join-Path $rawRoot 'article-35-same-call-recovery.patch') -Encoding utf8
$fixtureBin = (Resolve-Path -LiteralPath (Join-Path $fixtureRoot 'node_modules\.bin')).Path
$env:PATH = "$fixtureBin;$env:PATH"
Push-Location $fixtureRoot
corepack pnpm exec vitest run packages/core/agent-loop/tests/article-35-same-call-recovery.spec.ts --testNamePattern "^A35 recovery / 35-X0[1-5] / SAME-CALL$" --testTimeout=30000 --maxWorkers=1 --reporter=verbose --silent=false
Pop-Location
```

Capture combined stdout/stderr and exact exit; extract only lines beginning `A35_TRACE ` into `a35-recovery-traces.jsonl` after removing that prefix. The expected selected count is exactly `1 file / 5 tests`; any different discovery count is `NOT_ACCEPTED`. The command budget is `30s/test`, `120s` total watchdog, one worker, and no retry without a new recorded attempt. Per normalized record is `<=64 KiB`; controlled payloads are `1,600` and `1,000` bytes only.

### Acceptance mapping, safety, and cleanup

- `35-X01—X05` are evaluated independently against their original acceptance. Test exit `0` is necessary but insufficient; each case also needs the required JSONL records, raw command stream, content/patch hashes and clean-fixture receipt. Missing or contradictory data is `NOT_ACCEPTED / BLOCKED_EVIDENCE`.
- The harness uses no provider credential/request, network, server, browser, real command/file tool, production data, billing, persistent spill, Lab 07, or Article 36 asset. All tool effects are in-memory counters/latches. The only filesystem writes are the one exact untracked test and course raw artifacts.
- In a `finally` cleanup after capture, remove only `$tempTest` by exact literal path. Then require: exact `HEAD=cd5ef8148158c3a752a658978873241fdf8e2bbc`; `git status --porcelain=v1 --untracked-files=all`, unstaged diff stat, and staged diff stat all empty. Save those outputs after removal. A missing test path before removal, any additional untracked/modified path, or failed cleanup is a hard `NOT_ACCEPTED` fixture-cleanliness failure; do not use reset/checkout/clean.
- The executor must not edit this design, weaken acceptance, or classify Claims. Successful capture returns raw observations to a Researcher Evidence Merge; it does not itself pass the Evidence Gate.

Recovery handoff: `RESEARCHER / EXPERIMENT_DESIGN PASS`; next gate is exactly `EXPERIMENT_EXECUTE`.

## Lab Engineer Recovery Cycle 1 raw observation — 2026-08-30

The frozen hypotheses, falsifiers, fixtures, acceptance criteria, safety boundary, and budgets above were not weakened. This section records execution history; it does not rewrite Cycle 0.

### Attempt history and accepted receipt

1. Recovery Attempt 1 produced exit `0` but selected `0/5` because the additional top-level suite prefix changed each full Vitest name and the anchored frozen pattern matched none. It remains `NOT_ACCEPTED`; its source, patch and combined output are preserved.
2. The bounded correction removed only that suite wrapper. Case names, inputs, helpers, assertions, safety and acceptance remained unchanged. An intermediate pass was observed but its full stream was not the preserved evidence basis.
3. The final preserved capture replay ran the exact frozen command against the corrected temporary file and recorded `1 file / 5 tests / exit 0`, `13` `A35_TRACE` lines, and no assertion failure. This final capture is the accepted runtime receipt.

The temporary fixture path was saved as exact source plus new-file patch under `raw/recovery-cycle-1/`, then removed by exact literal path. Post-cleanup HEAD is `cd5ef8148158c3a752a658978873241fdf8e2bbc`; fixture status, unstaged diff and staged diff are empty.

### Raw acceptance table

| Case | Records | Accepted observations | Result |
|---|---:|---|---|
| `35-X01` | `3` | valid body `1`; malformed and schema-invalid bodies `0`; both `INVALID_ARGS`; verbatim raw args; one Session pair and one next/history projection per call | `PASS` |
| `35-X02` | `3` | allow sentinel `1`; deny/ask `0`; ordered policy stages; ask has linked asked/decided rejection; one terminal/session/next correlation per call | `PASS` |
| `35-X03` | `2` | signal observed at 100ms; no timeout result before cleanup release; drain then one `TOOL_TIMEOUT`; 10,000ms control succeeds | `PASS` |
| `35-X04` | `2` | started body `1`, held body `0`; no result before release; `ABORTED` / `ABORTED_BEFORE_DISPATCH`; independent follow-up completes | `PASS` |
| `35-X05` | `3` | small inline; 1,600-byte full/store hashes match with 200-byte preview and `/spill/big-ok.txt`; 1,000-byte failed save keeps exact inline hash; all `semanticSummary:false` | `PASS` |

### Integrity and safety qualification

- Fresh parsing found 13 unique callIds with the exact schema and every required top-level/nested field. Every record has one Session call/result and one next-request/derived-history match; Session and next-history content hashes agree.
- Fresh hash/byte recomputation matched the manifest for all 9 non-manifest Recovery artifacts. The final JSONL is `23,654` bytes with SHA-256 `3180b26cc779add7eab3943d235185675cce91ff2813c249b5e7b4062ebc2153`.
- Provider requests, experiment tool-body network operations, real command/file tools, production data, billing, persistent spill, Lab 07 and Article 36+ assets were absent. The fixture uses MockAdapter, in-memory counters/latches/approval/spill and a temporary test file.
- One earlier bare `corepack pnpm --version` preflight was mistakenly run from the course repository, attempted npm-registry discovery, and failed with blocked `EACCES`. This was not an experiment/Provider/tool-body request, but it means the manifest's blanket `NETWORK_REQUESTS=ZERO` must be read only within the accepted experiment boundary—not as “no network attempt anywhere in the executor turn.”
- Timeout remains cooperative: the wrapper emitted `TOOL_TIMEOUT` only after the body observed abort and the test released cleanup. Cancellation remains cooperative and is not rollback. Spill remains an opt-in post policy; storage failure keeps the full inline result, and no semantic summary generator ran.

## Researcher Evidence Merge disposition

`35-X01—X05 = PASS` for the frozen Recovery Cycle 1 source experiment. Cycle 0 and Recovery Attempt 1 remain `NOT_ACCEPTED`; they were not reclassified or used as acceptance evidence.

These observations confirm only the typed definition, ordered waterfall/approval, cooperative timeout/cancel, Session/next-model projection, and opt-in in-memory spill paths exercised at the pinned commit. They do not confirm raw-registration validation, a real Provider, production tools or side effects, every Host UI, retention/access guarantees, rollback/hard kill, or semantic summary.

`EVIDENCE_MERGE PASS / NEXT_ALLOWED_GATE = EVIDENCE_GATE`.
