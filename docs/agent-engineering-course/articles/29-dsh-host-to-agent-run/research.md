# Article 29 Research

Status: `EVIDENCE MERGED / EVIDENCE_GATE CANDIDATE PASS`

## 1. Research boundary

本篇不是 DeepSeek Harness（下称 DSH）的目录导览，也不把 `Host` 当成一个可随意覆盖 CLI、Node 进程、HTTP server 和浏览器的泛称。它要回答的是：在固定 revision 中，一个受支持的 application entry 如何选择 Profile、组合 Bundle、装载 Plugin，再抵达一次由 Agent、Session、Inbox、Turn、Step 共同承担的 run；其中哪些关系是静态 source fact，哪些需要本篇自己的 runtime trace。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Retrieved / checked：`2026-08-30 / Asia/Shanghai`

本 Researcher 的 fresh read-only check 得到：fixture `origin` 指向官方仓库，`HEAD == local tag target == cd5ef814...e2bbc`，`git status --porcelain=v1 --untracked-files=all` 没有输出。这个结果只确认当前 source fixture 的身份和 clean state，不确认后续 Source Investigator 的完整路径，也不确认任何 application run。

Article 28 的环境基线作为本篇的前置事实继承：Windows 10 x64、Node `v24.18.1`、project-pinned pnpm `11.7.0`；offline frozen install 可完成，build 只有在取得已记录的 host-filesystem access 后完成；direct full unit suite 保持 `FAIL`（`32` failed files / `129` failed tests）；built CLI help 与 config dump 成功；credential-free headless child 停在 `MISSING_CREDENTIAL`。Article 28 只保存 commands、environment、exit codes、terminal summaries、failure classification 与 sanitized excerpts，未保存完整 stdout/stderr stream。以上只定义本篇的环境与证据上限，不能复制成 Article 29 的动态 Agent Run 结论。

## 2. Article type and teaching problem

- Article Type：`ARCHITECTURE_MAP / SOURCE_TRACE`
- Problem Space：读者看到 `apps/`、`packages/core/`、`packages/bundle/` 和大量 `cordis.patch.yml` 后，很容易把目录关系、package dependency、配置顺序和运行生命周期画成同一张图；这会把“可被组合”误写成“已经激活”，也会把 Headless 的直接 core runner 误写成 Web Host 链。
- Abstract Model：`Launch plane -> Composition plane -> Plugin/service plane -> Runtime ownership plane -> Durable observation plane -> Host presentation/control plane`。这些 plane 可以在一个进程中相遇，但不能用同一个名字吞掉。
- Concrete landing：选择受支持的 `dsh --profile headless` 路线，先闭合 CLI/profile/Loader 到 `headless-runner`，再追 `AgentRegistry -> AgentLoop -> ReactLoopAgent -> Session/Inbox -> Turn/Step`；Web/Control 只作为旁路对照，说明它们不在 headless run 的必要链上。
- Scope ceiling：本篇只建立总图和一条 Host/profile 到 Agent Run 的路径，不提前证明 Article 30 的 dispose、Article 31 的 overlay 冲突、Article 32 的 assembly precedence、Article 33 的四类 loop trace、Article 34 的 replay/resume/fork、Article 35 的完整 tool policy pipeline、Article 36 的 recovery。

## 3. Research questions

### 29-RQ01｜Repository Map 应按什么关系画，而不是按文件夹平铺？

需要分别记录 application entry、boot/composition owner、bundle/profile assets、Cordis/Loader plugin core、core service definitions、default runtime driver、Session log、Web Host/Control/Client surfaces。每条边必须声明自己是 package dependency、config insertion、service injection、factory registration、method call、event append 还是 presentation/control relation。

### 29-RQ02｜受支持的 startup entry 到底在哪里？

固定版官方架构文档声明受支持的 Node application 都从 `dsh` CLI 加 named profile 启动。源码 seed 是 `apps/cli/src/bin.ts`、`args.ts:parseDshArgs`、`profile-boot.ts:runProfile` 和 `packages/boot/app-boot/src/index.ts:boot`。后续必须闭合真实分支与 caller/callee，不能仅复述文档。

