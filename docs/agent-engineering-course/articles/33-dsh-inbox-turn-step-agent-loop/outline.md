# Article 33 Outline｜Loop、Turn 与 Step：AgentLoop 怎样推进一次运行

Status: `AUTHOR OUTLINE / REVIEW PENDING`

## Article identity

- Type：原理篇。
- Series position：承接 Article 32 的 request assembly 边界，向下进入一次 Agent Run 的执行主链；Article 34 才处理 Replay/Fork，Article 35 才展开完整 Tool pipeline，Article 36 才处理 Cost/Compaction/Recovery。
- Core problem：一次 Agent Run 为什么不能压成“一次模型回复”，Host input、Inbox、Turn、Step、Tool Batch 与 termination 分别由谁拥有。
- Shortest claim：Agent Loop 的可靠性不来自一个 `while`，而来自每层都有可持久化边界、明确的继续/停止所有者，以及贯穿 active Turn 的 cancellation spine。
- Evidence posture：15 Claims / 15 Cards；14 CONFIRMED / 1 PROPOSAL / 0 BLOCKED；4/4 required Trace；10/10 selected owner tests；2/2 read-only observations。

## Navigation

- Previous：Article 32 published relref。
- Course index：Agent Engineering series index relref。
- Next：Article 34 只写计划中文本，不创建 future relref。

## Opening｜“模型回复一次”为什么是错误的运行单位

- 从 X02 开场：一个 Turn、两个 Steps、两个 requests、一个 tool call/result pair。
- 提问：若“一次回复=一次运行”，Tool result 回到第二次 request 属于哪里？
- 立四个反等同边界：Inbox != Chat UI；Turn != Step；Tool Batch != Multi-Agent；Stop != Success。
- 固定 source/tag/commit、fixture cleanliness 与 runtime scope。
- 明确 MockAdapter、owner fixture、无 Provider/network/billing/真实副作用。

## 1. 问题空间｜一个 `done` 会吞掉哪些工程事实

- Host delivery 与 runtime queue ownership。
- Turn durable interval 与 Step request attempt。
- Tool success、Turn completed 与业务目标成功不是一回事。
- cancel 是 cooperative stop，不是 rollback。
- 说明为什么 API-first/while-loop-first 都解释不了 replay、observability 与 recovery。

## 2. 抽象模型｜Host → Inbox → Turn → Step → Tool Batch

- Host：产生 user intent 和 delivery mode。
- Inbox/event：durable splice + live projection，target+wakeup 分账。
- Turn：一次可持久化推进区间，可含零个或多个 Step。
- Step：一次 model request、stream/parse、assistant anchor 与本 response tool batch。
- Tool Batch：同一 assistant message 的 ordered calls scheduler。
- Termination receipt：typed reason，不用 boolean。
- 画主链文本图。

## 3. 具体实现一｜两个 Host 怎样汇合到 Inbox

- Browser `SessionCommands.prompt` 路径。
- Headless `run` 路径。
- 汇合到 `Agent.followup/steer/inject -> send -> Inbox.splice -> wakeDriver`。
- `followup` / `steer` / `inject` 的 target+wakeup 差异。
- durable `agent/inbox/spliced` 与 live notifications 分账。
- Source links 指向 pinned public GitHub blob。

## 4. 具体实现二｜Turn 为什么不是 Step 的别名

- `wakeDriver -> kick -> turn`。
- `turn/start` 先于 first claim。
- first proposal empty 或 rejected 可产生 zero-Step Turn。
- admitted proposal 才产生 `step/start`。
- `step/end` / `turn/end` 必须平衡。
- X01 one-Turn/one-Step positive control。

## 5. 具体实现三｜一个 Step 如何闭合 request 生命周期

- `preStep`：claim、assembly、dynamic context、waterfall。
- admitted user messages durability。
- `step`：render system、derive history、build/freeze request。
- LLM stream：adapter selection/dispatch/iteration；BlockAssembler parse。
- `assistant/chunk* -> assistant/message` durable anchor。
- optional tool batch；finally `step/end`。
- 强调 real Provider wire 未验证。

## 6. Trace X01｜No-tool natural close

- 1 request / 1 Turn / 1 Step / 0 tool events。
- ordered seq 与 balanced boundaries。
- `turn/end(completed)` 只证明 fixture loop reason。
- 不覆盖 zero-Step/policy/cancel/real Provider。

## 7. Trace X02｜Tool result 为什么产生下一 Step

- Step 1 assistant tool call `c1`。
- call/result durable linkage。
- Step 2 request 精确包含 tool result。
- 同一 Turn 内 2 Steps，而不是新 Turn。
- Tool success 不是 task-success oracle。

