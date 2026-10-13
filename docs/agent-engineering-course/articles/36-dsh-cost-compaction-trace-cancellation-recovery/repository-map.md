# Article 36 Repository Map — Cost、Compaction、Trace、Cancellation 与 Recovery

## 1. 固定身份与证据边界

- Official repository：`https://github.com/deepseek-ai/deepseek-harness`
- Pinned tag：`dsh-v0.1.2-alpha.1`
- Pinned full commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- 调查前复核：fixture `HEAD` 等于上述 full commit；tag 精确指向该提交；`git status --short --branch` 为 detached HEAD 且无修改。
- 安全边界：已读取根 `SAFETY.md`、根 `AGENTS.md`、`docs/development.md` 与 `packages/AGENTS.md`。该版本是 Developer Preview；本 Gate 只读源码与测试，不执行实验，不把 package/test 存在升级成目标组合已激活或运行时已观测。

本文路径、symbol 与行号只锚定上述 commit。`SOURCE_CONFIRMED` 表示固定源码直接证明；`BOUNDED_ABSENCE` 表示在指定生产源码范围有界检索未发现 owner；`RUNTIME_PENDING` 表示必须由后续 36-X01 关闭。

## 2. 运行时激活边界

| 层 | Source：symbol / stable anchor | Caller → callee / 激活关系 | Source observation | Counter-evidence / limitation | Status |
|---|---|---|---|---|---|
| Profile layering | `docs/architecture.md`: `Profiles and bundles`（15-29） | named profile → ordered bundle patches → profile/home/CLI overlays | `web`、`headless`、`sdk`、`acp` 的共享第一层是 `dsh-base`；后置 patch 可按 id 整行替换配置。 | 默认项不等于某台机器的 effective composition；用户 overlay 可以禁用或替换行。 | `SOURCE_CONFIRMED` |
| Base composition | `packages/bundle/base/cordis.patch.yml`: `token-meter`（323-324）、`compaction-basic`（326-327）、`session-checkpoint-policy`（398-400）、`session-telemetry-otel`（168-203） | shipped profile including `dsh-base` → Loader rows → plugins | 随附 base 层挂载计量、自动压缩与 checkpoint policy；Telemetry 默认 `FEEDBACK_ONLY`，可由环境覆盖。 | `sdk-minimal` 不应用 base；`FEEDBACK_ONLY` 不是 full live Trace；行存在仍需 activation trace。 | `SOURCE_CONFIRMED / RUNTIME_PENDING` |
| Headless overlay | `packages/bundle/headless/cordis.patch.yml`: header（1-5）、`headless-startup` / `headless-runner`（22-30） | `dsh-base` → headless overlay → one-shot runner | Headless 是直接叠加在 base 上的一次性 task mode，无 Host/Web/server。 | 这是组合定义；36-X01 必须保存 resolved composition。 | `SOURCE_CONFIRMED / RUNTIME_PENDING` |
| Listener order | `vendor/cordis/src/events.ts`: `waterfall`（224-242）、registration（254-301） | earlier wrapper → `next()` → later wrapper | Waterfall outermost-first；不调用 `next()` 会截断后续监听器。 | YAML 行序只给候选注册序；实际生效顺序需要 trace。 | `SOURCE_CONFIRMED / RUNTIME_PENDING` |

## 3. Hook Map

