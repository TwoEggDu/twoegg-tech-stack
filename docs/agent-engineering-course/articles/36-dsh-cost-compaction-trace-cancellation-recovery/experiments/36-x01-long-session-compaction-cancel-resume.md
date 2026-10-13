# 36-X01｜Long Session → Compaction → Cancel → Fresh Resume 实验设计

Status: `FROZEN EXECUTABLE DESIGN / NOT EXECUTED`

本文件只把 `research.md §10` 已冻结的问题、假设、反证、acceptance 与安全边界映射到 pinned source 的真实 API。它不包含 Observed Result，不授权修改 acceptance，也不把 owner test、source existence 或预计输出写成 runtime fact。

## 1. 固定对象与预算

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Full commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture root：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Temporary spec：`packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts`
- Integrated harness：`1`
- Focused cases：`1`；该 case 内顺序执行 `P0—P5`，不得拆成互补测试后宣称同一 scenario 闭合。
- Command wall-clock cap：`180s`
- Network / credential / server / real Provider / production write：`ZERO`
- Allowed fixture writes：temporary spec、OS temp 下的 JSONL Session persistence root、OS temp 下的 sentinel receipt；全部必须清理。

## 2. Owner fixtures、imports 与 composition

Temporary spec 只能从 pinned workspace imports 复用下列 production owners / repository-owned testkit；不得复制实现到 Course repository：

| Responsibility | Exact import / owner | Intended use |
|---|---|---|
| Context | `Context` from `@deepseek-ai/cordis` | 创建 first runtime 与 fresh runtime。 |
| Agent prerequisites | `mountAgentLoopTestDependencies` from `@deepseek-ai/dsh-agent-loop-testkit` | 按 LLM → Session → System Prompt → Tools → Agent Registry 挂载 owner services。 |
| Loop | `AgentLoop` from `@deepseek-ai/dsh-agent-loop` | 创建、cancel、dispose、resume 同一 Session。 |
| Session identity/events | `SessionId` and event types from `@deepseek-ai/dsh-session` | 固定 `SessionId('article-36-x01')`，读取 raw durable ledger。 |
| Persistence | `JsonlSessionPersistence` from `@deepseek-ai/dsh-session-persistence-jsonl` | `compression: 'none'` 写入 test-owned OS temp root；fresh runtime 从同一 root 恢复。 |
| Projection | `SessionProjectionRegistry` from `@deepseek-ai/dsh-session-projection` | 读取 `tokenUsage`、`contextPressure` 的 consistent-cut snapshots。 |
| Usage | `TokenMeter` from `@deepseek-ai/dsh-token-meter` | 读取 native usage fold、pressure measurement 与 current-surface price。 |
| Compaction | `BasicCompactionEngine` from `@deepseek-ai/dsh-compaction-basic` | 保留真实 automatic pre-step trigger、region、summary request、replacement；summary由同一deterministic adapter的`purpose='compaction'`分支返回。 |
| Durability policy | namespace import from `@deepseek-ai/dsh-session-checkpoint-policy` | 在 model dispatch、top-level Tool dispatch、pre-step 前调用真实 `sessions.flush`。 |
| LLM types | `LlmAdapter`, `ToolCallId`, `createUserMessage`, `GenerateOptions`, `StreamChunk`, `TokenUsage` from `@deepseek-ai/dsh-llm` | 实现 deterministic adapter 与 provider-shaped usage。 |
| Tool | `defineContentToolFixture`, `PreToolDecision`, `PostToolDecision` from `@deepseek-ai/dsh-tools` | 注册 `known_work` 与 `external_sentinel`，观察 policy/body/result/cancel。 |
| Temp / receipt | `mkdtemp`, `appendFile`, `readFile`, `rm`；`tmpdir`, `join` | 只写 test-owned persistence 与 sentinel journal。 |
| Digest | `createHash` from `node:crypto` | 生成 canonical SHA-256，不把整段 prompt 重复写入 JSONL。 |

### 2.1 两个 runtime 的相同挂载顺序

`mountRuntime(root, adapter, trace)` 必须严格执行：

```text
new Context
-> mountAgentLoopTestDependencies
-> SessionProjectionRegistry
-> AgentLoop({ agents: [] })
-> JsonlSessionPersistence({ root, compression: 'none' })
-> session-checkpoint-policy
-> TokenMeter
-> deterministic adapter registration for provider "article36"
-> tool registrations
-> observation listeners
-> BasicCompactionEngine({
     auto: true,
     thresholdRatio: 0.50,
     retainTokens: 50,
     maxTokens: 64,
     compactionRetries: 0,
     maxOverflowRetries: 0
   })
```

