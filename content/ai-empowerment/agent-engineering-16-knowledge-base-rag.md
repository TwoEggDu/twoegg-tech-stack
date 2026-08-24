---
title: "Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite"
slug: "agent-engineering-16-knowledge-base-rag"
date: "2026-08-24T00:00:00+08:00"
description: "梳理 Agent 如何通过 Retrieve、Filter、Rerank、Inject、Cite 将外部知识以可追溯的检索链路接入工作流。"
draft: false
tags:
  - "Agent Engineering"
  - "AI Engineering"
  - "RAG"
  - "Knowledge Base"
series: "Agent Engineering"
primary_series: "agent-engineering"
series_role: "article"
series_order: 170
weight: 3170
---

> **上一篇**：[Session、Long-term Memory 与 Project Memory：事实、经验和作用域]({{< relref "ai-empowerment/agent-engineering-15-session-long-term-project-memory.md" >}})

# Knowledge Base 与 RAG：Retrieve、Filter、Rerank、Inject、Cite

> 如果这篇只记一句话，我建议记这个：**检索结果只是候选；搜到、选中、注入、引用和验证，是五个不同判断。**

在 Agent 工程里，“已经接入知识库”经常被压缩成一个过于顺滑的故事：

~~~text
用户提问 -> 搜到相似片段 -> 放进模型输入 -> 给出带引用的回答
~~~

这条故事省略了真正危险的部分：相似片段是否属于当前项目，版本是否兼容，当前请求者是否有资格读取，内容是否仍然新鲜，片段是否真的支撑答案里的具体主张，以及什么时候必须回到当前源码、构建制品或运行环境重新核验。

本文会采用两种明显不同的表述：

- **COURSE PROPOSAL / 课程审查模型**：Knowledge Base、RAG、Memory、Evidence 的职责分账，以及 Retrieve -> Filter -> Rerank -> Inject -> Cite 的审查链。这是一套便于工程评审的工作模型，不是行业统一 taxonomy，不要求每个产品具备独立组件，也不宣称这是普遍最优的物理执行顺序。
- **CONFIRMED / 窄范围事实**：Keyword、Vector、Hybrid 改变候选生成或融合方式，实际效果依赖 workload 与评价口径；citation presence 与 citation correctness / completeness 可以分开评价。这里的确认不包含本文实验效果，也不包含生产系统结论。

还有一条必须先冻结的边界：**16-EXP01 = PROPOSAL / NOT_RUN；Observed Result = ABSENT；Raw Artifact = NONE；拟议 fixture = NOT_CREATED。** 因此本文不会给出任何 recall、precision、ranking、accuracy、latency、cost、answer utility、quality improvement 或赢家结论。

## 1. 问题空间：为什么“搜到”不是“可以相信并使用”

假设一个 Agent 正在调查 Unity 构建失败。知识集合里有一条标题和错误码都很相似的 Jenkins 历史事故。它看起来很有价值，但至少还缺这些判断：

- 它属于同一个项目吗？
- Unity、SDK、平台和构建配置处于兼容范围吗？
- 它描述的是当前事实，还是只在旧构建中成立的历史经验？
- 当前 principal 是否可以读取并向本 Step 暴露这段内容？
- 被引用的段落是否真的支持答案中的具体 Claim？
- 该 Claim 是否仍需用当前源码、配置、制品或运行观测核验？

因此，本文先拒绝一组危险等号：

~~~text
Stored != Retrieved != Selected != Injected != Cited != Verified
~~~

Stored 只表示某种内容存在于集合或存储中；Retrieved 只表示查询产生了候选；Selected 只表示某个选择策略保留了它；Injected 只表示应用把它装入本次可见 Context；Cited 只表示答案与 locator 之间建立了映射；Verified 才表示已经完成相应范围的独立核验。

这也是为什么问题不应从“选哪个向量数据库”开始。数据库和检索 API 可以提供候选、属性、分数或过滤接口，却不会自动替应用回答 scope、authority、freshness、support 与 acceptance。

