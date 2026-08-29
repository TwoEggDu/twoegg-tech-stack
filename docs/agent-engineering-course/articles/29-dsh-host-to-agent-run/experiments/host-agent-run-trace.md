# Article 29 Host-to-Agent Runtime Trace

Status: `RAW OBSERVATION COMPLETE / TEST-FIXTURE RUNTIME CONFIRMED WITH WINDOWS COUNTER-EVIDENCE`

## 1. Frozen experiment design

### Research Question

在不使用真实 provider credential、不绑定公网、且不修改 DSH tracked source 的前提下，固定 revision 的 repo-owned product headless profile fixture 能否经 `dsh --profile headless`、Profile/Bundle/Loader、headless runner 与 Agent/Session 路径，产生一条包含 `turn/start`、`step/start`、`assistant/message`、`step/end`、`turn/end` 的 terminal durable event sequence？

### Hypothesis

`apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts` 的 `runs one task through the product headless profile command` 场景使用 `headless-profile.cordis.yml` 注入 deterministic `cli-mock` adapter，仍从真实 product CLI 的 named headless profile 启动；它应在 loopback-only 条件下完成两步 Agent loop、一次真实本机 `bash` tool round trip、Session 持久化与 terminal output，并由 owner test 对 persisted log 和 stdout 做交叉断言。

### What would falsify the hypothesis

以下任一项出现即反证动态命题，且不得以 expected snapshot 代替 observed runtime：

1. 精确 targeted test 无法启动、超时或非零退出。
2. 场景只调用手工 `ctx.plugin(...)` harness，未经过 product `dsh --profile headless` 与 Loader。
3. 本轮直接运行没有观察到一个新建 Session log，或 durable log 中缺少 `turn/start`、`step/start`、`assistant/message`、`step/end`、`turn/end` 任一 terminal lifecycle event。
4. observed stdout 与 durable Session 的最后 assistant message 不一致，或缺少 `CLI_TOOL_ROUND_TRIP`。
5. 发生外部 provider/network 请求、使用真实 credential、出现非 loopback bind、产生未声明成本，或 DSH fixture 的 tracked/untracked clean state 被破坏。

### Fixture and identity

- Official repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Product test source: `apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts`
- Overlay: `apps/cli/tests/profiles/headless/tests/fixtures/headless-profile.cordis.yml`
- Deterministic adapter: `packages/test-support/loader-smoke/tests/fixtures/cli-mock-llm.ts`

### Environment to capture

执行前记录本机时间、Windows version/architecture、Node、project-pinned pnpm、Git、PowerShell、fixture origin/HEAD/tag/clean；执行后再次记录 HEAD/tag/clean。环境只记录 secret-like variable name 的过滤计数，不记录任何值。

### Commands

1. Owner targeted test：

   ```text
   corepack pnpm run test:expected -- apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts -t "runs one task through the product headless profile command"
   ```

2. Direct product-profile observation：从一个新建隔离临时 cwd，以 source launch contract 运行：

   ```text
   node --import tsx/esm apps/cli/src/bin.ts --profile headless --patch <absolute headless-profile.cordis.yml> "Prove the product headless profile path with one real tool round trip."
   ```

   同时设置 `TSX_TSCONFIG_PATH=<repo>/tsconfig.json`、`DSH_HOME=<isolated cwd>/.dsh`、`DSH_AGENTS_HOME=<isolated cwd>/.agents`、`DSH_TELEMETRY_DISABLED=1`。直接探针复现 owner fixture 的 `DSH_PERMISSION_MODE=danger-full-access`，但 cwd 和 DSH homes 都是新建临时目录；mock 固定只调用 `printf CLI_TOOL_ROUND_TRIP`。继承环境中名字匹配 `KEY|SECRET|TOKEN|PASSWORD` 的变量全部删除。

3. Optional keyless negative probe：若正向 deterministic probe 成功，另用新的隔离 cwd、不带 overlay、`DSH_PERMISSION_MODE=read-only` 执行同一 product headless entry，确认真实 provider 路径停在 credential boundary；它不能替代正向 trace。

### Expected observations and acceptance

