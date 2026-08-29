# Article 31 Outline

Status: `OUTLINE COMPLETE / DRAFT ELIGIBLE`

## 0. Article type and governing argument

- Type: `PRINCIPLE / SOURCE ARCHITECTURE`。
- Length: `M / Standard Core Lesson`；围绕一条配置链和一条 FS Capability Seam 收口，不扩写成 DSH 配置手册或 Provider 百科。
- Progression: `problem space -> abstract model -> concrete DSH implementation -> counterexample -> engineering boundary -> BuildPilot transfer`。
- Central question: 一个 Profile 名称、一个 Provider row 或一份 Effective Config dump，究竟能证明 Capability 已经走到了哪一层？
- Shortest judgment:

  > Profile 决定的是候选组合；只有把 Effective Config、activation result 与 operation receipt 分层记录，才能知道 Capability 是否真的可用。

- Narrative anchor: repo-owned Cordis overlay 的 dump `exit 0`，却留下 duplicate id；同一 overlay 的 activation probe `exit 1`。开篇用这组矛盾立问题，之后再解释 schema、layer、seam 与 permission，而不是从 API/文件清单起笔。
- Provider vocabulary: 本篇的 `Provider` 主要指发布 `ctx.fs` 的 Capability Service Provider，不是 LLM Provider。本文没有真实模型 Provider、credential、网络、token 或 cost 证据。

## 1. Publication contract

### 1.1 Planned frontmatter

```yaml
---
title: "Profile、Bundle、Provider 与 Capability Seam"
slug: "agent-engineering-31-dsh-profile-bundle-capability-seam"
date: "2026-08-30T00:00:00+08:00"
description: "从两份 Effective Config 与一个 dump 成功、activation 失败的反例出发，解释 DSH 如何组合 Profile、Bundle 与 overlay，并沿 FS Service Definition、Provider、Consumer 与 operation 分层验证 Capability。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Configuration"
  - "Capability"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 320
weight: 3320
---
```

### 1.2 Navigation plan

- Opening previous link: `[Everything is a Plugin：插件内核如何承载 Capability 与生命周期]({{< relref "ai-empowerment/agent-engineering-30-dsh-plugin-core.md" >}})`。
- Opening course link: `[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})`。
- Bottom navigation repeats previous + course index，形成顶部进入、底部返回的双向阅读路径。
- Article 32 尚未发布；本 transaction 不创建 future `relref`。结尾只用无链接文字说明下一篇将进入 System Prompt Assembly / PromptContext。Article 32 发布时再独立回填 Article 31 的 next link。

### 1.3 Pinned official source links to use

