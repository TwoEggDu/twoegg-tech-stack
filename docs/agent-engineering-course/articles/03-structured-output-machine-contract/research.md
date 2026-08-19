# Article 03 Research

- Research Phase：`RESEARCH / PRELIMINARY_EVIDENCE / LAB_DESIGN / EVIDENCE_MERGE`
- Research Status：`EVIDENCE_COMPLETE`
- Lifecycle Candidate：`OUTLINE_READY`
- Evidence Status：`PASS`
- Evidence Gate Recommendation：`PASS / OUTLINE_ALLOWED`
- Required Lab：`Lab 01｜Structured Output`
- Lab Design Status：`FROZEN`
- Lab Execution Status：`PASS / OBSERVATION_MERGED`
- Lab Path：`docs/agent-engineering-course/labs/lab-01-structured-output-validation/`
- Research Window：`2026-08-20（Asia/Shanghai）`
- Provider Strategy：`OpenAI current official docs only`
- Non-OpenAI Counter-check：`NOT_REQUIRED`（不会改变本篇边界）
- Runtime Candidate：`.NET SDK 10.0.301`
- Provider Credentials：`ABSENT / NOT_REQUIRED`

## Scope And Method

本篇研究怎样把 Article 02 的自然语言 Output Requirement 推进为机器可消费的候选结果合同。问题空间不是“怎样让模型说出更漂亮的 JSON”，而是：Application 收到一段候选文本后，怎样把语法、结构、类型、业务引用与外部完成状态分开判断。

来源面保持最小闭合：

1. OpenAI 当前 Structured Outputs 官方文档用于确认一家 Provider 的 JSON mode、Structured Outputs、refusal、incomplete 与 supported-schema contract；这些结论不外推到其他 Provider。
2. JSON Schema Draft 2020-12 Core 与 Validation vocabulary 用于确认 assertion、`type`、`enum`、`required`、`additionalProperties` 与 dialect 边界。
3. Microsoft `System.Text.Json` 官方文档用于确认 .NET 10 的 parse、strict deserialization、required / unmapped-member 与 duplicate-property surface。
4. Lab 01 使用许可证清晰的 `NJsonSchema 11.6.1`，只验证一个显式标为 Draft 4、由两代规范共同支持的最小关键字子集；不宣称该 package 是 Draft 2020-12 完整实现。

未加入第二家 Provider。OpenAI 当前 contract 已足以说明“Provider envelope / supported schema 是版本敏感层”；JSON Schema 与本地 fixture 已承担可迁移边界。继续扩展 Provider 只会增加支持矩阵，不会改变 Lab 的本地可判定问题。

## Research Question Answers

| RQ | Status | Answer | Claim / Evidence |
|---|---|---|---|
| `RQ-01` | `ANSWERED` | 自然语言“请输出 JSON”只是 Output Requirement；OpenAI 当前 JSON mode 以 valid JSON 为目标但不保证指定 schema；当前 Structured Outputs 通过 `json_schema` / `strict` 与受支持的 JSON Schema 子集提供 schema adherence contract。typed SDK helper 是 wire contract 的语言映射，不自动证明事实或业务正确。 | `03-C01` / `03-E01` |
| `RQ-02` | `ANSWERED` | JSON Schema 只对 schema 中实际出现的 assertion 作 pass / fail。`type` 约束 JSON primitive，`enum` 约束候选值集合，`required` 约束属性存在，`additionalProperties: false` 可拒绝未声明属性；它们不能证明 Evidence ID 存在、摘要忠于日志、权限有效或外部动作已执行。Draft 2020-12 的 `format` 默认是 annotation，assertion vocabulary 仍是可选实现能力。 | `03-C02` / `03-E02` |
| `RQ-03` | `ANSWERED / LOCAL_FIXTURE_CONFIRMED` | 固定链路 `Raw Candidate -> Parse -> Schema Validate -> Typed Materialize -> Domain Validate -> Accept / Fail` 已在 Lab 01 的八个输入中执行。早期失败后的较晚层全部记录为 `NOT_RUN`；能够到达 DTO / Domain 的输入保留了对应 stage trace。该结果只覆盖固定本地 fixture。 | `03-C03` / `03-E03` |
| `RQ-04` | `ANSWERED / LOCAL_FIXTURE_CONFIRMED` | valid、invalid JSON、missing required、wrong type、extra property、schema-valid/nonexistent Evidence ID、truncated JSON 与 synthetic refusal text 八类输入全部命中冻结的 terminal stage、code 与 action；两次运行均得到相同的八行 JSONL。 | `03-C04` / `03-E04` |
| `RQ-05` | `ANSWERED / LOCAL_POLICY_CONFIRMED` | OpenAI 当前 contract 把 `refusal` 与 incomplete metadata 放在候选 JSON 之外；本地 Lab 进一步确认：固定 decision 能把 parse / schema failure 标为上游候选，把 domain failure 与 synthetic non-contract input 标为 stop，并要求 truncated fixture 补上游原因。八个 case 的 automatic repair attempts 均为 `0`。这些 action 依赖冻结的 `declared_input_class`，不能从 raw string 推断真实 Provider 原因。 | `03-C05` / `03-E05` |
| `RQ-06` | `ANSWERED` | Structured Output 能给 Tool 参数、Evidence result 与 Eval record 提供稳定字段和可分类失败，但不会执行 Tool、证明 Evidence 真实或替代 evaluator。OpenAI 当前也把“连接 tools / functions”与“结构化面向用户的文本输出”分为 function calling 与 structured `text.format` 两类职责。 | `03-C06` / `03-E06` |
| `RQ-07` | `ANSWERED / LOCAL_RUNTIME_CONFIRMED` | Lab 01 在 `.NET SDK 10.0.301`、`NJsonSchema 11.6.1`、Draft 4 shared-keyword schema 与固定 Evidence allowlist 下完成 locked restore、最终 clean build、5/5 tests 和两次 deterministic run。两份 JSONL 的 SHA-256 都是 `C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`。它没有 Provider call、credential read 或模型输出。 | `03-C07` / `03-E07` |

