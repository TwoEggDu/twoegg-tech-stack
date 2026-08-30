# Article 35 Evidence

Status: `EVIDENCE_MERGE PASS / CYCLE 0 NOT ACCEPTED / RECOVERY ATTEMPT 1 NOT ACCEPTED / FIVE REQUIRED TRACES ACCEPTED`

## Evidence boundary

- Frozen implementation target: official `deepseek-ai/deepseek-harness`, tag `dsh-v0.1.2-alpha.1`, commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Source Map / Call Path 已完成。Cycle 0 的 `22 passed / 0 failed` 仍因缺 SAME-CALL observers 而 `NOT_ACCEPTED`；Recovery Attempt 1 也因 anchored pattern 选择 `0/5` 而 `NOT_ACCEPTED`。最终 preserved capture 独立记录 `1 file / 5 tests / 13 records / exit 0`，并逐 case 满足 frozen acceptance。
- `DOC_CONFIRMED`、`SOURCE_CONFIRMED`、`RUNTIME_OBSERVATION` 与 `EXPERIMENT_CONFIRMED` 分账。Fixture trace 不升级为真实 Provider、生产 side effect、actual UI render 或 production safety guarantee。
- Article 33/34 evidence is a dependency boundary, not Article 35 evidence. Article 36 remains untouched and owns run-level cost/compaction/recovery questions.

## Historical preliminary claim register

| Claim ID | Status | Evidence needed before upgrade |
|---|---|---|
| `35-C01` | `DOC_CONFIRMED` | identity/read receipt only; no behavioral promotion. |
| `35-C02—C03` | `BLOCKED_SOURCE_MAP` | exact file/symbol/call path + counter-evidence. |
| `35-C04—C10` | `PARTIAL` | source closure; designated negative trace where listed. |
| `35-C11` | `CYCLE0_NOT_ACCEPTED / RECOVERY_FROZEN_DESIGN` | five Cycle 1 SAME-CALL raw execution records plus clean-fixture receipt. |
| `35-C12` | `PROPOSAL` | future Part VII authorization and tests. |

## Historical preliminary evidence cards

### Evidence 35-E01｜Pinned identity and official safety posture

- Claim ID / Status / Class: `35-C01 / DOC_CONFIRMED / OFFICIAL_DOC`.
- Source / Version scope: official DSH `README.md` and `SAFETY.md` at `dsh-v0.1.2-alpha.1 @ cd5ef8148158c3a752a658978873241fdf8e2bbc`; retrieved in the existing Article 35 research boundary on `2026-08-30 Asia/Shanghai`.
- Observation: README labels the project developer preview; SAFETY says it is not security-audited or production-ready.
- Counter-evidence: neither document is a Registry/pipeline/runtime proof.
- Proves: version/document posture and the safety limit on course wording.
- Does Not Prove / Limitations: tool semantics, sandbox enforcement, safe deployment, or fixture cleanliness after future execution.
- DSH verification / Course decision: `DOC_CONFIRMED / bind all Article 35 DSH claims to this exact baseline`.

### Evidence 35-E02｜Registry and scope seam

- Claim ID / Status / Class: `35-C02 / BLOCKED_SOURCE_MAP / PINNED_SOURCE`.
- Candidate source / version: `packages/core/tools/src/index.ts` (`ToolRuntime`, `ToolLayer`, `register`, `restrict`, `guard`, `view`, `get`, `schemas`) at the pinned revision.
- Observation: research located candidate owners for explicit registration, scoped visibility and disposal; no line-anchored chain was produced here.
- Counter-evidence: implicit discovery/provider registration, duplicate/shadow exceptions and incomplete disposer paths remain unsearched at source-map standard.
- Proves: only the next investigation surface.
- Does Not Prove / Limitations: registration mechanism, scope merge order, visibility, reachability or cleanup behavior.
- DSH verification / Course decision: `BLOCKED_SOURCE_MAP / do not teach Registry behavior as fact`.

### Evidence 35-E03｜Model schema versus executable definition

- Claim ID / Status / Class: `35-C03 / BLOCKED_SOURCE_MAP / PINNED_SOURCE`.
- Candidate source / version: `packages/core/tools/src/index.ts`, `schema.ts`, and Agent Loop tool-call consumers at the pinned revision.
- Observation: research proposes separate wire-schema, execute/body and Host-metadata lanes.
- Counter-evidence: Provider adapters may add fields; no consumer-field matrix exists yet.
- Proves: a required distinction to verify.
- Does Not Prove / Limitations: actual serialized field set, provider wire behavior, or metadata exclusion.
- DSH verification / Course decision: `BLOCKED_SOURCE_MAP / preserve the lane question`.

### Evidence 35-E04｜Argument materialization and validation

- Claim ID / Status / Class: `35-C04 / PARTIAL / PINNED_SOURCE + EXPERIMENT`.
- Candidate source / version: `packages/core/agent-loop/src/tool-calls.ts`; `packages/core/tools/src/schema.ts` (`defineTool`, validation) at the pinned revision.
- Observation: candidate path separates raw model argument text, parse/materialization, typed tool input and runtime metadata; `defineTool` versus raw registration is an explicit uncertainty.
- Counter-evidence: a central pre-policy validator covering all raw definitions would narrow/change the claim.
- Proves: the negative must inspect body count, not merely final error.
- Does Not Prove / Limitations: Cycle 0's green validation tests lack same-call raw ingress, body counter, Session pair and next projection; they do not satisfy `35-X01` acceptance.
- DSH verification / Course decision: `PARTIAL / CYCLE0_NOT_ACCEPTED / RECOVERY_PENDING`.