First runtime 用 `ctx.agents.create({ sessionId, agentOptions })` 或等价 public factory 创建 Session；fresh runtime 只能在 first `agent.whenIdle()`、`ctx.sessions.flush(session)`、handle/context dispose 完成后，再用相同 root 与 `ctx.agents.resume({ resumeSessionId: sessionId, agentOptions })` 创建。不得把同一 Context 中的 cache 或 live Agent 当作 fresh resume。

### 2.2 Activation receipt

P0 必须输出一个 `activation` record，至少包含：实际挂载的 plugin/service names、`ctx.get('tokenMeter')`/`ctx.get('compaction')`/`ctx.get('sessionPersistence')` 的存在性、compaction config、adapter provider/model/contextWindow、Session ID、persistence root 的脱敏 basename、checkpoint listener counters。`package exists` 或 YAML row 只作 source evidence；本 record 才是 selected fixture activation evidence。

## 3. Deterministic adapter 与 Tool 脚本

### 3.1 Adapter classification

一个 `Article36Adapter extends LlmAdapter` 记录每个 `GenerateOptions` 的 normalized request。它只处理 provider/model `article36/article36`，`resolveModel` 返回 `contextWindow: 400`。Conversation与compaction summary都穿过这一个adapter；不得override `BasicCompactionEngine.summarize`、委托任何 SDK/HTTP client或调用真实Provider。

请求按 `options.purpose === 'compaction'` 分类；若该字段缺失，只允许用 owner test 已采用的末尾 compaction instruction 检查作为 diagnostic fallback，并在 raw record 标记实际分类依据。每个 request 得到单调 `fixture_request_ordinal`，必须标注 `FIXTURE_INSTRUMENTATION`；pinned source 没有统一 model attempt ID 时，`model_attempt_id` 固定写 `ABSENT_IN_PINNED_CONTRACT`，不得把 ordinal 冒充 production identity。

### 3.2 Conversation script

同一 first runtime 的 adapter 按状态机返回：

1. `LONG_1`：`known_work` tool-call，`callId=36-known-1`，usage `{ inputTokens: 120, outputTokens: 5, cacheReadTokens: 7 }`。
2. `LONG_2`：`known_work` tool-call，`callId=36-known-2`，usage `{ inputTokens: 150, outputTokens: 6, cacheWriteTokens: 9 }`。
3. `LONG_N`：如果 automatic compaction 尚未完成，继续最多两次 `known_work`，分别使用 input `180/200` 与不同 call ID；超过第四次仍无 `compaction/end`，case 必须 fail，而不是直接调用 compaction method。
4. compaction request：返回固定 summary `ARTICLE36 COMPACTED CHECKPOINT`，usage `{ inputTokens: 40, outputTokens: 8 }`，保留 owner bracket/replacement path。
5. first post-compaction conversation request：返回 text `long phase complete`，usage `{ inputTokens: 70, outputTokens: 4 }`，结束第一个 turn；该 request 是 P3 的 after-request。
6. cancellation turn request：返回 `external_sentinel` tool-call，`callId=36-unknown-1`，usage `{ inputTokens: 80, outputTokens: 5 }`。
7. fresh resume 后的下一 request：返回 text `resumed safely without retrying unknown effect`，usage `{ inputTokens: 90, outputTokens: 7 }`；它不得自动再次发出 `36-unknown-1`。

每次 conversation request 的 stream 同时产生 `usage` chunk 与 terminal finish；Session 中的 final assistant message usage 与 chunk source seq 必须由真实 Agent Loop 生成。断言 projection 没有把同一 attempt 的 chunk sample与 final usage重复累计，并单列 compaction-summary usage，不能把 sample 合成为 provider invoice。

### 3.3 Tool 和 external sentinel receipt

- `known_work`：写入 test-owned `sentinel-receipt.jsonl` 的 `INTENT`，立即写 `COMPLETED`，再返回固定 content。分类为 `KNOWN`；其 `tools/pre-execute -> checkpoint flush -> body -> post-execute -> tool/result` 顺序必须被 trace 捕获。
- `external_sentinel`：先写 `INTENT` 并 resolve `bodyEntered` latch；不写 completion，等待 `exec.signal.abort`；观察 abort 后写 `ABORT_OBSERVED` instrumentation receipt并 reject/settle。因为没有 source-defined external reconcile / completion receipt，最终分类必须是 `UNKNOWN`，不是 rolled back、recovered 或 `KNOWN_ABSENT`。
- `agent.cancel({ kind: 'user' })` 只能在 `bodyEntered` resolve 后调用。另设 `bodySettled` latch，terminal 判断必须在它 settle 后读取；不得以 `signal.aborted` 代替 settlement。
- fresh resume 只读取 test-owned receipt 来报告 `UNKNOWN`，不得自动重跑该 tool；resume 后新增 user prompt应让 adapter给出普通 text response。

