# Article 30 Outline｜Everything is a Plugin：插件内核如何承载 Capability 与生命周期

Gate: `OUTLINE`

Author role: `AUTHOR`

Article type: `PRINCIPLE / SOURCE_TRACE / LIFECYCLE`（原理主线 + 固定版本源码落点 + 有界生命周期实验，不是插件目录导览或 Cordis API 教程）

Course weight: `M`

Required course Lab: `NONE`

Required Evidence work: `PINNED SOURCE MAP + ONE REAL PLUGIN LIFECYCLE TRACE`

Evidence posture: `PASS / 15 CLAIMS / 15 CARDS / 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`

DSH baseline: `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`

Runtime posture: `TEST_FIXTURE_RUNTIME_CONFIRMED + REAL_HEADLESS_MOCK_RUNTIME_CONFIRMED / REAL_PROVIDER_NOT_TESTED`

BuildPilot boundary: `ADOPT OWNERSHIP INVARIANTS / SIMPLIFY PLUGIN MACHINERY / DEFER RUNTIME ARCHITECTURE / PART VII NOT STARTED`

## 0. Outline Gate Summary

本篇不把 “Everything is a Plugin” 当作目录口号，也不把它解释成“所有对象都是同一种东西”。它只验证一个可证伪的工程命题：Capability 从配置候选变成可用行为，需要经历 dependency-ready、apply 与 owner-bound registration；当 owner dispose 后，未来贡献必须停止，而已经进入 Session 的历史不能被反向抹掉。

文章采用 TwoEgg 原理篇主结构：

```text
问题空间：把配置、激活、注册、运行和释放都写成“插件已加载”，诊断会失真
-> 抽象模型：Capability / Dependency / Contribution / Owner / Scope / Durable Handoff
-> 具体实现：固定 DSH revision 中 time-context 的 46 步生命周期链
-> 运行反证：missing agents = PENDING；downstream failure 不产生 phantom contribution
-> Dispose 证据：1 -> 1，只撤销未来 listener contribution，不改写既有历史
-> 工程取舍：普通 DI 何时足够，动态 plugin kernel 的组合力何时值得其诊断成本
-> BuildPilot：吸收显式 owner/disposer 与生命周期测试，默认 SIMPLIFY
```

第一读者承诺：读完后，读者应能判断一个插件到底处于 configured、PENDING、ACTIVE、operating 还是 disposed，并能分清 Plugin Context / Model Context、Plugin Event / Session Event、Plugin / Tool 三组最危险的同名误解。

中心判断只作为 Draft 的写作方向，不在 Outline Gate 展开成最终正文：

> 插件内核真正提供的不是“更多模块”，而是一套让依赖、贡献、作用域和清理共享生命周期 owner 的机制；只有这些边界可观察，Capability 才算真的可用、可撤销、可诊断。

## 1. Safe Front Matter Plan

Target published path: `content/ai-empowerment/agent-engineering-30-dsh-plugin-core.md`

```yaml
---
title: "Everything is a Plugin：插件内核如何承载 Capability 与生命周期"
slug: "agent-engineering-30-dsh-plugin-core"
date: "2026-08-30T00:00:00+08:00"
description: "沿固定版本的 time-context 插件闭合配置、依赖、注册、Agent-scoped operation 与 dispose，解释插件内核真正承担的 Capability 所有权和生命周期边界。"
draft: false
tags:
  - "Agent Engineering"
  - "DeepSeek Harness"
  - "Plugin Architecture"
  - "Lifecycle"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 310
weight: 3310
---
```

Navigation plan:

- Previous: Article 29，`ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md`。
- Course index: `ai-empowerment/agent-engineering-series-index.md`。
- Next: Article 31 只能在其独立 transaction 发布后再补 `relref`；Article 30 发布时不得制造指向未发布页面的链接。
- Article 31—37 只作为边界 owner 提及，不提前写它们的 Profile、Prompt、Loop、Session、Tool、Recovery 或 extension 结论。
- Article 38 / Part VII 不启动；BuildPilot 只保留 `ADOPT / SIMPLIFY / DEFER` 决策输入，不写实现计划。
- Hugo shortcode 参数只用 ASCII 双引号；`series_order = 310`、`weight = 3310` 固定。

## 2. Required Opening Shape

Opening 不从 `Context`、`Fiber`、`ctx.on` 或包目录开始，按以下顺序规划：

1. 从真实工程问题切入：配置文件里有一行插件、进程也没有报错，但 Capability 仍可能不可用。
2. 给出最常见的错误问法：“插件加载了吗？”这个二元问题吞掉了 configured、dependency-ready、registered、operating 与 disposed 五种状态。
3. 引出本文方法：选择一个足够小但能闭合全链的真实插件 `time-context`，同时追 source owner、owner tests、bounded probe 与真实 Loader/headless mock composition。
4. 第一屏必须先给术语防火墙：Plugin Context 不是 model context；Plugin Event 不是 Session Event；Plugin 也不等于 Tool。
5. 公布证据账：`15 / 15 Claims`、`15 / 15 Cards`、`12 CONFIRMED / 3 PROPOSAL / 0 BLOCKED`；real provider 没有运行。
6. 提前呈现两个反直觉结果：缺 `agents` 时没有立即 throw，而是 `state=0/PENDING`；dispose 前后 contribution count 为 `1 -> 1`，不是 `1 -> 0`。

