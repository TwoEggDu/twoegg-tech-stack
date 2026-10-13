# Article 36 / 36-X01 Recovery Cycle 9 bounded addendum

Status: `DESIGN ONLY / NO CYCLE 9 EXECUTION`. This addendum is the complete Cycle 9 execution authority candidate only after a fresh design Reviewer returns `design_decision=APPROVED` and Master validates that result. It records no runtime PASS and proves no fix. Article 37 remains forbidden.

## 1. Immutable input and truthful disposition

Cycle 9 preserves all Cycle 1--8 repository artifacts and external failure sites byte-for-byte. In particular, it MUST NOT modify, delete, rename, backfill, or rerun:

| immutable Cycle 8 input | bytes | SHA-256 / disposition |
|---|---:|---|
| `experiments/36-x01-recovery-cycle-8-plan.md` | 71430 | `7643609EA0B4AF534F598768EE99E309749DF2C896C3C0498C569DD89B510707` |
| `experiments/36-x01-recovery-cycle-8-recorder.Tests.ps1` | 10571 | `EBF92D46A9286C45EB8D47F0D25DC2AD3B9C6412512083AA6556ED7F3A907CFB` |
| `experiments/preflight/36-x01-recovery-cycle-8-tests/source-identity.kv` | 3169 | `FE7A83FBD0D5E970E673864BA98BF4ABCCC2A5966F4A73AF21963BD8F5F34AE2` |

The failed Cycle 8 Probe root is exactly `C:\Users\IGG\AppData\Local\Temp\dsh-a36-c8-bootstrap-prevalidation-662d5715168c4e909225b1b3ff1f46cd`. It is immutable and cleanup-ineligible. Its closed current inventory is:

| relative path | bytes | SHA-256 |
|---|---:|---|
| `probe.bootstrap.kv` | 2415 | `21BA412B4D727CB34DB514F6C272B5AB155931B38DB0B2C4B5E7ED9FB10410C7` |
| `probe/emergency.kv` | 337 | `68212A1A47EA7F9256FB9C1968F2F7E491DAB0A088DF9F2A553B51113326CC8B` |
| `probe/identity.kv` | 905 | `4013DD71A85E4D61892767F33E59BEA1FAB11B7F60206198D4542C4A33713B03` |
| `probe/ready.kv` | 146 | `767F2FF7F30CBD17B0A934018C36C70EE6D2D73772F11858AFCF542F45F9E2EF` |
| `probe/stderr.bin` | 27 | `AFF82EC7FE7BE9051248591704C400932F773ADBD60D4753FAC97E20BA573995` |
| `probe/stdout.bin` | 27 | `4CC904CDC605C3B41F5BD62B6B52B3758351C79C081063377C49997F40355B64` |

`probe/result.kv`, `probe/inventory.tsv`, and `probe/exception.kv` are absent. The Cycle 8 Tests source contains `[Task[]]$tasks` in its pump-settlement path, while the durable emergency receipt decodes to `System.Management.Automation.RuntimeException: Unable to find type [Task].` These facts identify the observed bootstrap failure surface. They do not prove what the child exited with: because the terminal result and inventory are absent, Cycle 8 child exit remains `NOT_DURABLY_PROVABLE`. Cycle 9 MUST retain that wording and MUST NOT retrospectively mark Cycle 8 PASS.

## 2. Minimal remedy and architecture decision

Eight recovery cycles show that infrastructure complexity, not Article 36's frozen research hypothesis, is now the dominant risk. Cycle 9 therefore rejects a new serializer, schema, module, launcher, framework, alternate shell, or generalized command engine. It changes one test-infrastructure expression in a new file: the Cycle 9 Tests pump settlement uses the fully qualified array type `[System.Threading.Tasks.Task[]]`. The call remains `[System.Threading.Tasks.Task]::WaitAll(...)`; both stdout and stderr tasks share one five-second pump deadline. This is a source-level candidate, not a verified correction until the single Cycle 9 Probe is accepted.

The new Tests file otherwise retains the Cycle 8 direct-byte first-marker contract and test cases. It is a new Cycle 9 artifact, never a patch to the Cycle 8 Tests. A design or implementation proposal that adds another abstraction to avoid this one type-resolution surface fails YAGNI review.

