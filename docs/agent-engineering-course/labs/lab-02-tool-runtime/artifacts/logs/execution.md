# Lab 02 execution log

- Working directory for every command: `E:\workspace\TechStackShow\docs\agent-engineering-course\labs\lab-02-tool-runtime`
- Shell: PowerShell; frozen command text is shown verbatim below. Timestamp markers were emitted by an outer recording wrapper and are not part of the frozen command.
- Stream evidence: complete `dotnet --info` stdout is in `dotnet-info.txt`; other captured stdout/stderr is in `execution.raw.log`. Successful commands had empty stderr.
- Timezone: all timestamps carry explicit `+08:00` offset.

| ID | Exact command | Start | End | Exit | Stdout | Stderr | Disposition |
|---|---|---|---|---:|---|---|---|
| ENV-01 | `dotnet --info` | `2026-08-20T07:10:15.3102963+08:00` | `2026-08-20T07:10:16.0167811+08:00` | 0 | `dotnet-info.txt` | empty | accepted; SDK / OS / RID exact |
| RESTORE-01 | `dotnet restore .\ToolRuntimeLab.slnx --configfile .\NuGet.Config` | `2026-08-20T07:10:27.7932891+08:00` | `2026-08-20T07:10:29.2602998+08:00` | 0 | `execution.raw.log` | empty | rejected attempt; missing ProjectReference was reported twice |
| RESTORE-02 | `dotnet restore .\ToolRuntimeLab.slnx --configfile .\NuGet.Config` | `2026-08-20T07:11:08.2028187+08:00` | `2026-08-20T07:11:09.4467186+08:00` | 0 | `execution.raw.log` | empty | accepted after path-only patch |
| BUILD-01 | `dotnet build .\ToolRuntimeLab.slnx --configuration Release --no-restore` | `2026-08-20T07:11:22.2422240+08:00` | `2026-08-20T07:11:25.6502945+08:00` | 0 | `execution.raw.log` | empty | accepted; 0 warnings / 0 errors |
| SETUP-FIRST-01 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-fixture.ps1 -RunLabel first` | `2026-08-20T07:11:41.1986005+08:00` | `2026-08-20T07:11:41.7554705+08:00` | 1 | timestamp markers | `execution.raw.log` | failed before temp creation; Windows PowerShell lacks `Path.GetRelativePath` |
| SETUP-FIRST-02 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-fixture.ps1 -RunLabel first` | `2026-08-20T07:12:44.2209060+08:00` | `2026-08-20T07:12:44.8568386+08:00` | 0 | `execution.raw.log` | empty | accepted; real junction and exact fixture hashes |
| RUN-FIRST | `dotnet run --project .\tests\ToolRuntimeLab.Specs\ToolRuntimeLab.Specs.csproj --configuration Release --no-build -- --manifest .\fixtures\cases.json --run-label first --trace .\artifacts\observation-first.jsonl` | `2026-08-20T07:12:57.5091138+08:00` | `2026-08-20T07:12:58.6455520+08:00` | 0 | `execution.raw.log` | empty | accepted; 12 cases / 14 rows |
| CLEANUP-FIRST-01 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\cleanup-fixture.ps1 -RunLabel first` | `2026-08-20T07:13:11.5033143+08:00` | `2026-08-20T07:13:12.0299379+08:00` | 1 | timestamp markers | `execution.raw.log` | failed safely; `Remove-Item` junction NullReferenceException; root preserved |
| CLEANUP-FIRST-02 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\cleanup-fixture.ps1 -RunLabel first` | `2026-08-20T07:14:24.0603887+08:00` | `2026-08-20T07:14:24.5473272+08:00` | 0 | `execution.raw.log` | empty | accepted after reparse-point-only deletion patch |
| SETUP-SECOND | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-fixture.ps1 -RunLabel second` | `2026-08-20T07:14:37.1199285+08:00` | `2026-08-20T07:14:37.6995166+08:00` | 0 | `execution.raw.log` | empty | accepted; fresh root and real junction |
| RUN-SECOND | `dotnet run --project .\tests\ToolRuntimeLab.Specs\ToolRuntimeLab.Specs.csproj --configuration Release --no-build -- --manifest .\fixtures\cases.json --run-label second --trace .\artifacts\observation.jsonl` | `2026-08-20T07:14:49.8069058+08:00` | `2026-08-20T07:14:50.9444506+08:00` | 0 | `execution.raw.log` | empty | accepted; fresh process, 12 cases / 14 rows |
| CLEANUP-SECOND | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\cleanup-fixture.ps1 -RunLabel second` | `2026-08-20T07:15:02.5513724+08:00` | `2026-08-20T07:15:03.0456038+08:00` | 0 | `execution.raw.log` | empty | accepted; guarded root removed |
| HASH-FIRST | `Get-FileHash -Algorithm SHA256 .\artifacts\observation-first.jsonl` | `2026-08-20T07:15:20.7485629+08:00` | `2026-08-20T07:15:20.9571542+08:00` | 0 | `execution.raw.log` | empty | accepted |
| HASH-SECOND | `Get-FileHash -Algorithm SHA256 .\artifacts\observation.jsonl` | `2026-08-20T07:15:34.4831163+08:00` | `2026-08-20T07:15:34.6872919+08:00` | 0 | `execution.raw.log` | empty | accepted |
| BYTE-COMPARE | BCL byte-array equality plus line count | `2026-08-20T07:16:19.9559257+08:00` | `2026-08-20T07:16:20.1767784+08:00` | 0 | `execution.raw.log` | empty | identical; 10607 bytes and 14 LF rows each |
| STATIC-AUDIT | PackageReference / cleanup / spill / view scan | `2026-08-20T07:16:57.0479800+08:00` | `2026-08-20T07:16:57.3058337+08:00` | 0 | `execution.raw.log` | empty | accepted; all supplementary checks passed |

## Failure history and allowed patches

1. `RESTORE-01` returned exit 0 but printed a missing ProjectReference because the spec project path used one extra `..`. The raw output was retained; only the relative ProjectReference was corrected before `RESTORE-02`.
2. `SETUP-FIRST-01` returned exit 1 before creating a temp root because Windows PowerShell's runtime lacks `Path.GetRelativePath`. The four cleanup/setup classifications were retained and implemented with fully-qualified parent-plus-separator comparison using `OrdinalIgnoreCase`.
3. `CLEANUP-FIRST-01` returned exit 1 while deleting the already validated junction because Windows PowerShell `Remove-Item` threw `NullReferenceException`. The guarded temp root and all evidence remained intact. The patch replaced only junction removal with `[IO.Directory]::Delete(path, false)` in the same PowerShell process; recursive root deletion still occurs only after absolute temp parent, prefix, sentinel, non-parent and remaining-reparse checks.

No failure output was deleted, no Expected field or case was changed, and no rerun overwrote the first trace.
