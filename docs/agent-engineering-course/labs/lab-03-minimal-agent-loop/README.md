# Lab 03｜Minimal Agent Loop

- Lab ID：`lab-03-minimal-agent-loop`
- Related Article：`08-agent-loop`
- Design Owner：`Researcher`
- Execution Owner：`Lab Engineer`
- Observation Owner：`Lab Engineer`
- Evidence Merge Owner：`Researcher`
- Status：`DESIGN_FROZEN / IMPLEMENTED / VERIFIED / EVIDENCE_MERGED`
- Design Freeze Date：`2026-08-20`（Asia/Shanghai）
- Evidence Gate：`PASS / 6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Provider：`NONE`
- Decision Source：`ScriptedDecisionSource v1`（deterministic substitute）
- Network / Credentials：`NONE / NONE`
- Safety：`READ-ONLY FIXTURES / NO EXTERNAL SIDE EFFECTS`

> 本文件同时保留运行前冻结的 Lab Card / Design 与后续追加的执行、Observation 和 Evidence Merge 记录。目录中的 `src/`、`tests/`、`fixtures/` 与 `observations/` 已存在；Design / Expected 继续作为运行前判据，不得用 Observed 反向改写，raw artifacts 也不得在本次 reconciliation 中重生成或覆盖。

## 1. Lab Goal

用一个 BCL-only、deterministic、无 Provider 的最小 Host loop，产生可审计的 `Decide -> Act -> Tool Outcome -> Observation -> State Update -> Stop` 轨迹，回答：

1. Tool Outcome 是否必须经过 correlation / normalization 才成为本文 Observation；
2. authoritative state 是否只由 Host reducer 提交；
3. `REQUEST_STOP` 是否只是 candidate，而不是 success；
4. success、unresolved tool failure、max-step exhaustion、repeat/no-progress + pseudo-completion 能否留下不同 terminal record；
5. 两个 fresh processes 是否生成 byte-identical normalized artifacts。

本 Lab 只验证 Host control-plane。它不验证真实模型会选对工具、会恢复、会停止，也不验证 Provider、MCP server、权限系统或外部副作用。

## 2. Hypotheses and falsifiers

### H-01｜Result-to-state chain is explicit

**Hypothesis**：每次 `ACT` 最多执行一个 fixture tool；Tool Outcome 被 correlation 后正规化为 Observation；只有 Host reducer 能提交 state revision。failed result 也可得到 `normalization_status=PASS` 的 `TOOL_FAILURE` Observation，但不能成为成功 Evidence。

**Falsifier**（任一即否定）：

- Tool 直接改 authoritative state；
- Tool Outcome 没有 correlation/digest 就进入下一 Decision；
- failed Tool Outcome 被丢弃，或被记录成 success Observation；
- 一个 committed Step 写入零次或多次 state revision；
- Observation 的 source-result digest 无法回指 raw Tool Outcome。

### H-02｜Stopped is not succeeded

**Hypothesis**：所有四个 case 都以 `lifecycle=STOPPED` 结束，但只有同时通过 Goal + Output + Evidence + unresolved-failure contracts 的 AL-01 得到 `outcome=SUCCEEDED`。

**Falsifier**（任一即否定）：

- `requested_outcome=SUCCEEDED` 被直接复制成 run outcome；
- unresolved tool failure、missing required evidence、fake evidence 或 max-step termination 被标成 success；
- termination reason 与 outcome 合并成一个无法区分 limit/failure/success 的枚举；
- terminal record 缺 explicit success-contract status。

### H-03｜Repeat and limit are independently observable

**Hypothesis**：canonical action fingerprint 不含 invocation ID；重复 read 的 invocation ID 不同但 fingerprint 相同。第二次重复只增长 history/full-state digest，不改变 goal-state digest，因此 `progress=NO_PROGRESS`。max-step gate 在任何第 `N+1` 次 Decision / Tool 调用前生效。

**Falsifier**（任一即否定）：

- fingerprint 含 invocation ID，导致相同 action 永远不重复；
- 只使用 full-state digest，把“追加一条历史”误判为 goal progress；
- AL-03 的第三条 scripted Decision 被消费；
- AL-03 在第三次调用之后才检查 limit。

### H-04｜Artifacts are reproducible

**Hypothesis**：同一 build、fixture 与 input 在两个 fresh processes 中产生 byte-identical normalized trace、state snapshot 与 case-result files。

**Falsifier**：除 process execution log 之外，normalized artifacts 包含 wall-clock time、absolute path、random ID、process ID 或其他不稳定字段，或两次 hash 不一致。

## 3. Frozen technical scope

### 3.1 Environment target

| Item | Frozen target |
|---|---|
| OS | Windows 10 `10.0.19045`, `win-x64` |
| .NET SDK | `10.0.301` |
| .NET Host / Runtime | `10.0.9` |
| Target Framework | `net10.0` |
| Dependencies | .NET BCL only；zero NuGet runtime dependencies |
| Network | disabled / not needed |
| Provider | none |
| Encoding / newline | UTF-8 without BOM / LF for normalized artifacts |

Lab Engineer 必须把真实 `dotnet --info`、OS、timezone、command、exit code 写入 execution log。若实际环境与 frozen target 不同，不得静默替换；停止并回报 Researcher / Master 决定是否解冻。环境记录可含时间，normalized evidence artifact 不可含时间。

### 3.2 Planned implementation layout（not created by Researcher）

```text
lab-03-minimal-agent-loop/
├── README.md                         # this frozen design
├── src/MinimalAgentLoop/
├── tests/MinimalAgentLoop.Tests/
├── fixtures/
│   ├── build.log
│   ├── BuildMenu.cs
│   ├── Unrelated.cs
│   └── cases.json                    # decisions/fault/max only; no expected answer
└── observations/
    ├── run-a/
    ├── run-b/
    └── execution-log.md
