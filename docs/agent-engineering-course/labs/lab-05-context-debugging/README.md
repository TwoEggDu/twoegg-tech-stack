# Lab 05｜Context Debugging

## Metadata

- Lab ID：`lab-05-context-debugging`
- Title：`Context Debugging：Packing、Compression、Pollution 与可重建性`
- Owning Article：`13-context-debugging`
- Design Owner：`Researcher`
- Execution / Observation Owner：`Lab Engineer`
- Evidence Merge Owner：`Researcher`
- Lifecycle Status：`DESIGN_FROZEN / IMPLEMENTED / OBSERVED / EVIDENCE_MERGED / EVIDENCE_GATE_PASS`
- Evidence Status：`EVIDENCE_GATE_PASS / 3 CONFIRMED / 6 PROPOSAL / 0 BLOCKED`
- Design Freeze Date：`2026-08-22 / Asia/Shanghai`
- Runtime / Language：`C# / .NET 10 / net10.0 / BCL-only`
- Fixture Version：`lab05-fixture-v1`
- Provider / Model：`NONE / NONE`
- Network / Credentials：`NONE / NONE`
- Last Run：`2026-08-22T12:54:48.3789231+08:00 - 2026-08-22T12:54:51.0346284+08:00`
- Observed Result：`PASS / FROZEN FIXTURE CONFORMANCE`
- Interpretation：`COMPLETE / FIXTURE-SCOPED`
- Evidence Merge：`COMPLETE`

> Design、Expected、Observed 与 raw links 保持冻结。Researcher 已只依据本 README observation summary 完成 Evidence Merge：`13-C05` 仅升级为 `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED`；所有 course-design `PROPOSAL` Claim 保持 proposal，local support 单独记录。Evidence Gate=`PASS`，next allowed gate=`OUTLINE`。

## 1. Goal

用一个完全离线、确定性的 C#/.NET fixture，验证 application-visible Context 在 selection、revision、relevance、conflict preservation、compression、budget fitting 与 receipt-based reconstruction 判断中的可观察边界。

本 Lab 要回答：

1. frozen contributor / Snapshot / Receipt contract 能否区分 good、stale、polluted、conflicting、compression-loss 与 budget/truncation paths；
2. deterministic bad compressor 是否会把 `SUPPORTED + CONTRADICTS + UNKNOWN` 非法压成确定结论，且 verifier 能否从 pre/post evidence 检出 loss；
3. budget fitter 能否保留 P0/P1、先移除 optional history、保留 output reserve，并在 required Evidence 将被 trim 时 fail closed；
4. Receipt 只有 ref / digest / order / disposition，而原 bytes 与 resolvable locator 不存在时，能否诚实输出 `AUDITABLE` 但 `NOT_RECONSTRUCTABLE`；
5. 同一 binary、fixture 与 normalized schema 在两个 fresh process 中能否生成 byte-identical artifacts。

## 2. Claims, hypotheses and falsifiers

| Hypothesis | Related claims | Frozen hypothesis | What would falsify it |
|---|---|---|---|
| `H-01 Diagnostic predicates` | `13-C01`, `13-C02`, `13-C09` | Cases A–D 的 verdict 只由 frozen contributor metadata / policy / conflict facts 得出，不由模型输出质量倒推 | baseline 误报；rev14 未相对 rev17 标 STALE；irrelevant contributor 未识别；conflict 被自动选边或 provenance 丢失 |
| `H-02 Compression loss detection` | `13-C02`, `13-C05`, `13-C08`, `13-C09` | Case E 的 independent verifier 能检测 uncertainty、conflict、provenance 与 claim-strength loss | bad compressor 输出 `Root cause confirmed.` 却未被判 loss，或 verifier 只比较 source text / hard-code implementation token |
| `H-03 Fail-closed budget fitting` | `13-C02`, `13-C06`, `13-C07`, `13-C09` | Case F 保留 output reserve 与全部 P0/P1，optional history 先被移除；required Evidence 不可容纳时不生成 silent Snapshot | P0/P1 被静默 trim、reserve 被占用、optional 在 required 前保留，或 required overflow 仍返回成功 Snapshot |
| `H-04 Reconstruction ceiling` | `13-C07`, `13-C08`, `13-C09` | Case G metadata 可 audit，但没有 original bytes / resolvable locator 时 byte reconstruction 必须 false/UNKNOWN | digest 被当成 bytes、content 被凭空恢复，或 verdict 宣称 Provider-internal/full-token reconstruction |
| `H-05 Reproducibility` | `13-C09` | run A / B 的 normalized artifacts 和 manifest SHA-256 byte-identical | normalized output 含 wall clock、absolute path、PID、random ID，或任一 file/aggregate hash 不同 |

`13-C03` 与 `13-C04` 的 current-source结论不由本 Lab 升级：本 Lab 不运行真实模型，也不模拟 Provider documented behavior。

### 2.1 Canonical Lab Design fields

| Template field | Frozen value / location |
|---|---|
| Related Article | `13-context-debugging` |
| Related Claim IDs | `13-C01`—`13-C09`；C03/C04 不由 Lab 升级 |
| Research Question | Section 1 的五个 application-visible questions |
| Hypothesis | `H-01`—`H-05` |
| What Would Falsify It | Section 2 falsifier column |
| Fixture Boundary | `lab05-fixture-v1`；offline local C#/.NET、Cases A–G、application-visible artifacts only |
| Environment | Section 3 frozen Windows/.NET/BCL-only table；actual values captured at execute time |
| Inputs | `fixtures/cases.json` planned input-only records for exact Cases A–G；optional V1–V4 are non-replacing extensions |
| Variables | injected revision、relevance、conflict pair、named transform、budget pressure、bytes/locator availability；controlled fixture/binary/policy/canonicalization remain fixed |
| Expected Observable | Section 8 case matrix and Section 11 Acceptance Criteria |
| Fault Injection | B rev mismatch；C irrelevant contributors；D contradictory build facts；E `BAD_COMPRESSOR_V1`；F budget pressure；G missing bytes/locator |
| Commands / Execution Needs | Section 9；strict RED before minimal implementation, then GREEN and formal run A/B |
| Acceptance Criteria | Section 11 |
| Evidence Mapping | Section 12 |
| Limitations | Section 16 |
| Safety / Permission Constraints | Section 3；no Provider/model/network/credentials and writes only under Lab-owned observations roots |

