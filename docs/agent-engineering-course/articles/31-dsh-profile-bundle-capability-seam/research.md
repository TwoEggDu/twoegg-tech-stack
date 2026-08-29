# Article 31 Research

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## 1. Merged boundary and inputs

本篇回答一个可证伪的问题：DSH 如何把 Bundle、Profile 与 overlay 组合成 Effective Configuration；配置中的 FS Capability 又怎样从 Service Definition 经 Provider 连接到 Tool Consumer。重点不是“YAML 里有一行”，而是区分 config row、effective config、activation 与一次真实 operation。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Environment：Windows 10 `10.0.19045` x64；Node `v24.18.1`；project pnpm `11.7.0`

本轮合并的 durable inputs：

- `repository-map.md`：Profile/Bundle validation、真实 layer precedence、Web/Headless topology、FS definition/provider/consumer ownership；
- `call-path.md`：argv 到 Effective Config、patch conflict、shipped `FileSystem -> SandboxedFileSystem -> ToolFs` 的 exact source arrows；
- `experiments/effective-config-diff.md`：built CLI dumps、repo-owned overlay drift、activation rejection、missing overlay negative、owner tests、Local FS operation 与 permission fake boundary。

Source Investigator 与 Lab 均确认 DSH fixture 最终仍为 pinned commit 且 clean。实验未读取 credential value、未发起模型或 Provider 网络请求、未开放 Web listener，也未修改 DSH tracked source。

## 2. Problem Space -> Abstract Model -> Concrete landing

### Problem Space

把配置声明写成运行事实会制造三类误判：

1. dump 中存在 `fs-sandbox`，就声称 Sandboxed FS 已构造并执行；
2. overlay dump 成功，就声称 overlay 能启动；
3. dump 保存了 permission `!!js`，就声称知道 Session 最终权限。

本轮实验给出直接反例：repo-owned overlay 的 dump exit `0`，却包含 duplicate id；同一 overlay 的 activation probe exit `1`。因此 provenance 是诊断材料，不是 activation certificate。

### Abstract Model

```text
shipped Profile template
  -> materialized mutable Profile
  -> ordered Bundle patches
  -> profile-local patch
  -> home-level patch
  -> argv-ordered --patch overlays
  -> launcher hard switch at boot
        |
        v
boot-free Effective Entry List + provenance
        |
        v
Loader import + dependency settlement + Service publication
        |
        v
Consumer registration + one observed operation
```

四层证据必须分开：

| Evidence layer | What it can prove | What it cannot prove alone |
|---|---|---|
| Config row | 某 layer 声明了 id/name/config | 最终值、Provider active |
| Effective Config dump | patch 算法后的 rows、provenance、静态 `!!js` source | 表达式求值、依赖结算、duplicate-id activation |
| Activation | Loader 接受 entry、Provider/Consumer dependency settled | 某个 operation 成功、OS sandbox 完整性 |
| Operation | 该次调用到达某 Provider/Consumer 并产生 observed result | shipped Profile 的其它 row、真实模型或生产安全 |

### Concrete landing：FS Capability Seam

中央 seam 收敛为 FS，因为 source 三端闭合，并有一条不冒充 shipped Provider 的可执行 owner test：

```text
Service Definition (source)
  FileSystem extends Service -> super(ctx, 'fs')

Shipped Provider (source/config)
  dsh-base row fs-sandbox
  -> SandboxedFileSystem extends LocalFileSystem
  -> inject=['sandboxPolicy']
  -> publishes ctx.fs

Consumer (source/config)
  dsh-base row tool-fs
  -> inject=['tools','fs','systemPrompt']
  -> read/write/edit call ctx.fs.*

Executed owner test (runtime, different provider)
  LocalFileSystem -> ToolRuntime/FsPolicy -> ToolFs
  -> ctx.tools.execute(write)
  -> disk readback equals requested bytes
```

必须保持两个结论：

- shipped topology 是 `FileSystem definition -> SandboxedFileSystem provider -> ToolFs consumer`，由 pinned source/config 确认；
- 实验真正运行的是 `LocalFileSystem implementation/provider -> ToolFs`，不是 shipped `SandboxedFileSystem` activation，更不是 OS confinement 证明。

## 3. Profile, Bundle and overlay contract

### 3.1 Distributed schema

Pinned source 没有一个单独的 Zod-style “Profile schema”。验证分散在：

