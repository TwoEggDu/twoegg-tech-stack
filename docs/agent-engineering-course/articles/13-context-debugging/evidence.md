# Article 13 Evidence｜Context Debugging

## Status

- Preliminary Evidence Gate：`PASS / FROZEN`
- Research evidence：`COMPLETE / FROZEN`
- Evidence Gate：`PASS / EVIDENCE_READY`
- Claim Register：`9 CORE CLAIMS / 3 CONFIRMED / 0 PARTIAL / 6 PROPOSAL`
- Core BLOCKED Claims：`0`
- Required Lab：`Lab 05 / OBSERVED / EVIDENCE_MERGED`
- Evidence Merge：`COMPLETE / FIXTURE-SCOPED`
- Source manifest：`11 PRIMARY SOURCES / FROZEN AT RETRIEVED SCOPE`
- Retrieved scope：`2026-08-22 / Asia/Shanghai`

## Final Claim Register

| Claim ID | Final primary status | Local Lab support | Lab Dependency disposition | Evidence refs |
|---|---|---|---|---|
| `13-C01` | `PROPOSAL` | `PARTIAL / A-D,F-G APPLICATION-VISIBLE` | `SATISFIED_FOR_LOCAL_FIXTURE / CONSUMPTION UNTESTED` | `13-E01`, `13-LE01`, `13-LE03` |
| `13-C02` | `PROPOSAL` | `PARTIAL / A-G MANDATORY CASES` | `PARTIAL_CASE_COVERAGE` | `13-E02`, `13-LE01`, `13-LE02`, `13-LE03` |
| `13-C03` | `CONFIRMED` | `NOT_APPLICABLE / NO LAB UPGRADE` | `NONE` | `13-E03` |
| `13-C04` | `CONFIRMED` | `NOT_APPLICABLE / NO PROVIDER CALL` | `NONE` | `13-E04` |
| `13-C05` | `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED` | `CONFIRMED / CASE E` | `SATISFIED_FOR_EXACT_FIXTURE` | `13-E05`, `13-LE02` |
| `13-C06` | `PROPOSAL` | `PARTIAL / CASE F LOCAL DISPOSITION` | `PARTIAL / V4 NOT OBSERVED` | `13-E06`, `13-LE03` |
| `13-C07` | `PROPOSAL` | `CONFIRMED / A,F,G LOCAL CONFORMANCE` | `SATISFIED_FOR_APP_VISIBLE_FIXTURE` | `13-E07`, `13-LE03` |
| `13-C08` | `PROPOSAL` | `PARTIAL / G L0-vs-L1 CEILING` | `PARTIAL / L2-L3 NOT FULLY TESTED; L4 UNSUPPORTED` | `13-E08`, `13-LE03` |
| `13-C09` | `PROPOSAL` | `CONFIRMED / TDD+A-G+RUN-A/B CONFORMANCE` | `SATISFIED_FOR_LOCAL_PROTOCOL` | `13-E09`, `13-LE01`, `13-LE04` |

## Preliminary Evidence Freeze

本节冻结的是进入 `LAB_DESIGN` 前的 evidence contract，不是 Lab Design、implementation 或 observed result。`Current maximum strength` 是本 Gate 允许进入后续写作资产的最强措辞；`Post-Lab ceiling` 也只是未来 Lab 按 frozen fixture 得到支持时的最高上限，不在本 Gate 提前生效。

