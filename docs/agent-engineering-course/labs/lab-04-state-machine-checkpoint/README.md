# Lab 04｜State Machine + Checkpoint

## Metadata

- Lab ID：`lab-04-state-machine-checkpoint`
- Title：`Fake Long-running Investigation：Cancellation、Retry、Checkpoint 与 Recovery`
- Owning Article：`11-long-running-agent`
- Related Article：`Article 11`
- Related Claim IDs：`11-C01`—`11-C08`
- Design Owner：`Researcher`
- Execution Owner：`Lab Engineer`
- Observation Owner：`Lab Engineer`
- Evidence Merge Owner：`Researcher`
- Lifecycle Status：`DESIGN_FROZEN / IMPLEMENTED / LAB_OBSERVATION_COMPLETE / EVIDENCE_MERGED`
- Evidence Status：`CONFIRMED / SCOPED / EVIDENCE_GATE_PASS`
- Runtime / Language：`C# / .NET / net10.0`
- Fixture Version：`lab04-fixture-v1`
- Design Freeze Date：`2026-08-21`（Asia/Shanghai）
- Environment：`.NET SDK 10.0.301 / Host 10.0.9 / Windows 10.0.19045 win-x64 / China Standard Time`
- Provider：`NONE`
- Network / Credentials：`NONE / NONE`
- Last Run：`2026-08-21T13:25:44+08:00`
- Observed Result：`PASS / LR-01—LR-08 observed / run A-B 105 normalized files byte-identical`

> Researcher冻结的Design保持不变；Lab Engineer已在本目录创建source、tests、fixtures与observations。Design中的case、terminal、count与artifact仍是运行前`Expected`；真实`Observed`只记录在后文Observations与其链接的raw artifacts中。

## Goal

用一个BCL-only、deterministic、无Provider / network / credential的`Fake Long-running Investigation`验证以下最小问题：

1. caller cancellation能否在side effect开始前的显式safe checkpoint停止，并由另一个fresh process恢复同一run；
2. transient pre-apply failure能否受Retry Budget约束，且idempotent retry不重复fake external side effect；
3. fake external side effect已应用但response丢失时，checkpoint能否保留in-flight identity，使fresh-process recovery用same identity查询 / 重放，而不是盲目新建；
4. unsafe non-idempotent comparator是否会在lost response后暴露duplicate side-effect risk；
5. checkpoint state表示action已进入、但缺少in-flight action时，resume是否在任何新side effect前fail closed；
6. interrupted / cancelled / exhausted / refused路径是否分别输出known / unknown / unverified / next safe action；
7. 相同fixture在两组formal fresh-process suites中是否生成byte-identical normalized artifacts。

本Lab验证课程Host control-plane与本地fake store，不验证真实远端服务、模型、Provider、MCP、分布式事务、并发调用、crash-consistent filesystem或production reliability。

## Lab Design（Owner：Researcher）

### Related Article

`Article 11｜Long-running Agent：Checkpoint、Retry、Cancellation 与 Recovery`

### Related Claim IDs

`11-C01`、`11-C02`、`11-C03`、`11-C04`、`11-C05`、`11-C06`、`11-C07`、`11-C08`。

### Research Question

当一个长任务在取消、暂态失败或“side effect已发生但响应丢失”窗口中停止时，最小checkpoint必须保存哪些状态，Runtime才能在fresh process中选择Retry、Lookup / Idempotent Replay、Resume或Refuse，而不重复副作用或伪造成功？

### Hypothesis

#### H-01｜Cancellation resumes only from a safe boundary

在`EVIDENCE_COLLECTED` checkpoint之后、`REGISTER_FINDING` side effect之前观察caller cancellation，首次process应提交`CANCELLED / INCOMPLETE`、zero side effects与`next_safe_action=REGISTER_FINDING`；显式resume命令由另一个process加载同一checkpoint后继续，且已完成evidence action不重跑。

#### H-02｜Retry needs identity and budget

同一`action_id + intent_digest + idempotency_key`在一次pre-apply transient failure或apply-then-lost-response后重送时，controlled fake store最多保留一条business effect；trace必须记录attempt、retry decision、budget used与lookup / replay disposition。

#### H-03｜Lost response creates an unknown window

fault在fake store持久化effect之后、Runtime接收result之前终止process时，workflow checkpoint只允许把action标为`STARTED / RESULT_UNKNOWN`；它不能写`NOT_APPLIED`或`SUCCEEDED`。fresh resume必须先以stable identity reconcile，再提交`FINDING_REGISTERED`。

