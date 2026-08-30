# Article 35 Research｜Tool Registry 与 Tool Execution Pipeline

Status: `EVIDENCE_MERGE PASS / CYCLE 0 NOT ACCEPTED / RECOVERY ATTEMPT 1 NOT ACCEPTED / CYCLE 1 FIVE-CASE TRACE ACCEPTED`

## 1. Research boundary

- Canonical scope：Article `35`，Part VI，required，weight `L`，标题固定为 `Tool Registry 与 Tool Execution Pipeline`。
- Article type：`原理篇`。后续写作必须遵循“问题空间 -> 抽象模型 -> DSH 固定版本实现 -> 工程边界”，不能退化为 API 清单。
- Pinned implementation authority：`https://github.com/deepseek-ai/deepseek-harness`，`dsh-v0.1.2-alpha.1`，完整 commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`。
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`；本轮重新读取 `HEAD` 为完整固定 SHA，`git status --porcelain=v1 --untracked-files=all` 无输出。
- Official posture：固定版 `README.md` 把项目标为 developer preview；固定版 `SAFETY.md` 明确未安全审计、不可视为 secure / production-ready，sandbox / approval / permission 不能作为唯一安全控制。
- 原 Research Gate 只做 Research 与 Preliminary Evidence；其后 Source Map / Call Path、Cycle 0 raw receipts 与 Recovery Cycle 1 raw receipts 已分别落盘。原 Researcher/Recovery-design turns 没有执行实验；实际 runtime observation 来自独立 Lab Engineer 的 preserved raw capture，不生成 Lab 07。
- 下文候选锚点保留原 Research 快照语义；最终静态闭合以 `repository-map.md` / `call-path.md` 为准，最终运行闭合以 `experiments/raw/recovery-cycle-1/` 为准。历史 snapshot 不因本次 Merge 被改写成当时已确认。

## 2. Dependencies and ownership seams

| Dependency | Reused boundary | Article 35 must not restate as its own result |
|---|---|---|
| Article 05 | `Tool Call != Executed`；schema-valid 不等于 authorized；多 call 不自动规定 Host concurrency | Provider wire format、模型选择质量 |
| Article 06 / Lab 02 | 课程 Tool Runtime 抽象；`Deny > Ask > Allow` 是 Lab 02 的课程设计并只在其 fixture 内确认 | 不能把该聚合规则投射为 DSH 行为 |
| Article 19 | Permission、Approval、HITL、Sandbox 分层；authority 与 evidence 分离 | 完整治理体系与生产安全保证 |
| Article 20 | Token / Step / Cost / Latency 分账；timeout 不等于完整 Budget | Cost、全局 deadline、budget reconciliation（留给 Article 36） |
| Article 21 | Trace、Audit、Replay、failure occurrence / observation / recovery 分离 | 完整 Trace schema、Eval 与 recovery taxonomy |
| Article 28 | 固定版本、source/artifact/runtime 分层与安全上限 | 不替 Article 35 证明具体源码链 |
| Article 29—32 | Host/Profile/Plugin/Scope/Prompt 的上游装配边界 | 不重讲启动、插件内核、Profile 或 Prompt Assembly |
| Article 33 | Agent Loop 调用 Tool Runtime；dispatch 可并行而 policy/result commit 保持 model order；cooperative cancellation | 不把 loop 调度结果替代 Tool Runtime 内部证据 |
| Article 34 | Session append/persistence/projection；Transcript != Model History | 不重讲 replay/resume/fork；只解释 tool result 如何进入这些投影 |
| Article 36 | Cost、Compaction、Trace、Cancellation 与 Recovery | Article 35 只讲单次 Tool execution 的 timeout/cancel/error seam，不展开 run-level recovery |

## 3. Evidence vocabulary

本篇只使用以下 Evidence Class：

