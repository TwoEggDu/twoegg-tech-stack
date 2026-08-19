# Structured Output：让模型输出成为机器可消费的合同

> 本文资料与 API contract 核对时间：2026-08-20。OpenAI 的字段、支持模型与 JSON Schema 子集可能继续变化，接入时应重新核对当前 Provider、API、模型和版本。本文没有调用 Provider；文中的运行结果只来自固定的本地 C# / .NET Lab。

上一篇把 Prompt 拆成了 Goal、Constraints、Inputs、Examples、Output Requirements 与 Failure Semantics。Application 已经能说清“请返回 `status`、`summary` 和 `evidence_ids`”，但这仍然只是自然语言中的输出要求。

假设模型返回下面这段内容：

```json
{
  "status": "SUPPORTED",
  "summary": "CS0103 at BuildMenu.cs:42",
  "evidence_ids": ["EV-999"]
}
```

它看起来很规整，程序却还不能直接宣布任务成功。至少还有几件事没有回答：

- 这是不是一个完整、合法的 JSON value？
- 必需字段是否齐全，字段类型与枚举值是否符合约定？
- C# 运行时能否把它严格映射成预期 DTO？
- `EV-999` 是否是 Application 当前认识的 Evidence ID？
- `summary` 是否忠于真实日志，相关 Tool 是否真的执行过？

这些问题分别站在不同边界。把它们压成一个“JSON 是否正确”，结果往往是：解析失败、结构错误、领域事实缺失和外部动作失败全部掉进同一个异常桶；或者更糟，程序看到一段合法 JSON 就继续产生副作用。

因此，本篇最重要的判断是：**Structured Output 把模型输出变成可解析、可分层拒绝的候选数据，但不会把候选数据变成事实。**

## 从“请输出 JSON”到机器合同，强度并不相同

先把几个经常被混称为“结构化输出”的东西分开。

| Contract Surface | 能提供什么 | 仍然缺什么 |
|---|---|---|
| Prompt 中的格式要求 | 表达调用方期望的字段与呈现方式 | 没有机器执行的 schema 校验 |
| JSON mode | 以合法 JSON 为目标 | 不保证满足调用方指定 schema |
| Structured Outputs | 在当前 Provider 支持的 JSON Schema 子集内提供结构合同 | 不证明字段值真实或业务有效 |
| Application Validation | Parse、Schema、DTO、Domain 分层判定 | 仍不替代 Tool、Evidence 与 Eval |

这里的 JSON mode 与 Structured Outputs 描述的是 2026-08-20 核对到的 OpenAI 当前公开 contract，不是跨 Provider 统一术语。OpenAI 当前文档还对 root object、所有字段列入 `required`、对象设置 `additionalProperties: false` 等提出自己的子集要求；这些规则不能反向写成 JSON Schema 的普遍定义。

更要紧的是：本文没有发送真实请求，也没有观测任何模型是否遵守 schema。官方文档能确认当前产品 contract，不能替代 runtime observation。接入另一家 Provider，仍要重新查它的请求字段、支持范围、refusal 与 incomplete 表达。

## 抽象模型：先处理 Envelope，再验证 Candidate

从 Application 的视角，一份 response 到最终接受，可以先拆成下面这条责任链：

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

Provider 已明确返回 request error、refusal 或 incomplete metadata 时，不应丢掉这些信息，把残缺文本伪装成普通 candidate。只有取得候选文本后，才进入本地验证链：

| Layer | 通过意味着什么 | 通过仍不能证明什么 |
|---|---|---|
| Provider / transport envelope | 当前响应中存在可消费 candidate，而不是已识别的 terminal state | candidate 正确、完整或可信 |
| Parse | raw string 是单一合法 JSON value | 字段齐全、类型或 schema 满足 |
| Schema Validation | instance 满足当前 schema 中实际声明的 assertions | 引用实体存在、内容忠实、权限或外部动作成立 |
| Typed Materialization | schema-shaped data 能映射到 Application 预期 DTO | 领域不变量成立或事实正确 |
| Domain Validation | DTO 满足当前 Application facts 与跨字段规则 | 外部 Evidence 自身真实、Tool 已执行或任务质量合格 |

