# Lab 02｜Tool Runtime

> **已有参考：** [Lab 01 Structured Output Validation](../lab-01-structured-output-validation/README.md) 已建立 Design / Expected / Observation / Evidence Merge 分离。本 Lab 复用该证据纪律，新增 Registry、filesystem boundary、Policy、timeout / cancellation、result views、idempotency 与 append-only trace，不复用 Lab 01 的 runtime conclusion。

## Metadata

- Lab ID：`Lab 02`
- Title：`Tool Runtime：Validate、Policy、Execute、Result 与 Trace`
- Owning Article：`Article 06｜Tool Runtime`
- Lifecycle Status：`EVIDENCE_MERGED / DESIGN_FROZEN`
- Evidence Status：`PASS / 5 LAB CLAIMS CONFIRMED`
- Runtime / Language：`C# / .NET 10`
- Fixture Version：`lab-02-design-v1`
- Environment Candidate：`Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301`
- External NuGet Packages：`NONE`
- Provider Calls：`0`
- Local Lab Runs / Invocation Trace Rows：`2 / 28`
- Lab Runtime Evidence：`CONFIRMED_WITHIN_FIXTURE`
- Design Frozen At：`2026-08-20（Asia/Shanghai）`
- Last Run：`2026-08-20T07:14:49+08:00 accepted second run`
- Next Allowed Action：`NO_LAB_ACTION / ARTICLE_GIT_DIFF_VERIFY`

> Researcher-owned frozen `LAB_DESIGN`和运行前Expected保持不变；Lab Engineer的Observed与raw artifacts追加在后，Researcher Interpretation单独合并。当前结论只覆盖本fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation。

## Goal

消除一个具体不确定性：在固定、纯本地、只读的 C# fixture 中，一个 Host Tool Runtime 能否把 model-visible call candidate 与真实 implementation 分开，并在每条成功 / 失败路径上保留可判定的 validation、policy、execute、result 与 trace boundary。

本 Lab 只判断当前 fixture 是否遵守课程 Tool Runtime v1 contract；不证明行业统一架构、Provider behavior、生产安全或 exactly-once。

## Lab Design（Owner：Researcher）

- Related Article：[`Article 06`](../../articles/06-tool-runtime/README.md)
- Related Claim IDs：`06-C05`、`06-C06`、`06-C07`、`06-C08`、`06-C09`
- Research Question：固定的 Calculator + ReadOnlyFileTool 是否能在 valid、path escape、policy conflict、timeout、caller cancellation、invalid result、large result 与 duplicate invocation 下产生可区分 terminal result 和 append-only JSONL trace？
- Hypothesis：如果 model-visible definition、Host Registry、arguments validation、policy、idempotency、execution、result validation、render / spill 与 trace 是独立 gate，那么 12 个 required cases 会精确命中 frozen terminal stage / code；任何 early terminal 之后的 later stage 都为 `NOT_RUN`，两个相同 run 的 JSONL byte-identical。
- What Would Falsify It：
  - 任一 required case 被错误接受、terminal stage / code 不匹配或 early failure 后仍 execute；
  - `DENY` 被 `ALLOW / ASK` 覆盖，或 `ASK` 进入 execute；
  - traversal / real-target escape 被读取，或 required link/junction fixture 无法真实创建与确认；
  - timeout 与 caller cancellation 合并成同一 origin，或产生成功 result；
  - invalid result 进入 render，large result 全文进入 Model / Trace view；
  - same ID / same args 二次调用 handler，或 same ID / different args 未 conflict；
  - 任一 invocation 没有追加 trace、trace 旧行被改写、required two-run artifact 不同；
  - 环境、SDK、fixture、commands 或 raw failure 没有保存。
- Fixture Boundary：未来实现和执行只能写本 Lab 目录与每次运行新建的 `%TEMP%/agent-engineering-lab-02-<guid>/`。Tool 只读 fixture files；唯一写入是 Lab-owned JSONL / logs / tests artifacts 与 temp spill。
- Environment：目标 `net10.0`；`global.json` 必须 pin `10.0.301`、`rollForward=disable`、`allowPrerelease=false`。执行前重新保存完整 `dotnet --info`；若 exact SDK 不匹配，停止。
- Dependencies：BCL only；External NuGet PackageReference=`0`。未来 `NuGet.Config` 必须清空 remote sources；restore 只生成本地 assets，不授权联网。
- Inputs：两个 ToolDefinition、Host-only Registry metadata、12 个 required case、三个 exact UTF-8 fixture files、Policy tuples、timeout / cancellation fault gate、invalid-result fault、64-byte inline threshold、fixed invocation IDs。
- Variables：
  - 自变量：`case_id`、tool name、raw arguments、Policy tuple、cancellation source、result fault、invocation ID。
  - 控制变量：SDK / TFM、culture=`InvariantCulture`、timezone-independent output、UTF-8 no BOM + LF、allow-root topology、inline threshold=`64 bytes`、large content=`1024 bytes`、timeout case budget=`50 ms`、caller-cancel case budget=`5000 ms`、trace schema v1。
  - 因变量：stage status、policy decision、handler execution count、terminal stage / code、cancellation origin、result validation、render mode、digest / byte count、relative spill ref、trace rows / hash。