#### H-04｜Blind non-idempotent retry exposes duplicate risk

test-only unsafe comparator若在同一lost-response窗口后使用新delivery identity进行append，应在fake store产生两条business effects并以`DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`停止。这个negative case是风险证据，不是推荐实现。

#### H-05｜Missing in-flight action fails closed

若checkpoint的state=`REGISTERING_FINDING`，却没有required `in_flight_action`，resume validator必须在fake store call count增加前返回`RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`；不得猜测action未执行、不得重试、不得伪成功。

#### H-06｜Partial result preserves uncertainty

所有非成功terminal都必须分别输出accepted known references、unknown action identities、unverified Goal requirements与next safe action / `NONE`。字段来源必须可追到checkpoint / trace，不可从expected matrix复制。

#### H-07｜Normalized artifacts are reproducible

formal suite A与B使用相同binary / fixture、不同Lab-owned run roots，并让每个start / resume phase进入独立OS process；两组normalized checkpoint、trace、case-result、partial-result与fake-store view应逐文件byte-identical。

### What Would Falsify It

任一项即否定对应Hypothesis，并使相关Claim保持`BLOCKED / PARTIAL`：

- cancellation在side effect之后才被记录，却被写成“safe pre-effect cancel”；
- resume重新执行已committed evidence action，或使用新的run identity；
- same idempotency key / same intent产生第二条controlled business effect；
- lost response被记录为`NOT_APPLIED`或直接`SUCCEEDED`；
- unsafe comparator没有真实产生duplicate，却由test手写duplicate flag；
- missing in-flight checkpoint在拒绝前调用fake store；
- retry次数超过budget，或Retry Budget耗尽后仍继续执行；
- non-success partial result把unknown写入known、漏掉unverified requirement或给出不安全next action；
- start与resume发生在同一process；
- Runtime读取README、test assertion、expected terminal / count / hash；
- normalized artifact包含wall-clock、PID、absolute path、random GUID，或run A / B hash不同；
- Lab Engineer为了PASS修改本Design、case matrix或acceptance criteria。

### Fixture Boundary

`Fake Long-running Investigation v1`只调查一个冻结Goal：

```text
Read fixture diagnostic
  -> register one fake finding in a Lab-owned external store
  -> verify finding identity
  -> produce supported completion
```

“external store”是Lab目录下、独立于workflow checkpoint的本地JSON artifact。它模拟“业务副作用存储与workflow checkpoint不是同一个transaction”，但不连接真实database或网络。controlled mode以stable idempotency key实现`CreateOrGet`；unsafe comparator以append-only new delivery模拟非幂等create。

Scripted control source只选择冻结action与fault seam，不调用模型。它不能写checkpoint、fake store、authoritative State或terminal outcome。

### Environment

| Item | Frozen requirement |
|---|---|
| Language / TFM | C# / `net10.0` |
| SDK | .NET 10 SDK；exact patch由Lab Engineer执行前记录并冻结；环境不匹配则停止，不静默降级 |
| Dependencies | .NET BCL only；zero third-party runtime / test packages |
| Package sources | `NuGet.Config`必须`<clear />`；restore不得访问network |
| OS | 当前Windows execution host；exact edition / build / architecture记录到execution log |
| Provider / model | none / none |
| Network / credentials | disabled / not needed；任何network attempt使Lab FAIL |
| Concurrency | single coordinator；不验证concurrent callers |
| Encoding | normalized artifacts为UTF-8 without BOM + LF，canonical property order |

Lab Engineer必须保存`dotnet --info`、OS、timezone、commands、exit codes与process IDs到非归一化execution log。PID / wall-clock只用于证明fresh process，不得进入normalized evidence artifacts。

### Inputs

未来`fixtures/cases.json`只允许包含：

- `fixture_version`、`case_id`、`goal_contract_id`；
- deterministic `run_id` / `action_id` / `idempotency_key` seeds；
- scripted action inputs；
- named fault ID与fault boundary；
- retry policy（max attempts、retryable fault code）；
- effect mode（`CONTROLLED_CREATE_OR_GET`或`UNSAFE_APPEND_COMPARATOR`）。

它禁止包含：

- expected terminal reason / outcome / success；
- expected retry count、effect count、trace rows或hash；
- expected known / unknown / unverified / next safe action；
- assertion result或Claim Status。

