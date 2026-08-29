# Article 28 Outline｜怎样把 DeepSeek Harness 当作 Evidence-first 源码教材

Gate: `OUTLINE`

Author role: `AUTHOR`

Article type: `STAGE_NAVIGATION / SOURCE_METHOD`（原理主线 + 系列路由，不是模块索引）

Course weight: `S`

Required course Lab: `NONE`

Required Evidence work: `BASELINE INSTALL / BUILD / TEST / RUN PROBES`

Evidence posture: `PASS / 16 CLAIMS / 12 CARDS / 6 CONFIRMED / 0 PARTIAL / 10 PROPOSAL / 0 BLOCKED`

DSH baseline: `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`

Runtime posture: `BASELINE OBSERVATIONS PRESENT / COMPLETED AGENT RUN = 0`

BuildPilot boundary: `ADOPT EVIDENCE-FIRST METHOD / DEFER ARCHITECTURE / PART VII NOT STARTED`

## 0. Outline Gate Summary

本篇是 Part VI 的 S 级阶段导航。它先回答“怎样研究一个持续变化、能够运行、但当前仍是 Developer Preview 的 Agent Harness”，再把后续 Article 29—37 的源码与运行证据任务分派出去。它不按目录逐模块介绍 DSH，也不提前回答 DSH 在 Model Wrapper、Runtime、Harness、Host、Product 五层中的最终身份。

文章类型采用“原理主线 + 路由索引”的组合：

```text
问题空间：源码可读不等于结论可证
-> 抽象模型：Evidence Class 与 Claim Status 两条轴
-> 证据升级：identity -> symbol -> call path -> test -> minimal run -> Trace
-> 具体落点：固定 DSH baseline 与失败探针
-> 工程边界：source/artifact/runtime、安全、版本与课程决策
-> 系列路由：Article 29—37 各自闭合自己的证据链
```

第一读者承诺：读完后，读者应能拿着同一套表格研究一个开源 Agent/Harness 项目，知道一个 file、symbol、test、config dump、run failure 或 Trace 分别能证明到哪里，也知道什么时候必须把结论停在 `PARTIAL / NOT_CONFIRMED / PROPOSAL`。

中心判断只规划概念，不在 Outline Gate 写成完整正文：源码教材的价值不在于读到了多少类名，而在于每个结论能否沿固定版本、调用路径、实验与反证被重新核对。

## 1. Safe Front Matter Plan

Target published path: `content/ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md`

```yaml
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
```

Navigation plan:

- Previous: Article 27, `ai-empowerment/agent-engineering-27-harness-design-tradeoffs.md`。
- Course index: `ai-empowerment/agent-engineering-series-index.md`。
- Next: Article 29 只能在其独立 transaction 发布后添加；本篇大纲只做证据路由，不创建 Article 29 workspace。
- Article 38 / Part VII 不出现为“已开始”或“下一步实现”；只允许在停止边界里写 `NOT STARTED / DEFER`。
- Hugo `relref` 只使用 ASCII 双引号；`series_order = 290`、`weight = 3290` 延续课程顺序。

## 2. Required Opening Shape

Opening 不从 package、class、CLI 参数或目录树开始，按以下顺序规划：

1. 从真实工程问题切入：源码文章最容易把“找到了名字”误写成“证明了行为”。
2. 给出三个典型误判：README 有启动命令、源码有 symbol、测试有同名用例，却仍无法证明一次真实 Agent Run。
3. 说明 Part VI 需要先冻结研究对象和证据合同，再进入任何架构解释。
4. 公布版本与安全上限：`dsh-v0.1.2-alpha.1 @ full SHA`、Developer Preview、未安全审计、非 production-ready。
5. 公布本篇证据账：`16 / 16 Claims`、`12 / 12 Cards`、`6 CONFIRMED / 10 PROPOSAL / 0 BLOCKED`；baseline runtime observation 存在，但 completed Agent Run 为 `0`。

Opening 必须提前拆掉四个错误等号：

```text
README says run != command ran
symbol exists != behavior traversed
test exists/passes != application runtime confirmed
effective config dump != plugin/profile activation
```

Evidence: `28-E01`, `28-E02`, `28-E03`, `28-E04`。

Does not prove: DSH 模块总图、Agent lifecycle、Provider 成功请求、生产适用性或 BuildPilot 架构。

Claim coverage: `28-C01`—`28-C06`。

## 3. Teaching Spine

