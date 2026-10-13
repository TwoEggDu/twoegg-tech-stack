# 36-X01 Recovery Cycle 5 focused-state diagnostic implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` to implement this plan with a fresh Revision Worker and a fresh read-only Reviewer. The Revision Worker must stop after the one diagnostic prevalidation; a later launcher-only Attempt requires a new Human Resume.

**Goal:** Preserve enough bounded, sanitized failure information to determine whether Cycle 4's exit `90` can be localized inside the focused-state boundary, without modifying or invoking the frozen launcher and without running Corepack, pnpm, Vitest, DSH, or the Article 36 experiment.

**Architecture:** Add one independent diagnostic candidate that models only the launcher window from the completed package-manager receipt through focused-prestate receipt creation. RED statically proves that the frozen Cycle 3 launcher lacks the required durable diagnostic surface; GREEN exercises the new candidate only with deterministic isolated adapters. One subsequent one-shot diagnostic prevalidation consumes immutable Cycle 4 receipts and the isolated model, writes a sanitized exception/phase receipt, and then stops for a fresh Reviewer.

**Tech Stack:** PowerShell 7, PowerShell AST inspection, JSON receipts, SHA-256 self-excluding manifests. No package installation, network access, DSH runtime execution, or Git history mutation.

**Spec:** This file is the frozen Recovery Cycle 5 specification. It is constrained by `docs/agent-engineering-course/subagent-contracts.md`, `docs/agent-engineering-course/course-factory.md`, `docs/agent-engineering-course/production-workflow.md`, the immutable Cycle 1-4 evidence, and the current Article 36 hard lock.

## Global constraints

- DSH repository/tag/full commit remain `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, and `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Frozen Cycle 3 launcher remains `36-x01-recovery-cycle-3-launcher.ps1`, SHA-256 `2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E`; it is read-only and must not be invoked in Cycle 5.
- Frozen harness remains SHA-256 `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B`; frozen collector remains SHA-256 `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045`.
- Article 36 hypothesis, falsifier, acceptance criteria, safety boundary, and experiment command remain unchanged. Cycle 5 produces diagnostic evidence only, never DSH runtime evidence.
- Cycle 1-4 plans, scripts, receipts, manifests, state projections, and failure records are immutable. Missing data remains `MISSING`; an unproven cause remains `NOT_PROVABLE`.
- No Cycle 5 process may invoke the original launcher, Corepack, pnpm, Vitest, the focused target, the collector, a DSH command, or a network command. No process may create the target spec in the pinned DSH fixture.
- No Cycle 5 result may authorize a launcher-only Attempt or `LAB_EXECUTE`. A fresh Reviewer can close the diagnostic gate, but only a new external Human Resume can authorize a new Attempt.
- All receipts use UTF-8 without BOM, closed schemas, explicit empty arrays, exact byte counts and uppercase SHA-256 values. Every manifest is self-excluding and enumerates every file below its root except itself.
- The current Researcher creates only this plan and executes none of the later commands.

---

## 1. Truthful disposition of Cycle 4

Cycle 4 Attempt 1 remains a terminal failed prevalidation. The surviving
receipts prove all of the following and nothing stronger:

- the Cycle 4 orchestrator started exactly once and invoked the frozen launcher exactly once;
- launcher identity, Corepack identity, and package-manager identity receipts completed;
- the launcher returned exit `90` before a completed `focused-prestate.json` existed;
- the launcher child-process, probe, focused-command, target-execution, collector, and bare-Corepack counts were all zero;
- stdout and stderr captured by the orchestrator were empty;
- no nested launcher manifest was produced;
- target-spec removal and fixture cleanup completed, and the frozen fixture ended pinned and clean.

These facts do not prove that `Get-ExactFocusedFixtureState` was entered. The
unreceipted window also contains the first full fixture-state capture, frozen
candidate identity capture, target/raw path construction, the focused-state
call, and the attempted focused-prestate receipt write. The exact exception,
statement, and internal failure code are therefore `NOT_PROVABLE`.

Finding `A36-C4-RR-F02` also remains open as a historical fact: Cycle 4 RED and
GREEN receipts did not persist the test script SHA, so their test immutability
cannot be reconstructed retrospectively. Cycle 5 must not edit those receipts.
It prevents recurrence only by durably storing both the test SHA and candidate
SHA in each new RED and GREEN phase receipt.

