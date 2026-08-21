# Article 12 Detailed Outline｜Context Engineering

## 0. Outline contract

- **Article type**：原理篇（信息主线篇；不写成 Provider request API 教程）。
- **Canonical title**：`Context Engineering：每一个 Step 到底应该看到什么`。
- **Shortest thesis**：`先审查某个 Step 的effective Context可能由什么构成；应用只能描述、审计和比较自己的application-visible Context Snapshot。`
- **Reader Change**：读者从“把 Prompt 写好即可”转为能够拆解一次 Model Step 的effective Context、application-visible Snapshot及其来源、选择、优先级、作用域、预算与可观察边界，并用 Receipt 审计或比较 Snapshot。
- **Course working definition (`PROPOSAL / NOT INDUSTRY STANDARD`)**：`Context = 某个 Model Step 实际可见的有效 token / 信息集合。Context Snapshot = 在特定 Provider / model / request contract 下，application-visible contributors 经 Select -> Order -> Scope -> Fit Budget 后形成的请求装配视图；effective Context还可能包含Provider-managed additions、transformations与unknowns。`
- **Evidence gate input**：`PASS`；Claim traceability `9 / 9`，core `BLOCKED=0`。正文不得把 `PARTIAL` 或 `PROPOSAL` 升格为 `CONFIRMED`。

### Teaching Spine

```text
Problem Space
  下一 Step 不只接收 Prompt；计划、State、工具、结果和历史都争夺有限输入。
        ↓
Abstract Model
  application-visible Contributor 经 Select -> Order -> Scope -> Fit Budget
  形成 Context Snapshot；Provider还可能加入、转换或保留unknown，形成effective Context。
        ↓
Concrete Mechanism
  用调查 Step 的 Request Breakdown、Contributor Priority 和 Receipt
  把“带什么、为何带、何时裁剪、已知什么未知什么”显式化。
        ↓
Engineering Judgment
  优先级不是 truth rank；预算不是通用质量公式；Stable / Dynamic 是审查标签。
        ↓
Verification Boundary
  Receipt 只描述、审计和比较 application-visible Context Snapshot；不能等同 Session、Memory、Checkpoint，
  更不能代表 Provider-managed / hidden additions、transformations或unknowns。
```

### Scope guardrails

- 维持 OpenAI Responses API、OpenAI Agents SDK、Anthropic Claude API / Platform 的 **2026-08-21 current hosted docs** 产品范围；不固定 SDK package / source commit，也不外推为跨 Provider 通用 wire schema。
- 三个 Snapshot 均为 `PROPOSAL / DESIGN INPUT ONLY / NOT EXECUTED`；本篇无独立 Lab，不实现、预演或宣称完成 Lab 05。
- 明确停止在 Context Assembly：不讲向量检索、长期 Memory、具体 Compaction、Article 13 Context Debugging、DeepSeek 私有源码或 BuildPilot Runtime；不把设计案例写成已运行系统。
- 四条不可混淆边界贯穿全文：`Prompt != Context`；`Context != Session`；`Tool Result != permanent history`；`Snapshot != Memory / Checkpoint`。

## 1. Section-by-section plan