### Evidence 35-E05｜Policy and approval boundary

- Claim ID / Status / Class: `35-C05 / PARTIAL / PINNED_SOURCE + EXPERIMENT`.
- Candidate source / version: core tool pipeline, `tools/pre-execute`, `serviceAsk`, and `packages/interaction/user-approval` at the pinned revision.
- Observation: research candidate is ordered pre-execute waterfall -> approval -> monotonic guards -> execute/post/finalize/result observation; Article 06's `Deny > Ask > Allow` fixture must not be projected onto DSH.
- Counter-evidence: an independent vote aggregator, or an error branch that skips/enters post/finalizer differently, remains to be mapped.
- Proves: policy result and stage ownership must be recorded separately.
- Does Not Prove / Limitations: Cycle 0 observed denial/approval in complementary tests, not one ask/refusal call with body count, audit pair, terminal Session event and next projection.
- DSH verification / Course decision: `PARTIAL / CYCLE0_NOT_ACCEPTED / RECOVERY_PENDING`; continue rejecting policy-vote shorthand.

### Evidence 35-E06｜Error-stage ownership

- Claim ID / Status / Class: `35-C06 / PARTIAL / PINNED_SOURCE + EXPERIMENT`.
- Candidate source / version: `prepareExecution`, `dispatchToolBody`, `postExecute`, `finalizeScheduledExecution`, `finishScheduledExecution` and `notifyResult` in the pinned core tool path.
- Observation: research identifies distinct candidate branches for deny, unknown tool, pre-hook throw, body throw, post-hook throw and finalizer throw.
- Counter-evidence: matching model-visible error payloads can hide different stage traversal; no branch table exists yet.
- Proves: final-result JSON cannot be the sole stage oracle.
- Does Not Prove / Limitations: Cycle 0 terminal objects do not provide the missing same-call stage/body/session/next-history correlation for `35-X01—02`.
- DSH verification / Course decision: `PARTIAL / CYCLE0_NOT_ACCEPTED / RECOVERY_PENDING`.

### Evidence 35-E07｜Timeout and cancellation seam

- Claim ID / Status / Class: `35-C07 / PARTIAL / PINNED_SOURCE + EXPERIMENT`.
- Candidate source / version: `packages/guard/timeout-policy/src/index.ts` (`apply`, `TOOL_TIMEOUT`) and ToolRuntime execution path at the pinned revision.
- Observation: candidate design treats timeout as wrapper-owned signal/reclassification and cancellation as cooperative.
- Counter-evidence: hard termination, rollback, remote quiescence or generic recovery are not established by a terminal code.
- Proves: `35-X03—04` must capture abort delivery and settlement/drain separately.
- Does Not Prove / Limitations: Cycle 0 separately observed timeout/cancel behavior but not same-call signal -> drain -> Session -> next-history correlation; hard kill, rollback and run-level recovery remain unproven.
- DSH verification / Course decision: `PARTIAL / CYCLE0_NOT_ACCEPTED / RECOVERY_PENDING`; reject timeout-or-cancel equals rollback.

### Evidence 35-E08｜Concurrency and ordered commit seam

- Claim ID / Status / Class: `35-C08 / PARTIAL / PINNED_SOURCE + DEPENDENCY`.
- Source boundary / version: Article 33 selected `tool-calls.ts` scheduler evidence at the same pinned revision; Article 35 has no Registry-to-scheduler source map.
- Observation: Article 33 establishes a dependency-level distinction between dispatch overlap and model-order commit.
- Counter-evidence: this does not identify ToolRuntime ownership, policy ordering or result lanes in Article 35.
- Proves: the Article 35 source investigator must draw the seam rather than repeat scheduler claims.
- Does Not Prove / Limitations: a completed Article 35 call path or any new runtime observation.
- DSH verification / Course decision: `PARTIAL / reuse only as bounded dependency`.

### Evidence 35-E09｜Result lanes and persistence

- Claim ID / Status / Class: `35-C09 / PARTIAL / PINNED_SOURCE + EXPERIMENT`.
- Candidate source / version: `ToolExecutionResult`, `createSuccessResult`, `appendToolResult`, Session/UI consumers at the pinned revision.
- Observation: research requires a value/content/meta/error/additional-context/persisted-event matrix.
- Counter-evidence: UI callbacks can be replay-only or Host-local; matching final JSON cannot establish each lane's owner.
- Proves: every negative trace needs normalized result plus raw Session event and next-step receipt.
- Does Not Prove / Limitations: Cycle 0 has separate result/history tests, not the required same-call projection for any X01—X05 case; UI rendering remains outside the recovery harness.
- DSH verification / Course decision: `PARTIAL / CYCLE0_NOT_ACCEPTED / RECOVERY_PENDING`; do not collapse result to one payload.

### Evidence 35-E10｜Large-result spill boundary

