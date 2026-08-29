# Article 28 Source Map

Status: `SOURCE_MAP COMPLETE / SOURCE_CONFIRMED + PARTIAL / RUNTIME PENDING`

## 1. Frozen source identity

| Field | Frozen value |
|---|---|
| Repository | `https://github.com/deepseek-ai/deepseek-harness` |
| Tag | `dsh-v0.1.2-alpha.1` |
| Full commit | `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| External fixture | `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` |
| Verified state | `HEAD == tag commit`; `origin` is the official repository; tracked worktree is clean |
| Verified at | `2026-08-30 / Asia/Shanghai` |
| Evidence ceiling | `PINNED_SOURCE / SOURCE_CONFIRMED` for identity only |

Every path below is relative to that fixture and valid only at the frozen commit. Article 29—37 must repeat the identity and cleanliness checks before reusing a seed. A different revision requires an explicit baseline migration; it must not be spliced into this map.

- **Counter-evidence searched:** latest-main, another checkout, abbreviated SHA and tag-name-only identification were rejected as substitutes; the full tag target, fixture HEAD, origin URL and clean tracked state were checked independently.
- **Proves:** the exact Git object inspected by this source map.
- **Does not prove:** dependency, build, test or application behavior.
- **Limitations:** ignored/generated files can exist without appearing in tracked-worktree status and must be classified separately.
- **DSH verification:** `SOURCE_CONFIRMED` for revision identity; runtime is not applicable.

## 2. Repository and source boundary

### 2.1 Included as primary pinned-source evidence

| Plane | Included paths | Source role |
|---|---|---|
| Repository instructions | `AGENTS.md`, `packages/AGENTS.md`, subtree `AGENTS.md` | Current rules, supported application boundary, package/lifecycle constraints |
| Official repository docs | `README.md`, `SAFETY.md`, `docs/architecture.md`, `docs/development.md`, owning package `README.md` | Official claims and supported entry descriptions at the pinned revision |
| Application source | `apps/cli/src/`, `apps/cli/package.json`, shipped profile/config assets | Source launcher, argument dispatch, profile boot and installed-bin declaration |
| Product source | `packages/*/*/src/`, owned `cordis*.yml`, package manifests | Symbols, registrations, composition and call-path anchors |
| Build and gate source | root `package.json`, `scripts/`, `vitest*.config.ts`, `tsconfig*.json`, `tsdown.config.ts` | Exact command dispatch and source/artifact-plane ownership |
| Tests and fixtures | `packages/*/*/tests/`, `apps/cli/tests/`, `snapshots/` | Candidate executable evidence and counterexamples; passing them is still not application runtime evidence |
| Vendored Cordis | `vendor/` plus `vendor/README.md` manifest | Consult only when a DSH path crosses into vendored Cordis; record the vendored upstream revision and local modifications separately |

### 2.2 Secondary or generated material

- Generated reference pages and regions such as `docs/module-graph.md`, catalogs, and `type-equiv` projections may locate an owner, but their source/generator must be checked before they support a source Claim.
- Checked-in snapshots are recorded fixtures. They can prove expected replay input/output at this revision; they do not prove a fresh provider call or current external service behavior.
- Agent Notes under `.agents/notes/implemented/` may explain the pinned design decision. Archived notes are historical evidence, not current authority.
- `packages/experimental/` is a private prototype area excluded from official releases by the pinned root instructions. It must be labeled `EXPERIMENTAL` and cannot establish shipped/default behavior.

### 2.3 Excluded from pinned-source evidence

The following are outside the primary source plane unless an experiment explicitly names them as generated artifacts and records the producing command: `node_modules/`, `lib/`, `apps/web/dist/`, `coverage/`, `.artifacts/`, `.dsh-build/`, `.cache/`, `.sessions/`, `.storages/`, `tmp/`, `dist-exe/`, `*.tsbuildinfo`, local `.env`, and other ignored runtime residue listed in `.gitignore`.

The TechStackShow repository does not vendor DSH. The external fixture must remain outside Git changes. Latest-main code, an installed npm package, the live documentation website, and another checkout are comparison sources only; none may silently fill a gap in the pinned call path.

- **Counter-evidence searched:** `.gitignore`, the source/artifact-plane rule in `AGENTS.md` and `docs/development.md`, generated-document rules in `docs/AGENTS.md`, the `experimental/` release exclusion, and the vendoring manifest rule.
- **Proves:** which tracked owners may establish pinned source facts and which files require artifact or comparison labels.
- **Does not prove:** that every included file is authoritative for every Claim, or that ignored residue is fresh.
- **Limitations:** a later call path may legitimately cross vendored or generated code only after recording its own provenance.
- **DSH verification:** `SOURCE_CONFIRMED` for the boundary declarations; runtime is not applicable.

## 3. Official documentation entry and authority

| Entry | File / symbol | Static path | Status |
|---|---|---|---|
| Public documentation entry | `README.md` / `Documentation` link to `https://deepseek-harness.github.io/deepseek-harness/` | repository README -> public docs website | `DOC_CONFIRMED`; website content is version-sensitive and was not used as pinned implementation evidence here |
| Contributor setup | `README.md: Run from source`; `docs/development.md: Setup tutorial, Profile runs` | `pnpm install -> pnpm run build -> pnpm dsh ...` | `DOC_CONFIRMED`; command outcome is pending Lab Engineer raw probes |
| Architecture map | `docs/architecture.md` / Cordis, package map, extension-point table | official architecture statement -> owning packages | `DOC_CONFIRMED`; package ownership and runtime traversal require source/trace evidence |
| Safety ceiling | `SAFETY.md`; `README.md: Developer preview` | safety notice -> least-privilege run boundary | `DOC_CONFIRMED`; no claim about actual security strength |

