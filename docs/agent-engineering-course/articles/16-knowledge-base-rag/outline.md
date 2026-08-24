# Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite

> 最短判断：检索结果只是候选；Knowledge Base、RAG、Memory、Evidence 可共用设施却不能互相代替，Citation 也不等于 Verification。

## Gate Metadata

- Type: PRINCIPLE; Mode: NORMAL_ARTICLE; Required Lab: NONE.
- 16-C01/C02/C04/C06 are COURSE PROPOSAL. 16-C03/C05 are CONFIRMED only within their recorded evidence scope.
- 16-EXP01 is PROPOSAL / NOT_RUN. Observed Result is ABSENT. Raw Artifact is NONE.
- Bridge: Article 12 supplies Context Snapshot / Receipt. Article 15 separates Stored, Retrieved, Eligible, Injected and owns Memory promotion, freshness, and conflict. This article only extends the current-task retrieval seam.

## Teaching Spine

1. Problem space: a similar search hit is not automatically usable knowledge, Memory, or Evidence.
2. Abstract model: separate four logical objects and the task-local review chain.
3. Concrete engineering: a synthetic Unity/Jenkins historical-incident fixture explains fields, traces, and failures.
4. Engineering judgment: separate eligibility from relevance, and citation mapping from Evidence acceptance.
5. Verification boundary: define an experiment contract only; no fixture, raw observation, or result exists.

## 1. 问题空间：为什么“搜到”不是“可以相信并使用”

- **Reader Question**：一条相似的历史故障、文档片段或构建记录，为什么不能直接进入回答并称作知识、记忆或证据？
- **Core Claim IDs**：16-C01、16-C02（COURSE PROPOSAL）。
- **Evidence IDs / source anchors**：16-E01（course glossary、Article 15、Lewis et al.）；16-E02（Lewis et al.、OpenAI Vector Store Search、Elastic、ALCE）。
- **Content**：从 project、Unity version、platform、build、observed_at 或 permission/scope 不同的相似事故切入；建立 Stored != Retrieved != Selected != Injected != Cited != Verified。问题不是缺一个向量数据库 API，而是候选、拒绝理由、排序、Context Receipt、citation mapping 与 Claim acceptance 被写成同一件事。
- **Boundary / Forbidden**：不得把 hit、score、filter survivor 或 citation presence 写成 Current Reality、Evidence acceptance 或 Verification；不重写 Article 15 promotion/conflict/lifecycle。
- **Transition**：先固定四个对象的责任，再描述链路。

## 2. 抽象模型（一）：四对象责任分离

- **Reader Question**：Knowledge Base、RAG、Memory、Evidence 的最小区别是什么？共用设施为何不代表同义？
- **Core Claim IDs**：16-C01（COURSE PROPOSAL）。
- **Evidence IDs / source anchors**：16-E01（course glossary；Article 15；Lewis et al. RAG paper）。
- **Content**：

  | Object | Course responsibility | Not automatically |
  |---|---|---|
  | Knowledge Base | organized, retrievable collection with source/version/scope boundary | chat history, current truth, one hit |
  | RAG | request/step retrieval-and-assembly pattern | KB itself, Memory, Evidence acceptance |
  | Memory | cross-step/session retention, recall, governance | Current Reality, KB, verified Evidence |
  | Evidence | claim-scoped auditable support with provenance/scope/observation/proves-does-not-prove | hit, score, citation presence |

  Shared infrastructure does not erase responsibility. Project Memory may provide a locator/historical candidate, but retrieval cannot make it current authority.
- **Boundary / Forbidden**：课程工作定义，不是行业 taxonomy 或物理分库要求；不预写 Article 18 完整 Evidence Contract 或企业知识治理。
- **Transition**：对象分账后，分开一次 RAG task 的阶段责任。

## 3. 抽象模型（二）：可审查运行链，不是统一产品协议

