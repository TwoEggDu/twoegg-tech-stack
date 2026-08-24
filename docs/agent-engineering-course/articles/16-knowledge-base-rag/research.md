# Research｜Article 16 Knowledge Base 与 RAG

## Research Boundary

本篇是原理 / 机制篇，按“问题空间 -> 抽象模型 -> 可观察运行链 -> 工程边界”研究。不得从某个向量数据库 API 或 Provider 产品能力起笔，也不得把产品文档中的单一实现写成行业统一事实。

本研究采用两层结论：

- 外部可确认事实：只来自论文、规范或官方文档，并保留来源、检索日与版本 / 页面范围。
- 课程工作模型：Knowledge Base、Memory、Evidence 的职责切分，以及 Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify 全链，均标为 COURSE PROPOSAL；它是可审查的工程分账，不冒充统一行业协议。

Article 16 是 NORMAL_ARTICLE，Required Lab 为 NONE。本轮没有获准创建 fixture，也没有 durable raw observation。因此所有具体效果比较均排除在已确认结论之外，实验只能以 PROPOSAL / NOT_RUN 设计进入研究产物。

## Research Questions

| RQ | Question | Status | Answer / Boundary |
|---|---|---|---|
| 16-RQ01 | Knowledge Base、RAG、Memory 与 Evidence 的最小边界分别是什么？ | ANSWERED / COURSE BOUNDARY | Knowledge Base 是带来源边界、可组织与检索的知识集合；RAG 是一次任务中的检索与生成模式；Memory 是跨步骤 / Session 保留、召回和治理信息的机制；Evidence 是针对具体 Claim 经来源、观测或实验限定后可审计的支持。物理存储可以重叠，职责不能互相冒充。 |
| 16-RQ02 | Retrieve、Filter、Rerank、Inject、Cite 每一段的输入、输出、失败语义与可观察证据是什么？ | ANSWERED / PROPOSAL | 已形成逐段 contract；原始 RAG 论文只直接支持检索增强生成的窄定义，不证明完整工程链是统一标准。 |
| 16-RQ03 | Keyword、Vector、Hybrid 分别改变 Retrieve 的什么行为，哪些结论需要 fixture 验证？ | ANSWERED / EFFECT NOT VERIFIED | Keyword / lexical 依词项匹配产生候选；dense / vector 依表示与相似度产生候选；Hybrid 融合多个候选或排名。哪种更好必须绑定 corpus、query、qrels、实现与 metric。 |
| 16-RQ04 | project、version、permission、freshness 与 scope 应在哪个阶段生效？ | ANSWERED / COURSE PROPOSAL | 这些字段首先是 eligibility / policy 条件，逻辑上应在候选进入任务相关性排序和 Context 前被审查；物理 pre-filter / post-filter 随实现而异。permission 还依赖真实身份、同步和 enforcement，不能因有一个 metadata filter 就声称授权闭环成立。 |
| 16-RQ05 | Citation 能证明什么、不能证明什么；检索结果何时只能是 Evidence candidate？ | ANSWERED | Citation 最多提供 answer span 到 source locator 的 provenance mapping；citation presence、correctness、completeness、source truth、current applicability 与 Claim acceptance 是不同问题。Retrieved item 在完成 scope / freshness / authority / claim-support 检查前只是 Evidence candidate。 |
| 16-RQ06 | 怎样用最小 golden query set 验证 Recall、Precision、ranking、citation correctness 与 answer utility？ | DESIGNED / NOT_RUN | 已冻结最小 fixture schema、query families、stage observations 与评价口径；无 raw observation，不报告任何效果。 |
| 16-RQ07 | Context Budget、chunk selection 与 inject format 怎样改变最终可用性，又有哪些外推限制？ | ANSWERED / EFFECT NOT VERIFIED | Inject 负责把已选内容以可追踪格式放入本 Step 的 Context Snapshot，并留下 omitted / truncated / conflict 信息；预算与格式的质量影响需要具体 workload 实验。 |

## Four-way Boundary｜KB、RAG、Memory、Evidence

| Object | 本篇工作定义 | Authority / lifecycle | 不自动等于 |
|---|---|---|---|
| Knowledge Base | 经过组织、可检索并带 source / version / scope 边界的知识集合 | collection 与 source lifecycle | 聊天历史、当前事实、一次检索结果 |
| RAG | 为当前任务检索外部知识、选择 / 装配后参与生成的模式 | request / step scoped；具体 pipeline 依实现 | Knowledge Base 本身、Memory 系统、Evidence acceptance |
| Memory | 在步骤或 Session 之间保留、恢复、检索和更新信息 / 状态的机制统称 | task / session / project policy；需要 promotion、freshness、conflict | Current Reality、Knowledge Base、已验证 Evidence |
| Evidence | 针对明确 Claim，带 provenance、scope、observation、proves / does-not-prove 与 limitations 的可审计支持 | claim-scoped acceptance；由 Evidence contract / Host 决定 | search hit、相似度分数、citation presence |