| Claim | Frozen status | Exact official/current source scope | Counter-evidence retained | Observable predicate | Lab dependency | Current maximum strength | Post-Lab ceiling |
|---|---|---|---|---|---|---|---|
| `13-C01` | `PROPOSAL` | Course-owned taxonomy；product examples only: `OAI-01` OpenAI `POST /responses`, deprecated `truncation`, request-selected model；`ANT-03` Anthropic Messages beta, header `context-management-2025-06-27`, named context-edit features/models；retrieved 2026-08-22 | final failure 也可能来自 evaluation、model variability 或 hidden Provider behavior；三层可暂时重叠 | assembly input 已违约；或 pre-transform inputs 合规但 app-visible post-transform Snapshot 违约；两者均通过而 deterministic contract 仍失败时只记 consumption candidate，内因 UNKNOWN | `REQUIRED_FOR_FIXTURE_CONFORMANCE` | 可提出三层 application-visible 定位法；不可断言真实故障归因 | 最多证明 frozen local fixture 能按 observable facts 路由；不能证明真实模型 consumption cause |
| `13-C02` | `PROPOSAL` | Course-owned taxonomy；mechanism examples only: `OAI-01/02` OpenAI Responses truncation / `gpt-5.3-codex` compaction example；`ANT-03/04` Anthropic Messages beta headers `context-management-2025-06-27` / `compact-2026-01-12`, features/models as manifest；retrieved 2026-08-22 | omission 可像 Missing，compaction 可像 Truncation；标签可共现且不穷尽 | 每个 label 只由 RQ2 frozen predicate + actor/stage/event evidence 触发，不能由回答质量触发 | `REQUIRED_FOR_CASE_COVERAGE` | 可把八类标签称为非互斥 `COURSE PROPOSAL` | 最多证明 Cases A–G 对 frozen predicates 的覆盖与 false-positive controls；不能成为 Provider taxonomy |
| `13-C03` | `CONFIRMED` | `PAPER-01` TACL 2024 multi-document QA / key-value retrieval, paper-listed 2023-era models；`PAPER-02` ICML 2023 GSM-IC, paper-listed models/prompts；`ANT-01` Anthropic context-window guidance/models as named by page；retrieved 2026-08-22 | 相关且组织良好的 context 可能有帮助；2026 models/tasks 与论文不同 | cited controlled tests 至少存在一个“增加/移动 context 未改善或降低 metric”的 counterexample | `NONE` | 只可写“more context 不是通用可靠性保证” | 不因 Lab 升级；不得写“context 越多越差”或当前模型降幅 |
| `13-C04` | `CONFIRMED` | `OAI-01` OpenAI `POST /responses` deprecated truncation；`OAI-02` Responses compaction example model `gpt-5.3-codex`；`OAI-03` `POST /responses/compact`；`ANT-01` Messages overflow behavior as named；`ANT-03/04` beta headers/features/models above；all retrieved 2026-08-22 | mechanisms 可组合；hosted docs 与 model support 会变化 | official docs 给出不同 parameter / beta header / feature marker / replacement artifact / error or stop contract | `NONE` | 只可陈述 fixed Provider/API/model/feature/retrieved-date 文档差异 | 不因 Lab 升级；不得声称某生产 request 已触发或跨模型一致 |
| `13-C05` | `PARTIAL` | `OAI-02/03` OpenAI Responses compaction (`gpt-5.3-codex` guide example / request-selected compact endpoint)；`ANT-03/04` Messages beta context editing / `claude-opus-5` compaction example and page-listed support；retrieved 2026-08-22 | Provider 目标是保留关键状态；client 可能保留完整 history；文档未证明课程字段实际丢失 | frozen required field 在 deterministic transform 前存在，之后 absent、unverifiable 或按 frozen invariant 被错误强化/折叠 | `REQUIRED` | 只能写 replacement creates a testable loss risk；不能写 Provider 已丢失具体字段 | 最多证明 frozen local transformer/fixture 的具体 pre/post loss；不能外推 Provider 或模型效果 |
| `13-C06` | `PROPOSAL` | Course observability contract；examples: `OAI-01/02` OpenAI Responses truncation/compaction；`ANT-01/03/04` Anthropic Messages overflow/context-editing/compaction；exact manifest scopes，retrieved 2026-08-22 | 不同 mechanisms 可得到相似 Snapshot，且可能连续发生 | event record 的 actor + stage + mechanism + control/version + disposition/reason 能区分 intentional omission、app trim、provider-documented transform/truncation 与 hard limit | `REQUIRED_FOR_EVENT_SEPARATION` | 可提出分字段落账要求 | 最多证明 local fixture event schema 可无歧义区分 injected mechanisms；不证明 Provider 实际发出这些字段 |
| `13-C07` | `PROPOSAL` | Frozen Article 12 course contract；`OAI-04` OpenAI `POST /responses/input_tokens`, request-selected model；`OAI-05` OpenAI Agents SDK hosted tracing docs, package version not pinned；`ANT-02` `POST /v1/messages/count_tokens`, request-selected model；retrieved 2026-08-22 | unredacted enabled trace 可补充 input/output；retained immutable bytes + digest 可验证 app-visible equality | Receipt 能列出/比较 contributor、scope、revision、order、disposition、transform 与 digest delta；缺少 bytes/locator 时 reconstruction predicate 必须 false/UNKNOWN | `REQUIRED_FOR_APP_VISIBLE_DIFF` | 只可保证 application-visible Snapshot 的 describe/audit/compare | 最多证明 frozen fixture 的 app-visible diff detection；不保证 Provider-internal/full-token reconstruction |
| `13-C08` | `PROPOSAL` | Frozen Article 12 boundary；capability ceilings informed by `OAI-04/05` and `ANT-02` exact scopes above；retrieved 2026-08-22 | 完整 retained bytes/trace 的系统可能达到更高 level；levels 不是自动递进保证 | L0 fields present；L1 requires retained bytes or resolvable locator + canonicalization；L2 requires frozen parser/schema/invariants；L3 requires deterministic rules/inputs/version；否则该 level false/UNKNOWN | `REQUIRED` | 可提出 Reconstruction Ladder 与各层 prerequisites | 最多证明 fixture 实际实现的 L0–L3 level；L4 Provider-internal/full-token 仍 UNKNOWN/UNSUPPORTED |
| `13-C09` | `PROPOSAL` | Course Factory/production contract and Article 13 Research；no Provider behavior claim | deterministic local protocol 不验证真实模型 consumption 或 Provider internals | frozen request/scope/revision/policy/transformer repeated with same inputs yields identical app-visible artifacts/verdicts；all injected faults retain raw evidence | `REQUIRED` | 可冻结 protocol requirements，不可声称已可执行/有效 | 最多证明 implemented local fixture 的 repeatability and case coverage；不证明 production best practice |

### Core BLOCKED audit

- Core Claim count：`9`
- `CONFIRMED`：`2` (`13-C03`, `13-C04`)
- `PARTIAL`：`1` (`13-C05`)
- `PROPOSAL`：`6` (`13-C01`, `13-C02`, `13-C06`, `13-C07`, `13-C08`, `13-C09`)
- `BLOCKED`：`0`
- Gate consequence：`PRELIMINARY_EVIDENCE / PASS`。这只允许进入 `LAB_DESIGN`；Evidence Gate 仍不可运行或通过。

## Lab 05 Validation Matrix｜Cases A–G

