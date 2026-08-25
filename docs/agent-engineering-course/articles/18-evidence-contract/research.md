# Article 18 Research｜Evidence Contract

## Research metadata

- Status: COMPLETE
- Evidence Gate Recommendation: PASS_RECOMMENDED / MASTER_VALIDATION_PENDING
- Required Lab: NONE
- Source access date: 2026-08-25 (Asia/Shanghai)
- Experiment count: 0
- Runtime observation: ABSENT
- BuildPilot posture: DESIGN / NOT IMPLEMENTED / NOT RUN
- Research boundary: 原理、记录模型与验收语义；不实现 Evidence Store，不运行 BuildPilot，不提前教授 Article 19、21、22。

## Research thesis and evidence posture

自然语言结论、schema-valid 对象、引用链接、执行 Trace 与评估结果是不同层次的数据。本文采用的最小判定是：一个 Claim 只有在“支持对象是谁、证据从哪里来、适用于什么版本与范围、存在什么限制或反证、由哪条验收策略作出什么决定”都可复核时，才可被接受为**当前作用域内的工程判断**。接受不是永真，也不把 Proposal 伪装成 Observation。

本研究严格使用四种陈述姿态：

- **Observation**：从指定来源、记录或未来实验直接读到的有界事实；不得夹带因果解释。
- **Inference**：从一项或多项 Observation 推出的解释；必须公开推理规则、替代解释和可证伪条件。
- **Proposal**：本课程选择的字段、状态机或验收策略；不声称已成为行业统一标准或已经实现。
- **Unknown**：现有输入无法支持答案；必须保留缺口，而不是用置信措辞覆盖它。

`CONFIRMED / PARTIAL / BLOCKED / PROPOSAL` 是 Claim 的证据状态；上面的四类是陈述来源/推理姿态。二者正交。例如，标准中的直接 Observation 可以支持一个 `CONFIRMED` Claim，课程字段集合则保持 `PROPOSAL`。

## Ten approved questions

### 1. 为什么自然语言判断不能直接成为工程事实？（18-C01）

自然语言的流畅度只反映表达；JSON Schema 验证只约束实例结构；in-toto Statement 只把 predicate 绑定到特定 subject；SLSA 又把 subject digest 校验、predicate 类型、信任根、期望和策略验收分开。因而，“句子写得确定”或“对象通过 schema”均没有单独完成来源、适用性、反证和接受策略的语义验收。本文把自然语言输出先当作 **Claim candidate**，而不是自动升级为事实。该结论不表示自然语言一定错误，只表示它本身不足以通过本课程的 Evidence Gate。

### 2. 六个术语如何区分？（18-C02）

- **Claim**：等待支持、反驳或明确标为设计选择的可审计命题。
- **Evidence**：可标识、可定位，并在明确 scope 下支持或反驳 Claim 的来源、制品、Observation、实验记录或设计记录。
- **Observation**：直接记录到的有界事实；只陈述“读到/测到/返回了什么”。
- **Inference**：由 Evidence 推导出的解释，记录推理规则、输入 Claim/Evidence 和替代解释。
- **Proposal**：尚未实现或尚未运行的设计选择；可以接受为课程设计，但不能当作运行事实。
- **Unknown**：关键输入缺失、冲突未解或 scope 不足时的诚实终态；它不是低置信度的同义词。

这是 Article 18 的课程术语模型，不宣称为 W3C、SLSA 或所有组织的统一 ontology。

### 3. 最小可审计 Evidence Record 包含什么？（18-C03）

课程提出七组最小字段：

1. **记录身份**：`record_id`、`schema_version`、`record_revision`。
2. **Claim 身份**：`claim_id`、`statement`、`claim_kind`、`scope`。
3. **证据引用**：`evidence_refs`、`counter_evidence_refs`；每项包含 source identity、版本/时间、locator，能固定内容时包含 digest。
4. **解释边界**：`observation`、可选 `inference_rule`、`limitations`、`does_not_prove`、`falsifier`。
5. **证据状态**：`CONFIRMED | PARTIAL | BLOCKED | PROPOSAL` 与理由。
6. **验收信息**：`acceptance_policy_id/version`、`decision`、`reviewer/verifier`、`decided_at`；可选记录未校准的 qualitative confidence scheme 与 rationale。
7. **生命周期**：`created_at`、`supersedes`、`invalidated_at/reason`、`review_history_refs`。

这是可序列化的语义记录提案，不是本文交付的 JSON Schema，也不表示字段存在即可自动通过验收。

### 4. identity、version/time、scope、limitations、falsifier 为什么不可省略？（18-C04）

它们分别阻止五类错误：证错对象、套错版本、越界外推、隐藏已知缺口、以及形成不可反驳的解释。W3C PROV 区分 entity/activity/agent、generation、revision 和 invalidation；in-toto/SLSA 使用 subject digest 和 predicate type 指定被证明对象；SLSA VSA 还记录 verifier、policy、`timeVerified` 和 decision。标准直接支持“对象身份、来源、版本/时间和验收上下文需要显式化”，但并不统一要求本文的 `limitations` 与 `falsifier` 字段，因此本 Claim 只记为 `PARTIAL`，后两者属于课程的 fail-closed 扩展。