| Section | Reader Question | Core Claim | Claim IDs / source IDs | Teaching duty | Diagram / table / example responsibility | Boundary / stop line | Bridge |
|---|---|---|---|---|---|---|---|
| 1. 问题空间：为什么“Prompt 写好了”仍会答错 | 08—11 已有 Plan、State、Result、Checkpoint 后，下一 Step 缺的是什么问题？ | 单一 Prompt 文本不能代表该 Step 的effective Context；本课程将应用可构建部分审查为 Context Snapshot。 | `C01` — `CONFIRMED / CITED-PRODUCTS-SCOPED + COURSE DEFINITION`；`S01/S04/S05/R02`；反证 `C08`。 | 从 Prompt Contract 推进到“effective Context可能由什么构成、应用实际带了什么”，不先介绍 API。 | 开场对照图：`Prompt text` vs `application-visible Context Snapshot` vs provider-managed additions / transformations / unknowns。 | 不把 Context 写为行业统一术语；不假定 request JSON 就是 Provider 内部全部有效上下文。 | 既然不止 Prompt，就要定义组成与责任。 |
| 2. 抽象模型：六类 Contributor 和一次 Assembly | 应按什么最小模型审查一次 Step？ | 本课程以 instruction、goal、working state、history、capability、external fact 六类 Contributor 做 provenance review；它们经 Select -> Order -> Scope -> Fit Budget 形成 Snapshot。 | `C02` — `PROPOSAL / COURSE TAXONOMY`；`C01`；`S01/S04/S05/R02/R06/R08/R09/R10`。 | 给出课程模型，区分来源责任与物理字段 / bytes。 | 主图：六类 Contributor → 四个 assembly action → Snapshot → Model Step；标 Stable / Dynamic 是生命周期审查标签。 | 六类不是 Provider wire schema、完备或互斥分类；同一 bytes 可承担不同责任。 | 模型确定后，读者要看真实粒度的选择过程。 |
| 3. Concrete mechanism I：同一调查 Step 应带什么 | 有限窗口下哪些资料进入，哪些留在 Receipt？ | 当前 State、Scope、Budget 使显式 Selection / Ordering 成为必要工程问题；本课程建议逐 Step 复核，不宣称统一最优算法。 | `C03` — `PARTIAL / PRODUCT-CONSTRAINT + COURSE DESIGN`；`C04`；`S01/S03/S04/S05/S06`。 | 用 Request Breakdown 说明 authority、scope、budget 与遗漏后果。 | 完整纳入 `INV-12-01` Request Breakdown（第 2 节），合成 Unity `CS0103` 首个可行动失败点。 | `INV-12-01` 是 `NOT_EXECUTED`；不把表中顺序称为产品事实或唯一正确顺序。 | “带什么”之后，回答“冲突时谁优先、预算先裁什么”。 |
| 4. Concrete mechanism II：Priority、Scope 和 Budget | Priority 是否等于真相等级？工具与输出如何争抢窗口？ | Priority 是课程保留策略，不是 Provider instruction hierarchy、truth rank 或 trust score；Schema、Result、输入与输出共享有限容量，质量后果须按 workload 验证。 | `C03` — `PARTIAL`；`C04` — `CONFIRMED / ANTHROPIC-CURRENT-DOCS-SCOPED + OPENAI-SURFACE`；`C05` — `PARTIAL / PRODUCT-DOC-SCOPED`；`C06` — `PROPOSAL`；`S01/S03/S04/S05/S06/R06/R08/R10`。 | 建立优先级、scope、lifecycle 与预算的工程判断；先预留输出再选择输入。 | 完整纳入 Contributor Priority 表；预算槽位图：output reserve、policy/goal/state、eligible tools、evidence、history、optional background。 | Anthropic token/window 结论不得外推为统一 tokenizer、计费或公式；不说更多 Context 必然更好 / 更差；Stable 不等于永不变化。 | 即使选得合理，还需记录选择、排除和未知。 |
| 5. Concrete mechanism III：Snapshot / Receipt | 只存最终 Prompt 或 Trace，能回答“它为何看到这些”吗？ | Trace 是可见 input/output/model/config/usage 的实现构件；Receipt 是课程 Proposal，补充来源、版本、选择、裁剪、冲突、未知和可见 request identity。 | `C07` — `PROPOSAL / COURSE OBSERVABILITY DESIGN`；`C08` — `CONFIRMED / COUNTER-EVIDENCE-SCOPED`；`C09` — `PROPOSAL / COURSE CONTRACT + CONFIRMED LIMITATION`；`S02/S03/S05/S06/R06/R10/R11`。 | 将“实际看到了什么”变成可追溯问题，明确 Receipt 不是完整复刻器。 | 完整纳入 `context-receipt-course-v1` schema 与 `INV-12-01 / diagnose-03` filled sample；以字段组讲教学职责。 | Sample hash 是 placeholder，token 为 `UNKNOWN`，无 runtime observation；Trace 可关闭 / 脱敏 / ZDR 不可用。不得称 Receipt 为产品保证或行业标准。 | 用 Receipt 比较未来 Lab 05 的三个设计输入。 |
| 6. Future Lab 05 design inputs：三个 Snapshot | 同一任务的不同 Context 包，怎样把差异暴露给未来验证？ | Snapshot 是 one-Step selected view；A/B/C 是未来 Lab 05 的 Proposal design inputs，不是观察或结果。 | `C03/C07/C09`；`S01/S02/S03/S05/R11`；research `SNAP-12-A/B/C`。 | 用最小包说明 revision mismatch、conflict、budget pressure 须在 assembly 时显式标记。 | 逐一纳入 A / B / C 的 contributor、conflict、omission、unknown 与 future-lab role（第 5 节）。 | 不实施 Lab 05；不写 Expected、Observation、Result、模型表现或 repair algorithm；不进入 Article 13 pollution / compression diagnosis。 | 设计输入只定义可见差异；回到本篇可验证边界。 |
| 7. Engineering judgment：什么常驻，什么按 scope 加载 | 怎样避免 Context system 吞掉 Session、Memory、Checkpoint 或全部历史？ | Stable / Dynamic 是课程生命周期审查标签；课程Session是可追踪、恢复或回放的交互与执行边界，可拥有、引用或治理history；OpenAI Agents SDK Sessions只是history implementation example。Memory可供应或保存历史，Checkpoint保存recovery control facts，Tool Result只有被后续assembly选中才成为Snapshot contributor。 | `C06` — `PROPOSAL / SOURCE-INFORMED COURSE TAXONOMY`；`C08/C09`；`S01/S03/S05/R11/R02/R06/R10`。 | 给出边界判断，复用02、06、08、09、10、11的定义而不重讲。 | 边界表：Prompt、Tool Result、History、Session、Memory、Checkpoint、Snapshot、Receipt各自负责 / 不负责什么。 | 不讲vector retrieval、long-term Memory机制或checkpoint recovery；不混淆raw Result、accepted State、Trace。 | 错答时先问“这个 Step 实际看到了什么？” |
| 8. Verification boundary、Learning Check 与收束 | Receipt 能证明 Provider 内部做了什么吗？如何审查 Snapshot？ | Receipt只能描述、审计和比较application-visible Context Snapshot，并声明known provider-managed additions与unknowns；不等于Session、Memory、Checkpoint或Provider内部完整Context。 | `C07/C08/C09`；`S02/S03/S05/S06/R11`。 | 用反证封顶承诺，交给Article 13的问题但不展开。 | 反证清单 + 3道Learning Check + 最短结论；不画故障分类或Compaction图。 | 停于“描述、审计和比较可见Snapshot”；不讲packing、compression、pollution、rehydration、retrieval或repair。 | 下一篇只接收Expected Context vs Actual Snapshot的问题。 |