所有 DSH 事实绑定 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1) 与完整 commit [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。正文按讨论对象就近链接，不集中堆在开篇：

- Profile / Bundle contract: [`packages/boot/app-boot/src/profile.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/boot/app-boot/src/profile.ts)。
- CLI layer collection and boot composition: [`apps/cli/src/args.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/args.ts)、[`profile-boot.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/profile-boot.ts)、[`dump-config.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/dump-config.ts)。
- Patch dialect and conflict algorithm: [`vendor/include/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/vendor/include/src/index.ts)。
- Shipped composition: [`dsh-base`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/base/cordis.patch.yml)、[`dsh-headless`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/headless/cordis.patch.yml)、[`dsh-web-app`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/web-app/cordis.patch.yml)。
- FS seam: [`FileSystem`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/fs/src/index.ts)、[`LocalFileSystem`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/fs-local/src/index.ts)、[`SandboxedFileSystem`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/fs-sandbox/src/index.ts)、[`ToolFs`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/tool-fs/src/index.ts)。
- Owner tests: [`config-dump.spec.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/boot/app-boot/tests/config-dump.spec.ts)、[`integration.spec.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/tool-fs/tests/integration.spec.ts)、[`tools.spec.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/tool-fs/tests/tools.spec.ts)。

## 2. Evidence and wording contract

- Evidence register: `15 / 15 Claims`、`15 / 15 Evidence Cards`、`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。
- Identity: official DSH fixture pinned at commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`；Lab 前后 clean。Identity 只固定研究对象。
- Runtime/artifact evidence:
  - built CLI Headless/Web/default dumps；
  - repo overlay dump and activation counterexample；
  - missing overlay negative；
  - config dump owner suite `6/6`；
  - real `LocalFileSystem -> ToolFs` targeted operation `1/1`；
  - permission protocol targeted cases `4/4`，但 FS Provider 是 `SandboxingFakeFs`。
- Four evidence layers must never collapse:

  ```text
  CONFIG ROW
    -> EFFECTIVE CONFIG
    -> ACTIVATION
    -> OPERATION
  ```

- Mandatory wording ceilings:
  - dump row 不写成 Provider active；
  - dump `!!js` source 不写成 process/Session/per-call selected permission；
  - shipped source topology 不写成 shipped runtime activation；
  - real `LocalFileSystem -> ToolFs` test 不冒充 shipped `SandboxedFileSystem`；
  - fake permission Provider 不写成 Windows ACL、Landlock、Seatbelt 或 OS confinement；
  - row-set diff 不写成 Headless/Web 的全部 runtime difference；
  - BuildPilot `ADOPT/DEFER` 只写为 Part VI design proposal，不写成 ADR、code 或 runtime。

## 3. Opening｜为什么 Profile 名称和 Provider row 都不是运行收据

### Purpose

从项目里最常见的误诊切入：看到 `web` Profile、`fs-sandbox` row、`tool-fs` row 与成功 dump，就直接说“Web Host 与文件能力已经起来了”。

### Planned flow

1. 用三个短问题建立读者直觉：
   - Profile 名叫 `web`，是否等于 Web Host 已监听？
   - dump 中有 `fs-sandbox`，是否等于 `ctx.fs` 已发布？
   - permission 行是 `workspace-write` 表达式，是否等于这次 Session 真有该权限？
2. 立即给出反例：repo-owned overlay dump `exit 0`，activation 却因 duplicate `cordis-host-runner` 以 `exit 1` 结束。
3. 提出本文任务：先重建 Effective Config，再沿一个 FS Definition/Provider/Consumer seam 逐层验证。
4. 简短交代 pinned version、claim count 与 no-credential/no-model/no-network 边界；不要在第一屏铺源码清单。

### Claim coverage

`31-C01`、`31-C05`、`31-C06`。

## 4. Abstract model｜配置来源、运行状态与能力路径是三条轴

### 4.1 Configuration composition axis

用一张小图建立全文主公式：

```text
shipped Profile template
  -> materialized mutable Profile
  -> ordered Bundle patches
  -> profile-local patch
  -> home-level patch
  -> argv-ordered --patch overlays
  -> boot-only telemetry hard patch
       |
       v
Effective Entry List + provenance
```

随后明确：`--dump-config` 组合的是 bundle/profile/home/CLI 的静态子集；不 boot、不求值 `!!js`，也不追加 boot-only telemetry hard patch。`--dump-default-config` 则只保留 bundle layers，是 recovery view，不是 effective run receipt。

### 4.2 Runtime evidence axis

规划四列表格：

| Layer | Minimum evidence | It proves | It cannot prove alone |
|---|---|---|---|
| Config row | layer 声明 | requested plugin/config | final value / active service |
| Effective Config | composed rows + provenance | static resolution result | expression evaluation / Loader acceptance |
| Activation | Loader/service settlement | provider/consumer active in selected generation | an operation succeeded |
| Operation | captured call/result | selected path executed | all other rows / production safety |

### 4.3 Capability seam axis

先用引擎无关模型定义：

```text
Service Definition
  -> Provider publishes capability
  -> Consumer injects capability
  -> Operation crosses the seam
```

把 `Profile / Bundle / Patch` 定位为 composition inputs，把 `Definition / Provider / Consumer / Operation` 定位为 capability availability path。配置组合与能力路径相交，但不是同一条链。

### Claim coverage

`31-C03`、`31-C05`、`31-C09`、`31-C10`。

## 5. Concrete DSH contract｜所谓 Profile schema 其实分散在四个 gate

### Purpose

回答 Required Question 1，不把 TypeScript interface 错写成完整 runtime schema。

### Planned structure

1. `DshProfileManifest`：ordered `bundles` + `patchReload: live | startup`。
2. `DshBundleManifest`：`dsh.bundle.patch` 指向 bundle patch。
3. `readProfileManifest/loadProfile`：JSON object、profile name、enum、bundle resolution、missing declaration/file checks。
4. `entryListSchema/parsePatchList`：top-level array、Loader entry/patch dialect、`!!js` source。
5. activation-time plugin Config schema 与 dependency settlement：只有到这一层，row 才接受插件自己的 runtime validation。

插入一张“声明 gate / 失败行为 / 仍未证明”表。特别保留：

- optional profile/home user patch 可以不存在；
- manifest 声明的 bundle patch 与 named CLI overlay 缺失则 fail loud；
- missing CLI overlay 的实测是 `exit 1`、labelled `ENOENT`、empty stdout；
- source 不存在单一 Zod-style Profile schema。

### Claim coverage

`31-C02`、`31-C12`。

## 6. Concrete DSH composition｜Base Bundle + Profile + Overlay 怎样形成 Effective Config

### 6.1 Proven precedence

按低到高写出普通 boot：

```text
bundles in manifest order
< profile-local cordis.patch.yml
< $DSH_HOME/cordis.patch.yml
< --patch files in argv order
< telemetry hard patch when applicable
```

说明 `prepareProfile()` 先清空 root `cordis.yml`，避免 Loader write-back 被误认成 base layer；`composeEntries()` 与 boot 共同使用 `applyEntryPatches()`，但 dump 与 boot 的层集合和执行动作仍不同。

### 6.2 Conflict semantics

用三个最小例子而不是 API 罗列：

1. **Matched unique id**：later layer top-level replacement；`config` 整体替换，不 deep merge。
2. **Missing id / name mismatch**：warning + skip，不创建 row，也不能说“后层赢了”。
3. **Duplicate insert**：两个 structural rows 都保留，index 指向后来者；不是 dedup。

正文必须纠正一句粗糙口号：

> “last layer wins”只对正确命中的 unique row 成立；它不表示 miss 会创建、config 会 deep merge，或 duplicate 会自动消失。

### 6.3 Owner-test support

简要带入 config dump owner suite `6/6`：ordered overlay、provenance、与 boot patch algorithm equality、absent-target warning、default warning sink、invalid base。Owner suite 是 algorithm fixture evidence；built CLI dumps 是 product artifact evidence，二者不互相替代。

### Claim coverage

`31-C03`、`31-C04`。

## 7. Two Effective Config receipts｜共享 Base，不共享 Host surface

### 7.1 Materialized Profiles

先给 Profile manifest 小表：

| Profile | Bundles | patchReload |
|---|---|---|
| headless | `dsh-base + dsh-headless` | `startup` |
| web | `dsh-base + dsh-web-app` | `live` |

保留 manifest SHA 与两个空 `cordis.patch.yml` 的 SHA 放在复现/脚注式信息，不让 hash 打断主线。

### 7.2 Dump receipts and diff

正文主表：

| Dump | Exit | Rows | Unique ids | Main observation |
|---|---:|---:|---:|---|
| headless effective | 0 | 89 | 89 | Base + one-shot Headless surface |
| web effective | 0 | 144 | 144 | Base + Web Host/API/UI surface |
| web default | 0 | 144 | 144 | 仅本次 fresh empty user layers 下与 effective byte-identical |

精确 SHA 放在同表或紧邻复现块：

- Headless: `7B00D284956107355C44629B861C1754A570835AE04F44F9AA15E9586ECA5298`。
- Web effective/default: `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A`。

### 7.3 Shared core and surface fork

- `87` shared unique ids；共同包含 `sandbox`、`sandbox-policy`、`approval`、`tool-bash`、`tool-pwsh`、`tool-fs`、`fs-sandbox`。
- Headless-only: `headless-startup`、`headless-runner`。
- Web-only set: `57` ids，代表项包括 `web-startup`、`webserver`、`cordis-host-runner`、`web-runtime`、controllers、workspace、UI。
- 历史 `[base, web-app, headless]` tuple 只是 installation-owned migration input；current headless 被规范化为 `[base, headless]`。
- `headless --help` 与 `web --help` 均 exit `0`，只证明 mode-owned help path；没有 task、model call、listener 或 long-running Host。

本节结论压成：Web/Headless 共享 core capability composition，在 Host/App surface bundle 分叉；“进程存在”不自动等于 repo 里的 Web Host 概念。

### Claim coverage

`31-C05`、`31-C07`、`31-C08`、`31-C12`。

## 8. Counterexample｜dump 成功，activation 为什么仍然失败

### Purpose

用一条完整负例把“Effective Config != active runtime”钉住，作为全文中段转折。

### Planned sequence

1. 固定 repo-owned overlay：`apps/cli/config/examples/cordis/cordis.yml`，`781 bytes`，SHA `62F0D905D430F7A1A517125AAE8EE5786EABCC9B77FA362AEA16A9104A0EFD31`。
2. dump observation：`exit 0`、`146 rows / 145 unique ids`、SHA `679CC5ED39C53FDBB2D6A57014DF4486FA274A0E3250D98AF4223DED6C6D76E9`。
3. visible change：`webserver` whole config 被替换为 `127.0.0.1:3081`；新增 unique `tool-cordis`。
4. collision：overlay 又 insert 了 shipped Web 已存在的 `cordis-host-runner`；dump 保留 duplicate。
5. activation probe：同一 overlay 经 `web --patch ... --help` 进入 Loader application，`exit 1`，deepest cause 是 `duplicate loader entry id: cordis-host-runner`。
6. interpretation：provenance 帮助定位 drift，但不是 structural validation 或 activation certificate。

紧接 missing named overlay negative：路径先确认不存在，dump `exit 1`、stdout empty、stderr labelled `ENOENT`。把它与“optional user patch absent”分开。

### Claim coverage

`31-C04`、`31-C06`、`31-C12`。

## 9. FS Capability Seam｜源码拓扑与运行实验必须分账

### 9.1 Service Definition / Provider / Consumer

先沿 pinned source 画 shipped topology：

```text
FileSystem extends Service
  -> super(ctx, 'fs')
  -> dsh-base row: fs-sandbox
  -> SandboxedFileSystem extends LocalFileSystem
  -> publishes ctx.fs after sandboxPolicy dependency settles
  -> dsh-base row: tool-fs
  -> ToolFs injects tools + fs + systemPrompt
  -> read/write/edit call ctx.fs.*
```

分别解释三个 owner：

- Definition 规定 `resolve/readText/writeText/editText` capability contract，不自行做 host I/O。
- Shipped Provider 是 `SandboxedFileSystem` row；`LocalFileSystem` 是其实现继承基座，不是 dsh-base 中第二个并列 provider row。
- Consumer `ToolFs` 等依赖 settled 后注册 read/write/edit；row order 没有 activation semantics。

### 9.2 State ladder

规划一个四行表：

| State | Required evidence | Allowed wording |
|---|---|---|
| Configured | effective rows contain `fs-sandbox` + `tool-fs` | Profile asks Loader to mount topology |
| Provider active | instance constructed + `ctx.fs` resolves | capability published in selected generation |
| Consumer active | injections settle + tools register | FS tools registered against active provider |
| Operation observed | captured `ctx.fs.*` call/result | selected operation executed |

### 9.3 Runtime experiment: a different Provider

必须单独写成另一条链：

```text
LocalFileSystem
  + ToolRuntime
  + FsPolicy
  + ToolFs
  -> ctx.tools.execute(write)
  -> disk readback equals exact requested bytes
```

结果：targeted owner integration `exit 0 / 1 passed / 32 skipped`。准确 label 是 `FS_CAPABILITY_SEAM_TEST_RUNTIME_CONFIRMED / LocalFileSystem -> ToolFs`。

紧接反向边界：这不是 shipped Profile activation，不是 `SandboxedFileSystem` runtime，也不是 Agent/model turn 或 OS confinement 证明。

### Claim coverage

`31-C09`、`31-C10`。

## 10. Permission is a separate plane｜source expression、Session policy 与 enforcement

### Purpose

防止读者看到 dump 中的 `!!js` 或测试名，就把 permission 写成安全保证。

### Planned flow

1. 展示最小 `sandbox-policy` / `approval` `!!js` 片段，强调 dump 保存 source expression，不做 evaluation。
2. 区分三层：process/config default、Session standing override、per-call escalation result。
3. 总结 targeted tests：
   - default `workspace-write` + calling Session root；
   - standing override `read-only`；
   - approved escalation `danger-full-access`；
   - rejected escalation fail closed，provider mutation count 不增加。
4. 明示 test shape：真实 `SandboxPolicyService`、Tool Runtime、FS policy、ToolFs 与 approval protocol，外围 Provider 是记录 policy 的 `SandboxingFakeFs`。
5. 结论 label：`PERMISSION_PROTOCOL_TEST_FIXTURE_RUNTIME_CONFIRMED`；不证明真实 approval UI、Windows ACL、Landlock、Seatbelt、shipped `SandboxedFileSystem` 或 security completeness。

### Claim coverage

`31-C11`、`31-C14`。

## 11. Engineering risks｜真正危险的不是“配置多”，而是来源与证据层混淆

按三个风险组织，不再重复 source path：

### 11.1 Config drift

- shipped bundle 不变，不代表 mutable Profile/home/CLI layer 没变；
- `--dump-default-config` 故意隐藏 user layers，必须与 effective dump 比较；
- repo-owned overlay 本身也可能相对当前 shipped composition 漂移。

### 11.2 Whole replacement and silent skip

- whole `config` replacement 要求 overlay 重述它拥有的全部 key；
- missing id/name mismatch warning + skip 可能留下“文件改了、effective row 没改”的错觉；
- duplicate 能进入 dump，activation 才 reject，故 dump receipt 必须配 activation result。

### 11.3 Permission drift

- static `!!js`、process env、Session override、per-call escalation 与 Provider enforcement 是不同平面；
- Prompt 中写“只读”不是 authority control；
- receipt 应记录 source/provenance、resolved policy、Provider identity 与 operation outcome，但不得泄露 credential value。

### Claim coverage

`31-C04`、`31-C06`、`31-C11`、`31-C12`。

## 12. BuildPilot transfer｜采纳可审计边界，不复制组合机器

### Planned decision table

| Mechanism | Decision | Minimal contract | Deferred evidence |
|---|---|---|---|
| Explicit Capability Set | `ADOPT` | definition、provider、consumer、permission、source/version、activation receipt | Part VII acceptance tests |
| Read-only Profile | `ADOPT` | first profile only exposes observation/diagnostic capability；write absent/denied by default | negative write tests + provider receipt |
| Effective/default diff and activation receipt | `ADOPT` | composition diff 与 activation result 分列；duplicate/missing retained | BuildPilot implementation |
| Arbitrary layering / `!!js` | `DEFER` | no open-ended user composition in v1 | proven requirement + conflict tests |
| Live reload / runtime replacement | `DEFER` | immutable run profile + explicit composition root first | lifecycle/rebinding requirement |
| Multi-Host | `DEFER` | CLI-first, one Host contract | second Host requirement and parity tests |

正文要明确：

- `ADOPT capability set` 不是照搬 DSH string-key registry；它是把 Capability availability 变成可审计合同。
- `read-only Profile` 的“只读”不能只写在名字或 Prompt 中；写能力应 absent 或由 policy/provider deny，并有 negative operation evidence。
- Article 38 / Part VII 未开始，本节不写 ADR、class、code、runtime 或 migration promise。

### Claim coverage

`31-C13`、`31-C14`、`31-C15`。

## 13. Hands-on verification｜怎样审计一份 Profile，而不把 dump 当完成证明

沿可复用方法写成短实验，不复制 Lab 日志：

1. Freeze official repository、tag、full SHA、toolchain 与 clean fixture。
2. 在 isolated `DSH_HOME` 物化两个 Profile；记录 manifest、bundle order、reload mode 与 local patch identity。
3. 分别保存 default/effective dumps、exit code、bytes、row/unique-id count、SHA 与 provenance。
4. 对 row set 做 shared/headless-only/web-only diff；只解释 composition difference。
5. 加入一个 pinned overlay；同时记录 dump 和 activation outcome。重复 id、warning、missing file 都必须保留。
6. 选择一个 Capability Seam，分别收集 definition、provider、consumer、activation 与 operation evidence。
7. 若 permission test 使用 fake Provider，在结果名和结论里显式写 fixture boundary。
8. 最后重新检查 fixture HEAD、status、diff；不读取 credential value，不发真实 Provider/model request。

### Expected interpretation table

| Observation | Allowed claim | Forbidden upgrade |
|---|---|---|
| dump row present | configured/effective row | provider active |
| effective/default same | selected fresh user layers caused no diff | commands are generally equivalent |
| overlay dump exit0 | static composition rendered | Loader accepts it |
| activation exit1 duplicate | selected overlay drift reproduced | all overlays are invalid |
| Local FS exact write/readback | local provider seam operation executed | shipped SandboxedFS active |
| fake permission cases pass | protocol handoff fixture confirmed | OS sandbox confirmed |

## 14. What this article establishes—and what it does not

### Established

- frozen identity and clean fixture boundary；
- distributed Profile/Bundle/patch validation；
- proven boot precedence and dump/default subsets；
- matched replacement、warning/skip、duplicate insert semantics；
- Headless `89`、Web `144`、shared `87` 的 exact Effective Config receipts/diff；
- current Headless/Web surface fork；
- repo-owned overlay dump success + duplicate activation rejection；
- shipped `FileSystem -> SandboxedFileSystem -> ToolFs` source/config topology；
- executed `LocalFileSystem -> ToolFs` exact write/readback operation；
- permission selection/handoff fixture results；
- BuildPilot `ADOPT capability set/read-only Profile` 与 `DEFER` decisions are proposals。

### Not established

- shipped `SandboxedFileSystem` activated in the Profile probes；
- Web listener、Agent turn、real model or LLM Provider request；
- credential、network、token、cost、latency or production workload；
- `!!js` runtime value or a Session's final permission from dump alone；
- Windows ACL、Landlock、Seatbelt or OS sandbox completeness；
- every DSH overlay/Profile/plugin path or cross-platform generality；
- BuildPilot ADR、implementation、runtime、Article 32 conclusions or Part VII start。

## 15. Claim-to-section coverage matrix

| Claim | Status | Primary landing | Evidence Card | Wording ceiling |
|---|---|---|---|---|
| `31-C01` pinned revision / clean fixture | `CONFIRMED` | Opening、§14 | `31-E01` | identity only |
| `31-C02` distributed schema + missing named file fail loud | `CONFIRMED` | §5 | `31-E02` | interfaces not whole runtime schema |
| `31-C03` ordered layers + dump subset | `CONFIRMED` | §§4、6 | `31-E03` | precedence does not mean target matched |
| `31-C04` replacement / skip / duplicate semantics | `CONFIRMED` | §§6、8、11 | `31-E04` | no unconditional last-wins |
| `31-C05` two built dumps / boot-free / unevaluated `!!js` | `CONFIRMED` | §§3、4、7 | `31-E05` | dump != activation |
| `31-C06` overlay dump success / activation rejection | `CONFIRMED` | §§3、8、11 | `31-E06` | selected overlay/path only |
| `31-C07` 87 shared ids / Base core / surface split | `CONFIRMED` | §7 | `31-E07` | row diff != full runtime diff |
| `31-C08` Headless no Web Host rows / Web owns Host surface | `CONFIRMED` | §7 | `31-E08` | help path != long-running Host |
| `31-C09` shipped FS source topology | `CONFIRMED` | §9 | `31-E09` | source/config, not active runtime |
| `31-C10` LocalFileSystem -> ToolFs operation | `CONFIRMED` | §9 | `31-E10` | do not relabel SandboxedFS |
| `31-C11` permission handoff with fake Provider | `CONFIRMED` | §§10—11 | `31-E11` | fixture only, no OS enforcement |
| `31-C12` default recovery view / local drift / missing overlay | `CONFIRMED` | §§5、7—8、11 | `31-E12` | empty-layer equality not general |
| `31-C13` explicit Capability Set | `PROPOSAL` | §12 | `31-E13` | no Part VII implementation |
| `31-C14` read-only Profile first | `PROPOSAL` | §§10、12 | `31-E14` | future negative tests required |
| `31-C15` defer layering/reload/replacement/multi-Host | `PROPOSAL` | §12 | `31-E15` | conditional defer, not universal rejection |

Coverage result: `15 / 15`。

## 16. Series boundary and handoff

- Article 29 owns Host/Profile/Loader 到 Agent Run 的总图；本篇只深入 Profile composition 与一个 FS Capability Seam。
- Article 30 owns representative plugin lifecycle；本篇不重复 install/register/dispose。
- Article 32 owns System Prompt Assembly / PromptContext；本篇只把 `systemPrompt` 写成 ToolFs consumer dependency，不展开 assembly precedence 或 request diff。
- Article 33—37 的 Loop/Step、Session、Tool full pipeline、Recovery/observability 与 extension mapping 不提前证明。
- Article 38—44、BuildPilot ADR/code/runtime 与 Part VII 保持 `NOT STARTED`。

## 17. Learning Check

1. 为什么 Profile 名称不能证明最终 Effective Config？
2. shipped Profile template 与 materialized mutable Profile 有什么区别？
3. Profile、Bundle、Patch/Overlay 的 validation 为什么是 distributed contract？
4. 普通 boot 的 layer precedence 是什么？dump 为什么只呈现其中一个子集？
5. `config` whole replacement 与 deep merge 的工程后果是什么？
6. missing id、name mismatch 与 duplicate insert 分别怎样处理？
7. `--dump-default-config` 与 `--dump-config` 各适合回答什么问题？
8. Headless 与 Web 为什么可以共享 87 个 ids，却仍拥有不同 Host surface？
9. 为什么 Web effective/default byte-identical 不能推广成一般规律？
10. repo-owned overlay 的 `146 rows / 145 unique ids` 暴露了什么 drift？
11. 为什么 dump `exit 0` 仍不能替代 activation result？
12. Service Definition、Provider、Consumer 与 Operation 四层分别需要什么证据？
13. shipped `SandboxedFileSystem` source seam 与实验 `LocalFileSystem -> ToolFs` 为什么必须分账？
14. permission targeted tests 通过后，为什么仍不能声称 OS sandbox 已验证？
15. `!!js` source expression、Session override 与 per-call escalation 属于哪些不同平面？
16. BuildPilot 为什么应先采用 explicit Capability Set 与 read-only Profile？
17. 为什么 arbitrary layering、live reload、runtime replacement 与 multi-Host 应先 `DEFER`？

## 18. Shortest conclusion

结尾不重复数字清单，回到开篇矛盾：dump 成功只能证明“组合结果可以被打印”，不能证明“能力已经可用”。最短收口：

> 先把配置来源组成可追溯的 Effective Config，再用 activation 和 operation 收据证明 Capability；Profile 名字、Provider row 与成功 dump 都不能替代这条证据链。

Bottom navigation follows §1.2；Article 32 只作无链接的 next-owner 提示。

## 19. Author self-check before DRAFT

- [x] Article type fixed as principle/source architecture。
- [x] Problem space precedes DSH APIs/files。
- [x] Abstract model separates composition、runtime evidence and Capability Seam。
- [x] Profile/Bundle/Patch/Overlay schema、precedence and conflict rules covered。
- [x] Base Bundle + Profile + Overlay -> Effective Config covered。
- [x] Two dumps、87-id diff、default/effective boundary covered。
- [x] dump != activation and repo overlay duplicate counterexample retained。
- [x] Service Definition/Provider/Consumer/Operation seam covered。
- [x] shipped SandboxedFileSystem source seam and executed LocalFileSystem seam strictly separated。
- [x] Web/Headless shared core + Host surface split covered。
- [x] config drift、local overlay and permission risks covered。
- [x] BuildPilot `ADOPT` and `DEFER` scope preserved。
- [x] Standard frontmatter、top/bottom navigation and pinned official links planned。
- [x] Learning Check and shortest conclusion planned。
- [x] `15 / 15` Claims mapped；no real provider/token/cost/OS sandbox claim introduced。