```text
1. 先冻结研究对象，不让版本漂移污染调用链。
2. 再把 Evidence Class 与 Claim Status 拆成两条轴。
3. 用 evidence-upgrade ladder 规定动态措辞怎样获得资格。
4. 把 source、generated artifact、runtime residue 切成不同平面。
5. 用 install/build/test/help/config/keyless run 展示失败也怎样限制 Claim。
6. 只把 DSH 五层身份列为调查问题，不在导航篇给答案。
7. 为 Article 29—37 分配各自必须闭合的 source path、negative case 与 Trace。
8. BuildPilot 只采用 Evidence-first 方法与安全约束，架构吸收全部延后。
9. 在 Article 37 / Part VI Audit 之前保持 Part VII 未启动。
```

## 4. Detailed Article Outline

### 4.1 Section 1｜源码能读，不等于行为已经被证明

Purpose: 立住问题空间，解释为什么 Part VI 必须先写研究方法，而不是先写 DSH 模块目录。

Key points:

- 目录、package、同名 class 只能提供调查入口。
- README 是官方表述，不能替代 pinned implementation 与 direct runtime。
- 测试是特定 fixture 与 assertion 的执行证据，不能自动代表 supported application profile。
- 生成的 `lib/`、catalog、snapshot、网站页面不是默认 source plane。
- 更换 SHA 后，旧 file/symbol/call-path 不能继续拼接进新版本结论。

| 看起来像证据的东西 | 它实际能提供 | 缺少什么时必须停下 |
|---|---|---|
| README 启动步骤 | official supported-path description | 当前 commit source owner 与 command result |
| 目录 / package | candidate ownership surface | registration、caller、lifecycle、activation |
| symbol | static anchor | caller/callee、branch、run traversal |
| test name | candidate executable fixture | assertion、raw result、application/runtime boundary |
| generated output | artifact from some production path | producing command、revision、freshness |

Evidence level: `OFFICIAL_DOC + PINNED_SOURCE + COURSE CONTRACT`。

Does not prove: 任何 DSH Agent Run 或通用 Harness 定义。

Claim/Card coverage: `28-C02`, `28-C03`, `28-C04`, `28-C07` / `28-E02`, `28-E03`, `28-E05`。

### 4.2 Section 2｜先冻结研究对象：Baseline Manifest 是证据链的根

Purpose: 用固定 baseline 解释为什么 repository/tag/full commit/environment 是所有后续结论的共同主键。

| Field | Frozen value / rule |
|---|---|
| Official repository | `https://github.com/deepseek-ai/deepseek-harness` |
| Tag | `dsh-v0.1.2-alpha.1` |
| Full commit | `cd5ef8148158c3a752a658978873241fdf8e2bbc` |
| Verified | `2026-08-30 / Asia/Shanghai` |
| Fixture | external, disposable, not vendored, not committed |
| Identity check | origin + HEAD + local tag + remote tag + clean status |
| Version rule | SHA 变化必须显式 migration，不得悄悄跟 latest main |

Environment block must list only direct observations:

- Windows NT `10.0.19045` / `X64`。
- Node `v24.18.1`。
- project-pinned pnpm `11.7.0`；global pnpm `11.19.0` 不定义项目结果。
- npm `11.16.0`、Git `2.53.0.windows.2`、PowerShell `7.6.4`。

Required distinctions:

- remote tag mapping 是验证时事实，不是远端永不变化保证。
- clean tracked worktree 不代表没有 ignored/generated artifact。
- exact revision identity 不证明 dependency、build、test、run。
- populated offline store success 不证明 clean-store 或 network reproducibility。

Evidence level: `PINNED_SOURCE / SOURCE_CONFIRMED` for identity only。

Does not prove: buildable、testable、runnable 或 production-ready。

Claim/Card coverage: `28-C01`, `28-C05` / `28-E01`, `28-E04`。

### 4.3 Section 3｜六类 Evidence，与确认层级分开记账

Purpose: 建立核心抽象模型，避免把“证据来源”和“Claim 已确认到哪层”混成一列。

| Article wording | Evidence Card value | Can support | Cannot support alone |
|---|---|---|---|
| Official Fact | `OFFICIAL_DOC` | 官方声明、支持入口、前置条件、安全警告 | 当前固定 commit 的实现或实际运行 |
| Source Fact | `PINNED_SOURCE` | 固定 revision 的 file、symbol、static branch/call relation | 真实输入走过该路径 |
| Runtime Observation | `RUNTIME_OBSERVATION` | 指定环境/输入出现的 output、event、error | 未观测分支、内部因果、跨版本稳定性 |
| Experiment | `EXPERIMENT` | 冻结 fixture/变量/步骤后的比较与结果 | 未覆盖平台、生产泛化 |
| Inference | `INFERENCE` | 多项证据之间的显式解释链 | source fact 或 runtime fact |
| Proposal | `DESIGN_PROPOSAL` | 课程采用、简化、拒绝、延后判断 | DSH 已实现或 BuildPilot 已运行 |