## 2. Required teaching artifact A｜Investigation-Step Request Breakdown

**Draft placement**：Section 3 的中心表格。标题标为 `课程设计示例 / NOT_EXECUTED`；情境为合成 Unity `CS0103` 日志和匹配源码，目标是判断首个可行动失败点，不代表 BuildPilot Runtime。

| Order | Contributor | Concrete source / selected form | Authority / trust | Scope | Budget treatment | Omission / stale consequence |
|---:|---|---|---|---|---|---|
| 1 | Instruction | `prompt-contract-v3`；只基于 Evidence、未知写 `UNKNOWN`、不修改；concise instruction | application / trusted | project + role | `MUST_KEEP` | 丢失证据与 failure 边界 |
| 2 | Current Goal | `DIAGNOSE_FIRST_FAILURE@rev17`；goal + output contract | workflow / trusted | this Step | `MUST_KEEP` | 漂移成“修复”或“总结” |
| 3 | Working State | `EV-LOG-017 / EV-SRC-009 / unresolved=ROOT_CAUSE`；typed summary + refs | reducer / mixed | current run / rev17 | `MUST_KEEP` | 旧 revision 混淆已知 / 未知 |
| 4 | Capabilities | `read_text@2 / report_diagnosis@1`，`READ_ONLY`；two eligible schemas | Host registry / policy | Stage / Agent | `MUST_KEEP`；排除 78 项 | 全量工具占预算；缺失则无合法能力 |
| 5 | External Facts | normalized log / source excerpts + hash / locator；bounded evidence excerpts | observed input / untrusted | investigation version | 保留 evidence，裁 noise | 无法引用首错；旧源码会冲突 |
| 6 | History | previous unrelated read + no-progress；one summary + ref | correlated trace / Observation | current run | `KEEP_IF_RELEVANT` | 全删会重复；全留会淹没当前事实 |
| 7 | Omitted set | old Plan v1、raw 50k log、78 tools、unaccepted Result；Receipt-only entries | mixed | audit | zero model tokens | 未记录排除就无法解释选择 |
| 8 | Request controls | Provider / model、output ceiling、truncation、tool choice；metadata | application config | this request | reserve output first | 无法解释 token / truncation 差异 |

