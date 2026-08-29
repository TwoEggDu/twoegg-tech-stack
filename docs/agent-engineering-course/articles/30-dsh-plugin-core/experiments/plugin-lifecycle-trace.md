# Article 30 Plugin Lifecycle Trace

Status: `RAW OBSERVATION COMPLETE / PASS WITH DISCLOSED NEGATIVE PROBES`

## Frozen experiment protocol

- **Research Question**: 在冻结的 DSH `dsh-v0.1.2-alpha.1` 中，代表性真实插件 `packages/context/time-context` 是否通过 Cordis plugin fiber 完成可观察的 `install/apply -> register -> operate -> dispose` 生命周期；其可逆效果能否由同一 `agent/pre-step` 输入在 dispose 前后产生不同结果来证明？
- **Hypothesis**: `ctx.plugin(timeContext)` 调用导出的 `apply` 并注册一个 prepend `agent/pre-step` listener；eligible pre-step 会追加一条 source 为 `plugin/time-context` 的 snapshot contribution；`fiber.dispose()` 后同一 Context 上再次触发 pre-step 不再追加该 contribution。
- **Falsifier**: 以下任一结果推翻或收窄假设：插件未能挂载；挂载后 eligible pre-step 没有 contribution；dispose 后 contribution 数仍增加；无效配置静默接受；缺失 `agents` 依赖仍正常挂载；或 owner test 只在修改预期/实现后才通过。
- **Fixture**: 外部只读基线仓库 `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`，预期 commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`；依赖已由主流程安装，实验不改源码、不更新 snapshot。
- **Environment**: Windows 10 x64 / PowerShell 7；Node、pnpm、Vitest 的实际版本在执行记录中采集；时间测试使用 owner fixture 的 fake clock 和 `TZ=UTC`，不能外推为真实墙钟或真实 provider。
- **Inputs**:
  1. 精确 owner test：`removes its listener when the plugin fiber disposes`。
  2. operation test：`persists one ordered context per request, accumulates readings, and leaves system headers unchanged`。
  3. loader/export test：`keeps namespace metadata and boots the agent listener through unwrapExports`。
  4. negative configuration tests：invalid zone、unavailable process zone、invalid refresh interval。
  5. dependency/order probe：在没有 `AgentRegistry` 的 Context 上挂载声明 `inject = ['agents']` 的插件，并检查失败/未就绪状态；另由 owner pre-step waterfall 观察 prepend listener 是先委托 downstream 再贡献。
- **Acceptance Criteria**:
  - 精确 dispose owner test 原样通过，且断言显示 dispose 前 1 条、dispose 后仍为 1 条 contribution。
  - operation test 原样通过，证明真实 AgentLoop fixture 中每个 request 有且仅有一个有来源、按序持久化的 time-context contribution。
  - loader/export test 原样通过，证明 Loader unwrap 后的 `name`、`inject`、`Config`、`apply` 与实际 listener boot。
  - negative tests 原样通过并给出 fail-loud 边界；dependency probe 的观察不得改写成强于实际输出的结论。
  - 每条命令保留 exit code、stdout/stderr 摘要；结束时 fixture HEAD 精确且 `git status --short` 为空。
- **Safety**: 不使用 provider credential，不访问网络，不改 DSH tracked files，不改 owner 预期，不写本文章除本文件外的任何资产；若命令失败，原样保留并只做不会改变测试语义的诊断。

## Execution record

### E0｜Baseline and environment

Observed on 2026-08-30 (Asia/Shanghai):

| Item | Observation |
|---|---|
| Fixture | `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` |
| Fixture HEAD before execution | `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| Fixture status before execution | empty |
| OS | `Microsoft Windows NT 10.0.19045.0`, `X64` |
| PowerShell | `7.6.4` |
| Node | `v24.18.1` |
| Vitest | `4.1.8`, `win32-x64`, Node `v24.18.1` |

The first package-manager version probe was deliberately retained as a failed observation:

```text
command: corepack pnpm --version
cwd: E:\workspace\TechStackShow
exit: 1
stderr summary: Corepack tried to fetch https://registry.npmjs.org/pnpm/latest and failed with fetch failed / AggregateError [EACCES].
```

