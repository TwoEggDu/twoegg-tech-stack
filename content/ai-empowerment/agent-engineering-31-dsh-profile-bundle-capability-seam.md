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

# Profile、Bundle、Provider 与 Capability Seam

> **上一篇**：[Everything is a Plugin：插件内核如何承载 Capability 与生命周期]({{< relref "ai-empowerment/agent-engineering-30-dsh-plugin-core.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

Profile 名叫 `web`，是不是就表示 Web Host 已经启动？

Effective Config 里有 `fs-sandbox` 和 `tool-fs`，是不是就表示文件能力已经可用？

配置里写着 `workspace-write`，是不是就表示这次 Session 的写权限已经确定，而且操作系统一定会替我们拦住越界访问？

这三个问题看起来都像在问“配置是什么”，实际上分别跨过了配置组合、Loader activation、Session policy 与 Provider enforcement。把它们压成一句“配置里已经有了”，正是 Agent Harness 最容易出现的诊断误区。

本文在固定版本的 DeepSeek Harness（下称 DSH）里得到了一组很直接的反例：仓库自带的一份 Cordis overlay 可以被 `--dump-config` 正常打印，命令 `exit 0`；但同一份 overlay 进入 Loader activation 后，因为重复的 `cordis-host-runner` id 以 `exit 1` 结束。

也就是说：

```text
dump succeeded != runtime activated
```

要解释这个差异，只看一行 YAML 不够。我们需要先重建 Bundle、Profile 与 overlay 怎样组成 Effective Config，再沿一条真实 Capability Seam 区分 Service Definition、Provider、Consumer 和一次 operation。

如果这篇只记一句话，我建议记这个：

> Profile 决定的是候选组合；只有把 Effective Config、activation result 与 operation receipt 分层记录，才能知道 Capability 是否真的可用。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 是 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。实验前后，外部 fixture 的 `HEAD` 与 tag target 都等于该 SHA，working tree、index 与 diff 为空。这个 identity check 只固定研究对象，不替任何配置或运行结论背书。

本文证据账为 `15 / 15 Claims`、`15 / 15 Evidence Cards`：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。本轮没有读取 credential value，没有真实 LLM Provider、模型请求、网络、token、cost 或生产负载证据。下文里的 Provider 主要指发布 `ctx.fs` 的 Capability Service Provider，不是 LLM Provider。

## 1. “这个 Profile 能做什么”为什么是一个坏问题

Profile、Bundle 和 overlay 都在描述候选组合，但“能做什么”是一个运行问题。

中间至少隔着四层：

| 证据层 | 当前真正知道了什么 | 单靠这一层仍然不知道什么 |
|---|---|---|
| Config row | 某个来源声明了 id、name、config | 最终值、Provider 是否 active |
| Effective Config | patch 算法得到 rows 与 provenance | `!!js` 是否求值、Loader 是否接受 |
| Activation | Loader 接受 entry，Provider/Consumer 依赖结算 | 某次 operation 是否成功 |
| Operation | 一次调用到达选定 seam，并产生结果 | 其他 row、其他 Session、生产安全 |

因此，“Profile 里有文件系统”可能只表示 shipped bundle 声明了两行配置；“文件工具已经注册”需要 Consumer activation 证据；“写文件成功”还需要一次 operation receipt；“文件访问被操作系统安全隔离”则是更高一层、本文没有获得的安全证据。

这四层不是措辞洁癖。repo-owned overlay 的反例说明，它们可以真实地分叉：Effective Config 能被打印，activation 仍然会失败。

同样，Web 与 Headless 的两份 dump 能告诉我们二者共享哪些 rows、在哪个 bundle 分叉，却不能证明每个共享插件在两个 Host 里的 runtime behavior 完全一致。配置差异是诊断入口，不是运行结论全集。

## 2. 先建立一个不依赖 DSH 类名的最小模型

在进入源码之前，先把问题拆成三条轴。

### 2.1 配置来源轴

```text
shipped Profile template
  -> materialized mutable Profile
  -> ordered Bundle patches
  -> profile-local patch
  -> home-level patch
  -> argv-ordered --patch overlays
  -> boot-only hard patch
       |
       v
Effective Entry List + provenance
```

这条轴回答的是：最终候选 row 从哪里来，后来的来源怎样覆盖前面的来源。

### 2.2 能力路径轴

```text
Service Definition
  -> Provider publishes capability
  -> Consumer injects capability
  -> Operation crosses the seam
```

Definition 规定能力合同；Provider 提供实现并发布 service；Consumer 在依赖满足后注册对外行为；operation 才证明某次调用真正穿过了这条 seam。

### 2.3 运行证据轴

```text
CONFIGURED
  -> PROVIDER ACTIVE
  -> CONSUMER ACTIVE
  -> OPERATION OBSERVED
```

三条轴会相交，但不能互相替代。Profile 和 Bundle 负责配置来源；Loader 和 service lifecycle 负责 activation；tool call 或直接 operation 才负责行为观察。

这也是本文的抽象模型：先把来源组合成可追溯的 Effective Config，再为重要 Capability 单独记录 activation 与 operation。不要让一个“enabled”字段同时承担这三类证明。

## 3. DSH 的 Profile schema 不是一个 schema 对象

看到 TypeScript interface 后，很容易写出一句：“DSH 用 `DshProfileManifest` 验证 Profile。”这句话只说对了一部分。

固定源码里的 contract 分散在多个 gate：

| Gate | 主要 owner | 验证或约束什么 |
|---|---|---|
| Profile manifest shape | [`profile.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/boot/app-boot/src/profile.ts) | ordered `bundles`、`patchReload: live` 或 `startup` |
| Bundle manifest | 同一文件的 bundle resolution | `dsh.bundle.patch` 声明与 patch 文件存在 |
| Patch document | [`vendor/include/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/vendor/include/src/index.ts) | top-level array、Loader entry/patch dialect、`!!js` source |
| Plugin activation | Loader + each plugin Config/dependency | row-local config 与 runtime service settlement |

`readProfileManifest()` 与 `loadProfile()` 会检查 JSON 是否为 object、profile name 是否安全、`patchReload` 是否为允许值、bundle 能否解析、manifest 是否声明 patch，以及 patch 文件是否存在。`entryListSchema` 和 `parsePatchList` 再检查 YAML/JSON 是否是 Loader 可以处理的 entry-list dialect。

但 TypeScript interface 本身不是 runtime validation，patch parser 也不会提前执行每个 plugin 的 Config schema。真正的 Provider/Consumer dependency settlement 仍在 activation 阶段。

文件缺失的语义也不是一句“optional config”可以概括：

- profile-local 与 home-level user patch 是 optional source，可以不存在；
- manifest 已声明的 bundle patch 必须存在；
- 用户显式传入的 named CLI overlay 也必须存在。

本轮先确认目标路径不存在，再执行：

```text
web --dump-config --patch <missing-overlay.yml>
```

结果是 `exit 1`、stdout empty，stderr 包含 labelled `failed to read overlay ... ENOENT`。这不是 Provider activation failure，而是在命名配置来源的读取阶段 fail loud。

所以更准确的说法是：DSH 的 Profile/Bundle/Patch schema 是一个 distributed contract。它从 manifest、文件解析一路延伸到 activation，而不是集中在一个万能 schema 对象里。

## 4. Base Bundle + Profile + Overlay 怎样形成 Effective Config

### 4.1 已证实的 precedence

CLI 先在 [`args.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/args.ts) 保留重复 `--patch` 的 argv 顺序，再由 [`profile-boot.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/profile-boot.ts) 组合 Profile。

普通 boot 的低到高 precedence 是：

```text
bundles in dsh.profile.bundles order
< $DSH_HOME/profiles/<name>/cordis.patch.yml
< $DSH_HOME/cordis.patch.yml
< --patch files in argv order
< DSH_TELEMETRY_DISABLED derived hard patch, when applicable
```

这里的 “hard patch” 是 launcher 在 boot 前推导的 telemetry disable patch。它不出现在 `--dump-config` 的静态组合结果里。

[`dump-config.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/dump-config.ts) 使用相同的 `applyEntryPatches()` 组合 bundle、profile、home 与 CLI layers，但不 boot、不求值 `!!js`。`--dump-default-config` 更窄，只保留 bundle layers，故意跳过 user/home/argv layers。

因此：

- default dump 是恢复 shipped baseline 的视图；
- effective dump 是静态 composition receipt；
- normal boot 才进入表达式求值、Loader activation 与 service settlement。

三者用途不同，不能用 default dump 证明用户环境没有 drift，也不能用 effective dump 证明插件已 active。

### 4.2 “last layer wins”只对一部分情况成立

真正的冲突规则由 [`applyEntryPatches()`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/vendor/include/src/index.ts) 定义。

对一个已存在、唯一且正确命中的 id，后层可以覆盖 top-level field。`config` 也是 top-level field，所以它被整体替换，不做 deep merge。

```text
base config = { host: expression, port: expression, trustedHosts: ... }
overlay config = { host: 127.0.0.1, port: 3081 }

effective config = overlay config
                != recursive merge(base, overlay)
```

这意味着 overlay 必须重述它真正拥有的所有 config key。遗漏字段不会因为“base 里曾经有”就自动保留。

另外三条规则更容易被忽略：

- missing id target：warning + skip，不会创建 row；
- optional `name` assertion mismatch：warning + skip；
- duplicate insert：两个 structural rows 都保留，id index 指向后来插入的 row，不做 dedup。

所以，“last layer wins”只适用于被正确命中的 unique row。它不能推广成“所有后层都必然生效”，更不能推广成“重复 id 会自动留下唯一胜者”。

仓库 owner suite [`config-dump.spec.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/boot/app-boot/tests/config-dump.spec.ts) 在本轮结果为 `6 passed / 6`，覆盖 ordered overlay、provenance grouping、与 boot patch algorithm 的相等性、absent-target warning、default warning sink 与 invalid base。

这些是 algorithm owner tests。下面两份 built CLI 输出才是 product artifact observation；二者相互支持，但不互相冒充。

## 5. 两份 Effective Config：共享 87 个 id，在 Host surface 分叉

### 5.1 Profile 先被物化成可变对象

在 isolated `DSH_HOME` 中第一次运行 built CLI 时，两个 shipped template 被物化为 mutable Profile：

| Profile | Bundles | patchReload | Manifest SHA-256 |
|---|---|---|---|
| headless | `dsh-base`, `dsh-headless` | `startup` | `104F43C27B3521E3B548FC3E5088DB93AF4F065F543860624005915F16E9D679` |
| web | `dsh-base`, `dsh-web-app` | `live` | `B07F71CBA5C341F6BD6EBF1316573588817E3A15629A86F11D34D4A01A82C94B` |

两个 profile-local `cordis.patch.yml` 都是说明注释加空数组 `[]`，SHA-256 都是 `EF189A8C27DB6D63930AA3046A3040482E952EAFCB7487C644D508E8D461F027`。

这里已经能区分三种东西：

1. shipped template：安装包给出的初始 tuple；
2. materialized mutable Profile：用户 Home 里可变化的 manifest 与 patch；
3. Effective Config：当前 layers 组合后的静态 entry list。

Profile 名称没有把三者冻结成一个东西。

### 5.2 Exact dump receipts

三次 built CLI dump 的 exact stdout receipts 是：

| Dump | Exit | Bytes | Rows | Unique ids | SHA-256 |
|---|---:|---:|---:|---:|---|
| `headless --dump-config` | 0 | 11,227 | 89 | 89 | `7B00D284956107355C44629B861C1754A570835AE04F44F9AA15E9586ECA5298` |
| `web --dump-config` | 0 | 16,558 | 144 | 144 | `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A` |
| `web --dump-default-config` | 0 | 16,558 | 144 | 144 | `0958D6C3EA1CB580AE58BCCF7294495EE5E786DB25F1EA6C39B7136039DF689A` |

Web effective 与 default 在这次实验里 byte-identical，只证明 fresh materialized Profile 的 local patch 为空、home patch 不存在。它不证明两个命令通常等价。只要 user layer 发生变化，二者按设计就应该分叉。

两份 Profile dump 有 `87` 个 shared unique ids，共同包含：

```text
sandbox
sandbox-policy
approval
tool-bash
tool-pwsh
tool-fs
fs-sandbox
```

这组交集说明 Headless 与 Web 共享 [`dsh-base`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/base/cordis.patch.yml) 的 capability stack。

真正的分叉发生在 surface bundle：

- [`dsh-headless`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/headless/cordis.patch.yml) 只增加 `headless-startup` 与 `headless-runner` 等 one-shot command-line rows；
- [`dsh-web-app`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/web-app/cordis.patch.yml) 带来 `57` 个 Headless absent ids，代表项包括 `web-startup`、`webserver`、`cordis-host-runner`、`web-runtime`、controllers、workspace 与 UI rows。

固定源码里还保留了旧的 `[base, web-app, headless]` tuple，但它是 installation-owned migration input，会被规范化为 current `[base, headless]`。它不是 current Headless 的第三个 bundle，更不能拿来证明 Headless 当前拥有 Web Host。

本轮 unmodified `headless --help` 与 `web --help` 都以 `exit 0` 结束。前者 stdout `339 bytes`，SHA-256 `DA8F354AAC509B233168982023F40FBAC43EF8E52615606F6FC82BB743913037`；后者 `759 bytes`，SHA-256 `8DBDE71AA3D2FACDA865ED27789F5E888D893003B57D3307D93DEE8E1A81D6D8`。

这两条 smoke 只证明 credential-free CLI 能到达各自 mode-owned help/exit。没有 task、model call、server listener 或 long-running Host。因此，准确结论是：两个 Profile 共享 Base capability composition，在 Host/App surface 分叉；不是“两个 runtime 除了 UI 之外完全一样”。

## 6. 最重要的反例：dump `exit 0`，activation `exit 1`

repo-owned overlay 位于：

```text
apps/cli/config/examples/cordis/cordis.yml
bytes: 781
SHA-256: 62F0D905D430F7A1A517125AAE8EE5786EABCC9B77FA362AEA16A9104A0EFD31
```

它覆盖 `webserver`，并插入 Cordis tool 相关 rows。

先走 dump path：

```text
web --dump-config --patch <cordis overlay>
```

结果是 `exit 0`、stderr empty、stdout `16,827 bytes`，共 `146 rows / 145 unique ids`，SHA-256 为 `679CC5ED39C53FDBB2D6A57014DF4486FA274A0E3250D98AF4223DED6C6D76E9`。

其中 `webserver` 的 whole config replacement 清楚可见：

```yaml
- id: webserver
  name: '@deepseek-ai/dsh-host-webserver'
  inject:
    - webStartup
  config:
    host: 127.0.0.1
    port: 3081
```

`tool-cordis` 是一个新 unique id。但 current Web Profile 本来已经有 `cordis-host-runner`，overlay 又 insert 了一次，所以 row count 是 `146`，unique id count 却只有 `145`。

dump 把 duplicate 连同 provenance 一起打印出来，没有拒绝。

接着让同一 overlay 进入 activation：

```text
web --patch <cordis overlay> --help
```

结果变成：

```text
exit: 1
stdout: empty
deepest cause:
TypeError: duplicate loader entry id: cordis-host-runner
```

这不是凭空构造的坏配置，而是 pinned repo 自带 optional overlay 与 current shipped Web composition 在这条路径上的 drift。

这组结果把三个概念拆得很清楚：

- provenance 能告诉我们 row 来自哪个 layer；
- Effective Config 能暴露 duplicate 这类结构异常；
- Loader activation 才决定这棵 entry tree 是否被接受。

因此，provenance 是诊断材料，不是 validation certificate；dump 是配置收据，不是 activation certificate。

missing overlay negative 又补上另一条边界：显式命名的配置来源连读取都失败时，命令以 labelled `ENOENT / exit 1` 结束。它不能和 optional user patch absent 混成同一种“空配置”。

## 7. FS Capability Seam：先闭合 shipped source，再看真正跑了哪条链

### 7.1 Definition、Provider 与 Consumer

本文选择 FS 作为代表 seam，因为三端都能在 pinned source 中闭合，而且有一条不需要模型的 operation test。

Definition 在 [`packages/fs/fs/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/fs/src/index.ts)：

```text
FileSystem extends Service
-> super(ctx, 'fs')
-> resolve / readText / writeText / editText contract
```

它定义 `ctx.fs` 的 service identity 与操作合同，本身不执行 host I/O。

shipped Provider row 是 Base Bundle 里的 `fs-sandbox`。对应实现 [`SandboxedFileSystem`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/fs-sandbox/src/index.ts) 继承 [`LocalFileSystem`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/fs-local/src/index.ts)，依赖 `sandboxPolicy`，读操作沿用 Local 实现，写与 edit 在 delegation 前经过 policy target check。源码把它界定为 trusted path-policy fence，不是 kernel boundary；因此即使将来取得 shipped activation 证据，也不能自动升级成 OS sandbox 证明。

Base Bundle 没有再并列挂载一个 `fs-local` row。`LocalFileSystem` 是 shipped `SandboxedFileSystem` 的实现基座，不是同时 active 的第二个 Provider。

Consumer 是 [`ToolFs`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/tool-fs/src/index.ts)。它声明：

```text
inject = ['tools', 'fs', 'systemPrompt']
```

依赖满足后才注册 read/write/edit；具体 operation 再调用 `ctx.fs.resolve/readText/writeText/editText`。

所以 shipped topology 是：

```text
FileSystem definition
  -> SandboxedFileSystem provider
  -> ToolFs consumer
```

两份 Effective Config 都包含 enabled `fs-sandbox` 与 `tool-fs` rows，这能确认 Profile **请求 Loader 组合这条 topology**。源码也能确认 class inheritance、service identity、inject 和 consumer call sites。

但 source/config 不能证明 `SandboxedFileSystem` 已构造、`ctx.fs` 已发布、ToolFs 已注册，或某次 write 真正执行。

### 7.2 Capability availability 要逐层升级

| 状态 | 最小证据 | 允许说什么 |
|---|---|---|
| Configured | effective rows 有 `fs-sandbox` + `tool-fs` | Profile 请求挂载这条 topology |
| Provider active | instance constructed，`ctx.fs` resolves | 当前 generation 发布了 FS capability |
| Consumer active | `tools/fs/systemPrompt` settled，tools registered | FS tools 对 active provider 可用 |
| Operation observed | captured call/result | 这次选定 operation 穿过 seam |

Base patch 自己就说明 row order 没有 load semantics，activation 由 service availability 驱动。因此，YAML 里谁写在前面也不能证明谁先完成 construction。

### 7.3 Runtime operation 用的是另一个 Provider

本轮真正执行的是 owner integration test [`packages/fs/tool-fs/tests/integration.spec.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/fs/tool-fs/tests/integration.spec.ts) 中的一条 Local FS seam：

```text
LocalFileSystem
  + ToolRuntime
  + FsPolicy
  + ToolFs
  -> ctx.tools.execute(write)
  -> disk readback equals exact requested bytes
```

exact targeted result 是：

```text
exit 0
1 passed / 32 skipped
```

测试创建 fresh temp directory，挂载真实 `LocalFileSystem` Provider、Tool Runtime、FsPolicy 与 ToolFs Consumer，经 `ctx.tools.execute` 执行 write，再从磁盘读取并比较 exact bytes，最后清理目录。

这项结果可以写成：

```text
FS_CAPABILITY_SEAM_TEST_RUNTIME_CONFIRMED
LocalFileSystem -> ToolFs
```

它不能改名为 `SandboxedFileSystem runtime confirmed`。shipped source seam 与 executed test seam 必须分账：

| Seam | Evidence | Status |
|---|---|---|
| `FileSystem -> SandboxedFileSystem -> ToolFs` | pinned source + shipped config rows | `SOURCE/CONFIG CONFIRMED` |
| `LocalFileSystem -> ToolFs -> exact write/readback` | targeted owner integration | `TEST RUNTIME CONFIRMED` |

一次真实 Local FS operation 也不证明 Agent turn、模型调用、OS confinement 或生产文件安全。

## 8. Permission 不是一个字段，而是三个决策平面

Effective Config 中的 permission rows 仍然保留 literal `!!js` source：

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

dump 只能证明表达式 source。它没有执行 `process.env`，更没有创建 Session 或处理某次 escalation。

这条权限路径至少包含三个平面：

1. process/config default；
2. Session standing override；
3. per-call escalation 与 approval result。

本轮运行了四个 targeted permission cases。plain default 为 `1 passed / 72 skipped`；standing override、approved escalation、rejected escalation 合计 `3 passed / 70 skipped`。观测结果是：

| Case | Observed policy/result |
|---|---|
| default | `workspace-write` + calling Session workspace root |
| standing override | `read-only` |
| approved one-call escalation | `danger-full-access` |
| rejected escalation | fail closed，Provider mutation count 不增加 |

这些 case 使用真实 `SandboxPolicyService`、Tool Runtime、FS policy、ToolFs 与 approval protocol，但外围 FS Provider 是记录 resolved policy 的 `SandboxingFakeFs`。

因此，准确标签是：

```text
PERMISSION_PROTOCOL_TEST_FIXTURE_RUNTIME_CONFIRMED
```

它确认 policy selection 与 Consumer handoff，不确认 Windows ACL、Linux Landlock、macOS Seatbelt、真实 approval UI、shipped `SandboxedFileSystem` enforcement 或 security completeness。

“Prompt 里告诉模型只读”也不能替代这条链。Prompt 是模型输入，authority control 属于 policy 和 Provider。真正的 read-only contract 应让写 Capability 不存在，或在 policy/provider 层被拒绝，并用 negative operation 证明。

## 9. 三类工程风险：配置 drift、结构 conflict 与权限错觉

### 9.1 Config drift 不只发生在 shipped bundle 之外

Profile 被物化后就是 mutable state。profile-local、home-level、CLI overlay 都可以改变 Effective Config，而 shipped bundle 和 Profile 名称保持不变。

`--dump-default-config` 故意跳过这些 user sources，所以它适合找回 bundle baseline，却不能独立审计 drift。更稳的做法是同时保存 default/effective receipt，并记录每一层 provenance。

更值得注意的是，repo-owned optional overlay 也可能相对 current shipped Profile 漂移。本文的 duplicate runner 就是实际例子。版本都来自同一 pinned repo，并不自动保证任意 example overlay 与任意 shipped Profile 可组合。

### 9.2 Whole replacement、warning skip 与 duplicate 是三种不同 conflict

whole replacement 会丢掉 overlay 没有重述的 config key；warning + skip 会造成“配置文件改了，但 effective row 没改”的错觉；duplicate insert 则可能让 dump 成功、activation 失败。

因此，配置审计至少要同时记录：

- layer source 与 order；
- target id/name 是否命中；
- before/after whole config；
- row count 与 unique id count；
- warning；
- activation result。

只保存最终 YAML、不保存 provenance 与 activation，恰好会丢掉最需要排障的信息。

### 9.3 Permission drift 不能从静态配置推断

静态 `!!js`、process env、Session override、per-call escalation 与 Provider enforcement 都可能改变最终结果。配置 receipt 应记录 source expression，但运行 receipt 还要记录 resolved policy、workspace root、Provider identity 与 operation outcome。

同时，receipt 不应该泄露 credential value。本文只统计了 secret-like environment **name count**：共 `5` 个，其中 DSH/DEEPSEEK-prefixed 为 `0`；没有读取值，也没有因此推出任何 credential 是否可用。

## 10. BuildPilot：采纳 Capability 合同，暂缓组合机器

DSH 的价值不在于给 BuildPilot 一份 YAML 模板，而在于暴露了两个必须被产品化的问题：最终组合是什么，以及能力究竟走到了哪一层。

本篇给 BuildPilot 的 transfer decision 是：

| Mechanism | Decision | 第一版合同 |
|---|---|---|
| Explicit Capability Set | `ADOPT` | definition、provider、consumer、permission、source/version、activation receipt |
| Read-only Profile | `ADOPT` | 只开放观察/诊断能力；写能力 absent/denied by default |
| Effective/default diff + activation receipt | `ADOPT` | composition diff 与 activation result 分列，duplicate/missing retained |
| Arbitrary layering / `!!js` | `DEFER` | 第一版不开放任意用户 composition |
| Live reload / runtime replacement | `DEFER` | 先使用 immutable run profile + explicit composition root |
| Multi-Host composition | `DEFER` | CLI-first，等第二个 Host 的真实需求 |

Explicit Capability Set 不是把 DSH 的 string-key registry 或 package layout 原样搬过去。它更接近一份可审计声明：这个运行配置允许哪些 Capability，由哪个 Provider 提供，哪些 Consumer 使用，权限边界是什么，当前状态是 configured、active 还是 operation-confirmed。

Read-only Profile 也不能只是一条名字约定。第一版应让写 Capability 不出现在 set 中，或由 policy/provider 默认拒绝；未来还要补 negative write tests 和 provider receipt。

为什么复杂 layering、live reload、runtime replacement 与 multi-Host 先 `DEFER`？因为本文已经观察到 whole replacement、warning skip、duplicate、local drift 与 dump/activation divergence 的真实成本，而 BuildPilot 当前没有已证需求必须承担这些维度。

`DEFER` 不是说这些机制永远错误。它表示等到第二 Host、热更新或 provider rebinding 成为真实 requirement 时，再连同 lifecycle 和 conflict tests 一起评估。

这些都是 Part VI 的 design proposal。Article 38 没有启动，Part VII 没有开始；当前没有 BuildPilot ADR、代码、runtime 或 migration 可供声称完成。

## 11. 动手验证：怎样审计一份 Profile，而不把 dump 当完成证明

这套方法可以迁移到其他 Harness，不要求使用 DSH，也不需要真实模型 Provider。

### 11.1 冻结研究对象

1. 记录 official repository、tag、full SHA、OS、Node/package-manager 版本。
2. 在运行前保存 `HEAD`、tag target、working tree、index 与 diff。
3. 使用 isolated Home 和 cwd，避免真实用户配置污染结果。
4. 不读取 credential value，不发真实 Provider request。

本轮环境是 Windows `10.0.19045` x64、Node `v24.18.1`、project pnpm `11.7.0`、Git `2.53.0.windows.2`、PowerShell `7.6.4`。

### 11.2 分别保存 default 与 effective receipt

对每个 Profile 记录：

- materialized manifest 与 bundle order；
- `patchReload` mode；
- local patch identity；
- command、exit code、stdout bytes、row count、unique id count、SHA；
- provenance sections；
- shared/profile-only row sets。

然后比较 default/effective。相同只能解释为这次 user layers 没有引入 byte diff，不能上升成命令等价。

### 11.3 给 dump 配一个 activation probe

选择一个 pinned overlay，先保存 dump，再用最小 activation path 观察 Loader 是否接受。对 target miss、name mismatch、duplicate、missing file 分别记录 warning/error 和 exit code。

不要因为 dump 输出了目标值就删除 duplicate 或 activation failure。反证是审计结果的一部分。

### 11.4 沿一个 Capability Seam 逐层取证

1. 找 Definition 的 service identity 与 contract。
2. 找 shipped Provider row、implementation 与 dependency。
3. 找 Consumer inject 和实际 call site。
4. 分别记录 configured、provider active、consumer active、operation observed。
5. 如果 operation test 换了 Provider，必须在结论里写出真实 Provider 名称。
6. 如果 permission test 使用 fake，必须在 evidence label 里写 fixture boundary。

解释结果时，可以使用这张最小表：

| Observation | 可以说 | 不能说 |
|---|---|---|
| dump row present | configured/effective row | Provider active |
| default/effective same | selected empty user layers caused no diff | commands generally equivalent |
| overlay dump `exit 0` | static composition rendered | Loader accepts it |
| activation duplicate `exit 1` | selected overlay drift reproduced | all overlays invalid |
| Local FS exact write/readback | Local Provider seam executed | shipped SandboxedFS active |
| fake permission cases pass | protocol handoff fixture confirmed | OS sandbox confirmed |

### 11.5 失败命令也必须进入复现记录

本轮有三组 harness mistakes，它们没有被改写成 DSH product failure：

1. 第一次 `corepack pnpm --version` 从错误的课程仓库 cwd 运行，Corepack 尝试访问 npm registry，并因 `EACCES` 以 `exit 1` 结束；改为 pinned fixture cwd 后，project pnpm `11.7.0` 以 `exit 0` 返回。
2. PowerShell 变量 `$home` 与 automatic `HOME` 冲突，目标 temp assignment 失败，后续 CLI 因 filesystem access 以 `exit 1` 结束；没有改动 DSH fixture。
3. `Start-Process -ArgumentList` capture helper 把参数 flatten 错误，四次命令都缺少 `--profile`，以 `error: --profile <name> is required / exit 1` 结束；改用 direct PowerShell argument arrays 后，才得到本文计数的有效结果。

记录这些失败的目的，不是增加日志长度，而是让读者知道为什么命令形状发生变化，并把 orchestration error 与 product behavior 分开。

最后再检查一次 fixture identity 与 clean state。本轮最终结果仍是 pinned commit，`git status --short` 为 `0` rows，diff 与 cached diff 为空。

## 12. Evidence Boundary：本篇建立了什么，还没有建立什么

### 12.1 已建立

- source 与 Lab evidence 绑定 official frozen revision，fixture 在检查时 clean；
- Profile、Bundle 与 patch validation 是 distributed contract；
- normal boot precedence 是 bundle -> profile -> home -> CLI -> telemetry hard patch，dump/default 各有明确子集；
- matched unique id 是 top-level replacement，`config` 不 deep merge；miss/mismatch warning + skip，duplicate insert 不 dedup；
- built CLI 生成 Headless `89/89`、Web `144/144` 的 Effective Config receipts，二者共享 `87` ids；
- current Headless 是 one-shot runner surface，Web 拥有 Host/API/UI composition；
- repo-owned overlay dump `exit 0 / 146 rows / 145 ids`，activation 因 duplicate id `exit 1`；
- missing named overlay `ENOENT / exit 1`；
- shipped FS source topology 是 `FileSystem -> SandboxedFileSystem -> ToolFs`；
- targeted runtime operation 是 `LocalFileSystem -> ToolFs` exact write/readback；
- permission targeted fixture 闭合 default、standing override、approved/rejected escalation handoff；
- BuildPilot 的 Capability Set、read-only Profile 与 `DEFER` 项已形成 proposal。

### 12.2 未建立

- shipped `SandboxedFileSystem` 在 Profile probe 中 active；
- `ctx.fs` 与 ToolFs 在真实 Agent turn 中 settled；
- Web server listener 或 long-running Host 成功；
- real LLM Provider、model output、credential、network、latency、token 或 cost；
- dump 中 `!!js` 的 runtime value，或某个真实 Session 的最终 permission；
- Windows ACL、Landlock、Seatbelt 或其他 OS sandbox enforcement；
- production security、production readiness 或跨平台普遍性；
- 所有 DSH Profile、overlay、plugin 与 Capability path；
- BuildPilot ADR、实现、runtime、Article 32 结论或 Part VII 已开始。

## 13. Claim 与 Evidence Card 对照

为了让结论可以回查，下面把 `15 / 15` Claims 映射回本文段落。`PROPOSAL` 是课程工程判断，不冒充 DSH fact。

| Claim | Status | 本文落点 | Evidence Card | 表述上限 |
|---|---|---|---|---|
| `31-C01` frozen revision / clean fixture | `CONFIRMED` | 开篇、§11—12 | `31-E01` | identity 不证明机制 |
| `31-C02` distributed schema / named file fail loud | `CONFIRMED` | §3 | `31-E02` | interface 不等于全部 runtime schema |
| `31-C03` ordered layers / dump subset | `CONFIRMED` | §§2、4 | `31-E03` | precedence 不等于 patch 命中 |
| `31-C04` replacement / skip / duplicate semantics | `CONFIRMED` | §§4、6、9 | `31-E04` | 不写成无条件 last-wins |
| `31-C05` two dumps / boot-free / unevaluated `!!js` | `CONFIRMED` | §§1、4—5 | `31-E05` | dump != activation |
| `31-C06` overlay dump success / activation rejection | `CONFIRMED` | §§1、6、9 | `31-E06` | 只限所测 overlay/path |
| `31-C07` 87 shared ids / surface split | `CONFIRMED` | §5 | `31-E07` | row diff != full runtime diff |
| `31-C08` Headless no Web Host rows | `CONFIRMED` | §5 | `31-E08` | help path != long-running Host |
| `31-C09` shipped FS source topology | `CONFIRMED` | §7 | `31-E09` | source/config，不声称 active |
| `31-C10` LocalFileSystem -> ToolFs operation | `CONFIRMED` | §7 | `31-E10` | 不冒充 SandboxedFS |
| `31-C11` permission protocol with fake Provider | `CONFIRMED` | §§8—9 | `31-E11` | fixture only，不证明 OS enforcement |
| `31-C12` default recovery / local drift / missing overlay | `CONFIRMED` | §§3—6、9 | `31-E12` | empty-layer equality 不可推广 |
| `31-C13` explicit Capability Set | `PROPOSAL` | §10 | `31-E13` | 无 Part VII implementation |
| `31-C14` read-only Profile first | `PROPOSAL` | §§8、10 | `31-E14` | future negative tests required |
| `31-C15` defer layering/reload/replacement/multi-Host | `PROPOSAL` | §10 | `31-E15` | conditional defer，不是永久否定 |

最终状态：

```text
Claims: 15 / 15
Evidence Cards: 15 / 15
Claim status: 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED
Headless effective config: 89 rows / 89 ids
Web effective config: 144 rows / 144 ids
Shared ids: 87
Repo overlay: dump exit 0 / 146 rows / 145 ids
Repo overlay activation: exit 1 / duplicate cordis-host-runner
Config owner tests: 6 / 6
FS operation: LocalFileSystem -> ToolFs / 1 passed
Permission protocol: 4 targeted cases / fake FS Provider
Real LLM Provider / model / token / cost: NOT TESTED
OS sandbox enforcement: NOT TESTED
BuildPilot: ADOPT CAPABILITY SET + READ-ONLY PROFILE / DEFER COMPOSITION MACHINERY
Part VII: NOT STARTED
```

## 14. 后续文章只接 owner，不在这里抢答

Article 29 已经建立 Host/Profile/Loader 到 Agent Run 的总图；Article 30 深入 representative plugin lifecycle。本篇只承担 Profile composition、Effective Config 与一个 FS Capability Seam。

下一篇将进入 System Prompt Assembly 与 PromptContext，研究多来源 Context 怎样组成 model request。它尚未发布，因此当前不创建 future `relref`，也不提前给出 assembly precedence 或两次 request diff 的结论。

Article 33—37 分别拥有 Loop/Step、Session continuation、完整 Tool pipeline、Recovery/observability 与 extension mapping。Article 38—44、BuildPilot implementation 和 Part VII 都保持 `NOT STARTED`。

## 15. 学习检查

1. 为什么 Profile 名称不能证明最终 Effective Config？
2. shipped Profile template 与 materialized mutable Profile 有什么区别？
3. Profile、Bundle、Patch/Overlay validation 为什么是 distributed contract？
4. 普通 boot 的 layer precedence 是什么？`--dump-config` 为什么只呈现其中一个子集？
5. `config` whole replacement 与 deep merge 的工程后果有什么不同？
6. missing id、name mismatch 与 duplicate insert 分别怎样处理？
7. `--dump-default-config` 与 `--dump-config` 各适合回答什么问题？
8. Headless 与 Web 为什么可以共享 `87` 个 ids，却拥有不同 Host surface？
9. Web effective/default byte-identical 为什么不能推广成一般规律？
10. repo-owned overlay 的 `146 rows / 145 unique ids` 暴露了什么 drift？
11. 为什么 dump `exit 0` 不能替代 activation result？
12. Service Definition、Provider、Consumer 与 Operation 分别需要什么证据？
13. shipped `SandboxedFileSystem` source seam 与实验 `LocalFileSystem -> ToolFs` 为什么必须分账？
14. permission targeted tests 通过后，为什么仍不能声称 OS sandbox 已验证？
15. `!!js` source、Session override 与 per-call escalation 属于哪些不同平面？
16. BuildPilot 为什么应先采用 explicit Capability Set 与 read-only Profile？
17. arbitrary layering、live reload、runtime replacement 与 multi-Host 为什么先 `DEFER`？

## 16. 最短结论

Profile、Bundle 与 overlay 能把通用 Harness 组合成不同 capability set，但组合力越强，越不能用一个“配置已加载”掩盖多个独立状态。

Headless `89` rows、Web `144` rows 和 `87` 个 shared ids，说明二者共享 Base、在 surface 分叉；repo overlay 的 dump `exit 0` 与 activation `exit 1`，说明 Effective Config 仍不是 active runtime；`LocalFileSystem -> ToolFs` 的 exact operation 则提醒我们，运行证据必须写出真正参与的 Provider，不能借用 shipped topology 的名字升级结论。

最后压成一句：

> 先把配置来源组成可追溯的 Effective Config，再用 activation 和 operation 收据证明 Capability；Profile 名字、Provider row 与成功 dump 都不能替代这条证据链。

> **上一篇**：[Everything is a Plugin：插件内核如何承载 Capability 与生命周期]({{< relref "ai-empowerment/agent-engineering-30-dsh-plugin-core.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

> **下一篇**：System Prompt Assembly 与 PromptContext：多来源 Context 怎样组成（计划中，发布后再补链接）。
