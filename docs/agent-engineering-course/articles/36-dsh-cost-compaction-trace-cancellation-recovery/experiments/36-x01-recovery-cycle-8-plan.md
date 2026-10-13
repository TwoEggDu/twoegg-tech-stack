# Article 36 / 36-X01 Recovery Cycle 8 plan

Status: `DESIGN ONLY / NO CYCLE 8 EXECUTION`. This plan preserves the entire Cycle 7 Tests file and empty RED scene. It does not backfill, delete, rerun, reinterpret, or complete Cycle 7. It does not change the 36-X01 hypothesis, acceptance criteria, safety boundary, pinned DSH baseline, frozen harness, or frozen collector. Article 37 is not started.

## 1. Root-cause ruling and YAGNI architecture decision

Cycle 7 failed before its first receipt. The only repository-durable facts are the frozen Tests identity and the exact empty directory scene. The worker reported a PowerShell overload-resolution failure at `JsonSerializer.Serialize`, but because no owner/result/stdout/stderr/exit receipt exists, the exact invocation, host, exit, output, and dynamic cause remain `NOT_DURABLY_PROVABLE`. This plan uses that report only to choose a smaller risk surface; it does not upgrade the report into runtime evidence.

The repeated Cycle 5, 6, and 7 failures challenge the architecture, not merely one overload:

- a general serializer, JSON schema catalog, module boundary, reusable receipt model, reflection helper, or after-the-fact command reconstruction is rejected;
- a separate fifth bootstrap module is rejected because it would create another identity and loading boundary before RED;
- JSON is not used by the bootstrap at all, not merely avoided in one function;
- only three production responsibilities survive the YAGNI test: a crash-surviving child recorder, an Article36-specific command runner, and an independent fixed verifier;
- the fourth file is the immutable Tests artifact. Its `Bootstrap` entry is test infrastructure, not production recorder behavior.

Future Cycle 8 may therefore create exactly four source files:

1. `36-x01-recovery-cycle-8-recorder.Tests.ps1`: immutable test contract plus the direct-byte bootstrap entry;
2. `36-x01-recovery-cycle-8-recorder.ps1`: minimal child-process capture used by compatibility and formal execution;
3. `36-x01-recovery-cycle-8-runner.ps1`: Article36-only `Compatibility` / `Formal` orchestration;
4. `36-x01-recovery-cycle-8-verify.ps1`: fixed-mode independent recomputation after the target process has settled.

Their combined UTF-8 byte count MUST be `<= 24576`. No fifth library, schema, model, helper module, or generalized framework is allowed. A source edit after the accepted GREEN abandons Cycle 8; compatibility/formal may not silently adopt a new identity.

## 2. Frozen baseline and evidence ceiling