- **Reader Question**：产品将 retrieval、filter、rank 融合在一次调用中时，怎样审查它？
- **Core Claim IDs**：16-C02（COURSE PROPOSAL）。
- **Evidence IDs / source anchors**：16-E02（Lewis et al.; OpenAI query/filter/score/chunks；Elastic reranking；ALCE）；Article 12/15。
- **Content**：主图明确标 COURSE PROPOSAL / review model：Task + Current State -> Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify。Query 记录 goal/rewrite/scope；Retrieve 输出 source ID/locator/score/retriever-version candidates；Filter 输出 kept/rejected + policy reason；Rerank 输出 eligible rank/score/ranker-version；Inject 输出 excerpts/order/omissions/metadata/Receipt ref；Cite 输出 claim/span 到 actually injected locator/version；Use/Reject/Verify 输出 disposition/reason。逐段写清不证明：candidate 非适用、survivor 非最相关、rank 非 truth、injection 非模型使用、citation 非已接受 Claim。
- **Boundary / Forbidden**：不称行业最低标准、必有组件或 universally optimal physical order；物理步骤可融合但审计责任不可消失；不宣称 BuildPilot Runtime。
- **Transition**：先讨论只改变 candidate pool 的 Retrieve strategies。

## 4. 具体工程（一）：Retrieve 策略只改变候选生成/融合

- **Reader Question**：Keyword、Vector、Hybrid 改变什么？为何没有普遍赢家？
- **Core Claim IDs**：16-C03（CONFIRMED，仅限记录的 workload/metric 证据范围）。
- **Evidence IDs / source anchors**：16-E03（DPR；BEIR；Elastic Hybrid Search；Azure Hybrid Search）。
- **Content**：Keyword/lexical 做词项或稀疏匹配，适合 error code/symbol/identifier，词面变化会漏；Vector/dense 按表示/相似度产生 candidate，可捕捉 paraphrase，受 model/domain/chunk/metric 限制且不表达 authority；Hybrid 融合 candidates/ranking，fusion/depth/dataset 改变结果。用“同错误码但 project 不同”“同义描述但 version 不兼容”的 SYNTHETIC ILLUSTRATIVE / NOT EXECUTED 例子说明 strategy 不代替 policy/verification。DPR 数字限其 QA setup；BEIR/产品文档支持 workload-dependent，不支持 universal winner。
- **Boundary / Forbidden**：不得写 Article 16 recall、precision、ranking、accuracy、latency、cost、answer utility、quality improvement 或赢家；不得说 Dense/Hybrid 恒优。
- **Transition**：candidate pool 后先判 eligibility，不能让 Top-K 替 scope/policy。

## 5. 具体工程（二）：Filter 管资格，Rerank 管任务相关性

- **Reader Question**：为何 Top-K similarity 不能裁定 project/version/permission/freshness/scope？是否要求固定执行计划？
- **Core Claim IDs**：16-C04（COURSE PROPOSAL）。
- **Evidence IDs / source anchors**：16-E04（Article 15；OpenAI Vector Store Search；Azure Hybrid/access docs）。
- **Content**：project、version、permission、freshness、scope 都是 eligibility/policy 条件；mismatch/denied/unknown 走 reject、historical-only、verify 或 unknown，并保存 exact reason。Rerank 只排序 eligible candidates 的当前任务相关性，score 不等于 truth、permission 或 freshness。synthetic incident 记录 project/version/platform/build/observed_at/scope/permission verdict。
- **Boundary / Forbidden**：Filter before Rank 是 logical responsibility order，非 physical pre/post-filter 要求；metadata filter 不等于 authentication、authorization、ACL、sandbox、compliance 或 permission closure，Article 19 才展开。
- **Transition**：eligible content 还需 Inject/Cite，二者留下 trace 而非 acceptance。

## 6. 具体工程（三）：Inject 与 Cite 留下 provenance，不替验证决定

