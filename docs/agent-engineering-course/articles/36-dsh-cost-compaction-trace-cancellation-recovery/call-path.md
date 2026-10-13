# Article 36 Call Path — Cost、Compaction、Trace、Cancellation 与 Recovery

## Evidence header

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Full commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Gate：`SOURCE_MAP`（source only；36-X01 未执行）

下列路径只证明 pinned source 控制流。凡是配置激活、provider usage、collector 交付、外部 side effect、真实 before/after request 均保留为 runtime evidence。

## 1. 一条 Step 的横切路径

```text
ReactLoopAgent.turn
  -> session.append(turn/start)
  -> preStep
       -> inbox.claim
       -> systemPrompt.assemble
       -> runtimeContext.project
       -> waterfall(agent/pre-step)
            -> compaction-basic pressure listener (if composed + auto)
                 -> TokenMeter.measure -> compactIfNeeded
            -> checkpoint-policy listener (if composed)
                 -> session.flush
            -> downstream next()
  -> session.append(step/start)
  -> session.append(user/message)
  -> step
       -> Session.deriveMessages(current surface)
       -> buildRequest
            -> effective header/context
            -> waterfall(agent/request)
            -> llm.prepareCall
            -> append request/header + request/context
            -> frozen LlmRequest
       -> ctx.llm.stream
            -> waterfall(llm/stream)
                 -> checkpoint: session.flush
                 -> adapter stream
       -> append assistant/chunk*
       -> assistant/message(usage?)
          OR agent/request-error -> retry in same Step
       -> executeToolCalls*
  -> session.append(step/end)
  -> waterfall(agent/turn-stopping)
  -> session.append(turn/end)
```

锚点：`packages/core/agent-loop/src/agent.ts` 的 `turn`（253-337）、`preStep`（232-250）、`step`（339-436）、`buildRequest`（442-541）；`packages/session/session-checkpoint-policy/src/index.ts` 的 `apply`（63-82）；`packages/compaction/compaction-basic/src/index.ts` 的 automatic listeners（137-224）。

边界：`agent/pre-step`、`agent/request` 与 `llm/stream` 是不同 seam。`vendor/cordis/src/events.ts` 的 waterfall contract（224-242）证明 outer listener 必须调用 `next()`；实际 target listener 次序/截断需 36-X01 trace。

## 2. Request 前 Hook 与 durability

```text
Session.deriveMessages()
  -> current surface (replacement-aware)
  -> effective request header/context
  -> waterfall(agent/request, draft)
  -> llm.prepareCall(route/model)
  -> append request/header
  -> append request/context
  -> frozen request { system, tools, messages, sessionId, signal, ... }
  -> waterfall(llm/stream)
       -> session.flush
       -> adapter downstream
```

| Stage | Source / anchor | Fact | Counter-evidence / limitation |
|---|---|---|---|
| Current messages | `packages/core/session/src/index.ts`: `deriveMessages`（706-744） | Raw ledger 与 current surface 分账；replacement 让旧节点退出 model projection。 | 无法从源码给某 Session 的实际 diff。 |
| Request hook | `agent.ts`: `buildRequest`（442-541） | `agent/request` 在最终 route/header/context 写入前改 draft。 | seam 存在不证明插件注册/返回值。 |
| Durable record | same: header/context append（505-530） | 每个 dispatch 候选记录 effective envelope/context。 | `Session.append` 先只是内存 ledger。 |
| Pre-dispatch flush | `session-checkpoint-policy/src/index.ts`: `afterCheckpoint` / `apply`（29-82） | 活 Session 在 adapter 前 flush；reject 不 dispatch。 | storage durability 不是 provider receipt。 |

## 3. Model 后、usage 与 retry attempt

