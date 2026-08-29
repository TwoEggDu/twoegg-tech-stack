# Article 28 Research

Status: `EVIDENCE MERGED / EVIDENCE_GATE CANDIDATE PASS`

## 1. Research boundary

本篇不是 DeepSeek Harness（下称 DSH）的模块导览，也不尝试从 README、目录名或单个 symbol 推出 Harness 的通用定义。它只建立 Part VI 的研究协议：冻结研究对象，区分证据类型与确认层级，规定从 symbol 到 runtime trace 的升级条件，并为 Article 29—37 路由后续取证。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Retrieved / verified：`2026-08-30 / Asia/Shanghai`

只读 Git 与 fresh remote tag 核验得到 `HEAD == local tag == remote tag == cd5ef814...e2bbc`，`origin` 指向官方仓库，fixture 在探针前后均为 clean。这个结果只证明研究快照身份；install、build、test 与 application entry 的结论分别来自 `baseline-manifest.md` 和 `experiments/baseline-probes.md` 的 direct structured observations：commands、environment、exit codes、terminal summaries、failure classification 与 sanitized excerpts。本次 baseline 没有保留完整 stdout/stderr stream。

## 2. Research questions

### 28-RQ01｜怎样证明 Article 29—37 研究的是同一个对象？

每篇 Evidence Card 必须重复记录 repository、完整 commit、文件、symbol、调用路径、run entry 与 trace；开始取证前重新核对 `HEAD` 和 tag，而不是只继承 Article 28 的文字。若任一文章使用不同 SHA，只能经显式 version migration 进入新基线，不能把两个快照拼成一条调用链。

### 28-RQ02｜六类 Evidence 各自能证明什么？

| Evidence Class | 可支持 | 不能单独支持 |
|---|---|---|
| `OFFICIAL_DOC` | 官方声明、支持入口、前置条件、安全警告 | 当前固定 commit 的实际实现、某路径实际运行 |
| `PINNED_SOURCE` | 固定 revision 下的文件、symbol、静态分支和可追踪调用关系 | 该分支被真实输入走过、外部系统行为 |
| `RUNTIME_OBSERVATION` | 指定环境和输入下实际出现的输出、事件、错误 | 未观测分支、内部因果或跨版本稳定性 |
| `EXPERIMENT` | fixture、变量、步骤、结果和限制均冻结时的可复现比较 | 通用生产效果、未覆盖平台和输入 |
| `INFERENCE` | 多项证据之间的显式解释链 | 被包装成源码事实或运行事实 |
| `DESIGN_PROPOSAL` | 课程吸收、简化、拒绝或延后的设计判断 | DSH 已实现或 BuildPilot 已运行 |

Evidence Class 与 Claim Status 是两条轴：例如 `PINNED_SOURCE` 也可能因调用链断裂而为 `PARTIAL`；`EXPERIMENT` 也可能因失败而只支持更窄 Claim。

### 28-RQ03｜`DOC_CONFIRMED`、`SOURCE_CONFIRMED`、`RUNTIME_CONFIRMED` 怎样分账？

- `DOC_CONFIRMED`：固定版本仓库内官方文档或当前官方站点明确声明；必须标明取证日期与版本范围。
- `SOURCE_CONFIRMED`：固定 commit 中的文件、symbol 与静态调用路径闭合；测试存在最多增强静态/fixture 证据，不自动升级为 runtime。
- `RUNTIME_CONFIRMED`：可复现 run entry 在记录环境、输入、exit code 与 raw trace 后，实际走过目标行为。

三者可以同时成立，也可以只成立一个。目录存在、README 描述、类型名、单元测试通过和运行输出都不得互相替代。

### 28-RQ04｜从 symbol 到行为结论的最小升级链是什么？

```text
repository + full revision
-> owning file + symbol
-> caller/callee relationship
-> runnable test or supported application entry
-> frozen input and environment
-> raw output / event / exit code
-> interpretation and counter-evidence
-> bounded Claim Status
```

任一中间环节缺失时，保留缺口并缩窄措辞；不得用架构图补调用路径，也不得用测试名补 raw trace。

### 28-RQ05｜失败探针怎样进入证据？