This was not needed for the experiment. Every test below invoked the already-installed fixture-local Vitest by absolute working-directory ownership; no install or network retry followed.

### E1｜Exact owner lifecycle test: install, operate, dispose

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "removes its listener when the plugin fiber disposes"
cwd: C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814
exit: 0
stdout summary: Test Files 1 passed (1); Tests 1 passed | 18 skipped (19); Duration 2.21s.
stderr summary: none; stdout included three non-failing Vite notices that vite-tsconfig-paths can be replaced by native resolve.tsconfigPaths.
```

The unchanged owner test performs this exact lifecycle:

1. creates a Cordis `Context`;
2. mounts `AgentRegistry`;
3. mounts the real `@deepseek-ai/dsh-time-context` plugin and retains its returned fiber;
4. opens one session turn and fires eligible `agent/pre-step` step 1;
5. awaits `fiber.dispose()`;
6. fires step 2 on the same Context and Agent;
7. asserts that the session still contains exactly one time-context contribution.

The passing test therefore establishes the reversible effect at owner-test-fixture level. It does not by itself expose the counts on stdout, so E2 adds an explicit read-only probe.

### E2｜Explicit before/after contribution-count probe

The probe used fixture-local `tsx -e`, the same public package entry points as the owner test, a fresh Cordis Context, `AgentRegistry`, one Session, and the real `agentEvents(...).waterfall('agent/pre-step', ...)` operation. No source or snapshot was changed.

```text
command: node node_modules/tsx/dist/cli.mjs -e "<bounded lifecycle probe>"
cwd: C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814
exit: 0
stdout:
{"beforeDispose":1,"afterDispose":1,"firstSource":{"kind":"plugin","plugin":"time-context","form":"snapshot","sections":[{"name":"time-context","text":"Time sampled while preparing turn 1, step 1: 2026-08-29T21:38:50+00:00[UTC]\nBrowser time zone for this request: unavailable. Ask the user to clarify otherwise-unqualified dates and times.\nElapsed since the preceding model-visible message: 0s."}]}}
stderr: empty
```

Interpretation bounded to this run:

- mounting the plugin with `timeZone: 'UTC'` produced one `time-context` snapshot contribution on the first eligible pre-step;
- the source shape was `kind=plugin`, `plugin=time-context`, `form=snapshot`, one section named `time-context`;
- after awaiting that plugin fiber's disposal, the second otherwise-eligible pre-step did not increase the count (`1 -> 1`);
- this observes listener removal by effect. It does not introspect Cordis's private listener collection.

One initial form of this probe failed before execution because `tsx -e` emitted CommonJS and rejected top-level await:

```text
exit: 1
error: Transform failed; Top-level await is currently not supported with the "cjs" output format (2 locations).
```

The retry only wrapped the same body in an async IIFE; it did not change the hypothesis, fixture, plugin, expected counts, or source.

### E3｜Operate path through the real AgentLoop owner fixture

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "persists one ordered context per request, accumulates readings, and leaves system headers unchanged"
exit: 0
stdout summary: Test Files 1 passed (1); Tests 1 passed | 18 skipped (19); Duration 2.15s.
stderr summary: none; same non-failing vite-tsconfig-paths notices on stdout.
```

The unchanged assertion drives a real `AgentLoop` with a deterministic in-process `ScriptedAdapter` and a fixture tool. Its two requests establish:

- two model requests and two `step/start` events;
- one time-context contribution per request;
- every contribution occurs after its matching `step/start`, uses `surfaceOp: append`, and is source-attributed to `time-context`;
- step 2 sees both accumulated readings while step 1 cannot see the future step-2 reading;
- system text and durable `request/header` remain free of the time-context prose.

This is an owner-test AgentLoop fixture, not a production model/provider call.

### E4｜Real Loader/headless composition e2e

