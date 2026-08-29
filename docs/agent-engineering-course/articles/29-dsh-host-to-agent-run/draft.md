# DeepSeek Harness 总图：从 Host 启动到一次 Agent Run

> **上一篇**：[怎样把 DeepSeek Harness 当作 Evidence-first 源码教材]({{< relref "ai-empowerment/agent-engineering-28-dsh-evidence-first-source-method.md" >}})

> **课程索引**：[Agent Engineering 系统课程]({{< relref "ai-empowerment/agent-engineering-series-index.md" >}})

一张 Agent Harness 架构图，最容易让人产生一种错觉：只要模块都画上去了，运行过程也就解释完了。

目录里有 CLI、Profile、Plugin、Agent、Session 和 Web，图上便顺手把它们从左到右连起来；某个 package 依赖另一个 package，就把依赖线当成调用方向；配置文件出现一个 plugin row，就默认它已经完成激活；项目里有一个 WebServer，便把所有运行模式都画成从 Web Host 开始。

这些图未必每个框都错，但箭头很可能没有证据。目录关系、package dependency、配置组合、service injection、真实调用、持久事件和输出投影，本来是不同的关系。把它们压成同一种箭头，最终得到的不是总图，而是一张无法复查的猜测图。

本文要做一件更窄、也更硬的事：在固定版本的 DeepSeek Harness（下称 DSH）中，选择受支持的 `headless` application path，从 `dsh` CLI 和 named profile 开始，沿 Profile、Bundle、Cordis Context、Loader、AgentRegistry、AgentLoop、Session、Inbox、Turn 与 Step，一直追到 Session flush、terminal projection 和 process exit；再用一次有界运行记录校正静态路径。

先把最容易误读的结果放在一起：这次 direct product fixture 进程 `exit 0`，持久化 Session 有 `36` 行事件，最后是 `turn/end(completed)`；但同一条 Session stream 也明确记录了 `tool/result.isError = true` 和 `UNKNOWN_TOOL`。换句话说，Turn 已经收敛，不等于 Tool 已经成功。

如果这篇只记一句话，可以先记这一句：

> 一张可信的 Harness 总图，不是把模块名连起来，而是让每条边都能说明关系类型、源码 owner、运行证据和证明上限。

