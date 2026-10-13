# 36-X01 Recovery Cycle 6 Schema-First Receipt Closure Implementation Plan

> **For agentic workers:** execute this frozen plan with one fresh Revision Worker and then one fresh read-only Reviewer. The Researcher who writes this plan runs no RED, GREEN, diagnostic, launcher, DSH, Corepack, pnpm, or Vitest command.

**Goal:** Replace neither Cycle 5 code nor Cycle 5 evidence, but create an independent Cycle 6 receipt-closure implementation that proves explicit empty arrays by JSON token kind and durably records the exact tokenized invocation for each new one-shot phase.

**Architecture:** Freeze a non-production schema catalog, a read-only Cycle 5 defect witness, and one Tests file containing a fixed test-only RED evidence bootstrap before the first failing run. RED closes its own evidence while the production serializer and candidate are absent; only after that expected failure may the production module and wholly new Cycle 6 candidate be created for GREEN and one isolated diagnostic. Every persisted array must pass `JsonValueKind.Array` before zero-length assertions, and every counted process invocation must persist its exact tokenized argv. A fresh Reviewer performs the final read-only recheck; PASS and FAIL both stop with no next execution authority.

**Tech Stack:** PowerShell 7, `System.Text.Json.Nodes`, `System.Text.Json.JsonDocument`, UTF-8 without BOM, SHA-256, source/AST inspection, self-excluding JSON manifests. No test framework, package manager, child process, fixture access, network access, Git mutation, launcher invocation, or DSH execution.

**Spec:** This file is the frozen Recovery Cycle 6 design. It is constrained by `docs/agent-engineering-course/subagent-contracts.md`, `docs/agent-engineering-course/course-factory.md`, `docs/agent-engineering-course/production-workflow.md`, the current Article 36 hard lock, and all immutable Cycle 1-5 evidence.

## Global constraints

- This is an architecture reset. Do not edit, patch, rerun, backfill, rename, delete, or reinterpret any Cycle 5 source or receipt.
- The Article 36 hypothesis, falsifier, acceptance criteria, safety boundary, experiment command, Claim status, and DSH baseline remain unchanged.
- DSH remains pinned to repository `https://github.com/deepseek-ai/deepseek-harness`, tag `dsh-v0.1.2-alpha.1`, commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Frozen launcher SHA-256 remains `2F16A499C1A799C8A15C03384C2BC41FB7C742236BD5DCF07229BA4C0415C64E`; frozen harness SHA-256 remains `D5DFBEC5286309C4BFB1027BA490892992144D49040C6A8E2AF201B842A0199B`; frozen collector SHA-256 remains `D04A8929EA32CF004D011D0241474C759143B75A8E88DEDBE8DC8E09080A5045`.
- The Cycle 5 plan remains `21952` bytes / `B0DD65CF4FC3C06A88EF23B3CE010ACC5F91E1C774903551B98316151EF288AA`; Tests remain `10787` bytes / `E042BC774E717ABEE31EE9B2BB297A5C4A49063C211B221C75D000BFB797219F`; candidate remains `19626` bytes / `D5AF4F2FF3F11A4BB06DD18D7357989722E851733D2F56220B2E5AFA25107274`; RED manifest remains `1086` bytes / `AC990F709427C60E2B9C312C8C41144A9A74E88E1A1DC33F870222459C555AF7`; GREEN manifest remains `1077` bytes / `7A5C834D04F200502BAC6D3D2F2C1380E343DB1EDC04FA79163AB22CB989412A`.
- Cycle 5 diagnostic `attempt-1` is absent. It must remain absent. Cycle 6 must not create a path below the Cycle 5 diagnostic root.
- The observed Cycle 5 facts remain terminal: Tests line 153 uses `@(...).Count` and cannot distinguish JSON `null` from `[]`; Tests line 183 feeds an inline conditional through PowerShell output enumeration, so its empty array becomes no pipeline object and serializes as JSON `null`; neither Cycle 5 RED nor GREEN root contains the exact-command receipt required by its plan.
- Original launcher, Cycle 5 Tests, Cycle 5 candidate, Corepack, pnpm, Vitest, focused target, collector, DSH commands, package installation, network commands, and formal Article 36 experiment have invocation budget `0`.
- The pinned DSH fixture has read budget `0` and write budget `0` for RED, GREEN, diagnostic, and Reviewer. No target spec or runtime raw root may be created.
- No worker may update `course-run-state.md`, `status.md`, Article/course README, `subagent-trace.md`, Research, Evidence, Outline, Review, published content, or Article 37+ assets.

