# Model Adapter 与 LLM Gateway：Streaming、Error、Retry 和 Provider 差异

- Lifecycle Input：`EVIDENCE_READY`
- Evidence Gate：`PASS`（`3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`）
- Outline Gate：`PASS_RECOMMENDED`（由 Master 核对后推进）
- Article Type：`原理篇 / Normal Article`
- Course Weight：`M（Standard Core Lesson）`
- Target Length：`约 5,000—6,500 中文字`
- Target Reading Time：`14—18 分钟`
- Provider Evidence Scope：`OpenAI Responses + Anthropic Messages 当前官方合同，核对于 2026-08-20（Asia/Shanghai）`
- Gateway Evidence Scope：`Cloudflare AI Gateway + Azure API Management AI gateway 当前官方产品文档，核对于 2026-08-20（Asia/Shanghai）`
- Provider Calls：`NONE`
- Runtime Evidence：`UNVERIFIED`
- Required Lab：`NONE`
- Fake Provider：`PROPOSAL / NOT_EXECUTED`

## 1. Reader Transformation

读者从“Provider 切换就是替换 endpoint、model name 和 API key”，转变为能够把一次模型调用拆成 Provider-neutral request、Provider Adapter、stream partial、Provider terminal、final validation、error classification 与 retry policy；能够判断哪些差异可被归一化、哪些差异必须保真暴露，以及为什么 Gateway 的流量治理不能替代 Agent Runtime 的执行状态。

完成本篇后，读者应能独立做出六个判断：

1. Provider switch 是一次合同迁移，不只是 URL 替换；
2. Adapter 负责 Provider contract translation，不拥有领域 DTO、领域真值或 Agent loop；
3. `partial != terminal != final validated result`，tool argument fragment 不能直接进入 DTO 或执行；
4. 错误必须先按发生层分类，429、timeout、5xx、refusal、truncation 与 schema/domain failure 不能共用一个 retry 开关；
5. transport retry、semantic/business retry 与 recovery 是不同动作，且自动 retry 需要唯一 owner、request state、replay safety、budget 与 stop gate；
6. Adapter、Gateway 与 Agent Runtime 是课程中的责任边界，不是行业唯一命名，也不要求分别部署。

## 2. Teaching Spine

> 如果这篇只记一句话：`Model Adapter 的价值不是抹平所有 Provider，而是把可归一化的调用职责收口，同时让不可归一化的能力、终态和失败保持可见。`

| Teaching Phase | Reader Movement | Main Sections | Claim / Evidence |
|---|---|---|---|
| Problem Space | 从“换 URL”转向“请求、流、终态、Usage、错误与 SDK retry 都是迁移面” | Opening、Section 1 | `04-C01` / `S-01`—`S-13` |
| Abstract Model | 建立 Domain / Task Policy → Provider-neutral Request → Adapter → Provider → normalized evidence → Article 03 validation 的责任链 | Section 2 | `04-C02` / `04-C01`、`R-02`—`R-04` |
| Concrete Mechanism | 区分 stream partial、Provider terminal、final validation，并按失败层分类 | Section 3—4 | `04-C03`、`04-C04` / `S-01`—`S-03`、`S-06`、`S-08`—`S-13`、`R-03` |
| Engineering Judgment | 区分 transport retry、semantic retry、recovery，并切开 Adapter、Gateway、Runtime | Section 5—6 | `04-C05`—`04-C07` / `S-03`—`S-16`、`R-03`—`R-04` |
| Verification Boundary | 用 capability descriptor 与 integration checklist 暴露适配前提、未知项和验证缺口 | Section 7、Closing | `04-C08` / `04-C01`、`S-07`—`S-08`、`R-02`—`R-04` |

### M 级篇幅职责

- 围绕一条 Provider integration responsibility chain 展开，不写成 OpenAI / Anthropic API 对照手册。
- 先建立 Provider-neutral 模型，再以两家当前官方合同作为反例和落地点；具体字段只服务于边界判断。
- Streaming、Error、Retry 都讲到能作工程决策的最小深度，但不吞掉 Article 05 Tool Use、Article 10/11 Runtime / Recovery、Article 20 Budget 或 Article 21 Trace。
- 本篇无 Required Lab。Fake Provider 只作为后续可执行验证的 `PROPOSAL / NOT_EXECUTED`，不生成 expected-as-observed 叙事。
- 所有 runtime 行为保持 `UNVERIFIED`；官方文档证明当前公开合同，不证明本 transaction 实际调用、事件顺序或重试次数。

## 3. Opening｜为什么“把 URL 换掉”之后，程序仍然不是同一个程序？

- Reader Question：两个 SDK 都提供 `GenerateAsync` 或 streaming helper，为什么 Provider migration 仍会改变上层正确性、诊断和预算判断？
- Section Goal：从一个最小失败场景立住问题空间，避免从产品 API 或统一接口代码开场。
- Core Thesis：endpoint、credential 与 model selector 只是迁移表面的一部分；请求结构、stream event vocabulary、terminal semantics、Usage dimensions、error taxonomy 与 SDK retry ownership 都可能上溢。
- Claim IDs：`04-C01`
- Evidence Scope：OpenAI Responses 与 Anthropic Messages 的当前 official docs / generated types / C# SDK docs，核对于 `2026-08-20`；只证明两家当前合同存在可点名差异。
- Opening Failure Scenario：
  1. 假想团队把 `ProviderAEndpoint` 改为 `ProviderBEndpoint`，继续沿用同一 message enum、同一 `done` 判定、同一 Usage total 与同一 429 retry handler。
  2. 第一类故障：system / instructions 与 message shape 不再等价；
  3. 第二类故障：consumer 把任意 stream end 或 content fragment 当作完整结果；
  4. 第三类故障：SDK 已 retry，上层又 retry，attempt 数被隐式放大；
  5. 第四类故障：Provider-specific incomplete / refusal / quota 被抹成 generic exception，无法决定下一动作。
