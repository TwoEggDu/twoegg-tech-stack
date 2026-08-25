# Article 18 Outline｜Evidence Contract：把自然语言推断变成可审计工程数据

## Outline contract

- Article Type: PRINCIPLE
- Teaching Spine: 问题空间 -> 抽象模型 -> 最小 Evidence Record -> 语义验收链 -> 生命周期与冲突 -> BuildPilot 设计落地 -> 工程边界与验证边界
- Core Claim Scope: `18-C01`—`18-C10` only；不新增核心 Claim
- Evidence Posture: `2 CONFIRMED / 2 PARTIAL / 6 PROPOSAL / 0 BLOCKED`
- Required Lab: `NONE`
- Experiment Count: `0`
- Runtime Observation: `ABSENT`
- BuildPilot: `DESIGN / NOT IMPLEMENTED / NOT RUN`

> 如果这篇只记一句话：`工程判断不是一句更确定的话，而是一条能回答“主张是谁、证据在哪、适用到哪、谁按什么规则接受”的可审计链。`

## Opening hook｜五样东西都齐了，为什么仍不能接受结论

- Reader Question: Agent 已经给出流畅结论、合法 JSON、citation URL、trace ID 和一个 confidence label，为什么 Reviewer 仍然不能把它当作工程事实？
- Claim / Evidence: `18-C01` / `18-E01`, `18-E03`, `18-E04`, `18-E08`；边界引用 `18-C10` / `18-E05`, `18-E08`。
- Teaching Role: 用一个明确标注为 `CONSTRUCTED / NOT EXECUTED` 的 BuildPilot 诊断输出开场：对象结构完整，却没有固定 subject/build、source identity、Observation、推理边界、反证、policy version 或 decision owner。
- Planned contrast:

  ```text
  “root cause is X”
  + schema-valid object
  + citation
  + trace id
  + confidence label
  != accepted engineering judgment
  ```

- Wording Boundary: 只说明这些信号单独不足以完成证据支持与 policy acceptance；不说自然语言、JSON、citation 或 Trace 必然错误。
- Section Takeaway: **看起来完整的输出仍只是 Claim candidate；接受它之前，还缺语义合同。**

## Part A｜问题空间：为什么“说得像事实”不是“事实已经成立”

### 1. 从表达质量切换到可审计性

- Reader Question: 自然语言判断反复进入报告、handoff 和自动化流程时，真正缺失的是什么？
- Claim / Evidence: `18-C01` / `18-E01`, `18-E03`, `18-E04`, `18-E08`。
- Section Responsibilities:
  - 区分 fluency、Parse、Schema validity、Evidence support 与 Acceptance decision。
  - 建立 `Claim candidate` 概念：输出可以待支持、待反驳或待标为设计选择，不能仅靠确定语气升级。
  - 用“哪一个对象、哪一版本、哪一范围、哪些反证、哪版 policy、谁决定”六问，把问题从文风拉回工程数据。
- Non-scope: 不重讲 Article 03 的 parser/schema/DTO/Domain 机制；不提前设计 Article 22 的质量评估。
- Section Takeaway: **结构正确让对象可处理；Evidence Contract 才开始判断它凭什么可被当前 scope 接受。**

### 2. 常见失败不是“没有日志”，而是语义被压平

- Reader Question: 为什么只保存一句结论、一个链接或一条 Trace，会让后续复核失去关键分层？
- Claim / Evidence: `18-C01`, `18-C05`, `18-C10` / `18-E01`, `18-E02`, `18-E04`, `18-E05`, `18-E08`。
- Section Responsibilities:
  - 展示四种压平：Observation 与 Inference 混写；locator 与 Provenance 混写；confidence 与 approval 混写；Trace presence 与 Claim acceptance 混写。
  - 把上游文章的共同边界收束为：`进入 Context / 被保存 / 被检索 / 被 Skill 加载 / 被 Tool Runtime 记录` 均不自动成为 accepted fact。
- Wording Ceiling for `18-C05`: Provenance 与 Acceptance 的分离有来源支撑；本文采用的 qualitative Confidence scheme 仍是课程提案、未校准，不是概率或批准。
- Section Takeaway: **审计失败常常不是数据不存在，而是不同责任被压成一个“已确认”。**

