# Evidence｜Article 16 Knowledge Base 与 RAG

## Evidence Gate Status

- Current: EVIDENCE_GATE PASS / EVIDENCE_READY
- Required Lab: NONE
- Experiment: 16-EXP01 / PROPOSAL / NOT_RUN
- Durable raw observation: ABSENT
- Core BLOCKED Claims: 0
- Gate rule applied: 所有具体效果 Claim 已移除；CONFIRMED 只保留来源直接支持的窄事实，课程架构与控制策略全部标为 PROPOSAL。实验未执行不支持任何 performance / utility 结论。
- Master Gate: PASS / 2026-08-24T15:35:42+08:00

## Claim Register

| Claim ID | Claim | Status | Evidence Card | Wording ceiling |
|---|---|---|---|---|
| 16-C01 | Knowledge Base、RAG、Memory 与 Evidence 在本课程中是不同职责对象 | PROPOSAL | 16-E01 | 课程工作定义，不写成统一行业 taxonomy |
| 16-C02 | 本课程用 Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify 审查 RAG 运行链 | PROPOSAL | 16-E02 | 可合并物理阶段；原始 RAG 论文未定义完整链 |
| 16-C03 | Keyword / lexical、Vector / dense 与 Hybrid 是不同的候选生成 / 融合策略，效果依 workload 与评价口径 | CONFIRMED | 16-E03 | 不宣称任一策略普遍更好 |
| 16-C04 | project、version、permission、freshness 与 scope 应作为 eligibility / policy 条件与 relevance ranking 分账 | PROPOSAL | 16-E04 | 逻辑责任，不规定通用 pre/post-filter 实现 |
| 16-C05 | Citation presence 与 citation correctness / completeness、source applicability 和 Verification 是不同判断 | CONFIRMED | 16-E05 | citation 只提供可审查 mapping，不自动接受 Claim |
| 16-C06 | 任何具体 retrieval / ranking / citation / answer utility 效果都必须绑定 fixture、gold query、版本化 config 与 stage observation | PROPOSAL | 16-E06 | 16-EXP01 未运行；不得出现 observed improvement |

## Evidence Cards

### Evidence 16-E01｜Four logical responsibilities

- Article: 16 Knowledge Base 与 RAG
- Claim ID: 16-C01
- Claim: Knowledge Base、RAG、Memory 与 Evidence 在本课程中是不同职责对象。
- Evidence Status: PROPOSAL
- Evidence Class: DESIGN_PROPOSAL
- Source Type: repository contract + primary paper
- Source: docs/agent-engineering-course/glossary.md；Article 15；Lewis et al. RAG paper https://papers.neurips.cc/paper/2020/file/6b493230205f780e1bc26945df7481e5-Paper.pdf
- Repository: twoegg-tech-stack
- Commit: current Article 15 completion ancestry + N/A for external paper
- File: glossary.md；content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md
- Symbol: N/A
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-24 Asia/Shanghai
- Version Scope: 当前课程 contract；RAG 论文为 NeurIPS 2020 特定模型
- Reproduction: 读取 Glossary 的 Knowledge Base / RAG / Memory / Evidence 定义、Article 15 的八对象边界，并对照 RAG 论文摘要与模型定义。
- Observation: 课程已把四个词分配给 collection、retrieval-generation pattern、cross-step/session retention 与 claim support；RAG 论文把其模型描述为 parametric 与 non-parametric memory 的组合，并未定义本课程四对象 taxonomy。
- Counter-evidence Searched: 检查原始 RAG 论文是否把 RAG、Knowledge Base、Memory、Evidence 定义成同一对象；未发现该统一定义。Article 15 还明确说明 Project Memory 与 Knowledge Base 可共用 store 但不等同。
- Interpretation: 这是一项课程边界设计，外部论文只支持 RAG 的窄历史语境，不能把课程 taxonomy 升格为行业事实。
- Proves: 课程内部可以稳定地分账四个职责，且该分账不与原始 RAG 窄定义冲突。
- Does Not Prove: 行业统一接受此 taxonomy；四个职责必须物理分库；任何具体系统已实现这些 guard。
- Limitations: 依赖课程工作定义；完整 Evidence Contract 要到 Article 18。
- Course Usage: 开篇边界与抽象模型；必须标 COURSE PROPOSAL。
- BuildPilot Implication: DEFER — 只保留 future design seam，不宣称 BuildPilot Runtime。
- Owner: RESEARCHER
- Verified At: 2026-08-24

### Evidence 16-E02｜Auditable RAG chain