- Counter-evidence / Guardrail：两家都提供 streaming、tool input、usage、errors 与 SDK retry，说明共同抽象确实存在；不得写成“Provider 永远不可替换”或“每一处差异都必须上溢到领域业务”。
- Wording Strength：使用“至少在本次核对的 OpenAI Responses 与 Anthropic Messages 当前合同中”；不使用“所有 Provider 都……”或“已实测迁移失败”。
- Boundary / Stop Line：不讲负载均衡、模型部署或供应商选型；不把假想失败场景写成真实事故。
- Bridge：问题不是要不要抽象，而是 Adapter 应该抽象到哪一层、又必须保留什么。

## 4. Section 1｜先列迁移面：哪些差异会穿过 SDK 上溢？

- Reader Question：面对一家新 Provider，Application 至少要重新核对哪些 contract surfaces？
- Section Goal：给出按责任面组织的迁移矩阵，替代按 Provider 产品顺序平铺文档。
- Core Thesis：稳定的是“需要核对哪些职责”，不稳定的是具体字段、事件名、stop reason、Usage 维度与默认 retry 值。
- Claim IDs：`04-C01`
- Evidence Scope：`S-01`—`S-13`；Provider / API / SDK language / retrieval date 不可省略。
- Migration Surface Table Plan：

| Surface | Reader Must Compare | Must Not Normalize Away |
|---|---|---|
| Request / instruction model | system / instructions / messages / content blocks 的位置、角色和生命周期 | Provider-specific placement 与 unsupported combination |
| Stream protocol | typed event vocabulary、content block/item lifecycle、unknown event policy | raw event type、sequence context、stream error |
| Terminal semantics | completed / incomplete / failed、stop reason、refusal、truncation | 原始 reason 与未知分支 |
| Usage | input/output/cache 等维度，incremental/cumulative/final-only 语义 | Provider scope、计数维度与累计方式 |
| Errors | network、timeout、rate limit、quota/action、5xx、stream-after-200 | status、Provider error type、request id、headers |
| SDK retry | eligible classes、内建 owner、额外 attempt、backoff surface | SDK language、version/main scope 与实际 attempt metadata |
| Capabilities / limits | structured output、tool streaming、modalities、limits | native / fallback / unsupported 差异 |

- Provider Example Policy：每个 surface 最多选一个 OpenAI / Anthropic 对照作为证据锚点，不分别开“OpenAI API”“Anthropic API”章节。
- Scoped Retry Callout：若正文保留精确数值，只能写为“截至 `2026-08-20`，OpenAI 官方 .NET SDK current main 文档写明对其列出的状态最多自动额外重试 3 次；Anthropic 官方 C# SDK current docs 写明对其列出的连接 / HTTP 类别默认重试 2 次”。紧接一句：这不是课程默认值，也不能外推到其他语言、版本、模型或 Provider。
- Counter-evidence / Guardrail：SDK helper 与 Gateway transform 可以减少应用样板；这反驳“所有字段必须直接暴露给领域层”，但不证明所有语义可无损压成一个字符串或枚举。
- Wording Strength：所有精确字段和数值带 Provider + API / SDK language + current docs/main + `2026-08-20` scope。
- Boundary / Stop Line：不维护完整 capability matrix，不比较价格、模型质量或 Provider 优劣。
- Bridge：迁移面明确后，建立一个不会把 Provider 差异、领域合同和运行时状态揉成一层的 Adapter 模型。

## 5. Section 2｜抽象模型：Adapter 归一化调用职责，不拥有领域真值

- Reader Question：Model Adapter 应该收口哪些变化，又有哪些变化必须继续向上可见？
- Section Goal：建立全文唯一的 provider-neutral responsibility chain，并明确 Adapter 的应 / 不应职责。
- Core Thesis：Adapter 把 provider-neutral request 映射到特定 Provider，并交付保真的 partial、terminal、usage、error 与 diagnostics；领域 DTO、领域验证、Agent loop、恢复与业务完成保持在上层。
- Claim IDs：`04-C02`（`PARTIAL`）
- Evidence Scope：事实前提 `04-C01`；课程链 `R-02`、`R-03`、`R-04`。这是课程 working boundary，不是官方或行业统一 Adapter 定义。
- Main Model：

```text
Domain Request / Task Policy
        ↓
Provider-neutral Model Request
        ↓
Model Adapter
        ↓
Provider SDK / HTTP / SSE
        ↓
Normalized partial events
  + provider terminal state
  + usage with provenance
  + raw diagnostics / unknowns
        ↓
Parse → Schema → DTO → Domain Validation       [Article 03]
        ↓
Final Structured Result
```

- Adapter Owns Table：
  - provider request mapping 与 SDK / HTTP boundary；
  - provider stream state-machine decoding；
  - stable error category + raw Provider evidence；
  - terminal / refusal / incomplete / usage 的保真映射；
  - retry ownership / attempt metadata 的暴露；
  - capability descriptor 的读取或提供（提案边界见 Section 7）。
- Adapter Must Not Swallow Table：
  - domain DTO、domain invariants 与 business success；
  -未经上层批准的 prompt rewrite、model switch、fallback 或无限 retry；
  - Agent step、tool dispatch、workflow checkpoint、recovery；
  - unknown event / reason、raw error、request id、headers 与 Usage dimensions；
  - Provider completion 到 task completion 的语义升级。
