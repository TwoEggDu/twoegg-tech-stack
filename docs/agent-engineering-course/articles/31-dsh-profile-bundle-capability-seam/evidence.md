# Article 31 Evidence

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## Evidence summary

- Frozen DSH revision：`dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Source closure：`Profile/Bundle/Patch -> Effective Config -> FS Capability Definition/Provider/Consumer`
- Product receipts：`headless 89 rows / web 144 rows / overlay 146 rows, 145 unique ids`
- Runtime closure：`LocalFileSystem -> ToolFs exact write/readback`，不是 shipped `SandboxedFileSystem` activation
- Negative evidence：`repo overlay duplicate activation exit1`；`missing overlay ENOENT exit1`
- Owner tests：`config dump 6/6`；`FS integration 1/1 targeted`；`permission protocol 4/4 targeted with fake FS`
- Claim count：`15`
- Final status：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`
- Excluded：`real model/provider/network/token/cost / shipped SandboxedFS runtime / OS sandbox / BuildPilot implementation`

### Evidence 31-E01｜Pinned identity and final fixture integrity

- Claim ID: `31-C01`
- Claim: `Article 31 source and Lab evidence is bounded to the official frozen revision, and the external fixture remained clean.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Type: `fresh git identity and before/after integrity checks`
- Repository: `https://github.com/deepseek-ai/deepseek-harness`
- Commit / Tag: `cd5ef8148158c3a752a658978873241fdf8e2bbc / dsh-v0.1.2-alpha.1`
- Trace: `experiments/effective-config-diff.md Probe A and Final integrity check`
- Observation: `HEAD and tag target matched; git status --short had 0 rows; diff and cached diff were empty before and after experiments.`
- Proves: `fixed version and clean input/output boundary`
- Does Not Prove: `any configuration or capability behavior by itself`
- Limitations: `point-in-time local integrity checks`
- Course Decision: `BOUND EVERY DSH CLAIM TO THIS REVISION`

### Evidence 31-E02｜Distributed Profile, Bundle and patch validation

- Claim ID: `31-C02`
- Claim: `Profile, Bundle and patch validation is distributed across manifest checks, patch parsing and activation-time plugin schemas; named bundle or CLI overlays fail loud when absent.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + PRODUCT_NEGATIVE`
- Source Type: `exact source map plus missing overlay observation`
- Source Location: `packages/boot/app-boot/src/profile.ts:52-91,121-158,684-710,761-843; packages/boot/app-boot/src/index.ts:271-307; vendor/include/src/index.ts:10-27,173-208`
- Call Path: `profile package.json -> dsh.profile.bundles -> bundle package.json.dsh.bundle.patch -> loadOverlayPatches -> entry-list dialect -> activation Config schema`
- Experiment: `Probe B materialization; Probe E missing CLI overlay`
- Observation: `Known Profiles materialized valid ordered bundle tuples; missing named overlay exited 1 with labelled ENOENT and empty stdout.`
- Counter-evidence: `There is no single Zod-style profile schema; TypeScript interfaces alone are not runtime validation.`
- Proves: `selected schema boundaries and fatal named-file behavior`
- Does Not Prove: `every plugin Config is valid before activation`
- Course Decision: `KEEP DECLARATION AND ACTIVATION VALIDATION SEPARATE`

### Evidence 31-E03｜Actual precedence and dump subset

- Claim ID: `31-C03`
- Claim: `Boot applies ordered bundles, Profile user, home user, argv overlays and then telemetry hard-disable; dump composes the static subset and default dump keeps bundle layers only.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + EFFECTIVE_CONFIG_DUMP`
- Source Type: `exact launcher/boot path and provenance receipts`
- Source Location: `apps/cli/src/args.ts:58-60,85-102,125-134; profile-boot.ts:118-174,209-272; dump-config.ts:20-55`
- Call Path: `parseDshArgs -> loadProfile -> bundlePatches -> profile.patches -> homePatches -> CLI overlays -> resolveTelemetryPatch -> boot`
- Experiment: `Probe C; web effective/default comparison`
- Observation: `Dump provenance showed base then mode bundle; fresh empty user layers made Web default/effective byte-identical.`
- Counter-evidence: `Telemetry hard patch is boot-only; byte equality in one empty Home is not general equality.`
- Proves: `layer order and command-view boundary`
- Does Not Prove: `that each patch matched, expressions evaluated or plugins activated`
- Course Decision: `RECORD LAYER SOURCE AND COMMAND MODE IN RECEIPTS`