### 2.2 Prerequisites

- .NET SDK `10.0.301` must be available and selected by the planned `global.json`；
- repository-local offline `NuGet.Config` and locked restore inputs must be created by Lab Engineer；
- Runtime CLI and independent Spec runner projects must remain BCL-only and project-reference-isolated；
- Cases A–G fixture inputs must not contain expected verdicts or hashes；
- Lab-owned output roots must be validated before any write；
- no Provider/model, network, credential or secret-valued environment access is required or allowed。

## 3. Frozen technical and safety scope

| Item | Frozen design |
|---|---|
| OS target | `Windows 10 10.0.19045 win-x64`；Lab Engineer 必须记录实际值，mismatch 不得静默忽略 |
| .NET SDK | `10.0.301` via planned `global.json`, `rollForward=disable` |
| Target framework | `net10.0` |
| Dependencies | .NET BCL only；zero third-party runtime/test packages；offline restore |
| Projects | public Runtime CLI + independent executable behavioral Spec runner；Spec project不 reference Runtime project |
| Network | prohibited；no HTTP, Provider, model, MCP, database or remote service |
| Credentials | prohibited；不得读取或记录 API key、token、cookie、secret-valued environment variables |
| Filesystem | 只读 frozen fixture；writes 仅限 Lab-owned `observations/` roots |
| Normalization | UTF-8 without BOM、LF、canonical JSON、ordinal property ordering、stable array ordering |
| Time / IDs | normalized artifacts 禁止 wall-clock、PID、absolute path、random GUID；真实 process metadata 只进入 execution log / process evidence，不参与 repeatability compare |

任何 Provider/model/network/credential attempt、写出 Lab root、修改本 Design、修改 Expected 适配结果，均使 Lab `FAILED_LAB`。

## 4. Strict TDD execution protocol

Lab Engineer 必须保留以下顺序和 raw evidence：

1. **Tests first**：先创建 independent `ContextDebuggingLab.Specs` behavioral verifier，并把 Cases A–G 的 expected values 写入 test assertions；`fixtures/cases.json` 只含 inputs / injected conditions，不得携带 expected verdict、expected hash、pass flag 或答案。
2. Specs 只能通过 planned public CLI / normalized artifacts 观察行为，不得读取 Runtime `.cs` source、README、test source text 或搜索实现 token。禁止“某字符串是否存在于源码”一类测试。
3. Specs project 不 reference Runtime project。Runtime binary 也不得读取 tests、README 或 expected-values artifact。
4. Tests 建立后，只允许创建使 public process boundary 可启动的 compile-only contract shell；它返回 `NOT_IMPLEMENTED`，不得含 case-specific behavior。
5. **RED**：Release build 必须成功，随后 behavioral Spec run 必须以 non-zero exit 失败，并至少为 Cases A–G 各保存一个源自 missing behavior 的 assertion failure。若 RED 意外全绿、只因编译错误失败、或没有 raw stdout/stderr/exit/schema result，立即停止，不能进入实现。
6. 完整保存 RED command、exit code、stdout、stderr 与 machine-readable failure summary 后，才允许写最小 Runtime implementation。
7. **GREEN**：只实现使 frozen public behaviors 通过的最小逻辑；Release build 与全部 mandatory behavioral assertions 必须 exit `0`。不得删除/放宽 RED assertions，变更 Expected 需返回 Researcher 解冻 Design。
8. GREEN 后才执行 formal run A、run B、independent artifact verification 与 SHA-256 compare。失败 case 的 expected fail-closed verdict 是测试 PASS；process crash、schema invalid 或 missing artifact 是 Lab failure。
9. execution log 必须按发生顺序保存 first RED、所有失败、最小 patch 摘要、GREEN 与 formal runs；不得只留下最终绿灯。

## 5. Planned implementation layout（not created by Researcher）

```text
lab-05-context-debugging/
├── README.md
├── ContextDebuggingLab.slnx
├── global.json
├── NuGet.Config
├── src/ContextDebuggingLab/
├── tests/ContextDebuggingLab.Specs/
├── fixtures/cases.json
└── observations/
    ├── environment/
    ├── tdd-red/
    ├── tdd-green/
    ├── run-a/
    ├── run-b/
    ├── execution-log.md
    └── repeatability.json
```

本 LAB_DESIGN Gate 不创建上述任何子文件或子目录。

## 6. Architecture and public behavior

```text
Frozen contributors + case policy
  -> Candidate selector / revision-scope-relevance checks
  -> Conflict-preserving packer
  -> optional deterministic BadCompressor seam
  -> deterministic BudgetFitter
  -> application-visible ContextSnapshot
  -> ContextReceipt
  -> DiagnosticEngine + ReconstructionEvaluator
  -> normalized case artifacts
  -> independent behavioral Spec verifier
```