The first e2e command used the wrong collector and is retained:

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.e2e.ts
exit: 1
stdout summary: No test files found; default config includes *.spec.ts and does not collect this *.e2e.ts file.
stderr summary: none.
```

Repository `package.json` declares `test:e2e` with `vitest.e2e.config.ts`, so the corrected command used that existing owner entry without changing collection rules:

```text
command: node node_modules/vitest/vitest.mjs run --config vitest.e2e.config.ts packages/context/time-context/tests/time-context.e2e.ts
exit: 0
stdout summary: Test Files 1 passed (1); Tests 1 passed (1); Duration 3.01s (test body 1.70s).
stderr summary: none; one non-failing vite-tsconfig-paths notice on stdout.
```

This owner e2e boots the real fixture `cordis.yml` through `boot`, with Loader rows for deterministic mock LLM, subprocess, bash, time-context, agent spine, JSONL persistence, and checkpoint policy. The unchanged assertions verified:

- two turns terminate and two `step/start` events exist;
- exactly two persisted plugin-context events exist, one after each corresponding step start;
- both carry `surfaceOp: append` and the exact `plugin/time-context/snapshot/section` attribution shape;
- the process-zone timestamp is rendered in `Asia/Shanghai`;
- request headers do not contain the injected prose.

This is **REAL HEADLESS FIXTURE RUNTIME with a deterministic mock LLM**. It proves Loader composition, Agent operation, and durable JSONL observation without credentials. It does not prove a production deployment, network provider, model quality, token accounting, or cost.

### E5｜Loader export and apply activation boundary

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "keeps namespace metadata and boots the agent listener through unwrapExports"
exit: 0
stdout summary: Test Files 1 passed (1); Tests 1 passed | 18 skipped (19); Duration 2.07s.
stderr summary: none; same non-failing Vite notices on stdout.
```

The owner assertion verifies the namespace has no synthetic default export, Loader `unwrapExports` preserves the namespace, `name === 'time-context'`, `inject === ['agents']`, `Config` exists, and `apply` is callable. It then mounts the unwrapped plugin with `AgentRegistry` and observes a time-context contribution on pre-step. Behaviorally, the plugin has moved from import/metadata through mount/apply into an active registered effect.

### E6｜Negative configuration evidence

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "fails loud for an invalid explicit zone or an unavailable process zone"
exit: 0
stdout summary: 1 passed | 18 skipped.

command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "rejects invalid refresh intervals at plugin load with one diagnostic"
exit: 0
stdout summary: 1 passed | 18 skipped.
```

The first unchanged test expects plugin-load rejection for `Not/A_Real_Zone` and for an unavailable system-zone resolver. The second iterates `-1`, `0.5`, `MAX_SAFE_INTEGER + 1`, positive infinity, and `NaN`, expecting the same explicit non-negative-safe-integer diagnostic. These are fail-loud configuration boundaries; they are not examples of a partially active plugin.

### E7｜Negative dependency evidence: pending, not thrown

The dependency probe intentionally omitted `AgentRegistry`:

```text
command: node node_modules/tsx/dist/cli.mjs -e "<mount time-context into a fresh Context without AgentRegistry; print fiber inject/missing/state>"
exit: 0
stdout: {"inject":["agents"],"missing":["agents"],"state":0}
stderr: empty
```

The frozen repository maps Cordis fiber state `0` to `PENDING`. Therefore the observed contract is precise: a consumer mounted before its required `agents` service is legal Cordis composition, but remains parked and names the missing service; it did not activate and it did not immediately throw. Product boot layers may later audit a settled Loader tree and turn unresolved pending entries into a composition error, but that is a distinct owner boundary.

### E8｜Negative order/downstream evidence

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "does not commit a preparation reading when a downstream pre-step listener"
exit: 0
stdout summary: Test Files 1 passed (1); Tests 2 passed | 17 skipped (19); Duration 2.10s.
stderr summary: none; same non-failing Vite notices on stdout.
```

The two parameterized owner cases install a later pre-step listener that either throws or cancels. Both passing cases assert zero time-context readings, zero adapter requests, and no `step/start`. Combined with the plugin's owner test fixture, this shows the prepended listener delegates downstream before committing its proposed contribution: downstream failure/cancellation does not leave a phantom reading.