Expected只存在于本README和独立Spec / verifier code。Runtime project不得reference tests project，不得读取README、test binary或expected data。Spec只能在Runtime process结束后读取其artifacts并独立判定。

### Variables

| Variable | Frozen values / rule |
|---|---|
| cancellation origin | `NONE / CALLER / TIMEOUT` |
| fault boundary | `NONE / BEFORE_APPLY / AFTER_APPLY_BEFORE_RESPONSE` |
| fault cardinality | `ONCE / ALWAYS` |
| effect policy | `CONTROLLED_CREATE_OR_GET / UNSAFE_APPEND_COMPARATOR` |
| retry max attempts | case-local fixed integer；attempt 1计入used |
| checkpoint completeness | `VALID / OMIT_IN_FLIGHT_ACTION` |
| process phase | `START / RESUME`；每个phase必须fresh process |
| run root | unique Lab-owned root；不进入normalized artifact |

### Expected Observable

> 本节全部是运行前Expected。`Observed Result = NONE`。

| Case | Start-phase fault / action | Expected start terminal | Resume behavior | Expected final / effect count |
|---|---|---|---|---|
| `LR-01` baseline | none | `SUCCEEDED / GOAL_SATISFIED` | N/A | success；controlled effect=`1`；retry used=`0` |
| `LR-02` cancel + resume | caller cancel after`EVIDENCE_COLLECTED` checkpoint、before side effect | `CANCELLED / INCOMPLETE`；effect=`0` | fresh process resumes at`REGISTER_FINDING`；evidence action not rerun | `SUCCEEDED`；effect=`1`；resume count=`1` |
| `LR-03` transient retry | `TRANSIENT_BEFORE_APPLY_ONCE` | same process classifies retryable failure and retries within budget | N/A | `SUCCEEDED`；attempts=`2`；effect=`1` |
| `LR-04` lost response + idempotent recovery | controlled store applies effect, then process exits before response / action commit | `INTERRUPTED / UNKNOWN_SIDE_EFFECT`；pre-call checkpoint has in-flight identity；effect=`1` | fresh process loads same identity，`CreateOrGet / lookup` returns existing effect | `SUCCEEDED`；effect remains=`1`；no duplicate |
| `LR-05` unsafe comparator | unsafe append applies effect, response lost | `INTERRUPTED / UNKNOWN_SIDE_EFFECT`；effect=`1` | fresh process blindly redelivers with new delivery identity | `DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`；effect=`2` |
| `LR-06` missing in-flight action | controlled effect applies, response lost；test seam writes state=`REGISTERING_FINDING` but omits in-flight action | `INTERRUPTED / INVALID_CHECKPOINT_CANDIDATE`；effect=`1` | fresh process validates before fake store access | `RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`；effect remains=`1` |
| `LR-07` retry exhausted | `TRANSIENT_BEFORE_APPLY_ALWAYS`；max attempts=`2` | `RETRY_BUDGET_EXHAUSTED / INCOMPLETE` | no automatic resume candidate | effect=`0`；known evidence preserved；registration unverified；next=`ASK_OR_STOP` |
| `LR-08` timeout classification | deterministic timeout signal before side effect | `TIMED_OUT / INCOMPLETE`；origin=`TIMEOUT` | no automatic resume in this case | effect=`0`；must not equalcaller cancellation trace |

Required normalized artifacts per case：

- `checkpoint.json`或明确的checkpoint-invalid artifact；
- `trace.jsonl`；
- `fake-store-view.json`；
- `partial-result.json`；
- `case-result.json`；
- `artifact-manifest.json`（relative path、byte count、SHA-256）。

### Fault Injection

| Fault ID | Injection point | Required property |
|---|---|---|
| `FI_CANCEL_AFTER_SAFE_CHECKPOINT` | checkpoint flush after`EVIDENCE_COLLECTED`、before in-flight action write | cooperative token observed；zero side effect |
| `FI_TRANSIENT_BEFORE_APPLY_ONCE` | fake store before mutation，first attempt only | retryable；effect absent is known |
| `FI_APPLY_THEN_LOSE_RESPONSE` | fake store durable mutation after write、before result delivery | process exits / returns named interruption；effect exists；workflow result unknown |
| `FI_UNSAFE_BLIND_REDELIVERY` | LR-05 resume only | new delivery append creates controlled duplicate risk |
| `FI_OMIT_IN_FLIGHT_ACTION` | test-only checkpoint serializer after state enters`REGISTERING_FINDING` | integrity is recomputed, but state invariant is invalid；validator must reportmissing field |
| `FI_TRANSIENT_BEFORE_APPLY_ALWAYS` | every attempt before mutation | budget exhausts exactly at frozen max |
| `FI_TIMEOUT_BEFORE_APPLY` | deterministic timeout source before mutation；nowall-clock sleep | origin remains`TIMEOUT`，not`CALLER` |