### Evidence 31-E04｜Replacement, warning and duplicate semantics

- Claim ID: `31-C04`
- Claim: `Patches replace top-level fields on matched unique ids, config is not deep-merged, miss/name mismatch warns and skips, and duplicate insert does not deduplicate structural rows.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + OWNER_TEST + COUNTEREXAMPLE`
- Source Type: `pure algorithm, exact owner suite and repo overlay collision`
- Source Location: `vendor/include/src/index.ts:58-112; packages/boot/app-boot/tests/config-dump.spec.ts`
- Call Path: `clone -> id index -> insert/buildMap or id/name match -> top-level assignment -> detached rows`
- Experiment: `Probe D and Probe G`
- Observation: `Owner suite passed 6/6. Overlay replacement changed the whole webserver config; duplicate cordis-host-runner remained, yielding 146 rows / 145 unique ids.`
- Counter-evidence: `Dump exit0 did not mean duplicate was valid; Loader activation rejected it.`
- Proves: `selected composition/conflict rules and a real drift instance`
- Does Not Prove: `unconditional last-write-wins or structural validation in dump`
- Course Decision: `SURFACE SKIPS, WHOLE REPLACEMENT AND DUPLICATES`

### Evidence 31-E05｜Built Effective Config receipts and dump ceiling

- Claim ID: `31-C05`
- Claim: `The built CLI produced non-empty provenance-bearing Headless and Web Effective Config receipts using the boot patch algorithm, while remaining boot-free and leaving !!js unevaluated.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PRODUCT_ARTIFACT + OWNER_TEST`
- Source Type: `built CLI observation plus algorithm owner tests`
- Source Location: `packages/boot/app-boot/src/index.ts:355-474; apps/cli/src/dump-config.ts:20-55`
- Call Path: `empty cordis.yml -> renderConfigDump -> prefix applyEntryPatches snapshots -> provenance grouping -> YAML stdout`
- Experiment: `Probe C and Probe G`
- Observation: `Headless: exit0, 11,227 bytes, 89 rows/89 ids, SHA 7B00D284956107355C44629B861C1754A570835AE04F44F9AA15E9586ECA5298. Web: exit0, 16,558 bytes, 144/144, SHA 0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A. Owner tests: 6/6.`
- Counter-evidence: `sandbox-policy and approval configs remained literal !!js expressions.`
- Proves: `pinned effective row lists and provenance`
- Does Not Prove: `expression values, Provider construction, dependency settlement or operation`
- Course Decision: `DUMP IS A CONFIG RECEIPT, NOT ACTIVATION PROOF`

### Evidence 31-E06｜Configured/effective is not active

- Claim ID: `31-C06`
- Claim: `A dump can succeed with a structurally duplicate id that Loader activation rejects, directly separating effective config from active runtime.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PRODUCT_COUNTEREVIDENCE`
- Source Type: `same overlay through dump and bounded activation paths`
- Source: `apps/cli/config/examples/cordis/cordis.yml`
- Overlay Identity: `781 bytes; SHA 62F0D905D430F7A1A517125AAE8EE5786EABCC9B77FA362AEA16A9104A0EFD31`
- Experiment: `Probe D`
- Observation: `Overlay dump exit0, 16,827 bytes, 146 rows/145 ids, SHA 679CC5ED39C53FDBB2D6A57014DF4486FA274A0E3250D98AF4223DED6C6D76E9; activation via --help exited1 with duplicate loader entry id: cordis-host-runner.`
- Counter-evidence: `Provenance rendered the duplicate rather than rejecting it.`
- Proves: `selected repo-owned overlay drift and dump/activation separation`
- Does Not Prove: `all overlays drift or all duplicate detection occurs at the same phase`
- Course Decision: `PAIR EFFECTIVE RECEIPT WITH ACTIVATION RESULT`

