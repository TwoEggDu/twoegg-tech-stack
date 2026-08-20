# Article 06 Research｜Tool Runtime

- Research Phase：`EVIDENCE_MERGE / EVIDENCE_GATE`
- Research Status：`EVIDENCE_MERGED / PASS`
- Lifecycle Candidate：`EVIDENCE_READY`
- Evidence Gate Recommendation：`PASS / NEXT_OUTLINE`
- Required Lab：`Lab 02｜Tool Runtime`
- Lab Design：`FROZEN`
- Lab Execution / Observation：`EXECUTED / COMPLETE`
- Evidence Merge：`COMPLETE`
- Research Window：`2026-08-20（Asia/Shanghai）`
- Runtime：`Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301 / net10.0`
- External NuGet Packages：`NONE`
- Provider Calls：`0`
- Local Lab Runs / Invocation Trace Rows：`2 / 28`
- Runtime Evidence：`CONFIRMED_WITHIN_FIXTURE`
- Claim Summary：`8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`

> Preliminary Evidence 与 frozen Design 没有在运行后改判据。Researcher重新读取两次完整 Lab run、raw trace、result views、run-state、spill和三次失败历史，再按 `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 合并。没有 Provider call；行为结论只覆盖本 fixture、Windows `10.0.19045`、.NET SDK `10.0.301`、single process、no concurrent link mutation。

## Scope and method

本篇接住 Article 05 已确认的边界：client-executed Tool Call 是模型提出的结构化行动请求，Host 仍需决定怎样 route、reject 或 execute。Article 06 的研究问题不是再讲 Provider payload，而是把这个 Host seam 拆成可拒绝、可取消、可限制结果、可追踪的最小本地执行管线。

来源面保持为一个 Provider contract 加 .NET 10 标准库合同：

1. OpenAI current Function Calling guide 只用于确认 model-visible definition / call 与 application-side code execution 是不同步骤；不把一家 Provider 的字段写成通用 Tool Runtime。
2. Microsoft .NET 10 文档只用于确认 `Path.GetFullPath`、`Path.GetRelativePath`、`Directory.ResolveLinkTarget`、cooperative cancellation、`CancelAfter` 与 `Task.WaitAsync` 的公开合同。
3. Article 03 / 05 published content 用于继承 Parse / Schema / DTO / Domain 与 `Tool Call != Executed` 边界。
4. Policy merge、stage names、result views、spill、idempotency 与 JSONL trace 是本课程 Lab 02 的 design proposal，不宣称是行业标准。
5. 所有行为问题由固定的纯本地 Lab 02判定；frozen Expected、Lab Engineer Observed与Researcher Interpretation始终分离。

未采用 OpenTelemetry。Lab 02 使用自有、版本化的本地 JSONL trace schema，避免把课程字段误写成 OpenTelemetry semantic convention。

## Research Question Answers

| RQ | Status | Answer | Claim / Evidence |
|---|---|---|---|
| `RQ-01` | `ANSWERED / DOCUMENT_CONTRACT` | ToolDefinition 是模型可见的 name / description / input contract；对 client-executed tools，Host 另行保存 name 到 executable handler 与 Host-only metadata 的映射。官方合同支持两者分离，但不规定本课程 Registry 的类形状。 | `06-C01 / 06-E01` |
| `RQ-02` | `ANSWERED_AS_PROPOSAL` | Article 03 已建立 Parse / Schema / DTO / Domain 的首失败边界。本篇采用 `Call -> Registry -> Canonicalize -> Validate -> Policy -> Idempotency -> Execute -> Validate Result -> Render / Spill -> Trace` 作为课程管线；Lab fixture已按此实现并通过，但它仍不是行业标准。 | `06-C04 / 06-E04` |
| `RQ-03` | `CONFIRMED_WITHIN_FIXTURE` | 两次 fresh run 都真实创建 junction并由 `ResolveLinkTarget(true)` 确认其指向 allow-root外；valid read成功，lexical traversal和junction escape分别以 frozen code在execute前拒绝。仍不覆盖TOCTOU或并发link mutation。 | `06-C02, 06-C05 / 06-E02, 06-E05` |
| `RQ-04` | `PROPOSAL + FIXTURE_BEHAVIOR_CONFIRMED` | `Deny > Ask > Allow` 继续是课程 Policy v1 proposal；TR-05 / TR-06两次都按 frozen decision终止且handler=0，不把结果外推成行业policy。 | `06-C04, 06-C06 / 06-E04, 06-E06` |
| `RQ-05` | `CONFIRMED_WITHIN_TEST_GATE` | 两次run中，50ms never-release gate均为 `TIMED_OUT / TIMEOUT`、handler=1；预取消caller均为 `CALLER_CANCELLED / CALLER`、handler=0；两者result/render=`NOT_RUN`。 | `06-C03, 06-C07 / 06-E03, 06-E07` |
| `RQ-06` | `CONFIRMED_WITHIN_SINGLE_PROCESS` | 每个single-process run中，同ID/同参数replay且handler count保持1；同ID/异参数conflict且没有result。它不证明exactly-once、跨进程durable idempotency或副作用安全。 | `06-C09 / 06-E09` |
| `RQ-07` | `CONFIRMED_WITHIN_RESULT_CONTRACT_V1` | valid calculate/read成功；invalid result停在result validation且未render/cache；1024-byte结果spill到Lab-owned temp，Model preview为64 bytes，Trace不含preview/full content/absolute temp path。 | `06-C04, 06-C08 / 06-E04, 06-E08` |
| `RQ-08` | `ANSWERED / EVIDENCE_MERGED` | 两次run各有12 groups / 14 invocation rows，case matrix exact；两份trace均10607 bytes、SHA-256=`50CEA4EC...21BD67`且byte-identical，三次失败尝试完整保留。 | `06-C05`—`06-C09` / `Lab 02` |

## Stable boundary model

### Model-visible definition is not the executable registry

```text
Model-visible ToolDefinition
  name + description + input schema
          ↓ call candidate
