# 36-X01 Recovery Cycle 4 orchestration recovery plan

Status: `FROZEN RECOVERY DESIGN / NOT IMPLEMENTED / NOT EXECUTED / PASS TO REVISION ONLY`

## 1. Authority, purpose, and evidence boundary

The 2026-09-01 Human Resume authorizes one independent Recovery Cycle 4 for
Article 36. This cycle exists only because the sole Cycle 3 Attempt 2
orchestration failed before it copied the target spec or invoked launcher v2.
The authorized sequence is:

```text
isolated helper RED
-> one new durable Cycle 4 orchestrator
-> isolated helper GREEN
-> one Cycle 4 launcher-only prevalidation
-> fresh independent REVIEW_RECHECK
-> LAB_EXECUTE only if that review passes
```

This design does not authorize the Researcher to implement the orchestrator,
create test or prevalidation receipts, touch the DSH fixture, or execute any
command against it. It creates only this plan.

Cycle 3 Attempt 2 remains immutable `FAIL / HUMAN_DECISION_REQUIRED`. Its
historical orchestration source was not persisted and is therefore
`MISSING / NOT_PROVABLE`. No Cycle 4 artifact may claim to recover, repair,
reconstruct, or quote that missing source.

The surviving receipts prove only these historical facts:

- all five Git child-process records in `clean-before-copy.json` contain empty
  `argument_vector` arrays and exited `1` with Git usage output;
- the candidate copy was not attempted;
- launcher v2, Corepack, pnpm, Vitest, focused test, target test, and collector
  invocation counts were all `0`;
- the initial failure closure then encountered `String.Join` with a null empty
  nested-file list;
- the closed Attempt 2 tree contains six files, with a top self-excluding
  manifest covering `5 / 5` actual members, while the required nested launcher
  subtree is absent;
- fresh read-only verification found the target spec, runtime source root, and
  Cycle 3 raw root absent and the fixture pinned and clean.

Those receipts do **not** prove the spelling, parameter declaration, callsite,
or complete control flow of the missing historical orchestration helper. Cycle
4 therefore avoids the evidenced failure classes in a newly authored durable
candidate; it does not diagnose a more specific historical source bug.

## 2. Immutable inputs and identities

The following inputs are read-only throughout Cycle 4:

| Artifact / boundary | Frozen identity |
|---|---|
| DSH repository | `https://github.com/deepseek-ai/deepseek-harness` |
| DSH tag / commit | `dsh-v0.1.2-alpha.1` / `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| Fixture root | `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` |
| Cycle 3 plan | `F5A9770EDE088BCA3B17AF3C06E5EDED46DA859BAFE349B887AAA4B077E47336` |
| Cycle 3 plan-defect addendum | `208F4F7EFE7B65663AF940FE16694107825799D659EF6D3C2921A8256A0C73ED` |
| Cycle 3 launcher v2 | `2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E` |
| Cycle 3 Attempt 1 manifest | `4F81810A40552C7623BA2552CB7400BABF44C554BF5ED5FEB63FEE1B318A8DAB` / `21 of 21` |
| Cycle 3 Attempt 2 manifest | `E9DE132AFB24D0C44938905A8227A7862E1F55B1FD56FD882741E6272D77B284` / `5 of 5` |
| Frozen harness candidate | `36-x01-recovery-cycle-2-instrumentation.ts` / `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B` |
| Frozen collector | `36-x01-recovery-cycle-2-collector.ps1` / `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045` |
| Expected Corepack identity | `C:\nvm4w\nodejs\corepack.cmd` / `920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F` |
| Package manager / observed Vitest probe | `pnpm@11.7.0` / Cycle 3 Attempt 1 observed `Vitest 4.1.8` |

Cycle 3 Attempt 1, terminal Attempt 2, their manifests and receipts, launcher
v2, the harness, the collector, Article 36 Research/Evidence, course state,
status, README, trace, and every Article 37+ path are read-only for the later
Revision.

## 3. Minimal semantic delta

The only two defect-class corrections allowed in the new orchestrator are:

1. **Native argument-vector binding.** Every native helper must declare an
   explicit, non-reserved `[string[]]$ArgumentVector` parameter. Every callsite
   must bind it by name. The helper must snapshot it as a defined array before
   rendering native arguments, pass that same ordered vector to the process,
   and persist the exact ordered vector and count in the child receipt. It must
   not use `$args`, an implicit remaining-arguments channel, or an unnamed
   positional splat as the process argument source.
2. **Empty nested-file-list closure.** Manifest enumeration must always produce
   a defined array, including `[object[]]@()` for zero members. Equality must
   compare counts first and then compare ordered path/bytes/SHA records; it must
   not use `String.Join` over a possibly null value as the equality predicate.
   An empty nested launcher root must close truthfully as `file_count=0` and
   `files=[]` rather than throwing.

No other repair is allowed. In particular, Cycle 4 must not modify launcher
v2, change Corepack/PATH behavior, loosen the exact focused-prestate predicate,
change the copied candidate, alter cleanup semantics, install or update a
dependency, add a fallback launcher, or change the real experiment.

The new durable candidate is:

```text
experiments/36-x01-recovery-cycle-4-orchestrator.ps1
```

It is an independent implementation of this frozen Cycle 4 contract. It is
not a recovered version of the missing Cycle 3 orchestration source.

## 4. TDD contract: isolated RED, then GREEN

The durable test candidate is:

```text
experiments/36-x01-recovery-cycle-4-orchestrator.Tests.ps1
```

It must support two explicit phases using the same test file:

- `RED` uses two self-contained, deliberately defective witnesses: one drops a
  nonempty synthetic native argument vector before receipt capture; the other
  passes a null zero-member list into the historical failure-shaped Join
  closure. These witnesses exist only to make the two observed failure shapes
  executable without the missing historical source.
- `GREEN` loads the new orchestrator helpers in a non-running/library mode and
  applies the same assertions to the corrected implementation. It may launch
  only a safe local native child under a test-owned temporary root; it must not
  resolve, read, write, or invoke the DSH fixture, launcher v2, Corepack, pnpm,
  Vitest, target spec, or collector.

The RED phase is accepted only when its process exits nonzero and the summary
records both expected failures:

1. supplied synthetic vector count is nonzero while the defective receipt
   vector count is zero; and
2. the defective zero-member closure receives null and fails at Join.

The RED summary must state
`historical_orchestration_source=MISSING_NOT_PROVABLE` and
`witness_is_historical_reconstruction=false`. Matching the receipt failure
shape is not proof of the old source implementation.

The GREEN phase is accepted only when it exits `0` and proves all of the
following with the corrected candidate:

1. the helper parameter is exactly `ArgumentVector`, the synthetic native
   child receives the intended ordered vector, and the child receipt preserves
   the same count and element-by-element values;
2. a zero-member nested directory closes with a defined `files=[]`,
   `file_count=0`, and a valid self-excluding manifest;
3. the top closure includes that nested manifest, excludes only its own
   manifest, and count-aware member comparison passes;
4. both phases use distinct previously absent receipt roots and distinct
   test-owned temporary roots, and cleanup removes only those temporary roots.

RED and GREEN are helper verification, not launcher prevalidation and not DSH
runtime evidence. An unexpected RED pass, missing expected RED failure,
GREEN failure, receipt mismatch, cleanup failure, or manifest mismatch is a
terminal Revision failure; it does not authorize an implementation tweak and
rerun in the same Cycle.

The exact RED and GREEN receipt trees are identical in shape:

```text
experiments/preflight/36-x01-recovery-cycle-4-orchestrator-tests/
  red/
    exact-test-command.txt
    process.json
    stdout.txt
    stderr.txt
    exit-code.txt
    test-summary.json
    cleanup-receipt.json
    manifest.json
  green/
    exact-test-command.txt
    process.json
    stdout.txt
    stderr.txt
    exit-code.txt
    test-summary.json
    cleanup-receipt.json
    manifest.json