```

Researcher 不创建上述子目录。Lab Engineer 实现时不得增加 Provider、HTTP、MCP、数据库或真实文件系统浏览。

## 4. Course-scoped loop vocabulary

| Term | Frozen Lab meaning | Explicit non-equivalence |
|---|---|---|
| Run | 一个 case 的 `RunAsync`，从 frozen goal + initial state 到 terminal record | 不是 OpenAI `Runner.run` 的 universal alias |
| Turn | 外部输入分组；每个 case 固定 `turn_index=1`，同一 Run 的所有 Steps 共享 turn | 不等于 OpenAI max-turn invocation；不承担 counter |
| Step | 一个 committed loop iteration：取得一个 Decision candidate，并提交 ACT 或 REQUEST_STOP 的 Host transition | 不是 LangGraph super-step；不保证等于一次模型调用 |
| Decide | `ScriptedDecisionSource` 给出的 frozen candidate | 不等于 Host 已授权执行或接受成功 |
| Tool Outcome | fixture tool 的 correlated raw execution record | 不等于 Observation，不等于 Evidence |
| Observation | Host normalization 后可进入 reducer / next Decide view 的课程对象 | 不是跨 SDK 标准 API 类型 |
| State Update | Host reducer 唯一提交 authoritative state revision | script/tool 不可直写 |
| Stop | Host 写入 lifecycle、termination reason、outcome | `STOPPED` 不推出 `SUCCEEDED` |

## 5. Deterministic Decision Source

### 5.1 Contract

`ScriptedDecisionSource v1`：

- 每个 case 只按 cursor 返回 README 冻结的 Decision candidates；
- 输入可以读取 case ID、cursor、read-only state view 与 previous normalized Observation；
- 不调用 model、Provider、network、environment secret；
- 不写 state、不执行 tool、不计算 run outcome；
- 不读取 Expected termination / outcome / success；
- 每次调用必须增加 `decision_calls_used`；未调用的 candidate 保持 `NOT_RUN`。

### 5.2 Decision schema

```text
schema_version          = "lab03-decision-v1"
decision_id             = deterministic case-local ID
decision_source         = "SCRIPTED_V1"
kind                    = ACT | REQUEST_STOP
invocation_id           = deterministic ID for ACT; NOT_RUN for REQUEST_STOP
tool_name               = parse_mock_log | read_mock_file | NOT_RUN
arguments               = canonical object or NOT_RUN
requested_outcome       = SUCCEEDED | NOT_RUN
output.status           = SUPPORTED | NOT_RUN
output.summary          = fixed string | NOT_RUN
output.evidence_ids     = sorted list | NOT_RUN
```

`requested_outcome` 与 `output.*` 是 candidate input。Host 不得复制它们生成 authoritative `outcome`。

### 5.3 Anti-self-fulfilling rule

`fixtures/cases.json` 只允许包含：

- `case_id`、goal contract ID、max steps；
- scripted Decisions；
- named fault ID / target invocation；
- fixture relative paths。

它**禁止**包含：

- expected termination reason；
- expected run outcome / success bool；
- expected counts/digests；
- assertion result；
- expected Evidence mapping。

Expected 只存在于本 README 与 independent test assertions。runtime 不能读 README 或 test expected data。若 runner 输入含 expected answer，本 Lab 无效。

## 6. Frozen fixtures

Lab Engineer 必须创建以下 UTF-8/LF 精确内容，并在 execution log 记录 SHA-256。路径只允许相对 `fixtures/` resolve。

### `build.log`

```text
BuildMenu.cs(3,5): error CS0103: The name 'missingIdentifier' does not exist in the current context
```

### `BuildMenu.cs`

```csharp
public static class BuildMenu
{
    missingIdentifier();
}
```

### `Unrelated.cs`

```csharp
public static class Unrelated
{
    public static void NoOp() { }
}
```

### Frozen goal contract `goal-contract-v1`

Goal：`Explain the CS0103 in build.log with log and matching source evidence.`

成功必须同时满足：

1. Decision kind 为 `REQUEST_STOP` 且 `requested_outcome=SUCCEEDED`；
2. output shape valid：`status=SUPPORTED`、非空 summary、evidence IDs 为 sorted unique allowlisted IDs；
3. state fact `diagnostic.code=CS0103`、`diagnostic.path=BuildMenu.cs`、`diagnostic.line=3`、`diagnostic.symbol=missingIdentifier`；
4. `source_match=true`；
5. accepted goal Evidence 精确包含 `EV-LOG-001` 与 `EV-FILE-001`；
6. `unresolved_requirement_codes=[]`；
7. `unresolved_tool_failure_count=0`；
8. requested evidence IDs 都曾由 accepted Observation 产生。

Evidence IDs：

| ID | Origin | Goal-relevant | Acceptance condition |
|---|---|---:|---|
| `EV-LOG-001` | successful normalized `parse_mock_log(build.log)` | yes | parsed CS0103 tuple matches frozen log |
| `EV-FILE-001` | successful normalized `read_mock_file(BuildMenu.cs)` | yes | line 3 contains `missingIdentifier` and matches parsed diagnostic |
| `EV-UNRELATED-001` | successful normalized `read_mock_file(Unrelated.cs)` | no | may enter history, never satisfies source requirement |
| `EV-FAKE` | no Tool Outcome / Observation | no / rejected | must fail evidence-domain + provenance allowlist |

## 7. Fixture tools and named fault

### 7.1 `parse_mock_log`

- Input：relative path，only `build.log` allowed。
- Success Tool Outcome：`disposition=SUCCEEDED`，code `LOG_PARSED`，data contains exact diagnostic tuple and semantic payload digest。
- Normalized Observation：`normalization_status=PASS`，kind `LOG_PARSED`，Evidence `EV-LOG-001`。

### 7.2 `read_mock_file`

- Input：relative path，only `BuildMenu.cs` or `Unrelated.cs` allowed。
- Success Tool Outcome：`disposition=SUCCEEDED`，code `FILE_READ`，data contains relative path、content digest、requested line text。
- Normalized Observation：kind `SOURCE_READ`；BuildMenu may create `EV-FILE-001` after match，Unrelated creates non-goal `EV-UNRELATED-001`。

### 7.3 Named fault `FI_PARSE_TYPED_FAILURE`

- Only AL-02 / invocation `al02-call-01`。
- Inject at fixture-tool seam；do not throw process-level exception。
- Raw Tool Outcome：`disposition=FAILED`，code `MOCK_PARSE_FAILED`，empty evidence IDs，deterministic error payload。
- Normalized Observation：`normalization_status=PASS`，kind `TOOL_FAILURE`，code `MOCK_PARSE_FAILED`，`source_result_record_sha256` points to failed Tool Outcome。
- Reducer：adds one unresolved typed failure and leaves goal evidence empty。
- Negative case still returns process exit `0` when Host behavior matches Expected；runner crash is a Lab failure。

这不是重做 Article 06 的 path/policy/idempotency Lab；这里只保留最小 typed result -> Observation -> state chain。

## 8. Authoritative state schema

每个 committed Step 写一条 after-state snapshot。State 只能由 Host reducer 创建：

```text
schema_version                  = "lab03-state-v1"
goal_contract_version           = "goal-contract-v1"
case_id                         = AL-01..AL-04
run_id                          = deterministic "lab03-suite-v1/<case>"
turn_index                      = 1
revision                        = integer; exactly +1 per committed Step
lifecycle                       = RUNNING | STOPPED
outcome                         = NOT_RUN | SUCCEEDED | FAILED | INCOMPLETE
termination_reason              = NOT_RUN | GOAL_SATISFIED | UNRESOLVED_TOOL_FAILURE |
                                  MAX_STEPS_EXHAUSTED | STOP_CONTRACT_FAILED |
                                  CANCELLED | HOST_FAILURE