- `DshProfileManifest`：ordered `bundles` 与 `patchReload: live | startup`；
- `DshBundleManifest`：`dsh.bundle.patch`；
- `readProfileManifest/loadProfile`：JSON object、enum、bundle resolution 和 missing declaration checks；
- `entryListSchema/parsePatchList`：顶层 array、Loader row/patch dialect 与 `!!js`；
- activation 时各 plugin Config schema 与 dependency settlement。

因此 TypeScript interface 只描述 shape，不等于全部 runtime validation。

### 3.2 Proven precedence

普通 boot 的低到高 precedence：

```text
bundles in dsh.profile.bundles order
< $DSH_HOME/profiles/<name>/cordis.patch.yml
< $DSH_HOME/cordis.patch.yml
< --patch files in argv order
< DSH_TELEMETRY_DISABLED derived hard patch, when applicable
```

`--dump-config` 使用相同 `applyEntryPatches` 组合 bundle/profile/home/CLI layers，但不追加 boot-only telemetry patch，不 boot，也不求值 `!!js`。`--dump-default-config` 只保留 bundle layers，故意跳过 user/home/argv layers，适合 recovery，不是 run receipt。

### 3.3 Conflict rules

`applyEntryPatches` 的实际规则：

- cloned input 上按序应用 patch；
- unique id 的 later override 覆盖 top-level field；`config` 整体替换，不 deep merge；
- inserted row 立即加入 id index，later patch 可命中；
- missing id、non-group insert target、name mismatch 都 warning + skip；
- duplicate insert 不去重：两个 structural rows 都保留，id index 指向最后一个。

所以“last layer wins”只适用于被正确命中的 unique row，不能推广成结构去重或 activation 一定成功。

## 4. Effective Config observations

### 4.1 Materialized Profiles

在 isolated `DSH_HOME` 中，第一次 dump 物化：

| Profile | Bundles | patchReload | Manifest SHA-256 |
|---|---|---|---|
| headless | `dsh-base`, `dsh-headless` | `startup` | `104F43C27B3521E3B548FC3E5088DB93AF4F065F543860624005915F16E9D679` |
| web | `dsh-base`, `dsh-web-app` | `live` | `B07F71CBA5C341F6BD6EBF1316573588817E3A15629A86F11D34D4A01A82C94B` |

两个 profile-local `cordis.patch.yml` 都是注释加 `[]`，SHA-256 均为 `EF189A8C27DB6D63930AA3046A3040482E952EAFCB7487C644D508E8D461F027`。

### 4.2 Dump receipts

| Dump | Exit | Bytes | Rows | Unique ids | SHA-256 |
|---|---:|---:|---:|---:|---|
| `headless --dump-config` | 0 | 11,227 | 89 | 89 | `7B00D284956107355C44629B861C1754A570835AE04F44F9AA15E9586ECA5298` |
| `web --dump-config` | 0 | 16,558 | 144 | 144 | `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A` |
| `web --dump-default-config` | 0 | 16,558 | 144 | 144 | `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A` |

Web effective/default byte-identical只证明本次 freshly materialized Profile 的 local patch 为空、home patch absent；不证明两个命令通常等价。

Headless 与 Web 共享 `87` 个 unique ids，包括 `sandbox`、`sandbox-policy`、`approval`、`tool-bash`、`tool-pwsh`、`tool-fs`、`fs-sandbox`。Headless-only 是 `headless-startup/headless-runner`；Web 有 `57` 个 headless absent ids，包括 `web-startup`、`webserver`、`cordis-host-runner`、`web-runtime`、connection、API controllers、workspace 和 UI rows。

这证明两个 Profile 共享 Base capability stack，并在外层 Host/App bundle 分叉；不证明每个共享 plugin 的 runtime behavior 相同。

## 5. Drift and negative evidence

### 5.1 Repo-owned overlay collision

Overlay：`apps/cli/config/examples/cordis/cordis.yml`，`781` bytes，SHA-256 `62F0D905D430F7A1A517125AAE8EE5786EABCC9B77FA362AEA16A9104A0EFD31`。

| Dump | Exit | Bytes | Rows | Unique ids | SHA-256 |
|---|---:|---:|---:|---:|---|
| `web --dump-config --patch <cordis overlay>` | 0 | 16,827 | 146 | 145 | `679CC5ED39C53FDBB2D6A57014DF4486FA274A0E3250D98AF4223DED6C6D76E9` |

Overlay 把 `webserver` 整体 config 替换为 `127.0.0.1:3081`，新增 unique `tool-cordis`，同时再次 insert 已存在的 `cordis-host-runner`。因此 rows `146`、unique ids `145`。dump 没有拒绝 duplicate；activation probe：