- Runtime CLI 是唯一 public execution boundary；按 fixture input 生成 artifacts，不读取 Expected。
- `BadCompressor` 只在 Case E 由 named transform ID 启用；它是课程 fault injection，不模拟任何 Provider algorithm。
- `BudgetFitter` 使用 fixture-defined integer `budget_units`，不是 Provider tokenizer。`usable_input = total_budget - output_reserve`。
- `DiagnosticEngine` 对 structured pre/post facts 与 policies 判定；不读取自然语言回答质量。
- `ReconstructionEvaluator` 只能检查 retained bytes / resolvable locator / canonicalization 前提；不能用 digest 反推 bytes。

## 7. Frozen data schemas

### 7.1 Contributor

| Field | Contract |
|---|---|
| `schema_version` | `lab05-contributor-v1` |
| `contributor_id` | deterministic case-local ID |
| `kind` | `GOAL / STATE / EVIDENCE / CAPABILITY / PLAN / TOOL_RESULT / HISTORY` |
| `priority` | `P0_REQUIRED / P1_REQUIRED / P2_OPTIONAL` |
| `required` | boolean；P0/P1 为 true |
| `source_ref / source_revision / required_revision` | stable ref/revision；不适用写 `NOT_APPLICABLE` |
| `scope` | canonical object：tenant / task / step / environment / time-scope IDs |
| `authority` | `AUTHORITATIVE / ADVISORY / UNTRUSTED` |
| `relevance` | fixture input fact；`RELEVANT / IRRELEVANT / UNKNOWN` |
| `content_bytes_utf8` | fixture-provided bytes；Case G 可为 `ABSENT` |
| `content_sha256` | known bytes 的 SHA-256；bytes absent 时只可为 pre-recorded digest |
| `locator / locator_resolvable` | stable relative locator or `ABSENT`; boolean |
| `budget_units / optional_drop_rank` | deterministic integers；不代表 token count |

### 7.2 Context Snapshot

| Field | Contract |
|---|---|
| `schema_version / fixture_version / case_id` | `lab05-snapshot-v1 / lab05-fixture-v1 / A..G` |
| `selected_contributor_ids` | final application-visible order |
| `materialized_blocks` | contributor ID + canonical visible bytes/digest；不含 hidden Provider content |
| `omitted_contributors` | ID + disposition + reason + transform event ref |
| `budget` | total / output reserve / usable input / used input / remaining input units |
| `transform_event_ids` | deterministic ordered list |
| `unresolved_conflict_ids / unknown_ids` | sorted stable lists |
| `canonical_snapshot_sha256` | excludes its own digest field |

### 7.3 Context Receipt

| Field | Contract |
|---|---|
| `schema_version / fixture_version / case_id` | `lab05-receipt-v1 / lab05-fixture-v1 / A..G` |
| `snapshot_sha256` | application-visible Snapshot digest |
| `contributors[]` | ref、digest、order、scope、source/required revision、authority、disposition、reason、bytes-retained、locator/resolvable |
| `transforms[]` | actor=`APPLICATION_FIXTURE`、stage、mechanism、version、input/output digest、affected IDs |
| `budget` | same deterministic ledger as Snapshot |
| `diagnostic_refs` | stable IDs only；diagnostic bodies stored separately |
| `receipt_sha256` | canonical Receipt digest excluding itself |

Receipt 只 describe / audit / compare application-visible Snapshot；它不声称是 Provider request trace、effective internal context 或 full-token receipt。

### 7.4 Diagnostic

```text
schema_version = lab05-diagnostic-v1
diagnostic_id / case_id / code
codes = GOOD_CONTEXT | STALE | REVISION_MISMATCH | POLLUTION |
        CONFLICT_UNRESOLVED | COMPRESSION_LOSS | BUDGET_OPTIONAL_OMITTED |
        REQUIRED_EVIDENCE_BUDGET_EXCEEDED | MISSING | WRONG_SCOPE | OVERPACKED
contributor_ids / expected / actual / predicate_version
evidence_refs / pre_digest / post_digest
claim_strength_before / claim_strength_after
status = DETECTED | NOT_DETECTED
```

### 7.5 Transform, budget and reconstruction records

- `transform-event.json`：schema/version、event ID、named mechanism、affected contributors、pre/post digests、lost invariant IDs；Case E mechanism 固定 `BAD_COMPRESSOR_V1`。
- `budget-result.json`：total、reserve、usable、required sum、optional sum、selected/omitted IDs、drop reasons、status=`PACKED / FAIL_CLOSED`、failure code。
- `reconstruction-verdict.json`：`metadata_audit = AUDITABLE / NOT_AUDITABLE`；`byte_reconstruction = RECONSTRUCTABLE / NOT_RECONSTRUCTABLE / UNKNOWN`；prerequisites 包含 bytes retained、locator resolvable、canonicalization version；reason codes 包含 `ORIGINAL_BYTES_ABSENT / LOCATOR_UNRESOLVABLE / DIGEST_NOT_CONTENT`。

## 8. Frozen Cases A–G