Host Registry
  registered name -> executable handler
  + timeout + result limit + resource policy metadata
          ↓ only after local gates
Host implementation
```

OpenAI current docs把“把 definitions 交给模型”“收到 call”“application-side execute code”“回注 output”列为不同步骤。因此可以确认 separation seam；但 `RegistryEntry`、metadata 字段与生命周期是课程设计，不从 Provider 文档反推。

### Course pipeline proposal

```text
Call
  -> Registry Lookup
  -> Canonicalize Arguments
  -> Schema / DTO / Domain Validate
  -> Merge Tool / Resource Policy
  -> Check Invocation Idempotency
  -> Execute with caller token + timeout budget
  -> Validate Result
  -> Render Inline / Spill
  -> Append Trace
```

每一阶段都必须有 `PASS / FAIL / NOT_RUN`，首失败之后不得继续 execute 或 render。Trace 是任何 terminal path 的收尾记录，不代表 Trace 自身是 Evidence；Lab 后仍需 Researcher解释 `Experiment -> Observation -> Evidence Interpretation -> Claim Status`。

## Path boundary

Lab 02 的 ReadOnlyFileTool 只接受 relative path：

1. 固定现有 allow-root 的 fully qualified path；不依赖可变 current directory。
2. 用 `Path.GetFullPath(requested, allowRoot)` 得到 lexical candidate。
3. 用 `Path.GetRelativePath(allowRoot, lexicalCandidate)` 与 Windows platform comparison 拒绝不同 root、rooted result、`..` 或 `../...`。
4. 对 allow-root 到目标的每个现有 component 检查 link / junction，解析 final target，并对 resolved target 重新执行 containment。
5. 只有 lexical 与 resolved containment 都通过才允许只读 open。

这个设计不宣称消除对抗性 TOCTOU：check 后 link target 仍可能被并发替换。Lab 不并发改变 fixture，只验证固定 filesystem topology；生产级 handle-based confinement / sandbox 留在限制中。

### Windows link / junction feasibility and fallback

- Required case 先在唯一 `%TEMP%` fixture 内尝试 PowerShell `Junction`，目标是同一 temp fixture 下 allow-root 外的 sibling directory。
- 若当前 PowerShell / filesystem 不支持 junction，再尝试 `SymbolicLink`；symlink 可能受 Developer Mode、权限或 host policy 限制，禁止为了实验静默提权。
- 只有 link 实际创建成功，且 `.NET Directory.ResolveLinkTarget(..., true)` 确认 final target 在 allow-root 外，才可执行该 case。
- 两种方式都失败时记录 setup failure，Lab 返回 `FAILED_LAB / CLAIMS_REMAIN_BLOCKED`；不得用字符串模拟 link escape，也不得写成已验证。

## Timeout and caller cancellation boundary

.NET 文档支持三个不同事实：cancellation 是 listener 必须配合的 request；`CancelAfter` 只是调度 token source 取消；`WaitAsync(timeout, cancellationToken)` 可以因任务完成、timeout 或 caller token 而结束。Lab 设计据此冻结：

- caller token 在执行前已取消、timeout budget 很长 -> `CALLER_CANCELLED`；
- caller 未取消、test-only execution gate 永不自行完成、短 timeout 到期 -> `TIMED_OUT`；
- Trace 分别记录 `cancellation_origin=CALLER | TIMEOUT`，不把二者压成 `CANCELLED`；
- 不断言任意第三方 handler 会及时停止，也不把 timeout 误写成强制终止线程。

## Result and trace boundary

Lab 02 的四个 view 是课程设计：

| View | Purpose | Full content policy |
|---|---|---|
| Canonical Result | result validation 的输入 | 仅在执行期内存中存在 |
| Model View | 给未来模型回注的有界摘要 | 最多 64 bytes preview + digest / byte count / spill ref |
| UI View | 给宿主 UI 的显示元数据 | inline 或相对 spill ref；不把绝对 temp path当稳定 contract |
| Trace View | 审计 stage / decision / digest | 不保存文件全文、秘密或环境变量值 |

阈值、字段与 spill policy 都只属于 `Lab Trace Schema v1`。Lab 只把大文件 spill 到本次运行的 Lab-owned temp subtree，不写业务文件。

## Duplicate invocation and idempotency boundary

Lab 只验证同一进程、同一 run 内的 narrow contract：

```text
new invocation_id
  -> execute once -> cache canonical args digest + validated result

