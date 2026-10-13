# Article 36 Recovery Cycle 10 — one caller-receipt Probe

Status: DESIGN_CANDIDATE / NOT_EXECUTED. Fresh independent design review must PASS before implementation. After implementation and parse-only validation, fresh independent preflight review of both scripts and frozen provenance must PASS BEFORE the sole caller invocation.

> 已有参考：`36-x01-recovery-cycle-9-plan.md` 与 `../subagent-trace.md#wr-a36-cycle9-implementation`。本设计只补 caller failure 留痕，不重跑 Cycle 9，也不推断其真实根因。采用 col-verify 的一个最小可观测验证块；本例不是 Unity 或 DSH 实验。

## 1. Question, hypothesis and falsifier

Question: Can a tiny caller persist invocation/source/start evidence before a parsing-sensitive child operation, then preserve its observed failure and actual child exit?

Diagnostic hypothesis: one synthetic, deliberately malformed expression can fail without leaving an empty evidence root. Falsified by missing/mismatched prelaunch provenance, missing actual error/exit, unexpected execution, or any acceptance row below failing.

This is a NEW infrastructure diagnostic hypothesis only. The original `36-x01-long-session-compaction-cancel-resume.md` hypothesis and acceptance, Cycle 2 frozen harness/collector, and every Cycle 1–9 artifact remain unchanged. It is NOT a formal recovery experiment or evidence of DSH behavior.

## 2. Frozen environment and paths

Working directory: `E:\workspace\TechStackShow`.

In this document `E` expands literally to `E:\workspace\TechStackShow\docs\agent-engineering-course\articles\36-dsh-cost-compaction-trace-cancellation-recovery\experiments`; `R` expands literally to `C:\Users\IGG\AppData\Local\Temp\dsh-a36-c10-caller-probe-20260907`. No runtime random suffix or alternate root.

Executable `P`: `C:\Users\IGG\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe`, current PowerShell Core 7.6.5 verified by Master (exact version/hash recorded before use). This is the new diagnostic environment, not a claim of Cycle 8/9 host identity. No profile, network, credentials, server, package manager or dependencies. Record caller/child PSVersion, PID, UTC, OS and cwd; do not dump environment variables.

