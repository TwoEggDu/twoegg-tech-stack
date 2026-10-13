# 36-X01 Recovery Cycle 3 experiment plan

Status: `FROZEN RECOVERY DESIGN / NOT IMPLEMENTED / NOT EXECUTED / REQUIRES REVISION AND FRESH REVIEW`

## 1. Purpose, authority, and immutable baseline

Recovery Cycle 3 is one independent launcher recovery for the already frozen
36-X01 integrated case. Human authority covers only:

1. the truthful Cycle 2 disposition in
   `raw/36-x01/recovery-cycle-2/post-review-disposition.md`;
2. one absolute-Corepack launcher candidate and one launcher-only
   prevalidation;
3. a fresh independent recheck; and
4. only after that recheck passes, one focused Cycle 3 experiment.

The DSH authority remains tag `dsh-v0.1.2-alpha.1` at full commit
`cd5ef8148158c3a752a658978873241fdf8e2bbc`, with fixture root
`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` and repository
package manager `pnpm@11.7.0`.

The hypothesis in `research.md §10.2`, the falsifiers, P0--P5, the original ten
execution assertions, the safety boundary, harness semantics, one-case
composition, 180-second cap, and claim ceiling are unchanged. Recovery Cycle 3
does not turn Attempt 1, Cycle 1, or Cycle 2 into passing evidence.

No install/update, network access, server bind, credential/provider call,
owner-suite substitute, second harness, second focused attempt, fixture repair,
Article 37+ work, or change to Research/Evidence/README/state/status/trace is
authorized by this plan.

## 2. Single root-cause hypothesis

### RED evidence

The unchanged Cycle 2 `preflight-environment.json` records the bare invocation
`corepack pnpm --version`, `invocation_executable=corepack.cmd`, exit `1`, and
`MODULE_NOT_FOUND` for the fixture-relative path:

```text
C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814\node_modules\corepack\dist\corepack.js
```

The process working directory was the fixture and fixture-local
`node_modules/.bin` had been prepended to `PATH`. Consequently no pnpm version
was established, the focused command count remained `0`, and no P0--P5 runtime
observation occurred.

### Current read-only identity observation

Before any new PATH mutation, the current host resolves the system Corepack
launcher to:

```text
C:\nvm4w\nodejs\corepack.cmd
```

That file currently exists and its design-time observed SHA-256 is
`920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F`.
The pinned fixture's `.bin` currently contains the Vitest shims
`vitest`, `vitest.CMD`, and `vitest.ps1`, and no Corepack shim.

These are design-time observations, not prevalidation receipts. The Revision
Worker must independently resolve and persist them. A mismatch is a fail-closed
prevalidation result, not permission to search for or install another
launcher.

### One hypothesis and falsifier

The single recovery hypothesis is: Cycle 2 failed at the launcher boundary
because a bare `corepack` / `corepack.cmd` invocation remained sensitive to the
fixture working directory and PATH mutation; resolving and hashing the system
Corepack absolute path before prepending fixture `.bin`, then invoking only
that absolute path, will preserve fixture-local Vitest resolution while
removing the ambiguous Corepack entry point.

This hypothesis is falsified if the reviewed absolute Corepack path does not
exist or lies inside the fixture, its hash changes, absolute Corepack cannot
return exact pnpm `11.7.0`, or absolute Corepack cannot resolve fixture-local
Vitest. Any falsifier stops before the target spec. No alternate launcher,
install, update, network action, or retry is permitted.

## 3. Frozen launcher implementation

The Revision Worker may create exactly one durable launcher candidate:

```text
experiments/36-x01-recovery-cycle-3-launcher.ps1
```

The launcher must implement this order without reordering:

1. Accept the exact fixture root, mode, receipt root, and, for experiment mode,
   the reviewed expected Corepack absolute path and SHA-256.
2. Before changing `PATH`, resolve the host `corepack.cmd` application to one
   absolute filesystem path. Multiple matches, no match, a relative path, or a
   path inside the fixture is a closed failure.