- Targeted owner test：exit `0`，且只有命名场景通过。
- Direct run：在 30 秒内退出 `0`；stdout 为 terminal model text；stderr 不包含秘密或外部请求证据。
- Durable Session：恰有一个新 Session log；事件序列至少包含 `session` header、`turn/start`、两轮 `step/start` / `step/end`、一次 `tool/call` / `tool/result`、最后 `assistant/message`、`turn/end`；terminal message 与 stdout 均包含 `CLI_TOOL_ROUND_TRIP`。
- Network/cost：只允许 repo-owned deterministic adapter 和本机 subprocess；不允许真实 provider、DNS/HTTP provider call、token charge 或 public listener。mock 里的 token usage 是 deterministic fixture metadata，不是 provider 计费证据。
- Evidence label：成功只能升级为 `TEST_FIXTURE_RUNTIME_CONFIRMED`，不能升级为 `SUPPORTED_REAL_PROVIDER_RUNTIME_CONFIRMED`。

### Safety, credential, network, cost and permission boundary

- 不读取、打印、传递或请求真实 provider credential。
- 子进程环境移除名字匹配 `KEY|SECRET|TOKEN|PASSWORD` 的全部变量；只落盘过滤前后计数。
- 不运行 real-model/full-loop e2e，不连接外部 provider，不绑定公网，不使用 production inputs。
- `danger-full-access` 是 owner test 为本机 `printf` tool round trip 设置的 DSH permission mode，不是 OS sandbox 绕过授权；探针仅能写新建临时 cwd/DSH homes。若常规 sandbox 因权限失败，只允许完全相同命令做一次窄 host-access retry，并同时保留首次失败。
- 不修改 DSH tracked source。自然生成的 ignored Session/temp artifacts留在隔离目录；完成后只做 read-only integrity check。
- DSH 是 developer preview；本实验不证明 sandbox 完备性、安全审计、生产可用性、网络可靠性、真实 token/cost 或真实模型质量。

## 2. Raw observations

The design in section 1 was written before any command below ran. The observed Windows result falsified the successful `bash` round-trip part of the hypothesis; the acceptance criteria were not changed afterward.

### Probe A — pre-execution fixture and environment identity

```text
Start: 2026-08-30T04:38:52.6190548+08:00
OS: Microsoft Windows NT 10.0.19045.0
Architecture: X64
Node: v24.18.1
Project-pinned pnpm: 11.7.0
Git: git version 2.53.0.windows.2
PowerShell: 7.6.4
origin: https://github.com/deepseek-ai/deepseek-harness.git
HEAD: cd5ef8148158c3a752a658978873241fdf8e2bbc
local tag target: cd5ef8148158c3a752a658978873241fdf8e2bbc
status rows: 0
secret-like environment names: 5
DEEPSEEK_API_KEY present: false
DEEPSEEK_BASE_URL present: false
DSH/DEEPSEEK-prefixed secret-like names: 0
```

Result: `PASS`. This fixes the source object and confirms that neither DSH nor DeepSeek provider credentials were present. No environment value was read or printed.

### Probe B — owner command as frozen, with its PowerShell argument-routing failure retained

Command:

```text
corepack pnpm run test:expected -- apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts -t "runs one task through the product headless profile command"
```

Observed terminal facts:

```text
Start: 2026-08-30 04:39:00 +08:00
Exit: 1
Test Files: 4 failed | 6 passed (10)
Tests: 4 failed | 23 passed (27)
Duration: 51.34s
Target file: 12 tests | 1 failed
Target case duration: 6212ms
```

The literal `--` reached Vitest before the requested file and name filter, so Vitest collected ten expected-test files rather than one. That is a command-routing failure in this PowerShell/pnpm invocation, not an Article 29 targeted result. The accidental wider run is retained because it happened; its unrelated ACP/path/timing failures are not used by this article.

The target case did execute inside that run and produced the same Windows counter-evidence as the corrected probe below: `tool/result` contained `Error: unknown tool "bash"`, then the deterministic adapter returned `CLI tool round trip complete: Error: unknown tool "bash"` and the expected Linux-oriented snapshot mismatched.

### Probe C — corrected exact owner test

Command:

```text
node node_modules/vitest/vitest.mjs run --config vitest.expected.config.ts apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts -t "runs one task through the product headless profile command"
```

Observed terminal facts:

```text
Start: 2026-08-30 04:40:08 +08:00
Exit: 1
Test Files: 1 failed (1)
Tests: 1 failed | 11 skipped (12)
Target case duration: 4189ms
Duration: 6.25s
Snapshot mismatch: expected bash success; received UNKNOWN_TOOL
```

The shortest exact counter-evidence from the observed snapshot diff was:

```json
{"type":"tool/result","data":{"turn":1,"step":1,"message":{"source":{"kind":"tool","callId":"cli-smoke-call"},"content":[{"type":"tool-result","toolCallId":"cli-smoke-call","content":[{"type":"text","text":"Error: unknown tool \"bash\""}],"isError":true}],"role":"user","id":"{{sessionId}}"},"error":{"name":"ToolNotFoundError","code":"UNKNOWN_TOOL"}},"sourceEventSeqs":[22],"surfaceOp":"append"}
```

This is not a sandbox-access failure and was not retried with host escalation. The pinned base patch itself conditionally disables `tool-bash` on `process.platform === 'win32'` and enables `tool-pwsh` there, while the deterministic `cli-mock` fixture always requests the tool name `bash`. The owner test therefore proves that the assembled product path ran, but its success snapshot is not portable to this Windows composition.

Result: `FAIL / COUNTER-EVIDENCE RETAINED`. The test cannot be reported as passing, and its expected snapshot cannot be presented as the current observation.

### Probe D — direct product headless profile observation

Invocation contract:

```text
node --import <absolute repo-owned tsx/esm> <fixture>/apps/cli/src/bin.ts --profile headless --patch <fixture>/apps/cli/tests/profiles/headless/tests/fixtures/headless-profile.cordis.yml "Prove the product headless profile path with one real tool round trip."
cwd: newly created isolated temporary directory
TSX_TSCONFIG_PATH: pinned fixture tsconfig.json
DSH_HOME / DSH_AGENTS_HOME: children of isolated cwd
DSH_PERMISSION_MODE: danger-full-access
DSH_TELEMETRY_DISABLED: 1
secret-like inherited environment names removed: 5
```

Observed process result:

```text
Start: 2026-08-30T04:40:47.7921734+08:00
End: 2026-08-30T04:40:50.9382747+08:00
Duration: 3146ms
Timed out: false
Exit: 0
```

Complete sanitized stdout and stderr were short enough to retain verbatim:

```text
stdout:
CLI tool round trip complete: Error: unknown tool "bash"

stderr:
dsh: reasoning:
Inspecting the task before the tool call.
```

The run created one compressed Session log. It is an external temporary artifact and is identified here so the durable bytes can be distinguished from the normalized expected snapshot:

```text
Path: C:\Users\IGG\AppData\Local\Temp\dsh-a29-direct-b1fdbbd7f1e844f4a971dae56dab0a28\.dsh\sessions\--C-Users-IGG-AppData-Local-Temp-dsh-a29-direct-b1fdbbd7f1e844f4a971dae56dab0a28--\session-803d1771-3898-4845-90de-4ae62aaf3efa\session.jsonl.zstd
Compressed bytes: 25430
Compressed SHA-256: 0F746D1BCF173B72352AD372DEFE31A4775DB3C5B0EBA1D487BC3D106DF38E3E
Zstandard frames: 8
Torn frame start: none
Decoded bytes: 74578
Decoded SHA-256: D835C590105F379DEBDE7A634CAB19F96CF05BE44A9CF9092D4243F48B0ECBDF
Rows: 36
```

Complete durable event-type sequence:

```text
session -> permission/preset -> sandbox/mode -> approval/policy -> agent/inbox/spliced -> turn/start -> agent/inbox/spliced -> step/start -> user/message -> user/message -> session/title -> request/header -> request/context -> session/title-llm-request -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/message -> tool/call -> tool/result -> step/end -> step/start -> request/header -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/chunk -> assistant/message -> step/end -> turn/end
```

The terminal subset below is copied from this run's decoded Session bytes without substituting the expected snapshot:

```jsonl
{"type":"turn/start","seq":4,"time":1788036050785,"data":{"turn":1}}
{"type":"step/start","seq":6,"time":1788036050834,"data":{"turn":1,"step":1}}
{"type":"assistant/message","seq":21,"time":1788036050849,"data":{"turn":1,"step":1,"message":{"role":"assistant","content":[{"type":"reasoning","text":"Inspecting the task before the tool call."},{"type":"tool-call","id":"cli-smoke-call","name":"bash","arguments":"{\"command\":\"printf CLI_TOOL_ROUND_TRIP\",\"description\":\"Prove the CLI tool round trip.\"}"}],"source":{"kind":"model","provider":"cli-mock","model":"cli-mock"},"id":"88533f9e-6c97-4d93-ab35-ac04672e7c62"},"usage":{"inputTokens":11,"outputTokens":3,"cacheReadTokens":2}},"sourceEventSeqs":[[13,20]],"surfaceOp":"append"}
{"type":"tool/call","seq":22,"time":1788036050850,"data":{"turn":1,"step":1,"callId":"cli-smoke-call","name":"bash","arguments":"{\"command\":\"printf CLI_TOOL_ROUND_TRIP\",\"description\":\"Prove the CLI tool round trip.\"}"}}
{"type":"tool/result","seq":23,"time":1788036050854,"data":{"turn":1,"step":1,"message":{"source":{"kind":"tool","callId":"cli-smoke-call"},"content":[{"type":"tool-result","toolCallId":"cli-smoke-call","content":[{"type":"text","text":"Error: unknown tool \"bash\""}],"isError":true}],"role":"user","id":"9a0a4279-e8ee-4504-905f-577da7d2523a"},"error":{"name":"ToolNotFoundError","code":"UNKNOWN_TOOL"}},"sourceEventSeqs":[22],"surfaceOp":"append"}
{"type":"step/end","seq":24,"time":1788036050855,"data":{"turn":1,"step":1}}
{"type":"step/start","seq":25,"time":1788036050865,"data":{"turn":1,"step":2}}
{"type":"assistant/message","seq":32,"time":1788036050870,"data":{"turn":1,"step":2,"message":{"role":"assistant","content":[{"type":"text","text":"CLI tool round trip complete: Error: unknown tool \"bash\""}],"source":{"kind":"model","provider":"cli-mock","model":"cli-mock"},"id":"11e82a61-74e4-47f9-b50c-a088f2af820a"},"usage":{"inputTokens":7,"outputTokens":5,"reasoningTokens":1}},"sourceEventSeqs":[[27,31]],"surfaceOp":"append"}
{"type":"step/end","seq":33,"time":1788036050870,"data":{"turn":1,"step":2}}
{"type":"turn/end","seq":34,"time":1788036050871,"data":{"turn":1,"reason":{"kind":"completed"}}}
```

Result: `TEST_FIXTURE_RUNTIME_CONFIRMED_WITH_COUNTEREVIDENCE`. The observed run confirms the product `dsh --profile headless` route reached Loader-composed runtime, created one Session, executed one Turn and two Steps, persisted model/tool events and reached `turn/end(completed)`. It also confirms that the Windows composition rejected the fixture's requested `bash` tool. Exit `0` and `turn/end(completed)` describe loop settlement, not successful tool execution.

### Probe E — independent credential-free real-provider negative path

Invocation used a second new isolated cwd and the same product source entry without the mock overlay. Conditions:

```text
Task: Return one inert word without tools.
DSH_PERMISSION_MODE: read-only
DSH_TELEMETRY_DISABLED: 1
secret-like inherited environment names removed: 5
Start: 2026-08-30T04:42:00.0749982+08:00
End: 2026-08-30T04:42:03.1801127+08:00
Duration: 3105ms
Timed out: false
Exit: 1
```

Complete sanitized process streams:

```text
stdout:

stderr:
dsh: MISSING_CREDENTIAL: llm-deepseek: no API key for provider route "deepseek-official"; store DEEPSEEK_API_KEY through the credentials service (the web Models page writes it), or export DEEPSEEK_API_KEY in the launching environment
```

The negative run also produced one Session log:

```text
Path: C:\Users\IGG\AppData\Local\Temp\dsh-a29-keyless-d51895340395467fb8da98ad3d64dc6d\.dsh\sessions\--C-Users-IGG-AppData-Local-Temp-dsh-a29-keyless-d51895340395467fb8da98ad3d64dc6d--\session-7200370f-f068-44fe-aef0-9a6dbb318f2f\session.jsonl.zstd
Compressed bytes: 13245
Compressed SHA-256: 59A3FC1BCC39EB225C4AD3F3EC8FA152CD0F769CD0CB7FA4520C7A3DC24B458B
Zstandard frames: 5
Decoded bytes: 37968
Decoded SHA-256: EE40D90EAEB7C32D759AA596FE6ED88EBD4CDFB4837E79A7EFA4434C06517B5D
Rows: 17
Event types: session -> permission/preset -> sandbox/mode -> approval/policy -> agent/inbox/spliced -> turn/start -> agent/inbox/spliced -> step/start -> user/message -> user/message -> session/title -> request/header -> request/context -> session/title-llm-request -> assistant/chunk -> step/end -> turn/end
```

Observed terminal subset:

```jsonl
{"type":"turn/start","seq":4,"time":1788036123048,"data":{"turn":1}}
{"type":"step/start","seq":6,"time":1788036123097,"data":{"turn":1,"step":1}}
{"type":"step/end","seq":14,"time":1788036123111,"data":{"turn":1,"step":1}}
{"type":"turn/end","seq":15,"time":1788036123112,"data":{"turn":1,"reason":{"kind":"error","error":{"message":"llm-deepseek: no API key for provider route \"deepseek-official\"; store DEEPSEEK_API_KEY through the credentials service (the web Models page writes it), or export DEEPSEEK_API_KEY in the launching environment","code":"MISSING_CREDENTIAL"}}}}
```

Result: `EXPECTED_FAIL / REAL_PROVIDER_RUNTIME_NOT_CONFIRMED`. The real-provider composition reaches request/credential resolution and persists a terminal error. It does not prove a provider network request, model response, token use or cost.

## 3. Evidence classification and acceptance result

| Question | Observation | Verdict |
|---|---|---|
| Did a repo-owned product headless profile execute? | Direct source contract called `dsh --profile headless` with the repo's deterministic overlay; one Session log was created. | `YES / TEST FIXTURE` |
| Did the trace include Agent/Session/Turn/Step terminal events? | 36 durable rows include one Turn, two Steps, two assistant messages and `turn/end(completed)`. | `YES / RUNTIME_CONFIRMED` |
| Did the fixture complete its advertised local tool round trip? | `tool/call bash` persisted, but Windows base composition returned `UNKNOWN_TOOL`. | `NO / HYPOTHESIS PARTIALLY FALSIFIED` |
| Did the owner snapshot test pass? | Exact single-case run exited 1 on the same `UNKNOWN_TOOL` diff. | `NO` |
| Was a real provider exercised? | No credential was present; independent path exited 1 at `MISSING_CREDENTIAL`. | `NO / CREDENTIAL GAP CONFIRMED` |
| Was network/token/cost behavior proven? | Deterministic `cli-mock` emitted fixed usage metadata; no provider request or charge was observed. | `NO CLAIM` |
| Was host escalation needed? | No command failed for sandbox access; no host-access retry ran. | `NO` |

The central Article 29 dynamic claim is supportable only in this bounded form: at the pinned revision, a repo-owned deterministic overlay can traverse the product headless profile into a persisted Agent/Session Turn and reach a terminal state on this Windows host. The experiment must disclose that the requested `bash` tool was unavailable and that the owner expected-output test failed. This runtime evidence is stronger than source-only inference for the Host-to-Agent chain, but it is not evidence of real-provider success, network behavior, valid token accounting, successful tool execution, production permissions or cost.

Evidence labels for merge:

- `29-C10`: may become `SOURCE_CONFIRMED + TEST_FIXTURE_RUNTIME_CONFIRMED` for the Host/profile-to-terminal-Turn skeleton, with the Windows tool failure attached.
- `29-C11`: must be split. Deterministic product-profile terminal tracing is `CONFIRMED`; successful cross-platform tool round trip is `DISCONFIRMED_ON_WINDOWS`; real-provider runtime remains `NOT_CONFIRMED`.
- `29-C12`: gains an Article 29-local credential-negative observation and remains a strict evidence ceiling.
- `29-E08`, `29-E09`, `29-E10`, `29-E11`: may cite the compressed artifact hashes, complete event-type sequence and raw terminal subset above; they must not cite the expected snapshot as observed success.

Next allowed gate: `EVIDENCE_MERGE` by the Researcher.