这是 preliminary evidence 对后续 Lab Design 的 mandatory validation needs，不是 durable Lab Card、Hypothesis、Commands、Acceptance Criteria 或 Observed Result。Cases A–G 的 identity 与含义在本 Gate 固定，不得由后续设计重排或替换；禁止真实 Provider/model call。

| Case | Injected condition to be designed later | Observable predicate to validate | Related claims | Expected evidence need, not observation | Maximum claim if later validated |
|---|---|---|---|---|---|
| `A — Baseline Good Context` | current Goal / State、correct Evidence / capability、bounded history；生成 valid application-visible Snapshot / Receipt | required contributors、current revisions、correct scope、resolved/no conflict、budget/reserve 与 receipt fields 全部满足 frozen contract，且无 fault label | `C01`, `C02`, `C06`, `C07`, `C09` | frozen baseline contributors、Snapshot、Receipt、budget ledger 与 no-fault verdict | fixture baseline 可重复且 receipt 可 describe/audit/compare；不证明模型输出质量 |
| `B — Stale Context` | current State 为 `rev17`，source summary 仍标 `rev14` | revision predicate 必须输出 `STALE / REVISION_MISMATCH`，并保留 expected=`rev17`、actual=`rev14` 与 source provenance | `C01`, `C02`, `C09` | frozen authoritative/current revision、stale contributor metadata、diagnosis 与 receipt delta | fixture 能检测这一个明确 revision mismatch；不证明任何生产 source 的 authority |
| `C — Pollution` | 注入 irrelevant old tool result、obsolete plan 与 unrelated history | 按事前 frozen relevance policy 识别 irrelevant contributors；不得使用模型回答质量作为 predicate | `C02`, `C03`, `C09` | contributor identity、locator、relevance rule/version、selected/disposition 与 pollution verdict | fixture 能识别 frozen-policy irrelevant contributors；不证明额外 context 普遍降低模型质量 |
| `D — Conflict` | 一个 contributor 记录 `build failed`，另一个记录 `build succeeded` | conflict、双方 provenance / revision / order 必须保留；没有 frozen resolution rule 时不得自动选择任一结论 | `C01`, `C02`, `C05`, `C09` | 两个原始 contributor、conflict key、provenance、resolution=`UNRESOLVED` 与 Snapshot/Receipt representation | fixture 能检测并保留该显式冲突；不证明模型会正确消费冲突 |
| `E — Compression Loss` | pre-transform facts 为 `EV-1 SUPPORTED`、`EV-2 CONTRADICTS`、`UNKNOWN root cause`；deterministic bad compressor 产出 `Root cause confirmed.` | pre/post invariant diff 必须检测 uncertainty、conflict 与 provenance loss，以及非法 claim-strength upgrade；C05 仍 `PARTIAL` 直到真实 Lab observation merge | `C02`, `C05`, `C08`, `C09` | retained pre/post bytes、EV-1/EV-2 provenance、UNKNOWN marker、canonical digests、field/invariant diff、transformer version | 最多证明 frozen bad compressor 在该 fixture 的具体 loss；不外推 Provider compaction 或模型效果 |
| `F — Truncation / Budget` | deterministic budget fitter 必须保留 P0/P1，先移除 optional history，保留 output reserve；若 required Evidence 会被 trim 则 fail closed | order、priority、omission reason、budget/reserve ledger 可审计；optional-first policy 生效；required Evidence trim attempt 产生 closed failure 而非 silent Snapshot | `C02`, `C06`, `C07`, `C09` | before/after items、P0/P1、optional history、reserve、disposition、failure record 与 digest | fixture 能验证 frozen local budget policy 与 fail-closed path；不证明 Provider token 精确值或内部 truncation behavior |
| `G — Reconstruction Boundary` | Receipt 保留 ref / digest / order / disposition，但 original contributor absent | metadata 仍 `AUDITABLE`；由于 bytes 与 resolvable locator 不存在，`RECONSTRUCTABLE=false/UNKNOWN`；digest 只可校验候选 bytes，不能执行 byte reconstruction | `C07`, `C08`, `C09` | Receipt fields、retention/locator availability、reconstruction-level verdict 与 explicit UNKNOWN | fixture 能证明该场景 `AUDITABLE != RECONSTRUCTABLE`；不保证 Provider-internal/full-token reconstruction |

### Additional validation variants（不得替代 Cases A–G）

| Variant | Predicate retained for later LAB_DESIGN | Related claims | Boundary |
|---|---|---|---|
| `V1 — Missing vs intentional omission` | required contributor 无 disposition/reason 时命中 Missing；存在 frozen-policy omission record 时只记 intentional omission | `C01`, `C02`, `C06`, `C09` | 只验证 application-visible records |
| `V2 — Wrong Scope` | tenant / user / task / step / environment / time scope 与 frozen request 不匹配时命中；matching control 不误报 | `C01`, `C02`, `C09` | scope rule 必须事前冻结 |
| `V3 — Overpacked` | packed size 越过 frozen threshold、侵占 reserve 或触发 local transform 时命中；仅 token 多但未违约不命中 | `C02`, `C06`, `C09` | 不推断模型质量 |
| `V4 — Event separation` | actor + stage + mechanism + control/version + disposition/reason 区分 omission、app trim、provider-documented transform/truncation 与 hard limit | `C04`, `C06`, `C07` | Provider examples 仍只按 manifest scope；Lab 不模拟 Provider internals |

