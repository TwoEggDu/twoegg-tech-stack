# Article 31 Effective Config Diff

Status: `RAW OBSERVATION COMPLETE / EFFECTIVE CONFIG + FS SEAM CONFIRMED / REPO-OWNED OVERLAY DRIFT REPRODUCED`

## 1. Frozen experiment contract

### Research question

在不使用真实 Provider credential、不连接模型、不开放监听端口、也不修改 DSH tracked source 的前提下，pinned DSH 的 repo-owned CLI 能否在隔离 `DSH_HOME` 中物化 `headless` 与 `web` 两个 shipped Profile，输出可比较的 Effective Config，并用一个最小可执行测试闭合 FS Service Provider 到 Tool Consumer 的 Capability Seam？配置 dump、真实 activation、权限变化又分别能证明什么？

### Hypothesis

1. 第一次执行 built `dsh --profile <name> --dump-config` 会从 shipped template 物化 mutable Profile；`headless` 应为 `base + headless`，`web` 应为 `base + web-app`。
2. 两份 dump 应共享 Base Bundle 中的 FS、Sandbox、Approval 与 Tool 行，只在 Host/App 行上分叉。
3. 空的 profile-local `cordis.patch.yml` 不应改变 `web` Effective Config，因此同一隔离 Home 下 `--dump-config` 与 `--dump-default-config` 应字节一致。
4. repo-owned Cordis Web overlay 应把 `webserver` 的动态默认值替换为 `127.0.0.1:3081` 并插入 Cordis tool 行；如果 overlay 已与当前 shipped Profile 漂移，dump 或 activation 应暴露冲突，而不是把冲突当成成功启动。
5. `LocalFileSystem -> ToolFs` 的 owner integration test 应证明一个真实本地 FS Provider 经 Tool Registry Consumer 写入并回读精确字节。权限切换测试若使用 fake confining FS，只能证明 policy / approval / consumer 协议，不得冒充真实 OS sandbox enforcement。

### Falsification criteria

以下任一项出现即收窄对应结论，且不得用源码意图或 expected 值替代 observed output：

1. 任一 Profile dump 非零退出、为空、没有 provenance group，或两个 Profile 不能在同一隔离 `DSH_HOME` 中物化。
2. dump 中找不到 Base、mode Bundle 或共同 Capability 行；或者 diff 不能定位 Host/App 分叉。
3. overlay change 没有在 dump 中体现，或冲突只在 boot 时出现却被 dump 成功掩盖。
4. exact FS Provider/Consumer test 非零退出，或测试只使用 fake Provider。
5. 运行使用真实 key、外部 Provider、真实模型、生产输入、非 loopback listener，或改变 DSH fixture 的 Git 状态。

### Frozen source and environment

