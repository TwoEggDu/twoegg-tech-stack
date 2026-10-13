# Article 36 Research｜Cost、Compaction、Trace、Cancellation 与 Recovery

Status: `RESEARCH COMPLETE / SOURCE_MAP PASS / EXPERIMENT_DESIGN PASS / REQUIRED SOURCE EXPERIMENT NOT EXECUTED`

## 1. Research boundary

- Canonical scope：Article `36`，Part VI，required，weight `L`，标题固定为 `Cost、Compaction、Trace、Cancellation 与 Recovery`。
- Article type：`原理篇 / SOURCE_TRACE / REQUIRED_SOURCE_EXPERIMENT`。后续正文必须按“问题空间 -> 抽象模型 -> pinned DSH 实现 -> 工程边界”推进，不能退化为 package/API 清单。
- Pinned implementation authority：`https://github.com/deepseek-ai/deepseek-harness`，tag `dsh-v0.1.2-alpha.1`，full commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`。
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`。Research Gate 只执行了只读 identity / cleanliness / toolchain probe：`HEAD == tag^{commit} == cd5ef814...`，`git status --porcelain=v1 --untracked-files=all` 无输出，Node `v24.18.1`，pnpm `11.7.0`。
- Official posture：固定版 `README.md` 把项目标为 developer preview；固定版 `SAFETY.md` 明确未安全审计、不可视为 secure / production-ready，sandbox、approval 与 permission 不能作为唯一安全控制。
- Required source experiment：`long Session -> compaction -> cancel -> resume`，并保存 usage、request diff、durable Session events、Trace correlation、cancellation provenance 与 external-side-effect boundary。它是 Article-specific source experiment，不创建 Lab 07。
- Research Gate 不执行 Source Map、Call Path、owner tests、临时 instrumentation、Provider call 或实验；下文任何 DSH 行为均保持 `BLOCKED_SOURCE_MAP` 或 `PENDING_EXPERIMENT`。
- Article 37 尚未开始；本文不研究 RAG / Skill / Workflow / Subagent / Web-Headless extension mapping，不编写 Part VII BuildPilot Architecture、ADR、Design v1 或 Runtime。

## 2. Problem space and intended reader change

长 Session 的工程问题不是“token 多了就压缩、用户点取消就停止、重新打开就恢复”。同一个 Run 内至少有六本不能互相替代的账：

1. Provider 或 estimator 报告了什么 usage；
2. Harness 怎样累计、投影并决定是否允许下一项工作；
3. Context pressure 何时触发 compaction，摘要怎样改变 Model History；
4. Context、Model、Tool、Policy、Error 与 Usage 记录怎样关联成可审查 Trace；
5. cancellation request 在哪些 observer 被看到，哪些工作真正 settled；
6. durable Session prefix 能恢复哪些控制事实，哪些 external side effect 仍是 `UNKNOWN`。

本篇要让读者从“看见 usage/summary/aborted/resume 字样”转向“逐层核对 measurement、policy、event、projection、effect 与 continuation owner”。最容易写错的六个等式必须在开题阶段禁止：

```text
Compaction != Memory
Trace != Session
Cancel != Rollback
Resume != external-side-effect recovery
Cost estimate != provider billing truth
Cancellation requested != work/effect stopped
```

## 3. Dependency and ownership ledger