Faults must be named deterministic seams. No timing race、randomness、network failure、process kill utility或real external mutation可替代它们。`AFTER_APPLY_BEFORE_RESPONSE`必须先flush Lab-owned fake store，然后才触发 interruption；否则不能支持unknown-side-effect问题。

### Commands / Execution Needs

以下是后续Lab Engineer必须实现的command surface；本Gate禁止执行：

```powershell
dotnet --info
dotnet restore .\LongRunningAgentLab.slnx --locked-mode --configfile .\NuGet.Config
dotnet build .\LongRunningAgentLab.slnx -c Release --no-restore
dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll static-contract
dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll formal-suite --suite run-a --output .\observations\run-a
dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll formal-suite --suite run-b --output .\observations\run-b
dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll compare --left .\observations\run-a --right .\observations\run-b
```

`formal-suite` verifier必须通过`System.Diagnostics.Process`启动Runtime CLI；每个`START`和`RESUME` phase都是新的child process。execution log记录command、PID、exit code；normalized artifact只记录`process_phase`。Verifier内置Expected，不把Expected作为Runtime argument或fixture file。

冻结CLI exit code contract：

| Exit | Meaning |
|---:|---|
| `0` | phase / case completed as a successful Runtime terminal |
| `10` | cooperative caller cancellation saved at safe boundary |
| `11` | named interruption after apply / before response |
| `12` | recovery refused by checkpoint validation |
| `13` | retry budget exhausted |
| `14` | unsafe comparator duplicate detected |
| other | unexpected Runtime / environment failure；Lab cannot pass |

Exact first-run command history、failed attempt、patch与accepted rerun都必须保存在`observations/execution-log.md`。Lab Engineer不得只留下最终green command。

### Acceptance Criteria

Lab 04只有全部满足才可返回`LAB_OBSERVATION` complete：

1. `dotnet restore`、Release build与static-contract verifier真实执行；build exit=`0`且`0 warnings / 0 errors`。
2. source、tests、fixtures、observations来自Lab Engineer；Researcher frozen Design未被修改。
3. Runtime binary无法读取README、tests assembly或任何Expected terminal / count / hash。
4. LR-01—LR-08全部执行；expected negative terminal也必须由raw trace与fake store支持，不能手写result。
5. LR-02的START / RESUME为不同PIDs；resume前effect count=`0`，final=`1`，completed evidence action不重跑。
6. LR-03 attempts精确=`2`、budget accounting一致、effect=`1`。
7. LR-04在lost response后effect=`1`且checkpoint为`RESULT_UNKNOWN`；resume使用sameaction identity / key，final effect仍=`1`。
8. LR-05真实fake store在unsafe comparator中出现两条business effects，且case必须失败；不得把该路径算为推荐Runtime PASS。
9. LR-06 resume validator在fake store access count增加前返回`IN_FLIGHT_ACTION_MISSING`；effect保持=`1`。
10. LR-07 attempts精确达到max后停止，effect=`0`；partial result分别保留known / unverified / next safe action。
11. LR-08无sleep即可稳定形成`TIMEOUT` origin，effect=`0`；不得与LR-02 caller cancellation共用同一origin。
12. 每条non-success case的partial result均有provenance，unknown与unverified不进入known。
13. 每个resume phase来自fresh process；execution log保留PID证据，normalized artifacts不含PID。
14. run A / B使用不同validated Lab-owned roots；normalized artifact清单、bytes与SHA-256逐文件相同。
15. network / Provider / credential access=`0`；任何network attempt、外部路径写入或未声明side effect使Lab FAIL。
16. 所有unexpected failure、first failure与limitations被保留，不为文章thesis改Expected。

### Evidence Mapping