```text
web --patch <cordis overlay> --help
exit 1
duplicate loader entry id: cordis-host-runner
```

这条 counter-evidence 必须进入正文：Effective Config dump 能暴露结构异常，但不是 Loader acceptance certificate。Pinned repo 的 optional overlay 在该 Web composition 路径上已漂移。

### 5.2 Missing overlay

确认路径不存在后运行 `web --dump-config --patch <missing-overlay.yml>`：exit `1`、stdout empty，stderr 包含 labelled `failed to read overlay ... ENOENT`。Named CLI overlay 缺失是 fatal configuration error，不等于 optional user patch absent。

### 5.3 Owner tests and bounded activation smokes

- config dump owner suite：`6 passed / 6`，覆盖 ordered overlay、provenance、boot algorithm equality、absent-target warning、default warning sink 与 invalid base；
- unmodified `headless --help`：exit `0`，stdout `339` bytes，SHA-256 `DA8F354AAC509B233168982023F40FBAC43EF8E52615606F6FC82BB743913037`；
- unmodified `web --help`：exit `0`，stdout `759` bytes，SHA-256 `8DBDE71AA3D2FACDA865ED27789F5E888D893003B57D3307D93DEE8E1A81D6D8`。

Help probes 只证明 credential-free product Profile path 能到 mode-owned help/exit；没有 task、model call、server listener 或 long-running Host。

## 6. Capability and permission runtime boundary

### 6.1 Real Local FS Provider -> ToolFs operation

Exact integration test：`1 passed / 32 skipped`。它创建临时目录，挂载真实 `LocalFileSystem`、`ToolRuntime`、`FsPolicy` 与 `ToolFs`，经 `ctx.tools.execute` 执行 write，再从磁盘回读 exact bytes。

可写：`FS_CAPABILITY_SEAM_TEST_RUNTIME_CONFIRMED / LocalFileSystem -> ToolFs`。

不可写：`shipped SandboxedFileSystem activated`、`Profile Agent turn used FS`、`OS sandbox enforced`。

### 6.2 Permission protocol with fake FS

四个 targeted cases 全部通过：plain default `1/1`；standing override、approved escalation、rejected escalation `3/3`。真实部分是 `SandboxPolicyService`、Tool Runtime、FS policy、ToolFs 和 approval protocol；Provider 是记录 policy 的 `SandboxingFakeFs`。

观测到：

- default：`workspace-write` + calling Session workspace root；
- standing override：`read-only`；
- approved one-call escalation：`danger-full-access`；
- rejected escalation：fail closed，fake provider mutation count 不增加。

标签只能是 `PERMISSION_PROTOCOL_TEST_FIXTURE_RUNTIME_CONFIRMED`。它不证明 Windows ACL、Landlock、Seatbelt 或 shipped `SandboxedFileSystem` 的真实 enforcement。

另外，dump 中 `sandbox-policy` / `approval` 的 `!!js` 仍是 source expression。它不等于 process expression 已求值，更不等于 Session/per-call policy active。

## 7. Final Claim register