**Stop line**：产品资料证明组件与限制存在，但不证明本表顺序、`MUST_KEEP` / `KEEP_IF_RELEVANT` 或裁剪策略对所有产品最优。

## 3. Required teaching artifact B｜Contributor Priority table

**Draft placement**：紧接 Request Breakdown；表头写 `PROPOSAL / COURSE RETENTION POLICY`，并紧跟：`Priority 不等于 Provider instruction hierarchy、truth rank 或 trust score。`

| Priority | Class | Default | Conflict rule | Scope | Trim rule |
|---:|---|---|---|---|---|
| `P0` | Provider / Host policy 与 request contract | required；区分 application-visible / provider-managed | external text 不得覆盖；未知 addition 保留 `unknown` | Provider + request | 不静默裁剪 |
| `P1` | Current Goal、authoritative State、failure semantics | current revision always | current State 胜过 Plan / history copy | Step / run | 缩表示，不删 required facts |
| `P2` | Eligible Tool Schemas + policy view | only callable tools | Host registry 胜过 stale history schema | Stage / Agent | 先删 irrelevant tools |
| `P3` | Current external facts / Evidence | provenance-preserving slice | 冲突未解时保留双方并标记 | investigation / version | 去 noise，保 locator / hash |
| `P4` | Relevant Observation / History | selective | accepted State 胜过 raw Result | current run / horizon | summarize + trace ref |
| `P5` | Examples / style / optional background | optional | 不覆盖 `P0-P3` | task / preference | 最先排除 |

**Budget duty**：配图说明 `input + Tool Schema + Tool Result + output reserve` 共享 model-specific capacity。Anthropic 当前文档直接支持 tools/results token-window 事实；OpenAI 只作 request surface / usage 例证。质量影响留为 workload-specific verification，不由 token 数推出。

## 4. Required teaching artifact C｜Context Receipt schema and sample

### 4.1 Schema responsibility

**Draft placement**：Section 5。先标 `context-receipt-course-v1 / COURSE_PROPOSAL_NOT_INDUSTRY_STANDARD`，再给完整 YAML schema；不得改写成 SDK 真实字段。