---

## 1. Truthful starting disposition and claim ceiling

Cycle 5 remains `RED EXPECTED_FAIL / GREEN RECEIPT CONTRACT FAIL / DIAGNOSTIC NOT_STARTED / TERMINAL / NO RERUN`. `A36-C5-RR-F01` and `A36-C5-RR-F02` remain historical findings; Cycle 6 can prevent recurrence but cannot close, overwrite, or retrospectively repair them.

Cycle 6 answers one question only: can a new receipt producer preserve schema-declared empty arrays as JSON `Array` tokens and preserve a complete tokenized script-level invocation receipt without touching the launcher, fixture, process APIs, network, or DSH?

The maximum successful claim is:

```text
SCHEMA_FIRST_RECEIPT_CLOSURE_PASS
EXPLICIT_EMPTY_ARRAY_TOKEN_KIND_CONFIRMED
TOKENIZED_SCRIPT_INVOCATION_PROVENANCE_CONFIRMED
RECEIPT_ONLY / NO_LAUNCHER_OR_DSH_RUNTIME_EVIDENCE
```

It never means that Cycle 5 passed, the launcher was repaired, the Cycle 4 cause was recovered, the Article 36 hypothesis was confirmed, required runtime evidence was obtained, or `LAB_EXECUTE` was authorized.

## 2. Exact Revision allowlist and file responsibilities

The later Revision Worker may create exactly these eight paths and may modify no existing path:

1. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-schemas.json`
2. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-cycle5-defect-witness.json`
3. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.Tests.ps1`
4. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-6-receipt-closure-tests/red/`
5. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-serialization.psm1`
6. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.ps1`
7. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-6-receipt-closure-tests/green/`
8. `docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-6-receipt-closure/attempt-1/`

The schema catalog is a non-production specification of shapes and token kinds. The witness is a new immutable, normalized description of Cycle 5 defects. The one Tests file is byte-identical across RED and GREEN and contains a test-only fixed RED bootstrap writer. The RED root proves the frozen Cycle 5 defect surface plus the absence of the new production closure without executing old code. Only after RED may the production serialization module and wholly new receipt-only candidate be created. The module is the sole production serializer/validator; the RED bootstrap is not imported by production code. The GREEN root proves the new module and candidate. The final `attempt-1` is one isolated diagnostic of the new candidate only.

No temporary repository path is allowed. GREEN may use one OS temporary leaf named `a36-cycle6-receipt-closure-<guid>` only for a candidate sample; it must be removed in `finally`, and no raw temporary path may be persisted. The isolated diagnostic uses only its frozen repository receipt root and creates no additional temporary root.

## 3. Schema-first contract

### 3.1 Definition order

Implementation order is normative:

1. create and parse the non-production specification `36-x01-recovery-cycle-6-receipt-schemas.json`, including `exact-command-v1` and the explicit-array token-kind rules;
2. create and freeze the Cycle 5 defect witness;
3. create and freeze the sole Tests file, including its test-only fixed RED bootstrap writer;
4. capture plan/schema/witness/test identities and verify that both production paths—serialization module and candidate—are absent;
5. run RED once against the frozen Cycle 5 witness and absent production closure;
6. only after expected RED closes, create and freeze the production serialization module;
7. create and freeze the wholly new candidate that consumes that module;
8. run GREEN once with the same Tests bytes;
9. run the isolated diagnostic once;
10. stop for a fresh read-only Reviewer.

`ConvertTo-Json` is forbidden in all new Cycle 6 executable sources. Receipt construction must use `JsonObject`, `JsonArray`, and `JsonValue`; serialization must use `JsonNode.ToJsonString()` and direct UTF-8-without-BOM file write. No array-valued field may be produced by a PowerShell pipeline or inline conditional.

### 3.2 Production serialization helper interfaces, implemented only after RED

The following interface is specified before RED but must not exist as executable production code until expected RED has closed. The later module exports only these functions:

```powershell
New-A36JsonArrayNode -Items <IEnumerable>                         # returns one JsonArray object, including zero members
New-A36ClosedObjectNode -SchemaName <string> -Values <IDictionary> # rejects missing, unknown, or wrong-kind properties
Assert-A36JsonTokenKind -Document <JsonDocument> -Pointer <string> -ExpectedKind <JsonValueKind>
Write-A36ClosedJson -Path <string> -SchemaName <string> -Root <JsonObject>
New-A36ExactCommandNode -InvocationId <string> -Phase <string> -ExecutableIdentity <JsonObject> -ScriptIdentity <JsonObject> -Argv <IEnumerable> -WorkingDirectoryToken <string> -NestedInvocations <IEnumerable>
Get-A36FileIdentityNode -Path <string> -PathToken <string>
Get-A36SelfExcludingManifestNode -Root <string> -ExcludedRelativePath 'manifest.json'
```

`Write-A36ClosedJson` must validate before writing, write one terminal LF in UTF-8 without BOM, reopen with `JsonDocument`, validate the exact key set and every declared token kind again, and only then return byte count and uppercase SHA-256. `New-A36JsonArrayNode` must return a `JsonArray` even for no input. Its mandatory GREEN-owned test is:

- schema expects `Array`, persisted `[]` -> `JsonValueKind.Array`, member count `0`, PASS;
- schema expects `Array`, persisted `null` -> `JsonValueKind.Null`, FAIL before any count comparison.

Using `@($value).Count -eq 0`, collection truthiness, or deserialized PowerShell object count as the only array proof is forbidden. Count is checked only after `JsonValueKind.Array` succeeds.

### 3.3 Closed schema catalog

The catalog root has exactly `schema_version` and `schemas`. Every schema entry has exactly `exact_properties`, `property_token_kinds`, `array_properties`, and `additional_properties_allowed`; the last field is always `false`. Nested objects also have named closed schemas. The catalog defines these eight receipt schemas:

- `defect-witness-v1`: `schema_version`, `witness_id`, `source_identities`, `observations`, `expected_red_failures`, `cycle5_tests_executed`, `cycle5_candidate_executed`, `mutation_count`;
- `exact-command-v1`: `schema_version`, `invocation_id`, `phase`, `provenance_basis`, `executable_identity`, `script_identity`, `ordered_tokenized_argv`, `argv_count`, `tokenized_working_directory`, `nested_invocations`, `raw_command_line_persisted`, `sanitization`;
- `phase-identities-v1`: `schema_version`, `phase`, `plan_identity`, `schema_identity`, `serializer_identity`, `witness_identity`, `test_identity`, `subject_identity`, `protected_cycle5_identities`, `prior_phase_manifests`;
- `phase-summary-v1`: `schema_version`, `phase`, `status`, `logical_exit_code`, `expected_failed_assertions`, `actual_failed_assertions`, `schema_checks`, `nested_invocations`, `execution_counts`, `started_utc`, `ended_utc`, `duration_milliseconds`, `subject_executed`, `disposition`;
- `schema-validation-v1`: `schema_version`, `status`, `checked_receipts`, `explicit_array_checks`, `closed_object_checks`, `violation_count`;
- `diagnostic-result-v1`: `schema_version`, `attempt_identity`, `status`, `disposition`, `claim_ceiling`, `expected_failed_assertions`, `actual_failed_assertions`, `input_receipt_identities`, `execution_counts`, `explicit_array_invariant_pass`, `exact_command_provenance_pass`, `historical_cycle4_root_cause`, `cycle5_failure_evidence_preserved`, `new_launcher_attempt_authorized`, `lab_execute_authorized`;
- `cleanup-v1`: `schema_version`, `cleanup_pass`, `temporary_root_used`, `temporary_root_absent`, `repository_write_count_outside_allowed_root`, `pinned_fixture_read_count`, `pinned_fixture_write_count`;
- `manifest-v1`: `schema_version`, `phase`, `status`, `self_excluding`, `excluded_relative_path`, `file_count`, `files`.

Every field named `source_identities`, `observations`, `expected_red_failures`, `ordered_tokenized_argv`, `nested_invocations`, `protected_cycle5_identities`, `prior_phase_manifests`, `expected_failed_assertions`, `actual_failed_assertions`, `schema_checks`, `checked_receipts`, `explicit_array_checks`, `closed_object_checks`, `input_receipt_identities`, or `files` is declared as JSON `Array` and must remain an `Array` at cardinality zero.

Common nested schemas are closed: `file-identity` has exactly `path_token`, `byte_count`, `sha256`; `artifact-state` has exactly `state`, `path_token`, `byte_count`, `sha256` and represents both `ABSENT` and `PRESENT` without `null`; `manifest-entry` has exactly `relative_path`, `byte_count`, `sha256`; `array-check` has exactly `json_pointer`, `expected_kind`, `actual_kind`, `expected_count`, `actual_count`, `passed`; `sanitization` has exactly `root_replacement_count`, `post_sanitization_sensitive_pattern_count`, `raw_absolute_path_count`, `unsafe_text_persisted`.

## 4. Exact-command provenance contract

Each RED, GREEN, GREEN candidate sample, and diagnostic root must contain `exact-command.json`, written before substantive phase logic and never rewritten. Its `ordered_tokenized_argv` and `nested_invocations` must be JSON `Array` tokens. `argv_count` must equal the array member count after token-kind validation.

For RED only, the Tests-local bootstrap writes the seven frozen non-manifest files `exact-command.json`, `identities.json`, `phase-summary.json`, `schema-validation.json`, `cleanup-receipt.json`, empty stdout, and empty stderr, then closes the self-excluding manifest. It is a fixed evidence-envelope writer, not a general serializer: it exports no function, accepts no schema name, is unavailable to the production module/candidate, and contains no reusable candidate-result construction. It uses `JsonObject`/`JsonArray` directly. After every JSON write it independently reopens the bytes with `JsonDocument`, proves every declared array property has `JsonValueKind.Array` before checking member count, proves the closed key set, and only then permits the RED manifest to close.

`provenance_basis` is exactly `BOUND_PARAMETERS_RECONSTRUCTED_IN_FROZEN_ORDER`. This proves the script-level invocation frozen by this plan; it does not claim a raw OS command line. `raw_command_line_persisted` is always `false`. Executable and script identities include tokenized path, byte count, and uppercase SHA-256; the executable identity additionally records the current PowerShell version.

The frozen tokenized argument vectors are:

```text
RED:
[-NoLogo,-NoProfile,-File,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.Tests.ps1,-Mode,RED_FROZEN_CYCLE5_WITNESS,-SchemaPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-schemas.json,-SerializationModulePath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-serialization.psm1,-WitnessPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-cycle5-defect-witness.json,-Cycle5TestsPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-5-diagnostic.Tests.ps1,-Cycle5CandidatePath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-5-diagnostic.ps1,-Cycle5RedRoot,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-5-diagnostic-tests/red,-Cycle5GreenRoot,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-5-diagnostic-tests/green,-CandidatePath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.ps1,-ReceiptRoot,<RECEIPT_ROOT>]