- Official repository: `https://github.com/deepseek-ai/deepseek-harness`
- Tag: `dsh-v0.1.2-alpha.1`
- Commit: `cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Built product entry: `apps/cli/lib/bin.js`
- Repo-owned overlay: `apps/cli/config/examples/cordis/cordis.yml`
- Isolated lab root: `C:\Users\IGG\AppData\Local\Temp\codex-dsh-a31-6aff3ba1b2794d8fb9bf7cc23cfe8eed`
- Isolated DSH Home: `<lab-root>\home`
- Isolated cwd: `<lab-root>\work`

### Frozen commands

Product dumps and credential-free app-CLI smokes:

```text
node <fixture>/apps/cli/lib/bin.js --profile headless --dump-config
node <fixture>/apps/cli/lib/bin.js --profile web --dump-config
node <fixture>/apps/cli/lib/bin.js --profile web --dump-default-config
node <fixture>/apps/cli/lib/bin.js --profile web --dump-config --patch <fixture>/apps/cli/config/examples/cordis/cordis.yml
node <fixture>/apps/cli/lib/bin.js --profile headless --help
node <fixture>/apps/cli/lib/bin.js --profile web --help
node <fixture>/apps/cli/lib/bin.js --profile web --patch <fixture>/apps/cli/config/examples/cordis/cordis.yml --help
node <fixture>/apps/cli/lib/bin.js --profile web --dump-config --patch <lab-root>/missing-overlay.yml
```

Owner tests, invoked directly through the repo-installed Vitest binary to avoid package-manager argument rewriting:

```text
node node_modules/vitest/vitest.mjs run packages/boot/app-boot/tests/config-dump.spec.ts
node node_modules/vitest/vitest.mjs run packages/fs/tool-fs/tests/integration.spec.ts -t "creates a file with exactly the requested bytes"
node node_modules/vitest/vitest.mjs run packages/fs/tool-fs/tests/tools.spec.ts -t "a plain write stamps the default mode with the calling session root"
node node_modules/vitest/vitest.mjs run packages/fs/tool-fs/tests/tools.spec.ts -t "a standing session override|an approved escalation stamps|a rejected escalation"
```

### Acceptance and evidence labels

- Both Profile dumps: exit `0`, non-empty YAML, provenance labels, unique row identifiers in the unmodified composition.
- Shared core: both dumps carry `sandbox`, `sandbox-policy`, `approval`, `tool-fs`, `fs-sandbox` and the platform-gated shell tool rows.
- Host split: `headless` owns one-shot runner/startup rows; `web` owns web host, API and UI rows.
- Overlay: the changed `webserver` config and inserted rows appear in the dump; any duplicate id or boot rejection is retained as counter-evidence.
- Real FS seam: exact targeted integration test passes with `LocalFileSystem` and `ToolFs` against a fresh temp directory.
- Permission seam: exact tests pass, but because `SandboxingFakeFs` records policies, label this only `TEST_FIXTURE_RUNTIME_CONFIRMED`, not OS sandbox confirmation.
- Dump results are `EFFECTIVE_CONFIG_DUMP_CONFIRMED`; app `--help` runs are bounded activation smokes. Neither proves real model/provider behavior, token/cost, production security or a long-running Web Host.

### Safety, credential, network and cost boundary

- No credential value was read, printed or passed. Only secret-like environment **name counts** were observed.
- No model request, provider HTTP request or charge was authorized or attempted.
- Web probes used `--help`; no listener was requested. The overlay activation failed during Loader application before host startup.
- Product-generated Profile files and capture files live only below the isolated lab root. They are not course artifacts and are not inside the DSH Git fixture.
- DSH is experimental developer-preview software. These observations do not establish production readiness, security isolation or sandbox completeness.

## 2. Raw observations

### Probe A — identity and clean-state envelope

```text
Observed at: 2026-08-30T06:28:49.5067955+08:00
OS: Microsoft Windows NT 10.0.19045.0
Architecture: X64
Node: v24.18.1
Project-pinned pnpm: 11.7.0
Git: git version 2.53.0.windows.2
PowerShell: 7.6.4
origin: https://github.com/deepseek-ai/deepseek-harness.git
HEAD: cd5ef8148158c3a752a658978873241fdf8e2bbc
tag target: cd5ef8148158c3a752a658978873241fdf8e2bbc
fixture status rows before experiments: 0
secret-like environment names: 5
DSH/DEEPSEEK-prefixed secret-like names: 0
```

Result: `PASS`. No secret value was inspected. A first `corepack pnpm --version` probe was accidentally launched from the course repository rather than the DSH fixture, so Corepack tried and failed to query `https://registry.npmjs.org/pnpm/latest` with `EACCES`. The corrected command was run from the pinned fixture and returned `11.7.0` with exit `0`; the failed network probe is not evidence about DSH dependency state.

Two lab-orchestration mistakes are retained but excluded from product conclusions:

1. A PowerShell variable named `$home` collided with the read-only automatic `HOME` variable. The intended temp directory assignment failed, the following CLI process exited `1` at filesystem access, and no DSH fixture file changed.
2. A `Start-Process -ArgumentList` capture helper flattened the arguments incorrectly. Four attempted captures each reached the CLI without `--profile` and exited `1` with `error: --profile <name> is required`. Direct PowerShell argument arrays replaced that helper; only the corrected runs below are counted.

### Probe B — shipped template materialization

The first successful dump created these mutable Profile manifests under the isolated `DSH_HOME`:

```json
{
  "headless": {
    "bundles": ["@deepseek-ai/dsh-base", "@deepseek-ai/dsh-headless"],
    "patchReload": "startup",
    "manifestSha256": "104F43C27B3521E3B548FC3E5088DB93AF4F065F543860624005915F16E9D679"
  },
  "web": {
    "bundles": ["@deepseek-ai/dsh-base", "@deepseek-ai/dsh-web-app"],
    "patchReload": "live",
    "manifestSha256": "B07F71CBA5C341F6BD6EBF1316573588817E3A15629A86F11D34D4A01A82C94B"
  }
}
```

Both generated `cordis.patch.yml` files contained the same documented comments followed by `[]`; each had SHA-256 `EF189A8C27DB6D63930AA3046A3040482E952EAFCB7487C644D508E8D461F027`.

