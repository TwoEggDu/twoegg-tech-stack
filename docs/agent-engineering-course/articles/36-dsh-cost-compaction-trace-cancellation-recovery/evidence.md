# Article 36 Evidence｜Preliminary Research Register

Status: `PRELIMINARY EVIDENCE / SOURCE_MAP NOT STARTED / EXPERIMENT NOT EXECUTED`

## Evidence posture

- Frozen object：official `deepseek-ai/deepseek-harness`，tag `dsh-v0.1.2-alpha.1`，commit `cd5ef8148158c3a752a658978873241fdf8e2bbc`。
- Research-time fixture receipt：`HEAD == tag^{commit}`；working tree clean；Node `v24.18.1`；pnpm `11.7.0`。
- Official posture：Developer Preview；not security-audited；not secure/production-ready。
- Claim count：`17` preliminary cards。
- Current distribution：`1 DOC_CONFIRMED / 2 PARTIAL_DEPENDENCY_ONLY / 1 PROPOSAL / 13 BLOCKED_SOURCE_MAP_OR_EXPERIMENT`。
- Runtime Observation：`ABSENT`。
- Required source experiment：`36-X01 FROZEN_DESIGN / NOT EXECUTED`。
- Evidence Gate：`NOT ELIGIBLE` until Source Map/Call Path and accepted long-session/compaction/cancel/resume Trace are merged。

> 这些卡是 Research Gate 的时间快照。Source Investigator 与 Lab Engineer 后续只能追加可追踪 disposition；不得把本节的 `BLOCKED/PARTIAL/NOT EXECUTED` 倒改成“当时已经确认”。

## Shared source priority and fixed boundary

1. pinned DSH source、tests、examples、docs；
2. pinned tag对应official release material；
3. DSH direct dependencies的official docs/source；
4. primary papers/standards；
5. secondary material只作线索。

所有核心源码卡最终必须包含 repository/tag/full commit、file/symbol/stable anchor、caller/callee/call path、source observation、runtime command/exit/stdout/stderr或Trace、fixture/mock/instrumentation/environment、counter-evidence/falsifier、claim status/limitation，以及明确标注为课程映射的BuildPilot implication。

## Preliminary Evidence 36-E01｜Pinned identity and safety ceiling

- Claim ID / Status / Class：`36-C01 / DOC_CONFIRMED / OFFICIAL_DOC + PINNED_IDENTITY`。
- Repository / Tag / Commit：`https://github.com/deepseek-ai/deepseek-harness` / `dsh-v0.1.2-alpha.1` / `cd5ef8148158c3a752a658978873241fdf8e2bbc`。
- Source：fixed checkout `README.md`、`SAFETY.md`、`AGENTS.md`；read-only Git/toolchain probe。
- Observation：HEAD与peeled tag相等；status无输出；README/SAFETY限定developer-preview、no security audit、not production-ready；Node/pnpm为`v24.18.1 / 11.7.0`。
- Counter-evidence / falsifier：HEAD/tag不相等、dirty fixture或tag漂移会阻断；selected tests通过不能推翻official posture。
- Proves：本篇研究输入身份、当前cleanliness和安全语态上限。
- Does Not Prove / limitations：build/test/runtime、usage、compaction、telemetry、cancel/resume、security、production readiness。
- Runtime Trace：`N/A — identity probe only`。
- Course Usage：全篇版本banner与每张source card的上限。

## Preliminary Evidence 36-E02｜Cross-cutting Hook Map

- Claim ID / Status / Class：`36-C02 / BLOCKED_SOURCE_MAP / PINNED_SOURCE`。
- Candidate source：`core/agent-loop`、`core/tools`、`llm`、`session-checkpoint-policy`、compaction、Session append/persistence seams。
- Observation：Research discovery找到多个candidate lifecycle events/listeners；未闭合registration order、caller/callee、short-circuit或error/cancel path。
- Counter-evidence / falsifier：同名event可能由不同composition owner挂载；package存在不等于目标profile mounted。
- Proves：`NOTHING YET` beyond a bounded investigation plan。
- Does Not Prove / limitations：request前/model后/tool前后/session write真实顺序或runtime traversal。
- Required closure：Hook Map + exact source path + representative ordering trace。
- BuildPilot implication：未来control-plane receipt应按hook owner分账；`COURSE_PROPOSAL ONLY`。

