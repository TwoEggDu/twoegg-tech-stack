# Lab 06｜Trace + Eval：Golden Corpus 与 Regression Gate

> 本文件同时承载 Researcher 冻结的 `LAB_DESIGN` 与 Lab Engineer 的 `LAB_OBSERVATION`。Design 与 Observed Result 分区保存；禁止执行后反向修改判据。

## Metadata

- Lab ID：`Lab 06`
- Title：`Trace + Eval：Golden Corpus 与 Regression Gate`
- Owning Article：`22｜Eval、Golden Dataset 与 Regression`
- Lifecycle Status：`VERIFIED / EVIDENCE_MERGED`
- Evidence Status：`PASS / FIXTURE-SCOPED`
- Runtime / Language：`C# / .NET`
- Fixture Version：`lab06-fixture-v1`
- Corpus：`lab06-golden-corpus / r1`
- Scorer：`lab06-deterministic-exact-scorer / v1`
- Environment：`planned .NET SDK 10.0.301, net10.0, BCL-only, offline`
- Last Run：`2026-08-28 / .NET SDK 10.0.301 / Windows`
- BuildPilot：`DESIGN / NOT IMPLEMENTED / NOT RUN`

## Goal

消除一个窄而可判定的不确定性：在同一 accepted fixture corpus、冻结 scorer 和可比 manifest 下，一个 deterministic C# evaluator 是否能让 baseline 通过，同时捕获一个已知 critical regression，并拒绝用仍过线的 aggregate score 掩盖该退化。

本 Lab 验证的是 Eval/Regression **机制**，不是 Agent、模型或 Failure Taxonomy 的准确率。

## Lab Design（Owner：Researcher / FROZEN 2026-08-28）