- `OFFICIAL_DOC`：固定仓库中的官方 README / SAFETY / docs；证明文档合同，不替代源码或运行。
- `PINNED_SOURCE`：固定 commit 的源码；必须由 Source Investigator 闭合位置和调用链。
- `RUNTIME_OBSERVATION`：Cycle 0 focused owner-test observations 仍为 `NOT_ACCEPTED`；Recovery Cycle 1 的最终 preserved capture 有 `13` 条 SAME-CALL records，覆盖 `35-X01—X05`。原始记录只证明该固定 fixture、MockAdapter 与 in-memory instrumentation 下实际观察到的行为。
- `EXPERIMENT`：具备冻结输入、命令、raw capture 与判据的受控实验。Recovery Attempt 1 因 anchored pattern 选择 `0/5` 而 `NOT_ACCEPTED`；最终 preserved capture 为 `1 file / 5 tests / exit 0`，并通过逐 case acceptance correlation。
- `INFERENCE`：由多张卡推导的课程解释；不得伪装为 DSH 原文。
- `COURSE_PROPOSAL`：课程抽象或未来架构选择；不代表 DSH 或 BuildPilot 已实现。

`DOC_CONFIRMED`、`SOURCE_CONFIRMED` 与 `EXPERIMENT_CONFIRMED` 分开判定。源码存在不等于运行经过；测试夹具通过也不等于真实 Provider、生产 Tool、外部副作用、所有 Host UI 或 production safety 已确认。最终分类见第 10 节。

## 4. Initial Research Questions（historical snapshot before Source Map / Recovery execution）

| ID | Research Question | Current answer | Status / falsifier |
|---|---|---|---|
| `35-RQ01` | Tool 从哪里进入 Registry；是扫描发现、显式注册还是 Provider 导入？ | Candidate source 指向插件显式 `ctx.tools.register()`，ToolRuntime 把 scope-visible definitions 投影给 Prompt Assembly；MCP 只是可能的注册来源，不是 Registry 自身。 | `PRELIMINARY`；若 Source Map 找到独立自动发现/隐式注册 owner，必须拆分。 |
| `35-RQ02` | global / ancestor / agent-local scope、restriction、shadow、duplicate、dispose 如何组合？ | Candidate source：同 layer 重名拒绝；近 scope shadow 远 scope；restriction 只过滤 inherited surface 且多个 restriction 相交；own registration 保留；注册返回 disposer。 | `PRELIMINARY`；Source Investigator 必须验证 `ScopedLayers / NamedEntries / view()` 完整链。 |
| `35-RQ03` | model-visible schema 与 executable definition / Host metadata 是否同一对象？ | Candidate source：wire schema 只投影 name / description / parameters；output schema、execute、timeout、concurrency、presentation callbacks 不上 wire。 | `PRELIMINARY`；若 Provider adapter 注入其他字段，必须按 consumer scope 单列。 |
| `35-RQ04` | raw model arguments、canonical execution args 与 Host metadata 如何分离？ | Agent Loop 保存 raw argument string 并先 JSON parse；Registry 再做 lossless snapshot + deep freeze；callId/name/agent/signal/rootCallId/token/parent 是 Host/runtime metadata，不属于 tool args。 | `PRELIMINARY`；需闭合 `tool-calls.ts -> ToolRuntime.createExecution`。 |
| `35-RQ05` | 参数 schema validation 在哪里发生？ | `defineTool` candidate wrapper 在调用 typed body 前校验；Registry 的 raw `register()` 不自动替任意 definition 执行 input schema validation。因此不能粗写“所有 Registry 调用在 policy 前统一校验”。 | `PRELIMINARY`；反证是找到 central validation 在 pre-policy 前覆盖 raw definitions。 |
| `35-RQ06` | 多个 Allow / Deny / Ask policy 如何合并？ | `tools/pre-execute` 是有序 waterfall：listener 可 `next()` 或短路返回一个 decision；不是可交换的 `Deny > Ask > Allow` vote merge。返回 `ask` 后走一次 approval；之后 monotonic guards 只可 abstain/deny。 | `PRELIMINARY`；若存在独立 policy aggregator，需限定 owner，不可覆盖 waterfall 事实。 |
| `35-RQ07` | pre / execute / post / finalizer / observer 的顺序与异常边界是什么？ | Candidate order：argument materialization -> pre waterfall -> approval -> guards -> execute waterfall/body -> post waterfall -> definition finalizer -> lossless materialization -> `tools/result` observe-only。不同阶段异常并非都走同一路径。 | `PRELIMINARY`；Source Map 必须列出 deny、unknown、pre throw、body throw、post throw、finalizer throw 的分支。 |
| `35-RQ08` | timeout 与 cancellation 是否强制终止？ | timeout plugin 是 `tools/execute` wrapper，换入 deadline signal、等待 next/body settlement，再只把自身 deadline 归类为 `TOOL_TIMEOUT`；caller cancellation 同样 cooperative，Registry 不 hard-kill same-process code。 | `PRELIMINARY`；负向 trace 必须证明“请求停止/等待 quiescence”，不能写成 rollback。 |
| `35-RQ09` | 多 tool concurrency 与结果顺序如何分账？ | Article 33 已确认 candidate scheduler：只有 dispatch/body overlap；pre/finalization/result/context 按 model order commit；exclusive call 形成 barrier，parallel 有 cap 且启动前重分类。 | `DEPENDENCY_CONFIRMED / A35 SOURCE LINK BLOCKED`；Source Map 需建立 Registry scheduler view 的 seam。 |
| `35-RQ10` | success / error result 如何区分 canonical value、model content、UI metadata、persisted event 与 next-step context？ | Candidate source：success value 仅 execution-local；content 是 model-facing；meta 是 replayable UI payload；error info 是诊断；additionalContexts 在 result commit 后进入 next-step FIFO。Session event 持久化 content/isError、error info、meta，不持久化 canonical value。 | `PRELIMINARY`；UI runtime snapshot 与 exact persistence path 仍待 Source Investigator / trace。 |
| `35-RQ11` | 大结果是否有 spill 或 summary？ | 固定源码 candidate 明确存在 opt-in plain-text spill：保存完整文本并以有界 head/tail preview + locator 替换 model/log projection，失败时 best-effort 保留 inline。当前候选范围没有证明独立的语义摘要生成器；UI 中名为 summary 的折叠行不是大结果 summarization。 | `SPILL PRELIMINARY / SEMANTIC SUMMARY NOT PROVEN`；只有 source-wide owning-path closure 可升级 absence。 |
| `35-RQ12` | 哪些最小负向观测足以支撑教学主线？ | 必须覆盖 bad args、deny、timeout、cancel、large result，并分别保存 body-invocation、stage ordering、final result、session event、spill/reference 与 fixture cleanliness。 | `FROZEN DESIGN / NOT EXECUTED`；任一 case 缺 raw capture 都不能升级 runtime Claim。 |

