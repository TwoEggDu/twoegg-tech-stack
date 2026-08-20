# Tool Runtime：Validate、Policy、Execute、Result 与 Trace

- Lifecycle Input：`EVIDENCE_READY`
- Evidence Gate：`PASS`（`8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`）
- Outline Gate：`PASS_RECOMMENDED`（候选；由 Master 核对后决定）
- Article Type：`原理篇（工程机制型） / Lab Article`
- Concept Maturity：`Engineering`
- Course Weight：`L（Major Core Lesson）`
- Target Length：`约 6,500—8,500 中文字`
- Target Reading Time：`20—26 分钟`
- Required Lab：`Lab 02｜Tool Runtime`
- Lab Status：`CONFIRMED / EVIDENCE_MERGED`
- Runtime Evidence：`CONFIRMED_WITHIN_FIXTURE`
- Lab Behavior Scope：`fixed Calculator + ReadOnlyFileTool / ASCII fixture / Windows 10.0.19045 / win-x64 / .NET SDK 10.0.301 / net10.0 / single process / no concurrent link mutation`
- Provider Calls / Network / Credentials：`0 / 0 / 0`

## 1. Article Thesis

> 如果这篇只记一句话：`Tool Call 不能直接跳进函数；Host 必须用一条可拒绝、可取消、可限制结果、可去重并可追踪的 Runtime 管线，把模型行动意图变成受控执行。`

### Type decision

- 采用原理篇（工程机制型）：核心任务是从真实风险建立稳定责任模型，再落到 path、Policy、timeout / cancellation、result、idempotency 与 Lab 02 证据。
- 不采用 API 教程：`.NET` API 只是具体构件，不能取代 Tool Runtime 的抽象模型或安全判断。
- 不采用纯案例篇：Lab 02 用来验证冻结合同，不让实验叙事吞掉全文主线。
- 不采用 Provider 映射篇：Provider 只用于确认 client-executed tool 的 definition / execution seam；本文不比较 Provider payload。

## 2. Reader Change

读者从“给模型一个函数名和 JSON Schema，再按名字调用函数就够了”，转变为能够沿一条可审查的 Host Runtime 管线判断：

1. model-visible ToolDefinition 与 Host executable Registry 为什么是两个责任面；
2. 为什么 `ToolDefinition != function`，也为什么 server-executed / built-in tools 是 execution owner 不在本地 Host 的反例；
3. 为什么 arguments 结构合法仍可能在路径、资源或 Policy 上被拒绝，即 `Schema Valid != Policy Allowed`；
4. Canonicalize、Validate、Policy、Idempotency、Execute、Result Validation、Render / Spill 与 Trace 各自守哪一道边界；
5. timeout 与 caller cancellation 为什么必须保留不同来源，且 cancellation 仍是 cooperative；
6. 为什么 Canonical Result、Model View、UI View 与 Trace View 不能共用一个无界字符串；
7. 为什么 duplicate seam 不等于 exactly-once，Trace 也不自动等于 Evidence；
8. 怎样把 fixed-scope Lab observation 与 production assurance、TOCTOU、Sandbox、Permission 分开。

完成本篇后，读者应能独立审查一个 Tool Runtime 设计，并对每条失败路径回答“在哪一层停止、后续哪些层必须是 `NOT_RUN`、当前证据能证明到哪里”。

## 3. Teaching Spine

| Teaching Phase | Reader Movement | Main Placement | Claim / Evidence |
|---|---|---|---|
| Problem Space | 从“Tool 是普通函数包装”推进到“概率性调用者会带来危险但结构合法的参数、重复副作用，以及取消、结果、审计丢层” | Opening | `06-C01` / `06-E01`、Published Article 05 |
| Abstract Model | 分开 model-visible definition 与 Host Registry，并建立每阶段可 `PASS / FAIL / NOT_RUN` 的完整管线 | Section 1—2 | `06-C01`、`06-C04` / `06-E01`、`06-E04` |
| Concrete Mechanism | 依次落到路径、Policy、timeout / cancellation、result views、idempotency 与 append-only Trace | Section 3—7 | `06-C02`—`06-C09` / `06-E02`—`06-E09` |
| Engineering Judgment | 用 first-failure short-circuit、fail closed、cooperative cancellation、result/evidence 与 sandbox/permission 边界审查设计 | Section 8—10 | `06-C02`—`06-C09` / final Evidence + Lab 02 |
| Verification Boundary | 用两次真实 Lab run、三次 first failure、fixed-scope limitation 与可判定检查题约束结论 | Section 8—11、Closing | `06-C05`—`06-C09` / `06-E05`—`06-E09` |

### L-level scope discipline

- 全文只建立 client-executed Tool 从 call candidate 到 Host execution result 的本地 Runtime 边界。
- C04 的 stage names、Policy v1、result views、spill、idempotency cache 与 JSONL schema 始终标为`课程设计 / PROPOSAL`；Lab 通过只证明 fixture 与这套设计相符，不把它升级成行业标准。
- C05—C09 的每个行为结论都必须就近带上：fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation。
- Lab 只提供最小可判定证据，不把 28 行完整 raw trace 或全部 JSON 字段搬进正文。
- 不开放 shell、业务写文件、network、credential 或生产数据；spill 只是 Host internal Lab artifact。
- MCP、Agent Loop、Permission / Approval、完整 Failure Taxonomy、DSH source 与 BuildPilot 分别停在后续文章入口。

## 4. Opening｜把 Tool 当普通函数包装，会在哪些地方失去控制？

- Problem：最短包装通常是“按模型给的 name 找同名函数，把 JSON arguments 反序列化后直接调用”。这种写法把危险但结构合法的参数、未注册实现、资源约束、重复调用、取消来源、无效结果和审计记录压成一次函数调用。
- Section Goal：从真实工程风险建立问题空间，不从 `Path.GetFullPath`、`CancellationToken` 或 Lab case 列表开场。
- Core Thesis：概率性调用者给出的是行动候选，不是已经获得执行权的普通调用；Runtime 的职责是让每一道边界都能明确拒绝，并让第一次失败之后的执行与渲染保持 `NOT_RUN`。
- Claim IDs：`06-C01`；为 C04—C09 建立问题入口，不在 Opening 提前宣称 Lab behavior。
- Evidence IDs：`06-E01`；Published Article 05 的 `Tool Call != Executed` 与 Host decision seam。
- Misuse Quartet：
  1. 参数 Schema 合法，却用 `../` 或 junction 走出允许目录；
  2. 同一个 invocation 因 retry / duplicate 再次触发真实副作用；
  3. timeout 与 caller cancel 被压成一个异常，系统不知道是谁停止了等待；
  4. handler 返回大结果或错误 shape，却直接塞给模型、UI 和日志。
- Guardrail：这些是问题类型与教学入口，不声称任意 Provider、生产系统或本 Lab 已发生业务副作用。
- Figure：`Figure 1｜普通函数包装 vs Tool Runtime`。
  - 左侧只画 `name + JSON -> function -> string`，在四个丢层位置标红。
  - 右侧只预告“多 Gate、可拒绝、可观测”，不在第一屏展开 API。
- Figure Must Not Imply：课程 Pipeline 是行业唯一结构，或 Tool Runtime 本身等于 Sandbox / Permission 系统。
- Transition：要补回这些丢失的层，先把“模型看见什么”与“Host 实际能执行什么”分开。

