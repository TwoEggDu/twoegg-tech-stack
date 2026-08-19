# Article 03 Detailed Outline｜Structured Output：让模型输出成为机器可消费的合同

- Lifecycle Input：`EVIDENCE_READY`
- Evidence Gate：`PASS`（`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`）
- Outline Gate：`PASS_RECOMMENDED`（由 Master 核对后推进）
- Article Type：`原理篇 / Lab Article`
- Course Weight：`L（Major Core Lesson）`
- Target Length：`约 6,000—8,000 中文字`
- Target Reading Time：`16—22 分钟`
- Provider Scope：`OpenAI 当前官方 contract（核对于 2026-08-20）；无 Provider call`
- Required Lab：`Lab 01｜Structured Output Validation / EXECUTED / EVIDENCE_MERGED`
- Lab Runtime Scope：`.NET SDK 10.0.301 + NJsonSchema 11.6.1 + Draft 4 frozen subset + eight synthetic fixtures + fixed allowlist`

## 1. Reader Transformation

读者从“只要让模型返回 JSON，程序就能安全使用”，转变为能够把一次候选输出拆成 Provider / transport envelope、Parse、Schema Validation、Typed Materialization、Domain Validation 与 Accept / Fail；能够指出每层成功究竟证明了什么、没有证明什么；也能够根据失败所在层决定停止、交给上游判断或补充领域输入，而不是盲目修补字符串。

完成本篇后，读者应能独立做出四个判断：

1. `合法 JSON != Schema Valid != Domain Valid != Verified Result`；
2. JSON Schema 约束的是已声明结构断言，不负责事实、权限、Tool 执行或外部状态；
3. refusal 与 incomplete / truncation 应先在 Provider envelope 处理，不能只靠 raw string 猜原因；
4. Lab 01 只证明固定本地 fixture 的首失败分层和可复现结果，不证明真实 Provider、模型或生产行为。

## 2. Teaching Spine

> 如果这篇只记一句话：`Structured Output 把模型输出变成可解析、可分层拒绝的候选数据，但不会把候选数据变成事实。`

| Teaching Phase | Reader Movement | Main Sections | Claim / Evidence |
|---|---|---|---|
| Problem Space | 从“输出看起来像 JSON”转向“程序能否知道自己接受了什么、在哪里失败” | Opening | `03-C01`、`03-C06` / `03-E01`、`03-E06` |
| Abstract Model | 建立 Provider envelope → Parse → Schema → DTO → Domain → Accept / Fail 的责任链 | Section 1—2 | `03-C02`、`03-C03` / `03-E02`、`03-E03` |
| Concrete Mechanism | 用最小 schema、DTO、allowlist 与 first-failure pipeline 落到 C# / .NET | Section 3 | `03-C02`、`03-C03` / `03-E02`、`03-E03` |
| Engineering Boundary | 区分 refusal、truncation、retry eligibility、repair、domain stop 与后续系统责任 | Section 4、6 | `03-C05`、`03-C06` / `03-E05`、`03-E06` |
| Verification Boundary | 用 Lab 01 的冻结 Expected、八类 Observed、失败记录与双运行 artifact 说明“证明到哪里” | Section 5 | `03-C04`、`03-C07` / `03-E04`、`03-E07` |

### L 级篇幅职责

- 主体围绕一条合同链展开，不写成 JSON Schema 关键字百科或 Provider SDK 教程。
- 抽象模型必须先于 `NJsonSchema`、`System.Text.Json` 或具体 API；package 只承担本地落地职责。
- Lab 01 是正文的验证骨架，不是附录装饰；Expected、Observed、Interpretation 与 Claim Status 必须分开。
- 保留 failure-first 叙事：invalid JSON、missing required、wrong type、extra property、unknown Evidence ID、synthetic truncation 与 synthetic non-contract text 都必须进入正文职责规划。
- 不实现 Provider call、model repair、完整 retry loop、Tool Runtime、Evidence truth、Eval system、Model Adapter / Gateway 或生产可靠性机制。

## 3. Opening｜Article 02 已经写清输出要求，为什么程序仍然不能直接相信结果？

- Reader Question：Prompt 中已经写了 `Status / Summary / Evidence IDs`，模型也返回了一段整齐 JSON，Application 为什么还不能直接反序列化后进入业务流程？
- Section Goal：接住 Article 02 的自然语言 `Output Requirements` stop line，把问题从“怎样要求模型呈现”推进到“程序怎样判定候选结果是否可消费”。
- Core Thesis：自然语言格式要求只能表达期望；机器合同至少还需要显式 schema、类型映射、领域规则和可追踪失败语义。
- Claim IDs：`03-C01`、`03-C06`
- Evidence IDs：`03-E01`、`03-E06`
- Opening Example Plan：
  1. 复用 Article 02 的诊断语境，只展示一个形状整齐的候选对象：`status / summary / evidence_ids`。
  2. 连续追问：它是不是合法 JSON？字段是否齐全？类型是否正确？`EV-999` 是否存在？Tool 是否执行过？
  3. 用这些问题暴露“呈现形状”“结构合同”“领域事实”“外部动作”不是同一层。