Policy observer必须是透明 waterfall wrapper：调用 `next()`，保存 actual pre/post decision，不改变 default allow。Checkpoint observer只计数真实 `session/flush`；不得用手写 `ALLOW` 或自造 durable event代替 owner行为。

## 4. 单一 integrated case 与六个 phase

Case ID 固定为 `36-X01-INTEGRATED-01`。

### P0 Baseline

1. 创建 persistence root 与 sentinel receipt path；保存为空/不存在状态。
2. 挂载 first runtime，记录 activation。
3. 创建 `article-36-x01` Session；记录 initial raw event/history/request/projection digests与 initial usage/pressure。
4. 断言没有 network/server/credential adapter、没有生产路径写入。

### P1 Long Session / Usage

1. 发送含 `OLD_HISTORY_SENTINEL` 和稳定重复文本的 first prompt。
2. 让真实 loop执行多个 `known_work` Step；每个 request、usage chunk、assistant final、Tool call/result和 policy/checkpoint receipt写入 correlation JSONL。
3. 每个 Step 后保存 tokenUsage projection、contextPressure projection、`tokenMeter.measure` 与 request digest。
4. 至少两次 conversation attempts后才能进入P2；若 usage字段、source seq或projection fold无法关联则 fail closed。

### P2 Source-defined Compaction

1. 由 automatic `agent/pre-step` listener读取真实 pressure并跨 `0.50 * 400 = 200` threshold；不得调用 `compactNow`、private method或直接 append compaction events。
2. 在 `compaction/start` 前后保存 trigger measurement、current raw events/current surface/derived History/projection digests。
3. 捕获 `compaction/start -> compaction/summary -> compaction/end` 的 exact seq、turn、compactionId、shadowed range/seqs/token count、summary usage/provider/model、end status。
4. 若到第四个long request仍没有真实bracket，case必须失败并保留 trace。

### P3 Before / After Diff

1. `before` 定义为最后一个仍包含 `OLD_HISTORY_SENTINEL` 的 conversation request及其同一cut的 raw/history/surface/projection snapshot。
2. `after` 定义为第一个在 `compaction/end` 之后由真实 next Step 构造的 conversation request及同一cut snapshot。
3. normalized diff必须保存：raw log digest/event count、surface generation/nodes、`deriveMessages()` digest/text sentinels、request header/context/messages/tools digest、tokenUsage/contextPressure snapshot。
4. 必须断言 raw old events仍存在、current request含 compacted checkpoint且不含被shadow的 old sentinel、next request确实变化；semantic equivalence不在 acceptance 内。

### P4 Cancellation / Durable terminal

1. first turn结束后发送 second prompt；adapter返回 `external_sentinel` call。
2. 等 `bodyEntered`，记录 cancel request，再调用 `agent.cancel`。
3. 分别记录 signal observed、body settled、Tool result/error、`step/end`、`turn/end(aborted)` 与 persistence flush完成；每一步用 source identity关联。
4. 读取 sentinel receipt：有 `INTENT`、无 `COMPLETED/RECONCILED`时写 `external_effect_status=UNKNOWN`。不得宣称 hard kill、remote quiescence、rollback或 side effect停止。
5. 保存 durable prefix digest，显式 `sessions.flush`，dispose first handle/context。

### P5 Dispose + Fresh Resume

1. 挂载全新 Context、Projection Registry、TokenMeter、AgentLoop、Persistence、Checkpoint Policy、Compaction和新 adapter instance。
2. `agents.resume` 相同 Session ID；加载后立即保存 raw prefix、History、surface与projection digests。
3. 断言 resume前的 prefix字节/normalized digest不变、seq单调、Session ID相同；记录 tokenUsage/contextPressure 是从 durable events full replay/recompute 得到，除非 raw artifact直接证明使用了projection checkpoint，不能猜测。
4. 发送第三个 user prompt，等待正常 terminal；断言 suffix新增、unknown call未自动重试、next request包含恢复后的 current History。
5. external sentinel仍为 `UNKNOWN`；fresh runtime没有 live promise/signal/subscriber继承。

## 5. Correlation JSONL closed schema

每行必须是一个 JSON object，键完整且不省略：

