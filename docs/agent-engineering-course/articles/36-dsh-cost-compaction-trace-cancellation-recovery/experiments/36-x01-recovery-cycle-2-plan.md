# 36-X01 Recovery Cycle 2 experiment plan

Status: `FROZEN RECOVERY DESIGN / NOT EXECUTED / REQUIRES FRESH REVIEW`

## 1. Purpose, baseline, and immutable boundary

Cycle 2 is one independent recovery attempt for the already frozen 36-X01
integrated case. Its baseline remains DSH tag commit
`cd5ef8148158c3a752a658978873241fdf8e2bbc`. The prior Cycle 1 failure is RED
evidence, not a passing precursor: its manifest and correlation failure
receipt remain invalid/non-authoritative as described in
`raw/36-x01/recovery-cycle-1/post-review-disposition.md`.

The only permitted changes are a minimal harness guard for the empty initial
event and a collector that can transfer test-owned raw files without a literal
wildcard error. The hypothesis, original ten acceptance assertions, P0--P5,
adapter script, usage values, compaction settings, cancellation/resume flow,
assertions, timeouts, safety boundary, and Claim ceiling remain unchanged.
This design does not claim any observed runtime behavior.

No install/update, network access, server bind, credential/provider use,
second harness, owner-suite substitute, Article 37+ work, or second execution
is allowed.

## 2. Candidate harness: one minimal, auditable diff

The Revision Worker must copy the archived
`experiments/raw/36-x01/instrumentation.ts` to
`experiments/36-x01-recovery-cycle-2-instrumentation.ts` and make no change
except the `appendCorrelation` empty-event guard. The archived source before
SHA-256 is
`C4AF80CEC0B068F7C54A87777F19751FE90EF2FF28ED3DCB619ADE689276660D`.

The required replacement semantics are exactly:

```ts
const lastEvent = session?.events.at(-1)
const data = extras.event_seq === undefined && lastEvent !== undefined
  ? eventData(lastEvent)
  : {}
```

In particular, `lastEvent` is obtained first; `eventData` is called only when
it exists; otherwise `data` is `{}`. The worker must not edit adapter scripts,
usage, compaction, cancel/resume, P0--P5, assertions, timeouts, imports other
than what this exact local diff requires, or the original archived harness.

Before/after SHA-256 values and the complete unified patch must be captured.
The execution handoff must retain the candidate source at the path above and,
under Cycle 2 raw capture, record the patch and hashes so a reviewer can prove
that this is the only semantic difference.

## 3. Collector: required implementation contract

The Revision Worker must create
`experiments/36-x01-recovery-cycle-2-collector.ps1`. It transfers only a
specified test-owned source root into a specified empty destination root and
returns a closed receipt plus an explicit process exit.

Required behavior:

1. Reject missing source/destination inputs, a missing source root, an empty
   source file set, a nonempty destination root, any missing file during copy,
   or any post-transfer mismatch. A rejection must have a nonzero exit and a
   closed failure receipt.
2. Never use a wildcard with `-LiteralPath` (in particular, never
   `-LiteralPath <root>\*`). Enumerate real files recursively from the source
   root, calculate each relative path, create its destination parent, then
   copy that individual source file with `Copy-Item -LiteralPath`.
3. For every source file, compare the destination's relative path, byte count,
   and SHA-256 after copying. Compare the final file count too; a transfer
   passes only when every file is present exactly once and all comparisons
   pass.
4. Emit one closed receipt containing source/destination counts, every
   relative path, source/destination byte count and SHA-256, all validation
   decisions, and final status. It must not synthesize DSH events or claim
   runtime behavior.

## 4. Independent collector prevalidation

Before any DSH execution, the Revision Worker must run the collector once in a
fresh OS-temp dummy tree, never against a real harness raw root. The dummy
source must contain exactly three nonempty files: one top-level file, one
nested file, and one file whose literal filename contains `[]`. The destination
must begin empty.

The prevalidation must demonstrate `3 / 3` path, bytes, and SHA matches. It
must persist, under
`experiments/preflight/36-x01-recovery-cycle-2-collector/`, the exact command,
stdout, stderr, exit code, closed collector receipt/manifest, and cleanup
receipt. The cleanup receipt must identify the exact test-only temp roots and
record that they no longer exist. This prevalidation is collector-only: it is
not a DSH experiment, does not create/use a temporary spec, and produces no
runtime evidence.

Any prevalidation failure is fail-closed: do not start Cycle 2, do not infer
that the collector would work with DSH raw, and return to `EXPERIMENT_DESIGN`.

## 5. Mandatory review gate

A fresh independent Reviewer must inspect the candidate harness diff,
before/after hashes, collector source, dummy-tree prevalidation receipts, the
unchanged Cycle 1 disposition, and this frozen plan. Review must confirm both
the no-extra-harness-change boundary and the collector's closed transfer
evidence. `prevalidation PASS` plus a fresh Reviewer `PASS` are prerequisites
to the single Cycle 2 command. Neither a Revision Worker result nor a
prevalidation PASS authorizes execution by itself.

## 6. One Cycle 2 execution, raw isolation, and hard stop

Only after the two prerequisites pass may the Lab Engineer materialize the
approved candidate as the one temporary spec in the pinned fixture. It must
first recheck the exact pinned HEAD/tag and clean fixture, fixture-local
`node_modules/.bin/vitest.cmd`, archived/candidate hashes, and Corepack pnpm
`11.7.0`; no repair is permitted if a precondition fails.

The one and only focused test command remains:

```powershell
corepack pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

It runs from the pinned fixture with fixture-local `.bin` prepended only to
the current process PATH, under Corepack pnpm `11.7.0`, once only, with a
180-second wall-clock cap. Cycle 2 raw may be written only under
`experiments/raw/36-x01/recovery-cycle-2/`; it must never overwrite Attempt 1
or Cycle 1.

Success continues to require selection `1`, observed completion of P0--P5,
all original ten acceptance assertions, complete raw capture, an exact
manifest, and final fixture cleanup/pinned cleanliness. Any nonzero exit,
selection other than `1`, incomplete P0--P5, observer/assertion failure,
timeout, capture/manifest/collector failure, or fixture-cleanup failure is a
hard stop: preserve the distinct Cycle 2 failure raw, keep Article 36 runtime
claims `BLOCKED_EVIDENCE`, and do not retry.

## 7. Execution ordering

```text
Cycle 1 retained as RED / INVALID evidence
  -> Revision Worker: minimal harness + collector
  -> collector dummy-tree prevalidation PASS
  -> fresh independent Reviewer PASS
  -> one Cycle 2 command and closed raw capture
  -> success only if unchanged 36-X01 acceptance is fully met
```

This Researcher design execution is complete only as a plan. It does not
authorize its own implementation or test execution; the next recommended gate
remains `EXPERIMENT_DESIGN` until the Revision Worker and independent review
are both complete and accepted by Master validation.