## Preliminary Evidence Cards（frozen pre-Lab snapshot）

以下 `13-E01`—`13-E09` 保留 PRELIMINARY_EVIDENCE 时点，供审计“Lab 前知道什么”。最终 status、local support 与 wording 以 `Final Claim Register`、`Final Evidence Merge` 和 `13-LE01`—`13-LE04` 为准。

### Evidence 13-E01｜Three diagnostic layers

- Article: `13 Context Debugging`
- Claim ID: `13-C01`
- Claim: `Assembly / Packing / Consumption 是三个应分开的 application-visible 诊断层；Provider 内因无证据时保持 UNKNOWN。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course design + official product docs`
- Source: `Article 12 Final Gate; Article 13 research.md; OpenAI Responses create https://developers.openai.com/api/reference/cli/resources/responses/methods/create ; Anthropic context editing https://platform.claude.com/docs/en/build-with-claude/context-editing`
- Repository: `TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Symbol: `RQ1-RQ3`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `COURSE PROPOSAL; product examples limited to manifest OAI-01 and ANT-03 scopes`
- Reproduction: `Deferred to LAB_DESIGN`
- Observation: `Provider docs expose distinct application/API controls and server-managed transforms, but do not define this three-layer taxonomy or reveal exact internal causes.`
- Counter-evidence Searched: `A final output failure may come from application assembly, transform, model variability, hidden Provider behavior, or evaluation error.`
- Interpretation: `Separating observable application stages avoids assigning an unsupported internal cause.`
- Proves: `Nothing implemented; this is an explicit diagnostic design choice.`
- Does Not Prove: `A real failure belongs to one layer, or a model consumed context incorrectly.`
- Limitations: `Non-exhaustive; layers may overlap until evidence resolves them.`
- Course Usage: `Use design language and preserve UNKNOWN.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED_FOR_FIXTURE_CONFORMANCE`

### Evidence 13-E02｜Observable diagnosis taxonomy

- Article: `13 Context Debugging`
- Claim ID: `13-C02`
- Claim: `八类标签可作为带 frozen observable predicate 的非互斥、非穷尽 taxonomy。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course design`
- Source: `docs/agent-engineering-course/articles/13-context-debugging/research.md RQ2; OpenAI compaction https://developers.openai.com/api/docs/guides/compaction ; Anthropic compaction https://platform.claude.com/docs/en/build-with-claude/compaction`
- Repository: `TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Symbol: `RQ2`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `COURSE PROPOSAL; Provider docs are examples, not taxonomy authority`
- Reproduction: `Deferred to LAB_DESIGN fault-injection cases`
- Observation: `Official docs distinguish drop, summary, compaction item and placeholder mechanisms; no official source defines the eight labels as one system.`
- Counter-evidence Searched: `Intentional omission can resemble Missing; managed compaction can resemble Truncation; labels may co-occur.`
- Interpretation: `Predicates, actor/stage evidence and multi-label support are required to prevent outcome-based diagnosis.`
- Proves: `The taxonomy is explicitly course-owned and bounded.`
- Does Not Prove: `Case coverage, classifier correctness, exhaustiveness, or Provider adoption.`
- Limitations: `Fixture predicates remain to be frozen.`
- Course Usage: `Label every taxonomy presentation COURSE PROPOSAL.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED_FOR_CASE_COVERAGE`

### Evidence 13-E03｜More context is not a reliability guarantee

- Article: `13 Context Debugging`
- Claim ID: `13-C03`
- Claim: `增加 context 不是可靠性保证；相关性、位置、任务与模型会影响测试结果。`
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `peer-reviewed primary papers + official Provider guidance`
- Source: `Liu et al., TACL 2024 https://aclanthology.org/2024.tacl-1.9/ ; Shi et al., ICML 2023 https://proceedings.mlr.press/v202/shi23a.html ; Anthropic context windows https://platform.claude.com/docs/en/build-with-claude/context-windows`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `paper-listed multi-document QA / key-value retrieval / GSM-IC experiments`
- Fixture: `paper-listed datasets and prompts`
- Trace: `paper results; no local raw trace`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `paper-listed 2023-era models/tasks; Anthropic current guidance retrieved 2026-08-22`
- Reproduction: `N/A for this Research Gate; consult paper methods`
- Observation: `The papers report position-dependent performance and degradation from irrelevant context in their controlled tasks.`
- Counter-evidence Searched: `Relevant, well-placed context may improve a task; current 2026 models differ from paper models.`
- Interpretation: `A universal “more is always more reliable” statement has primary counterexamples.`
- Proves: `More context is not a general reliability guarantee in the cited test scopes.`
- Does Not Prove: `More context is generally harmful, or current OpenAI/Anthropic models have the same effect size.`
- Limitations: `No current-model or production experiment was run.`
- Course Usage: `Use only the scoped negative claim; no model-performance number.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `NONE`

### Evidence 13-E04｜Provider mechanisms are distinct and versioned

- Article: `13 Context Debugging`
- Claim ID: `13-C04`
- Claim: `truncation、compaction、context editing 与 hard-limit behavior 是不同且 versioned 的 mechanisms。`
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official API / feature documentation`
- Source: `OpenAI Responses create https://developers.openai.com/api/reference/cli/resources/responses/methods/create ; OpenAI compaction https://developers.openai.com/api/docs/guides/compaction ; Anthropic context windows https://platform.claude.com/docs/en/build-with-claude/context-windows ; context editing https://platform.claude.com/docs/en/build-with-claude/context-editing ; compaction https://platform.claude.com/docs/en/build-with-claude/compaction`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `N/A`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `OpenAI Responses truncation and gpt-5.3-codex guide example; Anthropic Messages beta headers context-management-2025-06-27 and compact-2026-01-12, feature names and model support as retrieved`
- Reproduction: `Read parameter/feature contracts at fixed URLs and retrieved date.`
- Observation: `Docs describe start-of-conversation item dropping, opaque compaction items, placeholder-based clearing, summary blocks, and model-dependent overflow behavior as separate controls/features.`
- Counter-evidence Searched: `Features can overlap operationally; hosted docs can change; model support is not universal.`
- Interpretation: `A receipt must record the named mechanism and version rather than a generic “context shortened” flag.`
- Proves: `The documented mechanisms differ in the fixed scopes.`
- Does Not Prove: `A mechanism ran in a particular production request or behaves identically across models/providers.`
- Limitations: `No live API call; retrieved-date documentation only.`
- Course Usage: `Provider fact box with full scope lock.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `NONE`

### Evidence 13-E05｜Compression loss is a testable risk

- Article: `13 Context Debugging`
- Claim ID: `13-C05`
- Claim: `compression 可能使课程要求字段在 pre/post view 间不可验证；具体 loss 只能 fixture-scoped。`
- Evidence Status: `PARTIAL`
- Evidence Class: `INFERENCE`
- Source Type: `official docs + engineering inference`
- Source: `OpenAI compaction https://developers.openai.com/api/docs/guides/compaction ; OpenAI compact response https://developers.openai.com/api/reference/java/resources/responses/methods/compact ; Anthropic context editing https://platform.claude.com/docs/en/build-with-claude/context-editing ; Anthropic compaction https://platform.claude.com/docs/en/build-with-claude/compaction`
- Repository: `N/A`
- Commit: `N/A`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `OAI-02/03 and ANT-03/04 manifest scopes`
- Reproduction: `Deferred: deterministic pre/post transformer comparison`
- Observation: `Docs confirm prior content may be represented by an opaque compacted item, summary or placeholder.`
- Counter-evidence Searched: `Providers intend compaction to preserve key/task-relevant state; client history may remain complete; docs do not report loss of the course fields.`
- Interpretation: `Replacement creates an audit risk, but actual provenance/scope/uncertainty/conflict/ordering/negative-evidence/locator loss needs a frozen fixture.`
- Proves: `Transformation/replacement mechanisms exist in fixed product scopes.`
- Does Not Prove: `Any listed field is lost by a Provider feature, or model output quality changes.`
- Limitations: `No Lab raw Observation; the loss list is a verification target.`
- Course Usage: `Present as PARTIAL risk and Lab question, not Provider defect.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED`

