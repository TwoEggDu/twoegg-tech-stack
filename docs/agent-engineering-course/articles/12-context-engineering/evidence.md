# Article 12 Evidence｜Context Engineering

## Status

- Gate：`EVIDENCE_GATE`
- Claim Register：`COMPLETE`
- Traceability：`9 / 9 READY`
- Core Blocked：`0`
- Counter-evidence search：`COMPLETE`
- Current primary-source scope：`2026-08-21 / OpenAI + Anthropic official current hosted docs`
- Evidence Gate recommendation：`PASS`

> `CONFIRMED`仅确认注明的产品 / repository scope；`PARTIAL`必须使用收窄措辞；`PROPOSAL`是课程设计。没有来源支持行业统一Context schema，也没有来源支持完整重建Provider内部上下文。

## Claim Register

| ID | Claim | Status | Sources | Traceability / wording ceiling |
|---|---|---|---|---|
| `C01` | Context是一次Model Step实际可见的有效信息集合，不等于单一Prompt文本；application-visible assembly是可构建的Context Snapshot。 | `CONFIRMED / CITED-PRODUCTS-SCOPED + COURSE DEFINITION` | `S01 / S04 / S05 / R02` | 产品确认多类输入；effective Context与Context Snapshot的区分是课程工作定义，不写行业定义。 |
| `C02` | instruction、goal、working state、history、capability、external fact六类Contributor可用于课程分析。 | `PROPOSAL / COURSE TAXONOMY` | `R02 / R06 / R08 / R09 / R10` | 六类覆盖依赖文章对象；不写Provider wire schema或完备分类。 |
| `C03` | 每个Step应依据当前State、Scope与Budget执行Selection / Ordering，而非盲目复用全量历史。 | `PARTIAL / PRODUCT-CONSTRAINT + COURSE DESIGN` | `S01 / S03 / S04 / S05` | 可说“课程建议 / 产品约束使选择必要”；不说所有系统必须逐Step重建或该算法最优。 |
| `C04` | Tool Schema与Tool Result会进入或影响请求Context，并消耗有限输入 / 窗口预算。 | `CONFIRMED / ANTHROPIC-CURRENT-DOCS-SCOPED + OPENAI-SURFACE` | `S01 / S04 / S05 / S06 / R06` | Anthropic明确计入；OpenAI只用于请求surface / usage，不外推统一计费公式。 |
| `C05` | Context Budget约束可携带输入、工具、结果和输出空间，并影响成本；质量影响必须按workload验证。 | `PARTIAL / PRODUCT-DOC-SCOPED` | `S01 / S04 / S05 / S06` | 可说容量与成本tradeoff；不宣称“越长越差”或通用质量曲线。 |
| `C06` | Stable / Dynamic Context是课程用于管理来源、版本、生命周期与scope的分类。 | `PROPOSAL / SOURCE-INFORMED COURSE TAXONOMY` | `S01 / S03 / R02 / R08 / R10` | 可建议稳定规则与动态State分开；不绑定system/user role或永久生命周期。 |
| `C07` | Context Snapshot / Receipt应记录application-visible选择、顺序、裁剪、冲突、未知和可见request identity。 | `PROPOSAL / COURSE OBSERVABILITY DESIGN` | `S02 / S06 / R06 / R10` | Trace字段提供实现构件；Receipt仅描述、审计和比较Snapshot，不写产品保证或行业标准。 |
| `C08` | 只保存最终Prompt文本不足以解释tools / results / state / omissions / provider-managed additions的provenance。 | `CONFIRMED / COUNTER-EVIDENCE-SCOPED` | `S01 / S02 / S05 / S06 / R02 / R10` | 可说“final Prompt alone不足”；不说现有Receipt能恢复全部有效上下文。 |
| `C09` | Context Receipt可作为课程可观测性合同，但不等于Session、Memory、Checkpoint或Provider内部完整上下文。 | `PROPOSAL / COURSE CONTRACT + CONFIRMED LIMITATION` | `S02 / S03 / S05 / R11` | Receipt只描述、审计和比较application-visible Context Snapshot；禁止把它写成effective Context复现保证。 |

## Evidence Cards

### `C01` — Context is broader than one Prompt string