| Case | Frozen input / fault | Expected observable | Expected verdict and ceiling |
|---|---|---|---|
| `A — Baseline Good Context` | current Goal / State `rev17`、correct Evidence/capability、bounded relevant history | valid Snapshot/Receipt；required contributors、scope、order、budget/reserve 与 digests 合规；no fault diagnostic | `GOOD_CONTEXT`; proves only the local baseline is internally consistent |
| `B — Stale Context` | authoritative/current State=`rev17`；source summary=`rev14` | preserve expected/actual revision and provenance | `STALE + REVISION_MISMATCH`; no production authority claim |
| `C — Pollution` | irrelevant old tool result、obsolete plan、unrelated history | identify all three irrelevant contributors by frozen relevance predicate；do not inspect model output | `POLLUTION`; no model-quality or “more context is worse” claim |
| `D — Conflict` | one contributor=`build failed`，another=`build succeeded` | retain both values, provenance, revision/order and unresolved marker；no auto-selection | `CONFLICT_UNRESOLVED`; no claim that a model would resolve it correctly |
| `E — Compression Loss` | pre: `EV-1 SUPPORTED`, `EV-2 CONTRADICTS`, `UNKNOWN root cause`; `BAD_COMPRESSOR_V1` outputs `Root cause confirmed.` | independent verifier detects uncertainty、conflict、provenance loss and illegal claim-strength upgrade from pre/post records | `COMPRESSION_LOSS`; future ceiling is this deterministic bad-compressor fixture only；`13-C05` remains `PARTIAL` at Design |
| `F — Truncation / Budget` | deterministic budget costs force pressure | preserve all P0/P1 and output reserve；remove P2 optional history first；if required Evidence sum exceeds usable input, return no Snapshot and fail closed | optional path=`BUDGET_OPTIONAL_OMITTED / PACKED`; required-overflow path=`REQUIRED_EVIDENCE_BUDGET_EXCEEDED / FAIL_CLOSED` |
| `G — Reconstruction Boundary` | Receipt has ref/digest/order/disposition；original contributor bytes absent and locator absent/unresolvable | metadata comparison remains possible；byte recovery is impossible/unknown；digest is never treated as content | `metadata_audit=AUDITABLE`; `byte_reconstruction=NOT_RECONSTRUCTABLE`; Provider/full-token=`UNKNOWN / UNSUPPORTED` |

### Optional variants V1–V4

| Variant | Optional predicate | Boundary |
|---|---|---|
| `V1 Missing vs intentional omission` | missing required contributor vs explicit omission reason | may be implemented only after A–G；must not replace them |
| `V2 Wrong Scope` | tenant/task/step/environment/time mismatch | frozen scope rules only |
| `V3 Overpacked` | deterministic budget threshold/reserve violation | no model-quality inference |
| `V4 Event separation` | distinguish omission、app trim、documented Provider transform/truncation metadata and hard-limit label | uses synthetic records only；does not simulate Provider internals |

## 9. Commands / execution needs

Lab Engineer must record each exact command, working directory, start/end time outside normalized artifacts, stdout/stderr path and exit code. Planned order:

```powershell
dotnet --info
dotnet --version
dotnet restore ContextDebuggingLab.slnx --configfile NuGet.Config --locked-mode
dotnet build ContextDebuggingLab.slnx --configuration Release --no-restore
dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-runtime --runtime src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll --fixtures fixtures/cases.json --output observations/tdd-red
```

The first `verify-runtime` is the mandatory RED run and must exit non-zero after a successful build. After preserving RED artifacts and writing the minimal implementation:

```powershell
dotnet build ContextDebuggingLab.slnx --configuration Release --no-restore
dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-runtime --runtime src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll --fixtures fixtures/cases.json --output observations/tdd-green
dotnet src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll run --fixtures fixtures/cases.json --output observations/run-a
dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-artifacts --input observations/run-a --report observations/run-a/spec-result.json
dotnet src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll run --fixtures fixtures/cases.json --output observations/run-b
dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-artifacts --input observations/run-b --report observations/run-b/spec-result.json
dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll compare --left observations/run-a --right observations/run-b --report observations/repeatability.json
```

Expected exits：restore/build=`0`；RED verifier=`non-zero`；post-implementation build/GREEN/run-a/verify-a/run-b/verify-b/compare=`0`。Any deviation must be preserved, not overwritten.

## 10. Raw artifact contract（planned, none exist at Design）

| Artifact | Planned schema / purpose | Repeatability set |
|---|---|---:|
| `observations/environment/dotnet-info.txt` | raw `dotnet --info` | no |
| `observations/environment/environment.json` | SDK/runtime/OS/arch/timezone/working-directory-relative root; no secret values | no |
| `observations/tdd-red/{command.json,stdout.txt,stderr.txt,result.json}` | exact RED command/exit and failed behavioral assertions by Case A–G | no |
| `observations/tdd-green/{command.json,stdout.txt,stderr.txt,result.json}` | exact GREEN command/exit and assertion totals | no |
| `observations/execution-log.md` | chronological commands, exits, first failures, patch summary and unexpected behavior | no |
| `observations/run-{a,b}/<case>/contributors.json` | canonical input contributors copied as observed runtime input | yes |
| `observations/run-{a,b}/<case>/snapshot.json` | Context Snapshot；Case F required-overflow uses explicit `ABSENT` marker file, not fabricated Snapshot | yes |
| `observations/run-{a,b}/<case>/receipt.json` | application-visible Receipt | yes |
| `observations/run-{a,b}/<case>/diagnostics.json` | sorted diagnostic records | yes |
| `observations/run-{a,b}/<case>/transform-events.json` | pre/post transform records；empty canonical array when none | yes |
| `observations/run-{a,b}/<case>/budget-result.json` | deterministic budget ledger and fail-closed status | yes |
| `observations/run-{a,b}/<case>/reconstruction-verdict.json` | metadata/byte/Provider-internal verdicts and prerequisites | yes |
| `observations/run-{a,b}/<case>/case-result.json` | case ID、status、diagnostic refs、invariant results、unexpected failures | yes |
| `observations/run-{a,b}/artifact-manifest.json` | sorted relative path、byte length、SHA-256 for every normalized file | yes |
| `observations/run-{a,b}/spec-result.json` | independent assertion IDs, pass/fail, case/artifact refs | yes |
| `observations/repeatability.json` | run A/B per-file and aggregate comparison | no; it describes compare |

### 10.1 Common raw JSON envelope

Every machine-readable artifact contains：

```text
schema_version
fixture_version = lab05-fixture-v1
case_id = A..G | SUITE
artifact_kind
records / payload
status = PASS | EXPECTED_FAIL_CLOSED | FAIL | NOT_RUN
unexpected_failures[]
```