### Evidence 13-E06｜Disposition and transformation events stay separate

- Article: `13 Context Debugging`
- Claim ID: `13-C06`
- Claim: `intentional omission、app trim、provider truncation/transform、hard limit 应分开落账。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course observability contract + official API examples`
- Source: `Article 13 research.md RQ5; OAI-01/02; ANT-01/03/04 in current source manifest`
- Repository: `TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Symbol: `RQ5`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `COURSE PROPOSAL; examples limited to fixed Provider scopes`
- Reproduction: `Deferred to LAB_DESIGN`
- Observation: `Provider docs assign different actors, controls, markers and error/stop outcomes to the mechanisms.`
- Counter-evidence Searched: `The final Snapshot may look similar after different mechanisms; some application and Provider transforms can both occur.`
- Interpretation: `actor + stage + mechanism + control + event + disposition fields preserve the distinction.`
- Proves: `Nothing implemented; source facts motivate the schema choice.`
- Does Not Prove: `The proposed fields are sufficient or recorded by Providers.`
- Limitations: `No fixture or schema validation yet.`
- Course Usage: `Design-language receipt extension only.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED_FOR_EVENT_SEPARATION`

### Evidence 13-E07｜Receipt ceiling

- Article: `13 Context Debugging`
- Claim ID: `13-C07`
- Claim: `Receipt 支持 app-visible Snapshot 的 describe/audit/compare，不保证 Provider-internal/full-token reconstruction。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `frozen course contract + official capability/limitation docs`
- Source: `Article 12 review.md Final Gate; OpenAI token counting https://developers.openai.com/api/docs/guides/token-counting ; OpenAI tracing https://openai.github.io/openai-agents-python/tracing/ ; Anthropic token counting https://platform.claude.com/docs/en/build-with-claude/token-counting`
- Repository: `TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/12-context-engineering/review.md; docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Symbol: `Article 12 Final Gate; Article 13 RQ6`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `Course Receipt contract; token/tracing feature docs retrieved 2026-08-22`
- Reproduction: `Deferred app-visible diff fixture`
- Observation: `Token count endpoints return counts, not content/provenance; SDK trace may be disabled, unavailable under ZDR, or redact sensitive data; Provider-managed transforms may be opaque.`
- Counter-evidence Searched: `Enabled unredacted trace can supplement model input/output evidence; retained immutable bytes plus digest can verify app-visible equality.`
- Interpretation: `Receipt remains useful for app-side audit without claiming unavailable internal completeness.`
- Proves: `The course intentionally caps Receipt claims; current APIs do not supply a universal full reconstruction guarantee.`
- Does Not Prove: `No Provider can ever expose fuller telemetry, or Receipt alone reconstructs bytes/semantics/decisions.`
- Limitations: `Negative capability conclusion is current-source and interface-scoped.`
- Course Usage: `Hard stop line repeated in diagnosis and reconstruction sections.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED_FOR_APP_VISIBLE_DIFF`

### Evidence 13-E08｜Reconstruction ladder

- Article: `13 Context Debugging`
- Claim ID: `13-C08`
- Claim: `metadata、app-visible bytes、semantic、decision、Provider-internal reconstruction 各有独立前提，未满足则 UNKNOWN。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course design`
- Source: `Article 13 research.md RQ6; Article 12 review.md Final Gate`
- Repository: `TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Symbol: `RQ6`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `COURSE PROPOSAL; Provider-internal level explicitly unsupported by current Receipt contract`
- Reproduction: `Deferred to LAB_DESIGN`
- Observation: `A digest can compare known bytes but cannot recover missing bytes; semantic and decision replay require frozen parser/rules/inputs beyond metadata.`
- Counter-evidence Searched: `Some systems may retain complete bytes or traces and reach a higher level; levels are not automatically monotonic guarantees.`
- Interpretation: `Naming prerequisites prevents metadata audit from being mislabeled reconstruction.`
- Proves: `Nothing implemented; the ladder is a bounded claim vocabulary.`
- Does Not Prove: `L1-L3 work, preserve all semantics, or replay real model behavior.`
- Limitations: `Must be tested only against deterministic fixture invariants.`
- Course Usage: `COURSE PROPOSAL table; Provider/full-token remains UNKNOWN.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED`

