# Article 36 / 36-X01 Recovery Cycle 7 plan

Status: design only. This plan does not repair or reinterpret Cycles 1-6. Cycle 7 may supersede those terminal failures only with new, independently verified evidence. Article 37+ stays at zero assets; no commit or push is authorized.

## 1. Architecture decision

Use exactly four small files: `36-x01-recovery-cycle-7-recorder.ps1`, `36-x01-recovery-cycle-7-runner.ps1`, `36-x01-recovery-cycle-7-verify.ps1`, and `36-x01-recovery-cycle-7-recorder.Tests.ps1`. Combined size MUST be <=24576 UTF-8 bytes. The roles are deliberately separate:

1. recorder: crash-safe outer process recorder using direct UTF-8 text primitives;
2. runner: minimal Article36 command sequence and no receipt abstraction;
3. verifier: independent parser/recomputer, run only after runner/recorder exit;
4. tests: isolated RED/GREEN contract characterization.

Rejected: the Cycle6 schema catalog, serializer, generic candidate model, `JsonNode`, `ConvertTo-Json`, reusable module loading, and a general receipt framework. They add failure dependencies Article36 does not need. The recorder must work before DSH, Git, Node, Corepack/pnpm, a package manager, schema loading, or the subject under test works.

This path is justified only by existing protected evidence: Cycle5 GREEN remains a valid historical artifact but its formal Cycle5 raw root is absent; Cycle6 has only its preserved empty RED root while its serializer, closure script, compatibility attempt, GREEN, and raw root are absent. Cycle7 neither completes nor repairs them. It removes the observed failure surfaces: no generic schema/serializer, no after-the-fact command reconstruction, no verifier writing inside its target, and no failure receipt that calls the failing subject mechanism.