理想的失败探针协议要求每次 install、build、test、run 都保留命令、环境、exit code、完整 stdout/stderr 与重现条件。本次 baseline 实际持久化的是命令、环境、exit code、终局摘要、失败分类与脱敏关键行，没有保留完整 stdout/stderr stream。这组 direct structured observations 得到：offline frozen install `exit 0`；build 在普通 sandbox `exit 1`、相同命令取得必要 host filesystem access 后 `exit 0`；完整 unit suite `exit 1`（`32` files / `129` tests failed）；单独 notices suite 放宽 timeout 后 `27/27` 通过；CLI help 和 headless config dump `exit 0`；keyless headless child `exit 1` 并停在 `MISSING_CREDENTIAL`。这些结果分别证明自己的窄边界：isolated `27/27` 不能升级 full suite，config dump 不能升级 activation，credential failure 不能升级 Agent Turn、model/provider response、token 或 cost。

### 28-RQ06｜怎样避免把 Developer Preview 写成稳定产品合同？

固定版 README 明示 developer preview 和兼容性破坏风险；`SAFETY.md` 明示尚未安全审计、不得视为 secure 或 production-ready，并要求最小权限。Part VI 所有结论必须带 `dsh-v0.1.2-alpha.1 @ cd5ef814...` 版本上限；“当前固定版如此”不能改写成“所有 Harness 必须如此”。

### 28-RQ07｜Article 29—37 怎样共享证据而不复制结论？

Article 28 只维护路线：每篇在自己的 workspace 建立完整 source card、call path、runtime/experiment trace 和反证。跨篇复用时引用上游 Card 与原始 artifact，不复制一段无来源的总结；新文章需要更强动态措辞时必须补自己的 runtime evidence。

### 28-RQ08｜BuildPilot 可以从 Part VI 带走什么？

每张 DSH 卡最后只做 `ADOPT / SIMPLIFY / REJECT / DEFER` 教学决策，并写原因与证据上限。Article 28 只采用 Evidence-first 方法，暂缓具体架构吸收；不创建 Article 38—44 资产，不宣称 BuildPilot runtime 存在。

## 3. Claim register