- Article: 16 Knowledge Base 与 RAG
- Claim ID: 16-C02
- Claim: 本课程用 Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify 审查 RAG 运行链。
- Evidence Status: PROPOSAL
- Evidence Class: DESIGN_PROPOSAL
- Source Type: primary paper + official product docs + repository contract
- Source: Lewis et al. RAG paper；OpenAI Vector Store Search https://developers.openai.com/api/reference/python/resources/vector_stores/methods/search；Elasticsearch 8.19 Re-ranking https://www.elastic.co/guide/en/elasticsearch/reference/8.19/re-ranking-overview.html；ALCE https://aclanthology.org/2023.emnlp-main.398/
- Repository: twoegg-tech-stack
- Commit: N/A
- File: research.md；Article 12 Context Engineering；Article 15
- Symbol: N/A
- Call Path: Task -> Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify
- Experiment: 16-EXP01 / NOT_RUN
- Fixture: proposed 10 synthetic Markdown incidents / NOT_CREATED
- Trace: ABSENT
- Retrieved / Run At: 2026-08-24 Asia/Shanghai
- Version Scope: RAG paper 2020；OpenAI current hosted API reference；Elastic 8.19；ALCE EMNLP 2023
- Reproduction: 对照各来源的显式对象：retrieved passages；query / filters / ranking options / chunks / score；first-stage candidates / reranker；citation evaluation。
- Observation: 不同来源分别暴露链上的部分责任，没有一个来源定义完整七段统一协议。
- Counter-evidence Searched: 原始 RAG 论文没有 Filter、Rerank、Cite 或 Use / Reject / Verify 的生产合同；当前产品可能把多个阶段融合在一次 API 调用中。
- Interpretation: 完整链只能作为课程审查模型，用来保存 stage input/output/failure，不可写成 RAG 的最低行业定义。
- Proves: 每一段有独立审计价值；物理实现可合并但不能抹掉 provenance、filter reason、rank config、injection receipt 与 citation mapping。
- Does Not Prove: 所有 RAG 系统都有这些组件；该顺序是性能最优；16-EXP01 已执行。
- Limitations: Use / Reject / Verify 跨入 Evidence / Host acceptance 边界；Article 18 才完整展开。
- Course Usage: 全文主链；图和表必须带 COURSE PROPOSAL。
- BuildPilot Implication: ADOPT — 未来设计保留 stage receipt；仍无 production implementation。
- Owner: RESEARCHER
- Verified At: 2026-08-24

### Evidence 16-E03｜Lexical、dense、hybrid and workload scope

- Article: 16 Knowledge Base 与 RAG
- Claim ID: 16-C03
- Claim: Keyword / lexical、Vector / dense 与 Hybrid 是不同的候选生成 / 融合策略，效果依 workload 与评价口径。
- Evidence Status: CONFIRMED
- Evidence Class: INFERENCE
- Source Type: peer-reviewed papers + official product docs
- Source: DPR https://aclanthology.org/2020.emnlp-main.550/；BEIR https://arxiv.org/abs/2104.08663；Elastic Hybrid Search https://www.elastic.co/docs/solutions/search/hybrid-search；Azure Hybrid Search https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview
- Repository: N/A
- Commit: N/A
- File: N/A
- Symbol: N/A
- Call Path: query -> lexical candidates / dense candidates -> optional fusion
- Experiment: 16-EXP01 / NOT_RUN
- Fixture: proposed only
- Trace: ABSENT
- Retrieved / Run At: 2026-08-24 Asia/Shanghai
- Version Scope: DPR EMNLP 2020；BEIR paper；Elastic current hosted docs；Azure page updated 2026-07-21 with API 2026-04-01 example
- Reproduction: 阅读 DPR 对 sparse baseline 与 dense retriever 的定义和 scoped metric；阅读 BEIR 的 heterogeneous tasks / retrieval families；核对 Elastic / Azure 对 full-text + vector + fused ranking 的当前说明。
- Observation: DPR 的性能数字绑定其 open-domain QA datasets、baseline 与 top-20 accuracy；BEIR 在异构数据集比较 lexical、sparse、dense、late-interaction 与 reranking；Elastic / Azure 的 hybrid 是各自实现中的 full-text 与 vector result fusion。
- Counter-evidence Searched: DPR 的 scoped dense advantage 不能抵消 BEIR 展示的跨数据集差异；Azure 也明确建议对实际 query 测试 filter placement。未找到支持“Dense 或 Hybrid 普遍最佳”的一手证据。
- Interpretation: 可确认策略类别与融合职责；不能确认 Article 16 synthetic fixture 上的赢家。
- Proves: Keyword、dense、hybrid 改变 candidate generation / fusion；评价必须绑定 dataset、query、qrels、config 与 metric。
- Does Not Prove: Hybrid 普遍提升 recall / precision；dense 总胜 BM25；任一实现适合 Unity / Jenkins corpus。
- Limitations: 官方产品文档是产品范围事实；论文结果不代表 2026 产品默认设置。
- Course Usage: Retrieve strategy 稳定抽象与“不得写 universal winner”反证。
- BuildPilot Implication: DEFER — 必须先执行固定 fixture。
- Owner: RESEARCHER
- Verified At: 2026-08-24