sorted_facts                    = canonical goal facts only
accepted_goal_evidence_ids      = sorted unique list
non_goal_evidence_ids           = sorted unique list
rejected_evidence_ids           = sorted unique list
unresolved_requirement_codes    = sorted unique list
unresolved_tool_failures        = canonical sorted typed records
history_length                  = integer
last_observation_kind           = enum | NOT_RUN
last_observation_source_digest  = SHA-256 | NOT_RUN
last_action_fingerprint         = SHA-256 | NOT_RUN
repeat_action_fingerprint       = SHA-256 | NOT_RUN
progress_status                 = PROGRESS | NO_PROGRESS | NOT_RUN
steps_used / max_steps / remaining_steps
decision_calls_used / tool_calls_used
output_contract_status          = NOT_RUN | PASS | FAIL
success_contract_status         = NOT_RUN | PASS | FAIL
full_state_sha256               = SHA-256
goal_state_sha256               = SHA-256
```

### 8.1 Digest rules

- canonical JSON：UTF-8/LF、sorted property names、sorted set-like arrays、no whitespace variance。
- `action_fingerprint = SHA256(tool_name + canonical_arguments)`；**excludes** decision ID、invocation ID、case ID、time。
- `tool_result_payload_sha256` covers disposition/code/data/error only；repeat semantic results may match。
- `tool_result_record_sha256` also covers correlation/invocation ID；distinct invocations must differ。
- `full_state_sha256` covers all state except the digest fields themselves，including history metadata。
- `goal_state_sha256` covers only goal facts、accepted goal Evidence、unresolved requirements/failures；excludes history length、invocation IDs、last-action fields、budget counters、lifecycle/outcome。
- AL-04 repeated irrelevant reads must change full-state digest while leaving goal-state digest unchanged。

## 9. Observation schema

```text
schema_version                       = "lab03-observation-v1"
observation_id                       = deterministic case/step ID
source                               = TOOL_RUNTIME | LOOP_GUARD
source_invocation_id                 = deterministic ID | NOT_RUN
source_result_record_sha256          = SHA-256 | NOT_RUN
source_result_payload_sha256         = SHA-256 | NOT_RUN
normalization_status                 = PASS | FAIL
kind                                 = LOG_PARSED | SOURCE_READ | TOOL_FAILURE |
                                       DUPLICATE_ACTION | LIMIT_REACHED | NOT_RUN