### 5. Citation、Provenance、Confidence、Acceptance 各解决什么？（18-C05）

- **Citation** 回答“去哪里查看支持材料”，是 locator，不保证内容真实或适用。
- **Provenance** 回答“材料由哪些 entity/activity/agent 产生或派生”，支持来源与责任链复核，不自动给出可信结论。
- **Confidence** 回答“评估者在一套已声明、可能未校准的尺度上有多确定”，必须附 scheme 与 rationale；它不是概率，也不是批准。
- **Acceptance** 回答“哪一版 policy 针对哪个 Claim/subject 作出了什么使用决定”，需要 verifier/reviewer、时间和输入 Evidence 集。

W3C PROV 明确指出 provenance 可作为质量、可靠性或可信度评估的输入，但 provenance 自身不完成信任判断；SLSA VSA 将 policy、verifier 和 `verificationResult` 显式记录。Confidence 的具体尺度仍是课程提案，所以整体状态为 `PARTIAL`。

### 6. 冲突、过期和部分覆盖如何处理？（18-C06）

采用 fail-closed 的 `PROPOSAL`：

- 冲突 Evidence 并存并互相引用，不静默覆盖，也不默认“最新即正确”；无法用 scope/version/authority 解开时，Claim 进入 `BLOCKED` 或 `Unknown`。
- 过期 Evidence 保留历史身份，追加 `superseded` 或 `invalidated` 关系、原因、时间和责任者；不得改写原 Observation。
- 只覆盖部分 Claim 时收窄 statement/scope 并记 `PARTIAL`，不能把局部证据外推成完整因果结论。
- 新 Evidence 只触发新 revision 与复核；只有重新运行指定 acceptance policy 后，当前 decision 才改变。

### 7. Parse / Schema Validate 后还需要哪些 Gate？（18-C07）

课程提出一条语义验收链：

`Parse -> Schema -> Claim/Subject Identity -> Source Integrity/Resolution -> Provenance -> Version/Time/Scope Applicability -> Support/Refute Mapping -> Counter-evidence/Alternatives -> Limitations/Falsifier -> Confidence Scheme -> Acceptance Policy Decision`

任一步缺少核心输入都不得借后续高置信措辞放行。`Parse` 与 `Schema` 只保证可读和结构约束；它们对应 Article 03 的 Structured Output，不替代 Article 18 的语义 Gate。

### 8. 追加、替换、失效、复核和审计历史如何表达？（18-C08）

采用 append-only revision 的 `PROPOSAL`：原始 Evidence/Observation 不原地改写；追加生成新记录；替换生成新 revision 并以 `supersedes` 指向旧记录；失效生成带 reason/actor/time 的 invalidation event；复核生成 review event，固定 policy version、输入 Evidence IDs、decision 与 reviewer。当前视图是这些不可变事件的投影。W3C PROV 的 revision、invalidation 与 bundle-of-provenance，以及 SLSA VSA 的 policy-bound verification summary，为关系设计提供了先例，但没有规定本课程的完整状态机。

### 9. BuildPilot 的诊断证据包应该是什么？（18-C09）

仅作为 DESIGN，BuildPilot 的一次诊断包应包含：

- 诊断目标、Claim 集与各自 scope；
- source manifest（日志、配置、制品、版本、时间窗、digest/locator）；
- 原始 Observation 与不可用输入；
- 从 Observation 到候选原因的 inference graph；
- 替代解释、counter-evidence、limitations、falsifier 与 Unknown；
- 每个 Claim 的 Evidence status、acceptance policy/version、decision 和 review history refs。

一句“根因是 X”不能替代该包。此处没有 BuildPilot 实现、执行、Trace、样本、准确率、成本、延迟或收益证据；`experiment_count=0`，Observed Evidence `ABSENT`。

### 10. 与相邻课程主题的边界是什么？（18-C10）

| 主题 | 本主题回答的问题 | Article 18 不接管的内容 |
|---|---|---|
| Structured Output（03） | 输出能否被解析并满足结构约束 | 不判定 Claim 是否有证据、是否被 policy 接受 |
| Trace / Replay（06 的运行基础；21 的完整主题） | 执行发生了什么、如何关联或重放 | Trace 是候选 Evidence 来源，不等于 Claim 已成立；本文不提前设计完整 Replay |
| Failure Taxonomy（21） | 失败属于哪一层、如何分类 | Evidence Contract 记录支持/反驳/Unknown，不完成未来的完整分类法 |
| Eval（22） | 在数据集、workload、grader 与 metrics 上系统表现怎样 | 单个 Claim 的 Evidence 接受不等于总体质量或回归评估 |

Article 12—17 的 Context/State/Memory/KB/RAG/Skill 解决“信息如何进入执行”；Article 18 只建立“工程判断凭什么被当前 policy 接受”。

## Source register