## 5. Section 1｜ToolDefinition 不是函数：模型视图与 Host Registry 是两个合同面

- Problem：如果 ToolDefinition 直接持有 executable function，模型可见描述、Host-only timeout、result limit、resource policy 与 handler 生命周期会混成一个对象；反过来，如果只保留 schema，又找不到可审计的 executable owner。
- Section Goal：建立全文第一个抽象边界：model-visible definition / call candidate 与 Host Registry / implementation separation。
- Core Thesis：对 **client-executed tools**，模型可见的 name、description、input contract 与 Host 保存的 registered name → executable handler + Host-only metadata 是不同责任面；Provider 文档支持 application-side execution seam，但不规定本课程 `RegistryEntry` 的类形状。
- Claim IDs：`06-C01`
- Evidence IDs：`06-E01` / `S-01`；Published Article 05。
- Abstract Model：

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

- Counter-evidence：OpenAI built-in tools 以及其他 Provider-managed / server-executed tools 表明 execution owner 可以位于 Provider infrastructure；因此不能写“所有 Tool 都由本地 Host 执行”。
- Guardrail：`ToolDefinition != function` 的意思是二者不是同一责任面，不是否认 Registry 最终需要关联可执行实现；也不把 Registry 内部结构写成官方标准。
- Example：Calculator 与 ReadOnlyFileTool 各画一张双面卡：正面是 model-visible input contract，背面是 Host-only handler、timeout、result kind / size 和 resource metadata。
- Example Label：`COURSE RESPONSIBILITY SKETCH / CLIENT-EXECUTED SCOPE / NOT PROVIDER WIRE SCHEMA`
- Figure：`Figure 2｜Definition / Registry 双视图`。
- Figure Must Not Imply：Provider 看见 Host-only metadata，或 server tool 必须进入本地 Registry。
- Transition：双视图解决“谁看见什么”，仍未回答 call 如何逐层变成一次受控 execution；下一节给出全文唯一主链。

## 6. Section 2｜抽象模型：每个 Stage 都要能停止，第一次失败之后必须是 NOT_RUN

- Problem：只有 `try { handler(args) } catch { ... }` 时，路径拒绝、Policy、timeout、结果错误与 duplicate 都会落入同一个异常桶，无法知道副作用是否发生、结果是否可用、后续是否误跑。
- Section Goal：建立一条不依赖具体类名的最小 Runtime Pipeline，并说明 Trace 是每条 terminal path 的收尾记录。
- Core Thesis：本课程采用下面的 Tool Runtime v1 管线作为 **C04 design proposal**；Lab 02 实现与该设计相符，不代表行业统一 stage、顺序、Policy 或 Trace schema。
- Claim IDs：`06-C04（PROPOSAL）`
- Evidence IDs：`06-E04`；Lab 02 frozen Design。
- Course Pipeline Proposal：

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

- Stage Contract：每个 stage 显式为 `PASS / FAIL / NOT_RUN`；early terminal 后不能继续 execute、result validation 或 render；Trace 追加 terminal record，但不会把 trace 自动升级成 Evidence。
- Four Boundaries：
  - `Schema Valid != Policy Allowed`
  - `ToolDefinition != function`
  - `Result != Evidence`
  - `Sandbox != Permission`
- Counter-evidence：其他 framework 可以合并、拆分、重命名或远程承载这些阶段；OpenTelemetry 也可采用不同 semantic conventions。本篇没有证据支持唯一行业 Pipeline。
- Guardrail：所有“应该 / 采用 / 顺序”都标明是课程设计判断；行为结论只在后续 C05—C09 fixed-scope 段落陈述。
- Figure：`Figure 3｜Call 到 Trace 的 first-failure Pipeline`。
  - 主线显示 10 个阶段。
  - 每个失败分支落入 terminal + Append Trace。
  - 用灰色显示后续 `NOT_RUN`，避免图示暗示失败后仍执行。
- Figure Must Not Imply：Trace 是完整 Failure Taxonomy、Evidence Contract 或 production observability stack。
- Transition：管线建立后，先看 execute 之前最容易被“Schema 已通过”掩盖的两类拒绝：路径边界与 Policy。

## 7. Section 3｜Execute 前（一）：路径 Canonicalize 为什么不等于一次字符串前缀检查？

- Problem：`relative_path` 通过 Schema 只说明它是非空字符串；`GetFullPath` 得到完整路径也只完成 lexical canonicalization。`..`、不同 root 与 allow-root 内 junction 指向 root 外是不同风险。
- Section Goal：把 path API surface、课程 path decision 与 Lab 02 fixed-scope behavior 分三层讲清。
- Document Thesis：.NET 10 提供 `Path.GetFullPath(path, basePath)`、platform-aware `Path.GetRelativePath` 与 `Directory.ResolveLinkTarget(..., true)` 等构件；这些 API surface 本身不证明任意 containment algorithm 安全。
- Lab Behavior Thesis：在 **fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation** 条件下，valid relative read 成功，lexical traversal 与 allow-root 内真实 junction 指向 root 外都在 execute 前被拒绝。
- Claim IDs：`06-C02`、`06-C05`
- Evidence IDs：`06-E02`、`06-E05`；Lab TR-02 / TR-03 / TR-04、two run-state files。
- Minimal Mechanism Sketch：

```text
fix fully-qualified allowRoot
candidate = GetFullPath(relativePath, allowRoot)
reject if GetRelativePath(allowRoot, candidate) escapes
for each existing component:
  resolve link / junction final target
  reject if resolved target escapes
open read-only only after both checks pass
```

- Concrete Observation Plan：正文只保留一张三行表：

| Case | Fixed-scope terminal | Handler | What it demonstrates |
|---|---|---:|---|
| valid `small.txt` | `SUCCEEDED / OK` | 1 | 正常 read 不被边界检查误拒绝 |
| `../outside/secret.txt` | `CANONICALIZE / PATH_OUTSIDE_ROOT` | 0 | lexical escape 在 execute 前终止 |
| `link-out/secret.txt` | `CANONICALIZE / PATH_LINK_OUTSIDE_ROOT` | 0 | 真实 junction resolved-target escape 在 execute 前终止 |

- Fixed-scope Caption：表中三个行为只覆盖 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation。
- Counter-evidence：check 完成后 link target 仍可能被并发替换；API presence 不消除 TOCTOU，也不等于 handle-based confinement、production Sandbox 或跨 OS 安全。
- Guardrail：禁止写“`GetFullPath` 可防目录逃逸”“这套 walker 消除了所有 path traversal / link escape”或“Lab 证明生产 Sandbox 安全”。
- Figure：`Figure 4｜Lexical Path 与 Resolved Target 两次 containment`，在 check/open 之间明确画出 `TOCTOU NOT COVERED`。
- Transition：路径回答“目标资源在哪”，Policy 还要回答“当前调用能否用这项资源”；两个判断不能互相代替。

## 8. Section 4｜Execute 前（二）：Schema Valid 不等于 Policy Allowed