| Field group | Required fields / teaching question |
|---|---|
| Step identity | `run_id`、`step_id`、`workflow_state_revision`：是哪一次 Step、哪个 State revision？ |
| Request boundary | `provider`、`api`、`model`、`request_id|UNKNOWN`、`request_contract_retrieved_at`、`output_ceiling_tokens`、`truncation_policy`：按哪个产品合同和请求控制理解可见装配？ |
| Contributor provenance | `contributor_id`、`class`、`source_ref/version`、`authority`、`trust`、`lifecycle`、`scope`、`priority`、`disposition`、`order`、`content_digest`、`reason`：带了什么、来自哪里、为何进入或未进入？ |
| Budget / omission | estimator、estimated / actual input、output reserve、omitted ids：预算和排除能否审计？ |
| Conflict / unknown | conflicts、unknowns、provider-managed `known_present` / description / `reconstructable: false`：哪些未解决、哪些不可见？ |
| Trace references | `request_span_ref`、`raw_request_ref`、`response_usage_ref`：Receipt 可引用 Trace，不把 Trace 等同 complete provenance。 |

### 4.2 Filled sample responsibility

**Draft placement**：schema 后的完整 `INV-12-01 / diagnose-03 / rev17` sample；保留 Proposal 标签和 unknown 值，并注释：

- `provider: ANTHROPIC_EXAMPLE_ONLY`、`model: NOT_SELECTED`、`request_id: UNKNOWN`、`output_ceiling_tokens: 1200`、`truncation_policy: APPLICATION_FAIL_CLOSED_PROPOSAL` 解释 contract / intent，不是已发请求。
- `I-01` 到 `H-OLD` 示范 instruction、goal、working state、capability、external fact、history 的 source version、scope、priority、disposition；`T-78` 示范 excluded capability 和 omission reason。
- `estimator: ANTHROPIC_COUNT_TOKENS_PLANNED_NOT_CALLED`，estimated / actual input 均 `UNKNOWN`，trace 三项均 `NONE`。
- `provider_managed_context.known_present: true` 只记录 Anthropic tool use 的 disclosed special system prompt；`reconstructable: false` 必须保留。
- `content_digest` 仅证明所指 bytes 一致；hash 是 placeholder，不能证明来源可信或 Provider 实际使用顺序。

**Stop line**：Receipt 需要 application 在 assembly 时记录 selection decisions；不能由 Trace 推出 source provenance、omission reason、hidden system text、reasoning 或 server-loop 中间态。

## 5. Required teaching artifact D｜Three Context Snapshots for future Lab 05

**Placement**：Section 6。三项同用 `PROPOSAL / DESIGN INPUT ONLY / NOT EXECUTED`；动作是比较可见 package，不是报告模型实验。

| Snapshot | Required visible package | Conflict / omission / unknown | Future-Lab role | Article 12 teaching duty and stop line |
|---|---|---|---|---|
| `SNAP-12-A / CONSISTENT_CURRENT` | `rev17`；instruction `prompt-contract-v3`；goal `DIAGNOSE_FIRST_FAILURE@rev17`；state `[EV-LOG-017, EV-SRC-009, unresolved=ROOT_CAUSE]`；capabilities `[read_text@2, report_diagnosis@1]`；facts `[build-4310-log, source-tree-9f2a]`；history `[step-02-no-progress-summary]` | conflicts `[]`；omits `78-unrelated-tools`、`raw-log-after-first-error`；unknown actual-provider-tokenization | control candidate with current compatible sources | 展示选择后的一致包；不是 correct-answer observation。 |
| `SNAP-12-B / STALE_STATE` | goal `rev17`，state `rev14`：`[EV-LOG-011, unresolved=SOURCE]`；facts `[build-4291-log, source-tree-9f2a]` | goal expects `rev17` while state `rev14`；omits `EV-LOG-017`、`EV-SRC-009`；unknown staleness source | stale package; test whether Receipt exposes revision mismatch | 展示 Receipt 应暴露 revision mismatch；不推断 cache / session merge / caller 的真实原因。 |
| `SNAP-12-C / CONFLICT_AND_BUDGET_PRESSURE` | `rev17`；capabilities `[80-global-tool-schemas]`；facts `[build-4310-log, source-tree-9f2a, stale-wiki-build-4291]`；history `[full-unbounded-history]` | conflict `BuildMenu.cs:42` vs `LegacyBuild.cs:88`；oversized tools/history 威胁 current source excerpt 与 output reserve；unknown truncation survivor | pollution / conflict package; test explicit selection and fail-closed handling | 展示冲突与预算压力须记录；不实施 provider truncation、pollution diagnosis 或 fail-closed runtime。 |