- **Reader Question**：带 locator 的 excerpt 已 Inject、答案也有 citation 时，实际证明了什么？
- **Core Claim IDs**：16-C05（CONFIRMED，限 citation presence 与 correctness/completeness、applicability、Verification 可分）；承接 16-C02。
- **Evidence IDs / source anchors**：16-E05（ALCE；course glossary）；16-E02（ALCE；Article 12/15 bridge）。
- **Content**：Inject 保存 selected excerpts/order/source ID/locator/version/scope/conflict/omissions-truncation/Context Receipt ref；预算先保留任务限定与输出余量，不指定最佳 Top-K/chunk/format。Cite 把 answer claim/span 映射到 actually injected locator/version。图示审查梯：locator exists? -> retrieved and injected? -> passage supports claim? -> source applicable/authoritative now? -> independent verification required? ALCE 支持窄结论：citation presence 与 citation-support correctness/completeness 不同。
- **Boundary / Forbidden**：receipt 不证明模型使用或答案正确；citation 不证明 source truth/current applicability/Evidence acceptance/Verification；Article 18 才完整定义 Contract。
- **Transition**：工程 review 应在可融合实现中仍抓住这些责任。

## 7. 工程判断：实现可融合，责任与拒绝理由必须可审查

- **Reader Question**：不强制固定组件/时序时，如何判断实现越界？
- **Core Claim IDs**：16-C01/C02/C04（COURSE PROPOSAL）；16-C03/C05 只在窄确认范围内。
- **Evidence IDs / source anchors**：16-E01 至 16-E05，尤其 16-E02 stage responsibility、16-E04 eligibility seam、16-E05 citation/verification split。
- **Content**：坏实现 -> guard：KB 冒充 Memory -> 保留 Article 15 promotion/freshness/conflict；Retrieve score 冒充 eligibility -> verdict+reason；Rerank score 冒充 truth -> 只排 eligible；Inject 冒充模型已用 -> Receipt 只记 application-visible assembly；Cite 冒充 Verification -> support/applicability/acceptance 分账。最低 audit trail：candidate ref、source/locator/version/scope、retriever/ranker config、filter reason、selected/omitted、Receipt ref、citation mapping、disposition。
- **Boundary / Forbidden**：不设计 BuildPilot Runtime、production RAG、Skill loading、企业治理或 permission closure；不把 review model 写成生产事实。
- **Transition**：trace 不等于 observed effect，最后回到实验边界。

## 8. 验证边界：16-EXP01 只定义怎样观察

- **Reader Question**：最小实验冻结哪些输入、阶段输出、metrics 和失败条件？未运行时禁说什么？
- **Core Claim IDs**：16-C06（COURSE PROPOSAL）。
- **Evidence IDs / source anchors**：16-E06（BEIR；ALCE；research.md 16-EXP01）；16-CE06。
- **Content**：状态框：16-EXP01 = PROPOSAL / NOT_RUN；proposed 10 synthetic Markdown incidents = NOT_CREATED；Observed Result = ABSENT；Raw Artifact = NONE。获独立授权后才冻结 corpus revision、gold annotations、queries、retriever/embedding/index/chunk/fusion/ranker config、Top-K accounting、完整 stage output。设计 query families：exact、paraphrase、project/version conflict、permission、freshness、citation mismatch、no-answer。允许定义而不填数：Recall@K/Precision@K、MRR/nDCG@K、scope violation、permission exposure、false rejection、budget/omission trace、claim-to-source support、unsupported acceptance。未授权暴露、wrong-scope survivor、unmapped claim、no-answer 被编造成 answer 均为未来失败 criteria。
- **Boundary / Forbidden**：不得给任何 recall/precision/ranking/accuracy/latency/cost/answer-utility/quality-improvement/赢家结论；不得创建 fixture、运行实验、把 expected 写成 observed 或外推 production。
- **Transition**：没有观察即止于 proposal。

## 9. 最短结论

- **Reader Question**：面对“RAG 结果看起来不错”，第一句问什么？
- **Core Claim IDs**：16-C01 至 16-C06，按各自 ceiling。
- **Evidence IDs / source anchors**：16-E01 至 16-E06。
- **Content**：**检索结果只是候选：先分清对象、资格、相关性、注入与引用；能否支撑 Claim 仍取决于独立 Evidence/Verification。**
- **Boundary / Forbidden**：不扩写成行业 taxonomy、最优流水线或已验证生产效果。
- **Transition**：Article 17 Skill；Article 18 Evidence Contract；Article 19 Permission/Approval/Sandbox。

