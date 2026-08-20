# Article 06 Evidence Register｜Tool Runtime

- Evidence Phase：`EVIDENCE_MERGE / EVIDENCE_GATE`
- Evidence Status：`PASS`
- Evidence Gate：`PASS / OUTLINE_ALLOWED`
- Claim Count：`9`
- Claim Summary：`8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`
- Evidence Card Count：`9`
- Retrieved / Verified At：`2026-08-20（Asia/Shanghai）`
- Required Lab：`Lab 02｜Tool Runtime`
- Lab Dependency：`SATISFIED_WITHIN_FROZEN_SCOPE`
- Lab Design：`FROZEN / UNCHANGED`
- Lab Execution / Observation / Merge：`EXECUTED / COMPLETE / EVIDENCE_MERGED`
- Provider Calls：`0`
- Local Lab Runs / Invocation Trace Rows：`2 / 28`
- Runtime Evidence：`CONFIRMED_WITHIN_FIXTURE`

> `06-C05`—`06-C09` 只在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下升级为 `CONFIRMED`。`06-C04` 继续是课程设计 `PROPOSAL`，Lab 通过不把它升级成行业标准。

## Claim Register

| Claim ID | Narrow Claim | Status | Evidence Class | Lab Dependency | Evidence |
|---|---|---|---|---|---|
| `06-C01` | 对 client-executed tools，模型可见 definition / call 与 Host executable implementation 是不同 contract surface；Provider 文档不规定本课程 Registry 的内部结构。 | `CONFIRMED` | `OFFICIAL_DOC` | `CONDITIONAL` | `06-E01` |
| `06-C02` | .NET 10 提供 deterministic full-path、platform-aware relative-path 与 final link / junction target resolution API；这些 API surface 本身不证明任意 containment algorithm 安全。 | `CONFIRMED` | `OFFICIAL_DOC` | `CONDITIONAL` | `06-E02` |
| `06-C03` | .NET cancellation 是 cooperative request；`CancelAfter` 调度 cancellation；`Task.WaitAsync` 的 timeout 与 caller token 是可区分的公开 completion conditions。 | `CONFIRMED` | `OFFICIAL_DOC` | `CONDITIONAL` | `06-E03` |
| `06-C04` | Lab 02 采用 Registry -> Canonicalize -> Validate -> Policy -> Idempotency -> Execute -> Result Validation -> Render / Spill -> Trace，并冻结 `Deny > Ask > Allow`、分离 result views 与本地 JSONL trace。 | `PROPOSAL` | `DESIGN_PROPOSAL` | `SATISFIED_FOR_FIXTURE` | `06-E04` |
| `06-C05` | 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，valid relative read 成功，lexical traversal 与 allow-root 内真实 junction 指向 root 外均在 execute 前被拒绝。 | `CONFIRMED` | `EXPERIMENT` | `SATISFIED` | `06-E05` |
| `06-C06` | 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，课程 Policy v1 的 `DENY` 不被 `ALLOW / ASK` 覆盖；无 `DENY` 但有 `ASK` 时不 execute。 | `CONFIRMED` | `EXPERIMENT` | `SATISFIED` | `06-E06` |
| `06-C07` | 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，timeout test gate 与预取消 caller 分别记录 `TIMED_OUT / TIMEOUT` 和 `CALLER_CANCELLED / CALLER`，均不产生成功 result。 | `CONFIRMED` | `EXPERIMENT` | `SATISFIED` | `06-E07` |
| `06-C08` | 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，valid calculate/read 成功，invalid handler result 停在 result validation；1024-byte 结果 spill 到 Lab-owned temp，Model / UI / Trace view 按课程 Result Contract v1 有界分离。 | `CONFIRMED` | `EXPERIMENT` | `SATISFIED` | `06-E08` |
| `06-C09` | 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，同 invocation ID / 同 canonical args replay 而不二次执行，同 ID / 不同 args conflict；每个 invocation 追加一行 JSONL，两次 fresh run 的 trace byte-identical。 | `CONFIRMED` | `EXPERIMENT` | `SATISFIED` | `06-E09` |

## Source Manifest

网页来源均于 `2026-08-20（Asia/Shanghai）` 实际打开；Lab 证据均为当前 working tree 内的 durable raw artifact，尚未形成 Git checkpoint。

