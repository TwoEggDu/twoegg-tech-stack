# Article 28 Baseline Probes

Status: `STRUCTURED OBSERVATIONS COMPLETE / EVIDENCE MERGE PENDING`

Durable evidence form: this file retains direct structured observations with commands, environment, exit codes, terminal summaries, failure classification and sanitized excerpts. It does not retain the complete stdout/stderr stream from the build or full-test processes; the sections below are a structured, sanitized record rather than a full raw log.

## 1. Frozen experiment design

### Research questions

1. Does the external fixture resolve to the official `dsh-v0.1.2-alpha.1` commit and remain clean before and after the probes?
2. Can the pinned project-manager version install from the frozen lockfile and build the checked-out revision on this Windows host?
3. What is the honest result of the complete unit-test entry on this host, including failures that may be environment-specific?
4. Does the built CLI expose its help surface, and can a headless profile resolve and dump its effective configuration without credentials?
5. When credentials are deliberately absent, does a bounded headless run fail closed before a model-backed task completes?

### Hypotheses and falsification conditions

| Probe | Hypothesis | Falsified when |
|---|---|---|
| Identity | `HEAD`, the annotated/lightweight tag target and remote tag resolve to the pinned commit; worktree is clean | Any identity differs, origin is not the official repository, or tracked/untracked fixture changes exist before execution |
| Install | Corepack selects project-pinned pnpm and a frozen-lockfile install completes | Package-manager version differs, the lockfile would change, or install exits non-zero |
| Build | The declared root build completes from the pinned source/dependency state | Build exits non-zero or does not reach its terminal success state |
| Full unit test | The complete root unit-test dispatcher can be measured on this Windows host | Any test failure or harness failure occurs; targeted follow-up cannot replace this result |
| Built CLI help | The built CLI responds to `--help` without starting a service | It exits non-zero, requires credentials, or launches a listener/task |
| Headless config dump | A fresh isolated `DSH_HOME` can resolve the headless effective config without credentials | It exits non-zero, touches a non-isolated home, or needs a real provider call |
| Keyless headless run | With telemetry disabled, read-only permission and secret-like variables removed, the supported headless entry fails closed for missing provider credentials | It reaches a provider/model result, exposes a credential, mutates the DSH source, or succeeds despite the intended absence of credentials |

### Fixture and environment

- External fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Expected repository: `https://github.com/deepseek-ai/deepseek-harness`
- Expected tag: `dsh-v0.1.2-alpha.1`
- Expected commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Host: current Windows machine; exact OS, architecture and tool versions will be captured as direct structured observations.
- DSH homes: two newly created directories below the OS temporary directory, one for config dump and one for the keyless run. Their paths are runtime fixtures, not course assets.

### Inputs and acceptance criteria

- Install: `corepack pnpm install --frozen-lockfile --offline` is preferred because dependencies are already present; if the local store is insufficient, the unchanged frozen-lockfile command may be retried with bounded network settings and the retry must be recorded.
- Build: `corepack pnpm run build`.
- Test: `corepack pnpm run test`; preserve complete summary and exit code. A focused rerun may classify failures but cannot upgrade the full result.
- CLI help: `node apps/cli/lib/bin.js --help`.
- Config dump: built CLI, headless profile, isolated `DSH_HOME`, and the CLI's supported config-dump argument discovered from `--help`.
- Keyless run: supported built headless CLI; bounded inert prompt; isolated `DSH_HOME`; telemetry disabled; read-only permission; process environment filtered so variable names matching `KEY`, `SECRET`, `TOKEN`, or `PASSWORD` are absent.
- A probe passes only its own acceptance criterion. Install success does not imply build success; build success does not imply tests or runtime success; config resolution does not imply an Agent Turn.

### Safety boundary