- Expected Observable：
  - 每次 invocation 以 `FileMode.Append` 向新建 JSONL 追加一行；12 cases 共 14 invocation rows。
  - Trace 不含 wall-clock timestamp、absolute temp path、environment variable value、file content、credential 或 stack trace。
  - 每行保存 `schema_version`、`sequence`、`run_id`、`case_id`、`attempt`、`invocation_id`、`tool_name`、`arguments_sha256`、各 stage status、Policy inputs / decision、handler execution count、cancellation origin、terminal stage / code、result byte count / digest、render mode、relative spill ref。
  - 未运行的 later stages 必须是 `NOT_RUN`；不能用缺字段或 `PASS` 代替。
- Fault Injection：
  - lexical `..` traversal；
  - allow-root 内 junction（失败后 symlink fallback）指向 root 外；
  - `ALLOW + ASK + DENY` 与 `ALLOW + ASK + ALLOW`；
  - test-only never-release execution gate + 50ms timeout；
  - pre-cancelled caller token；
  - Calculator handler test seam 返回 wrong result kind；
  - 1024-byte file 超过 64-byte inline threshold；
  - duplicate same ID / same args 与 same ID / different args。
- Commands / Execution Needs：见 [Run Instructions](#run-instructions)。这些是 future Lab Engineer entrypoints，当前未运行。
- Acceptance Criteria：见 [Acceptance Criteria](#acceptance-criteria)。任一 required criterion 失败，Lab 返回 `FAILED_LAB / CLAIMS_REMAIN_BLOCKED`；不得调低 Expected。
- Evidence Mapping：
  - `06-C05 -> TR-02 / TR-03 / TR-04 + link setup evidence`
  - `06-C06 -> TR-05 / TR-06`
  - `06-C07 -> TR-07 / TR-08`
  - `06-C08 -> TR-01 / TR-02 / TR-09 / TR-10 + spill artifact`
  - `06-C09 -> TR-11 / TR-12 + two-run JSONL comparison`
- Limitations：
  - 只覆盖 fixed Windows / .NET / two-tool fixture；
  - path check 不覆盖并发替换 link 的 TOCTOU，也不证明 handle-based sandbox；
  - cancellation 是 cooperative，test gate 不是第三方慢 I/O；
  - idempotency 只在同进程 / 同 run，不是 exactly-once；
  - ASCII fixture 不覆盖 binary、encoding attack、secret redaction或生产规模；
  - custom JSONL 是课程 schema，不是 OpenTelemetry semantic convention。
- Safety / Permission Constraints：
  - 不调用 Provider、不读取 credentials、不访问网络；
  - 不开放 shell Tool、不写业务文件、不读取 fixture root 外的 target；
  - link/junction 只在 unique temp fixture 内创建，失败后不静默提权；
  - cleanup 只能删除经过绝对路径、temp-parent、name prefix 与 sentinel 四重核对的本 Lab temp root；
  - 失败 output 必须保留；Expected 不得在执行后修改。

## Prerequisites

- exact `.NET SDK 10.0.301` 可用；执行时重新核对，不从本轮 environment inventory 直接继承。
- Windows filesystem 允许在 unique temp root 内创建至少一种真实 directory junction 或 symbolic link；junction 优先，symlink 仅作 fallback。
- Lab Engineer 已创建并审查 future implementation / tests，但没有更改本 Design。
- `fixtures/manifest.md` 中 exact bytes、case IDs、threshold、terminal codes 与 trace fields 未变化。
- 无 Provider、模型、API key、production file 或 business repo dependency。

## Frozen Tool Contracts

### Calculator

Model-visible definition：

```text
name: calculate_binary
input:
  operation: add | subtract
  left: decimal
  right: decimal
```

Host-only Registry metadata：

- implementation：future `CalculatorTool`
- side effect：`NONE`
- default timeout：`1000 ms`
- result kind：`calculation`
- result contract：finite decimal `value`
- test seams：`invalid_result_kind` only；不得暴露给 model-visible schema。

### ReadOnlyFileTool

Model-visible definition：

```text
name: read_text
input:
  relative_path: non-empty relative string
```

Host-only Registry metadata：

- implementation：future `ReadOnlyFileTool`
- operation：`READ_ONLY`
- allow-root：future exact `<run-root>/allowed`
- max readable bytes：`4096`
- inline threshold：`64 bytes`
- default timeout：`1000 ms`
- result kind：`file_text`
- link policy：lexical containment + every-existing-component link resolution + final resolved containment。
- test seam：`never_release_execution_gate` only；不得开放给 model-visible schema。

## Course Policy v1

Policy inputs固定为 `global`、`tool`、`resource` 三层，每层只取 `ALLOW | DENY | ASK | MISSING`：

```text
if any decision is DENY   -> DENY
else if any is MISSING    -> DENY
else if any is ASK        -> ASK
else all are ALLOW        -> ALLOW
```

- `DENY` terminal：`POLICY_DENIED`，execute=`NOT_RUN`。
- `ASK` terminal：`APPROVAL_REQUIRED`，本 Lab 不等待真人输入，execute=`NOT_RUN`。
- 只有 `ALLOW` 可以进入 idempotency / execute。
- 这是课程 proposal，不宣称行业统一合并规则。

## Path Decision v1

ReadOnlyFileTool 只接受 relative path。future implementation 必须：

1. 固定 fully-qualified existing allow-root。
2. `Path.GetFullPath(relativePath, allowRoot)` 得到 lexical candidate。
3. `Path.GetRelativePath(allowRoot, candidate)`；不同 root、rooted result、`..` 或以 `.. + separator` 开头均拒绝为 `PATH_OUTSIDE_ROOT`。
4. 逐个检查 allow-root 到 target 的现有 path component；若 component 是 link / junction，用 `ResolveLinkTarget(true)` 取得 final target，再与 remaining path 组合。
5. resolved final path 再做 relative containment；失败为 `PATH_LINK_OUTSIDE_ROOT`。
6. 两个 check 都通过后才 open read-only stream。

该算法的 fixture 验证不覆盖 check/open race。实现若无法清晰地逐 component 解析，Lab Engineer必须返回 Researcher，不得退化为 string prefix。

## Timeout / Cancellation v1

- Runtime为每次 invocation接收 caller `CancellationToken`，另建 timeout `CancellationTokenSource`并按 frozen budget调用 `CancelAfter`；linked token传给 handler。
- caller预取消时在 handler前返回 `CALLER_CANCELLED / CALLER`；timeout case的 never-release gate只等待 linked token，timeout source触发后返回 `TIMED_OUT / TIMEOUT`。
- fixed cases不同时触发两个 source；若未来出现 race不得猜测唯一原因。`CancelAfter` 只是 cooperative signal，不得写成强制线程终止。

## Result Contract v1

### Canonical candidate

```text
calculation -> kind=calculation, value=finite decimal
file_text   -> kind=file_text, UTF-8 content, byte_count, sha256
```

- handler return 后先验证 kind、required fields、type、finite value、byte count 与 digest。
- invalid result terminal：`RESULT_VALIDATION / RESULT_SCHEMA_INVALID`；render=`NOT_RUN`。
- valid result不自动等于 Evidence；本 Lab 只检查内部 contract。

### Render / spill

- `byte_count <= 64`：`INLINE`。
- `byte_count > 64 && <= 4096`：full bytes 写到 `<run-root>/spills/<sha256>.txt`，render=`SPILLED`。
- Model view：最多 64 bytes preview + `byte_count` + `sha256` + relative `spill_ref`。
- UI view：`display_mode` + `byte_count` + `sha256` + relative `spill_ref`；不暴露 absolute temp path。
- Trace view：只保存 `byte_count`、`sha256`、render mode 与 relative spill ref；不保存 preview / full content。
- spill 是 Host internal Lab artifact，不是 ReadOnlyFileTool 的业务写能力。

## Idempotency v1

- key：`invocation_id`
- stored value：`canonical_arguments_sha256 + validated canonical result + render metadata`
- first invocation：执行 handler，并在 result validation 通过后 cache。
- same ID / same digest：`IDEMPOTENCY / REPLAYED`；复用 validated result / render metadata；handler count 不增加。
- same ID / different digest：`IDEMPOTENCY / IDEMPOTENCY_CONFLICT`；handler=`NOT_RUN`。
- cache 仅存活于 single process / single run；没有 durable store、distributed lock 或 crash recovery。

## Frozen Case Matrix

下表全部是运行前 Expected。TR-11 / TR-12 各含两个 invocation，因此每次 run 共 14 JSONL rows。

| Case | Tool / Input | Policy / Fault | Expected Terminal Stage | Expected Code | Expected Render | Handler Count |
|---|---|---|---|---|---|---:|
| `TR-01` | `calculate_binary(add,2,3)` | all ALLOW | `SUCCEEDED` | `OK` | `INLINE` | 1 |
| `TR-02` | `read_text(small.txt)` | all ALLOW | `SUCCEEDED` | `OK` | `INLINE` | 1 |
| `TR-03` | `read_text(../outside/secret.txt)` | all ALLOW | `CANONICALIZE` | `PATH_OUTSIDE_ROOT` | `NOT_RUN` | 0 |
| `TR-04` | `read_text(link-out/secret.txt)` | real junction or symlink points outside | `CANONICALIZE` | `PATH_LINK_OUTSIDE_ROOT` | `NOT_RUN` | 0 |
| `TR-05` | `read_text(small.txt)` | global ALLOW / tool ASK / resource DENY | `POLICY` | `POLICY_DENIED` | `NOT_RUN` | 0 |
| `TR-06` | `read_text(small.txt)` | global ALLOW / tool ASK / resource ALLOW | `POLICY` | `APPROVAL_REQUIRED` | `NOT_RUN` | 0 |
| `TR-07` | `read_text(small.txt)` | never-release gate / timeout 50ms / caller active | `EXECUTE` | `TIMED_OUT` | `NOT_RUN` | 1 |
| `TR-08` | `read_text(small.txt)` | caller pre-cancelled / timeout 5000ms | `EXECUTE` | `CALLER_CANCELLED` | `NOT_RUN` | 0 |
| `TR-09` | `calculate_binary(add,2,3)` | handler returns `kind=file_text` test fault | `RESULT_VALIDATION` | `RESULT_SCHEMA_INVALID` | `NOT_RUN` | 1 |
| `TR-10` | `read_text(large.txt)` | 1024 bytes / threshold 64 | `SUCCEEDED` | `OK` | `SPILLED` | 1 |
| `TR-11.1` | first `calculate_binary(add,2,3)` / ID `inv-replay` | all ALLOW | `SUCCEEDED` | `OK` | `INLINE` | 1 |
| `TR-11.2` | same call / same ID | cached digest matches | `IDEMPOTENCY` | `REPLAYED` | `INLINE` | 1 |
| `TR-12.1` | first `calculate_binary(add,2,3)` / ID `inv-conflict` | all ALLOW | `SUCCEEDED` | `OK` | `INLINE` | 1 |
| `TR-12.2` | `calculate_binary(subtract,2,3)` / same ID | cached digest differs | `IDEMPOTENCY` | `IDEMPOTENCY_CONFLICT` | `NOT_RUN` | 1 |

Additional Expected：

- TR-01 value exactly `5`。
- TR-02 byte_count / SHA-256 exactly match manifest。
- TR-03 / 04 / 05 / 06 / 08 / 12.2 的 handler未进入；TR-07 进入 handler test gate，但没有读取文件或产生 result。
- TR-07 `cancellation_origin=TIMEOUT`；TR-08 `cancellation_origin=CALLER`。
- TR-09 execute=`PASS`、result validation=`FAIL`、render=`NOT_RUN`。
- TR-10 spill bytes / SHA-256 与 `large.txt` 一致；Model / Trace view 不含 1024-byte content。
- TR-11.2 与 TR-12.2 都追加独立 trace row；replay / conflict不是“没有记录”。

## Trace Schema v1

每行是一个 JSON object，字段顺序固定；UTF-8 no BOM、LF line ending、InvariantCulture。至少包含：

```text
schema_version
sequence
run_id
case_id
attempt
invocation_id
tool_name
arguments_sha256
registry_status
canonicalize_status
validation_status
policy_inputs
policy_decision
idempotency_status
execute_status
handler_execution_count
cancellation_origin
result_validation_status
render_status
terminal_stage
terminal_code
result_byte_count
result_sha256
spill_ref
```

- `run_id` 固定为 `lab-02-fixed-run`，用于 deterministic artifact；真实执行时间与 environment 写到独立 execution log。
- 未执行 stage 必须为 `NOT_RUN`。
- `spill_ref` 必须是相对 ref；没有 spill 时为 `NONE`。
- `result_sha256` 只在 validated result 存在时填写，否则 `NONE`。
- writer 必须 `CreateNew` 建空 artifact，再以 append mode逐行写；任何 rewrite / truncate 是 test failure。

## Fixture

当前只有 design manifest：[fixtures/manifest.md](fixtures/manifest.md)。Lab Engineer应在本目录内创建下列未来结构；当前 Researcher不得提前创建：

```text
lab-02-tool-runtime/
├─ README.md
├─ global.json                         # future implementation asset
├─ NuGet.Config                        # future, clear remote sources
├─ ToolRuntimeLab.slnx                 # future
├─ fixtures/
│  ├─ manifest.md                      # current design-only asset
│  └─ cases.json                       # future exact machine input
├─ scripts/
│  ├─ setup-fixture.ps1                # future, unique temp root + link fallback
│  └─ cleanup-fixture.ps1              # future, sentinel-guarded cleanup
├─ src/
│  └─ ToolRuntimeLab/                  # future implementation
├─ tests/
│  └─ ToolRuntimeLab.Specs/            # future BCL-only executable specs
└─ artifacts/
   ├─ logs/                            # future command output / failures
   ├─ observation-first.jsonl          # future raw trace
   └─ observation.jsonl                # future raw trace
```

`src/`、`tests/`、scripts、solution、JSON input与 artifacts 当前均不存在；这正是 Design Gate 的预期状态。

## Run Instructions

以下命令均为 Lab Engineer future execution contract，当前未执行。Lab Engineer可以为路径 quoting 做机械修正，但不得改变命令语义、case matrix或 acceptance。

```powershell
dotnet --info
dotnet restore .\ToolRuntimeLab.slnx --configfile .\NuGet.Config
dotnet build .\ToolRuntimeLab.slnx --configuration Release --no-restore

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-fixture.ps1 -RunLabel first
dotnet run --project .\tests\ToolRuntimeLab.Specs\ToolRuntimeLab.Specs.csproj --configuration Release --no-build -- --manifest .\fixtures\cases.json --run-label first --trace .\artifacts\observation-first.jsonl
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\cleanup-fixture.ps1 -RunLabel first

powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\setup-fixture.ps1 -RunLabel second
dotnet run --project .\tests\ToolRuntimeLab.Specs\ToolRuntimeLab.Specs.csproj --configuration Release --no-build -- --manifest .\fixtures\cases.json --run-label second --trace .\artifacts\observation.jsonl
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\cleanup-fixture.ps1 -RunLabel second

Get-FileHash -Algorithm SHA256 .\artifacts\observation-first.jsonl
Get-FileHash -Algorithm SHA256 .\artifacts\observation.jsonl
```

Execution logging requirements：

- 每条命令、working directory、stdout / stderr、start / end、exit code 写入 `artifacts/logs/execution.md` 与 raw log。
- setup script必须记录 junction attempt、fallback attempt、exception / exit code、实际 `link_kind` 与 `ResolveLinkTarget(true)` final target classification；不得记录成“成功”除非实际成立。
- cleanup 必须在复制完 trace / spill evidence 后执行；若 sentinel guard 不满足，只报告路径，不删除。
- build / run不得联网；若 restore尝试 remote source，立即停止并修复 future `NuGet.Config`，不以提权绕过。

## Acceptance Criteria

- environment实际报告 `Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301`；任何漂移都保留并停止当前 evidence merge。
- project target=`net10.0`，External `PackageReference=0`；restore、build、two spec runs exit code均为 `0`。
- Required link case实际创建为 junction或symlink，`ResolveLinkTarget(true)` 明确指向 allow-root 外；两种 setup 都失败则 Lab=`FAILED_LAB`。
- 每次 run恰有 14 行、sequence=`1..14`、12 个 case groups、14 个 unique `case_id + attempt`。
- Case Matrix 的 terminal stage、code、render、handler count全部 exact match。
- 每个 early terminal 后 later stage=`NOT_RUN`；不存在 invalid / denied / ask / cancelled case被接受。
- TR-01 value=`5`；TR-02 / TR-10 byte count与 SHA-256 exact match manifest。
- TR-10 spill文件存在于 unique Lab temp，bytes / SHA-256与 large fixture相同；Model view最多64 bytes preview，Trace不含 full content，spill ref相对。
- TR-09 handler已执行一次但 result validation失败，render / cache均未发生。
- TR-11.2 handler count保持1，result digest与 first相同；TR-12.2 handler count保持1且没有 result。
- trace writer tests证明每次 append前的所有既有 bytes保持为新文件 prefix；禁止 reopen truncate / rewrite。
- 两次 JSONL byte-for-byte identical、SHA-256相同；任何差异必须保存 diff 并使 C09保持 BLOCKED。
- provider calls、credential reads、network access、shell Tool、business writes均为0；trace不含 absolute temp path、secret或environment variable value。
- 任一标准失败：保存 raw output、spill / trace partial artifact与 mismatch，结论 `FAILED / CLAIMS_REMAIN_BLOCKED`；不得改 Expected。

## Expected Failure Paths

- 无效输入：path traversal / real-target escape在 `CANONICALIZE` 停止。
- Provider / Tool 失败：Provider=`OUT_OF_SCOPE`；Tool fault通过 timeout / cancellation / invalid result exact cases注入。
- Policy：`DENY` 与 `ASK` 均不 execute。
- 超时或取消：分别为 `TIMED_OUT` 与 `CALLER_CANCELLED`；不压成一个 code。
- 结构化 result 不满足合同：`RESULT_SCHEMA_INVALID`，不 render / cache。
- 大结果：不 inline full content；spill 到 Lab-owned temp。
- duplicate：same args replay；different args conflict；不声称 exactly-once。
- setup / permission：junction失败后尝试 symlink；两者失败时保存错误并 `FAILED_LAB`，不静默提权。
- 预算耗尽：`OUT_OF_SCOPE`；64-byte threshold是 result rendering policy，不是 token budget。

## Allowed Patches for Lab Engineer

Lab Engineer只允许：

- 在本 Lab 目录创建上面列出的 future implementation、BCL-only specs、fixture input、scripts与 raw artifacts；
- 为满足 frozen contract修复 compile error、missing using、path quoting、serialization field order或实现 bug；
- 记录真实失败、environment drift、link setup limitation与 partial artifact。

Lab Engineer不得：

- 改 hypothesis、falsifier、12-case matrix、exact input bytes、Policy v1、50 / 5000ms、64 / 4096-byte threshold、terminal code、trace fields或 acceptance；
- 引入 Provider、外部 NuGet、network、shell Tool、业务写入或 production credential；
- 把 junction / symlink失败换成字符串模拟；
- 修改 Article Research / Evidence、Claim Status、global state、canonical、status、Outline / Draft；
- 删除失败 output或为了 PASS 改 Expected。

若 frozen design无法安全实现，返回 `FAILED_LAB` 给 Researcher；只有 Researcher可在新的 Design revision中改变判据。

## Observations（Owner：Lab Engineer）

> Status：`EXECUTED / COMPLETE`。运行前的`NOT_EXECUTED` placeholder已由下方实际记录取代；本节保留Lab Engineer追加的raw observation，不改写上面的Design / Expected。

### 2026-08-20 LAB_EXECUTE / LAB_OBSERVATION append

- Execution Status Candidate：`EXECUTED / LAB_PASS_CANDIDATE / EVIDENCE_MERGE_REQUIRED`（historical Lab Engineer handoff candidate；Researcher已在后文完成Evidence Merge）。这是 Lab Engineer 的 raw observation 候选，不修改 Claim Status、不执行 Evidence Interpretation。
- Environment：Windows `10.0.19045`、RID `win-x64`、`.NET SDK 10.0.301`、MSBuild `18.6.4`、Host Runtime `10.0.9`；完整 environment output 见 [`artifacts/logs/dotnet-info.txt`](artifacts/logs/dotnet-info.txt)。
- Dependency Boundary：两个 project 均为 `net10.0`；`global.json` 固定 `10.0.301 / rollForward=disable / allowPrerelease=false`；`NuGet.Config` 清空 package sources；External `PackageReference=0`。
- Files Created：`global.json`、`NuGet.Config`、`ToolRuntimeLab.slnx`、`fixtures/cases.json`、`scripts/setup-fixture.ps1`、`scripts/cleanup-fixture.ps1`、`src/ToolRuntimeLab/*`、`tests/ToolRuntimeLab.Specs/*`、`artifacts/observation*.jsonl`、result-view / run-state / spill evidence 与 execution logs。
- Commands / Exit Codes：accepted `dotnet --info=0`、restore `0`、build `0`、first setup / run / cleanup `0 / 0 / 0`、second setup / run / cleanup `0 / 0 / 0`、two `Get-FileHash=0`、byte compare `0`。全部 exact command、working directory、start / end、stdout / stderr 与被保留的失败尝试见 [`artifacts/logs/execution.md`](artifacts/logs/execution.md)和 [`artifacts/logs/execution.raw.log`](artifacts/logs/execution.raw.log)。
- Build Result：`PASS`；`0 warnings / 0 errors`。Custom executable specs使用 BCL-only console project，没有 NuGet test framework。
- Link Disposition：两次 accepted setup 都首先真实创建 `JUNCTION`，未进入 symlink fallback；两次 `.NET Directory.ResolveLinkTarget(path, true)` 都确认 final target位于 allow-root 外、同一 owned run-root 内，且 target fixture存在。两个 run-root 不同；cleanup后两者均不存在。
- Fixture Verification：每次 setup 都在 cases前实际验证 `small.txt=11 / E49C81E2D2F84E259D40E2FB8192F3BCD198B355184845D76D8F58807D0D78EE`、`large.txt=1024 / 26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61`、`secret.txt=15 / A532F53598B8BB67609FD55670AA58B9A1DD5F3F77E9C4FA44321533C85BAF6B`。
- Trace Writer：每个 run先用 `FileMode.CreateNew` 建空 artifact，再逐 invocation用 `FileMode.Append`；spec额外验证 second CreateNew被拒绝，并在每次 append后验证所有旧 bytes仍是新文件 prefix。两次均 `create_new=PASS / append_prefix=PASS`。

#### Observed 14-row distributions（两次相同）

| Field | Distribution |
|---|---|
| rows / groups / unique case+attempt | `14 / 12 / 14` |
| registry status | `PASS=14` |
| canonicalize status | `PASS=12 / FAIL=2` |
| validation status | `PASS=12 / NOT_RUN=2` |
| policy decision | `ALLOW=10 / ASK=1 / DENY=1 / NOT_RUN=2` |
| idempotency status | `PASS=9 / FAIL=1 / NOT_RUN=4` |
| execute status | `PASS=6 / FAIL=2 / NOT_RUN=6` |
| result validation status | `PASS=5 / FAIL=1 / NOT_RUN=8` |
| terminal stage | `SUCCEEDED=5 / CANONICALIZE=2 / POLICY=2 / EXECUTE=2 / IDEMPOTENCY=2 / RESULT_VALIDATION=1` |
| terminal code | `OK=5`；其余九种 frozen failure / replay / conflict code各 `1` |
| render | `INLINE=5 / SPILLED=1 / NOT_RUN=8` |
| handler execution count | `0=5 / 1=9` |
| cancellation origin | `NONE=12 / TIMEOUT=1 / CALLER=1` |

#### Observed exact cases（first / second一致）

| Case | Terminal | Render | Handler Count | Cancellation | Raw disposition |
|---|---|---|---:|---|---|
| `TR-01/1` | `SUCCEEDED / OK` | `INLINE` | 1 | `NONE` | value=`5` |
| `TR-02/1` | `SUCCEEDED / OK` | `INLINE` | 1 | `NONE` | `11` bytes + small SHA exact |
| `TR-03/1` | `CANONICALIZE / PATH_OUTSIDE_ROOT` | `NOT_RUN` | 0 | `NONE` | later stages `NOT_RUN` |
| `TR-04/1` | `CANONICALIZE / PATH_LINK_OUTSIDE_ROOT` | `NOT_RUN` | 0 | `NONE` | real junction escape rejected before execute |
| `TR-05/1` | `POLICY / POLICY_DENIED` | `NOT_RUN` | 0 | `NONE` | final policy `DENY` |
| `TR-06/1` | `POLICY / APPROVAL_REQUIRED` | `NOT_RUN` | 0 | `NONE` | final policy `ASK` |
| `TR-07/1` | `EXECUTE / TIMED_OUT` | `NOT_RUN` | 1 | `TIMEOUT` | gate entered；no result |
| `TR-08/1` | `EXECUTE / CALLER_CANCELLED` | `NOT_RUN` | 0 | `CALLER` | caller precheck；handler未进入 |
| `TR-09/1` | `RESULT_VALIDATION / RESULT_SCHEMA_INVALID` | `NOT_RUN` | 1 | `NONE` | execute=`PASS`；cache未写入 |
| `TR-10/1` | `SUCCEEDED / OK` | `SPILLED` | 1 | `NONE` | `1024` bytes + large SHA exact |
| `TR-11/1` | `SUCCEEDED / OK` | `INLINE` | 1 | `NONE` | first execution |
| `TR-11/2` | `IDEMPOTENCY / REPLAYED` | `INLINE` | 1 | `NONE` | handler count保持1；digest相同 |
| `TR-12/1` | `SUCCEEDED / OK` | `INLINE` | 1 | `NONE` | first execution |
| `TR-12/2` | `IDEMPOTENCY / IDEMPOTENCY_CONFLICT` | `NOT_RUN` | 1 | `NONE` | handler count保持1；无 result |

#### Determinism, spill and safety observations

- First trace：[`artifacts/observation-first.jsonl`](artifacts/observation-first.jsonl)，`10607 bytes / 14 LF rows / SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67`。
- Second trace：[`artifacts/observation.jsonl`](artifacts/observation.jsonl)，`10607 bytes / 14 LF rows / SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67`。
- Independent byte-array comparison：`identical=true`。两个 run来自不同 fresh process、不同 unique temp root和新 CreateNew trace。
- TR-10 cleanup前已把可审计 full spill分别复制到 `artifacts/spills/first/` 与 `artifacts/spills/second/`；两份均 `1024 bytes / SHA-256 26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61`。Model view仅含64-byte preview + metadata；Trace无 preview / full content；Model / Trace view均不含absolute temp path。
- Cleanup实际执行并通过 absolute temp parent、`agent-engineering-lab-02-` prefix、sentinel、non-parent、owned final link target、root非reparse point、移除link后无remaining reparse point检查；随后才recursive delete。两次 temp root均已确认删除。
- Provider calls=`0`、network calls=`0`、credential reads=`0`、shell Tool=`0`、business writes=`0`。唯一 executable Tool为 Calculator和ReadOnlyFileTool；Host internal write仅限 Lab temp spill / Lab artifacts。

#### Preserved failures and allowed patches

1. First restore exit `0` 但 stdout报告 spec ProjectReference多退一层并跳过；保留 raw output，只修相对 ProjectReference后重跑 accepted restore。
2. First setup attempt exit `1`：Windows PowerShell runtime没有 `Path.GetRelativePath`；失败发生在 temp root创建前。补丁保留 frozen guard语义，改为 fully-qualified parent + separator boundary的 `OrdinalIgnoreCase` containment。
3. First cleanup attempt exit `1`：Windows PowerShell `Remove-Item` 删除已验证 junction时抛 `NullReferenceException`；root与 evidence保持原样。补丁只把 reparse point本身删除改为同进程 `[IO.Directory]::Delete(path, false)`；guard通过后重跑 cleanup exit `0`。

#### Runtime limitations

- Observation只覆盖当前 Windows / `.NET 10.0.301`、固定 two-tool / ASCII fixture、single-process cache与无并发 link mutation；不覆盖 TOCTOU、跨进程 exactly-once、production sandbox、真实慢 I/O、secret redaction或其他 Provider / framework。
- Windows PowerShell缺少现代 `Path.GetRelativePath` 且对 junction `Remove-Item` 存在本机行为差异；脚本使用上述等价安全 guard与 .NET junction-only delete，不能外推到其他 shell / filesystem。
- JSONL是课程 Trace Schema v1；没有 wall-clock、absolute temp path、environment variable value、file content、credential或stack trace。执行时间和absolute setup path只保存在独立 Lab execution logs / run-state。

## Interpretation / Evidence Merge（Owner：Researcher）

> Status：`EVIDENCE_MERGED / PASS`。Researcher已重新读取frozen Design、Expected、全部raw Observation、failure history与runtime notes；没有修改Design/Expected或raw artifacts。

合并顺序固定为：

```text
Experiment -> Observation -> Evidence Interpretation -> Claim Status
```

| Claim | Expected | Observed / Raw evidence | Interpretation / scope | Status |
|---|---|---|---|---|
| `06-C05` | TR-02 valid；TR-03 traversal拒绝；TR-04真实link escape拒绝 | 两份trace exact；两份run-state均为真实`JUNCTION`，final target在allow-root外、owned run-root内 | fixed Windows fixture、single process、no concurrent link mutation成立；不覆盖TOCTOU/symlink fallback | `CONFIRMED` |
| `06-C06` | Deny wins；Ask不execute | TR-05 / TR-06两run均exact，handler=0、later stages=`NOT_RUN` | 只确认课程Policy v1，不是行业merge标准 | `CONFIRMED` |
| `06-C07` | timeout与caller产生不同terminal/origin且无result | TR-07=`TIMED_OUT/TIMEOUT`、handler=1；TR-08=`CALLER_CANCELLED/CALLER`、handler=0；两run exact | 只覆盖cooperative test gate，不证明强制终止/真实慢I/O | `CONFIRMED` |
| `06-C08` | valid tools、invalid result停止、1024-byte spill与bounded views | trace/result views/spill exact；两spill SHA=`26AD8132...55A61`；Model preview=64 bytes；Trace无全文/absolute temp path | 只覆盖课程Result Contract v1与ASCII fixture | `CONFIRMED` |
| `06-C09` | replay/conflict、append-only、两trace byte-identical | 每run 14 rows；TR-11/12 exact；`create_new/append_prefix=PASS`；两trace 10607 bytes、SHA=`50CEA4EC...21BD67`、byte-identical | 只证明single-process de-dup和deterministic artifact，不证明exactly-once | `CONFIRMED` |

三次保留失败为`RESTORE-01`、`SETUP-FIRST-01`、`CLEANUP-FIRST-01`；accepted rerun与allowed patches没有改变hypothesis、case matrix、threshold、terminal code或acceptance。

## Conclusion

- Confirmed：`06-C05`—`06-C09`（仅限frozen scope）
- Partial：`NONE_FROM_LAB`
- Blocked：`NONE_FROM_LAB`
- Proposal：`06-C04`继续为课程设计，不因Lab通过升级为行业标准
- Evidence Gate：`PASS`
- Follow-up：`NO_LAB_ACTION / ARTICLE_GIT_DIFF_VERIFY`

## Limitations

实验结论即使未来全部通过，也只覆盖本 fixture、固定 SDK、固定 Windows environment、fixed cases、single-process cache与无并发 link mutation条件。它不能自动外推到其他 OS、Provider、framework、真实 side effect、生产负载、distributed idempotency、Sandbox或 BuildPilot。

## Evidence Links

- Evidence Card：[`Article 06 Evidence Register`](../../articles/06-tool-runtime/evidence.md)
- Research：[`Article 06 Research`](../../articles/06-tool-runtime/research.md)
- Fixture Manifest：[fixtures/manifest.md](fixtures/manifest.md)
- Execution Summary：[`artifacts/logs/execution.md`](artifacts/logs/execution.md)
- Raw Log：[`artifacts/logs/execution.raw.log`](artifacts/logs/execution.raw.log)，SHA-256=`492C290405244289D8F2509866942FDB0061F672103257D2977BCA049EB7E639`
- Raw Traces：[`observation-first.jsonl`](artifacts/observation-first.jsonl) / [`observation.jsonl`](artifacts/observation.jsonl)，均为10607 bytes / 14 rows / SHA-256=`50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67`
- Result Views：[`first`](artifacts/result-views-first.json) / [`second`](artifacts/result-views-second.json)，均SHA-256=`5BD9F3452085153D6B87D735F0547D9505CC6BF746ECD4C3DC4FC0C980D6B638`
- Run State：[`first`](artifacts/run-state-first.json) / [`second`](artifacts/run-state-second.json)
- Spill Evidence：[`first`](artifacts/spills/first/26ad8132e3b544caefd85b30bf36df8d012dc7245c9d2224e0f9f50a2ac55a61.txt) / [`second`](artifacts/spills/second/26ad8132e3b544caefd85b30bf36df8d012dc7245c9d2224e0f9f50a2ac55a61.txt)，均1024 bytes / SHA-256=`26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61`
- Source revision：`N/A / working tree；checkpoint pending Master`
- Article section：`EVIDENCE_MERGE / EVIDENCE_GATE_PASS`

## Stop Line

`LAB_DESIGN`与Expected继续冻结，raw Observation/failure history不得改写。Evidence Merge、Article Final Gate、Publish / Build与Master Reconciliation均已完成，当前`NO_LAB_ACTION`；Article下一动作只能由Master执行`ARTICLE_GIT_DIFF_VERIFY`与独立checkpoint。Lab侧不得重开Design / Execute / Evidence Merge或启动Article 07。