### Evidence 31-E07｜Shared Base and surface split

- Claim ID: `31-C07`
- Claim: `Headless and Web share the Base capability stack and 87 unique ids, then diverge in their surface bundles.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_CONFIG + EFFECTIVE_CONFIG_DIFF`
- Source Type: `shipped templates/patches plus built dump row-set comparison`
- Source Location: `packages/boot/app-boot/src/profile.ts:137-163; packages/bundle/base|headless|web-app/cordis.patch.yml`
- Experiment: `Probe B and Probe C`
- Observation: `Headless materialized base+headless and 89 ids; Web materialized base+web-app and 144 ids; intersection was 87 ids. Both included sandbox, sandbox-policy, approval, tool-bash, tool-pwsh, tool-fs and fs-sandbox.`
- Counter-evidence: `Shared rows do not prove identical activation, Session policy or operation behavior.`
- Proves: `shared configured capability core and structural surface fork`
- Does Not Prove: `full runtime equivalence`
- Course Decision: `SHARE CORE, DIFF SURFACE EXPLICITLY`

### Evidence 31-E08｜Current Headless has no Web Host composition

- Claim ID: `31-C08`
- Claim: `Current Headless is a command-line one-shot runner over Base; Web owns the repository's Host/API/UI composition.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + EFFECTIVE_CONFIG_DIFF`
- Source Type: `current template migration logic, surface patches and observed row absence/presence`
- Source Location: `profile.ts:137-163,714-743; bundle/headless/cordis.patch.yml:1-30; bundle/web-app/cordis.patch.yml:39-137`
- Experiment: `Probe C and Probe F`
- Observation: `Only Headless had headless-startup/headless-runner. Web had 57 ids absent from Headless, including web-startup, webserver, cordis-host-runner, web-runtime, controllers and UI rows. Both unmodified --help smokes exited0 without opening a listener.`
- Counter-evidence: `Historical [base,web-app,headless] tuple is migration input normalized to current [base,headless], not current topology.`
- Proves: `selected current Host/App composition split`
- Does Not Prove: `a long-running Web Host or Agent turn succeeded`
- Course Decision: `HOST IS A SURFACE COMPOSITION, NOT EVERY PROCESS`

### Evidence 31-E09｜Shipped FS source seam

- Claim ID: `31-C09`
- Claim: `The shipped FS topology is FileSystem service definition -> SandboxedFileSystem provider -> ToolFs consumer.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + PINNED_CONFIG`
- Source Type: `closed source/provider/consumer arrows`
- Source Location: `packages/fs/fs/src/index.ts:86-240; fs-local/src/index.ts:64-238; fs-sandbox/src/index.ts:55-135; tool-fs/src/index.ts:22,54-75; read-target.ts:24; read.ts:146; write.ts:62-122; edit.ts:76-135; bundle/base/cordis.patch.yml:266-267,491-494`
- Call Path: `FileSystem super(ctx,'fs') -> dsh-base fs-sandbox row -> SandboxedFileSystem inject sandboxPolicy/publish ctx.fs -> tool-fs inject tools/fs/systemPrompt -> ctx.fs read/write/edit`
- Observation: `Both profile dumps contained enabled fs-sandbox and tool-fs rows; source closes definition, implementation inheritance and consumer calls.`
- Counter-evidence: `dsh-base does not separately mount fs-local; LocalFileSystem mechanics are inherited by the shipped SandboxedFileSystem.`
- Proves: `shipped source/config topology`
- Does Not Prove: `SandboxedFileSystem constructed, ctx.fs active, ToolFs registered or an operation ran in those Profile probes`
- Course Decision: `SOURCE_CONFIRMED SHIPPED SEAM ONLY`