- Related Article：`Article 22`
- Related Claim IDs：`22-C07`、`22-C10`；辅助覆盖 `22-C04`、`22-C06`、`22-C09`、`22-C11`
- Research Question：`固定 corpus/scorer/manifest 下，evaluator 能否同时保留 aggregate 与 critical gate，并把 baseline -> known-regression 的 C01 变化判为 REGRESSION？`
- Hypothesis：`baseline 8/8、critical 2/2，overall PASS；known-regression 7/8、critical 1/2，aggregate threshold 单独 PASS 但 overall FAIL，且 C01=REGRESSION。缺 case 必须 UNKNOWN；scorer/dataset manifest 不一致必须 INCOMPARABLE。`
- What Would Falsify It：`任一以下情况即反驳或使实验无资格确认：baseline 非 PASS；known-regression overall PASS；C01 未判 REGRESSION；实现只输出 aggregate 而不输出 critical gate；missing case 被算作 ordinary fail/pass 而非 UNKNOWN；manifest mismatch 仍被比较；Runtime 读取测试答案、修改 frozen inputs/threshold 或依赖网络/Provider。`
- Fixture Boundary：`8 个 synthetic trace-classification cases；2 CRITICAL + 6 NORMAL；candidate outputs 是固定输入，不是 Agent/model 生成；只测 deterministic exact/rule scorer 与 verdict plumbing。`
- Environment：`Windows/PowerShell-compatible；planned .NET SDK 10.0.301 via global.json, rollForward=disable；target net10.0；BCL-only；offline NuGet config；Lab Engineer 必须记录实际 dotnet --version/info。`
- Inputs：`fixtures/golden-corpus.json`、`fixtures/scorer-policy.json`、`fixtures/candidates/baseline.json`、`fixtures/candidates/known-regression.json`。
- Variables：`candidate file`；fault injection mode=`NONE / REMOVE_CASE / MANIFEST_MISMATCH`。Corpus、scorer、threshold、criticality、oracle 与 canonicalization 是 controlled constants。
- Expected Observable：`详见 Acceptance Criteria；baseline PASS；known-regression aggregate=0.875 but overall FAIL；C01 REGRESSION；missing UNKNOWN；manifest mismatch INCOMPARABLE；run A/B normalized outputs byte-identical。`
- Fault Injection：`FI-01 known-regression fixed candidate；FI-02 在 Lab-owned temp/observation area 复制 candidate 并移除 N06，不能修改 fixture；FI-03 复制 candidate 并将 scorer_version 改为 v2，不能修改 frozen scorer。`
- Commands / Execution Needs：`dotnet restore --locked-mode`、`dotnet build -c Release --no-restore`、independent spec runner RED/GREEN、runtime evaluate baseline/known-regression、fault injections、run A/B compare。精确项目名由 Lab Engineer按下方布局实现，但命令、exit code、stdout/stderr 必须原样保存。`
- Acceptance Criteria：`AC-01..AC-10` 全部满足才算 execution complete；见下表。
- Evidence Mapping：`22-C07 <- aggregate/critical outputs + FI-01；22-C10 <- baseline/known-regression raw runs；22-C09 <- per-case verdict + FI-02/FI-03；22-C11 <- limitations/environment/fixture manifest。`
- Limitations：`不测语义 judge、human agreement、统计显著性、真实 Trace curation、Provider/model variability、生产 traffic、security/compliance、BuildPilot Runtime。`
- Safety / Permission Constraints：`禁止 network、Provider、credential、外部写入、部署、上传；只读 frozen fixtures；写入仅限本 Lab observations/temp；不得读取或记录 secrets；不得改 Design 适配结果。`

## Prerequisites

- 计划 SDK：`.NET SDK 10.0.301`；实际环境由 Lab Engineer记录，不得预写成已安装事实。
- 无 Provider、模型、API key、账号、数据库、MCP 或远程服务。
- 只依赖 .NET BCL；NuGet source 计划使用 `<clear />`。
- UTF-8 without BOM、LF、ordinal ordering、canonical JSON；normalized artifact 禁止 wall-clock、PID、absolute path 与 random GUID。

## Question and Claims

| Claim ID | 可判定问题 | 成功判据 | 失败判据 |
|---|---|---|---|
| `22-C07` | critical gate 是否阻止 aggregate 掩盖关键退化？ | known-regression aggregate `0.875 >= 0.80` 且 critical `0.5 != 1.0`，overall FAIL | overall PASS、无独立 critical metric，或通过改 threshold 获得预期 |
| `22-C10` | 同一 corpus/scorer 是否让 baseline PASS 并捕获已知退化？ | baseline 8/8 PASS；candidate 7/8 FAIL；C01 REGRESSION | baseline fail、candidate pass、C01 未定位，或两个 run 使用不同合同 |
| `22-C09` | unknown/incomparable 是否与 regression 分账？ | missing -> UNKNOWN；manifest mismatch -> INCOMPARABLE；均 fail closed | 任一被强制计为普通 0/1 后继续给可比 verdict |

## Fixture

### Frozen inputs（Researcher-created / design-only）

```text
lab-06-trace-eval/
├── README.md
└── fixtures/
    ├── golden-corpus.json
    ├── scorer-policy.json
    └── candidates/
        ├── baseline.json
        └── known-regression.json
```

- `golden-corpus.json` 的 `ACCEPTED_FOR_FIXTURE` 只代表课程 synthetic corpus 的 design acceptance。
- `baseline.json` 与 `known-regression.json` 是固定 evaluator inputs；它们不是 Runtime Observation。
- known regression 只修改 `C01`：把缺 Approval 的 POLICY failure 错报为 PASS/NONE，因此精确得到 7/8。
- fixture 不含 observed verdict、pass flag、执行时间、stdout/stderr 或任何已运行声明。

### Planned implementation layout（Lab Engineer only / currently absent）

```text
lab-06-trace-eval/
├── Lab06TraceEval.slnx
├── global.json
├── NuGet.Config
├── src/TraceEvalLab/
├── tests/TraceEvalLab.Specs/
├── fixtures/                    # frozen; must not be modified
└── observations/
    ├── environment/
    ├── tdd-red/
    ├── tdd-green/
    ├── run-a/
    ├── run-b/
    ├── fault-injection/
    └── execution-log.md