Exactly four Cycle 9 source files may exist:

1. `experiments/36-x01-recovery-cycle-9-recorder.Tests.ps1`;
2. `experiments/36-x01-recovery-cycle-9-recorder.ps1`;
3. `experiments/36-x01-recovery-cycle-9-runner.ps1`;
4. `experiments/36-x01-recovery-cycle-9-verify.ps1`.

Their combined UTF-8 byte count MUST be `<= 24576`. This cap is retained because the new Tests is expected to remain near the 10571-byte Cycle 8 contract, leaving about 14 KiB for three Article-specific production scripts; exceeding it is evidence that the recovery has become another framework. No fifth source, generated schema, JSON model, helper module, copied launcher, or code generation is allowed. Source drift after accepted GREEN abandons Cycle 9.

## 3. Frozen Article 36 and DSH boundaries

Cycle 9 changes no research variable:

| frozen item | identity / rule |
|---|---|
| original design | `experiments/36-x01-long-session-compaction-cancel-resume.md`, 22585 bytes, `2C7377DB3EEC959487CC33EACADC55D2697020092EF97E2F56CD5ECF73F1852C` |
| frozen recovery harness | `experiments/36-x01-recovery-cycle-2-instrumentation.ts`, 34852 bytes, `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B` |
| frozen collector | `experiments/36-x01-recovery-cycle-2-collector.ps1`, 6398 bytes, `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045` |
| frozen Corepack | `C:\nvm4w\nodejs\corepack.cmd`, 218 bytes, `920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F` |
| DSH repository | `https://github.com/deepseek-ai/deepseek-harness` |
| DSH tag / commit | `dsh-v0.1.2-alpha.1` / `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| fixture | `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`; pinned and clean before every permitted stage |
| formal hypothesis / falsifier / acceptance | exactly the original design; no addition, deletion, reinterpretation, or threshold change |
| formal safety | zero network, Provider, credential, server, production write, package install, dependency update, or baseline migration |
| formal focus | one integrated P0--P5 case; no owner test or infrastructure receipt may substitute for runtime evidence |
| formal command | exact frozen Corepack/PATH launch and `pnpm exec vitest run SPEC --reporter=verbose`; one attempt, focused cap 180 seconds |

The full protected Cycle 1--7 anchor catalog and structural assertions are imported unchanged from immutable Cycle 8 plan sections 3.1 and 10.1. Cycle 9 compatibility MUST recompute them; it cannot replace a mismatched anchor with current bytes. Cycle 8 plan sections 8--10 remain the normative recorder grammar, process/stream semantics, formal helper semantics, evidence mapping, and 36-X01 acceptance contract except that every Cycle 8 source/preflight/raw path is substituted by the exact Cycle 9 path frozen below. Where this addendum narrows a Cycle 9 recovery surface, this addendum controls; it never weakens the original experiment.

Infrastructure Probe/RED/GREEN/compatibility can prove only capture integrity. Article runtime Claims remain `BLOCKED_EVIDENCE` until the one formal experiment succeeds and Researcher completes Evidence Merge.

## 4. Exact Cycle 9 paths and write ownership

Let `E` be the absolute `experiments/` directory of Article 36. Actual artifacts contain expanded absolute paths, never aliases or angle brackets.

| alias | exact path / owner |
|---|---|
| `TST` | `E/36-x01-recovery-cycle-9-recorder.Tests.ps1`; Revision Worker creates before Probe |
| `REC` | `E/36-x01-recovery-cycle-9-recorder.ps1`; Revision Worker creates only after accepted RED |
| `RUN` | `E/36-x01-recovery-cycle-9-runner.ps1`; Revision Worker creates only after accepted RED |
| `VER` | `E/36-x01-recovery-cycle-9-verify.ps1`; Revision Worker creates only after accepted RED |
| `P` | `E/preflight/36-x01-recovery-cycle-9-tests/`; Revision Worker owns parent receipt and official test receipts |
| source identity | `P/source-identity.kv`; Revision Worker CreateNew-writes before Probe |
| RED | sibling `P/red.bootstrap.kv` plus root `P/red/` |
| GREEN | sibling `P/green.bootstrap.kv` plus root `P/green/` |
| `C` | `E/preflight/36-x01-recovery-cycle-9/attempt-1/`; Revision Worker compatibility root |
| Probe | one new system-TEMP direct child `dsh-a36-c9-bootstrap-prevalidation-<32-lower-hex-nonce>/`; Revision Worker |
| `F` | `E/raw/36-x01/recovery-cycle-9/attempt-1/`; fresh Lab Engineer only after approval |
| `D` | `F/collector-destination/`; Lab caller pre-creates it empty; only frozen collector writes payload beneath it |
| `SPEC` | fixture-relative `packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts`; formal runner owns exact temporary copy |
| `ART` | system-TEMP `dsh-article36-x01-artifacts/`; frozen harness output only |

Before any stage, its marker/root and all later-stage paths MUST be absent. A pre-existing target is preserved and is terminal. No broad cleanup, `git clean`, overwrite, rename, reuse, or fallback target is allowed. Cycle 1--8 roots and the failed Cycle 8 TEMP root are never in the Cycle 9 write allowlist.

`D` is a dedicated empty destination, disjoint from `F/execution`, its subject and recorder roots, verifier roots, and assessment. The Lab caller CreateNew-writes `F/owners/collector-destination.owner`, records the expanded `D`, `ART`, collector identity, `expected_initial_state=EMPTY_DIR`, and `allowed_byte_writer=FROZEN_COLLECTOR`, then proves zero immediate and recursive entries before F0. Runner may validate and pass `D`; verifier may read it; neither may write payload beneath it. Failed transfer or ownership proof preserves `ART`, `D`, and all receipts.

## 5. Source identity, first marker, and exact-command provenance

The Revision Worker creates `TST`, freezes bytes/SHA, proves `REC/RUN/VER` absent, and then direct-byte CreateNew-writes `P/source-identity.kv` using the Cycle 8 23-row KV1 grammar with Cycle 9 paths. `creator=REVISION_WORKER` identifies the parent-receipt byte creator. The distinct field `expected_bootstrap_creator=Tests/Bootstrap` is the only field compared with each bootstrap marker's `creator`; these two creator fields MUST NOT be equated. Corresponding owner role/execution ID, nonce, Tests path/SHA, pwsh path/bytes/SHA, and selected marker/root paths must match exactly.

The new Tests `Bootstrap` entry retains the direct UTF-8/no-BOM/LF-only CreateNew first marker. Before marker flush and close it may perform only parameter binding, scalar/path checks, fixed string-array construction, UTF-8/Base64 encoding, and direct file write. It MUST NOT call JSON, `JsonSerializer`, `ConvertTo-Json`, a schema, catalog, module, dot-source, reflection, hashing, Git, Node, Corepack, pnpm, Vitest, DSH, or a complex generic overload. The only Cycle 9 source change candidate at pump settlement is the fully qualified `[System.Threading.Tasks.Task[]]` type.

Every attempted executable command has immutable provenance before launch: the owner KV1 stores expanded executable, optional script or `NONE`, cwd, timeout, expected start/exit/identity/exception, `argv_count`, and each ordered argv token in contiguous Base64 UTF-8 `A` rows; the recorder first marker binds owner path/SHA/nonce, marker/root, and explicit `PumpGraceSeconds`. An empty argv is represented by `argv_count=0` and exactly zero `A` rows, never `null`, omitted, truthy/falsy shorthand, or JSON. The bootstrap marker likewise stores its exact ordered child argv before process start.

Emergency outranks result. A missing/partial terminal result, pump failure, timeout, kill/death ambiguity, flush/close failure, identity mismatch, or inventory mismatch never inherits a child exit from stdout or expectation.

### 5.1 Transitive Cycle 8 preservation witness

At every boundary after this revised design -- design recheck; implementation entry; immediately before and after Probe, RED, GREEN, and compatibility; preflight recheck; Master cleanup/approval before and after cleanup; formal Lab allocation; immediately before F0; and F17 -- the responsible role MUST recompute rather than trust the three immutable repository-file identities in section 1. It MUST also re-enumerate the failed Cycle 8 TEMP root as exactly the six listed files, with the same byte counts and SHA-256 values, no seventh file, and exactly these three absent terminal artifacts: `probe/result.kv`, `probe/inventory.tsv`, and `probe/exception.kv`. Any mismatch is terminal preservation failure. The root remains cleanup-ineligible even when every identity matches.

Compatibility verifier CreateNew-writes `C/assessment/cycle-8-preservation.kv` as a durable closed KV1 witness. It binds the expanded absolute paths, bytes, and SHA-256 values of the Cycle 8 plan, Tests, and source identity; the expanded failed-TEMP root; six contiguous file rows containing normalized root-relative path, bytes, and SHA-256; three contiguous absent rows; `descendant_file_count=6`; `unexpected_file_count=0`; and `cleanup_eligible=false`. It then binds that witness path, bytes, and SHA-256 into `C/assessment/formal-source-hashes.tsv` in fixed identity KV rows after the imported Cycle 8 table. Compatibility cannot PASS if either durable file omits or disagrees with any preservation fact.

The fresh preflight Reviewer and Master independently recompute the same closed set and compare it to both compatibility files. `A36-C9-PREFLIGHT-APPROVAL-V1` MUST contain actual values for `cycle8_plan_bytes`, `cycle8_plan_sha256`, `cycle8_tests_bytes`, `cycle8_tests_sha256`, `cycle8_source_identity_bytes`, `cycle8_source_identity_sha256`, `cycle8_failed_temp_root_b64`, `cycle8_failed_temp_inventory_bytes`, `cycle8_failed_temp_inventory_sha256`, `cycle8_failed_temp_file_count` (exactly `6`), `cycle8_failed_temp_missing_terminal_count` (exactly `3`), and `cycle8_failed_temp_cleanup_eligible` (exactly `false`). The formal Lab owner/source-hash copy and F17 verifier bind and recheck these same approved values; no later stage may replace them with current bytes or omit the failed scene because it is external to the repository.

## 6. Required TDD order and one-shot budgets

The authorized order is strict and non-replayable:

1. fresh design Reviewer reads this addendum and immutable inputs; only `design_decision=APPROVED` permits implementation;
2. Revision Worker creates only `TST`, freezes its identity, creates `P/source-identity.kv`, and proves production scripts absent;
3. exactly one isolated `Bootstrap/Probe` attempt; accepted Probe proves only first-marker/process-capture settlement;
4. only after Probe acceptance, exactly one official `Bootstrap/Red`; production remains absent;
5. only after expected RED acceptance, create `REC/RUN/VER`; `TST` cannot change;
6. exactly one GREEN, then freeze all four source identities;
7. exactly one compatibility attempt with frozen identities;
8. fresh read-only preflight Reviewer independently rechecks all Probe/RED/GREEN/compatibility receipts and identities;
9. only after `preflight_decision=APPROVED`, Master performs the explicit successful-Probe cleanup transition and writes the bounded approval record;
10. only after cleanup and approval PASS, a fresh Lab Engineer performs exactly one formal experiment.

| stage | attempts | child/leaf budget | enclosing budget | caller total |
|---|---:|---:|---:|---:|
| isolated Probe | 1 | 10s | bootstrap 20s | 25s |
| official RED | 1 | 30s | bootstrap 40s | 45s |
| GREEN | 1 | absent 10+5, exit-23 15+5, verifier 30+5 | subject 80s; bootstrap 90s | 95s |
| compatibility | 1 | six leaves total 140s | runner 155s; top 165s; verifier 40s | 205s |
| formal | 1, conditional | leaves total 500s | runner 530s; top 540s; verifier 70s | 610s |

Every recorder call receives an explicit positive integer `PumpGraceSeconds`. Process timeout includes start, natural exit, or one bounded kill plus confirmed death. After terminal observation, stdout and stderr pumps share one deadline `terminal_observed + PumpGraceSeconds`; there is no per-stream grace and no hidden default. Required GREEN tuples are `(10,5)`, `(15,5)`, `(30,5)`.

Compatibility retains the Cycle 8 matrix with Cycle 9 paths: C1 absent `10/5`, C2 pwsh exit-23 `15/5`, C3 Git `--version` `15/5`, C4 Node `--version` `10/5`, C5 Corepack `pnpm --version` `30/5`, C6 Corepack `pnpm exec vitest --version` `30/5`, top runner `155/10`, and verifier `30/10`. Its leaf arithmetic is `(10+5)+(15+5)+(15+5)+(10+5)+(30+5)+(30+5)=140s`.

Formal retains the exact Cycle 8 F0--F17 commands and ordering, with Cycle 9 `RUN/REC/VER/TST/C/F` paths: Node `10/5`; pnpm `30/5`; ten ordinary Git helpers each `10/5`; `Get-FileHash` `10/5`; no-index diff `20/5` expecting exit 1; focused Vitest `180/10`; collector `60/10`; top runner `530/10`; verifier `60/10`. Leaf arithmetic is `500s`; total is `540+70=610s`. No original launcher, alternate Corepack, package manager fallback, internal hash substitute, or reporter change is allowed.

### 6.1 Frozen formal Corepack/PATH semantics

The exact F0/F1--F17 command matrix above does not change. The Cycle 9 formal runner is the sole owner of the process-scoped PATH mutation inside F0 and MUST implement this fixed order:

1. validate the F0 owner, approval block, formal source-hash copy, fixture root, WorkingDirectory, and frozen source identities while retaining the inherited process PATH unchanged;
2. capture that exact original process PATH as UTF-8 bytes plus SHA-256, and separately capture the current user- and machine-scope PATH values and hashes;
3. before any PATH mutation, resolve the host `corepack.cmd` application to exactly one absolute path outside the fixture, require exact equality with `C:\nvm4w\nodejs\corepack.cmd`, require `218` bytes and SHA-256 `920630A1D1EC47AEDEA7345E3C868ECDC07E191373497BBF47FBBF5942FBAD4F`, and close the Corepack identity receipt; no missing, multiple, relative, in-fixture, or hash-drifted result is accepted;
4. require fixture `package.json` to declare exact `packageManager=pnpm@11.7.0`, require the expanded fixture-local `node_modules/.bin` directory, then prepend exactly that one directory plus the platform path separator to the captured original PATH; no user- or machine-scope environment write is allowed;
5. close `F/execution/subject/formal-path-before.kv` and `F/execution/subject/formal-path-active.kv` before F1. They contain Base64 of the exact before/active process PATH strings, their UTF-8 byte counts and SHA-256 values, the expanded prepended directory, Corepack identity, mutation owner `RUNNER/F0`, and `scope=PROCESS_ONLY`;
6. start F1--F16 only after those receipts close. Every nested recorder uses `UseShellExecute=false`, receives no per-child PATH override, and therefore inherits the runner's exact active process PATH. Every nested owner binds the active PATH SHA-256; F2 and F10 additionally bind the already-resolved absolute Corepack path/bytes/SHA. F2 and F10 invoke only that absolute path; bare `corepack` / `corepack.cmd` invocation count is zero. This inheritance rule explicitly reaches unchanged F10 argv `pnpm,exec,vitest,run,SPEC,--reporter=verbose`;
7. a `finally` block runs after the F1--F16 body on success, failure, or exception. It restores the runner process PATH to the exact captured original string, verifies ordinal equality and matching UTF-8 bytes/SHA-256, re-reads user/machine PATH and requires their before/after hashes to match, then CreateNew-writes and flushes `F/execution/subject/formal-path-restored.kv` with the exact before, active, and restored identities and `restoration=PASS|FAIL`.

F0 cannot return success unless restoration is `PASS`; a missing/partial path receipt, environment inheritance mismatch, Corepack drift, user/machine mutation, or failed finally restoration is a formal failure and preserves all receipts. The Lab caller records and verifies these receipt paths but does not resolve Corepack or mutate PATH. The recorder and verifier do not mutate PATH. This freezes launch semantics without changing the formal command, DSH baseline, harness, collector, hypothesis, or acceptance criteria.

The first failure at any stage consumes that stage's one-shot budget, preserves the complete or partial site, starts no later stage, and returns a terminal recovery result. No same-cycle edit, cleanup, retry, diagnostic substitute, or Reviewer can replenish it.

## 7. Stage acceptance and preservation

Probe PASS requires the exact new `TST` identity; complete sibling marker, identity, ready, stdout, stderr, result, exception, and self-excluding inventory; child exit `23`; bootstrap exit `0`; exact stdout/stderr sentinels; complete pump/flush/close evidence; and zero writes/calls to official roots, production files, fixture, Corepack, pnpm, Vitest, DSH, `SPEC`, or `ART`. Failure preserves the exact TEMP root permanently and stops before RED.

RED is accepted only when the same `TST` identity produces bootstrap exit `0`, child exit `1`, empty stdout, exact `RECORDER_CONTRACT_UNIMPLEMENTED` stderr plus platform newline, complete receipts, and `REC/RUN/VER` remain absent. Marker-only, empty-root, generic exit 1, or production presence is terminal unexpected RED.

GREEN is accepted only when the same immutable Tests passes expected-absent start, started child exit-23, and independent verifier cases; all receipt/stream/identity/inventory checks pass; explicit empty argv representation is verified; and the four-file cap passes. It freezes all four source identities.

Compatibility is accepted only when the exact frozen sources, pwsh and Corepack identities, protected history, DSH pin/clean state, `SPEC`/`ART` absence, command provenance, row-wise timeouts/graces, manifests, and zero focused/collector/DSH calls all pass. It creates `C/assessment/formal-source-hashes.tsv`; no mismatch may establish a new baseline.

The fresh preflight Reviewer must recompute, not trust, all source hashes, owner/marker links, receipts, inventories, command tokens, protected history, budget arithmetic, fixture state, `SPEC`/`ART` absence, and formal-root absence. `preflight_decision=APPROVED` is necessary but not sufficient for formal.

Only Master may remove the exact successful Cycle 9 Probe root. Before removal it validates the system-TEMP parent, exact basename/nonce, owner/source links, closed inventory, absence of reparse points, and Probe PASS. It removes only that root, proves absence, then writes one bounded `A36-C9-PREFLIGHT-APPROVAL-V1` record binding plan/tests/production/source-hash/compatibility identities, every Cycle 8 preservation field required by section 5.1, and `probe_cleanup=PASS`. A failed Probe -- including the immutable Cycle 8 failed Probe -- is never cleanup-eligible. Cleanup failure preserves residue, writes no approval, and stops.

The formal Lab caller alone creates `F`, empty `F/execution`, empty `D`, owner receipts, and CreateNew-copies the approved compatibility source-hash bytes to `F/owners/source-hashes.tsv`. The Lab Engineer, not Master, Revision Worker, runner, or verifier, owns that formal source-hash copy and verifies byte/SHA equality before F0. Runner copies the frozen harness to `SPEC`, executes the exact formal matrix, uses the frozen collector to transfer `ART` into `D`, preserves all failure evidence, and removes only owned `SPEC`/`ART` after verified transfer. Verifier writes only sibling `F/assessment` and maps every original acceptance item to exact execution or collector bytes. Runtime success still requires every original P0--P5 assertion, not merely exit 0.

## 8. Exact allowlist and role/gate chain

Design Researcher allowed write is only this file. A bounded design Revision Worker may modify only this file and must include Finding-by-Finding disposition. The implementation Revision Worker may create only the four Cycle 9 source files, `P` receipts, `C`, and one Cycle 9 TEMP Probe root listed above. The preflight Reviewer is read-only. Master may update only canonical state/trace surfaces and remove the one proven-successful Cycle 9 Probe root during its explicit transition. The formal Lab Engineer may create only `F`, the exact owned `SPEC`, and `ART`; frozen collector transfers `ART` payload only to `D`. No role may write Article 37.

Canonical route:

```text
RESEARCHER / EXPERIMENT_DESIGN
-> fresh REVIEWER / REVIEW
-> bounded REVISION_WORKER / REVISION only for registered design Findings
-> fresh REVIEWER / REVIEW_RECHECK until design APPROVED
-> fresh REVISION_WORKER / REVISION for Tests -> Probe -> RED -> production -> GREEN -> compatibility
-> fresh REVIEWER / REVIEW_RECHECK for preflight
-> MASTER_ORCHESTRATOR / REVIEW_RECHECK / MASTER_DETERMINISTIC for successful-Probe cleanup and approval
-> fresh LAB_ENGINEER / LAB_EXECUTE for one formal experiment
-> fresh LAB_ENGINEER / LAB_OBSERVATION for read-only raw organization
-> fresh RESEARCHER / EVIDENCE_MERGE only after LAB_OBSERVATION succeeds
```

Every execution returns exactly one closed `worker_result` root with exactly 11 fields: `role`, `article`, `gate`, `execution_type`, `status`, `artifacts_created`, `artifacts_modified`, `gate_completed`, `next_allowed_gate`, `blocker`, and `notes`. All arrays, including empty artifact arrays, are explicit `[]`. Worker executions use `REAL_SUBAGENT`; only the cleanup/approval transition uses `MASTER_DETERMINISTIC`. Canonical role names and exact Gates above are mandatory; no wrapper, unknown field, alias, or prose-only handoff is valid.

Successful design returns `RESEARCHER / EXPERIMENT_DESIGN / PASS / true / REVIEW / NONE`. An approved design Reviewer routes to implementation `REVISION`. Reparable design Findings route to bounded `REVISION`, then fresh `REVIEW_RECHECK`; only the Reviewer can close them. Successful implementation routes `REVISION_WORKER / REVISION / PASS / true / REVIEW_RECHECK / NONE`. Successful preflight routes to the Master transition. Successful Master cleanup/approval routes `MASTER_ORCHESTRATOR / REVIEW_RECHECK / PASS / true / LAB_EXECUTE / NONE`. Formal success routes `LAB_ENGINEER / LAB_EXECUTE / PASS / true / LAB_OBSERVATION / NONE`. Successful raw organization routes `LAB_ENGINEER / LAB_OBSERVATION / PASS / true / EVIDENCE_MERGE / NONE`. Successful Evidence Merge is a separate `RESEARCHER / EVIDENCE_MERGE` result under the canonical Evidence contract. A Researcher never owns `LAB_OBSERVATION`, and a formal-success envelope never skips that Gate.

A one-shot infrastructure failure is `REVISION_WORKER / REVISION / BLOCKED / false / NONE / HUMAN_DECISION_REQUIRED`. Cleanup failure is the same status/gate mapping for `MASTER_ORCHESTRATOR / REVIEW_RECHECK / MASTER_DETERMINISTIC`. Formal failure is `LAB_ENGINEER / LAB_EXECUTE / FAIL / false / LAB_EXECUTE / FAILED_LAB`; its `LAB_EXECUTE` is only a future recovery candidate. Any failure triggers `STOP POLICY WINS`.

## 9. Design Gate disposition

This addendum is ready for fresh independent design review. It authorizes no execution by itself. The Reviewer must reject any attempt to modify or rerun Cycle 8, weaken exact-command provenance, omit empty arrays, merge collector destination with receipt trees, give each pump its own grace interval, conflate source-identity creator with bootstrap creator, let a non-Lab role copy formal source hashes, clean a failed root, change the baseline/hypothesis/acceptance, exceed the four-file cap, or start Article 37.

## 10. Finding-by-Finding revision disposition

| Finding | Files Changed | What Changed | Evidence Impact | Proposed Status |
|---|---|---|---|---|
| `A36-C9-DR-F01` | this plan only | Canonical tail is now `LAB_ENGINEER / LAB_EXECUTE -> LAB_ENGINEER / LAB_OBSERVATION -> RESEARCHER / EVIDENCE_MERGE`; success-envelope routing matches it. | Role/gate ownership is deterministic; no runtime claim changes. | `READY_FOR_RECHECK` |
| `A36-C9-DR-F02` | this plan only | Froze the runner-owned pre-mutation Corepack resolution, exact process-only PATH prepend, inherited environment through F10, exact before/active/restored receipts, and unconditional finally restoration verification. | Makes formal launch provenance auditable without changing the formal command, baseline, hypothesis, or acceptance. | `READY_FOR_RECHECK` |
| `A36-C9-DR-F03` | this plan only | Requires every later boundary to recompute the three immutable Cycle 8 file identities plus the exact six-file/three-absence failed-TEMP scene; compatibility and Master approval now bind the closed preservation witness. | Prevents later stages from silently dropping or normalizing failed Cycle 8 evidence; the failed root remains cleanup-ineligible. | `READY_FOR_RECHECK` |

This revision is design-only. It records no Probe, RED, GREEN, compatibility, formal, LAB_OBSERVATION, or Evidence Merge execution and grants no Article 37 authority.
