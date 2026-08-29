# Article 30 Research

Status: `EVIDENCE MERGED / EVIDENCE_GATE CANDIDATE PASS`

## 1. Research boundary and merged inputs

本篇不把“Everything is a Plugin”解释成目录口号。它验证一个更窄、可证伪的生命周期命题：固定 revision 中，一个真实插件怎样从 Loader 配置进入 Cordis Registry/Fiber，等待依赖、注册由同一 owner 持有的 Event/Effect，在 Agent-scoped operation 中贡献 model-visible message，再由 Dispose 撤销未来贡献而不改写既有 Session history。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Retrieved / verified：`2026-08-30 / Asia/Shanghai`

本轮合并的 durable inputs：

- `repository-map.md`：代表插件、Core object、16 条 source edge、配置/激活边界与术语防火墙；
- `call-path.md`：从配置 row 到 Fiber dispose 的 46 步 exact source chain；
- `experiments/plugin-lifecycle-trace.md`：exact owner tests、bounded probes、真实 Loader/headless mock E2E、负例、命令失败与 fixture integrity。

Source Investigator 与 Lab 均确认 fixture 仍为 pinned commit 且 clean。Article 29 只作为 Host/Profile/Agent Run 前置地图；Article 30 的生命周期动态结论全部来自自己的 owner fixtures 与 probes，不借用 Article 29 的运行结果。

## 2. Article type and teaching problem

- Article Type：`PRINCIPLE / SOURCE_TRACE / LIFECYCLE`
- Problem Space：插件化的价值不在“模块数量”，而在 Capability 的可用条件、Contribution 的 owner 和可逆释放能否组成一条可观察链。若 configured、created、ACTIVE、operating、disposed 被混成一个“已加载”，诊断就会失真。
- Abstract Model：`configured -> imported -> Fiber created -> dependency-ready -> apply -> owned service/event/effect -> scoped operation -> durable handoff -> reverse disposal`。
- Concrete landing：真实插件 `packages/context/time-context`；它消费 `agents` Service，注册 `agent/pre-step` Plugin Event listener，经 Agent Loop 把 source-attributed `UserMessage` 追加成 Session Event，最终由 plugin Fiber dispose 撤销 listener。
- Scope ceiling：不遍历全部插件；不把 Cordis Context 写成 Model Context；不把 Plugin Event 写成 Session Event；不把 Plugin 写成 Tool；不把 repo-owned mock LLM 写成真实 Provider；不提前进入 Article 31—37 专题或 Part VII 实现。

## 3. Vocabulary firewall after source closure

| Term | Pinned object / observed boundary | Must not be conflated with |
|---|---|---|
| Plugin definition | Cordis 接受的 function、constructor 或 `{ apply }` object，带可选 metadata | package、目录、Tool |
| Plugin Runtime | Registry 按 callback identity 维护的共享 record | 一次安装实例 |
| Plugin Fiber | 一次 `ctx.plugin()` 产生的 dependency/config/effect/lifecycle owner | Agent、Session、线程 |
| Plugin Context | `parent.extend({ fiber })` 产生、传给 `apply` 的 Cordis Context proxy | request messages、prompt、token window |
| Service | `ctx.provide` / `Service` 注册到 resolver 的具名 capability | 任意 import 或 class |
| Plugin Event | Cordis process-local hook；本篇是 `agent/pre-step` waterfall | durable Session Event |
| Session Event | `Session.append` 的 JSON-safe durable vocabulary；本篇是 `user/message` | listener registration |
| Effect | 当前 Fiber 收集的 reversible disposer；`ctx.on` 和 `ctx.provide` 都落为 Effect | domain side effect、Tool result |
| Scope | Fiber/Context owner，加上 DSH `scopeTarget/createScope` 的路由机制 | lexical scope、Agent Session、service isolation label |
| Tool | 经 `ToolRuntime.register` 暴露给模型的 definition/execution capability | 任意 plugin |
| Dispose | owner Fiber 反向清理已收集 effect，停止未来行为 | Cancel、Session erase、external rollback |

`time-context` 不调用 `ctx.tools.register`，不是 Tool；也不在 `apply` 内直接 `Session.append`。它返回修改后的 `PreStepDecision`，后续 append 由 `ReactLoopAgent.turn` 持有。

## 4. Answered research questions

### 30-RQ01｜标题能支持多强？

固定源码支持“Plugin/Fiber 是 DSH 组合与可逆 contribution 的核心 ownership unit”，不支持“任何对象都是同一种插件”。`AgentRegistry` 是提供 Service 的 class plugin，`time-context` 是消费 Service 并注册 Event/Effect 的 namespace plugin，Tool 与 Session Event 另有 owner。标题应解释为组合原则，不是绝对本体论。

### 30-RQ02｜为什么选 `time-context`？

