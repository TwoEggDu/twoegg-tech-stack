# Article 33 Research

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## 1. Research boundary

本篇是原理型源码追踪文。工程问题不是“DSH 有一个 while loop”，而是：Host 输入如何获得可追踪的 Inbox receipt，Runtime 如何把一次 Turn 拆成若干 Step，Tool batch 怎样回到下一次模型请求，以及停止、成功、失败与取消分别由谁裁决。

固定研究对象：

- Repository：`https://github.com/deepseek-ai/deepseek-harness`
- Tag：`dsh-v0.1.2-alpha.1`
- Commit：`cd5ef8148158c3a752a658978873241fdf8e2bbc`
- External fixture：`C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`
- Read at：`2026-08-30 / Asia/Shanghai`
- Identity observation：`HEAD` 与 exact tag 相符，`git status --short` 无输出。

证据边界：

- `repository-map.md` 与 `call-path.md` 已闭合 Browser/Headless Host ingress、Inbox、Turn/Step、Tool scheduler、停止、错误与取消的 production symbols/call paths。
- `experiments/agent-loop-four-traces.md` 已保存四条 required Trace；expected observable 与 observed result 仍分栏保留。
- Trace 使用 production `AgentLoop` 与 repo-owned in-memory `MockAdapter` / deterministic tools，不使用真实 Provider、credential、network 或生产资料。
- Inbox 不等于 Chat UI；Turn 不等于 Step；Tool Batch 不等于 Multi-Agent；Stop 不等于 Success。
- 本篇不展开 Article 34 的 Replay/Fork、Article 35 的完整 Tool pipeline、Article 36 的 Cost/Compaction/Recovery，也不启动 Part VII。

## 2. Research Questions

| RQ | Question | Preliminary answer / required closure |
|---|---|---|
| `33-RQ01` | Host 写入 Inbox/event 的入口在哪里？ | Browser `SessionCommands.prompt` 与 Headless `run` 都经 `Agent.followup/steer -> send -> Inbox.splice` 汇合；Inbox 是 Host-neutral runtime queue。 |
| `33-RQ02` | Runtime 如何形成 Turn？ | `wakeDriver -> kick -> turn` 先写 `turn/start` 再 claim，可经历零或多个 Step，最终以 typed reason 写 `turn/end`。 |
| `33-RQ03` | Step 的 assembly、model call、parse 与 event 生命周期是什么？ | `preStep -> step/start -> user/message -> buildRequest -> stream/BlockAssembler -> assistant/message -> tool batch? -> step/end` 已由 source path 与 X01/X02 闭合。 |
| `33-RQ04` | Tool batch 并发、顺序和汇总语义是什么？ | exclusive call 是 barrier；parallel-safe dispatch 可在 cap 内重叠，但 prepare/post、result 与 additional context 按 model order commit。X03 已复现。 |
| `33-RQ05` | Continue / Stop 的决策权在哪里？ | 无 tool call、`max-tokens`、tool `concludesTurn`、next-step inbox 与 `agent/turn-stopping` 共同影响边界；不能压成一个 `done`。 |
| `33-RQ06` | Policy、Budget、Error 怎样影响停止？ | Policy 先形成 Tool outcome；`maxTokens` 是 request cap，pinned source 无 generic Turn/Step/cost budget；request error 仅在 listener 显式返回 retry 时重试，否则关闭当前 Turn。 |
| `33-RQ07` | Cancellation signal 怎样穿过 Loop？ | active phase 的同一 signal 进入 assembly、pre-step、request、LLM 与 tool scheduler；X04 观察到 cooperative drain、synthetic skipped result、typed aborted reason 与 fresh next-Turn signal。 |
| `33-RQ08` | 四条 Trace 分别证明什么？ | X01 自然闭合；X02 跨 Step 回送；X03 overlap/barrier/model-order commit；X04 signal/abort 与 durable balancing。均只证明 MockAdapter/owner-fixture scope。 |

`33-RQ01—08` 已由 pinned source 与对应 Trace 回答；real Provider/network/billing/外部副作用 rollback 仍不在证明范围。

## 3. Claim Register