## Part B｜抽象模型：先把六个对象和四个审计问题分开

### 3. Claim、Evidence、Observation、Inference、Proposal、Unknown

- Reader Question: 一条判断在证据链中究竟扮演什么角色？
- Claim / Evidence: `18-C02` / `18-E02`, `18-E08`。
- Proposal Language: 本节始终使用“本课程把……定义为 / 本文采用……”；不称行业统一 ontology。
- Teaching Order:
  1. `Claim`：等待支持、反驳或明确标为设计选择的可审计命题。
  2. `Evidence`：在明确 scope 下支持或反驳 Claim 的可定位来源、制品、Observation、实验记录或设计记录。
  3. `Observation`：直接记录到的有界事实，不夹带因果解释。
  4. `Inference`：从 Evidence 推出的解释，公开输入、规则、替代解释与 falsifier。
  5. `Proposal`：尚未实现或运行的设计选择，可以接受为设计，不能写成运行事实。
  6. `Unknown`：关键输入缺失、冲突未解或 scope 不足时的诚实终态，不是低 confidence 的别名。
- Bridge: 说明“陈述姿态”与 `CONFIRMED / PARTIAL / BLOCKED / PROPOSAL` Evidence Status 正交，避免把 Proposal 字段设计误写成 Observation。
- Section Takeaway: **先给陈述分角色，才有可能阻止 Observation、Inference 与 Proposal 在一段自然语言里互相冒充。**

### 4. Citation、Provenance、Confidence、Acceptance 是四个不同问题

- Reader Question: 一个链接、一条来源链、一个 confidence label 和一次 review decision 分别回答什么？
- Claim / Evidence: `18-C05` / `18-E02`, `18-E04`, `18-E08`。
- Planned audit table:

  | Concern | 本节只回答 | 明确不自动证明 |
  |---|---|---|
  | Citation | 去哪里查看材料 | 内容真实、适用或完整支持 Claim |
  | Provenance | 材料怎样产生、派生、修订 | 材料可信或 Claim 已接受 |
  | Confidence | 在已声明且可能未校准的 scheme 下有多确定 | 概率、真值或批准 |
  | Acceptance | 哪版 policy 对固定 Claim/subject 与 Evidence inputs 作了什么决定 | 永真、production approval 或未来版本继续成立 |

- Mandatory Wording Ceiling: 明确写 `18-C05 = PARTIAL`；不宣称任何标准统一规定本文四分法或 confidence scale。
- Section Takeaway: **能找到来源、能追溯来源、有多确定、是否允许使用，是四次不同的审查。**

## Part C｜最小记录：把一句结论拆成七组可复核字段

### 5. Minimum Evidence Record（COURSE PROPOSAL）

- Reader Question: 如果不实现完整 Evidence Store，一条最小记录至少要留下什么？
- Claim / Evidence: `18-C03` / `18-E02`, `18-E03`, `18-E04`, `18-E07`。
- Proposal Language: 字段组是可序列化的课程设计，不是本文交付的 JSON Schema，也不是任何单一标准强制的格式。
- Seven field groups:
  1. Record identity: `record_id`, `schema_version`, `record_revision`。
  2. Claim identity: `claim_id`, `statement`, `claim_kind`, `scope`。
  3. Evidence references: `evidence_refs`, `counter_evidence_refs`，每项带 source identity、version/time、locator；内容可固定时带 digest。
  4. Interpretation boundary: `observation`, optional `inference_rule`, `limitations`, `does_not_prove`, `falsifier`。
  5. Evidence status: `CONFIRMED | PARTIAL | BLOCKED | PROPOSAL` + rationale。
  6. Acceptance: `acceptance_policy_id/version`, `decision`, `reviewer/verifier`, `decided_at`；confidence 若出现，必须带 scheme 与 rationale。
  7. Lifecycle: `created_at`, `supersedes`, `invalidated_at/reason`, `review_history_refs`。