## 2. 抽象模型：四个对象可以共用设施，但不能互相冒充

下面四项是 **COURSE PROPOSAL / 课程工作定义**。它们描述逻辑责任，不要求四套物理存储。

| Object | 本课程中的责任 | 不自动等于 |
|---|---|---|
| Knowledge Base | 经过组织、可检索并带 source、version、scope 边界的知识集合 | 聊天历史、当前真相、一次 search hit |
| RAG | 围绕当前 request / step 检索外部知识、选择与装配材料，再参与生成的模式 | Knowledge Base 本身、Memory 系统、Evidence acceptance |
| Memory | 在步骤或 Session 之间保留、恢复、召回与治理信息或状态的机制 | Current Reality、Knowledge Base、已验证 Evidence |
| Evidence | 针对明确 Claim，带 provenance、scope、observation、proves / does-not-prove 与 limitation 的可审计支持 | 相似候选、retrieval score、citation presence |

这里最容易混淆的是“共用 store”。Project Memory 和 Knowledge Base 完全可能引用同一份 Markdown、索引或数据库，但共享介质不会改变它们的 authority。Project Memory 可以告诉 Agent：“过去有过相似问题，locator 在这里”；它不能仅凭长期保存或再次召回，就把历史记录升级为当前项目的权威事实。

本文承接上一层已经建立的四段分账：

~~~text
Stored -> Retrieved -> Eligible -> Injected
~~~

其中 Memory 侧的 promotion、freshness、conflict 和 lifecycle 仍由原有边界负责。本文只把当前任务里的检索、资格判断、相关性排序、Context 装配和引用映射继续拆细，不重写 Memory schema。

同样，Evidence 也不是“质量更高的搜索结果”。一个 retrieved item 在被接受为某个 Claim 的 Evidence 之前，仍需回答：来源是什么、覆盖哪个版本和 scope、实际观察了什么、能证明什么、不能证明什么、当前是否还适用。本文只建立检索材料抵达 Evidence acceptance 之前的接口，不展开完整 Evidence Contract。

## 3. 抽象模型：Retrieve -> Filter -> Rerank -> Inject -> Cite 是审查链，不是统一产品协议

本文采用下面的 **COURSE PROPOSAL / review model**：

~~~text
Task + Current State
  -> Query -> Retrieve -> Filter -> Rerank
  -> Inject -> Cite -> Use / Reject / Verify
~~~

这条链表达“评审时必须分别问什么”，而不是“系统必须部署七个服务”。某个 Provider 可以在一次 API 调用中融合检索、属性过滤和排序；某个搜索引擎也可以选择物理 pre-filter 或 post-filter。即使物理步骤融合，责任、配置、拒绝理由和可观察输出仍不应一起消失。

| Stage | 要回答的问题 | 最小可审查输出 | 该输出不证明什么 |
|---|---|---|---|
| Query | 当前目标与限制被怎样表达？ | query ID、raw query / rewrite、project、version、scope、requester、time | corpus 一定有答案 |
| Retrieve | 哪些内容成为候选？ | source ID、locator、candidate score、retriever / index revision | 候选当前适用、可访问或可信 |
| Filter | 哪些候选具备进入后续处理的资格？ | kept / rejected、policy verdict、exact reason、metadata revision | survivor 对任务最相关或内容正确 |
| Rerank | eligible candidates 中谁更符合当前任务？ | rank、score / feature、ranker / config revision | top-1 是真相、权限已满足 |
| Inject | 哪些 excerpt 以什么顺序进入本 Step？ | selected、omitted、truncated、conflict、source metadata、Context Receipt ref | 模型完整使用了材料或答案正确 |
| Cite | 答案中的 Claim / span 回指哪里？ | claim / span -> source ID / locator / version mapping | 来源正确、完整、当前适用或 Claim 已验证 |
| Use / Reject / Verify | 当前怎样处置该 Claim？ | disposition、reason、verification ref 或 UNKNOWN | 独立核验已经发生，除非有对应 observation |

