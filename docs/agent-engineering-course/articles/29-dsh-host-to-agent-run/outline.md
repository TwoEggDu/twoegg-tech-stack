# Article 29 Outline｜DeepSeek Harness 总图：从 Host 启动到一次 Agent Run

Gate: `OUTLINE`

Author role: `AUTHOR`

Article type: `ARCHITECTURE_MAP / SOURCE_TRACE`（原理主线 + 固定版本源码落点，不是目录导览或 API 教程）

Course weight: `M`

Required course Lab: `NONE`

Required Evidence work: `HOST_TO_AGENT_RUN SOURCE PATH + BOUNDED TRACE`

Evidence posture: `PASS / 15 CLAIMS / 15 CARDS / 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

DSH baseline: `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`

Runtime posture: `TEST_FIXTURE_RUNTIME_CONFIRMED_WITH_COUNTEREVIDENCE / REAL_PROVIDER_RUNTIME_NOT_CONFIRMED`

BuildPilot boundary: `ADOPT EVIDENCE-TYPED OWNERSHIP MAP / SIMPLIFY ENTRY AND SEAMS / REJECT FALSE EQUIVALENCES / DEFER CONCRETE RUNTIME / PART VII NOT STARTED`

## 0. Outline Gate Summary

本篇是 Part VI 第一篇真正落入 DSH 主执行链的架构图文章。它要解决的不是“仓库里有哪些目录”，而是“怎样证明一个受支持的 application entry 经过 Profile、Bundle、Plugin/Service、Agent、Session、Turn 与 Step，最后形成一次可观察的终态”。

文章采用 TwoEgg 原理篇主结构：

```text
问题空间：目录图、依赖图、配置图与运行时序图经常被画成同一张图
-> 抽象模型：Launch / Composition / Plugin-Service / Runtime Ownership / Durable Observation / Presentation-Control 六个 plane
-> 具体实现：固定 DSH headless profile 的 54-arrow source path
-> 运行校正：exit 0、36-row Session、turn/end(completed) 与 UNKNOWN_TOOL 同时成立
-> 反证边界：Windows owner test exit 1；keyless real-provider path 停在 MISSING_CREDENTIAL
-> 工程取舍：权威事件与输出投影分离；Web Host 不是 headless 必经根
-> 系列路由：只把 Article 30—37 送到各自 owner，不提前证明专题结论
```

第一读者承诺：读完后，读者应能分清 package availability、profile composition、Loader activation、runtime ownership、durable event 与 terminal/UI projection，并能解释为什么一个进程 `exit 0` 仍可能包含失败的 Tool Result。

中心判断只作为 Draft 的写作方向，不在 Outline Gate 展开成完整正文：

> 一张可信的 Agent Harness 总图，不是把模块名连起来，而是让每条边说明关系类型、source owner、runtime 证据与终止上限。

## 1. Safe Front Matter Plan

Target published path: `content/ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md`

```yaml
---
title: "DeepSeek Harness 总图：从 Host 启动到一次 Agent Run"
slug: "agent-engineering-29-dsh-host-to-agent-run"
date: "2026-08-30T00:00:00+08:00"
description: "沿固定版本的 CLI、Profile、Loader、Agent、Session、Turn 与 Step 闭合一条 DeepSeek Harness 主执行链，并用有界 Trace 校正 Host、终态与工具成功边界。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Agent Runtime"
  - "Source Code Reading"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 300