GREEN:
[-NoLogo,-NoProfile,-File,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.Tests.ps1,-Mode,GREEN_NEW_CYCLE6_CANDIDATE,-SchemaPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-schemas.json,-SerializationModulePath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-serialization.psm1,-WitnessPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-cycle5-defect-witness.json,-CandidatePath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.ps1,-ReceiptRoot,<RECEIPT_ROOT>]

DIAGNOSTIC:
[-NoLogo,-NoProfile,-File,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-closure.ps1,-Mode,ISOLATED_RECEIPT_DIAGNOSTIC,-SchemaPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-schemas.json,-SerializationModulePath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-receipt-serialization.psm1,-WitnessPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/36-x01-recovery-cycle-6-cycle5-defect-witness.json,-ApprovedGreenManifestPath,<WORKSPACE>/docs/agent-engineering-course/articles/36-dsh-cost-compaction-trace-cancellation-recovery/experiments/preflight/36-x01-recovery-cycle-6-receipt-closure-tests/green/manifest.json,-ReceiptRoot,<RECEIPT_ROOT>,-AttemptIdentity,36-X01-RECOVERY-CYCLE-6-RECEIPT-CLOSURE-ATTEMPT-1]
```

The implementation persists these normalized full tokens exactly. No shell-joined display command is authoritative. The Reviewer compares every array member ordinally with the frozen vector.

## 5. Frozen Cycle 5 defect witness and RED

The witness is created once from read-only source inspection and then frozen before RED. It stores no arbitrary source content. Its `source_identities` array contains the five exact Cycle 5 plan/tests/candidate/RED-manifest/GREEN-manifest identities in Global constraints. Its `observations` array contains exactly:

1. `C5-W1`: Tests line `153`, `COUNT_ONLY_CANNOT_DISTINGUISH_NULL_FROM_EMPTY_ARRAY`, with existing GREEN `phase-summary.json /expected_failed_assertions` observed as token kind `Null`;
2. `C5-W2`: Tests line `183`, `INLINE_CONDITIONAL_EMPTY_OUTPUT_UNROLLED_BEFORE_SERIALIZATION`, correlated with that same persisted `Null` token;
3. `C5-W3`: both frozen RED and GREEN roots lack `exact-command.json`, disposition `EXACT_COMMAND_PROVENANCE_ABSENT`.

Its `expected_red_failures` array is exactly `C5_EXPLICIT_EMPTY_ARRAY_SERIALIZED_AS_NULL`, `C5_TEST_ACCEPTS_NULL_FOR_EMPTY_ARRAY`, `C5_RED_GREEN_EXACT_COMMAND_RECEIPTS_ABSENT`, and `C6_SCHEMA_FIRST_PRODUCTION_CLOSURE_ABSENT`. Both `cycle5_tests_executed` and `cycle5_candidate_executed` are `false`; `mutation_count` is `0`.

RED runs the new Cycle 6 Tests file once in `RED_FROZEN_CYCLE5_WITNESS`. It reads the schema specification, witness, two frozen Cycle 5 source files, and two frozen receipt roots only. It verifies the hashes, exact source-line structures, actual GREEN JSON token kind, absence of both Cycle 5 exact-command files, and absence of both new production paths. It must not import the not-yet-created production module and must not dot-source, import, call, or execute either Cycle 5 script. The Tests-local bootstrap closes the RED evidence tree and independently validates its persisted token kinds with `JsonDocument`.

RED succeeds only as `EXPECTED_FAIL / logical_exit_code 41` with the exact four failures above and `subject_executed: false`. `red/identities.json` records the serializer and new candidate with closed `artifact-state` objects whose state is `ABSENT`, byte count is `0`, and SHA is the exact sentinel `ABSENT`. Any production module/candidate already present, drift, extra/missing failure, bootstrap schema violation, or old-code execution is terminal and consumes RED.

## 6. Wholly new candidate and GREEN

Only after expected RED closes may the Revision Worker create `36-x01-recovery-cycle-6-receipt-serialization.psm1`, implement the interfaces already frozen in section 3.2, and run no standalone self-check. The same Tests file owns all executable positive/negative checks during GREEN. After the module bytes are frozen, the Worker creates `36-x01-recovery-cycle-6-receipt-closure.ps1`. It has one mode only: `ISOLATED_RECEIPT_DIAGNOSTIC`. It imports only the Cycle 6 serialization module, reads only the Cycle 6 schema/witness and optional approved GREEN manifest, and writes only its supplied receipt root.

The candidate must not contain a callable path to the launcher, Cycle 5 scripts, Corepack, pnpm, Vitest, Git, focused target, collector, DSH, network, package installation, `Start-Process`, `System.Diagnostics.Process`, WMI/CIM process APIs, or the pinned fixture. It must not expose `PREVALIDATE`, `VALIDATE_FOCUSED_PRESTATE`, `EXECUTE_FOCUSED`, or any formal-experiment mode.

GREEN runs the unchanged Cycle 6 Tests file once in `GREEN_NEW_CYCLE6_CANDIDATE`. The plan/schema/witness/test hashes captured before RED must match; the production module and candidate hashes captured only after RED must match their current bytes. GREEN invokes the new candidate exactly once against `green/candidate-sample/`; that invocation is the sole member of the top exact-command `nested_invocations` array. GREEN passes only when:

- the in-memory negative probe rejects `null` for every array-declared field and the positive probe accepts `[]`;
- persisted candidate fields `expected_failed_assertions`, `actual_failed_assertions`, and an intentionally empty `input_receipt_identities` are each `JsonValueKind.Array` with count `0`;
- both top and candidate `ordered_tokenized_argv` fields are JSON arrays with ordinal contents and counts equal to their frozen vectors;
- all closed schemas reject missing, unknown, or wrong-kind properties;
- source/AST inspection finds none of the forbidden modes, commands, APIs, or Cycle 5 execution references;
- candidate result is `PASS / SCHEMA_FIRST_RECEIPT_CLOSED`, every prohibited execution/read/write count is `0`, cleanup passes, and both manifests recompute exactly.

Any GREEN failure is terminal. The candidate, schema, module, witness, or Tests file may not be edited after GREEN starts; GREEN may not be rerun.

## 7. One isolated diagnostic

Only after GREEN PASS may the Revision Worker invoke the new candidate once into the frozen Cycle 6 `attempt-1` root. Before substantive logic it verifies the current schema/module/witness/candidate identities and the complete GREEN manifest. It reads no Cycle 5 script during this phase; the normalized witness is its only Cycle 5-derived input.

The diagnostic creates no temporary root, inspects no process, spawns no child process, reads or writes no pinned fixture, and performs no network or Git action. It writes the exact eight files below, validates all declared JSON array token kinds after persistence, closes its manifest, and stops. Its only successful disposition is `SCHEMA_FIRST_RECEIPT_CLOSED`; its claim ceiling remains receipt-only. Any non-PASS result, unsafe text, identity drift, manifest mismatch, or pre-existing root is terminal and consumes the diagnostic budget.

## 8. Exact receipt trees

The RED root is written only by the Tests-local bootstrap and contains exactly:

```text
red/
  exact-command.json
  identities.json
  phase-summary.json
  schema-validation.json
  stdout.txt
  stderr.txt
  cleanup-receipt.json
  manifest.json
