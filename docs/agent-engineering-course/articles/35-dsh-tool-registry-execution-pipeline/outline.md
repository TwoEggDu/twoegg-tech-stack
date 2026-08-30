# Article 35 Outline｜Tool Registry 与 Tool Execution Pipeline

Status: `OUTLINE READY / EVIDENCE GATE PASS`

## 1. Article identity

- Canonical title：`Tool Registry 与 Tool Execution Pipeline`
- Article type：`原理篇`
- Part：`VI｜DeepSeek Harness`
- Weight：`L`
- Previous dependency：Article 34 已把 Session event、Projection 与 next model history 分账；本篇从 `tool/call` 继续向下闭合 Registry 与 execution result。
- Next boundary：Article 36 才展开 run-level Cost、Compaction、Trace、Cancellation 与 Recovery；本篇只处理单次 Tool call 的 timeout/cancel/error seam。
- Pinned implementation：official `deepseek-ai/deepseek-harness`，tag `dsh-v0.1.2-alpha.1`，commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`。
- Official posture：Developer Preview；未经过安全审计，不可写成 secure / production-ready。

## 2. One-sentence thesis

> Tool 能否被模型看见、调用参数能否被解释、调用是否被允许、Tool body 是否运行，以及结果进入模型、UI 与 Session 的哪一种视图，是五本不同的账；可靠的 Tool Runtime 必须逐段留下可关联的 receipt。

## 3. Problem space

### Reader question

为什么“注册了一个 Tool，schema 也通过了”仍然不能证明调用安全、执行成功或结果已经可靠保存？

### Teaching points

- Registry 只回答 capability identity / visibility，不回答 authorization。
- Provider 只承载模型请求/响应，不是可执行 Tool。
- Canonical arguments 只回答输入快照，不是 permission grant。
- schema validation 只约束数据形状，不保证 side-effect safety。
- UI Presentation、Model Content、canonical value 与 persisted result 不能折叠成一个 payload。

### Evidence

- `35-C02—C10` / `35-E02—E10`
- `repository-map.md` Sections 2—7
- `call-path.md` Sections 1—5

### Boundary

- 不把 Article 06 / Lab 02 的 `Deny > Ask > Allow` 课程 fixture 规则投射为 DSH 行为。
- 不把 Article 33 的 scheduler trace 冒充本篇 Registry / policy evidence。

## 4. Abstract model：两条链、五本账

### Reader question

怎样在不依赖某个框架类名的情况下描述 Tool 系统？

### Model A：Registry -> Model View

```text
Contribution
  -> Registry Identity
  -> Scope / Dedup / Restriction
  -> Model-facing Schema View
```

### Model B：Call -> Receipts

```text
Model Tool Call
  -> Parse / Canonicalize
  -> Validate
  -> Policy / Approval
  -> Execute
  -> Normalize / Post Policy
  -> Persist
  -> Model / UI Views