### Evidence 16-E04｜Eligibility filters are not ranking

- Article: 16 Knowledge Base 与 RAG
- Claim ID: 16-C04
- Claim: project、version、permission、freshness 与 scope 应作为 eligibility / policy 条件与 relevance ranking 分账。
- Evidence Status: PROPOSAL
- Evidence Class: DESIGN_PROPOSAL
- Source Type: repository evidence + official API / security docs
- Source: Article 15；OpenAI Vector Store Search；Azure Hybrid Search；Azure Document-Level Access Control https://learn.microsoft.com/en-us/azure/search/search-document-level-access-overview
- Repository: twoegg-tech-stack
- Commit: Article 15 completion commit recorded by Master
- File: content/ai-empowerment/agent-engineering-15-session-long-term-project-memory.md
- Symbol: N/A
- Call Path: retrieved candidates -> eligibility verdict -> eligible candidates -> relevance rerank
- Experiment: 16-EXP01 / NOT_RUN
- Fixture: proposed permission / project / version / freshness conflicts
- Trace: ABSENT
- Retrieved / Run At: 2026-08-24 Asia/Shanghai
- Version Scope: Article 15 course contract；OpenAI current hosted reference；Azure hybrid page updated 2026-07-21；Azure access page updated 2026-08-12，GA / preview boundaries retained
- Reproduction: 对照 OpenAI API 中 score 与 attributes / filters 的分离；对照 Azure filter placement、ACL / RBAC / security filter、metadata synchronization 说明；回读 Article 15 scope / freshness / conflict verdict。
- Observation: 产品接口允许属性过滤与相关性排序分开表达；Azure permission result 取决于 identity、indexed permission metadata 与同步，且部分 native feature 是 preview。
- Counter-evidence Searched: Azure 允许物理 pre-filter / post-filter，说明“Filter 必须作为独立服务且永远先执行”不成立；security-filter pattern 也明确不等于自行完成 authentication / authorization。
- Interpretation: 课程应固定逻辑 eligibility seam，而不规定所有引擎的物理执行顺序。
- Proves: 当前产品中 permission / metadata filtering 与 relevance score 可分；permission metadata freshness 是真实限制。
- Does Not Prove: 五个字段是行业统一集合；一个 metadata filter 构成完整授权；某个 filter placement 普遍更好。
- Limitations: Article 19 才覆盖完整 Permission / Approval / Sandbox；本篇只定义检索侧 fail-closed seam。
- Course Usage: Filter vs Rerank 表；Unity / Jenkins project / version / build 示例。
- BuildPilot Implication: ADOPT — future schema保留 scope / permission / freshness verdict；DEFER enforcement。
- Owner: RESEARCHER
- Verified At: 2026-08-24

### Evidence 16-E05｜Citation is not verification

- Article: 16 Knowledge Base 与 RAG
- Claim ID: 16-C05
- Claim: Citation presence 与 citation correctness / completeness、source applicability 和 Verification 是不同判断。
- Evidence Status: CONFIRMED
- Evidence Class: INFERENCE
- Source Type: peer-reviewed paper + repository Evidence definition
- Source: ALCE https://aclanthology.org/2023.emnlp-main.398/；docs/agent-engineering-course/glossary.md
- Repository: twoegg-tech-stack
- Commit: N/A
- File: glossary.md
- Symbol: N/A
- Call Path: answer claim/span -> citation locator -> support check -> applicability / authority check -> Evidence disposition
- Experiment: 16-EXP01 / NOT_RUN
- Fixture: proposed citation-mismatch queries
- Trace: ABSENT
- Retrieved / Run At: 2026-08-24 Asia/Shanghai
- Version Scope: ALCE EMNLP 2023；current course glossary
- Reproduction: 阅读 ALCE 对 end-to-end retrieval + cited answer 的评价设置，以及 correctness、citation quality 等分离维度；对照课程 Evidence 的 proves / does-not-prove contract。
- Observation: ALCE 必须评价 citation quality，且报告 citation support 仍可能不完整；citation token / link 的存在没有自动完成 support judgment。
- Counter-evidence Searched: 检查论文是否把 citation presence 当作 correctness 或 completeness 的充分条件；没有。论文反而建立独立指标。
- Interpretation: Citation 提供可审查 provenance mapping；Evidence acceptance 仍需判断 passage 是否支持 claim、source 是否适用于当前 scope，以及是否需要 current verification。
- Proves: 有 citation 与 citation 正确 / 完整是不同事实；不能从 citation presence 自动推出 Verification。
- Does Not Prove: ALCE 指标覆盖所有 citation 语义；被引用来源本身真实；本文已验证任一具体答案。
- Limitations: 完整 Evidence acceptance contract 留给 Article 18；当前无 citation fixture run。
- Course Usage: Cite 与 Use / Reject / Verify 边界；必须避免“引用即真相”。
- BuildPilot Implication: ADOPT — future claim-to-source mapping；DEFER automated acceptance。
- Owner: RESEARCHER
- Verified At: 2026-08-24

