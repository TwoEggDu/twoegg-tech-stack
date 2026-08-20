# Lab 03 Execution Log

- Execution owner: `Lab Engineer`
- Execution date: `2026-08-20`（Asia/Shanghai）
- Formal execution window: `2026-08-20T13:13:44.4517708+08:00` to `2026-08-20T13:14:36.1180813+08:00`
- Continuation verification window after the first interrupted orchestration turn: `2026-08-20T13:21:19.8978167+08:00` to `2026-08-20T13:21:46.3123471+08:00`
- Provider / network / credentials: `NONE / NOT USED / NONE`
- Frozen Design before execution: `30312 bytes / 555 lines / SHA-256 242F28DB7151E4AA3359B4C22F526A98D2C476A48D27C85DB7752BBE0DDCDD86`

## Environment inventory

`dotnet --info` completed with exit `0` before implementation. Observed target values:

```text
.NET SDK Version: 10.0.301
SDK Commit: 96856fd726
MSBuild Version: 18.6.4+96856fd72
OS Name: Windows
OS Version: 10.0.19045
RID: win-x64
Host Version: 10.0.9
Host Architecture: x64
Microsoft.NETCore.App 10.0.9: installed
OS runtime description: Microsoft Windows 10.0.19045
Process architecture: X64
Timezone: China Standard Time / UTC+08:00
```

This exactly matched the frozen Windows / .NET target. `global.json` pins SDK `10.0.301` with roll-forward disabled; `NuGet.Config` clears package sources. Both projects are `net10.0`, BCL-only, and have no `PackageReference`.

At continuation inspection, one idle-looking `dotnet.exe` process was visible (`PID 38980`, start `2026-08-20 13:08:26`, low accumulated CPU). Its command line was not accessible in the current permission boundary, so it is recorded only as an observed background SDK process, not as completion evidence and not killed. Every Lab command below independently returned its own exit code within the bounded tool wait.

## Formal command ledger

| Command | Start | End | Exit | Observed result |
|---|---|---|---:|---|
| `dotnet --info` | pre-implementation | pre-implementation | 0 | frozen SDK / OS / RID / Host matched |
| `dotnet restore .\MinimalAgentLoop.slnx --locked-mode --nologo --verbosity minimal` | 13:13:44.4517708 | 13:13:45.5413156 | 0 | projects already current under locked restore |
| `dotnet build .\MinimalAgentLoop.slnx -c Release --no-restore --nologo --verbosity minimal` | 13:13:57.6706612 | 13:13:58.7974752 | 0 | 0 warnings / 0 errors |
| `dotnet test .\MinimalAgentLoop.slnx -c Release --no-build --no-restore --nologo --verbosity minimal` | 13:14:06.2541110 | 13:14:07.4558934 | 0 | BCL spec runner PASS; `4 / 10 / 4 / 10 / 7 / 10 / 1` |
| `dotnet run --project .\src\MinimalAgentLoop\MinimalAgentLoop.csproj -c Release --no-build --no-restore -- --cases .\fixtures\cases.json --out .\observations\run-a` | 13:14:15.9503056 | 13:14:16.8695642 | 0 | four-case suite PASS |
| `dotnet run --project .\src\MinimalAgentLoop\MinimalAgentLoop.csproj -c Release --no-build --no-restore -- --cases .\fixtures\cases.json --out .\observations\run-b` | 13:14:28.1765271 | 13:14:29.1383761 | 0 | second fresh-process four-case suite PASS |
| `dotnet .\tests\MinimalAgentLoop.Tests\bin\Release\net10.0\MinimalAgentLoop.Tests.dll --verify-only .\observations\run-a .\observations\run-b` | 13:14:35.9626625 | 13:14:36.1180813 | 0 | schema / count / digest / cross-reference / byte equality PASS |

Continuation after the orchestration turn was interrupted did not rewrite `run-a` or `run-b`. It rechecked the final project and existing raw artifacts:

| Command | Start | End | Exit | Observed result |
|---|---|---|---:|---|
| same locked restore command | 13:21:19.8978167 | 13:21:20.9059630 | 0 | current / locked |
| same Release build command | 13:21:27.2856355 | 13:21:28.3949929 | 0 | 0 warnings / 0 errors |
| same BCL test command | 13:21:36.1563749 | 13:21:37.3787851 | 0 | contract tests PASS |
| same `--verify-only run-a run-b` command | 13:21:46.1560459 | 13:21:46.3123471 | 0 | existing formal artifacts PASS |

## Raw fixture inventory

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `build.log` | 100 | `B87AFA6690B65BDD62521A1B79DC8A4AA93C8AA61B01596FDA2B0CF1897C70F3` |
| `BuildMenu.cs` | 59 | `DA684C745F501FFDEEC25A89D750DD1460FEB16653FB87636D53780B558FB3F1` |
| `Unrelated.cs` | 68 | `64FFA7EE32D5D16630D4AA79B211EBBBD113DA27ADC52BB50C00588EF8B045E7` |
| `cases.json` | 6166 | `ED2F677D9D3F3BDF6E79C697A3964A189D2EE88D61CBA45E20858737A3D0E47D` |

`cases.json` was parsed by the runtime and the independent specs. A case-insensitive scan for `expected_*`, `expected outcome`, `termination_reason`, `run_outcome`, `success_bool`, and `assertion_result` returned no match (`rg` exit `1`, meaning no matching line). It contains scripted candidates, limits, fault target, goal contract ID, and fixture paths, but no expected answer.

## Formal raw artifact inventory

Each formal fresh-process directory contains 6 files and 47,772 bytes. Across `run-a` and `run-b`: 12 files / 95,544 bytes.

| Artifact in each run | Bytes | run-a SHA-256 | run-b SHA-256 | Byte equal |
|---|---:|---|---|---|
| `artifact-manifest.json` | 1105 | `6B1E3148DF5812B92A155BCEB29783B540CF9D4E8576D9012388A6B73ACD00E6` | same | PASS |
| `case-results.jsonl` | 2506 | `90F2256AA18E401C6DDCEFFFB0837AB25105C58A80A3D24D3A87ADFD907D157D` | same | PASS |
| `observations.jsonl` | 3842 | `5A446F0327571D33AECFFB2B642C71A3CC9D28ADBE9E5341BAF8FD5D21809586` | same | PASS |
| `states.jsonl` | 11800 | `88F3E541C1A17FD44AA924ACB912C62B9C387F0669EF0F071979AD94A750E729` | same | PASS |
| `tool-outcomes.jsonl` | 4041 | `128FE933B0CFF633949B0EDABEF6B4294379D119C4174D2F08EBF420B54A1332` | same | PASS |
| `trace.jsonl` | 24478 | `3B816B5B7E2E370EED38268F02E83B045EAEDC6EAB9CEC801266ADD76D4D6427` | same | PASS |

Per run, the independent verifier observed exactly: 4 cases, 10 `STEP`, 4 `TERMINAL`, 10 state snapshots, 7 Tool Outcomes, 7 normalized Observations, 7 tool calls, 10 decision calls, and 1 `SUCCEEDED`.

## Case and fault observations

| Case | Lifecycle | Termination | Outcome | Steps / decisions / tools |
|---|---|---|---|---|
| AL-01 | `STOPPED` | `GOAL_SATISFIED` | `SUCCEEDED` | `3 / 3 / 2` |
| AL-02 | `STOPPED` | `UNRESOLVED_TOOL_FAILURE` | `FAILED` | `2 / 2 / 1` |
| AL-03 | `STOPPED` | `MAX_STEPS_EXHAUSTED` | `INCOMPLETE` | `2 / 2 / 2` |
| AL-04 | `STOPPED` | `STOP_CONTRACT_FAILED` | `FAILED` | `3 / 3 / 2` |