## 6. Dependency and source plan

| Article | Link responsibility | Boundary preserved |
|---:|---|---|
| 02 Prompt Engineering | Section 1 的 `Prompt != Context`；链接 `agent-engineering-02-prompt-engineering-contract-boundaries.md`。 | 不重讲 Prompt 六项合同，也不把 instruction hierarchy 外推为 Priority。 |
| 06 Tool Runtime | Section 2 capability / result source、Section 7 Tool Result 边界；链接 `agent-engineering-06-tool-runtime.md`。 | 不重讲 Validate / Policy / Execute，也不把 Trace 写为 complete provenance。 |
| 08 Agent Loop | 开头 Step identity；链接 `agent-engineering-08-agent-loop.md`。 | 不重讲 Loop 或 Stop。 |
| 09 Planning | old Plan v1 与 `P1` conflict rule；链接 `agent-engineering-09-planning.md`。 | 不把 Plan / history copy 升为 State。 |
| 10 State Machine 与 Workflow | State revision、Trace reference；链接 `agent-engineering-10-state-machine-workflow.md`。 | 不重讲状态机或把 Trace 等于 State。 |
| 11 Long-running Agent | `Snapshot != Checkpoint / Memory`；链接 `agent-engineering-11-long-running-agent.md`。 | 不进入 Resume、Retry、Cancellation 或 recovery。 |

外部引文只用 `S01` OpenAI Responses、`S02` Agents SDK tracing、`S03` Sessions、`S04` Anthropic context windows、`S05` Anthropic tool use、`S06` Anthropic token count；每处保留检索日、provider / product scope 和不证明范围。不得引用 DeepSeek 私有源码。

## 7. Learning Check, conclusion and competency mapping

### Learning Check

1. **State omission**：当前 Workflow State 未进入请求，是 Prompt 还是 Context 问题？预期：State 是 Context Contributor；先查 Receipt 的 selection、revision、disposition，不先重写 Prompt。
2. **Tool budget**：80 个 Tool Schema 常驻会挤占什么？预期：它们与输入、Tool Result、output reserve 共享容量；先选 eligible tools，质量影响须按 workload 验证。
3. **Final text provenance**：只保存最终 Prompt 能解释每段来源、版本、裁剪原因吗？预期：不能；Receipt 能描述、审计和比较 contributors、omissions、conflicts、unknowns、可见 request identity，但不代表 Provider Context。

### Shortest conclusion

`先审查这个 Step 的effective Context可能由什么构成，再讨论它为什么答错；应用只描述、审计和比较自己的Context Snapshot。`

### Job competency mapping

| Engineering judgment demonstrated | Article evidence | Implicit portfolio signal |
|---|---|---|
| 可解释请求装配 | 将目标、State、工具、外部事实、历史、预算与遗漏写成可审计 Receipt。 | 将“Prompt 调优”转成可复核的系统边界与数据合同。 |
| 约束下的选择 | 用 scope、priority、authority、trust、budget 解释保留 / 裁剪，不把优先级误称真相排序。 | 能说明有限窗口、能力治理、状态一致性的取舍。 |
| 可验证边界意识 | Proposal、产品事实、Trace、Snapshot、Session、Memory、Checkpoint、Provider unknown 分层。 | 不以设计材料冒充运行证明。 |

## 8. Claim-to-section coverage and New Core Facts Audit