| ID | Source | Retrieved / Version Scope | Access / Hash | Used by |
|---|---|---|---|---|
| `S-01` | [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling) | current hosted guide；2026-08-20 | `OPENED_CURRENT` | C01 |
| `S-02` | [Microsoft Path.GetFullPath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath?view=net-10.0) | `.NET 10` view；2026-08-20 | `OPENED_CURRENT` | C02 |
| `S-03` | [Microsoft Path.GetRelativePath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=net-10.0) | `.NET 10` view；2026-08-20 | `OPENED_CURRENT` | C02 |
| `S-04` | [Microsoft Directory.ResolveLinkTarget](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-10.0) | `.NET 10` view；2026-08-20 | `OPENED_CURRENT` | C02 |
| `S-05` | [Microsoft Cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) | current .NET guidance；2026-08-20 | `OPENED_CURRENT` | C03 |
| `S-06` | [Microsoft CancellationTokenSource.CancelAfter](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelafter?view=net-10.0) | `.NET 10` view；2026-08-20 | `OPENED_CURRENT` | C03 |
| `S-07` | [Microsoft Task<TResult>.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1.waitasync?view=net-10.0) | `.NET 10` view；2026-08-20 | `OPENED_CURRENT` | C03 |
| `R-01` | `content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md` | published Article 03 | `READ_LOCAL` | C04, C08 |
| `R-02` | `content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md` | published Article 05 | `READ_LOCAL` | C01, C04 |
| `L-01` | [Lab 02 frozen Design and appended Observation](../../labs/lab-02-tool-runtime/README.md) | Design v1 + 2026-08-20 run | `READ_LOCAL` | C04—C09 |
| `L-02` | [Frozen fixture manifest](../../labs/lab-02-tool-runtime/fixtures/manifest.md) | exact inputs / Expected | `READ_LOCAL / UNCHANGED` | C05—C09 |
| `L-03` | [Execution summary](../../labs/lab-02-tool-runtime/artifacts/logs/execution.md) | Windows `10.0.19045` / SDK `10.0.301` | SHA-256 `8E070EF2793B81F14D664E9206088C132D68789C3AE345A3A18C4572089F1521` | C05—C09 |
| `L-04` | [Raw execution log](../../labs/lab-02-tool-runtime/artifacts/logs/execution.raw.log) | accepted commands + 3 preserved failures | SHA-256 `492C290405244289D8F2509866942FDB0061F672103257D2977BCA049EB7E639` | C05—C09 |
| `L-05a` | [First JSONL trace](../../labs/lab-02-tool-runtime/artifacts/observation-first.jsonl) | 10607 bytes / 14 LF rows | SHA-256 `50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67` | C05—C09 |
| `L-05b` | [Second JSONL trace](../../labs/lab-02-tool-runtime/artifacts/observation.jsonl) | 10607 bytes / 14 LF rows | SHA-256 `50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67` | C05—C09 |
| `L-06` | [First](../../labs/lab-02-tool-runtime/artifacts/result-views-first.json) / [second](../../labs/lab-02-tool-runtime/artifacts/result-views-second.json) result views | each 5967 bytes / 14 views | both SHA-256 `5BD9F3452085153D6B87D735F0547D9505CC6BF746ECD4C3DC4FC0C980D6B638` | C08 |
| `L-07` | [First](../../labs/lab-02-tool-runtime/artifacts/run-state-first.json) / [second](../../labs/lab-02-tool-runtime/artifacts/run-state-second.json) run-state | two fresh roots / real junction | SHA-256 `CE3DC763E76BE6F61E1384FDEB47CFB688522902B4C8AEF454F878A0D1BDE542` / `107CAFA66DE18F0F6D28E28C1034ADA854CB44A39D7700EBF91CBC689898B5A3` | C05 |
| `L-08` | [First](../../labs/lab-02-tool-runtime/artifacts/spills/first/26ad8132e3b544caefd85b30bf36df8d012dc7245c9d2224e0f9f50a2ac55a61.txt) / [second](../../labs/lab-02-tool-runtime/artifacts/spills/second/26ad8132e3b544caefd85b30bf36df8d012dc7245c9d2224e0f9f50a2ac55a61.txt) spill | each 1024 bytes | both SHA-256 `26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61` | C08 |
| `L-09` | [Runtime](../../labs/lab-02-tool-runtime/src/ToolRuntimeLab/ToolRuntime.cs) / [specs](../../labs/lab-02-tool-runtime/tests/ToolRuntimeLab.Specs/Program.cs) | `net10.0` / BCL-only | working tree source anchors | C05—C09 |