## 4.1 Preliminary Claim Register（historical snapshot）

下表是 Research Gate 的可审计登记，不是 Source Map 或 Evidence Gate 结论。`PARTIAL` 只表示候选锚点和待证问题已被明确记录；它不表示源码链已闭合。除 `35-C01` 的固定版本/文档事实外，任何 DSH 行为必须等待 `SOURCE_MAP`；五个运行项还必须等待独立执行者的原始 trace。

| Claim ID | Preliminary claim | Class | Status | Required closure |
|---|---|---|---|---|
| `35-C01` | 本篇只讨论官方 DSH `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`；README 的 preview 与 SAFETY 的非生产安全姿态限制本文结论。 | `OFFICIAL_DOC` | `DOC_CONFIRMED` | fixture identity/read receipt；不需要把文档升级为行为证明。 |
| `35-C02` | Tool Registry 的显式注册、scope-visible view、schema 投影、executable lookup 与 disposer 是可分开的链路。 | `PINNED_SOURCE` | `BLOCKED_SOURCE_MAP` | `35-RQ01—02` complete path 与反例搜索。 |
| `35-C03` | Model wire schema 不等同于 executable definition 或 Host metadata。 | `PINNED_SOURCE` | `BLOCKED_SOURCE_MAP` | `35-RQ03` consumer-field matrix。 |
| `35-C04` | Raw model arguments、canonical execution args 与 runtime metadata 必须分开归属；raw-arg parse/validation failure 不可推断 body 执行。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | `35-RQ04—05` source path + `35-X01`。 |
| `35-C05` | DSH policy/approval 语义必须按实际 waterfall/guard owner 表述，不能套用 Lab 02 的 vote merge。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | `35-RQ06—07` branch map + `35-X02`。 |
| `35-C06` | deny、unknown、pre/body/post/finalizer failure 可能共享 model-facing error 但不能据此合并 stage ownership。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | error-branch map + `35-X01—02` selected terminals。 |
| `35-C07` | timeout 是 wrapper 归类的 cooperative cancellation seam；timeout/cancel 都不证明 side effect rollback 或 hard kill。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | `35-RQ08` path + `35-X03—04` drain receipts。 |
| `35-C08` | Tool dispatch overlap 与 model-order result/context commit 是不同账本；Article 33 的 scheduler evidence 不替代 Article 35 Registry path。 | `PINNED_SOURCE + DEPENDENCY` | `PARTIAL` | `35-RQ09` registry-to-scheduler seam；不重跑 Article 33。 |
| `35-C09` | canonical value、model content、UI metadata、persisted event 与 next-step context 是不同 result lanes。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | `35-RQ10` data-flow map + all five result captures。 |
| `35-C10` | 大结果候选为 opt-in spill + bounded preview/locator；语义摘要、retention 与 later availability 尚未证明。 | `PINNED_SOURCE + EXPERIMENT` | `PARTIAL` | `35-RQ11` owning path + `35-X05`。 |
| `35-C11` | 五个负例需要保留 body-invocation、stage/result/session/spill 和 clean-fixture receipts；没有 raw capture 不得称 runtime-confirmed。 | `EXPERIMENT_DESIGN` | `CYCLE0_NOT_ACCEPTED / RECOVERY_FROZEN_DESIGN` | Cycle 0 的 22 个 focused tests 不满足 SAME-CALL acceptance；需要 `35-X01—05` Recovery Cycle 1 records。 |
| `35-C12` | BuildPilot 仅可提议显式 ToolExecutionReceipt 与 result-lane separation；不宣称任何实现或安全保证。 | `COURSE_PROPOSAL` | `PROPOSAL` | Part VII authorization/ADR and independent acceptance tests. |