### 29-RQ03｜Profile 与 Bundle 怎样进入 Plugin tree？

`packages/boot/app-boot/src/profile.ts:PROFILE_TEMPLATES` 将 `headless` 映射到 `dsh-base + dsh-headless`，两个 bundle manifest 的 `dsh.bundle.patch` 指向各自 patch。要验证的是：Profile layer 如何被读取、按什么顺序组成、patch row 怎样交给 Loader、Loader settlement 在哪里发生。配置 row 存在不等于 plugin activation。

### 29-RQ04｜Plugin Core、Runtime、Session 的 owner 分别是谁？

调查 seed：vendored Cordis `vendor/cordis/src/context.ts` / `fiber.ts` 与 `vendor/loader/src/`；`core/agent:AgentRegistry`；`core/agent-loop:AgentLoop, ReactLoopAgent`；`core/session:SessionStore, Session`。Source Investigator 要把 service provision、factory registration、agent/session publication和 driver wake 连起来，并保留 Cordis vendor provenance；不能用 `peerDependencies` 猜生命周期。

### 29-RQ05｜Headless 路线中的 “Host” 到底是什么？

`packages/bundle/headless/cordis.patch.yml` 明示它不装 Host、HTTP server、Web runtime 或 browser plugin；`headless/src/index.ts` 自称 direct core Agent driver。因而本文标题里的 Host 必须解释为“承载受支持 profile 的 launch/application process”，而不是 `packages/host/*` Web Host layer。真正的 Web Host/Control 另由 `dsh-web-app`、`host/webserver`、`api/session-controller`、client packages 组成，只作为总图上的旁路。

### 29-RQ06｜一次 Agent Run 的静态最短链应闭合到哪里？

初始调查假设是：

```text
dsh CLI
-> profile resolution / ordered bundle patches
-> Loader mounts base + headless rows
-> headless-startup publishes task
-> headless-runner waits for settled application
-> AgentRegistry.create
-> registered AgentLoop factory
-> Session preparation/publication
-> ReactLoopAgent
-> followup / Inbox wake
-> turn / step
-> Session events
-> idle / flush / summarized terminal output
```

这只是待证假设。Source Investigator 必须为每个箭头记录 file、symbol 和 call relation；任何断点都保留为 gap，不能用 architecture diagram 或 JSDoc 自动补齐。

### 29-RQ07｜没有真实 credential 时取得了什么最小动态证据？

Article 29 Lab 通过 product source entry 与 repo-owned deterministic `cli-mock` overlay 执行了直接 headless profile：进程 `exit 0`，持久化 Session 解码为 `36` rows，包含一个 Turn、两个 Step，最终 `turn/end.reason.kind = completed`。同一条 authoritative event stream 同时记录 `tool/result.isError = true`、`ToolNotFoundError.code = UNKNOWN_TOOL`，所以 Turn 完成只表示 loop settlement，绝不等于工具成功。校正后的 exact owner test 则 `exit 1`：Windows 基础 patch 禁用 `tool-bash`、启用 `tool-pwsh`，而 deterministic mock 固定请求 `bash`。另一个无 mock、无 credential 的 real-provider composition `exit 1 / MISSING_CREDENTIAL`，没有 provider request、model response、token 或 cost 证据。

### 29-RQ08｜总图怎样路由 Article 30—37 而不抢跑？

本篇只标出专题 owner 和证据缺口：Plugin lifecycle、Profile/Capability seam、Prompt assembly、Agent loop scenarios、Session event/projection、Tool pipeline、cross-cutting controls/recovery、core/extension mapping。路由只证明“下一步去哪查”，不证明专题行为。

## 4. Falsifiable Claim register