## Preliminary Evidence 36-E03｜Usage fields and provenance

- Claim ID / Status / Class：`36-C03 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Candidate source：`packages/llm/token-meter`、LLM stream/message usage fields、Session projection consumers。
- Observation：candidate package/doc vocabulary区分provider usage、heuristic measurement、pressure和projection；未由Source Investigator闭合字段来源与consumer。
- Counter-evidence / falsifier：UI total、estimator值或route price不能替代provider-native receipt；adapter可能省略字段。
- Proves：`NOTHING YET` about pinned runtime values。
- Does Not Prove / limitations：exact token count、billing、cost、complete retry usage或real provider behavior。
- Required closure：field/provenance matrix + request/attempt/session correlation + raw samples。

## Preliminary Evidence 36-E04｜Attempt accumulation and retry accounting

- Claim ID / Status / Class：`36-C04 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Candidate source：token-meter usage projection/turn fold、assistant chunk/message、LLM retry events。
- Observation：Research发现candidate fold/retry files；尚未确认same-attempt replacement、new-attempt accumulation或malformed/incomplete handling。
- Counter-evidence / falsifier：stream sample与final message可能重复；retry可能产生另一个chargeable attempt；单一最终total无法说明算法。
- Proves：`NOTHING YET`。
- Does Not Prove / limitations：billing completeness、provider invoice、failed/cancelled attempt的收费真值。
- Required closure：exact fold transitions + retry raw trace + contradictory/missing usage negative。

## Preliminary Evidence 36-E05｜Measurement vs Budget policy

- Claim ID / Status / Class：`36-C05 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + DEPENDENCY`。
- Dependency observation：Article 20已建立measurement不是Budget；Article 33在pinned source中给出“no built-in turn budget”的bounded finding。
- Candidate source：Agent options、turn-stopping、token-meter consumers、guard/policy packages、profile configuration。
- Counter-evidence / falsifier：context capacity、max output tokens、parallel cap、tool timeout或compaction threshold都不能自动称为Run Token/Step/Cost/Latency Budget。
- Proves：只证明必须做presence/absence search；不证明Article 36结果。
- Does Not Prove / limitations：DSH有或没有任何完整Budget owner；source search未完成。
- Required closure：policy/decision/stop point/activation matrix或bounded absence。

## Preliminary Evidence 36-E06｜Stop point and overspend window