| Hook plane | Source：symbol / stable anchor | Caller → callee / call path | Observation | Counter-evidence / limitation | Status |
|---|---|---|---|---|---|
| Turn / pre-step | `packages/core/agent-loop/src/agent.ts`: `turn`（253-337）、`preStep`（232-250） | inbox claim → prompt assemble → context projection → `agent/pre-step` | `turn/start` 先写；pre-step 在新的 `step/start` 与 user message 前。Automatic compaction 与 checkpoint 可挂此处。 | listener 次序/short-circuit 需 trace。 | `SOURCE_CONFIRMED` |
| Request assembly | same: `step`（339-436）、`buildRequest`（442-541） | `deriveMessages` → `agent/request` → `prepareCall` → append header/context → request | `agent/request` 是请求前 route/envelope 改写点；header/context 先写 Session，再调用 stream。 | append 不等于 backend durable；flush wrapper 才是 dispatch barrier。 | `SOURCE_CONFIRMED` |
| Model checkpoint | `packages/session/session-checkpoint-policy/src/index.ts`: `afterCheckpoint`（29-37）、`apply`（63-82） | `llm/stream` waterfall → `session.flush()` → adapter | 活 Session 的完整 request 先 flush；失败不 dispatch。 | 不是 provider 接收回执，也不证明 billing。 | `SOURCE_CONFIRMED` |
| Model post | `packages/core/agent-loop/src/agent.ts`: `step`（362-425） | stream → chunk append → terminal message；error → `agent/request-error` → retry | usage 可随 chunk 进入，成功 message 可携最终 usage；取消只提交已有可见 prefix。 | adapter 可缺字段；失败 attempt 可无 final message。 | `SOURCE_CONFIRMED` |
| Tool scheduling | `packages/core/agent-loop/src/tool-calls.ts`: `executeToolCalls`（59-100）、`runGroup`（121-245） | tool-call blocks → append call → prepare → dispatch → ordered finalize → append result | Tool call 共享 Step signal；仅 dispatch/body 可重叠，policy/result/persistence 按模型序提交。 | 取消协作式；不响应 signal 的 body 仍可挂住。 | `SOURCE_CONFIRMED` |
| Tool pre | `packages/core/tools/src/index.ts`: `prepareExecution`（1462-1505） | prepare → `tools/pre-execute` → ask → guard | allow/deny/ask；deny/guard/cancel 可在 body 前产生 error result。 | pre policy 不是 side-effect sandbox；合并顺序依赖 composition。 | `SOURCE_CONFIRMED` |
| Tool around/body | same: `dispatchToolBody`（1531-1558）、`dispatchScheduledExecution`（1568-1597） | `tools/execute` → fused signal → body → normalized result | wrapper signal 与 caller signal 融合；body throw 归一化。 | signal 不能硬杀不协作代码；不是事务回滚。 | `SOURCE_CONFIRMED` |
| Tool post/result | same: `finalizeScheduledExecution`（1608-1619）、`finishScheduledExecution`（1630-1645）、`postExecute`（1741-1780） | result → `tools/post-execute` → materialize → observer → Session result | post 可 accept/block/替换 content 或 value；authoritative result 后 observer 只读。 | post-block 在 body 后，不能撤销副作用。 | `SOURCE_CONFIRMED` |
| Session append | `packages/core/session/src/index.ts`: `Session.append`（570-653） | validate/snapshot → seq/time → log push → synchronous observers | append 对内存 ledger 同步；surface replacement/source seq 同 invariant 校验。 | 不等于 backend durable。 | `SOURCE_CONFIRMED` |
| Durable write | `packages/session/session-persistence/src/coordinator.ts`: event wiring（1162-1214）、`flush`（1403-1414） | `session/event` → write-behind；`session/flush` → drain | 显式 flush 是 request/tool intent durability boundary。 | storage 成功不是 model/tool 外部世界状态，也非 exactly-once。 | `SOURCE_CONFIRMED` |

## 4. Usage 字段、来源、累计与 attempt 折叠