| Claim ID | Falsifiable claim | Status | Evidence | Wording ceiling |
|---|---|---|---|---|
| `28-C01` | Part VI 的固定研究对象是官方 repo 的 `dsh-v0.1.2-alpha.1`，其 tag 与 fixture HEAD 均解析到完整 SHA `cd5ef814...e2bbc`。 | `CONFIRMED` | `28-E01` | 只陈述快照身份，不延伸为 build/run 成功 |
| `28-C02` | 固定版官方文档将 DSH 标为 developer preview、未安全审计且非 production-ready，并要求最小权限与独立安全控制。 | `CONFIRMED` | `28-E02` | 只陈述官方安全立场，不评估实际安全强度 |
| `28-C03` | Part VI 必须把 `OFFICIAL_DOC / PINNED_SOURCE / RUNTIME_OBSERVATION / EXPERIMENT / INFERENCE / DESIGN_PROPOSAL` 分开记录。 | `CONFIRMED` | `28-E03` | 这是课程 Evidence Contract，不是 DSH 内建 taxonomy |
| `28-C04` | `SOURCE_CONFIRMED` 与 `RUNTIME_CONFIRMED` 是独立结论；静态路径或测试存在不能代替实际 trace。 | `CONFIRMED` | `28-E03` | 作为 Part VI 写作 Gate 使用，不宣称为 DSH API |
| `28-C05` | 固定 fixture 在已填充 store 上可 offline frozen install；完整 build 只在取得必要 host filesystem access 后成功；本轮完整 unit suite 明确失败（32 files / 129 tests），isolated 27/27 不改变该结论。 | `CONFIRMED` | `28-E04` | 只按本机/本轮/权限条件陈述，不写成 clean-machine、跨平台或 full-suite PASS |
| `28-C06` | built CLI help 与 headless effective-config dump 成功；credential-free headless child 在 `MISSING_CREDENTIAL` 处 fail closed，未完成 Agent Turn、model/provider request，也无 token/cost 观测。 | `CONFIRMED` | `28-E04` | config dump 不是 activation；credential boundary 不是 Agent Run completion |
| `28-C07` | Part VI 使用“identity -> symbol -> call path -> run -> trace -> interpretation”的升级链可阻止静态事实被误写成动态事实。 | `PROPOSAL` | `28-E05` | 课程方法选择；有效性需要 Part VI audit 回看 |
| `28-C08` | Article 29 应先闭合 CLI/profile boot 到 Agent Run 的静态路径，再用一次受控 run trace 验证。 | `PROPOSAL` | `28-E06` | 不在本篇声称调用链已闭合 |
| `28-C09` | Article 30 应以 install/register/operate/dispose 四段生命周期和 disposal 观测检验 “Everything is a Plugin”。 | `PROPOSAL` | `28-E07` | README 口号不是生命周期证明 |
| `28-C10` | Article 31 应沿 Profile、patch/bundle、preset/provider/capability 的加载与 effective config 路线取证。 | `PROPOSAL` | `28-E07` | 目录存在不证明 precedence、冲突或隔离语义 |
| `28-C11` | Article 32 应对两次 request 的 prompt assembly 做可重放 diff，并覆盖缺失/冲突 Context 的负例。 | `PROPOSAL` | `28-E08` | `PromptContext` symbol 只提供起点 |
| `28-C12` | Article 33 应分别保存 no-tool、single-tool、multi-tool 与 cancellation 四条 Turn/Step trace。 | `PROPOSAL` | `28-E08` | 单元测试名或 `AgentLoop` 类存在不等于四条 runtime trace |
| `28-C13` | Article 34 应把 event vocabulary、append、read、projection 与 replay/resume/fork 的 source path 和实际结果分开验证。 | `PROPOSAL` | `28-E09` | 不提前宣称所有事件可重放或任意边界可 fork |
| `28-C14` | Article 35 应闭合 registry -> schema/args -> policy -> executor -> result/event 的路径，并保存五类负例 trace。 | `PROPOSAL` | `28-E10` | 不以注册成功代替执行、拒绝、超时、取消和大结果行为 |
| `28-C15` | Article 36 应把 usage、长 Session、compaction、cancellation、resume/recovery 分成可独立失败的证据链。 | `PROPOSAL` | `28-E11` | 不把 compaction 等同 recovery，不预设成本收益 |
| `28-C16` | Article 37 应对 RAG、Skill、Workflow、Subagent 与 Web/Headless 分别做 core/extension mapping 和课程决策矩阵。 | `PROPOSAL` | `28-E12` | 不把扩展清单写成核心必选能力，也不启动 Part VII |

## 4. Counter-evidence and alternative explanations

1. **Latest-main drift**：未读取 latest main 来补固定版缺口；即使官方站点更新，也只能作为 current-doc 对照，不能覆盖 pinned fact。
2. **README optimism**：README 的启动步骤只是 supported path；实际可构建、可测试、可运行必须由 direct probes 决定。
3. **Test/runtime substitution**：测试通过只证明测试 fixture 和断言；应用真实 profile、provider、网络和进程路径仍需独立 trace。
4. **Name/ownership substitution**：目录名、package 名、类名可能只是组织方式；必须找到 owner、调用者、注册/释放点和 failure path。
5. **Generated/source mixing**：`lib/`、网站生成物、catalog、snapshot 与 source plane 不混用；生成物只能在明确声明其生产命令和 revision 时作为 artifact evidence。
6. **Credential ambiguity**：provider run 失败可能来自缺 key、错误 key、网络、额度、服务端或客户端；没有原始错误与前置核对时不得归因。
7. **Platform ambiguity**：Windows、Node、pnpm 和本地进程模型可能改变结果；单机成功不能升级为跨平台或生产结论。
8. **Safety-feature overclaim**：sandbox、approval 或 permission 只能降低风险，固定版安全文档明确反对把它们当唯一安全控制。
9. **Architecture universalization**：DSH 的 Cordis/all-plugin 选择是当前产品实现，不自动成为所有 Harness 的规范答案。
10. **BuildPilot leakage**：Part VI 的 `ADOPT / SIMPLIFY / REJECT / DEFER` 只是后续设计输入，不是 Article 38—44 已启动或已实现。

## 5. Evidence acquisition protocol

### 5.1 Identity record

每次取证先记录 `git remote get-url origin`、`git rev-parse HEAD`、`git rev-list -n 1 <tag>` 与 `git status --short`。若 identity 或 cleanliness 不匹配，停止，不在漂移 fixture 上继续积累证据。

### 5.2 Source card minimum