这张表真正保留的是 failure semantics。Query 可能遗漏版本限定；Retrieve 可能漏召回或返回错误候选；Filter 可能放过 wrong-scope 内容，也可能误拒合法内容；Rerank 可能把关键项压低；Inject 可能裁掉限定条件或隐藏冲突；Cite 可能指向错误 locator，或指向一个并不支持该 Claim 的段落。

如果系统只留下最终答案和几个链接，这些失败会被压缩成同一个“RAG 回答不好”。一旦保留逐段 trace，调查才有机会定位：问题发生在候选生成、资格政策、任务相关性、Context 装配，还是 Claim 支持关系。

## 4. 具体工程（一）：Keyword、Vector、Hybrid 只改变候选生成或融合

这一节只使用 **CONFIRMED / 窄范围事实**：lexical、dense 和 hybrid 是不同的候选生成或融合策略；它们的效果必须绑定数据集、query、qrels、实现、配置和 metric。DPR、BEIR 以及当前 Elastic / Azure 文档支持这种有限判断，但不支持“Dense 恒优”或“Hybrid 普遍胜出”。

| Strategy | 改变的责任 | 可能利用的信号 | 已知盲区与限制 |
|---|---|---|---|
| Keyword / lexical | 用词项或稀疏表示产生和排序候选 | error code、symbol、identifier、专有词 | paraphrase 或词面变化可能漏召回 |
| Vector / dense | 用表示与相似度产生候选 | 语义接近、同义改写 | 受 model、domain、chunk、index 与 metric 影响；不表达 authority |
| Hybrid | 融合多路 candidate 或 ranking | exact 与 semantic signal 的组合 | fusion、candidate depth、dataset 与实现会改变结果 |

DPR 的数字属于它自己的 open-domain QA setup 和评价口径；BEIR 的异构 benchmark 则提醒我们，不同 retrieval family 的表现会随数据集与设置变化。工程判断不是从一篇论文挑出一个赢家，而是把结论牢牢绑定到它真正覆盖的 workload。

> **SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM**
>
> 设想一个 Unity / Jenkins 事故集合：记录 A 与当前查询共享完全相同的错误码，但 project 不同；记录 B 没有共享关键词，却描述了相似故障，不过 Unity version 不兼容。这个例子只说明不同 Retrieve strategy 可能产生不同候选，以及候选仍需 scope / version 审查。它不是实验日志，没有候选输出、分数、排名或效果结果。

无论候选来自 lexical、dense 还是 hybrid，Retrieve 都没有资格替 Filter 裁定 project / permission，也没有资格替 Evidence 判断该段内容是否支撑当前 Claim。

## 5. 具体工程（二）：Filter 管资格，Rerank 管任务相关性

下面的职责切分是 **COURSE PROPOSAL**：

~~~text
Retrieved Candidates
  -> eligibility / policy verdict
  -> Eligible Candidates
  -> task relevance ranking
~~~

Filter 回答“它有没有资格进入当前任务的候选集”，Rerank 回答“在有资格的候选中，它对当前任务有多相关”。两者都很重要，但不能用一个 similarity score 互相替代。

| Constraint | Filter 应问什么 | 可保留的 verdict | Rerank 不得做什么 |
|---|---|---|---|
| project | 是否属于当前 repository / product identity？ | ACCEPT、REJECT、HISTORICAL_ONLY、UNKNOWN | 用高相似度抵消项目错误 |
| version | source / API / artifact version 是否兼容？ | ACCEPT、REJECT、VERIFY、UNKNOWN | 把版本不兼容降成一个小分差 |
| permission | 当前 principal 是否可读、可向本 Step 暴露？ | ACCEPT、REJECT、UNKNOWN；未知时 fail closed | 把权限当作普通 relevance feature |
| freshness | observed_at、indexed_at 或 source revision 是否仍在允许范围？ | CURRENT、HISTORICAL_ONLY、VERIFY、UNKNOWN | 把相似旧记录伪装成 Current Reality |
| scope | tenant、user、environment、platform、build、profile 是否匹配？ | ACCEPT、REJECT、SCOPED_ALTERNATIVE、UNKNOWN | 让 Top-K 吞掉 scope verdict |