Counter-evidence searched: the pinned README explicitly says developer preview and warns of compatibility-breaking changes; root instructions state supported Node applications launch only through `dsh` profiles; `SAFETY.md` rejects production-readiness and treating the built-in sandbox as the sole control.

**Proves:** the pinned project declares its documentation entry, supported source workflow and safety posture.

**Does not prove:** that the public website matches this commit byte-for-byte, or that install/build/test/run succeeds on the current machine.

**Limitations:** official prose is evidence class `OFFICIAL_DOC`, not a substitute for current source or runtime.

**DSH verification:** `SOURCE_CONFIRMED` for file/link existence; `RUNTIME_CONFIRMED = NONE`.

## 4. Baseline command-to-source map

### 4.1 Install entry

- **Command:** `pnpm install`
- **File / symbol:** root `package.json` / `packageManager = pnpm@11.7.0`, `engines.node = ^22.19.0 || >=24.0.0`, `workspaces`, `scripts.postinstall`.
- **Static path:** pnpm workspace install -> dependency graph/lockfile -> npm lifecycle `postinstall` -> `node scripts/install-lefthook.mjs`.
- **Counter-evidence searched:** root `package.json` has no `scripts.install`; therefore the install command is owned by pnpm, not by a repository `install` script. `postinstall` proves only the declared lifecycle tail.
- **Proves:** exact package-manager pin, Node range, workspace entry and repository-owned postinstall target.
- **Does not prove:** registry/network availability, dependency integrity beyond the lockfile, successful installation, hook installation, or later build success.
- **Limitations:** pnpm internals and downloaded dependency code are outside this repository source map.
- **DSH verification:** `SOURCE_CONFIRMED`; execution outcome `PENDING baseline probe`.

### 4.2 Build entry

- **Command:** `pnpm run build`
- **File / symbol:** root `package.json:scripts.build`; `scripts/build.ts:main`, `runScript`.
- **Static path:** `package.json build` -> `tsx scripts/build.ts` -> `main()` -> `runScript('build:lib')` -> root `build:lib` -> `build:lib:host` then `build:lib:client`; after that `runScript('build:web')`; finally `writeClientBuildRecord(...)`.
- **Host/client subpaths:** `build:lib:host` invokes `tsc -b tsconfig.host.json` then `tsdown --env.DSH_BUILD_FACE host`; `build:lib:client` invokes `tsc -b tsconfig.client.json` then `tsdown --env.DSH_BUILD_FACE client`; `build:web` delegates to the `@deepseek-ai/dsh-web-frontend` package.
- **Counter-evidence searched:** `scripts/build.ts` removes the prior build record and throws on a non-zero child exit; a stale `lib/` tree may exist independently, and the source launcher does not rebuild automatically.
- **Proves:** exact orchestration order and failure propagation in the frozen source.
- **Does not prove:** compilation success, artifact correctness, clean-artifact reproducibility, or runtime usability.
- **Limitations:** generated `lib/`, Web dist and build record are artifact-plane evidence only after a recorded run.
- **DSH verification:** `SOURCE_CONFIRMED`; execution outcome `PENDING baseline probe`.