- Problem：参数 shape、资源位置与执行许可是不同判断。即使 path 留在 allow-root，global、tool 与 resource 决策仍可能冲突；若用“最后一个配置覆盖前面”会让 deny 被无意冲掉。
- Section Goal：展示 fail-closed Policy gate，并严格区分课程规范性选择与 fixture behavior。
- Proposal Thesis：`Deny > Ask > Allow` 是 **Course Policy v1 / C04 PROPOSAL**，不是行业统一 merge rule；其他系统可以使用 specificity、priority、first-match 或真人 override。
- Lab Behavior Thesis：在 **fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation** 条件下，课程 Policy v1 的 `ALLOW + ASK + DENY` 终止为 `POLICY_DENIED`，`ALLOW + ASK + ALLOW` 终止为 `APPROVAL_REQUIRED`，两者 handler 都为 `0` 且后续 stages 为 `NOT_RUN`。
- Claim IDs：`06-C04（PROPOSAL）`、`06-C06`
- Evidence IDs：`06-E04`、`06-E06`；Lab TR-05 / TR-06。
- Minimal Policy Table：

| Inputs | Course Policy v1 decision | Fixed-scope terminal | Execute |
|---|---|---|---|
| `ALLOW / ASK / DENY` | `DENY` | `POLICY / POLICY_DENIED` | `NOT_RUN` |
| `ALLOW / ASK / ALLOW` | `ASK` | `POLICY / APPROVAL_REQUIRED` | `NOT_RUN` |

- Fixed-scope Caption：表中两个行为只覆盖 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation，并只说明实现符合课程 Policy v1。
- Guardrail：`APPROVAL_REQUIRED` 只是本 Lab terminal code；不等待真人、不设计 resume，也不进入 Article 19 的 Permission / Approval / HITL 系统。
- Boundary Sentence：`Schema Valid != Policy Allowed`；`Policy Allowed` 也不自动证明 execution 成功或 result 可信。
- Figure：不再新增流程图；把 Policy 两行表嵌在 Figure 3 的 gate 旁，控制视觉数量。
- Transition：越过 Registry、Canonicalize、Validate 与 Policy 后，execution 仍要保留“谁要求停止”和“是否真的停止”的差别。

## 9. Section 5｜Execute 中：timeout 与 caller cancellation 为什么不能压成一个 CANCELLED？

- Problem：统一抛出 `OperationCanceledException` 会丢失 caller 已取消、timeout budget 到期、handler 是否进入以及 underlying work 是否停下等不同事实。
- Section Goal：先建立 .NET cooperative cancellation contract，再用 fixed test gate 说明 Runtime 怎样保留 source identity。
- Document Thesis：.NET cancellation 是 requester / listener cooperative contract；`CancelAfter` 调度 cancellation request；`Task.WaitAsync` 的 timeout 与 caller token 是可区分的 completion conditions。它们都不保证任意第三方 handler 及时停止。
- Lab Behavior Thesis：在 **fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation** 条件下，50ms never-release cooperative test gate 记录 `TIMED_OUT / TIMEOUT` 且 handler count=`1`；预取消 caller 记录 `CALLER_CANCELLED / CALLER` 且 handler count=`0`；两者都没有 success result，result validation / render 为 `NOT_RUN`。
- Claim IDs：`06-C03`、`06-C07`
- Evidence IDs：`06-E03`、`06-E07`；Lab TR-07 / TR-08。
- Minimal Execution Sketch：

```text
caller token ─┐
              ├─ linked cooperative token -> handler / test gate
timeout CTS ──┘

terminal keeps origin = CALLER | TIMEOUT
```

- Fixed-scope Caption：上述两条 terminal behavior 只覆盖 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation，以及 test-only cooperative gate。
- Counter-evidence：test gate 不是真实慢 I/O；未覆盖 caller 与 timeout 同时触发、忽略 token 的 handler、process isolation 或精确 deadline。
- Guardrail：禁止写“timeout 强制杀死 Tool”“CancelAfter 会终止线程”“terminal 返回后 underlying work 必然停止”。
- Figure：`Figure 5｜Caller cancellation 与 timeout source 分流`，显示“等待停止”与“工作已停止”不是同一断言。
- Transition：execution 返回也不是终点；Runtime 还必须验证 result，决定向不同消费者暴露多少内容。

## 10. Section 6｜Execute 后：Result 为什么要先验证，再分成 Model / UI / Trace View？

- Problem：handler 返回一个 object 或 string，团队容易直接把同一份内容塞给模型、UI 和日志；无效 shape 会越过 validation，大结果或敏感数据会进入不合适的消费面。
- Section Goal：建立 Canonical Result、Result Validation、Render / Spill 与多视图责任；明确 `Result != Evidence`。
- Proposal Thesis：Canonical Result、Model View、UI View、Trace View、64-byte inline threshold、4096-byte read cap、spill path与字段都是 **Course Result Contract v1 / C04 PROPOSAL**，不是行业标准或 production redaction guarantee。
- Lab Behavior Thesis：在 **fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation** 条件下，valid calculate/read 成功；wrong result kind 在 execute=`PASS` 后停于 `RESULT_VALIDATION / RESULT_SCHEMA_INVALID`，没有 render / cache；1024-byte result spill 到 Lab-owned temp，Model View 只有 64-byte preview + metadata，UI / Trace 使用 relative spill ref，Trace 不含 preview、全文或 absolute temp path。
- Claim IDs：`06-C04（PROPOSAL）`、`06-C08`
- Evidence IDs：`06-E04`、`06-E08`；TR-01 / 02 / 09 / 10、两份 result views、两份 spill。
- View Responsibility Table：

| View | Responsibility | Full content boundary |
|---|---|---|
| Canonical Result | result validation input | 只在 execution 期内存中存在 |
| Model View | 有界回注候选 | 最多 64-byte preview + byte count / digest / relative ref |
| UI View | Host 显示 metadata | inline 或 relative spill ref，不暴露 absolute temp path |
| Trace View | stage / decision / digest 审计 | 不保存 preview / full content |

- Fixed-scope Caption：表中 result behavior 只在 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation 条件下经 Lab 02 验证；阈值和 view shape 仍是课程 proposal。
- Minimal Artifact Plan：正文只展示 TR-09 与 TR-10 两行 reduced result table，以及 `1024 bytes / spill SHA-256=26AD8132...55A61 / model preview=64 bytes`；不粘贴 14 个完整 result-view JSON object。
- Counter-evidence：ASCII fixture 不覆盖 binary、encoding attack、secret redaction、真实模型消费或生产内容安全。
- Guardrail：`Result != Evidence`：validated result 只满足当前内部 contract；provenance、claim mapping 与独立验证留给 Article 18。
- Figure：`Figure 6｜One Canonical Result, Three Bounded Views`。
- Figure Must Not Imply：UI 一定需要全文、spill 是业务写 Tool、Trace 可替代 Evidence，或 view policy 是生产最佳实践。
- Transition：结果通过后仍要面对 duplicate invocation；去重既不能依赖“模型不会重复”，也不能被夸大为 exactly-once。

## 11. Section 7｜Idempotency 与 Trace：重复调用怎样可判定，又为什么仍不是 exactly-once？