### Evidence 13-E09｜Deterministic debugging protocol

- Article: `13 Context Debugging`
- Claim ID: `13-C09`
- Claim: `调试协议需冻结 scope/version/policy、比较 pre/post、保留 unknown，并以 deterministic fixture 回归。`
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course verification design requirement`
- Source: `Article 13 research.md RQ7; course-factory.md; production-workflow.md`
- Repository: `TechStackShow`
- Commit: `N/A`
- File: `docs/agent-engineering-course/articles/13-context-debugging/research.md`
- Symbol: `RQ7`
- Call Path: `N/A`
- Experiment: `Lab 05 / NOT_INSTANTIATED`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-22 / Asia/Shanghai`
- Version Scope: `Article 13 research requirement only`
- Reproduction: `Commands, fixture and acceptance criteria intentionally deferred to LAB_DESIGN`
- Observation: `No Lab 05 design, execution, fault injection, raw observation or model call exists at this Gate.`
- Counter-evidence Searched: `A deterministic local packer can validate application invariants but cannot validate real-model consumption or Provider internals.`
- Interpretation: `The protocol must separate reproducible application evidence from unsupported model claims.`
- Proves: `Only that verification requirements and stop lines are frozen.`
- Does Not Prove: `Protocol executability, classifier correctness, reconstruction success, or model performance.`
- Limitations: `All behavioral evidence awaits later Lab gates.`
- Course Usage: `Handoff requirements for LAB_DESIGN; no premature confirmation.`
- BuildPilot Implication: `N/A`
- Owner: `/root/article_13_researcher`
- Verified At: `2026-08-22`
- Lab Dependency: `REQUIRED`

## Lab Evidence Cards

### Evidence 13-LE01｜TDD and mandatory diagnosis conformance

- Article: `13 Context Debugging`
- Claim IDs: `13-C01`, `13-C02`, `13-C09`
- Evidence Status: `CONFIRMED / LOCAL FIXTURE CONFORMANCE`（primary design claims remain `PROPOSAL`）
- Evidence Class: `RUNTIME_OBSERVATION / EXPERIMENT`
- Source Type: `Lab 05 README observation summary; raw refs not reread in this Gate`
- Source: `docs/agent-engineering-course/labs/lab-05-context-debugging/README.md Sections 14.1-14.4`
- Experiment: `lab05-fixture-v1 / offline C# .NET 10 / TDD RED-GREEN / Cases A-D`
- Retrieved / Run At: `2026-08-22 / China Standard Time +08:00`
- Version Scope: `Windows 10 build 19045 / X64 / .NET SDK 10.0.301 / Runtime 10.0.9 / net10.0 / BCL-only`
- Observation: `RED Spec exit 1 with A-G 7/7 failures; GREEN 15/15; A GOOD_CONTEXT, B rev17-vs-rev14 STALE/REVISION_MISMATCH, C three irrelevant contributors, D unresolved build conflict with both provenance records.`
- Counter-evidence Searched: `Fixture does not exercise real model consumption; Missing/Wrong Scope optional variants are not reported in the README observation summary.`
- Interpretation: `The local public behavior conforms for the mandatory application-visible cases, but the three-layer/taxonomy/protocol remain course designs.`
- Proves: `Local deterministic conformance for the listed cases and test protocol.`
- Does Not Prove: `Exhaustive taxonomy, Provider taxonomy, model reasoning, or production root cause.`
- Limitations: `README-summary evidence only; raw artifacts were deliberately not reread during merge.`
- Course Usage: `Use as fixture example with explicit COURSE PROPOSAL label.`
- Verified At: `2026-08-22`

### Evidence 13-LE02｜BAD_COMPRESSOR_V1 loss detection