- Status：`CONFIRMED / CITED-PRODUCTS-SCOPED + COURSE DEFINITION`
- Source / locator：`S01` OpenAI Responses `input / instructions / conversation / tools / truncation / usage`；`S04` Anthropic Context Windows standard behavior；`S05` tool schemas / results and managed prompt；`R02` Prompt Inputs boundary。
- Retrieved / product scope：`2026-08-21`；OpenAI Responses current reference、Anthropic current Claude docs；model / SDK version未固定。
- Proves：引用产品的一次调用可包含instructions / system、messages / input、tools、prior items / results与request controls；单一最终Prompt文本不能代表这些全部组件。
- Does not prove：`Context`是行业统一术语；所有Provider使用相同字段、role、优先级或装配顺序。
- Limitations / counter-evidence：Anthropic自动加入tool-use system prompt；实际有效输入可包含application未提供的managed component。
- Allowed article wording：`本课程把某个Step实际可见的有效信息集合称为Context；应用可构建的是application-visible Context Snapshot，Prompt只是其中一个Contributor。`

### `C02` — Six Contributor classes

- Status：`PROPOSAL / COURSE TAXONOMY`
- Source / locator：`R02` task instruction / current goal / inputs；`R06` ToolDefinition / Result；`R08` Working State / Observation；`R09` Plan / History；`R10` Runtime State / Trace。
- Retrieved / scope：repository current main，read `2026-08-21`；课程对象边界。
- Proves：六类能覆盖本文Investigation Step需要追踪的已发布课程对象，并保持各自authority。
- Does not prove：分类完备、互斥、跨行业通用；Provider request包含同名字段。
- Limitations / counter-evidence：同一bytes可能同时承担goal与external fact；分类按当前producer / responsibility，不按物理存储唯一归属。
- Allowed article wording：`为了审查来源，本课程暂按六类Contributor拆解；这是分析taxonomy，不是wire schema。`

### `C03` — Select / Order / Scope per Step

- Status：`PARTIAL / PRODUCT-CONSTRAINT + COURSE DESIGN`
- Source / locator：`S01` conversation items prepending、instructions不随previous response自动携带、truncation auto / disabled；`S03` session history merge / continuation choice；`S04` finite model-specific window；`S05` tool loading / token overhead。
- Retrieved / product scope：`2026-08-21`；current hosted docs，package未固定。
- Proves：历史如何进入请求、instructions生命周期、tool集合和overflow policy会改变当次输入；应用必须为当前请求决定携带什么。
- Does not prove：每个系统都必须用同一Select算法、每Step都从零构建、本文Priority顺序最优。
- Limitations / counter-evidence：OpenAI conversation可自动prepend items；某些runtime由Provider / SDK管理continuation，因此“application手工装配全部内容”不是共同事实。
- Allowed article wording：`有限窗口与动态State使每次请求都需要明确选择策略；本课程建议按Step复核，而非宣称Provider统一算法。`

### `C04` — Tool Schema / Result consume Context budget

- Status：`CONFIRMED / ANTHROPIC-CURRENT-DOCS-SCOPED + OPENAI-SURFACE`
- Source / locator：`S04` everything in request counts；tool result turn；`S05` pricing lists `tools` parameter、`tool_use`、`tool_result` and managed tool system tokens；`S06` total tokens across messages / system / tools；`S01` OpenAI `tools / input / usage`；`R06` ToolDefinition / Result seam。
- Retrieved / product scope：`2026-08-21`；Anthropic current Claude contract is direct token evidence；OpenAI current reference only confirms request components / usage surface。
- Proves：在Anthropic当前产品范围内tool definitions、tool-use blocks与tool results进入token / window accounting；两类对象都可能挤占其他输入。
- Does not prove：所有Provider采用相同序列化、tokenizer、隐藏overhead或计费；Tool Result自动成为永久历史。
- Limitations / counter-evidence：client result需next request回传；server tools由Provider内部处理；实际保留范围依continuation策略而变。
- Allowed article wording：`在Anthropic当前合同中Schema和Result都消耗上下文；跨Provider只能保留“必须测量”的责任，不能复制token公式。`

### `C05` — Budget as capacity / cost constraint