| Concern | Source：symbol / stable anchor | Caller → callee | Observation | Counter-evidence / limitation | Status |
|---|---|---|---|---|---|
| Vocabulary | `packages/llm/llm/src/types.ts`: `TokenUsage`（127-149）、stream events（356-376） | adapter → usage/finish | 一次 call 可报告 disjoint input、cache read/write、output、optional total/reasoning。 | optional 缺失不能补零；total 仅 provider 精确给出时权威。 | `SOURCE_CONFIRMED` |
| Durable accumulation | `packages/llm/token-meter/src/usage-projection.ts`: `tokenUsageProjectionDefinition`（110-157） | chunk/message/retry-started → projection | 同 attempt 的 chunk sample 被最终 message usage 替换；retry-started 关闭 replacement slot，下一 attempt 累加；compaction 不删除历史 usage。 | Token accounting 不是货币账单；缺 sample 不猜。 | `SOURCE_CONFIRMED` |
| Exact turn fold | `packages/llm/token-meter/src/turn-usage.ts`: `deriveTurnTokenUsage`（164-271） | closed turn → attempt boundaries → aggregate/undefined | 完整 lifecycle、边界与每 attempt usage 齐全才给 exact aggregate；retry attempt 分别计入。 | scheduled 未 started 不产生 attempt；缺失/冲突/溢出 fail closed。 | `SOURCE_CONFIRMED` |
| Context measurement | `packages/llm/token-meter/src/index.ts`: `measure`（129-170）、`surface-fold.ts`（78-118） | latest request anchor + reusable provider prompt usage + signed surface delta | usage 必须匹配当前 envelope 且不低于完整 heuristic；否则估算。Replacement 形成负 delta。 | 固定 heuristic 不是 provider billing truth；route/envelope change 使旧 anchor 失效。 | `SOURCE_CONFIRMED` |
| Context pressure | `usage-projection.ts`: pressure（159-225） | newest contextWindow + prompt-side usage + surface movement | numerator 为 input + cache read + cache write，不含 output；capacity 取最新 route。 | streaming 中沿用最近 sample；capacity 与 usage 非原子事实。 | `SOURCE_CONFIRMED` |
| Cost/billing | 对 `packages/**/src` 的 cost/currency/invoice 与 token/turn/step/latency budget owner 有界检索 | production inventory → no generic owner | 有 token accounting、per-request `maxTokens` 与局部上限；没有通用货币 Cost/Provider billing truth 或 Run/Turn/Step spend budget。 | 仅限 pinned revision/范围；不否认外部 provider 计费。 | `BOUNDED_ABSENCE` |

## 5. Budget policy、stop point 与 overshoot

| Finding | Source / anchor | Observation | Counter-evidence / limitation | Status |
|---|---|---|---|---|
| Generic budget owner | `packages/**/src` bounded search；`agent.ts` request `maxTokens`；`llm-retry/src/index.ts`；`compaction-basic/src/config.ts` | 仅有 request output cap、retry、context ratio、tool timeout/并发等局部 policy，没有统一预算 service。 | 局部 cap 不能合并成总预算；`maxTokens` 不是累计消费上限。 | `BOUNDED_ABSENCE` |
| Generic stop point | 同上 | 通用 owner 不存在，所以没有可归属的通用 spend stop point/overshoot guarantee。 | 不能改写成“窗口为零”或“无限”。 | `NOT_APPLICABLE / BOUNDED_ABSENCE` |
| Context stop point | `compaction-basic/src/index.ts`: pre-step listener（137-177）、`compactIfNeeded`（258-332） | pressure 在下一 pre-step 检查上一轮 usage/surface，并在新 Step 前压缩。它控制 context，不是成本。 | 当前 request 与同 Step retry 可在下次 pre-step 前继续产生 usage；summary 自身也可调用 model。 | `SOURCE_CONFIRMED` |
| Lag window | `agent.ts`: retry loop（339-436）；usage fold；compaction pre-step hook | context-pressure 信息/执行窗口覆盖当前已 dispatch attempt/同 Step recovery，直到下次 pre-step。 | 数值化费用 overshoot 为 `NOT_PROVABLE`。 | `INFERENCE_BOUNDARY` |

## 6. Compaction、History / Projection 与 request diff

