# Lab 04 execution log

All commands ran from `docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint`. Exit codes below are observed process exit codes. No command contacted a Provider or package source.

## Command history

| Seq | Command | Exit | Observed result |
|---:|---|---:|---|
| 1 | `dotnet --info` plus read-only OS / timezone probes | `0` overall | SDK `10.0.301`, Host `10.0.9`, Windows `10.0.19045`, `win-x64`, timezone `China Standard Time`; CIM sub-probe emitted `Access denied` |
| 2 | `dotnet restore .\LongRunningAgentLab.slnx --locked-mode --configfile .\NuGet.Config` | `0` | both BCL-only projects restored offline with cleared sources |
| 3 | `dotnet build .\LongRunningAgentLab.slnx -c Release --no-restore` | `1` | intentional TDD red: Runtime had no entry point; `CS5001`, `0 warnings / 1 error` |
| 4 | `dotnet build .\LongRunningAgentLab.slnx -c Release --no-restore` | `0` | Runtime implementation added; `0 warnings / 0 errors` |
| 5 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll static-contract` | `1` | first static check incorrectly scanned SDK-generated `obj/...GlobalUsings.g.cs` and reported `System.Net` |
| 6 | `dotnet build .\LongRunningAgentLab.slnx -c Release --no-restore` | `0` | verifier patched to inspect authored Runtime source only; `0 warnings / 0 errors` |
| 7 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll static-contract` | `0` | `runtime_isolated=true`, `bcl_only=true`, fixture expected-answer fields absent, runtime network/provider surface `0`, cases `8` |
| 8 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll formal-suite --suite run-a --output .\observations\run-a` | `0` | LR-01—LR-08 verified; 12 fresh Runtime child processes |
| 9 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll formal-suite --suite run-b --output .\observations\run-b` | `0` | LR-01—LR-08 independently verified; 12 new Runtime child processes |
| 10 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll compare --left .\observations\run-a --right .\observations\run-b` | `0` | 105 normalized files byte-identical; aggregate SHA-256 `27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a` |
| 11 | frozen Design byte check followed by `dotnet restore .\LongRunningAgentLab.slnx --locked-mode --configfile .\NuGet.Config` | `0` | Design remained 17833 UTF-8 bytes / SHA-256 `0146c43137ad2386397cc38fdea866731942a9e56ec0d55f2fbf57619c9d3101`; projects up-to-date offline |
| 12 | `dotnet build .\LongRunningAgentLab.slnx -c Release --no-restore` | `0` | final fresh Release build; `0 warnings / 0 errors` |
| 13 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll static-contract` | `0` | final fresh contract pass; runtime isolation/BCL/no-answer/no-network/no-provider all true |
| 14 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll formal-suite --suite run-a --output .\observations\run-a` | `0` | final fresh run-a; 8 cases / 12 new Runtime child processes |
| 15 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll formal-suite --suite run-b --output .\observations\run-b` | `0` | final fresh run-b; 8 cases / 12 new Runtime child processes |
| 16 | `dotnet .\tests\LongRunningAgentLab.Specs\bin\Release\net10.0\LongRunningAgentLab.Specs.dll compare --left .\observations\run-a --right .\observations\run-b` | `0` | final fresh compare; 105 files byte-identical with the same aggregate SHA-256 |
| 17 | read-only final acceptance audit (frozen Design hash + static-contract + compare + 16 manifest rehashes + case terminals + fresh PID pairs + external-access counters) | `0` | `FINAL_ACCEPTANCE_AUDIT PASS`; Design 17833 bytes unchanged, 2 suites / 8 cases, 4 resume cases per suite, network/provider/credential counters all 0 |

## First failures, patches, accepted reruns

1. The first Release build failed with `CS5001` because tests/specs and project structure existed before production Runtime code. Patch: added the minimal Runtime CLI implementing the frozen state/fault behaviors. Accepted rerun: build exit `0`, `0 warnings / 0 errors`.
2. The first static-contract run failed because the source scanner recursively included SDK-generated `obj/Release/net10.0/LongRunningAgentLab.GlobalUsings.g.cs`; implicit SDK global usings contain `System.Net.Http`, while the authored Runtime source did not and the compiled Runtime assembly had no `System.Net*` reference. Patch: restricted source-token inspection to authored top-level `*.cs`; retained the independent PE assembly-reference check. Accepted rerun: static-contract exit `0` with network/provider surface `0`.
3. The first CIM OS-edition probe returned `Access denied`. No implementation patch was made. The execution record uses the successful `dotnet --info` and `RuntimeInformation` results and retains the probe failure as a limitation.

