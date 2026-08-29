---
title: "怎样把 DeepSeek Harness 当作 Evidence-first 源码教材"
slug: "agent-engineering-28-dsh-evidence-first-source-method"
date: "2026-08-30T00:00:00+08:00"
description: "冻结 DeepSeek Harness 版本、证据分类、验证阶梯与失败边界，为后续源码调用链和运行实验建立可审计的研究方法。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Evidence Engineering"
  - "Source Code Reading"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 290
weight: 3290
---

> **上一篇**：[Harness 的设计取舍：可替换性、复杂度、Bloat 与演化]({{< relref "ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md" >}})

> **下一篇**：[DeepSeek Harness 总图：从 Host 启动到一次 Agent Run]({{< relref "ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

# 怎样把 DeepSeek Harness 当作 Evidence-first 源码教材

读一个开源 Agent 项目，最容易产生的错觉是：只要目录看完了、核心类找到了、测试也搜到了，就已经“理解了它的架构”。

但真实工程里，目录、类名和测试名往往只够提出问题，还不够回答问题。

README 里写着一条启动命令，不等于这条命令在你的环境里已经成功执行。源码里有一个 `AgentLoop`，不等于真实请求一定走过这个循环。配置输出里出现一个插件条目，也不等于插件已经激活。某个单元测试通过，更不等于 Web、Headless、Provider、Credential 和进程边界组成的真实应用路径已经跑通。

如果忽略这些差别，源码阅读很快会变成一种“看起来很完整”的叙事：从文件夹名猜模块边界，从同名类型猜生命周期，从测试名猜运行行为，再把这一切画成一张没有证据等级的架构图。图可能很漂亮，结论却无法复查。

所以 Part VI 不从“DeepSeek Harness 有哪些模块”开始，而先建立一套 Evidence-first 源码研究方法：冻结研究对象，区分证据来源与确认层级，沿 symbol、call path、test、minimal run 和 Trace 逐级升级结论，同时把失败、缺口和反证保留下来。

如果这篇只记一句话，可以先记这一句：

> 源码教材的价值，不在于读到了多少类名，而在于每个结论能否沿固定版本、调用路径、实验与反证被重新核对。

先把本文的版本和证据上限说清楚。本文研究的固定对象是 DeepSeek Harness 官方仓库的 `dsh-v0.1.2-alpha.1`，完整 commit 为 `cd5ef8148158c3a752a658978873241fdf8e2bbc`。后文所有 DSH 源码、命令路径和运行观察都只适用于这个快照与记录的 Windows 环境。固定版官方文档把项目定位为 Developer Preview；安全说明明确表示它尚未经过安全审计，不应被当作 secure 或 production-ready 系统。

本文覆盖 `16 / 16` Claims 与 `12 / 12` Evidence Cards：`6 CONFIRMED / 0 PARTIAL / 10 PROPOSAL / 0 BLOCKED`。这里已经有真实 baseline probes，但完成的 Agent Run 数量仍是 `0`。这两个事实必须同时保留：我们确实运行了一些东西，也确实没有运行到一次模型支持的 Agent Turn。

这次 baseline 的持久化记录是 direct structured observations：它保留了命令、环境、exit code、终局摘要、失败分类和脱敏关键行，但没有保留完整 stdout/stderr stream。因此，后文的 baseline Trace 都只按这个 structured/sanitized durable record 的上限解读，不把它称为完整 raw log。

## 1. 源码能读，不等于行为已经被证明

源码阅读当然重要。问题不在“要不要读源码”，而在“源码能把结论推到哪一层”。

同一个项目里，下面这些材料都可以成为证据，但它们的证明力不同：

| 看起来像证据的东西 | 它实际能提供什么 | 仍然缺什么 |
|---|---|---|
| README 启动步骤 | 官方支持入口和前置条件 | 当前版本的实现 owner 与真实命令结果 |
| 目录或 package | 候选责任面 | registration、caller、lifecycle、activation |
| symbol | 静态锚点 | caller/callee、分支与真实 traversal |
| test name | 候选可执行 fixture | 断言、原始结果与应用运行边界 |
| generated output | 某次生产过程的 artifact | 生产命令、revision 与 freshness |

例如，固定版根 `package.json` 里有 `dsh` script，`apps/cli/src/bin.ts` 也确实解析命令行参数。它们能支持“源码入口存在”这个结论。但如果没有继续追到 `parseDshArgs`、`runProfile`、`boot`，就不能说 profile 已经完成装载；即使静态路径追到了 Loader settlement，没有一次对应 Trace，也不能说 Agent 已经创建、Turn 已经开始。

再比如，固定版 `packages/bundle/headless/cordis.patch.yml` 里有 headless 相关配置行。它们能说明 profile composition 的候选入口，却不能单独证明这些行已被 Loader 激活。配置存在、配置被解析、配置被装载、能力实际可用，是四个不同的问题。

这也是为什么本文不是 DSH API 教程。我们关心的不是把文件名列全，而是让每个结论都带着四个字段离开：它来自哪类 Evidence，确认到了哪一层，它能证明什么，以及它不能证明什么。

## 2. 先冻结研究对象：Baseline Manifest 是证据链的根

多人、跨篇、长周期研究最危险的漂移，不一定发生在文字里，也可能发生在 Git 对象上。

第一篇读的是 tag，第二篇顺手查了 latest main，第三篇又从已经安装的 npm package 补了一个缺失实现。每一段局部内容可能都是真的，拼在一起却不再对应任何一个真实版本。这样的调用链无法复现，也无法知道某个差异究竟来自理解变化，还是代码已经变化。

因此，Part VI 先冻结统一 baseline：

| Field | Frozen value |
|---|---|
| Official repository | `https://github.com/deepseek-ai/deepseek-harness` |
| Tag | `dsh-v0.1.2-alpha.1` |
| Full commit | `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| Selected at | `2026-08-29` |
| Verified at | `2026-08-30 / Asia/Shanghai` |
| Fixture policy | 外部、可丢弃、不 vendor、不进入课程仓库提交 |

验证时分别核对了 official origin、fixture `HEAD`、本地 tag target、远端 tag target 和 working tree cleanliness。它们都指向同一个完整 SHA，探针执行前后 tracked worktree 也保持 clean。

这只能证明“研究的是哪个 Git 对象”。它不证明依赖已经装好，不证明构建成功，不证明测试通过，更不证明应用跑到了 Agent Turn。即使 tracked status 为空，也不能推断 ignored 的 `lib/`、cache、session 或临时目录不存在。

环境同样属于 baseline，而不是可省略的脚注：

| Component | Direct observation |
|---|---|
| OS | Microsoft Windows NT `10.0.19045` |
| Architecture | `X64` |
| Node.js | `v24.18.1` |
| Project-pinned pnpm | `11.7.0` |
| Global pnpm | `11.19.0`，不用于定义项目结果 |
| npm | `11.16.0` |
| Git | `2.53.0.windows.2` |
| PowerShell | `7.6.4` |

这里有一个很小但很典型的证据边界：从 TechStackShow 目录运行 `corepack pnpm --version`，看不到 DSH 根 `packageManager` 声明；只有在 DSH fixture 内运行，才得到项目固定的 pnpm `11.7.0`。全局装着什么版本，和项目实际选择什么版本，是两条不同证据。

后续 Article 29—37 每篇都要重新核对 repository、full commit、tag 与 cleanliness。只要 SHA 改变，就必须显式执行 version migration；不能把新版本的 symbol 静默接到旧版本的 call path 上。

## 3. 六类 Evidence，与确认层级分开记账

Evidence-first 的中心不是“给来源贴标签”，而是把几件经常混在一起的事拆开。

第一条轴是 Evidence Class，也就是材料从哪里来、以什么方式产生：

| 正文名称 | Evidence Card 值 | 可以支持 | 不能单独支持 |
|---|---|---|---|
| Official Fact | `OFFICIAL_DOC` | 官方声明、支持入口、前置条件、安全警告 | 固定 commit 的实际实现或真实运行 |
| Source Fact | `PINNED_SOURCE` | 固定 revision 的 file、symbol、静态分支和调用关系 | 真实输入走过该路径 |
| Runtime Observation | `RUNTIME_OBSERVATION` | 指定环境与输入下出现的输出、事件、错误 | 未观测分支、内部因果、跨版本稳定性 |
| Experiment | `EXPERIMENT` | 冻结 fixture、变量、步骤后的比较与结果 | 未覆盖平台或生产泛化 |
| Inference | `INFERENCE` | 多项证据之间的显式解释链 | 源码事实或运行事实 |
| Proposal | `DESIGN_PROPOSAL` | 课程采用、简化、拒绝、延后判断 | DSH 已实现或 BuildPilot 已运行 |

第二条轴是确认层级：

- `DOC_CONFIRMED`：固定版官方文档明确说了什么。
- `SOURCE_CONFIRMED`：固定 commit 中 owning file、symbol 与静态路径闭合到了哪里。
- `RUNTIME_CONFIRMED`：支持的 run entry 在记录环境、输入、exit code 与 raw Trace 后，实际走过了什么。

第三个字段才是 Claim Status：`CONFIRMED / PARTIAL / PROPOSAL / BLOCKED`。

这三组字段不能互相代替。一个 Evidence Card 可以属于 `PINNED_SOURCE`，却因为 caller 或 downstream path 没闭合而仍是 `PARTIAL`。一个失败的 `EXPERIMENT` 也可以确认一个更窄的 Claim，例如“当前 credential-free 路径 fail closed”，但不能因此确认 Agent Run。

这个拆分还能解释一个常见困惑：为什么“源码里确实有”仍然不能写成“系统就是这样运行的”？因为 source 能确认 owner、branch 和静态 reachability，runtime 才能确认指定输入真正走过哪条路径。前者不是弱化版的后者，而是不同维度的证据。

需要特别说明的是，这套六类 Evidence 和确认规则是 Course Factory 的研究合同，不是 DSH 内建 taxonomy，也不是 DSH API。它的价值要等 Article 29—37 实际使用后，再由 Part VI Audit 回看。

## 4. 从 symbol 到 Trace：结论怎样逐级获得资格

如果只记一条操作路径，可以使用下面这条 evidence-upgrade ladder：

```text
repository + full revision
-> owning file + symbol
-> caller / callee + closed call path
-> relevant test and its fixture boundary
-> supported minimal run entry
-> frozen input + environment + safety conditions
-> raw Trace / stdout / stderr / exit code
-> interpretation + counter-evidence
-> bounded Claim Status
```

它不是“资料越多越好”的收集清单，而是一条结论升级链。

### 4.1 Identity：先证明研究的是同一个对象

repository、full revision、tag、retrieved time 和 cleanliness 组成证据主键。缺了这一层，后续所有 file、symbol 和 test 都可能来自不同版本。

Identity 不证明代码能执行。它只保证讨论对象没有被悄悄换掉。

### 4.2 Symbol：找到名字，也要找到 owner

symbol 需要绑定 owning file，而不是只记搜索结果。随后还要回答：谁注册它、谁调用它、谁释放它、失败从哪里返回？

找到 `ReactLoopAgent`，只能证明这个类型在固定版源码中存在。没有 `turn`、`step`、tool calls、Session event 与 stop condition 的调用关系，不能写出完整 loop；没有运行 Trace，也不能说某个 scenario 走过了它。

### 4.3 Call path：静态可达，不等于运行经过

闭合 caller/callee 可以确认责任归属和静态顺序，但真实运行仍可能因为 profile、feature flag、credential、platform 或 input 走另一条分支。

所以 call path 的结论通常写成“固定版源码沿 A 到 B，再到 C”，而不是“每次运行都会 A、B、C”。后一句需要更强的动态证据。

### 4.4 Test：先写清 fixture 边界

测试可以把静态关系提升为 fixture-scoped executable evidence，但必须说明测试使用了什么输入、mock/fake 了什么、断言了什么、没有覆盖什么。

mock Provider 走通，只能证明这个 fixture 内的 loop 或 event behavior；它不能证明真实 Provider、网络、额度、token 成本或服务端行为。

### 4.5 Minimal run 与 Trace：记录实际发生的事

最小运行要使用 supported entry，冻结环境、输入、权限和安全条件，再保存 exit code、stdout/stderr、事件序列与重现说明。Trace 能证明目标 scenario 实际出现了哪些事件，但仍然不能自动证明未观测分支、内部因果或跨版本合同。

上一句是理想方法的教学标准。本次 Article 28 baseline 实际只有 commands、environment、exit codes、terminal summaries、failure classification 与 sanitized excerpts 组成的 durable record，未保留完整 stdout/stderr stream；这个限制不改变已记录的窄结果，但会限制后续审计能重放到多细。

最后一步才是 Interpretation。Interpretation 必须把 Source Fact 与 Runtime Observation 接起来，同时保留替代解释和 `Does Not Prove`。如果中间任何一层缺失，正确动作不是补一张想象中的架构图，而是收窄 Claim。

## 5. 先切 source boundary，再讨论实现

源码研究的另一个常见误区，是把 source、artifact 和 runtime residue 混进同一层。

在固定 commit 下，Part VI 使用下面的边界：

| Plane | 典型内容 | 使用规则 |
|---|---|---|
| Primary pinned source | `AGENTS.md`、固定版 docs、`apps/cli/src/`、`packages/*/*/src/`、owned config、scripts、tests | 建立 official/source Claim，但仍要确认 owner 与 path |
| Vendored source | `vendor/` 与 manifest | 调用路径跨入时记录 upstream revision 与 local modifications |
| Generated/artifact | `lib/`、`apps/web/dist/`、build record、catalog、snapshot | 记录 producing command、revision、freshness 后才能作 artifact evidence |
| Excluded local state | `.env`、credentials、sessions、storages、cache、coverage、temp homes | 不进入 pinned-source Claim；secret 值绝不进入课程仓库 |
| Comparison only | latest main、当前 docs 网站、另一 checkout 或 npm package | 不能静默填补 pinned-version 缺口 |

测试源码属于 pinned source，测试结果则属于 Experiment 或 fixture-scoped runtime evidence。`lib/` 中的 built CLI 可以证明这次 build 生成了可执行 artifact，但不能替代 `apps/cli/src/bin.ts` 对入口责任的说明。

固定版 README 确实链接了官方文档网站。本文把“链接存在”记录为固定版 Official Fact，却没有把网站当前内容当成 pinned implementation evidence，因为网站会继续更新，未必和 tag 一一对应。

同样，DSH fixture 始终放在课程仓库外部。课程仓库保存路径、命令、环境、exit code、summary、failure classification、sanitized excerpts 和解释，不把 DSH source、build artifacts、credential 或 session data 一起提交进来。本轮没有另存完整 stdout/stderr stream。

## 6. 失败也是 Evidence：Baseline Probes 怎样限制措辞

抽象规则只有落到失败上，才知道是否真的有效。

这次 baseline 不是一条从 install 到 Agent Run 全绿的演示。恰恰相反，它同时包含成功、权限 caveat、完整测试失败、孤立反例和预期的 credential failure。正因为这些结果不整齐，才适合说明每一层 Evidence 的上限。

### 6.1 Install：成功依赖已填充的离线 store

在固定 fixture 中执行：

```text
corepack pnpm install --frozen-lockfile --offline
```

命令以 project-pinned pnpm `11.7.0` 完成，覆盖 265 个 workspace projects，exit code 为 `0`。这支持的窄结论是：当前固定依赖状态可以从这台机器已填充的 store 完成 frozen install。

它不证明 clean store 可以离线安装，也不证明 registry/network 可用或跨机器可复现。Linux-only landlock packages 在 `win32/x64` 上出现 unsupported warning，同样属于环境记录，而不是可以删掉的噪声。

事务早期的 Master precheck 曾观察到两次网络 timeout，第三次使用 bounded network settings 后才成功。那不是本轮 Lab Engineer 的 raw output，只保留为历史上下文，不能替换上面的 direct offline observation。

### 6.2 Build：同一命令，sandbox failure 与 host success 都要保留

构建入口在固定版 root `package.json:scripts.build`，调用 `scripts/build.ts:main` 和 `runScript`。静态顺序是 Host、Client、Web，最后写 client build record。

第一次在普通 sandbox 中运行 `corepack pnpm run build`，Host 与 Client 已经推进，但 Vite/esbuild 读取上层目录时得到 `Access is denied`，无法解析 `apps/web/vite.config.ts`，最终 exit code 为 `1`。

随后在不改变源码和命令的情况下，给予构建所需的有限 host filesystem access，命令 exit code 为 `0`：Host、Client、Web 完成，Vite 报告 345 个 transformed modules，build record 记录 218 个 client artifacts。

因此，这里的 verdict 不是一个无条件 `PASS`，而是：

```text
PASS_WITH_HOST_ACCESS_CAVEAT
```

第一次失败证明的是 sandbox access boundary，不是 DSH source build defect；第二次成功证明的是当前 fixture 在这台 host、这组权限条件下可构建，不是 sandbox-only、clean-machine 或 cross-platform build success。

### 6.3 Test：完整 suite FAIL，isolated PASS 不能覆盖它

固定版 root unit-test entry 是 `package.json:scripts.test -> vitest run -> vitest.config.ts`。它与 `test:e2e`、`test:snapshot` 是不同 dispatcher，不能因为一个入口运行了，就把另外两个也算进结果。

本轮 Lab Engineer 的完整 unit test 结果是：

```text
Test Files  32 failed | 965 passed | 4 skipped (1001)
Tests       129 failed | 15939 passed | 66 skipped (16134)
Duration    305.24s
exit        1
```

因此，完整 suite 的状态只能写成 `FAIL`。

失败中确实出现了大量 Windows symlink `EPERM`，也出现了 ACL/sandbox 问题、`CreateRestrictedToken failed (Win32 87)`、process lifecycle timeout、teardown failure、`EBUSY` cleanup、network restriction，以及一些看起来独立的 assertion failure。现有证据不能把 129 个失败全部归为同一个原因。

`scripts/gen-third-party-notices.spec.ts` 在默认 5 秒限制下失败，改用 repository-local Vitest entry 并把 test timeout 提到 30 秒后，`27 / 27` 通过，exit code 为 `0`。这个实验是反证：它说明该项失败具有 timing-sensitive 特征，不像稳定的内容 mismatch。

但 isolated `27 / 27` 只分类了一个失败。它不能清除另外 128 个失败，更不能把 full suite 改写为 PASS。

事务早期 Master precheck 的另一轮完整测试曾得到 `24` 个 failed files、`44` 个 failed tests。它只说明不同运行条件下统计会变化；本文的 direct Lab 结果仍然是 `32 / 129`，两组数据不能互相替代。

### 6.4 CLI help：有入口，不等于启动了服务

运行 built artifact：

```text
node apps/cli/lib/bin.js --help
```

CLI 打印 usage、profile、patch、config dump、web 与 plugin 等入口，exit code 为 `0`。

它能证明 built CLI surface 可用。它没有启动 profile、服务或任务，也没有证明任何 plugin、Agent 或 Provider 可用。

### 6.5 Config dump：effective resolution，不是 activation

使用一个全新的 isolated `DSH_HOME` 执行 built CLI 的 headless config dump，命令 exit code 为 `0`，输出包含 base composition 和 headless patch，出现 `agent-loop`、`llm-deepseek`、`headless-startup`、`headless-runner` 等行。

这里允许写的是：effective config resolution 已经发生。

不能写的是：这些行都已被 Loader 激活，Agent 已经创建，Provider 已经连接，Turn 已经开始。配置输出是 composition artifact，不是 activation Trace。

### 6.6 Keyless run：`MISSING_CREDENTIAL` 不是 Agent Run

最后一个探针使用另一个 isolated `DSH_HOME`，关闭 telemetry，把 permission 设为 read-only，并从 child process environment 中移除名称匹配 `KEY|SECRET|TOKEN|PASSWORD` 的继承变量。输入是一个受限、无工具调用的 inert task，timeout 为 30 秒。

child 没有 timeout，stdout/model result 为空，exit code 为 `1`。sanitized stderr 报告 `MISSING_CREDENTIAL`，指出 `deepseek-official` route 缺少 `DEEPSEEK_API_KEY`。记录里只保留 credential 名称，没有 credential 值。

这个结果确认的是：当前 bounded path 到达 credential resolution，并在缺少 provider credential 时 fail closed。

它不确认 Agent Turn，不确认 model/provider request，不确认 network response，不确认 token usage，也没有成本观察。把这个 failure 写成“DSH 已经跑起来，只差 key”，仍然过度了；现有 Trace 只允许停在 credential boundary。

整个 baseline 可以压缩成下面这张表：

| Layer | Verdict | Narrow supported claim |
|---|---|---|
| Identity | `PASS` | official origin、HEAD、tag 与 clean tracked state 在验证时一致 |
| Install | `PASS_FROM_POPULATED_OFFLINE_STORE` | 当前 host 已填充 store 下 frozen install 成功 |
| Build | `PASS_WITH_HOST_ACCESS_CAVEAT` | 取得所需 host filesystem access 后完整 build 成功 |
| Full unit test | `FAIL` | 入口可执行、结果可测量，但本轮 129 tests failed |
| Built CLI/config | `PASS` | help 与 effective config resolution 成功 |
| Completed Agent Run | `NOT_CONFIRMED` | keyless path 停在 credential resolution |
| Production-ready | `NO CLAIM` | 固定版官方安全文档明确反对该推断 |

## 7. 先闭合启动基线，但停在 Article 29 的边界

Source Map 没有只给目录，而是把 baseline command 追到 owning source。

Install 的 source contract 来自 root `package.json`：`packageManager = pnpm@11.7.0`、Node engine、workspaces 与 `postinstall`，后者进入 `scripts/install-lefthook.mjs`。这说明 install 的 repository-owned lifecycle tail，但不证明 registry、dependency 或 hook 实际成功。

Build 的静态路径是：

```text
package.json scripts.build
-> scripts/build.ts main / runScript
-> build:lib (Host, then Client)
-> build:web
-> client build record
```

Unit test 的静态路径是：

```text
package.json scripts.test
-> vitest run
-> vitest.config.ts
-> configured unit projects
```

支持的 source CLI/profile boot 基线则是：

```text
package.json scripts.dsh
-> apps/cli/src/bin.ts
-> parseDshArgs
-> runProfile
-> composeProfile / prepareProfile
-> packages/boot/app-boot/src/index.ts:boot
-> Loader mount / await / activation audit
-> settled root context
```

固定版 `packages/bundle/headless/cordis.patch.yml` 又提供 `headless-startup` 与 `headless-runner` 两个 downstream seed。但 Article 28 在这里主动停止。

从 bundle row 到 runner、`ctx.agents`/factory、Agent、Turn 的完整静态路径属于 Article 29；对应的 bounded runtime Trace 也属于 Article 29。本文只确认 boot baseline 的源码路径与候选 downstream anchors，不把它们拼成一条尚未闭合的 Agent Run。

## 8. DSH 的五层身份：这里只列调查问题

前面几篇已经建立 Model Wrapper、Runtime、Harness、Host、Product 的通用分层。面对 DSH，很容易反过来给每个 package 强行找一个同名位置，然后宣布映射完成。

更稳的做法，是把这五层先写成调查问题：

| Investigation layer | 需要继续回答的问题 | 本篇不能给出的结论 |
|---|---|---|
| Model Wrapper | Provider route、credential、request/response 转换由谁拥有？ | DSH 等于某个模型 SDK 或 Wrapper |
| Runtime | 谁推进 Turn/Step、tool loop、cancel 与 terminal state？ | `AgentLoop` symbol 已经证明 runtime contract |
| Harness | 哪些 capability、policy、session、trace/recovery 是共享横切层？ | DSH 当前结构是所有 Harness 的标准答案 |
| Host | profile/config、boot、process/UI、approval/interaction 边界由谁拥有？ | “headless”或“web”名字已经证明 Host 责任 |
| Product | Web/Headless、RAG、Skill、Workflow、Subagent 哪些是默认、extension 或候选？ | package/config row 存在就等于 core、default、active |

这张表不是 DSH 架构图，而是一组需要被 falsify 的研究问题。后续文章必须回到 owning file、symbol、call path 和运行实验，而不是让课程里已有的五层模型替源码回答。

## 9. Article 29—37：每篇都要拥有自己的证据链

Article 28 负责路由，不负责替后面九篇提前得出答案。

### Article 29｜从 supported profile 到一次 Agent Run

起点是 `apps/cli/src/bin.ts`、`profile-boot.ts:runProfile`、`packages/boot/app-boot/src/index.ts:boot` 和 headless bundle rows。Article 29 要继续闭合 runner、agent registry/factory、Agent 与 Turn，并配一条 bounded Trace。

如果动态路径仍停在 credential resolution，就必须保留 runtime gap；不能用 static path 补成 Agent Run。

### Article 30｜Plugin lifecycle

“Everything is a Plugin”只能作为调查口号。Article 30 要选择一个真实插件，沿 `apply`、Cordis effect/fiber owner 和 disposer 追踪 install、register、operate、dispose，并观察 disposal 后 contribution 是否消失。

只有 registration，没有 disposal observation，生命周期 Claim 仍不完整。

### Article 31｜Profile、Bundle 与 effective config

调查入口包括 `args.ts:resolveBoot`、`profile-boot.ts:prepareProfile/composeProfile/allPatches`、`dump-config.ts:runDumpConfig`、`app-boot` 的 `composeEntries/renderConfigDump` 以及 shipped patch/preset。

需要冻结 precedence、conflict、missing cases；config dump 仍只能证明 resolution，不能替代 Loader activation。

### Article 32｜Context 到 model request

候选 owner 包括 `packages/core/system-prompt/src/index.ts` 中的 `PromptContext`、`PromptAssembly`、`SystemPrompt`、`renderPrompt`，以及 agent loop 的 `preStep/buildRequest`。

Article 32 要闭合 registration、assembly、request，并对两次 request 做可重放 diff，加入 missing/conflicting Context 负例。一个 `PromptContext` symbol 不能证明最终 request 顺序。

### Article 33｜Inbox、Turn、Step 与停止条件

候选 owner 包括 `ReactLoopAgent.turn/step`、`RuntimeContextProjection`、`executeToolCalls` 与 Session turn/step events。

Article 33 至少要分别保存 no-tool、single-tool、multi-tool、cancellation 四条 Trace。单元测试名或类存在不能替代这四个终态。

### Article 34｜append-only Session events

路线从 `SessionEventMap`、`SessionEvent`、`Session` 进入 `PersistenceCoordinator`、`JsonlSessionPersistence` 和 `SessionProjectionRegistry`。

Article 34 必须把 event vocabulary、append、write、read、projection 与 replay/resume/fork 分开验证。存在 fork/resume tests，不等于任意事件都可重放、任意边界都可 fork，也不提供跨版本兼容承诺。

### Article 35｜Tool execution pipeline

调查入口包括 `ToolRuntime`、`ToolDefinition`、`ToolExecutionResult`、`defineTool`、`validateArgs`、`executeToolCalls` 和 timeout policy 的 `apply/TOOL_TIMEOUT`。

Article 35 要闭合 registry、schema/args、policy、executor、result/event，并运行 bad args、deny、timeout、cancel、large result 五类负例。注册成功不能替代执行、拒绝、超时、取消和大结果边界。

### Article 36｜usage、compaction、cancellation 与 recovery

候选 surface 包括 session stats、`CompactionEngine`、compaction-basic、timeout policy、agent loop、checkpoint policy 与 persistence。

Article 36 必须把 usage、long-session pressure、compaction、cancellation、resume/recovery 当作可以独立失败的证据链。Compaction 不等于 recovery，resume 也不自动等于 crash recovery。

### Article 37｜core fact 与 extension mapping

Article 37 要分别核验 RAG、Skill、Workflow、Subagent、Web/Headless 的 owner、activation 与 default status，并区分 Core Fact、Documented Extension、Source-only Mechanism、Architecture Mapping、Course Proposal。

package、extension table 或 config row 存在，不能直接升级为 core/default/runtime。它最终只能给出 `ADOPT / SIMPLIFY / REJECT / DEFER` 输入，仍不能启动 Part VII。

这九条路线目前都是 `PARTIAL / DEFER`。它们证明的是“固定版中存在精确的调查锚点”，不是“后续结论已经完成”。每篇都要重新核对 SHA，建立自己的 Source Card、counter-evidence、实验与 Trace；后一篇需要更强措辞时，必须补更强 Evidence。

## 10. Developer Preview、安全与版本限制不是脚注

版本限定不应该只放在文末的一行小字里，因为它直接决定结论怎样表达。

固定版 README 把 DSH 标为 Developer Preview，并提醒 compatibility-breaking changes。固定版 `SAFETY.md` 说明项目尚未安全审计，不应被视为 secure 或 production-ready；它还要求最小权限、独立安全控制、凭证和数据最小化、备份以及对 plugin/config/command 的审查。

因此，Part VI 所有 DSH 行为句都要理解为：

```text
At dsh-v0.1.2-alpha.1
@ cd5ef8148158c3a752a658978873241fdf8e2bbc
in the recorded environment and fixture
with the stated credential / network / sandbox conditions
```

“当前固定版源码这样组织”不能改写成“所有 Harness 必须这样组织”。“内置 sandbox、approval 或 permission 可以降低风险”也不能改写成“它们已经提供充分隔离”。本次探针使用外部可丢弃 fixture、read-only permission、disabled telemetry、secret-like environment filtering 和无生产输入，是因为 DSH 自身控制不能替代宿主侧安全边界。

这同样解释了为什么真实 credential 没有进入 baseline。没有 key 会留下 runtime gap，但为了填满文章而读取、打印或消费生产 credential，会把研究完整性建立在更大的安全风险上。正确结果是把 gap 写清楚，而不是绕过它。

## 11. BuildPilot 只带走方法，不在这里长出架构

Part VI 最容易出现的另一个误区，是一边读 DSH，一边把每个 package 映射成 BuildPilot 的未来模块。这样做会让课程 proposal 反过来污染 source fact。

Article 28 的决策边界很窄：

| Decision | Article 28 treatment |
|---|---|
| `ADOPT` | fixed baseline、six-class Evidence、DOC/SOURCE/RUNTIME 分账、failure-as-evidence、counter-evidence、least privilege 与 independent controls |
| `SIMPLIFY` | 本篇不决定；只能作为 Article 37 / Part VI Audit 的后续输入 |
| `REJECT` | 本篇不决定；没有同名 symbol 不能推出能力不存在 |
| `DEFER` | lifecycle、composition、Agent loop、Session、Tool Pipeline、compaction/recovery、extension/product choices 的 BuildPilot 架构吸收 |

换句话说，Article 28 可以采用研究协议，不能设计或实现 BuildPilot。Article 37 可以提供决策矩阵，仍然不能开始 Part VII。只有 Article 28—37 都完成并通过 Part VI Audit 后，Article 38 才可能成为下一阶段候选；这篇文章不会提前创建、预写或实现它。

## 12. 一张可以复用的 Evidence-first 源码研究单

这套方法不只适用于 DSH。研究任何会运行、会更新、会连接外部系统的 Agent 项目，都可以先填写下面这张研究单。

### Identity

- repository、tag、full commit、retrieved at 是什么？
- origin、HEAD、tag target 是否一致？
- working tree 是否 clean？ignored/generated residue 怎样处理？

### Source

- owning file 和 symbol 在哪里？
- caller、callee 与 closed static call path 是什么？
- 路径是否跨入 vendored 或 generated code？
- 对应 test 的 fixture 与 assertion 边界是什么？

### Dynamic

- supported run entry 是什么？
- fixture、environment、input、permission、credential、network、cost 条件是什么？
- exit code、raw stdout/stderr、event/Trace 和 reproduction notes 保存在哪里？

### Interpretation

- Evidence Class 是什么？
- `DOC / SOURCE / RUNTIME` 确认到哪一层？
- Claim Status 是 `CONFIRMED / PARTIAL / PROPOSAL / BLOCKED` 中的哪一个？
- counter-evidence 和 alternative explanations 是什么？
- 它证明什么，又明确不证明什么？

### Course decision

- `ADOPT / SIMPLIFY / REJECT / DEFER` 是哪个？
- 决策依据来自 DSH fact、Inference，还是课程 Proposal？
- 是否已经越过当前阶段授权边界？

实际执行时，顺序也很重要：先做 identity check，再打开 symbol；先写 Research Question、Hypothesis 和 falsifier，再运行命令；Expected Result 与 Observed Result 分开；失败保留 exit code、raw error、reproduction 和 Claim impact；最后才写 Interpretation 和 Course Decision。

这份研究单描述的是应然标准。对本次 baseline，实际可持久复查的是 structured/sanitized record，而不是完整 raw stdout/stderr log；后续文章应在安全前提下补齐理想 Trace，不应反向把本轮记录升级。

## 13. 本篇能建立什么，不能证明什么

到这里，Article 28 可以建立的是：

- DSH 固定 baseline 的 exact identity，以及 source/artifact/excluded boundary。
- 固定版官方 Developer Preview 与 safety posture。
- Part VI 的六类 Evidence、独立 confirmation 与 Claim Status 合同。
- 当前 Windows host 上 bounded install、build、test、CLI help、config dump 和 credential boundary 结果。
- 从 source CLI 到 settled boot code 的 baseline 静态路径，以及 Article 29 尚未闭合的 runner-to-Agent gap。
- Article 29—37 的调查锚点、必须取得的动态证据与停止线。

它不能证明的是：

- full unit suite PASS。
- 没有 host-access caveat 的 build success，或 clean-machine/cross-platform reproducibility。
- headless config rows 已经 activation。
- completed Agent Turn、provider/model request/response、token usage 或 cost。
- DSH 在 Model Wrapper、Runtime、Harness、Host、Product 五层中的最终身份。
- package/extension/config row 一定属于 core、default 或 runtime-active 能力。
- BuildPilot 已经存在、运行、采用 DSH，或 Part VII 已经启动。

最终证据账保持为：

```text
Claims: 16 / 16
Claim status: 6 CONFIRMED / 0 PARTIAL / 10 PROPOSAL / 0 BLOCKED
Evidence Cards: 12 / 12
Card status: 4 CONFIRMED / 0 PARTIAL / 8 PROPOSAL / 0 BLOCKED
Full suite: FAIL (recorded Windows/sandbox run)
Build: PASS_WITH_HOST_ACCESS_CAVEAT
Config dump: effective resolution only
Keyless run: MISSING_CREDENTIAL / no completed Agent Run
BuildPilot: ADOPT METHOD / DEFER ARCHITECTURE
Part VII: NOT STARTED
```

## 14. Claim 与 Evidence Card 索引

下面的索引用于审计覆盖，不把 Proposal 伪装成 DSH fact：

| Claim | Status | Evidence Card | 正文落点与上限 |
|---|---|---|---|
| `28-C01` fixed revision identity | `CONFIRMED` | `28-E01` | Section 2；只证明 snapshot identity |
| `28-C02` Developer Preview / safety ceiling | `CONFIRMED` | `28-E02` | Opening、Section 10；官方姿态，不是独立安全评估 |
| `28-C03` six Evidence Classes | `CONFIRMED` | `28-E03` | Section 3；课程合同，不是 DSH taxonomy |
| `28-C04` SOURCE/RUNTIME confirmation separation | `CONFIRMED` | `28-E03` | Sections 1、3、4；静态路径不能替代 Trace |
| `28-C05` bounded install/build/test outcomes | `CONFIRMED` | `28-E04` | Section 6；current host/run，full suite 保持 FAIL |
| `28-C06` CLI/config/credential boundary | `CONFIRMED` | `28-E04` | Section 6；config dump 非 activation，credential failure 非 Agent Run |
| `28-C07` evidence-upgrade ladder | `PROPOSAL` | `28-E05` | Section 4；有效性等待 Part VI Audit |
| `28-C08` Article 29 profile-to-Agent route | `PROPOSAL` | `28-E06` | Sections 7、9；runner-to-Agent 与 runtime pending |
| `28-C09` Article 30 plugin lifecycle | `PROPOSAL` | `28-E07` | Section 9；register 不能证明 dispose |
| `28-C10` Article 31 composition | `PROPOSAL` | `28-E07` | Section 9；config row/dump 不能证明 activation/precedence |
| `28-C11` Article 32 request assembly | `PROPOSAL` | `28-E08` | Section 9；symbol/registration 不能证明 request Trace |
| `28-C12` Article 33 Turn/Step scenarios | `PROPOSAL` | `28-E08` | Section 9；必须取得四类独立 Trace |
| `28-C13` Article 34 Session events | `PROPOSAL` | `28-E09` | Section 9；source/tests 不能证明 replay/resume/fork semantics |
| `28-C14` Article 35 Tool Pipeline | `PROPOSAL` | `28-E10` | Section 9；registration 不能证明 enforcement/negative terminals |
| `28-C15` Article 36 usage/compaction/recovery | `PROPOSAL` | `28-E11` | Section 9；compaction 非 recovery，resume 非 crash recovery |
| `28-C16` Article 37 core/extension mapping | `PROPOSAL` | `28-E12` | Sections 8、9、11；extension row 非 core/default/runtime，不启动 Part VII |

## 15. Learning Check

1. 为什么 full commit 比 tag name 或 abbreviated SHA 更适合做跨篇证据主键？
2. Official Fact、Source Fact 与 Runtime Observation 为什么不能互相替代？
3. `SOURCE_CONFIRMED` 为什么不自动等于 `RUNTIME_CONFIRMED`？
4. 从一个 symbol 到 runtime Claim，中间至少还缺哪些 rung？
5. 为什么 isolated `27 / 27` 不能把完整 suite 改写为 PASS？
6. 为什么 host-access build success 必须同时保留首次 sandbox failure？
7. config dump 能证明什么，为什么不能证明 activation？
8. `MISSING_CREDENTIAL` 能确认哪个边界，又不能确认哪些行为？
9. DSH 五层身份为什么在 Article 28 只能列调查问题？
10. BuildPilot 在本篇可以采用什么，又必须延后什么？

## 16. 最短结论

把 DSH 当作源码教材，不是把它整理成一份类名和目录的百科，而是把每个结论变成一条可以重建的证据链：固定版本，找到 owner，闭合调用路径，区分测试与应用入口，保存运行 Trace，也保存失败和反证。

当证据只到 symbol，就写 symbol；只到 source path，就写 `SOURCE_CONFIRMED`；只到 credential boundary，就停在 `MISSING_CREDENTIAL`。不让图补全缺失路径，不让成功重试删除失败，也不让课程架构替源码作答。

Evidence-first 源码研究真正训练的，不是“看得更多”，而是知道每个判断什么时候成立、成立到哪里，以及什么时候必须诚实地停下。

> **上一篇**：[Harness 的设计取舍：可替换性、复杂度、Bloat 与演化]({{< relref "ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md" >}})

> **下一篇**：[DeepSeek Harness 总图：从 Host 启动到一次 Agent Run]({{< relref "ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})