## Stable Contract Model

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

这张图刻意把 Provider envelope 放在 raw candidate 之前。若 Provider 明确返回 refusal 或 incomplete metadata，Application 不应该假装它是一份普通候选 JSON 再强行修补。Lab 01 没有 Provider envelope，因此只验证下半段；synthetic refusal text 只是“非合同输入”fault case，不是 Provider observation。

### 每层负责什么

| Layer | Input | Success | Failure Meaning | Not Proved |
|---|---|---|---|---|
| Provider / transport envelope | Provider response | 有可消费 candidate text | request error、refusal、incomplete 或 Provider-specific terminal state | 不证明 candidate 正确 |
| Parse | raw string | 单一合法 JSON value | syntax、truncation或非 JSON；单靠 raw text 不一定能区分原因 | 不证明字段存在或类型满足 schema |
| Schema Validation | parsed / raw JSON + frozen schema | 满足 schema 中实际声明的 assertions | missing、wrong type、enum、extra field 等结构问题 | 不证明引用实体存在或事实真实 |
| Typed Materialization | schema-valid JSON | 得到预期 C# DTO | runtime binding contract 与 schema / naming 漂移 | 不证明领域不变量 |
| Domain Validation | typed DTO + application facts | Evidence ID 存在且跨字段规则成立 | schema 无法表达或不应承担的业务失败 | 不证明外部 Evidence 自身真实或 Tool 已执行 |

## JSON Schema Boundary

Draft 2020-12 Core 明确：JSON Schema assertion 对 instance 产生 pass / fail，而 instance 只能因为 schema 中存在的 assertion 失败。因此“schema valid”必须带上三项限定：

- 哪个 dialect；
- 哪个 validator / version；
- 哪份 schema 和哪些 vocabulary / keywords。

本篇可以安全使用的结构主张：

- `type`：约束 primitive type；
- `enum`：值必须等于列出的某一项；
- `required`：列出的 property 必须存在；
- `properties`：把 subschema 应用到已命名 property；
- `additionalProperties: false`：未被 `properties` / `patternProperties` 覆盖的 property 会对 `false` schema 验证失败；
- nested object / array：用 subschema 继续约束内部结构。

不能由这些关键字自动推出：

- `evidence_ids: ["EV-999"]` 对应真实 Evidence；
- `summary` 忠实引用了输入日志；
- 状态枚举代表外部系统当前状态；
- Tool 已执行、权限已批准或副作用已发生；
- validator 实现了没有被当前 dialect / vocabulary 要求的所有格式或扩展关键字。

OpenAI 当前 Structured Outputs 还增加了 Provider-specific subset：当前文档要求 root 为 object、所有字段列入 `required`、对象设置 `additionalProperties: false`，并只支持 JSON Schema 的一个子集。它不能被反向写成 JSON Schema 通用规范。

## .NET Fixture Surface

### Parse 与 typed DTO

- `JsonDocument.Parse(string, JsonDocumentOptions)` 在输入不是单一合法 JSON value 时抛 `JsonException`。
- .NET 10 `JsonDocumentOptions.AllowDuplicateProperties = false` 可让 duplicate property 在 parse 阶段失败。
- .NET 10 `JsonSerializerOptions.Strict` 会拒绝 unmapped members 与 duplicate properties，保持大小写敏感，并启用 nullable annotations / required constructor parameters。
- `required` / `[JsonRequired]` 可让 missing property 在 deserialization 时抛 `JsonException`；它与 JSON Schema `required` 是两套合同表面，必须避免默认它们永远自动同步。

