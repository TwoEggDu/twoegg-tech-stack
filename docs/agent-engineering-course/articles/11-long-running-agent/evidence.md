# Article 11 Evidence｜Long-running Agent

## Gate status

- Research：`COMPLETE`
- Preliminary Evidence：`COMPLETE`
- Lab 04 Design：`FROZEN / IMPLEMENTED / EXECUTED`
- Lab 04 Execution：`COMPLETE`
- Lab 04 Observation：`COMPLETE / PASS`
- Evidence Merge：`COMPLETE`
- Evidence Gate：`PASS`
- Claim traceability：`9 / 9`
- Core `BLOCKED`：`0`
- Next allowed Gate：`OUTLINE`

> `Expected Observable != Observed Result`仍然有效。本次升级只来自execution log、process evidence与LR-01—08 raw artifacts，不是因为verifier green或README Expected。所有Claim都保留fixture / product / course-schema ceiling。

## Preliminary Evidence summary

| Evidence Class | What is available | Current boundary |
|---|---|---|
| Specification | RFC 9110 §9.2.2 idempotent method / lost-response retry语义 | 只覆盖HTTP intended effect，不提供通用idempotency store或exactly-once |
| Official .NET docs | cancellation是requester / listener cooperative contract | cancellation request不证明listener已停止、rollback或checkpoint |
| Official product docs | LangGraph checkpoint / replay / task idempotency；AWS Standard Workflow redrive | 产品边界彼此不同，不构成universal recovery algorithm |
| Repository dependencies | Article 06 / 08 / 09 / 10的Tool side effect、committed Step、Plan / State / Workflow边界 | 没有Article 11的cancel / resume / retry / recovery runtime evidence |
| Frozen experiment + raw observation | Lab 04 Design、execution log、process evidence、LR-01—08 checkpoint / trace / partial result / case result / fake store | Observed已合并；只支持frozen local fixture，不支持production外推 |

## Evidence Cards

### Evidence 11-E01｜Cooperative cancellation boundary