四个 logical role 可以共用文件、索引或数据库，但共用设施不改变职责。Article 15 已建立 Stored、Retrieved、Eligible、Injected 的课程分账；本篇在检索侧继续细化，但不重写 Memory promotion、conflict 或 lifecycle schema。完整 Evidence Contract 留给 Article 18，完整 Permission / Approval / Sandbox 留给 Article 19。

## Traceable RAG Chain｜COURSE PROPOSAL

~~~text
Task + Current State
  -> Query -> Retrieve -> Filter -> Rerank
  -> Inject -> Cite -> Use / Reject / Verify
~~~

该链是审查模型，不宣称每个产品都有七个独立组件，也不要求物理执行顺序完全相同。一个 Provider 可以在一次 API 调用中融合 retrieve、filter 与 ranking；审计时仍应能区分各阶段的输入、输出和拒绝理由。

| Stage | Input | Output / trace candidate | Failure semantics | Does not prove |
|---|---|---|---|---|
| Query | current goal、accepted state、identity / project / version constraints | query ID、raw query、rewrite、scope、requester、time | query drift、遗漏限定、错误 rewrite | corpus 有答案、召回成功 |
| Retrieve | query、index / corpus revision、retriever config | candidates，含 source ID、locator、score、retriever/version | miss、false positive、index stale、corpus gap | candidate 当前适用、可访问、可信 |
| Filter | candidates、project / version / permission / freshness / scope policy | kept / rejected candidate + exact reason + policy / metadata revision | wrong-scope survivor、过期 survivor、permission mismatch、over-filter | kept item 对任务最相关、内容正确 |
| Rerank | eligible candidates、current task、ranker config | ranked eligible list + score / features + ranker/version | relevant item 被压低、score 不可比较、ranker drift | top-1 是真相、满足 Context Budget |
| Inject | ranked items、Context Budget、selection / packing policy | selected excerpts、omitted set、order、source metadata、Receipt ref | truncation、限定丢失、冲突被抹平、source ID 断链 | 模型实际完整使用、回答正确 |
| Cite | injected source map、answer spans / claims | claim or span -> source ID / locator / version mapping | missing、wrong source、source 未支撑 claim、citation 不完整 | 来源真实、当前适用、claim 已验证 |
| Use / Reject / Verify | answer claim、citation map、authority / Evidence policy、current artifact | disposition + reason + verification ref / unknown | retrieved candidate 被直接接受、未知被补成确定事实 | Verification 已发生，除非有独立 observation / experiment |

原始 RAG 论文证明的是一个特定模型把预训练生成模型与可检索的非参数记忆结合，并让生成条件依赖 retrieved passages；它没有定义 Filter、Rerank、Cite 或 Use / Reject / Verify 的统一生产合同。当前 OpenAI Vector Store Search API 则把 score、file attributes、filters、ranking_options 和返回 chunk 分成不同字段；这能作为“相关性信号与属性过滤可分”的产品例证，不能外推成所有 RAG 系统的标准对象。

## Retrieve Strategies｜Stable Abstraction Only

| Strategy | Stable abstraction | Useful signal | Known blind spot | Evidence ceiling |
|---|---|---|---|---|
| Keyword / lexical | 对查询与文档的词项 / 稀疏表示进行匹配与排序 | exact identifier、error code、symbol、specialized term | paraphrase 或词面变化可能漏召回 | 不声称在 Article 16 fixture 上优于 / 劣于其他策略 |
| Vector / dense | 把 query 与 passage 映射到向量空间，并以相似度生成候选 | paraphrase / semantic proximity | 受 model、training domain、chunk 与 metric 影响；不表达 authority | DPR 的结果只适用于其 open-domain QA setup |
| Hybrid | 并行或串联多个 retriever，并融合候选 / ranking | exact 与 semantic signals 的组合 | fusion 方法、candidate depth 与 dataset 会改变结果 | Elastic / Azure 证明各自当前实现；不证明 universal gain |

DPR 在其数据集和 top-20 passage retrieval accuracy 口径下报告特定 dense retriever 对特定 BM25 baseline 的改善；BEIR 跨异构任务的结果又显示 lexical、sparse、dense、late-interaction 与 reranking 的表现和成本随数据集 / 设置变化。两者支持的不是“Dense 更好”，而是“效果结论必须绑定 workload、模型、qrels 与 metric”。

## Filter Before Rank｜Eligibility Is Not Relevance