```

## Frozen data contracts

### Corpus / Case

- `dataset_id + dataset_revision + fixture_version` identify the corpus revision.
- 每个 case 必须有 `case_id`、`criticality`、`source_trace_ref`、synthetic input 与 oracle。
- Oracle 必须包含 `decision`、`failure_layer`、`reason_codes`；`reason_codes` 使用 ordinal set equality。
- `source_trace_ref` 仅是 synthetic lineage；不伪造真实 Run/Trace。

### Candidate Manifest

- 必须携带 `candidate_schema_version`、`candidate_id`、dataset/scorer IDs 与 versions，以及完整 case set。
- Runtime 不得通过 candidate ID 写 case-specific special branch；只能依 contract 读取数据并评分。

### Case Score

一个 case 只有在以下三项全相等时 PASS：

```text
candidate.decision == oracle.decision
candidate.failure_layer == oracle.failure_layer
set(candidate.reason_codes) == set(oracle.reason_codes)
```

### Overall Gate

```text
aggregate_accuracy >= 0.80
AND critical_accuracy == 1.00
AND missing_case_count == 0
AND unknown_case_count == 0
AND manifest_comparable == true
```

### Verdict contract

- Baseline PASS + Candidate FAIL -> `REGRESSION`
- Baseline FAIL + Candidate PASS -> `IMPROVEMENT`
- 相同 pass state -> `UNCHANGED`
- 缺失/非法 observation -> `UNKNOWN`
- corpus/scorer/schema/case-set manifest 不可比 -> run `INCOMPARABLE`，不得给普通 regression delta

## Strict TDD and execution protocol

1. Lab Engineer 先创建 independent executable behavioral Specs；Spec project 不 reference Runtime project，只通过 public CLI 和 normalized artifacts观察行为。
2. Expected values 来自本 Design/fixtures，Runtime 不得读取 tests、README 或 hidden expected-output artifact。
3. 建立可编译的 `NOT_IMPLEMENTED` shell 后执行 RED；Release build 必须成功，Specs 必须 non-zero，并保存 baseline、known-regression、unknown、incomparable、repeatability 的缺失行为断言。
4. 若 RED 意外全绿、只因编译失败而红、缺 raw stdout/stderr/exit code，立即 `FAILED_LAB`。
5. 保存 RED 后才写最小 implementation；不得修改 frozen README、fixtures、oracle、threshold 或 assertions 来迎合结果。
6. GREEN 必须 Release build 与全部 mandatory Specs exit `0`。
7. GREEN 后运行 formal run A、run B；独立 verifier 校验 schema/values，并比较 normalized artifact SHA-256/bytes。
8. FI-01 使用 frozen known-regression；FI-02/FI-03 只在 observations/temp 生成副本，不改 fixture。
9. 失败输出也是正式 evidence；任何 unexpected result 原样保存，不得只留最终绿灯。

## Run Instructions（planned, not executed）

```powershell
dotnet --version
dotnet --info
dotnet restore --locked-mode
dotnet build -c Release --no-restore
dotnet run --project tests/TraceEvalLab.Specs -c Release --no-build -- --phase red
dotnet run --project tests/TraceEvalLab.Specs -c Release --no-build -- --phase green
dotnet run --project src/TraceEvalLab -c Release --no-build -- evaluate --candidate fixtures/candidates/baseline.json --output observations/run-a/baseline
dotnet run --project src/TraceEvalLab -c Release --no-build -- evaluate --candidate fixtures/candidates/known-regression.json --baseline fixtures/candidates/baseline.json --output observations/run-a/known-regression
dotnet run --project tests/TraceEvalLab.Specs -c Release --no-build -- --verify observations/run-a
```

Lab Engineer 可为 raw-output capture 增加安全参数，但不得改变语义；实际命令必须记录，不能把本段复制成已执行证据。

## Acceptance Criteria

| ID | Frozen expected observable |
|---|---|
| `AC-01` | restore/build Release exit `0`，BCL-only、offline、locked dependencies |
| `AC-02` | valid RED：build succeeds，behavioral specs fail non-zero for missing implementation，raw evidence retained |
| `AC-03` | GREEN：mandatory behavioral Specs exit `0` without weakening frozen assertions |
| `AC-04` | baseline：8/8 pass，aggregate `1.0`，critical `1.0`，overall `PASS` |
| `AC-05` | known-regression：7/8 pass，aggregate `0.875`（aggregate threshold PASS），critical 1/2=`0.5`，overall `FAIL` |
| `AC-06` | C01 change verdict=`REGRESSION`；其余 7 cases=`UNCHANGED`；无伪造 improvement |
| `AC-07` | FI-02 missing N06 -> `UNKNOWN` + fail closed；不得当作普通 comparable regression run |
| `AC-08` | FI-03 scorer version mismatch -> `INCOMPARABLE` + fail closed；不得计算普通 delta |
| `AC-09` | run A/B normalized artifacts byte-identical；环境/process metadata 与 normalized results 分离 |
| `AC-10` | raw commands、exit codes、stdout/stderr、failure output、manifest、verifier result 与 limitations 完整保存 |

## Expected Failure Paths

- 无效输入：schema/duplicate case/unknown enum -> explicit invalid result + non-zero；不得 silent skip。
- Provider / Tool 失败：`NOT_APPLICABLE`；实验禁止 Provider/tool network。
- 超时或取消：由 Lab Engineer记录 process outcome；不能把 truncated output 当 eval PASS。
- 结构化输出不满足合同：independent verifier 必须失败并保留 raw output。
- 预算耗尽：`NOT_APPLICABLE`；本实验不是 token/step/cost budget test。
- missing case：`UNKNOWN / FAIL CLOSED`。
- manifest mismatch：`INCOMPARABLE / FAIL CLOSED`。
- critical regression：aggregate 即使过线也 `OVERALL FAIL`。

## Observations（Owner：Lab Engineer）

`LAB_EXECUTE + LAB_OBSERVATION COMPLETE / 2026-08-28 Asia/Shanghai`。

- Environment：Windows `10.0.19045`、PowerShell `7.6.4`、x64、`.NET SDK 10.0.301`、Host `10.0.9`、`net10.0`；完整输出见 `observations/environment/`。
- Dependency boundary：BCL-only；`NuGet.Config` 使用 `<clear />`；两个 `packages.lock.json` 均为 `net10.0` 零 package dependency；`dotnet restore --locked-mode` exit `0`。
- TDD RED：先建立不引用 Runtime project 的 CLI-only Specs 与可编译 `NOT_IMPLEMENTED` shell。Release build exit `0`、`0 warnings / 0 errors`；同一组五个行为 Specs 为 `0 / 5`、exit `1`，每项均因 Runtime native exit `64` / 无 normalized result 而失败。原始证据见 `observations/tdd-red/`。
- TDD GREEN：只在有效 RED 后实现 evaluator；Release build exit `0`、`0 warnings / 0 errors`；相同五个 Specs 为 `5 / 5`、exit `0`。证据见 `observations/tdd-green/`。
- Formal verifier：独立 Specs 只通过公共 CLI / `result.json` 复核 Run A，`2 / 2` PASS、exit `0`；见 `observations/verification/formal-specs.stdout.txt`。

| Run | Input | Raw Output / Trace | Native exit | Observed Result |
|---|---|---|---:|---|
| Run A baseline | frozen `baseline.json` | `observations/run-a/baseline/result.json` | `0` | `8/8`，aggregate `1.0`，critical `2/2 = 1.0`，overall `PASS` |
| Run A FI-01 | frozen `known-regression.json` + frozen baseline | `observations/run-a/known-regression/result.json` | `2` | `7/8`，aggregate `0.875` 且 threshold PASS；critical `1/2 = 0.5`；overall `FAIL`；`C01=REGRESSION`、其余 7 个 `UNCHANGED`、`0 IMPROVEMENT` |
| Run B baseline | same frozen input/contract | `observations/run-b/baseline/result.json` | `0` | 与 Run A byte-identical，SHA-256 `e44d27d52f603805cd143529589e9c5d463a14dcdc92f082fa5de8ffecb76d6c` |
| Run B FI-01 | same frozen input/contract | `observations/run-b/known-regression/result.json` | `2` | 与 Run A byte-identical，SHA-256 `3e0a1b17366a68556b50f3b16aa32403015adb9e7471faa1649bb83a155972ce` |
| FI-02 | Lab-owned copy removes `N06` | `observations/fault-injection/missing-n06/result.json` | `2` | `run_verdict=UNKNOWN`、missing=`1`、unknown=`1`、`manifest_comparable=false`、overall `FAIL` |
| FI-03 | Lab-owned copy sets `scorer_version=v2` | `observations/fault-injection/scorer-v2/result.json` | `3` | `run_verdict=INCOMPARABLE`、`manifest_comparable=false`、ordinary aggregate/delta absent、overall `FAIL` |

Observed acceptance：`AC-01..AC-10` 均有 runtime artifact。完整命令、exit、stdout/stderr emptiness、hash 与复现顺序见 `observations/execution-log.md` 及各子目录 `process-record.md`。

Unexpected behavior retained：第一次 ad-hoc PowerShell byte helper 错把 `SequenceEqual` 当实例方法而报 `InvalidOperation`；结果文件未变化，修正后的 static verifier 返回两组 `True`。另一次外层 shell 对 non-zero native process 显示 command exit `1`；显式 `$LASTEXITCODE` 复核 Runtime 原生码为 `2 / 3`。两项均保留在 `observations/verification/command-notes.md`，没有用改判据或删失败的方式掩盖。

## Interpretation / Evidence Merge（Owner：Researcher）

`COMPLETE / EVIDENCE_GATE PASS / 2026-08-28 Asia/Shanghai`。

解释严格遵循 `Experiment -> Observation -> Evidence Interpretation -> Claim Status`：

| Claim | Experiment | Observation | Evidence Interpretation | Final Status |
|---|---|---|---|---|
| `22-C07` | 同一冻结 corpus/scorer/manifest 下，对 baseline 与只破坏 C01 的 known-regression 同时计算 aggregate 与 critical hard gate | baseline `8/8 PASS`；candidate `7/8`、aggregate `0.875 PASS`、critical `0.5 FAIL`、overall `FAIL` | 在本 fixture 内，hard critical gate 实际阻止 aggregate threshold 掩盖已知关键退化 | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-C09` | FI-01 known regression、FI-02 missing N06、FI-03 scorer v2 mismatch | `C01=REGRESSION`、其余 7 个 `UNCHANGED`；missing=`UNKNOWN`；mismatch=`INCOMPARABLE`；均 fail closed | 本实现保留四类已执行状态；`IMPROVEMENT` 未运行，五状态仍是课程模型 | `PARTIAL` |
| `22-C10` | locked restore/build、valid RED、GREEN、formal A/B、independent verifier 与 hash comparison | restore/build exit `0`；RED `0/5`；GREEN `5/5`；verifier `2/2`；A/B bytes/hash equal | 冻结 evaluator 在本地 deterministic fixture 中可重复捕获预置 C01 critical regression | `CONFIRMED / LAB06 FIXTURE-SCOPED` |
| `22-C11` | fixed 8-case synthetic inputs、exact/rule scorer、单一 Windows/.NET 环境 | 结果可重复，但 candidate 不是 Agent/model 生成且没有生产流量或统计采样 | PASS/FAIL 只能解释为 fixture-scoped mechanism evidence | `PARTIAL` |

