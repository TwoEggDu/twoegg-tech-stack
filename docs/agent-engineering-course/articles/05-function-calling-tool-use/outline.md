# Function Calling 与 Tool Use：模型如何表达行动意图

- Lifecycle Input：`EVIDENCE_READY`
- Evidence Gate：`PASS`（`6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`）
- Outline Gate：`PASS_RECOMMENDED`（候选；由 Master 核对后决定）
- Article Type：`原理篇（机制型） / Normal Article`
- Concept Maturity：`Mechanism`
- Course Weight：`M（Standard Core Lesson）`
- Target Length：`约 5,000—6,500 中文字`
- Target Reading Time：`14—18 分钟`
- Required Lab：`NONE`
- Provider Calls / Tool Execution：`NONE / NONE`
- Provider Runtime Evidence：`UNVERIFIED`
- Tool Schema Fixture：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`
- Message Traces：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`

## 1. Article Thesis

> 如果这篇只记一句话：`Function Calling 把模型输出推进为结构化行动意图；对 client-executed tools，Host 仍决定怎样处理、是否执行，并负责把关联 Tool Result 带入下一次模型请求，而一次往返仍不足以证明 Agent Loop。`

### Type decision

- 采用原理篇：核心任务是建立稳定责任模型，再用 OpenAI Responses 与 Anthropic Messages 的 current document contract 落地。
- 不采用映射篇：两家 Provider trace 用于证明共同机制与差异边界，不做产品优劣或 API 对照大全。
- 不采用案例篇：Calculator 与 `deleteFile` 都未执行，不存在可复盘的 runtime incident。
- 不采用配置 / API 教程：正文不从字段表开场，也不提供可复制即运行的 SDK 示例。

## 2. Reader Change

读者从“模型返回了 Tool Call，所以工具已经开始工作”，转变为能够沿一条可审查链路区分：

1. 业务 Structured Result 与 Tool Call intent；
2. model-visible Tool definition / choice 与本地 registry / authorization；
3. Provider-specific call item / content block 与课程稳定抽象；
4. argument fragment、completed candidate、validated arguments、authorized action 与 executed result；
5. multiple calls 与 Host execution concurrency；
6. correlated Tool Result 与可审计 Evidence；
7. 一次 Tool Use 与课程定义下 Agent Loop 所需的后续机制。

完成本篇后，读者应能独立回答：

- 为什么 `Tool Call != Executed`，以及该结论为什么只对 client-executed tool flow 使用 Host-execution 表述；
- Tool name / description / schema / choice 能证明什么，为什么不能证明 selection quality 或 schema adherence improvement；
- OpenAI `call_id` 与 Anthropic `tool_use.id -> tool_use_id` 分别怎样承担 call/result correlation；
- 为什么 fragment 不能直接 Parse / Execute，multiple calls 也不要求 Host 必须并行；
- unknown、invalid、unauthorized 为什么应在 Host seam fail closed；
- 为什么 generic Tool Result 不自动成为 Evidence，一次 Tool Use 也不足以证明 Agent Loop。

## 3. Teaching Spine

| Teaching Phase | Reader Movement | Main Placement | Claim / Evidence |
|---|---|---|---|
| Problem Space | 从“结构化输出已经可消费”推进到“模型怎样表达行动意图”，同时拆开 intent 与 execution | Opening | `05-C01` / `S-01`、`S-03`、`S-07`、`R-01`、`R-02` |
| Abstract Model | 建立 client-tool 的 definition → call intent → Host decision → correlated result → next request 最小责任链 | Section 1 | `05-C01`、`05-C03` / `S-01`、`S-03`、`S-07` |
| Concrete Mechanism | 先看 model-visible schema / choice，再分别落 OpenAI Responses 与 Anthropic Messages trace，最后区分 fragment / completion / multiple calls | Section 2—4 | `05-C02`—`05-C05` / `S-01`—`S-06`、`R-01`、`R-02` |
| Engineering Judgment | 让 registry、validation、authorization、reject / error / execute decision 留在 Host；保留 Tool Result 的 Evidence 边界 | Section 5—6 | `05-C01`、`05-C03`、`05-C06`、`05-C07` / `S-01`、`S-03`、`S-05`、`S-07`、`R-01`、`R-03` |
| Verification Boundary | 用证据标签、Learning Check 和 stop lines 说明本文证明的是 document contract，不是 Provider runtime、Tool Runtime 或 Agent Loop closure | Section 7、Closing | `05-C07`、`05-C08` / `S-01`、`S-03`、`S-07`、`R-03`—`R-05` |

### M-level scope discipline

- 只围绕“一次 client-tool documented roundtrip 的责任边界”建立标准知识单元。
- Provider 字段只用于展示具体机制；不发明统一 wire schema，不做完整 API reference。
- Calculator A/B 只展示 model-visible contract surface；不写成 A/B 实验，不产生任何效果图。
- `deleteFile` 只作为 synthetic unknown / unauthorized negative；不声称模型生成过该 call，也不声称文件系统发生过副作用。
- 多调用只讲 output shape、独立 correlation 与 Host decision seam；不展开调度、依赖图、幂等、timeout 或 retry。
- Tool Runtime、MCP、Agent Loop、Evidence Contract 与 Permission / Approval 分别停在 Article 06、07、08、18、19 的入口。

## 4. Opening｜Structured Result 可消费之后，为什么仍没有行动发生？