### E9｜Whole owner spec regression check

```text
command: node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts
exit: 0
stdout summary: Test Files 1 passed (1); Tests 19 passed (19); Duration 2.26s.
stderr summary: none; same non-failing Vite notices on stdout.
```

This broader check confirms the targeted lifecycle observations coexist with the package's interval, browser-zone, abort, resume/shadowed history, AgentLoop, configuration, and Loader-export cases. It is still package-owner test evidence, not whole-repository health.

## Lifecycle observation matrix

| Phase | Concrete observation | Evidence layer | Result |
|---|---|---|---|
| Install/import | Loader unwrap preserves namespace metadata and exposes `name`, `inject`, `Config`, `apply` | owner test | CONFIRMED |
| Apply/activate | mount with `AgentRegistry` leads to an operative pre-step effect | owner test + bounded probe | CONFIRMED |
| Register | first eligible `agent/pre-step` returns one source-attributed time-context contribution | bounded probe | CONFIRMED BY EFFECT |
| Operate | two AgentLoop requests receive ordered, accumulated, append-only contributions | owner AgentLoop fixture | CONFIRMED |
| Operate through composition | real Loader/headless fixture persists one attributed event per request across two turns | real headless fixture + mock LLM | CONFIRMED |
| Dispose | after awaited plugin-fiber disposal, same Context/Agent pre-step count remains `1 -> 1` | exact owner test + bounded probe | CONFIRMED BY EFFECT |
| Invalid config | invalid time zones and unsafe refresh intervals reject during load | owner negative tests | CONFIRMED |
| Missing dependency | without `agents`, fiber reports missing `agents` and remains state `0/PENDING` | bounded negative probe | CONFIRMED |
| Downstream abort/failure | throw/cancel leaves no time reading, model request, or step start | owner negative tests | CONFIRMED |
| Real provider/model | no credentialed network provider was invoked | experiment boundary | NOT TESTED |
| Production runtime | no deployed service or production config was invoked | experiment boundary | NOT TESTED |

## Reproduction recipe

From the exact clean fixture commit:

```powershell
node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "removes its listener when the plugin fiber disposes"
node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "persists one ordered context per request, accumulates readings, and leaves system headers unchanged"
node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "keeps namespace metadata and boots the agent listener through unwrapExports"
node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts -t "does not commit a preparation reading when a downstream pre-step listener"
node node_modules/vitest/vitest.mjs run --config vitest.e2e.config.ts packages/context/time-context/tests/time-context.e2e.ts
node node_modules/vitest/vitest.mjs run packages/context/time-context/tests/time-context.spec.ts
```

The E2 and E7 inline probes are fully summarized above; they are diagnostic supplements, not owner test assets. Reproduce them only from a clean fixture and do not save them into the repository.

## Limitations and falsification status

- The main lifecycle hypothesis survived every valid targeted command.
- Disposal is observed through absence of a second effect, not through access to Cordis's private listener table.
- The e2e uses a deterministic mock LLM and local process composition. It closes the real Loader/headless/durable-event path, not a real provider path.
- The package-level results cannot establish all DSH plugins share identical lifecycle, dependency, event-order, or cleanup semantics.
- `time-context` is a Plugin Context contribution. It is not a Model Context abstraction, Session Event type family, generic Plugin Event bus, Tool registration, or BuildPilot architecture decision.
- Missing dependency is `PENDING` at raw Cordis mount in this probe; claiming immediate failure would contradict the observation. A higher boot audit may reject unresolved pending entries later.
- The failed Corepack probe, wrong-config e2e collection, and first top-level-await probe are harness-command errors, not product failures; all are retained to keep the experiment reproducible.

## Final fixture integrity

```text
git rev-parse HEAD
cd5ef8148158c3a752a658978873241fdf8e2bbc

git status --short
<empty>

git diff --stat
<empty>
```

Final classification: `TEST_FIXTURE_RUNTIME_CONFIRMED + REAL_HEADLESS_MOCK_RUNTIME_CONFIRMED`. No production/provider claim is made.
