# Article Card｜15 Session、Long-term Memory 与 Project Memory

## Canonical Metadata

- Article ID: `15`
- Part: `III｜Agent 的信息、状态与知识`
- Weight: `M`
- Optional: `NO`
- Mode: `NORMAL_ARTICLE`
- Required Lab: `NONE`

## Course Position

- Entry boundary: Article 11定义Checkpoint与恢复；Article 12定义单个Step的Context；Article 13定义Context debugging；Article 14定义task-scoped Working Memory与Investigation State。
- This article: 研究跨Run、跨Session、跨任务保存信息时的scope、lifecycle、authority、freshness、conflict、write与recall policy。
- Forward boundary: Article 16才展开Knowledge Base / RAG的Retrieve、Filter、Rerank、Inject与Cite；Article 19才展开完整Permission / Approval / Sandbox。

## Required Distinctions

- Context、History、Working Memory、Session、Long-term Memory、Project Memory、Checkpoint七个对象必须分层，并与Knowledge Base切清边界。
- Stored、Retrieved与Injected into Context不是同一动作。
- Working Memory hypothesis不得无证据直接promotion为durable fact。
- Project Memory不得冒充Current Reality；recall后仍需scope、freshness与verification。

## Engineering Anchor

- BuildPilot / Unity跨版本调查：旧build中的WeChat Mini Game启动瓶颈被后续Session recall时，必须验证build scope、observed_at、source与当前测量。
- Memory Write Candidate、promotion authority、recall policy与conflict schema若为课程综合设计，必须标`COURSE PROPOSAL`。

## Non-goals

- 不重写Article 14的Investigation State schema、epistemic taxonomy或mutation pipeline。
- 不写Vector DB、Embedding、Chunking、Retriever、Reranker或完整RAG pipeline。
- 不提前展开Article 19的完整权限系统。
- 不创建Article 16资产，不宣称BuildPilot production Runtime存在。

## Gate Status

- Article Card: `READY FROM CANONICAL + HUMAN RESUME BRIEF`
- Research: `NOT_STARTED`
- Evidence: `BLOCKED / NOT_STARTED`
