# Article Card｜14 Working Memory 与 Investigation State

## Canonical Metadata

- Article ID: `14`
- Part: `III｜Agent 的信息、状态与知识`
- Weight: `L`
- Optional: `NO`
- Mode: `NORMAL_ARTICLE`
- Required Lab: `NONE`

## Course Position

- Entry boundary: Article 12回答单个Step应看到什么；Article 13回答这个view为何错。
- This article: 当前任务未结束时，Working Memory与Investigation State保存什么、如何受控演化。
- Forward boundary: Article 15才展开Session、Long-term Memory、Project Memory与跨任务生命周期；Article 16才展开Knowledge Base / RAG。

## Required Distinctions

- Working Memory vs Context Snapshot / History / authoritative Workflow State / Checkpoint / Long-term Memory / Evidence。
- Hypothesis is not Fact；至少研究`OBSERVED / INFERRED / HYPOTHESIS / REJECTED / UNKNOWN`的证据适用性。
- Model suggestion is not authoritative mutation；优先研究`model proposes -> host/reducer validates -> accepted mutation`。

## Engineering Anchor

- Unity / BuildPilot构建调查：从`CS0103` observation、候选根因、rejected hypothesis、unresolved question与pending action展示state evolution。
- 具体Investigation State schema必须由Research / Evidence决定；课程自定义部分必须标`COURSE PROPOSAL`。

## Non-goals

- 不把Working Memory写成完整聊天记录或Context Window。
- 不展开跨Session storage、长期个人记忆、Project Memory policy、Memory DB、Embedding、RAG。
- 不创建Article 15/16资产，不宣称BuildPilot production Runtime存在。

## Gate Status

- Article Card: `READY FROM CANONICAL + USER-APPROVED CANARY BRIEF`
- Research: `NOT_STARTED`
- Evidence: `BLOCKED / NOT_STARTED`