- Minimal Interface Sketch Plan：只放 10—15 行 provider-neutral 伪代码 / C#-like interface，展示 `Request`、`IAsyncEnumerable<ModelEvent>`、`Terminal`、`Usage`、`Diagnostics` 与 `CapabilityDescriptor` 的职责名字；不使用真实 SDK 类型，不宣称它是完整生产接口。
- Counter-evidence / Guardrail：现实 SDK 可能直接生成 typed object，Gateway 也可能做 request/response transform，多层也可部署在同一进程；职责可以组合，证据语义不能因此合并。
- Wording Strength：明确 `04-C02 = PARTIAL`；使用“本课程采用”“建议责任切分”，不用“Adapter 必须作为独立服务”或“行业标准定义”。
- Boundary / Stop Line：不设计完整 DI / factory / routing implementation；不让 Adapter 执行 domain validation 或 Agent loop。
- Bridge：Adapter 最容易误做的地方不是 request mapping，而是把 streaming fragment 提前升级为 final result。

## 6. Section 3｜Streaming：partial、terminal、final validation 是三个阶段

- Reader Question：UI 可以边到边显示文本时，程序为什么不能边到边反序列化并执行 Tool 参数？
- Section Goal：用一个三阶段时间线建立 streaming 的最小安全状态机。
- Core Thesis：`STREAM_PARTIAL` 只可用于展示、缓冲与观测；`PROVIDER_TERMINAL` 才提供终止语义；聚合候选还必须经过 Article 03 的 Parse / Schema / DTO / Domain，才能成为 `FINAL_VALIDATED_RESULT`。
- Claim IDs：`04-C03`
- Evidence Scope：`S-01`、`S-02a`—`S-02c`、`S-06`、`S-08`—`S-10`、`R-03`；官方合同证据，Provider Calls `NONE`，runtime sequence `UNVERIFIED`。
- Main Timeline：

```text
Provider stream
  ├─ text delta --------------------> display / buffer / observe
  ├─ tool-arguments delta ----------> buffer only
  ├─ usage update ------------------> scoped accumulator
  ├─ unknown event -----------------> preserve / diagnose
  └─ terminal or stream error ------> classify terminal state
                                         ↓
                              aggregate candidate if allowed
                                         ↓
                              Parse → Schema → DTO → Domain
                                         ↓
                              Final Validated Result
```

- Three-phase Table：

| Phase | Allowed Action | Explicitly Not Proven |
|---|---|---|
| `STREAM_PARTIAL` | 展示、缓冲、进度观测、保存 raw provenance | JSON 完整、DTO 可构造、Tool 可执行、任务完成 |
| `PROVIDER_TERMINAL` | 保存 stop/incomplete/refusal/error、final usage、聚合候选 | schema/domain 有效、Agent 已停止、外部副作用成功 |
| `FINAL_VALIDATED_RESULT` | 交付通过 Article 03 全链的 typed result | Tool 已授权/执行、workflow 已恢复、业务目标完成 |

- Tool Arguments Guard：OpenAI function argument delta 与 Anthropic `partial_json` 都只作为 Provider-scoped fragment evidence；正文必须写“累积后再 parse / validate”，不得把 fragment 送入 DTO、Tool dispatch 或 execute。
- Explicit Concept Guard：`Streaming event != Agent event`。前者描述单次 Provider 调用内部的交付 / 生成进度；后者涉及 step、tool、state、stop 等运行时状态，正式展开留给 Article 08/10。
- Counter-evidence / Guardrail：官方 SDK accumulator/helper 可降低聚合负担，简单文本 UI 也可即时显示；helper 不会把早期 fragment 变成 verified result。
- Wording Strength：使用“当前官方 contract 定义”“应按其状态机处理”；不写“本次观测到事件必定按该顺序到达”。
- Boundary / Stop Line：不展开 backpressure、SSE reconnect、cancellation 或 Agent event schema；不实现 Tool Use。
- Bridge：stream 可以以 terminal、incomplete、refusal 或 stream error 收尾；下一步先分类错误，再讨论是否 retry。

## 7. Section 4｜Error：先判断失败发生在哪一层，再决定动作

- Reader Question：为什么 `HTTP 429`、`HTTP 200 + stream error`、refusal、max tokens 与 schema failure 不能都变成 `ModelCallFailedException`？
- Section Goal：给 retry policy 一个可靠输入，防止状态码或异常基类直接决定动作。
- Core Thesis：transport / Provider API、generation terminal、local validation 与 domain failure 是不同证据层；同一个状态码也可能承载不同原因，必须保留 Provider error body、type、headers、request id 与 unknown branch。
- Claim IDs：`04-C04`
- Evidence Scope：`S-03`、`S-06`、`S-08`—`S-13`、`R-03`；分类原则可确认，具体 code/type 仍是 Provider / version scoped。
- Classification Table Plan：

| Category | Example Signal | First Owner to Interpret | Retry Decision Here? |
|---|---|---|---|
| network / connection | SDK transport error | transport owner | 只形成候选，需 request state / replay safety |
| timeout / unknown outcome | connect/read/overall timeout | transport owner + upper policy | 不能假设服务端未处理 |
| rate limit | Provider-classified 429 + headers | Provider error mapper | 可按 guidance 进入 bounded candidate |
| quota / billing / action required | Provider error code / message | operator / business policy | 不按临时限流盲重试 |
| transient Provider error | Provider-scoped 5xx / overload type | transport policy | 只在 eligible + safe + budget 下 |
| stream error after success status | typed stream error event | Adapter decoder | 取决于已消费状态与 replay safety |
| refusal | output / terminal semantics | task policy | 不是 transport retry |
| truncation / incomplete | terminal status / reason | task policy | 可能新开 semantic attempt |
| parse / schema / DTO / domain | Article 03 first failure | structured-output / domain layer | 不重放同一 transport 伪装修复 |

