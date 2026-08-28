# Article 21 Evidence｜Trace、Replay 与 Failure Taxonomy

## Evidence Metadata

- Article: `21`
- Gate: `RESEARCH / EVIDENCE_GATE`
- Researcher execution: `/root/article21_researcher`
- External source access date: `2026-08-26`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`

## Cross-Claim Evidence Matrix

| Claim | Card | Class | Status | Primary-source role | Key limitation |
|---|---|---|---|---|---|
| `21-C01` | `21-E01` | STANDARD / GUIDANCE | `PARTIAL` | OTel signal models + NIST AU-3 | 没有统一 replay contract |
| `21-C02` | `21-E02` | STANDARD + COURSE CONTRACT | `PROPOSAL` | W3C/OTel correlation primitives | 不定义 run/turn/step/tool-call |
| `21-C03` | `21-E03` | PAPER / STANDARD | `PARTIAL` | Lamport causality + OTel timestamps/links | 不规定课程排序字段 |
| `21-C04` | `21-E04` | STANDARD PRECEDENT + COURSE CONTRACT | `PROPOSAL` | CloudEvents + in-toto descriptors | 课程 envelope 不是标准原文 |
| `21-C05` | `21-E05` | PRODUCT DOC / PATTERN + COURSE CONTRACT | `PROPOSAL` | LangGraph/AWS/Azure semantic counterexamples | 产品行为不可泛化 |
| `21-C06` | `21-E06` | PRODUCT DOC + COURSE CONTRACT | `PARTIAL` | LangGraph nondeterminism caveats + RFC 9110 | 不证明 deterministic replay |
| `21-C07` | `21-E07` | NIST GUIDANCE + COURSE CONTRACT | `PROPOSAL` | detect/analyze/recover separation | 不定义 Agent event layers |
| `21-C08` | `21-E08` | COURSE TAXONOMY | `PROPOSAL` | multiple primary-source constraints | 七层由课程提出 |
| `21-C09` | `21-E09` | STANDARD / GUIDANCE + COURSE CONTRACT | `PROPOSAL` | exception/status + root cause/recovery | symptom 不自动给 root cause |
| `21-C10` | `21-E10` | OFFICIAL GUIDANCE / PRODUCT DOC | `PARTIAL` | OTel sensitive-data + OpenAI config + NIST audit privacy | 不证明合规或可重放 |
| `21-C11` | `21-E11` | NIST FRAMEWORK + COURSE SEAM | `PROPOSAL` | AI RMF TEVV/metrics/benchmarks | 不定义课程 Eval verdict |
| `21-C12` | `21-E12` | REPOSITORY FACT | `CONFIRMED` | Article 21 README/card/dispatch | 只证明当前课程资产状态 |

## Evidence Cards

### Evidence Card `21-E01`｜Log / Metric / Trace / Audit boundary

- Article: `21`
- Claim ID: `21-C01`
- Claim: Log、Metric、Trace、Audit Record 是互补视图；任一单独存在都不证明可重放。
- Evidence Status / Class: `PARTIAL` / `STANDARD + OFFICIAL GUIDANCE`
- Source Type: hosted specification and NIST publication
- Sources: [OTel Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/), [Logs Data Model](https://opentelemetry.io/docs/specs/otel/logs/data-model/), [Metrics Data Model](https://opentelemetry.io/docs/specs/otel/metrics/data-model/), [NIST SP 800-53 Rev. 5 AU-3](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final)
- Source Identity / Version: hosted OTel pages display `1.60.0`; NIST SP 800-53 Rev. 5 current page notes release `5.2.0`; hosted pages may drift and OTel page-to-tag mapping was not independently pinned.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; documentation research only; experiment count `0`; runtime trace `ABSENT`.
- Observation: OTel defines spans with context/parent/events/links, logs as discrete records including timestamp/observed timestamp/body/correlation fields, and metrics as timeseries/aggregations. AU-3 asks audit records to cover event type/time/place/source/outcome/identity.
- Counter-evidence Searched: searched for a primary source asserting any one signal automatically gives replay; none of the cited specifications makes that claim.
- Interpretation: these views answer different questions and may correlate by IDs, but replay requires additional state, version, input and effect completeness.
- Proves: the four record families have materially different intended structures and questions.
- Does Not Prove: that the categories are exhaustive, mutually exclusive in every product, or that any stored Trace is replayable/audit-complete.
- Limitations: OTel is an observability standard, not the sole industry vocabulary; AU-3 is a security/privacy control, not an Agent Trace schema.
- Course Usage / BuildPilot Implication: teach the distinction; BuildPilot fields remain design-only.
- Owner: Article 21 Researcher

### Evidence Card `21-E02`｜Scoped identities

- Article: `21`
- Claim ID: `21-C02`
- Claim: 课程 Trace 必须显式区分 run / turn / step / tool-call / event identity，且 provider trace/span ID 只是 correlation identity。
- Evidence Status / Class: `PROPOSAL` / `STANDARD PRECEDENT + COURSE CONTRACT`
- Source Type: W3C Recommendation and OTel specification
- Sources: [W3C Trace Context Recommendation](https://www.w3.org/TR/2021/REC-trace-context-1-20211123/), [OTel Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/)
- Source Identity / Version: W3C Recommendation `23 November 2021`; hosted OTel page displays `1.60.0` and may drift.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; documentation research only.
- Observation: W3C supplies `trace-id`, `parent-id`, flags and propagation; OTel supplies immutable SpanContext and parent/link relationships.
- Counter-evidence Searched: checked whether either source defines Agent run/turn/step/tool-call/event scopes; neither does.
- Interpretation: stable correlation primitives are necessary but insufficient for course/business identities with different lifecycles.
- Proves: provider/distributed tracing IDs have specified scopes and parent relationships.
- Does Not Prove: the proposed five-level identity hierarchy is a W3C/OTel requirement or that provider IDs remain stable across rerun/resume.
- Limitations: the hierarchy is a course contract and must be mapped explicitly by each implementation.
- Course Usage / BuildPilot Implication: preserve provider IDs under `correlation_ids`; BuildPilot is not implemented.
- Owner: Article 21 Researcher

### Evidence Card `21-E03`｜Causal order and timestamps

- Article: `21`
- Claim ID: `21-C03`
- Claim: causal/ordering reconstruction needs scoped sequence and causal links; wall-clock timestamp alone is insufficient.
- Evidence Status / Class: `PARTIAL` / `PRIMARY PAPER + STANDARD`
- Source Type: peer-reviewed primary paper and OTel specification
- Sources: [Lamport 1978 publication](https://www.microsoft.com/en-us/research/publication/time-clocks-ordering-events-distributed-system/), [OTel Logs Data Model](https://opentelemetry.io/docs/specs/otel/logs/data-model/), [OTel Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/)
- Source Identity / Version: Lamport, CACM 21(7), 1978; hosted OTel pages display `1.60.0` and may drift.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; no runtime experiment.
- Observation: Lamport formalizes happens-before as a partial order; OTel distinguishes event timestamp from observed timestamp and supports parent/links.
- Counter-evidence Searched: looked for support that total timestamp sorting alone represents causality; the cited sources instead preserve distinct timing and relationship concepts.
- Interpretation: an Agent trace should declare a sequence scope and explicit causal edges, allowing concurrent events to remain partially ordered.
- Proves: timestamp and causality are different concerns; causal ordering can be partial.
- Does Not Prove: that the proposed `sequence`, `parent_event_id`, `caused_by[]` names are universal or sufficient for every distributed topology.
- Limitations: clock synchronization, delayed ingestion and producer bugs remain implementation concerns.
- Course Usage / BuildPilot Implication: use explicit causal references in the design sample only.
- Owner: Article 21 Researcher

### Evidence Card `21-E04`｜Event envelope and payload reference

- Article: `21`
- Claim ID: `21-C04`
- Claim: event contract 应分离所有事件共用的 base envelope 与按 `event_type` 条件必需的 causal/state/policy/approval/payload references。
- Evidence Status / Class: `PROPOSAL` / `STANDARD PRECEDENT + COURSE CONTRACT`
- Source Type: version-pinned specifications plus hosted OTel specification
- Sources: [CloudEvents v1.0.2 core](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md), [in-toto Resource Descriptor v1.0](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md), [OTel Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/#span)
- Source Identity / Version: CloudEvents tag `v1.0.2`; in-toto Attestation tag `v1.0`, Resource Descriptor；hosted OTel page displays `1.60.0` and may drift.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: source files are pinned in linked repositories; no call path/experiment/runtime trace.
- Observation: CloudEvents requires event `id`, `source`, `specversion`, `type` and defines optional `time`/`data`; source+id supports uniqueness/duplicate recognition. in-toto descriptors provide URI/digest/content metadata precedent. OTel spans may be roots with no parent and can have zero or more links.
- Counter-evidence Searched: checked both specs for Agent run hierarchy, state transitions and replay semantics; absent.
- Interpretation: the course contract composes established envelope/descriptor ideas with a small base envelope and Article-specific `event_type` specializations；root events may omit parent/cause，non-Tool events may omit Tool/attempt identity，and State/Policy/Approval/Payload refs become required only when the event-type contract declares them.
- Proves: identity/type/source and out-of-line payload descriptors are established design precedents.
- Does Not Prove: that the complete proposed event envelope is CloudEvents/in-toto compliant, universal, immutable in storage, or sufficient for replay.
- Limitations: digest proves content identity only when algorithm/use are sound; it does not establish truth, authorization or availability. The cited specs do not validate the course requiredness matrix；missing relations must not be replaced with fabricated placeholders.
- Course Usage / BuildPilot Implication: label the whole schema `COURSE PROPOSAL`; no BuildPilot serialization exists.
- Owner: Article 21 Researcher

### Evidence Card `21-E05`｜Replay semantic family

- Article: `21`
- Claim ID: `21-C05`
- Claim: Replay、Resume、Retry、Rerun、Simulation、Projection 必须分开声明。
- Evidence Status / Class: `PROPOSAL` / `OFFICIAL PRODUCT DOC + ARCHITECTURE PATTERN + COURSE CONTRACT`
- Source Type: official hosted product documentation and Microsoft pattern guidance
- Sources: [LangGraph Time Travel](https://docs.langchain.com/oss/python/langgraph/use-time-travel), [LangGraph Persistence](https://docs.langchain.com/oss/python/langgraph/persistence), [AWS Step Functions Redrive](https://docs.aws.amazon.com/step-functions/latest/dg/redrive-executions.html), [Azure Event Sourcing](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- Source Identity / Version: current hosted LangGraph/AWS docs with no tag mapping asserted; Azure page last updated `2026-03-28`; all accessed `2026-08-26` and subject to hosted-doc drift.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; product documentation only.
- Observation: LangGraph replays nodes after a checkpoint；AWS redrive usually preserves successful steps and continues from unsuccessful ones，but `States.DataLimitExceeded` causes Parallel、Inline Map and Distributed Map to rerun the whole relevant state，including previously successful branches、iterations or child workflows；Azure distinguishes event rehydration from read-model projections.
- Counter-evidence Searched: compared three official meanings; their execution, identity and side-effect boundaries differ.
- Interpretation: a bare `replay` label is under-specified; course operations need mode, source, boundary, identity and side-effect policy.
- Proves: real systems already use materially different replay/redrive/projection semantics.
- Does Not Prove: the course six-way taxonomy is an industry standard，that every product maps one-to-one，that successful AWS work is never rerun，or that Redrive provides exactly-once effects.
- Limitations: hosted product behavior can change; exact deployed versions and state-specific exception behavior must be recorded by an implementation.
- Course Usage / BuildPilot Implication: teach the distinction without claiming a working replay engine.
- Owner: Article 21 Researcher

### Evidence Card `21-E06`｜Nondeterminism and replayability manifest

- Article: `21`
- Claim ID: `21-C06`
- Claim: replayability depends on frozen/recorded versions, nondeterministic inputs, external responses and effect receipts; missing evidence forbids deterministic claims.
- Evidence Status / Class: `PARTIAL` / `OFFICIAL PRODUCT DOC + INTERNET STANDARD`
- Source Type: official hosted product docs and RFC
- Sources: [LangGraph Backward Compatibility](https://docs.langchain.com/oss/python/langgraph/backward-compatibility), [LangGraph Time Travel](https://docs.langchain.com/oss/python/langgraph/use-time-travel), [RFC 9110 §9.2.2 Idempotent Methods](https://www.rfc-editor.org/rfc/rfc9110.html#section-9.2.2)
- Source Identity / Version: current hosted LangGraph docs, no tag mapping asserted; RFC 9110, June 2022.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; no replay experiment was authorized.
- Observation: LangGraph warns replay can re-execute LLM/API/interrupt work and is sensitive to reordered tasks/interrupts and nondeterminism such as time/random/network. RFC 9110 limits automatic retry reasoning to idempotent methods and still does not give exactly-once effects.
- Counter-evidence Searched: searched cited docs for a guarantee that same prompt/checkpoint yields bit-identical output across versions/providers/environment; none found.
- Interpretation: a replay manifest must state source events, code/schema/model/policy/tool/state versions, nondeterministic inputs, external I/O and effect reconciliation.
- Proves: product replay may execute nondeterministic/external work, and idempotency has a narrower meaning than exactly-once.
- Does Not Prove: bit-for-bit deterministic replay, effect-free retry, or completeness of the proposed manifest.
- Limitations: this task ran no experiment; future implementations need provider/version-specific evidence.
- Course Usage / BuildPilot Implication: prohibit deterministic wording; BuildPilot remains `NOT RUN`.
- Owner: Article 21 Researcher

### Evidence Card `21-E07`｜Occurrence / observation / recovery layers

- Article: `21`
- Claim ID: `21-C07`
- Claim: failure occurrence、observation、recovery 应作为独立层与事件保存。
- Evidence Status / Class: `PROPOSAL` / `NIST GUIDANCE + COURSE CONTRACT`
- Source Type: NIST primary publications
- Sources: [NIST SP 800-61 Rev. 3](https://nvlpubs.nist.gov/nistpubs/specialpublications/nist.sp.800-61r3.pdf), [NIST SP 800-184](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-184.pdf)
- Source Identity / Version: SP 800-61 Rev. 3 and SP 800-184 official PDFs.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; no incident/runtime observation.
- Observation: NIST separates detection/analysis from recovery and treats recovery planning and root-cause learning as distinct concerns.
- Counter-evidence Searched: checked whether these publications define Agent event envelopes or require three exact record layers; they do not.
- Interpretation: separate event identities prevent later observation/recovery outcomes from overwriting what initially failed.
- Proves: detection/analysis and recovery are distinct incident-management concerns.
- Does Not Prove: that the proposed three-layer model is a NIST standard or that recovery success disproves an earlier failure.
- Limitations: cybersecurity/resilience guidance requires adaptation to Agent execution.
- Course Usage / BuildPilot Implication: course proposal only; no real BuildPilot incident.
- Owner: Article 21 Researcher

### Evidence Card `21-E08`｜Seven-layer Failure Taxonomy

- Article: `21`
- Claim ID: `21-C08`
- Claim: failure classification 应先求最早有证据的 contract-breach occurrence set，再以 `SINGLE / CO_PRIMARY / BOUNDARY / UNKNOWN` 和 `primary_layers[]` 表达是否存在唯一 owner。
- Evidence Status / Class: `PROPOSAL` / `COURSE TAXONOMY`
- Source Type: course synthesis constrained by official specifications/guidance
- Sources: [OTel Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/), [NIST SP 800-61 Rev. 3](https://nvlpubs.nist.gov/nistpubs/specialpublications/nist.sp.800-61r3.pdf), [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110.html)
- Source Identity / Version: hosted OTel page displays `1.60.0`; NIST SP 800-61 Rev. 3; RFC 9110, June 2022.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; no BuildPilot failure corpus or experiment.
- Observation: sources expose different operation/status, incident-analysis, protocol and dependency contracts but do not publish this seven-layer Agent taxonomy.
- Counter-evidence Searched: explicitly checked for an official source defining these exact seven layers; absent.
- Interpretation: classify the earliest evidenced breach occurrence set as `SINGLE`、`CO_PRIMARY` or `BOUNDARY`；reserve `UNKNOWN` for insufficient evidence. Only evidence-supported causal/contract ordering permits another breach to be demoted to contributing factor/symptom.
- Counterexample: concurrent Tool-schema and Runtime-callback-loss breaches with no causal edge can both be minimal elements；the record is `CO_PRIMARY` with two occurrence events and `[TOOL, RUNTIME]`, not `UNKNOWN` and not an invented factor hierarchy.
- Proves: only that multiple distinct contract domains exist and root-cause analysis needs evidence.
- Does Not Prove: exhaustiveness, mutual exclusivity, statistical validity, or industry adoption of the seven layers.
- Limitations: concurrent independent breaches can yield multiple minimal elements，and an owned contract can cross owner boundaries；both require explicit representation rather than an invented single layer. The taxonomy still needs Article 22 validation before operational thresholds.
- Course Usage / BuildPilot Implication: label every taxonomy table `COURSE PROPOSAL`; no production frequencies.
- Owner: Article 21 Researcher

### Evidence Card `21-E09`｜Root failure, factor, symptom, recovery outcome

- Article: `21`
- Claim ID: `21-C09`
- Claim: Trace 应分别记录 root failure candidate、contributing factor、symptom、recovery decision/outcome 与 unknown。
- Evidence Status / Class: `PROPOSAL` / `STANDARD + NIST GUIDANCE + COURSE CONTRACT`
- Source Type: OTel specification/semantic conventions and NIST publications
- Sources: [OTel Exception recording](https://opentelemetry.io/docs/specs/otel/trace/exceptions/), [OTel HTTP exceptions](https://opentelemetry.io/docs/specs/semconv/http/http-exceptions/), [NIST SP 800-184](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-184.pdf)
- Source Identity / Version: hosted OTel semantic-conventions specification `1.44.0`; NIST SP 800-184 official PDF.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; no failure trace captured.
- Observation: OTel records exception type/message/stack/status as observations; NIST resilience guidance values root-cause analysis and recovery outcomes.
- Counter-evidence Searched: checked whether an exception event or error status is defined as root cause; it is not.
- Interpretation: symptoms and recovery records are evidence about a failure, not substitutes for an evidenced root-failure hypothesis.
- Proves: exception/status and root-cause/recovery analysis are distinct information classes.
- Does Not Prove: that the outer exception identifies the causal root, or that the proposed failure record fields are universal.
- Limitations: exception messages may be incomplete or sensitive; root status must remain `CANDIDATE`/`UNKNOWN` until corroborated.
- Course Usage / BuildPilot Implication: retain evidence refs and unknowns in the design sample; no claimed real cause.
- Owner: Article 21 Researcher

### Evidence Card `21-E10`｜Sensitive data, approval evidence, redaction

- Article: `21`
- Claim ID: `21-C10`
- Claim: sensitive payload/tool output/approval evidence 应最小化、引用化、受权访问并显式记录 redaction；缺失内容限制 replayability。
- Evidence Status / Class: `PARTIAL` / `OFFICIAL GUIDANCE + PRODUCT DOC + NIST CONTROL`
- Source Type: official OTel guidance, OpenAI official SDK docs, NIST publication
- Sources: [OTel Handling Sensitive Data](https://opentelemetry.io/docs/security/handling-sensitive-data/), [OpenAI Agents SDK tracing](https://openai.github.io/openai-agents-python/tracing/), [OpenAI Agents SDK configuration](https://openai.github.io/openai-agents-python/config/), [NIST SP 800-53 Rev. 5](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final)
- Source Identity / Version: OTel page modified `2026-01-14`; OpenAI current hosted docs with no tag mapping asserted; NIST current page notes release `5.2.0`; hosted docs may drift.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: `N/A`; no sensitive payload inspected or stored.
- Observation: OTel recommends minimization/filtering/hashing/redaction and warns about hashing limits; OpenAI exposes tracing/sensitive-data controls; NIST audit controls include privacy-risk considerations and recording identities/outcomes.
- Counter-evidence Searched: checked for a guarantee that turning off sensitive capture proves compliance or preserves replay; none found.
- Interpretation: references, access control and redaction metadata preserve diagnostic lineage without assuming raw payload may be broadly copied; approval needs decision identity/scope references.
- Proves: sensitive telemetry needs explicit handling and product settings can change what trace content is captured.
- Does Not Prove: regulatory compliance, successful anonymization, authenticity of approval, or replayability after redaction/deletion.
- Limitations: sensitivity and retention are context-specific; digests can still leak low-entropy values and do not grant access.
- Course Usage / BuildPilot Implication: use placeholders/references only; no real secret, output or approval evidence exists.
- Owner: Article 21 Researcher

### Evidence Card `21-E11`｜Trace to Eval seam

- Article: `21`
- Claim ID: `21-C11`
- Claim: Trace 只向 Article 22 提供候选样本与 lineage；Golden Dataset、oracle、metric、threshold 和 regression verdict 属 Eval。
- Evidence Status / Class: `PROPOSAL` / `NIST FRAMEWORK + COURSE OWNERSHIP SEAM`
- Source Type: NIST primary framework and repository-local course contract
- Sources: [NIST AI RMF 1.0 publication](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10), `docs/agent-engineering-series-plan.md`, Article 21 `article-card.md`
- Source Identity / Version: NIST AI 100-1, January 2023; NIST publication page notes revision work in 2026; repository snapshot at Article 21 research transaction.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: repository files above; external call path/experiment/runtime trace `N/A`.
- Observation: AI RMF MEASURE discusses repeatable TEVV, metrics, methodologies, benchmarks and uncertainty; the course plan assigns Eval/Golden Dataset/Regression to Article 22.
- Counter-evidence Searched: checked for a primary source making raw traces self-validating Golden Datasets; none found.
- Interpretation: a trace slice can carry provenance and candidate outcomes, but curation, oracle/label, split, baseline, metric and verdict require a separate evaluation contract.
- Proves: evaluation needs methods/metrics/benchmarks beyond retaining execution records; course ownership is explicitly Article 22.
- Does Not Prove: that any trace sample is correct, representative, safe to retain, or accepted into a Golden Dataset.
- Limitations: AI RMF is being revised and does not prescribe this course's data schema.
- Course Usage / BuildPilot Implication: output only candidate input fields; perform no Article 22 eval or Lab 06 work.
- Owner: Article 21 Researcher

### Evidence Card `21-E12`｜Repository reality boundary

- Article: `21`
- Claim ID: `21-C12`
- Claim: Article 21 Required Lab 为 NONE、实验为 0、runtime observation 缺席，BuildPilot 未实现未运行。
- Evidence Status / Class: `CONFIRMED` / `REPOSITORY FACT`
- Source Type: repository-local canonical workspace records
- Sources: Article 21 `README.md`, `article-card.md`, `subagent-trace.md`; `docs/agent-engineering-course/course-factory.md`; `docs/agent-engineering-course/production-workflow.md`
- Source Identity / Version: current main-workspace Article 21 transaction; dispatch execution `/root/article21_researcher`; start ref `59f8c44df5d10894335bf5cd97d5b27552a830fe` recorded by Master.
- Retrieved / Verified At: `2026-08-26`
- Repository / Commit / File / Symbol / Call Path / Experiment / Fixture / Trace: repository files listed; experiment `0`; fixture `N/A`; runtime trace `ABSENT`.
- Observation: the card freezes `Required Lab=NONE`, `Experiment Count=0`, `Runtime Observation=ABSENT`, and BuildPilot `DESIGN / NOT IMPLEMENTED / NOT RUN`.
- Counter-evidence Searched: inspected the Article 21 workspace, published Articles 18–20 and prerequisites 06/08/11; no authorized Article 21 runtime result was found or created.
- Interpretation: all concrete examples in this research must remain design examples.
- Proves: current repository authorization/reality boundary for Article 21.
- Does Not Prove: that BuildPilot cannot later be implemented, that a runtime would pass, or that any production benefit/failure occurred.
- Limitations: repository-scoped fact only; future authorized transactions may change status.
- Course Usage / BuildPilot Implication: preserve exact design-only labels through Outline/Draft/Review.
- Owner: Article 21 Researcher

## Provider and Standard Preservation Matrix

| External vocabulary | Preserved meaning | Course extension | Forbidden inference |
|---|---|---|---|
| W3C `trace-id` / `parent-id` | distributed request correlation | map into `correlation_ids` / causal refs | equals run/turn/step identity |
| OTel trace/log/metric | observability signal models | reuse as evidence/source adapters | OTel is the only Trace standard or proves replay |
| CloudEvents envelope | event identity/type/source precedent | add Agent/state/approval/redaction fields | proposed envelope is required by CloudEvents |
| LangGraph replay | product checkpoint/time-travel semantics | use as counterexample | universal replay definition |
| AWS Redrive | product-specific boundary：通常保留成功步骤；`States.DataLimitExceeded` 可重跑成功 branch/iteration/child workflow | compare with Retry/Resume | successful work never reruns / exactly-once recovery |
| Azure projection | derived read model from events | define projection mode | projection executes tools or resumes a Run |
| OpenAI Agents SDK tracing | product trace/sensitive-data controls | provider adapter/correlation precedent | one switch proves compliance or safe retention |

## Evidence Gate Conclusion

- Core Claims: `12`
- Evidence Cards: `12`
- Coverage: `12 / 12`
- Status counts: `1 CONFIRMED / 4 PARTIAL / 7 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`
- Evidence Gate recommendation: `PASS`
- Next allowed gate: `OUTLINE`