| Claim ID | Falsifiable Claim | Current Status | Needed Evidence | Wording ceiling |
|---|---|---|---|---|
| `29-C01` | 本篇所有 DSH 事实都绑定 official repo 的 `dsh-v0.1.2-alpha.1 @ cd5ef814...e2bbc`，fresh fixture identity 与 clean state 匹配。 | `CONFIRMED` | `29-E01` | 只证明研究对象，不证明 path/run |
| `29-C02` | 固定版把 `dsh` CLI + named profile 定义并实现为受支持的 Node application startup boundary。 | `CONFIRMED` | `29-E02` | source path 只覆盖选定的 profile branch |
| `29-C03` | Repository Map 至少包含 Launch、Composition、Plugin Core、Runtime、Session、Control、Web 与 Headless 八类 owner，且边类型不能混用。 | `PROPOSAL` | `29-E03` | 课程总图方法，不是 DSH 自带 taxonomy |
| `29-C04` | Fresh headless Profile 由 `dsh-base` 与 `dsh-headless` 两个 bundle layer 组成，并经 prepare/load 进入 Loader。 | `CONFIRMED` | `29-E04` | 已存在且被修改的 materialized profile 可能不同 |
| `29-C05` | `dsh-base` patch 声明 Session、Agent、Tools、System Prompt、Agent Loop、LLM 与 persistence 等 shared rows。 | `CONFIRMED` | `29-E05` | 只证明声明与装载输入，不普遍声称每行激活 |
| `29-C06` | App boot 创建 Cordis Context/Loader、挂载 root include，并等待 Loader settlement；这是 profile 到 plugin tree 的 source bridge。 | `CONFIRMED` | `29-E06` | 不证明完整 dispose/HMR 语义 |
| `29-C07` | AgentRegistry 是 `ctx.agents` owner，AgentLoop 注册 default factory，`agents.create` 经它创建 ReactLoopAgent。 | `CONFIRMED` | `29-E07` | 不推广到 custom factory |
| `29-C08` | SessionStore/Session 是 run event owner；选定 fixture 的 Turn/Step/tool 结果写入一个持久化 Session stream，stdout 只是 projection。 | `CONFIRMED` | `29-E08` | 不证明 replay/resume/fork 或所有 backend |
| `29-C09` | Headless composition 不装 Web Host/HTTP/browser，而以 startup/runner 直接驱动 core Agent/Session。 | `CONFIRMED` | `29-E09` | 只覆盖 pinned headless composition |
| `29-C10` | `CLI/profile -> Loader -> headless runner -> AgentRegistry -> AgentLoop -> ReactLoopAgent -> Turn/Step -> Session terminal` 在 source 中闭合，并被一次 product-profile fixture trace 有界贯穿。 | `CONFIRMED` | `29-E10` | `exit 0`/Turn completed 不代表 tool success 或 real provider success |
| `29-C11` | 固定版 exact owner expected-success test 在 Windows `exit 1`：base patch 禁用 `tool-bash`、启用 `tool-pwsh`，而 mock 固定请求 `bash`，形成可复现 `UNKNOWN_TOOL` 反证。 | `CONFIRMED` | `29-E11` | 不推断非 Windows 结果；不能写成工具回路成功 |
| `29-C12` | Article 29 keyless real-provider composition `exit 1 / MISSING_CREDENTIAL`；它不提供真实 provider/model response、token 或 cost 证据。 | `CONFIRMED` | `29-E12` | Article 28 仅作环境事实，不替代本篇 observation |
| `29-C13` | Web Profile 的 Host/Control/Client surfaces 与 headless direct runner 是共享 core 上的不同 application composition。 | `CONFIRMED` | `29-E13` | Web 分支为 source-only，未运行 server/browser |
| `29-C14` | Article 30—37 可分别从总图的专题 owner 继续取证，而不需要在 Article 29 证明全部子系统语义。 | `PROPOSAL` | `29-E14` | 课程路由判断 |
| `29-C15` | BuildPilot 当前只应吸收 evidence-backed map/ownership 方法，具体 runtime architecture 决策保持 `DEFER`。 | `PROPOSAL` | `29-E15` | Part VI 输入，不启动 Part VII |

## 5. Merged Repository Map