| Claim | Experiment / cases | Required raw evidence | Possible post-merge ceiling |
|---|---|---|---|
| `11-C01` | LR-02、LR-03、LR-08 | distinct cancellation / retry / timeout events、origin、terminal | `CONFIRMED / FIXTURE-SCOPED` if observed |
| `11-C02` | LR-02、LR-04、LR-06 | checkpoint readback、state revision、completed / remaining / in-flight / budget / continuation | `CONFIRMED / PROPOSAL-CONFORMANCE` |
| `11-C03` | LR-03、LR-04、LR-07 | attempts、budget、effect counts、same intent identity | `CONFIRMED / FIXED-STORE-SCOPED` |
| `11-C04` | LR-04、LR-05 | apply-before-response trace、store before / after、duplicate comparator | `CONFIRMED / FIXED-STORE-SCOPED` |
| `11-C05` | LR-02 | cancel request / observed trace、safe checkpoint、fresh resume | `CONFIRMED / FIXTURE-SCOPED` |
| `11-C06` | LR-02、LR-04 | process boundary、checkpoint load、completed action not rerun、same run ID | `CONFIRMED / COURSE-RUNTIME-SCOPED` |
| `11-C07` | LR-02、LR-04—LR-08 | partial-result JSON + provenance refs | `CONFIRMED / COURSE-SCHEMA-CONFORMANCE` |
| `11-C08` | LR-06 + run A / B compare | pre-effect refusal、access count、artifact hashes | `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED` |

即使全部通过，Claim ceiling也不能超过表中scope；不得升级为production、distributed、cross-platform或exactly-once保证。

### Limitations

- fake store与checkpoint同机、不同文件，但不是独立database / service或真实transaction boundary；
- named interruption不是OS crash、power loss、partial disk write或process tree kill；
- single coordinator，不覆盖concurrent duplicate delivery、lock、race、lease或split-brain；
- idempotency registry不覆盖expiration、eviction、multi-tenant isolation、security或unbounded growth；
- `net10.0` / current Windows host结果不外推其他TFM / OS / filesystem；
- no Provider / model，因此不验证decision quality、context reconstruction或model determinism；
- no network / credential，因此不验证真实HTTP retry、remote query、authorization或rate limit；
- byte-identical只证明frozen normalized artifacts，不是性能、可用性或production reliability指标；
- checkpoint schema是course proposal，不是LangGraph、AWS、Temporal或其他产品schema。

### Safety / Permission Constraints

- 只读repository fixtures；所有Runtime writes必须位于Lab-owned、预验证的unique run root；
- fake side effect只能写fake store，禁止访问用户文件、registry、service、database、cloud或HTTP；
- no shell tool、no child process except verifier启动本Lab Runtime CLI；
- no credential / environment secret读取；execution log不得记录环境变量值；
- cleanup前必须验证absolute parent、Lab prefix、sentinel、not-parent、not-reparse-point；验证失败时保留目录并停止，禁止recursive delete；
- 不使用wall-clock sleep制造fault；不进行危险process termination；
- 不修改Article Draft、Published Content、global state或本frozen Design；
- 任何环境或权限不满足时返回`FAILED_LAB`，不得降级判据。

## Prerequisites

- .NET 10 SDK与`net10.0` target；exact version由Lab Engineer执行时记录；
- BCL-only solution、offline `NuGet.Config`、locked restore inputs；
- independent Runtime CLI与Spec verifier projects；Runtime不得reference tests；
- Lab-owned unique roots、sentinel与fake store；
- `fixtures/cases.json`不含expected answers；
- no model / Provider / network / credentials。

当前Prerequisite状态：`PASS / .NET SDK 10.0.301 / BCL-only / offline locked restore / isolated Runtime and Spec verifier / no Provider or credential`。

## Question and Claims

| Claim ID | 可判定问题 | 成功判据 | 失败判据 |
|---|---|---|---|
| `11-C01 / C05` | caller cancellation是否在safe boundary停止并fresh-process resume？ | LR-02 trace、effect count与process evidence全部满足 | cancel origin丢失、side effect先发生、same-process伪resume |
| `11-C03 / C04` | retry / lost response是否按identity与effect semantics处理？ | LR-03 / 04 effect=`1`；LR-05受控duplicate=`2`并失败 | blind retry被涂绿、same key duplicate、risk未真实出现 |
| `11-C02 / C08` | checkpoint是否足以判定continuation / in-flight？ | valid cases恢复；LR-06在store access前拒绝 | missing field仍调用store或伪成功 |
| `11-C07` | partial result是否诚实保留不确定性？ | known / unknown / unverified / next safe action逐项可追踪 | unknown进入known、未验证被省略、安全动作被猜测 |
| `11-C08` | fresh-process normalized artifact是否可复现？ | run A / B逐文件byte-identical | PID / time / path污染或hash mismatch |

## Fixture

### Frozen state machine