- AL-02 raw Tool Outcome: `FAILED / MOCK_PARSE_FAILED`, record SHA-256 `B39ED180065C66D1115C1ACC0A50F98204FC6B066A44DC0046BF0091D51C13A4`.
- AL-02 normalized Observation: `PASS / TOOL_FAILURE / MOCK_PARSE_FAILED`; its `source_result_record_sha256` is the same `B39E...13A4` value.
- AL-03 consumed `al03-decision-01` and `al03-decision-02`; `al03-decision-03` remained in `remaining_decision_ids`. Decision calls and tool calls both remained 2.
- AL-04 invocation IDs were `al04-call-01` and `al04-call-02`; both action fingerprints were `C25D1F779277059899AC5145991CB185E76ECC525CB5945ED741E07CCDFD9049`.
- AL-04 semantic result payload SHA-256 matched (`B86849C2F9F1A691648FD1DA4655D2FD0675F916A86026D7654CC9DDF61B4CED`), while correlated record SHA-256 differed (`7E4D...355B` vs `EA6F...8521`).
- AL-04 full-state SHA-256 changed on both reads; goal-state SHA-256 stayed `BF5111E00F2A28F270C7EAF159C1766E86D9D70D908F324CE7A6FCACD711879B`; both progress rows were `NO_PROGRESS`.
- `EV-FAKE` appeared only in rejected evidence, and AL-04 stopped failed.

## Preserved failed attempts

These attempts happened before the final green chain and were not erased from this log:

1. Environment enrichment: `Get-CimInstance Win32_OperatingSystem` returned `Access denied`. The independent runtime and `dotnet --info` OS values still exactly matched the frozen target; the denied call was not used as evidence.
2. First Release build: exit `1`, `0 warnings / 3 errors`; all three were `CS0136` local-name shadowing for `beforeFull`, `beforeGoal`, and `beforeRevision` in `LabRunner.cs`. Names were scoped correctly; the next build passed.
3. First BCL test: exit `1`; `build.log differs from frozen content`. Hex inspection showed a second LF at EOF in all three exact fixtures. Only that extra blank line was removed; frozen fixture text was not changed.
4. Next `dotnet test`: the custom BCL specs themselves printed PASS, but SDK VSTest then attempted unavailable `testhost 18.6.0-release-26270-133` and returned exit `1`. The test project was changed to an explicit SDK import and a BCL-owned `VSTest` target; no NuGet test dependency was added. Final `dotnet test` executes that independent spec binary and exits `0`.
5. One direct spec attempt: exit `1`, `full-state digest mismatch`. The writer had retained live dictionary references in earlier in-memory snapshots, so later reducer changes altered earlier rows before serialization. Snapshots now clone facts and unresolved-failure records at commit; subsequent digest verification passed.
6. Orchestration interruptions: Master interrupted two Lab Engineer turns while they were organizing the Markdown execution log / README Observation after the formal commands had already ended. No Lab command was running at either interruption. These were orchestration/log-delivery interruptions, **not Lab execution failures**. Formal normalized artifacts remained present and unchanged; after the first interruption the continuation reran locked restore/build/test and read-only artifact verification, all exit `0`. After the second interruption, the verifier inspected the already-written log/README and did not start another validation round.

No failed case was painted green: AL-02 and AL-04 remain `FAILED`, AL-03 remains `INCOMPLETE`, and exactly AL-01 is `SUCCEEDED`.

## Reproduction notes and limitations

- Run from this Lab directory on the frozen target. The offline `NuGet.Config`, pinned `global.json`, lock files, exact fixtures, and relative paths are required.
- Normalized artifacts contain no wall clock, PID, random GUID, run-directory name, or absolute path. Execution metadata appears only in this log.
- The suite is deterministic and read-only. It does not call a Provider, network, MCP server, database, shell tool, or repository path outside `fixtures/`.
- Observations cover only the frozen scripted substitute, Host reducer, four cases, named fault, and artifact reproducibility. No Claim status or general framework conclusion is assigned here; Evidence Merge remains owned by the Researcher.