| Dependency | Reused fact or model | Article 36 must not claim as its own result |
|---|---|---|
| Article 11 / Lab 04 | Checkpoint、Retry、Cancellation、Resume 与 external-effect uncertainty 分账；cancel request 不等于 rollback | 不把课程 fixture 的 recovery state machine 投射成 DSH 行为 |
| Article 13 / Lab 05 | Context Snapshot、compression/pollution 与可重建性边界 | 不把 context compression 自动称为 durable Memory |
| Article 18 | Claim / Observation / Inference / acceptance 分账；Trace 只是候选 Evidence source | 不把 Trace presence 当 Claim accepted |
| Article 20 | Token / Step / Cost / Latency 分账；estimate/reservation/pending/actual 不可互换；Budget 在新消耗前检查 | 不宣称 DSH 已实现课程 `BudgetVector`、reservation 或 provider billing reconciliation |
| Article 21 | Trace/Log/Metric/Audit、Replay/Resume/Retry 与 occurrence/observation/recovery 分账 | 不把 Session event stream 自动称为完整 Trace 或 deterministic Replay |
| Article 28 | 固定 repository/tag/commit、Developer Preview 与 Evidence ladder | 不替 Article 36 证明任何 package 行为 |
| Article 29 | Host/Profile -> Agent/Session/Turn/Step 主骨架；真实 Provider/token/cost 未执行 | 不复用 keyless/Mock runtime 充当 Article 36 long-session Trace |
| Article 30—31 | Plugin lifecycle、Profile/Bundle/Provider/Capability composition | 不重讲装配；只核对 Article 36 相关 plugins 是否实际挂载 |
| Article 32 | request/header、request/context、Prompt/History assembly 与 request diff 方法 | 不把两次 Prompt request trace 自动升级为 compaction request diff |
| Article 33 | Turn/Step、retry/cancel signal spine、无 built-in turn budget 的 bounded source finding | 不把 Agent Loop 的 cancellation fixture替代 Article 36 cancel -> durable event -> resume Trace |
| Article 34 | append-only Session、Projection、Resume 与 compaction append+replacement；generic cost/authorization inheritance 未证明 | 不重跑其全部 event taxonomy；必须新增 usage、long-session、cancel/resume 横切关联 |
| Article 35 | Tool timeout/cancel 是 cooperative；Tool result 可进入 Session/next model view；无 rollback/remote-stop guarantee | 不把单 Tool negative trace 扩张为 Run-level recovery、billing stop 或 external effect recovery |

Article 20 是预算语义 owner；Article 36 只回答 pinned DSH 目前有哪些 measurement / projection / policy / stop seams，以及真实 required experiment 观察到什么。Article 34 是 Session/Projection owner；Article 36 只消费 durable event spine并追踪横切 correlation，不把 Session 重新定义为 Trace。

## 4. Evidence vocabulary

本篇只使用以下 Evidence Class：

- `OFFICIAL_DOC`：固定 repository 内的 README、SAFETY、architecture/subsystem/package docs；只证明文档陈述。
- `PINNED_SOURCE`：固定 commit 的源码与 owner tests；只有 Source Investigator 闭合 file、symbol、caller/callee/call path 后才可标 `SOURCE_CONFIRMED`。
- `RUNTIME_OBSERVATION`：命令、exit code、stdout/stderr、durable events、request snapshots 与 fixture state 的直接观察；尚不存在。
- `EXPERIMENT`：冻结问题、输入、acceptance、safety、command 与 raw trace 后的受控 source experiment；当前仅冻结设计，`NOT EXECUTED`。
- `INFERENCE`：由多张卡推导的课程解释；必须写输入、替代解释与 falsifier。
- `COURSE_PROPOSAL`：未来 Harness/BuildPilot 可采用的 receipt 或 policy 设计；不代表 DSH 或 BuildPilot 已实现。

状态必须继续分开：

```text
DOC_CONFIRMED
SOURCE_CONFIRMED
RUNTIME_CONFIRMED
EXPERIMENT_CONFIRMED
```

`source exists != runtime executed`；`owner test exists != required trace accepted`；`MockAdapter usage != provider billing truth`；`summary persisted != semantic completeness`；`resume succeeded != external effect recovered`。

## 5. Research Questions