```

Each phase manifest excludes only its own `manifest.json` and covers the other
seven files by exact relative path, bytes, and SHA-256.

## 5. Exact later-Revision write allowlist and order

After Master validates this design, one Revision Worker may create only these
repository artifacts:

```text
experiments/36-x01-recovery-cycle-4-orchestrator.ps1
experiments/36-x01-recovery-cycle-4-orchestrator.Tests.ps1
experiments/preflight/36-x01-recovery-cycle-4-orchestrator-tests/red/
experiments/preflight/36-x01-recovery-cycle-4-orchestrator-tests/green/
experiments/preflight/36-x01-recovery-cycle-4-orchestrator/attempt-1/
```

All five paths must be absent at Revision start. No create, modify, delete, or
rename outside this list is allowed. Within the DSH fixture, the only temporary
write is the exact target spec copied during the Cycle 4 prevalidation; it must
be removed by exact literal path. Test-owned OS-temp roots may be created only
for isolated RED/GREEN and must be removed by their own cleanup receipts.

The Revision order is frozen:

1. create the durable test candidate;
2. run isolated RED once and close its receipts;
3. create the durable orchestrator candidate with only the two corrections in
   section 3;
4. run isolated GREEN once and close its receipts;
5. only after the RED and GREEN contracts both pass, perform the one Cycle 4
   launcher-only prevalidation;
6. close the prevalidation receipts and stop for fresh review.

The worker may not edit test expectations after RED, rewrite RED after GREEN,
rewrite a failed prevalidation, or take a second RED, GREEN, or prevalidation
attempt.

## 6. Master pre-run checks

Before dispatching Revision, the Master must perform a fresh read-only
reconciliation and require every item below:

1. current branch is `main`; current Article is `36`; current Gate is the
   Cycle 4 `REVISION` candidate after this design is validated; Article 36
   transaction authorization is active and Article 37 is not authorized;
2. Article 35 completion still resolves from Git history and remote refs, and
   current Article 36 working-tree scope remains isolatable from unrelated
   user changes;
3. every identity in section 2 matches current bytes; Cycle 3 Attempt 1 and
   Attempt 2 manifests independently recompute exactly and their trees are not
   changed;
4. the five future repository paths in section 5 are absent;
5. fixture HEAD/tag are the pinned values, full porcelain status is empty,
   unstaged and staged tracked diffs are empty, and the exact target spec is
   absent;
6. test-owned runtime source root
   `C:\Users\IGG\AppData\Local\Temp\dsh-article36-x01-artifacts` and
   `experiments/raw/36-x01/recovery-cycle-3/` are absent;
7. Article 37 has zero workspace/content/assets and Articles 38--44 remain
   forbidden with zero assets;
8. no DSH process, target test, collector, server, provider, credential,
   install, update, network, commit, push, or publication action is active or
   implied by the Revision brief.

Any mismatch hard-stops before creating the test file. The Master must not
repair the mismatch, reinterpret Cycle 3 receipts, or widen the allowlist.

Immediately before the one Cycle 4 prevalidation, the Revision Worker must
repeat checks 3--7 and additionally require exact RED/GREEN receipt manifests,
RED expected-failure confirmation, and GREEN `PASS`.

## 7. One Cycle 4 launcher-only prevalidation

The only Cycle 4 launcher prevalidation root is:

```text
experiments/preflight/36-x01-recovery-cycle-4-orchestrator/attempt-1/
```

It must be absent before execution. There is exactly one orchestration and one
launcher invocation. The frozen order is:

### Stage A -- strict clean before copy

1. Resolve one absolute `git.exe` and capture these five read-only child calls
   with their exact nonempty `argument_vector`, stdout, stderr, exit code, and
   working directory:

   ```text
   -C <fixture> rev-parse HEAD
   -C <fixture> describe --tags --exact-match HEAD
   -C <fixture> diff --quiet --
   -C <fixture> diff --cached --quiet --
   -C <fixture> status --porcelain=v1 --untracked-files=all
   ```

2. `clean-before-copy.json` passes only when HEAD/tag are exact, all five calls
   have the intended argument vector and expected exit, full status is empty,
   both tracked diffs are empty, the target/runtime/Cycle 3 raw roots are
   absent, and launcher v2/harness/collector hashes match section 2.
3. Copy the exact `D5DF...A0199B` harness candidate byte-for-byte to:

   ```text
   C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814\packages\compaction\compaction-basic\tests\36-x01-long-session-compaction-cancel-resume.spec.ts
   ```

4. The copy receipt must prove exact source/destination paths, byte count and
   SHA equality, one exact porcelain line for the untracked target, `ls-files`
   exit `1`, `check-ignore -q` exit `1`, and no other fixture change.

### Stage B -- unchanged launcher v2, exactly once

Invoke the unchanged launcher SHA
`2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E`
exactly once with:

```text
Mode = VALIDATE_FOCUSED_PRESTATE
FixtureRoot = C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814
ReceiptRoot = <attempt-1>/launcher
AttemptIdentity = 36-X01-RECOVERY-CYCLE-4-PREVALIDATION-ATTEMPT-1
ExpectedCorepackPath = C:\nvm4w\nodejs\corepack.cmd
ExpectedCorepackSha256 = 920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F
```

The launcher must exit `0` and retain its existing v2 closed contracts. Its
receipts must prove exactly two child probes, both through the captured
absolute Corepack executable, with exact vectors:

```text
pnpm --version
pnpm exec vitest --version
```

Required counts are:

```text
orchestrator_invocation_count = 1
launcher_invocation_count = 1
launcher_child_process_count = 2
prevalidation_probe_count = 2
focused_command_count = 0
target_spec_execution_count = 0
collector_invocation_count = 0
bare_corepack_invocation_count = 0
```

The prevalidation must not call `EXECUTE_FOCUSED`, run Vitest with `run`,
create the test-owned runtime source root, create Cycle 3 raw evidence, or
invoke the collector.

### Stage C -- exact removal, final proof, and closure

1. In a `finally` path after the single launcher returns or throws, remove only
   the exact target spec by literal path if it exists. Never use `git clean`,
   reset, checkout, stash, recursive fixture deletion, wildcard deletion, or
   any cleanup of a second path.
2. Record whether the target existed, whether removal was attempted, the exact
   path removed, any error, and final absence. If copy never occurred, record
   `NOT_ATTEMPTED / TARGET_ABSENT` rather than inventing a removal.
3. Capture fresh final Git child receipts with exact argument vectors and prove
   pinned HEAD/tag, empty full status, empty unstaged/staged diffs, target,
   runtime source, and Cycle 3 raw roots absent, process PATH restored, and no
   other cleanup attempted.
4. Close `attempt-summary.json`, then the nested and top self-excluding
   manifests. The top manifest is written last.

Any failure in Master pre-run, RED/GREEN, clean-before-copy, copy, launcher,
probe identities/counts, target removal, final fixture proof, receipt schema,
or either manifest makes Cycle 4 Attempt 1 terminal `FAIL`. There is no Cycle 4
Attempt 2, in-place rewrite, orchestrator repair, test repair, launcher repair,
or retry. Cleanup and truthful failure closure remain mandatory.

## 8. Exact Cycle 4 prevalidation receipt tree

The attempt tree is exactly:

```text
attempt-1/
  clean-before-copy.json
  target-spec-copy-receipt.json
  launcher/
    exact-launcher-command.txt
    working-directory.json
    launcher-identity.json
    corepack-identity.json
    package-manager.json
    frozen-candidate-identities.json
    focused-prestate.json
    focused-poststate.json
    vitest-resolution.json
    pnpm-version-command.txt
    pnpm-version-stdout.txt
    pnpm-version-stderr.txt
    pnpm-version-exit-code.txt
    pnpm-version-process.json
    vitest-version-command.txt
    vitest-version-stdout.txt
    vitest-version-stderr.txt
    vitest-version-exit-code.txt
    vitest-version-process.json
    path-scope.json
    bare-command-audit.json
    prevalidation-summary.json
    cleanup-receipt.json
    manifest.json
  target-spec-removal-receipt.json
  cleanup-receipt.json
  attempt-summary.json
  manifest.json