same invocation_id + same canonical args digest
  -> REPLAYED -> no second handler execution

same invocation_id + different canonical args digest
  -> IDEMPOTENCY_CONFLICT -> no second handler execution
```

Calculator 与 ReadOnlyFileTool 都无业务写副作用，因此这个 case 只能说明 de-dup seam 和 trace 可见性；不能证明真实外部系统的 exactly-once 或跨重启恢复。

## Counter-evidence and limitations

1. Provider built-in / server tools 的执行 owner 可以不在本地 application；本篇只把 Registry / implementation separation 用于 client-executed tools。
2. `Path.GetFullPath` / `GetRelativePath` 是 lexical path 构件，不等于 link-safe authorization；`ResolveLinkTarget` 存在也不自动组成安全 walker。
3. Windows link creation受filesystem、PowerShell、Developer Mode、权限与policy影响；本次两次 accepted setup都真实创建 junction，但不证明其他环境或symlink fallback可用。
4. Cancellation 是 cooperative；token requested 不保证 handler 已经停止。
5. `Deny > Ask > Allow`、spill threshold、trace schema 与 idempotency cache 都是课程设计，不是行业标准。
6. Lab 不覆盖 shell、业务写入、生产 credential、network、Provider、并发 link replacement、跨进程 cache、crash recovery 或生产负载。

## Source Manifest

所有网页均于 `2026-08-20（Asia/Shanghai）` 实际打开核对。Hosted docs 未 pinned；发布前应重新核对 current page 与 `.NET 10` view。

| ID | Primary Source | Retrieved / Version Scope | Used For | Does Not Prove |
|---|---|---|---|---|
| `S-01` | [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling) | current hosted guide；2026-08-20 | tool definitions、call、application-side execution 与 output return 分步 | 不规定本课程 Registry / Policy / Trace；不证明本轮 Provider call |
| `S-02` | [Microsoft Path.GetFullPath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath?view=net-10.0) | `.NET 10` view；2026-08-20 | `GetFullPath(path, basePath)` 的 deterministic fully-qualified surface | 不解析完整 link chain，不证明 containment algorithm 安全 |
| `S-03` | [Microsoft Path.GetRelativePath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=net-10.0) | `.NET 10` view；2026-08-20 | 先 full-path、再按当前平台默认 path comparison 计算 relative path | 不证明只靠 relative path 就能挡住 junction / symlink |
| `S-04` | [Microsoft Directory.ResolveLinkTarget](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-10.0) | `.NET 10` view；2026-08-20 | final target、symbolic link 与 junction resolution surface | 不证明当前环境能创建 link，也不消除 TOCTOU |
| `S-05` | [Microsoft Cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) | current .NET guidance；2026-08-20 | cooperative cancellation、requester / listener distinction、linked token surface | 不证明 handler 一定及时停止 |
| `S-06` | [Microsoft CancellationTokenSource.CancelAfter](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelafter?view=net-10.0) | `.NET 10` view；2026-08-20 | scheduled cancellation and reset semantics | 不等同 `TimeoutException`，不证明 Lab classification |
| `S-07` | [Microsoft Task<TResult>.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1.waitasync?view=net-10.0) | `.NET 10` view；2026-08-20 | task completion / timeout / caller-cancellation completion surface | 不证明 Lab implementation preserves cause |
| `R-01` | `content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md` | published local Article 03 | Parse / Schema / DTO / Domain 与 first-failure boundary | 不证明 Tool Runtime behavior |
| `R-02` | `content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md` | published local Article 05 | client-tool call intent、Host decision、result / Evidence boundary | 不证明 Tool execution occurred |
| `R-03` | `docs/agent-engineering-series-plan.md` + frozen Article 06 review section | canonical + historical frozen detail | scope、required Lab、non-goals、learning questions | 是课程合同，不是行业标准 |
| `L-01` | [`Lab 02 README`](../../labs/lab-02-tool-runtime/README.md) + [`execution.md`](../../labs/lab-02-tool-runtime/artifacts/logs/execution.md) | frozen Design + appended Observation + accepted/failing command history | experiment、environment、failure history、scope | Lab Engineer的pass candidate不是Claim Status |
| `L-02` | [`observation-first.jsonl`](../../labs/lab-02-tool-runtime/artifacts/observation-first.jsonl) + [`observation.jsonl`](../../labs/lab-02-tool-runtime/artifacts/observation.jsonl) | each 10607 bytes / 14 LF rows / SHA-256 `50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67` | C05—C09 exact runtime rows | 不证明超出fixture的production behavior |
| `L-03` | [`result-views-first.json`](../../labs/lab-02-tool-runtime/artifacts/result-views-first.json) + [`result-views-second.json`](../../labs/lab-02-tool-runtime/artifacts/result-views-second.json) + preserved spills | views both SHA-256 `5BD9F345...6B638`；spills both SHA-256 `26AD8132...55A61` | C08 view / spill boundary | 不证明敏感内容或binary安全 |

## Evidence Merge

- Durable Lab：[`Lab 02 Tool Runtime`](../../labs/lab-02-tool-runtime/README.md)
- Fixture Manifest：[`fixtures/manifest.md`](../../labs/lab-02-tool-runtime/fixtures/manifest.md)
- Design Status：`FROZEN / UNCHANGED`
- Execution / Observation / Merge：`COMPLETE / COMPLETE / EVIDENCE_MERGED`
- Required Claims：`06-C05`—`06-C09`
- Environment：`Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301 / net10.0 / external PackageReference=0`
- Raw Trace：两份各14行；SHA-256均为`50CEA4EC1B0D2F15E9789603032AB0BF4DD7F4B0ADDFFC16BBF9733A0321BD67`；byte-identical=`true`。
- Failure History：`RESTORE-01`、`SETUP-FIRST-01`、`CLEANUP-FIRST-01`均保留；allowed patches未改变frozen Expected。

### Claim-by-Claim disposition

| Claim | Expected | Observed / Raw mapping | Interpretation | Disposition |
|---|---|---|---|---|
| `06-C05` | TR-02 valid；TR-03 traversal拒绝；TR-04真实link escape拒绝 | 两份trace逐行exact；两份run-state均为真实`JUNCTION`且final target在allow-root外、owned run-root内 | 在fixed topology、no concurrent mutation条件下，valid read与两类拒绝边界成立；不覆盖TOCTOU/symlink fallback | `CONFIRMED` |
| `06-C06` | TR-05 Deny wins；TR-06 Ask不execute | 两份trace均为`POLICY_DENIED / APPROVAL_REQUIRED`、handler=0、later stages=`NOT_RUN` | 实现符合课程Policy v1；不外推行业merge标准 | `CONFIRMED` |
| `06-C07` | timeout与caller cancellation不同code/origin，均无result | 两份trace中TR-07=`TIMED_OUT/TIMEOUT`、handler=1；TR-08=`CALLER_CANCELLED/CALLER`、handler=0 | test gate保留source identity；不证明强制终止或第三方I/O取消 | `CONFIRMED` |
| `06-C08` | valid calculate/read；invalid result停止；large result spill且views有界 | trace、result views与两份1024-byte spill exact；TR-09未render/cache；Model preview=64 bytes；Trace无全文/absolute temp path | 课程Result Contract v1在ASCII fixture中成立；不外推production内容安全 | `CONFIRMED` |
| `06-C09` | replay/conflict、append-only、两run byte-identical | TR-11/12 exact；`create_new=PASS / append_prefix=PASS`；两trace各14 rows且hash相同/byte-identical | 证明single-process de-dup seam与deterministic artifact，不证明distributed exactly-once | `CONFIRMED` |

### Evidence Gate decision

- Research Questions：`8`；document/design/runtime answers已落盘。
- Claim Register：`9`；`8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。
- Evidence Cards：`9`，一一对应。
- Provider Calls：`0`；Local Lab Runs / Invocation Rows=`2 / 28`。
- Runtime：`CONFIRMED_WITHIN_FIXTURE`。
- Evidence Gate：`PASS`。
- Blocker：`NONE`。
- Next Action：`OUTLINE`。

## Research Stop Line

Researcher在`EVIDENCE_MERGE / EVIDENCE_GATE PASS`后停止。Author下一步只能依据final Claim/Evidence创建Outline；不得把C04写成行业标准，不得越过Windows `10.0.19045`、SDK `10.0.301`、single process、no concurrent link mutation与two-tool/ASCII fixture边界。Researcher不得创建Outline/Draft/Published Content，不修改raw observation、global state、canonical或status，不启动Article 07。