- Planned schema-shaped block: 只展示字段分组和 `UNKNOWN / NONE / NOT_APPLICABLE` 的诚实缺省；不伪造真实 ID、时间、digest 或 reviewer。
- Section Takeaway: **最小记录的目标不是字段多，而是让对象、支持、解释、决定和演化能够分别被复核。**

### 6. 为什么 identity、version/time、scope、limitations、falsifier 不能被一句“有来源”代替

- Reader Question: 这五类边界分别阻止什么越界？
- Claim / Evidence: `18-C04` / `18-E02`, `18-E03`, `18-E04`, `18-E07`。
- Planned five-error mapping:
  - missing identity -> 证错对象；
  - missing version/time -> 套错版本或历史状态；
  - missing scope -> 局部 Observation 被外推；
  - missing limitations -> 已知缺口被隐藏；
  - missing falsifier -> 解释变成不可反驳。
- Mandatory Wording Ceiling: primary standards directly motivate subject/source identity、version/time、scope 与 policy context；`limitations` 和 `falsifier` 是本课程 fail-closed 扩展。不得写成标准统一强制这五个精确字段或名称。
- Section Takeaway: **“有来源”仍太粗；工程接受需要知道它支持的是哪个对象、哪个时间和多窄的结论。**

## Part D｜语义验收：Parse / Schema 以后还要过哪些 Gate

### 7. Semantic Acceptance Chain（COURSE PROPOSAL）

- Reader Question: 对象已经能 Parse 且满足 Schema，系统还应按什么顺序拒绝不够格的 Claim？
- Claim / Evidence: `18-C07` / `18-E01`, `18-E03`, `18-E04`, `18-E08`。
- Proposed chain:

  ```text
  Parse
    -> Schema
    -> Claim / Subject Identity
    -> Source Integrity / Resolution
    -> Provenance
    -> Version / Time / Scope Applicability
    -> Support / Refute Mapping
    -> Counter-evidence / Alternatives
    -> Limitations / Falsifier
    -> Confidence Scheme
    -> Acceptance Policy Decision
  ```

- Teaching treatment:
  - Gate 0 `Parse / Schema` 明确归 Article 03；它只确认可读与结构约束。
  - 后续 Gate 逐层回答 identity、source、applicability、support、alternatives、boundary 与 decision。
  - 核心输入缺失时 fail closed：收窄为 `PARTIAL`、保留 `BLOCKED / Unknown` 或取消 accepted decision；不允许用后续高 confidence 覆盖前置缺口。
- Proposal Boundary: 顺序、Gate 命名和 fail-closed disposition 是课程设计，不宣称标准化 pipeline 或已实现 Runtime。
- Section Takeaway: **Schema 是语义验收的入口，不是终点；前置证据缺口不能被后置措辞强度冲掉。**

### 8. Acceptance 不是永真：decision 必须绑定 policy、inputs、reviewer 与时间

- Reader Question: 为什么同一 Claim 在 Evidence 或 policy 改变后必须重新 review？
- Claim / Evidence: `18-C05`, `18-C07` / `18-E04`, `18-E08`。
- Section Responsibilities:
  - decision 与 Claim/subject、Evidence IDs、policy version、reviewer/verifier、decided_at 绑定。
  - confidence 只作为可选、未校准的 review label；它不能替代 policy decision。
  - accepted 只表示当前 scope 可用，不等于未来版本、其他环境或 production approval。
- Section Takeaway: **Acceptance 是一条带输入和版本的 decision record，不是给 Claim 盖永久真值章。**

## Part E｜生命周期与冲突：不靠覆盖制造“当前真相”

### 9. Append、Supersede、Invalidate、Review（COURSE PROPOSAL）

- Reader Question: 新证据到来、旧记录过期或复核结论变化时，怎样保留审计历史？
- Claim / Evidence: `18-C08` / `18-E02`, `18-E04`, `18-E07`。
- Proposed lifecycle:

  ```text
  APPEND -> REVIEW -> ACCEPT | REJECT | NEEDS_REVIEW
      |         |-> review event binds policy/version + Evidence IDs + reviewer + time
      |-> SUPERSEDE points to prior revision
      |-> INVALIDATE records target + reason + actor + time
  current view = projection of retained events
  ```