3. Confirm the resolved file exists, compute SHA-256, and persist the path,
   hash, resolver evidence, and pre-PATH value needed for audit. In experiment
   mode require exact equality with the fresh-reviewed path/hash.
4. Read fixture `package.json` and require exact
   `packageManager=pnpm@11.7.0`.
5. Set WorkingDirectory to the pinned fixture and prepend only that fixture's
   `node_modules/.bin` to the current launcher process `PATH`. Do not persist a
   user/machine environment change.
6. Invoke every pnpm/version/focused process through the previously captured
   absolute Corepack path. No executable call may use bare `corepack` or
   `corepack.cmd` after PATH mutation.
7. Capture each invoked process's exact executable path, argument vector,
   WorkingDirectory, stdout bytes/text, stderr bytes/text, exit code, start/end
   status, and timeout status without merging streams.
8. Emit a closed summary and explicit exit code. A failed check or process is
   terminal for that mode.

The only permitted launcher modes are:

- `PREVALIDATE`: launcher identity plus the two version probes below; target
  spec execution is structurally forbidden.
- `EXECUTE_FOCUSED`: re-resolve and re-hash before PATH mutation, match the
  reviewed identity, run the one frozen focused command, and expose its exact
  receipts. It must not install, update, enable Corepack, or run any other test.

Source and receipt audit must establish
`bare_corepack_invocation_count=0`. The resolver lookup is not a Corepack
execution; all actual process invocations must name the absolute path.

## 4. One launcher-only prevalidation

The Revision Worker is limited to creating the launcher candidate and running
one launcher-only prevalidation. It must not create the temporary target spec,
run the target spec, invoke the collector, change the candidate harness, or
touch the DSH fixture beyond read-only command/module resolution performed by
the two version probes.

The sole durable attempt directory is:

```text
experiments/preflight/36-x01-recovery-cycle-3-launcher/attempt-1/
```

It must be absent before the attempt and contain, at minimum:

```text
exact-launcher-command.txt
working-directory.json
corepack-identity.json
package-manager.json
pnpm-version-command.txt
pnpm-version-stdout.txt
pnpm-version-stderr.txt
pnpm-version-exit-code.txt
vitest-version-command.txt
vitest-version-stdout.txt
vitest-version-stderr.txt
vitest-version-exit-code.txt
bare-command-audit.json
prevalidation-summary.json
cleanup-receipt.json
manifest.json
```

The one prevalidation must prove all of the following:

1. Corepack was resolved before PATH mutation to one absolute path outside the
   fixture; expected current path is
   `C:\nvm4w\nodejs\corepack.cmd`.
2. The Corepack file exists and its exact SHA-256 is fixed in the receipt. The
   current design-time candidate is
   `920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F`;
   any difference fails closed for fresh review.
3. Fixture `package.json` declares exact `pnpm@11.7.0`.
4. From fixture WorkingDirectory, the absolute Corepack invocation equivalent
   to `pnpm --version` exits `0` and stdout, after only newline normalization,
   equals exact `11.7.0`; stderr must be captured.
5. From the same WorkingDirectory and process-scoped PATH, the absolute
   Corepack invocation equivalent to `pnpm exec vitest --version` exits `0`,
   and the receipt proves the resolved Vitest entry is fixture-local. Exact
   stdout is retained; it must identify a Vitest version rather than execute a
   test.
6. Bare Corepack commands were never executed. The exact absolute executable
   and argument vector for both probes are retained.
7. No target spec, test-owned runtime source root, network/server/provider
   operation, install/update, or fixture write occurred.
8. Exact stdout, stderr, exit code, command identity, WorkingDirectory,
   launcher/Corepack identity, cleanup state, and a self-excluding exact
   bytes/SHA-256 manifest are durable.

`cleanup-receipt.json` must record that the target temporary spec and
test-owned runtime source root are absent and that no cleanup deletion was
needed. It must also record fixture HEAD/tag/cleanliness using read-only checks
without changing the fixture.