```text
adapter stream
  -> assistant/chunk(..., usage?)
       -> tokenUsage projection opens/replaces attempt sample
  -> finish
       success:
         -> assistant/message(..., usage?)
         -> final usage replaces same-attempt chunk sample
       error / aborted:
         -> waterfall(agent/request-error)
              -> llm-retry.recover
                   -> append llm/retry(retryId, number, delay)
                   -> cancellable backoff
                   -> append llm/retry-started
                   -> close prior replacement slot
                   -> retry same Step / new attempt
  -> step/end -> deriveTurnTokenUsage(closed window)
```

| Fact | Source / anchor | Observation | Limitation |
|---|---|---|---|
| Fields | `packages/llm/llm/src/types.ts`: `TokenUsage`（127-149） | input/cache read/cache write/output/optional total/reasoning 属一次 call。 | 缺字段不能补零；usage 不等于费用。 |
| Same-attempt replace | `packages/llm/token-meter/src/usage-projection.ts`（110-157） | chunk early sample 与 final message 属同 attempt 时 replacement。 | 只有 chunk 时会保留 sample，但不等于最终 bill。 |
| Retry accumulate | same；`packages/llm/llm-retry/src/index.ts`: `recover`（111-207） | `llm/retry-started` 分隔 attempt；started attempt usage 累加。 | scheduled 未 started 不产生 attempt。 |
| Exact turn | `packages/llm/token-meter/src/turn-usage.ts`: `deriveTurnTokenUsage`（164-271） | 完整 lifecycle + 全 attempt usage 才有 exact aggregate。 | 缺失/冲突/未闭合返回 `undefined`。 |

`request-error` 可在**同一 Step**发起下一 attempt，context pressure 则到下一次 `agent/pre-step` 才重检。这是 context-protection 的源码可见 lag，不是费用预算承诺。

## 4. Token measurement 与“Cost”边界

```text
latest successful request anchor
  + reusable provider prompt usage
  + signed surface delta
  -> TokenMeter.measure

newest request/context.contextWindow
  + newest prompt-side usage
  + later surface delta
  -> contextPressure
  -> threshold check at next pre-step
```

锚点：`packages/llm/token-meter/src/index.ts` 的 `measure`（129-170）、`usage-projection.ts` pressure（159-225）、`surface-fold.ts`（78-118）、`estimate.ts` fixed heuristic。

- usage 只在匹配最新成功 request 且不低于完整 heuristic envelope 时复用，否则估算；
- compaction replacement 缩小 current-context 估计，但不抹掉历史 tokenUsage；
- pinned production source 未找到通用货币 Cost ledger、invoice、Run/Turn/Step spend budget 或统一 stop controller；
- `maxTokens` 只是每 request 输出 cap；retry、threshold、timeout 是不同局部 policy；
- 所以 generic budget stop point 与 cost overshoot 数值是 `NOT_APPLICABLE / NOT_PROVABLE`，不能伪造。

## 5. Compaction：trigger → summary → replacement → next request

### 5.1 Pressure 与 overflow

```text
agent/pre-step
  -> compactIfNeeded(trigger=pressure)
       -> latest durable route/model
       -> contextWindow + threshold/retain
       -> TokenMeter.measure
       -> below threshold: no-op
       -> above: optional prune -> select head/retain tail -> compact

agent/request-error(CONTEXT_WINDOW_EXCEEDED)
  -> compactIfNeeded(trigger=context-overflow)
       -> bypass pressure threshold/capacity requirement
       -> force compactable region
  -> surface generation advanced + signal live + retry cap
       -> retry same Step
```

锚点：`packages/compaction/compaction-basic/src/index.ts` 的 listeners（137-224）与 `compactIfNeeded`（258-332）。只有 exact context-window error 进入此恢复；overflow cap 与 llm-retry cap 独立；取消不重试。

### 5.2 Summary transaction

```text
selected current-surface region
  -> append compaction/start(compactionId, trigger, range)
  -> snapshot/pricing/latest envelope
  -> build summary input:
       latest system + tools
       selected current messages
       final compaction instruction
  -> llm.stream(purpose='compaction', signal)
  -> require successful shorter text
  -> revalidate region/generation/pricing
  -> append compaction/summary(usage/rawOutput/route/...)
  -> append user/message(surfaceOp=replace, sourceEventSeqs=old nodes)
  -> append compaction/end(success)

failure/cancel after start
  -> append compaction/end(error)
  -> no replacement
```

