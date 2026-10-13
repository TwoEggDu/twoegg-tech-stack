# 36-X01 Recovery Cycle 3 plan-defect addendum

Status: `FROZEN ADDENDUM / A36-C3-RR-F01 ONLY / NOT IMPLEMENTED / NOT EXECUTED`

## 1. Authority, precedence, and immutable records

This addendum supplements, but does not rewrite,
`36-x01-recovery-cycle-3-plan.md`. The original plan remains the historical
design record, and the complete launcher-only `attempt-1/` receipt set remains
immutable.

The fresh Reviewer finding `A36-C3-RR-F01` established one unsatisfied plan /
launcher contract: the existing launcher requires a strictly clean fixture
before it enters the focused branch, while that branch also requires the one
temporary target spec to exist. The target spec is untracked and non-ignored,
so its required presence makes the earlier `FixtureStateBefore.clean` check
false. The focused branch is therefore structurally unreachable; Attempt 1 did
not validate that branch.

The Master ruled that the human Recovery Cycle 3 authority permits bounded
Corepack/PATH launcher repair and launcher prevalidation, while the one-attempt
cap applies to the actual focused experiment. Therefore this addendum
supersedes only these Cycle 3 plan statements as they apply to launcher
prevalidation:

- section 1's count of one launcher-only prevalidation;
- section 4's `there is no Attempt 2` sentence; and
- any equivalent statement that would forbid the one `A36-C3-RR-F01`
  launcher-only revalidation defined below.

All other original plan rules remain in force. In particular, there is still
only one actual focused experiment attempt, with a 180-second cap and no retry.
Attempt 1, Cycle 1, Cycle 2, the hypothesis, falsifiers, P0--P5, the ten
acceptance assertions, baseline, safety boundary, harness semantics, collector
semantics, raw-evidence boundary, and claim ceiling are unchanged.

The immutable identities carried into this addendum are:

| Artifact | Frozen identity |
|---|---|
| Original Cycle 3 plan | `F5A9770EDE088BCA3B17AF3C06E5EDED46DA859BAFE349B887AAA4B077E47336` |
| Attempt 1 launcher | `524D95F34F65435D48696B0A688276A106D0A8610D78FFD43E06A8BF375CA8F2` |
| Attempt 1 self-excluding manifest | `21 / 21` entries exact; manifest SHA-256 `4F81810A40552C7623BA2552CB7400BABF44C554BF5ED5FEB63FEE1B318A8DAB` |
| Frozen harness candidate, `36-x01-recovery-cycle-2-instrumentation.ts` | `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B` |
| Frozen collector, `36-x01-recovery-cycle-2-collector.ps1` | `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045` |

Attempt 1 remains truthful launcher-only evidence: absolute Corepack resolved,
pnpm `11.7.0` and Vitest `4.1.8` probes exited `0`, and focused / target-test
execution counts were `0`. It is not DSH runtime evidence and is not evidence
that focused prestate is reachable.

## 2. Minimal launcher-v2 revision boundary

The Revision Worker may modify only
`experiments/36-x01-recovery-cycle-3-launcher.ps1` and create only the new
`experiments/preflight/36-x01-recovery-cycle-3-launcher/attempt-2/` receipt
tree. Attempt 1 must not be changed, moved, regenerated, or referenced as a
writable receipt root.

Launcher v2 must retain the existing absolute-Corepack-before-PATH behavior,
exact fixture/package/candidate identities, process-scoped PATH mutation,
separate stdout/stderr capture, closed failures, and forbidden command set. Its
only semantic repair is to distinguish strict clean prevalidation from the one
expected untracked-spec prestate.

The only permitted modes after the repair are:

| Mode | Fixture prestate | Processes it may start |
|---|---|---|
| `PREVALIDATE` | Strictly clean; target spec, runtime source root, and Cycle 3 raw root absent. Existing semantics remain unchanged. | Exactly the absolute-Corepack pnpm and Vitest version probes; zero focused commands. |
| `VALIDATE_FOCUSED_PRESTATE` | Exact pinned HEAD/tag, no tracked or staged diff, and exactly one status entry: the expected untracked target spec defined below. | Exactly the same two absolute-Corepack version probes; zero focused commands and zero target-test executions. |
| `EXECUTE_FOCUSED` | The same exact one-untracked-spec prestate as `VALIDATE_FOCUSED_PRESTATE`. | Exactly one frozen focused command with the existing 180-second cap; zero version probes. |