- Claim ID / Status / Class: `35-C10 / PARTIAL / PINNED_SOURCE + EXPERIMENT`.
- Candidate source / version: `packages/spill/spill-policy`, `spill`, `spill-local`, `util/output-retention` at the pinned revision.
- Observation: research candidate is opt-in plain-text full-save with bounded preview/locator and best-effort inline fallback.
- Counter-evidence: a UI `summary` label does not prove semantic summarization; successful storage does not prove retention/authorization/later availability.
- Proves: `35-X05` must compare full payload, preview/locator and spill-failure fallback.
- Does Not Prove / Limitations: Cycle 0 did not correlate full hash, save/locator, bounded preview, Session/model projection and storage-failure fallback for the same calls; semantic summary, production retention and access control remain unproven.
- DSH verification / Course decision: `PARTIAL / CYCLE0_NOT_ACCEPTED / RECOVERY_PENDING`; call it spill only if Cycle 1 closes the trace.

### Evidence 35-E11｜Five negative-trace contract

- Claim ID / Status / Class: `35-C11 / CYCLE0_NOT_ACCEPTED + RECOVERY_FROZEN_DESIGN / EXPERIMENT_DESIGN`.
- Source / version: `experiments/tool-execution-negative-traces.md`; fixed DSH baseline above.
- Observation: original design freezes bad args, deny, timeout, cancel and large result. Cycle 0 ran 22 focused tests but failed the unchanged SAME-CALL acceptance in all five categories. Recovery Cycle 1 now freezes one temporary source-owned harness that supplies the missing observers; it is `NOT_EXECUTED`.
- Counter-evidence: a passing owner test, final error object, or clean command exit alone cannot replace the requested multi-lane raw record.
- Proves: the future executor's required evidence shape.
- Does Not Prove / Limitations: the recovery design proves no runtime behavior; Cycle 0 green counts cannot be reinterpreted as acceptance, and Cycle 1 has no observation yet.
- DSH verification / Course decision: `BLOCKED_EVIDENCE / RECOVERY_FROZEN_DESIGN / NEXT EXPERIMENT_EXECUTE`.

### Evidence 35-E12｜BuildPilot receipt proposal

- Claim ID / Status / Class: `35-C12 / PROPOSAL / COURSE_PROPOSAL`.
- Source basis: the unclosed result-lane/policy/cancellation questions in `35-E04—E10`.
- Proposal: a future `ToolExecutionReceipt` should record `callId`, tool identity/scope, raw-argument hash, validation/policy decision, start/end/terminal kind, model-content reference, persistence reference, spill reference and redacted diagnostics; it must keep canonical value out of default durable output.
- Counter-evidence: one universal receipt can leak secrets or erase Host/model/UI lane differences.
- Proves: `N/A — future design candidate only`.
- Does Not Prove / Limitations: DSH API, BuildPilot ADR/code, security approval or runtime behavior.
- DSH verification / Course decision: `N/A / DEFER until Part VII`.

## Recovery Cycle 1 evidence receipt

- Accepted evidence run：最终 preserved capture replay，命令选择 `1 file / 5 tests`、exit `0`、13 条 `a35-same-call-recovery-v1` JSONL。Cycle 0 与 Recovery Attempt 1 不参与 acceptance，分别保留 `BLOCKED_EVIDENCE` 与 `0/5 selected / NOT_ACCEPTED`。
- Integrity：manifest 中 9 个非 manifest artifacts 的 bytes / SHA-256 与 fresh recomputation 一致；13 个 callId 唯一，所有 required top-level/nested fields 存在，每条 Session call/result 与 next-request/derived-history 都为 `1/1` 且 content hash 相等。
- Fixture：post-cleanup `HEAD=cd5ef8148158c3a752a658978873241fdf8e2bbc`，status、unstaged diff、staged diff 均为空；instrumentation 是 temporary untracked test，保存 source/patch 后从 fixture 精确删除。
- Network qualification：accepted experiment、MockAdapter、Provider/tool bodies 没有网络或真实 side effect；但早先从错误 course cwd 执行的裸 Corepack version probe 曾尝试访问 npm registry 并被 `EACCES` 阻止。因此 raw `NETWORK_REQUESTS=ZERO` 只能作 accepted experiment scope 的限定陈述。

| Evidence | Class | Accepted observation | Limit |
|---|---|---|---|
| `35-E01` | `OFFICIAL_DOC` | 固定 tag/commit、Developer Preview 与 SAFETY posture | 文档不证明 Registry/runtime/safety。 |
| `35-E02—E03` | `PINNED_SOURCE` | registration/scope/wire schema/executable/Host metadata owners 与 call path 闭合 | 无 profile composition 或真实 Provider wire observation。 |
| `35-E04` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | typed valid body `1`；malformed/schema invalid body `0`，`INVALID_ARGS`，Session/next correlated | raw direct registration 未被证明自动 validation。 |
| `35-E05` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | allow body `1`；deny/ask body `0`；ask audit pair rejected；terminal/session/next correlated | ordered waterfall，不是 vote merge；permission/sandbox safety 另账。 |
| `35-E06` | `PINNED_SOURCE + RUNTIME_OBSERVATION` | selected X01/X02 stage traces 区分 pre/execute/post/result ownership | 未以 runtime 穷举 unknown/pre/body/post/finalizer 所有失败分支。 |
| `35-E07` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | timeout signal -> cleanup release -> settle -> `TOOL_TIMEOUT`；cancel started/held 分别 `ABORTED` / `ABORTED_BEFORE_DISPATCH` | cooperative only；不是 hard kill、rollback、remote stop 或 recovery。 |
| `35-E08` | `PINNED_SOURCE + INFERENCE` | Registry execution mode 接入 bounded scheduler，dispatch/commit 两账 | Article 33 dependency，不重称新 runtime matrix。 |
| `35-E09` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | 每个 negative 都有 normalized terminal result、Session event 与 next model/history projection correlation | actual client UI render 与 arbitrary canonical value persistence 未执行。 |
| `35-E10` | `PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT` | 1,600-byte full save + 200-byte preview/locator；storage failure 保留 1,000-byte inline full hash；`semanticSummary:false` | optional policy；无 retention/access/retrieval/universal summary 保证。 |
| `35-E11` | `EXPERIMENT` | 五类 required SAME-CALL trace 全部通过 frozen acceptance | 仅 pinned fixture + MockAdapter + in-memory instrumentation。 |
| `35-E12` | `COURSE_PROPOSAL` | future ToolExecutionReceipt 的 bounded proposal | Part VII 前不宣称 BuildPilot design/code。 |