- 429 Counterexample：同一个 HTTP 429 不一律意味着“等待后重试”；必须读取当前 Provider error category 与 headers，区分临时 rate limit 和 quota / action 类问题。
- Stream Counterexample：Anthropic 当前文档允许成功 HTTP 后的 SSE error；这只证明 contract 存在该分支，不证明本 transaction 实测到它。
- Counter-evidence / Guardrail：统一异常基类能简化普通调用代码；它可作为入口，不足以证明 retry semantics 相同。并非所有 4xx 永不 retry，也并非所有 5xx 必须 retry。
- Wording Strength：使用“先分类”“成为 retry candidate”；不写“某状态码固定采取唯一动作”。
- Boundary / Stop Line：不建立 Article 21 的完整 failure taxonomy / Trace schema；只保留本篇决定 retry 所需最小分类。
- Bridge：错误分类只是输入；下一步还要区分到底是重放同一请求、改变任务后新开 attempt，还是恢复整个 workflow。

## 8. Section 5｜Retry：transport retry、semantic retry 与 recovery 不可互换

- Reader Question：一次模型调用失败后，“再试一次”究竟重放了什么、改变了什么，又由谁负责停止？
- Section Goal：把 retry 从一个布尔开关拆成动作类型和安全 Gate。
- Core Thesis：transport retry 重放同一逻辑 Provider 请求；semantic/business retry 是新的业务 attempt；recovery 从持久化 workflow state 恢复。三者可由同一 orchestrator 承载，但证据语义、预算和停止条件不同。
- Claim IDs：`04-C05`（`PARTIAL`）、`04-C06`（`PARTIAL`）
- Evidence Scope：`S-03`—`S-13`、`S-16`、`R-03`、`R-04`；Provider docs 未统一采用课程三分法，无跨 Provider exactly-once 证据。
- Action Comparison Table：

| Action | Repeats / Changes | Candidate Trigger | Must Not Hide |
|---|---|---|---|
| Transport retry | 重放同一逻辑 Provider request | eligible network / rate / transient error | SDK/Gateway 已 retry、unknown request state、副作用风险 |
| Semantic / business retry | 创建新 attempt，可能改 prompt/input/schema/model/budget | refusal、truncation、parse/schema/domain failure 且 policy 允许 | 它不是底层自动 retry，也不是旧 attempt 的继续成功 |
| Recovery | 从 durable state 核对 completed step / side effect 后继续或补偿 | crash、restart、long-running interruption、unknown side-effect state | 不能只靠重新请求模型恢复完整 workflow |

- Transport Retry Gate：正文使用顺序化检查表，而不是固定次数：
  1. eligible category 已按 Provider / SDK 当前合同确认；
  2. SDK、Gateway、Adapter / Application 中存在唯一 retry owner；
  3. request state 已知，未把 terminal result、已接受 tool call 或 unknown outcome 当作“未发生”；
  4. replay safety / idempotency 已由操作语义或可确认状态支持；
  5. 优先服从 Provider `Retry-After` / guidance，否则才用 bounded exponential backoff + jitter；
  6. attempt、elapsed time、token/cost 与队列等待预算有界；
  7. 不可重试类别、budget/deadline、unknown state 或上限命中时明确停止 / 升级。
- Nested Retry Example：只画 `SDK × Gateway × App` attempt multiplication 的概念算式，不代入某个“正确总次数”；重点是唯一 owner 与可观察 attempt metadata。
- Exactly-once Guard：明确“本次证据没有建立 OpenAI / Anthropic create API 的跨 Provider exactly-once 保证”；timeout 后重放不是默认安全。
- Counter-evidence / Guardrail：官方 SDK 存在 bounded automatic retry，反驳“所有生成请求都绝不能自动 retry”；这也不等于 eligible request 在任意上下文都安全。
- Wording Strength：`04-C05/C06` 始终标 `PARTIAL / course working model`；使用“候选”“Gate”“由策略决定”，不用“429 一律 retry”或“semantic retry 会修复”。
- Boundary / Stop Line：只点到 budget dimensions，不展开 Article 20；只定义 recovery stop line，不展开 Article 11 checkpoint / compensation；不设计 fallback router。
- Bridge：retry owner 可能在 SDK、Adapter 或集中 Gateway；组件位置不能替代责任分类。

## 9. Section 6｜工程边界：Adapter、Gateway 与 Agent Runtime 各自回答什么问题？

- Reader Question：既然 Gateway 也能做 routing、retry、fallback 和 telemetry，Application 内为什么还需要 Adapter？Gateway 是否已经等于 Agent Runtime？
- Section Goal：用责任、作用域与状态生命周期切开三个概念，同时承认现实产品可组合职责。
- Core Thesis：Adapter 负责 Provider contract translation；Gateway 负责跨调用 / 租户的集中流量治理；Agent Runtime 负责 goal / step / tool / state / stop / recovery。部署可以合并，证明责任不能靠产品名称互相替代。
- Claim IDs：`04-C07`（`PARTIAL`），并回收 `04-C02`（`PARTIAL`）
- Evidence Scope：`S-14`、`S-15`、`R-04`；两项产品实例证明部分 Gateway capability 组合存在，不证明行业唯一 Gateway 定义。
- Responsibility Table：

| Component | Course Working Responsibility | State / Scope | It Does Not Prove |
|---|---|---|---|
| Model Adapter | request translation、stream/terminal/error/usage decoding、capability/raw diagnostics | 单个 Provider integration / 一次调用短期状态 | domain truth、Gateway、Agent loop |
| LLM Gateway | credential/routing/rate/quota/retry/fallback policy、audit/telemetry 等集中 traffic concerns | 跨应用、Provider、租户或流量策略 | 完整 Agent step/state/stop/recovery |
| Agent Runtime | model call、tool dispatch、loop、state、stop、checkpoint/recovery | 长于单次 model call 的 execution state | Gateway traffic governance 本身 |