| Concern | Source：symbol / stable anchor | Caller → callee | Observation | Counter-evidence / limitation | Status |
|---|---|---|---|---|---|
| Contract/trigger | `packages/compaction/compaction/src/index.ts`: trigger（24-25）、engine（96-172）；`types.ts`（19-118） | owner → compact APIs | Trigger 为 pressure/context-overflow；bracket 有 start/summary/end，summary 可带 route/model/maxTokens/usage/raw output。 | seam 存在不等于 target 激活。 | `SOURCE_CONFIRMED` |
| Automatic trigger | `compaction-basic/src/index.ts`: constructor/listeners（126-224）、`compactIfNeeded`（258-332） | pre-step pressure；request error overflow → forced compact → retry | `auto:false` 无监听；pressure 需 capacity，overflow 可绕阈值；overflow retry 独立 cap。 | 只处理 exact context-window error。 | `SOURCE_CONFIRMED` |
| Range/retention | `compaction-basic/src/region.ts`: select（100-135）、prepare（341-393） | priced surface → retain tail/balance tool pairs → snapshot | 压安全闭合 head，保留尾部；异步 summary 后重验。 | 不可分割 pair/open lifecycle/surface drift 会拒绝或重试。 | `SOURCE_CONFIRMED` |
| Summary request | `summarizer.ts`: input/result（73-108）、`summarizeWithLlm`（121-213） | selected surface + latest system/tools + instruction → `llm.stream(purpose='compaction')` | 独立 model request，signal 透传；仅成功 text 提交，usage/raw output 可记录。 | Mock/test 不等于真实 provider；summary 不是 Memory。 | `SOURCE_CONFIRMED / RUNTIME_PENDING` |
| Transaction/replacement | `region.ts`: `compactSurfaceRegion`（154-255）、commit（436-487） | start → summary → revalidate → summary event → replacement message → end | Raw append-only log 保留旧 events；current surface 用 summary replacement 影子化旧节点。失败写 error end。 | History 与 model Projection 不同；没有删除 ledger。 | `SOURCE_CONFIRMED` |
| Request diff | `Session.deriveMessages`（706-744）；`agent.ts buildRequest`（442-541） | replacement generation → next derive → next request | 压缩前取旧 surface；后续请求取 summary + retained tail，并重写 header/context。 | 具体 before/after diff 仍需 36-X01。 | `SOURCE_CONFIRMED / RUNTIME_PENDING` |
| Projection survival | token usage fold；surface fold；`session-projection/src/index.ts` restore（319-513） | durable events → fold；valid checkpoint+tail 或 full refold | 历史 token usage 保持；current-context 随 replacement 收缩；cache 非 authority。 | Projection 不是 raw History。 | `SOURCE_CONFIRMED` |

## 7. Cancellation、durable terminal、Resume 与外部副作用

| Concern | Source：symbol / stable anchor | Caller → callee | Observation | Counter-evidence / limitation | Status |
|---|---|---|---|---|---|
| Agent cancel | `agent.ts`: `cancel`（141-147）、`turn`（253-337）、`step`（339-436） | caller → phase AbortController → model/tool/summarizer | cooperative abort；turn finally 写 step/end/turn/end；visible prefix 可写 interrupted message。 | 无 durable cancel-request event；terminal 不证明远端停止。 | `SOURCE_CONFIRMED` |
| Tool cancel | `core/tools` signal fusion（1531-1558）；`tool-calls.ts` abort commit（237-288） | phase signal → body → await quiescence → aborted result | 未 dispatch 为 `ABORTED_BEFORE_DISPATCH`；started 后为 `ABORTED`；未开始 call 写 synthetic pair。 | 不协作 body 可挂；abort 不是 rollback。 | `SOURCE_CONFIRMED` |
| Crash unknown | `session-checkpoint-policy/tests/crash-recovery.e2e.ts`；`core/session/src/repair.ts`（89-132） | flushed tool intent → missing result → resume repair | repair 写 `TOOL_OUTCOME_UNKNOWN` 并要求先核验外部状态、不得盲重试，再闭合 step/turn。 | Owner test/rule，不是 exactly-once 或 compensation。 | `SOURCE_CONFIRMED` |
| Resume | `core/agent-loop/src/index.ts`: `resume` / `resumeWith`（654-710）；persistence coordinator（972-1043） | resume → load/validate/repair → fresh Agent publish | 从 durable Session 恢复，使用新 Agent/AbortController；turn/seq/projection 可延续。 | promise、旧 signal/socket、外部工具事务不会恢复。 | `SOURCE_CONFIRMED` |
| Projection restore | `session-projection/src/index.ts`（110-118, 319-513） | valid cache+tail or full log refold | cache 是可验证捷径，不匹配即 refold。 | telemetry cursor 是进程内状态。 | `SOURCE_CONFIRMED` |
| External boundary | tool intent flush + unknown repair + cooperative cancel | durable intent → uncertain outcome → explicit unknown | 无跨 provider/tool 事务日志、补偿或通用幂等键；`Cancel != Rollback`，`Resume != side-effect recovery`。 | 仅有界说明 pinned core。 | `SOURCE_CONFIRMED / BOUNDED_ABSENCE` |

## 8. Trace correlation 与 Telemetry