下表合并 `repository-map.md` 与 `call-path.md` 的 exact owner/edge closure；动态栏只使用 `host-agent-run-trace.md` 的 bounded observation。配置存在与运行激活仍分开表述。

| Plane | Source-confirmed location / seed | Relationship to close | Current ceiling |
|---|---|---|---|
| Launch / CLI | `apps/cli/src/bin.ts`; `args.ts:parseDshArgs`; `profile-boot.ts:runProfile` | argv -> parse -> resolveBoot -> runProfile | `SOURCE_CONFIRMED` |
| Profile / Bundle | `app-boot/src/profile.ts:PROFILE_TEMPLATES,composeProfile,prepareProfile`; `bundle/base`; `bundle/headless` | template -> ordered patches -> prepared profile -> load | `SOURCE_CONFIRMED` |
| Plugin Core | `vendor/cordis/src/context.ts`; `vendor/loader/src/`; `app-boot/src/index.ts:boot,mountRootInclude` | Context + Loader -> mount include -> create/start/await/audit | `SOURCE_CONFIRMED` |
| Runtime interface | `core/agent/src/index.ts:AgentRegistry` | service provide -> factory registration -> `agents.create` dispatch | `SOURCE_CONFIRMED` |
| Runtime driver | `core/agent-loop/src/index.ts:AgentLoop`; `agent.ts:ReactLoopAgent`; `inbox.ts:Inbox` | setFactory -> createAgent -> followup/wake -> turn -> step | `SOURCE_CONFIRMED` |
| Session | `core/session/src/index.ts:SessionStore,Session`; `types.ts:SessionEventMap` | prepare/publish -> append/live event -> flush | `SOURCE_CONFIRMED + FIXTURE_RUNTIME_CONFIRMED` |
| Headless application | `bundle/headless/cordis.patch.yml`; `src/startup.ts`; `src/index.ts` | startup task -> injected runner -> idle/flush/summarize/appExit | `SOURCE_CONFIRMED + FIXTURE_RUNTIME_CONFIRMED` |
| Web application | `bundle/web-app/cordis.patch.yml`; `host/webserver`; `apps/web/src/main.ts:AppWebEntry` | Web rows -> Host transport -> browser entry | `SOURCE_CONFIRMED / SOURCE_ONLY` |
| Control | `api/session-controller`; `api/gateway`; `client/connection` | controller/remote -> live commands -> client projection | `SOURCE_CONFIRMED / SOURCE_ONLY` |
| Observation | `Session.append`; JSONL.zstd persistence; headless `summarize/flush` | durable events -> terminal projection | `36-ROW FIXTURE TRACE; TOOL RESULT UNKNOWN_TOOL` |

Package manifests 和 `peerDependencies` 仍不证明初始化顺序；`cordis.patch.yml` 只证明 configured rows。完整 source path 已闭合 selected branch，direct trace 只确认本次 fixture 实际经过的中央骨架。

## 6. Counter-evidence and alternative explanations

1. **Directory-map shortcut**：`packages/core/agent-loop` 位于 core 目录不等于它在每个 profile 中激活；必须查 composed rows。
2. **Dependency-direction shortcut**：`peerDependencies` 表示 contract surface，不等于 call direction 或 lifecycle order。
3. **Config-row shortcut**：`headless-runner` row 存在不等于已解析其 lazy config、成功 injected service 或执行 `apply/run`。
4. **Headless-as-Web-Host shortcut**：headless patch 明示无 Host/HTTP/browser；若正文画出 Web server，路径即被反证。
5. **Doc-as-source shortcut**：`docs/architecture.md` 是官方 map，但完整 call path 仍需源码 closure；文档不可替代 caller/callee。
6. **Turn-completed-as-tool-success shortcut**：direct product run 的 `exit 0` 与 `turn/end(completed)` 和同一 stream 的 `tool/result UNKNOWN_TOOL` 并存；前两者只证明 loop settlement。
7. **Mock-as-production shortcut**：`cli-mock` fixture 的 `36` rows 只证明 fixture-scoped traversal，不证明 real provider、network、latency、token cost 或 production safety。
8. **Credential-failure shortcut**：Article 29 real-provider composition 的 `MISSING_CREDENTIAL` 只确认本地 fail-closed boundary；没有真实 provider request、model response、token 或 cost。
9. **Stdout-as-session shortcut**：headless terminal text 是 projection；durable outcome 应回到 Session event stream 验证。
10. **Lifecycle universalization**：固定 alpha 版本的组合是 DSH 当前实现，不是所有 Agent Harness 的规范模型。