code                                 = stable code
goal_relevant                        = true | false
evidence_ids                         = sorted list
normalized_data_sha256               = SHA-256
```

关键断言：`Tool Outcome disposition=FAILED` 与 `Observation normalization_status=PASS / kind=TOOL_FAILURE` 必须同时成立；前者描述执行失败，后者描述失败被可靠观察，不能共用一个 status 字段。

## 10. Trace schema

每个 normalized `trace.jsonl` 共 14 行：10 个 `STEP` + 4 个 `TERMINAL`。所有字段在所有记录中存在；不适用阶段写稳定枚举 `NOT_RUN`，不得省略以掩盖早停：

```text
schema_version = "lab03-trace-v1"
sequence
event_type = STEP | TERMINAL
case_id / run_id / turn_index / step_index
state_revision_before / state_revision_after
full_state_sha256_before / full_state_sha256_after
goal_state_sha256_before / goal_state_sha256_after
decision_source / decision_id / decision_kind / decision_contract_status
requested_outcome
invocation_id / tool_name / arguments_sha256 / action_fingerprint
tool_executed
tool_result_disposition / tool_result_code
tool_result_record_sha256 / tool_result_payload_sha256
observation_normalization_status / observation_kind
observation_sha256 / observation_source_result_sha256
repeat_detected / progress_status
unresolved_requirement_codes / unresolved_tool_failure_count
steps_used / max_steps / remaining_steps
decision_calls_used / tool_calls_used
model_stop_requested
output_contract_status / success_contract_status
control_decision = CONTINUE | STOP
termination_reason
run_outcome
```

Normalized artifacts 禁止 wall-clock、absolute path、PID、random GUID。每个 fresh process 的真实 metadata 只写 `execution-log.md`。

## 11. Stop precedence and state transition algorithm

冻结顺序如下；实现不得重排：

1. Start Run，创建 revision 0 state，`turn_index=1`。
2. **Pre-decision guard**：若 cancellation 已观察，terminal `CANCELLED / INCOMPLETE`；否则若 `steps_used >= max_steps`，terminal `MAX_STEPS_EXHAUSTED / INCOMPLETE`。两者都禁止调用 Decision source。
3. 调用 `ScriptedDecisionSource`，增加 decision-call count；验证 Decision shape。
4. 若 `ACT`：
   1. canonicalize action，计算 fingerprint / repeat；
   2. execute at most one fixture tool，增加 tool-call count；
   3. correlate + normalize Tool Outcome；
   4. Host reducer 提交 exactly one state revision，增加 steps-used；
   5. 比较 goal-state digest 判定 `PROGRESS / NO_PROGRESS`；
   6. control=`CONTINUE`，回到 pre-decision guard。
5. 若 `REQUEST_STOP`：
   1. 增加 steps-used；
   2. Host 验证 output shape、goal facts、Evidence provenance/allowlist、unresolved failures；
   3. 若 unresolved tool failure > 0：`UNRESOLVED_TOOL_FAILURE / FAILED`；
   4. 否则若任一 output/goal/evidence contract fail：`STOP_CONTRACT_FAILED / FAILED`；
   5. 否则：`GOAL_SATISFIED / SUCCEEDED`；
   6. Host 提交 exactly one terminal state revision，control=`STOP`。
6. 对 terminal state 写一条独立 TERMINAL trace record；不再写额外 state snapshot。

Host internal exception 才可产生 `HOST_FAILURE / FAILED`。本 Lab 不执行 cancellation trajectory；其 precedence 仅冻结 schema，不能产出 cancellation Claim。

## 12. Frozen case matrix

| Case | max_steps | Scripted Steps | Expected tool calls | Expected termination | Expected outcome | Claim focus |
|---|---:|---:|---:|---|---|---|
| AL-01 `SUCCESS` | 3 | 3 | 2 | `GOAL_SATISFIED` | `SUCCEEDED` | full loop + accepted stop contract |
| AL-02 `TOOL_FAILURE_THEN_STOP` | 3 | 2 | 1 | `UNRESOLVED_TOOL_FAILURE` | `FAILED` | failed Result -> failure Observation；requested success rejected |
| AL-03 `MAX_STEPS` | 2 | 2（third candidate unconsumed） | 2 | `MAX_STEPS_EXHAUSTED` | `INCOMPLETE` | pre-decision off-by-one guard |
| AL-04 `REPEAT_PSEUDO_COMPLETE` | 4 | 3 | 2 | `STOP_CONTRACT_FAILED` | `FAILED` | same fingerprint/no goal progress/fake Evidence rejected |

Total Expected：4 cases、10 STEP rows、4 TERMINAL rows、10 state snapshots、7 tool calls、10 decision calls、exactly 1 `SUCCEEDED`。

### 12.1 AL-01｜Success in three Steps

| Step | Decision candidate | Tool / Observation | Expected state / control |
|---:|---|---|---|
| 1 | `ACT parse_mock_log("build.log")`, invocation `al01-call-01` | success `LOG_PARSED` -> Observation `LOG_PARSED`, `EV-LOG-001` | diagnostic tuple accepted；source requirement remains；CONTINUE |
| 2 | `ACT read_mock_file("BuildMenu.cs")`, invocation `al01-call-02` | success `FILE_READ` -> Observation `SOURCE_READ`, `EV-FILE-001` | `source_match=true`；requirements empty；CONTINUE |
| 3 | `REQUEST_STOP`, requested `SUCCEEDED`, output `SUPPORTED`, summary fixed, evidence `[EV-FILE-001, EV-LOG-001]` | tool fields `NOT_RUN` | output/goal/evidence/failure contracts PASS；STOP |

Expected terminal：`lifecycle=STOPPED / termination=GOAL_SATISFIED / outcome=SUCCEEDED`。

### 12.2 AL-02｜Typed tool failure cannot be painted green

| Step | Decision candidate | Tool / Observation | Expected state / control |
|---:|---|---|---|
| 1 | `ACT parse_mock_log("build.log")`, invocation `al02-call-01` | named fault -> Tool Outcome `FAILED/MOCK_PARSE_FAILED`; normalization PASS -> `TOOL_FAILURE` | unresolved tool failure count 1；no Evidence；CONTINUE |
| 2 | `REQUEST_STOP`, requested `SUCCEEDED`, structurally valid `SUPPORTED` output, evidence `[]` | tool fields `NOT_RUN` | output shape PASS；success contract FAIL because unresolved failure；STOP |

Expected terminal：`STOPPED / UNRESOLVED_TOOL_FAILURE / FAILED`。failure result record digest 必须被 Step 1 Observation 引用；negative case 符合预期时 runner exit 0。

### 12.3 AL-03｜Max step before third Decide

| Step | Decision candidate | Tool / Observation | Expected state / control |
|---:|---|---|---|
| 1 | `ACT parse_mock_log("build.log")`, invocation `al03-call-01` | success -> `LOG_PARSED`, `EV-LOG-001` | one source requirement remains；CONTINUE |
| 2 | `ACT read_mock_file("Unrelated.cs")`, invocation `al03-call-02` | success -> `SOURCE_READ`, `EV-UNRELATED-001` non-goal | required BuildMenu evidence remains；steps used reaches 2；CONTINUE to guard |
| NOT_RUN | queued third `REQUEST_STOP` | must not call Decision source or tool | pre-decision guard stops Run |

Expected terminal：`STOPPED / MAX_STEPS_EXHAUSTED / INCOMPLETE`；decision calls 2、tool calls 2、steps 2。必须证明 third candidate 未消费，而不只是没有第三个 tool call。

### 12.4 AL-04｜Repeat/no-progress and pseudo-completion

| Step | Decision candidate | Tool / Observation | Expected state / control |
|---:|---|---|---|
| 1 | `ACT read_mock_file("Unrelated.cs")`, invocation `al04-call-01` | success -> `SOURCE_READ`, `EV-UNRELATED-001` non-goal | full-state changes；goal-state digest unchanged；`NO_PROGRESS`；CONTINUE |
| 2 | same tool + canonical args, invocation `al04-call-02` | success with same semantic payload digest；new correlated record | same action fingerprint；repeat true；full-state changes；goal-state digest still unchanged；`NO_PROGRESS` |
| 3 | `REQUEST_STOP`, requested `SUCCEEDED`, output `SUPPORTED`, claims `[EV-FAKE]` | tool fields `NOT_RUN` | fake Evidence provenance/allowlist FAIL；required log/source missing；STOP |

Expected terminal：`STOPPED / STOP_CONTRACT_FAILED / FAILED`。action fingerprint 必须相同，invocation IDs 与 result-record digests 必须不同；runtime 不能因为 history 增长宣称 goal progress。

## 13. Expected observable artifacts

每个 fresh process 目录必须有：

| File | Expected shape | Evidence use |
|---|---|---|
| `trace.jsonl` | exactly 14 rows：10 STEP + 4 TERMINAL | control flow、NOT_RUN、stop/outcome、Result->Observation refs |
| `states.jsonl` | exactly 10 after-state snapshots | revision、full/goal digests、requirements/failures |
| `case-results.jsonl` | exactly 4 terminal summaries | one SUCCEEDED、two FAILED、one INCOMPLETE |
| `artifact-manifest.json` | relative paths、SHA-256、schema versions、fixture digests | freshness/completeness |

Global `execution-log.md` 记录两次 commands、start/end time、environment、exit code、build/test results、failed attempts；它不参与 byte comparison。

Fresh-process comparison：

- `run-a/trace.jsonl == run-b/trace.jsonl` byte-for-byte；
- `states.jsonl` byte-for-byte；
- `case-results.jsonl` byte-for-byte；
- corresponding SHA-256 identical；
- manifest 中排除 run directory name / wall clock 后的 normalized artifact entries identical。

## 14. Planned commands / execution needs（not run by Researcher）

Lab Engineer 必须在 Lab 目录执行并记录真实输出；命令可因最终 project filename 的大小写微调，但语义不得改变：

```powershell
dotnet --info
dotnet restore .\MinimalAgentLoop.slnx --locked-mode
dotnet build .\MinimalAgentLoop.slnx -c Release --no-restore
dotnet test .\MinimalAgentLoop.slnx -c Release --no-build
dotnet run --project .\src\MinimalAgentLoop\MinimalAgentLoop.csproj -c Release --no-build -- --cases .\fixtures\cases.json --out .\observations\run-a
dotnet run --project .\src\MinimalAgentLoop\MinimalAgentLoop.csproj -c Release --no-build -- --cases .\fixtures\cases.json --out .\observations\run-b
```

需要独立 artifact verifier（可作为 test project command）检查 row counts、enums、digests、cross-references、NOT_RUN、expected case outcomes 与 byte equality。所有负例符合 Expected 时 test/runner exit `0`；任何断言失败、schema 缺字段或 runner crash 为 non-zero。

## 15. Acceptance criteria

### Build / environment

- [ ] environment exact match frozen target, or Design formally unfrozen before running
- [ ] BCL-only；no Provider/network/credential/package runtime dependency
- [ ] restore/build/test exit 0；warnings/errors recorded
- [ ] fixture contents and SHA-256 match both runs

### Counts and terminal semantics

- [ ] exactly 4 cases / 10 STEP / 4 TERMINAL / 10 state snapshots / 7 tool calls / 10 decision calls per run
- [ ] every case terminal `lifecycle=STOPPED` with explicit termination reason and outcome
- [ ] exactly one outcome `SUCCEEDED`（AL-01）
- [ ] AL-02 `FAILED`、AL-03 `INCOMPLETE`、AL-04 `FAILED`
- [ ] all non-entered phases use `NOT_RUN`；no missing-field shortcut

### Result -> Observation -> State

- [ ] every ACT has exactly one correlated Tool Outcome and one normalized Observation
- [ ] failed Tool Outcome in AL-02 is referenced by a `normalization=PASS / kind=TOOL_FAILURE` Observation
- [ ] only Host reducer changes state；revision exactly +1 per Step
- [ ] Tool Result / Observation / Evidence IDs remain separate

### Stop / success

- [ ] run outcome is derived only by Host Goal + Output + Evidence + unresolved-failure contract
- [ ] AL-02 requested success cannot override unresolved failure
- [ ] AL-03 third Decision and any third tool call are provably unconsumed / `NOT_RUN`
- [ ] AL-04 fake Evidence fails provenance/domain allowlist
- [ ] limit/failure/cancellation semantics are never recorded as success

### Repeat / progress

- [ ] AL-04 invocation IDs differ while canonical action fingerprints match
- [ ] repeat semantic result payload digests match；correlated result-record digests differ
- [ ] full-state digest changes after each repeat Step
- [ ] goal-state digest remains unchanged；both repeat Steps report `NO_PROGRESS`

### Reproducibility / evidence hygiene

- [ ] normalized run-a and run-b artifacts are byte-identical
- [ ] actual timestamps/absolute paths/process IDs appear only in execution log
- [ ] raw artifacts and all failed attempts preserved
- [ ] Lab Engineer writes Observation only from actual files, never copies Expected
- [ ] Researcher performs separate Evidence Merge before any Claim upgrade

任何一项不满足：Lab status 不得 `VERIFIED`，08-C07/C08 不得 `CONFIRMED`，Evidence Gate 保持 `NOT_READY`。

## 16. Evidence mapping

| Lab evidence | Claim | Interpretation allowed after execution | Interpretation forbidden |
|---|---|---|---|
| AL-01 trace/state/output | 08-C05, C07, C08 | fixed Host implementation accepted a valid completion | 真实模型会自主完成目标 |
| AL-02 failed Result + failure Observation + terminal | 08-C04, C05, C07, C08 | fixed Host preserved typed failure and rejected requested success | 任意 runtime 都采用相同 failure policy |
| AL-03 guard/counters/unconsumed decision | 08-C06, C08 | Lab max_steps is pre-decision external bound | 等同 OpenAI max_turn / LangGraph recursion limit / cost budget |
| AL-04 fingerprints + two state digests + terminal | 08-C05, C07, C08 | fixed Host distinguishes history growth from goal progress and rejects fake evidence | duplicate action detection proves idempotency或planning quality |
| fresh-process equality | 08-C08 | fixtures/Host produce reproducible normalized artifacts | Provider/LLM output deterministic |

## 17. Limitations and safety boundaries

- Decision 是 scripted substitute，不是 Model / Provider behavior evidence。
- 每 case 只有一个 external Turn，`turn_index=1`；Lab 只证明本文映射，不证明任何 universal Turn hierarchy。
- sequential、single-process-per-suite、single-action-per-Step；不含 handoff、parallel nodes、workflow/state machine（Article 10）。
- 不含 planning、replanning、search quality（Article 09）。
- 不执行 cancellation；不证明停止正在运行的 I/O，也不撤销 external side effects。checkpoint/resume/recovery 属于 Article 11。
- 不含 context compaction、session memory、knowledge retrieval（Article 12+）。
- `max_steps` 不是 token/cost/latency budget；预算工程属于 Article 20。
- fixture tools 全部 read-only；不访问 repository 其他路径，不创建真实外部副作用。
- named fault 是 test seam，不是生产 failure-rate 或 recovery evidence。
- 不重复证明 Article 06 的 policy/idempotency，也不把 Article 07 MCP protocol 当 runtime execution。

## 18. Design freeze / change control

以下为 frozen 项，Lab Engineer 不得自行修改：

- four case IDs、Decision order、max steps、fault target；
- fixtures exact content、goal contract、Evidence IDs；
- termination/outcome matrix 与 stop precedence；
- state / observation / trace schema；
- action/full-state/goal-state digest rules；
- exact expected counts；
- two-fresh-process comparison；
- acceptance criteria 与 limitations。

若实现发现矛盾：停止，不运行变体，不编辑 Expected；向 Researcher 回报精确冲突，由 Researcher决定是否解冻并更新 Design。Master 才能安排下一 Gate。

## 19. Current handoff

- Preliminary Evidence：`COMPLETE`
- Design completeness：`FROZEN / COMPLETE`
- Source / Tests / Fixtures：`NOT CREATED`
- Build / Run / Fault Injection：`NOT RUN`
- Observed：`NONE`
- Exact next action：Master 分派真实 Lab Engineer，先按 frozen environment 和 layout 实现 source/tests/fixtures，再执行 build、four-case two-process suite，保留 raw artifacts，由 Researcher做 Evidence Merge。

## Lab Engineer Observations

> 本节由 Lab Engineer 在冻结 Design 的原始 555 行之后追加。上文仍是执行前 Expected；本节只记录 2026-08-20 的实际环境、命令退出码与 raw artifact 观测，不升级 Claim，不执行 Evidence Merge。

### Execution status

- Lab Execute：`COMPLETE`
- Lab Observation：`COMPLETE`
- Evidence Merge：`PENDING / RESEARCHER OWNED`
- Environment match：`PASS`
- Locked restore：`PASS / exit 0`
- Release build：`PASS / exit 0 / 0 warnings / 0 errors`
- BCL spec test：`PASS / exit 0`
- Formal run-a：`PASS / exit 0`
- Formal run-b：`PASS / exit 0`
- Independent artifact verifier：`PASS / exit 0`
- Provider / network / credentials：`NONE / NOT USED / NONE`
- Detailed command and failure ledger：[observations/execution-log.md](observations/execution-log.md)

### Observed environment

| Item | Observed |
|---|---|
| OS / RID | Windows `10.0.19045` / `win-x64` |
| .NET SDK | `10.0.301` |
| .NET Host / Runtime | `10.0.9` |
| Target Framework | `net10.0` |
| Timezone | `China Standard Time / UTC+08:00` |
| Dependencies | BCL-only；offline package sources；locked restore |

### Observed commands and exits

```powershell
dotnet restore .\MinimalAgentLoop.slnx --locked-mode --nologo --verbosity minimal                       # exit 0
dotnet build .\MinimalAgentLoop.slnx -c Release --no-restore --nologo --verbosity minimal              # exit 0
dotnet test .\MinimalAgentLoop.slnx -c Release --no-build --no-restore --nologo --verbosity minimal    # exit 0
dotnet run --project .\src\MinimalAgentLoop\MinimalAgentLoop.csproj -c Release --no-build --no-restore -- --cases .\fixtures\cases.json --out .\observations\run-a  # exit 0
dotnet run --project .\src\MinimalAgentLoop\MinimalAgentLoop.csproj -c Release --no-build --no-restore -- --cases .\fixtures\cases.json --out .\observations\run-b  # exit 0
dotnet .\tests\MinimalAgentLoop.Tests\bin\Release\net10.0\MinimalAgentLoop.Tests.dll --verify-only .\observations\run-a .\observations\run-b                       # exit 0
```

### Observed counts and terminal records

Each fresh process produced exactly `4 cases / 10 STEP / 4 TERMINAL / 10 state snapshots / 7 Tool Outcomes / 7 normalized Observations / 7 tool calls / 10 decision calls / 1 SUCCEEDED`.

| Case | Observed lifecycle | Observed termination | Observed outcome | Steps / decisions / tools |
|---|---|---|---|---|
| AL-01 | `STOPPED` | `GOAL_SATISFIED` | `SUCCEEDED` | `3 / 3 / 2` |
| AL-02 | `STOPPED` | `UNRESOLVED_TOOL_FAILURE` | `FAILED` | `2 / 2 / 1` |
| AL-03 | `STOPPED` | `MAX_STEPS_EXHAUSTED` | `INCOMPLETE` | `2 / 2 / 2` |
| AL-04 | `STOPPED` | `STOP_CONTRACT_FAILED` | `FAILED` | `3 / 3 / 2` |

### Observed Result -> Observation -> State facts

- AL-02 Tool Outcome was `FAILED / MOCK_PARSE_FAILED`; its record SHA-256 was `B39ED180065C66D1115C1ACC0A50F98204FC6B066A44DC0046BF0091D51C13A4`.
- AL-02 normalized Observation was `PASS / TOOL_FAILURE / MOCK_PARSE_FAILED` and referenced that exact failed Tool Outcome digest.
- AL-03 used 2 Decision calls and 2 tool calls; `al03-decision-03` remained explicitly unconsumed in `remaining_decision_ids`.
- AL-04 used distinct invocation IDs with the same action fingerprint `C25D1F779277059899AC5145991CB185E76ECC525CB5945ED741E07CCDFD9049`.
- AL-04 repeated semantic payload digests matched while correlated result-record digests differed. Both full-state digests changed, both goal-state digests stayed equal, and both ACT rows reported `NO_PROGRESS`.
- `EV-FAKE` was rejected; `STOPPED` did not become `SUCCEEDED` for AL-02, AL-03, or AL-04.

### Observed artifacts and reproducibility

Each formal run directory contains 6 files / 47,772 bytes. The corresponding run-a / run-b files were byte-identical:

| Artifact | SHA-256 in both fresh processes |
|---|---|
| `artifact-manifest.json` | `6B1E3148DF5812B92A155BCEB29783B540CF9D4E8576D9012388A6B73ACD00E6` |
| `case-results.jsonl` | `90F2256AA18E401C6DDCEFFFB0837AB25105C58A80A3D24D3A87ADFD907D157D` |
| `observations.jsonl` | `5A446F0327571D33AECFFB2B642C71A3CC9D28ADBE9E5341BAF8FD5D21809586` |
| `states.jsonl` | `88F3E541C1A17FD44AA924ACB912C62B9C387F0669EF0F071979AD94A750E729` |
| `tool-outcomes.jsonl` | `128FE933B0CFF633949B0EDABEF6B4294379D119C4174D2F08EBF420B54A1332` |
| `trace.jsonl` | `3B816B5B7E2E370EED38268F02E83B045EAEDC6EAB9CEC801266ADD76D4D6427` |

`fixtures/cases.json` SHA-256 is `ED2F677D9D3F3BDF6E79C697A3964A189D2EE88D61CBA45E20858737A3D0E47D`. The runtime and specs found no expected termination, expected outcome, success boolean, expected count/digest, or assertion-result input field.

### Unexpected behavior and preserved failures

The final execution had no unexpected case outcome or artifact mismatch. Earlier failed attempts remain in `observations/execution-log.md`: CIM inventory access denied, one compile-name collision, an extra fixture EOF blank line, an unavailable NuGet testhost path that was replaced by the BCL-owned test target, and one live-reference snapshot digest mismatch. Two later Master interrupts occurred only while Lab Engineer turns were organizing this Markdown delivery; no Lab command was running at either interrupt, so they are recorded as orchestration/log-delivery interruptions rather than Lab failures. None changed the frozen trajectory, Expected matrix, or acceptance criteria.

### Runtime limitations and handoff

- These observations cover only this deterministic, read-only, no-Provider fixture on the frozen environment.
- No cancellation trajectory, external side effect, model quality, planning quality, framework universality, or production reliability was observed.
- Lab Engineer assigns no Claim status. Exact next action：Researcher reads the frozen Design, raw run-a/run-b artifacts, full failure ledger, and this Observation, then performs `Experiment -> Observation -> Evidence Interpretation -> Claim Status` Evidence Merge.

## Researcher Evidence Merge

> 本节由 Article 08 Researcher 在 frozen Design 与 Lab Engineer Observations 之后追加。Researcher 未修改 frozen first `30312` bytes、Lab Engineer Observations、source/tests/fixtures/raw artifacts，也未重新执行 Lab。

### Merge status

- Merge Date：`2026-08-20`（Asia/Shanghai）
- Evidence Merge：`COMPLETE`
- Lab Status Candidate：`VERIFIED / EVIDENCE_MERGED`
- Claim Summary：`6 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 2 PROPOSAL`
- Evidence Gate Recommendation：`PASS`
- Evidence Gate Closure：`MASTER DECISION PENDING`
- Blocker：`NONE`

### Experiment

Researcher 重新读取并交叉核对完整 Design + Lab Engineer Observations、execution log、run-a/run-b 全部 raw JSONL/manifests、`cases.json`、fixture hashes、与 Claim 相关的 `LabRunner` / canonicalization / model records / independent BCL specs。额外只读机械核验确认：

- frozen first `30312` bytes SHA-256 为 `242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`；
- run-a / run-b 六个对应文件逐 byte 相等；
- per run 为 `10 STEP / 4 TERMINAL / 10 states / 7 Tool Outcomes / 7 Observations / 7 tool calls / 10 decisions / 1 SUCCEEDED`。

### Observation

| Case | Raw observation | Terminal |
|---|---|---|
| AL-01 | two goal-relevant Results/Observations produced exact log + source Evidence；completion contracts PASS | `GOAL_SATISFIED / SUCCEEDED` |
| AL-02 | `FAILED/MOCK_PARSE_FAILED` Tool Outcome 被 `PASS/TOOL_FAILURE` Observation 以同一 record digest 引用；requested success 未覆盖 unresolved failure | `UNRESOLVED_TOOL_FAILURE / FAILED` |
| AL-03 | only two Decisions/tools consumed；`al03-decision-03` remains in `remaining_decision_ids` | `MAX_STEPS_EXHAUSTED / INCOMPLETE` |
| AL-04 | distinct invocation IDs share one action fingerprint；semantic payload equal、record digests distinct；full-state changed、goal-state unchanged、both `NO_PROGRESS`；`EV-FAKE` rejected | `STOP_CONTRACT_FAILED / FAILED` |

### Evidence interpretation

Lab 03 证明的是：在 frozen Windows/.NET 环境、固定 read-only fixtures、`ScriptedDecisionSource v1` 与当前 fixed Host implementation 中，Result -> Observation -> Host state、completion validation、pre-decision max-step、repeat/no-progress 与四种 terminal outcome 按 Design 工作，并能在两个 fresh processes 中复现。

Lab 03 不证明真实模型/Provider 会选对 action、会 recovery 或会停止；不证明课程 Run/Turn/Step 与 Host reducer 是行业标准；不证明 cancellation、external side-effect rollback、planning、workflow/state machine、long-running、context/memory、budget engineering 或生产可靠性。

### Claim status

| Claim | Merged status |
|---|---|
| 08-C01 | `CONFIRMED / PRODUCT-SCOPED` |
| 08-C02 | `CONFIRMED / CITED-PRODUCTS-SCOPED` |
| 08-C03 | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` |
| 08-C04 | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` |
| 08-C05 | `PROPOSAL / IMPLEMENTATION CONFORMANCE OBSERVED` |
| 08-C06 | `CONFIRMED / PRODUCT-SCOPED + LAB CONFORMANCE` |
| 08-C07 | `CONFIRMED / FIXED-HOST-FIXTURE-SCOPED` |
| 08-C08 | `CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED` |

### Preserved counter-evidence

execution log 中的 CIM denied、compile collision、fixture EOF、unavailable testhost 与 live-reference snapshot digest mismatch 均保留；它们限制成功结论只能指向 current source/final raw artifacts，不构成 recovery evidence。两次 Master interruption 发生在正式命令结束后的 Markdown delivery，当时没有 Lab command 运行；它们不是 case failure，也不是 cancellation Observation。

### Handoff

Exact next action：Master 独立复核 Researcher Merge，关闭 Evidence Gate 后分派真实 Outliner。Researcher 不创建 Outline / Draft。