## Final Evidence Cards｜post-Recovery deterministic record

> 本节是 Recovery Cycle 1 accepted capture 之后追加的 final cards。上方 historical preliminary cards 保留其当时的 `BLOCKED_SOURCE_MAP / PARTIAL / NOT_EXECUTED` 时间语义，没有被倒改成 retrospective PASS。

### Shared fixed identity and accepted experiment receipt

- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact accepted command: `corepack pnpm exec vitest run packages/core/agent-loop/tests/article-35-same-call-recovery.spec.ts --testNamePattern "^A35 recovery / 35-X0[1-5] / SAME-CALL$" --testTimeout=30000 --maxWorkers=1 --reporter=verbose --silent=false`.
- Accepted exit / discovery: exit `0`; `1 file / 5 tests`; `13` `a35-same-call-recovery-v1` records. Cycle 0 `22 passed / 0 failed` and Recovery Attempt 1 `exit 0 / 0 of 5 selected` remain `NOT_ACCEPTED`.
- Fixture / instrumentation: pinned fixture `C:\Users\IGG\AppData\Local\Temp\codex-dsh-part-vi-cd5ef814`; one temporary untracked source-owned test composed pinned DSH runtime components with repo-owned `MockAdapter` and in-memory Tool / approval / spill fixtures. The exact test source and new-file patch were preserved, then the fixture copy was removed; post-cleanup HEAD equals the full commit and status/staged/unstaged diff are empty.
- Stable raw artifact roots: `experiments/raw/recovery-cycle-1/command.txt`, `combined-output.txt`, `a35-recovery-traces.jsonl`, `article-35-same-call-recovery.spec.ts`, `article-35-same-call-recovery.patch`, `environment-and-cleanliness.txt`, and `manifest.txt`. `combined-output.txt` ends with `A35_COMMAND_EXIT=0`; the manifest records the bytes / SHA-256 receipts.
- Runtime boundary: accepted experiment / Provider / tool-body network requests=`ZERO`; production service / deployment, real Provider, production Tool, external side effect, persistent spill and actual client UI=`NOT RUN`. The earlier blocked Corepack version probe is preserved separately and prevents a whole-turn `NETWORK_REQUESTS=ZERO` claim.

### Final Evidence 35-E02｜Registry, scope, deduplication and model view

- Claim ID / Status / Class: `35-C02 / SOURCE_CONFIRMED / PINNED_SOURCE`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/tools/src/index.ts` — `ToolRuntime` (class, stable lines `788-838`), `register` (`1030-1061`), `restrict` (`1063-1096`), `guard/guardReason` (`1099-1127`), `view` (`1129-1191`), `get` (`1194-1205`), `wireSchemas` (`976-1000`), `schemas/schemaOf` (`1227-1266`). Stable cross-reference: `repository-map.md` §2 and `call-path.md` §1 `Register / Discovery / scope`.
- Exact call path: plugin `-> ctx.tools.register(definition) -> ToolRuntime.layers.effect / layer.tools.insert -> view(scope) -> wireSchemas(scope) -> schemaOf(name, description, parameters) -> request.header.tools`.
- Runtime / experiment receipt: `NOT RUN / NOT REQUIRED` for profile composition, provider delivery or disposer invocation; this card is static-source only.
- Observation: registration is explicit and lifecycle-owned; same-layer duplicate/reserved-name/invalid output declarations reject; nearest scope shadows inherited entries, restrictions intersect inherited visibility, and registration returns its disposer. Model view is a bounded schema projection, not the executable definition.
- Counter-evidence / falsifier: an active profile proving a different composition path, or a pinned owner bypassing `register/view/wireSchemas`, would require a separate scoped card. Source presence alone cannot establish which Tool a real profile registered.
- Proves: fixed-source ownership and behavior of the explicit Registry/view path.
- Does Not Prove / limitations: active discovery across every extension, OS permission, per-call authorization, provider delivery, body execution or cleanup in a deployed Host.
- BuildPilot implication: keep Registry identity/visibility/disposal receipts separate from invocation authority; this is a bounded course mapping input, not implemented BuildPilot design.

### Final Evidence 35-E03｜Model schema, executable definition and Host metadata

- Claim ID / Status / Class: `35-C03 / SOURCE_CONFIRMED / PINNED_SOURCE`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/tools/src/index.ts` — `wireSchemas` (`976-1000`), `schemaOf` (`1254-1266`), `ToolExecutionInput` (`315-339`), `ToolExecution` (`373-385`), `createExecution` (`1363-1449`); `packages/core/agent-loop/src/agent.ts` — `step` (`339-363`) and `buildRequest` return (`533-541`); `packages/core/agent-loop/src/tool-calls.ts` — `executeToolCalls/parseArguments` (`59-110`). Stable cross-reference: `repository-map.md` §3 and `call-path.md` §1 `Discovery / scope`, `Provider request`, `Call ingress`, `Canonicalize input`.
- Exact call path: `systemPrompt.tools -> wireSchemas -> schemaOf -> Agent.step -> buildRequest -> llm.stream`; response path `assistant tool-call block -> executeToolCalls -> parseArguments -> createExecution`, where `arguments` remain model-originated while `callId/rootCallId/agent/parent/signal/registry token` are Host-owned.
- Runtime / experiment receipt: real Provider wire capture=`NOT RUN`; no experiment is used to claim serialization or provider conformance.
- Observation: native model schema contains `name/description/parameters`; executable callback, output schema, timeout/concurrency metadata, signal, agent and presentation callback do not cross that source-defined projection.
- Counter-evidence / falsifier: a concrete Provider adapter may add its own wire fields; such evidence would narrow the consumer-specific statement rather than make executable/Host metadata model-owned.
- Proves: fixed-source field ownership and the native client-tool request construction seam.
- Does Not Prove / limitations: a real Provider received this request, preserved the exact fields, enforced schema or returned a conforming call.
- BuildPilot implication: model schema, executable definition and Host execution metadata require distinct data products; this remains a course mapping input.

