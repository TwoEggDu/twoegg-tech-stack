# 36-X01 Recovery Cycle 2 post-review disposition

Status: `POST_REVIEW_DISPOSITION / NOT_RAW / NOT_RETROSPECTIVE_PASS`

## Scope and authority boundary

This document truthfully disposes `A36-C2-FR-F01` through
`A36-C2-FR-F05`. It is a later review disposition, not a replacement for the
Cycle 2 raw capture, not a reconstructed worker envelope, and not a new Lab
result. No file in the original Cycle 2 raw directory, manifest, reproduction
record, or envelope record is amended by this disposition.

Recovery Cycle 2 remains `FAILED_LAB`. Its focused command count remains `0`,
Vitest selection remains `0`, P0--P5 remain `NOT_OBSERVED`, acceptance remains
`0 / 10`, and Article 36 runtime claims remain `BLOCKED_EVIDENCE`. This
disposition cannot be used as runtime evidence and does not convert Cycle 2
into a retrospective `PASS`.

## Finding dispositions

### A36-C2-FR-F01 — first invalid envelope was not durably preserved

Disposition: `MISSING / NOT_PROVABLE`.

The full payload of the first invalid Cycle 2 Lab envelope was not preserved
verbatim in a durable repository artifact. The retained subagent trace records
only a later summary: `artifacts_created` had the wrong object type and an
unknown root field named `result` was present. That summary can prove that the
Master rejected a malformed envelope and requested an envelope-only
correction. It cannot prove the complete original payload, every original key
or value, the contents of the malformed object, the value of the unknown
field, or any omitted field.

The missing payload must not be reconstructed from conversation memory,
worker recollection, the corrected envelope, or the later summary. There is no
truthful backfill available. The first envelope therefore remains
`MISSING / NOT_PROVABLE` and supplies no execution authority.

### A36-C2-FR-F02 — corrected envelope used an invalid recovery mapping

Disposition: `INVALID / NON_AUTHORITY`.

The corrected Cycle 2 Lab envelope is retained in `subagent-trace.md` with
`status: FAIL`, `blocker: FAILED_LAB`, and
`next_allowed_gate: EXPERIMENT_EXECUTE`. That transition is invalid under the
repository Worker Result Contract. The frozen common mapping is:

```text
FAILED_LAB -> status FAIL
           -> gate_completed false
           -> next_allowed_gate LAB_EXECUTE
           -> blocker FAILED_LAB
```

The corrected envelope cannot authorize recovery or execution. Its historical
record remains unchanged and must not be rewritten to appear valid. Any future
failed required-Lab envelope must use the repository mapping
`FAILED_LAB -> LAB_EXECUTE`; that future rule does not retroactively validate
the Cycle 2 record.

### A36-C2-FR-F03 — reproduction narrative contains escaped-control corruption

Disposition: `ORIGINAL NARRATIVE INVALID / RAW JSON FACTS RETAINED`.

The original `reproduction-and-limitations.md` remains unchanged. It contains
a line break inside the intended `node_modules` path and U+000C before the
intended `focused_command_count` text. Those corrupted strings are not an
exact failure receipt and must not be quoted as authoritative command output.

The precise preflight facts are instead taken from the unchanged
`preflight-environment.json`:

- `pnpm_version_probe.exact_command` is `corepack pnpm --version`;
- `pnpm_version_probe.invocation_executable` is `corepack.cmd`;
- exit code is `1`, stdout is empty, and no pnpm version was observed;
- stderr reports `MODULE_NOT_FOUND` for
  `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814\node_modules\corepack\dist\corepack.js`;
- required package-manager identity is `pnpm@11.7.0`;
- `focus_command_count` is `0`; and
- the version probe was not retried.

The unchanged `timing-selection-phase-summary.json` additionally records
focused command `NOT_EXECUTED`, selection `0`, P0--P5 `NOT_OBSERVED`, and
acceptance `0 / 10`. This disposition cites those durable facts; it does not
repair the original narrative, raw files, or manifest.

### A36-C2-FR-F04 — current durable projection versus historical stale state

Disposition: `CURRENT SURFACES PROJECT CYCLE 3 / HISTORICAL STALENESS RETAINED`.

The failure review found stale durable detail and timestamp values at review
time. Current durable surfaces now project the authorized Cycle 3 design:

- `course-run-state.md` records Article `36`, Gate `EXPERIMENT_DESIGN`, active
  worker `RESEARCHER / /root/a36_cycle3_design`, `active_blocker: NONE`, and
  `last_updated: 2026-09-01T13:27:18+08:00`;
- the course README and `status.md` identify
  `RECOVERY CYCLE 3 AUTHORIZED`; and
- the Article 36 README identifies
  `RUNNING / RECOVERY_CYCLE_3_DESIGN / NOT_PUBLISHED` and keeps
  `A36-C2-FR-F01--F05` visible as an authorized truthful repair scope.

These current projections prove only the present Cycle 3 control surface. They
do not prove that the stale values never existed, do not erase the review-time
finding, and do not repair the missing/invalid Cycle 2 envelope or raw
evidence.

### A36-C2-FR-F05 — destination state was not observed

Disposition: `DESTINATION INITIALLY ABSENT / DESTINATION STATE NOT_OBSERVED`.

`preflight-environment.json` records the Cycle 2 raw destination as initially
absent. The collector then evaluated the source root first, found that source
root absent, and returned `SOURCE_ROOT_MISSING` before it reached destination
existence or emptiness validation. The `false` destination validation fields
in the closed failure receipt are therefore unexecuted defaults, not an
observation that the destination was empty or nonempty.

The truthful statement is: the destination was initially absent, and its state
was `NOT_OBSERVED` by the collector because `SOURCE_ROOT_MISSING` returned
first. The original prose saying the destination was initially empty remains
overstated and unchanged.

## Final disposition and forward boundary

| Item | Final status | Authority |
|---|---|---|
| First invalid Lab envelope | `MISSING / NOT_PROVABLE` | None |
| Corrected Lab envelope | `INVALID / NON_AUTHORITY` | None |
| Original reproduction narrative | `INVALID` where control characters corrupted exact text | None beyond separately cited JSON facts |
| Current durable Cycle 3 projection | `CURRENT / OBSERVED` | Current control-state evidence only |
| Collector destination at invocation | `NOT_OBSERVED` | None |
| Recovery Cycle 2 | `FAILED_LAB` | Failure evidence only |
| Article 36 runtime behavior | `BLOCKED_EVIDENCE` | No runtime evidence |

Cycle 3 may consume this document only as a truthful failure disposition. It
must use a distinct raw directory, preserve all old files, obtain a fresh
launcher prevalidation and independent review, and execute no more than the
separately authorized single focused attempt. Nothing here changes the frozen
hypothesis, P0--P5, ten acceptance assertions, safety boundary, pinned DSH
baseline, candidate harness, collector, or claim ceiling.