- Status：`PARTIAL / PRODUCT-DOC-SCOPED`
- Source / locator：`S01` max output、truncation、usage；`S04` input + output share model context capacity；`S05` tool token / cost；`S06` preflight count。
- Retrieved / product scope：`2026-08-21`；current OpenAI / Anthropic docs。
- Proves：输入过大可失败或截断；output需要上限 / reserve；tools与results参与token成本；可做request-scoped preflight / usage observation。
- Does not prove：固定预算分配比例、更多context必然提升或降低quality、跨Provider统一成本模型。
- Limitations / counter-evidence：OpenAI truncation mode可自动drop earlier items或fail；Anthropic window / thinking处理随model变化。质量需具体eval。
- Allowed article wording：`预算同时约束可携带材料、输出余量和成本；质量后果是workload-specific，不能从token数量直接推出。`

### `C06` — Stable / Dynamic lifecycle split

- Status：`PROPOSAL / SOURCE-INFORMED COURSE TAXONOMY`
- Source / locator：`S01` per-response instructions / previous-response behavior；`S03` session-managed history；`R02` stable instruction / per-request goal / dynamic facts；`R08 / R10` state revision。
- Retrieved / scope：`2026-08-21`；product current docs + repository course definitions。
- Proves：不同来源有不同change cadence与authority，版本与scope必须记录；OpenAI instructions可按response替换，history可由session管理。
- Does not prove：stable永不变、dynamic必对应user role、两类覆盖全部Context或Provider使用此taxonomy。
- Limitations / counter-evidence：稳定规则也会版本升级；同一内容可在某scope稳定、在另一个scope动态。
- Allowed article wording：`Stable / Dynamic是生命周期审查标签；每条Contributor仍需单独记录source version和scope。`

### `C07` — Snapshot / Receipt observability design

- Status：`PROPOSAL / COURSE OBSERVABILITY DESIGN`
- Source / locator：`S02` generation span input / output / model / config / usage and trace controls；`S06` input token count；`R06 / R10` Trace records events but is not Evidence / State。
- Retrieved / product scope：`2026-08-21`；OpenAI Agents SDK current hosted docs，package未固定。
- Proves：公开SDK提供捕获request-visible input、output、model config与usage的实现构件；token preflight可增加budget observation。
- Does not prove：SDK trace天然包含source provenance、omission reason、conflict disposition、provider hidden context或本文Receipt字段。
- Limitations / counter-evidence：trace可disabled、exclude sensitive data，ZDR下不可用；需要application在assembly time主动记录selection decisions。
- Allowed article wording：`本文提出Receipt补充Trace通常不回答的“来源与为何舍弃”；schema是Proposal。`

### `C08` — Final Prompt is insufficient provenance

- Status：`CONFIRMED / COUNTER-EVIDENCE-SCOPED`
- Source / locator：`S01` separate instructions / input / tools / conversation；`S02` trace has multiple fields；`S05` managed tool system prompt；`S06` system / messages / tools counted；`R02 / R10` Prompt / State / Trace boundaries。
- Retrieved / product scope：`2026-08-21`；cited current products + repository boundaries。
- Proves：一个final Prompt string不含独立tool schemas、later tool results、current State revision、request controls、excluded contributor list或managed additions；不能回答完整provenance / omission reason。
- Does not prove：本文Receipt已经充分、Trace都不可靠、Provider内部一定存在某个未公开具体文本。
- Limitations / counter-evidence：某些应用会把很多组件render成单一文本；即便如此，rendered text仍不自动包含source version与omission decision。
- Allowed article wording：`只存最终文本最多说明“发送了什么可见文本”，不足以说明每段从哪来、什么没进入以及Provider还管理了什么。`

### `C09` — Receipt boundary: not Session / Memory / Checkpoint / complete effective Context