- Claim ID / Status / Class：`36-C06 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Research hypothesis：若usage只在response/stream后确定，则任何只读该usage的check都有至少一attempt的information-lag；实际window必须由source order证明。
- Counter-evidence / falsifier：preflight estimator、provider hard cap或pre-dispatchpolicy可能改变window；不能先写“一定超一轮”。
- Proves：`NOTHING YET`；这是待反驳假设。
- Does Not Prove / limitations：overspend amount、billing loss、stop guarantee、race freedom。
- Required closure：measurement/check/dispatch/receipt order + threshold-cross trace + explicit terminal。

## Preliminary Evidence 36-E07｜Compaction trigger and owner split

- Claim ID / Status / Class：`36-C07 / BLOCKED_SOURCE_MAP / PINNED_SOURCE`。
- Candidate source：`packages/compaction/compaction`、`compaction-basic`、`command-compact`、token-meter、Agent pre-step/request-error seams。
- Observation：fixed checkout含candidate `pressure`、`context-overflow`与manual surfaces；未闭合activation、threshold、selection、summarizer与continuation paths。
- Counter-evidence / falsifier：直接调用`compactNow`不能证明automatic trigger；package/test存在不能证明target composition mounted。
- Proves：`NOTHING YET` about runtime trigger。
- Does Not Prove / limitations：所有Host默认启用、成功摘要、成本下降或稳定性。
- Required closure：trigger/caller/config/activation table + source path。

## Preliminary Evidence 36-E08｜Compaction is not Memory

- Claim ID / Status / Class：`36-C08 / PARTIAL_DEPENDENCY_ONLY / DEPENDENCY + PINNED_SOURCE + EXPERIMENT`。
- Dependency observation：Article 34已在同baseline确认compaction facts append且current surface由replacement改变；raw old events保留。
- Current gap：Article 36尚未验证trigger、summary input/output、usage、request diff、rehydration或semantic fidelity。
- Counter-evidence / falsifier：summary message持久化不等于长期知识、truth maintenance或Evidence verification preserved。
- Proves：只可继承`append+replace`依赖边界，不可提前写A36主张。
- Does Not Prove / limitations：Memory、semantic equivalence、compression quality、cost saving、generic evidence preservation。
- Required closure：summary transaction + before/after model/projection trace。

## Preliminary Evidence 36-E09｜History / Projection / request diff

- Claim ID / Status / Class：`36-C09 / PENDING_EXPERIMENT / PINNED_SOURCE + EXPERIMENT`。
- Candidate views：raw Session log、current surface、`deriveMessages()` History、next provider request、token/context/session projections、telemetry copy。
- Observation：no Article 36 before/after snapshot exists。
- Counter-evidence / falsifier：token count下降或UI变短不能证明request改变；raw preservation不能证明projection equality。
- Proves：`NOTHING YET`。
- Does Not Prove / limitations：rehydration、cache reuse、semantic completeness或provider receipt。
- Required closure：same-session normalized pre/post diff with source ranges/digests。

## Preliminary Evidence 36-E10｜Rehydration and projection state

- Claim ID / Status / Class：`36-C10 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Candidate source：Session persistence/load、projection registry/checkpoint、token-meter/session-stats folds、AgentLoop resume。
- Observation：Article 34已确认same-identity Resume append，但A36 usage/compaction/projection rehydration未测。
- Counter-evidence / falsifier：in-memory cache surviving同process不能证明fresh process rehydration；recompute不等于persisted snapshot。
- Proves：`NOTHING YET` for A36 state restoration。
- Does Not Prove / limitations：credential/approval/Budget/external state inheritance或crash consistency。
- Required closure：load/reducer/checkpoint path + fresh-runtime prefix/state diff。

## Preliminary Evidence 36-E11｜Session event vs telemetry vs Trace correlation

- Claim ID / Status / Class：`36-C11 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Candidate source：Session event envelope、session-telemetry coordinator、OTel logs backend、runtime diagnostics。
- Observation：candidate docs describe event projection/redaction/handoff/export; no complete Agent Trace correlation has been demonstrated。
- Counter-evidence / falsifier：one record per Session event、OTel SDK或`trace` label都不能自动提供run/attempt causality、delivery或Replay。
- Proves：`NOTHING YET` beyond separate surfaces to investigate。
- Does Not Prove / limitations：collector delivery、at-least-once、redaction safety、complete payload、root cause或deterministic Replay。
- Required closure：identity/correlation matrix + capture/export call path + joined raw trace。

## Preliminary Evidence 36-E12｜Cancellation propagation and durable terminal

- Claim ID / Status / Class：`36-C12 / PARTIAL_DEPENDENCY_ONLY / DEPENDENCY + PINNED_SOURCE + EXPERIMENT`。
- Dependency observation：A33确认Agent turn signal spine与durable aborted balance；A35确认Tool timeout/cancel cooperative drain。两者都排除hard kill/rollback/remote quiescence。
- Current gap：long Session/compaction/model/tool/checkpoint/resume integrated propagation尚未执行。
- Counter-evidence / falsifier：signal aborted、`turn/end(aborted)`或error result都不单独证明Provider/Tool/summary settled或external effect stopped。
- Proves：只继承“cancel不是rollback”的课程/依赖边界。
- Does Not Prove / limitations：run-level recovery、hard kill、remote cancel、billing stop或production Tool safety。
- Required closure：request -> observer -> settle -> durable terminal -> resume trace。

## Preliminary Evidence 36-E13｜Checkpoint policy and interruption recovery

- Claim ID / Status / Class：`36-C13 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Candidate source：`packages/session/session-checkpoint-policy`、persistence coordinator/backends、recovery owner tests。
- Observation：fixed package docs claim pre-request、pre-top-level-tool与pre-step durability barriers以及unknown-outcome repair；Source Map/experiment未闭合。
- Counter-evidence / falsifier：documentation或test name不能证明target profile activation；durable call intent不能证明effect committed/exactly-once。
- Proves：`NOTHING YET` about selected runtime。
- Does Not Prove / limitations：per-chunk durability、remote state、automatic safe retry、cross-backend/crash guarantee。
- Required closure：listener registration -> flush -> body/adapter dispatch -> recovery append -> resume path + interruption case。

