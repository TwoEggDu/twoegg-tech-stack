# Article Card｜16 Knowledge Base 与 RAG

## Canonical Metadata

- Article ID: `16`
- Part: `III｜Agent 的信息、状态与知识`
- Weight: `M`
- Optional: `NO`
- Mode: `NORMAL_ARTICLE`
- Required Lab: `NONE`

## Course Position

- Entry boundary: Article 12定义Context的选择与装配；Article 15定义Session、Memory、authority与freshness。
- This article: 把Knowledge Base定义为带来源边界的可检索知识集合，把RAG定义为当前任务中的`Query -> Retrieve -> Filter -> Rerank -> Inject -> Cite -> Use / Reject / Verify`运行链。
- Forward boundary: Article 17才展开Skill Engineering；Article 18才完整展开Evidence Contract；Article 19才完整展开Permission / Approval / Sandbox。

## Required Distinctions

- Knowledge Base、RAG、Memory、Evidence必须分别定义，不得互相冒充。
- `Stored != Retrieved != Selected != Injected != Cited != Verified`。
- Keyword、Vector、Hybrid只作为Retrieve策略，不得写成完整RAG系统。
- Filter必须显式处理project、version、permission、freshness与scope；Rerank处理当前任务相关性，两者不得合并成“Top-K足够”。
- Citation提供provenance pointer，不自动证明来源正确、当前适用或回答忠实。
- 检索效果与具体系统效果需要实验；未经验证的推断只能标`PARTIAL / PROPOSAL / BLOCKED`。

## Engineering Anchor

- Unity / Jenkins历史事故与构建合同检索：相似故障若项目、Unity版本、平台、构建号或观测时间不同，必须在Filter / Rerank / Verify边界中显式处理。
- Retrieved Evidence候选至少携带source ID、locator、version / observed_at、scope、permission、retrieval score与citation mapping。

## Evidence and Experiment Needs

- RAG原始论文或权威一手资料，及当前官方检索 / citation文档。
- 一个固定的小型Markdown事故集与golden query set，用于比较Retrieve、Filter、Rerank、Inject、Cite各阶段的可观察差异。
- 实验必须保存fixture、query、阶段输出、评价口径与limitations；若未真实执行，不得写成observed result。

## Non-goals

- 不做向量数据库、Embedding模型或Chunking参数大全。
- 不设计企业级知识治理平台，不重写知识生产、准入、记账、保鲜和退场全链路。
- 不重写Article 15的Memory schema，不提前展开Article 17 Skill、Article 18 Evidence Contract或Article 19完整权限系统。
- 不宣称存在BuildPilot production RAG Runtime，不把synthetic fixture结果外推为生产效果。

## Gate Status

- Article Card: `READY FROM CANONICAL + HUMAN APPROVAL`
- Research: `COMPLETE / PASS`
- Evidence: `EVIDENCE_GATE PASS / 2 CONFIRMED / 4 PROPOSAL / 0 BLOCKED`