Any failure makes prevalidation `FAIL`; there is no Attempt 2 and no in-place
rewrite of Attempt 1 receipts. The next route is back to
`EXPERIMENT_DESIGN`, and the target experiment remains forbidden.

## 5. Frozen harness and collector identities

Recovery Cycle 3 must reuse these candidates byte-for-byte:

| Candidate | SHA-256 | Disposition |
|---|---|---|
| `experiments/36-x01-recovery-cycle-2-instrumentation.ts` | `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B` | Frozen; no edit |
| `experiments/36-x01-recovery-cycle-2-collector.ps1` | `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045` | Frozen; no edit |

The Revision Worker and later Lab Engineer must hash both files and compare
them with these exact values. A mismatch returns to design; it does not permit
a repair in this recovery cycle.

## 6. Mandatory fresh recheck

After the launcher-only prevalidation, a fresh independent Reviewer must read:

- this plan and the Cycle 2 post-review disposition;
- the launcher candidate source and complete attempt-1 receipts;
- the fixed Corepack absolute path/hash, packageManager identity, exact pnpm
  and Vitest version probe outputs, bare-command audit, cleanup, and manifest;
- the unchanged candidate harness and collector hashes;
- fixture HEAD/tag/cleanliness and absence of the temporary spec/source root;
- current durable state and Article 37+ guards.

The Reviewer must fail if the script can execute a bare Corepack command, if
resolution occurs after PATH mutation, if target-spec execution is reachable
in `PREVALIDATE`, if identity/outputs are incomplete, if any prevalidation
artifact was rewritten after failure, or if any forbidden write/command
occurred.

Only a fresh `REVIEW_RECHECK PASS` may recommend `EXPERIMENT_EXECUTE`. The
prevalidation itself is not DSH runtime evidence and does not authorize the Lab
Engineer without Master validation.

## 7. One Cycle 3 execution and raw isolation

The independent raw root is fixed as:

```text
experiments/raw/36-x01/recovery-cycle-3/
```

It must be absent at the start of the one Lab attempt. Attempt 1, Cycle 1, and
Cycle 2 files remain immutable. The Lab Engineer must first recheck the pinned
HEAD/tag and clean fixture, reviewed launcher/Corepack path/hash, exact
candidate harness/collector hashes, fixture-local Vitest shim, and package
manager declaration. Any mismatch stops before the target command.

The approved candidate harness is copied byte-for-byte to the one temporary
spec:

```text
packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts
```

The focused command semantics remain exactly:

```text
pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

The exact executable invocation must, however, record and use the reviewed
absolute launcher, currently expected as:

```powershell
& 'C:\nvm4w\nodejs\corepack.cmd' pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

The launcher executes it from the fixture WorkingDirectory exactly once, in
one attempt, with a 180-second wall-clock cap. A bare `corepack` or
`corepack.cmd` command is forbidden. No alternate launcher or retry is
allowed.

Command stdout/stderr/exit and launcher receipts must first be captured in a
test-owned staging location. Immediately after the focused process terminates,
the Lab Engineer creates the previously absent Cycle 3 destination as an empty
directory and invokes the frozen collector against the test-owned harness raw
source. The collector transfer and its closed receipt occur before deletion of
the temporary spec or any test-owned source/staging root. Only after transfer
and receipt capture may cleanup begin.

If the runtime source root is missing, the collector must truthfully return its
failure; no runtime file may be synthesized. After the collector step, the Lab
Engineer may append the launcher/preflight identity, command streams, collector
receipt, timing/selection/P0--P5 summary, reproduction/limitations, hashes,
cleanup/postflight, and final self-excluding manifest to the Cycle 3 raw root.
Failure receipts remain part of the raw attempt.

Cleanup removes only the one temporary spec and test-owned temporary roots,
then proves the fixture remains at exact pinned HEAD/tag with empty status and
passing diff check. Failure at command, collector, capture, manifest, or cleanup
is terminal and must not be retried.