这里的“先 Filter、后 Rerank”只表示逻辑责任顺序，不规定数据库的物理执行计划。产品可能为了索引特性、召回率或成本采用不同 filter placement；本文没有实验，不能给出普遍最优配置。

permission 还需要额外克制。metadata filter 最多参与 candidate eligibility；完整授权还涉及真实 identity、权限元数据、同步和 enforcement。一个字符串字段匹配成功，不是 authentication、authorization、ACL、sandbox 或 compliance closure 的证明。

> **SYNTHETIC ILLUSTRATIVE / NOT EXECUTED / NO RUNTIME CLAIM**
>
> 一个供评审使用的事故记录 schema 可以包含 incident ID、project、Unity version、platform、build ID、observed_at、scope、permission groups、source ID 与 locator。评审者可以设计“同错误码但项目不同”“旧记录更相似但已过期”“未授权记录有强 exact match”三类冲突，检查系统是否为每个 reject / historical-only / verify 决定留下明确原因。这里没有创建记录，没有运行查询，也没有任何 observed survivor 或 ranking。

一个安全的审查问题是：**如果最高分候选 project 错了，系统在哪里明确写下了 REJECT，而不是只把它排到第二名？**

## 6. 具体工程（三）：Inject 与 Cite 留下 provenance，不替系统做验证决定

进入 Inject 的已经是“可考虑装配的候选”，仍不是“已经被模型正确使用的事实”。

Inject 至少要保留：

- 实际选择的 excerpt、顺序与 source ID / locator；
- source version、scope、conflict 与 qualifier；
- 因预算被 omitted 或 truncated 的候选；
- 能回到本 Step application-visible Context Snapshot 的 Receipt ref。

Context Budget 的重点不是找到一个万能 Top-K。它首先要保护当前目标、必要限定、权威 State 与输出余量，再决定外部材料的数量和长度。没有具体 workload observation 时，不能声称某种 chunk size、Top-K 或 inject format 会提高 answer utility。

Context Receipt 也有严格上限：它记录应用可见的选择、排除、版本和冲突，让装配决策可回查；它不证明 Provider 内部怎样使用输入，更不证明模型确实关注、理解或忠实采用了某段内容。

Cite 的最小责任则是建立：

~~~text
answer claim / span -> actually injected source ID / locator / version
~~~

ALCE 把答案 correctness 与 citation quality 等维度分开评价，并观察到引用支持仍可能不完整。由此可以确认一个窄边界：**有 citation** 与 **citation 正确、完整地支持 Claim** 不是同一事实。

评审 citation 时，可以沿着下面的梯子逐层问：

1. locator 是否存在？
2. 对应 source 是否真的被 Retrieve，并实际进入本次 Inject？
3. 被引用段落是否支持答案中的这个 Claim？
4. source 的 scope、version、freshness 与 authority 是否适用于现在？
5. 该 Claim 是否仍要求独立 observation 或 experiment？

第一层通过不能替第五层做决定。Citation presence != support correctness / completeness != current applicability / authority != Evidence acceptance / Verification。

## 7. 工程判断：实现可以融合，责任和拒绝理由不能消失

审查一个 RAG 实现时，不必先数它有几个服务。更有效的做法，是寻找下面这些越界及其最小 guard：

