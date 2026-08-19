# Article 03 Evidence Register

- Evidence Phase：`PRELIMINARY_EVIDENCE / LAB_DESIGN / LAB_OBSERVATION / EVIDENCE_MERGE`
- Evidence Status：`PASS`
- Evidence Gate：`PASS`
- Claim Count：`7`
- Claim Summary：`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`
- Evidence Card Count：`7`
- Retrieved / Verified At：`2026-08-20（Asia/Shanghai）`
- Required Lab：`Lab 01｜Structured Output`
- Lab Dependency：`REQUIRED`
- Lab Design：`FROZEN`
- Lab Observation：`EXECUTED / MERGED / PASS`
- Provider Calls：`NONE`

## Claim Register

| Claim ID | 可进入后续 Outline 的收窄主张 | Status | Lab Dependency | Evidence |
|---|---|---|---|---|
| `03-C01` | 自然语言 JSON 要求、JSON mode 与 Structured Outputs 是不同强度的 contract；OpenAI 当前 Structured Outputs 只支持 JSON Schema 子集，Provider 字段与支持矩阵不可外推。 | `CONFIRMED` | `CONDITIONAL` | `03-E01` |
| `03-C02` | JSON Schema 只证明 instance 满足 schema 中实际声明的 assertions；它不证明事实、引用实体、权限、Tool 执行或外部状态。 | `CONFIRMED` | `CONDITIONAL` | `03-E02` |
| `03-C03` | 在固定 `.NET 10.0.301 + NJsonSchema 11.6.1` fixture 中，Parse、Schema、Typed DTO 与 Domain Validation 按冻结链路保留了首个失败层，早期失败后的层为 `NOT_RUN`。 | `CONFIRMED` | `SATISFIED_BY_LOCAL_LAB` | `03-E03` |
| `03-C04` | 八类 frozen raw-string fixture 的 terminal stage、code 与 action 全部命中预期，且两次运行生成 byte-identical JSONL。 | `CONFIRMED` | `SATISFIED_BY_LOCAL_LAB` | `03-E04` |
| `03-C05` | 固定本地 classifier 能区分 retry-eligible、需要上游原因与 stop decision，并保持 automatic repair attempts=`0`；它不推断真实 refusal / truncation 原因，也不执行模型 repair。 | `CONFIRMED` | `SATISFIED_BY_DOCS_AND_LOCAL_LAB` | `03-E05` |
| `03-C06` | Structured Output 为 Tool / Evidence / Eval 提供稳定字段与失败标签，但不执行 Tool、不证明 Evidence 真实、不替代 evaluator。 | `CONFIRMED` | `CONDITIONAL` | `03-E06` |
| `03-C07` | Lab 01 只确认 `.NET 10.0.301 + NJsonSchema 11.6.1 + frozen Draft 4 subset + eight fixtures + fixed allowlist` 的本地可复现行为；不外推 Provider、模型、Draft 2020-12 或生产。 | `CONFIRMED` | `SATISFIED_BY_LOCAL_LAB` | `03-E07` |

## Source Manifest

所有网页均于 `2026-08-20（Asia/Shanghai）` 实际打开。OpenAI 事实只来自当前官方 OpenAI Docs；版本敏感字段、模型支持与 product contract 以后必须重新核对。