还要保留首个失败层：Parse 失败后，Schema、DTO 与 Domain 必须是 `NOT_RUN`；Schema 失败后也不能继续。这让上层能区分语法、结构、运行时绑定与领域输入问题。本图只是 Application 侧的最小合同，不推测 Provider 内部实现；streaming、Provider error 与 retry normalization 留给下一篇。

## JSON Schema 只判断声明过的结构断言

JSON Schema Draft 2020-12 Core 中，assertion 对 instance 产生通过或失败；instance 也只能因 schema 中实际存在的 assertion 失败。因此，“schema valid”至少要同时说清：

1. 使用哪个 dialect；
2. 使用哪个 validator 与版本；
3. 验证的是哪一份 schema，以及它声明了哪些 vocabulary / keywords。

`type`、`enum`、`required` 与 `additionalProperties: false` 能守住类型、值集合、属性存在与未声明字段，却不知道 Application 的 Evidence registry 里有哪些实体。

Lab 01 使用的最小合同可以压缩成下面这样：

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "additionalProperties": false,
  "required": ["status", "summary", "evidence_ids"],
  "properties": {
    "status": {
      "type": "string",
      "enum": ["SUPPORTED", "INSUFFICIENT_EVIDENCE"]
    },
    "summary": { "type": "string", "minLength": 1 },
    "evidence_ids": {
      "type": "array",
      "items": { "type": "string", "minLength": 1 }
    }
  }
}
```

实验明确使用 Draft 4 与上述冻结关键词。Draft 2020-12 规范用于解释通用 assertion / vocabulary 边界，不代表 NJsonSchema 11.6.1 完整实现 Draft 2020-12。尤其 `format` 在默认 dialect 中至少是 annotation，assertion vocabulary 仍是可选能力，必须核对 validator。

现在回到开头的两个候选：

```json
{"status":"SUPPORTED","summary":"compiler error","evidence_ids":["EV-001"]}
```

```json
{"status":"SUPPORTED","summary":"compiler error","evidence_ids":["EV-999"]}
```

两者都可能通过 schema；固定 allowlist `{EV-001, EV-002}` 只接受前者。**Schema 负责形状，Domain 负责 Application 知道的业务事实。** allowlist 接受 `EV-001` 仍不证明 `summary` 忠于日志或 Tool 已执行。

## 落到 C#：Schema、DTO 与 Domain 是三套需要对齐的合同

直接把 `Deserialize<T>()` 成功当作业务成功，会丢失 Parse、Schema、DTO 与 Domain 的失败边界。Lab 01 的 `DiagnosisCandidate` 只有三个字段：

```csharp
public sealed record DiagnosisCandidate(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("evidence_ids")] IReadOnlyList<string> EvidenceIds);
```

Schema 通过后，Lab 才用 `JsonSerializerOptions.Strict` materialize DTO。Schema 与 DTO 需要测试对齐，却不会自动同步；JSON Schema `required` 和 C# 构造参数 / required / nullability 是不同合同表面。

最小执行骨架不需要复制完整实现，可以写成：

```text
parse(raw)                         or fail(PARSE_FAILED)
schema.validate(raw)               or fail(SCHEMA_FAILED)
dto = strict_materialize(raw)      or fail(DTO_FAILED)
domain.validate(dto, allowlist)    or fail(DOMAIN_FAILED)
accept(dto)
```

在 Lab 中，Domain 规则是：

- `status == SUPPORTED` 时，`evidence_ids` 至少有一个值，且每个 ID 都在 allowlist 中；
- `status == INSUFFICIENT_EVIDENCE` 时，`evidence_ids` 必须为空；
- `summary` 非空由 schema 检查，但本 Lab 不验证它的事实真实性。

每个 `or fail` 保存 terminal stage、code 与 action，后续层为 `NOT_RUN`。`DTO_FAILED` 是正式分支，但八个 case 没有独立 DTO terminal case；观测只能说明两个 schema-valid case 成功 materialize。NJsonSchema 11.6.1 只是固定实验依赖，不代表“最佳 validator”；换条件就要重新验证。

## 同样是 `INVALID_JSON`，失败原因与恢复动作仍然不同

普通 malformed JSON、fixture 标记的 truncated JSON，以及 `I cannot provide that result.` 在本地 parser 看来都只是 `INVALID_JSON`。真实原因应来自可信 Provider envelope 或 trace，不能由字符串外观猜测。OpenAI 当前文档把 incomplete metadata 与 refusal 放在普通结构化 output 之外处理；本文只据此保留 envelope 分支，没有执行 Provider call。Lab 则只根据冻结的 `declared_input_class` 输出本地 action label，并不执行 action。

| Observed / Declared Class | Local Result | Recommended Action | 这不表示什么 |
|---|---|---|---|
| 普通 malformed JSON | `PARSE_FAILED / INVALID_JSON` | `UPSTREAM_RETRY_ELIGIBLE` | 已经重试或必然成功 |
| Schema failure | `SCHEMA_FAILED` | `UPSTREAM_RETRY_ELIGIBLE` | 模型能够 repair |
| Domain failure | `DOMAIN_FAILED` | `STOP_AND_RECHECK_DOMAIN_INPUT` | 可以靠补字符串制造事实 |
| synthetic truncated fixture | `PARSE_FAILED / INVALID_JSON` | `UPSTREAM_CAUSE_REQUIRED` | 已观察真实 token truncation |
| synthetic non-contract text | `PARSE_FAILED / INVALID_JSON` | `STOP_NON_CONTRACT_INPUT` | 已观察真实 Provider refusal |
| accepted fixture | `ACCEPTED` | `ACCEPT` | 内容、Evidence 或任务质量已验证 |

`UPSTREAM_RETRY_ELIGIBLE` 不等于已重试；automatic repair attempts 固定为 `0`，没有 repaired candidate。Domain failure 也不能靠把 `EV-999` 改成 `EV-001` 来“修复”，那只会伪造引用。Provider error、streaming、retry、backoff 与 normalization 留给下一篇。

## Lab 01：从冻结判据到原始观测

只有示例代码和 expected output 还不是 Lab evidence。Lab 01 按 Design、Expected、Observed、Interpretation 保存完整链路。

### Design：先冻结实验条件

运行前固定 Windows / `.NET SDK 10.0.301`、`NJsonSchema 11.6.1` 与 lockfiles、Draft 4 最小关键词 schema、`DiagnosisCandidate` DTO、allowlist `{EV-001, EV-002}`、八个 exact raw strings、culture=`InvariantCulture` 以及 automatic repair attempts=`0`。Lab 不需要 Provider 或 credentials，不读取在线输入，也不生成或修补 candidate。

假设同样先冻结：valid input 应被接受；malformed / synthetic truncated / synthetic non-contract text 停在 Parse；缺字段、类型错误和额外字段停在 Schema；未知 Evidence ID 停在 Domain。任一 invalid case 被接受、stage / code 不匹配或 early failure 后仍运行 later stage，都会使 Lab 失败。

### Expected：八类输入先定义成功与失败判据

下面是运行前冻结的矩阵，不是运行结果：

| Case | Expected Stage | Expected Code | Expected Action |
|---|---|---|---|
| `valid-accepted` | `ACCEPTED` | `NONE` | `ACCEPT` |
| `invalid-json` | `PARSE_FAILED` | `INVALID_JSON` | `UPSTREAM_RETRY_ELIGIBLE` |
| `missing-required` | `SCHEMA_FAILED` | `REQUIRED` | `UPSTREAM_RETRY_ELIGIBLE` |
| `wrong-type` | `SCHEMA_FAILED` | `TYPE` | `UPSTREAM_RETRY_ELIGIBLE` |
| `extra-property` | `SCHEMA_FAILED` | `ADDITIONAL_PROPERTY` | `UPSTREAM_RETRY_ELIGIBLE` |
| `nonexistent-evidence` | `DOMAIN_FAILED` | `UNKNOWN_EVIDENCE_ID` | `STOP_AND_RECHECK_DOMAIN_INPUT` |
| `truncated-json` | `PARSE_FAILED` | `INVALID_JSON` | `UPSTREAM_CAUSE_REQUIRED` |
| `synthetic-refusal-text` | `PARSE_FAILED` | `INVALID_JSON` | `STOP_NON_CONTRACT_INPUT` |

每行还必须保存 raw SHA-256、四层状态、terminal stage、error codes、action 和 repair count；early failure 后的层必须为 `NOT_RUN`。

### Observed：保存成功，也保存过程中真实发生的失败

执行记录保留了非绿色过程：首次 sandboxed restore 与 locked restore 因 NuGet socket access 受限返回 `NU1301`；相同命令取得权限后重跑为 `0`。初始 build 因 test source 缺少 `using Xunit;` 产生 10 个 `CS0246`；局部修复未改变 Design、schema、fixture 或 Expected，最终 build 为 `0 warnings / 0 errors`。

最终 tests=`5 / 5`，分别覆盖 schema / DTO parity、Domain rules、八类 matrix、first-failure short-circuit 和 automatic repair attempts=`0`。runner 以相同命令执行两次，exit code 都为 `0`；每次得到八行、八个唯一 case，只有一个 accepted：

| Terminal Stage | Observed Count | Cases |
|---|---:|---|
| `ACCEPTED` | 1 | `valid-accepted` |
| `PARSE_FAILED` | 3 | `invalid-json`、`truncated-json`、`synthetic-refusal-text` |
| `SCHEMA_FAILED` | 3 | `missing-required`、`wrong-type`、`extra-property` |
| `DOMAIN_FAILED` | 1 | `nonexistent-evidence` |

三条 trace 覆盖了首失败层：`invalid-json` 在 Parse 后三层均为 `NOT_RUN`；`extra-property` 在 Schema 后 DTO / Domain 为 `NOT_RUN`；`nonexistent-evidence` 则通过 Parse / Schema / DTO 后在 Domain 失败。

两份 JSONL 的 SHA-256 均为：

```text
C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8
```

byte-for-byte comparison=`True`。这只说明两次固定本机运行生成了相同 artifact，不证明跨 runtime、package、OS 或输入的 determinism。Provider calls、credential reads 与 automatic repair attempts 均为 `0`；两个 synthetic class 仍只是 fixture metadata。

### Interpretation：只把结论升级到实验真正覆盖的范围

Expected 与 Observed 命中后，可确认的只有：固定 `.NET SDK 10.0.301 + NJsonSchema 11.6.1 + Draft 4 subset + DTO + eight fixtures + allowlist` 下，本地 pipeline 保留首失败层，八类 stage / code / action 符合矩阵，两次运行生成 byte-identical JSONL。

它不证明 Provider runtime adherence、真实 refusal / truncation 原因、model repair 或 retry success；不证明 NJsonSchema 完整支持 Draft 2020-12或跨环境 determinism；也不证明真实 summary / Evidence、Tool execution、权限、Eval 或生产可靠性。Lab 的价值是留下可复查边界，不是给 Structured Output 打“可靠”标签。

## 工程边界：机器可消费不是可信、可执行或已评测

Structured Output 是很多后续系统的前置，但不是它们的替代品。

| Structured Output 提供的接口 | 独立系统仍需负责 | 课程边界 |
|---|---|---|
| typed Tool arguments / result shape | Tool policy、permission、execute、timeout、side effect、trace | Article 05—06 |
| `evidence_ids` 等稳定字段 | Evidence existence、provenance、claim-to-source、verification | Article 18 |
| failure stage / error labels | dataset、grader、metric、golden set、regression | Article 21—22 |
| local candidate acceptance | Provider normalization、streaming completion、retry / backoff | Article 04 |

即使一份结果通过 Domain Validation，也只能说明当前 Application rules 接受这个 DTO。如果 allowlist 已经过期，`summary` 与输入日志不符，或者外部 Tool 从未执行，结果仍然可能不可信。

下面几种推理都应该被拒绝：

```text
schema valid              -> answer true
recommended_action exists -> retry executed
evidence_ids present      -> Evidence verified
typed Tool arguments      -> Tool authorized / executed
8 / 8 fixtures pass       -> production reliable
```

Structured Output 真正提供的是一个稳定的交界面：后续系统不用再从自由文本里猜字段，可以接收 typed data 和明确 failure labels；与此同时，它们仍要承担自己的执行、事实与质量责任。

## 怎样审查一条 Structured Output Pipeline？

下一次看到一段“看起来正确”的 JSON，可以按下面顺序检查：

1. Provider envelope 表达的是 ordinary candidate、refusal、incomplete，还是 request error？
2. raw string 是否为单一合法 JSON value？
3. 使用的是哪个 dialect、validator / version 与 schema？
4. schema 和 DTO 是否显式对齐，materialization 是否严格？
5. 哪些 Application facts 与跨字段不变量仍需 Domain Validation？
6. failure 后的 action 是一个 label、一项提议，还是实际已经执行的动作？
7. 当前结论来自官方 contract、本地 fixture，还是 production observation？

上一篇解决的是“怎样让自然语言任务与 Output Requirement 可审查”；本篇解决的是“怎样让候选结果可解析、可分层拒绝”。下一篇 Model Adapter / Gateway 会继续接住 Provider envelope，处理 streaming、error、retry 与 Provider 差异。本篇到本地机器合同为止。

## Learning Check

1. 一段文本能被 `JsonDocument.Parse` 接受，是否已经是机器可消费的业务结果？还缺哪些层？
2. `evidence_ids` 是 string array 且值为 `EV-999`，为什么 Schema 可能通过、Domain 仍应失败？
3. 同样是 `INVALID_JSON`，为什么 ordinary malformed、synthetic truncated 与 synthetic refusal-like text 的 action 可以不同？
4. `UPSTREAM_RETRY_ELIGIBLE` 是否表示系统已经重试，或者模型一定能 repair？
5. Lab 01 的八类 case 全部命中 Expected，且两次 JSONL byte-identical，能证明什么，不能证明什么？
6. Structured Result 通过 Domain Validation 后，是否能宣布 Tool 已执行、Evidence 已验证、任务质量已合格？
7. 接入新 Provider 时，本篇哪些抽象可以保留，哪些字段与行为必须重新核对？

### 参考思路

1. 不能；仍需 Schema、DTO、Domain，并保留外部 truth / execution 边界。
2. Schema 只约束声明结构；ID 是否存在依赖 Application facts。
3. parse observation 可以相同；action 来自可信 envelope / declared metadata 与 policy，raw string 不能证明原因。
4. 都不是；它只是 label，Lab repair attempts=`0`。
5. 只证明固定本机与固定依赖 / inputs；不证明 Provider、其他环境或生产。
6. 不能；Tool Runtime、Evidence Contract 与 Eval 各有独立责任。
7. 分层责任可保留；envelope、schema subset、refusal / incomplete、streaming / error / retry contract 必须按当前 Provider / API / model / version 重查。

## 最短结论

`Structured Output 的价值不是让 JSON 看起来整齐，而是让程序知道何时接受、何时拒绝，以及自己仍然没有证明什么。`

## 参考资料

- [OpenAI：Structured model outputs](https://developers.openai.com/api/docs/guides/structured-outputs)
- [OpenAI：Responses API create reference](https://developers.openai.com/api/reference/resources/responses/methods/create)
- [JSON Schema：Draft 2020-12](https://json-schema.org/draft/2020-12)
- [JSON Schema：Core 2020-12](https://json-schema.org/draft/2020-12/json-schema-core)
- [JSON Schema：Validation vocabulary 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation)
- [Microsoft：System.Text.Json](https://learn.microsoft.com/en-us/dotnet/api/system.text.json?view=net-10.0)
- [Microsoft：JsonDocument.Parse](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument.parse?view=net-10.0)
- [Microsoft：.NET 10 library changes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/libraries)
- [Microsoft：Required properties](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/required-properties)
- [Microsoft：Unmapped members](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members)
- [NuGet：NJsonSchema 11.6.1](https://www.nuget.org/packages/NJsonSchema/11.6.1)
- [NJsonSchema：v11.6.1 release](https://github.com/RicoSuter/NJsonSchema/releases/tag/v11.6.1)
- [NJsonSchema：v11.6.1 JsonSchema API](https://github.com/RicoSuter/NJsonSchema/blob/v11.6.1/src/NJsonSchema/JsonSchema.cs)
- [NJsonSchema：MIT License](https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md)

### 本地实验资产

- [Lab 01 Design / Observation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-01-structured-output-validation/README.md)
- [Lab 01 Execution Record](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-01-structured-output-validation/artifacts/logs/execution.md)
- [First JSONL Observation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-01-structured-output-validation/artifacts/observation-first.jsonl)
- [Second JSONL Observation](https://github.com/TwoEggDu/twoegg-tech-stack/blob/main/docs/agent-engineering-course/labs/lab-01-structured-output-validation/artifacts/observation.jsonl)