| Constraint | Filter question | Reject / retain rule | Rerank may do | Rerank must not do |
|---|---|---|---|---|
| project | candidate 是否属于当前 repository / product identity？ | mismatch -> reject or historical-only | 在同项目 eligible set 内排序 | 用高 similarity 抵消项目错误 |
| version | source / artifact / API version 是否覆盖当前目标？ | incompatible / unknown -> reject、verify 或 mark unknown | 偏好当前任务更相关的 compatible item | 把版本不兼容降成小分差 |
| permission | 当前 principal 是否可读 / 可向本 Step 暴露？ | denied / unknown -> fail closed | 只排序已授权内容 | 把权限当 relevance feature 或仅靠模型忽略 |
| freshness | observed_at / indexed_at / source revision 是否仍在允许 horizon？ | stale -> historical-only、verify 或 reject | 在合格时间窗内排序 | 将相似旧记录伪装成 Current Reality |
| scope | tenant / user / environment / platform / build / profile 是否匹配？ | out-of-scope -> reject / scoped alternative | 排序同 scope 候选 | 用 Top-K 吞掉 scope verdict |

这里的“Filter before Rank”是逻辑责任顺序，不限定数据库的物理执行计划。Azure 当前文档允许过滤在 query processing 的不同位置发生，并建议为实际 query 测试；Article 16 不能把某个 pre/post-filter 配置写成通用最优解。

permission 还需要更严格的边界。Azure 当前文档展示 query-time ACL / RBAC 或 security filter 如何排除不匹配文档，也说明权限变更要等 metadata 同步后才反映到结果，并且部分能力是 preview。由此最多确认 permission metadata、query identity、filtering 与 synchronization 都影响 candidate eligibility；不能把字符串 filter 的存在升级为完整 authentication / authorization / compliance 证明。

## Rerank、Inject、Context Budget、Cite

Rerank 接受有限 candidate set，并为当前任务重新排序。Elastic 8.19 文档把 first-stage retrieval 与更昂贵的 reranker 分开；这支持“candidate generation 与 reranking 是不同责任”的窄事实。Reranker score 仍是 configured model / feature 下的 relevance signal，不是 truth、permission 或 freshness verdict。

Inject 不是“把 Top-K 全文粘进 Prompt”。它需要把 selected excerpt、source ID、locator、version、scope、conflict 与 omission 一起映射到 Article 12 的 application-visible Context Snapshot / Receipt。Context Budget 应先保留当前任务必需限定与输出余量，再决定 excerpt 长度、数量与顺序；没有真实 workload observation 时，不能声称某个 Top-K、chunk size 或注入格式提高 answer utility。

Citation 的最小职责是让 answer claim / span 回指某个实际 injected source locator。ALCE 把 correctness、citation quality 等维度分别评价，并报告现有系统仍会出现 citation support 不完整；这直接反驳“只要输出 citation 就已验证”的粗糙等式。

~~~text
Cited
  -> locator exists?
  -> source was actually retrieved and injected?
  -> cited passage supports this claim?
  -> source is applicable and authoritative for current scope?
  -> independent verification required?
~~~

只有最后的 Evidence acceptance / Verification 决策，才决定检索材料能否支撑某个 Claim。Citation 让审查成为可能，不替审查做决定。

## Minimum Reproducible Experiment｜PROPOSAL / NOT_RUN

- Experiment ID: 16-EXP01
- Status: PROPOSAL / NOT_RUN
- Required Lab: NONE
- Durable raw observation: ABSENT
- Fixture write authority in this execution: NONE
- Claim ceiling: 不比较 Keyword / Vector / Hybrid 效果，不给出最佳 Top-K / chunk / filter placement，不宣称 answer utility 改善。

未来由获得授权的执行者创建固定的 10 篇 synthetic Markdown 事故记录，不使用用户私有生产数据。每篇至少冻结 incident ID、project、Unity version、platform、build ID、observed_at、permission groups、title、summary、verified root cause 与 source locator。需要成对设计：同一错误码但项目不同、语义相似但版本不同、旧记录更相似但 scope 错误、未授权记录具有强 exact match，以及 corpus 无答案。

| Query family | Test purpose | Gold annotation |
|---|---|---|
| Q-EXACT | error code / symbol exact match | relevant IDs + locator |
| Q-PARAPHRASE | 同义改写 / 无共同关键词 | relevant IDs |
| Q-PROJECT-CONFLICT | 语义相似但 project 错误 | eligible / rejected IDs + reason |
| Q-VERSION-CONFLICT | Unity / SDK version 不兼容 | compatible range + verify verdict |
| Q-PERMISSION | 未授权文档具有高相关性 | allowed IDs；disallowed exposure = 0 |
| Q-FRESHNESS | 旧事故更相似但过期 | current / historical-only labels |
| Q-CITATION-MISMATCH | answer claim 与 locator 不一致 | claim-to-source support labels |
| Q-NO-ANSWER | corpus 无足够证据 | REJECT / UNKNOWN / VERIFY |