Opening 必须拆掉以下错误等号：

```text
configured row != ACTIVE capability
Plugin Context != model/request context
Plugin Event != durable Session Event
Plugin != Service != Tool
Agent-scoped operation != one plugin Fiber per Agent
prepend listener != contribution committed first
dispose != history erase / cancellation / external rollback
mock Loader runtime != real provider runtime
targeted package PASS != whole-repository health
```

Evidence: `30-E01`—`30-E12`。

Does not prove: 所有 DSH plugin 形状一致、完整 Profile 合成、Prompt assembly、Loop variants、Session replay、Tool policy、Recovery、真实 provider/model/network/token/cost，或 BuildPilot 已经需要 plugin kernel。

Claim coverage: `30-C01`—`30-C12`。

## 3. Teaching Spine

```text
1. 先解释为什么“插件已加载”不是可诊断的生命周期状态。
2. 建立六对象最小模型：Capability、Dependency、Contribution、Owner、Scope、Durable Handoff。
3. 用术语防火墙切开 Context、Event 与 Tool 三组错误等号。
4. 说明为什么选择 time-context，而不是遍历所有 plugin。
5. 把 46 步 source path 压成 Configure / Activate / Register / Operate / Persist / Dispose 六段。
6. 用 missing dependency、downstream throw/cancel、invalid config 校正 happy path。
7. 用 owner test 与显式 probe 解释 dispose 的 1 -> 1 语义。
8. 用真实 Loader/headless mock E2E 证明 composition path，同时拒绝 provider/token/cost 外推。
9. 给出普通 DI 足够的判断标准，再解释 plugin kernel 的适用前提与调试代价。
10. 将 BuildPilot 收敛为 SIMPLIFY 输入，并在 Part VII 之前停止。
```

## 4. Detailed Article Outline

### 4.1 Section 1｜“插件已加载”为什么是一个坏诊断问题

Purpose: 立住问题空间，把二元 loaded/not-loaded 拆成可观察生命周期。

Key points:

- 配置 row 只说明候选 membership，不说明模块已 import、dependency 已满足或 `apply` 已完成。
- Fiber 被创建也不等于 ACTIVE；缺 service 时可能合法停在 PENDING。
- listener 已注册不等于它一定对当前 subject 生效；还要看 scope filter 与 waterfall order。
- process/Loader 成功不等于每个 downstream contribution 都成功。
- dispose 不是“删掉插件痕迹”，而是 owner 对 future effects 的反向清理。

Planned state table:

| State | What is known | What is still unknown |
|---|---|---|
| Configured | Loader row names a module | import, dependency, apply, operation |
| Imported / Fiber created | callback identity and owner exist | dependency-ready and ACTIVE |
| PENDING | missing runtime service is observable | capability is unavailable |
| ACTIVE | config/apply/effect setup settled | whether a specific operation is admitted |
| Operating | a scoped event reached the listener | whether result crossed durable boundary |
| Disposed | owner effects were reversed | old durable history remains |

Figure plan: “一个 loaded 词吞掉六个状态”的横向状态图；每个箭头标 source owner 与可观察证据。

Claim/Card coverage: `30-C02`, `30-C04`, `30-C05`, `30-C09` / `30-E02`, `30-E04`, `30-E05`, `30-E09`。

### 4.2 Section 2｜先建立一个不依赖 Cordis 的插件生命周期模型

Purpose: 建立抽象层，避免文章退化成框架 API 说明书。

| Abstract object | Responsibility | Failure question |
|---|---|---|
| Capability | 对 consumer 可用的能力 | 名字存在，还是行为真的 ready？ |
| Dependency | Capability 可用前必须成立的条件 | 缺失时 fail-fast、PENDING，还是降级？ |
| Contribution | service、listener、effect、tool 等具体贡献 | 它由谁注册、对谁生效？ |
| Owner | 收集 contribution 与 disposer 的生命周期单元 | teardown 是否能找到所有反向动作？ |
| Scope / Subject | 决定本次 operation 对谁、哪些 listener 生效 | owner scope 与 dispatch subject 是否混淆？ |
| Durable Handoff | 把瞬时计算转成可回放事实的边界 | live hook 结果是否真的持久化？ |

Required lifecycle model:

```text
configured -> imported / owner created -> dependency-ready -> apply
-> owned contribution registered -> scoped operation
-> explicit durable handoff -> reverse disposal
```

Required invariant: `register(contribution) and dispose(contribution) must share one owner`。

Boundary note: 这套六对象模型是课程抽象，不宣称是 DSH 官方术语，也不要求每个对象成为独立 class/package/process。

Claim/Card coverage: `30-C03`, `30-C06`, `30-C08`, `30-C11`, `30-C13` / `30-E03`, `30-E06`, `30-E08`, `30-E11`, `30-E13`。