| Plane | Source：symbol / stable anchor | Correlation | Counter-evidence / limitation | Status |
|---|---|---|---|---|
| Session ledger | Agent/Tool/Retry/Compaction events | `session.id` + `event.seq`；常见 `turn/step`；Tool `callId`；Retry `retryId`/number；Compaction `compactionId`。 | 没有全 event 统一 `traceId/spanId/runId/attemptId`。 | `SOURCE_CONFIRMED` |
| Model/usage | request route + retry + usage owners | route/provider/model；retryId 界定 recovery chain；turn/step 界定 model Step。 | usage 不是 universal attempt object，边界靠 events fold。 | `SOURCE_CONFIRMED` |
| Telemetry capture | `session-telemetry/src/coordinator.ts`: capture（138-245）、identity（305-318） | records 至少含 session id、type、seq；body 保留域字段；assistant chunk 每 turn/step 只取首个。 | Resume 不回填旧生命周期失败 handoff；cursor 为 WeakMap。 | `SOURCE_CONFIRMED` |
| OTel backend | `session-telemetry-otel/src/index.ts`: log emit（221-252） | canonical record → OTel logs；FULL 或反馈触发。 | 不是 spans；base 默认 feedback-only；bounded shutdown 非交付保证。 | `SOURCE_CONFIRMED` |
| Universal trace owner | production `packages/**/src` 对 `traceId|spanId|runId|attemptId` 有界检索 | 未找到统一 owner。 | 不否认 Host/collector 外加字段。 | `BOUNDED_ABSENCE` |

## 9. Owner tests、activation path 与最小命令候选

本 Gate 未运行任何命令。

| Requirement | Existing owner test | Proves | Does not prove |
|---|---|---|---|
| Usage fold | `token-meter/tests/token-usage-projection.spec.ts`、`turn-usage.spec.ts` | attempt replace/retry accumulate/fail closed。 | 真实 provider usage/账单。 |
| Pressure loop | `compaction-basic/tests/compaction-loop-repro.spec.ts`: routed model、automatic pressure、independent retry budgets | 真实 Agent Loop owner 顺序。 | 完整长 Session→cancel→resume。 |
| Transaction | `compaction-basic.spec.ts`、`manual-compaction.spec.ts` | trigger/summary/replacement/failure/cancel/flush。 | Shipped target 实际激活。 |
| Checkpoint | `session-checkpoint-policy.spec.ts`、`crash-recovery.e2e.ts` | request/tool intent dispatch 前 durability 与 unknown repair。 | rollback/exactly-once。 |
| Cancel/resume | `agent-loop/tests/cancel.spec.ts`、`resume.spec.ts` | terminal ledger 与 fresh resume。 | 同一 Session integrated sequence。 |
| Telemetry | `session-telemetry/tests/telemetry.spec.ts`、`session-telemetry-otel/tests/otel.spec.ts` | record identity/log mapping/mode。 | 默认 full trace 或 collector receipt。 |
| Base activation | `compaction-basic/tests/loader-composition.spec.ts` | shipped YAML loader order。 | 用户 overlay 后 live target。 |

36-X01 Lab Engineer 应冻结独立临时 spec/fixture。Integrated focused command 候选（**未执行**）：

```powershell
pnpm exec vitest run <article-36-temporary-integrated-spec> --reporter=verbose
```

Owner-test preflight 候选（**未执行**）：

```powershell
pnpm exec vitest run packages/compaction/compaction-basic/tests/compaction-loop-repro.spec.ts packages/session/session-checkpoint-policy/tests/crash-recovery.e2e.ts packages/llm/token-meter/tests/token-usage-projection.spec.ts packages/core/agent-loop/tests/cancel.spec.ts packages/core/agent-loop/tests/resume.spec.ts --reporter=verbose
```

Preflight 不能替代 integrated 36-X01。目标 spec 必须用同一 resolved composition/Session id/可控 adapter-tool fixture，生成长 Session→usage→pressure compaction→before/after request diff→cancel→durable terminal→dispose→fresh resume 的连续 raw trace，并记录 command、exit code、stdout/stderr、environment、instrumentation diff 与限制。

## 10. Source Map disposition

- Hook、usage fold、compaction、projection/rehydration、cancel/resume 与 external-effect boundary 已在固定源码闭合。
- 通用 Cost/Run/Turn/Step spend Budget 与 universal Trace ID owner 保持 `BOUNDED_ABSENCE`，未发明能力。
- Base/Headless activation、真实 provider usage、实际 request diff 与复合 cancel/resume trace 保持 `RUNTIME_PENDING`。
- `SOURCE_MAP = PASS`；下一允许 Gate 为 `EXPERIMENT_DESIGN`。