## Evidence Cards

### Evidence 06-E01｜Definition and Host implementation seam

- Article: `06 Tool Runtime`
- Claim ID: `06-C01`
- Claim: 对 client-executed tools，模型可见 definition / call 与 Host executable implementation 是不同 contract surface；Provider 文档不规定本课程 Registry 的内部结构。
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `current official Provider documentation`
- Source: `S-01`；local dependency `R-02`
- Repository: `N/A`
- Commit: `N/A / hosted docs`
- File: `N/A`
- Symbol: `N/A`
- Call Path: `tools in request -> model tool call -> application-side execution -> tool output`
- Experiment: `N/A for document claim`
- Fixture: `N/A`
- Trace: `N/A`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `OpenAI current Function Calling guide；client-executed function tools only`
- Reproduction: 打开 `S-01`，核对 definition、call、application-side execution 与 output return 的分步合同。
- Observation: 官方 flow 把模型可见 Tool contract 与 application-side executable step 分开。
- Counter-evidence Searched: built-in / server-executed tools 的 execution owner 可在 Provider 侧。
- Interpretation: client-executed tool 需要 Host seam；用 Registry 承载该 seam 是课程设计。
- Proves: `Tool Call != Executed`；definition / call 与 executable implementation 可独立建模。
- Does Not Prove: 所有 Tool 都本地执行；Registry 必须采用某个类或 DI 形状。
- Limitations: 只覆盖一家 Provider 的 current client-tool contract；hosted docs 会变化。
- Course Usage: Article 06 Definition / Registry 边界，使用确定但收窄的语态。
- BuildPilot Implication: `DEFER` + Article 42 才回收 Capability / Tool design。
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `CONDITIONAL`；seam 不依赖 Lab，Registry behavior 依赖。
- Allowed Wording: “对 client-executed tools，模型可见 definition 与 Host implementation 是不同责任面。”
- Stop Line: 不得写成“所有 Tool 都在本地 Host 执行”或“官方规定了本课程 Registry”。

### Evidence 06-E02｜.NET lexical path and link-target surfaces