```

Additional diagnostic files are forbidden. Every JSON receipt must carry a
Cycle 4 schema version, mode, attempt identity, status, and truthful failure
fields where applicable. The orchestrator-owned receipts must also preserve
each native executable absolute path, exact argument vector, argument count,
display command, working directory, start/end state, separate stdout/stderr,
exit code, and timeout state.

The launcher subtree remains byte-for-byte launcher v2 output on any path that
invokes launcher v2. The orchestrator must not edit or normalize launcher
receipts. If failure occurs before launcher invocation, the orchestrator may
create only `launcher/manifest.json`, truthfully closing the previously empty
nested list as `file_count=0 / files=[] / launcher_invoked=false`; it must not
synthesize the other launcher receipts. A failure after launcher invocation
must preserve whatever launcher v2 actually wrote; a missing launcher manifest
is a terminal manifest failure, not permission for the orchestrator to forge
one.

The nested launcher manifest excludes only `launcher/manifest.json`. On the
successful path it must verify every other launcher file by relative path,
bytes, and SHA-256. The top manifest excludes only `attempt-1/manifest.json`,
recursively includes every other actual Attempt 1 file including the nested
manifest, and contains no Cycle 3 member. Count comparison occurs before
element comparison; both sides are always defined arrays.

## 9. Fresh independent review gate

After and only after Cycle 4 prevalidation `PASS`, a fresh Reviewer must read:

- this plan, the original Cycle 3 plan and addendum;
- immutable Cycle 3 Attempt 1 and terminal Attempt 2 receipts/manifests;
- the new test, RED and GREEN receipts/manifests;
- the new orchestrator source and complete Cycle 4 Attempt 1 tree;
- unchanged launcher v2 source/hash, harness hash, and collector hash;
- current Article/course state, fixture cleanup proof, and Article 37+ guards.

The review must independently check both **spec compliance** and **quality**:

1. no historical-source reconstruction claim and no Cycle 3 rewrite;
2. exact write allowlist and one-attempt budgets;
3. RED witnesses truthful and isolated; GREEN exercises the candidate rather
   than a duplicate implementation;
4. explicit `ArgumentVector` binding and element-preserving receipts at every
   native callsite;
5. defined zero-member arrays, count-aware manifest comparison, nested/top
   closure, and no nullable Join predicate;
6. exact copy, launcher-v2 identity and one invocation, two absolute-Corepack
   probes, zero focused/target/collector calls;
7. literal-path cleanup, final pinned/clean fixture, complete receipts, and
   independently recomputed manifests;
8. readable fail-closed control flow with no hidden fallback, retry, broad
   deletion, network, install/update, or future-Article side effect;
9. helper, launcher-prevalidation, and actual-experiment evidence are clearly
   separated and no Article 36 runtime Claim is upgraded.

Any missing or weak item is `FAIL / HUMAN_DECISION_REQUIRED`; there is no
automatic revision loop in Cycle 4. Only a fresh Reviewer
`REVIEW_RECHECK PASS`, followed by Master artifact/envelope validation, may map
to canonical `LAB_EXECUTE`.

## 10. Actual focused Recovery Cycle 3 experiment remains unchanged

Cycle 4 allocates no new DSH experiment and does not rename the future raw
root. The still-unused actual experiment remains the single focused Recovery
Cycle 3 attempt defined by the frozen Cycle 3 plan/addendum:

| Counter / identity | Frozen value |
|---|---|
| Actual focused attempts | `1 total / 1 unused` |
| Focused commands | `1` |
| Wall-clock cap | `180 seconds` |
| Harness / focused case | `1 / 1`, sequential `P0--P5` |
| Runtime raw root | `experiments/raw/36-x01/recovery-cycle-3/` |
| Launcher | unchanged launcher v2, `EXECUTE_FOCUSED` |
| Harness / collector | exact section 2 hashes |
| Retry after failure | `0` |

The hypothesis remains: the same durable Session log should expose per-attempt
usage and session accumulation, source-defined compaction trigger/bracket/
replacement, before/after request change, cooperative cancellation ordering,
and fresh-resume reconstruction, while external Tool state remains `UNKNOWN`
without an explicit receipt/reconcile owner.

The phases remain:

```text
P0 Baseline
-> P1 Long Session / Usage
-> P2 Source-defined Compaction
-> P3 Before / After Diff
-> P4 Cancellation / Durable terminal
-> P5 Dispose + Fresh Resume
```

The only focused command semantics remain:

```text
pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