No Cycle 5 artifact may relabel Cycle 4 as PASS, claim that an isolated model
is the original launcher, or write a retrospective exception into Cycle 4.

## 2. Single diagnostic question and falsifiable hypothesis

### Diagnostic question

Within the boundary that begins immediately after the completed Cycle 4
`package-manager.json` receipt and ends when a completed
`focused-prestate.json` would have been durably written, can the observed
exit-`90` class be localized to one precise phase and exception using an
independent, isolated diagnostic candidate whose inputs are derived from the
immutable Cycle 4 receipt shapes?

### Hypothesis `A36-C5-H1`

The failure class is reproducible inside that bounded window. When the new
diagnostic candidate receives Cycle 4-shaped identities and deterministic
isolated adapter results, its one diagnostic invocation will close with either:

1. exactly one `FAIL` receipt containing a nonempty precise phase, failure
   code, exception type, sanitized message, sanitized PowerShell script stack,
   and invocation record; or
2. exactly one `PASS / NO_EXCEPTION_REPRODUCED` receipt proving that the
   isolated model crossed the entire boundary and therefore did not reproduce
   Cycle 4's internal cause.

`A36-C5-H1` is falsified for root-cause attribution if the isolated invocation
crosses the boundary without the Cycle 4 failure, produces multiple candidate
phases, cannot preserve a complete sanitized receipt, or depends on an actual
launcher/Corepack/pnpm/Vitest/DSH invocation. In every falsifying case the
historical root cause remains `NOT_PROVABLE`; the result must not be promoted
to a launcher fix or retry authorization.

Even a single isolated exception match supports only
`ISOLATED_REPRODUCTION_CONSISTENT_WITH_CYCLE4_WINDOW`. It does not prove that
the same exception occurred during Cycle 4 because Cycle 4 did not preserve
the original exception.

## 3. Frozen file structure and exact Revision allowlist

The later Revision Worker may create exactly these five paths and may modify
no existing file:

1. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-5-diagnostic.Tests.ps1`
2. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-5-diagnostic-tests/red/`
3. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-5-diagnostic.ps1`
4. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-5-diagnostic-tests/green/`
5. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-5-diagnostic/attempt-1/`

Responsibilities are frozen as follows:

- `36-x01-recovery-cycle-5-diagnostic.Tests.ps1` is the sole immutable test
  driver used in both RED and GREEN. It performs AST/source checks and invokes
  only the independent diagnostic candidate with in-memory or temporary
  isolated adapters. It must reject any candidate that references an actual
  Corepack/pnpm/Vitest/collector execution path.
- `...diagnostic-tests/red/` contains the one RED invocation's exact command,
  phase summary, stdout/stderr, identity receipt, cleanup receipt, and manifest.
  Its candidate identity is the read-only Cycle 3 launcher source; that source
  is inspected, never executed.
- `36-x01-recovery-cycle-5-diagnostic.ps1` is a new independent diagnostic
  candidate. It is not launcher v3, not a repaired launcher, and cannot expose
  `PREVALIDATE`, `VALIDATE_FOCUSED_PRESTATE`, or `EXECUTE_FOCUSED` modes. Its
  only mode is `DIAGNOSE_FOCUSED_BOUNDARY_ISOLATED`.
- `...diagnostic-tests/green/` contains the one GREEN invocation's exact
  command, phase summary, stdout/stderr, identities, cleanup receipt, and
  manifest.
- `...diagnostic/attempt-1/` contains the sole Cycle 5 diagnostic
  prevalidation. It consumes the immutable Cycle 4 receipt tree read-only and
  invokes only the new diagnostic candidate against deterministic isolated
  adapters. It contains a top summary, the required sanitized failure receipt,
  source/input identities, exact tokenized invocation, cleanup receipt, and a
  self-excluding manifest.

Temporary scratch content is allowed only below a newly created OS temporary
directory whose leaf begins `a36-cycle5-isolated-`. It must not be below the
course repository or pinned DSH fixture, and it must be removed in the same
invocation. Its path may be retained only as the token `<ISOLATED_ROOT>` plus a
SHA-256 identity; the raw absolute temporary path must not be written to a
repository receipt.

The Revision Worker is forbidden to modify the Cycle 3 launcher, Cycle 4
orchestrator/tests/receipts, harness, collector, Article 36 Research/Evidence,
course state/status/README/trace, any published content, or Article 37+ paths.
Deletion and rename are forbidden.

## 4. Diagnostic candidate contract

The candidate must model the existing boundary in the original order and emit
an in-memory phase marker before each operation:

```text
BOUNDARY_ENTER
FIXTURE_STATE_BEFORE_BEGIN
FIXTURE_STATE_BEFORE_END
FROZEN_CANDIDATE_IDENTITY_BEGIN
FROZEN_CANDIDATE_IDENTITY_END
TARGET_AND_RAW_PATHS_BEGIN
TARGET_AND_RAW_PATHS_END
FOCUSED_STATE_CALL_BEGIN
  FOCUSED_GET_FIXTURE_STATE_BEGIN
  FOCUSED_GET_FIXTURE_STATE_END
  FOCUSED_PATH_NORMALIZATION_BEGIN
  FOCUSED_PATH_NORMALIZATION_END
  FOCUSED_READ_ONLY_GIT_SHAPES_BEGIN
  FOCUSED_READ_ONLY_GIT_SHAPES_END
  FOCUSED_CANDIDATE_IDENTITIES_BEGIN
  FOCUSED_CANDIDATE_IDENTITIES_END
  FOCUSED_BYTE_COMPARISON_BEGIN
  FOCUSED_BYTE_COMPARISON_END
  FOCUSED_PREDICATES_BEGIN
  FOCUSED_PREDICATES_END
  FOCUSED_RETURN