## 5. Candidate Source Plan for `SOURCE_MAP`

以下位置只作为下一角色的查找起点，不是当前 `SOURCE_CONFIRMED`：

| Area | Candidate paths / symbols | Required closure |
|---|---|---|
| Registry/service | `packages/core/tools/src/index.ts`：`ToolRuntime`、`ToolLayer`、`register`、`restrict`、`guard`、`view`、`get`、`schemas` | contribution -> visible view -> model schema -> executable lookup -> disposer |
| Schema | `packages/core/tools/src/schema.ts`：`defineTool`、`validate`；`json-schema.ts` | raw args -> typed validation -> body-not-invoked negative |
| Pipeline | `packages/core/tools/src/index.ts`：`prepareExecution`、`dispatchToolBody`、`finalizeScheduledExecution`、`postExecute`、`finishScheduledExecution`、`notifyResult` | all normal/error/bypass branches |
| Policy / approval | `tools/pre-execute` event；`serviceAsk`；`packages/interaction/user-approval` | ordered waterfall -> ask outcome -> guard deny -> no body |
| Timeout | `packages/guard/timeout-policy/src/index.ts`：`apply`、`TOOL_TIMEOUT` | wrapper signal -> cooperative settle -> classification |
| Concurrency | `packages/core/agent-loop/src/tool-calls.ts`：`executeToolCalls`、`runGroup`、`commitReady` | model order vs settlement order, cap, barrier, cancel drain |
| Result / persistence | `ToolExecutionResult`、`createSuccessResult`、`appendToolResult` | value/content/meta/context -> tool/result event -> Model History |
| UI | `packages/client/ui-tool` models/slots and owning chat projection | persisted raw call/result/meta -> replayable card; no Host callback conflation |
| Spill | `packages/spill/spill-policy`、`spill`、`spill-local`、`util/output-retention` | threshold -> full save -> bounded preview/locator -> best-effort fallback |
| Docs / tests | `docs/tool-execution-pipeline.md`、`packages/core/tools/tests`、owner tests for timeout/spill/agent-loop | docs cannot replace source; tests identify exact trace entry |

## 6. Counter-evidence and risk register

