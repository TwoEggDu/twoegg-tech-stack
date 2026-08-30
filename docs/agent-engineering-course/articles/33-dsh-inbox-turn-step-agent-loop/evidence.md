# Article 33 Evidence

Status: `EVIDENCE MERGED / OUTLINE ELIGIBLE`

## Evidence summary

- Frozen revision：`dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Claim/Card count：`15 / 15`
- Final distribution：`14 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Required Trace：`33-X01 no-tool / 33-X02 single-tool / 33-X03 multi-tool / 33-X04 cancellation`
- Trace state：`4/4 PASS; 10/10 selected owner tests; 2/2 inline observations`
- Trace artifact：`experiments/agent-loop-four-traces.md`
- Current Gate recommendation：`PASS / OUTLINE ELIGIBLE`

`repository-map.md` 与 `call-path.md` 已复核全部关键 `File/Symbol/Call Path`；runtime evidence 仅来自 production services + repo-owned MockAdapter / deterministic owner fixtures。

### Evidence 33-E01｜Pinned identity

- Claim ID / Status / Class: `33-C01 / CONFIRMED / PINNED_SOURCE`
- Source: `official DSH fixture`; Repository/Commit: `deepseek-ai/deepseek-harness @ cd5ef8148158c3a752a658978873241fdf8e2bbc`
- Observation: `HEAD` 与 exact tag `dsh-v0.1.2-alpha.1` 相符，working tree status 为空。
- Counter-evidence Searched: `wrong HEAD, missing exact tag, dirty fixture`。
- Proves: `研究输入身份与 cleanliness`。
- Does Not Prove / Limitations: `不证明任何 loop runtime behavior；仅当前 fixture/timepoint`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED / ADOPT pinned baseline discipline`。

### Evidence 33-E02｜Host delivery to Inbox

- Claim ID / Status / Class: `33-C02 / CONFIRMED / PINNED_SOURCE`
- Source Location / Symbols: `packages/core/agent-loop/src/agent.ts:ReactLoopAgent.send/followup/steer/inject; packages/core/agent/src/inbox.ts:Inbox.splice`。
- Candidate Call Path: `Host -> Agent.followup|steer|inject -> send -> Inbox.splice -> wakeDriver`。
- Observation: `followup targets next-turn+wakeup; steer targets next-step+wakeup; inject targets next-step without wakeup; Inbox emits durable splice plus live notifications`。
- Counter-evidence Searched: `把单一 Chat UI 当唯一 producer`；官方 architecture 同时列 UI/SDK/extension seams。
- Proves: `Browser 与 Headless Host 在公共 delivery seam 汇合；target+wakeup 语义`。
- Does Not Prove / Limitations: `不是所有未来 Host 的穷举；Inbox 不负责 UI rendering`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED / ADOPT explicit target+wakeup`。

### Evidence 33-E03｜Turn boundary