## 8. Immutable hypothesis, phases, acceptance, safety, and claim ceiling

The frozen hypothesis remains: if the target composition mounts the required
services, the same durable Session log should expose per-attempt usage and
whole-session accumulation, a source-defined compaction
trigger/bracket/replacement, the before/after current-request change,
cooperative cancellation request/observation/settlement/terminal ordering, and
fresh-resume reconstruction from the durable prefix; external Tool state stays
`UNKNOWN` without an explicit receipt/reconcile owner.

The phase sequence is unchanged:

```text
P0 Baseline
-> P1 Long Session / Usage
-> P2 Source-defined Compaction
-> P3 Before / After Diff
-> P4 Cancellation / Durable terminal
-> P5 Dispose + Fresh Resume
```

The original ten execution assertions remain the only acceptance set:

1. selection is `1` and the integrated case completes P0--P5;
2. conversation attempts are at least two, native usage has raw events, and
   same-attempt usage is not double counted;
3. pressure crosses the real threshold and the automatic source listener
   creates the complete compaction bracket;
4. old raw events remain while current surface/history/next request show the
   explained replacement change;
5. cancel request, signal observation, body settlement, Tool terminal, and turn
   terminal remain distinct;
6. after durable-prefix flush and first-runtime disposal, a fresh Context
   resumes the same Session with an unchanged prefix;
7. `known_work=KNOWN`, the cancelled sentinel remains `UNKNOWN`, and no
   automatic rollback/reconcile/retry is claimed;
8. Context/Model/Tool/Policy/Error/Usage correlate through session, seq,
   turn/step, and available call/compaction identities while model-attempt-ID
   absence is explicit;
9. real Provider, network, server, credential, and production-path use are all
   zero; and
10. cleanup returns the fixture to the exact pinned and clean state.

Safety remains one integrated harness, one case, one attempt, 180 seconds,
fixture/test-owned temporary writes only, no install/update/network/server/
credential/real Provider/production side effect, and mandatory cleanup. A
mocked passing trace remains fixture-bounded and cannot prove provider billing,
production backend semantics, summary fidelity, hard cancellation, rollback,
or fresh-process recovery.

Success alone may hand raw observation to the Researcher. Any missing item,
nonzero target exit, selection other than `1`, incomplete P0--P5, assertion or
observer failure, timeout, capture/collector/manifest/cleanup failure, or
identity drift keeps Article 36 behavior claims at `BLOCKED_EVIDENCE`.

## 9. Worker-result mappings and stop rules

The Revision Worker is complete only after the candidate launcher and one
successful launcher-only prevalidation are independently reviewable. Its next
candidate is `REVIEW_RECHECK`; it does not authorize the target experiment.

For the future Lab Engineer:

```text
valid failure:
  status: FAIL
  gate_completed: false
  next_allowed_gate: LAB_EXECUTE
  blocker: FAILED_LAB

valid success:
  status: PASS
  gate_completed: true
  next_allowed_gate: RAW_OBSERVATION
  blocker: NONE
```

`LAB_EXECUTE` after failure is only a recovery candidate. With
`stop_on.required_lab_failure=true`, `STOP POLICY WINS`; no worker or Master may
retry without a new external Human Resume. `RAW_OBSERVATION` is allowed only
after the one attempt meets all unchanged acceptance and artifact contracts.

## 10. Design Gate disposition

This plan freezes a single-variable launcher repair: resolve and hash absolute
system Corepack before PATH mutation, prepend fixture `.bin` afterward, and use
only the captured absolute executable for pnpm/version/focused invocations.
No launcher candidate, prevalidation receipt, temporary spec, target command,
collector execution, runtime raw, or Article 37+ asset was created by this
Researcher design.

The Design Gate may pass to `REVISION` only. Experiment execution remains
forbidden until the bounded Revision Worker output and a fresh independent
recheck both pass Master validation.