- Contract-strength Callout：可用一个小型阶梯区分 `Prompt format instruction`、OpenAI 当前 `JSON mode`、OpenAI 当前 `Structured Outputs` 与 Application validation；必须注明这只是职责强度对照，不是跨 Provider 统一 API。
- Wording Strength：可以依据当前 OpenAI 官方文档说明 JSON mode 不保证指定 schema、Structured Outputs 提供受支持子集内的 schema contract；必须同时注明本篇没有执行 Provider call，也没有测量 runtime adherence。
- Boundary / Stop Line：不从官方 contract 推出实际模型质量、字段事实正确性或生产可靠性；不把 OpenAI 当前字段外推到其他 Provider。
- Bridge：先不讨论具体 SDK，建立 Application 真正需要守住的分层合同。

## 4. Section 1｜抽象模型：Candidate Output 进入系统前，要经过哪些责任边界？

- Reader Question：一份 Provider response 从到达 Application 到被业务接受，中间最少要拆成哪些层？
- Section Goal：建立全文唯一的合同主链，并让每个后续案例都能回到首失败层。
- Core Thesis：先处理 Provider / transport envelope，再让 candidate text 依次经过 Parse、Schema Validation、Typed Materialization 与 Domain Validation；每层只回答自己的问题，前一层失败后后一层必须保持 `NOT_RUN`。
- Claim IDs：`03-C02`、`03-C03`
- Evidence IDs：`03-E02`、`03-E03`
- Main Model：

```text
Provider / transport envelope
  ├─ request error
  ├─ refusal
  ├─ incomplete / truncation metadata
  └─ candidate text
         ↓
       Parse
         ↓
  Schema Validation
         ↓
  Typed Materialization
         ↓
  Domain Validation
         ↓
     Accept / Fail
```

- Layer Responsibility Table：

| Layer | Reader Must Learn | Success Does Not Prove |
|---|---|---|
| Provider / transport envelope | 是否存在可消费 candidate，还是 request error、refusal、incomplete 等 Provider-specific terminal state | candidate 正确或完整 |
| Parse | raw string 是否是单一合法 JSON value | 字段存在、类型或 schema 满足 |
| Schema Validation | instance 是否满足指定 dialect / validator / schema 中实际声明的 assertions | 引用实体存在、内容忠实、权限或外部动作成立 |
| Typed Materialization | schema-shaped data 是否能映射到 Application 预期 DTO | 领域不变量或事实正确 |
| Domain Validation | DTO 是否满足 Application facts 与跨字段规则 | 外部 Evidence 自身真实、Tool 已执行或任务质量合格 |

- First-failure Rule：正文必须明确“失败在哪一层，就停在哪一层”；不能把未运行的较晚层记作 `PASS`，也不能把所有错误统一包装成“JSON 错误”。
- Figure Responsibility：Figure 1 画完整责任链，Provider envelope 与 Lab 实际覆盖面用不同底色；Lab coverage 从 raw candidate 开始，不覆盖 envelope。
- Wording Strength：抽象链是本课程 / 本地实现采用的最小模型，不宣称所有 Provider 内部或所有应用都按同一内部 pipeline 部署。
- Boundary / Stop Line：不推断 Provider 内部 validation 顺序；不进入 Model Adapter 的跨 Provider error normalization。
- Bridge：链路建立后，先精确说明 Schema Validation 能守住哪些结构，又会把哪些问题留给 Domain。

## 5. Section 2｜JSON Schema 证明的是结构断言，不是事实

- Reader Question：当 validator 返回 `valid`，Application 到底获得了什么保证？
- Section Goal：把 `type / enum / required / additionalProperties` 的结构职责与 Evidence、权限、状态、Tool completion 的领域职责切开。
- Core Thesis：JSON Schema 只能判断 instance 是否满足当前 schema 中实际出现的 assertions；“schema valid”必须同时带上 dialect、validator / version 与 schema 三个限定。
- Claim IDs：`03-C02`
- Evidence IDs：`03-E02`
- Explanation Order：
  1. 用 `type`、`enum`、`required`、`properties`、`additionalProperties: false` 说明结构断言能判什么。
  2. 说明 Draft 2020-12 中 `format` 默认 annotation 与 optional assertion vocabulary 边界，避免把所有 format 都写成强制验证。
  3. 再说明 OpenAI 当前 Structured Outputs 有自己的 supported subset 与 root / required / additional-properties 规则；这是 Provider-specific contract，不是 JSON Schema 通用规范。
  4. 用 `evidence_ids: ["EV-999"]` 反例说明 string-array schema 可以通过，但 Application registry 仍可能拒绝它。