- Do not obtain, read, print or use a real provider credential.
- Do not start a server or bind a public interface.
- Do not use production data, repositories or writable project inputs.
- Do not modify DSH source. Natural ignored build/test artifacts and isolated temporary homes are allowed, then fixture cleanliness must be rechecked.
- Treat DSH as experimental developer-preview software, not security-audited or production-ready. Sandboxing and permission controls reduce risk but do not guarantee isolation.
- Scrub command output before it enters this document; retain only variable names or redacted failure text, never secret values.

## 2. Direct structured observations

All timestamps and outcomes below are direct observations from this Lab Engineer unless a row is explicitly labeled `MASTER PRECHECK CONTEXT`.

### Probe A — fixture identity and toolchain

```text
Local time: 2026-08-30T03:16:38+08:00
OS: Microsoft Windows NT 10.0.19045.0
Architecture: X64
Node: v24.18.1
Project-pinned pnpm: 11.7.0
Global pnpm: 11.19.0
npm: 11.16.0
Git: git version 2.53.0.windows.2
PowerShell: 7.6.4
origin: https://github.com/deepseek-ai/deepseek-harness.git
HEAD: cd5ef8148158c3a752a658978873241fdf8e2bbc
local tag target: cd5ef8148158c3a752a658978873241fdf8e2bbc
remote tag target: cd5ef8148158c3a752a658978873241fdf8e2bbc
initial status rows: 0
```

The first remote query inside the sandbox exited `1` because GitHub was unreachable. The unchanged read-only `git ls-remote` query exited `0` under narrow network escalation. A package-manager version query issued from the wrong repository attempted a blocked npm lookup; the in-fixture query selected pnpm `11.7.0` and is the accepted result.

**Result:** `PASS`. Local and fresh remote identity agree. This does not guarantee future tag immutability.

### Probe B — frozen install

Command:

```text
corepack pnpm install --frozen-lockfile --offline
```

Terminal facts:

```text
Scope: all 265 workspace projects
Already up to date
Done in 583ms using pnpm v11.7.0
exit: 0
```

Two warnings identified Linux-only landlock packages as unsupported on the current `win32/x64` host. No lockfile modification was reported.

**Result:** `PASS` for a populated local store. This does not prove clean-machine or registry availability.

`MASTER PRECHECK CONTEXT`, not this Lab's direct probe run: two earlier online frozen-lockfile attempts exited `1` after network timeouts; a third bounded-network attempt exited `0` and installed 1011 packages. These historical outcomes explain why the direct probe preferred offline mode, but they do not alter its exit code.

### Probe C — build and sandbox discrimination

Command:

```text
corepack pnpm run build
```

Attempt 1, normal sandbox:

```text
Host/client compilation and bundling progressed.
Vite/esbuild: Cannot read directory "../../../../../..": Access is denied.
Could not resolve apps/web/vite.config.ts.
scripts/build.ts: build:web exited with 1.
exit: 1
```

Attempt 2, unchanged command under narrow host-filesystem escalation:

```text
Host build: complete
Client build: complete
Web: 345 modules transformed; built in 3.39s
Build record: 218 client artifacts; 2 public values
exit: 0
```

Non-fatal output included plugin timing hints, dependency-bundling hints, Linux-only landlock platform warnings and a Vite chunk-size warning.

**Result:** `PASS_WITH_HOST_ACCESS_CAVEAT`. The first result is retained as sandbox evidence; the successful retry proves current-host buildability only with the required access.

### Probe D — complete unit-test entry

Command:

```text
corepack pnpm run test
```

Final summary:

```text
Test Files  32 failed | 965 passed | 4 skipped (1001)
Tests       129 failed | 15939 passed | 66 skipped (16134)
Duration    305.24s
exit: 1
```

Observed failure classes:

1. Windows symlink creation `EPERM` across filesystem, workspace, skill, spill, snapshot and doc-site tests.
2. Windows ACL/sandbox failures, including `CreateRestrictedToken failed (Win32 87)`.
3. Process lifecycle and teardown timeouts across subprocess, LSP, PowerShell and real-product fixtures, plus some `EBUSY` cleanup errors.
4. Network-restricted real-product/plugin activity.
5. Independent-looking assertions, including a PowerShell persistent end marker, an LLM Retry-After case and a directory-picker home case; no common cause was established.