| Claim | Status preserved | Sections | Wording ceiling |
|---|---|---|---|
| `C01` | `CONFIRMED / CITED-PRODUCTS-SCOPED + COURSE DEFINITION` | 1, 2 | 多类 request contributors 是产品范围事实；effective Context与application-visible Snapshot的区分是课程定义。 |
| `C02` | `PROPOSAL / COURSE TAXONOMY` | 2, 3 | 六类只作 provenance taxonomy，不作 wire schema / complete taxonomy。 |
| `C03` | `PARTIAL / PRODUCT-CONSTRAINT + COURSE DESIGN` | 3, 4, 5 | 逐 Step review 是课程建议，不称共同事实或最优算法。 |
| `C04` | `CONFIRMED / ANTHROPIC-CURRENT-DOCS-SCOPED + OPENAI-SURFACE` | 1, 4 | Anthropic tools/results window 范围明确；OpenAI 仅 request surface / usage。 |
| `C05` | `PARTIAL / PRODUCT-DOC-SCOPED` | 4, 7 | 预算约束容量、输出余量、成本；质量须 workload-specific verification。 |
| `C06` | `PROPOSAL / SOURCE-INFORMED COURSE TAXONOMY` | 2, 4, 7 | Stable / Dynamic 是 lifecycle review label，非永久属性或 Provider role mapping。 |
| `C07` | `PROPOSAL / COURSE OBSERVABILITY DESIGN` | 4, 5, 8 | Receipt描述、审计和比较可见Snapshot；Trace不自动有provenance。 |
| `C08` | `CONFIRMED / COUNTER-EVIDENCE-SCOPED` | 1, 4, 7, 8 | final Prompt alone 不足；不反向宣称 Receipt 完整。 |
| `C09` | `PROPOSAL / COURSE CONTRACT + CONFIRMED LIMITATION` | 4, 5, 7, 8 | Receipt != Session / Memory / Checkpoint / internal complete Context。 |

### New Core Facts Audit

| Audit item | Result |
|---|---|
| Claims covered | `9 / 9` (`C01-C09`) |
| New core fact required | `NO` |
| Evidence status altered | `NO` |
| Product scope broadened | `NO`；仅 OpenAI / Anthropic 2026-08-21 current-doc 范围。 |
| Runtime / Lab claim introduced | `NO`；`INV-12-01`、Receipt sample、`SNAP-12-A/B/C` 均为 Proposal / NOT_EXECUTED。 |
| Future scope consumed | `NO`；停止于 Context Assembly observability，不展开 Article 13、14—17 或 Lab 05。 |
| Stronger statement needed? | `NO`。如需 Provider 内部实际 Context、跨 Provider 统一公式、Receipt复现保证、Lab结果或质量曲线，必须 `RETURN_TO_RESEARCH`。 |

## 9. Length budget and drafting order

| Draft block | Target words | Required output |
|---|---:|---|
| Problem space and boundary | 700–900 | Prompt / Context distinction、provider-managed ceiling |
| Abstract model | 900–1,100 | assembly diagram + six Contributor definitions |
| Request Breakdown and Priority | 1,100–1,400 | two required tables + budget / scope judgment |
| Receipt schema and sample | 1,000–1,300 | schema responsibility + annotated Proposal sample |
| Snapshot design inputs | 600–800 | A/B/C comparison，明确非 Lab result |
| Engineering boundary, Learning Check, conclusion | 700–900 | 四条非等价边界、checks、最短结论 |
| **Total** | **5,000–6,400 Chinese characters-equivalent draft scale** | Major core lesson；表 / 图服务教学，不替代证据。 |

**Drafting order**：先写 Section 1–2 的 problem/model，再写 Request Breakdown 与 Priority，随后 Receipt / snapshots，最后边界与 Learning Check。任何超过 wording ceiling 的句子先回到 Research，不在 Draft 补新事实。