The ten acceptance assertions remain unchanged:

1. selection=`1` and the integrated case completes P0--P5;
2. conversation attempts are at least two, native usage has raw events, and
   same-attempt usage is not double counted;
3. pressure crosses the real threshold and the automatic source listener
   produces the complete compaction bracket;
4. old raw events remain while current surface/history/next request show the
   explained replacement change;
5. cancel request, signal observation, body settlement, Tool terminal, and
   turn terminal remain distinct;
6. after durable-prefix flush and first-runtime disposal, a fresh Context
   resumes the same Session with an unchanged prefix;
7. `known_work=KNOWN`, the cancelled sentinel remains `UNKNOWN`, and no
   automatic rollback, reconcile, or retry is claimed;
8. Context/Model/Tool/Policy/Error/Usage correlate through session, seq,
   turn/step, and available call/compaction identities, while model-attempt-ID
   absence remains explicit;
9. real Provider, network, server, credential, and production-path use are all
   zero; and
10. cleanup returns the fixture to the exact pinned and clean state.

P0--P5, the baseline commit/tag, harness and collector hashes, raw staging and
transfer order, limitations, safety boundary, and claim ceiling are unchanged.
Helper GREEN proves only orchestration helpers. Cycle 4 prevalidation proves
only launcher-only reachability. Neither is P0, selection=`1`, or runtime
evidence. Article 36 behavior remains `BLOCKED_EVIDENCE` until the one actual
experiment satisfies all ten assertions and its raw evidence passes Evidence
Merge. A mocked passing trace remains fixture-bounded and does not prove
provider billing, production backend behavior, summary fidelity, hard
cancellation, rollback, exactly-once effects, or fresh-process recovery.