- Problem：Article 03 已让候选数据经过 Parse / Schema / DTO / Domain，Article 04 已保留 Provider stream / terminal 边界；读者仍可能把“模型返回一个带函数名和参数的结构”误当成外部动作已经发生。
- Section Goal：从真实工程误判立住问题空间，不从 Provider API 字段或 SDK 代码开场。
- Core Thesis：业务 Structured Result 描述“模型返回什么数据”；client-tool call 描述“模型请求 Host 考虑什么行动”。两者都只是 Application 收到的结构化内容，后者仍不能证明 route、authorization、execution 或 side effect。
- Claim IDs：`05-C01`
- Evidence IDs：`S-01`、`S-03`、`S-07`、`R-01`、`R-02`
- Counter-evidence：OpenAI built-in tools 与 Anthropic server-executed tools 表明某些能力可由 Provider 基础设施执行，反驳“所有 Tool 都必由应用进程执行”。
- Guardrail：只有在明确写出 `client-executed tools` 后，才使用“Host / application 决定是否执行”；不把 Provider-managed tools 强塞进同一执行 owner。
- Example：并列一个“业务诊断 Structured Result”与一个“请求 `calculate_binary` 的行动意图”概念卡；只比较语义职责，不伪造某家 Provider payload。
- Example Label：`CONCEPTUAL CONTRAST / NO PROVIDER CALL / NO TOOL EXECUTION`
- Figure：`Figure 1｜Structured Result vs Tool Call Intent`，两条线都到达 Application，Tool Call 线在副作用前保留明显的 Host decision gap。
- Figure Must Not Imply：Tool Call 已授权、已执行、已成功或已产生真实 Result。
- Transition：既然 Tool Call 是 intent，下一节先建立一个不依赖具体字段名的最小责任链。

## 5. Section 1｜抽象模型：一次 client-tool 往返里，谁拥有什么决定权？

- Problem：如果只说“模型调用工具”，definition、model output、Host decision、execution 与下一次 request 会被压成一个动作。
- Section Goal：建立全文唯一的稳定抽象，并在图中保留 Provider-managed tool counterexample scope。
- Core Thesis：对 client-executed tools，稳定链路是 model-visible definition → tool-call request → Host decision / optional execution → correlated result → next model request；每个箭头之间都存在独立责任，不因 SDK helper 自动化而消失。
- Claim IDs：`05-C01`、`05-C03`
- Evidence IDs：`S-01`、`S-03`、`S-07`
- Abstract Model：

```text
Provider-scoped tool definitions
        ↓
model response: tool-call intent
        ↓
Host: correlate + decide route / reject / optional execute
        ↓
correlated tool-result content
        ↓
next model request
        ↓
text or additional tool calls
```

- Counter-evidence：SDK Tool Runner 可以自动承载循环和消息拼装；server / built-in tools 可由 Provider 基础设施执行。
- Guardrail：不写“每个应用都必须手写 loop”，也不写“所有 Tool 都由 Host 进程执行”；本图是 client-tool 的责任抽象，不是行业统一 wire protocol。
- Example：把“自动 Tool Runner”画成覆盖多个步骤的 owner box，保留其中的 call/result correlation、validation 与 authorization seam，不猜测某 SDK 的未执行行为。
- Example Label：`RESPONSIBILITY SKETCH / NOT SDK RUNTIME OBSERVATION`
- Figure：`Figure 2｜Client-tool Responsibility Chain`；旁路标出 `Provider-managed built-in / server tool` 是 counterexample scope，不进入 Host-execution 的普遍结论。
- Figure Must Not Imply：Provider runtime 已在本地发生、Runner 已执行、或 Result 真实可信。
- Transition：责任链建立后，先看模型在第一步究竟看到了什么，以及 API 能怎样约束选择。

## 6. Section 2｜Tool Schema 与 Tool Choice：给模型看的能力合同，不是执行许可

- Problem：团队容易把 name / description / enum / strict / forced choice 同时理解成“模型会选对、参数会正确、业务允许执行”。
- Section Goal：把 model-visible contract、Provider choice control 与 Host authorization 分开。
- Core Thesis：Tool name、description、input schema 与 tool choice 改变模型可见的能力合同和可表达参数空间；字段、strict 与 choice vocabulary 受 Provider / API / model / version 限定，且不能在无实验时推出质量提升。
- Claim IDs：`05-C02`
- Evidence IDs：`S-01`、`S-02`
- Provider Mapping Plan：

| Contract Surface | OpenAI Responses document contract | Anthropic Messages document contract | Course Abstraction |
|---|---|---|---|
| Definition | function tool 的 `type` / `name` / `description` / `parameters`，scoped `strict` | client tool 的 `name` / `description` / `input_schema`，scoped strict tool use | model-visible capability + input shape |
| Choice | current guide 中的 `auto` / `required` / forced / allowed / `none` surface | current docs 中的 `auto` / `any` / specific `tool` / `none` surface | selection constraint，不等于 authorization |
| Arguments | JSON-encoded `arguments` | object `input` | Provider-specific representation，不统一字段 |

- Counter-evidence：两家 scoped strict mode 可以提供比 best-effort 更强的 schema guarantee，反驳“Tool arguments 永远都不符合 schema”；两家字段、schema subset 与兼容面又不相同。
- Guardrail：`Schema Valid != Domain Valid != Authorized != Executed`；不写“description 更详细会提高准确率”“enum 会降低错误率”或“forced choice 会提高最终任务质量”。
- Example：保留 Research 中的 Calculator A/B 对照：
  - `Calculator-A`：`calculate(expression: string)`，model-visible input surface 较开放；
  - `Calculator-B`：`calculate_binary(operation: enum(add, subtract), left: number, right: number)`，model-visible 可表达参数空间较窄。
- Example Label：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`
- Example May Illustrate：schema surface、required/type/enum 对可表达参数的静态约束差异。
- Example Must Not Claim：selection rate、actual schema adherence、error rate、latency、token、跨 Provider 一致性或最终质量变化。
- Figure：`Table 1｜Calculator Contract Surface A/B`，列 `Visible Surface / Static Constraint / Host Still Owns / Not Observed`。
- Figure Must Not Imply：这是一项实验、存在 Expected / Observed、或 Schema B 已被任何模型证明更好。
- Transition：合同表面可以抽象，消息里的 call / result 形状不能抽象成同一套字段；下一节分别读两家 documented trace。

## 7. Section 3｜两家 official trace：共同的是 correlation，具体的是 item / content-block shape

- Problem：若只展示一条“标准 Tool Calling JSON”，读者会把课程 normalized sequence 误认为跨 Provider wire schema；若只列字段，又看不见 correlation 跨越下一次 request 的机制。
- Section Goal：让 OpenAI Responses 与 Anthropic Messages 各自承担一个 concrete mechanism 落地点，再提炼最小共同抽象。
- Core Thesis：两家 current document contract 都要求 Host 保留 call identifier，把 correlated Tool Result 放入下一次 model request；identifier 名称、message placement 与 content shape 不相同。
- Claim IDs：`05-C03`
- Evidence IDs：`S-01`、`S-03`
- Trace A — OpenAI Responses：
  - Responsibility：展示 response output item `function_call(call_id, name, arguments)` 与下一次 input `function_call_output(call_id, output)` 的 documented correlation。
  - Label：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`
  - Must Not Prove：本地 credentials、network、model choice、Tool side effect、output truth、final answer quality。