- Proves / Does Not Prove Table：

| Schema Can Prove Under Frozen Conditions | Schema Cannot Prove By Itself |
|---|---|
| 字段存在、primitive type、枚举集合、未声明字段、嵌套结构等已声明 assertions | Evidence ID 存在、summary 忠于日志、权限有效、状态当前、Tool 已执行、结果已验证 |

- Figure / Example Responsibility：Example 1 并排展示 `schema-valid / domain-valid` 与 `schema-valid / domain-invalid` 两个候选；唯一变量是 Evidence ID 是否在固定 allowlist 中。
- Wording Strength：使用“对当前 dialect / validator / schema 中声明的 assertion 通过”，不写“JSON Schema 保证数据正确”。
- Boundary / Stop Line：不展开完整 JSON Schema dialect 教程，不声称 NJsonSchema 实现 Draft 2020-12，不把 domain rules 强塞回 schema。
- Bridge：结构合同还要落到 Application 的 DTO；两套合同需要对齐，但不能假设自动同步。

## 6. Section 3｜具体机制：Schema、Typed DTO 与 Domain Rule 怎样在 C# 中各守一层？

- Reader Question：在 C# / .NET 中，如何把前面的抽象落成最小可追踪实现，而不是一个 `Deserialize<T>()` 就结束？
- Section Goal：以 Lab 01 的冻结 `DiagnosisCandidate` 合同说明 schema-first、strict DTO materialization 与 allowlist domain validation 的职责分工。
- Core Thesis：Schema 与 DTO 是两套需要显式保持一致的合同表面；Domain Validation 再消费 Application facts。任何一层失败都应产出自己的 terminal stage 与 error code。
- Claim IDs：`03-C02`、`03-C03`
- Evidence IDs：`03-E02`、`03-E03`
- Frozen Minimal Contract：
  - 字段：`status`、`summary`、`evidence_ids`。
  - Schema：Draft 4，冻结使用 `type / properties / required / enum / items / minLength / minItems / additionalProperties` 子集。
  - DTO：三个 required non-null 字段，使用严格 materialization 与显式 JSON 字段映射。
  - Domain：allowlist 固定为 `EV-001 / EV-002`；`SUPPORTED` 至少一个合法 ID，`INSUFFICIENT_EVIDENCE` 必须为空列表。
- Minimal Pseudocode Plan：正文只放一段 10—15 行职责伪代码，不复制完整 Lab 实现。

```text
parse(raw)                         or fail(PARSE_FAILED)
schema.validate(raw)               or fail(SCHEMA_FAILED)
dto = strict_materialize(raw)      or fail(DTO_FAILED)
domain.validate(dto, allowlist)    or fail(DOMAIN_FAILED)
accept(dto)
```

- Code Explanation Responsibilities：
  1. 每个 `or fail` 都保存该层结果，并让后续层保持 `NOT_RUN`。
  2. Schema pass 后仍要 materialize DTO；schema / DTO drift 必须作为实现差异暴露。
  3. Domain rule 依赖 allowlist，不可以用字符串修补制造 `EV-001`。
  4. `DTO_FAILED` 是正式设计分支，但冻结八类 fixture 没有独立 DTO terminal case；正文不得宣称已完整覆盖该分支。
- Concrete Runtime Callout：Lab 使用 `.NET SDK 10.0.301`、`System.Text.Json` 与 `NJsonSchema 11.6.1` 的固定 API 表面；package 选择只说明当前 Lab 可复现，不表达“最佳 validator”。
- Wording Strength：所有运行结论都带固定 runtime / package / schema / fixture / allowlist 限定。
- Boundary / Stop Line：不生成完整生产 library，不比较 validator 性能，不推断其他 runtime / OS / package 行为。
- Bridge：结构与领域失败已经可区分，但 refusal、truncation 与 repair 决策还不能只看 Parse error。

## 7. Section 4｜失败语义：Refusal、Truncation、Retry 与 Repair 为什么不能混成一个错误？

