# DSH Evidence Baseline / Baseline Manifest

Status: `STRUCTURED OBSERVATIONS COMPLETE / EVIDENCE MERGE PENDING`

Durable evidence form: this manifest and `experiments/baseline-probes.md` retain direct structured observations with commands, environment, exit codes, terminal summaries, failure classification and sanitized excerpts. The complete stdout/stderr stream was not retained.

## 1. Identity

| Field | Frozen value | Verification |
|---|---|---|
| Repository | `https://github.com/deepseek-ai/deepseek-harness` | local `origin` is `https://github.com/deepseek-ai/deepseek-harness.git` |
| Tag | `dsh-v0.1.2-alpha.1` | local dereference and fresh `git ls-remote --tags` both resolve to the full commit below |
| Full commit | `cd5ef8148158c3a752a658978873241fdf8e2bbc` | `HEAD`, local tag target and remote tag target agree |
| Selected at | `2026-08-29` | Course Factory baseline selection |
| Verified at | `2026-08-30 / Asia/Shanghai` | direct Lab Engineer probes |
| External fixture | `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814` | external, not vendored and not committed |
| Initial/final fixture state | clean | `git status --porcelain=v1 --untracked-files=all` returned no rows before and after probes |

The remote tag query first failed inside the network-restricted sandbox and then exited `0` under a narrow read-only `git ls-remote` escalation. This proves the remote tag mapping at verification time; it does not prove later remote immutability.

## 2. Project posture and documentation

- License: root `package.json` declares `MIT`; the repository includes `LICENSE`.
- Product posture: pinned `README.md` calls the project a developer preview. `SAFETY.md` says it has not undergone a security audit and must not be treated as secure or production-ready.
- Safety guidance: least privilege, disposable/dedicated environments, backups, credential/data minimization, and review of plugins/configuration/commands.
- Official documentation entry: the pinned `README.md` links `https://deepseek-harness.github.io/deepseek-harness/`; the website's current contents were not used as pinned implementation evidence.
- Contributor/source references used for the baseline: pinned `README.md`, `AGENTS.md`, `SAFETY.md`, `docs/architecture.md`, `docs/development.md`, `docs/testing.md`, `docs/defensive-patterns.md`, and owning source/manifests.

These are `OFFICIAL_DOC` or `PINNED_SOURCE` facts. They do not demonstrate a successful install, build, test or Agent Turn.

## 3. Environment

| Component | Direct observation |
|---|---|
| OS | `Microsoft Windows NT 10.0.19045.0` |
| Architecture | `X64` |
| Node.js | `v24.18.1` |
| Root Node engine | `^22.19.0 || >=24.0.0` |
| Project package manager | `pnpm@11.7.0` from root `packageManager` and `corepack pnpm --version` inside the fixture |
| Globally installed pnpm | `11.19.0`; not used to define the project result |
| npm | `11.16.0` |
| Git | `2.53.0.windows.2` |
| PowerShell | `7.6.4` |

Calling `corepack pnpm --version` from the TechStackShow directory did not see DSH's `packageManager` declaration and attempted a blocked npm lookup. Repeating it from the DSH fixture returned `11.7.0`. Only the in-fixture result belongs to the project baseline.

## 4. Direct Lab Engineer command results

| Layer | Command / condition | Exit | Direct result | Evidence ceiling |
|---|---|---:|---|---|
| Install | `corepack pnpm install --frozen-lockfile --offline` | `0` | all 265 workspace projects; already up to date; completed with pnpm `11.7.0`; Linux-only landlock packages warned as unsupported on `win32/x64` | frozen dependency state is locally installable from the populated store; no clean-store or network reproducibility claim |
| Build, sandbox attempt | `corepack pnpm run build` | `1` | Host and Client build advanced, then Vite/esbuild could not read a parent directory and could not resolve `apps/web/vite.config.ts`: `Access is denied` | proves a sandbox restriction, not a source build defect |
| Build, unchanged host retry | same command under narrow host escalation | `0` | Host, Client and Web builds completed; Vite reported 345 transformed modules; build recorded 218 client artifacts and 2 public values | this fixture is buildable on this host when the required filesystem access is available; warnings remain non-fatal |
| Full unit test | `corepack pnpm run test` | `1` | 32 test files failed, 965 passed, 4 skipped; 129 tests failed, 15939 passed, 66 skipped; duration 305.24 s | full unit suite is **not passing** on this Windows/sandbox execution; no targeted test may upgrade it |
| Focused timeout check | `node node_modules/vitest/vitest.mjs run scripts/gen-third-party-notices.spec.ts --testTimeout=30000` | `0` | 1 file and 27 tests passed; test body took 4.94 s, total duration 5.79 s | classifies one default-5-second failure as timing-sensitive; says nothing about the other 128 failures |
| Built CLI help | `node apps/cli/lib/bin.js --help` | `0` | usage, profile, patch and config-dump options printed; no service or task started | built CLI surface exists; no profile activation or Agent Run claim |
| Headless config dump | isolated `DSH_HOME`; built CLI `--profile headless --dump-config` | `0` | composed base plus headless rows printed, including `headless-startup`, `headless-runner`, provider, telemetry and permission expressions | composition output is runtime/artifact evidence for effective config resolution; it is not proof that every row activated |
| Keyless headless run | second isolated `DSH_HOME`; telemetry `DISABLED`; permission `read-only`; all inherited environment names matching `KEY|SECRET|TOKEN|PASSWORD` removed | child `1` | no timeout, no stdout/model result; stderr reported `MISSING_CREDENTIAL` for route `deepseek-official` and named `DEEPSEEK_API_KEY` without exposing a value | confirms fail-closed credential resolution on this path; Agent Turn completion, provider/network response and model behavior remain unconfirmed |