- Problem：网络 retry、上层 replay 或模型重复 call 都可能带来同一个 invocation；如果只记录最后成功，没有办法判断 handler 是否执行第二次，也无法区分同 ID 同参数与同 ID 异参数。
- Section Goal：建立 narrow invocation-id de-dup seam、append-only terminal trace 与 Evidence interpretation 的边界。
- Proposal Thesis：以 `invocation_id + canonical_arguments_sha256` 建 single-process cache、每 invocation 追加一行 JSONL，是 **Course Idempotency / Trace v1 / C04 PROPOSAL**。
- Lab Behavior Thesis：在 **fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation** 条件下，同 invocation ID / 同 canonical args 返回 `REPLAYED` 且 handler count 保持 `1`；同 ID / 异 args 返回 `IDEMPOTENCY_CONFLICT` 且不产生 result；每次 fresh run 各有 14 rows，两份 JSONL SHA-256 都为 `50CEA4EC...21BD67` 且 byte-identical。
- Claim IDs：`06-C04（PROPOSAL）`、`06-C09`
- Evidence IDs：`06-E04`、`06-E09`；TR-11 / TR-12、two JSONL traces、execution log。
- Minimal State Model：

```text
new invocation_id
  -> execute once -> cache args digest + validated result

same id + same digest
  -> REPLAYED -> no second handler execution

same id + different digest
  -> IDEMPOTENCY_CONFLICT -> no second handler execution
```

- Fixed-scope Caption：上述 replay、conflict 与 byte-identical behavior 只覆盖 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation。
- Minimal Trace Plan：正文只放一张四行表（TR-11.1 / 11.2 / 12.1 / 12.2），列 `attempt / args digest relation / terminal / execute / handler count / result digest`；raw JSONL 只给本地证据链接和整体 hash，不堆 24 个 trace 字段。
- Counter-evidence：Calculator / read-only file 没有业务写副作用；cache 不跨 process / restart；未覆盖 concurrency、crash、eviction、transactional side effect、distributed lock 或 global order。
- Guardrail：禁止使用 `exactly-once`、durable idempotency、distributed replay 或副作用安全语态。
- Boundary Sentence：`Trace != Evidence`。append-only record 是 Evidence candidate；必须经过 `Experiment -> Observation -> Evidence Interpretation -> Claim Status` 才支持文章 Claim。
- Figure：不新增图；将四行 de-dup table 与 Figure 3 的 Idempotency / Trace stage 对齐。
- Transition：机制已经逐层落地；下一节用 Lab 02 的最小观测检查整条链是否真的按照 frozen Expected 运行。

## 12. Section 8｜Lab 02：最小证据不是“14 行都绿”，而是每类边界都可判定

- Problem：只展示最终 `PASS` 会抹掉 Expected、Observed、Interpretation 的所有边界；反过来把 28 行 raw JSONL 全贴进正文，会让读者淹没在字段里。
- Section Goal：用一张 claim-level 汇总表和少量 artifact metadata 建立实验责任，不重复 Lab README。
- Core Thesis：在 **fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation** 条件下，两次 fresh run 各完成 12 groups / 14 invocation rows，C05—C09 的 frozen Expected 与 raw Observation 对齐；这只支持 scoped Claim，不支持 production assurance。
- Claim IDs：`06-C05`—`06-C09`
- Evidence IDs：`06-E05`—`06-E09`；Lab Design、execution summary、two JSONL、result views、run-state、spills。
- Minimal Claim-level Evidence Table：

| Claim | Minimum observation to show | Fixed-scope interpretation |
|---|---|---|
| C05 Path | valid read + traversal reject + real junction reject；handler `1 / 0 / 0` | fixed topology 内 two checks 可判定；不覆盖 TOCTOU |
| C06 Policy | Deny / Ask 都在 execute 前 terminal，handler=0 | 实现符合 Course Policy v1；不是行业 merge rule |
| C07 Cancel | `TIMED_OUT/TIMEOUT` vs `CALLER_CANCELLED/CALLER` | test gate 保留 source；不证明强制停止 |
| C08 Result | invalid result不 render/cache；1024-byte spill + 64-byte preview | Course Result Contract v1在 ASCII fixture 成立 |
| C09 Duplicate / Trace | replay/conflict exact；two 14-row trace byte-identical | single-process de-dup + deterministic artifact；不是 exactly-once |

- Fixed-scope Caption：本表所有行为均只覆盖 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation。
- Artifact Summary：
  - two traces：各 `10607 bytes / 14 LF rows / SHA-256 50CEA4EC...21BD67 / byte-identical=true`；
  - result views：各 14 views，hash相同；
  - two spills：各 `1024 bytes / SHA-256 26AD8132...55A61`；
  - two accepted setup：真实 `JUNCTION` 指向 allow-root 外、owned run-root 内；
  - cleanup：两次 temp root 最终都通过 guards 后删除；
  - Provider / network / credential / shell Tool / business writes：全部 `0`。
- Guardrail：两次 byte-identical 只说明固定两次本机 run 的 artifact determinism；不写成跨 runtime、OS、filesystem 或负载的 determinism。
- Figure：`Table 1｜Claim-level Lab Evidence` 即本节唯一视觉；不复制完整 case matrix、distribution table 或 raw JSONL。
- Transition：可信实验不只保存最终观测，还要保存最初失败及其最小修补；下一节单独保留三次 first failure。

## 13. Section 9｜三次 first failure 为什么是工程证据，而不是应该删掉的噪声？

- Problem：如果正文只写 restore / build / two runs 都成功，读者看不到 ProjectReference、PowerShell runtime 与 junction cleanup 的真实环境差异，也无法判断修补是否改变了 frozen Expected。
- Section Goal：完整保留三次 first failure、失败时的安全状态、allowed patch 与 accepted rerun，展示 evidence discipline。
- Core Thesis：失败与修复不被抹平，才能审查“修的是执行入口 / 平台兼容 / cleanup mechanism，还是偷偷改了 hypothesis、case matrix 或 acceptance”。
- Claim IDs：支持 `06-C04` 的 design-conformity 边界与 `06-C05`—`06-C09` 的 evidence credibility；不新增独立 core Claim。
- Evidence IDs：Lab execution summary / raw log；`RESTORE-01`、`SETUP-FIRST-01`、`CLEANUP-FIRST-01`。
- Required Failure Table：

| First failure | Exact observed boundary | Minimal allowed patch | What remained frozen |
|---|---|---|---|
| ProjectReference | restore exit `0`，stdout 报 spec ProjectReference 多退一层并被跳过 | 只修 relative ProjectReference 后重跑 accepted restore | Design、cases、Expected、dependencies不变 |
| Windows PowerShell 5 `GetRelativePath` | setup exit `1`，temp root 创建前失败 | 改用 fully-qualified parent + separator + `OrdinalIgnoreCase` containment guard | 四类 safety classification 与 link requirement不变 |
| junction `Remove-Item` | cleanup exit `1`，抛 `NullReferenceException`；root 与 evidence 原样保留 | 只用同进程 `[IO.Directory]::Delete(path, false)` 删除已验证 reparse point，再执行 guarded recursive cleanup | sentinel / parent / prefix / link-target / remaining-reparse guards不变 |

- Guardrail：不把三次失败写成产品普遍缺陷，也不把 allowed patch 描述成跨 PowerShell / filesystem 通用方案。
- Figure：`Table 2｜First Failure -> Preserved State -> Allowed Patch -> Accepted Rerun`；不展示大段 raw log。
- Figure Must Not Imply：第一次失败是预期 test case，或最终 PASS 取消了 failure history。
- Transition：Lab 支持的是 fixed-scope behavior；要把这些机制用于真实系统，还必须主动列出没有被实验关闭的 production 风险。

## 14. Section 10｜工程边界：fixed-scope evidence、TOCTOU 与 production assurance 不能互相替代