- Reader Question：同样得到 `INVALID_JSON`，为什么 malformed JSON、截断候选和一段 refusal-like 文本不应该自动走同一 repair loop？
- Section Goal：把 Provider envelope evidence、raw parse observation、fixture metadata 与 Application policy decision 分开。
- Core Thesis：raw string 可以证明 Parse 失败，却通常不能证明为什么失败；真实 refusal / incomplete 原因必须来自当前 Provider envelope 或 trace。`recommended_action` 是 policy label，不是已执行的 retry / repair。
- Claim IDs：`03-C05`
- Evidence IDs：`03-E05`
- Failure Decision Table：

| Observed / Declared Class | Local Result | Recommended Action | Must Not Be Written As |
|---|---|---|---|
| ordinary malformed JSON | `PARSE_FAILED / INVALID_JSON` | `UPSTREAM_RETRY_ELIGIBLE` | retry 已执行或必然成功 |
| schema failure | `SCHEMA_FAILED` | `UPSTREAM_RETRY_ELIGIBLE` | 模型能自动 repair |
| domain failure | `DOMAIN_FAILED` | `STOP_AND_RECHECK_DOMAIN_INPUT` | 通过改字符串即可制造事实 |
| synthetic truncated fixture | `PARSE_FAILED / INVALID_JSON` | `UPSTREAM_CAUSE_REQUIRED` | 已观察真实 token truncation |
| synthetic non-contract text | `PARSE_FAILED / INVALID_JSON` | `STOP_NON_CONTRACT_INPUT` | 已观察真实 Provider refusal |
| accepted fixture | `ACCEPTED` | `ACCEPT` | 内容、Evidence 或任务质量已验证 |

- Provider Contract Callout：根据 OpenAI 当前官方 contract，Application 应先检查 incomplete metadata，并把 refusal 与普通 output text 分开；这里只说明当前文档分支，不宣称 Lab 调用了 OpenAI 或观察了 runtime adherence。
- Automatic Repair Boundary：Lab 的 automatic repair attempts 固定为 `0`；正文不得展示“修复后成功”示例，也不得把 `UPSTREAM_RETRY_ELIGIBLE` 改写成模型调用已经发生。
- Figure Responsibility：Figure 2 画 `Envelope evidence -> candidate classification -> local validation -> policy decision`，其中 synthetic metadata 使用虚线，防止读者误解为 Provider observation。
- Wording Strength：使用“可以交给上游策略决定”“需要上游原因”“本地停止标签”；不使用“系统会重试”“模型会修复”。
- Boundary / Stop Line：完整 Provider error、streaming、retry、backoff 与 normalization 留给 Article 04；模型修复效果与 retry budget 需要新实验，当前不写。
- Bridge：接下来不用理想化示例收口，而是检查冻结 Lab 真实运行到底观察到了什么。

## 8. Section 5｜Lab 01：八类输入怎样把首失败层变成可复查证据？

- Reader Question：怎样证明本地 pipeline 真的保留了 Parse、Schema、DTO、Domain 边界，而不是只画了一张架构图？
- Section Goal：把 Lab 01 的 Design、Expected、Observed、Interpretation 与 Claim Status 串成一条可审计证据链，并保留失败过程。
- Core Thesis：冻结 Expected 在运行前定义判据，raw JSONL 保存每层状态；当前环境中的两次运行精确命中八类矩阵且 byte-identical，但结论只覆盖该 fixture。
- Claim IDs：`03-C03`、`03-C04`、`03-C05`、`03-C07`
- Evidence IDs：`03-E03`、`03-E04`、`03-E05`、`03-E07`
- Lab Integration Order：
  1. **Design**：说明固定 runtime、package、schema、DTO、allowlist、八个 exact raw strings 与 first-failure acceptance criteria。
  2. **Expected**：展示八类 case 的预期 terminal stage / code / action，明确这是运行前判据。
  3. **Observed**：引用两份各八行 JSONL 的 terminal counts、stage trace、repair count 与共同 hash。
  4. **Interpretation**：只把观察升级为 fixed-local-fixture Claim；列出全部未证明项。
- Observed Matrix Summary：

| Terminal Stage | Count | Representative Case |
|---|---:|---|
| `ACCEPTED` | 1 | `valid-accepted` |
| `PARSE_FAILED` | 3 | `invalid-json`、`truncated-json`、`synthetic-refusal-text` |
| `SCHEMA_FAILED` | 3 | `missing-required`、`wrong-type`、`extra-property` |
| `DOMAIN_FAILED` | 1 | `nonexistent-evidence` |