### Full-test failure classification

The full test result remains `FAIL`. Its failures are not safely reducible to one cause:

- Many filesystem, workspace, skill, spill, snapshot and documentation cases failed at Windows symlink creation with `EPERM`.
- Windows ACL probes included `CreateRestrictedToken failed (Win32 87)` and sandbox-mode assertion failures.
- Process-bound subprocess, LSP, PowerShell, Codex and Claude fixtures produced 5 s, 10 s, 15 s, 60 s or 70 s timeouts, teardown failures and some `EBUSY` cleanup errors.
- Network-dependent fixture activity could not reach external plugin/service endpoints inside the sandbox.
- At least one persistent PowerShell assertion retained an unexpected end-marker line; an LLM adapter Retry-After assertion and a directory-picker home case also failed. They were not independently proven to share the symlink or timeout cause.
- The third-party notices test failed at its default 5 s limit but passed 27/27 when isolated with a 30 s limit. This is counter-evidence against treating that one failure as a deterministic content mismatch.

Therefore Article 28 may say the test entry executes and produces a measured result, but must say the current-host full suite fails.

## 5. Master precheck observations, not produced by this Lab's direct probe set

The transaction Master supplied earlier baseline context. It is retained only to expose run-to-run/environment variation:

- Two earlier `corepack pnpm install --frozen-lockfile` attempts exited `1` after network timeouts; a third attempt with `--network-concurrency=1 --fetch-timeout=300000` exited `0` after installing 1011 packages.
- An earlier build exited `0` and recorded 218 client artifacts.
- An earlier full test exited `1` with 24 failed, 973 passed and 4 skipped files; 44 failed, 16028 passed and 62 skipped tests. Its isolated notices probe passed 27/27 with a 30 s timeout.

These values were not generated by this Lab Engineer and are not substituted for the direct table in section 4. The differing full-test counts reinforce that the baseline must preserve command, host and sandbox context instead of reporting a single context-free “testable” label.

## 6. Source, generated and excluded boundaries

- Primary pinned source: tracked root documentation/manifests, `apps/cli/src/`, `packages/*/*/src/`, owned `cordis*.yml`, scripts, test source and vendored Cordis with its manifest.
- Generated/artifact plane: `node_modules/`, `lib/`, `apps/web/dist/`, build records and test-generated temporary residue. They may support the recorded command result but cannot silently replace pinned source evidence.
- Excluded local state: `.env`, credentials, sessions, storages, caches, coverage, temporary homes and other ignored residue.
- The fixture is external to TechStackShow. The course records paths and outcomes, not DSH source or generated payloads.
- After all probes, fixture `HEAD` and tag still resolved to the pinned commit and `git status --porcelain` remained empty.

## 7. Provider, network, sandbox and cost conditions

- No real provider credential was obtained, read, printed or used.
- The keyless child inherited zero environment variable names matching `KEY`, `SECRET`, `TOKEN` or `PASSWORD` after filtering.
- `DSH_TELEMETRY_MODE=DISABLED` and `DSH_PERMISSION_MODE=read-only` were set for the bounded run.
- The keyless run ended at credential resolution. There is no evidence of a provider request, token usage or monetary cost.
- No server was started and no interface was bound.
- The only intentional external operation was a read-only official-repository tag query. Unit-test fixtures attempted network access but the sandbox denied it; those failures do not establish remote service behavior.
- The successful build required a narrow host-filesystem escalation after direct sandbox evidence. DSH's own sandbox/approval mechanisms are not treated as sufficient isolation.

## 8. Baseline verdict

| Question | Verdict | Narrow supported claim |
|---|---|---|
| Buildable? | `PASS_WITH_HOST_ACCESS_CAVEAT` | frozen install succeeds offline and the complete build exits 0 when Vite/esbuild receives required host filesystem access |
| Testable? | `ENTRY_CONFIRMED / FULL_SUITE_FAIL` | the complete unit-test entry executes and is measurable, but 129 tests fail in the direct Windows/sandbox run |
| Runnable? | `PARTIAL / AGENT_RUN_NOT_CONFIRMED` | built CLI help and effective headless config resolution succeed; the bounded keyless run exits 1 at missing credentials before a model result |
| Production-ready? | `NO CLAIM / OFFICIAL WARNING` | pinned official safety text explicitly rejects treating this developer preview as secure or production-ready |

Claim impact for the Evidence Merge:

- `28-C01` gains fresh remote/local identity and final cleanliness evidence and remains `CONFIRMED`.
- `28-C02` remains `CONFIRMED` from pinned official documentation, with no security-strength inference.
- `28-C05` must be split or narrowed: install and host-access build are confirmed, while the full unit suite is a direct `FAIL` on this environment.
- `28-C06` remains `PARTIAL`: CLI/config/credential-boundary observations exist, but no completed Agent Run exists.
- `28-C07` gains an example of why command, sandbox, artifact and runtime evidence must stay separate; it remains a course-method proposal until the Part VI audit.

Next allowed gate: `EVIDENCE_MERGE` by the Researcher. No Article 29 conclusion is upgraded here.