- Product Reality Callout：Cloudflare 与 Azure 的能力集合不同，Azure 文档还可能覆盖 tools/agents 相关面；所以正文写“本课程按责任切分”，不写“凡名为 Gateway 的产品都只做网络代理”。
- Deployment Guard：Adapter 可在应用进程、服务模块或 Gateway 内；Gateway 也可与 Runtime 同产品交付。`same process/product != same responsibility/evidence`。
- Counter-evidence / Guardrail：现实产品组合职责反驳过度洁癖式分层；组合部署不反驳对 request translation、traffic governance 与 execution state 分别验证。
- Wording Strength：`04-C07 = PARTIAL`；不使用“标准架构”“唯一正确定义”。
- Boundary / Stop Line：不搭建 Gateway，不讲 LB、模型部署、credential implementation、multi-region 或 vendor comparison；不提前讲 Article 10/11 Runtime mechanics。
- Bridge：责任切开后，最后还要让路由者知道一个 Provider 到底能否满足当前任务，而不是只返回 `supports_llm=true`。

## 10. Section 7｜Capability Descriptor：把不可归一化差异变成显式决策输入

- Reader Question：如果 Adapter 不应把 Provider 差异全部抹掉，上层怎样在调用前知道 native support、fallback 与 unsupported？
- Section Goal：把已确认的迁移差异转成一个可审查的设计提案，并提供验证清单；不假装已经实现统一 negotiation 标准。
- Core Thesis：一个布尔 `SupportsModel` 不足以表达 structured output、streaming、terminal、usage、retry ownership 与 limits；课程建议用带 Provider / API / model / version scope 的 descriptor，输出 `native / explicit fallback / unsupported`，缺失能力时 fail closed。
- Claim IDs：`04-C08`（`PROPOSAL / NOT_EXECUTED`）
- Evidence Scope：差异前提 `04-C01`；`S-07`、`S-08`、`R-02`—`R-04`。该 Evidence 只支持“差异需要可见”，不证明存在统一 descriptor schema 或已实现 negotiation。
- Descriptor Dimensions Plan：
  - `provider / api / model / version_scope`；
  - instruction / message model；
  - structured output：`native strict / JSON-only / prompt fallback / unsupported`；
  - text / tool-argument streaming 与 fragment lifecycle；
  - Usage delivery semantics 与 dimensions；
  - terminal / refusal / incomplete semantics；
  - retry owner、eligible classes、attempt metadata 与 guidance support；
  - context / output limits 与 modalities；
  - unknown event / reason preservation policy。
- Negotiation Decision Table：

| Result | Meaning | Required Upper-layer Action |
|---|---|---|
| `NATIVE` | 当前 scoped contract 原生满足需求 | 保存 scope 与 diagnostics 后调用 |
| `EXPLICIT_FALLBACK` | 只能用较弱、语义不同的替代路径 | 上层显式批准，不与 native guarantee 等价 |
| `UNSUPPORTED` | 当前 scope 无法满足必需能力 | fail closed、换候选或返回不可执行 |

- Minimal Descriptor Example：只展示 `PROPOSAL` 伪结构，不使用某 SDK 当前类型；所有字段都标为课程建议，而非 interoperable schema。
- Fake Provider Verification Proposal：
  - Classification：`PROPOSAL / NOT_EXECUTED`；
  - Inputs：增量文本、tool argument fragments、429(rate-limit vs quota)、terminal truncation、unknown event；
  - Intended Checks：fragment 不进 DTO/execute、terminal 后才走 Article 03 validation、429 分类不同、retry owner 唯一、unsupported fail closed；
  - Current Observation：`NONE`；Provider call=`NONE`；不可写 PASS、事件顺序实测或 retry behavior 已验证。
- Counter-evidence / Guardrail：简单单 Provider 应用可通过静态配置固定能力，现有 SDK / Gateway 也可能已有 discovery；这说明 descriptor 不是所有应用都必须动态实现，更不证明本课程提案优于现有产品。
- Wording Strength：所有设计句前后保留 `建议 / proposal / not executed`；不使用“系统已经支持”“已协商成功”。
- Boundary / Stop Line：不实现 router、fallback chain 或 Gateway；不把 `native/fallback/unsupported` 写成行业标准枚举。
- Bridge：用 integration verification checklist 收口：真正可替换的是经过显式能力与失败边界核对的 contract，不是一段隐藏差异的 wrapper。

## 11. Closing｜怎样审查一个“可切换 Provider”的模型层？

- Reader Question：读者离开本篇后，面对一个 `IModelClient` 或 Gateway 方案，应该按什么顺序判断它是否真正可替换、可观察？
- Section Goal：把全文压缩成一张 verification checklist，并桥接 Article 05。
- Core Thesis：一个可替换模型层必须同时能说明 request mapping、stream phases、terminal semantics、error class、retry owner、usage provenance 与 capability gaps；只暴露 `GenerateAsync(string)` 不足以形成工程闭环。
- Claim IDs：`04-C01`—`04-C08`
- Evidence Scope：沿用各 Claim 的 `CONFIRMED / PARTIAL / PROPOSAL` 上限；Provider Calls `NONE`、Runtime `UNVERIFIED`。
- Verification Checklist：
  1. request / instruction / messages 怎样映射到当前 Provider / API / model / version？
  2. partial、terminal、final validated result 是否被不同类型 / 状态表达？
  3. tool argument fragments 是否只缓冲，完成后才进入 Article 03 validation？
  4. unknown event / stop reason / Usage dimension / raw diagnostics 是否被保留？
  5. error 是 network、timeout、rate、quota/action、transient、refusal、truncation、parse/schema/domain 中哪一层？
  6. transport retry owner 是否唯一，request state、replay safety、budget 与 stop 是否可审查？
  7. semantic retry 与 recovery 是否另建 attempt / state，而非冒充 transport retry？
  8. Gateway traffic policy 与 Agent Runtime execution state 是否分别有证据？
  9. capability 是 native、显式 fallback 还是 unsupported？scope 与版本是否随结果返回？
  10. 当前结论来自 official docs、runtime observation 还是 design proposal？