1. `Deny > Ask > Allow` is confirmed only for Article 06's course fixture; DSH candidate semantics are ordered waterfall plus monotonic guards. Treating them as the same would be a false product claim.
2. `defineTool` validation does not prove a raw `ToolDefinition` registered directly gets the same input validation. The Article must preserve this authoring-path boundary.
3. Tool name visibility, model presentation mode and executable reachability may differ under PTC. Article 35 may mention the seam but must not become a PTC tutorial.
4. A denied call, an unknown tool, a pre-hook throw and a body throw may all end as model-visible errors while traversing different post-hook paths. A single final JSON snapshot cannot prove stage ownership.
5. Timeout/cancel signals are cooperative. A terminal error code does not prove remote work stopped, side effects rolled back or a checkpoint exists.
6. Reverse settlement with model-order persistence means timestamps and append order answer different questions. Concurrency claims require both dispatch and commit observations.
7. UI callback/presentation code can be replay-only or Host-local; it must not be treated as model content or durable canonical value.
8. Spill storage success does not imply semantic summarization, retention, authorization or later availability. Spill failure intentionally may keep the oversized inline result.
9. Earlier Article 33/34 runtime traces are dependency evidence, not Article 35 negative trace execution.
10. Article 28's full unit suite failed on the recorded Windows/sandbox environment; targeted future tests cannot upgrade whole-repository health.

## 7. Required trace decision

- Required Lab：`NONE`。Canonical does not designate Article 35 as a Lab Article；`Lab 07` is forbidden.
- Required article experiment：`YES`，five-case negative trace。
- Frozen design artifact：`experiments/tool-execution-negative-traces.md`。
- Current observation：Cycle 0 保持 `22 PASSED / 0 FAILED / NOT_ACCEPTED`；Recovery Attempt 1 保持 `0/5 selected / NOT_ACCEPTED`；最终 preserved Recovery capture 为 `1 file / 5 tests / 13 records / exit 0`，逐 case closure 通过。
- Evidence Gate recommendation：`READY FOR EVIDENCE_GATE`。这只表示五类 required source experiment 与静态 Source Map / Call Path 已具备一致的 Evidence Merge，不把 fixture 结果扩张为 Provider、生产副作用、真实 UI 或 production guarantee。

## 8. Historical handoff to Source Investigator（已由 Source Map / Call Path 完成）

Source Investigator must return exact file/symbol/line anchors and call paths for RQ01—RQ11, explicitly classify every branch as `SOURCE_CONFIRMED / PARTIAL / BLOCKED`, and preserve the following unresolved items:

- whether any discovery/provider path bypasses explicit `register()`;
- input validation coverage for raw registrations versus `defineTool`;
- exact multi-listener waterfall priority and ask/guard interaction;
- error-path post-hook inclusion/exclusion;
- exact fields crossing model/UI/session persistence boundaries;
- bounded absence or presence of semantic large-result summary distinct from spill preview.

本段保留原 Research Gate 的 handoff 内容作为时间语义；其 Source Investigator route 已完成并被下方 Recovery Cycle 1 handoff 取代。Research completion 不等于 Evidence Gate `PASS`，Cycle 0 focused tests 也不等于 negative-trace acceptance，Article 仍未获 Outline/Draft 授权。

## 9. Recovery decision after Cycle 0

### 9.1 Disposition

- Cycle 0 保留为失败的 acceptance attempt：`35-X01—X05 = FAILED_REQUIRED_SOURCE_EXPERIMENT / BLOCKED_EVIDENCE`。`22 passed / 0 failed` 不是五条实验的 acceptance，也不升级任何 Claim 为 `RUNTIME_CONFIRMED` 或 `EXPERIMENT_CONFIRMED`。
- 原 `35-X01—X05` 的 Hypothesis、Falsifier、Inputs、Acceptance、Safety 与 Budget 全部保持不变；本轮不删减字段、不把互补测试拼成同一次调用、不把缺失 observer 解释成隐含成功。
- Source APIs 支持一个安全、source-owned、无 Provider/网络/真实副作用的恢复夹具：AgentLoop 可通过 `MockAdapter` 注入原始 tool-call 字符串，Session 可读取 `tool/call` / `tool/result` 和 `deriveMessages()`，ApprovalService 可记录 asked/decided，timeout wrapper 可用 fake timer + latch 证明 cooperative drain，SpillPolicy 可使用内存 `SpillStore`。未发现需要真实文件、命令、服务器或 credential 的阻断条件。

### 9.2 Selected recovery harness