| Claim ID | Preliminary claim | Class | Status | Required closure |
|---|---|---|---|---|
| `33-C01` | 全篇绑定同一 clean pinned revision。 | `PINNED_SOURCE` | `CONFIRMED` | identity only |
| `33-C02` | 公共 Agent seam 把 Host delivery 路由到 `next-turn/next-step` Inbox，并用 durable splice receipt 与 live inbox events 分账。 | `PINNED_SOURCE` | `CONFIRMED` | Browser + Headless paths closed |
| `33-C03` | Turn 在首次 claim 前打开，可包含零或多个 Step，并以 typed reason 关闭。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | source + X01 |
| `33-C04` | Step 闭合 assembly、entered messages、model stream/parse、assistant anchor、optional tool batch 与 durable end。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | source + X01/X02 |
| `33-C05` | no-tool response 在一个 Step 后自然结束 Turn，且没有 tool events。 | `EXPERIMENT` | `CONFIRMED` | X01 fixture scope |
| `33-C06` | single-tool response 产生 call/result，并在同一 Turn 的下一 Step 请求中回送结果后完成。 | `EXPERIMENT` | `CONFIRMED` | X02 fixture scope |
| `33-C07` | parallel-safe siblings 可有界重叠；exclusive call 是 barrier，而不是所有 Tool 一律并发。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | source + X03 |
| `33-C08` | Tool dispatch 可乱序完成，但 durable results 与 additional contexts 按 model call order 提交。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | source + X03 |
| `33-C09` | Continue/Stop 由 tool debt、next-step Inbox、`concludesTurn` 与 `agent/turn-stopping` 数据共同决定；Stop 不推出 Success。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | source + four traces |
| `33-C10` | Policy denial 形成一个 tool error outcome，但不自动等同 Turn error 或成功。 | `PINNED_SOURCE` | `CONFIRMED` | Tool pipeline path closed |
| `33-C11` | `maxTokens` 是 per-request output cap；`max-tokens` 可成为 sticky Turn reason；当前 loop 没有 built-in turn budget。 | `OFFICIAL_DOC + PINNED_SOURCE` | `CONFIRMED` | source + bounded absence search |
| `33-C12` | terminal model failure 可由 `agent/request-error` retry；未处理模型错误和 extension failure 以不同路径关闭当前 Turn，loop 仍可服务后续 Turn。 | `PINNED_SOURCE` | `CONFIRMED` | error path closed |
| `33-C13` | active Turn 的同一 cancellation signal 进入 assembly、pre-step、request、stream 与 tool execution，abort 形成 typed `turn/end(aborted)`。 | `PINNED_SOURCE + EXPERIMENT` | `CONFIRMED` | source + X04 |
| `33-C14` | cancellation 可保留已展示 stream prefix、drain started calls，并为未 dispatch calls 写 synthetic aborted result；这不是 rollback。 | `EXPERIMENT` | `CONFIRMED` | X04 fixture scope |
| `33-C15` | BuildPilot 可候选采用显式 `Turn/Step/TerminationReceipt` 与单一 cancellation spine，但当前未实现。 | `DESIGN_PROPOSAL` | `PROPOSAL` | Part VII only |