- Claim ID / Status / Class: `33-C03 / CONFIRMED / OFFICIAL_DOC + PINNED_SOURCE + EXPERIMENT`
- Source Location / Symbol: `docs/architecture.md#turn-flow; packages/core/agent-loop/src/agent.ts:ReactLoopAgent.turn`。
- Candidate Call Path: `wakeDriver -> kick -> turn -> turn/start -> claim/preStep/step* -> turn/end`。
- Observation: `official docs define Turn as zero or more Steps; source candidate opens Turn before first claim and records typed reason in finally`。
- Counter-evidence Searched: `Turn=one model request`；empty/rejected first claim is a zero-Step counterexample。
- Proves: `Turn 是 typed durable interval，可包含零或多个 Steps；X01 复现 one-Step completed path`。
- Does Not Prove / Limitations: `X01 不覆盖全部 zero-Step reject/empty variants`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + RUNTIME_CONFIRMED (MOCK FIXTURE) / ADOPT Turn as durable interval`。

### Evidence 33-E04｜Step lifecycle

- Claim ID / Status / Class: `33-C04 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source Location / Symbols: `agent.ts:preStep,step,buildRequest; BlockAssembler; executeToolCalls`。
- Candidate Call Path: `assemble -> pre-step -> step/start -> user/message -> buildRequest -> stream -> assistant message -> tool batch? -> step/end`。
- Observation: `source candidate places assembly before admitted Step, appends model-visible inputs, parses stream blocks, and always closes an opened Step`。
- Counter-evidence Searched: `Step=Tool call`；no-tool Step and multi-tool Step refute it。
- Proves: `source ownership/order；X01/X02 的 exact event sequences 与 balanced Step boundaries`。
- Does Not Prove / Limitations: `不证明 real Provider stream/wire`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + RUNTIME_CONFIRMED (MOCK FIXTURE) / ADOPT explicit Step receipt`。

### Evidence 33-E05｜No-tool behavior

- Claim ID / Status / Class: `33-C05 / CONFIRMED / EXPERIMENT`
- Experiment / Fixture / Trace: `33-X01 / production AgentLoop + MockAdapter / experiments/agent-loop-four-traces.md`。
- Observation: `1 Turn, 1 Step, 1 request, 0 tool events, completed reason, final idle`。
- Counter-evidence Searched: `empty first claim can yield zero Step; max-tokens is not normal completion`。
- Proves: `fixture-scoped no-tool natural close`。
- Does Not Prove / Limitations: `reject/empty first admission or real Provider`。
- DSH Verification / Course Decision: `RUNTIME_CONFIRMED (MOCK FIXTURE) / ADOPT`。

### Evidence 33-E06｜Single-tool round trip

- Claim ID / Status / Class: `33-C06 / CONFIRMED / EXPERIMENT`
- Experiment / Fixture / Trace: `33-X02 / deterministic echo + MockAdapter / experiments/agent-loop-four-traces.md`。
- Observation: `1 Turn, 2 Steps, two requests, one linked call/result; request 2 contains c1 echo result`。
- Counter-evidence Searched: `Tool success directly terminates Turn`。
- Proves: `fixture-scoped Tool result-to-next-request loop`。
- Does Not Prove / Limitations: `policy/concurrency/real Tool excluded`。
- DSH Verification / Course Decision: `RUNTIME_CONFIRMED (MOCK FIXTURE) / ADOPT result-to-next-request loop`。

### Evidence 33-E07｜Bounded concurrency and barriers

- Claim ID / Status / Class: `33-C07 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source Location / Symbols: `packages/core/agent-loop/src/tool-calls.ts:executeToolCalls,runGroup,fillPool`。
- Candidate Call Path: `assistant tool blocks -> executeToolCalls -> executionMode -> exclusive group|parallel pool`。
- Observation: `candidate source describes exclusive barriers, bounded pool and reclassification before start`。
- Counter-evidence Searched: `Promise.all all calls`; source comments and branches contradict unconditional concurrency。
- Trace / Proves: `agent-loop-four-traces.md X03; cap=2 overlap and exclusive barrier`。
- Does Not Prove / Limitations: `arbitrary Tool thread safety or external side-effect order`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + RUNTIME_CONFIRMED (OWNER FIXTURE) / SIMPLIFY default serial`。

### Evidence 33-E08｜Model-order result aggregation

- Claim ID / Status / Class: `33-C08 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source Location / Symbols: `tool-calls.ts:runGroup.commitReady,appendToolResult`。
- Observation: `slots settle independently; contiguous committed cursor finalizes results and additional contexts in model order`。
- Counter-evidence Searched: `settlement order equals durable order`；candidate code separates them。
- Trace / Proves: `X03 controlled reverse settlement with model-order result/context/history commit`。
- Does Not Prove / Limitations: `production load or external completion order`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + RUNTIME_CONFIRMED (OWNER FIXTURE) / ADOPT ordered receipt`。

### Evidence 33-E09｜Continue and Stop ownership

- Claim ID / Status / Class: `33-C09 / CONFIRMED / OFFICIAL_DOC + PINNED_SOURCE + EXPERIMENT`
- Source Location / Symbols: `agent.ts:step,turn; agent/runtime-types.ts:agent/turn-stopping; ToolExecutionResult.concludesTurn`。
- Observation: `no tool call/max-tokens/concludesTurn can produce a step-end candidate; pending next-step input and turn-stopping listeners may require another Step`。
- Counter-evidence Searched: `single done boolean; Stop=Success`。
- Proves: `multi-owner Continue/Stop decision boundary；X01—X04 的 completed/continued/aborted paths`。
- Does Not Prove / Limitations: `business goal success is outside current source claim`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + RUNTIME_CONFIRMED (FIXTURE) / ADOPT typed reason, reject done=true`。

### Evidence 33-E10｜Policy affects Tool outcome