Expected results are not stored in Runtime inputs. `spec-result.json` is produced by the independent verifier after reading public artifacts.

### 10.2 SHA-256 and repeatability plan

1. Canonical JSON uses UTF-8 without BOM, LF, stable property order and stable set ordering; no indentation variance.
2. Each run manifest hashes normalized file bytes with SHA-256. Manifest excludes itself from its file list, then records its own hash externally in `repeatability.json`.
3. Aggregate digest input is the LF-joined ordinal-sorted sequence `relative_path<TAB>byte_length<TAB>sha256` plus final LF.
4. run A / B use different validated output roots and fresh Runtime processes, but the same Release binary and fixture bytes.
5. Compare must assert identical relative file set, byte length, per-file SHA-256, aggregate SHA-256 and direct byte equality. Hash equality alone does not replace direct byte comparison.
6. Environment, TDD and execution logs are excluded because they may contain process/time metadata; all case evidence is included.

## 11. Acceptance Criteria

Lab 05 can return `LAB_OBSERVATION` complete only when all are true：

1. Actual environment, SDK/runtime, OS/arch/timezone, exact commands and exit codes are preserved; mismatch to frozen environment is explicit.
2. Restore is offline/BCL-only and network/Provider/model/credential access is zero.
3. Independent behavioral Specs were authored before behavior implementation, contain independent expected values, and neither Specs nor Runtime read README/source text/each other’s expected data.
4. A successful Release build is followed by a genuine RED run: non-zero exit and behavioral assertion failures for every mandatory Case A–G; RED raw artifacts remain after GREEN.
5. Minimal implementation is followed by GREEN build/spec exit `0`; no assertion/Expected weakening occurred.
6. Cases A–G retain exact identities and inputs in Section 8; optional V1–V4 cannot replace or renumber them.
7. A produces valid Snapshot/Receipt and no fault diagnostic；B yields `STALE / REVISION_MISMATCH` for rev17 vs rev14；C identifies all three irrelevant contributors without model-quality claim；D retains both build results/provenance and never auto-selects.
8. E preserves pre/post bytes and detects uncertainty/conflict/provenance loss plus claim-strength upgrade caused by exact `BAD_COMPRESSOR_V1` output `Root cause confirmed.`.
9. F preserves P0/P1 and output reserve, omits optional history first, records reasons, and creates no silent Snapshot when required Evidence would be trimmed; required-overflow must be explicit `FAIL_CLOSED`.
10. G returns `AUDITABLE` but `NOT_RECONSTRUCTABLE` when bytes/locator are absent and records `DIGEST_NOT_CONTENT`; no Provider/full-token claim.
11. All expected failure paths produce structured records; a crash, missing artifact, schema invalidity or unexpected failure makes the Lab fail.
12. Formal run A/B use fresh processes and distinct validated roots; every normalized file is direct-byte-identical and SHA-256-identical with matching aggregate digest.
13. Raw artifact manifest is complete, references resolvable relative files and does not contain fabricated Observed values.
14. Execution log retains RED, GREEN, all commands/exits, first failures, unexpected behavior and limitations.
15. Researcher frozen Design remains unchanged during execution; Evidence interpretation is deferred to EVIDENCE_MERGE.

## 12. Evidence Mapping and maximum post-merge ceiling

| Claim | Cases / artifacts | Required evidence | Maximum possible wording after successful merge |
|---|---|---|---|
| `13-C01` | A–D + diagnostics/receipts | layer predicates and no-auto-cause records | `CONFIRMED / FROZEN FIXTURE CONFORMANCE` only |
| `13-C02` | B–F + V1–V3 if implemented | independent multi-label predicate assertions | taxonomy remains `COURSE PROPOSAL`; only case coverage can be confirmed |
| `13-C03` | C | irrelevant contributors identified | no upgrade；no real-model quality claim |
| `13-C04` | V4 if implemented | synthetic event records | no Provider fact upgrade；current docs remain sole Provider evidence |
| `13-C05` | E pre/post + bad compressor + verifier | raw bytes/digests/invariant loss | at most `CONFIRMED / BAD_COMPRESSOR FIXTURE-SCOPED`; never Provider-wide |
| `13-C06` | F + V1/V4 if implemented | disposition/actor/stage/mechanism records | `CONFIRMED / LOCAL SCHEMA CONFORMANCE` only |
| `13-C07` | A/F/G receipts | app-visible diff, omission, fail-closed and ceiling verdict | describe/audit/compare app-visible Snapshot only |
| `13-C08` | E/G reconstruction verdicts | retained/missing prerequisites and level result | only implemented fixture levels；Provider/full-token remains UNKNOWN |
| `13-C09` | RED/GREEN + A–G + run A/B compare | exact commands, exits, artifacts, hashes | `CONFIRMED / DETERMINISTIC LOCAL FIXTURE-SCOPED` only |

Lab success never upgrades a Provider fact, proves model consumption, or proves production/cross-platform reliability.

## 13. Expected failure paths

- invalid fixture/schema/version：fail before Snapshot materialization with structured `INPUT_INVALID`；no guessing or migration；
- RED unexpectedly green, RED only compile-fails, or RED artifacts missing：`TDD_PROTOCOL_INVALID` and stop；
- Provider/network/credential attempt：`SAFETY_BOUNDARY_VIOLATION` and fail；
- unknown contributor kind/scope/revision format：structured diagnostic and fail closed；
- Case E pre/post bytes missing：cannot assess compression loss，case fails rather than infers；
- Case F required Evidence exceeds usable budget：`REQUIRED_EVIDENCE_BUDGET_EXCEEDED / FAIL_CLOSED`，no Snapshot；
- Case G bytes absent + locator absent/unresolvable：expected `NOT_RECONSTRUCTABLE`，not a process error；
- artifact schema/hash/manifest mismatch：suite fail；
- run A/B byte or hash mismatch：`NON_DETERMINISTIC_ARTIFACT`，Evidence cannot merge as reproducible；
- timeout/cancellation/process crash：preserve raw stderr/exit and return `FAILED_LAB`；do not fabricate missing case results。