```text
INTAKE
  -> EVIDENCE_COLLECTED
  -> REGISTERING_FINDING
  -> FINDING_REGISTERED
  -> VERIFIED
  -> SUCCEEDED

control terminals / resumable boundaries:
  CANCELLED_INCOMPLETE
  INTERRUPTED_UNKNOWN_SIDE_EFFECT
  RETRY_BUDGET_EXHAUSTED
  RECOVERY_REFUSED
  DUPLICATE_SIDE_EFFECT_DETECTED
```

Legal guard summary：

- `INTAKE -> EVIDENCE_COLLECTED`：fixture diagnostic accepted；
- `EVIDENCE_COLLECTED -> REGISTERING_FINDING`：cancellation / timeout clear，budget available，in-flight identity written and checkpoint flushed；
- `REGISTERING_FINDING -> FINDING_REGISTERED`：fake store result reconciled tosame action identity；
- `FINDING_REGISTERED -> VERIFIED`：accepted store record identity / payload digest match；
- `VERIFIED -> SUCCEEDED`：Goal、output、Evidence与unknown-action contracts全部PASS；
- any recovery candidate：checkpoint schema、integrity、state / in-flight invariant与fixture version必须先PASS。

### Frozen checkpoint schema candidate

```text
schema_version
fixture_version
run_id / case_id / goal_contract_id
state / state_revision / last_committed_sequence
completed_actions[] { action_id, intent_digest, result_ref, evidence_refs[] }
remaining_actions[]
in_flight_action | null {
  action_id, intent_digest, idempotency_key,
  phase, attempt, result_status
}
retry_budget { max_attempts, attempts_used, remaining }
last_failure { class, code, retryable }
cancellation { requested, observed, origin }
continuation { resume_state, next_safe_action }
partial_result { known_refs[], unknown_actions[], unverified_requirements[], next_safe_action }
integrity { canonical_payload_sha256 }
```

State invariant：`REGISTERING_FINDING`必须有non-null in-flight action；`FINDING_REGISTERED`必须有matching completed action / fake-store result ref；`SUCCEEDED`必须没有unknown action与unverified Goal requirement。

### Planned implementation layout（not created by Researcher）

```text
lab-04-state-machine-checkpoint/
├── README.md
├── LongRunningAgentLab.slnx
├── global.json
├── NuGet.Config
├── src/LongRunningAgentLab/
├── tests/LongRunningAgentLab.Specs/
├── fixtures/cases.json
└── observations/
    ├── execution-log.md
    ├── run-a/
    └── run-b/
```

当前除README外全部`ABSENT / EXPECTED TO BE CREATED BY LAB ENGINEER`。

## Run Instructions

按“Commands / Execution Needs”执行。必须先环境记录与offline restore，再Release build、static contract、formal run A、formal run B、independent compare。不得跳过negative case、fresh-process resume或first-failure记录。

本轮Run状态：`PASS / restore + Release build + static-contract + formal run-a + formal run-b + compare all executed`。

## Observations（Owner：Lab Engineer）

- Environment：`.NET SDK 10.0.301；Host 10.0.9；Windows 10.0.19045 win-x64 / X64；China Standard Time；详见 observations/environment.md 与 dotnet-info.txt`
- Commands：`全部七条frozen commands真实执行；first failures、patch与rerun见 observations/execution-log.md`
- Exit Codes：`restore=0；first build=1 (CS5001 TDD red)；accepted build=0；first static=1 (generated-source false positive)；accepted static=0；run-a=0；run-b=0；compare=0`
- Build Result：`PASS；Release build 0 warnings / 0 errors`
- Test Result：`PASS；static-contract + LR-01—LR-08 in both formal suites + independent compare`
- Runtime Output：`run-a与run-b各12个fresh Runtime child processes；machine-readable PID / exit / stdout / stderr见 process-evidence-run-a.json与process-evidence-run-b.json`
- Fault Injection Result：`PASS；caller cancel、transient-once、apply-then-lost-response、unsafe blind redelivery、missing in-flight、transient-always与timeout全部命中frozen seam和terminal`
- Observed Behavior：`LR-02 pre-effect cancel后effect 0且fresh resume后1；LR-03 attempts 2/effect 1；LR-04 RESULT_UNKNOWN后same identity恢复且effect仍1；LR-05真实两条store records并FAILED；LR-06 store access count保持1；LR-07 attempts 2/effect 0；LR-08 TIMEOUT/effect 0；non-success partial results均带provenance`
- Unexpected Behavior：`CIM OS probe Access denied；首次static verifier递归扫描SDK generated GlobalUsings导致false positive。两者均保留，后者经最小patch后由assembly reference check与authored-source check重新通过`
- Reproduction Notes：`按frozen commands顺序运行；run root已有内容时只在direct observations child、exact suite sentinel、非reparse验证后清理。run A/B compare=105 files，aggregate SHA-256 27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a`
- Runtime Limitations：`仅当前Windows/.NET、本地双文件边界、deterministic named interruption与single coordinator；不外推OS crash、并发、distributed transaction或production reliability`