```text
case_id
phase
runtime_id
record_ordinal
session_id
event_seq
turn
step
model_attempt_id
fixture_request_ordinal
tool_call_id
compaction_id
request_digest
context_digest
history_digest
surface_digest
projection_digest
event_type
usage_native_fields
usage_source
policy_or_trigger_decision
cancellation_origin
cancellation_observed
cancellation_settled
external_effect_status
identity_limitations
```

- 没有对应值时使用 exact sentinel：`N/A` 或 `ABSENT_IN_PINNED_CONTRACT`；禁止删除键。
- `runtime_id`只允许 `FIRST_RUNTIME` / `FRESH_RUNTIME`；`fixture_request_ordinal`必须注明 instrumentation。
- digest算法为 UTF-8 canonical JSON（递归排序object keys、保留array order）上的 SHA-256；AbortSignal、functions、absolute temp root与时间戳不得进入digest。
- `usage_native_fields`原样保留存在字段；optional缺失不补零。
- `external_effect_status`只允许 `NOT_RUN | KNOWN | UNKNOWN`。
- JSONL不得包含 hidden reasoning、credential、私人路径内容或完整大prompt；必要文本只保存stable sentinel与digest。

## 6. Raw capture contract

Lab Engineer 后续应在 Article 36 的 experiment/raw allowlist 中持久化以下材料；本设计 Gate 不创建它们：

```text
experiments/raw/36-x01/
  environment-and-cleanliness.txt
  command.txt
  stdout.txt
  stderr.txt
  exit-code.txt
  correlation.jsonl
  request-before.json
  request-after.json
  history-projection-diff.json
  sentinel-receipt.jsonl
  instrumentation.patch
  instrumentation.sha256
  reproduction-and-limitations.md
  manifest.txt
```

`stdout`只打印phase summaries与最终 assertion count；source-of-truth raw identities在 `correlation.jsonl`。unexpected behavior即使导致exit非零也必须追加到 `reproduction-and-limitations.md`，不得删除失败attempt。

## 7. Exact commands and capture order

所有命令从 fixture root执行。设计冻结的唯一 test command为：