### Evidence 31-E10｜Executed Local FS -> ToolFs operation

- Claim ID: `31-C10`
- Claim: `A targeted owner integration test executed a real LocalFileSystem provider through ToolFs and read back exactly the requested bytes.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `TEST_FIXTURE_RUNTIME`
- Source Type: `real local filesystem provider integration test`
- Source Location: `packages/fs/tool-fs/tests/integration.spec.ts`
- Call Path: `LocalFileSystem + ToolRuntime + FsPolicy + ToolFs -> ctx.tools.execute(write) -> disk write -> exact byte readback`
- Experiment: `Probe H`
- Observation: `Exit0; 1 passed / 32 skipped; fresh temp directory created and cleaned.`
- Counter-evidence: `The test provider is LocalFileSystem, not the shipped SandboxedFileSystem row.`
- Proves: `minimal real FS Provider -> policy -> Tool Consumer operation without a model`
- Does Not Prove: `shipped Profile activation, Agent turn, OS confinement or production filesystem safety`
- Course Decision: `RUNTIME_CONFIRMED LOCAL PROVIDER SEAM; DO NOT RELABEL`

### Evidence 31-E11｜Permission protocol with fake provider

- Claim ID: `31-C11`
- Claim: `Targeted tests confirm default, standing override, approved escalation and rejected escalation handoff through policy/approval/ToolFs, but use a fake FS provider.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `TEST_FIXTURE_RUNTIME`
- Source Type: `real policy/consumer protocol around SandboxingFakeFs`
- Source Location: `packages/fs/tool-fs/tests/tools.spec.ts`
- Call Path: `Session policy -> SandboxPolicyService/approval -> FsSandboxController -> ToolFs -> SandboxingFakeFs policy recorder`
- Experiment: `Probe I`
- Observation: `Plain default 1/1; three override/escalation cases 3/3. Observed workspace-write+Session root, read-only override, danger-full-access approved escalation, and fail-closed rejection with no fake-provider mutation.`
- Counter-evidence: `No real approval UI and no OS-enforcing provider participated.`
- Proves: `policy selection and Consumer handoff contract in deterministic fixture`
- Does Not Prove: `Windows ACL, Landlock, Seatbelt, shipped SandboxedFileSystem enforcement or security completeness`
- Course Decision: `LABEL PERMISSION EVIDENCE TEST_FIXTURE ONLY`

### Evidence 31-E12｜Recovery view, local drift and missing overlay

- Claim ID: `31-C12`
- Claim: `Default dump is a bundle-only recovery view; user/CLI layers are separate drift inputs, and a missing named CLI overlay fails nonzero.`
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE + PRODUCT_ARTIFACT + NEGATIVE`
- Source Type: `defaultOnly branch, isolated Profile files and ENOENT probe`
- Source Location: `apps/cli/src/args.ts:85-102; dump-config.ts:20-55; profile-boot.ts:118-174`
- Experiment: `Probe B, Probe C, Probe E`
- Observation: `Fresh Web default/effective dumps were byte-identical only because profile patch was empty and home patch absent. Missing named overlay exited1 with labelled ENOENT and empty stdout.`
- Counter-evidence: `--dump-default-config intentionally hides local user layers; it cannot audit their drift by itself.`
- Proves: `recovery/effective distinction and named overlay failure`
- Does Not Prove: `a user's real Home is clean or secure`
- Course Decision: `COMPARE DEFAULT WITH EFFECTIVE AND RECORD LOCAL SOURCES`

### Evidence 31-E13｜BuildPilot explicit Capability Set