- Status：`PROPOSAL / COURSE CONTRACT + CONFIRMED LIMITATION`
- Source / locator：`S03` OpenAI Agents SDK Sessions stores conversation history；`S02` incomplete / optional trace surface；`S05` provider-managed tool prompt / server tools；`R11` Checkpoint / Memory responsibility boundary；课程Glossary定义Session为可追踪、恢复或回放的交互与执行边界。
- Retrieved / scope：`2026-08-21`；current SDK / product docs + Article 11 published boundary。
- Proves：课程Session可拥有、引用或治理history，但不等于单次Snapshot；OpenAI Agents SDK Sessions只证明其history实现。Checkpoint负责恢复所需control facts；Trace / Provider surfaces存在可见性限制。Receipt只能声明application-visible Snapshot及known unknowns。
- Does not prove：Receipt可复现hidden system text、model internal state、reasoning、server loop中间态；Receipt可安全Resume；Receipt字段是标准。
- Limitations / counter-evidence：同一storage component可以同时保存session、memory、checkpoint与receipt；物理合并不改变proof responsibility。Provider还可加入或转换应用不可见的信息。
- Allowed article wording：`Receipt回答“应用构建并可观察到的Context Snapshot”；provider-managed部分只记录存在 / unknown，Receipt只作描述、审计和比较。`

## Counter-evidence register

| Boundary | Evidence | Result |
|---|---|---|
| `Context != Prompt` | `S01 / S04 / S05 / R02`：tools、results、prior items、controls及managed prompt超出单一文本 | `CONFIRMED / CITED-SCOPE` |
| `Context != Session` | 课程Glossary：Session是可追踪、恢复或回放的交互与执行边界，可拥有、引用或治理history；`S03`只说明OpenAI Agents SDK Sessions的history实现 | `CONFIRMED / COURSE DEFINITION + OPENAI-SDK-SCOPED EXAMPLE` |
| `Tool Result != permanent history` | `S05`：client result要在下一request显式回传；未来保留取决于history mechanism | `CONFIRMED / ANTHROPIC-SCOPED` |
| `Snapshot != Memory` | `S03 / R11`：Memory / Session retention范围可跨run；Snapshot是one-Step selected view | `CONFIRMED BOUNDARY / COURSE MAPPING` |
| `Snapshot != Checkpoint` | `R11`：Checkpoint需identity、committed state、in-flight、budget、continuation | `CONFIRMED / REPOSITORY BOUNDARY` |
| `Provider-managed context exists` | `S05`：tools触发Anthropic special system prompt；server tools may loop internally | `CONFIRMED / ANTHROPIC-SCOPED` |
| `Receipt cannot represent complete effective Context` | `S02` tracing optional / redacted / ZDR unavailable；`S05` managed additions | `CONFIRMED LIMITATION` |

## Required design artifacts

- Investigation-Step Request Breakdown：`COMPLETE` in `research.md`
- Contributor Priority table：`COMPLETE / PROPOSAL` in `research.md`
- Context Receipt schema：`COMPLETE / PROPOSAL` in `research.md`
- Filled Context Receipt sample：`COMPLETE / PROPOSAL / NOT_EXECUTED` in `research.md`
- Snapshot A / B / C：`3 / 3 COMPLETE / PROPOSAL / DESIGN INPUT ONLY / NOT_EXECUTED` in `research.md`

## Evidence Gate checklist

- [x] `C01-C09` all have exact status, source, locator, date / scope, proves, does-not-prove, limitation and allowed wording.
- [x] Traceability `9 / 9`; core `BLOCKED=0`.
- [x] Current product facts use official primary docs retrieved `2026-08-21`.
- [x] Course working definition, six classes, priority and Receipt stay separate from product facts.
- [x] Counter-evidence covers Context / Prompt / Session, Tool Result / history, Snapshot / Memory / Checkpoint, provider-managed context and the effective-Context observability limit.
- [x] Request Breakdown, priority table, Receipt schema + filled sample complete.
- [x] Three snapshots are explicitly `PROPOSAL / DESIGN INPUT ONLY / NOT_EXECUTED`; Lab 05 was not implemented or pre-run.
- [x] No universal schema, Provider internals or effective-Context reproduction claim.

## Gate decision

`PASS`

Reason：`9 / 9` claims are traceable, `0` core `BLOCKED`, current primary-source scope and counter-evidence are complete, and all required design artifacts exist. `PARTIAL / PROPOSAL` claims have explicit wording ceilings and do not block the teaching spine.

Next allowed gate recommendation：`OUTLINE`。Author must preserve all product scopes and proposal labels; any stronger behavioral or quality claim returns to Research.