## 7. Evidence acquisition plan

### 7.1 Source Investigator closure order

1. Fresh identity check：repo URL、HEAD、tag target、clean status。
2. Repository Map：用 package manifests、bundle/profile schema、source owners 和 config rows标记边类型；排除 generated `lib/` 为 source evidence。
3. Launch path：`bin.ts -> parseDshArgs -> runProfile -> composeProfile/prepareProfile -> boot`。
4. Composition path：`PROFILE_TEMPLATES.headless -> dsh-base patch -> dsh-headless patch -> Loader mount/await/activation audit`。
5. Application path：`headless-startup.apply -> provided headlessStartup.task -> headless-runner.apply -> run`。
6. Agent factory path：`agents.create -> AgentRegistry.requireFactory -> AgentLoop.setFactory/createAgent -> Session preparation -> ReactLoopAgent publication`。
7. Driver path：`followup -> Inbox insertion/wake -> kick -> turn -> step`；本篇只需闭合一次 run 的骨架，内部 assembly/tool细节分别路由 Article 32/33/35。
8. Terminal observation path：`whenIdle -> sessions.flush -> summarize(session.events) -> stdout/stderr -> appExit`。
9. Web/Control side map：只记录 `web-app`、`host/webserver`、`api/session-controller`、connection/client owner 与 headless 不同点，不追全部 UI。

### 7.2 Frozen Lab design and observed disposition

Lab 在执行前冻结了如下问题与反证条件，随后按该设计执行：

- Research Question：在不使用真实 provider credential、不绑定公网的前提下，固定 revision 的 product headless profile 能否产生一条包含 Agent/Session/Turn/Step terminal events 的可重现 trace？
- Hypothesis：pinned repo 的 deterministic loopback/replay/mock fixture 可以经受支持的 profile/Loader 路线完成一次 headless run，并输出可与 persisted Session log 交叉核对的 terminal result。
- What would falsify it：targeted suite 无法启动；只跑手工 `ctx.plugin(...)` 而没有 product profile；没有 `turn/start`/`step/start`/`assistant/message`/`step/end`/`turn/end`；stdout 与 persisted log 不一致；发生外部 provider call；fixture 修改 tracked source。
- Fixture：优先 `headless.expected.e2e.ts` 的 product profile scenario 或 `keyless-smoke.e2e.ts`，只用 loopback / replay / repo-owned mock；isolated temp home/workdir。
- Environment：继承 Article 28 toolchain，但重新记录当前 OS/Node/pnpm、command、start/end、exit code。
- Safety：loopback only；no real credentials；telemetry disabled；最小 permission；secret-like env names过滤/不输出值；不绑定公网；不修改 DSH source。
- Acceptance：完整 raw stdout/stderr/exit code 或 durable structured event copy必须保留；明确区分 test fixture trace 与 supported real-provider runtime。
- Observed product fixture：direct product source entry `exit 0 / 3146 ms`；一份 JSONL.zstd Session log，`8` frames、decoded `74578` bytes、`36` rows；`turn/start -> two step pairs -> turn/end(completed)`。其中 `tool/call bash` 后是 `tool/result UNKNOWN_TOOL / isError:true`；stdout 也报告 `Error: unknown tool "bash"`。
- Exact owner test counter-evidence：校正后的 targeted Vitest command `exit 1`，`1 failed / 11 skipped`；Windows base patch `tool-bash disabled / tool-pwsh enabled`，fixture mock 却固定请求 `bash`。此前通过 pnpm wrapper 带 literal `--` 的命令意外收集十个文件，保留为路由失败，不作为 targeted test 结论。
- Real-provider negative：无 mock、移除 secret-like env 的 product source entry `exit 1 / 3105 ms`，Session `17` rows，`turn/end(error MISSING_CREDENTIAL)`；没有 provider request、response、token 或 cost 证明。
- Disposition：中央 source path 与 bounded fixture runtime 已足够支撑 Article 29 的总图主张；Windows tool counterexample 和 credential gap 均作为反证保留，不制造 `BLOCKED_EVIDENCE`。