固定相同 corpus revision、gold annotations、query、Top-K accounting 与 evaluation script，分别运行 lexical、vector、hybrid，并保存每个配置的全部阶段输出。不得预设 Hybrid 胜出；model、embedding、index、chunking、fusion / reranker config 必须版本化。

| Stage | Raw observation to save | Metric / check | Not inferred |
|---|---|---|---|
| Retrieve | query、retriever/version、candidate IDs、scores、rank | Recall@K、Precision@K | candidate 当前适用 |
| Filter | input、kept / rejected、rule + metadata revision | scope violations、permission exposure、false rejection | filter placement 普遍最优 |
| Rerank | before / after ranks、ranker/version、score | MRR / nDCG@K | score 等于 truth |
| Inject | excerpts、order、token estimate、omitted / truncated IDs | gold-support coverage、lost qualifier | 模型已使用全部内容 |
| Cite | claim / span、source ID、locator、support label | citation correctness / completeness | source 真实 / 当前适用 |
| Use / Reject / Verify | disposition、reason、verification ref | abstention / verify routing、answer utility rubric | production reliability |

每次 run 必须保存所有 stage output；只有最终 answer 没有候选 / filter / inject trace，实验无效。Q-PERMISSION 的 disallowed exposure 必须为 0；citation 必须回指本次实际 injected item；Q-NO-ANSWER 若被当成 confirmed answer，则否定 fail-closed 设计。以上只验 synthetic fixture，不证明 production authorization、规模、延迟或跨领域效果。

16-EXP01 在本轮不是 Evidence Gate 的硬依赖，因为所有具体效果 Claim 已从文章允许表述中移除；Author 最多解释实验协议和未验证边界。若后续要声称 Hybrid 优于 Keyword、Rerank 改善排名、某个 Filter placement 更好、某种 Inject format 提升 utility，必须先由具有 fixture 写入和真实执行权限的 execution 完成实验，并回到 RESEARCH / EVIDENCE 合并 raw observation。

## Source Manifest

| ID | Primary / official source | Retrieved | Version / page scope | Used for |
|---|---|---|---|---|
| 16-S01 | Lewis et al., RAG, NeurIPS 2020 — https://papers.neurips.cc/paper/2020/file/6b493230205f780e1bc26945df7481e5-Paper.pdf | 2026-08-24 | NeurIPS 2020 paper | RAG narrow origin / model scope |
| 16-S02 | Karpukhin et al., DPR, EMNLP 2020 — https://aclanthology.org/2020.emnlp-main.550/ | 2026-08-24 | EMNLP 2020 paper | dense retrieval abstraction / scoped result |
| 16-S03 | Thakur et al., BEIR — https://arxiv.org/abs/2104.08663 | 2026-08-24 | paper / heterogeneous benchmark | counter-evidence; evaluation binding |
| 16-S04 | Gao et al., ALCE, EMNLP 2023 — https://aclanthology.org/2023.emnlp-main.398/ | 2026-08-24 | EMNLP 2023 paper | citation quality boundary |
| 16-S05 | OpenAI API, Search vector store — https://developers.openai.com/api/reference/python/resources/vector_stores/methods/search | 2026-08-24 | current hosted reference; no pinned SDK | filters、ranking、attributes、chunks、score |
| 16-S06 | Elastic Docs, Hybrid search — https://www.elastic.co/docs/solutions/search/hybrid-search | 2026-08-24 | current hosted docs | full-text + vector + fusion example |
| 16-S07 | Elasticsearch 8.19, Re-ranking — https://www.elastic.co/guide/en/elasticsearch/reference/8.19/re-ranking-overview.html | 2026-08-24 | explicitly 8.19 | candidate vs reranker |
| 16-S08 | Microsoft Learn, Azure Hybrid Search — https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview | 2026-08-24 | updated 2026-07-21; API 2026-04-01 example | hybrid / filter placement / ranking |
| 16-S09 | Microsoft Learn, Azure Document-Level Access Control — https://learn.microsoft.com/en-us/azure/search/search-document-level-access-overview | 2026-08-24 | updated 2026-08-12; GA / preview separated | permission filtering / sync limits |

Repository canonical、course plan、Glossary、Article 12 与 Article 15 只提供课程定位和内部 contract；课程规划不得冒充外部事实来源。

## Research Result

- Answered Research Questions: 7 / 7（16-RQ06 为 DESIGNED / NOT_RUN）。
- Core Claim direction: CONFIRMED=2、PROPOSAL=4、PARTIAL=0、BLOCKED=0。
- Concrete effect claims: 0；实验未执行，禁止补写 observed result。
- Evidence Gate recommendation: RECOMMEND EVIDENCE_GATE。
- Gate boundary: recommendation 只基于窄外部事实和显式课程提案；不批准 Outline，不改变 global durable state。若后续叙事需要任何效果比较，必须 RETURN_TO_RESEARCH。