- Trace B — Anthropic Messages：
  - Responsibility：展示 assistant `tool_use(id, name, input)` content block 与下一条 user message `tool_result(tool_use_id, content)` 的 documented correlation / placement。
  - Label：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`
  - Must Not Prove：本地 Provider/runtime、Tool side effect、result truth、next answer quality。
- Counter-evidence：同一 Provider 的其他 API surface 也可能使用不同 message / item shape；OpenAI 与 Anthropic 当前字段不支持统一 payload。
- Guardrail：先分别画两条 trace，再提炼 `call -> Host decision -> correlated result -> next request`；不把 normalized lane 标成 official schema，不写“Role / Item 可以一一转换”。
- Example：使用两个并列、字段最小化的 message timeline；不嵌入可运行 SDK 代码，不添加 Evidence manifest 之外的 Provider 字段。
- Example Label：`DOCUMENTED ROUNDTRIP SHAPE / NO LOCAL ROUNDTRIP`
- Figure：`Figure 3｜OpenAI Responses and Anthropic Messages Correlation Lanes`。
- Figure Must Not Imply：两个 contract 同构、Result 由模型自动生成、Host 已实际执行 Tool。
- Transition：Trace 说明了 completed call / result 的位置；streaming 时还要先回答 arguments 何时才成为 completed candidate，以及同一 response 有多个 call 时怎样保持归属。

## 8. Section 4｜从 fragment 到 executed result：五个状态不能提前合并

- Problem：streaming helper、strict mode 或“看起来完整的 JSON”会诱使 Application 把早期 fragment 提前送入 Parse、authorization 或 execute；同一 response 多个 call 又会放大片段串线与结果错配风险。
- Section Goal：建立 arguments 状态序列，并把 multiple-call output shape 与 Host concurrency decision 分开。
- Core Thesis：fragment 只能按 call / item / block 归属缓冲；Provider completion 后才得到 completed candidate，随后才依次进入 validation、authorization 与 optional execution。一个 response 可含多个 call，但“多个”不等于 Host 必须并行。
- Claim IDs：`05-C04`、`05-C05`
- Evidence IDs：`S-01`、`S-04`、`S-05`、`S-06`、`R-01`、`R-02`
- State Model：

```text
ARGUMENT_FRAGMENT
  -> COMPLETED_ARGUMENTS_CANDIDATE
  -> VALIDATED_ARGUMENTS
  -> AUTHORIZED_ACTION
  -> EXECUTED_RESULT
```

- State Responsibilities：
  - fragment：只按 Provider key / index / block 归属缓冲；
  - completed candidate：只表示到达 Provider-defined completion seam；
  - validated arguments：沿 Article 03 的 Parse / Schema / DTO / Domain 责任检查；
  - authorized action：只表示越过本篇点到为止的 Host authorization seam；
  - executed result：只在真实 operation 返回后存在；本篇没有该 observation。
- Multiple-call Plan：画出 `Call A`、`Call B` 独立的 buffer、id、decision 与 result；Host 根据 Provider control surface、Tool semantics、共享状态 / 副作用约束决定 concurrent、sequential 或 mixed，而不是由“同一 response 有多个 call”自动决定。
- Counter-evidence：SDK accumulator helper 可以减少手工拼装；scoped strict / buffered mode 可以提供更强保证；它们都不能把 fragment 提前升级。并非所有 model / tool combination 都具有相同 parallel semantics。
- Guardrail：Anthropic invalid / incomplete JSON 只作为 fine-grained tool streaming feature scope 的 official counterexample；不外推到 Anthropic standard buffering 或 OpenAI strict。不得写本轮观察到 actual fragment order、parallel frequency、execution order 或 error recovery。
- Example：两条 call 的 conceptual keyed-buffer sketch，只显示 independent correlation；不安排“最佳执行顺序”。
- Example Label：`CONCEPTUAL MULTI-CALL CORRELATION / NOT RUNTIME OBSERVATION`
- Figure：`Figure 4A｜Argument State Ladder` + `Figure 4B｜Multiple Calls, Independent Correlation`。
- Figure Must Not Imply：某种 concurrency 总是正确、Provider 会解决副作用冲突、或本文实现了 scheduler / Tool Runtime。
- Transition：当 completed candidate 到达 Host 后，仍要经过 registry、invalid / unauthorized gate；下一节只讲“谁有权拒绝”，不展开完整 Runtime 实现。

## 9. Section 5｜Host 决定权：unknown、invalid、unauthorized 都必须停在副作用之前

- Problem：如果 Application 只按模型返回的 name 调用同名函数，unknown Tool、invalid arguments 和 unauthorized action 会被当作 dispatch detail，而不是 fail-closed boundary。
- Section Goal：落地 Host registry / decision seam，解释 `Schema Valid != Authorized`，但不吞掉 Article 06 / 19 的 Runtime 与权限模型。
- Core Thesis：对 client-executed tools，Provider call item 不会绕过本地 registry、completed-argument handling、Parse / Schema / Domain 与 authorization decision；Host 可 reject、返回 error result 或在允许时 execute。
- Claim IDs：`05-C01`、`05-C06`
- Evidence IDs：`S-01`、`S-05`、`S-07`、`R-01`
- Decision Skeleton：

```text
call intent
  -> registered name?         no  -> reject / error result
  -> completed arguments?     no  -> reject / error result
  -> parse / validate?        no  -> reject / error result
  -> authorized?              no  -> reject / approval boundary
  -> optional execute         yes -> executed result