- Determinism Record：两次 JSONL 均为八行、八个唯一 case、一个 accepted；SHA-256 均为 `C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`，byte-for-byte comparison=`True`。
- Stage-trace Example Plan：正文最多选三行 raw observation 做对照：
  - `invalid-json`：Parse `FAIL`，其余 `NOT_RUN`；
  - `extra-property`：Parse `PASS`、Schema `FAIL`，DTO / Domain `NOT_RUN`；
  - `nonexistent-evidence`：Parse / Schema / DTO `PASS`，Domain `FAIL`。
  其余五类放在汇总表，不复制整份 JSONL。
- Failure-preservation Callout：执行记录必须保留首次 sandbox restore 的 `NU1301` 与初始 build 的 `CS0246`；说明相同 restore 经审批成功、测试 using 的局部修复没有改 Design / schema / fixture / Expected，最终 build `0 warnings / 0 errors`、tests `5/5`。这些是实验过程事实，不是 Provider 或 Claim failure。
- What the Lab Proves：固定 `.NET 10.0.301 + NJsonSchema 11.6.1 + Draft 4 subset + DTO + eight fixtures + allowlist` 下的首失败分层、矩阵匹配、zero automatic repair 与本机重复 artifact。
- What the Lab Does Not Prove：
  - Provider schema adherence 或真实 model output；
  - 真实 refusal、incomplete 或 truncation cause；
  - model repair、retry success、accuracy 或 production reliability；
  - NJsonSchema 完整 Draft 2020-12 conformance；
  - 其他 runtime、package、OS、schema、allowlist、并发或负载行为；
  - Evidence / summary 事实正确、Tool 已执行或 evaluator 已完成。
- Figure / Table Responsibility：Table 3 是唯一 Lab result table；不生成成功率图、Provider 对比图或泛化趋势图。
- Wording Strength：使用“本机固定 fixture 观察到”“两次 artifact 相同”；不使用“Structured Outputs 已被证明可靠”。
- Boundary / Stop Line：不重跑 Lab、不修改 Expected、不创造第三次结果、不把 synthetic fixture 改写成线上案例。
- Bridge：Lab 证明了合同链能拒绝哪些候选；最后要明确这条链在完整 Agent 系统里不承担哪些职责。

## 9. Section 6｜工程边界：机器可消费不是可信、可执行或已评测

- Reader Question：当 Structured Result 已经通过 Parse、Schema、DTO 与 Domain，Application 可以安全宣布什么，又必须把什么交给后续系统？
- Section Goal：把 Structured Output 定位为后续 Tool、Evidence 与 Eval 的前置合同，而不是替代品。
- Core Thesis：结构合同提供稳定字段、typed boundary 与 failure labels；Tool Runtime 负责 policy / execute / result，Evidence Contract 负责可审计事实链，Eval 负责质量判定与回归。Structured Output 不吞掉这些系统。
- Claim IDs：`03-C06`、`03-C07`
- Evidence IDs：`03-E06`、`03-E07`
- Boundary Matrix：

| Structured Output Can Provide | Independent System Responsibility | Course Stop Line |
|---|---|---|
| typed Tool arguments / result shape | Tool policy、permission、execute、timeout、side effect、trace | Article 05—06 |
| `evidence_ids` 等稳定字段 | Evidence existence、provenance、claim-to-source、verification | Article 18 |
| failure stage / error labels | dataset、grader、metric、golden set、regression | Article 21—22 |
| local candidate acceptance | Provider normalization、streaming completion、retry / backoff | Article 04 |

- Engineering Judgment：即使 Domain Validation 通过，也只能说明当前 Application rules 接受该 DTO；如果 allowlist 本身过期、summary 与日志不符或外部 Tool 没执行，结果仍可能不可信。
- Anti-patterns：
  1. `schema valid -> answer true`；
  2. `recommended_action -> retry executed`；
  3. `evidence_ids present -> Evidence verified`；
  4. `typed tool arguments -> Tool authorized / executed`；
  5. `8 / 8 fixture pass -> production reliable`。
- Wording Strength：使用“前置”“提供接口”“不替代”，不提前定义后续文章完整机制。
- Boundary / Stop Line：不设计 Tool Registry、Evidence DTO final shape、Eval grader 或 BuildPilot final contract。
- Bridge：用一个审查清单和最短结论收回完整责任链，并把 Provider integration 交给 Article 04。

## 10. Closing｜下次收到一段“看起来正确”的 JSON，先问它通过了哪一层