## Preliminary Evidence 36-E14｜Resume and external-effect boundary

- Claim ID / Status / Class：`36-C14 / BLOCKED_SOURCE_MAP / PINNED_SOURCE + EXPERIMENT`。
- Candidate source：AgentLoop resume、Session recovery/persistence、Tool unknown-outcome representation。
- Observation：no A36 fresh-runtime cancel/resume trace or external sentinel exists。
- Counter-evidence / falsifier：same Session id/History continuation does not prove remote transaction resumed, rolled back or reconciled。
- Proves：`NOTHING YET`。
- Does Not Prove / limitations：exactly-once、rollback、compensation、effect lookup、credential/approval continuity。
- Required closure：same-prefix resume + effect status `KNOWN/UNKNOWN` receipt + no optimistic retry。

## Preliminary Evidence 36-E15｜Cost estimate is not billing truth

- Claim ID / Status / Class：`36-C15 / BLOCKED_SOURCE_MAP / DEPENDENCY + PINNED_SOURCE_SEARCH`。
- Dependency observation：Article 20区分estimate/reservation/incurred-pending/source-qualified actual；Provider usage与billing是不同surface。
- Candidate source：token-meter route pricing/usage projections、session telemetry；需bounded search for billing/cost owner。
- Counter-evidence / falsifier：usage × public/current price、heuristic route price或UI cost label不能冒充invoice/billed actual。
- Proves：课程语义边界；不证明DSH具体实现。
- Does Not Prove / limitations：actual cost、currency/tier/discount/cache/non-token items、provider invoice finality。
- Required closure：source-qualified cost/billing surface或`BOUNDED ABSENCE`；本篇禁止真实billing read/credential。

## Preliminary Evidence 36-E16｜Required integrated source experiment

- Claim ID / Status / Class：`36-C16 / FROZEN_DESIGN_NOT_EXECUTED / EXPERIMENT`。
- Design source：`research.md §10`，experiment ID `36-X01`。
- Required path：`long Session -> usage accumulation -> source-defined compaction -> before/after request diff -> cancel -> durable terminal -> fresh resume`。
- Required correlation：Context / Model / Tool / Policy / Error / Usage，绑定session/event seq/turn/step及可用attempt/call/compaction identities。
- Observation：`NONE`；Researcher没有运行命令或创建raw artifact。
- Counter-evidence / falsifier：任何关键observer缺失、0 selected、直接调用private compaction method、旧runtime未销毁、真实Provider/外部写入或fixture不clean均拒绝acceptance。
- Proves：only that a falsifiable design now exists。
- Does Not Prove / limitations：任何runtime behavior、performance、cost saving、reliability或production safety。
- Required closure：Lab Engineer exact command/exit/stdout/stderr/raw trace/manifest + Researcher Evidence Merge。

## Preliminary Evidence 36-E17｜RunControlReceipt proposal