它在 production package 中导出 `name/inject/Config/apply`，真实 opt-in row 可经 Loader unwrap，依赖真实 `agents` Service，`apply` 注册 `agent/pre-step`，owner tests 覆盖 apply、operation、dispose、invalid config、downstream throw/cancel，另有真实 headless `cordis.yml` E2E。它足够小，又闭合本篇全部生命周期阶段。

### 30-RQ03｜configured 到 ACTIVE 怎样走？

Source Map 闭合：`Loader Entry -> import -> unwrapExports -> RegistryService.plugin -> Inject.resolve -> new Fiber(PENDING) -> agents provider -> epoch active -> Config resolve -> apply -> ctx.on effect -> ACTIVE`。配置 row 只证明 membership；缺依赖 probe 观测到 `inject=['agents'] / missing=['agents'] / state=0(PENDING)`，没有立即 throw，也没有运行 `apply`。

### 30-RQ04｜Service/Dependency 是什么？

`AgentRegistry extends Service` 经 `super(ctx, 'agents') -> reflect.provide` 注册具名 capability；`time-context.inject=['agents']` 是 runtime availability constraint，不是 import 或 YAML 顺序。Provider uid 改变会让 consumer Fiber unload/reload。Lab 只实测 missing dependency 的 PENDING 边界；provider replacement 仍保持 source-confirmed，不扩张成 runtime claim。

### 30-RQ05｜`ctx.on` 为什么也是 Effect？

Exact path 为 `timeContext.apply -> ctx.on -> EventsService.on/register -> ctx.fiber.effect -> hook insert + unregister disposer`。`ctx.on` 的返回 disposer 可手工调用，同时结构 owner Fiber 会在 unload 时调用它。Dispose targeted test `1 passed / 18 skipped`，bounded probe 观测 `beforeDispose=1 / afterDispose=1`。

### 30-RQ06｜operate in scope 的精确含义是什么？

`ReactLoopAgent.preStep -> agentEvents.waterfall -> scopeTarget(agent, agent) -> Cordis filter -> global time-context listener`。operation 由 exact Agent carrier 限定，但所选 plugin Fiber 不是“一 Agent 一实例”；它是 global listener，读取 payload 中的 exact Agent。listener 先 `await next()`，只有 accepted 且未 abort 才返回 source-attributed snapshot message。

### 30-RQ07｜Plugin Event 与 Session Event 怎样分开？

`agent/pre-step` 是 process-local Cordis waterfall；listener 返回的 `UserMessage` 仍只是 proposed model input；Agent Loop 随后先 append `step/start`，再 append `user/message`，Session 才验证、冻结、push。真实 Loader/headless mock E2E 持久化了两 turn、两 step、两条 ordered plugin contribution，但这不把 Cordis hook 自身变成 durable event。

### 30-RQ08｜Dispose 撤销什么？

`fiber.dispose -> epoch inactive -> _unload -> reverse effect disposers -> EventsService.unregister`。显式 probe 中第一次 eligible pre-step 写入一条 contribution；dispose 后第二次 eligible pre-step 没有增加，计数保持 `1 -> 1`。这同时说明旧的那一条仍在 Session 中，Dispose 停止未来 listener contribution，不删除历史，也不回滚外部世界。

### 30-RQ09｜无效配置和 downstream failure 怎样表现？

invalid timezone/unavailable process zone 与 unsafe refresh intervals 的 targeted owner tests 都 exit `0` 并通过预期 rejection；downstream throw/cancel 参数化 case 为 `2 passed / 17 skipped`，观测零 time-context reading、零 adapter request、零 `step/start`。prepend 只表示 hook position；因为 listener 先 `await next()`，downstream failure 不留下 phantom contribution。

### 30-RQ10｜真实 Loader/headless 运行证明了什么？

校正 collector 后 E2E exit `0 / 1 passed`：真实 app boot/Loader composition 使用 repo-owned deterministic mock LLM，完成两 turns、两 steps，JSONL 中恰有两条 plugin-attributed `user/message`，每条在对应 `step/start` 后，`surfaceOp=append`，header 不含注入正文。它是 `REAL_HEADLESS_MOCK_RUNTIME_CONFIRMED`，不是 real provider、production deployment、token/cost evidence。

### 30-RQ11｜生命周期证据是否只靠 targeted cherry-pick？

不是。除 targeted owner cases 外，完整 `time-context.spec.ts` 为 `19 passed / 19`。它说明 dispose、operation、Loader export、invalid config、abort、interval、browser zone、resume/shadowed history 等 package owner assertions 在同一环境共存；它不等于 whole-repository suite health。

### 30-RQ12｜普通 DI 何时足够，BuildPilot 为什么 `SIMPLIFY`？