### 4.3 Section 3｜三组术语防火墙：Context、Event、Tool

Purpose: 在进入具体源码前固定名词边界。

| Easy-to-confuse pair | In this article | Never write as |
|---|---|---|
| Plugin Context / Model Context | 前者是 Cordis DI/effect lifecycle container；后者是进入模型请求的消息材料 | 同一个 Context 被 plugin 直接修改 |
| Plugin Event / Session Event | 前者是 process-local `agent/pre-step` waterfall；后者是 `Session.append` 的 durable vocabulary | hook 调用本身已持久化 |
| Plugin / Service / Tool | Plugin 是 lifecycle owner/extension unit；Service 是 named capability；Tool 走独立 model-visible path | 每个 plugin 都是 service 或 tool |
| Fiber owner / dispatch scope | Fiber 持有 effect；`scopeTarget(agent, agent)` 选择 operation subject | Agent-scoped operation 等于每 Agent 一个 Fiber |

Concrete negative facts:

- `time-context` 消费 `agents` Service，不提供新 Service。
- 它调用 `ctx.on`，没有 `ctx.tools.register` / `ToolRuntime.register`，因此不是 Tool。
- 它不在 `apply` 里直接 `Session.append`；只返回修改后的 `PreStepDecision`。
- 后续 `ReactLoopAgent.turn` 才 append `step/start` 与 `user/message`。

Claim/Card coverage: `30-C03`, `30-C07`, `30-C08`, `30-C10`, `30-C11` / `30-E03`, `30-E07`, `30-E08`, `30-E10`, `30-E11`。

### 4.4 Section 4｜为什么用 `time-context` 闭合一条链

Purpose: 说明代表样本的选择逻辑与外推上限。

Selection reasons:

1. shipped opt-in composition row 真实存在；
2. namespace 导出 `name / inject / Config / apply`，能经过 Loader unwrap；
3. 消费真实 `agents` Service并注册真实 `agent/pre-step` listener；
4. `ctx.on` 降为 owner Fiber effect；
5. operation 经 exact Agent carrier，contribution 再跨入 Session durable vocabulary；
6. owner test 与 bounded probe 同时覆盖 dispose 后无新增；
7. real Loader/headless mock E2E 覆盖两 turn 的 ordered contribution。

Required limitation: 一个代表插件只能证明所选 shape/path；不能推出所有 plugin 的 shape、cleanup 或 “Everything is a Plugin” 绝对本体论。

Evidence identity box: official repository/tag/full SHA；clean external fixture before/after；Windows 10 x64、Node `v24.18.1`、Vitest `4.1.8`；no credentials/network/provider；owner tests unchanged。

Claim/Card coverage: `30-C01`, `30-C02`, `30-C12` / `30-E01`, `30-E02`, `30-E12`。

### 4.5 Section 5｜46 步源码链，压成六段生命周期

Purpose: 落到具体实现，同时保留 exact source trace 的回溯能力。

| Phase | Exact steps | Readable chain | Boundary to keep visible |
|---|---:|---|---|
| A. Configure / Import | `1—7` | config row -> Entry -> disabled gate -> import -> unwrap -> Registry start/await | row membership 不等于 activation |
| B. Dependency / Activate | `8—18` | plugin shape -> inject agents -> Fiber -> provider Service -> epoch -> config -> apply -> ACTIVE | missing agents 时 PENDING；YAML order 不是 dependency contract |
| C. Register / Own | `19—25` | validate/setup -> `ctx.on` -> Events register -> Fiber effect -> hook + disposer | listener 是 owner-bound Effect，不是孤儿 side table |
| D. Scoped Operate | `26—34` | preStep -> Agent carrier -> scope filter -> waterfall -> `await next()` -> sourced message -> decision | global listener 对 exact Agent operation；prepend 不等于先 commit |
| E. Durable Handoff | `35—40` | accepted decision -> step/start -> user/message -> validate/freeze/push -> model projection | Plugin Event 与 Session Event 分界；backend flush 不在本篇 |
| F. Dispose / Retain | `41—46` | fiber.dispose -> unload -> reverse effects -> unregister -> no future contribution -> old event retained | unregister 不是 cancellation、history erase 或 rollback |

Required compact source chain:

```text
Loader row -> import / unwrap -> Registry + Fiber(PENDING)
-> agents Service resolved -> config + apply -> ctx.on -> Fiber-owned effect
-> Agent-scoped pre-step -> sourced UserMessage proposal
-> Session.append(user/message)
-> fiber.dispose -> reverse effect -> unregister
```

Author guardrail: 正文不逐行抄 46-row register；每阶段挑 2—4 个改变 ownership 的 symbol。`46 steps` 是 pinned static closure，不是逐步 runtime instrumentation。provider replacement/reload 只保留 source-confirmed side path。

Claim/Card coverage: `30-C02`—`30-C11` / `30-E02`—`30-E11`。

### 4.6 Section 6｜Configured、PENDING、ACTIVE：依赖不是 import order

Purpose: 用 `agents` provider/consumer 关系解释 lifecycle-aware dependency。

Required factual sequence:

1. `AgentRegistry extends Service`，构造时以 `agents` 名称提供 capability；provider registration 本身也是 provider Fiber 的 effect。
2. `time-context.inject = ['agents']` 成为 runtime required-name map，不是 TypeScript import 或 constructor parameter。
3. consumer Fiber 根据 active provider uid 形成 epoch；dependency-ready 后才 resolve config、执行 `apply`。
4. 缺 `AgentRegistry` 的 probe 输出 `inject=['agents'] / missing=['agents'] / state=0`。
5. frozen source 将 state `0` 映射为 `PENDING`；本轮没有 immediate throw。
6. 更高 app-boot 层可能审计 unresolved entry，但那是另一个 owner boundary。

| Tempting inference | Observation | Allowed wording |
|---|---|---|
| 配置里有 row，所以已 active | missing service 时 state 0 | configured candidate, not active capability |
| 没 throw，所以运行正常 | Fiber reports missing agents | legal composition but parked PENDING |
| package/YAML 排前面就是依赖满足 | resolver checks active named provider | dependency is runtime lifecycle relation |
| provider 换对象就是指针替换 | source uses provider uid epoch and unload/reload | source-confirmed only; replacement not lab-run |

Claim/Card coverage: `30-C04`, `30-C05`, `30-C10` / `30-E04`, `30-E05`, `30-E10`。

### 4.7 Section 7｜Register 与 Operate：全局 listener 怎样执行精确 Agent 工作

Purpose: 拆开 effect owner、event scope 与 operation subject。

```text
time-context.apply
-> ctx.on(agent/pre-step, prepend)
-> EventsService.register
-> current Fiber.effect collects unregister

ReactLoopAgent.preStep
-> agentEvents.waterfall
-> scopeTarget(agent, agent)
-> filter admits global time-context hook
-> listener awaits next()
-> accepted decision gets one plugin-attributed UserMessage
```

Required nuances:

- selected plugin Fiber 是全局 listener owner，不是每个 Agent 一个 plugin instance。
- exact Agent 通过 event carrier/payload 进入 operation；scope routing 与 DI isolation 不是同一机制。
- `{ prepend: true }` 只影响 hook position；listener 先 `await next()`。
- downstream throw/cancel 两个 owner cases 均通过，且零 reading、零 adapter request、零 `step/start`。
- contribution 先是 proposed message；Session append 后才成为 durable event。

Figure plan: registration/owner 与 per-Agent dispatch 双泳道在 listener 交汇，再在 Session append 处分离 transient 与 durable。

Claim/Card coverage: `30-C06`, `30-C07`, `30-C08`, `30-C11` / `30-E06`, `30-E07`, `30-E08`, `30-E11`。

### 4.8 Section 8｜Dispose 的中心证据：为什么是 `1 -> 1`，不是 `1 -> 0`

Purpose: 用最小可观察反事实解释 reversible effect。

Required owner-test lifecycle:

1. fresh Context 挂载 `AgentRegistry` 与真实 `time-context`，保留 plugin Fiber；
2. 同一 Session/Agent 第一次 eligible pre-step 产生一条 contribution；
3. `await fiber.dispose()`；
4. 同一 Context/Agent 第二次 eligible pre-step；
5. Session 内 time-context contribution 总数仍为一条。

```text
beforeDispose = 1
afterDispose  = 1
firstSource.kind = plugin
firstSource.plugin = time-context
firstSource.form = snapshot
```

Required interpretation:

- 第一条仍可计数，证明旧 Session history 没被删除。
- 第二次没有新增，证明未来 listener contribution 停止。
- source chain 对应 `fiber.dispose -> _unload -> reverse effect disposers -> EventsService.unregister`。
- 证据通过“第二次效果缺席”观察清理，不窥探私有 hook table。

Forbidden interpretations: dispose 取消已开始 callback、回滚外部世界、删除 durable history、证明所有 plugin effect，或 `1 -> 1` 是泄漏。

Planned diagram:

```text
t0 register listener
t1 pre-step -> append history[0]
t2 dispose -> unregister listener
t3 pre-step -> no new contribution

history count: 1 -----------------> 1
future behavior: enabled ---------> disabled
```

Claim/Card coverage: `30-C06`, `30-C09` / `30-E06`, `30-E09`。

### 4.9 Section 9｜运行证据与反证：PASS 也必须标上限

Purpose: 把 owner tests、bounded probes、real Loader/headless mock 与 real provider 分层。

| Probe | Result | Proves | Does not prove |
|---|---|---|---|
| exact dispose owner test | `exit 0 / 1 passed / 18 skipped` | representative listener stops future contribution | private hook state, arbitrary effects |
| explicit count probe | `1 -> 1` | old history retained, no second contribution | cancellation/external rollback |
| AgentLoop owner fixture | `exit 0`，two requests/two contributions | per-request order/source attribution | production provider/model |
| Loader unwrap owner test | `exit 0` | namespace metadata + apply activation with agents | all Loader paths |
| invalid zone/interval cases | expected rejections PASS | fail-loud config boundary | partial activation |
| missing agents probe | `state=0/PENDING` | inactive consumer names dependency | higher boot error semantics |
| downstream throw/cancel | `2 passed`，no reading/request/step | prepend listener delegates first | every waterfall listener |
| full package spec | `19/19 passed` | owner assertions coexist | full monorepo health |
| real Loader/headless E2E | `exit 0 / 1 passed`，two persisted contributions | real composition with deterministic mock LLM | real provider/network/token/cost |