### 4.3 Unit-test entry

- **Command:** `pnpm run test`
- **File / symbol:** root `package.json:scripts.test = vitest run`; `vitest.config.ts:default configuration`.
- **Static path:** pnpm script dispatch -> Vitest CLI `run` -> root `vitest.config.ts` -> configured projects/includes/setup files -> selected `*.spec.ts`/`*.spec.tsx` suites.
- **Adjacent but distinct entries:** `test:e2e` uses `vitest.e2e.config.ts` and real-provider suites self-skip without credentials; `test:snapshot` uses `vitest.snapshot.config.ts` and defaults to keyless replay. Neither is part of `pnpm run test`.
- **Counter-evidence searched:** root instructions state `test:coverage`, not `test`, is the CI coverage gate; SDK/snapshot surfaces are not covered merely because unit tests pass.
- **Proves:** exact unit-test dispatcher and the separation of unit, e2e and snapshot planes.
- **Does not prove:** any suite passed, every package behavior is covered, or an application profile ran.
- **Limitations:** Vitest and imported dependencies are external executors; raw command output belongs in the baseline probe artifact.
- **DSH verification:** `SOURCE_CONFIRMED`; execution outcome `PENDING baseline probe`.

### 4.4 Supported source CLI and profile boot

- **Command:** `pnpm dsh --profile headless "<bounded task>"`.
- **File / symbols:** root `package.json:scripts.dsh`; `apps/cli/src/bin.ts`; `apps/cli/src/args.ts:parseDshArgs, resolveBoot`; `apps/cli/src/profile-boot.ts:prepareProfile, composeProfile, runProfile`; `packages/boot/app-boot/src/index.ts:boot, mountRootInclude, assertEntriesActivated`.
- **Closed static path:** root script `node --import tsx/esm apps/cli/src/bin.ts` -> `parseDshArgs(process.argv.slice(2), readVersion())` -> `ProfileInvocation` -> dynamic import of `profile-boot.ts` -> `runProfile(...)` -> `composeProfile(...)` / `prepareProfile(...)` -> `boot(...)` -> new Cordis `Context` -> `ctx.plugin(Loader)` -> launcher `prepare` callback (`provideCmdline`, frozen launch environment) -> `mountRootInclude(...)` -> `ctx.loader.await()` -> `assertEntriesActivated(...)` -> settled root context.
- **Headless composition seed, deliberately not a completed Agent path:** `packages/bundle/headless/package.json` declares the bundle patch; `packages/bundle/headless/cordis.patch.yml` inserts `@deepseek-ai/dsh-headless/startup` and `@deepseek-ai/dsh-headless`. Article 29 must independently follow those rows through the runner, agent registry/factory and a real trace.
- **Installed binary distinction:** `apps/cli/package.json:bin.dsh = lib/bin.js`; this is the built/installed artifact entry, while the root `dsh` script is the supported source entry.
- **Counter-evidence searched:** package bins and demos are not alternative supported Node application launches; the pinned instructions prohibit bypassing `dsh` profiles. `apps/cli/reference/README.md` warns that source launch requires prior build artifacts and can consume stale browser bundles.
- **Proves:** argument ownership, profile selection, configuration composition entry, Loader installation/mount/settlement and the headless bundle rows exist at the frozen revision.
- **Does not prove:** that the headless row activated in a probe, that credentials/provider/network are valid, that an Agent was created, that a Turn completed, or that output is durable/correct.
- **Limitations:** this map intentionally stops at application boot. The complete Host/profile-to-Agent Run chain belongs to Article 29.
- **DSH verification:** `SOURCE_CONFIRMED` through settled-boot code path; headless Agent Run `PARTIAL / RUNTIME PENDING`.