- Article 03 Backward Bridge：Article 03 负责 Parse / Schema / DTO / Domain；本篇负责在 candidate 进入该链前保留 Provider stream / terminal / error evidence，并决定是否允许形成候选。
- Article 05 Forward Boundary：下一篇才讨论模型怎样表达“我要行动”的 Function Calling / Tool Use contract；本篇只保证 tool argument fragment 不能越过 validation 直接执行。
- Final Sentence：`可替换性不是把 Provider 名字藏起来，而是让差异有明确归属、失败有明确语义、能力缺口有明确出口。`

## 12. Figure / Table / Example Responsibilities

| ID | Artifact | Teaching Responsibility | Must Not Imply |
|---|---|---|---|
| Figure 1 | `Provider Migration Surface` | 从换 URL 失败场景展示 request/stream/terminal/usage/error/retry 上溢 | 所有 Provider 相同、已执行真实迁移 |
| Figure 2 | `Provider-neutral Adapter Responsibility Chain` | 展示 Adapter 到 Article 03 validation 的责任边界 | 行业统一架构、Adapter 拥有 domain truth |
| Figure 3 | `Partial → Terminal → Final Validation Timeline` | 展示 fragment buffer、terminal classification 与 validation 顺序 | stream fragment 可执行、Provider terminal 等于业务完成 |
| Table 1 | `Migration Surface Matrix` | 按 contract surface 对照两家当前 evidence | Provider 产品优劣榜、完整字段百科 |
| Table 2 | `Failure Layer Classification` | 为 retry policy 提供 layer / signal / owner | 每类失败只有唯一动作 |
| Table 3 | `Transport Retry vs Semantic Retry vs Recovery` | 区分重复对象、策略和 stop line | Provider 官方统一采用课程术语 |
| Table 4 | `Adapter vs Gateway vs Runtime` | 按责任 / 状态范围切分 | 必须分进程、Gateway 产品绝无 Agent 能力 |
| Table 5 | `Capability Decision` | 表达 native / explicit fallback / unsupported 提案 | 已存在统一标准、已执行 negotiation |
| Example 1 | `Change URL Failure` | 立住 problem space | 真实事故或 Provider runtime observation |
| Pseudocode 1 | `IModelAdapter Responsibility Sketch` | 落地 provider-neutral contract | 可直接生产使用的完整 SDK abstraction |
| Proposal 1 | `Fake Provider Verification` | 定义未来可执行检查项 | Lab 已运行、结果 PASS |

Asset Policy：本轮不创建 `assets/`。Draft 优先使用 Markdown 文本图、表和短伪代码；不生成 Provider 品牌图、性能图或未执行实验结果图。

## 13. Failure-path Coverage Plan

| Failure Path | Main Placement | Required Interpretation | Forbidden Upgrade |
|---|---|---|---|
| Request / message mapping mismatch | Opening、Section 1—2 | Provider switch 是 contract migration | 已实测某 Provider migration 失败 |
| Tool argument fragment | Section 3 | buffer only；terminal 后才 parse / validate | DTO 已构造、Tool 可执行 |
| Unknown stream event / reason | Section 1、3 | preserve raw + diagnose | 静默归一成 success / generic done |
| HTTP 429 | Section 4—5 | 区分 rate limit 与 quota/action；进入 gated candidate | 429 一律自动 retry |
| Timeout / unknown state | Section 4—5 | request state 与 replay safety 未知时停止/升级 | 服务端一定未处理、跨 Provider exactly-once |
| HTTP success + stream error | Section 3—4 | terminal success 不能只看初始 HTTP status | 本 transaction 已 runtime 观测 |
| Refusal / incomplete | Section 3—5 | generation terminal / semantic policy | transport retry 必然合适 |
| Parse / schema / domain failure | Section 3—5 | Article 03 first-failure layer；semantic attempt 候选 | 重放同一 transport 即可修复 |
| Nested SDK / Gateway / app retry | Section 5 | 唯一 owner + attempt metadata | 固定跨 Provider最佳次数 |
| Gateway combined product | Section 6 | 产品可组合职责，分别验证 evidence | 行业唯一 Gateway 定义 |
| Unsupported capability | Section 7 | fail closed 或显式 fallback | fallback 等价 native guarantee |

Coverage Rule：Draft 不能只展示 happy path。至少保留 fragment、429 category split、timeout unknown state、stream-after-success error 与 nested retry 五个失败/风险路径；均不得升级为 runtime observation。

## 14. Learning Check Plan

1. 为什么把 endpoint、API key 与 model selector 换掉，不足以证明 Provider migration 完成？
   - Reference Judgment：还需核对 request/message、stream、terminal、usage、error、SDK retry 与 capabilities；本篇证据只覆盖两家当前合同。
2. Model Adapter 哪些职责应该收口，哪些信息必须向上保真暴露？
   - Reference Judgment：收口 request / stream / error / usage translation；保留 raw diagnostics、terminal/capability scope，不吞 domain DTO、Agent state 或 recovery。
3. UI 已显示完整一句话，为什么仍不能把 tool argument fragment 送入 DTO 或 Tool execute？
   - Reference Judgment：partial 不证明 block/item/stream terminal，更不证明 Parse / Schema / DTO / Domain 通过。
4. 为什么 `HTTP 429` 不能自动映射为“sleep 后重试”？
   - Reference Judgment：同状态可能有 rate limit 与 quota/action 等不同 Provider cause；还需 retry owner、request state、replay safety、budget 与 stop gate。