Failures that must remain visible:

- `corepack pnpm --version` 在 course cwd 尝试 registry fetch，`EACCES / exit 1`；没有网络重试。
- 首次 `*.e2e.ts` 使用错误 collector，`No test files found / exit 1`；校正为 repo-owned `vitest.e2e.config.ts` 后才有效 PASS。
- 首版 `tsx -e` 因 CJS top-level await transform 失败；只包 async IIFE 后重跑，假设与输入未变。

Required label: `TEST_FIXTURE_RUNTIME_CONFIRMED + REAL_HEADLESS_MOCK_RUNTIME_CONFIRMED`。

Mock boundary: deterministic repo-owned mock 证明 Loader/Agent/Session composition path；它没有 credential、network、provider output、真实 token accounting 或 cost 证据。

Claim/Card coverage: `30-C01`, `30-C02`, `30-C04`—`30-C09`, `30-C12` / `30-E01`, `30-E02`, `30-E04`—`30-E09`, `30-E12`。

### 4.10 Section 10｜普通 DI 什么时候已经足够

Purpose: 回到工程取舍，避免把强机制默认当成熟架构。

普通 constructor DI + composition root + explicit start/stop 足够，当当前需求大体满足：

- capability 集合稳定，依赖在启动时确定；
- 不需要运行期 provider rebinding、HMR 或 plugin marketplace；
- 不需要多个动态 isolation scope；
- contribution 数量有限，owner 与 disposer 可以显式列举；
- order 可用显式 pipeline/contributor list 表达；
- 启动/停止可由一个 composition root 编排并测试。

更强 plugin kernel 才值得评估的触发条件：provider 需运行期替换并驱动 unload/reload；extension 来自独立团队或第三方；同一进程存在动态 scope/isolation；HMR、late activation 或 partial availability 是产品需求。

| Mechanism | Composition power | Diagnostic cost |
|---|---|---|
| string-key dynamic inject | runtime rebinding / late availability | typo, name, isolation, PENDING diagnosis |
| proxy/mixin Context | uniform extension surface | hidden owner and access path |
| waterfall + prepend | composable interception | order, delegate-first and veto reasoning |
| Fiber-owned reverse effects | structured teardown | helpers must preserve owner semantics |
| explicit DI + contributor list | fewer dynamic dimensions | less runtime extensibility |

Evidence posture: DSH mechanism事实为 `CONFIRMED`；“何时选择普通 DI”是 `PROPOSAL`，不能写成 DSH 官方动机或实测生产率。

Claim/Card coverage: `30-C13`, `30-C14` / `30-E13`, `30-E14`。

### 4.11 Section 11｜动手验证：不要只断言 disposer 存在

Purpose: 给读者一个可迁移的最小生命周期实验，不要求复制 DSH 或使用真实 provider。

#### 4.11.1 Source verification

1. 固定 repository、tag、full SHA 与 clean state。
2. 记录 configured entry、dependency provider、consumer apply、registration helper 与 exact disposer owner。
3. 为边标 `CONFIG / INJECT / REGISTER / DISPATCH / DURABLE_APPEND / DISPOSE`。
4. 分开 owner scope、dispatch subject 与 durable record owner。
5. 找一个负例：missing dependency、invalid config、downstream cancel 或 post-dispose operation。

#### 4.11.2 Runtime verification

1. 冻结 hypothesis：第一次 eligible input 产生一次 contribution；dispose 后同输入不再新增。
2. 使用同一 Context、subject、输入构造前后对照；只改变 owner lifecycle。
3. 记录 `before count / dispose completion / after count / source attribution`。
4. 再跑 unchanged owner test，避免临时 probe 代替仓库 contract。
5. composition E2E 必须标明 mock 或真实 provider，不得借 mock 填 provider 空白。

```text
given one active owner and one eligible input
when input runs before disposal
then contribution count increases by one

when the same owner's disposal completes
and the same eligible input runs again
then historical count is unchanged
and no future contribution is emitted
```

Safety note: 不需要真实 credential、网络、production input 或 provider 调用；不要修改 owner expectation 来让测试通过。

Claim/Card coverage: `30-C01`, `30-C06`, `30-C09`, `30-C12` / `30-E01`, `30-E06`, `30-E09`, `30-E12`。

### 4.12 Section 12｜BuildPilot：吸收 owner 约束，默认 `SIMPLIFY`

Purpose: 将研究转成 Part VI 决策输入，但不进入 Part VII 设计或实现。