- Article: `06 Tool Runtime`
- Claim ID: `06-C02`
- Claim: .NET 10 提供 deterministic full-path、platform-aware relative-path 与 final link / junction target resolution API；这些 API surface 本身不证明任意 containment algorithm 安全。
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official runtime API documentation`
- Source: `S-02`、`S-03`、`S-04`
- Repository: `N/A / Microsoft Learn`
- Commit: `N/A`
- File: `System.IO.Path`、`System.IO.Directory`
- Symbol: `Path.GetFullPath(string,string)`、`Path.GetRelativePath(string,string)`、`Directory.ResolveLinkTarget(string,bool)`
- Call Path: `relative input -> full path / relative containment；link path -> final target`
- Experiment: `Lab 02 TR-02 / TR-03 / TR-04 supports C05`
- Fixture: `L-02`
- Trace: `L-05a / L-05b`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `.NET 10 API view`
- Reproduction: 打开三份 Microsoft Learn API 页，核对 base-path overload、platform comparison 和 junction/final-target support。
- Observation: API contract 提供 lexical canonicalization 与 final link/junction target resolution primitives。
- Counter-evidence Searched: lexical normalization 不解析 ancestor link；API presence 不消除 authorization race / TOCTOU。
- Interpretation: 这些 API 足以作为本 Lab 构件，但不能单独推出任意安全 walker。
- Proves: .NET 10 有本篇需要的公开 path / link surface。
- Does Not Prove: 只调用 `GetFullPath` 即可挡住 junction；production sandbox 或 TOCTOU 安全成立。
- Limitations: hosted docs；未 pin runtime source；行为结论单列为 C05。
- Course Usage: Path Design 的 document contract。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `CONDITIONAL` for API；C05 另由 Lab 支撑。
- Allowed Wording: “.NET 10 提供 lexical canonicalization 与 link/junction target resolution 构件。”
- Stop Line: 不得写“调用 GetFullPath 就已经阻止 junction escape”。

### Evidence 06-E03｜Cancellation and timeout are distinct contracts

- Article: `06 Tool Runtime`
- Claim ID: `06-C03`
- Claim: .NET cancellation 是 cooperative request；`CancelAfter` 调度 cancellation；`Task.WaitAsync` 的 timeout 与 caller token 是可区分的公开 completion conditions。
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: `official runtime guidance and API documentation`
- Source: `S-05`、`S-06`、`S-07`
- Repository: `N/A / Microsoft Learn`
- Commit: `N/A`
- File: `System.Threading`、`System.Threading.Tasks`
- Symbol: `CancellationToken`、`CancellationTokenSource.CancelAfter`、`Task<TResult>.WaitAsync`
- Call Path: `caller token / timeout budget -> cooperative listener or wait completion`
- Experiment: `Lab 02 TR-07 / TR-08 supports C07`
- Fixture: `L-02`
- Trace: `L-05a / L-05b`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `.NET 10 API view + current managed-cancellation guidance`
- Reproduction: 打开三份 Microsoft Learn 页面，核对 cooperative cancellation、scheduled cancel 与 timeout / token completion contracts。
- Observation: cancellation request、`CancelAfter` scheduling 与 timeout/caller-token completion branches 是不同公开合同。
- Counter-evidence Searched: 请求取消不保证 handler 停止；wrapper 可以丢失 source identity。
- Interpretation: Tool Runtime 应保留 cancellation source；具体 fixture behavior 单列为 C07。
- Proves: cancellation 不是强制线程终止，timeout 与 caller cancellation 不应混为一个原因。
- Does Not Prove: 任意 Tool 会及时停止；精确 deadline 或 process termination。
- Limitations: 不覆盖第三方不可取消代码与 production scheduling。
- Course Usage: Execute cancellation / timeout contract。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `CONDITIONAL` for API；C07 另由 Lab 支撑。
- Allowed Wording: “.NET contract 区分 cooperative cancellation request 与 timeout completion condition。”
- Stop Line: 不得写“CancelAfter 会强制杀死 Tool”。

### Evidence 06-E04｜Course Tool Runtime v1 design

- Article: `06 Tool Runtime`
- Claim ID: `06-C04`
- Claim: Lab 02 采用 Registry -> Canonicalize -> Validate -> Policy -> Idempotency -> Execute -> Result Validation -> Render / Spill -> Trace，并冻结 `Deny > Ask > Allow`、分离 result views 与本地 JSONL trace。
- Evidence Status: `PROPOSAL`
- Evidence Class: `DESIGN_PROPOSAL`
- Source Type: `course Lab Design`
- Source: `L-01`、`L-02`；runtime conformity见 `L-03`—`L-09`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A / working tree`
- File: `docs/agent-engineering-course/labs/lab-02-tool-runtime/README.md`
- Symbol: `Lab Design v1 / Course Policy v1 / Result Contract v1 / Trace Schema v1`
- Call Path: `Call -> Registry -> Canonicalize -> Validate -> Policy -> Idempotency -> Execute -> Validate Result -> Render / Spill -> Append Trace`
- Experiment: `Lab 02 executed；normative design Claim remains proposal`
- Fixture: `Calculator + ReadOnlyFileTool / lab-02-design-v1`
- Trace: `L-05a / L-05b`
- Retrieved / Run At: `Design frozen and run 2026-08-20（Asia/Shanghai）`
- Version Scope: `Course Lab 02 Design v1 only`
- Reproduction: 读取 frozen Design / Expected，再对照 Lab execution 与 raw artifacts。
- Observation: 实现与 12-case / 14-row frozen contract 相符；三次失败尝试及 allowed patches保留。
- Counter-evidence Searched: 其他 Provider、framework、policy、OpenTelemetry 与 sandbox 可采用不同 stages / names / merge semantics。
- Interpretation: runtime conformity支持 C05—C09，不把课程规范性选择变成描述性行业标准。
- Proves: 本课程 fixture 可沿该设计产生可判定 evidence。
- Does Not Prove: 该 pipeline、Policy 或 Trace Schema 是行业标准或 production architecture。
- Limitations: two-tool local fixture；无 shell / network / production credential / business write。
- Course Usage: 始终使用“课程设计 / 本 Lab 采用”的 proposal 语态。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `SATISFIED_FOR_FIXTURE`；规范性选择仍是 `PROPOSAL`。
- Allowed Wording: “Lab 02 冻结并实现了这条课程管线 / 课程 policy。”
- Stop Line: 不得写“业界 Tool Runtime 都采用这条 pipeline”。

