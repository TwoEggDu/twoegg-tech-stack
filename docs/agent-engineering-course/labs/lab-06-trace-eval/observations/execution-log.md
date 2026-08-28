# Lab 06 Execution Log

## Execution order

1. Captured actual `.NET 10.0.301` / Windows environment and frozen-fixture hashes.
2. Created independent CLI-only Specs and a compilable `NOT_IMPLEMENTED` Runtime shell.
3. Ran locked restore and Release build, then observed valid RED: `0 / 5` Specs, exit `1`.
4. Implemented the minimum BCL-only evaluator without modifying Design, fixture, oracle, threshold or Spec assertions.
5. Ran Release build and GREEN: `5 / 5` Specs, exit `0`.
6. Ran formal baseline and known-regression A/B; retained normalized outputs and native exits.
7. Ran FI-02 missing N06 and FI-03 scorer-version mismatch from Lab-owned copies.
8. Ran the independent formal verifier, byte equality comparison and SHA-256 inventory.
9. After fresh restore/build/Specs verification, removed only the four verified Lab-local generated `bin/` / `obj/` directories so compiled intermediates do not contaminate the Article checkpoint; source, lock files and all durable observations remain.

## Observed summary

| Observation | Actual |
|---|---|
| baseline | `8/8`; aggregate `1.0`; critical `2/2 = 1.0`; overall `PASS`; native exit `0` |
| known-regression | `7/8`; aggregate `0.875`, threshold passes; critical `1/2 = 0.5`; overall `FAIL`; native exit `2` |
| per-case delta | `C01=REGRESSION`; other seven=`UNCHANGED`; improvements=`0` |
| FI-02 | missing `N06`; `UNKNOWN`; `missing=1`; `unknown=1`; `manifest_comparable=false`; overall `FAIL`; native exit `2` |
| FI-03 | `scorer_version=v2`; `INCOMPARABLE`; no ordinary aggregate/delta emitted; overall `FAIL`; native exit `3` |
| repeatability | baseline A/B byte-equal and SHA `e44d27...76d6c`; regression A/B byte-equal and SHA `3e0a1b...972ce` |

## Unexpected behavior retained

- The first ad-hoc PowerShell byte-comparison helper invoked a static extension as an instance method and failed. The normalized files were unaffected; a corrected static-call verifier returned both equality checks `True`.
- One outer shell response represented a non-zero native evaluation as command exit `1`; explicit `$LASTEXITCODE` capture proved the evaluator codes were `2` and `3`. The native codes, raw summaries and normalized artifacts are authoritative.

## Reproduction boundary

Run from this Lab directory using the exact commands in `tdd-red/process-record.md`, `tdd-green/process-record.md`, `run-a/process-record.md`, `run-b/process-record.md`, and `fault-injection/process-record.md`. Build output directories are not evidence; durable evidence is the source, frozen inputs, process records and normalized observations.