| Decision | Article 30 input | Boundary |
|---|---|---|
| `ADOPT` | explicit dependency；configured/ready 分开；contribution/disposer 同 owner；transient/durable 分开；post-dispose negative test | 吸收 invariant 与测试，不照搬 class/package identity |
| `SIMPLIFY` | composition root + typed interfaces + explicit contributor order + owner-held disposers | 没有 runtime rebinding/HMR/multi-scope/ecosystem 需求证据 |
| `REJECT` as default | ambient string-key dependency、隐式 waterfall/order、用 Plugin 吞掉 contribution 类型 | reject 默认复杂度，不是否定 DSH 已有需求 |
| `DEFER` | dynamic registry、provider epoch reload、proxy Context、multi-host/plugin marketplace | 等明确需求与授权；Part VII 未启动 |

Required sentence: 本篇可以把“register 与 dispose 必须共享 owner”带入未来 ADR；不能把 Cordis plugin kernel 直接写成 BuildPilot 既定架构。

Evidence posture: `30-C15 / 30-E15 = PROPOSAL`。

### 4.13 Section 13｜本篇能建立什么，不能证明什么

Can establish:

- fixed official revision and clean fixture boundary；
- one real production plugin's Loader-compatible identity；
- 46-step configured-to-dispose pinned source closure；
- configured、PENDING、ACTIVE 区别与 missing-service observation；
- `ctx.on` listener is plugin-Fiber-owned reversible effect；
- global listener performs exact Agent-scoped operations；
- Plugin Event、decision message 与 durable Session Event are separate；
- dispose count `1 -> 1`: old history retained, future contribution stopped；
- targeted/full owner tests and real Loader/headless mock E2E pass in recorded environment。

Cannot establish:

- all plugins share one shape/lifecycle；provider replacement runtime beyond source；
- in-flight cancellation、external rollback or arbitrary cleanup；
- every scoped-routing/service-isolation behavior；whole-repository health；
- real provider/model/network/latency/token/cost；Article 31—37 conclusions；
- BuildPilot ADR/runtime/code or Part VII start。

```text
Claims: 15 / 15
Evidence Cards: 15 / 15
Claim status: 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED
Static lifecycle: 46 steps / SOURCE_CONFIRMED
Owner lifecycle: PASS / before 1 / after 1
Missing dependency: state 0 / PENDING / no immediate throw
Composition runtime: REAL HEADLESS MOCK / PASS
Real provider: NOT TESTED
BuildPilot: ADOPT INVARIANTS / SIMPLIFY MACHINERY / DEFER ARCHITECTURE
Part VII: NOT STARTED
```

Claim/Card coverage: all `30-C01`—`30-C15` / all `30-E01`—`30-E15`。

### 4.14 Section 14｜系列导航与最短结论

Navigation wording:

- 回看 Article 29：它负责 Host/Profile/Loader 到 Agent Run 总图；Article 30 只深入 Plugin lifecycle owner。
- Article 31—37 只按问题 owner 路由：Profile composition、Prompt/context assembly、Loop/Step、Session continuation、Tool path、Recovery/observability、最终 DSH mapping；不陈述未完成结论。
- 不创建未发布 `relref`，不出现 Article 38 workspace、设计或启动语气。

Conclusion plan: 回扣“插件化价值不是模块数量，而是 contribution 能否由同一 owner 创建、观察和撤销”；用 `PENDING` 与 `1 -> 1` 压缩反直觉判断；最后停在“先让 lifecycle invariant 可证，再决定是否需要更强 plugin machinery”。

## 5. Claim-to-Section Traceability

| Claim | Status | Outline sections | Evidence Card | Wording ceiling |
|---|---|---|---|---|
| `30-C01` frozen official revision and clean fixture | `CONFIRMED` | 4.4, 4.9, 4.11, 4.13 | `30-E01` | identity/clean state only |
| `30-C02` real Loader-compatible time-context plugin | `CONFIRMED` | 4.4, 4.5, 4.9 | `30-E02` | one representative plugin only |
| `30-C03` Plugin Context differs from model Context | `CONFIRMED` | 4.2, 4.3, 4.5 | `30-E03` | no full Prompt/context assembly claim |
| `30-C04` configured/PENDING/ACTIVE are distinct | `CONFIRMED` | 4.1, 4.5, 4.6 | `30-E04` | selected Loader/Fiber path only |
| `30-C05` agents dependency; missing remains PENDING | `CONFIRMED` | 4.6, 4.9 | `30-E05` | no immediate throw; replacement source-only |
| `30-C06` ctx.on listener is Fiber-owned Effect | `CONFIRMED` | 4.2, 4.5, 4.7, 4.8 | `30-E06` | no private-hook/all-effects inference |
| `30-C07` Agent-scoped message contribution, not Tool | `CONFIRMED` | 4.3, 4.7, 4.9 | `30-E07` | global listener; not per-Agent Fiber |
| `30-C08` Plugin Event/decision/Session Event differ | `CONFIRMED` | 4.2, 4.3, 4.5, 4.7 | `30-E08` | no persistence/replay generalization |
| `30-C09` dispose keeps count 1 -> 1 | `CONFIRMED` | 4.1, 4.5, 4.8, 4.11 | `30-E09` | no cancellation/history erase/rollback |
| `30-C10` Service/Event/Tool contribution kinds differ | `CONFIRMED` | 4.3, 4.6 | `30-E10` | module-scoped absence only |
| `30-C11` Fiber owner and Agent dispatch scope differ | `CONFIRMED` | 4.2, 4.3, 4.7 | `30-E11` | selected dispatch only |
| `30-C12` owner/full-spec/headless-mock evidence passes | `CONFIRMED` | 4.4, 4.9, 4.11, 4.13 | `30-E12` | package/mock scope; not full repo/provider |
| `30-C13` dynamic composition adds diagnostic dimensions | `PROPOSAL` | 4.1, 4.10 | `30-E13` | conditional inference; no measured cost |
| `30-C14` ordinary DI sufficiency criteria | `PROPOSAL` | 4.10 | `30-E14` | rubric, not DSH fact |
| `30-C15` BuildPilot defaults to SIMPLIFY | `PROPOSAL` | 4.12—4.14 | `30-E15` | no ADR/code/Part VII |