## 14. Observations（Owner：Lab Engineer）

- Environment：`Microsoft Windows 10 Pro / 10.0.19045 / X64 / win-x64 / .NET SDK 10.0.301 / .NET Runtime 10.0.9 / China Standard Time +08:00`
- Commands：frozen commands were executed from the Lab root；closure reran restore、Release build、GREEN、run A、verify A、run B、verify B and compare in locked order
- Exit Codes：mandatory RED Spec=`1`、Runtime shell=`3`；closure 8/8 commands=`0`
- Build Result：`PASS`；final Release build=`0 warnings / 0 errors`
- Test Result / TDD RED：`PASS / GENUINE RED`；successful Release build was followed by 7/7 mandatory public-behavior assertion failures for Cases A–G
- Test Result / TDD GREEN：`PASS`；15/15 assertions passed without changing Spec or fixture bytes after RED
- Runtime Output：`PASS`；formal run A/B each completed in a fresh Runtime process and emitted the complete normalized artifact set
- Fault Injection Result：`PASS`；`BAD_COMPRESSOR_V1` emitted exactly `Root cause confirmed.` and the verifier detected uncertainty、conflict、provenance and claim-strength loss
- Run A / Run B：`PASS / FRESH PROCESSES`；each manifest lists 58 normalized files
- SHA-256 / Byte Compare：`PASS`；59 compared files including manifest/spec result were direct-byte and SHA-256 identical；aggregate=`621cde0ec3c1ed7b7f6334dac4d0187b522625f37e220957e48b6dcac3bd3f50`
- Observed Behavior：Cases A–G matched their mandatory public contracts；Case F required-overflow is an expected fail-closed outcome, not a Lab failure
- Unexpected Behavior：initial post-implementation build `CS0411` and the first PowerShell secondary-audit helper failure were retained and recovered；neither was hidden or counted as Lab RED
- Reproduction Notes：closure verification from `2026-08-22T12:54:48.3789231+08:00` to `2026-08-22T12:54:51.0346284+08:00` reran all eight frozen closure commands with exit `0`
- Runtime Limitations：`PASS_WITH_DISCLOSED_LIMITATIONS`；scope remains the offline deterministic fixture and does not establish Provider/model/production/cross-platform behavior

### 14.1 Actual environment

| Field | Observed value |
|---|---|
| OS | `Microsoft Windows 10 Pro / 10.0.19045 / build 19045` |
| Architecture / RID | `X64 / win-x64` |
| SDK / Runtime / TFM | `.NET SDK 10.0.301 / Microsoft.NETCore.App 10.0.9 / net10.0` |
| Timezone | `China Standard Time / +08:00` |
| Dependency boundary | `BCL_ONLY / NuGet sources CLEARED` |
| Provider / model / network / credentials | `NONE / NONE / NONE / NONE` |
| Frozen environment match | `true` |

### 14.2 Commands and exits

| Stage | Exact command | Exit / observation |
|---|---|---|
| Environment | `dotnet --info` | `0` |
| Environment | `dotnet --version` | `0`；`10.0.301` |
| Initial restore | `dotnet restore ContextDebuggingLab.slnx --configfile NuGet.Config --locked-mode` | `0` |
| Pre-implementation build | `dotnet build ContextDebuggingLab.slnx --configuration Release --no-restore` | `0` |
| Mandatory RED | `dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-runtime --runtime src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll --fixtures fixtures/cases.json --output observations/tdd-red` | Spec=`1`；Runtime=`3`；A–G 7/7 failed |
| Post-implementation build attempt 01 | `dotnet build ContextDebuggingLab.slnx --configuration Release --no-restore` | `1`；compiler `CS0411` at three overloaded `JsonValue.Create` method-group sites |
| Post-implementation build attempt 02 | same Release build command | `0`；`0 warnings / 0 errors` |
| GREEN | `dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-runtime --runtime src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll --fixtures fixtures/cases.json --output observations/tdd-green` | `0`；15/15 passed |
| Run A | `dotnet src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll run --fixtures fixtures/cases.json --output observations/run-a` | `0` |
| Verify A | `dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-artifacts --input observations/run-a --report observations/run-a/spec-result.json` | `0` |
| Run B | `dotnet src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll run --fixtures fixtures/cases.json --output observations/run-b` | `0` |
| Verify B | `dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-artifacts --input observations/run-b --report observations/run-b/spec-result.json` | `0` |
| Compare | `dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll compare --left observations/run-a --right observations/run-b --report observations/repeatability.json` | `0`；direct bytes、per-file SHA-256 and aggregate SHA-256 equal |

The locked closure repeated restore、build、GREEN、run A、verify A、run B、verify B and compare in the order above；all eight exits were `0`。The build reported `0 warnings / 0 errors`，and GREEN reported 15/15 assertions passed.

### 14.3 TDD RED / GREEN evidence