## 5. Baseline path summary

```text
pnpm install
  -> pnpm workspace/lockfile resolution
  -> package.json postinstall
  -> scripts/install-lefthook.mjs

pnpm run build
  -> package.json scripts.build
  -> scripts/build.ts main/runScript
  -> build:lib (Host, then Client)
  -> build:web
  -> client build record

pnpm run test
  -> package.json scripts.test
  -> vitest run
  -> vitest.config.ts unit projects

pnpm dsh --profile headless "<bounded task>"
  -> package.json scripts.dsh
  -> apps/cli/src/bin.ts
  -> parseDshArgs
  -> runProfile
  -> composeProfile / prepareProfile
  -> app-boot boot
  -> Loader mount / await / activation audit
  -> STOP: runner -> Agent path is Article 29 evidence work
```

This is a static map. It contains no exit code, stdout/stderr, event sequence or runtime confirmation.

## 6. Article 29—37 source routing

These are verified route anchors, not completed call paths. Each later article must produce its own source card, counter-evidence and runtime/experiment trace before strengthening wording.

| Article | Owning question | Exact source anchors at the pinned revision | Required next closure | Current status |
|---|---|---|---|---|
| 29 | How does a supported profile reach one Agent Run? | `apps/cli/src/bin.ts:parse dispatch`; `profile-boot.ts:runProfile`; `packages/boot/app-boot/src/index.ts:boot`; `packages/bundle/headless/cordis.patch.yml:headless-startup/headless-runner`; `packages/bundle/headless/src/startup.ts`; `src/index.ts`; `packages/core/agent-loop/src/index.ts:AgentLoop`; `agent.ts:ReactLoopAgent` | Close bundle row -> runner -> `ctx.agents`/factory -> Agent -> Turn, then pair with a bounded run trace | `PARTIAL / DEFER` |
| 30 | Does a plugin install, register, operate and dispose its contribution? | root/package `AGENTS.md` registration rules; vendored Cordis `Context.plugin`/fiber/effect owners when crossed; representative package `apply`; package lifecycle/HMR tests; `packages/AGENTS.md` disposal rule | Select one representative plugin and observe install/register/operate/dispose, including contribution removal | `PARTIAL / DEFER` |
| 31 | How do profile, bundle, overlay and effective config compose? | `apps/cli/src/args.ts:resolveBoot`; `profile-boot.ts:prepareProfile, composeProfile, allPatches`; `dump-config.ts:runDumpConfig`; `packages/boot/app-boot/src/index.ts:composeEntries, renderConfigDump`; `packages/bundle/*/cordis.patch.yml`; `packages/preset/agent-presets/` | Freeze schema/layers, precedence/conflict/missing cases and effective-config dumps | `PARTIAL / DEFER` |
| 32 | How do context sources become a model request? | `packages/core/system-prompt/src/index.ts:PromptContext, PromptAssembly, SystemPrompt, renderPrompt`; `packages/context/*/src/`; `packages/core/agent-loop/src/agent.ts:preStep, buildRequest` | Close registration -> assembly -> request path and save a two-step diff plus negative cases | `PARTIAL / DEFER` |
| 33 | How do Inbox, Turn, Step and stop conditions progress? | `packages/core/agent-loop/src/agent.ts:ReactLoopAgent, turn, step`; `runtime-context.ts:RuntimeContextProjection`; `tool-calls.ts:executeToolCalls`; `packages/core/session/src/types.ts:turn/step events` | Produce no-tool, single-tool, multi-tool and cancellation traces | `PARTIAL / DEFER` |
| 34 | How do append-only events support reads, projections and continuation operations? | `packages/core/session/src/types.ts:SessionEventMap, SessionEvent`; `src/index.ts:Session`; `packages/session/session-persistence/src/coordinator.ts:PersistenceCoordinator`; `session-persistence-jsonl/src/index.ts:JsonlSessionPersistence`; `session-projection/src/index.ts:SessionProjectionRegistry`; fork/resume tests | Close append/write/read/projection paths, then run replay/resume/fork experiments with event sequences | `PARTIAL / DEFER` |
| 35 | How does a tool travel from schema/registry through enforcement to result/event? | `packages/core/tools/src/index.ts:ToolRuntime, ToolDefinition, ToolExecutionResult`; `schema.ts:defineTool, validateArgs`; `packages/core/agent-loop/src/tool-calls.ts:executeToolCalls`; `packages/guard/timeout-policy/src/index.ts:apply, TOOL_TIMEOUT`; interaction/permission packages selected by the scenario | Close policy/executor ownership and run bad-args, deny, timeout, cancel and large-result negatives | `PARTIAL / DEFER` |
| 36 | Which owners handle usage, compaction, cancellation and recovery? | `packages/session/session-stats/`; `packages/compaction/compaction/src/index.ts`; `packages/compaction/compaction-basic/`; `packages/guard/timeout-policy/`; `packages/core/agent-loop/`; `packages/session/session-checkpoint-policy/src/index.ts`; persistence packages | Keep usage, pressure, compaction, cancel and resume terminal evidence separate; do not equate resume with crash recovery | `PARTIAL / DEFER` |
| 37 | Which features are core, extension or product composition? | `docs/architecture.md` extension table; `packages/skill/`; `packages/workflow/`; `packages/subagent/`; `packages/web/`; `packages/bundle/base/`, `headless/`, `web-app/`; relevant shipped `cordis.patch.yml` files | Verify activation/default status per feature and produce `ADOPT / SIMPLIFY / REJECT / DEFER`; no Part VII design | `PARTIAL / DEFER` |