### Final Evidence 35-E04｜Arguments, canonical snapshot and typed validation

- Claim ID / Status / Class: `35-C04 / SOURCE_CONFIRMED + EXPERIMENT_CONFIRMED_FOR_TYPED_PATH / PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/agent-loop/src/tool-calls.ts` — `executeToolCalls/parseArguments` (`59-110`); `packages/core/tools/src/index.ts` — scheduler `prepare` and `createExecution` (`1363-1449`); `packages/core/tools/src/schema.ts` — `defineTool` (`545-588`). Stable cross-reference: `call-path.md` §1 `Call ingress / Canonicalize input / Validate`.
- Exact call path: `assistant tool-call -> executeToolCalls -> parseArguments -> scheduler.prepare -> createExecution(snapshot + deepFreeze) -> pre-policy -> dispatch definition -> defineTool argument validation -> typed body`.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; this card uses `35-X01` records `x01-valid`, `x01-malformed`, `x01-schema`.
- Fixture / instrumentation / raw artifacts: shared temporary source-owned harness; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (`case=35-X01`), `combined-output.txt`, `command.txt`, preserved source/patch and `manifest.txt`; Cycle 0 remains visible at `experiments/raw/35-x01-bad-arguments.txt` and is not the acceptance basis.
- Observation: valid typed input starts the body once; malformed and schema-invalid inputs start it zero times and end as `INVALID_ARGS`; each call preserves raw arguments and has exactly one Session call/result plus matching next-request/derived-history content hash.
- Counter-evidence / falsifier: a central pinned validator covering arbitrary raw `ToolDefinition` registrations would broaden the source claim; a negative record with body start `>0`, missing `INVALID_ARGS`, duplicate/missing Session event or mismatched projection hash would falsify the accepted typed-path observation.
- Proves: parse/canonicalization/Host metadata are separate, and the selected typed `defineTool` path rejects bad arguments before its typed body.
- Does Not Prove / limitations: raw registration automatically validates, canonical input is authorized, schema-valid inputs are side-effect-safe, or production tools/providers behave identically.
- BuildPilot implication: any future receipt should identify validation owner/path and body-start state rather than infer validation from Registry membership; proposal only.

### Final Evidence 35-E05｜Ordered policy, approval and guard boundary

- Claim ID / Status / Class: `35-C05 / SOURCE_CONFIRMED + EXPERIMENT_CONFIRMED / PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/tools/src/index.ts` — `tools/pre-execute` declaration (`149-160`), `prepareExecution` (`1462-1505`), `serviceAsk` (`1677-1728`), `guard/guardReason` (`1099-1127`); `packages/interaction/user-approval/src/index.ts` — `ApprovalService.request/decide` (`222-309`). Stable cross-reference: `repository-map.md` §4 and `call-path.md` §3.
- Exact call path: `scheduler.prepare -> tools/pre-execute waterfall -> allow | deny | ask`; `ask -> serviceAsk -> ApprovalService.request/decide`; only `allowed-once -> guardReason -> dispatch`, while rejected/cancelled/unavailable paths fail closed.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; this card uses `35-X02` records `x02-allow`, `x02-deny`, `x02-ask`.
- Fixture / instrumentation / raw artifacts: shared harness with an in-memory sentinel Tool and ApprovalService answerer; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (`case=35-X02`), `combined-output.txt`, `command.txt`, source/patch and `manifest.txt`; Cycle 0 receipt `experiments/raw/35-x02-deny-ask.txt` remains non-accepted history.
- Observation: allow delegates and starts the sentinel once; deny and ask/rejected start it zero times; ask records one linked `approval/asked` and `approval/decided(rejected)` pair; all three terminate and correlate through Session and next projection.
- Counter-evidence / falsifier: a pinned independent vote aggregator would require a different owner-specific claim; any deny/ask body start, missing approval pair, or missing terminal/session/projection correlation would falsify this observation.
- Proves: the selected fixed path is a composition-ordered waterfall with optional approval and monotonic guard seam, not Article 06/Lab 02's vote merge.
- Does Not Prove / limitations: deployed listener order, a real approval UI/human decision, OS permission, sandbox safety or universal multi-policy priority.
- BuildPilot implication: record the ordered policy owner, decision and approval receipt independently of Registry visibility; proposal only.