- Reader Question：读者离开本篇后，怎样快速审查一条 Structured Output pipeline？
- Section Goal：把全文压缩成可迁移的工程检查顺序。
- Core Thesis：不要把 `JSON` 当作一个布尔标签；记录 candidate 来自什么 envelope、在哪个 dialect / validator / schema 下通过、是否能 materialize、通过了哪些 domain rules，以及尚未证明哪些外部事实。
- Claim IDs：`03-C01`—`03-C07`
- Evidence IDs：`03-E01`—`03-E07`
- Recap Checklist：
  1. Provider envelope 是 ordinary candidate、refusal、incomplete 还是 request error？
  2. raw string 是否为单一合法 JSON value？
  3. 使用的是哪个 dialect、validator / version 与 schema？
  4. schema 和 DTO 是否保持一致，materialization 是否严格？
  5. 哪些 domain facts / invariants 仍需 Application 验证？
  6. failure 后的 action 是标签、提议还是实际已执行动作？
  7. 当前证据来自官方 contract、本地 fixture，还是 production observation？
- Article 02 Bridge Recap：Article 02 负责自然语言任务与 Output Requirement 可审查；本篇负责候选结果的机器合同与本地 validation boundary。
- Article 04 Forward Boundary：下一篇接管 Provider streaming、error、retry 与 adapter normalization；本篇只把 envelope 放到图上并保存 local policy label，不实现 Gateway 或跨 Provider taxonomy。
- Final Sentence：`Structured Output 的价值不是让 JSON 看起来整齐，而是让程序知道何时接受、何时拒绝，以及自己仍然没有证明什么。`

## 11. Figure / Table / Example Responsibilities

| ID | Artifact | Teaching Responsibility | Must Not Imply |
|---|---|---|---|
| Figure 1 | `Envelope-to-Accept Contract Chain` | 展示 Provider envelope、四层 local validation 与 first-failure stop | Provider 内部 pipeline、Lab 覆盖 envelope、跨 Provider统一字段 |
| Figure 2 | `Observation vs Metadata vs Policy Decision` | 区分 raw parse result、declared synthetic class 与 recommended action | 真实 refusal / truncation cause、retry / repair 已执行 |
| Table 1 | `Contract Strength and Responsibility` | 从 Article 02 Output Requirement 过渡到 Provider contract 与 Application validation | OpenAI 字段是行业统一 schema、runtime adherence 已测 |
| Table 2 | `Schema Proves / Does Not Prove` | 切开结构断言与 domain / truth / execution | schema valid 等于事实正确 |
| Table 3 | `Lab 01 Observed Matrix` | 保存八类 frozen fixture 的 terminal counts、codes 与 stage traces | model / Provider / production distribution |
| Table 4 | `Structured Output vs Downstream Systems` | 把 Tool、Evidence、Eval、Gateway 责任切出 | 本篇已经实现后续系统 |
| Example 1 | `EV-001 vs EV-999` | 展示 schema-valid 可以 domain-invalid | allowlist 证明 Evidence truth |
| Pseudocode 1 | `First-failure Validation Pipeline` | 展示 parse / schema / DTO / domain 的早停骨架 | 完整生产实现、所有 failure branch 已由 Lab 覆盖 |

Asset Policy：本轮不创建 `assets/`。Outline 只定义图表、代码与 example 职责；Draft 优先使用 Markdown 文本图、表和短伪代码。Lab raw artifacts 只链接或摘取最小必要字段，不复制为新的“结果资产”。

## 12. Failure-path Coverage Plan

| Failure Path | Main Placement | Required Interpretation | Forbidden Upgrade |
|---|---|---|---|
| Request error / refusal / incomplete metadata | Section 1、4 | Provider envelope 分支；按当前 Provider contract 读取 | Lab 已观察、跨 Provider统一 taxonomy |
| Malformed JSON | Section 1、4、5 | Parse 失败，后续层 `NOT_RUN` | 自动 repair 会成功 |
| Synthetic truncated JSON | Section 4、5 | raw-only=`INVALID_JSON`；原因来自 fixture metadata | 真实 token limit / transport truncation |
| Synthetic non-contract text | Section 4、5 | 本地 parse reject + stop label | 真实 Provider refusal observation |
| Missing required / wrong type / extra property | Section 2、3、5 | Schema assertion 失败 | DTO / Domain 已运行 |
| DTO materialization failure | Section 3 | 正式 branch，但无独立 frozen terminal fixture | Lab 已完整覆盖 DTO failure space |
| Unknown Evidence ID | Section 2、3、5 | Schema / DTO pass，Domain fail | Evidence truth 被完整验证 |
| Restore / build early failure | Section 5 | 保留真实执行过程与局部 disposition | Provider / contract claim failure |

Coverage Rule：Draft 不得只展示 happy path。至少保留一个 Parse、一个 Schema、一个 Domain stage trace，以及 synthetic refusal / truncation 的非 Provider边界。

## 13. Learning Check Plan

1. 一段文本能被 `JsonDocument.Parse` 接受，是否已经是机器可消费的业务结果？还缺哪些层？
   - Reference Judgment：只证明当前 parse contract 通过；仍需 schema、DTO、domain，并保留外部 truth / execution boundary。