本文所有 DSH 源码事实都绑定官方仓库的 [`dsh-v0.1.2-alpha.1`](https://github.com/deepseek-ai/deepseek-harness/tree/dsh-v0.1.2-alpha.1)，完整 commit 为 [`cd5ef8148158c3a752a658978873241fdf8e2bbc`](https://github.com/deepseek-ai/deepseek-harness/tree/cd5ef8148158c3a752a658978873241fdf8e2bbc)。研究时的外部 fixture 指向官方 origin，`HEAD` 与本地 tag target 都等于该 SHA，working tree 没有 tracked/untracked status rows。这个 identity check 只证明研究对象，没有替 build、test 或 run 背书。

本文覆盖 `15 / 15` Claims 与 `15 / 15` Evidence Cards：`12 CONFIRMED / 0 PARTIAL / 3 PROPOSAL / 0 BLOCKED`。其中总图的六层划分、后续文章路由和 BuildPilot 决策输入属于课程方法或设计判断，不冒充 DSH 内建 taxonomy。

## 1. 为什么目录图画不出一次 Agent Run

要画一条可信的运行链，第一步不是找更多模块，而是先承认不同图分别能证明什么。

| 看起来都像“架构图”的材料 | 它能提供什么 | 它不能单独提供什么 |
|---|---|---|
| Directory / package map | 候选 owner 与代码分区 | 调用方向、激活结果、运行顺序 |
| Manifest dependency graph | package availability 与 contract surface | lifecycle、caller/callee、选中的运行路径 |
| Profile / patch graph | composition input 与配置层 | Loader 是否成功激活每个 row |
| Static call path | 固定 revision 的 caller/callee 与分支 | 某次真实输入是否走过该路径 |
| Durable Session trace | 一次指定运行发生过的事件 | 未观测分支、跨平台和跨版本结论 |

例如，`packages/bundle/base/cordis.patch.yml` 声明了 Session、Agent、Tools、System Prompt、Agent Loop、LLM 和 persistence 等 shared rows。这足以证明它们是 base composition 的配置输入，却不能据此说每个 row 都已经激活。Loader 可能仍在等待 service，import 或 apply 可能失败，某些 fiber 也可能处于 pending 状态。

同样，`dsh-agent-loop` 的 manifest 声明对 Agent、Session、LLM、Tools 和 System Prompt 的依赖，只能说明它需要这些 contract surface。真正的 factory 关系必须继续追到 `AgentLoop` 注册 factory，以及 `AgentRegistry.create` 向 factory dispatch 的源码调用。

因此，本文给箭头分类型，而不是只画一种实线：

- `PROFILE_TEMPLATE / BUNDLE_EXPORT / PATCH_COMPOSE` 表示组合关系，不表示调用时序。
- `LOADER_MOUNT / SERVICE_PROVIDE / SERVICE_INJECT` 表示 plugin/service 关系，不自动表示 consumer 已经成功运行。
- `FACTORY_REGISTER / FACTORY_DISPATCH / CALL` 才进入 runtime ownership 和具体调用。
- `DURABLE_APPEND / LIVE_EVENT` 区分 Session 事实与进程内通知。
- `PROJECTION / CONTROL / PRESENTATION` 表示状态的输出或操控表面，不是底层状态 owner。

这套 typed-edge 规则是课程为了可审计性提出的方法，不是 DSH 自己定义的一套架构术语。它的价值在于：当读者问“为什么这里有一条箭头”时，答案不再是“因为两个目录看起来相关”。

## 2. 先把总图拆成六个 plane

在落到类名之前，可以先把一个 application-to-Agent Run 拆成六个问题层。它们可能同时存在于一个 Node 进程里，但不能因为同处一个进程，就被同一个“Host”概念吞掉。

| Plane | 它回答的问题 | 本文中的 DSH 落点 |
|---|---|---|
| Launch | 哪个受支持入口承载应用 | `dsh` CLI、argv、named profile |
| Composition | 哪些 bundle/profile/patch 构成应用 | `PROFILE_TEMPLATES`、`composeProfile`、`dsh-base + dsh-headless` |
| Plugin / Service | 配置怎样成为 plugin tree 与 service | Cordis `Context`、Loader、provide/inject/settlement |
| Runtime Ownership | 谁创建并驱动 Agent Run | `AgentRegistry`、`AgentLoop`、`ReactLoopAgent`、Inbox |
| Durable Observation | 谁记录 Turn、Step、Tool 与终态 | `SessionStore`、`Session.append`、flush |
| Presentation / Control | 谁把状态变成 terminal、Web/API/UI | headless `summarize`/stdout；Web/Control side branch |

这里最关键的不是“六”这个数字，而是边界。

Launch 只负责把 application 拉起来；Composition 决定这次应用由哪些 layer 组成；Plugin/Service plane 把配置条目变成可解析的 service tree；Runtime Ownership 才负责创建和驱动 Agent；Durable Observation 保存执行事实；Presentation/Control 则把事实投影给终端、API 或 UI。

一旦把这些问题拆开，许多争论会自然消失。Profile 里出现一个 row，不代表 Runtime 已经使用它；Terminal 打印一句话，不代表那句话是状态的权威存储；Web 控制面能创建 Agent，也不代表 Headless 必须经过 Web 控制面。

## 3. 标题里的 Host，不是 Web Host

本文标题中的 Host，指承载 `dsh` CLI、Cordis `Context`、Loader 和 application plugin tree 的 launch/application process。它不是 `packages/host/*` 的 Web Host 同义词。

这个区别不是文字游戏，而是由固定版 composition 直接决定的。

在 [`packages/boot/app-boot/src/profile.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/boot/app-boot/src/profile.ts) 中，fresh `headless` profile template 选择 `dsh-base`，再叠加 `dsh-headless`。base patch 声明 shared core candidates；headless patch 提供 startup task 和 direct runner。它没有插入 WebServer、HTTP/browser、SessionController 或 client UI rows。

Web profile 则是另一条 composition：它同样可以复用 base candidates，但第二个 bundle 是 web-app，随后才出现 WebServer、Host Connection、SessionController 和 browser `AppWebEntry`。

```text
dsh CLI / named profile
  -> app-boot + Cordis Context + Loader
     -> dsh-base（shared configured candidates）
     -> selected application bundle
        headless -> startup -> direct runner -> Agent / Session
        web      -> WebServer -> Control / Connection -> browser UI
```

因此，准确的说法不是“Headless 没有 Host”，而是：Headless 没有 Web Host、HTTP server 或 browser layer，但它仍然由一个 launch/application process 承载。

这条结论只覆盖固定 revision 的 selected composition。Web 分支在本文是 source-confirmed side path，没有启动 Web server，也没有验证 browser rendering、transport security 或 Headless/Web 的运行等价性。

## 4. 54 个源码箭头，压成五段主链

完整 source investigation 在固定 revision 上闭合了 `54` 个 caller/callee 或配置、注入、factory、event、projection edge。正文如果把 54 行逐条抄出来，读者会被文件名淹没；如果只留一句“CLI 最后创建 Agent”，可审计性又会丢失。

更合适的表达，是把它压成五段，并保留 exact step range：

| 可读阶段 | Source steps | 压缩后的主链 | 必须保留的边界 |
|---|---:|---|---|
| A. Launch -> settled plugin tree | `1—14` | bin -> parse args -> named profile -> compose/prepare/load -> boot -> Context/Loader -> mount/await/audit | shipped template 可能不同于已被用户修改的 materialized profile；patch list 不是 lifecycle list |
| B. Loader -> direct headless runner | `15—22` | base/headless rows -> startup task service -> injected runner -> await Loader -> lookup core services | configured row 不等于 activation；ordering 来自 injection 与 settlement |
| C. Registry -> published Agent/Session | `23—31` | AgentLoop 注册 factory -> `agents.create` -> dispatch -> Session prepare -> ReactLoopAgent -> publish | selected path 是 `AgentRegistry.create -> createAgent`，不能混入 startup/resume alternate path |
| D. Inbox -> Turn/Step | `32—47` | whenIdle -> followup -> Inbox splice -> wake/kick -> turn/start -> preStep -> step/start -> request/assistant/tool branch -> step/end -> turn/end | 一个 Turn 可以有零个、一个或多个 Step；本文只闭合 skeleton |
| E. Session -> projection/exit | `48—54` | whenIdle -> append -> flush -> summarize -> stdout/stderr -> appExit -> ProcessShutdown | await flush 不等于每种 composition 都有 persistence listener 成功写盘 |

压到最短，整条链是：

```text
dsh --profile headless
-> Profile / Bundle composition
-> Cordis Context + Loader settlement
-> headless-startup + direct runner
-> AgentRegistry -> AgentLoop -> ReactLoopAgent
-> Session prepare/publication -> Inbox -> Turn -> Step
-> Session append/flush -> summarize -> appExit
```

下面按 ownership 变化解释五段，而不是按目录平铺。

### 4.1 从 argv 到 settled plugin tree：steps 1—14

installed `dsh` bin 对应 [`apps/cli/src/bin.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/apps/cli/src/bin.ts) 的构建产物。入口先调用 `parseDshArgs`，`--profile headless` 被解析成 profile invocation，再动态进入 `runProfile`。

`runProfile` 不是直接 new Agent。它先 `composeProfile`，再经过 `prepareProfile` / `loadProfile` 读取或初始化 materialized profile，解析 bundle manifest 暴露的 patch，并按 bundle、profile、home、CLI `--patch` 和可选 telemetry layer 组成 `allPatches`。

之后 [`app-boot/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/boot/app-boot/src/index.ts) 的 `boot` 创建 Cordis `Context`、安装 Loader、挂载 root Include，并等待 Loader settlement 与 activation audit。

这段路径纠正了一个常见误解：不是“profile YAML 列出所有 plugin，然后按列表顺序执行”，而是“空 root config + patch composition -> Include/Loader transactional entry tree”。

### 4.2 从 Loader 到 direct runner：steps 15—22

base bundle 提供 core row candidates，headless bundle 插入 `headless-startup` 与 `headless-runner`。startup 解析 application-owned args，并通过 `headlessStartup` service 提供 task；runner row 注入该 service，再解析 task config。

[`packages/bundle/headless/src/index.ts`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/bundle/headless/src/index.ts) 的 runner 启动后，还会显式等待 Loader settlement，才读取 `agents`、`agentDefaultModel` 与 `sessions` 等 service 并进入创建路径。

所以这里能证明的 ordering，不来自 patch row 在文件里的上下位置，而来自两个更硬的约束：runner 对 `headlessStartup` 的 service injection，以及 runner 内部对 Loader settlement 的 await。

### 4.3 从 Registry 到 Agent/Session publication：steps 23—31

[`AgentLoop`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent-loop/src/index.ts) 在自己的 effect 中向 `ctx.agents` 注册 default factory。headless runner 调用 `agents.create` 后，[`AgentRegistry`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/agent/src/index.ts) 查找 active factory，并把创建请求 dispatch 给 `createAgent`。

default driver 随后准备一个尚未发布的 Session，构造 `ReactLoopAgent`，执行 scoped setup，再按源码规定的顺序发布 Session 与 Agent。只有 setup 和 publication 都成功后，headless consumer 才拿到 Agent。

这里最值得保留的不是类名，而是 interface/driver seam：`AgentRegistry` 是创建接口 owner，`AgentLoop` 是当前默认 factory/driver。这个 seam 能支持替换性讨论，但本文没有验证 custom factory，也不把同名的 configured startup 或 resume path 偷接到 selected headless path 上。

### 4.4 从 Inbox 到 Turn/Step：steps 32—47

headless 先等待 Agent idle，记录这次请求的 Session interval 起点，再调用 `followup`。消息经 `send` 进入 Inbox，`Inbox.splice` 先追加 `agent/inbox/spliced`，然后 driver 被唤醒，进入 `kick -> turn -> preStep -> step`。

Turn 开始时追加 `turn/start`；pre-step claim Inbox input，并进入 system prompt/request assembly anchor；被接受的输入形成 `step/start` 与 `user/message`。随后 request metadata、assistant chunks、assembled assistant message、可选 Tool branch、`step/end` 与最终 `turn/end` 依次进入 Session surface。

这里的静态链只证明 skeleton。一个 Turn 可能在 Step 前拒绝，也可能因为 Tool obligation 形成多个 Step，还可能 abort、error 或以 max-tokens 结束。本文观察到的是一个 Turn、两个 Step，但不能把它写成所有 Turn 的固定形状。

### 4.5 从 Session 到 terminal projection：steps 48—54

driver 收敛后，headless 再次 `whenIdle`，调用 `sessions.flush(agent.session)`，然后通过 `summarize` 折叠自己拥有的 Session interval，写入 stdout/stderr，并把 terminal reason 映射成 exit code。最后 `appExit` 进入 `ProcessShutdown`，dispose root fiber 并记录 process exit code。

[`Session.append`](https://github.com/deepseek-ai/deepseek-harness/blob/cd5ef8148158c3a752a658978873241fdf8e2bbc/packages/core/session/src/index.ts) 的关键顺序是：验证并冻结 event，先把它 push 到 Session log，再通知 process-local `session/event` observer。因此，本轮运行事实要回到 Session stream 判断，不能只看 `agent/status` 或 terminal text。

但仍要保留一个上限：`SessionStore.flush` 会等待 scoped flush listeners，却只返回是否有 listener 参与；headless 不检查这个 boolean。静态路径能证明 awaited checkpoint，不能保证每种 composition 都挂载了 persistence provider 并成功写出同样的 bytes。本次 direct fixture 确实观察到了 JSONL.zstd 文件，这是本次运行的证据，不是对所有运行的普遍保证。

## 5. 三个真正值得迁移的 owner seam

54-arrow 主链之外，有三个边界比任何单个类名更值得带走。

### 5.1 Composition 不等于 activation

`composeProfile/allPatches` 解决的是“这次应用想装什么”；`Context + Loader + mountRootInclude + await/audit` 才把配置送入 plugin tree；consumer 还可能受 service injection 与 settlement 约束。

这意味着，配置输出和 runtime activation 必须分开记账。Article 30 会继续验证 representative plugin 的 install/register/operate/dispose；Article 31 会验证 Profile layer 的 precedence、conflict 与 missing provider。本文只闭合 boot-to-run bridge，不提前替它们下结论。

### 5.2 Interface owner 不等于默认 driver

`AgentRegistry` 暴露 Agent 创建接口，`AgentLoop` 注册当前 default factory，`ReactLoopAgent` 承担默认 driver。把这三者画成一个“Agent”大框，调用链会短一点，却会丢掉替换点、failure point 和 publication boundary。

更稳的总图应该分别回答：谁拥有入口、谁注册实现、谁承担一次实例的运行。

### 5.3 Execution fact 不等于 presentation

Session event stream 保存这次 run 的 Turn、Step、assistant 与 Tool facts；`session/event` 是进程内 live notification；headless stdout 和 Web UI 则是 projection。

如果 terminal text 和 Session stream 冲突，首先应该回到 authoritative Session interval，而不是把 UI 或 stdout 反向当成状态 owner。本文的 direct run 恰好提供了一个很好的例子：stdout 看上去像一句完成文案，但其中已经包含 Tool error；Session 则把错误的发生位置和最终 Turn settlement 分别记录下来。

## 6. 一次 `exit 0` 的 Run，为什么仍然有失败

静态路径闭合后，Lab 使用固定 revision 的 product source entry、named `headless` profile 和 repo-owned deterministic `cli-mock` overlay，执行了一次隔离运行。它没有使用真实 provider credential，也没有连接外部 provider。

| Field | Direct observation |
|---|---|
| Environment | Windows NT `10.0.19045` x64；Node `v24.18.1`；project pnpm `11.7.0` |
| Entry | product source contract；`--profile headless` + repo-owned deterministic overlay |
| Isolation | new cwd、DSH home、agents home；telemetry disabled；secret-like inherited names removed |
| Process | `exit 0`；`3146 ms`；no timeout |
| Session artifact | one JSONL.zstd log；`36` rows；one Turn；two Steps |
| Turn terminal | `turn/end.reason.kind = completed` |
| Tool result | `isError = true`；`ToolNotFoundError.code = UNKNOWN_TOOL`；tool name `bash` |

36 行 event 的完整类型序列很长，压成执行骨架是：

```text
session / permission / sandbox / approval
-> inbox splice -> turn/start
-> step 1 -> request -> assistant/message -> tool/call(bash)
-> tool/result(UNKNOWN_TOOL) -> step/end
-> step 2 -> request -> assistant/message
-> step/end -> turn/end(completed)
```

这条序列至少确认了三件事。

第一，product profile fixture 不是手工 `ctx.plugin(...)` 小 harness。它从 product source contract 进入 named profile，经过 composed runtime，创建了一个 Session，并实际产生了 Turn/Step terminal events。

第二，静态主链得到了有界动态校正。我们没有对 54 个源码箭头逐个插桩，因此不能说每个 arrow 都被 runtime instrumentation 独立捕获；但中央骨架从 Profile/Loader 到 Agent/Session/Turn terminal，已经不再只是目录或 symbol 推断。

第三，也是最重要的一点：同一条 authoritative stream 同时支持“Turn completed”和“Tool failed”。第一步的 assistant message 请求 `bash`；`tool/result` 返回 `UNKNOWN_TOOL`；第二步的 deterministic adapter 读取这个结果，再生成 terminal assistant message；最后 Turn 以 `completed` 收敛。

三层终态必须分开看：

| Terminal layer | Observed fact | Correct interpretation |
|---|---|---|
| Process | `exit 0` | headless 把 completed Turn 映射为进程成功退出 |
| Turn | `turn/end(completed)` | driver 已收敛并给出 terminal reason |
| Tool | `UNKNOWN_TOOL / isError:true` | 请求的 `bash` Tool 没有成功执行 |

完整 stdout 也没有证明 Tool success：

```text
CLI tool round trip complete: Error: unknown tool "bash"
```

它与最后一条 assistant message 一致，只说明 projection 与 Session terminal content 对上了。它不能被截断成“CLI tool round trip complete”后再当作成功案例。

因此，这次运行的准确标签是 `TEST_FIXTURE_RUNTIME_CONFIRMED_WITH_COUNTEREVIDENCE`：确认了 fixture-scoped product traversal 和 terminal Turn，同时明确反证了这台 Windows 主机上的 `bash` Tool round trip。

fixture 中出现的 provider/model 名称是 `cli-mock`，usage 字段也是 deterministic fixture metadata。本文没有把它们写成真实模型响应、真实 token 使用或真实费用。

## 7. Owner test 为什么在 Windows 明确失败

direct run `exit 0`，不代表 repo owner 的 expected-success test 通过。两者回答的是不同问题。

最初按冻结设计执行的 pnpm wrapper command，把 literal `--` 传给了 Vitest，导致它意外收集 `10` 个 expected-test files。这个 wider run 保留为 command-routing failure，但不能充当 Article 29 targeted result。

校正后的 exact owner command 是：

```text
node node_modules/vitest/vitest.mjs run --config vitest.expected.config.ts apps/cli/tests/profiles/headless/tests/headless.expected.e2e.ts -t "runs one task through the product headless profile command"
```

结果为：

```text
Exit: 1
Test Files: 1 failed (1)
Tests: 1 failed | 11 skipped (12)
Target case duration: 4189ms
Snapshot mismatch: expected bash success; received UNKNOWN_TOOL
```

根因不在 OS sandbox。固定版 base patch 在 `process.platform === 'win32'` 时禁用 `tool-bash`、启用 `tool-pwsh`，而 deterministic mock fixture 固定请求名为 `bash` 的 Tool。Tool 在执行之前就无法从当前 composed registry 中找到，因此没有 host-access retry，也不能把失败解释成 shell 被 sandbox 拒绝。

| Question | Result |
|---|---|
| Product path 是否组装到能产生 Session/Tool events？ | 是，fixture-scoped evidence 已取得 |
| Owner expected-success snapshot 是否在本机通过？ | 否，`exit 1` |
| composed Windows profile 中是否有 `bash` Tool？ | 没有，`UNKNOWN_TOOL` |
| 这能否说明非 Windows 结果？ | 不能 |
| 这是不是 sandbox-access failure？ | 不是 |

所以这张反证卡支持的是：exact owner expected-success test 在记录的 Windows 环境中失败，source-confirmed 原因是 `tool-bash` / `tool-pwsh` 条件组合与 mock 固定请求 `bash` 不匹配。它不支持 `pwsh` 已成功执行，也不支持任何非 Windows、真实 provider、token 或 cost 结论。

## 8. 没有凭证的真实 Provider 路线，停在哪里

为了不把 deterministic mock 偷换成真实 provider runtime，Lab 又使用第二个隔离 cwd，去掉 mock overlay，保持 read-only permission、关闭 telemetry，并移除 inherited secret-like environment names，执行同一个 product source entry。

结果如下：

| Field | Direct observation |
|---|---|
| Composition | headless product source entry；no mock overlay |
| Process | `exit 1`；`3105 ms` |
| Session | one log；`17` rows |
| Terminal | `turn/end(error MISSING_CREDENTIAL)` |
| stderr | provider route `deepseek-official` 缺少 credential |
| Provider/model outcome | 没有观察到完成的 provider request 或 model response |

这条 negative path 走到了 request/credential resolution，并把 terminal error 写进 Session。它确认的是本地 fail-closed credential boundary，不是一次 provider 请求失败，更不是 provider/model 已经运行。对应状态必须写成 `REAL_PROVIDER_RUNTIME_NOT_CONFIRMED`。

`MISSING_CREDENTIAL` 不能推出以下任何结论：

- 已经向外部 provider 发出网络请求；
- provider 返回了响应或错误；
- 某个真实模型生成了内容；
- 发生了真实 token 使用、计费或 latency observation；
- authenticated behavior 能正常工作。

同样，前一节的 `cli-mock` completion 不能借给这条路径填空。测试夹具 runtime 与真实 provider runtime 是两种证据，名字相似不允许合并。

## 9. 动手验证：把“调用链成立”和“运行成功”分开

这套验证方法可以迁移到别的 Agent/Harness 项目。重点不是复刻本文的临时路径，而是把静态、动态和终态分开记账。

### 9.1 Source verification

1. 冻结 official repository、tag、full SHA 与 clean state。
2. 选择一个 supported application entry，不用 package-local demo 或手工 plugin harness 替代产品入口。
3. 为每个 edge 记录 caller、callee、file、symbol 与 relation type。
4. 主动寻找 mutable profile、service-gated row、alternate caller、optional listener 等反例。
5. 静态闭合只能标为 `SOURCE_CONFIRMED`；没有 Trace 时，不写“真实输入已经走过”。

### 9.2 Runtime verification

1. 在执行前冻结 research question、hypothesis、falsifier、fixture、permission、credential、network 与 cost boundary。
2. 使用 isolated home/workdir 与 repo-owned deterministic fixture，不使用 production input 或真实 credential。
3. 同时保存 command、exit code、stdout/stderr、Session artifact identity 与 durable events。
4. 把 process exit、Turn terminal、Step count、Tool result 分列检查。
5. 保留失败 snapshot、platform mismatch 和 credential-negative run，不只摘“最像成功”的一行。

最后，用一张表限制措辞：

| Evidence layer | Article 29 result | Allowed wording |
|---|---|---|
| Static source path | 54 arrows closed | fixed revision 中存在该 source path |
| Product fixture traversal | exit 0 / 36 rows / terminal Turn | 一个 deterministic fixture 有界贯穿中央 skeleton |
| Tool operation | `UNKNOWN_TOOL` | 本次 Windows composition 的 Tool success 被反证 |
| Owner test | exit 1 | expected-success contract 在本机失败 |
| Real provider | `MISSING_CREDENTIAL` | credential boundary 已观察；provider runtime 未确认 |

本文 direct fixture 继承了 owner scenario 的 DSH permission mode，但只在新建隔离目录中运行。这个配置名本身不证明 OS sandbox 或安全边界，也不应脱离 fixture 上下文被当成推荐的生产设置。

## 10. 总图能证明什么，专题必须留给谁

总图的价值之一，是告诉后续调查“去哪里找 owner”；它不应该替后续文章提前给答案。

| Article | 本篇交出的 owner seed | 本篇仍未证明的问题 |
|---|---|---|
| 30 | Cordis Context/Fiber、Loader Entry tree、representative plugin effect | install/register/operate/dispose 与 contribution removal |
| 31 | Profile template、bundle/profile/home/CLI patches、service seam | precedence、conflict、effective config、missing provider |
| 32 | `preStep`、System Prompt owner、request boundary | ordered assembly、duplicate/missing variables、two-step diff |
| 33 | Inbox、wake/kick、Turn/Step skeleton | no-tool、single-tool、multi-tool、cancel 四类 terminal trace |
| 34 | SessionStore、Session、EventMap、persistence plane | append/write/read/projection 与 replay/resume/fork |
| 35 | Tools registry、schema、`executeToolCalls` anchor | canonicalize/validate/policy/execute/persist 与 negative traces |
| 36 | request/tool/session cross-cutting hooks | usage、compaction、cancel/resume/recovery terminal behavior |
| 37 | base/headless/web 与 extension owners | core/default/optional/extension matrix 与最终课程映射 |

这些只是 investigation routes。Article 29 找到了 owner 和静态 anchor，不表示 Article 30—37 的专门实验已经完成。Plugin 生命周期不能由 Loader mount 代替，Prompt assembly 不能由 `preStep` symbol 代替，Replay/Resume/Fork 不能由 Session package 名代替，Tool Policy 也不能由 `executeToolCalls` 这个函数名代替。

后续每篇都要在同一 pinned revision 上建立自己的 Claims、Evidence Cards、negative cases 和 Trace。本文不创建指向未发布文章的 `relref`，也不启动 Part VII。

## 11. BuildPilot：吸收画图和验图方法，不照搬 Runtime

把 DSH 读懂，不等于 BuildPilot 应照搬 DSH。Article 29 能提供的是证据受限的设计输入：

| Decision | Article 29 input | Boundary |
|---|---|---|
| `ADOPT` | typed owner / typed edge map；authoritative Session event 与 presentation projection 分离；deterministic contract fixture；missing credential fail-closed expectation | 采用审计方法与失败边界，不采用 DSH package/class identity |
| `SIMPLIFY` | 一个明确 supported entry；显式 capability owner；registry/factory interface seam | 只保留问题与最小接口，不复制多层 Profile/plugin roster |
| `REJECT` | “所有 application 都从 Web Host 开始”；“exit 0/Turn completed 等于 Tool 成功”；“mock usage 等于真实 token/cost” | 拒绝的是已被 source/runtime 反证的等号，不永久否定所有 multi-host 架构 |
| `DEFER` | Cordis plugin kernel、layered Profile、concrete AgentLoop、Web multi-host、persistence/replay、Tool policy、Recovery、真实 provider integration | 等 Article 30—37 与 Part VI Audit；Part VII 未启动 |

这里最重要的克制是：方法吸收可以早于架构吸收。

显式 owner、typed edge、authoritative event 和 negative fixture 都是跨实现可迁移的方法；Cordis、Loader、Profile layer、具体 AgentLoop 和 Web topology 则是固定 DSH revision 的实现选择。只有后续专题证据闭合后，才能判断哪些问题也存在于 BuildPilot，哪些机制值得简化采用，哪些复杂度应当拒绝。

到本文为止，BuildPilot 没有 ADR、没有 runtime implementation、没有 provider integration，Part VII 也没有开始。

## 12. 本篇能建立什么，不能证明什么

本文可以建立：

- official DSH fixed revision 与研究时 clean external fixture identity；
- supported CLI/named-profile boundary 与 selected fresh headless composition；
- 从 bin/Profile/Loader 经 Agent/Session/Turn/Step 到 projection/exit 的 54-arrow static path；
- Headless direct runner 与 Web Host/Control 是共享 base candidates 上的不同 application composition；
- 一个 deterministic product-profile fixture 产生了 36-row durable Session 与 terminal Turn；
- 同一 Session 记录 `UNKNOWN_TOOL`，owner expected-success test 在本机失败；
- keyless real-provider composition 在 `MISSING_CREDENTIAL` 处 fail closed，没有完成的 provider result。

本文不能建立：

- `bash` 或 `pwsh` Tool 成功执行，以及 owner test 跨平台通过；
- 真实 provider/model request/response、network、latency、token accounting 或 cost；
- 完整 Plugin lifecycle、Profile conflict、Prompt assembly、Loop variants、Session continuation、Tool Policy 或 Recovery；
- 每个 configured row 都已激活，或每次 flush 都成功写出 persistence artifact；
- Web server/browser runtime behavior 或 security；
- DSH production readiness、BuildPilot architecture/runtime；
- Article 30—37 已完成，或 Part VII 已启动。

证据账收束如下：

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

## 13. 学习检查

1. 为什么 package dependency 不能直接画成 runtime call arrow？
2. Profile template、materialized profile 与 Loader activation 分别处在哪一层？
3. 本文标题里的 Host 为什么不是 WebServer？
4. Headless 与 Web 共享 base rows，为什么仍是不同 application composition？
5. 54-arrow static path 的五个 phase 分别解决什么 ownership question？
6. 为什么 `AgentRegistry` 和 `AgentLoop` 的 factory seam 值得单独画？
7. `Session.append`、`session/event` 与 stdout 为什么是三个不同层次？
8. direct fixture 为什么可以同时 `exit 0`、`turn/end(completed)` 和 `UNKNOWN_TOOL`？
9. exact owner test 的 Windows failure 为什么不是 sandbox failure？
10. deterministic `cli-mock` usage 为什么不能成为真实 token/cost 证据？
11. `MISSING_CREDENTIAL` 能确认什么，又不能确认什么？
12. Article 29 为什么不能用 source anchor 提前完成 Article 30—37？
13. BuildPilot 本篇可以 `ADOPT / SIMPLIFY / REJECT / DEFER` 什么？

## 14. Claim 与 Evidence Traceability

| Claim | Status | 本文落点 | Evidence | 公开措辞上限 |
|---|---|---|---|---|
| `29-C01` fixed official revision identity | `CONFIRMED` | 开篇、Section 4、9、12 | `29-E01` | identity/clean state only；不推出 path/run |
| `29-C02` supported CLI + named-profile startup | `CONFIRMED` | Sections 2—4 | `29-E02` | selected profile branch only |
| `29-C03` typed Repository Map taxonomy | `PROPOSAL` | Sections 1、2、11 | `29-E03` | 课程方法，不是 DSH taxonomy |
| `29-C04` fresh headless = base + headless | `CONFIRMED` | Sections 3、4 | `29-E04` | mutable materialized profile 可能不同 |
| `29-C05` base patch declares shared rows | `CONFIRMED` | Sections 1、2、4、5 | `29-E05` | declaration/composition，不普遍声称 activation |
| `29-C06` Context/Loader boot-to-tree bridge | `CONFIRMED` | Sections 2、4、5 | `29-E06` | 不证明完整 dispose/HMR |
| `29-C07` registry/default factory/driver seam | `CONFIRMED` | Sections 2、4、5 | `29-E07` | default selected path；不推广 custom factory |
| `29-C08` Session owns event plane; stdout is projection | `CONFIRMED` | Sections 2、4—6 | `29-E08` | 不证明 replay/resume/fork 或所有 backend |
| `29-C09` headless excludes Web Host/HTTP/browser | `CONFIRMED` | Sections 3、4 | `29-E09` | pinned headless composition only |
| `29-C10` static skeleton + bounded fixture traversal | `CONFIRMED` | Sections 4、6、9 | `29-E10` | exit 0/completed Turn 不等于 Tool/provider success |
| `29-C11` Windows owner test fails on bash/pwsh mismatch | `CONFIRMED` | Sections 6、7、9 | `29-E11` | 不推断非 Windows 或 pwsh execution |
| `29-C12` keyless path stops at credential boundary | `CONFIRMED` | Sections 8、9、12 | `29-E12` | 无 provider/model/token/cost success claim |
| `29-C13` Web/Control and headless are distinct | `CONFIRMED` | Sections 3、10 | `29-E13` | Web branch source-only；runtime not run |
| `29-C14` Article 30—37 owner routing | `PROPOSAL` | Sections 10、12 | `29-E14` | route only；future behavior not proven |
| `29-C15` BuildPilot adopts method, defers runtime | `PROPOSAL` | Sections 11、12 | `29-E15` | 无 ADR、implementation 或 Part VII |

覆盖结果：`15 / 15 Claims`、`15 / 15 Evidence Cards`。

## 15. 最短结论

DSH 的 headless 主链在固定 revision 上已经可以从 CLI/Profile 静态追到 Agent、Session、Turn、Step 和 terminal projection，也有一次 deterministic product fixture 为中央骨架提供运行校正。

但最有价值的不是“链跑到了终点”，而是终点仍然保留了内部差异：进程 `exit 0`，Turn `completed`，Tool 却是 `UNKNOWN_TOOL`；owner test 在 Windows `exit 1`；去掉 mock 后，真实 Provider composition 又停在 `MISSING_CREDENTIAL`。

所以，真正可靠的总图必须同时画出 owner、关系和证据上限。

`终态已经收敛，不等于链上每个动作都成功。`