`PREVALIDATE` must not be weakened to accept the target spec. The new
`VALIDATE_FOCUSED_PRESTATE` mode must contain no target command arguments,
`run` callsite, collector call, runtime-source creation, or raw-root creation.
It exists only to prove that the shared focused prestate checks are reachable
with the real copied spec.

Launcher v2 must use one shared focused-prestate validator for
`VALIDATE_FOCUSED_PRESTATE` and `EXECUTE_FOCUSED`. A separate, looser copy of
the check is forbidden. Source audit and runtime receipts must prove that both
modes pass through the same validator before their mode-specific branch.

## 3. Exact focused-prestate predicate

The shared focused-prestate validator must accept if and only if all of the
following are true at one captured cut:

1. Fixture root equals
   `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`.
2. `HEAD` equals full commit
   `cd5ef8148158c3a752a658978873241fdf8e2bbc` and the exact tag at `HEAD` is
   `dsh-v0.1.2-alpha.1`.
3. Unstaged tracked diff is empty and staged diff is empty. Both exact commands
   and exit codes must be captured; an untracked-file allowance must not be
   implemented by dropping tracked-diff checks.
4. Full porcelain status, with all untracked files included and path separators
   normalized to `/`, contains exactly this one line and no other status:

   ```text
   ?? packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts
   ```

5. The target resolves to exactly that fixture-relative path, exists as one
   file, is not tracked, and is not ignored. The exact `ls-files` /
   `check-ignore` commands and their expected non-match exit codes must be
   captured rather than inferred from prose.
6. The source candidate is exactly
   `experiments/36-x01-recovery-cycle-2-instrumentation.ts`; its SHA-256 equals
   `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B`.
7. The target bytes and byte count equal that source candidate byte-for-byte,
   and its SHA-256 equals the same frozen hash.
8. The frozen collector remains at SHA-256
   `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045`.
9. The test-owned runtime source root and Cycle 3 raw destination are absent.
10. The resolved Corepack path/hash, fixture `packageManager`, and fixture-local
    Vitest shim still match the original Cycle 3 frozen identities.

Any additional untracked, modified, staged, deleted, renamed, ignored-target,
or path-mismatched entry is a closed failure. A different candidate hash,
destination hash, HEAD, tag, Corepack identity, package manager, Vitest
resolution, or collector identity is also a closed failure. The launcher may
not repair, reset, stash, stage, ignore, install, update, or search for an
alternate identity.

The identity/status command semantics are frozen as:

```text
git -C <fixture> rev-parse HEAD                                      -> exit 0, exact pinned SHA
git -C <fixture> describe --tags --exact-match HEAD                  -> exit 0, exact pinned tag
git -C <fixture> diff --quiet --                                    -> exit 0
git -C <fixture> diff --cached --quiet --                            -> exit 0
git -C <fixture> status --porcelain=v1 --untracked-files=all         -> exit 0, exact one line
git -C <fixture> ls-files --error-unmatch -- <target-relative-path>  -> exit 1, no match
git -C <fixture> check-ignore -q -- <target-relative-path>           -> exit 1, not ignored
```

After the two probes in `VALIDATE_FOCUSED_PRESTATE`, launcher v2 must capture a
`focused-poststate.json` receipt and require the same exact HEAD/tag, one-line
status, empty tracked/staged diffs, target path, and target hash. In
`EXECUTE_FOCUSED`, the same receipt is mandatory immediately after the focused
process terminates, including on nonzero exit or timeout, and before collector
transfer or Lab cleanup. The fixture may still contain only the exact untracked
target spec; no second fixture change is accepted. Test-owned OS-temp runtime
output may exist after an actual focused run, but it is not a fixture-status
exception and must be handled only by the original collector/cleanup contract.

## 4. One Attempt 2 launcher-only revalidation

Exactly one additional launcher prevalidation attempt is authorized at:

```text
experiments/preflight/36-x01-recovery-cycle-3-launcher/attempt-2/
```

The directory must be absent before the attempt. Attempt 2 is one orchestration
with exactly one launcher invocation in `VALIDATE_FOCUSED_PRESTATE` mode. It is
not an actual experiment attempt.

The order is frozen:

### Stage A — clean cut and exact copy

1. Before copying the spec, capture a clean-before-copy receipt proving pinned
   HEAD/tag, empty full status, empty unstaged and staged tracked diffs, absent
   target spec, absent runtime source root, absent Cycle 3 raw root, and exact
   harness/collector identities.
2. Copy the frozen harness candidate byte-for-byte to the exact target path.
   Capture source/destination absolute and relative paths, bytes, hashes,
   non-tracked/non-ignored results, and the exact one-line porcelain status.