This observation separates four states:

1. **Shipped template**: the installation-owned tuple and reload default used to initialize a known Profile.
2. **Materialized mutable Profile**: the generated manifest and profile-local `cordis.patch.yml` below the selected `DSH_HOME`.
3. **Effective resolved config**: the boot-free composed YAML printed by the CLI after bundle and patch application.
4. **Activation**: Loader application and plugin lifecycle during a real Profile run; the dump does not perform it.

### Probe C — headless and web Effective Config dumps

All hashes below are over the exact stdout bytes captured from the built CLI. Stderr was empty.

| Dump | Exit | Bytes | Rows | Unique ids | SHA-256 |
|---|---:|---:|---:|---:|---|
| `headless --dump-config` | 0 | 11,227 | 89 | 89 | `7B00D284956107355C44629B861C1754A570835AE04F44F9AA15E9586ECA5298` |
| `web --dump-config` | 0 | 16,558 | 144 | 144 | `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A` |
| `web --dump-default-config` | 0 | 16,558 | 144 | 144 | `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A` |

The web effective and default dumps were byte-identical. That does **not** prove a general equivalence: it proves only that this freshly materialized Profile's empty local patch added no change. A later local edit would make the two commands diverge by design.

Provenance labels showed the layer order directly:

```text
headless: @deepseek-ai/dsh-base
       -> @deepseek-ai/dsh-base, patched by @deepseek-ai/dsh-headless
       -> @deepseek-ai/dsh-headless

web:      @deepseek-ai/dsh-base
       -> @deepseek-ai/dsh-base, patched by @deepseek-ai/dsh-web-app
       -> @deepseek-ai/dsh-web-app
```

The two dumps shared 87 unique ids. Both included the common Capability and policy rows:

```text
sandbox
sandbox-policy
approval
tool-bash
tool-pwsh
tool-fs
fs-sandbox
```

The common permission rows remained verbatim `!!js` expressions in the dump rather than evaluated runtime values:

```yaml
- id: sandbox-policy
  name: '@deepseek-ai/dsh-sandbox-policy'
  config:
    mode: !!js process.env.DSH_PERMISSION_MODE ?? 'workspace-write'
    workspaceRoot: !!js process.cwd()
- id: approval
  name: '@deepseek-ai/dsh-user-approval'
  config:
    policy: !!js "(process.env.DSH_PERMISSION_MODE ?? 'workspace-write') === 'danger-full-access' ? 'never' : 'ask'"
```

Therefore a dump proves configuration composition and the permission expression's source, not the permission actually selected for a Session.

Only headless had:

```text
headless-runner
headless-startup
```

Web had 57 ids absent from headless. The Host/App-specific set included `web-startup`, `webserver`, `cordis-host-runner`, `web-runtime`, `connection`, API controllers, workspace services and UI rows. This is observed composition evidence that the modes share the Base capability stack while the Web Host and one-shot Headless runner sit in mode bundles.

### Probe D — explicit overlay change and drift collision

Overlay source:

```text
apps/cli/config/examples/cordis/cordis.yml
bytes: 781
SHA-256: 62F0D905D430F7A1A517125AAE8EE5786EABCC9B77FA362AEA16A9104A0EFD31
```

The repo-owned overlay targets `webserver` and inserts `cordis-host-runner` plus `tool-cordis`. The composed dump exited `0` with empty stderr:

| Dump | Exit | Bytes | Rows | Unique ids | SHA-256 |
|---|---:|---:|---:|---:|---|
| `web --dump-config --patch <cordis overlay>` | 0 | 16,827 | 146 | 145 | `679CC5ED39C53FDBB2D6A57014DF4486FA274A0E3250D98AF4223DED6C6D76E9` |

The target replacement was visible and correctly attributed:

```yaml
# == @deepseek-ai/dsh-web-app, patched by <repo-owned cordis overlay>
- id: webserver
  name: '@deepseek-ai/dsh-host-webserver'
  inject:
    - webStartup
  config:
    host: 127.0.0.1
    port: 3081
```

Compared with the unmodified dump, `tool-cordis` was one new unique id. However, the current web Profile already contained `cordis-host-runner`; the overlay inserted a second row with that id. This explains `146 rows / 145 unique ids` and is a concrete repo-owned overlay drift signal.

The dump alone did not reject the duplicate. A bounded activation probe retained the counter-evidence:

```text
Command: node apps/cli/lib/bin.js --profile web --patch <repo-owned cordis overlay> --help
Exit: 1
stdout: empty
stderr key line:
Error: dsh: plugin tree failed to load: failed to apply loader entry include (cordis:include): duplicate loader entry id: cordis-host-runner
deepest cause:
TypeError: duplicate loader entry id: cordis-host-runner
```

Result: `REPO_OWNED_OVERLAY_DRIFT_RUNTIME_CONFIRMED`. The overlay dump is useful diagnostic output but is not an activation certificate. The pinned repo's own optional overlay is stale against its current Web composition on this path.

### Probe E — invalid overlay negative

The missing path was confirmed absent before invocation:

```text
Command: node apps/cli/lib/bin.js --profile web --dump-config --patch C:\Users\IGG\AppData\Local\Temp\codex-dsh-a31-6aff3ba1b2794d8fb9bf7cc23cfe8eed\missing-overlay.yml
Exit: 1
stdout: empty
stderr key line:
Error: dsh: failed to read overlay C:\Users\IGG\AppData\Local\Temp\codex-dsh-a31-6aff3ba1b2794d8fb9bf7cc23cfe8eed\missing-overlay.yml: Error: ENOENT: no such file or directory
Node.js: v24.18.1
```

Result: `INVALID_OVERLAY_FAILS_NONZERO_CONFIRMED`. The built entry emitted a Node stack around the labelled error; this probe does not claim a single-line user-facing diagnostic.

### Probe F — unmodified Profile app-CLI activation smokes

```text
headless --help: exit 0, stdout 339 bytes, SHA-256 DA8F354AAC509B233168982023F40FBAC43EF8E52615606F6FC82BB743913037, stderr empty
web --help:      exit 0, stdout 759 bytes, SHA-256 8DBDE71AA3D2FACDA865ED27789F5E888D893003B57D3307D93DEE8E1A81D6D8, stderr empty
```

The outputs were the mode-owned help texts: headless described one task and exit; web described `--host`, `--no-open`, `--port` and `--trusted-host`. No task, model call or server listener ran. These are credential-free product Profile path smokes, not proof that a full long-running Host, Agent or every plugin lifecycle remained active.

### Probe G — config dump owner tests

Command:

```text
node node_modules/vitest/vitest.mjs run packages/boot/app-boot/tests/config-dump.spec.ts
```

Observed:

```text
Exit: 0
Test Files: 1 passed (1)
Tests: 6 passed (6)
Duration: 1.18s
stderr: repeated vite-tsconfig-paths deprecation notice only
```

The six owner tests cover ordered overlay composition, contiguous provenance grouping, equality with the boot patch algorithm, labelled absent-target warnings, the default warning sink, and missing/unparsable/non-array base failures. This is test-fixture evidence for the dump algorithm; the direct CLI probes above are the product artifact observations.

### Probe H — real FS Provider to Tool Consumer seam

Command:

```text
node node_modules/vitest/vitest.mjs run packages/fs/tool-fs/tests/integration.spec.ts -t "creates a file with exactly the requested bytes"
```

Observed:

```text
Exit: 0
Test Files: 1 passed (1)
Tests: 1 passed | 32 skipped (33)
Duration: 2.03s
stderr: repeated vite-tsconfig-paths deprecation notice only
```

The targeted test creates a new temp directory, mounts the real `LocalFileSystem` Provider, `ToolRuntime`, `FsPolicy` and the `ToolFs` Consumer, executes the `write` tool through `ctx.tools.execute`, then reads the disk file back and asserts exact bytes. The test cleans its temp directory.

Result: `FS_CAPABILITY_SEAM_TEST_RUNTIME_CONFIRMED`. This closes one minimal Service Provider -> policy -> Tool Consumer path without a model. It does not prove the Profile activated those exact rows in a real Agent turn, and it does not exercise an OS confinement Provider.

### Probe I — permission change boundary with an explicit mock

Commands and observed results:

```text
node node_modules/vitest/vitest.mjs run packages/fs/tool-fs/tests/tools.spec.ts -t "a plain write stamps the default mode with the calling session root"
Exit: 0
Tests: 1 passed | 72 skipped (73)
Duration: 1.91s

node node_modules/vitest/vitest.mjs run packages/fs/tool-fs/tests/tools.spec.ts -t "a standing session override|an approved escalation stamps|a rejected escalation"
Exit: 0
Tests: 3 passed | 70 skipped (73)
Duration: 1.94s
```