```powershell
pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

执行前后只允许以下只读/清理辅助操作；Lab Engineer必须记录每条exit code：

```powershell
git rev-parse HEAD
git rev-parse 'dsh-v0.1.2-alpha.1^{commit}'
git status --short --branch
git diff --check
node --version
pnpm --version
Get-FileHash -Algorithm SHA256 packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts
git diff --no-index -- /dev/null packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts
```

`git diff --no-index` 对新增文件预期exit `1`，它是instrumentation patch receipt，不是test failure。执行结果必须分别保存 test command 的 stdout/stderr/exit；不能只保存终端摘要。删除temporary spec与test-owned temp roots后再次运行 identity/status/diff检查。fixture最终必须 `HEAD == tag^{commit}` 且 `git status --short`为空；否则36-X01拒绝acceptance。

## 8. Assertions / fail-closed routes

单一case至少断言：

1. selection=`1`，integrated case完成`P0—P5`；
2. conversation attempts≥2，native usage有raw events且same-attempt不double count；
3. pressure跨真实threshold，automatic source listener产生完整compaction bracket；
4. old raw events保留而current surface/history/next request发生有解释的replacement变化；
5. cancel request、signal observation、body settlement、Tool terminal与turn terminal分离；
6. durable prefix flush后first runtime已dispose，fresh Context resume同一Session且prefix不变；
7. `known_work=KNOWN`、cancelled sentinel=`UNKNOWN`，无自动rollback/reconcile/retry；
8. Context/Model/Tool/Policy/Error/Usage能以session+seq+turn/step+现有call/compaction IDs关联；model attempt ID absence显式记录；
9. no real Provider/network/server/credential/production path；
10. cleanup后fixture exact pinned and clean。

任一断言失败、0 selected、observer缺失、temporary patch无法完整保存、cleanup不clean或命令超180s时，Lab Engineer必须返回 `FAILED_REQUIRED_SOURCE_EXPERIMENT` / `BLOCKED_EVIDENCE`，保留失败raw output，并把 Article 36 行为Claim维持blocked。不得追加另一个harness或用互补owner tests拼成accepted integrated trace。

## 9. Explicit limitations

- MockAdapter usage只证明 pinned fold 对provider-shaped samples的处理，不证明真实Provider计费、invoice或字段完整性。
- OS temp JSONL persistence与sentinel只证明selected fixture；不证明production backend、remote system、crash consistency或exactly-once。
- deterministic adapter的compaction分支只提供mock Provider response；它保留真实summary request/trigger/bracket/replacement路径，但不证明真实summary quality、latency或semantic fidelity。
- policy observer记录default decision，不证明side-effect safety、sandbox或用户approval。
- fresh Context近似fresh runtime，不能证明fresh process/host；若实验没有实际process boundary，必须以此limitation收窄。
- absence of universal model attempt/trace ID必须保留；fixture ordinal不能升级为DSH内置identity。
- cancellation terminal不证明external work已停止；UNKNOWN必须持续到独立reconcile owner给出receipt，而pinned core无该通用owner。

## 10. Design Gate disposition

Source Map已提供足够的production/public seams与owner fixtures，可以在不改变`research.md §10` acceptance、不联网、不使用credential、不启动server且只写一个temporary spec的条件下设计36-X01。当前只允许进入`EXPERIMENT_EXECUTE`；runtime status仍为`NOT_EXECUTED`，任何行为Claim仍不得升级。

## Post-failure frozen contract — 2026-09-01, Recovery Cycle 1

### Purpose and immutable failure boundary

This is a launcher recovery only. Attempt 1 remains immutable and permanently
`FAILED_REQUIRED_SOURCE_EXPERIMENT`: focused-command exit=`1`, Vitest did not
start, selection=`0`, and P0—P5 were not observed. Its existing raw directory,
including every receipt and manifest entry, must neither be overwritten nor
rewritten. It supplies no runtime evidence and cannot be combined with this
cycle or with other owner tests to establish any behavioral claim.

The sole changed variable is the launcher chain: prepend the pinned fixture's
`node_modules/.bin` to the current process `PATH`, then use Corepack to run the
repository-declared pnpm. This contract does not alter the hypothesis,
acceptance criteria, safety boundary, harness semantics, or claim ceiling.

### Preconditions — fail closed before any test command

All operations are from the pinned fixture root
`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`, whose required
tag/HEAD is `cd5ef8148158c3a752a658978873241fdf8e2bbc`. Before a test is allowed,
the Lab Engineer must set the current process launcher path only, then verify
all of the following and save the receipts:

1. `corepack pnpm --version` returns exactly `11.7.0` (the repository-declared
   pnpm version). The Corepack launcher is the host `C:\nvm4w\nodejs\corepack.cmd`.
2. `node_modules/.bin/vitest.cmd` exists inside the pinned fixture.
3. The fixture is clean and still pinned: `git rev-parse HEAD`, tag resolution,
   `git status --short`, and `git diff --check` all meet the existing identity
   and cleanliness contract.

If any precondition is not met, the focused command must not run. Record the
failure in the new cycle's raw directory and return
`FAILED_REQUIRED_SOURCE_EXPERIMENT` / `BLOCKED_EVIDENCE` as applicable; do not
install, update, repair the fixture, or take a second launcher attempt.

### Frozen harness and one allowed execution

The archived `experiments/raw/36-x01/instrumentation.ts` is the sole harness
source. Copy it byte-for-byte to the same temporary spec path:

```text
packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts
```

Immediately before execution, verify the archived source SHA-256 exactly equals
`C4AF80CEC0B068F7C54A87777F19751FE90EF2FF28ED3DCB619ADE689276660D`.
The temporary spec must be an unedited byte-for-byte copy; a hash mismatch or
any harness edit is fail-closed and prohibits execution.

After the PATH prepend and successful prechecks, the sole frozen test command
is:

```powershell
corepack pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

It may execute once only, with a 180-second wall-clock cap. No install, update,
network access, server bind, credential read, provider call, alternate
launcher, owner-suite substitute, or second harness is permitted.

### Evidence, acceptance, and cleanup

All new receipts belong only under
`experiments/raw/36-x01/recovery-cycle-1/`; this is a distinct attempt and may
not overwrite Attempt 1. Capture precheck results, the exact command, stdout,
stderr, exit code, all required runtime/raw artifacts, a manifest, and a
reproduction-and-limitations record. On success, acceptance still requires
selection=`1`, every P0—P5 observation, and every pre-existing 36-X01
acceptance assertion. A nonzero exit, zero selection, missing observer,
assertion failure, timeout, failed raw capture, or failed cleanup must still
preserve complete new raw evidence and re-trigger
`FAILED_REQUIRED_SOURCE_EXPERIMENT`; Article 36 behavior claims remain
`BLOCKED_EVIDENCE`.

After capture, remove the temporary spec and only test-owned temporary roots.
Re-run the identity and cleanliness checks so the fixture finishes pinned and
clean. This cleanup requirement is part of acceptance, not evidence of runtime
success.