### Evidence 16-E06｜Experiment protocol, no observed effect

- Article: 16 Knowledge Base 与 RAG
- Claim ID: 16-C06
- Claim: 任何具体 retrieval / ranking / citation / answer utility 效果都必须绑定 fixture、gold query、版本化 config 与 stage observation。
- Evidence Status: PROPOSAL
- Evidence Class: DESIGN_PROPOSAL
- Source Type: benchmark papers + frozen course experiment design
- Source: BEIR https://arxiv.org/abs/2104.08663；ALCE https://aclanthology.org/2023.emnlp-main.398/；research.md section 16-EXP01
- Repository: twoegg-tech-stack
- Commit: N/A
- File: docs/agent-engineering-course/articles/16-knowledge-base-rag/research.md
- Symbol: N/A
- Call Path: fixture + qrels + config -> staged run -> raw observations -> metrics -> limited claim
- Experiment: 16-EXP01 / PROPOSAL / NOT_RUN
- Fixture: 10 synthetic Markdown incidents / NOT_CREATED
- Trace: ABSENT
- Retrieved / Run At: 2026-08-24 Asia/Shanghai
- Version Scope: proposed Article 16 fixture；BEIR / ALCE paper scopes
- Reproduction: 当前不可复现运行；先获得 fixture 写入 / execution authority，再按 research.md 冻结 corpus revision、query、qrels、retriever / ranker config 与 stage outputs。
- Observation: 当前 repository 没有 Article 16 fixture、gold query、commands、exit code 或 raw observation。
- Counter-evidence Searched: Course plan 提到可用 10 篇 Markdown 事故记录，但它只是规划输入，不是已执行实验；文件不存在不能解释为零结果或 PASS。
- Interpretation: 只能确认实验设计可审查，不能确认 retrieval、ranking、citation 或 utility 的具体效果。
- Proves: 下一次实验需要保存什么；哪些主张在执行前必须被禁止。
- Does Not Prove: 16-EXP01 complete；任何策略改善指标；production reliability / security / scale。
- Limitations: Article 16 不是 required Lab，本 execution 无 fixture write authority；synthetic corpus 即使执行也不能外推生产。
- Course Usage: Retrieval Eval 设计与验证边界；只用未来式 / proposal 语态。
- BuildPilot Implication: DEFER — no Runtime or effect claim。
- Owner: RESEARCHER
- Verified At: 2026-08-24

## Experiment Evidence

### 16-EXP01 status

- Expected Observable: Retrieve candidates、Filter removals、Rerank order、Injected context / omitted set、Citation mappings、Use / Reject / Verify dispositions。
- Observed Result: ABSENT / NOT_RUN。
- Raw Artifact: NONE。
- Claim impact: C03 只保留策略抽象；C04 / C06 保持 PROPOSAL；不得增加任何 effect Claim。
- Authorization needed before execution: 独立、明确授权创建 fixture / golden query / observation artifacts，并允许真实执行；完成后必须返回 RESEARCH / EVIDENCE 合并，不得让 Author 自行解释。

## Counter-evidence Register