- Claim ID: `31-C13`
- Claim: `BuildPilot should adopt an explicit Capability Set with provider, consumer, permission, provenance and activation receipts.`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course transfer over confirmed DSH boundaries`
- Evidence Basis: `31-E05 through 31-E11`
- Observation: `Dump/activation divergence and provider identity matter independently; one capability name is insufficient.`
- Proves: `N/A — design proposal`
- Does Not Prove: `BuildPilot ADR, code or runtime`
- Required Next Evidence: `Part VII authorization and capability acceptance tests`
- Course Decision: `ADOPT`

### Evidence 31-E14｜BuildPilot read-only Profile first

- Claim ID: `31-C14`
- Claim: `BuildPilot should make a read-only Profile its first contract, with write capabilities absent or denied by default.`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `least-authority transfer decision`
- Evidence Basis: `31-E09 through 31-E12`
- Observation: `Permission source expression, Session policy and provider enforcement are separate planes; Prompt text cannot enforce filesystem authority.`
- Proves: `N/A — design proposal`
- Does Not Prove: `implemented sandbox or production security guarantee`
- Required Next Evidence: `future negative write tests and explicit provider receipt`
- Course Decision: `ADOPT`

### Evidence 31-E15｜Defer arbitrary composition machinery

- Claim ID: `31-C15`
- Claim: `BuildPilot should defer arbitrary patch layering, live reload, runtime provider replacement and multi-Host composition until requirements justify them.`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `scope and complexity decision`
- Evidence Basis: `31-E03, 31-E04, 31-E06, 31-E12`
- Observation: `Whole replacement, duplicate insert, local drift and dump/activation divergence are demonstrated costs; BuildPilot has no proven need for DSH-equivalent machinery.`
- Proves: `N/A — explicit defer decision`
- Does Not Prove: `those mechanisms are universally wrong or permanently rejected`
- Required Next Evidence: `real second-Host, live reload or provider-rebinding requirement plus tests`
- Course Decision: `DEFER`

## Traceability matrix

| Claim group | Source artifact | Runtime/artifact evidence | Final label |
|---|---|---|---|
| Schema/order/conflict | `repository-map.md` §§1-3; `call-path.md` A-B | owner dump tests 6/6; missing overlay exit1 | `SOURCE + TEST/NEGATIVE CONFIRMED` |
| Effective config | `call-path.md` A-C | Headless/Web dumps and exact hashes | `EFFECTIVE_CONFIG_DUMP_CONFIRMED` |
| Dump vs activation | `call-path.md` E | overlay dump exit0; duplicate activation exit1 | `COUNTEREVIDENCE CONFIRMED` |
| Host split | `repository-map.md` §4; `call-path.md` C | 87 shared ids; Headless-only/Web-only sets | `SOURCE + CONFIG DIFF CONFIRMED` |
| Shipped FS seam | `repository-map.md` §5; `call-path.md` D | rows present only | `SOURCE/CONFIG CONFIRMED` |
| Executed FS seam | `call-path.md` D as contract | LocalFileSystem integration 1/1 | `TEST RUNTIME CONFIRMED, LOCAL PROVIDER` |
| Permission | sandbox policy source paths | 4/4 targeted with SandboxingFakeFs | `TEST_FIXTURE_RUNTIME_CONFIRMED` |
| BuildPilot | merged course decision | none in Part VI | `PROPOSAL` |

## Retained failures and boundary notes

1. Repo-owned overlay duplicate id and activation exit1 are product counter-evidence, not harness noise.
2. Missing overlay ENOENT exit1 is the expected named-file negative and remains in evidence.
3. A first Corepack probe from the wrong cwd hit registry `EACCES`; the corrected fixture-local command returned pnpm `11.7.0` exit0.
4. A PowerShell `$home` collision and four `Start-Process` argument-flattening failures are retained as orchestration mistakes; corrected direct argument arrays produced the counted results.
5. `!!js` row source is not selected permission. Fake policy handoff is not OS enforcement. LocalFileSystem runtime is not shipped SandboxedFileSystem activation.

## Evidence Merge recommendation

`EVIDENCE_MERGE PASS / OUTLINE ELIGIBLE`。

15 个 Claim 与 15 张 Evidence Card 一一对应：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。Source closure、两份 Effective Config receipts、overlay duplicate/activation rejection、missing overlay、6/6 owner tests、shipped FS source seam、Local FS runtime seam 与 permission fake boundary 都已合并，且证据层级没有互相升级。Next allowed gate：`OUTLINE`。