5. transport retry、semantic/business retry 与 recovery 分别重复或改变了什么？
   - Reference Judgment：同 request 重放、新业务 attempt、durable workflow state 恢复；课程分类为 `PARTIAL` working model。
6. Gateway 已有 routing、retry 和 telemetry，为什么仍不能仅据此宣布 Agent Runtime 完整？
   - Reference Judgment：traffic governance 不证明 goal/step/tool/state/stop/checkpoint/recovery；产品可组合职责，但证据分开。
7. Capability descriptor 为什么应返回 `native / explicit fallback / unsupported`，又为什么本篇不能宣称它已经工作？
   - Reference Judgment：差异需要显式决策输入；`04-C08` 只是 `PROPOSAL / NOT_EXECUTED`，无统一 schema 或 runtime evidence。
8. 截至 `2026-08-20` 的 OpenAI .NET / Anthropic C# retry 默认值，为什么不能变成课程默认次数？
   - Reference Judgment：它们绑定不同 Provider、SDK language、current docs/main、eligible categories 与版本；其他语言 / 版本 / Provider 未被证明。

## 15. Claim-to-Section Coverage Matrix

| Claim ID | Status | Main Placement | Evidence Scope | Counter-evidence / Coverage Guard |
|---|---|---|---|---|
| `04-C01` | `CONFIRMED` | Opening、Section 1、Closing | `S-01`—`S-13`；OpenAI Responses + Anthropic Messages / official SDK docs，`2026-08-20` | 两家共同能力支持抽象；只证明当前两家差异会上溢，不推出所有 Provider 或 runtime observation |
| `04-C02` | `PARTIAL` | Section 2、6 | `04-C01`、`R-02`—`R-04` | SDK/Gateway 可组合 transform，多层可同进程；课程 responsibility boundary 不是行业标准 |
| `04-C03` | `CONFIRMED` | Section 3、Closing | `S-01`、`S-02a`—`S-02c`、`S-06`、`S-08`—`S-10`、`R-03` | SDK helper 可聚合但不升级 fragment；Provider Calls `NONE`、runtime sequence `UNVERIFIED` |
| `04-C04` | `CONFIRMED` | Section 4 | `S-03`、`S-06`、`S-08`—`S-13`、`R-03` | 统一异常可简化入口但不足以决定 retry；不写所有 4xx/5xx 单一规则 |
| `04-C05` | `PARTIAL` | Section 5 | `S-03`—`S-05`、`S-11`—`S-13`、`S-16` | SDK automatic retry 反驳绝对禁止；无跨 Provider exactly-once / 固定次数，精确值只限 SDK language/date scope |
| `04-C06` | `PARTIAL` | Section 5 | `S-03`—`S-13`、`R-03`、`R-04` | 现实框架命名可合并；课程按重复对象 / state boundary 分类，不称 Provider 官方统一术语 |
| `04-C07` | `PARTIAL` | Section 6 | `S-14`、`S-15`、`R-04` | 两家 Gateway 产品能力不同且可含 tools/agents；无行业唯一 Gateway 定义，组合部署不合并证据 |
| `04-C08` | `PROPOSAL` | Section 7、Closing | `04-C01`、`S-07`、`S-08`、`R-02`—`R-04` | 现有 SDK/Gateway 可有 discovery，单 Provider 可静态配置；`NOT_EXECUTED`，不写统一 schema / 已实现 negotiation |

Coverage Result：`8 / 8 Claims mapped`；状态保持 `3 CONFIRMED / 4 PARTIAL / 1 PROPOSAL / 0 BLOCKED`；`new core facts = 0`；`RETURN_TO_RESEARCH = NONE`。

## 16. Job Competency Mapping

| Competency | Article Evidence of Learning | Assessment Surface |
|---|---|---|
| Cross-provider contract reading | 能按 request/stream/terminal/usage/error/retry/capability surface 核对 Provider，而非抄字段 | Section 1 + Learning Check 1、8 |
| Interface / seam design | 能定义 Adapter 应 / 不应职责并保留 raw diagnostics 与 capability scope | Section 2 + Learning Check 2 |
| Streaming state modeling | 能区分 partial、terminal、final validation，并阻止 fragment 进入 DTO / execute | Section 3 + Learning Check 3 |
| Failure classification | 能从 network、timeout、rate、quota、terminal、validation 层定位 owner | Section 4 + Learning Check 4 |
| Retry safety judgment | 能检查唯一 owner、request state、replay safety、idempotency、budget 与 stop | Section 5 + Learning Check 4—5 |
| Architecture boundary judgment | 能区分 Adapter、Gateway 与 Runtime，同时接受组合部署 | Section 6 + Learning Check 6 |
| Capability / fallback design | 能以 native/fallback/unsupported 设计 fail-closed 决策输入，并标 proposal 边界 | Section 7 + Learning Check 7 |
| Evidence discipline | 能区分 official-doc contract、runtime observation 与 design proposal | 全文 guardrails + Closing checklist 10 |

## 17. Adjacent Article Stop Lines

| Adjacent / Future Article | Article 04 May Introduce | Article 04 Must Stop Before |
|---|---|---|
| Article 03｜Structured Output | terminal 后候选继续进入 Parse / Schema / DTO / Domain | 重讲 JSON Schema / Lab 01、把 validation failure 当 transport failure |
| Article 05｜Function Calling / Tool Use | tool argument stream 是 fragment，完整候选也需 validation | tool selection、function-calling wire contract、action intent 与 execute |
| Article 08｜Agent Loop | `Streaming event != Agent event`，Runtime 有更高层状态 | Turn / Step / Decide / Act / Observe / Stop 机制 |
| Article 10—11｜Workflow / Long-running | recovery 不等于 retry，Runtime 拥有长于一次调用的状态 | state machine、checkpoint、resume、cancellation、compensation |
| Article 20｜Budget | retry 需 attempt / time / token / cost / queue bounds | 完整 Budget policy、accounting 与 allocation |
| Article 21｜Trace / Replay | attempt、request id、raw diagnostics 是可观测输入 | 完整 Trace schema、Replay 与 Failure Taxonomy |
| Article 31、33｜DSH Provider / Step | capability seam 与 step model call 是后续源码验证点 | 提前推断 DSH source/runtime implementation |
| BuildPilot | `IModelAdapter`、Usage provenance、routing input 的设计影响 | 实现 Gateway / Runtime 或宣称 BuildPilot 已存在 |