- Required distinction: 原 Observation 不原地改写；replacement、invalidation 与 review 产生新 revision/event，历史记录仍可寻址。
- Proposal Boundary: W3C PROV 与审计控制只提供 revision/invalidation/history 先例，不规定本文完整状态机；没有实现 Evidence Store 或 append-only infrastructure。
- Section Takeaway: **新结论应追加关系和决定，不应通过改写旧 Observation 伪造一条从未发生过的历史。**

### 10. Conflict、Stale、Partial 的 fail-closed 处理（COURSE PROPOSAL）

- Reader Question: 两条证据互相冲突、证据过期或只覆盖 Claim 一部分时，系统怎样停止过度外推？
- Claim / Evidence: `18-C06` / `18-E02`, `18-E04`, `18-E08`。
- Proposed policy:
  - Conflict: 双方并存并互相引用；只按显式 scope/version/authority rule 处理，未解则 `BLOCKED / Unknown`。
  - Stale: 保留历史 identity，追加 superseded/invalidated relation、reason、time、actor；不默认 newest-wins。
  - Partial: 收窄 statement/scope 并保持 `PARTIAL`，不把局部支持升级为完整因果结论。
  - New Evidence: 生成新 revision 并触发 review；只有重跑指定 acceptance policy，decision 才改变。
- Proposal Boundary: 这是课程冲突 policy；不宣称组织级统一规则或已实现并发存储语义。
- Section Takeaway: **解决不了的冲突应成为正式 Unknown，而不是被 latest record 或更顺的叙述静默覆盖。**

## Part F｜具体设计：BuildPilot 诊断证据包，而不是一句“根因是 X”

### 11. BuildPilot diagnostic evidence package（DESIGN / NOT IMPLEMENTED / NOT RUN）

- Reader Question: 如果 BuildPilot 将来要交付一次可复核诊断，它最少应交付哪些相互关联的对象？
- Claim / Evidence: `18-C09` / `18-E08`。
- Global label for the entire section: `COURSE DESIGN / SYNTHETIC SHAPE / NOT IMPLEMENTED / NOT RUN`。
- Package responsibilities:
  1. `case_id`、诊断目标与精确 subject/build scope；
  2. Claim set 与各自 statement/scope；
  3. source manifest：日志、配置、制品、版本、时间窗、digest/locator；
  4. raw Observations 与不可用 inputs；
  5. Observation -> candidate cause 的 inference graph；
  6. alternatives、counter-evidence、limitations、falsifiers、Unknowns；
  7. 每个 Claim 的 Evidence status；
  8. acceptance policy/version、decision、review history refs；
  9. lifecycle/revision relations。
- Concrete inline design example:

  ```yaml
  case_id: BP-DIAG-DESIGN-001
  classification: DESIGN_NOT_IMPLEMENTED_NOT_RUN
  diagnostic_target: "a Unity/Jenkins build whose exact identity is still required"
  claims:
    - {claim_id: BP-C01, statement: "root cause remains UNKNOWN", status: BLOCKED}
  source_manifest:
    - {source_id: BUILD_LOG, state: REQUIRED_NOT_ACQUIRED}
    - {source_id: BUILD_CONFIG, state: REQUIRED_NOT_ACQUIRED}
    - {source_id: ARTIFACT_INVENTORY, state: REQUIRED_NOT_ACQUIRED}
  observations: []
  inference_graph: []
  alternatives: []
  unknowns: [EXACT_BUILD_IDENTITY, FIRST_FAILING_STAGE, ROOT_CAUSE]
  acceptance:
    policy: buildpilot-evidence-course-v1-design
    decision: NOT_RUN
  ```

- Teaching purpose: 一个具体的“缺口也能被结构化保存”的包，比伪造 Observation 的完整样例更符合本篇证据边界；只有未来取得真实 inputs 后，才允许追加 Observation、Inference 与 review event。
- Mandatory disclaimer: 本文没有生成运行时诊断包，没有 BuildPilot Trace、实验、样本、accuracy、cost、latency、production 或 benefit evidence；`experiment_count=0`，Observed Evidence=`ABSENT`。
- Section Takeaway: **可审计诊断包允许结果停在 Unknown；一句“根因是 X”会把来源、推理、反证和验收全部丢掉。**