锚点：`packages/compaction/compaction-basic/src/region.ts` 的 `compactSurfaceRegion`（154-255）、prepare/summarize（341-393）、commit（436-487）；`summarizer.ts`（73-108, 121-213）。Summary 是单独 model call、signal 透传；它不是 Memory，也不证明真实 provider 已执行。

### 5.3 Before / after request

```text
BEFORE
  raw ledger: E1 ... En
  current surface: M1 ... Mk | retained tail
  request.messages = M1 ... Mk | retained tail

COMMIT
  raw ledger appends start + summary + replacement + end
  current surface becomes Summary(M1..Mk) | retained tail

AFTER (next request)
  deriveMessages -> Summary(M1..Mk) | retained tail
  append new request/header + request/context
  dispatch new messages
```

原始 M1..Mk 未删除，只退出 current surface。实际 message/token/字段 diff 必须由 36-X01 保存。

## 6. Cancellation propagation 与 durable terminal

```text
ReactLoopAgent.cancel(reason, {keepInbox?})
  -> optional inbox.clear
  -> active phase AbortController.abort(reason)
       -> model signal
       -> retry backoff signal
       -> compaction summarizer signal
       -> shared tool signal
            -> fuse caller + wrapper signal
            -> body must cooperate
  -> cancel convergence
       -> optional interrupted assistant prefix
       -> tool result pairs as applicable
       -> step/end
       -> turn/end(reason=aborted)
  -> idle
```

| Boundary | Source / anchor | Fact | Counter-evidence |
|---|---|---|---|
| Agent abort | `agent.ts`: `cancel`（141-147）、turn/step（253-436） | 同一 phase signal 向 model/tool/recovery 传播并闭合 lifecycle。 | 无 durable cancel-request；terminal 前 crash 只能 repair。 |
| Tool quiescence | `packages/core/tools/src/index.ts`: `dispatchToolBody`（1531-1558） | started body 被 await；signal 后成功 outcome 规范化 aborted。 | 无 hard kill；副作用可能已发生。 |
| Skipped calls | `packages/core/agent-loop/src/tool-calls.ts`（237-288） | 未开始 calls 写 paired synthetic aborted result。 | 不代表 started 外部 call 已停止。 |
| Durable terminal | `agent.ts`: `turn` finally（299-329） | step/end 与 turn/end 闭合 normal/error/abort。 | durable 仍依赖 persistence/flush。 |

所以 `cancellation request != side effect stopped`，`Cancel != Rollback`。

## 7. Session write、crash repair 与 Resume

```text
Session.append
  -> validate/snapshot -> seq/time -> in-memory log push
  -> synchronous session/event observers
       -> persistence write-behind
       -> projection fold
       -> telemetry capture (if active)
  -> explicit session.flush at request/tool/pre-step boundaries
       -> backend drain
```

锚点：`packages/core/session/src/index.ts` 的 `append`（570-653）；`packages/session/session-persistence/src/coordinator.ts` 的 event wiring（1162-1214）与 `flush`（1403-1414）。

```text
flushed tool/call intent
  -> external body starts
  -> crash before tool/result
  -> persistence.prepare(sessionId)
       -> interruptedTurnClosers
            -> tool/result(error=TOOL_OUTCOME_UNKNOWN)
            -> step/end(interrupted)
            -> turn/end(interrupted)
```

`packages/core/session/src/repair.ts`（89-132）要求恢复后先核验外部状态、不得盲重试；这不是 compensation/exactly-once。

```text
AgentLoop.resume(sessionId)
  -> fuse caller/owner/factory signals
  -> persistence.prepare -> load/validate/repair -> fresh Session
  -> setupAndPublish(source='resume')
       -> fresh ReactLoopAgent + fresh AbortController
       -> publish Session/Agent

projection restore
  -> valid version+seq checkpoint + durable tail
  -> otherwise full refold from raw log
```