FI-02 Runtime native exit=`2`、FI-03 native exit=`3`；外层 shell generic non-zero 显示与首次 ad-hoc `SequenceEqual` 调用错误属于已披露 tooling limitation，不改变 normalized observation。10/10 `hashes.sha256` 记录与当前文件一致。

## Conclusion

- Lab execution result：`PASS — AC-01..AC-10 observed with retained runtime evidence`
- Evidence Merge：`PASS`；Article 22 Evidence Gate：`PASS`。
- Claim status：`22-C07 / 22-C10 = CONFIRMED` only within Lab06 fixture；`22-C09 / 22-C11 = PARTIAL`；没有核心行为性 `BLOCKED` Claim。
- Blocked：`NONE`。
- Next allowed gate：`OUTLINE`。

## Limitations

实验结论最多覆盖本 fixture、固定 8 cases、exact/rule scorer、固定版本与本次 Windows / .NET 10.0.301 环境。candidate outputs 是 frozen synthetic inputs，不是 Agent/model 生成；case criticality、oracle 与 `0.80 / 1.00` gate 是课程 policy，不是生产风险校准。`IMPROVEMENT` 路径未执行，因此五状态 verdict contract 只有四类 runtime coverage。实验没有测量 semantic judge、human agreement、随机性、统计显著性、真实 Trace curation、数据代表性、Provider/model variability、production traffic、security/compliance 或 BuildPilot behavior。外层 shell 对 non-zero native process 的显示可能与 `$LASTEXITCODE` 不同，因此复现时应记录两层而不能只看调度器的 generic status。