### Stage B — launcher-only reachability and restoration

3. Invoke launcher v2 exactly once in `VALIDATE_FOCUSED_PRESTATE`, using a
   previously absent nested launcher receipt root. The launcher must resolve
   and hash absolute Corepack before PATH mutation, prepend only fixture
   `.bin`, run the exact pnpm `11.7.0` and Vitest version probes, exercise the
   shared focused-prestate validator, and run zero tests.
4. Whether the launcher passes or fails, remove only the exact temporary spec.
   Do not remove, reset, clean, or repair any other fixture path.
5. Capture a final cleanup receipt proving the target is absent, runtime source
   and Cycle 3 raw roots are absent, HEAD/tag are pinned, full status is empty,
   both tracked diffs are empty, process PATH is restored, and no other cleanup
   was attempted.
6. Write `attempt-summary.json`, then a top-level, self-excluding Attempt 2
   manifest only after all receipts are closed. The manifest must recursively
   include every Attempt 2 file except itself with relative path, bytes, and
   SHA-256.

The launcher invocation may start exactly two child processes: absolute
Corepack with argument vector `pnpm --version`, and the same absolute Corepack
with argument vector `pnpm exec vitest --version`. Required counts are:

```text
launcher_invocation_count = 1
prevalidation_probe_count = 2
focused_command_count = 0
target_spec_execution_count = 0
collector_invocation_count = 0
```

No target test, collector, runtime source, Cycle 3 raw evidence, network,
server, provider, credential, install, update, or Article 37+ operation is
permitted.

An Attempt 2 failure is terminal for this recovery design. Cleanup and truthful
failure receipts are still mandatory, but there is no Attempt 3, in-place
receipt rewrite, launcher re-repair, or retry. The worker must hard-stop and
return the failure mapping in section 9.

## 5. Attempt 2 receipt and manifest schema

The top-level Attempt 2 tree is frozen as follows; additional diagnostic files
are forbidden unless a caught launcher failure requires its already-defined
stderr/process receipt.

```text
attempt-2/
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

Every JSON receipt uses a closed schema version containing its mode and attempt
identity. `focused-prestate.json` must retain all raw commands, exit codes,
unnormalized status bytes/text, normalized status lines, exact expected line,
HEAD/tag results, tracked/staged diff results, tracked/ignored checks, source
and destination identities, and one boolean per predicate in section 3.
`focused-poststate.json` must repeat the shared fixture predicates after the
probes and before removal; it must also state `focused_command_count=0` and
`target_spec_execution_count=0` for Attempt 2.

The launcher summary must state
`claim_ceiling=LAUNCHER_ONLY_PREVALIDATION_NOT_DSH_RUNTIME_EVIDENCE` and
`target_experiment_authorized=false`. The top-level summary/manifest must
distinguish launcher exit from orchestration cleanup. A launcher `PASS` with
failed spec removal, dirty final status, PATH leakage, missing receipt, or
manifest mismatch makes the whole Attempt 2 `FAIL`.

The nested launcher manifest excludes only its own `launcher/manifest.json`.
The top-level manifest excludes only `attempt-2/manifest.json` and includes the
nested manifest itself. No Attempt 1 file may appear as an Attempt 2 manifest
member.

## 6. Separate Lab clean-before-copy receipt

Attempt 2's clean-before-copy receipt does not replace the future Lab
Engineer's precondition receipt. After a fresh Reviewer passes Attempt 2 and
the Master authorizes `LAB_EXECUTE`, the Lab Engineer must independently
capture a separate `lab-clean-before-copy.json` in test-owned staging before
copying the target spec.

That Lab receipt must prove the same strict-clean facts as Attempt 2 step 1:
pinned HEAD/tag, empty full status, empty tracked and staged diffs, absent target
spec/runtime source/Cycle 3 raw root, and exact frozen harness/collector hashes.
It must be transferred into the Cycle 3 raw evidence before cleanup. A stale
Attempt 2 receipt, a status check taken after the copy, or a statement that the
fixture was previously clean cannot substitute for this receipt.

Only after the Lab clean-before-copy receipt passes may the Lab Engineer copy
the exact
`D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B`
candidate to the target path. `EXECUTE_FOCUSED` must
then re-run the shared one-untracked-spec prestate validator and fail before the
target command if any predicate differs.

## 7. Actual experiment remains one attempt

Launcher prevalidation Attempt 1 and Attempt 2 consume only the launcher
prevalidation budget. Neither increments the actual experiment attempt count.

The actual Recovery Cycle 3 Lab budget remains:

| Counter | Frozen value |
|---|---:|
| Launcher prevalidation attempts | `2 total` (`attempt-1` preserved, one `attempt-2` authorized) |
| Actual focused experiment attempts | `1 total`, still unused |
| Focused commands in that experiment | `1` |
| Focused command wall-clock cap | `180 seconds` |
| Integrated harness / focused case | `1 / 1`, sequential P0--P5 |
| Retry after actual experiment failure | `0` |

The focused command semantics remain exactly:

```text
pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