| Confirmation | Minimum meaning | Explicit ceiling |
|---|---|---|
| `DOC_CONFIRMED` | 固定版官方文档明确声明 | 不是 implementation/runtime confirmation |
| `SOURCE_CONFIRMED` | owning file + symbol + static path 闭合 | 不是 target path traversed |
| `RUNTIME_CONFIRMED` | supported entry + frozen input/env + raw Trace 实际走过 | 不泛化到未测平台/版本/生产 |

Claim Status (`CONFIRMED / PARTIAL / PROPOSAL / BLOCKED`) 作为第三个独立字段出现；`PINNED_SOURCE` card 仍可能因 call path 未闭合而为 `PARTIAL`，失败的 `EXPERIMENT` 也可能确认一个更窄的 failure-boundary Claim。

Evidence level: `COURSE CONTRACT / DESIGN_PROPOSAL CONFIRMED AS CONTRACT`。

Does not prove: taxonomy 是 DSH 内建 API，或后续文章已满足它。

Claim/Card coverage: `28-C03`, `28-C04`, `28-C07` / `28-E03`, `28-E05`。

### 4.4 Section 4｜从 symbol 到 Trace：结论怎样逐级获得资格

Purpose: 把 evidence-upgrade ladder 写成后续九篇可执行的最小协议。

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

| Rung | Adds | Still does not prove |
|---|---|---|
| Identity | one exact research object | code path or command success |
| Symbol | named owner/anchor | caller, activation or traversal |
| Call path | static reachability and ownership | runtime branch actually taken |
| Test | fixture-scoped executable evidence | supported app/profile behavior |
| Minimal run | bounded supported entry outcome | unobserved branches or production suitability |
| Trace | actual event/output sequence in that scenario | internal causality without source join; cross-version guarantee |
| Interpretation | explicit source/runtime join | fact unless alternatives and ceiling are recorded |

Mandatory rule: 任一 rung 缺失，就保留缺口并缩窄 Claim；架构图、测试名、预期结果都不能替代 raw observation。

Evidence level: `DESIGN_PROPOSAL`，有效性留给 Part VI Audit 回看。

Does not prove: Article 29—37 已经闭合路径。

Claim/Card coverage: `28-C04`, `28-C07`—`28-C16` / `28-E03`, `28-E05`—`28-E12`。

### 4.5 Section 5｜先切 source boundary，再讨论实现

Purpose: 明确 pinned source、generated/artifact 与 excluded runtime state，防止 stale build output 或本机 residue 冒充源码事实。

| Plane | Included examples | Usage rule |
|---|---|---|
| Primary pinned source | root/subtree instructions, pinned docs, `apps/cli/src`, `packages/*/*/src`, owned config/manifests, scripts, tests | 可以建立 official/source Claim，但仍需 owner 与 path |
| Vendored source | `vendor/` + manifest | 调用路径跨入时记录 upstream revision 和 local modifications |
| Generated/artifact | `lib/`, `apps/web/dist/`, build records, generated catalogs/snapshots | 只有记录 producing command/revision 后才作 artifact evidence |
| Excluded local state | `.env`, credentials, sessions, storages, caches, coverage, temp homes | 不进入 pinned-source Claim；secret 绝不抄入课程仓库 |
| Comparison only | latest main, live docs site, another checkout/npm package | 不能静默补 pinned-version 缺口 |

Required nuance:

- test source 属于 pinned source；test result 属于 experiment/runtime fixture evidence。
- build output 可以证明本次构建 artifact，但不能替代 TypeScript source。
- current website 可以作为 current-doc 对照，但不保证与 pinned tag byte-for-byte 一致。
- DSH fixture 保持课程仓库外部，不 vendor、不 commit。

Evidence level: `PINNED_SOURCE / SOURCE_CONFIRMED` for boundaries。

Does not prove: included path 对所有 Claim 都权威，或 ignored artifact 新鲜。

Claim/Card coverage: `28-C01`, `28-C04`, `28-C05`, `28-C07` / `28-E01`, `28-E04`, `28-E05`。

### 4.6 Section 6｜失败也是 Evidence：Baseline Probes 怎样限制措辞

Purpose: 用真实 baseline 结果落地抽象模型；失败案例必须在主线中出现，不得被 isolated PASS 覆盖。