### 12. 用同一 package 做一次 review walk-through

- Reader Question: Reviewer 应从哪里开始，何时必须停止？
- Claim / Evidence: `18-C03`, `18-C06`, `18-C07`, `18-C08`, `18-C09` / `18-E02`, `18-E03`, `18-E04`, `18-E07`, `18-E08`。
- Review path:
  1. fixed case/Claim identity 是否存在；
  2. source 是否可解析、版本/scope 是否覆盖；
  3. Observation 是否与 Inference 分离；
  4. alternatives/counter-evidence/limitations/falsifier 是否可见；
  5. policy/version/reviewer 是否对固定 inputs 作出 decision；
  6. 任一前置缺口命中时，保持 `BLOCKED / Unknown / NOT_RUN`。
- Boundary: 这是对设计包的审查演示，不是 BuildPilot execution、Trace replay 或 Eval run。
- Section Takeaway: **审查的价值在于知道何时不能接受，而不是把每个 package 都推到 PASS。**

## Part G｜工程边界：Article 18 不吞掉相邻系统

### 13. Structured Output、Runtime Trace、Trace/Replay、Eval 各负责哪一层

- Reader Question: Evidence Contract 与课程已讲、将讲的结构、执行记录、故障定位和评估怎样分账？
- Claim / Evidence: `18-C10` / `18-E01`, `18-E05`, `18-E06`, `18-E08`。
- Boundary matrix:

  | Boundary | Owner | Article 18 只承接什么 | Article 18 不做什么 |
  |---|---|---|---|
  | Parse / Schema / machine contract | Article 03 Structured Output | 把 schema-valid record 作为语义 Gate 入口 | 不重讲 parser、DTO、Domain Validation；不把 schema validity 当 truth |
  | Tool Result / terminal execution record / Trace foundation | Article 06 Tool Runtime | 把 Result/Trace ref 当候选 Evidence source | 不把 Result 或 Trace presence 当 Claim acceptance；不重讲 Runtime pipeline |
  | Cross-step Trace、Replay、Failure Taxonomy | Article 21 | 只要求未来 trace 可被 Evidence Record 引用 | 不设计完整 correlation、reconstruction/re-execution 或 failure classification |
  | Eval、Golden Dataset、Regression | Article 22 | 只保留“单个 Claim acceptance 不等于系统质量” | 不设计 dataset、grader、metrics、regression 或 Lab 06 |

- Additional bridge: Article 19 才展开 Permission / Approval / Sandbox；Evidence acceptance 不授予 action authority。Article 20 才展开 Budget；本篇不输出成本或延迟结果。
- Wording Boundary: `18-C10` 只确认当前课程 ownership，不宣称行业统一命名。
- Section Takeaway: **Evidence Contract 决定一个 scoped Claim 凭什么被接受；它既不是执行记录本身，也不是完整可重放 Trace 或系统级 Eval。**

### 14. 坏实现通常怎样坏

- Reader Question: 一份看似“证据化”的实现，最常在哪些责任边界上退化？
- Claim / Evidence: 不新增 Claim；仅作为 `18-C01`—`18-C10` 的应用检查。
- Planned anti-pattern table:
  - schema-valid -> fact true；
  - citation present -> source supports Claim；
  - provenance present -> source trusted；
  - confidence high -> accepted；
  - latest Evidence -> overwrite conflict；
  - trace exists -> root cause proven；
  - accepted once -> forever valid；
  - design package -> BuildPilot implemented。
- Minimum guards: 对应回到 subject/source/scope/support/alternative/boundary/policy/lifecycle Gate。
- Section Takeaway: **Evidence 工程不是给结论加 metadata，而是阻止 metadata 被误当成真值捷径。**

## Part H｜验证边界、学习检查与职业能力

### 15. 本篇能证明什么，不能证明什么