```

- Counter-evidence：scoped strict mode 可以减少某些结构性失败；Provider / SDK 也可自动承载部分 routing。它们不生成本地 registry、domain fact、permission、approval 或 side-effect policy。
- Guardrail：只建立 seam，不设计 Permission object、approval UX、sandbox、credential scope、timeout、retry、idempotency、audit 或 Trace；这些分别留给 Article 06 / 19 及后续文章。
- Example：synthetic `deleteFile` negative 分两种教学分支：
  - `unknown`：name 不在允许的 registry；
  - `unauthorized`：即使 name / schema 形式成立，当前 Host authorization 仍不允许执行。
- Example Label：`SYNTHETIC NEGATIVE EXAMPLE / NOT_EXECUTED / NO FILE SIDE EFFECT OBSERVED`
- Example Must Not Claim：模型实际生成过 `deleteFile`、某 Provider 会违反 tool list、Host 已实现 permission system、error 后模型会 recover。
- Figure：`Figure 5｜Fail-closed Host Decision Gate`，所有拒绝分支终止在 execute 之前。
- Figure Must Not Imply：authorization 的完整设计已经定义、permission 与 schema 是同一层、或 Runtime failure injection 已执行。
- Transition：Host 的决定需要通过 correlated result 告诉模型；但“回注了一个结果”仍不能自动把内容升级成 Evidence。

## 10. Section 6｜Tool Result 回注：消息闭环不等于事实闭环

- Problem：Tool Result 位于专用 envelope / content block 中，容易被误认为“这就是可信工具事实”。
- Section Goal：解释 correlated result 的消息职责，并保留 `Tool Result by itself != Evidence` 的 `PARTIAL` 边界。
- Core Thesis：generic Tool Result 是与 call 关联并放入下一次 model request 的 content；只有附加 provenance / verification contract 后，某个具体 result 才可能成为 Evidence 输入。
- Claim IDs：`05-C03`、`05-C07`（`PARTIAL`）
- Evidence IDs：`S-01`、`S-03`、`R-03`
- Result Responsibility：
  - correlation：让下一次请求知道内容对应哪个 call；
  - status / content：可表达成功内容或 error content，具体 shape 按 Provider；
  - continuation：给模型生成文本或更多 calls 的输入；
  - non-guarantee：envelope 本身不统一保证来源、采集时间、claim mapping 或独立 verification。
- Counter-evidence：特定 server/search tools、document blocks 或业务 Tool 可以携带 citation、source metadata 或 verified artifact；它们反驳“Tool Result 永远不能成为 Evidence”。
- Guardrail：只写 `generic Tool Result by itself != Evidence`；不得写“所有 Tool Result 都不可信”，不得定义完整 Evidence schema，不评价具体 citation contract。
- Example：同一 `result content` 画两条用途线：一条直接作为 model continuation input；另一条只有在“额外 provenance / verification contract”存在时才进入 Evidence consideration。
- Example Label：`COURSE BOUNDARY SKETCH / NO RESULT TRUTH OBSERVED`
- Figure：`Figure 6｜Message Closure vs Evidence Closure`。
- Figure Must Not Imply：本文已经定义 Evidence Contract、验证了某个 Tool output、或 citations 自动正确。
- Transition：即使 call/result message closure 已完成，也只证明一次 Tool Use 往返；最后一节说明为什么这仍不足以证明 Agent Loop。

## 11. Section 7｜为什么一次 Tool Use 仍不足以证明 Agent Loop？

- Problem：官方文档、SDK Tool Runner 或产品文案可能把持续 Tool Use 称为 agentic loop，读者容易把任意 request → call → result → answer 都归类为完整 Agent。
- Section Goal：只建立 Article 05 到 08 的必要性边界，不在本篇正式定义 Turn / Step / loop state / stop。
- Core Thesis：Tool Use 可以参与 Agent Loop；但一次 Tool Use 只证明一个 call/result roundtrip，不能单独证明课程定义下围绕目标推进、多步行动 / 反馈、runtime state 与 stop semantics 已闭合。
- Claim IDs：`05-C08`（`PARTIAL / COURSE WORKING BOUNDARY`）
- Evidence IDs：`S-01`、`S-07`、`R-03`、`R-04`
- Counter-evidence：SDK Tool Runner 或 Provider server-side loop 可能在一个高层 API abstraction 内持续多轮；不同生态可以合理地把这类产品能力称为 agentic。
- Guardrail：使用“按课程工作定义，一次 Tool Use 不足以证明”而非“所有 single-turn tool 应用都不是 Agent”；不否定官方产品术语，不主张行业唯一 Agent 定义。
- Example：`request -> one call -> one result -> answer` 的单往返示意；只标出“尚未由本篇证明的 goal-driven multi-step / runtime state / stop closure”，不提前讲 08 的机制。
- Example Label：`BOUNDARY EXAMPLE / NOT AN AGENT CLASSIFICATION TEST`
- Figure：`Figure 7｜One Roundtrip vs Unproven Loop Closure`。
- Figure Must Not Imply：多调用次数本身定义 Agent、single-turn 应用绝对不是 Agent、或 Article 08 的 Loop 已在本文建立。
- Transition：回到全文审查清单：看到 Tool Call 时，先问 intent、contract、state、Host decision、correlation 与 evidence boundary，再决定下一层由谁处理。

## 12. Closing｜怎样审查一条 Function Calling / Tool Use 链？

- Problem：读者理解单个字段后，仍可能缺少能带回项目的完整审查顺序。
- Section Goal：把全文压缩成一张 verification checklist 与一句最短结论，不添加新概念。
- Core Thesis：审查重点不是“有没有 Tool Call 字段”，而是每次语义升级是否有明确 completion、validation、authorization、execution、correlation 与 evidence owner。
- Claim IDs：`05-C01`—`05-C08`
- Evidence IDs：`S-01`—`S-07`、`R-01`—`R-05`
- Checklist Plan：
  1. 当前讨论的是 client-executed tool，还是 Provider-managed built-in / server tool？
  2. Tool definition / choice 的 Provider、API、model、version scope 是什么？
  3. 当前对象是 fragment、completed candidate、validated arguments、authorized action，还是 executed result？
  4. call identifier 怎样跨下一次 request 关联 Result？
  5. 同一 response 多个 call 怎样独立保留 buffer、decision 与 result？Host 是否有明确 execution-order owner？
  6. unknown / invalid / unauthorized 是否都在副作用前 fail closed？
  7. Result 只完成了消息回注，还是另有 provenance / verification contract？
  8. 当前证据只证明一次 Tool Use，还是已由后续机制证明 Agent Loop？
- Counter-evidence：server tool、strict mode、Tool Runner、parallel-call output、rich result 与 agentic terminology 都保留为检查中的 scope branches，而不是删掉的例外。
- Guardrail：Checklist 不升级为 Runtime implementation、MCP architecture、Permission policy、Evidence schema 或 Agent Loop definition。
- Example：用 Calculator contract、两家 trace、multi-call sketch、`deleteFile` negative 四张小卡回看同一审查顺序；每张卡保留自己的 evidence label。
- Figure：`Figure 8｜Function Calling Review Checklist`。
- Figure Must Not Imply：任何 fixture / trace 在本地执行过，或 Outline Gate 已由 Author 自行批准。
- Transition：进入 Learning Check；Draft 结尾只用无链接 prose 引出 Article 06 Tool Runtime，不提前建立未发布文章的 `relref`。
- Shortest Takeaway Responsibility：压缩为“模型表达行动意图；Host 保留执行决定；Result 回注仍不等于 Evidence 或 Agent Loop”这一责任判断，不写新 Claim。

## 13. Figure / Table / Example Responsibilities

| ID | Artifact | Evidence Label | Teaching Responsibility | Must Not Imply |
|---|---|---|---|---|
| Figure 1 | Structured Result vs Tool Call Intent | `CONCEPTUAL / NOT_EXECUTED` | 立住数据候选与行动请求的语义差别 | call 已执行、业务 result 与 tool call 共用 wire schema |
| Figure 2 | Client-tool Responsibility Chain | `COURSE ABSTRACTION / DOC-BACKED` | 展示 Host decision 与 next-request correlation | 所有 tools 都由 Host 进程执行、行业统一架构 |
| Table 1 | Calculator Contract Surface A/B | `SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED` | 展示 model-visible schema surface 的静态差异 | selection / adherence / quality improvement |
| Figure 3 | Two Provider Correlation Lanes | `OFFICIAL EXAMPLE / NOT LOCALLY EXECUTED` | 分别落 OpenAI Responses / Anthropic Messages document contract | 统一 wire schema、本地 roundtrip |
| Figure 4A | Argument State Ladder | `DOC-BACKED COURSE MODEL` | 分开 fragment、candidate、validated、authorized、executed | strict/helper 可提前升级 fragment |
| Figure 4B | Multiple Calls, Independent Correlation | `CONCEPTUAL / NOT RUNTIME OBSERVATION` | 分开 output shape 与 Host concurrency decision | Host 必须并行、某种顺序最佳 |
| Figure 5 | Fail-closed Host Decision Gate | `SYNTHETIC NEGATIVE / NOT_EXECUTED` | 展示 unknown / invalid / unauthorized 止于副作用前 | Permission / Runtime 已实现 |
| Figure 6 | Message Closure vs Evidence Closure | `PARTIAL COURSE BOUNDARY` | 展示 generic Result 需要额外 Evidence contract | 所有 result 永远不能成为 Evidence |
| Figure 7 | One Roundtrip vs Unproven Loop Closure | `PARTIAL COURSE BOUNDARY` | 说明 Tool Use 可能是 Loop 部件但不构成充分证明 | 行业唯一 Agent 定义 |
| Figure 8 | Function Calling Review Checklist | `SYNTHESIS` | 提供项目审查入口 | runtime verification 已完成 |

Asset Policy：本轮不创建 `assets/`。Draft 优先使用 Markdown 文本图、表格和短伪代码；不生成性能图、准确率图、执行截图或 runtime trace 图。

## 14. Fixture / Negative Example Label Contract

### Calculator A/B

- Required Label：`SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`
- Provider / Model / Parameters：`NONE / NONE / NONE`
- Expected / Observed：`NONE / NONE`
- Allowed Wording：model-visible contract 不同；required / type / enum 静态收窄可表达参数空间。
- Forbidden Wording：更准确、更稳定、选择率更高、adherence 更高、错误更少、速度更快、token 更省。

### `deleteFile` negative

- Required Label：`SYNTHETIC NEGATIVE EXAMPLE / NOT_EXECUTED / NO FILE SIDE EFFECT OBSERVED`
- Allowed Wording：展示 registry unknown 与 authorization denied 两个 fail-closed branch。
- Forbidden Wording：模型实际生成、Provider 实际违反 schema / tool list、文件实际删除、Host permission 已实现、模型收到 error 后实际恢复。

### OpenAI / Anthropic traces

- Required Label：`OFFICIAL EXAMPLE / DOCUMENT CONTRACT / NOT LOCALLY EXECUTED`
- Allowed Wording：各自 current documented item / content-block placement 与 identifier correlation。
- Forbidden Wording：本地 request/response、credential/network/SDK success、Tool execution、Result truth、final-answer quality。

## 15. Learning Check Plan

| # | Question | Claim Coverage | Reference-Thought Responsibility |
|---:|---|---|---|
| 1 | 模型返回一个 client-tool `deleteFile` call，文件是否已经删除？先检查哪几个 Host gate？ | `05-C01`、`05-C06` | 引导区分 intent、registry、completed arguments、validation、authorization、execution；重申 synthetic negative 未执行。 |
| 2 | Tool arguments 符合 scoped schema，是否意味着业务允许执行？ | `05-C02`、`05-C06` | 引导区分 schema / domain / authorization；不展开 Permission 实现。 |
| 3 | Calculator-B 比 Calculator-A 多了 enum 和 typed operands，本文能否说它让模型更准确？ | `05-C02` | 引导识别静态 contract surface 与 runtime effect evidence 的差别；答案必须引用 `NOT_EXECUTED`。 |
| 4 | OpenAI `call_id` 与 Anthropic `tool_use.id -> tool_use_id` 的共同职责是什么？为什么不能合成统一 payload？ | `05-C03` | 引导说出跨下一 request correlation 与 Provider-specific placement / field shape。 |
| 5 | 收到 arguments delta 后为什么不能立即 Parse 或 execute？ | `05-C04` | 引导沿 fragment → completion → validation → authorization → execution 分态；不声称某次 stream 已观测。 |
| 6 | 同一 response 有两个 call，Host 是否必须并行？每个 call 至少要独立保存什么？ | `05-C05` | 引导区分 output shape 与 concurrency decision，并点出 id / buffer / arguments / decision / result correlation。 |
| 7 | Tool Result 位于专用 result block，为什么仍不能自动叫 Evidence？什么反例要求我们保留窄措辞？ | `05-C07` | 引导说出 generic envelope 缺少统一 provenance / verification，同时承认 rich server/search result 可有额外合同。 |
| 8 | 一次查天气的 call → result → answer 是否已经证明课程定义下的 Agent Loop？ | `05-C08` | 引导使用“不足以证明”而非绝对分类；指出正式 Loop 的 goal/state/step/stop closure 留给 08。 |
| 9 | 哪些结论来自 official document contract，哪些在本文仍是 `UNVERIFIED`？ | 全部 | 引导逐类区分 Provider fields / trace contract、synthetic fixtures、runtime behavior、side effect 与 recovery。 |

Reference-thought Style：给出判断路径和缺失证据，不写成背字段答案；每题至少包含一个“能证明 / 不能证明”边界。

## 16. Claim-to-Section Coverage Matrix

| Claim ID | Status | Main Placement | Evidence IDs | Counter-evidence / Guard |
|---|---|---|---|---|
| `05-C01` | `CONFIRMED` | Opening、Section 1、5、Closing | `S-01`、`S-03`、`S-07` | built-in / server tools 限定 client-executed scope；不声称本轮执行 |
| `05-C02` | `CONFIRMED` | Section 2、Learning 2—3 | `S-01`、`S-02` | Provider fields / strict / choice scoped；fixture 不证明效果 |
| `05-C03` | `CONFIRMED` | Section 1、3、6 | `S-01`、`S-03` | 两家 message / item shape 不统一；official example 非 local observation |
| `05-C04` | `CONFIRMED` | Section 4、Learning 5 | `S-01`、`S-04`、`S-05`、`R-01`、`R-02` | fine-grained invalid JSON 只限其 feature scope；strict/helper 不升级 fragment |
| `05-C05` | `CONFIRMED` | Section 4、Learning 6 | `S-01`、`S-06` | multiple calls 不规定 Host 并行；不写实际频率 / 顺序 |
| `05-C06` | `CONFIRMED` | Section 5、Learning 1—2 | `S-01`、`S-05`、`S-07`、`R-01` | strict 不生成 registry / authorization；negative 未执行 |
| `05-C07` | `PARTIAL` | Section 6、Learning 7 | `S-01`、`S-03`、`R-03` | rich results 可带 provenance；只保留 generic-result 窄命题，18 正式展开 |
| `05-C08` | `PARTIAL` | Section 7、Learning 8 | `S-01`、`S-07`、`R-03`、`R-04` | agentic terminology / Runner counterexample；只说不足以证明，08 正式展开 |

Coverage Result：`8 / 8 Claims mapped`；状态保持 `6 CONFIRMED / 2 PARTIAL / 0 BLOCKED / 0 PROPOSAL`。

## 17. Job Competency Mapping

| Competency | Article Evidence of Learning | Assessment Surface |
|---|---|---|
| Provider contract reading | 能分别读取 Responses item 与 Messages content block，并抽象 correlation 而不伪造统一 schema | Section 2—3 + Learning 4 |
| Interface / seam design | 能切开 model-visible definition、Host registry、validation、authorization、execution 与 result return | Section 1、5 + Learning 1—2 |
| Streaming state modeling | 能把 fragment、completed candidate、validated、authorized、executed 分态 | Section 4 + Learning 5 |
| Multi-call correctness judgment | 能为每个 call 保留独立 correlation，并把 concurrency 留给 Host / Tool semantics | Section 4 + Learning 6 |
| Fail-closed engineering | 能识别 unknown / invalid / unauthorized，阻止它们越过副作用边界 | Section 5 + Learning 1—2 |
| Evidence discipline | 能区分 generic Tool Result 与带额外 provenance / verification 的 Evidence candidate | Section 6 + Learning 7、9 |
| Architecture boundary judgment | 能区分 Function Calling、Tool Runtime、MCP、Agent Loop、Evidence 与 Permission | Section 7 + Adjacent Stop Lines |
| Experiment literacy | 能识别 synthetic fixture、official example 与 runtime observation 的不同证明力 | Section 2—3、14 + Learning 3、9 |

## 18. Adjacent Article Stop Lines

| Adjacent / Future Article | Article 05 May Introduce | Article 05 Must Stop Before |
|---|---|---|
| Article 03｜Structured Output | completed arguments 继续经过 Parse / Schema / DTO / Domain | 重讲 JSON Schema / Lab 01、把 schema valid 写成 authorized |
| Article 04｜Adapter / Gateway | tool arguments fragment 只能 buffer，Provider completion 后才形成 candidate | 重讲 streaming protocol、error / retry / Gateway |
| Article 06｜Tool Runtime | registry、validation、authorization、reject / error / optional execute seam | ToolDefinition implementation、Canonicalize、Policy、Execute、Timeout、Retry、Result validation、Trace、failure injection |
| Article 07｜MCP | 外部能力还需要后续协议边界 | transport、discovery、interoperability、MCP server / client architecture |
| Article 08｜Agent Loop | Tool Use 可以成为 Loop 的一部分；一次往返不足以证明 Loop | Turn、Step、Decide、Act、Observe、state、stop 的正式机制 |
| Article 18｜Evidence Contract | generic Tool Result 不自动成为 Evidence | provenance schema、claim-to-source mapping、verification workflow |
| Article 19｜Permission / Approval / Sandbox | `Schema Valid != Authorized`，Host 保留授权 seam | permission model、approval UX、credential / filesystem scope、sandbox enforcement |
| Article 35｜DSH Tool Pipeline | 本篇机制未来可作为模型侧入口的阅读前置 | DSH source path、symbol、call path 或 runtime behavior |
| BuildPilot | 模型只能请求能力、Harness 保留执行决定 | BuildPilot Tool Runtime implementation 或 production claim |

Cross-boundary Rule：Draft 若需要 timeout、retry、actual error recovery、MCP、multi-step loop、Evidence schema、permission implementation、DSH source/runtime 或 BuildPilot implementation 才能支撑段落，删除越界内容；若该内容成为核心论证所需事实，则 `RETURN_TO_RESEARCH`。

## 19. Source / Link Plan

### External source whitelist

Draft 外链只允许使用 Evidence Manifest 的下列 current official / primary sources；不新增教程、博客、产品对比或未登记链接：

| ID | Planned Link | Draft Responsibility |
|---|---|---|
| `S-01` | [OpenAI Function calling](https://developers.openai.com/api/docs/guides/function-calling) | OpenAI Responses definition / choice / call / result / strict / parallel / streaming document contract |
| `S-02` | [Anthropic Define tools](https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools) | Anthropic definition / choice / strict document contract |
| `S-03` | [Anthropic Handle tool calls](https://platform.claude.com/docs/en/agents-and-tools/tool-use/handle-tool-calls) | call/result correlation、placement、error-result document contract |
| `S-04` | [Anthropic Streaming Messages](https://platform.claude.com/docs/en/build-with-claude/streaming) | partial JSON / content block completion document contract |
| `S-05` | [Anthropic Fine-grained tool streaming](https://platform.claude.com/docs/en/agents-and-tools/tool-use/fine-grained-tool-streaming) | scoped invalid / incomplete JSON counterexample 与 parse guard |
| `S-06` | [Anthropic Parallel tool use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/parallel-tool-use) | multiple tool-use blocks、Host execution-order boundary |
| `S-07` | [Anthropic How tool use works](https://platform.claude.com/docs/en/agents-and-tools/tool-use/how-tool-use-works) | client / server tool scope 与 documented tool-use loop |

External Link Rule：发布前 Publisher / Reviewer 必须按 Evidence 要求重查 hosted current docs；Outline 不把 `2026-08-20` 的核对升级为永久版本保证。

### Local source and navigation plan

| ID / Purpose | Local Target | Link / Use Plan |
|---|---|---|
| `R-01` | [Published Article 03](../../../../content/ai-empowerment/agent-engineering-03-structured-output-machine-contract.md) | 继承 Parse / Schema / DTO / Domain，必要时正文以普通站内链接引用 |
| `R-02` / Backward navigation | [Published Article 04](../../../../content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md) | Published Content 顶部“上一篇”使用 `relref`；正文承接 fragment / terminal boundary |
| `R-03` | [Course Glossary](../../glossary.md) | 只引用 Tool、Agent、Host、Evidence 当前工作定义；不借 PI-F03 缺失术语扩写 glossary support |
| `R-04a` | [Canonical Series Plan](../../../agent-engineering-series-plan.md) | title、Part、weight、dependency 与 stop line |
| `R-04b` | [Frozen Article 05 Plan](../../../agent-engineering-course-plan-v3.1-review.md) | canonical content spine、examples、Learning Check 与 M scope |
| `R-05` | [Part I Audit](../../audits/part-i-audit.md) | 只用于 verified handoff 与 `PI-F03 OPEN MINOR` 边界；不修 Finding |
| Current Evidence | [Article 05 Evidence Register](evidence.md) | Claim / Evidence / label truth source |
| Current Research | [Article 05 Research](research.md) | Provider differences、counter-evidence 与 stop lines |

Publication Plan：

- Published Target：`content/ai-empowerment/agent-engineering-05-function-calling-tool-use.md`（仅 Publisher 在后续 Gate 创建）。
- Backward `relref` target：`ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`，shortcode 参数使用 ASCII 双引号。
- Article 03 如需正文内引用，使用已存在 target；不重复 Part I 全部导航。
- Future Article 06 / 07 / 08 / 18 / 19 尚未发布，只用无链接 prose stop line，不创建可能导致 `REF_NOT_FOUND` 的 `relref`。
- 本 Outline 不修改 Article 04 forward navigation；是否回补由后续 Publisher / Master transaction 决定。

## 20. Length Budget

| Section | Budget | Compression Rule |
|---|---:|---|
| Opening | 400—550 字 | 一个误判 + 一张对照图，不回顾完整 Article 03 / 04 |
| Section 1 Abstract Model | 550—700 字 | 全文只保留一条主责任链；Runner / server tool 只作 counterexample |
| Section 2 Schema / Choice | 650—800 字 | 两家字段只保留 contract surface；Calculator 不写实验叙事 |
| Section 3 Two Traces | 750—950 字 | 两条 lane 各自完整、字段最小；不放 SDK 教程 |
| Section 4 State / Multiple Calls | 700—900 字 | 五状态 + multi-call 独立 correlation；不扩成 scheduler |
| Section 5 Host Decision | 650—850 字 | registry / invalid / authorization seam；不展开 Runtime / Permission 实现 |
| Section 6 Result / Evidence | 450—600 字 | 保留 `PARTIAL` 窄命题与 rich-result counterexample |
| Section 7 Not Yet Agent | 400—550 字 | 只讲不足以证明，不定义 Loop |
| Closing + Learning Check | 450—600 字 | Checklist、问题与参考思路压缩重复定义 |

Budget Result：主体约 `5,000—6,500 中文字`；若 Draft 超预算，优先压缩 Provider字段、重复 boundary 与示例说明，不删 C01—C08 coverage、证据标签或 stop lines。

## 21. Evidence Omission / Runtime Guard List

- 不新增 Provider request、response、credentials、network、SDK log、Tool execution、side effect 或 final-answer observation。
- 不声称某 schema / description / enum / choice 提高 selection quality、actual schema adherence、accuracy、reliability、latency 或 cost。
- 不声称本轮实际出现 multiple / parallel calls，也不写实际频率、顺序或最佳 concurrency。
- 不声称 unknown tool、invalid arguments、Tool error 或 denied authorization 在 runtime 中实际发生或被模型恢复。
- 不把 OpenAI / Anthropic official example 写成 local trace；不把课程 normalized lane 写成统一 wire schema。
- 不把 argument fragment 写成 completed candidate，不把 completed candidate 写成 validated / authorized / executed。
- 不把 scoped strict guarantee 外推到其他 Provider / API / model / version，也不写“arguments 永远不可靠”。
- 不把 generic Tool Result 写成天然 Evidence，也不写“Tool Result 永远不能成为 Evidence”。
- 不把一次 Tool Use 绝对分类为“不是 Agent”；只写课程定义下不足以证明完整 Agent Loop。
- 不展开 06 Tool Runtime、07 MCP、08 Agent Loop、18 Evidence Contract、19 Permission / Approval / Sandbox。

## 22. New Core Facts Audit

| Candidate Addition | Classification | Evidence / Disposition |
|---|---|---|
| Article thesis 与 client-tool responsibility chain | Existing core Claim synthesis | `05-C01`、`05-C03`；无新事实 |
| Calculator A/B | Editorial teaching fixture | Research 已冻结；保留 `SYNTHETIC / NOT_EXECUTED`，不产生效果 Claim |
| Two Provider lanes | Existing document-contract examples | `05-C03` / `S-01`、`S-03`；不新增字段或 local observation |
| Five-state ladder | Existing Claim synthesis | `05-C04`、`05-C06`、`R-01`、`R-02`；是课程教学状态，不是统一 Provider protocol |
| Multiple-call independent correlation | Existing confirmed mechanism | `05-C05` / `S-01`、`S-06`；不增加 actual order / frequency |
| `deleteFile` negative | Editorial synthetic negative | `05-C06` 已允许；保留 unknown / unauthorized 两分支与 `NOT_EXECUTED` |
| Message closure vs Evidence closure | Existing `PARTIAL` course boundary | `05-C07`；不定义 Article 18 schema |
| One roundtrip vs Agent Loop | Existing `PARTIAL` course boundary | `05-C08`；不定义 Article 08 mechanism |
| Length、Figures、section order | Editorial planning metadata | 不构成技术 Claim |

New Core Facts Result：`0`。

### RETURN_TO_RESEARCH decision

- Decision：`NONE`
- Reason：全部主体段落、examples、figures 与 Learning Checks 可由 `05-C01`—`05-C08` 和当前 Evidence Manifest 支撑；没有新增 runtime、Provider behavior 或后续 Article mechanism。
- Mandatory Return Triggers：Draft 若需要实际 model selection / adherence、parallel frequency / order、unknown / invalid / authorization failure observation、error recovery、SDK Tool Runner behavior、side effect、Provider roundtrip 或 Evidence Manifest 外的核心 Provider 字段，必须停止并返回 `RETURN_TO_RESEARCH`。

## 23. Outline Gate Checklist and Recommendation

- [x] H1 与 canonical Article 05 标题精确一致。
- [x] Article Type 明确为原理篇（机制型）/ Normal Article；第一屏从工程误判开场，不从 API 字段列表开场。
- [x] Teaching Spine 遵循 Problem Space → Abstract Model → Concrete Mechanism → Engineering Judgment → Verification / Learning Check。
- [x] 每个主体 Section 都包含 Problem、Claim IDs、Evidence IDs、Counter-evidence、Guardrail、Example、Figure 与 Transition。
- [x] `8 / 8` Claims 显式映射；`05-C07`、`05-C08` 保持 `PARTIAL`。
- [x] client-executed scope 与 built-in / server-tool counterexample 均已保留。
- [x] Provider definition / choice / trace fields 保持各自 scope，没有发明统一 wire schema。
- [x] call/result correlation 明确跨越下一次 request；两家 trace 均标 `OFFICIAL EXAMPLE / NOT LOCALLY EXECUTED`。
- [x] fragment → completed candidate → validated → authorized → executed 已分态。
- [x] multiple calls 不等于 Host 必须并行；未新增频率 / 顺序 / recovery Claim。
- [x] registry / unknown / invalid / unauthorized 保持 fail-closed seam；`deleteFile` 明示 synthetic negative。
- [x] Calculator A/B 明示 `SYNTHETIC TEACHING FIXTURE / NOT_EXECUTED`，无效果 Claim。
- [x] generic Tool Result / Evidence 与 one Tool Use / Agent Loop 都采用窄 `PARTIAL` 措辞。
- [x] 06 Tool Runtime、07 MCP、08 Agent Loop、18 Evidence、19 Permission stop lines 明确。
- [x] Learning Check、参考思路职责、Job Competency 与 M-level length budget 完整。
- [x] 外链只使用 Evidence Manifest `S-01`—`S-07`；本地链接目标存在计划可验证。
- [x] `new core facts = 0`；`RETURN_TO_RESEARCH = NONE`。
- [x] 本 Gate 未创建 Draft、assets、Published Content，也未修改 global / canonical / glossary / Article 06。

Recommendation：`PASS_RECOMMENDED`。由 Master 独立核对 Outline Gate；通过后的唯一下一动作是 `AUTHOR_DRAFT`，由 Author 仅依据本 Outline 与批准 Evidence 创建当前 Article 的 `draft.md`。不得把本候选推荐写成 Author 自批准，也不得跳到 Review、Publish 或 Article 06。