| Probe | Direct outcome | Allowed wording | Forbidden upgrade |
|---|---|---|---|
| frozen install from populated offline store | `exit 0`, 265 workspaces, pnpm `11.7.0` | `PASS_FROM_POPULATED_OFFLINE_STORE` | clean-store/network reproducible |
| build in normal sandbox | `exit 1`, host/client advanced, Vite/esbuild parent-directory access denied | sandbox access failure observed | source build defect |
| unchanged build with necessary host access | `exit 0`, Host/Client/Web complete, 345 modules, 218 artifacts | `PASS_WITH_HOST_ACCESS_CAVEAT` | sandbox-only/cross-host build success |
| full `pnpm run test` | `exit 1`; 32 failed files, 129 failed tests | `FULL_SUITE_FAIL` on recorded Windows/sandbox run | test suite PASS |
| isolated notices test, 30 s | `exit 0`, `27/27` | one default-timeout failure is timing-sensitive | other 128 failures cleared; full suite upgraded |
| built CLI help | `exit 0` | built CLI surface available | profile/service/task started |
| isolated headless config dump | `exit 0`, base + headless rows emitted | effective config resolution observed | Loader activation, Agent creation or Turn |
| keyless headless child | `exit 1`, no timeout/stdout/model result, sanitized `MISSING_CREDENTIAL` | fail-closed credential resolution observed | Agent Run, provider request/response, token usage or cost |

Failure classification must preserve uncertainty:

- 多项 symlink `EPERM`、ACL/sandbox error、process timeout、teardown/cleanup、network restriction 与独立 assertion 共存。
- 不得把 129 个失败全部归为 Windows symlink 或 timeout。
- 早期 Master precheck 的另一组统计只作 historical context，不替换 direct Lab `32 / 129`。
- build 失败与 host retry success 共同定义 caveat；不能删掉首次失败只报 PASS。

Safety conditions:

- no real provider credential read/used/printed；只保留 credential variable name，不保留值。
- telemetry disabled、permission read-only、secret-like environment names removed。
- no public bind、no production input、no provider cost observed。
- DSH 自身 sandbox/permission 不是唯一安全控制。

Evidence level: `EXPERIMENT + RUNTIME_OBSERVATION` for bounded outcomes。

Does not prove: clean-machine reproducibility、cross-platform success、full-suite health、profile activation、Agent Run、model behavior、安全强度或 production readiness。

Claim/Card coverage: `28-C02`, `28-C05`, `28-C06` / `28-E02`, `28-E04`。

### 4.7 Section 7｜DSH 的五层身份：这里只列调查问题

Purpose: 给 Article 29—37 一张“要问什么”的地图，同时明确本篇不抢跑 Host/Runtime/Harness/Product 分层结论。

| Investigation layer | Questions to carry forward | What Article 28 must not conclude |
|---|---|---|
| Model Wrapper | Provider route、credential、request/response 转换由谁拥有？是否只是 model adapter？ | DSH 等于某个模型 SDK/Wrapper |
| Runtime | 谁推进 Turn/Step、tool loop、cancel 和 terminal state？ | symbol 存在就等于 runtime contract 已证实 |
| Harness | 哪些 capability、policy、session、evidence/trace、recovery 是共享横切层？ | DSH 当前结构等于所有 Harness 的标准定义 |
| Host | profile/config、boot、process/UI、approval/interaction 边界由谁拥有？ | “headless”或“web”名字已经证明 Host 责任 |
| Product | Web/Headless composition、RAG/Skill/Workflow/Subagent 哪些属于产品默认、extension 或仅候选？ | package/config row 存在就等于 core/default/active |

Author instruction: 本节只能写问题、证据要求和禁止推论；不能画出已经确认的五层归属图。静态 boot baseline 只作为 Article 29 的起点。

Evidence level: `INFERENCE QUESTION SET + DESIGN_PROPOSAL`。

Does not prove: 五层 owner、边界、生命周期或 runtime traversal。

Claim/Card coverage: `28-C07`, `28-C08`, `28-C16` / `28-E05`, `28-E06`, `28-E12`。

### 4.8 Section 8｜Article 29—37 的 Evidence Routing

Purpose: 把后续文章路由成独立的 falsifiable evidence work，不在 Article 28 复制未来结论。