weight: 3300
---
```

Navigation plan:

- Previous: Article 28，`ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md`。
- Course index: `ai-empowerment/agent-engineering-series-index.md`。
- Next: Article 30 只能在其独立 transaction 发布后添加；Article 29 发布时不得创建指向未发布页面的 `relref`。
- Article 30—37 在正文中只按专题编号与调查问题路由，不写成已完成结论。
- Article 38 / Part VII 不出现为“下一步实现”；只允许在 BuildPilot 停止边界中写 `DEFER / NOT STARTED`。
- Hugo shortcode 内只使用 ASCII 双引号；`series_order = 300`、`weight = 3300` 延续课程顺序。

## 2. Required Opening Shape

Opening 不从 `apps/`、`packages/`、`AgentLoop` 或 CLI 参数开始，按以下顺序规划：

1. 从真实工程问题切入：模块齐全的目录图常被误读成一条已经运行的链。
2. 给出三个会直接改变架构图的错误等号：package dependency 等于调用方向、配置 row 等于 activation、Host 等于 WebServer。
3. 引出本文任务：为同一固定 revision 闭合一条 supported headless application path，并用自己的 runtime observation 校正静态图。
4. 提前修正标题术语：本文 `Host` 指承载 CLI、Cordis Context、Loader 与 application tree 的 launch/application process；它不是 `packages/host/*` 的 Web Host。
5. 公布证据账与运行上限：`15 / 15 Claims`、`15 / 15 Cards`、`12 CONFIRMED / 3 PROPOSAL / 0 BLOCKED`；测试夹具 runtime 已取得，真实 provider runtime 未确认。
6. 第一屏必须同时出现这组反直觉事实：direct product fixture `exit 0`、Session `36` rows、`turn/end(completed)`，同一个 authoritative stream 里也有 `tool/result UNKNOWN_TOOL / isError:true`。

Opening 必须拆掉以下错误等号：

```text
directory/package adjacency != call edge
dependency != lifecycle order
configured row != activated capability
Host != Web Host
process exit 0 != every operation succeeded
turn/end(completed) != tool success
deterministic mock != real provider runtime
stdout != authoritative Session history
```

Evidence: `29-E01`—`29-E03`, `29-E08`—`29-E13`。

Does not prove: Plugin 完整生命周期、Profile 冲突语义、Prompt assembly 顺序、四类 Loop、Replay/Resume/Fork、完整 Tool Policy、Recovery、真实 provider/model/token/cost 或 BuildPilot runtime。

Claim coverage: `29-C01`—`29-C03`, `29-C08`—`29-C13`。

## 3. Teaching Spine

```text
1. 先说明为什么目录图不能承担运行图的证明责任。
2. 建立六个 plane 与 typed-edge 图例，把“谁拥有”与“怎样相连”分开。
3. 用 headless/Web 两条 application composition 修正 Host 术语。
4. 把 54 个 source arrows 压成五段主链，同时保留到 exact step range 的回溯。
5. 回到一次 36-row durable Session Trace，说明 source closure 与 runtime traversal 怎样交叉核对。
6. 把 UNKNOWN_TOOL、owner test exit 1 与 MISSING_CREDENTIAL 放在主线里，而不是降为脚注。
7. 说明 Session event stream 为什么是本轮 authority，stdout/stderr 只是 projection。
8. 把 Article 30—37 分派到专题 owner，明确本篇没有提前证明什么。
9. 给 BuildPilot 形成 ADOPT / SIMPLIFY / REJECT / DEFER 输入，但停止在 Part VII 之前。
```

## 4. Detailed Article Outline

### 4.1 Section 1｜为什么目录图画不出一次 Agent Run

Purpose: 立住问题空间，解释 repository map、dependency graph、profile config、runtime call path 与 observation graph 为什么必须分开。

Key points:

- `apps/`、`packages/core/`、`packages/bundle/` 只给出 candidate ownership，不给出运行顺序。
- package manifest 的 dependency/peerDependency 说明 availability 或 contract surface，不说明 caller/callee。
- `cordis.patch.yml` row 是 composition input；Loader 可能等待 service、import/apply 失败或留下 pending fiber。
- Web、Headless 可以共享 base rows，却仍是不同 application composition。
- 一张总图必须给每条边标类型，否则读者无法知道箭头是配置、注入、factory、call、event 还是 projection。

Planned comparison table:

| Map / evidence | Can show | Cannot show alone |
|---|---|---|
| Directory / package map | candidate owners | call direction, activation, order |
| Manifest dependency graph | package availability/contracts | lifecycle and selected runtime |
| Profile / patch graph | configured composition inputs | successful Loader activation |
| Static call path | exact caller/callee in pinned source | observed traversal |
| Durable Session trace | one run's event facts | unobserved branches or cross-platform universality |

Evidence level: `PINNED_SOURCE + DESIGN_PROPOSAL`。

Does not prove: typed-edge taxonomy 是 DSH 内建模型；它是课程为审计性提出的方法。

Claim/Card coverage: `29-C03`, `29-C05` / `29-E03`, `29-E05`。

### 4.2 Section 2｜先把总图拆成六个 plane

Purpose: 建立抽象模型，让后续 DSH 类名只作为模型落点，而不是文章开头的 API 清单。

Six-plane model:

| Plane | Question it answers | DSH landing in this article |
|---|---|---|
| Launch | 哪个受支持入口承载应用 | `dsh` CLI、argv、named profile |
| Composition | 哪些 bundle/profile/patch 构成应用 | `PROFILE_TEMPLATES`、`composeProfile`、base + headless |
| Plugin / Service | 配置如何成为 plugin tree 与 service | Cordis `Context`、Loader、provide/inject/settlement |
| Runtime Ownership | 谁创建并驱动 Agent Run | `AgentRegistry`、`AgentLoop`、`ReactLoopAgent`、Inbox |
| Durable Observation | 谁记录 Turn/Step/tool/terminal facts | `SessionStore`、`Session.append`、flush |
| Presentation / Control | 谁把状态变成 terminal、Web/API/UI | headless summarize/stdout；Web/Control side branch |

Typed-edge legend to keep visible:

- `PROFILE_TEMPLATE / BUNDLE_EXPORT / PATCH_COMPOSE`：组合关系，不是调用时序。
- `LOADER_MOUNT / SERVICE_PROVIDE / SERVICE_INJECT`：plugin/service 关系，不自动等于 consumer 已成功运行。
- `FACTORY_REGISTER / FACTORY_DISPATCH / CALL`：runtime ownership 与执行路径。
- `DURABLE_APPEND / LIVE_EVENT`：authoritative log 与 process-local notification 不可混用。
- `PROJECTION / CONTROL / PRESENTATION`：观察或操控表面，不是底层状态 owner。

Figure plan: 一张从左到右的六层图，箭头颜色/线型按 edge type 分组；图注必须写“plane 可以在同一进程中相遇，但名称不能互相吞并”。

Evidence level: owner locations `SOURCE_CONFIRMED`；六层 taxonomy `PROPOSAL`。

Does not prove: 每个 plane 都必须成为独立进程、package 或 BuildPilot component。

Claim/Card coverage: `29-C02`, `29-C03`, `29-C05`, `29-C06`, `29-C07`, `29-C08` / `29-E02`—`29-E08`。

### 4.3 Section 3｜标题里的 Host，不是 Web Host

Purpose: 修正本文最危险的术语歧义，并给出 shared root / distinct application branches。

Required explanation order:

1. 固定版的 supported Node application boundary 是 `dsh` CLI + named profile。
2. fresh `headless` template 组合 `dsh-base` 与 `dsh-headless`。
3. headless patch 插入 startup 与 direct runner，不插入 WebServer、HTTP/browser、SessionController 或 client UI rows。
4. 因而主路径中的 Host 只表示 launch/application process。
5. `web` template 组合 base + web-app，才进入 WebServer、Host Connection、SessionController 与 browser `AppWebEntry`。

Required branch diagram:

```text
dsh CLI / named profile
  -> app-boot + Cordis Context + Loader
     -> dsh-base (shared configured candidates)
     -> selected application bundle
        headless -> startup -> direct runner -> Agent / Session
        web      -> WebServer -> Control / Connection -> browser UI
```

Author guardrail: 不写“Headless 没有 Host”；应写“Headless 没有 Web Host/HTTP/browser layer，但仍由 launch/application process 承载”。

Evidence level: `SOURCE_CONFIRMED` for pinned headless/Web composition split；Web runtime `NOT_RUN`。

Does not prove: Web server activation、安全性、browser correctness 或 headless/Web runtime equivalence。

Claim/Card coverage: `29-C02`, `29-C04`, `29-C09`, `29-C13` / `29-E02`, `29-E04`, `29-E09`, `29-E13`。

### 4.4 Section 4｜54 个源码箭头，怎样压成五段可读主链

Purpose: 给出具体实现主干，同时保留从正文宏观链回到 `call-path.md` exact caller/callee 的映射。

Required five-phase compression:

| Readable phase | Exact source steps | Compressed chain | Boundary that must stay visible |
|---|---:|---|---|
| A. Launch -> settled plugin tree | `1—14` | installed bin -> parse args -> named profile -> compose/prepare/load -> boot -> Context/Loader -> mount/await/audit | shipped template may differ from a mutable materialized profile；patch composition is not list-order lifecycle |
| B. Loader -> direct headless runner | `15—22` | base/headless rows -> startup task service -> injected runner -> await Loader -> look up core services | configured row does not prove activation；runner ordering comes from injection + settlement |
| C. Registry -> published Agent/Session | `23—31` | AgentLoop registers factory -> `agents.create` -> factory dispatch -> Session prepare -> ReactLoopAgent -> publish Session/Agent | selected path is `AgentRegistry.create -> createAgent`；不要混入 configured startup/resume alternatives |
| D. Inbox -> Turn/Step | `32—47` | whenIdle -> followup -> Inbox splice -> wake/kick -> turn/start -> preStep -> step/start -> request/assistant/tool branch -> step/end -> turn/end | one Turn may contain zero/one/multiple Steps；本篇只闭合 skeleton |
| E. Session -> projection/exit | `48—54` | whenIdle -> `Session.append` -> flush -> summarize -> stdout/stderr -> appExit -> ProcessShutdown | awaited flush does not universally prove a persistence listener wrote bytes |

Required compact chain in the draft:

```text
dsh --profile headless
-> Profile / Bundle composition
-> Cordis Context + Loader settlement
-> headless-startup + direct runner
-> AgentRegistry -> AgentLoop -> ReactLoopAgent
-> Session prepare/publication -> Inbox -> Turn -> Step
-> Session append/flush -> summarize -> appExit
```

Required readable expansion:

- 正文不逐行抄 54-row source register。
- 每个 phase 选择 2—4 个真正改变 ownership 的 symbol 解释。
- 每个 phase 末尾标 exact step range，读者可回到 source package 查完整 caller/callee。
- `54 arrows` 只指 pinned static closure；不能把每个 source arrow 都宣称为 runtime instrumentation 已逐条捕获。

Evidence level: `SOURCE_CONFIRMED` at exact pinned revision；selected central skeleton receives bounded fixture corroboration only in Section 6。

Does not prove: custom factory、完整 plugin lifecycle、request assembly precedence、tool policy、replay/resume/fork、recovery 或所有 terminal branches。

Claim/Card coverage: `29-C01`, `29-C02`, `29-C04`—`29-C10` / `29-E01`, `29-E02`, `29-E04`—`29-E10`。

### 4.5 Section 5｜三个 owner seam：Loader、Factory、Session

Purpose: 不让 54-arrow 图退化成流水账；抽出能迁移到其他 Harness 的三个边界。

#### 4.5.1 Composition -> activation seam

- `composeProfile/allPatches` 产出配置输入。
- `boot -> Context -> Loader -> mountRootInclude -> await/audit` 才建立 plugin tree bridge。
- `headless-runner` 还要受 `headlessStartup` injection 与 Loader settlement 约束。
- Article 30 才负责完整 install/register/operate/dispose；本篇停在 selected boot-to-run bridge。

#### 4.5.2 Interface -> driver seam

- `AgentRegistry` 拥有 `ctx.agents` interface。
- `AgentLoop` 注册 default `AgentFactory`。
- headless consumer 调 `agents.create`，registry 再 dispatch 到 `createAgent` 与 `ReactLoopAgent`。
- 该 seam 支持“接口 owner 与默认 driver 分离”的架构观察，但不证明 custom factory compatibility。

#### 4.5.3 Execution -> observation seam

- Agent driver 追加 Turn/Step/request/assistant/tool terminal events。
- `Session.append` 先提交 event，再发布 process-local `session/event`。
- headless 的 `summarize` 读取 owned Session interval；stdout/stderr 是 projection。
- Article 34 才负责 persistence backend、read/projection、replay/resume/fork。

Figure plan: 三个 seam 的小图，分别标 `input / owner / output / does not prove`。

Evidence level: `SOURCE_CONFIRMED`；Session fixture runtime 在下一节交叉核对。

Does not prove: 这些 seam 是唯一设计方式，或 BuildPilot 应照搬 DSH class/package。

Claim/Card coverage: `29-C05`—`29-C08` / `29-E05`—`29-E08`。

### 4.6 Section 6｜一次 `exit 0` 的 Run，为什么仍然有失败

Purpose: 让 runtime Trace 校正静态图，并把 “completed” 与 “successful” 拆开。

Required experiment banner:

| Field | Observed value |
|---|---|
| Revision / environment | pinned DSH；Windows NT `10.0.19045` x64；Node `v24.18.1`；pnpm `11.7.0` |
| Entry | product source contract，`--profile headless` + repo-owned deterministic `cli-mock` overlay |
| Isolation | new cwd/home/agents；telemetry disabled；secret-like inherited names removed；no external provider |
| Process | `exit 0`，`3146 ms`，no timeout |
| Durable log | one JSONL.zstd Session；`36` rows；one Turn；two Steps |
| Terminal | `turn/end.reason.kind = completed` |
| Counter-evidence | `tool/result.isError = true`，`UNKNOWN_TOOL` for `bash` |

Required event sequence visualization:

```text
session / permission / sandbox / approval
-> inbox splice -> turn/start
-> step 1 -> request -> assistant/message -> tool/call(bash)
-> tool/result(UNKNOWN_TOOL) -> step/end
-> step 2 -> request -> assistant/message
-> step/end -> turn/end(completed)
```

Required interpretation:

- Source closure confirms the path exists in the pinned code；Trace confirms one fixture-scoped traversal reached its central durable skeleton。
- `exit 0` is derived from completed Turn reason in the headless projection path。
- The loop consumed the Tool error and produced a second assistant message；therefore Turn settlement and Tool success are distinct axes。
- stdout `CLI tool round trip complete: Error: unknown tool "bash"` agrees with the persisted terminal message；它不是成功 round trip。
- fixture usage fields are deterministic mock metadata，不能写成真实 provider token/cost。

Required evidence label: `TEST_FIXTURE_RUNTIME_CONFIRMED_WITH_COUNTEREVIDENCE`。

Does not prove: successful Tool execution、real provider/model response、network、latency、token accounting、cost、production permission safety 或 cross-platform behavior。

Claim/Card coverage: `29-C08`, `29-C10`, `29-C11` / `29-E08`, `29-E10`, `29-E11`。

### 4.7 Section 7｜Owner test 为什么在 Windows 明确失败

Purpose: 保留与 direct run 相互独立的 counter-evidence，避免从 process exit 推导测试通过。

Required factual sequence:

1. 冻结的 pnpm wrapper command 把 literal `--` 传给 Vitest，意外收集 `10` files；这个 wider run 只保留为 command-routing failure，不作为 targeted conclusion。
2. 校正后的 exact Vitest command 只运行 owner case。
3. Exact result: `exit 1`；`1 failed / 11 skipped`；snapshot expected local `bash` success，observed `UNKNOWN_TOOL`。
4. Pinned base patch 在 Windows 禁用 `tool-bash`、启用 `tool-pwsh`；deterministic mock 固定请求 `bash`。
5. 因而这是 composition/fixture mismatch，不是 OS sandbox 拒绝；没有 host-access retry。

Planned command block:

```text
node node_modules/vitest/vitest.mjs run --config vitest.expected.config.ts apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts -t "runs one task through the product headless profile command"
```

Planned result table:

| Question | Answer |
|---|---|
| Product path assembled far enough to emit Session/tool events? | Yes, fixture-scoped evidence |
| Owner expected-success snapshot passed on this Windows host? | No, exit 1 |
| Was `bash` tool available in the composed Windows profile? | No, `UNKNOWN_TOOL` |
| Does this establish non-Windows behavior? | No |
| Is this a sandbox-access failure? | No |

Evidence level: `RUNTIME_CONFIRMED` for exact owner-test failure；successful tool round trip `DISCONFIRMED_ON_WINDOWS`。

Does not prove: `pwsh` execution、non-Windows result、sandbox semantics、real-provider behavior、token 或 cost。

Claim/Card coverage: `29-C11` / `29-E11`。

### 4.8 Section 8｜没有凭证的真实 Provider 路线，停在哪里

Purpose: 把 deterministic fixture 与 real-provider composition 的证据类彻底分开。

Required observation block:

| Field | Observed value |
|---|---|
| Composition | product source entry，headless profile，no mock overlay |
| Safety | second isolated cwd；read-only permission；telemetry disabled；secret-like names removed |
| Process | `exit 1`，`3105 ms` |
| Session | `17` durable rows |
| Terminal | `turn/end(error MISSING_CREDENTIAL)` |
| Provider/model outcome | no completed provider request or model response observed |

Required narrow conclusion:

- This run confirms a local fail-closed credential boundary for the recorded composition。
- It does not confirm network dispatch, authenticated behavior, provider latency, model output, token use or cost。
- Probe D's `cli-mock` completion cannot be borrowed to fill this gap。
- Article 28 的 keyless baseline 只作环境前置；Article 29 的 `17`-row negative Session 是本篇自己的 observation。

Evidence level: `RUNTIME_CONFIRMED` for credential failure only；`REAL_PROVIDER_RUNTIME_NOT_CONFIRMED`。

Claim/Card coverage: `29-C12` / `29-E12`。

### 4.9 Section 9｜动手验证：把“调用链成立”和“运行成功”分开

Purpose: 给读者一个可迁移的最小验证流程；不是让读者复制未经边界说明的全权限或真实凭证实验。

#### 4.9.1 Source verification checklist

1. 冻结 official repository、tag、full SHA 与 clean state。
2. 选择一个 supported application entry，不从 package-local demo 或手工 `ctx.plugin(...)` 替代。
3. 为每个箭头记录 caller、callee、file、symbol 与 edge type。
4. 标记 mutable profile、service-gated row、alternate caller、optional flush listener 等反例。
5. 把静态 closure 标成 `SOURCE_CONFIRMED`，不提前写成 runtime success。

#### 4.9.2 Runtime verification checklist

1. 实验前冻结 hypothesis、falsifier、fixture、permission、credential/network/cost boundary。
2. 使用 isolated home/workdir 与 deterministic repo-owned fixture；不使用 production input 或真实 credential。
3. 同时保留 command、exit code、stdout/stderr 与 authoritative durable events。
4. 将 process terminal、Turn terminal、Tool result 分三列检查。
5. 保留失败 snapshot、platform mismatch 与 negative run；不要只展示“最像成功”的行。

Required interpretation table:

| Layer | Article 29 result | Allowed wording |
|---|---|---|
| Static source path | 54 arrows closed | source path exists at pinned revision |
| Product fixture traversal | exit 0 / 36 rows / terminal Turn | one deterministic fixture traversed the bounded skeleton |
| Tool operation | `UNKNOWN_TOOL` | Tool success disconfirmed in this Windows composition |
| Owner test | exit 1 | expected-success contract failed on this Windows host |
| Real provider | `MISSING_CREDENTIAL` | credential boundary observed; provider runtime unconfirmed |

Safety note: 正文不得引导读者填入或输出真实 secret；不得把 `danger-full-access` 名称单独抽离上下文作为推荐配置。若提及它，只能说明这是 owner fixture 的 DSH permission mode，且本轮局限在新建临时目录，并不证明 OS sandbox/security。

Claim/Card coverage: `29-C01`, `29-C10`—`29-C12` / `29-E01`, `29-E10`—`29-E12`。

### 4.10 Section 10｜总图能证明什么，专题必须留给谁

Purpose: 把 scope ceiling 写成架构纪律，不把 Article 29 变成后八篇的摘要答案。

Required route table:

| Article | Owner seed handed off by Article 29 | Question still unproven here |
|---|---|---|
| 30 | Cordis Context/Fiber、Loader Entry tree、representative plugin effect | install/register/operate/dispose 与 contribution removal |
| 31 | Profile templates、bundle/profile/home/CLI patches、service seams | precedence/conflict/effective config 与 missing provider |
| 32 | `preStep`、system prompt owner、request boundary | ordered assembly、duplicate/missing variables、two-step diff |
| 33 | Inbox、wake/kick、Turn/Step skeleton | no-tool/single-tool/multi-tool/cancel 四类 terminal trace |
| 34 | SessionStore/Session/EventMap/persistence plane | append/write/read/projection 与 replay/resume/fork |
| 35 | Tools registry、schemas、`executeToolCalls` anchor | canonicalize/validate/policy/execute/persist 与 negative traces |
| 36 | request/tool/session cross-cutting hooks | usage、compaction、cancel/resume/recovery terminal behavior |
| 37 | base/headless/web 与 extension owners | core/default/optional/extension matrix and final course mapping |

Author wording rules:

- 写“Article 29 找到 owner/anchor，Article N 需要继续验证”，不写“DSH 已经支持完整语义”。
- 不为 Article 30—37 创建未来页面链接；只在 later publication transaction 里补导航。
- Article 30—37 的 `PARTIAL / DEFER` 是本篇 scope boundary，不是本文核心证据缺失。
- 不出现 Article 38 的设计、workspace、实现或启动语气。

Evidence level: `DESIGN_PROPOSAL / COURSE ROUTING`。

Does not prove: later article gate、experiment 或 conclusion 已完成。

Claim/Card coverage: `29-C14` / `29-E14`。

### 4.11 Section 11｜BuildPilot：吸收画图与验图方法，不照搬 runtime

Purpose: 把本篇研究转成 Part VI 决策输入，同时严格停止在 Part VII 之前。

Required decision table:

| Decision | Article 29 input | Evidence boundary |
|---|---|---|
| `ADOPT` | typed owner / typed edge map；authoritative Session events 与 presentation projection 分离；deterministic contract fixture；missing credential fail-closed expectation | 采用审计方法与边界，不采用 DSH package/class identity |
| `SIMPLIFY` | 一个明确 supported entry；显式 capability owner；registry/factory interface seam | 只保留问题与最小接口，不复制多层 profile/plugin roster |
| `REJECT` | “所有 application 都从 Web Host 开始”；“exit 0/Turn completed 等于 Tool 成功”；“mock usage 等于真实 provider token/cost” | reject 的是被 source/runtime 反证的等号，不是对所有 multi-host architecture 的永久否定 |
| `DEFER` | Cordis plugin kernel、layered Profile、concrete AgentLoop、Web multi-host、persistence/replay、Tool policy、Recovery、真实 provider integration | 等 Article 30—37 与 Part VI Audit；Part VII 未启动 |

Required short conclusion: 方法吸收可以早于架构吸收；相似 class/package name 不是适配 BuildPilot 的证据。

Evidence level: `DESIGN_PROPOSAL`。

Does not prove: BuildPilot ADR、implementation、runtime、provider integration 或 Part VII start。

Claim/Card coverage: `29-C15` / `29-E15`。

### 4.12 Section 12｜本篇能建立什么，不能证明什么

Purpose: 在结尾前集中收束静态、动态和课程边界。

Can establish:

- fixed official DSH revision and clean external source identity at check time。
- supported CLI/named-profile boundary and selected fresh headless composition。
- 54-arrow static path from bin/Profile/Loader through Agent/Session/Turn/Step to projection/exit。
- headless direct runner and Web Host/Control are distinct application compositions over shared base candidates。
- one deterministic product-profile fixture produced a 36-row durable Session with terminal Turn。
- the same Session proves `UNKNOWN_TOOL`; owner expected-success test fails on this Windows host。
- keyless real-provider composition fails at `MISSING_CREDENTIAL` and provides no completed provider result。

Cannot establish:

- successful `bash`/`pwsh` tool execution or cross-platform owner-test pass。
- real provider/model request/response、network、latency、token accounting or cost。
- complete Plugin lifecycle、Profile conflict semantics、Prompt assembly、Loop variants、Session continuation、Tool policy or Recovery。
- every configured row activated or every flush wrote a persistence artifact。
- Web server/browser runtime behavior or security。
- DSH production readiness or BuildPilot architecture/runtime。
- Article 30—37 completion or Part VII start。

Required final status block:

```text
Claims: 15 / 15
Evidence Cards: 15 / 15
Claim status: 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED
Static path: 54 arrows / SOURCE_CONFIRMED
Fixture runtime: exit 0 / 36 rows / turn-end completed / tool-result UNKNOWN_TOOL
Owner test: FAIL / exit 1 / Windows bash-vs-pwsh mismatch
Real provider: NOT_CONFIRMED / keyless exit 1 / MISSING_CREDENTIAL
Web branch: SOURCE_ONLY / NOT_RUN
BuildPilot: ADOPT METHOD / SIMPLIFY SEAMS / REJECT FALSE EQUIVALENCES / DEFER RUNTIME
Part VII: NOT STARTED
```

Claim/Card coverage: all `29-C01`—`29-C15` / all `29-E01`—`29-E15`。

### 4.13 Section 13｜最短结论

Conclusion plan only, no drafted final paragraph:

- 回扣总图可信度来自 typed edge + exact owner + bounded Trace，而不是模块数量。
- 用 `exit 0 + UNKNOWN_TOOL` 再次压缩“终态与局部成功必须分层”的判断。
- 下一步只路由 Article 30 的 Plugin lifecycle evidence work，不声称其已经开始。
- 最后一行停止在 Part VI 的研究边界，不预告 BuildPilot 实现。

## 5. Claim-to-Section Traceability

| Claim | Status | Outline sections | Evidence Card | Wording ceiling |
|---|---|---|---|---|
| `29-C01` fixed official revision identity | `CONFIRMED` | opening, 4.4, 4.9, 4.12 | `29-E01` | identity/clean state only；no path/run inference |
| `29-C02` supported CLI + named-profile startup | `CONFIRMED` | opening, 4.2—4.4 | `29-E02` | selected profile branch only |
| `29-C03` typed Repository Map taxonomy | `PROPOSAL` | 4.1, 4.2, 4.11 | `29-E03` | course method, not DSH taxonomy |
| `29-C04` fresh headless = base + headless, prepared/loaded | `CONFIRMED` | 4.3, 4.4 | `29-E04` | mutable materialized profile may differ |
| `29-C05` base patch declares shared rows | `CONFIRMED` | 4.1, 4.2, 4.4, 4.5 | `29-E05` | declaration/composition only；not universal activation |
| `29-C06` Context/Loader boot-to-tree bridge | `CONFIRMED` | 4.2, 4.4, 4.5 | `29-E06` | no full dispose/HMR claim |
| `29-C07` registry/default factory/ReactLoopAgent owner seam | `CONFIRMED` | 4.2, 4.4, 4.5 | `29-E07` | default selected path only；not custom factory |
| `29-C08` Session owns event plane; stdout is projection | `CONFIRMED` | opening, 4.2, 4.5, 4.6 | `29-E08` | no replay/resume/fork or every backend claim |
| `29-C09` headless direct runner excludes Web Host/HTTP/browser | `CONFIRMED` | opening, 4.3, 4.4 | `29-E09` | pinned headless composition only |
| `29-C10` static skeleton closed and bounded fixture traversal | `CONFIRMED` | opening, 4.4, 4.6, 4.9 | `29-E10` | exit 0/completed Turn != Tool/provider success |
| `29-C11` exact owner test fails on Windows bash-vs-pwsh mismatch | `CONFIRMED` | opening, 4.6, 4.7, 4.9 | `29-E11` | no non-Windows or pwsh execution inference |
| `29-C12` keyless real-provider composition stops at credential boundary | `CONFIRMED` | opening, 4.8, 4.9, 4.12 | `29-E12` | no provider/model/token/cost success claim |
| `29-C13` Web/Control and headless are distinct compositions | `CONFIRMED` | 4.3, 4.10 | `29-E13` | Web branch source-only；runtime not run |
| `29-C14` Article 30—37 owner routing | `PROPOSAL` | 4.10, 4.13 | `29-E14` | route only；future behavior not proven |
| `29-C15` BuildPilot adopts method, defers concrete runtime | `PROPOSAL` | 4.11—4.13 | `29-E15` | no ADR/implementation/Part VII |

Coverage result: `15 / 15`。

## 6. Evidence-Card-to-Section Traceability

| Evidence Card | Status | Primary sections | Required visible limitation |
|---|---|---|---|
| `29-E01` frozen source identity | `CONFIRMED` | 4.4, 4.9, 4.12 | source object identity does not prove traversal |
| `29-E02` supported application startup | `CONFIRMED` | 4.2—4.4 | selected named-profile branch only |
| `29-E03` map edge taxonomy | `PROPOSAL` | 4.1, 4.2, 4.11 | course method, not future source-map correctness by itself |
| `29-E04` headless profile/bundle seeds | `CONFIRMED` | 4.3, 4.4 | fresh template; mutable profile may differ |
| `29-E05` shared base rows | `CONFIRMED` | 4.1, 4.2, 4.4 | configured row does not equal activation |
| `29-E06` Plugin Core/Loader bridge | `CONFIRMED` | 4.2, 4.4, 4.5 | full lifecycle belongs to Article 30 |
| `29-E07` Agent registry/default driver | `CONFIRMED` | 4.4, 4.5 | custom factory and universal timing not proven |
| `29-E08` Session event owner and observed stream | `CONFIRMED` | 4.5, 4.6 | one fixture; no continuation/backend generalization |
| `29-E09` headless direct-runner boundary | `CONFIRMED` | 4.3, 4.4 | no Tool/provider success inference |
| `29-E10` closed skeleton + product fixture | `CONFIRMED` | 4.4, 4.6, 4.9 | Tool error and mock boundary must remain visible |
| `29-E11` owner-test Windows counter-evidence | `CONFIRMED` | 4.6, 4.7, 4.9 | successful cross-platform Tool round trip disconfirmed here |
| `29-E12` keyless credential boundary | `CONFIRMED` | 4.8, 4.9 | real provider/model/token/cost not confirmed |
| `29-E13` Web/Control/headless split | `CONFIRMED` | 4.3, 4.10 | Web runtime not run |
| `29-E14` Article 30—37 routing | `PROPOSAL` | 4.10, 4.13 | routes do not prove future articles |
| `29-E15` BuildPilot boundary | `PROPOSAL` | 4.11—4.13 | no concrete runtime choice or Part VII |

Coverage result: `15 / 15`。

## 7. Figures and Tables Plan

Draft should use compact Markdown/ASCII assets; no generated image is required.

1. “五种图不能混成一张”证据能力对照表。
2. Six-plane abstract model with typed-edge legend。
3. Shared base / headless / web composition branch diagram。
4. 54-arrow -> five-phase compression table，保留 exact step ranges `1—14 / 15—22 / 23—31 / 32—47 / 48—54`。
5. Loader、Factory、Session three-seam diagram。
6. Direct fixture environment/result table。
7. 36-row durable event sequence compressed timeline。
8. Process / Turn / Tool 三层终态对照表。
9. Owner test expected/observed/root-cause table。
10. Keyless real-provider negative boundary table。
11. Article 30—37 route table。
12. BuildPilot `ADOPT / SIMPLIFY / REJECT / DEFER` table。
13. Claim and Evidence Card traceability tables。

## 8. Learning Check

1. 为什么 package dependency 不能直接画成 runtime call arrow？
2. Profile template、effective materialized profile 与 Loader activation 分别处在哪一层？
3. 本文标题里的 Host 为什么不是 WebServer？
4. Headless 与 Web 共享 base rows，为什么仍是两条 application composition？
5. 54-arrow 静态链的五个 phase 分别解决什么 ownership question？
6. 为什么 `AgentRegistry` 与 `AgentLoop` 的 factory seam 值得单独画？
7. 为什么 `Session.append` 与 `session/event` 不能视为同一种证据？
8. 为什么 stdout 只是 projection，而 Session interval 才是本次 run authority？
9. direct fixture 为什么可以同时 `exit 0`、`turn/end(completed)` 和 `UNKNOWN_TOOL`？
10. exact owner test 的 Windows failure 为什么不是 sandbox failure？
11. deterministic `cli-mock` usage 为什么不能作为真实 token/cost 证据？
12. `MISSING_CREDENTIAL` 能证明什么，又不能证明什么？
13. Article 29 为什么不能用 source anchors 提前完成 Article 30—37？
14. BuildPilot 本篇可以采用哪些方法，又必须延后哪些 runtime decision？

## 9. Practical Actions for Readers

1. 为自己的 Agent/Harness 画一张 typed-edge owner map：每条边写明 config、inject、factory、call、event 或 projection。
2. 选择一条 supported entry，按 `Launch -> Composition -> Plugin/Service -> Runtime -> Durable Observation -> Presentation` 闭合 caller/callee，并记录 exact revision。
3. 对一次最小 Run 分别记录 process exit、Turn terminal、Tool result 和 authoritative Session events，主动寻找它们互相不一致的情况。
4. 为 deterministic fixture 再配一条 credential-free negative path，确保 mock success 不会被误写成 provider success。

## 10. Job Competency Coverage

Keep implicit; do not turn this into self-promotion.

| Competency | How the article demonstrates it |
|---|---|
| Architecture mapping | separates planes, owners and relation types instead of flattening package layout |
| Source investigation | closes exact caller/callee across CLI, Loader, Agent, Session and process shutdown |
| Experimental rigor | keeps frozen hypothesis, direct Trace, counterexample and credential-negative path together |
| Runtime reasoning | distinguishes process, Turn, Step, Tool and projection terminals |
| Cross-platform judgment | retains the Windows bash/pwsh mismatch without generalizing to other hosts |
| Reliability engineering | treats durable Session events as authority and failures as evidence |
| Architecture restraint | routes specialist semantics to Articles 30—37 and stops before Part VII |
| Decision discipline | converts findings into ADOPT/SIMPLIFY/REJECT/DEFER inputs without pretending BuildPilot exists |

## 11. Draft Guardrails

- Do not start with directory/package/class/API lists。
- Do not call the six-plane or typed-edge taxonomy a DSH-native architecture model。
- Do not use package dependency, directory adjacency or patch row order as a call/lifecycle edge。
- Do not say every base row activated just because it is declared in `cordis.patch.yml`。
- Do not call WebServer, SessionController or browser UI part of the selected headless main path。
- Do not write “Headless has no Host” without clarifying launch/application process versus Web Host。
- Do not present all 54 source arrows as individually runtime-instrumented。
- Do not say one Turn always has one Step；the observed fixture has two。
- Do not say flush universally proves a persistence provider wrote bytes。
- Do not say `exit 0` or `turn/end(completed)` means Tool success。
- Do not hide `tool/result UNKNOWN_TOOL / isError:true` or rewrite stdout as a successful round trip。
- Do not say the owner expected-success test passed；the exact Windows run is `exit 1`。
- Do not blame sandbox access；source-confirmed mismatch is `tool-bash disabled / tool-pwsh enabled` versus mock requesting `bash`。
- Do not use the accidental ten-file pnpm-wrapper run as the targeted result。
- Do not call `cli-mock` a real provider or its fixed usage metadata real token/cost evidence。
- Do not say the keyless run contacted a provider；it stopped at `MISSING_CREDENTIAL`。
- Do not imply provider request/model response/network/latency/token/cost success。
- Do not infer Web runtime behavior from the source-only side branch。
- Do not pre-prove lifecycle、Profile conflict、Prompt assembly、Loop variants、Replay/Resume/Fork、Tool policy or Recovery。
- Do not create future Article 30—37 links before those pages exist。
- Do not start Article 38、BuildPilot implementation or Part VII。
- Do not include real credential values、production input、public-bind steps or unbounded model calls。

## 12. Outline Gate Checklist

- [x] Article type fixed as `ARCHITECTURE_MAP / SOURCE_TRACE`。
- [x] TwoEgg progression preserved: problem space -> abstract model -> concrete implementation -> engineering boundary。
- [x] Standard Hugo front matter and safe navigation planned。
- [x] First-reader promise and shortest judgment defined。
- [x] Host corrected to launch/application process；not Web Host。
- [x] Headless main path and Web/Control side branch kept distinct。
- [x] Six-plane abstract model and typed-edge legend included。
- [x] 54-arrow static path compressed into five readable phases with exact step ranges。
- [x] Source closure kept separate from runtime traversal。
- [x] Direct fixture `exit 0 / 36 rows / one Turn / two Steps / turn-end completed` retained。
- [x] `UNKNOWN_TOOL / isError:true` retained in the same authoritative event stream。
- [x] Owner exact test remains `FAIL / exit 1` on Windows。
- [x] Windows `tool-bash` disabled / `tool-pwsh` enabled versus mock `bash` mismatch retained。
- [x] Keyless real-provider path remains `exit 1 / MISSING_CREDENTIAL / NOT_CONFIRMED`。
- [x] No provider/model/token/cost success implication introduced。
- [x] Session event authority and stdout projection boundary included。
- [x] Article 30—37 only routed to unproven owner questions。
- [x] BuildPilot ADOPT/SIMPLIFY/REJECT/DEFER plan included；Part VII stopped。
- [x] Claim coverage: `15 / 15`。
- [x] Evidence Card coverage: `15 / 15`。
- [x] No draft prose, review, published content or global-state edit introduced。