## Claim-to-section Traceability（6 / 6）

| Claim | Ceiling | Sections | Evidence anchors | Wording boundary |
|---|---|---|---|---|
| 16-C01 | PROPOSAL | 1,2,7,9 | 16-E01: glossary, Article 15, Lewis | Course split; not industry taxonomy/physical split. |
| 16-C02 | PROPOSAL | 1,3,6,7,9 | 16-E02: Lewis, OpenAI, Elastic, ALCE | Review model; not fixed components/optimal order. |
| 16-C03 | CONFIRMED | 4,7,9 | 16-E03: DPR, BEIR, Elastic, Azure | workload/metric scope; no effect/winner. |
| 16-C04 | PROPOSAL | 5,7,9 | 16-E04: Article 15, OpenAI, Azure | eligibility/relevance; no filter-placement/permission closure. |
| 16-C05 | CONFIRMED | 6,7,9 | 16-E05: ALCE, glossary | citation not correctness/applicability/Verification. |
| 16-C06 | PROPOSAL | 8,9 | 16-E06: BEIR, ALCE, 16-EXP01 | contract; NOT_RUN/ABSENT/NONE. |

Coverage: **6 / 6**. New core fact or wording above ceiling => RETURN_TO_RESEARCH.

## Figure / Example Duties

| Item | Teaching duty | Boundary |
|---|---|---|
| Four-object diagram (S2) | collection/task RAG/cross-session governance/claim support | COURSE PROPOSAL; not industry taxonomy |
| Review chain/stage receipts (S3) | input/output/failure/trace | not mandatory provider components/optimal order |
| Strategy table (S4) | lexical/dense/hybrid signal/blind spot | no Article 16 metric/winner |
| Filter/Rerank matrix (S5) | eligibility vs relevance | not Article 19 enforcement |
| Citation review ladder (S6) | locator/support/applicability/verification | citation not acceptance |
| Unity/Jenkins incident (S4-5) | scope/version/permission/freshness verdict | SYNTHETIC ILLUSTRATIVE / NOT EXECUTED |
| 16-EXP01 contract (S8) | later-run preservation | no fixture/raw artifact/observed result |

## Learning Check

1. Different project/build after Retrieve: which verdict is missing, and why cannot rank first?
2. One API fuses search/filter/rank: why keep responsibilities and reasons distinct?
3. Injected excerpt with locator: does it prove model use, answer correctness, or verification?
4. Citation but unsupported passage: which boundary failed, and what can citation presence prove?
5. Why cannot DPR's scoped result choose a 16-EXP01/Unity-Jenkins winner?
6. Before 16-EXP01 runs, which design facts are writable and which numeric/effect conclusions must be absent?

## Job Competency Mapping

| Competency | Learning outcome | Boundary |
|---|---|---|
| Retrieval design review | candidate/eligibility/relevance/assembly/claim support | review model, not production certification |
| Incident knowledge reuse | scope/version/build/freshness fail-closed or verify reason | synthetic only |
| Auditability | locator/config/filter reason/Receipt/citation mapping | trace not verification |
| Experiment design | fixture/gold query/config/output/metric/criteria | 16-EXP01 NOT_RUN |
| Evidence discipline | citation/applicability/acceptance/Verification | full Contract is Article 18 |

## Explicit Non-scope

- No embedding/chunking/vector-database/Top-K/provider-API parameter catalogue.
- No Article 15 Memory schema, promotion, conflict, lifecycle, Session taxonomy, or Current Reality rewrite.
- No prewriting Article 17 Skill, Article 18 full Evidence Contract, or Article 19 permission/approval/sandbox/ACL/identity/enforcement.
- No BuildPilot Runtime, production RAG, real Unity/Jenkins corpus, or production effect claim.
- No industry-universal taxonomy, mandatory component set, or universally optimal physical order.