| Article | Central question | Minimum source closure | Required runtime / negative evidence | Current ceiling |
|---:|---|---|---|---|
| 29 | supported profile 怎样到一次 Agent Run？ | CLI/profile boot -> bundle row -> runner -> agent registry/factory -> Agent -> Turn | bounded run Trace；若仍停 credential boundary，就保留 runtime gap | `PARTIAL / DEFER` |
| 30 | plugin 怎样 install/register/operate/dispose？ | representative `apply` -> Cordis effect/fiber owner -> disposer | disposal 后 contribution 消失；lifecycle/HMR counterexample | `PARTIAL / DEFER` |
| 31 | Profile/Bundle/patch/preset/provider/capability 怎样组合？ | resolve/prepare/compose -> effective config owner -> Loader boundary | precedence、conflict、missing cases；dump 不代替 activation | `PARTIAL / DEFER` |
| 32 | Context 怎样进入 model request？ | registration -> prompt assembly -> `preStep/buildRequest` | two-request diff；missing/conflicting Context negatives | `PARTIAL / DEFER` |
| 33 | Inbox/Turn/Step/loop 怎样推进与停止？ | Agent turn/step -> tool calls -> Session events/terminal | no-tool、single-tool、multi-tool、cancellation four traces | `PARTIAL / DEFER` |
| 34 | append-only event 怎样写、读、投影与继续？ | event/session -> persistence -> projection | event sequence + replay/resume/fork；不预设任意 fork 或 compatibility | `PARTIAL / DEFER` |
| 35 | Tool 怎样从 schema/registry 到 policy/executor/result/event？ | registry/schema -> enforcement owner -> executor -> terminal result/event | bad args、deny、timeout、cancel、large result five negatives | `PARTIAL / DEFER` |
| 36 | usage、compaction、cancel、resume/recovery 怎样分层？ | usage/pressure/compaction 与 cancel/checkpoint/persistence owners 分开闭合 | long session、compaction、cancel、resume terminals；不把 resume 等同 crash recovery | `PARTIAL / DEFER` |
| 37 | RAG/Skill/Workflow/Subagent/Web/Headless 哪些是 core/extension/composition？ | feature owner + profile activation/default path | bounded composition trace 或 source-only downgrade；decision matrix | `PARTIAL / DEFER` |

Cross-article reuse rule:

- 每篇重新验证 repository/tag/full SHA/cleanliness。
- 每篇建立自己的 source card、call path、raw trace 与 counter-evidence。
- 可引用 Article 28 的 baseline，不得继承它未确认的动态结论。
- mock/fake Provider 只能证明 fixture-scoped behavior。
- 后一篇需要更强措辞时，必须补更强 evidence，不能从上一篇推断升级。

Evidence level: exact route anchors `SOURCE_CONFIRMED`；完整 owning paths 与 runtime 均 `PARTIAL / DEFER`。

Does not prove: 任一后续文章已经完成。

Claim/Card coverage: `28-C08`—`28-C16` / `28-E06`—`28-E12`。

### 4.9 Section 9｜Developer Preview、安全与版本限制不是脚注

Purpose: 把官方 posture 转成整个 Part VI 的长期 wording guardrail。

Required boundaries:

- pinned README: Developer Preview，存在 compatibility-breaking change 风险。
- pinned `SAFETY.md`: 未安全审计，不应视为 secure 或 production-ready。
- least privilege、disposable/dedicated environment、backup、credential/data minimization 与 independent controls 必须贯穿实验。
- built-in sandbox/approval/permission 可降低风险，不能当唯一隔离保证。
- 所有 DSH 行为句都带 `dsh-v0.1.2-alpha.1 @ full SHA` 的版本上限。
- “当前固定版如此”不得写成“所有 Agent Harness 必须如此”。

Warning box fields: `Version scope / Environment scope / Credential-provider condition / Network-sandbox condition / Observed-not observed / Does not prove`。

Evidence level: `OFFICIAL_DOC / DOC_CONFIRMED` for posture; no independent security assessment。

Does not prove: 具体漏洞、安全机制有效性、incident history 或生产可用性。

Claim/Card coverage: `28-C02`, `28-C05`, `28-C06` / `28-E02`, `28-E04`。

### 4.10 Section 10｜BuildPilot 只带走方法，不在这里长出架构

Purpose: 明确 `ADOPT / DEFER` 边界，防止从 DSH 源码研究直接跳进 Part VII BuildPilot 设计。

| Decision | Article 28 treatment | Evidence ceiling |
|---|---|---|
| `ADOPT` | fixed baseline、six-class evidence、independent DOC/SOURCE/RUNTIME confirmation、failure-as-evidence、counter-evidence、least privilege 与 independent controls | 课程方法与安全约束，不是 DSH runtime adoption |
| `SIMPLIFY` | `NOT DECIDED HERE`；只允许 Article 37/Part VI Audit 给出后续输入 | 不能在导航篇预写 |
| `REJECT` | `NOT DECIDED HERE`；没有证据时不把缺失 symbol 当作能力缺失 | 不能从 name search 得出架构否定 |
| `DEFER` | DSH lifecycle、composition、Agent loop、Session、Tool Pipeline、compaction/recovery、extension/product choices 的 BuildPilot 架构吸收 | 等 owning article + Part VI Audit，且仍不是 Part VII implementation |