2. `evidence_ids` 是 string array 且值为 `EV-999`，为什么 Schema 可能通过、Domain 仍应失败？
   - Reference Judgment：schema 只约束已声明结构；ID 是否存在需要 Application registry / facts。
3. 同样是 `INVALID_JSON`，为什么 ordinary malformed、synthetic truncated 与 synthetic refusal-like text 的 action 可以不同？
   - Reference Judgment：raw parse observation 相同，差异来自可信 envelope / declared metadata 与 policy；不能从 raw string 单独推断真实原因。
4. `UPSTREAM_RETRY_ELIGIBLE` 是否表示系统已经重试，或者模型一定能 repair？
   - Reference Judgment：都不是；它只是本地 decision label，Lab automatic repair attempts=`0`。
5. Lab 01 的 `8 / 8` 与 byte-identical 双运行能证明什么，不能证明什么？
   - Reference Judgment：只证明固定本机、package、schema、DTO、fixtures 与 allowlist 的本地可复现结果；不证明 Provider、模型、其他环境或生产可靠性。
6. Structured Result 通过 Domain Validation 后，是否能宣布 Tool 已执行、Evidence 已验证、任务质量已合格？
   - Reference Judgment：不能；三者分别属于 Tool Runtime、Evidence Contract 与 Eval。
7. 接入新 Provider 时，本篇哪些抽象可以保留，哪些字段与行为必须重新核对？
   - Reference Judgment：分层责任可以保留；envelope fields、supported schema subset、refusal / incomplete、streaming / error / retry contract 必须按当前 Provider / API / model / version 核对。

## 14. Claim-to-Section Coverage Matrix

| Claim ID | Status | Main Placement | Evidence | Wording / Coverage Guard |
|---|---|---|---|---|
| `03-C01` | `CONFIRMED` | Opening、Closing | `03-E01` | 只写 OpenAI 当前 docs contract；无 Provider call / adherence observation，不外推其他 Provider |
| `03-C02` | `CONFIRMED` | Section 1—3 | `03-E02` | Schema 只覆盖声明 assertions；dialect / validator / schema 限定不可省略，不写事实保证 |
| `03-C03` | `CONFIRMED` | Section 1、3、5 | `03-E03` | 固定本地 fixture 的首失败链；无独立 DTO terminal fixture，禁止泛化 |
| `03-C04` | `CONFIRMED` | Section 5 | `03-E04` | 八类 synthetic inputs、两次 JSONL、byte-identical；不写真实模型分布或可靠率 |
| `03-C05` | `CONFIRMED` | Section 4—5 | `03-E05` | local policy labels + repair attempts=0；不写真实 refusal / truncation cause、retry 或 model repair |
| `03-C06` | `CONFIRMED` | Opening、Section 6、Closing | `03-E06` | Structured Output 是前置合同，不执行 Tool、不证明 Evidence、不替代 Eval |
| `03-C07` | `CONFIRMED` | Section 5—6 | `03-E07` | 版本、环境、package、Draft 4 subset、fixture 与 allowlist 全限定；不外推生产或 Draft 2020-12 conformance |

Coverage Result：`7 / 7 Claims mapped`；`0 PARTIAL / 0 BLOCKED` 进入 Outline；四个 Lab-dependent Claim 均保留 fixed-local-fixture wording guard。

## 15. Job Competency Mapping

| Competency | Article Evidence of Learning | Assessment Surface |
|---|---|---|
| API / Provider contract reading | 能区分 documented contract 与实际 runtime observation，并保留 Provider / model / version scope | Opening + Learning Check 7 |
| Schema boundary judgment | 能解释 assertion、dialect、validator、schema 与 domain fact 的边界 | Section 2 + Learning Check 2 |
| Typed result design | 能说明 schema、strict DTO 与 domain rules 是三套需对齐但不等价的合同 | Section 3 + Learning Check 1 |
| Failure taxonomy | 能把 Parse、Schema、DTO、Domain 与 envelope failure 分层并保留 first failure | Section 1、4 + Learning Check 3 |
| C# validation pipeline | 能用早停伪代码表达本地 pipeline，不把 `Deserialize<T>()` 当完整验证 | Section 3 |
| Fixture / fault injection | 能解释八类输入分别注入什么 failure，以及 Expected 如何先于 Observed 冻结 | Section 5 + Learning Check 5 |
| Raw observation discipline | 能从 JSONL stage trace、hash 与 execution log 形成收窄 Claim | Section 5 |
| Retry / repair boundary | 能区分 policy label、实际 action 与 model outcome | Section 4 + Learning Check 4 |
| Cross-system architecture boundary | 能把 Structured Output 与 Tool Runtime、Evidence、Eval、Gateway 分开 | Section 6 + Learning Check 6 |