Cross-boundary Rule：若 Draft 需要 Tool execution、Agent step/state、workflow checkpoint、完整 budget/trace、DSH runtime 或 Gateway deployment 才能支撑核心结论，必须删除越界段；若产生新的 Provider 行为事实需求，返回 `RETURN_TO_RESEARCH`。

## 18. Publication Link Plan

- Published Target：`content/ai-empowerment/agent-engineering-04-model-adapter-llm-gateway.md`。
- Frontmatter Plan：`series_order: 50`、`weight: 3050`、`series: "Agent Engineering"`、`primary_series: "agent-engineering"`；Publisher 负责最终校验，本 Outline 不创建 Published Content。
- Backward Link：正文首部链接已发布 Article 03：
  - `{{< relref "ai-empowerment/agent-engineering-03-structured-output-machine-contract.md" >}}`
- Series Link：如正文需要课程地图，链接已发布 `agent-engineering-series-index.md`；不重复 Article 01—02 的所有前置链接。
- Forward Link：Article 05 当前尚未发布，正文只用无链接的课程桥接句，不创建会导致 `REF_NOT_FOUND` 的 `relref`；待 Article 05 发布 transaction 再由其建立 backward link，是否回补 Article 04 由对应 Publisher / Master 决定。
- External Reference Plan：只在 Draft 参考资料区链接 Evidence Register 已核对的 official / primary sources；正文内高风险 Provider 数值就近说明 scope，不堆砌产品链接。
- Lab / Asset Link：本篇无 Required Lab、无 executed Fake Provider、无 assets；不得创建 Lab result link。

## 19. Evidence Omission List

- 不新增 Provider call、SSE observation、SDK attempt log、延迟、成本、成功率或运行时顺序结论。
- 不把 OpenAI / Anthropic 当前字段、stop reason、event type 或 retry default 写成跨 Provider标准。
- 不省略 OpenAI `.NET` 与 Anthropic `C#` 的 SDK language、current main/docs 与 `2026-08-20` scope；不把 `3 / 2` 写成课程 retry policy。
- 不宣称 429 一律 retry，不宣称所有 4xx 永不 retry或所有 5xx 必须 retry。
- 不宣称 timeout 后服务端未处理，不宣称 create API 有跨 Provider exactly-once / idempotency guarantee。
- 不把 stream fragment 反序列化为 final DTO，不让 fragment 进入 Tool dispatch / execute。
- 不把 Provider terminal / completion 写成 domain success、Agent stop 或 side effect success。
- 不把 SDK helper、Gateway transform 或 typed response 写成领域事实已验证。
- 不把 Adapter / Gateway / Runtime 写成行业唯一术语或强制进程部署。
- 不把 capability descriptor、`native / fallback / unsupported` 或 Fake Provider 写成已实现 / 已执行。
- 不展开 Tool Use、Agent Loop、Workflow Recovery、Budget、Trace、DSH runtime 或 BuildPilot implementation。

## 20. Outline Gate Checklist

- [x] H1 与 canonical 标题精确一致
- [x] Article Type 明确为原理篇 / Normal Article，第一屏从“换 URL”的工程失败开始，不以 Provider API 列表开场
- [x] Teaching Spine 遵循 Problem Space → Abstract Model → Concrete Mechanism → Engineering Judgment / Boundary → Verification
- [x] 每个主体 Section 都有 Reader Question、Core Thesis、Claim IDs、Evidence Scope、Counter-evidence / Guardrail 与 Stop Line
- [x] `8 / 8` Claims 显式映射；`04-C02/C05/C06/C07` 保持 `PARTIAL`；`04-C08` 保持 `PROPOSAL / NOT_EXECUTED`
- [x] Provider Calls=`NONE`、Runtime Evidence=`UNVERIFIED` 未被升级
- [x] `partial != terminal != final validated result`、`Streaming event != Agent event`、tool fragment 不进 DTO / execute 已冻结
- [x] final result 仍走 Article 03 Parse / Schema / DTO / Domain gate
- [x] 429 非一律 retry；transport retry 的唯一 owner、request state、replay safety/idempotency、budget 与 stop 已覆盖
- [x] 无跨 Provider exactly-once 结论；SDK 精确默认值保持 Provider + language + current docs/main + date scope
- [x] Adapter ≠ Gateway、Gateway ≠ Agent Runtime，同时明确产品可组合职责、无行业唯一 Gateway 定义
- [x] Fake Provider 仅作 `PROPOSAL / NOT_EXECUTED`，不宣称实验
- [x] Figures / Tables / Examples 均写明 `Must Not Imply`
- [x] Learning Check 覆盖 Reader Promise；Job Competency mapping 有 assessment surface
- [x] Publication link plan 避免 Article 05 未发布时的 broken `relref`
- [x] 本 Outline 没有引入新核心事实；`RETURN_TO_RESEARCH = NONE`

Recommendation：`PASS`。建议 Master 将 Article 04 推进为 `OUTLINE_READY`；下一允许动作仅为 Author 依据本 Outline 与批准 Evidence 创建 `draft.md`。Author 不更新 README、`status.md`、`course-run-state.md`、canonical、Research、Evidence、Review 或 Published Content。