| 坏实现 | 混淆了什么 | 最小 guard |
|---|---|---|
| Knowledge Base 直接冒充 Memory | collection 与跨 Session 治理 | 保留 promotion、freshness、conflict 与 lifecycle authority |
| retrieval score 直接决定可用 | relevance signal 与 eligibility | 保存 filter verdict、reason 与 metadata revision |
| reranker top-1 直接成为答案 | task relevance 与 truth | 只在 eligible candidates 中排序；Claim 仍走 acceptance |
| Inject 后宣称“模型已使用” | application assembly 与 model behavior | Receipt 只记录 application-visible package |
| 有链接就宣称答案可验证 | provenance mapping 与 support / applicability | 保存 claim-to-source mapping，并独立判断 support |
| 一个 permission 字段宣称授权闭环 | metadata eligibility 与真实 enforcement | 未知或不同步时 fail closed，不越权外推 |

由此可以得到一份最小 audit trail。它仍是 **COURSE PROPOSAL**，可以按风险裁剪，但不应靠最终答案反推：

- query 与 current goal / scope；
- candidate ref、source ID、locator、version、observed_at；
- retriever / index / ranker config revision；
- filter kept / rejected verdict 与 exact reason；
- rerank before / after order；
- Inject selected / omitted / truncated set 与 Context Receipt ref；
- citation 的 claim / span mapping；
- Use / Reject / Verify disposition、reason 与 verification ref。

这份 trace 的价值是让错误可以被定位，不是给系统颁发“已验证”证书。字段齐全也只能证明应用记录了什么；记录是否真实、策略是否合适、来源是否权威、Claim 是否被接受，仍需各自的审查。

本文的工程能力落点也在这里：

- 评审 retrieval design 时，能分开 candidate、eligibility、relevance、assembly 与 claim support；
- 复用历史事故时，能对 project、version、build、freshness 与 scope 给出 fail-closed 或 verify 理由；
- 设计 trace 时，能把 locator、config、filter reason、Receipt 与 citation mapping 串成可追踪链；
- 设计实验时，能冻结 fixture、gold query、配置、阶段输出和失败 criteria；
- 写结论时，能让 proposal、confirmed fact 与 absent observation 保持不同证据强度。

## 8. 验证边界：16-EXP01 只定义怎样观察，不提供任何结果

先重复当前真实状态：

| Field | Current state |
|---|---|
| Experiment | 16-EXP01 |
| Status | PROPOSAL / NOT_RUN |
| Proposed corpus | 10 synthetic Markdown incidents / NOT_CREATED |
| Observed Result | ABSENT |
| Raw Artifact | NONE |
| Runtime / production claim | NONE |

如果未来获得独立授权，实验开始前需要冻结以下变量：

- corpus revision 与每条 incident 的 source locator；
- gold annotations 与 query set revision；
- retriever、embedding、index、chunking、fusion 与 ranker 的版本化配置；
- 每个阶段的 Top-K accounting；
- Filter policy / metadata revision；
- Inject budget、selection、omission 与 truncation 记录；
- citation support label 与 Use / Reject / Verify disposition rubric。

建议的 query families 只用于设计覆盖面，不是已运行用例：

| Query family | 想观察的边界 | Gold / expected annotation |
|---|---|---|
| Q-EXACT | error code、symbol、identifier 的精确检索 | relevant IDs + locator |
| Q-PARAPHRASE | 同义改写且缺少共同关键词 | relevant IDs |
| Q-PROJECT-CONFLICT | 语义相似但 project 错误 | eligible / rejected IDs + reason |
| Q-VERSION-CONFLICT | Unity / SDK version 不兼容 | compatible range + verify verdict |
| Q-PERMISSION | 未授权内容具有高相关性 | allowed IDs；任何 unauthorized exposure 都是失败 |
| Q-FRESHNESS | 旧事故更相似但已过期 | current / historical-only labels |
| Q-CITATION-MISMATCH | answer Claim 与 locator 不匹配 | claim-to-source support labels |
| Q-NO-ANSWER | corpus 没有足够支持 | REJECT / UNKNOWN / VERIFY |