### Evidence 06-E05｜Path traversal and real junction escape

- Article: `06 Tool Runtime`
- Claim ID: `06-C05`
- Claim: 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，valid relative read 成功，lexical traversal 与 allow-root 内真实 junction 指向 root 外均在 execute 前被拒绝。
- Evidence Status: `CONFIRMED`
- Evidence Class: `EXPERIMENT`
- Source Type: `required local Lab + raw runtime artifacts`
- Source: `L-01`—`L-05`、`L-07`、`L-09`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A / working tree`
- File: `ToolRuntime.cs`、`Program.cs`、`run-state-*.json`、`observation*.jsonl`
- Symbol: `ToolRuntime.Canonicalize`、`ValidateLinkState`、`TR-02 / TR-03 / TR-04`
- Call Path: `relative path -> lexical containment -> component link resolution -> resolved containment -> read or reject`
- Experiment: `TR-02 / TR-03 / TR-04；two fresh runs`
- Fixture: `small.txt + outside/secret.txt + real JUNCTION link-out`
- Trace: `L-05a / L-05b；both SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67；run-state hashes见L-07`
- Retrieved / Run At: `2026-08-20T07:12:44+08:00 through 2026-08-20T07:15:03+08:00`
- Version Scope: `Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301 / single process / no concurrent link mutation`
- Reproduction: 按 `L-03` accepted setup/run/cleanup 命令；两次 setup 均创建并用 `ResolveLinkTarget(true)` 核对真实 junction。
- Observation: Expected：TR-02 `SUCCEEDED/OK`，TR-03 `CANONICALIZE/PATH_OUTSIDE_ROOT`，TR-04 `CANONICALIZE/PATH_LINK_OUTSIDE_ROOT`，两个拒绝 case handler=0。Observed：两份 trace逐项 exact match；TR-02为11 bytes及manifest SHA，TR-03/04 later stages=`NOT_RUN`。
- Counter-evidence Searched: 两个run-state显示不同fresh root；junction final target均在allow-root外、owned run-root内；setup首次失败完整保留。
- Interpretation: 本 fixture 的 lexical traversal和固定真实 junction escape均在execute前被拒绝，valid read不受影响。
- Proves: 当前 frozen Windows fixture在无 concurrent link mutation时符合 C05。
- Does Not Prove: TOCTOU、handle-based confinement、其他OS/filesystem、任意path walker或production sandbox安全。
- Limitations: ASCII fixture；junction目标仍在Lab-owned run-root；不并发替换link；未覆盖symlink fallback分支。
- Course Usage: Path canonicalization与resolved-target recheck的最小行为例；必须连同环境和TOCTOU限制使用。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `SATISFIED`
- Allowed Wording: “在 Lab 02 的固定 Windows fixture中，traversal与真实junction escape都在execute前被拒绝。”
- Stop Line: 不得外推为“该算法消除了所有路径逃逸或TOCTOU”。

### Evidence 06-E06｜Course Policy v1 conflict behavior

- Article: `06 Tool Runtime`
- Claim ID: `06-C06`
- Claim: 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，课程 Policy v1 的 `DENY` 不被 `ALLOW / ASK` 覆盖；无 `DENY` 但有 `ASK` 时不 execute。
- Evidence Status: `CONFIRMED`
- Evidence Class: `EXPERIMENT`
- Source Type: `required local Lab over a course design proposal`
- Source: `L-01`—`L-05`、`L-09`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A / working tree`
- File: `ToolRuntime.cs`、`Program.cs`、`observation*.jsonl`
- Symbol: `PolicyMerger.Merge`、`TR-05 / TR-06`
- Call Path: `global + tool + resource -> merge -> DENY / ASK -> terminal trace`
- Experiment: `TR-05 / TR-06；two fresh runs`
- Fixture: `ALLOW/ASK/DENY` and `ALLOW/ASK/ALLOW`
- Trace: `L-05a / L-05b；both SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `Course Policy v1 / Windows 10.0.19045 / SDK 10.0.301 / single process / no concurrent link mutation`
- Reproduction: 按 `L-03` 执行两次 spec run，读取 TR-05 / TR-06 的 policy inputs、decision、terminal与handler count。
- Observation: Expected：TR-05=`DENY / POLICY_DENIED`，TR-06=`ASK / APPROVAL_REQUIRED`，均execute=`NOT_RUN`、handler=0。Observed：两份trace均exact match，later result/render stages也为`NOT_RUN`。
- Counter-evidence Searched: 其他policy系统可能使用specificity、first-match、priority或真人override；本Lab没有真人approval resume。
- Interpretation: 实现符合课程 Policy v1，但只证明这个proposal在本fixture中的行为。
- Proves: C06 的 narrow fixture behavior。
- Does Not Prove: 行业统一merge规则、Policy v1最优、production approval flow或Article 19 permission model。
- Limitations: 只覆盖两个conflict tuple；`ASK`返回terminal，不等待真人输入。
- Course Usage: deny / approval gate 的可审计terminal；Policy顺序保持课程语态。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `SATISFIED`
- Allowed Wording: “课程 Policy v1 的两个冲突case在Lab 02中均按frozen rule终止且未execute。”
- Stop Line: 不得称为行业标准或完整 HITL 系统。

### Evidence 06-E07｜Timeout versus caller cancellation behavior

- Article: `06 Tool Runtime`
- Claim ID: `06-C07`
- Claim: 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，timeout test gate 与预取消 caller 分别记录 `TIMED_OUT / TIMEOUT` 和 `CALLER_CANCELLED / CALLER`，均不产生成功 result。
- Evidence Status: `CONFIRMED`
- Evidence Class: `EXPERIMENT`
- Source Type: `required local Lab + raw runtime artifacts`
- Source: `L-01`—`L-05`、`L-09`；document contracts `S-05`—`S-07`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A / working tree`
- File: `ToolRuntime.cs`、`Program.cs`、`observation*.jsonl`
- Symbol: `ToolRuntime.InvokeAsync`、`TR-07 / TR-08`
- Call Path: `caller token + CancelAfter timeout source -> linked token / precheck -> terminal classification -> trace`
- Experiment: `TR-07 never-release gate / 50ms；TR-08 caller pre-cancelled / 5000ms`
- Fixture: `test-only cooperative execution gate`
- Trace: `L-05a / L-05b；both SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `Windows 10.0.19045 / SDK 10.0.301 / single process / no concurrent link mutation / cooperative test gate`
- Reproduction: 按 `L-03` 执行两次spec run，不以wall-clock elapsed time作为判据，只核对source、terminal、handler与result stages。
- Observation: Expected：TR-07=`EXECUTE/TIMED_OUT`、origin=`TIMEOUT`、handler=1；TR-08=`EXECUTE/CALLER_CANCELLED`、origin=`CALLER`、handler=0；两者result/render=`NOT_RUN`。Observed：两份trace逐项exact match且无成功result。
- Counter-evidence Searched: cancellation是cooperative；test gate不是第三方慢I/O；并发触发两个source的race未覆盖。
- Interpretation: 本fixture保留了timeout source与caller source，未压成单一`CANCELLED`。
- Proves: C07 的 narrow test-gate behavior。
- Does Not Prove: 强制线程终止、精确deadline、underlying第三方work已停止或production latency。
- Limitations: 不覆盖同时触发、真实慢I/O、process isolation与忽略token的handler。
- Course Usage: Execute failure taxonomy实例；必须说明cooperative boundary。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `SATISFIED`
- Allowed Wording: “固定test gate中，timeout与caller cancellation产生了不同terminal和origin。”
- Stop Line: 不得写成“timeout强制杀死Tool”或推广到所有handler。

### Evidence 06-E08｜Valid execution, result validation and spill views

- Article: `06 Tool Runtime`
- Claim ID: `06-C08`
- Claim: 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，valid calculate/read 成功，invalid handler result 停在 result validation；1024-byte 结果 spill 到 Lab-owned temp，Model / UI / Trace view 按课程 Result Contract v1 有界分离。
- Evidence Status: `CONFIRMED`
- Evidence Class: `EXPERIMENT`
- Source Type: `required local Lab + raw trace / result-view / spill artifacts`
- Source: `L-01`—`L-06`、`L-08`、`L-09`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A / working tree`
- File: `ToolRuntime.cs`、`Program.cs`、`observation*.jsonl`、`result-views-*.json`、`artifacts/spills/*`
- Symbol: `ValidateResult`、`Render`、`ValidateSpecialCases`、`CopyAndValidateSpillEvidence`、`TR-01 / 02 / 09 / 10`
- Call Path: `execute -> candidate result -> result validation -> inline or spill -> model/ui/trace views`
- Experiment: `TR-01 / TR-02 / TR-09 / TR-10；two fresh runs`
- Fixture: `2+3；11-byte small.txt；invalid result kind；1024-byte large.txt；64-byte threshold`
- Trace: `L-05a / L-05b both SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67；L-06 both 5BD9F3452085153D6B87D735F0547D9505CC6BF746ECD4C3DC4FC0C980D6B638；L-08 both 26AD8132E3B544CAEFD85B30BF36DF8D012DC7245C9D2224E0F9F50A2AC55A61`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `Course Result Contract v1 / Windows 10.0.19045 / SDK 10.0.301 / single process / no concurrent link mutation / ASCII fixture`
- Reproduction: 按 `L-03` 执行两次spec run；对照trace、result views和两份保留spill的bytes/hash。
- Observation: Expected：TR-01 value=5、TR-02 11 bytes/small SHA；TR-09 execute=`PASS`后`RESULT_SCHEMA_INVALID`且render/cache不发生；TR-10=`SPILLED`、1024 bytes/large SHA、Model preview<=64 bytes、relative ref。Observed：两次exact match；result views各14项且同hash；两份spill均1024 bytes且SHA=`26AD...55A61`；Trace不含preview/full content或absolute temp path。
- Counter-evidence Searched: result views和trace扫描absolute path/full content；spec断言invalid result未进入cache；spill是Host internal write。
- Interpretation: 本fixture在result validation之后才render/cache，并把大结果full bytes与Model/UI/Trace view分离。
- Proves: C08 的 narrow Result Contract v1 behavior。
- Does Not Prove: production最佳large-result策略、敏感内容安全、binary/encoding通用性或真实模型消费效果。
- Limitations: ASCII fixture；64/4096阈值是课程设计；spill只在unique Lab temp；不覆盖secret redaction。
- Course Usage: `Result != Model View != UI View != Trace` 的可执行最小例。
- BuildPilot Implication: `N/A`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `SATISFIED`
- Allowed Wording: “Lab 02中，无效result停在validation；1024-byte result只把64-byte preview与metadata暴露给Model view，full bytes留在Lab-owned spill。”
- Stop Line: 不得把spill策略写成行业标准或production敏感信息保证。