Explicit stop line:

```text
Article 28 may adopt the research protocol.
Article 28 may not design or implement BuildPilot.
Article 37 may only produce decision inputs.
Part VI Audit must complete before any Article 38 / Part VII work.
```

Evidence level: `DESIGN_PROPOSAL`。

Does not prove: BuildPilot exists、runs、adopts DSH、or has a final architecture。

Claim/Card coverage: `28-C03`, `28-C07`, `28-C16` / `28-E03`, `28-E05`, `28-E12`。

### 4.11 Section 11｜读者可以复用的 Evidence-first 源码研究单

Purpose: 落到具体工程实践，但保持方法文而非 DSH API 教程。

```text
Identity
- repository / tag / full commit / retrieved at
- origin / HEAD / tag / cleanliness

Source
- owning file / symbol / caller / callee
- closed static call path
- generated / vendored / excluded boundary

Dynamic
- supported run entry
- fixture / environment / input / safety boundary
- exit code / raw stdout-stderr / Trace

Interpretation
- Evidence Class
- DOC / SOURCE / RUNTIME confirmation
- Claim Status
- counter-evidence / alternative explanations
- proves / does not prove / version ceiling

Course decision
- ADOPT / SIMPLIFY / REJECT / DEFER
- rationale
```

Practical sequence:

1. 先做 identity check，再打开 symbol。
2. 先写 Hypothesis 与 falsifier，再运行命令。
3. expected result 和 observed result 分列。
4. failure 记录命令、exit、raw error 与 Claim impact。
5. 最后才写 interpretation 和 course decision。

Evidence level: `DESIGN_PROPOSAL`，以 baseline experiment 作为 bounded example。

Does not prove: 方法已在 Article 29—37 全部执行成功；由 Part VI Audit 判定。

Claim/Card coverage: `28-C03`—`28-C07` / `28-E03`—`28-E05`。

### 4.12 Section 12｜本篇能建立什么，不能证明什么

Purpose: 在结尾前集中收束证据上限，避免读者把导航篇误读成 DSH 架构结论。

Can establish:

- exact DSH baseline identity and recorded source/artifact boundaries。
- fixed-version official Developer Preview / safety posture。
- Part VI evidence taxonomy and independent confirmation contract。
- current-host bounded install/build/test/help/config/credential outcomes。
- exact baseline source path through settled boot code, with Article 29 runner-to-Agent path still pending。
- Article 29—37 investigation routes and falsifiers。

Cannot establish:

- full unit suite PASS。
- build without host-access caveat or clean-machine/cross-platform reproducibility。
- headless config rows actually activated。
- completed Agent Turn、provider/model request/response、token usage or cost。
- DSH five-layer identity conclusions。
- every extension is core/default/enabled。
- BuildPilot architecture、runtime 或 Part VII start。

Required final status block:

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

Claim/Card coverage: all `28-C01`—`28-C16` / all `28-E01`—`28-E12`。

### 4.13 Section 13｜最短结论

Conclusion plan only, no drafted final paragraph:

- 回扣“源码教材”的标准是可重建证据链，而不是类名数量。
- 强调失败、缺口与 `Does Not Prove` 是研究结果的一部分。
- 把下一步严格指向 Article 29 的独立 source path / runtime work，不写其答案。
- 最后一行只压缩方法判断，不预告 Part VII。

## 5. Claim-to-Section Traceability