| ID | Research Question | Required closure | Current disposition |
|---|---|---|---|
| `36-RQ01` | Request 前、Model 后、Tool 前后和 Session write 分别有哪些横切 Hook；谁拥有顺序、短路与错误传播？ | complete Hook Map + source call paths + representative runtime ordering | `BLOCKED_SOURCE_MAP` |
| `36-RQ02` | Provider usage、heuristic estimate、request pressure、whole-session projection 和 UI disclosure 分别有哪些字段与 owner？ | field/provenance matrix + source consumers | `BLOCKED_SOURCE_MAP` |
| `36-RQ03` | usage 样本按 chunk、assistant message、attempt、Step、Turn、Session 怎样去重/累计；retry 是否产生新 billed attempt？ | fold state/transitions + retry counterexample + raw events | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ04` | pinned DSH 是否存在 Token/Step/Cost/Latency Budget policy；若不存在，measurement 在哪里停止，谁决定下一工作？ | bounded production-source absence/presence search + config/profile activation | `BLOCKED_SOURCE_MAP` |
| `36-RQ05` | Budget/pressure check 的 stop point 在 request 前、Step 前还是 Provider error 后；它允许多大的 overspend/overshoot window？ | check/dispatch order + late usage timing + at least one threshold trace | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ06` | Compaction 的 trigger 是 pressure、context overflow、manual 还是其他；每种 trigger 在哪个 lifecycle seam 进入？ | trigger table + caller/callee paths + activation proof | `BLOCKED_SOURCE_MAP` |
| `36-RQ07` | summary 输入、summarizer route、usage、failure 与 cancellation 怎样记录；summary 是事实、projection 还是 replacement checkpoint？ | compaction transaction path + event/payload matrix + runtime trace | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ08` | Compaction 前后 raw History、current surface、Model request、Projection 与 telemetry 分别改变什么？ | before/after raw stream + deriveMessages/request/projection diff | `PENDING_EXPERIMENT` |
| `36-RQ09` | fresh process rehydration 从 durable log 恢复哪些 compaction、usage 与 projection state；哪些只是 recompute/cache？ | persistence/load/fold path + fresh-runtime equality/known-difference trace | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ10` | Context、Model、Tool、Policy、Error、Usage 能否用 session/turn/step/attempt/call/compaction identities 关联；哪些 ID 缺失？ | correlation matrix + one joined trace; absence must be explicit | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ11` | Session telemetry、OpenTelemetry 或其他 diagnostics 是 Trace、Log export 还是完整 Run Trace；redaction、queue、delivery/loss 在谁的边界？ | capture/export path + configuration/activation + delivery limitation | `BLOCKED_SOURCE_MAP` |
| `36-RQ12` | Agent cancel 怎样传播到 model stream、compaction、Tool 和 maintenance work；request/observed/settled/terminal 是否分开？ | AbortSignal ownership map + durable event ordering + cancel trace | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ13` | crash/cancel 后 Session 怎样记录 incomplete Model/Tool/Step；checkpoint policy 在哪些 side-effect seams fail closed？ | checkpoint/write/flush/recovery path + interruption cases | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ14` | Resume 恢复同一 Session/History 后，usage/projection/cancel state 是否继承、重算或重置？ | fresh-runtime before/after state diff + exact identities | `PENDING_EXPERIMENT` |
| `36-RQ15` | external Tool/Provider side effect 的 status 在 cancel/resume 后能否被 DSH 证明、查询或回滚？ | source owner + deliberately bounded sentinel trace; missing protocol remains `UNKNOWN` | `BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT` |
| `36-RQ16` | 哪组最小 observations 能真实关闭 long Session -> compaction -> cancel -> resume 主线？ | frozen experiment below + accepted raw trace | `FROZEN_DESIGN / NOT_EXECUTED` |

## 6. Preliminary Claim Register

本表是 Research Gate 登记，不是 Source Map 或 Evidence Gate 结论。除固定版本/文档 posture 与已发布依赖外，任何 DSH 行为均未升级。

| Claim ID | Preliminary claim | Class | Status | Required closure |
|---|---|---|---|---|
| `36-C01` | 本篇只讨论 official DSH `dsh-v0.1.2-alpha.1 @ cd5ef814...`；Developer Preview / SAFETY posture 限制所有结论。 | `OFFICIAL_DOC + PINNED_IDENTITY` | `DOC_CONFIRMED` | 已完成只读 identity/cleanliness/toolchain receipt；不升级行为。 |
| `36-C02` | 横切能力必须以 Request 前、Model 后、Tool 前后、Session write 的 owner/seam map 表述，不能靠 package 名猜顺序。 | `PINNED_SOURCE` | `BLOCKED_SOURCE_MAP` | `36-RQ01` complete call paths。 |
| `36-C03` | Provider-reported usage、heuristic estimate、context pressure 与 accumulated projection 是不同账本。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | field/fold map + runtime samples。 |
| `36-C04` | retry/streaming/final message 的 usage 样本需要按 attempt identity 去重或累计；最终值不能仅从一个 UI total 反推。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | attempt/fold path + retry trace。 |
| `36-C05` | measurement/projection 不自动等于 Budget policy；是否存在 built-in Turn/Step/Cost stop 必须做 bounded source search。 | `PINNED_SOURCE + DEPENDENCY` | `BLOCKED_SOURCE_MAP` | policy/activation/absence search；Article 33 只提供一条 bounded clue。 |
| `36-C06` | 当 usage 只能在 Provider response/stream 后确定时，pressure/Budget check 可能存在 overshoot window；窗口必须按实际 check/dispatch order 定义。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | check timing + threshold trace；不得凭常识推断。 |
| `36-C07` | compaction trigger、measurement、region selection、summarizer、durability bracket 与 continuation 是不同 owner。 | `PINNED_SOURCE` | `BLOCKED_SOURCE_MAP` | `36-RQ06—07` call path/table。 |
| `36-C08` | Compaction 是 History/projection transform，不是 Long-term Memory；summary 不自动保留原语义或 Evidence status。 | `PINNED_SOURCE + DEPENDENCY + EXPERIMENT` | `PARTIAL_DEPENDENCY_ONLY` | Article 34 只确认 append+replace；A36 需 trigger/summary/request trace。 |
| `36-C09` | raw log、surface/History、next Model request 与 whole-session projections 在 compaction 后可能不同，必须逐视图比较。 | `PINNED_SOURCE + EXPERIMENT` | `PENDING_EXPERIMENT` | before/after multi-view diff。 |
| `36-C10` | rehydration 只能从 durable records 与 registered reducers恢复；cache、live subscriber、external state 不能静默继承。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | load/fold path + fresh-runtime trace。 |
| `36-C11` | Session event、telemetry export 与可关联 Trace 不是同一对象；完整 correlation 需要显式 identity matrix。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | capture/export path + joined trace。 |
| `36-C12` | cancellation 是协作 signal/observation/settlement/terminal 链，不是强杀或 rollback。 | `PINNED_SOURCE + DEPENDENCY + EXPERIMENT` | `PARTIAL_DEPENDENCY_ONLY` | A33/A35 不替 A36 run-level trace；需 full propagation + durable receipt。 |
| `36-C13` | checkpoint policy 若存在，只能证明 durable intent/barrier；不能证明 exactly-once 或 external effect status。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | request/tool/session flush path + interruption trace。 |
| `36-C14` | Resume 可恢复 Session/History/control facts 的范围必须逐项验证；它不自动恢复 Provider/Tool external side effects。 | `PINNED_SOURCE + EXPERIMENT` | `BLOCKED_SOURCE_MAP` | cancel/resume + unknown-effect trace。 |
| `36-C15` | Cost estimate 或 token-derived price 不是 provider billing truth；无 source-qualified billing record 时 cost 保持 estimate/unknown。 | `DEPENDENCY + PINNED_SOURCE_SEARCH` | `BLOCKED_SOURCE_MAP` | DSH cost surface bounded search；无真实 billing call。 |
| `36-C16` | required experiment 必须保存 long-session usage、compaction、request diff、cancel、resume、Trace correlation 与 fixture cleanliness，任一关键 observation 缺失都保持 `BLOCKED_EVIDENCE`。 | `EXPERIMENT_DESIGN` | `FROZEN_DESIGN / NOT_EXECUTED` | accepted `36-X01` raw capture。 |
| `36-C17` | 未来 Harness 可使用分账的 `RunControlReceipt`，但这里只能作为 Part VII 输入候选，不能成为 BuildPilot Design。 | `COURSE_PROPOSAL` | `PROPOSAL / DEFER` | Article 37 matrix + Part VII authorization。 |

## 7. Candidate source plan for `SOURCE_MAP`

下列文件在 fixed checkout 中作为 discovery pointers 存在；Research Gate 没有闭合 symbol、line anchor、caller/callee 或 activation，因此不能标为 `SOURCE_CONFIRMED`。

| Area | Candidate paths / symbols | Required Source Investigator closure |
|---|---|---|
| Request/Model/Session spine | `packages/core/agent-loop/src/agent.ts`；`preStep`、`step`、`buildRequest`、`request/header`、`request/context`、`assistant/chunk`、`assistant/message` | request before -> adapter stream -> usage/message -> Session append -> next Step |
| Cancellation/Resume | `packages/core/agent-loop/src/agent.ts`、`src/index.ts`；`cancel`、turn signal、`resume/resumeWith`、`runMaintenance` | caller signal -> each operation -> durable terminal -> fresh resume |
| Tool boundary | `packages/core/agent-loop/src/tool-calls.ts`、`packages/core/tools`、`packages/guard/timeout-policy` | call/result/policy/timeout/cancel seams，复用 A35 但建立 A36 correlation |
| Usage measurement | `packages/llm/token-meter/src/index.ts`、`usage-projection.ts`、`turn-usage.ts`、projection/types/readme | provider sample -> attempt fold -> session total/context pressure -> consumers |
| Session stats/projection | `packages/session/session-stats`、`session-projection` | whole-log counters/timing、checkpoint/cache/rehydration 与 compaction independence |
| Compaction contract | `packages/compaction/compaction/src/index.ts`、`types.ts`、`checkpoint.ts` | trigger/service -> start/summary/replacement/end -> durability/continuation |
| Basic backend | `packages/compaction/compaction-basic/src/index.ts`、`region.ts`、`summarizer.ts`、`config.ts` | pressure/context-overflow/manual -> selection -> LLM summary -> usage/failure/cancel |
| Tool-result pruning | `packages/compaction/compaction-tool-result-pruner` | pre-summary pruning vs semantic summary；不可与 A35 spill 混为一谈 |
| Checkpoint policy | `packages/session/session-checkpoint-policy/src/index.ts` | request pre-dispatch、top-level Tool pre-body、pre-Step flush，失败/取消分支 |
| Persistence/recovery | `packages/core/session`、`packages/session/session-persistence*` | append -> coordinator/backend/flush -> load -> synthetic recovery records -> resume |
| Telemetry/Trace export | `packages/session/session-telemetry/src/coordinator.ts`、`session-telemetry-otel/src/index.ts` | event projection/redaction/handoff/export identity/loss；判断是否只是 log export |
| Retry | `packages/llm/llm-retry`、Agent Loop request-error seam | attempt identity、usage accounting 与 stop point |
| Activation | `packages/bundle/base`、headless/web/profile rows与 generated config catalog | source existence vs shipped/default/fixture activation |
| Owner tests | token-meter、compaction、checkpoint-policy、agent-loop、telemetry、persistence owner tests | only test selection candidates; tests do not replace production call path |

Source Investigator 必须额外执行 bounded absence searches：

- generic Turn/Step/Cost/Latency Budget policy 与 stop owner；
- source-qualified billing/cost record，而不是 heuristic pricing；
- universal Trace/run identity distinct from Session id；
- external effect reconcile/rollback protocol；
- cancellation settled/remote-quiesced receipt；
- summary semantic fidelity或 verified/unverified preservation guarantee。

没有命中时只能写 `BOUNDED ABSENCE AT PINNED REVISION`，不能写“DSH 永远没有”。

## 8. Hook Map acceptance matrix

`SOURCE_MAP` 必须输出下面这张表的真实 owner/path，而不是只列事件名：

| Hook position | Required input/output | Must capture | Failure/cancel question |
|---|---|---|---|
| Before request construction | current Session surface、header、Context contributors | session/turn/step/request revision | compaction/steer 后是否重新 derive |
| Before Provider dispatch | final request、model/provider route、signal、durability barrier | request digest/header/context + attempt id | barrier/adapter failure是否产生 usage |
| During/after Model stream | chunks、usage、finish/error、retry | per-attempt usage + final message source seqs | cancel 后 partial usage/message怎样记录 |
| Before Tool body | call、policy/approval/guard、checkpoint、signal | call id + decision + flush receipt | deny/cancel/checkpoint fail是否 body=0 |
| After Tool body | result/error/meta、settlement、Session append | result id/content/error + model-order commit | terminal 是否证明 external effect stopped |
| Compaction trigger/summary | pressure snapshot、selected region、summary request | compaction id + input range + usage + replacement | cancel/fail 中 bracket怎样关闭 |
| Session write/flush | event identity、seq、backend write、projection/telemetry consumers | append/live/durable/flush ordering | crash window与 telemetry loss |
| Resume/rehydration | persistent prefix、reducers、request header、continuation | old prefix digest + new suffix + state equality/diff | unknown Tool/Provider effect怎样表示 |

## 9. Counter-evidence and risk register

1. `tokenUsage` 或 `contextPressure` projection 存在，不证明存在 admission/stop Budget policy。
2. Provider usage 能累计，不证明字段等价于 invoice/billing，也不证明所有 retries/failed streams 都完整计费。
3. heuristic token price 或 route pricing 不是 provider billing truth；没有 source-qualified record 时 cost只能是 estimate/unknown。
4. compaction package、command或trigger存在，不证明它在目标 profile 激活，也不证明本次 long Session 实际触发。
5. summary 成功落盘，不证明语义等价、Evidence verification preserved、Memory built或成本下降。
6. raw events 保留，不代表 Model History、UI view、telemetry 与 rehydrated projection 都完整相等。
7. Session telemetry 的 handed-off cursor 可能只表示 enqueue/handoff，不表示 collector delivered；Trace correlation也可能缺少 Run/attempt identity。
8. cancellation signal 到达某层，不证明 Provider/Tool/summary task已经停止；terminal event也不证明外部 side effect回滚。
9. checkpoint before Tool body 只能缩小 durable-intent window；persisted call without result 仍可能是 effect `UNKNOWN`。
10. fresh resume 能继续 Session，不证明恢复同一 process、live subscriber、credential、approval、Budget policy或external transaction。
11. A33/A35 的 cancel traces 是依赖证据，不是本篇 long-session/compaction/cancel/resume experiment。
12. owner tests green 不等于 required integrated Trace accepted；必须在同一 scenario 中闭合身份、顺序和 before/after views。
13. A28 记录过 whole-suite failure；focused experiments不得升级 whole-repository或production health。
14. Developer Preview / no security audit posture必须进入正文，不可被 selected passing tests冲淡。

## 10. Frozen required source experiment｜`36-X01`

> 本节冻结 research question、hypothesis、falsifier、fixture、安全与 acceptance。它不包含 Observed Result，且不授权 Researcher执行。Source Investigator 可以用真实源码补 exact command/observer，但不得改变问题与 acceptance 来适配结果。

### 10.1 Research Question

在 official pinned DSH 的隔离 fixture 中，一条 deterministic long Session 能否：

```text
accumulate provider-shaped usage
-> cross a source-defined pressure threshold
-> run compaction and replace current History
-> expose a before/after next-request diff
-> receive cancellation during a later active operation
-> persist an honest terminal/incomplete boundary
-> start a fresh runtime and resume the same Session
```

同时，用 stable identities 把 Context、Model、Tool、Policy、Error 与 Usage 关联，并明确 external side effect 没有被 rollback/recovered。

### 10.2 Hypothesis

若目标 composition 实际挂载相关 services，则同一 durable Session log 应足以观察：per-attempt usage与whole-session accumulation、source-defined compaction trigger/bracket/replacement、compaction 前后 current request差异、cooperative cancellation 的 request/observation/settlement/terminal顺序，以及 fresh resume 后从 durable prefix重建的 History/Projection。任何 external Tool effect只能由显式 receipt/reconcile owner判断；缺少该 owner时状态保持 `UNKNOWN`。

### 10.3 What Would Falsify It

- usage 无法绑定 attempt/turn/step，或 retry sample发生无法解释的重复/丢失；
- threshold 已跨越但没有 source-defined check，或所谓“触发”只是直接调用 internal method；
- compaction 只产生 UI 文本，未保存 raw bracket/replacement；
- compaction 前后 request 无法重建或对比；
- cancel后仍声称 work stopped，但没有 settle/terminal receipt；
- fresh resume 修改旧 prefix、重置 usage/identity而无明确 source rule，或依赖旧 live-only state；
- external sentinel 被自动视为 rolled back/recovered，却没有 source-defined receipt/reconcile path；
- required case只能通过真实 Provider、credential、network、production file或外部写操作运行。

### 10.4 Fixture and environment

- official clean checkout at exact commit；运行前后保存 `HEAD/tag/status/diff`。
- Node `v24.18.1`、pnpm `11.7.0`；实际命令由 Source Map 后冻结，禁止 package manager install/update。
- 优先复用 production services + repository-owned `MockAdapter`、in-memory或隔离临时 persistence、deterministic clock/id/latch。
- 如果 owner fixtures 缺 observer，最多增加一个 temporary source-owned instrumentation harness，并保存完整 source/patch/hash；实验结束必须从 fixture移除，checkout恢复 clean。
- no real Provider、credential、network、server、production config、private document、production Tool或真实远端 side effect。

### 10.5 Inputs and phases

| Phase | Input/action | Required observation |
|---|---|---|
| `P0 Baseline` | stable Session + route + exact compaction config + MockAdapter script | activation receipt、initial request/header/context、zero/initial usage |
| `P1 Long Session` | deterministic messages/Tool pair/retries sufficient to grow pressure | attempt IDs、usage events、session total、pressure/projected values、request digests |
| `P2 Compaction` | cross actual source-defined trigger; no direct private-method shortcut | trigger owner、measurement snapshot、start/summary/replacement/end、summary usage/error |
| `P3 Request diff` | let next Step build after compaction | pre/post raw History、derived messages、request header/context/messages/tools、Projection diff |
| `P4 Cancellation` | hold one source-defined active Model/Tool/compaction operation with latch, then cancel | request issued、signal observed、body/stream settlement、durable Step/Turn/error/result、no optimistic rollback |
| `P5 Fresh Resume` | dispose first runtime; create a fresh owner and resume same persisted Session | prefix digest equality、new suffix、usage/projection rehydration、next request、unknown external effect |

### 10.6 Required correlation record

每条 accepted raw record至少包含：

```text
case_id
session_id
event_seq
turn
step
model_attempt_id or explicit ABSENT
tool_call_id or explicit N/A
compaction_id or explicit N/A
request_digest
context_digest / history_digest
event_type + phase
usage_native_fields + usage_source
policy_or_trigger_decision
cancellation_origin / observed / settled
external_effect_status
```

字段在 pinned source不存在时写 `ABSENT_IN_PINNED_CONTRACT`，不得发明 synthetic production identity；临时 instrumentation ID必须标 `FIXTURE_INSTRUMENTATION`。

### 10.7 Acceptance Criteria

1. baseline identity/cleanliness和目标 composition activation可复核；
2. 至少两次 model attempts或 source-defined retry path，使 usage accumulation而非单样本可判定；
3. usage native fields、attempt de-dup/replacement规则、session total和pressure各有raw source；
4. compaction由真实 source-defined trigger进入，并保存完整 bracket、summary/replacement与失败状态；
5. before/after raw log、current surface/History、next request和selected projections均有normalized diff；
6. cancel trace区分request、observation、settlement与terminal，不宣称hard kill/rollback；
7. fresh resume保留同一Session identity与旧prefix，清楚记录usage/projection是persisted、replayed、recomputed还是reset；
8. Context/Model/Tool/Policy/Error/Usage 至少通过session+event seq+turn/step及现有attempt/call/compaction identity闭合；缺ID项显式受限；
9. external sentinel的状态由独立fixture receipt记录，结论只能是`NOT_RUN / KNOWN / UNKNOWN`，不得伪造reconcile/rollback；
10. exact command、exit code、stdout/stderr、raw Trace、unexpected behavior、instrumentation diff、reproduction notes与limitations全部持久化；
11. fixture最终回到exact pinned commit且clean；
12. 任一第2—9项缺失时，`36-C03—C16`相关行为Claim保持`PARTIAL/BLOCKED_EVIDENCE`，不得发布确定性主线。

### 10.8 Safety Boundary / Allowed Commands / Writes / Budget

- Allowed commands：Source Map后冻结的 focused Vitest命令、只读 Git identity/status/diff、Node/pnpm version、只读raw解析；禁止full suite、install/update、real API/e2e、network、server、deployment和外部写操作。
- Allowed fixture writes：一个temporary harness及其 test-owned temp files；必须保存patch/hash并清理。Course repository writes由后续Lab Engineer严格限于Article 36 experiment/raw artifact allowlist，Researcher本轮不创建这些文件。
- Time/attempt budget：最多`1`个integrated harness、最多`8`个focused cases、每条命令wall-clock cap `180s`；selection为`0`或observer缺失时保留失败并返回重新设计，不把互补测试拼成same-call/同一scenario。
- Loopback/server：默认不启动；若Source Map证明owner test不可避免，必须单独获得合法command并只bind loopback。
- Secrets/data：`ZERO`。不读取或写入`.env`、API key、私人文档或production data。

### 10.9 Source Map executable-design disposition

`research question / hypothesis / falsifier / acceptance / safety`保持本节10.1—10.8原文不变。Source Map后的唯一可执行映射冻结在[36-X01实验设计](experiments/36-x01-long-session-compaction-cancel-resume.md)：temporary spec=`packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts`；一个integrated Vitest case顺序覆盖`P0—P5`；复用Agent Loop testkit、Jsonl persistence、Session Projection Registry、TokenMeter、checkpoint policy、BasicCompaction automatic trigger与deterministic adapter/tool latch；focused command固定为`pnpm exec vitest run packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts --reporter=verbose`。

该映射要求保存before/after raw History/request/projection digests、native usage与fold、真实compaction bracket、cancel request/observation/settlement/terminal、dispose后的fresh Context resume、`KNOWN/UNKNOWN` sentinel receipt、closed correlation JSONL、instrumentation patch/hash和cleanup receipt。它没有执行实验，也没有把fixture ordinal写成production model attempt ID；下一Gate只能是`EXPERIMENT_EXECUTE`。

## 11. Source Investigator handoff

Source Investigator 必须返回：

1. `36-RQ01—15` 的 exact file/symbol/stable line anchor；
2. request/model/tool/session/compaction/checkpoint/resume 完整 caller/callee paths；
3. Hook Map、Usage field/fold matrix、Budget/stop/overshoot matrix、Compaction transaction/rehydration matrix、Trace correlation matrix、Cancellation/Resume/external-effect matrix；
4. target profile/bundle activation，区分 package exists、configured、mounted与runtime traversed；
5. owner tests和最小 integrated experiment entry，不执行实验；
6. bounded absence结果与反证搜索；
7. `SOURCE_CONFIRMED / PARTIAL / BLOCKED`逐Claim disposition。

如果 core path、usage owner、compaction trigger、resume/persistence path或required observer不存在，必须保留真实gap并返回 `BLOCKED_EVIDENCE` candidate；不得靠 README 叙述、test name或Article 34/35结论补齐。

## 12. Historical Research Gate result

- Research Questions：`16 / 16` registered。
- Preliminary Claims：`17`；`36-C01 DOC_CONFIRMED`，`36-C08/C12 PARTIAL_DEPENDENCY_ONLY`，`36-C17 PROPOSAL`，其余行为Claims保持`BLOCKED_SOURCE_MAP / PENDING_EXPERIMENT`。
- Required source experiment：`36-X01 FROZEN_DESIGN / NOT_EXECUTED`。
- Current Evidence Gate recommendation：`NOT READY / BLOCKED PENDING SOURCE_MAP + REQUIRED TRACE`。
- Research Gate recommendation：`PASS / NEXT_ALLOWED_GATE = SOURCE_MAP`。

Research completion只表示问题、Claim、反证、来源优先级和实验acceptance已冻结；它不授予 Outline/Draft，也不证明Usage、Compaction、Trace、Cancellation或Recovery行为。

## 13. Current Experiment Design Gate result

- Source Map / Call Path：`PASS`；production owners、activation path与owner tests已闭合。
- Frozen acceptance：§10.1—10.8保持不变。
- Executable design：`PASS`；见`experiments/36-x01-long-session-compaction-cancel-resume.md`。
- Experiment runtime：`NOT EXECUTED`；raw observation、exit code与fixture cleanup receipt均不存在。
- Evidence Gate：`NOT READY`；`36-C03—C16`的runtime部分仍保持pending/blocked。
- Experiment Design Gate recommendation：`PASS / NEXT_ALLOWED_GATE = EXPERIMENT_EXECUTE`。

该结果只授权Lab Engineer按一个integrated harness、一个sequential case与180秒预算执行冻结设计；不授权修改hypothesis/acceptance、补第二个harness、进入Outline/Draft或升级行为Claim。