- Problem：Lab 通过后最容易发生语义升级：path check 被叫成 Sandbox，Policy Ask 被叫成 Permission system，timeout 被叫成强杀，de-dup 被叫成 exactly-once，Trace 被叫成 Evidence。
- Section Goal：把文章所有“能证明 / 不能证明”收束为可复用的工程判断。
- Core Boundary Matrix：

| What exists here | What it does not become automatically | Why |
|---|---|---|
| lexical + resolved-target path check | production Sandbox | 不覆盖 check/open TOCTOU、handle confinement、其他 OS / filesystem |
| Course Policy v1 `DENY / ASK / ALLOW` | Permission / Approval system | 没有 identity、credential scope、human resume 或 enforcement model |
| caller / timeout terminal source | guaranteed work termination | cancellation cooperative；underlying handler可能继续 |
| validated / bounded Result View | Evidence | 没有 provenance、claim mapping、independent verification |
| same-ID de-dup seam | exactly-once side effect | single process、无真实业务写、无 crash / distributed transaction |
| append-only deterministic JSONL | complete Trace / Failure Taxonomy | 只是一份课程 schema和固定事件集合 |

- Scope Contract：任何复述 C05—C09 behavior 的句子继续就近保留 fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows `10.0.19045`、`.NET SDK 10.0.301`、single process、no concurrent link mutation。
- Four Must-retain Sentences：
  - `Schema Valid != Policy Allowed`
  - `ToolDefinition != function`
  - `Result != Evidence`
  - `Sandbox != Permission`
- Production Follow-up Questions：只问，不在本文回答：是否需要 handle-based confinement、process isolation、durable idempotency store、secret redaction、identity-aware authorization、crash recovery、production load test？
- Guardrail：问题列表不是后续设计结论，不把它们扩写成 Article 19、21 或 BuildPilot 内容。
- Figure：`Table 3｜Lab Boundary -> Missing Production Proof`。
- Transition：边界明确后，读者需要一个不依赖 Lab 内部类名的项目审查入口。

## 15. Section 11｜怎样审查一条 Tool Runtime？

- Section Goal：把全文压缩成可判定的工程验证清单，作为招聘 / 项目评审可复用输出。
- Review Checklist：
  1. 当前能力是 client-executed，还是 Provider-managed built-in / server tool？execution owner是否明确？
  2. model-visible ToolDefinition 与 Host Registry / metadata 是否分开？unknown name 是否 fail closed？
  3. arguments 是否在 execute 前经过 canonicalization、Schema / DTO / Domain 与 resource checks？
  4. `Schema Valid` 后是否仍有独立 Policy decision？Deny / Ask 冲突如何记录？
  5. invocation identity 与 canonical args digest 是否可审查？duplicate replay / conflict 是否会二次执行？
  6. caller cancellation 与 timeout 是否保留来源？系统能否区分“停止等待”与“工作已停止”？
  7. handler result 是否在 render / cache 前验证？Model / UI / Trace 是否各有有界 view？
  8. early terminal 后哪些 stage 是 `NOT_RUN`？每条 terminal path 是否追加 Trace？
  9. Trace 是否仍需 Evidence interpretation？Scope、environment、failure history 与 limitations 是否保存？
  10. 当前保证来自 official API contract、course proposal、fixed Lab observation，还是 production evidence？
- Claim Coverage：综合 `06-C01`—`06-C09`。
- Guardrail：清单不要求所有产品采用同一类名或 stage 数量；判定的是责任是否有 owner、失败是否可拒绝、证据是否不过度外推。
- Figure：`Figure 7｜Tool Runtime Review Checklist`，正文使用编号清单，不创建 assets。
- Transition：以一句最短结论和可判定 Learning Check 收口。

## 16. Closing Plan

- Bridge Back：Article 05 只建立 Tool Call intent 与 Host decision seam；本篇补上 local Runtime gates，仍不进入 MCP 或 Agent Loop。
- Final Boundary：Lab 02 证明的是 fixed fixture 对课程 proposal 的符合性；production safety、Permission、Sandbox、distributed idempotency 与 complete Trace / Failure Taxonomy 仍需要独立证据。
- Shortest Conclusion：`Tool Runtime 的价值，不是替模型多包一层函数，而是让每次执行都知道为何能继续、为何必须停止、结果能给谁看，以及证据到底证明到哪里。`
- Future Navigation：Article 07、08、18、19、21 只用 prose 点名职责，不创建 `relref`。

## 17. Figures / Tables / Code Block Plan

| ID | Planned Material | Format | Teaching Duty | Must Not Imply |
|---|---|---|---|---|
| Figure 1 | 普通函数包装 vs Tool Runtime | ASCII flow | 立住危险参数、duplicate、cancel/result/audit 丢层 | Runtime 已实现 production safety |
| Figure 2 | Definition / Registry 双视图 | ASCII boxes | 建立 model-visible 与 Host-only responsibility | 所有 Tool 本地执行；Registry是官方结构 |
| Figure 3 | Call -> Trace first-failure Pipeline | ASCII flow | 全文主抽象，显示 `PASS/FAIL/NOT_RUN` | C04 是行业标准 |
| Figure 4 | Lexical vs resolved containment | ASCII path flow | 解释 traversal / junction 与 TOCTOU gap | path API = Sandbox |
| Figure 5 | caller vs timeout source | ASCII fork | 保留 cancellation origin 与 cooperative boundary | timeout 强制杀死工作 |
| Figure 6 | Canonical Result -> Model/UI/Trace views | ASCII fan-out | 解释 bounded views / spill | validated Result = Evidence |
| Table 1 | C05—C09 Lab evidence | 5-row Markdown table | 最小实验闭环 | 14 rows 全绿 = production可靠 |
| Table 2 | 三次 first failure | 3-row Markdown table | 保留失败、修补与 frozen boundary | 修复抹平失败历史 |
| Table 3 | Lab boundary -> missing production proof | Markdown matrix | 收束 engineering judgment | 本文已设计后续系统 |
| Code A | Path decision pseudocode | 7-line text block | 展示 two containment checks | 可复制即获 production security |
| Code B | Idempotency state model | 9-line text block | 区分 execute / replay / conflict | exactly-once |

Asset Policy：本 Gate 与后续 Draft 默认不创建 `assets/`；优先使用 Markdown 表、ASCII 图与短伪代码。正文不复制完整 Lab implementation、28 条 raw JSONL、14 个 result-view object 或 execution raw log。

## 18. Lab Behavior Scope Wording Contract

### Mandatory nearby scope

Draft 只要陈述 C05—C09 的 observed behavior，句内或紧邻句必须出现完整 scope：

`fixed Calculator + ReadOnlyFileTool / ASCII fixture、Windows 10.0.19045、.NET SDK 10.0.301、single process、no concurrent link mutation`

不得只写“本 Lab 中”“当前环境”“在 Windows 上”或把 scope 放到文章开头后全篇省略。

### Required evidence labels

- C01：`OFFICIAL DOCUMENT CONTRACT / CLIENT-EXECUTED SCOPE`，并保留 built-in / server-executed counterexample。
- C02—C03：`OFFICIAL .NET CONTRACT / API OR GUIDANCE SURFACE`，不升级为 fixture behavior。
- C04：`COURSE DESIGN PROPOSAL / NOT INDUSTRY STANDARD`。
- C05—C09：`CONFIRMED WITHIN FIXED LAB SCOPE`，完整 scope 就近出现。
- Lab hashes / rows：`DURABLE LOCAL ARTIFACT / WORKING TREE / CHECKPOINT PENDING MASTER`。