For every row, the **counter-evidence searched** was the same shortcut set: same-named package, README wording, config-row existence, test-name existence and generated catalog presence. The table **proves** that exact pinned route anchors exist and bounds the next investigation. It **does not prove** ownership across the full path, activation, runtime traversal or BuildPilot suitability. Its **limitation** is deliberate: all rows remain `PARTIAL`, and their DSH verification can become `SOURCE_CONFIRMED` or `RUNTIME_CONFIRMED` only in the owning article.

## 7. Counter-evidence register

| Risky shortcut | Evidence found against it | Consequence |
|---|---|---|
| “README says run, therefore it ran” | README and development guide provide steps and prerequisites only | Keep command outcomes pending raw probes |
| “Package/directory exists, therefore enabled” | Activation is profile/bundle/config-row owned | Later articles must trace the actual composition |
| “Test exists or passes, therefore application runtime is confirmed” | Unit, e2e, snapshot and supported profile entries are separate dispatchers | Preserve `SOURCE_CONFIRMED` versus `RUNTIME_CONFIRMED` |
| “Source launcher needs no build” | Reference docs say it starts TS source but still consumes required built artifacts and may see stale browser bundles | Record build state and artifact provenance in every run |
| “Headless means Host path” | Headless package describes a direct core runner with no Host/HTTP/browser layer | Article 29 must name the exact plane instead of forcing a generic Host label |
| “Generated output is current source” | `.gitignore` and development guide separate source plane from artifact plane | Generated `lib/dist` requires producing-command evidence |
| “All packages are shipped core” | `experimental/` is excluded from official releases; profiles select compositions | Article 37 must verify core/extension/default independently |

## 8. Source-map decision

- Source map result: `PASS`.
- DSH source cards established here: `6` core records — boundary, official docs, install, build, test, CLI/profile boot.
- Confirmation: `SOURCE_CONFIRMED = 6`; `PARTIAL = headless Agent Run and all Article 29—37 dynamic conclusions`; `RUNTIME_CONFIRMED = 0`.
- BuildPilot decision: `ADOPT` the evidence-class/source-boundary method; `DEFER` every DSH architectural adoption until its owning article closes source plus runtime evidence.
- No fixture files were modified. No Article 29 workspace or Article 38—44 asset was created.
- Next allowed gate: `EXPERIMENT_DESIGN` for Article 28 baseline probes.