| Counter ID | Risky statement | Counter-evidence / boundary | Disposition |
|---|---|---|---|
| 16-CE01 | RAG 天然包含完整 Retrieve -> Cite 链 | 原始 RAG 论文只定义特定 retrieval-augmented generation model | 改为 COURSE PROPOSAL |
| 16-CE02 | Dense / Hybrid 普遍优于 keyword | DPR 数字是 scoped setup；BEIR 显示跨 dataset 差异 | 禁止 universal winner |
| 16-CE03 | Top-K similarity 足以决定可用性 | OpenAI API 分离 score 与 attributes / filters；Article 15 分离 Retrieved / Eligible | relevance 与 eligibility 分账 |
| 16-CE04 | 有 permission filter 就完成授权 | Azure security filter / ACL 依身份、metadata 与同步，且 feature scope 不同 | 只写检索侧 eligibility seam |
| 16-CE05 | Citation 等于 Verification | ALCE 独立评价 citation quality；support 可不完整 | citation 只作 mapping |
| 16-CE06 | Course plan 中有 fixture 就代表实验完成 | 当前没有 fixture、command、trace、Observed Result | 保持 NOT_RUN |

## Master Evidence Gate Result

- Gate: `EVIDENCE_GATE`
- Executor: `MASTER_ORCHESTRATOR / MASTER_DETERMINISTIC`
- Decision: `PASS`
- Lifecycle Transition: `RESEARCHING -> EVIDENCE_READY`
- Next Allowed Gate: `OUTLINE`
- Outline / Author Status: `NOT_STARTED / NOT_DISPATCHED`
- Blocker: `NONE`

| Claim | Status reviewed | Gate result | Master rationale |
|---|---|---|---|
| 16-C01 | PROPOSAL | PASS | 四对象分账明确标为课程工作定义；外部RAG论文只支撑窄历史语境，不冒充行业taxonomy。 |
| 16-C02 | PROPOSAL | PASS | 完整运行链明确标为审查模型；各产品可合并物理阶段，未声称统一标准或最优顺序。 |
| 16-C03 | CONFIRMED | PASS | DPR、BEIR与Elastic / Azure官方文档共同支持策略类别、融合责任及workload / metric范围；所有universal winner与Article 16效果结论均排除。 |
| 16-C04 | PROPOSAL | PASS | eligibility与relevance分账保持设计语态；pre/post-filter、认证授权与同步限制均已保留。 |
| 16-C05 | CONFIRMED | PASS | ALCE直接分离correctness与citation quality并记录支持不完整；课程Evidence contract进一步限定applicability与Verification，不把citation presence升级为接受。 |
| 16-C06 | PROPOSAL | PASS | fixture、gold query、版本化config与stage observation只作为实验合同；当前没有运行、raw artifact或observed improvement。 |

### Cross-cutting checks

- Evidence Card completeness: `6 / 6`；每卡`28 / 28`必填字段存在并可定位。
- Source traceability: `9 / 9` primary / official来源记录URL、retrieved date与version / page scope；live spot-check与当前窄措辞一致。
- Status strength: `2 CONFIRMED / 0 PARTIAL / 4 PROPOSAL / 0 BLOCKED`；没有把课程设计或未运行实验升级为外部事实。
- Experiment boundary: `16-EXP01 = PROPOSAL / NOT_RUN`；Observed Result=`ABSENT`；Raw Artifact=`NONE`。
- Effect exclusion: 未形成Article 16 fixture上的recall、precision、ranking、accuracy、latency、cost或answer utility结论。
- Concept boundary: Knowledge Base、RAG、Memory、Evidence分别承担collection、request / step retrieval-generation、cross-step retention与claim-scoped support职责；共用设施不等于同义。
- Future boundary: 未预写Article 17 Skill、Article 19完整权限系统或BuildPilot production Runtime；只保留显式defer / non-goal seam。

- Claim coverage: 6 / 6。
- Status summary: CONFIRMED=2、PROPOSAL=4、PARTIAL=0、BLOCKED=0。
- Core behavioral BLOCKED: 0。
- Experiment gap: 16-EXP01 NOT_RUN，但所有依赖效果的表述已删除或限定为 PROPOSAL；因此当前 gap 不阻止窄 Evidence Gate。
- Gate Decision: PASS -> EVIDENCE_READY。
- Future Author hard boundaries:
  - 必须把四对象边界和完整 RAG chain 标为课程工作模型。
  - 不得声称 Dense、Hybrid、Rerank、Filter placement、Top-K、chunk 或 inject format 有任何已观察优势。
  - 不得把 retrieval score、semantic similarity、filter survivor 或 citation presence 写成 Evidence acceptance / Verification。
  - 产品事实必须保留来源页面、检索日与版本 / preview scope。
  - 若 Outline / Draft 需要具体效果或新的核心事实，必须 RETURN_TO_RESEARCH。

本次Master Gate approval只推进到`EVIDENCE_READY / OUTLINE NOT_STARTED`；不创建Outline / Draft，不启动Author，不进入未来Gate。