## 11. Article 37+ and worker-result boundaries

Article 37 must remain `NOT_STARTED / ZERO ASSETS`. Articles 38--44 remain
`FORBIDDEN / ZERO ASSETS`. Cycle 4 creates no Article 37+ workspace, content,
asset, Lab, BuildPilot implementation, publication, commit, or push.

Closed result mappings are:

| Event | Role / Gate | Status | Gate completed | Next allowed gate | Blocker |
|---|---|---|:---:|---|---|
| This design is complete | `RESEARCHER / EXPERIMENT_DESIGN` | `PASS` | `true` | `REVISION` | `NONE` |
| RED + orchestrator + GREEN + Cycle 4 Attempt 1 all satisfy this plan | `REVISION_WORKER / REVISION` | `PASS` | `true` | `REVIEW_RECHECK` | `NONE` |
| Any Revision or prevalidation contract fails | `REVISION_WORKER / REVISION` | `FAIL` | `false` | `NONE` | `HUMAN_DECISION_REQUIRED` |
| Fresh review passes | `REVIEWER / REVIEW_RECHECK` | `PASS` | `true` | `LAB_EXECUTE` | `NONE` |
| Fresh review rejects any item | `REVIEWER / REVIEW_RECHECK` | `FAIL` | `true` | `NONE` | `HUMAN_DECISION_REQUIRED` |
| The one actual experiment satisfies its execution contract | `LAB_ENGINEER / LAB_EXECUTE` | `PASS` | `true` | `LAB_OBSERVATION` | `NONE` |
| The one actual experiment fails | `LAB_ENGINEER / LAB_EXECUTE` | `FAIL` | `false` | `LAB_EXECUTE` | `FAILED_LAB` |

For the last row, `LAB_EXECUTE` is only a recovery candidate. With
`continuous_run.stop_on.required_lab_failure=true`, `STOP POLICY WINS`; no
retry is authorized without another external Human Resume.

## 12. Design disposition

This plan freezes one new durable orchestrator, one durable isolated test, one
RED receipt set, one GREEN receipt set, and one independent Cycle 4
launcher-only prevalidation attempt. It preserves all historical failure
evidence and changes neither launcher v2 nor the real experiment.

The Design Gate result is `PASS -> REVISION`. No orchestrator, test, receipt,
target spec, runtime raw, DSH command, state update, Article 37+ artifact, or
runtime confirmation was created by this Researcher design.