```

### Required correction

- 上述链是问题清单，不可直接冒充 pinned DSH 的固定 stage order。
- pinned DSH 的 typed `defineTool` validation 位于 definition execute wrapper：pre-policy 先运行，typed body 前才 validation；raw registration 没有统一 validator 保证。

### Five ledgers

1. Discovery ledger：谁注册、在哪个 scope 可见、何时 dispose。
2. Input ledger：raw argument text、parsed/canonical snapshot、Host metadata。
3. Authority ledger：pre-policy、ask/approval、guard、post-policy。
4. Execution ledger：body start/settle、timeout、cancel、concurrency、error owner。
5. Projection ledger：canonical value、model content、UI meta、Session event、next-step context。

### Figure responsibility

- Figure 1：双链总图，强调 visibility 与 execution 分离。
- Figure 2：五本账的 receipt correlation，不绘制不存在的 central authorization object。

## 5. DSH implementation I：Registry、scope 与 model view

### Reader question

Tool 怎样进入 pinned DSH，又怎样成为模型可见 schema？

### Claims / anchors

- `ToolRuntime.register`：显式注册；同 layer duplicate / reserved `run_code` / invalid output declaration fail closed；返回 disposer。
- `ToolRuntime.view` / `get` / `restrict`：nearest scope shadow、restriction intersection、local registration visibility。
- `wireSchemas` / `schemas` / `schemaOf`：model-facing projection 只有 `name / description / parameters`。
- `Agent.step -> buildRequest -> llm.stream`：schema view 进入 request；无真实 Provider wire capture。
- `35-C02—C03 / 35-E02—E03`。

### Boundaries

- Source proves explicit registration path, not active profile composition or implicit discovery across all extensions.
- scope restriction is not OS permission or execution audit。
- Provider receives a schema/call surface; Provider is not Tool。
- PTC mode 只作为 visibility/reachability seam，本文不展开成 PTC 教程。

## 6. DSH implementation II：arguments、canonicalization 与 validation ownership

### Reader question

模型给出的 arguments 到 typed body 之前，究竟发生了什么？

### Claims / anchors

- `executeToolCalls -> parseArguments`：raw argument string 先 JSON parse；malformed JSON 保留为 raw candidate。
- `ToolRuntime.createExecution`：lossless JSON snapshot、deep freeze、runtime token/root/parent、agent 与 fused signal。
- `defineTool` / `validate`：typed definition path 在 typed body 前校验并产生 `INVALID_ARGS`。
- `35-X01`：valid body `1`；malformed/schema-invalid body `0`；Session/next history correlated。

### Boundaries

- raw string parse != semantic validation。
- canonical snapshot != authorized。
- `callId / agent / signal / parent / token` 是 Host metadata，不属于 model arguments。
- typed-path validation 不能推广到 raw `ToolDefinition.register()`。
- schema validation != side-effect safety guarantee。

## 7. DSH implementation III：allow / deny / ask 是 waterfall，不是投票

### Reader question

多个 policy、approval 与 guard 怎样组合？

### Claims / anchors

- `tools/pre-execute` / `prepareExecution`：composition-ordered waterfall，listener 可以 `next()` 或短路。
- `serviceAsk -> ApprovalService.request/decide`：只有 `allowed-once` 放行；rejected/cancelled/unavailable/no-agent/no-service fail closed。
- `ToolRuntime.guard / guardReason`：pre-policy 之后的 monotonic deny seam，不能 force allow。
- `35-X02`：allow body/sentinel `1`；deny/ask `0`；ask 有一对 `approval/asked` / `approval/decided(rejected)`。

### Boundaries

- 不写成 `Deny > Ask > Allow` vote merge。
- listener ordering 取决于 composition。
- ApprovalService optional；没有真实 UI/human response evidence。
- Registry != Permission；Policy decision 也不替代 sandbox / external authority。

## 8. DSH implementation IV：execute、post hook、normalize 与 error ownership

### Reader question

Tool body 返回以后，为什么还不能直接把值写进聊天？

### Claims / anchors

- `dispatchScheduledExecution -> dispatchToolBody`：`tools/execute` waterfall / definition body。
- `createSuccessResult`：output snapshot/schema validation、model render、optional presentation meta。
- `tools/post-execute -> postExecute`：accept 可替换 content 或 canonical value（不能同时替换）；block 将结果改成 valueless error，可附加 next-step context。
- finalizer / materialize / `tools/result` observer：observer 只观察 finalized outcome。
- selected X01/X02 stage traces 区分 pre/execute/post/result；没有 runtime 穷举 unknown/pre/body/post/finalizer 所有 failure branch。

### Boundaries

- post-policy block 发生在 body/side effect 之后，不能 rollback。
- 相同 model-facing error 不证明相同 stage owner。
- Tool-specific error code 不可泛化为全系统 taxonomy。

## 9. Timeout、cancellation 与 concurrency

### Reader question

超时、取消与并发上限到底控制什么，又控制不了什么？

### Claims / anchors

- optional timeout plugin `apply`：读取 `timeoutMs`、替换 deadline signal、等待 delegated body settle，再归类 `TOOL_TIMEOUT`。
- caller/wrapper signal fusion：started call settle 后 `ABORTED`；unstarted call `ABORTED_BEFORE_DISPATCH`。
- `executionMode` + `tool-calls.ts/runGroup`：只有 literal `true` 允许 parallel；exclusive barrier；cap 限制 in-flight；commit 按 model order。
- `35-X03`：deadline signal -> no result before cleanup release -> settle -> `TOOL_TIMEOUT`；control success。
- `35-X04`：cap `1`；started body `1`、held body `0`；release 后分别 `ABORTED` / `ABORTED_BEFORE_DISPATCH`；follow-up completed。

### Boundaries

- timeout/cancel cooperative，不是 hard kill。
- cancellation request 不证明 remote work 或计费停止。
- cancel 不等于 rollback/recovery；run-level recovery 归 Article 36。
- dispatch overlap 与 model-order persistence 是两本账；不声称 external side effects 按 model order 发生。

## 10. Result lanes：value、model、UI、Session 与 next context

### Reader question

“Tool result”为什么至少有五种产品？

### Result matrix

| Lane | Owner / anchor | Usage | Evidence ceiling |
|---|---|---|---|
| canonical value | `createSuccessResult/materializeFinalResult` | runtime/post-policy | generic raw value 不持久化 |
| model content | `render` + `appendToolResult` | next model request | real Provider delivery 未运行 |
| UI meta | `presentationMeta` + client tool node | replayable presentation input | actual client screen 未运行 |
| Session result | `appendToolCall/appendToolResult` | durable call/result pair | stores content/error/meta, not arbitrary canonical value |
| next-step context | post decision / ordered context acceptance | later Step | context 不是 UI meta 或 canonical result |

### Key rule

- UI Presentation != Model Content。
- persisted result != canonical value dump。
- matching final text 不能单独证明 lane ownership；需 callId + Session + next-history correlation。

## 11. Large result：optional spill、exact fallback 与 separate prune

### Reader question

大结果是否必然被 summary？

### Claims / anchors

- `spill-policy.apply / spillReplacement`：opt-in、all-text、configured threshold；full save + bounded head/tail preview + locator。
- storage absent/failure：best-effort 保留原成功 inline result。
- `35-X05`：small inline；1,600 bytes full hash = stored hash、preview 200 bytes、locator `/spill/big-ok.txt`；1,000 bytes failed save exact inline fallback；`semanticSummary:false`。
- `ToolResultPruner.pruneSession` 是另一个 post-persistence deterministic prune path；不展开 Article 36 的 Compaction 主线。

### Boundaries

- spill 不是 universal guarantee。
- full save 不证明 retention、authorization、later retrieval 或 UI。
- bounded preview 不是 semantic summary；固定 source / trace 没有 universal semantic summarizer evidence。

## 12. Five negative traces：为什么失败历史也必须保留

### Cycle 0

- `22 passed / 0 failed`，但五类都缺 frozen SAME-CALL correlation，结论保持 `NOT_ACCEPTED / BLOCKED_EVIDENCE`。

### Recovery Attempt 1

- command exit `0`，但 anchored pattern 因 suite prefix 选中 `0/5`；保持 `NOT_ACCEPTED`。

### Accepted Recovery Cycle 1

- single temporary untracked source-owned harness；`1 file / 5 tests / exit 0`；`13` JSONL records，分布 `3 / 3 / 2 / 2 / 3`。
- post-cleanup fixture `HEAD` 回到 pinned SHA，status/staged/unstaged diff empty。
- blocked Corepack preflight：一次错误 cwd 的裸 version probe 尝试 npm registry 并被 `EACCES` 阻止；`NETWORK_REQUESTS=ZERO` 只限定 accepted experiment / Provider / tool body。

### Figure responsibility

- Figure 3：五类 Trace 的 `callId -> stages -> body -> result -> session -> nextHistory` correlation table。
- 不展示完整 1,600-byte payload；只展示 bytes/hash/preview/locator。

## 13. Engineering judgment：怎样设计自己的 Tool Runtime

### Proposed checks

- Registry contract：identity、scope、schema projection、disposer。
- Execution contract：raw hash、canonical args ref、policy receipt、body start/settle、terminal reason。
- Projection contract：model content ref、UI meta ref、Session seq、spill ref、redacted diagnostics。
- policy/timeout/cancel 必须有 owner，不用一个 `success:boolean` 代替。

### BuildPilot implication

- `ToolExecutionReceipt` 只作为 `COURSE_PROPOSAL / DEFER`。
- 建议字段只来自已闭合问题：callId、tool identity/scope、raw-arg hash、validation/policy decision、start/end/terminal kind、model/persistence/spill refs、redacted diagnostics。
- 不宣称 DSH 有该统一 receipt；不宣称 BuildPilot 已有 ADR、schema、code、runtime 或 security review。
- 不在 Article 35 完成 ADOPT/SIMPLIFY/REJECT/DEFER 总矩阵；最终矩阵归 Article 37。

## 14. Verification and non-scope

### Confirmed layers

- `OFFICIAL_DOC`：pinned identity、Developer Preview、SAFETY posture。
- `PINNED_SOURCE`：Registry/model view、call path、policy、timeout/cancel、result/persist/UI source path、optional spill。
- `RUNTIME_OBSERVATION + EXPERIMENT`：typed-path invalid args、deny/ask、cooperative timeout/cancel、Session/next projection、opt-in in-memory spill。

### Explicit non-scope

- no real Provider wire/request/response；
- no production Tool or external side effect；
- no actual client UI render；
- no production safety / security guarantee；
- no raw-registration validation guarantee；
- no hard kill、rollback、remote quiescence 或 run-level recovery；
- no universal spill、retention、authorization、retrieval 或 semantic summary；
- no Article 36/37 conclusion，no Part VII / BuildPilot implementation。

## 15. Learning check

1. Registry、Permission 与 Provider 为什么是三个不同 owner？
2. model schema、raw arguments、canonical snapshot 与 Host metadata 各自属于哪一层？
3. 为什么 typed `defineTool` 的验证不能推广到 raw registration？
4. DSH pre-policy 为什么是 waterfall 而不是 vote merge？
5. post-policy block 为什么不能保证副作用安全？
6. timeout/cancel 为什么必须保存 signal、drain 与 terminal 三类 receipt？
7. dispatch order、settlement order 与 persist order 为什么要分账？
8. canonical value、model content、UI meta 与 Session result 分别服务谁？
9. spill failure 为何保持 inline success，而不应该被改写成 summary？
10. Cycle 0 的 22 个绿灯与 Attempt 1 的 exit 0 为什么都不能成为 accepted trace？

## 16. Job competency mapping

| Competency | Article evidence |
|---|---|
| 架构分层 | 能把 discovery、authority、execution、persistence 与 presentation 拆成独立 owner |
| 源码阅读 | 能给出 pinned file + symbol + caller/callee，并保留反证与版本上限 |
| 安全工程 | 能区分 schema、policy、approval、sandbox、timeout 与 side-effect safety |
| 并发与取消 | 能区分 dispatch/settle/commit，解释 cooperative cancellation 与 drain |
| 可观测性 | 能设计 callId 贯穿 stage/body/result/session/next-history 的 trace |
| 工程判断 | 能把 DSH 事实转成 bounded course proposal，而不提前实现 BuildPilot |

## 17. Claim-to-section coverage

| Claim | Draft sections |
|---|---|
| `35-C01` | opening, Evidence Boundary |
| `35-C02—C03` | Registry / Model View |
| `35-C04` | Arguments / Validation, X01 |
| `35-C05—C06` | Policy / Hooks / Errors, X02 |
| `35-C07—C08` | Timeout / Cancel / Concurrency, X03/X04 |
| `35-C09` | Result lanes, all traces |
| `35-C10` | Large result, X05 |
| `35-C11` | Experiment history / accepted recovery |
| `35-C12` | BuildPilot proposal only |

Coverage: `12 / 12` Claims, `12 / 12` Evidence Cards, `35-X01—X05` all present with limitations.

## 18. Closing sentence

> Tool Runtime 不是“把函数交给模型调用”，而是让 capability visibility、input ownership、authority decision、execution terminal 与每一种 result view 都有独立、可关联、不过度承诺的证据。