### Final Evidence 35-E06｜Stage-owned errors and finalization

- Claim ID / Status / Class: `35-C06 / SOURCE_CONFIRMED + SELECTED_BRANCHES_OBSERVED / PINNED_SOURCE + RUNTIME_OBSERVATION`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/tools/src/index.ts` — `prepareExecution` (`1462-1505`), `dispatchToolBody` (`1526-1558`), `dispatchScheduledExecution` (`1561-1597`), `postExecute` (`1730-1780`), `createSuccessResult` (`1791-1821`), `finishScheduledExecution/materializeFinalResult` (`1622-1675`, `1823-1860`) and result observers; `packages/core/agent-loop/src/tool-calls.ts` — `appendToolCall/appendToolResult` (`248-288`). Stable cross-reference: `call-path.md` §1 `Pre policy / Execute / Normalize and finalize / Persist`.
- Exact call path: `prepare -> pre waterfall -> dispatchScheduledExecution -> execute waterfall -> definition.execute -> createSuccessResult -> postExecute -> finalizer/materialize -> tools/result observer -> append tool/call + tool/result`.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; this card uses the stage arrays for `35-X01` and `35-X02`.
- Fixture / instrumentation / raw artifacts: shared stage-observer harness; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (`case=35-X01` and `35-X02`), `combined-output.txt`, `command.txt`, source/patch and `manifest.txt`; Cycle 0 inputs `35-x01-bad-arguments.txt` and `35-x02-deny-ask.txt` are retained but non-accepted.
- Observation: selected valid/invalid/deny/ask calls expose distinct pre/execute/post/result traversals even when several terminal results are model-visible errors.
- Counter-evidence / falsifier: equal final error text is not a stage oracle; a full runtime branch table that shows unknown/pre/body/post/finalizer failures share one owner would narrow this claim. Missing or reordered observed stages would falsify the selected trace statement.
- Proves: source-defined stage ownership and runtime traversal for the selected X01/X02 branches.
- Does Not Prove / limitations: runtime coverage of every unknown-tool, pre-hook throw, body throw, post-hook throw or finalizer failure; post block cannot prove rollback of an earlier side effect.
- BuildPilot implication: terminal receipts should retain failure owner/stage instead of only a final error string; course mapping input only.

### Final Evidence 35-E07｜Cooperative timeout and caller cancellation

- Claim ID / Status / Class: `35-C07 / SOURCE_CONFIRMED + EXPERIMENT_CONFIRMED / PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/guard/timeout-policy/src/index.ts` — `apply` (`50-80`) and `TOOL_TIMEOUT`; `packages/core/tools/src/index.ts` — `dispatchToolBody` (`1508-1558`) and cancellation helpers; `packages/core/agent-loop/src/tool-calls.ts` — skipped/synthetic cancellation pairs (`248-258`). Stable cross-reference: `repository-map.md` §5 and `call-path.md` §4.
- Exact call path: timeout `tools/execute wrapper -> derived deadline signal -> delegated body -> wait for settle -> TOOL_TIMEOUT if wrapper deadline won`; cancel `caller signal -> fused execution signal -> started body drains to ABORTED | held call never dispatches and persists ABORTED_BEFORE_DISPATCH`.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; this card uses `35-X03` and `35-X04`.
- Fixture / instrumentation / raw artifacts: fake timers plus deferred in-memory latches, cap `1`, no sleeps or external effect; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (`case=35-X03/35-X04`), `combined-output.txt`, `command.txt`, source/patch, `environment-and-cleanliness.txt`, `manifest.txt`; Cycle 0 receipts `35-x03-timeout.txt` and `35-x04-caller-cancellation.txt` remain non-accepted.
- Observation: timeout signal is observed before cleanup release and no Session result exists before drain; after settle it becomes `TOOL_TIMEOUT`, while control succeeds. Cancellation starts only the first body, suppresses the held body, waits for cleanup, emits `ABORTED` / `ABORTED_BEFORE_DISPATCH`, and permits an independent follow-up.
- Counter-evidence / falsifier: result emission before cleanup release, an un-aborted timeout signal, held-body start, wrong terminal codes, or failed follow-up would falsify acceptance. An uncooperative/remote tool could continue despite signal and therefore limits any broader claim.
- Proves: selected timeout/cancel behavior is cooperative signal + drain + terminal classification.
- Does Not Prove / limitations: hard kill, rollback, remote quiescence, provider billing stop, external side-effect cancellation or run-level recovery/resume.
- BuildPilot implication: separate cancellation request, signal observation, drain and terminal receipts; do not model cancel as rollback. Proposal only.

### Final Evidence 35-E08｜Concurrency classification and ordered commit seam

- Claim ID / Status / Class: `35-C08 / SOURCE_CONFIRMED + DEPENDENCY_BOUNDED / PINNED_SOURCE + INFERENCE`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/tools/src/index.ts` — `executionMode` (`1268-1284`); `packages/core/agent-loop/src/tool-calls.ts` — `runGroup` (`121-245`) and ordered append path (`248-288`). Stable cross-reference: `repository-map.md` §5 `Concurrency` and `call-path.md` §4 `Scheduler`.
- Exact call path: `executeToolCalls -> tools.executionMode(call) -> exclusive barrier | bounded parallel pool -> scheduler.prepare in model order -> scheduler.dispatch may overlap -> finalize/finish + Session append in model order`.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; the bounded Article 35 observation is `35-X04` with `maxParallelToolCalls=1`.
- Fixture / instrumentation / raw artifacts: shared latch harness; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (`case=35-X04`), `combined-output.txt`, `command.txt`, source/patch and `manifest.txt`; `experiments/raw/35-x04-caller-cancellation.txt` remains the non-accepted Cycle 0 receipt.
- Observation: literal concurrency-safe classification enters the bounded scheduler seam; X04 confirms cap-one started/held separation and ordered durable pairs during cancellation. The broader reverse-settlement/model-order matrix remains Article 33 dependency evidence, not a new Article 35 runtime claim.
- Counter-evidence / falsifier: a pinned path that commits results by settlement order, or X04 showing the held body starts under cap one, would falsify this bounded statement.
- Proves: the Registry execution mode connects to a scheduler that separates dispatch eligibility from durable commit order; X04 observes only the cap-one negative boundary.
- Does Not Prove / limitations: all Article 33 concurrency cases anew, external side-effect ordering, transactional serialization or deployed scheduler configuration.
- BuildPilot implication: keep dispatch, settlement and durable commit as separate ledgers; this is dependency-bounded course mapping, not an adopted design.