动态 named injection、proxy/mixin、waterfall 与 provider epoch 提供强组合力，也带来 configured/active、global/scoped、prepend/delegate-first 等诊断维度。当 capability 稳定、不需 runtime rebinding/HMR、多 scope isolation 或 plugin ecosystem 时，constructor DI + composition root + explicit contributor/disposer 足够。BuildPilot 吸收 ownership invariant，不复制 Cordis machinery。

## 5. Final Claim register

| Claim ID | Falsifiable Claim | Final Status | Evidence | Wording ceiling |
|---|---|---|---|---|
| `30-C01` | 本篇所有 DSH 事实绑定 official repo 的 frozen tag/full revision，source 与 Lab 结束时 fixture clean。 | `CONFIRMED` | `30-E01` | 版本身份不证明机制 |
| `30-C02` | `time-context` 是真实 plugin，Loader metadata 与 `apply/ctx.on` 路径存在且 owner Loader test 通过。 | `CONFIRMED` | `30-E02` | 不推广到全部 plugin |
| `30-C03` | Plugin Context 是 Cordis lifecycle container，不等于 model/request Context。 | `CONFIRMED` | `30-E03` | Article 32 才覆盖完整 assembly |
| `30-C04` | configured row 经 Loader/Registry/Fiber/dependency/config 到 `apply`，且 ACTIVE 与 configured/PENDING 可区分。 | `CONFIRMED` | `30-E04` | real Provider 未运行 |
| `30-C05` | `inject=['agents']` 是 runtime service dependency；缺失时 observed state 为 `0/PENDING`，非立即 throw。 | `CONFIRMED` | `30-E05` | replacement path 仅 source-confirmed |
| `30-C06` | `ctx.on` listener 是 plugin Fiber-owned reversible Effect。 | `CONFIRMED` | `30-E06` | 私有 hook table 未 introspect |
| `30-C07` | 代表插件经 Agent-scoped pre-step operation 产生 ordered source-attributed message，不注册 Tool。 | `CONFIRMED` | `30-E07` | global listener，不是一 Agent 一 Fiber |
| `30-C08` | Plugin Event、PreStepDecision 与 durable Session Event 是三个连续但不同的边界。 | `CONFIRMED` | `30-E08` | persistence/replay 专题不在本篇 |
| `30-C09` | Dispose 后 contribution count `1 -> 1`：既有历史保留，未来 listener contribution 停止。 | `CONFIRMED` | `30-E09` | 不等于 cancellation/rollback |
| `30-C10` | Service 可由 owner Fiber 提供；`time-context` 只消费 `agents` 并贡献 Event/Effect，不提供 Service/Tool。 | `CONFIRMED` | `30-E10` | absence 只限代表模块 |
| `30-C11` | Fiber ownership、Context isolation、event scope routing 是不同但相关机制；selected listener global、operation Agent-scoped。 | `CONFIRMED` | `30-E11` | scope runtime 仅闭合所选 dispatch |
| `30-C12` | exact lifecycle/operation/Loader/negative/full-spec tests 与 real headless mock E2E 在当前环境通过。 | `CONFIRMED` | `30-E12` | package owner health，不是全仓健康 |
| `30-C13` | 动态 inject、proxy/mixin、waterfall ordering 与 reverse cleanup 提供组合力，也增加 order/debug 成本。 | `PROPOSAL` | `30-E13` | 成本是课程 inference，非官方动机 |
| `30-C14` | capability 稳定、无需 runtime rebinding 且 owner 显式时，普通 DI + explicit lifecycle 足够。 | `PROPOSAL` | `30-E14` | 工程选择，不是 DSH fact |
| `30-C15` | BuildPilot 默认 `SIMPLIFY`：采纳 explicit dependency、owner-bound disposer 与 lifecycle tests，不照搬 Everything-is-a-plugin。 | `PROPOSAL` | `30-E15` | Part VII 未授权 |