| Source ID | Primary Source | Used For | Retrieved / Version Scope | Does Not Prove |
|---|---|---|---|---|
| `OA-SO` | [OpenAI Structured model outputs](https://developers.openai.com/api/docs/guides/structured-outputs) | Structured Outputs / JSON mode、supported subset、`required` / `additionalProperties` Provider rule、refusal、incomplete | `2026-08-20`；OpenAI current Responses / Chat Completions guide | 不证明其他 Provider 相同，也不证明内容事实正确 |
| `OA-RESP` | [OpenAI Responses API reference](https://developers.openai.com/api/reference/resources/responses/methods/create) | `text.format` 的 `json_schema` / `json_object` 职责 | `2026-08-20`；current OpenAI API reference | 不证明本 Lab 调用了 API |
| `JS-2020` | [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12) | 当前固定规范入口、dialect / vocabulary 路由 | Published `2022-06-16`；retrieved `2026-08-20` | 不证明某个 validator 完整实现该 dialect |
| `JS-CORE` | [JSON Schema Core 2020-12](https://json-schema.org/draft/2020-12/json-schema-core) | assertions、instance 只因已声明 assertion 失败、`$schema` dialect、`properties` / `additionalProperties` | Draft 2020-12；retrieved `2026-08-20` | 不证明 domain facts |
| `JS-VAL` | [JSON Schema Validation vocabulary 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation) | `type`、`enum`、`required`、format annotation / assertion 边界 | Draft 2020-12；retrieved `2026-08-20` | 不证明所有 implementation 启用 optional format assertion |
| `MS-STJ` | [System.Text.Json namespace](https://learn.microsoft.com/en-us/dotnet/api/system.text.json?view=net-10.0) | parse / deserialize / `JsonException` 基线 | `.NET 10` view；retrieved `2026-08-20` | 不提供完整 JSON Schema validation |
| `MS-PARSE` | [JsonDocument.Parse](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument.parse?view=net-10.0) | 单一合法 JSON value 与 parse failure | `.NET 10` view；retrieved `2026-08-20` | 不区分截断根因，不验证 schema |
| `MS-STRICT` | [.NET 10 library changes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/libraries) | duplicate-property rejection、`JsonSerializerOptions.Strict` | `.NET 10`；retrieved `2026-08-20` | 不证明 schema 与 DTO 自动同步 |
| `MS-REQ` | [Required properties](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/required-properties) | `required` / `[JsonRequired]` missing-property behavior | .NET 7+ / 9+ details；retrieved `2026-08-20` | 不等于 JSON Schema `required` implementation |
| `MS-UNMAP` | [Unmapped members](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members) | unknown property 默认与 `Disallow` 差异 | .NET 8+；retrieved `2026-08-20` | 不替代 schema dialect validation |
| `NJS-NUGET` | [NJsonSchema 11.6.1](https://www.nuget.org/packages/NJsonSchema/11.6.1) | fixed package、target、dependency、MIT package metadata | Version `11.6.1`, updated `2026-04-20`, retrieved `2026-08-20` | 不证明 Draft 2020-12 完整兼容 |
| `NJS-REL` | [NJsonSchema v11.6.1 release](https://github.com/RicoSuter/NJsonSchema/releases/tag/v11.6.1) | 固定 release；避开 v11.6.0 required regression | Tag `v11.6.1`, commit `ac2ba4a`, retrieved `2026-08-20` | 不证明本地 restore / run 已成功 |
| `NJS-API` | [NJsonSchema v11.6.1 JsonSchema.cs](https://github.com/RicoSuter/NJsonSchema/blob/v11.6.1/src/NJsonSchema/JsonSchema.cs) | `FromJsonAsync(string)`、`Validate(string)` API | Tag `v11.6.1`；retrieved `2026-08-20` | 不提供 Lab observation |
| `NJS-LIC` | [NJsonSchema MIT License](https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md) | 许可证核对 | MIT；retrieved `2026-08-20` | 不覆盖传递依赖的独立 notices；Lab Engineer仍需保留 lockfile |

## Local Evidence Manifest

| Evidence ID | Artifact | Observed Fact | Does Not Prove |
|---|---|---|---|
| `LAB-EXEC` | [Execution record](../../labs/lab-01-structured-output-validation/artifacts/logs/execution.md) | SDK `10.0.301`；最终 build `0 warnings / 0 errors`；tests `5/5`；两次 runner exit `0`；Provider calls、credential reads、automatic repair attempts 均为 `0` | 不证明未被 fixture 观测的环境或生产行为 |
| `LAB-OBS-1` | [First observation](../../labs/lab-01-structured-output-validation/artifacts/observation-first.jsonl) | 8 行、8 个唯一 case、1 个 accepted；每行保留 raw hash、stage trace、code、action | 不证明输入来自真实模型或 Provider envelope |
| `LAB-OBS-2` | [Second observation](../../labs/lab-01-structured-output-validation/artifacts/observation.jsonl) | 与第一次 SHA-256 同为 `C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`，byte-for-byte 相同 | 不证明跨 runtime / package / OS 的 determinism |
| `LAB-LOCK-ROOT` | [Runner package lock](../../labs/lab-01-structured-output-validation/packages.lock.json) | `NJsonSchema` direct resolved version=`11.6.1`，并记录传递依赖与 content hash | 不证明其他 package graph 或未来 restore 相同 |
| `LAB-LOCK-TEST` | [Test package lock](../../labs/lab-01-structured-output-validation/tests/StructuredOutputValidation.Tests/packages.lock.json) | test SDK、xUnit、project dependency 与 NJsonSchema graph 已锁定 | 不证明测试覆盖所有输入空间 |
| `LAB-ENV` | [dotnet --info](../../labs/lab-01-structured-output-validation/artifacts/logs/dotnet-info.txt) | Windows `10.0.19045`、win-x64、SDK `10.0.301`、Host `10.0.9` | 不证明其他环境兼容 |

## Evidence Cards

### Evidence 03-E01｜自然语言 JSON、JSON mode 与 Structured Outputs 不是同一合同

- Claim ID：`03-C01`
- Evidence Status / Class：`CONFIRMED / CURRENT_PROVIDER_OFFICIAL_DOC`
- Source：`OA-SO`、`OA-RESP`
- Retrieved / Version Scope：`2026-08-20（Asia/Shanghai）`；OpenAI current Structured Outputs / Responses contract
- Observation：OpenAI 当前文档把 JSON mode 描述为 valid JSON contract，但明确不保证指定 schema；`json_schema` Structured Outputs 才提供对所给 schema 的 adherence，并只支持 JSON Schema 子集。当前 Responses 表面使用 `text.format`；当前 guide 还要求 root object、所有字段 `required`、`additionalProperties: false` 等 Provider-specific subset rules。
- Counter-evidence / Failure Case：JSON mode 页面同时要求 Application 处理不完整 JSON 等 edge cases；Structured Outputs 也可能返回 refusal / incomplete，而不是普通 schema-shaped output。
- Interpretation：可迁移边界是“输出强度不同、应用仍处理 envelope”；字段、支持模型、subset 与 failure metadata 是 Provider / version contract。
- Proves：正文可区分 Prompt format instruction、JSON mode、Structured Outputs 与 typed helper。
- Does Not Prove：不证明所有 Provider 都有 JSON mode；不证明 Structured Outputs 的字段值真实、Evidence 存在或业务正确。
- Limitations / Course Usage：必须保留 OpenAI 核对日期；不在本篇比较 Provider 准确率、成本或可靠性。
- Lab Traceability：Lab 不调用 OpenAI；此卡是 docs-only。

### Evidence 03-E02｜Schema Valid 只覆盖声明的结构断言

- Claim ID：`03-C02`
- Evidence Status / Class：`CONFIRMED / FORMAL_SPEC`
- Source：`JS-2020`、`JS-CORE`、`JS-VAL`
- Retrieved / Version Scope：JSON Schema Draft 2020-12，retrieved `2026-08-20`
- Observation：Core 定义 assertion 产生 boolean result，并明确 instance 只能因 schema 中存在的 assertion 失败。Validation vocabulary 定义 `type`、`enum`、`required`；Core applicator 定义 `properties` / `additionalProperties`。`format` 在默认 dialect 中至少是 annotation，format assertion vocabulary 是 optional。
- Counter-evidence / Failure Case：`{"evidence_ids":["EV-999"]}` 可以满足 string-array schema，却仍引用不存在的实体；schema 没有 Application 的 Evidence registry 就不能判定该事实。
- Interpretation：Schema Validation 是结构 / 表达合同层；Domain Validation 消费 Application facts 与跨字段不变量。
- Proves：可写 `Parse Success != Schema Valid != Domain Valid != Verified Result`。
- Does Not Prove：不证明某个 validator 实现全部 vocabulary；不证明内容忠实、权限成立、Tool 已执行或结果已验证。
- Limitations / Course Usage：正文必须标 dialect / validator / schema；不要把 OpenAI subset rule 写成 JSON Schema 普遍规则。
- Lab Traceability：`nonexistent-evidence` 已在固定本地 Lab 中通过 Parse / Schema / DTO 后以 `DOMAIN_FAILED / UNKNOWN_EVIDENCE_ID` 结束；这只观察到当前 allowlist 的 domain gap，不证明真实 Evidence truth。

### Evidence 03-E03｜Parse、Schema、Typed DTO、Domain 的实际分层

- Claim ID：`03-C03`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_RUNTIME_SURFACE + LOCAL_LAB`
- Source：`MS-STJ`、`MS-PARSE`、`MS-STRICT`、`MS-REQ`、`MS-UNMAP`、`LAB-EXEC`、`LAB-OBS-1`、`LAB-OBS-2`
- Retrieved / Version Scope：`.NET SDK 10.0.301`、`NJsonSchema 11.6.1`、frozen Lab 01；executed `2026-08-20`
- Observation：八个 case 都输出 Parse / Schema / DTO / Domain 状态。三个 parse failure 的后三层全为 `NOT_RUN`；三个 schema failure 的 DTO / Domain 为 `NOT_RUN`；`nonexistent-evidence` 通过 Parse、Schema、DTO 后在 Domain 失败；`valid-accepted` 四层全通过。5/5 tests 同时覆盖 schema / DTO parity 与 first-failure short-circuit。
- Counter-evidence / Failure Case：冻结矩阵没有 `DTO_FAILED` terminal case，因此当前只观察到 DTO 在两个 schema-valid case 中通过；NJsonSchema 仍会使用自己的 JSON reader，不能据此宣布两个 parser 对所有输入等价。
- Interpretation：固定实现保留了冻结链路的首失败层，且没有把未运行的较晚层伪装为通过。
- Proves：仅证明本 fixture 的八个输入在固定 runtime / schema / package / DTO / allowlist 下按预期分层。
- Does Not Prove：不证明任意 JSON、其他 validator / dialect、Provider output 或生产 pipeline 都有相同行为。
- Limitations / Course Usage：正文可用作最小分层案例；必须同时写明没有独立 DTO-failure fixture 与泛化边界。
- Lab Traceability：`LAB-EXEC`、`LAB-OBS-1`、`LAB-OBS-2`。

### Evidence 03-E04｜八类 Fixture 的观察结果

- Claim ID：`03-C04`
- Evidence Status / Class：`CONFIRMED / FROZEN_MATRIX + LOCAL_LAB`
- Source：[Lab 01 Design / Observation](../../labs/lab-01-structured-output-validation/README.md)、`LAB-EXEC`、`LAB-OBS-1`、`LAB-OBS-2`
- Retrieved / Version Scope：Design frozen and executed `2026-08-20`；fixed eight-case matrix
- Observation：两次 runner 都写入 8 行、8 个唯一 case ID、1 个 accepted。terminal stage 计数均为 `ACCEPTED=1 / PARSE_FAILED=3 / SCHEMA_FAILED=3 / DOMAIN_FAILED=1`；全部 stage、code 与 action 命中冻结矩阵。两份 JSONL hash 相同且 byte-for-byte comparison=`True`。
- Counter-evidence / Failure Case：这些输入是课程 synthetic fixtures；`truncated-json` 与 `synthetic-refusal-text` 的类别来自运行前 metadata，不是由 raw text 证明的 Provider 原因。
- Interpretation：Expected 与 Observed 独立保存后完全匹配，因此八类固定 fixture 的本地行为可以确认。
- Proves：确认本 fixture 对 accepted、parse、schema、domain 四种 terminal outcome 的冻结映射与 deterministic artifact。
- Does Not Prove：不证明真实模型输出分布、Provider refusal / incomplete 行为、所有 JSON Schema keyword 或生产可靠性。
- Limitations / Course Usage：仅把八个 case 写成可复现教学证据，不把 synthetic input 改写成线上 observation。
- Lab Traceability：`LAB-EXEC`、`LAB-OBS-1`、`LAB-OBS-2`，共同 SHA-256=`C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`。

### Evidence 03-E05｜Refusal、Truncation 与 Repair / Retry 边界

- Claim ID：`03-C05`
- Evidence Status / Class：`CONFIRMED / CURRENT_PROVIDER_DOC + LOCAL_POLICY_FIXTURE`
- Source：`OA-SO`、[Lab 01 Design / Observation](../../labs/lab-01-structured-output-validation/README.md)、`LAB-EXEC`、`LAB-OBS-1`、`LAB-OBS-2`
- Retrieved / Version Scope：OpenAI current contract + fixed local classifier，`2026-08-20`
- Observation：OpenAI 当前示例先判断 incomplete metadata，再把 `refusal` 与 output text 分开。本地运行中，ordinary malformed input 得到 `UPSTREAM_RETRY_ELIGIBLE`，synthetic truncated input 得到 `UPSTREAM_CAUSE_REQUIRED`，synthetic refusal text 得到 `STOP_NON_CONTRACT_INPUT`，domain failure 得到 `STOP_AND_RECHECK_DOMAIN_INPUT`；所有八行 automatic repair attempts 均为 `0`。
- Counter-evidence / Failure Case：不同 action 依赖冻结的 `declared_input_class`；三种 parse-failed raw string 本身都只产生 `INVALID_JSON`。本 Lab 没有 Provider envelope、真实 refusal、真实 truncation 或 repaired candidate。
- Interpretation：固定本地 policy 能把“可交给上游决策”“必须补原因”“立即停止”分开，同时拒绝伪造模型 repair；Provider 原因仍必须来自 envelope / trace。
- Proves：确认本 fixture 的 local decision mapping 与 zero automatic repair behavior；docs-only 部分确认 OpenAI current envelope 分支。
- Does Not Prove：不证明 retry success、模型 repair、真实 truncation 原因、真实 refusal 发生或跨 Provider taxonomy。
- Limitations / Course Usage：正文必须把 `recommended_action` 写成 policy label，而不是已执行的 retry / repair；synthetic metadata 只能作为分类示例。
- Lab Traceability：`invalid-json`、`truncated-json`、`synthetic-refusal-text`、`nonexistent-evidence` in `LAB-OBS-1 / LAB-OBS-2`。

### Evidence 03-E06｜Structured Output 是前置合同，不是 Tool / Evidence / Eval

- Claim ID：`03-C06`
- Evidence Status / Class：`CONFIRMED / OFFICIAL_DOC + COURSE_DEPENDENCY_SYNTHESIS`
- Source：`OA-SO`、`JS-CORE`、Article 01、Article 02、canonical Article 05 / 18 / 22 路由
- Retrieved / Version Scope：`2026-08-20` current docs + repository canonical
- Observation：OpenAI 当前 guide 把“连接 tools / functions / data”路由到 function calling，把面向用户回复的结构路由到 structured `text.format`。JSON Schema 只评价声明的 assertions。Article 02 已把 Output Requirement 与 permission / facts / validation 分开。
- Counter-evidence / Failure Case：一个 schema-valid `{status, evidence_ids}` 仍可能引用不存在的 Evidence；它也没有执行任何 Tool 或评价任务质量。
- Interpretation：结构合同为后续系统提供 typed input / result 与 failure labels，但后续系统仍有独立 runtime / truth / evaluator 责任。
- Proves：本篇可桥接 Tool Call、Evidence Contract 与 Eval，同时明确不替代它们。
- Does Not Prove：不定义 Article 05 Function Calling wire schema、Article 18 Evidence truth 或 Article 22 grader。
- Limitations / Course Usage：只在相邻文章边界处简述，不提前写完后续课程。
- Lab Traceability：`nonexistent-evidence` 已作为 schema-valid / domain-invalid 的本地最小反例；它不证明真实 Evidence、Tool execution 或 evaluator completion。

### Evidence 03-E07｜Lab 01 的最终证明范围

- Claim ID：`03-C07`
- Evidence Status / Class：`CONFIRMED / PINNED_DEPENDENCY + LOCAL_REPRODUCTION`
- Source：`NJS-NUGET`、`NJS-REL`、`NJS-API`、`NJS-LIC`、`LAB-EXEC`、`LAB-LOCK-ROOT`、`LAB-LOCK-TEST`、`LAB-ENV`、`LAB-OBS-1`、`LAB-OBS-2`
- Retrieved / Version Scope：`.NET SDK 10.0.301`、Host `10.0.9`、Windows `10.0.19045`、`NJsonSchema 11.6.1`、frozen Draft 4 subset；executed `2026-08-20`
- Observation：root lockfile 将 direct `NJsonSchema` resolved version 固定为 `11.6.1` 并保存 content hash；test lockfile 保存 test graph。approved identical restore 与 locked restore 最终成功，final build 为 `0 warnings / 0 errors`，tests `5/5`，两次 runner exit `0` 且产物 byte-identical。
- Counter-evidence / Failure Case：sandboxed restore 首次因 NuGet socket access 返回 `NU1301`；初始 build 因测试源缺 `using Xunit;` 返回 10 个 `CS0246`。两者及局部 disposition 均保留在 execution log，最终 clean verification 通过且未改变冻结 Design / fixture / expected。
- Interpretation：依赖、环境、命令与 raw artifacts 足以复现并确认当前固定 Lab；早期环境/实现失败没有被抹去，也没有被误写为 Claim 失败或 Provider failure。
- Proves：只确认固定 runtime、package graph、schema、DTO、八个 fixtures 与 allowlist 的本地运行结果。
- Does Not Prove：不证明 NJsonSchema 完整支持 Draft 2020-12、不证明其他 OS / runtime / package 版本、不证明 Provider adherence、模型质量、事实正确性或生产可靠性。
- Limitations / Course Usage：任何版本、schema、fixture、allowlist 或 runtime 变化都必须作为新实验条件重新执行；课程引用必须保留 hash 与环境范围。
- Lab Traceability：`LAB-EXEC`、`LAB-LOCK-ROOT`、`LAB-LOCK-TEST`、`LAB-ENV`、`LAB-OBS-1`、`LAB-OBS-2`。

## Evidence Gate Checklist

- [x] 7 个 Research Questions 已完成 Preliminary Answer
- [x] 7 个核心 Claim 均有 Evidence Card
- [x] 当前 OpenAI 官方页已实际打开，Provider-specific contract 有 retrieved / scope
- [x] JSON Schema specification / vocabulary 与 domain boundary 已分开
- [x] Microsoft `System.Text.Json` current runtime surface 已核对
- [x] `NJsonSchema 11.6.1` package / version / MIT license / API 已固定
- [x] Validator dialect 降为 Draft 4 shared-keyword subset，未宣称 Draft 2020-12 full compliance
- [x] JsonSchema.Net 因 binary EULA / maintenance-fee ambiguity 被排除
- [x] 4 个 Lab-dependent Claim 已由固定本地 Lab artifact 收窄确认
- [x] Lab 01 Design 已在运行前冻结
- [x] Expected Observable 与未来 Observed Result 分离
- [x] Lab restore / build / test / run / fault injection 已执行
- [x] raw observation / exit codes / failure output 已保存
- [x] Researcher Evidence Merge 已完成

## Evidence Merge Decision

- Claim Summary：`7 CONFIRMED / 0 PARTIAL / 0 BLOCKED`
- Lab Dependency：`SATISFIED_BY_FIXED_LOCAL_LAB`
- Evidence Gate：`PASS`
- Outcome：`OUTLINE_ALLOWED`
- Remaining Blockers：`NONE_AT_EVIDENCE_GATE`
- Next Action：`OUTLINE`

本次合并只把 fixed local fixture observation 升级为收窄后的本地 Claim；OpenAI Provider adherence、真实 refusal / truncation cause、模型 repair、Evidence 事实正确性、Tool 执行与生产泛化均未被升级。

## Stop Line

`EVIDENCE_MERGE` 与 Evidence Gate PASS 到此停止。下一动作是 `OUTLINE`；本角色不创建或修改 Outline、Draft、Review、Published Content、Article README、`status.md`、`course-run-state.md` 或 canonical，不重新执行 Lab，也不 commit / push。