### Evidence 06-E09｜Duplicate invocation and append-only deterministic trace

- Article: `06 Tool Runtime`
- Claim ID: `06-C09`
- Claim: 在本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation 条件下，同 invocation ID / 同 canonical args replay 而不二次执行，同 ID / 不同 args conflict；每个 invocation 追加一行 JSONL，两次 fresh run 的 trace byte-identical。
- Evidence Status: `CONFIRMED`
- Evidence Class: `EXPERIMENT`
- Source Type: `required local Lab over narrow in-memory idempotency and course trace proposals`
- Source: `L-01`—`L-05`、`L-09`
- Repository: `TwoEggDu/twoegg-tech-stack`
- Commit: `N/A / working tree`
- File: `ToolRuntime.cs`、`Program.cs`、`observation-first.jsonl`、`observation.jsonl`
- Symbol: `invocationCache`、`JsonlTraceWriter`、`ValidateSpecialCases`、`TR-11 / TR-12`
- Call Path: `invocation id + canonical digest -> execute/cache | replay | conflict -> append JSONL`
- Experiment: `TR-11 / TR-12 + two fresh 14-row runs + CreateNew / append-prefix checks`
- Fixture: `fixed invocation IDs and canonical arguments；single process cache per run`
- Trace: `L-05a / L-05b；10607 bytes each；both SHA-256 50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67；byte-identical=true`
- Retrieved / Run At: `2026-08-20（Asia/Shanghai）`
- Version Scope: `single process / single run idempotency；Course Trace Schema v1；two fresh process determinism check；no concurrent link mutation`
- Reproduction: 每个run从fresh process/root与CreateNew trace开始，按固定14 invocation顺序执行；核对TR-11/12并比较两份raw bytes/hash。
- Observation: Expected：TR-11.2=`REPLAYED`、handler count保持1、result digest与TR-11.1相同；TR-12.2=`IDEMPOTENCY_CONFLICT`、handler count保持1且无result；每run 14 append rows且两trace byte-identical。Observed：两份trace各14 rows/12 groups/14 unique case+attempt/sequence 1..14，TR-11/12 exact match，`create_new=PASS / append_prefix=PASS`，两hash相同且independent byte compare=`true`。
- Counter-evidence Searched: 三次失败历史全部保留；没有覆盖crash、cache eviction、external side effect、concurrent call或distributed retry。
- Interpretation: 本fixture证明narrow in-memory de-dup seam和deterministic append-only artifact，不证明exactly-once。
- Proves: C09 在fixed single-process fixture中的replay/conflict与trace behavior。
- Does Not Prove: 跨进程/跨重启idempotency、transactional side effects、global ordering、distributed exactly-once或并发安全。
- Limitations: Calculator/read-only file无业务写副作用；trace无wall clock；fresh processes只用于artifact determinism，不共享cache。
- Course Usage: 幂等边界与Trace可审计性的最小行为证据。
- BuildPilot Implication: `DEFER`
- Owner: `Article 06 Researcher`
- Verified At: `2026-08-20`
- Lab Dependency: `SATISFIED`
- Allowed Wording: “固定single-process run中，同ID同参数replay、同ID异参数conflict；两次fresh run的14-row JSONL byte-identical。”
- Stop Line: 不得使用“exactly-once”、durable idempotency或distributed replay语态。