The actual executable must remain the freshly re-resolved and re-hashed
absolute Corepack path. All original raw staging, collector transfer, manifest,
cleanup, failure-preservation, acceptance, limitation, and claim-ceiling rules
continue unchanged. A launcher/prestate PASS is not P0, is not selection `1`,
and cannot upgrade any Article 36 runtime Claim.

## 8. Fresh review gate

After launcher v2 and Attempt 2 exist, a fresh independent Reviewer must verify
the original plan, this addendum, launcher v1 identity, immutable Attempt 1,
launcher v2 source, the complete Attempt 2 tree, both manifests, cleanup, and
current course/future-article guards.

The Reviewer must reject the revision if:

- `PREVALIDATE` no longer requires a strictly clean fixture;
- focused and prestate-only modes do not share one exact prestate validator;
- the allowed status is broader than the one exact untracked target;
- target path/hash/non-ignored state is not proved;
- any version probe uses a bare or non-reviewed Corepack executable;
- `VALIDATE_FOCUSED_PRESTATE` can reach the target test or collector;
- counts differ from section 4;
- Attempt 1 changed;
- cleanup or either manifest is incomplete; or
- any forbidden fixture, runtime, raw, state, evidence, Article 37+, install,
  network, server, provider, or credential action occurred.

Only a fresh Review `PASS` may recommend the canonical `LAB_EXECUTE` gate. The
Master must separately validate the result; neither this addendum nor Attempt 2
authorizes the actual experiment by itself.

## 9. Closed worker-result mappings

All worker envelopes retain the repository's exact 11-field closed schema.
The frozen mappings for this correction are:

| Event | `role` / `gate` | `status` | `gate_completed` | `next_allowed_gate` | `blocker` |
|---|---|---|:---:|---|---|
| This addendum is complete | `RESEARCHER / EXPERIMENT_DESIGN` | `PASS` | `true` | `REVISION` | `NONE` |
| Launcher v2 + Attempt 2 fully pass | `REVISION_WORKER / REVISION` | `PASS` | `true` | `REVIEW_RECHECK` | `NONE` |
| Attempt 2 launcher, receipt, cleanup, or manifest fails | `REVISION_WORKER / REVISION` | `FAIL` | `false` | `NONE` | `HUMAN_DECISION_REQUIRED` |
| Fresh Review passes | `REVIEWER / REVIEW_RECHECK` | `PASS` | `true` | `LAB_EXECUTE` | `NONE` |
| Fresh Review rejects v2 or Attempt 2 | `REVIEWER / REVIEW_RECHECK` | `FAIL` | `true` | `NONE` | `HUMAN_DECISION_REQUIRED` |
| One actual experiment passes execution contract | `LAB_ENGINEER / LAB_EXECUTE` | `PASS` | `true` | `LAB_OBSERVATION` | `NONE` |
| One actual experiment fails | `LAB_ENGINEER / LAB_EXECUTE` | `FAIL` | `false` | `LAB_EXECUTE` | `FAILED_LAB` |

For the final row, `LAB_EXECUTE` is only a recovery candidate. Because
`continuous_run.stop_on.required_lab_failure=true`, `STOP POLICY WINS`; no retry
is authorized without a new external Human Resume. `EXPERIMENT_EXECUTE` and
`RAW_OBSERVATION` wording in historical Article 36 records must not be emitted
as substitutes for the canonical envelope gates `LAB_EXECUTE` and
`LAB_OBSERVATION`.

## 10. Design disposition

`A36-C3-RR-F01` now has a bounded revision design, but it is not closed. This
addendum authorizes only the minimal launcher-v2 change and the one Attempt 2
launcher-only revalidation. Its successful Gate transition is
`EXPERIMENT_DESIGN -> REVISION`.

No launcher was modified, no Attempt 2 receipt was created, no target spec was
copied, no command was run against the DSH fixture, and no runtime, raw,
evidence, state, hypothesis, acceptance, baseline, or Article 37+ artifact was
changed by this Researcher task. The actual focused experiment remains
forbidden until Revision and fresh Review both pass Master validation.