源码卡必须有：repository、完整 revision、file、symbol、caller/callee、静态 call path、相关 test、supported run entry、trace 路径、反证搜索、版本限制和 `ADOPT / SIMPLIFY / REJECT / DEFER`。只有 file/symbol 的卡保持 `PARTIAL`。

### 5.3 Dynamic claim minimum

运行主张必须有：OS/architecture、Node/pnpm/Git、依赖状态、命令、输入、环境变量“是否存在”而非 secret 值、exit code、raw stdout/stderr、开始/结束时间、复现步骤和替代解释。任何真实 key、token 或 `.env` 内容不得进入记录。

这是后续文章应达到的教学标准；Article 28 baseline 当前只有 direct structured observations 与 sanitized excerpts，未保留完整 stdout/stderr stream，因此它的 durable evidence ceiling 更低。

### 5.4 Safe execution boundary

使用外部、可丢弃 fixture；不 vendor 到课程仓库；不传真实生产输入；不绑定公网；不读取或打印 secret；不把 DSH 自身 sandbox 当唯一防线；所有可能执行模型命令的 run 使用最小权限与明确输入。Article 28 的 probes 只验证基线，不承载生产任务。

## 6. Article 29—37 evidence routing

下表中的 file/symbol 只是 pinned-source **seed**，由后续 Source Investigator 重新读取并闭合，不构成本篇的 source map 或行为结论。

| Article | Central evidence question | Pinned-source seed | Required runtime / experiment | Falsifier / stop line | Preliminary course decision |
|---|---|---|---|---|---|
| 29 | supported profile 如何进入一次 Agent Run？ | `apps/cli/src/bin.ts:parse dispatch` -> `profile-boot.ts:runProfile` -> `app-boot:index.ts:boot` -> Loader mount/await/activation audit；headless rows在 `packages/bundle/headless/cordis.patch.yml`，runner/loop anchors为 `headless/src/index.ts`、`AgentLoop`、`ReactLoopAgent` | close bundle row -> runner -> `ctx.agents`/factory -> Agent -> Turn，并补 bounded trace | 当前 static baseline 在 settled boot 停止；keyless run 又停在 credential resolution | `DEFER` |
| 30 | plugin 如何安装、贡献能力并释放？ | root/package `AGENTS.md` registration/disposal rules；vendored Cordis effect/fiber owners；代表性 `apply` 与 lifecycle/HMR tests | install/register/operate/dispose 四阶段 trace；dispose 后贡献消失 | 只有 apply/register，无 dispose observation | `DEFER` |
| 31 | profile/bundle/provider/capability 怎样组合并解析配置？ | `args.ts:resolveBoot`；`profile-boot.ts:prepareProfile,composeProfile,allPatches`；`dump-config.ts:runDumpConfig`；`app-boot:index.ts:composeEntries,renderConfigDump`；bundle patches/presets | 冻结 layers/schema、precedence/conflict/missing cases；config dump 仅为 resolution observation | dump 中有 row 不能证明 Loader activation | `DEFER` |
| 32 | 多来源 Context 怎样组成 model request？ | `system-prompt:index.ts:PromptContext,PromptAssembly,SystemPrompt,renderPrompt`；context plugins；`agent.ts:preStep,buildRequest` | registration -> assembly -> request path；两步 diff 与负例 | 只看到 registration/assembly symbol，未看到最终 request trace | `DEFER` |
| 33 | Inbox/Turn/Step/loop 怎样推进和停止？ | `agent.ts:ReactLoopAgent,turn,step`；`runtime-context.ts:RuntimeContextProjection`；`tool-calls.ts:executeToolCalls`；Session turn/step events | no-tool、single-tool、multi-tool、cancellation 四条 trace | keyless baseline 未进入 Agent Turn | `DEFER` |
| 34 | append-only event 如何支持读取、投影与恢复操作？ | `session/types.ts:SessionEventMap,SessionEvent`；`session/index.ts:Session`；`PersistenceCoordinator`；`JsonlSessionPersistence`；`SessionProjectionRegistry`；fork/resume tests | append/write/read/projection path；replay/resume/fork experiments | test/source existence不能证明实际 event sequence 或 compatibility | `DEFER` |
| 35 | tool 从 schema 到 result/event 经哪些 Gate？ | `tools/index.ts:ToolRuntime,ToolDefinition,ToolExecutionResult`；`schema.ts:defineTool,validateArgs`；`tool-calls.ts:executeToolCalls`；`timeout-policy:index.ts:apply,TOOL_TIMEOUT` | close policy/executor ownership；五类 negative traces | baseline 未进入 tool execution | `DEFER` |
| 36 | usage/compaction/cancellation/recovery 怎样分层？ | `session-stats/`；`CompactionEngine`；`compaction-basic/`；`timeout-policy/`；agent-loop；checkpoint-policy 与 persistence | usage/pressure/compaction/cancel/resume 独立终态证据 | 不把 resume 等同 crash recovery，不从本轮失败推成本结论 | `DEFER` |
| 37 | 哪些是 core，哪些是 extension/product composition？ | `docs/architecture.md` extension table；skill/workflow/subagent/web；base/headless/web-app bundles及 shipped patch rows | 每项核验 activation/default status 与 decision matrix | package/config row 存在不等于 core/default/runtime | `DEFER` |