**Result:** `FAIL`. The suite is not passing on this Windows/sandbox execution.

### Probe E — focused timing counterexample

The first focused form, `corepack pnpm exec vitest ...`, exited `1` because Windows command resolution did not find `vitest`. The repository-local module entry was then used:

```text
node node_modules/vitest/vitest.mjs run scripts/gen-third-party-notices.spec.ts --testTimeout=30000
Test Files  1 passed (1)
Tests       27 passed (27)
test body   4.94s
duration    5.79s
exit: 0
```

**Result:** the notices failure in the full run is timing-sensitive at the default 5 s threshold. The full suite remains `FAIL`; no other failure is cleared by this probe.

### Probe F — built CLI help

Command:

```text
node apps/cli/lib/bin.js --help
```

The built CLI printed usage plus `--profile`, repeatable `--patch`, `--dump-config`, `--dump-default-config`, `web` and `plugin` entries, then exited `0`.

**Result:** `PASS` for CLI surface availability. No profile was activated and no service started.

### Probe G — isolated headless effective-config dump

Conditions:

```text
DSH_HOME=C:\Users\IGG\AppData\Local\Temp\dsh-a28-config-33bbc1ea732d4b2caf5717d9e144a14e
node apps/cli/lib/bin.js --profile headless --dump-config
exit: 0
```

The output contained the base composition followed by the headless patch. Representative rows included `agent-loop`, `llm-deepseek`, `headless-startup` and `headless-runner`; permission and telemetry remained configuration expressions rather than proof of later execution.

**Result:** `PASS` for effective config resolution. It does not prove Loader activation, Agent construction, a provider call or a completed Turn.

### Probe H — isolated credential-free headless run

Conditions:

```text
DSH_HOME=C:\Users\IGG\AppData\Local\Temp\dsh-a28-keyless-a19008e99cc34861af2eb15fb81db2c9
DSH_TELEMETRY_MODE=DISABLED
DSH_PERMISSION_MODE=read-only
inherited environment names matching KEY|SECRET|TOKEN|PASSWORD after filtering: 0
bounded task: reply with one inert word and do not call tools
timeout: 30 seconds
timed out: false
child exit: 1
stdout/model result: empty
```

Sanitized stderr, containing a credential name but no value:

```text
dsh: MISSING_CREDENTIAL: llm-deepseek: no API key for provider route "deepseek-official"; store DEEPSEEK_API_KEY through the credentials service (the web Models page writes it), or export DEEPSEEK_API_KEY in the launching environment
```

**Result:** `EXPECTED_FAIL / RUNTIME_PARTIAL`. The path reaches credential resolution and fails closed without a provider credential. It does not confirm an Agent Turn, model request, network response, token use or cost.

### Probe I — final fixture integrity

```text
HEAD: cd5ef8148158c3a752a658978873241fdf8e2bbc
local tag target: cd5ef8148158c3a752a658978873241fdf8e2bbc
final status rows: 0
```

**Result:** `PASS`. Build/test natural artifacts remained ignored; no DSH source change was introduced.

## 3. Structured-observation gate result

- Identity: `PASS`.
- Install: `PASS_FROM_POPULATED_OFFLINE_STORE`.
- Build: `PASS_WITH_HOST_ACCESS_CAVEAT`.
- Full unit test: `FAIL`.
- Built CLI/config resolution: `PASS`.
- Completed headless Agent Run: `NOT_CONFIRMED`; intentional keyless probe ended at `MISSING_CREDENTIAL`.
- Credential, provider response and cost: `NONE OBSERVED`.
- Fixture integrity: `PASS / CLEAN`.
- Next allowed gate: `EVIDENCE_MERGE`.
