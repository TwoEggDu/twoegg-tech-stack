# Lab 01 Execution Record

- Executed at: `2026-08-20T00:42:30.7668366+08:00`
- Source revision before Lab commit: `b359a329df02ce7487b0cb1a9feaad66c886d4dc`
- Working directory: `docs/agent-engineering-course/labs/lab-01-structured-output-validation/`
- Provider calls: `0`
- Credential reads: `0`
- Automatic repair attempts: `0`

The entries below were transcribed from the actual command results. Initial failures are retained rather than rewritten as successful first attempts.

## Command Results

1. `dotnet --info`
   - Exit code: `0`
   - Observed SDK: `10.0.301`
   - Full output: [`dotnet-info.txt`](dotnet-info.txt)
2. `dotnet restore .\StructuredOutputValidation.slnx --use-lock-file`
   - First sandboxed attempt exit code: `1`
   - Raw failure class: `NU1301`; socket access to `https://api.nuget.org/v3/index.json` was denied by the sandbox.
   - Approved retry of the identical restore command exit code: `0`
   - Raw success: both source and test projects restored.
3. `dotnet restore .\StructuredOutputValidation.slnx --locked-mode`
   - First sandboxed attempt exit code: `1`
   - Raw failure class: `NU1301`; NuGet attempted package metadata reads for `xunit.runner.visualstudio`, `xunit`, and `Newtonsoft.Json`, and socket access was denied.
   - Approved retry of the identical locked restore command exit code: `0`
   - Raw success: both source and test projects restored from the lock files.
4. `dotnet build .\StructuredOutputValidation.slnx --configuration Release --no-restore`
   - Initial exit code: `1`
   - Raw compiler failure: `CS0246` for missing `Fact` / `FactAttribute`; 10 errors, 0 warnings.
   - Disposition: added the missing `using Xunit;` in the test source; no Design, schema, fixture, expected result, or implementation behavior changed.
   - Intermediate retry exit code: `0`; 1 analyzer warning, 0 errors.
   - Final retry after the analyzer-only test cleanup exit code: `0`; 0 warnings, 0 errors.
5. `dotnet test .\StructuredOutputValidation.slnx --configuration Release --no-build --logger "console;verbosity=detailed"`
   - First exit code: `0`; 5 passed, 0 failed.
   - Final exit code: `0`; 5 passed, 0 failed; total time `0.7532 s`.
   - Passed tests: schema/DTO parity; Domain rules; all-eight-case matrix; first-failure short-circuit; automatic repair attempts equal zero.
6. `dotnet run --project .\src\StructuredOutputValidation\StructuredOutputValidation.csproj --configuration Release --no-build -- --cases .\fixtures\cases.json --schema .\schema\diagnosis-candidate.schema.json --allowlist .\fixtures\evidence-allowlist.json --output .\artifacts\observation.jsonl`
   - First exit code: `0`
   - Raw stdout: `Wrote 8 observation rows`; `Accepted cases: 1`; `Automatic repair attempts: 0`.
   - Second, identical command exit code: `0`
   - Raw stdout: `Wrote 8 observation rows`; `Accepted cases: 1`; `Automatic repair attempts: 0`.
7. SHA-256 and byte comparison
   - `artifacts/observation-first.jsonl`: `C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`
   - `artifacts/observation.jsonl`: `C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`
   - Result: `BYTE_IDENTICAL=True`
   - Second artifact: 8 lines, 8 unique case IDs, 1 accepted case.

## Final Matrix Summary

| Terminal stage | Count |
|---|---:|
| `ACCEPTED` | 1 |
| `PARSE_FAILED` | 3 |
| `SCHEMA_FAILED` | 3 |
| `DOMAIN_FAILED` | 1 |

All eight observed terminal stages, error codes, and recommended actions matched the frozen case metadata. This is a raw local-fixture result; Evidence interpretation and Claim status remain owned by the Researcher.