Source baseline is still official `deepseek-ai/deepseek-harness`, tag `dsh-v0.1.2-alpha.1`, commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`. The separately restored source copy ends in `codex-dsh-part-vi-cd5ef814-cycle10-restored-20260907`; it has no installed dependencies and is not accessed by the Probe.

Implementation source allowlist (new files only):

- `E\36-x01-recovery-cycle-10-caller.ps1`
- `E\36-x01-recovery-cycle-10-subject.ps1`
- `E\preflight\36-x01-recovery-cycle-10-tests\invocation.txt`
- `E\preflight\36-x01-recovery-cycle-10-tests\source-identity.kv`

Before execution, implementation persists the exact following invocation (expand `E` to its literal path in `invocation.txt`):

```powershell
& 'C:\Users\IGG\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File 'E\36-x01-recovery-cycle-10-caller.ps1'
```

`source-identity.kv` records absolute path, byte count and SHA-256 of plan, both scripts, invocation and executable; computed from final reviewed bytes, never placeholders. No cycle source is copied or executed. Static `Parser.ParseFile` validation of the TWO script files is allowed before the Probe, without evaluating their ASTs. The intentionally malformed string is data, not a syntax error in either script. Changing source after freeze invalidates review.

## 3. Single observable verification block

Minimal scene: one caller process, one child PowerShell process, no grandchildren. Caller uses .NET IO and Process directly; no JSON, schema, module, serializer, Corepack, pnpm, Vitest, DSH, old launcher or old Tests.

The caller first refuses an existing `R` using `[System.IO.Directory]::Exists` / `File.Exists` (not Test-Path). It creates `R` once and writes closed files `started.kv`, `invocation.txt`, `source-identity.kv`, `child-command.txt` before `Process.Start`. Copy the already-frozen provenance bytes; do not regenerate an imaginary invocation. `started.kv` records actual start identity and `synthetic=true`. `child-command.txt` records literal executable, arguments and cwd passed to ProcessStartInfo. Child arguments are exactly `-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "E\36-x01-recovery-cycle-10-subject.ps1"`, with literal `E` expansion. UseShellExecute=false; redirect stdout/stderr; CreateNoWindow=true.

Subject emits one ASCII start token and its actual host identity. Inside try/catch it calls `[scriptblock]::Create('Test-Path -LiteralPath (')`. The unmatched parenthesis is intentional; no resulting scriptblock is invoked. Catch records the actual exception type/message and error ID on stderr, emits `SYNTHETIC_PARSE_CAUGHT` on stdout, and exits 23. If compilation unexpectedly returns, emit `UNEXPECTED_PARSE_SUCCESS` and exit 24. This is controlled fault injection, NOT a reproduction of the unknown Cycle 9 command or proof that Test-Path executed.

Caller waits at most 10 seconds. Subject output is bounded below 4 KiB per stream (truncate diagnostic text with an explicit truncation flag if necessary), permitting simple redirected text reads without Task/async machinery. Preserve captured decoded text as `stdout.txt` and `stderr.txt`; do not call these original byte streams. On normal child exit write `terminal.kv` with actual exit, PID, timeout=false, UTC, and actual stream lengths/hashes. Caller exits 0 only for complete observed expected failure (child exit 23 plus expected tokens and nonempty error); else exits 1. On caller exception write its actual exception to `caller-error.txt` if possible, plus truthful terminal fields; a missing receipt remains MISSING. On timeout terminate only this owned child, record whether termination/exit was actually observed, preserve evidence, fail. Never fabricate a zero exit or empty-array success.

| Observable | Required result |
|---|---|
| Prelaunch receipts | Four closed files precede Start in reviewed source; stored invocation and identity bytes equal frozen inputs |
| Source/host binding | Hashes recompute; child host identity and PID correlate with Process result |
| Synthetic failure | Start token, SYNTHETIC_PARSE_CAUGHT, actual parser exception, actual child exit 23 |
| Caller closure | Actual caller exit 0 AND terminal/streams complete; no unexpected-success token or timeout |
| Scope | One caller, one subject; forbidden invocation counts zero; historical hashes unchanged |

Read-only recheck inspects file contents/hashes and source order; timestamps alone do not prove write-before-launch. The source order plus successful child start is the bounded execution evidence. No claim of atomic crash-proof recording is made.

## 4. Budget, allowed writes and stop

Budget: exactly ONE invocation of caller, at most ONE child start, 10-second child cap and 30-second external command cap. Budget is consumed when the caller invocation is attempted, even if no receipt appears. No rerun or alternate root after failure. Existing `R` before first invocation is a state conflict: stop, do not clean or select another root.

Runtime writes ONLY under `R`: `started.kv`, `invocation.txt`, `source-identity.kv`, `child-command.txt`, `stdout.txt`, `stderr.txt`, `terminal.kv`, and conditional `caller-error.txt`. All survive success/failure; no cleanup. Implementation may additionally create a truthful post-run observation at `E\36-x01-recovery-cycle-10-observation.md`, reporting exact tool command/actual exit and observed versus missing artifacts; no retrospective fills in raw receipts.

Failure handling: preserve all outputs and return blocked; source/parser/hash/prelaunch failure is not a license to spend another Probe. Fresh Reviewer independently rechecks outcome and preservation; then STOP irrespective of success. RED/GREEN/compatibility/formal budgets and Article 37 authority remain ZERO. Success proves only this synthetic caller-receipt path, not original launcher readiness, Cycle 9 root cause, frozen collector compatibility or Article 36 Evidence Gate PASS.