## Fresh child process evidence

### run-a

| Case | START PID / exit | RESUME PID / exit |
|---|---|---|
| LR-01 | `31708 / 0` | N/A |
| LR-02 | `47100 / 10` | `48912 / 0` |
| LR-03 | `16184 / 0` | N/A |
| LR-04 | `57556 / 11` | `9340 / 0` |
| LR-05 | `10044 / 11` | `18724 / 14` |
| LR-06 | `43768 / 11` | `8416 / 12` |
| LR-07 | `34344 / 13` | N/A |
| LR-08 | `57388 / 0` | N/A |

### run-b

| Case | START PID / exit | RESUME PID / exit |
|---|---|---|
| LR-01 | `48332 / 0` | N/A |
| LR-02 | `36800 / 10` | `53724 / 0` |
| LR-03 | `44212 / 0` | N/A |
| LR-04 | `45740 / 11` | `38864 / 0` |
| LR-05 | `52324 / 11` | `42180 / 14` |
| LR-06 | `56348 / 11` | `44852 / 12` |
| LR-07 | `37476 / 13` | N/A |
| LR-08 | `56024 / 0` | N/A |

The machine-readable PID/stdout/stderr records are `process-evidence-run-a.json` and `process-evidence-run-b.json`. PIDs, wall-clock timestamps, and absolute paths are excluded from each normalized run root.

### final completion-verification PIDs (machine-readable files currently on disk)

| Case | run-a START / RESUME | run-b START / RESUME |
|---|---|---|
| LR-01 | `2296 / N/A` | `55740 / N/A` |
| LR-02 | `33800 / 39584` | `53452 / 19760` |
| LR-03 | `57584 / N/A` | `56636 / N/A` |
| LR-04 | `57020 / 53748` | `32924 / 46352` |
| LR-05 | `34132 / 54784` | `56144 / 26324` |
| LR-06 | `11860 / 53120` | `47916 / 43860` |
| LR-07 | `35160 / N/A` | `56112 / N/A` |
| LR-08 | `50304 / N/A` | `58060 / N/A` |

## Fault/result summary

| Case | Observed terminal | Effects | Attempts | Key observation |
|---|---|---:|---:|---|
| LR-01 | `SUCCEEDED / GOAL_SATISFIED` | 1 | 1 | baseline controlled create |
| LR-02 | `CANCELLED / INCOMPLETE` then `SUCCEEDED` | 0 then 1 | 0 then 1 | caller cancel was pre-effect; fresh resume did not repeat evidence collection |
| LR-03 | `SUCCEEDED / GOAL_SATISFIED` | 1 | 2 | one retry approved inside max-attempt budget |
| LR-04 | `INTERRUPTED / UNKNOWN_SIDE_EFFECT` then `SUCCEEDED` | 1 | 2 | start checkpoint preserved `RESULT_UNKNOWN`; resume used same action/key and `CreateOrGet` found the existing record |
| LR-05 | `INTERRUPTED / UNKNOWN_SIDE_EFFECT` then `DUPLICATE_SIDE_EFFECT_DETECTED / FAILED` | 2 | 2 | unsafe resume used a new delivery and the verifier read two real store records |
| LR-06 | `INTERRUPTED / INVALID_CHECKPOINT_CANDIDATE` then `RECOVERY_REFUSED / IN_FLIGHT_ACTION_MISSING` | 1 | 1 | resume refused before fake-store call count changed; final access count remained 1 |
| LR-07 | `RETRY_BUDGET_EXHAUSTED / INCOMPLETE` | 0 | 2 | exactly two failed pre-apply attempts, then `ASK_OR_STOP` |
| LR-08 | `TIMED_OUT / INCOMPLETE` | 0 | 0 | deterministic timeout with origin `TIMEOUT`, no sleep and no caller-cancel event |

## Reproduction and limitations

- Re-run the seven frozen commands in README order. Existing `run-a` / `run-b` roots are only replaced after exact parent/name, sentinel, and reparse-point validation.
- Runtime writes are constrained to a validated `observations/run-*/LR-*` child. Fake store and checkpoint are separate files, not separate services or transactions.
- The named after-apply interruption is a deterministic Runtime terminal, not an OS crash or partial disk write.
- Single coordinator only; no concurrent caller, lease, race, lock, or split-brain behavior is tested.
- The verifier independently owns the acceptance matrix; Runtime reads only `fixtures/cases.json`, its checkpoint, trace, and fake store.
