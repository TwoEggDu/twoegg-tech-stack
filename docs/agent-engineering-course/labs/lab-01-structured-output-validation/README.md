# Lab 01｜Structured Output Validation

本 Lab Card 同时保留运行前冻结的 `LAB_DESIGN` 与后续由 Lab Engineer 写入的 `LAB_OBSERVATION`。当前已完成 Lab 执行与 Researcher Evidence Merge；本文中的 Expected 不是 Observed，禁止在执行后反向修改 Expected 来适配结果。

## Metadata

- Lab ID：`Lab 01`
- Title：`Structured Output Validation`
- Owning Article：`Article 03｜Structured Output：让模型输出成为机器可消费的合同`
- Lifecycle Status：`EXECUTED / EVIDENCE_MERGED`
- Evidence Status：`PASS / EVIDENCE_MERGED`
- Runtime / Language：`C# / .NET 10`
- Fixture Version：`lab-01-design-v1`
- Environment：`Windows / .NET SDK 10.0.301`
- JSON Schema Validator：`NJsonSchema 11.6.1`
- Schema Dialect：`JSON Schema Draft 4，冻结关键词子集`
- Provider：`NONE`
- Credentials：`NOT_REQUIRED`
- Design Frozen At：`2026-08-20（Asia/Shanghai）`
- Last Run：`2026-08-20T00:42:30.7668366+08:00`

## Goal

用一组预置 candidate raw-output strings，真实执行一个最小、确定性的本地验证链：

```text
Raw Candidate
  -> JSON Parse
  -> JSON Schema Validate
  -> Strict Typed Materialize
  -> Domain Validate
  -> Accept / Fail At First Boundary
```

实验只回答：这条固定本地链路能否接受完整合法输入，并把 malformed JSON、结构合同违反与 domain 违反保留在不同失败层。它不调用模型，也不证明任何 Provider 的 schema adherence、refusal、truncation 或 repair 行为。

## Lab Design（Owner：Researcher）

