# 36-X01 reproduction and limitations

## Result

`FAILED_REQUIRED_SOURCE_EXPERIMENT`

The frozen command was executed exactly once from the pinned fixture root and exited `1` before Vitest started:

```text
'vitest' is not recognized as an internal or external command,
operable program or batch file.
```

The command selected zero cases because `pnpm exec` could not resolve the `vitest` executable in this restored fixture environment. This is a command-launch/toolchain failure, not a failing DSH assertion and not evidence about runtime Usage, Compaction, Cancellation, persistence, Resume, or external-effect behavior.

## Reproduction

Fixture root:

```text
C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814
```

Pinned identity:

```text
tag: dsh-v0.1.2-alpha.1
commit: cd5ef8148158c3a752a658978873241fdf8e2bbc
```

Exact command:

```text
pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose
```

Observed host tools were Node `v24.18.1` and pnpm `11.19.0`; the repository declares pnpm `11.7.0`. No install, update, Corepack mutation, alternate launcher, second harness, second attempt, owner-suite substitute, network request, server bind, credential access, or real Provider call was authorized or performed.

## Evidence boundary

- The harness source, full no-index patch, and SHA-256 prove what was prepared, not what ran.
- `correlation.jsonl` contains one closed-schema Lab-authored launch-failure receipt. It is not a runtime Session event.
- `request-before.json`, `request-after.json`, and `history-projection-diff.json` are explicit `NOT_OBSERVED` receipts; they are not synthetic runtime values.
- `sentinel-receipt.jsonl` records `NOT_CREATED / NOT_RUN`; no tool body entered and no external effect occurred.
- P0-P5, model attempts, usage fold, pressure crossing, compaction bracket, before/after diff, cancel settlement, durable prefix, and fresh resume remain unobserved.
- Article 36 runtime Claims therefore remain `BLOCKED_EVIDENCE`; the failed experiment must not be rewritten as PASS or combined with unrelated owner tests.

## Next contractual disposition

Return to the experiment-design/recovery authority named by the Master contract. Any environment repair or retry requires a separately authorized bounded recovery cycle; this Lab Engineer does not choose a new command or execute a second attempt.