## 7. Source manifest used at Research Gate

| Source | Revision / retrieved | Used for | Confirmation |
|---|---|---|---|
| DSH `README.md` | pinned commit / 2026-08-30 | developer preview、source run outline、official docs entry | `DOC_CONFIRMED` |
| DSH `SAFETY.md` | pinned commit / 2026-08-30 | non-production、安全审计缺失、最小权限、sandbox 上限 | `DOC_CONFIRMED` |
| DSH `AGENTS.md` | pinned commit / 2026-08-30 | repo layout、supported app launch、commands、registrations/model-visible/capability rules | `DOC_CONFIRMED`; dynamic behavior not confirmed |
| DSH `docs/architecture.md` | pinned commit / 2026-08-30 | extension map 与后续 source route seed | `DOC_CONFIRMED`; call paths pending |
| DSH `docs/development.md` | pinned commit / 2026-08-30 | prerequisites、build/profile run requirements、credential policy | `DOC_CONFIRMED`; probes pending |
| DSH `package.json` + `scripts/build.ts` + CLI/boot source | pinned commit / 2026-08-30 | install/build/test/dsh dispatch 与 closed boot baseline | `SOURCE_CONFIRMED`; Agent path remains Article 29 |
| `source-map.md` | pinned commit / 2026-08-30 | source/artifact boundary、6 baseline source records、Article 29—37 exact anchors | `SOURCE_CONFIRMED`; future full paths `PARTIAL/DEFER` |
| `baseline-manifest.md` + `experiments/baseline-probes.md` | direct Lab / 2026-08-30 | environment、commands、exit codes、failure classes、credential boundary | `RUNTIME_CONFIRMED` only for bounded observations |
| Course Factory / Production Workflow / Evidence template | course repository current transaction | Evidence taxonomy、DSH gate、confirmation separation、decision field | course contract confirmed |

## 8. Research recommendation

`EVIDENCE_MERGE = PASS`；推荐 `EVIDENCE_GATE = PASS`。

- Claims：`16`
- `CONFIRMED = 6`
- `PARTIAL = 0`
- `PROPOSAL = 10`
- `BLOCKED = 0`
- Evidence Cards：`12 = 4 CONFIRMED / 0 PARTIAL / 8 PROPOSAL / 0 BLOCKED`
- DSH confirmation：`DOC_CONFIRMED = 2 card surfaces`；`SOURCE_CONFIRMED = baseline 6-record map + 9 cards with exact identity/command/route anchors`；`RUNTIME_CONFIRMED = 1 baseline experiment card with bounded outcomes`；completed Agent Run=`0`
- Runtime Observation：`PRESENT / BASELINE ONLY`
- Experiment：`1 baseline probe set / direct structured observations with commands, environment, exit codes, terminal summaries, failure classification and sanitized excerpts; full stdout/stderr stream not retained`
- Next allowed Gate：`EVIDENCE_GATE`

核心 Claim 没有 `BLOCKED`：build 以 host-access caveat 收窄，full suite 如实保留 `FAIL`，keyless run 只写到 credential resolution。Article 29—37 的完整 call paths 与动态 evidence 仍由 owning article 负责；它们的 `PENDING/DEFER` 不构成 Article 28 baseline 的缺失，也不得在本篇升级。