可以在实验合同里定义 metric，却不能在运行前填入结果：

- Recall@K / Precision@K：观察 relevant items 的覆盖与候选纯度；
- MRR / nDCG@K：观察 gold relevance 下的 ranking；
- scope violation / permission exposure / false rejection：观察 eligibility policy；
- budget / omission trace：观察关键限定是否在 Inject 中丢失；
- claim-to-source support：观察 citation mapping 是否真的支撑 Claim；
- unsupported acceptance：观察无足够支持时是否仍被当成 confirmed answer。

每个 run 都需要保存 Query、Retrieve candidates、Filter removals、Rerank before / after、Injected excerpts 与 omitted set、Citation mappings，以及 Use / Reject / Verify disposition。只有最终回答、没有阶段输出的 run，无法支撑本文要审查的因果边界。

未来实验的失败 criteria 可以预先写清：

- 未授权内容被暴露给当前 Step；
- wrong-project、wrong-version 或 stale candidate 被当作 current eligible material；
- citation 没有映射到本次实际 Inject 的 source；
- cited passage 不支持对应 Claim，却被接受；
- Q-NO-ANSWER 被编造成确定答案；
- 关键 qualifier 在 Inject 中被静默裁掉。

这些是未来实验的判失败条件，不是本轮观察。即使 synthetic fixture 将来执行通过，也只能说明该固定 fixture、配置和评价口径下的行为，不能外推为 production authorization、scale、latency、cost、跨领域质量或可靠性认证。

## Learning Check

### 1. Retrieve 后发现候选来自不同 project / build，缺少哪个判断？为什么不能只让它排第一？

**参考思路**：缺少的是 eligibility / scope verdict。Rerank 只能在有资格的候选中讨论当前任务相关性；高 similarity 不能抵消 project、build 或 version 不兼容。系统应留下 REJECT、HISTORICAL_ONLY、VERIFY 或 UNKNOWN 及理由，而不是用排名隐藏 policy 决定。

### 2. 一个 API 融合 search、filter 与 rank，为什么仍要分开责任？

**参考思路**：分开的是审计问题，不是强制部署拓扑。候选从哪里来、哪些内容因 policy 被拒绝、eligible set 如何排序，拥有不同失败语义和证据。一次物理调用可以完成多项工作，但 trace 仍应让这些理由可定位。

### 3. excerpt 已经 Inject 且有 locator，能否证明模型使用了它、回答正确或完成了 Verification？

**参考思路**：都不能。Inject 最多证明应用把 excerpt 放进可见 Context；Receipt 记录装配；locator 建立 provenance pointer。模型是否采用、答案是否忠实、source 是否适用以及是否完成独立核验，都是后续判断。

### 4. 答案带 citation，但 passage 不支持该 Claim，哪一层失败？Citation presence 还能证明什么？

**参考思路**：失败发生在 claim-to-source support correctness。Citation presence 最多证明存在一个引用标记或 locator mapping；它不证明 support completeness、source truth、当前 applicability、Evidence acceptance 或 Verification。

### 5. 为什么不能用 DPR 的 scoped result 直接选出 Unity / Jenkins 或 16-EXP01 的赢家？

**参考思路**：DPR 的数字绑定其数据集、baseline、模型和 metric；本文拟议 corpus、query、配置与 observation 都不存在。跨 workload 外推会越过 Evidence ceiling。本文只能确认 strategy 类别与“效果依赖 workload / metric”，不能选择 universal winner。

### 6. 在 16-EXP01 运行前，哪些内容可以写，哪些必须缺席？

**参考思路**：可以写 fixture schema、query families、待冻结变量、阶段输出、metrics 定义、limitations 与失败 criteria。必须缺席的是任何 observed value、策略优劣、ranking 改善、answer utility、latency、cost、quality gain、production reliability 或“实验通过”表述。

## 最终 Claim Traceability（6 / 6）