- Claim ID / Status / Class：`36-C17 / PROPOSAL / COURSE_PROPOSAL`。
- Proposal：未来Harness可候选使用`RunControlReceipt`分账`usageRef / budgetDecisionRef / compactionRef / traceRef / cancellationRef / checkpointRef / externalEffectStatus`。
- Source basis：only the unresolved seams registered in `36-E02—E16`。
- Counter-evidence / falsifier：如果未来Article 37/Part VII选择不同owner或证明字段重复，应`SIMPLIFY/REJECT`，不能把这张卡当DSH API。
- Proves：`N/A — design discussion candidate only`。
- Does Not Prove / limitations：DSH built-in type、BuildPilot ADR/Architecture/Design v1/code/runtime或approved schema。
- Course usage：只允许作为Article 37 decision matrix输入候选；本文不展开Part VII。

## Evidence gaps required before merge

| Gap | Required artifact | Fail-closed result if absent |
|---|---|---|
| Hook ownership/order | `repository-map.md` + `call-path.md` | `BLOCKED_EVIDENCE` |
| Usage fields/fold/retry | source matrix + accepted raw samples | `BLOCKED_EVIDENCE` |
| Budget/stop/overshoot | source decision/absence map + threshold trace | narrow to measurement-only or `BLOCKED_EVIDENCE` |
| Compaction trigger/summary | source path + activation + transaction trace | `BLOCKED_EVIDENCE` |
| History/request/projection diff | `36-X01` before/after artifacts | `BLOCKED_EVIDENCE` |
| Trace correlation | joined identity matrix/raw records | `BLOCKED_EVIDENCE` |
| Cancel/resume | cooperative settle + durable terminal + fresh resume trace | `BLOCKED_EVIDENCE` |
| External effect | explicit `KNOWN/UNKNOWN/NOT_RUN` receipt | no rollback/recovery claim |
| Billing | source-qualified record or bounded absence | estimate/unknown only |
| Fixture integrity | before/after HEAD/status/diff + instrumentation manifest | experiment rejected |

## Evidence Gate recommendation

`NOT READY / BLOCKED PENDING SOURCE_MAP + REQUIRED SOURCE EXPERIMENT`。

Research Gate已经冻结17张preliminary cards、反证和`36-X01` acceptance，但没有一项Article 36核心行为取得`SOURCE_CONFIRMED`或`RUNTIME_CONFIRMED`。下一合法Gate是`SOURCE_MAP`；缺少Usage验证、long Session、Compaction、Cancel/Resume或joined raw Trace时不得进入Outline/Draft/Publish。

## Experiment Design disposition｜36-X01

Status: `SOURCE_MAP PASS / EXECUTABLE DESIGN FROZEN / NOT EXECUTED`

- Durable design：[36-X01 Long Session → Compaction → Cancel → Fresh Resume](experiments/36-x01-long-session-compaction-cancel-resume.md)。
- Temporary spec：`packages/compaction/compaction-basic/tests/36-x01-long-session-compaction-cancel-resume.spec.ts`。
- Integrated scope：`1 harness / 1 sequential case / P0—P5 / 180s`。
- Production owners：Agent Loop、Session/Jsonl Persistence、Session Projection Registry、TokenMeter、session-checkpoint-policy、BasicCompaction automatic pre-step trigger、Tools waterfall。
- Instrumentation：single deterministic adapter（含conversation与compaction branches）、request ordinal、Tool latch、test-owned sentinel receipt、SHA-256 digests；全部显式标为fixture instrumentation；不override compaction summarizer owner。
- Required runtime outputs：usage fold/pressure、complete compaction bracket、before/after raw/history/request/projection diff、cancel request/observed/settled/terminal、first-runtime dispose、fresh Context resume、external status `KNOWN/UNKNOWN`、closed correlation JSONL、patch/hash/cleanup receipts。
- Safety：no real Provider、credential、network、server、production config/data/write；temporary spec与OS temp files执行后清理。

这次Gate只证明Source Map足以形成不改变§10 acceptance的安全可执行方案。`36-E03—E16` 的runtime side仍保持`PENDING/BLOCKED_EVIDENCE`；只有Lab Engineer真实执行、保存exact command/exit/stdout/stderr/raw trace且fixture恢复exact pinned clean后，Researcher才可进入Evidence Merge。