## Evidence Merge Gate

| Status | Count | Claim IDs |
|---|---:|---|
| `CONFIRMED` | 8 | `06-C01`—`06-C03`、`06-C05`—`06-C09` |
| `PARTIAL` | 0 | `NONE` |
| `BLOCKED` | 0 | `NONE` |
| `PROPOSAL` | 1 | `06-C04` |

### Acceptance audit

| Requirement | Result | Direct evidence |
|---|---|---|
| Windows `10.0.19045` / win-x64 / SDK `10.0.301` | `PASS` | `L-03` + `dotnet-info.txt` |
| `net10.0` / external PackageReference=0 / accepted restore-build-runs exit 0 | `PASS` | `L-03`、`L-04`、`L-09` |
| real link target outside allow-root | `PASS` | two `JUNCTION` run-states + `ResolveLinkTarget(true)` in `L-04` |
| 12 groups / 14 rows / sequence 1..14 / exact case matrix | `PASS` | `L-05a`、`L-05b` |
| timeout vs caller / policy / invalid result | `PASS` | exact TR-05—TR-09 rows |
| spill bytes/hash and bounded result views | `PASS` | `L-06`、`L-08` |
| replay/conflict / CreateNew / append-prefix | `PASS` | TR-11/12 + `L-04` |
| two trace hashes and byte identity | `PASS` | both `50CEA4EC...21BD67` / `identical=true` |
| three failed attempts retained | `PASS` | RESTORE-01、SETUP-FIRST-01、CLEANUP-FIRST-01 in `L-03` / `L-04` |
| provider/network/credentials/shell/business writes | `0` | both `SPEC_RESULT PASS` rows in `L-04` |

### Recommendation：`PASS / NEXT OUTLINE`

`Experiment -> Observation -> Evidence Interpretation -> Claim Status` 已对 C05—C09逐项完成。所有 required acceptance都有direct raw mapping；未修改 frozen Expected，也未删除失败历史。Author只能按上述 scoped claims与limitations进入 Outline；任何超出fixed fixture、Windows版本、single-process或no-concurrent-link-mutation边界的新核心事实必须返回 Research。

## Stop Line

Evidence Gate=`PASS`，next=`OUTLINE`。C04继续是课程 `PROPOSAL`；C05—C09只在明确scope内为`CONFIRMED`。Researcher不得创建 Outline / Draft / Published Content，不得修改 frozen Design、raw observation、global state、canonical、status或启动 Article 07。