| Source ID | Identity and version | Stable locator | Claims | Drift / applicability |
|---|---|---|---|---|
| S01 | [JSON Schema Core, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-core) and [Validation, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation) | Validation §3, validating instance data | 18-C01, C07, C10 | Fixed draft; defines structural/data validation, not evidence truth |
| S02 | [W3C PROV-DM Recommendation, 30 Apr 2013](https://www.w3.org/TR/2013/REC-prov-dm-20130430/) | §§2–3; §§5.1.8, 5.2.2, 5.2.4, 5.4.1 | 18-C02–C06, C08 | Fixed Recommendation; domain-agnostic provenance, not a universal Evidence Contract |
| S03 | [in-toto Attestation Framework v1.0 Statement](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/statement.md) and [Resource Descriptor](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md) | tag `v1.0`; Statement layer; Resource Descriptor Schema/Fields/Parsing rules | 18-C01, C03, C04, C07 | Pinned specification; supply-chain attestation scope |
| S04 | [SLSA v1.2 Provenance](https://slsa.dev/spec/v1.2/provenance), [Verifying Artifacts](https://slsa.dev/spec/v1.2/verifying-artifacts), [Verification Summary Attestation](https://slsa.dev/spec/v1.2/verification_summary) | v1.2; subject verification, expectations; VSA schema and verificationResult | 18-C01, C03–C08 | Fixed version; supply-chain verification analogy, not generic agent truth |
| S05 | [OpenTelemetry Specification 1.60.0](https://opentelemetry.io/docs/specs/otel/) and [Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/) | Trace API: Span, SpanContext, Links, Events, Status | 18-C10 | Moving specification snapshot accessed 2026-08-25; execution telemetry only |
| S06 | [NIST AI RMF 1.0](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10) | NIST AI 100-1, §5.3 MEASURE; TEVV and documented/repeatable measurement references | 18-C10 | Fixed v1.0; risk-management/evaluation framing, not Article 22 design |
| S07 | [NIST SP 800-53 Rev. 5.1 OSCAL-derived PDF](https://csrc.nist.gov/CSRC/media/Projects/risk-management/800-53%20Downloads/800-53r5/SP_800-53_v5_1-derived-OSCAL.pdf) | AU-3, AU-8, AU-9, AU-11 | 18-C03, C04, C08 | Audit-control precedent only; does not prescribe this course schema |
| S08 | TechStackShow course corpus at baseline `272ff0e24450ead78ff959dd019da202593a518d` plus current Article 18 card | Published Articles 03, 06, 12–17; glossary; series plan; Article 18 card | 18-C01–C10 | Repository/course authority; Article 18 card is active uncommitted workspace input |

No provider-specific product documentation was needed: the ten questions can be bounded with fixed standards and the repository's canonical course contract. This avoids turning a vendor behavior into a general principle.

## Counter-evidence and alternative-model search

- Checked whether JSON Schema validation defines factual truth or source trust: it defines instance/schema validation, so it cannot collapse Structured Output into Evidence acceptance.
- Checked whether provenance standards themselves decide trust: W3C PROV treats provenance as information usable in assessment and remains domain-agnostic; it does not supply a universal acceptance policy.
- Checked whether a signed/pinned attestation eliminates semantic review: in-toto binds a predicate to a subject; SLSA separately requires digest matching, trusted roots, expectations and policy verification.
- Checked whether trace fields can stand in for evidence semantics: OpenTelemetry defines execution spans, context, links, events and status, but not Claim support, falsifier or policy acceptance.
- Checked whether audit controls mandate this exact record: NIST AU controls motivate content, time, protection and retention, but do not define a universal agent Evidence schema.
- Checked whether one current standard fixes the meanings of Claim/Evidence/Confidence/Acceptance for all agent systems: none of the selected primary sources does. The precise Article 18 vocabulary and field/state design therefore remain explicit course proposals.

## Claim and evidence statistics

- Approved questions answered: 10 / 10
- Core Claims: 10
- `CONFIRMED`: 2 (`18-C01`, `18-C10`)
- `PARTIAL`: 2 (`18-C04`, `18-C05`)
- `PROPOSAL`: 6 (`18-C02`, `18-C03`, `18-C06`, `18-C07`, `18-C08`, `18-C09`)
- `BLOCKED`: 0
- Evidence Cards: 8
- Primary official/pinned external sources: 7 source groups
- Repository/course evidence groups: 1
- Runtime observations: 0
- Experiments: 0
- Core Claim coverage: 10 / 10; zero core `BLOCKED`

## Evidence Gate recommendation

**PASS_RECOMMENDED / MASTER_VALIDATION_PENDING.** All ten approved questions have stable Claim IDs and mapped Evidence Cards; no core Claim is `BLOCKED`. The two `PARTIAL` Claims are safe only with the stated wording ceilings, and all six schema/state/BuildPilot choices must remain `PROPOSAL`. Required Lab remains `NONE`; BuildPilot remains `DESIGN / NOT IMPLEMENTED / NOT RUN`; observed runtime evidence remains `ABSENT`.

Outline and Draft remain forbidden until the Master independently validates the Evidence Gate.