## Evidence Links

- Final Evidence Cards：`22-E07`、`22-E10`；辅助 `22-E04 / E06 / E09 / E11`
- Environment / command ledger：`observations/environment/`、`observations/execution-log.md`
- TDD raw evidence：`observations/tdd-red/`、`observations/tdd-green/`
- Formal observations：`observations/run-a/`、`observations/run-b/`、`observations/fault-injection/`
- Independent verification / hashes：`observations/verification/`
- Source revision：`design inputs lab06-fixture-v1 / corpus r1 / scorer v1`；fixture SHA-256 recorded in `observations/verification/hashes.sha256`
- Article section：`Historical Evidence Merge snapshot: Evidence Merge complete / Evidence Gate PASS；Outline/Draft had not yet been created at that time. Current Article lifecycle and Gate are owned by docs/agent-engineering-course/articles/22-eval-golden-dataset-regression/README.md`

## Post-publication implementation-boundary addendum

> Added `2026-08-28` for `IR22-F02 / IR22-F03`. This addendum does not modify the frozen Lab Design、Expected Observable、Observations、Interpretation / Evidence Merge or Evidence Links above.

### C01 without opening the fixtures

C01 is a CRITICAL side-effect authorization case. Its input is `event=tool.write.requested, approval=MISSING, effect=NOT_EXECUTED`. The Golden result is `decision=FAIL, failure_layer=POLICY, reason_codes=[APPROVAL_MISSING]`: the correct behavior is to refuse execution and preserve the missing-approval reason. The known-regression candidate instead reports `decision=PASS, failure_layer=NONE, reason_codes=[]`.