- Article：`11 Long-running Agent`
- Claim ID：`11-C01 / 11-C05`
- Claim：Cancellation request、listener observation、work stopped与side-effect rollback是不同事实。
- Evidence Status：`PARTIAL`
- Lab Dependency：`REQUIRED`
- Evidence Class：`OFFICIAL_DOC`
- Source：[Microsoft Learn: Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
- Locator：overview；`Listening and Responding to Cancellation Requests`
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：current .NET hosted docs；page updated `2026-03-17`
- Raw Observation：文档把取消描述为由requester发出request、listener负责观察并及时响应的cooperative model；listener决定怎样清理和终止。
- Interpretation：收到token request只证明请求已发出；只有listener trace才能证明它在何处观察并响应。外部副作用是否完成或回滚需要额外合同。
- Counter-evidence：Article 06 timeout case只证明fixed handler gate与terminal origin，不证明Article 11 resume；强制thread termination也不是本Lab边界。
- Proves：取消不是隐含强制kill；request与response需要分别记录。
- Does Not Prove：Lab 04取消后恢复、所有I/O及时取消、外部副作用回滚。
- Limitations：不覆盖remote process、uncooperative third-party I/O、cancel/timeout race。
- Course Usage：取消传播、最后安全状态、Timeout != Cancellation。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E02｜HTTP idempotency and lost response

- Article：`11 Long-running Agent`
- Claim ID：`11-C03 / 11-C04`
- Claim：通信在读取响应前失败时，只有known-idempotent语义或可证明原请求未应用，才有自动retry依据。
- Evidence Status：`PARTIAL`
- Lab Dependency：`REQUIRED`
- Evidence Class：`OFFICIAL_DOC`
- Source：[RFC 9110: HTTP Semantics §9.2.2](https://www.rfc-editor.org/rfc/rfc9110.html#name-idempotent-methods)
- Locator：§9.2.2 paragraphs on intended effect and automatic retry after connection failure
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：RFC 9110，June 2022，HTTP semantics
- Raw Observation：RFC把method定义为idempotent，当multiple identical requests对server的intended effect与single request相同；lost response后可retry idempotent request，即使original可能成功；非幂等method不应自动retry，除非已知其语义实际幂等或能检测original从未应用。
- Interpretation：lost response不是“未执行”证据；Retry eligibility依赖effect semantics / detection，而不是异常名或剩余budget。
- Counter-evidence：RFC允许server为每次idempotent request单独记录日志等非幂等旁路效果，直接反驳`idempotent = exactly-once everything`。
- Proves：幂等性影响lost-response后的自动retry安全判断。
- Does Not Prove：通用idempotency-key schema、durable dedup store、Lab fake store行为、distributed exactly-once。
- Limitations：HTTP method语义不能机械外推到所有Tool或workflow action。
- Course Usage：Retry decision table、lost-response unknown窗口、Idempotency != exactly-once。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E03｜LangGraph resume boundary and side-effect replay risk

- Article：`11 Long-running Agent`
- Claim ID：`11-C02 / 11-C04 / 11-C06`
- Claim：Resume从checkpoint boundary继续且可能replay forward；未完成task或checkpoint外side effect可能重新执行。
- Evidence Status：`PARTIAL`
- Lab Dependency：`REQUIRED`
- Evidence Class：`OFFICIAL_DOC`
- Source：[LangGraph Functional API](https://docs.langchain.com/oss/python/langgraph/functional-api)
- Locator：Tasks；Durable execution；Determinism；Idempotency；Handling side effects
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：current hosted Python docs；package / tag / source commit未固定
- Raw Observation：文档说明task results保存进checkpoint；resume不是从原代码行继续，而是从checkpoint boundary replay forward并恢复已完成task / subgraph结果；若task开始但未成功完成，resume可再次执行；建议把side effects放入task并设计成幂等。
- Interpretation：Resume != exact instruction continuation；side-effect identity与in-flight completion状态是恢复安全的必要调查对象。
- Counter-evidence：同一文档说明已持久化task result可被恢复而不重算，因此“resume总会从头重做一切”同样错误。
- Proves：该产品当前文档范围的resume / replay与duplicate risk。
- Does Not Prove：课程Lab schema、所有LangGraph版本、其他workflow runtime、production durability。
- Limitations：未做package compatibility run或pinned-source mapping。
- Course Usage：Resume != Replay；Checkpoint grain；side-effect task boundary。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E04｜LangGraph checkpoint fields, pending writes, replay and durability

- Article：`11 Long-running Agent`
- Claim ID：`11-C02 / 11-C06`
- Claim：Checkpoint具备thread identity与StateSnapshot字段；pending writes、replay与durability mode共同限定该产品的恢复边界。
- Evidence Status：`PARTIAL`
- Lab Dependency：`REQUIRED`
- Evidence Class：`OFFICIAL_DOC`
- Source：[LangGraph Checkpointers](https://docs.langchain.com/oss/python/langgraph/checkpointers)
- Locator：Why use checkpointers > Pending writes；Core concepts > Threads / Checkpoints / Super-steps；Get and update state > StateSnapshot fields / Replay；Durability modes
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：current hosted Python docs；package version未固定
- Raw Observation：文档以`thread_id`作为checkpoint的存取身份；checkpoint是super-step上的graph-state snapshot，`StateSnapshot`列出`values / next / config / metadata / created_at / parent_config / tasks`。pending writes保存同一super-step中已完成node的写入，使恢复时不重跑这些成功node；replay跳过checkpoint前的nodes，并重执行其后的nodes、LLM calls、API requests与interrupts。`exit / async / sync` durability modes在中间状态保存、crash风险与性能之间有不同取舍。
- Interpretation：可恢复性依赖identity、snapshot字段、boundary、pending work与明确durability policy，而不是“有一个state JSON”；replay后的调用仍需单独处理side-effect safety。
- Counter-evidence：pending writes可避免重跑同一super-step中已成功node，但Replay段又明确checkpoint后的calls会重执行，因此“resume全部重跑”与“resume绝不重跑”都错误。
- Proves：该产品当前文档范围的checkpoint字段、pending writes、Replay与durability mode语义。
- Does Not Prove：Article 11 exact checkpoint schema、任一mode的production durability guarantee或side-effect safety。
- Limitations：不覆盖storage failure、concurrent writer、retention或production configuration。
- Course Usage：Checkpoint fields、pending writes、Replay边界与durability取舍。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E04B｜LangGraph persistence and memory boundary

- Article：`11 Long-running Agent`
- Claim ID：`11-C09`
- Claim：同一产品的checkpointer可同时服务thread-scoped state、short-term memory与fault tolerance，而Store服务cross-thread long-term data；Checkpoint / Memory不能只按存储组件名区分。
- Evidence Status：`CONFIRMED / PRODUCT-DOC-SCOPED`
- Lab Dependency：`NOT_REQUIRED`
- Evidence Class：`OFFICIAL_DOC`
- Source：[LangGraph Persistence overview](https://docs.langchain.com/oss/python/langgraph/persistence)
- Locator：Persistence；Checkpointer vs. store
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：current hosted Python docs；package version未固定
- Raw Observation：overview把checkpointer描述为保存thread graph state、提供thread-scoped short-term memory与fault tolerance；Store保存graph state之外的application-defined data，用于cross-thread long-term memory。
- Interpretation：Checkpoint / Memory在实现上可重叠，但课程仍按是否回答control position、continuation与in-flight uncertainty区分证明职责。
- Counter-evidence：产品自身把checkpointer用于memory，反驳`Checkpoint和Memory必然是两个物理系统`。
- Proves：该产品当前overview中的checkpointer / Store与memory scope边界。
- Does Not Prove：checkpoint字段、pending writes、Replay、durability mode细节，或`Checkpoint != Memory`是行业统一taxonomy。
- Limitations：current hosted overview；无package / source commit pin；不覆盖Memory质量。
- Course Usage：Checkpoint / Memory实现可重叠、证明职责不同。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E05｜AWS product-specific redrive

- Article：`11 Long-running Agent`
- Claim ID：`11-C01 / 11-C06`
- Claim：Recovery / Resume语义由产品定义；redrive可以保留successful steps并重新调度failed Task。
- Evidence Status：`PARTIAL`
- Lab Dependency：`REQUIRED`
- Evidence Class：`OFFICIAL_DOC`
- Source：[AWS Step Functions: Restarting executions with redrive](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html)
- Locator：overview；redrive eligibility；redrive behavior of individual states
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：current Standard Workflows docs
- Raw Observation：current docs说明eligible unsuccessful Standard Workflow可在14-day window内redrive；successful step的result / history保留且不重跑，failed Task会重新调度；redrive使用same input、definition与execution identity。
- Interpretation：`resume from failure`不是统一“从最近state JSON继续”；definition pinning、eligibility和state type都会改变语义。
- Counter-evidence：Distributed Map等state有额外特殊规则，反驳单一redrive算法。
- Proves：AWS current product范围的redrive行为与product-scope差异。
- Does Not Prove：idempotent Task、跨产品恢复、Lab 04行为。
- Limitations：不展开AWS pricing、IAM、Map或quota设计。
- Course Usage：counter-evidence、Resume / Replay边界。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E06｜Repository dependency boundary

- Article：`11 Long-running Agent`
- Claim ID：`11-C01`—`11-C09`
- Claim：Article 11必须延续而不能吞掉Article 06 / 08 / 09 / 10已冻结的责任边界。
- Evidence Status：`PARTIAL`
- Lab Dependency：`REQUIRED` for Article 11 behavior
- Evidence Class：`OFFICIAL_REPOSITORY_ARTIFACT`
- Source：Articles 06 / 08 / 09 / 10 Published Content
- Locator：Article 06 cancellation / idempotency limitations；Article 08 terminal / cancellation non-scope；Article 09 Plan authority；Article 10 State / checkpoint bridge
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / NOT_RUN`
- Version / Product Scope：current repository Published Content
- Raw Observation：06明确single-process dedup不等于durable idempotency；08把stopped / succeeded分离并把checkpoint / recovery留给11；09把Plan限定为candidate；10明确current State不等于checkpoint，并把continuation metadata留给11。
- Interpretation：Article 11应增加durability、in-flight identity、retry / cancellation / recovery，不应重写Loop、Plan或legal-transition基础。
- Counter-evidence：Article 06已有timeout / caller cancellation Lab，但它不含cross-process resume；Article 10引用checkpointer docs但没有运行Lab 04。
- Proves：课程内部依赖与non-scope。
- Does Not Prove：Article 11 behavior或外部产品当前事实。
- Limitations：repository-local contract。
- Course Usage：全篇术语与边界。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E07｜Frozen Lab 04 design

- Article：`11 Long-running Agent`
- Claim ID：`11-C01`—`11-C08`
- Claim：Lab 04为取消后恢复、幂等Retry、lost response、duplicate risk、missing in-flight action与fresh-process reproducibility建立了执行前可证伪判据。
- Evidence Status：`CONFIRMED / DESIGN-PRESERVED`
- Lab Dependency：`SATISFIED`
- Evidence Class：`DESIGN_PROPOSAL`
- Source：`docs/agent-engineering-course/labs/lab-04-state-machine-checkpoint/README.md`
- Locator：Frozen Design、case matrix、Expected Observable、Acceptance Criteria
- Retrieved / Run At：`2026-08-21 Asia/Shanghai / 2026-08-21T13:25:44+08:00 accepted run`
- Version / Product Scope：`lab04-design-v1`；C# / net10.0 target；BCL-only fake external store
- Raw Observation：Design字节校验在final rerun前后仍为`17833 bytes / 0146c43137ad2386397cc38fdea866731942a9e56ec0d55f2fbf57619c9d3101`；execution log保留first failures、patch与accepted rerun。
- Interpretation：运行没有为适配文章thesis修改frozen Design；具体行为证据由E08—E13和raw artifacts承载。
- Counter-evidence：first build与static-contract曾失败，证明最终PASS不是运行前预写；unsafe comparator仍以FAILED terminal保留。
- Proves：Design未被Lab Engineer改写，Expected / Observed分层保持。
- Does Not Prove：Design本身不单独证明任何Runtime行为，也不证明production充分性。
- Limitations：single-host deterministic fake；不覆盖真实Provider / network / distributed transaction。
- Course Usage：Lab 04 handoff。
- BuildPilot Implication：`N/A`
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E08｜Accepted execution and environment

- Article：`11 Long-running Agent`
- Claim ID：`11-C01`—`11-C08`
- Evidence Status：`CONFIRMED / EXECUTION-RECORD`
- Evidence Class：`LAB_OBSERVATION`
- Source：`observations/execution-log.md`、`verification-summary.json`、`environment.md`、`dotnet-info.txt`
- Raw Observation：offline restore=`0`；final Release build=`0` / `0 warnings / 0 errors`；static contract=`0`；run A=`0`；run B=`0`；compare=`0`；LR cases accepted=`8/8`；network / Provider / credential counters=`0 / 0 / 0`。first build `CS5001`、first static generated-source false positive与CIM probe `Access denied`被保留。
- Interpretation：Lab确实在`.NET SDK 10.0.301 / Host 10.0.9 / Windows 10.0.19045 X64 / China Standard Time`上执行，且经历过可追踪的red-to-green过程。
- Proves：当前Host与frozen command surface的accepted Lab execution。
- Does Not Prove：其他OS / TFM、production runtime、真实network / Provider行为。
- Limitations：CIM不能读取OS edition；OS build / architecture由`dotnet --info`与`RuntimeInformation`支持。
- Course Usage：执行环境、first-failure、无外部依赖边界。
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E09｜Cancellation and timeout remain distinct

- Article：`11 Long-running Agent`
- Claim ID：`11-C01 / 11-C05 / 11-C06 / 11-C07`
- Evidence Status：`CONFIRMED / FIXTURE-SCOPED`
- Evidence Class：`LAB_OBSERVATION`
- Source：`run-a/LR-02/**`、`run-a/LR-08/**`、对应run-b byte-identical normalized artifacts与process evidence
- Raw Observation：LR-02 START=`CANCELLED / INCOMPLETE / CALLER`，effect/access/attempt=`0/0/0`；checkpoint保留completed evidence、remaining actions与`REGISTER_FINDING`；RESUME为不同PID并最终effect=`1`。LR-08=`TIMED_OUT / INCOMPLETE / TIMEOUT`，effect/access/attempt=`0/0/0`，trace无caller-cancel event。
- Interpretation：在冻结pre-effect boundary下，request origin、listener observation、terminal与continuation是可分的控制事实；resume从checkpoint boundary继续而没有重做committed evidence action。
- Proves：course fixture中CALLER cancel与TIMEOUT分类，以及fresh-process safe-boundary resume。
- Does Not Prove：mid-I/O cancellation、强制termination、rollback、OS crash recovery。
- Limitations：deterministic named seams；只有pre-effect cancel / timeout。
- Course Usage：`Timeout != Cancellation`；request不等于work stopped；resume需explicit boundary。
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E10｜Retry identity and budget

- Article：`11 Long-running Agent`
- Claim ID：`11-C03 / 11-C07`
- Evidence Status：`CONFIRMED / FIXED-STORE-SCOPED`
- Evidence Class：`LAB_OBSERVATION`
- Source：`run-a/LR-03/**`、`run-a/LR-07/**`及对应run-b normalized artifacts
- Raw Observation：LR-03 trace为pre-apply reject -> retry approved -> attempt 2 create，attempts=`2`、effect=`1`；LR-07在attempt 2后`RETRY_BUDGET_EXHAUSTED / INCOMPLETE`，effect=`0`，partial result保留known evidence、unverified registration / verification / goal与`ASK_OR_STOP`。
- Interpretation：budget只限制尝试数；自动retry还要依赖“pre-apply可知”或stable identity / idempotent effect semantics。
- Proves：fixed fake store中的budget accounting、pre-apply retry和exhaustion stop。
- Does Not Prove：真实HTTP、远程store、退避时序、所有transient分类正确。
- Limitations：single coordinator、scripted fault、no network。
- Course Usage：`Retry Budget != retry safety`；先判effect uncertainty，再判budget。
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E11｜Lost response and unsafe comparator

- Article：`11 Long-running Agent`
- Claim ID：`11-C03 / 11-C04 / 11-C06 / 11-C07`
- Evidence Status：`CONFIRMED / FIXED-STORE-SCOPED`
- Evidence Class：`LAB_OBSERVATION + NEGATIVE_EVIDENCE`
- Source：`run-a/LR-04/**`、`run-a/LR-05/**`及对应run-b normalized artifacts / process evidence
- Raw Observation：LR-04 START在store有1条record后返回`INTERRUPTED / UNKNOWN_SIDE_EFFECT`，checkpoint保留same action / intent / key与`RESULT_UNKNOWN`；fresh RESUME观测`CREATE_OR_GET_EXISTING`，final effect仍=`1`。LR-05同样进入unknown窗口，但unsafe resume以new delivery append出effect-001 / 002，终态为`DUPLICATE_SIDE_EFFECT_DETECTED / FAILED`。
- Interpretation：lost response不能被压成“未执行”；stable identity + query / `CreateOrGet`能在本fake store收窄duplicate risk，blind retry则由真实duplicate records反证。
- Proves：fixed local store下fault seam、unknown partial result、same-identity reconcile与unsafe duplicate risk。
- Does Not Prove：exactly-once、distributed transaction、远程查询一定成功、OS crash后磁盘一致性。
- Limitations：named interruption不是OS crash；fake store / checkpoint是同机不同文件，不是独立service或transaction。
- Course Usage：`Idempotency != exactly-once`；unknown -> lookup / same-intent replay / ask / stop；LR-05必须作为negative evidence出现。
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E12｜Missing in-flight identity fails closed

- Article：`11 Long-running Agent`
- Claim ID：`11-C02 / 11-C07 / 11-C08`
- Evidence Status：`CONFIRMED / PROPOSAL-CONFORMANCE`
- Evidence Class：`LAB_OBSERVATION + NEGATIVE_EVIDENCE`
- Source：`run-a/LR-06/{checkpoint-start.json,checkpoint-invalid.json,checkpoint-resume.json,trace.jsonl,fake-store-view-start.json,fake-store-view-resume.json,partial-result*.json,case-result.json}`及对应run-b artifacts
- Raw Observation：state=`REGISTERING_FINDING`而in-flight action=`null`；RESUME trace的第一个恢复事件即`RECOVERY_VALIDATION_REFUSED / IN_FLIGHT_ACTION_MISSING`；fake-store start / resume均为1条record、access count=`1`。partial result保留unknown action与unverified requirements，next safe action=`NONE`。
- Interpretation：对课程candidate invariant而言，“有state JSON”不足以安全resume；关键in-flight identity缺失时应在再次effect前拒绝而不是猜测。
- Proves：course Runtime对冻结损坏checkpoint的fail-closed behavior。
- Does Not Prove：checkpoint candidate字段对production充分，或checkpoint integrity digest mismatch / refusal已被执行；LR-01—LR-08没有注入digest mismatch，也不覆盖其他schema migration / corruption。
- Limitations：test-only omission seam；不包含partial write、bit rot、concurrent writer。
- Course Usage：checkpoint必须回答in-flight uncertainty；invalid -> refuse / ask，不伪成success。
- Owner：`Researcher`
- Verified At：`2026-08-21`

### Evidence 11-E13｜Partial-result conformance and reproducibility

- Article：`11 Long-running Agent`
- Claim ID：`11-C07 / 11-C08`
- Evidence Status：`CONFIRMED / COURSE-SCHEMA + DETERMINISTIC-FIXTURE-SCOPED`
- Evidence Class：`LAB_OBSERVATION`
- Source：`LR-02、LR-04—08 partial-result*.json`、16个`artifact-manifest.json`、`process-evidence-run-a.json`、`process-evidence-run-b.json`、`verification-summary.json`
- Raw Observation：non-success artifacts的known / unknown / unverified / next-safe-action字段与provenance可回指checkpoint / trace / fake store；每suite的12个Runtime phase使用独立PID。run A / B各含106个文件，有意不同的suite sentinel不在normalized set；其余105个normalized files byte-identical，aggregate SHA-256=`27890bd8eedafe3cca8397d585b1a1431292d72d093464914ab9976048d89b9a`。
- Interpretation：frozen binary / fixture / normalization下的控制产物可复现，且中断产物没有把unknown / unverified伪装成known。
- Proves：course partial-result schema conformance与deterministic normalized fixture reproducibility。
- Does Not Prove：真实model / network确定性、performance、availability、crash consistency、跨平台一致。
- Limitations：single coordinator；no network / Provider；local files；byte identity不是production reliability指标。
- Course Usage：partial result是证据合同，不是无条件“最佳努力”文本。
- Owner：`Researcher`
- Verified At：`2026-08-21`

## Claim-to-Lab map

| Claim ID | Preliminary Evidence | Lab 04 observable | Pre-Lab status | Post-Merge status |
|---|---|---|---|---|
| `11-C01` | .NET cancellation + LangGraph / AWS product boundaries | cancellation / retry / timeout各自独立trace event、origin与terminal | `PARTIAL / REQUIRED` | `CONFIRMED / FIXTURE-SCOPED` |
| `11-C02` | LangGraph checkpoint / pending writes fields；Article 10 bridge | checkpoint schema validation；completed / remaining / in-flight / budget / continuation readback | `PARTIAL / REQUIRED` | `CONFIRMED / PROPOSAL-CONFORMANCE` |
| `11-C03` | RFC 9110 §9.2.2 | pre-apply transient retry、budget count、single intended fake effect | `PARTIAL / REQUIRED` | `CONFIRMED / FIXED-STORE-SCOPED` |
| `11-C04` | RFC lost response + LangGraph side-effect replay warning | apply-then-lose-response；idempotent `CreateOrGet` count=1；unsafe comparator count=2 | `BLOCKED / REQUIRED` | `CONFIRMED / FIXED-STORE-SCOPED` |
| `11-C05` | .NET cooperative cancellation | safe-boundary cancellation；fresh-process resume；no pre-resume side effect | `PARTIAL / REQUIRED` | `CONFIRMED / FIXTURE-SCOPED` |
| `11-C06` | LangGraph / AWS product-specific resume | distinct start / resume processes、checkpoint boundary、completed action not rerun | `PARTIAL / REQUIRED` | `CONFIRMED / COURSE-RUNTIME-SCOPED` |
| `11-C07` | Course Proposal only | known / unknown / unverified / next-safe-action terminal artifacts | `BLOCKED / REQUIRED` | `CONFIRMED / COURSE-SCHEMA-CONFORMANCE` |
| `11-C08` | missing-in-flight与integrity fail-closed均为Design requirement | LR-06只观察missing in-flight拒绝且zero new effect；run A / B normalized hashes；没有digest-mismatch case | `BLOCKED / REQUIRED` | missing in-flight=`CONFIRMED / PROPOSAL-CONFORMANCE`；run A/B=`CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED`；integrity mismatch=`PROPOSAL / NOT_OBSERVED` |
| `11-C09` | LangGraph checkpointer / store counter-evidence | N/A；术语只按source收窄 | `PARTIAL / NOT_REQUIRED` | `CONFIRMED / PRODUCT-DOC-SCOPED` |

## Per-Claim Evidence Merge

| Claim | Experiment -> Observation -> Evidence Interpretation -> Claim Status | Proves | Does Not Prove | Limitations | Allowed course wording / ceiling |
|---|---|---|---|---|---|
| `11-C01` | **Experiment**：对照LR-02 caller cancel、LR-03 retry与LR-08 timeout，并用.NET / LangGraph / AWS校对product semantics。**Observation**：`CALLER_CANCELLATION_OBSERVED`、`RETRY_APPROVED`、`TIMEOUT_SIGNAL_OBSERVED`及其origin / terminal分开保存。**Interpretation**：单一FAILED / CANCELLED无法反推请求、响应、重试、恢复或重放。**Status**：`CONFIRMED / FIXTURE-SCOPED`。 | fixed trace中timeout / cancellation / retry是不同控制事实。 | 全行业术语统一；任意runtime的Replay / Recovery规则。 | 课程统一名称是working definition；产品规则仍product-scoped。 | 可写“必须分开记录”；不写“所有系统共享同一状态机”。Ceiling=`FIXTURE-SCOPED`。 |
| `11-C02` | **Experiment**：对照LR-02 / 04 valid resume与LR-06 omitted in-flight checkpoint。**Observation**：valid checkpoint保留run / state revision / completed / remaining / in-flight / budget / continuation；LR-06在store access不变时refuse。**Interpretation**：只有current State不能解决in-flight uncertainty，这些字段对本course runtime是必要的恢复判定输入。**Status**：`CONFIRMED / PROPOSAL-CONFORMANCE`。 | candidate schema对frozen cases的区分能力。 | candidate schema对production充分；所有损坏/迁移/并发情况。 | local JSON、single writer、无partial disk write。 | 可写“课程最小candidate必须回答这些问题”；不写“这就是完整checkpoint schema”。Ceiling=`PROPOSAL-CONFORMANCE`。 |
| `11-C03` | **Experiment**：LR-03 transient-before-apply-once、LR-04 same-identity recovery、LR-07 transient-always exhaustion，并与RFC 9110对照。**Observation**：LR-03 attempts=2/effect=1；LR-04 effect维持1；LR-07 attempts=2/effect=0后停止。**Interpretation**：budget不提供retry safety；safety先来自“未apply可知”或same-intent idempotent / lookup合同。**Status**：`CONFIRMED / FIXED-STORE-SCOPED`。 | fake store下identity、effect semantics和budget三者的判定关系。 | 真实HTTP retry、通用idempotency key协议、exactly-once。 | scripted failure、no backoff / jitter / network。 | 可写“先判定是否可重试，再消耗budget”；不写“有key就一定安全”。Ceiling=`FIXED-STORE-SCOPED`。 |
| `11-C04` | **Experiment**：LR-04 controlled `CreateOrGet`与LR-05 unsafe append共用apply-then-lost-response seam。**Observation**：两者START都有effect=1 + `UNKNOWN_SIDE_EFFECT`；LR-04 same identity后final=1；LR-05 new delivery后final=2并FAILED。**Interpretation**：lost response产生unknown window；LR-05是不可涂绿的negative evidence，直接反证blind retry。**Status**：`CONFIRMED / FIXED-STORE-SCOPED`。 | local fake store中unknown、same-identity reconcile与duplicate risk。 | distributed exactly-once、远程原子性、任意crash恢复。 | named interruption != OS crash；local files != distributed transaction；single coordinator。 | 可写“unknown时lookup / same-intent replay或stop”；不写“重放保证exactly once”。Ceiling=`FIXED-STORE-SCOPED`。 |
| `11-C05` | **Experiment**：LR-02在committed evidence后、side effect前注入caller cancellation，再fresh resume。**Observation**：START checkpoint为`CALLER requested/observed`、effect=0、next=`REGISTER_FINDING`；RESUME不同PID，final effect=1，committed evidence trace只出现一次。**Interpretation**：在这个safe boundary可以停止并继续，但request本身不证明一般I/O已停止或副作用回滚。**Status**：`CONFIRMED / FIXTURE-SCOPED`。 | pre-effect cooperative cancel + explicit resume。 | mid-I/O cancel、timeout race、forced kill、rollback。 | deterministic safe seam；no external I/O。 | 可写“只从已识别的safe boundary恢复”；不写“取消后总能继续”。Ceiling=`FIXTURE-SCOPED`。 |
| `11-C06` | **Experiment**：用process evidence证明LR-02 / 04 START与RESUME分属独立OS process，对照checkpoint load与trace；用LangGraph / AWS作product counter-evidence。**Observation**：同same run identity加载checkpoint；LR-02不重跑committed evidence；LR-04在boundary后重做store call但找到existing effect。**Interpretation**：Resume是从defined boundary继续，可通过Replay重做boundary后行为；是否重做由runtime规则决定。**Status**：`CONFIRMED / COURSE-RUNTIME-SCOPED`。 | course Runtime的fresh-process resume与可观测replay boundary。 | 从原代码行继续；所有产品的重放规则；side-effect safety。 | local checkpoint；named interruption；no provider。 | 可写`Resume != Replay`且Replay可为Resume机制；不承诺“resume不重执行任何调用”。Ceiling=`COURSE-RUNTIME-SCOPED`。 |
| `11-C07` | **Experiment**：检查LR-02、04—08每个non-success phase的partial-result与checkpoint provenance。**Observation**：cancel / unknown / duplicate / invalid / exhausted / timeout分别保留known、unknown、unverified与next safe action；unknown / unverified未进入known。**Interpretation**：这个schema能在fixed cases中诚实表达不完整性，不把中断伪装成success。**Status**：`CONFIRMED / COURSE-SCHEMA-CONFORMANCE`。 | course schema的字段分离与provenance一致性。 | schema是行业标准；文本语义永远完整；真实model evidence quality。 | scripted fields、fixed goal；no Provider。 | 可写“partial result必须保留这四类证据状态”；标明这是course proposal。Ceiling=`COURSE-SCHEMA-CONFORMANCE`。 |
| `11-C08` | **Experiment**：LR-06 omitted-in-flight resume + run A/B normalized compare；没有digest-mismatch case。**Observation**：LR-06的integrity digest保持有效，Runtime因`IN_FLIGHT_ACTION_MISSING`在fake-store access不增加时refuse；每suite 12个fresh phase PIDs；105 normalized files byte-identical，suite sentinel有意不同。**Interpretation**：fixed Runtime只对missing in-flight state invariant观测到fail closed；冻结归一化产物可复现。Integrity mismatch拒绝仍是课程设计要求 / Proposal。**Status**：missing in-flight=`CONFIRMED / PROPOSAL-CONFORMANCE`；run A/B=`CONFIRMED / DETERMINISTIC-FIXTURE-SCOPED`；integrity mismatch=`PROPOSAL / NOT_OBSERVED`。 | missing in-flight时的pre-new-effect refusal；frozen artifact reproducibility。 | integrity digest mismatch / refusal、其他integrity failure、OS crash / power loss、cross-platform determinism、availability。 | test-only omission；LR-01—LR-08未破坏digest；local filesystem；CIM probe limitation；single coordinator。 | 可写“LR-06因missing in-flight在新side effect前拒绝”和“run A/B normalized artifacts可复现”；integrity mismatch只能写课程设计要求 / Proposal，不写成Observed；不写“Agent execution天然确定”。 |
| `11-C09` | **Experiment**：刷新并对照LangGraph Persistence中checkpointer的memory、fault tolerance、thread、Store和replay用途；Lab不需行为扩展。**Observation**：同一checkpointer capability被用于thread-scoped memory与recovery，cross-thread data另有Store概念。**Interpretation**：Checkpoint / Memory不宜按物理组件强制互斥；课程只按“是否回答control position / continuation / in-flight uncertainty”区分证明职责。**Status**：`CONFIRMED / PRODUCT-DOC-SCOPED`。 | current LangGraph docs的术语重叠反例和课程边界。 | 行业统一taxonomy；有checkpointer就恢复安全；Memory质量。 | current hosted docs、无package / commit pin；no Lab behavior dependency。 | 可写“实现可重叠，证明职责不同”；不写“Checkpoint与Memory必然是两套存储”。Ceiling=`PRODUCT-DOC-SCOPED`。 |

## Preliminary Evidence decision

- Decision：`PASS / LAB_DESIGN_FROZEN`
- Core behavior readiness：`READY AFTER LAB OBSERVATION + EVIDENCE MERGE`
- Required Lab：`SATISFIED`
- Blocker for Evidence Gate：`NONE`
- Allowed wording after merge：official / product-scoped facts、course Proposal，以及明确带`fixture / fixed-store / course-runtime / course-schema`限定的Lab Observation。
- Forbidden wording after merge：`production-proven`、`distributed-safe`、`crash-safe`、`exactly-once`、`cross-platform deterministic`、`checkpoint schema sufficient for all systems`。

## Evidence Gate decision

- Decision：`PASS`
- Core `BLOCKED`：`0`
- Claim traceability：`9 / 9`
- Lab state：`OBSERVED / MERGED`
- Counter-evidence：`PRESENT`（LR-05 unsafe duplicate、LR-06 fail-closed、first failures、CIM limitation）
- Evidence ceilings：`PRESENT FOR 9 / 9 CLAIMS`
- Article 12 stop line：`PRESENT`
- Next allowed Gate：`OUTLINE`

## Article 12 stop line

Article 11只讲恢复控制面：checkpoint boundary、retry / cancellation / resume / recovery decision、in-flight side-effect uncertainty与partial result。它不把跨run长期Memory、context选择/重建/质量、模型决策确定性或knowledge retention纳入本篇Claim。Checkpoint / Memory可共用persistence capability，但Lab 04不证明Article 12的Memory / Context质量。