### 7.3 Core Evidence Card minimum

每张核心卡必须填满：Repository、Pinned Revision、File、Symbol、Call Path、Run Entry、Trace、Evidence Status、DSH Verification、Counter-evidence、Proves、Does Not Prove、Limitations 与 BuildPilot Decision。只有目录、manifest 或 symbol 的卡保持 `PARTIAL`；只有完整静态边闭合后才可 `SOURCE_CONFIRMED`；只有真实命令和 raw trace 才可 `RUNTIME_CONFIRMED`。

## 8. Article 30—37 routing

| Article | Route from Article 29 map | Evidence Article 29 must not preempt |
|---|---|---|
| 30 | Cordis Context/Fiber/Loader + one representative plugin row/service/effect | install/register/operate/dispose semantics and HMR/disposal trace |
| 31 | Profile manifest, bundle patch layers, provider/service/consumer seams | precedence/conflict/effective config dumps and capability substitutions |
| 32 | Agent pre-step/request boundary + system-prompt owner | ordered assembly, duplicate/missing variables, two-step request diff |
| 33 | ReactLoopAgent/Inbox/Turn/Step skeleton | no-tool/single-tool/multi-tool/cancellation four traces |
| 34 | SessionStore/Session/EventMap/persistence observation plane | append/read/projection and replay/resume/fork behavior |
| 35 | Tools registry and loop execution hook | canonicalize/validate/policy/execute/persist plus five negative traces |
| 36 | cross-cutting hooks around request/tool/session | usage, compaction, cancel/resume/recovery terminal experiment |
| 37 | Web/Headless and extension owners | RAG/Skill/Workflow/Subagent core-vs-extension mapping and final matrix |

## 9. Evidence Merge recommendation

`EVIDENCE_MERGE = PASS / CONTINUE TO EVIDENCE_GATE`。

- Claims：`15`
- `CONFIRMED = 12`
- `PARTIAL = 0`
- `PROPOSAL = 3`
- `BLOCKED = 0`
- Evidence Cards：`15`
- Repository Map：`SOURCE_CONFIRMED`; exact selected branch recorded in `repository-map.md` and `call-path.md`
- Host/profile -> Agent Run path：`CLOSED / SOURCE_CONFIRMED`
- Runtime Trace：`TEST_FIXTURE_RUNTIME_CONFIRMED_WITH_COUNTEREVIDENCE`; direct product `exit 0 / 36 events / turn-end(completed)` 与 `tool/result UNKNOWN_TOOL` 同时保留
- Exact owner test：`FAIL / exit 1`; Windows `tool-bash disabled / tool-pwsh enabled` 与 mock 请求 `bash` 构成确定性反证
- Real-provider runtime：`NOT_CONFIRMED`; keyless run `exit 1 / MISSING_CREDENTIAL`; no provider/model response/token/cost claim
- Inherited Article 28：full-suite failure 与 structured/sanitized record ceiling 仅作环境事实，不复制为 Article 29 动态结论
- Core evidence availability：中央静态路径和有界动态骨架已闭合；当前没有 `BLOCKED_EVIDENCE`
- Next allowed Gate：`EVIDENCE_GATE`

本文可以进入 Evidence Gate，但证据上限必须保持：fixture Turn settlement 不是 tool success，mock 不是 real provider，credential failure 不是 provider/model/token/cost observation，Web/Control 分支只做 source-confirmed routing。