| Run | Input | Raw Output / Trace | Result |
|---|---|---|---|
| `run-a` | `fixtures/cases.json / LR-01—LR-08` | `observations/run-a/**；observations/process-evidence-run-a.json` | `PASS / 8 cases / 12 child processes` |
| `run-b` | `same binary + same fixture / distinct validated root` | `observations/run-b/**；observations/process-evidence-run-b.json` | `PASS / 8 cases / 12 child processes` |
| `compare` | `run-a vs run-b normalized roots` | `observations/execution-log.md；observations/verification-summary.json` | `PASS / 105 files byte-identical` |

Lab Engineer必须在这里或明确链接的observation artifact记录真实结果；不得把Expected复制进本节。

## Expected Failure Paths

- 无效输入：fixture version、case ID、state transition或intent digest不合法时，side effect前`INPUT_OR_STATE_INVALID`。
- Provider / Tool失败：Provider不存在；fake store permanent failure不得retry，transient failure只按frozen budget处理。
- 超时或取消：保留`TIMEOUT`与`CALLER` origin；request不证明work stopped；本Lab只在pre-effect deterministic boundary注入。
- 结构化输出不满足合同：checkpoint / partial result / case result schema invalid即case FAIL，不进入resume或success。
- 预算耗尽：`RETRY_BUDGET_EXHAUSTED / INCOMPLETE`，不得再调用fake store。
- 响应丢失：effect可能已发生，必须标`UNKNOWN_SIDE_EFFECT`并使用same identity reconcile；不得假设未应用。
- 重复副作用：controlled path发现same-key duplicate即FAIL；unsafe comparator必须保留两条record并显式FAILED。
- checkpoint缺少in-flight action：`RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING`，fake store access count不增加。
- checkpoint integrity / fixture version mismatch：`RECOVERY_REFUSED`，不得自动迁移或猜测。

## Interpretation / Evidence Merge（Owner：Researcher）

`COMPLETE / EVIDENCE_MERGED`。Researcher未修改frozen Design、Lab Engineer Observations或raw output。

| Claim | Experiment -> Observation -> Evidence Interpretation -> Claim Status |
|---|---|
| `11-C01` | LR-02 / 03 / 08分别注入caller cancel / transient retry / timeout -> trace分开event、origin与terminal -> 这些是不同控制事实，不能由单一FAILED / CANCELLED互推 -> `CONFIRMED / FIXTURE-SCOPED`。 |
| `11-C02` | LR-02 / 04 valid checkpoint与LR-06 omitted in-flight对照 -> valid cases依据run / state / continuation / completed / remaining / budget / in-flight恢复，LR-06在store access不变时refuse -> current State JSON不足以处理in-flight uncertainty，字段表只是course candidate -> `CONFIRMED / PROPOSAL-CONFORMANCE`。 |
| `11-C03` | LR-03 / 04 / 07对照pre-apply、same-identity unknown recovery和budget exhaustion -> effects分别`1 / 1 / 0`，attempts受budget限制 -> Retry eligibility先由effect semantics / identity决定，budget不替代幂等性 -> `CONFIRMED / FIXED-STORE-SCOPED`。 |
| `11-C04` | LR-04 controlled path对照LR-05 unsafe comparator -> 两者均先有effect=1 + `UNKNOWN_SIDE_EFFECT`；controlled resume仍1，unsafe new delivery为2并FAILED -> lost response不等于未执行，LR-05是blind retry的negative evidence -> `CONFIRMED / FIXED-STORE-SCOPED`。 |
| `11-C05` | LR-02在committed evidence后、effect前cancel再fresh resume -> START effect=0，RESUME不同PID且final effect=1，committed evidence不重跑 -> 只能声称pre-effect cooperative safe-boundary recovery，不能推导mid-I/O stop或rollback -> `CONFIRMED / FIXTURE-SCOPED`。 |
| `11-C06` | LR-02 / 04 process evidence + checkpoint trace -> START / RESUME不同PID，同same run identity加载checkpoint，可能重执行boundary后store call -> Resume从定义boundary继续，Replay可为其实现机制但不自动安全 -> `CONFIRMED / COURSE-RUNTIME-SCOPED`。 |
| `11-C07` | 读取LR-02、04—08的non-success partial-result、checkpoint与trace -> known / unknown / unverified / next safe action分开并带provenance -> fixed cases没有把中断伪成success，但schema仍是course proposal -> `CONFIRMED / COURSE-SCHEMA-CONFORMANCE`。 |
| `11-C08` | LR-06 fail-closed + run A/B compare -> refusal前后store access=1；每suite 12个fresh process phases；105 normalized files byte-identical，不同suite sentinel未纳入compare -> 证明frozen fixture可复现，不是OS-crash / cross-platform / production determinism -> `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED`。 |
| `11-C09` | 对照current LangGraph Persistence的checkpointer memory / fault-tolerance / Store语义 -> 同一persistence capability可同时服务memory与recovery -> 课程按证明职责区分Checkpoint / Memory，不建立物理互斥taxonomy -> `CONFIRMED / PRODUCT-DOC-SCOPED`。 |