- Article: `13 Context Debugging`
- Claim ID: `13-C05`
- Evidence Status: `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED`
- Evidence Class: `RUNTIME_OBSERVATION / EXPERIMENT`
- Source Type: `Lab 05 README observation summary`
- Source: `Lab 05 README Sections 14, 14.4 Case E, 16, 18 raw references`
- Experiment: `lab05-fixture-v1 / Case E / BAD_COMPRESSOR_V1`
- Retrieved / Run At: `2026-08-22 / China Standard Time +08:00`
- Version Scope: `exact precondition EV-1 SUPPORTED + EV-2 CONTRADICTS + UNKNOWN root cause; exact output Root cause confirmed.`
- Observation: `The named bad compressor emitted the exact frozen output; independent verifier detected UNCERTAINTY, CONFLICT, PROVENANCE and CLAIM_STRENGTH loss.`
- Counter-evidence Searched: `This compressor is intentionally faulty; current Provider docs do not say their compaction loses these fields and may aim to preserve key state.`
- Interpretation: `The originally broad compression-risk claim is narrowed to a confirmed deterministic fault-injection observation.`
- Proves: `Case E detector catches the four named loss dimensions for BAD_COMPRESSOR_V1.`
- Does Not Prove: `OpenAI/Anthropic compaction loss, summary quality, model accuracy, hallucination or causal production impact.`
- Limitations: `Single artificial transform and frozen bytes.`
- Course Usage: `Maximum wording must name BAD_COMPRESSOR_V1 and lab05-fixture-v1.`
- Verified At: `2026-08-22`

### Evidence 13-LE03｜Budget, Receipt and reconstruction ceiling

- Article: `13 Context Debugging`
- Claim IDs: `13-C01`, `13-C02`, `13-C06`, `13-C07`, `13-C08`
- Evidence Status: `MIXED / LOCAL CONFORMANCE AS FINAL REGISTER`
- Evidence Class: `RUNTIME_OBSERVATION / EXPERIMENT`
- Source Type: `Lab 05 README observation summary`
- Source: `Lab 05 README Section 14.4 Cases F-G and Section 16`
- Experiment: `lab05-fixture-v1 / Cases F-G`
- Retrieved / Run At: `2026-08-22 / China Standard Time +08:00`
- Version Scope: `deterministic integer budget units; application-visible Receipt; missing bytes and unresolvable locator`
- Observation: `F omitted optional history first, retained P0/P1 and four output units, and returned explicit ABSENT Snapshot + REQUIRED_EVIDENCE_BUDGET_EXCEEDED/FAIL_CLOSED for required overflow. G remained AUDITABLE but NOT_RECONSTRUCTABLE with ORIGINAL_BYTES_ABSENT, LOCATOR_UNRESOLVABLE and DIGEST_NOT_CONTENT; Provider-internal was UNKNOWN_UNSUPPORTED.`
- Counter-evidence Searched: `Budget units are not Provider tokens; a system retaining bytes/resolvable locator could be reconstructable; V4 Provider-event separation was not observed.`
- Interpretation: `The local Receipt contract and negative reconstruction boundary conform; the taxonomy, event schema and ladder remain proposals, with only scoped support.`
- Proves: `Local fail-closed packing and app-visible audit/reconstruction distinction.`
- Does Not Prove: `Provider truncation, full-token reconstruction, semantic equivalence, L2/L3 completeness or production retention behavior.`
- Limitations: `Synthetic budget and absence conditions.`
- Course Usage: `Use F/G as deterministic examples; keep Receipt ceiling explicit.`
- Verified At: `2026-08-22`

### Evidence 13-LE04｜Repeatability and recovered tooling failures

- Article: `13 Context Debugging`
- Claim ID: `13-C09`
- Evidence Status: `CONFIRMED / DETERMINISTIC LOCAL FIXTURE-SCOPED`
- Evidence Class: `RUNTIME_OBSERVATION / EXPERIMENT`
- Source Type: `Lab 05 README observation summary`
- Source: `Lab 05 README Sections 14.2, 14.3, 14.5, 16 and Section 18 raw references`
- Experiment: `fresh-process run A/B + compare`
- Retrieved / Run At: `2026-08-22 / China Standard Time +08:00`
- Version Scope: `same Release binary/fixture; 59 compared files; aggregate SHA-256 621cde0ec3c1ed7b7f6334dac4d0187b522625f37e220957e48b6dcac3bd3f50`
- Observation: `59 files were direct-byte and SHA-256 identical. CS0411, non-escalated helper_unknown_error, initial timestamp omission and invalid PowerShell ReadOnlySpan audit helper were retained; recovery/closure results were recorded separately.`
- Counter-evidence Searched: `Environment/TDD/execution metadata are outside normalized compare; one OS/runtime and two runs do not prove cross-platform or production determinism.`
- Interpretation: `The frozen normalized artifact pipeline is repeatable in this environment, while recovered tooling failures remain limitations rather than Lab RED or hidden success.`
- Proves: `Two-run local normalized artifact repeatability and transparent failure retention.`
- Does Not Prove: `Availability, performance, production reliability, distributed behavior or universal determinism.`
- Limitations: `Two fresh runs on one Windows/.NET environment; merge relies on README summary.`
- Course Usage: `Protocol evidence and disclosed-failure sidebar.`
- Verified At: `2026-08-22`

## Final Evidence Merge