Final distribution：`14 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。

## 4. Counter-evidence and terminology guards

- `turn/start -> turn/end(completed)` 只能证明 loop 的 durable reason；Tool result 仍可 `isError=true`，不能据此宣布任务成功。
- first claim 被 reject 或改为空时可出现 `Turn=1 / Step=0`；因此 Turn 不是 Step 的别名。
- `followup/steer/inject` 都进 Inbox，但 target 与 wakeup 不同；不能把 Inbox 写成 Chat UI 队列。
- parallel-safe calls 只证明同一 Tool batch 内可能 overlap；没有 Agent creation、delegation 或 handoff，不是 Multi-Agent。
- Policy denial 是 Tool pipeline 的结果之一；它是否继续进入下一模型 Step 由 Loop 与 inbox debt 决定。
- `maxTokens` 与 `maxParallelToolCalls` 都不是总 token/cost/turn budget。
- `agent.cancel()` 是 cooperative abort；started side effect 可能已经发生，synthetic result 只补平 replay，不回滚外部世界。
- MockAdapter/runtime fixture 只证明 selected in-memory path，不证明 DeepSeek HTTP wire、真实模型输出或生产并发安全。

## 5. Frozen required experiment design

Lab Dependency: `REQUIRED`

本节是 Article-specific source Trace plan，不创建 `Lab 07` 或第二套课程 Lab schema。Lab Engineer 不得修改 hypothesis、falsifier 或 acceptance 来适配结果。

### Common fixture and safety

- Fixture：clean pinned checkout；production services `LlmRuntime -> SessionStore -> SystemPrompt -> ToolRuntime -> AgentRegistry -> AgentLoop`；repo-owned `MockAdapter`；deterministic in-memory tools。
- Environment：Windows 10 `10.0.19045` x64；Node `24.18.1`；pnpm `11.7.0`；PowerShell `7.6.4`。
- Variables：adapter response script、tool execution mode、settlement gates、cancel injection point；其余 config 固定，`maxParallelToolCalls=2`。
- Trace fields：ordered Session event types/seq/turn/step/callId；normalized request count/messages；tool start/settle/commit order；turn reasons；signal identity/aborted state；exit code/test counts。
- Commands / execution needs：实际先记录了 host-global `pnpm exec` PATH failure，随后直接使用 workspace-local `node_modules/.bin/vitest.CMD` 运行 owner tests；两个 isolated read-only observation 复用 production services。完整 receipt 保存于 `experiments/agent-loop-four-traces.md`。
- Safety：无 credential、network、server、真实 FS/command Tool、生产数据或费用；不修改 pinned source。若临时 instrumentation 必需，保存 exact diff 并标记非 pristine behavior。

### `33-X01`｜No-tool trace

- Related Claims：`33-C03`、`33-C04`、`33-C05`。
- Research Question：文本完成且没有 tool-call 时，Turn/Step 怎样关闭？
- Hypothesis：一次 waking followup 形成一个 Turn、一个 Step、一个 request 和一个 assistant anchor；无 tool events；最终 `turn/end(completed)`。
- What Would Falsify It：出现第二 Step、任意 tool event、缺失 balanced boundaries，或 reason 非 completed。
- Inputs：user `no-tool`; MockAdapter 返回确定性 text + normal finish。
- Expected Observable：`inbox receipt -> turn/start -> step/start -> user/message -> request/header -> assistant/chunk* -> assistant/message -> step/end -> turn/end(completed)`。
- Fault Injection：无；本例是最小 positive control。
- Acceptance：事件 seq 严格递增；`turn=1/step=1` 成对；request count `1`；tool call/result count `0`；agent 回到 idle。
- Evidence Mapping：满足后可把 `33-C05` 升为 fixture-scoped `CONFIRMED`。
- Observed Result：`PASS`；`1 request / 1 Turn / 1 Step / 0 tool events / turn-end(completed) / idle`，事件 seq 严格递增且边界平衡。
- Limitations：不证明空 first claim、policy、tool 或 cancellation 分支。

### `33-X02`｜Single-tool trace

- Related Claims：`33-C04`、`33-C06`、`33-C09`。
- Research Question：单个 tool-call 怎样在同一 Turn 内触发下一 Step？
- Hypothesis：Step 1 记录 assistant tool-call、单个 call/result；Step 2 request history 含配对 result，随后 no-tool response 完成；Turn 只有一对 start/end。
- What Would Falsify It：结果未进入第二 request、call/result 失配、另开 Turn、或 Tool success 被直接当作 Turn success。
- Inputs：first response 调用 deterministic `echo`; tool 返回 `echo-ok`; second response 返回 final text。
- Expected Observable：`1 Turn / 2 Steps / 2 requests / 1 call-result pair`，第二 request 可按 callId 追到结果。
- Fault Injection：tool 本体无故障；此例验证 happy path 的跨 Step debt。
- Acceptance：call/result `callId` 一致；result source 引用 call seq；Step boundaries balanced；final reason completed。
- Evidence Mapping：满足后可把 `33-C06` 升为 fixture-scoped `CONFIRMED`。
- Observed Result：`PASS`；`1 Turn / 2 Steps / 2 requests / 1 linked call-result pair`，第二 request 精确包含 `c1` 的 `echo: ping` result。
- Limitations：单 Tool 不证明并发、barrier 或 policy。

### `33-X03`｜Multi-tool ordering trace

- Related Claims：`33-C07`、`33-C08`、`33-C09`。
- Research Question：同一步多个 Tool 怎样并发、过 barrier 并按模型顺序汇总？
- Hypothesis：两个 parallel-safe calls 在 cap 内重叠并按反序 settle，但 result/context 按 model order commit；中间 exclusive call 形成 barrier，不与两侧 group overlap。
- What Would Falsify It：超过 cap、exclusive overlap、result/context 采用 settlement order、call/result 配对丢失，或未 drain started calls。
- Inputs：model order `p1,p2,exclusive,p3`; deferred gates 强制 `p2` 先 settle；每个结果带唯一 text/additionalContext。
- Expected Observable：start/settle 可证明 overlap 与反序完成；durable result/context order 仍为 `p1,p2,exclusive,p3`；下一 request history 顺序一致。
- Fault Injection：controlled out-of-order settlement；不是随机 sleep。
- Acceptance：最大 in-flight `<=2` 且曾为 `2`；exclusive 前后 in-flight `0`；四个 call/result 完整；model-order commit 与 next request 匹配。
- Evidence Mapping：满足后可把 `33-C07/C08` 的 runtime 部分升为 fixture-scoped `CONFIRMED`。
- Observed Result：`PASS`；deterministic owner fixtures 覆盖 cap=2、parallel overlap、exclusive barrier、反序 settlement 与 model-order result/context/history commit。
- Limitations：Tool batch 不是 Multi-Agent；不证明任意 Tool 的线程安全或外部副作用顺序。

### `33-X04`｜Cancellation propagation trace

- Related Claims：`33-C13`、`33-C14`。
- Research Question：user cancellation 怎样穿过 active Turn，并留下可 replay 的持久边界？
- Hypothesis：同一 turn signal 到达 request/tool lane；cancel 后停止补充 dispatch，started work drain，unstarted call 得到 `ABORTED_BEFORE_DISPATCH`，Step balanced，Turn reason 为 typed aborted；新 prompt 使用 fresh controller 正常运行。
- What Would Falsify It：取消后继续启动 sibling、unstarted call 无 result、step/turn 不平衡、reason 被写成 completed、旧 aborted signal 污染下一 Turn，或宣称回滚已发生副作用。
- Inputs：multi-call response；第一个 parallel tool started 后触发 cancel；保留一个未启动 sibling；随后发送 replacement prompt。
- Expected Observable：signal identity receipts；started/settled/skipped 集合；synthetic result code；`turn/end(aborted,user)`；下一 Turn `completed` 且 signal 非 aborted。
- Fault Injection：deterministic cancel latch，不用 wall-clock 猜测；必要时用 deferred promise 握手。
- Acceptance：取消 Turn 的 call/result replay balanced；未启动 tool body count `0`；skipped result `isError=true` + exact code；下一 Turn 独立完成。
- Evidence Mapping：满足后可把 `33-C13/C14` 升为 fixture-scoped runtime conclusion。
- Observed Result：`PASS`；started calls drain，unstarted bodies 为零并收到 `ABORTED_BEFORE_DISPATCH`；aborted Turn 与后续 completed Turn 分离，visible prefix 以 `interrupted=true` 保留。
- Limitations：不证明 OS/process 强杀、远端 API 取消、外部副作用 rollback 或 Resume recovery。

## 6. Evidence Merge result

- Source Map / Call Path：`PASS`。
- Required traces：`4/4 PASS`。
- Selected owner tests：`10/10 PASS`；inline observations：`2/2 exit 0`。
- Initial `pnpm exec` failure 已原样保留；workspace-local Vitest 已存在，无 install/network fallback。
- Runtime boundary：production AgentLoop + Session/SystemPrompt/Tools/AgentRegistry + repo-owned MockAdapter + deterministic in-memory tools。
- Still unproved：real Provider/network/billing、OS hard-kill、任意 Tool thread safety、外部副作用 rollback、production reliability。

## 7. Evidence Gate recommendation

`PASS / OUTLINE ELIGIBLE`。

15 Claims 与 15 Cards 已收敛为 `14 CONFIRMED / 1 PROPOSAL / 0 BLOCKED`。关键 production symbol/call path 与四条 required Trace 均已闭合；所有 runtime claim 保持 MockAdapter/owner-fixture scope，BuildPilot lifecycle receipt 仍只作 Proposal。