Coverage result: `15 / 15`。

## 6. Evidence-Card-to-Section Traceability

| Evidence Card | Status | Primary sections | Required visible limitation |
|---|---|---|---|
| `30-E01` pinned identity/integrity | `CONFIRMED` | 4.4, 4.9, 4.13 | identity does not prove behavior |
| `30-E02` real plugin + Loader unwrap | `CONFIRMED` | 4.4, 4.5, 4.9 | one representative owner path |
| `30-E03` Plugin versus model Context | `CONFIRMED` | 4.2, 4.3 | Article 32 owns wider assembly |
| `30-E04` configured/PENDING/ACTIVE | `CONFIRMED` | 4.1, 4.5, 4.6 | real provider not involved |
| `30-E05` missing agents PENDING | `CONFIRMED` | 4.6, 4.9 | higher boot audit is separate |
| `30-E06` ctx.on reversible Effect | `CONFIRMED` | 4.7, 4.8 | selected listener/effect only |
| `30-E07` Agent-scoped operate, non-Tool | `CONFIRMED` | 4.3, 4.7 | deterministic fixture, global listener |
| `30-E08` transient-to-durable boundary | `CONFIRMED` | 4.3, 4.5, 4.7 | external persistence not covered |
| `30-E09` post-dispose 1 -> 1 | `CONFIRMED` | 4.8, 4.11 | no rollback/cancellation inference |
| `30-E10` service contribution optional | `CONFIRMED` | 4.3, 4.6 | absence limited to time-context |
| `30-E11` global Fiber / Agent dispatch | `CONFIRMED` | 4.3, 4.7 | createScope wider runtime not tested |
| `30-E12` owner/full spec/headless mock | `CONFIRMED` | 4.9, 4.13 | not whole repo or real provider |
| `30-E13` composition/debug cost | `PROPOSAL` | 4.10 | course inference, not measured fact |
| `30-E14` ordinary DI rubric | `PROPOSAL` | 4.10 | future requirements may change choice |
| `30-E15` BuildPilot simplify | `PROPOSAL` | 4.12—4.14 | Part VII not authorized |

Coverage result: `15 / 15`。

## 7. Figures and Tables Plan

Draft should use compact Markdown/ASCII assets; no generated image is required.

1. configured/imported/PENDING/ACTIVE/operating/disposed state table。
2. 六对象抽象模型与 owner-bound disposer invariant。
3. Plugin Context / Model Context、Plugin Event / Session Event、Plugin / Service / Tool 防火墙表。
4. 46-step -> six-phase compression table，保留 `1—7 / 8—18 / 19—25 / 26—34 / 35—40 / 41—46`。
5. registration-owner 与 Agent-scoped dispatch 双泳道图。
6. transient Plugin Event -> decision -> durable Session Event handoff 图。
7. dispose `1 -> 1` 时间线。
8. owner tests / probes / headless mock / real provider evidence matrix。
9. configured / PENDING / ACTIVE negative interpretation table。
10. ordinary DI versus dynamic plugin kernel decision rubric。
11. BuildPilot `ADOPT / SIMPLIFY / REJECT / DEFER` table。
12. Claim and Evidence Card traceability tables。

## 8. Learning Check

1. 为什么配置 row 存在不能证明 Capability ACTIVE？
2. Cordis Plugin Context 与 model/request Context 分别拥有什么？
3. Plugin Event 怎样才成为 durable Session Event？
4. 为什么一个 plugin 不一定是 Service，更不一定是 Tool？
5. `inject=['agents']` 为什么不是 import order 或 constructor DI？
6. 缺 `agents` 的 `state=0/PENDING` 能证明什么，不能证明什么？
7. `ctx.on` 怎样变成 plugin Fiber-owned reversible Effect？
8. selected listener 为什么是 global，却仍执行 exact Agent-scoped operation？
9. `{ prepend: true }` 为什么不等于 contribution 先提交？
10. 46 步 source path 的六个 phase 分别改变什么 ownership boundary？
11. dispose 后 `1 -> 1` 为什么同时证明旧历史保留与未来贡献停止？
12. 为什么不能外推为 cancellation、rollback 或所有 effect 清理证明？
13. package spec `19/19` 为什么不代表 whole repository health？
14. Loader/headless mock E2E 为什么不能证明 real provider/token/cost？
15. 哪些条件下普通 DI + composition root 已经足够？
16. BuildPilot 应采纳哪些 invariant，又为什么默认 `SIMPLIFY`？