| item | frozen value |
|---|---|
| Course repository main / local HEAD / origin/main / live main at design start | `b27b4057fa87f64bc11110cd2cc9e69d738b1f19` |
| DSH repository | `https://github.com/deepseek-ai/deepseek-harness` |
| DSH tag | `dsh-v0.1.2-alpha.1` |
| DSH commit | `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| DSH fixture | `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` |
| absolute Corepack | `C:\nvm4w\nodejs\corepack.cmd` |
| Corepack bytes / SHA-256 | `218 / 920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F` |
| frozen harness | `36-x01-recovery-cycle-2-instrumentation.ts` |
| harness bytes / SHA-256 | `34852 / D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B` |
| frozen collector | `36-x01-recovery-cycle-2-collector.ps1` |
| collector bytes / SHA-256 | `6398 / D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045` |
| Cycle 7 plan bytes / SHA-256 | `38714 / 7CA33D19E8E719E9991A54E9433671BB601F205B24EF39ADE4D32BE5848B65AE` |
| Cycle 7 Tests bytes / SHA-256 | `3614 / 27897F0F23526BD202E35DE229FA1C352AAC5ADC66B59AC953DDF7E04DA536C1` |
| Cycle 7 RED | exactly `owners/`, `execution/`, `execution/bootstrap-process/`; `0` files |
| Cycle 7 production / GREEN / compatibility / formal | `ABSENT` |

The selected host PowerShell is late-bound at implementation preflight: the caller resolves one absolute `pwsh.exe`, records its byte count and SHA-256 before the disposable probe, and requires the same path/identity for probe, official RED, GREEN, compatibility, approval, and formal. The path observed during this design read is `C:\Users\IGG\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe`, `301368` bytes, SHA-256 `DB6DD81183FE57D22E03B911EC9A30A2FD7C40542E97743615355A6FB44F458F`; it is a design-time observation, and any implementation-time drift fails closed rather than being accepted as a new baseline.

Runtime Claims remain blocked until the one formal execution and later Evidence Merge. A bootstrap probe, official RED, GREEN, compatibility PASS, source hash, owner test, or process receipt is infrastructure evidence only.

## 3. Write roots, owners, and immutable history

Let `A=docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery`, `E=A/experiments`, and `P=E/preflight/36-x01-recovery-cycle-8-tests`.

| stage / role | only allowed writes |
|---|---|
| original initial-design Researcher | `E/36-x01-recovery-cycle-8-plan.md` only at `EXPERIMENT_DESIGN`; this historical identity is unchanged and grants no design-Finding repair authority |
| fresh design Reviewer / recheck Reviewer | none |
| fresh design Revision Worker | `E/36-x01-recovery-cycle-8-plan.md` only for bounded design Findings at `REVISION`, including the required plan-level Revision Disposition; no implementation or execution writes |
| fresh Cycle 8 implementation Revision Worker | the four source files in section 1; exact parent receipt `E/preflight/36-x01-recovery-cycle-8-tests/source-identity.kv`; exact sibling markers `E/preflight/36-x01-recovery-cycle-8-tests/red.bootstrap.kv` and `E/preflight/36-x01-recovery-cycle-8-tests/green.bootstrap.kv`; exact receipt roots `E/preflight/36-x01-recovery-cycle-8-tests/red/` and `E/preflight/36-x01-recovery-cycle-8-tests/green/`; `E/preflight/36-x01-recovery-cycle-8/attempt-1/`; and the one isolated TEMP probe root below |
| isolated bootstrap probe | only `C:\Users\IGG\AppData\Local\Temp\dsh-a36-c8-bootstrap-prevalidation-<nonce>\`; the actual `<nonce>` is one lower-case 32-hex GUID persisted in its first marker |
| fresh preflight Reviewer | none |
| Master `REVIEW_RECHECK` transition / state projection | after a preflight PASS only, delete exactly the validated successful TEMP probe container; append exactly one bounded approval block in `A/subagent-trace.md`; update only the four canonical current-state files already owned by Master; never write `F`, `SPEC`, `ART`, an official preflight root, or an old experiment root |
| formal Lab Engineer | after the pre-formal absence check, create and own new `F=E/raw/36-x01/recovery-cycle-8/attempt-1/`, including the fixed topology and `F/owners/source-hashes.tsv` in sections 9-10; create only the exact relative temporary spec in the pinned fixture and the exact owned `TEMP/dsh-article36-x01-artifacts/` harness output root |
| Lab Observation / Evidence Merge | only the existing role-owned Article36 observation/evidence paths allowed by the Factory contracts; no receipt mutation |

`<nonce>` above is notation for the runtime-owned GUID component; angle brackets are forbidden in an actual path or receipt. The Revision Worker is logical owner of `P`, both official marker/root pairs, compatibility, and the probe; the immutable Tests `Bootstrap` entry is the byte creator of each bootstrap marker/root under that owner. The Revision Worker itself is byte creator and owner of `P/source-identity.kv` under the fixed grammar in section 7. It must persist each expanded absolute root, its own canonical execution ID, and nonce. It must not write under Cycle 1-7. A pre-existing Cycle 8 RED, GREEN, compatibility, formal, marker, source-identity receipt, or TEMP probe target is a conflict: preserve it, create no child, and stop. No `git clean`, broad deletion, rename, overwrite, or replay is allowed.

### 3.1 Protected Cycle 1-7 identity anchors

Every Cycle 1-7 file and directory is read-only. The following independently read design-time anchors are mandatory inputs to later compatibility and fresh review; a mismatch is `HISTORICAL_EVIDENCE_CHANGED` and stops before formal.

| cycle | path relative to `E` | bytes | SHA-256 |
|---|---|---:|---|
| original | `36-x01-long-session-compaction-cancel-resume.md` | 22585 | `2C7377DB3EEC959487CC33EACADC55D2697020092EF97E2F56CD5ECF73F1852C` |
| original | `raw/36-x01/manifest.txt` | 1702 | `A6A4B5F48BD28757B45B3862A12BD6BA3B4DD3701B1F9656167F177D9196E203` |
| 1 | `raw/36-x01/recovery-cycle-1/manifest.txt` | 2308 | `C33CF93D24E6A99C481D7168DEA3CFCC4CA499219E5595A14E9DB7DA398A6474` |
| 1 | `raw/36-x01/recovery-cycle-1/post-review-disposition.md` | 4399 | `ECD3E7CCDCE02D400B80F7D4E9B9CA13EA66DAB5D9ED3DF0C03AE6D965BA0B5D` |
| 2 | `36-x01-recovery-cycle-2-plan.md` | 7264 | `F23EB8CB77625E09E0E46E2856BDBA1F65969D70A6D212B3531200279B94C92A` |
| 2 | `36-x01-recovery-cycle-2-instrumentation.ts` | 34852 | `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B` |
| 2 | `36-x01-recovery-cycle-2-collector.ps1` | 6398 | `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045` |
| 2 | `raw/36-x01/recovery-cycle-2/final-manifest.json` | 3424 | `271BE29E95822D7AFFC241EFE896A279AA60692FF225496FB9E9961019EEE637` |
| 2 | `raw/36-x01/recovery-cycle-2/post-review-disposition.md` | 6904 | `119B501AACBD41771A70F1CE844C3B0B5A0E3245CA7994F1D24B0A2C16D9F487` |
| 3 | `36-x01-recovery-cycle-3-plan.md` | 17073 | `F5A9770EDE088BCA3B17AF3C06E5EDED46DA859BAFE349B887AAA4B077E47336` |
| 3 | `36-x01-recovery-cycle-3-plan-defect-addendum.md` | 19330 | `208F4F7EFE7B65663AF940FE16694107825799D659EF6D3C2921A8256A0C73ED` |
| 3 | `36-x01-recovery-cycle-3-launcher.ps1` | 55531 | `2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E` |
| 3 | `preflight/36-x01-recovery-cycle-3-launcher/attempt-1/manifest.json` | 3948 | `4F81810A40552C7623BA2552CB7400BABF44C554BF5ED5FEB63FEE1B318A8DAB` |
| 3 | `preflight/36-x01-recovery-cycle-3-launcher/attempt-2/manifest.json` | 1430 | `E9DE132AFB24D0C44938905A8227A7862E1F55B1FD56FD882741E6272D77B284` |
| 4 | `36-x01-recovery-cycle-4-plan.md` | 25381 | `7A68CE03E6D6894C6CA5113715BC61B47987C5DA0DE2A8B5A31865CAF04AD104` |
| 4 | `36-x01-recovery-cycle-4-orchestrator.ps1` | 42672 | `E78DEBF39728DCE6B9CCE79524D2734D310C2C488DCBAF104C925F3F8D921314` |
| 4 | `36-x01-recovery-cycle-4-orchestrator.Tests.ps1` | 22785 | `8C137687B2977D85E96E5EA68637C28A166658309528E2F21E86C88D56F02571` |
| 4 | `preflight/36-x01-recovery-cycle-4-orchestrator-tests/red/manifest.json` | 1655 | `863D5B2A9808E1A64FC0062748F86C4E5B3B54C4E12A80842B4FE385EE6EEE59` |
| 4 | `preflight/36-x01-recovery-cycle-4-orchestrator-tests/green/manifest.json` | 1538 | `3EC872C6649A4064027E449CCFEE9CFAD623994271CE84431975C648429B2AE4` |
| 4 | `preflight/36-x01-recovery-cycle-4-orchestrator/attempt-1/manifest.json` | 5674 | `CD7495F2E5C9EBA8E1B22F5BA9679BAF1333ACBAB4B229998D13597E25B634E7` |
| 5 | `36-x01-recovery-cycle-5-plan.md` | 21952 | `B0DD65CF4FC3C06A88EF23B3CE010ACC5F91E1C774903551B98316151EF288AA` |
| 5 | `36-x01-recovery-cycle-5-diagnostic.ps1` | 19626 | `D5AF4F2FF3F11A4BB06DD18D7357989722E851733D2F56220B2E5AFA25107274` |
| 5 | `36-x01-recovery-cycle-5-diagnostic.Tests.ps1` | 10787 | `E042BC774E717ABEE31EE9B2BB297A5C4A49063C211B221C75D000BFB797219F` |
| 5 | `preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/manifest.json` | 1086 | `AC990F709427C60E2B9C312C8C41144A9A74E88E1A1DC33F870222459C555AF7` |
| 5 | `preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/manifest.json` | 1077 | `7A5C834D04F200502BAC6D3D2F2C1380E343DB1EDC04FA79163AB22CB989412A` |
| 6 | `36-x01-recovery-cycle-6-plan.md` | 34525 | `DE82A0056E1832A8E9AC3E280022593961B421327E1474EBF0C3C646E111F16A` |
| 6 | `36-x01-recovery-cycle-6-receipt-schemas.json` | 7078 | `91E9EFCA5E67498825C78C9D14BB85D046478C8CBF5DDF40045A0EF05E67AB1A` |
| 6 | `36-x01-recovery-cycle-6-cycle5-defect-witness.json` | 1885 | `ECFE687E331E722EED88A56BBD4F4A38C5273833A07D194EF15C915D607D4744` |
| 6 | `36-x01-recovery-cycle-6-receipt-closure.Tests.ps1` | 23082 | `20E149429B1DF9AEB8E25BC237FFAD735DEEB17FC6AFC87078D784BE04022109` |
| 7 | `36-x01-recovery-cycle-7-plan.md` | 38714 | `7CA33D19E8E719E9991A54E9433671BB601F205B24EF39ADE4D32BE5848B65AE` |
| 7 | `36-x01-recovery-cycle-7-recorder.Tests.ps1` | 3614 | `27897F0F23526BD202E35DE229FA1C352AAC5ADC66B59AC953DDF7E04DA536C1` |

Structural anchors are equally frozen:

- Cycle 5 diagnostic `attempt-1`, raw Cycle 5, and raw Cycle 6 are absent.
- Cycle 6 receipt-closure parent contains only the empty `red/` directory; Cycle 6 GREEN, diagnostic attempt, production serializer module, and closure candidate are absent.
- Cycle 7 RED has exactly three descendant directories named above and zero files; Cycle 7 recorder, runner, verifier, GREEN, compatibility, and formal roots are absent.
- The compatibility `formal-source-hashes.tsv` must encode all file anchors plus the structural assertions; it may not treat current bytes as a replacement baseline when any fixed anchor differs.

## 4. Exact bootstrap artifact and first-marker path

### 4.1 One immutable file, two entry points

`36-x01-recovery-cycle-8-recorder.Tests.ps1` has one parameter surface:

```text
Entry = Bootstrap | Subject
Case = Probe | Red | Green
MarkerPath = absolute file path
ReceiptRoot = absolute directory path
PwshPath = frozen absolute pwsh.exe
AttemptNonce = 32 lower-case hex
OwnerExecutionId = canonical Revision Worker execution/task ID
ExpectedTestsSha256 = 64 uppercase hex
```

Actual command lines expand every value; literal aliases, angle brackets, `same`, or omitted arrays are forbidden. `Bootstrap` starts the same file as a child with `Entry=Subject` and the selected `Case`. The ordered child argv is exactly:

```text
-NoLogo
-NoProfile
-File
<absolute Cycle 8 Tests path>
-Entry
Subject
-Case
<Probe|Red|Green>
-MarkerPath
<same expanded marker path>
-ReceiptRoot
<same expanded receipt root>
-PwshPath
<same expanded pwsh path>
-AttemptNonce
<same nonce>
-OwnerExecutionId
<same exact Revision Worker execution/task ID>
-ExpectedTestsSha256
<same SHA>
```

The notation is descriptive only. Every actual value is expanded before launch and encoded as a row in the first marker. `Subject/Probe` writes exact stdout `C8_BOOTSTRAP_PROBE_STDOUT`, exact stderr `C8_BOOTSTRAP_PROBE_STDERR`, and exits `23`. `Subject/Red` requires all three production files absent, writes only stderr `RECORDER_CONTRACT_UNIMPLEMENTED`, and exits `1`; presence of any production file exits `2` with `RED_PRODUCTION_PRESENT`. `Subject/Green` executes the immutable GREEN assertions.

### 4.2 First executable code path

Before the first marker, `Bootstrap` may perform only PowerShell parameter binding, direct scalar checks, construction of the fixed string-array above, UTF-8/Base64 encoding with explicitly typed `byte[]`, and opening the already-parented `MarkerPath`. It MUST NOT call `JsonSerializer`, `ConvertTo-Json`, `JsonDocument`, `JsonNode`, a schema/catalog, `Import-Module`, dot-source another file, reflection, `Get-Command`, hashing, Git, Node, Corepack, a package manager, DSH, or an ambiguous generic/collection `Add` overload.

Its first filesystem mutation is the following semantic sequence, implemented locally in Tests rather than imported:

```text
marker text = fixed ordered lines
marker bytes = UTF8(no BOM, LF only)
File.Open(MarkerPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)
FileStream.Write(byte[], 0, byteCount)
FileStream.Flush(true)
FileStream.Dispose()
```

The marker grammar is not loaded from a schema. It is direct ordered text:

```text
H<TAB>C8-BOOTSTRAP-PREPARING-V1
K<TAB>0001<TAB>creator<TAB>Base64(UTF8(Tests/Bootstrap))
K<TAB>0002<TAB>owner_role<TAB>Base64(UTF8(REVISION_WORKER))
K<TAB>0003<TAB>owner_execution_id<TAB>Base64(UTF8(value))
K<TAB>0004<TAB>attempt_nonce<TAB>Base64(UTF8(value))
K<TAB>0005<TAB>marker_path<TAB>Base64(UTF8(value))
K<TAB>0006<TAB>receipt_root<TAB>Base64(UTF8(value))
K<TAB>0007<TAB>tests_path<TAB>Base64(UTF8(value))
K<TAB>0008<TAB>expected_tests_sha256<TAB>Base64(UTF8(value))
K<TAB>0009<TAB>subject_executable<TAB>Base64(UTF8(value))
K<TAB>0010<TAB>working_directory<TAB>Base64(UTF8(value))
K<TAB>0011<TAB>timeout_seconds<TAB>Base64(UTF8(value))
K<TAB>0012<TAB>case<TAB>Base64(UTF8(value))
K<TAB>0013<TAB>argv_count<TAB>Base64(UTF8(value))
A<TAB>0001<TAB>Base64(UTF8(argv[0]))
... one contiguous A row per ordered token
```

Only after `Flush(true)` and close may Bootstrap create `ReceiptRoot`, compute Tests/pwsh identity, query host facts, allocate `ProcessStartInfo`, or start the child. It uses no JSON anywhere in Bootstrap, including error paths. The result never serializes argv; it references the already durable marker SHA and repeats the actual child exit as an invariant-culture decimal scalar.

### 4.3 Bootstrap inventory and precedence

For Probe, RED, and GREEN the sibling marker and receipt root have one owner nonce. Bootstrap CreateNew-writes all files. The closed base inventory is:

```text
<marker path>                         first, outside ReceiptRoot
ReceiptRoot/stdout.bin               raw child bytes
ReceiptRoot/stderr.bin               raw child bytes
ReceiptRoot/exception.kv             NONE or exact exception type/message in Base64
ReceiptRoot/identity.kv              actual Tests and pwsh path/bytes/SHA plus host
ReceiptRoot/ready.kv                 identity checked; immediately before Process.Start
ReceiptRoot/result.kv                authoritative only after pumps/flushes/closes
ReceiptRoot/inventory.tsv            self-excluding path/bytes/SHA inventory
```

On any post-marker bootstrap exception, timeout, pump failure, flush failure, or close failure, CreateNew-write `ReceiptRoot/emergency.kv`; `result.kv` is absent or ignored. A partial result never wins over emergency. If the root itself cannot be created, the flushed sibling marker remains the minimum durable invocation/ownership evidence. If even that marker is absent after the caller attempted the one-shot command, the result remains `NOT_DURABLY_PROVABLE` and still receives no retry. Creator validation is deliberately asymmetric: marker `creator` must equal parent `expected_bootstrap_creator` and literal `Tests/Bootstrap`; parent `creator` remains independently equal to `REVISION_WORKER` and is never compared with marker `creator`. Linkage equality is otherwise only between genuinely corresponding fields: marker `owner_role` / `owner_execution_id` equal the same-named parent fields; marker `tests_path` / `expected_tests_sha256` equal parent `tests_path` / `tests_sha256`; marker `subject_executable` and the later `identity.kv` actual pwsh path/bytes/SHA equal the parent pwsh fields; official RED/GREEN marker `marker_path` / `receipt_root` equal the selected parent RED/GREEN path pair; and marker `attempt_nonce` equals the caller's immutable allocation and every corresponding nonce field in that marker/root receipt set. The Probe marker/root instead equal the caller-frozen TEMP path allocation and are never compared with the official RED/GREEN path fields. Any missing field, cross-field comparison, or mismatch is terminal ownership failure.

`result.kv` records marker SHA, expected/actual Tests and pwsh identity, case, start attempted/actual, signed 32-bit child exit or `NOT_AVAILABLE`, timeout, kill attempted/succeeded, still-running state, stdout/stderr pump completion, all stream flush states, child/process/stream handle-close states, exception discriminator, and bootstrap exit. The bootstrap exit is `0` only for complete capture, independent of the child's expected RED/probe exit; `90` is recorder-integrity failure and `124` is bounded timeout after successful kill/drain/close. Any failed kill, living child, pump fault/timeout, flush/close failure, or unexpected bootstrap exception is `90`.

The child is started only after `ready.kv` is CreateNew-written and `Flush(true)` has completed. Stdout and stderr pumps start concurrently with the child. After natural child termination, or after one process-tree kill and at most 5 seconds to confirm death, one shared monotonic pump deadline is set to `terminal_observed + 5s`; both pumps must finish by that same deadline, not one 5-second allowance per stream. Handles then close within the enclosing bootstrap allowance. There is no sequential pump multiplication or unbounded wait.

## 5. Isolated TEMP bootstrap prevalidation

Fresh design review must first return an explicit design decision `APPROVED`. Only then may a fresh Revision Worker create the Cycle 8 Tests file, compute its exact bytes/SHA, prove all three production files absent, and run one independent probe.

The probe container is exactly a new direct child of `[System.IO.Path]::GetTempPath()` named `dsh-a36-c8-bootstrap-prevalidation-<nonce>`. It is outside the course repository, pinned fixture, `SPEC`, `ART`, and every official RED/GREEN/compatibility/formal root. The container parent may be created before invocation; both `probe.bootstrap.kv` and `probe/` must be absent. The exact Tests file is called with `Entry=Bootstrap / Case=Probe`; no copy and no generated substitute are allowed.

PASS requires:

1. marker exists and decodes to the exact expanded child argv in section 4;
2. marker is the first file by contract and points to the exact Tests and pwsh identities;
3. ready precedes attempted child start;
4. child actually starts, stdout and stderr equal the two fixed probe lines, and child exit is exactly `23`;
5. bootstrap exit is `0`; pumps, flushes, closes, identity, exception=`NONE`, and inventory recomputation all pass;
6. the Probe causes zero writes to `P/red.bootstrap.kv`, `P/red/`, `P/green.bootstrap.kv`, `P/green/`, compatibility, or formal; `P` contains only its already-frozen `source-identity.kv`; production files, fixture, Corepack, pnpm, Vitest, DSH, `SPEC`, and `ART` have zero calls/writes.

This is one probe attempt and does not consume official RED because it does not execute `Subject/Red`, assert a missing production contract, or write the RED root. It is not allowed to prove recorder behavior or any 36-X01 Claim.

On probe failure, preserve the exact TEMP container and stop before RED; no cleanup, edit, second probe, official attempt, or later Reviewer can authorize its deletion. On probe PASS, make the TEMP container read-only by policy and retain it unchanged through RED, GREEN, compatibility, and fresh preflight review. The fresh Reviewer is strictly read-only and recomputes the marker, identity, result, streams, and inventory against the unchanged Tests SHA.

Only a Reviewer `REVIEW_RECHECK / PASS` authorizes the following Master deterministic transition action while the durable current gate is still `REVIEW_RECHECK`. Its canonical executor is `MASTER_ORCHESTRATOR / MASTER_DETERMINISTIC`; its only non-repository mutation is removal of the one exact successful probe container, and its only repository writes are bounded records in `A/subagent-trace.md`, `A/README.md`, `docs/agent-engineering-course/README.md`, `docs/agent-engineering-course/status.md`, and `docs/agent-engineering-course/course-run-state.md`. Before removal it validates the immediate system-TEMP parent, exact basename+nonce, marker nonce/root/owner identity, source-identity link, no reparse point anywhere in the closed tree, closed inventory, and Probe PASS. It then removes only that root and proves it absent. A failed Probe is ineligible and always preserved. Cleanup failure or partial residue is preserved as-is, emits `BLOCKED / false / NONE / HUMAN_DECISION_REQUIRED`, writes no approval block, and forbids formal. Cleanup PASS is recorded before the same Master action appends the one bounded approval block; the block stores the verified probe inventory SHA and `probe_cleanup=PASS`. Master then emits the closed success envelope in section 11 and only its validated transition permits `LAB_EXECUTE`.

## 6. Honest TDD ordering and one-shot budgets

Prevalidation is a test of the test-capture infrastructure, not a test of the production contract. TDD remains honest because production recorder/runner/verifier do not exist during probe or RED, and the same frozen Tests bytes that passed Probe execute official RED unchanged.

The only allowed ordering is:

1. fresh design Reviewer `APPROVED`;
2. create Tests only, freeze its bytes/SHA, then create and freeze the parent source-identity receipt while all production files remain absent;
3. run one TEMP `Probe` through its exact `Bootstrap` entry; do not edit Tests afterward;
4. prove official RED marker/root absent and production files absent; run one official `Bootstrap/Red`;
5. accept RED only from complete bootstrap evidence with child exit `1`, exact discriminator, and all production files still absent;
6. create recorder, runner, verifier; Tests bytes remain identical;
7. run one official `Bootstrap/Green`; freeze the four source identities;
8. run one compatibility attempt; freeze manifests and formal source hashes;
9. stop for one fresh preflight Reviewer;
10. only a preflight PASS permits the Master deterministic `REVIEW_RECHECK` transition to validate and remove the successful TEMP probe, durably record cleanup PASS, write the approval block, and then authorize one formal execution.

Every production-recorder call passes an explicit `PumpGraceSeconds`; there is no default or inferred value. A recorder-call completion cap is `subject TimeoutSeconds + PumpGraceSeconds`, where the grace is counted once for the two concurrently running pumps. GREEN fixes the exact `(TimeoutSeconds,PumpGraceSeconds)` tuples `(10,5)` for expected-absent start, `(15,5)` for exit-23, and `(30,5)` for its verifier. Compatibility and formal values are frozen row by row in section 9.

Attempt counts and caps are strict:

| stage | attempt budget | child / leaf cap | enclosing cap | total cap |
|---|---:|---:|---:|---:|
| TEMP Probe | 1 | probe child `10s` | bootstrap `20s` | caller `25s` |
| official RED | 1 | RED subject `30s` | bootstrap `40s` | caller `45s` |
| GREEN | 1 | absent/exit-23/verifier recorder completions `15s + 20s + 35s` | GREEN subject `80s`; bootstrap `90s` | caller `95s` |
| compatibility | 1 | six leaves including grace `140s` | runner `155s`; top `165s`; verifier `40s` | `205s` |
| formal | 1 | formal leaves `500s` | runner `530s`; top `540s`; verifier `70s` | `610s` |

Required inequalities are closed:

```text
Probe: 20 >= 10 + 10; 25 >= 20 + 5
RED: 40 >= 30 + 10; 45 >= 40 + 5
GREEN: 80 >= (15 + 20) + 35 + 10; 90 >= 80 + 10; 95 >= 90 + 5
Compatibility: 155 >= 140 + 15; 165 >= 155 + 10; 205 >= 165 + 40
Formal: 530 >= 500 + 30; 540 >= 530 + 10; 610 >= 540 + 70
```

Any timeout or mismatch preserves its root and consumes that stage. There is no in-cycle source fix, root deletion, rerun, backfill, or renamed attempt.

## 7. Official RED and GREEN acceptance

Before Probe, RED, or production creation, the Revision Worker proves `P` absent, creates only that parent, and direct-byte CreateNew-writes `P/source-identity.kv` with `Flush(true)`. It is both creator and logical owner of this one parent receipt; no child process or Reviewer writes it. Its fixed `KV1` grammar has exactly 23 contiguous `K` rows:

```text
H<TAB>C8-TEST-SOURCE-IDENTITY-V1
K<TAB>0001<TAB>creator<TAB>Base64(UTF8(REVISION_WORKER))
K<TAB>0002<TAB>expected_bootstrap_creator<TAB>Base64(UTF8(Tests/Bootstrap))
K<TAB>0003<TAB>owner_role<TAB>Base64(UTF8(REVISION_WORKER))
K<TAB>0004<TAB>owner_execution_id<TAB>Base64(UTF8(actual canonical execution/task ID))
K<TAB>0005<TAB>plan_path<TAB>Base64(UTF8(expanded repository path))
K<TAB>0006<TAB>plan_bytes<TAB>Base64(UTF8(invariant decimal))
K<TAB>0007<TAB>plan_sha256<TAB>Base64(UTF8(uppercase 64-hex))
K<TAB>0008<TAB>tests_path<TAB>Base64(UTF8(expanded repository path))
K<TAB>0009<TAB>tests_bytes<TAB>Base64(UTF8(invariant decimal))
K<TAB>0010<TAB>tests_sha256<TAB>Base64(UTF8(uppercase 64-hex))
K<TAB>0011<TAB>pwsh_path<TAB>Base64(UTF8(expanded absolute path))
K<TAB>0012<TAB>pwsh_bytes<TAB>Base64(UTF8(invariant decimal))
K<TAB>0013<TAB>pwsh_sha256<TAB>Base64(UTF8(uppercase 64-hex))
K<TAB>0014<TAB>red_marker_path<TAB>Base64(UTF8(expanded P/red.bootstrap.kv))
K<TAB>0015<TAB>red_receipt_root<TAB>Base64(UTF8(expanded P/red))
K<TAB>0016<TAB>green_marker_path<TAB>Base64(UTF8(expanded P/green.bootstrap.kv))
K<TAB>0017<TAB>green_receipt_root<TAB>Base64(UTF8(expanded P/green))
K<TAB>0018<TAB>recorder_path<TAB>Base64(UTF8(expanded production path))
K<TAB>0019<TAB>recorder_initial_state<TAB>Base64(UTF8(ABSENT))
K<TAB>0020<TAB>runner_path<TAB>Base64(UTF8(expanded production path))
K<TAB>0021<TAB>runner_initial_state<TAB>Base64(UTF8(ABSENT))
K<TAB>0022<TAB>verifier_path<TAB>Base64(UTF8(expanded production path))
K<TAB>0023<TAB>verifier_initial_state<TAB>Base64(UTF8(ABSENT))
```

The Worker computes the actual values before the write, and the receipt contains no literal explanatory notation shown above. It is immutable after close. Its own `creator=REVISION_WORKER` proves who wrote this parent receipt; its distinct `expected_bootstrap_creator=Tests/Bootstrap` is the sole source-identity field compared with a bootstrap marker's `creator`. Probe, RED, and GREEN must all match its corresponding plan/Tests/pwsh/owner identities under section 4.3; official RED and GREEN additionally use exactly its four recorded marker/root paths. Production remains absent through accepted RED; GREEN and compatibility compare the later three production identities without modifying this original absence receipt.

### 7.1 RED

Official RED parent is `E/preflight/36-x01-recovery-cycle-8-tests/`; marker is exactly `E/preflight/36-x01-recovery-cycle-8-tests/red.bootstrap.kv`; receipt root is exactly `E/preflight/36-x01-recovery-cycle-8-tests/red/`. The parent may exist because it contains the immutable source identity receipt, but marker/root must be absent before the one call.

RED is `EXPECTED_FAIL` only when:

- bootstrap inventory in section 4 is exact and independently recomputable;
- Tests identity equals the prevalidated immutable bytes/SHA;
- bootstrap child started; child exit=`1`; bootstrap exit=`0`;
- stderr contains exactly `RECORDER_CONTRACT_UNIMPLEMENTED` plus one platform newline and stdout is empty;
- recorder, runner, and verifier were absent before, during, and after the child;
- `Subject/Red` created no child file;
- fixture/package/DSH/formal invocation and write counts are zero.

An empty root, generic exit 1, marker-only result, wrong discriminator, production presence, identity drift, or incomplete pump/close evidence is terminal unexpected RED. Preserve it and stop.

### 7.2 GREEN

GREEN parent is the same fixed parent; marker is exactly `E/preflight/36-x01-recovery-cycle-8-tests/green.bootstrap.kv`; root is exactly `E/preflight/36-x01-recovery-cycle-8-tests/green/`. `Subject/Green` performs only these fixed assertions:

1. the immutable Tests SHA still equals Probe and RED;
2. exactly three production files exist and the four-file total is within `24576` bytes;
3. recorder expected-absent start failure produces `EXPECTED_ABSENT`, an actual `System.ComponentModel.Win32Exception`, actual start false, child exit `NOT_AVAILABLE`, and complete emergency-safe evidence;
4. recorder-started pwsh child exits `23`; recorder remains `0`; stdout/stderr and signed child exit remain separate;
5. each recorder writes its first sibling marker before owner parsing, identity/hash, or process start and uses no JSON/module/schema path;
6. after both leaf recorder roots settle, `Subject/Green` starts the verifier through a third recorder call with subject timeout `30s` and `PumpGraceSeconds=5`; verifier directly verifies only the fixed GREEN execution subtree and writes sibling `assessment/`, never a root it owns;
7. verifier recomputes an exact self-excluding manifest and writes `four-file-identities.tsv` for Tests, recorder, runner, verifier;
8. owner files preserve every argv token as contiguous Base64 UTF-8 `A` rows; empty argv is zero A rows plus `argv_count=0`, not `null`, an omitted field, a truthiness check, or JSON;
9. all stream, timeout, kill, flush, close, result/emergency precedence, and unexpected-file checks pass.

GREEN child exit and bootstrap exit must both be `0`. The accepted GREEN freezes all four source files. Compatibility is forbidden after any source drift.

## 8. Minimal production recorder, runner, and verifier

### 8.1 Fixed receipt grammar

Control files use UTF-8 without BOM and LF only. `KV1` is fixed text, not a loaded schema:

```text
H<TAB><FIXED-TYPE>-V1
K<TAB><four-digit contiguous ordinal><TAB><lower_ascii_key><TAB><canonical Base64 UTF-8 value>
A<TAB><four-digit contiguous ordinal><TAB><canonical Base64 UTF-8 argv token>
```

Keys are unique; values contain no literal TAB/CR/LF; Base64 must round-trip exactly. `MANIFEST-V1` and `SOURCE-HASHES-V1` use:

```text
H<TAB><TYPE>-V1
F<TAB><four-digit ordinal><TAB><Base64 normalized relative path><TAB><FILE|EMPTY_DIR|ABSENT><TAB><decimal bytes|NA><TAB><uppercase SHA256|NA>
```

Paths use `/`, have no drive, leading slash, `.`/`..`, TAB/CR/LF, duplicate, or case-insensitive duplicate. Manifest rows are lexical, contiguous, self-excluding, and cover the fixed target root only. New Cycle 8 infrastructure writes no JSON projection. Existing frozen harness/collector JSON artifacts remain unchanged.

### 8.2 Recorder interface and first marker

The caller CreateNew-writes one owner KV1 before each recorder launch. It contains role, nonce, absolute executable, optional script or `NONE`, cwd, timeout, expected start, expected signed child exit or `NA`, expected executable bytes/SHA or `EXPECTED_ABSENT`, expected start exception or `NONE`, host marker, and every ordered argv token. The caller hashes the closed owner.

Recorder CLI has only six scalar values:

```text
-OwnerPath
-OwnerSha256
-OwnerNonce
-MarkerPath
-ReceiptRoot
-PumpGraceSeconds
```

Before its first marker the recorder does not parse/hash the owner. It direct-byte CreateNew-writes and `Flush(true)`s `MarkerPath` containing those six caller-supplied scalars. Only afterward does it create `ReceiptRoot`, parse the fixed owner grammar, validate owner SHA/nonce, compute executable identity, write/flush READY, and call `Process.Start`.

`PumpGraceSeconds` is a required invariant-culture positive integer and must equal the exact call-site value in sections 6, 7, and 9. `TimeoutSeconds` is the hard process-phase cap from start attempt through natural terminal state or the single bounded kill plus confirmed death; kill handling cannot add time beyond that cap. Both asynchronous stdout/stderr pumps start at process start. Once natural termination is observed, or the bounded kill completes and death is observed, the recorder captures one monotonic `pump_deadline = terminal_observed + PumpGraceSeconds`; both pumps share that same deadline and must be complete by it. It does not await one stream for a full grace and then grant another grace to the other stream. The recorder receipt records the passed grace, process-phase deadline/outcome, terminal-observed instant/tick, shared pump deadline, and separate pump outcomes; the arithmetic counts grace exactly once per recorder call.

The deliberately absent executable contract is valid only with `expected_start=N`, `expected_exit=NA`, `expected_identity=EXPECTED_ABSENT`, and expected exception `System.ComponentModel.Win32Exception`. It still reaches READY and actually calls `Process.Start`; an absent executable under any other contract is recorder-integrity failure. Normal and failure inventories and exit precedence equal section 4.3. Every signed 32-bit child exit is preserved; recorder exit never replaces it.

### 8.3 Article36-specific runner

Runner accepts only `Mode=Compatibility|Formal`, one exact owner path/SHA/nonce, fixed output root, fixed fixture root, and, for Formal only, the approval-block SHA plus frozen SPEC/harness/collector paths and the exact pre-created `CollectorDestination`. It has no arbitrary command list, plugin system, receipt schema, or extension hook. Its command rows are source-coded from section 9. Formal validates the destination owner and emptiness but never creates, replaces, or writes beneath that destination itself.

Top recorder owns only `execution/top-process` and its sibling marker. Runner validates the top owner, proves `execution/subject` absent, creates it, and owns nested owner/marker/receipt roots. After runner exit and top pumps/handles settle, caller launches the verifier through a distinct recorder in sibling `verifier-process`; verifier alone creates sibling `assessment`. Top, subject, verifier, and assessment roots never overlap.

### 8.4 Independent verifier

Verifier supports exactly `Green`, `Compatibility`, and `Formal`. It cannot write beneath any target. It reparses fixed KV1/manifest grammars, recomputes file bytes/SHA and source identities, validates every expected/missing file, verifies count/budget/ownership/exit invariants, and writes only its sibling assessment. It never trusts runner summaries or mutable whole-trace hashes. Formal takes the two disjoint read-only targets `ExecutionRoot=F/execution` and `CollectorDestination=D`; it writes `F/assessment/execution-manifest.tsv` covering only the first tree, `F/assessment/collector-manifest.tsv` covering every file beneath `D`, `F/assessment/evidence-map.tsv`, and `F/assessment/result.kv`. The evidence map has one fixed row per frozen acceptance item with source kind `EXECUTION|COLLECTOR`, normalized target-relative path, decimal bytes, uppercase SHA-256, and observed/missing discriminator. Every `COLLECTOR` row must resolve beneath `D` and match both the frozen collector stdout receipt and `collector-transfer.tsv`; every execution row must resolve beneath `F/execution`. It cannot change acceptance criteria, infer a Claim, or use a runner summary as evidence.

## 9. Compatibility and formal launch matrix

Aliases are descriptive: `W=E`; `FIX=C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`; `PS=the approval-frozen absolute pwsh.exe`; `REC/RUN/VER/TST=the four Cycle 8 files`; `CORE=C:\nvm4w\nodejs\corepack.cmd`; `GIT=the compatibility-resolved absolute git.exe`; `NODE=the compatibility-resolved absolute node.exe`; `SPEC=packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts`; `ART=[System.IO.Path]::GetTempPath()/dsh-article36-x01-artifacts`; `COL=E/36-x01-recovery-cycle-2-collector.ps1`; `C=E/preflight/36-x01-recovery-cycle-8/attempt-1`; `F=E/raw/36-x01/recovery-cycle-8/attempt-1`; `D=F/collector-destination`. Actual owners contain expanded absolute paths, except the formal helper token `SPEC`, which remains the exact fixture-relative token.

`D` is the frozen collector's dedicated destination and is never `F/execution`, `F/execution/subject`, a recorder receipt root, or an assessment root. After proving pre-allocation `F`, `SPEC`, and `ART` absent, the authorized formal Lab caller creates exactly `F/owners/`, empty `F/execution/`, and empty `D/`; it then CreateNew-writes `F/owners/source-hashes.tsv` and `F/owners/collector-destination.owner`. The latter is fixed KV1 containing Lab role/execution ID, formal nonce, expanded `D`, expected initial state `EMPTY_DIR`, expanded `ART`, frozen collector path/bytes/SHA, and `allowed_byte_writer=FROZEN_COLLECTOR`. The caller re-enumerates `D` as zero immediate and recursive entries immediately before F0. No other process may write beneath `D`; the runner may only validate it and pass it unchanged to F11, and the verifier may only read it.

The exact formal topology and byte owners are closed:

```text
F/owners/source-hashes.tsv                 Lab caller; CreateNew before F0
F/owners/collector-destination.owner       Lab caller; CreateNew before F0
F/owners/top.owner                         Lab caller; CreateNew before F0
F/execution/top.bootstrap.kv               top recorder first marker
F/execution/top-process/**                 top recorder receipt root
F/execution/subject/**                     runner and its nested recorders
F/collector-destination/**                 initially EMPTY; frozen collector only, during F11
F/owners/verifier.owner                    Lab caller; CreateNew after F0 settles
F/verifier.bootstrap.kv                    verifier recorder first marker
F/verifier-process/**                      verifier recorder receipt root
F/assessment/**                            verifier only
```

Paths not listed are unexpected. `F/assessment` and `F/verifier-process` are absent until their owning post-F0 processes create them. The Lab caller owns the new attempt root and owner files but cannot place payload bytes in `D`; runner ownership of `execution/subject` cannot cross into `D`; the frozen collector cannot write any sibling; verifier cannot write any target.

Compatibility executes once:

| id | subject | exact ordered subject argv | cwd | timeout / pump grace / expected child |
|---|---|---|---|---|
| C0 | `PS -File RUN` | `-NoLogo,-NoProfile,-File,RUN,-Mode,Compatibility,-OwnerPath,C/owners/top.owner,-OutputRoot,C/execution/subject,-FixtureRoot,FIX` | `FIX` | `155 / 10 / 0` |
| C1 | deliberately absent executable | empty argv | `FIX` | `10 / 5 / NA`, expected start false + Win32Exception |
| C2 | `PS` | `-NoLogo,-NoProfile,-Command,exit 23` | `FIX` | `15 / 5 / 23` |
| C3 | `GIT` | `--version` | `FIX` | `15 / 5 / 0` |
| C4 | `NODE` | `--version` | `FIX` | `10 / 5 / 0` |
| C5 | `CORE` | `pnpm,--version` | `FIX` | `30 / 5 / 0` |
| C6 | `CORE` | `pnpm,exec,vitest,--version` | `FIX` | `30 / 5 / 0` |
| C7 | `PS -File VER` | `-NoLogo,-NoProfile,-File,VER,-Mode,Compatibility,-OwnerPath,C/owners/verifier.owner,-TargetRoot,C/execution,-AssessmentRoot,C/assessment` | `FIX` | `30 / 10 / 0` |

C5 and C6 independently require the same absolute Corepack path and SHA frozen in section 2. Compatibility cannot approve a replacement. PASS also requires the exact pwsh/Tests/three-production identities, GREEN identity equality, protected Cycle1-7 catalog, DSH fixture pin/clean state, `SPEC` and `ART` absence, manifest recomputation, and zero focused/collector/DSH calls.

Formal executes once after preflight approval:

| id | subject | exact ordered subject argv | cwd | timeout / pump grace / expected child |
|---|---|---|---|---|
| F0 | `PS -File RUN` | `-NoLogo,-NoProfile,-File,RUN,-Mode,Formal,-OwnerPath,F/owners/top.owner,-ApprovalBlockSha256,ABSHA,-OutputRoot,F/execution/subject,-FixtureRoot,FIX,-RelativeSpec,SPEC,-HarnessPath,E/36-x01-recovery-cycle-2-instrumentation.ts,-CollectorPath,COL,-CollectorDestination,D` | `FIX` | `530 / 10 / 0` |
| F1 | `NODE` | `--version` | `FIX` | `10 / 5 / 0` |
| F2 | `CORE` | `pnpm,--version` | `FIX` | `30 / 5 / 0` |
| F3 | `GIT` | `config,--get,remote.origin.url` | `FIX` | `10 / 5 / 0` |
| F4 | `GIT` | `rev-parse,HEAD` | `FIX` | `10 / 5 / 0` |
| F5 | `GIT` | `rev-parse,dsh-v0.1.2-alpha.1^{commit}` | `FIX` | `10 / 5 / 0` |
| F6 | `GIT` | `status,--short,--branch` | `FIX` | `10 / 5 / 0` |
| F7 | `GIT` | `diff,--check` | `FIX` | `10 / 5 / 0` |
| F8 | `PS` | `-NoLogo,-NoProfile,-Command,Get-FileHash,-Algorithm,SHA256,SPEC` | `FIX` | `10 / 5 / 0` |
| F9 | `GIT` | `diff,--no-index,--,/dev/null,SPEC` | `FIX` | `20 / 5 / 1` |
| F10 | `CORE` | `pnpm,exec,vitest,run,SPEC,--reporter=verbose` | `FIX` | `180 / 10 / 0` |
| F11 | `PS -File COL` | `-NoLogo,-NoProfile,-File,COL,-SourceRoot,ART,-DestinationRoot,D` | `FIX` | `60 / 10 / 0` |
| F12 | `GIT` | `config,--get,remote.origin.url` | `FIX` | `10 / 5 / 0` |
| F13 | `GIT` | `rev-parse,HEAD` | `FIX` | `10 / 5 / 0` |
| F14 | `GIT` | `rev-parse,dsh-v0.1.2-alpha.1^{commit}` | `FIX` | `10 / 5 / 0` |
| F15 | `GIT` | `status,--short,--branch` | `FIX` | `10 / 5 / 0` |
| F16 | `GIT` | `diff,--check` | `FIX` | `10 / 5 / 0` |
| F17 | `PS -File VER` | `-NoLogo,-NoProfile,-File,VER,-Mode,Formal,-OwnerPath,F/owners/verifier.owner,-ApprovalBlockSha256,ABSHA,-ExecutionRoot,F/execution,-CollectorDestination,D,-AssessmentRoot,F/assessment,-SourceHashes,F/owners/source-hashes.tsv` | `FIX` | `60 / 10 / 0` |

In both matrices, the middle numeric component is the literal `-PumpGraceSeconds` scalar passed to the recorder for that row; it is not appended to the subject argv. The caller and verifier require the same value in the first marker and final receipt. No row inherits a prior/default value.

These preserve the original formal helper semantics: no `git -C`, no substituted internal hash helper, exact explicit `Get-FileHash`, exact no-index patch command, unchanged `--reporter=verbose`, fixed Corepack, frozen harness, and frozen collector.

Compatibility leaf arithmetic is exactly `(10+5) + (15+5) + (15+5) + (10+5) + (30+5) + (30+5) = 140s`; the runner cap is `140+15=155s`, the C0 recorder cap is `155+10=165s`, C7 is `30+10=40s`, and caller total is `165+40=205s`. Formal leaf arithmetic is exactly `(10+5) node + (30+5) pnpm + 10*(10+5) ten ordinary Git helpers + (10+5) Get-FileHash + (20+5) no-index + (180+10) focused + (60+10) collector = 500s`; the runner cap is `500+30=530s`, F0 is `530+10=540s`, F17 is `60+10=70s`, and caller total is `540+70=610s`. Each parenthesis is `TimeoutSeconds + one shared PumpGraceSeconds`; section 8.2 makes the kill/death phase inclusive in `TimeoutSeconds`, so no hidden per-stream or kill term exists.

## 10. Formal source hashes, approval, ART/SPEC, and failure preservation

### 10.1 Formal source-hashes

Compatibility verifier writes `C/assessment/formal-source-hashes.tsv`. It contains, in fixed order:

1. current Cycle 8 plan identity as independently read by Master;
2. Tests, recorder, runner, verifier identities frozen by GREEN;
3. original 36-X01 design, harness, collector, and every file anchor in section 3.1;
4. explicit structural `EMPTY_DIR` / `ABSENT` rows for Cycle 5-7 states in section 3.1;
5. DSH commit/tag/fixture identity, absolute pwsh identity, and absolute Corepack identity in fixed KV rows following the file table.

No placeholder string is allowed in the actual file: every FILE row has decimal bytes and uppercase 64-hex SHA; EMPTY_DIR/ABSENT rows use literal `NA/NA`. Compatibility fails rather than writing a new baseline for a mismatch. Master validates the compatibility file and binds its SHA into the approval block but never creates or writes beneath `F`. After that approval and a final absence check, the formally authorized Lab Engineer reads the exact approved bytes from `C/assessment/formal-source-hashes.tsv`, opens `F/owners/source-hashes.tsv` with `FileMode.CreateNew`, writes the byte array, calls `Flush(true)`, closes it, and requires source/destination byte and SHA equality before F0.

The file table has exactly 52 rows after its header, in this order. Rows 01-05 take their actual byte/SHA values from the approved Cycle 8 identity set; rows 06-36 must equal the fixed values in sections 2 and 3.1; rows 37-52 use `NA/NA`. These are value-source rules, not strings that may appear in the artifact.

```text
01 FILE 36-x01-recovery-cycle-8-plan.md
02 FILE 36-x01-recovery-cycle-8-recorder.Tests.ps1
03 FILE 36-x01-recovery-cycle-8-recorder.ps1
04 FILE 36-x01-recovery-cycle-8-runner.ps1
05 FILE 36-x01-recovery-cycle-8-verify.ps1
06 FILE 36-x01-long-session-compaction-cancel-resume.md
07 FILE raw/36-x01/manifest.txt
08 FILE raw/36-x01/recovery-cycle-1/manifest.txt
09 FILE raw/36-x01/recovery-cycle-1/post-review-disposition.md
10 FILE 36-x01-recovery-cycle-2-plan.md
11 FILE 36-x01-recovery-cycle-2-instrumentation.ts
12 FILE 36-x01-recovery-cycle-2-collector.ps1
13 FILE raw/36-x01/recovery-cycle-2/final-manifest.json
14 FILE raw/36-x01/recovery-cycle-2/post-review-disposition.md
15 FILE 36-x01-recovery-cycle-3-plan.md
16 FILE 36-x01-recovery-cycle-3-plan-defect-addendum.md
17 FILE 36-x01-recovery-cycle-3-launcher.ps1
18 FILE preflight/36-x01-recovery-cycle-3-launcher/attempt-1/manifest.json
19 FILE preflight/36-x01-recovery-cycle-3-launcher/attempt-2/manifest.json
20 FILE 36-x01-recovery-cycle-4-plan.md
21 FILE 36-x01-recovery-cycle-4-orchestrator.ps1
22 FILE 36-x01-recovery-cycle-4-orchestrator.Tests.ps1
23 FILE preflight/36-x01-recovery-cycle-4-orchestrator-tests/red/manifest.json
24 FILE preflight/36-x01-recovery-cycle-4-orchestrator-tests/green/manifest.json
25 FILE preflight/36-x01-recovery-cycle-4-orchestrator/attempt-1/manifest.json
26 FILE 36-x01-recovery-cycle-5-plan.md
27 FILE 36-x01-recovery-cycle-5-diagnostic.ps1
28 FILE 36-x01-recovery-cycle-5-diagnostic.Tests.ps1
29 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/manifest.json
30 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/manifest.json
31 FILE 36-x01-recovery-cycle-6-plan.md
32 FILE 36-x01-recovery-cycle-6-receipt-schemas.json
33 FILE 36-x01-recovery-cycle-6-cycle5-defect-witness.json
34 FILE 36-x01-recovery-cycle-6-receipt-closure.Tests.ps1
35 FILE 36-x01-recovery-cycle-7-plan.md
36 FILE 36-x01-recovery-cycle-7-recorder.Tests.ps1
37 ABSENT preflight/36-x01-recovery-cycle-5-diagnostic/attempt-1
38 ABSENT raw/36-x01/recovery-cycle-5
39 ABSENT raw/36-x01/recovery-cycle-6
40 EMPTY_DIR preflight/36-x01-recovery-cycle-6-receipt-closure-tests/red
41 ABSENT preflight/36-x01-recovery-cycle-6-receipt-closure-tests/green
42 ABSENT preflight/36-x01-recovery-cycle-6-receipt-closure/attempt-1
43 ABSENT 36-x01-recovery-cycle-6-receipt-serialization.psm1
44 ABSENT 36-x01-recovery-cycle-6-receipt-closure.ps1
45 EMPTY_DIR preflight/36-x01-recovery-cycle-7-tests/red/owners
46 EMPTY_DIR preflight/36-x01-recovery-cycle-7-tests/red/execution/bootstrap-process
47 ABSENT 36-x01-recovery-cycle-7-recorder.ps1
48 ABSENT 36-x01-recovery-cycle-7-runner.ps1
49 ABSENT 36-x01-recovery-cycle-7-verify.ps1
50 ABSENT preflight/36-x01-recovery-cycle-7-tests/green
51 ABSENT preflight/36-x01-recovery-cycle-7/attempt-1
52 ABSENT raw/36-x01/recovery-cycle-7/attempt-1
```

In addition to those rows, the verifier requires the Cycle 6 parent to contain only `red/`, and Cycle 7 RED to contain exactly the three directories `owners/`, `execution/`, and `execution/bootstrap-process/` with zero files. Thus the two parent-tree shapes cannot pass merely because rows 40, 45, and 46 are empty.

### 10.2 Immutable approval record

After fresh preflight Reviewer PASS and successful exact TEMP probe cleanup, Master writes exactly one `A36-C8-PREFLIGHT-APPROVAL-V1` block in Article36 `subagent-trace.md`. The fixed field set is:

```text
schema
approval_id
reviewer_role
gate
status
execution_type
review_task_id
review_result_sha256
plan_path
plan_bytes
plan_sha256
tests_path
tests_bytes
tests_sha256
recorder_sha256
runner_sha256
verifier_sha256
pwsh_path
pwsh_bytes
pwsh_sha256
corepack_path
corepack_bytes
corepack_sha256
probe_inventory_sha256
probe_cleanup
red_inventory_sha256
green_manifest_sha256
compat_manifest_sha256
source_hashes_sha256
approved_identity_set_sha256
next_allowed_gate
```

Values are actual validated scalars; braces, angle brackets, aliases, `same`, and `PENDING` are invalid. Block bytes run from the exact BEGIN marker through the LF after END. One following SHA marker records its SHA-256; exactly one BEGIN, END, and SHA marker may exist. Formal owner contains the same block SHA, and runner/verifier independently extract/recompute the bounded block. A mutable whole-trace hash is not authority.

### 10.3 Formal root ownership and cleanup

Throughout compatibility, preflight review, and the Master `REVIEW_RECHECK` transition, `F` must remain absent; Master rechecks `F`, `SPEC`, and `ART` absent immediately before writing approval and still does not allocate them. On the authorized `LAB_EXECUTE`, the Lab caller performs a fresh final absence check of all three before any formal write. A pre-existing target is preserved and blocks without creating a child. Only after that check may the same Lab execution allocate `F`, the exact initial directories and destination owner described in section 9, and the CreateNew source-hashes copy; this same-execution allocation is the formal attempt, not a pre-existing-root conflict. Any allocation/copy/flush/identity failure preserves the partial new `F`, consumes the one formal budget, and starts no F0. The caller freezes the resolved system TEMP, exact `ART`, exact fixture-relative `SPEC`, nonce, owner paths, and source identities.

Runner copies frozen harness bytes to `SPEC` with CreateNew, flushes, closes, and checks exact bytes/SHA. F8 records the literal helper output. After the accepted F9 exit 1 and complete pumps/flush/closes, runner CreateNew-copies F9 stdout byte-for-byte to `F/execution/subject/instrumentation.patch`, flushes/closes, and verifies source/destination bytes/SHA. It then CreateNew-writes `instrumentation.sha256` with the exact harness, target-spec, and patch identities; the historical filename is preserved.

F10 remains the unchanged focused command. Until F11, `D` must still have zero immediate and recursive entries. F11 remains the byte-identical frozen collector, invoked exactly with `-SourceRoot ART -DestinationRoot D`; its recorder owner is `F/execution/subject/owners/f11.owner`, sibling marker is `F/execution/subject/f11.bootstrap.kv`, receipt root is `F/execution/subject/f11-collector-process/`, and its unchanged JSON stdout is captured only as that recorder's `stdout.bin`. Thus prior execution receipts cannot pre-populate `D`. The collector itself rechecks that `ART` exists/nonempty and `D` exists/empty, copies every source file beneath `D`, and reports path/count/bytes/SHA validations. Even if focused exits unexpectedly, runner still attempts this bounded raw preservation and owned cleanup; the formal verdict remains FAIL.

Before deleting `ART`, runner independently reconciles the frozen collector stdout, `ART`, and `D`, then CreateNew-writes `F/execution/subject/collector-transfer.tsv` with one lexical row per source relative path, source/destination bytes and SHA, plus source/destination counts; it flushes/closes and rechecks equality. No runner-written transfer receipt is placed in `D`. It revalidates TEMP parent, exact basename, nonce/owner marker, no reparse point, and current ownership. If collector/transfer/ownership cannot be proved, preserve `ART` and the complete or partial `D` and fail rather than deleting evidence. If deletion is attempted but incomplete, preserve remnants and fail. Only a fully transferred owned `ART` may be deleted, followed by an absence check. `D` is durable formal raw evidence and is never cleaned, rewritten, or merged into `execution/subject`.

Runner removes only the exact `SPEC` that this attempt created, then requires `SPEC` absent before F12-F16. A pre-existing/unowned spec is never touched. Postflight head/tag/status/diff must match the frozen baseline. After top settlement, F17 independently checks `collector-destination.owner`, the frozen collector stdout, `collector-transfer.tsv`, the complete `D` file set, both formal manifests, and every evidence-map path/SHA using the two disjoint targets in section 8.4. Formal success requires nonempty closed `D`, matching manifests/evidence map, and absent `ART` and `SPEC`; any preservation exception is an explicit failure limitation, not success.

Success still requires all original 36-X01 assertions: one selected integrated P0-P5 case; native usage and no same-attempt double count; real automatic compaction bracket and before/after request/history/surface difference with raw events retained; cancellation request/signal/body/Tool/turn terminal separation; durable prefix plus disposed first runtime and fresh Context resume; known effect=`KNOWN`, cancelled sentinel=`UNKNOWN`, and no automatic retry; available source IDs plus explicit model-attempt-ID absence; zero real Provider/network/server/credential/production write; and exact cleanup/pinned fixture. No infrastructure PASS upgrades any one of these.

## 11. Role and gate chain with closed 11-field envelopes

Every execution returns exactly one `worker_result` root and exactly these 11 fields: `role`, `article`, `gate`, `execution_type`, `status`, `artifacts_created`, `artifacts_modified`, `gate_completed`, `next_allowed_gate`, `blocker`, `notes`. Arrays are explicit even when empty. Unknown wrappers/fields, missing fields, aliases, or invalid types are rejected. `article="36"`; worker tasks use `execution_type=REAL_SUBAGENT`; only the explicit Master transition below uses `MASTER_DETERMINISTIC`; only canonical role names are used.

The gate chain is:

```text
RESEARCHER / EXPERIMENT_DESIGN                original initial design only
-> fresh REVIEWER / REVIEW                    design approval or bounded Findings
-> fresh REVISION_WORKER / REVISION           bounded design-Finding repair + Revision Disposition when required
-> fresh REVIEWER / REVIEW_RECHECK            repaired-design approval only
-> fresh REVISION_WORKER / REVISION           implementation: Tests -> Probe -> RED -> production -> GREEN -> compatibility
-> fresh REVIEWER / REVIEW_RECHECK            preflight approval only; zero writes
-> MASTER_ORCHESTRATOR / REVIEW_RECHECK       validated successful-Probe cleanup + approval/state transition
-> fresh LAB_ENGINEER / LAB_EXECUTE            one formal execution
-> fresh LAB_ENGINEER / LAB_OBSERVATION        read-only raw organization
-> fresh RESEARCHER / EVIDENCE_MERGE
```

Exact successful mappings are:

- initial design: `RESEARCHER / EXPERIMENT_DESIGN / PASS / true / REVIEW / NONE`;
- reparable initial-design Findings: `REVIEWER / REVIEW / PASS / true / REVISION / NONE`; the durable Findings are the complete repair boundary and do not grant the Reviewer write authority;
- reparable repaired-design Findings: `REVIEWER / REVIEW_RECHECK / PASS / true / REVISION / NONE`; the durable Findings remain the complete repair boundary and require another fresh Revision Worker execution;
- bounded design revision: `REVISION_WORKER / REVISION / PASS / true / REVIEW_RECHECK / NONE`, with a complete Revision Disposition;
- initial design approval: `REVIEWER / REVIEW / PASS / true / REVISION / NONE`, with `notes` containing `design_decision=APPROVED`;
- repaired-design approval: `REVIEWER / REVIEW_RECHECK / PASS / true / REVISION / NONE`, with `notes` containing `design_decision=APPROVED`;
- implementation+Probe+RED+GREEN+compatibility: `REVISION_WORKER / REVISION / PASS / true / REVIEW_RECHECK / NONE`;
- preflight approval recommendation: `REVIEWER / REVIEW_RECHECK / PASS / true / LAB_EXECUTE / NONE`, with `notes` containing `preflight_decision=APPROVED`; the durable Gate does not advance before the following Master action;
- successful Master cleanup/approval transition: `MASTER_ORCHESTRATOR / REVIEW_RECHECK / MASTER_DETERMINISTIC / PASS / true / LAB_EXECUTE / NONE`;
- formal success: `LAB_ENGINEER / LAB_EXECUTE / PASS / true / LAB_OBSERVATION / NONE`;
- observation success: `LAB_ENGINEER / LAB_OBSERVATION / PASS / true / EVIDENCE_MERGE / NONE`.

### 11.1 Revision Disposition for `A36-C8-RR1-F07`

- Finding ID: `A36-C8-RR1-F07`.
- Files Changed: `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-8-plan.md` only.
- What Changed: every design-Finding repair mapping now uses canonical `REVISION_WORKER / REVISION`; the allowed-writes table, gate chain, successful mappings, fresh-review checklist, and self-review all require `Reviewer Findings -> Revision Worker -> Reviewer Recheck`. The original initial-design producer remains `RESEARCHER / EXPERIMENT_DESIGN` and is not relabeled.
- Historical Authority: `wr-a36-cycle8-design-revision-1` and its `RESEARCHER / REVISION` result remain immutable historical facts in `subagent-trace.md`, but that result is `INVALID / NON_AUTHORITY`; its plan changes are review inputs only and do not grant continuation or implementation authority.
- Canonical Revision Authority: `wr-a36-cycle8-design-revision-2` (execution ID `/root/a36_cycle8_design_revision_worker`) was the canonical `REVISION_WORKER / REVISION` producer and validator of the bounded F07 role-authority repair. Final Finding closure and repaired-design approval remained exclusively with its fresh `REVIEWER / REVIEW_RECHECK`.
- Evidence Impact: none. `A36-C8-DR-F01--F06` technical repairs, the evidence ceiling, hypothesis, acceptance criteria, DSH baseline, frozen harness/collector, protected Cycle1-7 history, one-shot budgets, and Article37 boundary are unchanged; no runtime or infrastructure claim is upgraded.
- Proposed Status: `READY_FOR_RECHECK` at this disposition's handoff; the later independent `wr-a36-cycle8-design-recheck-2` record closed `A36-C8-RR1-F07`.

### 11.2 Revision Disposition for `A36-C8-RR2-F08`

- Finding ID: `A36-C8-RR2-F08`.
- Files Changed: `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-8-plan.md` only.
- What Changed: the immutable parent source-identity grammar now has exactly 23 contiguous `K` rows. Row `0001 creator=REVISION_WORKER` remains the truthful parent-receipt byte creator; new row `0002 expected_bootstrap_creator=Tests/Bootstrap` freezes the truthful marker byte creator; former rows `0002--0022` are consistently re-ordinaled to `0003--0023` without changing their values or semantics.
- Cross-Link Disposition: marker `creator` is compared only with parent `expected_bootstrap_creator`, never with parent `creator`. Equality is retained only for corresponding owner role/execution ID, Tests path/SHA, pwsh path/bytes/SHA through marker plus `identity.kv`, the selected official RED/GREEN marker/root paths, and each caller-allocated nonce across its corresponding marker/root receipts; the Probe uses only its caller-frozen TEMP path pair. Any cross-field comparison fails closed.
- Prior Findings: `A36-C8-DR-F01--F06` and `A36-C8-RR1-F07` remain `CLOSED` by their independent Reviewer records; this revision neither reopens nor re-disposes them.
- Evidence Impact: none. The marker byte creator remains Tests `Bootstrap`; the source-identity byte creator remains `REVISION_WORKER`. The evidence ceiling, hypothesis, acceptance criteria, DSH baseline, frozen harness/collector, protected Cycle1-7 history, one-shot budgets, four-file cap, and Article37 boundary are unchanged; no execution or Claim is added or upgraded.
- Proposed Status: `READY_FOR_RECHECK`.

The successful post-review cleanup transition has this exact closed envelope shape; aliases `A`/`E`, absolute TEMP paths, or a deleted external path never appear in repository artifact arrays:

```yaml
worker_result:
  role: MASTER_ORCHESTRATOR
  article: "36"
  gate: REVIEW_RECHECK
  execution_type: MASTER_DETERMINISTIC
  status: PASS
  artifacts_created: []
  artifacts_modified:
    - docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/subagent-trace.md
    - docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/README.md
    - docs/agent-engineering-course/README.md
    - docs/agent-engineering-course/status.md
    - docs/agent-engineering-course/course-run-state.md
  gate_completed: true
  next_allowed_gate: LAB_EXECUTE
  blocker: NONE
  notes:
    - "probe_cleanup=PASS; the exact validated external TEMP root is absent."
    - "F_SPEC_ART_preapproval_absence=PASS; approval block identity is durable."
```

Reparable design Findings use the canonical Reviewer-to-Revision-Worker mapping above. A terminal design or preflight rejection with no safe bounded repair is exactly `REVIEWER / assigned gate / FAIL / true / NONE / FAILED_REVIEW`. A one-shot infrastructure failure is `REVISION_WORKER / REVISION / BLOCKED / false / NONE / HUMAN_DECISION_REQUIRED`; it preserves the attempt and stops. Master cleanup failure is `MASTER_ORCHESTRATOR / REVIEW_RECHECK / MASTER_DETERMINISTIC / BLOCKED / false / NONE / HUMAN_DECISION_REQUIRED`; it preserves any residue, writes no approval, and stops. Formal failure is exactly `LAB_ENGINEER / LAB_EXECUTE / FAIL / false / LAB_EXECUTE / FAILED_LAB`; `LAB_EXECUTE` is only a recovery candidate and grants no retry because `STOP POLICY WINS`.

Reviewer PASS is necessary but not sufficient by envelope status alone: Master must validate the explicit `design_decision=APPROVED` or `preflight_decision=APPROVED`, exact artifacts, identities, budgets, root ownership, and absence of open BLOCKER/MAJOR before dispatching the next role. No ordinary confirmation is inserted between already authorized passing stages. No role starts Article 37.

## 12. Fresh review checklist and stop lines

Fresh design Reviewer must check this plan, original 36-X01 design, Cycle7 plan/Tests/trace, all protected anchors, and current state. Any bounded design Findings must route to a fresh `REVISION_WORKER / REVISION`, whose plan-only repair and complete Revision Disposition then route to a fresh `REVIEWER / REVIEW_RECHECK`; a historical `RESEARCHER / REVISION` result is never repair or continuation authority. `A36-C8-DR-F06` remains Master-owned: this Revision Worker modifies no current-state surface. The repaired-design Reviewer must freshly re-read `A/README.md`, course `README.md`, `status.md`, `course-run-state.md`, the latest trace record, and sections 11.1--11.2, then require Article36 identity, current Gate, last result/ref, next action, blocker/stop projection, canonical revision role, Revision Disposition, and repository/local/origin/live parity to agree with the actual reviewed boundary. A stale or split projection fails recheck rather than being repaired by the Reviewer. PASS also requires all of the following:

- zero JSON/schema/module/reflection/complex-overload dependency before first marker;
- Tests-integrated bootstrap rather than a fifth source file;
- exact bootstrap interface, direct first-marker bytes, inventory, timeout, and precedence, including the 23-row source-identity grammar and the field-specific creator/owner/path/nonce/Tests/pwsh cross-link in sections 4.3 and 7;
- exact same Tests bytes from Probe through RED/GREEN/compatibility;
- honest TDD boundary and production absence during RED;
- external TEMP isolation plus preserve-on-failure / reviewed-cleanup-on-pass policy;
- exact one-shot budgets and all inequalities;
- fixed compatibility/formal topology and original helper/reporter semantics;
- fixed Corepack, DSH, harness, collector, `ART`/`SPEC`, history, source-hash, approval, roles, and closed-envelope mappings.

Fresh preflight Reviewer must independently recompute Tests/source identities, Probe/RED/GREEN/compatibility receipts, manifests, protected Cycle1-7 anchors, pwsh/Corepack identity, DSH fixture pin/clean state, `SPEC`/`ART` absence, source cap, root ownership, invocation token order, timeout arithmetic, and zero formal/focused calls. It must inspect the still-present TEMP probe root. Formal remains forbidden until Reviewer PASS, exact TEMP cleanup PASS, and the Master approval block are all durable.

The following are terminal for Cycle 8: any pre-existing target; any source drift after its freeze point; incomplete first marker or receipt; unexpected Probe/RED/GREEN/compatibility outcome; root ownership ambiguity; history drift; fixture/Corepack/pwsh drift; timeout/kill/pump/flush/close failure; cleanup failure; invalid envelope; or Reviewer FAIL. Preserve evidence, project the exact blocker through Master, and stop. No repair, retry, backfill, rename, commit, push, Article37 work, or hypothesis/acceptance/baseline change is authorized.

## 13. Self-review

- `A36-C8-DR-F01`: closed by dedicated pre-created empty `D`, exact writer ownership, split formal manifests, D-backed evidence map, frozen-collector transfer reconciliation, and preserve/cleanup rules; collector bytes are unchanged.
- `A36-C8-DR-F02`: closed by the exact `P/source-identity.kv`, `P/red.bootstrap.kv`, `P/red/`, `P/green.bootstrap.kv`, and `P/green/` allowlist plus fixed creator/owner/KV1 fields.
- `A36-C8-DR-F03`: closed because Master never writes `F`; the Lab checks absence, allocates `F` once, and direct-byte CreateNew-copies approved source hashes before F0.
- `A36-C8-DR-F04`: closed by a read-only Reviewer followed by the explicit Master `REVIEW_RECHECK / MASTER_DETERMINISTIC` exact-root cleanup/approval transition; failed Probe evidence is never cleanup-eligible.
- `A36-C8-DR-F05`: closed by exact GREEN and row-wise compatibility/formal pump-grace values, one shared concurrent-pump deadline per call, inclusive process-phase timeout semantics, and expanded arithmetic.
- `A36-C8-DR-F06`: closed by the latest fresh current-surface parity recheck; no Revision Worker wrote a current-state surface and the correction remains Master-owned.
- `A36-C8-RR1-F07`: closed by `wr-a36-cycle8-design-recheck-2`; every current design-repair interface is `Reviewer Findings -> Revision Worker -> Reviewer Recheck`, while the original initial-design Researcher and historical invalid `RESEARCHER / REVISION` trace record remain truthfully identified.
- `A36-C8-RR2-F08`: the impossible creator-to-creator equality is removed. The 23-row source identity keeps `creator=REVISION_WORKER` and adds `expected_bootstrap_creator=Tests/Bootstrap`; marker creator is checked only against that expected field, while corresponding owner/path/nonce/Tests/pwsh fields retain exact equality. The complete section 11.2 disposition is `READY_FOR_RECHECK`; only the fresh Reviewer may record final `CLOSED`.
- Placeholders: no placeholder value may enter an actual owner, receipt, source-hash file, command, path, or approval block. Angle-bracket text in this plan is notation only and is explicitly required to expand to validated scalars before use.
- Root ownership: Probe, RED, GREEN, compatibility, and formal roots are disjoint. Marker/root nonce pairs have one owner. Top recorder never owns runner subject output; verifier owns only sibling assessment.
- Counts: Cycle7 remains `3 descendant directories / 0 files`; Cycle8 begins at zero assets. Four future source files only, combined `<=24576` bytes. Attempt budgets are `1 Probe + 1 RED + 1 GREEN + 1 compatibility + conditional 1 formal`.
- Budget closure: every leaf sum is less than or equal to its runner; every runner plus grace is less than or equal to top; top plus verifier is less than or equal to total.
- TDD: immutable Tests precede and characterize absent production in official RED; disposable Probe validates only the exact bootstrap infrastructure and cannot satisfy RED or any production/runtime assertion.
- Failure truth: Cycle7's reported `JsonSerializer.Serialize` failure remains explanatory but `NOT_DURABLY_PROVABLE`; Cycle1-7 artifacts are immutable.
- Formal continuity: exact original helpers, `--reporter=verbose`, fixed Corepack path/SHA, DSH pin, harness/collector bytes, ART transfer/cleanup, SPEC cleanup, approval identity, and 36-X01 acceptance remain unchanged.
- Envelope closure: every current role transition uses all 11 exact fields and canonical roles/statuses; design Findings route only through `REVISION_WORKER / REVISION` with a Revision Disposition before Reviewer Recheck, and reviewer/process verifier are not conflated.
- Course boundary: no Article37 asset, publication, commit, push, state write by a worker, or Evidence promotion is authorized by this design.