## 16. Adjacent Article Stop Lines

| Adjacent / Future Article | Article 03 May Introduce | Article 03 Must Stop Before |
|---|---|---|
| Article 02｜Prompt Engineering | 自然语言 Output Requirement 是本篇输入前置 | 重讲完整 Prompt 六项合同、few-shot、delimiter 或 Prompt change fixture |
| Article 04｜Model Adapter / Gateway | Provider envelope 有 error、refusal、incomplete / truncation 与 candidate；local action 需要上游承接 | streaming aggregation、跨 Provider error normalization、retry / backoff、Gateway interface 与 production policy |
| Article 05｜Function Calling | Tool arguments 也需要结构合同 | function calling wire contract、action intent 与 tool selection |
| Article 06｜Tool Runtime | schema / domain boundary 会被 Tool validation 复用 | policy、permission、execute、timeout、side effect、result / trace pipeline |
| Article 18｜Evidence Contract | typed result 可以携带 `evidence_ids` | provenance、claim-to-evidence、truth / verification 与完整 Evidence DTO |
| Article 21—22｜Trace / Eval | failure labels 与 raw artifact 是后续 trace / eval input | failure taxonomy 全貌、dataset、grader、metric、golden set、regression system |

Cross-boundary Rule：如果 Draft 需要完整 Provider retry、model repair、Tool execution、Evidence truth、Eval 或 production statistics 才能支撑核心结论，返回 `RETURN_TO_RESEARCH` 或删除该结论；不能以“工程建议”掩盖证据缺口。

## 17. Evidence Omission List

- 不新增 Provider 调用、模型输出或 runtime adherence 数字；当前 Provider 内容只使用已确认的 OpenAI 官方 contract。
- 不把 OpenAI supported subset 写成 JSON Schema 通用规则，也不写成其他 Provider 事实。
- 不宣称 NJsonSchema 完整实现 Draft 2020-12；Lab 只使用 Draft 4 frozen shared-keyword subset。
- 不把 `synthetic-refusal-text` 写成真实 refusal，不把 `SYNTHETIC_TRUNCATED` 写成真实 token / transport truncation。
- 不展示 repaired candidate，不声称 retry 或 model repair 已执行 / 成功；automatic repair attempts=`0`。
- 不把 allowlist validation 写成 Evidence 或 summary 的事实核验。
- 不把 typed Tool arguments 写成 Tool 已授权、已执行或产生副作用。
- 不把八类 fixture 的本机 PASS 写成 accuracy、跨环境 determinism、生产 reliability 或质量 Eval。
- 不为完整性新增 DTO-failure observation；当前只说明设计 branch 未被独立 fixture 覆盖。

## 18. Outline Gate Checklist

- [x] Article Type 明确为原理篇 / Lab Article，第一屏从 Article 02 留下的工程问题开始，不以 API / package 开场
- [x] Teaching Spine 遵循 Problem Space -> Abstract Model -> Concrete Mechanism -> Engineering Boundary -> Verification Boundary
- [x] 每个主体 Section 均有 Reader Question、Section Goal、Claim、Evidence、Wording Strength 与 Boundary / Stop Line
- [x] `7 / 7` Claim 已映射；`0 PARTIAL / 0 BLOCKED`
- [x] Lab 01 的 Design、Expected、Observed、Interpretation 与 Claim Status 分离
- [x] 八类 fixture、首失败层、failure process、5/5 tests、双运行 JSONL 与 hash 均有明确教学职责
- [x] no Provider call / adherence observation、no real refusal / truncation cause、no model repair、no truth / Tool / Eval / production claim 已显式冻结
- [x] minimal C# / pseudocode plan 不复制完整实现，也不虚构未覆盖的 DTO observation
- [x] Figures、Tables、Examples 均写明 `Must Not Imply`
- [x] Learning Check 覆盖 Reader Promise；Job Competency mapping 有 assessment surface
- [x] Article 02 bridge 与 Article 04 forward boundary 清楚，未提前写完整 Gateway / retry
- [x] 本 Outline 没有引入新核心事实；`RETURN_TO_RESEARCH = NONE`

Recommendation：`PASS`。建议 Master 将 Article 03 推进为 `OUTLINE_READY`；下一允许动作仅为 Author 依据本 Outline 与批准 Evidence 创建 `draft.md`。Author 不更新 README、`status.md`、`course-run-state.md`、canonical、Research、Evidence、Review、Lab 或 Published Content。