| Claim ID | Falsifiable claim | Final status | Evidence | Wording ceiling |
|---|---|---|---|---|
| `31-C01` | Article 31 source/lab 全部绑定 official frozen revision，Lab 后 fixture clean。 | `CONFIRMED` | `31-E01` | identity 不证明机制 |
| `31-C02` | Profile、Bundle 与 patch validation 是 distributed contract，且 named bundle/CLI overlay missing 会 fail loud。 | `CONFIRMED` | `31-E02` | interface 不等于全部 runtime schema |
| `31-C03` | Boot layer precedence 是 bundles -> profile -> home -> CLI -> telemetry hard patch；dump 有明确子集。 | `CONFIRMED` | `31-E03` | precedence 不等于 patch 命中 |
| `31-C04` | Patch 对 unique id 做 top-level replacement；config 非 deep merge，miss/mismatch skip，duplicate insert 不去重。 | `CONFIRMED` | `31-E04` | 不概括为无条件 last-wins |
| `31-C05` | built CLI 生成 89-row Headless 与 144-row Web Effective Config receipts，dump 不 boot/不求值 `!!js`。 | `CONFIRMED` | `31-E05` | dump != activation |
| `31-C06` | Repo overlay dump exit0 且 duplicate id，activation exit1；configured/effective/active 必须分层。 | `CONFIRMED` | `31-E06` | 只限所测 overlay/path |
| `31-C07` | 两份 dump 共享 87 ids/Base capability stack；Headless 与 Web 在 surface bundle 分叉。 | `CONFIRMED` | `31-E07` | row-set diff != 全 runtime diff |
| `31-C08` | Current Headless 没有 Web Host rows；Web 拥有 Host/API/UI，Headless 是 cmdline one-shot runner。 | `CONFIRMED` | `31-E08` | process 不自动等于 repo Host concept |
| `31-C09` | Shipped FS seam 的 source topology 是 FileSystem definition -> SandboxedFileSystem provider -> ToolFs consumer。 | `CONFIRMED` | `31-E09` | source/config confirmed，不声称 runtime active |
| `31-C10` | 实验 operation 闭合 LocalFileSystem provider -> ToolFs consumer，并回读 exact bytes。 | `CONFIRMED` | `31-E10` | 不冒充 shipped SandboxedFileSystem |
| `31-C11` | Permission targeted tests 闭合 policy/approval/consumer handoff，但 Provider 为 fake，未证明 OS enforcement。 | `CONFIRMED` | `31-E11` | test-fixture only |
| `31-C12` | Default dump 是 bundle-only recovery view；local/CLI overlay 会漂移，missing named overlay exit1 ENOENT。 | `CONFIRMED` | `31-E12` | fresh empty local layer byte-equality 不可推广 |
| `31-C13` | BuildPilot 应采用显式 Capability Set 与 provenance/activation receipt。 | `PROPOSAL` | `31-E13` | Part VII 未实现 |
| `31-C14` | BuildPilot 首个 Profile 应为 read-only，写能力 absent/denied by default。 | `PROPOSAL` | `31-E14` | future policy/runtime tests required |
| `31-C15` | BuildPilot 应 DEFER arbitrary layering、live reload、runtime replacement 与 multi-Host。 | `PROPOSAL` | `31-E15` | 有真实需求后重审 |

最终统计：`15 Claims = 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。

## 8. BuildPilot transfer decision

| Mechanism | Decision | Evidence boundary |
|---|---|---|
| Explicit Capability Set | `ADOPT` | provider、consumer、permission、source/version 与 activation receipt |
| Read-only Profile | `ADOPT` | 第一版只开放观察/诊断 capability，写能力默认 absent/denied |
| Effective Config receipt/diff | `ADOPT` | dump 与 activation result 分列，duplicate/missing 保留 |
| Arbitrary YAML/`!!js` layering | `DEFER` | replacement/duplicate/drift 成本已实证，需求尚未成立 |
| Live reload/runtime replacement | `DEFER` | 先用 immutable run profile 与显式 composition root |
| Multi-Host composition | `DEFER` | CLI-first，没有第二 Host 的已证需求 |

`ADOPT` 是 Part VI 课程设计输入，不是 BuildPilot ADR、代码或 runtime 完成声明。

## 9. Retained counter-evidence and harness mistakes

1. Repo-owned Cordis overlay 的 dump 成功、activation 失败；不得删除或柔化。
2. Missing named overlay 是 `ENOENT / exit1`；optional profile/home user patch absent 才可为空。
3. Permission tests 使用 fake FS；不得写成 OS sandbox confirmation。
4. Real FS operation 使用 `LocalFileSystem`；不得写成 shipped `SandboxedFileSystem` active。
5. Web effective/default byte-identical只限 fresh empty user layer。
6. 第一次 `corepack pnpm --version` 在错误 cwd 触发 registry `EACCES`；corrected fixture-local command exit0。
7. `$home` 与 PowerShell automatic `HOME` 冲突的首次编排失败，以及 `Start-Process -ArgumentList` flatten 导致四次缺 `--profile`，均属 harness mistakes，不是 product failure；有效结果来自 corrected direct argument arrays。

## 10. Evidence Merge result

`EVIDENCE_MERGE PASS / OUTLINE ELIGIBLE`。

15 个 Claim 与 15 张 Evidence Card 已一一收敛为 `12 CONFIRMED / 3 PROPOSAL / 0 BLOCKED`。两份 shipped Profile dump、repo overlay drift、activation rejection、missing overlay、owner tests、shipped FS source seam、real Local FS runtime seam 与 permission fake boundary 都有独立 traceability。中心命题已在“pinned source + effective config receipts + bounded owner runtime”范围闭合；real model/provider、shipped SandboxedFS activation、OS sandbox、production security、token/cost 均明确未证。Next allowed gate：`OUTLINE`。