## 9. Practical Actions for Readers

1. 为自己的 extension 画一条 `configured -> dependency-ready -> register -> operate -> durable handoff -> dispose` 链，并给每条边写 owner。
2. 把“插件加载了吗”改成六个可观测问题：配置、导入、依赖、ACTIVE、operation、dispose。
3. 为每个 registration 保存 exact disposer，并让 composition/lifecycle owner 统一收集。
4. 写一个 post-dispose negative test：同一输入在 dispose 后不得产生新贡献，同时旧 durable history 不变。
5. 在采用动态 plugin kernel 前，用普通 DI 判断表逐项证明 runtime rebinding、multi-scope 或 ecosystem 需求真的存在。

## 10. Job Competency Coverage

Keep implicit; do not turn this into self-promotion.

| Competency | How the article demonstrates it |
|---|---|
| Architecture modeling | separates capability, dependency, owner, scope and durable handoff |
| Source investigation | closes 46 exact steps across Loader, Cordis, Agent and Session |
| Experimental rigor | pairs owner tests, bounded probes, negatives and fixture integrity |
| Lifecycle reasoning | distinguishes configured/PENDING/ACTIVE/operating/disposed |
| Reliability engineering | tests absence of future behavior while preserving history |
| Terminology discipline | keeps Context, Event, Plugin, Service and Tool boundaries explicit |
| Architecture restraint | defines when ordinary DI is sufficient and simplifies BuildPilot input |
| Evidence discipline | keeps mock runtime, package health and provider claims separate |

## 11. Draft Guardrails

- Do not start with packages, classes, APIs or YAML rows。
- Do not write “Everything is a Plugin” as literal ontology or universal DSH guarantee。
- Do not generalize from `time-context` to every plugin shape or cleanup path。
- Do not call configured row、imported namespace or created Fiber an ACTIVE capability。
- Do not say missing `agents` immediately throws；observed result is `state=0/PENDING`。
- Do not infer dependency semantics from import、manifest adjacency or YAML order。
- Do not call Plugin Context model context or token window。
- Do not call `agent/pre-step` a durable Session Event。
- Do not call `time-context` a Tool or Service provider。
- Do not say Agent-scoped operation means one plugin Fiber per Agent。
- Do not merge Fiber ownership、service isolation and event dispatch scope into one Scope。
- Do not say prepend commits first；listener delegates with `await next()`。
- Do not say all 46 source steps were individually runtime-instrumented。
- Do not turn provider replacement source path into runtime-confirmed claim。
- Do not say Session append proves external persistence backend flush。
- Do not interpret `1 -> 1` as history erase、rollback、callback cancellation or leak。
- Do not hide invalid config、downstream throw/cancel or harness-command failures。
- Do not call targeted/full package PASS whole-repository health。
- Do not call deterministic mock a real provider or infer network/model/token/cost。
- Do not state dynamic plugin cost as measured DSH fact；it is a course inference。
- Do not state ordinary DI or BuildPilot choice as source-confirmed fact。
- Do not pre-prove Article 31—37 or create links to unpublished pages。
- Do not start Article 38、BuildPilot implementation or Part VII。

## 12. Outline Gate Checklist

- [x] Article type fixed as `PRINCIPLE / SOURCE_TRACE / LIFECYCLE`。
- [x] TwoEgg progression: problem space -> abstract model -> implementation -> tradeoff。
- [x] Front matter includes `series_order: 310` and `weight: 3310`。
- [x] Previous/index navigation planned；Article 31 link deferred。
- [x] Opening starts from bad diagnostic question, not Cordis APIs。
- [x] Six-object model and owner-bound disposer invariant included。
- [x] Plugin Context / Model Context kept distinct。
- [x] Plugin Event / Session Event kept distinct。
- [x] Plugin / Service / Tool kept distinct。
- [x] One representative plugin selected without all-plugin traversal。
- [x] 46-step chain compressed into six phases with exact ranges。
- [x] configured / PENDING / ACTIVE boundaries explicit。
- [x] missing agents stays `state=0/PENDING`；no throw overclaim。
- [x] global listener and Agent-scoped operation remain separate axes。
- [x] downstream throw/cancel counter-evidence retained。
- [x] dispose `1 -> 1` means old history retained + future contribution stopped。
- [x] mock runtime is not real provider/network/token/cost evidence。
- [x] package PASS is not whole repository health。
- [x] ordinary DI sufficiency rubric included。
- [x] BuildPilot remains `SIMPLIFY` proposal；Part VII not started。
- [x] Article 31—37 only receive boundary routing。
- [x] Figures、hands-on verification、learning check and navigation included。
- [x] Claim coverage: `15 / 15`。
- [x] Evidence Card coverage: `15 / 15`。
- [x] No draft prose, review, published content or global-state edit introduced。