| Claim | Status | Outline sections | Evidence Cards | Wording ceiling |
|---|---|---|---|---|
| `28-C01` fixed official revision identity | `CONFIRMED` | opening, 4.2, 4.5, 4.12 | `28-E01` | snapshot identity only; no build/run inference |
| `28-C02` Developer Preview and safety ceiling | `CONFIRMED` | opening, 4.6, 4.9, 4.12 | `28-E02` | official posture only; no security-strength assessment |
| `28-C03` six Evidence Classes | `CONFIRMED` | 4.1, 4.3, 4.10, 4.11 | `28-E03` | course contract, not DSH taxonomy |
| `28-C04` SOURCE/RUNTIME confirmation separation | `CONFIRMED` | opening, 4.1, 4.3, 4.4, 4.5 | `28-E03` | source/test cannot substitute actual Trace |
| `28-C05` install/build/test bounded outcomes | `CONFIRMED` | 4.2, 4.5, 4.6, 4.9, 4.12 | `28-E04` | current host/run only; full suite remains FAIL |
| `28-C06` CLI/config/credential boundary | `CONFIRMED` | opening, 4.6, 4.9, 4.12 | `28-E04` | config dump not activation; missing credential not Agent Run |
| `28-C07` evidence-upgrade ladder | `PROPOSAL` | 4.1, 4.3, 4.4, 4.5, 4.7, 4.10, 4.11 | `28-E05` | method proposal; Part VI Audit must evaluate |
| `28-C08` Article 29 Host/profile-to-Agent route | `PROPOSAL` | 4.4, 4.7, 4.8 | `28-E06` | baseline closes only through settled boot; remainder pending |
| `28-C09` Article 30 plugin lifecycle | `PROPOSAL` | 4.4, 4.8 | `28-E07` | README slogan/register does not prove dispose |
| `28-C10` Article 31 profile/config composition | `PROPOSAL` | 4.4, 4.8 | `28-E07` | config-row existence/dump does not prove activation or precedence |
| `28-C11` Article 32 request assembly | `PROPOSAL` | 4.4, 4.8 | `28-E08` | symbol/registration not request Trace |
| `28-C12` Article 33 Turn/Step scenarios | `PROPOSAL` | 4.4, 4.8 | `28-E08` | AgentLoop/test names not four runtime traces |
| `28-C13` Article 34 Session events | `PROPOSAL` | 4.4, 4.8 | `28-E09` | source/tests do not prove replay/resume/fork semantics |
| `28-C14` Article 35 Tool Pipeline | `PROPOSAL` | 4.4, 4.8 | `28-E10` | registration does not prove enforcement/negative terminals |
| `28-C15` Article 36 usage/compaction/recovery | `PROPOSAL` | 4.4, 4.8 | `28-E11` | compaction != recovery; resume != crash recovery |
| `28-C16` Article 37 core/extension mapping | `PROPOSAL` | 4.4, 4.7, 4.8, 4.10 | `28-E12` | extension/config row != core/default/runtime; no Part VII |

Coverage result: `16 / 16`。

## 6. Evidence-Card-to-Section Traceability

| Evidence Card | Status | Primary sections | Required visible limitation |
|---|---|---|---|
| `28-E01` frozen revision identity | `CONFIRMED` | 4.2, 4.5 | identity/cleanliness does not prove commands or ignored-artifact freshness |
| `28-E02` preview and safety ceiling | `CONFIRMED` | opening, 4.9 | official statement, not independent security audit |
| `28-E03` class/confirmation separation | `CONFIRMED` | 4.3, 4.10, 4.11 | course contract, not DSH feature |
| `28-E04` command paths and direct probes | `CONFIRMED` | 4.6, 4.12 | host caveat, full FAIL, no activation/Agent Run/provider result |
| `28-E05` upgrade ladder | `PROPOSAL` | 4.4, 4.11 | audit still needed |
| `28-E06` Article 29 route | `PROPOSAL` | 4.7, 4.8 | runner-to-Agent and runtime pending |
| `28-E07` Articles 30—31 routes | `PROPOSAL` | 4.8 | lifecycle/config behavior pending |
| `28-E08` Articles 32—33 routes | `PROPOSAL` | 4.8 | request and loop runtime pending |
| `28-E09` Article 34 route | `PROPOSAL` | 4.8 | replay/resume/fork behavior pending |
| `28-E10` Article 35 route | `PROPOSAL` | 4.8 | executor ownership and five negatives pending |
| `28-E11` Article 36 route | `PROPOSAL` | 4.8 | usage/compaction/cancel/recovery terminals pending |
| `28-E12` Article 37 route | `PROPOSAL` | 4.7, 4.8, 4.10 | activation/default/core mapping pending; Part VII stopped |

Coverage result: `12 / 12`。

## 7. Figures and Tables Plan

Draft should use only compact Markdown/ASCII assets; no generated image is required.

1. Evidence-first teaching spine diagram。
2. “看起来像证据 / 实际能证明 / 缺口”对照表。
3. Frozen Baseline Manifest identity/environment table。
4. Six Evidence Classes table。
5. DOC/SOURCE/RUNTIME confirmation table。
6. Evidence-upgrade ladder and rung ceiling table。
7. Source/generated/excluded boundary table。
8. Baseline probe outcome/ceiling table，必须保留 failures。
9. DSH five-layer question-only matrix。
10. Article 29—37 routing table。
11. BuildPilot `ADOPT / SIMPLIFY / REJECT / DEFER` boundary table。
12. Claim and Evidence Card traceability tables。

## 8. Learning Check