- Exact temporary path：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814\packages\core\agent-loop\tests\article-35-same-call-recovery.spec.ts`。
- Ownership：单个临时、untracked Vitest test file；不修改 production source、既有 tests、config、dependency、lockfile 或 Git metadata。
- Composition：production `Context / LlmRuntime / SessionStore / SystemPrompt / ToolRuntime / AgentRegistry / AgentLoop`，repo-owned `MockAdapter`，可选 `ApprovalService / timeout-policy / SpillPolicy`，以及 test-local in-memory tools、sentinels、`SpillStore`、fake timers 与 deferred latches。
- Exact five tests：`A35 recovery / 35-X01 / SAME-CALL`、`A35 recovery / 35-X02 / SAME-CALL`、`A35 recovery / 35-X03 / SAME-CALL`、`A35 recovery / 35-X04 / SAME-CALL`、`A35 recovery / 35-X05 / SAME-CALL`。每个 test 必须自行断言并输出逐 call JSONL；任一 required field 缺失即 test fail 且该 case `NOT_ACCEPTED`。
- Full frozen content contract、imports、helper signatures、case fixtures、JSONL schema、commands、acceptance mapping、capture/cleanup order 位于 `experiments/tool-execution-negative-traces.md` 的 `Recovery Cycle 1` 章节。

### 9.3 Handoff

`RESEARCHER / EXPERIMENT_DESIGN PASS`。Next allowed gate：`EXPERIMENT_EXECUTE`。Executor 只能物化冻结的临时测试文件，先把其 exact source 和 generated patch 保存到 Article 35 course raw artifacts，再执行一次 focused command；捕获完成后删除上述 exact temporary path，并用 `HEAD/status/diff` 证明 pinned fixture 恢复 clean。任何 API 漂移、编译失败、missing field、timeout、脏 fixture 或 cleanup failure 都保持 `BLOCKED_EVIDENCE`，不得转向 Provider、网络、服务器、真实 side effect、Lab 07 或 Article 36。

## 10. Recovery Cycle 1 Evidence Merge

### 10.1 Receipt reconciliation

- Baseline and cleanliness：最终 fixture `HEAD` 重新核验为 `cd5ef8148158c3a752a658978873241fdf8e2bbc`；post-cleanup status、unstaged diff 与 staged diff 均为空。临时 instrumentation 只存在于已保存的 course copy / new-file patch，pinned fixture 中已删除 exact untracked test path。
- Attempt history：Cycle 0 的五类 focused runs 和 Recovery Attempt 1 都保留为失败。Attempt 1 虽 exit `0`，但 suite prefix 使 frozen anchored pattern 选择 `0/5`，不能提供 acceptance。最终 Evidence 只采用 preserved capture replay：同一 bounded harness、`1 file / 5 tests / exit 0`、`13` 条 schema-valid records。
- Artifact integrity：Recovery manifest 列出的 `9` 个非 manifest artifacts 的 bytes 与 SHA-256 已逐一重算一致；JSONL 为 `23,654` bytes、SHA-256 `3180b26cc779add7eab3943d235185675cce91ff2813c249b5e7b4062ebc2153`。最终 source / patch 的保存哈希分别为 `7ea805316c4fceb50c88d579a25b6a5cd4527103d77d8d498d32e2e664b8d5ca` 与 `e0e653d08ae75d1a03f69f70589c8f6c219d1e77ec1fe1ce0bd552eab88a7eb2`。
- Toolchain/network caveat：实验命令和 tool bodies 没有 Provider request、网络请求或真实副作用；但一次从 course repository 错误目录执行的裸 `corepack pnpm --version` 预检曾尝试访问 `https://registry.npmjs.org/pnpm/latest`，并被 `EACCES` 阻止。raw manifest 的 `NETWORK_REQUESTS=ZERO` 只可解释为 accepted experiment / Provider / tool-body 范围，不能改写成整个执行 turn 没有网络尝试。

### 10.2 Five-case acceptance