### Final Evidence 35-E09｜Canonical value, model content, UI meta and persistence

- Claim ID / Status / Class: `35-C09 / SOURCE_CONFIRMED + EXPERIMENT_CONFIRMED_FOR_SESSION_AND_NEXT_MODEL_VIEW / PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/core/tools/src/index.ts` — `createSuccessResult/materializeFinalResult` (`1791-1860`); `packages/core/agent-loop/src/tool-calls.ts` — `appendToolCall/appendToolResult` (`248-288`); `packages/core/agent-loop/src/agent.ts` — `session.deriveMessages` use (`353`); `packages/client/ui-chat/src/client/conversation-nodes/tool.ts` — `rootCall/rootResult` (`39-68`) and `toolDefinition` (`230-264`); `register.ts` — `registerConversationNodes` (`21-35`). Stable cross-reference: `repository-map.md` §6 and `call-path.md` §1 `Normalize / Persist / Model / UI projection`.
- Exact call path: `canonical body value -> output validation -> render model ContentBlock[] + optional presentationMeta -> post/finalize -> append tool/call(raw args) + tool/result(content/error/meta) -> session.deriveMessages -> next request`; separately, client conversation projection pairs durable events by `callId`.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; every `35-X01—X05` record participates.
- Fixture / instrumentation / raw artifacts: shared in-memory Session/MockAdapter harness; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (all `13` records), `combined-output.txt`, `command.txt`, source/patch and `manifest.txt`; Cycle 0 case files remain historical non-accepted inputs.
- Observation: every accepted call has one durable call/result pair and one matching next-request plus derived-history result; Session and next-history content hashes agree. Source keeps canonical value, model content, optional UI meta, persisted event and additional context as separate lanes.
- Counter-evidence / falsifier: missing/duplicate event pairs, mismatched next-history hashes, or a generic persisted canonical-value field on the pinned path would change this claim.
- Proves: selected final model content is persisted and enters the next model view; source defines a separate client projection seam.
- Does Not Prove / limitations: arbitrary canonical values are persisted, a real Provider received the next request, actual client UI rendered the card, or every Host uses this client package.
- BuildPilot implication: use separate references for canonical value, model content, presentation and persistence instead of one universal result payload; proposal only.

### Final Evidence 35-E10｜Optional spill, bounded preview and exact fallback