FOCUSED_STATE_CALL_END
FOCUSED_PRESTATE_RECEIPT_WRITE_BEGIN
FOCUSED_PRESTATE_RECEIPT_WRITE_END
BOUNDARY_EXIT
```

The adapter contract supplies deterministic objects rather than executing Git
or reading the DSH fixture. The fixture-state, Corepack, package-manager,
harness, collector, target, `ls-files`, and `check-ignore` objects must retain
the key names, nullability, scalar/array shapes, byte counts, exit codes, and
hashes needed by the boundary logic. Cycle 4 absolute paths are tokenized to
`<WORKSPACE>`, `<PINNED_FIXTURE>`, `<USER_PROFILE>`, `<TEMP>`, and
`<RECEIPT_ROOT>` before the candidate consumes them.

The candidate must support deterministic failure injection at every phase for
GREEN testing. Injection is test-only configuration and must be present in the
receipt. The one diagnostic prevalidation may not arbitrarily inject a chosen
exception to manufacture a match; it may only execute the adapter data and
candidate logic derived from Cycle 4 receipt shapes.

The candidate's terminal result is always written in `finally`, followed by a
self-excluding manifest. Receipt finalization failure must produce a separate
minimal emergency receipt with failure code
`CYCLE5_DIAGNOSTIC_RECEIPT_FINALIZATION_FAILURE`; it must not replace the
original exception fields with a success value.

## 5. Durable failure receipt and minimization contract

The canonical `diagnostic-result.json` must contain at least:

- schema version, attempt identity, status, and claim ceiling;
- exact last-completed phase and precise failing phase;
- stable failure code;
- exception .NET type and PowerShell fully qualified error ID;
- sanitized exception message;
- sanitized `ScriptStackTrace` preserving function names and line/character
  coordinates while tokenizing absolute roots;
- invocation name, candidate SHA, test SHA where applicable, executable file
  identity, ordered tokenized argument vector and count, tokenized working
  directory, start/end UTC, duration, timeout, and exit code;
- immutable Cycle 4 input receipt identities and their SHA-256 values;
- original-launcher invocation count, Corepack count, pnpm count, Vitest count,
  focused-target count, collector count, DSH command count, and network count,
  all required to equal zero;
- cleanup result, temporary-root absence, and repository/DSH-fixture write
  counts;
- hypothesis result and one of the exact dispositions
  `ISOLATED_EXCEPTION_REPRODUCED`, `NO_EXCEPTION_REPRODUCED`,
  `AMBIGUOUS_DIAGNOSTIC_FAILURE`, or `RECEIPT_FINALIZATION_FAILURE`;
- `historical_cycle4_root_cause: NOT_PROVABLE` and
  `new_launcher_attempt_authorized: false` unconditionally.

No receipt may persist environment-variable values, credential-store output,
home-directory listings, authentication headers, tokens, cookies, private
keys, or arbitrary file contents. The sanitizer must run before serialization
and replace all known absolute roots with the tokens above. It must also scan
message, stack, command, and arguments for case-insensitive credential markers
(`authorization`, `bearer`, `token`, `password`, `secret`, `api_key`,
`private_key`) and URI user-info. A nonzero post-sanitization sensitive-pattern
count is a terminal `FAIL`; the unsafe text must stay only in memory and must
not be written. Receipts store replacement counts and the post-sanitization
scan result, not the removed values.

The exact unredacted exception text is deliberately not durable. The receipt
must preserve enough sanitized structure to locate the phase and script line
without exporting unrelated machine state.

## 6. RED/GREEN proof and identity closure

The Revision Worker must create the test script first and compute its SHA-256
before either phase. The same exact test bytes must be used in RED and GREEN.

### RED, exactly once

Run the test driver once in `RED_STATIC_FROZEN_LAUNCHER` mode against the
read-only Cycle 3 launcher source. The test must not invoke that launcher. RED
passes only as an expected failure proving that the frozen source lacks the
Cycle 5 diagnostic contract: no durable precise-phase receipt, no sanitized
script-stack receipt, and no exception-preserving finalization contract.

The RED identity receipt must persist:

- test path, byte count, and SHA-256;
- candidate path, byte count, and Cycle 3 launcher SHA-256;
- `candidate_executed: false`;
- exact expected failed assertions and actual failed assertions;
- original-launcher/Corepack/pnpm/Vitest/DSH/network counts, all zero.

### GREEN, exactly once

After RED closes, create the independent diagnostic candidate and run the same
test script once in `GREEN_ISOLATED_DIAGNOSTIC` mode. GREEN passes only if:

- the test SHA exactly equals the RED test SHA;
- the candidate SHA is durably stored and differs from the frozen launcher SHA;
- every phase supports deterministic injection and produces exactly one
  sanitized failure receipt with the expected phase/code/type/message/stack;
- the no-injection path reaches `BOUNDARY_EXIT` and emits
  `NO_EXCEPTION_REPRODUCED`;
- empty arrays remain explicit arrays, all invocation counts remain zero, the
  temporary isolated root is removed, and manifest verification passes;
- source/AST checks prove the candidate has no callable original-launcher,
  Corepack, pnpm, Vitest, focused target, collector, DSH, network, package
  installation, or process-start path.

The GREEN identity receipt must persist the same test identity fields and the
new candidate path/byte count/SHA-256. Reviewer comparison of RED and GREEN
must be able to establish test immutability from receipts alone; file mtime is
not evidence.

Any RED or GREEN unexpected outcome is terminal for Cycle 5. No rewrite and
rerun is authorized under this plan.

## 7. One-shot diagnostic prevalidation

Only after one expected RED and one passing GREEN may the Revision Worker run
one diagnostic command. It must:

1. verify the current test and candidate hashes exactly equal the GREEN
   identities;
2. verify every consumed Cycle 4 receipt hash and preserve the exact input
   inventory;
3. create one isolated temporary root outside the repository and pinned DSH
   fixture;
4. invoke only `36-x01-recovery-cycle-5-diagnostic.ps1` in
   `DIAGNOSE_FOCUSED_BOUNDARY_ISOLATED` mode;
5. write the closed diagnostic result, exact tokenized invocation, stdout,
   stderr, cleanup receipt, input/source identities, and manifest below the
   frozen `attempt-1` path;
6. delete the isolated temporary root in `finally` and verify its absence;
7. stop. It must not dispatch the frozen launcher or Article 36 experiment.

Exit `0` means only that the diagnostic transaction closed durably. The
diagnostic disposition inside `diagnostic-result.json` determines whether an
isolated exception was reproduced. A nonzero exit, incomplete receipt, failed
cleanup, hash drift, unsafe text, or manifest mismatch is a Cycle 5 diagnostic
failure and requires `HUMAN_DECISION_REQUIRED`.

## 8. Frozen budgets

| Operation | Maximum count | Timeout | Prohibited escalation |
|---|---:|---:|---|
| Test script creation | 1 | n/a | no edit after RED identity capture |
| RED test invocation | 1 | 60 seconds | no RED retry |
| Diagnostic candidate creation | 1 | n/a | no edit after GREEN begins |
| GREEN test invocation | 1 | 60 seconds | no GREEN retry |
| Diagnostic prevalidation invocation | 1 | 60 seconds | no attempt-2 |
| Original Cycle 3 launcher invocation | 0 | n/a | always forbidden |
| Corepack / pnpm / Vitest / focused target / collector / DSH / network invocation | 0 each | n/a | always forbidden |
| Pinned DSH fixture writes | 0 | n/a | target spec must remain absent |
| Article 36 experiment attempts | 0 | n/a | requires later Human Resume |

The whole Revision has one RED, one GREEN, and one diagnostic prevalidation.
An interrupted invocation consumes its budget if the process started or its
receipt root was created. Existing receipt roots make a rerun fail closed.

## 9. Fresh Reviewer gate

After the one diagnostic prevalidation, discard the Revision Worker context and
dispatch a fresh read-only Reviewer at `REVIEW_RECHECK`. Reviewer writes are
zero. The Reviewer must independently verify:

1. actual diff equals the five-path allowlist and contains no modifications,
   deletions, renames, state changes, or Article 37+ assets;
2. Cycle 1-4 evidence and the frozen launcher/harness/collector hashes remain
   unchanged;
3. RED and GREEN each ran exactly once, both receipts persist test and
   candidate SHAs, and the RED/Green test SHAs are exactly equal;
4. RED inspected but did not execute the frozen launcher;
5. GREEN and the diagnostic prevalidation invoked no original launcher,
   Corepack, pnpm, Vitest, focused target, collector, DSH, or network command;
6. the diagnostic candidate cannot enter real launcher or experiment modes;
7. the diagnostic receipt preserves precise phase, failure code, exception
   type, sanitized message, sanitized script stack, invocation information,
   input identities, zero execution counters, and cleanup status;
8. sanitization replacement/scan evidence is complete and no secret or raw
   machine path leaked into repository receipts;
9. every manifest recomputes exactly, including explicit zero-member arrays;
10. the pinned fixture is still at the frozen commit/tag, clean, and lacks the
    target spec, runtime source root, and Cycle 3 raw output;
11. the result retains `historical_cycle4_root_cause: NOT_PROVABLE` and does
    not authorize a new launcher Attempt or `LAB_EXECUTE`.

Reviewer verdicts are closed:

- `PASS / DIAGNOSTIC_CLOSED`: Cycle 5 artifacts are internally valid. The
  Reviewer reports whether an isolated exception was reproduced, but returns
  `next_allowed_gate: NONE / blocker: HUMAN_DECISION_REQUIRED` because a new
  launcher-only Attempt still needs an external Human Resume.
- `FAIL / DIAGNOSTIC_NOT_CLOSED`: any identity, receipt, scope, sanitization,
  cleanup, manifest, or zero-invocation failure. It also returns
  `next_allowed_gate: NONE / blocker: HUMAN_DECISION_REQUIRED`.

There is no automatic Revision/Recheck loop in this bounded cycle. A diagnosis
is evidence for a later decision, not authority to repair or retry the launcher.

## 10. Stop and claim ceiling

Cycle 5 stops immediately after the fresh Reviewer. It does not update
`course-run-state.md`, `status.md`, course/workspace README, or
`subagent-trace.md`; those projections remain Master-owned and outside the
Revision allowlist.

The maximum honest claim is one of:

- `ISOLATED_REPRODUCTION_CONSISTENT_WITH_CYCLE4_WINDOW / HISTORICAL_CAUSE_NOT_PROVABLE`;
- `NO_EXCEPTION_REPRODUCED / HISTORICAL_CAUSE_NOT_PROVABLE`;
- `AMBIGUOUS_DIAGNOSTIC_FAILURE / HISTORICAL_CAUSE_NOT_PROVABLE`.

None means launcher repaired, prevalidation passed, DSH behavior observed,
Article 36 evidence unblocked, or a new Attempt authorized.

## 11. Design Gate result

This plan freezes a bounded diagnostic route without assuming the root cause or
changing the Article 36 experiment. The Design Gate result is `PASS -> REVISION`.
The later Revision remains subject to the exact allowlist and one-shot budgets
above, and must stop for the fresh Reviewer before any new launcher-only Attempt.