### Proves

- 在`lab04-fixture-v1`、当前Windows / .NET Host和single coordinator下，课程Runtime可按origin、budget、stable identity、checkpoint invariant与effect semantics选择Stop / Retry / Reconcile / Resume / Refuse。
- partial-result course schema能为fixed terminal保留known / unknown / unverified / next-safe-action provenance。
- LR-05两条真实fake-store record是unsafe blind retry的negative evidence；LR-06是missing identity应fail closed的negative evidence。

### Does Not Prove / Limitations

- named interruption不是OS crash、power loss、partial disk write或process-tree kill。
- fake store与checkpoint是同机不同文件，不是independent service或distributed transaction；idempotency不是exactly-once。
- single coordinator不覆盖concurrent delivery、lock、race、lease或split-brain。
- no network / Provider / credential意味着未验证真实HTTP / remote store、模型质量、授权、限流或上下文重建。
- CIM OS-edition probe的`Access denied`被保留；Windows build / X64只由`dotnet --info`与`RuntimeInformation`支持。
- 105 files byte-identical只证明 frozen normalized artifact reproducibility，不是performance、availability、production reliability或cross-platform determinism。

### Course wording and Article 12 stop line

课程可以使用上表的fixture / proposal / course-runtime / course-schema / product-doc限定表述，但禁用`production-proven`、`distributed-safe`、`crash-safe`、`exactly-once`或“有checkpoint就能安全恢复”。Article 11在recovery control plane停止；跨run Memory、context选择/重建/质量与模型决策确定性属于Article 12，不由Lab 04推出。

## Conclusion

- Confirmed：`C01—C09全部完成Claim Status裁决；0核心BLOCKED；9/9 traceability`
- Lab status：`OBSERVED / EVIDENCE_MERGED`
- Evidence Gate：`PASS`
- Evidence ceiling：`fixture / fixed-store / proposal / course-runtime / course-schema / product-doc scoped only`
- Blocked：`NONE`（production、distributed、OS-crash、concurrent、network / Provider与cross-platform仍为non-scope，不是被证明的结论）
- Follow-up：`OUTLINE`

## Limitations

见Design中的Limitations。任何未来Conclusion都必须连同exact runtime、fixture version、case matrix、fresh-process证据和unexpected failures一起使用。

## Evidence Links

- Evidence Card：`docs/agent-engineering-course/articles/11-long-running-agent/evidence.md`
- Research Register：`docs/agent-engineering-course/articles/11-long-running-agent/research.md`
- Raw trace / terminal / checkpoint / partial / fake-store：`observations/run-a/LR-01—LR-08/**；observations/run-b/LR-01—LR-08/**`
- Execution / fresh-process proof：`observations/execution-log.md；observations/process-evidence-run-a.json；observations/process-evidence-run-b.json`
- Environment / summary：`observations/dotnet-info.txt；observations/environment.md；observations/verification-summary.json`
- Source / Specs / Fixture：`src/LongRunningAgentLab/；tests/LongRunningAgentLab.Specs/；fixtures/cases.json`
- Claim merge：`11-C01—C09 -> Article 11 evidence.md / Per-Claim Evidence Merge`
- Article section：`Article 11 / NOT DRAFTED / NEXT OUTLINE`