1. 为什么 full commit 比 tag name 或 abbreviated SHA 更适合做跨篇证据主键？
2. Official Fact 和 Source Fact 为什么不能互相替代？
3. `SOURCE_CONFIRMED` 为什么不自动等于 `RUNTIME_CONFIRMED`？
4. 一个 symbol 到 runtime Claim 之间至少还缺哪些 rung？
5. 为什么 isolated `27/27` 不能把完整 unit suite 改写为 PASS？
6. 为什么 host-access build success 必须保留首次 sandbox failure？
7. config dump 能证明什么，为什么不能证明 activation？
8. `MISSING_CREDENTIAL` 能确认哪个边界，又不能确认哪些行为？
9. generated artifact 什么时候可以进入 Evidence Card？
10. DSH 五层身份为什么在 Article 28 只能列调查问题？
11. Article 29—37 为什么必须各自重新闭合 call path 与 Trace？
12. BuildPilot 在本篇可以 `ADOPT` 什么，又必须 `DEFER` 什么？

## 9. Practical Actions for Readers

1. 为任意开源 Agent 项目建立一页 Baseline Manifest：repository、tag、full SHA、toolchain、source boundary、credential/network/sandbox condition。
2. 选择一个行为 Claim，沿 `symbol -> call path -> test -> minimal run -> Trace` 标出缺口，并写 `Does Not Prove`。
3. 对一次失败 run 保留 expected/observed、exit code、raw error、alternative explanation 与 Claim impact，而不是删除失败后只展示成功重试。

## 10. Job Competency Coverage

Keep implicit; do not turn this into self-promotion.

| Competency | How the article demonstrates it |
|---|---|
| Source investigation | connects exact revision, owner, symbol and call path instead of listing names |
| Experimental rigor | freezes hypothesis/falsifier/fixture/environment before execution |
| Reliability engineering | keeps failure classes, counterexamples and terminal boundaries visible |
| Security judgment | treats credentials, sandbox, permissions, network and cost as explicit conditions |
| Architecture restraint | separates investigation questions from source facts and defers unsupported mappings |
| Technical leadership | gives Articles 29—37 shared gates without stealing their conclusions |
| Decision discipline | keeps BuildPilot choices as evidence-bounded inputs and stops before Part VII |

## 11. Draft Guardrails

- Do not write a package/module catalog or API tutorial。
- Do not infer lifecycle, precedence, activation, event order, replay, cancellation or recovery from README/class/test names。
- Do not change the frozen repository/tag/full SHA or mix latest main/current website into pinned facts。
- Do not hide the sandbox build failure, full-suite `32 files / 129 tests` failure or unresolved failure classes。
- Do not use isolated `27/27` to upgrade the full suite。
- Do not say the build passes without `host-access caveat`。
- Do not say config dump means Loader/plugin/profile activation。
- Do not say `MISSING_CREDENTIAL` means Agent Run、provider request、model response、token or cost。
- Do not claim Developer Preview is secure, audited or production-ready。
- Do not turn the DSH five-layer question matrix into a confirmed architecture diagram。
- Do not upgrade Article 29—37 route anchors into completed call paths or runtime evidence。
- Do not write BuildPilot as implemented/running or decide its final architecture。
- Do not create, preview-write or start Article 38 / Part VII。
- Do not include real credential values、production inputs、public-bind steps or unbounded model calls。

## 12. Outline Gate Checklist

- [x] Article type fixed as `STAGE_NAVIGATION / SOURCE_METHOD`。
- [x] S-level stage-navigation scope preserved；not a DSH module tutorial。
- [x] Standard Hugo front matter and navigation planned。
- [x] First-reader promise defined。
- [x] Problem space -> abstract model -> concrete baseline -> engineering boundaries -> routing progression preserved。
- [x] Six Evidence Classes planned with Evidence Card aliases。
- [x] DOC/SOURCE/RUNTIME confirmation kept separate from Evidence Class and Claim Status。
- [x] `identity -> symbol -> call path -> test -> minimal run -> Trace` ladder included。
- [x] Fixed Baseline Manifest, source boundary and environment included。
- [x] Sandbox/host build pair and host-access caveat retained。
- [x] Full suite remains `FAIL`; isolated `27/27` cannot upgrade it。
- [x] Config dump explicitly does not prove activation。
- [x] `MISSING_CREDENTIAL` explicitly does not prove Agent Run/provider/model/token/cost。
- [x] Developer Preview, security and version ceilings included。
- [x] DSH Model Wrapper/Runtime/Harness/Host/Product identity appears as questions only。
- [x] Article 29—37 evidence routing included with `PARTIAL / DEFER` ceilings。
- [x] BuildPilot `ADOPT method / DEFER architecture` and Part VII stop boundary included。
- [x] Claim coverage: `16 / 16`。
- [x] Evidence Card coverage: `12 / 12`。
- [x] No draft prose, unsupported DSH fact or future-article conclusion introduced。