| Run | Input | Raw Output / Trace | Result |
|---|---|---|---|
| `TDD RED` | frozen Specs + input-only fixture + compile/start shell | [`tdd-red/result.json`](observations/tdd-red/result.json)、[`command.json`](observations/tdd-red/command.json)、[`stdout.txt`](observations/tdd-red/stdout.txt)、[`stderr.txt`](observations/tdd-red/stderr.txt) | `PASS / GENUINE RED`；Spec `1`、Runtime `3`、A–G 7/7 failed |
| `TDD GREEN` | unchanged Spec/fixture bytes + minimal implementation | [`tdd-green/result.json`](observations/tdd-green/result.json)、[`command.json`](observations/tdd-green/command.json)、[`stdout.txt`](observations/tdd-green/stdout.txt)、[`stderr.txt`](observations/tdd-green/stderr.txt) | `PASS`；exit `0`、15/15 passed |
| `run-a` | `fixtures/cases.json` / fresh Runtime process | [`run-a/artifact-manifest.json`](observations/run-a/artifact-manifest.json)、[`run-a/spec-result.json`](observations/run-a/spec-result.json) | `PASS`；58 manifest-listed normalized files |
| `run-b` | `fixtures/cases.json` / fresh Runtime process | [`run-b/artifact-manifest.json`](observations/run-b/artifact-manifest.json)、[`run-b/spec-result.json`](observations/run-b/spec-result.json) | `PASS`；58 manifest-listed normalized files |
| `compare` | run A vs run B | [`repeatability.json`](observations/repeatability.json) | `PASS`；59 files compared including manifest/spec result；direct bytes and SHA-256 equal |

### 14.4 Mandatory Cases A–G

| Case | Observed public behavior | Result |
|---|---|---|
| A | baseline emits `GOOD_CONTEXT`；required contributors、ordering ledger and output reserve are retained | `PASS` |
| B | `STALE` + `REVISION_MISMATCH` retain `expected=rev17`、`actual=rev14`、source ref and provenance | `PASS` |
| C | `POLLUTION` identifies old tool result、obsolete plan and unrelated history (`C-OLD-TOOL`、`C-OBSOLETE-PLAN`、`C-UNRELATED-HISTORY`) | `PASS` |
| D | `CONFLICT_UNRESOLVED` retains `Build failed.` and `Build succeeded.` with distinct `build-job-41` / `build-job-42` provenance；no automatic side selection | `PASS` |
| E | `BAD_COMPRESSOR_V1` emits exact `Root cause confirmed.`；verifier detects `UNCERTAINTY`、`CONFLICT`、`PROVENANCE` and `CLAIM_STRENGTH` loss | `PASS` |
| F | optional-pressure omits optional history first、retains all P0/P1 and four output units；required-overflow returns `REQUIRED_EVIDENCE_BUDGET_EXCEEDED / FAIL_CLOSED` with explicit `ABSENT` Snapshot | `PASS / EXPECTED_FAIL_CLOSED` |
| G | Receipt metadata remains `AUDITABLE`；missing original bytes and resolvable locator yields `NOT_RECONSTRUCTABLE` with `ORIGINAL_BYTES_ABSENT`、`LOCATOR_UNRESOLVABLE` and `DIGEST_NOT_CONTENT`；Provider-internal remains `UNKNOWN_UNSUPPORTED` | `PASS` |

### 14.5 Repeatability and recovered tooling failures

- run A and run B each contain 58 manifest-listed normalized files. Compare covers 59 files including the manifest and independently produced Spec result.
- Relative file sets、byte lengths、direct bytes、per-file SHA-256 and aggregate SHA-256 all match. Both aggregate values are `621cde0ec3c1ed7b7f6334dac4d0187b522625f37e220957e48b6dcac3bd3f50`.
- The initial environment/restore/build wrapper preserved raw streams and exits but not start timestamps. The closure rerun records exact start/end times for the same commands.
- A non-escalated command helper failed before process creation with `helper_unknown_error`; elevated local execution recovered the command path. This infrastructure recovery is not Lab RED.
- The first post-implementation build failed with `CS0411` at three `JsonValue.Create` method-group call sites. Raw compiler output is preserved；the minimal recovery used explicit lambdas, after which the Release build passed.
- The first PowerShell secondary-audit helper attempted unsupported `ReadOnlySpan<byte>` casts and is preserved as `INVALID`. The corrected helper used structural byte-array equality and returned `PASS`.

## 15. Interpretation / Evidence Merge（Owner：Researcher）

- Interpretation：`COMPLETE / FIXTURE-SCOPED`
- Evidence Merge：`COMPLETE`
- Claim Status Changes：`13-C05 PARTIAL -> CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED`
- Proposal Claims：`UNCHANGED AS PROPOSAL / LOCAL SUPPORT RECORDED SEPARATELY`
- Core BLOCKED Claims：`0`

| Claim | Experiment -> Observation -> Evidence Interpretation -> Claim Status |
|---|---|
| `13-C01` | A-D,F-G -> mandatory application-visible behaviors conformed -> supports local layer routing but not consumption/internal cause -> `PROPOSAL / LOCAL SUPPORT PARTIAL`. |
| `13-C02` | A-G -> mandatory predicates conformed -> does not cover all optional labels or create a Provider taxonomy -> `PROPOSAL / LOCAL SUPPORT PARTIAL`. |
| `13-C03` | no model experiment -> no Lab evidence for model reliability -> keep current-source result -> `CONFIRMED / NO LAB UPGRADE`. |
| `13-C04` | Provider/model/network NONE -> no product behavior observed -> keep current product-doc result -> `CONFIRMED / NO LAB UPGRADE`. |
| `13-C05` | Case E BAD_COMPRESSOR_V1 -> exact output plus four detected loss dimensions -> narrow broad risk to direct fixture fact -> `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED`. |
| `13-C06` | Case F -> local dispositions and fail-closed event retained -> V4/Provider event paths not observed -> `PROPOSAL / LOCAL SUPPORT PARTIAL`. |
| `13-C07` | A,F,G -> Receipt describes/audits/compares local Snapshot and G refuses reconstruction -> local ceiling conforms -> `PROPOSAL / LOCAL SUPPORT CONFIRMED`. |
| `13-C08` | E,G -> transform loss plus L0 audit/L1 negative boundary -> L2/L3 not fully tested and L4 unsupported -> `PROPOSAL / LOCAL SUPPORT PARTIAL`. |
| `13-C09` | TDD RED/GREEN + A-G + run A/B -> 59 files byte/hash identical and recovered failures retained -> frozen local protocol conforms -> `PROPOSAL / LOCAL SUPPORT CONFIRMED`. |