### Forbidden shorthand

- “ToolDefinition 就是函数”
- “所有 Tool 都由 Host 执行”
- “Schema 通过就允许执行”
- “GetFullPath / ResolveLinkTarget 已保证路径安全”
- “Deny > Ask > Allow 是行业标准”
- “timeout 会杀死 Tool”
- “spill 已保证敏感信息安全”
- “Trace 就是 Evidence”
- “两次 hash 相同证明跨环境 determinism”
- “同 ID replay 证明 exactly-once”
- “Sandbox 已实现 Permission”

## 19. Learning Check Plan

| # | 判定题 / 任务 | Claim Coverage | 可判定答案必须包含 |
|---:|---|---|---|
| 1 | 一个 client-executed ToolDefinition 已包含 name / description / schema，为什么仍需要 Host Registry？built-in / server tool 对结论有什么限制？ | `06-C01` | definition / implementation 两责任面；Host-only metadata；execution owner counterexample；不得说所有 Tool 本地执行 |
| 2 | `relative_path` 通过 Schema 且 `GetFullPath` 落在 allow-root 下，能否直接读取？ | `06-C02`、`06-C05` | lexical与resolved-target两次 containment；API只是构件；fixed Lab behavior完整scope；TOCTOU未覆盖 |
| 3 | global=`ALLOW`、tool=`ASK`、resource=`DENY` 时，本课程 Policy v1 怎样终止？这个答案能否写成行业规则？ | `06-C04`、`06-C06` | `DENY / POLICY_DENIED / execute NOT_RUN`；完整Lab scope；C04仍为proposal |
| 4 | 一个 invocation 因 50ms budget 返回 timeout，能否断言 handler 已停止？它与 caller 预取消有哪些可观察差别？ | `06-C03`、`06-C07` | cooperative；`TIMEOUT/handler=1` vs `CALLER/handler=0`；无result；完整Lab scope |
| 5 | handler 已执行一次但返回 wrong result kind，Trace 应怎样记录，哪些 stage 不能继续？ | `06-C08` | execute PASS；result validation FAIL；render/cache NOT_RUN；完整Lab scope |
| 6 | UI 要展示文件信息、模型只需摘要、Trace只需审计字段，为什么不能共享一个无界 string？ | `06-C04`、`06-C08` | Canonical/Model/UI/Trace职责；64-byte preview与spill是课程proposal + fixed behavior；Result != Evidence |
| 7 | 同 invocation ID 同参数与异参数分别怎样处理？为什么两次行为仍不证明 exactly-once？ | `06-C09` | replay/conflict、handler count、single-process、无业务写/crash/distributed guarantee；完整Lab scope |
| 8 | 两次 14-row JSONL byte-identical、spill hash相同且 junction cleanup成功，能证明什么，不能证明什么？ | `06-C05`—`06-C09` | fixed artifact determinism与scoped behavior；不证明跨环境、TOCTOU、production security、permission、exactly-once |
| 9 | 三次 first failure 为什么必须保留？怎样判断 patch 没有为了 PASS 改 Expected？ | Evidence discipline | 原始失败、失败时安全状态、最小patch、accepted rerun、hypothesis/cases/threshold/codes/acceptance未变 |
| 10 | 对一个 Tool Runtime 方案，怎样逐项验证 `Schema Valid != Policy Allowed`、`Result != Evidence`、`Sandbox != Permission`？ | 全部 | 每个不等式各给责任 owner、可观察终止与缺失证据，不靠口号作答 |

Reference-thought Style：每题参考思路按“当前对象 / 当前 Gate / 可观察 terminal / 能证明 / 不能证明”五项给出；不背 API 字段，不把课程 proposal写成行业标准。

## 20. Claim-to-Section Coverage Matrix

| Claim ID | Status | Main Placement | Evidence IDs | Semantic Guard |
|---|---|---|---|---|
| `06-C01` | `CONFIRMED` | Opening、Section 1、Checklist | `06-E01` / `S-01` / Article 05 | 只对 client-executed tools建立 Host seam；built-in / server tools反驳“全部本地执行” |
| `06-C02` | `CONFIRMED` | Section 3、Boundary、Learning 2 | `06-E02` / `S-02`—`S-04` | path APIs只是 lexical / link-target构件，不等于安全算法、Sandbox或TOCTOU closure |
| `06-C03` | `CONFIRMED` | Section 5、Boundary、Learning 4 | `06-E03` / `S-05`—`S-07` | cancellation cooperative；timeout/caller是completion conditions，不证明handler停止 |
| `06-C04` | `PROPOSAL` | Section 2、4、6、7、Figures、Boundary | `06-E04` / Lab Design | Pipeline、Policy、result views、spill、idempotency、JSONL始终是课程proposal，不写行业标准 |
| `06-C05` | `CONFIRMED` | Section 3、8、10、Learning 2 / 8 | `06-E05` / TR-02—04 / run-state / trace | 每条behavior就近保留Windows 10.0.19045、SDK 10.0.301、fixed fixture、single-process、no concurrent link mutation |
| `06-C06` | `CONFIRMED` | Section 4、8、10、Learning 3 | `06-E06` / TR-05—06 / trace | 只确认Course Policy v1在完整fixed scope中的行为；Ask不是完整approval flow |
| `06-C07` | `CONFIRMED` | Section 5、8、10、Learning 4 | `06-E07` / TR-07—08 / trace | 只确认完整fixed scope + cooperative test gate；不证明强制终止/真实慢I/O |
| `06-C08` | `CONFIRMED` | Section 6、8、10、Learning 5—6 | `06-E08` / TR-01,02,09,10 / result views / spill | 只确认完整fixed scope + ASCII Result Contract v1；不证明secret/binary/production safety |
| `06-C09` | `CONFIRMED` | Section 7、8、10、Learning 7—8 | `06-E09` / TR-11—12 / two JSONL / execution log | 只确认完整fixed scope中的de-dup与artifact；不使用exactly-once / durable / distributed语态 |

