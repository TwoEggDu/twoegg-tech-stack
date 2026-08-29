# Everything is a Plugin：插件内核如何承载 Capability 与生命周期

> **上一篇**：[DeepSeek Harness 总图：从 Host 启动到一次 Agent Run]({{< relref "ai-empowerment/agent-engineering-29-dsh-host-to-agent-run.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

配置文件里已经有一行插件，启动过程也没有立刻报错，这个 Capability 就能用了吗？

很多插件系统最后都会把这个问题压成一句：“插件加载了吗？”这个问法太粗。一个插件可以已经出现在配置里，却还没有 import；可以已经创建运行对象，却因为缺依赖停在 PENDING；可以已经 ACTIVE，却没有被当前 operation 的 scope 选中；也可以已经 dispose，但过去写入 Session 的历史仍然存在。

如果这些状态都叫“已加载”，出了问题就只能靠猜。你不知道该查配置、依赖、注册、事件路由、持久化边界，还是清理逻辑。

“Everything is a Plugin”真正值得研究的地方，也不是项目里有多少插件目录，而是另一件事：一个 Capability 的依赖、贡献和清理，能否被同一个生命周期 owner 组织起来；当 owner 被释放时，未来行为能否停止，同时不篡改已经发生的事实。

本文选择 DeepSeek Harness（下称 DSH）中的真实插件 `time-context`，沿固定源码闭合这样一条链：

```text
configured
-> imported / owner created
-> dependency-ready
-> apply
-> owned contribution registered
-> scoped operation
-> durable handoff
-> reverse disposal
```

运行结果里有两个很适合先记住的反直觉事实：

- 缺少 `agents` 依赖时，插件没有立即 throw，而是 `state=0/PENDING`。
- dispose 前后 contribution count 是 `1 -> 1`，不是 `1 -> 0`：旧历史保留，未来贡献停止。

如果这篇只记一句话，我建议记这个：

> 插件内核的价值，不在于把所有东西都叫 Plugin，而在于让 contribution 的注册、作用范围和反向清理拥有可观察的生命周期 owner。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 是 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。实验前后外部 fixture 的 `HEAD` 都等于该 SHA，working tree 为空。这个 identity check 只固定研究对象，不替任何机制或运行结果背书。

本文证据账为 `15 / 15 Claims`、`15 / 15 Evidence Cards`：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。真实 Loader/headless composition 使用的是仓库自带 deterministic mock LLM；没有运行真实 Provider，也没有 credential、network、真实 token 或 cost 证据。

## 1. “插件已加载”为什么是一个坏诊断问题

先把 loaded/not-loaded 这个二元开关拆开。

| 状态 | 当前真正知道了什么 | 仍然不能推出什么 |
|---|---|---|
| Configured | Loader row 指向一个模块 | import、依赖、apply、operation |
| Imported / Fiber created | callback identity 与生命周期 owner 已存在 | dependency-ready、ACTIVE |
| PENDING | 缺失的 runtime service 可被观察 | Capability 可用 |
| ACTIVE | config、apply 与 effect setup 已完成 | 某次 operation 一定命中 |
| Operating | 某个 scoped event 到达 listener | 结果已经持久化 |
| Disposed | owner 收集的 effect 已反向清理 | 旧历史会被删除 |

这六种状态回答的是六个不同问题。

配置属于 composition；import 和 Fiber creation 建立候选 owner；dependency-ready 决定 apply 能否开始；ACTIVE 表示 setup 已经结算；operation 还要经过 subject/scope 与 event order；durable handoff 则由另一个 owner 把临时结果写成历史。最后，dispose 只应该撤销 owner 的未来贡献，不应该偷偷重写已经发生的记录。

因此，下面这些等号在本文里都不成立：

```text
configured row != ACTIVE capability
Plugin Context != model/request context
Plugin Event != durable Session Event
Plugin != Service != Tool
Agent-scoped operation != one plugin Fiber per Agent
prepend listener != contribution committed first
dispose != history erase / cancellation / external rollback
mock runtime != real provider runtime
```

## 2. 先建立一个不依赖 Cordis 的最小模型

在看 DSH 类名之前，可以先把插件生命周期压成六个对象。

| 抽象对象 | 它负责什么 | 排障时要问什么 |
|---|---|---|
| Capability | consumer 真正可调用或可观察的能力 | 名字存在，还是行为 ready？ |
| Dependency | Capability 可用前必须成立的条件 | 缺失时 fail-fast、PENDING，还是降级？ |
| Contribution | service、listener、effect、tool 等具体贡献 | 谁注册、对谁生效？ |
| Owner | 收集 contribution 与 disposer 的生命周期单元 | teardown 能否找到所有反向动作？ |
| Scope / Subject | 决定某次 operation 对谁、哪些 listener 生效 | owner scope 与 dispatch subject 是否混了？ |
| Durable Handoff | 把瞬时结果转成历史事实的边界 | live hook 结果真的 append 了吗？ |

这里最关键的 invariant 很短：

```text
register(contribution) 与 dispose(contribution) 必须共享一个 owner
```

如果注册发生在一个地方，清理却依赖另一个模块“记得”来做，插件系统很快会出现幽灵 listener、重复订阅和无法解释的重载状态。反过来，如果 owner 能收集每个 reversible effect，teardown 就可以被结构化地等待和验证。

这套模型是课程为了分析生命周期提出的抽象，不是 DSH 官方 taxonomy。它也不要求六个对象都成为独立类、包或进程。抽象的作用，是先把责任切开，再看具体框架怎样落地。

## 3. 三组必须先拆开的术语

### 3.1 Plugin Context 不是 Model Context

DSH 这条链里的 Plugin Context，是 Cordis `Context` proxy。它关联当前 Fiber，承担 service lookup、event registration 与 effect ownership。源码 owner 在 [`vendor/cordis/src/context.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/vendor/cordis/src/context.ts) 和 [`fiber.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/vendor/cordis/src/fiber.ts)。

Model Context 则是最终进入模型请求的 system、user、assistant、tool 等材料。`time-context` 不会“修改 Cordis Context 让模型看到时间”。它创建一条带 source attribution 的 `UserMessage`，先放进 `PreStepDecision.messages`，后续才由 Agent Loop append 到 Session，并进入模型历史投影。

同一个 `Context` 单词，背后是两个完全不同的 owner 和数据边界。

### 3.2 Plugin Event 不是 Session Event

`agent/pre-step` 是进程内 Cordis waterfall。listener 被调用，只说明一次 live interception 发生了；它本身不是 durable record。

listener 返回的 message 也仍然只是 proposed model input。只有 [`ReactLoopAgent.turn`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/agent.ts) 接受 decision，并调用 `Session.append('user/message', ...)`，这条内容才跨过 durable boundary。

```text
Plugin Event：agent/pre-step（process-local）
  -> PreStepDecision.messages（proposed input）
  -> Session Event：user/message（durable vocabulary）
  -> model request history projection
```

### 3.3 Plugin 不等于 Service，更不等于 Tool

Plugin 是 extension 与 lifecycle ownership 单元；Service 是按名字注册、可被 consumer inject 的 capability；Tool 则通过单独的 Tool runtime 暴露给模型并执行。

`time-context` 的具体形状很能说明这一区别：

- 它消费 `agents` Service，不提供新的 Service。
- 它调用 `ctx.on('agent/pre-step', ...)`，贡献一个 Event/Effect。
- 它没有调用 `ctx.tools.register` 或 `ToolRuntime.register`，因此不是 Tool。
- 它不在 `apply` 里直接 append Session Event。

所以，“Everything is a Plugin”在这里应理解为组合与生命周期原则，而不是“所有 contribution 都是同一种东西”。

## 4. 为什么选择 `time-context`

本文不遍历所有插件，只选 [`packages/context/time-context`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/context/time-context) 作为代表。

这个样本足够小，却能闭合整条文章主线：

1. shipped composition 中有真实 opt-in row；
2. namespace 导出 `name`、`inject`、`Config` 与 `apply`，可以经过 Loader unwrap；
3. 它依赖真实 `agents` Service；
4. `apply` 注册真实 `agent/pre-step` listener；
5. `ctx.on` 把 listener 降为当前 Fiber 拥有的 effect；
6. Agent carrier 把 exact Agent 带入每次 operation；
7. contribution 先进入 decision，再跨到 Session Event；
8. owner test 和 bounded probe 都覆盖 dispose 后不再新增；
9. real Loader/headless fixture 覆盖两 turn、两 step、两条 ordered contribution。

这只证明所选插件和所选路径。它不证明所有 DSH plugin 都是 listener consumer，也不证明所有 effect 都有完全相同的 dispose 语义。

## 5. `time-context` 的 46 步生命周期

固定源码闭合了从配置 row 到旧历史保留的 `46` 个步骤。正文不逐行抄 source register，而是压成六段；每段都保留 exact step range，方便回到源码复查。

| 阶段 | Source steps | 压缩后的链 | 必须保留的边界 |
|---|---:|---|---|
| A. Configure / Import | `1—7` | row -> Entry -> disabled gate -> import -> unwrap -> Registry start/await | row 不等于 activation |
| B. Dependency / Activate | `8—18` | plugin shape -> inject agents -> Fiber -> provider -> epoch -> config -> apply -> ACTIVE | missing agents 时 PENDING |
| C. Register / Own | `19—25` | validate/setup -> `ctx.on` -> Events register -> Fiber effect -> hook + disposer | listener 有结构 owner |
| D. Scoped Operate | `26—34` | preStep -> Agent carrier -> filter -> waterfall -> `await next()` -> sourced message | global listener 执行 exact Agent operation |
| E. Durable Handoff | `35—40` | decision -> step/start -> user/message -> validate/freeze/push -> model projection | Plugin Event 不等于 Session Event |
| F. Dispose / Retain | `41—46` | dispose -> unload -> reverse effects -> unregister -> no future contribution -> old event retained | unregister 不等于历史删除 |

### 5.1 Configure / Import：steps 1—7

shipped Schedule example 的 patch row 指向 `@deepseek-ai/dsh-time-context`。Loader 为 row 创建 `Entry`，先判断 own/ancestor disabled state，再 import module namespace，通过 `unwrapExports` 规范化 export，最后调用 `ctx.registry.plugin(plugin, config)` 并等待 Fiber settlement。

这段只能把“配置候选”送到 Registry。disabled entry 甚至没有 Fiber；import 成功也只说明 namespace 可解析。owner Loader test 确认 `time-context` 没有 synthetic default export，unwrap 后仍保留 `name / inject / Config / apply`，并且在依赖满足时可以实际挂载 listener。

### 5.2 Dependency / Activate：steps 8—18

Registry 接受 function、class 或 `{ apply }` object，把 `inject = ['agents']` 规范化为 runtime required-name map，然后创建一个初始为 PENDING 的 Fiber。这个 Fiber 的 derived Context 会传给 `apply`，而它自己又由 parent Fiber 的 effect 持有。

依赖的 provider 是 [`AgentRegistry`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent/src/index.ts)。它作为 class plugin 继承 `Service`，通过 `super(ctx, 'agents')` 最终把实例注册到 Reflect store；provider registration 也是 provider Fiber 的 reversible effect。

consumer Fiber 找到 ACTIVE 且 isolation 匹配的 `agents` implementation 后，以 provider Fiber uid 形成 epoch，随后 resolve `Config`、调用 `time-context.apply(ctx, config)`，成功结算后才进入 ACTIVE。

因此，`inject=['agents']` 不是 TypeScript import，也不是 YAML list order。它是运行期 capability availability constraint。

### 5.3 Register / Own：steps 19—25

[`time-context/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/context/time-context/src/index.ts) 的 `apply` 先验证 refresh interval 和 timezone，再创建 formatter，最后调用：

```text
ctx.on('agent/pre-step', listener, { prepend: true })
```

`ctx.on` 不是把 callback 丢进一个与 lifecycle 无关的全局数组。它进入 [`EventsService.register`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/vendor/cordis/src/events.ts)，再通过 `ctx.fiber.effect` 插入 hook，并返回 exact unregister disposer。Fiber 同时收集 wrapper，保证 unload 时可以反向调用。

也就是说，register 与 dispose 从一开始就共享同一个 plugin Fiber owner。

### 5.4 Scoped Operate：steps 26—34

[`ReactLoopAgent.preStep`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/agent.ts) 调用 per-Agent dispatcher。dispatcher 通过 `scopeTarget(agent, agent)` 把 exact Agent 同时放进 payload 和 scope carrier，再进入 Cordis waterfall。

这里有一个容易写反的细节：`time-context` listener 是全局注册的 untagged hook，不是每个 Agent 一个 Fiber。Agent-scoped 的是这次 operation；全局 listener 读取 payload 中的 exact Agent 并完成工作。Fiber ownership、service isolation 与 dispatch subject 是相关但不同的三条轴。

listener 虽然带 `{ prepend: true }`，却先执行 `await next()`。只有 downstream 接受且没有 abort，它才读取 Agent Session/browser/time state，创建 source 为 `plugin/time-context` 的 snapshot `UserMessage`，并返回扩展后的 decision。

所以 prepend 只改变 hook position，不代表 contribution 已经先提交。

### 5.5 Durable Handoff：steps 35—40

accepted decision 回到 Agent Loop 后，`turn` 先 append `step/start`，再逐条 append decision messages，其中包括 `time-context` 的 sourced `user/message`。

[`Session.append`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/index.ts) 会先做 JSON snapshot、validation 和 freeze，再 push 到 Session log，之后才通知 live `session/event` observers。

这一步完成了 owner 迁移：Plugin Event owner 负责产生 proposal；Agent Loop 决定接受；Session owner 负责 durable vocabulary。外部 persistence provider 是否 flush bytes，属于另一层，本篇不从一次 append 自动推出。

### 5.6 Dispose / Retain：steps 41—46

显式 `fiber.dispose()` 或 parent teardown 会让 plugin Fiber 的 epoch 失活，进入 `_unload()`；Fiber 反向 drain 已收集的 effect，listener disposer 最终调用 `EventsService.unregister`，从 hook list 移除 exact callback。

之后同样的 `agent/pre-step` operation 不再调用这个 listener。但此前已经 append 的 `user/message` 属于 Session 历史，不受 unregister 影响。

完整链压到一行就是：

```text
Loader row -> import/unwrap -> Fiber(PENDING) -> agents ready
-> config/apply -> ctx.on -> Fiber-owned effect
-> Agent-scoped pre-step -> sourced message proposal
-> Session.append(user/message)
-> fiber.dispose -> reverse effect -> unregister
```

这 `46` 步是 pinned static closure，不是 46 个 runtime probe。provider loss/replacement 会引发 consumer unload/reload 的 side path 在源码中成立，但本轮没有执行 replacement runtime，因此不把它写成运行确认。

## 6. 三组反证，比 happy path 更能说明边界

### 6.1 Configured、PENDING、ACTIVE 必须分开

Lab 在一个 fresh Cordis `Context` 中挂载真实 `time-context`，但故意不挂 `AgentRegistry`。进程没有立即 throw，输出是：

```json
{"inject":["agents"],"missing":["agents"],"state":0}
```

固定源码把 state `0` 映射为 `PENDING`。因此，这个 observation 允许我们说：consumer composition 合法，但缺少 required `agents` service，Fiber 被 parked，Capability 没有 ACTIVE。

它不允许说“没有报错，所以插件运行正常”，也不允许说“缺依赖一定立即失败”。更高层的 app-boot 可以在 Loader tree settled 后审计 unresolved entry，并把它报告成 composition error；那是更高 owner 的策略，不应反向改写 raw Cordis mount 的事实。

| 容易做出的推断 | 实际证据 | 准确说法 |
|---|---|---|
| 配置里有 row，所以已 ACTIVE | missing service 时 state 0 | 只有 configured candidate |
| 没 throw，所以可用 | `missing=['agents']` | 合法 composition，但 parked PENDING |
| package/YAML 顺序就是依赖 | resolver 查 active named provider | 依赖是 runtime lifecycle relation |
| provider 变化只是换指针 | source 使用 provider uid epoch | source-confirmed unload/reload；本轮未运行 replacement |

### 6.2 Prepend 不等于先提交

owner test 安装了一个 downstream pre-step listener，让它分别 throw 和 cancel。两个参数化 case 都通过：零 time-context reading、零 adapter request、零 `step/start`。

这正好验证 `await next()` 的意义。`time-context` 虽在 hook list 中 prepend，但它先把控制权交给 downstream；只有 downstream 成功返回，才采样并提出 contribution。否则不会留下 phantom reading。

### 6.3 Invalid config 不是“部分激活”

invalid explicit timezone、unavailable process zone，以及 `-1`、`0.5`、超出 safe integer、正无穷、`NaN` 等 refresh interval 都由 unchanged owner tests 验证为 load-time rejection。

这些 case 说明 config validation 是 ACTIVE 前的 gate。出现 rejection 时，不能写成“插件已经部分运行，只是 formatter 不工作”。

## 7. Dispose 的中心证据：`1 -> 1`

源码里存在 disposer，不等于 disposer 真的撤销了 representative contribution。更强的验证，是在 dispose 前后送入相同类型的 eligible input，只改变 owner lifecycle。

owner test 的过程是：

1. 创建 fresh Cordis Context；
2. 挂载 `AgentRegistry`；
3. 挂载真实 `time-context` 并保留返回的 plugin Fiber；
4. 在一个 Session/Agent 上触发第一次 eligible `agent/pre-step`；
5. `await fiber.dispose()`；
6. 在同一 Context/Agent 上触发第二次 eligible pre-step；
7. 断言 Session 中仍然只有一条 time-context contribution。

exact owner case 原样运行结果为 `exit 0 / 1 passed / 18 skipped`。为了让前后计数直接可见，Lab 又用同样的公开 package entry point 做了 bounded probe：

```text
beforeDispose = 1
afterDispose  = 1
firstSource.kind = plugin
firstSource.plugin = time-context
firstSource.form = snapshot
```

这个 `1 -> 1` 同时证明两件事。

第一，第一次 contribution 仍然可以从 Session 里数到，因此旧历史没有被 dispose 删除。

第二，第二次 otherwise-eligible event 没有增加计数，因此 listener 的未来贡献已经停止。

```text
t0 register listener
t1 pre-step -> append history[0]
t2 dispose -> unregister listener
t3 pre-step -> no new contribution

history count: 1 -----------------> 1
future behavior: enabled ---------> disabled
```

这项证据通过“第二次效果缺席”观察 cleanup，没有读取 Cordis 私有 hook table。它也没有证明：正在执行的 callback 会被取消、外部系统会回滚、进程资源全部清理，或所有 DSH plugin effect 都遵循完全相同的细节。

Dispose 的准确含义是：撤销这个 owner 持有的 registration mechanism，停止未来行为；不是改写已经发生的 durable facts。

## 8. 运行证据：哪些 PASS，哪些仍然没有发生

本轮把 source closure、owner fixture、bounded probe 与 Loader composition 分开记账。

| 验证 | 结果 | 能证明什么 | 不能证明什么 |
|---|---|---|---|
| dispose owner test | `exit 0 / 1 passed / 18 skipped` | representative listener 停止未来贡献 | 私有 hook state、任意 effect |
| explicit count probe | `1 -> 1` | 旧历史保留、没有第二条 contribution | cancellation、external rollback |
| AgentLoop owner fixture | `exit 0`，two requests/two ordered contributions | operation order 与 source attribution | production Provider/model |
| Loader unwrap owner test | `exit 0` | namespace metadata 与依赖满足后的 apply | 所有 Loader 路径 |
| invalid config cases | expected rejection PASS | fail-loud config gate | partial activation |
| missing agents probe | `state=0/PENDING` | inactive consumer 显示缺失依赖 | higher boot error policy |
| downstream throw/cancel | `2 passed`，no reading/request/step | listener delegate-first | 所有 waterfall listener |
| complete package spec | `19/19 passed` | 这些 owner assertions 在同一环境共存 | whole-repository health |
| Loader/headless E2E | `exit 0 / 1 passed`，two turns/two persisted contributions | 真实 composition + deterministic mock | real Provider/network/token/cost |

AgentLoop owner fixture 使用 deterministic in-process `ScriptedAdapter` 和 fixture Tool，观察到两次 request、两次 `step/start`，每次恰有一条带 `plugin/time-context/snapshot` attribution 的 contribution。每条 contribution 位于对应 `step/start` 后，`surfaceOp` 是 `append`；第二步能看到累积 reading，第一步看不到未来 reading；system text 和 durable `request/header` 都不含注入正文。

real Loader/headless E2E 则通过真实 `cordis.yml` 进入 app boot 和 Loader composition，使用仓库自带 deterministic mock LLM，完成两 turn、两 step，并在 JSONL 中看到两条 ordered plugin contribution。这个证据可以标为：

```text
TEST_FIXTURE_RUNTIME_CONFIRMED
+ REAL_HEADLESS_MOCK_RUNTIME_CONFIRMED
```

它不能改写成 `REAL_PROVIDER_RUNTIME_CONFIRMED`。本轮没有 provider credential、网络请求、真实模型输出、真实 token accounting 或 cost。

### 8.1 失败命令也要留在证据里

实验中有三次失败命令，它们不是 product failure，但必须保留：

1. 在课程仓库 cwd 执行 `corepack pnpm --version` 时，Corepack 尝试访问 npm registry，因 `EACCES` 以 `exit 1` 结束。实验没有做网络重试，后续直接调用 fixture-local Vitest。
2. 第一次直接运行 `time-context.e2e.ts` 使用了默认 collector，得到 `No test files found / exit 1`。仓库的 e2e 脚本明确使用 `vitest.e2e.config.ts`；校正为这个 repo-owned collector 后，才得到有效的 `1 passed`。
3. 第一次 `tsx -e` probe 因 CommonJS output 不支持 top-level await 而 transform fail。重跑只把相同 body 包进 async IIFE，没有改变 hypothesis、fixture、plugin 或 expected count。

保留这些记录有两个价值：一是区分 harness-command error 与 product behavior；二是防止读者只看到最终 PASS，却无法复现为什么命令形状发生了变化。

同样要保留证据上限：package spec `19/19` 只能说明 `time-context` owner assertions 在当前环境通过，不代表此前已经发现失败的全仓测试突然变成健康，也不代表 production readiness。

## 9. 组合力为什么会带来诊断成本

Cordis 这套机制提供了真实的组合力：named service 可以 late availability；provider uid 变化可以驱动 consumer unload/reload；Context proxy 和 mixin 提供统一 extension surface；waterfall 允许多个 listener 组合；Fiber effect 能反向清理 registration。

但这些能力也增加了独立诊断维度。

| 机制 | 提供的组合力 | 新增的诊断问题 |
|---|---|---|
| string-key dynamic inject | late availability、runtime rebinding | 名字、provider、isolation、PENDING |
| proxy/mixin Context | 统一 extension surface | 属性 owner、访问路径、隐式依赖 |
| waterfall + prepend | 可组合 interception | order、delegate-first、veto |
| scope filter | 同一事件面路由不同 subject | owner scope、DI isolation、dispatch key |
| Fiber-owned reverse effects | structured teardown | helper 是否正确保留 owner 语义 |

这不是说动态插件内核“太复杂，所以不该用”。准确判断是：只有真实需求需要这些维度时，组合力才值得其成本。缺 `agents` 的 PENDING 和 prepend listener 的 delegate-first，恰好说明一个成熟 plugin kernel 要诊断的不是单一“加载失败”，而是多个正交状态。

这里关于“调试成本”的判断是课程 inference，不是 DSH 官方设计动机，也没有生产率或 defect-rate 测量。

## 10. 普通 DI 什么时候已经足够

插件化不是成熟度勋章。很多系统用 constructor DI、一个明确 composition root、显式 contributor list 和 start/stop lifecycle，已经可以把问题闭合。

普通 DI 通常足够，当这些前提成立：

- Capability 集合稳定，依赖在启动阶段确定；
- 不需要运行期 provider rebinding 或 HMR；
- 没有第三方 plugin marketplace 或跨团队动态扩展；
- 不需要多个动态 isolation scope；
- contribution 数量有限，owner 可以显式持有 disposer；
- order 能用显式 pipeline/list 表达；
- 一个 composition root 可以编排并测试启动和停止。

更强的 plugin kernel 应在真实需求打破这些前提时再评估，例如：provider 必须运行时替换并驱动 consumer 重载；第三方 extension 需要统一 metadata/dependency/teardown；同一进程需要动态 scope/isolation；late activation 和 partial availability 本身就是产品能力。

判断原则可以压成一句：

> 选择能闭合当前 lifecycle requirements 的最小机制，而不是选择理论扩展性最大的机制。

“普通 DI 何时足够”属于工程 proposal，不是 DSH fact。未来需求改变时，答案也可以改变。

## 11. BuildPilot：吸收约束，默认 `SIMPLIFY`

本篇对 BuildPilot 的价值，不是给出一份 Cordis 移植清单，而是形成一组 future ADR input。

| 决策 | 从 Article 30 带走什么 | 边界 |
|---|---|---|
| `ADOPT` | explicit capability dependency；configured/ready 分离；contribution/disposer 同 owner；transient/durable 分离；post-dispose negative test | 吸收 invariant，不照搬 DSH class/package |
| `SIMPLIFY` | composition root + typed interfaces + explicit contributor order + owner-held disposers | 当前没有 rebinding/HMR/multi-scope/ecosystem 需求证据 |
| `REJECT` as default | ambient string-key dependency、隐式 waterfall order、用 Plugin 吞掉 contribution 类型 | 拒绝默认复杂度，不否定 DSH 的现实需求 |
| `DEFER` | dynamic registry、provider epoch reload、proxy Context、multi-host/plugin marketplace | 等明确需求与 Part VII 授权 |

BuildPilot 当前最该采用的是这条 invariant：register 与 dispose 必须共享 owner，并且要有 dispose 后不再产生新贡献的负向测试。

它不需要因为 DSH 使用 plugin kernel，就提前复制 Context proxy、string-key DI、dynamic epoch 或 waterfall vocabulary。本篇的结论是 `SIMPLIFY`，不是“先照搬，未来再删”。

这仍然只是课程设计 proposal。Article 38 没有启动，Part VII 没有开始，也没有 BuildPilot ADR、代码或 runtime 可供声称完成。

## 12. 动手验证：不要只断言 disposer 存在

下面这套验证方法可以迁移到其他插件系统，不要求使用 DSH，也不需要真实 Provider。

### 12.1 先闭合 source owner

1. 固定 repository、tag、full SHA 与 clean state。
2. 选择一个代表 Capability，找到 configured entry、dependency provider、consumer apply、registration helper 和 exact disposer owner。
3. 给每条边标类型：`CONFIG / INJECT / REGISTER / DISPATCH / DURABLE_APPEND / DISPOSE`。
4. 把 lifecycle owner、dispatch subject 和 durable record owner 分开写。
5. 找一个能推翻 happy path 的负例：missing dependency、invalid config、downstream cancel 或 post-dispose operation。

不要用目录邻接、manifest dependency 或配置 list order 替代 caller/callee 与 lifecycle relation。

### 12.2 冻结一个最小反事实实验

实验 hypothesis 可以写成：

```text
given one active owner and one eligible input
when input runs before disposal
then contribution count increases by one

when the same owner's disposal completes
and the same eligible input runs again
then historical count is unchanged
and no future contribution is emitted
```

实际执行时：

1. dispose 前后复用同一 Context、同一 subject、同一类输入，只改变 owner lifecycle。
2. 记录 `before count`、dispose 是否完成、`after count` 与 source attribution。
3. 临时 probe 只用于让 observation 可见；还要运行 unchanged owner test，不能让 probe 替代仓库 contract。
4. 如果跑 composition E2E，明确写出 Provider 是 deterministic mock 还是真实网络 Provider。
5. 记录失败命令、exit code、stdout/stderr 和校正原因，不只保存最终 PASS。

这个实验不需要真实 credential、production input 或公开网络，也不应该通过修改 owner expectation 来“制造通过”。

### 12.3 怎样解释结果

| 观察 | 可以说 | 不能说 |
|---|---|---|
| first count `0 -> 1` | active path 产生一次 contribution | 所有 operation 都会命中 |
| after dispose `1 -> 1` | 旧历史保留，未来 contribution 停止 | rollback、cancellation、history deletion |
| missing service -> PENDING | dependency 不 ready，Capability inactive | 没 throw 所以运行正常 |
| mock E2E persisted events | composition path 在 fixture 中成立 | real Provider/model/token/cost 成立 |
| package owner tests PASS | selected owner contract 在当前环境通过 | whole repository 或 production 健康 |

## 13. 本篇建立了什么，还没有建立什么

本文可以建立：

- source/runtime 证据绑定 official DSH frozen revision，fixture 在检查时 clean；
- `time-context` 是真实 Loader-compatible plugin；
- 从 configured row 到 dispose/retain 的 `46` 步 pinned source chain 闭合；
- configured、PENDING、ACTIVE 是不同状态；
- `inject=['agents']` 是 runtime service dependency，缺失 observation 是 `state=0/PENDING`；
- `ctx.on` listener 是 plugin Fiber-owned reversible Effect；
- selected listener 全局注册，但每次 operation 由 exact Agent carrier 路由；
- Plugin Context、Model Context，Plugin Event、Session Event，Plugin、Service、Tool 各有不同 owner；
- dispose 后计数 `1 -> 1`，既有历史保留，未来 listener contribution 停止；
- targeted/full package owner tests 与 real Loader/headless deterministic-mock E2E 在记录环境通过。

本文不能建立：

- 所有插件共享同一 shape、依赖、scope 或 cleanup 语义；
- provider replacement runtime 已经执行；
- dispose 会取消 in-flight callback、回滚外部世界或清理任意资源；
- Session append 一定意味着任意 persistence backend 已 flush；
- whole-repository suite health、production readiness 或跨平台普遍性；
- real Provider/model/network/latency/token/cost；
- Article 31—37 的专题结论；
- BuildPilot ADR、实现、runtime 或 Part VII 已启动。

最终证据状态是：

```text
Claims: 15 / 15
Evidence Cards: 15 / 15
Claim status: 12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED
Static lifecycle: 46 steps / SOURCE_CONFIRMED
Owner lifecycle: PASS / before 1 / after 1
Missing dependency: state 0 / PENDING / no immediate throw
Composition runtime: REAL HEADLESS MOCK / PASS
Real Provider: NOT TESTED
BuildPilot: ADOPT INVARIANTS / SIMPLIFY MACHINERY / DEFER ARCHITECTURE
Part VII: NOT STARTED
```

## 14. Claim 与 Evidence Card 对照

为了让结论能回查，下面把 `15 / 15` Claim 映射回本文段落和 Evidence Card。`PROPOSAL` 行是课程判断，不冒充 DSH fact。

| Claim | Status | 本文落点 | Evidence Card | 表述上限 |
|---|---|---|---|---|
| `30-C01` frozen revision / clean fixture | `CONFIRMED` | 开篇、Section 8 | `30-E01` | identity 不证明行为 |
| `30-C02` real Loader-compatible plugin | `CONFIRMED` | Sections 4—5、8 | `30-E02` | 只限代表插件 |
| `30-C03` Plugin Context != Model Context | `CONFIRMED` | Sections 2—3 | `30-E03` | 不覆盖完整 Prompt assembly |
| `30-C04` configured/PENDING/ACTIVE distinct | `CONFIRMED` | Sections 1、5—6 | `30-E04` | selected Loader/Fiber path |
| `30-C05` missing agents -> PENDING | `CONFIRMED` | Section 6 | `30-E05` | 无 immediate throw；replacement source-only |
| `30-C06` `ctx.on` is Fiber-owned Effect | `CONFIRMED` | Sections 5、7 | `30-E06` | 不窥探私有 hook，不外推所有 effect |
| `30-C07` Agent-scoped contribution, not Tool | `CONFIRMED` | Sections 3、5、8 | `30-E07` | global listener，不是 per-Agent Fiber |
| `30-C08` Plugin Event/decision/Session Event differ | `CONFIRMED` | Sections 3、5 | `30-E08` | 不覆盖 replay/backend |
| `30-C09` dispose keeps count `1 -> 1` | `CONFIRMED` | Section 7 | `30-E09` | 不等于 cancellation/rollback |
| `30-C10` Service/Event/Tool kinds differ | `CONFIRMED` | Sections 3、5 | `30-E10` | absence 只限 inspected module |
| `30-C11` Fiber owner != Agent dispatch scope | `CONFIRMED` | Sections 2—3、5 | `30-E11` | 只闭合 selected dispatch |
| `30-C12` owner/full-spec/headless-mock PASS | `CONFIRMED` | Section 8 | `30-E12` | package/mock scope，不是全仓/provider |
| `30-C13` composition power adds debug dimensions | `PROPOSAL` | Section 9 | `30-E13` | conditional inference，无量化测量 |
| `30-C14` ordinary DI sufficiency rubric | `PROPOSAL` | Section 10 | `30-E14` | engineering choice，不是 DSH fact |
| `30-C15` BuildPilot defaults to `SIMPLIFY` | `PROPOSAL` | Section 11 | `30-E15` | 无 ADR、code 或 Part VII |

## 15. 后续文章只接 owner，不在这里抢答

Article 29 建立了 Host/Profile/Loader 到 Agent Run 的总图；本篇只深入其中的 Plugin lifecycle owner。后续 Article 31—37 分别继续承担 Profile composition、Prompt/context assembly、Loop/Step、Session continuation、Tool path、Recovery/observability 与最终 DSH mapping。

这些名字在本文只用于画边界，不表示它们的实验或结论已经完成。当前也不创建指向未发布文章的 `relref`。Article 38 与 Part VII 仍然停止，BuildPilot 只获得研究输入，没有进入设计实现。

## 16. 学习检查

1. 为什么配置 row 存在不能证明 Capability ACTIVE？
2. Cordis Plugin Context 与 model/request Context 分别拥有什么？
3. Plugin Event 要经过哪些 owner 才成为 durable Session Event？
4. 为什么一个 Plugin 不一定是 Service，更不一定是 Tool？
5. `inject=['agents']` 为什么不是 import order 或 constructor DI？
6. 缺 `agents` 时的 `state=0/PENDING` 能证明什么，不能证明什么？
7. `ctx.on` 怎样变成 plugin Fiber-owned reversible Effect？
8. 为什么 selected listener 是 global，却仍执行 exact Agent-scoped operation？
9. `{ prepend: true }` 为什么不表示 contribution 先提交？
10. 46 步 chain 的六个 phase 分别改变了什么 ownership boundary？
11. dispose 后 `1 -> 1` 为什么同时证明旧历史保留与未来贡献停止？
12. 为什么这个结果不能外推为 cancellation、external rollback 或所有 effect 的证明？
13. package spec `19/19` 为什么不代表 whole-repository health？
14. real Loader/headless mock E2E 为什么不能证明 real Provider/token/cost？
15. 哪些条件下普通 DI + composition root 已经足够？
16. BuildPilot 应采纳哪些 invariant，又为什么默认 `SIMPLIFY`？

## 17. 最短结论

“Everything is a Plugin”最容易被写成扩展点清单，但真正决定系统是否可靠的，是 Capability 从 configured 到 ACTIVE 的条件、contribution 的 owner、operation 的 subject，以及 dispose 后还能否用反事实实验观察到未来行为停止。

`state=0/PENDING` 提醒我们：配置存在不等于能力可用。`1 -> 1` 提醒我们：释放的目标不是抹掉历史，而是撤销未来贡献。

最后压成一句：

> 先让依赖、贡献和清理共享可验证的 owner，再决定项目是否真的需要一个更强的插件内核。