| Case | Preserved records | Acceptance result | Exact boundary |
|---|---:|---|---|
| `35-X01` | `3` | `PASS` | typed path 的 valid body `1`；malformed/schema-invalid body `0`，均为 `INVALID_ARGS`，raw args、Session pair 与 next/history hash 同 callId。raw direct registration 是否自行 validation 仍不由此证明。 |
| `35-X02` | `3` | `PASS` | allow sentinel `1`；deny/ask sentinel `0`；ask 有一对 linked `asked/decided(rejected)`；三条均有 terminal Session/next projection。实际语义是 ordered waterfall / approval seam，不是 vote merge。 |
| `35-X03` | `2` | `PASS` | timeout body 观察 signal；cleanup release 前 Session result 为 `0`；settle 后产生 `TOOL_TIMEOUT`，control 成功。它证明 cooperative drain，不证明 hard kill、rollback 或外部工作停止。 |
| `35-X04` | `2` | `PASS` | cap `1` 下 started body `1`、held body `0`；cleanup release 前无 result，之后分别为 `ABORTED` / `ABORTED_BEFORE_DISPATCH`，follow-up completed。cancel 不是 rollback。 |
| `35-X05` | `3` | `PASS` | small inline；`1,600` bytes payload 的 stored/full hash 相同，preview `200` bytes、locator `/spill/big-ok.txt`；`1,000` bytes fault path 保留完整 inline hash。所有记录 `semanticSummary:false`。spill 是可选 policy，失败 fallback 不做 semantic summary。 |

### 10.3 Final Claim Register

| Claim | Evidence class | Final status | Teaching limit |
|---|---|---|---|
| `35-C01` | `OFFICIAL_DOC` | `DOC_CONFIRMED` | Developer Preview / non-security-audited posture，不是运行或安全保证。 |
| `35-C02` | `PINNED_SOURCE` | `SOURCE_CONFIRMED` | 显式 registration、scope view/dedup/restrict/dispose 已闭合；不推断某 profile 实际装配。 |
| `35-C03` | `PINNED_SOURCE` | `SOURCE_CONFIRMED` | model wire schema、executable definition 与 Host metadata 分 lane；无真实 Provider wire capture。 |
| `35-C04` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED_FOR_TYPED_PATH` | `defineTool` typed path 被 trace；raw registration 不能继承该 validation 保证。 |
| `35-C05` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED` | allow/deny/ask 是 composition-ordered waterfall + approval/guard seam，不是 `Deny > Ask > Allow` 投票。 |
| `35-C06` | `PINNED_SOURCE + RUNTIME_OBSERVATION` | `SOURCE_CONFIRMED / SELECTED_BRANCHES_OBSERVED` | X01/X02 只观察 selected branches；不能把相同 model error 扩张为所有 error-owner 路径相同。 |
| `35-C07` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED` | timeout/cancel cooperative；终态不证明 rollback、hard kill 或 remote quiescence。 |
| `35-C08` | `PINNED_SOURCE + INFERENCE` | `SOURCE_CONFIRMED / DEPENDENCY_BOUNDED` | dispatch overlap 与 ordered commit 分账；本轮未重跑 Article 33 全部 scheduler matrix。 |
| `35-C09` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED_FOR_SESSION_AND_NEXT_MODEL_VIEW` | canonical value、model content、UI meta 与 persisted result 不可折叠；实际 client screen 未执行。 |
| `35-C10` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | `SOURCE_CONFIRMED / EXPERIMENT_CONFIRMED_FOR_OPT_IN_SPILL` | spill/preview/fallback 被验证；universal spill、retention、authorization、retrieval UI、semantic summary 都未证明。 |
| `35-C11` | `EXPERIMENT` | `EXPERIMENT_CONFIRMED` | 仅五类 frozen source experiment；Cycle 0 与 Attempt 1 仍是 `NOT_ACCEPTED`。 |
| `35-C12` | `COURSE_PROPOSAL` | `PROPOSAL / DEFER` | BuildPilot receipt 只是 Part VII 输入，不是 DSH 或 BuildPilot 已实现事实。 |

### 10.4 Merge result

`EVIDENCE_MERGE PASS / NEXT_ALLOWED_GATE = EVIDENCE_GATE`。五类 required traces 已关闭；保留的 limitation 包括：无真实 Provider、无生产 Tool/外部 side effect、无实际 client UI render、无 production safety guarantee、无 universal semantic summarizer。上述 limitation 不阻断 Article 35 的 bounded teaching claims，但必须进入 Outline、Draft 与 Review。