最终统计：`15 Claims = 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。

## 6. Merged lifecycle map

| Phase | Source closure | Runtime observation | Final ceiling |
|---|---|---|---|
| Configure/import | Loader Entry、namespace unwrap、metadata | Loader export owner test PASS | `SOURCE + TEST_FIXTURE CONFIRMED` |
| Dependency/activate | Registry/Fiber epoch、AgentRegistry provides `agents` | missing dependency prints `state=0/PENDING`; no immediate throw | `SOURCE + BOUNDED NEGATIVE RUNTIME` |
| Apply/register | config resolve -> `apply` -> `ctx.on` -> Fiber effect | active listener owner test PASS | `SOURCE + TEST_FIXTURE CONFIRMED` |
| Operate | Agent carrier -> waterfall -> sourced message -> Session append | AgentLoop targeted PASS; two requests/two contributions | `SOURCE + OWNER TEST CONFIRMED` |
| Loader/headless | app boot/Loader -> two turns -> JSONL | corrected E2E PASS; two turns/two steps/two ordered contributions | `REAL HEADLESS MOCK RUNTIME CONFIRMED` |
| Dispose | Fiber unload -> reverse effects -> unregister | targeted PASS; explicit `before=1/after=1` | `SOURCE + EFFECT OBSERVATION CONFIRMED` |
| Invalid config | config validation before ACTIVE | invalid zone/interval owner cases PASS | `TEST_FIXTURE CONFIRMED` |
| Downstream fail/cancel | prepend listener delegates first | two cases PASS; no reading/request/step | `TEST_FIXTURE CONFIRMED` |
| Real provider | no source/lab request in Article30 | not invoked | `NOT TESTED` |

## 7. Counter-evidence and retained failures

1. **Configured != ACTIVE**：缺 `agents` 的 raw Cordis mount 没有抛错，而是 `state=0/PENDING`；更高 app-boot audit 才可能把 unresolved entry 报错。
2. **Prepend != commit first**：listener 先进入 waterfall，但 `await next()`；downstream throw/cancel 时没有 phantom reading。
3. **Dispose != erase**：`before=1/after=1` 同时证明旧 contribution 仍在、第二次没有新增。
4. **Plugin Context != Model Context**：Cordis Context 承担 DI/effect owner；model-visible content 是 sourced `UserMessage`。
5. **Plugin Event != Session Event**：`agent/pre-step` process-local；`Session.append('user/message')` 是后续 durable boundary。
6. **Plugin != Tool**：`time-context` 没有 `ToolRuntime.register/ctx.tools.register`。
7. **Agent-scoped operation != per-Agent plugin instance**：selected listener 未 tagged，是 global hook；carrier 传入 exact Agent。
8. **Mock != Provider**：E2E 的 LLM 是 deterministic repo-owned mock；没有 credential、network、token、cost 或 model-quality evidence。
9. **Targeted PASS != repository health**：完整 package spec 为 19/19，但不能替代 full monorepo suite。
10. **Corepack command failure retained**：`corepack pnpm --version` 从 course cwd 触发 registry fetch，因 `EACCES` exit `1`；实验随后直接用 fixture-local Vitest，没有安装或网络重试。
11. **Wrong collector retained**：首次运行 `*.e2e.ts` 用 default config，exit `1 / No test files found`；改用 repo-owned `vitest.e2e.config.ts` 后才得到有效 PASS。
12. **First tsx probe retained**：top-level await 在 CJS output 下 transform fail；只包 async IIFE 后重跑，hypothesis/input/expected counts 未变。

后三项是 harness-command errors，不是 product failures；保留它们是 reproducibility 的一部分。

## 8. BuildPilot decision frame

| Mechanism | Final course decision | Reason |
|---|---|---|
| Explicit capability dependency | `ADOPT` | configured 与 ready 必须可区分，依赖缺失要可观测 |
| Contribution/disposer share owner | `ADOPT` | source chain 与 `1 -> 1` effect observation 已闭合 |
| Negative-after-dispose lifecycle test | `ADOPT` | 能证明未来行为停止，而不是只检查 disposer 存在 |
| Transient hook vs durable record boundary | `ADOPT` | Plugin Event 与 Session Event 已由 owner/call path 分开 |
| Proxy Context + string-key dynamic DI | `SIMPLIFY` | BuildPilot 未证明需要 runtime rebinding/isolation/HMR |
| Implicit waterfall/config ordering | `REJECT` as default | 优先显式 contributor order 与 diagnostics |
| Multi-host/plugin ecosystem | `DEFER` | Part VI 不决定 Part VII 架构 |

普通 DI 足够的判断标准：依赖在 composition root 固定；实现不需运行期替换；组件拥有少量明确 disposer；启动/停止可由一个显式 owner 编排；不需要多维 isolation/filter。只有真实需求打破这些前提时，才评估更强 plugin kernel。

## 9. Evidence Gate recommendation

`EVIDENCE_MERGE PASS / EVIDENCE_GATE ELIGIBLE`。

Source Map 与 46 步 Call Path 已闭合 `install -> dependency/service -> event/effect -> Agent-scoped operate -> durable handoff -> dispose`。Lab 同时给出 exact owner lifecycle PASS、显式 `1 -> 1` dispose probe、AgentLoop two-request PASS、real Loader/headless mock two-turn E2E PASS、invalid config、missing dependency PENDING、downstream throw/cancel 负例以及 full package spec `19/19`。失败的 Corepack、collector 和首版 tsx 命令均原样保留。中心命题已在“pinned source + test fixture/runtime”范围内闭合，real Provider/production claims 明确排除，BuildPilot 仍为 `SIMPLIFY` proposal。Next allowed gate：`EVIDENCE_GATE`。