Coverage Result：`9 / 9 Claims semantically mapped`；状态保持 `8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。

## 21. Job Competency Mapping

| Competency | Observable Article Outcome | Assessment Surface | Pass Criterion |
|---|---|---|---|
| Contract boundary design | 能分开 model-visible definition、Host Registry、handler 与 Host-only metadata | Section 1 + Learning 1 | 明确client-executed scope和server/built-in counterexample，不发明统一Registry |
| Pipeline / state modeling | 能为每个 stage定义 `PASS / FAIL / NOT_RUN` 与 first terminal | Section 2 + Checklist 8 | 给定失败case能正确指出terminal与未运行后续层 |
| Filesystem security judgment | 能区分 lexical containment、resolved-target containment、TOCTOU与Sandbox | Section 3 + Learning 2 | 不把API presence当authorization或production security |
| Policy engineering | 能把schema/domain/resource/policy分层并审查冲突 | Section 4 + Learning 3 | 正确判断Course Policy v1，同时标明proposal scope |
| Async cancellation semantics | 能区分caller、timeout、停止等待与工作停止 | Section 5 + Learning 4 | 明确cooperative boundary和固定test-gate证据 |
| Result contract design | 能设计canonical result validation与bounded consumer views | Section 6 + Learning 5—6 | invalid result不render/cache；区分Model/UI/Trace与Evidence |
| Idempotency judgment | 能区分replay、conflict、single-process de-dup与exactly-once | Section 7 + Learning 7 | 不把无业务写fixture外推到distributed side effect |
| Experiment / evidence literacy | 能从Expected、Observed、first failures、hash与limitations形成scoped Claim | Section 8—10 + Learning 8—9 | 完整保留environment / scope / failure history，不用最终PASS抹平过程 |
| Architecture review | 能用10项清单定位责任缺口和证据等级 | Section 11 + Learning 10 | 分清official contract、course proposal、Lab observation、production evidence |

## 22. Explicit Non-scope and Adjacent Stop Lines

| Adjacent / Future Topic | Article 06 May Introduce | Article 06 Must Stop Before |
|---|---|---|
| Article 03｜Structured Output | arguments / result继续经过Parse / Schema / DTO / Domain | 重讲JSON Schema / Lab 01、把Schema Valid写成Policy Allowed |
| Article 05｜Function Calling | client-tool call intent、correlation与Host decision seam | 重讲Provider payload、tool choice、multiple-call protocol |
| Article 07｜MCP | 外部Tool将需要protocol / transport边界 | MCP client/server、discovery、transport、interop、remote cancellation实现 |
| Article 08｜Agent Loop | Tool execution可成为未来Loop的一步 | Turn、Step、Decide、Act、Observe、state、stop机制 |
| Article 18｜Evidence | Result / Trace需要额外证据合同才能支持Claim | provenance schema、claim-to-source mapping、verification workflow |
| Article 19｜Permission / Approval / Sandbox | Policy可返回Deny / Ask；`Sandbox != Permission` | identity、permission model、approval UX、credential scope、sandbox enforcement、HITL resume |
| Article 21｜Trace / Replay / Failure Taxonomy | 本篇只使用局部terminal code与Course Trace Schema v1 | 完整failure taxonomy、跨step trace、replay architecture、production observability |
| Article 35｜DSH Tool Pipeline | 本篇可作为未来源码阅读的机制前置 | DSH repository、symbol、call path、source / runtime claim |
| BuildPilot | 不需要提及实现；最多一句“后续设计可复用责任问题” | BuildPilot architecture、Tool set、interface、production Runtime或任何已实现语态 |

Hard Non-goals：

- 不开放 shell Tool。
- 不写业务文件；1024-byte spill只作为Lab-owned Host internal artifact。
- 不读取credentials，不访问network，不调用Provider。
- 不讲MCP（07）、Agent Loop（08）、Permission / Approval / Sandbox实现（19）、完整Failure Taxonomy（21）。
- 不进入DeepSeek Harness source verification或BuildPilot design / implementation。
- 不把课程Course Factory pipeline、Course Policy v1或C04 Tool Runtime pipeline写成行业标准。

Future Link Rule：Article 07 / 08 / 18 / 19 / 21 / 35 与 BuildPilot 均只用 prose，不创建未来 `relref`。

## 23. Source / Link Plan

### External source whitelist

Draft 外链只允许使用 Evidence Manifest 的下列 7 个 current official URL；不新增博客、教程、OpenTelemetry、framework文档或未登记链接：

| ID | Planned Link | Draft Responsibility |
|---|---|---|
| `S-01` | [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling) | client-executed definition / call / application-side execution seam；built-in / server execution owner边界 |
| `S-02` | [Microsoft Path.GetFullPath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath?view=net-10.0) | deterministic fully-qualified path构件 |
| `S-03` | [Microsoft Path.GetRelativePath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getrelativepath?view=net-10.0) | platform-aware relative path构件 |
| `S-04` | [Microsoft Directory.ResolveLinkTarget](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget?view=net-10.0) | final link / junction target resolution surface |
| `S-05` | [Microsoft Cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) | cooperative cancellation、requester / listener distinction |
| `S-06` | [Microsoft CancellationTokenSource.CancelAfter](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelafter?view=net-10.0) | scheduled cancellation request surface |
| `S-07` | [Microsoft Task<TResult>.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1.waitasync?view=net-10.0) | task completion / timeout / caller-cancellation conditions |

External Link Rule：7 个 hosted docs 均于 `2026-08-20（Asia/Shanghai）` 由 Researcher核对；发布前按 Evidence要求重查current page与`.NET 10` view。Outline / Draft不把核对日期升级为永久版本保证。

### Local source and navigation plan

| Purpose | Existing Local Target | Link / Use Plan |
|---|---|---|
| Structured Output dependency | [Published Article 03](../../../../content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md) | 继承Parse / Schema / DTO / Domain与first-failure boundary，不重讲Lab 01 |
| Adjacent Provider boundary | [Published Article 04](../../../../content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md) | 只用于连续阅读与timeout/retry owner边界背景，不扩写Provider内容 |
| Backward navigation | [Published Article 05](../../../../content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md) | Published Content未来顶部“上一篇”target；正文承接Tool Call intent与Host decision seam |
| Course terms | [Glossary](../../glossary.md) | 只继承Tool、Host、Evidence、Trace、Agent Runtime工作定义 |
| Canonical | [Series Plan](../../../agent-engineering-series-plan.md) | Article ID / title / Part / weight / dependency / future stop lines |
| Frozen Article 06 detail | [v3.1 Frozen Plan Section](../../../agent-engineering-course-plan-v3.1-review.md) | responsibility、questions、mental model、Lab、Learning Check、L / Engineering maturity |
| Current Article state | [Article 06 README](README.md) | Gate、scope、next action与stop line |
| Current Research | [Article 06 Research](research.md) | stable model、counter-evidence、Source Manifest、Evidence Merge |
| Current Evidence | [Article 06 Evidence Register](evidence.md) | 9 Claims / 9 Cards、allowed wording、limitations |
| Required Lab | [Lab 02 Design / Observation / Interpretation](../../labs/lab-02-tool-runtime/README.md) | frozen Expected、Observed、Interpretation与scope |
| Lab execution summary | [Execution Summary](../../labs/lab-02-tool-runtime/artifacts/logs/execution.md) | accepted commands、three first failures、hash / cleanup evidence |
| First raw trace | [First JSONL](../../labs/lab-02-tool-runtime/artifacts/observation-first.jsonl) | 14-row direct evidence；正文不复制全部字段 |
| Second raw trace | [Second JSONL](../../labs/lab-02-tool-runtime/artifacts/observation.jsonl) | 14-row direct evidence；与first byte-identical |
| First result views | [First Result Views](../../labs/lab-02-tool-runtime/artifacts/result-views-first.json) | invalid result与large result多视图证据 |
| Second result views | [Second Result Views](../../labs/lab-02-tool-runtime/artifacts/result-views-second.json) | 第二次fresh run的相同视图证据 |

Publication Plan：

- Future Published Target：`content/ai-empowerment/agent-engineering-06-tool-runtime.md`（本 Gate 禁止创建；只有后续 Publisher 可创建）。
- Future backward `relref` target：`ai-empowerment/agent-engineering-05-function-calling-tool-use.md`，shortcode 参数必须使用 ASCII 双引号。
- Article 03 / 04如正文需要复习链接，只使用已存在target；不修改已发布文章的forward navigation。
- Article 07及以后尚未发布，只用无链接prose，不创建可能导致`REF_NOT_FOUND`的`relref`。

## 24. Length Budget

| Section | Budget | Compression Rule |
|---|---:|---|
| Opening | 450—600 字 | 四类丢层 + Figure 1；不提前贴API/Lab |
| Section 1 Definition / Registry | 550—700 字 | 一张双视图；server/built-in只作必要counterexample |
| Section 2 Runtime Pipeline | 650—850 字 | 全文唯一主链；stage contract不重复三次 |
| Section 3 Path | 800—1,050 字 | 两次containment + 三行case；不复制walker实现 |
| Section 4 Policy | 550—700 字 | 两个conflict case；不展开Permission / HITL |
| Section 5 Timeout / Cancellation | 650—850 字 | document contract + 两个test gate case；不写生产调度 |
| Section 6 Result Views | 750—950 字 | 四视图 + invalid / large两行；不贴14个JSON object |
| Section 7 Idempotency / Trace | 700—900 字 | 四行duplicate table + hash；不展开distributed exactly-once |
| Section 8 Lab Evidence | 650—850 字 | 5-claim汇总 + artifact metadata；不重抄case matrix |
| Section 9 First Failures | 500—650 字 | 保留3行failure table，不贴raw log |
| Section 10 Boundary | 550—750 字 | 六行proof-gap matrix；未来问题只列不答 |
| Section 11 Checklist + Closing + Learning | 650—850 字 | 清单与参考思路压缩重复定义 |

Budget Result：Draft target约 `6,500—8,500 中文字`。超预算时优先压缩重复scope解释、API名称、raw artifact字段与图注；不得删9/9 Claim coverage、C04 proposal标签、C05—C09 nearby full scope、三次first failure、Learning Check或production boundary。

## 25. New Core Facts Audit

| Candidate Addition | Classification | Evidence / Disposition |
|---|---|---|
| “普通函数包装丢失危险参数、duplicate、cancel/result/audit边界” | Existing problem-space synthesis | Article Card / frozen plan / C01 / C04—C09；不构成新behavior claim |
| Definition / Registry双视图 | Existing confirmed seam + course shape | C01；Provider只证明separation，Registry字段保持课程设计 |
| Full Runtime Pipeline | Existing course proposal | C04；始终标`PROPOSAL / NOT INDUSTRY STANDARD` |
| Path two-check mechanism | Existing document + course design | C02 / C05；API与fixture behavior分开 |
| Policy conflict table | Existing proposal + confirmed fixture behavior | C04 / C06；不扩写Article 19 |
| cancellation source split | Existing document + confirmed fixture behavior | C03 / C07；保持cooperative boundary |
| Result four views / spill | Existing course proposal + confirmed fixture behavior | C04 / C08；不声称production redaction |
| idempotency / append-only trace | Existing course proposal + confirmed fixture behavior | C04 / C09；不使用exactly-once |
| Lab claim summary / hashes / real junction / cleanup | Existing final Evidence | C05—C09 / L-01—L-08；不新增raw interpretation |
| Three first failures | Existing preserved execution evidence | execution summary / raw log；不改失败或patch叙事 |
| Figures、section order、length、Learning Check | Editorial planning metadata | 不构成技术Claim |

New Core Facts Result：`0`。

### RETURN_TO_RESEARCH decision

- Decision：`NONE`
- Reason：问题空间、抽象模型、concrete mechanisms、Lab observations、first failures、engineering boundaries、Learning Check与Job Competency均可由`06-C01`—`06-C09`和final Evidence支撑；不存在新增Provider/runtime/production核心事实。
- Mandatory Return Triggers：Draft若需要以下任一项才能成立，必须停止并返回`RETURN_TO_RESEARCH`：
  - 其他OS / filesystem / PowerShell / .NET版本行为；
  - concurrent link mutation、TOCTOU closure或handle-based confinement效果；
  - 真实慢I/O、忽略token handler或process kill behavior；
  - secret / binary / encoding安全、真实模型消费或production large-result策略；
  - cross-process / crash / distributed idempotency、真实业务副作用或exactly-once；
  - Provider roundtrip、MCP、Agent Loop、Permission / Approval、完整Trace / Failure Taxonomy；
  - DSH source/runtime或BuildPilot design / implementation；
  - Evidence Manifest 7个URL以外的核心外部事实。

## 26. Outline Gate Checklist and Recommendation

- [x] H1 与 canonical Article 06 标题精确一致。
- [x] Article Type明确为原理篇（工程机制型）/ Lab Article；第一屏从普通函数包装的工程风险开场，不从API或Lab case开场。
- [x] Teaching Spine遵循 Problem Space -> Abstract Model -> Concrete Mechanism -> Engineering Judgment -> Verification Boundary。
- [x] model-visible ToolDefinition与Host Registry明确分开；C01保留client-executed scope与built-in / server counterexample。
- [x] Runtime主链完整覆盖Call -> Registry -> Canonicalize -> Validate -> Policy -> Idempotency -> Execute -> Validate Result -> Render / Spill -> Trace。
- [x] `Schema Valid != Policy Allowed`、`ToolDefinition != function`、`Result != Evidence`、`Sandbox != Permission`均显式保留。
- [x] C02明确API只是构件，不等于安全算法、Sandbox或TOCTOU closure。
- [x] C03明确cancellation cooperative；不把timeout写成强制终止。
- [x] C04在所有出现位置保持`PROPOSAL / COURSE DESIGN / NOT INDUSTRY STANDARD`。
- [x] C05—C09所有规划中的behavior句均就近带Windows `10.0.19045`、`.NET SDK 10.0.301`、fixed fixture、single process、no concurrent link mutation scope。
- [x] `9 / 9 Claims semantically mapped`；状态保持`8 CONFIRMED / 0 PARTIAL / 0 BLOCKED / 1 PROPOSAL`。
- [x] 两run各14 rows、trace SHA=`50CEA4EC...21BD67` byte-identical、spill SHA=`26AD8132...55A61`、real junction与guarded cleanup均规划进最小证据表。
- [x] ProjectReference、PowerShell 5 `GetRelativePath`、junction `Remove-Item`三次first failure完整保留，没有被accepted rerun抹平。
- [x] 图、表、代码块职责明确；正文不计划堆28行raw trace、14个result-view object或完整implementation。
- [x] fixed-scope / TOCTOU / production boundary与工程验证清单完整。
- [x] shell、业务写文件、credentials、Provider/network均未开放；MCP、Agent Loop、Permission、完整Failure Taxonomy、DSH、BuildPilot stop lines明确。
- [x] Learning Check与Job Competency均有可判定pass criterion。
- [x] External links只使用Evidence 7个官方URL；local targets均计划为现存文件；future articles只用prose、不创建`relref`。
- [x] L级Draft target为约`6,500—8,500 中文字`。
- [x] `new core facts = 0`；`RETURN_TO_RESEARCH = NONE`。
- [x] 本Gate只创建当前Article的`outline.md`；不创建Draft、assets、Published Content或Article 07，不修改global state / canonical / status / Lab。

Recommendation：`PASS_RECOMMENDED`。由 Master 独立核对 Outline Gate；通过后的唯一下一动作是`AUTHOR_DRAFT`，由 Author仅依据本Outline与批准Evidence创建当前Article的`draft.md`。不得把本候选推荐写成Author自批准，也不得跳到Review、Publish或Article 07。