```

The GREEN top receipts and nested candidate sample are written through the post-RED production module and contain exactly:

```text
green/
  exact-command.json
  identities.json
  phase-summary.json
  schema-validation.json
  stdout.txt
  stderr.txt
  cleanup-receipt.json
  candidate-sample/
    exact-command.json
    source-input-identities.json
    diagnostic-result.json
    schema-validation.json
    stdout.txt
    stderr.txt
    cleanup-receipt.json
    manifest.json
  manifest.json
```

The isolated diagnostic root is written through the same frozen post-RED production module and contains exactly:

```text
attempt-1/
  exact-command.json
  source-input-identities.json
  diagnostic-result.json
  schema-validation.json
  stdout.txt
  stderr.txt
  cleanup-receipt.json
  manifest.json
```

RED and diagnostic manifests each enumerate exactly seven files. The nested GREEN candidate manifest enumerates exactly seven files. The GREEN top manifest recursively enumerates all top files and all eight nested files, excludes only the top `manifest.json`, and therefore has `file_count: 15`. Every `files` property is a JSON `Array`; sorted `relative_path`, exact byte count, and uppercase SHA-256 are mandatory.

## 9. Identity and hash closure

- Before RED: freeze plan, schema, witness, and Tests byte counts/SHA-256 in `red/identities.json`; record the production serializer and candidate as exact `ABSENT` artifact states.
- RED identity also includes all five immutable Cycle 5 identities and the frozen launcher/harness/collector identities.
- Before GREEN: verify all pre-RED source identities and the complete bootstrap-written RED manifest; freeze the newly created production serializer and candidate identities. `green/identities.json` includes the RED manifest identity and both post-RED source identities.
- Before diagnostic: verify all Cycle 6 source identities and the complete GREEN top and nested manifests. `source-input-identities.json` includes the GREEN top manifest identity.
- Each phase refuses a pre-existing receipt root. Root creation consumes that phase budget even if later work fails.
- Every source and receipt created in a completed phase becomes immutable. No missing receipt may be backfilled and no manifest may be regenerated after the phase returns.
- The fresh Reviewer recomputes every protected Cycle 5 hash, every Cycle 6 source hash, every manifest entry, and the absence of Cycle 5 diagnostic `attempt-1` independently.

## 10. Sanitization and no-touch boundary

Before serialization, replace known absolute roots longest-first with exactly `<WORKSPACE>`, `<RECEIPT_ROOT>`, `<TEMP_ROOT>`, `<USER_PROFILE>`, and `<POWERSHELL_HOST>`. Repository source identities use repository-relative tokens. Persist no environment values, home listing, credential-store output, authentication header, cookie, private key, arbitrary source excerpt, raw command-line string, or URI user-info.

Scan every persisted string value for case-insensitive `authorization`, `bearer`, `password`, `secret`, `api_key`, `private_key`, URI user-info, Windows absolute paths, UNC paths, and an un-tokenized workspace/user/temp root. A hit changes the current phase to terminal FAIL; unsafe text remains only in memory and the durable value is exactly `UNSAFE_TEXT_NOT_PERSISTED`. The receipt records only replacement count, post-sanitization count, raw-absolute-path count, and `unsafe_text_persisted: false`.

Execution counters are a closed object and distinguish the allowed phase-host invocation from forbidden reach: `phase_host_invocation` is `1`; `spawned_child_process`, `process_api`, `original_launcher`, `cycle5_tests`, `cycle5_candidate`, `corepack`, `pnpm`, `vitest`, `focused_target`, `collector`, `dsh_command`, `network`, `pinned_fixture_read`, and `pinned_fixture_write` are all `0`. GREEN's in-process call to the new Cycle 6 candidate is recorded separately as `cycle6_candidate: 1`.

## 11. Frozen one-shot budgets

| Operation | Maximum | Timeout | Failure rule |
|---|---:|---:|---|
| Schema catalog creation | 1 | n/a | non-production spec; immutable after pre-RED identity |
| Defect witness creation | 1 | n/a | immutable after pre-RED identity |
| Tests plus fixed RED bootstrap creation | 1 | n/a | immutable before RED; byte-identical in RED and GREEN |
| RED invocation | 1 | 60 seconds | no retry; downstream phases forfeited on unexpected result |
| Production serialization module creation | 1 | n/a | only after expected RED; no standalone self-check; immutable once GREEN starts |
| New candidate creation | 1 | n/a | only after expected RED; immutable once GREEN starts |
| GREEN invocation | 1 | 60 seconds | no retry; diagnostic forfeited on failure |
| GREEN nested Cycle 6 candidate invocation | 1 | within GREEN 60 seconds | in-process only; no child process |
| Isolated diagnostic invocation | 1 | 60 seconds | no attempt-2 |
| Fresh Reviewer | 1 | read-only | always final; no repair loop |
| Cycle 5 source execution | 0 | n/a | always forbidden |
| Original launcher/Corepack/pnpm/Vitest/focused target/collector/DSH/network | 0 each | n/a | always forbidden |
| Pinned fixture reads/writes | 0 / 0 | n/a | always forbidden |
| Formal Article 36 experiment | 0 | n/a | always forbidden |

An invocation consumes its budget when its receipt root is created or its script begins, whichever occurs first. Timeout, interruption, incomplete receipt, unexpected result, or cleanup failure never grants a retry.

## 12. Fresh Reviewer and unconditional stop

After Revision ends—successful full path or earlier terminal failure—discard its context and run one fresh read-only Reviewer at `REVIEW_RECHECK`. Reviewer writes are `0` and must verify:

1. actual new paths are a subset of the eight-path allowlist appropriate to the reached phase; there are no modifications, deletions, renames, state writes, Cycle 5 backfills, or Article 37+ assets;
2. all frozen DSH/launcher/harness/collector/Cycle 5 identities are exact and Cycle 5 diagnostic `attempt-1` remains absent;
3. budgets were not exceeded and every unstarted downstream phase is truthfully absent;
4. RED used only its test-local bootstrap, inspected but did not execute Cycle 5 code, proved the production serializer/candidate were absent, and produced the exact four expected failures;
5. the production serializer and candidate were created only after expected RED; GREEN used the same pre-RED plan/schema/witness/test bytes plus the frozen post-RED module/candidate bytes, executed only the new candidate once, and proved JSON token kind `Array` before count for every declared array path;
6. every reached phase contains its exact-command receipt and every tokenized argv equals the expanded frozen vector ordinally;
7. schema catalog, closed-object rejection, sanitization, no-touch counters, cleanup, receipt tree, nested manifest, and top manifests all recompute exactly;
8. the diagnostic, if reached, touched no fixture, process API, child process, network, Git, launcher, old candidate, package manager, focused target, collector, or DSH path;
9. claim ceiling and authorization fields remain receipt-only, `historical_cycle4_root_cause: NOT_PROVABLE`, `new_launcher_attempt_authorized: false`, and `lab_execute_authorized: false`.

Reviewer verdicts are closed:

- `PASS / CYCLE6_RECEIPT_CLOSURE_CLOSED`: `status: PASS`, `gate_completed: true`, `next_allowed_gate: NONE`, `blocker: HUMAN_DECISION_REQUIRED`;
- `FAIL / CYCLE6_RECEIPT_CLOSURE_NOT_CLOSED`: `status: FAIL`, `gate_completed: true`, `next_allowed_gate: NONE`, `blocker: HUMAN_DECISION_REQUIRED`.

After this fresh recheck, PASS and FAIL both stop. There is no automatic patch, rerun, second diagnostic, launcher attempt, state update, Evidence Merge, `LAB_EXECUTE`, or later Gate. A new external Human Resume and a new frozen design would be required for any further action.

## 13. Worker Result envelopes

Every Revision and Reviewer return must have the closed 11-field `worker_result` envelope: `role`, `article`, `gate`, `execution_type`, `status`, `artifacts_created`, `artifacts_modified`, `gate_completed`, `next_allowed_gate`, `blocker`, `notes`. Empty arrays are explicit `[]`; wrappers, aliases, extra fields, missing fields, or wrong types are invalid.

On the complete Revision path, the Revision Worker returns `PASS / gate_completed true / next_allowed_gate REVIEW_RECHECK / blocker NONE`, with the exact eight created paths and no modified paths. On early terminal failure it returns `BLOCKED / gate_completed false / next_allowed_gate NONE / blocker HUMAN_DECISION_REQUIRED`, lists only paths actually created, and does not consume downstream budgets. The Reviewer always returns `next_allowed_gate NONE / blocker HUMAN_DECISION_REQUIRED` as frozen above.

## 14. Design Gate result

This plan freezes an independent schema-first receipt closure and preserves all Cycle 5 failures unchanged. Design Gate result is `PASS -> REVISION`, but only the exact allowlist and budgets above are authorized. This Researcher stops after creating this plan and returns one closed 11-field envelope; it executes none of the later phase commands.