## 8. Tool Batch｜并发执行与有序提交必须分开

- `executeToolCalls -> runGroup -> fillPool -> commitReady`。
- only `parallel` may overlap；exclusive is barrier。
- ordered prepare/post/result/additional context。
- cap=2 rolling pool。
- settlement order != durable commit order。
- Tool Batch 不创建 Agent。

## 9. Trace X03｜Overlap、barrier 与 model-order aggregation

- parallel `c1,c2` overlap；settlement 可反序。
- result/context/history 仍按 `c1,c2` commit。
- `parallel -> exclusive -> parallel` 无 overlap。
- cap 不超 2 且实际到 2。
- 不证明任意 Tool thread safety、外部 side-effect order 或 Multi-Agent。

## 10. Continue / Stop｜谁拥有下一步

- no-tool -> completed candidate。
- max-tokens -> sticky non-success reason。
- tool debt without concludesTurn -> another Step。
- successful `concludesTurn` -> completed candidate。
- queued next-step Inbox / `agent/turn-stopping` 可继续。
- `agent/pre-step` reject -> blocked without opened Step。
- Stop 是 runtime interval close，不是 business success。

## 11. Policy、Budget、Error 与 Cancellation

- Policy denial：canonical isError Tool result，通常回模型；不是 Turn error oracle。
- Budget：`maxTokens` per-request output cap；`maxParallelToolCalls` concurrency cap；Tool `timeoutMs` result-scoped；pinned source 无 generic Turn/Step/cost budget。
- Error：terminal model error 给 `agent/request-error` retry owner；otherwise structured Turn error；extension failure separate path。
- Cancellation：single active AbortSignal through assembly/pre-step/request/stream/tool；new Turn gets fresh controller。

## 12. Trace X04｜Cancel 是 cooperative drain，不是 rollback

- cancellation stops replenishment。
- started calls drain；unstarted bodies stay zero。
- synthetic `ABORTED_BEFORE_DISPATCH` balances replay。
- visible stream prefix retained with `interrupted=true`。
- current Turn `aborted(user)`；later Turn fresh and completed。
- 不证明 process kill、remote cancel 或 external rollback。

## 13. 四个不得等同的边界

- Inbox != Chat UI：Browser/Headless counterexample。
- Turn != Step：zero/multi-step counterexample。
- Tool Batch != Multi-Agent：no agent creation/delegation/handoff。
- Stop != Success：blocked/max-tokens/aborted/error are all terminal。
- 补充 Cancel != Rollback，作为第五个工程警戒而不改题面四边界。

## 14. 一个更稳的工程骨架

- Event ledger：InboxReceipt / TurnReceipt / StepReceipt / ToolBatchReceipt。
- 状态机：IDLE -> RUNNING(Turn) -> Step* -> typed TurnEnd -> IDLE。
- continue decision reads debts and typed outcomes，不读单一 `done`。
- cancellation context scoped per Turn。
- receipt 保留 source/runtime/mock/absence level。

## 15. BuildPilot implication｜仅 Proposal

- Candidate `TurnReceipt`、`StepReceipt`、`TerminationReceipt`、single CancellationContext。
- 不照搬 DSH plugin/event model。
- acceptance candidates：zero-Step、two-Step tool roundtrip、barrier/model-order commit、cancel balancing。
- 明确没有 ADR、implementation、runtime、migration；Part VII NOT STARTED。

## 16. 动手验证

- 冻结 source identity。
- X01—X04 deterministic fixture recipe。
- expected/observed 分栏。
- 保存 event seq、request count、callId、start/settle/commit、turn reason、signal identity。
- 先 owner tests，再可选 read-only observation。
- 失败命令也保留 receipt。
- 最终 fixture clean recheck。

## 17. Evidence Boundary

- Source proves ownership/call paths only。
- Runtime owner fixtures prove selected MockAdapter/in-memory behaviors only。
- Mock receipt does not prove real Provider/network/model/billing。
- Bounded absence says no generic budget in pinned production search，不是永恒不存在。
- Proposal remains proposal。

## 18. Claim mapping and learning check

- 15-row compact mapping。
- 18—20 questions，覆盖主链、四 Trace、四边界、limits、BuildPilot proposal。

## 19. Series handoff and shortest conclusion

- Article 34 Replay/Fork not pre-answered。
- Article 35 full Tool pipeline、Article 36 Cost/Compaction/Recovery、Article 37 extension mapping 保持 owner。
- Article 38 / Part VII NOT STARTED。
- Final sentence：不要用一次 model reply 或一个 done 描述 Agent Run；记录 durable Turn/Step、ordered Tool receipts 与 typed termination。