- Reader Question: 没有 Lab、实验或 Runtime observation 时，这篇原理文的可信上限在哪里？
- Claim / Evidence: `18-C01`—`18-C10` / `18-E01`—`18-E08`，严格按 Claim Register 各自 ceiling 使用。
- Can establish:
  - fixed sources 支持结构验证、subject identity、provenance relation 与 policy-bound decision 的分层；
  - canonical 支持 Article 03 / 06 / 18 / 21 / 22 的课程 ownership；
  - 课程可以提出可审查的 Record、Gate、lifecycle 与 BuildPilot package design。
- Must remain absent:
  - BuildPilot implementation、runtime package、Trace、accuracy、cost、latency、benefit 或 production evidence；
  - 通用 Evidence ontology、统一 confidence scale、标准强制的完整字段/状态机；
  - Article 21 Replay/Failure Taxonomy 与 Article 22 Eval/Regression 的详细机制。
- Frozen reality: Required Lab=`NONE`；Experiment Count=`0`；Runtime Observation=`ABSENT`；BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`。
- Section Takeaway: **设计可审查，不等于设计已实现；来源可复核，不等于运行结果已经存在。**

### 16. Learning Check

1. 一份对象通过 Parse 与 Schema 后，为什么仍不能宣布其 Claim 成立？下一步至少要检查哪几类语义输入？
2. `Observation` 与 `Inference` 的最小差别是什么？一个 `Unknown` 为什么不是低 confidence 的别名？
3. Citation、Provenance、Confidence、Acceptance 各回答什么，哪两个最容易被误写成批准？
4. 为什么 identity、version/time、scope 可以得到直接标准先例，而 limitations / falsifier 必须保留课程扩展语态？
5. 两条 in-scope Evidence 冲突且 authority 无法裁决时，为什么不能 newest-wins？
6. 新 Evidence 到来后，为什么不能直接改写旧 Observation 或自动翻转 decision？
7. BuildPilot package 的 sources / observations 仍为空时，`BLOCKED / Unknown / NOT_RUN` 为什么比一个 root-cause sentence 更正确？
8. Article 06 的 terminal Trace、Article 21 的 Replay/Failure Taxonomy 与 Article 22 的 Eval 分别为什么不能被 Article 18 吞掉？

### 17. Job Competency mapping

| Competency | Reader-visible output | Observable standard | Explicit ceiling |
|---|---|---|---|
| Evidence modeling | Minimum Evidence Record field groups | 能把 identity、support、interpretation、acceptance、lifecycle 分账 | schema proposal 不等于 implementation |
| Diagnostic reasoning | Observation -> Inference -> alternatives -> falsifier -> Unknown | 能拒绝把相关性、Trace 或流畅措辞写成因果事实 | 没有真实 BuildPilot diagnosis |
| Reliability design | semantic Gate + fail-closed disposition | 能说明缺哪项输入时收窄、BLOCK 或停止 | Gate order 是课程 proposal |
| Audit / governance | policy-bound decision + append/supersede/invalidate/review | 能保留 decision inputs、owner、version 与历史 | 不宣称 compliance 或 production store |
| Cross-system architecture | Article 03 / 06 / 21 / 22 boundary matrix | 能把 machine contract、runtime record、trace/replay 与 eval 分层 | 课程 ownership 不等于行业 taxonomy |

### 18. Closing bridge

- Closing sentence: `工程判断真正可交接的形态，不是更确定的结论，而是可定位、可反驳、可复核、可重新验收的 Claim/Evidence/Decision 链。`
- Next bridge: 下一篇 Article 19 将追问“即使判断有证据，谁可以批准哪种行动、Runtime 又怎样执行隔离”；本篇 acceptance decision 不替代 permission or approval。

## Claim-to-section coverage（10 / 10）

| Claim | Status ceiling | Primary sections | Evidence IDs | Mandatory wording / boundary |
|---|---|---|---|---|
| `18-C01` | CONFIRMED | Opening, 1, 2, 14, 15 | `18-E01`, `18-E03`, `18-E04`, `18-E08` | fluency/parse/schema alone insufficient；不说自然语言必错 |
| `18-C02` | PROPOSAL | 3 | `18-E02`, `18-E08` | 六术语是课程模型，不称统一 ontology |
| `18-C03` | PROPOSAL | 5, 12 | `18-E02`, `18-E03`, `18-E04`, `18-E07` | 七组字段是课程设计，不称标准 schema / 已实现 |
| `18-C04` | PARTIAL | 6 | `18-E02`, `18-E03`, `18-E04`, `18-E07` | identity/version/time/scope 有直接先例；limitations/falsifier 为课程扩展 |
| `18-C05` | PARTIAL | 2, 4, 8 | `18-E02`, `18-E04`, `18-E08` | confidence scheme 未校准、非概率/批准；四分法不称标准 |
| `18-C06` | PROPOSAL | 10, 12 | `18-E02`, `18-E04`, `18-E08` | 冲突/过期/部分覆盖 policy 是设计，未实现 Evidence Store |
| `18-C07` | PROPOSAL | 7, 8, 12 | `18-E01`, `18-E03`, `18-E04`, `18-E08` | semantic Gate 顺序与 fail-closed 行为是课程设计 |
| `18-C08` | PROPOSAL | 9, 12 | `18-E02`, `18-E04`, `18-E07` | append-only revision/current projection 是设计，不称标准状态机 |
| `18-C09` | PROPOSAL | 11, 12 | `18-E08` | BuildPilot=`DESIGN / NOT IMPLEMENTED / NOT RUN`；无效果/Runtime Claim |
| `18-C10` | CONFIRMED | Opening, 13, 15 | `18-E01`, `18-E05`, `18-E06`, `18-E08` | 仅确认课程 ownership，不提前讲完 21/22 |

Coverage: `10 / 10`；new core Claim: `NONE`；core `BLOCKED`: `0`。

## Source and visual plan

### Source plan

- Draft 不新增来源或核心事实；逐节只消费 `18-E01`—`18-E08` 已验证的 `Proves / Does Not Prove / Limitations`。
- Fixed-source anchors: JSON Schema 2020-12 (`18-E01`)、W3C PROV-DM (`18-E02`)、in-toto v1.0 (`18-E03`)、SLSA v1.2 (`18-E04`)、NIST audit precedent (`18-E07`)。
- Moving/high-level anchors 只用于窄边界：OpenTelemetry Trace API (`18-E05`) 与 NIST AI RMF (`18-E06`)；不得由此补写 Article 21/22 机制。
- Course ownership 和 BuildPilot posture 只取 `18-E08`；不把 repository 设计写成行业事实。

### Visual plan

1. **One compact flow diagram**: `Claim candidate -> Record -> Semantic Gates -> policy-bound decision`，标出 Article 03 在 Gate 0、Article 06 Trace 作为 Evidence input、Article 21/22 作为后续边界。职责是展示三层关系，不增加事实。
2. **One lifecycle diagram**: `APPEND -> REVIEW -> ACCEPT/REJECT/NEEDS_REVIEW` 加 `SUPERSEDE / INVALIDATE / CONFLICT` 旁路。明确标注 `COURSE PROPOSAL`。
3. BuildPilot 使用表格 + inline YAML 设计片段即可；不创建图片或 asset，避免把设计示意包装成运行截图。

## Outline Gate self-check

- [x] Problem space -> abstract model -> concrete design -> engineering / verification boundary 完整。
- [x] `18-C01`—`18-C10` coverage = `10 / 10`；无新核心 Claim。
- [x] `18-C04 / C05` 保留 PARTIAL wording ceilings。
- [x] `18-C02 / C03 / C06 / C07 / C08 / C09` 全部保留 Proposal language。
- [x] Minimum Evidence Record、semantic acceptance chain、lifecycle/conflict policy 与 BuildPilot package 已分段落位。
- [x] Article 03、Article 06 runtime trace foundations、Article 21、Article 22 边界显式。
- [x] Required Lab `NONE`；Experiment Count `0`；Runtime Observation `ABSENT`。
- [x] BuildPilot `DESIGN / NOT IMPLEMENTED / NOT RUN`；无 accuracy / cost / latency / benefit / production Claim。
- [x] Learning Check、Job Competency、source plan 与最小 visual plan 已包含。