These cases use the real `SandboxPolicyService`, Tool Runtime, FS policy, Tool FS Consumer and approval protocol around `SandboxingFakeFs`. The fake Provider records the resolved per-call policy. The assertions confirmed:

```text
default: workspace-write + calling Session workspace root
standing Session override: read-only
approved one-call escalation: danger-full-access
rejected escalation: fail closed and no provider mutation
```

Result: `PERMISSION_PROTOCOL_TEST_FIXTURE_RUNTIME_CONFIRMED`. Because the FS Provider is a recorder fake, this proves policy resolution and Consumer handoff, not Windows ACL, Landlock, Seatbelt or any other OS sandbox enforcement. No real approval prompt was shown; the test supplies deterministic allow/reject listeners.

## 3. Structured diff and conclusion

### Observed composition

```text
shipped Profile template
-> materialized mutable package.json + cordis.patch.yml
-> ordered Bundle patches (base, then mode)
-> profile-local patch (empty in this lab)
-> home-level patch (absent in this lab)
-> CLI --patch overlay (only in Probe D/E)
-> boot-free Effective Config dump
-> separate Loader activation when the Profile is actually run
```

The first five steps are configuration-source and composition facts. The dump makes their resolved row list and provenance observable, but it leaves `!!js` unevaluated and performs no plugin activation. Probe D demonstrates why this distinction matters: a dump can complete with duplicate ids that activation rejects.

### Web versus headless

| Boundary | Shared / different | Observed evidence |
|---|---|---|
| Base capabilities | Shared | 87 common ids; FS, Sandbox, Approval and tool rows in both dumps |
| Profile manifest | Different mode bundle | `base + headless`, `base + web-app` |
| Patch lifecycle | Different | headless `startup`; web `live` |
| Host surface | Different | headless runner/startup vs web host, API, workspace and UI rows |
| Local user patch | Same initial bytes | both generated as comments plus `[]` |
| Provider credential | Not exercised | no key, model request or provider call |

### Permission and drift risk

- The dump preserves permission `!!js` source but cannot show the value selected when the process or Session runs.
- An unrecorded profile-local or home-level patch can change the effective tree without changing the shipped bundle. `--dump-default-config` intentionally hides those user layers; compare it with `--dump-config` when auditing drift.
- A CLI overlay can replace a row's whole `config`; retained fields must be restated. The Cordis example changes `webserver` from dynamic expressions to static `127.0.0.1:3081`.
- Provenance is diagnostic, not validation. The observed dump rendered a duplicate id; only activation rejected it.
- Permission changes have two planes in this version: process/config defaults and Session/per-call policy. A config receipt alone cannot prove the Session's effective mode or OS enforcement.

### Evidence classification

| Observation | Classification | Claim boundary |
|---|---|---|
| Two built-CLI dumps and their hashes | `EFFECTIVE_CONFIG_DUMP_CONFIRMED` | Pinned build, isolated empty user layers |
| Profile auto-materialization | `PRODUCT_ARTIFACT_RUNTIME_CONFIRMED` | Generated manifests/files only |
| Web/headless shared and differing ids | `EFFECTIVE_CONFIG_DIFF_CONFIRMED` | Configuration rows, not behavior of every plugin |
| Optional overlay port replacement | `OVERLAY_DIFF_CONFIRMED` | Dump-time composition |
| Duplicate runner rejected on `--help` boot | `REPO_OWNED_OVERLAY_DRIFT_RUNTIME_CONFIRMED` | Pinned overlay + pinned web Profile |
| Missing overlay exit `1` | `INVALID_OVERLAY_NEGATIVE_CONFIRMED` | File-read failure path |
| Real Local FS write tool test | `FS_CAPABILITY_SEAM_TEST_RUNTIME_CONFIRMED` | No model and no OS confinement Provider |
| Permission transition tests | `TEST_FIXTURE_RUNTIME_CONFIRMED` | Fake confining FS; real policy/consumer protocol |
| Real model/provider/security/cost | `NOT_TESTED` | No credential, network, token or production workload |

## 4. Final integrity check

```text
DSH HEAD: cd5ef8148158c3a752a658978873241fdf8e2bbc
tag target: cd5ef8148158c3a752a658978873241fdf8e2bbc
git status --short rows: 0
git diff --stat: empty
git diff --cached --stat: empty
```

The external DSH fixture remained exact and clean. Raw captures and materialized Profiles remain below the isolated lab root for this transaction only; they contain no credential values and are outside both Git repositories.