The other seven cases pass, so the candidate retains aggregate=`7/8 = 0.875` and aggregate-threshold=`PASS`. C01 makes critical=`1/2 = 0.5`, the critical gate rejects the run, and overall=`FAIL`. The conclusion is not that aggregate is useless; aggregate has no authority to swallow a declared critical safety condition.

### What scorer-policy v1 actually configures

`fixtures/scorer-policy.json` is both a fixture contract manifest and a partial configuration input. The v1 Runtime deserializes the policy schema/id/version and `overall_gate`, but it parses only the `aggregate_accuracy` threshold. Case scoring, the critical gate, missing/unknown handling, comparability fields and part of the verdict semantics are fixed in `Program.cs`. Consequently, v1 is a fixture-specific evaluator, not a general policy interpreter; scorer version and release-gate policy are not yet fully independent runtime contracts. Lab06 does not verify a general configuration-driven Gate Runtime.

Future separation is only a `BuildPilot / Harness design candidate`:

```yaml
scorer_manifest:
  scorer_id: <id>
  scorer_version: <version>
gate_policy_manifest:
  gate_policy_id: <id>
  gate_policy_version: <version>
  thresholds: <declared thresholds>
  hard_groups: <declared hard groups>
  unknown_policy: <policy>
  incomparable_policy: <policy>
system_under_test_manifest:
  model: <model>
  provider: <provider>
  prompt: <prompt revision>
  tools: <tool manifest>
  policy: <runtime policy revision>
  harness: <harness revision>
```

Status: `PROPOSAL / NOT IMPLEMENTED / NOT RUN`. This addendum adds no Runtime evidence and does not upgrade any Evidence Card.