Lab 使用 schema-first，再使用显式 `[JsonPropertyName]` 的 `DiagnosisCandidate` DTO 做 strict materialization。若两者结果不一致，必须保存为 fixture / implementation discrepancy，不能选择性忽略。

### Validator / package 决策

- Selected：`NJsonSchema 11.6.1`
- NuGet：[NJsonSchema 11.6.1](https://www.nuget.org/packages/NJsonSchema/11.6.1)
- Release Tag：[v11.6.1](https://github.com/RicoSuter/NJsonSchema/releases/tag/v11.6.1)
- License：`MIT`，NuGet package metadata 与 repository `LICENSE.md` 一致。
- Runtime Surface：package 包含 `net8.0` target，NuGet 标为与 `net10.0` compatible。
- Fixed API：`JsonSchema.FromJsonAsync(string)` 加载 schema；`schema.Validate(string)` 返回 `ICollection<ValidationError>`。
- Dialect Boundary：项目 README 描述 Draft v4+，但没有提供本实验所需的 Draft 2020-12 完整合规承诺；因此 fixture 明确使用 `http://json-schema.org/draft-04/schema#`，只使用 `type / properties / required / enum / items / minItems / additionalProperties` 共同子集。
- Excluded：`JsonSchema.Net 9.4.0`。其源码仓库显示 MIT，同时当前 README 说明 NuGet binary 带 EULA 且营收用户可能需要维护费；许可面不够单一，不冻结为课程 Lab 依赖。

选择 NJsonSchema 不是为了证明某个 validator 普遍最佳，而是得到一个许可证清晰、版本固定、API 可核对的最小执行面。Lab 结论只覆盖这一个 package / schema / runtime。

## Repair / Retry Local Decision Boundary

Lab 不调用模型，也不实现自然语言 repair。它只输出一个可审计的本地 decision label：

| Failure Class | Local Decision | Boundary |
|---|---|---|
| Parse failure | `UPSTREAM_RETRY_ELIGIBLE` | 表示外层策略可以选择重新请求；不修改 raw text，不证明重试会成功 |
| Schema failure | `UPSTREAM_RETRY_ELIGIBLE` | 可把 validation errors 作为未来上游输入；本 Lab 不调用 Provider |
| Domain failure | `STOP_AND_RECHECK_DOMAIN_INPUT` | 不允许用语法修补伪造不存在的 Evidence ID |
| Synthetic refusal text | `STOP_NON_CONTRACT_INPUT` | 只说明此 raw string 不是候选 JSON；不声称观察到 Provider refusal |
| Truncated raw JSON | `UPSTREAM_CAUSE_REQUIRED` | raw text 只能证明 parse fail；需要 Provider envelope / trace 才能区分 token limit、传输中断或其他原因 |
| Accepted | `NONE` | 只接受当前 fixture；不证明内容真实 |

自动 repair attempt 数固定为 `0`。未来若课程需要 Provider retry、budget adjustment 或 repair prompt，必须在 Model Adapter / Eval 文章中单独设计并真实执行。

Lab 01 的实际 observation 与这张决策表一致：三个 parse failure 分别保留 `UPSTREAM_RETRY_ELIGIBLE`、`UPSTREAM_CAUSE_REQUIRED`、`STOP_NON_CONTRACT_INPUT`；三个 schema failure 为 `UPSTREAM_RETRY_ELIGIBLE`；domain failure 为 `STOP_AND_RECHECK_DOMAIN_INPUT`；accepted case 为 `ACCEPT`。这只确认本地 classifier 对冻结 metadata 的处理，不确认 retry 会成功，也不确认模型可以 repair。

## Evidence Merge Decision

- `03-C01`、`03-C02`、`03-C06` 继续由当前一手文档确认。
- `03-C03`、`03-C04`、`03-C05`、`03-C07` 已按 `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 合并 Lab 01 raw artifacts，并收窄为固定本地 fixture Claim。
- Lab 01 的 Expected、Acceptance Criteria 与 Observations 原始事实未在合并阶段反向修改。
- 所有七个核心 Claim 均无 `BLOCKED`；Evidence Gate = `PASS`，允许下一阶段创建 Outline，但本角色不创建。

## Research Conclusions

- Research Questions：`7`；`7` 个均已回答。
- Claim Register：`7`；`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`。
- Provider call：`NONE`；Provider credentials：`ABSENT / NOT_REQUIRED`。
- Lab Design：`FROZEN`；执行状态：`PASS / EVIDENCE_MERGED`。
- Remaining Blocker：`NONE_AT_EVIDENCE_GATE`。
- Next Action：`OUTLINE`。

## Research Stop Line

Researcher 在 `EVIDENCE_MERGE` 与 Evidence Gate PASS 后停止。下一动作是 `OUTLINE`；本角色不创建或修改 Outline、Draft、Review、Published Content、Article README、`status.md`、`course-run-state.md` 或 canonical，不重新执行 Lab，也不 commit / push。