| Claim | Status ceiling | 正文位置 | Evidence anchor | 本稿措辞边界 |
|---|---|---|---|---|
| 16-C01 | PROPOSAL | 1、2、7 | 16-E01；Glossary；Article 15；Lewis et al. | 明确写成课程四对象分账；不称行业 taxonomy 或物理分库要求 |
| 16-C02 | PROPOSAL | 1、3、6、7 | 16-E02；Lewis et al.；OpenAI；Elastic；ALCE | 明确写成 review model；不称必备组件或最优物理顺序 |
| 16-C03 | CONFIRMED | 4、7 | 16-E03；DPR；BEIR；Elastic；Azure | 只确认 strategy / fusion 与 workload / metric 依赖；不写本文效果或赢家 |
| 16-C04 | PROPOSAL | 5、7 | 16-E04；Article 15；OpenAI；Azure | eligibility 与 relevance 逻辑分账；不规定 filter placement 或权限闭环 |
| 16-C05 | CONFIRMED | 6、7 | 16-E05；ALCE；Glossary | 只确认 citation presence 与 support correctness / completeness 可分；不升级为 applicability / Verification |
| 16-C06 | PROPOSAL | 8 | 16-E06；BEIR；ALCE；16-EXP01 design | 只写实验合同；保持 NOT_RUN / ABSENT / NONE / NOT_CREATED |

Coverage：**6 / 6**。如果后续修订需要新增核心事实、具体效果或超过上述 ceiling 的措辞，应 RETURN_TO_RESEARCH，而不是在正文中补结论。

## 参考资料

### 课程与依赖

- Agent Engineering 课程术语表（repository production reference；无公开Hugo route）
- [Article 12｜Context Engineering：每一个 Step 到底应该看到什么]({{< relref "ai-empowerment/agent-engineering-12-context-engineering.md" >}})
- [Article 15｜Session、Long-term Memory 与 Project Memory：事实、经验和作用域]({{< relref "ai-empowerment/agent-engineering-15-session-long-term-project-memory.md" >}})
- Article 16 Research（repository production reference；无公开Hugo route）
- Article 16 Evidence（repository production reference；无公开Hugo route）

### 论文与官方文档

- Lewis et al., [Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks](https://papers.neurips.cc/paper/2020/file/6b493230205f780e1bc26945df7481e5-Paper.pdf), NeurIPS 2020.
- Karpukhin et al., [Dense Passage Retrieval for Open-Domain Question Answering](https://aclanthology.org/2020.emnlp-main.550/), EMNLP 2020.
- Thakur et al., [BEIR: A Heterogeneous Benchmark for Zero-shot Evaluation of Information Retrieval Models](https://arxiv.org/abs/2104.08663).
- Gao et al., [Enabling Large Language Models to Generate Text with Citations](https://aclanthology.org/2023.emnlp-main.398/), EMNLP 2023.
- OpenAI API, [Search vector store](https://developers.openai.com/api/reference/python/resources/vector_stores/methods/search), retrieved 2026-08-24.
- Elastic, [Hybrid search](https://www.elastic.co/docs/solutions/search/hybrid-search), retrieved 2026-08-24.
- Elasticsearch 8.19, [Re-ranking overview](https://www.elastic.co/guide/en/elasticsearch/reference/8.19/re-ranking-overview.html), retrieved 2026-08-24.
- Microsoft Learn, [Hybrid search in Azure AI Search](https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview), retrieved 2026-08-24.
- Microsoft Learn, [Document-level access control in Azure AI Search](https://learn.microsoft.com/en-us/azure/search/search-document-level-access-overview), retrieved 2026-08-24.

后续课程将分别讨论 Skill、Evidence Contract 与 Permission / Approval / Sandbox；本文只保留接口边界，不预设任何未来实现。

## 9. 最短结论

**检索结果只是候选：先分清对象、资格、相关性、注入与引用；能否支撑 Claim，仍取决于独立 Evidence / Verification。**