- Related Article：[`Article 03`](../../articles/03-structured-output-machine-contract/README.md)
- Related Claim IDs：`03-C03`、`03-C04`、`03-C05`、`03-C07`
- Research Question：在固定 `.NET 10.0.301 + NJsonSchema 11.6.1 + Draft 4 subset` 下，本地 pipeline 是否能对八类预置 raw strings 输出唯一、可追踪的首个失败层，并且只接受 schema 与 domain 都通过的 candidate？
- Hypothesis：严格按本文实现时，valid case 被接受；malformed / truncated / synthetic refusal text 停在 Parse；missing required、wrong type、extra property 停在 Schema；不存在的 Evidence ID 停在 Domain；任何较晚阶段不得在较早阶段失败后继续运行。
- What Would Falsify It：任一 case 的实际 terminal stage 或 error code 与冻结判据不一致；valid case 被拒绝；任一 invalid case 被接受；较早层失败后仍执行较晚层；schema 与 DTO 的字段合同不一致；输出缺失 raw hash / stage trace；fixture 隐式依赖 Provider、凭证、网络或作者机器状态。
- Fixture Boundary：只允许 Lab Engineer 在本 Lab 目录内创建 solution、source、tests、fixtures、package lock 与 artifacts；只读取预置 raw strings 和固定 Evidence allowlist；不读取文章外部业务数据；不调用 Provider；不生成或修补 candidate 内容。
- Environment：Windows PowerShell；本机基线 `.NET SDK 10.0.301`；目标框架 `net10.0`；执行前记录 `dotnet --info`；版本变化视为新的实验条件。
- Package / Version / License：固定 `NJsonSchema 11.6.1`，NuGet package metadata 与 GitHub repository license 均为 MIT；固定 API 为 `JsonSchema.FromJsonAsync(string)` 与 `schema.Validate(string)`；必须提交 lockfile 并保留依赖 notices。明确排除 JsonSchema.Net，因为其 source license 与 compiled NuGet binary EULA / commercial maintenance 条件存在本 Lab 不接受的复用歧义。
- Inputs：固定 schema、固定 DTO、固定 Evidence allowlist `{EV-001, EV-002}`、下表八个 UTF-8 raw strings；不使用随机数、当前时间或在线输入。
- Variables：自变量仅为 `case_id`、`declared_input_class` 与 raw string；控制变量为 runtime、package、schema、DTO、allowlist、culture=`InvariantCulture`、automatic repair attempts=`0`；因变量为各层状态、terminal stage、error codes、recommended action 与 raw SHA-256。
- Expected Observable：每个 case 输出一行稳定 JSONL，字段至少含 `case_id`、`declared_input_class`、`raw_sha256`、`parse_status`、`schema_status`、`dto_status`、`domain_status`、`terminal_stage`、`error_codes`、`recommended_action`；未运行的较晚层必须写 `NOT_RUN`，不得伪装成通过。
- Fault Injection：通过固定 raw strings 注入 syntax error、missing required、wrong type、additional property、unknown Evidence ID、truncation 与 synthetic non-contract text；不注入真实 Provider 故障。
- Commands / Execution Needs：见 [Run Instructions](#run-instructions)。Restore 是唯一允许联网的步骤；locked restore 完成后，build / test / run 不得访问网络。
- Acceptance Criteria：见 [Acceptance Criteria](#acceptance-criteria)。任何一项不满足，Lab 结论必须为 `FAILED / CLAIMS_REMAIN_BLOCKED`，不得调低 Expected。
- Evidence Mapping：`03-C03 -> stage trace`；`03-C04 -> eight-case matrix`；`03-C05 -> recommended_action + declared metadata boundary`；`03-C07 -> environment, lockfile, commands, raw artifact`。
- Limitations：只证明固定本地 fixture 的行为；NJsonSchema 在本 Lab 中只按明确 Draft 4 schema 与冻结关键词判定，不主张完整 Draft 2020-12 conformance；合成 raw strings 不是 model raw output；synthetic refusal text 不是 Provider refusal observation；truncated case 的原因标签来自 fixture metadata，不是从 raw string 可靠推断；Domain allowlist 不证明 summary 的事实正确；不覆盖并发、负载、安全对抗或生产重试。
- Safety / Permission Constraints：不得读取或要求任何 Provider key；不得调用 Provider；不得把环境变量值写入 trace；不得自动 repair、重试模型或改写 raw input；不得修改本 Lab 目录以外文件；不得把 Expected 写成 Observed。

## Frozen Contract

### JSON Schema

Lab 使用下列 Draft 4 schema。冻结关键词只有 `type`、`properties`、`required`、`enum`、`items`、`minLength`、`minItems` 与 `additionalProperties`；不把此实验外推为任意 dialect 或 vocabulary 的兼容测试。

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "DiagnosisCandidate",
  "type": "object",
  "additionalProperties": false,
  "required": ["status", "summary", "evidence_ids"],
  "properties": {
    "status": {
      "type": "string",
      "enum": ["SUPPORTED", "INSUFFICIENT_EVIDENCE"]
    },
    "summary": {
      "type": "string",
      "minLength": 1
    },
    "evidence_ids": {
      "type": "array",
      "items": {
        "type": "string",
        "minLength": 1
      },
      "minItems": 0
    }
  }
}
```

### Typed DTO

Schema 通过后才可用 `JsonSerializerOptions.Strict` materialize 为包含下列三个 JSON 字段的不可变 DTO；字段名、required/nullability 与 schema 必须有测试锁定：

```text
status       -> required non-null string
summary      -> required non-null string
evidence_ids -> required non-null IReadOnlyList<string>
```

Typed materialization 若失败，terminal stage 必须为 `DTO_FAILED`；它不是 schema failure，也不得继续进入 Domain。

### Domain Rules

- Evidence allowlist 固定为 `EV-001`、`EV-002`。
- `status == SUPPORTED` 时，`evidence_ids` 至少包含一个 ID，且每个 ID 都必须在 allowlist 中。
- `status == INSUFFICIENT_EVIDENCE` 时，`evidence_ids` 必须为空。
- `summary` 的非空条件由 schema 检查；本 Lab 不验证 summary 的事实真实性。

## Frozen Input Cases

表中的 Stage / Code / Action 是运行前判据，不是运行结果。

| Case ID | Declared Input Class | Exact Raw String | Expected Terminal Stage | Expected Code | Expected Action |
|---|---|---|---|---|---|
| `valid-accepted` | `CONTRACT_CANDIDATE` | `{"status":"SUPPORTED","summary":"CS0103 at BuildMenu.cs:42","evidence_ids":["EV-001"]}` | `ACCEPTED` | `NONE` | `ACCEPT` |
| `invalid-json` | `MALFORMED_JSON` | `{"status":"SUPPORTED",` | `PARSE_FAILED` | `INVALID_JSON` | `UPSTREAM_RETRY_ELIGIBLE` |
| `missing-required` | `CONTRACT_CANDIDATE` | `{"status":"SUPPORTED","evidence_ids":["EV-001"]}` | `SCHEMA_FAILED` | `REQUIRED` | `UPSTREAM_RETRY_ELIGIBLE` |
| `wrong-type` | `CONTRACT_CANDIDATE` | `{"status":"SUPPORTED","summary":"bad type","evidence_ids":"EV-001"}` | `SCHEMA_FAILED` | `TYPE` | `UPSTREAM_RETRY_ELIGIBLE` |
| `extra-property` | `CONTRACT_CANDIDATE` | `{"status":"SUPPORTED","summary":"extra","evidence_ids":["EV-001"],"confidence":0.9}` | `SCHEMA_FAILED` | `ADDITIONAL_PROPERTY` | `UPSTREAM_RETRY_ELIGIBLE` |
| `nonexistent-evidence` | `CONTRACT_CANDIDATE` | `{"status":"SUPPORTED","summary":"unknown id","evidence_ids":["EV-999"]}` | `DOMAIN_FAILED` | `UNKNOWN_EVIDENCE_ID` | `STOP_AND_RECHECK_DOMAIN_INPUT` |
| `truncated-json` | `SYNTHETIC_TRUNCATED` | `{"status":"SUPPORTED","summary":"truncated","evidence_ids":["EV-001"]` | `PARSE_FAILED` | `INVALID_JSON` | `UPSTREAM_CAUSE_REQUIRED` |
| `synthetic-refusal-text` | `SYNTHETIC_NON_CONTRACT_INPUT` | `I cannot provide that result.` | `PARSE_FAILED` | `INVALID_JSON` | `STOP_NON_CONTRACT_INPUT` |

`invalid-json` 与 `truncated-json` 的 raw-only Parse 结果都只能是 `INVALID_JSON`。不同 Action 依赖 fixture 预先声明的 `declared_input_class`；实际系统若没有 Provider envelope metadata，不得仅凭 raw string 猜测“截断”。同理，synthetic refusal text 只证明非 JSON 输入会被拒绝，不证明 OpenAI 或任何 Provider 返回了 refusal。

## Prerequisites

- `.NET SDK 10.0.301` 可用；Lab Engineer 执行时重新记录完整 `dotnet --info`。
- `NJsonSchema` 的 central package declaration 指定 `11.6.1`；提交的 root / test lockfiles 记录 resolved graph，精确依赖图由 lockfiles 与 `--locked-mode` 执行并验证。
- schema、DTO、allowlist 与八个 case 完全按本文冻结。
- Provider 与 credentials 均不需要；若实现尝试读取 Provider 配置，立即判为越界。
- Lab Engineer 在首次 restore 前核对 package source；restore 后使用 locked mode。

## Question and Claims

| Claim ID | 可判定问题 | 成功判据 | 失败判据 |
|---|---|---|---|
| `03-C03` | 四层 pipeline 是否保留首个失败层？ | 每个 case 都有完整 stage trace，早期失败后的层为 `NOT_RUN`。 | later stage 在 early failure 后运行，或错误层混淆。 |
| `03-C04` | 八类 fixture 是否符合冻结矩阵？ | 八个 case 的 terminal stage、code 与 action 全部精确匹配。 | 任一 mismatch、漏 case 或 invalid case 被接受。 |
| `03-C05` | 本地 decision 是否保持 refusal / truncation / repair 边界？ | automatic repair 为 0；action 明确区分 raw observation 与 declared metadata。 | 改写 raw、调用模型、把 raw-only 结果写成 Provider 原因。 |
| `03-C07` | 固定 runtime / package 下实际证明了什么？ | 保留环境、lockfile、commands、JSONL 与 hash，可复跑。 | 版本漂移、依赖未锁或缺少 raw artifact。 |

## Fixture

Lab Engineer 应在本目录内创建下列最小结构；文件名在设计阶段冻结，内容在实现与执行阶段补齐：

```text
lab-01-structured-output-validation/
├─ README.md
├─ StructuredOutputValidation.slnx
├─ Directory.Packages.props
├─ packages.lock.json
├─ schema/
│  └─ diagnosis-candidate.schema.json
├─ fixtures/
│  ├─ evidence-allowlist.json
│  └─ cases.json
├─ src/
│  └─ StructuredOutputValidation/
│     └─ StructuredOutputValidation.csproj
├─ tests/
│  └─ StructuredOutputValidation.Tests/
│     └─ StructuredOutputValidation.Tests.csproj
└─ artifacts/
   └─ observation.jsonl
```

`cases.json` 必须保存表中 exact raw strings 与 declared classes；runner 不得在读取后 normalize 或 repair raw。`artifacts/observation.jsonl` 不含当前时间，使相同环境的两次执行可以 byte-for-byte 对比；运行时间与环境另记在 Observations。

## Run Instructions

以下命令均从本 Lab 目录执行。Lab Engineer 可以记录 shell transcript，但不得改变命令语义或跳过 locked restore。

```powershell
dotnet --info
dotnet restore .\StructuredOutputValidation.slnx --use-lock-file
dotnet restore .\StructuredOutputValidation.slnx --locked-mode
dotnet build .\StructuredOutputValidation.slnx --configuration Release --no-restore
dotnet test .\StructuredOutputValidation.slnx --configuration Release --no-build --logger "console;verbosity=detailed"
dotnet run --project .\src\StructuredOutputValidation\StructuredOutputValidation.csproj --configuration Release --no-build -- --cases .\fixtures\cases.json --schema .\schema\diagnosis-candidate.schema.json --allowlist .\fixtures\evidence-allowlist.json --output .\artifacts\observation.jsonl
```

为验证 deterministic artifact，随后把第一次 JSONL 复制为保留样本，再以同一 run command 重跑，并比较 SHA-256。复制与比较只针对本 Lab 的两个明确 artifact，不删除或覆盖其他文件。

## Acceptance Criteria

- SDK 必须实际报告 `10.0.301`；否则记录版本漂移并停止，不把结果合并到当前 Evidence。
- `packages.lock.json` 必须将 `NJsonSchema` 直接依赖锁为 `11.6.1`；restore 后 locked restore 成功。
- build、test、runner 均以 exit code `0` 结束；预期无效 case 由测试断言为通过，不靠进程 crash 表示成功。
- `observation.jsonl` 恰有八行，case ID 唯一且与冻结输入一一对应。
- 八个 case 的 terminal stage、error code 与 recommended action 精确匹配 Frozen Input Cases。
- 每行包含 raw SHA-256 与四层状态；早期失败后的所有层均为 `NOT_RUN`。
- `valid-accepted` 是唯一 `ACCEPTED` case；不存在 invalid case 被接受。
- tests 至少锁定 schema / DTO parity、Domain rules、all-case matrix、first-failure short-circuit 与 automatic repair attempts=`0`。
- 两次 runner 执行产生 byte-for-byte 相同 JSONL；若不同，必须保存 diff 并判定 Lab 未通过。
- build / test / run 阶段无 Provider call、无 credential access、无网络依赖；trace 不包含环境变量值或秘密。
- 任一标准失败时，保留 raw output 与 mismatch；Claim 状态继续为 `BLOCKED`，不得修改 Expected 来获得 PASS。

## Expected Failure Paths

- 无效输入：malformed / truncated / synthetic text 在 Parse 失败；结构违反在 Schema 失败；Evidence 违反在 Domain 失败。
- Provider / Tool 失败：`OUT_OF_SCOPE / NOT_INJECTED`；本 Lab 没有 Provider 或 Tool。
- 超时或取消：`OUT_OF_SCOPE`；不把本地 process interruption 解释成 Provider incomplete。
- 结构化输出不满足合同：返回 `PARSE_FAILED`、`SCHEMA_FAILED` 或 `DTO_FAILED`，不进入 Domain / Accept。
- 预算耗尽：`OUT_OF_SCOPE`；synthetic truncated metadata 不能证明真实 token budget exhausted。
- Repair / Retry：automatic repair attempts 固定为 `0`；`UPSTREAM_RETRY_ELIGIBLE` 只是本地 policy label，不执行 retry，也不证明模型能 repair。

## Observations（Owner：Lab Engineer）

> Status：`EXECUTED / LAB_PASS_CANDIDATE`（Lab Engineer 当时的 handoff 状态）。这里记录的是 Lab Engineer 的原始本地观测；当时 Claim Status 与 Evidence Gate 仍等待 Researcher执行 `EVIDENCE_MERGE`，当前该 Merge 已在下方完成。

- Environment：`Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301 / Host 10.0.9 / MSBuild 18.6.4`；`NJsonSchema 11.6.1`；source HEAD before Lab commit `b359a329df02ce7487b0cb1a9feaad66c886d4dc`；执行时间 `2026-08-20T00:42:30.7668366+08:00`。完整环境输出见 `artifacts/logs/dotnet-info.txt`。
- Commands：按 [Run Instructions](#run-instructions) 顺序执行 `dotnet --info`、首次 restore、locked restore、Release build、detailed test 与 runner；runner 使用完全相同命令执行两次。逐命令记录见 `artifacts/logs/execution.md`。
- Exit Codes：`dotnet --info=0`；首次 sandboxed restore=`1`、approved identical retry=`0`；首次 sandboxed locked restore=`1`、approved identical retry=`0`；initial build=`1`、intermediate build=`0`、final build=`0`；tests=`0`、final tests=`0`；runner first=`0`、runner second=`0`；byte compare=`True`。
- Build Result：`PASS`；最终 `0 warnings / 0 errors`。首次实现 build 因测试源缺少 `using Xunit;` 出现 10 个 `CS0246`，局部修复后通过；未改变冻结 Design、fixture 或 expected result。
- Test Result：`PASS / 5 of 5`。实际覆盖 schema / DTO parity、Domain rules、eight-case matrix、first-failure short-circuit 与 automatic repair attempts=`0`。
- Runtime Output：两次均写入 `8` 行；`8` 个唯一 case ID；`valid-accepted` 是唯一 `ACCEPTED`；automatic repair attempts=`0`。第一次保留在 `artifacts/observation-first.jsonl`，第二次在 `artifacts/observation.jsonl`。
- Fault Injection Result：`invalid-json`、`truncated-json`、`synthetic-refusal-text` 均为 `PARSE_FAILED / INVALID_JSON`；`missing-required` 为 `SCHEMA_FAILED / REQUIRED`；`wrong-type` 为 `SCHEMA_FAILED / TYPE`；`extra-property` 为 `SCHEMA_FAILED / ADDITIONAL_PROPERTY`；`nonexistent-evidence` 为 `DOMAIN_FAILED / UNKNOWN_EVIDENCE_ID`。所有 stage / code / action 均与冻结矩阵一致。
- Observed Behavior：terminal stage 计数为 `ACCEPTED=1 / PARSE_FAILED=3 / SCHEMA_FAILED=3 / DOMAIN_FAILED=1`；所有早期失败之后的较晚层均为 `NOT_RUN`；每行都有 raw SHA-256、四层状态、terminal stage、error codes、action 与 repair count。
- Unexpected Behavior：sandbox 内的两个 restore 尝试因 NuGet socket access 被拒绝而返回 `NU1301`；按权限流程仅对 restore 重跑后成功。首次 build 的测试 using 缺失与一次 xUnit analyzer warning 均在 Lab 范围内局部修正；最终 build / test / run 无未关闭异常。
- Reproduction Notes：root `packages.lock.json` 把 direct `NJsonSchema` 锁为 `11.6.1`，test project 另有 lockfile；restore 后 build / test / run 均使用 `--no-restore` 或 `--no-build`；未调用 Provider、未读取 credentials、未改写 raw input。
- Runtime Limitations：只观察固定本机、固定 package、Draft 4 schema、八个 synthetic raw strings 与 allowlist；没有 Provider envelope、模型输出、线上网络、并发、负载或真实 Evidence truth。`synthetic-refusal-text` 与 `SYNTHETIC_TRUNCATED` 仍只是 fixture metadata，不是 Provider refusal / truncation observation。
- Determinism：两份 JSONL SHA-256 均为 `C484C1221933784679FC7AA5EC7185C928434019CE43B483913CEC2514469CC8`，byte-for-byte comparison=`True`。
- Lab Conclusion Candidate：`PASS / EVIDENCE_MERGE_REQUIRED`（Lab Engineer 当时的 handoff 候选）；本角色不更新 Claim Status，当前 Researcher Merge 已在下方完成。

| Run | Input | Raw Output / Trace | Result |
|---|---|---|---|
| `1` | frozen 8-case matrix | `artifacts/observation-first.jsonl` | `8 rows / 8 unique / 1 accepted / exit 0` |
| `2` | same command and frozen inputs | `artifacts/observation.jsonl` | `8 rows / 8 unique / 1 accepted / exit 0` |
| `compare` | first vs second JSONL | SHA-256 `C484C122...69CC8` for both | `BYTE_IDENTICAL=True` |

## Interpretation / Evidence Merge（Owner：Researcher）

> Status：`MERGED / PASS`。

合并严格按 `Experiment -> Observation -> Evidence Interpretation -> Claim Status`：

- `03-C03`：Experiment 固定四层 pipeline 与 first-failure 判据；Observation 显示三个 parse failure、三个 schema failure、一个 domain failure 与一个 accepted case 都保留完整 stage trace，早期失败后的层为 `NOT_RUN`；Interpretation 只确认八个固定输入的本地分层；Claim Status=`CONFIRMED`。
- `03-C04`：Experiment 冻结八类 case 的 stage / code / action；Observation 是两份各 8 行、8 个唯一 case、1 个 accepted 的 JSONL，全部命中矩阵且 SHA-256 相同；Interpretation 只确认 frozen matrix 与当前环境下的 deterministic artifact；Claim Status=`CONFIRMED`。
- `03-C05`：Experiment 固定 automatic repair attempts=`0` 与 local decision labels；Observation 显示 retry-eligible、upstream-cause-required、stop-non-contract 与 stop-domain actions 均按 `declared_input_class` 产生，repair count 始终为 `0`；Interpretation 只确认本地 classifier，不把 synthetic metadata 写成真实 refusal / truncation，也不声称模型 repair；Claim Status=`CONFIRMED`。
- `03-C07`：Experiment 固定 `.NET 10.0.301 + NJsonSchema 11.6.1 + Draft 4 subset`、lockfiles、commands 与 acceptance；Observation 保留最终 clean build、5/5 tests、双 run、byte comparison、早期 restore / build failure 及 disposition；Interpretation 只确认当前 runtime、package graph、schema、fixtures 与 allowlist；Claim Status=`CONFIRMED`。

替代解释仍被保留：`SYNTHETIC_TRUNCATED` 与 `SYNTHETIC_NON_CONTRACT_INPUT` 的含义来自 fixture metadata；raw string 自身只证明 parse failure。Domain allowlist 只证明当前列表内的引用判断，不证明 Evidence 或 summary 的事实真实性。

## Conclusion

- Confirmed：`03-C03`、`03-C04`、`03-C05`、`03-C07`
- Partial：`NONE`
- Blocked：`NONE`
- Follow-up：`OUTLINE`

## Limitations

实验结论只覆盖本 fixture、固定版本、运行环境和观测条件。它不能自动外推到其他 Provider、模型、schema dialect、validator、规模或生产负载。即使所有 case 通过，也不能证明候选内容真实、Evidence 实体存在于 allowlist 之外的真实系统、Tool 已执行、权限正确或 evaluator 已完成。

## Evidence Links

- Evidence Card：[`Article 03 Evidence Register`](../../articles/03-structured-output-machine-contract/evidence.md)
- Raw trace / log：[`execution.md`](artifacts/logs/execution.md)、[`observation-first.jsonl`](artifacts/observation-first.jsonl)、[`observation.jsonl`](artifacts/observation.jsonl)
- Dependency / environment：[`runner lock`](packages.lock.json)、[`test lock`](tests/StructuredOutputValidation.Tests/packages.lock.json)、[`dotnet-info.txt`](artifacts/logs/dotnet-info.txt)
- Source revision：`b359a329df02ce7487b0cb1a9feaad66c886d4dc`（Lab 执行前 HEAD；本轮不 commit）
- Article section：`Evidence Gate PASS / next OUTLINE`

## Stop Line

`LAB_DESIGN / LAB_EXECUTE / LAB_OBSERVATION / EVIDENCE_MERGE` 到此完成。Design、Expected、Acceptance Criteria 与 Observations 原始事实保持冻结；下一动作是 `OUTLINE`，本角色不创建 Outline / Draft、不重新执行 Lab、不修改全局状态，也不 commit / push。