Frozen baseline: repository `https://github.com/deepseek-ai/deepseek-harness`; fixture `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`; tag `dsh-v0.1.2-alpha.1`; commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`; Node `v24.18.1`; absolute Corepack `C:\nvm4w\nodejs\corepack.cmd` SHA-256 `920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF5942FBAD4F`; harness `36-x01-recovery-cycle-2-instrumentation.ts` SHA-256 `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B`; collector `36-x01-recovery-cycle-2-collector.ps1` SHA-256 `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045`. The original 36-X01 hypothesis, acceptance criteria, safety boundary, harness semantics, collector semantics, and fixture baseline remain unchanged.

## 2. Frozen write allowlists

Let `A=docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery` and `E=A/experiments`.

| stage | only permitted writes |
|---|---|
| design/revision | `E/36-x01-recovery-cycle-7-plan.md` |
| compatibility implementation + receipts | the four files in section 1; `E/preflight/36-x01-recovery-cycle-7-tests/{red,green}/`; `E/preflight/36-x01-recovery-cycle-7/attempt-1/` |
| fresh review | Reviewer performs read-only checks and returns only its closed envelope; no Reviewer filesystem write |
| formal implementation + raw receipts | no source edits; only `E/raw/36-x01/recovery-cycle-7/attempt-1/`, the relative temporary spec inside the fixture, and the frozen harness artifact source `<resolved system TEMP>/dsh-article36-x01-artifacts/` |
| Master state/trace projection | `docs/agent-engineering-course/course-run-state.md`; `docs/agent-engineering-course/status.md`; `docs/agent-engineering-course/README.md`; `A/README.md`; bounded approval/result records in `A/subagent-trace.md` |

Researcher, Revision Worker, Reviewer, and Lab Engineer write no current-state projection. Master has mechanically closed the current projection finding and remains sole projection writer; this plan only freezes its canonical allowlist. No Cycle5/6 path is writable. A pre-existing Cycle7 stage/root is a fail-closed conflict: preserve it, write no child, stop; never delete, rename, backfill, repair, or replay.

## 3. Protected historical evidence and formal source-hashes row set

Before GREEN, compatibility, review, and formal execution, compare the applicable snapshot to this catalog. Each mismatch is terminal `HISTORICAL_EVIDENCE_CHANGED`. `source-hashes.tsv` MUST contain exactly the following 36 ordered rows after its header; no duplicate or extra row is allowed. Paths below are the decoded display values; the file encodes them as section 4 requires. `FILE` rows carry exact byte count and SHA; four Cycle7 file identities and the revised-plan identity are filled from the Master-validated approval set. `EMPTY_DIR` and `ABSENT` use `NA/NA` and are verified from directory entries, not inferred.

```text
01 FILE 36-x01-recovery-cycle-7-plan.md APPROVAL_BYTES APPROVAL_SHA
02 FILE 36-x01-recovery-cycle-7-recorder.ps1 APPROVAL_BYTES APPROVAL_SHA
03 FILE 36-x01-recovery-cycle-7-runner.ps1 APPROVAL_BYTES APPROVAL_SHA
04 FILE 36-x01-recovery-cycle-7-verify.ps1 APPROVAL_BYTES APPROVAL_SHA
05 FILE 36-x01-recovery-cycle-7-recorder.Tests.ps1 APPROVAL_BYTES APPROVAL_SHA
06 FILE 36-x01-recovery-cycle-2-instrumentation.ts 34852 D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B
07 FILE 36-x01-recovery-cycle-2-collector.ps1 6398 D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045
08 FILE 36-x01-recovery-cycle-5-diagnostic.ps1 19626 D5AF4F2FF3F11A4BB06DD18D7357989722E851733D2F56220B2E5AFA25107274
09 FILE 36-x01-recovery-cycle-5-diagnostic.Tests.ps1 10787 E042BC774E717ABEE31EE9B2BB297A5C4A49063C211B221C75D000BFB797219F
10 FILE 36-x01-recovery-cycle-5-plan.md 21952 B0DD65CF4FC3C06A88EF23B3CE010ACC5F91E1C774903551B98316151EF288AA
11 FILE 36-x01-recovery-cycle-6-cycle5-defect-witness.json 1885 ECFE687E331E722EED88A56BBD4F4A38C5273833A07D194EF15C915D607D4744
12 FILE 36-x01-recovery-cycle-6-plan.md 34525 DE82A0056E1832A8E9AC3E280022593961B421327E1474EBF0C3C646E111F16A
13 FILE 36-x01-recovery-cycle-6-receipt-closure.Tests.ps1 23082 20E149429B1DF9AEB8E25BC237FFAD735DEEB17FC6AFC87078D784BE04022109
14 FILE 36-x01-recovery-cycle-6-receipt-schemas.json 7078 91E9EFCA5E67498825C78C9D14BB85D046478C8CBF5DDF40045A0EF05E67AB1A
15 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/cleanup-receipt.json 214 5DA8F306BE28FBE7195B6DC1576A5179544CFE0B81683FE370EDE96B6C35BB05
16 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/identities.json 647 CA06E318033F9B9C7A2C5AA3E5CF12B3D5B26CBE58504D8ED818934B170C5BD3
17 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/manifest.json 1086 AC990F709427C60E2B9C312C8C41144A9A74E88E1A1DC33F870222459C555AF7
18 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/phase-summary.json 919 EA1FE94E6435A5F31D0ABE15A0E7437C7FE391EEE985D5F08A90243548CBCD6A
19 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/stderr.txt 0 E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855
20 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/stdout.txt 0 E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855
21 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/cleanup-receipt.json 214 5DA8F306BE28FBE7195B6DC1576A5179544CFE0B81683FE370EDE96B6C35BB05
22 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/identities.json 647 3366F972D3376CB935558E3E6604D866BF0B2F92DD90BCBEB1D44F73B8FED9D3
23 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/manifest.json 1077 7A5C834D04F200502BAC6D3D2F2C1380E343DB1EDC04FA79163AB22CB989412A
24 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/phase-summary.json 6765 ED7F81D008CDFEE01736BC4996FB7CA5303485C5B9AEFE5D900AE79AAF5C65B3
25 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/stderr.txt 0 E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855
26 FILE preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/stdout.txt 0 E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855
27 ABSENT preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/exact-command.txt NA NA
28 ABSENT preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/exact-command.txt NA NA
29 ABSENT preflight/36-x01-recovery-cycle-5-diagnostic/attempt-1 NA NA
30 EMPTY_DIR preflight/36-x01-recovery-cycle-6-receipt-closure-tests/red NA NA
31 ABSENT preflight/36-x01-recovery-cycle-6-receipt-closure-tests/green NA NA
32 ABSENT preflight/36-x01-recovery-cycle-6-receipt-closure/attempt-1 NA NA
33 ABSENT 36-x01-recovery-cycle-6-receipt-serialization.psm1 NA NA
34 ABSENT 36-x01-recovery-cycle-6-receipt-closure.ps1 NA NA
35 ABSENT raw/36-x01/recovery-cycle-5 NA NA
36 ABSENT raw/36-x01/recovery-cycle-6 NA NA
```

Additionally, `preflight/36-x01-recovery-cycle-6-receipt-closure-tests/` MUST exist with `red/` as its only child, and `red/` MUST be empty. Cycle5 red and green each remain exactly their six listed files. Thus old GREEN and the empty Cycle6 RED root remain byte-for-byte evidence.

## 4. Receipt primitives and precedence

### 4.1 Grammar and ownership

`KV1`: UTF-8 without BOM, LF only; first line `H<TAB>TYPE-V1`; following lines `K<TAB><four-digit ordinal><TAB><ASCII key><TAB><canonical Base64 UTF-8 value>`. Keys are unique and ordinals contiguous. Values never contain literal TAB/CR/LF. Base64 must decode and re-encode identically.

`SOURCE-HASHES-V1`: header `H<TAB>SOURCE-HASHES-V1`; row grammar `F<TAB><four-digit ordinal><TAB><canonical Base64 normalized E-relative path><TAB><FILE|EMPTY_DIR|ABSENT><TAB><decimal bytes|NA><TAB><uppercase SHA256|NA>`. Paths use `/`, no `.`/`..`, drive, leading slash, CR, TAB, or duplicate case-insensitive target; rows are fixed in section 3 order.

`MANIFEST-V1`, created only in `assessment/`, has header `H<TAB>MANIFEST-V1` and the same `F` row grammar/path rules as SOURCE-HASHES. Rows are unique, case-insensitive-target unique, lexically sorted then contiguously numbered, with one row for every file below `execution/`, excluding `assessment/`, `verifier-process/`, itself, and temporary verifier files. It records bytes and SHA-256. Hence hashes are self-excluding and independently recomputable.

Every stage has sibling roots `owners/`, `execution/`, `verifier-process/`, `assessment/`. Caller first proves the entire stage root absent, creates the stage and only `owners/`, and writes a CreateNew owner marker containing role, nonce, exact invocation contract, and its SHA. Top recorder alone creates `execution/` plus its owned `execution/top-process/` receipt root. Its Tests/runner subject validates owner path, SHA, nonce, and role; proves `execution/subject/` absent; then alone creates and owns `execution/subject/`. Every nested receipt and subject output is below that subject root. Top recorder never owns subject root and Tests/runner never writes top-process. After top exits, both pumps finish, and all handles close, caller launches verifier through a new recorder in sibling `verifier-process/`; verifier alone creates sibling `assessment/` and recomputes all of `execution/`. Verifier never verifies a root it owns. `subject_started_count` counts recorder-started subjects: GREEN is 3 (top, inner, verifier), not 2; outer expected-start-failure is not counted. Compatibility is 7 (top, inner, four tools, verifier); formal is 18 (top, sixteen leaves, verifier).

### 4.2 Crash-safe recorder

For each launch, caller freezes executable, optional script, cwd, ordered argv tokens, timeout, expected start/exit, expected executable identity, host marker, owner role/path/SHA/nonce, and receipt/output roots before invocation. Host marker is caller-supplied canonical Base64 of machine name, OS version, and process architecture, obtained without an external command. Recorder uses CreateNew files only. Its first action is to CreateNew `preparing.txt`, write and `FileStream.Flush(true)` a minimal KV1 containing only caller-supplied executable/script/cwd/argv/timeout/expected identity/host values and start UTC. It then precreates `stdout.bin`, `stderr.bin`, and `exception.txt`; reads/verifies owner and executable identity; writes `identity.txt`; CreateNew-writes and Flush(true) `ready.txt`; only then calls `Process.Start`.

The only absent-executable exception is a closed owner contract with `ExpectedStart=N`, `ExpectedExecutableIdentity=EXPECTED_ABSENT`, expected child exit `NA`, and expected exception type `System.ComponentModel.Win32Exception`. In that case identity stage MUST require the resolved executable absent, record literal sentinel `EXPECTED_ABSENT` instead of trying to hash it, write/flush READY, and still call `Process.Start`. It must catch the actual expected start exception and fully write its type/message, `actual_start=false`, child `NOT_AVAILABLE`, and result discriminator `EXPECTED_ABSENT_START_FAILURE`; only that complete match returns recorder 0. Any missing executable under any other contract is identity failure before start and returns recorder-integrity failure, never an expected-start result.

No JSON, schema, module import, Git, DSH, Node, Corepack, or package manager is a prerequisite for `preparing.txt`. Exception and cleanup paths use only direct UTF-8 file primitives. Stdout/stderr use concurrent asynchronous drains started immediately. Normal order is child exit, both pump completions, stream flushes, handle closes, then result.

On timeout call `Kill(entireProcessTree:true)` once; wait at most 5 s for tree death. Kill throw/failure or a still-running child is `KILL_FAILED`. Pump grace after child/kill is 5 s for ordinary leaves and 10 s for top/focused/collector/verifier. Pump timeout/fault, stream flush failure, or handle-close failure is receipt-integrity failure. Every signed 32-bit child exit code (`-2147483648..2147483647`) is preserved exactly; unavailable is `NOT_AVAILABLE`.

`result.txt` is authoritative only if CreateNew, complete, flushed, and all integrity flags pass. Otherwise CreateNew `emergency.txt` is authoritative and `result.txt` must not be interpreted. Precedence:

| condition | recorder exit |
|---|---:|
| complete receipt; start/child exit/executable identity/start exception all match contract | 0 |
| complete receipt; any start/exit/identity/expected-exception value differs | 91 |
| bounded subject timeout with successful kill/drain/close | 124 |
| kill failed/still running, pump fault/timeout, flush/close failure, or recorder fault | 90 |

Child exit is never replaced by recorder exit. Result fields include expected/actual start, expected/actual child exit, expected/actual executable identity or sentinel, expected/actual start-exception type and discriminator, timeout, kill attempted/succeeded, child still running, each pump state, each flush/close state, exception type/message Base64, start/end UTC, and recorder exit.

Each completed recorder root contains `preparing.txt`, `stdout.bin`, `stderr.bin`, `exception.txt`, `identity.txt`, `ready.txt`, and `result.txt`; structured text uses KV1 with type headers matching filenames (`PREPARING-V1`, `IDENTITY-V1`, `READY-V1`, `RESULT-V1`, `EMERGENCY-V1`) and the two streams are raw bytes. On recorder-integrity failure, `emergency.txt` is required; a partial `result.txt` may coexist but is ignored. Each successful verifier creates exactly `manifest.tsv` and `verification.txt`; GREEN also creates `argv.json` and `four-file-identities.tsv`, compatibility also creates `formal-source-hashes.tsv`, and formal also creates `evidence-map.tsv`. Unexpected/missing files fail verification.

JSON is optional for presentation. Receipt arrays are eliminated except `argv` in a verifier-produced JSON projection. That projection MUST be parsed independently with `System.Text.Json.JsonDocument`; `argv.ValueKind` MUST equal `Array` before `GetArrayLength`. Empty argv is allowed only after this exact kind check. Truthiness/count-only checks are forbidden.

## 5. Complete pre-run launch matrix

Aliases: `W=E`; `FIX=C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`; `PS=pwsh.exe`; `REC=E/36-x01-recovery-cycle-7-recorder.ps1`; `RUN=E/36-x01-recovery-cycle-7-runner.ps1`; `VER=E/36-x01-recovery-cycle-7-verify.ps1`; `TST=E/36-x01-recovery-cycle-7-recorder.Tests.ps1`; `CORE=C:\nvm4w\nodejs\corepack.cmd`; `CORE_SHA=920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF5942FBAD4F`; `GIT=git.exe`; `NODE=node.exe`; `SPEC=packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts`; `MISS=<caller-created unique absolute CONTRACT_TEMP>/intentionally-absent-child.exe`; `TEMP=<caller-resolved [System.IO.Path]::GetTempPath()>`; `ART=TEMP/dsh-article36-x01-artifacts`; `COL=E/36-x01-recovery-cycle-2-collector.ps1`; `D=E/preflight/36-x01-recovery-cycle-7-tests/red`; `G=E/preflight/36-x01-recovery-cycle-7-tests/green`; `GS=G/execution/subject`; `C=E/preflight/36-x01-recovery-cycle-7/attempt-1`; `CS=C/execution/subject`; `F=E/raw/36-x01/recovery-cycle-7/attempt-1`; `FS=F/execution/subject`. Caller freezes `CONTRACT_TEMP`, `TEMP`, and every resolved path in owner before launch. It proves `MISS` contained by the attempt temp root and absent; formal proves fixed `ART`, `SPEC`, and `F` absent and fails closed without deleting them if any pre-exists.

For every non-RED row, recorder invocation is exactly: executable `PS`, script `REC`, cwd shown, ordered recorder argv `[-NoLogo,-NoProfile,-File,REC,-OwnerPath,O,-OwnerSha256,OSHA,-OwnerNonce,NONCE,-ReceiptRoot,R,-SubjectExecutable,SE,-SubjectScript,SS,-WorkingDirectory,CWD,-TimeoutSeconds,T,-ExpectedStart,ES,-ExpectedChildExitCode,EC,-ExpectedExecutableIdentity,EI,-ExpectedStartException,ESE,-HostMarkerBase64,HM,-InvocationContractBase64,IC]`. `EI`, `ESE`, `HM`, and `IC` are frozen in owner before launch; `IC` is the canonical KV1 of that row and `SS=NONE` for native commands. Matrix cells list subject argv in exact order. Expected is `start/child/recorder;identity/start-exception`; default is the pinned executable SHA and `NONE` unless a row says otherwise.

R0 intentionally has no production recorder. Its caller first CreateNew-writes and Flush(true) `D/owners/bootstrap.owner` with exact PS/TST executable+script, TST bytes/SHA, cwd, ordered argv, 30 s timeout, expected start/exit, host marker, start-attempt UTC, role, nonce, expected discriminator `RECORDER_CONTRACT_UNIMPLEMENTED`, and expected ABSENT state for REC/RUN/VER. Before `Process.Start`, caller CreateNew-opens `D/execution/bootstrap-process/stdout.bin` and `stderr.bin`, pumps both concurrently, then waits for exit, awaits pumps, Flush(true)s both, and closes child/stream handles. It finally CreateNew-writes and Flush(true)s `tests-identity.txt` and KV1 `result.txt`.

The exact RED inventory is owner plus those four bootstrap-process files; no other RED file is allowed. `result.txt` records actual host, cwd, ordered argv, start attempted/start actual, exact child exit `1`, actual TST bytes/SHA, REC/RUN/VER each actually `ABSENT`, actual failure discriminator, both pump states, both stream flush states, and every handle-close state. RED is `EXPECTED_FAIL` only when the inventory is complete, TST identity matches owner, all three production files remained absent, exit is exactly 1, and discriminator is exactly `RECORDER_CONTRACT_UNIMPLEMENTED`. Any other exit 1, exception, missing receipt, identity drift, pump/flush/close failure, or discriminator is terminal unexpected RED and cannot masquerade as characterization success.

| id | recorder exe/script | subject exe; script; ordered argv tokens | cwd; T | owner path/role | receipt root; subject output | expected |
|---|---|---|---|---|---|---|
| R0 RED-bootstrap | `NONE;NONE` | `PS;TST;[-NoLogo,-NoProfile,-File,TST,-Phase,Red,-OwnerPath,D/owners/bootstrap.owner]` | `W;30` | `D/owners/bootstrap.owner;REVISION_WORKER` | `D/execution/bootstrap-process;stdout/stderr pipes captured there` | `Y/1/NA;TEST_SHA/NONE` |
| G0 green-top | `PS;REC` | `PS;TST;[-NoLogo,-NoProfile,-File,TST,-Phase,Green,-OwnerPath,G/owners/top.owner,-ReceiptRoot,GS]` | `W;40` | `G/owners/top.owner;REVISION_WORKER` | `G/execution/top-process;GS` | `Y/0/0` |
| G1 green-outer | `PS;REC` | `MISS;NONE;[]` | `W;10` | `G/owners/outer.owner;REVISION_WORKER` | `GS/outer;GS/outer` | `N/NA/0;EXPECTED_ABSENT/System.ComponentModel.Win32Exception` |
| G2 green-inner | `PS;REC` | `PS;NONE;[-NoLogo,-NoProfile,-Command,exit 23]` | `W;15` | `G/owners/inner.owner;REVISION_WORKER` | `GS/inner;GS/inner` | `Y/23/0` |
| G3 green-verifier | `PS;REC` | `PS;VER;[-NoLogo,-NoProfile,-File,VER,-Mode,Green,-OwnerPath,G/owners/verifier.owner,-TargetRoot,G/execution,-AssessmentRoot,G/assessment]` | `W;30` | `G/owners/verifier.owner;REVISION_WORKER` | `G/verifier-process;G/assessment` | `Y/0/0` |
| C0 compat-top | `PS;REC` | `PS;RUN;[-NoLogo,-NoProfile,-File,RUN,-Mode,Compatibility,-OwnerPath,C/owners/top.owner,-OutputRoot,CS,-FixtureRoot,FIX]` | `FIX;155` | `C/owners/top.owner;REVISION_WORKER` | `C/execution/top-process;CS` | `Y/0/0` |
| C1 compat-outer | `PS;REC` | `MISS;NONE;[]` | `FIX;10` | `C/owners/outer.owner;REVISION_WORKER` | `CS/outer;CS/outer` | `N/NA/0;EXPECTED_ABSENT/System.ComponentModel.Win32Exception` |
| C2 compat-inner | `PS;REC` | `PS;NONE;[-NoLogo,-NoProfile,-Command,exit 23]` | `FIX;15` | `C/owners/inner.owner;REVISION_WORKER` | `CS/inner;CS/inner` | `Y/23/0` |
| C3 compat-git | `PS;REC` | `GIT;NONE;[--version]` | `FIX;15` | `C/owners/git.owner;REVISION_WORKER` | `CS/git;CS/git` | `Y/0/0` |
| C4 compat-node | `PS;REC` | `NODE;NONE;[--version]` | `FIX;10` | `C/owners/node.owner;REVISION_WORKER` | `CS/node;CS/node` | `Y/0/0` |
| C5 compat-pnpm | `PS;REC` | `CORE;NONE;[pnpm,--version]` | `FIX;30` | `C/owners/pnpm.owner;REVISION_WORKER` | `CS/pnpm;CS/pnpm` | `Y/0/0` |
| C6 compat-vitest | `PS;REC` | `CORE;NONE;[pnpm,exec,vitest,--version]` | `FIX;30` | `C/owners/vitest.owner;REVISION_WORKER` | `CS/vitest;CS/vitest` | `Y/0/0` |
| C7 compat-verifier | `PS;REC` | `PS;VER;[-NoLogo,-NoProfile,-File,VER,-Mode,Compatibility,-OwnerPath,C/owners/verifier.owner,-TargetRoot,C/execution,-AssessmentRoot,C/assessment]` | `FIX;30` | `C/owners/verifier.owner;REVISION_WORKER` | `C/verifier-process;C/assessment` | `Y/0/0` |
| F0 formal-top | `PS;REC` | `PS;RUN;[-NoLogo,-NoProfile,-File,RUN,-Mode,Formal,-OwnerPath,F/owners/top.owner,-ApprovalBlockSha256,ABSHA,-OutputRoot,FS,-FixtureRoot,FIX,-RelativeSpec,SPEC,-HarnessPath,E/36-x01-recovery-cycle-2-instrumentation.ts,-CollectorPath,COL]` | `FIX;530` | `F/owners/top.owner;LAB_ENGINEER` | `F/execution/top-process;FS` | `Y/0/0` |
| F1 node-version | `PS;REC` | `NODE;NONE;[--version]` | `FIX;10` | `F/owners/01.owner;LAB_ENGINEER` | `FS/01-node;FS/01-node` | `Y/0/0` |
| F2 pnpm-version | `PS;REC` | `CORE;NONE;[pnpm,--version]` | `FIX;30` | `F/owners/02.owner;LAB_ENGINEER` | `FS/02-pnpm;FS/02-pnpm` | `Y/0/0` |
| F3 before-remote | `PS;REC` | `GIT;NONE;[config,--get,remote.origin.url]` | `FIX;10` | `F/owners/03.owner;LAB_ENGINEER` | `FS/03-before-remote;FS/03-before-remote` | `Y/0/0` |
| F4 before-head | `PS;REC` | `GIT;NONE;[rev-parse,HEAD]` | `FIX;10` | `F/owners/04.owner;LAB_ENGINEER` | `FS/04-before-head;FS/04-before-head` | `Y/0/0` |
| F5 before-tag | `PS;REC` | `GIT;NONE;[rev-parse,dsh-v0.1.2-alpha.1^{commit}]` | `FIX;10` | `F/owners/05.owner;LAB_ENGINEER` | `FS/05-before-tag;FS/05-before-tag` | `Y/0/0` |
| F6 before-status | `PS;REC` | `GIT;NONE;[status,--short,--branch]` | `FIX;10` | `F/owners/06.owner;LAB_ENGINEER` | `FS/06-before-status;FS/06-before-status` | `Y/0/0` |
| F7 before-diff | `PS;REC` | `GIT;NONE;[diff,--check]` | `FIX;10` | `F/owners/07.owner;LAB_ENGINEER` | `FS/07-before-diff;FS/07-before-diff` | `Y/0/0` |
| F8 target-hash | `PS;REC` | `PS;NONE;[-NoLogo,-NoProfile,-Command,Get-FileHash,-Algorithm,SHA256,SPEC]` | `FIX;10` | `F/owners/08.owner;LAB_ENGINEER` | `FS/08-target-hash;FS/08-target-hash/stdout.bin` | `Y/0/0` |
| F9 no-index-patch | `PS;REC` | `GIT;NONE;[diff,--no-index,--,/dev/null,SPEC]` | `FIX;20` | `F/owners/09.owner;LAB_ENGINEER` | `FS/09-patch;FS/09-patch/stdout.bin` | `Y/1/0` |
| F10 focused | `PS;REC` | `CORE;NONE;[pnpm,exec,vitest,run,SPEC,--reporter=verbose]` | `FIX;180` | `F/owners/10.owner;LAB_ENGINEER` | `FS/10-focused;ART` | `Y/0/0` |
| F11 collector | `PS;REC` | `PS;COL;[-NoLogo,-NoProfile,-File,COL,-SourceRoot,ART,-DestinationRoot,FS]` | `FIX;60` | `F/owners/11.owner;LAB_ENGINEER` | `FS/11-collector;FS` | `Y/0/0` |
| F12 after-remote | `PS;REC` | `GIT;NONE;[config,--get,remote.origin.url]` | `FIX;10` | `F/owners/12.owner;LAB_ENGINEER` | `FS/12-after-remote;FS/12-after-remote` | `Y/0/0` |
| F13 after-head | `PS;REC` | `GIT;NONE;[rev-parse,HEAD]` | `FIX;10` | `F/owners/13.owner;LAB_ENGINEER` | `FS/13-after-head;FS/13-after-head` | `Y/0/0` |
| F14 after-tag | `PS;REC` | `GIT;NONE;[rev-parse,dsh-v0.1.2-alpha.1^{commit}]` | `FIX;10` | `F/owners/14.owner;LAB_ENGINEER` | `FS/14-after-tag;FS/14-after-tag` | `Y/0/0` |
| F15 after-status | `PS;REC` | `GIT;NONE;[status,--short,--branch]` | `FIX;10` | `F/owners/15.owner;LAB_ENGINEER` | `FS/15-after-status;FS/15-after-status` | `Y/0/0` |
| F16 after-diff | `PS;REC` | `GIT;NONE;[diff,--check]` | `FIX;10` | `F/owners/16.owner;LAB_ENGINEER` | `FS/16-after-diff;FS/16-after-diff` | `Y/0/0` |
| F17 formal-verifier | `PS;REC` | `PS;VER;[-NoLogo,-NoProfile,-File,VER,-Mode,Formal,-OwnerPath,F/owners/verifier.owner,-ApprovalBlockSha256,ABSHA,-TargetRoot,F/execution,-AssessmentRoot,F/assessment,-SourceHashes,F/owners/source-hashes.tsv]` | `FIX;60` | `F/owners/verifier.owner;LAB_ENGINEER` | `F/verifier-process;F/assessment` | `Y/0/0` |

Before any launch, caller expands every display alias to its frozen absolute value, except `SPEC`, which expands to the exact fixture-relative token shown. It resolves executable tokens to absolute identities, canonicalizes `IC`, and persists it in owner. No alias, angle-bracket placeholder, or `same` token may enter an actual owner/receipt; post-run reconstruction is never provenance.

## 6. TDD, compatibility, and budgets

The current design recheck happens first. Reviewer PASS is exactly `PASS/true/REVISION/NONE` and authorizes a fresh `REVISION_WORKER/REVISION`; Reviewer failure is `FAIL/true/NONE/FAILED_REVIEW` and Master maps it to terminal pause. Only then is this one-shot implementation order allowed:

1. RED: Revision Worker creates only `36-x01-recovery-cycle-7-recorder.Tests.ps1`; all three production files must still be absent. Caller proves RED root absent, writes the frozen owner, and launches R0 once. Only the closed actual-receipt/discriminator contract in section 5 is `EXPECTED_FAIL`. Preserve RED forever. Unexpected pass, generic exit 1, wrong failure, or incomplete durable RED evidence stops.
2. Without changing the accepted characterization, Revision Worker implements only recorder, runner, and verifier. GREEN: prove GREEN stage absent, write owners, launch G0-G3 exactly once. Tests deliberately inject G1 EXPECTED_ABSENT start failure and G2 exit 23. PASS requires G1 identity sentinel (never a missing-file hash), READY before an actually attempted Process.Start, exact expected exception/result evidence, G2 child/recorder separation, complete pumps/handles, external manifest recomputation, JSON empty-argv `JsonValueKind.Array`, and `subject_started_count=3`. Preserve GREEN forever.
3. GREEN verifier freezes `G/assessment/four-file-identities.tsv` with `SOURCE-HASHES-V1`; no later source edit is allowed without abandoning this attempt and returning to a new RED/GREEN cycle.
4. Compatibility: prove `C` absent; before C0 recompute the exact absolute `CORE` identity and require `CORE_SHA`, then launch C0-C7 once. C5 and C6 must each independently record that same path/SHA; compatibility may not approve any replacement identity. Six leaf budgets are `(10+5)+(15+5)+(15+5)+(10+5)+(30+5)+(30+5)=140 s`. Runner is 155 s (`140+15` setup/cleanup), top is 165 s (`155+10` grace), verifier is 40 s (`30+10`), total wall budget is 205 s (`165+40`). Required inequalities: `155>=140+15`, `165>=155+10`, `205>=165+40`. Compatibility verifier writes `C/assessment/formal-source-hashes.tsv` with exactly section 3's bytes and records the fixed Corepack identity; its SHA is reviewed. Any failure preserves all evidence and stops before formal.
5. Revision Worker returns `PASS/true/REVIEW_RECHECK/NONE` only after compatibility PASS. A fresh Reviewer independently recomputes the fixed absolute Corepack identity and checks it against both frozen baseline and compatibility receipts, then checks protected catalog, four-file cap/identity, topology, ownership, every matrix row, manifests, grammar, injected-failure survival, budgets, and compatibility result. No ordinary human confirmation is inserted because conditional continuation is already authorized.

Fresh preflight Reviewer emits a closed `REVIEWER/REVIEW_RECHECK` result, which Master durably records before branching. PASS is exactly `PASS/true/LAB_EXECUTE/NONE`; failure is exactly `FAIL/true/NONE/FAILED_REVIEW`, and Master then projects terminal pause without an approval block. Preflight cannot create `F`, alter fixture, or consume the one formal attempt.

RED has one 30 s subject wait plus 5 s caller margin, total 35 s. GREEN leaf bounds are `(10+5)+(15+5)=35 s`; Tests runner is 40 s (`35+5`), top recorder is 50 s (`40+10`), verifier is 40 s (`30+10`), total 90 s (`50+40`). Required inequalities are `40>=35+5`, `50>=40+10`, and `90>=50+40`. Every timeout/failure preserves its one root and forbids a retry.

## 7. Immutable approval block

Before formal, Master writes exactly one block to Article36 `subagent-trace.md`:

```text
<!-- A36-C7-PREFLIGHT-APPROVAL-BEGIN id="A36-C7-PREFLIGHT-REVIEW-001" -->
schema=A36-C7-PREFLIGHT-APPROVAL-V1
reviewer_role=REVIEWER
gate=REVIEW_RECHECK
status=PASS
execution_type=REAL_SUBAGENT
review_task_id=<id>
review_result_sha256=<sha>
plan_path=<path>
plan_bytes=<decimal>
plan_sha256=<sha>
corepack_path=C:\nvm4w\nodejs\corepack.cmd
corepack_sha256=920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF5942FBAD4F
four_file_identities_sha256=<sha>
green_manifest_sha256=<sha>
compat_manifest_sha256=<sha>
source_hashes_sha256=<sha>
approved_identity_set_sha256=<sha>
next_allowed_gate=LAB_EXECUTE
<!-- A36-C7-PREFLIGHT-APPROVAL-END id="A36-C7-PREFLIGHT-REVIEW-001" -->
<!-- A36-C7-PREFLIGHT-APPROVAL-SHA256 id="A36-C7-PREFLIGHT-REVIEW-001" value="<sha>" -->
```

Block bytes are UTF-8 without BOM from `<` of BEGIN through the LF immediately after END. Exactly one BEGIN, END, and following SHA marker may exist. At approval time Master freshly recomputes the absolute Corepack file and requires the frozen path/SHA, then independently reparses/revalidates every identity including the current revised plan and `C/assessment/formal-source-hashes.tsv`. Master computes block SHA, writes the SHA marker, and copies the same SHA into formal owner. After proving `F` absent, formal caller CreateNew-copies those exact source-hashes bytes to `F/owners/source-hashes.tsv` and recomputes the approved SHA before top launch. It also recomputes `CORE` immediately before F0 and requires both the frozen `CORE_SHA` and approval-block identity. Formal consumes only this bounded block SHA: runner and verifier extract by exact markers, recompute bytes/SHA, compare owner+marker, and fail closed. No stage may accept drift as a new baseline; mutable whole-trace SHA is never authority.

## 8. Formal 36-X01, cleanup, and evidence

A fresh `LAB_ENGINEER/LAB_EXECUTE` proves `F` absent and runs F0-F17 exactly once. All subject commands run with cwd `FIX`. Original helper vectors are preserved literally: `git rev-parse HEAD`, `git rev-parse 'dsh-v0.1.2-alpha.1^{commit}'`, `git status --short --branch`, `git diff --check`, `node --version`, `pnpm --version`, `Get-FileHash -Algorithm SHA256 <relative temp spec>`, and `git diff --no-index -- /dev/null <relative temp spec>`. No `git -C` and no internal hash may replace them. Extra `remote.origin.url` is additive.

Before instrumentation, head/tag must equal the frozen full commit, status/diff must match the approved clean baseline, Node exactly `v24.18.1`, and pnpm/Vitest approved compatibility identities. Copy frozen harness bytes to `SPEC` without semantic change. F8 keeps cwd exactly `FIX` and passes the literal relative `SPEC` token, so its effective helper remains exactly `Get-FileHash -Algorithm SHA256 <relative temp spec>`; absolute substitution is forbidden.

F9 recorder owns only `FS/09-patch/` and captures command stdout in `FS/09-patch/stdout.bin`; `instrumentation.patch` is not a recorder output. Only after F9 child exit 1 is accepted, both pumps complete, every stream flushes, and every handle closes, runner byte-for-byte copies that stdout with CreateNew to `FS/instrumentation.patch`, calls Flush(true), closes it, and independently requires source/destination byte count and SHA-256 equality. Copy/hash/flush failure is terminal. Then runner CreateNew-writes `FS/instrumentation.sha256` as `KV1/INSTRUMENTATION-SHA256-V1`; it records literal F8 Get-FileHash stdout plus harness, temporary spec, and copied patch bytes/SHA. It is never named `instrumentation.patch.sha256`.

F10 remains the original `pnpm exec vitest run <SPEC> --reporter=verbose`; it is not changed to a JSON reporter. F2 and F10 identity stages each recompute the absolute `CORE` file and require `CORE_SHA`; neither compatibility nor formal can accept drift. Required JSON raw artifacts come only from the unchanged frozen harness and the F11 frozen collector. F11 remains exactly `-SourceRoot ART -DestinationRoot FS`. Run unchanged focused harness and frozen collector semantics.

Cleanup is in runner's 30 s margin. Before F10, owner/runner must have recorded exact resolved `ART`, its `TEMP` parent, attempt nonce, immediate ABSENT state, and that only this attempt's frozen harness may create it. After F11 result is complete and its pumps/handles close, runner independently writes `FS/collector-transfer.tsv`, proving every source relative path, byte count, and SHA appears at exact destination `FS` with no source/destination substitution. Only when that transfer proof and ownership chain both close may runner delete the one exact resolved `ART`: it rechecks parent=`TEMP`, basename=`dsh-article36-x01-artifacts`, nonce/owner path, and refuses any reparse/containment mismatch. It then requires `ART` absent. If ownership or transfer fails, do not delete the root; if deletion is partial/fails or absence cannot be proved, preserve remnants and FAIL. Unknown or pre-existing roots are never deleted.

Runner also removes only exact temporary `SPEC` after proving it was created by this attempt; never touch a pre-existing file. Only after both `ART` and `SPEC` cleanup receipts close may it run F12-F16. The original head, tag, status, and diff helpers (F13-F16) must prove identity/cleanliness equal before. Any ownership, transfer, cleanup, or postflight failure preserves evidence and is formal failure; no retry.

Formal leaf budget is `15 node +35 pnpm +150 ten Git helpers +15 Get-FileHash +25 no-index +190 focused +70 collector =500 s`. Runner 530 s (`500+30` cleanup/margin), top 540 s (`530+10` grace), verifier 70 s (`60+10`), total 610 s (`540+70`). Each nested recorder timeout is matrix T plus pump grace; inequalities: `530>=500+30`, `540>=530+10`, `610>=540+70`.

Success requires: provenance before invocation; complete recorder results; independent manifest/hash PASS; approval/protected identities PASS; exact before/after/cleanup identity; expected exits; focused test PASS; collector PASS; and unchanged Article36 acceptance mapping for cost, compaction trigger, trace/parent identity, cancellation propagation, recovery, and no-retry safety. `assessment/evidence-map.tsv` maps each fixed criterion to concrete raw files and SHA without changing hypothesis/baseline. Failure is preserved and stops; no retry, repair, backfill, rename, or second attempt.

Formal success returns `PASS/true/LAB_OBSERVATION/NONE`. Formal failure returns exactly `FAIL/false/LAB_EXECUTE/FAILED_LAB`. A separate fresh `LAB_ENGINEER/LAB_OBSERVATION` then reads and organizes raw evidence only: it runs no fixture command, creates no experiment root, and cannot change receipts. Observation PASS may proceed to `RESEARCHER/EVIDENCE_MERGE`; it never upgrades failed formal.

## 9. Closed worker envelope and transitions

Every role returns exactly one root `worker_result` with exactly 11 fields: `role`, `article`, `gate`, `execution_type`, `status`, `artifacts_created`, `artifacts_modified`, `gate_completed`, `next_allowed_gate`, `blocker`, `notes`. `article` is `"36"`; `execution_type` is `REAL_SUBAGENT`; arrays are exact path arrays; `notes` is a string array. Only canonical roles: design/evidence `RESEARCHER`, RED/implementation/GREEN/compatibility `REVISION_WORKER`, review `REVIEWER`, formal execution/observation `LAB_ENGINEER`, projections `MASTER_ORCHESTRATOR`. Process verifier is a tool, never a worker role.

Canonical path: `RESEARCHER/REVISION -> REVIEWER/REVIEW_RECHECK (design) -> REVISION_WORKER/REVISION -> REVIEWER/REVIEW_RECHECK (fresh preflight) -> LAB_ENGINEER/LAB_EXECUTE -> LAB_ENGINEER/LAB_OBSERVATION -> RESEARCHER/EVIDENCE_MERGE`. Status is only `PASS|FAIL|BLOCKED`. Both review failures are `FAIL/true/NONE/FAILED_REVIEW`; formal failure is `FAIL/false/LAB_EXECUTE/FAILED_LAB`. No role may claim old failures repaired.

## 10. Self-review

- Scope: one design file now; future architecture four files <=24 KiB; Article37+, commits, pushes, old roots, and Master projections excluded.
- TDD: isolated RED precedes implementation; GREEN proves deliberately injected start and exit failure receipts survive before compatibility.
- Survivability: CreateNew+Flush(true) marker precedes fragile work; catch/finally direct text, concurrent pumps, bounded kill/drain/close, and result/emergency precedence cover every signed 32-bit exit.
- Provenance/verification: every token frozen in owner before launch; verifier owns neither target nor manifest inputs; empty JSON argv requires `JsonValueKind.Array`; hashes self-excluding/recomputable.
- Root ownership: top recorder owns only `execution/top-process`, Tests/runner owns only the initially absent `execution/subject`, nested paths stay below subject, and external verifier writes only sibling roots; no matrix row shares top and subject ownership.
- Fixed experiment: repo/tag/full commit, absolute Corepack path/SHA across compatibility/approval/formal, exact original helpers, verbose Vitest, Node, harness/collector semantics, hypothesis, acceptance, baseline, bounded owned ART transfer/cleanup, and postflight checks are unchanged. Compatibility cannot consume formal. Each stage one-shot; failure terminal evidence.
