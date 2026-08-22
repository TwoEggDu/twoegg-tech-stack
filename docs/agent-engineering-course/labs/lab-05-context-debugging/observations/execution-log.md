# Lab 05 execution log

- Gate: `LAB_EXECUTE`
- Execution owner: `LAB_ENGINEER / REAL_SUBAGENT`
- Fixture: `lab05-fixture-v1`
- Working directory for all frozen commands: `docs/agent-engineering-course/labs/lab-05-context-debugging` (recorded as `.` in machine evidence)
- Provider / model / network / credentials: `NONE / NONE / NONE / NONE`
- Evidence interpretation: deferred to Researcher `EVIDENCE_MERGE`

## Chronology

1. Read the frozen Design and repository/course contracts. The frozen `README.md` SHA-256 was `566755e49eba1473ffe6ee373afd72250aee309b478d8996004461c8c6998ae1` before execution.
2. Authored the independent executable behavioral Specs and input-only `fixtures/cases.json` before any Runtime project existed. The Specs contain literal behavioral expectations for mandatory Cases A-G, invoke the public Runtime process, and inspect only public artifacts. The Specs project has no Runtime project reference.
3. Created the compile/start-only Runtime shell. It printed `NOT_IMPLEMENTED` to stderr and exited `3`; it contained no case behavior.
4. Ran `dotnet --info`, `dotnet --version`, `dotnet restore ContextDebuggingLab.slnx --configfile NuGet.Config --locked-mode`, and `dotnet build ContextDebuggingLab.slnx --configuration Release --no-restore`. All exited `0`; raw streams are under `observations/environment/`. The initial wrapper did not retain start timestamps; this is disclosed in `limitations.json`. The final independent rerun retained exact times.
5. Mandatory RED ran from `2026-08-22T12:37:02.8031814+08:00` to `2026-08-22T12:37:02.9159843+08:00`:
   - Command: `dotnet tests/ContextDebuggingLab.Specs/bin/Release/net10.0/ContextDebuggingLab.Specs.dll verify-runtime --runtime src/ContextDebuggingLab/bin/Release/net10.0/ContextDebuggingLab.dll --fixtures fixtures/cases.json --output observations/tdd-red`
   - Spec exit: `1` (required nonzero); Runtime shell exit observed by Specs: `3`.
   - Behavioral failures: exactly one mandatory public-artifact failure for each Case A, B, C, D, E, F and G.
   - Raw evidence: `observations/tdd-red/{command.json,stdout.txt,stderr.txt,result.json,runtime-stdout.txt,runtime-stderr.txt,source-state.json}`.
6. After RED evidence was durable, replaced only the Runtime shell behavior with the minimal generic revision/relevance/conflict/compression/budget/receipt implementation. The fixture and Spec bytes were not changed.
7. First post-implementation Release build failed with compiler `CS0411` at three `JsonValue.Create` method-group sites. Root cause: the overloaded generic method group was ambiguous to LINQ type inference. Raw output remains at `observations/tdd-green/build-attempt-01.*`. The only correction was to use explicit lambdas at those three sites.
8. Second Release build exited `0`, with `0` warnings and `0` errors. Raw output remains at `observations/tdd-green/build-attempt-02.*`.
9. First GREEN behavioral run ran from `2026-08-22T12:43:20.0944849+08:00` to `2026-08-22T12:43:20.3150188+08:00`, exited `0`, and passed all 15 assertions: seven per-case file-contract assertions, seven mandatory A-G behavior assertions, and the suite manifest assertion. The first output is preserved as `stdout-attempt-01.txt` and `command-attempt-01.json`.
10. First formal run A, verifier A, run B, verifier B and compare all exited `0` between `2026-08-22T12:44:13.6916605+08:00` and `2026-08-22T12:44:14.2145045+08:00`. Their process evidence is retained with the `.attempt-01` suffix.
11. Final independent environment/restore/build rerun occurred between `2026-08-22T12:45:57.3343605+08:00` and `2026-08-22T12:45:59.8192721+08:00`; every command exited `0`, and the Release build again had `0` warnings and `0` errors.
12. Final independent GREEN rerun occurred from `2026-08-22T12:46:27.0780305+08:00` to `2026-08-22T12:46:27.2701178+08:00`; exit `0`, all 15 assertions passed.
13. Final independent formal rerun used fresh Runtime processes:
    - run A: `2026-08-22T12:47:09.8554987+08:00` to `2026-08-22T12:47:09.9611821+08:00`, exit `0`.
    - verifier A: `2026-08-22T12:47:09.9789226+08:00` to `2026-08-22T12:47:10.0661879+08:00`, exit `0`.
    - run B: `2026-08-22T12:47:10.0678907+08:00` to `2026-08-22T12:47:10.1648043+08:00`, exit `0`.
    - verifier B: `2026-08-22T12:47:10.1660592+08:00` to `2026-08-22T12:47:10.2584326+08:00`, exit `0`.
    - compare: `2026-08-22T12:47:10.2603984+08:00` to `2026-08-22T12:47:10.3456940+08:00`, exit `0`.
14. A PowerShell-only secondary audit helper initially used unsupported `ReadOnlySpan<byte>` casts and emitted `InvalidArgument`; that invalid helper result is preserved and explicitly labeled. The corrected helper used structural byte-array equality, then independently confirmed 58/58 manifest entries for each run, direct byte equality, aggregate equality, unchanged frozen inputs/Specs, zero project/package references, zero embedded fixture answers, and `git diff --check` exit `0`. Git emitted only an unrelated LF/CRLF warning for the pre-existing parent-owned `course-run-state.md` change.

## Observed behavior

- A: `GOOD_CONTEXT`; required contributors, ordering ledger and output reserve are retained.
- B: both `STALE` and `REVISION_MISMATCH` preserve `expected=rev17`, `actual=rev14`, source ref and provenance.
- C: `POLLUTION` identifies the old tool result, obsolete plan and unrelated history solely from frozen relevance facts.
- D: `CONFLICT_UNRESOLVED` retains both `Build failed.` and `Build succeeded.` with their distinct build-job provenance and unresolved marker.
- E: `BAD_COMPRESSOR_V1` emits exactly `Root cause confirmed.`; the independent verifier detects uncertainty, conflict, provenance and claim-strength loss from preserved pre/post records.
- F: optional-pressure retains all P0/P1, reserves four output units and omits optional history first; required-overflow returns `REQUIRED_EVIDENCE_BUDGET_EXCEEDED / FAIL_CLOSED` and an explicit absent-Snapshot marker.
- G: receipt metadata is `AUDITABLE`; absent original bytes plus absent/unresolvable locator yields `NOT_RECONSTRUCTABLE` with `DIGEST_NOT_CONTENT`, while Provider-internal context stays `UNKNOWN_UNSUPPORTED`.
- Repeatability: run A/B relative file sets, byte lengths, direct bytes, per-file SHA-256 and aggregate SHA-256 are identical. Final aggregate SHA-256 is recorded in `observations/repeatability.json`.

## Boundaries

No real model, Provider, network, HTTP service, credential, database, MCP, tokenizer or deployment was used. The observation supports only the frozen local fixture boundary. It does not establish Provider behavior, model consumption, production reliability, semantic replay or full-token reconstruction.