### Proves

- `lab05-fixture-v1` 的 mandatory Cases A-G、fail-closed budget、application-visible Receipt ceiling 与 normalized repeatability 按 README summary 成立。
- `BAD_COMPRESSOR_V1` 的四类 loss detection 是 direct fixture evidence。

### Does Not Prove / retained limitations

- 不证明任何 Provider compaction、真实模型质量、生产/跨平台/分布式行为或 Provider-internal/full-token reconstruction。
- Missing/Wrong Scope/Overpacked/V4 variants 未由 summary 记录为执行，不能写 full taxonomy/event coverage。
- budget units 非 Provider tokens；Case G 是 synthetic missing-bytes/locator condition。
- recovered `CS0411`、`helper_unknown_error`、timestamp gap 与 invalid PowerShell audit helper 保持披露；不改写为 Lab RED 或隐藏成功。

Full maximum wording、counter-evidence、Claim traceability 与 Lab Evidence Cards 见 Article 13 `evidence.md`。

## 16. Limitations and evidence ceiling

- 只覆盖 `lab05-fixture-v1`、planned Windows/.NET host、BCL-only deterministic implementation 与 named fault；
- budget units 是人工 deterministic units，不是 OpenAI/Anthropic tokenizer 或 billing tokens；
- bad compressor 是刻意错误的 local transform，不代表任何 Provider compaction algorithm；
- no Provider/model/network/credentials，因此不验证真实模型准确率、attention、hallucination、context rot、hidden prompt、server transform 或 internal token sequence；
- Receipt 只支持 application-visible Snapshot 的 describe/audit/compare；不保证 semantic equivalence、decision replay、Provider-internal/full-token reconstruction；
- direct-byte/SHA repeatability 只证明 normalized local artifacts，不证明生产 determinism、availability、performance、cross-platform 或 distributed behavior；
- 不覆盖 concurrency、large-scale context、security、multi-tenant isolation、retention service 或 production observability；
- 不进入 Article 14 Working Memory，也不进入 Articles 15–16 Memory / RAG 生命周期、检索或知识新鲜度。
- initial command wrapper 的时间戳缺口由 final closure rerun 的精确 start/end time 补足，但不会回写或伪造首次命令时间；
- recovered `CS0411` build failure、non-escalated helper failure 与 invalid PowerShell audit helper 均是已披露 tooling / implementation failures，不扩展 Lab behavior 结论。

## 17. Conclusion（Lab Observation Gate）

- Design：`FROZEN`
- Expected：`FROZEN`
- Observed：`PASS / A-G FROZEN FIXTURE CONFORMANCE`
- Interpretation：`COMPLETE / FIXTURE-SCOPED`
- Evidence Merge：`COMPLETE`
- Lab execution / observation：`COMPLETE`
- Evidence Gate：`PASS / ARTICLE EVIDENCE_READY`
- Blocker：`NONE`
- Next Gate：`OUTLINE / AUTHOR`

Lab 05 的 frozen offline fixture 已完成真实 TDD RED/GREEN、Cases A–G、fresh-process run A/B 和 direct-byte/SHA-256 repeatability observation。Researcher 已完成 scoped Evidence Merge 与 Evidence Gate；next allowed gate=`OUTLINE`，所有 Design/Expected/Observed/raw links 保持冻结。

## 18. Evidence Links

- Preliminary Evidence：`docs/agent-engineering-course/articles/13-context-debugging/evidence.md`
- Research Register：`docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Chronological execution log：[`observations/execution-log.md`](observations/execution-log.md)
- Environment：[`observations/environment/environment.json`](observations/environment/environment.json)、[`dotnet-info.txt`](observations/environment/dotnet-info.txt)
- TDD RED：[`observations/tdd-red/result.json`](observations/tdd-red/result.json)、[`command.json`](observations/tdd-red/command.json)、[`source-state.json`](observations/tdd-red/source-state.json)
- TDD GREEN：[`observations/tdd-green/result.json`](observations/tdd-green/result.json)、[`command.json`](observations/tdd-green/command.json)
- Formal artifacts：[`run-a/artifact-manifest.json`](observations/run-a/artifact-manifest.json)、[`run-a/spec-result.json`](observations/run-a/spec-result.json)、[`run-b/artifact-manifest.json`](observations/run-b/artifact-manifest.json)、[`run-b/spec-result.json`](observations/run-b/spec-result.json)
- Repeatability：[`observations/repeatability.json`](observations/repeatability.json)
- Closure verification：[`observations/final-verification/closure-verification.json`](observations/final-verification/closure-verification.json)、[`independent-audit.json`](observations/final-verification/independent-audit.json)
- Limitations：[`observations/limitations.json`](observations/limitations.json)
- Source：[`src/ContextDebuggingLab/Program.cs`](src/ContextDebuggingLab/Program.cs)、[`LabRuntime.cs`](src/ContextDebuggingLab/LabRuntime.cs)
- Tests：[`tests/ContextDebuggingLab.Specs/Program.cs`](tests/ContextDebuggingLab.Specs/Program.cs)
- Fixture：[`fixtures/cases.json`](fixtures/cases.json)
- Article section：`Article 13 / NOT DRAFTED`