- Claim ID / Status / Class: `35-C10 / SOURCE_CONFIRMED + EXPERIMENT_CONFIRMED_FOR_OPT_IN_SPILL / PINNED_SOURCE + RUNTIME_OBSERVATION + EXPERIMENT`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / symbols: `packages/spill/spill-policy/src/index.ts` — `apply` (`110-231`), `spillReplacement` (`130-187`) and `tools/ptc-dispatch-log` handler (`211-231`); `packages/compaction/compaction-tool-result-pruner/src/index.ts` — `ToolResultPruner.pruneSession` (`124-184`). Stable cross-reference: `repository-map.md` §7 and `call-path.md` §5.
- Exact call path: composed `tools/post-execute -> SpillPolicy.apply -> oversized all-text result -> SpillStore.saveText(full) -> bounded head/tail preview + locator -> final result`; missing/failing storage `-> preserve original successful inline result`. Separate post-persistence prune `-> compaction/prune + replacement tool/result`.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`; this card uses `35-X05` records `x05-small`, `x05-spill`, `x05-fallback`.
- Fixture / instrumentation / raw artifacts: `RecordingSpillStore` in memory with cap `200`, successful synthetic locator and injected failure; `experiments/raw/recovery-cycle-1/a35-recovery-traces.jsonl` (`case=35-X05`), `combined-output.txt`, `command.txt`, source/patch and `manifest.txt`; Cycle 0 `experiments/raw/35-x05-large-result.txt` remains non-accepted.
- Observation: `4` bytes stay inline without a save; `1,600` bytes save with equal full/stored hash and yield a `200`-byte preview plus `/spill/big-ok.txt`; injected failure for `1,000` bytes retains the exact inline hash/length. All records state `semanticSummary:false`.
- Counter-evidence / falsifier: automatic spill without policy composition, content loss/error on injected storage failure, unequal save hash, preview above cap, or any semantic-summary operation would falsify the bounded claim.
- Proves: the selected opt-in in-memory path supports full-save + bounded preview/locator and source-defined successful inline fallback.
- Does Not Prove / limitations: universal spill, production storage, retention, authorization, later availability/retrieval UI, semantic LLM summary, or compaction execution in this experiment.
- BuildPilot implication: if a later design adopts spill, record full-content digest/reference and fallback disposition separately from model preview; adoption remains undecided.

### Final Evidence 35-E11｜Five required SAME-CALL negative traces

- Claim ID / Status / Class: `35-C11 / EXPERIMENT_CONFIRMED / EXPERIMENT`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`.
- Exact source / call surface: preserved instrumentation `experiments/raw/recovery-cycle-1/article-35-same-call-recovery.spec.ts` composes pinned `LlmRuntime -> SessionStore -> SystemPrompt -> ToolRuntime -> optional ApprovalService/timeoutPolicy/SpillPolicy -> AgentRegistry -> AgentLoop`, with repo-owned `packages/core/agent-loop/tests/mock-adapter.ts`; per-call source paths are the exact E04—E10 paths above.
- Exact experiment command / exit: shared accepted command above, exit `0`, selected `1 file / 5 tests`, `35-X01=3`, `X02=3`, `X03=2`, `X04=2`, `X05=3` records.
- Fixture / instrumentation / raw artifacts: the shared fixed fixture and temporary untracked test; exact source/patch, `command.txt`, `combined-output.txt`, `a35-recovery-traces.jsonl`, `environment-and-cleanliness.txt`, `manifest.txt`, plus preserved Attempt 1 source/patch/output. Cycle 0 raw files `35-x01-*.txt`—`35-x05-*.txt` remain `NOT_ACCEPTED`.
- Observation: all five frozen negative categories satisfy their SAME-CALL acceptance with unique callIds, complete required schema, one Session call/result and one next-request/derived-history match per record; manifest hashes revalidate. The accepted run did not overwrite the two failed histories.
- Counter-evidence / falsifier: any selected count other than five, nonzero exit, missing required JSONL field, duplicate callId, mismatched content hash, dirty post-cleanup fixture, or a failed individual X01—X05 criterion makes the experiment `NOT_ACCEPTED`.
- Proves: the five bounded source experiments passed exactly at the pinned fixture and instrumentation boundary.
- Does Not Prove / limitations: whole-repository test health, real Provider, production service/deployment, production Tool or side effect, actual client UI, credentialed environment, security certification or cross-platform guarantee.
- BuildPilot implication: the five-case receipt shape is reusable as a future acceptance-test candidate, but it is not a BuildPilot architecture/runtime artifact.

### Final Evidence 35-E12｜BuildPilot ToolExecutionReceipt proposal

- Claim ID / Status / Class: `35-C12 / PROPOSAL / DEFER / COURSE_PROPOSAL`.
- Repository / tag / full commit: official `https://github.com/deepseek-ai/deepseek-harness`, `dsh-v0.1.2-alpha.1`, `cd5ef8148158c3a752a658978873241fdf8e2bbc`; this is the proposal's evidence input, not the owner of the proposed type.
- Exact basis / stable references: final `35-E02—E11`, `draft.md` §20, and the unresolved separation among Registry, validation, policy, execution terminal, model content, persistence and spill references.
- Proposal: a future `ToolExecutionReceipt` may record correlation identity, tool/scope reference, raw-argument digest, validation and policy disposition, body start/settle, typed terminal, model-content/session/spill references and redacted diagnostics without copying secrets or arbitrary canonical values by default.
- Counter-evidence / falsifier: one universal receipt could collapse Host/model/UI ownership or create a sensitive-data aggregation point; any later ADR/security review may simplify or reject it.
- Proves: `N/A` — this is a course design candidate only.
- Does Not Prove / limitations: a DSH built-in API, BuildPilot Architecture/ADR/Design v1/code/runtime, approved schema, safety posture or implementation completion.
- BuildPilot implication / decision: `DEFER` to Article 37 decision matrix and future Part VII authorization; no adoption is made in Article 35.

## Evidence Gate recommendation

`EVIDENCE_MERGE PASS / NEXT REQUIRED GATE: EVIDENCE_GATE`。

12 个 Claims / 12 张 Evidence Cards 均有最终 disposition。`35-X01—X05` required traces 已关闭；Cycle 0 与 Recovery Attempt 1 的失败记录保持可见。Evidence Gate 可以据此判定 bounded Article 35 claims，但必须把 typed-vs-raw registration、waterfall-vs-vote、cooperative timeout/cancel、optional spill/fallback、no semantic summary、no actual Provider/UI/production guarantee 作为不可删除的写作边界。