| Claim | Experiment | Observation from Lab README summary | Evidence Interpretation | Final Claim Status | Maximum article wording |
|---|---|---|---|---|---|
| `13-C01` | A-D,F-G | observable good/stale/pollution/conflict/budget/reconstruction cases conformed | supports application-visible layer routing, not consumption/internal cause | `PROPOSAL`; local support `PARTIAL` | “课程把 Assembly/Packing/Consumption 分层；fixture 能定位若干应用侧差异，模型/Provider 内因仍 UNKNOWN。” |
| `13-C02` | A-G | mandatory cases conformed | several predicates work locally, but taxonomy is non-exhaustive and optional Missing/Wrong Scope/Overpacked coverage is not in summary | `PROPOSAL`; local support `PARTIAL` | “这是 COURSE PROPOSAL；Lab 只验证 mandatory A-G 的 frozen predicates。” |
| `13-C03` | none required | Lab C classifies irrelevant contributors without model output | no effect on paper/provider-scoped negative claim | `CONFIRMED / CURRENT-SOURCE TEST-SCOPED` | “更多 context 不是通用可靠性保证”；不得写当前模型降幅或“越多越差” |
| `13-C04` | no Provider call | Provider/model/network all NONE | Lab neither verifies nor changes current product-doc claims | `CONFIRMED / CURRENT PRODUCT-DOC SCOPE` | 只按 manifest 的 Provider/API/model/feature/version/retrieved date 陈述机制差异 |
| `13-C05` | E | exact bad-compressor output and four losses detected | direct support after narrowing to named fixture | `CONFIRMED / BAD_COMPRESSOR_V1 FIXTURE-SCOPED` | “在 lab05-fixture-v1，BAD_COMPRESSOR_V1 的确定结论被检出 uncertainty/conflict/provenance/claim-strength loss。” |
| `13-C06` | F | local omission reasons/fail-closed event observed | supports some local event separation; V4/Provider paths untested | `PROPOSAL`; local support `PARTIAL` | “课程建议分开落账；Lab 只确认 Case F local dispositions。” |
| `13-C07` | A,F,G | Receipt described local Snapshot/budget/disposition; G audit survived without reconstruction | local conformance supports ceiling, not industry standard | `PROPOSAL`; local support `CONFIRMED` | “该 fixture Receipt 可 describe/audit/compare app-visible Snapshot；不保证 Provider/full-token reconstruction。” |
| `13-C08` | E,G | G L0 audit yes/L1 byte reconstruction no; E exposes transform loss | validates one negative boundary, not full ladder | `PROPOSAL`; local support `PARTIAL` | “G 证明 digest metadata 可 audit 但不能恢复 bytes；L2/L3 未完整证明，L4 UNKNOWN/UNSUPPORTED。” |
| `13-C09` | RED/GREEN+A-G+run A/B | genuine RED, 15/15 GREEN, A-G pass, 59-file repeatability, recovered failures retained | strong local conformance for frozen protocol | `PROPOSAL`; local support `CONFIRMED` | “frozen offline protocol 在同一 Windows/.NET fixture 可重复；不外推生产/跨平台。” |

### Final counter-evidence and limitations retained

- Provider compaction 旨在保留关键状态，当前文档未证明它会复现 Case E；`BAD_COMPRESSOR_V1` 是刻意错误的 local transform。
- Missing、Wrong Scope、Overpacked 与 V4 event separation 没有在 README mandatory observation summary 中作为已执行 variant 报告，因此 C02/C06 local support 不能写成 full coverage。
- Case F 的 budget units 是人工整数，不是 OpenAI/Anthropic token count、billing 或真实 output limit。
- Case G 人为移除 original bytes/locator；若系统保留 immutable bytes 或 resolvable locator，byte reconstruction verdict 可不同。
- 59-file equality 不覆盖 environment/TDD/execution log，只证明同一 binary/fixture/environment 的 normalized set。
- `CS0411`、non-escalated `helper_unknown_error`、首次 command timestamp gap 与 invalid PowerShell ReadOnlySpan audit helper 均已披露并恢复；不能删除，也不能当作 behavior failure 或扩大 Claim。
- 两篇性能论文的任务/模型早于当前产品面；C03 仍只是一条有限反例支持的 universal-negative claim。
- no Provider/model/network/credentials；不证明真实模型质量、Provider internal context、生产/cross-platform/distributed behavior。

### Final BLOCKED audit

- Core Claims：`9`
- `CONFIRMED` primary claims：`3` (`13-C03`, `13-C04`, narrowed `13-C05`)
- `PROPOSAL` primary claims：`6` (`13-C01`, `13-C02`, `13-C06`, `13-C07`, `13-C08`, `13-C09`)
- `PARTIAL` primary claims：`0`（proposal 的 local support 可为 PARTIAL，不改变 primary status）
- `BLOCKED` primary claims：`0`
- Provider-internal / full-token reconstruction：`UNKNOWN / UNSUPPORTED`（non-scope，不伪装成已证 Claim）
- Evidence Gate：`PASS / EVIDENCE_READY`

## Evidence Gate Decision

- Decision：`PASS`
- Audited Core Claims：`9 / 9`
- Primary Claim Status Audit：`3 CONFIRMED / 6 PROPOSAL / 0 PARTIAL / 0 BLOCKED`
- Maximum Wording Audit：`PASS / FINAL EVIDENCE MERGE CEILINGS FROZEN`
- Evidence Consistency：`PASS / 0 UNRESOLVED`
- Next Allowed Gate：`OUTLINE`
- Blocker：`NONE`