- Claim ID / Status / Class: `33-C10 / CONFIRMED / OFFICIAL_DOC + PINNED_SOURCE`
- Source Location: `docs/tool-execution-pipeline.md; packages/core/tools; agent-loop tests/tool-calls.spec.ts policy denial case`。
- Observation: `deny/approval refusal skips tool body but still normalizes and persists one tool result`。
- Counter-evidence Searched: `deny immediately equals turn error`；pipeline returns model-facing error outcome instead。
- Proves: `policy denial enters canonical error-result path, not a Turn-success oracle`。
- Does Not Prove / Limitations: `full Article 35 policy merge semantics；no runtime upgrade here`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED / ADOPT policy before side effect`。

### Evidence 33-E11｜Budget boundaries

- Claim ID / Status / Class: `33-C11 / CONFIRMED / OFFICIAL_DOC + PINNED_SOURCE`
- Source Location / Symbols: `agent-loop/README.md Known Limitations; agent.ts:AgentOptions.maxTokens,finish.kind=max-tokens,turnEnds`。
- Observation: `README says no built-in turn budget; maxTokens seeds request; max-tokens reason is sticky within current Turn`。
- Counter-evidence Searched: `maxTokens as total cost/turn budget; maxParallelToolCalls as step budget`。
- Proves: `request cap、concurrency cap 与 bounded absence of generic Turn/Step/cost budget`。
- Does Not Prove / Limitations: `usage/cost accumulation belongs Article 36；absence bounded to pinned production search`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + BOUNDED ABSENCE / DEFER complete budget policy`。

### Evidence 33-E12｜Error and retry boundary

- Claim ID / Status / Class: `33-C12 / CONFIRMED / PINNED_SOURCE`
- Source Location / Symbols: `agent.ts:step request-error waterfall,turn catch,throwError,kick`。
- Observation: `terminal model finish can be retried by listener; otherwise LlmError closes Turn; non-model extension errors are structured UNKNOWN at Turn boundary; kick contains driver failure`。
- Counter-evidence Searched: `every error stops the whole Agent forever; every tool error throws`。
- Proves: `model error normalization/retry owner and thrown extension-error Turn close path`。
- Does Not Prove / Limitations: `Provider retry quality or recovery success；no runtime upgrade here`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED / ADOPT typed error owner and bounded retry`。

### Evidence 33-E13｜Cancellation signal spine

- Claim ID / Status / Class: `33-C13 / CONFIRMED / PINNED_SOURCE + EXPERIMENT`
- Source Location / Symbols: `ReactLoopAgent.cancel,preStep,turn,step,buildRequest; executeToolCalls`。
- Candidate Call Path: `Agent.cancel -> phase.abort.abort -> shared signal -> assemble/pre-step/request/stream/tool -> turn catch -> turn/end(aborted)`。
- Observation: `source candidate passes the active turn AbortSignal through all listed boundaries and replaces controller for another pending Turn`。
- Counter-evidence Searched: `global sticky cancel flag; cancellation equals disposal`。
- Proves: `source signal propagation；X04 cooperative cancel/drain/typed aborted behavior`。
- Does Not Prove / Limitations: `external provider cancellation, process kill or remote quiescence`。
- DSH Verification / Course Decision: `SOURCE_CONFIRMED + RUNTIME_CONFIRMED (OWNER FIXTURE) / ADOPT signal spine`。

### Evidence 33-E14｜Cancellation durable balance

- Claim ID / Status / Class: `33-C14 / CONFIRMED / EXPERIMENT`
- Experiment / Fixture / Trace: `33-X04 / controlled deferred tools + MockAdapter / experiments/agent-loop-four-traces.md`。
- Observation: `started calls drained; unstarted bodies stayed zero and got ABORTED_BEFORE_DISPATCH; aborted Turn balanced; next Turn completed; visible prefix persisted interrupted=true`。
- Counter-evidence Searched: `cancel rollback; discard all partial evidence; reuse aborted marker`。
- Proves: `fixture-scoped cooperative cancellation and durable replay balance`。
- Does Not Prove / Limitations: `OS kill, remote cancel and side-effect rollback excluded`。
- DSH Verification / Course Decision: `RUNTIME_CONFIRMED (OWNER FIXTURE) / REJECT cancel-as-rollback`。

### Evidence 33-E15｜BuildPilot lifecycle receipt proposal

- Claim ID / Status / Class: `33-C15 / PROPOSAL / DESIGN_PROPOSAL`
- Source Basis: `33-C02—C14 preliminary boundaries`。
- Proposal: `TurnReceipt {turnId,start,endReason,stepIds}; StepReceipt {requestRef,toolBatchRef,endReason}; one scoped CancellationContext`。
- Counter-evidence Searched: `copy DSH plugin/event model wholesale; one done/success boolean`。
- Proves: `N/A — future candidate only`。
- Does Not Prove / Limitations: `BuildPilot ADR, implementation, runtime or Part VII authorization`。
- DSH Verification / Course Decision: `N/A / SIMPLIFY`。

## Evidence Gate recommendation

`PASS / OUTLINE ELIGIBLE`。15 Claims/Cards 最终为 `14 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。关键 symbols/call paths 与 `4/4` required Trace 已闭合；runtime claims 限定 MockAdapter/owner fixture，BuildPilot 仍只作 Proposal。