锚点：`packages/core/agent-loop/src/index.ts`（654-710）；persistence coordinator（972-1043）；`packages/session/session-projection/src/index.ts`（319-513）。可恢复 durable History、turn/step/seq、surface 与 projections；不可恢复旧 signal/promise/socket、telemetry cursor、外部工具事务/outcome。所以 `Resume != external-side-effect recovery`。

## 8. Trace correlation

```text
session.id + event.seq
  + turn / step                   Agent/Model lifecycle
  + callId                        Tool call/result
  + retryId + retry number        retry chain / attempt boundary
  + compactionId                  compaction bracket
  + provider/model route          model route
  + usage                         token observation

session/event
  -> SessionTelemetryCoordinator.captureEvent
       -> attributes(session id, type, seq, identity)
       -> structured body(domain fields)
       -> redaction waterfall
       -> backend.emit -> OTel LogRecord
```

锚点：`packages/session/session-telemetry/src/coordinator.ts`（138-245, 305-318）；`packages/session/session-telemetry-otel/src/index.ts` log emit（221-252）。

- Session ledger 是本地时序 backbone，但不是 OTel Trace；
- production source 对 `traceId|spanId|runId|attemptId` 有界检索无统一 owner；
- OTel backend 导出 logs，不是 spans；base 默认 `FEEDBACK_ONLY`；
- assistant stream 每 turn/step 只捕获首 chunk，handoff cursor 是进程内状态；
- 因而各域可由 Session id/seq + domain id 本地关联，但不能声称已有统一分布式 Trace 或 collector delivery guarantee。

## 9. 36-X01 activation path（设计输入，不执行）

已有 owner tests 分别证明组件语义，但没有一条现成测试覆盖完整复合路径。后续临时 spec 至少应组装：

```text
resolved dsh-base composition
  -> persistence + projection registry
  -> token-meter
  -> compaction-basic(auto=true)
  -> session-checkpoint-policy
  -> deterministic scripted adapter with provider-style usage
  -> cooperative instrumented tool with external-effect marker
  -> AgentLoop create(Session S)
       -> long turns exceed pressure threshold
       -> capture BEFORE request
       -> automatic compaction + summary usage
       -> capture AFTER request + exact diff
       -> start request/tool and cancel shared signal
       -> await idle; durable terminal
  -> flush + dispose live owners
  -> new runtime / AgentLoop.resume(S)
       -> assert history/surface/usage rehydration
       -> do not claim external side effect rollback/recovery
```

Focused candidate（**未执行**）：

```powershell
pnpm exec vitest run <article-36-temporary-integrated-spec> --reporter=verbose
```

Owner-test preflight candidate（**未执行**）：

```powershell
pnpm exec vitest run packages/compaction/compaction-basic/tests/compaction-loop-repro.spec.ts packages/session/session-checkpoint-policy/tests/crash-recovery.e2e.ts packages/llm/token-meter/tests/token-usage-projection.spec.ts packages/core/agent-loop/tests/cancel.spec.ts packages/core/agent-loop/tests/resume.spec.ts --reporter=verbose
```

这不能替代 integrated 36-X01。本 Gate 未运行命令，也未修改其 hypothesis/acceptance/safety boundary。

## 10. Call-path disposition

- Request 前、Model 后、Tool 前后、Session write 静态路径已闭合。
- Usage replacement/retry accumulation/exact fold 已闭合。
- Compaction trigger/summary/History/Projection/next request/rehydration 已闭合。
- Cancel propagation/durable terminal/unknown outcome/fresh resume 已闭合。
- 通用 spend Budget 与 universal Trace owner 保持有界不存在；目标 composition/runtime 保持 `RUNTIME_PENDING`。
- `SOURCE_MAP = PASS`；`NEXT_ALLOWED_GATE = EXPERIMENT_DESIGN`。
