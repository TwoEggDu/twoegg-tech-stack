# 36-X01 Recovery Cycle 2 reproduction and limitations

## Result

FAILED_REQUIRED_SOURCE_EXPERIMENT

Preflight failed before the temporary spec was materialized. The one permitted
corepack pnpm --version probe exited 1 and produced no version; stderr
reported MODULE_NOT_FOUND for the fixture-local

ode_modules\corepack\dist\corepack.js. The required exact 11.7.0 result was
therefore not established.

The focused Vitest command was not executed (ocused_command_count=0), no case
was selected, and P0--P5 were not observed. The version probe was not retried.

## Collector boundary

The reviewed collector was invoked exactly once against the required source
root and an initially empty Cycle 2 destination. Because the harness never ran,
the source root did not exist. The collector returned its closed
SOURCE_ROOT_MISSING failure receipt. No runtime file was synthesized.

## Claim ceiling

No DSH behavior is established. All Article 36 behavioral claims remain
BLOCKED_EVIDENCE. Attempt 1 and Recovery Cycle 1 remain unchanged. There was
no install, update, network, provider, server, credential, dependency, cache,
Git, Article 37+, state, status, trace, research, evidence, README, or ledger
write.